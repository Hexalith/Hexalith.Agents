using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// Deferred placeholder for <see cref="IAgentSetupGateway"/> that keeps the DI graph complete and the UI project
/// buildable before the live Agents read path is wired. Every method returns the fail-closed
/// <see cref="AgentInspectionResult.NotAuthorized"/> result, so a host that has not yet bound the real read path
/// renders the permission-denied surface rather than fabricating a "ready/healthy" Agent (AD-12).
/// </summary>
/// <remarks>
/// The real implementation calls <c>Hexalith.Agents.Client</c> → BFF/API → the Agents read-model and lands with the
/// dedicated Agents read-model / BFF story (Epic 4, 4.1/4.3) — exactly like the deferred ports in
/// <c>Hexalith.Agents.Server</c> (<c>DeferredAgentCommandDispatcher</c>, <c>DeferredProviderCatalogReader</c>). The
/// bUnit tests substitute the gateway with NSubstitute, so this placeholder is never exercised in tests.
/// </remarks>
public sealed class DeferredAgentSetupGateway : IAgentSetupGateway
{
    /// <inheritdoc />
    public Task<AgentInspectionResult> GetStatusAsync(CancellationToken cancellationToken)
        => Task.FromResult(AgentInspectionResult.NotAuthorized());

    /// <inheritdoc />
    public Task<AgentInspectionResult> GetConfigurationAsync(CancellationToken cancellationToken)
        => Task.FromResult(AgentInspectionResult.NotAuthorized());
}
