namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that one preserved proposal version was approved and frozen for posting.
/// </summary>
/// <param name="AgentInteractionId">The AgentInteraction aggregate id.</param>
/// <param name="Evidence">Safe approval evidence.</param>
public sealed record ProposedAgentReplyApproved(
    string AgentInteractionId,
    AgentProposedReplyApprovalEvidence Evidence) : IEventPayload;
