using System.Collections.Generic;

using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Server-internal request driving the Agent Call request orchestration (Story 2.1; AC1, AC2, AC3, AC4). It carries
/// the already-authenticated caller context and the caller-supplied references/prompt/idempotency the orchestration
/// turns into a deterministic, snapshot-bearing <c>RequestAgentInteraction</c>. The reserved trust extensions are
/// stripped from <see cref="ClientSuppliedExtensions"/> so a client cannot smuggle an admin/verdict onto the
/// interaction stream. Cross-aggregate authorization and dependency readiness are Story 2.2 — not done here.
/// </summary>
/// <param name="MessageId">The command idempotency key (ULID), supplied by the API layer.</param>
/// <param name="CorrelationId">The correlation id for tracing, supplied by the API layer.</param>
/// <param name="TenantId">The Agent's tenant scope (also a deterministic-id input).</param>
/// <param name="AgentId">The target Agent id the caller is invoking (a deterministic-id and snapshot-read input).</param>
/// <param name="ActorUserId">The authenticated caller (the command-envelope user).</param>
/// <param name="SourceConversationId">The source Conversation reference (opaque — AD-6).</param>
/// <param name="CallerPartyId">The caller's stable Party reference (opaque — a reference, not PII; AD-7).</param>
/// <param name="Prompt">The caller's prompt (sensitive — AD-14).</param>
/// <param name="IdempotencyKey">The caller idempotency metadata (a deterministic-id input; AD-13).</param>
/// <param name="ClientCorrelationId">An optional opaque client correlation reference for the caller's own tracing.</param>
/// <param name="ClientSuppliedExtensions">Any client-supplied envelope extensions to sanitize (reserved trust keys are stripped).</param>
public sealed record AgentInteractionRequest(
    string MessageId,
    string CorrelationId,
    string TenantId,
    string AgentId,
    string ActorUserId,
    string SourceConversationId,
    string CallerPartyId,
    string Prompt,
    string IdempotencyKey,
    string? ClientCorrelationId = null,
    IReadOnlyDictionary<string, string>? ClientSuppliedExtensions = null);

/// <summary>
/// Server-internal outcome of the Agent Call request orchestration (Story 2.1; AC2). It carries the safe
/// <see cref="AgentInteractionReference"/> returned to the caller — never an EventStore stream name, provider SDK
/// detail, or internal projection identifier — plus whether a usable snapshot was available and whether the command
/// was dispatched. The reference status is <see cref="AgentInteractionStatus.Requested"/> when a usable snapshot was
/// dispatched, otherwise <see cref="AgentInteractionStatus.Unknown"/> (the aggregate rejects the not-available case).
/// </summary>
/// <param name="Reference">The safe Agent Call status reference for the caller.</param>
/// <param name="SnapshotAvailable">Whether a populated AD-4 snapshot was read and recorded on the command.</param>
/// <param name="Dispatched">Whether the request command was dispatched (always true — a not-available snapshot is still dispatched for an auditable rejection).</param>
public sealed record AgentInteractionRequestOutcome(
    AgentInteractionReference Reference,
    bool SnapshotAvailable,
    bool Dispatched);
