using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// Lifecycle status of one Agent Call (<c>AgentInteraction</c>) (AC2; FR-8). Story 2.1 introduces only the
/// initial <see cref="Requested"/> state; later states (authorized/denied, context-loading, generating, posted)
/// are appended <em>additively</em> by Stories 2.2–2.5 without reshaping this enum or its ordinals (AD-2).
/// </summary>
/// <remarks>
/// <see cref="Unknown"/> (ordinal 0) is the "not-yet-known" sentinel: an absent/unrecognized status must never
/// deserialize to a concrete state. Serialized by name so a missing value never resolves to <see cref="Requested"/>.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentInteractionStatus
{
    /// <summary>Not-yet-known sentinel — an absent/unrecognized status never resolves to a concrete state.</summary>
    Unknown = 0,

    /// <summary>The Agent Call request record was created with its configuration snapshot (Story 2.1).</summary>
    Requested,
}
