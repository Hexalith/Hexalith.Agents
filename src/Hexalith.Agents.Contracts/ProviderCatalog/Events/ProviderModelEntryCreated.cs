namespace Hexalith.Agents.Contracts.ProviderCatalog.Events;

/// <summary>
/// Records that a governed provider/model catalog entry was created (AC1). Display/audit safe: carries only
/// safe capability metadata, versioned pricing, CapabilityVersion, and a safe configuration reference/state —
/// never raw credentials, provider SDK options, or secret values (AD-9, AD-14). No wall-clock timestamp is
/// carried; occurrence time is supplied by the EventStore event metadata (aggregates stay pure — AD-3).
/// </summary>
/// <param name="CatalogId">The platform entry stream identifier.</param>
/// <param name="ProviderId">The provider identifier.</param>
/// <param name="ModelId">The model identifier.</param>
/// <param name="DisplayLabel">The safe display label.</param>
/// <param name="Enabled">Whether the platform entry was initially enabled.</param>
/// <param name="SupportsTextGeneration">Whether text generation is supported.</param>
/// <param name="ContextWindowTokenLimit">The context token limit.</param>
/// <param name="MaxOutputTokenLimit">The output token limit.</param>
/// <param name="TimeoutPolicy">The safe timeout policy.</param>
/// <param name="SafeCapabilityFlags">The safe capability flags.</param>
/// <param name="ConfigurationState">The safe configured state.</param>
/// <param name="ConfigurationReferenceId">The opaque configuration reference.</param>
/// <param name="Pricing">The versioned unit pricing.</param>
/// <param name="CapabilityVersion">The initial capability version.</param>
/// <param name="DataHandling">The initial governed terms.</param>
/// <param name="MigratedFrom">The migration provenance, if imported.</param>
public record ProviderModelEntryCreated(
    string CatalogId,
    string ProviderId,
    string ModelId,
    string DisplayLabel,
    bool Enabled,
    bool SupportsTextGeneration,
    int ContextWindowTokenLimit,
    int MaxOutputTokenLimit,
    ProviderModelTimeoutPolicy TimeoutPolicy,
    ProviderModelCapabilityFlags SafeCapabilityFlags,
    ProviderConfigurationState ConfigurationState,
    string? ConfigurationReferenceId,
    ProviderModelPricing Pricing,
    int CapabilityVersion,
    ProviderDataHandlingRecord? DataHandling = null,
    string? MigratedFrom = null) : IEventPayload;
