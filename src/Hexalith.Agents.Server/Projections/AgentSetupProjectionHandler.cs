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
/// Persists the aggregate-owned Agent setup read model (Story 5.2 AC2, AC3): the single authoritative projected
/// truth the setup queries, the public client, and FrontComposer all read, carrying the configuration version, the
/// projection version, and the projected-at freshness stamp.
/// </summary>
/// <remarks>
/// The fold is idempotent on the stored sequence checkpoint, so a duplicate or replayed delivery converges on the
/// same persisted end state; a gapped or unknown-event delivery is reported retryable rather than folded, so the
/// read model never advertises a version it did not actually build.
/// </remarks>
public sealed class AgentSetupProjectionHandler(
    IReadModelStore readModelStore,
    IReadModelBatchStore batchStore,
    IOptions<AgentSetupReadModelOptions> options) :
    IAsyncDomainProjectionRebuildHandler,
    IDeclaresProjectionReadModelSlots
{
    /// <summary>Gets the read-model slots this projection declares as its canonical writer.</summary>
    public static IReadOnlyList<ProjectionReadModelSlotDeclaration> ProjectionReadModelSlots { get; } =
    [
        new(
            AgentSetupReadModelAddresses.Domain,
            AgentSetupReadModelAddresses.ProjectionName,
            AgentSetupReadModelAddresses.DetailSlot,
            ProjectionReadModelSlotKind.AggregateOwned,
            declaresCanonicalWriter: true),
    ];

    /// <inheritdoc />
    public string Domain => AgentSetupReadModelAddresses.Domain;

    /// <inheritdoc />
    public string ProjectionType => AgentSetupReadModelAddresses.ProjectionName;

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
        string key = AgentSetupReadModelAddresses.Detail(request.TenantId, request.AggregateId);
        ReadModelEntry<AgentSetupReadModel> current = await readModelStore
            .GetAsync<AgentSetupReadModel>(storeName, key, cancellationToken)
            .ConfigureAwait(false);

        if (AgentSetupProjectionFold.GetDeliveryFailureReason(
                request.Events,
                current.Value?.LastSequenceNumber ?? 0) is { } deliveryFailure)
        {
            return DomainProjectionHandlerResult.Retryable(deliveryFailure);
        }

        AgentSetupReadModel next = AgentSetupProjectionFold.Fold(request, current.Value);
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
        if (AgentSetupProjectionFold.GetDeliveryFailureReason(request.Events, 0) is { } deliveryFailure)
        {
            throw new InvalidOperationException(deliveryFailure);
        }

        return Task.FromResult(new DomainProjectionRebuildPlan(
            StoreName,
            [
                ReadModelBatchOperation.Write(
                    AgentSetupReadModelAddresses.Detail(request.TenantId, request.AggregateId),
                    AgentSetupProjectionFold.Fold(request, current: null),
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
        if (!string.Equals(request.Domain, AgentSetupReadModelAddresses.Domain, StringComparison.Ordinal))
        {
            throw new ArgumentException("Projection request domain is not supported.", nameof(request));
        }

        _ = AgentSetupReadModelAddresses.Detail(request.TenantId, request.AggregateId);
    }
}
