using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// How a governed Agent (<c>hexa</c>) delivers generated content to a Conversation (AC1; FR-6). In
/// <see cref="Automatic"/> mode generated content is posted directly; in <see cref="Confirmation"/> mode a
/// configured Approver Policy must confirm before content reaches the Conversation. A mode change applies only to
/// future Agent Calls — prior interactions are never rewritten (AC1; AD-4).
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> (ordinal 0) is the "not-yet-configured" sentinel: an Agent created in Story 1.1 before a
/// mode was chosen must NOT be silently treated as <see cref="Automatic"/>. It <b>fails the activation gate</b>
/// (<see cref="AgentActivationBlocker.MissingResponseMode"/>) so an administrator must make an explicit, governed
/// choice before <c>hexa</c> can be activated. Serialized by name so an absent value never deserializes to a
/// concrete mode.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentResponseMode
{
    /// <summary>Not-yet-configured sentinel — fails the activation gate so no mode is ever assumed.</summary>
    Unknown = 0,

    /// <summary>Generated content is posted directly to the Conversation without prior approval.</summary>
    Automatic,

    /// <summary>Generated content requires a configured Approver Policy to confirm before it reaches the Conversation.</summary>
    Confirmation,
}
