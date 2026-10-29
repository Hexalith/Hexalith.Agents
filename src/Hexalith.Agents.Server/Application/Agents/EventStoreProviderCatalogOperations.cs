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
    IEventStoreGatewayClient? gateway = null,
    IAgentCommandStatusReader? statusReader = null) : IProviderCatalogOperations
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
    private readonly IAgentCommandStatusReader? _statusReader = statusReader
        ?? (gateway is null ? null : new EventStoreAgentCommandStatusReader(_ => gateway));

    /// <inheritdoc />
    [Obsolete("Use GetCommandWriteOutcomeAsync; the legacy operation is rejected.")]
    public ValueTask<AgentOperationResult<AgentSetupWriteStatus>> GetCommandOutcomeAsync(
        string targetTenantId,
        string messageId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => ValueTask.FromResult(AgentOperationResult<AgentSetupWriteStatus>.Failed(
            AgentOperationErrorCode.Rejected, IsCanonicalUlid(options?.CorrelationId) ? options!.CorrelationId : null));

    /// <inheritdoc />
    public async ValueTask<AgentOperationResult<AgentSetupWriteOutcome>> GetCommandWriteOutcomeAsync(
        string targetTenantId,
        string messageId,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => (await GetCommandOutcomeCoreAsync(targetTenantId, messageId, hasSubmissionReceipt: false,
            cancellationToken, options).ConfigureAwait(false)) with
        {
            CorrelationId = IsCanonicalUlid(options?.CorrelationId) ? options!.CorrelationId : null,
        };

    private async ValueTask<AgentOperationResult<AgentSetupWriteOutcome>> GetCommandOutcomeCoreAsync(
        string targetTenantId,
        string messageId,
        bool hasSubmissionReceipt,
        CancellationToken cancellationToken,
        AgentOperationOptions? options = null,
        AgentAdministrationContext? originatingContext = null)
    {
        AgentAdministrationContext context = originatingContext ?? await _contextProvider.GetContextAsync(targetTenantId == ProviderCatalogIdentity.PlatformTenantId, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (string.Equals(targetTenantId, "current", StringComparison.Ordinal))
        {
            targetTenantId = context.TenantId;
        }

        if (string.IsNullOrWhiteSpace(targetTenantId) || string.IsNullOrWhiteSpace(messageId)
            || string.IsNullOrWhiteSpace(context.ActorUserId)
            || !context.HasCurrentAuthority
            || !(context.IsPlatformOperator || context.IsAuthorized)
            || (targetTenantId == ProviderCatalogIdentity.PlatformTenantId && !context.IsPlatformOperator)
            || (targetTenantId != ProviderCatalogIdentity.PlatformTenantId
                && !context.IsPlatformOperator && targetTenantId != context.TenantId))
        {
            return hasSubmissionReceipt
                ? AgentOperationResult<AgentSetupWriteOutcome>.Succeeded(AgentSetupWriteOutcome.Submitted)
                : AgentOperationResult<AgentSetupWriteOutcome>.Failed(AgentOperationErrorCode.NotAuthorized);
        }

        if (options?.CorrelationId is { } suppliedCorrelation && !IsCanonicalUlid(suppliedCorrelation))
        {
            return AgentOperationResult<AgentSetupWriteOutcome>.Failed(AgentOperationErrorCode.ValidationFailed);
        }

        cancellationToken.ThrowIfCancellationRequested();
        if (_statusReader is null)
        {
            return AgentOperationResult<AgentSetupWriteOutcome>.Succeeded(hasSubmissionReceipt
                ? AgentSetupWriteOutcome.Submitted : AgentSetupWriteOutcome.UnableToVerify);
        }

        try
        {
            string domain = targetTenantId == ProviderCatalogIdentity.PlatformTenantId
                ? ProviderCatalogAggregate.Domain : TenantProviderEnablementAggregate.Domain;
            CommandStatusQueryResponse? status = await _statusReader.ReadAsync(
                targetTenantId, domain, messageId, options?.CorrelationId, cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            context.Authority?.ThrowIfNotCurrent();

            // A status from another tenant or message is answered exactly like an absent one, so the lookup
            // cannot reveal that the identifier exists elsewhere.
            if (status is null
                || !string.Equals(status.TenantId, targetTenantId, StringComparison.Ordinal)
                || !string.Equals(status.MessageId, messageId, StringComparison.Ordinal)
                || !string.Equals(status.Domain, domain, StringComparison.Ordinal)
                || (options?.CorrelationId is { } expectedCorrelation
                    && !string.Equals(status.CorrelationId, expectedCorrelation, StringComparison.Ordinal)))
            {
                return AgentOperationResult<AgentSetupWriteOutcome>.Succeeded(hasSubmissionReceipt
                    ? AgentSetupWriteOutcome.Submitted : AgentSetupWriteOutcome.UnableToVerify);
            }

            AgentSetupWriteOutcome outcome = status.IsRejected
                ? AgentSetupWriteOutcome.Rejected
                : (status.StatusCode, status.Status) switch
                {
                    ((int)CommandStatus.Completed, nameof(CommandStatus.Completed)) => status.EventCount switch
                    {
                        0 => AgentSetupWriteOutcome.AlreadyApplied,
                        > 0 => AgentSetupWriteOutcome.AwaitingProjection,
                        _ => AgentSetupWriteOutcome.UnableToVerify,
                    },
                    ((int)CommandStatus.Rejected, nameof(CommandStatus.Rejected)) when status.Retryable is true
                        => AgentSetupWriteOutcome.Submitted,
                    ((int)CommandStatus.Rejected, nameof(CommandStatus.Rejected)) when !string.IsNullOrWhiteSpace(status.FailureReason)
                        => AgentSetupWriteOutcome.Unavailable,
                    ((int)CommandStatus.PublishFailed, nameof(CommandStatus.PublishFailed))
                        => AgentSetupWriteOutcome.AwaitingProjection,
                    ((int)CommandStatus.TimedOut, nameof(CommandStatus.TimedOut))
                        => AgentSetupWriteOutcome.Unavailable,
                    ((int)CommandStatus.Received, nameof(CommandStatus.Received))
                        or ((int)CommandStatus.Processing, nameof(CommandStatus.Processing))
                        or ((int)CommandStatus.EventsStored, nameof(CommandStatus.EventsStored))
                        or ((int)CommandStatus.EventsPublished, nameof(CommandStatus.EventsPublished))
                        => AgentSetupWriteOutcome.Submitted,
                    _ => AgentSetupWriteOutcome.UnableToVerify,
                };
            return AgentOperationResult<AgentSetupWriteOutcome>.Succeeded(outcome);
        }
        catch (AgentCommandAuthorityExpiredException)
        {
            return hasSubmissionReceipt
                ? AgentOperationResult<AgentSetupWriteOutcome>.Succeeded(AgentSetupWriteOutcome.Submitted)
                : AgentOperationResult<AgentSetupWriteOutcome>.Failed(AgentOperationErrorCode.NotAuthorized);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!context.HasCurrentAuthority)
            {
                return hasSubmissionReceipt
                    ? AgentOperationResult<AgentSetupWriteOutcome>.Succeeded(AgentSetupWriteOutcome.Submitted)
                    : AgentOperationResult<AgentSetupWriteOutcome>.Failed(AgentOperationErrorCode.NotAuthorized);
            }
            return AgentOperationResult<AgentSetupWriteOutcome>.Succeeded(AgentSetupWriteOutcome.UnableToVerify);
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
        AgentAdministrationContext context = await _contextProvider.GetContextAsync(true, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.HasCurrentAuthority || !context.IsPlatformOperator || string.IsNullOrWhiteSpace(context.ActorUserId))
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
            cancellationToken.ThrowIfCancellationRequested();
            context.Authority?.ThrowIfNotCurrent();
            if (read.Value?.IsScopedTo(tenantId) == false)
            {
                return AgentOperationResult<TenantProviderEnablementInspectionResult>.Succeeded(
                    new(ProviderCatalogInspectionStatus.Unavailable, null, null), correlationId: options?.CorrelationId);
            }

            ProviderCatalogReadModel? platform = await ReadPlatformModelAsync(cancellationToken, context.Authority).ConfigureAwait(false);
            TenantProviderEntryState? entry = null;
            bool found = read.Value?.State.Entries.TryGetValue(
                ProviderCatalogState.EntryKey(providerId, modelId), out entry) == true;
            if (!await ProviderCatalogReadFreshness.HasHeadAsync(_gateway, tenantId,
                TenantProviderEnablementAggregate.Domain, tenantId,
                read.Value?.LastSequenceNumber ?? 0, cancellationToken, context.Authority).ConfigureAwait(false)
                || !await ProviderCatalogReadFreshness.IsPlatformCurrentAsync(_gateway, platform,
                    providerId, modelId, cancellationToken, context.Authority).ConfigureAwait(false))
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
        catch (AgentCommandAuthorityExpiredException)
        {
            return AgentOperationResult<TenantProviderEnablementInspectionResult>.Succeeded(
                new(ProviderCatalogInspectionStatus.NotAuthorized, null, null), correlationId: options?.CorrelationId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!context.HasCurrentAuthority)
            {
                return AgentOperationResult<TenantProviderEnablementInspectionResult>.Succeeded(
                    new(ProviderCatalogInspectionStatus.NotAuthorized, null, null), correlationId: options?.CorrelationId);
            }
            return AgentOperationResult<TenantProviderEnablementInspectionResult>.Failed(
                AgentCommandDispatchFailure.Map(exception), options?.CorrelationId);
        }
    }

    /// <inheritdoc />
    public ValueTask<AgentOperationResult<TenantProviderCatalogInspectionResult>> ListTenantEntriesAsync(
        bool includeDisabled = false,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
        => ReadTenantAsync((platform, tenant, evaluatedAt) => TenantProviderCatalogViewFactory.CreateList(
            platform, tenant, authorized: true, evaluatedAt, includeDisabled),
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
        return ReadTenantAsync((platform, tenant, evaluatedAt) => TenantProviderCatalogViewFactory.CreateEntry(
            platform, tenant, authorized: true, providerId, modelId, evaluatedAt),
            providerId, modelId, options, cancellationToken);
    }

    /// <inheritdoc />
    public async ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> SetTenantEnablementAsync(
        SetTenantProviderModelEnablement command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        AgentAdministrationContext context = await _contextProvider.GetContextAsync(true, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.HasCurrentAuthority || !context.IsPlatformOperator || string.IsNullOrWhiteSpace(context.ActorUserId))
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.NotAuthorized);
        }

        if (ValidateSuppliedWriteIdentities(options) is { } invalidIdentity)
        {
            return invalidIdentity;
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
                platformAuthority: true, cancellationToken, context).ConfigureAwait(false);
        }

        try
        {
            ProviderCatalogReadModel? platform = await ReadPlatformModelAsync(cancellationToken, context.Authority).ConfigureAwait(false);
            ProviderCatalogEntryView? entry = platform?.Entries.FirstOrDefault(item =>
                item.ProviderId == command.ProviderId && item.ModelId == command.ModelId);
            if (entry is null)
            {
                string entryId = ProviderCatalogIdentity.EntryId(command.ProviderId, command.ModelId);
                bool absent = await ProviderCatalogReadFreshness.HasHeadAsync(_gateway,
                    ProviderCatalogIdentity.PlatformTenantId, ProviderCatalogAggregate.Domain, entryId,
                    platform?.StreamSequences.GetValueOrDefault(entryId) ?? 0,
                    cancellationToken, context.Authority).ConfigureAwait(false);
                return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(
                    absent ? AgentOperationErrorCode.NotFound : AgentOperationErrorCode.Unavailable);
            }

            if (!await ProviderCatalogReadFreshness.IsPlatformCurrentAsync(_gateway, platform,
                command.ProviderId, command.ModelId, cancellationToken, context.Authority).ConfigureAwait(false))
            {
                return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.Unavailable);
            }

            if (entry.Status != ProviderModelStatus.Enabled || entry.DataHandling is not { DataHandlingVersion: >= 1 })
            {
                return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.Blocked);
            }

            context.Authority?.ThrowIfNotCurrent();
            SetTenantProviderModelEnablement trusted = command with { CurrentTerms = entry.DataHandling };
            return await WriteTenantAsync(command.ProviderId, command.ModelId, command.TenantId, options,
                (request, ct) => _catalog.SetTenantEnablementAsync(request, trusted, ct),
                platformAuthority: true, cancellationToken, context).ConfigureAwait(false);
        }
        catch (AgentCommandAuthorityExpiredException)
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.NotAuthorized);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(context.HasCurrentAuthority
                ? AgentOperationErrorCode.Unavailable : AgentOperationErrorCode.NotAuthorized);
        }
    }

    /// <inheritdoc />
    public async ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> DecideDataHandlingAsync(
        DecideProviderDataHandling command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        AgentAdministrationContext context = await _contextProvider.GetContextAsync(false, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.IsAuthorized)
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.NotAuthorized);
        }

        if (ValidateSuppliedWriteIdentities(options) is { } invalidIdentity)
        {
            return invalidIdentity;
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
                platformAuthority: false, cancellationToken, context).ConfigureAwait(false);
        }

        TenantProviderCatalogInspectionResult visible = (await ReadTenantAsync(
            (platform, tenant, evaluatedAt) => TenantProviderCatalogViewFactory.CreateEntry(
                platform, tenant, authorized: true, command.ProviderId, command.ModelId, evaluatedAt),
            command.ProviderId, command.ModelId, options, cancellationToken, context).ConfigureAwait(false)).Value
            ?? new(ProviderCatalogInspectionStatus.Unavailable, []);
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.IsAuthorized || visible.Status == ProviderCatalogInspectionStatus.NotAuthorized)
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.NotAuthorized);
        }
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
            platformAuthority: false, cancellationToken, context).ConfigureAwait(false);
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
    public async ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> CreateEntryAsync(
        CreateProviderModelEntry command,
        AgentOperationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        AgentAdministrationContext context = await _contextProvider.GetContextAsync(true, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.HasCurrentAuthority || !context.IsPlatformOperator || string.IsNullOrWhiteSpace(context.ActorUserId))
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.NotAuthorized);
        }

        if (command.MigratedFrom is not null || command.InitialCapabilityVersion != 1)
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.Blocked);
        }

        return await WriteAsync(
            command.ProviderId,
            command.ModelId,
            options,
            (request, ct) => _catalog.CreateAsync(request, Stamp(command), ct),
            cancellationToken, context).ConfigureAwait(false);
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
        AgentAdministrationContext context = await _contextProvider.GetContextAsync(true, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        string? correlationId = options?.CorrelationId;
        if (!context.HasCurrentAuthority || !context.IsPlatformOperator)
        {
            return AgentOperationResult<ProviderCatalogInspectionResult>.Succeeded(
                ProviderCatalogInspectionResult.NotAuthorized(), correlationId: correlationId);
        }

        try
        {
            ReadModelEntry<ProviderCatalogReadModel> entry = await _readModelStore.GetAsync<ProviderCatalogReadModel>(
                _options.Value.StateStoreName, ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId),
                cancellationToken).ConfigureAwait(false);
            cancellationToken.ThrowIfCancellationRequested();
            context.Authority?.ThrowIfNotCurrent();
            bool knownHeadsCurrent = await ProviderCatalogReadFreshness.IsPlatformCurrentAsync(_gateway, entry.Value,
                providerId, modelId, cancellationToken, context.Authority).ConfigureAwait(false);
            ProviderCatalogInspectionResult result = create(entry.Value, ProviderCatalogIdentity.PlatformTenantId);
            if (!knownHeadsCurrent || providerId is null)
            {
                // A list cannot claim a complete inventory from checks of its projected stream names alone.
                result = result with
                {
                    Status = ProviderCatalogInspectionStatus.Success,
                    Entries = knownHeadsCurrent && providerId is null ? result.Entries : [],
                    Freshness = AgentSetupFreshness.Stale,
                    TruthState = AgentSetupTruthState.AuthoritativePending,
                    ProjectedCommandMessageIds = null,
                };
            }
            else if (result.Status == ProviderCatalogInspectionStatus.EntryNotFound && expectedCapabilityVersion is null)
            {
                result = result with
                {
                    Freshness = AgentSetupFreshness.Current,
                    TruthState = AgentSetupTruthState.ProjectionConfirmed,
                    ProjectedCommandMessageIds = null,
                };
            }
            context.Authority?.ThrowIfNotCurrent();
            return AgentOperationResult<ProviderCatalogInspectionResult>.Succeeded(result, correlationId: correlationId);
        }
        catch (AgentCommandAuthorityExpiredException)
        {
            return AgentOperationResult<ProviderCatalogInspectionResult>.Succeeded(
                ProviderCatalogInspectionResult.NotAuthorized(), correlationId: correlationId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return context.HasCurrentAuthority
                ? AgentOperationResult<ProviderCatalogInspectionResult>.Failed(AgentCommandDispatchFailure.Map(exception), correlationId)
                : AgentOperationResult<ProviderCatalogInspectionResult>.Succeeded(ProviderCatalogInspectionResult.NotAuthorized(), correlationId: correlationId);
        }
    }

    private async ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> WriteAsync(
        string providerId,
        string modelId,
        AgentOperationOptions? options,
        Func<ProviderCatalogAdministrationRequest, CancellationToken, Task<AgentAdministrationOutcome>> dispatch,
        CancellationToken cancellationToken,
        AgentAdministrationContext? originatingContext = null)
    {
        AgentAdministrationContext context = originatingContext ?? await _contextProvider.GetContextAsync(true, cancellationToken).ConfigureAwait(false);
        cancellationToken.ThrowIfCancellationRequested();
        if (!context.HasCurrentAuthority || !context.IsPlatformOperator || string.IsNullOrWhiteSpace(context.ActorUserId))
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(
                AgentOperationErrorCode.NotAuthorized,
                IsCanonicalUlid(options?.CorrelationId) ? options!.CorrelationId : null);
        }

        string correlationId = options?.CorrelationId ?? _identityFactory.NewCorrelationId();
        if (!IsCanonicalUlid(correlationId))
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.ValidationFailed);
        }

        string messageId = options?.IdempotencyKey ?? _identityFactory.NewMessageId();
        if (!IsCanonicalUlid(messageId))
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.ValidationFailed, correlationId);
        }

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
                        IsPlatformOperator: true) { Authority = context.Authority },
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
            providerId, modelId, messageId, correlationId, cancellationToken, context).ConfigureAwait(false);
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
        Func<ProviderCatalogReadModel?, TenantProviderEnablementReadModel?, DateTimeOffset, TenantProviderCatalogInspectionResult> create,
        string? providerId,
        string? modelId,
        AgentOperationOptions? options,
        CancellationToken ct,
        AgentAdministrationContext? originatingContext = null)
    {
        AgentAdministrationContext context = originatingContext ?? await _contextProvider.GetContextAsync(false, ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
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
            ct.ThrowIfCancellationRequested();
            context.Authority?.ThrowIfNotCurrent();
            ReadModelEntry<TenantProviderEnablementReadModel> tenant = await _readModelStore.GetAsync<TenantProviderEnablementReadModel>(
                _options.Value.StateStoreName,
                TenantProviderEnablementReadModelAddresses.Detail(context.TenantId), ct).ConfigureAwait(false);
            ct.ThrowIfCancellationRequested();
            context.Authority?.ThrowIfNotCurrent();
            if (tenant.Value?.IsScopedTo(context.TenantId) == false)
            {
                return AgentOperationResult<TenantProviderCatalogInspectionResult>.Succeeded(
                    new(ProviderCatalogInspectionStatus.Unavailable, []), correlationId: options?.CorrelationId);
            }

            if (!await ProviderCatalogReadFreshness.IsTenantCurrentAsync(_gateway,
                context.TenantId, platform.Value, tenant.Value,
                providerId, modelId, ct, context.Authority).ConfigureAwait(false))
            {
                DateTimeOffset evaluatedAt = _clock.GetUtcNow();
                context.Authority?.ThrowIfNotCurrent(evaluatedAt);
                TenantProviderCatalogInspectionResult pending = create(platform.Value, tenant.Value, evaluatedAt) with
                {
                    Status = ProviderCatalogInspectionStatus.Success,
                    Entries = [],
                    Freshness = AgentSetupFreshness.Stale,
                    TruthState = AgentSetupTruthState.AuthoritativePending,
                    ProjectedCommandMessageIds = null,
                };
                context.Authority?.ThrowIfNotCurrent();
                return AgentOperationResult<TenantProviderCatalogInspectionResult>.Succeeded(
                    pending, correlationId: options?.CorrelationId);
            }
            // Freshness confirmed that a missing tenant read model has no enablement stream.
            DateTimeOffset completedAt = _clock.GetUtcNow();
            context.Authority?.ThrowIfNotCurrent(completedAt);
            TenantProviderCatalogInspectionResult result = create(platform.Value, tenant.Value ?? new TenantProviderEnablementReadModel(), completedAt);
            context.Authority?.ThrowIfNotCurrent();
            return AgentOperationResult<TenantProviderCatalogInspectionResult>.Succeeded(result, correlationId: options?.CorrelationId);
        }
        catch (AgentCommandAuthorityExpiredException)
        {
            return AgentOperationResult<TenantProviderCatalogInspectionResult>.Succeeded(
                new(ProviderCatalogInspectionStatus.NotAuthorized, []), correlationId: options?.CorrelationId);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            ct.ThrowIfCancellationRequested();
            if (!context.HasCurrentAuthority)
            {
                return AgentOperationResult<TenantProviderCatalogInspectionResult>.Succeeded(
                    new(ProviderCatalogInspectionStatus.NotAuthorized, []), correlationId: options?.CorrelationId);
            }
            return AgentOperationResult<TenantProviderCatalogInspectionResult>.Failed(
                AgentCommandDispatchFailure.Map(exception), options?.CorrelationId);
        }
    }

    private async Task<ProviderCatalogReadModel?> ReadPlatformModelAsync(CancellationToken ct, AgentAuthorityValidity? authority = null)
    {
        ReadModelEntry<ProviderCatalogReadModel> entry = await _readModelStore.GetAsync<ProviderCatalogReadModel>(
            _options.Value.StateStoreName,
            ProviderCatalogReadModelAddresses.Detail(ProviderCatalogIdentity.PlatformTenantId), ct).ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();
        authority?.ThrowIfNotCurrent();
        return entry.Value;
    }

    private async ValueTask<AgentOperationResult<ProviderCatalogCommandAcceptance>> WriteTenantAsync(
        string providerId,
        string modelId,
        string targetTenantId,
        AgentOperationOptions? options,
        Func<ProviderCatalogAdministrationRequest, CancellationToken, Task<AgentAdministrationOutcome>> dispatch,
        bool platformAuthority,
        CancellationToken ct,
        AgentAdministrationContext context)
    {
        ct.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(context.ActorUserId)
            || !context.HasCurrentAuthority
            || (platformAuthority ? !context.IsPlatformOperator : !context.IsAuthorized || targetTenantId != context.TenantId)
            || string.IsNullOrWhiteSpace(targetTenantId)
            || string.Equals(targetTenantId, ProviderCatalogIdentity.PlatformTenantId, StringComparison.Ordinal))
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.NotAuthorized,
                IsCanonicalUlid(options?.CorrelationId) ? options!.CorrelationId : null);
        }

        string correlationId = options?.CorrelationId ?? _identityFactory.NewCorrelationId();
        if (!IsCanonicalUlid(correlationId))
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.ValidationFailed);
        }

        string messageId = options?.IdempotencyKey ?? _identityFactory.NewMessageId();
        if (!IsCanonicalUlid(messageId))
        {
            return AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.ValidationFailed, correlationId);
        }
        try
        {
            AgentAdministrationOutcome outcome = await dispatch(new ProviderCatalogAdministrationRequest(
                messageId, correlationId, targetTenantId, context.ActorUserId,
                IsProviderAdmin: context.IsAgentsAdmin,
                IsPlatformOperator: context.IsPlatformOperator) { Authority = context.Authority }, ct).ConfigureAwait(false);
            return await ResolveWriteAsync(outcome, targetTenantId, providerId, modelId,
                messageId, correlationId, ct, context).ConfigureAwait(false);
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
        CancellationToken ct,
        AgentAdministrationContext context)
    {
        ct.ThrowIfCancellationRequested();
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
            AgentOperationResult<AgentSetupWriteOutcome> observed = await GetCommandOutcomeCoreAsync(
                targetTenantId, messageId, hasSubmissionReceipt: true, ct, new AgentOperationOptions(correlationId), context).ConfigureAwait(false);
            status = observed.IsSuccess && observed.Value is { } value
                ? value == AgentSetupWriteOutcome.UnableToVerify ? AgentSetupWriteStatus.Submitted : (AgentSetupWriteStatus)((int)value - 1)
                : AgentSetupWriteStatus.UnableToVerify;
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

    private static AgentOperationResult<ProviderCatalogCommandAcceptance>? ValidateSuppliedWriteIdentities(AgentOperationOptions? options)
        => options is not null
            && (options.CorrelationId is not null && !IsCanonicalUlid(options.CorrelationId)
                || options.IdempotencyKey is not null && !IsCanonicalUlid(options.IdempotencyKey))
                ? AgentOperationResult<ProviderCatalogCommandAcceptance>.Failed(AgentOperationErrorCode.ValidationFailed,
                    IsCanonicalUlid(options.CorrelationId) ? options.CorrelationId : null)
                : null;

    private static bool IsCanonicalUlid(string? value)
        => !string.IsNullOrWhiteSpace(value)
            && NUlid.Ulid.TryParse(value, out NUlid.Ulid parsed)
            && string.Equals(parsed.ToString(), value, StringComparison.Ordinal);

    private static AgentSetupWriteStatus? ReceiptEffect(SubmitCommandResponse receipt)
    {
        if (receipt.ResultPayload is not { ValueKind: JsonValueKind.Object } payload
            || !payload.TryGetProperty("effect", out JsonElement effect))
        {
            return null;
        }

        if (effect.ValueKind != JsonValueKind.String)
        {
            return AgentSetupWriteStatus.UnableToVerify;
        }

        return effect.GetString() switch
        {
            "Applied" => AgentSetupWriteStatus.AwaitingProjection,
            "AlreadyApplied" => AgentSetupWriteStatus.AlreadyApplied,
            _ => null,
        };
    }
}
