using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;

using Shouldly;

using static Hexalith.Agents.Tests.AgentInteractionTestData;

namespace Hexalith.Agents.Tests;

/// <summary>
/// Task 6 / AD-17 "audit completeness for every posted response" — the Confirmation-mode counterpart to the
/// Automatic-mode posting trace. It drives the WHOLE Confirmation command chain
/// (request → gate → context → generate → propose → approve/post) through the real reflection-dispatch pipeline, then
/// proves the new safe audit read surface lets an authorized reader trace the posted Conversation Message back to its
/// source interaction: <c>MessageId</c> ← <c>AgentInteractionId</c> + approved <c>VersionId</c> (AD-13) ←
/// <c>AgentInteractionSnapshot</c> (caller, provider/model, response mode, content-safety/context policy versions, AD-4),
/// while no prompt/generated/edited content is ever emitted (AD-14).
/// </summary>
public sealed class AgentInteractionAuditTraceabilityE2ETests
{
    private const string ApproverPartyId = "approver-party-001";
    private const string ApprovalMessageId = "approval-message-001";

    private static RequestAgentInteraction ConfirmationRequest { get; } = new(
        AgentId,
        SourceConversationId,
        CallerPartyId,
        Prompt,
        IdempotencyKey,
        ConfirmationSnapshot,
        ClientCorrelationId);

    [Fact]
    public async Task A_posted_confirmation_response_is_fully_traceable_through_the_audit_read_surface()
    {
        var aggregate = new AgentInteractionAggregate();
        var state = new AgentInteractionState();

        await ProcessAndApplyAsync(aggregate, state, ConfirmationRequest);
        await ProcessAndApplyAsync(aggregate, state, GateCommand(AllSatisfied()));
        await ProcessAndApplyAsync(aggregate, state, ContextCommand(FullFitsMeasurement()));
        await ProcessAndApplyAsync(aggregate, state, GenerateCommand(SucceededGenerationResult()));
        await ProcessAndApplyAsync(aggregate, state, ProposalCommand(CreatedProposalResult()));
        await ProcessAndApplyAsync(aggregate, state, ApproveCommand());
        state.Status.ShouldBe(AgentInteractionStatus.ProposalPosted);

        // Query the safe audit surface the way an authorized reader would.
        AgentInteractionStatusView status = AgentInteractionAuditInspection.GetStatus(state, isAuthorized: true).View
            .ShouldNotBeNull();
        AgentProposalApprovalEvidenceView approval = AgentInteractionAuditInspection.GetProposalApprovalEvidence(state, isAuthorized: true).Evidence
            .ShouldNotBeNull();
        AgentGenerationAttemptEvidence generation = AgentInteractionAuditInspection.GetGenerationEvidence(state, isAuthorized: true)
            .ShouldNotBeNull();

        // Linked, complete evidence (AC1): the posted message ties to the interaction, the approved version, and the snapshot.
        status.ShouldSatisfyAllConditions(
            s => s.AgentInteractionId.ShouldBe(InteractionId),
            s => s.CallerPartyId.ShouldBe(CallerPartyId),
            s => s.SourceConversationId.ShouldBe(SourceConversationId),
            s => s.ResponseMode.ShouldBe(AgentResponseMode.Confirmation),
            s => s.ProviderId.ShouldBe(ConfirmationSnapshot.ProviderId),
            s => s.ModelId.ShouldBe(ConfirmationSnapshot.ModelId),
            s => s.ContentSafetyPolicyVersion.ShouldBe(ConfirmationSnapshot.ContentSafetyPolicyVersion));

        approval.ShouldSatisfyAllConditions(
            a => a.AgentInteractionId.ShouldBe(status.AgentInteractionId),
            a => a.MessageId.ShouldBe(ApprovalMessageId),
            a => a.PostedConversationMessageId.ShouldBe(ApprovalMessageId),
            a => a.SourceConversationId.ShouldBe(status.SourceConversationId),
            a => a.AgentPartyId.ShouldBe(AgentPartyId),
            a => a.ApproverPartyId.ShouldBe(ApproverPartyId),
            a => a.IdempotencyKey.ShouldBe(IdempotencyKey));

        // AD-13 chain: the approved version is exactly one of the preserved, immutable source generated versions.
        state.GeneratedVersions.ShouldNotBeNull()
            .Select(v => v.VersionId).ShouldContain(approval.ApprovedVersionId);
        generation.AttemptId.ShouldBe(GenerationAttemptId);

        // AD-14: the full aggregate audit surface never carries prompt, generated, or edited content.
        string serialized = JsonSerializer.Serialize(new { status, approval, generation });
        serialized.ShouldNotContain(Prompt);
        serialized.ShouldNotContain(GeneratedContentText);
        serialized.ShouldNotContain(EditedContentText);
        serialized.ShouldNotContain(RegeneratedContentText);
    }

    private static ApproveProposedAgentReply ApproveCommand()
        => new(
            InteractionId,
            new AgentProposalApprovalResult(
                AgentProposalApprovalOutcome.Posted,
                SampleProposalId,
                SourceConversationId,
                PostedVersionId,
                ApproverPartyId,
                ConfirmationSnapshot.ApproverPolicyVersion,
                ApproverPolicyValidationStatus.Valid,
                ApproverPolicyBasisDisclosure.UserVisible,
                AgentPartyId,
                ApprovalMessageId,
                IdempotencyKey,
                ApprovalMessageId));
}
