namespace Hexalith.Agents.Contracts.ProviderCatalog.Events;

/// <summary>
/// Records that a governed provider/model catalog entry was created (AC1). Display/audit safe: carries only
/// safe capability metadata and a safe configuration reference/state — never raw credentials, provider SDK
/// options, or secret values (AD-9, AD-14). No wall-clock timestamp is carried; occurrence time is supplied by
/// the EventStore event metadata (aggregates stay pure — AD-3).
/// </summary>
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
    string? ConfigurationReferenceId) : IEventPayload;
