using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Dom;

using Bunit;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.UI.Components.Pages;
using Hexalith.Agents.UI.Components.Shared;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC1–AC4 — the proposal-detail workspace. It maps the fail-closed gateway result to the FC-DTL surface state (a
/// non-ready read never renders the ready body), renders the SAFE AC1 metadata block + the AC2 version history, hosts the
/// 3.3–3.6 action controls gated on the retryable sub-state set, routes terminal proposals to "start a new Agent Call",
/// refreshes on action completion, blocks approval of a stale selection when a newer version appears, and never leaks
/// generated content (AD-14).
/// </summary>
public sealed class ProposalDetailTests : AgentsTestContext
{
    [Fact]
    public void Fails_closed_to_permission_denied_by_default_and_renders_no_workspace()
    {
        // The default substituted detail gateway returns NotAuthorized (AgentsTestContext) — the page renders the
        // permission-denied surface, never the ready workspace (AD-12).
        IRenderedComponent<ProposalDetail> cut = RenderDetail();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-proposal-detail-state']");
            cut.FindAll("[data-testid='agents-proposal-detail-workspace']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void Unavailable_read_shows_the_error_surface_and_no_workspace()
    {
        ProposalDetailGateway.GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalDetailResult.Unavailable()));

        IRenderedComponent<ProposalDetail> cut = RenderDetail();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-proposal-detail-state']");
            cut.FindAll("[data-testid='agents-proposal-detail-workspace']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void Not_found_read_shows_a_neutral_not_found_surface_and_no_workspace()
    {
        ProposalDetailGateway.GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalDetailResult.NotFound()));

        IRenderedComponent<ProposalDetail> cut = RenderDetail();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-proposal-detail-notfound']");
            cut.FindAll("[data-testid='agents-proposal-detail-workspace']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void Authorized_pending_proposal_renders_the_safe_metadata_block()
    {
        ProposalDetailGateway.GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.DetailResult(AgentUiTestData.Detail(
                sourceConversationId: "conv-a", callerPartyId: "caller-a", agentId: "agent-a", providerId: "prov-a", expiresAt: "2026-12-31T23:59:59Z"))));

        IRenderedComponent<ProposalDetail> cut = RenderDetail();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-proposal-detail-workspace']");
            cut.FindComponent<ProposedAgentReplyStateBadge>();
            cut.Find("[data-testid='agents-proposal-detail-source-conversation']").TextContent.ShouldContain("conv-a");
            cut.Find("[data-testid='agents-proposal-detail-provider']").TextContent.ShouldContain("prov-a");
            cut.Find("[data-testid='agents-proposal-detail-response-mode']").TextContent.ShouldContain("Agents.ResponseMode.Confirmation");
            cut.Find("[data-testid='agents-proposal-detail-expiry']").TextContent.ShouldContain("2026-12-31");
            cut.Find("[data-testid='agents-proposal-detail-posting-outcome']");
        });
    }

    [Fact]
    public void Authorized_pending_proposal_hosts_the_action_controls_and_version_history()
    {
        ProposalDetailGateway.GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.DetailResult()));

        IRenderedComponent<ProposalDetail> cut = RenderDetail();

        cut.WaitForAssertion(() =>
        {
            cut.FindComponent<ProposalVersionHistory>();
            cut.FindComponent<ProposalEditor>();
            cut.FindComponent<ProposalRegenerator>();
            cut.FindComponent<ProposalApprover>();
            cut.FindComponent<ProposalRejector>();
            cut.FindComponent<ProposalAbandoner>();
        });
    }

    [Fact]
    public void Read_dispatches_with_the_route_agent_interaction_id()
    {
        ProposalDetailGateway.GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.DetailResult()));

        _ = RenderDetail("interaction-9");

        ProposalDetailGateway.Received().GetProposalDetailAsync("interaction-9", Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(ProposedAgentReplyState.Rejected)]
    [InlineData(ProposedAgentReplyState.Abandoned)]
    [InlineData(ProposedAgentReplyState.Expired)]
    [InlineData(ProposedAgentReplyState.Posted)]
    [InlineData(ProposedAgentReplyState.PostingFailed)]
    public void Terminal_proposal_routes_to_start_new_call_and_offers_no_act_on(ProposedAgentReplyState terminalState)
    {
        ProposalDetailGateway.GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.DetailResult(AgentUiTestData.Detail(state: terminalState))));

        IRenderedComponent<ProposalDetail> cut = RenderDetail();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-proposal-detail-start-new-call']");
            cut.FindAll("[data-testid='agents-proposal-detail-editor']").ShouldBeEmpty();
            cut.FindAll("[data-testid='agents-proposal-detail-approver-approve']").ShouldBeEmpty();
            cut.FindAll("[data-testid='agents-proposal-detail-regenerator']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void Edit_failed_retryable_substate_still_offers_act_on()
    {
        // The edit-failed retry trap (Dev Notes): the coarse status is ProposalEditFailed but the sub-state is the
        // retryable Edited, so act-on stays available — the page gates on the sub-state, not the coarse status.
        ProposalDetailGateway.GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.DetailResult(AgentUiTestData.Detail(
                state: ProposedAgentReplyState.Edited, interactionStatus: AgentInteractionStatus.ProposalEditFailed))));

        IRenderedComponent<ProposalDetail> cut = RenderDetail();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-proposal-detail-actions']");
            cut.FindAll("[data-testid='agents-proposal-detail-start-new-call']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void Action_completion_refreshes_the_detail()
    {
        ProposalDetailGateway.GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.DetailResult()));

        IRenderedComponent<ProposalDetail> cut = RenderDetail();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-proposal-detail-approver-approve']"));

        cut.Find("[data-testid='agents-proposal-detail-approver-approve']").Click();

        // Initial read + the post-action refresh = two reads.
        cut.WaitForAssertion(() =>
            ProposalDetailGateway.Received(2).GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()));
    }

    [Fact]
    public void Approval_is_blocked_when_a_newer_version_appears_after_selection()
    {
        // review-governance selected-version lock: a regeneration appends v2 while v1 was selected — approval is blocked
        // and the re-prompt shows, until the Approver reviews the latest version.
        ProposalDetailView before = AgentUiTestData.Detail(
            selectedVersionId: "v1",
            state: ProposedAgentReplyState.Pending,
            versions: [AgentUiTestData.VersionSummary(versionId: "v1")]);
        ProposalDetailView after = AgentUiTestData.Detail(
            selectedVersionId: "v1",
            state: ProposedAgentReplyState.Regenerated,
            versions: [AgentUiTestData.VersionSummary(versionId: "v1"), AgentUiTestData.VersionSummary(versionId: "v2", kind: AgentGenerationKind.Regenerated)]);
        ProposalDetailGateway.GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalDetailResult.Success(before)), Task.FromResult(ProposalDetailResult.Success(after)));

        IRenderedComponent<ProposalDetail> cut = RenderDetail();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-proposal-detail-regenerator-regenerate']"));

        cut.Find("[data-testid='agents-proposal-detail-regenerator-regenerate']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-proposal-detail-selection-lock']");
            cut.FindAll("[data-testid='agents-proposal-detail-approver-approve']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void Reviewing_the_latest_version_clears_the_lock_and_restores_approval()
    {
        ProposalDetailView before = AgentUiTestData.Detail(
            selectedVersionId: "v1",
            state: ProposedAgentReplyState.Pending,
            versions: [AgentUiTestData.VersionSummary(versionId: "v1")]);
        ProposalDetailView after = AgentUiTestData.Detail(
            selectedVersionId: "v1",
            state: ProposedAgentReplyState.Regenerated,
            versions: [AgentUiTestData.VersionSummary(versionId: "v1"), AgentUiTestData.VersionSummary(versionId: "v2", kind: AgentGenerationKind.Regenerated)]);
        ProposalDetailGateway.GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalDetailResult.Success(before)), Task.FromResult(ProposalDetailResult.Success(after)));

        IRenderedComponent<ProposalDetail> cut = RenderDetail();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-proposal-detail-regenerator-regenerate']"));
        cut.Find("[data-testid='agents-proposal-detail-regenerator-regenerate']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-proposal-detail-selection-lock-review']"));

        cut.Find("[data-testid='agents-proposal-detail-selection-lock-review']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll("[data-testid='agents-proposal-detail-selection-lock']").ShouldBeEmpty();
            cut.Find("[data-testid='agents-proposal-detail-approver-approve']");
        });
    }

    [Fact]
    public void Compare_metadata_is_reachable_before_approve_and_shows_selected_and_latest()
    {
        // review-accessibility — "compare metadata" must be reachable BEFORE approval; toggling it reveals the safe
        // selected-vs-latest metadata panel alongside (not after) the approve control.
        ProposalDetailView detail = AgentUiTestData.Detail(
            selectedVersionId: "v1",
            versions:
            [
                AgentUiTestData.VersionSummary(versionId: "v1"),
                AgentUiTestData.VersionSummary(versionId: "v2", kind: AgentGenerationKind.Regenerated),
            ]);
        ProposalDetailGateway.GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalDetailResult.Success(detail)));

        IRenderedComponent<ProposalDetail> cut = RenderDetail();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-proposal-detail-compare-toggle']"));

        cut.Find("[data-testid='agents-proposal-detail-approver-approve']");
        cut.Find("[data-testid='agents-proposal-detail-compare-toggle']").Click();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-proposal-detail-compare-panel']");
            cut.Find("[data-testid='agents-proposal-detail-compare-selected']").TextContent.ShouldContain("v1");
            cut.Find("[data-testid='agents-proposal-detail-compare-latest']").TextContent.ShouldContain("v2");
        });
    }

    [Fact]
    public void Esc_closes_the_compare_panel_without_committing_or_re_reading()
    {
        // AC3 — Esc closes the transient compare panel without committing; it triggers no gateway re-read (only the
        // single initial load) and leaves the workspace in place.
        ProposalDetailGateway.GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.DetailResult()));

        IRenderedComponent<ProposalDetail> cut = RenderDetail();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-proposal-detail-compare-toggle']"));
        cut.Find("[data-testid='agents-proposal-detail-compare-toggle']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-proposal-detail-compare-panel']"));

        cut.Find("[data-testid='agents-proposal-detail-compare']").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cut.WaitForAssertion(() =>
            cut.FindAll("[data-testid='agents-proposal-detail-compare-panel']").ShouldBeEmpty());
        ProposalDetailGateway.Received(1).GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Approval_summary_names_the_exact_selected_version()
    {
        // review-accessibility — the approval confirmation must name the exact version (id, timestamp, source) so the
        // Approver knows precisely which version they are about to post.
        ProposalDetailView detail = AgentUiTestData.Detail(
            selectedVersionId: "v2",
            versions:
            [
                AgentUiTestData.VersionSummary(versionId: "v1"),
                AgentUiTestData.VersionSummary(versionId: "v2", kind: AgentGenerationKind.Edited, sourceVersionId: "v1", editorPartyId: "editor-1", createdAt: "2026-06-24T09:00:00Z"),
            ]);
        ProposalDetailGateway.GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalDetailResult.Success(detail)));

        IRenderedComponent<ProposalDetail> cut = RenderDetail();

        cut.WaitForAssertion(() =>
        {
            string summary = cut.Find("[data-testid='agents-proposal-detail-approval-summary']").TextContent;
            summary.ShouldContain("v2");
            summary.ShouldContain("2026-06-24T09:00:00Z");
            summary.ShouldContain("v1");
        });
    }

    [Fact]
    public void Expiry_with_no_snapshot_renders_the_none_label()
    {
        // AC1 — the page displays the snapshotted ExpiresAt; with no snapshot it shows the explicit "none" label, never
        // a fabricated default expiry.
        ProposalDetailGateway.GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.DetailResult(AgentUiTestData.Detail(expiresAt: null))));

        IRenderedComponent<ProposalDetail> cut = RenderDetail();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-proposal-detail-expiry']").TextContent.ShouldContain("Agents.ProposalQueue.Expiry.None");
            cut.FindAll("[data-testid='agents-proposal-detail-expiry'] time").ShouldBeEmpty();
        });
    }

    [Theory]
    [InlineData(ProposedAgentReplyState.Posted, "Agents.ProposalDetail.PostingOutcome.Posted")]
    [InlineData(ProposedAgentReplyState.PostingFailed, "Agents.ProposalDetail.PostingOutcome.Failed")]
    [InlineData(ProposedAgentReplyState.Approved, "Agents.ProposalDetail.PostingOutcome.Approved")]
    [InlineData(ProposedAgentReplyState.Pending, "Agents.ProposalDetail.PostingOutcome.None")]
    public void Posting_outcome_distinguishes_approval_from_posting(ProposedAgentReplyState state, string expectedKey)
    {
        // AC1/AC4 — the posting outcome distinguishes approval failure from posting failure; "approved" is never
        // conflated with "posted" until posting confirms.
        ProposalDetailGateway.GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.DetailResult(AgentUiTestData.Detail(state: state))));

        IRenderedComponent<ProposalDetail> cut = RenderDetail();

        cut.WaitForAssertion(() =>
            cut.Find("[data-testid='agents-proposal-detail-posting-outcome']").TextContent.ShouldContain(expectedKey));
    }

    [Theory]
    [InlineData(ProposedAgentReplyState.Posted, "Agents.ProposalDetail.Transition.ApprovalPosted")]
    [InlineData(ProposedAgentReplyState.PostingFailed, "Agents.ProposalDetail.Transition.PostingFailed")]
    [InlineData(ProposedAgentReplyState.Expired, "Agents.ProposalDetail.Transition.ProposalExpired")]
    public void Load_time_transition_announces_politely(ProposedAgentReplyState state, string expectedMessage)
    {
        // AC4 — a notable load-time state (posted / posting failed / expired) is announced through the polite live
        // region with safe whole-string text.
        ProposalDetailGateway.GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.DetailResult(AgentUiTestData.Detail(state: state))));

        IRenderedComponent<ProposalDetail> cut = RenderDetail();

        cut.WaitForAssertion(() =>
        {
            IElement region = cut.Find("[data-testid='agents-proposal-detail-announcer']");
            region.GetAttribute("role").ShouldBe("status");
            region.GetAttribute("aria-live").ShouldBe("polite");
            cut.Find("[data-testid='agents-proposal-detail-announcer-message']").TextContent.Trim().ShouldBe(expectedMessage);
        });
    }

    [Fact]
    public void Ordinary_pending_progress_does_not_announce_assertively()
    {
        // AC4 — an ordinary pending proposal keeps the live region silent; it never emits a disruptive assertive cue.
        ProposalDetailGateway.GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.DetailResult()));

        IRenderedComponent<ProposalDetail> cut = RenderDetail();

        cut.WaitForAssertion(() =>
        {
            IElement region = cut.Find("[data-testid='agents-proposal-detail-announcer']");
            region.GetAttribute("aria-live").ShouldBe("polite");
            cut.FindAll("[data-testid='agents-proposal-detail-announcer-message']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void Stale_selection_block_announces_assertively_for_destructive_action_prevention()
    {
        // review-accessibility — when a newer version appears after selection, the block is the ONE assertive
        // announcement (immediate destructive-action prevention), reinforcing the visual selection lock.
        ProposalDetailView before = AgentUiTestData.Detail(
            selectedVersionId: "v1",
            state: ProposedAgentReplyState.Pending,
            versions: [AgentUiTestData.VersionSummary(versionId: "v1")]);
        ProposalDetailView after = AgentUiTestData.Detail(
            selectedVersionId: "v1",
            state: ProposedAgentReplyState.Regenerated,
            versions: [AgentUiTestData.VersionSummary(versionId: "v1"), AgentUiTestData.VersionSummary(versionId: "v2", kind: AgentGenerationKind.Regenerated)]);
        ProposalDetailGateway.GetProposalDetailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalDetailResult.Success(before)), Task.FromResult(ProposalDetailResult.Success(after)));

        IRenderedComponent<ProposalDetail> cut = RenderDetail();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-proposal-detail-regenerator-regenerate']"));
        cut.Find("[data-testid='agents-proposal-detail-regenerator-regenerate']").Click();

        cut.WaitForAssertion(() =>
        {
            IElement region = cut.Find("[data-testid='agents-proposal-detail-announcer']");
            region.GetAttribute("role").ShouldBe("alert");
            region.GetAttribute("aria-live").ShouldBe("assertive");
            cut.Find("[data-testid='agents-proposal-detail-announcer-message']").TextContent.Trim()
                .ShouldBe("Agents.ProposalDetail.Transition.StaleApprovalBlocked");
        });
    }

    private IRenderedComponent<ProposalDetail> RenderDetail(string agentInteractionId = "interaction-1")
    {
        InitializeStoreAsync().GetAwaiter().GetResult();
        return Render<ProposalDetail>(parameters => parameters.Add(page => page.AgentInteractionId, agentInteractionId));
    }
}
