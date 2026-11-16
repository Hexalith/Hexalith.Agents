using Dapr.Workflow;
using Microsoft.Extensions.Logging;
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Application.Workflows;

namespace Hexalith.Agents.Server.Application.Activities;

/// <summary>One exact owner read/effect/command checkpoint; activity contracts contain only execution references.</summary>
public sealed partial class ExecuteAutomaticInteractionStepActivity(AutomaticInteractionStepOrchestrator orchestrator, ILogger<ExecuteAutomaticInteractionStepActivity> logger) : WorkflowActivity<WorkflowExecutionState, WorkflowExecutionState>
{
    /// <inheritdoc />
    public override async Task<WorkflowExecutionState> RunAsync(WorkflowActivityContext context, WorkflowExecutionState input)
    {
        ArgumentNullException.ThrowIfNull(context);
        try { return await orchestrator.ExecuteAsync(input, CancellationToken.None).ConfigureAwait(false); }
        catch (Exception exception)
        {
            // This boundary supplies CancellationToken.None. Adapter-thrown cancellation is a dependency failure here;
            // Dapr must receive only the safe retry code, never the adapter's cancellation or other error text.
            string interactionId = "invalid-interaction-id";
            try { WorkflowExecutionStateValidation.Validate(input); interactionId = input.AgentInteractionId; }
            catch (ArgumentException) { }
            try { LogCheckpointFailure(logger, exception.GetType().Name, interactionId); }
            catch (Exception) { /* A failed diagnostic sink must also remain outside Dapr failure history. */ }
            throw new AutomaticInteractionCheckpointUnavailableException();
        }
    }

    [LoggerMessage(EventId = 1, Level = LogLevel.Warning, Message = "Automatic interaction checkpoint failed: {ExceptionType}; interaction {AgentInteractionId}")]
    private static partial void LogCheckpointFailure(ILogger logger, string exceptionType, string agentInteractionId);
}
