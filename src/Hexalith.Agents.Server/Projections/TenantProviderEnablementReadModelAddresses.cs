using Hexalith.Agents.TenantProviderEnablement;

namespace Hexalith.Agents.Server.Projections;

/// <summary>Canonical EventStore read-model address for tenant provider governance.</summary>
public static class TenantProviderEnablementReadModelAddresses
{
    /// <summary>The projection name.</summary>
    public const string ProjectionName = TenantProviderEnablementAggregate.Domain;

    /// <summary>The aggregate-owned slot.</summary>
    public const string DetailSlot = "detail";

    /// <summary>Builds a tenant's read-model key.</summary>
    public static string Detail(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return $"{ProjectionName}:{tenantId}:{ProjectionName}:{DetailSlot}:{tenantId}";
    }
}
