using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>Deferred approval gateway that fails closed until a live host binds the write path.</summary>
public sealed class DeferredProposalApprovalGateway : IProposalApprovalGateway
{
    /// <inheritdoc />
    public Task<ProposalApprovalResult> ApproveProposalAsync(ProposalApprovalRequest request, CancellationToken cancellationToken)
        => Task.FromResult(ProposalApprovalResult.NotAuthorized());
}
