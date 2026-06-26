using System.Text.Json.Serialization;

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
[JsonConverter(typeof(JsonStringEnumConverter))]
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

/// <summary>
/// The server-assembled input to the pure posting decision (AC1–AC4; AD-3, AD-14). The orchestration assembles this from
/// the Agent Party read, the selected generated version read, the Conversations membership ensure, and the message append,
/// then puts it on <see cref="Commands.PostAgentResponse"/>; the pure aggregate decides on it and never reads any
/// dependency itself (AD-3). It mirrors <see cref="AgentOutputGenerationResult"/> as the server→aggregate carrier.
/// </summary>
/// <remarks>
/// <b>Crucially, it carries NO generated content</b> — the content was already durably recorded on
/// <see cref="Events.AgentOutputGenerated"/>/state by Story 2.4; posting transports only safe ids into the aggregate
/// (AD-14). Every member is a safe id: the deterministic <see cref="MessageId"/> (AD-13), the opaque
/// <see cref="SourceConversationId"/>, the Agent's stable <see cref="AgentPartyId"/> (a reference, not PII — AD-7), and
/// the selected <see cref="PostedVersionId"/>. On a fail-closed outcome the ids are the ones that were attempted.
/// </remarks>
/// <param name="Outcome">The server-assembled outcome classification the aggregate decides on.</param>
/// <param name="MessageId">The deterministic Conversation Message identifier (reused across retries — AD-13).</param>
/// <param name="SourceConversationId">The source Conversation reference the message was appended to (an opaque reference — AD-6).</param>
/// <param name="AgentPartyId">The Agent's stable Party reference the message is authored by (a reference, not PII — AD-7).</param>
/// <param name="PostedVersionId">The selected generated version identifier that was posted (no content — AD-14).</param>
public record AgentResponsePostingResult(
    AgentResponsePostingOutcome Outcome,
    string MessageId,
    string SourceConversationId,
    string AgentPartyId,
    string PostedVersionId);
