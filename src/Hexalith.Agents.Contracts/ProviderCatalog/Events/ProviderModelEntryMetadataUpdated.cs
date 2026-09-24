namespace Hexalith.Agents.Contracts.ProviderCatalog.Events;

/// <summary>
/// Records that the safe metadata or pricing of a provider/model catalog entry was updated (AC1). The enabled
/// state is unchanged by this event. Display/audit safe: no raw credentials, provider SDK options, or secret
/// values (AD-9, AD-14).
/// </summary>
/// <param name="CatalogId">The platform entry stream identifier.</param>
/// <param name="ProviderId">The provider identifier.</param>
/// <param name="ModelId">The model identifier.</param>
/// <param name="DisplayLabel">The safe display label.</param>
/// <param name="SupportsTextGeneration">Whether text generation is supported.</param>
/// <param name="ContextWindowTokenLimit">The context token limit.</param>
/// <param name="MaxOutputTokenLimit">The output token limit.</param>
/// <param name="TimeoutPolicy">The safe timeout policy.</param>
/// <param name="SafeCapabilityFlags">The safe capability flags.</param>
/// <param name="ConfigurationState">The safe configured state.</param>
/// <param name="ConfigurationReferenceId">The opaque configuration reference.</param>
/// <param name="Pricing">The versioned unit pricing.</param>
/// <param name="CapabilityVersion">The next capability version.</param>
/// <param name="DataHandling">The current governed terms.</param>
public record ProviderModelEntryMetadataUpdated(
    string CatalogId,
    string ProviderId,
    string ModelId,
    string DisplayLabel,
    bool SupportsTextGeneration,
    int ContextWindowTokenLimit,
    int MaxOutputTokenLimit,
    ProviderModelTimeoutPolicy TimeoutPolicy,
    ProviderModelCapabilityFlags SafeCapabilityFlags,
    ProviderConfigurationState ConfigurationState,
    string? ConfigurationReferenceId,
    ProviderModelPricing Pricing,
    int CapabilityVersion,
    ProviderDataHandlingRecord? DataHandling = null) : IEventPayload;
