namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Safe, audit-ready evidence of one Proposed-Agent-Reply creation attempt, recorded on
/// <see cref="Events.ProposedAgentReplyCreated"/> and <see cref="Events.ProposedAgentReplyCreationFailed"/> so an authorized
/// administrator can see WHICH proposal was created (or attempted), against which Source Conversation, for which generated
/// version, under which policy snapshots, and with what optional expiry — without any generated content (AC1, AC3, AC4;
/// AD-5, AD-14). It mirrors <see cref="AgentPostedMessageEvidence"/>: safe ids only, symmetric across success + failure.
/// </summary>
/// <remarks>
/// Carries ONLY safe ids — deliberately NEVER the generated content, a raw provider/Conversations payload, a stack trace,
/// or a secret (AD-14). <see cref="ProposalId"/> is the deterministic proposal id derived from
/// <c>(AgentInteractionId, ProposedVersionId)</c> (AD-13). <see cref="ProposedVersionId"/> is the <em>id</em> of the
/// version held in the proposal, never its content (the content's sole durable home stays the Story 2.4 generated version).
/// <see cref="ExpiresAt"/> is the optional expiry metadata "where configured" (<see langword="null"/> when no expiry policy
/// is configured — AC1; the default/min/max and enforcement are a deferred product decision, Story 3.6).
/// </remarks>
/// <param name="ProposalId">The deterministic proposal identifier derived from the interaction + proposed version (AD-13).</param>
/// <param name="SourceConversationId">The source Conversation reference the proposal is linked to (an opaque reference — AD-6).</param>
/// <param name="ProposedVersionId">The selected generated version identifier held in the proposal (no content; AD-14).</param>
/// <param name="ApproverPolicyVersion">The approver-policy version snapshotted at request time (the later policy basis; Story 3.5 enforces it).</param>
/// <param name="ContentSafetyPolicyVersion">The content-safety policy version snapshotted at request time the content passed.</param>
/// <param name="ExpiresAt">The optional ISO-8601 expiry timestamp "where configured" (<see langword="null"/> when no expiry policy is configured).</param>
public record AgentProposedReplyEvidence(
    string ProposalId,
    string SourceConversationId,
    string ProposedVersionId,
    int ApproverPolicyVersion,
    int ContentSafetyPolicyVersion,
    string? ExpiresAt);
