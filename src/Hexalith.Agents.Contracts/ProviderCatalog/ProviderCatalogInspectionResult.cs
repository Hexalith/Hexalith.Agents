using System.Collections.Generic;

namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>
/// Structured result of an authorized provider-catalog inspection (AC2, AC3). On <see cref="ProviderCatalogInspectionStatus.NotAuthorized"/>
/// or <see cref="ProviderCatalogInspectionStatus.EntryNotFound"/> the <see cref="Entries"/> list is empty, so a
/// failed inspection never fingerprints other entries or tenant records.
/// </summary>
/// <param name="Status">The inspection outcome.</param>
/// <param name="Entries">Safe entry views (empty unless <see cref="Status"/> is <see cref="ProviderCatalogInspectionStatus.Success"/>).</param>
public record ProviderCatalogInspectionResult(
    ProviderCatalogInspectionStatus Status,
    IReadOnlyList<ProviderCatalogEntryView> Entries)
{
    /// <summary>Creates a successful inspection result carrying the given safe views.</summary>
    public static ProviderCatalogInspectionResult Success(IReadOnlyList<ProviderCatalogEntryView> entries)
        => new(ProviderCatalogInspectionStatus.Success, entries);

    /// <summary>Creates a not-authorized result with no entry data (AC3 fail-closed).</summary>
    public static ProviderCatalogInspectionResult NotAuthorized()
        => new(ProviderCatalogInspectionStatus.NotAuthorized, []);

    /// <summary>Creates an entry-not-found result with no entry data.</summary>
    public static ProviderCatalogInspectionResult NotFound()
        => new(ProviderCatalogInspectionStatus.EntryNotFound, []);
}
