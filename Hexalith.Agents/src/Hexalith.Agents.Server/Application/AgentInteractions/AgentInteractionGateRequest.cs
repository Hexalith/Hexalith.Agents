using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Server-internal request driving the invocation gate orchestration (Story 2.2; AC1–AC4; AD-3, AD-12). It carries the
/// already-authenticated caller context plus the snapshot-recorded identity references (captured on the interaction's
/// <c>InteractionRequested</c>/snapshot) the orchestration needs to drive the dependency reads. The orchestration reads
/// every dependency through its port, assembles the server-trusted verdicts, and dispatches an
/// <c>EvaluateAgentInteractionGate</c> command — any client-supplied verdict is discarded and the reserved trust
/// extensions are stripped from <see cref="ClientSuppliedExtensions"/>.
/// </summary>
/// <param name="MessageId">The command idempotency key (ULID), supplied by the API layer.</param>
/// <param name="CorrelationId">The correlation id for tracing, supplied by the API layer.</param>
/// <param name="TenantId">The interaction's tenant scope (the envelope scope and every read's tenant).</param>
/// <param name="AgentInteractionId">The deterministic interaction id (the gate's aggregate id; reused from Story 2.1).</param>
/// <param name="AgentId">The snapshot-recorded target Agent id (drives the readiness read).</param>
/// <param name="ActorUserId">The authenticated caller (the command-envelope user and the tenant-access principal).</param>
/// <param name="CallerPartyId">The snapshot-recorded caller Party reference (opaque — drives tenant/party/conversation reads; AD-7).</param>
/// <param name="SourceConversationId">The snapshot-recorded source Conversation reference (opaque — drives the conversation-access read; AD-6).</param>
/// <param name="ProviderId">The snapshot-recorded provider reference (drives the provider/model readiness read; AD-9).</param>
/// <param name="ModelId">The snapshot-recorded model reference (drives the provider/model readiness read; AD-9).</param>
/// <param name="ResponseMode">The snapshot-recorded Response Mode, carried for symmetry/audit; the live ResponsePolicy check uses CURRENT readiness (AD-12).</param>
/// <param name="ClientCorrelationId">An optional opaque client correlation reference for the caller's own tracing.</param>
/// <param name="ClientSuppliedExtensions">Any client-supplied envelope extensions to sanitize (reserved trust keys are stripped).</param>
public sealed record AgentInteractionGateRequest(
    string MessageId,
    string CorrelationId,
    string TenantId,
    string AgentInteractionId,
    string AgentId,
    string ActorUserId,
    string CallerPartyId,
    string SourceConversationId,
    string ProviderId,
    string ModelId,
    AgentResponseMode ResponseMode,
    string? ClientCorrelationId = null,
    IReadOnlyDictionary<string, string>? ClientSuppliedExtensions = null);

/// <summary>
/// Server-internal outcome of the invocation gate orchestration (Story 2.2; AC1–AC4). It carries ONLY the safe
/// interaction id and the decided <see cref="AgentInteractionStatus"/> — never an EventStore stream name, provider SDK
/// detail, raw error text, or any blocker payload (AD-14). The status is computed by the shared
/// <c>AgentInvocationGatePolicy</c> from the same verdicts the command dispatched, so the reported status cannot drift
/// from the aggregate's recorded decision (AD-12).
/// </summary>
/// <param name="AgentInteractionId">The deterministic interaction id the gate evaluated.</param>
/// <param name="Status">The decided gate status — <see cref="AgentInteractionStatus.Authorized"/>, <see cref="AgentInteractionStatus.Denied"/>, or <see cref="AgentInteractionStatus.Blocked"/>.</param>
public sealed record AgentInteractionGateOutcomeResult(
    string AgentInteractionId,
    AgentInteractionStatus Status);
