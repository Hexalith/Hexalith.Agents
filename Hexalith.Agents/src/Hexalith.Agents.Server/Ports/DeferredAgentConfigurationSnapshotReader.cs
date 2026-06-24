using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Placeholder <see cref="IAgentConfigurationSnapshotReader"/> registered so the Server DI graph is complete and
/// compiles cleanly, while the live binding to the rehydrated <c>AgentState</c> read-model (over the EventStore read
/// path) is deferred to the dedicated read-model story (mirroring Story 1.2 deferring its read-path binding). It is
/// never exercised by this story's unit tests, which substitute the seam.
/// </summary>
/// <remarks>
/// Unlike the throw-on-invoke deferred dispatcher/catalog-reader seams, this placeholder <b>fails closed by
/// returning <see cref="AgentConfigurationSnapshot.NotAvailable"/></b>: an accidental live call therefore yields a
/// clean structural <c>MissingAgentSnapshot</c> rejection from the aggregate rather than a runtime fault, while the
/// command dispatch itself stays the deferred boundary (the <c>DeferredAgentCommandDispatcher</c> throws). The
/// snapshot-assembly logic is implemented and unit-tested now with a stubbed reader.
/// </remarks>
public sealed class DeferredAgentConfigurationSnapshotReader : IAgentConfigurationSnapshotReader
{
    /// <inheritdoc />
    public Task<AgentConfigurationSnapshot> ReadAsync(string tenantId, string agentId, CancellationToken ct)
        => Task.FromResult(AgentConfigurationSnapshot.NotAvailable);
}
