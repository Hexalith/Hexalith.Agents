using System.Text.Json;

using Hexalith.Agents.Agent;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Agent.Events;
using Hexalith.Agents.Contracts.Agent.Events.Rejections;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using static Hexalith.Agents.Tests.AgentTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Proves AD-4 configuration-version behavior for current and legacy Agent lifecycle events.
/// </summary>
public sealed class AgentLifecycleConfigurationVersionTests
{
    [Fact]
    public void SuccessfulLifecycleTransitionsIncrementConfigurationVersionExactlyOnce()
    {
        AgentState state = StateWithSelectedProvider(ValidCreate());
        int beforeActivation = state.ConfigurationVersion;

        DomainResult activation = AgentAggregate.Handle(
            new ActivateAgent(),
            state,
            SelectEnvelope(new ActivateAgent(), activationExpectedConfigurationVersion: state.ConfigurationVersion));

        AgentActivated activated = activation.Events.Single().ShouldBeOfType<AgentActivated>();
        activated.ConfigurationVersion.ShouldBe(beforeActivation + 1);
        ApplyAll(state, activation);
        state.ConfigurationVersion.ShouldBe(beforeActivation + 1);

        DomainResult disable = AgentAggregate.Handle(new DisableAgent(), state, Envelope(new DisableAgent()));

        AgentDisabled disabled = disable.Events.Single().ShouldBeOfType<AgentDisabled>();
        disabled.ConfigurationVersion.ShouldBe(beforeActivation + 2);
        ApplyAll(state, disable);
        state.ConfigurationVersion.ShouldBe(beforeActivation + 2);
    }

    [Fact]
    public void RejectedRepeatedLifecycleCommandsDoNotIncrementConfigurationVersion()
    {
        AgentState state = StateWithSelectedProvider(ValidCreate());
        DomainResult activation = AgentAggregate.Handle(
            new ActivateAgent(),
            state,
            SelectEnvelope(new ActivateAgent(), activationExpectedConfigurationVersion: state.ConfigurationVersion));
        ApplyAll(state, activation);
        int activeVersion = state.ConfigurationVersion;

        DomainResult repeatedActivation = AgentAggregate.Handle(
            new ActivateAgent(),
            state,
            SelectEnvelope(new ActivateAgent(), activationExpectedConfigurationVersion: state.ConfigurationVersion));

        _ = repeatedActivation.Events.Single().ShouldBeOfType<AgentLifecycleStateAlreadySetRejection>();
        ApplyAll(state, repeatedActivation);
        state.ConfigurationVersion.ShouldBe(activeVersion);

        DomainResult disable = AgentAggregate.Handle(new DisableAgent(), state, Envelope(new DisableAgent()));
        ApplyAll(state, disable);
        int disabledVersion = state.ConfigurationVersion;

        DomainResult repeatedDisable = AgentAggregate.Handle(new DisableAgent(), state, Envelope(new DisableAgent()));

        _ = repeatedDisable.Events.Single().ShouldBeOfType<AgentLifecycleStateAlreadySetRejection>();
        ApplyAll(state, repeatedDisable);
        state.ConfigurationVersion.ShouldBe(disabledVersion);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("+5")]
    [InlineData("05")]
    [InlineData(" 5")]
    public void Activation_requires_a_canonical_positive_expected_configuration_version(string? value)
    {
        AgentState state = StateWithSelectedProvider(ValidCreate());
        CommandEnvelope envelope = SelectEnvelope(
            new ActivateAgent(),
            activationExpectedConfigurationVersion: state.ConfigurationVersion);
        var extensions = new Dictionary<string, string>(envelope.Extensions!, StringComparer.Ordinal);
        if (value is null)
        {
            extensions.Remove(ActivationExpectedConfigurationVersionExtensionKey);
        }
        else
        {
            extensions[ActivationExpectedConfigurationVersionExtensionKey] = value;
        }

        DomainResult result = AgentAggregate.Handle(
            new ActivateAgent(),
            state,
            envelope with { Extensions = extensions });

        result.IsRejection.ShouldBeTrue();
        _ = result.Events.Single().ShouldBeOfType<InvalidAgentConfigurationRejection>();
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(1)]
    public void Activation_rejects_stale_and_future_configuration_versions_before_lifecycle_gates(int delta)
    {
        AgentState state = StateWithSelectedProvider(ValidCreate());
        int expected = state.ConfigurationVersion + delta;

        DomainResult result = AgentAggregate.Handle(
            new ActivateAgent(),
            state,
            SelectEnvelope(new ActivateAgent(), activationExpectedConfigurationVersion: expected));

        AgentActivationConfigurationVersionMismatchRejection rejection = result.Events
            .Single()
            .ShouldBeOfType<AgentActivationConfigurationVersionMismatchRejection>();
        rejection.ExpectedConfigurationVersion.ShouldBe(expected);
        rejection.ActualConfigurationVersion.ShouldBe(state.ConfigurationVersion);
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft);
    }

    [Fact]
    public void Never_admitted_replay_lane_attempt_is_rejected_by_the_version_fence_before_dependency_gates()
    {
        AgentState state = StateWithSelectedProvider(ValidCreate());
        int retainedVersion = state.ConfigurationVersion - 1;

        DomainResult result = AgentAggregate.Handle(
            new ActivateAgent(),
            state,
            ActivateEnvelope(
                providerValidation: ProviderSelectionValidationStatus.Unavailable,
                approverValidation: ApproverPolicyValidationStatus.Unavailable,
                expectedConfigurationVersion: retainedVersion));

        AgentActivationConfigurationVersionMismatchRejection rejection = result.Events
            .Single()
            .ShouldBeOfType<AgentActivationConfigurationVersionMismatchRejection>();
        rejection.ExpectedConfigurationVersion.ShouldBe(retainedVersion);
        rejection.ActualConfigurationVersion.ShouldBe(state.ConfigurationVersion);
        result.Events.ShouldNotContain(item => item is AgentActivated);
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft);
    }

    [Fact]
    public void LegacyLifecycleEventsReplayDeterministicallyWithoutRenumberingHistory()
    {
        AgentActivated activated = JsonSerializer.Deserialize<AgentActivated>("{\"AgentId\":\"hexa\"}")!;
        AgentDisabled disabled = JsonSerializer.Deserialize<AgentDisabled>("{\"AgentId\":\"hexa\"}")!;

        AgentState first = Replay(activated, disabled);
        AgentState second = Replay(activated, disabled);

        first.ShouldBeEquivalentTo(second);
        first.Lifecycle.ShouldBe(AgentLifecycleStatus.Disabled);
        first.ConfigurationVersion.ShouldBe(2);

        static AgentState Replay(AgentActivated activated, AgentDisabled disabled)
        {
            AgentState state = StateWith(ValidCreate());
            state.Apply(activated);
            state.Apply(new AgentResponseModeConfigured(
                AgentId,
                AgentResponseMode.Automatic,
                ConfigurationVersion: 2));
            state.Apply(disabled);
            return state;
        }
    }

    [Fact]
    public void NegativeLifecycleVersionsAreRejectedWithoutMutatingState()
    {
        AgentState state = StateWith(ValidCreate());

        Should.Throw<ArgumentOutOfRangeException>(() =>
            state.Apply(new AgentActivated(AgentId) { ConfigurationVersion = -1 }));
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft);
        state.ConfigurationVersion.ShouldBe(1);

        Should.Throw<ArgumentOutOfRangeException>(() =>
            state.Apply(new AgentDisabled(AgentId) { ConfigurationVersion = -1 }));
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft);
        state.ConfigurationVersion.ShouldBe(1);
    }

    [Fact]
    public void EvolvedLifecycleEventsReplayToTheSameVersionedState()
    {
        var activated = new AgentActivated(AgentId) { ConfigurationVersion = 6 };
        var disabled = new AgentDisabled(AgentId) { ConfigurationVersion = 7 };

        AgentState first = StateWithSelectedProvider(ValidCreate());
        AgentState second = StateWithSelectedProvider(ValidCreate());

        first.Apply(activated);
        first.Apply(disabled);
        second.Apply(activated);
        second.Apply(disabled);

        second.ShouldBeEquivalentTo(first);
        second.Lifecycle.ShouldBe(AgentLifecycleStatus.Disabled);
        second.ConfigurationVersion.ShouldBe(7);
    }
}
