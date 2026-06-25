using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// A specific reason an Agent (<c>hexa</c>) cannot have production-like generation enabled (Story 4.4 AC1, AC4;
/// FR-28). This is the higher, distinct gate above baseline <see cref="AgentActivationBlocker"/> activation
/// (dev/staging): launch readiness gates production-like enablement. Returned both on the enablement rejection (so a
/// Release Operator sees exactly what to fix) and on the status/operational views, so the readiness surface explains
/// blockers rather than hiding them.
/// </summary>
/// <remarks>
/// Blockers are safe by construction — they classify <em>which</em> required readiness decision is missing/invalid
/// and never carry secrets, raw payloads, or content (AD-14). Serialized by name so an absent value never
/// deserializes to a concrete blocker. The enum is <em>additively extensible</em> (ordinals stable; AD-17).
/// <see cref="Unknown"/> (ordinal 0) is the unrecognized sentinel.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AgentLaunchReadinessBlocker
{
    /// <summary>Absent/unrecognized blocker sentinel.</summary>
    Unknown = 0,

    /// <summary>No active Content Safety Policy is configured (AC1; references the Story 1.7 policy).</summary>
    MissingContentSafetyPolicy,

    /// <summary>No Conversation Context Policy is in force (AC1; references the Story 2.3 context policy).</summary>
    MissingContextPolicy,

    /// <summary>No launch metrics have been recorded (AC2).</summary>
    MissingLaunchMetrics,

    /// <summary>A recorded launch metric is missing a required field (numerator/denominator/target/window/cohort/classification; AC2).</summary>
    IncompleteLaunchMetricDefinition,

    /// <summary>No explicit Automatic Response Mode latency target is recorded (AC3).</summary>
    MissingAutomaticLatencyTarget,

    /// <summary>No explicit Confirmation Response Mode latency target is recorded (AC3).</summary>
    MissingConfirmationLatencyTarget,

    /// <summary>No cost-control posture is recorded (AC3).</summary>
    MissingCostControlPosture,

    /// <summary>The Agents audit-evidence governance is unresolved (AC1; consumes the Story 4.2 audit-governance launch-readiness blocker).</summary>
    UnresolvedAuditGovernance,
}
