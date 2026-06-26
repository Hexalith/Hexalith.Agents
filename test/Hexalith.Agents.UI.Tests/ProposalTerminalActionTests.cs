using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Dom;

using Bunit;

using Hexalith.Agents.UI.Components.Shared;
using Hexalith.Agents.UI.Services.Gateways;

using Microsoft.AspNetCore.Components.Web;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>Tests for the reusable Story 3.6 proposal reject/abandon terminal action controls.</summary>
public sealed class ProposalTerminalActionTests : AgentsTestContext
{
    [Fact]
    public void Authorized_viewer_gets_reject_and_abandon_actions()
    {
        RenderRejector(canReject: true).Find("[data-testid='proposal-rejector-reject']");
        RenderAbandoner(canAbandon: true).Find("[data-testid='proposal-abandoner-abandon']");
    }

    [Fact]
    public void Non_authorized_viewer_gets_no_terminal_actions()
    {
        RenderRejector(canReject: false).FindAll("[data-testid='proposal-rejector-reject']").ShouldBeEmpty();
        RenderAbandoner(canAbandon: false).FindAll("[data-testid='proposal-abandoner-abandon']").ShouldBeEmpty();
    }

    [Fact]
    public void Rejecting_calls_gateway_with_safe_ids_and_shows_rejected_status()
    {
        ProposalRejectionGateway.RejectProposalAsync(Arg.Any<ProposalRejectionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalRejectionResult.Rejected()));

        IRenderedComponent<ProposalRejector> cut = RenderRejector(canReject: true);

        cut.Find("[data-testid='proposal-rejector-reject']").Click();
        cut.Find("[data-testid='proposal-rejector-rationale']");
        cut.Find("[data-testid='proposal-rejector-confirm']").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.ProposalRejector.Status.Rejected"));
        ProposalRejectionGateway.Received(1).RejectProposalAsync(
            Arg.Is<ProposalRejectionRequest>(r =>
                r.AgentInteractionId == "i1"
                && r.ProposalId == "p1"
                && r.RationaleCode == null),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Abandoning_calls_gateway_with_safe_ids_and_shows_abandoned_status()
    {
        ProposalAbandonmentGateway.AbandonProposalAsync(Arg.Any<ProposalAbandonmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalAbandonmentResult.Abandoned()));

        IRenderedComponent<ProposalAbandoner> cut = RenderAbandoner(canAbandon: true);

        cut.Find("[data-testid='proposal-abandoner-abandon']").Click();
        cut.Find("[data-testid='proposal-abandoner-confirm']").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.ProposalAbandoner.Status.Abandoned"));
        ProposalAbandonmentGateway.Received(1).AbandonProposalAsync(
            Arg.Is<ProposalAbandonmentRequest>(r => r.AgentInteractionId == "i1" && r.ProposalId == "p1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Fail_closed_gateways_show_not_authorized_statuses()
    {
        IRenderedComponent<ProposalRejector> rejector = RenderRejector(canReject: true);
        rejector.Find("[data-testid='proposal-rejector-reject']").Click();
        rejector.Find("[data-testid='proposal-rejector-confirm']").Click();

        IRenderedComponent<ProposalAbandoner> abandoner = RenderAbandoner(canAbandon: true);
        abandoner.Find("[data-testid='proposal-abandoner-abandon']").Click();
        abandoner.Find("[data-testid='proposal-abandoner-confirm']").Click();

        rejector.WaitForAssertion(() => rejector.Markup.ShouldContain("Agents.ProposalRejector.Status.NotAuthorized"));
        abandoner.WaitForAssertion(() => abandoner.Markup.ShouldContain("Agents.ProposalAbandoner.Status.NotAuthorized"));
    }

    [Fact]
    public void Escape_cancels_confirmation_without_calling_gateways()
    {
        IRenderedComponent<ProposalRejector> rejector = RenderRejector(canReject: true);
        rejector.Find("[data-testid='proposal-rejector-reject']").Click();
        rejector.Find("[data-testid='proposal-rejector']").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        IRenderedComponent<ProposalAbandoner> abandoner = RenderAbandoner(canAbandon: true);
        abandoner.Find("[data-testid='proposal-abandoner-abandon']").Click();
        abandoner.Find("[data-testid='proposal-abandoner']").KeyDown(new KeyboardEventArgs { Key = "Escape" });

        rejector.FindAll("[data-testid='proposal-rejector-confirm']").ShouldBeEmpty();
        abandoner.FindAll("[data-testid='proposal-abandoner-confirm']").ShouldBeEmpty();
        ProposalRejectionGateway.DidNotReceive().RejectProposalAsync(Arg.Any<ProposalRejectionRequest>(), Arg.Any<CancellationToken>());
        ProposalAbandonmentGateway.DidNotReceive().AbandonProposalAsync(Arg.Any<ProposalAbandonmentRequest>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Successful_terminal_actions_raise_host_refresh_callbacks()
    {
        ProposalRejectionGateway.RejectProposalAsync(Arg.Any<ProposalRejectionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalRejectionResult.Rejected()));
        ProposalAbandonmentGateway.AbandonProposalAsync(Arg.Any<ProposalAbandonmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalAbandonmentResult.Abandoned()));
        ProposalRejectionResult? rejection = null;
        ProposalAbandonmentResult? abandonment = null;

        IRenderedComponent<ProposalRejector> rejector = Render<ProposalRejector>(parameters => parameters
            .Add(e => e.CanReject, true)
            .Add(e => e.AgentInteractionId, "i1")
            .Add(e => e.ProposalId, "p1")
            .Add(e => e.OnRejected, result => { rejection = result; }));
        IRenderedComponent<ProposalAbandoner> abandoner = Render<ProposalAbandoner>(parameters => parameters
            .Add(e => e.CanAbandon, true)
            .Add(e => e.AgentInteractionId, "i1")
            .Add(e => e.ProposalId, "p1")
            .Add(e => e.OnAbandoned, result => { abandonment = result; }));

        rejector.Find("[data-testid='proposal-rejector-reject']").Click();
        rejector.Find("[data-testid='proposal-rejector-confirm']").Click();
        abandoner.Find("[data-testid='proposal-abandoner-abandon']").Click();
        abandoner.Find("[data-testid='proposal-abandoner-confirm']").Click();

        rejector.WaitForAssertion(() => rejection.ShouldNotBeNull());
        abandoner.WaitForAssertion(() => abandonment.ShouldNotBeNull());
        rejection!.Status.ShouldBe(ProposalRejectionStatus.Rejected);
        abandonment!.Status.ShouldBe(ProposalAbandonmentStatus.Abandoned);
    }

    [Fact]
    public void Rejection_not_pending_status_uses_whole_localized_string_and_alert_region()
    {
        ProposalRejectionGateway.RejectProposalAsync(Arg.Any<ProposalRejectionRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalRejectionResult.NotPending()));

        IRenderedComponent<ProposalRejector> cut = RenderRejector(canReject: true);
        cut.Find("[data-testid='proposal-rejector-reject']").Click();
        cut.Find("[data-testid='proposal-rejector-confirm']").Click();
        cut.WaitForAssertion(() =>
        {
            IElement region = cut.Find("[data-testid='proposal-rejector-status']");
            region.TextContent.ShouldBe("Agents.ProposalRejector.Status.NotPending");
            region.GetAttribute("role").ShouldBe("alert");
            region.GetAttribute("aria-live").ShouldBe("assertive");
        });
    }

    [Fact]
    public void Abandonment_not_pending_status_uses_whole_localized_string_and_alert_region()
    {
        ProposalAbandonmentGateway.AbandonProposalAsync(Arg.Any<ProposalAbandonmentRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalAbandonmentResult.NotPending()));

        IRenderedComponent<ProposalAbandoner> cut = RenderAbandoner(canAbandon: true);
        cut.Find("[data-testid='proposal-abandoner-abandon']").Click();
        cut.Find("[data-testid='proposal-abandoner-confirm']").Click();
        cut.WaitForAssertion(() =>
        {
            IElement region = cut.Find("[data-testid='proposal-abandoner-status']");
            region.TextContent.ShouldBe("Agents.ProposalAbandoner.Status.NotPending");
            region.GetAttribute("role").ShouldBe("alert");
            region.GetAttribute("aria-live").ShouldBe("assertive");
        });
    }

    private IRenderedComponent<ProposalRejector> RenderRejector(bool canReject)
        => Render<ProposalRejector>(parameters => parameters
            .Add(e => e.CanReject, canReject)
            .Add(e => e.AgentInteractionId, "i1")
            .Add(e => e.ProposalId, "p1"));

    private IRenderedComponent<ProposalAbandoner> RenderAbandoner(bool canAbandon)
        => Render<ProposalAbandoner>(parameters => parameters
            .Add(e => e.CanAbandon, canAbandon)
            .Add(e => e.AgentInteractionId, "i1")
            .Add(e => e.ProposalId, "p1"));
}
