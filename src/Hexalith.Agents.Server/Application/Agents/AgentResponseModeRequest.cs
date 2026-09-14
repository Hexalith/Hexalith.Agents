using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Server-internal request driving the Response Mode configuration orchestration (Story 1.6 AC1). It carries the
/// trusted, already-resolved <see cref="IsAgentsAdmin"/> authorization decision (from claims) and any
/// <see cref="ClientSuppliedExtensions"/> the orchestration must sanitize — the reserved <c>actor:agentsAdmin</c>
/// (and the activation-path verdict keys) are stripped and repopulated from trusted sources only. Response-mode
/// configuration has no external dependency, so no verdict is computed here.
/// </summary>
/// <param name="MessageId">The command idempotency key (ULID), supplied by the API layer.</param>
/// <param name="CorrelationId">The correlation id for tracing, supplied by the API layer.</param>
/// <param name="TenantId">The Agent's tenant scope.</param>
/// <param name="AgentId">The Agent aggregate id.</param>
/// <param name="ActorUserId">The authenticated actor.</param>
/// <param name="IsAgentsAdmin">The trusted Agents-admin decision from claims (the orchestration fails closed when false).</param>
/// <param name="Mode">The Response Mode the administrator chose.</param>
/// <param name="ClientSuppliedExtensions">Any client-supplied envelope extensions to sanitize (reserved keys are stripped).</param>
public sealed record AgentResponseModeRequest(
    string MessageId,
    string CorrelationId,
    string TenantId,
    string AgentId,
    string ActorUserId,
    bool IsAgentsAdmin,
    AgentResponseMode Mode,
    IReadOnlyDictionary<string, string>? ClientSuppliedExtensions = null);
