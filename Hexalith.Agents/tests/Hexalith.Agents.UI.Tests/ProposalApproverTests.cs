using System.Threading;
using System.Threading.Tasks;

using AngleSharp.Dom;

using Bunit;

using Hexalith.Agents.UI.Components.Shared;
using Hexalith.Agents.UI.Services.Gateways;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>Tests for the reusable Story 3.5 proposal approval control.</summary>
public sealed class ProposalApproverTests : AgentsTestContext
{
    [Fact]
    public void Authorized_viewer_gets_an_approve_action()
    {
        IRenderedComponent<ProposalApprover> cut = RenderApprover(canApprove: true);

        cut.Find("[data-testid='proposal-approver-approve']");
    }

    [Fact]
    public void Non_authorized_viewer_gets_no_approve_action()
    {
        IRenderedComponent<ProposalApprover> cut = RenderApprover(canApprove: false);

        cut.FindAll("[data-testid='proposal-approver-approve']").ShouldBeEmpty();
    }

    [Fact]
    public void Approving_calls_gateway_with_selected_version_and_shows_posted_status()
    {
        ProposalApprovalGateway.ApproveProposalAsync(Arg.Any<ProposalApprovalRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalApprovalResult.Posted("version-2", "message-2")));

        IRenderedComponent<ProposalApprover> cut = RenderApprover(canApprove: true, selectedVersionId: "version-2");

        cut.Find("[data-testid='proposal-approver-approve']").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.ProposalApprover.Status.Posted"));
        ProposalApprovalGateway.Received(1).ApproveProposalAsync(
            Arg.Is<ProposalApprovalRequest>(r => r.AgentInteractionId == "i1" && r.ProposalId == "p1" && r.SelectedVersionId == "version-2"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Fail_closed_gateway_shows_not_authorized_status()
    {
        IRenderedComponent<ProposalApprover> cut = RenderApprover(canApprove: true);

        cut.Find("[data-testid='proposal-approver-approve']").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.ProposalApprover.Status.NotAuthorized"));
    }

    [Fact]
    public void Posting_failed_status_uses_alert_live_region()
    {
        ProposalApprovalGateway.ApproveProposalAsync(Arg.Any<ProposalApprovalRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalApprovalResult.PostingFailed("version-1")));

        IRenderedComponent<ProposalApprover> cut = RenderApprover(canApprove: true);

        cut.Find("[data-testid='proposal-approver-approve']").Click();

        cut.WaitForAssertion(() =>
        {
            IElement status = cut.Find("[data-testid='proposal-approver-status']");
            status.GetAttribute("role").ShouldBe("alert");
            status.GetAttribute("aria-live").ShouldBe("assertive");
            status.TextContent.ShouldBe("Agents.ProposalApprover.Status.PostingFailed");
        });
    }

    [Fact]
    public void Successful_approval_raises_host_refresh_callback()
    {
        ProposalApprovalGateway.ApproveProposalAsync(Arg.Any<ProposalApprovalRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalApprovalResult.PostingPending("version-1")));
        ProposalApprovalResult? captured = null;

        IRenderedComponent<ProposalApprover> cut = Render<ProposalApprover>(parameters => parameters
            .Add(e => e.CanApprove, true)
            .Add(e => e.AgentInteractionId, "i1")
            .Add(e => e.ProposalId, "p1")
            .Add(e => e.SelectedVersionId, "version-1")
            .Add(e => e.OnApproved, result => { captured = result; }));

        cut.Find("[data-testid='proposal-approver-approve']").Click();

        cut.WaitForAssertion(() => captured.ShouldNotBeNull());
        captured!.Status.ShouldBe(ProposalApprovalStatus.PostingPending);
        captured.SelectedVersionId.ShouldBe("version-1");
    }

    [Fact]
    public void Empty_selected_version_short_circuits_to_unavailable_without_calling_the_gateway()
    {
        IRenderedComponent<ProposalApprover> cut = RenderApprover(canApprove: true, selectedVersionId: string.Empty);

        cut.Find("[data-testid='proposal-approver-approve']").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.ProposalApprover.Status.Unavailable"));
        ProposalApprovalGateway.DidNotReceive().ApproveProposalAsync(Arg.Any<ProposalApprovalRequest>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(ProposalApprovalStatus.Approved, "Agents.ProposalApprover.Status.Approved", "status", "polite")]
    [InlineData(ProposalApprovalStatus.NotPending, "Agents.ProposalApprover.Status.NotPending", "alert", "assertive")]
    [InlineData(ProposalApprovalStatus.Unavailable, "Agents.ProposalApprover.Status.Unavailable", "alert", "assertive")]
    public void Gateway_status_renders_its_whole_string_key_in_the_correct_live_region(
        ProposalApprovalStatus status, string expectedKey, string expectedRole, string expectedLive)
    {
        ProposalApprovalGateway.ApproveProposalAsync(Arg.Any<ProposalApprovalRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new ProposalApprovalResult(status, "version-1", null)));

        IRenderedComponent<ProposalApprover> cut = RenderApprover(canApprove: true);

        cut.Find("[data-testid='proposal-approver-approve']").Click();

        cut.WaitForAssertion(() =>
        {
            IElement region = cut.Find("[data-testid='proposal-approver-status']");
            region.TextContent.ShouldBe(expectedKey);
            region.GetAttribute("role").ShouldBe(expectedRole);
            region.GetAttribute("aria-live").ShouldBe(expectedLive);
        });
    }

    private IRenderedComponent<ProposalApprover> RenderApprover(bool canApprove, string selectedVersionId = "version-1")
        => Render<ProposalApprover>(parameters => parameters
            .Add(e => e.CanApprove, canApprove)
            .Add(e => e.AgentInteractionId, "i1")
            .Add(e => e.ProposalId, "p1")
            .Add(e => e.SelectedVersionId, selectedVersionId));
}
