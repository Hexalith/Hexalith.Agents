using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Contracts.Operations;

using Microsoft.Extensions.Options;

namespace Hexalith.Agents.UI.Services.Gateways;

/// <summary>
/// Live <see cref="IAgentSetupGateway"/> over the public Agents client (Story 5.2). FrontComposer reads exactly
/// what the API and any other automation client read, and writes through the same public operations — so the
/// surfaces cannot disagree about the current setup truth.
/// </summary>
/// <remarks>
/// The gateway adds no authorization of its own and holds no tenant: the server derives both from the
/// authenticated principal. A non-success operation result keeps its own meaning — denied stays denied, missing
/// stays missing, and everything else reads as unavailable — so a transport failure is never shown to an
/// administrator as a permission problem, and no failure path ever fabricates Agent state.
/// </remarks>
public sealed class AgentsClientSetupGateway(
    IAgentsClient client,
    IOptions<AgentSetupTargetOptions> options) : IAgentSetupGateway
{
    private readonly IAgentsClient _client = client ?? throw new ArgumentNullException(nameof(client));
    private readonly IOptions<AgentSetupTargetOptions> _options = options ?? throw new ArgumentNullException(nameof(options));

    /// <inheritdoc />
    public async Task<AgentInspectionResult> GetStatusAsync(CancellationToken cancellationToken)
        => ToInspection(await GetSetupAsync(expectedConfigurationVersion: null, cancellationToken).ConfigureAwait(false));

    /// <inheritdoc />
    public async Task<AgentInspectionResult> GetConfigurationAsync(CancellationToken cancellationToken)
        => ToInspection(await GetSetupAsync(expectedConfigurationVersion: null, cancellationToken).ConfigureAwait(false));

    /// <inheritdoc />
    public async Task<AgentSetupResult> GetSetupAsync(int? expectedConfigurationVersion, CancellationToken cancellationToken)
    {
        if (AgentId is not { Length: > 0 } agentId)
        {
            // No configured target is a host misconfiguration, not a denial — say unavailable rather than implying
            // the administrator lacks permission.
            return AgentSetupResult.Unavailable();
        }

        AgentOperationResult<AgentSetupResult> result = await _client.AgentAdministration
            .GetConfigurationAsync(agentId, expectedConfigurationVersion, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        // The server already answers with a structured AgentSetupResult on success, including its own
        // not-authorized / not-found shapes. Only an outer transport-or-operation failure has to be mapped here,
        // and each outcome keeps its own meaning instead of collapsing into a blanket denial.
        return result is { IsSuccess: true, Value: { } setup } ? setup : ToReadResult(result.Status);
    }

    /// <inheritdoc />
    public Task<AgentSetupWriteResult> UpdateConfigurationAsync(UpdateAgentConfiguration command, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return WriteAsync(
            (agentId, ct) => _client.AgentAdministration.UpdateConfigurationAsync(agentId, command, cancellationToken: ct),
            cancellationToken);
    }

    /// <inheritdoc />
    public Task<AgentSetupWriteResult> ConfigureResponseModeAsync(AgentResponseMode mode, CancellationToken cancellationToken)
        => WriteAsync(
            (agentId, ct) => _client.AgentAdministration.ConfigureResponseModeAsync(agentId, new ConfigureAgentResponseMode(mode), cancellationToken: ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<AgentSetupWriteResult> ActivateAsync(CancellationToken cancellationToken)
        => WriteAsync(
            (agentId, ct) => _client.AgentAdministration.ActivateAsync(agentId, new ActivateAgent(), cancellationToken: ct),
            cancellationToken);

    /// <inheritdoc />
    public Task<AgentSetupWriteResult> DisableAsync(CancellationToken cancellationToken)
        => WriteAsync(
            (agentId, ct) => _client.AgentAdministration.DisableAsync(agentId, new DisableAgent(), cancellationToken: ct),
            cancellationToken);

    private string? AgentId => string.IsNullOrWhiteSpace(_options.Value.AgentId) ? null : _options.Value.AgentId;

    private static AgentInspectionResult ToInspection(AgentSetupResult setup)
        => setup.Setup is { } view
            ? AgentInspectionResult.Success(view.Agent)
            : new AgentInspectionResult(setup.Status, null);

    private static AgentSetupResult ToReadResult(AgentOperationStatus status)
        => status switch
        {
            AgentOperationStatus.NotAuthorized => AgentSetupResult.NotAuthorized(),
            AgentOperationStatus.NotFound => AgentSetupResult.NotFound(),
            _ => AgentSetupResult.Unavailable(),
        };

    private static AgentSetupWriteStatus ToWriteStatus(AgentOperationStatus status)
        => status switch
        {
            AgentOperationStatus.NotAuthorized => AgentSetupWriteStatus.NotAuthorized,
            AgentOperationStatus.NotFound => AgentSetupWriteStatus.NotFound,
            AgentOperationStatus.ValidationFailed => AgentSetupWriteStatus.ValidationFailed,
            AgentOperationStatus.Conflict or AgentOperationStatus.Stale => AgentSetupWriteStatus.Conflict,
            _ => AgentSetupWriteStatus.Unavailable,
        };

    private async Task<AgentSetupWriteResult> WriteAsync(
        Func<string, CancellationToken, ValueTask<AgentOperationResult<AgentCommandAcceptance>>> write,
        CancellationToken cancellationToken)
    {
        if (AgentId is not { Length: > 0 } agentId)
        {
            return AgentSetupWriteResult.Failed(AgentSetupWriteStatus.Unavailable);
        }

        AgentOperationResult<AgentCommandAcceptance> result = await write(agentId, cancellationToken).ConfigureAwait(false);

        return result is { IsSuccess: true, Value: { } acceptance }
            ? AgentSetupWriteResult.Submitted(acceptance)
            : AgentSetupWriteResult.Failed(ToWriteStatus(result.Status));
    }
}
