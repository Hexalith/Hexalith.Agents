using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Operations;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// UI-side read seam for the operational-status summary surface (Story 4.3 AC1, AC4; AD-15). The operational-status page
/// depends only on this abstraction and the public <see cref="AgentOperationalStatusSummaryResult"/>/
/// <see cref="AgentOperationalStatusSummaryView"/> contracts; it never touches <c>Hexalith.Agents.Server</c>, EventStore
/// streams, provider SDKs, or aggregate internals. Mirrors <see cref="IProposalQueueGateway"/>; the result is the
/// fail-closed wrapper, so a non-success outcome carries no record identity, counts, or rates (AD-12, AD-14).
/// </summary>
/// <remarks>
/// The live implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the Agents operational read-model path and is
/// deferred to the operational read-model/topology work (AD-16), mirroring the deferred-binding convention. The bUnit
/// component tests substitute this seam with NSubstitute, so the <see cref="DeferredOperationalStatusGateway"/>
/// placeholder is never exercised in tests but keeps the DI graph complete.
/// </remarks>
public interface IOperationalStatusGateway
{
    /// <summary>
    /// Reads the authorized safe <see cref="AgentOperationalStatusSummaryView"/>. Returns a structured fail-closed result;
    /// <see cref="AgentOperationalStatusSummaryResult.Summary"/> is <see langword="null"/> on every
    /// non-<see cref="OperationalStatusInspectionStatus.Success"/>/<see cref="OperationalStatusInspectionStatus.Stale"/>
    /// outcome (denied/unavailable never disclose a count or rate; AD-12, AD-14).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the in-flight read.</param>
    /// <returns>The fail-closed operational-status summary read result.</returns>
    Task<AgentOperationalStatusSummaryResult> GetSummaryAsync(CancellationToken cancellationToken);
}
