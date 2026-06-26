using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Server-internal request for approving one selected proposal version and posting it as the Agent Party.
/// </summary>
/// <param name="MessageId">The EventStore command id.</param>
/// <param name="CorrelationId">The correlation id.</param>
/// <param name="TenantId">The tenant scope.</param>
/// <param name="AgentInteractionId">The AgentInteraction aggregate id.</param>
/// <param name="ProposalId">The proposal id.</param>
/// <param name="ProposalState">The trusted current proposal sub-state.</param>
/// <param name="AgentId">The Agent id used to resolve the Agent Party identity.</param>
/// <param name="SourceConversationId">The source Conversation id.</param>
/// <param name="SelectedVersionId">The exact selected version id to approve and post.</param>
/// <param name="ApproverPartyId">The approving Party id.</param>
/// <param name="ApproverPolicy">The snapshotted approver policy.</param>
/// <param name="ApproverPolicyVersion">The snapshotted approver-policy version.</param>
/// <param name="ActorUserId">The authenticated actor user id.</param>
/// <param name="ClientCorrelationId">Optional client correlation id.</param>
/// <param name="ClientSuppliedExtensions">Optional client-supplied extensions to sanitize.</param>
public sealed record AgentInteractionProposalApprovalRequest(
    string MessageId,
    string CorrelationId,
    string TenantId,
    string AgentInteractionId,
    string ProposalId,
    ProposedAgentReplyState ProposalState,
    string AgentId,
    string SourceConversationId,
    string SelectedVersionId,
    string ApproverPartyId,
    AgentApproverPolicy? ApproverPolicy,
    int ApproverPolicyVersion,
    string ActorUserId,
    string? ClientCorrelationId = null,
    IReadOnlyDictionary<string, string>? ClientSuppliedExtensions = null);

/// <summary>
/// Safe server outcome of a proposal approval attempt.
/// </summary>
/// <param name="SelectedVersionId">The selected version id.</param>
/// <param name="MessageId">The deterministic Conversation Message id, if derived.</param>
/// <param name="Status">The decided status.</param>
/// <param name="NotApprovableReason">The no-dispatch structural denial reason, when applicable.</param>
public sealed record AgentInteractionProposalApprovalOutcomeResult(
    string SelectedVersionId,
    string MessageId,
    AgentInteractionStatus Status,
    AgentProposedReplyNotApprovableReason NotApprovableReason = AgentProposedReplyNotApprovableReason.Unknown);
