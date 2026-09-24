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
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.TenantProviderEnablement;

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
    IAgentCommandIdentityFactory identityFactory,
    TimeProvider? clock = null) : IProviderCatalogOperations
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

    private readonly TimeProvider _clock = clock ?? TimeProvider.System;

    /// <inheritdoc />
    public async ValueTask<AgentOperationResult<TenantProviderEnablementInspectionResult>> GetTenantEnablementAsync(
        string tenantId,
        string providerId,
        string modelId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        AgentAdministrationContext context = _contextProvider.GetContext();
        if (!context.IsPlatformOperator || string.IsNullOrWhiteSpace(context.ActorUserId))
        {
            return AgentOperationResult<TenantProviderEnablementInspectionResult>.Succeeded(
                new(ProviderCatalogInspectionStatus.NotAuthorized, null, null), correlationId: options?.CorrelationId);
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(tenantId);
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        if (string.Equals(tenantId, ProviderCatalogIdentity.PlatformTenantId, StringComparison.Ordinal))
        {
            return AgentOperationResult<TenantProviderEnablementInspectionResult>.Succeeded(
                new(ProviderCatalogInspectionStatus.NotAuthorized, null, null), correlationId: options?.CorrelationId);
        }

        try
        {
            ReadModelEntry<TenantProviderEnablementReadModel> read = await _readModelStore
                .GetAsync<TenantProviderEnablementReadModel>(_options.Value.StateStoreName,
                    TenantProviderEnablementReadModelAddresses.Detail(tenantId), cancellationToken).ConfigureAwait(false);
            TenantProviderEntryState? entry = null;
            bool found = read.Value?.State.Entries.TryGetValue(
                ProviderCatalogState.EntryKey(providerId, modelId), out entry) == true;
            return AgentOperationResult<TenantProviderEnablementInspectionResult>.Succeeded(
                new(found ? ProviderCatalogInspectionStatus.Success : ProviderCatalogInspectionStatus.EntryNotFound,
                    found ? entry!.Enabled : null, read.Value?.State.Revision, read.Value?.ProjectionVersion,
                    found ? entry!.LastEnablementMessageId : null),
                correlationId: options?.CorrelationId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return AgentOperationResult<TenantProviderEnablementInspectionResult>.Failed(
                AgentCommandDispatchFailure.Map(exception), options?.CorrelationId);
        }
    }

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<TenantProviderCatalogInspectionResult>> ListTenantEntriesAsync(
        bool includeDisabled = false,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => ReadTenantAsync((platform, tenant) => TenantProviderCatalogViewFactory.CreateList(
            platform, tenant, authorized: true, _clock.GetUtcNow(), includeDisabled), options, cancellationToken);

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<TenantProviderCatalogInspectionResult>> GetTenantEntryAsync(
        string providerId,
        string modelId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providerId);
        ArgumentException.ThrowIfNullOrWhiteSpace(modelId);
        return ReadTenantAsync((platform, tenant) => TenantProviderCatalogViewFactory.CreateEntry(
            platform, tenant, authorized: true, providerId, modelId, _clock.GetUtcNow()), options, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> SetTenantEnablementAsync(
        SetTenantProviderModelEnablement command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        AgentAdministrationContext context = _contextProvider.GetContext();
        if (!context.IsPlatformOperator || string.IsNullOrWhiteSpace(context.ActorUserId))
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.NotAuthorized);
        }

        ProviderCatalogReadModel? platform = await ReadPlatformModelAsync(cancellationToken).ConfigureAwait(false);
        ProviderCatalogEntryView? entry = platform?.Entries.FirstOrDefault(item =>
            item.ProviderId == command.ProviderId && item.ModelId == command.ModelId);
        if (entry is null)
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.NotFound);
        }

        if (command.Enabled && entry.DataHandling is not { DataHandlingVersion: >= 1 })
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.Blocked);
        }

        SetTenantProviderModelEnablement trusted = command with { CurrentTerms = entry.DataHandling };
        return await WriteTenantAsync(
            command.ProviderId, command.ModelId, command.TenantId, options,
            (request, ct) => _catalog.SetTenantEnablementAsync(request, trusted, ct),
            platformAuthority: true, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> DecideDataHandlingAsync(
        DecideProviderDataHandling command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        AgentAdministrationContext context = _contextProvider.GetContext();
        if (!context.IsAuthorized)
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.NotAuthorized);
        }

        if (command.ConfirmedTerms is not { DataHandlingVersion: >= 1 } submitted
            || submitted.DataHandlingVersion != command.DataHandlingVersion
            || ProviderDataHandlingPolicy.Validate(submitted, current: null, allowHistoricalVersion: true) is not null
            || string.IsNullOrWhiteSpace(command.Justification))
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.Stale);
        }

        // A retained key may already own a committed target outcome. Let EventStore admission and the
        // coordinated actor reconcile that identity before comparing it with today's platform terms.
        if (options?.IdempotencyKey is { Length: > 0 })
        {
            DecideProviderDataHandling retried = command with { DecidedAt = _clock.GetUtcNow() };
            return await WriteTenantAsync(command.ProviderId, command.ModelId, context.TenantId, options,
                (request, ct) => _catalog.DecideDataHandlingAsync(request, retried, ct),
                platformAuthority: false, cancellationToken).ConfigureAwait(false);
        }

        TenantProviderCatalogInspectionResult visible = (await GetTenantEntryAsync(
            command.ProviderId, command.ModelId, options, cancellationToken).ConfigureAwait(false)).Value
            ?? new(ProviderCatalogInspectionStatus.Unavailable, []);
        if (visible.Status != ProviderCatalogInspectionStatus.Success || visible.Entries.Count != 1)
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(
                visible.Status == ProviderCatalogInspectionStatus.EntryNotFound
                    ? AgentOperationErrorCode.NotFound : AgentOperationErrorCode.Unavailable);
        }

        ProviderDataHandlingRecord? terms = visible.Entries[0].DataHandling;
        if (terms is null || ProviderDataHandlingPolicy.Validate(terms, current: null, allowHistoricalVersion: true) is not null
            || !ProviderDataHandlingPolicy.SameSnapshot(terms, submitted))
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.Stale);
        }

        DecideProviderDataHandling trusted = command with { DecidedAt = _clock.GetUtcNow() };
        return await WriteTenantAsync(
            command.ProviderId, command.ModelId, context.TenantId, options,
            (request, ct) => _catalog.DecideDataHandlingAsync(request, trusted, ct),
            platformAuthority: false, cancellationToken).ConfigureAwait(false);
    }

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
            (request, ct) => _catalog.CreateAsync(request, Stamp(command), ct),
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
            (request, ct) => _catalog.UpdateAsync(request, Stamp(command), ct),
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
        if (!context.IsPlatformOperator)
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
                    ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId),
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
            create(entry.Value, ProviderCatalogIdentity.PlatformTenantId),
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

        if (!context.IsPlatformOperator || string.IsNullOrWhiteSpace(context.ActorUserId))
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
                        IsProviderAdmin: true,
                        IsPlatformOperator: true),
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

    private CreateProviderModelEntry Stamp(CreateProviderModelEntry command)
        => command.DataHandling is null
            ? command
            : command with { DataHandling = command.DataHandling with { EffectiveAt = _clock.GetUtcNow() } };

    private UpdateProviderModelEntry Stamp(UpdateProviderModelEntry command)
        => command.DataHandling is null
            ? command
            : command with { DataHandling = command.DataHandling with { EffectiveAt = _clock.GetUtcNow() } };

    private async ValueTask<AgentOperationResult<TenantProviderCatalogInspectionResult>> ReadTenantAsync(
        Func<ProviderCatalogReadModel?, TenantProviderEnablementReadModel?, TenantProviderCatalogInspectionResult> create,
        AgentOperationOptions? options,
        CancellationToken ct)
    {
        AgentAdministrationContext context = _contextProvider.GetContext();
        if (!context.IsAuthorized || string.Equals(context.TenantId, ProviderCatalogIdentity.PlatformTenantId, StringComparison.Ordinal))
        {
            return AgentOperationResult<TenantProviderCatalogInspectionResult>.Succeeded(
                new(ProviderCatalogInspectionStatus.NotAuthorized, []), correlationId: options?.CorrelationId);
        }

        try
        {
            ReadModelEntry<ProviderCatalogReadModel> platform = await _readModelStore.GetAsync<ProviderCatalogReadModel>(
                _options.Value.StateStoreName,
                ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId), ct).ConfigureAwait(false);
            ReadModelEntry<TenantProviderEnablementReadModel> tenant = await _readModelStore.GetAsync<TenantProviderEnablementReadModel>(
                _options.Value.StateStoreName,
                TenantProviderEnablementReadModelAddresses.Detail(context.TenantId), ct).ConfigureAwait(false);
            return AgentOperationResult<TenantProviderCatalogInspectionResult>.Succeeded(
                create(platform.Value, tenant.Value), correlationId: options?.CorrelationId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return AgentOperationResult<TenantProviderCatalogInspectionResult>.Failed(
                AgentCommandDispatchFailure.Map(exception), options?.CorrelationId);
        }
    }

    private async Task<ProviderCatalogReadModel?> ReadPlatformModelAsync(CancellationToken ct)
    {
        try
        {
            ReadModelEntry<ProviderCatalogReadModel> entry = await _readModelStore.GetAsync<ProviderCatalogReadModel>(
                _options.Value.StateStoreName,
                ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId), ct).ConfigureAwait(false);
            return entry.Value;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return null;
        }
    }

    private async ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> WriteTenantAsync(
        string providerId,
        string modelId,
        string targetTenantId,
        AgentOperationOptions? options,
        Func<ProviderCatalogAdministrationRequest, CancellationToken, Task<AgentAdministrationOutcome>> dispatch,
        bool platformAuthority,
        CancellationToken ct)
    {
        AgentAdministrationContext context = _contextProvider.GetContext();
        string correlationId = options?.CorrelationId is { Length: > 0 } supplied
            ? supplied : _identityFactory.NewCorrelationId();
        if (string.IsNullOrWhiteSpace(context.ActorUserId)
            || (platformAuthority ? !context.IsPlatformOperator : !context.IsAuthorized || targetTenantId != context.TenantId)
            || string.IsNullOrWhiteSpace(targetTenantId)
            || string.Equals(targetTenantId, ProviderCatalogIdentity.PlatformTenantId, StringComparison.Ordinal))
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.NotAuthorized, correlationId);
        }

        string messageId = options?.IdempotencyKey is { Length: > 0 } key ? key : _identityFactory.NewMessageId();
        try
        {
            AgentAdministrationOutcome outcome = await dispatch(new ProviderCatalogAdministrationRequest(
                messageId, correlationId, targetTenantId, context.ActorUserId,
                IsProviderAdmin: context.IsAgentsAdmin,
                IsPlatformOperator: context.IsPlatformOperator), ct).ConfigureAwait(false);
            return outcome switch
            {
                { Authorized: false } => AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.NotAuthorized, correlationId),
                { Dispatched: false } => AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.Unavailable, correlationId),
                _ => AgentOperationResult<ProviderCatalogCommandAcceptance>.Succeeded(
                    new(providerId, modelId, messageId, correlationId, AgentSetupTruthState.Submitted),
                    correlationId: correlationId),
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentCommandDispatchFailure.Map(exception), correlationId);
        }
    }
}
