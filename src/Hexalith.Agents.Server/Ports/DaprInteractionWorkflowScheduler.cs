using Dapr.Workflow;
using Hexalith.Agents.Server.Application.Workflows;

namespace Hexalith.Agents.Server.Ports;

/// <summary>Exact Dapr instance lookup precedes start and resolves concurrent start or a lost acknowledgement.</summary>
public sealed class DaprInteractionWorkflowScheduler(IDaprWorkflowClient client) : IInteractionWorkflowScheduler
{
    /// <inheritdoc />
    public async Task EnsureScheduledAsync(WorkflowExecutionState input, CancellationToken ct)
    {
        WorkflowExecutionStateValidation.Validate(input);
        if (HasUsableOwner(await client.GetWorkflowStateAsync(input.AgentInteractionId, false, ct).ConfigureAwait(false))) { return; }
        try
        {
            await client.ScheduleNewWorkflowAsync(nameof(AutomaticInteractionWorkflow), input.AgentInteractionId, input, (DateTimeOffset?)null, ct).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (!HasUsableOwner(await client.GetWorkflowStateAsync(input.AgentInteractionId, false, ct).ConfigureAwait(false))) { throw; }
        }
    }
    private static bool HasUsableOwner(WorkflowState state)
    {
        if (!state.Exists) { return false; }
        if (state.RuntimeStatus is WorkflowRuntimeStatus.Pending or WorkflowRuntimeStatus.Running or WorkflowRuntimeStatus.Suspended
            or WorkflowRuntimeStatus.ContinuedAsNew or WorkflowRuntimeStatus.Completed) { return true; }
        throw new InvalidOperationException("InteractionWorkflowRecoveryRequired");
    }
}
