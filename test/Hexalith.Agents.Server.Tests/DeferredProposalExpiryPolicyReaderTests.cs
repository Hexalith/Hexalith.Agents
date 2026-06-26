namespace Hexalith.Agents.Server.Tests;

using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Server.Ports;

using Shouldly;

/// <summary>
/// Guard for the placeholder <see cref="DeferredProposalExpiryPolicyReader"/> (Story 3.1 deferred expiry binding). The live
/// expiry-policy binding and expiry enforcement are deferred to Story 3.6, so the registered seam records no expiry — the
/// optional <c>ExpiresAt</c> is "where configured" (AC1), and its absence simply means an unbounded proposal until
/// enforcement arrives. This locks the deferred placeholder's fail-closed (no-expiry) contract.
/// </summary>
public sealed class DeferredProposalExpiryPolicyReaderTests
{
    [Fact]
    public async Task Read_returns_no_expiry_until_the_live_binding_is_wired()
    {
        var reader = new DeferredProposalExpiryPolicyReader();

        ProposalExpiryPolicyResult result = await reader.ReadAsync("acme", "hexa", CancellationToken.None);

        result.ExpiresAt.ShouldBeNull();
    }
}
