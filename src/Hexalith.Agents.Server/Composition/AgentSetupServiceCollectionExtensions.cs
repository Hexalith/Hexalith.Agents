namespace Hexalith.Agents.Server.Composition;

using System;

using Hexalith.Agents.Client;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Client.Registration;
using Hexalith.EventStore.Client.Gateway;

using Microsoft.Extensions.Options;

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
        services.Configure<ProviderCatalogReadModelOptions>(configuration.GetSection(ProviderCatalogReadModelOptions.SectionName));
        services.TryAddSingleton<IAgentCommandIdentityFactory, AgentCommandIdentityFactory>();
        services.AddHttpContextAccessor();
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.TryAddSingleton<IAgentDependencyAvailability, CommittedAgentDependencyAvailability>();
        services.TryAddSingleton<ICurrentTenantAuthoritySource, UnavailableCurrentTenantAuthoritySource>();
        services.TryAddSingleton<ICurrentHumanActorSource, UnavailableCurrentHumanActorSource>();
        services.TryAddSingleton<ICurrentApproverEvidenceSource, UnavailableCurrentApproverEvidenceSource>();
        services.TryAddSingleton<IAgentTrustedCommandAdmission, UnavailableAgentTrustedCommandAdmission>();
        services.RemoveAll<ITenantAccessReader>();
        services.AddSingleton<ITenantAccessReader, ProjectedTenantAccessReader>();
        services.RemoveAll<IApproverPolicyResolver>();
        services.AddSingleton<IApproverPolicyResolver, ProjectedApproverPolicyResolver>();
        services.TryAddSingleton<IAgentAdministrationContextProvider, HttpAgentAdministrationContextProvider>();
        services.TryAddSingleton(AgentsClient.Unavailable().ProviderCatalog);

        // The projection handler is discovered by the domain-service assembly scan; the orchestrations are always
        // registered so the DI graph resolves whether or not the gateway is bound.
        services.AddScoped<AgentAdministrationOrchestrator>();
        services.AddScoped<ProviderCatalogAdministrationOrchestrator>();
        services.AddScoped<ProviderCatalogMigrationService>();

        // Each branch installs a default status reader with TryAdd, preserving an existing host binding.
        string? baseUrl = configuration[$"{EventStoreSectionName}:BaseUrl"];
        string? appId = configuration[$"{EventStoreSectionName}:AppId"];
        if (string.IsNullOrWhiteSpace(baseUrl)
            || !Uri.TryCreate(baseUrl, UriKind.Absolute, out Uri? baseAddress)
            || (baseAddress.Scheme != Uri.UriSchemeHttp && baseAddress.Scheme != Uri.UriSchemeHttps))
        {
            services.TryAddSingleton<IAgentCommandStatusReader, DeferredAgentCommandStatusReader>();
            // No gateway: keep DeferredAgentCommandDispatcher and the Unavailable client. A host that cannot reach
            // EventStore must report unavailable rather than accept an administration command it cannot persist.
            return services;
        }

        if (string.IsNullOrWhiteSpace(appId))
        {
            services.TryAddSingleton<IAgentCommandStatusReader, DeferredAgentCommandStatusReader>();
            // A direct HTTP binding cannot authenticate the reserved Agents extensions. Without an exact Dapr
            // destination app id the live setup path therefore remains unavailable.
            return services;
        }

        string? daprApiToken = configuration[$"{EventStoreSectionName}:DaprApiToken"];
        IHttpClientBuilder gatewayBuilder = services
            .AddEventStoreGatewayClient(options => options.BaseAddress = baseAddress)
            .AddEventStoreDaprServiceInvocation(appId, daprApiToken);
        services.TryAddScoped<IAgentCommandStatusReader>(provider => new EventStoreAgentCommandStatusReader(domain =>
            new EventStoreGatewayClient(
                provider.GetRequiredService<IHttpClientFactory>().CreateClient(gatewayBuilder.Name),
                provider.GetRequiredService<IOptions<EventStoreGatewayClientOptions>>(),
                new CommandStatusWorkloadAssertionSource(
                    provider.GetService<IEventStoreGatewayWorkloadAssertionSource>(), domain))));

        // Replace the deferred dispatcher rather than TryAdd-ing beside it: for the in-scope administration
        // commands the deferred throw must no longer be reachable.
        services.RemoveAll<IAgentCommandDispatcher>();
        services.AddSingleton<IAgentCommandDispatcher, EventStoreAgentCommandDispatcher>();

        services.AddScoped<IAgentAdministrationOperations, EventStoreAgentAdministrationOperations>();
        services.RemoveAll<IProviderCatalogOperations>();
        services.AddScoped<IProviderCatalogOperations, EventStoreProviderCatalogOperations>();
        services.RemoveAll<IProviderCatalogReader>();
        services.AddScoped<IProviderCatalogReader, ProjectedProviderCatalogReader>();
        services.RemoveAll<IAgentsClient>();
        services.AddScoped<IAgentsClient>(provider => AgentsClient.WithProviderCatalog(
            provider.GetRequiredService<IProviderCatalogOperations>(),
            provider.GetRequiredService<IAgentAdministrationOperations>()));

        return services;
    }
}
