using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Operations;

namespace Hexalith.Agents.AgentInteraction;

/// <summary>Content-bearing audit policy for Story 4.2.</summary>
public static class AgentAuditContentPolicy
{
    /// <summary>Gets the current audit governance readiness state.</summary>
    public static AgentAuditGovernanceReadiness GetReadiness()
        => AgentAuditGovernanceReadiness.MetadataOnlyBlocked;

    /// <summary>Evaluates whether an audit treatment may emit content-bearing audit output.</summary>
    public static AgentOperationResult<ContentSafetyAuditTreatment> Evaluate(ContentSafetyAuditTreatment treatment)
        => treatment == ContentSafetyAuditTreatment.MetadataOnly
            ? AgentOperationResult<ContentSafetyAuditTreatment>.Succeeded(ContentSafetyAuditTreatment.MetadataOnly)
            : AgentOperationResult<ContentSafetyAuditTreatment>.Failed(AgentOperationErrorCode.Blocked);
}
