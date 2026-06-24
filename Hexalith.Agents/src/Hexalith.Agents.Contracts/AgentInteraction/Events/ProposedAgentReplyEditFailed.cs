namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that an edit attempt on a pending Proposed Agent Reply failed closed, durably transitioning the interaction to
/// a <see cref="AgentInteractionStatus.ProposalEditFailed"/> decision with safe attempt evidence — no new version is
/// created and every prior version is preserved (AC2, AC4; FR-15, FR-24; AD-5, AD-12, AD-14). This is a durable success
/// event (NOT an <c>IRejectionEvent</c>): a failed edit is a <em>successfully-recorded negative decision</em> and this
/// record IS the Audit Evidence — distinct from <see cref="Rejections.ProposedAgentReplyNotEditableRejection"/>, which is
/// used only when an edit cannot be evaluated at all.
/// </summary>
/// <remarks>
/// <see cref="Reason"/> classifies the failure (not-authorized / adapter) and <see cref="Evidence"/> carries the safe ids
/// + policy basis attempted — NEVER the edited content, a raw provider/Conversations payload, a stack trace, or a secret
/// (AD-14). There is no wall-clock field — failure time is the EventStore event metadata (AD-3). Mirrors
/// <see cref="ProposedAgentReplyCreationFailed"/>.
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="Reason">The safe classification of the edit-failure class.</param>
/// <param name="Evidence">The safe edit-attempt evidence (ids + policy basis only; never content).</param>
public record ProposedAgentReplyEditFailed(
    string AgentInteractionId,
    AgentProposalEditFailureReason Reason,
    AgentProposedReplyEditEvidence Evidence) : IEventPayload;
