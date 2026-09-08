using System.Collections.Generic;
using System.Linq;

using Hexalith.Agents.Client;
using Hexalith.Agents.UI;
using Hexalith.Agents.UI.Services.Gateways;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

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
        services.Single(d => d.ServiceType == typeof(TimeProvider)).Lifetime.ShouldBe(ServiceLifetime.Singleton);

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
        scope.ServiceProvider.GetRequiredService<TimeProvider>().ShouldBeSameAs(TimeProvider.System);
    }

    [Fact]
    public void AddAgentsUi_does_not_override_an_existing_live_registration()
    {
        ServiceCollection services = new();
        services.AddScoped<IAgentSetupGateway, DeferredAgentSetupGateway>();

        services.AddAgentsUi();

        services.Count(d => d.ServiceType == typeof(IAgentSetupGateway)).ShouldBe(1);
    }

    [Fact]
    public void AddAgentsUi_does_not_override_an_existing_time_provider_registration()
    {
        ServiceCollection services = new();
        TimeProvider clock = new FixedTimeProvider(new DateTimeOffset(2026, 9, 8, 12, 0, 0, TimeSpan.Zero));
        services.AddSingleton(clock);

        services.AddAgentsUi();

        services.Count(d => d.ServiceType == typeof(TimeProvider)).ShouldBe(1);
        using ServiceProvider provider = services.BuildServiceProvider();
        provider.GetRequiredService<TimeProvider>().ShouldBeSameAs(clock);
    }

    // ===== Story 5.2: the live setup composition =====

    [Fact]
    public void AddAgentsUiSetup_resolves_the_live_gateway_when_an_agent_target_is_configured()
    {
        ServiceCollection services = new();
        services.AddSingleton(AgentsClient.Unavailable());

        services.AddAgentsUiSetup(Configuration(("Agents:Ui:AgentId", "hexa")));

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IAgentSetupGateway>().ShouldBeOfType<AgentsClientSetupGateway>();
        scope.ServiceProvider.GetRequiredService<IOptions<AgentSetupTargetOptions>>().Value.AgentId.ShouldBe("hexa");
    }

    [Fact]
    public void AddAgentsUiSetup_stays_fail_closed_when_no_agent_target_is_configured()
    {
        ServiceCollection services = new();
        services.AddSingleton(AgentsClient.Unavailable());

        services.AddAgentsUiSetup(Configuration());

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        // An unnamed Agent must not produce unaddressed reads and writes — the deferred, denying gateway stands.
        scope.ServiceProvider.GetRequiredService<IAgentSetupGateway>().ShouldBeOfType<DeferredAgentSetupGateway>();
    }

    [Fact]
    public void AddAgentsUiSetup_still_completes_the_rest_of_the_gateway_graph()
    {
        ServiceCollection services = new();
        services.AddSingleton(AgentsClient.Unavailable());

        services.AddAgentsUiSetup(Configuration(("Agents:Ui:AgentId", "hexa")));

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IProviderCatalogGateway>().ShouldBeOfType<AgentsClientProviderCatalogGateway>();
        scope.ServiceProvider.GetRequiredService<ILaunchReadinessGateway>().ShouldBeOfType<DeferredLaunchReadinessGateway>();
    }

    [Fact]
    public void AddAgentsUiSetup_resolves_the_live_catalog_gateway()
    {
        ServiceCollection services = new();
        services.AddSingleton(AgentsClient.Unavailable());

        services.AddAgentsUiSetup(Configuration());

        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        scope.ServiceProvider.GetRequiredService<IProviderCatalogGateway>().ShouldBeOfType<AgentsClientProviderCatalogGateway>();
    }

    private static IConfiguration Configuration(params (string Key, string Value)[] values)
        => new ConfigurationBuilder()
            .AddInMemoryCollection(values.Select(pair => new KeyValuePair<string, string?>(pair.Key, pair.Value)))
            .Build();
}
