namespace Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;

/// <summary>
/// A command named an <c>ExpectedCapabilityVersion</c> ahead of the current catalog entry (Story 5.3). The
/// write is rejected before mutation so a stale or gapped revision cannot overwrite current truth.
/// </summary>
public record ProviderModelEntryStaleRevisionRejection(
    string CatalogId,
    string ProviderId,
    string ModelId,
    int ExpectedCapabilityVersion,
    int CurrentCapabilityVersion) : IRejectionEvent;
