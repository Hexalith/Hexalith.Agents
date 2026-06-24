using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// UI-side read seam for the governed Agent (<c>hexa</c>) status/configuration surfaces (AC2, AC4; AD-15). The
/// admin-setup pages depend only on this abstraction and the public display contracts, never on
/// <c>Hexalith.Agents.Server</c>, EventStore streams, provider SDKs, or aggregate internals.
/// </summary>
/// <remarks>
/// The live implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the Agents read-model and is deferred
/// to the dedicated Agents read-model / BFF story (Epic 4, 4.1/4.3), mirroring the 1.2–1.7 deferred-binding
/// convention. The bUnit component tests substitute this seam with NSubstitute, so the
/// <see cref="DeferredAgentSetupGateway"/> placeholder is never exercised in tests but keeps the DI graph complete.
/// Every method returns the existing fail-closed result wrapper so a non-success outcome carries no Agent data
/// (AD-12, AD-14).
/// </remarks>
public interface IAgentSetupGateway
{
    /// <summary>
    /// Loads the safe Agent status view backing the Agents overview (and the lifecycle/blocker sections of the
    /// configuration form). Returns a structured fail-closed result; <see cref="AgentInspectionResult.Agent"/> is
    /// non-null only on <see cref="AgentInspectionStatus.Success"/>.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the in-flight read.</param>
    /// <returns>The fail-closed Agent inspection result.</returns>
    Task<AgentInspectionResult> GetStatusAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Loads the safe Agent configuration view backing the configuration form. Returns the same safe
    /// <see cref="AgentStatusView"/> as <see cref="GetStatusAsync"/> via a structured fail-closed result.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for the in-flight read.</param>
    /// <returns>The fail-closed Agent inspection result.</returns>
    Task<AgentInspectionResult> GetConfigurationAsync(CancellationToken cancellationToken);
}
