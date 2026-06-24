using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The outcome discriminator of one server-assembled Proposed-Agent-Reply rejection attempt (AC1, AC4; AD-3, AD-5, AD-12).
/// The rejection orchestrator classifies the authorized rejection assembly into exactly one of these; the pure policy maps it
/// to the terminal event + status. <see cref="Rejected"/> is the only success outcome (and only when the authorization
/// verdict is <c>Valid</c>); every other value is a fail-closed class.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never resolves to a concrete outcome; <see cref="Unknown"/> (ordinal 0) is the
/// fail-safe sentinel (treated as a rejection failure). Mirrors <see cref="AgentProposalAbandonmentOutcome"/>.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentProposalRejectionOutcome
{
    /// <summary>Not-an-outcome sentinel — treated as a rejection failure (fail closed).</summary>
    Unknown = 0,

    /// <summary>An authorized rejection was assembled — the only success outcome (moves the proposal to <see cref="ProposedAgentReplyState.Rejected"/>).</summary>
    Rejected,

    /// <summary>The proposal was already terminal (rejected/abandoned/expired/posted) — handled as an idempotent no-op upstream.</summary>
    AlreadyTerminal,

    /// <summary>The approver policy did not authorize the rejection (fail closed).</summary>
    NotAuthorized,

    /// <summary>The policy basis could not be resolved safely (fail closed).</summary>
    PolicyFailure,

    /// <summary>The proposal was not in a pending/rejectable state.</summary>
    ProposalNotPending,
}
