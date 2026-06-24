namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// The fail-closed classification of an Agent-Party validity read for posting (Story 2.5; AC1; AD-7, AD-12). Only
/// <see cref="Available"/> permits posting; every other value fails closed to <c>PartyIdentityUnavailable</c>.
/// </summary>
public enum AgentPartyReadOutcome
{
    /// <summary>Not-an-outcome sentinel — treated as not-available (fail closed).</summary>
    Unknown = 0,

    /// <summary>The Agent has a valid, enabled, linked Party identity that is available for posting.</summary>
    Available,

    /// <summary>The Agent has no linked Party identity — posting must not proceed (AD-7).</summary>
    NotLinked,

    /// <summary>The Agent (or its linked Party) is disabled — posting must not proceed.</summary>
    Disabled,

    /// <summary>The Agent-Party state could not be read (missing, not-found, or read failure) — fail closed.</summary>
    Unavailable,
}

/// <summary>
/// Server-internal result of reading an Agent's linked Party identity + validity at posting time (Story 2.5; AC1; AD-7,
/// AD-12). It carries the fail-closed <see cref="Outcome"/> and the resolved <see cref="PartyId"/> (present only on an
/// <see cref="AgentPartyReadOutcome.Available"/> read).
/// </summary>
/// <remarks>
/// This is not a public contract — it is a Server-only port type (mirroring <c>AgentContentSafetyPolicyReadResult</c>).
/// The <c>AgentInteractionSnapshot</c> does NOT carry the Agent's <c>PartyId</c> (only provider/model/versions/response
/// mode), and AD-7 requires a posting-time Party-validity gate, so the live reader reads it fresh from the Agent
/// read-model and never trusts a stale snapshot. <see cref="PartyId"/> is a stable Party reference, not PII (AD-7).
/// </remarks>
/// <param name="Outcome">The fail-closed read classification (only <see cref="AgentPartyReadOutcome.Available"/> permits posting).</param>
/// <param name="PartyId">The Agent's linked Party reference (non-null only on an available read), or <see langword="null"/>.</param>
public sealed record AgentPartyReadResult(
    AgentPartyReadOutcome Outcome,
    string? PartyId)
{
    /// <summary>Gets the fail-closed not-available result (the deferred default) — no Party resolved.</summary>
    public static AgentPartyReadResult Unavailable { get; } = new(AgentPartyReadOutcome.Unavailable, null);
}
