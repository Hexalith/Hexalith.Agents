namespace Hexalith.Agents.Contracts.AgentInteraction.Events;

/// <summary>
/// Records that the approved proposal version was posted as a Conversation Message.
/// </summary>
/// <param name="AgentInteractionId">The AgentInteraction aggregate id.</param>
/// <param name="Evidence">Safe approval/posting evidence with the posted message id.</param>
public sealed record ProposedAgentReplyPosted(
    string AgentInteractionId,
    AgentProposedReplyApprovalEvidence Evidence) : IEventPayload;
