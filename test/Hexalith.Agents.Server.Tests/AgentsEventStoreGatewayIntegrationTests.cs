namespace Hexalith.Agents.Server.Tests;

using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Dapr.Actors;
using Dapr.Actors.Client;

using Hexalith.Agents.Client;
using Hexalith.Agents.Agent;
using Hexalith.Agents.AgentInteraction;
using Hexalith.Agents.ConversationAgentState;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Agent.Events;
using Hexalith.Agents.Contracts.AgentInteraction;
using Hexalith.Agents.Contracts.AgentInteraction.Commands;
using Hexalith.Agents.Contracts.ConversationAgentState;
using Hexalith.Agents.Contracts.ConversationAgentState.Commands;
using Hexalith.Agents.Contracts.Security;
using System.Text.Json.Nodes;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.EventStore;
using Hexalith.Agents.TenantProviderEnablement;
using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;
using Hexalith.Agents.Server.Composition;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Authentication;
using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Configuration;
using Hexalith.EventStore.Controllers;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.Contracts.Results;
using Hexalith.EventStore.Contracts.Serialization;
#if HEXALITH_EVENTSTORE_SOURCE
using Hexalith.EventStore.Contracts.Routing;
#endif
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.Server.Configuration;
using Hexalith.EventStore.Server.Pipeline;
using Hexalith.EventStore.Server.Pipeline.Commands;
#if HEXALITH_EVENTSTORE_SOURCE
using Microsoft.AspNetCore.Authentication;
using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.EventStore.ServiceDefaults.Authentication;
using Hexalith.EventStore.Server.Projections;
using Hexalith.EventStore.ServiceDefaults.Routing;
#endif
using Hexalith.EventStore.Validation;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

/// <summary>
/// Split-provider gateway integration tests for the explicit Agents EventStore package.
/// </summary>
public sealed class AgentsEventStoreGatewayIntegrationTests
{
    private const string AgentsAppId = "agents";

    [Fact]
    public void Gateway_registration_is_explicit_idempotent_and_owns_setup_and_governance_adapters()
    {
        ServiceCollection gatewayServices = new();
        _ = gatewayServices.AddAgentsEventStore(AgentsAppId, new FixtureTrustedCommandProofReader());
        _ = gatewayServices.AddAgentsEventStore(AgentsAppId, new FixtureTrustedCommandProofReader());
        using ServiceProvider gateway = gatewayServices.BuildServiceProvider();

        string[] commandTypes = gateway.GetServices<IIdempotencyIntentAdapter>()
            .Select(adapter => adapter.CommandType)
            .Order(StringComparer.Ordinal)
            .ToArray();
        commandTypes.ShouldBe(
        [
            "AcknowledgeInteractionCreation",
            "AcquireConversationEffectLease",
            nameof(ActivateAgent),
            "CancelConversationEffectLease",
            "CommitConversationEffect",
            nameof(ConfigureAgentResponseMode),
            nameof(CreateAgent),
            "CreatePermittedAgentInteraction",
            nameof(CreateProviderModelEntry),
            nameof(DecideProviderDataHandling),
            nameof(DisableAgent),
            nameof(DisableProviderModelEntry),
            nameof(EnableProviderModelEntry),
            "EnterConversationDeletionClosing",
            "RecordAutomaticInteractionStep",
            "RegisterInteractionPermit",
            nameof(SetTenantProviderModelEnablement),
            "SettleConversationEffectLease",
            nameof(UpdateAgentConfiguration),
            nameof(UpdateProviderModelEntry),
        ]);
        gateway.GetServices<ITrustedCommandExtensionPolicy>().Count().ShouldBe(1);
        gateway.GetServices<ICoordinatedCommandPolicy>().Count().ShouldBe(1);

        // This deliberately represents the independently composed Agents process. Exercise its real setup
        // composition so an accidental Server-side registration of either gateway-owned seam is observable here.
        ServiceCollection agentsServices = new();
        agentsServices.AddSingleton<IAgentCommandDispatcher, DeferredAgentCommandDispatcher>();
        agentsServices.AddSingleton(AgentsClient.Unavailable());
        IConfiguration agentsConfiguration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Agents:EventStore:BaseUrl"] = "https://eventstore.example",
                ["Agents:EventStore:AppId"] = "eventstore",
            })
            .Build();
        _ = agentsServices.AddAgentSetupServices(agentsConfiguration);
        using ServiceProvider agents = agentsServices.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = false,
            ValidateScopes = true,
        });
        agents.GetRequiredService<IAgentCommandDispatcher>()
            .ShouldBeOfType<EventStoreAgentCommandDispatcher>();
        agents.GetServices<IIdempotencyIntentAdapter>().ShouldBeEmpty();
        agents.GetServices<ITrustedCommandExtensionPolicy>().ShouldBeEmpty();
        agents.GetServices<ICoordinatedCommandPolicy>().ShouldBeEmpty();
    }

    [Fact]
    public async Task Di_resolved_router_sends_a_claimed_governance_command_to_the_coordinator()
    {
        var accepted = new CommandProcessingResult(true, CorrelationId: "correlation");
        ICoordinatedCommandActor coordinator = Substitute.For<ICoordinatedCommandActor>();
        coordinator.ProcessCommandAsync(Arg.Any<CommandEnvelope>()).Returns(accepted);
        IActorProxyFactory factory = Substitute.For<IActorProxyFactory>();
        factory.CreateActorProxy<ICoordinatedCommandActor>(Arg.Any<ActorId>(), CoordinatedCommandActor.ActorTypeName)
            .Returns(coordinator);
        ServiceCollection services = new();
        _ = services.AddAgentsEventStore(AgentsAppId, new FixtureTrustedCommandProofReader());
        services.AddLogging();
        services.AddSingleton(factory);
        services.AddSingleton(Options.Create(new EventStoreActorOptions()));
#if HEXALITH_EVENTSTORE_SOURCE
        services.AddSingleton<IRouteCatalogAuthority>(new FixtureAgentRouteCatalog());
#endif
        services.AddSingleton<ICommandRouter, CommandRouter>();
        using ServiceProvider provider = services.BuildServiceProvider();
        DateTimeOffset now = new(2026, 9, 23, 0, 0, 0, TimeSpan.Zero);
        var terms = new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1, now);
        var decision = new DecideProviderDataHandling("provider", "model", 1, true, "approved", 1, terms, now);
        var command = new SubmitCommand("msg", "tenant-a", "tenant-provider-enablement", "tenant-a",
            nameof(DecideProviderDataHandling), JsonSerializer.SerializeToUtf8Bytes(decision), "correlation", "user",
            new Dictionary<string, string> { ["actor:tenantAgentAdministrator"] = "true" });

        CommandProcessingResult result = await provider.GetRequiredService<ICommandRouter>().RouteCommandAsync(command);

        result.ShouldBe(accepted);
        string sourceActorId = new Hexalith.EventStore.Contracts.Identity.AggregateIdentity(
            ProviderCatalogIdentity.PlatformTenantId, ProviderCatalogIdentity.Domain,
            ProviderCatalogIdentity.EntryId("provider", "model")).ActorId;
        _ = factory.Received(1).CreateActorProxy<ICoordinatedCommandActor>(
            Arg.Is<ActorId>(id => id.ToString() == sourceActorId), CoordinatedCommandActor.ActorTypeName);
        _ = factory.DidNotReceive().CreateActorProxy<IAggregateActor>(Arg.Any<ActorId>(), Arg.Any<string>());
    }

    [Fact]
    public void Governance_admission_intent_ignores_restamped_decision_time_but_rejects_semantic_change()
    {
        ServiceCollection services = new();
        _ = services.AddAgentsEventStore(AgentsAppId, new FixtureTrustedCommandProofReader());
        using ServiceProvider provider = services.BuildServiceProvider();
        IIdempotencyIntentAdapter adapter = provider.GetServices<IIdempotencyIntentAdapter>()
            .Single(item => item.CommandType == nameof(DecideProviderDataHandling));
        DateTimeOffset now = new(2026, 9, 23, 0, 0, 0, TimeSpan.Zero);
        var terms = new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1, now);
        var decision = new DecideProviderDataHandling("provider", "model", 1, true, "approved", 1, terms, now);
        var original = new IdempotencyIntentCommand(nameof(DecideProviderDataHandling), "tenant-a",
            "tenant-provider-enablement", "tenant-a", JsonSerializer.SerializeToUtf8Bytes(decision),
            new Dictionary<string, string> { ["actor:tenantAgentAdministrator"] = "true" });
        IdempotencyCanonicalIntent first = adapter.CreateIntent(original);
        IdempotencyCanonicalIntent retry = adapter.CreateIntent(original with
        {
            Payload = JsonSerializer.SerializeToUtf8Bytes(decision with { DecidedAt = now.AddMinutes(1) }),
        });
        IdempotencyCanonicalIntent divergent = adapter.CreateIntent(original with
        {
            Payload = JsonSerializer.SerializeToUtf8Bytes(decision with { Justification = "different" }),
        });

        retry.SemanticPayload.SequenceEqual(first.SemanticPayload).ShouldBeTrue();
        divergent.SemanticPayload.SequenceEqual(first.SemanticPayload).ShouldBeFalse();
    }

    [Fact]
    public void Enablement_admission_intent_ignores_server_derived_terms_but_rejects_semantic_change()
    {
        ServiceCollection services = new();
        _ = services.AddAgentsEventStore(AgentsAppId, new FixtureTrustedCommandProofReader());
        using ServiceProvider provider = services.BuildServiceProvider();
        IIdempotencyIntentAdapter adapter = provider.GetServices<IIdempotencyIntentAdapter>()
            .Single(item => item.CommandType == nameof(SetTenantProviderModelEnablement));
        DateTimeOffset now = new(2026, 9, 26, 0, 0, 0, TimeSpan.Zero);
        var termsV1 = new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1, now);
        var termsV2 = new ProviderDataHandlingRecord(40, false, ["EU"], "terms-v2", 2, now.AddMinutes(1));
        var enable = new SetTenantProviderModelEnablement("tenant-a", "provider", "model", true, 0, termsV1);
        var original = new IdempotencyIntentCommand(nameof(SetTenantProviderModelEnablement), "tenant-a",
            "tenant-provider-enablement", "tenant-a", JsonSerializer.SerializeToUtf8Bytes(enable),
            new Dictionary<string, string> { ["actor:platformOperator"] = "true" });
        IdempotencyCanonicalIntent first = adapter.CreateIntent(original);
        IdempotencyCanonicalIntent retry = adapter.CreateIntent(original with
        {
            Payload = JsonSerializer.SerializeToUtf8Bytes(enable with { CurrentTerms = termsV2 }),
        });
        IdempotencyCanonicalIntent divergent = adapter.CreateIntent(original with
        {
            Payload = JsonSerializer.SerializeToUtf8Bytes(enable with { ExpectedRevision = 1 }),
        });

        retry.SemanticPayload.SequenceEqual(first.SemanticPayload).ShouldBeTrue();
        divergent.SemanticPayload.SequenceEqual(first.SemanticPayload).ShouldBeFalse();
    }

    [Theory]
    [InlineData(nameof(CreateProviderModelEntry))]
    [InlineData(nameof(UpdateProviderModelEntry))]
    public void Registered_catalog_admission_intent_ignores_restamped_terms_time_but_rejects_field_change(
        string commandType)
    {
        ServiceCollection services = new();
        _ = services.AddAgentsEventStore(AgentsAppId, new FixtureTrustedCommandProofReader());
        using ServiceProvider provider = services.BuildServiceProvider();
        IIdempotencyIntentAdapter adapter = provider.GetServices<IIdempotencyIntentAdapter>()
            .Single(item => item.CommandType == commandType);
        DateTimeOffset now = new(2026, 9, 25, 0, 0, 0, TimeSpan.Zero);
        var declaration = new ProviderDataHandlingTighteningDeclaration(1, 2,
            new ProviderDataHandlingFieldDiff(30, 14, true, false, ["US"], [], "terms-v1", "terms-v1"),
            "operator", "Agents.PlatformOperator", now);
        var terms = new ProviderDataHandlingRecord(14, false, ["EU"], "terms-v1",
            commandType == nameof(CreateProviderModelEntry) ? 1 : 2, now,
            commandType == nameof(CreateProviderModelEntry) ? null : declaration);
        var pricing = new ProviderModelPricing("USD", 1m, 2m, 1);
        var timeout = new ProviderModelTimeoutPolicy(30_000, 3);
        byte[] Payload(ProviderDataHandlingRecord record) => commandType == nameof(CreateProviderModelEntry)
            ? JsonSerializer.SerializeToUtf8Bytes(new CreateProviderModelEntry("provider", "model", "Model", true,
                true, 128_000, 16_000, timeout, ProviderModelCapabilityFlags.None, null, pricing, record))
            : JsonSerializer.SerializeToUtf8Bytes(new UpdateProviderModelEntry("provider", "model", "Model",
                true, 128_000, 16_000, timeout, ProviderModelCapabilityFlags.None, null, pricing,
                ExpectedCapabilityVersion: 1, DataHandling: record, DeclareDataHandlingTightening: true));
        var original = new IdempotencyIntentCommand(commandType, ProviderCatalogIdentity.PlatformTenantId,
            "provider-catalog", ProviderCatalogIdentity.EntryId("provider", "model"), Payload(terms),
            new Dictionary<string, string> { ["actor:agentsProviderAdmin"] = "true" });
        ProviderDataHandlingRecord restamped = terms with
        {
            EffectiveAt = now.AddMinutes(1),
            TighteningDeclaration = terms.TighteningDeclaration is { } prior
                ? prior with { DeclaredAt = now.AddMinutes(1) } : null,
        };

        IdempotencyCanonicalIntent first = adapter.CreateIntent(original);
        IdempotencyCanonicalIntent retry = adapter.CreateIntent(original with { Payload = Payload(restamped) });
        IdempotencyCanonicalIntent divergent = adapter.CreateIntent(original with
        {
            Payload = Payload(terms with { RetentionDays = 7 }),
        });

        var encoder = new CanonicalIdempotencyIntentEncoder();
        byte[] Canonical(IdempotencyCanonicalIntent intent) => encoder.Encode(adapter.AdapterId,
            adapter.OperationId, adapter.DescriptorVersion, adapter.RetentionTier, intent);
        Canonical(retry).SequenceEqual(Canonical(first)).ShouldBeTrue();
        Canonical(divergent).SequenceEqual(Canonical(first)).ShouldBeFalse();
    }

    [Theory]
    [InlineData(" agents")]
    [InlineData("agents ")]
    public void Gateway_registration_rejects_a_padded_Dapr_app_id(string appId)
    {
        ServiceCollection services = new();

        Should.Throw<ArgumentException>(() => services.AddAgentsEventStore(appId));
        services.ShouldBeEmpty();
    }

    [Fact]
    public void Gateway_registration_rejects_a_conflicting_Dapr_app_id()
    {
        ServiceCollection services = new();
        _ = services.AddAgentsEventStore(AgentsAppId, new FixtureTrustedCommandProofReader());

        Should.Throw<InvalidOperationException>(() => services.AddAgentsEventStore("other-agents"));
        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetServices<ITrustedCommandExtensionPolicy>().Count().ShouldBe(1);
    }

    [Theory]
    [InlineData(nameof(ActivateAgent), "Hexalith.Agents.EventStore.ActivateAgent.v1", "agents.setup.activate", 1)]
    [InlineData(nameof(ConfigureAgentResponseMode), "Hexalith.Agents.EventStore.ConfigureAgentResponseMode.v1", "agents.setup.response-mode", 1)]
    [InlineData(nameof(CreateAgent), "Hexalith.Agents.EventStore.CreateAgent.v1", "agents.setup.create", 1)]
    [InlineData(nameof(DisableAgent), "Hexalith.Agents.EventStore.DisableAgent.v1", "agents.setup.disable", 1)]
    [InlineData(nameof(UpdateAgentConfiguration), "Hexalith.Agents.EventStore.UpdateAgentConfiguration.v1", "agents.setup.update", 1)]
    public void Every_setup_adapter_publishes_its_stable_descriptor_contract(
        string commandType,
        string expectedAdapterId,
        string expectedOperationId,
        int expectedDescriptorVersion)
    {
        SubmitCommand command = ActivationCommand(
            payload: "{\"value\":1}",
            version: "7",
            providerVerdict: nameof(ProviderSelectionValidationStatus.Valid),
            approverVerdict: nameof(ApproverPolicyValidationStatus.Valid)) with
        {
            CommandType = commandType,
        };

        TrustedIdempotencyDescriptor descriptor = Registry().Resolve(command);

        descriptor.AdapterId.ShouldBe(expectedAdapterId);
        descriptor.OperationId.ShouldBe(expectedOperationId);
        descriptor.DescriptorVersion.ShouldBe(expectedDescriptorVersion);
    }

    [Fact]
    public void Activation_intent_excludes_dependency_evidence_but_keeps_payload_authorization_and_version()
    {
        IdempotencyIntentAdapterRegistry registry = Registry();
        SubmitCommand original = ActivationCommand(
            payload: "{\"value\":1,\"nested\":{\"b\":2,\"a\":1}}",
            version: "7",
            providerVerdict: nameof(ProviderSelectionValidationStatus.Valid),
            approverVerdict: nameof(ApproverPolicyValidationStatus.Valid));
        SubmitCommand reorderedRetry = ActivationCommand(
            payload: "{\"nested\":{\"a\":1,\"b\":2},\"value\":1}",
            version: "7",
            providerVerdict: nameof(ProviderSelectionValidationStatus.Unavailable),
            approverVerdict: nameof(ApproverPolicyValidationStatus.Unavailable));

        TrustedIdempotencyDescriptor descriptor = registry.Resolve(original);
        descriptor.AdapterId.ShouldBe("Hexalith.Agents.EventStore.ActivateAgent.v1");
        descriptor.OperationId.ShouldBe("agents.setup.activate");
        descriptor.DescriptorVersion.ShouldBe(1);
        registry.Resolve(reorderedRetry).CanonicalIntent.ShouldBe(descriptor.CanonicalIntent);
        registry.Resolve(ActivationCommand(
            "{\"value\":1}",
            "7",
            nameof(ProviderSelectionValidationStatus.Valid),
            nameof(ApproverPolicyValidationStatus.Valid))).CanonicalIntent
            .ShouldBe(descriptor.CanonicalIntent);
        registry.Resolve(ActivationCommand(
            "{\"value\":2}",
            "7",
            nameof(ProviderSelectionValidationStatus.Valid),
            nameof(ApproverPolicyValidationStatus.Valid))).CanonicalIntent
            .ShouldBe(descriptor.CanonicalIntent);

        SubmitCommand disabled = StandardCommand(nameof(DisableAgent), "true");
        registry.Resolve(disabled with { Payload = Encoding.UTF8.GetBytes("{\"value\":2}") }).CanonicalIntent
            .ShouldBe(registry.Resolve(disabled).CanonicalIntent);

        SubmitCommand[] conflicts =
        [
            ActivationCommand("{\"value\":1}", "8", nameof(ProviderSelectionValidationStatus.Valid), nameof(ApproverPolicyValidationStatus.Valid)),
            ActivationCommand("{\"value\":1}", "7", nameof(ProviderSelectionValidationStatus.Valid), nameof(ApproverPolicyValidationStatus.Valid), administratorAuthorization: "false"),
        ];
        foreach (SubmitCommand conflict in conflicts)
        {
            registry.Resolve(conflict).CanonicalIntent.ShouldNotBe(descriptor.CanonicalIntent);
        }
    }

    [Theory]
    [InlineData(nameof(ConfigureAgentResponseMode))]
    [InlineData(nameof(CreateAgent))]
    [InlineData(nameof(DisableAgent))]
    [InlineData(nameof(UpdateAgentConfiguration))]
    public void Standard_setup_intent_keeps_administrator_authorization(string commandType)
    {
        IdempotencyIntentAdapterRegistry registry = Registry();
        SubmitCommand authorized = StandardCommand(commandType, administratorAuthorization: "true");
        SubmitCommand unauthorized = StandardCommand(commandType, administratorAuthorization: "false");

        registry.Resolve(unauthorized).CanonicalIntent
            .ShouldNotBe(registry.Resolve(authorized).CanonicalIntent);
    }

    [Fact]
    public void Setup_intent_scopes_the_canonical_target_by_tenant_and_agent()
    {
        IdempotencyIntentAdapterRegistry registry = Registry();
        SubmitCommand original = ActivationCommand(
            payload: "{\"value\":1}",
            version: "7",
            providerVerdict: nameof(ProviderSelectionValidationStatus.Valid),
            approverVerdict: nameof(ApproverPolicyValidationStatus.Valid));
        byte[] originalIntent = registry.Resolve(original).CanonicalIntent;

        Registry("tenant-b").Resolve(original with { Tenant = "tenant-b" }).CanonicalIntent.ShouldNotBe(originalIntent);
        registry.Resolve(original with { AggregateId = "agent-b" }).CanonicalIntent.ShouldNotBe(originalIntent);
    }

    [Fact]
    public void Declared_command_spelling_shares_one_canonical_intent_and_a_semantic_change_does_not()
    {
        IdempotencyIntentAdapterRegistry registry = Registry();
        SubmitCommand pascalCase = UpdateCommand("{\"DisplayName\":\"Hexa\",\"Instructions\":\"Stay terse\"}");
        SubmitCommand camelCase = UpdateCommand("{\"instructions\":\"Stay terse\",\"displayName\":\"Hexa\"}");
        SubmitCommand changedInstructions = UpdateCommand("{\"displayName\":\"Hexa\",\"instructions\":\"Say more\"}");
        SubmitCommand paddedName = UpdateCommand("{\"displayName\":\"Hexa \",\"instructions\":\"Stay terse\"}");

        byte[] canonical = registry.Resolve(pascalCase).CanonicalIntent;
        registry.Resolve(pascalCase).RetentionTier.ShouldBe(IdempotencyReplayRetentionTier.Mutation);
        registry.Resolve(camelCase).CanonicalIntent.SequenceEqual(canonical).ShouldBeTrue();
        registry.Resolve(changedInstructions).CanonicalIntent.SequenceEqual(canonical).ShouldBeFalse();
        registry.Resolve(paddedName).CanonicalIntent.SequenceEqual(canonical).ShouldBeFalse();

        SubmitCommand namedMode = ResponseModeCommand("{\"Mode\":\"Automatic\"}");
        SubmitCommand numericMode = ResponseModeCommand("{\"mode\":1}");
        SubmitCommand otherMode = ResponseModeCommand("{\"mode\":\"Confirmation\"}");
        byte[] modeCanonical = registry.Resolve(namedMode).CanonicalIntent;
        registry.Resolve(numericMode).CanonicalIntent.SequenceEqual(modeCanonical).ShouldBeTrue();
        registry.Resolve(otherMode).CanonicalIntent.SequenceEqual(modeCanonical).ShouldBeFalse();

        SubmitCommand unknownMode = ResponseModeCommand("{\"mode\":\"Unknown\"}");
        byte[] unknownCanonical = registry.Resolve(unknownMode).CanonicalIntent;
        unknownCanonical.SequenceEqual(modeCanonical).ShouldBeFalse();
        registry.Resolve(ResponseModeCommand("{\"mode\":\"nope\"}")).CanonicalIntent
            .SequenceEqual(unknownCanonical).ShouldBeTrue();
        registry.Resolve(ResponseModeCommand("{\"mode\":9999}")).CanonicalIntent
            .SequenceEqual(unknownCanonical).ShouldBeTrue();
        registry.Resolve(ResponseModeCommand("{}")).CanonicalIntent
            .SequenceEqual(unknownCanonical).ShouldBeTrue();
    }

    [Fact]
    public void Create_declared_command_spelling_shares_one_canonical_intent_and_semantic_changes_do_not()
    {
        IdempotencyIntentAdapterRegistry registry = Registry();
        SubmitCommand pascalCase = CreateCommand(
            "{\"TenantId\":\"tenant-a\",\"DisplayName\":\"Hexa\",\"Description\":null,\"Instructions\":\"Stay terse\"}");
        SubmitCommand camelCase = CreateCommand(
            "{\"instructions\":\"Stay terse\",\"description\":null,\"displayName\":\"Hexa\",\"tenantId\":\"tenant-a\"}");
        SubmitCommand changedTenant = CreateCommand(
            "{\"tenantId\":\"tenant-b\",\"displayName\":\"Hexa\",\"description\":null,\"instructions\":\"Stay terse\"}");
        SubmitCommand changedInstructions = CreateCommand(
            "{\"tenantId\":\"tenant-a\",\"displayName\":\"Hexa\",\"description\":null,\"instructions\":\"Say more\"}");

        byte[] canonical = registry.Resolve(pascalCase).CanonicalIntent;
        registry.Resolve(camelCase).CanonicalIntent.SequenceEqual(canonical).ShouldBeTrue();
        registry.Resolve(changedTenant).CanonicalIntent.SequenceEqual(canonical).ShouldBeFalse();
        registry.Resolve(changedInstructions).CanonicalIntent.SequenceEqual(canonical).ShouldBeFalse();

        SubmitCommand omittedDescription = CreateCommand(
            "{\"tenantId\":\"tenant-a\",\"displayName\":\"Hexa\",\"instructions\":\"Stay terse\"}");
        registry.Resolve(omittedDescription).CanonicalIntent.SequenceEqual(canonical).ShouldBeTrue();
    }

    [Theory]
    [InlineData("null")]
    [InlineData("[]")]
    public void A_payload_that_is_not_the_declared_command_is_rejected(string payload)
    {
        IdempotencyIntentAdapterRegistry registry = Registry();

        Should.Throw<ArgumentException>(() => registry.Resolve(UpdateCommand(payload)));
    }

    [Fact]
    public void A_non_agent_domain_is_rejected_by_the_setup_adapter()
    {
        SubmitCommand foreign = UpdateCommand("{\"displayName\":\"Hexa\",\"instructions\":\"Stay terse\"}") with
        {
            Domain = "other-domain",
        };

#if HEXALITH_EVENTSTORE_SOURCE
        Should.Throw<InvalidOperationException>(() => Registry().Resolve(foreign));
#else
        Should.Throw<ArgumentException>(() => Registry().Resolve(foreign));
#endif
    }

    [Fact]
    public async Task Real_gateway_HTTP_path_replays_the_same_intent_and_conflicts_changed_semantics_without_a_second_execution()
    {
        var executedCommands = new List<SubmitCommand>();
        await using WebApplication domain = BuildDomainApp(executedCommands.Add);
        await domain.StartAsync().ConfigureAwait(true);
        domain.Services.GetServices<IIdempotencyIntentAdapter>().ShouldBeEmpty();
        domain.Services.GetServices<ITrustedCommandExtensionPolicy>().ShouldBeEmpty();
        using HttpClient domainClient = domain.GetTestClient();
        var ledger = new InMemoryAdmissionLedger(domainClient);
        await using WebApplication gateway = BuildGatewayApp(
            Principal(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId),
            ledger,
            registerAgentsIntegration: true);
        await gateway.StartAsync().ConfigureAwait(true);
        using HttpClient client = GatewayClient(gateway);

        SubmitCommandRequest original = ActivationRequest(
            payload: JsonSerializer.SerializeToElement(new { value = 1, nested = new { b = 2, a = 1 } }),
            version: "7",
            providerVerdict: nameof(ProviderSelectionValidationStatus.Valid),
            approverVerdict: nameof(ApproverPolicyValidationStatus.Valid));
        SubmitCommandRequest reorderedRetry = ActivationRequest(
            payload: JsonSerializer.SerializeToElement(new { nested = new { a = 1, b = 2 }, value = 1 }),
            version: "7",
            providerVerdict: nameof(ProviderSelectionValidationStatus.Unavailable),
            approverVerdict: nameof(ApproverPolicyValidationStatus.Unavailable));

        using HttpResponseMessage originalResponse = await client
            .PostAsJsonAsync("/api/v1/commands", original)
            .ConfigureAwait(true);
        originalResponse.StatusCode.ShouldBe(System.Net.HttpStatusCode.Accepted);
        SubmitCommandResponse originalReceipt = (await originalResponse.Content
            .ReadFromJsonAsync<SubmitCommandResponse>()
            .ConfigureAwait(true)).ShouldNotBeNull();

        using HttpResponseMessage replayResponse = await client
            .PostAsJsonAsync("/api/v1/commands", reorderedRetry)
            .ConfigureAwait(true);
        replayResponse.StatusCode.ShouldBe(System.Net.HttpStatusCode.Accepted);
        SubmitCommandResponse replayReceipt = (await replayResponse.Content
            .ReadFromJsonAsync<SubmitCommandResponse>()
            .ConfigureAwait(true)).ShouldNotBeNull();
        replayReceipt.CorrelationId.ShouldBe(originalReceipt.CorrelationId);
        replayReceipt.MessageId.ShouldBe(originalReceipt.MessageId);
        replayReceipt.ResultPayload.ShouldNotBeNull().GetRawText()
            .ShouldBe(originalReceipt.ResultPayload.ShouldNotBeNull().GetRawText());
        executedCommands.Count.ShouldBe(1);

        using HttpResponseMessage undeclaredValueReplay = await client
            .PostAsJsonAsync(
                "/api/v1/commands",
                ActivationRequest(
                    JsonSerializer.SerializeToElement(new { value = 2 }),
                    "7",
                    nameof(ProviderSelectionValidationStatus.Valid),
                    nameof(ApproverPolicyValidationStatus.Valid)))
            .ConfigureAwait(true);
        undeclaredValueReplay.StatusCode.ShouldBe(System.Net.HttpStatusCode.Accepted);
        SubmitCommandResponse undeclaredValueReceipt = (await undeclaredValueReplay.Content
            .ReadFromJsonAsync<SubmitCommandResponse>()
            .ConfigureAwait(true)).ShouldNotBeNull();
        undeclaredValueReceipt.MessageId.ShouldBe(originalReceipt.MessageId);
        executedCommands.Count.ShouldBe(1);
        executedCommands[0].Extensions?[AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion]
            .ShouldBe("7");

        SubmitCommandRequest[] conflicts =
        [
            ActivationRequest(JsonSerializer.SerializeToElement(new { value = 1 }), "8", nameof(ProviderSelectionValidationStatus.Valid), nameof(ApproverPolicyValidationStatus.Valid)),
            ActivationRequest(
                JsonSerializer.SerializeToElement(new { value = 1 }),
                "7",
                nameof(ProviderSelectionValidationStatus.Valid),
                nameof(ApproverPolicyValidationStatus.Valid),
                administratorAuthorization: null),
        ];
        foreach (SubmitCommandRequest conflict in conflicts)
        {
            using HttpResponseMessage response = await client
                .PostAsJsonAsync("/api/v1/commands", conflict)
                .ConfigureAwait(true);
            response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Conflict);
        }

        executedCommands.Count.ShouldBe(1);
        ledger.ExecutionCount.ShouldBe(1);
    }

    [Fact]
    public async Task Host_trust_metadata_reaches_execution_and_a_new_delivery_nonce_replays_the_same_logical_command()
    {
        var executedCommands = new List<SubmitCommand>();
        await using WebApplication domain = BuildDomainApp(executedCommands.Add);
        await domain.StartAsync().ConfigureAwait(true);
        using HttpClient domainClient = domain.GetTestClient();
        var ledger = new InMemoryAdmissionLedger(domainClient);
        await using WebApplication gateway = BuildGatewayApp(
            Principal(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId),
            ledger,
            registerAgentsIntegration: true);
        await gateway.StartAsync().ConfigureAwait(true);
        using HttpClient client = GatewayClient(gateway);

        using HttpResponseMessage first = await client
            .PostAsJsonAsync("/api/v1/commands", Delivery("trust:deliveryNonce", "delivery-1"))
            .ConfigureAwait(true);
        first.StatusCode.ShouldBe(System.Net.HttpStatusCode.Accepted);
        SubmitCommandResponse firstReceipt = (await first.Content
            .ReadFromJsonAsync<SubmitCommandResponse>()
            .ConfigureAwait(true)).ShouldNotBeNull();

        using HttpResponseMessage redelivery = await client
            .PostAsJsonAsync("/api/v1/commands", Delivery("trust:deliveryNonce", "delivery-2"))
            .ConfigureAwait(true);
        redelivery.StatusCode.ShouldBe(System.Net.HttpStatusCode.Accepted);
        (await redelivery.Content.ReadFromJsonAsync<SubmitCommandResponse>().ConfigureAwait(true))
            .ShouldNotBeNull().MessageId.ShouldBe(firstReceipt.MessageId);

        using HttpResponseMessage caseVariant = await client
            .PostAsJsonAsync("/api/v1/commands", Delivery("TRUST:deliveryNonce", "delivery-3"))
            .ConfigureAwait(true);
        caseVariant.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);

        executedCommands.Count.ShouldBe(1);
        executedCommands[0].Extensions?["trust:deliveryNonce"].ShouldBe("delivery-1");
        ledger.ExecutionCount.ShouldBe(1);

        static SubmitCommandRequest Delivery(string key, string nonce)
        {
            SubmitCommandRequest request = ActivationRequest(
                JsonSerializer.SerializeToElement(new { value = 1 }),
                "7",
                nameof(ProviderSelectionValidationStatus.Valid),
                nameof(ApproverPolicyValidationStatus.Valid));
            request.Extensions.ShouldNotBeNull()[key] = nonce;
            return request;
        }
    }

    [Fact]
    public async Task Equivalent_setup_spelling_replays_on_the_gateway_and_a_semantic_change_does_not_append_again()
    {
        var executedCommands = new List<SubmitCommand>();
        await using WebApplication domain = BuildDomainApp(executedCommands.Add);
        await domain.StartAsync().ConfigureAwait(true);
        using HttpClient domainClient = domain.GetTestClient();
        var ledger = new InMemoryAdmissionLedger(domainClient);
        var admission = new ProductionAdmissionState();
        await using WebApplication gateway = BuildGatewayApp(
            Principal(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId),
            ledger,
            registerAgentsIntegration: true,
            admission);
        await gateway.StartAsync().ConfigureAwait(true);
        using HttpClient client = GatewayClient(gateway);

        SubmitCommandRequest original = ResponseModeRequest("{\"Mode\":\"Automatic\"}");
        SubmitCommandRequest spelled = ResponseModeRequest("{\"mode\":\"automatic\"}");
        SubmitCommandRequest numeric = ResponseModeRequest("{\"mode\":1}");
        TrustedIdempotencyDescriptor descriptor = Registry().Resolve(Submitted(original));
        descriptor.RetentionTier.ShouldBe(IdempotencyReplayRetentionTier.Mutation);
        Registry().Resolve(Submitted(spelled)).CanonicalIntent.SequenceEqual(descriptor.CanonicalIntent).ShouldBeTrue();
        Registry().Resolve(Submitted(numeric)).CanonicalIntent.SequenceEqual(descriptor.CanonicalIntent).ShouldBeTrue();

        using HttpResponseMessage originalResponse = await client
            .PostAsJsonAsync("/api/v1/commands", original)
            .ConfigureAwait(true);
        originalResponse.StatusCode.ShouldBe(System.Net.HttpStatusCode.Accepted);
        SubmitCommandResponse originalReceipt = (await originalResponse.Content
            .ReadFromJsonAsync<SubmitCommandResponse>()
            .ConfigureAwait(true)).ShouldNotBeNull();
        admission.ObservedRetentionTier.ShouldBe(IdempotencyReplayRetentionTier.Mutation);
        admission.ObservedIntentDigest.ShouldBe(await IntentDigestAsync(descriptor).ConfigureAwait(true));
        // A fixed cross-run digest catches accidental canonical-byte or retention-policy drift.
        admission.ObservedIntentDigest.ShouldBe("xwzhj031wJ2Up6cE0vPZaOY6M2ZH7WsxcdBkiLf-0No");

        using HttpResponseMessage replayResponse = await client
            .PostAsJsonAsync("/api/v1/commands", spelled)
            .ConfigureAwait(true);
        replayResponse.StatusCode.ShouldBe(System.Net.HttpStatusCode.Accepted);
        SubmitCommandResponse replayReceipt = (await replayResponse.Content
            .ReadFromJsonAsync<SubmitCommandResponse>()
            .ConfigureAwait(true)).ShouldNotBeNull();
        replayReceipt.MessageId.ShouldBe(originalReceipt.MessageId);
        replayReceipt.ResultPayload.ShouldNotBeNull().GetRawText()
            .ShouldBe(originalReceipt.ResultPayload.ShouldNotBeNull().GetRawText());

        using HttpResponseMessage numericResponse = await client
            .PostAsJsonAsync("/api/v1/commands", numeric)
            .ConfigureAwait(true);
        numericResponse.StatusCode.ShouldBe(System.Net.HttpStatusCode.Accepted);
        SubmitCommandResponse numericReceipt = (await numericResponse.Content
            .ReadFromJsonAsync<SubmitCommandResponse>()
            .ConfigureAwait(true)).ShouldNotBeNull();
        numericReceipt.MessageId.ShouldBe(originalReceipt.MessageId);
        numericReceipt.ResultPayload.ShouldNotBeNull().GetRawText()
            .ShouldBe(originalReceipt.ResultPayload.ShouldNotBeNull().GetRawText());

        using HttpResponseMessage conflictResponse = await client
            .PostAsJsonAsync("/api/v1/commands", ResponseModeRequest("{\"mode\":\"Confirmation\"}"))
            .ConfigureAwait(true);
        conflictResponse.StatusCode.ShouldBe(System.Net.HttpStatusCode.Conflict);
        executedCommands.Count.ShouldBe(1);
        ledger.ExecutionCount.ShouldBe(1);
    }

    [Fact]
    public async Task SetupReceiptReplayFeedsTheProductionProjectionOnceAfterLagAndConflict()
    {
        const string StoreName = "statestore";
        var created = new AgentCreated("agent-a", "tenant-a", "Hexa", null, "private instructions long enough", 1, 1);
        var state = new AgentState();
        state.Apply(created);
        var history = new List<ProjectionEventDto>
        {
            new(nameof(AgentCreated), JsonSerializer.SerializeToUtf8Bytes(created), "json", 1, DateTimeOffset.UtcNow, "create-correlation", "create-message"),
        };
        var store = new FakeReadModelStore();
        var projection = new AgentSetupProjectionHandler(store, store,
            Options.Create(new AgentSetupReadModelOptions { StateStoreName = StoreName }));
        (await projection.ProjectAsync(new("tenant-a", "agent", "agent-a", [history[0]]), "seed",
            TestContext.Current.CancellationToken)).Status.ShouldBe(ProjectionDispatchStatus.Completed);
        string? resultPayload = null;
        await using WebApplication domain = BuildDomainApp(command =>
        {
            var payload = JsonSerializer.Deserialize<ConfigureAgentResponseMode>(command.Payload,
                new JsonSerializerOptions(JsonSerializerDefaults.Web)).ShouldNotBeNull();
            DomainResult result = AgentAggregate.Handle(payload, state, command.ToCommandEnvelope());
            var applied = result.Events.Single().ShouldBeOfType<AgentResponseModeConfigured>();
            state.Apply(applied);
            history.Add(new(nameof(AgentResponseModeConfigured), JsonSerializer.SerializeToUtf8Bytes(applied), "json",
                history.Count + 1, DateTimeOffset.UtcNow, command.CorrelationId, command.MessageId));
            resultPayload = result.ResultPayload.ShouldNotBeNull();
        }, _ => resultPayload);
        await domain.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient domainClient = domain.GetTestClient();
        var ledger = new InMemoryAdmissionLedger(domainClient);
        await using WebApplication gateway = BuildGatewayApp(Principal(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId), ledger, true);
        await gateway.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient client = GatewayClient(gateway);
        using HttpResponseMessage original = await client.PostAsJsonAsync("/api/v1/commands",
            ResponseModeRequest("{\"Mode\":\"Automatic\"}"), TestContext.Current.CancellationToken);
        original.StatusCode.ShouldBe(System.Net.HttpStatusCode.Accepted);
        SubmitCommandResponse receipt = (await original.Content.ReadFromJsonAsync<SubmitCommandResponse>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
        JsonElement effect = receipt.ResultPayload.ShouldNotBeNull();
        effect.GetProperty("effect").GetString().ShouldBe(nameof(AgentSetupWriteEffect.Applied));
        int version = effect.GetProperty("configurationVersion").GetInt32();
        version.ShouldBe(2);
        string key = AgentSetupReadModelAddresses.Detail("tenant-a", "agent-a");
        AgentSetupReadModel lagged = store.Snapshot<AgentSetupReadModel>(StoreName, key).ShouldNotBeNull();
        lagged.ConfigurationVersion.ShouldBe(1);
        AgentSetupViewFactory.Create(lagged, "tenant-a", "agent-a", version, true).Setup.ShouldNotBeNull().TruthState.ShouldBe(AgentSetupTruthState.AuthoritativePending);

        using HttpResponseMessage retry = await client.PostAsJsonAsync("/api/v1/commands",
            ResponseModeRequest("{\"mode\":1}"), TestContext.Current.CancellationToken);
        retry.StatusCode.ShouldBe(System.Net.HttpStatusCode.Accepted);
        SubmitCommandResponse replay = (await retry.Content.ReadFromJsonAsync<SubmitCommandResponse>(TestContext.Current.CancellationToken)).ShouldNotBeNull();
        replay.MessageId.ShouldBe(receipt.MessageId);
        replay.CorrelationId.ShouldBe(receipt.CorrelationId);
        replay.ResultPayload.ShouldNotBeNull().GetRawText().ShouldBe(effect.GetRawText());
        using HttpResponseMessage conflict = await client.PostAsJsonAsync("/api/v1/commands",
            ResponseModeRequest("{\"mode\":\"Confirmation\"}"), TestContext.Current.CancellationToken);
        conflict.StatusCode.ShouldBe(System.Net.HttpStatusCode.Conflict);
        ledger.ExecutionCount.ShouldBe(1);
        history.Count.ShouldBe(2);

        var delivery = new ProjectionRequest("tenant-a", "agent", "agent-a", [history[1]]);
        (await projection.ProjectAsync(delivery, "accepted-command", TestContext.Current.CancellationToken))
            .Status.ShouldBe(ProjectionDispatchStatus.Completed);
        AgentSetupReadModel persisted = store.Snapshot<AgentSetupReadModel>(StoreName, key).ShouldNotBeNull();
        persisted.ConfigurationVersion.ShouldBe(version);
        persisted.LastSequenceNumber.ShouldBe(2);
        persisted.ResponseMode.ShouldBe(AgentResponseMode.Automatic);
        history[1].MessageId.ShouldBe(receipt.MessageId);
        history[1].CorrelationId.ShouldBe(receipt.CorrelationId);
        AgentSetupViewFactory.Create(persisted, "tenant-a", "agent-a", version, true).Setup.ShouldNotBeNull().TruthState.ShouldBe(AgentSetupTruthState.ProjectionConfirmed);
        int writes = store.BatchCount;
        (await projection.ProjectAsync(delivery, "duplicate-command", TestContext.Current.CancellationToken))
            .Status.ShouldBe(ProjectionDispatchStatus.AlreadyCompleted);
        store.BatchCount.ShouldBe(writes);
        store.Snapshot<AgentSetupReadModel>(StoreName, key).ShouldBeEquivalentTo(persisted);
        JsonSerializer.Serialize(persisted).ShouldNotContain("private instructions");
    }

    [Theory]
    [InlineData("exact")]
    [InlineData("missing-registration")]
    [InlineData("missing-proof")]
    [InlineData("wrong-intent")]
    [InlineData("wrong-workload")]
    public async Task HttpEnrollmentRequiresTheRegisteredExactProofBeforeAnyDomainEffects(string scenario)
    {
        SubmitCommandRequest admitted = ResponseModeRequest("{\"mode\":\"Automatic\"}");
        // The SDK removes all extension metadata from the proof reader's command and passes each exact key/value separately.
        string intent = JsonSerializer.Serialize(admitted with { Extensions = null });
        var proof = Substitute.For<IAgentsTrustedCommandProofReader>();
        proof.HasAdmittedProof(Arg.Any<ClaimsPrincipal>(), Arg.Any<SubmitCommandRequest>(), Arg.Any<string>(), Arg.Any<string>())
            .Returns(call => JsonSerializer.Serialize(call.Arg<SubmitCommandRequest>()) == intent
                && call.ArgAt<string>(2) == AgentSetupTrustedExtensions.AgentAdministrator && call.ArgAt<string>(3) == "true");
        var history = new List<SubmitCommand>();
        await using WebApplication domain = BuildDomainApp(history.Add);
        await domain.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient domainClient = domain.GetTestClient();
        var ledger = new InMemoryAdmissionLedger(domainClient);
        await using WebApplication gateway = BuildGatewayApp(
            Principal(DaprInternalAuthenticationOptions.SchemeName, scenario == "wrong-workload" ? "foreign-agents" : AgentsAppId),
            ledger, scenario != "missing-registration", proofReader: proof, proofAvailable: scenario != "missing-proof");
        await gateway.StartAsync(TestContext.Current.CancellationToken);
        using HttpClient client = GatewayClient(gateway);
        SubmitCommandRequest submitted = scenario == "wrong-intent"
            ? admitted with { Payload = JsonPayload("{\"mode\":\"Confirmation\"}") } : admitted;

        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", submitted,
            TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(scenario == "exact" ? System.Net.HttpStatusCode.Accepted : System.Net.HttpStatusCode.BadRequest);
        history.Count.ShouldBe(scenario == "exact" ? 1 : 0);
        ledger.ExecutionCount.ShouldBe(scenario == "exact" ? 1 : 0);
    }

    [Fact]
    public async Task An_in_flight_key_from_the_previous_encoder_conflicts_without_a_second_execution()
    {
        const string RawPayload = "{\"Mode\":\"Automatic\"}";
        var semanticOptions = new Dictionary<string, string>
        {
            [AgentSetupTrustedExtensions.AgentAdministrator] = "true",
        };
        byte[] previousCanonical = new CanonicalIdempotencyIntentEncoder().Encode(
            "Hexalith.Agents.EventStore.ConfigureAgentResponseMode.v1",
            "agents.setup.response-mode",
            1,
            IdempotencyReplayRetentionTier.Mutation,
            new IdempotencyCanonicalIntent(
                JsonSerializer.Serialize(new[] { "tenant-a", "agent", "agent-a" }),
                Encoding.UTF8.GetBytes(RawPayload),
                semanticOptions,
                PolicyVersion: "1",
                DelegatedTaskScope: null,
                CredentialScope: null));
        SubmitCommandRequest retry = ResponseModeRequest(RawPayload);
        byte[] currentCanonical = Registry().Resolve(Submitted(retry)).CanonicalIntent;
        currentCanonical.SequenceEqual(previousCanonical).ShouldBeFalse();

        var admission = new ProductionAdmissionState();
        admission.SeedInFlight(await IntentDigestAsync(new TrustedIdempotencyDescriptor(
            "Hexalith.Agents.EventStore.ConfigureAgentResponseMode.v1",
            "agents.setup.response-mode",
            1,
            previousCanonical,
            IdempotencyReplayRetentionTier.Mutation)).ConfigureAwait(true));

        int domainExecutions = 0;
        await using WebApplication domain = BuildDomainApp(_ => domainExecutions++);
        await domain.StartAsync().ConfigureAwait(true);
        using HttpClient domainClient = domain.GetTestClient();
        var ledger = new InMemoryAdmissionLedger(domainClient);
        await using WebApplication gateway = BuildGatewayApp(
            Principal(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId),
            ledger,
            registerAgentsIntegration: true,
            admission);
        await gateway.StartAsync().ConfigureAwait(true);
        using HttpClient client = GatewayClient(gateway);

        using HttpResponseMessage response = await client
            .PostAsJsonAsync("/api/v1/commands", retry)
            .ConfigureAwait(true);
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Conflict);
        domainExecutions.ShouldBe(0);
        ledger.ExecutionCount.ShouldBe(0);
    }

    [Theory]
    [InlineData("other-domain", "{\"displayName\":\"Hexa\",\"instructions\":\"Stay terse\"}", false)]
    [InlineData("agent", "null", true)]
    [InlineData("agent", "[]", true)]
    public async Task A_command_the_setup_adapter_rejects_fails_on_the_gateway_before_execution(
        string commandDomain,
        string payload,
        bool declaredRoute)
    {
        int domainExecutions = 0;
        await using WebApplication domain = BuildDomainApp(_ => domainExecutions++);
        await domain.StartAsync().ConfigureAwait(true);
        using HttpClient domainClient = domain.GetTestClient();
        var ledger = new InMemoryAdmissionLedger(domainClient);
        await using WebApplication gateway = BuildGatewayApp(
            Principal(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId),
            ledger,
            registerAgentsIntegration: true);
        await gateway.StartAsync().ConfigureAwait(true);
        using HttpClient client = GatewayClient(gateway);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/commands",
            Request(commandDomain, nameof(UpdateAgentConfiguration)) with
            {
                Payload = JsonPayload(payload),
            }).ConfigureAwait(true);

#if HEXALITH_EVENTSTORE_SOURCE
        if (payload is "null" or "[]")
        {
            response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Forbidden);
            domainExecutions.ShouldBe(0);
            ledger.ExecutionCount.ShouldBe(0);
            return;
        }
#endif
        using JsonDocument failure = JsonDocument.Parse(
            await response.Content.ReadAsStringAsync().ConfigureAwait(true));
#if !HEXALITH_EVENTSTORE_SOURCE
        declaredRoute = true;
#endif
        response.StatusCode.ShouldBe(declaredRoute ? System.Net.HttpStatusCode.BadRequest : System.Net.HttpStatusCode.ServiceUnavailable);
        failure.RootElement.GetProperty("retryable").GetBoolean().ShouldBe(!declaredRoute);
        failure.RootElement.GetProperty("clientAction").GetString().ShouldBe(declaredRoute ? "correct_request" : "retry_later");
        domainExecutions.ShouldBe(0);
        ledger.ExecutionCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(false, DaprInternalAuthenticationOptions.SchemeName)]
    [InlineData(true, "Bearer")]
    public async Task Missing_gateway_registration_or_non_Dapr_caller_rejects_reserved_extensions_before_execution(
        bool registerAgentsIntegration,
        string authenticationType)
    {
        int domainExecutions = 0;
        await using WebApplication domain = BuildDomainApp(_ => domainExecutions++);
        await domain.StartAsync().ConfigureAwait(true);
        using HttpClient domainClient = domain.GetTestClient();
        var ledger = new InMemoryAdmissionLedger(domainClient);
        await using WebApplication gateway = BuildGatewayApp(
            Principal(authenticationType, AgentsAppId),
            ledger,
            registerAgentsIntegration);
        await gateway.StartAsync().ConfigureAwait(true);
        using HttpClient client = GatewayClient(gateway);

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/commands",
            ActivationRequest(
                JsonSerializer.SerializeToElement(new ActivateAgent()),
                "7",
                nameof(ProviderSelectionValidationStatus.Valid),
                nameof(ApproverPolicyValidationStatus.Valid))).ConfigureAwait(true);

#if HEXALITH_EVENTSTORE_SOURCE
        if (authenticationType != DaprInternalAuthenticationOptions.SchemeName)
        {
            response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Forbidden);
            domainExecutions.ShouldBe(0);
            ledger.ExecutionCount.ShouldBe(0);
            return;
        }
#endif
        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
        domainExecutions.ShouldBe(0);
        ledger.ExecutionCount.ShouldBe(0);
    }

    [Theory]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.AgentAdministrator, "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ConfigureAgentResponseMode), AgentSetupTrustedExtensions.AgentAdministrator, "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(CreateAgent), AgentSetupTrustedExtensions.AgentAdministrator, "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(DisableAgent), AgentSetupTrustedExtensions.AgentAdministrator, "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(UpdateAgentConfiguration), AgentSetupTrustedExtensions.AgentAdministrator, "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "provider-catalog", nameof(CreateProviderModelEntry), "actor:agentsProviderAdmin", "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "provider-catalog", nameof(UpdateProviderModelEntry), "actor:agentsProviderAdmin", "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "provider-catalog", nameof(EnableProviderModelEntry), "actor:agentsProviderAdmin", "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "provider-catalog", nameof(DisableProviderModelEntry), "actor:agentsProviderAdmin", "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "tenant-provider-enablement", nameof(SetTenantProviderModelEnablement), "actor:platformOperator", "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "tenant-provider-enablement", nameof(DecideProviderDataHandling), "actor:tenantAgentAdministrator", "true", true)]
    [InlineData("Bearer", AgentsAppId, "tenant-provider-enablement", nameof(DecideProviderDataHandling), "actor:tenantAgentAdministrator", "true", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, "other-app", "provider-catalog", nameof(CreateProviderModelEntry), "actor:agentsProviderAdmin", "true", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "provider-catalog", nameof(CreateProviderModelEntry), "actor:agentsProviderAdmin", "false", false)]
    [InlineData("Bearer", AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.AgentAdministrator, "true", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, "other-app", "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.AgentAdministrator, "true", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "other-domain", nameof(ActivateAgent), AgentSetupTrustedExtensions.AgentAdministrator, "true", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", "OtherCommand", AgentSetupTrustedExtensions.AgentAdministrator, "true", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.AgentAdministrator, "True", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.AgentAdministrator, "false", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.AgentAdministrator, "0", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion, "0", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(CreateAgent), AgentSetupTrustedExtensions.ProviderSelectionValidation, "Valid", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(CreateAgent), AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion, "7", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(DisableAgent), AgentSetupTrustedExtensions.ApproverPolicyValidation, "Valid", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(DisableAgent), AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion, "7", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ConfigureAgentResponseMode), AgentSetupTrustedExtensions.ProviderSelectionValidation, "Valid", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ConfigureAgentResponseMode), AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion, "7", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(UpdateAgentConfiguration), AgentSetupTrustedExtensions.ProviderSelectionValidation, "Valid", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.ProviderSelectionValidation, "Valid", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.ApproverPolicyValidation, "Valid", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion, "7", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.ProviderSelectionValidation, "valid", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion, "07", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), "agent:unknown", "true", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), "trust:deliveryNonce", "delivery-1", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "tenant-provider-enablement", nameof(SetTenantProviderModelEnablement), "trust:deliveryNonce", "delivery-1", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), "trust:deliveryNonce", " ", false)]
    [InlineData("Bearer", AgentsAppId, "agent", nameof(ActivateAgent), "trust:deliveryNonce", "delivery-1", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, "other-app", "agent", nameof(DisableAgent), "trust:deliveryNonce", "delivery-1", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", "OtherCommand", "trust:deliveryNonce", "delivery-1", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(DisableAgent), "TRUST:deliveryNonce", "delivery-1", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(DisableAgent), "trust:", "delivery-1", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "conversation-agent-state", nameof(RegisterInteractionPermit), ConversationAgentStateIdentity.WorkflowAuthority, "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "conversation-agent-state", nameof(AcknowledgeInteractionCreation), ConversationAgentStateIdentity.WorkflowAuthority, "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "conversation-agent-state", nameof(AcquireConversationEffectLease), ConversationAgentStateIdentity.WorkflowAuthority, "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "conversation-agent-state", nameof(CommitConversationEffect), ConversationAgentStateIdentity.WorkflowAuthority, "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "conversation-agent-state", nameof(SettleConversationEffectLease), ConversationAgentStateIdentity.WorkflowAuthority, "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "conversation-agent-state", nameof(CancelConversationEffectLease), ConversationAgentStateIdentity.WorkflowAuthority, "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "conversation-agent-state", nameof(EnterConversationDeletionClosing), ConversationAgentStateIdentity.DeletionAuthority, "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent-interaction", nameof(CreatePermittedAgentInteraction), ConversationAgentStateIdentity.WorkflowAuthority, "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent-interaction", nameof(RecordAutomaticInteractionStep), ConversationAgentStateIdentity.WorkflowAuthority, "true", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "conversation-agent-state", nameof(EnterConversationDeletionClosing), ConversationAgentStateIdentity.WorkflowAuthority, "true", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent-interaction", nameof(RecordAutomaticInteractionStep), ConversationAgentStateIdentity.DeletionAuthority, "true", false)]
    public void Reserved_extension_policy_accepts_only_the_exact_Dapr_identity_command_key_and_value(
        string authenticationType,
        string appId,
        string domain,
        string commandType,
        string key,
        string value,
        bool expected)
    {
        ITrustedCommandExtensionPolicy policy = Policy();

        policy.Accepts(
            Principal(authenticationType, appId),
            Request(domain, commandType),
            key,
            value).ShouldBe(expected);
    }

    [Theory]
    [InlineData("agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.AgentAdministrator, true)]
    [InlineData("agent", nameof(UpdateAgentConfiguration), AgentSetupTrustedExtensions.AgentAdministrator, true)]
    [InlineData("agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.ProviderSelectionValidation, true)]
    [InlineData("agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.ApproverPolicyValidation, true)]
    [InlineData("agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion, true)]
    [InlineData("provider-catalog", nameof(CreateProviderModelEntry), "actor:agentsProviderAdmin", true)]
    [InlineData("provider-catalog", nameof(UpdateProviderModelEntry), "actor:agentsProviderAdmin", true)]
    [InlineData("tenant-provider-enablement", nameof(SetTenantProviderModelEnablement), "actor:platformOperator", true)]
    [InlineData("tenant-provider-enablement", nameof(DecideProviderDataHandling), "actor:tenantAgentAdministrator", true)]
    [InlineData("tenant-provider-enablement", nameof(DecideProviderDataHandling), "actor:platformOperator", false)]
    [InlineData("provider-catalog", nameof(CreateProviderModelEntry), "actor:platformOperator", false)]
    [InlineData("agent", nameof(UpdateAgentConfiguration), AgentSetupTrustedExtensions.ProviderSelectionValidation, false)]
    [InlineData("agent", nameof(CreateAgent), AgentSetupTrustedExtensions.ProviderSelectionValidation, false)]
    [InlineData("agent", nameof(CreateAgent), AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion, false)]
    [InlineData("agent", nameof(DisableAgent), AgentSetupTrustedExtensions.ApproverPolicyValidation, false)]
    [InlineData("agent", nameof(DisableAgent), AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion, false)]
    [InlineData("agent", nameof(ConfigureAgentResponseMode), AgentSetupTrustedExtensions.ProviderSelectionValidation, false)]
    [InlineData("agent", nameof(ConfigureAgentResponseMode), AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion, false)]
    [InlineData("other-domain", nameof(ActivateAgent), AgentSetupTrustedExtensions.AgentAdministrator, false)]
    [InlineData("agent", "OtherCommand", AgentSetupTrustedExtensions.AgentAdministrator, false)]
    [InlineData("agent", nameof(ActivateAgent), "agent:unknown", false)]
    [InlineData("agent", nameof(ActivateAgent), "trust:deliveryNonce", true)]
    [InlineData("provider-catalog", nameof(CreateProviderModelEntry), "trust:deliveryNonce", true)]
    [InlineData("tenant-provider-enablement", nameof(DecideProviderDataHandling), "trust:signingKeyVersion", true)]
    [InlineData("other-domain", nameof(ActivateAgent), "trust:deliveryNonce", false)]
    [InlineData("agent", "OtherCommand", "trust:deliveryNonce", false)]
    [InlineData("agent", nameof(DisableAgent), "TRUST:deliveryNonce", false)]
    [InlineData("agent", nameof(DisableAgent), "trust:", false)]
    [InlineData("conversation-agent-state", nameof(RegisterInteractionPermit), ConversationAgentStateIdentity.WorkflowAuthority, true)]
    [InlineData("conversation-agent-state", nameof(AcknowledgeInteractionCreation), ConversationAgentStateIdentity.WorkflowAuthority, true)]
    [InlineData("conversation-agent-state", nameof(AcquireConversationEffectLease), ConversationAgentStateIdentity.WorkflowAuthority, true)]
    [InlineData("conversation-agent-state", nameof(CommitConversationEffect), ConversationAgentStateIdentity.WorkflowAuthority, true)]
    [InlineData("conversation-agent-state", nameof(SettleConversationEffectLease), ConversationAgentStateIdentity.WorkflowAuthority, true)]
    [InlineData("conversation-agent-state", nameof(CancelConversationEffectLease), ConversationAgentStateIdentity.WorkflowAuthority, true)]
    [InlineData("conversation-agent-state", nameof(EnterConversationDeletionClosing), ConversationAgentStateIdentity.DeletionAuthority, true)]
    [InlineData("agent-interaction", nameof(CreatePermittedAgentInteraction), ConversationAgentStateIdentity.WorkflowAuthority, true)]
    [InlineData("agent-interaction", nameof(RecordAutomaticInteractionStep), ConversationAgentStateIdentity.WorkflowAuthority, true)]
    [InlineData("conversation-agent-state", nameof(EnterConversationDeletionClosing), ConversationAgentStateIdentity.WorkflowAuthority, false)]
    [InlineData("agent-interaction", nameof(RecordAutomaticInteractionStep), ConversationAgentStateIdentity.DeletionAuthority, false)]
    public void Reserved_extension_policy_claims_only_its_exact_keys(
        string domain,
        string commandType,
        string key,
        bool expected)
    {
        ITrustedCommandExtensionPolicy policy = Policy();
        MethodInfo claims = policy.GetType().GetMethod(
            "Claims",
            BindingFlags.Instance | BindingFlags.Public,
            [typeof(string), typeof(string), typeof(string)]).ShouldNotBeNull();

        claims.Invoke(policy, [domain, commandType, key]).ShouldBe(expected);
    }

    [Theory]
    [InlineData(nameof(CreateAgent))]
    [InlineData(nameof(DisableAgent))]
    [InlineData(nameof(ConfigureAgentResponseMode))]
    [InlineData(nameof(ActivateAgent))]
    public void Reserved_extension_policy_rejects_missing_duplicate_or_foreign_identity_claims(string commandType)
    {
        ITrustedCommandExtensionPolicy policy = Policy();
        SubmitCommandRequest request = Request("agent", commandType);
        policy.Accepts(
            Principal(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId),
            request,
            AgentSetupTrustedExtensions.AgentAdministrator,
            "true").ShouldBeTrue();
        var missingClaim = new ClaimsPrincipal(new ClaimsIdentity(
            [new Claim("sub", "system:agents")],
            DaprInternalAuthenticationOptions.SchemeName));
        var duplicateClaim = new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim("sub", "system:agents"),
                new Claim("dapr_caller_app_id", AgentsAppId),
                new Claim("dapr_caller_app_id", AgentsAppId),
            ],
            DaprInternalAuthenticationOptions.SchemeName));
        var claimOnAnotherIdentity = new ClaimsPrincipal(
        [
            new ClaimsIdentity([new Claim("sub", "system:agents")], DaprInternalAuthenticationOptions.SchemeName),
            new ClaimsIdentity([new Claim("dapr_caller_app_id", AgentsAppId)], "Bearer"),
        ]);
        var additionalAuthenticatedIdentity = new ClaimsPrincipal(
        [
            new ClaimsIdentity(
            [
                new Claim("sub", "system:agents"),
                new Claim("dapr_caller_app_id", AgentsAppId),
            ],
            DaprInternalAuthenticationOptions.SchemeName),
            new ClaimsIdentity([new Claim("sub", "public-user")], "Bearer"),
        ]);
        var mixedAppId = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("sub", "system:agents"),
            new Claim("dapr_caller_app_id", AgentsAppId),
            new Claim("dapr_caller_app_id", "other-app"),
        ],
        DaprInternalAuthenticationOptions.SchemeName));

        foreach (ClaimsPrincipal principal in new[]
        {
            missingClaim,
            duplicateClaim,
            claimOnAnotherIdentity,
            additionalAuthenticatedIdentity,
            mixedAppId,
        })
        {
            policy.Accepts(
                principal,
                request,
                AgentSetupTrustedExtensions.AgentAdministrator,
                "true").ShouldBeFalse();
        }
    }

    [Fact]
    public async Task RegisteredAutomaticAdaptersIgnoreRevisionRestampsAndRetainBusinessProtectedAndAuthorityIntent()
    {
        object[] commands = await AutomaticCommandsAsync();
        var p = ((RegisterInteractionPermit)commands[0]).Permit;
        var registry = Registry();
        foreach (object command in commands)
        {
            string domain = command is CreatePermittedAgentInteraction or RecordAutomaticInteractionStep ? "agent-interaction" : ConversationAgentStateIdentity.Domain;
            string authority = command is EnterConversationDeletionClosing ? ConversationAgentStateIdentity.DeletionAuthority : ConversationAgentStateIdentity.WorkflowAuthority;
            var payload = JsonSerializer.SerializeToNode(command, command.GetType())!.AsObject();
            var submission = new SubmitCommand("message", p.TenantId, domain, domain == "agent-interaction" ? p.AgentInteractionId : ConversationAgentStateIdentity.Directory(p.TenantId, p.ConversationId),
                command.GetType().Name, JsonSerializer.SerializeToUtf8Bytes(payload), "correlation", "workflow", new() { [authority] = "true" });
            byte[] original = registry.Resolve(submission).CanonicalIntent;
            var restamp = payload.DeepClone().AsObject(); restamp["ExpectedRevision"] = 100;
            if (restamp["Fact"] is JsonObject f) { f["Revision"] = 101; }
            registry.Resolve(submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(restamp) }).CanonicalIntent.ShouldBe(original);
            var changed = payload.DeepClone().AsObject();
            if (changed["Permit"] is JsonObject permit) { permit["RequestDigest"] = new string('f', 64); }
            else if (changed["Lease"] is JsonObject acquired) { acquired["OperationIdentity"] = new string('f', 64); }
            else if (changed["Fact"] is JsonObject recorded) { recorded["OperationId"] = new string('f', 64); }
            else if (changed.ContainsKey("DeletionRequestId")) { changed["DeletionRequestId"] = new string('f', 64); }
            else { changed["AgentInteractionId"] = "different-interaction"; }
            registry.Resolve(submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(changed) }).CanonicalIntent.ShouldNotBe(original);
            registry.Resolve(submission with { Extensions = new() { [authority] = "false" } }).CanonicalIntent.ShouldNotBe(original);
            if (payload["Permit"] is JsonObject)
            {
                var protectedChange = payload.DeepClone().AsObject();
                protectedChange["Permit"]!["Request"]!["Value"] = JsonSerializer.SerializeToNode(new { FixtureSealed = "changed-ciphertext" });
                registry.Resolve(submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(protectedChange) }).CanonicalIntent.ShouldNotBe(original);
            }
            if (payload["Fact"]?["OutputAuthority"] is JsonObject)
            {
                var authorityChange = payload.DeepClone().AsObject(); authorityChange["Fact"]!["OutputAuthority"]!["FailureStatus"] = nameof(AgentInteractionStatus.SafetyFailed);
                registry.Resolve(submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(authorityChange) }).CanonicalIntent.ShouldNotBe(original);
            }
        }
    }

    [Theory]
    [InlineData("PascalCase")]
    [InlineData("camelCase")]
    [InlineData("mixedCase")]
    public async Task RegisteredAutomaticAdaptersBindEveryDeclaredCasingAndPreserveOpaqueProtectedValues(string casing)
    {
        var registry = Registry();
        foreach (var scenario in await AutomaticCasesAsync())
        {
            object command = scenario.Command;
            var payload = JsonSerializer.SerializeToNode(command, command.GetType())!.AsObject();
            var submission = AutomaticSubmission(command, payload);
            byte[] original = registry.Resolve(submission).CanonicalIntent;
            var alternate = SpellDeclaredMembers(payload, casing);
            byte[] canonical = registry.Resolve(submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(alternate) }).CanonicalIntent;
            canonical.ShouldBe(original, command.GetType().Name + "/" + casing);
            await AssertAutomaticExecutionAsync(command, scenario.State, submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(alternate) });
            var changed = payload.DeepClone().AsObject();
            if (changed["Permit"] is JsonObject permit) { permit["RequestDigest"] = new string('f', 64); }
            else if (changed["Lease"] is JsonObject lease) { lease["OperationIdentity"] = new string('f', 64); }
            else if (changed["Fact"] is JsonObject fact) { fact["OperationId"] = new string('f', 64); }
            else if (changed.ContainsKey("DeletionRequestId")) { changed["DeletionRequestId"] = new string('f', 64); }
            else { changed["AgentInteractionId"] = "changed-business-id"; }
            registry.Resolve(submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(SpellDeclaredMembers(changed, casing)) }).CanonicalIntent.ShouldNotBe(original);
            foreach (string protectedMember in new[] { "Permit", "Fact" })
            {
                string contentMember = protectedMember == "Permit" ? "Request" : "Output";
                if (payload[protectedMember]?[contentMember] is not JsonObject) { continue; }
                var opaqueCasingChange = payload.DeepClone().AsObject();
                var opaque = opaqueCasingChange[protectedMember]![contentMember]!["Value"]!.AsObject();
                JsonNode? sealedValue = opaque["FixtureSealed"]; opaque.Remove("FixtureSealed"); opaque["fixtureSealed"] = sealedValue;
                registry.Resolve(submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(SpellDeclaredMembers(opaqueCasingChange, casing)) }).CanonicalIntent.ShouldNotBe(original);
                var opaqueChange = payload.DeepClone().AsObject();
                opaqueChange[protectedMember]![contentMember]!["Value"]!["FixtureSealed"] = "changed-ciphertext";
                registry.Resolve(submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(SpellDeclaredMembers(opaqueChange, casing)) }).CanonicalIntent.ShouldNotBe(original);
                var opaqueRevisionChange = payload.DeepClone().AsObject();
                opaqueRevisionChange[protectedMember]![contentMember]!["Value"]!["ExpectedRevision"] = 38;
                registry.Resolve(submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(SpellDeclaredMembers(opaqueRevisionChange, casing)) }).CanonicalIntent.ShouldNotBe(original);
                opaqueRevisionChange = payload.DeepClone().AsObject();
                opaqueRevisionChange[protectedMember]![contentMember]!["Value"]!["nested"]!["Fact"]!["Revision"] = 29;
                registry.Resolve(submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(SpellDeclaredMembers(opaqueRevisionChange, casing)) }).CanonicalIntent.ShouldNotBe(original);
            }
            var invalid = payload.DeepClone().AsObject(); invalid["ExpectedRevision"] = new JsonObject { ["Invalid"] = 1 };
            foreach (byte[] malformed in new[] { "{"u8.ToArray(), "[]"u8.ToArray(), JsonSerializer.SerializeToUtf8Bytes(SpellDeclaredMembers(invalid, casing)) })
            {
                var malformedSubmission = submission with { Payload = malformed };
                Should.Throw<JsonException>(() => registry.Resolve(malformedSubmission));
                await Should.ThrowAsync<JsonException>(() => ExecuteAutomaticAsync(malformedSubmission, scenario.State));
            }
            var nullSubmission = submission with { Payload = "null"u8.ToArray() };
            Should.Throw<ArgumentException>(() => registry.Resolve(nullSubmission)).Message.ShouldStartWith("InvalidCommandPayload");
            (await Should.ThrowAsync<InvalidOperationException>(() => ExecuteAutomaticAsync(nullSubmission, scenario.State)))
                .Message.ShouldStartWith("Failed to deserialize payload");
        }
    }

    [Theory]
    [InlineData("PascalCase")]
    [InlineData("camelCase")]
    [InlineData("mixedCase")]
    public async Task RegisteredAutomaticAdaptersRetainEachBusinessRevisionUnderEveryWireSpelling(string casing)
    {
        object[] commands = await AutomaticCommandsAsync();
        var registry = Registry();
        (string CommandType, string? Parent, string Member)[] revisions =
        [
            (nameof(AcquireConversationEffectLease), "Lease", "CommitRevision"),
            (nameof(AcquireConversationEffectLease), "Lease", "ResultRevision"),
            (nameof(SettleConversationEffectLease), null, "CommitRevision"),
            (nameof(SettleConversationEffectLease), null, "ResultRevision"),
            (nameof(CreatePermittedAgentInteraction), null, "PermitRevision"),
            (nameof(AcknowledgeInteractionCreation), null, "InteractionCreationRevision"),
            (nameof(RecordAutomaticInteractionStep), "Fact", "LeaseCommitRevision"),
        ];
        foreach (var revision in revisions)
        {
            object command = commands.First(c => c.GetType().Name == revision.CommandType);
            var payload = JsonSerializer.SerializeToNode(command, command.GetType())!.AsObject();
            var submission = AutomaticSubmission(command, SpellDeclaredMembers(payload, casing));
            byte[] original = registry.Resolve(submission).CanonicalIntent;
            var changed = payload.DeepClone().AsObject();
            JsonObject owner = revision.Parent is null ? changed : changed[revision.Parent]!.AsObject();
            owner[revision.Member] = owner[revision.Member]!.GetValue<long>() + 1;
            registry.Resolve(submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(SpellDeclaredMembers(changed, casing)) }).CanonicalIntent.ShouldNotBe(original, revision.CommandType + "/" + revision.Member);
            registry.Resolve(submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(SpellDeclaredMembers(QuoteDeclaredRevisions(changed), casing)) }).CanonicalIntent
                .ShouldBe(registry.Resolve(submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(SpellDeclaredMembers(changed, casing)) }).CanonicalIntent);
            foreach (string malformed in new[] { "", "not-a-number", "1.5", "9223372036854775808" })
            {
                var invalid = payload.DeepClone().AsObject();
                (revision.Parent is null ? invalid : invalid[revision.Parent]!.AsObject())[revision.Member] = malformed;
                var refused = submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(SpellDeclaredMembers(invalid, casing)) };
                Should.Throw<JsonException>(() => registry.Resolve(refused));
                await Should.ThrowAsync<JsonException>(() => ExecuteAutomaticAsync(refused, null));
            }
            var restamped = payload.DeepClone().AsObject(); restamped["ExpectedRevision"] = 999;
            registry.Resolve(submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(SpellDeclaredMembers(restamped, casing)) }).CanonicalIntent.ShouldBe(original);
            if (payload["Fact"] is JsonObject)
            {
                restamped = payload.DeepClone().AsObject(); restamped["Fact"]!["Revision"] = 1000;
                registry.Resolve(submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(SpellDeclaredMembers(restamped, casing)) }).CanonicalIntent.ShouldBe(original);
            }
        }
    }

    [Theory]
    [InlineData("PascalCase")]
    [InlineData("camelCase")]
    [InlineData("mixedCase")]
    public async Task RegisteredAutomaticAdaptersAgreeWithSdkOnQuotedBusinessAndTransportRevisions(string casing)
    {
        var registry = Registry();
        foreach (var scenario in await AutomaticCasesAsync())
        {
            var payload = JsonSerializer.SerializeToNode(scenario.Command, scenario.Command.GetType())!.AsObject();
            var submission = AutomaticSubmission(scenario.Command, payload);
            byte[] original = registry.Resolve(submission).CanonicalIntent;
            var quoted = submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(SpellDeclaredMembers(QuoteDeclaredRevisions(payload), casing)) };
            registry.Resolve(quoted).CanonicalIntent.ShouldBe(original);
            await AssertAutomaticExecutionAsync(scenario.Command, scenario.State, quoted);
            var restamped = payload.DeepClone().AsObject(); restamped["ExpectedRevision"] = 999;
            if (restamped["Fact"] is JsonObject fact) { fact["Revision"] = 1000; }
            registry.Resolve(submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(SpellDeclaredMembers(QuoteDeclaredRevisions(restamped), casing)) }).CanonicalIntent.ShouldBe(original);
            foreach (string malformed in new[] { "", "not-a-number", "1.5", "9223372036854775808" })
            {
                var invalid = payload.DeepClone().AsObject(); invalid["ExpectedRevision"] = malformed;
                var refused = submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(SpellDeclaredMembers(invalid, casing)) };
                Should.Throw<JsonException>(() => registry.Resolve(refused));
                await Should.ThrowAsync<JsonException>(() => ExecuteAutomaticAsync(refused, scenario.State));
                if (payload["Fact"] is JsonObject)
                {
                    invalid = payload.DeepClone().AsObject(); invalid["Fact"]!["Revision"] = malformed;
                    refused = submission with { Payload = JsonSerializer.SerializeToUtf8Bytes(SpellDeclaredMembers(invalid, casing)) };
                    Should.Throw<JsonException>(() => registry.Resolve(refused));
                    await Should.ThrowAsync<JsonException>(() => ExecuteAutomaticAsync(refused, scenario.State));
                }
            }
        }
    }

    [Fact]
    public async Task RegisteredAutomaticAdaptersExcludeAdmittedDeliveryNoncesButRetainActorAuthority()
    {
        const string key = "trust:deliveryNonce";
        var registry = Registry();
        foreach (object command in await AutomaticCommandsAsync())
        {
            var submission = AutomaticSubmission(command, JsonSerializer.SerializeToNode(command, command.GetType())!.AsObject());
            byte[] original = registry.Resolve(submission).CanonicalIntent;
            var proof = Substitute.For<IAgentsTrustedCommandProofReader>();
            var services = new ServiceCollection(); services.AddAgentsEventStore(AgentsAppId, proof);
            using var provider = services.BuildServiceProvider(); var policy = provider.GetRequiredService<ITrustedCommandExtensionPolicy>();
            var principal = Principal(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId);
            foreach (string nonce in new[] { "nonce-1", "nonce-2" })
            {
                var extensions = new Dictionary<string, string>(submission.Extensions!, StringComparer.Ordinal) { [key] = nonce };
                var request = Request(submission.Domain, submission.CommandType) with
                { AggregateId = submission.AggregateId, Payload = JsonSerializer.Deserialize<JsonElement>(submission.Payload), Extensions = extensions };
                proof.HasAdmittedProof(Arg.Is<ClaimsPrincipal>(p => ReferenceEquals(p, principal)), Arg.Is<SubmitCommandRequest>(r => ReferenceEquals(r, request)), key, nonce).Returns(true);
                policy.Accepts(principal, request, key, nonce).ShouldBeTrue();
                registry.Resolve(submission with { Extensions = extensions }).CanonicalIntent.ShouldBe(original);
                string authority = command is EnterConversationDeletionClosing ? ConversationAgentStateIdentity.DeletionAuthority : ConversationAgentStateIdentity.WorkflowAuthority;
                extensions = new Dictionary<string, string>(extensions, StringComparer.Ordinal) { [authority] = "false" };
                registry.Resolve(submission with { Extensions = extensions }).CanonicalIntent.ShouldNotBe(original);
            }
        }
    }

    private static JsonObject QuoteDeclaredRevisions(JsonObject payload)
    {
        var quoted = payload.DeepClone().AsObject();
        foreach (var member in payload)
        {
            if (member.Key == "Value") { continue; }
            if (member.Value is JsonObject child) { quoted[member.Key] = QuoteDeclaredRevisions(child); }
            else if (member.Value is not null && member.Key.EndsWith("Revision", StringComparison.Ordinal))
            { quoted[member.Key] = member.Value.ToJsonString(); }
        }
        return quoted;
    }

    private static CommandEnvelope AutomaticEnvelope(SubmitCommand submission) => new(submission.MessageId, submission.Tenant, submission.Domain,
        submission.AggregateId, submission.CommandType, submission.Payload, submission.CorrelationId, null, submission.UserId, submission.Extensions);

    private static Task<DomainResult> ExecuteAutomaticAsync(SubmitCommand submission, object? state)
        => submission.Domain == ConversationAgentStateIdentity.Domain
            ? new ConversationAgentStateAggregate().ProcessAsync(AutomaticEnvelope(submission), state)
            : new AgentInteractionAggregate().ProcessAsync(AutomaticEnvelope(submission), state);

    private static async Task AssertAutomaticExecutionAsync(object declared, object? state, SubmitCommand submission)
    {
        object bound = JsonSerializer.Deserialize(submission.Payload, declared.GetType(), EventStorePayloadSerialization.Options).ShouldNotBeNull();
        JsonSerializer.Serialize(bound, bound.GetType()).ShouldBe(JsonSerializer.Serialize(declared, declared.GetType()));
        var envelope = AutomaticEnvelope(submission);
        DomainResult expected = submission.Domain == ConversationAgentStateIdentity.Domain
            ? ConversationAgentStateAggregate.Handle((dynamic)declared, (ConversationAgentStateState?)state, envelope)
            : AgentInteractionAggregate.Handle((dynamic)declared, (AgentInteractionState?)state, envelope);
        expected.IsRejection.ShouldBeFalse(); expected.IsNoOp.ShouldBeFalse(); expected.Events.ShouldNotBeEmpty();
        DomainResult executed = await ExecuteAutomaticAsync(submission, state);
        executed.IsRejection.ShouldBeFalse(); executed.IsNoOp.ShouldBeFalse();
        executed.Events.Select(e => JsonSerializer.Serialize(e, e.GetType())).ToArray()
            .ShouldBe(expected.Events.Select(e => JsonSerializer.Serialize(e, e.GetType())).ToArray());
        object replayed = submission.Domain == ConversationAgentStateIdentity.Domain
            ? state is null ? new ConversationAgentStateState() : JsonSerializer.Deserialize<ConversationAgentStateState>(JsonSerializer.Serialize(state)).ShouldNotBeNull()
            : state is null ? new AgentInteractionState() : JsonSerializer.Deserialize<AgentInteractionState>(JsonSerializer.Serialize(state)).ShouldNotBeNull();
        string before = JsonSerializer.Serialize(replayed, replayed.GetType());
        foreach (var item in executed.Events)
        {
            if (replayed is ConversationAgentStateState directory) { directory.Apply((dynamic)item); }
            else { ((AgentInteractionState)replayed).Apply((dynamic)item); }
        }
        JsonSerializer.Serialize(replayed, replayed.GetType()).ShouldNotBe(before);
    }

    private static JsonObject SpellDeclaredMembers(JsonObject payload, string casing)
    {
        var spelled = new JsonObject();
        foreach (var member in payload)
        {
            string key = casing switch
            {
                "camelCase" => char.ToLowerInvariant(member.Key[0]) + member.Key[1..],
                "mixedCase" => string.Concat(member.Key.Select((c, i) => i % 2 == 0 ? char.ToLowerInvariant(c) : char.ToUpperInvariant(c))),
                _ => member.Key,
            };
            // ProtectedContent.Value is opaque JSON; only declared contract members are respelled.
            spelled[key] = member.Key != "Value" && member.Value is JsonObject child ? SpellDeclaredMembers(child, casing) : member.Value?.DeepClone();
        }
        return spelled;
    }

    private static SubmitCommand AutomaticSubmission(object command, JsonObject payload)
    {
        var fixture = new AutomaticInteractionReplayFixture();
        var p = fixture.Request;
        string domain = command is CreatePermittedAgentInteraction or RecordAutomaticInteractionStep ? "agent-interaction" : ConversationAgentStateIdentity.Domain;
        string authority = command is EnterConversationDeletionClosing ? ConversationAgentStateIdentity.DeletionAuthority : ConversationAgentStateIdentity.WorkflowAuthority;
        return new SubmitCommand("message", p.TenantId, domain,
            domain == "agent-interaction" ? fixture.InteractionId : ConversationAgentStateIdentity.Directory(p.TenantId, p.ConversationId), command.GetType().Name,
            JsonSerializer.SerializeToUtf8Bytes(payload), "correlation", "workflow", new() { [authority] = "true" });
    }

    [Theory]
    [InlineData("conversation-agent-state", nameof(RegisterInteractionPermit), ConversationAgentStateIdentity.WorkflowAuthority)]
    [InlineData("conversation-agent-state", nameof(AcknowledgeInteractionCreation), ConversationAgentStateIdentity.WorkflowAuthority)]
    [InlineData("conversation-agent-state", nameof(AcquireConversationEffectLease), ConversationAgentStateIdentity.WorkflowAuthority)]
    [InlineData("conversation-agent-state", nameof(CommitConversationEffect), ConversationAgentStateIdentity.WorkflowAuthority)]
    [InlineData("conversation-agent-state", nameof(SettleConversationEffectLease), ConversationAgentStateIdentity.WorkflowAuthority)]
    [InlineData("conversation-agent-state", nameof(CancelConversationEffectLease), ConversationAgentStateIdentity.WorkflowAuthority)]
    [InlineData("conversation-agent-state", nameof(EnterConversationDeletionClosing), ConversationAgentStateIdentity.DeletionAuthority)]
    [InlineData("agent-interaction", nameof(CreatePermittedAgentInteraction), ConversationAgentStateIdentity.WorkflowAuthority)]
    [InlineData("agent-interaction", nameof(RecordAutomaticInteractionStep), ConversationAgentStateIdentity.WorkflowAuthority)]
    public void AutomaticAuthorityRequiresExactAdmittedPrincipalCommandKeyAndValue(string domain, string commandType, string key)
    {
        ArgumentNullException.ThrowIfNull(key);
        var proof = Substitute.For<IAgentsTrustedCommandProofReader>(); var services = new ServiceCollection();
        services.AddAgentsEventStore(AgentsAppId, proof); using var provider = services.BuildServiceProvider();
        var policy = provider.GetRequiredService<ITrustedCommandExtensionPolicy>();
        var principal = Principal(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId); var request = Request(domain, commandType);
        policy.Accepts(principal, request, key, "true").ShouldBeFalse();
        proof.HasAdmittedProof(Arg.Is<ClaimsPrincipal>(p => ReferenceEquals(p, principal)), Arg.Is<SubmitCommandRequest>(r => ReferenceEquals(r, request)), key, "true").Returns(true);
        policy.Accepts(principal, request, key, "true").ShouldBeTrue();
        policy.Accepts(principal, request with { AggregateId = "another-owner" }, key, "true").ShouldBeFalse();
        policy.Accepts(principal, request, key, "True").ShouldBeFalse();
        policy.Accepts(principal, request, key, "false").ShouldBeFalse();
        policy.Accepts(principal, request, key == ConversationAgentStateIdentity.WorkflowAuthority ? ConversationAgentStateIdentity.DeletionAuthority : ConversationAgentStateIdentity.WorkflowAuthority, "true").ShouldBeFalse();
        policy.Accepts(Principal("Bearer", AgentsAppId), request, key, "true").ShouldBeFalse();
        policy.Accepts(Principal(DaprInternalAuthenticationOptions.SchemeName, "other-app"), request, key, "true").ShouldBeFalse();
        var claims = policy.GetType().GetMethod("Claims", BindingFlags.Instance | BindingFlags.Public)!.Invoke(policy, [domain, commandType, key]); claims.ShouldBe(true);
        policy.GetType().GetMethod("Claims", BindingFlags.Instance | BindingFlags.Public)!.Invoke(policy, ["other-domain", commandType, key]).ShouldBe(false);
        policy.GetType().GetMethod("Claims", BindingFlags.Instance | BindingFlags.Public)!.Invoke(policy, [domain, commandType, key.ToUpperInvariant()]).ShouldBe(false);
        policy.GetType().GetMethod("Claims", BindingFlags.Instance | BindingFlags.Public)!.Invoke(policy, [domain, "OtherCommand", key]).ShouldBe(false);
    }

    [Theory]
    [InlineData("conversation-agent-state", nameof(RegisterInteractionPermit))]
    [InlineData("conversation-agent-state", nameof(AcknowledgeInteractionCreation))]
    [InlineData("conversation-agent-state", nameof(AcquireConversationEffectLease))]
    [InlineData("conversation-agent-state", nameof(CommitConversationEffect))]
    [InlineData("conversation-agent-state", nameof(SettleConversationEffectLease))]
    [InlineData("conversation-agent-state", nameof(CancelConversationEffectLease))]
    [InlineData("conversation-agent-state", nameof(EnterConversationDeletionClosing))]
    [InlineData("agent-interaction", nameof(CreatePermittedAgentInteraction))]
    [InlineData("agent-interaction", nameof(RecordAutomaticInteractionStep))]
    public void AutomaticDeliveryMetadataRequiresGovernedRouteExactProofAndAuthenticatedAgentsWorkload(string domain, string commandType)
    {
        const string key = "trust:deliveryNonce";
        const string nonce = "delivery-nonce";
        var proof = Substitute.For<IAgentsTrustedCommandProofReader>();
        var services = new ServiceCollection(); services.AddAgentsEventStore(AgentsAppId, proof);
        using var provider = services.BuildServiceProvider(); var policy = provider.GetRequiredService<ITrustedCommandExtensionPolicy>();
        var principal = Principal(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId);
        var request = Request(domain, commandType);
        var claims = policy.GetType().GetMethod("Claims", BindingFlags.Instance | BindingFlags.Public)!;
        claims.Invoke(policy, [domain, commandType, key]).ShouldBe(true);
        claims.Invoke(policy, ["other-domain", commandType, key]).ShouldBe(false);
        claims.Invoke(policy, [domain, "OtherCommand", key]).ShouldBe(false);
        claims.Invoke(policy, [domain, commandType, "TRUST:deliveryNonce"]).ShouldBe(false);
        claims.Invoke(policy, [domain, commandType, "trust:"]).ShouldBe(false);
        policy.Accepts(principal, request, key, nonce).ShouldBeFalse();
        void Admit(ClaimsPrincipal actor, SubmitCommandRequest submission, string extensionKey, string value)
            => proof.HasAdmittedProof(Arg.Is<ClaimsPrincipal>(p => ReferenceEquals(p, actor)), Arg.Is<SubmitCommandRequest>(r => ReferenceEquals(r, submission)), extensionKey, value).Returns(true);
        Admit(principal, request, key, nonce);
        policy.Accepts(principal, request, key, nonce).ShouldBeTrue();
        // The proof binds all four exact inputs; a changed admitted delivery is not reusable.
        policy.Accepts(Principal(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId), request, key, nonce).ShouldBeFalse();
        policy.Accepts(principal, request with { AggregateId = "another-owner" }, key, nonce).ShouldBeFalse();
        policy.Accepts(principal, request with { Payload = JsonSerializer.SerializeToElement(new { Changed = true }) }, key, nonce).ShouldBeFalse();
        policy.Accepts(principal, request, key, "another-nonce").ShouldBeFalse();
        policy.Accepts(principal, request, "trust:DeliveryNonce", nonce).ShouldBeFalse();
        foreach (string value in new[] { "", " ", "\t\r\n" })
        {
            Admit(principal, request, key, value);
            policy.Accepts(principal, request, key, value).ShouldBeFalse();
        }
        foreach (string invalidKey in new[] { "TRUST:deliveryNonce", "trust:" })
        {
            Admit(principal, request, invalidKey, nonce);
            policy.Accepts(principal, request, invalidKey, nonce).ShouldBeFalse();
        }
        foreach (var wrongRoute in new[] { request with { Domain = "other-domain" }, request with { CommandType = "OtherCommand" } })
        {
            Admit(principal, wrongRoute, key, nonce);
            policy.Accepts(principal, wrongRoute, key, nonce).ShouldBeFalse();
        }
        foreach (var caller in new[]
        {
            Principal("Bearer", AgentsAppId), Principal(DaprInternalAuthenticationOptions.SchemeName, "other-app"),
            Principal(null!, AgentsAppId), new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "system:agents")], DaprInternalAuthenticationOptions.SchemeName)),
        })
        {
            Admit(caller, request, key, nonce);
            policy.Accepts(caller, request, key, nonce).ShouldBeFalse();
        }
        policy.Accepts(principal, request, key, nonce).ShouldBeTrue();
    }

    private static async Task<object[]> AutomaticCommandsAsync() => (await AutomaticCasesAsync()).Select(c => c.Command).ToArray();

    private static async Task<(object Command, object? State)[]> AutomaticCasesAsync()
    {
        var fixture = new AutomaticInteractionReplayFixture(); await fixture.StartAsync();
        var p = fixture.Directory!.Permits[fixture.InteractionId].Permit;
        p = p with { Request = p.Request with { Value = JsonSerializer.SerializeToElement(new
        {
            FixtureSealed = "request", ExpectedRevision = 37,
            nested = new { Snapshot = "opaque", Fact = new { Revision = 28 } },
            Values = new[] { new { MixedKey = "kept" } },
        }) } };
        var cases = new List<(object Command, object? State)>();
        var register = new RegisterInteractionPermit(p, 0); cases.Add((register, null));
        var directory = new ConversationAgentStateState();
        void AcceptDirectory(object command)
        {
            var submission = AutomaticSubmission(command, JsonSerializer.SerializeToNode(command, command.GetType())!.AsObject());
            DomainResult accepted = ConversationAgentStateAggregate.Handle((dynamic)command, directory, AutomaticEnvelope(submission));
            accepted.IsRejection.ShouldBeFalse(); accepted.IsNoOp.ShouldBeFalse();
            foreach (var item in accepted.Events) { directory.Apply((dynamic)item); }
        }
        object CopyDirectory() => JsonSerializer.Deserialize<ConversationAgentStateState>(JsonSerializer.Serialize(directory)).ShouldNotBeNull();
        AcceptDirectory(register);
        cases.Add((new AcknowledgeInteractionCreation(p.AgentInteractionId, p.PermitId, p.CreationOutboxId, 1, directory.Revision), CopyDirectory()));
        cases.Add((new EnterConversationDeletionClosing(p.TenantId, p.ConversationId, new string('e', 64), directory.Revision), CopyDirectory()));
        var lease = fixture.Directory.Leases.Values.Single() with { Status = ConversationEffectLeaseStatus.Reserved, CommitRevision = 0, ResultRevision = 0, MustSettle = false };
        var acquire = new AcquireConversationEffectLease(lease, directory.Revision); cases.Add((acquire, CopyDirectory())); AcceptDirectory(acquire);
        cases.Add((new CancelConversationEffectLease(lease.LeaseId, p.AgentInteractionId, p.AgentInteractionId, directory.Revision), CopyDirectory()));
        var commit = new CommitConversationEffect(lease.LeaseId, p.AgentInteractionId, p.AgentInteractionId, directory.Revision);
        cases.Add((commit, CopyDirectory())); AcceptDirectory(commit);
        cases.Add((new SettleConversationEffectLease(lease.LeaseId, p.AgentInteractionId, p.AgentInteractionId, directory.Leases[lease.LeaseId].CommitRevision, 11, directory.Revision), CopyDirectory()));
        cases.Add((new CreatePermittedAgentInteraction(p, 1, 0), null));
        fixture.Effects.GenerationResultOverride = new(AutomaticInteractionOutcome.Allowed, Output: p.Request, ContentDigest: new string('d', 64), DigestKeyVersion: p.DigestKeyVersion);
        fixture.BeforeDispatchAsync = intent =>
        {
            if (intent.CommandType == nameof(RecordAutomaticInteractionStep))
            {
                var command = JsonSerializer.Deserialize<RecordAutomaticInteractionStep>(intent.Payload, EventStorePayloadSerialization.Options).ShouldNotBeNull();
                if (command.Fact.Step is AutomaticInteractionStep.ProviderInvocationAuthorized or AutomaticInteractionStep.GeneratedVersionRecorded)
                {
                    var state = JsonSerializer.Deserialize<AgentInteractionState>(JsonSerializer.Serialize(fixture.Interaction)).ShouldNotBeNull();
                    var submission = AutomaticSubmission(command, JsonSerializer.SerializeToNode(command)!.AsObject());
                    var accepted = AgentInteractionAggregate.Handle(command, state, AutomaticEnvelope(submission));
                    accepted.IsRejection.ShouldBeFalse(); accepted.IsNoOp.ShouldBeFalse();
                    foreach (var item in accepted.Events) { state.Apply((dynamic)item); }
                    cases.Add((command, JsonSerializer.Deserialize<AgentInteractionState>(JsonSerializer.Serialize(fixture.Interaction)).ShouldNotBeNull()));
                }
            }
            return Task.CompletedTask;
        };
        await fixture.AdvanceUntilAsync(AutomaticInteractionStep.GeneratedVersionRecorded);
        cases.Select(c => c.Command.GetType().Name).Distinct(StringComparer.Ordinal).Count().ShouldBe(9);
        return cases.ToArray();
    }

    private static IdempotencyIntentAdapterRegistry Registry(string tenantId = "tenant-a")
    {
        ServiceCollection services = new();
        _ = services.AddAgentsEventStore(AgentsAppId, new FixtureTrustedCommandProofReader());
        using ServiceProvider provider = services.BuildServiceProvider();
#if HEXALITH_EVENTSTORE_SOURCE
        var catalog = new FixtureAgentRouteCatalog(tenantId);
        return new IdempotencyIntentAdapterRegistry(catalog,
            provider.GetServices<IIdempotencyIntentAdapter>().Select(adapter => new FixtureDeclaredAgentIntentAdapter(adapter)).ToArray(),
            new CanonicalIdempotencyIntentEncoder());
#else
        return new IdempotencyIntentAdapterRegistry(provider.GetServices<IIdempotencyIntentAdapter>().ToArray(),
            new CanonicalIdempotencyIntentEncoder());
#endif
    }

    private static ITrustedCommandExtensionPolicy Policy()
    {
        ServiceCollection services = new();
        _ = services.AddAgentsEventStore(AgentsAppId, new FixtureTrustedCommandProofReader());
        using ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<ITrustedCommandExtensionPolicy>();
    }

    private static SubmitCommand ActivationCommand(
        string payload,
        string version,
        string providerVerdict,
        string approverVerdict,
        string administratorAuthorization = "true")
        => new(
            MessageId: "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            Tenant: "tenant-a",
            Domain: "agent",
            AggregateId: "agent-a",
            CommandType: nameof(ActivateAgent),
            Payload: Encoding.UTF8.GetBytes(payload),
            CorrelationId: "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            UserId: "system:agents",
            Extensions: new Dictionary<string, string>
            {
                [AgentSetupTrustedExtensions.AgentAdministrator] = administratorAuthorization,
                [AgentSetupTrustedExtensions.ProviderSelectionValidation] = providerVerdict,
                [AgentSetupTrustedExtensions.ApproverPolicyValidation] = approverVerdict,
                [AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion] = version,
            },
            IsGlobalAdmin: true,
            IdempotencyKey: "01ARZ3NDEKTSV4RRFFQ69G5FAV");

    private static SubmitCommand UpdateCommand(string payload)
        => ResponseModeCommand(payload) with
        {
            CommandType = nameof(UpdateAgentConfiguration),
        };

    private static SubmitCommand CreateCommand(string payload)
        => StandardCommand(nameof(CreateAgent), "true") with
        {
            Payload = Encoding.UTF8.GetBytes(payload),
        };

    private static SubmitCommand ResponseModeCommand(string payload)
        => StandardCommand(nameof(ConfigureAgentResponseMode), "true") with
        {
            Payload = Encoding.UTF8.GetBytes(payload),
        };

    private static SubmitCommand StandardCommand(string commandType, string administratorAuthorization)
        => ActivationCommand(
            payload: "{\"value\":1}",
            version: "7",
            providerVerdict: nameof(ProviderSelectionValidationStatus.Valid),
            approverVerdict: nameof(ApproverPolicyValidationStatus.Valid),
            administratorAuthorization: administratorAuthorization) with
        {
            CommandType = commandType,
            Extensions = new Dictionary<string, string>
            {
                [AgentSetupTrustedExtensions.AgentAdministrator] = administratorAuthorization,
            },
        };

    private static ClaimsPrincipal Principal(string authenticationType, string appId)
        => new(new ClaimsIdentity(
        [
            new Claim("sub", $"system:{appId}"),
            new Claim("dapr_caller_app_id", appId),
        ],
        authenticationType));

    private static SubmitCommandRequest Request(string domain, string commandType)
        => new(
            MessageId: "01ARZ3NDEKTSV4RRFFQ69G5FAV",
            Tenant: "tenant-a",
            Domain: domain,
            AggregateId: "agent-a",
            CommandType: commandType,
            Payload: JsonSerializer.SerializeToElement(new ActivateAgent()),
            CorrelationId: "01ARZ3NDEKTSV4RRFFQ69G5FAW",
            Extensions: null,
            IdempotencyKey: "01ARZ3NDEKTSV4RRFFQ69G5FAV");

    private static SubmitCommandRequest ActivationRequest(
        JsonElement payload,
        string version,
        string providerVerdict,
        string approverVerdict,
        string? administratorAuthorization = "true")
    {
        var extensions = new Dictionary<string, string>
        {
            [AgentSetupTrustedExtensions.ProviderSelectionValidation] = providerVerdict,
            [AgentSetupTrustedExtensions.ApproverPolicyValidation] = approverVerdict,
            [AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion] = version,
        };
        if (administratorAuthorization is not null)
        {
            extensions[AgentSetupTrustedExtensions.AgentAdministrator] = administratorAuthorization;
        }

        return Request("agent", nameof(ActivateAgent)) with
        {
            Payload = payload,
            Extensions = extensions,
        };
    }

    private static SubmitCommandRequest ResponseModeRequest(string json)
        => Request("agent", nameof(ConfigureAgentResponseMode)) with
        {
            Payload = JsonPayload(json),
            Extensions = new Dictionary<string, string>
            {
                [AgentSetupTrustedExtensions.AgentAdministrator] = "true",
            },
        };

    private static JsonElement JsonPayload(string json)
        => JsonSerializer.Deserialize<JsonElement>(json);

    private static SubmitCommand Submitted(SubmitCommandRequest request)
        => new(
            MessageId: request.MessageId,
            Tenant: request.Tenant,
            Domain: request.Domain,
            AggregateId: request.AggregateId,
            CommandType: request.CommandType,
            Payload: JsonSerializer.SerializeToUtf8Bytes(request.Payload),
            CorrelationId: string.IsNullOrWhiteSpace(request.CorrelationId) ? request.MessageId : request.CorrelationId,
            UserId: "system:agents",
            Extensions: request.Extensions is null
                ? null
                : new Dictionary<string, string>(request.Extensions, StringComparer.Ordinal),
            IsGlobalAdmin: true,
            IdempotencyKey: request.IdempotencyKey);

    private static StaticIdempotencyDigestKeyProvider CreateDigestKeyProvider()
        => new(
            "v1",
            new Dictionary<string, byte[]>(StringComparer.Ordinal)
            {
                ["v1"] = Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef"),
            },
            []);

    private static async Task<string> IntentDigestAsync(TrustedIdempotencyDescriptor descriptor)
    {
        IdempotencyProtectedIdentitySet identities = await new IdempotencyKeyProtector(CreateDigestKeyProvider())
            .ProtectAsync("tenant-a", "01ARZ3NDEKTSV4RRFFQ69G5FAV", descriptor)
            .ConfigureAwait(false);
        return identities.Active.IntentDigest;
    }

    private static WebApplication BuildDomainApp(Action<SubmitCommand> onExecute, Func<SubmitCommand, string?>? resultPayload = null)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Agents:EventStore:BaseUrl"] = "https://eventstore.example",
            ["Agents:EventStore:AppId"] = "eventstore",
        });
        builder.Services.AddSingleton<IAgentCommandDispatcher, DeferredAgentCommandDispatcher>();
        builder.Services.AddSingleton(AgentsClient.Unavailable());
        _ = builder.Services.AddAgentSetupServices(builder.Configuration);
        WebApplication app = builder.Build();
        app.MapPost("/process", (SubmitCommand command) =>
        {
            onExecute(command);
            return Results.Ok(new SubmitCommandResult(
                command.CorrelationId,
                resultPayload is null ? "{\"effect\":\"Applied\",\"configurationVersion\":8}" : resultPayload(command),
                command.MessageId));
        });
        return app;
    }

    private static HttpClient GatewayClient(WebApplication app)
    {
#if HEXALITH_EVENTSTORE_SOURCE
        return new HttpClient(new FixtureBoundAgentCommandHandler(app.GetTestServer().CreateHandler())) { BaseAddress = new Uri("http://localhost") };
#else
        return app.GetTestClient();
#endif
    }

#if HEXALITH_EVENTSTORE_SOURCE
    /// <summary>The SDK's human policy refuses a workload principal even when its operation policy would allow bound dispatch.</summary>
    [Fact]
    public async Task WorkloadPrincipalCannotSubmitOnTheHumanCommandSurface()
    {
        int executions = 0;
        await using WebApplication domain = BuildDomainApp(_ => executions++);
        await domain.StartAsync();
        using HttpClient domainClient = domain.GetTestClient();
        var ledger = new InMemoryAdmissionLedger(domainClient);
        await using WebApplication gateway = BuildGatewayApp(Principal(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId), ledger, true);
        await gateway.StartAsync();
        using HttpClient client = gateway.GetTestClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/commands", Request("agent", nameof(ActivateAgent)));

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Forbidden);
        executions.ShouldBe(0);
        ledger.ExecutionCount.ShouldBe(0);
    }
#endif

    /// <summary>The public SDK gateway never treats the historical provider storage partition as a public tenant.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task RawPlatformGovernanceTimestampsCannotBypassTheStorageScopeBoundary(bool update)
    {
        foreach (int offsetDays in new[] { -365, 365 })
        foreach (bool flags in new[] { false, true })
        {
            DateTimeOffset serverTime = DateTimeOffset.UtcNow;
            var terms = new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1, serverTime.AddDays(offsetDays));
            object payload = update
                ? new UpdateProviderModelEntry("provider", "model", "Model", true, 1000, 500, new(30000, 3),
                    ProviderModelCapabilityFlags.Streaming, "cfg-ref", new("USD", 0.002m, 0.008m, 1), 1, terms)
                : new CreateProviderModelEntry("provider", "model", "Model", true, true, 1000, 500, new(30000, 3),
                    ProviderModelCapabilityFlags.Streaming, "cfg-ref", new("USD", 0.002m, 0.008m, 1), terms);
            var request = Request("provider-catalog", update ? nameof(UpdateProviderModelEntry) : nameof(CreateProviderModelEntry)) with
            {
                Tenant = ProviderCatalogIdentity.PlatformTenantId,
                AggregateId = ProviderCatalogIdentity.EntryId("provider", "model"),
                Payload = JsonSerializer.SerializeToElement(payload, payload.GetType()),
                Extensions = flags ? new Dictionary<string, string> { ["actor:agentsProviderAdmin"] = "true" } : null,
            };
            var persisted = new List<SubmitCommand>();
            await using WebApplication domain = BuildDomainApp(persisted.Add);
            await domain.StartAsync(); using var domainClient = domain.GetTestClient();
            var ledger = new InMemoryAdmissionLedger(domainClient);
            var proof = Substitute.For<IAgentsTrustedCommandProofReader>();
            await using WebApplication gateway = BuildGatewayApp(Principal(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId), ledger,
                true, proofReader: proof, allowedDomains: "provider-catalog");
            await gateway.StartAsync(); using var client = GatewayClient(gateway);

            using var response = await client.PostAsJsonAsync("/api/v1/commands", request);

            response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Forbidden);
            persisted.ShouldBeEmpty(); ledger.ExecutionCount.ShouldBe(0); proof.ReceivedCalls().ShouldBeEmpty();
        }
    }

    /// <summary>Malformed governance JSON is a nonretryable client rejection before admission or model replay.</summary>
    [Theory]
    [InlineData("duplicate")]
    [InlineData("missing-enabled")]
    [InlineData("null-enabled")]
    public async Task AmbiguousGovernanceJsonReturnsClientValidationBeforeAdmission(string scenario)
    {
        string json = scenario == "duplicate"
            ? "{\"ProviderId\":\"provider\",\"providerId\":\"other\",\"ModelId\":\"model\"}"
            : "{\"TenantId\":\"tenant-a\",\"ProviderId\":\"provider\",\"ModelId\":\"model\""
                + (scenario == "null-enabled" ? ",\"Enabled\":null}" : "}");
        var request = Request("tenant-provider-enablement", scenario == "duplicate" ? nameof(DecideProviderDataHandling) : nameof(SetTenantProviderModelEnablement)) with
        {
            AggregateId = "tenant-a", Payload = JsonPayload(json), Extensions = null,
        };
        ClaimsPrincipal principal = Principal("Bearer", AgentsAppId);
        ((ClaimsIdentity)principal.Identity!).AddClaim(new Claim("eventstore:tenant", "tenant-a"));
        var modelCommands = new List<SubmitCommand>();
        await using WebApplication domain = BuildDomainApp(modelCommands.Add);
        await domain.StartAsync(); using var domainClient = domain.GetTestClient();
        var ledger = new InMemoryAdmissionLedger(domainClient); var admission = new ProductionAdmissionState();
        await using WebApplication gateway = BuildGatewayApp(principal, ledger, true, admission);
        await gateway.StartAsync(); using var client = gateway.GetTestClient();

        using var response = await client.PostAsJsonAsync("/api/v1/commands", request);

        response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        body.GetProperty("retryable").GetBoolean().ShouldBeFalse();
        body.GetProperty("clientAction").GetString().ShouldBe("correct_request");
        admission.ObservedIntentDigest.ShouldBeNull(); admission.ObservedRetentionTier.ShouldBeNull();
        ledger.ExecutionCount.ShouldBe(0); modelCommands.ShouldBeEmpty();
    }

    /// <summary>Raw governance clocks confer no authority through real gateway reserved-extension admission.</summary>
    [Theory]
    [InlineData("missing")]
    [InlineData("forged")]
    [InlineData("wrong-app")]
    [InlineData("false-flag")]
    [InlineData("wrong-key")]
    [InlineData("proof-time")]
    [InlineData("proof-payload")]
    [InlineData("proof-subject")]
    [InlineData("valid")]
    public async Task RawGovernanceTimestampRequiresExactPriorAuthorityAdmission(string mutation)
    {
        foreach (int offsetDays in new[] { -365, 365 })
        {
            DateTimeOffset serverTime = DateTimeOffset.UtcNow;
            var terms = new ProviderDataHandlingRecord(30, false, ["EU"], "terms-v1", 1, serverTime);
            var admittedPayload = new DecideProviderDataHandling("provider", "model", 1, true, "approved", 1, terms, serverTime);
            var request = Request("tenant-provider-enablement", nameof(DecideProviderDataHandling)) with
            {
                AggregateId = "tenant-a",
                Payload = JsonSerializer.SerializeToElement(admittedPayload),
                Extensions = new Dictionary<string, string> { [TenantProviderEnablementAggregate.TenantAdministratorExtensionKey] = "true" },
            };
            string admittedRequest = JsonSerializer.Serialize(request with { Extensions = null });
            var proof = Substitute.For<IAgentsTrustedCommandProofReader>();
            proof.HasAdmittedProof(Arg.Any<ClaimsPrincipal>(), Arg.Any<SubmitCommandRequest>(), Arg.Any<string>(), Arg.Any<string>())
                .Returns(call => mutation != "forged"
                    && call.ArgAt<ClaimsPrincipal>(0).FindFirst("sub")?.Value == "system:agents"
                    && JsonSerializer.Serialize(call.ArgAt<SubmitCommandRequest>(1)) == admittedRequest
                    && call.ArgAt<string>(2) == TenantProviderEnablementAggregate.TenantAdministratorExtensionKey
                    && call.ArgAt<string>(3) == "true");
            if (mutation != "valid")
            {
                request = request with { Payload = JsonSerializer.SerializeToElement(admittedPayload with
                {
                    DecidedAt = serverTime.AddDays(offsetDays),
                    ConfirmedTerms = mutation == "proof-time" ? terms : terms with { EffectiveAt = serverTime.AddDays(offsetDays) },
                }) };
                if (mutation == "missing") { request = request with { Extensions = null }; }
                if (mutation == "false-flag") { request = request with { Extensions = new Dictionary<string, string> { [TenantProviderEnablementAggregate.TenantAdministratorExtensionKey] = "false" } }; }
                if (mutation == "wrong-key") { request = request with { Extensions = new Dictionary<string, string> { [TenantProviderEnablementAggregate.PlatformOperatorExtensionKey] = "true" } }; }
            }
            // Wrong-app and wrong-subject controls have an exact prior request admission, but the wrong authenticated transport.
            if (mutation is "wrong-app" or "proof-subject") { admittedRequest = JsonSerializer.Serialize(request with { Extensions = null }); }
            ClaimsPrincipal principal = Principal(DaprInternalAuthenticationOptions.SchemeName, mutation == "wrong-app" ? "foreign-app" : AgentsAppId);
            if (mutation == "proof-subject")
            {
                var identity = (ClaimsIdentity)principal.Identity!;
                identity.RemoveClaim(identity.FindFirst("sub")!); identity.AddClaim(new Claim("sub", "other-subject"));
            }
            var enabled = new TenantProviderModelEnablementSet("tenant-a", "provider", "model", true, 1, "operator", null);
            var state = new TenantProviderEnablementState(); state.Apply(enabled);
            var persisted = new List<Hexalith.EventStore.Contracts.Events.IEventPayload> { enabled };
            await using WebApplication domain = BuildDomainApp(command =>
            {
                var payload = JsonSerializer.Deserialize<DecideProviderDataHandling>(command.Payload).ShouldNotBeNull();
                var result = TenantProviderEnablementAggregate.Handle(payload, state, command.ToCommandEnvelope());
                foreach (var item in result.Events)
                {
                    persisted.Add(item);
                    if (item is ProviderDataHandlingDecided accepted) { state.Apply(accepted); }
                    else { state.Apply(item.ShouldBeOfType<TenantProviderGovernanceRejected>()); }
                }
            });
            await domain.StartAsync();
            using var domainClient = domain.GetTestClient(); var ledger = new InMemoryAdmissionLedger(domainClient);
            await using WebApplication gateway = BuildGatewayApp(principal, ledger, true, proofReader: proof,
                allowedDomains: "tenant-provider-enablement");
            await gateway.StartAsync(); using var client = GatewayClient(gateway);

            using var response = await client.PostAsJsonAsync("/api/v1/commands", request);

            var replay = new TenantProviderEnablementState();
            foreach (var item in persisted)
            {
                if (item is TenantProviderModelEnablementSet seed) { replay.Apply(seed); }
                else if (item is ProviderDataHandlingDecided accepted) { replay.Apply(accepted); }
                else { replay.Apply(item.ShouldBeOfType<TenantProviderGovernanceRejected>()); }
            }
            JsonSerializer.Serialize(replay).ShouldBe(JsonSerializer.Serialize(state));
            if (mutation == "valid")
            {
                response.StatusCode.ShouldBe(System.Net.HttpStatusCode.Accepted);
                replay.Revision.ShouldBe(2);
                replay.Entries.Single().Value.LastDecision!.DecidedAt.ShouldBe(serverTime);
                persisted.OfType<ProviderDataHandlingDecided>().ShouldHaveSingleItem().ConfirmedTerms.EffectiveAt.ShouldBe(serverTime);
                ledger.ExecutionCount.ShouldBe(1);
            }
            else
            {
                replay.Revision.ShouldBe(1); replay.Entries.Single().Value.LastDecision.ShouldBeNull();
                persisted.OfType<ProviderDataHandlingDecided>().ShouldBeEmpty();
                if (mutation == "missing")
                {
                    persisted.OfType<TenantProviderGovernanceRejected>().ShouldHaveSingleItem().Reason.ShouldBe("NotAuthorized");
                }
                else
                {
                    response.StatusCode.ShouldBe(System.Net.HttpStatusCode.BadRequest);
                    ledger.ExecutionCount.ShouldBe(0); persisted.Count.ShouldBe(1);
                }
            }
        }
    }

    private static WebApplication BuildGatewayApp(
        ClaimsPrincipal principal,
        InMemoryAdmissionLedger ledger,
        bool registerAgentsIntegration,
        ProductionAdmissionState? admissionState = null,
        IAgentsTrustedCommandProofReader? proofReader = null,
        string allowedDomains = "agent,other-domain",
        bool proofAvailable = true)
    {
        ProductionAdmissionState state = admissionState ?? new ProductionAdmissionState();
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
#if HEXALITH_EVENTSTORE_SOURCE
        principal = principal.Clone();
        ClaimsIdentity identity = principal.Identities.Single();
        if (identity.AuthenticationType == DaprInternalAuthenticationOptions.SchemeName)
        {
            identity.AddClaim(new Claim(ClaimTypes.NameIdentifier, "workload:agents"));
            identity.AddClaim(new Claim(EventStoreWorkloadAuthenticationDefaults.WorkloadClaimType, "agents"));
            identity.AddClaim(new Claim(EventStoreWorkloadAuthenticationDefaults.OperationClaimType, EventStoreOperationIdentifiers.GatewayCommandSubmit));
        }
        builder.Services.AddSingleton(principal);
        builder.Services.AddAuthentication(DaprInternalAuthenticationOptions.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, FixtureGatewayPrincipalAuthenticationHandler>(DaprInternalAuthenticationOptions.SchemeName, _ => { })
            .AddScheme<AuthenticationSchemeOptions, FixtureGatewayPrincipalAuthenticationHandler>("Bearer", _ => { });
        builder.Services.AddEventStoreWorkloadPolicies(DaprInternalAuthenticationOptions.SchemeName,
            [EventStoreOperationIdentifiers.GatewayCommandSubmit]);
        builder.Services.AddAuthorization(options => options.AddPolicy(EventStoreHttpHostSecurityExtensions.HumanPolicy,
            EventStoreHttpHostSecurityExtensions.CreateHumanPolicy()));
        builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Authentication:BoundCommand:AllowedDomains:agents"] = allowedDomains,
            ["Authentication:BoundStreamRead:PublicKeys:agents"] = FixtureBoundAgentCommandHandler.PublicKey,
        });
#else
        _ = builder.Services.AddAuthorization();
#endif
        _ = builder.Services.AddControllers().AddApplicationPart(typeof(CommandsController).Assembly);
        builder.Services.AddSingleton<IOptions<ExtensionMetadataOptions>>(
            Options.Create(new ExtensionMetadataOptions()));
        builder.Services.AddSingleton<ExtensionMetadataSanitizer>();
        if (registerAgentsIntegration)
        {
            _ = builder.Services.AddAgentsEventStore(AgentsAppId,
                proofAvailable ? proofReader ?? new FixtureTrustedCommandProofReader() : null);
        }

        builder.Services.AddSingleton<IIdempotencyDigestKeyProvider>(_ => CreateDigestKeyProvider());
        builder.Services.AddSingleton(CreateProductionAdmissionActorFactory(state));
        builder.Services.AddSingleton<IMediator>(services =>
        {
            var mediator = Substitute.For<IMediator>();
#if HEXALITH_EVENTSTORE_SOURCE
            var catalog = new FixtureAgentRouteCatalog();
            var registry = new IdempotencyIntentAdapterRegistry(catalog,
                services.GetServices<IIdempotencyIntentAdapter>().Select(adapter => new FixtureDeclaredAgentIntentAdapter(adapter)),
                new CanonicalIdempotencyIntentEncoder());
#else
            var registry = new IdempotencyIntentAdapterRegistry(services.GetServices<IIdempotencyIntentAdapter>(),
                new CanonicalIdempotencyIntentEncoder());
#endif
            IActorProxyFactory actorProxyFactory = services.GetRequiredService<IActorProxyFactory>();
            IIdempotencyDigestKeyProvider keyProvider = services
                .GetRequiredService<IIdempotencyDigestKeyProvider>();
            var coordinator = new IdempotencyAdmissionCoordinator(
#if HEXALITH_EVENTSTORE_SOURCE
                catalog,
#endif
                actorProxyFactory,
                new IdempotencyKeyProtector(keyProvider),
                registry,
                new IdempotencyExecutionContextProtector(keyProvider, actorProxyFactory)
#if HEXALITH_EVENTSTORE_SOURCE
                ,
                Options.Create(new IdempotencyAdmissionOptions { Enabled = true, ActiveDigestKeyVersion = "v1" }),
                Options.Create(new RouteCatalogHostOptions
                {
                    Identity = new("eventstore", "eventstore", "fixture-inventory", "fixture-topology", "fixture-authentication"),
                    Profile = "Development", DevelopmentFixture = true,
                })
#endif
                );

            ICommandRouter router = Substitute.For<ICommandRouter>();
            router.RouteFencedCommandAsync(
                    Arg.Any<SubmitCommand>(),
                    Arg.Any<IdempotencyExecutionContext>(),
                    Arg.Any<CancellationToken>())
                .Returns(call => ledger.RouteAsync(
                    call.ArgAt<SubmitCommand>(0),
                    call.ArgAt<CancellationToken>(2)));
            ICommandStatusStore statusStore = Substitute.For<ICommandStatusStore>();
            statusStore.ReadStatusAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(call => Task.FromResult<CommandStatusRecord?>(new CommandStatusRecord(
                    CommandStatus.Completed,
                    DateTimeOffset.UtcNow,
                    AggregateId: "agent-a",
                    EventCount: 1,
                    RejectionEventType: null,
                    FailureReason: null,
                    TimeoutDuration: null,
                    MessageId: call.ArgAt<string>(1),
                    CorrelationId: "01ARZ3NDEKTSV4RRFFQ69G5FAW")));
            ICommandArchiveStore archiveStore = Substitute.For<ICommandArchiveStore>();
            archiveStore.ReadCommandAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(Task.FromResult<ArchivedCommand?>(null));
            var handler = new SubmitCommandHandler(
#if HEXALITH_EVENTSTORE_SOURCE
                catalog,
#endif
                statusStore,
                archiveStore,
                router,
#if HEXALITH_EVENTSTORE_SOURCE
                null,
                null,
                new NoOpProjectionUpdateOrchestrator(),
#endif
                NullLogger<SubmitCommandHandler>.Instance,
                idempotencyAdmissionCoordinator: coordinator);
            mediator.Send(Arg.Any<SubmitCommand>(), Arg.Any<CancellationToken>())
                .Returns(call => handler.Handle(
                    call.ArgAt<SubmitCommand>(0),
                    call.ArgAt<CancellationToken>(1)));
            return mediator;
        });

        WebApplication app = builder.Build();
        app.Use(async (context, next) =>
        {
            try
            {
                context.User = principal;
                await next(context).ConfigureAwait(false);
            }
            catch (IdempotencyConflictException)
            {
                context.Response.StatusCode = StatusCodes.Status409Conflict;
            }
            catch (IdempotencyAdmissionFailureException exception)
            {
                context.Response.StatusCode = exception.StatusCode;
                await context.Response.WriteAsJsonAsync(new
                {
                    status = exception.StatusCode,
                    retryable = exception.Retryable,
                    clientAction = exception.ClientAction,
                }).ConfigureAwait(false);
            }
#if HEXALITH_EVENTSTORE_SOURCE
            catch (RouteCatalogUnavailableException)
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                await context.Response.WriteAsJsonAsync(new
                {
                    status = StatusCodes.Status503ServiceUnavailable,
                    retryable = true,
                    clientAction = "retry_later",
                }).ConfigureAwait(false);
            }
#endif
        });
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }

    private static IActorProxyFactory CreateProductionAdmissionActorFactory(ProductionAdmissionState state)
    {
        IIdempotencyAdmissionActor admission = Substitute.For<IIdempotencyAdmissionActor>();
        IIdempotencyAdmissionDirectoryActor directory = Substitute.For<IIdempotencyAdmissionDirectoryActor>();
        IIdempotencyTenantLifecycleActor lifecycle = Substitute.For<IIdempotencyTenantLifecycleActor>();
        IIdempotencyLegacyInventoryActor legacyInventory = Substitute.For<IIdempotencyLegacyInventoryActor>();
        IActorProxyFactory factory = Substitute.For<IActorProxyFactory>();

        _ = factory.CreateActorProxy<IIdempotencyAdmissionActor>(
                Arg.Any<ActorId>(),
                IdempotencyAdmissionActor.ActorTypeName)
            .Returns(admission);
        _ = factory.CreateActorProxy<IIdempotencyAdmissionDirectoryActor>(
                Arg.Any<ActorId>(),
                IdempotencyAdmissionDirectoryActor.ActorTypeName)
            .Returns(directory);
        _ = factory.CreateActorProxy<IIdempotencyTenantLifecycleActor>(
                Arg.Any<ActorId>(),
                IdempotencyTenantLifecycleActor.ActorTypeName)
            .Returns(lifecycle);
        _ = factory.CreateActorProxy<IIdempotencyLegacyInventoryActor>(
                Arg.Any<ActorId>(),
                IdempotencyLegacyInventoryActor.ActorTypeName)
            .Returns(legacyInventory);

        _ = legacyInventory.InspectAsync(Arg.Any<IdempotencyAdmissionDirectoryAlias[]>())
            .Returns(new IdempotencyLegacyInventoryInspection(IdempotencyLegacyInventoryDecision.NoLegacy));
        _ = admission.InspectAsync().Returns(_ => state.Inspect());
        _ = directory.ResolveAsync(Arg.Any<IdempotencyAdmissionDirectoryRequest>())
            .Returns(call =>
            {
                IdempotencyAdmissionDirectoryRequest request = call
                    .ArgAt<IdempotencyAdmissionDirectoryRequest>(0);
                return new IdempotencyAdmissionDirectoryResult(
                    request.ExistingActorId ?? request.ActiveActorId,
                    IdempotencyAdmissionPromotionPhase.Stable);
            });
        _ = lifecycle.RegisterAsync(Arg.Any<IdempotencyTenantLifecycleReference[]>())
            .Returns(Task.CompletedTask);
        _ = lifecycle.AdmitAsync(Arg.Any<IdempotencyTenantLifecycleAdmissionRequest>())
            .Returns(call => state.Admit(call.ArgAt<IdempotencyTenantLifecycleAdmissionRequest>(0).Admission));
        _ = admission.BeginAsync(Arg.Any<IdempotencyAdmissionTransitionRequest>())
            .Returns(call =>
            {
                state.ValidateFence(call.ArgAt<IdempotencyAdmissionTransitionRequest>(0).FencingToken);
                return Task.CompletedTask;
            });
        _ = admission.ValidateAuthorityAsync(Arg.Any<IdempotencyAdmissionAuthorityRequest>())
            .Returns(call =>
            {
                state.ValidateAuthority(call.ArgAt<IdempotencyAdmissionAuthorityRequest>(0));
                return Task.CompletedTask;
            });
        _ = admission.CompleteAsync(Arg.Any<IdempotencyAdmissionCompletionRequest>())
            .Returns(call =>
            {
                state.Complete(call.ArgAt<IdempotencyAdmissionCompletionRequest>(0));
                return Task.CompletedTask;
            });

        return factory;
    }

    private sealed class ProductionAdmissionState
    {
        private const long FencingToken = 1;

        private string? _intentDigest;
        private string? _executionMessageId;
        private string? _executionCorrelationId;
        private CommandProcessingResult? _result;

        public IdempotencyReplayRetentionTier? ObservedRetentionTier { get; private set; }

        public string? ObservedIntentDigest { get; private set; }

        public IdempotencyAdmissionInspection Inspect()
            => new(_intentDigest is not null);

        public void SeedInFlight(string intentDigest)
        {
            _intentDigest = intentDigest;
            _executionMessageId = "01ARZ3NDEKTSV4RRFFQ69G5FAV";
            _executionCorrelationId = "01ARZ3NDEKTSV4RRFFQ69G5FAW";
        }

        public IdempotencyAdmissionResult Admit(IdempotencyAdmissionRequest request)
        {
            ObservedRetentionTier = request.RetentionTier;
            ObservedIntentDigest = request.IntentDigest;
            if (_intentDigest is null)
            {
                _intentDigest = request.IntentDigest;
                _executionMessageId = request.ExecutionMessageId;
                _executionCorrelationId = request.ExecutionCorrelationId;
                return Result(IdempotencyAdmissionDecision.Execute);
            }

            if (!string.Equals(_intentDigest, request.IntentDigest, StringComparison.Ordinal))
            {
                return Result(IdempotencyAdmissionDecision.Conflict);
            }

            return _result is null
                ? Result(IdempotencyAdmissionDecision.Pending)
                : Result(IdempotencyAdmissionDecision.Replay, _result);
        }

        public void ValidateFence(long fencingToken)
        {
            if (fencingToken != FencingToken || _intentDigest is null)
            {
                throw new InvalidOperationException("The production-coordinator test fence is invalid.");
            }
        }

        public void ValidateAuthority(IdempotencyAdmissionAuthorityRequest request)
        {
            ValidateFence(request.FencingToken);
            if (!string.Equals(request.DigestKeyVersion, "v1", StringComparison.Ordinal)
                || !string.Equals(request.ExecutionMessageId, _executionMessageId, StringComparison.Ordinal)
                || !string.Equals(request.ExecutionCorrelationId, _executionCorrelationId, StringComparison.Ordinal)
                || request.Purpose != IdempotencyExecutionPurpose.Execute)
            {
                throw new InvalidOperationException("The production-coordinator test authority is invalid.");
            }
        }

        public void Complete(IdempotencyAdmissionCompletionRequest request)
        {
            ValidateFence(request.FencingToken);
            _result = request.Result;
        }

        private IdempotencyAdmissionResult Result(
            IdempotencyAdmissionDecision decision,
            CommandProcessingResult? replay = null)
            => new(
                decision,
                FencingToken,
                replay,
                ExecutionMessageId: _executionMessageId,
                ExecutionCorrelationId: _executionCorrelationId);
    }

    private sealed class InMemoryAdmissionLedger(HttpClient domainClient)
    {
        public int ExecutionCount { get; private set; }

        public async Task<CommandProcessingResult> RouteAsync(
            SubmitCommand command,
            CancellationToken cancellationToken)
        {
            ExecutionCount++;
            using HttpResponseMessage response = await domainClient
                .PostAsJsonAsync("/process", command, cancellationToken)
                .ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            SubmitCommandResult receipt = await response.Content
                .ReadFromJsonAsync<SubmitCommandResult>(cancellationToken: cancellationToken)
                .ConfigureAwait(false)
                ?? throw new InvalidOperationException("The independent domain endpoint returned no receipt.");
            return new CommandProcessingResult(
                Accepted: true,
                CorrelationId: receipt.CorrelationId,
                EventCount: 1,
                ResultPayload: receipt.ResultPayload);
        }
    }
}
