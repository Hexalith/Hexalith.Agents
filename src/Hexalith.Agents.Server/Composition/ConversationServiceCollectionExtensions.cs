namespace Hexalith.Agents.Server.Composition;

using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Ports;
#if HEXALITH_CONVERSATIONS_SOURCE
using Hexalith.Conversations.Client;
#endif

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

/// <summary>Registers the conversation ports and every orchestration that consumes them.</summary>
internal static class ConversationServiceCollectionExtensions
{
    /// <summary>
    /// Registers live conversation adapters only in the source graph when configuration is present; the package
    /// graph always receives fail-closed deferred adapters while retaining a resolvable orchestration graph.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configuration">The host configuration.</param>
    /// <returns>The supplied service collection.</returns>
    internal static IServiceCollection AddAgentsConversationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddSingleton<IConversationContextTokenMeasurer, DeferredConversationContextTokenMeasurer>();
#if HEXALITH_CONVERSATIONS_SOURCE
        if (configuration.GetSection("Conversations").Exists())
        {
            services.AddHexalithConversationsClient(
                options => options.Endpoint = new Uri(configuration["Conversations:BaseUrl"]!));
            services.AddSingleton<IConversationContextReader, ConversationClientContextReader>();
            services.AddSingleton<IConversationResponsePoster, ConversationClientResponsePoster>();
        }
        else
        {
            services.AddSingleton<IConversationContextReader, DeferredConversationContextReader>();
            services.AddSingleton<IConversationResponsePoster, DeferredConversationResponsePoster>();
        }
#else
        services.AddSingleton<IConversationContextReader, DeferredConversationContextReader>();
        services.AddSingleton<IConversationResponsePoster, DeferredConversationResponsePoster>();
#endif

        services.AddScoped<AgentInteractionContextOrchestrator>();
        services.AddScoped<AgentInteractionGenerationOrchestrator>();
        services.AddScoped<AgentInteractionPostingOrchestrator>();
        services.AddScoped<AgentInteractionProposalRegenerationOrchestrator>();
        services.AddScoped<AgentInteractionProposalApprovalOrchestrator>();
        return services;
    }
}
