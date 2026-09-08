namespace Hexalith.Agents.Contracts.ProviderCatalog.Queries;

/// <summary>
/// Requests authorized inspection of a single provider/model catalog entry, including disabled entries, without
/// exposing secrets (AC2, AC3).
/// </summary>
/// <param name="ProviderId">Stable provider identifier.</param>
/// <param name="ModelId">Stable model identifier.</param>
/// <param name="ExpectedCapabilityVersion">
/// The capability version the caller is waiting to see, or <see langword="null"/> when the caller wants current
/// projected truth. Supplying it is what lets the answer separate <c>AuthoritativePending</c> from
/// <c>ProjectionConfirmed</c>.
/// </param>
public record GetProviderCatalogEntryQuery(
    string ProviderId,
    string ModelId,
    int? ExpectedCapabilityVersion = null)
{
    /// <summary>The kebab-case EventStore domain this query is routed to.</summary>
    public const string Domain = "provider-catalog";

    /// <summary>The query type discriminator served by the live catalog query handler.</summary>
    public const string QueryType = nameof(GetProviderCatalogEntryQuery);
}
