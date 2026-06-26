namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that an Agent Call (<c>AgentInteraction</c>) failed generation closed, durably transitioning it to a
/// <see cref="AgentInteractionStatus.GenerationFailed"/> or <see cref="AgentInteractionStatus.SafetyFailed"/> decision
/// with safe attempt evidence (AC2, AC3; FR-10, FR-12, FR-24; AD-5, AD-9, AD-12, AD-14). This is a durable success event
/// (NOT an <c>IRejectionEvent</c>): a failed generation is a <em>successfully-recorded negative decision</em> and this
/// record IS the Audit Evidence — distinct from
/// <see cref="Rejections.AgentOutputNotGeneratableRejection"/>, which is used only when generation cannot be evaluated at
/// all. No provider call result is persisted as a version, no Conversation Message, and no approvable proposal is created
/// (AD-5).
/// </summary>
/// <remarks>
/// <see cref="Decision"/> is <see cref="AgentInteractionStatus.SafetyFailed"/> (the content was blocked by Content Safety
/// Policy) or <see cref="AgentInteractionStatus.GenerationFailed"/> (every other failure class). <see cref="Reason"/>
/// classifies the failure and <see cref="Evidence"/> carries safe attempt metadata only — NEVER the generated/failed
/// content, a raw provider payload, a provider-specific error string, a stack trace, or a secret (AD-9, AD-14). On a safety
/// failure the unsafe content is never attached (default <c>MetadataOnly</c> audit treatment). There is no wall-clock
/// field — failure time is the EventStore event metadata (AD-3).
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="Decision">The recorded failure decision — <see cref="AgentInteractionStatus.GenerationFailed"/> or <see cref="AgentInteractionStatus.SafetyFailed"/>.</param>
/// <param name="Reason">The safe classification of the failure class.</param>
/// <param name="Evidence">The safe generation-attempt evidence (ids + token usage; never unsafe content).</param>
public record AgentOutputGenerationFailed(
    string AgentInteractionId,
    AgentInteractionStatus Decision,
    AgentOutputGenerationFailureReason Reason,
    AgentGenerationAttemptEvidence Evidence) : IEventPayload;
