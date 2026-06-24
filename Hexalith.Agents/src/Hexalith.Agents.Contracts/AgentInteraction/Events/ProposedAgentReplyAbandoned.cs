namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that an authorized Approver abandoned a pending Proposed Agent Reply, moving it to the
/// <see cref="ProposedAgentReplyState.Abandoned"/> terminal state and preserving every prior version for audit (AC2, AC4;
/// FR-18, FR-14; AD-5, AD-13). This is a durable success event (NOT an <c>IRejectionEvent</c>): it transitions the
/// interaction status to <see cref="AgentInteractionStatus.ProposalAbandoned"/>. This event <b>is</b> the AC2/AC4 Audit
/// Evidence that an abandonment happened.
/// </summary>
/// <remarks>
/// <b>Content-free (AD-14):</b> a terminal transition appends nothing to the version history. The companion
/// <see cref="Evidence"/> is the safe, content-free audit record (ids + policy basis only). There is no wall-clock field —
/// abandonment time is the EventStore event metadata (AD-3). Mirrors <see cref="ProposedAgentReplyRejected"/>.
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="Evidence">The safe abandonment evidence (ids + policy basis only; never content).</param>
public sealed record ProposedAgentReplyAbandoned(
    string AgentInteractionId,
    AgentProposedReplyAbandonmentEvidence Evidence) : IEventPayload;
