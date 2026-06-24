namespace Hexalith.Agents.Contracts.AgentInteraction.Commands;

/// <summary>
/// Moves a pending Proposed Agent Reply to the <see cref="ProposedAgentReplyState.Abandoned"/> terminal state, recording the
/// trusted abandonment decision — a terminal transition, not a new version (AC2, AC4; FR-18, FR-7; AD-3, AD-5, AD-13,
/// AD-14). The pure aggregate validates the precondition (the proposal must be pending/edited/regenerated) and maps the
/// deterministic outcome carried in <see cref="Result"/> to the success/failure event — it never resolves authorization or
/// reads any dependency itself. An abandoned proposal can never be approved, edited, regenerated, or posted again.
/// </summary>
/// <remarks>
/// The <see cref="Result"/> is <b>server-assembled</b> by <c>AgentInteractionProposalAbandonmentOrchestrator</c> (Story 3.6),
/// which resolves abandonment-time approver authorization and assembles the safe result. Any client-supplied value is
/// stripped/overwritten by the orchestrator (AD-3 round-trip). This command carries NO content — only safe ids + the policy
/// basis (AD-14).
/// </remarks>
/// <param name="AgentInteractionId">The Agent Call aggregate identifier the proposal abandonment targets (mirrors the envelope's aggregate id).</param>
/// <param name="Result">The server-assembled abandonment outcome the aggregate decides on (safe ids + policy basis only).</param>
public sealed record AbandonProposedAgentReply(
    string AgentInteractionId,
    AgentProposalAbandonmentResult Result);
