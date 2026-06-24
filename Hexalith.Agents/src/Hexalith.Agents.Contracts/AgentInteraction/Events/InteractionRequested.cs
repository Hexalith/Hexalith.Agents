namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that an Agent Call (<c>AgentInteraction</c>) was requested from a Source Conversation with its AD-4
/// configuration snapshot frozen (AC1; FR-8, FR-9). This is the durable success event and the sanctioned home for
/// the sensitive <see cref="Prompt"/> (AD-14) — the prompt lives only here and on <c>AgentInteractionState</c>,
/// never on a rejection, the status view/reference, logs, telemetry, or audit summaries.
/// </summary>
/// <remarks>
/// <see cref="AgentInteractionId"/> is the deterministic aggregate id derived server-side from
/// <c>(tenant, agent, source conversation, caller, idempotency key)</c> (AD-13), so re-issuing the same call yields
/// the same id and a deterministic no-op. <see cref="CallerPartyId"/>/<see cref="SourceConversationId"/> are opaque
/// references — never Party PII or Conversation content (AD-6, AD-7, AD-14). The <see cref="Snapshot"/> freezes the
/// Agent configuration so later Agent/ProviderCatalog edits affect only future interactions (AD-4). There is no
/// wall-clock field: request time is the EventStore event-metadata timestamp, server-stamped at persist (AD-3).
/// </remarks>
/// <param name="AgentInteractionId">The deterministic Agent Call identifier (the aggregate id).</param>
/// <param name="AgentId">The target Agent identifier captured at request time.</param>
/// <param name="CallerPartyId">The caller's stable Party reference (a reference, not PII — AD-7).</param>
/// <param name="SourceConversationId">The source Conversation reference (an opaque reference — AD-6).</param>
/// <param name="Snapshot">The AD-4 configuration snapshot frozen at request time.</param>
/// <param name="Prompt">The caller's prompt (sensitive — durable here only; AD-14).</param>
/// <param name="IdempotencyKey">The caller idempotency metadata recorded for the deterministic-id derivation (AD-13).</param>
public record InteractionRequested(
    string AgentInteractionId,
    string AgentId,
    string CallerPartyId,
    string SourceConversationId,
    AgentInteractionSnapshot Snapshot,
    string Prompt,
    string IdempotencyKey) : IEventPayload;
