namespace Hexalith.Agents.Server.Tests;

using System.Collections.Generic;
using System.Linq;

using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.ProviderCatalog;
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
/// Composition tests for the deferred Story 5.3 catalog write path. A configured EventStore gateway resolves the
/// projected reader but keeps catalog mutations on the unavailable default until Story 5.3 supplies its own trusted
/// gateway policy and idempotency adapters.
/// </summary>
public sealed class ProviderCatalogCompositionTests
{
    [Fact]
    public async Task A_configured_gateway_binds_live_catalog_operations_and_requires_platform_authority()
    {
        using ServiceProvider provider = Build(
            ("Agents:EventStore:BaseUrl", "https://eventstore.example"),
            ("Agents:EventStore:AppId", "eventstore"));
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IProviderCatalogOperations>()
            .ShouldBeOfType<EventStoreProviderCatalogOperations>();
        scope.ServiceProvider.GetRequiredService<IProviderCatalogReader>()
            .ShouldBeOfType<ProjectedProviderCatalogReader>();
        scope.ServiceProvider.GetRequiredService<IAgentsClient>().ProviderCatalog
            .ShouldBeOfType<EventStoreProviderCatalogOperations>();
        scope.ServiceProvider.GetRequiredService<IAgentsClient>().AgentAdministration
            .ShouldBeOfType<EventStoreAgentAdministrationOperations>();

        AgentOperationResult<ProviderCatalogCommandAcceptance> result = await scope.ServiceProvider
            .GetRequiredService<IProviderCatalogOperations>()
            .CreateEntryAsync(new Hexalith.Agents.Contracts.ProviderCatalog.Commands.CreateProviderModelEntry(
                "openai",
                "gpt-4o",
                "OpenAI GPT-4o",
                Enabled: true,
                SupportsTextGeneration: true,
                128_000,
                16_000,
                new ProviderModelTimeoutPolicy(30_000, 3),
                ProviderModelCapabilityFlags.Streaming,
                "cfg-openai-gpt4o",
                new ProviderModelPricing("USD", 0.002m, 0.008m, 0)));
        result.Status.ShouldBe(AgentOperationStatus.NotAuthorized);
    }

    [Fact]
    public void An_unconfigured_gateway_leaves_the_catalog_reader_on_the_seeded_placeholder()
    {
        using ServiceProvider provider = Build();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IProviderCatalogOperations>()
            .ShouldNotBeOfType<EventStoreProviderCatalogOperations>();
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
