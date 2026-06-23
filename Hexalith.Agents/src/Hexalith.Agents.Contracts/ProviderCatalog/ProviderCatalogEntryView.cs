namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>
/// Safe, display/audit-ready projection of a provider/model catalog entry for authorized inspection (AC2, AC3)
/// and the future Provider catalog grid (UX provider-catalog-grid). Exposes provider/model identity, safe
/// capability metadata, lifecycle status, and a safe configuration reference/state only — never a secret value
/// or provider-SDK type (AD-9, AD-14).
/// </summary>
/// <param name="ProviderId">Stable provider identifier.</param>
/// <param name="ModelId">Stable model identifier.</param>
/// <param name="DisplayLabel">Safe admin-facing label.</param>
/// <param name="Status">Lifecycle/health status.</param>
/// <param name="SupportsTextGeneration">Required V1 capability: text-generation support.</param>
/// <param name="ContextWindowTokenLimit">Context-window token limit.</param>
/// <param name="MaxOutputTokenLimit">Max-output token limit.</param>
/// <param name="TimeoutPolicy">Safe timeout metadata.</param>
/// <param name="SafeCapabilityFlags">Allow-listed safe capability flags.</param>
/// <param name="ConfigurationState">Safe configured/not-configured state.</param>
/// <param name="ConfigurationReferenceId">Safe configuration reference identifier (never a secret value).</param>
/// <param name="IsSelectableForNewActiveUse">
/// Whether the entry may be selected for new active Agent configuration. Disabled entries are inspectable but
/// not selectable (AC2).
/// </param>
public record ProviderCatalogEntryView(
    string ProviderId,
    string ModelId,
    string DisplayLabel,
    ProviderModelStatus Status,
    bool SupportsTextGeneration,
    int ContextWindowTokenLimit,
    int MaxOutputTokenLimit,
    ProviderModelTimeoutPolicy TimeoutPolicy,
    ProviderModelCapabilityFlags SafeCapabilityFlags,
    ProviderConfigurationState ConfigurationState,
    string? ConfigurationReferenceId,
    bool IsSelectableForNewActiveUse);
