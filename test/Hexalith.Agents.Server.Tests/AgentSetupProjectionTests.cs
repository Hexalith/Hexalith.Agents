namespace Hexalith.Agents.Server.Tests;

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Events;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.Options;

using Shouldly;

/// <summary>
/// Tests for the Agent setup projection (Story 5.2 AC2, AC3). They assert on the <b>persisted</b> read-model end
/// state rather than on the fold's return value, because the story's evidence is what a later reader would find in
/// the store — not what the handler computed in memory.
/// </summary>
public sealed class AgentSetupProjectionTests
{
    private const string TenantId = "acme";
    private const string AgentId = "hexa";
    private const string StoreName = "statestore";

    private static readonly string Key = AgentSetupReadModelAddresses.Detail(TenantId, AgentId);

    private readonly FakeReadModelStore _store = new();

    [Fact]
    public async Task Projecting_a_created_agent_persists_its_safe_setup_with_version_and_freshness()
    {
        DomainProjectionHandlerResult result = await ProjectAsync(Created());

        result.Status.ShouldBe(ProjectionDispatchStatus.Completed);

        AgentSetupReadModel persisted = Persisted();
        persisted.IsCreated.ShouldBeTrue();
        persisted.AgentId.ShouldBe(AgentId);
        persisted.TenantId.ShouldBe(TenantId);
        persisted.DisplayName.ShouldBe("hexa");
        persisted.ConfigurationVersion.ShouldBe(1);
        persisted.LastSequenceNumber.ShouldBe(1);
        persisted.ProjectionVersion.ShouldBe("1");
        persisted.ProjectedAt.ShouldNotBeNull();
    }

    [Fact]
    public async Task The_persisted_read_model_never_carries_the_instruction_text()
    {
        await ProjectAsync(Created(instructions: "a very sensitive system prompt"));

        AgentSetupReadModel persisted = Persisted();
        persisted.HasInstructions.ShouldBeTrue();
        persisted.InstructionsValid.ShouldBeTrue();

        // AD-14: presence/validity/version may be projected; the text itself must exist nowhere in the read model.
        JsonSerializer.Serialize(persisted).ShouldNotContain("sensitive system prompt");
    }

    [Fact]
    public async Task A_duplicate_delivery_of_an_already_folded_slice_changes_nothing()
    {
        await ProjectAsync(Created(), ResponseMode(sequence: 2, AgentResponseMode.Confirmation));
        AgentSetupReadModel first = Persisted();

        DomainProjectionHandlerResult replay = await ProjectAsync(Created(), ResponseMode(sequence: 2, AgentResponseMode.Confirmation));

        replay.Status.ShouldBe(ProjectionDispatchStatus.AlreadyCompleted);
        Persisted().ShouldBeEquivalentTo(first);
    }

    [Fact]
    public async Task A_full_replay_from_an_empty_store_reaches_the_same_end_state_as_incremental_delivery()
    {
        await ProjectAsync(Created());
        await ProjectAsync(ResponseMode(sequence: 2, AgentResponseMode.Confirmation));
        await ProjectAsync(Disabled(sequence: 3));
        AgentSetupReadModel incremental = Persisted();

        var replayed = new FakeReadModelStore();
        AgentSetupReadModel fromReplay = AgentSetupProjectionFold.Fold(
            Request(Created(), ResponseMode(sequence: 2, AgentResponseMode.Confirmation), Disabled(sequence: 3)),
            current: null);
        replayed.Seed(StoreName, Key, fromReplay);

        replayed.Snapshot<AgentSetupReadModel>(StoreName, Key).ShouldBeEquivalentTo(incremental);
    }

    [Fact]
    public async Task A_gapped_delivery_is_retryable_and_leaves_the_prior_state_untouched()
    {
        await ProjectAsync(Created());
        AgentSetupReadModel before = Persisted();

        // Sequence 2 was never delivered; folding 3 onto 1 would bake a state that never existed.
        DomainProjectionHandlerResult result = await ProjectAsync(Disabled(sequence: 3));

        result.Status.ShouldBe(ProjectionDispatchStatus.Retryable);
        result.ReasonCode.ShouldBe(AgentSetupProjectionFold.DeliverySequenceGapReason);
        Persisted().ShouldBeEquivalentTo(before);
    }

    [Fact]
    public async Task An_unresolvable_event_type_is_retryable_rather_than_folded_past()
    {
        DomainProjectionHandlerResult result = await ProjectAsync(
            new ProjectionEventDto("SomeEventFromAFutureDeployment", "{}"u8.ToArray(), "json", 1, Timestamp(1), "corr-1"));

        result.Status.ShouldBe(ProjectionDispatchStatus.Retryable);
        result.ReasonCode.ShouldBe(AgentSetupProjectionFold.UnresolvedEventReason);
        _store.Snapshot<AgentSetupReadModel>(StoreName, Key).ShouldBeNull();
    }

    [Fact]
    public async Task A_first_write_that_loses_the_race_is_retryable_and_keeps_the_winner()
    {
        // A concurrent dispatch creates the key after this handler read an empty slot, so its create-only write
        // must lose rather than clobber the state the winner already persisted.
        _store.ConcurrentWriteBeforeBatch = () => _store.Seed(
            StoreName,
            Key,
            AgentSetupProjectionFold.Fold(
                Request(Event(nameof(AgentCreated), 1, new AgentCreated(AgentId, TenantId, "winner", null, "instructions long enough to be valid", 1, 1))),
                current: null));

        DomainProjectionHandlerResult result = await ProjectAsync(Created());

        result.Status.ShouldBe(ProjectionDispatchStatus.Retryable);
        Persisted().DisplayName.ShouldBe("winner");
    }

    [Fact]
    public async Task A_follow_up_write_against_a_stale_etag_is_retryable_and_keeps_the_stored_state()
    {
        await ProjectAsync(Created());

        // The key moved on between this handler's read and its write, so the expected-ETag precondition fails.
        _store.ConcurrentWriteBeforeBatch = () =>
        {
            AgentSetupReadModel concurrent = Persisted();
            concurrent.DisplayName = "moved on";
            _store.Seed(StoreName, Key, concurrent);
        };

        DomainProjectionHandlerResult result = await ProjectAsync(Created(), ResponseMode(sequence: 2, AgentResponseMode.Confirmation));

        result.Status.ShouldBe(ProjectionDispatchStatus.Retryable);
        Persisted().DisplayName.ShouldBe("moved on");
    }

    [Fact]
    public async Task A_rebuild_plan_writes_the_same_end_state_the_incremental_path_produced()
    {
        await ProjectAsync(Created(), ResponseMode(sequence: 2, AgentResponseMode.Confirmation));
        AgentSetupReadModel incremental = Persisted();

        DomainProjectionRebuildPlan plan = await Handler().PrepareRebuildAsync(
            Request(Created(), ResponseMode(sequence: 2, AgentResponseMode.Confirmation)),
            "rebuild-1",
            CancellationToken.None);

        plan.StoreName.ShouldBe(StoreName);
        plan.Operations.Count.ShouldBe(1);

        var rebuilt = new FakeReadModelStore();
        _ = await rebuilt.ExecuteAsync(
            new EventStore.Client.Projections.ReadModelBatch(
                new EventStore.Client.Projections.ReadModelBatchScope(
                    StoreName, TenantId, AgentSetupReadModelAddresses.Domain, AgentId, AgentSetupReadModelAddresses.ProjectionName, "rebuild-1"),
                plan.Operations),
            CancellationToken.None);

        rebuilt.Snapshot<AgentSetupReadModel>(StoreName, Key).ShouldBeEquivalentTo(incremental);
    }

    [Fact]
    public async Task Two_tenants_owning_the_same_agent_id_never_share_a_read_model()
    {
        await ProjectAsync(Created());
        _ = await Handler().ProjectAsync(
            new ProjectionRequest(
                "other-tenant",
                AgentSetupReadModelAddresses.Domain,
                AgentId,
                [Event(nameof(AgentCreated), 1, new AgentCreated(AgentId, "other-tenant", "their hexa", null, "instructions long enough to be valid", 1, 1))]),
            "dispatch-other",
            CancellationToken.None);

        Persisted().DisplayName.ShouldBe("hexa");
        _store.Snapshot<AgentSetupReadModel>(StoreName, AgentSetupReadModelAddresses.Detail("other-tenant", AgentId))
            .ShouldNotBeNull().DisplayName.ShouldBe("their hexa");
    }

    private AgentSetupProjectionHandler Handler()
        => new(_store, _store, Options.Create(new AgentSetupReadModelOptions { StateStoreName = StoreName }));

    private Task<DomainProjectionHandlerResult> ProjectAsync(params ProjectionEventDto[] events)
        => Handler().ProjectAsync(Request(events), "dispatch-1", CancellationToken.None);

    private AgentSetupReadModel Persisted()
        => _store.Snapshot<AgentSetupReadModel>(StoreName, Key).ShouldNotBeNull();

    private static ProjectionRequest Request(params ProjectionEventDto[] events)
        => new(TenantId, AgentSetupReadModelAddresses.Domain, AgentId, events);

    private static ProjectionEventDto Created(string instructions = "instructions long enough to be valid")
        => Event(nameof(AgentCreated), 1, new AgentCreated(AgentId, TenantId, "hexa", null, instructions, 1, 1));

    private static ProjectionEventDto ResponseMode(long sequence, AgentResponseMode mode)
        => Event(nameof(AgentResponseModeConfigured), sequence, new AgentResponseModeConfigured(AgentId, mode, (int)sequence));

    private static ProjectionEventDto Disabled(long sequence)
        => Event(nameof(AgentDisabled), sequence, new AgentDisabled(AgentId));

    private static ProjectionEventDto Event<T>(string eventTypeName, long sequence, T payload)
        => new(
            eventTypeName,
            JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            "json",
            sequence,
            Timestamp(sequence),
            "corr-1");

    private static DateTimeOffset Timestamp(long sequence)
        => new DateTimeOffset(2026, 6, 24, 12, 0, 0, TimeSpan.Zero).AddSeconds(sequence);
}
