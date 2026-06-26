using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// Deferred placeholder for <see cref="IProposalQueueGateway"/> that keeps the DI graph complete and the UI project
/// buildable before the live proposal-queue read path is wired. Returns the fail-closed
/// <see cref="PendingProposalsResult.NotAuthorized"/> result, so a host that has not yet bound the real read path
/// renders the permission-denied surface rather than an empty "no proposals" success — deny, disclose nothing
/// (AD-12), exactly as <see cref="DeferredProviderCatalogGateway"/> does.
/// </summary>
/// <remarks>
/// The real implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the Agents read-model and lands with the
/// dedicated Agents read-model / BFF story (Epic 4). The bUnit tests substitute the gateway with NSubstitute, so this
/// placeholder is never exercised in tests.
/// </remarks>
public sealed class DeferredProposalQueueGateway : IProposalQueueGateway
{
    /// <inheritdoc />
    public Task<PendingProposalsResult> ListPendingProposalsAsync(bool includeHistorical, CancellationToken cancellationToken)
        => Task.FromResult(PendingProposalsResult.NotAuthorized());
}
