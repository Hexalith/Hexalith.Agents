using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.Operations;

namespace Hexalith.Agents.Client;

/// <summary>Public proposal workflow operations.</summary>
public interface IProposalWorkflowOperations
{
    /// <summary>Lists authorized pending or historical proposals.</summary>
    ValueTask<AgentOperationResult<PendingProposalsResult>> ListAsync(
        bool includeHistorical,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Gets one authorized proposal detail.</summary>
    ValueTask<AgentOperationResult<ProposalDetailResult>> GetDetailAsync(
        string agentInteractionId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Edits a proposed Agent reply version.</summary>
    ValueTask<AgentOperationResult<AgentProposalEditResult>> EditAsync(
        EditProposedAgentReply command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Regenerates a proposed Agent reply version.</summary>
    ValueTask<AgentOperationResult<AgentProposalRegenerationResult>> RegenerateAsync(
        RegenerateProposedAgentReply command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Approves a selected proposed Agent reply version.</summary>
    ValueTask<AgentOperationResult<AgentProposalApprovalResult>> ApproveAsync(
        ApproveProposedAgentReply command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Rejects a proposed Agent reply.</summary>
    ValueTask<AgentOperationResult<AgentProposalRejectionResult>> RejectAsync(
        RejectProposedAgentReply command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Abandons a proposed Agent reply.</summary>
    ValueTask<AgentOperationResult<AgentProposalAbandonmentResult>> AbandonAsync(
        AbandonProposedAgentReply command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>Expires a proposed Agent reply.</summary>
    ValueTask<AgentOperationResult<AgentProposalExpiryResult>> ExpireAsync(
        ExpireProposedAgentReply command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default);
}
