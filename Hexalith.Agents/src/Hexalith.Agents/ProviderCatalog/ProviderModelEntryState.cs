using Hexalith.Agents.Contracts.ProviderCatalog;

namespace Hexalith.Agents.ProviderCatalog;

/// <summary>
/// Replay state for a single governed provider/model catalog entry. Mutated only through
/// <see cref="ProviderCatalogState"/>'s <c>Apply</c> methods (AD-3 pure aggregates). Holds only safe metadata
/// and a safe configuration reference/state — never a secret value.
/// </summary>
public sealed class ProviderModelEntryState
{
    /// <summary>Gets or sets the stable provider identifier.</summary>
    public string ProviderId { get; set; } = string.Empty;

    /// <summary>Gets or sets the stable model identifier.</summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>Gets or sets the safe admin-facing label.</summary>
    public string DisplayLabel { get; set; } = string.Empty;

    /// <summary>Gets or sets a value indicating whether the entry is enabled (selectable for new active use).</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Gets or sets a value indicating whether the model supports text generation.</summary>
    public bool SupportsTextGeneration { get; set; }

    /// <summary>Gets or sets the context-window token limit.</summary>
    public int ContextWindowTokenLimit { get; set; }

    /// <summary>Gets or sets the max-output token limit.</summary>
    public int MaxOutputTokenLimit { get; set; }

    /// <summary>Gets or sets the safe timeout metadata.</summary>
    public ProviderModelTimeoutPolicy TimeoutPolicy { get; set; } = new(0, 0);

    /// <summary>Gets or sets the allow-listed safe capability flags.</summary>
    public ProviderModelCapabilityFlags SafeCapabilityFlags { get; set; }

    /// <summary>Gets or sets the safe configured/not-configured state.</summary>
    public ProviderConfigurationState ConfigurationState { get; set; }

    /// <summary>Gets or sets the safe configuration reference identifier (never a secret value).</summary>
    public string? ConfigurationReferenceId { get; set; }

    /// <summary>
    /// Gets or sets the replay-derived provider capability version (Story 1.5). Set to 1 on create and incremented
    /// on each safe-metadata update; enable/disable do not change capability metadata, so they do not bump it. The
    /// version is derived during replay only — no existing event payload carries it. A plain int — exposes nothing
    /// secret (AC1; AD-9).
    /// </summary>
    public int CapabilityVersion { get; set; }
}
