using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// Deferred placeholder for <see cref="ILaunchReadinessGateway"/> that keeps the DI graph complete and the UI project
/// buildable before the live read path is wired. Returns the fail-closed <see cref="LaunchReadinessResult.NotAuthorized"/>
/// result, so a host that has not yet bound the real read path renders the permission-denied surface rather than
/// fabricated readiness — deny, disclose nothing (AD-12), exactly as <see cref="DeferredOperationalStatusGateway"/> does.
/// </summary>
/// <remarks>
/// The real implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the Agents read-model and lands with the
/// read-model/topology work (AD-16). The bUnit tests substitute the gateway with NSubstitute, so this placeholder is
/// never exercised in tests.
/// </remarks>
public sealed class DeferredLaunchReadinessGateway : ILaunchReadinessGateway
{
    /// <inheritdoc />
    public Task<LaunchReadinessResult> GetLaunchReadinessAsync(CancellationToken cancellationToken)
        => Task.FromResult(LaunchReadinessResult.NotAuthorized());
}
