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

    // ===== Story 2.3 context replay (status transitions; AC2, AC3, AC4) =====

    [Fact]
    public void Apply_context_ready_transitions_status_and_records_safe_evidence()
    {
        AgentInteractionState state = StateAuthorized();
        AgentInteractionContextReady ready = AgentInteractionContextPolicy.Evaluate(InteractionId, FullFitsMeasurement())
            .Events[0].ShouldBeOfType<AgentInteractionContextReady>();

        state.Apply(ready);

        state.Status.ShouldBe(AgentInteractionStatus.ContextReady);
        state.ContextEvidence.ShouldNotBeNull().Mode.ShouldBe(AgentInteractionContextMode.Full);
        state.ContextBlockReason.ShouldBeNull();
        state.Prompt.ShouldBe(Prompt); // the request payload is untouched by context building (AD-14)
    }

    [Fact]
    public void Apply_context_blocked_records_the_decision_and_block_reason()
    {
        AgentInteractionState state = StateAuthorized();
        AgentInteractionContextBlocked blocked = AgentInteractionContextPolicy.Evaluate(InteractionId, OversizedMeasurement())
            .Events[0].ShouldBeOfType<AgentInteractionContextBlocked>();

        state.Apply(blocked);

        state.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
        state.ContextBlockReason.ShouldBe(AgentInteractionContextBlockReason.ExceedsModelBudget);
        state.ContextEvidence.ShouldNotBeNull().FullContextTokenCount.ShouldBe(200_000);
    }

    [Fact]
    public void Apply_context_not_buildable_rejection_is_a_replay_safe_noop()
    {
        AgentInteractionState state = StateAuthorized();

        // A persisted not-buildable rejection must not throw or mutate the recorded request/status.
        state.Apply(new AgentInteractionContextNotBuildableRejection(InteractionId, AgentInteractionContextNotBuildableReason.InteractionNotAuthorized));

        state.Status.ShouldBe(AgentInteractionStatus.Authorized);
        state.IsRequested.ShouldBeTrue();
        state.ContextEvidence.ShouldBeNull();
        state.ContextBlockReason.ShouldBeNull();
    }

    [Fact]
    public void Context_outcome_applies_only_over_a_requested_stream()
    {
        // A context outcome event ahead of the request (a malformed stream) must not flip status — every non-request
        // Apply keeps the IsRequested guard so replay over a stream that begins before the request stays total.
        var state = new AgentInteractionState();
        AgentInteractionContextEvidence evidence = SampleEvidence();

        state.Apply(new AgentInteractionContextReady(InteractionId, evidence));
        state.Apply(new AgentInteractionContextBlocked(InteractionId, AgentInteractionContextBlockReason.ExceedsModelBudget, evidence));

        state.IsRequested.ShouldBeFalse();
        state.Status.ShouldBe(AgentInteractionStatus.Unknown);
        state.ContextEvidence.ShouldBeNull();
        state.ContextBlockReason.ShouldBeNull();
    }

    [Fact]
    public void Replay_over_request_authorize_then_context_ready_is_deterministic_across_rebuilds()
    {
        AgentInteractionState first = Rebuild();
        AgentInteractionState second = Rebuild();

        second.Status.ShouldBe(first.Status);
        second.Status.ShouldBe(AgentInteractionStatus.ContextReady);
        second.ContextEvidence.ShouldNotBeNull().Mode.ShouldBe(first.ContextEvidence!.Mode);
        second.AgentInteractionId.ShouldBe(first.AgentInteractionId);

        static AgentInteractionState Rebuild()
        {
            var state = new AgentInteractionState();
            state.Apply(RequestedEvent(ValidRequest()));
            state.Apply(new AgentInteractionAuthorized(InteractionId));
            state.Apply(new AgentInteractionContextReady(InteractionId, SampleEvidence()));
            return state;
        }
    }

    [Fact]
    public void Replay_over_request_authorize_then_context_blocked_is_deterministic_across_rebuilds()
    {
        // The fail-closed block decision (status + reason + evidence) must rehydrate identically on every rebuild so an
        // administrator inspecting the audit record always sees the same context-blocked outcome (AC3; FR-24).
        AgentInteractionState first = Rebuild();
        AgentInteractionState second = Rebuild();

        second.Status.ShouldBe(first.Status);
        second.Status.ShouldBe(AgentInteractionStatus.ContextBlocked);
        second.ContextBlockReason.ShouldBe(first.ContextBlockReason);
        second.ContextBlockReason.ShouldBe(AgentInteractionContextBlockReason.ExceedsModelBudget);
        second.ContextEvidence.ShouldNotBeNull().FullContextTokenCount.ShouldBe(first.ContextEvidence!.FullContextTokenCount);

        static AgentInteractionState Rebuild()
        {
            var state = new AgentInteractionState();
            state.Apply(RequestedEvent(ValidRequest()));
            state.Apply(new AgentInteractionAuthorized(InteractionId));
            state.Apply(AgentInteractionContextPolicy.Evaluate(InteractionId, OversizedMeasurement())
                .Events[0].ShouldBeOfType<AgentInteractionContextBlocked>());
            return state;
        }
    }

    // ===== Story 2.4 generation replay (status transitions; AC2, AC3, AC4) =====

    [Fact]
    public void Apply_generated_transitions_status_and_appends_the_version()
    {
        AgentInteractionState state = StateContextReady();

        state.Apply(new AgentOutputGenerated(InteractionId, SampleVersion()));

        state.Status.ShouldBe(AgentInteractionStatus.Generated);
        state.GeneratedVersions.ShouldNotBeNull().ShouldHaveSingleItem().GeneratedContent.ShouldBe(GeneratedContentText);
        state.GenerationFailureReason.ShouldBeNull();
    }

    [Fact]
    public void Apply_generated_twice_appends_to_the_version_history()
    {
        // Epic 3 regeneration appends; the state holds an append-only version history (AD-5).
        AgentInteractionState state = StateContextReady();

        state.Apply(new AgentOutputGenerated(InteractionId, SampleVersion("attempt-a")));
        state.Apply(new AgentOutputGenerated(InteractionId, SampleVersion("attempt-b")));

        state.GeneratedVersions.ShouldNotBeNull().Count.ShouldBe(2);
        state.GeneratedVersions![0].AttemptId.ShouldBe("attempt-a");
        state.GeneratedVersions[1].AttemptId.ShouldBe("attempt-b");
    }

    [Fact]
    public void Apply_generation_failed_records_the_decision_and_reason()
    {
        AgentInteractionState state = StateContextReady();

        state.Apply(new AgentOutputGenerationFailed(
            InteractionId,
            AgentInteractionStatus.SafetyFailed,
            AgentOutputGenerationFailureReason.ContentSafetyBlocked,
            SampleAttemptEvidence()));

        state.Status.ShouldBe(AgentInteractionStatus.SafetyFailed);
        state.GenerationFailureReason.ShouldBe(AgentOutputGenerationFailureReason.ContentSafetyBlocked);
        state.GeneratedVersions.ShouldBeNull(); // no approvable version on a failure (AD-5)
    }

    [Fact]
    public void Apply_not_generatable_rejection_is_a_replay_safe_noop()
    {
        AgentInteractionState state = StateContextReady();

        // A persisted not-generatable rejection must not throw or mutate the recorded request/status.
        state.Apply(new AgentOutputNotGeneratableRejection(InteractionId, AgentOutputNotGeneratableReason.ContextNotReady));

        state.Status.ShouldBe(AgentInteractionStatus.ContextReady);
        state.IsRequested.ShouldBeTrue();
        state.GeneratedVersions.ShouldBeNull();
        state.GenerationFailureReason.ShouldBeNull();
    }

    [Fact]
    public void Generation_outcome_applies_only_over_a_requested_stream()
    {
        // A generation outcome event ahead of the request (a malformed stream) must not flip status — every non-request
        // Apply keeps the IsRequested guard so replay over a stream that begins before the request stays total.
        var state = new AgentInteractionState();

        state.Apply(new AgentOutputGenerated(InteractionId, SampleVersion()));
        state.Apply(new AgentOutputGenerationFailed(
            InteractionId,
            AgentInteractionStatus.GenerationFailed,
            AgentOutputGenerationFailureReason.ProviderTimeout,
            SampleAttemptEvidence()));

        state.IsRequested.ShouldBeFalse();
        state.Status.ShouldBe(AgentInteractionStatus.Unknown);
        state.GeneratedVersions.ShouldBeNull();
        state.GenerationFailureReason.ShouldBeNull();
    }

    [Fact]
    public void Replay_over_request_authorize_context_then_generated_is_deterministic_across_rebuilds()
    {
        AgentInteractionState first = Rebuild();
        AgentInteractionState second = Rebuild();

        second.Status.ShouldBe(first.Status);
        second.Status.ShouldBe(AgentInteractionStatus.Generated);
        second.GeneratedVersions.ShouldNotBeNull().Count.ShouldBe(first.GeneratedVersions!.Count);
        second.GeneratedVersions![0].VersionId.ShouldBe(first.GeneratedVersions![0].VersionId);

        static AgentInteractionState Rebuild()
        {
            AgentInteractionState state = StateContextReady();
            state.Apply(new AgentOutputGenerated(InteractionId, SampleVersion()));
            return state;
        }
    }

    // ===== Story 2.5 posting replay (status transitions; AC1, AC4) =====

    [Fact]
    public void Apply_posted_transitions_status_and_records_safe_evidence()
    {
        AgentInteractionState state = StateGenerated();

        state.Apply(new AgentResponsePosted(InteractionId, SamplePostedEvidence()));

        state.Status.ShouldBe(AgentInteractionStatus.Posted);
        state.PostingEvidence.ShouldNotBeNull().MessageId.ShouldBe(PostedMessageId);
        state.PostingFailureReason.ShouldBeNull();
        state.Prompt.ShouldBe(Prompt); // the request payload is untouched by posting (AD-14)
    }

    [Fact]
    public void Apply_posting_failed_records_the_decision_reason_and_evidence()
    {
        AgentInteractionState state = StateGenerated();

        state.Apply(new AgentResponsePostingFailed(
            InteractionId,
            AgentResponsePostingFailureReason.MembershipUnavailable,
            SamplePostedEvidence()));

        state.Status.ShouldBe(AgentInteractionStatus.PostingFailed);
        state.PostingFailureReason.ShouldBe(AgentResponsePostingFailureReason.MembershipUnavailable);
        state.PostingEvidence.ShouldNotBeNull().AgentPartyId.ShouldBe(AgentPartyId);
    }

    [Fact]
    public void Apply_not_postable_rejection_is_a_replay_safe_noop()
    {
        AgentInteractionState state = StateGenerated();

        // A persisted not-postable rejection must not throw or mutate the recorded request/status.
        state.Apply(new AgentResponseNotPostableRejection(InteractionId, AgentResponseNotPostableReason.OutputNotGenerated));

        state.Status.ShouldBe(AgentInteractionStatus.Generated);
        state.IsRequested.ShouldBeTrue();
        state.PostingEvidence.ShouldBeNull();
        state.PostingFailureReason.ShouldBeNull();
    }

    [Fact]
    public void Posting_outcome_applies_only_over_a_requested_stream()
    {
        // A posting outcome event ahead of the request (a malformed stream) must not flip status — every non-request
        // Apply keeps the IsRequested guard so replay over a stream that begins before the request stays total.
        var state = new AgentInteractionState();

        state.Apply(new AgentResponsePosted(InteractionId, SamplePostedEvidence()));
        state.Apply(new AgentResponsePostingFailed(InteractionId, AgentResponsePostingFailureReason.AdapterFailure, SamplePostedEvidence()));

        state.IsRequested.ShouldBeFalse();
        state.Status.ShouldBe(AgentInteractionStatus.Unknown);
        state.PostingEvidence.ShouldBeNull();
        state.PostingFailureReason.ShouldBeNull();
    }

    [Fact]
    public void Replay_over_request_through_posted_is_deterministic_across_rebuilds()
    {
        AgentInteractionState first = Rebuild();
        AgentInteractionState second = Rebuild();

        second.Status.ShouldBe(first.Status);
        second.Status.ShouldBe(AgentInteractionStatus.Posted);
        second.PostingEvidence.ShouldNotBeNull().MessageId.ShouldBe(first.PostingEvidence!.MessageId);
        second.AgentInteractionId.ShouldBe(first.AgentInteractionId);

        static AgentInteractionState Rebuild()
        {
            AgentInteractionState state = StateGenerated();
            state.Apply(new AgentResponsePosted(InteractionId, SamplePostedEvidence()));
            return state;
        }
    }

    [Fact]
    public void Replay_over_request_through_posting_failed_is_deterministic_across_rebuilds()
    {
        // The fail-closed posting audit record (status + safe reason + safe-id evidence) must rehydrate identically across
        // independent rebuilds, so the durable PostingFailed Audit Evidence is stable for inspection and never drifts
        // (AC4; AD-13). Mirrors the context-blocked determinism guard for the post step.
        AgentInteractionState first = Rebuild();
        AgentInteractionState second = Rebuild();

        second.Status.ShouldBe(first.Status);
        second.Status.ShouldBe(AgentInteractionStatus.PostingFailed);
        second.PostingFailureReason.ShouldBe(first.PostingFailureReason);
        second.PostingFailureReason.ShouldBe(AgentResponsePostingFailureReason.MembershipUnavailable);
        second.PostingEvidence.ShouldNotBeNull().AgentPartyId.ShouldBe(first.PostingEvidence!.AgentPartyId);

        static AgentInteractionState Rebuild()
        {
            AgentInteractionState state = StateGenerated();
            state.Apply(new AgentResponsePostingFailed(
                InteractionId, AgentResponsePostingFailureReason.MembershipUnavailable, SamplePostedEvidence()));
            return state;
        }
    }

    // ===== Story 3.1 proposal replay (status transitions; AC1, AC3, AC4) =====

    [Fact]
    public void Apply_proposal_created_transitions_status_and_records_pending_state_and_safe_evidence()
    {
        AgentInteractionState state = StateGeneratedConfirmationMode();

        state.Apply(new ProposedAgentReplyCreated(InteractionId, SampleProposalEvidence()));

        state.Status.ShouldBe(AgentInteractionStatus.ProposalCreated);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Pending);
        state.ProposalEvidence.ShouldNotBeNull().ProposalId.ShouldBe(SampleProposalId);
        state.ProposalCreationFailureReason.ShouldBeNull();
        state.Prompt.ShouldBe(Prompt); // the request payload is untouched by proposal creation (AD-14)
    }

    [Fact]
    public void Apply_proposal_creation_failed_records_the_decision_reason_and_evidence_with_no_proposal_state()
    {
        AgentInteractionState state = StateGeneratedConfirmationMode();

        state.Apply(new ProposedAgentReplyCreationFailed(
            InteractionId,
            AgentProposalCreationFailureReason.GeneratedVersionUnavailable,
            SampleProposalEvidence()));

        state.Status.ShouldBe(AgentInteractionStatus.ProposalCreationFailed);
        state.ProposalCreationFailureReason.ShouldBe(AgentProposalCreationFailureReason.GeneratedVersionUnavailable);
        state.ProposalEvidence.ShouldNotBeNull().ProposalId.ShouldBe(SampleProposalId);
        state.ProposalState.ShouldBeNull(); // no proposal exists on a failure
    }

    [Fact]
    public void Apply_not_creatable_rejection_is_a_replay_safe_noop()
    {
        AgentInteractionState state = StateGeneratedConfirmationMode();

        // A persisted not-creatable rejection must not throw or mutate the recorded request/status.
        state.Apply(new ProposedAgentReplyNotCreatableRejection(InteractionId, AgentProposedReplyNotCreatableReason.OutputNotGenerated));

        state.Status.ShouldBe(AgentInteractionStatus.Generated);
        state.IsRequested.ShouldBeTrue();
        state.ProposalEvidence.ShouldBeNull();
        state.ProposalState.ShouldBeNull();
        state.ProposalCreationFailureReason.ShouldBeNull();
    }

    [Fact]
    public void Proposal_outcome_applies_only_over_a_requested_stream()
    {
        // A proposal outcome event ahead of the request (a malformed stream) must not flip status — every non-request Apply
        // keeps the IsRequested guard so replay over a stream that begins before the request stays total.
        var state = new AgentInteractionState();

        state.Apply(new ProposedAgentReplyCreated(InteractionId, SampleProposalEvidence()));
        state.Apply(new ProposedAgentReplyCreationFailed(InteractionId, AgentProposalCreationFailureReason.AdapterFailure, SampleProposalEvidence()));

        state.IsRequested.ShouldBeFalse();
        state.Status.ShouldBe(AgentInteractionStatus.Unknown);
        state.ProposalEvidence.ShouldBeNull();
        state.ProposalState.ShouldBeNull();
        state.ProposalCreationFailureReason.ShouldBeNull();
    }

    [Fact]
    public void Replay_over_request_through_proposal_created_is_deterministic_across_rebuilds()
    {
        AgentInteractionState first = Rebuild();
        AgentInteractionState second = Rebuild();

        second.Status.ShouldBe(first.Status);
        second.Status.ShouldBe(AgentInteractionStatus.ProposalCreated);
        second.ProposalState.ShouldBe(first.ProposalState);
        second.ProposalEvidence.ShouldNotBeNull().ProposalId.ShouldBe(first.ProposalEvidence!.ProposalId);
        second.AgentInteractionId.ShouldBe(first.AgentInteractionId);

        static AgentInteractionState Rebuild()
        {
            AgentInteractionState state = StateGeneratedConfirmationMode();
            state.Apply(new ProposedAgentReplyCreated(InteractionId, SampleProposalEvidence()));
            return state;
        }
    }

    [Fact]
    public void Replay_over_request_through_proposal_creation_failed_is_deterministic_across_rebuilds()
    {
        // The fail-closed proposal audit record (status + safe reason + safe-id evidence) must rehydrate identically across
        // independent rebuilds, so the durable ProposalCreationFailed Audit Evidence is stable for inspection (AC4; AD-13).
        AgentInteractionState first = Rebuild();
        AgentInteractionState second = Rebuild();

        second.Status.ShouldBe(first.Status);
        second.Status.ShouldBe(AgentInteractionStatus.ProposalCreationFailed);
        second.ProposalCreationFailureReason.ShouldBe(first.ProposalCreationFailureReason);
        second.ProposalCreationFailureReason.ShouldBe(AgentProposalCreationFailureReason.GeneratedVersionUnavailable);
        second.ProposalEvidence.ShouldNotBeNull().ProposalId.ShouldBe(first.ProposalEvidence!.ProposalId);

        static AgentInteractionState Rebuild()
        {
            AgentInteractionState state = StateGeneratedConfirmationMode();
            state.Apply(new ProposedAgentReplyCreationFailed(
                InteractionId, AgentProposalCreationFailureReason.GeneratedVersionUnavailable, SampleProposalEvidence()));
            return state;
        }
    }

    private static AgentProposedReplyEvidence SampleProposalEvidence()
        => new(
            SampleProposalId,
            SourceConversationId,
            PostedVersionId,
            ConfirmationSnapshot.ApproverPolicyVersion,
            ConfirmationSnapshot.ContentSafetyPolicyVersion,
            ExpiresAt: null);

    private static AgentPostedMessageEvidence SamplePostedEvidence()
        => new(PostedMessageId, SourceConversationId, AgentPartyId, PostedVersionId);

    private static AgentInteractionContextEvidence SampleEvidence()
        => new(
            AgentInteractionContextMode.Full,
            FullContextTokenCount: 1_000,
            UsedContextTokenCount: 1_000,
            MessageCount: 3,
            ReservedOutputTokenCount: ReservedOutputTokenCount,
            ContextWindowTokenLimit: ContextWindowTokenLimit,
            ProviderCapabilityVersion: ProviderCapabilityVersion,
            AgentInteractionSnapshot.DefaultContextPolicyReference,
            BoundedBehaviorReference: null);

    private static AgentGeneratedVersion SampleVersion(string attemptId = GenerationAttemptId)
        => new(
            VersionId: $"version-{attemptId}",
            attemptId,
            AgentGenerationKind.Generated,
            GeneratedContentText,
            SampleSnapshot.ProviderId,
            SampleSnapshot.ModelId,
            SampleSnapshot.ProviderCapabilityVersion,
            ContentSafetyPolicyVersion,
            PromptTokenCount,
            OutputTokenCount);

    private static AgentGenerationAttemptEvidence SampleAttemptEvidence()
        => new(
            GenerationAttemptId,
            SampleSnapshot.ProviderId,
            SampleSnapshot.ModelId,
            SampleSnapshot.ProviderCapabilityVersion,
            PromptTokenCount,
            OutputTokenCount);
}
