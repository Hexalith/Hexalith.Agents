namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that an Agent Call (<c>AgentInteraction</c>) passed the invocation gate — every dependency check was
/// satisfied — and may proceed to Conversation context building (Story 2.3) (AC1; FR-20, FR-21, FR-24). This is a
/// durable success event (NOT an <c>IRejectionEvent</c>): it is the safe Audit Evidence that the gate cleared, and it
/// transitions the interaction status to <see cref="AgentInteractionStatus.Authorized"/>.
/// </summary>
/// <remarks>
/// The event carries only the deterministic aggregate id — no prompt, claims, tokens, <c>PartyId</c> personal data,
/// provider payloads, or content (AD-14). There is no wall-clock field: gate time is the EventStore event-metadata
/// timestamp, server-stamped at persist (AD-3).
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
public record AgentInteractionAuthorized(
    string AgentInteractionId) : IEventPayload;
