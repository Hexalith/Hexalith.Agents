using System.Text.Json;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Events;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Contracts.Operations;

using Shouldly;

using static Hexalith.Agents.Tests.AgentInteractionTestData;

namespace Hexalith.Agents.Tests;

public sealed class AgentInteractionAuditInspectionTests
{
    [Fact]
    public void GetStatus_when_not_authorized_returns_no_view()
    {
        AgentInteractionInspectionResult result = AgentInteractionAuditInspection.GetStatus(StateGenerated(), isAuthorized: false);

        result.Status.ShouldBe(AgentInteractionInspectionStatus.NotAuthorized);
        result.View.ShouldBeNull();
    }

    [Fact]
    public void GetGateEvidence_when_missing_returns_not_found_like_unrequested_stream()
    {
        AgentInteractionGateEvidenceResult missing = AgentInteractionAuditInspection.GetGateEvidence(null, isAuthorized: true);
        AgentInteractionGateEvidenceResult unrequested = AgentInteractionAuditInspection.GetGateEvidence(new AgentInteractionState(), isAuthorized: true);

        missing.Status.ShouldBe(AgentInteractionGateInspectionStatus.NotFound);
        missing.Evidence.ShouldBeNull();
        unrequested.Status.ShouldBe(AgentInteractionGateInspectionStatus.NotFound);
        unrequested.Evidence.ShouldBeNull();
    }

    [Fact]
    public void GetStatus_maps_snapshot_without_prompt_or_generated_content()
    {
        AgentInteractionState state = StateGenerated();

        AgentInteractionInspectionResult result = AgentInteractionAuditInspection.GetStatus(state, isAuthorized: true);

        result.Status.ShouldBe(AgentInteractionInspectionStatus.Success);
        result.View.ShouldNotBeNull().ShouldSatisfyAllConditions(
            v => v.AgentInteractionId.ShouldBe(InteractionId),
            v => v.AgentId.ShouldBe(AgentId),
            v => v.CallerPartyId.ShouldBe(CallerPartyId),
            v => v.SourceConversationId.ShouldBe(SourceConversationId),
            v => v.ProviderId.ShouldBe(SampleSnapshot.ProviderId),
            v => v.ModelId.ShouldBe(SampleSnapshot.ModelId),
            v => v.ResponseMode.ShouldBe(AgentResponseMode.Automatic),
            v => v.ContentSafetyPolicyVersion.ShouldBe(SampleSnapshot.ContentSafetyPolicyVersion));
        JsonSerializer.Serialize(result).ShouldNotContain(Prompt);
        JsonSerializer.Serialize(result).ShouldNotContain(GeneratedContentText);
    }

    [Fact]
    public void GetGenerationEvidence_maps_latest_safe_generated_version_without_content()
    {
        AgentInteractionState state = StateGenerated();

        AgentGenerationAttemptEvidence? evidence = AgentInteractionAuditInspection.GetGenerationEvidence(state, isAuthorized: true);

        evidence.ShouldNotBeNull().ShouldSatisfyAllConditions(
            e => e.AttemptId.ShouldBe(GenerationAttemptId),
            e => e.ProviderId.ShouldBe(SampleSnapshot.ProviderId),
            e => e.ModelId.ShouldBe(SampleSnapshot.ModelId),
            e => e.ProviderCapabilityVersion.ShouldBe(SampleSnapshot.ProviderCapabilityVersion),
            e => e.PromptTokenCount.ShouldBe(PromptTokenCount),
            e => e.OutputTokenCount.ShouldBe(OutputTokenCount));
        JsonSerializer.Serialize(evidence).ShouldNotContain(GeneratedContentText);
    }

    [Fact]
    public void GetPostingEvidence_traces_message_to_interaction_version_and_snapshot()
    {
        AgentInteractionState state = StateGenerated();
        state.Apply(new AgentResponsePosted(InteractionId, new AgentPostedMessageEvidence(
            PostedMessageId,
            SourceConversationId,
            AgentPartyId,
            PostedVersionId)));

        AgentPostedMessageEvidence? posting = AgentInteractionAuditInspection.GetPostingEvidence(state, isAuthorized: true);
        AgentGenerationAttemptEvidence? generation = AgentInteractionAuditInspection.GetGenerationEvidence(state, isAuthorized: true);
        AgentInteractionStatusView status = AgentInteractionAuditInspection.GetStatus(state, isAuthorized: true).View!;

        posting.ShouldNotBeNull().ShouldSatisfyAllConditions(
            p => p.MessageId.ShouldBe(PostedMessageId),
            p => p.SourceConversationId.ShouldBe(status.SourceConversationId),
            p => p.PostedVersionId.ShouldBe(PostedVersionId),
            p => p.AgentPartyId.ShouldBe(AgentPartyId));
        generation.ShouldNotBeNull().AttemptId.ShouldBe(GenerationAttemptId);
        status.ShouldSatisfyAllConditions(
            s => s.AgentInteractionId.ShouldBe(InteractionId),
            s => s.CallerPartyId.ShouldBe(CallerPartyId),
            s => s.ProviderId.ShouldBe(SampleSnapshot.ProviderId),
            s => s.ModelId.ShouldBe(SampleSnapshot.ModelId),
            s => s.ResponseMode.ShouldBe(AgentResponseMode.Automatic),
            s => s.ContentSafetyPolicyVersion.ShouldBe(ContentSafetyPolicyVersion));
    }

    [Fact]
    public void GetProposalEvidence_maps_edit_regeneration_and_approval_without_content()
    {
        AgentInteractionState state = StateProposalCreated();
        state.Apply(new ProposedAgentReplyEdited(
            InteractionId,
            SampleEditedVersion(),
            new AgentProposedReplyEditEvidence(
                SampleProposalId,
                SourceConversationId,
                EditedVersionId,
                PostedVersionId,
                EditorPartyId,
                ConfirmationSnapshot.ApproverPolicyVersion,
                ApproverPolicyValidationStatus.Valid,
                ApproverPolicyBasisDisclosure.OperatorOnly)));
        state.Apply(new ProposedAgentReplyRegenerated(
            InteractionId,
            SampleRegeneratedVersion(),
            new AgentProposedReplyRegenerationEvidence(
                SampleProposalId,
                SourceConversationId,
                RegeneratedVersionId,
                RegenerationAttemptId,
                RequesterPartyId,
                SampleSnapshot.ProviderId,
                SampleSnapshot.ModelId,
                SampleSnapshot.ProviderCapabilityVersion,
                ContentSafetyPolicyVersion,
                ConfirmationSnapshot.ApproverPolicyVersion,
                ApproverPolicyValidationStatus.Valid,
                ApproverPolicyBasisDisclosure.OperatorOnly)));
        state.Apply(new ProposedAgentReplyPosted(
            InteractionId,
            new AgentProposedReplyApprovalEvidence(
                SampleProposalId,
                SourceConversationId,
                RegeneratedVersionId,
                ActorPartyId,
                ConfirmationSnapshot.ApproverPolicyVersion,
                ApproverPolicyValidationStatus.Valid,
                ApproverPolicyBasisDisclosure.OperatorOnly,
                AgentPartyId,
                PostedMessageId,
                IdempotencyKey,
                PostedMessageId)));

        AgentProposalEditEvidenceResult edit = AgentInteractionAuditInspection.GetProposalEditEvidence(state, isAuthorized: true);
        AgentProposalRegenerationEvidenceResult regeneration = AgentInteractionAuditInspection.GetProposalRegenerationEvidence(state, isAuthorized: true);
        AgentProposalApprovalEvidenceResult approval = AgentInteractionAuditInspection.GetProposalApprovalEvidence(state, isAuthorized: true);

        edit.Evidence.ShouldNotBeNull().EditedVersionId.ShouldBe(EditedVersionId);
        regeneration.Evidence.ShouldNotBeNull().RegeneratedVersionId.ShouldBe(RegeneratedVersionId);
        approval.Evidence.ShouldNotBeNull().ShouldSatisfyAllConditions(
            a => a.ApprovedVersionId.ShouldBe(RegeneratedVersionId),
            a => a.MessageId.ShouldBe(PostedMessageId),
            a => a.PostedConversationMessageId.ShouldBe(PostedMessageId));
        string serialized = JsonSerializer.Serialize(new { edit, regeneration, approval });
        serialized.ShouldNotContain(EditedContentText);
        serialized.ShouldNotContain(RegeneratedContentText);
        serialized.ShouldNotContain(GeneratedContentText);
    }

    [Theory]
    [InlineData(false, true, AuditAvailabilityStatus.AuditUnavailable)]
    [InlineData(true, false, AuditAvailabilityStatus.AuditDelayed)]
    [InlineData(true, true, AuditAvailabilityStatus.AuditAvailable)]
    public void GetAuditAvailability_never_promotes_unavailable_or_delayed_to_available(
        bool canLoad,
        bool isFresh,
        AuditAvailabilityStatus expected)
    {
        AgentInteractionAuditInspection.GetAuditAvailability(StateGenerated(), canLoad, isFresh).ShouldBe(expected);
    }

    [Fact]
    public void GetAuditAvailability_for_requested_state_with_snapshot_is_available_and_null_is_unknown()
    {
        // A requested interaction carries the AD-4 snapshot (the linked-evidence floor), so fresh expected evidence
        // is present and the state renders AuditAvailable. A missing/never-requested state resolves to the Unknown
        // sentinel, which never promotes to available.
        AgentInteractionAuditInspection.GetAuditAvailability(StateRequested(), canLoadState: true, isFresh: true)
            .ShouldBe(AuditAvailabilityStatus.AuditAvailable);

        AgentInteractionAuditInspection.GetAuditAvailability(null, canLoadState: true, isFresh: true)
            .ShouldBe(AuditAvailabilityStatus.Unknown);
    }

    // ===== Gap: AC3 never-pending-as-success — the genuine "expected-but-not-captured-yet" branch =====

    [Theory]
    [InlineData(true, AuditAvailabilityStatus.AuditPending)]
    [InlineData(false, AuditAvailabilityStatus.AuditPending)]
    public void GetAuditAvailability_with_no_expected_evidence_is_pending_never_available_or_delayed(
        bool isFresh,
        AuditAvailabilityStatus expected)
    {
        // A requested interaction that has captured no expected evidence yet must render AuditPending — never
        // AuditAvailable when fresh, never AuditDelayed when stale (AC3 "audit pending is never displayed as success").
        var pendingState = new AgentInteractionState { IsRequested = true };

        AgentInteractionAuditInspection.GetAuditAvailability(pendingState, canLoadState: true, isFresh)
            .ShouldBe(expected);
    }

    // ===== Gap: AC2/AD-12 fail-closed — NotAuthorized and NotFound are indistinguishable, per evidence kind =====

    [Fact]
    public void GetStatus_when_missing_is_not_found_indistinguishable_from_unrequested()
    {
        AgentInteractionInspectionResult missing = AgentInteractionAuditInspection.GetStatus(null, isAuthorized: true);
        AgentInteractionInspectionResult unrequested = AgentInteractionAuditInspection.GetStatus(new AgentInteractionState(), isAuthorized: true);

        missing.Status.ShouldBe(AgentInteractionInspectionStatus.NotFound);
        missing.View.ShouldBeNull();
        unrequested.Status.ShouldBe(AgentInteractionInspectionStatus.NotFound);
        unrequested.View.ShouldBeNull();
    }

    [Fact]
    public void GetGateEvidence_when_not_authorized_returns_no_view()
    {
        AgentInteractionGateEvidenceResult result = AgentInteractionAuditInspection.GetGateEvidence(StateGenerated(), isAuthorized: false);

        result.Status.ShouldBe(AgentInteractionGateInspectionStatus.NotAuthorized);
        result.Evidence.ShouldBeNull();
    }

    [Fact]
    public void GetGateEvidence_maps_status_and_interaction_id_on_success()
    {
        AgentInteractionState state = StateGenerated();

        AgentInteractionGateEvidenceResult result = AgentInteractionAuditInspection.GetGateEvidence(state, isAuthorized: true);

        result.Status.ShouldBe(AgentInteractionGateInspectionStatus.Success);
        result.Evidence.ShouldNotBeNull().ShouldSatisfyAllConditions(
            e => e.AgentInteractionId.ShouldBe(InteractionId),
            e => e.Status.ShouldBe(state.Status),
            e => e.Verdicts.ShouldNotBeNull());
    }

    [Fact]
    public void GetContextEvidence_fails_closed_for_unauthorized_and_missing()
    {
        AgentInteractionContextEvidenceResult notAuthorized = AgentInteractionAuditInspection.GetContextEvidence(StateGenerated(), isAuthorized: false);
        AgentInteractionContextEvidenceResult missing = AgentInteractionAuditInspection.GetContextEvidence(null, isAuthorized: true);
        AgentInteractionContextEvidenceResult unrequested = AgentInteractionAuditInspection.GetContextEvidence(new AgentInteractionState(), isAuthorized: true);

        notAuthorized.Status.ShouldBe(AgentInteractionContextInspectionStatus.NotAuthorized);
        notAuthorized.Evidence.ShouldBeNull();
        missing.Status.ShouldBe(AgentInteractionContextInspectionStatus.NotFound);
        unrequested.Status.ShouldBe(AgentInteractionContextInspectionStatus.NotFound);
        unrequested.Evidence.ShouldBeNull();
    }

    [Fact]
    public void GetContextEvidence_maps_context_view_without_prompt_or_content()
    {
        AgentInteractionContextEvidenceResult result = AgentInteractionAuditInspection.GetContextEvidence(StateGenerated(), isAuthorized: true);

        result.Status.ShouldBe(AgentInteractionContextInspectionStatus.Success);
        result.Evidence.ShouldNotBeNull().AgentInteractionId.ShouldBe(InteractionId);
        string serialized = JsonSerializer.Serialize(result);
        serialized.ShouldNotContain(Prompt);
        serialized.ShouldNotContain(GeneratedContentText);
    }

    [Fact]
    public void GetGenerationEvidence_fails_closed_to_null_when_unauthorized_or_absent()
    {
        AgentInteractionAuditInspection.GetGenerationEvidence(StateGenerated(), isAuthorized: false).ShouldBeNull();
        AgentInteractionAuditInspection.GetGenerationEvidence(null, isAuthorized: true).ShouldBeNull();
        AgentInteractionAuditInspection.GetGenerationEvidence(StateRequested(), isAuthorized: true).ShouldBeNull();
    }

    [Fact]
    public void GetGenerationEvidence_returns_the_latest_generated_attempt()
    {
        AgentInteractionState state = StateGenerated();
        state.GeneratedVersions =
        [
            new AgentGeneratedVersion("v1", "attempt-old", AgentGenerationKind.Generated, GeneratedContentText, "p", "m", 1, 1, 1, 1),
            new AgentGeneratedVersion("v2", "attempt-new", AgentGenerationKind.Generated, GeneratedContentText, "p", "m", 1, 1, 1, 1),
        ];

        AgentInteractionAuditInspection.GetGenerationEvidence(state, isAuthorized: true)
            .ShouldNotBeNull().AttemptId.ShouldBe("attempt-new");
    }

    [Fact]
    public void GetPostingEvidence_fails_closed_to_null_when_unauthorized_or_absent()
    {
        AgentInteractionState posted = StateGenerated();
        posted.Apply(new AgentResponsePosted(InteractionId, new AgentPostedMessageEvidence(
            PostedMessageId, SourceConversationId, AgentPartyId, PostedVersionId)));

        AgentInteractionAuditInspection.GetPostingEvidence(posted, isAuthorized: false).ShouldBeNull();
        AgentInteractionAuditInspection.GetPostingEvidence(StateGenerated(), isAuthorized: true).ShouldBeNull();
    }

    [Fact]
    public void GetProposalEditEvidence_fails_closed_for_unauthorized_and_absent()
    {
        AgentInteractionAuditInspection.GetProposalEditEvidence(StateProposalCreated(), isAuthorized: false)
            .Status.ShouldBe(AgentInteractionInspectionStatus.NotAuthorized);
        AgentInteractionAuditInspection.GetProposalEditEvidence(StateProposalCreated(), isAuthorized: true)
            .ShouldSatisfyAllConditions(
                r => r.Status.ShouldBe(AgentInteractionInspectionStatus.NotFound),
                r => r.Evidence.ShouldBeNull());
    }

    [Fact]
    public void GetProposalRegenerationEvidence_fails_closed_for_unauthorized_and_absent()
    {
        AgentInteractionAuditInspection.GetProposalRegenerationEvidence(StateProposalCreated(), isAuthorized: false)
            .Status.ShouldBe(AgentInteractionInspectionStatus.NotAuthorized);
        AgentInteractionAuditInspection.GetProposalRegenerationEvidence(StateProposalCreated(), isAuthorized: true)
            .ShouldSatisfyAllConditions(
                r => r.Status.ShouldBe(AgentInteractionInspectionStatus.NotFound),
                r => r.Evidence.ShouldBeNull());
    }

    [Fact]
    public void GetProposalApprovalEvidence_fails_closed_for_unauthorized_and_absent()
    {
        AgentInteractionAuditInspection.GetProposalApprovalEvidence(StateProposalCreated(), isAuthorized: false)
            .Status.ShouldBe(AgentInteractionInspectionStatus.NotAuthorized);
        AgentInteractionAuditInspection.GetProposalApprovalEvidence(StateProposalCreated(), isAuthorized: true)
            .ShouldSatisfyAllConditions(
                r => r.Status.ShouldBe(AgentInteractionInspectionStatus.NotFound),
                r => r.Evidence.ShouldBeNull());
    }
}
