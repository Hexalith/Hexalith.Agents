namespace Hexalith.Agents.Server.Ports;

/// <summary>Technical-host adapter for current Tenants source evidence; old projections are insufficient.</summary>
public interface ICurrentTenantAuthoritySource
{
    /// <summary>Reads complete current exact-source evidence without looking up a claimed resource.</summary>
    Task<TenantAuthorityEvidence?> ReadAsync(string tenantId, bool globalAdministrators, CancellationToken cancellationToken);
}
