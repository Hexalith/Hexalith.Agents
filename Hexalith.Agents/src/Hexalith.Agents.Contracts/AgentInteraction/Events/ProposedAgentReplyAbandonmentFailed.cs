namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that an abandonment attempt on a pending Proposed Agent Reply failed closed before any side effect — the trusted
/// approver verdict was not <c>Valid</c> — durably transitioning the interaction to a
/// <see cref="AgentInteractionStatus.ProposalAbandonmentFailed"/> decision with safe attempt evidence; no terminal
/// transition occurs and every prior version is preserved (AC2, AC4; FR-7, FR-24; AD-5, AD-12, AD-14). This is a durable
/// success event (NOT an <c>IRejectionEvent</c>): a failed abandonment is a <em>successfully-recorded negative decision</em>
/// and this record IS the Audit Evidence — distinct from
/// <see cref="Rejections.ProposedAgentReplyNotAbandonableRejection"/>, which is used only when an abandonment cannot be
/// evaluated at all.
/// </summary>
/// <remarks>
/// <see cref="Reason"/> classifies the failure (e.g. <see cref="AgentProposedReplyNotAbandonableReason.NotAuthorized"/>) and
/// <see cref="Evidence"/> carries the safe ids + policy basis attempted — NEVER any version content, raw
/// provider/Conversations payload, stack trace, or secret (AD-14). There is no wall-clock field — failure time is the
/// EventStore event metadata (AD-3). Mirrors <see cref="ProposedAgentReplyRejectionFailed"/>.
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="Reason">The safe classification of the abandonment-failure class.</param>
/// <param name="Evidence">The safe abandonment-attempt evidence (ids + policy basis only; never content).</param>
public sealed record ProposedAgentReplyAbandonmentFailed(
    string AgentInteractionId,
    AgentProposedReplyNotAbandonableReason Reason,
    AgentProposedReplyAbandonmentEvidence Evidence) : IEventPayload;
