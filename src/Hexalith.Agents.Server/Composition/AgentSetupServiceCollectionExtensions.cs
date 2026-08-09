namespace Hexalith.Agents.Server.Composition;

using System;

using Hexalith.Agents.Client;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Client.Registration;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

/// <summary>
/// Registers the Story 5.2 live Agent setup path: the EventStore command dispatcher, the administration
/// orchestrations, the projected setup read model, and the public administration operations.
/// </summary>
internal static class AgentSetupServiceCollectionExtensions
{
    /// <summary>The configuration section carrying the EventStore gateway binding for Agent administration.</summary>
    internal const string EventStoreSectionName = "Agents:EventStore";

    /// <summary>
    /// Registers the live administration path when the EventStore gateway is configured, and otherwise leaves the
    /// fail-closed deferred seams in place so a host without a gateway cannot silently accept mutations.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>The supplied service collection.</returns>
    internal static IServiceCollection AddAgentSetupServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AgentSetupReadModelOptions>(configuration.GetSection(AgentSetupReadModelOptions.SectionName));
        services.TryAddSingleton<IAgentCommandIdentityFactory, AgentCommandIdentityFactory>();

        // The projection handler is discovered by the domain-service assembly scan; the orchestrations are always
        // registered so the DI graph resolves whether or not the gateway is bound.
        services.AddScoped<AgentAdministrationOrchestrator>();

        string? baseUrl = configuration[$"{EventStoreSectionName}:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? baseAddress))
        {
            // No gateway: keep DeferredAgentCommandDispatcher and the Unavailable client. A host that cannot reach
            // EventStore must report unavailable rather than accept an administration command it cannot persist.
            return services;
        }

        _ = services.AddEventStoreGatewayClient(options => options.BaseAddress = baseAddress);
        services.AddHttpContextAccessor();
        services.AddSingleton<IAgentAdministrationContextProvider, HttpAgentAdministrationContextProvider>();

        // Replace the deferred dispatcher rather than TryAdd-ing beside it: for the in-scope administration
        // commands the deferred throw must no longer be reachable.
        services.RemoveAll<IAgentCommandDispatcher>();
        services.AddSingleton<IAgentCommandDispatcher, EventStoreAgentCommandDispatcher>();

        services.AddScoped<IAgentAdministrationOperations, EventStoreAgentAdministrationOperations>();
        services.RemoveAll<IAgentsClient>();
        services.AddScoped<IAgentsClient>(provider => AgentsClient.WithAdministration(
            provider.GetRequiredService<IAgentAdministrationOperations>()));

        return services;
    }
}
