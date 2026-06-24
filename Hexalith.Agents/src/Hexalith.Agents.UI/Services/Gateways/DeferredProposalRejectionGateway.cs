using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>Deferred rejection gateway that fails closed until a live host binds the write path (Story 3.6).</summary>
public sealed class DeferredProposalRejectionGateway : IProposalRejectionGateway
{
    /// <inheritdoc />
    public Task<ProposalRejectionResult> RejectProposalAsync(ProposalRejectionRequest request, CancellationToken cancellationToken)
        => Task.FromResult(ProposalRejectionResult.NotAuthorized());
}
