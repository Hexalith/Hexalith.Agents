namespace Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;

/// <summary>Rejects invalid or missing governed terms without disclosing submitted content.</summary>
/// <param name="CatalogId">The catalog stream identifier.</param>
/// <param name="ProviderId">The provider identifier.</param>
/// <param name="ModelId">The model identifier.</param>
/// <param name="Reason">A safe validation reason.</param>
public sealed record InvalidProviderDataHandlingRejection(string CatalogId, string ProviderId, string ModelId, string Reason) : IRejectionEvent;
