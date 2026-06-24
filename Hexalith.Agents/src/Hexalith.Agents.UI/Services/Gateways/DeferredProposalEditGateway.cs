using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// Deferred placeholder for <see cref="IProposalEditGateway"/> that keeps the DI graph complete and the UI project
/// buildable before the live proposal-edit write path is wired. Returns the fail-closed
/// <see cref="ProposalEditResult.NotAuthorized"/> result, so a host that has not yet bound the real edit path denies the
/// edit and discloses nothing rather than fabricating a success — deny, disclose nothing (AD-12), exactly as
/// <see cref="DeferredProposalQueueGateway"/> does. It never throws on the happy path.
/// </summary>
/// <remarks>
/// The real implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the Agents edit orchestration and lands with
/// the dedicated Agents read-model / BFF story (Epic 4). The bUnit tests substitute the gateway with NSubstitute, so this
/// placeholder is never exercised in tests.
/// </remarks>
public sealed class DeferredProposalEditGateway : IProposalEditGateway
{
    /// <inheritdoc />
    public Task<ProposalEditResult> EditProposalAsync(ProposalEditRequest request, CancellationToken cancellationToken)
        => Task.FromResult(ProposalEditResult.NotAuthorized());
}
