using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// Fail-closed placeholder for <see cref="IAgentSetupGateway"/> used by a host that has not bound the live Agents
/// client. Reads return not-authorized and writes return <see cref="AgentSetupWriteStatus.Unavailable"/>, so an
/// unbound host renders the permission-denied surface rather than fabricating a "ready/healthy" Agent or letting a
/// write appear to succeed (AD-12).
/// </summary>
/// <remarks>
/// The live implementation is <see cref="AgentsClientSetupGateway"/>, registered by
/// <c>AddAgentsUiSetup</c> when an Agent target is configured (<c>Agents:Ui:AgentId</c>). Plain <c>AddAgentsUi</c>
/// always leaves this placeholder in place. The bUnit component tests substitute this seam with NSubstitute, so it
/// is never exercised in them.
/// </remarks>
public sealed class DeferredAgentSetupGateway : IAgentSetupGateway
{
    /// <inheritdoc />
    public Task<AgentInspectionResult> GetStatusAsync(CancellationToken cancellationToken)
        => Task.FromResult(AgentInspectionResult.NotAuthorized());

    /// <inheritdoc />
    public Task<AgentInspectionResult> GetConfigurationAsync(CancellationToken cancellationToken)
        => Task.FromResult(AgentInspectionResult.NotAuthorized());

    /// <inheritdoc />
    public Task<AgentSetupResult> GetSetupAsync(int? expectedConfigurationVersion, CancellationToken cancellationToken)
        => Task.FromResult(AgentSetupResult.NotAuthorized());

    /// <inheritdoc />
    public Task<AgentSetupWriteResult> UpdateConfigurationAsync(UpdateAgentConfiguration command, CancellationToken cancellationToken)
        => Unavailable();

    /// <inheritdoc />
    public Task<AgentSetupWriteResult> ConfigureResponseModeAsync(AgentResponseMode mode, CancellationToken cancellationToken)
        => Unavailable();

    /// <inheritdoc />
    public Task<AgentSetupWriteResult> ActivateAsync(CancellationToken cancellationToken)
        => Unavailable();

    /// <inheritdoc />
    public Task<AgentSetupWriteResult> DisableAsync(CancellationToken cancellationToken)
        => Unavailable();

    private static Task<AgentSetupWriteResult> Unavailable()
        => Task.FromResult(AgentSetupWriteResult.Failed(AgentSetupWriteStatus.Unavailable));
}
