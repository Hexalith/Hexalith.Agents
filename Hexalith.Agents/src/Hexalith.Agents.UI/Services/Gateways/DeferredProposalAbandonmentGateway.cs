using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>Deferred abandonment gateway that fails closed until a live host binds the write path (Story 3.6).</summary>
public sealed class DeferredProposalAbandonmentGateway : IProposalAbandonmentGateway
{
    /// <inheritdoc />
    public Task<ProposalAbandonmentResult> AbandonProposalAsync(ProposalAbandonmentRequest request, CancellationToken cancellationToken)
        => Task.FromResult(ProposalAbandonmentResult.NotAuthorized());
}
