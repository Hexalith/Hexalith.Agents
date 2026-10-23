using Hexalith.Agents.Client;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;
using Hexalith.Parties.Client.Extensions;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.Agents.Server.Composition;

/// <summary>
/// Registers the domain-service collaborators the Agents host composes around the EventStore domain-service SDK.
/// </summary>
internal static class AgentDomainHostComposition
{
    /// <summary>
    /// Adds the domain host's service graph. Gateway admission adapters are not part of this graph.
    /// </summary>
    /// <param name="builder">The domain host builder, after the EventStore domain service is added.</param>
    internal static void Configure(WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Domain-service collaborators consume the shared SDK's transitive Dapr client.
        builder.Services.AddDaprClient();

        // Optional sibling clients validate their configuration when present.
        if (builder.Configuration.GetSection("Parties").Exists())
        {
            builder.Services.AddPartiesClient(builder.Configuration);
        }

        builder.Services.AddScoped<IAgentPartyDirectory, PartiesAgentPartyDirectory>();
        // Start with deferred ports; AddAgentSetupServices below selects live bindings when configured.
        builder.Services.AddSingleton<IAgentCommandDispatcher, DeferredAgentCommandDispatcher>();
        builder.Services.AddScoped<AgentPartyIdentityOrchestrator>();

        builder.Services.AddSingleton<IProviderCatalogReader, DeferredProviderCatalogReader>();
        builder.Services.AddScoped<AgentProviderSelectionOrchestrator>();
        builder.Services.AddScoped<AgentActivationProviderRevalidation>();

        builder.Services.AddSingleton<IApproverPolicyResolver, DeferredApproverPolicyResolver>();
        builder.Services.AddScoped<AgentResponseModeOrchestrator>();
        builder.Services.AddScoped<AgentApproverPolicyOrchestrator>();

        builder.Services.AddScoped<AgentContentSafetyPolicyOrchestrator>();

        builder.Services.AddSingleton<IAgentConfigurationSnapshotReader, DeferredAgentConfigurationSnapshotReader>();
        builder.Services.AddScoped<AgentInteractionRequestOrchestrator>();

        builder.Services.AddSingleton<ITenantAccessReader, DeferredTenantAccessReader>();
        builder.Services.AddSingleton<IConversationAccessReader, DeferredConversationAccessReader>();
        builder.Services.AddSingleton<IAgentInvocationReadinessReader, DeferredAgentInvocationReadinessReader>();
        builder.Services.AddSingleton<IAgentInteractionAuditStateReader, DeferredAgentInteractionAuditStateReader>();
        builder.Services.AddSingleton<IAgentAuditGovernanceReadinessProvider, AgentAuditGovernanceReadinessProvider>();
        builder.Services.AddScoped<AgentInteractionGateOrchestrator>();

        builder.Services.AddSingleton<IAgentGenerationProvider, DeferredAgentGenerationProvider>();
        builder.Services.AddSingleton<IContentSafetyEvaluator, DeferredContentSafetyEvaluator>();
        builder.Services.AddSingleton<IAgentContentSafetyPolicyReader, DeferredAgentContentSafetyPolicyReader>();

        builder.Services.AddSingleton<IAgentPartyReader, DeferredAgentPartyReader>();
        builder.Services.AddSingleton<IAgentGeneratedVersionReader, DeferredAgentGeneratedVersionReader>();
        builder.Services.AddAgentsConversationServices(builder.Configuration);

        builder.Services.AddSingleton<IProposalExpiryPolicyReader, DeferredProposalExpiryPolicyReader>();
        builder.Services.AddScoped<AgentInteractionProposalOrchestrator>();

        builder.Services.AddScoped<AgentInteractionProposalEditOrchestrator>();

        builder.Services.AddScoped<AgentInteractionProposalRejectionOrchestrator>();
        builder.Services.AddScoped<AgentInteractionProposalAbandonmentOrchestrator>();
        builder.Services.AddScoped<AgentInteractionProposalExpiryOrchestrator>();

        // Setup always registers projected tenant/approver readers, whose authority sources default to unavailable.
        // An HTTP(S) EventStore BaseUrl and a nonblank AppId enable live setup dispatch and operations.
        builder.Services.AddAgentSetupServices(builder.Configuration);

        // Automatic interactions remain gated by dependency availability and directory readiness.
        builder.Services.TryAddSingleton<IAgentDependencyAvailability, CommittedAgentDependencyAvailability>();
        builder.Services.TryAddSingleton<IAutomaticInteractionIntakeGate, DeferredAutomaticInteractionIntakeGate>();
        builder.Services.TryAddSingleton<IInteractionDirectoryReadiness, DeferredInteractionDirectoryReadiness>();
        builder.Services.TryAddSingleton<IConversationAgentStateReader, DeferredConversationAgentStateReader>();
        builder.Services.TryAddSingleton<IAgentInteractionStateReader, DeferredAgentInteractionStateReader>();
        builder.Services.TryAddSingleton<IInteractionWorkflowScheduler, DeferredInteractionWorkflowScheduler>();
        builder.Services.TryAddSingleton<IAutomaticInteractionStepEffects, DeferredAutomaticInteractionStepEffects>();
        builder.Services.AddScoped<AutomaticInteractionIntakeOrchestrator>();
        builder.Services.AddScoped<InteractionOutboxRecoveryOrchestrator>();
        builder.Services.AddScoped<AutomaticInteractionStepOrchestrator>();

        builder.Services.TryAddSingleton<IAgentsClient>(_ => AgentsClient.Unavailable());
    }
}
