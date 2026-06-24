namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Safe, audit-ready evidence of one Proposed-Agent-Reply expiry transition, recorded on
/// <see cref="Events.ProposedAgentReplyExpired"/> so an authorized administrator can see WHICH proposal expired, against
/// which Source Conversation, and at what configured expiry — without any version content (AC3; FR-24; AD-5, AD-14). Expiry
/// is system policy, so there is no actor Party or approver policy basis (unlike rejection/abandonment): the recorded
/// <see cref="ExpiresAt"/> is the sole authority.
/// </summary>
/// <remarks>
/// Carries ONLY safe ids and the recorded expiry timestamp — deliberately NEVER any generated/edited content, a raw payload,
/// a stack trace, or a secret (AD-14). The expiry <em>decision</em> timestamp is the EventStore event metadata (AD-3 — no
/// wall-clock field); <see cref="ExpiresAt"/> is the recorded policy expiry that elapsed, not a clock read in the aggregate.
/// </remarks>
/// <param name="ProposalId">The deterministic proposal identifier that expired (AD-13).</param>
/// <param name="SourceConversationId">The source Conversation reference the proposal is linked to (an opaque reference — AD-6).</param>
/// <param name="ExpiresAt">The recorded ISO-8601 expiry timestamp that elapsed (the deterministic expiry authority).</param>
public record AgentProposedReplyExpiryEvidence(
    string ProposalId,
    string SourceConversationId,
    string? ExpiresAt);
