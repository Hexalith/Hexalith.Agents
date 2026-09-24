namespace Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;

/// <summary>Rejects a lifecycle command whose observed lifecycle revision is missing or stale.</summary>
/// <param name="CatalogId">The catalog entry stream identifier.</param>
/// <param name="ProviderId">The provider identifier.</param>
/// <param name="ModelId">The model identifier.</param>
/// <param name="ExpectedLifecycleRevision">The caller's observed revision, if supplied.</param>
/// <param name="CurrentLifecycleRevision">The current lifecycle revision.</param>
public record ProviderModelLifecycleRevisionRejected(
    string CatalogId,
    string ProviderId,
    string ModelId,
    int? ExpectedLifecycleRevision,
    int CurrentLifecycleRevision) : IRejectionEvent;
