namespace Hexalith.Agents.Contracts.AgentInteraction.Commands;

/// <summary>
/// Moves a pending Proposed Agent Reply to the <see cref="ProposedAgentReplyState.Expired"/> terminal state when its
/// configured expiry has elapsed — a deterministic, system-policy terminal transition, not a new version (AC3; FR-18; AD-3,
/// AD-5, AD-13). The pure aggregate validates the precondition (the proposal must be pending/edited/regenerated) and maps the
/// deterministic outcome carried in <see cref="Result"/> to the expired event — it never reads the clock itself (AD-3:
/// expiry "now" is decided by the orchestrator/durable owner and the elapsed <c>ExpiresAt</c> rides the result).
/// </summary>
/// <remarks>
/// The <see cref="Result"/> is <b>server-assembled</b> by <c>AgentInteractionProposalExpiryOrchestrator</c> (Story 3.6),
/// which compares the recorded <c>ExpiresAt</c> to a trusted evaluation timestamp and dispatches this command ONLY when the
/// expiry elapsed. Expiry requires no approver authorization (system policy) but stays tenant-scoped (FR-19). This command
/// carries NO content — only safe ids + the recorded expiry timestamp (AD-14).
/// </remarks>
/// <param name="AgentInteractionId">The Agent Call aggregate identifier the proposal expiry targets (mirrors the envelope's aggregate id).</param>
/// <param name="Result">The server-assembled expiry outcome the aggregate decides on (safe ids + recorded expiry only).</param>
public sealed record ExpireProposedAgentReply(
    string AgentInteractionId,
    AgentProposalExpiryResult Result);
