using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Dom;

using Bunit;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.UI.Components.Pages;
using Hexalith.Agents.UI.Components.Shared;
using Hexalith.FrontComposer.Shell.Components.Layout;

using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC6 — setup pages compose the shell-owned skip links and landmarks, name their main landmark via the page
/// heading, expose a focusable heading target, and announce surface states with the correct live-region politeness
/// (assertive for error/permission-denied, polite otherwise).
/// </summary>
public sealed class AccessibilityTests : AgentsTestContext
{
    [Fact]
    public void Overview_in_shell_exposes_skip_links_and_named_content_landmark()
    {
        SetupGateway.GetStatusAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentInspectionResult.Success(AgentUiTestData.Status(AgentLifecycleStatus.Active))));

        IRenderedComponent<FrontComposerShell> cut = RenderInShellWithNavigation<AgentsOverview>();

        cut.WaitForAssertion(() =>
        {
            IElement[] anchors = cut.FindAll("a[href]").Take(2).ToArray();
            anchors.Length.ShouldBe(2);
            anchors[0].GetAttribute("href").ShouldBe("#fc-main-content");
            anchors[1].GetAttribute("href").ShouldBe("#fc-nav");

            IElement content = cut.Find("#fc-main-content");
            content.GetAttribute("role").ShouldBe("main");

            cut.Find("#fc-nav");
            cut.Find("#agents-overview-heading").GetAttribute("tabindex").ShouldBe("-1");
        });
    }

    [Theory]
    [InlineData(AgentSurfaceKind.Error, "alert", "assertive")]
    [InlineData(AgentSurfaceKind.PermissionDenied, "alert", "assertive")]
    [InlineData(AgentSurfaceKind.Loading, "status", "polite")]
    [InlineData(AgentSurfaceKind.Empty, "status", "polite")]
    [InlineData(AgentSurfaceKind.Stale, "status", "polite")]
    [InlineData(AgentSurfaceKind.FilteredEmpty, "status", "polite")]
    public void Surface_state_announces_with_the_correct_politeness(AgentSurfaceKind kind, string role, string ariaLive)
    {
        IRenderedComponent<AgentSurfaceState> cut = Render<AgentSurfaceState>(parameters => parameters
            .Add(state => state.Kind, kind)
            .Add(state => state.TestId, "surface"));

        IElement region = cut.Find("[data-testid='surface']");
        region.GetAttribute("role").ShouldBe(role);
        region.GetAttribute("aria-live").ShouldBe(ariaLive);
    }

    [Fact]
    public void Filtered_empty_surface_offers_a_filter_reset()
    {
        bool reset = false;
        IRenderedComponent<AgentSurfaceState> cut = Render<AgentSurfaceState>(parameters => parameters
            .Add(state => state.Kind, AgentSurfaceKind.FilteredEmpty)
            .Add(state => state.TestId, "surface")
            .Add(state => state.OnReset, () => reset = true));

        cut.Find("[data-testid='surface-reset']").Click();
        reset.ShouldBeTrue();
    }

    [Fact]
    public void Stale_surface_offers_a_refresh()
    {
        bool refreshed = false;
        IRenderedComponent<AgentSurfaceState> cut = Render<AgentSurfaceState>(parameters => parameters
            .Add(state => state.Kind, AgentSurfaceKind.Stale)
            .Add(state => state.TestId, "surface")
            .Add(state => state.OnRefresh, () => refreshed = true));

        cut.Find("[data-testid='surface-refresh']").Click();
        refreshed.ShouldBeTrue();
    }

    [Fact]
    public void Configuration_in_shell_exposes_a_focusable_heading()
    {
        SetupGateway.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentInspectionResult.Success(AgentUiTestData.Status(AgentLifecycleStatus.Draft))));

        IRenderedComponent<FrontComposerShell> cut = RenderInShellWithNavigation<AgentConfiguration>();

        cut.WaitForAssertion(() =>
            cut.Find("#agents-config-heading").GetAttribute("tabindex").ShouldBe("-1"));
    }

    [Fact]
    public void Provider_catalog_in_shell_exposes_a_focusable_heading()
    {
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([AgentUiTestData.Entry()])));

        IRenderedComponent<FrontComposerShell> cut = RenderInShellWithNavigation<ProviderCatalog>();

        cut.WaitForAssertion(() =>
            cut.Find("#agents-provider-catalog-heading").GetAttribute("tabindex").ShouldBe("-1"));
    }

    [Fact]
    public void Approver_policy_in_shell_exposes_a_focusable_heading()
    {
        SetupGateway.GetConfigurationAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentInspectionResult.Success(
                AgentUiTestData.Status(AgentLifecycleStatus.Draft, responseMode: AgentResponseMode.Confirmation))));

        IRenderedComponent<FrontComposerShell> cut = RenderInShellWithNavigation<ApproverPolicy>();

        cut.WaitForAssertion(() =>
            cut.Find("#agents-approver-policy-heading").GetAttribute("tabindex").ShouldBe("-1"));
    }

    [Fact]
    public void Proposal_queue_in_shell_exposes_a_focusable_heading()
    {
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.ProposalsResult(AgentUiTestData.PendingProposal())));

        IRenderedComponent<FrontComposerShell> cut = RenderInShellWithNavigation<ProposalQueue>();

        cut.WaitForAssertion(() =>
            cut.Find("#agents-proposal-queue-heading").GetAttribute("tabindex").ShouldBe("-1"));
    }

    [Fact]
    public void Conversation_call_in_shell_exposes_a_focusable_heading()
    {
        // The invocation page's heading is a focusable route target (tabindex=-1) regardless of the authorization
        // outcome — the FcPageHeader renders outside the surface branch, so the page is keyboard-reachable even when it
        // fails closed to permission-denied (AC4 keyboard/focus parity with the other four setup pages).
        IRenderedComponent<FrontComposerShell> cut = RenderInShellWithNavigation<ConversationCall>();

        cut.WaitForAssertion(() =>
            cut.Find("#agents-conversation-call-heading").GetAttribute("tabindex").ShouldBe("-1"));
    }

    [Fact]
    public void Call_status_feedback_exposes_a_named_polite_status_live_region_perceivable_without_animation()
    {
        IRenderedComponent<AgentCallStatusFeedback> cut = Render<AgentCallStatusFeedback>(
            parameters => parameters.Add(f => f.State, AgentCallStatus.Generating));

        IElement region = cut.Find("[data-testid='agent-call-status-feedback']");
        region.GetAttribute("role").ShouldBe("status");
        region.GetAttribute("aria-live").ShouldBe("polite");

        // Reduced motion does not gate perceivability: status is conveyed by the badge text + icon swap, not animation
        // (UX-DR38). The badge resolves a visible whole string and a non-null icon.
        cut.Find("[data-testid='agent-call-status-feedback-badge']").TextContent.Trim().ShouldNotBeNullOrEmpty();
        cut.FindComponent<AgentCallStatusBadge>().FindComponent<FluentBadge>().Instance.IconStart.ShouldNotBeNull();
    }

    [Fact]
    public void Call_status_feedback_constrained_viewport_reason_is_focusable_and_described()
    {
        IRenderedComponent<AgentCallStatusFeedback> cut = Render<AgentCallStatusFeedback>(parameters => parameters
            .Add(f => f.State, AgentCallStatus.Generating)
            .Add(f => f.Constrained, true));

        IElement unavailable = cut.Find("[data-testid='agent-call-status-feedback-unavailable']");
        unavailable.GetAttribute("tabindex").ShouldBe("0");
        unavailable.GetAttribute("aria-describedby").ShouldNotBeNullOrWhiteSpace();
    }
}
