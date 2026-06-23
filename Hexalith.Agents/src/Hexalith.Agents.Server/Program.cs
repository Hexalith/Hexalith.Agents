// Hexalith Agents module domain-service host (Story 1.2).
//
// Now that the ProviderCatalog aggregate exists, the Story 1.1 placeholder host is replaced with the canonical
// Hexalith EventStore domain-service host. The SDK owns all hosting, DAPR-endpoint, observability, and
// convention-discovery boilerplate; this module ships only its domain code plus this short host. The
// explicit-assemblies overload (never the calling-assembly one) targets the Agents domain assembly — where
// ProviderCatalogAggregate lives — plus the Server boundary assembly, so future IDomainQueryHandler /
// IDomainProjectionHandler implementations are discovered without re-touching this host.
//
// Scope (Story 1.2): no provider adapter, AgentHost workflow, FrontComposer UI page, or runnable full AppHost
// topology — those arrive in later stories.
using Hexalith.Agents;
using Hexalith.Agents.Server;
using Hexalith.EventStore.DomainService;

using Microsoft.Extensions.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddEventStoreDomainService(
    typeof(AgentsAssemblyMarker).Assembly,
    typeof(ServerAssemblyMarker).Assembly);

// The shared domain-service host does not register a DaprClient; register it here (mirroring the sibling
// hosts). DAPR arrives transitively through the DomainService SDK, so the Server declares no direct Dapr
// package. TryAdd semantics keep this safe if a future host already registers one.
builder.Services.AddDaprClient();

WebApplication app = builder.Build();

app.UseEventStoreDomainService();

app.Run();
