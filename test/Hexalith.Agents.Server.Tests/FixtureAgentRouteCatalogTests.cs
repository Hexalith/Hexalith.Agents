using Hexalith.EventStore.Contracts.Routing;
using Hexalith.EventStore.ServiceDefaults.Routing;
using Shouldly;

namespace Hexalith.Agents.Server.Tests;

/// <summary>Proves the SDK compatibility fixture grants only its explicit local routes and refuses activation.</summary>
public sealed class FixtureAgentRouteCatalogTests
{
    /// <summary>Validated finite routes reject foreign tenant, domain, discriminator and route kind before execution.</summary>
    [Fact]
    public async Task LocalCatalogHasExactScopeAndCannotActivateRuntimeRoutes()
    {
        var authority = new FixtureAgentRouteCatalog();
        await using RouteCatalogDispatchLease lease = await authority.AcquireAsync(CancellationToken.None);
        CommittedRouteCatalogSnapshot snapshot = lease.Snapshot;
        RouteCatalogValidator.Validate(snapshot.Envelope);
        snapshot.Envelope.Routes.Count.ShouldBe(20);
        snapshot.Envelope.Routes.ShouldAllBe(route => route.ResolverTenantPattern == "tenant-a" || route.ResolverTenantPattern == "system");
        snapshot.Resolve("command", "tenant-a", "agent", "ActivateAgent").Declaration.MessageType.ShouldBe("ActivateAgent");
        snapshot.Resolve("command", "system", "provider-catalog", "UpdateProviderModelEntry").Declaration.Domain.ShouldBe("provider-catalog");
        Should.Throw<RouteCatalogUnavailableException>(() => snapshot.Resolve("command", "tenant-b", "agent", "ActivateAgent"));
        Should.Throw<RouteCatalogUnavailableException>(() => snapshot.Resolve("command", "tenant-a", "provider-catalog", "UpdateProviderModelEntry"));
        Should.Throw<RouteCatalogUnavailableException>(() => snapshot.Resolve("command", "tenant-a", "other-domain", "ActivateAgent"));
        Should.Throw<RouteCatalogUnavailableException>(() => snapshot.Resolve("command", "tenant-a", "agent", "UnregisteredCommand"));
        Should.Throw<RouteCatalogUnavailableException>(() => snapshot.Resolve("query", "tenant-a", "agent", "ActivateAgent"));
        await Should.ThrowAsync<RouteCatalogUnavailableException>(() => authority.CommitAsync("unapproved-attempt"));
        (await authority.ReadbackAsync()).RootDigest.ShouldBe(snapshot.RootDigest);
    }
}
