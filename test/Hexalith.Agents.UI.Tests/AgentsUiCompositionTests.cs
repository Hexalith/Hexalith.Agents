using System.Linq;

using Hexalith.Agents.UI;
using Hexalith.Agents.UI.Services.Gateways;

using Microsoft.Extensions.DependencyInjection;

using Shouldly;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// AddAgentsUi keeps the DI graph complete: it registers both read gateways (scoped) behind their interfaces with
/// the deferred placeholder implementations, so the (deferred) host and the tests share one registration seam.
/// </summary>
public sealed class AgentsUiCompositionTests
{
    [Fact]
    public void AddAgentsUi_registers_all_gateways_scoped_with_deferred_placeholders()
    {
        ServiceCollection services = new();

        services.AddAgentsUi();

        services.Single(d => d.ServiceType == typeof(IAgentSetupGateway)).Lifetime.ShouldBe(ServiceLifetime.Scoped);
        services.Single(d => d.ServiceType == typeof(IProviderCatalogGateway)).Lifetime.ShouldBe(ServiceLifetime.Scoped);
        services.Single(d => d.ServiceType == typeof(IConversationAgentCallGateway)).Lifetime.ShouldBe(ServiceLifetime.Scoped);
        services.Single(d => d.ServiceType == typeof(IProposalQueueGateway)).Lifetime.ShouldBe(ServiceLifetime.Scoped);
        services.Single(d => d.ServiceType == typeof(IProposalDetailGateway)).Lifetime.ShouldBe(ServiceLifetime.Scoped);
        // Story 4.3 — the operational-status and audit-evidence read gateways are registered scoped + fail-closed.
        services.Single(d => d.ServiceType == typeof(IOperationalStatusGateway)).Lifetime.ShouldBe(ServiceLifetime.Scoped);
        services.Single(d => d.ServiceType == typeof(IAuditEvidenceGateway)).Lifetime.ShouldBe(ServiceLifetime.Scoped);
        // Story 4.4 — the launch-readiness read gateway is registered scoped + fail-closed.
        services.Single(d => d.ServiceType == typeof(ILaunchReadinessGateway)).Lifetime.ShouldBe(ServiceLifetime.Scoped);

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IAgentSetupGateway>().ShouldBeOfType<DeferredAgentSetupGateway>();
        scope.ServiceProvider.GetRequiredService<IProviderCatalogGateway>().ShouldBeOfType<DeferredProviderCatalogGateway>();
        scope.ServiceProvider.GetRequiredService<IConversationAgentCallGateway>().ShouldBeOfType<DeferredConversationAgentCallGateway>();
        scope.ServiceProvider.GetRequiredService<IProposalQueueGateway>().ShouldBeOfType<DeferredProposalQueueGateway>();
        scope.ServiceProvider.GetRequiredService<IProposalDetailGateway>().ShouldBeOfType<DeferredProposalDetailGateway>();
        scope.ServiceProvider.GetRequiredService<IOperationalStatusGateway>().ShouldBeOfType<DeferredOperationalStatusGateway>();
        scope.ServiceProvider.GetRequiredService<IAuditEvidenceGateway>().ShouldBeOfType<DeferredAuditEvidenceGateway>();
        scope.ServiceProvider.GetRequiredService<ILaunchReadinessGateway>().ShouldBeOfType<DeferredLaunchReadinessGateway>();
    }

    [Fact]
    public void AddAgentsUi_does_not_override_an_existing_live_registration()
    {
        ServiceCollection services = new();
        services.AddScoped<IAgentSetupGateway, DeferredAgentSetupGateway>();

        services.AddAgentsUi();

        services.Count(d => d.ServiceType == typeof(IAgentSetupGateway)).ShouldBe(1);
    }
}
