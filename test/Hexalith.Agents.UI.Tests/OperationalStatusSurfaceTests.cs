using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Dom;

using Bunit;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.UI.Components.Pages;
using Hexalith.Agents.UI.Components.Shared;

using Microsoft.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// Story 4.3 AC1, AC2, AC4 — the operational-status panel groups readiness/failures by recovery action (never the raw
/// subsystem), the page routes every data-state through the eight surface kinds, surfaces the AC4 read signals from
/// safe counts only, and fails closed against the default deferred gateway.
/// </summary>
public sealed class OperationalStatusSurfaceTests : AgentsTestContext
{
    [Fact]
    public void Panel_groups_readiness_and_failures_by_recovery_action()
    {
        // AC1 — a ProviderUnavailable readiness + a provider blocker share the ConfigureProvider action group; a posting
        // failure lands in RetryGeneration; pending approval in WaitForApproval.
        AgentOperationalStatusSummaryView summary = AgentUiTestData.OperationalStatusSummary(
            agentReadiness: AgentReadinessStatus.ProviderUnavailable,
            readinessBlockers: [AgentActivationBlocker.MissingProviderSelection],
            recentCallOutcomes: [new AgentCallOutcomeCount(AgentCallOperationStatus.Denied, 2)],
            proposalOutcomes: [new ProposalOutcomeCount(ProposalOperationStatus.PendingApproval, 1), new ProposalOutcomeCount(ProposalOperationStatus.PostingFailed, 1)]);

        IRenderedComponent<OperationalStatusPanel> cut = Render<OperationalStatusPanel>(parameters => parameters
            .Add(panel => panel.Summary, summary));

        // The group region exists for each recovery action with its safe localized guidance (the action, not the subsystem).
        cut.Find("[data-testid='agents-operational-status-panel-group-ConfigureProvider']");
        cut.Find("[data-testid='agents-operational-status-panel-guidance-ConfigureProvider']")
            .TextContent.Trim().ShouldBe("Agents.OperationalStatus.Recovery.ConfigureProvider");
        cut.Find("[data-testid='agents-operational-status-panel-group-WaitForApproval']");
        cut.Find("[data-testid='agents-operational-status-panel-group-RetryGeneration']");
        cut.Find("[data-testid='agents-operational-status-panel-group-FixPolicy']");

        // The blocker is grouped by action and labelled with its safe whole-string reason (never the raw Instructions text).
        cut.Find("[data-testid='agents-operational-status-blocker-MissingProviderSelection']");
    }

    [Fact]
    public void Panel_renders_posted_and_posting_failed_as_distinct_states()
    {
        // AC1 / AD-5 — Posted (success) and PostingFailed (danger) are distinct, never collapsed.
        AgentOperationalStatusSummaryView summary = AgentUiTestData.OperationalStatusSummary(
            proposalOutcomes: [new ProposalOutcomeCount(ProposalOperationStatus.Posted, 4), new ProposalOutcomeCount(ProposalOperationStatus.PostingFailed, 1)]);

        IRenderedComponent<OperationalStatusPanel> cut = Render<OperationalStatusPanel>(parameters => parameters
            .Add(panel => panel.Summary, summary));

        cut.Find("[data-testid='agents-operational-status-proposal-Posted']");
        cut.Find("[data-testid='agents-operational-status-proposal-PostingFailed']");
    }

    [Fact]
    public void Page_renders_ac4_signals_from_safe_fields_and_no_content_sentinel()
    {
        // AC4 — recent outcomes, proposal-queue summary, terminal/posting rates, and readiness blockers render from safe
        // counts; no prompt/generated-content string can appear (the summary is content-free by construction; AD-14).
        const string contentSentinel = "top-secret-generated-content-9d2";
        OperationalStatusGateway.GetSummaryAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.OperationalStatusResult()));

        IRenderedComponent<OperationalStatus> cut = RenderOperationalStatus();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-operational-status-panel']");
            cut.Find("[data-testid='agents-operational-status-pending-count']").TextContent.Trim().ShouldBe("2");
            cut.Find("[data-testid='agents-operational-status-audit-link']");
            cut.Markup.ShouldNotContain(contentSentinel);
        });
    }

    [Fact]
    public void Page_renders_not_available_yet_when_no_live_source_produced_the_summary()
    {
        // AC4 read-source reconciliation — a signal with no live source renders an explicit "not available yet"
        // affordance, never a fabricated value.
        OperationalStatusGateway.GetSummaryAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.OperationalStatusResult(AgentUiTestData.OperationalStatusSummary(generatedAt: null))));

        IRenderedComponent<OperationalStatus> cut = RenderOperationalStatus();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='agents-operational-status-generated-at']").TextContent.Trim().ShouldBe("Agents.Common.NotAvailableYet"));
    }

    [Fact]
    public void Page_fails_closed_to_permission_denied_against_the_default_deferred_gateway()
    {
        // AC2 / AD-12 — the default substitute returns NotAuthorized → the page renders the permission-denied surface.
        IRenderedComponent<OperationalStatus> cut = RenderOperationalStatus();

        cut.WaitForAssertion(() =>
        {
            IElement region = cut.Find("[data-testid='agents-operational-status-state']");
            region.ClassList.ShouldContain("agent-surface-state--permissiondenied");
            region.GetAttribute("role").ShouldBe("alert");
        });
    }

    [Fact]
    public void Page_renders_the_unavailable_surface_when_the_dependency_is_down()
    {
        // AC2 — an Unavailable read renders the assertive Unavailable surface kind (a down dependency/projection).
        OperationalStatusGateway.GetSummaryAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentOperationalStatusSummaryResult.Unavailable()));

        IRenderedComponent<OperationalStatus> cut = RenderOperationalStatus();

        cut.WaitForAssertion(() =>
        {
            IElement region = cut.Find("[data-testid='agents-operational-status-state']");
            region.ClassList.ShouldContain("agent-surface-state--unavailable");
            region.GetAttribute("role").ShouldBe("alert");
            region.GetAttribute("aria-live").ShouldBe("assertive");
        });
    }

    [Fact]
    public void Page_renders_the_degraded_surface_for_a_stale_summary()
    {
        // AC2 — a Stale read that still carries a summary renders the completed-but-stale Degraded surface (polite), never
        // a fresh success.
        OperationalStatusGateway.GetSummaryAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentOperationalStatusSummaryResult.Stale(AgentUiTestData.OperationalStatusSummary())));

        IRenderedComponent<OperationalStatus> cut = RenderOperationalStatus();

        cut.WaitForAssertion(() =>
        {
            IElement region = cut.Find("[data-testid='agents-operational-status-state']");
            region.ClassList.ShouldContain("agent-surface-state--degraded");
            region.GetAttribute("aria-live").ShouldBe("polite");
        });
    }

    [Fact]
    public void Panel_renders_outcome_counts_and_omits_zero_count_outcomes()
    {
        // AC4 — a non-zero outcome count renders as a visible "(n)" suffix on its chip; a zero-count outcome contributes
        // no chip (the rates are content-free counts, never a per-record list). The single readiness + audit chips are
        // always present so the operator always sees the headline readiness/audit state.
        AgentOperationalStatusSummaryView summary = AgentUiTestData.OperationalStatusSummary(
            proposalOutcomes: [new ProposalOutcomeCount(ProposalOperationStatus.Posted, 4), new ProposalOutcomeCount(ProposalOperationStatus.Rejected, 0)]);

        IRenderedComponent<OperationalStatusPanel> cut = Render<OperationalStatusPanel>(parameters => parameters
            .Add(panel => panel.Summary, summary));

        cut.Find("[data-testid='agents-operational-status-proposal-Posted']").TextContent.ShouldContain("(4)");
        cut.FindAll("[data-testid='agents-operational-status-proposal-Rejected']").ShouldBeEmpty();
        cut.Find("[data-testid='agents-operational-status-readiness']");
        cut.Find("[data-testid='agents-operational-status-audit']");
    }

    [Fact]
    public void Page_renders_empty_surface_when_the_authorized_summary_has_no_signals()
    {
        // AC2 — an authorized but signal-free summary renders the Empty surface, never a fabricated panel, and is
        // distinct from permission-denied/unavailable.
        AgentOperationalStatusSummaryView empty = AgentUiTestData.OperationalStatusSummary(
            agentReadiness: AgentReadinessStatus.Unknown,
            readinessBlockers: [],
            auditGovernanceBlockers: [],
            auditAvailability: AuditAvailabilityStatus.Unknown,
            recentCallOutcomes: [],
            proposalOutcomes: [],
            pendingProposalCount: 0);
        OperationalStatusGateway.GetSummaryAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.OperationalStatusResult(empty)));

        IRenderedComponent<OperationalStatus> cut = RenderOperationalStatus();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='agents-operational-status-state']").ClassList.ShouldContain("agent-surface-state--empty"));
    }

    [Fact]
    public void Page_renders_stale_surface_with_refresh_when_no_trustworthy_summary()
    {
        // AC2 — a Stale read carrying no trustworthy summary renders the Stale surface (fresh-but-aged; offers a
        // refresh), distinct from the Degraded surface used when a completed-but-stale summary is still rendered.
        OperationalStatusGateway.GetSummaryAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentOperationalStatusSummaryResult.Stale(null)));

        IRenderedComponent<OperationalStatus> cut = RenderOperationalStatus();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-operational-status-state']").ClassList.ShouldContain("agent-surface-state--stale");
            cut.Find("[data-testid='agents-operational-status-state-refresh']");
        });
    }

    private IRenderedComponent<OperationalStatus> RenderOperationalStatus()
    {
        InitializeStoreAsync().GetAwaiter().GetResult();
        return Render<OperationalStatus>();
    }
}
