namespace Hexalith.Agents.Contracts.ProviderCatalog.Commands;

/// <summary>
/// Creates (or, when re-sent identically, idempotently acknowledges) a governed provider/model catalog entry
/// (AC1). Carries only safe capability metadata and an optional safe configuration reference — never raw
/// credentials, provider SDK options, or secret values (AD-9, AD-14).
/// </summary>
/// <param name="ProviderId">Stable provider identifier (non-empty; no provider-SDK object).</param>
/// <param name="ModelId">Stable model identifier (non-empty).</param>
/// <param name="DisplayLabel">Safe admin-facing label.</param>
/// <param name="Enabled">Initial enabled state (governs future selectable/usable state).</param>
/// <param name="SupportsTextGeneration">Required V1 capability: text-generation support.</param>
/// <param name="ContextWindowTokenLimit">Context-window token limit (must be positive).</param>
/// <param name="MaxOutputTokenLimit">Max-output token limit (positive and not greater than the context window).</param>
/// <param name="TimeoutPolicy">Safe timeout metadata.</param>
/// <param name="SafeCapabilityFlags">Optional allow-listed safe capability flags.</param>
/// <param name="ConfigurationReferenceId">Optional safe configuration reference identifier (never a secret value).</param>
public record CreateProviderModelEntry(
    string ProviderId,
    string ModelId,
    string DisplayLabel,
    bool Enabled,
    bool SupportsTextGeneration,
    int ContextWindowTokenLimit,
    int MaxOutputTokenLimit,
    ProviderModelTimeoutPolicy TimeoutPolicy,
    ProviderModelCapabilityFlags SafeCapabilityFlags,
    string? ConfigurationReferenceId);
