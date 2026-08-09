namespace Hexalith.Agents.Server.Tests;

using System.Collections.Generic;
using System.Linq;

using Hexalith.Agents.Client;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Composition;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Client.Projections;

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
        using ServiceProvider provider = Build(("Agents:EventStore:BaseUrl", "https://eventstore.example"));
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
    public void The_configured_read_model_store_name_binds_from_configuration()
    {
        using ServiceProvider provider = Build(
            ("Agents:EventStore:BaseUrl", "https://eventstore.example"),
            ($"{AgentSetupReadModelOptions.SectionName}:StateStoreName", "agents-statestore"));

        provider.GetRequiredService<IOptions<AgentSetupReadModelOptions>>().Value.StateStoreName
            .ShouldBe("agents-statestore");
    }

    private static ServiceProvider Build(params (string Key, string Value)[] settings)
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

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateScopes = true,
            ValidateOnBuild = false,
        });
    }
}
