using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal port the invocation gate orchestration uses to read the caller's tenant access from Agents' own
/// local Tenants projection (Story 2.2; AC1, AC3; AD-3, AD-12). Per AD-12 Agents owns its tenant-access decision — the
/// implementation MUST NOT depend on Conversations' server-internal <c>IConversationTenantAccessService</c>; it maps
/// the projection state (tenant role, tenant status) to a safe <see cref="TenantAccessReadResult"/> and never surfaces
/// a raw claim, token, or cross-tenant record across this boundary (AC3; AD-14).
/// </summary>
/// <remarks>
/// Keeping this a port (rather than reading the projection inline) preserves the AD-3 round-trip: the pure aggregate
/// receives the resulting verdict through the trusted gate command, never by reading the projection itself. The live
/// binding to the rehydrated Tenants projection is deferred (mirroring Story 1.2) so the gate decision logic stays
/// fully unit-testable here; the deferred reader fails closed by returning <see cref="TenantAccessReadResult.Unavailable"/>.
/// </remarks>
public interface ITenantAccessReader
{
    /// <summary>Reads the caller's tenant access and maps it to a fail-closed gate result (AC1, AC3).</summary>
    /// <param name="tenantId">The Agent's tenant scope (cross-tenant reads fail closed, never revealing existence — AC3).</param>
    /// <param name="actorUserId">The authenticated caller principal whose tenant access is checked.</param>
    /// <param name="callerPartyId">The caller's stable Party reference (an opaque reference, not PII — AD-7).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe tenant-access read result (outcome + freshness).</returns>
    Task<TenantAccessReadResult> ReadAsync(string tenantId, string actorUserId, string callerPartyId, CancellationToken ct);
}
