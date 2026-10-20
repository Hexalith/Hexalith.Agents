using Hexalith.EventStore.Contracts.Security;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Exact authoritative EventStore read; absence never stands in for a projection.</summary>
public interface IInteractionDirectoryReadiness
{
    /// <summary>Reads the exact owner facts or fails closed.</summary>
    Task<DirectoryEpochInstallation?> ReadAsync(string tenantId, CancellationToken ct);
}
