namespace Hexalith.Agents.Server.Tests;

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Client.Projections;
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
    private const string TenantId = ProviderCatalogIdentity.PlatformTenantId;
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
    public async Task Replayed_platform_terms_preserve_the_operator_declaration_and_field_diff()
    {
        ProviderDataHandlingRecord prior = new(30, false, ["EU"], "terms-v1", 1,
            new DateTimeOffset(2026, 6, 24, 0, 0, 0, TimeSpan.Zero));
        ProviderDataHandlingRecord proposed = new(14, false, ["EU"], "terms-v1", 2,
            new DateTimeOffset(2026, 6, 25, 0, 0, 0, TimeSpan.Zero));
        ProviderDataHandlingRecord declared = proposed with
        {
            TighteningDeclaration = ProviderDataHandlingPolicy.DeclareTightening(prior, proposed, "operator"),
        };
        ProjectionEventDto update = Event(nameof(ProviderModelEntryMetadataUpdated), 2,
            new ProviderModelEntryMetadataUpdated(
                ProviderCatalogIdentity.EntryId("openai", "gpt-4o"), "openai", "gpt-4o",
                "OpenAI GPT-4o", true, 128_000, 16_000,
                new ProviderModelTimeoutPolicy(30_000, 3), ProviderModelCapabilityFlags.Streaming,
                ProviderConfigurationState.Configured, "cfg-openai-gpt4o",
                new ProviderModelPricing("USD", 0.002m, 0.008m, 1), 2, declared));

        await ProjectAsync(Created(), update);
        ProviderCatalogReadModel replayed = ProviderCatalogProjectionFold.Fold(Request(Created(), update), current: null);

        ProviderDataHandlingRecord latest = replayed.Entries.ShouldHaveSingleItem().DataHandling.ShouldNotBeNull();
        latest.TighteningDeclaration.ShouldNotBeNull().FieldDiff.NewRetentionDays.ShouldBe(14);
        Persisted().Entries.ShouldHaveSingleItem().DataHandling.ShouldBeEquivalentTo(latest);
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

    [Fact]
    public async Task Rebuilding_one_platform_entry_keeps_the_other_entry_in_the_shared_index()
    {
        await ProjectAsync(Created());
        ProviderModelEntryCreated second = new(
            ProviderCatalogIdentity.EntryId("other", "model"), "other", "model", "Other model",
            true, true, 32_000, 8_000, new ProviderModelTimeoutPolicy(30_000, 3),
            ProviderModelCapabilityFlags.Streaming, ProviderConfigurationState.Configured, "cfg-other",
            new ProviderModelPricing("USD", 0.001m, 0.002m, 1), 1,
            new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1,
                new DateTimeOffset(2026, 6, 24, 0, 0, 0, TimeSpan.Zero)));
        ProjectionRequest secondRequest = new(TenantId, ProviderCatalogReadModelAddresses.Domain,
            second.CatalogId, [Event(nameof(ProviderModelEntryCreated), 1, second)]);
        (await Handler().ProjectAsync(secondRequest, "dispatch-2", CancellationToken.None))
            .Status.ShouldBe(ProjectionDispatchStatus.Completed);

        DomainProjectionRebuildPlan plan = await Handler().PrepareRebuildAsync(
            Request(Created(), Updated(sequence: 2)), "rebuild-1", CancellationToken.None);
        ReadModelBatchResult applied = await _store.ExecuteAsync(new ReadModelBatch(
            new ReadModelBatchScope(StoreName, TenantId, ProviderCatalogReadModelAddresses.Domain,
                ProviderCatalogIdentity.EntryId("openai", "gpt-4o"),
                ProviderCatalogReadModelAddresses.ProjectionName, "rebuild-1"),
            plan.Operations), CancellationToken.None);

        applied.ShouldNotBeNull();
        ProviderCatalogReadModel rebuilt = Persisted();
        rebuilt.Entries.Count.ShouldBe(2);
        rebuilt.Entries.Single(entry => entry.ProviderId == "openai").CapabilityVersion.ShouldBe(2);
        rebuilt.Entries.Single(entry => entry.ProviderId == "other").CapabilityVersion.ShouldBe(1);
        rebuilt.StreamSequences[second.CatalogId].ShouldBe(1);
    }

    private ProviderCatalogProjectionHandler Handler()
        => new(_store, _store, Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }));

    private Task<DomainProjectionHandlerResult> ProjectAsync(params ProjectionEventDto[] events)
        => Handler().ProjectAsync(Request(events), "dispatch-1", CancellationToken.None);

    private ProviderCatalogReadModel Persisted()
        => _store.Snapshot<ProviderCatalogReadModel>(StoreName, Key).ShouldNotBeNull();

    private static ProjectionRequest Request(params ProjectionEventDto[] events)
        => new(TenantId, ProviderCatalogReadModelAddresses.Domain, ProviderCatalogIdentity.EntryId("openai", "gpt-4o"), events);

    private static ProjectionEventDto Created()
        => Event(nameof(ProviderModelEntryCreated), 1, new ProviderModelEntryCreated(
            ProviderCatalogIdentity.EntryId("openai", "gpt-4o"),
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
            CapabilityVersion: 1,
            new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1, new DateTimeOffset(2026, 6, 24, 0, 0, 0, TimeSpan.Zero))));

    private static ProjectionEventDto Updated(long sequence)
        => Event(nameof(ProviderModelEntryMetadataUpdated), sequence, new ProviderModelEntryMetadataUpdated(
            ProviderCatalogIdentity.EntryId("openai", "gpt-4o"),
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
            CapabilityVersion: 2,
            new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1, new DateTimeOffset(2026, 6, 24, 0, 0, 0, TimeSpan.Zero))));

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
