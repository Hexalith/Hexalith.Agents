using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Placeholder <see cref="ITenantAccessReader"/> registered so the Server DI graph is complete and compiles cleanly,
/// while the live binding to the rehydrated Agents-local Tenants projection is deferred to the dedicated read-model
/// story (mirroring <c>DeferredAgentConfigurationSnapshotReader</c>). It is never exercised by this story's unit tests,
/// which substitute the seam.
/// </summary>
/// <remarks>
/// Like the snapshot-reader seam, this placeholder <b>fails closed by returning</b>
/// <see cref="TenantAccessReadResult.Unavailable"/>: an accidental live call yields a clean <c>Denied</c> gate
/// (tenant access is authorization-class) rather than a runtime fault (AD-12; FR-21).
/// </remarks>
public sealed class DeferredTenantAccessReader : ITenantAccessReader
{
    /// <inheritdoc />
    public Task<TenantAccessReadResult> ReadAsync(string tenantId, string actorUserId, string callerPartyId, CancellationToken ct)
        => Task.FromResult(TenantAccessReadResult.Unavailable);
}
