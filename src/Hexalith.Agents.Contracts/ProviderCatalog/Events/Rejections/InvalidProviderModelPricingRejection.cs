namespace Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;

/// <summary>
/// A create/update command carried missing or invalid administrator-supplied pricing (Story 5.3). The
/// <paramref name="Reason"/> is a safe classification — never a secret value, Provider SDK error, or raw payload.
/// </summary>
public record InvalidProviderModelPricingRejection(
    string CatalogId,
    string ProviderId,
    string ModelId,
    string Reason) : IRejectionEvent;
