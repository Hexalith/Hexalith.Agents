namespace Hexalith.Agents.Server.Tests;

using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;

using Hexalith.Agents.Client;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Composition;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;

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
        handler.DaprApiToken.ShouldBe("secret-token");
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

    private static ServiceProvider Build(params (string Key, string Value)[] settings)
        => BuildCore(null, settings);

    private static ServiceProvider BuildWithHandler(
        HttpMessageHandler handler,
        params (string Key, string Value)[] settings)
        => BuildCore(handler, settings);

    private static ServiceProvider BuildCore(
        HttpMessageHandler? handler,
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
        services.AddSingleton(Substitute.For<IReadModelStore>());
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

        internal string? DaprApiToken { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            DaprAppId = request.Headers.GetValues("dapr-app-id").Single();
            DaprApiToken = request.Headers.GetValues("dapr-api-token").Single();
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
