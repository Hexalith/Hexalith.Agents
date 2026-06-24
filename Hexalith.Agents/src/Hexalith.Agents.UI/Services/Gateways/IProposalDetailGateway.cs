using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// UI-side read seam for the single-proposal detail workspace (AC1, AC2; AD-15). The proposal-detail page depends only
/// on this abstraction and the public <see cref="ProposalDetailResult"/>/<see cref="ProposalDetailView"/> contracts; it
/// never touches <c>Hexalith.Agents.Server</c>, EventStore streams, provider SDKs, or aggregate internals. Mirrors
/// <see cref="IProposalQueueGateway"/>; the request is the single safe Agent Call id and the result is the fail-closed
/// wrapper, so a non-success outcome carries no record identity (AD-12, AD-14).
/// </summary>
/// <remarks>
/// The live implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the Agents read-model path and is deferred to
/// the dedicated Agents read-model / BFF story (Epic 4, 4.1/4.3), mirroring the deferred-binding convention. The bUnit
/// component tests substitute this seam with NSubstitute, so the <see cref="DeferredProposalDetailGateway"/> placeholder
/// is never exercised in tests but keeps the DI graph complete.
/// </remarks>
public interface IProposalDetailGateway
{
    /// <summary>
    /// Reads the authorized safe <see cref="ProposalDetailView"/> for one Proposed Agent Reply. Returns a structured
    /// fail-closed result; <see cref="ProposalDetailResult.Detail"/> is <see langword="null"/> on every
    /// non-<see cref="ProposalDetailInspectionStatus.Success"/>/<see cref="ProposalDetailInspectionStatus.Stale"/>
    /// outcome (denied/unavailable/not-found never disclose a record; AD-12, AD-14).
    /// </summary>
    /// <param name="agentInteractionId">The deterministic Agent Call identifier whose proposal detail is read.</param>
    /// <param name="cancellationToken">Cancellation token for the in-flight read.</param>
    /// <returns>The fail-closed proposal-detail read result.</returns>
    Task<ProposalDetailResult> GetProposalDetailAsync(string agentInteractionId, CancellationToken cancellationToken);
}
