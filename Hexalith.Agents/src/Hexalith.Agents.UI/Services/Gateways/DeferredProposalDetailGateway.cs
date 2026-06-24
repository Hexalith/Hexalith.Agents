using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// Deferred placeholder for <see cref="IProposalDetailGateway"/> that keeps the DI graph complete and the UI project
/// buildable before the live proposal-detail read path is wired. Returns the fail-closed
/// <see cref="ProposalDetailResult.NotAuthorized"/> result, so a host that has not yet bound the real read path renders
/// the permission-denied surface rather than a fabricated detail — deny, disclose nothing (AD-12), exactly as
/// <see cref="DeferredProposalQueueGateway"/> does.
/// </summary>
/// <remarks>
/// The real implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the Agents read-model and lands with the
/// dedicated Agents read-model / BFF story (Epic 4). The bUnit tests substitute the gateway with NSubstitute, so this
/// placeholder is never exercised in tests.
/// </remarks>
public sealed class DeferredProposalDetailGateway : IProposalDetailGateway
{
    /// <inheritdoc />
    public Task<ProposalDetailResult> GetProposalDetailAsync(string agentInteractionId, CancellationToken cancellationToken)
        => Task.FromResult(ProposalDetailResult.NotAuthorized());
}
