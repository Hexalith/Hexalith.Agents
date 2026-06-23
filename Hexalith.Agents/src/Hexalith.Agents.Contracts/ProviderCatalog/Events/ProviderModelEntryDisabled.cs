namespace Hexalith.Agents.Contracts.ProviderCatalog.Events;

/// <summary>
/// Records that a provider/model catalog entry was disabled. The entry is no longer selectable for new active
/// Agent configuration; its historical state remains inspectable (AC2).
/// </summary>
public record ProviderModelEntryDisabled(string CatalogId, string ProviderId, string ModelId) : IEventPayload;
