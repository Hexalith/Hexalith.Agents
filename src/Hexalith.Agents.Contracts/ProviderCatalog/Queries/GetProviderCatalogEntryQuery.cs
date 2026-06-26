namespace Hexalith.Agents.Contracts.ProviderCatalog.Queries;

/// <summary>
/// Requests authorized inspection of a single provider/model catalog entry, including disabled entries, without
/// exposing secrets (AC2, AC3).
/// </summary>
/// <param name="ProviderId">Stable provider identifier.</param>
/// <param name="ModelId">Stable model identifier.</param>
public record GetProviderCatalogEntryQuery(string ProviderId, string ModelId);
