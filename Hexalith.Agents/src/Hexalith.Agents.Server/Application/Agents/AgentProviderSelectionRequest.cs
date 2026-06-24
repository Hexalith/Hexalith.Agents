using System.Collections.Generic;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Server-internal request driving the Provider/model selection orchestration (Story 1.5; AC1, AC2, AC4). It
/// carries the trusted, already-resolved <see cref="IsAgentsAdmin"/> authorization decision (from claims) and any
/// <see cref="ClientSuppliedExtensions"/> the orchestration must sanitize — the reserved <c>actor:agentsAdmin</c>
/// and <c>provider:selectionValidation</c> keys are stripped and repopulated from trusted sources only, so a client
/// can never assert <c>provider:selectionValidation=Valid</c> to bypass catalog validation.
/// </summary>
/// <param name="MessageId">The command idempotency key (ULID), supplied by the API layer.</param>
/// <param name="CorrelationId">The correlation id for tracing, supplied by the API layer.</param>
/// <param name="TenantId">The Agent's tenant scope (also the catalog scope).</param>
/// <param name="AgentId">The Agent aggregate id.</param>
/// <param name="ActorUserId">The authenticated actor.</param>
/// <param name="IsAgentsAdmin">The trusted Agents-admin decision from claims (the orchestration fails closed when false).</param>
/// <param name="ProviderId">The stable safe provider identifier the administrator selected.</param>
/// <param name="ModelId">The stable safe model identifier the administrator selected.</param>
/// <param name="ClientSuppliedExtensions">Any client-supplied envelope extensions to sanitize (reserved keys are stripped).</param>
public sealed record AgentProviderSelectionRequest(
    string MessageId,
    string CorrelationId,
    string TenantId,
    string AgentId,
    string ActorUserId,
    bool IsAgentsAdmin,
    string ProviderId,
    string ModelId,
    IReadOnlyDictionary<string, string>? ClientSuppliedExtensions = null);
