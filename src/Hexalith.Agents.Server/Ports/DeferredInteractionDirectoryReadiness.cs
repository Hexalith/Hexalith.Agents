using Hexalith.EventStore.Contracts.Security;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Closed default; live directory migration/read binding has not been qualified.</summary>
public sealed class DeferredInteractionDirectoryReadiness : IInteractionDirectoryReadiness
{
    /// <inheritdoc />
    public Task<DirectoryEpochInstallation?> ReadAsync(string tenantId, CancellationToken ct) => Task.FromResult<DirectoryEpochInstallation?>(null);
}
