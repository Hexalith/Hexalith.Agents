using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>UI-side write seam for abandoning a pending proposal (Story 3.6; AC2, AC4).</summary>
public interface IProposalAbandonmentGateway
{
    /// <summary>Abandons the pending proposal, moving it to the abandoned terminal state.</summary>
    /// <param name="request">The safe abandonment request (ids only).</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The safe abandonment result.</returns>
    Task<ProposalAbandonmentResult> AbandonProposalAsync(ProposalAbandonmentRequest request, CancellationToken cancellationToken);
}
