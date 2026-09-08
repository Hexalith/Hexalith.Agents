using Hexalith.Agents.UI.Services.Gateways;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.Agents.UI;

/// <summary>
/// Service-collection registration for the Agents admin-setup UI. Registers the UI-side gateways behind a
/// single seam so the runnable host and the bUnit component tests share one registration point. The
/// gateways are registered scoped (a circuit/request-scoped seam), mirroring the Tenants UI gateway
/// registration.
/// </summary>
public static class AgentsUiServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Agents admin-setup UI gateways with their deferred placeholder implementations. A host
    /// that wires the live read path replaces these registrations behind the same interfaces; the bUnit tests
    /// substitute them with NSubstitute. Uses <c>TryAdd</c> so an earlier live registration always wins.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddAgentsUi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.TryAddScoped<IAgentSetupGateway, DeferredAgentSetupGateway>();
        services.TryAddScoped<IProviderCatalogGateway, DeferredProviderCatalogGateway>();
        services.TryAddScoped<IConversationAgentCallGateway, DeferredConversationAgentCallGateway>();
        services.TryAddScoped<IProposalQueueGateway, DeferredProposalQueueGateway>();
        services.TryAddScoped<IProposalDetailGateway, DeferredProposalDetailGateway>();
        services.TryAddScoped<IProposalEditGateway, DeferredProposalEditGateway>();
        services.TryAddScoped<IProposalRegenerationGateway, DeferredProposalRegenerationGateway>();
        services.TryAddScoped<IProposalApprovalGateway, DeferredProposalApprovalGateway>();
        services.TryAddScoped<IProposalRejectionGateway, DeferredProposalRejectionGateway>();
        services.TryAddScoped<IProposalAbandonmentGateway, DeferredProposalAbandonmentGateway>();
        services.TryAddScoped<IOperationalStatusGateway, DeferredOperationalStatusGateway>();
        services.TryAddScoped<IAuditEvidenceGateway, DeferredAuditEvidenceGateway>();
        services.TryAddScoped<ILaunchReadinessGateway, DeferredLaunchReadinessGateway>();

        return services;
    }

    /// <summary>
    /// Registers the live Agent setup gateway over the public Agents client (Story 5.2), and otherwise leaves the
    /// deferred placeholder in place. Registration is skipped when no Agent target is configured, so a host that
    /// has not named its Agent keeps the fail-closed surface instead of issuing unaddressed reads and writes.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>The same service collection, for chaining.</returns>
    public static IServiceCollection AddAgentsUiSetup(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        IConfigurationSection section = configuration.GetSection(AgentSetupTargetOptions.SectionName);
        _ = services.Configure<AgentSetupTargetOptions>(section);

        if (!string.IsNullOrWhiteSpace(section["AgentId"]))
        {
            services.TryAddScoped<IAgentSetupGateway, AgentsClientSetupGateway>();
        }

        return services.AddAgentsUi();
    }
}
