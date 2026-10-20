using Hexalith.Agents.Server.Application.Workflows;

namespace Hexalith.Agents.Server.Ports;

/// <summary>No runtime is registered by the default host while the runtime security decision is Open.</summary>
public sealed class DeferredInteractionWorkflowScheduler : IInteractionWorkflowScheduler
{
    /// <inheritdoc />
    public Task EnsureScheduledAsync(WorkflowExecutionState input, CancellationToken ct) => throw new NotSupportedException("DependencyNotAvailable(OD-DAPR-SECURITY-1)");
}
