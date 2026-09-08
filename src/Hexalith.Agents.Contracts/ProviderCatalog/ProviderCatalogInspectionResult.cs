using System;
using System.Collections.Generic;

using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>
/// Structured result of an authorized provider-catalog inspection (AC2, AC3). On
/// <see cref="ProviderCatalogInspectionStatus.NotAuthorized"/>,
/// <see cref="ProviderCatalogInspectionStatus.EntryNotFound"/>, or
/// <see cref="ProviderCatalogInspectionStatus.Unavailable"/> the <see cref="Entries"/> list is empty, so a
/// failed inspection never fingerprints other entries or tenant records. Projection version and freshness mirror
/// the Agent-setup read-model freshness convention rather than inventing catalog-only metadata.
/// </summary>
/// <param name="Status">The inspection outcome.</param>
/// <param name="Entries">Safe entry views (empty unless <see cref="Status"/> is <see cref="ProviderCatalogInspectionStatus.Success"/>).</param>
/// <param name="ProjectionVersion">The projected catalog sequence version, or <see langword="null"/> when unknown.</param>
/// <param name="ProjectedAt">When the catalog projection was last folded, or <see langword="null"/> when unknown.</param>
/// <param name="Freshness">Whether the projection reflects the version the caller is waiting for.</param>
/// <param name="TruthState">The truth stage of this observation.</param>
public record ProviderCatalogInspectionResult(
    ProviderCatalogInspectionStatus Status,
    IReadOnlyList<ProviderCatalogEntryView> Entries,
    string? ProjectionVersion = null,
    DateTimeOffset? ProjectedAt = null,
    AgentSetupFreshness Freshness = AgentSetupFreshness.Unknown,
    AgentSetupTruthState TruthState = AgentSetupTruthState.Unknown)
{
    /// <summary>Creates a successful inspection result carrying the given safe views and freshness.</summary>
    public static ProviderCatalogInspectionResult Success(
        IReadOnlyList<ProviderCatalogEntryView> entries,
        string? projectionVersion = null,
        DateTimeOffset? projectedAt = null,
        AgentSetupFreshness freshness = AgentSetupFreshness.Unknown,
        AgentSetupTruthState truthState = AgentSetupTruthState.Unknown)
        => new(ProviderCatalogInspectionStatus.Success, entries, projectionVersion, projectedAt, freshness, truthState);

    /// <summary>Creates a not-authorized result with no entry data (AC3 fail-closed).</summary>
    public static ProviderCatalogInspectionResult NotAuthorized()
        => new(ProviderCatalogInspectionStatus.NotAuthorized, []);

    /// <summary>Creates an entry-not-found result with no entry data.</summary>
    public static ProviderCatalogInspectionResult NotFound()
        => new(ProviderCatalogInspectionStatus.EntryNotFound, []);

    /// <summary>Creates an unavailable result with no entry data.</summary>
    public static ProviderCatalogInspectionResult Unavailable()
        => new(ProviderCatalogInspectionStatus.Unavailable, []);
}
