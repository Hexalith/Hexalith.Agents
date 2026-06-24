namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that a rejection attempt on a pending Proposed Agent Reply failed closed before any side effect — the trusted
/// approver verdict was not <c>Valid</c> — durably transitioning the interaction to a
/// <see cref="AgentInteractionStatus.ProposalRejectionFailed"/> decision with safe attempt evidence; no terminal transition
/// occurs and every prior version is preserved (AC1, AC4; FR-7, FR-24; AD-5, AD-12, AD-14). This is a durable success event
/// (NOT an <c>IRejectionEvent</c>): a failed rejection is a <em>successfully-recorded negative decision</em> and this record
/// IS the Audit Evidence — distinct from <see cref="Rejections.ProposedAgentReplyNotRejectableRejection"/>, which is used
/// only when a rejection cannot be evaluated at all.
/// </summary>
/// <remarks>
/// <see cref="Reason"/> classifies the failure (e.g. <see cref="AgentProposedReplyNotRejectableReason.NotAuthorized"/>) and
/// <see cref="Evidence"/> carries the safe ids + policy basis attempted — NEVER any version content, raw
/// provider/Conversations payload, stack trace, or secret (AD-14). There is no wall-clock field — failure time is the
/// EventStore event metadata (AD-3). Mirrors <see cref="ProposedAgentReplyEditFailed"/>.
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="Reason">The safe classification of the rejection-failure class.</param>
/// <param name="Evidence">The safe rejection-attempt evidence (ids + policy basis only; never content).</param>
public sealed record ProposedAgentReplyRejectionFailed(
    string AgentInteractionId,
    AgentProposedReplyNotRejectableReason Reason,
    AgentProposedReplyRejectionEvidence Evidence) : IEventPayload;
