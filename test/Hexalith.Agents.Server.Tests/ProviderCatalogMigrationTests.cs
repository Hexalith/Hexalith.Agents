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
    [InlineData("missing-inventory", "InvalidLegacyInventory")]
    public async Task Invalid_migration_matrix_rejects_every_row_before_dispatch(string scenario, string expected)
    {
        if (scenario != "missing-inventory")
        {
            SeedLegacy("tenant-a", scenario == "missing-terms" ? Entry() with { DataHandling = null } : Entry());
        }

        if (scenario == "divergent-legacy")
        {
            SeedLegacy("tenant-b", Entry() with { DisplayLabel = "Different" });
        }
        else if (scenario == "divergent-target")
        {
            _store.Seed(StoreName, ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId),
                new ProviderCatalogReadModel
                {
                    CatalogId = ProviderCatalogIdentity.PlatformTenantId,
                    TenantId = ProviderCatalogIdentity.PlatformTenantId,
                    Entries = [Entry() with { DisplayLabel = "Different", MigratedFrom =
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
        IAgentCommandDispatcher dispatcher = Substitute.For<IAgentCommandDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                CommandEnvelope envelope = call.Arg<CommandEnvelope>();
                _sent.Add(envelope);
                Persist(envelope);
                return Task.FromResult(new SubmitCommandResponse("corr-1", null, "msg-1"));
            });
        IAgentAdministrationContextProvider context = Substitute.For<IAgentAdministrationContextProvider>();
        context.GetContext().Returns(new AgentAdministrationContext(
            "tenant-a", "operator", IsAgentsAdmin: false, IsPlatformOperator: isPlatformOperator));
        return new(new ProviderCatalogAdministrationOrchestrator(dispatcher), context,
            new AgentCommandIdentityFactory(), _store,
            Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName }));
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
            _store.Seed(StoreName, key, model);
        }
        else if (envelope.CommandType == nameof(EnableProviderModelEntry))
        {
            EnableProviderModelEntry command = JsonSerializer.Deserialize<EnableProviderModelEntry>(envelope.Payload).ShouldNotBeNull();
            string key = ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId);
            ProviderCatalogReadModel model = _store.Snapshot<ProviderCatalogReadModel>(StoreName, key).ShouldNotBeNull();
            ProviderCatalogEntryView current = model.Entries.Single(item => item.ProviderId == command.ProviderId && item.ModelId == command.ModelId);
            model.Entries.Remove(current);
            model.Entries.Add(current with { Status = ProviderModelStatus.Enabled, IsSelectableForNewActiveUse = true });
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
            _store.Seed(StoreName, key, model);
        }
        else
        {
            throw new InvalidOperationException($"Unexpected migration command {envelope.CommandType}.");
        }
    }

    private void SeedLegacy(string tenantId, ProviderCatalogEntryView entry)
        => _store.Seed(StoreName, ProviderCatalogReadModelAddresses.Detail(tenantId),
            new ProviderCatalogReadModel { CatalogId = tenantId, TenantId = tenantId, Entries = [entry] });

    private static ProviderCatalogEntryView Entry()
        => new("openai", "gpt-4o", "OpenAI GPT-4o", ProviderModelStatus.Enabled,
            SupportsTextGeneration: true, 128_000, 16_000,
            new ProviderModelTimeoutPolicy(30_000, 3), ProviderModelCapabilityFlags.Streaming,
            ProviderConfigurationState.Configured, "cfg-openai-gpt4o", true, 3,
            new ProviderModelPricing("USD", 0.002m, 0.008m, 2),
            new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v2", 2));
}
