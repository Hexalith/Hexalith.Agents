namespace Hexalith.Agents.Server.Tests;

using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Composition;
using Hexalith.Agents.Server.Ports;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using NSubstitute;

using Shouldly;

/// <summary>Verifies the package-safe conversation composition seam.</summary>
public sealed class ConversationServiceCollectionExtensionsTests
{
    [Fact]
    public void PackageSafeRegistrationShouldResolveDeferredPortsAndAffectedOrchestrators()
    {
        var services = new ServiceCollection();
        services.AddSingleton(Substitute.For<IProviderCatalogReader>());
        services.AddSingleton(Substitute.For<IAgentCommandDispatcher>());
        services.AddSingleton(Substitute.For<IAgentGenerationProvider>());
        services.AddSingleton(Substitute.For<IAgentContentSafetyPolicyReader>());
        services.AddSingleton(Substitute.For<IContentSafetyEvaluator>());
        services.AddSingleton(Substitute.For<IAgentPartyReader>());
        services.AddSingleton(Substitute.For<IAgentGeneratedVersionReader>());
        services.AddSingleton(Substitute.For<IApproverPolicyResolver>());

        services.AddAgentsConversationServices(new ConfigurationBuilder().Build());

        using ServiceProvider provider = services.BuildServiceProvider(validateScopes: true);
        provider.GetRequiredService<IConversationContextReader>()
            .ShouldBeOfType<DeferredConversationContextReader>();
        provider.GetRequiredService<IConversationResponsePoster>()
            .ShouldBeOfType<DeferredConversationResponsePoster>();

        using IServiceScope scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<AgentInteractionContextOrchestrator>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<AgentInteractionGenerationOrchestrator>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<AgentInteractionPostingOrchestrator>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<AgentInteractionProposalRegenerationOrchestrator>().ShouldNotBeNull();
        scope.ServiceProvider.GetRequiredService<AgentInteractionProposalApprovalOrchestrator>().ShouldNotBeNull();
    }
}
