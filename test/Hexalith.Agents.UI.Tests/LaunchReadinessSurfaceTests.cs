using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Dom;

using Bunit;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.UI.Components.Pages;
using Hexalith.Agents.UI.Components.Shared;
using Hexalith.Agents.UI.Services.Gateways;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// Story 4.4 AC2, AC3, AC4 — the launch-readiness panel surfaces the production-like-enablement state and groups the
/// launch-readiness blockers by recovery action (never the raw subsystem), the page routes every state through the
/// surface kinds and fails closed against the default deferred gateway, the metric/latency/cost governance data renders
/// from safe fields only, and the enable affordance is gated by the recorded blockers.
/// </summary>
public sealed class LaunchReadinessSurfaceTests : AgentsTestContext
{
    [Fact]
    public void Page_fails_closed_to_permission_denied_against_the_default_deferred_gateway()
    {
        // AC4 / AD-12 — the default substitute returns NotAuthorized → the page renders the permission-denied surface.
        IRenderedComponent<LaunchReadiness> cut = RenderLaunchReadiness();

        cut.WaitForAssertion(() =>
        {
            IElement region = cut.Find("[data-testid='agents-launch-readiness-state']");
            region.ClassList.ShouldContain("agent-surface-state--permissiondenied");
            region.GetAttribute("role").ShouldBe("alert");
        });
    }

    [Fact]
    public void Page_renders_the_unavailable_surface_when_the_dependency_is_down()
    {
        // AC4 — an Unavailable read renders the assertive Unavailable surface (a down dependency/projection).
        LaunchReadinessGateway.GetLaunchReadinessAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(LaunchReadinessResult.Unavailable()));

        IRenderedComponent<LaunchReadiness> cut = RenderLaunchReadiness();

        cut.WaitForAssertion(() =>
        {
            IElement region = cut.Find("[data-testid='agents-launch-readiness-state']");
            region.ClassList.ShouldContain("agent-surface-state--unavailable");
            region.GetAttribute("aria-live").ShouldBe("assertive");
        });
    }

    [Fact]
    public void Page_renders_the_empty_surface_when_no_readiness_view_is_returned()
    {
        // AC4 — a not-found read carries no readiness view; the page renders the authorized Empty surface (no leak; AD-12).
        LaunchReadinessGateway.GetLaunchReadinessAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(LaunchReadinessResult.NotFound()));

        IRenderedComponent<LaunchReadiness> cut = RenderLaunchReadiness();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='agents-launch-readiness-state']").ClassList.ShouldContain("agent-surface-state--empty"));
    }

    [Fact]
    public void Page_renders_metrics_latency_cost_and_panel_from_safe_fields()
    {
        // AC2, AC3, AC4 — the recorded metric/latency/cost governance data renders, and the panel surfaces the gate state.
        LaunchReadinessGateway.GetLaunchReadinessAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.LaunchReadinessResultSuccess()));

        IRenderedComponent<LaunchReadiness> cut = RenderLaunchReadiness();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-launch-readiness-panel']");
            cut.Find("[data-testid='agents-launch-readiness-metric-SM-2']");
            // The per-mode latency rows render through the localized "{0} ms" whole string (the key-returning stub proves
            // a single whole string is resolved, not a runtime-assembled fragment; UX-DR14).
            cut.Find("[data-testid='agents-launch-readiness-latency-Automatic']").TextContent.Trim()
                .ShouldBe("Agents.LaunchReadiness.Latency.Milliseconds");
            cut.Find("[data-testid='agents-launch-readiness-latency-Confirmation']");
            cut.Find("[data-testid='agents-launch-readiness-cost-posture']").TextContent.Trim()
                .ShouldBe("Agents.LaunchReadiness.CostPosture.Budgets");
        });
    }

    [Fact]
    public void Panel_groups_blockers_by_recovery_action_with_color_icon_and_visible_text()
    {
        // AC4 / UX-DR9/12 — a policy blocker lands in FixPolicy, the audit-governance blocker in InspectAudit; each badge
        // carries a semantic color + icon + visible whole-string label (never color-only).
        AgentLaunchReadinessView view = AgentUiTestData.LaunchReadinessView(
            blockers: [AgentLaunchReadinessBlocker.MissingLaunchMetrics, AgentLaunchReadinessBlocker.UnresolvedAuditGovernance]);

        IRenderedComponent<LaunchReadinessPanel> cut = Render<LaunchReadinessPanel>(parameters => parameters
            .Add(panel => panel.Readiness, view));

        cut.Find("[data-testid='agents-launch-readiness-panel-group-FixPolicy']");
        cut.Find("[data-testid='agents-launch-readiness-panel-group-InspectAudit']");
        IElement blocker = cut.Find("[data-testid='agents-launch-readiness-blocker-MissingLaunchMetrics']");
        blocker.TextContent.Trim().ShouldBe("Agents.LaunchReadiness.Blocker.MissingLaunchMetrics");
        // The badge carries a semantic icon (an <svg>) alongside the visible text — color is never the sole signal.
        blocker.QuerySelector("svg").ShouldNotBeNull();
    }

    [Fact]
    public void Panel_shows_enabled_state_only_when_production_like_generation_is_enabled()
    {
        AgentLaunchReadinessView enabled = AgentUiTestData.LaunchReadinessView(productionLikeGenerationEnabled: true, blockers: []);

        IRenderedComponent<LaunchReadinessPanel> cut = Render<LaunchReadinessPanel>(parameters => parameters
            .Add(panel => panel.Readiness, enabled));

        cut.Find("[data-testid='agents-launch-readiness-panel-enablement']").TextContent
            .ShouldContain("Agents.LaunchReadiness.ProductionLike.Enabled");
    }

    [Fact]
    public void Enable_button_is_disabled_while_blockers_remain()
    {
        // AC4 — production-like generation cannot be enabled while a launch-readiness gate fails.
        LaunchReadinessGateway.GetLaunchReadinessAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.LaunchReadinessResultSuccess(
                AgentUiTestData.LaunchReadinessView(blockers: [AgentLaunchReadinessBlocker.MissingLaunchMetrics]))));

        IRenderedComponent<LaunchReadiness> cut = RenderLaunchReadiness();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='agents-launch-readiness-enable-button']").HasAttribute("disabled").ShouldBeTrue());
    }

    [Fact]
    public void Enable_button_is_actionable_and_announces_submission_when_all_gates_pass()
    {
        // AC4 — with no blockers and not yet enabled, the operator can request enablement; a safe localized status is shown.
        LaunchReadinessGateway.GetLaunchReadinessAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.LaunchReadinessResultSuccess(
                AgentUiTestData.LaunchReadinessView(blockers: [], productionLikeGenerationEnabled: false))));

        IRenderedComponent<LaunchReadiness> cut = RenderLaunchReadiness();

        IElement button = null!;
        cut.WaitForAssertion(() =>
        {
            button = cut.Find("[data-testid='agents-launch-readiness-enable-button']");
            button.HasAttribute("disabled").ShouldBeFalse();
        });

        button.Click();

        cut.Find("[data-testid='agents-launch-readiness-enable-status']").TextContent.Trim()
            .ShouldBe("Agents.LaunchReadiness.Enable.Submitted");
    }

    [Fact]
    public void Page_does_not_leak_a_cross_tenant_sentinel_in_markup_or_attributes()
    {
        // AD-14 — the view carries only safe governance data; no secret/cross-tenant sentinel ever reaches the markup.
        const string sentinel = "cross-tenant-leak-sentinel-9z8";
        LaunchReadinessGateway.GetLaunchReadinessAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.LaunchReadinessResultSuccess()));

        IRenderedComponent<LaunchReadiness> cut = RenderLaunchReadiness();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-launch-readiness-panel']");
            cut.Markup.ShouldNotContain(sentinel);
        });
    }

    private IRenderedComponent<LaunchReadiness> RenderLaunchReadiness() => RenderPage<LaunchReadiness>();
}
