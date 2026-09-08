using System;
using System.Collections.Generic;

using Hexalith.Agents.Contracts.ProviderCatalog;

using Hexalith.EventStore.Client.Projections;

namespace Hexalith.Agents.Server.Projections;

/// <summary>
/// The persisted provider-catalog read model (Story 5.3). It is a safe mirror of the replayed catalog state plus
/// the freshness metadata a caller compares against an accepted capability or projection version.
/// </summary>
/// <remarks>
/// Secret values never persist here — only the safe configuration reference and configured/not-configured state
/// (AD-9, AD-14).
/// </remarks>
public sealed class ProviderCatalogReadModel : IReadModelFreshness
{
    /// <summary>Gets or sets the catalog aggregate identifier (the tenant id).</summary>
    public string CatalogId { get; set; } = string.Empty;

    /// <summary>Gets or sets the tenant scope.</summary>
    public string TenantId { get; set; } = string.Empty;

    /// <summary>Gets or sets the projected catalog entries.</summary>
    public List<ProviderCatalogEntryView> Entries { get; set; } = [];

    /// <summary>Gets or sets the highest aggregate sequence number folded into this read model.</summary>
    public long LastSequenceNumber { get; set; }

    /// <inheritdoc />
    public DateTimeOffset? ProjectedAt { get; set; }

    /// <inheritdoc />
    public string? ProjectionVersion { get; set; }
}
