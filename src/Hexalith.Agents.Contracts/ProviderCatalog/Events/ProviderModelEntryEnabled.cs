namespace Hexalith.Agents.Contracts.ProviderCatalog.Events;

/// <summary>
/// Records that a provider/model catalog entry was enabled and is selectable for new active Agent
/// configuration (AC1, AC2).
/// </summary>
public record ProviderModelEntryEnabled(string CatalogId, string ProviderId, string ModelId) : IEventPayload;
