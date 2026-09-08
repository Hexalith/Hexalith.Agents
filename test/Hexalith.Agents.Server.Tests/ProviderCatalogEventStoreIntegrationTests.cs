namespace Hexalith.Agents.Server.Tests;

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.Options;

using Shouldly;

/// <summary>
/// Story 5.3 EventStore command-query-projection evidence: the persisted catalog read-model end state carries
/// capability, pricing, versions, and freshness, and a replayed delivery converges on the same snapshot.
/// </summary>
public sealed class ProviderCatalogEventStoreIntegrationTests
{
    private const string TenantId = "acme";
    private const string StoreName = "statestore";

    private static readonly string Key = ProviderCatalogReadModelAddresses.Detail(TenantId);

    private readonly FakeReadModelStore _store = new();

    [Fact]
    public async Task Projecting_a_created_entry_persists_pricing_capability_version_and_freshness()
    {
        DomainProjectionHandlerResult result = await ProjectAsync(Created());

        result.Status.ShouldBe(ProjectionDispatchStatus.Completed);

        ProviderCatalogReadModel persisted = Persisted();
        persisted.CatalogId.ShouldBe(TenantId);
        persisted.TenantId.ShouldBe(TenantId);
        ProviderCatalogEntryView entry = persisted.Entries.ShouldHaveSingleItem();
        entry.ProviderId.ShouldBe("openai");
        entry.Pricing.ShouldNotBeNull().Currency.ShouldBe("USD");
        entry.CapabilityVersion.ShouldBe(1);
        entry.IsSelectableForNewActiveUse.ShouldBeTrue();
        persisted.ProjectionVersion.ShouldBe("1");
        persisted.ProjectedAt.ShouldNotBeNull();
        JsonSerializer.Serialize(persisted).ShouldNotContain("sk-");
    }

    [Fact]
    public async Task A_duplicate_delivery_of_an_already_folded_slice_changes_nothing()
    {
        await ProjectAsync(Created());
        ProviderCatalogReadModel first = Persisted();

        DomainProjectionHandlerResult replay = await ProjectAsync(Created());

        replay.Status.ShouldBe(ProjectionDispatchStatus.AlreadyCompleted);
        Persisted().ShouldBeEquivalentTo(first);
    }

    [Fact]
    public async Task A_full_replay_from_an_empty_store_reaches_the_same_end_state_as_incremental_delivery()
    {
        await ProjectAsync(Created());
        await ProjectAsync(Updated(sequence: 2));
        ProviderCatalogReadModel incremental = Persisted();

        ProviderCatalogReadModel rebuilt = ProviderCatalogProjectionFold.Fold(Request(Created(), Updated(sequence: 2)), current: null);

        rebuilt.Entries.ShouldBeEquivalentTo(incremental.Entries);
        rebuilt.LastSequenceNumber.ShouldBe(incremental.LastSequenceNumber);
        rebuilt.Entries.ShouldHaveSingleItem().CapabilityVersion.ShouldBe(2);
        rebuilt.Entries[0].Pricing!.PricingVersion.ShouldBe(2);
    }

    [Fact]
    public async Task An_empty_payload_does_not_advance_the_checkpoint()
    {
        await ProjectAsync(Created());
        ProviderCatalogReadModel first = Persisted();

        DomainProjectionHandlerResult result = await ProjectAsync(Event(
            nameof(ProviderModelEntryMetadataUpdated),
            2,
            payloadBytes: []));

        result.Status.ShouldBe(ProjectionDispatchStatus.AlreadyCompleted);
        Persisted().LastSequenceNumber.ShouldBe(first.LastSequenceNumber);
        Persisted().Entries.ShouldHaveSingleItem().DisplayLabel.ShouldBe("OpenAI GPT-4o");
    }

    private ProviderCatalogProjectionHandler Handler()
        => new(_store, _store, Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }));

    private Task<DomainProjectionHandlerResult> ProjectAsync(params ProjectionEventDto[] events)
        => Handler().ProjectAsync(Request(events), "dispatch-1", CancellationToken.None);

    private ProviderCatalogReadModel Persisted()
        => _store.Snapshot<ProviderCatalogReadModel>(StoreName, Key).ShouldNotBeNull();

    private static ProjectionRequest Request(params ProjectionEventDto[] events)
        => new(TenantId, ProviderCatalogReadModelAddresses.Domain, TenantId, events);

    private static ProjectionEventDto Created()
        => Event(nameof(ProviderModelEntryCreated), 1, new ProviderModelEntryCreated(
            TenantId,
            "openai",
            "gpt-4o",
            "OpenAI GPT-4o",
            Enabled: true,
            SupportsTextGeneration: true,
            128_000,
            16_000,
            new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.Streaming,
            ProviderConfigurationState.Configured,
            "cfg-openai-gpt4o",
            new ProviderModelPricing("USD", 0.002m, 0.008m, 1),
            CapabilityVersion: 1));

    private static ProjectionEventDto Updated(long sequence)
        => Event(nameof(ProviderModelEntryMetadataUpdated), sequence, new ProviderModelEntryMetadataUpdated(
            TenantId,
            "openai",
            "gpt-4o",
            "OpenAI GPT-4o (v2)",
            SupportsTextGeneration: true,
            200_000,
            32_000,
            new ProviderModelTimeoutPolicy(45_000, 2),
            ProviderModelCapabilityFlags.Streaming,
            ProviderConfigurationState.Configured,
            "cfg-openai-gpt4o",
            new ProviderModelPricing("USD", 0.003m, 0.009m, 2),
            CapabilityVersion: 2));

    private static ProjectionEventDto Event<T>(string eventTypeName, long sequence, T payload)
        => Event(eventTypeName, sequence, JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

    private static ProjectionEventDto Event(string eventTypeName, long sequence, byte[] payloadBytes)
        => new(
            eventTypeName,
            payloadBytes,
            "json",
            sequence,
            new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero).AddSeconds(sequence),
            "corr-1");
}
