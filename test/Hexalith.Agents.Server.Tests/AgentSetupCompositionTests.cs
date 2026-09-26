namespace Hexalith.Agents.Server.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text;

using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Application.Queries;
using Hexalith.Agents.Server.Composition;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;
using Hexalith.Agents.TenantProviderEnablement;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using NSubstitute;

using Shouldly;

/// <summary>
/// Composition tests for the Story 5.2 live setup path. They resolve the graph rather than inspecting descriptors,
/// because the story's risk is a host that <i>looks</i> wired and then answers with a deferred seam: with a gateway
/// configured the live dispatcher and administration operations must actually come out of the container, and without
/// one the fail-closed placeholders must stay in place.
/// </summary>
public sealed class AgentSetupCompositionTests
{
    [Fact]
    public void A_configured_gateway_resolves_the_live_dispatcher_and_administration_operations()
    {
        using ServiceProvider provider = Build(
            ("Agents:EventStore:BaseUrl", "https://eventstore.example"),
            ("Agents:EventStore:AppId", "eventstore"));
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IAgentCommandDispatcher>()
            .ShouldBeOfType<EventStoreAgentCommandDispatcher>();
        scope.ServiceProvider.GetRequiredService<IAgentAdministrationOperations>()
            .ShouldBeOfType<EventStoreAgentAdministrationOperations>();
        scope.ServiceProvider.GetRequiredService<IAgentAdministrationContextProvider>()
            .ShouldBeOfType<HttpAgentAdministrationContextProvider>();

        // The public client the API surface consumes must be the one backed by the live administration operations.
        scope.ServiceProvider.GetRequiredService<IAgentsClient>().AgentAdministration
            .ShouldBeOfType<EventStoreAgentAdministrationOperations>();
        scope.ServiceProvider.GetRequiredService<IAgentsClient>().ProviderCatalog
            .ShouldBeOfType<EventStoreProviderCatalogOperations>();
        scope.ServiceProvider.GetRequiredService<IProviderCatalogOperations>()
            .ShouldBeOfType<EventStoreProviderCatalogOperations>();
        scope.ServiceProvider.GetRequiredService<IProviderCatalogReader>()
            .ShouldBeOfType<ProjectedProviderCatalogReader>();
    }

    [Fact]
    public async Task Configured_Dapr_identity_reaches_the_outbound_gateway_request()
    {
        var handler = new CapturingHandler();
        using ServiceProvider provider = BuildWithHandler(
            handler,
            ("Agents:EventStore:BaseUrl", "https://eventstore.example"),
            ("Agents:EventStore:AppId", "eventstore-runtime"),
            ("Agents:EventStore:DaprApiToken", "secret-token"));

        IAgentCommandDispatcher dispatcher = provider.GetRequiredService<IAgentCommandDispatcher>();
        _ = await dispatcher.DispatchAsync(
            new CommandEnvelope(
                "message-1",
                "tenant-a",
                "agent",
                "agent-a",
                "DisableAgent",
                [],
                "correlation-1",
                null,
                "system:agents",
                null),
            CancellationToken.None);

        handler.DaprAppId.ShouldBe("eventstore-runtime");
        handler.DaprAppIdHeaderValueCount.ShouldBe(1);
        handler.DaprApiToken.ShouldBe("secret-token");
        handler.DaprApiTokenHeaderValueCount.ShouldBe(1);
    }

    [Fact]
    public async Task Configured_Dapr_identity_without_an_optional_token_sends_only_the_app_id_header()
    {
        var handler = new CapturingHandler();
        using ServiceProvider provider = BuildWithHandler(
            handler,
            ("Agents:EventStore:BaseUrl", "https://eventstore.example"),
            ("Agents:EventStore:AppId", "eventstore-runtime"));

        IAgentCommandDispatcher dispatcher = provider.GetRequiredService<IAgentCommandDispatcher>();
        _ = await dispatcher.DispatchAsync(
            new CommandEnvelope(
                "message-1",
                "tenant-a",
                "agent",
                "agent-a",
                "DisableAgent",
                [],
                "correlation-1",
                null,
                "system:agents",
                null),
            CancellationToken.None);

        handler.DaprAppId.ShouldBe("eventstore-runtime");
        handler.DaprAppIdHeaderValueCount.ShouldBe(1);
        handler.DaprApiToken.ShouldBeNull();
        handler.DaprApiTokenHeaderValueCount.ShouldBe(0);
    }

    [Fact]
    public void An_unconfigured_gateway_leaves_the_administration_path_fail_closed()
    {
        using ServiceProvider provider = Build();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IAgentCommandDispatcher>()
            .ShouldBeOfType<DeferredAgentCommandDispatcher>();
        scope.ServiceProvider.GetService<IAgentAdministrationOperations>().ShouldBeNull();

        // The seeded Unavailable client stands, so every administration operation answers Unavailable rather than
        // silently accepting a command this host cannot persist (AD-12).
        scope.ServiceProvider.GetRequiredService<IAgentsClient>().AgentAdministration
            .ShouldNotBeOfType<EventStoreAgentAdministrationOperations>();
    }

    [Fact]
    public void A_malformed_gateway_url_is_treated_as_unconfigured_rather_than_partially_live()
    {
        using ServiceProvider provider = Build(("Agents:EventStore:BaseUrl", "not-a-url"));
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IAgentCommandDispatcher>()
            .ShouldBeOfType<DeferredAgentCommandDispatcher>();
    }

    [Fact]
    public void A_gateway_without_a_Dapr_app_id_leaves_the_administration_path_fail_closed()
    {
        using ServiceProvider provider = Build(("Agents:EventStore:BaseUrl", "https://eventstore.example"));
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IAgentCommandDispatcher>()
            .ShouldBeOfType<DeferredAgentCommandDispatcher>();
        scope.ServiceProvider.GetService<IAgentAdministrationOperations>().ShouldBeNull();
    }

    [Fact]
    public void The_configured_read_model_store_name_binds_from_configuration()
    {
        using ServiceProvider provider = Build(
            ("Agents:EventStore:BaseUrl", "https://eventstore.example"),
            ("Agents:EventStore:AppId", "eventstore"),
            ($"{AgentSetupReadModelOptions.SectionName}:StateStoreName", "agents-statestore"));

        provider.GetRequiredService<IOptions<AgentSetupReadModelOptions>>().Value.StateStoreName
            .ShouldBe("agents-statestore");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Provider_catalog_query_and_migration_services_resolve_with_and_without_a_gateway(bool gateway)
    {
        using ServiceProvider provider = gateway
            ? Build(("Agents:EventStore:BaseUrl", "https://eventstore.example"), ("Agents:EventStore:AppId", "eventstore"))
            : Build();
        using IServiceScope scope = provider.CreateScope();

        // Query handlers are materialized by the EventStore domain-service scan; they need the context provider
        // whether or not the gateway is bound.
        scope.ServiceProvider.GetRequiredService<IAgentAdministrationContextProvider>()
            .ShouldBeOfType<HttpAgentAdministrationContextProvider>();
        scope.ServiceProvider.GetRequiredService<ProviderCatalogMigrationService>().ShouldNotBeNull();
        ActivatorUtilities.CreateInstance<ListProviderCatalogEntriesQueryHandler>(scope.ServiceProvider).ShouldNotBeNull();
        ActivatorUtilities.CreateInstance<GetProviderCatalogEntryQueryHandler>(scope.ServiceProvider).ShouldNotBeNull();
    }

    [Fact]
    public async Task Configured_gateway_reaches_the_projected_reader_authority_check()
    {
        const string storeName = "statestore";
        var handler = new CapturingHandler();
        var store = new FakeReadModelStore();
        var tenant = new TenantProviderEnablementReadModel { LastSequenceNumber = 1 };
        tenant.State.Apply(new Hexalith.Agents.Contracts.ProviderCatalog.Events.TenantProviderModelEnablementSet(
            "tenant-a", "openai", "gpt-4o", true, 1, "operator", null));
        store.Seed(storeName, TenantProviderEnablementReadModelAddresses.Detail("tenant-a"), tenant);
        using ServiceProvider provider = BuildCore(
            handler,
            store,
            ("Agents:EventStore:BaseUrl", "https://eventstore.example"),
            ("Agents:EventStore:AppId", "eventstore"),
            ($"{ProviderCatalogReadModelOptions.SectionName}:StateStoreName", storeName));
        using IServiceScope scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim("tenantId", "tenant-a"), new Claim("sub", "admin"),
                    new Claim(ClaimTypes.Role, HttpAgentAdministrationContextProvider.AgentsAdministratorRole)],
                "test")),
        };

        ProviderCatalogEntryReadResult result = await scope.ServiceProvider.GetRequiredService<IProviderCatalogReader>()
            .GetEntryAsync("tenant-a", "openai", "gpt-4o", CancellationToken.None);

        // The stub answers with a non-stream payload, so the head check fails closed after reaching the gateway.
        result.Status.ShouldBe(ProviderCatalogInspectionStatus.Unavailable);
        handler.RequestCount.ShouldBeGreaterThan(0);
    }

    private static ServiceProvider Build(params (string Key, string Value)[] settings)
        => BuildCore(null, null, settings);

    private static ServiceProvider BuildWithHandler(
        HttpMessageHandler handler,
        params (string Key, string Value)[] settings)
        => BuildCore(handler, null, settings);

    private static ServiceProvider BuildCore(
        HttpMessageHandler? handler,
        IReadModelStore? store,
        params (string Key, string Value)[] settings)
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)))
            .Build();

        // The pre-existing host registrations the live path either keeps or replaces.
        services.AddSingleton<IAgentCommandDispatcher, DeferredAgentCommandDispatcher>();
        services.AddSingleton(AgentsClient.Unavailable());
        services.AddSingleton(Substitute.For<IProviderCatalogReader>());
        services.AddSingleton(Substitute.For<IApproverPolicyResolver>());
        services.AddSingleton(store ?? Substitute.For<IReadModelStore>());
        services.AddSingleton(Substitute.For<ITenantAccessReader>());
        services.AddScoped<AgentResponseModeOrchestrator>();
        services.AddScoped<AgentActivationProviderRevalidation>();

        _ = services.AddAgentSetupServices(configuration);
        if (handler is not null)
        {
            _ = services
                .AddHttpClient<IEventStoreGatewayClient, EventStoreGatewayClient>()
                .ConfigurePrimaryHttpMessageHandler(() => handler);
        }

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = false,
        });
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        internal string? DaprAppId { get; private set; }

        internal int DaprAppIdHeaderValueCount { get; private set; }

        internal string? DaprApiToken { get; private set; }

        internal int DaprApiTokenHeaderValueCount { get; private set; }

        internal int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            string[] appIds = request.Headers.TryGetValues("dapr-app-id", out IEnumerable<string>? appIdValues)
                ? appIdValues.ToArray()
                : [];
            string[] apiTokens = request.Headers.TryGetValues("dapr-api-token", out IEnumerable<string>? apiTokenValues)
                ? apiTokenValues.ToArray()
                : [];
            DaprAppIdHeaderValueCount = appIds.Length;
            DaprAppId = appIds.SingleOrDefault();
            DaprApiTokenHeaderValueCount = apiTokens.Length;
            DaprApiToken = apiTokens.SingleOrDefault();
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)
            {
                Content = new StringContent(
                    "{\"correlationId\":\"correlation-1\",\"messageId\":\"message-1\"}",
                    Encoding.UTF8,
                    "application/json"),
            });
        }
    }
}
