using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Outcome of submitting a normalized Conversation-originated Agent Call request (AC1, AC3). The request seam returns a
/// structured status rather than throwing; only <see cref="Accepted"/> carries an
/// <see cref="AgentInteractionReference"/> safe handle, while <see cref="NotAuthorized"/>/<see cref="Rejected"/> carry
/// none so a failed request never reveals interaction identity (AD-12, AD-14). Mirrors the
/// <see cref="Agent.AgentInspectionStatus"/> shape.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentCallRequestStatus
{
    /// <summary>Not-yet-known sentinel — an absent/unrecognized status never resolves to a concrete outcome.</summary>
    Unknown = 0,

    /// <summary>The Agent Call was accepted; the result carries the safe <see cref="AgentInteractionReference"/> handle.</summary>
    Accepted,

    /// <summary>The caller is not authorized to call the Agent in this Conversation; no reference is returned (AD-12).</summary>
    NotAuthorized,

    /// <summary>The request was rejected (invalid or not acceptable); no reference is returned (AD-12).</summary>
    Rejected,
}
