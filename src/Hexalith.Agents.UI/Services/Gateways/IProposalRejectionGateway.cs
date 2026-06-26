using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>UI-side write seam for rejecting a pending proposal (Story 3.6; AC1, AC4).</summary>
public interface IProposalRejectionGateway
{
    /// <summary>Rejects the pending proposal, moving it to the rejected terminal state.</summary>
    /// <param name="request">The safe rejection request (ids + optional rationale code only).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The safe rejection result.</returns>
    Task<ProposalRejectionResult> RejectProposalAsync(ProposalRejectionRequest request, CancellationToken cancellationToken);
}
