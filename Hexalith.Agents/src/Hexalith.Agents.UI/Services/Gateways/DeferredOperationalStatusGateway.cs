using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Operations;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// Deferred placeholder for <see cref="IOperationalStatusGateway"/> that keeps the DI graph complete and the UI project
/// buildable before the live operational read-model path is wired. Returns the fail-closed
/// <see cref="AgentOperationalStatusSummaryResult.NotAuthorized"/> result, so a host that has not yet bound the real read
/// path renders the permission-denied surface rather than fabricated counts/rates — deny, disclose nothing (AD-12),
/// exactly as <see cref="DeferredProposalQueueGateway"/> does.
/// </summary>
/// <remarks>
/// The real implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the Agents operational read-model and lands
/// with the operational read-model/topology work (AD-16). The bUnit tests substitute the gateway with NSubstitute, so
/// this placeholder is never exercised in tests.
/// </remarks>
public sealed class DeferredOperationalStatusGateway : IOperationalStatusGateway
{
    /// <inheritdoc />
    public Task<AgentOperationalStatusSummaryResult> GetSummaryAsync(CancellationToken cancellationToken)
        => Task.FromResult(AgentOperationalStatusSummaryResult.NotAuthorized());
}
