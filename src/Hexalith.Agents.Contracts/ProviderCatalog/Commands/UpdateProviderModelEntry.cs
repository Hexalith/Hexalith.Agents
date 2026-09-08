namespace Hexalith.Agents.Contracts.ProviderCatalog.Commands;

/// <summary>
/// Updates the safe metadata and pricing of an existing provider/model catalog entry (AC1). Does not change the
/// enabled state (use <see cref="EnableProviderModelEntry"/> / <see cref="DisableProviderModelEntry"/>).
/// Carries only safe metadata — never raw credentials or provider SDK options (AD-9, AD-14).
/// </summary>
/// <param name="ProviderId">Stable provider identifier (non-empty).</param>
/// <param name="ModelId">Stable model identifier (non-empty).</param>
/// <param name="DisplayLabel">Safe admin-facing label.</param>
/// <param name="SupportsTextGeneration">Required V1 capability: text-generation support.</param>
/// <param name="ContextWindowTokenLimit">Context-window token limit (must be positive).</param>
/// <param name="MaxOutputTokenLimit">Max-output token limit (positive and not greater than the context window).</param>
/// <param name="TimeoutPolicy">Safe timeout metadata.</param>
/// <param name="SafeCapabilityFlags">Optional allow-listed safe capability flags.</param>
/// <param name="ConfigurationReferenceId">Optional safe configuration reference identifier (never a secret value).</param>
/// <param name="Pricing">Administrator-supplied versioned pricing units and currency (required).</param>
/// <param name="ExpectedCapabilityVersion">
/// The capability version the caller last observed, or <see langword="null"/> when the caller is not asserting a
/// revision. A value below the current version is a regression; a value above it is a stale revision.
/// </param>
public record UpdateProviderModelEntry(
    string ProviderId,
    string ModelId,
    string DisplayLabel,
    bool SupportsTextGeneration,
    int ContextWindowTokenLimit,
    int MaxOutputTokenLimit,
    ProviderModelTimeoutPolicy TimeoutPolicy,
    ProviderModelCapabilityFlags SafeCapabilityFlags,
    string? ConfigurationReferenceId,
    ProviderModelPricing Pricing,
    int? ExpectedCapabilityVersion = null);
