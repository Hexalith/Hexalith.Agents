using Hexalith.Agents.Contracts.Operations;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Provides queryable launch-readiness blockers for Agents audit governance.</summary>
public interface IAgentAuditGovernanceReadinessProvider
{
    /// <summary>Gets the current governance readiness state.</summary>
    AgentAuditGovernanceReadiness GetReadiness();
}
