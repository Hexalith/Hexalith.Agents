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

    // Story 2.5: the live Conversation posting + membership port reuses the same IConversationClient registered above.
    builder.Services.AddSingleton<IConversationResponsePoster, ConversationClientResponsePoster>();
}
else
{
    builder.Services.AddSingleton<IConversationContextReader, DeferredConversationContextReader>();

    // Story 2.5: no Conversations config → the deferred poster fails closed (membership SeamUnavailable, append unavailable).
    builder.Services.AddSingleton<IConversationResponsePoster, DeferredConversationResponsePoster>();
}

builder.Services.AddScoped<AgentInteractionContextOrchestrator>();

// Story 2.4: Agent-output generation + content-safety wiring. The generation aggregate handler auto-registers via the
// existing AddEventStoreDomainService assembly scan (no host change needed). The generation orchestration re-reads the
// Source Conversation content (reusing the Story 2.3 IConversationContextReader registered above — live behind the
// "Conversations" section, else deferred), reads the reused provider-catalog budget/timeout, invokes the provider behind
// the NEW IAgentGenerationProvider adapter (the first real model-invocation seam), resolves + evaluates the effective
// Content Safety Policy, and dispatches the GenerateAgentOutput command. The three new ports' live bindings stay deferred
// and FAIL CLOSED so the default DI graph blocks content-bearing generation safely: DeferredAgentGenerationProvider
// (ProviderUnavailable), DeferredContentSafetyEvaluator (Blocked — engine deferred, PRD OQ-9), and
// DeferredAgentContentSafetyPolicyReader (not-available). A future live provider-SDK adapter binding is gated behind a
// config section, mirroring the Conversations block; no provider SDK enters the default graph (AD-9, AD-14). Live command
// dispatch stays deferred behind DeferredAgentCommandDispatcher, mirroring 1.2/1.4/1.5/1.6/1.7/2.1/2.2/2.3.
builder.Services.AddSingleton<IAgentGenerationProvider, DeferredAgentGenerationProvider>();
builder.Services.AddSingleton<IContentSafetyEvaluator, DeferredContentSafetyEvaluator>();
builder.Services.AddSingleton<IAgentContentSafetyPolicyReader, DeferredAgentContentSafetyPolicyReader>();
builder.Services.AddScoped<AgentInteractionGenerationOrchestrator>();

// Story 2.5: Automatic-response posting wiring. The posting aggregate handler auto-registers via the existing
// AddEventStoreDomainService assembly scan (no host change needed). The posting orchestration reads the Agent's linked
// Party identity + posting-time validity (new IAgentPartyReader), reads the selected generated version + content (new
// IAgentGeneratedVersionReader), verifies the Agent's AiAgent membership and appends the message authored by the Agent
// Party through the NEW IConversationResponsePoster (over the same Hexalith.Conversations.Client, live behind the
// "Conversations" section, else deferred — registered above), and dispatches the PostAgentResponse command. The two new
// in-module readers' live bindings stay deferred and FAIL CLOSED so the default DI graph cannot read content, cannot prove
// membership, and cannot post — every path resolves to PostingFailed: DeferredAgentPartyReader (PartyIdentityUnavailable)
// and DeferredAgentGeneratedVersionReader (content unavailable → AdapterFailure). A future live IAgentPartyReader /
// IAgentGeneratedVersionReader binding (over the in-module Agent/AgentInteraction read models) and the live Conversations
// poster are wired only when their dependencies/config are present. Live command dispatch stays deferred behind
// DeferredAgentCommandDispatcher, mirroring 1.2/1.4/1.5/1.6/1.7/2.1/2.2/2.3/2.4.
builder.Services.AddSingleton<IAgentPartyReader, DeferredAgentPartyReader>();
builder.Services.AddSingleton<IAgentGeneratedVersionReader, DeferredAgentGeneratedVersionReader>();
builder.Services.AddScoped<AgentInteractionPostingOrchestrator>();

// Story 3.1: Confirmation-mode Proposed-Agent-Reply creation wiring. The new creation aggregate handler auto-registers via
// the existing AddEventStoreDomainService assembly scan (no host change needed). The proposal orchestration reuses the
// selected-version reader (Story 2.5 IAgentGeneratedVersionReader — for the authoritative version id ONLY; the generated
// content is deliberately ignored, AD-14) and the IAgentCommandDispatcher (both already registered above), reads the
// optional expiry via the NEW IProposalExpiryPolicyReader, derives a deterministic ProposalId, and dispatches the
// CreateProposedAgentReply command. A Proposed Agent Reply is NEVER a Conversation Message: this path makes no Conversations
// write and reads no Party identity (AD-6, AC2) — NO new sibling-module/provider references are added. The new expiry
// reader's live binding (and expiry enforcement → an Expired terminal state) stays deferred to Story 3.6; the
// DeferredProposalExpiryPolicyReader records no expiry, keeping the DI graph complete and the default graph fail-closed to
// ProposalCreationFailed (the deferred version reader returns not-available). Live command dispatch / read-model bindings
// and the durable-owner chaining (generate → branch on response mode → post (2.5) / propose (3.1)) remain deferred to the
// operational-topology story (Epic 4), mirroring 1.2/1.4/1.5/1.6/1.7/2.1–2.5.
builder.Services.AddSingleton<IProposalExpiryPolicyReader, DeferredProposalExpiryPolicyReader>();
builder.Services.AddScoped<AgentInteractionProposalOrchestrator>();

// Story 3.3: Confirmation-mode Proposed-Agent-Reply edit wiring. The new edit aggregate handler (the 7th Handle on
// AgentInteraction) auto-registers via the existing AddEventStoreDomainService assembly scan (no host change needed). The
// edit orchestration is the first edit-time approver-authorization use: it reuses the IApproverPolicyResolver (Story 1.6,
// already registered above) to re-resolve the snapshotted Approver Policy against current dependencies + freshness and the
// IAgentCommandDispatcher (already registered above) to dispatch the EditProposedAgentReply command. It reads no Party
// identity and makes no Conversations write — an edited Proposed Agent Reply is NEVER a Conversation Message (AD-6) — so NO
// new sibling-module/provider reference is added, and NO new read port is needed (the source version + edited content
// arrive on the request). The deferred approver resolver fails closed (denied, no dispatch), keeping the default graph
// fail-closed. The live command dispatch / read-model bindings and the audit-evidence projection remain deferred to the
// operational-topology / read-model story (Epic 4), mirroring 1.2/1.4/1.5/1.6/1.7/2.1-2.5/3.1.
builder.Services.AddScoped<AgentInteractionProposalEditOrchestrator>();

// Story 3.4: Confirmation-mode Proposed-Agent-Reply regeneration wiring. The new regeneration aggregate handler (the 8th
// Handle on AgentInteraction) auto-registers via the existing AddEventStoreDomainService assembly scan (no host change
// needed). The regeneration orchestration combines the Story 3.3 edit-time approver authorization (IApproverPolicyResolver,
// already registered above) with the Story 2.4 provider invocation + content-safety gate, reusing the IConversationContextReader
// (live behind the "Conversations" section, else deferred), IProviderCatalogReader, IAgentGenerationProvider,
// IAgentContentSafetyPolicyReader, IContentSafetyEvaluator, and IAgentCommandDispatcher already registered above — NO new port
// is introduced. AC4 (a terminal proposal never invokes the provider) is enforced before any conversation re-read/provider
// call from the trusted proposal sub-state on the request. A regenerated Proposed Agent Reply is NEVER a Conversation Message
// and reads no Party identity (AD-6) — no new sibling-module/provider reference is added. The deferred provider/safety/approver
// seams keep the default graph fail-closed (no content-bearing regeneration). The live command dispatch / read-model bindings
// and the audit-evidence projection remain deferred to the operational-topology / read-model story (Epic 4), mirroring
// 1.2/1.4/1.5/1.6/1.7/2.1-2.5/3.1/3.3.
builder.Services.AddScoped<AgentInteractionProposalRegenerationOrchestrator>();

// Story 3.5: Confirmation-mode approval + posting wiring. The approval aggregate handler auto-registers via the existing
// assembly scan. The approval orchestration reuses approver authorization, exact selected-version reads, Agent Party
// identity, Conversations membership/posting, deterministic message identity, and command dispatch. The deferred default
// graph fails closed and cannot post content until live readers/posters are wired.
builder.Services.AddScoped<AgentInteractionProposalApprovalOrchestrator>();

// Story 3.6: Confirmation-mode reject / abandon / expire wiring. The three new terminal-transition aggregate handlers (the
// 9th-11th Handle on AgentInteraction) and their event types auto-register via the existing AddEventStoreDomainService
// assembly scan (no host change needed). The reject/abandon orchestrators are minimal-deps terminal actions: they reuse the
// IApproverPolicyResolver (Story 1.6) to re-resolve the snapshotted Approver Policy and the IAgentCommandDispatcher to
// dispatch the terminal command — they read no Party identity and make no Conversations write (a terminal proposal is NEVER
// a Conversation Message — AD-6). The expiry orchestrator reuses the IProposalExpiryPolicyReader (Story 3.1) + dispatcher to
// ship the deterministic expiry decision + transition against a trusted evaluation timestamp supplied on the request (AD-3 —
// no aggregate wall-clock). Per AD-18 this story does NOT introduce the module's first scheduler: the reader stays the
// fail-closed DeferredProposalExpiryPolicyReader and the dispatcher stays Deferred, so the default graph fails closed (no
// expiry, no terminal transition) until the live reader/dispatcher bindings and the automatic firing trigger are wired in
// Epic 4, mirroring every prior Epic-3 orchestrator.
builder.Services.AddScoped<AgentInteractionProposalRejectionOrchestrator>();
builder.Services.AddScoped<AgentInteractionProposalAbandonmentOrchestrator>();
builder.Services.AddScoped<AgentInteractionProposalExpiryOrchestrator>();

WebApplication app = builder.Build();

app.UseEventStoreDomainService();

app.Run();
