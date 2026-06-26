namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that a regeneration attempt on a pending Proposed Agent Reply failed closed, durably transitioning the
/// interaction to a <see cref="AgentInteractionStatus.ProposalRegenerationFailed"/> decision with safe attempt evidence — no
/// new version is created, every prior version is preserved, and the proposal remains retryable while its sub-state stays
/// pending/edited/regenerated (AC3, AC4; FR-16, FR-24; AD-5, AD-12, AD-14). This is a durable success event (NOT an
/// <c>IRejectionEvent</c>): a failed regeneration is a <em>successfully-recorded negative decision</em> and this record IS
/// the Audit Evidence — distinct from <see cref="Rejections.ProposedAgentReplyNotRegeneratableRejection"/>, which is used
/// only when a regeneration cannot be evaluated at all (e.g. a terminal proposal, where no provider invocation occurs).
/// </summary>
/// <remarks>
/// <see cref="Reason"/> classifies the failure (not-authorized / provider / timeout / safety / policy) and
/// <see cref="Evidence"/> carries the safe ids + provider/model/policy versions + policy basis attempted — NEVER the
/// regenerated content, a prompt, a raw provider/Conversations payload, a stack trace, or a secret (AD-9, AD-14). There is
/// no wall-clock field — failure time is the EventStore event metadata (AD-3). Mirrors <see cref="ProposedAgentReplyEditFailed"/>.
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="Reason">The safe classification of the regeneration-failure class.</param>
/// <param name="Evidence">The safe regeneration-attempt evidence (ids + provider/model/policy versions + policy basis only; never content).</param>
public record ProposedAgentReplyRegenerationFailed(
    string AgentInteractionId,
    AgentProposalRegenerationFailureReason Reason,
    AgentProposedReplyRegenerationEvidence Evidence) : IEventPayload;
