namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that an Agent Call (<c>AgentInteraction</c>) failed automatic posting closed AFTER successful generation,
/// durably transitioning it to a <see cref="AgentInteractionStatus.PostingFailed"/> decision with safe attempt evidence
/// (AC1, AC4; FR-11, FR-12, FR-24; AD-6, AD-7, AD-12, AD-14). This is a durable success event (NOT an
/// <c>IRejectionEvent</c>): a failed post is a <em>successfully-recorded negative decision</em> and this record IS the
/// Audit Evidence — distinct from <see cref="Rejections.AgentResponseNotPostableRejection"/>, which is used only when
/// posting cannot be evaluated at all. No Conversation Message exists (AD-6).
/// </summary>
/// <remarks>
/// <see cref="Reason"/> classifies the failure (party/membership/conversation/append) and <see cref="Evidence"/> carries
/// the safe ids that were attempted (the deterministic <c>MessageId</c>, <c>SourceConversationId</c>, <c>AgentPartyId</c>,
/// <c>PostedVersionId</c>) — NEVER the generated content, a raw Conversations payload, a Conversations-specific error
/// string, a stack trace, or a secret (AD-14). There is no wall-clock field — failure time is the EventStore event
/// metadata (AD-3). Mirrors <see cref="AgentOutputGenerationFailed"/> (with <see cref="Reason"/> + safe-id evidence).
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="Reason">The safe classification of the posting failure class.</param>
/// <param name="Evidence">The safe posting-attempt evidence (ids only; never content).</param>
public record AgentResponsePostingFailed(
    string AgentInteractionId,
    AgentResponsePostingFailureReason Reason,
    AgentPostedMessageEvidence Evidence) : IEventPayload;
