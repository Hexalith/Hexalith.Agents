using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Dom;

using Bunit;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.UI.Components.Shared;
using Hexalith.Agents.UI.Services.Gateways;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC1 — the pattern-agnostic invocation panel visibly names hexa, captures the prompt through a Fluent input (never a
/// raw &lt;textarea&gt;) and the Source Conversation context, shows the response-mode implication for both modes without
/// biasing Automatic, never implies a posted message before status is Posted (UX-DR22), submits only the safe inputs to
/// the gateway (the sensitive prompt is never echoed into a badge label/accessible name/data-testid — AD-14), and
/// cancels (Esc/Cancel) back to the trigger (UX-DR32/UX-DR37).
/// </summary>
public sealed class ConversationAgentCallPanelTests : AgentsTestContext
{
    private const string Prompt = "SENSITIVE-PROMPT-XYZ";

    private IRenderedComponent<ConversationAgentCallPanel> RenderPanel(
        string sourceConversationId = "conv-7",
        AgentResponseMode mode = AgentResponseMode.Automatic,
        System.Action? onCancel = null)
        => Render<ConversationAgentCallPanel>(parameters =>
        {
            parameters
                .Add(p => p.AgentDisplayName, "hexa")
                .Add(p => p.SourceConversationId, sourceConversationId)
                .Add(p => p.ResponseMode, mode);
            if (onCancel is not null)
            {
                parameters.Add(p => p.OnCancel, onCancel);
            }
        });

    private static async Task TypePromptAsync(IRenderedComponent<ConversationAgentCallPanel> cut, string prompt)
    {
        IRenderedComponent<FluentTextArea> textArea = cut.FindComponent<FluentTextArea>();
        await cut.InvokeAsync(() => textArea.Instance.ValueChanged.InvokeAsync(prompt));
    }

    private async Task<IRenderedComponent<ConversationAgentCallPanel>> SubmitWithResultAsync(
        AgentCallRequestResult result, AgentResponseMode mode = AgentResponseMode.Automatic)
    {
        CallGateway.RequestCallAsync(Arg.Any<ConversationAgentCallRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(result));

        IRenderedComponent<ConversationAgentCallPanel> cut = RenderPanel(mode: mode);
        await TypePromptAsync(cut, Prompt);
        cut.Find("[data-testid='conversation-agent-call-panel-submit']").Click();
        return cut;
    }

    private static string CaptionOf(IRenderedComponent<ConversationAgentCallPanel> cut)
        => cut.Find("[data-testid='conversation-agent-call-panel-status-caption']").TextContent.Trim();

    [Fact]
    public void Panel_visibly_names_hexa()
    {
        IRenderedComponent<ConversationAgentCallPanel> cut = RenderPanel();

        cut.Find("[data-testid='conversation-agent-call-panel-agent-name-value']").TextContent.Trim().ShouldBe("hexa");
        cut.Find("[data-testid='conversation-agent-call-panel-agent']").TextContent.Trim().ShouldBe("hexa");
    }

    [Fact]
    public void Panel_renders_a_fluent_prompt_input_and_no_raw_textarea()
    {
        IRenderedComponent<ConversationAgentCallPanel> cut = RenderPanel();

        cut.FindComponents<FluentTextArea>().Count.ShouldBe(1);
        cut.FindAll("textarea").ShouldBeEmpty();
        cut.Find("[data-testid='conversation-agent-call-panel-prompt']");
    }

    [Theory]
    [InlineData(AgentResponseMode.Automatic)]
    [InlineData(AgentResponseMode.Confirmation)]
    public void Panel_shows_the_response_mode_implication_for_both_modes(AgentResponseMode mode)
    {
        IRenderedComponent<ConversationAgentCallPanel> cut = RenderPanel(mode: mode);

        cut.Find("[data-testid='conversation-agent-call-panel-implication-automatic']").TextContent
            .ShouldContain("Agents.ConversationCall.Panel.Implication.Automatic");
        cut.Find("[data-testid='conversation-agent-call-panel-implication-confirmation']").TextContent
            .ShouldContain("Agents.ConversationCall.Panel.Implication.Confirmation");
    }

    [Fact]
    public void Panel_surfaces_the_source_conversation_context()
    {
        IRenderedComponent<ConversationAgentCallPanel> cut = RenderPanel(sourceConversationId: "conv-7");

        cut.Find("[data-testid='conversation-agent-call-panel-source-conversation']").TextContent.Trim().ShouldBe("conv-7");
    }

    [Fact]
    public async Task Submitting_calls_request_with_the_captured_source_conversation_and_prompt()
    {
        CallGateway.RequestCallAsync(Arg.Any<ConversationAgentCallRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentCallRequestResult.Accepted(
                new AgentInteractionReference("call-1", AgentInteractionStatus.Requested))));

        IRenderedComponent<ConversationAgentCallPanel> cut = RenderPanel(sourceConversationId: "conv-7");
        await TypePromptAsync(cut, Prompt);

        cut.Find("[data-testid='conversation-agent-call-panel-submit']").Click();

        await CallGateway.Received(1).RequestCallAsync(
            Arg.Is<ConversationAgentCallRequest>(r => r.SourceConversationId == "conv-7" && r.Prompt == Prompt),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Submitted_prompt_is_never_echoed_into_a_badge_label_accessible_name_or_test_id()
    {
        CallGateway.RequestCallAsync(Arg.Any<ConversationAgentCallRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentCallRequestResult.Accepted(
                new AgentInteractionReference("call-1", AgentInteractionStatus.Requested))));

        IRenderedComponent<ConversationAgentCallPanel> cut = RenderPanel();
        await TypePromptAsync(cut, Prompt);
        cut.Find("[data-testid='conversation-agent-call-panel-submit']").Click();

        foreach (IElement element in cut.FindAll("[aria-label]"))
        {
            (element.GetAttribute("aria-label") ?? string.Empty).ShouldNotContain(Prompt);
        }

        foreach (IElement element in cut.FindAll("[data-testid]"))
        {
            (element.GetAttribute("data-testid") ?? string.Empty).ShouldNotContain(Prompt);
        }
    }

    [Theory]
    [InlineData(AgentInteractionStatus.Requested)]
    [InlineData(AgentInteractionStatus.Authorized)]
    [InlineData(AgentInteractionStatus.ContextReady)]
    [InlineData(AgentInteractionStatus.Generated)]
    public async Task Panel_never_renders_posted_message_wording_for_a_non_posted_status(AgentInteractionStatus status)
    {
        CallGateway.RequestCallAsync(Arg.Any<ConversationAgentCallRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentCallRequestResult.Accepted(
                new AgentInteractionReference("call-1", status))));

        IRenderedComponent<ConversationAgentCallPanel> cut = RenderPanel();
        await TypePromptAsync(cut, Prompt);
        cut.Find("[data-testid='conversation-agent-call-panel-submit']").Click();

        string visible = cut.VisibleText().ToLowerInvariant();
        visible.ShouldNotContain("posted");
        visible.ShouldNotContain("sent");
    }

    [Fact]
    public void Submitting_an_empty_prompt_does_not_call_the_gateway_and_shows_no_feedback()
    {
        // The prompt is Required (AC1): clicking submit with no prompt is a no-op — the gateway is never called and no
        // in-flight/feedback wording is rendered. Guards the declared contract without an EditForm.
        IRenderedComponent<ConversationAgentCallPanel> cut = RenderPanel();

        cut.Find("[data-testid='conversation-agent-call-panel-submit']").Click();

        CallGateway.DidNotReceive().RequestCallAsync(
            Arg.Any<ConversationAgentCallRequest>(), Arg.Any<CancellationToken>());
        cut.FindAll("[data-testid='conversation-agent-call-panel-status-caption']").ShouldBeEmpty();
    }

    [Fact]
    public void Cancel_button_returns_control_to_the_trigger()
    {
        bool cancelled = false;
        IRenderedComponent<ConversationAgentCallPanel> cut = RenderPanel(onCancel: () => cancelled = true);

        cut.Find("[data-testid='conversation-agent-call-panel-cancel']").Click();

        cancelled.ShouldBeTrue();
    }

    [Fact]
    public void Escape_cancels_the_transient_panel_without_committing()
    {
        bool cancelled = false;
        IRenderedComponent<ConversationAgentCallPanel> cut = RenderPanel(onCancel: () => cancelled = true);

        cut.Find("[data-testid='conversation-agent-call-panel']").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        cancelled.ShouldBeTrue();
    }

    [Fact]
    public async Task A_denied_gateway_outcome_renders_a_failure_feedback_with_a_safe_reason_and_never_implies_a_post()
    {
        // The gateway fails closed to NotAuthorized; the panel maps it to the Denied UX state — a safe failure feedback
        // (coarse reason, assertive region) that is never worded as posted (AC1, AC3; AD-12/AD-14).
        IRenderedComponent<ConversationAgentCallPanel> cut =
            await SubmitWithResultAsync(AgentCallRequestResult.NotAuthorized());

        cut.FindComponent<AgentCallStatusBadge>().Instance.State.ShouldBe(AgentCallStatus.Denied);
        cut.Find("[data-testid='conversation-agent-call-panel-feedback-reason']").TextContent.Trim()
            .ShouldBe("Agents.CallStatus.Reason.Denied");
        CaptionOf(cut).ShouldBe("Agents.ConversationCall.Panel.Status.Failed");

        string visible = cut.VisibleText().ToLowerInvariant();
        visible.ShouldNotContain("posted");
        visible.ShouldNotContain("sent");
    }

    [Fact]
    public async Task A_rejected_gateway_outcome_renders_a_blocked_failure_feedback_with_a_safe_reason()
    {
        // A rejected request maps to the Blocked UX state (a dependency-class failure) — still a safe coarse reason,
        // never raw text, never posted wording (AC1, AC3).
        IRenderedComponent<ConversationAgentCallPanel> cut =
            await SubmitWithResultAsync(AgentCallRequestResult.Rejected());

        cut.FindComponent<AgentCallStatusBadge>().Instance.State.ShouldBe(AgentCallStatus.Blocked);
        cut.Find("[data-testid='conversation-agent-call-panel-feedback-reason']").TextContent.Trim()
            .ShouldBe("Agents.CallStatus.Reason.Blocked");
        CaptionOf(cut).ShouldBe("Agents.ConversationCall.Panel.Status.Failed");

        string visible = cut.VisibleText().ToLowerInvariant();
        visible.ShouldNotContain("posted");
        visible.ShouldNotContain("sent");
    }

    [Fact]
    public async Task An_unknown_gateway_outcome_renders_the_unknown_state_with_no_reason_and_no_posted_wording()
    {
        // An unrecognized outcome resolves to the Unknown sentinel — no reason, still "calling…", never posted (AC1).
        IRenderedComponent<ConversationAgentCallPanel> cut =
            await SubmitWithResultAsync(new AgentCallRequestResult(AgentCallRequestStatus.Unknown, null));

        cut.FindComponent<AgentCallStatusBadge>().Instance.State.ShouldBe(AgentCallStatus.Unknown);
        cut.FindAll("[data-testid='conversation-agent-call-panel-feedback-reason']").ShouldBeEmpty();
        CaptionOf(cut).ShouldBe("Agents.ConversationCall.Panel.Status.Calling");

        string visible = cut.VisibleText().ToLowerInvariant();
        visible.ShouldNotContain("posted");
        visible.ShouldNotContain("sent");
    }

    [Fact]
    public async Task A_generated_automatic_call_reads_posting_not_posted()
    {
        // The explicit AC1 guard, asserted positively: a Generated (Automatic) state shows the in-flight "posting…"
        // caption — never "posted" — because Conversations has not confirmed the final message (UX-DR22).
        IRenderedComponent<ConversationAgentCallPanel> cut = await SubmitWithResultAsync(
            AgentCallRequestResult.Accepted(new AgentInteractionReference("call-1", AgentInteractionStatus.Generated)),
            AgentResponseMode.Automatic);

        CaptionOf(cut).ShouldBe("Agents.ConversationCall.Panel.Status.Posting");
        cut.VisibleText().ToLowerInvariant().ShouldNotContain("posted");
    }

    [Theory]
    [InlineData(AgentInteractionStatus.Requested, "Agents.ConversationCall.Panel.Status.Calling")]
    [InlineData(AgentInteractionStatus.ContextReady, "Agents.ConversationCall.Panel.Status.Generating")]
    public async Task Accepted_in_flight_states_read_with_their_in_flight_caption(
        AgentInteractionStatus status, string expectedCaptionKey)
    {
        IRenderedComponent<ConversationAgentCallPanel> cut = await SubmitWithResultAsync(
            AgentCallRequestResult.Accepted(new AgentInteractionReference("call-1", status)));

        CaptionOf(cut).ShouldBe(expectedCaptionKey);
        cut.VisibleText().ToLowerInvariant().ShouldNotContain("posted");
    }

    [Fact]
    public async Task A_posted_result_is_the_only_state_that_reads_posted_and_renders_the_success_badge()
    {
        // The single legitimate "posted" wording — only when Conversations confirms the final message (UX-DR11/UX-DR22).
        IRenderedComponent<ConversationAgentCallPanel> cut = await SubmitWithResultAsync(
            AgentCallRequestResult.Accepted(new AgentInteractionReference("call-1", AgentInteractionStatus.Posted)));

        CaptionOf(cut).ShouldBe("Agents.ConversationCall.Panel.Status.Posted");
        cut.FindComponent<AgentCallStatusBadge>().Instance.State.ShouldBe(AgentCallStatus.Posted);
        cut.FindComponent<FluentBadge>().Instance.Color.ShouldBe(BadgeColor.Success);
    }

    [Fact]
    public void Panel_falls_back_to_the_localized_hexa_label_when_no_agent_display_name_is_supplied()
    {
        // With no AgentDisplayName, the panel still visibly names the Agent via the localized hexa whole string — it
        // never renders an empty name or a raw id (AC1; AD-14).
        IRenderedComponent<ConversationAgentCallPanel> cut = Render<ConversationAgentCallPanel>(
            parameters => parameters.Add(p => p.SourceConversationId, "conv-7"));

        cut.Find("[data-testid='conversation-agent-call-panel-agent-name-value']").TextContent.Trim()
            .ShouldBe("Agents.ConversationCall.Panel.AgentName");
        cut.Find("[data-testid='conversation-agent-call-panel-agent']").TextContent.Trim()
            .ShouldBe("Agents.ConversationCall.Panel.AgentName");
    }

    [Fact]
    public void Panel_falls_back_to_the_localized_you_caller_label_when_no_caller_reference_is_supplied()
    {
        IRenderedComponent<ConversationAgentCallPanel> cut = Render<ConversationAgentCallPanel>(parameters => parameters
            .Add(p => p.AgentDisplayName, "hexa")
            .Add(p => p.SourceConversationId, "conv-7"));

        cut.Find("[data-testid='conversation-agent-call-panel-caller']").TextContent.Trim()
            .ShouldBe("Agents.ConversationCall.Panel.Caller.You");
    }

    [Fact]
    public void Panel_surfaces_a_supplied_caller_reference_in_the_mono_context_slot()
    {
        // A supplied caller reference is a safe reference (not PII) and is shown verbatim in the mono context slot (AC1).
        IRenderedComponent<ConversationAgentCallPanel> cut = Render<ConversationAgentCallPanel>(parameters => parameters
            .Add(p => p.AgentDisplayName, "hexa")
            .Add(p => p.CallerReference, "party-9")
            .Add(p => p.SourceConversationId, "conv-7"));

        cut.Find("[data-testid='conversation-agent-call-panel-caller']").TextContent.Trim().ShouldBe("party-9");
    }
}
