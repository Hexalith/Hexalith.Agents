namespace Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;

/// <summary>
/// A command attempted to reuse or decrease <c>CapabilityVersion</c> (Story 5.3). Capability versions are
/// monotonic and non-reusable; the command is rejected before mutation.
/// </summary>
public record ProviderModelCapabilityVersionRegressedRejection(
    string CatalogId,
    string ProviderId,
    string ModelId,
    int AttemptedCapabilityVersion,
    int CurrentCapabilityVersion) : IRejectionEvent;
