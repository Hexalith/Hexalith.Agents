using Hexalith.Agents.EventStore;
using Hexalith.EventStore.Client.Routing;
using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.DomainService;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Immutable exact-tenant catalog for local Agents protocol fixtures; it cannot activate a runtime host.</summary>
internal sealed class FixtureAgentRouteCatalog : IRouteCatalogAuthority
{
    private readonly CommittedRouteCatalogSnapshot _snapshot;

    /// <summary>Builds validated local routes for registered commands and an explicitly selected fixture tenant.</summary>
    internal FixtureAgentRouteCatalog(string tenantId = "tenant-a")
    {
        var services = new ServiceCollection();
        services.AddAgentsEventStore("agents", new FixtureTrustedCommandProofReader());
        using ServiceProvider provider = services.BuildServiceProvider();
        Adapters = provider.GetServices<IIdempotencyIntentAdapter>().Select(adapter => new FixtureDeclaredAgentIntentAdapter(adapter)).ToArray();
        RouteContractDeclaration[] declarations = Adapters.Select(adapter => adapter.Declaration.Route).ToArray();
        RouteCatalogEntry[] routes = declarations.Select(route => new RouteCatalogEntry(route.RouteId, 1,
            RouteCatalogCodec.ComputeDeclarationDigest(route), "agents", "process", "v1", route.CredentialKind,
            route.Operation, false, route.Domain == "provider-catalog" ? "system" : tenantId)).ToArray();
        IdempotencyCatalogEntry[] admission = Adapters.Select(adapter =>
        {
            IdempotencyCatalogEntry entry = DevelopmentIdempotencyBindings.Bind(adapter.Declaration, 1) with
            {
                ActiveDigestKeyGeneration = "v1", ReaderDigestKeyGenerations = ["v1"], SecretMapGeneration = "v1",
                KeyMap = [new("v1", "v1")],
            };
            return entry with { ContentDigest = RouteCatalogCodec.ComputeIdempotencyEntryDigest(entry) };
        }).ToArray();
        RouteCatalogEnvelope envelope = RouteCatalogCodec.Seal(new(2, 1, "Development", false,
            declarations, routes, admission, string.Empty, string.Empty));
        _snapshot = new(RouteCatalogCodec.Encode(envelope), RouteCatalogCodec.ComputeRootDigest(envelope), 1);
    }

    /// <summary>Gets test-only declared wrappers around the real registered adapters.</summary>
    internal IReadOnlyList<FixtureDeclaredAgentIntentAdapter> Adapters { get; }

    /// <inheritdoc />
    public Task<RouteCatalogDispatchLease> AcquireAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(new RouteCatalogDispatchLease(_snapshot, static () => Task.CompletedTask));
    }

    /// <inheritdoc />
    public Task<RouteCatalogGenerationReadback> ReadbackAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        RouteCatalogEnvelope envelope = _snapshot.Envelope;
        return Task.FromResult(new RouteCatalogGenerationReadback(_snapshot.Epoch, envelope.Generation,
            _snapshot.RootDigest, envelope.RouteFacetDigest, envelope.IdempotencyFacetDigest, envelope.Profile, [], true));
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

    private static RouteCatalogUnavailableException Immutable() => new("Agents protocol fixtures accept only their immutable local catalog.");
}
