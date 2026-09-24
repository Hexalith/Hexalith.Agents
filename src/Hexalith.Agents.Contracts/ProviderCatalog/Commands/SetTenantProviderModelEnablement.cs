namespace Hexalith.Agents.Contracts.ProviderCatalog.Commands;

/// <summary>Platform decision to set one tenant's visibility for a governed provider/model.</summary>
/// <param name="TenantId">The target tenant.</param>
/// <param name="ProviderId">The provider identifier.</param>
/// <param name="ModelId">The model identifier.</param>
/// <param name="Enabled">Whether the tenant can see this entry.</param>
/// <param name="ExpectedRevision">The current tenant stream revision.</param>
/// <param name="CurrentTerms">The authoritative platform terms when enabling.</param>
/// <param name="MigratedFrom">Migration-only provenance.</param>
public sealed record SetTenantProviderModelEnablement(
    string TenantId,
    string ProviderId,
    string ModelId,
    bool Enabled,
    int ExpectedRevision,
    ProviderDataHandlingRecord? CurrentTerms,
    string? MigratedFrom = null);
