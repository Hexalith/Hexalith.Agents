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

/// <summary>
/// Server-internal outcome of an Agent administration mutation (Story 5.2 AC1). <see cref="Authorized"/> is
/// <see langword="false"/> when the actor failed the Agents-admin gate, in which case nothing was dispatched.
/// </summary>
/// <param name="Authorized">Whether the actor passed the Agents-admin gate.</param>
/// <param name="Dispatched">Whether the command was dispatched.</param>
public sealed record AgentAdministrationOutcome(bool Authorized, bool Dispatched)
{
    /// <summary>Creates the fail-closed outcome for an unauthorized actor — nothing was dispatched.</summary>
    /// <returns>The denied outcome.</returns>
    public static AgentAdministrationOutcome Denied() => new(Authorized: false, Dispatched: false);

    /// <summary>Creates the outcome for an authorized request whose command was dispatched.</summary>
    /// <returns>The dispatched outcome.</returns>
    public static AgentAdministrationOutcome FromDispatch() => new(Authorized: true, Dispatched: true);
}
