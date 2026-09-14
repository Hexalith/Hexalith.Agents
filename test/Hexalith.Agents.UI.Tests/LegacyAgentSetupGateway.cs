using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.UI.Services.Gateways;

namespace Hexalith.Agents.UI.Tests;

/// <summary>
/// Represents a pre-Story-5.2 gateway implementation that supplies only the original interface members.
/// </summary>
internal sealed class LegacyAgentSetupGateway : IAgentSetupGateway
{
    /// <inheritdoc />
    public Task<AgentInspectionResult> GetStatusAsync(CancellationToken cancellationToken)
        => Task.FromResult(AgentInspectionResult.NotAuthorized());

    /// <inheritdoc />
    public Task<AgentInspectionResult> GetConfigurationAsync(CancellationToken cancellationToken)
        => Task.FromResult(AgentInspectionResult.NotAuthorized());

    /// <inheritdoc />
    public Task<AgentSetupResult> GetSetupAsync(
        int? expectedConfigurationVersion,
        CancellationToken cancellationToken)
        => Task.FromResult(AgentSetupResult.Unavailable());

    /// <inheritdoc />
    public Task<AgentSetupWriteResult> UpdateConfigurationAsync(
        UpdateAgentConfiguration command,
        CancellationToken cancellationToken)
        => throw new InvalidOperationException("The legacy write overload must not be called for an exact retry.");

    /// <inheritdoc />
    public Task<AgentSetupWriteResult> ConfigureResponseModeAsync(
        AgentResponseMode mode,
        CancellationToken cancellationToken)
        => throw new InvalidOperationException("The legacy write overload must not be called for an exact retry.");

    /// <inheritdoc />
    public Task<AgentSetupWriteResult> ActivateAsync(CancellationToken cancellationToken)
        => throw new InvalidOperationException("The legacy write overload must not be called for an exact retry.");

    /// <inheritdoc />
    public Task<AgentSetupWriteResult> DisableAsync(CancellationToken cancellationToken)
        => throw new InvalidOperationException("The legacy write overload must not be called for an exact retry.");
}
