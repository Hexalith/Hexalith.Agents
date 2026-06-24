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
/// Handle-method tests for the Story 3.3 Confirmation-mode proposal edit on <see cref="AgentInteractionAggregate"/> (the
/// 7th handler) (AC1, AC2, AC4; FR-14, FR-15; AD-3, AD-5, AD-13). Cover the authorized edit path (appends the immutable
/// edited version, sets ProposalState=Edited, preserves prior versions), every failure combination →
/// ProposalEditFailed with the mapped reason, the not-editable rejections (never-requested / no-pending-proposal for AC2),
/// idempotent terminal no-op (no duplicate version) plus a second distinct edit, the Evaluate/Decide no-drift theory, and
/// the full reflection-dispatch + JSON round-trip through <c>ProcessAsync</c>.
/// </summary>
public sealed class AgentInteractionProposalEditAggregateTests
{
    // ===== Success: authorized edit (AC1, AC4) =====

    [Fact]
    public void Authorized_edit_records_proposal_edited_appending_the_version_and_preserving_prior_versions()
    {
        AgentInteractionState state = StateProposalCreated();
        int priorCount = state.GeneratedVersions!.Count; // the single generated version (Story 2.4)

        DomainResult result = Edit(EditedProposalResult(), state);

        result.IsSuccess.ShouldBeTrue();
        ProposedAgentReplyEdited edited = result.Events[0].ShouldBeOfType<ProposedAgentReplyEdited>();
        edited.AgentInteractionId.ShouldBe(InteractionId);
        edited.EditedVersion.VersionId.ShouldBe(EditedVersionId);
        edited.EditedVersion.Kind.ShouldBe(AgentGenerationKind.Edited);
        edited.EditedVersion.SourceVersionId.ShouldBe(PostedVersionId);
        edited.EditedVersion.EditorPartyId.ShouldBe(EditorPartyId);
        edited.Evidence.ProposalId.ShouldBe(SampleProposalId);
        edited.Evidence.EditedVersionId.ShouldBe(EditedVersionId);
        edited.Evidence.SourceVersionId.ShouldBe(PostedVersionId);
        edited.Evidence.EditorPartyId.ShouldBe(EditorPartyId);
        edited.Evidence.PolicyBasisVerdict.ShouldBe(ApproverPolicyValidationStatus.Valid);

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.ProposalEdited);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Edited);
        state.GeneratedVersions!.Count.ShouldBe(priorCount + 1); // appended (AC1)
        state.GeneratedVersions[0].VersionId.ShouldBe(PostedVersionId); // prior generated version preserved + inspectable (AC1)
        state.GeneratedVersions[^1].VersionId.ShouldBe(EditedVersionId);
        state.ProposalEditEvidence.ShouldNotBeNull().EditedVersionId.ShouldBe(EditedVersionId);
        state.ProposalEditFailureReason.ShouldBeNull();

        // AD-14: the safe evidence is content-free, while the edit event itself IS the content's durable home.
        JsonSerializer.Serialize(edited.Evidence).ShouldNotContain(EditedContentText);
        JsonSerializer.Serialize(edited).ShouldContain(EditedContentText);
    }

    // ===== Every failure combination → ProposalEditFailed with the mapped reason (AC2, AC4) =====

    [Theory]
    [InlineData(AgentProposalEditOutcome.AdapterFailure, ApproverPolicyValidationStatus.Valid, AgentProposalEditFailureReason.AdapterFailure)]
    [InlineData(AgentProposalEditOutcome.Unknown, ApproverPolicyValidationStatus.Valid, AgentProposalEditFailureReason.AdapterFailure)] // garbage outcome fails closed
    [InlineData(AgentProposalEditOutcome.Edited, ApproverPolicyValidationStatus.Unauthorized, AgentProposalEditFailureReason.NotAuthorized)] // non-Valid verdict dominates (defense in depth)
    [InlineData(AgentProposalEditOutcome.Edited, ApproverPolicyValidationStatus.Unavailable, AgentProposalEditFailureReason.NotAuthorized)]
    public void Each_failure_combination_records_proposal_edit_failed_and_preserves_prior_versions(
        AgentProposalEditOutcome outcome,
        ApproverPolicyValidationStatus verdict,
        AgentProposalEditFailureReason expected)
    {
        AgentInteractionState state = StateProposalCreated();

        DomainResult result = Edit(EditResult(outcome, verdict), state);

        ProposedAgentReplyEditFailed failed = result.Events[0].ShouldBeOfType<ProposedAgentReplyEditFailed>();
        failed.Reason.ShouldBe(expected);
        failed.AgentInteractionId.ShouldBe(InteractionId);
        result.Events.OfType<ProposedAgentReplyEdited>().ShouldBeEmpty();

        ApplyAll(state, result);
        state.Status.ShouldBe(AgentInteractionStatus.ProposalEditFailed);
        state.ProposalEditFailureReason.ShouldBe(expected);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Pending); // unchanged — no new version on a failed edit (AC2)
        state.GeneratedVersions!.Count.ShouldBe(1); // prior versions preserved; nothing appended

        // AD-14: a failed edit records NO content.
        JsonSerializer.Serialize(failed).ShouldNotContain(EditedContentText);
    }

    // ===== Not editable: never requested / no pending proposal (AC2; AD-12) =====

    [Fact]
    public void Editing_a_never_requested_interaction_is_not_editable_interaction_not_proposed()
        => AssertNotEditable(Edit(EditedProposalResult(), state: null), AgentProposedReplyNotEditableReason.InteractionNotProposed);

    [Fact]
    public void Editing_a_rejection_only_stream_is_not_editable_interaction_not_proposed()
        => AssertNotEditable(Edit(EditedProposalResult(), new AgentInteractionState()), AgentProposedReplyNotEditableReason.InteractionNotProposed);

    [Theory]
    [MemberData(nameof(NonPendingProposalStates))]
    public void Editing_without_a_pending_proposal_is_not_editable_proposal_not_pending(AgentInteractionState state)
    {
        // A requested interaction that holds no pending/edited proposal — never proposed, generated-only, or
        // creation-failed — is a structural rejection (no state change, no version), the AC2 enforcement that an
        // un-postable proposal can never be edited.
        DomainResult result = Edit(EditedProposalResult(), state);

        AssertNotEditable(result, AgentProposedReplyNotEditableReason.ProposalNotPending);
    }

    public static TheoryData<AgentInteractionState> NonPendingProposalStates() =>
    [
        StateRequested(),
        StateGeneratedConfirmationMode(),
        ProposalCreationFailedState(),
    ];

    // ===== Idempotent terminal determinism (AD-13, AC4) =====

    [Fact]
    public void Re_dispatching_a_landed_edit_is_a_noop_and_never_duplicates_the_version()
    {
        AgentInteractionState state = StateProposalCreated();
        ApplyAll(state, Edit(EditedProposalResult(), state)); // now ProposalEdited, the edited version appended
        state.Status.ShouldBe(AgentInteractionStatus.ProposalEdited);
        int count = state.GeneratedVersions!.Count;

        DomainResult reissue = Edit(EditedProposalResult(), state); // same deterministic edited version id

        reissue.IsNoOp.ShouldBeTrue();
        reissue.Events.ShouldBeEmpty();
        state.GeneratedVersions!.Count.ShouldBe(count); // no duplicate version (AC4)
        state.Status.ShouldBe(AgentInteractionStatus.ProposalEdited);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Edited);
    }

    [Fact]
    public void A_second_distinct_edit_on_an_edited_proposal_appends_another_version()
    {
        // A proposal in the Edited sub-state is still pending and may be edited again — each distinct edit (a different
        // deterministic edited version id) appends a new immutable version, preserving every prior one (AC1).
        AgentInteractionState state = StateProposalCreated();
        ApplyAll(state, Edit(EditedProposalResult(), state)); // first edit (edited-version-001)
        int count = state.GeneratedVersions!.Count;

        DomainResult second = Edit(EditResult(editedVersionId: "edited-version-002"), state);

        second.IsSuccess.ShouldBeTrue();
        second.Events[0].ShouldBeOfType<ProposedAgentReplyEdited>().EditedVersion.VersionId.ShouldBe("edited-version-002");
        ApplyAll(state, second);
        state.GeneratedVersions!.Count.ShouldBe(count + 1);
        state.GeneratedVersions!.Select(v => v.VersionId).ToList()
            .ShouldBe([PostedVersionId, EditedVersionId, "edited-version-002"]); // every prior version preserved + inspectable (AC1)
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Edited);
    }

    // ===== Decide / Evaluate no-drift =====

    [Theory]
    [InlineData(AgentProposalEditOutcome.Edited, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalEdited)]
    [InlineData(AgentProposalEditOutcome.AdapterFailure, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalEditFailed)]
    [InlineData(AgentProposalEditOutcome.Unknown, ApproverPolicyValidationStatus.Valid, AgentInteractionStatus.ProposalEditFailed)]
    [InlineData(AgentProposalEditOutcome.Edited, ApproverPolicyValidationStatus.Unauthorized, AgentInteractionStatus.ProposalEditFailed)]
    public void Decide_matches_the_aggregate_recorded_decision_for_each_outcome(
        AgentProposalEditOutcome outcome,
        ApproverPolicyValidationStatus verdict,
        AgentInteractionStatus expected)
    {
        AgentProposalEditResult result = EditResult(outcome, verdict);

        AgentProposalEditPolicy.Decide(result).ShouldBe(expected);

        DomainResult domainResult = Edit(result, StateProposalCreated());
        AgentInteractionStatus recorded = domainResult.Events[0] switch
        {
            ProposedAgentReplyEdited => AgentInteractionStatus.ProposalEdited,
            ProposedAgentReplyEditFailed => AgentInteractionStatus.ProposalEditFailed,
            _ => AgentInteractionStatus.Unknown,
        };
        recorded.ShouldBe(expected);
    }

    // ===== Full pipeline: reflection dispatch + JSON round-trip =====

    [Fact]
    public async Task Process_async_round_trips_the_edit_command_and_records_proposal_edited()
    {
        var aggregate = new AgentInteractionAggregate();
        AgentInteractionState state = StateProposalCreated();

        DomainResult result = await ProcessAndApplyAsync(aggregate, state, EditCommand(EditedProposalResult()));

        result.IsSuccess.ShouldBeTrue();
        ProposedAgentReplyEdited edited = result.Events[0].ShouldBeOfType<ProposedAgentReplyEdited>();
        edited.EditedVersion.GeneratedContent.ShouldBe(EditedContentText); // survived the JSON round-trip
        state.Status.ShouldBe(AgentInteractionStatus.ProposalEdited);
        state.ProposalState.ShouldBe(ProposedAgentReplyState.Edited);
    }

    // ===== Helpers =====

    private static DomainResult Edit(AgentProposalEditResult result, AgentInteractionState? state)
    {
        EditProposedAgentReply command = EditCommand(result);
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

    private static void AssertNotEditable(DomainResult result, AgentProposedReplyNotEditableReason expected)
    {
        result.IsRejection.ShouldBeTrue();
        ProposedAgentReplyNotEditableRejection rejection = result.Events[0].ShouldBeOfType<ProposedAgentReplyNotEditableRejection>();
        rejection.Reason.ShouldBe(expected);
        rejection.AgentInteractionId.ShouldBe(InteractionId);
        result.Events.OfType<ProposedAgentReplyEdited>().ShouldBeEmpty();
        result.Events.OfType<ProposedAgentReplyEditFailed>().ShouldBeEmpty();
    }
}
