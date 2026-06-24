namespace Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

/// <summary>
/// Structural rejection for reject-proposed-reply commands that cannot be evaluated against the proposal lifecycle (no
/// pending proposal, or the proposal is already terminal/non-pending). Carries no state change — distinct from
/// <see cref="ProposedAgentReplyRejectionFailed"/>, which is a recorded fail-closed decision (AC1, AC4; AD-12).
/// </summary>
/// <param name="AgentInteractionId">The AgentInteraction aggregate id.</param>
/// <param name="Reason">The safe structural reason.</param>
public sealed record ProposedAgentReplyNotRejectableRejection(
    string AgentInteractionId,
    AgentProposedReplyNotRejectableReason Reason) : IRejectionEvent;
