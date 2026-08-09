using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent.Commands;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Application orchestration for the Agent administration mutations that carry no dependency verdict (Story 5.2
/// AC1): <see cref="CreateAgent"/>, <see cref="UpdateAgentConfiguration"/>, and <see cref="DisableAgent"/>. Each
/// authorizes the actor, builds the command envelope, and dispatches it with the <b>server-populated</b> trusted
/// <c>actor:agentsAdmin</c> extension.
/// </summary>
/// <remarks>
/// <b>Trust model (CRITICAL):</b> the reserved extension keys (<c>actor:agentsAdmin</c> plus the activation-path
/// verdict keys <c>provider:selectionValidation</c> / <c>approver:policyValidation</c>) are server-populated only.
/// Any client-supplied value for them is stripped here; only <c>actor:agentsAdmin</c> is repopulated from the
/// trusted authorization decision, so a client can neither forge admin nor smuggle an activation verdict onto a
/// configuration command — exactly the rule <see cref="AgentResponseModeOrchestrator"/> applies. Activation keeps
/// its own orchestration (<see cref="AgentActivationProviderRevalidation"/>) because it must re-resolve the
/// provider and approver dependencies before the aggregate's gates can clear.
/// </remarks>
public sealed class AgentAdministrationOrchestrator
{
    private const string _agentDomain = "agent";

    private static readonly string[] _reservedExtensionKeys =
    [
        AgentProviderSelectionOrchestrator.AgentAdminExtensionKey,
        AgentProviderSelectionOrchestrator.ProviderSelectionValidationExtensionKey,
        AgentActivationProviderRevalidation.ApproverPolicyValidationExtensionKey,
    ];

    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="AgentAdministrationOrchestrator"/> class.</summary>
    /// <param name="dispatcher">The command-dispatch seam.</param>
    public AgentAdministrationOrchestrator(IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        _dispatcher = dispatcher;
    }

    /// <summary>Authorizes and dispatches the Agent creation command.</summary>
    /// <param name="request">The sanitized orchestration request.</param>
    /// <param name="command">The safe create payload.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The orchestration outcome (denied, or dispatched).</returns>
    /// <remarks>
    /// <see cref="CreateAgent.TenantId"/> is the one command field that names a trust boundary, so it is replaced
    /// with the tenant the server derived from the authenticated caller. A body that asks to create an Agent in
    /// another tenant is written into the requesting tenant instead of being honoured, keeping the stored tenant
    /// scope and the envelope tenant identical for every created aggregate.
    /// </remarks>
    public Task<AgentAdministrationOutcome> CreateAsync(
        AgentAdministrationRequest request,
        CreateAgent command,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(command);

        return DispatchAsync(
            request,
            nameof(CreateAgent),
            command with { TenantId = request.TenantId },
            ct);
    }

    /// <summary>Authorizes and dispatches the configuration update command.</summary>
    /// <param name="request">The sanitized orchestration request.</param>
    /// <param name="command">The safe update payload.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The orchestration outcome (denied, or dispatched).</returns>
    public Task<AgentAdministrationOutcome> UpdateConfigurationAsync(
        AgentAdministrationRequest request,
        UpdateAgentConfiguration command,
        CancellationToken ct)
        => DispatchAsync(request, nameof(UpdateAgentConfiguration), command, ct);

    /// <summary>Authorizes and dispatches the disable command (a lifecycle flag flip; all prior state preserved).</summary>
    /// <param name="request">The sanitized orchestration request.</param>
    /// <param name="command">The disable payload.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The orchestration outcome (denied, or dispatched).</returns>
    public Task<AgentAdministrationOutcome> DisableAsync(
        AgentAdministrationRequest request,
        DisableAgent command,
        CancellationToken ct)
        => DispatchAsync(request, nameof(DisableAgent), command, ct);

    private async Task<AgentAdministrationOutcome> DispatchAsync<TCommand>(
        AgentAdministrationRequest request,
        string commandType,
        TCommand command,
        CancellationToken ct)
        where TCommand : class
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(command);

        // Authorize — fail closed before any dispatch (AD-12).
        if (!request.IsAgentsAdmin)
        {
            return AgentAdministrationOutcome.Denied();
        }

        var envelope = new CommandEnvelope(
            request.MessageId,
            request.TenantId,
            _agentDomain,
            request.AgentId,
            commandType,
            JsonSerializer.SerializeToUtf8Bytes(command),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            BuildTrustedExtensions(request.ClientSuppliedExtensions));

        await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);

        return AgentAdministrationOutcome.FromDispatch();
    }

    private static Dictionary<string, string> BuildTrustedExtensions(IReadOnlyDictionary<string, string>? clientSupplied)
    {
        var extensions = new Dictionary<string, string>(StringComparer.Ordinal);

        if (clientSupplied is not null)
        {
            foreach ((string key, string value) in clientSupplied)
            {
                if (Array.IndexOf(_reservedExtensionKeys, key) < 0)
                {
                    extensions[key] = value;
                }
            }
        }

        extensions[AgentProviderSelectionOrchestrator.AgentAdminExtensionKey] = "true";
        return extensions;
    }
}
