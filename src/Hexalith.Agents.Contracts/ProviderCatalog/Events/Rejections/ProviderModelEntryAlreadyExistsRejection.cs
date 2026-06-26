namespace Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;

/// <summary>
/// A create command targeted a provider/model entry that already exists with conflicting metadata (AC4). An
/// exact-duplicate create is a deterministic no-op instead; this rejection is reserved for conflicting payloads
/// so duplicate commands never mutate state silently.
/// </summary>
public record ProviderModelEntryAlreadyExistsRejection(
    string CatalogId,
    string ProviderId,
    string ModelId) : IRejectionEvent;
