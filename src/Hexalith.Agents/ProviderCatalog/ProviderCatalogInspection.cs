using System;
using System.Collections.Generic;
using System.Linq;

using Hexalith.Agents.Contracts.ProviderCatalog;

namespace Hexalith.Agents.ProviderCatalog;

/// <summary>
/// Pure, dependency-free read path over rehydrated <see cref="ProviderCatalogState"/> for authorized inspection
/// of current and historical provider/model state without exposing secrets (AC2, AC3). Because it operates on a
/// single tenant's catalog aggregate state, cross-tenant isolation is structural — it can never observe another
/// tenant's records. Authorization is decided by the caller (server/application) from trusted claims and passed
/// in as <c>isProviderAdmin</c>; unauthorized inspection returns a structured fail-closed result rather than
/// throwing or leaking which entries exist.
/// </summary>
public static class ProviderCatalogInspection
{
    /// <summary>
    /// Inspects a single provider/model catalog entry, including disabled entries (AC2, AC3).
    /// </summary>
    /// <param name="state">The rehydrated catalog state (null when the catalog has no entries yet).</param>
    /// <param name="isProviderAdmin">Whether the caller is an authorized provider administrator.</param>
    /// <param name="providerId">The provider identifier to inspect.</param>
    /// <param name="modelId">The model identifier to inspect.</param>
    /// <returns>A structured inspection result.</returns>
    public static ProviderCatalogInspectionResult GetEntry(
        ProviderCatalogState? state,
        bool isProviderAdmin,
        string providerId,
        string modelId)
    {
        if (!isProviderAdmin)
        {
            return ProviderCatalogInspectionResult.NotAuthorized();
        }

        ProviderModelEntryState? entry = null;
        _ = state?.Entries.TryGetValue(ProviderCatalogState.EntryKey(providerId, modelId), out entry);

        return entry is null
            ? ProviderCatalogInspectionResult.NotFound()
            : ProviderCatalogInspectionResult.Success([ToView(entry)]);
    }

    /// <summary>
    /// Lists the provider/model catalog entries (AC2, AC3). Disabled entries are included only when
    /// <paramref name="includeDisabled"/> is set, and are flagged as not selectable for new active use.
    /// </summary>
    /// <param name="state">The rehydrated catalog state (null when the catalog has no entries yet).</param>
    /// <param name="isProviderAdmin">Whether the caller is an authorized provider administrator.</param>
    /// <param name="includeDisabled">Whether to include disabled entries for historical inspection.</param>
    /// <returns>A structured inspection result.</returns>
    public static ProviderCatalogInspectionResult ListEntries(
        ProviderCatalogState? state,
        bool isProviderAdmin,
        bool includeDisabled)
    {
        if (!isProviderAdmin)
        {
            return ProviderCatalogInspectionResult.NotAuthorized();
        }

        if (state is null)
        {
            return ProviderCatalogInspectionResult.Success([]);
        }

        ProviderCatalogEntryView[] views = state.Entries.Values
            .Where(entry => includeDisabled || entry.IsEnabled)
            .OrderBy(entry => entry.ProviderId, StringComparer.Ordinal)
            .ThenBy(entry => entry.ModelId, StringComparer.Ordinal)
            .Select(ToView)
            .ToArray();

        return ProviderCatalogInspectionResult.Success(views);
    }

    /// <summary>
    /// Returns whether the entry may be selected for new active Agent use: enabled, configured, text-generation
    /// capable, valid limits, valid pricing, and a non-regressed capability version.
    /// </summary>
    /// <param name="entry">The catalog entry.</param>
    /// <returns><see langword="true"/> when the entry is eligible for a new active selection.</returns>
    public static bool IsSelectableForNewActiveUse(ProviderModelEntryState entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return entry.IsEnabled
            && entry.SupportsTextGeneration
            && entry.ConfigurationState == ProviderConfigurationState.Configured
            && HasValidLimits(entry)
            && ProviderCatalogAggregate.HasValidPricing(entry.Pricing)
            && entry.CapabilityVersion >= 1
            && entry.Pricing is { PricingVersion: >= 1 };
    }

    /// <summary>Maps a replayed entry to the safe public view.</summary>
    /// <param name="entry">The catalog entry.</param>
    /// <returns>The safe view.</returns>
    public static ProviderCatalogEntryView ToView(ProviderModelEntryState entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return new(
            entry.ProviderId,
            entry.ModelId,
            entry.DisplayLabel,
            entry.IsEnabled ? ProviderModelStatus.Enabled : ProviderModelStatus.Disabled,
            entry.SupportsTextGeneration,
            entry.ContextWindowTokenLimit,
            entry.MaxOutputTokenLimit,
            entry.TimeoutPolicy,
            entry.SafeCapabilityFlags,
            entry.ConfigurationState,
            entry.ConfigurationReferenceId,
            IsSelectableForNewActiveUse(entry),
            entry.CapabilityVersion,
            entry.Pricing);
    }

    private static bool HasValidLimits(ProviderModelEntryState entry)
        => entry.ContextWindowTokenLimit > 0
            && entry.MaxOutputTokenLimit > 0
            && entry.MaxOutputTokenLimit <= entry.ContextWindowTokenLimit
            && entry.TimeoutPolicy is { RequestTimeoutMilliseconds: > 0, MaxRetries: >= 0 };
}
