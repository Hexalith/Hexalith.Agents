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
/// <remarks>
/// Binding this logic to the EventStore SDK <c>IDomainQueryHandler</c>/<c>IReadModelStore</c> DAPR read path is
/// deferred to the dedicated read-model story (mirroring how sibling modules landed their DAPR-backed read path
/// in a later story); Story 1.2 keeps the inspection logic pure so it is fully unit-testable here.
/// </remarks>
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

    private static ProviderCatalogEntryView ToView(ProviderModelEntryState entry)
        => new(
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
            IsSelectableForNewActiveUse: entry.IsEnabled,
            entry.CapabilityVersion);
}
