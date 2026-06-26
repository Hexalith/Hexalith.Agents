using AngleSharp.Dom;

using Bunit;

using Hexalith.Agents.UI.Components.Shared;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC2/AC3/AC4 — the call-status feedback surface renders the badge in a reserved slot plus a coarse, safe reason for
/// the failure/blocked states (never raw error/prompt/content/cross-tenant text — AD-14), announces ordinary
/// transitions politely and denial/terminal-failure assertively (the transition matrix), fails closed on a constrained
/// viewport (the high-impact action becomes unavailable with a focusable, aria-describedby-linked reason while
/// review-only status stays visible), and renders the Stale surface for a stale read rather than fresh status.
/// </summary>
public sealed class AgentCallStatusFeedbackTests : AgentsTestContext
{
    [Theory]
    [InlineData(AgentCallStatus.Denied)]
    [InlineData(AgentCallStatus.Blocked)]
    [InlineData(AgentCallStatus.ContextBlocked)]
    [InlineData(AgentCallStatus.GenerationFailed)]
    [InlineData(AgentCallStatus.SafetyFailed)]
    [InlineData(AgentCallStatus.PostingFailed)]
    public void Failure_states_render_the_safe_coarse_reason_label(AgentCallStatus state)
    {
        IRenderedComponent<AgentCallStatusFeedback> cut = Render<AgentCallStatusFeedback>(
            parameters => parameters.Add(f => f.State, state));

        string reasonKey = AgentCallStatusPresentation.ReasonKeyFor(state)!;
        cut.Find("[data-testid='agent-call-status-feedback-reason']").TextContent.Trim().ShouldBe(reasonKey);

        // Whole-string safe reason — never raw provider/Conversations error text, prompt, or content (AD-14).
        string visible = cut.VisibleText();
        visible.ShouldNotContain("Exception");
        visible.ShouldNotContain("stack");
        visible.ShouldNotContain("SELECT");
    }

    [Theory]
    [InlineData(AgentCallStatus.Requested)]
    [InlineData(AgentCallStatus.Authorized)]
    [InlineData(AgentCallStatus.ContextLoading)]
    [InlineData(AgentCallStatus.Generating)]
    [InlineData(AgentCallStatus.Generated)]
    [InlineData(AgentCallStatus.PostingPending)]
    public void Ordinary_transitions_have_no_reason_and_render_no_reason_element(AgentCallStatus state)
    {
        IRenderedComponent<AgentCallStatusFeedback> cut = Render<AgentCallStatusFeedback>(
            parameters => parameters.Add(f => f.State, state));

        cut.FindAll("[data-testid='agent-call-status-feedback-reason']").ShouldBeEmpty();
    }

    [Theory]
    [InlineData(AgentCallStatus.Requested, "status", "polite")]
    [InlineData(AgentCallStatus.Authorized, "status", "polite")]
    [InlineData(AgentCallStatus.ContextLoading, "status", "polite")]
    [InlineData(AgentCallStatus.Generating, "status", "polite")]
    [InlineData(AgentCallStatus.Generated, "status", "polite")]
    [InlineData(AgentCallStatus.PostingPending, "status", "polite")]
    [InlineData(AgentCallStatus.Posted, "status", "polite")]
    [InlineData(AgentCallStatus.Denied, "alert", "assertive")]
    [InlineData(AgentCallStatus.Blocked, "alert", "assertive")]
    [InlineData(AgentCallStatus.ContextBlocked, "alert", "assertive")]
    [InlineData(AgentCallStatus.GenerationFailed, "alert", "assertive")]
    [InlineData(AgentCallStatus.SafetyFailed, "alert", "assertive")]
    [InlineData(AgentCallStatus.PostingFailed, "alert", "assertive")]
    public void Live_region_is_polite_for_ordinary_and_assertive_only_for_denial_or_failure(
        AgentCallStatus state, string role, string ariaLive)
    {
        IRenderedComponent<AgentCallStatusFeedback> cut = Render<AgentCallStatusFeedback>(
            parameters => parameters.Add(f => f.State, state));

        IElement region = cut.Find("[data-testid='agent-call-status-feedback']");
        region.GetAttribute("role").ShouldBe(role);
        region.GetAttribute("aria-live").ShouldBe(ariaLive);
    }

    [Fact]
    public void Posted_state_renders_in_the_reserved_badge_slot_without_announcing_assertively()
    {
        IRenderedComponent<AgentCallStatusFeedback> cut = Render<AgentCallStatusFeedback>(
            parameters => parameters.Add(f => f.State, AgentCallStatus.Posted));

        cut.Find("[data-testid='agent-call-status-feedback-slot']");
        cut.Find("[data-testid='agent-call-status-feedback-badge']");
        cut.Find("[data-testid='agent-call-status-feedback']").GetAttribute("aria-live").ShouldBe("polite");
    }

    [Fact]
    public void Constrained_viewport_makes_the_action_unavailable_with_a_focusable_reason_and_keeps_review_only_status()
    {
        IRenderedComponent<AgentCallStatusFeedback> cut = Render<AgentCallStatusFeedback>(parameters => parameters
            .Add(f => f.State, AgentCallStatus.Generating)
            .Add(f => f.Constrained, true));

        // The unavailable reason is focusable (tabindex) and aria-describedby-linked — never a bare disabled control.
        IElement unavailable = cut.Find("[data-testid='agent-call-status-feedback-unavailable']");
        unavailable.GetAttribute("tabindex").ShouldBe("0");
        unavailable.GetAttribute("aria-describedby").ShouldNotBeNullOrWhiteSpace();
        unavailable.TextContent.Trim().ShouldBe("Agents.ConversationCall.Feedback.Unavailable");

        // Review-only status (the badge) remains visible.
        cut.Find("[data-testid='agent-call-status-feedback-badge']");
    }

    [Fact]
    public void Stale_read_renders_the_stale_surface_and_never_fresh_status()
    {
        IRenderedComponent<AgentCallStatusFeedback> cut = Render<AgentCallStatusFeedback>(parameters => parameters
            .Add(f => f.State, AgentCallStatus.Posted)
            .Add(f => f.Stale, true));

        cut.Find("[data-testid='agent-call-status-feedback-stale']");
        cut.Markup.ShouldContain("Agents.Surface.Stale.Title");
        // The fresh status badge must not render alongside the stale surface (stale != fresh).
        cut.FindAll("[data-testid='agent-call-status-feedback-badge']").ShouldBeEmpty();
    }
}
