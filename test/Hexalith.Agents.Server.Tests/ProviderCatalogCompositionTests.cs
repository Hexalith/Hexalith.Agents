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

using NSubstitute;

using Shouldly;

/// <summary>
/// Composition tests for the Story 5.3 live catalog path. A configured EventStore gateway must resolve the live
/// catalog operations and projected reader; an unconfigured host must keep the deferred seams.
/// </summary>
public sealed class ProviderCatalogCompositionTests
{
    [Fact]
    public void A_configured_gateway_resolves_the_live_catalog_seams()
    {
        using ServiceProvider provider = Build(("Agents:EventStore:BaseUrl", "https://eventstore.example"));
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IProviderCatalogOperations>()
            .ShouldBeOfType<EventStoreProviderCatalogOperations>();
        scope.ServiceProvider.GetRequiredService<IProviderCatalogReader>()
            .ShouldBeOfType<ProjectedProviderCatalogReader>();
        scope.ServiceProvider.GetRequiredService<IAgentsClient>().ProviderCatalog
            .ShouldBeOfType<EventStoreProviderCatalogOperations>();
        scope.ServiceProvider.GetRequiredService<IAgentsClient>().AgentAdministration
            .ShouldBeOfType<EventStoreAgentAdministrationOperations>();
    }

    [Fact]
    public void An_unconfigured_gateway_leaves_the_catalog_reader_on_the_seeded_placeholder()
    {
        using ServiceProvider provider = Build();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetService<IProviderCatalogOperations>().ShouldBeNull();
        scope.ServiceProvider.GetRequiredService<IAgentsClient>().ProviderCatalog
            .ShouldNotBeOfType<EventStoreProviderCatalogOperations>();
    }

    private static ServiceProvider Build(params (string Key, string Value)[] settings)
    {
        ServiceCollection services = new();
        IConfiguration configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)))
            .Build();

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
