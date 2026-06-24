using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Server-assembled approval/posting result consumed by the pure aggregate. It carries safe ids only; selected version
/// content is never included.
/// </summary>
/// <param name="Outcome">The approval/posting outcome classification.</param>
/// <param name="ProposalId">The deterministic proposal identifier.</param>
/// <param name="SourceConversationId">The source Conversation reference.</param>
/// <param name="SelectedVersionId">The exact preserved version selected by the Approver.</param>
/// <param name="ApproverPartyId">The approving Party reference.</param>
/// <param name="ApproverPolicyVersion">The snapshotted approver-policy version.</param>
/// <param name="AuthorizationVerdict">The resolved approver-policy verdict.</param>
/// <param name="DisclosureCategory">The policy-basis disclosure class.</param>
/// <param name="AgentPartyId">The Agent Party identity used for posting, if resolved.</param>
/// <param name="MessageId">The deterministic Conversation Message id, if derivable.</param>
/// <param name="IdempotencyKey">The deterministic posting idempotency key, if derivable.</param>
/// <param name="PostedConversationMessageId">The Conversation Message id confirmed by the append operation, if posted.</param>
public sealed record AgentProposalApprovalResult(
    AgentProposalApprovalOutcome Outcome,
    string ProposalId,
    string SourceConversationId,
    string SelectedVersionId,
    string ApproverPartyId,
    int ApproverPolicyVersion,
    ApproverPolicyValidationStatus AuthorizationVerdict,
    ApproverPolicyBasisDisclosure DisclosureCategory,
    string AgentPartyId,
    string MessageId,
    string IdempotencyKey,
    string PostedConversationMessageId);
