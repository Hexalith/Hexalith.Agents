using System.Threading;
using System.Threading.Tasks;

using Bunit;

using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.UI.Components.Shared;
using Hexalith.Agents.UI.Services.Gateways;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AC1, AC2, AC3, AC4 — the proposal-regenerator control. An authorized Approver gets a regenerate action; a non-authorized
/// viewer gets none; generated, edited, and regenerated versions are labeled DISTINCTLY; regenerating calls the fail-closed
/// gateway with the safe ids-only request (no content) and surfaces the distinct safe status (Regenerated / NotAuthorized /
/// Unavailable / NotPending); and a successful regeneration raises OnRegenerated so the Story 3.7 host can refresh. The proposal
/// is never styled as a posted Conversation Message.
/// </summary>
public sealed class ProposalRegeneratorTests : AgentsTestContext
{
    [Fact]
    public void Authorized_viewer_gets_a_regenerate_action()
    {
        IRenderedComponent<ProposalRegenerator> cut = RenderRegenerator(canRegenerate: true);

        cut.Find("[data-testid='proposal-regenerator-regenerate']");
    }

    [Fact]
    public void Non_authorized_viewer_gets_no_regenerate_action()
    {
        IRenderedComponent<ProposalRegenerator> cut = RenderRegenerator(canRegenerate: false);

        cut.FindAll("[data-testid='proposal-regenerator-regenerate']").ShouldBeEmpty();
        // The preserved notice still renders (the control is informative even when the viewer cannot act).
        cut.Find("[data-testid='proposal-regenerator-preserved']");
    }

    [Theory]
    [InlineData(AgentGenerationKind.Generated, "Agents.GenerationKind.Label.Generated")]
    [InlineData(AgentGenerationKind.Edited, "Agents.GenerationKind.Label.Edited")]
    [InlineData(AgentGenerationKind.Regenerated, "Agents.GenerationKind.Label.Regenerated")]
    public void The_version_is_labeled_distinctly_by_kind(AgentGenerationKind kind, string expectedKey)
    {
        IRenderedComponent<ProposalRegenerator> cut = Render<ProposalRegenerator>(parameters => parameters
            .Add(e => e.CanRegenerate, true)
            .Add(e => e.Kind, kind));

        cut.Find("[data-testid='proposal-regenerator-version-label']").TextContent.Trim().ShouldBe(expectedKey);
    }

    [Fact]
    public void Regenerating_calls_the_gateway_with_the_safe_ids_only_request_and_shows_the_regenerated_status()
    {
        ProposalRegenerationGateway.RegenerateProposalAsync(Arg.Any<ProposalRegenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalRegenerationResult.Regenerated("regenerated-version-2")));

        IRenderedComponent<ProposalRegenerator> cut = Render<ProposalRegenerator>(parameters => parameters
            .Add(e => e.CanRegenerate, true)
            .Add(e => e.AgentInteractionId, "i1")
            .Add(e => e.ProposalId, "p1"));

        cut.Find("[data-testid='proposal-regenerator-regenerate']").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.ProposalRegenerator.Status.Regenerated"));
        ProposalRegenerationGateway.Received(1).RegenerateProposalAsync(
            Arg.Is<ProposalRegenerationRequest>(r => r.AgentInteractionId == "i1" && r.ProposalId == "p1"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void A_fail_closed_gateway_shows_the_not_authorized_status()
    {
        // The default substituted gateway returns NotAuthorized (AgentsTestContext) — never a fabricated success (AD-12).
        IRenderedComponent<ProposalRegenerator> cut = RenderRegenerator(canRegenerate: true);

        cut.Find("[data-testid='proposal-regenerator-regenerate']").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.ProposalRegenerator.Status.NotAuthorized"));
    }

    [Fact]
    public void An_unavailable_gateway_shows_the_unavailable_status()
    {
        ProposalRegenerationGateway.RegenerateProposalAsync(Arg.Any<ProposalRegenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalRegenerationResult.Unavailable()));

        IRenderedComponent<ProposalRegenerator> cut = RenderRegenerator(canRegenerate: true);

        cut.Find("[data-testid='proposal-regenerator-regenerate']").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.ProposalRegenerator.Status.Unavailable"));
    }

    [Fact]
    public void A_terminal_proposal_gateway_shows_the_not_pending_status()
    {
        // AC4 surfaced to the UI: a no-longer-pending proposal returns NotPending — the distinct terminal status, never a
        // fabricated success and never the NotAuthorized denial.
        ProposalRegenerationGateway.RegenerateProposalAsync(Arg.Any<ProposalRegenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalRegenerationResult.NotPending()));

        IRenderedComponent<ProposalRegenerator> cut = RenderRegenerator(canRegenerate: true);

        cut.Find("[data-testid='proposal-regenerator-regenerate']").Click();

        cut.WaitForAssertion(() => cut.Markup.ShouldContain("Agents.ProposalRegenerator.Status.NotPending"));
    }

    [Fact]
    public void A_successful_regeneration_raises_on_regenerated_with_the_result_so_the_host_can_refresh()
    {
        ProposalRegenerationGateway.RegenerateProposalAsync(Arg.Any<ProposalRegenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProposalRegenerationResult.Regenerated("regenerated-version-9")));
        ProposalRegenerationResult? captured = null;

        IRenderedComponent<ProposalRegenerator> cut = Render<ProposalRegenerator>(parameters => parameters
            .Add(e => e.CanRegenerate, true)
            .Add(e => e.OnRegenerated, r => { captured = r; }));

        cut.Find("[data-testid='proposal-regenerator-regenerate']").Click();

        cut.WaitForAssertion(() => captured.ShouldNotBeNull());
        captured!.Status.ShouldBe(ProposalRegenerationStatus.Regenerated);
        captured.RegeneratedVersionId.ShouldBe("regenerated-version-9");
    }

    [Fact]
    public void A_double_activation_does_not_double_submit_the_regeneration()
    {
        // Story 3.7 in-flight guard (the 3.4 control left no guard): while a regeneration is in flight, a second
        // activation must not dispatch a second request.
        TaskCompletionSource<ProposalRegenerationResult> pending = new();
        ProposalRegenerationGateway.RegenerateProposalAsync(Arg.Any<ProposalRegenerationRequest>(), Arg.Any<CancellationToken>())
            .Returns(pending.Task);

        IRenderedComponent<ProposalRegenerator> cut = RenderRegenerator(canRegenerate: true);

        cut.Find("[data-testid='proposal-regenerator-regenerate']").Click();
        cut.Find("[data-testid='proposal-regenerator-regenerate']").Click();

        pending.SetResult(ProposalRegenerationResult.Regenerated("regenerated-version-1"));

        cut.WaitForAssertion(() =>
            ProposalRegenerationGateway.Received(1).RegenerateProposalAsync(Arg.Any<ProposalRegenerationRequest>(), Arg.Any<CancellationToken>()));
    }

    private IRenderedComponent<ProposalRegenerator> RenderRegenerator(bool canRegenerate)
        => Render<ProposalRegenerator>(parameters => parameters
            .Add(e => e.CanRegenerate, canRegenerate)
            .Add(e => e.AgentInteractionId, "i1")
            .Add(e => e.ProposalId, "p1")
            .Add(e => e.Kind, AgentGenerationKind.Generated));
}
