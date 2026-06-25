using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.UI.Components.Shared;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// Story 4.4 AC2/AC3/AC4 — pure-mapper coverage for <see cref="LaunchReadinessPresentation"/>. The surface tests only
/// render two of the eight launch-readiness blockers, so these pin every blocker's recovery-action group (grouped by
/// operator <em>action</em>, never the raw subsystem; UX-DR9), prove the <c>Unknown</c> sentinel falls through to a safe
/// default (the switch is total), and assert the whole-string localization keys are one stable key per value with no
/// runtime-assembled fragment (UX-DR14).
/// </summary>
public sealed class LaunchReadinessPresentationTests
{
    [Theory]
    [InlineData(AgentLaunchReadinessBlocker.MissingContentSafetyPolicy, RecoveryActionGroup.FixPolicy)]
    [InlineData(AgentLaunchReadinessBlocker.MissingContextPolicy, RecoveryActionGroup.FixPolicy)]
    [InlineData(AgentLaunchReadinessBlocker.MissingLaunchMetrics, RecoveryActionGroup.FixPolicy)]
    [InlineData(AgentLaunchReadinessBlocker.IncompleteLaunchMetricDefinition, RecoveryActionGroup.FixPolicy)]
    [InlineData(AgentLaunchReadinessBlocker.MissingAutomaticLatencyTarget, RecoveryActionGroup.FixPolicy)]
    [InlineData(AgentLaunchReadinessBlocker.MissingConfirmationLatencyTarget, RecoveryActionGroup.FixPolicy)]
    [InlineData(AgentLaunchReadinessBlocker.MissingCostControlPosture, RecoveryActionGroup.FixPolicy)]
    [InlineData(AgentLaunchReadinessBlocker.UnresolvedAuditGovernance, RecoveryActionGroup.InspectAudit)]
    public void Every_blocker_maps_to_its_recovery_action_group(AgentLaunchReadinessBlocker blocker, RecoveryActionGroup expected)
        => LaunchReadinessPresentation.GroupForBlocker(blocker).ShouldBe(expected);

    [Fact]
    public void Unknown_blocker_falls_through_to_the_safe_fix_policy_default()
        => LaunchReadinessPresentation.GroupForBlocker(AgentLaunchReadinessBlocker.Unknown).ShouldBe(RecoveryActionGroup.FixPolicy);

    [Fact]
    public void Blocker_key_is_a_single_whole_string_per_blocker()
    {
        LaunchReadinessPresentation.BlockerKeyFor(AgentLaunchReadinessBlocker.MissingLaunchMetrics)
            .ShouldBe("Agents.LaunchReadiness.Blocker.MissingLaunchMetrics");
        LaunchReadinessPresentation.BlockerKeyFor(AgentLaunchReadinessBlocker.UnresolvedAuditGovernance)
            .ShouldBe("Agents.LaunchReadiness.Blocker.UnresolvedAuditGovernance");
    }

    [Theory]
    [InlineData(LaunchMetricClassification.Primary, "Agents.LaunchReadiness.Classification.Primary")]
    [InlineData(LaunchMetricClassification.Secondary, "Agents.LaunchReadiness.Classification.Secondary")]
    [InlineData(LaunchMetricClassification.Counter, "Agents.LaunchReadiness.Classification.Counter")]
    public void Classification_key_is_a_single_whole_string_per_classification(LaunchMetricClassification classification, string expected)
        => LaunchReadinessPresentation.ClassificationKeyFor(classification).ShouldBe(expected);

    [Theory]
    [InlineData(CostControlPosture.Quotas, "Agents.LaunchReadiness.CostPosture.Quotas")]
    [InlineData(CostControlPosture.Budgets, "Agents.LaunchReadiness.CostPosture.Budgets")]
    [InlineData(CostControlPosture.ProviderModelLimits, "Agents.LaunchReadiness.CostPosture.ProviderModelLimits")]
    [InlineData(CostControlPosture.ReportingOnlyMonitoring, "Agents.LaunchReadiness.CostPosture.ReportingOnlyMonitoring")]
    [InlineData(CostControlPosture.AcceptedLaunchRisk, "Agents.LaunchReadiness.CostPosture.AcceptedLaunchRisk")]
    public void Cost_posture_key_is_a_single_whole_string_per_posture(CostControlPosture posture, string expected)
        => LaunchReadinessPresentation.CostPostureKeyFor(posture).ShouldBe(expected);

    [Theory]
    [InlineData(AgentResponseMode.Automatic, "Agents.LaunchReadiness.Latency.Automatic")]
    [InlineData(AgentResponseMode.Confirmation, "Agents.LaunchReadiness.Latency.Confirmation")]
    public void Latency_mode_key_is_a_single_whole_string_per_mode(AgentResponseMode mode, string expected)
        => LaunchReadinessPresentation.LatencyModeKeyFor(mode).ShouldBe(expected);
}
