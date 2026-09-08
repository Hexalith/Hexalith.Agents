using System;

namespace Hexalith.Agents.Server.Projections;

/// <summary>
/// Canonical addresses for the provider-catalog read model (Story 5.3 AC2). The tenant is part of every key, so a
/// read for one tenant can never resolve another tenant's catalog.
/// </summary>
public static class ProviderCatalogReadModelAddresses
{
    /// <summary>The kebab-case domain that owns the projection.</summary>
    public const string Domain = "provider-catalog";

    /// <summary>The projection name declared to the platform slot registry.</summary>
    public const string ProjectionName = "provider-catalog";

    /// <summary>The single aggregate-owned slot the projection writes.</summary>
    public const string DetailSlot = "detail";

    /// <summary>Builds the tenant-scoped key holding one catalog read model.</summary>
    /// <param name="tenantId">The tenant scope (also the catalog aggregate id).</param>
    /// <returns>The state key.</returns>
    public static string Detail(string tenantId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        return $"{Domain}:{tenantId}:{ProjectionName}:{DetailSlot}:{tenantId}";
    }
}
