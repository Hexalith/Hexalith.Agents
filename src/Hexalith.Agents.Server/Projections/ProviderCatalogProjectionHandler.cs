using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Hexalith.EventStore.Client.Projections;
using Hexalith.EventStore.Contracts.Projections;
using Hexalith.EventStore.DomainService;

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
            ProjectionReadModelSlotKind.AggregateOwned,
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
                current.Value?.LastSequenceNumber ?? 0) is { } deliveryFailure)
        {
            return DomainProjectionHandlerResult.Retryable(deliveryFailure);
        }

        ProviderCatalogReadModel next = ProviderCatalogProjectionFold.Fold(request, current.Value);
        if (current.Value is not null && next.LastSequenceNumber == current.Value.LastSequenceNumber)
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
    public Task<DomainProjectionRebuildPlan> PrepareRebuildAsync(
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

        return Task.FromResult(new DomainProjectionRebuildPlan(
            StoreName,
            [
                ReadModelBatchOperation.Write(
                    ProviderCatalogReadModelAddresses.Detail(request.TenantId),
                    ProviderCatalogProjectionFold.Fold(request, current: null),
                    ReadModelBatchConcurrency.LastWrite),
            ]));
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
