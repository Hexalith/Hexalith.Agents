using System.Net.Http.Json;
using System.Text.Json;

using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Api;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;
using Hexalith.Agents.TenantProviderEnablement;

using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Projections;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Persisted migration targets, exact retries, and preflight conflicts.</summary>
public sealed class ProviderCatalogMigrationTests
{
    private const string StoreName = "statestore";
    private readonly FakeReadModelStore _store = new();
    private readonly List<CommandEnvelope> _sent = [];
    private readonly Dictionary<string, long> _legacyHeads = new(StringComparer.Ordinal);
    private readonly Dictionary<string, long> _targetHeads = new(StringComparer.Ordinal);
    private readonly HashSet<string> _rejectedMessages = new(StringComparer.Ordinal);
    private bool _rejectNextCommand;
    private bool _mutatePlatformAfterTenantB;
    private bool _togglePlatformAfterTenantB;
    private bool _advanceLegacyAfterTenantB;
    private CommandStatus? _forcedStatus;
    private string? _missingTargetReason;

    private static string TargetKey(string tenantId, string domain, string aggregateId)
        => $"{tenantId}/{domain}/{aggregateId}";

    [Theory]
    [InlineData(CommandStatus.PublishFailed, "AuthoritativePending")]
    [InlineData(CommandStatus.TimedOut, "DispatchFailed")]
    public async Task Stored_publish_failure_keeps_migration_pending_for_drain_recovery(
        CommandStatus status, string expected)
    {
        SeedLegacy("tenant-a", Entry());
        _forcedStatus = status;

        ProviderCatalogMigrationResult result = await Service().MigrateAsync(["tenant-a"]);

        result.Status.ShouldBe(expected);
        result.PlatformEntries.ShouldBe(1);
        _sent.ShouldHaveSingleItem().CommandType.ShouldBe(nameof(CreateProviderModelEntry));
        _store.Snapshot<ProviderCatalogReadModel>(StoreName,
            ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId))
            .ShouldNotBeNull().Entries.ShouldHaveSingleItem();
    }

    [Theory]
    [InlineData("missing-stream", "ProjectionConfirmed")]
    [InlineData("other", "AuthoritativePending")]
    public async Task First_migration_accepts_only_exact_missing_stream_as_zero_target(
        string reason, string expected)
    {
        SeedLegacy("tenant-a", Entry());
        _missingTargetReason = reason;

        ProviderCatalogMigrationResult result = await Service().MigrateAsync(["tenant-a"]);

        result.Status.ShouldBe(expected);
        if (reason == "missing-stream")
        {
            _sent.Count.ShouldBe(2);
        }
        else
        {
            _sent.ShouldBeEmpty();
        }
    }

    [Theory]
    [InlineData("platform")]
    [InlineData("tenant")]
    public async Task Repeat_with_a_stale_target_projection_remains_pending_without_dispatch(string target)
    {
        SeedLegacy("tenant-a", Entry());
        ProviderCatalogMigrationService migration = Service();
        (await migration.MigrateAsync(["tenant-a"])).Status.ShouldBe("ProjectionConfirmed");
        int sent = _sent.Count;
        string key = target == "platform"
            ? TargetKey(ProviderCatalogIdentity.PlatformTenantId, ProviderCatalogAggregate.Domain,
                ProviderCatalogIdentity.EntryId("openai", "gpt-4o"))
            : TargetKey("tenant-a", TenantProviderEnablementAggregate.Domain, "tenant-a");
        _targetHeads[key]++;

        ProviderCatalogMigrationResult repeat = await migration.MigrateAsync(["tenant-a"]);

        repeat.Status.ShouldBe("AuthoritativePending");
        _sent.Count.ShouldBe(sent);
    }

    [Fact]
    public async Task Rejected_migration_command_cannot_be_confirmed_by_a_matching_target_projection()
    {
        SeedLegacy("tenant-a", Entry());
        _rejectNextCommand = true;

        ProviderCatalogMigrationResult result = await Service().MigrateAsync(["tenant-a"]);

        result.Status.ShouldBe("Rejected");
        _sent.Count.ShouldBe(1);
        _store.Snapshot<ProviderCatalogReadModel>(StoreName,
            ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId))
            .ShouldNotBeNull().Entries.ShouldHaveSingleItem();
    }

    [Fact]
    public async Task Delayed_legacy_delivery_blocks_migration_until_projected_then_exact_repeat_is_no_op()
    {
        SeedLegacy("tenant-a", Entry());
        _legacyHeads["tenant-a"] = 2;
        ProviderCatalogMigrationService migration = Service();

        ProviderCatalogMigrationResult before = await migration.MigrateAsync(["tenant-a"]);
        before.Status.ShouldBe("AuthoritativePending");
        _sent.ShouldBeEmpty();

        var handler = new ProviderCatalogProjectionHandler(_store, _store,
            Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }));
        var delivered = new ProviderModelEntryDisabled("tenant-a", "openai", "gpt-4o");
        var request = new ProjectionRequest("tenant-a", ProviderCatalogAggregate.Domain, "tenant-a",
            [new ProjectionEventDto(nameof(ProviderModelEntryDisabled),
                JsonSerializer.SerializeToUtf8Bytes(delivered), "json", 2,
                new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero), "corr-legacy", "msg-legacy")]);
        await handler.ProjectAsync(request, "dispatch-legacy", CancellationToken.None);

        ProviderCatalogReadModel caughtUp = _store.Snapshot<ProviderCatalogReadModel>(StoreName,
            ProviderCatalogReadModelAddresses.Detail("tenant-a")).ShouldNotBeNull();
        caughtUp.StreamSequences["tenant-a"].ShouldBe(2);
        caughtUp.Entries.ShouldHaveSingleItem().Status.ShouldBe(ProviderModelStatus.Disabled);

        (await migration.MigrateAsync(["tenant-a"])).Status.ShouldBe("ProjectionConfirmed");
        (await migration.MigrateAsync(["tenant-a"])).Status.ShouldBe("NoOp");
        _sent.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Pre_upgrade_legacy_checkpoint_accepts_sequence_two_then_migration_repeats_as_no_op()
    {
        SeedLegacy("tenant-a", Entry());
        ProviderCatalogReadModel prior = _store.Snapshot<ProviderCatalogReadModel>(StoreName,
            ProviderCatalogReadModelAddresses.Detail("tenant-a")).ShouldNotBeNull();
        prior.StreamSequences.Clear();
        _store.Seed(StoreName, ProviderCatalogReadModelAddresses.Detail("tenant-a"), prior);
        _legacyHeads["tenant-a"] = 2;
        ProviderCatalogMigrationService migration = Service();
        (await migration.MigrateAsync(["tenant-a"])).Status.ShouldBe("AuthoritativePending");

        var handler = new ProviderCatalogProjectionHandler(_store, _store,
            Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }));
        var disabled = new ProviderModelEntryDisabled("tenant-a", "openai", "gpt-4o");
        var request = new ProjectionRequest("tenant-a", ProviderCatalogAggregate.Domain, "tenant-a",
            [new ProjectionEventDto(nameof(ProviderModelEntryDisabled),
                JsonSerializer.SerializeToUtf8Bytes(disabled), "json", 2,
                new DateTimeOffset(2026, 9, 24, 12, 0, 0, TimeSpan.Zero), "corr-legacy", "msg-legacy")]);

        (await handler.ProjectAsync(request, "dispatch-upgrade", CancellationToken.None))
            .Status.ShouldBe(ProjectionDispatchStatus.Completed);
        ProviderCatalogReadModel caughtUp = _store.Snapshot<ProviderCatalogReadModel>(StoreName,
            ProviderCatalogReadModelAddresses.Detail("tenant-a")).ShouldNotBeNull();
        caughtUp.StreamSequences["tenant-a"].ShouldBe(2);
        caughtUp.Entries.ShouldHaveSingleItem().Status.ShouldBe(ProviderModelStatus.Disabled);
        (await migration.MigrateAsync(["tenant-a"])).Status.ShouldBe("ProjectionConfirmed");
        (await migration.MigrateAsync(["tenant-a"])).Status.ShouldBe("NoOp");
    }

    [Fact]
    public async Task Migration_confirms_persisted_targets_and_an_exact_repeat_sends_nothing()
    {
        SeedLegacy("tenant-b", Entry());
        SeedLegacy("tenant-a", Entry());
        ProviderCatalogMigrationService migration = Service();

        ProviderCatalogMigrationResult first = await migration.MigrateAsync(["tenant-b", "tenant-a"]);
        ProviderCatalogMigrationResult repeat = await migration.MigrateAsync(["tenant-a", "tenant-b"]);

        first.Status.ShouldBe("ProjectionConfirmed");
        first.PlatformEntries.ShouldBe(1);
        first.TenantEntries.ShouldBe(2);
        repeat.Status.ShouldBe("NoOp");
        repeat.PlatformEntries.ShouldBe(0);
        repeat.TenantEntries.ShouldBe(0);
        _sent.Count.ShouldBe(3);
        _sent[0].TenantId.ShouldBe(ProviderCatalogIdentity.PlatformTenantId);
        _sent[0].AggregateId.ShouldBe(ProviderCatalogIdentity.EntryId("openai", "gpt-4o"));
        _sent.Skip(1).Select(item => item.TenantId).ShouldBe(["tenant-a", "tenant-b"]);

        ProviderCatalogReadModel platform = _store.Snapshot<ProviderCatalogReadModel>(StoreName,
            ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId)).ShouldNotBeNull();
        platform.Entries.ShouldHaveSingleItem().MigratedFrom.ShouldBe(
            $"legacy:provider-model:{ProviderCatalogIdentity.EntryId("openai", "gpt-4o")}");
        CreateProviderModelEntry submitted = JsonSerializer.Deserialize<CreateProviderModelEntry>(_sent[0].Payload).ShouldNotBeNull();
        ProviderCatalogEntryView source = Entry();
        submitted.DisplayLabel.ShouldBe(source.DisplayLabel);
        submitted.SupportsTextGeneration.ShouldBe(source.SupportsTextGeneration);
        submitted.ContextWindowTokenLimit.ShouldBe(source.ContextWindowTokenLimit);
        submitted.MaxOutputTokenLimit.ShouldBe(source.MaxOutputTokenLimit);
        submitted.TimeoutPolicy.ShouldBe(source.TimeoutPolicy);
        submitted.SafeCapabilityFlags.ShouldBe(source.SafeCapabilityFlags);
        submitted.ConfigurationReferenceId.ShouldBe(source.ConfigurationReferenceId);
        submitted.Pricing.ShouldBe(source.Pricing);
        submitted.InitialCapabilityVersion.ShouldBe(source.CapabilityVersion);
        foreach (string tenantId in new[] { "tenant-a", "tenant-b" })
        {
            TenantProviderEnablementReadModel tenant = _store.Snapshot<TenantProviderEnablementReadModel>(StoreName,
                TenantProviderEnablementReadModelAddresses.Detail(tenantId)).ShouldNotBeNull();
            tenant.State.Revision.ShouldBe(1);
            tenant.State.Entries.ShouldHaveSingleItem().Value.Enabled.ShouldBeTrue();
        }
    }

    [Fact]
    public async Task Final_revalidation_detects_a_projected_mutation_to_an_earlier_target()
    {
        SeedLegacy("tenant-a", Entry());
        SeedLegacy("tenant-b", Entry());
        _mutatePlatformAfterTenantB = true;

        ProviderCatalogMigrationResult result = await Service().MigrateAsync(["tenant-a", "tenant-b"]);

        result.Status.ShouldBe("AuthoritativePending");
        result.PlatformEntries.ShouldBe(1);
        result.TenantEntries.ShouldBe(2);
        _sent.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Final_revalidation_rejects_a_legacy_event_admitted_during_multi_tenant_dispatch()
    {
        SeedLegacy("tenant-a", Entry());
        SeedLegacy("tenant-b", Entry());
        _advanceLegacyAfterTenantB = true;

        ProviderCatalogMigrationResult result = await Service().MigrateAsync(["tenant-a", "tenant-b"]);

        result.Status.ShouldBe("AuthoritativePending");
        _legacyHeads["tenant-a"].ShouldBe(2);
        _sent.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Final_revalidation_rejects_a_disable_reenable_with_equal_final_metadata()
    {
        SeedLegacy("tenant-a", Entry());
        SeedLegacy("tenant-b", Entry());
        _togglePlatformAfterTenantB = true;

        ProviderCatalogMigrationResult result = await Service().MigrateAsync(["tenant-a", "tenant-b"]);

        result.Status.ShouldBe("AuthoritativePending");
        ProviderCatalogReadModel platform = _store.Snapshot<ProviderCatalogReadModel>(StoreName,
            ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId)).ShouldNotBeNull();
        platform.Entries.ShouldHaveSingleItem().Status.ShouldBe(ProviderModelStatus.Enabled);
        _sent.Count.ShouldBe(3);
    }

    [Fact]
    public async Task Mixed_legacy_status_uses_valid_enabled_source_for_platform_and_preserves_tenant_statuses()
    {
        SeedLegacy("tenant-a", Entry() with { Status = ProviderModelStatus.Disabled, IsSelectableForNewActiveUse = false });
        SeedLegacy("tenant-b", Entry());

        ProviderCatalogMigrationResult result = await Service().MigrateAsync(["tenant-a", "tenant-b"]);

        result.Status.ShouldBe("ProjectionConfirmed");
        CreateProviderModelEntry create = JsonSerializer.Deserialize<CreateProviderModelEntry>(_sent[0].Payload).ShouldNotBeNull();
        create.Enabled.ShouldBeTrue();
        _store.Snapshot<TenantProviderEnablementReadModel>(StoreName,
            TenantProviderEnablementReadModelAddresses.Detail("tenant-a")).ShouldNotBeNull()
            .State.Entries.ShouldHaveSingleItem().Value.Enabled.ShouldBeFalse();
        _store.Snapshot<TenantProviderEnablementReadModel>(StoreName,
            TenantProviderEnablementReadModelAddresses.Detail("tenant-b")).ShouldNotBeNull()
            .State.Entries.ShouldHaveSingleItem().Value.Enabled.ShouldBeTrue();
    }

    [Fact]
    public async Task Later_tenant_extends_a_platform_model_without_changing_stable_provenance_or_allowing_divergent_metadata()
    {
        SeedLegacy("tenant-a", Entry() with { Status = ProviderModelStatus.Disabled, IsSelectableForNewActiveUse = false });
        ProviderCatalogMigrationService migration = Service();
        (await migration.MigrateAsync(["tenant-a"])).Status.ShouldBe("ProjectionConfirmed");
        string provenance = _store.Snapshot<ProviderCatalogReadModel>(StoreName,
            ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId)).ShouldNotBeNull()
            .Entries.ShouldHaveSingleItem().MigratedFrom.ShouldNotBeNull();

        SeedLegacy("tenant-b", Entry());
        ProviderCatalogMigrationResult extended = await migration.MigrateAsync(["tenant-b"]);
        extended.Status.ShouldBe("ProjectionConfirmed");
        extended.PlatformEntries.ShouldBe(1);
        extended.TenantEntries.ShouldBe(1);
        _sent.Count.ShouldBe(4);
        ProviderCatalogEntryView platform = _store.Snapshot<ProviderCatalogReadModel>(StoreName,
            ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId)).ShouldNotBeNull()
            .Entries.ShouldHaveSingleItem();
        platform.MigratedFrom.ShouldBe(provenance);
        platform.Status.ShouldBe(ProviderModelStatus.Enabled);

        SeedLegacy("tenant-c", Entry() with { DisplayLabel = "Divergent" });
        (await migration.MigrateAsync(["tenant-c"])).Status.ShouldBe("TargetConflict");
        _sent.Count.ShouldBe(4);
    }

    [Theory]
    [InlineData("divergent-legacy", "DivergentLegacyMetadata")]
    [InlineData("divergent-target", "TargetConflict")]
    [InlineData("enablement-target", "TargetConflict")]
    [InlineData("missing-inventory", "InvalidLegacyInventory")]
    public async Task Invalid_migration_matrix_rejects_every_row_before_dispatch(string scenario, string expected)
    {
        if (scenario != "missing-inventory")
        {
            SeedLegacy("tenant-a", scenario == "enablement-target"
                ? Entry() with { Status = ProviderModelStatus.Disabled, IsSelectableForNewActiveUse = false }
                : Entry());
        }

        if (scenario == "divergent-legacy")
        {
            SeedLegacy("tenant-b", Entry() with { DisplayLabel = "Different" });
        }
        else if (scenario is "divergent-target" or "enablement-target")
        {
            _store.Seed(StoreName, ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId),
                new ProviderCatalogReadModel
                {
                    CatalogId = ProviderCatalogIdentity.PlatformTenantId,
                    TenantId = ProviderCatalogIdentity.PlatformTenantId,
                    Entries = [Entry() with {
                        DisplayLabel = scenario == "divergent-target" ? "Different" : Entry().DisplayLabel,
                        Status = ProviderModelStatus.Enabled,
                        MigratedFrom =
                        $"legacy:provider-model:{ProviderCatalogIdentity.EntryId("openai", "gpt-4o")}" }],
                });
        }

        ProviderCatalogMigrationResult result = await Service().MigrateAsync(
            scenario == "divergent-legacy" ? ["tenant-a", "tenant-b"] : ["tenant-a"]);

        result.Status.ShouldBe(expected);
        _sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Legacy_entry_without_complete_terms_is_migrated_disabled_and_stays_disabled_on_retry()
    {
        SeedLegacy("tenant-a", Entry() with { DataHandling = null });
        ProviderCatalogMigrationService migration = Service();

        ProviderCatalogMigrationResult first = await migration.MigrateAsync(["tenant-a"]);
        ProviderCatalogMigrationResult repeat = await migration.MigrateAsync(["tenant-a"]);

        first.Status.ShouldBe("ProjectionConfirmed");
        repeat.Status.ShouldBe("NoOp");
        ProviderCatalogReadModel platform = _store.Snapshot<ProviderCatalogReadModel>(StoreName,
            ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId)).ShouldNotBeNull();
        platform.Entries.ShouldHaveSingleItem().Status.ShouldBe(ProviderModelStatus.Disabled);
        TenantProviderEnablementReadModel tenant = _store.Snapshot<TenantProviderEnablementReadModel>(StoreName,
            TenantProviderEnablementReadModelAddresses.Detail("tenant-a")).ShouldNotBeNull();
        tenant.State.Entries.ShouldHaveSingleItem().Value.Enabled.ShouldBeFalse();
    }

    [Fact]
    public async Task Legacy_inventory_rejects_a_read_model_with_a_different_persisted_tenant_identity()
    {
        _store.Seed(StoreName, ProviderCatalogReadModelAddresses.Detail("tenant-a"),
            new ProviderCatalogReadModel
            {
                CatalogId = "tenant-b",
                TenantId = "tenant-b",
                Entries = [Entry()],
            });

        ProviderCatalogMigrationResult result = await Service().MigrateAsync(["tenant-a"]);

        result.Status.ShouldBe("InvalidLegacyInventory");
        _sent.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("XTS")]
    [InlineData("XXX")]
    [InlineData("XAU")]
    public async Task Invalid_second_legacy_price_rejects_before_dispatching_the_first_target(string currency)
    {
        SeedLegacy("tenant-a", Entry() with { ProviderId = "a-provider", ModelId = "first" });
        string key = ProviderCatalogReadModelAddresses.Detail("tenant-a");
        ProviderCatalogReadModel inventory = _store.Snapshot<ProviderCatalogReadModel>(StoreName, key)
            .ShouldNotBeNull();
        inventory.Entries.Add(Entry() with
        {
            ProviderId = "z-provider", ModelId = "second",
            Pricing = Entry().Pricing! with { Currency = currency },
        });
        _store.Seed(StoreName, key, inventory);

        ProviderCatalogMigrationResult result = await Service().MigrateAsync(["tenant-a"]);

        result.Status.ShouldBe("InvalidLegacyInventory");
        _sent.ShouldBeEmpty();
    }

    [Fact]
    public async Task Platform_operator_can_invoke_migration_through_the_operations_route()
    {
        SeedLegacy("tenant-a", Entry());
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(Service());
        builder.Services.AddSingleton(AgentsClient.Unavailable());
        await using WebApplication app = builder.Build();
        app.MapAgentsOperationEndpoints();
        await app.StartAsync();

        using var response = await app.GetTestClient().PostAsJsonAsync(
            "/api/agents/operations/providers/migration",
            new ProviderCatalogMigrationRequest(["tenant-a"]));
        response.EnsureSuccessStatusCode();
        ProviderCatalogMigrationResult result = (await response.Content.ReadFromJsonAsync<ProviderCatalogMigrationResult>()).ShouldNotBeNull();
        result.Status.ShouldBe("ProjectionConfirmed");
        _sent.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Migration_route_rejects_a_caller_without_platform_operator_authority()
    {
        SeedLegacy("tenant-a", Entry());
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddSingleton(Service(isPlatformOperator: false));
        builder.Services.AddSingleton(AgentsClient.Unavailable());
        await using WebApplication app = builder.Build();
        app.MapAgentsOperationEndpoints();
        await app.StartAsync();

        using var response = await app.GetTestClient().PostAsJsonAsync(
            "/api/agents/operations/providers/migration",
            new ProviderCatalogMigrationRequest(["tenant-a"]));
        response.EnsureSuccessStatusCode();
        ProviderCatalogMigrationResult result = (await response.Content.ReadFromJsonAsync<ProviderCatalogMigrationResult>()).ShouldNotBeNull();
        result.Status.ShouldBe("NotAuthorized");
        _sent.ShouldBeEmpty();
    }

    private ProviderCatalogMigrationService Service(bool isPlatformOperator = true)
    {
        IEventStoreGatewayClient gateway = Substitute.For<IEventStoreGatewayClient>();
        gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                StreamReadRequest request = call.Arg<StreamReadRequest>();
                if (_missingTargetReason is not null
                    && !(request.Domain == ProviderCatalogAggregate.Domain && request.AggregateId == request.Tenant)
                    && !_targetHeads.ContainsKey(TargetKey(request.Tenant, request.Domain,
                        request.AggregateId ?? string.Empty)))
                {
                    throw new EventStoreGatewayException(404, "Not Found", reasonCode: _missingTargetReason);
                }
                long head = request.Domain == ProviderCatalogAggregate.Domain && request.AggregateId == request.Tenant
                    ? _legacyHeads.GetValueOrDefault(request.Tenant)
                    : _targetHeads.GetValueOrDefault(TargetKey(request.Tenant, request.Domain, request.AggregateId ?? string.Empty));
                return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                    new StreamReadMetadata(request.FromSequence, null, null, head, 0, false, null));
            });
        gateway.GetCommandStatusAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                CommandEnvelope? command = _sent.FirstOrDefault(item => item.MessageId == call.Arg<string>());
                bool rejected = command is not null && _rejectedMessages.Contains(command.MessageId);
                CommandStatus commandStatus = _forcedStatus ?? (rejected ? CommandStatus.Rejected : CommandStatus.Completed);
                return command is null ? null : new CommandStatusQueryResponse(command.CorrelationId,
                    commandStatus.ToString(), (int)commandStatus,
                    RejectionEventType: rejected ? "InvalidProviderModelMetadataRejection" : null,
                    MessageId: command.MessageId)
                {
                    TenantId = command.TenantId,
                    EventCount = rejected ? null : 1,
                };
            });
        IAgentCommandDispatcher dispatcher = Substitute.For<IAgentCommandDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                CommandEnvelope envelope = call.Arg<CommandEnvelope>();
                _sent.Add(envelope);
                if (_rejectNextCommand)
                {
                    _rejectNextCommand = false;
                    _rejectedMessages.Add(envelope.MessageId);
                    // Another writer projects the same target while this exact command is rejected.
                    Persist(envelope with { MessageId = "unrelated-writer" });
                }
                else
                {
                    Persist(envelope);
                }
                return Task.FromResult(new SubmitCommandResponse(envelope.CorrelationId, null, envelope.MessageId));
            });
        IAgentAdministrationContextProvider context = Substitute.For<IAgentAdministrationContextProvider>();
        context.GetContext().Returns(new AgentAdministrationContext(
            "tenant-a", "operator", IsAgentsAdmin: false, IsPlatformOperator: isPlatformOperator));
        return new(new ProviderCatalogAdministrationOrchestrator(dispatcher), context,
            new AgentCommandIdentityFactory(), _store,
            Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }), gateway: gateway);
    }

    private void Persist(CommandEnvelope envelope)
    {
        if (envelope.CommandType == nameof(CreateProviderModelEntry))
        {
            CreateProviderModelEntry command = JsonSerializer.Deserialize<CreateProviderModelEntry>(envelope.Payload).ShouldNotBeNull();
            string key = ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId);
            ProviderCatalogReadModel model = _store.Snapshot<ProviderCatalogReadModel>(StoreName, key)
                ?? new ProviderCatalogReadModel
                {
                    CatalogId = ProviderCatalogIdentity.PlatformTenantId,
                    TenantId = ProviderCatalogIdentity.PlatformTenantId,
                };
            model.Entries.Add(new ProviderCatalogEntryView(command.ProviderId, command.ModelId,
                command.DisplayLabel, command.Enabled ? ProviderModelStatus.Enabled : ProviderModelStatus.Disabled,
                command.SupportsTextGeneration, command.ContextWindowTokenLimit, command.MaxOutputTokenLimit,
                command.TimeoutPolicy, command.SafeCapabilityFlags,
                string.IsNullOrWhiteSpace(command.ConfigurationReferenceId)
                    ? ProviderConfigurationState.NotConfigured : ProviderConfigurationState.Configured,
                command.ConfigurationReferenceId, command.Enabled, command.InitialCapabilityVersion,
                command.Pricing, command.DataHandling, command.MigratedFrom));
            string targetKey = TargetKey(envelope.TenantId, envelope.Domain, envelope.AggregateId);
            long sequence = _targetHeads[targetKey] = _targetHeads.GetValueOrDefault(targetKey) + 1;
            model.StreamSequences[envelope.AggregateId] = sequence;
            model.StreamCommandMessageIds[envelope.AggregateId] = [envelope.MessageId];
            _store.Seed(StoreName, key, model);
        }
        else if (envelope.CommandType == nameof(EnableProviderModelEntry))
        {
            EnableProviderModelEntry command = JsonSerializer.Deserialize<EnableProviderModelEntry>(envelope.Payload).ShouldNotBeNull();
            string key = ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId);
            ProviderCatalogReadModel model = _store.Snapshot<ProviderCatalogReadModel>(StoreName, key).ShouldNotBeNull();
            ProviderCatalogEntryView current = model.Entries.Single(item => item.ProviderId == command.ProviderId && item.ModelId == command.ModelId);
            command.ExpectedLifecycleRevision.ShouldBe(current.LifecycleRevision);
            model.Entries.Remove(current);
            model.Entries.Add(current with
            {
                Status = ProviderModelStatus.Enabled,
                IsSelectableForNewActiveUse = true,
                LifecycleRevision = current.LifecycleRevision + 1,
            });
            string targetKey = TargetKey(envelope.TenantId, envelope.Domain, envelope.AggregateId);
            long sequence = _targetHeads[targetKey] = _targetHeads.GetValueOrDefault(targetKey) + 1;
            model.StreamSequences[envelope.AggregateId] = sequence;
            model.StreamCommandMessageIds[envelope.AggregateId] = [envelope.MessageId];
            _store.Seed(StoreName, key, model);
        }
        else if (envelope.CommandType == nameof(SetTenantProviderModelEnablement))
        {
            SetTenantProviderModelEnablement command = JsonSerializer.Deserialize<SetTenantProviderModelEnablement>(envelope.Payload).ShouldNotBeNull();
            string key = TenantProviderEnablementReadModelAddresses.Detail(command.TenantId);
            TenantProviderEnablementReadModel model = _store.Snapshot<TenantProviderEnablementReadModel>(StoreName, key)
                ?? new TenantProviderEnablementReadModel();
            model.State.Apply(new TenantProviderModelEnablementSet(command.TenantId, command.ProviderId,
                command.ModelId, command.Enabled, model.State.Revision + 1, "operator", command.MigratedFrom));
            string targetKey = TargetKey(envelope.TenantId, envelope.Domain, envelope.AggregateId);
            model.LastSequenceNumber = _targetHeads[targetKey] = _targetHeads.GetValueOrDefault(targetKey) + 1;
            model.ProjectedCommandMessageIds.Add(envelope.MessageId);
            _store.Seed(StoreName, key, model);
            if (_mutatePlatformAfterTenantB && command.TenantId == "tenant-b")
            {
                _mutatePlatformAfterTenantB = false;
                string platformKey = ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId);
                ProviderCatalogReadModel platform = _store.Snapshot<ProviderCatalogReadModel>(StoreName, platformKey)
                    .ShouldNotBeNull();
                platform.Entries[0] = platform.Entries[0] with { DisplayLabel = "Concurrent operator edit" };
                string platformEntryId = ProviderCatalogIdentity.EntryId("openai", "gpt-4o");
                string platformTargetKey = TargetKey(ProviderCatalogIdentity.PlatformTenantId,
                    ProviderCatalogAggregate.Domain, platformEntryId);
                platform.StreamSequences[platformEntryId] = _targetHeads[platformTargetKey]
                    = _targetHeads.GetValueOrDefault(platformTargetKey) + 1;
                _store.Seed(StoreName, platformKey, platform);
            }
            if (_advanceLegacyAfterTenantB && command.TenantId == "tenant-b")
            {
                _advanceLegacyAfterTenantB = false;
                _legacyHeads["tenant-a"]++;
            }
            if (_togglePlatformAfterTenantB && command.TenantId == "tenant-b")
            {
                _togglePlatformAfterTenantB = false;
                string platformKey = ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId);
                ProviderCatalogReadModel platform = _store.Snapshot<ProviderCatalogReadModel>(StoreName, platformKey)
                    .ShouldNotBeNull();
                string platformEntryId = ProviderCatalogIdentity.EntryId("openai", "gpt-4o");
                string platformTargetKey = TargetKey(ProviderCatalogIdentity.PlatformTenantId,
                    ProviderCatalogAggregate.Domain, platformEntryId);
                platform.StreamSequences[platformEntryId] = _targetHeads[platformTargetKey]
                    = _targetHeads.GetValueOrDefault(platformTargetKey) + 2;
                _store.Seed(StoreName, platformKey, platform);
            }
        }
        else
        {
            throw new InvalidOperationException($"Unexpected migration command {envelope.CommandType}.");
        }
    }

    private void SeedLegacy(string tenantId, ProviderCatalogEntryView entry)
    {
        _legacyHeads[tenantId] = 1;
        _store.Seed(StoreName, ProviderCatalogReadModelAddresses.Detail(tenantId),
            new ProviderCatalogReadModel
            {
                CatalogId = tenantId, TenantId = tenantId, Entries = [entry],
                LastSequenceNumber = 1,
                StreamSequences = new Dictionary<string, long>(StringComparer.Ordinal) { [tenantId] = 1 },
            });
    }

    private static ProviderCatalogEntryView Entry()
        => new("openai", "gpt-4o", "OpenAI GPT-4o", ProviderModelStatus.Enabled,
            SupportsTextGeneration: true, 128_000, 16_000,
            new ProviderModelTimeoutPolicy(30_000, 3), ProviderModelCapabilityFlags.Streaming,
            ProviderConfigurationState.Configured, "cfg-openai-gpt4o", true, 3,
            new ProviderModelPricing("USD", 0.002m, 0.008m, 2),
            new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v2", 2));
}
