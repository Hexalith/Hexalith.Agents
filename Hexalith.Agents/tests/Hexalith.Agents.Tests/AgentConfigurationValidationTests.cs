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
/// Gap-filling configuration-validation tests for <see cref="AgentAggregate"/> and the shared
/// <see cref="AgentConfigurationPolicy"/> (AC1, AC2). The dev-story suite covers the missing-tenant and
/// over-long-instructions/display-name rejections; these exercise the remaining storable-input branches that were
/// untested — the over-long-description bound (on create and update), the max-length boundaries that must be
/// <em>accepted</em>, optional-description normalization, and the instructions-validity band edges that decide
/// <see cref="AgentActivationBlocker.InvalidInstructions"/> versus a clean activation.
/// </summary>
public sealed class AgentConfigurationValidationTests
{
    // ===== Over-long description (the previously untested ValidateStorableInput branch) =====

    [Fact]
    public void Create_with_over_long_description_produces_invalid_configuration()
    {
        string description = new('d', AgentConfigurationPolicy.MaxDescriptionLength + 1);
        CreateAgent command = ValidCreate(description: description);

        DomainResult result = AgentAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        InvalidAgentConfigurationRejection rejection = result.Events[0].ShouldBeOfType<InvalidAgentConfigurationRejection>();
        rejection.Reason.ShouldNotContain(description); // never echoes the offending value
        result.Events.ShouldNotContain(e => e is AgentCreated);
    }

    [Fact]
    public void Update_with_over_long_description_produces_invalid_configuration_before_mutation()
    {
        AgentState state = StateWith(ValidCreate());
        string description = new('d', AgentConfigurationPolicy.MaxDescriptionLength + 1);
        var command = new UpdateAgentConfiguration("Hexa Assistant", description, ValidInstructions);

        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<InvalidAgentConfigurationRejection>();
        state.Description.ShouldBe("Tenant governed assistant"); // original metadata untouched (fails before mutation)
    }

    [Fact]
    public void Update_with_over_long_instructions_produces_invalid_configuration()
    {
        AgentState state = StateWith(ValidCreate());
        string instructions = new('a', AgentConfigurationPolicy.MaxInstructionsLength + 1);
        var command = new UpdateAgentConfiguration("Hexa Assistant", "desc", instructions);

        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        InvalidAgentConfigurationRejection rejection = result.Events[0].ShouldBeOfType<InvalidAgentConfigurationRejection>();
        rejection.Reason.ShouldNotContain(instructions);
    }

    // ===== Max-length boundaries must be accepted (off-by-one guard) =====

    [Fact]
    public void Create_with_display_name_at_max_length_is_accepted()
    {
        string displayName = new('x', AgentConfigurationPolicy.MaxDisplayNameLength);
        CreateAgent command = ValidCreate(displayName: displayName);

        DomainResult result = AgentAggregate.Handle(command, state: null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<AgentCreated>().DisplayName.ShouldBe(displayName);
    }

    [Fact]
    public void Create_with_instructions_at_max_length_is_accepted()
    {
        string instructions = new('a', AgentConfigurationPolicy.MaxInstructionsLength);
        CreateAgent command = ValidCreate(instructions: instructions);

        DomainResult result = AgentAggregate.Handle(command, state: null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
    }

    // ===== Optional-description normalization =====

    [Fact]
    public void Create_with_whitespace_description_normalizes_to_null()
    {
        CreateAgent command = ValidCreate(description: "   ");

        DomainResult result = AgentAggregate.Handle(command, state: null, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<AgentCreated>().Description.ShouldBeNull();
    }

    // ===== Instructions-validity band edges (decide InvalidInstructions vs. a clean activation) =====

    [Fact]
    public void Activate_with_instructions_exactly_at_min_length_succeeds()
    {
        // A present, trimmed-to-exactly-min instructions string sits on the valid edge of the band. A linked Party
        // is also required to activate (1.4 AC4), so the agent is created with one.
        string instructions = new('a', AgentConfigurationPolicy.MinInstructionsLength);
        AgentState state = StateWithLinkedParty(ValidCreate(instructions: instructions));

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, Envelope(new ActivateAgent()));

        result.IsSuccess.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentActivated>();
    }

    [Fact]
    public void Activate_with_instructions_one_below_min_length_blocks_with_invalid_instructions()
    {
        string instructions = new('a', AgentConfigurationPolicy.MinInstructionsLength - 1);
        AgentState state = StateWith(ValidCreate(instructions: instructions));

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, Envelope(new ActivateAgent()));

        result.IsRejection.ShouldBeTrue();
        AgentActivationBlockedRejection blocked = result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>();
        blocked.Blockers.ShouldContain(AgentActivationBlocker.InvalidInstructions);
        blocked.Blockers.ShouldNotContain(AgentActivationBlocker.MissingInstructions); // present-but-short, not missing
    }

    // ===== Lifecycle is distinct from callability: an update may push an Active agent into a blocked config =====

    [Fact]
    public void Update_blanking_display_name_on_active_agent_keeps_active_lifecycle_but_surfaces_blocker()
    {
        // By design (UX DR: agent-readiness-badge must not collapse "active lifecycle" with "callable"), an
        // update that blanks a required field is not a structural error and does not auto-demote the lifecycle.
        // The status path independently recomputes the blockers, so a consumer can still tell that the Active
        // agent is no longer callable as configured — lifecycle state and the readiness/blocker set stay distinct.
        AgentState state = ActiveStateWith(ValidCreate());
        var command = new UpdateAgentConfiguration("", state.Description, ValidInstructions);

        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        ApplyAll(state, result);
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Active); // the edit does not flip lifecycle

        AgentStatusView view = AgentInspection.GetStatus(state, isAgentsAdmin: true).Agent.ShouldNotBeNull();
        view.Lifecycle.ShouldBe(AgentLifecycleStatus.Active);
        view.ActivationBlockers.ShouldContain(AgentActivationBlocker.MissingDisplayName);
    }
}
