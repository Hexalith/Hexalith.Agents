namespace Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;

/// <summary>Support-safe rejection of tenant enablement or terms governance.</summary>
/// <param name="TenantId">The target tenant.</param>
/// <param name="ProviderId">The provider identifier.</param>
/// <param name="ModelId">The model identifier.</param>
/// <param name="Reason">A support-safe reason code.</param>
public sealed record TenantProviderGovernanceRejected(string TenantId, string ProviderId, string ModelId, string Reason) : IRejectionEvent;
