namespace Hexalith.Agents.Contracts.AgentInteraction.Commands;

/// <summary>
/// Approves exactly one selected Proposed Agent Reply version and records the trusted posting outcome.
/// </summary>
/// <param name="AgentInteractionId">The AgentInteraction aggregate id.</param>
/// <param name="Result">The server-assembled approval/posting result.</param>
public sealed record ApproveProposedAgentReply(
    string AgentInteractionId,
    AgentProposalApprovalResult Result);
