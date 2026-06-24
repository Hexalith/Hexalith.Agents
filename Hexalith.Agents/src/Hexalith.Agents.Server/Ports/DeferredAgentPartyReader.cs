using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Placeholder <see cref="IAgentPartyReader"/> registered by default so the Server DI graph is complete and compiles
/// cleanly while the live binding to the Agent read-model (<c>AgentState.PartyId</c>) stays deferred (mirroring the other
/// deferred readers). It is never exercised by this story's unit tests, which substitute the seam.
/// </summary>
/// <remarks>
/// This placeholder <b>fails closed by returning</b> <see cref="AgentPartyReadResult.Unavailable"/>: with no Party
/// identity resolvable, posting fails closed to <c>PartyIdentityUnavailable</c> rather than posting with an unverified
/// author identity (AD-7, AD-12).
/// </remarks>
public sealed class DeferredAgentPartyReader : IAgentPartyReader
{
    /// <inheritdoc />
    public Task<AgentPartyReadResult> ReadAsync(string tenantId, string agentId, CancellationToken ct)
        => Task.FromResult(AgentPartyReadResult.Unavailable);
}
