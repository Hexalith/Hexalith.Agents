using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
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
}
