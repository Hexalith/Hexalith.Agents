namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Server-assembled expiry result consumed by the pure aggregate (AC3; AD-3, AD-5). The expiry orchestration compares the
/// recorded <see cref="ExpiresAt"/> to a trusted evaluation timestamp supplied on the request and assembles this when (and
/// only when) the expiry elapsed, then puts it on <see cref="Commands.ExpireProposedAgentReply"/>. Expiry is system policy:
/// there is no approver verdict (AD-3 — the aggregate never reads the clock; "now" is decided outside and the elapsed
/// <see cref="ExpiresAt"/> is the sole authority). It carries safe ids + the recorded expiry timestamp only.
/// </summary>
/// <param name="Outcome">The expiry outcome classification the aggregate decides on (only <see cref="AgentProposalExpiryOutcome.Expired"/> transitions).</param>
/// <param name="ProposalId">The deterministic proposal identifier the expiry targets (AD-13).</param>
/// <param name="SourceConversationId">The source Conversation reference the proposal is linked to (an opaque reference — AD-6).</param>
/// <param name="ExpiresAt">The recorded ISO-8601 expiry timestamp that elapsed (the deterministic expiry authority; never the clock read inside the aggregate).</param>
public sealed record AgentProposalExpiryResult(
    AgentProposalExpiryOutcome Outcome,
    string ProposalId,
    string SourceConversationId,
    string? ExpiresAt);
