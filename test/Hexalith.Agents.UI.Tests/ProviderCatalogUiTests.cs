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
    public async Task Create_requires_explicit_retention_training_regions_and_terms_reference()
    {
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([])));
        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-create']"));
        cut.Find("[data-testid='agents-provider-catalog-create']").Click();
        cut.Find("[data-testid='agents-provider-catalog-provider-input']").Change("openai");
        cut.Find("[data-testid='agents-provider-catalog-model-input']").Change("gpt-x");
        cut.Find("[data-testid='agents-provider-catalog-label-input']").Change("OpenAI GPT-x");

        await cut.Find("[data-testid='agents-provider-catalog-save']").ClickAsync(new MouseEventArgs());

        await CatalogGateway.DidNotReceive().CreateAsync(Arg.Any<CreateProviderModelEntry>(), Arg.Any<CancellationToken>());
        cut.Find("[data-testid='agents-provider-catalog-truth']").TextContent
            .ShouldContain("Agents.ProviderCatalog.Write.Status.ValidationFailed");
    }

    [Fact]
    public async Task Platform_operator_can_submit_scoped_tenant_enablement()
    {
        var terms = new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1);
        ProviderCatalogEntryView entry = AgentUiTestData.Entry("openai", "gpt-x") with { DataHandling = terms };
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([entry])));
        CatalogGateway.SetTenantEnablementAsync(Arg.Any<SetTenantProviderModelEnablement>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogWriteResult.Submitted(new ProviderCatalogCommandAcceptance(
                "openai", "gpt-x", "msg-tenant", "corr-tenant", AgentSetupTruthState.Submitted))));
        CatalogGateway.GetCommandOutcomeAsync("tenant-a", "msg-tenant", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.AwaitingProjection));
        CatalogGateway.GetTenantEnablementAsync("tenant-a", "openai", "gpt-x", Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(new TenantProviderEnablementInspectionResult(
                    ProviderCatalogInspectionStatus.Success, true, 2)),
                Task.FromResult(new TenantProviderEnablementInspectionResult(
                    ProviderCatalogInspectionStatus.Success, true, 3, LastEnablementMessageId: "another-writer")),
                Task.FromResult(new TenantProviderEnablementInspectionResult(
                    ProviderCatalogInspectionStatus.Success, true, 3, LastEnablementMessageId: "msg-tenant",
                    ProjectedCommandMessageIds: ["msg-tenant"])));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-set-tenant-enablement']"));
        cut.Find("[data-testid='agents-provider-catalog-set-tenant-enablement']").Click();
        cut.Find("[data-testid='agents-provider-catalog-enablement-tenant']").Change("tenant-a");
        await cut.Find("[data-testid='agents-provider-catalog-enablement-load']").ClickAsync(new MouseEventArgs());
        cut.Find("[data-testid='agents-provider-catalog-enablement-revision']").Change("2");
        Task submission = cut.Find("[data-testid='agents-provider-catalog-enablement-save']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-enablement-truth']").TextContent
            .ShouldContain("Agents.ProviderCatalog.Truth.Stage.AuthoritativePending"));
        await Task.Delay(25);
        Clock.Advance(TimeSpan.FromMilliseconds(200));
        await submission.WaitAsync(TimeSpan.FromSeconds(2));

        await CatalogGateway.Received().SetTenantEnablementAsync(
            Arg.Is<SetTenantProviderModelEnablement>(command => command.TenantId == "tenant-a"
                && command.ProviderId == "openai" && command.ModelId == "gpt-x"
                && command.Enabled && command.ExpectedRevision == 2
                && command.CurrentTerms == terms),
            Arg.Any<CancellationToken>());
        cut.Find("[data-testid='agents-provider-catalog-enablement-truth']").TextContent
            .ShouldContain("Agents.ProviderCatalog.Truth.Stage.ProjectionConfirmed");
    }

    [Fact]
    public void Initially_pending_tenant_projection_is_not_rendered_as_an_empty_catalog()
    {
        CatalogGateway.ListTenantEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantProviderCatalogInspectionResult(
                ProviderCatalogInspectionStatus.Success, [],
                TruthState: AgentSetupTruthState.AuthoritativePending)));

        IRenderedComponent<TenantProviderCatalog> cut = RenderPage<TenantProviderCatalog>();

        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-tenant-provider-state']")
            .ClassList.ShouldContain("agent-surface-state--stale"));
        cut.Markup.ShouldContain("Agents.ProviderCatalog.Truth.Stage.AuthoritativePending");
        cut.Markup.ShouldNotContain("agent-surface-state--empty");
    }

    [Fact]
    public async Task Tenant_admin_confirms_the_exact_terms_fields_and_justification()
    {
        var terms = new ProviderDataHandlingRecord(14, false, ["EU"], "terms-v2", 2);
        var entry = new TenantProviderCatalogEntryView("openai", "gpt-x", "OpenAI GPT-x", true,
            true, true, 128_000, 16_000, new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.Streaming, 2, new ProviderModelPricing("USD", 0m, 0m, 1),
            terms, false, "AcceptanceRequired", null, 1, 3);
        TenantProviderCatalogEntryView confirmed = entry with
        {
            DataHandlingStatus = "Current",
            InForceDataHandlingVersion = 2,
            TenantRevision = 4,
            LastDecisionMessageId = "another-writer",
        };
        TenantProviderCatalogEntryView confirmedOwn = confirmed with { LastDecisionMessageId = "msg-decision" };
        CatalogGateway.ListTenantEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(
                Task.FromResult(new TenantProviderCatalogInspectionResult(ProviderCatalogInspectionStatus.Success, [entry])),
                Task.FromResult(new TenantProviderCatalogInspectionResult(ProviderCatalogInspectionStatus.Success, [confirmed])),
                Task.FromResult(new TenantProviderCatalogInspectionResult(ProviderCatalogInspectionStatus.Success, [confirmedOwn],
                    ProjectedCommandMessageIds: ["msg-decision"])));
        CatalogGateway.DecideDataHandlingAsync(Arg.Any<DecideProviderDataHandling>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogWriteResult.Submitted(new ProviderCatalogCommandAcceptance(
                "openai", "gpt-x", "msg-decision", "corr-decision", AgentSetupTruthState.Submitted))));
        CatalogGateway.GetCommandOutcomeAsync("current", "msg-decision", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.AwaitingProjection));

        IRenderedComponent<TenantProviderCatalog> cut = RenderPage<TenantProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-tenant-provider-review']"));
        cut.Find("[data-testid='agents-tenant-provider-review']").Click();
        cut.Find("[data-testid='agents-tenant-provider-confirmation']");
        cut.Find("[data-testid='agents-tenant-provider-justification']").Change("Approved after review");
        Task submission = cut.Find("[data-testid='agents-tenant-provider-accept']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-tenant-provider-truth']").TextContent
            .ShouldContain("Agents.ProviderCatalog.Truth.Stage.AuthoritativePending"));
        await Task.Delay(25);
        Clock.Advance(TimeSpan.FromMilliseconds(200));
        await submission.WaitAsync(TimeSpan.FromSeconds(2));

        await CatalogGateway.Received().DecideDataHandlingAsync(
            Arg.Is<DecideProviderDataHandling>(decision => decision.ProviderId == "openai"
                && decision.ModelId == "gpt-x" && decision.DataHandlingVersion == 2
                && decision.Accepted && decision.Justification == "Approved after review"
                && decision.ExpectedRevision == 3 && decision.ConfirmedTerms == terms),
            Arg.Any<CancellationToken>());
        cut.Markup.ShouldNotContain(SentinelReference);
    }

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
            truthState: AgentSetupTruthState.ProjectionConfirmed) with
        {
            ProjectedCommandMessageIds = ["msg-1"],
        };

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
        CatalogGateway.GetCommandOutcomeAsync("system", "msg-1", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.AwaitingProjection));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-create']"));

        cut.Find("[data-testid='agents-provider-catalog-create']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-editor']"));
        cut.Find("[data-testid='agents-provider-catalog-provider-input']").Change("openai");
        cut.Find("[data-testid='agents-provider-catalog-model-input']").Change("gpt-x");
        cut.Find("[data-testid='agents-provider-catalog-label-input']").Change("OpenAI GPT-x");
        cut.Find("[data-testid='agents-provider-catalog-retention-input']").Change("30");
        cut.Find("[data-testid='agents-provider-catalog-regions-input']").Change("EU");
        cut.Find("[data-testid='agents-provider-catalog-terms-input']").Change("terms-v1");
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
            cut.Markup.ShouldContain("Agents.ProviderCatalog.Truth.Stage.ProjectionConfirmed");
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
            truthState: AgentSetupTruthState.ProjectionConfirmed) with
        {
            ProjectedCommandMessageIds = ["msg-2"],
        };

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
        CatalogGateway.GetCommandOutcomeAsync("system", "msg-2", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.AwaitingProjection));

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
