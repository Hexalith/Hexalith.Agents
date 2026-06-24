using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Placeholder <see cref="IAgentInvocationReadinessReader"/> registered so the Server DI graph is complete and compiles
/// cleanly, while the live binding to the rehydrated Agent read-model is deferred to the dedicated read-model story
/// (mirroring <c>DeferredAgentConfigurationSnapshotReader</c>). It is never exercised by this story's unit tests, which
/// substitute the seam.
/// </summary>
/// <remarks>
/// Like the snapshot-reader seam, this placeholder <b>fails closed by returning</b>
/// <see cref="AgentInvocationReadiness.NotAvailable"/>: an accidental live call maps every readiness-class check to a
/// clean <c>Unavailable</c> verdict rather than a runtime fault (AD-12; FR-21).
/// </remarks>
public sealed class DeferredAgentInvocationReadinessReader : IAgentInvocationReadinessReader
{
    /// <inheritdoc />
    public Task<AgentInvocationReadiness> ReadAsync(string tenantId, string agentId, CancellationToken ct)
        => Task.FromResult(AgentInvocationReadiness.NotAvailable);
}
