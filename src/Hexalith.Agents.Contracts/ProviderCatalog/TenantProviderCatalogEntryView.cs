namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>A provider entry visible to an enabled tenant, without configuration references or state.</summary>
/// <param name="ProviderId">The provider identifier.</param>
/// <param name="ModelId">The model identifier.</param>
/// <param name="DisplayLabel">The display label.</param>
/// <param name="PlatformEnabled">Whether the platform has enabled the entry.</param>
/// <param name="TenantEnabled">Whether the tenant has been enabled by the platform.</param>
/// <param name="SupportsTextGeneration">Whether text generation is supported.</param>
/// <param name="ContextWindowTokenLimit">The context limit.</param>
/// <param name="MaxOutputTokenLimit">The output limit.</param>
/// <param name="TimeoutPolicy">The timeout policy.</param>
/// <param name="SafeCapabilityFlags">The safe capability flags.</param>
/// <param name="CapabilityVersion">The capability version.</param>
/// <param name="Pricing">The governed pricing.</param>
/// <param name="DataHandling">The governed data handling terms.</param>
/// <param name="IsSelectableForNewActiveUse">Whether selection is currently permitted.</param>
/// <param name="LastDecisionMessageId">The last projected terms decision command identity, for exact write confirmation.</param>
public sealed record TenantProviderCatalogEntryView(
    string ProviderId,
    string ModelId,
    string DisplayLabel,
    bool PlatformEnabled,
    bool TenantEnabled,
    bool SupportsTextGeneration,
    int ContextWindowTokenLimit,
    int MaxOutputTokenLimit,
    ProviderModelTimeoutPolicy TimeoutPolicy,
    ProviderModelCapabilityFlags SafeCapabilityFlags,
    int CapabilityVersion,
    ProviderModelPricing? Pricing,
    ProviderDataHandlingRecord? DataHandling,
    bool IsSelectableForNewActiveUse,
    string DataHandlingStatus,
    DateTimeOffset? GraceExpiresAt,
    int? InForceDataHandlingVersion,
    int TenantRevision,
    string? LastDecisionMessageId = null);
