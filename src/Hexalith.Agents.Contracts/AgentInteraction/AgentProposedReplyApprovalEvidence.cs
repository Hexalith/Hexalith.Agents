using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Safe, content-free audit evidence for approval and posting of one selected proposal version.
/// </summary>
/// <param name="ProposalId">The deterministic proposal identifier.</param>
/// <param name="SourceConversationId">The source Conversation reference.</param>
/// <param name="ApprovedVersionId">The exact preserved version approved for posting.</param>
/// <param name="ApproverPartyId">The approving Party reference.</param>
/// <param name="ApproverPolicyVersion">The snapshotted approver-policy version.</param>
/// <param name="PolicyBasisVerdict">The resolved approver-policy verdict.</param>
/// <param name="DisclosureCategory">The policy-basis disclosure class.</param>
/// <param name="AgentPartyId">The Agent Party identity used for posting, if resolved.</param>
/// <param name="MessageId">The deterministic Conversation Message id, if derivable.</param>
/// <param name="IdempotencyKey">The deterministic posting idempotency key, if derivable.</param>
/// <param name="PostedConversationMessageId">The Conversation Message id confirmed by the append operation, if posted.</param>
public sealed record AgentProposedReplyApprovalEvidence(
    string ProposalId,
    string SourceConversationId,
    string ApprovedVersionId,
    string ApproverPartyId,
    int ApproverPolicyVersion,
    ApproverPolicyValidationStatus PolicyBasisVerdict,
    ApproverPolicyBasisDisclosure DisclosureCategory,
    string AgentPartyId,
    string MessageId,
    string IdempotencyKey,
    string PostedConversationMessageId);
