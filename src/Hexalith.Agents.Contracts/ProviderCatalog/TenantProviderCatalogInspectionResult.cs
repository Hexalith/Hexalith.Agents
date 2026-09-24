using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>Authorized tenant catalog result without platform configuration state.</summary>
public sealed record TenantProviderCatalogInspectionResult(
    ProviderCatalogInspectionStatus Status,
    IReadOnlyList<TenantProviderCatalogEntryView> Entries,
    string? PlatformProjectionVersion = null,
    string? TenantProjectionVersion = null,
    DateTimeOffset? ProjectedAt = null,
    AgentSetupFreshness Freshness = AgentSetupFreshness.Unknown,
    AgentSetupTruthState TruthState = AgentSetupTruthState.Unknown);
