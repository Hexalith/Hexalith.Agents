namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that posting the approved proposal version failed closed.
/// </summary>
/// <param name="AgentInteractionId">The AgentInteraction aggregate id.</param>
/// <param name="Reason">The safe posting failure reason.</param>
/// <param name="Evidence">Safe approval/posting evidence.</param>
public sealed record ProposedAgentReplyPostingFailed(
    string AgentInteractionId,
    AgentProposalApprovalFailureReason Reason,
    AgentProposedReplyApprovalEvidence Evidence) : IEventPayload;
