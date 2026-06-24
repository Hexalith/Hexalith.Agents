using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal seam that dispatches a fully-built <see cref="CommandEnvelope"/> through the EventStore command
/// path. Binding this to the live DAPR/EventStore gateway is deferred (mirroring Story 1.2/1.3 deferring the
/// read-model binding) so the application orchestration's decision logic stays pure and fully unit-testable: tests
/// substitute this seam and assert the dispatched envelope (command + server-populated trusted extensions).
/// </summary>
public interface IAgentCommandDispatcher
{
    /// <summary>Dispatches the command envelope through the EventStore command path.</summary>
    /// <param name="envelope">The fully-built command envelope (with server-populated trusted extensions).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A task that completes when the command has been dispatched.</returns>
    Task DispatchAsync(CommandEnvelope envelope, CancellationToken ct);
}
