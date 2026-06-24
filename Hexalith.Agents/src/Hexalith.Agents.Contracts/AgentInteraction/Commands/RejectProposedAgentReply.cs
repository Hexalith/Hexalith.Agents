namespace Hexalith.Agents.Contracts.AgentInteraction.Commands;

/// <summary>
/// Moves a pending Proposed Agent Reply to the <see cref="ProposedAgentReplyState.Rejected"/> terminal state, recording the
/// trusted rejection decision — a terminal transition, not a new version (AC1, AC4; FR-18, FR-7; AD-3, AD-5, AD-13, AD-14).
/// The pure aggregate validates the precondition (the proposal must be pending/edited/regenerated) and maps the
/// deterministic outcome carried in <see cref="Result"/> to the success/failure event — it never resolves authorization or
/// reads any dependency itself. No Conversation side effect occurs; every preserved version is retained.
/// </summary>
/// <remarks>
/// The <see cref="Result"/> is <b>server-assembled</b> by <c>AgentInteractionProposalRejectionOrchestrator</c> (Story 3.6),
/// which resolves rejection-time approver authorization and assembles the safe result. Any client-supplied value is
/// stripped/overwritten by the orchestrator (AD-3 round-trip). This command carries NO content — only safe ids + the policy
/// basis + an optional rationale code (AD-14).
/// </remarks>
/// <param name="AgentInteractionId">The Agent Call aggregate identifier the proposal rejection targets (mirrors the envelope's aggregate id).</param>
/// <param name="Result">The server-assembled rejection outcome the aggregate decides on (safe ids + policy basis only).</param>
public sealed record RejectProposedAgentReply(
    string AgentInteractionId,
    AgentProposalRejectionResult Result);
