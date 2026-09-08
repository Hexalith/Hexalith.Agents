using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.Server.Ports;

using Hexalith.EventStore.Contracts.Commands;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Application orchestration for provider-catalog mutations (Story 5.3): create, update, enable, and disable.
/// Each authorizes the actor, builds the command envelope, and dispatches it with the <b>server-populated</b>
/// trusted <c>actor:agentsProviderAdmin</c> extension.
/// </summary>
/// <remarks>
/// Reserved extension keys are server-populated only. Any client-supplied value for them is stripped here; only
/// <c>actor:agentsProviderAdmin</c> is repopulated from the trusted authorization decision.
/// </remarks>
public sealed class ProviderCatalogAdministrationOrchestrator
{
    private static readonly string[] _reservedExtensionKeys =
    [
        ProviderCatalogAggregate.ProviderAdminExtensionKey,
        AgentProviderSelectionOrchestrator.AgentAdminExtensionKey,
        AgentProviderSelectionOrchestrator.ProviderSelectionValidationExtensionKey,
        AgentActivationProviderRevalidation.ApproverPolicyValidationExtensionKey,
    ];

    private readonly IAgentCommandDispatcher _dispatcher;

    /// <summary>Initializes a new instance of the <see cref="ProviderCatalogAdministrationOrchestrator"/> class.</summary>
    /// <param name="dispatcher">The command-dispatch seam.</param>
    public ProviderCatalogAdministrationOrchestrator(IAgentCommandDispatcher dispatcher)
    {
        ArgumentNullException.ThrowIfNull(dispatcher);
        _dispatcher = dispatcher;
    }

    /// <summary>Authorizes and dispatches the create command.</summary>
    /// <param name="request">The sanitized orchestration request.</param>
    /// <param name="command">The safe create payload.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The orchestration outcome (denied, or dispatched).</returns>
    public Task<AgentAdministrationOutcome> CreateAsync(
        ProviderCatalogAdministrationRequest request,
        CreateProviderModelEntry command,
        CancellationToken ct)
        => DispatchAsync(request, nameof(CreateProviderModelEntry), command, ct);

    /// <summary>Authorizes and dispatches the update command.</summary>
    /// <param name="request">The sanitized orchestration request.</param>
    /// <param name="command">The safe update payload.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The orchestration outcome (denied, or dispatched).</returns>
    public Task<AgentAdministrationOutcome> UpdateAsync(
        ProviderCatalogAdministrationRequest request,
        UpdateProviderModelEntry command,
        CancellationToken ct)
        => DispatchAsync(request, nameof(UpdateProviderModelEntry), command, ct);

    /// <summary>Authorizes and dispatches the enable command.</summary>
    /// <param name="request">The sanitized orchestration request.</param>
    /// <param name="command">The enable payload.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The orchestration outcome (denied, or dispatched).</returns>
    public Task<AgentAdministrationOutcome> EnableAsync(
        ProviderCatalogAdministrationRequest request,
        EnableProviderModelEntry command,
        CancellationToken ct)
        => DispatchAsync(request, nameof(EnableProviderModelEntry), command, ct);

    /// <summary>Authorizes and dispatches the disable command.</summary>
    /// <param name="request">The sanitized orchestration request.</param>
    /// <param name="command">The disable payload.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The orchestration outcome (denied, or dispatched).</returns>
    public Task<AgentAdministrationOutcome> DisableAsync(
        ProviderCatalogAdministrationRequest request,
        DisableProviderModelEntry command,
        CancellationToken ct)
        => DispatchAsync(request, nameof(DisableProviderModelEntry), command, ct);

    private async Task<AgentAdministrationOutcome> DispatchAsync<TCommand>(
        ProviderCatalogAdministrationRequest request,
        string commandType,
        TCommand command,
        CancellationToken ct)
        where TCommand : class
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(command);

        if (!request.IsProviderAdmin)
        {
            return AgentAdministrationOutcome.Denied();
        }

        var envelope = new CommandEnvelope(
            request.MessageId,
            request.TenantId,
            ProviderCatalogAggregate.Domain,
            request.TenantId,
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

        extensions[ProviderCatalogAggregate.ProviderAdminExtensionKey] = "true";
        return extensions;
    }
}
