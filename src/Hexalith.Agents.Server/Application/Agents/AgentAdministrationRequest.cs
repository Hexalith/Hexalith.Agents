using System.Collections.Generic;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Server-internal request driving an Agent administration mutation that carries no dependency verdict
/// (Story 5.2 AC1): create, configuration update, and disable. Identity, tenant scope, and the Agents-admin
/// decision are all server-derived — never taken from the caller's payload.
/// </summary>
/// <param name="MessageId">The command idempotency key (ULID), supplied by the API layer.</param>
/// <param name="CorrelationId">The correlation id for tracing, supplied by the API layer.</param>
/// <param name="TenantId">The Agent's tenant scope.</param>
/// <param name="AgentId">The Agent aggregate id.</param>
/// <param name="ActorUserId">The authenticated actor.</param>
/// <param name="IsAgentsAdmin">The trusted Agents-admin decision from claims (the orchestration fails closed when false).</param>
/// <param name="ClientSuppliedExtensions">Any client-supplied envelope extensions to sanitize (reserved keys are stripped).</param>
public sealed record AgentAdministrationRequest(
    string MessageId,
    string CorrelationId,
    string TenantId,
    string AgentId,
    string ActorUserId,
    bool IsAgentsAdmin,
    IReadOnlyDictionary<string, string>? ClientSuppliedExtensions = null);
