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
/// Activation-gate tests for the Story 1.7 content-safety readiness gate (AC2, AC4). Covers: activation blocked by
/// <c>MissingContentSafetyPolicy</c> when everything else is ready but no policy is configured (Automatic and
/// Confirmation); a fully-configured Automatic and a fully-configured Confirmation agent activating once a
/// content-safety policy is present; the gate being a <em>pure state check</em> that no envelope verdict can bypass
/// (it cannot be cleared by a direct-gateway activation that forges every other verdict); and the
/// <c>MissingContentSafetyPolicy</c> blocker being appended <em>last</em> in the documented deterministic order.
/// </summary>
public sealed class AgentContentSafetyActivationTests
{
    // Party + provider + Automatic mode, but NO content-safety policy (so the content-safety gate is the one in play).
    private static AgentState AutomaticReadyNoContentSafety()
    {
        AgentState state = StateWithLinkedParty(ValidCreate());
        state.Apply(new AgentProviderModelSelected(AgentId, SelectedProviderId, SelectedModelId, SelectedCapabilityVersion, state.ConfigurationVersion + 1));
        state.Apply(new AgentResponseModeConfigured(AgentId, AgentResponseMode.Automatic, state.ConfigurationVersion + 1));
        return state;
    }

    // Party + provider + Confirmation mode + approver policy, but NO content-safety policy.
    private static AgentState ConfirmationReadyNoContentSafety()
    {
        AgentState state = StateWithLinkedParty(ValidCreate());
        state.Apply(new AgentProviderModelSelected(AgentId, SelectedProviderId, SelectedModelId, SelectedCapabilityVersion, state.ConfigurationVersion + 1));
        state.Apply(new AgentResponseModeConfigured(AgentId, AgentResponseMode.Confirmation, state.ConfigurationVersion + 1));
        state.Apply(new AgentApproverPolicyConfigured(AgentId, SampleApproverPolicy, 1, state.ConfigurationVersion + 1));
        return state;
    }

    [Fact]
    public void Automatic_agent_otherwise_ready_is_blocked_by_missing_content_safety_policy()
    {
        AgentState state = AutomaticReadyNoContentSafety();

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, ActivateEnvelope());

        result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>()
            .Blockers.ShouldBe([AgentActivationBlocker.MissingContentSafetyPolicy]);
        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft);
    }

    [Fact]
    public void Confirmation_agent_otherwise_ready_is_blocked_by_missing_content_safety_policy()
    {
        AgentState state = ConfirmationReadyNoContentSafety();

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, ActivateEnvelope());

        result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>()
            .Blockers.ShouldBe([AgentActivationBlocker.MissingContentSafetyPolicy]);
    }

    [Fact]
    public void Fully_configured_automatic_agent_with_content_safety_is_activatable()
    {
        // StateWithSelectedProvider now also records a content-safety policy (1.7), so the only remaining gate is the
        // live provider verdict supplied on the envelope.
        AgentState state = StateWithSelectedProvider(ValidCreate());
        state.ContentSafety.ShouldNotBeNull();

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, ActivateEnvelope());

        result.IsSuccess.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentActivated>();
    }

    [Fact]
    public void Fully_configured_confirmation_agent_with_content_safety_is_activatable()
    {
        AgentState state = StateConfirmationReady(ValidCreate());
        state.ContentSafety.ShouldNotBeNull();

        DomainResult result = AgentAggregate.Handle(
            new ActivateAgent(),
            state,
            ActivateEnvelope(providerValidation: ProviderSelectionValidationStatus.Valid, approverValidation: ApproverPolicyValidationStatus.Valid));

        result.IsSuccess.ShouldBeTrue();
        _ = result.Events[0].ShouldBeOfType<AgentActivated>();
    }

    [Fact]
    public void Content_safety_gate_is_a_pure_state_check_that_no_envelope_verdict_can_bypass()
    {
        // Everything else is ready and the envelope carries every other verdict as Valid — yet because content safety
        // has NO verdict to forge, the gate reads straight from state and still blocks. A direct-gateway activation
        // cannot bypass it (AC2, AC4).
        AgentState state = AutomaticReadyNoContentSafety();

        DomainResult result = AgentAggregate.Handle(
            new ActivateAgent(),
            state,
            ActivateEnvelope(providerValidation: ProviderSelectionValidationStatus.Valid, approverValidation: ApproverPolicyValidationStatus.Valid));

        result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>()
            .Blockers.ShouldBe([AgentActivationBlocker.MissingContentSafetyPolicy]);
    }

    [Fact]
    public void Content_safety_blocker_is_appended_last_in_the_documented_order()
    {
        // A fresh draft yields the full gate order with the 1.7 content-safety gate appended last, never reordered.
        AgentState state = StateWith(ValidCreate());

        DomainResult result = AgentAggregate.Handle(new ActivateAgent(), state, ActivateEnvelope());

        result.Events[0].ShouldBeOfType<AgentActivationBlockedRejection>()
            .Blockers.ShouldBe([
                AgentActivationBlocker.MissingPartyIdentity,
                AgentActivationBlocker.MissingProviderSelection,
                AgentActivationBlocker.MissingResponseMode,
                AgentActivationBlocker.MissingContentSafetyPolicy,
            ]);
    }

    [Fact]
    public void Configuring_content_safety_clears_the_gate_but_does_not_auto_activate()
    {
        // Story 1.3 invariant: a configured content-safety policy clears MissingContentSafetyPolicy but does not by
        // itself make a Draft agent Active.
        AgentState state = AutomaticReadyNoContentSafety();

        ApplyAll(state, AgentAggregate.Handle(
            new ConfigureAgentContentSafetyPolicy(SampleContentSafetyConfiguration),
            state,
            Envelope(new ConfigureAgentContentSafetyPolicy(SampleContentSafetyConfiguration))));

        state.Lifecycle.ShouldBe(AgentLifecycleStatus.Draft); // still a draft — configuration is not activation
        AgentInspection.GetStatus(state, isAgentsAdmin: true).Agent.ShouldNotBeNull()
            .ActivationBlockers.ShouldBeEmpty(); // the readiness set is now complete (AC4 capstone)
    }
}
