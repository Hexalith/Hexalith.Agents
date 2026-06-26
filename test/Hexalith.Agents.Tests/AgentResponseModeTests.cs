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
/// Handle-method tests for the Response Mode configuration behaviour added to <see cref="AgentAggregate"/>
/// (Story 1.6 AC1). Covers: recording Automatic/Confirmation and bumping the configuration version while lifecycle
/// stays unchanged, rejecting the <c>Unknown</c> sentinel, idempotent same-mode no-op, a future-only mode change,
/// authorization / not-found fail-closed behaviour, and replay through <c>Apply</c>.
/// </summary>
public sealed class AgentResponseModeTests
{
    [Theory]
    [InlineData(AgentResponseMode.Automatic)]
    [InlineData(AgentResponseMode.Confirmation)]
    public void Configure_mode_records_it_bumps_configuration_version_and_keeps_lifecycle(AgentResponseMode mode)
    {
        AgentState state = StateWith(ValidCreate()); // ConfigurationVersion = 1, ResponseMode = Unknown

        var command = new ConfigureAgentResponseMode(mode);
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsSuccess.ShouldBeTrue();
        AgentResponseModeConfigured configured = result.Events[0].ShouldBeOfType<AgentResponseModeConfigured>();
        configured.AgentId.ShouldBe(AgentId);
        configured.Mode.ShouldBe(mode);
        configured.ConfigurationVersion.ShouldBe(2);

        ApplyAll(state, result);
        state.ResponseMode.ShouldBe(mode);
        state.ConfigurationVersion.ShouldBe(2);
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft); // choosing a mode never changes lifecycle (Story 1.3 invariant)
    }

    [Fact]
    public void Configure_unknown_mode_is_rejected_as_invalid_configuration()
    {
        // The Unknown sentinel is the "not-yet-configured" marker — it can never be chosen (AC1).
        AgentState state = StateWith(ValidCreate());

        var command = new ConfigureAgentResponseMode(AgentResponseMode.Unknown);
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<InvalidAgentConfigurationRejection>();

        ApplyAll(state, result);
        state.ResponseMode.ShouldBe(AgentResponseMode.Unknown); // nothing recorded on a rejected configuration
    }

    [Fact]
    public void Reconfigure_same_mode_is_an_idempotent_noop()
    {
        AgentState state = StateWithResponseMode(ValidCreate(), AgentResponseMode.Automatic);

        var command = new ConfigureAgentResponseMode(AgentResponseMode.Automatic);
        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command));

        result.IsNoOp.ShouldBeTrue();
        result.Events.ShouldBeEmpty();
    }

    [Fact]
    public void Changing_the_mode_emits_a_new_event_and_bumps_version_without_rewriting_prior_events()
    {
        AgentState state = StateWith(ValidCreate());

        // First choice: Automatic.
        var first = new ConfigureAgentResponseMode(AgentResponseMode.Automatic);
        DomainResult firstResult = AgentAggregate.Handle(first, state, Envelope(first));
        AgentResponseModeConfigured firstEvent = firstResult.Events[0].ShouldBeOfType<AgentResponseModeConfigured>();
        ApplyAll(state, firstResult);
        int versionAfterFirst = state.ConfigurationVersion;

        // Change to Confirmation (future-only).
        var changed = new ConfigureAgentResponseMode(AgentResponseMode.Confirmation);
        DomainResult changedResult = AgentAggregate.Handle(changed, state, Envelope(changed));

        changedResult.IsSuccess.ShouldBeTrue();
        AgentResponseModeConfigured changedEvent = changedResult.Events[0].ShouldBeOfType<AgentResponseModeConfigured>();
        changedEvent.Mode.ShouldBe(AgentResponseMode.Confirmation);
        changedEvent.ConfigurationVersion.ShouldBe(versionAfterFirst + 1);

        // AC1: the prior append-only event is unchanged — a changed mode never rewrites it (future-only).
        firstEvent.Mode.ShouldBe(AgentResponseMode.Automatic);

        ApplyAll(state, changedResult);
        state.ResponseMode.ShouldBe(AgentResponseMode.Confirmation);
    }

    [Fact]
    public void Configure_on_a_missing_agent_is_rejected_as_not_found()
    {
        var command = new ConfigureAgentResponseMode(AgentResponseMode.Automatic);

        DomainResult result = AgentAggregate.Handle(command, state: null, Envelope(command));

        result.IsRejection.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentNotFoundRejection>();
    }

    [Fact]
    public void Configure_without_agents_admin_is_denied()
    {
        AgentState state = StateWith(ValidCreate());
        var command = new ConfigureAgentResponseMode(AgentResponseMode.Automatic);

        DomainResult result = AgentAggregate.Handle(command, state, Envelope(command, isAgentsAdmin: false, actorUserId: "intruder"));

        AgentAdministrationDeniedRejection denied = result.Events[0].ShouldBeOfType<AgentAdministrationDeniedRejection>();
        denied.CommandName.ShouldBe(nameof(ConfigureAgentResponseMode));
        result.Events.ShouldNotContain(e => e is AgentResponseModeConfigured);
    }

    [Fact]
    public void Apply_mode_change_tracks_the_single_recorded_mode_and_bumps_version()
    {
        AgentState state = StateWith(ValidCreate()); // ConfigurationVersion = 1

        state.Apply(new AgentResponseModeConfigured(AgentId, AgentResponseMode.Automatic, ConfigurationVersion: 2));
        state.ResponseMode.ShouldBe(AgentResponseMode.Automatic);
        state.ConfigurationVersion.ShouldBe(2);

        state.Apply(new AgentResponseModeConfigured(AgentId, AgentResponseMode.Confirmation, ConfigurationVersion: 3));
        state.ResponseMode.ShouldBe(AgentResponseMode.Confirmation);
        state.ConfigurationVersion.ShouldBe(3);
    }

    [Fact]
    public void Apply_mode_before_create_is_ignored()
    {
        var state = new AgentState();

        state.Apply(new AgentResponseModeConfigured(AgentId, AgentResponseMode.Automatic, ConfigurationVersion: 2));

        state.ResponseMode.ShouldBe(AgentResponseMode.Unknown);
        state.IsCreated.ShouldBeFalse();
    }

    [Fact]
    public async Task ProcessAndApply_configure_then_reconfigure_threads_state_through_the_pipeline()
    {
        var aggregate = new AgentAggregate();
        var state = new AgentState();

        (await ProcessAndApplyAsync(aggregate, state, ValidCreate())).IsSuccess.ShouldBeTrue();

        (await ProcessAndApplyAsync(aggregate, state, new ConfigureAgentResponseMode(AgentResponseMode.Confirmation))).IsSuccess.ShouldBeTrue();
        state.ResponseMode.ShouldBe(AgentResponseMode.Confirmation);

        // The chosen mode surfaces through the read path (AC1).
        AgentInspection.GetStatus(state, isAgentsAdmin: true).Agent.ShouldNotBeNull()
            .ResponseMode.ShouldBe(AgentResponseMode.Confirmation);

        // Re-asserting the same mode is an idempotent no-op.
        (await ProcessAndApplyAsync(aggregate, state, new ConfigureAgentResponseMode(AgentResponseMode.Confirmation))).IsNoOp.ShouldBeTrue();
    }
}
