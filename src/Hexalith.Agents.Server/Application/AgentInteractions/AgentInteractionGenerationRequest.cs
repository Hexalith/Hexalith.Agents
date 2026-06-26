using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.Server.Application.AgentInteractions;

/// <summary>
/// Server-internal request driving the Agent-output generation orchestration (Story 2.4; AC1–AC4; AD-3, AD-9, AD-13,
/// AD-18). It carries the already-authenticated caller context plus the snapshot-recorded identity/policy references the
/// orchestration needs to re-read the Source Conversation content, read the Provider budget, invoke the provider behind
/// its adapter, and resolve + evaluate the effective Content Safety Policy. The orchestration assembles a server-trusted
/// <c>AgentOutputGenerationResult</c> and dispatches a <c>GenerateAgentOutput</c> command — any client-supplied value is
/// discarded and the reserved trust extensions are stripped from <see cref="ClientSuppliedExtensions"/>.
/// </summary>
/// <param name="MessageId">The command idempotency key (ULID), supplied by the API layer.</param>
/// <param name="CorrelationId">The correlation id for tracing, supplied by the API layer.</param>
/// <param name="TenantId">The interaction's tenant scope (the envelope scope and every read's tenant).</param>
/// <param name="AgentInteractionId">The deterministic interaction id (the generation's aggregate id; reused from Story 2.1) — the deterministic attempt id is derived from it (AD-13).</param>
/// <param name="AgentId">The snapshot-recorded Agent reference (drives the effective Content Safety Policy read).</param>
/// <param name="SourceConversationId">The snapshot-recorded source Conversation reference (opaque — drives the content re-read; AD-6).</param>
/// <param name="ProviderId">The snapshot-recorded provider reference (drives the budget read + provider invocation; AD-9).</param>
/// <param name="ModelId">The snapshot-recorded model reference (drives the budget read + provider invocation; AD-9).</param>
/// <param name="ProviderCapabilityVersion">The snapshot-recorded provider capability version (recorded on the generated version/evidence; AD-4, AD-10).</param>
/// <param name="ContentSafetyPolicyVersion">The snapshot-recorded content-safety policy version (carried for audit symmetry; the live effective version is read for the gate).</param>
/// <param name="ResponseMode">The snapshot-recorded response mode (selects the mode-specific content-safety override, if any; FR-26).</param>
/// <param name="ActorUserId">The authenticated caller (the command-envelope user and the Conversations authorized-read principal).</param>
/// <param name="ClientCorrelationId">An optional opaque client correlation reference for the caller's own tracing.</param>
/// <param name="ClientSuppliedExtensions">Any client-supplied envelope extensions to sanitize (reserved trust keys are stripped).</param>
public sealed record AgentInteractionGenerationRequest(
    string MessageId,
    string CorrelationId,
    string TenantId,
    string AgentInteractionId,
    string AgentId,
    string SourceConversationId,
    string ProviderId,
    string ModelId,
    int ProviderCapabilityVersion,
    int ContentSafetyPolicyVersion,
    AgentResponseMode ResponseMode,
    string ActorUserId,
    string? ClientCorrelationId = null,
    IReadOnlyDictionary<string, string>? ClientSuppliedExtensions = null);

/// <summary>
/// Server-internal outcome of the Agent-output generation orchestration (Story 2.4; AC1–AC4). It carries ONLY the safe
/// interaction id and the decided <see cref="AgentInteractionStatus"/> — never the generated content, an EventStore
/// stream name, provider SDK detail, raw error text, or any evidence payload (AD-9, AD-14). The status is computed by the
/// shared <c>AgentOutputGenerationPolicy</c> from the same result the command dispatched, so the reported status cannot
/// drift from the aggregate's recorded decision (AD-3).
/// </summary>
/// <param name="AgentInteractionId">The deterministic interaction id the generation evaluated.</param>
/// <param name="Status">The decided generation status — <see cref="AgentInteractionStatus.Generated"/>, <see cref="AgentInteractionStatus.GenerationFailed"/>, or <see cref="AgentInteractionStatus.SafetyFailed"/>.</param>
public sealed record AgentInteractionGenerationOutcomeResult(
    string AgentInteractionId,
    AgentInteractionStatus Status);
