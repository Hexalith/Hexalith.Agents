using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.Agents.Contracts.ProviderCatalog;
using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;
using Hexalith.Agents.ProviderCatalog;

using Microsoft.Extensions.Options;

namespace Hexalith.Agents.Server.Projections;

/// <summary>
/// Persists the aggregate-owned provider-catalog read model (Story 5.3 AC2, AC3): the single authoritative
/// projected truth the catalog queries, the public client, and FrontComposer all read.
/// </summary>
public sealed class ProviderCatalogProjectionHandler(
    IReadModelStore readModelStore,
    IReadModelBatchStore batchStore,
    IOptions<ProviderCatalogReadModelOptions> options) :
    IAsyncDomainProjectionRebuildHandler,
    IDeclaresProjectionReadModelSlots
{
    /// <summary>Gets the read-model slots this projection declares as its canonical writer.</summary>
    public static IReadOnlyList<ProjectionReadModelSlotDeclaration> ProjectionReadModelSlots { get; } =
    [
        new(
            ProviderCatalogReadModelAddresses.Domain,
            ProviderCatalogReadModelAddresses.ProjectionName,
            ProviderCatalogReadModelAddresses.DetailSlot,
            ProjectionReadModelSlotKind.Shared,
            declaresCanonicalWriter: true),
    ];

    /// <inheritdoc />
    public string Domain => ProviderCatalogReadModelAddresses.Domain;

    /// <inheritdoc />
    public string ProjectionType => ProviderCatalogReadModelAddresses.ProjectionName;

    /// <inheritdoc />
    public DomainProjectionRebuildSemantics RebuildSemantics => DomainProjectionRebuildSemantics.FullReplay;

    /// <inheritdoc />
    public async Task<DomainProjectionHandlerResult> ProjectAsync(
        ProjectionRequest request,
        string dispatchId,
        CancellationToken cancellationToken)
    {
        Validate(request, dispatchId);
        cancellationToken.ThrowIfCancellationRequested();
        if (request.Events.Length == 0)
        {
            return DomainProjectionHandlerResult.Completed();
        }

        string storeName = StoreName;
        string key = ProviderCatalogReadModelAddresses.Detail(request.TenantId);
        ReadModelEntry<ProviderCatalogReadModel> current = await readModelStore
            .GetAsync<ProviderCatalogReadModel>(storeName, key, cancellationToken)
            .ConfigureAwait(false);

        if (ProviderCatalogProjectionFold.GetDeliveryFailureReason(
                request.Events,
                ProviderCatalogProjectionFold.Checkpoint(current.Value, request.TenantId, request.AggregateId)) is { } deliveryFailure)
        {
            return DomainProjectionHandlerResult.Retryable(deliveryFailure);
        }

        ProviderCatalogReadModel next = ProviderCatalogProjectionFold.Fold(request, current.Value);
        if (current.Value is not null
            && next.StreamSequences.GetValueOrDefault(request.AggregateId)
                == ProviderCatalogProjectionFold.Checkpoint(current.Value, request.TenantId, request.AggregateId))
        {
            return DomainProjectionHandlerResult.AlreadyCompleted();
        }

        var batch = new ReadModelBatch(
            new ReadModelBatchScope(storeName, request.TenantId, Domain, request.AggregateId, ProjectionType, dispatchId),
            [
                ReadModelBatchOperation.Write(
                    key,
                    next,
                    current.ETag is { Length: > 0 } etag
                        ? ReadModelBatchConcurrency.Match(etag)
                        : ReadModelBatchConcurrency.CreateOnly),
            ]);

        ReadModelBatchResult result = await batchStore.ExecuteAsync(batch, cancellationToken).ConfigureAwait(false);
        return ReadModelBatchProjectionResultMapper.Map(result);
    }

    /// <inheritdoc />
    public async Task<DomainProjectionRebuildPlan> PrepareRebuildAsync(
        ProjectionRequest request,
        string operationId,
        CancellationToken cancellationToken)
    {
        Validate(request, operationId);
        cancellationToken.ThrowIfCancellationRequested();
        if (ProviderCatalogProjectionFold.GetDeliveryFailureReason(request.Events, 0) is { } deliveryFailure)
        {
            throw new InvalidOperationException(deliveryFailure);
        }

        string key = ProviderCatalogReadModelAddresses.Detail(request.TenantId);
        ReadModelEntry<ProviderCatalogReadModel> stored = await readModelStore
            .GetAsync<ProviderCatalogReadModel>(StoreName, key, cancellationToken)
            .ConfigureAwait(false);
        ProviderCatalogReadModel? current = stored.Value;
        bool legacyTenantStream = !string.Equals(request.TenantId, ProviderCatalogIdentity.PlatformTenantId, StringComparison.Ordinal)
            && string.Equals(request.AggregateId, request.TenantId, StringComparison.Ordinal);
        ProviderCatalogReadModel? unaffected = current is null || legacyTenantStream ? null : new ProviderCatalogReadModel
        {
            CatalogId = current.CatalogId,
            TenantId = current.TenantId,
            Entries = [.. current.Entries.Where(entry =>
                !string.Equals(ProviderCatalogIdentity.EntryId(entry.ProviderId, entry.ModelId), request.AggregateId, StringComparison.Ordinal))],
            StreamSequences = current.StreamSequences
                .Where(pair => !string.Equals(pair.Key, request.AggregateId, StringComparison.Ordinal))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            StreamCommandMessageIds = current.StreamCommandMessageIds
                .Where(pair => !string.Equals(pair.Key, request.AggregateId, StringComparison.Ordinal))
                .ToDictionary(pair => pair.Key, pair => pair.Value, StringComparer.Ordinal),
            ProjectedAt = current.ProjectedAt,
        };

        return new DomainProjectionRebuildPlan(
            StoreName,
            [
                ReadModelBatchOperation.Write(
                    key,
                    ProviderCatalogProjectionFold.Fold(request, unaffected),
                    stored.ETag is { Length: > 0 } etag
                        ? ReadModelBatchConcurrency.Match(etag)
                        : ReadModelBatchConcurrency.CreateOnly),
            ]);
    }

    private string StoreName
    {
        get
        {
            string value = options.Value.StateStoreName;
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            return value;
        }
    }

    private static void Validate(ProjectionRequest request, string operationId)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationId);
        if (!string.Equals(request.Domain, ProviderCatalogReadModelAddresses.Domain, StringComparison.Ordinal))
        {
            throw new ArgumentException("Projection request domain is not supported.", nameof(request));
        }

        _ = ProviderCatalogReadModelAddresses.Detail(request.TenantId);
    }
}
