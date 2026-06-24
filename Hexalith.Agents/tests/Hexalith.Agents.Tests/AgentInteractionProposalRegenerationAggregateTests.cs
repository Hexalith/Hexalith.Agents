using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

using Hexalith.EventStore.Contracts.Results;

using Shouldly;

using static Hexalith.Agents.Tests.AgentInteractionTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Handle-method tests for the Story 3.4 Confirmation-mode proposal regeneration on <see cref="AgentInteractionAggregate"/>
/// (the 8th handler) (AC1, AC2, AC3, AC4; FR-14, FR-16; AD-3, AD-5, AD-13). Cover the authorized regeneration path (appends
/// the immutable regenerated version, sets ProposalState=Regenerated, preserves prior versions), every failure combination →
/// ProposalRegenerationFailed with the mapped reason while the proposal stays retryable, the not-regeneratable rejections
/// (never-requested / no-pending-proposal for AC4 — no provider event), idempotent terminal no-op (no duplicate version) plus
/// a second distinct regeneration, the failure-status retry trap (a failed regeneration never blocks a later one), the
/// Evaluate/Decide no-drift theory, and the full reflection-dispatch + JSON round-trip through <c>ProcessAsync</c>.
/// </summary>
public sealed class AgentInteractionProposalRegenerationAggregateTests
{
    // ===== Success: authorized regeneration (AC1, AC2) =====

    [Fact]
    public void Authorized_regeneration_records_proposal_regenerated_appending_the_version_and_preserving_prior_versions()
    {
        AgentInteractionState state = StateProposalCreated();
        int priorCount = state.GeneratedVersions!.Count; // the single generated version (Story 2.4)

        DomainResult result = Regenerate(RegeneratedProposalResult(), state);

        result.IsSuccess.ShouldBeTrue();
        ProposedAgentReplyRegenerated regenerated = result.Events[0].ShouldBeOfType<ProposedAgentReplyRegenerated>();
        regenerated.AgentInteractionId.ShouldBe(InteractionId);
        regenerated.RegeneratedVersion.VersionId.ShouldBe(RegeneratedVersionId);
        regenerated.RegeneratedVersion.Kind.ShouldBe(AgentGenerationKind.Regenerated);
        regenerated.RegeneratedVersion.SourceVersionId.ShouldBeNull(); // a fresh provider generation, not an edit
        regenerated.RegeneratedVersion.EditorPartyId.ShouldBeNull();
        regenerated.Evidence.ProposalId.ShouldBe(SampleProposalId);
        regenerated.Evidence.RegeneratedVersionId.ShouldBe(RegeneratedVersionId);
        regenerated.Evidence.RequesterPartyId.ShouldBe(RequesterPartyId);
        regenerated.Evidence.PolicyBasisVerdict.ShouldBe(ApproverPolicyValidationStatus.Valid);

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerated);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Regenerated);
        state.GeneratedVersions!.Count.ShouldBe(priorCount + 1); // appended (AC2)
        state.GeneratedVersions[0].VersionId.ShouldBe(PostedVersionId); // prior generated version preserved + inspectable (AC2)
        state.GeneratedVersions[^1].VersionId.ShouldBe(RegeneratedVersionId);
        state.ProposalRegenerationEvidence.ShouldNotBeNull().RegeneratedVersionId.ShouldBe(RegeneratedVersionId);
        state.ProposalRegenerationFailureReason.ShouldBeNull();

        // AD-14: the safe evidence is content-free, while the regeneration event itself IS the content's durable home.
        JsonSerializer.Serialize(regenerated.Evidence).ShouldNotContain(RegeneratedContentText);
        JsonSerializer.Serialize(regenerated).ShouldContain(RegeneratedContentText);
    }

    // ===== Every failure combination → ProposalRegenerationFailed with the mapped reason (AC3, AC4) =====

    [Theory]
    [InlineData(AgentProposalRegenerationOutcome.ProviderTimeout, ApproverPolicyValidationStatus.Valid, AgentProposalRegenerationFailureReason.ProviderTimeout)]
    [InlineData(AgentProposalRegenerationOutcome.ProviderDisabled, ApproverPolicyValidationStatus.Valid, AgentProposalRegenerationFailureReason.ProviderDisabled)]
    [InlineData(AgentProposalRegenerationOutcome.ProviderUnavailable, ApproverPolicyValidationStatus.Valid, AgentProposalRegenerationFailureReason.ProviderUnavailable)]
    [InlineData(AgentProposalRegenerationOutcome.AdapterFailure, ApproverPolicyValidationStatus.Valid, AgentProposalRegenerationFailureReason.AdapterFailure)]
    [InlineData(AgentProposalRegenerationOutcome.InvalidContext, ApproverPolicyValidationStatus.Valid, AgentProposalRegenerationFailureReason.InvalidContext)]
    [InlineData(AgentProposalRegenerationOutcome.ContentSafetyBlocked, ApproverPolicyValidationStatus.Valid, AgentProposalRegenerationFailureReason.ContentSafetyBlocked)]
    [InlineData(AgentProposalRegenerationOutcome.PolicyFailure, ApproverPolicyValidationStatus.Valid, AgentProposalRegenerationFailureReason.PolicyFailure)]
    [InlineData(AgentProposalRegenerationOutcome.Unknown, ApproverPolicyValidationStatus.Valid, AgentProposalRegenerationFailureReason.AdapterFailure)] // garbage outcome fails closed
    [InlineData(AgentProposalRegenerationOutcome.Regenerated, ApproverPolicyValidationStatus.Unauthorized, AgentProposalRegenerationFailureReason.NotAuthorized)] // non-Valid verdict dominates (defense in depth)
    [InlineData(AgentProposalRegenerationOutcome.Regenerated, ApproverPolicyValidationStatus.Unavailable, AgentProposalRegenerationFailureReason.NotAuthorized)]
    public void Each_failure_combination_records_proposal_regeneration_failed_keeps_proposal_retryable_and_preserves_versions(
        AgentProposalRegenerationOutcome outcome,
        ApproverPolicyValidationStatus verdict,
        AgentProposalRegenerationFailureReason expected)
    {
        AgentInteractionState state = StateProposalCreated();

        DomainResult result = Regenerate(RegenerationResult(outcome, verdict), state);

        ProposedAgentReplyRegenerationFailed failed = result.Events[0].ShouldBeOfType<ProposedAgentReplyRegenerationFailed>();
        failed.Reason.ShouldBe(expected);
        failed.AgentInteractionId.ShouldBe(InteractionId);
        result.Events.OfType<ProposedAgentReplyRegenerated>().ShouldBeEmpty();

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerationFailed);
        state.ProposalRegenerationFailureReason.ShouldBe(expected);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Pending); // retryable — preserved on a failed regeneration (AC3)
        state.GeneratedVersions!.Count.ShouldBe(1); // prior versions preserved; nothing appended

        // AD-14: a failed regeneration records NO content even when the (defense-in-depth) result carried a version.
        JsonSerializer.Serialize(failed).ShouldNotContain(RegeneratedContentText);
    }

    // ===== Not regeneratable: never requested / no pending proposal — AC4 (no provider event) =====

    [Fact]
    public void Regenerating_a_never_requested_interaction_is_not_regeneratable_interaction_not_proposed()
        => AssertNotRegeneratable(Regenerate(RegeneratedProposalResult(), state: null), AgentProposedReplyNotRegeneratableReason.InteractionNotProposed);

    [Fact]
    public void Regenerating_a_rejection_only_stream_is_not_regeneratable_interaction_not_proposed()
        => AssertNotRegeneratable(Regenerate(RegeneratedProposalResult(), new AgentInteractionState()), AgentProposedReplyNotRegeneratableReason.InteractionNotProposed);

    [Theory]
    [MemberData(nameof(NonPendingProposalStates))]
    public void Regenerating_without_a_pending_proposal_is_not_regeneratable_proposal_not_pending(AgentInteractionState state)
    {
        // A requested interaction that holds no pending/edited/regenerated proposal — never proposed, generated-only, or
        // creation-failed — is a structural rejection (no state change, no version, NO provider event), the AC4 enforcement
        // that a terminal proposal can never invoke the provider.
        DomainResult result = Regenerate(RegeneratedProposalResult(), state);

        AssertNotRegeneratable(result, AgentProposedReplyNotRegeneratableReason.ProposalNotPending);
    }

    public static TheoryData<AgentInteractionState> NonPendingProposalStates() =>
    [
        StateRequested(),
        StateGeneratedConfirmationMode(),
        ProposalCreationFailedState(),
    ];

    // ===== Idempotent terminal determinism (AD-13, AC2) =====

    [Fact]
    public void Re_dispatching_a_landed_regeneration_is_a_noop_and_never_duplicates_the_version()
    {
        AgentInteractionState state = StateProposalCreated();
        ApplyAll(state, Regenerate(RegeneratedProposalResult(), state)); // now ProposalRegenerated, the regenerated version appended
        state.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerated);
        int count = state.GeneratedVersions!.Count;

        DomainResult reissue = Regenerate(RegeneratedProposalResult(), state); // same deterministic regenerated version id

        reissue.IsNoOp.ShouldBeTrue();
        reissue.Events.ShouldBeEmpty();
        state.GeneratedVersions!.Count.ShouldBe(count); // no duplicate version (AC2)
        state.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerated);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Regenerated);
    }

    [Fact]
    public void A_second_distinct_regeneration_on_a_regenerated_proposal_appends_another_version()
    {
        // A proposal in the Regenerated sub-state is still pending and may be regenerated again — each distinct attempt (a
        // different deterministic regenerated version id) appends a new immutable version, preserving every prior one (AC2).
        AgentInteractionState state = StateProposalCreated();
        ApplyAll(state, Regenerate(RegeneratedProposalResult(), state)); // first regeneration (regenerated-version-001)
        int count = state.GeneratedVersions!.Count;

        DomainResult second = Regenerate(RegenerationResult(regeneratedVersionId: "regenerated-version-002"), state);

        second.IsSuccess.ShouldBeTrue();
        second.Events[0].ShouldBeOfType<ProposedAgentReplyRegenerated>().RegeneratedVersion.VersionId.ShouldBe("regenerated-version-002");
        ApplyAll(state, second);
        state.GeneratedVersions!.Count.ShouldBe(count + 1);
        state.GeneratedVersions!.Select(v => v.VersionId).ToList()
            .ShouldBe([PostedVersionId, RegeneratedVersionId, "regenerated-version-002"]); // every prior version preserved + inspectable (AC2)
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Regenerated);
    }

    // ===== Failure-status retry trap: a failed regeneration never blocks a later one (AC3) =====

    [Fact]
    public void A_regeneration_after_a_failed_regeneration_still_succeeds_because_the_proposal_stays_retryable()
    {
        AgentInteractionState state = StateProposalCreated();
        ApplyAll(state, Regenerate(ProviderTimeoutRegenerationResult(), state)); // failed regeneration → ProposalRegenerationFailed
        state.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerationFailed);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Pending); // still retryable (NOT blocked by the failure status)

        DomainResult retry = Regenerate(RegeneratedProposalResult(), state); // a fresh, successful attempt

        retry.IsSuccess.ShouldBeTrue();
        retry.Events[0].ShouldBeOfType<ProposedAgentReplyRegenerated>();
        ApplyAll(state, retry);
        state.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerated);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Regenerated);
        state.GeneratedVersions!.Count.ShouldBe(2); // the original generated version + the regenerated one
    }

    // ===== Decide / Evaluate no-drift =====

    [Theory]
    [InlineData(AgentProposalRegenerationOutcome.Regenerated, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalRegenerated)]
    [InlineData(AgentProposalRegenerationOutcome.ProviderTimeout, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalRegenerationFailed)]
    [InlineData(AgentProposalRegenerationOutcome.ContentSafetyBlocked, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalRegenerationFailed)]
    [InlineData(AgentProposalRegenerationOutcome.Unknown, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalRegenerationFailed)]
    [InlineData(AgentProposalRegenerationOutcome.Regenerated, ApproverPolicyValidationStatus.Unauthorized, AgentInteractionStatus.ProposalRegenerationFailed)]
    public void Decide_matches_the_aggregate_recorded_decision_for_each_outcome(
        AgentProposalRegenerationOutcome outcome,
        ApproverPolicyValidationStatus verdict,
        AgentInteractionStatus expected)
    {
        AgentProposalRegenerationResult result = RegenerationResult(outcome, verdict);

        AgentProposalRegenerationPolicy.Decide(result).ShouldBe(expected);

        DomainResult domainResult = Regenerate(result, StateProposalCreated());
        AgentInteractionStatus recorded = domainResult.Events[0] switch
        {
            ProposedAgentReplyRegenerated => AgentInteractionStatus.ProposalRegenerated,
            ProposedAgentReplyRegenerationFailed => AgentInteractionStatus.ProposalRegenerationFailed,
            _ => AgentInteractionStatus.Unknown,
        };
        recorded.ShouldBe(expected);
    }

    [Fact]
    public void Decide_and_Evaluate_never_drift_across_every_outcome_and_verdict()
    {
        // Task 5 / AD-5: Decide (orchestrator-facing) and Evaluate (aggregate-facing) both delegate to one private Compute, so
        // for EVERY (outcome, verdict) pair the decided status must equal the status the event Evaluate emits — no-drift proven
        // exhaustively over the whole enum domain, not a sampled subset. Only an authorized + content-bearing Regenerated
        // outcome succeeds; every other combination fails closed (AC3).
        ApproverPolicyValidationStatus[] verdicts =
        [
            ApproverPolicyValidationStatus.Valid,
            ApproverPolicyValidationStatus.Unauthorized,
            ApproverPolicyValidationStatus.Unavailable,
            ApproverPolicyValidationStatus.Incomplete,
        ];

        foreach (AgentProposalRegenerationOutcome outcome in Enum.GetValues<AgentProposalRegenerationOutcome>())
        {
            foreach (ApproverPolicyValidationStatus verdict in verdicts)
            {
                AgentProposalRegenerationResult result = RegenerationResult(outcome, verdict);

                AgentInteractionStatus decided = AgentProposalRegenerationPolicy.Decide(result);
                AgentInteractionStatus evaluated = AgentProposalRegenerationPolicy.Evaluate(InteractionId, result).Events[0] switch
                {
                    ProposedAgentReplyRegenerated => AgentInteractionStatus.ProposalRegenerated,
                    ProposedAgentReplyRegenerationFailed => AgentInteractionStatus.ProposalRegenerationFailed,
                    _ => AgentInteractionStatus.Unknown,
                };

                evaluated.ShouldBe(decided, $"Evaluate/Decide drifted for outcome={outcome}, verdict={verdict}");

                AgentInteractionStatus expected =
                    outcome == AgentProposalRegenerationOutcome.Regenerated && verdict == ApproverPolicyValidationStatus.Valid
                        ? AgentInteractionStatus.ProposalRegenerated
                        : AgentInteractionStatus.ProposalRegenerationFailed;
                decided.ShouldBe(expected, $"unexpected decision for outcome={outcome}, verdict={verdict}");
            }
        }
    }

    // ===== Full pipeline: reflection dispatch + JSON round-trip =====

    [Fact]
    public async Task Process_async_round_trips_the_regenerate_command_and_records_proposal_regenerated()
    {
        var aggregate = new AgentInteractionAggregate();
        AgentInteractionState state = StateProposalCreated();

        DomainResult result = await ProcessAndApplyAsync(aggregate, state, RegenerateCommand(RegeneratedProposalResult()));

        result.IsSuccess.ShouldBeTrue();
        ProposedAgentReplyRegenerated regenerated = result.Events[0].ShouldBeOfType<ProposedAgentReplyRegenerated>();
        regenerated.RegeneratedVersion.GeneratedContent.ShouldBe(RegeneratedContentText); // survived the JSON round-trip
        state.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerated);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Regenerated);
    }

    [Fact]
    public async Task Process_async_round_trips_a_failed_regeneration_and_keeps_the_proposal_retryable()
    {
        // The AC3 failure path through the SAME reflection-dispatch + JSON round-trip: a provider-timeout result records
        // ProposedAgentReplyRegenerationFailed, the proposal stays retryable (Pending), and no version is appended.
        var aggregate = new AgentInteractionAggregate();
        AgentInteractionState state = StateProposalCreated();
        int priorCount = state.GeneratedVersions!.Count;

        DomainResult result = await ProcessAndApplyAsync(aggregate, state, RegenerateCommand(ProviderTimeoutRegenerationResult()));

        result.IsSuccess.ShouldBeTrue();
        result.Events[0].ShouldBeOfType<ProposedAgentReplyRegenerationFailed>().Reason.ShouldBe(AgentProposalRegenerationFailureReason.ProviderTimeout);
        state.Status.ShouldBe(AgentInteractionStatus.ProposalRegenerationFailed);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Pending); // retryable — preserved on a failed regeneration (AC3)
        state.GeneratedVersions!.Count.ShouldBe(priorCount); // nothing appended
    }

    // ===== Helpers =====

    private static DomainResult Regenerate(AgentProposalRegenerationResult result, AgentInteractionState? state)
    {
        RegenerateProposedAgentReply command = RegenerateCommand(result);
        return AgentInteractionAggregate.Handle(command, state, Envelope(command));
    }

    private static AgentInteractionState ProposalCreationFailedState()
    {
        AgentInteractionState state = StateGeneratedConfirmationMode();
        state.Apply(new ProposedAgentReplyCreationFailed(
            InteractionId,
            AgentProposalCreationFailureReason.AdapterFailure,
            new AgentProposedReplyEvidence(
                string.Empty,
                SourceConversationId,
                string.Empty,
                ConfirmationSnapshot.ApproverPolicyVersion,
                ConfirmationSnapshot.ContentSafetyPolicyVersion,
                ExpiresAt: null)));
        return state;
    }

    private static void AssertNotRegeneratable(DomainResult result, AgentProposedReplyNotRegeneratableReason expected)
    {
        result.IsRejection.ShouldBeTrue();
        ProposedAgentReplyNotRegeneratableRejection rejection = result.Events[0].ShouldBeOfType<ProposedAgentReplyNotRegeneratableRejection>();
        rejection.Reason.ShouldBe(expected);
        rejection.AgentInteractionId.ShouldBe(InteractionId);
        result.Events.OfType<ProposedAgentReplyRegenerated>().ShouldBeEmpty();
        result.Events.OfType<ProposedAgentReplyRegenerationFailed>().ShouldBeEmpty();
    }
}
