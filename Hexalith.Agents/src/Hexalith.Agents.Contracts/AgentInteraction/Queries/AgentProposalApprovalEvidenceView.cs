using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>
/// Safe, audit-ready projection of proposal approval and approved-version posting evidence.
/// </summary>
/// <param name="AgentInteractionId">The AgentInteraction aggregate id.</param>
/// <param name="ProposalId">The proposal id.</param>
/// <param name="State">The proposal sub-state.</param>
/// <param name="ApprovedVersionId">The exact version approved for posting.</param>
/// <param name="ApproverPartyId">The approving Party reference.</param>
/// <param name="SourceConversationId">The source Conversation reference.</param>
/// <param name="AgentPartyId">The Agent Party identity used for posting.</param>
/// <param name="MessageId">The deterministic Conversation Message id.</param>
/// <param name="IdempotencyKey">The deterministic posting idempotency key.</param>
/// <param name="PostedConversationMessageId">The posted Conversation Message id, when posted.</param>
/// <param name="ApproverPolicyVersion">The approver-policy version.</param>
/// <param name="PolicyBasisVerdict">The resolved policy-basis verdict.</param>
/// <param name="DisclosureCategory">The policy-basis disclosure category.</param>
/// <param name="ApprovalFailureReason">The approval failure reason, or Unknown.</param>
/// <param name="PostingFailureReason">The posting failure reason, or Unknown.</param>
/// <param name="ApprovedAt">The optional approval timestamp from EventStore metadata.</param>
/// <param name="PostedAt">The optional posting timestamp from EventStore metadata.</param>
public sealed record AgentProposalApprovalEvidenceView(
    string AgentInteractionId,
    string ProposalId,
    ProposedAgentReplyState State,
    string ApprovedVersionId,
    string ApproverPartyId,
    string SourceConversationId,
    string AgentPartyId,
    string MessageId,
    string IdempotencyKey,
    string PostedConversationMessageId,
    int ApproverPolicyVersion,
    ApproverPolicyValidationStatus PolicyBasisVerdict,
    ApproverPolicyBasisDisclosure DisclosureCategory,
    AgentProposalApprovalFailureReason ApprovalFailureReason,
    AgentProposalApprovalFailureReason PostingFailureReason,
    string? ApprovedAt,
    string? PostedAt);
