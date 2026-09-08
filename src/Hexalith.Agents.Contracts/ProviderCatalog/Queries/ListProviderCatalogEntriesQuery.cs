namespace Hexalith.Agents.Contracts.ProviderCatalog.Queries;

/// <summary>
/// Requests authorized inspection of the provider/model catalog entries without exposing secrets (AC2, AC3).
/// </summary>
/// <param name="IncludeDisabled">
/// When <see langword="true"/>, disabled entries are included for historical inspection; when
/// <see langword="false"/>, only entries selectable for new active use are returned.
/// </param>
/// <param name="ExpectedProjectionVersion">
/// The projection version the caller is waiting to see, or <see langword="null"/> when the caller wants current
/// projected truth.
/// </param>
public record ListProviderCatalogEntriesQuery(
    bool IncludeDisabled,
    string? ExpectedProjectionVersion = null)
{
    /// <summary>The kebab-case EventStore domain this query is routed to.</summary>
    public const string Domain = "provider-catalog";

    /// <summary>The query type discriminator served by the live catalog query handler.</summary>
    public const string QueryType = nameof(ListProviderCatalogEntriesQuery);
}
