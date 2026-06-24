using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Placeholder <see cref="IAgentContentSafetyPolicyReader"/> registered by default so the Server DI graph is complete and
/// compiles cleanly while the live binding to the Agent read-model (<c>AgentState.ContentSafety</c>) stays deferred
/// (mirroring the other deferred readers). It is never exercised by this story's unit tests, which substitute the seam.
/// </summary>
/// <remarks>
/// This placeholder <b>fails closed by returning</b> <see cref="AgentContentSafetyPolicyReadResult.NotAvailable"/>: with
/// no effective policy resolvable, generation fails closed to <c>PolicyFailure</c> rather than skipping the safety gate
/// (AD-12; FR-27).
/// </remarks>
public sealed class DeferredAgentContentSafetyPolicyReader : IAgentContentSafetyPolicyReader
{
    /// <inheritdoc />
    public Task<AgentContentSafetyPolicyReadResult> ReadAsync(string tenantId, string agentId, AgentResponseMode mode, CancellationToken ct)
        => Task.FromResult(AgentContentSafetyPolicyReadResult.NotAvailable);
}
