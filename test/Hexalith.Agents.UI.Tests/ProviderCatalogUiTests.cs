using System.Threading;
using System.Threading.Tasks;

using Bunit;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.UI.Components.Pages;

using Microsoft.AspNetCore.Components.Web;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// Story 5.3 UI truth-flow: a catalog write shows submitted then projection-confirmed, and never treats Success
/// as callability or renders a secret reference.
/// </summary>
public sealed class ProviderCatalogUiTests : AgentsTestContext
{
    private const string SentinelReference = "SECRET-REF-DO-NOT-RENDER-9f3a";

    [Fact]
    public async Task A_submitted_create_polls_until_projection_confirmed()
    {
        ProviderCatalogInspectionResult empty = ProviderCatalogInspectionResult.Success([]);
        ProviderCatalogInspectionResult pendingMiss = ProviderCatalogInspectionResult.NotFound() with
        {
            Freshness = AgentSetupFreshness.Stale,
            TruthState = AgentSetupTruthState.AuthoritativePending,
        };
        ProviderCatalogInspectionResult confirmed = ProviderCatalogInspectionResult.Success(
            [AgentUiTestData.Entry("openai", "gpt-x")],
            projectionVersion: "1",
            projectedAt: new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero),
            freshness: AgentSetupFreshness.Current,
            truthState: AgentSetupTruthState.ProjectionConfirmed);

        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(empty),
                Task.FromResult(empty),
                Task.FromResult(confirmed));
        CatalogGateway.GetEntryAsync("openai", "gpt-x", 1, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(pendingMiss),
                Task.FromResult(confirmed));
        CatalogGateway.CreateAsync(Arg.Any<CreateProviderModelEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogWriteResult.Submitted(
                new ProviderCatalogCommandAcceptance(
                    "openai",
                    "gpt-x",
                    "msg-1",
                    "corr-1",
                    AgentSetupTruthState.Submitted))));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-create']"));

        cut.Find("[data-testid='agents-provider-catalog-create']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-editor']"));
        cut.Find("[data-testid='agents-provider-catalog-provider-input']").Change("openai");
        cut.Find("[data-testid='agents-provider-catalog-model-input']").Change("gpt-x");
        cut.Find("[data-testid='agents-provider-catalog-label-input']").Change("OpenAI GPT-x");
        Task submission = cut.Find("[data-testid='agents-provider-catalog-save']").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-provider-catalog-truth']").TextContent
                .ShouldContain("Agents.ProviderCatalog.Truth.Stage.AuthoritativePending");
        });

        Clock.Advance(TimeSpan.FromMilliseconds(200));
        await submission;

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-provider-catalog-grid']");
            cut.Markup.ShouldContain("Agents.ProviderCatalog.Freshness.Current");
            cut.Markup.ShouldNotContain(SentinelReference);
            cut.Markup.ShouldNotContain("Callable");
        });

        await CatalogGateway.Received().CreateAsync(Arg.Any<CreateProviderModelEntry>(), Arg.Any<CancellationToken>());
        await CatalogGateway.Received().GetEntryAsync("openai", "gpt-x", 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Disable_polls_until_the_row_status_matches()
    {
        ProviderCatalogEntryView enabled = AgentUiTestData.Entry("openai", "gpt-x");
        ProviderCatalogEntryView disabled = enabled with
        {
            Status = ProviderModelStatus.Disabled,
            IsSelectableForNewActiveUse = false,
        };
        ProviderCatalogInspectionResult enabledList = ProviderCatalogInspectionResult.Success(
            [enabled],
            projectionVersion: "1",
            freshness: AgentSetupFreshness.Current,
            truthState: AgentSetupTruthState.ProjectionConfirmed);
        ProviderCatalogInspectionResult disabledConfirmed = ProviderCatalogInspectionResult.Success(
            [disabled],
            projectionVersion: "2",
            freshness: AgentSetupFreshness.Current,
            truthState: AgentSetupTruthState.ProjectionConfirmed);

        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(enabledList),
                Task.FromResult(enabledList),
                Task.FromResult(disabledConfirmed));
        CatalogGateway.GetEntryAsync("openai", "gpt-x", null, Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(enabledList),
                Task.FromResult(disabledConfirmed));
        CatalogGateway.DisableAsync(Arg.Any<DisableProviderModelEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogWriteResult.Submitted(
                new ProviderCatalogCommandAcceptance(
                    "openai",
                    "gpt-x",
                    "msg-2",
                    "corr-2",
                    AgentSetupTruthState.Submitted))));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-disable']"));
        Task submission = cut.Find("[data-testid='agents-provider-catalog-disable']").ClickAsync(new MouseEventArgs());

        cut.WaitForAssertion(() =>
        {
            cut.Find("[data-testid='agents-provider-catalog-truth']").TextContent
                .ShouldContain("Agents.ProviderCatalog.Truth.Stage.AuthoritativePending");
        });

        Clock.Advance(TimeSpan.FromMilliseconds(200));
        await submission;

        await CatalogGateway.Received().DisableAsync(Arg.Any<DisableProviderModelEntry>(), Arg.Any<CancellationToken>());
        await CatalogGateway.Received().GetEntryAsync("openai", "gpt-x", null, Arg.Any<CancellationToken>());
        await CatalogGateway.DidNotReceive().GetEntryAsync("openai", "gpt-x", 1, Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Opening_the_editor_does_not_render_the_stored_configuration_reference()
    {
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success(
                [AgentUiTestData.Entry("openai", "gpt-x", configurationReferenceId: SentinelReference)],
                projectionVersion: "1",
                freshness: AgentSetupFreshness.Current,
                truthState: AgentSetupTruthState.ProjectionConfirmed)));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-edit']"));
        cut.Find("[data-testid='agents-provider-catalog-edit']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-configuration-reference-input']"));

        cut.Markup.ShouldNotContain(SentinelReference);
        cut.VisibleText().ShouldNotContain(SentinelReference);
        string.IsNullOrEmpty(
                cut.Find("[data-testid='agents-provider-catalog-configuration-reference-input']")
                    .GetAttribute("value"))
            .ShouldBeTrue();
        cut.Find("[data-testid='agents-provider-catalog-configuration-reference-input']")
            .GetAttribute("autocomplete")
            .ShouldBe("off");
    }
}
