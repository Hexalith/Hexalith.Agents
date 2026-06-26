using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The safe classification of <em>why</em> automatic posting failed closed AFTER successful generation (AC1, AC4; AD-7,
/// AD-12, AD-14). Recorded on <see cref="Events.AgentResponsePostingFailed"/> as fail-closed Audit Evidence so an
/// administrator can distinguish the failure class (party/membership/conversation/append) without any generated content,
/// raw Conversations payload, provider-specific error text, or secret.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never resolves to a concrete reason; <see cref="Unknown"/> (ordinal 0) is the
/// fail-safe sentinel. The reasons are deliberately coarse and content-free — they carry no Conversations SDK detail.
/// Mirrors <see cref="AgentOutputGenerationFailureReason"/>.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentResponsePostingFailureReason
{
    /// <summary>Not-a-reason sentinel — an absent/unrecognized classification never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>The Agent's linked Party identity is missing, not linked, disabled, or unavailable — posting must not proceed (AD-7).</summary>
    PartyIdentityUnavailable,

    /// <summary>The Agent could not be established as an <c>AiAgent</c> participant because the Conversations membership seam is missing/unavailable — fail closed (AD-6, AD-7).</summary>
    MembershipUnavailable,

    /// <summary>Conversations rejected the Agent's <c>AiAgent</c> participation — fail closed.</summary>
    MembershipRejected,

    /// <summary>The Source Conversation is missing, unauthorized, or stale — posting must not proceed.</summary>
    ConversationUnavailable,

    /// <summary>Conversations rejected the message append — fail closed.</summary>
    PostRejected,

    /// <summary>The posting adapter failed (it threw or returned a fail-closed adapter outcome) — no Conversations error text is exposed (AD-14).</summary>
    AdapterFailure,
}
