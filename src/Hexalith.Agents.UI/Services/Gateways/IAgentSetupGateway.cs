using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// UI-side seam for the governed Agent (<c>hexa</c>) setup surfaces (AC2, AC4; AD-15; Story 5.2). The admin-setup
/// pages depend only on this abstraction and the public display contracts, never on <c>Hexalith.Agents.Server</c>,
/// EventStore streams, provider SDKs, or aggregate internals.
/// </summary>
/// <remarks>
/// <para>
/// Reads answer with the same authoritative <see cref="AgentSetupResult"/> the API and the public client serve, so
/// FrontComposer cannot drift from them. Writes answer with an <see cref="AgentSetupWriteResult"/> whose accepted
/// identity means <em>submitted</em> and nothing more — the page must re-read the setup at the accepted
/// configuration version to learn whether the projection has confirmed the change.
/// </para>
/// <para>
/// Nothing on this seam reports callability. Lifecycle <see cref="AgentLifecycleStatus.Active"/> is a lifecycle
/// flag; whether an Agent can be called is decided by later stories' readiness gates.
/// </para>
/// </remarks>
public interface IAgentSetupGateway
{
    /// <summary>
    /// Loads the safe Agent status view backing the Agents overview. Returns a structured fail-closed result;
    /// <see cref="AgentInspectionResult.Agent"/> is non-null only on <see cref="AgentInspectionStatus.Success"/>.
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

    /// <summary>
    /// Loads the authoritative setup truth — the safe status view plus configuration version, projection version,
    /// freshness, and truth stage.
    /// </summary>
    /// <param name="expectedConfigurationVersion">
    /// The configuration version the page is waiting to see after a write, or <see langword="null"/> for current
    /// projected truth.
    /// </param>
    /// <param name="cancellationToken">Cancellation token for the in-flight read.</param>
    /// <returns>The fail-closed setup result.</returns>
    Task<AgentSetupResult> GetSetupAsync(int? expectedConfigurationVersion, CancellationToken cancellationToken);

    /// <summary>Submits a safe configuration metadata change.</summary>
    /// <param name="command">The safe update payload.</param>
    /// <param name="cancellationToken">Cancellation token for the in-flight write.</param>
    /// <returns>The fail-closed write result.</returns>
    Task<AgentSetupWriteResult> UpdateConfigurationAsync(UpdateAgentConfiguration command, CancellationToken cancellationToken);

    /// <summary>Submits a Response Mode change, which affects future Agent Calls only.</summary>
    /// <param name="mode">The chosen Response Mode.</param>
    /// <param name="cancellationToken">Cancellation token for the in-flight write.</param>
    /// <returns>The fail-closed write result.</returns>
    Task<AgentSetupWriteResult> ConfigureResponseModeAsync(AgentResponseMode mode, CancellationToken cancellationToken);

    /// <summary>Submits an activation. Acceptance is a lifecycle transition, never a callability claim.</summary>
    /// <param name="cancellationToken">Cancellation token for the in-flight write.</param>
    /// <returns>The fail-closed write result.</returns>
    Task<AgentSetupWriteResult> ActivateAsync(CancellationToken cancellationToken);

    /// <summary>Submits a disable.</summary>
    /// <param name="cancellationToken">Cancellation token for the in-flight write.</param>
    /// <returns>The fail-closed write result.</returns>
    Task<AgentSetupWriteResult> DisableAsync(CancellationToken cancellationToken);
}
