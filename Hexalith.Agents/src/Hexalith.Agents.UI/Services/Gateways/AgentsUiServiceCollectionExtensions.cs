using Hexalith.Agents.UI.Services.Gateways;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.Agents.UI;

/// <summary>
/// Service-collection registration for the Agents admin-setup UI. Registers the UI-side read gateways behind a
/// single seam so the (deferred) runnable host and the bUnit component tests share one registration point. The
/// gateways are registered scoped (a circuit/request-scoped read seam), mirroring the Tenants UI gateway
/// registration.
/// </summary>
public static class AgentsUiServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Agents admin-setup UI read gateways with their deferred placeholder implementations. A host
    /// that wires the live read path replaces these registrations behind the same interfaces; the bUnit tests
    /// substitute them with NSubstitute. Uses <c>TryAdd</c> so an earlier live registration always wins.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddAgentsUi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddScoped<IAgentSetupGateway, DeferredAgentSetupGateway>();
        services.TryAddScoped<IProviderCatalogGateway, DeferredProviderCatalogGateway>();
        services.TryAddScoped<IConversationAgentCallGateway, DeferredConversationAgentCallGateway>();
        services.TryAddScoped<IProposalQueueGateway, DeferredProposalQueueGateway>();
        services.TryAddScoped<IProposalEditGateway, DeferredProposalEditGateway>();

        return services;
    }
}
