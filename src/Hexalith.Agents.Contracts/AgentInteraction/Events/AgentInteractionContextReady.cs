namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that an Agent Call (<c>AgentInteraction</c>) built its Conversation context within safe bounds — full or an
/// approved bounded behavior — and may proceed to generation (Story 2.4) (AC2, AC4; FR-9, FR-24). This is a durable
/// success event (NOT an <c>IRejectionEvent</c>): it is the safe Audit Evidence that context cleared the bounds, and it
/// transitions the interaction status to <see cref="AgentInteractionStatus.ContextReady"/>.
/// </summary>
/// <remarks>
/// The event carries only the deterministic aggregate id and the safe <see cref="AgentInteractionContextEvidence"/>
/// (numerics/enums/references only — never message text, the prompt, claims, tokens, <c>PartyId</c> personal data, or
/// provider payloads — AD-14). <see cref="AgentInteractionContextEvidence.Mode"/> distinguishes the AC2 full-context
/// case from the AC4 bounded case. There is no wall-clock field — build time is the EventStore event metadata (AD-3).
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="Evidence">The safe context evidence (mode, token counts, budget, policy reference, optional bounded behavior reference).</param>
public record AgentInteractionContextReady(
    string AgentInteractionId,
    AgentInteractionContextEvidence Evidence) : IEventPayload;
