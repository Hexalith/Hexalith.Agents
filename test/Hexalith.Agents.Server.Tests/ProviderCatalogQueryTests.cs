namespace Hexalith.Agents.Server.Tests;

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Queries;
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

    public ProviderCatalogQueryTests()
        => _tenantAccess
            .ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, IsFresh: true));

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
        get.Entries.ShouldHaveSingleItem().ShouldBe(list.Entries.ShouldHaveSingleItem());
        get.Entries[0].Pricing.ShouldNotBeNull();
        get.ProjectionVersion.ShouldBe("1");
        get.TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
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
            _tenantAccess);

        ProviderCatalogInspectionResult result = await ExecuteAsync(
            handler,
            Query(ListProviderCatalogEntriesQuery.QueryType, new ListProviderCatalogEntriesQuery(true)));

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
            Query(ListProviderCatalogEntriesQuery.QueryType, new ListProviderCatalogEntriesQuery(true)));

        denied.Status.ShouldBe(ProviderCatalogInspectionStatus.NotAuthorized);
        denied.Entries.ShouldBeEmpty();
        JsonSerializer.Serialize(denied).ShouldNotContain("openai");
    }

    private ListProviderCatalogEntriesQueryHandler ListHandler()
        => new(_store, Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }), _tenantAccess);

    private GetProviderCatalogEntryQueryHandler GetHandler()
        => new(_store, Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }), _tenantAccess);

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
            ProviderCatalogReadModelAddresses.Detail(TenantId),
            new ProviderCatalogReadModel
            {
                CatalogId = TenantId,
                TenantId = TenantId,
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
                        new ProviderModelPricing("USD", 0.002m, 0.008m, 1)),
                ],
                LastSequenceNumber = 1,
                ProjectedAt = new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero),
                ProjectionVersion = "1",
            });

    private static QueryEnvelope Query(string queryType, object payload, string tenantId = TenantId)
        => new(
            tenantId,
            ProviderCatalogReadModelAddresses.Domain,
            TenantId,
            queryType,
            JsonSerializer.SerializeToUtf8Bytes(payload, _json),
            "corr-1",
            UserId);
}
