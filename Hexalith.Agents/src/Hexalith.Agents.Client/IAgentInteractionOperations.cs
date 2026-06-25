using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Contracts.Operations;

namespace Hexalith.Agents.Client;

/// <summary>Public Agent invocation and invocation-status operations.</summary>
public interface IAgentInteractionOperations
{
    /// <summary>Requests a governed Agent interaction.</summary>
    ValueTask<AgentOperationResult<AgentCallRequestResult>> RequestAsync(
        RequestAgentInteraction command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets safe Agent interaction status.</summary>
    ValueTask<AgentOperationResult<AgentInteractionInspectionResult>> GetStatusAsync(
        string agentInteractionId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets support-safe invocation gate evidence.</summary>
    ValueTask<AgentOperationResult<AgentInteractionGateEvidenceResult>> GetGateEvidenceAsync(
        string agentInteractionId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets support-safe context evidence.</summary>
    ValueTask<AgentOperationResult<AgentInteractionContextEvidenceResult>> GetContextEvidenceAsync(
        string agentInteractionId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets support-safe generation attempt evidence.</summary>
    ValueTask<AgentOperationResult<AgentGenerationAttemptEvidence>> GetGenerationEvidenceAsync(
        string agentInteractionId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets support-safe posting evidence.</summary>
    ValueTask<AgentOperationResult<AgentPostedMessageEvidence>> GetPostingEvidenceAsync(
        string agentInteractionId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets support-safe proposal approval/posting evidence.</summary>
    ValueTask<AgentOperationResult<AgentProposalApprovalEvidenceResult>> GetApprovalEvidenceAsync(
        string agentInteractionId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);
}
