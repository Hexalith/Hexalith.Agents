using System.Threading.Tasks;

using Hexalith.Agents.Agent;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Agent.Events;
using Hexalith.Agents.Contracts.Agent.Events.Rejections;

using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using static Hexalith.Agents.Tests.AgentTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Handle-method tests for <see cref="AgentAggregate"/> covering create/update/activate/disable, authorization
/// fail-closed (AC4), idempotent no-op vs conflicting-duplicate (AC1), draft tolerance, activation gates with
/// specific blockers and lifecycle-unchanged on block (AC2), and lifecycle/not-found rejections (AC3).
/// </summary>
public sealed class AgentAggregateTests
{
    // ===== Create =====

    [Fact]
    public void Create_with_no_state_and_admin_produces_created_draft()
    {
        CreateAgent command = ValidCreate();

        DomainResult result = AgentAggregate.Handle(command, state: null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        AgentCreated created = result.Events[0].ShouldBeOfType<AgentCreated>();
        created.AgentId.ShouldBe(AgentId);
        created.TenantId.ShouldBe(TenantId);
        created.DisplayName.ShouldBe("Hexa Assistant");
        created.Instructions.ShouldBe(ValidInstructions);
        created.ConfigurationVersion.ShouldBe(1);
        created.InstructionsVersion.ShouldBe(1);
    }

    [Fact]
    public void Create_empty_draft_is_allowed_with_zero_instructions_version()
    {
        CreateAgent command = ValidCreate(displayName: "", instructions: "");

        DomainResult result = AgentAggregate.Handle(command, state: null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        AgentCreated created = result.Events[0].ShouldBeOfType<AgentCreated>();
        created.DisplayName.ShouldBe(string.Empty);
        created.Instructions.ShouldBe(string.Empty);
        created.InstructionsVersion.ShouldBe(0);
    }

    [Fact]
    public void Create_without_agents_admin_produces_denied_and_no_created()
    {
        CreateAgent command = ValidCreate();

        DomainResult result = AgentAggregate.Handle(command, state: null, Envelope(command, isAgentsAdmin: false, actorUserId: "intruder"));

        result.IsRejection.ShouldBeTrue();
        AgentAdministrationDeniedRejection denied = result.Events[0].ShouldBeOfType<AgentAdministrationDeniedRejection>();
        denied.AgentId.ShouldBe(AgentId);
        denied.ActorUserId.ShouldBe("intruder");
        denied.CommandName.ShouldBe(nameof(CreateAgent));
        result.Events.ShouldNotContain(e => e is AgentCreated);
    }

    [Fact]
    public void Create_with_missing_tenant_scope_produces_invalid_configuration()
    {
        CreateAgent command = ValidCreate(tenantId: "   ");

        DomainResult result = AgentAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<InvalidAgentConfigurationRejection>();
    }

    [Fact]
    public void Create_with_over_long_instructions_produces_invalid_configuration_without_echoing_value()
    {
        string instructions = new('a', AgentConfigurationPolicy.MaxInstructionsLength + 1);
        CreateAgent command = ValidCreate(instructions: instructions);

        DomainResult result = AgentAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        InvalidAgentConfigurationRejection rejection = result.Events[0].ShouldBeOfType<InvalidAgentConfigurationRejection>();
        rejection.Reason.ShouldNotContain(instructions); // never echoes the offending value (AD-14)
        result.Events.ShouldNotContain(e => e is AgentCreated);
    }

    [Fact]
    public void Create_exact_duplicate_produces_noop()
    {
        CreateAgent command = ValidCreate();
        AgentState state = StateWith(command);

        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsNoOp.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void Create_conflicting_duplicate_produces_already_exists_and_no_mutation()
    {
        AgentState state = StateWith(ValidCreate());
        CreateAgent conflicting = ValidCreate(displayName: "A Different Name");

        DomainResult result = AgentAggregate.Handle(conflicting, state, Envelope(conflicting));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentAlreadyExistsRejection>();
        result.Events.ShouldNotContain(e => e is AgentCreated);
    }

    // ===== Update =====

    [Fact]
    public void Update_changes_safe_fields_and_instructions_bumps_both_versions()
    {
        AgentState state = StateWith(ValidCreate());
        var command = new UpdateAgentConfiguration("Hexa Renamed", "New description", "You are hexa, an updated and careful assistant.");

        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        AgentConfigurationUpdated updated = result.Events[0].ShouldBeOfType<AgentConfigurationUpdated>();
        updated.DisplayName.ShouldBe("Hexa Renamed");
        updated.InstructionsChanged.ShouldBeTrue();
        updated.ConfigurationVersion.ShouldBe(2);
        updated.InstructionsVersion.ShouldBe(2);
    }

    [Fact]
    public void Update_changing_only_display_name_does_not_bump_instructions_version()
    {
        CreateAgent create = ValidCreate();
        AgentState state = StateWith(create);
        var command = new UpdateAgentConfiguration("Hexa Renamed", create.Description, create.Instructions);

        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        AgentConfigurationUpdated updated = result.Events[0].ShouldBeOfType<AgentConfigurationUpdated>();
        updated.InstructionsChanged.ShouldBeFalse();
        updated.ConfigurationVersion.ShouldBe(2);
        updated.InstructionsVersion.ShouldBe(1); // unchanged from create
    }

    [Fact]
    public void Update_identical_configuration_produces_noop()
    {
        CreateAgent create = ValidCreate();
        AgentState state = StateWith(create);
        var command = new UpdateAgentConfiguration(create.DisplayName, create.Description, create.Instructions);

        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsNoOp.ShouldBeTrue();
    }

    [Fact]
    public void Update_missing_agent_produces_not_found()
    {
        var command = new UpdateAgentConfiguration("Name", "Desc", ValidInstructions);

        DomainResult result = AgentAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentNotFoundRejection>();
    }

    [Fact]
    public void Update_with_over_long_display_name_produces_invalid_configuration()
    {
        AgentState state = StateWith(ValidCreate());
        string displayName = new('x', AgentConfigurationPolicy.MaxDisplayNameLength + 1);
        var command = new UpdateAgentConfiguration(displayName, "Desc", ValidInstructions);

        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<InvalidAgentConfigurationRejection>();
    }

    [Fact]
    public void Update_without_agents_admin_produces_denied()
    {
        AgentState state = StateWith(ValidCreate());
        var command = new UpdateAgentConfiguration("Renamed", "Desc", ValidInstructions);

        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command, isAgentsAdmin: false));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentAdministrationDeniedRejection>();
    }

    // ===== Activate =====

    [Fact]
    public void Activate_valid_draft_produces_activated()
    {
        // Story 1.4 AC4: a linked Party identity is now a required activation gate.
        AgentState state = StateWithLinkedParty(ValidCreate());

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, Envelope(new ActivateAgent()));

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<AgentActivated>().AgentId.ShouldBe(AgentId);
    }

    [Fact]
    public void Activate_missing_display_name_blocks_with_specific_blocker_and_keeps_lifecycle_non_active()
    {
        AgentState state = StateWith(ValidCreate(displayName: ""));

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, Envelope(new ActivateAgent()));

        result.IsRejection.ShouldBeTrue();
        AgentActivationBlockedRejection blocked = result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>();
        blocked.Blockers.ShouldContain(AgentActivationBlocker.MissingDisplayName);

        ApplyAll(state, result);
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft); // AC2: rejected activation does not flip lifecycle
    }

    [Fact]
    public void Activate_missing_instructions_blocks_with_specific_blocker()
    {
        AgentState state = StateWith(ValidCreate(instructions: ""));

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, Envelope(new ActivateAgent()));

        AgentActivationBlockedRejection blocked = result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>();
        blocked.Blockers.ShouldContain(AgentActivationBlocker.MissingInstructions);
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft);
    }

    [Fact]
    public void Activate_invalid_instructions_blocks_with_specific_blocker()
    {
        AgentState state = StateWith(ValidCreate(instructions: "short")); // present but below the validity band

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, Envelope(new ActivateAgent()));

        AgentActivationBlockedRejection blocked = result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>();
        blocked.Blockers.ShouldContain(AgentActivationBlocker.InvalidInstructions);
        blocked.Blockers.ShouldNotContain(AgentActivationBlocker.MissingInstructions);
    }

    [Fact]
    public void Activate_already_active_produces_lifecycle_already_set()
    {
        AgentState state = ActiveStateWith(ValidCreate());

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, Envelope(new ActivateAgent()));

        result.IsRejection.ShouldBeTrue();
        AgentLifecycleStateAlreadySetRejection rejection = result.Events[0].ShouldBeOfType<AgentLifecycleStateAlreadySetRejection>();
        rejection.CurrentStatus.ShouldBe(AgentLifecycleStatus.Active);
        rejection.RequestedStatus.ShouldBe(AgentLifecycleStatus.Active);
        rejection.CommandName.ShouldBe(nameof(ActivateAgent));
    }

    [Fact]
    public void Activate_missing_agent_produces_not_found()
    {
        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state: null, Envelope(new ActivateAgent()));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentNotFoundRejection>();
    }

    [Fact]
    public void Activate_without_agents_admin_produces_denied()
    {
        AgentState state = StateWith(ValidCreate());

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, Envelope(new ActivateAgent(), isAgentsAdmin: false));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentAdministrationDeniedRejection>();
    }

    [Fact]
    public void Reactivate_disabled_valid_agent_reruns_gates_and_activates()
    {
        // A disabled agent that has a linked Party (1.4 AC4) and valid config re-activates when its gates pass.
        AgentState state = StateWithLinkedParty(ValidCreate());
        state.Apply(new AgentDisabled(AgentId));

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, Envelope(new ActivateAgent()));

        result.IsSuccess.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentActivated>();
    }

    [Fact]
    public void Reactivate_disabled_invalid_agent_reruns_gates_and_blocks()
    {
        AgentState state = DisabledStateWith(ValidCreate(displayName: ""));

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, Envelope(new ActivateAgent()));

        result.IsRejection.ShouldBeTrue();
        AgentActivationBlockedRejection blocked = result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>();
        blocked.Blockers.ShouldContain(AgentActivationBlocker.MissingDisplayName);
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Disabled); // not blindly flipped to active
    }

    // ===== Disable =====

    [Fact]
    public void Disable_active_agent_produces_disabled()
    {
        AgentState state = ActiveStateWith(ValidCreate());

        DomainResult result = AgentAggregate.Handle(new DisableAgent(), state, Envelope(new DisableAgent()));

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<AgentDisabled>().AgentId.ShouldBe(AgentId);
    }

    [Fact]
    public void Disable_draft_agent_produces_disabled()
    {
        AgentState state = StateWith(ValidCreate());

        DomainResult result = AgentAggregate.Handle(new DisableAgent(), state, Envelope(new DisableAgent()));

        result.IsSuccess.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentDisabled>();
    }

    [Fact]
    public void Disable_already_disabled_produces_lifecycle_already_set()
    {
        AgentState state = DisabledStateWith(ValidCreate());

        DomainResult result = AgentAggregate.Handle(new DisableAgent(), state, Envelope(new DisableAgent()));

        result.IsRejection.ShouldBeTrue();
        AgentLifecycleStateAlreadySetRejection rejection = result.Events[0].ShouldBeOfType<AgentLifecycleStateAlreadySetRejection>();
        rejection.CurrentStatus.ShouldBe(AgentLifecycleStatus.Disabled);
        rejection.RequestedStatus.ShouldBe(AgentLifecycleStatus.Disabled);
        rejection.CommandName.ShouldBe(nameof(DisableAgent));
    }

    [Fact]
    public void Disable_missing_agent_produces_not_found()
    {
        DomainResult result = AgentAggregate.Handle(new DisableAgent(), state: null, Envelope(new DisableAgent()));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentNotFoundRejection>();
    }

    [Fact]
    public void Disable_without_agents_admin_produces_denied()
    {
        AgentState state = ActiveStateWith(ValidCreate());

        DomainResult result = AgentAggregate.Handle(new DisableAgent(), state, Envelope(new DisableAgent(), isAgentsAdmin: false));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentAdministrationDeniedRejection>();
    }

    // ===== AC4: denial must not reveal whether the agent exists =====

    [Fact]
    public void Denied_mutation_does_not_reveal_agent_existence()
    {
        // The agent exists, but the unauthorized caller only ever sees a generic denial — never an
        // "already exists" / "not found" signal that would fingerprint whether hexa is configured.
        AgentState state = StateWith(ValidCreate());
        CreateAgent command = ValidCreate(displayName: "Conflicting");

        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command, isAgentsAdmin: false));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentAdministrationDeniedRejection>();
        result.Events.ShouldNotContain(e => e is AgentAlreadyExistsRejection);
        result.Events.ShouldNotContain(e => e is AgentNotFoundRejection);
    }

    // ===== Full reflection dispatch + JSON payload round-trip via ProcessAsync =====

    [Fact]
    public async Task ProcessAsync_create_round_trips_payload_and_dispatches_handler()
    {
        var aggregate = new AgentAggregate();
        CreateAgent command = ValidCreate();

        DomainResult result = await aggregate.ProcessAsync(Envelope(command), currentState: null);

        result.IsSuccess.ShouldBeTrue();
        AgentCreated created = result.Events[0].ShouldBeOfType<AgentCreated>();
        created.Instructions.ShouldBe(ValidInstructions);
        created.TenantId.ShouldBe(TenantId);
    }

    [Fact]
    public async Task ProcessAsync_create_activate_disable_threads_state_through_lifecycle()
    {
        var aggregate = new AgentAggregate();
        var state = new AgentState();

        DomainResult created = await ProcessAndApplyAsync(aggregate, state, ValidCreate());
        created.IsSuccess.ShouldBeTrue();
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft);

        // 1.4 AC4: a linked Party identity is required before activation.
        var link = new LinkAgentPartyIdentity(LinkedPartyId);
        (await ProcessAndApplyAsync(aggregate, state, link, LinkEnvelope(link))).IsSuccess.ShouldBeTrue();

        DomainResult activated = await ProcessAndApplyAsync(aggregate, state, new ActivateAgent());
        activated.IsSuccess.ShouldBeTrue();
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Active);

        DomainResult disabled = await ProcessAndApplyAsync(aggregate, state, new DisableAgent());
        disabled.IsSuccess.ShouldBeTrue();
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Disabled);
    }

    [Fact]
    public async Task ProcessAsync_create_without_admin_round_trips_to_denied()
    {
        var aggregate = new AgentAggregate();
        CreateAgent command = ValidCreate();

        DomainResult result = await aggregate.ProcessAsync(Envelope(command, isAgentsAdmin: false), currentState: null);

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentAdministrationDeniedRejection>();
    }
}
