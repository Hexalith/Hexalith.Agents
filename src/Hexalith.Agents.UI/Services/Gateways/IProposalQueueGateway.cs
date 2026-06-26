using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.AgentInteraction;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// UI-side read seam for the proposal-queue discovery surface (AC1, AC3, AC4; AD-15). The proposal-queue page depends
/// only on this abstraction and the public <see cref="PendingProposalsResult"/>/<see cref="PendingProposalView"/>
/// contracts; it never touches <c>Hexalith.Agents.Server</c>, EventStore streams, provider SDKs, or aggregate internals.
/// Mirrors <see cref="IConversationAgentCallGateway"/>.
/// </summary>
/// <remarks>
/// The live implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the Agents read-model path and is deferred to
/// the dedicated Agents read-model / BFF story (Epic 4, 4.1/4.3), mirroring the deferred-binding convention. The bUnit
/// component tests substitute this seam with NSubstitute, so the <see cref="DeferredProposalQueueGateway"/> placeholder
/// is never exercised in tests but keeps the DI graph complete. The method returns a fail-closed result wrapper so a
/// non-success outcome carries no record identity (AD-12, AD-14).
/// </remarks>
public interface IProposalQueueGateway
{
    /// <summary>
    /// Lists the authorized safe <see cref="PendingProposalView"/> rows backing the proposal queue. Returns a structured
    /// fail-closed result; <see cref="PendingProposalsResult.Proposals"/> is empty (and <see cref="PendingProposalsResult.PendingCount"/>
    /// is zero) on every non-<see cref="PendingProposalsInspectionStatus.Success"/>/<see cref="PendingProposalsInspectionStatus.Stale"/> outcome.
    /// </summary>
    /// <param name="includeHistorical">When <see langword="true"/>, authorized historical/terminal proposals are also included (Stories 3.5/3.6).</param>
    /// <param name="cancellationToken">Cancellation token for the in-flight read.</param>
    /// <returns>The fail-closed pending-proposals read result.</returns>
    Task<PendingProposalsResult> ListPendingProposalsAsync(bool includeHistorical, CancellationToken cancellationToken);
}
