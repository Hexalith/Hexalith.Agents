using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Queries;
using Hexalith.Agents.Contracts.Operations;

namespace Hexalith.Agents.Client;

/// <summary>Public support-safe audit inspection operations.</summary>
public interface IAgentAuditOperations
{
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

    /// <summary>Gets support-safe automatic posting evidence.</summary>
    ValueTask<AgentOperationResult<AgentPostedMessageEvidence>> GetPostingEvidenceAsync(
        string agentInteractionId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets support-safe proposal edit evidence.</summary>
    ValueTask<AgentOperationResult<AgentProposalEditEvidenceResult>> GetProposalEditEvidenceAsync(
        string agentInteractionId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets support-safe proposal regeneration evidence.</summary>
    ValueTask<AgentOperationResult<AgentProposalRegenerationEvidenceResult>> GetProposalRegenerationEvidenceAsync(
        string agentInteractionId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets support-safe proposal approval/posting evidence.</summary>
    ValueTask<AgentOperationResult<AgentProposalApprovalEvidenceResult>> GetProposalApprovalEvidenceAsync(
        string agentInteractionId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);
}
