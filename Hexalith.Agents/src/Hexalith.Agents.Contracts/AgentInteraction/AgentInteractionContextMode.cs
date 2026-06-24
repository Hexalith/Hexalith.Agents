using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.AgentInteraction;

/// <summary>
/// How the Conversation context that satisfied the safe bounds was assembled (AC2, AC4; AD-11). <see cref="Full"/> =
/// the entire Source Conversation fit the selected Provider/model context budget after reserving configured output
/// tokens; <see cref="Bounded"/> = an explicitly approved bounded-context behavior was applied (its reference and
/// bounds are recorded as evidence — never a silent truncation).
/// </summary>
/// <remarks>
/// Carries no raw content (AD-14) — it is a coarse classification on the safe context evidence. Serialized by name so
/// an absent value never resolves to a concrete mode; <see cref="Unknown"/> (ordinal 0) is the sentinel (used for a
/// context-blocked record, which has no usable mode).
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentInteractionContextMode
{
    /// <summary>Not-a-mode sentinel — an absent/unrecognized mode never resolves to a concrete one (e.g. on a blocked record).</summary>
    Unknown = 0,

    /// <summary>The entire Source Conversation fit the model context budget after reserving configured output tokens.</summary>
    Full,

    /// <summary>An approved bounded-context behavior was applied; its reference and bounds are recorded as evidence (AC4).</summary>
    Bounded,
}
