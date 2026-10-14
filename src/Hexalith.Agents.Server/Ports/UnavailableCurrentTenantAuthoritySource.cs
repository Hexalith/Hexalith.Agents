namespace Hexalith.Agents.Server.Ports;

/// <summary>Closed default until the host supplies authoritative contiguous source and source-head admission.</summary>
public sealed class UnavailableCurrentTenantAuthoritySource : ICurrentTenantAuthoritySource
{
    /// <inheritdoc />
    public Task<TenantAuthorityEvidence?> ReadAsync(string tenantId, bool globalAdministrators, CancellationToken cancellationToken)
        => Task.FromResult<TenantAuthorityEvidence?>(null);
}
