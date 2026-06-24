using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Placeholder <see cref="IAgentCommandDispatcher"/> registered so the Server DI graph is complete and compiles
/// cleanly, while the live binding to the DAPR/EventStore command gateway is deferred to the operational-topology
/// story (mirroring Story 1.2/1.3 deferring the read-model binding). It throws a clear, actionable error if it is
/// ever invoked at runtime before the real dispatcher is wired — it is never exercised by this story's unit tests,
/// which substitute the seam.
/// </summary>
public sealed class DeferredAgentCommandDispatcher : IAgentCommandDispatcher
{
    /// <inheritdoc />
    public Task DispatchAsync(CommandEnvelope envelope, CancellationToken ct)
        => throw new NotSupportedException(
            "The live Agents command dispatcher is not wired yet (Story 1.4 defers the DAPR/EventStore command-path "
            + "binding, mirroring Story 1.2/1.3). Register a concrete IAgentCommandDispatcher in the operational-topology story.");
}
