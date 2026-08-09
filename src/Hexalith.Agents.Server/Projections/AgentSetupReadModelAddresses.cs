using System;

namespace Hexalith.Agents.Server.Projections;

/// <summary>
/// Canonical addresses for the Agent setup read model (Story 5.2 AC2). The tenant is part of every key, so a
/// read for one tenant can never resolve another tenant's Agent.
/// </summary>
public static class AgentSetupReadModelAddresses
{
    /// <summary>The kebab-case domain that owns the projection.</summary>
    public const string Domain = "agent";

    /// <summary>The projection name declared to the platform slot registry.</summary>
    public const string ProjectionName = "agent-setup";

    /// <summary>The single aggregate-owned slot the projection writes.</summary>
    public const string DetailSlot = "detail";

    /// <summary>Builds the tenant-scoped key holding one Agent's setup read model.</summary>
    /// <param name="tenantId">The tenant scope.</param>
    /// <param name="agentId">The Agent aggregate id.</param>
    /// <returns>The state key.</returns>
    public static string Detail(string tenantId, string agentId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentId);
        return $"{Domain}:{tenantId}:{ProjectionName}:{DetailSlot}:{agentId}";
    }
}
