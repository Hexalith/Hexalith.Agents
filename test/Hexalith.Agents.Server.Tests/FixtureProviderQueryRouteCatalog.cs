using Hexalith.Agents.Contracts.ProviderCatalog.Queries;
using Hexalith.Agents.Server.Application.Queries;
using Hexalith.Agents.Server.Projections;
using Hexalith.EventStore.Contracts.Authorization;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Immutable local query enrollment for endpoint prerequisites; it supplies no accepted host provenance.</summary>
internal sealed class FixtureProviderQueryRouteCatalog : IRouteCatalogAuthority
{
    /// <summary>Gets the exact local generation checked by the real SDK receiver guard.</summary>
    internal CommittedRouteCatalogSnapshot Snapshot { get; } = CreateSnapshot();

    /// <inheritdoc />
    public Task<RouteCatalogDispatchLease> AcquireAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new RouteCatalogDispatchLease(Snapshot, static () => Task.CompletedTask));
    }

    /// <inheritdoc />
    public Task<RouteCatalogGenerationReadback> ReadbackAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RouteCatalogEnvelope envelope = Snapshot.Envelope;
        return Task.FromResult(new RouteCatalogGenerationReadback(Snapshot.Epoch, envelope.Generation,
            Snapshot.RootDigest, envelope.RouteFacetDigest, envelope.IdempotencyFacetDigest, envelope.Profile, [], true));
    }

    /// <inheritdoc />
    public Task PrepareAsync(RouteCatalogActivation candidate, CancellationToken cancellationToken = default) => Task.FromException(Immutable());
    /// <inheritdoc />
    public Task<RouteCatalogHostAttestation> ReadyAsync(string attemptId, CancellationToken cancellationToken = default) => Task.FromException<RouteCatalogHostAttestation>(Immutable());
    /// <inheritdoc />
    public Task CommitAsync(string attemptId, CancellationToken cancellationToken = default) => Task.FromException(Immutable());
    /// <inheritdoc />
    public Task RollbackAsync(RouteCatalogRollbackRequest request, CancellationToken cancellationToken = default) => Task.FromException(Immutable());
    /// <inheritdoc />
    public Task ReconcileAsync(RouteCatalogQuiescenceRequest request, CancellationToken cancellationToken = default) => Task.FromException(Immutable());

    private static RouteCatalogUnavailableException Immutable() => new("Local provider query enrollment cannot activate a runtime host.");

    private static CommittedRouteCatalogSnapshot CreateSnapshot()
    {
        RouteContractDeclaration[] declarations =
        [
            Declaration("fixture-provider-list", ListProviderCatalogEntriesQuery.QueryType, typeof(ListProviderCatalogEntriesQueryHandler)),
            Declaration("fixture-provider-detail", GetProviderCatalogEntryQuery.QueryType, typeof(GetProviderCatalogEntryQueryHandler)),
        ];
        RouteCatalogEntry[] routes = declarations.Select(route => new RouteCatalogEntry(route.RouteId, 1,
            RouteCatalogCodec.ComputeDeclarationDigest(route), "agents", "query", "v1", "workload",
            EventStoreOperationIdentifiers.DomainServiceQuery, false, "system")).ToArray();
        RouteCatalogEnvelope envelope = RouteCatalogCodec.Seal(new(2, 1, "Development", false,
            declarations, routes, [], string.Empty, string.Empty));
        return new(RouteCatalogCodec.Encode(envelope), RouteCatalogCodec.ComputeRootDigest(envelope), 1);
    }

    private static RouteContractDeclaration Declaration(string routeId, string queryType, Type handler)
        => new(routeId, "query", ProviderCatalogReadModelAddresses.Domain, queryType, null,
            "v1", null, "workload", EventStoreOperationIdentifiers.DomainServiceQuery, null, [],
            new(handler.FullName!, "exact", [queryType], "domain-service", "computed", []));
}
