using System.Collections.Generic;
using System.Linq;

using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Agent;

/// <summary>
/// Pure, dependency-free launch-readiness gate rules shared by <see cref="AgentAggregate"/> (the
/// production-like enablement gate) and the read path (<see cref="AgentLaunchReadinessInspection"/>) so the status
/// view's launch-readiness blockers always match what an enablement attempt would reject (Story 4.4 AC1, AC4).
/// </summary>
/// <remarks>
/// These are <em>pure state checks</em> over internal Agent state — no trusted verdict and no dependency read. The
/// only externally-resolved input is <c>auditGovernanceResolved</c>, which the Server read/orchestration layer
/// resolves from the <c>IAgentAuditGovernanceReadinessProvider</c> port and passes in as a bool (never read from the
/// pure aggregate/policy; AD-3). Because an empty/invalid readiness is rejected at recording time, a recorded
/// readiness is exactly a valid one, so a present-and-recorded readiness clears the readiness-derived blockers and a
/// missing readiness fails them all (fail-closed; AD-12). Mirrors <see cref="AgentConfigurationPolicy.ComputeActivationBlockers"/>.
/// </remarks>
internal static class AgentLaunchReadinessPolicy
{
    /// <summary>
    /// Computes the current launch-readiness blockers preventing production-like enablement (AC1, AC4). An empty list
    /// means the Agent may have production-like generation enabled. Order is stable and deterministic: content safety
    /// → context policy → launch metrics present → metric definitions complete → automatic latency target →
    /// confirmation latency target → cost posture → audit governance.
    /// </summary>
    /// <param name="hasContentSafetyPolicy">Whether an active Content Safety Policy is configured (Story 1.7 state).</param>
    /// <param name="hasContextPolicy">Whether a Conversation Context Policy is in force (the recorded context-policy reference is present).</param>
    /// <param name="readiness">The recorded launch-readiness decision (<see langword="null"/> = none recorded; fails every readiness-derived gate).</param>
    /// <param name="auditGovernanceResolved">Whether the Agents audit-evidence governance is resolved (the Story 4.2 port verdict, resolved in the Server layer).</param>
    /// <returns>The specific launch-readiness blockers (empty when none).</returns>
    internal static IReadOnlyList<AgentLaunchReadinessBlocker> ComputeLaunchReadinessBlockers(
        bool hasContentSafetyPolicy,
        bool hasContextPolicy,
        AgentLaunchReadiness? readiness,
        bool auditGovernanceResolved)
    {
        var blockers = new List<AgentLaunchReadinessBlocker>();

        if (!hasContentSafetyPolicy)
        {
            blockers.Add(AgentLaunchReadinessBlocker.MissingContentSafetyPolicy);
        }

        if (!hasContextPolicy)
        {
            blockers.Add(AgentLaunchReadinessBlocker.MissingContextPolicy);
        }

        IReadOnlyList<LaunchMetricDefinition> metrics = readiness?.Metrics ?? [];
        if (metrics.Count == 0)
        {
            blockers.Add(AgentLaunchReadinessBlocker.MissingLaunchMetrics);
        }
        else if (metrics.Any(metric => !IsMetricComplete(metric)))
        {
            blockers.Add(AgentLaunchReadinessBlocker.IncompleteLaunchMetricDefinition);
        }

        IReadOnlyList<ResponseModeLatencyTarget> latencyTargets = readiness?.LatencyTargets ?? [];
        if (!latencyTargets.Any(target => target.Mode == AgentResponseMode.Automatic))
        {
            blockers.Add(AgentLaunchReadinessBlocker.MissingAutomaticLatencyTarget);
        }

        if (!latencyTargets.Any(target => target.Mode == AgentResponseMode.Confirmation))
        {
            blockers.Add(AgentLaunchReadinessBlocker.MissingConfirmationLatencyTarget);
        }

        if ((readiness?.CostPosture ?? CostControlPosture.Unknown) == CostControlPosture.Unknown)
        {
            blockers.Add(AgentLaunchReadinessBlocker.MissingCostControlPosture);
        }

        if (!auditGovernanceResolved)
        {
            blockers.Add(AgentLaunchReadinessBlocker.UnresolvedAuditGovernance);
        }

        return blockers;
    }

    /// <summary>
    /// Whether a launch metric definition is complete: a concrete classification and all five governance descriptors
    /// present (AC2). Defensive — recording validation already enforces completeness, so this never fires for a
    /// recorded readiness, but keeping the check pure keeps the read path robust to any future recording path.
    /// </summary>
    /// <param name="metric">The metric definition to check.</param>
    /// <returns><see langword="true"/> when complete.</returns>
    internal static bool IsMetricComplete(LaunchMetricDefinition metric)
        => metric is not null
            && metric.Classification != LaunchMetricClassification.Unknown
            && !string.IsNullOrWhiteSpace(metric.MetricId)
            && !string.IsNullOrWhiteSpace(metric.Numerator)
            && !string.IsNullOrWhiteSpace(metric.Denominator)
            && !string.IsNullOrWhiteSpace(metric.MeasurementWindow)
            && !string.IsNullOrWhiteSpace(metric.LaunchCohort);
}
