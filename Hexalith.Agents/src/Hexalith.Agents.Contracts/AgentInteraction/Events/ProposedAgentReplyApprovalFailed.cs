namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that approval failed closed before posting side effects.
/// </summary>
/// <param name="AgentInteractionId">The AgentInteraction aggregate id.</param>
/// <param name="Reason">The safe failure reason.</param>
/// <param name="Evidence">Safe approval evidence.</param>
public sealed record ProposedAgentReplyApprovalFailed(
    string AgentInteractionId,
    AgentProposalApprovalFailureReason Reason,
    AgentProposedReplyApprovalEvidence Evidence) : IEventPayload;
