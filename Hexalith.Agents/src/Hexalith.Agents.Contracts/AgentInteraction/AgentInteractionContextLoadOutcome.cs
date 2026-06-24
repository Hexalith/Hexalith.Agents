using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The server-assembled classification of the authorized Conversations read used for context building (AC1, AC3;
/// AD-6, AD-12). The orchestration assembles this from the live <c>GetConversationAsync</c> result; only
/// <see cref="Loaded"/> (authorized + fresh enough) permits context building. The three non-<see cref="Loaded"/>
/// values are AD-12's fail-closed vocabulary mapped from the Conversations detail result
/// (<c>Hidden</c>/<c>Redacted</c> → <see cref="Unauthorized"/>, <c>Unavailable</c>/<c>Rebuilding</c> →
/// <see cref="Unavailable"/>, not-trust-bearing freshness → <see cref="Stale"/>).
/// </summary>
/// <remarks>
/// A coarse classification carrying no raw content (AD-14). <see cref="Unauthorized"/> and <see cref="Unavailable"/>
/// are returned identically whether a conversation is absent or cross-tenant, so a probe cannot learn whether it
/// exists in another tenant (AC1). Serialized by name so an absent value never resolves to a concrete outcome;
/// <see cref="Unknown"/> (ordinal 0) is the fail-safe sentinel (treated as a degraded read that fails closed).
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentInteractionContextLoadOutcome
{
    /// <summary>Absent/unrecognized outcome sentinel — treated as a degraded read and fails closed.</summary>
    Unknown = 0,

    /// <summary>The Source Conversation was loaded authorized and fresh enough; context building may proceed.</summary>
    Loaded,

    /// <summary>The caller is not authorized for the Source Conversation, or it is outside the Agent's tenant scope (access denied).</summary>
    Unauthorized,

    /// <summary>The Source Conversation could not be loaded fresh enough (not trust-bearing) — fail closed.</summary>
    Stale,

    /// <summary>The Source Conversation could not be read at all (transport failure, reader threw, or not-available) — fail closed.</summary>
    Unavailable,
}
