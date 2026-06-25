using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Contracts.Operations;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>
/// Pure support-safe audit read helpers over a rehydrated <see cref="AgentInteractionState"/>.
/// </summary>
public static class AgentInteractionAuditInspection
{
    /// <summary>Gets the safe status view.</summary>
    public static AgentInteractionInspectionResult GetStatus(AgentInteractionState? state, bool isAuthorized)
    {
        if (!isAuthorized)
        {
            return AgentInteractionInspectionResult.NotAuthorized();
        }

        if (state is not { IsRequested: true } current)
        {
            return AgentInteractionInspectionResult.NotFound();
        }

        return AgentInteractionInspectionResult.Success(ToStatusView(current));
    }

    /// <summary>Gets the invocation-gate evidence view.</summary>
    public static AgentInteractionGateEvidenceResult GetGateEvidence(AgentInteractionState? state, bool isAuthorized)
    {
        if (!isAuthorized)
        {
            return AgentInteractionGateEvidenceResult.NotAuthorized();
        }

        if (state is not { IsRequested: true } current)
        {
            return AgentInteractionGateEvidenceResult.NotFound();
        }

        return AgentInteractionGateEvidenceResult.Success(new AgentInteractionGateEvidenceView(
            current.AgentInteractionId,
            current.Status,
            current.GateVerdicts ?? []));
    }

    /// <summary>Gets the context evidence view.</summary>
    public static AgentInteractionContextEvidenceResult GetContextEvidence(AgentInteractionState? state, bool isAuthorized)
    {
        if (!isAuthorized)
        {
            return AgentInteractionContextEvidenceResult.NotAuthorized();
        }

        if (state is not { IsRequested: true } current)
        {
            return AgentInteractionContextEvidenceResult.NotFound();
        }

        return AgentInteractionContextEvidenceResult.Success(new AgentInteractionContextEvidenceView(
            current.AgentInteractionId,
            current.Status,
            current.ContextEvidence,
            current.ContextBlockReason));
    }

    /// <summary>Gets the latest safe generation-attempt evidence, or <see langword="null"/> when unavailable.</summary>
    public static AgentGenerationAttemptEvidence? GetGenerationEvidence(AgentInteractionState? state, bool isAuthorized)
    {
        if (!isAuthorized || state is not { IsRequested: true } current)
        {
            return null;
        }

        AgentGeneratedVersion? version = current.GeneratedVersions?.LastOrDefault(v => v.Kind == AgentGenerationKind.Generated);
        return version is null
            ? null
            : new AgentGenerationAttemptEvidence(
                version.AttemptId,
                version.ProviderId,
                version.ModelId,
                version.ProviderCapabilityVersion,
                version.PromptTokenCount,
                version.OutputTokenCount);
    }

    /// <summary>Gets safe posting evidence, or <see langword="null"/> when unavailable.</summary>
    public static AgentPostedMessageEvidence? GetPostingEvidence(AgentInteractionState? state, bool isAuthorized)
        => isAuthorized && state is { IsRequested: true } current ? current.PostingEvidence : null;

    /// <summary>Gets safe proposal-edit evidence.</summary>
    public static AgentProposalEditEvidenceResult GetProposalEditEvidence(AgentInteractionState? state, bool isAuthorized)
    {
        if (!isAuthorized)
        {
            return AgentProposalEditEvidenceResult.NotAuthorized();
        }

        if (state is not { IsRequested: true, ProposalEditEvidence: not null } current)
        {
            return AgentProposalEditEvidenceResult.NotFound();
        }

        AgentProposedReplyEditEvidence evidence = current.ProposalEditEvidence;
        return AgentProposalEditEvidenceResult.Success(new AgentProposalEditEvidenceView(
            current.AgentInteractionId,
            evidence.ProposalId,
            current.ProposalState ?? ProposedAgentReplyState.Unknown,
            evidence.EditedVersionId,
            evidence.SourceVersionId,
            evidence.EditorPartyId,
            evidence.ApproverPolicyVersion,
            evidence.PolicyBasisVerdict,
            evidence.DisclosureCategory,
            EditedAt: null));
    }

    /// <summary>Gets safe proposal-regeneration evidence.</summary>
    public static AgentProposalRegenerationEvidenceResult GetProposalRegenerationEvidence(AgentInteractionState? state, bool isAuthorized)
    {
        if (!isAuthorized)
        {
            return AgentProposalRegenerationEvidenceResult.NotAuthorized();
        }

        if (state is not { IsRequested: true, ProposalRegenerationEvidence: not null } current)
        {
            return AgentProposalRegenerationEvidenceResult.NotFound();
        }

        AgentProposedReplyRegenerationEvidence evidence = current.ProposalRegenerationEvidence;
        return AgentProposalRegenerationEvidenceResult.Success(new AgentProposalRegenerationEvidenceView(
            current.AgentInteractionId,
            evidence.ProposalId,
            current.ProposalState ?? ProposedAgentReplyState.Unknown,
            evidence.RegeneratedVersionId,
            evidence.RegenerationAttemptId,
            evidence.SourceConversationId,
            evidence.RequesterPartyId,
            evidence.ProviderId,
            evidence.ModelId,
            evidence.ProviderCapabilityVersion,
            evidence.ContentSafetyPolicyVersion,
            evidence.ApproverPolicyVersion,
            evidence.PolicyBasisVerdict,
            evidence.DisclosureCategory,
            current.ProposalRegenerationFailureReason ?? AgentProposalRegenerationFailureReason.Unknown,
            RegeneratedAt: null));
    }

    /// <summary>Gets safe proposal-approval/posting evidence.</summary>
    public static AgentProposalApprovalEvidenceResult GetProposalApprovalEvidence(AgentInteractionState? state, bool isAuthorized)
    {
        if (!isAuthorized)
        {
            return AgentProposalApprovalEvidenceResult.NotAuthorized();
        }

        if (state is not { IsRequested: true, ProposalApprovalEvidence: not null } current)
        {
            return AgentProposalApprovalEvidenceResult.NotFound();
        }

        AgentProposedReplyApprovalEvidence evidence = current.ProposalApprovalEvidence;
        return AgentProposalApprovalEvidenceResult.Success(new AgentProposalApprovalEvidenceView(
            current.AgentInteractionId,
            evidence.ProposalId,
            current.ProposalState ?? ProposedAgentReplyState.Unknown,
            evidence.ApprovedVersionId,
            evidence.ApproverPartyId,
            evidence.SourceConversationId,
            evidence.AgentPartyId,
            evidence.MessageId,
            evidence.IdempotencyKey,
            evidence.PostedConversationMessageId,
            evidence.ApproverPolicyVersion,
            evidence.PolicyBasisVerdict,
            evidence.DisclosureCategory,
            current.ProposalApprovalFailureReason ?? AgentProposalApprovalFailureReason.Unknown,
            current.ProposalPostingFailureReason ?? AgentProposalApprovalFailureReason.Unknown,
            ApprovedAt: null,
            PostedAt: null));
    }

    /// <summary>Derives the canonical audit availability display state.</summary>
    public static AuditAvailabilityStatus GetAuditAvailability(AgentInteractionState? state, bool canLoadState, bool isFresh)
    {
        if (!canLoadState)
        {
            return AuditAvailabilityStatus.AuditUnavailable;
        }

        if (state is not { IsRequested: true } current)
        {
            return AuditAvailabilityStatus.Unknown;
        }

        if (!isFresh)
        {
            return HasAnyExpectedEvidence(current)
                ? AuditAvailabilityStatus.AuditDelayed
                : AuditAvailabilityStatus.AuditPending;
        }

        return HasAnyExpectedEvidence(current)
            ? AuditAvailabilityStatus.AuditAvailable
            : AuditAvailabilityStatus.AuditPending;
    }

    private static bool HasAnyExpectedEvidence(AgentInteractionState state)
        => state.Snapshot is not null
            || state.GateVerdicts is not null
            || state.ContextEvidence is not null
            || state.GeneratedVersions is { Count: > 0 }
            || state.PostingEvidence is not null
            || state.ProposalEvidence is not null
            || state.ProposalEditEvidence is not null
            || state.ProposalRegenerationEvidence is not null
            || state.ProposalApprovalEvidence is not null
            || state.ProposalRejectionEvidence is not null
            || state.ProposalAbandonmentEvidence is not null
            || state.ProposalExpiryEvidence is not null;

    private static AgentInteractionStatusView ToStatusView(AgentInteractionState state)
        => new(
            state.AgentInteractionId,
            state.Status,
            state.AgentId,
            state.CallerPartyId,
            state.SourceConversationId,
            state.Snapshot?.ResponseMode ?? default,
            state.Snapshot?.ConfigurationVersion ?? 0,
            state.Snapshot?.InstructionsVersion ?? 0,
            state.Snapshot?.ApproverPolicyVersion ?? 0,
            state.Snapshot?.ProviderId ?? string.Empty,
            state.Snapshot?.ModelId ?? string.Empty,
            state.Snapshot?.ProviderCapabilityVersion ?? 0,
            state.Snapshot?.ContentSafetyPolicyVersion ?? 0);
}
