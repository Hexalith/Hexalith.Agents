namespace Hexalith.Agents.Server.Tests;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.Contracts.ProviderCatalog.Queries;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.Server.Application.Queries;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;
using Hexalith.Agents.TenantProviderEnablement;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

/// <summary>
/// Live provider-catalog query handlers (Story 5.3 AC2, AC4).
/// </summary>
public sealed class ProviderCatalogQueryTests
{
    private const string TenantId = "acme";
    private const string StoreName = "statestore";
    private const string UserId = "admin-user";

    private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private readonly FakeReadModelStore _store = new();
    private readonly ITenantAccessReader _tenantAccess = Substitute.For<ITenantAccessReader>();
    private readonly IAgentAdministrationContextProvider _contextProvider = Substitute.For<IAgentAdministrationContextProvider>();
    private readonly IEventStoreGatewayClient _gateway = Substitute.For<IEventStoreGatewayClient>();

    public ProviderCatalogQueryTests()
    {
        _contextProvider.GetContext().Returns(new AgentAdministrationContext(
            TenantId, UserId, IsAgentsAdmin: false, IsPlatformOperator: true));
        _tenantAccess
            .ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, IsFresh: true));
        _gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                StreamReadRequest request = call.Arg<StreamReadRequest>();
                long latest = request.Domain == TenantProviderEnablementAggregate.Domain
                    ? _store.Snapshot<TenantProviderEnablementReadModel>(StoreName,
                        TenantProviderEnablementReadModelAddresses.Detail(request.Tenant))?.LastSequenceNumber ?? 0
                    : _store.Snapshot<ProviderCatalogReadModel>(StoreName,
                        ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId))?
                        .StreamSequences.GetValueOrDefault(request.AggregateId ?? string.Empty) ?? 0;
                return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                    new StreamReadMetadata(request.FromSequence, null, null, latest, 0, false, null));
            });
    }

    [Fact]
    public async Task List_and_get_answer_with_the_same_safe_projection()
    {
        Seed();

        ProviderCatalogInspectionResult list = await ExecuteAsync(
            ListHandler(),
            Query(ListProviderCatalogEntriesQuery.QueryType, new ListProviderCatalogEntriesQuery(IncludeDisabled: true)));
        ProviderCatalogInspectionResult get = await ExecuteAsync(
            GetHandler(),
            Query(GetProviderCatalogEntryQuery.QueryType, new GetProviderCatalogEntryQuery("openai", "gpt-4o")));

        list.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
        JsonSerializer.Serialize(get.Entries.ShouldHaveSingleItem())
            .ShouldBe(JsonSerializer.Serialize(list.Entries.ShouldHaveSingleItem()));
        get.Entries[0].Pricing.ShouldNotBeNull();
        get.ProjectionVersion.ShouldBe("1");
        get.TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
    }

    [Fact]
    public async Task Authorized_tenant_list_and_get_expose_only_enabled_entries_without_platform_configuration()
    {
        Seed();
        string platformKey = ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId);
        ProviderCatalogReadModel platform = _store.Snapshot<ProviderCatalogReadModel>(StoreName, platformKey).ShouldNotBeNull();
        platform.Entries.Add(platform.Entries[0] with
        {
            ModelId = "gpt-hidden",
            ConfigurationReferenceId = "cfg-hidden",
        });
        _store.Seed(StoreName, platformKey, platform);
        var tenant = new TenantProviderEnablementReadModel();
        tenant.State.Apply(new TenantProviderModelEnablementSet(TenantId, "openai", "gpt-4o", true, 1, UserId, null));
        tenant.State.Apply(new TenantProviderModelEnablementSet(TenantId, "openai", "gpt-hidden", false, 2, UserId, null));
        tenant.LastSequenceNumber = 2;
        _store.Seed(StoreName, TenantProviderEnablementReadModelAddresses.Detail(TenantId), tenant);

        QueryResult listWire = await ListHandler().ExecuteAsync(Query(ListProviderCatalogEntriesQuery.QueryType,
            new ListProviderCatalogEntriesQuery(false), tenantId: TenantId, global: false), CancellationToken.None);
        QueryResult getWire = await GetHandler().ExecuteAsync(Query(GetProviderCatalogEntryQuery.QueryType,
            new GetProviderCatalogEntryQuery("openai", "gpt-4o"), tenantId: TenantId, global: false), CancellationToken.None);
        QueryResult hiddenWire = await GetHandler().ExecuteAsync(Query(GetProviderCatalogEntryQuery.QueryType,
            new GetProviderCatalogEntryQuery("openai", "gpt-hidden"), tenantId: TenantId, global: false), CancellationToken.None);

        TenantProviderCatalogInspectionResult list = JsonSerializer.Deserialize<TenantProviderCatalogInspectionResult>(
            listWire.PayloadBytes.ShouldNotBeNull(), _json).ShouldNotBeNull();
        TenantProviderCatalogInspectionResult get = JsonSerializer.Deserialize<TenantProviderCatalogInspectionResult>(
            getWire.PayloadBytes.ShouldNotBeNull(), _json).ShouldNotBeNull();
        TenantProviderCatalogInspectionResult hidden = JsonSerializer.Deserialize<TenantProviderCatalogInspectionResult>(
            hiddenWire.PayloadBytes.ShouldNotBeNull(), _json).ShouldNotBeNull();
        list.Entries.ShouldHaveSingleItem().ModelId.ShouldBe("gpt-4o");
        get.Entries.ShouldHaveSingleItem().ModelId.ShouldBe("gpt-4o");
        hidden.Status.ShouldBe(ProviderCatalogInspectionStatus.EntryNotFound);
        foreach (QueryResult wire in new[] { listWire, getWire, hiddenWire })
        {
            string payload = System.Text.Encoding.UTF8.GetString(wire.PayloadBytes.ShouldNotBeNull());
            payload.ShouldNotContain("ConfigurationReferenceId", Case.Insensitive);
            payload.ShouldNotContain("ConfigurationState", Case.Insensitive);
            payload.ShouldNotContain("cfg-openai-gpt4o");
            payload.ShouldNotContain("cfg-hidden");
        }
    }

    [Fact]
    public async Task Platform_query_list_and_detail_remain_pending_when_a_committed_entry_is_ahead()
    {
        Seed();
        _gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                StreamReadRequest request = call.Arg<StreamReadRequest>();
                return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                    new StreamReadMetadata(request.FromSequence, null, null, 2, 0, false, null));
            });

        ProviderCatalogInspectionResult list = await ExecuteAsync(ListHandler(),
            Query(ListProviderCatalogEntriesQuery.QueryType, new ListProviderCatalogEntriesQuery(true)));
        ProviderCatalogInspectionResult detail = await ExecuteAsync(GetHandler(),
            Query(GetProviderCatalogEntryQuery.QueryType, new GetProviderCatalogEntryQuery("openai", "gpt-4o")));

        foreach (ProviderCatalogInspectionResult result in new[] { list, detail })
        {
            result.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
            result.Entries.ShouldBeEmpty();
            result.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
            result.Freshness.ShouldBe(AgentSetupFreshness.Stale);
        }
    }

    [Fact]
    public async Task Operator_list_does_not_claim_completeness_for_an_uninventoried_committed_create()
    {
        Seed();
        _gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                StreamReadRequest request = call.Arg<StreamReadRequest>();
                long latest = 1;
                return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                    new StreamReadMetadata(request.FromSequence, null, null, latest, 0, false, null));
            });

        ProviderCatalogInspectionResult list = await ExecuteAsync(ListHandler(),
            Query(ListProviderCatalogEntriesQuery.QueryType, new ListProviderCatalogEntriesQuery(true)));
        ProviderCatalogInspectionResult newDetail = await ExecuteAsync(GetHandler(),
            Query(GetProviderCatalogEntryQuery.QueryType,
                new GetProviderCatalogEntryQuery("openai", "new-model")));

        list.Entries.ShouldHaveSingleItem().ModelId.ShouldBe("gpt-4o");
        list.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        newDetail.Entries.ShouldBeEmpty();
        newDetail.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
    }

    [Fact]
    public async Task Tenant_query_list_and_detail_remain_pending_when_disable_is_awaiting_projection()
    {
        Seed();
        var tenant = new TenantProviderEnablementReadModel();
        tenant.State.Apply(new TenantProviderModelEnablementSet(TenantId, "openai", "gpt-4o", true, 1, UserId, null));
        tenant.LastSequenceNumber = 1;
        _store.Seed(StoreName, TenantProviderEnablementReadModelAddresses.Detail(TenantId), tenant);
        _gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                StreamReadRequest request = call.Arg<StreamReadRequest>();
                long latest = request.Domain == TenantProviderEnablementAggregate.Domain ? 2 : 1;
                return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                    new StreamReadMetadata(request.FromSequence, null, null, latest, 0, false, null));
            });

        QueryResult listWire = await ListHandler().ExecuteAsync(Query(ListProviderCatalogEntriesQuery.QueryType,
            new ListProviderCatalogEntriesQuery(false), tenantId: TenantId, global: false), CancellationToken.None);
        QueryResult detailWire = await GetHandler().ExecuteAsync(Query(GetProviderCatalogEntryQuery.QueryType,
            new GetProviderCatalogEntryQuery("openai", "gpt-4o"), tenantId: TenantId, global: false), CancellationToken.None);
        foreach (QueryResult wire in new[] { listWire, detailWire })
        {
            TenantProviderCatalogInspectionResult result = JsonSerializer.Deserialize<TenantProviderCatalogInspectionResult>(
                wire.PayloadBytes.ShouldNotBeNull(), _json).ShouldNotBeNull();
            result.Entries.ShouldBeEmpty();
            result.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
            result.Freshness.ShouldBe(AgentSetupFreshness.Stale);
        }
    }

    [Fact]
    public async Task Tenant_query_cannot_confirm_old_terms_while_platform_terms_delivery_lags()
    {
        Seed();
        var tenant = new TenantProviderEnablementReadModel { LastSequenceNumber = 1 };
        tenant.State.Apply(new TenantProviderModelEnablementSet(TenantId, "openai", "gpt-4o", true, 1, UserId, null));
        _store.Seed(StoreName, TenantProviderEnablementReadModelAddresses.Detail(TenantId), tenant);
        _gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                StreamReadRequest request = call.Arg<StreamReadRequest>();
                long latest = request.Domain == TenantProviderEnablementAggregate.Domain ? 1 : 2;
                return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                    new StreamReadMetadata(request.FromSequence, null, null, latest, 0, false, null));
            });

        QueryResult resultWire = await GetHandler().ExecuteAsync(Query(GetProviderCatalogEntryQuery.QueryType,
            new GetProviderCatalogEntryQuery("openai", "gpt-4o"), tenantId: TenantId, global: false), CancellationToken.None);
        TenantProviderCatalogInspectionResult result = JsonSerializer.Deserialize<TenantProviderCatalogInspectionResult>(
            resultWire.PayloadBytes.ShouldNotBeNull(), _json).ShouldNotBeNull();

        result.Entries.ShouldBeEmpty();
        result.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        result.Freshness.ShouldBe(AgentSetupFreshness.Stale);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Tenant_query_evaluates_grace_after_authoritative_reads(bool detail)
    {
        Seed();
        DateTimeOffset now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset evaluatedAt = now;
        TimeProvider clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_ => evaluatedAt);
        string platformKey = ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId);
        ProviderCatalogReadModel platform = _store.Snapshot<ProviderCatalogReadModel>(StoreName, platformKey).ShouldNotBeNull();
        ProviderDataHandlingRecord accepted = platform.Entries[0].DataHandling.ShouldNotBeNull() with
        {
            RetentionDays = 30,
            AllowsTrainingUse = true,
            ProcessingRegions = ["EU", "US"],
            EffectiveAt = now.AddDays(-40),
        };
        var changed = new ProviderDataHandlingRecord(14, false, ["EU"], accepted.TermsReferenceId,
            2, now.AddDays(-30).AddSeconds(1));
        changed = changed with { TighteningDeclaration = ProviderDataHandlingPolicy.DeclareTightening(accepted, changed, "operator") };
        platform.Entries[0] = platform.Entries[0] with
        {
            DataHandling = changed,
            DataHandlingHistory = [accepted, changed],
        };
        _store.Seed(StoreName, platformKey, platform);
        var tenant = new TenantProviderEnablementReadModel { LastSequenceNumber = 2 };
        tenant.State.Apply(new TenantProviderModelEnablementSet(TenantId, "openai", "gpt-4o", true, 1, UserId, null));
        tenant.State.Apply(new ProviderDataHandlingDecided(TenantId, "openai", "gpt-4o", true,
            "Reviewed", 2, UserId, "TenantAgentAdministrator", accepted, now.AddDays(-39)));
        _store.Seed(StoreName, TenantProviderEnablementReadModelAddresses.Detail(TenantId), tenant);
        _gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                StreamReadRequest request = call.Arg<StreamReadRequest>();
                evaluatedAt = now.AddSeconds(2);
                return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                    new StreamReadMetadata(0, null, null,
                        request.Domain == TenantProviderEnablementAggregate.Domain ? 2 : 1, 0, false, null));
            });
        ProviderCatalogQueryHandlerBase handler = detail
            ? new GetProviderCatalogEntryQueryHandler(_store,
                Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }),
                _tenantAccess, _contextProvider, _gateway, clock)
            : new ListProviderCatalogEntriesQueryHandler(_store,
                Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }),
                _tenantAccess, _contextProvider, _gateway, clock);
        QueryEnvelope query = detail
            ? Query(GetProviderCatalogEntryQuery.QueryType, new GetProviderCatalogEntryQuery("openai", "gpt-4o"), TenantId, false)
            : Query(ListProviderCatalogEntriesQuery.QueryType, new ListProviderCatalogEntriesQuery(false), TenantId, false);

        QueryResult wire = await handler.ExecuteAsync(query, CancellationToken.None);
        TenantProviderCatalogEntryView view = JsonSerializer.Deserialize<TenantProviderCatalogInspectionResult>(
            wire.PayloadBytes.ShouldNotBeNull(), _json).ShouldNotBeNull().Entries.ShouldHaveSingleItem();
        view.DataHandlingStatus.ShouldBe("GraceExpired");
        view.IsSelectableForNewActiveUse.ShouldBeFalse();
    }

    [Fact]
    public async Task Hidden_and_absent_tenant_keys_match_without_reading_their_platform_streams()
    {
        Seed();
        string platformKey = ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId);
        ProviderCatalogReadModel platform = _store.Snapshot<ProviderCatalogReadModel>(StoreName, platformKey).ShouldNotBeNull();
        platform.Entries.Add(platform.Entries[0] with { ModelId = "hidden" });
        _store.Seed(StoreName, platformKey, platform);
        var tenant = new TenantProviderEnablementReadModel { LastSequenceNumber = 1 };
        tenant.State.Apply(new TenantProviderModelEnablementSet(TenantId, "openai", "hidden", false, 1, UserId, null));
        _store.Seed(StoreName, TenantProviderEnablementReadModelAddresses.Detail(TenantId), tenant);

        QueryResult hiddenWire = await GetHandler().ExecuteAsync(Query(GetProviderCatalogEntryQuery.QueryType,
            new GetProviderCatalogEntryQuery("openai", "hidden"), tenantId: TenantId, global: false), CancellationToken.None);
        QueryResult absentWire = await GetHandler().ExecuteAsync(Query(GetProviderCatalogEntryQuery.QueryType,
            new GetProviderCatalogEntryQuery("openai", "absent"), tenantId: TenantId, global: false), CancellationToken.None);
        TenantProviderCatalogInspectionResult hidden = JsonSerializer.Deserialize<TenantProviderCatalogInspectionResult>(
            hiddenWire.PayloadBytes.ShouldNotBeNull(), _json).ShouldNotBeNull();
        TenantProviderCatalogInspectionResult absent = JsonSerializer.Deserialize<TenantProviderCatalogInspectionResult>(
            absentWire.PayloadBytes.ShouldNotBeNull(), _json).ShouldNotBeNull();
        JsonSerializer.Serialize(hidden).ShouldBe(JsonSerializer.Serialize(absent));
        hidden.Status.ShouldBe(ProviderCatalogInspectionStatus.EntryNotFound);
        await _gateway.DidNotReceive().ReadStreamAsync(Arg.Is<StreamReadRequest>(request =>
            request.AggregateId == ProviderCatalogIdentity.EntryId("openai", "hidden")
            || request.AggregateId == ProviderCatalogIdentity.EntryId("openai", "absent")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unrelated_platform_update_does_not_stall_tenant_list()
    {
        Seed();
        string platformKey = ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId);
        ProviderCatalogReadModel platform = _store.Snapshot<ProviderCatalogReadModel>(StoreName, platformKey).ShouldNotBeNull();
        platform.Entries.Add(platform.Entries[0] with { ModelId = "other" });
        _store.Seed(StoreName, platformKey, platform);
        var tenant = new TenantProviderEnablementReadModel { LastSequenceNumber = 1 };
        tenant.State.Apply(new TenantProviderModelEnablementSet(TenantId, "openai", "gpt-4o", true, 1, UserId, null));
        _store.Seed(StoreName, TenantProviderEnablementReadModelAddresses.Detail(TenantId), tenant);
        _gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                StreamReadRequest request = call.Arg<StreamReadRequest>();
                long latest = request.AggregateId == ProviderCatalogIdentity.EntryId("openai", "other") ? 2 : 1;
                return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                    new StreamReadMetadata(request.FromSequence, null, null, latest, 0, false, null));
            });

        QueryResult wire = await ListHandler().ExecuteAsync(Query(ListProviderCatalogEntriesQuery.QueryType,
            new ListProviderCatalogEntriesQuery(false), tenantId: TenantId, global: false), CancellationToken.None);
        TenantProviderCatalogInspectionResult result = JsonSerializer.Deserialize<TenantProviderCatalogInspectionResult>(
            wire.PayloadBytes.ShouldNotBeNull(), _json).ShouldNotBeNull();
        result.Entries.ShouldHaveSingleItem().ModelId.ShouldBe("gpt-4o");
        result.TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
        await _gateway.DidNotReceive().ReadStreamAsync(Arg.Is<StreamReadRequest>(request =>
            request.AggregateId == ProviderCatalogIdentity.EntryId("openai", "other")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_read_waiting_for_a_newer_capability_version_is_authoritative_pending()
    {
        Seed();

        ProviderCatalogInspectionResult result = await ExecuteAsync(
            GetHandler(),
            Query(GetProviderCatalogEntryQuery.QueryType, new GetProviderCatalogEntryQuery("openai", "gpt-4o", ExpectedCapabilityVersion: 2)));

        result.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        result.Freshness.ShouldBe(AgentSetupFreshness.Stale);
    }

    [Fact]
    public async Task List_waiting_for_a_newer_projection_version_is_authoritative_pending()
    {
        Seed();

        ProviderCatalogInspectionResult result = await ExecuteAsync(
            ListHandler(),
            Query(ListProviderCatalogEntriesQuery.QueryType, new ListProviderCatalogEntriesQuery(true, ExpectedProjectionVersion: "9")));

        result.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        result.Freshness.ShouldBe(AgentSetupFreshness.Stale);
        result.Entries.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Get_missing_entry_waiting_for_capability_version_is_authoritative_pending()
    {
        Seed();

        ProviderCatalogInspectionResult result = await ExecuteAsync(
            GetHandler(),
            Query(GetProviderCatalogEntryQuery.QueryType, new GetProviderCatalogEntryQuery("openai", "missing", ExpectedCapabilityVersion: 1)));

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.EntryNotFound);
        result.Entries.ShouldBeEmpty();
        result.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        result.Freshness.ShouldBe(AgentSetupFreshness.Stale);
    }

    [Fact]
    public async Task A_store_failure_returns_structured_unavailable_without_rows()
    {
        IReadModelStore store = Substitute.For<IReadModelStore>();
        store
            .GetAsync<ProviderCatalogReadModel>(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns<Task<ReadModelEntry<ProviderCatalogReadModel>>>(_ => throw new InvalidOperationException("store-down"));
        var handler = new ListProviderCatalogEntriesQueryHandler(
            store,
            Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }),
            _tenantAccess,
            _contextProvider);

        ProviderCatalogInspectionResult result = await ExecuteAsync(
            handler,
            Query(ListProviderCatalogEntriesQuery.QueryType, new ListProviderCatalogEntriesQuery(true), tenantId: TenantId, global: false));

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.Unavailable);
        result.Entries.ShouldBeEmpty();
        JsonSerializer.Serialize(result).ShouldNotContain("store-down");
    }

    [Fact]
    public async Task An_unauthorized_query_returns_not_authorized_with_no_rows()
    {
        Seed();
        _tenantAccess
            .ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(TenantAccessReadResult.Unavailable);

        ProviderCatalogInspectionResult denied = await ExecuteAsync(
            ListHandler(),
            Query(ListProviderCatalogEntriesQuery.QueryType, new ListProviderCatalogEntriesQuery(true), tenantId: TenantId, global: false));

        denied.Status.ShouldBe(ProviderCatalogInspectionStatus.NotAuthorized);
        denied.Entries.ShouldBeEmpty();
        JsonSerializer.Serialize(denied).ShouldNotContain("openai");
    }

    [Fact]
    public async Task Infrastructure_global_administrator_without_platform_role_cannot_read_platform_configuration()
    {
        Seed();
        _contextProvider.GetContext().Returns(new AgentAdministrationContext(
            TenantId, UserId, IsAgentsAdmin: false, IsPlatformOperator: false));

        ProviderCatalogInspectionResult denied = await ExecuteAsync(
            GetHandler(), Query(GetProviderCatalogEntryQuery.QueryType,
                new GetProviderCatalogEntryQuery("openai", "gpt-4o")));

        denied.Status.ShouldBe(ProviderCatalogInspectionStatus.NotAuthorized);
        denied.Entries.ShouldBeEmpty();
        JsonSerializer.Serialize(denied).ShouldNotContain("cfg-openai-gpt4o");
    }

    [Theory]
    [InlineData("not-global")]
    [InlineData("other-user")]
    public async Task Platform_query_requires_the_global_envelope_and_the_same_platform_operator(string scenario)
    {
        Seed();
        if (scenario == "other-user")
        {
            _contextProvider.GetContext().Returns(new AgentAdministrationContext(
                TenantId, "another-operator", IsAgentsAdmin: false, IsPlatformOperator: true));
        }

        QueryResult wire = await GetHandler().ExecuteAsync(Query(GetProviderCatalogEntryQuery.QueryType,
            new GetProviderCatalogEntryQuery("openai", "gpt-4o"), global: scenario != "not-global"),
            CancellationToken.None);

        ProviderCatalogInspectionResult denied = JsonSerializer.Deserialize<ProviderCatalogInspectionResult>(
            wire.PayloadBytes.ShouldNotBeNull(), _json).ShouldNotBeNull();
        denied.Status.ShouldBe(ProviderCatalogInspectionStatus.NotAuthorized);
        denied.Entries.ShouldBeEmpty();
        System.Text.Encoding.UTF8.GetString(wire.PayloadBytes!).ShouldNotContain("cfg-openai-gpt4o");
    }

    [Fact]
    public async Task Platform_detail_carries_the_entry_stream_projected_command_ids()
    {
        Seed();
        string platformKey = ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId);
        ProviderCatalogReadModel platform = _store.Snapshot<ProviderCatalogReadModel>(StoreName, platformKey).ShouldNotBeNull();
        platform.StreamCommandMessageIds[ProviderCatalogIdentity.EntryId("openai", "gpt-4o")] = ["msg-x"];
        _store.Seed(StoreName, platformKey, platform);

        ProviderCatalogInspectionResult detail = await ExecuteAsync(GetHandler(),
            Query(GetProviderCatalogEntryQuery.QueryType, new GetProviderCatalogEntryQuery("openai", "gpt-4o")));

        detail.TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
        detail.ProjectedCommandMessageIds.ShouldNotBeNull().ShouldBe(["msg-x"]);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Never_enabled_tenant_gets_a_confirmed_empty_catalog(bool platformProjected)
    {
        if (platformProjected)
        {
            Seed();
        }

        _gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns<Task<StreamReadPage>>(call => call.Arg<StreamReadRequest>().Domain == TenantProviderEnablementAggregate.Domain
                ? throw new EventStoreGatewayException(404, "Not Found", reasonCode: StreamReplayReasonCodes.MissingStream)
                : throw new InvalidOperationException("A never-enabled tenant must not probe platform streams."));

        QueryResult listWire = await ListHandler().ExecuteAsync(Query(ListProviderCatalogEntriesQuery.QueryType,
            new ListProviderCatalogEntriesQuery(false), tenantId: TenantId, global: false), CancellationToken.None);
        QueryResult getWire = await GetHandler().ExecuteAsync(Query(GetProviderCatalogEntryQuery.QueryType,
            new GetProviderCatalogEntryQuery("openai", "gpt-4o"), tenantId: TenantId, global: false), CancellationToken.None);

        TenantProviderCatalogInspectionResult list = JsonSerializer.Deserialize<TenantProviderCatalogInspectionResult>(
            listWire.PayloadBytes.ShouldNotBeNull(), _json).ShouldNotBeNull();
        TenantProviderCatalogInspectionResult get = JsonSerializer.Deserialize<TenantProviderCatalogInspectionResult>(
            getWire.PayloadBytes.ShouldNotBeNull(), _json).ShouldNotBeNull();
        list.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
        list.Entries.ShouldBeEmpty();
        list.TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
        get.Status.ShouldBe(ProviderCatalogInspectionStatus.EntryNotFound);
        get.TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
    }

    private ListProviderCatalogEntriesQueryHandler ListHandler()
        => new(_store, Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }), _tenantAccess, _contextProvider, _gateway);

    private GetProviderCatalogEntryQueryHandler GetHandler()
        => new(_store, Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }), _tenantAccess, _contextProvider, _gateway);

    private static async Task<ProviderCatalogInspectionResult> ExecuteAsync(ProviderCatalogQueryHandlerBase handler, QueryEnvelope query)
    {
        QueryResult result = await handler.ExecuteAsync(query, CancellationToken.None);
        result.Success.ShouldBeTrue();
        return JsonSerializer.Deserialize<ProviderCatalogInspectionResult>(result.PayloadBytes.ShouldNotBeNull(), _json)
            .ShouldNotBeNull();
    }

    private void Seed()
        => _store.Seed(
            StoreName,
            ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId),
            new ProviderCatalogReadModel
            {
                CatalogId = ProviderCatalogIdentity.PlatformTenantId,
                TenantId = ProviderCatalogIdentity.PlatformTenantId,
                Entries =
                [
                    new ProviderCatalogEntryView(
                        "openai",
                        "gpt-4o",
                        "OpenAI GPT-4o",
                        ProviderModelStatus.Enabled,
                        true,
                        128_000,
                        16_000,
                        new ProviderModelTimeoutPolicy(30_000, 3),
                        ProviderModelCapabilityFlags.Streaming,
                        ProviderConfigurationState.Configured,
                        "cfg-openai-gpt4o",
                        true,
                        1,
                        new ProviderModelPricing("USD", 0.002m, 0.008m, 1),
                        new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1)),
                ],
                LastSequenceNumber = 1,
                StreamSequences = new Dictionary<string, long>(StringComparer.Ordinal)
                {
                    [ProviderCatalogIdentity.EntryId("openai", "gpt-4o")] = 1,
                },
                ProjectedAt = new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero),
                ProjectionVersion = "1",
            });

    private static QueryEnvelope Query(string queryType, object payload, string tenantId = ProviderCatalogIdentity.PlatformTenantId, bool global = true)
        => new(
            tenantId,
            ProviderCatalogReadModelAddresses.Domain,
            ProviderCatalogIdentity.EntryId("openai", "gpt-4o"),
            queryType,
            JsonSerializer.SerializeToUtf8Bytes(payload, _json),
            "corr-1",
            UserId,
            isGlobalAdmin: global);
}
