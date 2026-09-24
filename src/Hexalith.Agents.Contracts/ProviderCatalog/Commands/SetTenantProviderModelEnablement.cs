namespace Hexalith.Agents.Contracts.ProviderCatalog.Commands;

/// <summary>Platform decision to set one tenant's visibility for a governed provider/model.</summary>
public sealed record SetTenantProviderModelEnablement(
    string TenantId,
    string ProviderId,
    string ModelId,
    bool Enabled,
    int ExpectedRevision,
    ProviderDataHandlingRecord? CurrentTerms,
    string? MigratedFrom = null);
