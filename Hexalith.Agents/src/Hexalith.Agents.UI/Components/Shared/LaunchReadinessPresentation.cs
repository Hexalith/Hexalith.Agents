using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.UI.Components.Shared;

/// <summary>
/// Pure, dependency-free mapping from every launch-readiness contract value (<see cref="AgentLaunchReadinessBlocker"/>,
/// <see cref="LaunchMetricClassification"/>, <see cref="CostControlPosture"/>, and the per-mode latency
/// <see cref="AgentResponseMode"/>) onto its <see cref="RecoveryActionGroup"/> and whole-string localization keys
/// (Story 4.4 AC2, AC3, AC4). Recovery guidance is grouped by the operator <em>action</em>, never the raw subsystem
/// (UX-DR9), and every label is a localized whole string with no runtime-assembled fragments (UX-DR14). Mirrors
/// <see cref="OperationalStatusPresentation.GroupForBlocker(AgentActivationBlocker)"/> and reuses the
/// <see cref="AgentReadiness.BlockerKeyFor(AgentActivationBlocker)"/> key idiom. The mapping never exposes any secret,
/// payload, content, or PII (AD-14). Every switch is <b>total</b> so the <c>Unknown</c> sentinels render through a safe
/// default.
/// </summary>
public static class LaunchReadinessPresentation
{
    /// <summary>Maps a launch-readiness blocker to its recovery-action group (group by action, not subsystem; UX-DR9).</summary>
    /// <param name="blocker">The launch-readiness blocker.</param>
    /// <returns>The recovery-action group.</returns>
    public static RecoveryActionGroup GroupForBlocker(AgentLaunchReadinessBlocker blocker)
        => blocker switch
        {
            AgentLaunchReadinessBlocker.MissingContentSafetyPolicy => RecoveryActionGroup.FixPolicy,
            AgentLaunchReadinessBlocker.MissingContextPolicy => RecoveryActionGroup.FixPolicy,
            AgentLaunchReadinessBlocker.MissingLaunchMetrics => RecoveryActionGroup.FixPolicy,
            AgentLaunchReadinessBlocker.IncompleteLaunchMetricDefinition => RecoveryActionGroup.FixPolicy,
            AgentLaunchReadinessBlocker.MissingAutomaticLatencyTarget => RecoveryActionGroup.FixPolicy,
            AgentLaunchReadinessBlocker.MissingConfirmationLatencyTarget => RecoveryActionGroup.FixPolicy,
            AgentLaunchReadinessBlocker.MissingCostControlPosture => RecoveryActionGroup.FixPolicy,
            AgentLaunchReadinessBlocker.UnresolvedAuditGovernance => RecoveryActionGroup.InspectAudit,
            _ => RecoveryActionGroup.FixPolicy,
        };

    /// <summary>The whole-string localization key for a launch-readiness blocker reason (one key per blocker; UX-DR14).</summary>
    /// <param name="blocker">The launch-readiness blocker.</param>
    /// <returns>The resource key.</returns>
    public static string BlockerKeyFor(AgentLaunchReadinessBlocker blocker)
        => $"Agents.LaunchReadiness.Blocker.{blocker}";

    /// <summary>The whole-string localization key for a launch-metric classification label (UX-DR14).</summary>
    /// <param name="classification">The launch-metric classification.</param>
    /// <returns>The resource key.</returns>
    public static string ClassificationKeyFor(LaunchMetricClassification classification)
        => $"Agents.LaunchReadiness.Classification.{classification}";

    /// <summary>The whole-string localization key for a cost-control posture label (UX-DR14).</summary>
    /// <param name="posture">The cost-control posture.</param>
    /// <returns>The resource key.</returns>
    public static string CostPostureKeyFor(CostControlPosture posture)
        => $"Agents.LaunchReadiness.CostPosture.{posture}";

    /// <summary>The whole-string localization key for a per-mode latency-target label (UX-DR14).</summary>
    /// <param name="mode">The response mode the latency target applies to.</param>
    /// <returns>The resource key.</returns>
    public static string LatencyModeKeyFor(AgentResponseMode mode)
        => $"Agents.LaunchReadiness.Latency.{mode}";
}
