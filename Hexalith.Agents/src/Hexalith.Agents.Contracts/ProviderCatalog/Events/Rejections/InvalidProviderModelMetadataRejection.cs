namespace Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;

/// <summary>
/// A create/update command carried invalid capability metadata — e.g. a non-positive token limit, a max-output
/// limit greater than the context window, or an out-of-range timeout/retry value (AC1, AC4). The
/// <paramref name="Reason"/> is a safe, display-friendly classification (never a raw provider error or payload).
/// </summary>
public record InvalidProviderModelMetadataRejection(
    string CatalogId,
    string ProviderId,
    string ModelId,
    string Reason) : IRejectionEvent;
