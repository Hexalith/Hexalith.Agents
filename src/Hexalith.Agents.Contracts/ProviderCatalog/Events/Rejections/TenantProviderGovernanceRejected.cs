namespace Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;

/// <summary>Support-safe rejection of tenant enablement or terms governance.</summary>
public sealed record TenantProviderGovernanceRejected(string TenantId, string ProviderId, string ModelId, string Reason) : IRejectionEvent;
