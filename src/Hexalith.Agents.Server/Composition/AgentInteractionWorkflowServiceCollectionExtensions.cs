using Dapr.Workflow;
using Hexalith.Agents.Server.Application.Activities;
using Hexalith.Agents.Server.Application.Workflows;
using Hexalith.Agents.Server.Ports;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Hexalith.Agents.Server.Composition;

/// <summary>Explicit qualified-host opt-in only; never called by the default Agents host.</summary>
public static class AgentInteractionWorkflowServiceCollectionExtensions
{
    /// <summary>Registers the sole durable owner only after the exact runtime security record is Available.</summary>
    public static IServiceCollection AddAgentsInteractionWorkflow(this IServiceCollection services, IAgentDependencyAvailability availability)
    {
        ArgumentNullException.ThrowIfNull(services); ArgumentNullException.ThrowIfNull(availability);
        if (availability.Check("OD-DAPR-SECURITY-1") is { } blocker) { throw new NotSupportedException($"DependencyNotAvailable({blocker.RecordId})"); }
        services.AddDaprWorkflow(options => { options.RegisterWorkflow<AutomaticInteractionWorkflow>(); options.RegisterActivity<ExecuteAutomaticInteractionStepActivity>(); });
        services.Replace(ServiceDescriptor.Singleton<IInteractionWorkflowScheduler, DaprInteractionWorkflowScheduler>());
        return services;
    }
}
