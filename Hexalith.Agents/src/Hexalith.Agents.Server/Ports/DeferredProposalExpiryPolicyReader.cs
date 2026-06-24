using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.Server.Ports;

/// <summary>
/// Placeholder <see cref="IProposalExpiryPolicyReader"/> registered by default so the Server DI graph is complete and
/// compiles cleanly while the live binding to the proposal expiry policy stays deferred to Story 3.6 (mirroring the other
/// deferred readers). It is never exercised by this story's unit tests, which substitute the seam.
/// </summary>
/// <remarks>
/// This placeholder returns <see cref="ProposalExpiryPolicyResult.None"/> (no expiry configured): proposal creation records
/// no expiry metadata until the live expiry-policy binding and expiry enforcement are wired in Story 3.6. Recording no
/// expiry is the safe default — the optional <c>ExpiresAt</c> is "where configured" (AC1), so its absence simply means an
/// unbounded proposal until enforcement arrives.
/// </remarks>
public sealed class DeferredProposalExpiryPolicyReader : IProposalExpiryPolicyReader
{
    /// <inheritdoc />
    public Task<ProposalExpiryPolicyResult> ReadAsync(string tenantId, string agentId, CancellationToken ct)
        => Task.FromResult(ProposalExpiryPolicyResult.None);
}
