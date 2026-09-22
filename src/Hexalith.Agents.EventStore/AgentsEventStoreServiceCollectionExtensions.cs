using Hexalith.EventStore.Authorization;
using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.Agents.EventStore;

/// <summary>
/// Registers the Hexalith Agents integrations owned by an EventStore gateway host.
/// </summary>
public static class AgentsEventStoreServiceCollectionExtensions
{
    /// <summary>
    /// Adds all live Agent setup intent adapters and the reserved-extension policy for one Agents Dapr application.
    /// </summary>
    /// <param name="services">The gateway service collection.</param>
    /// <param name="agentsAppId">The exact allow-listed Dapr application id of the trusted Agents service.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddAgentsEventStore(this IServiceCollection services, string agentsAppId)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(agentsAppId);
        if (!string.Equals(agentsAppId, agentsAppId.Trim(), StringComparison.Ordinal))
        {
            throw new ArgumentException("The Agents Dapr application id must not have surrounding whitespace.", nameof(agentsAppId));
        }

        foreach (AgentsTrustedCommandExtensionPolicy registeredPolicy in services
            .Where(descriptor => descriptor.ServiceType == typeof(ITrustedCommandExtensionPolicy))
            .Select(descriptor => descriptor.ImplementationInstance)
            .OfType<AgentsTrustedCommandExtensionPolicy>())
        {
            if (!string.Equals(registeredPolicy.AgentsAppId, agentsAppId, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("The Agents EventStore integration is already registered for a different Dapr application id.");
            }
        }

        services.AddIdempotencyIntentAdapter<CreateAgentIdempotencyIntentAdapter>();
        services.AddIdempotencyIntentAdapter<UpdateAgentConfigurationIdempotencyIntentAdapter>();
        services.AddIdempotencyIntentAdapter<ConfigureAgentResponseModeIdempotencyIntentAdapter>();
        services.AddIdempotencyIntentAdapter<ActivateAgentIdempotencyIntentAdapter>();
        services.AddIdempotencyIntentAdapter<DisableAgentIdempotencyIntentAdapter>();
        services.TryAddEnumerable(ServiceDescriptor.Singleton<ITrustedCommandExtensionPolicy>(
            new AgentsTrustedCommandExtensionPolicy(agentsAppId)));
        return services;
    }
}
