using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// How a launch-readiness metric is classified for governance (Story 4.4 AC2; FR-28; PRD §11). Distinguishing
/// <see cref="Primary"/> (SM-1…SM-3), <see cref="Secondary"/> (SM-4…SM-6), and <see cref="Counter"/> (SM-C1…SM-C3)
/// metrics is the AC2 "distinguish primary/secondary/counter-metrics" requirement.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never deserializes to a concrete classification. <see cref="Unknown"/>
/// (ordinal 0) is the not-yet-classified sentinel — a recorded metric must carry a concrete classification, so
/// <see cref="Unknown"/> is rejected at recording time and can never satisfy the readiness gate.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum LaunchMetricClassification
{
    /// <summary>Absent/unrecognized classification sentinel — fails readiness recording.</summary>
    Unknown = 0,

    /// <summary>A primary launch success metric (SM-1…SM-3).</summary>
    Primary,

    /// <summary>A secondary launch success metric (SM-4…SM-6).</summary>
    Secondary,

    /// <summary>A counter-metric guarding against undesired side effects (SM-C1…SM-C3).</summary>
    Counter,
}
