using System.Collections.Generic;

using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Server-internal request driving the Conversation context-building orchestration (Story 2.3; AC1–AC4; AD-3, AD-11,
/// AD-12). It carries the already-authenticated caller context plus the snapshot-recorded identity/policy references
/// (captured on the interaction's <c>InteractionRequested</c>/snapshot) the orchestration needs to drive the content
/// read, token measurement, and budget read. The orchestration assembles a server-trusted
/// <c>AgentInteractionContextMeasurement</c> and dispatches a <c>BuildAgentInteractionContext</c> command — any
/// client-supplied measurement is discarded and the reserved trust extensions are stripped from
/// <see cref="ClientSuppliedExtensions"/>.
/// </summary>
/// <param name="MessageId">The command idempotency key (ULID), supplied by the API layer.</param>
/// <param name="CorrelationId">The correlation id for tracing, supplied by the API layer.</param>
/// <param name="TenantId">The interaction's tenant scope (the envelope scope and every read's tenant).</param>
/// <param name="AgentInteractionId">The deterministic interaction id (the context's aggregate id; reused from Story 2.1).</param>
/// <param name="SourceConversationId">The snapshot-recorded source Conversation reference (opaque — drives the content read; AD-6).</param>
/// <param name="CallerPartyId">The snapshot-recorded caller Party reference (opaque — available for participation cross-checks; AD-7).</param>
/// <param name="ProviderId">The snapshot-recorded provider reference (drives the token measurement + budget read; AD-9).</param>
/// <param name="ModelId">The snapshot-recorded model reference (drives the token measurement + budget read; AD-9).</param>
/// <param name="ProviderCapabilityVersion">The snapshot-recorded provider capability version (carried for symmetry/audit; the live budget uses the catalog's current capability version; AD-4, AD-10).</param>
/// <param name="ContextPolicyReference">The snapshot-recorded Conversation Context Policy reference (resolved to an approved bounded behavior, if any, and recorded in evidence; FR-9).</param>
/// <param name="ActorUserId">The authenticated caller (the command-envelope user and the Conversations authorized-read principal).</param>
/// <param name="ClientCorrelationId">An optional opaque client correlation reference for the caller's own tracing.</param>
/// <param name="ClientSuppliedExtensions">Any client-supplied envelope extensions to sanitize (reserved trust keys are stripped).</param>
public sealed record AgentInteractionContextRequest(
    string MessageId,
    string CorrelationId,
    string TenantId,
    string AgentInteractionId,
    string SourceConversationId,
    string CallerPartyId,
    string ProviderId,
    string ModelId,
    int ProviderCapabilityVersion,
    string ContextPolicyReference,
    string ActorUserId,
    string? ClientCorrelationId = null,
    IReadOnlyDictionary<string, string>? ClientSuppliedExtensions = null);

/// <summary>
/// Server-internal outcome of the Conversation context-building orchestration (Story 2.3; AC1–AC4). It carries ONLY the
/// safe interaction id and the decided <see cref="AgentInteractionStatus"/> — never an EventStore stream name, provider
/// SDK detail, raw error text, raw Conversation content, or any evidence payload (AD-14). The status is computed by the
/// shared <c>AgentInteractionContextPolicy</c> from the same measurement the command dispatched, so the reported status
/// cannot drift from the aggregate's recorded decision (AD-3).
/// </summary>
/// <param name="AgentInteractionId">The deterministic interaction id the context build evaluated.</param>
/// <param name="Status">The decided context status — <see cref="AgentInteractionStatus.ContextReady"/> or <see cref="AgentInteractionStatus.ContextBlocked"/>.</param>
public sealed record AgentInteractionContextOutcomeResult(
    string AgentInteractionId,
    AgentInteractionStatus Status);
