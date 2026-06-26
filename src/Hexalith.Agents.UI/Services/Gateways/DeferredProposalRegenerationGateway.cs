using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// Deferred placeholder for <see cref="IProposalRegenerationGateway"/> that keeps the DI graph complete and the UI project
/// buildable before the live proposal-regeneration write path is wired. Returns the fail-closed
/// <see cref="ProposalRegenerationResult.NotAuthorized"/> result, so a host that has not yet bound the real regeneration path
/// denies the regeneration and discloses nothing rather than fabricating a success — deny, disclose nothing (AD-12), exactly
/// as <see cref="DeferredProposalEditGateway"/> does. It never throws on the happy path.
/// </summary>
/// <remarks>
/// The real implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the Agents regeneration orchestration and lands
/// with the dedicated Agents read-model / BFF story (Epic 4). The bUnit tests substitute the gateway with NSubstitute, so
/// this placeholder is never exercised in tests.
/// </remarks>
public sealed class DeferredProposalRegenerationGateway : IProposalRegenerationGateway
{
    /// <inheritdoc />
    public Task<ProposalRegenerationResult> RegenerateProposalAsync(ProposalRegenerationRequest request, CancellationToken cancellationToken)
        => Task.FromResult(ProposalRegenerationResult.NotAuthorized());
}
