using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// UI-side write seam for an authorized Approver's edit of a pending Proposed Agent Reply (Story 3.3; AC1, AC3; AD-15).
/// The proposal-editor component depends only on this abstraction and the public display contracts; it never touches
/// <c>Hexalith.Agents.Server</c>, EventStore streams, provider SDKs, or aggregate internals. Mirrors
/// <see cref="IConversationAgentCallGateway"/>.
/// </summary>
/// <remarks>
/// The live implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the Agents edit orchestration and is deferred
/// to the dedicated Agents read-model / BFF story (Epic 4, 4.1/4.3), mirroring the deferred-binding convention. The bUnit
/// component tests substitute this seam with NSubstitute, so the <see cref="DeferredProposalEditGateway"/> placeholder is
/// never exercised in tests but keeps the DI graph complete. The method returns a fail-closed result wrapper so a
/// non-success outcome carries no version identity and never throws on the happy path; a genuine cancellation propagates
/// (AD-12, AD-14).
/// </remarks>
public interface IProposalEditGateway
{
    /// <summary>
    /// Submits the authorized edit of a pending Proposed Agent Reply. Returns a structured fail-closed result; the
    /// <see cref="ProposalEditResult.EditedVersionId"/> is non-null only on <see cref="ProposalEditStatus.Edited"/>.
    /// </summary>
    /// <param name="request">The safe edit inputs (the sensitive edited content flows here only — AD-14).</param>
    /// <param name="cancellationToken">Cancellation token for the in-flight edit.</param>
    /// <returns>The fail-closed proposal-edit result.</returns>
    Task<ProposalEditResult> EditProposalAsync(ProposalEditRequest request, CancellationToken cancellationToken);
}
