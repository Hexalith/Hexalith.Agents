using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

using Hexalith.Agents.Contracts.ProviderCatalog.Events;
using Hexalith.Agents.Contracts.ProviderCatalog.Events.Rejections;
using Hexalith.Agents.Contracts.Serialization;
using Hexalith.Agents.ProviderCatalog;
using Hexalith.Agents.TenantProviderEnablement;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.Options;

namespace Hexalith.Agents.Server.Projections;

/// <summary>Full replay and incremental projection for tenant provider governance.</summary>
public sealed class TenantProviderEnablementProjectionHandler(
    IReadModelStore readModelStore,
    IReadModelBatchStore batchStore,
    IOptions<ProviderCatalogReadModelOptions> options) :
    IAsyncDomainProjectionRebuildHandler,
    IDeclaresProjectionReadModelSlots
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new UnknownFallbackEnumConverterFactory(), new JsonStringEnumConverter() },
    };

    /// <summary>Gets the canonical read-model slot declaration.</summary>
    public static IReadOnlyList<ProjectionReadModelSlotDeclaration> ProjectionReadModelSlots { get; } =
    [
        new(TenantProviderEnablementAggregate.Domain,
            TenantProviderEnablementReadModelAddresses.ProjectionName,
            TenantProviderEnablementReadModelAddresses.DetailSlot,
            ProjectionReadModelSlotKind.AggregateOwned,
            declaresCanonicalWriter: true),
    ];

    /// <inheritdoc />
    public string Domain => TenantProviderEnablementAggregate.Domain;

    /// <inheritdoc />
    public string ProjectionType => TenantProviderEnablementReadModelAddresses.ProjectionName;

    /// <inheritdoc />
    public DomainProjectionRebuildSemantics RebuildSemantics => DomainProjectionRebuildSemantics.FullReplay;

    /// <inheritdoc />
    public async Task<DomainProjectionHandlerResult> ProjectAsync(ProjectionRequest request, string dispatchId, CancellationToken cancellationToken)
    {
        Validate(request, dispatchId);
        string key = TenantProviderEnablementReadModelAddresses.Detail(request.TenantId);
        string store = options.Value.StateStoreName;
        ReadModelEntry<TenantProviderEnablementReadModel> current = await readModelStore.GetAsync<TenantProviderEnablementReadModel>(store, key, cancellationToken).ConfigureAwait(false);
        if (TryFold(request, current.Value, out TenantProviderEnablementReadModel? next) is { } failure)
        {
            return DomainProjectionHandlerResult.Retryable(failure);
        }

        if (next!.LastSequenceNumber == current.Value?.LastSequenceNumber)
        {
            return DomainProjectionHandlerResult.AlreadyCompleted();
        }

        var batch = new ReadModelBatch(
            new ReadModelBatchScope(store, request.TenantId, Domain, request.AggregateId, ProjectionType, dispatchId),
            [ReadModelBatchOperation.Write(key, next, current.ETag is { Length: > 0 } tag
                ? ReadModelBatchConcurrency.Match(tag) : ReadModelBatchConcurrency.CreateOnly)]);
        return ReadModelBatchProjectionResultMapper.Map(await batchStore.ExecuteAsync(batch, cancellationToken).ConfigureAwait(false));
    }

    /// <inheritdoc />
    public Task<DomainProjectionRebuildPlan> PrepareRebuildAsync(ProjectionRequest request, string operationId, CancellationToken cancellationToken)
    {
        Validate(request, operationId);
        cancellationToken.ThrowIfCancellationRequested();
        if (TryFold(request, current: null, out TenantProviderEnablementReadModel? next) is { } failure)
        {
            throw new InvalidOperationException(failure);
        }

        return Task.FromResult(new DomainProjectionRebuildPlan(options.Value.StateStoreName,
            [ReadModelBatchOperation.Write(TenantProviderEnablementReadModelAddresses.Detail(request.TenantId), next!, ReadModelBatchConcurrency.LastWrite)]));
    }

    private static string? TryFold(ProjectionRequest request, TenantProviderEnablementReadModel? current, out TenantProviderEnablementReadModel? next)
    {
        long sequence = current?.LastSequenceNumber ?? 0;
        TenantProviderEnablementState state = current?.State ?? new TenantProviderEnablementState { TenantId = request.TenantId };
        DateTimeOffset? projectedAt = current?.ProjectedAt;
        foreach (ProjectionEventDto item in request.Events.Where(item => item.SequenceNumber > sequence).OrderBy(item => item.SequenceNumber))
        {
            if (item.SequenceNumber != sequence + 1)
            {
                next = null;
                return "delivery-sequence-gap";
            }

            object? payload = Deserialize(item);
            switch (payload)
            {
                case TenantProviderModelEnablementSet e:
                    state.Apply(e);
                    state.Entries[ProviderCatalogState.EntryKey(e.ProviderId, e.ModelId)].LastEnablementMessageId = item.MessageId;
                    break;
                case ProviderDataHandlingDecided e:
                    state.Apply(e);
                    if (state.Entries.TryGetValue(ProviderCatalogState.EntryKey(e.ProviderId, e.ModelId), out TenantProviderEntryState? decided))
                    {
                        decided.LastDecisionMessageId = item.MessageId;
                    }
                    break;
                case TenantProviderGovernanceRejected e: state.Apply(e); break;
                default:
                    next = null;
                    return "unresolved-event-type";
            }

            sequence = item.SequenceNumber;
            DateTimeOffset timestamp = item.Timestamp.ToUniversalTime();
            projectedAt = projectedAt is { } known && known >= timestamp ? known : timestamp;
        }

        next = new TenantProviderEnablementReadModel
        {
            State = state,
            LastSequenceNumber = sequence,
            ProjectedAt = projectedAt,
            ProjectionVersion = sequence == 0 ? null : sequence.ToString(CultureInfo.InvariantCulture),
        };
        return null;
    }

    private static object? Deserialize(ProjectionEventDto item)
    {
        Type? type = ProviderCatalogEventTypeResolver.Resolve(item.EventTypeName);
        if (type is null || item.Payload is null or { Length: 0 })
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize(item.Payload, type, _jsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static void Validate(ProjectionRequest request, string operationId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        if (!string.Equals(request.Domain, TenantProviderEnablementAggregate.Domain, StringComparison.Ordinal)
            || !string.Equals(request.AggregateId, request.TenantId, StringComparison.Ordinal))
        {
            throw new ArgumentException("Invalid tenant provider projection scope.", nameof(request));
        }
    }
}
