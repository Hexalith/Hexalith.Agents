using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Server-internal port the invocation gate orchestration uses to read a governed Agent's CURRENT readiness — lifecycle,
/// linked Party identity, Response Mode, active Content Safety Policy, and selected provider/model — at gate time
/// (Story 2.2; AC1; AD-3, AD-12). This is the current-state analogue of Story 2.1's
/// <see cref="IAgentConfigurationSnapshotReader"/>: keep the two distinct — one freezes config at request time, this
/// one reads live readiness at gate time. The implementation reads the in-module Agent read-model and maps it to a safe
/// <see cref="AgentInvocationReadiness"/>; it MUST never surface a secret, provider SDK type, prompt, or Conversation
/// content across this boundary (AD-9, AD-14).
/// </summary>
/// <remarks>
/// Keeping this a port preserves the AD-3 round-trip: the pure aggregate receives the readiness verdicts through the
/// trusted gate command, never by reading the Agent itself. The live read-model binding is deferred (mirroring Story
/// 1.2) so the gate logic stays fully unit-testable here; the deferred reader fails closed by returning
/// <see cref="AgentInvocationReadiness.NotAvailable"/>.
/// </remarks>
public interface IAgentInvocationReadinessReader
{
    /// <summary>Reads the Agent's current readiness, or a fail-closed not-available result (AC1).</summary>
    /// <param name="tenantId">The Agent's tenant scope (cross-tenant reads fail closed as not-available).</param>
    /// <param name="agentId">The target Agent identifier to read.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The safe Agent readiness result.</returns>
    Task<AgentInvocationReadiness> ReadAsync(string tenantId, string agentId, CancellationToken ct);
}
