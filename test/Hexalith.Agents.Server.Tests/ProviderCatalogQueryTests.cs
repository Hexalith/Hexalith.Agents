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

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Queries;

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

    public ProviderCatalogQueryTests()
    {
        _contextProvider.GetContext().Returns(new AgentAdministrationContext(
            TenantId, UserId, IsAgentsAdmin: false, IsPlatformOperator: true));
        _tenantAccess
            .ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, IsFresh: true));
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

    private ListProviderCatalogEntriesQueryHandler ListHandler()
        => new(_store, Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }), _tenantAccess, _contextProvider);

    private GetProviderCatalogEntryQueryHandler GetHandler()
        => new(_store, Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }), _tenantAccess, _contextProvider);

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
