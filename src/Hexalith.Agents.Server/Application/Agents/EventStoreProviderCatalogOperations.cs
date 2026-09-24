using System;
using System.Text.Json;
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
using Hexalith.EventStore.Client.Gateway;
using Hexalith.EventStore.Contracts.Commands;
using Hexalith.EventStore.Contracts.Streams;

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
    TimeProvider? clock = null,
    IEventStoreGatewayClient? gateway = null) : IProviderCatalogOperations
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
    private readonly IEventStoreGatewayClient? _gateway = gateway;

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<AgentSetupWriteStatus>> GetCommandOutcomeAsync(
        string targetTenantId,
        string messageId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => GetCommandOutcomeCoreAsync(targetTenantId, messageId, hasSubmissionReceipt: false,
            cancellationToken);

    private async ValueTask<AgentOperationResult<AgentSetupWriteStatus>> GetCommandOutcomeCoreAsync(
        string targetTenantId,
        string messageId,
        bool hasSubmissionReceipt,
        CancellationToken cancellationToken)
    {
        AgentAdministrationContext context = _contextProvider.GetContext();
        if (string.Equals(targetTenantId, "current", StringComparison.Ordinal))
        {
            targetTenantId = context.TenantId;
        }

        if (string.IsNullOrWhiteSpace(targetTenantId) || string.IsNullOrWhiteSpace(messageId)
            || string.IsNullOrWhiteSpace(context.ActorUserId)
            || !(context.IsPlatformOperator || context.IsAuthorized)
            || (targetTenantId == ProviderCatalogIdentity.PlatformTenantId && !context.IsPlatformOperator)
            || (targetTenantId != ProviderCatalogIdentity.PlatformTenantId
                && !context.IsPlatformOperator && targetTenantId != context.TenantId))
        {
            return AgentOperationResult<AgentSetupWriteStatus>.Failed(AgentOperationErrorCode.NotAuthorized);
        }

        if (_gateway is null)
        {
            return AgentOperationResult<AgentSetupWriteStatus>.Succeeded(hasSubmissionReceipt
                ? AgentSetupWriteStatus.Submitted : AgentSetupWriteStatus.UnableToVerify);
        }

        try
        {
            CommandStatusQueryResponse? status = await _gateway.GetCommandStatusAsync(messageId, cancellationToken)
                .ConfigureAwait(false);
            if (status is null)
            {
                return AgentOperationResult<AgentSetupWriteStatus>.Succeeded(hasSubmissionReceipt
                    ? AgentSetupWriteStatus.Submitted : AgentSetupWriteStatus.UnableToVerify);
            }

            if (!string.Equals(status.TenantId, targetTenantId, StringComparison.Ordinal)
                || !string.Equals(status.MessageId, messageId, StringComparison.Ordinal))
            {
                return AgentOperationResult<AgentSetupWriteStatus>.Failed(AgentOperationErrorCode.NotAuthorized);
            }

            AgentSetupWriteStatus outcome = status.IsRejected
                ? AgentSetupWriteStatus.Rejected
                : (status.StatusCode, status.Status) switch
                {
                    ((int)CommandStatus.Completed, nameof(CommandStatus.Completed)) => status.EventCount switch
                    {
                        0 => AgentSetupWriteStatus.AlreadyApplied,
                        > 0 => AgentSetupWriteStatus.AwaitingProjection,
                        _ => AgentSetupWriteStatus.UnableToVerify,
                    },
                    ((int)CommandStatus.Rejected, nameof(CommandStatus.Rejected)) when status.Retryable is true
                        => AgentSetupWriteStatus.Submitted,
                    ((int)CommandStatus.Rejected, nameof(CommandStatus.Rejected)) when !string.IsNullOrWhiteSpace(status.FailureReason)
                        => AgentSetupWriteStatus.Unavailable,
                    ((int)CommandStatus.PublishFailed, nameof(CommandStatus.PublishFailed))
                        or ((int)CommandStatus.TimedOut, nameof(CommandStatus.TimedOut))
                        => AgentSetupWriteStatus.Unavailable,
                    ((int)CommandStatus.Received, nameof(CommandStatus.Received))
                        or ((int)CommandStatus.Processing, nameof(CommandStatus.Processing))
                        or ((int)CommandStatus.EventsStored, nameof(CommandStatus.EventsStored))
                        or ((int)CommandStatus.EventsPublished, nameof(CommandStatus.EventsPublished))
                        => AgentSetupWriteStatus.Submitted,
                    _ => AgentSetupWriteStatus.UnableToVerify,
                };
            return AgentOperationResult<AgentSetupWriteStatus>.Succeeded(outcome);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return AgentOperationResult<AgentSetupWriteStatus>.Succeeded(AgentSetupWriteStatus.UnableToVerify);
        }
    }

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
            ProviderCatalogReadModel? platform = await ReadPlatformModelAsync(cancellationToken).ConfigureAwait(false);
            TenantProviderEntryState? entry = null;
            bool found = read.Value?.State.Entries.TryGetValue(
                ProviderCatalogState.EntryKey(providerId, modelId), out entry) == true;
            if (!await ProviderCatalogReadFreshness.HasHeadAsync(_gateway, tenantId,
                TenantProviderEnablementAggregate.Domain, tenantId,
                read.Value?.LastSequenceNumber ?? 0, cancellationToken).ConfigureAwait(false)
                || !await ProviderCatalogReadFreshness.IsPlatformCurrentAsync(_gateway, platform,
                    providerId, modelId, cancellationToken).ConfigureAwait(false))
            {
                return AgentOperationResult<TenantProviderEnablementInspectionResult>.Succeeded(
                    new(ProviderCatalogInspectionStatus.Success, null, null,
                        Freshness: AgentSetupFreshness.Stale,
                        TruthState: AgentSetupTruthState.AuthoritativePending),
                    correlationId: options?.CorrelationId);
            }
            return AgentOperationResult<TenantProviderEnablementInspectionResult>.Succeeded(
                new(found ? ProviderCatalogInspectionStatus.Success : ProviderCatalogInspectionStatus.EntryNotFound,
                    found ? entry!.Enabled : null, read.Value?.State.Revision, read.Value?.ProjectionVersion,
                    found ? entry!.LastEnablementMessageId : null,
                    read.Value?.ProjectedCommandMessageIds,
                    AgentSetupFreshness.Current,
                    AgentSetupTruthState.ProjectionConfirmed),
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
            platform, tenant, authorized: true, _clock.GetUtcNow(), includeDisabled),
            providerId: null, modelId: null, options, cancellationToken);

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
            platform, tenant, authorized: true, providerId, modelId, _clock.GetUtcNow()),
            providerId, modelId, options, cancellationToken);
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

        if (command.MigratedFrom is not null)
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.Blocked);
        }

        if (string.IsNullOrWhiteSpace(command.ProviderId) || string.IsNullOrWhiteSpace(command.ModelId))
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.ValidationFailed);
        }

        if (!command.Enabled)
        {
            // Revocation needs no platform terms or projection. The tenant aggregate still checks
            // the caller's expected revision and the write path still enforces platform authority.
            return await WriteTenantAsync(command.ProviderId, command.ModelId, command.TenantId,
                options, (request, ct) => _catalog.SetTenantEnablementAsync(
                    request, command with { CurrentTerms = null }, ct),
                platformAuthority: true, cancellationToken).ConfigureAwait(false);
        }

        ProviderCatalogReadModel? platform;
        try
        {
            platform = await ReadPlatformModelAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.Unavailable);
        }
        ProviderCatalogEntryView? entry = platform?.Entries.FirstOrDefault(item =>
            item.ProviderId == command.ProviderId && item.ModelId == command.ModelId);
        if (entry is null)
        {
            bool absent = await ProviderCatalogReadFreshness.HasHeadAsync(_gateway,
                ProviderCatalogIdentity.PlatformTenantId, ProviderCatalogAggregate.Domain,
                ProviderCatalogIdentity.EntryId(command.ProviderId, command.ModelId), 0,
                cancellationToken).ConfigureAwait(false);
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(
                absent ? AgentOperationErrorCode.NotFound : AgentOperationErrorCode.Unavailable);
        }

        if (!await ProviderCatalogReadFreshness.IsPlatformCurrentAsync(_gateway, platform,
            command.ProviderId, command.ModelId, cancellationToken).ConfigureAwait(false))
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.Unavailable);
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
            providerId: null, modelId: null, cancellationToken);

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
            providerId, modelId, cancellationToken, expectedCapabilityVersion);
    }

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> CreateEntryAsync(
        CreateProviderModelEntry command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        AgentAdministrationContext context = _contextProvider.GetContext();
        if (!context.IsPlatformOperator || string.IsNullOrWhiteSpace(context.ActorUserId))
        {
            return ValueTask.FromResult(AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.NotAuthorized));
        }

        if (command.MigratedFrom is not null || command.InitialCapabilityVersion != 1)
        {
            return ValueTask.FromResult(AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.Blocked));
        }

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
        string? providerId,
        string? modelId,
        CancellationToken cancellationToken,
        int? expectedCapabilityVersion = null)
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

        ProviderCatalogInspectionResult result = create(entry.Value, ProviderCatalogIdentity.PlatformTenantId);
        bool knownHeadsCurrent = await ProviderCatalogReadFreshness.IsPlatformCurrentAsync(_gateway, entry.Value,
            providerId, modelId, cancellationToken).ConfigureAwait(false);
        if (!knownHeadsCurrent || providerId is null)
        {
            result = result with
            {
                Status = ProviderCatalogInspectionStatus.Success,
                // A list has no authoritative global stream inventory. Safe projected rows may be shown,
                // but the list cannot claim completeness or current truth from named stream checks alone.
                Entries = knownHeadsCurrent && providerId is null ? result.Entries : [],
                Freshness = AgentSetupFreshness.Stale,
                TruthState = AgentSetupTruthState.AuthoritativePending,
                ProjectedCommandMessageIds = null,
            };
        }
        else if (result.Status == ProviderCatalogInspectionStatus.EntryNotFound
            && expectedCapabilityVersion is null)
        {
            result = result with
            {
                Freshness = AgentSetupFreshness.Current,
                TruthState = AgentSetupTruthState.ProjectionConfirmed,
                ProjectedCommandMessageIds = null,
            };
        }

        return AgentOperationResult<ProviderCatalogInspectionResult>.Succeeded(
            result,
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

        return await ResolveWriteAsync(outcome, ProviderCatalogIdentity.PlatformTenantId,
            providerId, modelId, messageId, correlationId, cancellationToken).ConfigureAwait(false);
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
        string? providerId,
        string? modelId,
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
            if (!await ProviderCatalogReadFreshness.IsTenantCurrentAsync(_gateway,
                context.TenantId, platform.Value, tenant.Value,
                providerId, modelId, ct).ConfigureAwait(false))
            {
                TenantProviderCatalogInspectionResult pending = create(platform.Value, tenant.Value) with
                {
                    Status = ProviderCatalogInspectionStatus.Success,
                    Entries = [],
                    Freshness = AgentSetupFreshness.Stale,
                    TruthState = AgentSetupTruthState.AuthoritativePending,
                    ProjectedCommandMessageIds = null,
                };
                return AgentOperationResult<TenantProviderCatalogInspectionResult>.Succeeded(
                    pending, correlationId: options?.CorrelationId);
            }
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
        ReadModelEntry<ProviderCatalogReadModel> entry = await _readModelStore.GetAsync<ProviderCatalogReadModel>(
            _options.Value.StateStoreName,
            ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId), ct).ConfigureAwait(false);
        return entry.Value;
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
            return await ResolveWriteAsync(outcome, targetTenantId, providerId, modelId,
                messageId, correlationId, ct).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentCommandDispatchFailure.Map(exception), correlationId);
        }
    }

    private async ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> ResolveWriteAsync(
        AgentAdministrationOutcome outcome,
        string targetTenantId,
        string providerId,
        string modelId,
        string messageId,
        string correlationId,
        CancellationToken ct)
    {
        if (!outcome.Authorized)
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.NotAuthorized, correlationId);
        }

        if (!outcome.Dispatched)
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.Unavailable, correlationId);
        }

        SubmitCommandResponse? receipt = outcome.Receipt;
        if (receipt is null || !string.Equals(receipt.MessageId, messageId, StringComparison.Ordinal)
            || !string.Equals(receipt.CorrelationId, correlationId, StringComparison.Ordinal))
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.UnableToVerify, correlationId);
        }

        AgentSetupWriteStatus? receiptEffect = ReceiptEffect(receipt);
        AgentSetupWriteStatus status;
        if (receiptEffect is { } effect)
        {
            status = effect;
        }
        else
        {
            AgentOperationResult<AgentSetupWriteStatus> observed = await GetCommandOutcomeCoreAsync(
                targetTenantId, messageId, hasSubmissionReceipt: true, ct).ConfigureAwait(false);
            status = observed.IsSuccess && observed.Value is { } value
                ? value : AgentSetupWriteStatus.UnableToVerify;
        }
        return status switch
        {
            AgentSetupWriteStatus.Rejected => AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.Rejected, correlationId),
            AgentSetupWriteStatus.Unavailable => AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.Unavailable, correlationId),
            AgentSetupWriteStatus.UnableToVerify => AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.UnableToVerify, correlationId),
            AgentSetupWriteStatus.NotAuthorized => AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.NotAuthorized, correlationId),
            _ => AgentOperationResult<ProviderCatalogCommandAcceptance>.Succeeded(
                new(providerId, modelId, messageId, correlationId,
                    status == AgentSetupWriteStatus.AlreadyApplied
                        ? AgentSetupTruthState.ProjectionConfirmed
                        : status == AgentSetupWriteStatus.AwaitingProjection
                            ? AgentSetupTruthState.AuthoritativePending
                            : AgentSetupTruthState.Submitted),
                correlationId: correlationId),
        };
    }

    private static AgentSetupWriteStatus? ReceiptEffect(SubmitCommandResponse receipt)
    {
        if (receipt.ResultPayload is not { ValueKind: JsonValueKind.Object } payload
            || !payload.TryGetProperty("effect", out JsonElement effect))
        {
            return null;
        }

        return effect.GetString() switch
        {
            "Applied" => AgentSetupWriteStatus.AwaitingProjection,
            "AlreadyApplied" => AgentSetupWriteStatus.AlreadyApplied,
            _ => null,
        };
    }
}
