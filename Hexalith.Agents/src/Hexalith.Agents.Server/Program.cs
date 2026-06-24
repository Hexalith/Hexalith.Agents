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
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;
using Hexalith.EventStore.DomainService;
using Hexalith.Parties.Client.Extensions;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddEventStoreDomainService(
    typeof(AgentsAssemblyMarker).Assembly,
    typeof(ServerAssemblyMarker).Assembly);

// The shared domain-service host does not register a DaprClient; register it here (mirroring the sibling
// hosts). DAPR arrives transitively through the DomainService SDK, so the Server declares no direct Dapr
// package. TryAdd semantics keep this safe if a future host already registers one.
builder.Services.AddDaprClient();

// Story 1.4: Party-identity link/provision wiring. The decision logic — the Parties adapter (IAgentPartyDirectory)
// and the orchestration — is registered here and fully unit-tested. The live Parties client requires a
// "Parties:{ BaseUrl, Tenant }" config section and validates it eagerly, so it is registered only when present;
// binding the orchestration's command dispatch to the live DAPR/EventStore gateway (and standing up a runnable
// AppHost topology) is deferred to the operational-topology story, mirroring Story 1.2/1.3 deferring the
// read-model binding. The IAgentCommandDispatcher placeholder keeps the DI graph complete and compiling.
if (builder.Configuration.GetSection("Parties").Exists())
{
    builder.Services.AddPartiesClient(builder.Configuration);
}

builder.Services.AddScoped<IAgentPartyDirectory, PartiesAgentPartyDirectory>();
builder.Services.AddSingleton<IAgentCommandDispatcher, DeferredAgentCommandDispatcher>();
builder.Services.AddScoped<AgentPartyIdentityOrchestrator>();

WebApplication app = builder.Build();

app.UseEventStoreDomainService();

app.Run();
