using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Bunit;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.UI.Components.Pages;
using Hexalith.Agents.UI.Components.Shared;

using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC1–AC4 — the proposal-queue discovery surface. The grid lists Proposed Agent Replies with SAFE columns + a state
/// badge (never content, never styled as a Conversation Message); the read dispatches with the expected args; the six
/// non-data surface states route from the substituted gateway; filters narrow the queue and offer a reset; the AC3
/// count indication renders only on an authorized result; and a denied/empty read discloses no ids, caller, conversation,
/// content, count, or rows (AC4 fail-closed).
/// </summary>
public sealed class ProposalQueueTests : AgentsTestContext
{
    private const string CallerSentinel = "caller-do-not-leak-7f3a";
    private const string ConversationSentinel = "conversation-do-not-leak-7f3a";
    private const string ContentSentinel = "top-secret-generated-answer-do-not-leak";

    [Fact]
    public void Grid_renders_one_row_per_proposal_with_safe_columns_and_a_state_badge()
    {
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.ProposalsResult(
                AgentUiTestData.PendingProposal(agentInteractionId: "i1", agentId: "agent-a", sourceConversationId: "conv-a", callerPartyId: "caller-a"),
                AgentUiTestData.PendingProposal(agentInteractionId: "i2", agentId: "agent-b", sourceConversationId: "conv-b", callerPartyId: "caller-b"))));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-proposal-queue-grid']");
            cut.Markup.ShouldContain("agent-a");
            cut.Markup.ShouldContain("agent-b");
            cut.Markup.ShouldContain("conv-a");
            cut.Markup.ShouldContain("caller-a");
            cut.FindComponents<ProposedAgentReplyStateBadge>().Count.ShouldBe(2);
        });
    }

    [Fact]
    public void List_is_requested_without_historical_proposals()
    {
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.ProposalsResult(AgentUiTestData.PendingProposal())));

        _ = RenderPage<ProposalQueue>();

        ProposalGateway.Received().ListPendingProposalsAsync(false, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Before_load_completes_the_loading_surface_is_shown()
    {
        // A never-completing read keeps the page in its initial (pre-result) render so the Loading surface is asserted.
        var pending = new TaskCompletionSource<PendingProposalsResult>();
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(pending.Task);

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();

        cut.Find("[data-testid='agents-proposal-queue-state']");
        cut.Markup.ShouldContain("Agents.Surface.Loading.Title");
    }

    [Fact]
    public void Not_authorized_result_renders_the_permission_denied_surface()
    {
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(PendingProposalsResult.NotAuthorized()));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.Surface.PermissionDenied.Title"));
    }

    [Fact]
    public void Unavailable_result_renders_the_error_surface()
    {
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(PendingProposalsResult.Unavailable()));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.Surface.Error.Title"));
    }

    [Fact]
    public void Stale_result_renders_the_stale_surface_with_a_refresh_action()
    {
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(PendingProposalsResult.Stale([AgentUiTestData.PendingProposal()], pendingCount: 1)));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Agents.Surface.Stale.Title");
            cut.Find("[data-testid='agents-proposal-queue-state-refresh']");
        });
    }

    [Fact]
    public void Empty_success_result_renders_the_empty_surface()
    {
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.ProposalsResult()));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.Surface.Empty.Title"));
    }

    [Fact]
    public void Needs_my_action_filter_yields_a_filtered_empty_surface_that_can_be_reset()
    {
        // No proposal needs the current user's action: turning on the switch filters to zero rows, which must be the
        // distinct filtered-empty state (offering a reset), never the no-records empty state (AC2).
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.ProposalsResult(
                AgentUiTestData.PendingProposal(agentInteractionId: "i1", needsCurrentUserAction: false),
                AgentUiTestData.PendingProposal(agentInteractionId: "i2", needsCurrentUserAction: false))));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-proposal-queue-grid']"));

        cut.Find("[data-testid='agents-proposal-queue-needs-action-filter']").Change(true);

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Agents.Surface.FilteredEmpty.Title");
            cut.FindAll("[data-testid='agents-proposal-queue-grid']").ShouldBeEmpty();
        });

        cut.Find("[data-testid='agents-proposal-queue-state-reset']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-proposal-queue-grid']"));
    }

    [Fact]
    public async Task State_filter_yields_a_filtered_empty_surface_that_can_be_reset()
    {
        // Every row is Pending; selecting the (unmatched) Unknown state filters to zero rows → filtered-empty (AC2).
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.ProposalsResult(
                AgentUiTestData.PendingProposal(agentInteractionId: "i1", state: ProposedAgentReplyState.Pending))));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-proposal-queue-grid']"));

        // The state select is the first FluentSelect<string, string>; drive its two-way binding directly.
        FluentSelect<string, string> stateSelect = cut.FindComponents<FluentSelect<string, string>>()[0].Instance;
        await cut.InvokeAsync(() => stateSelect.ValueChanged.InvokeAsync(nameof(ProposedAgentReplyState.Unknown)));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Agents.Surface.FilteredEmpty.Title");
            cut.FindAll("[data-testid='agents-proposal-queue-grid']").ShouldBeEmpty();
        });

        cut.Find("[data-testid='agents-proposal-queue-state-reset']").Click();

        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-proposal-queue-grid']"));
    }

    [Fact]
    public void Count_indication_renders_on_an_authorized_result()
    {
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.ProposalsResult(
                AgentUiTestData.PendingProposal(needsCurrentUserAction: true))));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-proposal-queue-count']");
            cut.Markup.ShouldContain("Agents.ProposalQueue.PendingCount");
        });
    }

    [Fact]
    public void Count_indication_is_absent_when_the_read_is_not_authorized()
    {
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(PendingProposalsResult.NotAuthorized()));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Agents.Surface.PermissionDenied.Title");
            cut.FindAll("[data-testid='agents-proposal-queue-count']").ShouldBeEmpty();
        });
    }

    [Fact]
    public void Not_authorized_read_discloses_no_records_count_or_grid()
    {
        // Even though the deferred read fails closed (empty list), assert nothing fingerprints another tenant's records:
        // no sample id/caller/conversation/content, no count, no grid (AC4).
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(PendingProposalsResult.NotAuthorized()));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Agents.Surface.PermissionDenied.Title");
            cut.FindAll("[data-testid='agents-proposal-queue-grid']").ShouldBeEmpty();
            cut.FindAll("[data-testid='agents-proposal-queue-count']").ShouldBeEmpty();
            cut.Markup.ShouldNotContain(CallerSentinel);
            cut.Markup.ShouldNotContain(ConversationSentinel);
            cut.Markup.ShouldNotContain(ContentSentinel);
        });
    }

    [Fact]
    public void Unavailable_read_discloses_no_records_count_or_grid()
    {
        // The fault path fails closed exactly like denial: the error surface shows, but nothing fingerprints another
        // tenant's records — no grid, no count, no sample id/caller/conversation/content (AC4).
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(PendingProposalsResult.Unavailable()));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Agents.Surface.Error.Title");
            cut.FindAll("[data-testid='agents-proposal-queue-grid']").ShouldBeEmpty();
            cut.FindAll("[data-testid='agents-proposal-queue-count']").ShouldBeEmpty();
            cut.Markup.ShouldNotContain(CallerSentinel);
            cut.Markup.ShouldNotContain(ConversationSentinel);
            cut.Markup.ShouldNotContain(ContentSentinel);
        });
    }

    [Fact]
    public async Task Agent_filter_narrows_the_queue_to_the_selected_agent()
    {
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.ProposalsResult(
                AgentUiTestData.PendingProposal(agentInteractionId: "i1", agentId: "agent-alpha", callerPartyId: "caller-alpha"),
                AgentUiTestData.PendingProposal(agentInteractionId: "i2", agentId: "agent-beta", callerPartyId: "caller-beta"))));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-proposal-queue-grid']"));

        // The Agent select is the second FluentSelect<string, string> (state=0, agent=1, expiry=2); its option value is the
        // raw AgentId, so binding it filters the in-memory rows to that agent (AC2).
        FluentSelect<string, string> agentSelect = cut.FindComponents<FluentSelect<string, string>>()[1].Instance;
        await cut.InvokeAsync(() => agentSelect.ValueChanged.InvokeAsync("agent-alpha"));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("caller-alpha");
            cut.Markup.ShouldNotContain("caller-beta");
        });
    }

    [Theory]
    [InlineData("Expired", "caller-expired")]
    [InlineData("ExpiringSoon", "caller-soon")]
    [InlineData("None", "caller-none")]
    public async Task Expiry_filter_narrows_the_queue_to_the_matching_bucket(string expiryOption, string expectedCaller)
    {
        // The Clock is fixed at 2026-06-24T12:00:00Z (AgentsTestContext.Clock): one row already expired, one expiring
        // within 24h, one with no expiry — each must surface under exactly its expiry bucket and no other (AC2).
        string[] allCallers = ["caller-expired", "caller-soon", "caller-none"];
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.ProposalsResult(
                AgentUiTestData.PendingProposal(agentInteractionId: "expired", callerPartyId: "caller-expired", expiresAt: "2026-06-24T10:00:00Z"),
                AgentUiTestData.PendingProposal(agentInteractionId: "soon", callerPartyId: "caller-soon", expiresAt: "2026-06-24T20:00:00Z"),
                AgentUiTestData.PendingProposal(agentInteractionId: "none", callerPartyId: "caller-none", expiresAt: null))));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-proposal-queue-grid']"));

        FluentSelect<string, string> expirySelect = cut.FindComponents<FluentSelect<string, string>>()[2].Instance;
        await cut.InvokeAsync(() => expirySelect.ValueChanged.InvokeAsync(expiryOption));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain(expectedCaller);
            foreach (string absentCaller in allCallers.Where(caller => caller != expectedCaller))
            {
                cut.Markup.ShouldNotContain(absentCaller);
            }
        });
    }

    [Fact]
    public async Task Source_conversation_filter_keeps_only_contains_matches()
    {
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.ProposalsResult(
                AgentUiTestData.PendingProposal(agentInteractionId: "i1", sourceConversationId: "needle-conversation", callerPartyId: "cal-keep"),
                AgentUiTestData.PendingProposal(agentInteractionId: "i2", sourceConversationId: "other-conversation", callerPartyId: "cal-drop"))));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-proposal-queue-grid']"));

        // The Source Conversation contains-match is the first FluentTextInput (source-conversation=0, caller=1).
        FluentTextInput sourceFilter = cut.FindComponents<FluentTextInput>()[0].Instance;
        await cut.InvokeAsync(() => sourceFilter.ValueChanged.InvokeAsync("needle"));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("cal-keep");
            cut.Markup.ShouldNotContain("cal-drop");
        });
    }

    [Fact]
    public async Task Caller_filter_keeps_only_contains_matches()
    {
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.ProposalsResult(
                AgentUiTestData.PendingProposal(agentInteractionId: "i1", callerPartyId: "needle-caller", sourceConversationId: "src-keep"),
                AgentUiTestData.PendingProposal(agentInteractionId: "i2", callerPartyId: "other-caller", sourceConversationId: "src-drop"))));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-proposal-queue-grid']"));

        // The caller contains-match is the second FluentTextInput (source-conversation=0, caller=1).
        FluentTextInput callerFilter = cut.FindComponents<FluentTextInput>()[1].Instance;
        await cut.InvokeAsync(() => callerFilter.ValueChanged.InvokeAsync("needle"));

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("src-keep");
            cut.Markup.ShouldNotContain("src-drop");
        });
    }

    [Fact]
    public void Responsibility_column_reads_you_for_actionable_rows_and_approver_otherwise()
    {
        // The AC1 "current responsibility" column is derived in presentation from the server-computed NeedsCurrentUserAction
        // flag — not a separate contract field (Task 1 note).
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.ProposalsResult(
                AgentUiTestData.PendingProposal(agentInteractionId: "mine", needsCurrentUserAction: true),
                AgentUiTestData.PendingProposal(agentInteractionId: "theirs", needsCurrentUserAction: false))));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-proposal-queue-grid']");
            cut.Markup.ShouldContain("Agents.ProposalQueue.Responsibility.You");
            cut.Markup.ShouldContain("Agents.ProposalQueue.Responsibility.Approver");
        });
    }

    [Fact]
    public void Age_column_renders_a_deterministic_bucket_from_the_injected_clock()
    {
        // The Clock is fixed at 2026-06-24T12:00:00Z; a row created 30 minutes earlier renders the "<1h" bucket — proving
        // the page derives "age" from the injected TimeProvider, never DateTimeOffset.UtcNow (deterministic, non-flaky).
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.ProposalsResult(
                AgentUiTestData.PendingProposal(createdAt: "2026-06-24T11:30:00Z"))));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-proposal-queue-grid']");
            cut.Markup.ShouldContain("Agents.ProposalQueue.Age.LessThanHour");
        });
    }

    [Fact]
    public void Expiry_column_shows_the_no_expiry_label_and_a_time_element()
    {
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentUiTestData.ProposalsResult(
                AgentUiTestData.PendingProposal(agentInteractionId: "no-expiry", expiresAt: null),
                AgentUiTestData.PendingProposal(agentInteractionId: "with-expiry", expiresAt: "2026-12-31T23:59:59Z"))));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-proposal-queue-grid']");
            cut.Markup.ShouldContain("Agents.ProposalQueue.Expiry.None");
            cut.Find("time[datetime='2026-12-31T23:59:59Z']");
        });
    }

    [Fact]
    public void Count_indication_renders_on_a_stale_result()
    {
        // AC3 — the pending count is surfaced on Stale as well as Success (both are authorized reads).
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(PendingProposalsResult.Stale([AgentUiTestData.PendingProposal(needsCurrentUserAction: true)], pendingCount: 1)));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-proposal-queue-count']");
            cut.Markup.ShouldContain("Agents.ProposalQueue.PendingCount");
        });
    }

    [Fact]
    public void Stale_result_still_renders_the_authorized_rows_behind_the_stale_notice()
    {
        // Degraded data may still carry trustworthy rows the UI renders behind a stale notice (AD-12); the Stale surface
        // and the grid coexist.
        ProposalGateway.ListPendingProposalsAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(PendingProposalsResult.Stale([AgentUiTestData.PendingProposal(callerPartyId: "caller-stale")], pendingCount: 1)));

        IRenderedComponent<ProposalQueue> cut = RenderPage<ProposalQueue>();

        cut.WaitForAssertion(() =>
        {
            cut.Markup.ShouldContain("Agents.Surface.Stale.Title");
            cut.Find("[data-testid='agents-proposal-queue-grid']");
            cut.Markup.ShouldContain("caller-stale");
        });
    }
}
