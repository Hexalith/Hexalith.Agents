namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that an Agent Call (<c>AgentInteraction</c>) could not build its Conversation context within safe bounds,
/// durably transitioning it to a fail-closed <see cref="AgentInteractionStatus.ContextBlocked"/> decision with safe
/// evidence (AC3; FR-9, FR-10, FR-21, FR-24, FR-25; AD-11). This is a durable success event (NOT an
/// <c>IRejectionEvent</c>): a context-blocked call is a <em>successfully-recorded fail-closed decision</em> and this
/// record IS the Audit Evidence — distinct from
/// <see cref="Rejections.AgentInteractionContextNotBuildableRejection"/>, which is used only when context cannot be
/// evaluated at all. No provider call, Proposed Agent Reply, or Conversation Message is created (AC3).
/// </summary>
/// <remarks>
/// <see cref="Reason"/> classifies the failure (unavailable/not-fresh/oversized/untrustworthy-budget). The
/// <see cref="Evidence"/> still carries the measured tokens + budget + policy reference so an administrator can see WHY
/// it was blocked (e.g. <see cref="AgentInteractionContextEvidence.FullContextTokenCount"/> vs the available budget) —
/// numerics/enums/references only, never message text or any provider payload (AD-14). There is no wall-clock field —
/// block time is the EventStore event metadata (AD-3).
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="Reason">The safe classification of why context could not be built within safe bounds.</param>
/// <param name="Evidence">The safe context evidence recording the budget math behind the block.</param>
public record AgentInteractionContextBlocked(
    string AgentInteractionId,
    AgentInteractionContextBlockReason Reason,
    AgentInteractionContextEvidence Evidence) : IEventPayload;
