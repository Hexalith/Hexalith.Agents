using System;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Client;
using Hexalith.Agents.Contracts.Agent;
using Hexalith.Agents.Contracts.Operations;
using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.Agents.Contracts.ProviderCatalog.Commands;
using Hexalith.Agents.Server.Ports;
using Hexalith.Agents.Server.Projections;

using Hexalith.EventStore.Client.Projections;

using Microsoft.Extensions.Options;

namespace Hexalith.Agents.Server.Application.Agents;

/// <summary>
/// Live public provider-catalog surface over the EventStore command path and the projected catalog read model
/// (Story 5.3). Writes go through authorize-then-dispatch; reads answer from the single authoritative catalog
/// read model so the API, the public client, and FrontComposer always see identical truth.
/// </summary>
public sealed class EventStoreProviderCatalogOperations(
    IAgentAdministrationContextProvider contextProvider,
    ProviderCatalogAdministrationOrchestrator catalog,
    IReadModelStore readModelStore,
    IOptions<ProviderCatalogReadModelOptions> options,
    IAgentCommandIdentityFactory identityFactory) : IProviderCatalogOperations
{
    private readonly IAgentAdministrationContextProvider _contextProvider = contextProvider
        ?? throw new ArgumentNullException(nameof(contextProvider));

    private readonly ProviderCatalogAdministrationOrchestrator _catalog = catalog
        ?? throw new ArgumentNullException(nameof(catalog));

    private readonly IReadModelStore _readModelStore = readModelStore
        ?? throw new ArgumentNullException(nameof(readModelStore));

    private readonly IOptions<ProviderCatalogReadModelOptions> _options = options
        ?? throw new ArgumentNullException(nameof(options));

    private readonly IAgentCommandIdentityFactory _identityFactory = identityFactory
        ?? throw new ArgumentNullException(nameof(identityFactory));

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<ProviderCatalogInspectionResult>> ListEntriesAsync(
        bool includeDisabled,
        string? expectedProjectionVersion = null,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => ReadAsync(
            options,
            (model, tenantId) => ProviderCatalogViewFactory.CreateList(
                model,
                tenantId,
                includeDisabled,
                expectedProjectionVersion,
                isProviderAdmin: true),
            cancellationToken);

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<ProviderCatalogInspectionResult>> GetEntryAsync(
        string providerId,
        string modelId,
        int? expectedCapabilityVersion = null,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        return ReadAsync(
            options,
            (model, tenantId) => ProviderCatalogViewFactory.CreateEntry(
                model,
                tenantId,
                providerId,
                modelId,
                expectedCapabilityVersion,
                isProviderAdmin: true),
            cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> CreateEntryAsync(
        CreateProviderModelEntry command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return WriteAsync(
            command.ProviderId,
            command.ModelId,
            options,
            (request, ct) => _catalog.CreateAsync(request, command, ct),
            cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> UpdateEntryAsync(
        UpdateProviderModelEntry command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return WriteAsync(
            command.ProviderId,
            command.ModelId,
            options,
            (request, ct) => _catalog.UpdateAsync(request, command, ct),
            cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> EnableEntryAsync(
        EnableProviderModelEntry command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return WriteAsync(
            command.ProviderId,
            command.ModelId,
            options,
            (request, ct) => _catalog.EnableAsync(request, command, ct),
            cancellationToken);
    }

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> DisableEntryAsync(
        DisableProviderModelEntry command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return WriteAsync(
            command.ProviderId,
            command.ModelId,
            options,
            (request, ct) => _catalog.DisableAsync(request, command, ct),
            cancellationToken);
    }

    private async ValueTask<AgentOperationResult<ProviderCatalogInspectionResult>> ReadAsync(
        AgentOperationOptions? options,
        Func<ProviderCatalogReadModel?, string, ProviderCatalogInspectionResult> create,
        CancellationToken cancellationToken)
    {
        AgentAdministrationContext context = _contextProvider.GetContext();
        string? correlationId = options?.CorrelationId;
        if (!context.IsAuthorized)
        {
            return AgentOperationResult<ProviderCatalogInspectionResult>.Succeeded(
                ProviderCatalogInspectionResult.NotAuthorized(),
                correlationId: correlationId);
        }

        ReadModelEntry<ProviderCatalogReadModel> entry;
        try
        {
            entry = await _readModelStore
                .GetAsync<ProviderCatalogReadModel>(
                    _options.Value.StateStoreName,
                    ProviderCatalogReadModelAddresses.Detail(context.TenantId),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return AgentOperationResult<ProviderCatalogInspectionResult>.Failed(
                AgentCommandDispatchFailure.Map(exception),
                correlationId);
        }

        return AgentOperationResult<ProviderCatalogInspectionResult>.Succeeded(
            create(entry.Value, context.TenantId),
            correlationId: correlationId);
    }

    private async ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> WriteAsync(
        string providerId,
        string modelId,
        AgentOperationOptions? options,
        Func<ProviderCatalogAdministrationRequest, CancellationToken, Task<AgentAdministrationOutcome>> dispatch,
        CancellationToken cancellationToken)
    {
        AgentAdministrationContext context = _contextProvider.GetContext();
        string correlationId = options?.CorrelationId is { Length: > 0 } supplied
            ? supplied
            : _identityFactory.NewCorrelationId();

        if (!context.IsAuthorized)
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(
                AgentOperationErrorCode.NotAuthorized,
                correlationId);
        }

        string messageId = options?.IdempotencyKey is { Length: > 0 } key ? key : _identityFactory.NewMessageId();

        AgentAdministrationOutcome outcome;
        try
        {
            outcome = await dispatch(
                    new ProviderCatalogAdministrationRequest(
                        messageId,
                        correlationId,
                        context.TenantId,
                        context.ActorUserId,
                        context.IsAgentsAdmin),
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(
                AgentCommandDispatchFailure.Map(exception),
                correlationId);
        }

        return outcome switch
        {
            { Authorized: false } => AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.NotAuthorized, correlationId),
            { Dispatched: false } => AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.Unavailable, correlationId),
            _ => AgentOperationResult<ProviderCatalogCommandAcceptance>.Succeeded(
                new ProviderCatalogCommandAcceptance(
                    providerId,
                    modelId,
                    messageId,
                    correlationId,
                    AgentSetupTruthState.Submitted),
                correlationId: correlationId),
        };
    }
}
