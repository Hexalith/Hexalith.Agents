using System;
using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.Serialization;

namespace Hexalith.Agents.Contracts.Agent;

/// <summary>
/// The recorded cost-control posture checked before production-like generation is enabled. Only quotas, budgets,
/// and Provider/model limits are accepted for new readiness decisions. Historical prohibited values remain readable.
/// </summary>
/// <remarks>
/// Names and ordinals are stable for historical replay. Unknown and undefined values fail closed. A recorded
/// posture describes a governance decision; it does not supply proof of live enforcement or Provider exclusion.
/// </remarks>
[JsonConverter(typeof(UnknownFallbackEnumConverter<CostControlPosture>))]
public enum CostControlPosture
{
    /// <summary>Absent/unrecognized posture sentinel — fails readiness recording and enablement.</summary>
    Unknown = 0,

    /// <summary>Cost is controlled by usage quotas.</summary>
    Quotas = 1,

    /// <summary>Cost is controlled by spending budgets.</summary>
    Budgets = 2,

    /// <summary>Cost is controlled by Provider/model limits.</summary>
    ProviderModelLimits = 3,

    /// <summary>Historical reporting-only monitoring without hard enforcement; prohibited for readiness.</summary>
    [Obsolete("Reporting-only monitoring is prohibited. Record quotas, budgets, or Provider/model limits instead.")]
    ReportingOnlyMonitoring = 4,

    /// <summary>Historical acceptance of uncontrolled cost as a launch risk; prohibited for readiness.</summary>
    [Obsolete("Accepted launch risk is prohibited. Record quotas, budgets, or Provider/model limits instead.")]
    AcceptedLaunchRisk = 5,
}
