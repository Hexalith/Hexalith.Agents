namespace Hexalith.Agents.Contracts.ProviderCatalog.Queries;

/// <summary>
/// Requests authorized inspection of the provider/model catalog entries without exposing secrets (AC2, AC3).
/// </summary>
/// <param name="IncludeDisabled">
/// When <see langword="true"/>, disabled entries are included for historical inspection; when
/// <see langword="false"/>, only entries selectable for new active use are returned.
/// </param>
public record ListProviderCatalogEntriesQuery(bool IncludeDisabled);
