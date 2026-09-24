using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>Authorized tenant catalog result without platform configuration state.</summary>
/// <param name="Status">The safe inspection status.</param>
/// <param name="Entries">Visible enabled entries.</param>
/// <param name="PlatformProjectionVersion">The platform catalog checkpoint.</param>
/// <param name="TenantProjectionVersion">The tenant enablement checkpoint.</param>
/// <param name="ProjectedAt">The projection time.</param>
/// <param name="Freshness">The read freshness.</param>
/// <param name="TruthState">The submitted-to-confirmed stage.</param>
/// <param name="ProjectedCommandMessageIds">Recent successful tenant governance commands folded into the projection.</param>
public sealed record TenantProviderCatalogInspectionResult(
    ProviderCatalogInspectionStatus Status,
    IReadOnlyList<TenantProviderCatalogEntryView> Entries,
    string? PlatformProjectionVersion = null,
    string? TenantProjectionVersion = null,
    DateTimeOffset? ProjectedAt = null,
    AgentSetupFreshness Freshness = AgentSetupFreshness.Unknown,
    AgentSetupTruthState TruthState = AgentSetupTruthState.Unknown,
    IReadOnlyList<string>? ProjectedCommandMessageIds = null);
