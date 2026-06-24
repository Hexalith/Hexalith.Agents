namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that an authorized Approver rejected a pending Proposed Agent Reply, moving it to the
/// <see cref="ProposedAgentReplyState.Rejected"/> terminal state and preserving every prior version for audit (AC1, AC4;
/// FR-18, FR-14; AD-5, AD-13). This is a durable success event (NOT an <c>IRejectionEvent</c>): it transitions the
/// interaction status to <see cref="AgentInteractionStatus.ProposalRejected"/>. This event <b>is</b> the AC1/AC4 Audit
/// Evidence that a rejection happened.
/// </summary>
/// <remarks>
/// <b>Content-free (AD-14):</b> unlike the edit event, this carries no version — a terminal transition appends nothing to
/// the version history. The companion <see cref="Evidence"/> is the safe, content-free audit record (ids + policy basis +
/// optional rationale code only). There is no wall-clock field — rejection time is the EventStore event metadata (AD-3).
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="Evidence">The safe rejection evidence (ids + policy basis + optional rationale code only; never content).</param>
public sealed record ProposedAgentReplyRejected(
    string AgentInteractionId,
    AgentProposedReplyRejectionEvidence Evidence) : IEventPayload;
