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
using Hexalith.Agents.Server.Application.AgentInteractions;
using Hexalith.Agents.Server.Application.Agents;
using Hexalith.Agents.Server.Ports;
using Hexalith.Conversations.Client;
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

// Story 2.1: Agent Call request-creation wiring. The new AgentInteraction aggregate auto-registers via the existing
// AddEventStoreDomainService assembly scan (no host change needed). The request orchestration (deterministic id +
// AD-4 snapshot assembly + dispatch) and the Agent configuration snapshot reader port are registered here and fully
// unit-tested. The live read-model binding stays deferred behind DeferredAgentConfigurationSnapshotReader (it fails
// closed to a not-available snapshot), and live command dispatch stays deferred behind DeferredAgentCommandDispatcher
// (mirroring 1.2/1.4/1.5/1.6/1.7). No provider adapter and no IConversationClient/Parties client is wired (AC3).
builder.Services.AddSingleton<IAgentConfigurationSnapshotReader, DeferredAgentConfigurationSnapshotReader>();
builder.Services.AddScoped<AgentInteractionRequestOrchestrator>();

// Story 2.2: Invocation authorization + dependency-readiness gate wiring. The gate aggregate handler auto-registers
// via the existing AddEventStoreDomainService assembly scan (no host change needed). The gate orchestration assembles
// trusted verdicts from three new read ports — tenant access, Source Conversation access, and current Agent readiness
// — plus the reused Parties/ProviderCatalog/approver ports registered above, then dispatches the EvaluateAgentInteractionGate
// command. The three new ports' live bindings stay deferred and fail closed (Tenants projection, Story 2.3 Conversations
// read, and the Agent read-model respectively); their Deferred* placeholders keep the DI graph complete and compiling.
// No provider adapter, no Conversations post, and no proposal is wired here (AC2), mirroring 1.2/1.4/1.5/1.6/1.7/2.1.
builder.Services.AddSingleton<ITenantAccessReader, DeferredTenantAccessReader>();
builder.Services.AddSingleton<IConversationAccessReader, DeferredConversationAccessReader>();
builder.Services.AddSingleton<IAgentInvocationReadinessReader, DeferredAgentInvocationReadinessReader>();
builder.Services.AddScoped<AgentInteractionGateOrchestrator>();

// Story 2.3: Conversation context-building wiring. The context aggregate handler auto-registers via the existing
// AddEventStoreDomainService assembly scan (no host change needed). The context orchestration reads the authorized
// Source Conversation content, measures tokens, reads the reused provider-catalog budget, and dispatches the
// BuildAgentInteractionContext command. Per Story 2.2's hand-off, the LIVE Conversations content reader
// (ConversationClientContextReader over IConversationClient.GetConversationAsync) IS authored here, but registered only
// when a "Conversations" config section exists; otherwise the DeferredConversationContextReader keeps the DI graph
// complete and fails closed (ContextBlocked/ContextUnavailable). Token measurement stays deferred (no tokenizer bound),
// and live command dispatch stays deferred behind DeferredAgentCommandDispatcher. No provider adapter, no Conversations
// post, and no proposal is wired here (AC3), mirroring 1.2/1.4/1.5/1.6/1.7/2.1/2.2.
builder.Services.AddSingleton<IConversationContextTokenMeasurer, DeferredConversationContextTokenMeasurer>();
if (builder.Configuration.GetSection("Conversations").Exists())
{
    builder.Services.AddHexalithConversationsClient(o => o.Endpoint = new Uri(builder.Configuration["Conversations:BaseUrl"]!));
    builder.Services.AddSingleton<IConversationContextReader, ConversationClientContextReader>();
}
else
{
    builder.Services.AddSingleton<IConversationContextReader, DeferredConversationContextReader>();
}

builder.Services.AddScoped<AgentInteractionContextOrchestrator>();

WebApplication app = builder.Build();

app.UseEventStoreDomainService();

app.Run();
