using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Server.Ports;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Pure, deterministic mapping from a safe catalog read result to the trusted
/// <see cref="ProviderSelectionValidationStatus"/> verdict (Story 1.5; AC2, AC4; AD-9, AD-10, AD-12). Shared by the
/// selection orchestration and the activation re-validation step so both compute provider readiness identically.
/// </summary>
/// <remarks>
/// Precedence is top-to-bottom: existence → authorization → availability → enabled → text-gen → configured →
/// capability metadata → <see cref="ProviderSelectionValidationStatus.Valid"/>. It reads only the safe projection
/// and fails closed on any uncertainty (an unauthorized/unavailable/degraded read never resolves to <c>Valid</c>).
/// </remarks>
internal static class ProviderSelectionVerdict
{
    /// <summary>Computes the fail-closed provider-readiness verdict from a catalog read result.</summary>
    /// <param name="result">The safe catalog read result.</param>
    /// <returns>The deterministic verdict.</returns>
    internal static ProviderSelectionValidationStatus Evaluate(ProviderCatalogEntryReadResult result)
        => result.Status switch
        {
            // Existence and authorization fail closed before anything else; a cross-tenant read surfaces as
            // NotAuthorized → Unauthorized and never leaks another tenant's records (AC4).
            ProviderCatalogInspectionStatus.EntryNotFound => ProviderSelectionValidationStatus.Missing,
            ProviderCatalogInspectionStatus.NotAuthorized => ProviderSelectionValidationStatus.Unauthorized,

            // A successful read with a concrete entry is graded by capability; a successful read with no entry is a
            // degraded/unavailable read (fail closed — AD-12), as is any unexpected/future status.
            ProviderCatalogInspectionStatus.Success when result.Entry is not null => EvaluateEntry(result.Entry),
            _ => ProviderSelectionValidationStatus.Unavailable,
        };

    private static ProviderSelectionValidationStatus EvaluateEntry(ProviderCatalogEntryView entry)
    {
        if (entry.Status != ProviderModelStatus.Enabled)
        {
            return ProviderSelectionValidationStatus.Disabled;
        }

        if (!entry.SupportsTextGeneration)
        {
            return ProviderSelectionValidationStatus.NotTextGenerationCapable;
        }

        if (entry.ConfigurationState != ProviderConfigurationState.Configured)
        {
            return ProviderSelectionValidationStatus.NotConfigured;
        }

        return HasValidCapabilityMetadata(entry)
            ? ProviderSelectionValidationStatus.Valid
            : ProviderSelectionValidationStatus.MissingCapabilityMetadata;
    }

    // The AD-10 capability floor: positive, internally-consistent context/output limits and a valid timeout policy.
    private static bool HasValidCapabilityMetadata(ProviderCatalogEntryView entry)
        => entry.ContextWindowTokenLimit > 0
            && entry.MaxOutputTokenLimit > 0
            && entry.MaxOutputTokenLimit <= entry.ContextWindowTokenLimit
            && entry.TimeoutPolicy is { RequestTimeoutMilliseconds: > 0, MaxRetries: >= 0 };
}
