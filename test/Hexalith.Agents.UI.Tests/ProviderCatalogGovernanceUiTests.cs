using System.Globalization;
using System.Resources;

using Bunit;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.UI.Components.Pages;
using Hexalith.Agents.UI.Resources;
using Hexalith.Agents.UI.State;

using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.FluentUI.AspNetCore.Components;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>Tests the tenant terms review and the platform training-use control in both supported cultures.</summary>
public sealed class ProviderCatalogGovernanceUiTests : AgentsTestContext
{
    [Theory]
    [InlineData("load")]
    [InlineData("save")]
    public async Task Session_storage_failure_keeps_operator_write_controls_closed(string failure)
    {
        IPendingProviderCommandStore store = Substitute.For<IPendingProviderCommandStore>();
        store.LoadAsync(Arg.Any<CancellationToken>())
            .Returns(failure == "load"
                ? Task.FromException<IReadOnlyList<PendingProviderCommand>>(new InvalidOperationException("storage offline"))
                : Task.FromResult<IReadOnlyList<PendingProviderCommand>>([]));
        store.SaveAsync(Arg.Any<PendingProviderCommand>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("storage offline")));
        Services.AddSingleton(store);
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([
                AgentUiTestData.Entry("openai", "gpt-x")])));
        CatalogGateway.DisableAsync(Arg.Any<DisableProviderModelEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogWriteResult.Submitted(new ProviderCatalogCommandAcceptance(
                "openai", "gpt-x", "msg-pending", "corr-pending", AgentSetupTruthState.Submitted))));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-disable']"));
        if (failure == "save")
        {
            await cut.Find("[data-testid='agents-provider-catalog-disable']").ClickAsync(new MouseEventArgs());
            await CatalogGateway.Received(1).DisableAsync(Arg.Any<DisableProviderModelEntry>(),
                Arg.Any<CancellationToken>());
        }

        cut.Find("[data-testid='agents-provider-catalog-disable']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='agents-provider-catalog-create']").HasAttribute("disabled").ShouldBeTrue();
        await cut.Find("[data-testid='agents-provider-catalog-disable']").ClickAsync(new MouseEventArgs());
        await CatalogGateway.Received(failure == "save" ? 1 : 0).DisableAsync(
            Arg.Any<DisableProviderModelEntry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Failed_session_removal_keeps_the_exact_pending_model_locked()
    {
        IPendingProviderCommandStore store = Substitute.For<IPendingProviderCommandStore>();
        var retained = new PendingProviderCommand("ProviderCatalogMutation",
            ProviderCatalogIdentity.EntryId("openai", "gpt-x"), "system",
            new ProviderCatalogCommandAcceptance("openai", "gpt-x", "msg-retained", "corr-retained",
                AgentSetupTruthState.Submitted));
        store.LoadAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<PendingProviderCommand>>([retained]));
        store.RemoveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException(new InvalidOperationException("storage offline")));
        Services.AddSingleton(store);
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([
                AgentUiTestData.Entry("openai", "gpt-x")])));
        CatalogGateway.GetCommandOutcomeAsync("system", "msg-retained", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.AlreadyApplied));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-check-pending']"));
        await cut.Find("[data-testid='agents-provider-catalog-check-pending']").ClickAsync(new MouseEventArgs());

        cut.Find("[data-testid='agents-provider-catalog-disable']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='agents-provider-catalog-create']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='agents-provider-catalog-check-pending']").ShouldNotBeNull();
    }


    [Fact]
    public async Task Tenant_decision_lock_survives_page_navigation_until_its_exact_terminal_result()
    {
        var terms = new ProviderDataHandlingRecord(14, false, ["EU"], "terms-v1", 1);
        var entry = TenantEntry("openai", "gpt-x", terms);
        CatalogGateway.ListTenantEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantProviderCatalogInspectionResult(
                ProviderCatalogInspectionStatus.Success, [entry])));
        await PendingCommandStore.SaveAsync(new PendingProviderCommand("DataHandlingAcceptance",
            ProviderCatalogIdentity.EntryId("openai", "gpt-x"), "current",
            new ProviderCatalogCommandAcceptance("openai", "gpt-x", "msg-retained", "corr-retained",
                AgentSetupTruthState.Submitted)));
        CatalogGateway.GetCommandOutcomeAsync("current", "msg-retained", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.Submitted));

        IRenderedComponent<TenantProviderCatalog> first = RenderPage<TenantProviderCatalog>();
        first.WaitForAssertion(() => first.Find("[data-testid='agents-tenant-provider-review']"));
        first.Dispose();
        IRenderedComponent<TenantProviderCatalog> returned = RenderPage<TenantProviderCatalog>();
        returned.WaitForAssertion(() => returned.Find("[data-testid='agents-tenant-provider-review']"));
        returned.Find("[data-testid='agents-tenant-provider-review']").Click();
        returned.Find("[data-testid='agents-tenant-provider-justification']").Change("Reviewed");
        returned.Find("[data-testid='agents-tenant-provider-accept']").HasAttribute("disabled").ShouldBeTrue();
        await returned.Find("[data-testid='agents-tenant-provider-check-pending']").ClickAsync(new MouseEventArgs());
        (await PendingCommandStore.LoadAsync()).ShouldHaveSingleItem();
        await CatalogGateway.DidNotReceive().DecideDataHandlingAsync(
            Arg.Any<DecideProviderDataHandling>(), Arg.Any<CancellationToken>());

        CatalogGateway.GetCommandOutcomeAsync("current", "msg-retained", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.AlreadyApplied));
        await returned.Find("[data-testid='agents-tenant-provider-check-pending']").ClickAsync(new MouseEventArgs());
        (await PendingCommandStore.LoadAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Platform_mutation_lock_survives_page_navigation_and_blocks_only_its_model()
    {
        ProviderCatalogEntryView first = AgentUiTestData.Entry("a-provider", "model");
        ProviderCatalogEntryView second = AgentUiTestData.Entry("b-provider", "model");
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([first, second])));
        await PendingCommandStore.SaveAsync(new PendingProviderCommand("ProviderCatalogMutation",
            ProviderCatalogIdentity.EntryId("a-provider", "model"), "system",
            new ProviderCatalogCommandAcceptance("a-provider", "model", "msg-retained", "corr-retained",
                AgentSetupTruthState.Submitted), 2, ProviderModelStatus.Disabled));
        CatalogGateway.GetCommandOutcomeAsync("system", "msg-retained", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.Submitted));

        IRenderedComponent<ProviderCatalog> firstPage = RenderPage<ProviderCatalog>();
        firstPage.WaitForAssertion(() => firstPage.FindAll("[data-testid='agents-provider-catalog-disable']").Count.ShouldBe(2));
        firstPage.Dispose();
        IRenderedComponent<ProviderCatalog> returned = RenderPage<ProviderCatalog>();
        returned.WaitForAssertion(() => returned.Find("[data-testid='agents-provider-catalog-check-pending']"));
        returned.FindAll("[data-testid='agents-provider-catalog-disable']")[0].HasAttribute("disabled").ShouldBeTrue();
        returned.FindAll("[data-testid='agents-provider-catalog-disable']")[1].HasAttribute("disabled").ShouldBeFalse();
        await returned.Find("[data-testid='agents-provider-catalog-check-pending']").ClickAsync(new MouseEventArgs());
        (await PendingCommandStore.LoadAsync()).ShouldHaveSingleItem();
        await CatalogGateway.DidNotReceive().DisableAsync(
            Arg.Any<DisableProviderModelEntry>(), Arg.Any<CancellationToken>());

        CatalogGateway.GetCommandOutcomeAsync("system", "msg-retained", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.AlreadyApplied));
        await returned.Find("[data-testid='agents-provider-catalog-check-pending']").ClickAsync(new MouseEventArgs());
        (await PendingCommandStore.LoadAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Projected_catalog_message_does_not_confirm_a_nonterminal_command()
    {
        ProviderCatalogEntryView entry = AgentUiTestData.Entry("openai", "gpt-x");
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([entry])));
        CatalogGateway.GetEntryAsync("openai", "gpt-x", Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([entry]) with
            {
                ProjectedCommandMessageIds = ["msg-catalog"],
            }));
        await PendingCommandStore.SaveAsync(new PendingProviderCommand("ProviderCatalogMutation",
            ProviderCatalogIdentity.EntryId("openai", "gpt-x"), "system",
            new ProviderCatalogCommandAcceptance("openai", "gpt-x", "msg-catalog", "corr-catalog",
                AgentSetupTruthState.AuthoritativePending)));
        CatalogGateway.GetCommandOutcomeAsync("system", "msg-catalog", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.Submitted));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-check-pending']"));
        await cut.Find("[data-testid='agents-provider-catalog-check-pending']").ClickAsync(new MouseEventArgs());

        (await PendingCommandStore.LoadAsync()).ShouldHaveSingleItem();
        cut.Find("[data-testid='agents-provider-catalog-truth']").TextContent
            .ShouldContain("AuthoritativePending");
    }

    [Fact]
    public async Task Projected_enablement_message_does_not_confirm_a_nonterminal_command()
    {
        ProviderCatalogEntryView entry = AgentUiTestData.Entry("openai", "gpt-x");
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([entry])));
        CatalogGateway.GetTenantEnablementAsync("tenant-a", "openai", "gpt-x", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantProviderEnablementInspectionResult(
                ProviderCatalogInspectionStatus.Success, false, 1,
                ProjectedCommandMessageIds: ["msg-enablement"])));
        await PendingCommandStore.SaveAsync(new PendingProviderCommand("TenantProviderEnablement",
            $"tenant-a:{ProviderCatalogIdentity.EntryId("openai", "gpt-x")}", "tenant-a",
            new ProviderCatalogCommandAcceptance("openai", "gpt-x", "msg-enablement", "corr-enablement",
                AgentSetupTruthState.AuthoritativePending)));
        CatalogGateway.GetCommandOutcomeAsync("tenant-a", "msg-enablement", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.Submitted));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-enablement-check-pending']"));
        await cut.Find("[data-testid='agents-provider-catalog-enablement-check-pending']")
            .ClickAsync(new MouseEventArgs());

        (await PendingCommandStore.LoadAsync()).ShouldHaveSingleItem();
        cut.Find("[data-testid='agents-provider-catalog-enablement-truth']").TextContent
            .ShouldContain("AuthoritativePending");
    }

    [Fact]
    public async Task Projected_decision_message_does_not_confirm_a_nonterminal_command()
    {
        var terms = new ProviderDataHandlingRecord(14, false, ["EU"], "terms-v1", 1);
        TenantProviderCatalogEntryView entry = TenantEntry("openai", "gpt-x", terms);
        CatalogGateway.ListTenantEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantProviderCatalogInspectionResult(
                ProviderCatalogInspectionStatus.Success, [entry],
                ProjectedCommandMessageIds: ["msg-decision"])));
        await PendingCommandStore.SaveAsync(new PendingProviderCommand("DataHandlingAcceptance",
            ProviderCatalogIdentity.EntryId("openai", "gpt-x"), "current",
            new ProviderCatalogCommandAcceptance("openai", "gpt-x", "msg-decision", "corr-decision",
                AgentSetupTruthState.AuthoritativePending)));
        CatalogGateway.GetCommandOutcomeAsync("current", "msg-decision", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.Submitted));

        IRenderedComponent<TenantProviderCatalog> cut = RenderPage<TenantProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-tenant-provider-review']"));
        cut.Find("[data-testid='agents-tenant-provider-review']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-tenant-provider-check-pending']"));
        await cut.Find("[data-testid='agents-tenant-provider-check-pending']").ClickAsync(new MouseEventArgs());

        (await PendingCommandStore.LoadAsync()).ShouldHaveSingleItem();
        cut.Find("[data-testid='agents-tenant-provider-truth']").TextContent
            .ShouldContain("AuthoritativePending");
    }

    [Fact]
    public void Open_tenant_page_refreshes_grace_status_at_its_exclusive_deadline()
    {
        var terms = new ProviderDataHandlingRecord(14, false, ["EU"], "terms-v2", 2);
        DateTimeOffset deadline = Clock.GetUtcNow().AddSeconds(1);
        var grace = TenantEntry("openai", "gpt-x", terms) with
        {
            DataHandlingStatus = "Grace",
            GraceExpiresAt = deadline,
            IsSelectableForNewActiveUse = true,
        };
        var expired = grace with
        {
            DataHandlingStatus = "GraceExpired",
            IsSelectableForNewActiveUse = false,
        };
        int reads = 0;
        CatalogGateway.ListTenantEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                reads++;
                return Task.FromResult(new TenantProviderCatalogInspectionResult(
                    ProviderCatalogInspectionStatus.Success,
                    [Clock.GetUtcNow() < deadline ? grace : expired]));
            });

        IRenderedComponent<TenantProviderCatalog> cut = RenderPage<TenantProviderCatalog>();
        cut.WaitForAssertion(() => cut.VisibleText().ShouldContain("Agents.TenantProviders.Status.Grace"));

        Clock.Advance(TimeSpan.FromSeconds(1));

        cut.WaitForAssertion(() => cut.VisibleText().ShouldContain("Agents.TenantProviders.Status.GraceExpired"));
        reads.ShouldBeGreaterThan(1);
    }

    [Fact]
    public async Task Grace_refresh_clears_prior_decision_input_and_truth_when_terms_change()
    {
        var terms = new ProviderDataHandlingRecord(14, false, ["EU"], "terms-v2", 2);
        DateTimeOffset deadline = Clock.GetUtcNow().AddSeconds(1);
        TenantProviderCatalogEntryView grace = TenantEntry("openai", "gpt-x", terms) with
        {
            DataHandlingStatus = "Grace",
            GraceExpiresAt = deadline,
        };
        TenantProviderCatalogEntryView changed = grace with
        {
            DisplayLabel = "Changed terms model",
            DataHandling = terms with { RetentionDays = 7, DataHandlingVersion = 3 },
            DataHandlingStatus = "AcceptanceRequired",
            GraceExpiresAt = null,
        };
        CatalogGateway.ListTenantEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(new TenantProviderCatalogInspectionResult(
                ProviderCatalogInspectionStatus.Success, [Clock.GetUtcNow() < deadline ? grace : changed])));
        CatalogGateway.DecideDataHandlingAsync(Arg.Any<DecideProviderDataHandling>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogWriteResult.Submitted(new ProviderCatalogCommandAcceptance(
                "openai", "gpt-x", "msg-decided", "corr", AgentSetupTruthState.ProjectionConfirmed))));

        IRenderedComponent<TenantProviderCatalog> cut = RenderPage<TenantProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-tenant-provider-review']"));
        cut.Find("[data-testid='agents-tenant-provider-review']").Click();
        cut.Find("[data-testid='agents-tenant-provider-justification']").Change("Reviewed old terms");
        await cut.Find("[data-testid='agents-tenant-provider-accept']").ClickAsync(new MouseEventArgs());
        cut.Find("[data-testid='agents-tenant-provider-truth']").TextContent.ShouldContain("ProjectionConfirmed");

        Clock.Advance(TimeSpan.FromSeconds(1));

        cut.WaitForAssertion(() => cut.VisibleText().ShouldContain("Changed terms model"));
        cut.Find("[data-testid='agents-tenant-provider-justification']").GetAttribute("value").ShouldBe(string.Empty);
        cut.Find("[data-testid='agents-tenant-provider-accept']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='agents-tenant-provider-decline']").HasAttribute("disabled").ShouldBeTrue();
        cut.FindAll("[data-testid='agents-tenant-provider-truth']").ShouldBeEmpty();
        await CatalogGateway.Received(1).DecideDataHandlingAsync(Arg.Any<DecideProviderDataHandling>(),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("en", "Effective at", "Tightening declared", "Retention changed", "Allow training use", "Training use: Not allowed", "Allowed to Not allowed", "Platform Operator")]
    [InlineData("fr", "En vigueur", "Durcissement déclaré", "Conservation modifiée", "Autoriser l'utilisation", "Utilisation pour l'entraînement : Non autorisée", "Autorisée à Non autorisée", "Opérateur de plateforme")]
    public void Terms_review_renders_effective_time_and_complete_declared_diff_in_both_cultures(
        string cultureName, string effectiveLabel, string declarationLabel, string retentionLabel, string trainingLabel,
        string tenantTraining, string tighteningTraining, string roleBasis)
    {
        Services.AddSingleton<IStringLocalizer<AgentsResources>>(
            new EmbeddedResourceLocalizer(CultureInfo.GetCultureInfo(cultureName)));
        var diff = new ProviderDataHandlingFieldDiff(30, 14, true, false, ["US"], [], "terms-v1", "terms-v1");
        var declaration = new ProviderDataHandlingTighteningDeclaration(
            1, 2, diff, "operator-a", "Agents.PlatformOperator", new DateTimeOffset(2026, 6, 20, 10, 0, 0, TimeSpan.Zero));
        var terms = new ProviderDataHandlingRecord(14, false, ["EU"], "terms-v1", 2,
            new DateTimeOffset(2026, 6, 21, 10, 0, 0, TimeSpan.Zero), declaration);
        var entry = TenantEntry("openai", "gpt-x", terms);
        CatalogGateway.ListTenantEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantProviderCatalogInspectionResult(
                ProviderCatalogInspectionStatus.Success, [entry])));

        IRenderedComponent<TenantProviderCatalog> cut = RenderPage<TenantProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-tenant-provider-review']"));
        cut.Find("[data-testid='agents-tenant-provider-review']").Click();

        cut.Find("[data-testid='agents-tenant-provider-effective-at']").TextContent.ShouldContain(effectiveLabel);
        cut.Find("[data-testid='agents-tenant-provider-effective-at']").TextContent
            .ShouldContain(terms.EffectiveAt.ShouldNotBeNull().ToString(CultureInfo.GetCultureInfo(cultureName)));
        cut.Find("[data-testid='agents-tenant-provider-tightening']").TextContent.ShouldContain(declarationLabel);
        cut.Find("[data-testid='agents-tenant-provider-tightening']").TextContent.ShouldContain(roleBasis);
        cut.VisibleText().ShouldContain(tenantTraining);
        cut.VisibleText().ShouldContain(tighteningTraining);
        cut.Find("[data-testid='agents-tenant-provider-tightening']").TextContent
            .ShouldContain(declaration.DeclaredAt.ToString(CultureInfo.GetCultureInfo(cultureName)));
        cut.VisibleText().ShouldContain(retentionLabel);
        cut.VisibleText().ShouldContain("30");
        cut.VisibleText().ShouldContain("14");
        cut.VisibleText().ShouldContain("US");
        cut.VisibleText().ShouldContain("terms-v1");
        cut.VisibleText().ShouldContain("operator-a");

        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([])));
        IRenderedComponent<ProviderCatalog> editor = RenderPage<ProviderCatalog>();
        editor.WaitForAssertion(() => editor.Find("[data-testid='agents-provider-catalog-create']"));
        editor.Find("[data-testid='agents-provider-catalog-create']").Click();
        editor.VisibleText().ShouldContain(trainingLabel);
    }

    [Fact]
    public async Task Training_switch_emits_a_boolean_value_when_activated()
    {
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([])));
        CatalogGateway.CreateAsync(Arg.Any<CreateProviderModelEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogWriteResult.Submitted(new ProviderCatalogCommandAcceptance(
                "openai", "gpt-x", "msg-training", "corr-training", AgentSetupTruthState.ProjectionConfirmed))));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-create']"));
        cut.Find("[data-testid='agents-provider-catalog-create']").Click();
        var training = cut.Find("[data-testid='agents-provider-catalog-training-input']");
        training.GetAttribute("role").ShouldBe("switch");
        // FluentSwitch handles Space/Enter inside its web component; bUnit dispatches the resulting change.
        training.Change(true);
        cut.Find("[data-testid='agents-provider-catalog-text-generation-input']").Change(false);
        cut.Find("[data-testid='agents-provider-catalog-vision-input']").Change(true);

        cut.Find("[data-testid='agents-provider-catalog-provider-input']").Change("openai");
        cut.Find("[data-testid='agents-provider-catalog-model-input']").Change("gpt-x");
        cut.Find("[data-testid='agents-provider-catalog-label-input']").Change("OpenAI GPT-x");
        cut.Find("[data-testid='agents-provider-catalog-retention-input']").Change("30");
        cut.Find("[data-testid='agents-provider-catalog-regions-input']").Change("EU");
        cut.Find("[data-testid='agents-provider-catalog-terms-input']").Change("terms-v1");
        await cut.Find("[data-testid='agents-provider-catalog-save']").ClickAsync(new MouseEventArgs());

        await CatalogGateway.Received().CreateAsync(
            Arg.Is<CreateProviderModelEntry>(command => command.DataHandling != null
                && command.DataHandling.AllowsTrainingUse
                && !command.SupportsTextGeneration
                && command.SafeCapabilityFlags == ProviderModelCapabilityFlags.Vision), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Operator_can_stage_a_disabled_model_before_terms_are_complete()
    {
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([])));
        CatalogGateway.CreateAsync(Arg.Any<CreateProviderModelEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogWriteResult.Submitted(new ProviderCatalogCommandAcceptance(
                "openai", "staged", "msg-stage", "corr-stage", AgentSetupTruthState.ProjectionConfirmed))));
        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-create']"));
        cut.Find("[data-testid='agents-provider-catalog-create']").Click();
        cut.Find("[data-testid='agents-provider-catalog-create-enabled-input']").Change(false);
        cut.Find("[data-testid='agents-provider-catalog-provider-input']").Change("openai");
        cut.Find("[data-testid='agents-provider-catalog-model-input']").Change("staged");
        cut.Find("[data-testid='agents-provider-catalog-label-input']").Change("Staged model");

        await cut.Find("[data-testid='agents-provider-catalog-save']").ClickAsync(new MouseEventArgs());

        await CatalogGateway.Received(1).CreateAsync(Arg.Is<CreateProviderModelEntry>(command =>
            command.ProviderId == "openai" && command.ModelId == "staged"
            && !command.Enabled && command.DataHandling == null), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Operator_can_edit_disabled_staged_metadata_without_terms()
    {
        ProviderCatalogEntryView entry = AgentUiTestData.Entry("openai", "staged",
            status: ProviderModelStatus.Disabled) with { DataHandling = null };
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([entry])));
        CatalogGateway.UpdateAsync(Arg.Any<UpdateProviderModelEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogWriteResult.Submitted(new ProviderCatalogCommandAcceptance(
                "openai", "staged", "msg-edit", "corr-edit", AgentSetupTruthState.ProjectionConfirmed))));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-edit']"));
        cut.Find("[data-testid='agents-provider-catalog-edit']").Click();
        cut.Find("[data-testid='agents-provider-catalog-label-input']").Change("Revised staged model");
        cut.Find("[data-testid='agents-provider-catalog-input-price-input']").Change("2");
        await cut.Find("[data-testid='agents-provider-catalog-save']").ClickAsync(new MouseEventArgs());

        await CatalogGateway.Received(1).UpdateAsync(Arg.Is<UpdateProviderModelEntry>(command =>
            command.DisplayLabel == "Revised staged model" && command.DataHandling == null
            && command.Pricing.InputTokenUnitPrice == 2m), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Grid_keys_keep_slash_delimited_identifier_pairs_distinct()
    {
        ProviderCatalogIdentity.EntryId("a/b", "c")
            .ShouldNotBe(ProviderCatalogIdentity.EntryId("a", "b/c"));
        ProviderCatalogEntryView first = AgentUiTestData.Entry("a/b", "c");
        ProviderCatalogEntryView second = AgentUiTestData.Entry("a", "b/c");
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([first, second])));

        IRenderedComponent<ProviderCatalog> platform = RenderPage<ProviderCatalog>();
        platform.WaitForAssertion(() => platform.FindAll("[data-testid='agents-provider-catalog-edit']").Count.ShouldBe(2));
        Func<ProviderCatalogEntryView, object> platformKey = platform
            .FindComponent<FluentDataGrid<ProviderCatalogEntryView>>().Instance.ItemKey.ShouldNotBeNull();
        platformKey(first).ShouldNotBe(platformKey(second));

        var terms = new ProviderDataHandlingRecord(14, false, ["EU"], "terms-v1", 1);
        CatalogGateway.ListTenantEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantProviderCatalogInspectionResult(
                ProviderCatalogInspectionStatus.Success,
                [TenantEntry("a/b", "c", terms), TenantEntry("a", "b/c", terms)])));

        IRenderedComponent<TenantProviderCatalog> tenant = RenderPage<TenantProviderCatalog>();
        tenant.WaitForAssertion(() => tenant.FindAll("[data-testid='agents-tenant-provider-review']").Count.ShouldBe(2));
        Func<TenantProviderCatalogEntryView, object> tenantKey = tenant
            .FindComponent<FluentDataGrid<TenantProviderCatalogEntryView>>().Instance.ItemKey.ShouldNotBeNull();
        tenantKey(TenantEntry("a/b", "c", terms))
            .ShouldNotBe(tenantKey(TenantEntry("a", "b/c", terms)));
    }

    [Fact]
    public async Task Pending_enablement_for_one_tenant_does_not_lock_same_model_for_another()
    {
        var entry = AgentUiTestData.Entry() with
        {
            DataHandling = new ProviderDataHandlingRecord(14, false, ["EU"], "terms-v1", 1),
        };
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([entry])));
        CatalogGateway.SetTenantEnablementAsync(Arg.Any<SetTenantProviderModelEnablement>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                string tenantId = call.Arg<SetTenantProviderModelEnablement>().TenantId;
                return Task.FromResult(ProviderCatalogWriteResult.Submitted(new ProviderCatalogCommandAcceptance(
                    "openai", "gpt-x", tenantId == "tenant-a" ? "msg-a" : "msg-b", "corr",
                    tenantId == "tenant-a" ? AgentSetupTruthState.Submitted : AgentSetupTruthState.ProjectionConfirmed)));
            });
        CatalogGateway.GetCommandOutcomeAsync("tenant-a", "msg-a", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.Submitted));
        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-set-tenant-enablement']"));
        cut.Find("[data-testid='agents-provider-catalog-set-tenant-enablement']").Click();
        cut.Find("[data-testid='agents-provider-catalog-enablement-tenant']").Change("tenant-a");
        cut.Find("[data-testid='agents-provider-catalog-enablement-revision']").Change("5");
        cut.Find("[data-testid='agents-provider-catalog-enablement-enabled']").Change(false);
        Task pending = cut.Find("[data-testid='agents-provider-catalog-enablement-save']")
            .ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-enablement-check-pending']"));
        cut.Find("[data-testid='agents-provider-catalog-enablement-tenant']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='agents-provider-catalog-enablement-revision']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='agents-provider-catalog-enablement-enabled']").HasAttribute("disabled").ShouldBeTrue();
        Clock.Advance(TimeSpan.FromSeconds(8));
        await pending.WaitAsync(TimeSpan.FromSeconds(2));

        cut.Find("[data-testid='agents-provider-catalog-enablement-tenant']").Change("tenant-b");
        cut.Find("[data-testid='agents-provider-catalog-enablement-revision']").GetAttribute("value").ShouldBe("0");
        cut.Find("[data-testid='agents-provider-catalog-enablement-save']")
            .HasAttribute("disabled").ShouldBeFalse();
        await cut.Find("[data-testid='agents-provider-catalog-enablement-save']")
            .ClickAsync(new MouseEventArgs());
        await CatalogGateway.Received(1).SetTenantEnablementAsync(
            Arg.Is<SetTenantProviderModelEnablement>(command => command.TenantId == "tenant-b"
                && command.ExpectedRevision == 0 && command.Enabled),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Restored_enablements_are_checkable_before_the_catalog_list_finishes_loading()
    {
        TaskCompletionSource<ProviderCatalogInspectionResult> list = new();
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(list.Task);
        foreach (string tenant in new[] { "tenant-a", "tenant-b" })
        {
            await PendingCommandStore.SaveAsync(new PendingProviderCommand("TenantProviderEnablement",
                $"{tenant}:{ProviderCatalogIdentity.EntryId("openai", "gpt-x")}", tenant,
                new ProviderCatalogCommandAcceptance("openai", "gpt-x", $"msg-{tenant}", "corr",
                    AgentSetupTruthState.Submitted)));
            CatalogGateway.GetCommandOutcomeAsync(tenant, $"msg-{tenant}", Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(AgentSetupWriteStatus.Submitted));
        }

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='agents-provider-catalog-enablement-check-pending']")
            .Count.ShouldBe(2));
        await cut.FindAll("[data-testid='agents-provider-catalog-enablement-check-pending']")[1]
            .ClickAsync(new MouseEventArgs());
        await CatalogGateway.Received(1).GetCommandOutcomeAsync("tenant-b", "msg-tenant-b",
            Arg.Any<CancellationToken>());
        (await PendingCommandStore.LoadAsync()).Count.ShouldBe(2);
        list.SetResult(ProviderCatalogInspectionResult.Success([]));
    }

    [Fact]
    public async Task Enabling_a_tenant_without_platform_terms_shows_validation_failure()
    {
        ProviderCatalogEntryView entry = AgentUiTestData.Entry("openai", "staged",
            status: ProviderModelStatus.Disabled) with { DataHandling = null };
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([entry])));
        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-set-tenant-enablement']"));
        cut.Find("[data-testid='agents-provider-catalog-set-tenant-enablement']").Click();
        cut.Find("[data-testid='agents-provider-catalog-enablement-tenant']").Change("tenant-a");

        await cut.Find("[data-testid='agents-provider-catalog-enablement-save']").ClickAsync(new MouseEventArgs());

        cut.Find("[data-testid='agents-provider-catalog-enablement-truth']").TextContent
            .ShouldContain("ValidationFailed");
        await CatalogGateway.DidNotReceive().SetTenantEnablementAsync(
            Arg.Any<SetTenantProviderModelEnablement>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Loading_tenant_state_fills_revision_and_enabled_intent()
    {
        ProviderCatalogEntryView entry = AgentUiTestData.Entry();
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([entry])));
        CatalogGateway.GetTenantEnablementAsync("tenant-a", entry.ProviderId, entry.ModelId,
                Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantProviderEnablementInspectionResult(
                ProviderCatalogInspectionStatus.Success, false, 7)));
        CatalogGateway.SetTenantEnablementAsync(Arg.Any<SetTenantProviderModelEnablement>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogWriteResult.Submitted(new ProviderCatalogCommandAcceptance(
                entry.ProviderId, entry.ModelId, "msg-loaded", "corr", AgentSetupTruthState.ProjectionConfirmed))));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-set-tenant-enablement']"));
        cut.Find("[data-testid='agents-provider-catalog-set-tenant-enablement']").Click();
        cut.Find("[data-testid='agents-provider-catalog-enablement-tenant']").Change("tenant-a");
        await cut.Find("[data-testid='agents-provider-catalog-enablement-load']").ClickAsync(new MouseEventArgs());
        cut.Find("[data-testid='agents-provider-catalog-enablement-revision']").GetAttribute("value").ShouldBe("7");
        await cut.Find("[data-testid='agents-provider-catalog-enablement-save']").ClickAsync(new MouseEventArgs());

        await CatalogGateway.Received(1).SetTenantEnablementAsync(
            Arg.Is<SetTenantProviderModelEnablement>(command => command.TenantId == "tenant-a"
                && command.ExpectedRevision == 7 && !command.Enabled), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Enabled_decline_submits_the_exact_displayed_terms_and_revision()
    {
        var terms = new ProviderDataHandlingRecord(14, false, ["EU"], "terms-v2", 2);
        TenantProviderCatalogEntryView entry = TenantEntry("openai", "gpt-x", terms) with { TenantRevision = 4 };
        CatalogGateway.ListTenantEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantProviderCatalogInspectionResult(
                ProviderCatalogInspectionStatus.Success, [entry])));
        CatalogGateway.DecideDataHandlingAsync(Arg.Any<DecideProviderDataHandling>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogWriteResult.Submitted(new ProviderCatalogCommandAcceptance(
                "openai", "gpt-x", "msg-decline", "corr", AgentSetupTruthState.ProjectionConfirmed))));

        IRenderedComponent<TenantProviderCatalog> cut = RenderPage<TenantProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-tenant-provider-review']"));
        cut.Find("[data-testid='agents-tenant-provider-review']").Click();
        cut.Find("[data-testid='agents-tenant-provider-justification']").Change("Reviewed and declined");
        await cut.Find("[data-testid='agents-tenant-provider-decline']").ClickAsync(new MouseEventArgs());

        await CatalogGateway.Received(1).DecideDataHandlingAsync(
            Arg.Is<DecideProviderDataHandling>(decision => !decision.Accepted
                && decision.ProviderId == "openai" && decision.ModelId == "gpt-x"
                && decision.DataHandlingVersion == 2 && decision.ExpectedRevision == 4
                && decision.Justification == "Reviewed and declined" && decision.ConfirmedTerms == terms),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Terms_edit_submits_changed_fields_and_tightening_declaration()
    {
        ProviderCatalogEntryView entry = AgentUiTestData.Entry() with
        {
            DataHandling = new ProviderDataHandlingRecord(30, true, ["EU", "US"], "terms-v1", 1),
        };
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([entry])));
        CatalogGateway.UpdateAsync(Arg.Any<UpdateProviderModelEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogWriteResult.Submitted(new ProviderCatalogCommandAcceptance(
                entry.ProviderId, entry.ModelId, "msg-terms", "corr", AgentSetupTruthState.ProjectionConfirmed))));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-edit']"));
        cut.Find("[data-testid='agents-provider-catalog-edit']").Click();
        cut.Find("[data-testid='agents-provider-catalog-retention-input']").Change("14");
        cut.Find("[data-testid='agents-provider-catalog-training-input']").Change(false);
        cut.Find("[data-testid='agents-provider-catalog-regions-input']").Change("EU");
        cut.Find("[data-testid='agents-provider-catalog-declare-tightening']").Change(true);
        await cut.Find("[data-testid='agents-provider-catalog-save']").ClickAsync(new MouseEventArgs());

        await CatalogGateway.Received(1).UpdateAsync(
            Arg.Is<UpdateProviderModelEntry>(command => command.DeclareDataHandlingTightening
                && command.DataHandling != null && command.DataHandling.RetentionDays == 14
                && !command.DataHandling.AllowsTrainingUse
                && command.DataHandling.ProcessingRegions.Count == 1
                && command.DataHandling.ProcessingRegions[0] == "EU"
                && command.DataHandling.TermsReferenceId == "terms-v1"),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("catalog", AgentSetupWriteStatus.Rejected)]
    [InlineData("catalog", AgentSetupWriteStatus.Unavailable)]
    [InlineData("enablement", AgentSetupWriteStatus.Rejected)]
    [InlineData("enablement", AgentSetupWriteStatus.Unavailable)]
    [InlineData("decision", AgentSetupWriteStatus.Rejected)]
    [InlineData("decision", AgentSetupWriteStatus.Unavailable)]
    public async Task Terminal_outcome_releases_the_exact_pending_command_and_entry(
        string family, AgentSetupWriteStatus terminal)
    {
        var terms = new ProviderDataHandlingRecord(14, false, ["EU"], "terms-v1", 1);
        ProviderCatalogEntryView operatorEntry = AgentUiTestData.Entry("openai", "gpt-x") with { DataHandling = terms };
        string resourceKey = family == "enablement"
            ? $"tenant-a:{ProviderCatalogIdentity.EntryId("openai", "gpt-x")}" : ProviderCatalogIdentity.EntryId("openai", "gpt-x");
        string tenantId = family == "catalog" ? "system" : family == "decision" ? "current" : "tenant-a";
        string pendingFamily = family switch
        {
            "catalog" => "ProviderCatalogMutation",
            "enablement" => "TenantProviderEnablement",
            _ => "DataHandlingAcceptance",
        };
        await PendingCommandStore.SaveAsync(new PendingProviderCommand(pendingFamily, resourceKey, tenantId,
            new ProviderCatalogCommandAcceptance("openai", "gpt-x", "msg-terminal", "corr",
                AgentSetupTruthState.Submitted)));
        CatalogGateway.GetCommandOutcomeAsync(tenantId, "msg-terminal", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(terminal));

        if (family == "decision")
        {
            CatalogGateway.ListTenantEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(new TenantProviderCatalogInspectionResult(
                    ProviderCatalogInspectionStatus.Success, [TenantEntry("openai", "gpt-x", terms)])));
            IRenderedComponent<TenantProviderCatalog> page = RenderPage<TenantProviderCatalog>();
            page.WaitForAssertion(() => page.Find("[data-testid='agents-tenant-provider-review']"));
            page.Find("[data-testid='agents-tenant-provider-review']").Click();
            page.Find("[data-testid='agents-tenant-provider-justification']").Change("Reviewed");
            page.Find("[data-testid='agents-tenant-provider-accept']").HasAttribute("disabled").ShouldBeTrue();
            await page.Find("[data-testid='agents-tenant-provider-check-pending']").ClickAsync(new MouseEventArgs());
            page.Find("[data-testid='agents-tenant-provider-accept']").HasAttribute("disabled").ShouldBeFalse();
        }
        else
        {
            CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([operatorEntry])));
            IRenderedComponent<ProviderCatalog> page = RenderPage<ProviderCatalog>();
            if (family == "catalog")
            {
                page.WaitForAssertion(() => page.Find("[data-testid='agents-provider-catalog-check-pending']"));
                page.Find("[data-testid='agents-provider-catalog-disable']").HasAttribute("disabled").ShouldBeTrue();
                await page.Find("[data-testid='agents-provider-catalog-check-pending']").ClickAsync(new MouseEventArgs());
                page.Find("[data-testid='agents-provider-catalog-disable']").HasAttribute("disabled").ShouldBeFalse();
            }
            else
            {
                page.WaitForAssertion(() => page.Find("[data-testid='agents-provider-catalog-enablement-check-pending']"));
                page.Find("[data-testid='agents-provider-catalog-set-tenant-enablement']").Click();
                page.Find("[data-testid='agents-provider-catalog-enablement-tenant']").Change("tenant-a");
                page.Find("[data-testid='agents-provider-catalog-enablement-save']").HasAttribute("disabled").ShouldBeTrue();
                await page.Find("[data-testid='agents-provider-catalog-enablement-check-pending']").ClickAsync(new MouseEventArgs());
                page.Find("[data-testid='agents-provider-catalog-enablement-save']").HasAttribute("disabled").ShouldBeFalse();
            }
        }

        (await PendingCommandStore.LoadAsync()).ShouldBeEmpty();
    }

    [Fact]
    public async Task Timed_out_decision_keeps_its_entry_locked_until_exact_command_reaches_terminal_truth()
    {
        var terms = new ProviderDataHandlingRecord(14, false, ["EU"], "terms-v1", 1);
        var entry = TenantEntry("openai", "gpt-x", terms);
        CatalogGateway.ListTenantEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantProviderCatalogInspectionResult(
                ProviderCatalogInspectionStatus.Success, [entry])));
        CatalogGateway.DecideDataHandlingAsync(Arg.Any<DecideProviderDataHandling>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogWriteResult.Submitted(new ProviderCatalogCommandAcceptance(
                "openai", "gpt-x", "msg-pending", "corr-pending", AgentSetupTruthState.Submitted))));
        CatalogGateway.GetCommandOutcomeAsync("current", "msg-pending", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.Submitted));

        IRenderedComponent<TenantProviderCatalog> cut = RenderPage<TenantProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-tenant-provider-review']"));
        cut.Find("[data-testid='agents-tenant-provider-review']").Click();
        cut.Find("[data-testid='agents-tenant-provider-justification']").Change("Reviewed");
        Task submission = cut.Find("[data-testid='agents-tenant-provider-accept']").ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-tenant-provider-check-pending']"));
        Clock.Advance(TimeSpan.FromSeconds(8));
        await submission.WaitAsync(TimeSpan.FromSeconds(2));

        cut.Find("[data-testid='agents-tenant-provider-accept']").HasAttribute("disabled").ShouldBeTrue();
        cut.Find("[data-testid='agents-tenant-provider-decline']").HasAttribute("disabled").ShouldBeTrue();
        await cut.Find("[data-testid='agents-tenant-provider-decline']").ClickAsync(new MouseEventArgs());
        await CatalogGateway.Received(1).DecideDataHandlingAsync(
            Arg.Any<DecideProviderDataHandling>(), Arg.Any<CancellationToken>());

        CatalogGateway.GetCommandOutcomeAsync("current", "msg-pending", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.AlreadyApplied));
        await cut.Find("[data-testid='agents-tenant-provider-check-pending']").ClickAsync(new MouseEventArgs());
        cut.FindAll("[data-testid='agents-tenant-provider-check-pending']").ShouldBeEmpty();
        cut.Find("[data-testid='agents-tenant-provider-accept']").HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public async Task Operator_can_disable_without_terms_and_pending_enablement_locks_only_its_target()
    {
        ProviderCatalogEntryView first = AgentUiTestData.Entry("a-provider", "model");
        ProviderCatalogEntryView second = AgentUiTestData.Entry("b-provider", "model");
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([first, second])));
        CatalogGateway.SetTenantEnablementAsync(Arg.Any<SetTenantProviderModelEnablement>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                SetTenantProviderModelEnablement command = call.Arg<SetTenantProviderModelEnablement>();
                return Task.FromResult(ProviderCatalogWriteResult.Submitted(new ProviderCatalogCommandAcceptance(
                    command.ProviderId, command.ModelId,
                    command.ProviderId == "a-provider" ? "msg-first" : "msg-second", "corr",
                    command.ProviderId == "a-provider"
                        ? AgentSetupTruthState.Submitted : AgentSetupTruthState.ProjectionConfirmed)));
            });
        CatalogGateway.GetCommandOutcomeAsync("tenant-a", "msg-first", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.Submitted));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='agents-provider-catalog-set-tenant-enablement']")
            .Count.ShouldBe(2));
        cut.FindAll("[data-testid='agents-provider-catalog-set-tenant-enablement']")[0].Click();
        cut.Find("[data-testid='agents-provider-catalog-enablement-tenant']").Change("tenant-a");
        cut.Find("[data-testid='agents-provider-catalog-enablement-enabled']").Change(false);
        Task firstSubmission = cut.Find("[data-testid='agents-provider-catalog-enablement-save']")
            .ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-enablement-check-pending']"));
        Clock.Advance(TimeSpan.FromSeconds(8));
        await firstSubmission.WaitAsync(TimeSpan.FromSeconds(2));

        cut.Find("[data-testid='agents-provider-catalog-enablement-save']")
            .HasAttribute("disabled").ShouldBeTrue();
        await CatalogGateway.Received(1).SetTenantEnablementAsync(
            Arg.Is<SetTenantProviderModelEnablement>(command => command.ProviderId == "a-provider"
                && !command.Enabled && command.CurrentTerms == null), Arg.Any<CancellationToken>());

        cut.FindAll("[data-testid='agents-provider-catalog-set-tenant-enablement']")[1].Click();
        cut.Find("[data-testid='agents-provider-catalog-enablement-tenant']").Change("tenant-a");
        cut.Find("[data-testid='agents-provider-catalog-enablement-enabled']").Change(false);
        await cut.Find("[data-testid='agents-provider-catalog-enablement-save']")
            .ClickAsync(new MouseEventArgs());
        await CatalogGateway.Received(1).SetTenantEnablementAsync(
            Arg.Is<SetTenantProviderModelEnablement>(command => command.ProviderId == "b-provider"
                && !command.Enabled && command.CurrentTerms == null), Arg.Any<CancellationToken>());

        cut.FindAll("[data-testid='agents-provider-catalog-set-tenant-enablement']")[0].Click();
        cut.Find("[data-testid='agents-provider-catalog-enablement-tenant']").Change("tenant-a");
        cut.Find("[data-testid='agents-provider-catalog-enablement-truth']").TextContent
            .ShouldContain("AuthoritativePending");
        cut.Find("[data-testid='agents-provider-catalog-enablement-save']")
            .HasAttribute("disabled").ShouldBeTrue();
    }

    [Fact]
    public async Task Operator_can_edit_capabilities_and_clear_configuration_reference_without_rendering_it()
    {
        const string reference = "cfg-private-reference";
        var entry = AgentUiTestData.Entry(configurationReferenceId: reference,
            capabilities: ProviderModelCapabilityFlags.Streaming) with
        {
            DataHandling = new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1),
        };
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([entry])));
        CatalogGateway.UpdateAsync(Arg.Any<UpdateProviderModelEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogWriteResult.Submitted(new ProviderCatalogCommandAcceptance(
                "openai", "gpt-x", "msg-edit", "corr-edit", AgentSetupTruthState.ProjectionConfirmed))));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-edit']"));
        cut.Find("[data-testid='agents-provider-catalog-edit']").Click();
        cut.Markup.ShouldNotContain(reference);
        cut.Find("[data-testid='agents-provider-catalog-text-generation-input']").Change(false);
        cut.Find("[data-testid='agents-provider-catalog-streaming-input']").Change(false);
        cut.Find("[data-testid='agents-provider-catalog-vision-input']").Change(true);
        cut.Find("[data-testid='agents-provider-catalog-structured-output-input']").Change(true);
        cut.Find("[data-testid='agents-provider-catalog-clear-configuration-reference']").Change(true);
        await cut.Find("[data-testid='agents-provider-catalog-save']").ClickAsync(new MouseEventArgs());

        await CatalogGateway.Received(1).UpdateAsync(
            Arg.Is<UpdateProviderModelEntry>(command => !command.SupportsTextGeneration
                && command.SafeCapabilityFlags == (ProviderModelCapabilityFlags.Vision
                    | ProviderModelCapabilityFlags.StructuredOutput)
                && command.ConfigurationReferenceId == null
                && command.ExpectedCapabilityVersion == 1),
            Arg.Any<CancellationToken>());
        cut.Markup.ShouldNotContain(reference);
    }

    [Fact]
    public void Missing_operator_projection_shows_pending_instead_of_an_empty_catalog()
    {
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([]) with
            {
                TruthState = AgentSetupTruthState.AuthoritativePending,
                Freshness = AgentSetupFreshness.Stale,
            }));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();

        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-state']")
            .ClassList.ShouldContain("agent-surface-state--stale"));
        cut.Find("[data-testid='agents-provider-catalog-state']")
            .ClassList.ShouldNotContain("agent-surface-state--empty");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Lifecycle_toggle_sends_the_observed_lifecycle_revision(bool enable)
    {
        ProviderCatalogEntryView entry = AgentUiTestData.Entry(
            status: enable ? ProviderModelStatus.Disabled : ProviderModelStatus.Enabled) with { LifecycleRevision = 3 };
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([entry])));
        ProviderCatalogWriteResult submitted = ProviderCatalogWriteResult.Submitted(new ProviderCatalogCommandAcceptance(
            "openai", "gpt-x", "msg-toggle", "corr-toggle", AgentSetupTruthState.ProjectionConfirmed));
        CatalogGateway.EnableAsync(Arg.Any<EnableProviderModelEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(submitted));
        CatalogGateway.DisableAsync(Arg.Any<DisableProviderModelEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(submitted));
        string button = enable ? "agents-provider-catalog-enable" : "agents-provider-catalog-disable";

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find($"[data-testid='{button}']"));
        await cut.Find($"[data-testid='{button}']").ClickAsync(new MouseEventArgs());

        if (enable)
        {
            await CatalogGateway.Received(1).EnableAsync(
                Arg.Is<EnableProviderModelEntry>(command => command.ExpectedLifecycleRevision == 3),
                Arg.Any<CancellationToken>());
        }
        else
        {
            await CatalogGateway.Received(1).DisableAsync(
                Arg.Is<DisableProviderModelEntry>(command => command.ExpectedLifecycleRevision == 3),
                Arg.Any<CancellationToken>());
        }
    }

    [Fact]
    public async Task Timed_out_catalog_mutation_locks_only_its_entry_until_exact_outcome_is_known()
    {
        ProviderCatalogEntryView first = AgentUiTestData.Entry("a-provider", "model");
        ProviderCatalogEntryView second = AgentUiTestData.Entry("b-provider", "model");
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([first, second])));
        CatalogGateway.GetEntryAsync("a-provider", "model", Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.NotFound() with
            {
                TruthState = AgentSetupTruthState.AuthoritativePending,
            }));
        CatalogGateway.DisableAsync(Arg.Any<DisableProviderModelEntry>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                string provider = call.Arg<DisableProviderModelEntry>().ProviderId;
                return Task.FromResult(ProviderCatalogWriteResult.Submitted(new ProviderCatalogCommandAcceptance(
                    provider, "model", provider == "a-provider" ? "msg-first" : "msg-second", "corr",
                    provider == "a-provider" ? AgentSetupTruthState.Submitted : AgentSetupTruthState.ProjectionConfirmed)));
            });
        CatalogGateway.GetCommandOutcomeAsync("system", "msg-first", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.Submitted));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='agents-provider-catalog-disable']").Count.ShouldBe(2));
        Task firstSubmission = cut.FindAll("[data-testid='agents-provider-catalog-disable']")[0]
            .ClickAsync(new MouseEventArgs());
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-check-pending']"));
        Clock.Advance(TimeSpan.FromSeconds(8));
        await firstSubmission.WaitAsync(TimeSpan.FromSeconds(2));

        cut.FindAll("[data-testid='agents-provider-catalog-disable']")[0].HasAttribute("disabled").ShouldBeTrue();
        cut.FindAll("[data-testid='agents-provider-catalog-disable']")[1].HasAttribute("disabled").ShouldBeFalse();
        await cut.FindAll("[data-testid='agents-provider-catalog-disable']")[0].ClickAsync(new MouseEventArgs());
        await CatalogGateway.Received(1).DisableAsync(
            Arg.Is<DisableProviderModelEntry>(command => command.ProviderId == "a-provider"),
            Arg.Any<CancellationToken>());

        await cut.FindAll("[data-testid='agents-provider-catalog-disable']")[1].ClickAsync(new MouseEventArgs());
        await CatalogGateway.Received(1).DisableAsync(
            Arg.Is<DisableProviderModelEntry>(command => command.ProviderId == "b-provider"),
            Arg.Any<CancellationToken>());

        CatalogGateway.GetCommandOutcomeAsync("system", "msg-first", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.AlreadyApplied));
        await cut.Find("[data-testid='agents-provider-catalog-check-pending']").ClickAsync(new MouseEventArgs());
        cut.FindAll("[data-testid='agents-provider-catalog-check-pending']").ShouldBeEmpty();
        cut.FindAll("[data-testid='agents-provider-catalog-disable']")[0].HasAttribute("disabled").ShouldBeFalse();
    }

    [Fact]
    public async Task Selecting_another_entry_clears_the_prior_decision_truth()
    {
        var terms = new ProviderDataHandlingRecord(14, false, ["EU"], "terms-v1", 1);
        var first = TenantEntry("openai", "gpt-x", terms);
        var second = TenantEntry("anthropic", "claude-x", terms);
        CatalogGateway.ListTenantEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantProviderCatalogInspectionResult(
                ProviderCatalogInspectionStatus.Success, [first, second])));
        CatalogGateway.DecideDataHandlingAsync(Arg.Any<DecideProviderDataHandling>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogWriteResult.Submitted(new ProviderCatalogCommandAcceptance(
                "openai", "gpt-x", "msg-noop", "corr-noop", AgentSetupTruthState.ProjectionConfirmed))));

        IRenderedComponent<TenantProviderCatalog> cut = RenderPage<TenantProviderCatalog>();
        cut.WaitForAssertion(() => cut.FindAll("[data-testid='agents-tenant-provider-review']").Count.ShouldBe(2));
        cut.FindAll("[data-testid='agents-tenant-provider-review']")[0].Click();
        cut.Find("[data-testid='agents-tenant-provider-justification']").Change("Reviewed");
        cut.Find("[data-testid='agents-tenant-provider-accept']").Click();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-tenant-provider-truth']").TextContent
            .ShouldContain("ProjectionConfirmed"));

        cut.FindAll("[data-testid='agents-tenant-provider-review']")[1].Click();
        cut.FindAll("[data-testid='agents-tenant-provider-truth']").ShouldBeEmpty();
        cut.Find("[data-testid='agents-tenant-provider-justification']").GetAttribute("value")
            .ShouldBeNullOrEmpty();
        await CatalogGateway.DidNotReceive().GetCommandOutcomeAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Platform_authoritative_noop_confirms_without_waiting_for_projection()
    {
        var entry = AgentUiTestData.Entry("openai", "gpt-x");
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([entry])));
        CatalogGateway.DisableAsync(Arg.Any<DisableProviderModelEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogWriteResult.Submitted(new ProviderCatalogCommandAcceptance(
                "openai", "gpt-x", "msg-noop", "corr-noop", AgentSetupTruthState.ProjectionConfirmed))));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-disable']"));
        await cut.Find("[data-testid='agents-provider-catalog-disable']").ClickAsync(new MouseEventArgs());

        cut.Find("[data-testid='agents-provider-catalog-truth']").TextContent
            .ShouldContain("Agents.ProviderCatalog.Truth.Stage.ProjectionConfirmed");
        await CatalogGateway.DidNotReceive().GetCommandOutcomeAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        await CatalogGateway.DidNotReceive().GetEntryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Rejected_platform_command_shows_a_terminal_safe_failure()
    {
        var entry = AgentUiTestData.Entry("openai", "gpt-x");
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogInspectionResult.Success([entry])));
        CatalogGateway.DisableAsync(Arg.Any<DisableProviderModelEntry>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ProviderCatalogWriteResult.Submitted(new ProviderCatalogCommandAcceptance(
                "openai", "gpt-x", "msg-rejected", "corr-rejected", AgentSetupTruthState.Submitted))));
        CatalogGateway.GetCommandOutcomeAsync("system", "msg-rejected", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.Rejected));

        IRenderedComponent<ProviderCatalog> cut = RenderPage<ProviderCatalog>();
        cut.WaitForAssertion(() => cut.Find("[data-testid='agents-provider-catalog-disable']"));
        await cut.Find("[data-testid='agents-provider-catalog-disable']").ClickAsync(new MouseEventArgs());

        cut.Find("[data-testid='agents-provider-catalog-truth']").TextContent
            .ShouldContain("Agents.ProviderCatalog.Write.Status.Rejected");
        cut.Find("[data-testid='agents-provider-catalog-truth']").TextContent
            .ShouldNotContain("ProjectionConfirmed");
        await CatalogGateway.DidNotReceive().GetEntryAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>());
    }

    private static TenantProviderCatalogEntryView TenantEntry(
        string provider, string model, ProviderDataHandlingRecord terms)
        => new(provider, model, $"{provider} {model}", true, true, true, 128_000, 16_000,
            new ProviderModelTimeoutPolicy(30_000, 3), ProviderModelCapabilityFlags.Streaming, 2,
            new ProviderModelPricing("USD", 1m, 1m, 1), terms, false, "AcceptanceRequired", null, 1, 3);

    private sealed class EmbeddedResourceLocalizer(CultureInfo culture) : IStringLocalizer<AgentsResources>
    {
        private static readonly ResourceManager Resources = new(
            "Hexalith.Agents.UI.Resources.AgentsResources", typeof(AgentsResources).Assembly);

        public LocalizedString this[string name]
            => new(name, Resources.GetString(name, culture) ?? name);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, string.Format(culture, Resources.GetString(name, culture) ?? name, arguments));

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }
}
