using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Server-internal request driving the Approver Policy configuration orchestration (Story 1.6 AC2, AC4). It carries
/// the trusted, already-resolved <see cref="IsAgentsAdmin"/> authorization decision (from claims) and any
/// <see cref="ClientSuppliedExtensions"/> the orchestration must sanitize. Storing the policy does NOT resolve its
/// sources (structural validation is in the aggregate; resolution is the activation concern), so no dependency
/// verdict is computed here — the orchestration carries only <c>actor:agentsAdmin</c>.
/// </summary>
/// <param name="MessageId">The command idempotency key (ULID), supplied by the API layer.</param>
/// <param name="CorrelationId">The correlation id for tracing, supplied by the API layer.</param>
/// <param name="TenantId">The Agent's tenant scope.</param>
/// <param name="AgentId">The Agent aggregate id.</param>
/// <param name="ActorUserId">The authenticated actor.</param>
/// <param name="IsAgentsAdmin">The trusted Agents-admin decision from claims (the orchestration fails closed when false).</param>
/// <param name="Policy">The safe Approver Policy value (sources + disclosure) the administrator configured.</param>
/// <param name="ClientSuppliedExtensions">Any client-supplied envelope extensions to sanitize (reserved keys are stripped).</param>
public sealed record AgentApproverPolicyRequest(
    string MessageId,
    string CorrelationId,
    string TenantId,
    string AgentId,
    string ActorUserId,
    bool IsAgentsAdmin,
    AgentApproverPolicy Policy,
    IReadOnlyDictionary<string, string>? ClientSuppliedExtensions = null);

/// <summary>
/// Server-internal outcome of the Approver Policy configuration orchestration (Story 1.6 AC2). <see cref="Authorized"/>
/// is <see langword="false"/> when the actor was not an Agents admin (fail closed before any dispatch).
/// </summary>
/// <param name="Authorized">Whether the actor passed the Agents-admin gate.</param>
/// <param name="Dispatched">Whether the configure command was dispatched.</param>
public sealed record AgentApproverPolicyOutcome(bool Authorized, bool Dispatched)
{
    /// <summary>Creates the fail-closed outcome for an unauthorized actor — nothing was dispatched.</summary>
    /// <returns>The denied outcome.</returns>
    public static AgentApproverPolicyOutcome Denied() => new(Authorized: false, Dispatched: false);

    /// <summary>Creates the outcome for an authorized request whose configure command was dispatched.</summary>
    /// <returns>The dispatched outcome.</returns>
    public static AgentApproverPolicyOutcome FromDispatch() => new(Authorized: true, Dispatched: true);
}
