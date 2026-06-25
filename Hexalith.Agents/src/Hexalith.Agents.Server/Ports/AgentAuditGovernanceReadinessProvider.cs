using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.Contracts.Operations;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Default metadata-only audit governance readiness provider.</summary>
public sealed class AgentAuditGovernanceReadinessProvider : IAgentAuditGovernanceReadinessProvider
{
    /// <inheritdoc />
    public AgentAuditGovernanceReadiness GetReadiness()
        => AgentAuditContentPolicy.GetReadiness();
}
