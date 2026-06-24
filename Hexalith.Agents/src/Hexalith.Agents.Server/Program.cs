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

// Story 1.5: Provider/model selection wiring. The decision logic — the provider-catalog reader port and the
// selection + activation re-validation orchestrations — is registered here and fully unit-tested. Binding the
// reader to the live ProviderCatalog read-model (and the dispatcher to the live DAPR/EventStore gateway), plus a
// runnable AppHost topology, is deferred to the operational-topology/read-model story (mirroring Story 1.2/1.3/1.4).
// The DeferredProviderCatalogReader keeps the DI graph complete and compiling. No provider SDK is referenced — the
// ProviderCatalog is in-module.
builder.Services.AddSingleton<IProviderCatalogReader, DeferredProviderCatalogReader>();
builder.Services.AddScoped<AgentProviderSelectionOrchestrator>();
builder.Services.AddScoped<AgentActivationProviderRevalidation>();

// Story 1.6: Response-mode + approver-policy configuration wiring. The two thin config orchestrations and the
// approver-policy resolver port are registered here and fully unit-tested. The extended activation re-validation
// (registered above) now also resolves the added IApproverPolicyResolver dependency to populate the trusted
// approver:policyValidation verdict at activation. The live Tenants-projection / Conversations-facilitator legs (and
// the live command dispatch / AppHost topology) remain deferred — the DeferredApproverPolicyResolver keeps the DI
// graph complete and compiling (mirroring Story 1.2/1.4/1.5). No provider SDK or sibling-module reference is needed
// (Parties is already referenced; its leg can reuse PartiesAgentPartyDirectory once the read-model story wires it).
builder.Services.AddSingleton<IApproverPolicyResolver, DeferredApproverPolicyResolver>();
builder.Services.AddScoped<AgentResponseModeOrchestrator>();
builder.Services.AddScoped<AgentApproverPolicyOrchestrator>();

// Story 1.7: Content Safety Policy configuration wiring. The thin config orchestration is registered here and fully
// unit-tested; the aggregate's new Handle auto-registers via the existing assembly scan. Content safety is
// self-contained Agent state — no Parties/Tenants/Conversations/provider read, no resolver port, no verdict, and no
// change to AgentActivationProviderRevalidation (it still re-resolves only provider + approver). Live command dispatch
// / AppHost topology remain deferred via DeferredAgentCommandDispatcher (mirroring 1.2/1.4/1.5/1.6).
builder.Services.AddScoped<AgentContentSafetyPolicyOrchestrator>();

WebApplication app = builder.Build();

app.UseEventStoreDomainService();

app.Run();
