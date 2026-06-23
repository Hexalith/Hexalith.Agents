namespace Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;

/// <summary>
/// An update/enable/disable command targeted a provider/model entry that does not exist in the catalog (AC4).
/// </summary>
public record ProviderModelEntryNotFoundRejection(
    string CatalogId,
    string ProviderId,
    string ModelId) : IRejectionEvent;
