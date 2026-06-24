using System.Linq;
using System.Threading.Tasks;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using static Hexalith.Agents.Tests.AgentInteractionTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Handle-method tests for the Story 2.5 automatic posting on <see cref="AgentInteractionAggregate"/> (AC1–AC4; FR-11,
/// FR-12; AD-3, AD-6, AD-7, AD-13). Cover the posted path (records the safe evidence), every failure outcome →
/// PostingFailed with the mapped reason, the not-postable rejections (not-requested / not-generated / not-automatic),
/// terminal idempotency for both terminal statuses (no decision flip, no duplicate message), the Evaluate/Decide no-drift
/// theory, and the full reflection-dispatch + JSON round-trip through <c>ProcessAsync</c>.
/// </summary>
public sealed class AgentInteractionPostingAggregateTests
{
    // ===== Success: posted (AC1, AC2) =====

    [Fact]
    public void Posted_outcome_records_posted_with_the_safe_evidence()
    {
        AgentInteractionState state = StateGenerated();

        DomainResult result = Post(PostedResult(), state);

        result.IsSuccess.ShouldBeTrue();
        AgentResponsePosted posted = result.Events[0].ShouldBeOfType<AgentResponsePosted>();
        posted.AgentInteractionId.ShouldBe(InteractionId);
        posted.Evidence.MessageId.ShouldBe(PostedMessageId);
        posted.Evidence.SourceConversationId.ShouldBe(SourceConversationId);
        posted.Evidence.AgentPartyId.ShouldBe(AgentPartyId);
        posted.Evidence.PostedVersionId.ShouldBe(PostedVersionId);

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.Posted);
        state.PostingEvidence.ShouldNotBeNull().MessageId.ShouldBe(PostedMessageId);
        state.PostingFailureReason.ShouldBeNull();
    }

    // ===== Every failure outcome → PostingFailed with the mapped reason (AC4) =====

    [Theory]
    [InlineData(AgentResponsePostingOutcome.PartyIdentityUnavailable, AgentResponsePostingFailureReason.PartyIdentityUnavailable)]
    [InlineData(AgentResponsePostingOutcome.MembershipUnavailable, AgentResponsePostingFailureReason.MembershipUnavailable)]
    [InlineData(AgentResponsePostingOutcome.MembershipRejected, AgentResponsePostingFailureReason.MembershipRejected)]
    [InlineData(AgentResponsePostingOutcome.ConversationUnavailable, AgentResponsePostingFailureReason.ConversationUnavailable)]
    [InlineData(AgentResponsePostingOutcome.PostRejected, AgentResponsePostingFailureReason.PostRejected)]
    [InlineData(AgentResponsePostingOutcome.AdapterFailure, AgentResponsePostingFailureReason.AdapterFailure)]
    [InlineData(AgentResponsePostingOutcome.Unknown, AgentResponsePostingFailureReason.AdapterFailure)] // an unmapped/garbage outcome fails closed to the generic reason (AD-12)
    public void Each_failure_outcome_records_posting_failed_with_the_mapped_reason(
        AgentResponsePostingOutcome outcome,
        AgentResponsePostingFailureReason expected)
    {
        AgentInteractionState state = StateGenerated();

        DomainResult result = Post(PostingResult(outcome), state);

        AgentResponsePostingFailed failed = result.Events[0].ShouldBeOfType<AgentResponsePostingFailed>();
        failed.Reason.ShouldBe(expected);
        failed.AgentInteractionId.ShouldBe(InteractionId);
        result.Events.OfType<AgentResponsePosted>().ShouldBeEmpty();

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.PostingFailed);
        state.PostingFailureReason.ShouldBe(expected);
        state.PostingEvidence.ShouldNotBeNull().AgentPartyId.ShouldBe(AgentPartyId); // the attempted safe ids are recorded
    }

    // ===== Not postable: not requested / not generated / not automatic (AD-12) =====

    [Fact]
    public void Posting_on_a_never_requested_interaction_is_not_postable_interaction_not_requested()
    {
        DomainResult result = Post(PostedResult(), state: null);

        AssertNotPostable(result, AgentResponseNotPostableReason.InteractionNotRequested);
    }

    [Fact]
    public void Posting_on_a_rejection_only_stream_is_not_postable_interaction_not_requested()
    {
        DomainResult result = Post(PostedResult(), new AgentInteractionState());

        AssertNotPostable(result, AgentResponseNotPostableReason.InteractionNotRequested);
    }

    [Fact]
    public void Posting_a_confirmation_mode_interaction_is_not_postable_not_automatic_response_mode()
    {
        // Generated + Confirmation mode posts via Epic 3 approval (Story 3.5), not automatic posting — a structural
        // rejection (no state change), checked before the status precondition.
        DomainResult result = Post(PostedResult(), StateGeneratedConfirmationMode());

        AssertNotPostable(result, AgentResponseNotPostableReason.NotAutomaticResponseMode);
    }

    [Theory]
    [MemberData(nameof(NonGeneratedAutomaticStates))]
    public void Posting_before_generation_is_not_postable_output_not_generated(AgentInteractionState state)
    {
        // Any Automatic-mode status that is not Generated (Requested/Authorized/ContextReady/ContextBlocked/
        // GenerationFailed/SafetyFailed) is a structural rejection (no state change), distinct from a recorded
        // posting-failed decision, which only ever follows Generated (AD-12).
        DomainResult result = Post(PostedResult(), state);

        AssertNotPostable(result, AgentResponseNotPostableReason.OutputNotGenerated);
    }

    public static TheoryData<AgentInteractionState> NonGeneratedAutomaticStates() =>
    [
        StateRequested(),
        StateAuthorized(),
        StateContextReady(),
        StateContextBlocked(),
        GenerationFailedState(),
        SafetyFailedState(),
    ];

    // ===== Idempotent terminal determinism (AD-13, AC3) — no decision flip, no duplicate message =====

    [Fact]
    public void Re_post_after_posted_is_a_noop_and_preserves_the_recorded_outcome()
    {
        AgentInteractionState state = StateGenerated();
        ApplyAll(state, Post(PostedResult(), state)); // now Posted
        state.Status.ShouldBe(AgentInteractionStatus.Posted);

        // Re-dispatching a post command that WOULD fail must be a clean no-op — no flip, no second message (AC3).
        DomainResult reissue = Post(PostRejectedResult(), state);

        reissue.IsNoOp.ShouldBeTrue();
        reissue.Events.ShouldBeEmpty();
        state.Status.ShouldBe(AgentInteractionStatus.Posted);
        state.PostingFailureReason.ShouldBeNull();
    }

    [Fact]
    public void Re_post_after_posting_failed_is_a_noop_and_never_flips()
    {
        AgentInteractionState state = StateGenerated();
        ApplyAll(state, Post(MembershipUnavailableResult(), state)); // now PostingFailed
        state.Status.ShouldBe(AgentInteractionStatus.PostingFailed);

        DomainResult reissue = Post(PostedResult(), state);

        reissue.IsNoOp.ShouldBeTrue();
        state.Status.ShouldBe(AgentInteractionStatus.PostingFailed);
        state.PostingFailureReason.ShouldBe(AgentResponsePostingFailureReason.MembershipUnavailable);
    }

    // ===== Decide / Evaluate no-drift =====

    [Theory]
    [InlineData(AgentResponsePostingOutcome.Posted, AgentInteractionStatus.Posted)]
    [InlineData(AgentResponsePostingOutcome.PartyIdentityUnavailable, AgentInteractionStatus.PostingFailed)]
    [InlineData(AgentResponsePostingOutcome.MembershipUnavailable, AgentInteractionStatus.PostingFailed)]
    [InlineData(AgentResponsePostingOutcome.MembershipRejected, AgentInteractionStatus.PostingFailed)]
    [InlineData(AgentResponsePostingOutcome.ConversationUnavailable, AgentInteractionStatus.PostingFailed)]
    [InlineData(AgentResponsePostingOutcome.PostRejected, AgentInteractionStatus.PostingFailed)]
    [InlineData(AgentResponsePostingOutcome.AdapterFailure, AgentInteractionStatus.PostingFailed)]
    [InlineData(AgentResponsePostingOutcome.Unknown, AgentInteractionStatus.PostingFailed)] // an unmapped/garbage outcome fails closed (never Posted) — Decide/Evaluate still agree
    public void Decide_matches_the_aggregate_recorded_decision_for_each_outcome(AgentResponsePostingOutcome outcome, AgentInteractionStatus expected)
    {
        AgentResponsePostingResult result = PostingResult(outcome);

        AgentResponsePostingPolicy.Decide(result).ShouldBe(expected);

        DomainResult domainResult = Post(result, StateGenerated());
        AgentInteractionStatus recorded = domainResult.Events[0] switch
        {
            AgentResponsePosted => AgentInteractionStatus.Posted,
            AgentResponsePostingFailed => AgentInteractionStatus.PostingFailed,
            _ => AgentInteractionStatus.Unknown,
        };
        recorded.ShouldBe(expected);
    }

    // ===== Full pipeline: reflection dispatch + JSON round-trip =====

    [Fact]
    public async Task Process_async_round_trips_the_post_command_and_records_posted()
    {
        var aggregate = new AgentInteractionAggregate();
        AgentInteractionState state = StateGenerated();

        DomainResult result = await ProcessAndApplyAsync(aggregate, state, PostCommand(PostedResult()));

        result.IsSuccess.ShouldBeTrue();
        AgentResponsePosted posted = result.Events[0].ShouldBeOfType<AgentResponsePosted>();
        posted.Evidence.MessageId.ShouldBe(PostedMessageId); // survived the JSON round-trip
        state.Status.ShouldBe(AgentInteractionStatus.Posted);
    }

    [Fact]
    public async Task Process_async_round_trips_a_failing_post_command_and_records_the_failure()
    {
        var aggregate = new AgentInteractionAggregate();
        AgentInteractionState state = StateGenerated();

        DomainResult result = await ProcessAndApplyAsync(aggregate, state, PostCommand(MembershipUnavailableResult()));

        AgentResponsePostingFailed failed = result.Events[0].ShouldBeOfType<AgentResponsePostingFailed>();
        failed.Reason.ShouldBe(AgentResponsePostingFailureReason.MembershipUnavailable); // survived the round-trip by name
        state.Status.ShouldBe(AgentInteractionStatus.PostingFailed);
    }

    // ===== Helpers =====

    private static DomainResult Post(AgentResponsePostingResult result, AgentInteractionState? state)
    {
        PostAgentResponse command = PostCommand(result);
        return AgentInteractionAggregate.Handle(command, state, Envelope(command));
    }

    private static AgentInteractionState GenerationFailedState()
    {
        AgentInteractionState state = StateContextReady();
        state.Apply(new AgentOutputGenerationFailed(
            InteractionId,
            AgentInteractionStatus.GenerationFailed,
            AgentOutputGenerationFailureReason.ProviderTimeout,
            new AgentGenerationAttemptEvidence(GenerationAttemptId, SampleSnapshot.ProviderId, SampleSnapshot.ModelId, SampleSnapshot.ProviderCapabilityVersion, PromptTokenCount, OutputTokenCount)));
        return state;
    }

    private static AgentInteractionState SafetyFailedState()
    {
        AgentInteractionState state = StateContextReady();
        state.Apply(new AgentOutputGenerationFailed(
            InteractionId,
            AgentInteractionStatus.SafetyFailed,
            AgentOutputGenerationFailureReason.ContentSafetyBlocked,
            new AgentGenerationAttemptEvidence(GenerationAttemptId, SampleSnapshot.ProviderId, SampleSnapshot.ModelId, SampleSnapshot.ProviderCapabilityVersion, PromptTokenCount, OutputTokenCount)));
        return state;
    }

    private static void AssertNotPostable(DomainResult result, AgentResponseNotPostableReason expected)
    {
        result.IsRejection.ShouldBeTrue();
        AgentResponseNotPostableRejection rejection = result.Events[0].ShouldBeOfType<AgentResponseNotPostableRejection>();
        rejection.Reason.ShouldBe(expected);
        rejection.AgentInteractionId.ShouldBe(InteractionId);
        result.Events.OfType<AgentResponsePosted>().ShouldBeEmpty();
        result.Events.OfType<AgentResponsePostingFailed>().ShouldBeEmpty();
    }
}
