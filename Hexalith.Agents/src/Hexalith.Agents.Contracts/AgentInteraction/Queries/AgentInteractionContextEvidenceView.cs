namespace Hexalith.Agents.Contracts.AgentInteraction.Queries;

/// <summary>
/// Safe, audit-ready projection of an Agent Call's Conversation context evidence for authorized inspection (AC2, AC3,
/// AC4; FR-25). The <see cref="Evidence"/> and <see cref="BlockReason"/> let an authorized administrator distinguish a
/// full vs bounded vs blocked context decision without exposing anything sensitive.
/// </summary>
/// <remarks>
/// The view carries ONLY the safe identity reference, the coarse <see cref="Status"/>, the safe
/// <see cref="AgentInteractionContextEvidence"/>, and the safe <see cref="BlockReason"/> — deliberately never the
/// prompt, any Conversation-derived content/message text, raw claims, tokens, <c>PartyId</c> personal data, provider
/// payloads, an EventStore stream name, or a stack trace (AD-14). <see cref="Evidence"/> is <see langword="null"/>
/// before a context decision is recorded, and <see cref="BlockReason"/> is non-null only for a
/// <see cref="AgentInteractionStatus.ContextBlocked"/> decision.
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="Status">The coarse Agent Call status (the recorded context decision: ContextReady/ContextBlocked, or an earlier state).</param>
/// <param name="Evidence">The safe context evidence recorded for the interaction, or <see langword="null"/> before a context decision.</param>
/// <param name="BlockReason">The safe block-reason classification for a blocked context decision, or <see langword="null"/> otherwise.</param>
public record AgentInteractionContextEvidenceView(
    string AgentInteractionId,
    AgentInteractionStatus Status,
    AgentInteractionContextEvidence? Evidence,
    AgentInteractionContextBlockReason? BlockReason);
