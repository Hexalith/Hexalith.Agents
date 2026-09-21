namespace Hexalith.Agents.Server.Tests;

using System.Net.Http.Json;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using System.Text.Json;

using Dapr.Actors;
using Dapr.Actors.Client;

using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.EventStore;
using Hexalith.Agents.Server.Composition;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Authentication;
using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.Configuration;
using Hexalith.EventStore.Controllers;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.Server.Actors;
using Hexalith.EventStore.Server.Commands;
using Hexalith.EventStore.Server.Pipeline;
using Hexalith.EventStore.Server.Pipeline.Commands;
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
    public void Gateway_registration_is_explicit_idempotent_and_owns_all_five_setup_adapters()
    {
        ServiceCollection gatewayServices = new();
        _ = gatewayServices.AddAgentsEventStore(AgentsAppId);
        _ = gatewayServices.AddAgentsEventStore(AgentsAppId);
        using ServiceProvider gateway = gatewayServices.BuildServiceProvider();

        string[] commandTypes = gateway.GetServices<IIdempotencyIntentAdapter>()
            .Select(adapter => adapter.CommandType)
            .Order(StringComparer.Ordinal)
            .ToArray();
        commandTypes.ShouldBe(
        [
            nameof(ActivateAgent),
            nameof(ConfigureAgentResponseMode),
            nameof(CreateAgent),
            nameof(DisableAgent),
            nameof(UpdateAgentConfiguration),
        ]);
        gateway.GetServices<ITrustedCommandExtensionPolicy>().Count().ShouldBe(1);

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

        SubmitCommand[] conflicts =
        [
            ActivationCommand("{\"value\":2}", "7", nameof(ProviderSelectionValidationStatus.Valid), nameof(ApproverPolicyValidationStatus.Valid)),
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

        registry.Resolve(original with { Tenant = "tenant-b" }).CanonicalIntent.ShouldNotBe(originalIntent);
        registry.Resolve(original with { AggregateId = "agent-b" }).CanonicalIntent.ShouldNotBe(originalIntent);
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
        using HttpClient client = gateway.GetTestClient();

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
        executedCommands[0].Extensions?[AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion]
            .ShouldBe("7");

        SubmitCommandRequest[] conflicts =
        [
            ActivationRequest(JsonSerializer.SerializeToElement(new { value = 2 }), "7", nameof(ProviderSelectionValidationStatus.Valid), nameof(ApproverPolicyValidationStatus.Valid)),
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
        using HttpClient client = gateway.GetTestClient();

        using HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/v1/commands",
            ActivationRequest(
                JsonSerializer.SerializeToElement(new ActivateAgent()),
                "7",
                nameof(ProviderSelectionValidationStatus.Valid),
                nameof(ApproverPolicyValidationStatus.Valid))).ConfigureAwait(true);

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
    [InlineData("Bearer", AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.AgentAdministrator, "true", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, "other-app", "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.AgentAdministrator, "true", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "other-domain", nameof(ActivateAgent), AgentSetupTrustedExtensions.AgentAdministrator, "true", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", "OtherCommand", AgentSetupTrustedExtensions.AgentAdministrator, "true", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.AgentAdministrator, "True", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(UpdateAgentConfiguration), AgentSetupTrustedExtensions.ProviderSelectionValidation, "Valid", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.ProviderSelectionValidation, "Valid", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.ApproverPolicyValidation, "Valid", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion, "7", true)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.ProviderSelectionValidation, "valid", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion, "07", false)]
    [InlineData(DaprInternalAuthenticationOptions.SchemeName, AgentsAppId, "agent", nameof(ActivateAgent), "agent:unknown", "true", false)]
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
    [InlineData("agent", nameof(UpdateAgentConfiguration), AgentSetupTrustedExtensions.ProviderSelectionValidation, false)]
    [InlineData("other-domain", nameof(ActivateAgent), AgentSetupTrustedExtensions.AgentAdministrator, false)]
    [InlineData("agent", "OtherCommand", AgentSetupTrustedExtensions.AgentAdministrator, false)]
    [InlineData("agent", nameof(ActivateAgent), "agent:unknown", false)]
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

    [Fact]
    public void Reserved_extension_policy_rejects_missing_duplicate_or_foreign_identity_claims()
    {
        ITrustedCommandExtensionPolicy policy = Policy();
        SubmitCommandRequest request = Request("agent", nameof(ActivateAgent));
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

        foreach (ClaimsPrincipal principal in new[]
        {
            missingClaim,
            duplicateClaim,
            claimOnAnotherIdentity,
            additionalAuthenticatedIdentity,
        })
        {
            policy.Accepts(
                principal,
                request,
                AgentSetupTrustedExtensions.AgentAdministrator,
                "true").ShouldBeFalse();
        }
    }

    private static IdempotencyIntentAdapterRegistry Registry()
    {
        ServiceCollection services = new();
        _ = services.AddAgentsEventStore(AgentsAppId);
        using ServiceProvider provider = services.BuildServiceProvider();
        return new IdempotencyIntentAdapterRegistry(
            provider.GetServices<IIdempotencyIntentAdapter>().ToArray(),
            new CanonicalIdempotencyIntentEncoder());
    }

    private static ITrustedCommandExtensionPolicy Policy()
    {
        ServiceCollection services = new();
        _ = services.AddAgentsEventStore(AgentsAppId);
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

    private static WebApplication BuildDomainApp(Action<SubmitCommand> onExecute)
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
                "{\"effect\":\"Applied\",\"configurationVersion\":8}",
                command.MessageId));
        });
        return app;
    }

    private static WebApplication BuildGatewayApp(
        ClaimsPrincipal principal,
        InMemoryAdmissionLedger ledger,
        bool registerAgentsIntegration)
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        _ = builder.Services.AddAuthorization();
        _ = builder.Services.AddControllers().AddApplicationPart(typeof(CommandsController).Assembly);
        builder.Services.AddSingleton<IOptions<ExtensionMetadataOptions>>(
            Options.Create(new ExtensionMetadataOptions()));
        builder.Services.AddSingleton<ExtensionMetadataSanitizer>();
        if (registerAgentsIntegration)
        {
            _ = builder.Services.AddAgentsEventStore(AgentsAppId);
        }

        builder.Services.AddSingleton<IIdempotencyDigestKeyProvider>(_ =>
            new StaticIdempotencyDigestKeyProvider(
                "v1",
                new Dictionary<string, byte[]>(StringComparer.Ordinal)
                {
                    ["v1"] = Encoding.UTF8.GetBytes("0123456789abcdef0123456789abcdef"),
                },
                []));
        builder.Services.AddSingleton(CreateProductionAdmissionActorFactory());
        builder.Services.AddSingleton<IMediator>(services =>
        {
            var mediator = Substitute.For<IMediator>();
            var registry = new IdempotencyIntentAdapterRegistry(
                services.GetServices<IIdempotencyIntentAdapter>(),
                new CanonicalIdempotencyIntentEncoder());
            IActorProxyFactory actorProxyFactory = services.GetRequiredService<IActorProxyFactory>();
            IIdempotencyDigestKeyProvider keyProvider = services
                .GetRequiredService<IIdempotencyDigestKeyProvider>();
            var coordinator = new IdempotencyAdmissionCoordinator(
                actorProxyFactory,
                new IdempotencyKeyProtector(keyProvider),
                registry,
                new IdempotencyExecutionContextProtector(keyProvider, actorProxyFactory));

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
                statusStore,
                archiveStore,
                router,
                NullLogger<SubmitCommandHandler>.Instance,
                coordinator);
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
        });
        app.UseAuthorization();
        app.MapControllers();
        return app;
    }

    private static IActorProxyFactory CreateProductionAdmissionActorFactory()
    {
        var state = new ProductionAdmissionState();
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

        public IdempotencyAdmissionInspection Inspect()
            => new(_intentDigest is not null);

        public IdempotencyAdmissionResult Admit(IdempotencyAdmissionRequest request)
        {
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
