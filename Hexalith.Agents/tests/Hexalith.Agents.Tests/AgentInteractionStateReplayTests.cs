using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

using Shouldly;

using static Hexalith.Agents.Tests.AgentInteractionTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Replay tests for <see cref="AgentInteractionState"/> (Story 2.1; replay determinism; AC4 sensitive-content
/// retention). Verify the <c>Apply</c> method records the request + snapshot deterministically, that the no-op
/// rejection <c>Apply</c> methods keep replay total so a persisted rejection never breaks rehydration, and that a
/// stream containing only rejections leaves the interaction un-requested.
/// </summary>
public sealed class AgentInteractionStateReplayTests
{
    [Fact]
    public void Apply_interaction_requested_records_the_request_and_snapshot()
    {
        var state = new AgentInteractionState();

        state.Apply(RequestedEvent(ValidRequest()));

        state.IsRequested.ShouldBeTrue();
        state.AgentInteractionId.ShouldBe(InteractionId);
        state.AgentId.ShouldBe(AgentId);
        state.CallerPartyId.ShouldBe(CallerPartyId);
        state.SourceConversationId.ShouldBe(SourceConversationId);
        state.Prompt.ShouldBe(Prompt);
        state.IdempotencyKey.ShouldBe(IdempotencyKey);
        state.Snapshot.ShouldBe(SampleSnapshot);
    }

    [Fact]
    public void Replay_is_deterministic_across_independent_rebuilds()
    {
        AgentInteractionState first = StateWith(ValidRequest());
        AgentInteractionState second = StateWith(ValidRequest());

        second.IsRequested.ShouldBe(first.IsRequested);
        second.AgentInteractionId.ShouldBe(first.AgentInteractionId);
        second.AgentId.ShouldBe(first.AgentId);
        second.Prompt.ShouldBe(first.Prompt);
        second.Snapshot.ShouldBe(first.Snapshot);
    }

    [Fact]
    public void Apply_rejection_events_are_replay_safe_noops()
    {
        AgentInteractionState state = StateWith(ValidRequest());

        // Replaying a stream that contains persisted rejection events must not throw or mutate the recorded request.
        state.Apply(new InvalidAgentInteractionRequestRejection(InteractionId, default));
        state.Apply(new AgentInteractionAlreadyRequestedRejection(InteractionId));

        state.IsRequested.ShouldBeTrue();
        state.Prompt.ShouldBe(Prompt);
        state.Snapshot.ShouldBe(SampleSnapshot);
    }

    [Fact]
    public void Rejection_only_stream_leaves_the_interaction_unrequested()
    {
        var state = new AgentInteractionState();

        // A stream whose only persisted event is a pre-request rejection rehydrates to "no interaction".
        state.Apply(new InvalidAgentInteractionRequestRejection(InteractionId, default));

        state.IsRequested.ShouldBeFalse();
        state.Prompt.ShouldBeEmpty();
        state.Snapshot.ShouldBeNull();
    }

    [Fact]
    public void Replay_of_a_rejection_then_a_successful_request_rebuilds_the_requested_state()
    {
        // Real scenario for one deterministic id: a first attempt while the Agent was pre-activation persisted a
        // MissingAgentSnapshot rejection; after activation the same call was re-issued and succeeded. Replaying the
        // mixed stream in order must total to a fully-requested interaction (the no-op rejection Apply must not block
        // the later success).
        var state = new AgentInteractionState();

        state.Apply(new InvalidAgentInteractionRequestRejection(InteractionId, AgentInteractionRequestValidationStatus.MissingAgentSnapshot));
        state.Apply(RequestedEvent(ValidRequest()));

        state.IsRequested.ShouldBeTrue();
        state.Prompt.ShouldBe(Prompt);
        state.Snapshot.ShouldBe(SampleSnapshot);
        state.AgentInteractionId.ShouldBe(InteractionId);
    }

    // ===== Story 2.2 gate replay (status transitions; AC1, AC4) =====

    [Fact]
    public void Apply_authorized_transitions_status_to_authorized()
    {
        AgentInteractionState state = StateRequested();

        state.Apply(new AgentInteractionAuthorized(InteractionId));

        state.Status.ShouldBe(AgentInteractionStatus.Authorized);
        state.GateVerdicts.ShouldBeNull();
        state.Prompt.ShouldBe(Prompt); // the request payload is untouched by the gate (AD-14)
    }

    [Fact]
    public void Apply_gate_failed_records_the_decision_and_safe_blocker_evidence()
    {
        AgentInteractionState state = StateRequested();
        var blockers = new[] { Verdict(AgentInteractionGateCheck.TenantAccess, AgentInteractionGateOutcome.Unauthorized) };

        state.Apply(new AgentInteractionGateFailed(InteractionId, AgentInteractionStatus.Denied, blockers));

        state.Status.ShouldBe(AgentInteractionStatus.Denied);
        state.GateVerdicts.ShouldNotBeNull().ShouldHaveSingleItem().Check.ShouldBe(AgentInteractionGateCheck.TenantAccess);
    }

    [Fact]
    public void Apply_gate_not_evaluable_rejection_is_a_replay_safe_noop()
    {
        AgentInteractionState state = StateRequested();

        // A persisted not-evaluable rejection must not throw or mutate the recorded request/status.
        state.Apply(new AgentInteractionGateNotEvaluableRejection(InteractionId, AgentInteractionGateNotEvaluableReason.NoVerdictsProvided));

        state.Status.ShouldBe(AgentInteractionStatus.Requested);
        state.IsRequested.ShouldBeTrue();
        state.GateVerdicts.ShouldBeNull();
    }

    [Fact]
    public void Gate_outcome_applies_only_over_a_requested_stream()
    {
        // A gate outcome event ahead of the request (a malformed stream) must not flip status — every non-request Apply
        // keeps the IsRequested guard so replay over a stream that begins before the request stays total.
        var state = new AgentInteractionState();

        state.Apply(new AgentInteractionAuthorized(InteractionId));
        state.Apply(new AgentInteractionGateFailed(InteractionId, AgentInteractionStatus.Blocked, [Verdict(AgentInteractionGateCheck.AgentLifecycle, AgentInteractionGateOutcome.Missing)]));

        state.IsRequested.ShouldBeFalse();
        state.Status.ShouldBe(AgentInteractionStatus.Unknown);
        state.GateVerdicts.ShouldBeNull();
    }

    [Fact]
    public void Replay_over_request_then_gate_failed_is_deterministic_across_rebuilds()
    {
        var blockers = new[] { Verdict(AgentInteractionGateCheck.ProviderModelReadiness, AgentInteractionGateOutcome.Disabled) };

        AgentInteractionState first = Rebuild(blockers);
        AgentInteractionState second = Rebuild(blockers);

        second.Status.ShouldBe(first.Status);
        second.Status.ShouldBe(AgentInteractionStatus.Blocked);
        second.GateVerdicts.ShouldNotBeNull().Count.ShouldBe(first.GateVerdicts!.Count);
        second.AgentInteractionId.ShouldBe(first.AgentInteractionId);

        static AgentInteractionState Rebuild(AgentInvocationGateVerdict[] blockers)
        {
            AgentInteractionState state = StateRequested();
            state.Apply(new AgentInteractionGateFailed(InteractionId, AgentInteractionStatus.Blocked, blockers));
            return state;
        }
    }
}
