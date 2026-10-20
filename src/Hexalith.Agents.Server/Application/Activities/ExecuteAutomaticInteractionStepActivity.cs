using Dapr.Workflow;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Application.Workflows;

namespace Hexalith.Agents.Server.Application.Activities;

/// <summary>One exact owner read/effect/command checkpoint; activity contracts contain only execution references.</summary>
public sealed class ExecuteAutomaticInteractionStepActivity(AutomaticInteractionStepOrchestrator orchestrator) : WorkflowActivity<WorkflowExecutionState, WorkflowExecutionState>
{
    /// <inheritdoc />
    public override async Task<WorkflowExecutionState> RunAsync(WorkflowActivityContext context, WorkflowExecutionState input)
    {
        ArgumentNullException.ThrowIfNull(context);
        try { return await orchestrator.ExecuteAsync(input, CancellationToken.None).ConfigureAwait(false); }
        catch (Exception)
        {
            // This boundary supplies CancellationToken.None. Adapter-thrown cancellation is a dependency failure here;
            // Dapr must receive only the safe retry code, never the adapter's cancellation or other error text.
            throw new AutomaticInteractionCheckpointUnavailableException();
        }
    }
}
