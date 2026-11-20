using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Identifies the authenticated subject, resource, and exact disclosure being authorized.</summary>
public sealed record AgentAuditReadTarget(string TenantId, string Subject, string InteractionId, string QueryType);

/// <summary>The FR-24/33 authority established by a trusted host for one read.</summary>
public enum AgentAuditAudience
{
    /// <summary>No authority established.</summary>
    Unknown,
    /// <summary>Current tenant administrator, or release/platform operator authorized for this tenant.</summary>
    TenantOperations,
    /// <summary>Current human Party inspecting the outcome of its own call.</summary>
    Caller,
    /// <summary>Current Participant with read access to the source Conversation.</summary>
    Participant,
    /// <summary>Party resolved as eligible for the specified proposal.</summary>
    EligibleApprover,
    /// <summary>Inspector with a governed case authorizing this specific resource and disclosure.</summary>
    ComplianceInspector,
}

/// <summary>Owner-verified scope and exclusive lifetime. Never take grant fields from request payloads.</summary>
public sealed record AgentInteractionAuditGrant(AgentAuditReadTarget Target, AgentAuditAudience Audience,
    AgentAuthorityValidity Validity, string? PartyId = null, string? ConversationId = null, string? ProposalId = null, string? InspectionCaseId = null)
{
    internal static bool IsOperationalQuery(string queryType) => queryType is GetAgentInteractionStatusQuery.QueryType
        or GetAgentInteractionGateEvidenceQuery.QueryType or GetAgentGenerationEvidenceQuery.QueryType
        or GetAgentAuditAvailabilityQuery.QueryType;

    internal bool Authorizes(AgentAuditReadTarget requested) => Target == requested && Validity?.IsCurrent == true
        && (Audience switch
        {
            AgentAuditAudience.TenantOperations => IsOperationalQuery(requested.QueryType),
            AgentAuditAudience.Caller => requested.QueryType == GetAgentInteractionStatusQuery.QueryType
                && !string.IsNullOrWhiteSpace(PartyId),
            AgentAuditAudience.Participant => requested.QueryType == GetAgentPostingEvidenceQuery.QueryType
                && !string.IsNullOrWhiteSpace(ConversationId),
            AgentAuditAudience.EligibleApprover or AgentAuditAudience.ComplianceInspector =>
                (requested.QueryType is GetAgentInteractionContextEvidenceQuery.QueryType or GetAgentProposalEditEvidenceQuery.QueryType
                    or GetAgentProposalRegenerationEvidenceQuery.QueryType or GetAgentProposalApprovalEvidenceQuery.QueryType)
                && !string.IsNullOrWhiteSpace(ConversationId)
                && (Audience == AgentAuditAudience.ComplianceInspector
                    ? !string.IsNullOrWhiteSpace(InspectionCaseId)
                    : !string.IsNullOrWhiteSpace(PartyId) && !string.IsNullOrWhiteSpace(ProposalId)),
            _ => false,
        });

    internal bool Includes(AgentInteractionState state) => Audience switch
    {
        AgentAuditAudience.TenantOperations => true,
        AgentAuditAudience.Caller => state.CallerPartyId == PartyId,
        AgentAuditAudience.Participant => state.SourceConversationId == ConversationId
            && (state.PostingEvidence is null || state.PostingEvidence.SourceConversationId == ConversationId),
        AgentAuditAudience.EligibleApprover or AgentAuditAudience.ComplianceInspector =>
            state.SourceConversationId == ConversationId
            && (Audience != AgentAuditAudience.EligibleApprover || state.CallerPartyId != PartyId)
            && (ProposalId is null || state.ProposalEvidence is { } proposal
                && proposal.ProposalId == ProposalId && proposal.SourceConversationId == ConversationId)
            && IncludesProposalEvidence(state),
        _ => false,
    };

    private bool IncludesProposalEvidence(AgentInteractionState state)
    {
        // The row rendered by the handler must also belong to the authorized conversation and proposal.
        (string Proposal, string Conversation)? resource = Target.QueryType switch
        {
            GetAgentProposalEditEvidenceQuery.QueryType when state.ProposalEditEvidence is { } edit
                => (edit.ProposalId, edit.SourceConversationId),
            GetAgentProposalRegenerationEvidenceQuery.QueryType when state.ProposalRegenerationEvidence is { } regeneration
                => (regeneration.ProposalId, regeneration.SourceConversationId),
            GetAgentProposalApprovalEvidenceQuery.QueryType when state.ProposalApprovalEvidence is { } approval
                => (approval.ProposalId, approval.SourceConversationId),
            _ => null,
        };
        return resource is null || resource.Value.Conversation == ConversationId
            && (ProposalId is null || resource.Value.Proposal == ProposalId);
    }
}

/// <summary>
/// Host binding for current, operation-specific FR-24/33 authority. Verify ownership, participation, proposal
/// eligibility, or a justified inspection with required oversight against authoritative sources before issuing a
/// grant. Bound the grant to the earliest deadline of those checks. Return null when any proof is unavailable.
/// </summary>
public interface IAgentInteractionAuditAuthority
{
    /// <summary>Resolves authority for this exact read without disclosing state to the caller.</summary>
    Task<AgentInteractionAuditGrant?> AuthorizeAsync(AgentAuditReadTarget target, CancellationToken cancellationToken);
}
