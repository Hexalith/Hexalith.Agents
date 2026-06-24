using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>UI-side write seam for approving one selected proposal version.</summary>
public interface IProposalApprovalGateway
{
    /// <summary>Approves and posts the selected version.</summary>
    /// <param name="request">The safe approval request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>The safe approval result.</returns>
    Task<ProposalApprovalResult> ApproveProposalAsync(ProposalApprovalRequest request, CancellationToken cancellationToken);
}
