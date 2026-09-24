using System.Text.Json;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Queries;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.Server.Application.Queries;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;
using Hexalith.Agents.TenantProviderEnablement;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Queries;
using Hexalith.EventStore.Contracts.Streams;
using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Persisted platform and tenant projection join, including safe disclosure.</summary>
public sealed class TenantProviderCatalogIntegrationTests
{
    private const string StoreName = "statestore";
    private readonly FakeReadModelStore _store = new();

    [Fact]
    public async Task Skipped_tenant_event_does_not_advance_checkpoint_or_claim_current_truth()
    {
        var handler = new TenantProviderEnablementProjectionHandler(_store, _store, Options());
        var enabled = new TenantProviderModelEnablementSet("tenant-a", "openai", "gpt-4o",
            true, 1, "operator", null);
        (await handler.ProjectAsync(new ProjectionRequest("tenant-a", "tenant-provider-enablement", "tenant-a",
            [Event(nameof(TenantProviderModelEnablementSet), 1, enabled)]),
            "initial", CancellationToken.None)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        var disabled = new TenantProviderModelEnablementSet("tenant-a", "openai", "gpt-4o",
            false, 3, "operator", null);

        DomainProjectionHandlerResult gap = await handler.ProjectAsync(
            new ProjectionRequest("tenant-a", "tenant-provider-enablement", "tenant-a",
                [Event(nameof(TenantProviderModelEnablementSet), 3, disabled)]),
            "gap", CancellationToken.None);

        gap.Status.ShouldBe(ProjectionDispatchStatus.Retryable);
        TenantProviderEnablementReadModel projected = _store.Snapshot<TenantProviderEnablementReadModel>(
            StoreName, TenantProviderEnablementReadModelAddresses.Detail("tenant-a")).ShouldNotBeNull();
        projected.LastSequenceNumber.ShouldBe(1);
        projected.State.Entries.ShouldHaveSingleItem().Value.Enabled.ShouldBeTrue();
        IEventStoreGatewayClient gateway = Substitute.For<IEventStoreGatewayClient>();
        gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                StreamReadRequest request = call.Arg<StreamReadRequest>();
                return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                    new StreamReadMetadata(0, null, null,
                        request.Domain == TenantProviderEnablementAggregate.Domain ? 3 : 1,
                        0, false, null));
            });
        bool current = await ProviderCatalogReadFreshness.IsTenantCurrentAsync(gateway, "tenant-a",
            platform: null, projected, "openai", "gpt-4o", CancellationToken.None);
        current.ShouldBeFalse();

        string entryId = ProviderCatalogIdentity.EntryId("openai", "gpt-4o");
        var terms = new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1);
        var created = new ProviderModelEntryCreated(entryId, "openai", "gpt-4o", "GPT-4o", true,
            true, 128000, 16000, new ProviderModelTimeoutPolicy(30000, 3),
            ProviderModelCapabilityFlags.Streaming, ProviderConfigurationState.Configured,
            "cfg-ref", new ProviderModelPricing("USD", 0.002m, 0.008m, 1), 1, terms);
        var platformHandler = new ProviderCatalogProjectionHandler(_store, _store, Options());
        (await platformHandler.ProjectAsync(new ProjectionRequest(ProviderCatalogIdentity.PlatformTenantId,
            ProviderCatalogAggregate.Domain, entryId,
            [Event(nameof(ProviderModelEntryCreated), 1, created)]),
            "platform", CancellationToken.None)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        ITenantAccessReader access = Substitute.For<ITenantAccessReader>();
        access.ReadAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, IsFresh: true));
        IAgentAdministrationContextProvider context = Substitute.For<IAgentAdministrationContextProvider>();
        var queryHandler = new ListProviderCatalogEntriesQueryHandler(_store, Options(), access, context, gateway);
        QueryEnvelope query = new("tenant-a", ProviderCatalogReadModelAddresses.Domain, entryId,
            ListProviderCatalogEntriesQuery.QueryType,
            JsonSerializer.SerializeToUtf8Bytes(new ListProviderCatalogEntriesQuery(false)),
            "corr", "admin", isGlobalAdmin: false);

        QueryResult read = await queryHandler.ExecuteAsync(query, CancellationToken.None);
        using JsonDocument payload = JsonDocument.Parse(read.PayloadBytes.ShouldNotBeNull());
        payload.RootElement.GetProperty("truthState").GetString().ShouldBe("AuthoritativePending");
        payload.RootElement.GetProperty("entries").GetArrayLength().ShouldBe(0);
    }

    [Fact]
    public async Task Tenant_projection_keeps_only_recent_successful_command_ids()
    {
        ProjectionEventDto[] events = [.. Enumerable.Range(1, ProjectedCommandIdentityWindow.Capacity + 2)
            .Select(sequence => Event(nameof(TenantProviderModelEnablementSet), sequence,
                new TenantProviderModelEnablementSet("tenant-a", "openai", "gpt-4o", true,
                    sequence, "operator", null)))];
        var handler = new TenantProviderEnablementProjectionHandler(_store, _store, Options());

        DomainProjectionHandlerResult result = await handler.ProjectAsync(
            new ProjectionRequest("tenant-a", "tenant-provider-enablement", "tenant-a", events),
            "tenant-window", CancellationToken.None);

        result.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        TenantProviderEnablementReadModel projected = _store.Snapshot<TenantProviderEnablementReadModel>(
            StoreName, TenantProviderEnablementReadModelAddresses.Detail("tenant-a")).ShouldNotBeNull();
        projected.ProjectedCommandMessageIds.Count.ShouldBe(ProjectedCommandIdentityWindow.Capacity);
        projected.ProjectedCommandMessageIds.ShouldNotContain("msg-1");
        projected.ProjectedCommandMessageIds.ShouldNotContain("msg-2");
        projected.ProjectedCommandMessageIds.ShouldContain($"msg-{ProjectedCommandIdentityWindow.Capacity + 2}");
        projected.LastSequenceNumber.ShouldBe(ProjectedCommandIdentityWindow.Capacity + 2);
    }

    [Fact]
    public async Task Persisted_enablement_and_decision_join_only_for_the_enabled_tenant_without_configuration()
    {
        DateTimeOffset now = new(2026, 9, 23, 0, 0, 0, TimeSpan.Zero);
        var terms = new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1, now);
        string entryId = ProviderCatalogIdentity.EntryId("openai", "gpt-4o");
        var platformHandler = new ProviderCatalogProjectionHandler(_store, _store, Options());
        var platformEvent = new ProviderModelEntryCreated(entryId, "openai", "gpt-4o", "GPT-4o", true, true,
            128000, 16000, new ProviderModelTimeoutPolicy(30000, 3), ProviderModelCapabilityFlags.Streaming,
            ProviderConfigurationState.Configured, "cfg-safe-ref", new ProviderModelPricing("USD", 0.002m, 0.008m, 1),
            1, terms);
        DomainProjectionHandlerResult platformResult = await platformHandler.ProjectAsync(
            new ProjectionRequest(ProviderCatalogIdentity.PlatformTenantId, ProviderCatalogAggregate.Domain, entryId,
                [Event(nameof(ProviderModelEntryCreated), 1, platformEvent)]), "platform-dispatch", CancellationToken.None);
        platformResult.Status.ShouldBe(ProjectionDispatchStatus.Completed);

        var tenantHandler = new TenantProviderEnablementProjectionHandler(_store, _store, Options());
        var enabled = new TenantProviderModelEnablementSet("tenant-a", "openai", "gpt-4o", true, 1, "operator", null);
        var accepted = new ProviderDataHandlingDecided("tenant-a", "openai", "gpt-4o", true, "approved", 2,
            "admin", "TenantAgentAdministrator", terms, now);
        DomainProjectionHandlerResult tenantResult = await tenantHandler.ProjectAsync(
            new ProjectionRequest("tenant-a", "tenant-provider-enablement", "tenant-a",
                [Event(nameof(TenantProviderModelEnablementSet), 1, enabled), Event(nameof(ProviderDataHandlingDecided), 2, accepted)]),
            "tenant-dispatch", CancellationToken.None);
        tenantResult.Status.ShouldBe(ProjectionDispatchStatus.Completed);

        ProviderCatalogReadModel platform = _store.Snapshot<ProviderCatalogReadModel>(StoreName,
            ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId)).ShouldNotBeNull();
        TenantProviderEnablementReadModel tenant = _store.Snapshot<TenantProviderEnablementReadModel>(StoreName,
            TenantProviderEnablementReadModelAddresses.Detail("tenant-a")).ShouldNotBeNull();
        tenant.State.Revision.ShouldBe(2);
        tenant.State.Entries.ShouldHaveSingleItem().Value.AcceptedTerms!.DataHandlingVersion.ShouldBe(1);
        tenant.State.Entries.ShouldHaveSingleItem().Value.LastEnablementMessageId.ShouldBe("msg-1");
        tenant.State.Entries.ShouldHaveSingleItem().Value.LastDecisionMessageId.ShouldBe("msg-2");

        TenantProviderCatalogInspectionResult joined = TenantProviderCatalogViewFactory.CreateList(platform, tenant,
            authorized: true, now);
        joined.Entries.ShouldHaveSingleItem().IsSelectableForNewActiveUse.ShouldBeTrue();
        joined.Entries.ShouldHaveSingleItem().LastDecisionMessageId.ShouldBe("msg-2");
        joined.TruthState.ShouldBe(Hexalith.Agents.Contracts.Agent.AgentSetupTruthState.ProjectionConfirmed);
        string json = JsonSerializer.Serialize(joined);
        json.ShouldNotContain("cfg-safe-ref");
        json.ShouldNotContain("configurationReferenceId", Case.Insensitive);
        json.ShouldNotContain("configurationState", Case.Insensitive);
        TenantProviderCatalogViewFactory.CreateEntry(platform, null, true, "openai", "gpt-4o", now).Status
            .ShouldBe(ProviderCatalogInspectionStatus.Success);
        TenantProviderCatalogViewFactory.CreateEntry(platform, tenant, true, "openai", "missing", now).Status
            .ShouldBe(ProviderCatalogInspectionStatus.EntryNotFound);

        DomainProjectionHandlerResult disableResult = await platformHandler.ProjectAsync(
            new ProjectionRequest(ProviderCatalogIdentity.PlatformTenantId, ProviderCatalogAggregate.Domain, entryId,
                [Event(nameof(ProviderModelEntryDisabled), 2,
                    new ProviderModelEntryDisabled(entryId, "openai", "gpt-4o"))]),
            "platform-disable", CancellationToken.None);
        disableResult.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        ProviderCatalogReadModel disabledPlatform = _store.Snapshot<ProviderCatalogReadModel>(StoreName,
            ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId)).ShouldNotBeNull();
        TenantProviderCatalogViewFactory.CreateList(disabledPlatform, tenant, true, now)
            .Entries.ShouldBeEmpty();
        var disabled = TenantProviderCatalogViewFactory.CreateEntry(disabledPlatform, tenant, true, "openai", "gpt-4o", now);
        var absent = TenantProviderCatalogViewFactory.CreateEntry(disabledPlatform, tenant, true, "openai", "missing", now);
        disabled.Entries.ShouldHaveSingleItem().DataHandlingStatus.ShouldBe("PlatformNotReady");
        absent.Status.ShouldBe(ProviderCatalogInspectionStatus.EntryNotFound);

        DomainProjectionHandlerResult tenantDisableResult = await tenantHandler.ProjectAsync(
            new ProjectionRequest("tenant-a", "tenant-provider-enablement", "tenant-a",
                [Event(nameof(TenantProviderModelEnablementSet), 3,
                    new TenantProviderModelEnablementSet("tenant-a", "openai", "gpt-4o", false, 3, "operator", null))]),
            "tenant-disable", CancellationToken.None);
        tenantDisableResult.Status.ShouldBe(ProjectionDispatchStatus.Completed);
        TenantProviderEnablementReadModel disabledTenant = _store.Snapshot<TenantProviderEnablementReadModel>(StoreName,
            TenantProviderEnablementReadModelAddresses.Detail("tenant-a")).ShouldNotBeNull();
        var tenantDisabled = TenantProviderCatalogViewFactory.CreateEntry(disabledPlatform, disabledTenant, true, "openai", "gpt-4o", now);
        var tenantAbsent = TenantProviderCatalogViewFactory.CreateEntry(disabledPlatform, disabledTenant, true, "openai", "missing", now);
        tenantDisabled.Status.ShouldBe(ProviderCatalogInspectionStatus.EntryNotFound);
        JsonSerializer.Serialize(tenantDisabled).ShouldBe(JsonSerializer.Serialize(tenantAbsent));
    }

    private static IOptions<ProviderCatalogReadModelOptions> Options()
        => Microsoft.Extensions.Options.Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = StoreName });

    private static ProjectionEventDto Event<T>(string name, long sequence, T payload)
        => new(name, JsonSerializer.SerializeToUtf8Bytes(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web)),
            "json", sequence, new DateTimeOffset(2026, 9, 23, 0, 0, 0, TimeSpan.Zero).AddSeconds(sequence), "corr-1",
            MessageId: $"msg-{sequence}");
}
