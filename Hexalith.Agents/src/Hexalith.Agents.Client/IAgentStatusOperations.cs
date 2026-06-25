using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Operations;

namespace Hexalith.Agents.Client;

/// <summary>Public operational status inspection operations.</summary>
public interface IAgentStatusOperations
{
    /// <summary>Gets canonical Agent readiness status.</summary>
    ValueTask<AgentOperationResult<AgentReadinessStatus>> GetAgentReadinessAsync(
        string agentId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets the safe Agent launch-readiness view (recorded metrics/latency/cost + presence flags + blockers).</summary>
    ValueTask<AgentOperationResult<AgentLaunchReadinessView>> GetAgentLaunchReadinessAsync(
        string agentId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets canonical provider/model readiness status.</summary>
    ValueTask<AgentOperationResult<ProviderModelReadinessStatus>> GetProviderModelReadinessAsync(
        string providerId,
        string modelId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets canonical Agent call status.</summary>
    ValueTask<AgentOperationResult<AgentCallOperationStatus>> GetCallStatusAsync(
        string agentInteractionId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets canonical proposal workflow status.</summary>
    ValueTask<AgentOperationResult<ProposalOperationStatus>> GetProposalStatusAsync(
        string agentInteractionId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets canonical audit availability status.</summary>
    ValueTask<AgentOperationResult<AuditAvailabilityStatus>> GetAuditAvailabilityAsync(
        string agentInteractionId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);
}
