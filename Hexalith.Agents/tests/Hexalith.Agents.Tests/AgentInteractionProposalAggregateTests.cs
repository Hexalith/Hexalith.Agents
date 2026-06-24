using System.Linq;
using System.Text.Json;
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
/// Handle-method tests for the Story 3.1 Confirmation-mode proposal creation on <see cref="AgentInteractionAggregate"/>
/// (AC1–AC4; FR-13, FR-14, FR-27; AD-3, AD-5, AD-6, AD-13). Cover the created path (records the safe evidence + Pending
/// proposal state), every failure outcome → ProposalCreationFailed with the mapped reason, the not-creatable rejections
/// (not-requested / not-confirmation / not-generated — incl. SafetyFailed/GenerationFailed for AC3), terminal idempotency
/// for both terminal statuses (no decision flip, no duplicate proposal), the Evaluate/Decide no-drift theory, and the full
/// reflection-dispatch + JSON round-trip through <c>ProcessAsync</c>.
/// </summary>
public sealed class AgentInteractionProposalAggregateTests
{
    // ===== Success: created (AC1, AC2) =====

    [Fact]
    public void Created_outcome_records_proposal_created_with_the_safe_evidence()
    {
        AgentInteractionState state = StateGeneratedConfirmationMode();

        DomainResult result = Create(CreatedProposalResult(), state);

        result.IsSuccess.ShouldBeTrue();
        ProposedAgentReplyCreated created = result.Events[0].ShouldBeOfType<ProposedAgentReplyCreated>();
        created.AgentInteractionId.ShouldBe(InteractionId);
        created.Evidence.ProposalId.ShouldBe(SampleProposalId);
        created.Evidence.SourceConversationId.ShouldBe(SourceConversationId);
        created.Evidence.ProposedVersionId.ShouldBe(PostedVersionId);
        created.Evidence.ApproverPolicyVersion.ShouldBe(ConfirmationSnapshot.ApproverPolicyVersion);
        created.Evidence.ContentSafetyPolicyVersion.ShouldBe(ConfirmationSnapshot.ContentSafetyPolicyVersion);
        created.Evidence.ExpiresAt.ShouldBeNull();

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.ProposalCreated);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Pending);
        state.ProposalEvidence.ShouldNotBeNull().ProposalId.ShouldBe(SampleProposalId);
        state.ProposalCreationFailureReason.ShouldBeNull();

        // The proposal surface is structurally content-free — no generated content rides anywhere (AD-14).
        JsonSerializer.Serialize(created).ShouldNotContain(GeneratedContentText);
    }

    [Fact]
    public void Created_with_a_configured_expiry_records_the_expiry_metadata()
    {
        AgentInteractionState state = StateGeneratedConfirmationMode();

        DomainResult result = Create(ProposalResult(expiresAt: SampleExpiresAt), state);

        ProposedAgentReplyCreated created = result.Events[0].ShouldBeOfType<ProposedAgentReplyCreated>();
        created.Evidence.ExpiresAt.ShouldBe(SampleExpiresAt);
    }

    // ===== Every failure outcome → ProposalCreationFailed with the mapped reason (AC3, AC4) =====

    [Theory]
    [InlineData(AgentProposalCreationOutcome.GeneratedVersionUnavailable, AgentProposalCreationFailureReason.GeneratedVersionUnavailable)]
    [InlineData(AgentProposalCreationOutcome.AdapterFailure, AgentProposalCreationFailureReason.AdapterFailure)]
    [InlineData(AgentProposalCreationOutcome.Unknown, AgentProposalCreationFailureReason.AdapterFailure)] // an unmapped/garbage outcome fails closed to the generic reason (AD-12)
    public void Each_failure_outcome_records_proposal_creation_failed_with_the_mapped_reason(
        AgentProposalCreationOutcome outcome,
        AgentProposalCreationFailureReason expected)
    {
        AgentInteractionState state = StateGeneratedConfirmationMode();

        DomainResult result = Create(ProposalResult(outcome), state);

        ProposedAgentReplyCreationFailed failed = result.Events[0].ShouldBeOfType<ProposedAgentReplyCreationFailed>();
        failed.Reason.ShouldBe(expected);
        failed.AgentInteractionId.ShouldBe(InteractionId);
        result.Events.OfType<ProposedAgentReplyCreated>().ShouldBeEmpty();

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.ProposalCreationFailed);
        state.ProposalCreationFailureReason.ShouldBe(expected);
        state.ProposalState.ShouldBeNull(); // no proposal exists on a failure
        state.ProposalEvidence.ShouldNotBeNull(); // the attempted safe ids are recorded
    }

    // ===== Not creatable: not requested / not confirmation / not generated (AD-12) =====

    [Fact]
    public void Creating_on_a_never_requested_interaction_is_not_creatable_interaction_not_requested()
    {
        DomainResult result = Create(CreatedProposalResult(), state: null);

        AssertNotCreatable(result, AgentProposedReplyNotCreatableReason.InteractionNotRequested);
    }

    [Fact]
    public void Creating_on_a_rejection_only_stream_is_not_creatable_interaction_not_requested()
    {
        DomainResult result = Create(CreatedProposalResult(), new AgentInteractionState());

        AssertNotCreatable(result, AgentProposedReplyNotCreatableReason.InteractionNotRequested);
    }

    [Fact]
    public void Creating_for_an_automatic_mode_interaction_is_not_creatable_not_confirmation_response_mode()
    {
        // Generated + Automatic mode posts via Story 2.5, not proposal creation — a structural rejection (no state change),
        // checked before the status precondition.
        DomainResult result = Create(CreatedProposalResult(), StateGenerated());

        AssertNotCreatable(result, AgentProposedReplyNotCreatableReason.NotConfirmationResponseMode);
    }

    [Theory]
    [MemberData(nameof(NonGeneratedConfirmationStates))]
    public void Creating_before_generation_is_not_creatable_output_not_generated(AgentInteractionState state)
    {
        // Any Confirmation-mode status that is not Generated (Requested/Authorized/ContextReady/ContextBlocked/
        // GenerationFailed/SafetyFailed) is a structural rejection (no state change). For SafetyFailed/GenerationFailed this
        // is the structural enforcement of AC3 — a failed/unsafe generation can never reach an approvable proposal.
        DomainResult result = Create(CreatedProposalResult(), state);

        AssertNotCreatable(result, AgentProposedReplyNotCreatableReason.OutputNotGenerated);
    }

    public static TheoryData<AgentInteractionState> NonGeneratedConfirmationStates() =>
    [
        ConfirmationStateRequested(),
        ConfirmationStateAuthorized(),
        ConfirmationStateContextReady(),
        ConfirmationStateContextBlocked(),
        ConfirmationGenerationFailedState(),
        ConfirmationSafetyFailedState(),
    ];

    // ===== Idempotent terminal determinism (AD-13, AC4) — no decision flip, no duplicate proposal =====

    [Fact]
    public void Re_create_after_proposal_created_is_a_noop_and_preserves_the_recorded_outcome()
    {
        AgentInteractionState state = StateGeneratedConfirmationMode();
        ApplyAll(state, Create(CreatedProposalResult(), state)); // now ProposalCreated
        state.Status.ShouldBe(AgentInteractionStatus.ProposalCreated);

        // Re-dispatching a create command that WOULD fail must be a clean no-op — no flip, no second proposal (AC4).
        DomainResult reissue = Create(AdapterFailureProposalResult(), state);

        reissue.IsNoOp.ShouldBeTrue();
        reissue.Events.ShouldBeEmpty();
        state.Status.ShouldBe(AgentInteractionStatus.ProposalCreated);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Pending);
        state.ProposalCreationFailureReason.ShouldBeNull();
    }

    [Fact]
    public void Re_create_after_proposal_creation_failed_is_a_noop_and_never_flips()
    {
        AgentInteractionState state = StateGeneratedConfirmationMode();
        ApplyAll(state, Create(AdapterFailureProposalResult(), state)); // now ProposalCreationFailed
        state.Status.ShouldBe(AgentInteractionStatus.ProposalCreationFailed);

        DomainResult reissue = Create(CreatedProposalResult(), state);

        reissue.IsNoOp.ShouldBeTrue();
        state.Status.ShouldBe(AgentInteractionStatus.ProposalCreationFailed);
        state.ProposalCreationFailureReason.ShouldBe(AgentProposalCreationFailureReason.AdapterFailure);
        state.ProposalState.ShouldBeNull();
    }

    // ===== Decide / Evaluate no-drift =====

    [Theory]
    [InlineData(AgentProposalCreationOutcome.Created, AgentInteractionStatus.ProposalCreated)]
    [InlineData(AgentProposalCreationOutcome.GeneratedVersionUnavailable, AgentInteractionStatus.ProposalCreationFailed)]
    [InlineData(AgentProposalCreationOutcome.AdapterFailure, AgentInteractionStatus.ProposalCreationFailed)]
    [InlineData(AgentProposalCreationOutcome.Unknown, AgentInteractionStatus.ProposalCreationFailed)] // an unmapped/garbage outcome fails closed (never Created) — Decide/Evaluate still agree
    public void Decide_matches_the_aggregate_recorded_decision_for_each_outcome(AgentProposalCreationOutcome outcome, AgentInteractionStatus expected)
    {
        AgentProposalCreationResult result = ProposalResult(outcome);

        AgentProposalCreationPolicy.Decide(result).ShouldBe(expected);

        DomainResult domainResult = Create(result, StateGeneratedConfirmationMode());
        AgentInteractionStatus recorded = domainResult.Events[0] switch
        {
            ProposedAgentReplyCreated => AgentInteractionStatus.ProposalCreated,
            ProposedAgentReplyCreationFailed => AgentInteractionStatus.ProposalCreationFailed,
            _ => AgentInteractionStatus.Unknown,
        };
        recorded.ShouldBe(expected);
    }

    // ===== Full pipeline: reflection dispatch + JSON round-trip =====

    [Fact]
    public async Task Process_async_round_trips_the_create_command_and_records_proposal_created()
    {
        var aggregate = new AgentInteractionAggregate();
        AgentInteractionState state = StateGeneratedConfirmationMode();

        DomainResult result = await ProcessAndApplyAsync(aggregate, state, ProposalCommand(CreatedProposalResult()));

        result.IsSuccess.ShouldBeTrue();
        ProposedAgentReplyCreated created = result.Events[0].ShouldBeOfType<ProposedAgentReplyCreated>();
        created.Evidence.ProposalId.ShouldBe(SampleProposalId); // survived the JSON round-trip
        state.Status.ShouldBe(AgentInteractionStatus.ProposalCreated);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Pending);
    }

    [Fact]
    public async Task Process_async_round_trips_a_failing_create_command_and_records_the_failure()
    {
        var aggregate = new AgentInteractionAggregate();
        AgentInteractionState state = StateGeneratedConfirmationMode();

        DomainResult result = await ProcessAndApplyAsync(aggregate, state, ProposalCommand(GeneratedVersionUnavailableProposalResult()));

        ProposedAgentReplyCreationFailed failed = result.Events[0].ShouldBeOfType<ProposedAgentReplyCreationFailed>();
        failed.Reason.ShouldBe(AgentProposalCreationFailureReason.GeneratedVersionUnavailable); // survived the round-trip by name
        state.Status.ShouldBe(AgentInteractionStatus.ProposalCreationFailed);
    }

    // ===== Helpers =====

    private static DomainResult Create(AgentProposalCreationResult result, AgentInteractionState? state)
    {
        CreateProposedAgentReply command = ProposalCommand(result);
        return AgentInteractionAggregate.Handle(command, state, Envelope(command));
    }

    private static AgentInteractionState ConfirmationStateRequested()
    {
        var state = new AgentInteractionState();
        state.Apply(new InteractionRequested(
            InteractionId, AgentId, CallerPartyId, SourceConversationId, ConfirmationSnapshot, Prompt, IdempotencyKey));
        return state;
    }

    private static AgentInteractionState ConfirmationStateAuthorized()
    {
        AgentInteractionState state = ConfirmationStateRequested();
        state.Apply(new AgentInteractionAuthorized(InteractionId));
        return state;
    }

    private static AgentInteractionState ConfirmationStateContextReady()
    {
        AgentInteractionState state = ConfirmationStateAuthorized();
        state.Apply(new AgentInteractionContextReady(InteractionId, ConfirmationContextEvidence()));
        return state;
    }

    private static AgentInteractionState ConfirmationStateContextBlocked()
    {
        AgentInteractionState state = ConfirmationStateAuthorized();
        state.Apply(new AgentInteractionContextBlocked(
            InteractionId, AgentInteractionContextBlockReason.ExceedsModelBudget, ConfirmationContextEvidence()));
        return state;
    }

    private static AgentInteractionState ConfirmationGenerationFailedState()
        => ConfirmationGenerationFailed(AgentInteractionStatus.GenerationFailed, AgentOutputGenerationFailureReason.ProviderTimeout);

    private static AgentInteractionState ConfirmationSafetyFailedState()
        => ConfirmationGenerationFailed(AgentInteractionStatus.SafetyFailed, AgentOutputGenerationFailureReason.ContentSafetyBlocked);

    private static AgentInteractionState ConfirmationGenerationFailed(AgentInteractionStatus decision, AgentOutputGenerationFailureReason reason)
    {
        AgentInteractionState state = ConfirmationStateContextReady();
        state.Apply(new AgentOutputGenerationFailed(
            InteractionId,
            decision,
            reason,
            new AgentGenerationAttemptEvidence(GenerationAttemptId, SampleSnapshot.ProviderId, SampleSnapshot.ModelId, SampleSnapshot.ProviderCapabilityVersion, PromptTokenCount, OutputTokenCount)));
        return state;
    }

    private static AgentInteractionContextEvidence ConfirmationContextEvidence()
        => new(
            AgentInteractionContextMode.Full,
            FullContextTokenCount: 1_000,
            UsedContextTokenCount: 1_000,
            MessageCount: 3,
            ReservedOutputTokenCount,
            ContextWindowTokenLimit,
            ProviderCapabilityVersion,
            AgentInteractionSnapshot.DefaultContextPolicyReference,
            BoundedBehaviorReference: null);

    private static void AssertNotCreatable(DomainResult result, AgentProposedReplyNotCreatableReason expected)
    {
        result.IsRejection.ShouldBeTrue();
        ProposedAgentReplyNotCreatableRejection rejection = result.Events[0].ShouldBeOfType<ProposedAgentReplyNotCreatableRejection>();
        rejection.Reason.ShouldBe(expected);
        rejection.AgentInteractionId.ShouldBe(InteractionId);
        result.Events.OfType<ProposedAgentReplyCreated>().ShouldBeEmpty();
        result.Events.OfType<ProposedAgentReplyCreationFailed>().ShouldBeEmpty();
    }
}
