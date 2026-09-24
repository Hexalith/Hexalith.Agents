namespace Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;

/// <summary>Rejects invalid or missing governed terms without disclosing submitted content.</summary>
public sealed record InvalidProviderDataHandlingRejection(string CatalogId, string ProviderId, string ModelId, string Reason) : IRejectionEvent;
