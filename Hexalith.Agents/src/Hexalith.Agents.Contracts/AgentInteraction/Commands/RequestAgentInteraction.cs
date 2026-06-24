namespace Hexalith.Agents.Contracts.AgentInteraction.Commands;

/// <summary>
/// Explicitly calls a governed Agent (<c>hexa</c>) from a Source Conversation with a prompt (AC1, AC3; FR-8). This
/// is the ONLY path that creates an <c>AgentInteraction</c> — there is no ambient/external trigger (AC3). The
/// caller supplies the source/caller references, the prompt, and the idempotency metadata; the server request
/// orchestration populates the trusted <see cref="AgentId"/> and the AD-4 <see cref="Snapshot"/> from a trusted
/// Agent read, overwriting any client-supplied value.
/// </summary>
/// <remarks>
/// <para>
/// The interaction's own deterministic identity is the command envelope's <c>AggregateId</c> and the tenant scope
/// is the envelope's <c>TenantId</c> — neither is taken from this payload. <see cref="AgentId"/> and
/// <see cref="Snapshot"/> are <b>server-populated trusted fields</b>: a direct-gateway client cannot forge a valid
/// snapshot, because the aggregate rejects an absent/empty snapshot with
/// <see cref="AgentInteractionRequestValidationStatus.MissingAgentSnapshot"/> (AC1 precondition). Cross-aggregate
/// authorization and dependency readiness are Story 2.2 — this command performs only the request-creation step.
/// </para>
/// <para>
/// <see cref="Prompt"/> is sensitive Conversation-derived content (AD-14): it is carried here only so the durable
/// <see cref="Events.InteractionRequested"/> event can record it, and it never appears on a rejection, the status
/// view, the status reference, logs, telemetry, or audit summaries. There is deliberately no wall-clock field —
/// request time is the EventStore event-metadata timestamp, server-stamped at persist (AD-3).
/// </para>
/// </remarks>
/// <param name="AgentId">The target Agent identifier (server-populated/trusted; the orchestration overwrites any client value).</param>
/// <param name="SourceConversationId">The source Conversation reference (opaque — not the Conversations <c>ConversationId</c> record; AD-6).</param>
/// <param name="CallerPartyId">The caller's stable Party reference (opaque — a reference, not PII; AD-7).</param>
/// <param name="Prompt">The caller's prompt (sensitive — durable event/state only; AD-14).</param>
/// <param name="IdempotencyKey">The caller idempotency metadata — also a deterministic-id input (AD-13).</param>
/// <param name="Snapshot">The AD-4 configuration snapshot (server-populated/trusted; the orchestration overwrites any client value).</param>
/// <param name="ClientCorrelationId">An optional opaque client correlation reference for the caller's own tracing.</param>
public record RequestAgentInteraction(
    string AgentId,
    string SourceConversationId,
    string CallerPartyId,
    string Prompt,
    string IdempotencyKey,
    AgentInteractionSnapshot? Snapshot,
    string? ClientCorrelationId = null);
