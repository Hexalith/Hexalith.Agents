namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>Platform-only projection of one tenant's enablement state, without decision details.</summary>
public sealed record TenantProviderEnablementInspectionResult(
    ProviderCatalogInspectionStatus Status,
    bool? Enabled,
    int? Revision,
    string? ProjectionVersion = null,
    string? LastEnablementMessageId = null);
