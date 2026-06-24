using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The outcome discriminator of one server-assembled Proposed-Agent-Reply expiry evaluation (AC3; AD-3, AD-5). Expiry is
/// system policy, not an approver action: the expiry orchestrator compares the recorded <c>ExpiresAt</c> to a trusted
/// evaluation timestamp supplied on the request and classifies the result into exactly one of these. <see cref="Expired"/>
/// is the only success outcome (the elapsed time reached the recorded expiry); the other values are deterministic
/// no-transition outcomes and dispatch nothing.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never resolves to a concrete outcome; <see cref="Unknown"/> (ordinal 0) is the
/// fail-safe sentinel. Unlike rejection/abandonment, there is no approver verdict and no fail-closed "expiry-failed" terminal
/// status — a non-<see cref="Expired"/> outcome simply leaves the proposal pending (AD-3: the aggregate never reads the clock).
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentProposalExpiryOutcome
{
    /// <summary>Not-an-outcome sentinel — no transition (fail closed to no-expiry).</summary>
    Unknown = 0,

    /// <summary>The configured expiry elapsed against the trusted evaluation timestamp — the only success outcome (moves the proposal to <see cref="ProposedAgentReplyState.Expired"/>).</summary>
    Expired,

    /// <summary>The proposal was already terminal (rejected/abandoned/expired/posted) — handled as an idempotent no-op upstream.</summary>
    AlreadyTerminal,

    /// <summary>No proposal expiry policy is configured (no recorded <c>ExpiresAt</c>) — the proposal is unbounded; no transition.</summary>
    NoExpiryPolicy,

    /// <summary>The configured expiry has not yet elapsed against the trusted evaluation timestamp — the proposal stays pending; no transition.</summary>
    ExpiryNotReached,

    /// <summary>The proposal was not in a pending/expirable state.</summary>
    ProposalNotPending,
}
