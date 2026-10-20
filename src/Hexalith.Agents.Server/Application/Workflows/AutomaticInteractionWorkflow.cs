using Dapr.Workflow;
using Hexalith.Agents.Server.Application.Activities;

namespace Hexalith.Agents.Server.Application.Workflows;

/// <summary>The one durable owner, with EventStore as business truth and the interaction id as exact instance id.</summary>
public sealed class AutomaticInteractionWorkflow : Workflow<WorkflowExecutionState, WorkflowExecutionState>
{
    /// <inheritdoc />
    public override async Task<WorkflowExecutionState> RunAsync(WorkflowContext context, WorkflowExecutionState input)
    {
        ArgumentNullException.ThrowIfNull(context); WorkflowExecutionStateValidation.Validate(input);
        if (context.InstanceId != input.AgentInteractionId) { throw new ArgumentException("WorkflowInstanceMismatch", nameof(input)); }
        var retry = new WorkflowTaskOptions(new WorkflowRetryPolicy(100, TimeSpan.FromSeconds(1), maxRetryInterval: TimeSpan.FromMinutes(1)));
        WorkflowExecutionState state = input;
        while (state.Status == WorkflowExecutionStatus.Running)
        {
            state = await context.CallActivityAsync<WorkflowExecutionState>(nameof(ExecuteAutomaticInteractionStepActivity), state, retry).ConfigureAwait(true);
            WorkflowExecutionStateValidation.Validate(state);
            context.SetCustomStatus(state);
        }
        return state;
    }
}
