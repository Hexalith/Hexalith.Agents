using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.TenantProviderEnablement;
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
        TenantProviderEnablementAggregate.PlatformOperatorExtensionKey,
        TenantProviderEnablementAggregate.TenantAdministratorExtensionKey,
        AgentProviderSelectionOrchestrator.AgentAdminExtensionKey,
        AgentProviderSelectionOrchestrator.ProviderSelectionValidationExtensionKey,
        AgentActivationProviderRevalidation.ApproverPolicyValidationExtensionKey,
        AgentSetupTrustedExtensions.ActivationExpectedConfigurationVersion,
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

    /// <summary>Dispatches a platform-owned tenant enablement decision.</summary>
    public Task<AgentAdministrationOutcome> SetTenantEnablementAsync(
        ProviderCatalogAdministrationRequest request,
        SetTenantProviderModelEnablement command,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(command);
        return DispatchTenantAsync(request, command.TenantId, nameof(SetTenantProviderModelEnablement), command,
            TenantProviderEnablementAggregate.PlatformOperatorExtensionKey, request.IsPlatformOperator, ct);
    }

    /// <summary>Dispatches a tenant administrator's terms decision.</summary>
    public Task<AgentAdministrationOutcome> DecideDataHandlingAsync(
        ProviderCatalogAdministrationRequest request,
        DecideProviderDataHandling command,
        CancellationToken ct)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(command);
        return DispatchTenantAsync(request, request.TenantId, nameof(DecideProviderDataHandling), command,
            TenantProviderEnablementAggregate.TenantAdministratorExtensionKey, request.IsProviderAdmin, ct);
    }

    private async Task<AgentAdministrationOutcome> DispatchAsync<TCommand>(
        ProviderCatalogAdministrationRequest request,
        string commandType,
        TCommand command,
        CancellationToken ct)
        where TCommand : class
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(command);

        if (!request.IsPlatformOperator)
        {
            return AgentAdministrationOutcome.Denied();
        }

        (string providerId, string modelId) = command switch
        {
            CreateProviderModelEntry value => (value.ProviderId, value.ModelId),
            UpdateProviderModelEntry value => (value.ProviderId, value.ModelId),
            EnableProviderModelEntry value => (value.ProviderId, value.ModelId),
            DisableProviderModelEntry value => (value.ProviderId, value.ModelId),
            _ => throw new InvalidOperationException("Unsupported provider catalog command."),
        };

        var envelope = new CommandEnvelope(
            request.MessageId,
            ProviderCatalogIdentity.PlatformTenantId,
            ProviderCatalogAggregate.Domain,
            ProviderCatalogIdentity.EntryId(providerId, modelId),
            commandType,
            JsonSerializer.SerializeToUtf8Bytes(command),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            BuildTrustedExtensions(request.ClientSuppliedExtensions));

        SubmitCommandResponse receipt = await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);

        return AgentAdministrationOutcome.FromDispatch(receipt);
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

    private async Task<AgentAdministrationOutcome> DispatchTenantAsync<TCommand>(
        ProviderCatalogAdministrationRequest request,
        string tenantId,
        string commandType,
        TCommand command,
        string authorityKey,
        bool authorized,
        CancellationToken ct)
        where TCommand : class
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(command);
        if (!authorized || string.IsNullOrWhiteSpace(tenantId)
            || string.Equals(tenantId, ProviderCatalogIdentity.PlatformTenantId, StringComparison.Ordinal))
        {
            return AgentAdministrationOutcome.Denied();
        }

        Dictionary<string, string> extensions = BuildTrustedExtensions(request.ClientSuppliedExtensions);
        extensions.Remove(ProviderCatalogAggregate.ProviderAdminExtensionKey);
        extensions[authorityKey] = "true";
        var envelope = new CommandEnvelope(
            request.MessageId,
            tenantId,
            TenantProviderEnablementAggregate.Domain,
            tenantId,
            commandType,
            JsonSerializer.SerializeToUtf8Bytes(command),
            request.CorrelationId,
            CausationId: null,
            request.ActorUserId,
            extensions);
        SubmitCommandResponse receipt = await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);
        return AgentAdministrationOutcome.FromDispatch(receipt);
    }
}
