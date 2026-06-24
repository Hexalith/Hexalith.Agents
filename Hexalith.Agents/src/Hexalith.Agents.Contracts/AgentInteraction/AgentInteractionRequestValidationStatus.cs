using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// The safe classification of <em>why</em> an Agent Call request failed structural validation in the pure
/// <c>AgentInteraction</c> aggregate (AC1, AC4; FR-8). It records which required field or snapshot scalar was
/// absent — never the raw prompt, Conversation-derived content, or caller PII (AD-14). Carried only on the typed
/// rejection event, never on the durable success event or the status surface.
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> (ordinal 0) is the fail-safe sentinel: an absent/unrecognized reason is treated as "not a
/// concrete validation reason". Serialized by name so an absent value never resolves to a concrete classification.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentInteractionRequestValidationStatus
{
    /// <summary>Not-a-reason sentinel — an absent/unrecognized classification never resolves to a concrete one.</summary>
    Unknown = 0,

    /// <summary>The caller <c>PartyId</c> reference was missing.</summary>
    MissingCaller,

    /// <summary>The source <c>ConversationId</c> reference was missing.</summary>
    MissingSourceConversation,

    /// <summary>The prompt was missing or blank.</summary>
    MissingPrompt,

    /// <summary>The server-assembled Agent configuration snapshot (or its required scalars) was absent (AC1 precondition not met).</summary>
    MissingAgentSnapshot,
}
