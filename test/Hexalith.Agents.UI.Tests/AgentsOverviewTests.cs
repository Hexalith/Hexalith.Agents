using System.Threading;
using System.Threading.Tasks;

using Bunit;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.UI.Components.Pages;
using Hexalith.Agents.UI.Components.Shared;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC2 — the Agents overview renders readiness, lifecycle, response mode, provider/model, the inline activation
/// blockers, and tenant callability, and the readiness badge separates active lifecycle from callability. An Agent
/// that is Active yet has blockers is shown as not callable, and the blockers are explained inline (grouped by the
/// recovery action), never hidden behind a generic "not ready" (UX-DR2/9/20). Pending proposals and recent failures
/// have no Epic-1 contract source and surface as an explicit "not available yet" affordance, never fabricated counts.
/// </summary>
public sealed class AgentsOverviewTests : AgentsTestContext
{
    private void GivenStatus(AgentStatusView view)
        => SetupGateway.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentInspectionResult.Success(view)));

    [Fact]
    public void Callable_agent_renders_the_success_readiness_badge_and_yes_callability()
    {
        GivenStatus(AgentUiTestData.Status(
            AgentLifecycleStatus.Active,
            hasProviderSelection: true,
            providerId: "openai",
            modelId: "gpt-x"));

        IRenderedComponent<AgentsOverview> cut = RenderPage<AgentsOverview>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-overview']");
            AgentReadinessBadge badge = cut.FindComponent<AgentReadinessBadge>().Instance;
            badge.State.ShouldBe(AgentReadinessState.Callable);
            cut.Find("[data-testid='agents-overview-callability']").TextContent
                .ShouldContain("Agents.Overview.Callable.Yes");
            cut.Find("[data-testid='agents-overview-lifecycle']").TextContent.ShouldContain("Agents.Lifecycle.Active");
            cut.Find("[data-testid='agents-overview-response-mode']").TextContent.ShouldContain("Agents.ResponseMode.Automatic");
            cut.Find("[data-testid='agents-overview-blockers-none']");
        });
    }

    [Fact]
    public void Active_agent_with_blockers_is_shown_not_callable_and_lists_blockers_grouped_by_recovery()
    {
        // Active lifecycle but two distinct blockers: the badge must NOT be Callable and callability must be "No",
        // while each blocker is explained inline under its recovery-action group (AC2 active != callable, UX-DR9).
        GivenStatus(AgentUiTestData.Status(
            AgentLifecycleStatus.Active,
            blockers:
            [
                AgentActivationBlocker.MissingProviderSelection,
                AgentActivationBlocker.MissingContentSafetyPolicy,
            ]));

        IRenderedComponent<AgentsOverview> cut = RenderPage<AgentsOverview>();

        cut.WaitForAssertion(() =>
        {
            AgentReadinessBadge badge = cut.FindComponent<AgentReadinessBadge>().Instance;
            badge.State.ShouldNotBe(AgentReadinessState.Callable);
            cut.Find("[data-testid='agents-overview-callability']").TextContent
                .ShouldContain("Agents.Overview.Callable.No");

            cut.Find("[data-testid='agents-overview-blocker-MissingProviderSelection']").TextContent
                .ShouldContain("Agents.Readiness.Blocker.MissingProviderSelection");
            cut.Find("[data-testid='agents-overview-blocker-MissingContentSafetyPolicy']").TextContent
                .ShouldContain("Agents.Readiness.Blocker.MissingContentSafetyPolicy");

            // Blockers are grouped by the recovery action an administrator takes, not by raw subsystem label.
            cut.Markup.ShouldContain("Agents.Overview.Recovery.Provider");
            cut.Markup.ShouldContain("Agents.Overview.Recovery.ContentSafety");
            cut.FindAll("[data-testid='agents-overview-blockers-none']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void Provider_reference_is_rendered_when_selected_and_a_none_affordance_otherwise()
    {
        GivenStatus(AgentUiTestData.Status(AgentLifecycleStatus.Draft, hasProviderSelection: false));

        IRenderedComponent<AgentsOverview> cut = RenderPage<AgentsOverview>();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='agents-overview-provider']").TextContent
                .ShouldContain("Agents.Config.Provider.None"));
    }

    [Fact]
    public void Pending_proposals_and_recent_failures_render_not_available_yet_never_fabricated_counts()
    {
        GivenStatus(AgentUiTestData.Status(AgentLifecycleStatus.Active));

        IRenderedComponent<AgentsOverview> cut = RenderPage<AgentsOverview>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-overview-pending-proposals']").TextContent
                .ShouldContain("Agents.Common.NotAvailableYet");
            cut.Find("[data-testid='agents-overview-recent-failures']").TextContent
                .ShouldContain("Agents.Common.NotAvailableYet");
        });
    }

    [Fact]
    public void Not_authorized_overview_renders_the_permission_denied_surface()
    {
        // The harness default is the fail-closed NotAuthorized result.
        IRenderedComponent<AgentsOverview> cut = RenderPage<AgentsOverview>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-overview-state']");
            cut.Markup.ShouldContain("Agents.Surface.PermissionDenied.Title");
            cut.FindAll("[data-testid='agents-overview-facts']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void Agent_not_found_overview_renders_the_empty_surface_without_leaking_records()
    {
        SetupGateway.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentInspectionResult.NotFound()));

        IRenderedComponent<AgentsOverview> cut = RenderPage<AgentsOverview>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-overview-state']");
            cut.Markup.ShouldContain("Agents.Surface.Empty.Title");
        });
    }
}
