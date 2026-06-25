using System.Threading;
using System.Threading.Tasks;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// UI-side read seam for the launch-readiness surface (Story 4.4 AC4; AD-15). The launch-readiness page depends only on
/// this abstraction and the public <see cref="LaunchReadinessResult"/>/<see cref="Contracts.Agent.AgentLaunchReadinessView"/>
/// contracts; it never touches <c>Hexalith.Agents.Server</c>, EventStore streams, provider SDKs, or aggregate internals.
/// Mirrors <see cref="IOperationalStatusGateway"/>; the result is the fail-closed wrapper, so a non-success outcome
/// carries no readiness data (AD-12, AD-14).
/// </summary>
/// <remarks>
/// The live implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the Agents read-model path and is deferred to
/// the read-model/topology work (AD-16), mirroring the deferred-binding convention. The bUnit component tests substitute
/// this seam with NSubstitute, so the <see cref="DeferredLaunchReadinessGateway"/> placeholder is never exercised in
/// tests but keeps the DI graph complete.
/// </remarks>
public interface ILaunchReadinessGateway
{
    /// <summary>
    /// Reads the authorized safe <see cref="Contracts.Agent.AgentLaunchReadinessView"/>. Returns a structured fail-closed
    /// result; <see cref="LaunchReadinessResult.Readiness"/> is <see langword="null"/> on every
    /// non-<see cref="LaunchReadinessInspectionStatus.Success"/> outcome (denied/unavailable disclose nothing; AD-12, AD-14).
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the in-flight read.</param>
    /// <returns>The fail-closed launch-readiness read result.</returns>
    Task<LaunchReadinessResult> GetLaunchReadinessAsync(CancellationToken cancellationToken);
}
