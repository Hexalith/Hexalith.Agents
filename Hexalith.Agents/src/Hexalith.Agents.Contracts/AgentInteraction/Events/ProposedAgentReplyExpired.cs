namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that a pending Proposed Agent Reply deterministically reached its configured expiry, moving it to the
/// <see cref="ProposedAgentReplyState.Expired"/> terminal state and preserving every prior version for audit (AC3; FR-18,
/// FR-14; AD-3, AD-5, AD-13). This is a durable success event (NOT an <c>IRejectionEvent</c>): it transitions the
/// interaction status to <see cref="AgentInteractionStatus.ProposalExpired"/>. This event <b>is</b> the AC3 Audit Evidence
/// that the proposal expired.
/// </summary>
/// <remarks>
/// <b>Content-free (AD-14):</b> a terminal transition appends nothing to the version history. The companion
/// <see cref="Evidence"/> carries safe ids + the recorded expiry timestamp only. There is no wall-clock field — the expiry
/// <em>decision</em> time is the EventStore event metadata, and the elapsed <c>ExpiresAt</c> was decided outside the
/// aggregate (AD-3). Expiry is system policy, so there is no actor/approver basis (mirrors <see cref="ProposedAgentReplyRejected"/>).
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="Evidence">The safe expiry evidence (ids + recorded expiry only; never content).</param>
public sealed record ProposedAgentReplyExpired(
    string AgentInteractionId,
    AgentProposedReplyExpiryEvidence Evidence) : IEventPayload;
