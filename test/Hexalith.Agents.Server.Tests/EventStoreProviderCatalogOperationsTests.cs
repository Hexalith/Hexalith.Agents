namespace Hexalith.Agents.Server.Tests;

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

/// <summary>
/// Live public catalog surface (Story 5.3): accepted writes return a structured identity, reads serve the
/// projected catalog, and unauthorized callers learn nothing.
/// </summary>
public sealed class EventStoreProviderCatalogOperationsTests
{
    private const string TenantId = "acme";
    private const string StoreName = "statestore";

    private readonly FakeReadModelStore _store = new();
    private readonly IEventStoreGatewayClient _gateway = Substitute.For<IEventStoreGatewayClient>();
    private readonly IAgentAdministrationContextProvider _contextProvider = Substitute.For<IAgentAdministrationContextProvider>();
    private readonly List<SubmitCommandRequest> _submitted = [];

    public EventStoreProviderCatalogOperationsTests()
    {
        _contextProvider.GetContext().Returns(new AgentAdministrationContext(TenantId, "admin-user", IsAgentsAdmin: true, IsPlatformOperator: true));
        _ = _gateway
            .SubmitCommandAsync(Arg.Any<SubmitCommandRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                _submitted.Add(call.Arg<SubmitCommandRequest>());
                return new SubmitCommandResponse("corr-1", null, "msg-1");
            });
    }

    [Fact]
    public async Task Platform_enablement_read_reports_disabled_state_and_denies_tenant_admin_before_lookup()
    {
        var tenant = new TenantProviderEnablementReadModel { ProjectionVersion = "2" };
        tenant.State.Apply(new TenantProviderModelEnablementSet(TenantId, "openai", "gpt-4o", false, 2,
            "operator", null));
        _store.Seed(StoreName, TenantProviderEnablementReadModelAddresses.Detail(TenantId), tenant);

        TenantProviderEnablementInspectionResult visible = (await Operations()
            .GetTenantEnablementAsync(TenantId, "openai", "gpt-4o")).Value.ShouldNotBeNull();
        visible.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
        visible.Enabled.ShouldBe(false);
        visible.Revision.ShouldBe(2);

        _contextProvider.GetContext().Returns(new AgentAdministrationContext(TenantId, "tenant-admin", IsAgentsAdmin: true));
        int priorGets = _store.GetCount;
        TenantProviderEnablementInspectionResult denied = (await Operations()
            .GetTenantEnablementAsync(TenantId, "openai", "gpt-4o")).Value.ShouldNotBeNull();
        denied.Status.ShouldBe(ProviderCatalogInspectionStatus.NotAuthorized);
        denied.Enabled.ShouldBeNull();
        _store.GetCount.ShouldBe(priorGets);
    }

    [Fact]
    public async Task Tenant_disable_dispatches_even_when_platform_terms_are_absent()
    {
        SeedProjectedEntry(1);
        ProviderCatalogReadModel platform = _store.Snapshot<ProviderCatalogReadModel>(StoreName,
            ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId)).ShouldNotBeNull();
        platform.Entries[0] = platform.Entries[0] with { DataHandling = null };
        _store.Seed(StoreName, ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId), platform);
        var command = new SetTenantProviderModelEnablement(TenantId, "openai", "gpt-4o", false, 0, null);

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().SetTenantEnablementAsync(command);

        result.IsSuccess.ShouldBeTrue();
        SubmitCommandRequest sent = _submitted.ShouldHaveSingleItem();
        sent.Tenant.ShouldBe(TenantId);
        sent.CommandType.ShouldBe(nameof(SetTenantProviderModelEnablement));
        JsonSerializer.Deserialize<SetTenantProviderModelEnablement>(sent.Payload.GetRawText()).ShouldNotBeNull()
            .CurrentTerms.ShouldBeNull();
    }

    [Fact]
    public async Task Authorized_decision_dispatches_exact_current_terms_with_tenant_actor_and_trusted_decision_time()
    {
        SeedTenantDecisionView();
        DateTimeOffset before = DateTimeOffset.UtcNow;
        ProviderDataHandlingRecord terms = SelectableEntry(1).DataHandling.ShouldNotBeNull();
        var decision = new DecideProviderDataHandling("openai", "gpt-4o", 1, true, "approved", 1, terms, default);

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().DecideDataHandlingAsync(decision);

        DateTimeOffset after = DateTimeOffset.UtcNow;
        result.IsSuccess.ShouldBeTrue();
        SubmitCommandRequest sent = _submitted.ShouldHaveSingleItem();
        sent.Tenant.ShouldBe(TenantId);
        sent.AggregateId.ShouldBe(TenantId);
        sent.CommandType.ShouldBe(nameof(DecideProviderDataHandling));
        sent.Extensions.ShouldNotBeNull()["actor:tenantAgentAdministrator"].ShouldBe("true");
        DecideProviderDataHandling payload = JsonSerializer.Deserialize<DecideProviderDataHandling>(sent.Payload.GetRawText()).ShouldNotBeNull();
        ProviderDataHandlingPolicy.SameSnapshot(payload.ConfirmedTerms, terms).ShouldBeTrue();
        payload.DecidedAt.ShouldBeGreaterThanOrEqualTo(before);
        payload.DecidedAt.ShouldBeLessThanOrEqualTo(after);
    }

    [Theory]
    [InlineData("effective-at")]
    [InlineData("declaration")]
    [InlineData("null-regions")]
    public async Task Decision_rejects_an_altered_or_malformed_submitted_terms_snapshot(string change)
    {
        SeedTenantDecisionView();
        ProviderDataHandlingRecord terms = SelectableEntry(1).DataHandling.ShouldNotBeNull();
        ProviderDataHandlingRecord submitted = change switch
        {
            "effective-at" => terms with { EffectiveAt = DateTimeOffset.UtcNow },
            "declaration" => terms with { TighteningDeclaration = new ProviderDataHandlingTighteningDeclaration(
                0, 1, new ProviderDataHandlingFieldDiff(40, 30, false, false, [], [], "old", "terms-v1"),
                "operator", "PlatformOperator", DateTimeOffset.UtcNow) },
            _ => terms with { ProcessingRegions = null! },
        };
        var decision = new DecideProviderDataHandling("openai", "gpt-4o", 1, true, "approved", 1, submitted, default);

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().DecideDataHandlingAsync(decision);

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.Stale);
        await _gateway.DidNotReceiveWithAnyArgs().SubmitCommandAsync(default!, default);
    }

    [Fact]
    public async Task Retained_key_sends_original_terms_for_authoritative_reconciliation_after_platform_terms_advance()
    {
        SeedTenantDecisionView();
        ProviderDataHandlingRecord original = SelectableEntry(1).DataHandling.ShouldNotBeNull();
        ProviderCatalogReadModel platform = _store.Snapshot<ProviderCatalogReadModel>(StoreName,
            ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId)).ShouldNotBeNull();
        platform.Entries[0] = platform.Entries[0] with
        {
            DataHandling = original with { DataHandlingVersion = 2, TermsReferenceId = "terms-v2" },
        };
        _store.Seed(StoreName, ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId), platform);
        var decision = new DecideProviderDataHandling("openai", "gpt-4o", 1, true, "approved", 1, original, default);

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().DecideDataHandlingAsync(
            decision, new AgentOperationOptions("corr-retry", "msg-original"));

        result.IsSuccess.ShouldBeTrue();
        SubmitCommandRequest sent = _submitted.ShouldHaveSingleItem();
        sent.MessageId.ShouldBe("msg-original");
        sent.IdempotencyKey.ShouldBe("msg-original");
        JsonSerializer.Deserialize<DecideProviderDataHandling>(sent.Payload.GetRawText()).ShouldNotBeNull()
            .ConfirmedTerms.DataHandlingVersion.ShouldBe(1);
    }

    [Fact]
    public async Task An_accepted_create_answers_with_a_structured_identity_and_the_submitted_stage()
    {
        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().CreateEntryAsync(CreateCommand());

        result.IsSuccess.ShouldBeTrue();
        ProviderCatalogCommandAcceptance acceptance = result.Value.ShouldNotBeNull();
        acceptance.ProviderId.ShouldBe("openai");
        acceptance.ModelId.ShouldBe("gpt-4o");
        acceptance.MessageId.ShouldNotBeNullOrWhiteSpace();
        acceptance.CorrelationId.ShouldNotBeNullOrWhiteSpace();
        acceptance.TruthState.ShouldBe(AgentSetupTruthState.Submitted);
    }

    [Fact]
    public async Task An_acceptance_carries_no_eventstore_or_secret_internals()
    {
        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().CreateEntryAsync(CreateCommand());

        string serialized = JsonSerializer.Serialize(result.Value);
        serialized.ShouldNotContain("revision", Case.Insensitive);
        serialized.ShouldNotContain("stream", Case.Insensitive);
        serialized.ShouldNotContain("aggregate", Case.Insensitive);
        serialized.ShouldNotContain("cfg-openai-gpt4o");
        serialized.ShouldNotContain("sk-");
    }

    [Fact]
    public async Task A_write_then_projection_then_read_reaches_projection_confirmed()
    {
        _ = await Operations().CreateEntryAsync(CreateCommand());

        ProviderCatalogInspectionResult before = (await Operations().GetEntryAsync("openai", "gpt-4o", expectedCapabilityVersion: 1))
            .Value
            .ShouldNotBeNull();
        before.Status.ShouldBe(ProviderCatalogInspectionStatus.EntryNotFound);
        before.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        before.Freshness.ShouldBe(AgentSetupFreshness.Stale);
        before.Entries.ShouldBeEmpty();

        SeedProjectedEntry(capabilityVersion: 1);

        ProviderCatalogInspectionResult after = (await Operations().GetEntryAsync("openai", "gpt-4o", expectedCapabilityVersion: 1))
            .Value
            .ShouldNotBeNull();
        after.TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
        after.Entries.ShouldHaveSingleItem().Pricing.ShouldNotBeNull().Currency.ShouldBe("USD");
        after.Entries[0].IsSelectableForNewActiveUse.ShouldBeTrue();
    }

    [Fact]
    public async Task A_read_that_outruns_the_projection_is_authoritative_pending()
    {
        SeedProjectedEntry(capabilityVersion: 1);

        ProviderCatalogInspectionResult result = (await Operations().GetEntryAsync("openai", "gpt-4o", expectedCapabilityVersion: 2))
            .Value
            .ShouldNotBeNull();

        result.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        result.Freshness.ShouldBe(AgentSetupFreshness.Stale);
        result.Entries.ShouldHaveSingleItem().CapabilityVersion.ShouldBe(1);
    }

    [Fact]
    public async Task A_list_waiting_on_a_newer_projection_version_is_authoritative_pending()
    {
        SeedProjectedEntry(capabilityVersion: 1);

        ProviderCatalogInspectionResult result = (await Operations().ListEntriesAsync(includeDisabled: true, expectedProjectionVersion: "9"))
            .Value
            .ShouldNotBeNull();

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
        result.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        result.Freshness.ShouldBe(AgentSetupFreshness.Stale);
        result.Entries.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task An_unparseable_expected_projection_version_is_treated_as_behind()
    {
        SeedProjectedEntry(capabilityVersion: 1);

        ProviderCatalogInspectionResult result = (await Operations().ListEntriesAsync(includeDisabled: true, expectedProjectionVersion: "not-a-number"))
            .Value
            .ShouldNotBeNull();

        result.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        result.Freshness.ShouldBe(AgentSetupFreshness.Stale);
    }

    [Fact]
    public async Task An_unauthorized_write_does_not_dispatch()
    {
        _contextProvider.GetContext().Returns(AgentAdministrationContext.Anonymous);

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await Operations().CreateEntryAsync(CreateCommand());

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.NotAuthorized);
        await _gateway.DidNotReceiveWithAnyArgs().SubmitCommandAsync(default!, default);
    }

    [Fact]
    public async Task Select_provider_model_stays_out_of_scope_on_the_administration_surface()
    {
        IAgentAdministrationOperations administration = new EventStoreAgentAdministrationOperations(
            _contextProvider,
            new AgentAdministrationOrchestrator(new EventStoreAgentCommandDispatcher(_gateway)),
            new AgentResponseModeOrchestrator(new EventStoreAgentCommandDispatcher(_gateway)),
            new AgentActivationProviderRevalidation(
                Substitute.For<IProviderCatalogReader>(),
                Substitute.For<IApproverPolicyResolver>(),
                new EventStoreAgentCommandDispatcher(_gateway)),
            _store,
            Options.Create(new AgentSetupReadModelOptions { StateStoreName = StoreName }),
            new AgentCommandIdentityFactory(),
            new DeferredAgentCommandStatusReader());

        AgentOperationResult result = await administration.SelectProviderModelAsync(
            new Hexalith.Agents.Contracts.Agent.Commands.SelectAgentProviderModel("openai", "gpt-4o", 1));

        result.IsSuccess.ShouldBeFalse();
        result.Error.ShouldNotBeNull().Code.ShouldBe(AgentOperationErrorCode.Unavailable);
    }

    private IProviderCatalogOperations Operations()
    {
        var dispatcher = new EventStoreAgentCommandDispatcher(_gateway);
        return new EventStoreProviderCatalogOperations(
            _contextProvider,
            new ProviderCatalogAdministrationOrchestrator(dispatcher),
            _store,
            Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }),
            new AgentCommandIdentityFactory());
    }

    private void SeedProjectedEntry(int capabilityVersion)
        => _store.Seed(
            StoreName,
            ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId),
            new ProviderCatalogReadModel
            {
                CatalogId = ProviderCatalogIdentity.PlatformTenantId,
                TenantId = ProviderCatalogIdentity.PlatformTenantId,
                Entries = [SelectableEntry(capabilityVersion)],
                LastSequenceNumber = capabilityVersion,
                ProjectedAt = new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero),
                ProjectionVersion = capabilityVersion.ToString(System.Globalization.CultureInfo.InvariantCulture),
            });

    private void SeedTenantDecisionView()
    {
        SeedProjectedEntry(1);
        var tenant = new TenantProviderEnablementReadModel();
        tenant.State.Apply(new TenantProviderModelEnablementSet(TenantId, "openai", "gpt-4o", true, 1,
            "operator", null));
        _store.Seed(StoreName, TenantProviderEnablementReadModelAddresses.Detail(TenantId), tenant);
    }

    private static CreateProviderModelEntry CreateCommand()
        => new(
            "openai",
            "gpt-4o",
            "OpenAI GPT-4o",
            Enabled: true,
            SupportsTextGeneration: true,
            128_000,
            16_000,
            new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.Streaming,
            "cfg-openai-gpt4o",
            new ProviderModelPricing("USD", 0.002m, 0.008m, 0),
            new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1));

    private static ProviderCatalogEntryView SelectableEntry(int capabilityVersion)
        => new(
            "openai",
            "gpt-4o",
            "OpenAI GPT-4o",
            ProviderModelStatus.Enabled,
            SupportsTextGeneration: true,
            128_000,
            16_000,
            new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.Streaming,
            ProviderConfigurationState.Configured,
            "cfg-openai-gpt4o",
            IsSelectableForNewActiveUse: true,
            capabilityVersion,
            new ProviderModelPricing("USD", 0.002m, 0.008m, 1),
            new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1));
}
