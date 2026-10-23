using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The outcome discriminator of one server-assembled automatic-posting attempt (AC1–AC4; AD-3, AD-7). The orchestrator
/// classifies the Party read + membership ensure + message append into exactly one of these; the pure policy maps it to
/// the terminal event + status. <see cref="Posted"/> means membership was present/established AND the append succeeded;
/// every other value is a fail-closed class.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never resolves to a concrete outcome; <see cref="Unknown"/> (ordinal 0) is the
/// fail-safe sentinel (treated as a posting failure). The values mirror <see cref="AgentResponsePostingFailureReason"/>
/// (plus <see cref="Posted"/>) so the policy mapping is total. <see cref="MembershipUnavailable"/> is the fail-closed
/// value the deferred/seam-absent membership path returns.
/// </remarks>
[JsonConverter(typeof(UnknownFallbackEnumConverter<AgentResponsePostingOutcome>))]
public enum AgentResponsePostingOutcome
{
    /// <summary>Not-an-outcome sentinel — treated as a posting failure (fail closed).</summary>
    Unknown = 0,

    /// <summary>The Agent was present/established as an <c>AiAgent</c> participant and the message append succeeded — the only success outcome.</summary>
    Posted,

    /// <summary>The Agent's linked Party identity is missing, not linked, disabled, or unavailable.</summary>
    PartyIdentityUnavailable,

    /// <summary>The Conversations membership seam is missing/unavailable so the Agent could not be established as a participant — fail closed.</summary>
    MembershipUnavailable,

    /// <summary>Conversations rejected the Agent's <c>AiAgent</c> participation.</summary>
    MembershipRejected,

    /// <summary>The Source Conversation is missing, unauthorized, or stale.</summary>
    ConversationUnavailable,

    /// <summary>Conversations rejected the message append.</summary>
    PostRejected,

    /// <summary>The posting adapter failed (threw or returned a fail-closed adapter outcome), including the all-deferred default graph and an unavailable version read.</summary>
    AdapterFailure,
}
