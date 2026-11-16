using Bunit;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.UI.Components.Pages;
using Hexalith.Agents.UI.State;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>Write-ahead ordering and exact pending identity recovery across all governance families.</summary>
public sealed class ProviderRecoveryUiTests : AgentsTestContext
{
    private const string MessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
    private const string CorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";

    public static TheoryData<string, string> Failures()
    {
        TheoryData<string, string> result = [];
        foreach (string family in new[] { "catalog", "enablement", "decision" })
        {
            foreach (string failure in new[] { "first-save", "transport", "receipt-save", "mismatch" })
            {
                result.Add(family, failure);
            }
        }
        return result;
    }

    [Theory]
    [MemberData(nameof(Failures))]
    public Task EveryGovernanceFamilyPersistsBeforeEffectsAndRefreshRetainsAmbiguousIdentity(string family, string failure)
        => family == "decision" ? VerifyAsync<TenantProviderCatalog>(family, failure) : VerifyAsync<ProviderCatalog>(family, failure);

    private async Task VerifyAsync<TPage>(string family, string failure) where TPage : IComponent
    {
        UseCatalogIdentities(MessageId, CorrelationId);
        var durable = new MemoryPendingProviderCommandStore();
        IPendingProviderCommandStore store = Substitute.For<IPendingProviderCommandStore>();
        store.LoadAsync(Arg.Any<CancellationToken>()).Returns(call => durable.LoadAsync(call.Arg<CancellationToken>()));
        int saves = 0;
        store.SaveAsync(Arg.Any<PendingProviderCommand>(), Arg.Any<CancellationToken>()).Returns(call =>
        {
            saves++;
            return saves == (failure == "first-save" ? 1 : failure == "receipt-save" ? 2 : -1)
                ? Task.FromException(new InvalidOperationException("storage denied"))
                : durable.SaveAsync(call.Arg<PendingProviderCommand>());
        });
        store.RemoveAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => durable.RemoveAsync(call.ArgAt<string>(0), call.ArgAt<string>(1), call.ArgAt<string>(2)));
        Services.AddSingleton(store);
        int submissions = 0;
        Task<ProviderCatalogWriteResult> Submit(ProviderCatalogCommandAcceptance identity)
        {
            submissions++;
            PendingProviderCommand saved = durable.LoadAsync().GetAwaiter().GetResult().ShouldHaveSingleItem();
            saved.IsPrepared.ShouldBeTrue();
            saved.Acceptance.ShouldBe(identity);
            identity.MessageId.ShouldBe(MessageId);
            identity.CorrelationId.ShouldBe(CorrelationId);
            identity.TruthState.ShouldBe(AgentSetupTruthState.Unknown);
            return failure == "transport"
                ? Task.FromException<ProviderCatalogWriteResult>(new HttpRequestException("response lost"))
                : Task.FromResult(ProviderCatalogWriteResult.Submitted(identity with
                {
                    MessageId = failure == "mismatch" ? "01ARZ3NDEKTSV4RRFFQ69G5FAX" : identity.MessageId,
                    TruthState = AgentSetupTruthState.Submitted,
                }));
        }
        CatalogGateway.DisableAsync(Arg.Any<DisableProviderModelEntry>(), Arg.Any<ProviderCatalogCommandAcceptance>(), Arg.Any<CancellationToken>())
            .Returns(call => Submit(call.Arg<ProviderCatalogCommandAcceptance>()));
        CatalogGateway.SetTenantEnablementAsync(Arg.Any<SetTenantProviderModelEnablement>(), Arg.Any<ProviderCatalogCommandAcceptance>(), Arg.Any<CancellationToken>())
            .Returns(call => Submit(call.Arg<ProviderCatalogCommandAcceptance>()));
        CatalogGateway.DecideDataHandlingAsync(Arg.Any<DecideProviderDataHandling>(), Arg.Any<ProviderCatalogCommandAcceptance>(), Arg.Any<CancellationToken>())
            .Returns(call => Submit(call.Arg<ProviderCatalogCommandAcceptance>()));
        ConfigureReads();
        CatalogGateway.GetCommandOutcomeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(AgentSetupWriteStatus.UnableToVerify));
        IRenderedComponent<TPage> page = RenderPage<TPage>();
        string submit = SelectSubmission(page, family);
        await page.Find(submit).ClickAsync(new MouseEventArgs());
        submissions.ShouldBe(failure == "first-save" ? 0 : 1);
        AssertUnknown(page);
        page.Find(submit).HasAttribute("disabled").ShouldBeTrue();
        page.Dispose();
        if (failure == "first-save")
        {
            (await durable.LoadAsync()).ShouldBeEmpty();
            return;
        }
        PendingProviderCommand retained = (await durable.LoadAsync()).ShouldHaveSingleItem();
        retained.Acceptance.MessageId.ShouldBe(MessageId);
        retained.Acceptance.CorrelationId.ShouldBe(CorrelationId);
        retained.IsPrepared.ShouldBeTrue();
        IRenderedComponent<TPage> refreshed = RenderPage<TPage>();
        string refreshedSubmit = SelectSubmission(refreshed, family);
        refreshed.Find(refreshedSubmit).HasAttribute("disabled").ShouldBeTrue();
        AssertUnknown(refreshed);
        await refreshed.Find(CheckSelector(family)).ClickAsync(new MouseEventArgs());
        AssertUnknown(refreshed);
        SelectSubmission(refreshed, family); // Reselection must preserve prepared truth.
        AssertUnknown(refreshed);
        CatalogGateway.GetCommandOutcomeAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromException<AgentSetupWriteStatus>(new HttpRequestException("status offline")));
        await refreshed.Find(CheckSelector(family)).ClickAsync(new MouseEventArgs());
        AssertUnknown(refreshed);
        refreshed.Find(refreshedSubmit).HasAttribute("disabled").ShouldBeTrue();
        submissions.ShouldBe(1);
        (await durable.LoadAsync()).ShouldHaveSingleItem().ShouldBe(retained);
    }

    [Theory]
    [InlineData("catalog", false)]
    [InlineData("catalog", true)]
    [InlineData("enablement", false)]
    [InlineData("enablement", true)]
    [InlineData("decision", false)]
    [InlineData("decision", true)]
    public Task ExactManualRecoveryReplacesThePriorAmbiguousFailure(string family, bool projection)
        => family == "decision" ? ConfirmAsync<TenantProviderCatalog>(family, projection) : ConfirmAsync<ProviderCatalog>(family, projection);

    private async Task ConfirmAsync<TPage>(string family, bool projection) where TPage : IComponent
    {
        UseCatalogIdentities(MessageId, CorrelationId);
        ConfigureReads();
        ConfigureSubmission(_ => Task.FromException<ProviderCatalogWriteResult>(new HttpRequestException("response lost")));
        IRenderedComponent<TPage> page = RenderPage<TPage>();
        await page.Find(SelectSubmission(page, family)).ClickAsync(new MouseEventArgs());
        page.Markup.ShouldContain("Write.Status.UnableToVerify");
        AssertUnknown(page);
        ConfigureReads(projected: projection);
        CatalogGateway.GetCommandOutcomeAsync(Arg.Any<string>(), MessageId, CorrelationId, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(projection ? AgentSetupWriteStatus.AwaitingProjection : AgentSetupWriteStatus.AlreadyApplied));
        await page.Find(CheckSelector(family)).ClickAsync(new MouseEventArgs());
        page.Markup.ShouldContain("Truth.Stage.ProjectionConfirmed");
        page.Markup.ShouldNotContain("Write.Status.UnableToVerify");
        (await PendingCommandStore.LoadAsync()).ShouldBeEmpty();
    }

    [Theory]
    [InlineData("catalog")]
    [InlineData("enablement")]
    [InlineData("decision")]
    public Task AccountChangeDuringGatewayAwaitNeverRedirectsTheAcknowledgement(string family)
        => family == "decision" ? SwitchAccountAsync<TenantProviderCatalog>(family) : SwitchAccountAsync<ProviderCatalog>(family);

    private async Task SwitchAccountAsync<TPage>(string family) where TPage : IComponent
    {
        UseCatalogIdentities(MessageId, CorrelationId);
        var javascript = new InMemorySessionStorageJsRuntime();
        var authentication = new ControlledSetupAuthenticationStateProvider();
        using var store = new BrowserSessionPendingProviderCommandStore(javascript, authentication);
        Services.AddSingleton<IPendingProviderCommandStore>(store);
        ConfigureReads();
        var submitted = new TaskCompletionSource<ProviderCatalogCommandAcceptance>(TaskCreationOptions.RunContinuationsAsynchronously);
        var resume = new TaskCompletionSource<ProviderCatalogWriteResult>(TaskCreationOptions.RunContinuationsAsynchronously);
        ConfigureSubmission(identity => { submitted.TrySetResult(identity); return resume.Task; });
        IRenderedComponent<TPage> page = RenderPage<TPage>();
        Task submission = page.Find(SelectSubmission(page, family)).ClickAsync(new MouseEventArgs());
        ProviderCatalogCommandAcceptance identity = await submitted.Task;
        KeyValuePair<string, string> original = javascript.Values.ShouldHaveSingleItem();
        authentication.Publish(Task.FromResult(ControlledSetupAuthenticationStateProvider.State("bob")));
        resume.SetResult(ProviderCatalogWriteResult.Submitted(identity with { TruthState = AgentSetupTruthState.Submitted }));
        await submission;
        javascript.Values.ShouldHaveSingleItem().ShouldBe(original);
        (await store.LoadAsync()).ShouldBeEmpty();
        AssertUnknown(page);
        authentication.Publish(Task.FromResult(ControlledSetupAuthenticationStateProvider.State()));
        using var refreshed = new BrowserSessionPendingProviderCommandStore(javascript, authentication);
        PendingProviderCommand retained = (await refreshed.LoadAsync()).ShouldHaveSingleItem();
        retained.IsPrepared.ShouldBeTrue();
        retained.Acceptance.ShouldBe(identity);
    }

    private void ConfigureSubmission(Func<ProviderCatalogCommandAcceptance, Task<ProviderCatalogWriteResult>> submit)
    {
        CatalogGateway.DisableAsync(Arg.Any<DisableProviderModelEntry>(), Arg.Any<ProviderCatalogCommandAcceptance>(), Arg.Any<CancellationToken>())
            .Returns(call => submit(call.Arg<ProviderCatalogCommandAcceptance>()));
        CatalogGateway.SetTenantEnablementAsync(Arg.Any<SetTenantProviderModelEnablement>(), Arg.Any<ProviderCatalogCommandAcceptance>(), Arg.Any<CancellationToken>())
            .Returns(call => submit(call.Arg<ProviderCatalogCommandAcceptance>()));
        CatalogGateway.DecideDataHandlingAsync(Arg.Any<DecideProviderDataHandling>(), Arg.Any<ProviderCatalogCommandAcceptance>(), Arg.Any<CancellationToken>())
            .Returns(call => submit(call.Arg<ProviderCatalogCommandAcceptance>()));
    }

    private void ConfigureReads(bool projected = false)
    {
        var terms = new ProviderDataHandlingRecord(14, false, ["EU"], "terms-v1", 1);
        ProviderCatalogInspectionResult platform = ProviderCatalogInspectionResult.Success([AgentUiTestData.Entry() with { DataHandling = terms }]) with
        {
            ProjectedCommandMessageIds = projected ? [MessageId] : [],
        };
        CatalogGateway.ListEntriesAsync(Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(platform));
        CatalogGateway.GetEntryAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int?>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(platform));
        CatalogGateway.GetTenantEnablementAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantProviderEnablementInspectionResult(ProviderCatalogInspectionStatus.Success,
                true, 1, ProjectedCommandMessageIds: projected ? [MessageId] : [])));
        CatalogGateway.ListTenantEntriesAsync(Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(new TenantProviderCatalogInspectionResult(ProviderCatalogInspectionStatus.Success,
                [new("openai", "gpt-x", "Model", true, true, true, 128_000, 16_000,
                    new ProviderModelTimeoutPolicy(30_000, 3), ProviderModelCapabilityFlags.Streaming, 2,
                    new ProviderModelPricing("USD", 1m, 1m, 1), terms, false, "AcceptanceRequired", null, 1, 3)],
                ProjectedCommandMessageIds: projected ? [MessageId] : [])));
    }

    private static void AssertUnknown<TPage>(IRenderedComponent<TPage> page) where TPage : IComponent
    {
        page.Markup.ShouldNotContain("Truth.Stage.Submitted");
        page.Markup.ShouldNotContain("Truth.Stage.AuthoritativePending");
        page.Markup.ShouldNotContain("Truth.Stage.ProjectionConfirmed");
    }

    private static string CheckSelector(string family) => family switch
    {
        "catalog" => "[data-testid='agents-provider-catalog-check-pending']",
        "enablement" => "[data-testid='agents-provider-catalog-enablement-check-pending']",
        _ => "[data-testid='agents-tenant-provider-check-pending']",
    };

    private static string SelectSubmission<TPage>(IRenderedComponent<TPage> page, string family) where TPage : IComponent
    {
        string start = family switch
        {
            "catalog" => "[data-testid='agents-provider-catalog-disable']",
            "enablement" => "[data-testid='agents-provider-catalog-set-tenant-enablement']",
            _ => "[data-testid='agents-tenant-provider-review']",
        };
        page.WaitForAssertion(() => page.Find(start));
        if (family == "catalog")
        {
            return start;
        }
        page.Find(start).Click();
        if (family == "enablement")
        {
            page.Find("[data-testid='agents-provider-catalog-enablement-tenant']").Change("tenant-a");
            return "[data-testid='agents-provider-catalog-enablement-save']";
        }
        page.Find("[data-testid='agents-tenant-provider-justification']").Change("Reviewed terms");
        return "[data-testid='agents-tenant-provider-accept']";
    }
}
