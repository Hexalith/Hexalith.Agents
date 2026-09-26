using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;
using Hexalith.Agents.TenantProviderEnablement;

using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Streams;

using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Live projected joins fail selection and activation closed against authoritative stream heads.</summary>
public sealed class ProjectedProviderCatalogReaderTests
{
    [Theory]
    [InlineData("enabled-committed", ProviderCatalogInspectionStatus.Unavailable, 1)]
    [InlineData("enabled-absent", ProviderCatalogInspectionStatus.EntryNotFound, 1)]
    [InlineData("enabled-rejected-create", ProviderCatalogInspectionStatus.EntryNotFound, 1)]
    [InlineData("hidden", ProviderCatalogInspectionStatus.EntryNotFound, 0)]
    [InlineData("absent", ProviderCatalogInspectionStatus.EntryNotFound, 0)]
    [InlineData("never-enabled", ProviderCatalogInspectionStatus.EntryNotFound, 0)]
    public async Task Missing_platform_row_checks_authority_only_for_a_tenant_visible_key(
        string scenario, ProviderCatalogInspectionStatus expected, int expectedHeadReads)
    {
        const string tenantId = "tenant-a";
        const string providerId = "openai";
        const string modelId = "gpt-x";
        const string storeName = "statestore";
        string entryId = ProviderCatalogIdentity.EntryId(providerId, modelId);
        var store = new FakeReadModelStore();
        var platform = new ProviderCatalogReadModel
        {
            CatalogId = ProviderCatalogIdentity.PlatformTenantId,
            TenantId = ProviderCatalogIdentity.PlatformTenantId,
        };
        if (scenario == "enabled-rejected-create")
        {
            // A rejected create advances the stream and its projected checkpoint without adding a row.
            platform.StreamSequences[entryId] = 1;
        }

        store.Seed(storeName, ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId), platform);
        if (scenario != "never-enabled")
        {
            var tenant = new TenantProviderEnablementReadModel();
            if (scenario != "absent")
            {
                tenant.State.Apply(new TenantProviderModelEnablementSet(tenantId, providerId, modelId,
                    scenario != "hidden", 1, "operator", null));
            }
            tenant.LastSequenceNumber = 1;
            store.Seed(storeName, TenantProviderEnablementReadModelAddresses.Detail(tenantId), tenant);
        }

        IAgentAdministrationContextProvider context = Substitute.For<IAgentAdministrationContextProvider>();
        context.GetContext().Returns(new AgentAdministrationContext(tenantId, "admin", IsAgentsAdmin: true));
        IEventStoreGatewayClient gateway = Substitute.For<IEventStoreGatewayClient>();
        gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                StreamReadRequest request = call.Arg<StreamReadRequest>();
                bool tenantStream = request.Domain == TenantProviderEnablementAggregate.Domain;
                if (tenantStream ? scenario == "never-enabled" : scenario == "enabled-absent")
                {
                    // EventStore reports a stream that was never written as 404 missing-stream, not head 0.
                    throw new EventStoreGatewayException(404, "Not Found",
                        reasonCode: StreamReplayReasonCodes.MissingStream);
                }

                return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                    new StreamReadMetadata(0, null, null, 1, 0, false, null));
            });
        var reader = new ProjectedProviderCatalogReader(store,
            Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = storeName }), context, gateway);

        ProviderCatalogEntryReadResult result = await reader.GetEntryAsync(tenantId, providerId, modelId,
            CancellationToken.None);

        result.Status.ShouldBe(expected);
        await gateway.Received(expectedHeadReads).ReadStreamAsync(Arg.Is<StreamReadRequest>(request =>
            request.Domain == ProviderCatalogAggregate.Domain), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Unprojected_tenant_enablement_remains_unavailable_before_hidden_key_check()
    {
        const string tenantId = "tenant-a";
        const string storeName = "statestore";
        var store = new FakeReadModelStore();
        var projectedTenant = new TenantProviderEnablementReadModel { LastSequenceNumber = 1 };
        projectedTenant.State.Apply(new TenantProviderModelEnablementSet(tenantId, "other", "model", true,
            1, "operator", null));
        store.Seed(storeName, TenantProviderEnablementReadModelAddresses.Detail(tenantId), projectedTenant);
        IAgentAdministrationContextProvider context = Substitute.For<IAgentAdministrationContextProvider>();
        context.GetContext().Returns(new AgentAdministrationContext(tenantId, "admin", IsAgentsAdmin: true));
        IEventStoreGatewayClient gateway = Substitute.For<IEventStoreGatewayClient>();
        gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                StreamReadRequest request = call.Arg<StreamReadRequest>();
                return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                    new StreamReadMetadata(0, null, null, 2, 0, false, null));
            });
        var reader = new ProjectedProviderCatalogReader(store,
            Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = storeName }), context, gateway);

        ProviderCatalogEntryReadResult result = await reader.GetEntryAsync(tenantId, "openai", "gpt-x",
            CancellationToken.None);

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.Unavailable);
        await gateway.Received(1).ReadStreamAsync(Arg.Is<StreamReadRequest>(request =>
            request.Domain == TenantProviderEnablementAggregate.Domain), Arg.Any<CancellationToken>());
        await gateway.DidNotReceive().ReadStreamAsync(Arg.Is<StreamReadRequest>(request =>
            request.Domain == ProviderCatalogAggregate.Domain), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Grace_expiring_during_authoritative_reads_is_blocked_at_selection_time()
    {
        const string tenantId = "tenant-a";
        const string providerId = "openai";
        const string modelId = "gpt-x";
        const string storeName = "statestore";
        string entryId = ProviderCatalogIdentity.EntryId(providerId, modelId);
        DateTimeOffset now = new(2026, 9, 24, 12, 0, 0, TimeSpan.Zero);
        DateTimeOffset evaluatedAt = now;
        TimeProvider clock = Substitute.For<TimeProvider>();
        clock.GetUtcNow().Returns(_ => evaluatedAt);
        var accepted = new ProviderDataHandlingRecord(30, true, ["EU", "US"], "terms-v1", 1,
            now.AddDays(-40));
        var tightening = new ProviderDataHandlingRecord(14, false, ["EU"], "terms-v1", 2,
            now.AddDays(-30).AddSeconds(1));
        tightening = tightening with
        {
            TighteningDeclaration = ProviderDataHandlingPolicy.DeclareTightening(accepted, tightening, "operator"),
        };
        var platform = new ProviderCatalogReadModel
        {
            CatalogId = ProviderCatalogIdentity.PlatformTenantId,
            TenantId = ProviderCatalogIdentity.PlatformTenantId,
            Entries = [new ProviderCatalogEntryView(providerId, modelId, "GPT-X", ProviderModelStatus.Enabled,
                true, 128_000, 16_000, new ProviderModelTimeoutPolicy(30_000, 1),
                ProviderModelCapabilityFlags.Streaming, ProviderConfigurationState.Configured, "cfg-ref",
                true, 1, new ProviderModelPricing("USD", 0.001m, 0.002m, 1), tightening,
                DataHandlingHistory: [accepted, tightening])],
            StreamSequences = new Dictionary<string, long>(StringComparer.Ordinal) { [entryId] = 1 },
        };
        var tenant = new TenantProviderEnablementReadModel { LastSequenceNumber = 2 };
        tenant.State.Apply(new TenantProviderModelEnablementSet(tenantId, providerId, modelId, true, 1,
            "operator", null));
        tenant.State.Apply(new ProviderDataHandlingDecided(tenantId, providerId, modelId, true,
            "Reviewed", 2, "admin", "TenantAgentAdministrator", accepted, now.AddDays(-39)));
        var store = new FakeReadModelStore();
        store.Seed(storeName, ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId), platform);
        store.Seed(storeName, TenantProviderEnablementReadModelAddresses.Detail(tenantId), tenant);
        IAgentAdministrationContextProvider context = Substitute.For<IAgentAdministrationContextProvider>();
        context.GetContext().Returns(new AgentAdministrationContext(tenantId, "admin", IsAgentsAdmin: true));
        IEventStoreGatewayClient gateway = Substitute.For<IEventStoreGatewayClient>();
        gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                StreamReadRequest request = call.Arg<StreamReadRequest>();
                if (request.Domain == TenantProviderEnablementAggregate.Domain)
                {
                    evaluatedAt = now.AddSeconds(2);
                }

                return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                    new StreamReadMetadata(0, null, null,
                        request.Domain == TenantProviderEnablementAggregate.Domain ? 2 : 1, 0, false, null));
            });
        var reader = new ProjectedProviderCatalogReader(store,
            Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = storeName }), context, gateway, clock);

        ProviderCatalogEntryReadResult result = await reader.GetEntryAsync(tenantId, providerId, modelId,
            CancellationToken.None);

        result.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
        result.Entry.ShouldNotBeNull().IsSelectableForNewActiveUse.ShouldBeFalse();
    }

    [Fact]
    public void Missing_join_projection_remains_pending_even_for_an_entry_lookup()
    {
        var tenant = new TenantProviderEnablementReadModel();

        TenantProviderCatalogInspectionResult missingPlatform = TenantProviderCatalogViewFactory.CreateEntry(
            null, tenant, authorized: true, "openai", "gpt-x", DateTimeOffset.UtcNow);
        TenantProviderCatalogInspectionResult missingTenant = TenantProviderCatalogViewFactory.CreateEntry(
            new ProviderCatalogReadModel(), null, authorized: true, "openai", "gpt-x", DateTimeOffset.UtcNow);

        missingPlatform.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
        missingPlatform.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        missingTenant.Status.ShouldBe(ProviderCatalogInspectionStatus.Success);
        missingTenant.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
    }

    [Fact]
    public void Missing_platform_projection_is_pending_for_operator_list_and_detail_without_expected_version()
    {
        ProviderCatalogInspectionResult list = ProviderCatalogViewFactory.CreateList(
            null, ProviderCatalogIdentity.PlatformTenantId, includeDisabled: true,
            expectedProjectionVersion: null, isProviderAdmin: true);
        ProviderCatalogInspectionResult detail = ProviderCatalogViewFactory.CreateEntry(
            null, ProviderCatalogIdentity.PlatformTenantId, "openai", "gpt-x",
            expectedCapabilityVersion: null, isProviderAdmin: true);

        list.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        detail.TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);
        list.Freshness.ShouldBe(AgentSetupFreshness.Stale);
        detail.Freshness.ShouldBe(AgentSetupFreshness.Stale);
    }

    [Theory]
    [InlineData("unaccepted", ProviderSelectionValidationStatus.Unavailable)]
    [InlineData("expired", ProviderSelectionValidationStatus.Unavailable)]
    [InlineData("platform-ahead", ProviderSelectionValidationStatus.Unavailable)]
    [InlineData("tenant-disable-ahead", ProviderSelectionValidationStatus.Unavailable)]
    [InlineData("tenant-decline-ahead", ProviderSelectionValidationStatus.Unavailable)]
    [InlineData("current", ProviderSelectionValidationStatus.Valid)]
    public async Task Selection_and_activation_use_tenant_terms_and_authoritative_platform_freshness(
        string scenario, ProviderSelectionValidationStatus expected)
    {
        const string tenantId = "tenant-a";
        const string providerId = "openai";
        const string modelId = "gpt-x";
        const string storeName = "statestore";
        string aggregateId = ProviderCatalogIdentity.EntryId(providerId, modelId);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        var acceptedTerms = new ProviderDataHandlingRecord(30, true, ["EU", "US"], "terms-v1", 1, now.AddDays(-40));
        ProviderDataHandlingRecord currentTerms = acceptedTerms;
        if (scenario == "expired")
        {
            var next = new ProviderDataHandlingRecord(14, false, ["EU"], "terms-v1", 2, now.AddDays(-31));
            currentTerms = next with
            {
                TighteningDeclaration = ProviderDataHandlingPolicy.DeclareTightening(acceptedTerms, next, "operator"),
            };
        }

        var platform = new ProviderCatalogReadModel
        {
            CatalogId = ProviderCatalogIdentity.PlatformTenantId,
            TenantId = ProviderCatalogIdentity.PlatformTenantId,
            Entries = [new ProviderCatalogEntryView(providerId, modelId, "GPT-X", ProviderModelStatus.Enabled,
                true, 128_000, 16_000, new ProviderModelTimeoutPolicy(30_000, 1),
                ProviderModelCapabilityFlags.Streaming, ProviderConfigurationState.Configured, "cfg-ref",
                true, 1, new ProviderModelPricing("USD", 0.001m, 0.002m, 1),
                currentTerms, DataHandlingHistory: scenario == "expired" ? [acceptedTerms, currentTerms] : [acceptedTerms])],
            StreamSequences = new Dictionary<string, long>(StringComparer.Ordinal) { [aggregateId] = 1 },
        };
        var tenant = new TenantProviderEnablementReadModel();
        tenant.State.Apply(new TenantProviderModelEnablementSet(tenantId, providerId, modelId, true, 1, "operator", null));
        if (scenario != "unaccepted")
        {
            tenant.State.Apply(new ProviderDataHandlingDecided(tenantId, providerId, modelId, true,
                "Reviewed", 2, "admin", "TenantAgentAdministrator", acceptedTerms, now.AddDays(-39)));
        }
        tenant.LastSequenceNumber = scenario == "unaccepted" ? 1 : 2;

        var store = new FakeReadModelStore();
        store.Seed(storeName, ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId), platform);
        store.Seed(storeName, TenantProviderEnablementReadModelAddresses.Detail(tenantId), tenant);
        IAgentAdministrationContextProvider context = Substitute.For<IAgentAdministrationContextProvider>();
        context.GetContext().Returns(new AgentAdministrationContext(tenantId, "admin", IsAgentsAdmin: true));
        IEventStoreGatewayClient gateway = Substitute.For<IEventStoreGatewayClient>();
        gateway.ReadStreamAsync(Arg.Any<StreamReadRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                StreamReadRequest request = call.Arg<StreamReadRequest>();
                long latest = request.Domain == TenantProviderEnablementAggregate.Domain
                    ? tenant.LastSequenceNumber + (scenario is "tenant-disable-ahead" or "tenant-decline-ahead" ? 1 : 0)
                    : scenario == "platform-ahead" ? 2 : 1;
                return new StreamReadPage(request.Tenant, request.Domain, request.AggregateId, [],
                    new StreamReadMetadata(0, null, null, latest, 0, false, null));
            });
        var reader = new ProjectedProviderCatalogReader(store,
            Options.Create(new ProviderCatalogReadModelOptions { StateStoreName = storeName }), context, gateway);
        IAgentCommandDispatcher dispatcher = Substitute.For<IAgentCommandDispatcher>();
        dispatcher.DispatchAsync(Arg.Any<CommandEnvelope>(), Arg.Any<CancellationToken>())
            .Returns(new SubmitCommandResponse("corr", null, "msg"));

        AgentProviderSelectionOutcome selection = await new AgentProviderSelectionOrchestrator(reader, dispatcher)
            .ExecuteAsync(new AgentProviderSelectionRequest("msg", "corr", tenantId, "hexa", "admin", true,
                providerId, modelId), CancellationToken.None);
        AgentActivationRevalidationOutcome activation = await new AgentActivationProviderRevalidation(reader,
            Substitute.For<IApproverPolicyResolver>(), dispatcher)
            .ExecuteAsync(new AgentActivationRevalidationRequest("msg-2", "corr", tenantId, "hexa", "admin", true,
                1, providerId, modelId, AgentResponseMode.Automatic), CancellationToken.None);

        ITenantAccessReader tenantAccess = Substitute.For<ITenantAccessReader>();
        tenantAccess.ReadAsync(tenantId, Arg.Any<string>(), "caller-party", Arg.Any<CancellationToken>())
            .Returns(new TenantAccessReadResult(AgentInteractionGateOutcome.Satisfied, IsFresh: true));
        IConversationAccessReader conversationAccess = Substitute.For<IConversationAccessReader>();
        conversationAccess.ReadAsync(tenantId, "conversation", "caller-party", Arg.Any<CancellationToken>())
            .Returns(new ConversationAccessReadResult(AgentInteractionGateOutcome.Satisfied, IsFresh: true));
        IAgentInvocationReadinessReader readiness = Substitute.For<IAgentInvocationReadinessReader>();
        readiness.ReadAsync(tenantId, "hexa", Arg.Any<CancellationToken>())
            .Returns(new AgentInvocationReadiness(true, AgentLifecycleStatus.Active, true, "agent-party",
                AgentResponseMode.Automatic, true, providerId, modelId, null, true, true));
        IAgentPartyDirectory parties = Substitute.For<IAgentPartyDirectory>();
        parties.ValidateExistingPartyAsync(tenantId, Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new AgentPartyValidationResult(PartyLinkValidationStatus.Valid, "agent-party"));
        AgentInteractionGateOutcomeResult interaction = await new AgentInteractionGateOrchestrator(
            tenantAccess, conversationAccess, readiness, parties, reader,
            Substitute.For<IApproverPolicyResolver>(), dispatcher)
            .ExecuteAsync(new AgentInteractionGateRequest("msg-3", "corr", tenantId, "interaction", "hexa",
                "caller", "caller-party", "conversation", providerId, modelId,
                AgentResponseMode.Automatic, "client-corr", null), CancellationToken.None);

        selection.Verdict.ShouldBe(expected);
        activation.ProviderVerdict.ShouldBe(expected);
        interaction.Status.ShouldBe(scenario == "current"
            ? AgentInteractionStatus.Authorized : AgentInteractionStatus.Blocked);
        if (scenario is "platform-ahead" or "tenant-disable-ahead" or "tenant-decline-ahead")
        {
            (await reader.GetEntryAsync(tenantId, providerId, modelId, CancellationToken.None))
                .Status.ShouldBe(ProviderCatalogInspectionStatus.Unavailable);
        }
    }
}
