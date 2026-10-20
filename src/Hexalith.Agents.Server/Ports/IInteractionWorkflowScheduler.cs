using Hexalith.Agents.Server.Application.Workflows;

namespace Hexalith.Agents.Server.Ports;

/// <summary>The sole host-owned Dapr scheduling seam. Exact instance lookup resolves a lost acknowledgement.</summary>
public interface IInteractionWorkflowScheduler
{
    /// <summary>Schedules the exact instance only if absent, preserving every existing nonterminal or terminal instance.</summary>
    Task EnsureScheduledAsync(WorkflowExecutionState input, CancellationToken ct);
}
