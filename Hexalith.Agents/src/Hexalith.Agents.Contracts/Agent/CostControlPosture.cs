using System.Text.Json.Serialization;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// The recorded cost-control posture required before production-like generation is enabled (Story 4.4 AC3; FR-28;
/// PRD §7 "Cost Control"). Cost controls are recorded as quotas, budgets, Provider/model limits, reporting-only
/// monitoring, or an explicitly accepted launch risk — never left implicit.
/// </summary>
/// <remarks>
/// Serialized by name so an absent value never deserializes to a concrete posture. <see cref="Unknown"/>
/// (ordinal 0) is the not-yet-decided sentinel — a recorded readiness must carry a concrete posture, so
/// <see cref="Unknown"/> is rejected at recording time and can never satisfy the readiness gate. The concrete
/// cost-control choice is itself an accepted downstream governance blocker (OQ-6); this enum records which posture
/// kind was chosen, not the provider-specific numbers.
/// </remarks>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum CostControlPosture
{
    /// <summary>Absent/unrecognized posture sentinel — fails readiness recording.</summary>
    Unknown = 0,

    /// <summary>Cost is controlled by usage quotas.</summary>
    Quotas,

    /// <summary>Cost is controlled by spending budgets.</summary>
    Budgets,

    /// <summary>Cost is controlled by Provider/model limits.</summary>
    ProviderModelLimits,

    /// <summary>Cost is observed by reporting-only monitoring (no hard enforcement).</summary>
    ReportingOnlyMonitoring,

    /// <summary>Uncontrolled cost is an explicitly accepted launch risk.</summary>
    AcceptedLaunchRisk,
}
