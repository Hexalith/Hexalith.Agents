namespace Hexalith.Agents.Contracts.AgentInteraction.Events.Rejections;

/// <summary>
/// Structural rejection for expire-proposed-reply commands that cannot be evaluated against the proposal lifecycle (no
/// pending proposal, or the proposal is already terminal/non-pending). Carries no state change. Expiry is system policy, so
/// the deterministic no-transition outcomes (no expiry configured, expiry not reached) are decided in the orchestrator and
/// never dispatch a command — this structural rejection covers only the aggregate-level lifecycle guard (AC3; AD-3, AD-12).
/// </summary>
/// <param name="AgentInteractionId">The AgentInteraction aggregate id.</param>
/// <param name="Reason">The safe structural reason.</param>
public sealed record ProposedAgentReplyNotExpirableRejection(
    string AgentInteractionId,
    AgentProposedReplyNotExpirableReason Reason) : IRejectionEvent;
