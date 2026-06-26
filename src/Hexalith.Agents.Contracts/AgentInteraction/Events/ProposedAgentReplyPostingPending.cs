namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that posting of the approved proposal version is pending.
/// </summary>
/// <param name="AgentInteractionId">The AgentInteraction aggregate id.</param>
/// <param name="Evidence">Safe approval/posting evidence.</param>
public sealed record ProposedAgentReplyPostingPending(
    string AgentInteractionId,
    AgentProposedReplyApprovalEvidence Evidence) : IEventPayload;
