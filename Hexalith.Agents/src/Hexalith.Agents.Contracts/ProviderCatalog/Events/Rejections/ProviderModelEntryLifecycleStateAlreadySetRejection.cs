namespace Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;

/// <summary>
/// An enable/disable command requested the lifecycle state the entry is already in (AC4 same-state lifecycle
/// request). A deterministic, structured rejection rather than a silent mutation.
/// </summary>
public record ProviderModelEntryLifecycleStateAlreadySetRejection(
    string CatalogId,
    string ProviderId,
    string ModelId,
    ProviderModelStatus CurrentStatus,
    ProviderModelStatus RequestedStatus,
    string CommandName) : IRejectionEvent;
