---
baseline_commit: 501407baea45a1cb9be4479ab4ca3f63651be922
---

# Story 2.1: Request `hexa` From A Source Conversation

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a Conversation Participant,
I want to explicitly call `hexa` from a Source Conversation with a prompt,
so that I can request contextual help without leaving the Conversation or creating an anonymous AI response.

## Acceptance Criteria

**AC1 - Create the AgentInteraction request record with a configuration snapshot**
**Given** `hexa` is active in setup readiness
**When** an authorized Conversation Participant submits an Agent Call request with tenant, Source Conversation, Agent, caller, prompt, and idempotency metadata
**Then** the system creates an `AgentInteraction` request record with deterministic identity and response mode snapshot
**And** the request captures caller `PartyId`, source `ConversationId`, Agent configuration version, instructions version, approver policy version, Provider/model identity, Provider capability version, context policy, and request timestamp.

**AC2 - Return a safe status reference and dedupe repeated requests**
**Given** the request is accepted
**When** public API/client contracts return the result
**Then** callers receive a structured Agent Call status reference rather than raw EventStore stream names, provider SDK details, or internal projection identifiers
**And** repeated requests with the same idempotency metadata do not create duplicate interactions.

**AC3 - Explicit invocation only; no ambient triggers and no side effects**
**Given** V1 excludes ambient or external triggers
**When** Conversation state changes without an explicit Agent Call
**Then** no AgentInteraction is created
**And** no provider invocation, proposal, or Conversation Message side effect occurs.

**AC4 - Protect sensitive prompt/context content and tenant boundaries**
**Given** prompt and Conversation-derived data are sensitive
**When** the interaction is logged, traced, returned in status summaries, or represented in audit summaries
**Then** raw prompt/context content is excluded or protected according to EventStore payload-protection/redaction conventions
**And** no unrelated tenant data is exposed.

[Source: _bmad-output/planning-artifacts/epics.md#Story-2.1-Request-hexa-From-A-Source-Conversation; _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-8-Call-Agent-From-Conversation; #FR-24-Capture-Agent-Audit-Evidence; _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-2-Aggregate-Boundaries; #AD-4-Interaction-Snapshot; #AD-13-Idempotent-External-Effects; #AD-14-Sensitive-Content-And-Secret-Safety]

## Tasks / Subtasks

- [x] **Task 1 - Add safe public AgentInteraction contracts** (AC: #1, #2, #4)
  - [x] Create `src/Hexalith.Agents.Contracts/AgentInteraction/` with the same folder/namespace layout the existing aggregates use: `Commands/`, `Events/`, `Events/Rejections/`, `Queries/`, plus value objects/enums directly under `AgentInteraction/`. Namespaces are file-scoped and mirror folders (`Hexalith.Agents.Contracts.AgentInteraction[.Commands|.Events|.Events.Rejections|.Queries]`). One public type per file.
  - [x] Add the public caller command `RequestAgentInteraction` as a plain `public record` (NO base interface, NO attribute — mirror `LinkAgentPartyIdentity.cs`). Caller-supplied fields only: `SourceConversationId` (string, opaque reference), `CallerPartyId` (string, opaque reference), `Prompt` (string, sensitive), `IdempotencyKey` (string), optional `ClientCorrelationId` (string?). The Agent id and tenant come from the `CommandEnvelope` (`AggregateId`/`TenantId`), never the payload — document this in the XML doc.
  - [x] Add the durable success event `InteractionRequested : IEventPayload` (`public record … : IEventPayload`). It carries the deterministic `AgentInteractionId`, `AgentId`, `CallerPartyId`, `SourceConversationId`, the snapshot fields (see Task 3 / the Snapshot Field Mapping table), the `Prompt`, and the `IdempotencyKey`. **Do NOT add a timestamp field** — request time is the EventStore event-metadata timestamp (server-stamped at persist), per the module's no-wall-clock rule.
  - [x] Add typed rejection events under `Events/Rejections/` implementing `IRejectionEvent`: `InvalidAgentInteractionRequestRejection(string AgentInteractionId, AgentInteractionRequestValidationStatus Status)` and `AgentInteractionAlreadyRequestedRejection(string AgentInteractionId)`. Rejections carry only the aggregate id plus a safe classification enum — never the prompt, conversation content, or caller PII.
  - [x] Add value objects/enums: `AgentInteractionStatus` (`Unknown = 0, Requested` — additive; later states like authorized/denied/contextLoading/generating/posted are added by Stories 2.2-2.5), `AgentInteractionRequestValidationStatus` (`Unknown = 0` plus safe reasons such as `MissingCaller`, `MissingSourceConversation`, `MissingPrompt`, `MissingAgentSnapshot`), and the safe status reference `AgentInteractionReference(string AgentInteractionId, AgentInteractionStatus Status)`. Every serialized enum uses `[JsonConverter(typeof(JsonStringEnumConverter))]` with an `Unknown = 0` sentinel (mirror `AgentResponseMode.cs`).
  - [x] Add the read query `GetAgentInteractionStatusQuery` and the safe `AgentInteractionStatusView` (interaction id, status, agent id, caller `PartyId`, source `ConversationId`, response mode, version numbers — NO prompt/context content). Mirror `GetAgentStatusQuery.cs` / `AgentStatusView.cs`.
  - [x] Keep `Hexalith.Agents.Contracts` inward-only: no server infrastructure, provider SDKs, Dapr runtime packages, UI packages, EventStore server internals, and **no reference to `Hexalith.Conversations.Contracts` or `Hexalith.Parties.Contracts`** — `SourceConversationId`/`CallerPartyId` are opaque `string`s, not the sibling `ConversationId`/`PartyId` record types.

- [x] **Task 2 - Implement the AgentInteraction aggregate and state** (AC: #1, #2, #3, #4)
  - [x] Create `src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs`: `[EventStoreDomain("agent-interaction")] public class AgentInteractionAggregate : EventStoreAggregate<AgentInteractionState>` (kebab-case domain, matching `provider-catalog`). Implement a single static handler `public static DomainResult Handle(RequestAgentInteraction command, AgentInteractionState? state, CommandEnvelope envelope)`.
  - [x] Handler guard cascade (mirror `Handle(CreateAgent)` in `AgentAggregate.cs:91-138`): `ArgumentNullException.ThrowIfNull(command/envelope)` → read `string interactionId = envelope.AggregateId` → structural validation of caller/source/prompt/snapshot → `Invalid(...)` rejection on any missing required field → idempotent duplicate check → emit `InteractionRequested`.
  - [x] Idempotent duplicate handling (AD-13): if `state is { IsRequested: true }`, compare the new request field-by-field (ordinal `string` comparison; compare snapshot scalars) against the recorded request. Exact match → `DomainResult.NoOp()`. Conflicting payload on the same deterministic id → `DomainResult.Rejection([new AgentInteractionAlreadyRequestedRejection(interactionId)])`. Never silently mutate. Mirror `CreateMatchesExisting` at `AgentAggregate.cs:818-827`.
  - [x] Keep the aggregate PURE (AD-3): emit events only. No provider SDK, no Conversations/Parties/Tenants reads, no Dapr, no HTTP, no logging/telemetry, no `DateTimeOffset.UtcNow`, no `Guid.NewGuid`. The deterministic id and the snapshot arrive pre-assembled in the command/envelope from the Server orchestration (Task 4).
  - [x] Create `src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs`: `public sealed class AgentInteractionState` (mutable class, NOT a record — match `AgentState`/`ProviderCatalogState`). Add an `IsRequested` flag, the request/snapshot fields as get/set auto-properties with safe initializers, and `public void Apply(InteractionRequested e)` that sets `IsRequested = true` and copies fields. Add a no-op `Apply` for EVERY rejection event via a private `MarkReplayOnlyEventHandled()` helper (mirror `AgentState.cs:289-293, :335`) so replay over a stream containing rejections stays total. Non-`InteractionRequested` applies guard `if (!IsRequested) return;`.
  - [x] Add `src/Hexalith.Agents/AgentInteraction/AgentInteractionRequestPolicy.cs` (internal static) for pure structural validation helpers (required-field checks, prompt-present check), mirroring `AgentConfigurationPolicy`. `InternalsVisibleTo` already exposes internals to `Hexalith.Agents.Tests`.

- [x] **Task 3 - Capture the AD-4 configuration snapshot** (AC: #1)
  - [x] The snapshot is sourced from the **Agent's current configuration at request time** and must be assembled by the Server orchestration (Task 4), because the pure aggregate cannot read the `Agent` aggregate (AD-3). Add the snapshot value object `AgentInteractionSnapshot` in Contracts carrying: `ConfigurationVersion` (int), `InstructionsVersion` (int), `ResponseMode` (`AgentResponseMode`), `ApproverPolicyVersion` (int), `ProviderId` (string), `ModelId` (string), `ProviderCapabilityVersion` (int), `ContentSafetyPolicyVersion` (int), and `ContextPolicyReference` (string). Carry it on `RequestAgentInteraction` (server-populated) and record it on `InteractionRequested`.
  - [x] **Nullability reconciliation:** on `AgentState` these fields are nullable until configured (`ProviderId`/`ModelId` are `string?` at `AgentState.cs:61,67`; `ProviderCapabilityVersion` is `int?` at `:73`; `ApproverPolicyVersion`/`ContentSafetyPolicyVersion` are `0` until set). The snapshot value object declares them **non-nullable** because the snapshot reader returns a populated `AgentInteractionSnapshot` ONLY for an Agent that has passed activation (AC1's "active in setup readiness" precondition) — by that point Provider/Model/capability version are guaranteed present. The reader's **not-available/blocked result** covers the pre-activation case; the aggregate's `MissingAgentSnapshot` check is a pure null/empty-scalar guard on the command payload (never a cross-aggregate read). Do not weaken the snapshot fields to nullable.
  - [x] `ContentSafetyPolicyVersion` is an **additive extension beyond AD-4's enumerated floor** (AD-4 lists config/instructions/response-mode/approver-policy versions, Provider/Model identity, capability version, caller/source ids, and context-build policy — not content-safety version). It is snapshotted here to anticipate Story 2.4's safety check; keep it, but treat it as additive, not AD-4-mandated.
  - [x] Add a server port `IAgentConfigurationSnapshotReader` in `src/Hexalith.Agents.Server/Ports/` returning an `AgentConfigurationSnapshot` (or a safe not-available result) for `(TenantId, AgentId)`. Provide a `DeferredAgentConfigurationSnapshotReader` placeholder that returns a not-available/blocked result until the live read-model binding lands (mirror `DeferredProviderCatalogReader.cs`). The snapshot-assembly logic and the aggregate recording are implemented and unit-tested NOW with a stubbed reader (NSubstitute); only the live DAPR/read-model binding is deferred — exactly how Story 1.2 deferred its read path.
  - [x] `ResponseMode` is snapshotted so a later admin mode change does not affect this interaction (AD-4). `ConfigurationVersion`/`InstructionsVersion`/`ApproverPolicyVersion`/`ContentSafetyPolicyVersion` are the monotonic ints already maintained on `AgentState` for this exact purpose ([Source: Hexalith.Agents/src/Hexalith.Agents/Agent/AgentAggregate.cs:415-418]).
  - [x] `ContextPolicyReference`: the Agent does not yet store a distinct Conversation Context Policy (that policy is elaborated and consumed in Story 2.3). For 2.1, snapshot a single stable V1 default reference. **Define it once as a named const** (e.g. `AgentInteractionRequestPolicy.DefaultContextPolicyReference = "full-conversation-v1"`) and use that same const in both the orchestrator and the tests so they cannot drift. Treat it as an opaque reference that Story 2.3 replaces; keep the field additive so 2.3 can populate the concrete policy without a contract break. [Source: PRD FR-9 "records … the Conversation Context Policy version or equivalent identifier"]

- [x] **Task 4 - Implement the request orchestration (deterministic id, snapshot, no side effects)** (AC: #1, #2, #3, #4)
  - [x] Add `src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionRequestOrchestrator.cs` (mirror the thin `AgentProviderSelectionOrchestrator`/`AgentPartyIdentityOrchestrator` shape). It: (1) derives the deterministic `AgentInteractionId`; (2) reads the Agent snapshot via `IAgentConfigurationSnapshotReader`; (3) assembles the server-trusted `RequestAgentInteraction` command with the snapshot filled in (any client-supplied snapshot value is overwritten from the trusted read); (4) builds the `CommandEnvelope` with `AggregateId = AgentInteractionId`, `Domain = "agent-interaction"`, trusted extensions stripped/repopulated via the established `BuildTrustedExtensions` pattern; (5) dispatches via the existing `IAgentCommandDispatcher` (live dispatch stays deferred behind `DeferredAgentCommandDispatcher`); (6) returns an `AgentInteractionReference` (safe id + `Requested` status).
  - [x] **Deterministic id derivation** (AD-13): compose a stable id from `(TenantId, AgentId, SourceConversationId, CallerPartyId, IdempotencyKey)` so re-issuing the same call yields the same `AgentInteractionId` (→ aggregate `NoOp`). The id MUST satisfy the EventStore `AggregateId` regex `^[a-zA-Z0-9]([a-zA-Z0-9._-]*[a-zA-Z0-9])?$`, ≤256 chars, **no colons**. Use a deterministic, collision-resistant encoding (e.g. a hash of the joined components rendered as base36/hex). Do NOT use `Guid.NewGuid`/ULID (non-deterministic) and do NOT `Guid.TryParse` any id field. Id derivation lives in the orchestration/a pure helper, never in the aggregate.
  - [x] **No side effects (AC3):** the orchestration MUST NOT call any provider adapter and MUST NOT call `IConversationClient` (read or post) or any Parties/validation client in this story. There is also **no ambient subscription** that creates an `AgentInteraction` from Conversation state changes — only the explicit `RequestAgentInteraction` command path. Authorization/dependency gating (tenant access, caller Party state, conversation access, agent/provider readiness, policy) is **Story 2.2** — do not implement it here beyond the structural-validity checks above.
  - [x] **AC1 precondition, not a gate:** AC1's Given ("`hexa` is active in setup readiness") is the operating assumption under which this story runs, NOT a readiness check to implement here. Story 2.1 performs only structural validation (required fields + snapshot present); proving the Agent is active/callable is **Story 2.2**. The snapshot reader naturally reflects this — it returns a populated snapshot only for an activated Agent and a not-available result otherwise (the latter surfaces as a structural `MissingAgentSnapshot` rejection, not a re-implemented activation gate).
  - [x] Register the orchestrator, the new port, and the `DeferredAgentConfigurationSnapshotReader` in `src/Hexalith.Agents.Server/Program.cs` alongside the existing deferred registrations. The aggregate itself needs no host change — it is auto-discovered by the existing `AddEventStoreDomainService(typeof(AgentsAssemblyMarker).Assembly, …)` scan.

- [x] **Task 5 - Add focused tests and run the narrow verification** (AC: #1, #2, #3, #4)
  - [x] **Domain tests** (`tests/Hexalith.Agents.Tests/`): add `AgentInteractionTestData.cs` (fixture: `Envelope<T>`, `ValidRequest(...)`, `RequestedEvent(...)`, `StateWith(request)` driving the real `Apply`, an `ApplyAll`/`ProcessAndApplyAsync` helper — mirror `AgentTestData.cs`). Add `AgentInteractionAggregateTests.cs` and `AgentInteractionStateReplayTests.cs` covering: request with no state → `InteractionRequested` with full snapshot + `Requested` status; deterministic-id exact-duplicate → `NoOp`; conflicting payload on same id → `AgentInteractionAlreadyRequestedRejection`; missing caller/source/prompt/snapshot → `InvalidAgentInteractionRequestRejection`; replay determinism; persisted rejection events are replay-safe no-ops; full reflection-dispatch + JSON round-trip via `ProcessAsync`.
  - [x] **Contract tests** (`tests/Hexalith.Agents.Contracts.Tests/`): System.Text.Json round-trip for `RequestAgentInteraction`, `InteractionRequested`, rejections, query, and views; marker-interface conformance (`IEventPayload`/`IRejectionEvent`); enum serialize-by-name + `Unknown` fallback. The assembly-wide `ContractsSecretNonDisclosureTests` auto-covers the new types — confirm no member trips the forbidden secret tokens (`Secret`/`ApiKey`/`Credential`/`Password`/`ConnectionString`) or PII tokens (`DisplayName`/`Contact`/`Email`/`Phone`/`PersonalData`). The `Prompt` field is permitted on the durable event/state only; assert it is absent from `AgentInteractionStatusView`, `AgentInteractionReference`, and every rejection.
  - [x] **Server tests** (`tests/Hexalith.Agents.Server.Tests/`): orchestrator unit tests with NSubstitute stubs proving (a) deterministic id is identical for identical request inputs and regex-valid; (b) the snapshot is read from `IAgentConfigurationSnapshotReader` and written into the dispatched command; (c) a safe `AgentInteractionReference` is returned with no stream name/provider detail; (d) **no provider adapter and no `IConversationClient`/Parties client is invoked**; (e) reserved client-supplied extensions/snapshot values are stripped/overwritten. The structural guard tests (`StructuralSeedConformanceTests`, `ProjectReferenceDirectionTests`, `PublicContractPackageBoundaryTests`, `ModuleLayout`, `PackageVersionCentralizationTests`, `BuildContractConformanceTests`) must stay green.
  - [x] Keep xUnit v3 + Shouldly (no raw `Assert.*`); NSubstitute only outside aggregate logic; aggregate tests are pure command/state/event tests. Test method names follow the surrounding files' style.
  - [x] Write a test-summary artifact to `_bmad-output/implementation-artifacts/tests/2-1-test-summary.md` and include it in the File List (matches the prior-story convention).
  - [x] Run from `/home/administrator/projects/hexalith/agents/Hexalith.Agents`:
    - `dotnet restore Hexalith.Agents.slnx`
    - `dotnet build Hexalith.Agents.slnx --configuration Release` (must be 0 warnings / 0 errors — `TreatWarningsAsErrors=true`)
    - `dotnet test tests/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj --configuration Release`
    - `dotnet test tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release`
    - `dotnet test tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj --configuration Release`

## Dev Notes

### Critical Guardrails

- This story is the **AgentInteraction request-creation step ONLY**. Implement: the `AgentInteraction` aggregate + state, the `RequestAgentInteraction` command, the `InteractionRequested` event (with snapshot), rejections, the request orchestration (deterministic id + snapshot assembly), the safe status reference/view, and tests. Do **NOT** implement: authorization/dependency gating (Story 2.2), Conversation context building/reads (Story 2.3), provider generation + safety checks (Story 2.4), automatic posting to Conversations (Story 2.5), or invocation UX/status UI (Story 2.6). [Source: _bmad-output/planning-artifacts/epics.md#Epic-2-Safe-Conversation-Invocation-And-Automatic-Replies]
- `AgentInteraction` is its **own aggregate boundary** (AD-2), distinct from `Agent` and `ProviderCatalog`. It owns each call, the request snapshot, and (in later stories) generation attempts, proposal lifecycle, and posting outcome. It snapshots Agent/ProviderCatalog state; it never mutates them. [Source: ARCHITECTURE-SPINE.md#AD-2-Aggregate-Boundaries; #AD-4-Interaction-Snapshot]
- **AD-4 snapshot is the heart of this story.** The interaction must freeze Agent configuration version, instructions version, response mode, approver policy version, `ProviderId`, `ModelId`, provider capability version, caller `PartyId`, source `ConversationId`, and context-build policy at request time so later Agent/ProviderCatalog edits affect only future interactions. (Content-safety policy version is additionally snapshotted as an additive extension anticipating Story 2.4 — it is not part of AD-4's enumerated floor.) [Source: ARCHITECTURE-SPINE.md#AD-4-Interaction-Snapshot; PRD FR-8 "Every Agent Call records caller, Agent, Source Conversation, request timestamp, and response mode"]
- **Pure aggregate, side effects outside (AD-3).** The aggregate emits events only. The deterministic id, the Agent snapshot read, and all dependency reads happen in Server orchestration/adapters and feed back through the command. No `UtcNow`, no `Guid.NewGuid`, no I/O in `Handle`/`Apply`. [Source: ARCHITECTURE-SPINE.md#AD-3-Pure-Aggregates-Side-Effects-Outside; Hexalith.EventStore/_bmad-output/project-context.md]
- **No ambient triggers, no side effects (AC3).** Only the explicit `RequestAgentInteraction` command creates an interaction. This story performs zero provider calls, zero Conversations reads/posts, zero proposal creation. Wire nothing live to `IConversationClient` or any provider adapter. [Source: ARCHITECTURE-SPINE.md#AD-6-Conversations-Boundary; #AD-18-Hybrid-Agent-Runtime-Ownership; epics.md#Story-2.1 AC3]
- **Sensitive content (AD-14).** The raw `Prompt` is sensitive conversation-derived content. It may live only on the durable `InteractionRequested` event and `AgentInteractionState` — never on rejections, the status view, the status reference, logs, telemetry dimensions, or audit summaries. Follow the exact precedent set for the Agent `Instructions` text: plaintext on the durable event for local/dev, no bespoke encryption invented here, and a note that content-bearing events need EventStore payload-protection before production. [Source: ARCHITECTURE-SPINE.md#AD-14-Sensitive-Content-And-Secret-Safety; Hexalith.Agents/src/Hexalith.Agents/Agent/AgentAggregate.cs:38-44]
- **Idempotency (AD-13).** The deterministic `AgentInteractionId` derived from the caller's idempotency metadata is the dedup key. Re-issuing the same call → aggregate `NoOp` (no duplicate event); a conflicting payload on the same id → rejection. EventStore additionally dedupes at the command layer by `CausationId`/`MessageId`. [Source: ARCHITECTURE-SPINE.md#AD-13-Idempotent-External-Effects; Hexalith.EventStore/docs/concepts/command-lifecycle.md]
- **Tenant isolation (AD-12, FR-19).** Tenant scope comes from the envelope; the aggregate only ever sees its own tenant's stream (structural disjointness — colons forbidden in identity components). No unrelated-tenant data appears on any event/state/status. [Source: ARCHITECTURE-SPINE.md#AD-12-Authorization-And-Dependency-Uncertainty; Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Identity/AggregateIdentity.cs]

### Design: Where Each Responsibility Lives

```
Conversation Participant (explicit call)
        │  RequestAgentInteraction (SourceConversationId, CallerPartyId, Prompt, IdempotencyKey)
        ▼
Hexalith.Agents.Server — AgentInteractionRequestOrchestrator   [impure: derives id, reads snapshot, dispatches]
        │  1. derive deterministic AgentInteractionId from (tenant, agent, conversation, caller, idempotencyKey)
        │  2. IAgentConfigurationSnapshotReader → AgentConfigurationSnapshot   [DeferredAgentConfigurationSnapshotReader for now]
        │  3. assemble server-trusted RequestAgentInteraction (+ snapshot) ; build CommandEnvelope(AggregateId=id, Domain="agent-interaction")
        │  4. IAgentCommandDispatcher.Dispatch(...)   [DeferredAgentCommandDispatcher — no live dispatch yet]
        │  5. return AgentInteractionReference(id, Requested)   ← safe, no stream names
        ▼
Hexalith.Agents — AgentInteractionAggregate.Handle(...)        [pure: events only]
        │  guards → validate → idempotent NoOp/rejection → DomainResult.Success([InteractionRequested(snapshot, prompt)])
        ▼
AgentInteractionState.Apply(InteractionRequested)              [pure: in-place mutation, replay-safe]
```

No provider, no Conversations, no Parties calls anywhere in this flow. Context building (2.3), generation (2.4), and posting (2.5) attach to this same aggregate later.

### Current Code State To Preserve

Read these files completely before editing; they are the exact templates to mirror:

- `Hexalith.Agents/src/Hexalith.Agents/Agent/AgentAggregate.cs` — static `Handle(command, state, envelope) -> DomainResult`, guard cascade order (null-check → admin/authorization → not-found/state-check → `Invalid(...)` structural validation → idempotent `NoOp` → success event), `CreateMatchesExisting` duplicate compare, private `Denied(...)`/`Invalid(...)` rejection factories. `AgentInteraction` is explicitly named there as a future distinct aggregate.
- `Hexalith.Agents/src/Hexalith.Agents/Agent/AgentState.cs` — `sealed class`, `IsCreated` flag, mutate only in `Apply(success)` under `if (!IsCreated) return;`, no-op `Apply` per rejection via `MarkReplayOnlyEventHandled()`, monotonic version ints. These are the exact snapshot source fields (`ConfigurationVersion`, `InstructionsVersion`, `ResponseMode`, `ApproverPolicyVersion`, `ProviderId`, `ModelId`, `ProviderCapabilityVersion`, `ContentSafetyPolicyVersion`).
- `Hexalith.Agents/src/Hexalith.Agents/ProviderCatalog/ProviderCatalogAggregate.cs` + `ProviderCatalogState.cs` — second worked example of the same aggregate/state pattern, including dictionary-keyed child state and composite-key helpers if you need a parallel.
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/Commands/LinkAgentPartyIdentity.cs` and `Events/AgentPartyIdentityLinked.cs` — the canonical "store only an opaque `PartyId` reference, not PII; no wall-clock field; id+tenant from envelope" command/event shape.
- `Hexalith.Agents/src/Hexalith.Agents.Server/Application/Agents/AgentProviderSelectionOrchestrator.cs` (reads a sibling via a port + verdict) and `AgentResponseModeOrchestrator.cs` (the simpler no-verdict variant) — the thin orchestration shape, `BuildTrustedExtensions` strip/repopulate, and deferred dispatch.
- `Hexalith.Agents/src/Hexalith.Agents.Server/Ports/DeferredProviderCatalogReader.cs` + `IProviderCatalogReader.cs` + `DeferredAgentCommandDispatcher.cs` + `IAgentCommandDispatcher.cs` — the deferred-binding placeholder pattern to copy for `IAgentConfigurationSnapshotReader`/`DeferredAgentConfigurationSnapshotReader`.
- `Hexalith.Agents/src/Hexalith.Agents.Server/Program.cs` — canonical host (`AddEventStoreDomainService(typeof(AgentsAssemblyMarker).Assembly, typeof(ServerAssemblyMarker).Assembly)` + `AddDaprClient()` + `UseEventStoreDomainService()`); add the new orchestrator/port/reader registrations alongside the existing deferred ones. The new aggregate needs no host change (auto-discovered).
- `Hexalith.Agents/tests/Hexalith.Agents.Tests/AgentTestData.cs` and `AgentAggregateTests.cs`/`AgentStateReplayTests.cs` — fixture builders (`Envelope<T>`, `StateWith*`, `ApplyAll`, `ProcessAndApplyAsync`) and the snake_case `[Fact]` Shouldly style to reuse.
- `Hexalith.Agents/tests/Hexalith.Agents.Contracts.Tests/ContractsSecretNonDisclosureTests.cs` and `AgentContractsRoundTripTests.cs` — the assembly-wide secret/PII guard (auto-covers new types) and round-trip pattern.

What must be preserved:

- `.slnx` only, `net10.0`, `LangVersion 14`, nullable, implicit usings, **warnings as errors**, Central Package Management, no inline package versions. [Source: Hexalith.Agents/Directory.Build.props; global.json (SDK 10.0.301)]
- Aggregate purity and replay-safety: no I/O, no provider/dependency reads, no wall-clock, no direct state mutation outside `Apply`, no-op `Apply` for every rejection. [Source: ARCHITECTURE-SPINE.md#AD-3; Hexalith.Agents/src/Hexalith.Agents/Agent/AgentState.cs]
- Contracts stay provider-SDK-free, secret-value-free, PII-free, and free of sibling-module contract references. [Source: Story 1.1/1.2 guard tests; ARCHITECTURE-SPINE.md#AD-9; #AD-14]
- Sibling Hexalith modules are referenced as **source `ProjectReference`s** via the `$(HexalithEventStoreRoot)` etc. discovery blocks — never converted to NuGet. EventStore client/contracts are already referenced by the domain and contracts projects. [Source: Hexalith.Agents/Directory.Build.props]

### Snapshot Field Mapping (AD-4)

| Snapshot field on `InteractionRequested` | Type | Source |
| --- | --- | --- |
| `AgentInteractionId` | `string` | Deterministic id from orchestration (= `envelope.AggregateId`) |
| `AgentId` | `string` | Caller request / envelope target Agent |
| `CallerPartyId` | `string` (opaque) | Caller request — reference only, no PII [Source: Hexalith.Parties no-PII rule; AgentPartyIdentityLinked.cs] |
| `SourceConversationId` | `string` (opaque) | Caller request — reference only, not the Conversations `ConversationId` record |
| `ResponseMode` | `AgentResponseMode` | `AgentState.ResponseMode` via snapshot reader (AD-4 freeze) |
| `ConfigurationVersion` | `int` | `AgentState.ConfigurationVersion` |
| `InstructionsVersion` | `int` | `AgentState.InstructionsVersion` |
| `ApproverPolicyVersion` | `int` | `AgentState.ApproverPolicyVersion` |
| `ProviderId` | `string` | `AgentState.ProviderId` (`string?` on state, non-null once activated) |
| `ModelId` | `string` | `AgentState.ModelId` (`string?` on state, non-null once activated) |
| `ProviderCapabilityVersion` | `int` | `AgentState.ProviderCapabilityVersion` (`int?` on state, non-null once activated) |
| `ContentSafetyPolicyVersion` | `int` | `AgentState.ContentSafetyPolicyVersion` (additive beyond AD-4; anticipates Story 2.4) |
| `ContextPolicyReference` | `string` | V1 default const `"full-conversation-v1"`; concrete policy bound in Story 2.3 |
| `Prompt` | `string` (sensitive) | Caller request — durable event/state ONLY (AD-14) |
| `IdempotencyKey` | `string` | Caller request — also a deterministic-id input |
| request timestamp | (none) | EventStore event-metadata `Timestamp` (server-stamped at persist) — **no field on the event** |

All snapshot fields are populated server-side from the trusted Agent read; any client-supplied snapshot value is overwritten. The aggregate additionally rejects (`MissingAgentSnapshot`) if required snapshot scalars are absent, so a forged/empty snapshot cannot create a valid interaction.

### Idempotency And Deterministic Identity

- Deterministic `AgentInteractionId` = stable encoding of `(TenantId, AgentId, SourceConversationId, CallerPartyId, IdempotencyKey)`. Must match `^[a-zA-Z0-9]([a-zA-Z0-9._-]*[a-zA-Z0-9])?$`, ≤256 chars, **no colons**. Use a hash (e.g. SHA-256 → hex/base36) of the joined components; do not concatenate raw values that could contain illegal characters. [Source: ARCHITECTURE-SPINE.md#AD-13; Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Identity/AggregateIdentity.cs]
- There is **no built-in deterministic-id helper in EventStore** — derive it in the Agents orchestration (a pure helper is fine and unit-testable). System ids elsewhere use ULIDs (`UniqueIdHelper`) which are non-deterministic — do NOT use them for the interaction id.
- Aggregate-level idempotency mirrors `Handle(CreateAgent)`: exact-duplicate request on an already-`IsRequested` aggregate → `DomainResult.NoOp()`; conflicting payload → `AgentInteractionAlreadyRequestedRejection`. Record value-equality does not deep-compare lists/snapshots — compare snapshot scalars explicitly with ordinal string comparison. [Source: Hexalith.Agents/src/Hexalith.Agents/Agent/AgentAggregate.cs:118-125, :818-827]
- Command-layer idempotency: the EventStore actor caches the result keyed on `CausationId`; `MessageId` is the ULID idempotency key on `CommandEnvelope`. This is automatic — do not re-implement it. [Source: Hexalith.EventStore/docs/concepts/command-lifecycle.md]

### Cross-Module Boundaries (referenced, not called, in this story)

- **Conversations:** the public read boundary is `IConversationClient.GetConversationAsync(...)` and posting is `AppendMessageAsync(...)` — both are wired in Stories 2.3/2.5, not here. `ConversationId`/`MessageId` are `sealed record(string Value)` in Conversations; the canonical AI author participant is `ParticipantType.AiAgent` (wire value `"AIAgent"`) and the V1 conversation-owner authority is `ParticipantRole.Facilitator`. Store `SourceConversationId` as an opaque `string` on the Agents side. [Source: Hexalith.Conversations/src/Hexalith.Conversations.Client/IConversationClient.cs; Contracts/Identifiers/ConversationId.cs; Contracts/Participants/ParticipantType.cs; ARCHITECTURE-SPINE.md#AD-6; #AD-8]
- **Parties:** `PartyId` is a GUID-validated string in Parties, wrapped as `record PartyId(string Value)` in Conversations; the Agents `Agent` aggregate stores it as a plain `string?`. Follow the Agents precedent: opaque `string`, never parsed, never carrying a name/contact/identifier. Personal data is GDPR-protected (`[PersonalData]`) and must never be copied into an `AgentInteraction` event/state. [Source: Hexalith.Parties/src/Hexalith.Parties.Contracts/PersonalDataAttribute.cs; Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/Events/AgentPartyIdentityLinked.cs:9-14]
- **Tenants:** `TenantId` is a meaningful caller-supplied string (NOT a ULID/GUID) carried on the envelope; do not parse it. The local Tenants access projection that decides authorization is a **Story 2.2** concern. [Source: Hexalith.Tenants/_bmad-output/project-context.md; ARCHITECTURE-SPINE.md#AD-12]
- **EventStore payload protection:** the default `IEventPayloadProtectionService` is a no-op (payloads persisted as-is, marked `Unprotected`); both `CommandEnvelope.ToString()` and `EventEnvelope.ToString()` redact the payload with `[REDACTED]` to prevent accidental logging. There is no live encryption to wire here — match the Agent `Instructions` handling and note AD-14 for production. [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IEventPayloadProtectionService.cs; Contracts/Commands/CommandEnvelope.cs:79-89; docs/guides/payload-protection-and-crypto-shredding.md]

### Deferred-Binding Convention (mandatory)

Like Stories 1.2-1.8, keep the live external bindings deferred so the DI graph stays complete and unit-testable:

- `IAgentConfigurationSnapshotReader` → `DeferredAgentConfigurationSnapshotReader` (live read-model/DAPR binding deferred; snapshot-assembly logic implemented + tested with a stub).
- `IAgentCommandDispatcher` → existing `DeferredAgentCommandDispatcher` (no live dispatch yet).
- Do NOT wire `IConversationClient`, provider adapters, or a runnable AppHost topology in this story.

[Source: Hexalith.Agents/src/Hexalith.Agents.Server/Ports/DeferredProviderCatalogReader.cs; DeferredAgentCommandDispatcher.cs; _bmad-output/implementation-artifacts/1-2-govern-provider-catalog-entries.md#Completion-Notes-List]

### Latest Technical Information

- Checked package baselines on 2026-06-23 (Story 1.2 notes): NuGet shows `Microsoft.Agents.AI` ahead of the architecture baseline, but Agent Framework / provider SDK packages are **out of scope** for this request-creation story — add none. Provider invocation is Story 2.4. [Source: ARCHITECTURE-SPINE.md#Stack; #Deferred]
- xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute centrally managed. SDK pinned `10.0.301` (`rollForward: latestPatch`). No new packages are required for this story. [Source: Hexalith.Agents/Directory.Packages.props; global.json]

### Testing Requirements

- **Aggregate** (`Hexalith.Agents.Tests`): request→`InteractionRequested` (full snapshot + `Requested` status, prompt captured on the durable event); deterministic-id exact-duplicate → `NoOp`; conflicting payload same id → `AgentInteractionAlreadyRequestedRejection`; missing caller / source / prompt / snapshot → `InvalidAgentInteractionRequestRejection`; replay determinism (rebuild state from event stream); persisted rejection events are replay-safe no-ops; before-request events ignored via `IsRequested` guard; full `ProcessAsync` reflection-dispatch + JSON round-trip.
- **Contracts** (`Hexalith.Agents.Contracts.Tests`): round-trip serialization of all new types; marker conformance; enum serialize-by-name + `Unknown` fallback; secret/PII non-disclosure guard stays green; explicit assertion that `Prompt` is absent from `AgentInteractionStatusView`/`AgentInteractionReference`/rejections.
- **Server** (`Hexalith.Agents.Server.Tests`): deterministic-id stability + regex validity; snapshot read-and-recorded; safe reference returned (no stream names/provider detail); **no provider/Conversations/Parties client invoked**; trusted-extension/snapshot strip-and-repopulate; structural guard tests remain green.
- Build/test loop (run from `Hexalith.Agents/`): `dotnet restore Hexalith.Agents.slnx`; `dotnet build Hexalith.Agents.slnx --configuration Release` (0W/0E); then `dotnet test` each of `Hexalith.Agents.Tests`, `Hexalith.Agents.Contracts.Tests`, `Hexalith.Agents.Server.Tests` individually `--configuration Release`. Do not run solution-wide `dotnet test`.

### Project Structure Notes

New/changed code (no new projects — extend existing `Hexalith.Agents.Contracts`, `Hexalith.Agents`, `Hexalith.Agents.Server`, and the three test projects):

- `src/Hexalith.Agents.Contracts/AgentInteraction/` — `RequestAgentInteraction` (Commands), `InteractionRequested` (Events), `InvalidAgentInteractionRequestRejection` + `AgentInteractionAlreadyRequestedRejection` (Events/Rejections), `GetAgentInteractionStatusQuery` (Queries), value objects/enums (`AgentInteractionStatus`, `AgentInteractionRequestValidationStatus`, `AgentInteractionReference`, `AgentInteractionStatusView`, `AgentInteractionSnapshot`).
- `src/Hexalith.Agents/AgentInteraction/` — `AgentInteractionAggregate`, `AgentInteractionState`, `AgentInteractionRequestPolicy`.
- `src/Hexalith.Agents.Server/Ports/` — `IAgentConfigurationSnapshotReader`, `DeferredAgentConfigurationSnapshotReader`, `AgentConfigurationSnapshot` (server-side reader result).
- `src/Hexalith.Agents.Server/Application/AgentInteractions/` — `AgentInteractionRequestOrchestrator` (+ a deterministic-id helper).
- `src/Hexalith.Agents.Server/Program.cs` — register orchestrator/port/reader.
- `tests/` — `Hexalith.Agents.Tests/AgentInteractionTestData.cs`, `AgentInteractionAggregateTests.cs`, `AgentInteractionStateReplayTests.cs`; `Hexalith.Agents.Contracts.Tests/AgentInteractionContractsTests.cs` (+ round-trip); `Hexalith.Agents.Server.Tests/AgentInteractionRequestOrchestratorTests.cs`; `_bmad-output/implementation-artifacts/tests/2-1-test-summary.md`.

Discovery loaded: root epics + nested planning artifacts (PRD `prd-agents-2026-06-23`, architecture spine `architecture-agents-2026-06-23-2`, UX design/experience, implementation-readiness report), the as-built Agents module source (Agent/ProviderCatalog aggregates, contracts, server orchestrators/ports, prior story files 1.2/1.7/1.8), and sibling `project-context.md` files for EventStore, Conversations, Parties, and Tenants. No root `project-context.md` exists for `agents`; persistent facts came from the sibling module contexts (EventStore aggregate purity, source references for siblings, opaque cross-module id references, payload redaction precedent). This story was prepared with parallel research subagents analyzing the domain, contracts, and cross-module/prior-story conventions.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.1-Request-hexa-From-A-Source-Conversation]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic-2-Safe-Conversation-Invocation-And-Automatic-Replies]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-8-Call-Agent-From-Conversation]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-24-Capture-Agent-Audit-Evidence]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-19-Enforce-Tenant-Isolation]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-2-Aggregate-Boundaries]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-3-Pure-Aggregates-Side-Effects-Outside]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-4-Interaction-Snapshot]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-6-Conversations-Boundary]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-12-Authorization-And-Dependency-Uncertainty]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-13-Idempotent-External-Effects]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-14-Sensitive-Content-And-Secret-Safety]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-18-Hybrid-Agent-Runtime-Ownership]
- [Source: Hexalith.Agents/src/Hexalith.Agents/Agent/AgentAggregate.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents/Agent/AgentState.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents/ProviderCatalog/ProviderCatalogAggregate.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/Commands/LinkAgentPartyIdentity.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/Events/AgentPartyIdentityLinked.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Application/Agents/AgentProviderSelectionOrchestrator.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Ports/DeferredProviderCatalogReader.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Ports/DeferredAgentCommandDispatcher.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Program.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Client/Aggregates/EventStoreAggregate.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandEnvelope.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Results/DomainResult.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Identity/AggregateIdentity.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IEventPayloadProtectionService.cs]
- [Source: Hexalith.EventStore/docs/concepts/command-lifecycle.md]
- [Source: Hexalith.Conversations/src/Hexalith.Conversations.Client/IConversationClient.cs]
- [Source: Hexalith.Conversations/_bmad-output/project-context.md]
- [Source: Hexalith.Parties/src/Hexalith.Parties.Contracts/PersonalDataAttribute.cs]
- [Source: Hexalith.Parties/_bmad-output/project-context.md]
- [Source: Hexalith.Tenants/_bmad-output/project-context.md]
- [Source: _bmad-output/implementation-artifacts/1-2-govern-provider-catalog-entries.md#Completion-Notes-List]
- [Source: _bmad-output/implementation-artifacts/1-7-configure-content-safety-policy-and-activation-gate.md]

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created. Story status is `ready-for-dev`.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (claude-opus-4-8, 1M context) — BMAD dev-story workflow.

### Debug Log References

- `dotnet build Hexalith.Agents.slnx --configuration Release` → 0 warnings / 0 errors (`TreatWarningsAsErrors=true`).
- `dotnet test` per project (Release, run individually per the EventStore project-context rule):
  - `Hexalith.Agents.Tests` → 293 passed / 0 failed.
  - `Hexalith.Agents.Contracts.Tests` → 99 passed / 0 failed.
  - `Hexalith.Agents.Server.Tests` → 108 passed / 0 failed.
  - `Hexalith.Agents.UI.Tests` (regression, consumes Contracts) → 156 passed / 0 failed.
  - _(Counts reflect the senior-review re-run on 2026-06-24, including the 23 tests the QA-automation step added after the dev pass: 280→293 / 96→99 / 101→108.)_
- One self-introduced contract test was tightened after first run: `Request_command_has_no_marker_interface_or_attribute` originally asserted zero custom attributes, but the compiler emits `Nullable*Attribute` on a record with nullable members. Re-scoped it to assert no `Hexalith`-namespaced (domain/EventStore) attribute. All other tests passed on first run.

### Completion Notes List

Implemented the `AgentInteraction` request-creation step only (Epic 2 entry point); Stories 2.2–2.6 deferred as scoped.

- **AC1** — `RequestAgentInteraction` → `InteractionRequested` records the AD-4 `AgentInteractionSnapshot` (config/instructions/approver/content-safety versions, response mode, provider/model + capability version, context-policy reference) frozen at request time. The snapshot is server-assembled by `AgentInteractionRequestOrchestrator` from the trusted `IAgentConfigurationSnapshotReader` read (pure aggregate cannot read the `Agent` aggregate — AD-3). `ContentSafetyPolicyVersion` kept as the documented additive extension beyond AD-4's floor.
- **AC2** — orchestrator returns the safe `AgentInteractionReference` (id + coarse status only — no stream name / provider detail). Deterministic id `AgentInteractionIdentity.Derive(tenant, agent, source, caller, idempotencyKey)` = lowercase hex of a length-prefixed SHA-256 (regex-valid, ≤256, colon-free, no ULID/Guid). Re-issue → aggregate `NoOp`; conflicting payload on the same id → `AgentInteractionAlreadyRequestedRejection` (snapshot scalars compared explicitly, ordinal).
- **AC3** — only the explicit command path creates an interaction; the orchestrator touches solely the snapshot reader + command dispatcher — no provider adapter, no `IConversationClient`/Parties client, no ambient subscription. Authorization/dependency gating left to Story 2.2.
- **AC4** — the raw `Prompt` lives only on the durable `InteractionRequested` event and `AgentInteractionState`; it is absent from the status view, the reference, and every rejection (asserted by tests + auto-covered by `ContractsSecretNonDisclosureTests`). Tenant scope flows from the envelope; rejections carry only the id + a safe classification enum.
- **Deferred bindings** — `DeferredAgentConfigurationSnapshotReader` fails closed by **returning** `AgentConfigurationSnapshot.NotAvailable` (per the task's "returns a not-available result"), so an accidental live call yields a clean `MissingAgentSnapshot` rejection rather than a 500; live command dispatch stays the deferred throw-boundary (`DeferredAgentCommandDispatcher`). The aggregate is auto-discovered by the existing `AddEventStoreDomainService` scan (no host change beyond the reader/orchestrator registrations).
- **Reconciliations (noted for review):**
  1. Story Task 1 says "the Agent id … comes from the CommandEnvelope (AggregateId)", but Task 4 sets the envelope `AggregateId = AgentInteractionId` (the deterministic interaction id) under domain `agent-interaction`. Since the aggregate is keyed by the interaction id, the target `AgentId` is carried on `RequestAgentInteraction` as a **server-populated trusted field** (alongside the snapshot), and recorded on the event. This is the only way the pure aggregate can know the target Agent without a cross-aggregate read.
  2. The shared V1 default context-policy reference is defined once as a `public const` on the public contract `AgentInteractionSnapshot.DefaultContextPolicyReference` (re-exported as `AgentInteractionRequestPolicy.DefaultContextPolicyReference` in the domain) so the domain, the orchestrator, and both test assemblies reference a single source and cannot drift — the policy helper stays `internal` per the task.

### File List

**New — Contracts (`src/Hexalith.Agents.Contracts/AgentInteraction/`)**
- `AgentInteractionStatus.cs`
- `AgentInteractionRequestValidationStatus.cs`
- `AgentInteractionSnapshot.cs`
- `AgentInteractionReference.cs`
- `AgentInteractionStatusView.cs`
- `Commands/RequestAgentInteraction.cs`
- `Events/InteractionRequested.cs`
- `Events/Rejections/InvalidAgentInteractionRequestRejection.cs`
- `Events/Rejections/AgentInteractionAlreadyRequestedRejection.cs`
- `Queries/GetAgentInteractionStatusQuery.cs`

**New — Domain (`src/Hexalith.Agents/AgentInteraction/`)**
- `AgentInteractionAggregate.cs`
- `AgentInteractionState.cs`
- `AgentInteractionRequestPolicy.cs`

**New — Server (`src/Hexalith.Agents.Server/`)**
- `Ports/IAgentConfigurationSnapshotReader.cs`
- `Ports/DeferredAgentConfigurationSnapshotReader.cs`
- `Ports/AgentConfigurationSnapshot.cs`
- `Application/AgentInteractions/AgentInteractionRequest.cs` (request + outcome)
- `Application/AgentInteractions/AgentInteractionIdentity.cs`
- `Application/AgentInteractions/AgentInteractionRequestOrchestrator.cs`

**Modified — Server**
- `src/Hexalith.Agents.Server/Program.cs` (register snapshot reader + request orchestrator)

**New — Tests**
- `tests/Hexalith.Agents.Tests/AgentInteractionTestData.cs`
- `tests/Hexalith.Agents.Tests/AgentInteractionAggregateTests.cs`
- `tests/Hexalith.Agents.Tests/AgentInteractionStateReplayTests.cs`
- `tests/Hexalith.Agents.Contracts.Tests/AgentInteractionContractsTests.cs`
- `tests/Hexalith.Agents.Server.Tests/AgentInteractionRequestOrchestratorTests.cs`

**New — Artifacts**
- `_bmad-output/implementation-artifacts/tests/2-1-test-summary.md`
- `_bmad-output/implementation-artifacts/tests/2-1-qa-e2e-test-summary.md` (QA-automation step; added to the File List by the senior review for completeness)

### Senior Developer Review (AI)

**Reviewer:** Administrator (automated adversarial review) — 2026-06-24
**Outcome:** ✅ Approve → status `done`

**Scope of review:** read every file in the File List; cross-referenced the File List against `git status`; audited all 5 tasks marked `[x]` against the implementation; validated all 4 ACs; independently re-ran the build and all four test projects.

**Independently verified:**
- `dotnet build Hexalith.Agents.slnx --configuration Release` → **0 warnings / 0 errors**.
- `Hexalith.Agents.Tests` → **293/0**, `Hexalith.Agents.Contracts.Tests` → **99/0**, `Hexalith.Agents.Server.Tests` → **108/0**, `Hexalith.Agents.UI.Tests` (regression) → **156/0**. Total **656** pass.
- Tests are real assertions (Shouldly), not placeholders — incl. a `[Theory]` exercising every snapshot scalar in the idempotency compare, a rejection-non-disclosure JSON assertion, and a cross-seam orchestrator→aggregate E2E.

**AC validation:** AC1 (AD-4 snapshot frozen on `InteractionRequested`) ✓ · AC2 (safe `AgentInteractionReference`, deterministic-id dedup → `NoOp`/conflict rejection) ✓ · AC3 (only the explicit command path; orchestrator touches only the snapshot reader + dispatcher — no provider/Conversations/Parties call) ✓ · AC4 (raw `Prompt` confined to the durable event/state; absent from view/reference/rejections; `CommandEnvelope`/`EventEnvelope` redact the payload; tenant scope from the envelope) ✓.

**Findings:** 0 Critical, 0 High, 0 Medium.
- 🟢 LOW — stale recorded test counts (280/96/101 → actual 293/99/108); the QA-automation step added 23 tests after the dev pass. **Fixed** (this story file + `2-1-test-summary.md`).
- 🟢 LOW — File List omitted `2-1-qa-e2e-test-summary.md`. **Fixed** (added under Artifacts).
- ⚪ Observation (no change made) — `AgentInteractionStatusView` exposes every snapshot scalar except `ContextPolicyReference`. Defensible: the view is deferred and Story 2.3 reworks context policy; altering the public contract now would be unjustified scope creep.

No production code was changed by this review — the implementation is correct and fully green; only documentation/accuracy items were reconciled.

### Change Log

| Date | Change |
| --- | --- |
| 2026-06-24 | Implemented Story 2.1 (Request `hexa` From A Source Conversation): `AgentInteraction` aggregate/state/policy, the `RequestAgentInteraction` command + `InteractionRequested` event (with AD-4 snapshot) + typed rejections, the safe status reference/view/query, the deterministic-id + snapshot-assembly request orchestration with a deferred snapshot reader, and full domain/contract/server tests. Build 0W/0E; 533 module tests pass (280 domain + 96 contracts + 101 server + 156 UI regression). Status → review. |
| 2026-06-24 | Senior adversarial review (auto-fix): re-verified build 0W/0E and re-ran all test projects — **656** pass (293 domain + 99 contracts + 108 server + 156 UI regression). 0 Critical/High/Medium findings. Reconciled stale test counts and added the QA-E2E artifact to the File List. Status → done. |
