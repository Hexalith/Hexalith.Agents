namespace Hexalith.Agents.Contracts.ProviderCatalog.Events;

/// <summary>
/// Records that the safe metadata of a provider/model catalog entry was updated (AC1). The enabled state is
/// unchanged by this event. Display/audit safe: no raw credentials, provider SDK options, or secret values
/// (AD-9, AD-14).
/// </summary>
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
    string? ConfigurationReferenceId) : IEventPayload;
