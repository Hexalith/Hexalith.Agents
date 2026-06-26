namespace Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

/// <summary>
/// Structural rejection for approval commands that cannot be evaluated against the proposal lifecycle.
/// </summary>
/// <param name="AgentInteractionId">The AgentInteraction aggregate id.</param>
/// <param name="Reason">The safe structural reason.</param>
public sealed record ProposedAgentReplyNotApprovableRejection(
    string AgentInteractionId,
    AgentProposedReplyNotApprovableReason Reason) : IRejectionEvent;
