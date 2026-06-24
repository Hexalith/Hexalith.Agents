using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal port the Agent Call request orchestration uses to read a governed Agent's current configuration
/// as the AD-4 snapshot (Story 2.1; AD-3, AD-4, AD-9). The implementation reads the in-module <c>Agent</c> state
/// through the EventStore read path and maps it to a safe <see cref="AgentConfigurationSnapshot"/>; it MUST never
/// surface a secret value, provider SDK type, prompt, or Conversation content across this boundary (AD-9, AD-14).
/// </summary>
/// <remarks>
/// Keeping this a port (rather than reading the Agent inline) preserves the AD-3 round-trip: the pure
/// <c>AgentInteraction</c> aggregate receives the snapshot through the command, never by reading the <c>Agent</c>
/// aggregate itself. The live read-model/DAPR binding is deferred (mirroring Story 1.2) so the orchestration's
/// snapshot-assembly logic stays fully unit-testable here. The reader returns a populated snapshot only for an Agent
/// that has passed activation; otherwise it returns <see cref="AgentConfigurationSnapshot.NotAvailable"/>.
/// </remarks>
public interface IAgentConfigurationSnapshotReader
{
    /// <summary>Reads the Agent's current configuration snapshot, or a fail-closed not-available result (AC1).</summary>
    /// <param name="tenantId">The Agent's tenant scope (cross-tenant reads fail closed as not-available).</param>
    /// <param name="agentId">The target Agent identifier to read.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe snapshot read result (availability + optional safe snapshot).</returns>
    Task<AgentConfigurationSnapshot> ReadAsync(string tenantId, string agentId, CancellationToken ct);
}
