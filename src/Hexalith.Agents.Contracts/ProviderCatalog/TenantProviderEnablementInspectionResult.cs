using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Contracts.ProviderCatalog;

/// <summary>Platform-only projection of one tenant's enablement state, without decision details.</summary>
/// <param name="Status">The safe inspection status.</param>
/// <param name="Enabled">The platform's tenant enablement decision.</param>
/// <param name="Revision">The tenant stream revision.</param>
/// <param name="ProjectionVersion">The projection checkpoint.</param>
/// <param name="LastEnablementMessageId">The last projected command identity.</param>
/// <param name="ProjectedCommandMessageIds">Recent successful command identities folded into this tenant stream.</param>
/// <param name="Freshness">Whether the projection agrees with the authoritative stream.</param>
/// <param name="TruthState">The truth stage of the inspection.</param>
public sealed record TenantProviderEnablementInspectionResult(
    ProviderCatalogInspectionStatus Status,
    bool? Enabled,
    int? Revision,
    string? ProjectionVersion = null,
    string? LastEnablementMessageId = null,
    IReadOnlyList<string>? ProjectedCommandMessageIds = null,
    AgentSetupFreshness Freshness = AgentSetupFreshness.Unknown,
    AgentSetupTruthState TruthState = AgentSetupTruthState.Unknown);
