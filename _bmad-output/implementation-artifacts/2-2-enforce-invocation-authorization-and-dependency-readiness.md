---
baseline_commit: 74f05dc9500ea03e82f7a34f5e2ec34db3b0f936
---

# Story 2.2: Enforce Invocation Authorization And Dependency Readiness

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a Conversation Participant,
I want unauthorized or unsafe Agent Calls to be rejected before generation,
so that `hexa` cannot leak tenant data or act when required state is uncertain.

## Acceptance Criteria

**AC1 - Gate every dependency before any provider invocation; fail closed on uncertainty**
**Given** an Agent Call request exists
**When** invocation gating runs
**Then** tenant access, caller Party state, Source Conversation access, Agent lifecycle, Agent Party identity, Provider/model readiness, response policy, Content Safety Policy, and dependency freshness are checked before any provider invocation
**And** missing, stale, ambiguous, disabled, or unavailable state fails closed.

**AC2 - Missing/stale caller conversation access produces a denied/blocked status with no side effects**
**Given** caller access to the Source Conversation is missing or stale
**When** the call is evaluated
**Then** the system records a denied or blocked interaction status
**And** no provider invocation, Proposed Agent Reply, or Conversation Message is created.

**AC3 - Unprovable tenant/Party authorization fails closed without revealing cross-tenant existence**
**Given** tenant access or Party state is unavailable
**When** the system cannot prove authorization
**Then** the request fails closed with a safe structured error
**And** the response does not reveal whether records exist in another tenant.

**AC4 - Safe audit evidence distinguishes failure classes without leaking sensitive data**
**Given** authorization failure occurs
**When** authorized administrators inspect status or audit
**Then** the system exposes enough safe evidence to distinguish authorization, dependency, Agent readiness, Provider readiness, and policy failures
**And** raw claims, tokens, Party personal data, provider payloads, and stack traces are not displayed.

[Source: _bmad-output/planning-artifacts/epics.md#Story-2.2-Enforce-Invocation-Authorization-And-Dependency-Readiness; _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-20-Enforce-Role-And-Policy-Authorization; #FR-21-Fail-Closed-On-Dependency-Uncertainty; #FR-19-Enforce-Tenant-Isolation; #FR-24-Capture-Agent-Audit-Evidence; #FR-12-Prevent-Automatic-Posting-When-Policy-Fails; _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-12-Authorization-And-Dependency-Uncertainty; #AD-3-Pure-Aggregates-Side-Effects-Outside; #AD-14-Sensitive-Content-And-Secret-Safety]

## Tasks / Subtasks

- [x] **Task 1 - Add the safe gate-evidence contracts (classifications, verdict, command, events, query/view)** (AC: #1, #2, #3, #4)
  - [x] Extend `src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionStatus.cs` **additively** — append `Authorized`, `Denied`, `Blocked` after the existing `Unknown = 0, Requested`. Do NOT renumber or reshape existing members (AD-2 additive rule, exactly as `AgentActivationBlocker`/`AgentLifecycleStatus` were extended). `Authorized` = gate passed and the interaction may proceed to context building (Story 2.3); `Denied` = an **authorization-class** check failed (caller not permitted); `Blocked` = a **dependency-readiness-class** check failed (required state missing/stale/ambiguous/disabled/unavailable). Keep the `[JsonConverter(typeof(JsonStringEnumConverter))]` serialize-by-name attribute; `Unknown = 0` stays the sentinel.
  - [x] Add `AgentInteractionGateCheck.cs` (enum) under `AgentInteraction/`: `Unknown = 0, TenantAccess, CallerPartyState, SourceConversationAccess, AgentLifecycle, AgentPartyIdentity, ProviderModelReadiness, ResponsePolicy, ContentSafetyPolicy, DependencyFreshness`. Mirror `AgentActivationBlocker.cs` exactly (serialize-by-name, `Unknown = 0` sentinel, additively extensible, one safe XML doc line per member — never carries raw content, AD-14). These nine values are the AC1 gate inventory in evaluation order.
  - [x] Add `AgentInteractionGateOutcome.cs` (enum) under `AgentInteraction/`: `Unknown = 0, Satisfied, Missing, Stale, Ambiguous, Disabled, Unavailable, Unauthorized`. The five failure values `Missing/Stale/Ambiguous/Disabled/Unavailable` are AD-12's exact fail-closed vocabulary; `Unauthorized` is the access-denied outcome; `Satisfied` means the check passed. Serialize-by-name, `Unknown = 0` sentinel.
  - [x] Add the value object `AgentInvocationGateVerdict.cs` under `AgentInteraction/`: `public record AgentInvocationGateVerdict(AgentInteractionGateCheck Check, AgentInteractionGateOutcome Outcome)`. One verdict per evaluated check. A verdict is a **blocker** iff `Outcome != Satisfied`. Carries only the two safe enums — never claims, tokens, PartyId values, provider payloads, or messages (AD-14, AC4).
  - [x] Add the server-trusted command `EvaluateAgentInteractionGate.cs` under `AgentInteraction/Commands/`: `public record EvaluateAgentInteractionGate(string AgentInteractionId, IReadOnlyList<AgentInvocationGateVerdict> Verdicts)` (plain `public record`, NO base interface, NO attribute — mirror `RequestAgentInteraction.cs`). XML doc MUST state: the `Verdicts` are **server-assembled from trusted dependency reads** by `AgentInteractionGateOrchestrator` (Task 4); the pure aggregate never reads dependencies itself (AD-3); any client-supplied verdict is stripped/overwritten by the orchestrator. The interaction id and tenant come from the `CommandEnvelope` (`AggregateId`/`TenantId`); `AgentInteractionId` on the payload mirrors the request command's redundant-id precedent.
  - [x] Add the durable outcome events under `AgentInteraction/Events/` implementing `IEventPayload` (these RECORD the gate decision as Audit Evidence — FR-24 — so they are success events, NOT `IRejectionEvent`):
    - `AgentInteractionAuthorized.cs`: `public record AgentInteractionAuthorized(string AgentInteractionId) : IEventPayload` — transitions status to `Authorized`.
    - `AgentInteractionGateFailed.cs`: `public record AgentInteractionGateFailed(string AgentInteractionId, AgentInteractionStatus Decision, IReadOnlyList<AgentInvocationGateVerdict> Blockers) : IEventPayload` — `Decision` is `Denied` or `Blocked`; `Blockers` are the non-`Satisfied` verdicts (safe classifications only). This is the durable denied/blocked audit record (AC2, AC4).
  - [x] Add the structural rejection under `AgentInteraction/Events/Rejections/` implementing `IRejectionEvent` (the gate **cannot be evaluated** — distinct from a recorded denied/blocked decision): `AgentInteractionGateNotEvaluableRejection.cs`: `public record AgentInteractionGateNotEvaluableRejection(string AgentInteractionId, AgentInteractionGateNotEvaluableReason Reason) : IRejectionEvent`. Add the enum `AgentInteractionGateNotEvaluableReason.cs`: `Unknown = 0, InteractionNotRequested, NoVerdictsProvided`. Mirror `InvalidAgentInteractionRequestRejection.cs` — carries only the id plus a safe classification enum.
  - [x] Add the safe inspection query/view under `AgentInteraction/Queries/` (read binding deferred, mirroring `GetAgentInteractionStatusQuery.cs`):
    - `GetAgentInteractionGateEvidenceQuery.cs`: `public record GetAgentInteractionGateEvidenceQuery(string AgentInteractionId)`.
    - `AgentInteractionGateInspectionStatus.cs` (enum): `Unknown = 0, Success, NotAuthorized, NotFound` — mirror `AgentInspectionStatus.cs`.
    - `AgentInteractionGateEvidenceView.cs`: `public record AgentInteractionGateEvidenceView(string AgentInteractionId, AgentInteractionStatus Status, IReadOnlyList<AgentInvocationGateVerdict> Verdicts)` — the safe audit view; the `Verdicts` list lets an administrator distinguish authorization vs dependency vs Agent-readiness vs Provider-readiness vs policy failures (AC4) via the `Check` categories. NO prompt, no claims/tokens, no PartyId personal data, no provider payloads, no stack traces.
    - `AgentInteractionGateEvidenceResult.cs`: `public record AgentInteractionGateEvidenceResult(AgentInteractionGateInspectionStatus Status, AgentInteractionGateEvidenceView? Evidence)` with `Success(view)`/`NotAuthorized()`/`NotFound()` factories — mirror `AgentInspectionResult.cs`. On `NotAuthorized`/`NotFound` the view is `null` so a failed inspection NEVER reveals whether the interaction exists in another tenant (AC3, AC4).
  - [x] Keep `Hexalith.Agents.Contracts` inward-only (no sibling-module contract references, no server/provider/Dapr/UI packages). All new identifiers (`AgentInteractionId`, any party/conversation reference) stay opaque `string`s — do NOT import `Hexalith.Conversations.Contracts`/`Hexalith.Parties.Contracts` types. [Source: ARCHITECTURE-SPINE.md#AD-9-Provider-Adapter-And-Catalog-Boundary; 2-1 story Task 1]

- [x] **Task 2 - Implement the pure gate evaluation on the AgentInteraction aggregate + state** (AC: #1, #2, #3, #4)
  - [x] Add a second static handler to `src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs`: `public static DomainResult Handle(EvaluateAgentInteractionGate command, AgentInteractionState? state, CommandEnvelope envelope)`. Keep the existing `Handle(RequestAgentInteraction, …)` unchanged.
  - [x] Guard cascade (mirror the request handler order): `ArgumentNullException.ThrowIfNull(command/envelope)` → read `string interactionId = envelope.AggregateId` → **state precondition**: `state is not { IsRequested: true }` → `DomainResult.Rejection([new AgentInteractionGateNotEvaluableRejection(interactionId, AgentInteractionGateNotEvaluableReason.InteractionNotRequested)])` → **empty verdicts**: `command.Verdicts is null or { Count: 0 }` → `Rejection([... NoVerdictsProvided])` → **idempotent terminal check** → **evaluate**.
  - [x] **Idempotent terminal gate (AD-13):** if `state.Status` is already a terminal gate outcome (`Authorized`, `Denied`, or `Blocked`), return `DomainResult.NoOp()`. The gate decision is recorded **once and is terminal**; the aggregate NEVER silently flips a recorded decision (fail-closed determinism). A re-issued gate command on an already-gated interaction is a clean no-op.
  - [x] **Pure evaluation** via a new internal helper `AgentInvocationGatePolicy.Evaluate(command.Verdicts)`: compute `blockers = Verdicts.Where(v => v.Outcome != AgentInteractionGateOutcome.Satisfied)`. If `blockers` is empty → `DomainResult.Success([new AgentInteractionAuthorized(interactionId)])`. Otherwise classify the decision and → `DomainResult.Success([new AgentInteractionGateFailed(interactionId, decision, blockers)])`.
  - [x] **Decision classification (Denied vs Blocked):** authorization-class checks = `{ TenantAccess, CallerPartyState, SourceConversationAccess }`; readiness-class = the other six. If ANY blocker is authorization-class → `Decision = Denied`; else (only readiness-class blockers) → `Decision = Blocked`. `Denied` takes precedence so a failed-closed authorization is never downgraded to "blocked" (AC2/AC3 more-restrictive rule).
  - [x] Keep the aggregate PURE (AD-3): emit events only. No provider SDK, no Conversations/Parties/Tenants reads, no Dapr, no HTTP, no logging/telemetry, no `DateTimeOffset.UtcNow`, no `Guid.NewGuid`. The verdicts arrive pre-assembled from trusted server reads (Task 4). The aggregate's only job is the deterministic blockers→decision computation.
  - [x] Add `src/Hexalith.Agents/AgentInteraction/AgentInvocationGatePolicy.cs` (internal static, mirror `AgentInteractionRequestPolicy.cs`) holding the pure `Evaluate` + the authorization-class/readiness-class classification. `InternalsVisibleTo` already exposes internals to `Hexalith.Agents.Tests`.
  - [x] Extend `src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs` **additively**: add `public AgentInteractionStatus Status { get; set; } = AgentInteractionStatus.Unknown;` and a safe `public IReadOnlyList<AgentInvocationGateVerdict>? GateVerdicts { get; set; }`. In the existing `Apply(InteractionRequested e)` set `Status = AgentInteractionStatus.Requested`. Add `Apply(AgentInteractionAuthorized e)` → `Status = Authorized`; `Apply(AgentInteractionGateFailed e)` → `Status = e.Decision; GateVerdicts = e.Blockers;`. Add a no-op `Apply(AgentInteractionGateNotEvaluableRejection e)` via the existing `MarkReplayOnlyEventHandled()` helper. Every non-`InteractionRequested` apply keeps the `if (!IsRequested) return;` guard so replay over a stream that begins before the request stays total.
  - [x] Do NOT add any new snapshot fields. The AD-4 `AgentInteractionSnapshot` frozen at request time is unchanged; the gate reads CURRENT dependency readiness (Task 4), not the snapshot, because AD-12 readiness is "current dependency availability." The snapshot's frozen config/policy versions are recorded for audit, not re-evaluated here.

- [x] **Task 3 - Add the server-side gate-readiness ports (deferred, fail-closed)** (AC: #1, #3)
  - [x] Add `src/Hexalith.Agents.Server/Ports/ITenantAccessReader.cs` → `Task<TenantAccessReadResult> ReadAsync(string tenantId, string actorUserId, string callerPartyId, CancellationToken ct)`. `TenantAccessReadResult` carries an `AgentInteractionGateOutcome` (and optional freshness/projection-version metadata for the `DependencyFreshness` check). Per AD-12, tenant access comes from **Agents' own local Tenants projection** — NOT Conversations' server-internal `IConversationTenantAccessService` (Agents must not depend on `Hexalith.Conversations.Server`). Add `DeferredTenantAccessReader` returning a fail-closed `Unavailable` result (mirror `DeferredAgentConfigurationSnapshotReader.cs`'s return-not-available shape).
  - [x] Add `src/Hexalith.Agents.Server/Ports/IConversationAccessReader.cs` → `Task<ConversationAccessReadResult> ReadAsync(string tenantId, string sourceConversationId, string callerPartyId, CancellationToken ct)`. It is the Agents-owned wrapper over the Conversations **public** authorized read seam `IConversationClient.GetConversationAsync(...)` — it checks (a) the caller participates with sufficient role and (b) the conversation loaded fresh enough. Result carries an `AgentInteractionGateOutcome` + freshness metadata. Add `DeferredConversationAccessReader` returning fail-closed `Unavailable`. Do NOT wire `IConversationClient` live in this story (Story 2.3 wires the live context read).
  - [x] Add `src/Hexalith.Agents.Server/Ports/IAgentInvocationReadinessReader.cs` → `Task<AgentInvocationReadiness> ReadAsync(string tenantId, string agentId, CancellationToken ct)`. `AgentInvocationReadiness` exposes the CURRENT Agent state needed for the readiness-class checks: `AgentLifecycleStatus Lifecycle`, `bool HasPartyIdentity` + `string? PartyId`, `AgentResponseMode ResponseMode`, `bool HasActiveContentSafetyPolicy`, `string? ProviderId`, `string? ModelId`, plus an `IsAvailable` flag + freshness metadata. Add `DeferredAgentInvocationReadinessReader` returning a fail-closed not-available result. (This is the current-state analogue of Story 2.1's `IAgentConfigurationSnapshotReader`; keep the two distinct — one freezes config at request time, this one reads live readiness at gate time.)
  - [x] **Reuse — do NOT re-create:** `IAgentPartyDirectory.ValidateExistingPartyAsync(string tenantId, string partyId, CancellationToken ct)` already exists and returns `AgentPartyValidationResult` — use it for BOTH `CallerPartyState` (validate `CallerPartyId`) and `AgentPartyIdentity` (validate the Agent's linked `PartyId`). `IProviderCatalogReader.GetEntryAsync(tenantId, providerId, modelId, ct)` already exists and returns `ProviderCatalogEntryReadResult` — use it for `ProviderModelReadiness`. `IApproverPolicyResolver.ResolveAsync(...)` already exists — use it for the Confirmation-mode branch of `ResponsePolicy`. All three are already registered as `Deferred*` in `Program.cs`; add no duplicates. [Source: as-built `Hexalith.Agents.Server/Ports/` — IAgentPartyDirectory, IProviderCatalogReader, IApproverPolicyResolver]
  - [x] Map each port/reader result to an `AgentInteractionGateOutcome` using the **Gate Evaluation Rules** table in Dev Notes. Any reader that returns not-available OR throws maps to `Unavailable` (fail closed, AD-12) — never propagate the raw exception (AD-14: no stack traces/payloads leak).

- [x] **Task 4 - Implement the gate orchestration (trusted verdict assembly, no side effects)** (AC: #1, #2, #3, #4)
  - [x] Add `src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionGateOrchestrator.cs` (mirror the thin shape of `AgentInteractionRequestOrchestrator.cs`). Constructor injects the three new readers + the three reused ports (`ITenantAccessReader`, `IConversationAccessReader`, `IAgentInvocationReadinessReader`, `IAgentPartyDirectory`, `IProviderCatalogReader`, `IApproverPolicyResolver`) and `IAgentCommandDispatcher`.
  - [x] `ExecuteAsync(AgentInteractionGateRequest request, CancellationToken ct)`: (1) read every dependency through its port; (2) map each to an `AgentInvocationGateVerdict` (one per `AgentInteractionGateCheck`, evaluation order = the enum order); (3) wrap each read in fail-closed exception handling → `Unavailable` verdict on throw/not-available; (4) set the `DependencyFreshness` verdict to `Stale` when any consulted projection is behind its freshness threshold, else `Satisfied`; (5) assemble the server-trusted `EvaluateAgentInteractionGate(interactionId, verdicts)` — any client-supplied verdict value is discarded; (6) build the `CommandEnvelope` with `AggregateId = AgentInteractionId`, `Domain = "agent-interaction"`, reserved client extensions stripped via the established `BuildTrustedExtensions`/reserved-key pattern; (7) dispatch via `IAgentCommandDispatcher` (live dispatch stays deferred behind `DeferredAgentCommandDispatcher`); (8) return a safe `AgentInteractionGateOutcomeResult` (interaction id + the decided `AgentInteractionStatus` computed from the same pure rule the aggregate uses — reuse `AgentInvocationGatePolicy` so orchestrator and aggregate cannot drift).
  - [x] Add `Application/AgentInteractions/AgentInteractionGateRequest.cs` (request + `AgentInteractionGateOutcomeResult` outcome record), mirroring `AgentInteractionRequest.cs`. The request carries `TenantId`, `AgentInteractionId`, the snapshot-recorded `AgentId`/`CallerPartyId`/`SourceConversationId`/`ProviderId`/`ModelId`/`ResponseMode` needed to drive the reads (these come from the interaction's recorded `InteractionRequested`/snapshot — passed in by the caller of the gate step), `ActorUserId`, and the message/correlation ids.
  - [x] **No side effects (AC2):** the orchestration MUST NOT call any provider adapter (provider READINESS is a catalog/projection read, never a model invocation), MUST NOT post to Conversations (`AppendMessageAsync`), and MUST NOT create a Proposed Agent Reply. It only reads dependency state and dispatches the gate command. Generation is Story 2.4; posting is Story 2.5.
  - [x] Register the new orchestrator + the three new readers (`ITenantAccessReader`/`DeferredTenantAccessReader`, `IConversationAccessReader`/`DeferredConversationAccessReader`, `IAgentInvocationReadinessReader`/`DeferredAgentInvocationReadinessReader`) in `src/Hexalith.Agents.Server/Program.cs` alongside the existing deferred registrations. The new aggregate handler needs no host change — it is auto-discovered by the existing `AddEventStoreDomainService(typeof(AgentsAssemblyMarker).Assembly, …)` scan.

- [x] **Task 5 - Add focused tests and run the narrow verification** (AC: #1, #2, #3, #4)
  - [x] **Domain tests** (`tests/Hexalith.Agents.Tests/`): extend `AgentInteractionTestData.cs` with verdict builders (`Verdict(check, outcome)`, `AllSatisfied()`, `StateRequested(...)` driving the real `Apply(InteractionRequested)`). Add `AgentInteractionGateAggregateTests.cs` + extend `AgentInteractionStateReplayTests.cs` covering: all-satisfied verdicts → `AgentInteractionAuthorized` + state `Authorized`; each authorization-class blocker (`TenantAccess`/`CallerPartyState`/`SourceConversationAccess`) with each AD-12 outcome (`Missing`/`Stale`/`Ambiguous`/`Disabled`/`Unavailable`/`Unauthorized`) → `AgentInteractionGateFailed(Denied, blockers)`; each readiness-class blocker → `AgentInteractionGateFailed(Blocked, blockers)`; mixed authorization+readiness blockers → `Denied` precedence; gate command on a not-yet-requested aggregate → `AgentInteractionGateNotEvaluableRejection(InteractionNotRequested)`; empty verdicts → `…(NoVerdictsProvided)`; idempotent re-evaluation on an already-gated interaction → `NoOp` (decision never flips); replay determinism over a stream containing request + gate events; persisted `AgentInteractionGateNotEvaluableRejection` is a replay-safe no-op; full reflection-dispatch + JSON round-trip via `ProcessAsync`.
  - [x] **Contract tests** (`tests/Hexalith.Agents.Contracts.Tests/`): System.Text.Json round-trip for `EvaluateAgentInteractionGate`, `AgentInteractionAuthorized`, `AgentInteractionGateFailed`, `AgentInteractionGateNotEvaluableRejection`, `AgentInvocationGateVerdict`, the new enums, the query, the evidence view + result; marker conformance (`AgentInteractionAuthorized`/`AgentInteractionGateFailed` are `IEventPayload`; `AgentInteractionGateNotEvaluableRejection` is `IRejectionEvent`); enum serialize-by-name + `Unknown` fallback. The assembly-wide `ContractsSecretNonDisclosureTests` auto-covers the new types — confirm no member trips the forbidden secret/PII tokens. Explicitly assert: the verdict/evidence types carry ONLY the two safe enums (no prompt, claims, tokens, PartyId, provider payload, or message string), and `AgentInteractionGateEvidenceResult.NotAuthorized()`/`NotFound()` carry a `null` view (AC3, AC4).
  - [x] **Server tests** (`tests/Hexalith.Agents.Server.Tests/`): `AgentInteractionGateOrchestratorTests.cs` with NSubstitute stubs proving (a) all-ready stubs → all-`Satisfied` verdicts → dispatched `EvaluateAgentInteractionGate` + returned `Authorized`; (b) each reader's denial/disabled/missing/stale result maps to the correct `(Check, Outcome)` verdict and the correct `Denied`/`Blocked` decision; (c) a reader that THROWS or returns not-available → fail-closed `Unavailable` verdict (never an unhandled exception, no raw error text in the outcome — AD-14); (d) client-supplied verdict values are discarded and only server-read verdicts are dispatched; (e) reserved client-supplied extensions are stripped; (f) **no provider adapter is invoked and no `IConversationClient.AppendMessageAsync`/proposal is created**; (g) with ALL ports left as their `Deferred*` defaults the gate fails closed to `Denied` (tenant access `Unavailable` is authorization-class). The structural guard tests (`StructuralSeedConformanceTests`, `ProjectReferenceDirectionTests`, `PublicContractPackageBoundaryTests`, `ModuleLayout`, `PackageVersionCentralizationTests`, `BuildContractConformanceTests`) must stay green.
  - [x] Keep xUnit v3 + Shouldly (no raw `Assert.*`); NSubstitute only outside aggregate logic; aggregate tests are pure command/state/event tests. Test method names follow the surrounding files' style.
  - [x] Write a test-summary artifact to `_bmad-output/implementation-artifacts/tests/2-2-test-summary.md` and include it in the File List (matches the prior-story convention).
  - [x] Run from `/home/administrator/projects/hexalith/agents/Hexalith.Agents`:
    - `dotnet restore Hexalith.Agents.slnx`
    - `dotnet build Hexalith.Agents.slnx --configuration Release` (must be 0 warnings / 0 errors — `TreatWarningsAsErrors=true`)
    - `dotnet test tests/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj --configuration Release`
    - `dotnet test tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release`
    - `dotnet test tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj --configuration Release`

## Dev Notes

### Critical Guardrails

- This story is the **invocation authorization + dependency-readiness GATE step ONLY**, layered onto the `AgentInteraction` aggregate created in Story 2.1. Implement: the gate command + the `Authorized`/`GateFailed` outcome events + the `NotEvaluable` rejection, the safe verdict/evidence contracts, the pure aggregate gate evaluation + state transition, the server gate orchestration (trusted verdict assembly via deferred readers), and tests. Do **NOT** implement: Conversation context building/content loading (Story 2.3 — this story checks conversation *access + freshness* only, not content load), provider generation + safety-check execution (Story 2.4 — this story checks provider *readiness*, never invokes a model), automatic posting to Conversations (Story 2.5), or invocation UX/status UI (Story 2.6). [Source: epics.md#Epic-2-Safe-Conversation-Invocation-And-Automatic-Replies]
- **AD-12 is the heart of this story.** "Authorization gates run before every side effect and fail closed on missing, stale, ambiguous, disabled, or unavailable dependency state. Tenant access comes from Agents' local Tenants projection; conversation access/context from Conversations authorized queries; Party state from Parties adapters/projections; provider/model readiness from ProviderCatalog projections; approver rights from the AgentInteraction policy snapshot plus current dependency availability." Every one of the nine `AgentInteractionGateCheck` values traces to that sentence. [Source: ARCHITECTURE-SPINE.md#AD-12-Authorization-And-Dependency-Uncertainty; PRD FR-20, FR-21]
- **Pure aggregate, side effects outside (AD-3).** All dependency reads happen in the Server gate orchestration/adapters and feed back through the trusted `EvaluateAgentInteractionGate` command. The aggregate does the deterministic blockers→decision math only. This is the SAME shape as the Agent activation gate, where `Handle(ActivateAgent)` reads verdicts (`ReadProviderSelectionValidation`/`ReadApproverPolicyValidation`/`ReadPartyLinkValidation`) the orchestration supplied and computes `ComputeActivationBlockers(...)`. [Source: ARCHITECTURE-SPINE.md#AD-3-Pure-Aggregates-Side-Effects-Outside; Hexalith.Agents/src/Hexalith.Agents/Agent/AgentAggregate.cs:239-252, :576-603]
- **Denied/Blocked is a recorded SUCCESS outcome, NOT an `IRejectionEvent`.** The interaction durably transitions to `Denied`/`Blocked` with safe blocker evidence — that record IS the Audit Evidence (FR-24, AC4). Reserve `IRejectionEvent` (`AgentInteractionGateNotEvaluableRejection`) for the case where the gate cannot be evaluated at all (interaction not requested / no verdicts) — that produces no state change, exactly like `AgentActivationBlockedRejection` leaves the Agent in `Draft`. Do not conflate the two: a denied call is a successfully-recorded negative decision; an unevaluable command is a rejection. [Source: ARCHITECTURE-SPINE.md#AD-2-Aggregate-Boundaries; #AD-5-Proposal-Lifecycle; Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Results/DomainResult.cs]
- **Fail closed everywhere (FR-21).** Missing/stale/ambiguous/disabled/unavailable → a blocking verdict, never a pass. A reader that throws or returns not-available → `Unavailable` verdict. With all live bindings deferred, every verdict is `Unavailable` and the gate decides `Denied` — the correct safe default until the real projections are wired. [Source: PRD FR-21-Fail-Closed-On-Dependency-Uncertainty; ARCHITECTURE-SPINE.md#AD-12]
- **No cross-tenant disclosure (AC3, FR-19).** Tenant scope comes from the envelope; the aggregate only ever sees its own tenant's stream (structural disjointness — colons forbidden in identity). The evidence inspection returns a `null` view on `NotAuthorized`/`NotFound` so a probe cannot learn whether an interaction exists in another tenant. Verdict outcomes are coarse safe enums — `Unavailable` is returned identically whether a record is absent or cross-tenant. [Source: ARCHITECTURE-SPINE.md#AD-12; PRD FR-19-Enforce-Tenant-Isolation; Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/AgentInspectionResult.cs]
- **Sensitive content & safe errors (AD-14).** Verdicts and evidence carry ONLY `AgentInteractionGateCheck` + `AgentInteractionGateOutcome` enums. Never put raw claims, JWT/tokens, PartyId personal data, provider payloads, configured policy values, or stack traces on any event/view/log. The `Prompt` (on the Story-2.1 `InteractionRequested` event/state) is untouched and never copied onto a gate event. [Source: ARCHITECTURE-SPINE.md#AD-14-Sensitive-Content-And-Secret-Safety; PRD FR-12, FR-24]
- **Idempotency & determinism (AD-13).** The gate decision is recorded once and is terminal — re-evaluation is a `NoOp`; the decision never silently flips. Command-layer dedup by `CausationId`/`MessageId` is automatic (EventStore actor) — do not re-implement. [Source: ARCHITECTURE-SPINE.md#AD-13-Idempotent-External-Effects; Hexalith.EventStore/docs/concepts/command-lifecycle.md]

### Design: Where Each Responsibility Lives

```
(durable owner / gate step — AD-18; live wiring deferred)
        │  AgentInteractionGateRequest (interactionId + snapshot-recorded ids, ActorUserId)
        ▼
Hexalith.Agents.Server — AgentInteractionGateOrchestrator            [impure: reads dependencies, dispatches]
        │  reads (each fail-closed → Unavailable on throw/not-available):
        │   • ITenantAccessReader                → TenantAccess           (Agents-owned local Tenants projection — AD-12)
        │   • IAgentPartyDirectory(callerPartyId)→ CallerPartyState       [REUSE]
        │   • IConversationAccessReader          → SourceConversationAccess (wraps IConversationClient.GetConversationAsync)
        │   • IAgentInvocationReadinessReader    → AgentLifecycle, ResponsePolicy, ContentSafetyPolicy + provider/model ids
        │   • IAgentPartyDirectory(agentPartyId) → AgentPartyIdentity      [REUSE]
        │   • IProviderCatalogReader             → ProviderModelReadiness  [REUSE]
        │   • IApproverPolicyResolver            → ResponsePolicy (Confirmation branch) [REUSE]
        │   • (freshness of consulted projections)→ DependencyFreshness
        │  → assemble trusted EvaluateAgentInteractionGate(interactionId, verdicts[])
        │  → CommandEnvelope(AggregateId=interactionId, Domain="agent-interaction"); strip reserved client extensions
        │  → IAgentCommandDispatcher.Dispatch(...)   [DeferredAgentCommandDispatcher — no live dispatch yet]
        │  → return AgentInteractionGateOutcomeResult(id, status)   ← safe; status via shared AgentInvocationGatePolicy
        ▼
Hexalith.Agents — AgentInteractionAggregate.Handle(EvaluateAgentInteractionGate, state, envelope)   [pure: events only]
        │  guards → not-requested/empty-verdicts rejection → terminal NoOp →
        │  AgentInvocationGatePolicy.Evaluate(verdicts):
        │     blockers = verdicts where Outcome != Satisfied
        │     none      → DomainResult.Success([AgentInteractionAuthorized])
        │     authz-class present → Success([AgentInteractionGateFailed(Denied,  blockers)])
        │     else (readiness only) → Success([AgentInteractionGateFailed(Blocked, blockers)])
        ▼
AgentInteractionState.Apply(...)   [pure: in-place, replay-safe]   Status → Authorized | Denied | Blocked ; GateVerdicts recorded
```

No provider model invocation, no Conversations post, no proposal anywhere in this flow. Context building (2.3), generation (2.4), and posting (2.5) attach to the same aggregate later, gated on `Status == Authorized`.

### Current Code State To Preserve

Read these files completely before editing; they are the exact templates to mirror (do not reinvent):

- `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs` — the Story-2.1 `Handle(RequestAgentInteraction, …)`: guard cascade order, `RequestMatchesExisting`/`SnapshotsEqual` idempotency, pure events-only shape. Add the second `Handle(EvaluateAgentInteractionGate, …)` beside it.
- `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs` — `IsRequested` flag, `Apply(InteractionRequested)`, the `MarkReplayOnlyEventHandled()` no-op pattern, the `if (!IsRequested) return;` guard. Extend additively with `Status` + `GateVerdicts` and the three new `Apply` overloads.
- `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionRequestPolicy.cs` — the internal-static pure-helper shape and the `DefaultContextPolicyReference` const re-export. `AgentInvocationGatePolicy.cs` is its sibling.
- `Hexalith.Agents/src/Hexalith.Agents/Agent/AgentAggregate.cs` — **the authoritative gate precedent**: `Handle(ActivateAgent)` at `:205-252` reads orchestration-supplied verdicts and calls `ComputeActivationBlockers(...)` returning a blocker list → `AgentActivationBlockedRejection(agentId, blockers)`; the hardened verdict readers `ReadPartyLinkValidation`/`ReadProviderSelectionValidation`/`ReadApproverPolicyValidation` at `:576-603` (case-sensitive enum parse, fail-closed to `Unknown`); the `Denied(...)`/`Invalid(...)` rejection factories at `:812-816`. The gate's verdict-driven, fail-closed-to-Unknown evaluation copies this exactly.
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/AgentActivationBlocker.cs` — the exact additive-enum + safe-classification shape to copy for `AgentInteractionGateCheck`.
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/AgentInspectionResult.cs` + `AgentInspectionStatus.cs` — the exact "fail-closed inspection result with a `null` view on NotAuthorized/NotFound" shape to copy for `AgentInteractionGateEvidenceResult`/`…InspectionStatus`.
- `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionRequestOrchestrator.cs` — the thin orchestrator shape, reserved-extension stripping (`_reservedExtensionKeys`), envelope build, deferred dispatch. `AgentInteractionGateOrchestrator` mirrors it with more readers.
- `Hexalith.Agents/src/Hexalith.Agents.Server/Ports/` — `IAgentConfigurationSnapshotReader` + `DeferredAgentConfigurationSnapshotReader` (return-not-available deferral shape to copy), and the EXISTING reuse ports `IAgentPartyDirectory` (`ValidateExistingPartyAsync`/`AgentPartyValidationResult`), `IProviderCatalogReader` (`GetEntryAsync`/`ProviderCatalogEntryReadResult`), `IApproverPolicyResolver`. Do NOT duplicate these.
- `Hexalith.Agents/src/Hexalith.Agents.Server/Program.cs` — canonical host; add the three new reader registrations + the gate orchestrator alongside the existing deferred ones. The new aggregate handler is auto-discovered (no host change for it).
- `Hexalith.Agents/tests/Hexalith.Agents.Tests/AgentInteractionTestData.cs` / `AgentInteractionAggregateTests.cs` / `AgentInteractionStateReplayTests.cs`, `Hexalith.Agents.Contracts.Tests/AgentInteractionContractsTests.cs`, `Hexalith.Agents.Server.Tests/AgentInteractionRequestOrchestratorTests.cs` — fixture builders and the snake_case `[Fact]` Shouldly style to extend.

What must be preserved:

- `.slnx` only, `net10.0`, `LangVersion 14`, nullable, implicit usings, **warnings as errors**, Central Package Management, no inline package versions. No new packages — the gate needs none. [Source: Hexalith.Agents/Directory.Build.props; Directory.Packages.props; global.json (SDK 10.0.301)]
- Aggregate purity and replay-safety: no I/O, no provider/dependency reads, no wall-clock, no direct state mutation outside `Apply`, no-op `Apply` for every rejection. [Source: ARCHITECTURE-SPINE.md#AD-3; AgentInteractionState.cs]
- Contracts stay provider-SDK-free, secret-value-free, PII-free, and free of sibling-module contract references. The `AgentInteractionStatus` extension is additive — existing ordinals `Unknown = 0`, `Requested` are untouched so Story 2.1 round-trip/replay tests stay green. [Source: Story 1.1/1.2 guard tests; ARCHITECTURE-SPINE.md#AD-9]
- Sibling Hexalith modules referenced as **source `ProjectReference`s** via the `$(HexalithEventStoreRoot)` etc. discovery blocks — never converted to NuGet, and Agents never references `Hexalith.Conversations.Server`/`…Parties.Server`/`…Tenants.Server` (server internals). Use only the public `*.Client`/`*.Contracts` seams. [Source: Hexalith.Agents/Directory.Build.props; ARCHITECTURE-SPINE.md#AD-6-Conversations-Boundary]

### Gate Evaluation Rules (AC1 — nine checks, fail closed)

The orchestrator produces exactly one verdict per check (enum order). The aggregate blocks on any `Outcome != Satisfied`. **Class** drives the Denied-vs-Blocked decision.

| `AgentInteractionGateCheck` | Class | Source (port) | `Satisfied` when | Blocking outcomes (AD-12) |
| --- | --- | --- | --- | --- |
| `TenantAccess` | authorization | `ITenantAccessReader` (Agents local Tenants projection) | caller principal has required tenant role | `Unauthorized` (denied) · `Stale`/`Unavailable` (projection behind/missing) |
| `CallerPartyState` | authorization | `IAgentPartyDirectory.ValidateExistingPartyAsync(callerPartyId)` [REUSE] | caller Party exists & active | `Missing`·`Disabled`·`Ambiguous`·`Unavailable` |
| `SourceConversationAccess` | authorization | `IConversationAccessReader` (wraps `IConversationClient.GetConversationAsync`) | caller participates w/ sufficient role AND conversation loaded fresh | `Unauthorized`·`Stale`·`Unavailable` |
| `AgentLifecycle` | readiness | `IAgentInvocationReadinessReader` | `Lifecycle == Active` | `Disabled` (Disabled) · `Missing` (Draft/Unknown) · `Unavailable` |
| `AgentPartyIdentity` | readiness | `IAgentInvocationReadinessReader` + `IAgentPartyDirectory.ValidateExistingPartyAsync(agentPartyId)` [REUSE] | linked Party present & valid (AD-7) | `Missing`·`Disabled`·`Ambiguous`·`Unavailable` |
| `ProviderModelReadiness` | readiness | `IProviderCatalogReader.GetEntryAsync` [REUSE] | provider+model entry enabled/ready | `Disabled`·`Missing`·`Unavailable` |
| `ResponsePolicy` | readiness | `IAgentInvocationReadinessReader` (+ `IApproverPolicyResolver` for Confirmation) | `ResponseMode` set; if Confirmation, approver policy resolvable | `Missing` (no mode) · `Ambiguous`/`Unavailable` (approver unresolvable) |
| `ContentSafetyPolicy` | readiness | `IAgentInvocationReadinessReader` | active Content Safety Policy configured | `Missing`·`Unavailable` |
| `DependencyFreshness` | readiness | freshness metadata of all consulted projections | every consulted projection within its freshness threshold | `Stale`·`Unavailable` |

Decision: any authorization-class blocker → **`Denied`**; else any readiness-class blocker → **`Blocked`**; none → **`Authorized`**. (The same `AgentInvocationGatePolicy` computes this for both the aggregate and the orchestrator's returned status so they cannot drift.)

### Server Gate Ports (new + reused)

| Port | New/Reuse | Method | Deferred impl | Live binding deferred to |
| --- | --- | --- | --- | --- |
| `ITenantAccessReader` | NEW | `ReadAsync(tenantId, actorUserId, callerPartyId, ct)` → `TenantAccessReadResult` | `DeferredTenantAccessReader` → `Unavailable` | Agents local Tenants projection story |
| `IConversationAccessReader` | NEW | `ReadAsync(tenantId, sourceConversationId, callerPartyId, ct)` → `ConversationAccessReadResult` | `DeferredConversationAccessReader` → `Unavailable` | Story 2.3 live Conversations read |
| `IAgentInvocationReadinessReader` | NEW | `ReadAsync(tenantId, agentId, ct)` → `AgentInvocationReadiness` | `DeferredAgentInvocationReadinessReader` → not-available | Agent read-model binding (Story 1.2 lineage) |
| `IAgentPartyDirectory` | REUSE | `ValidateExistingPartyAsync(tenantId, partyId, ct)` → `AgentPartyValidationResult` | existing registration | existing |
| `IProviderCatalogReader` | REUSE | `GetEntryAsync(tenantId, providerId, modelId, ct)` → `ProviderCatalogEntryReadResult` | `DeferredProviderCatalogReader` | existing |
| `IApproverPolicyResolver` | REUSE | `ResolveAsync(tenantId, policy, ct)` → `ApproverPolicyResolutionResult` | `DeferredApproverPolicyResolver` | existing |

Deferred convention (mandatory, mirrors Stories 1.2–2.1): live external bindings stay deferred so the DI graph is complete and unit-testable; the gate decision logic + verdict assembly are implemented and tested NOW against NSubstitute stubs. A deferred reader fails closed by **returning** an `Unavailable`/not-available result (snapshot-reader precedent), so an accidental production call yields a clean `Denied` gate rather than a 500. [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Ports/DeferredAgentConfigurationSnapshotReader.cs; _bmad-output/implementation-artifacts/2-1-request-hexa-from-a-source-conversation.md#Deferred-Binding-Convention]

### Cross-Module Boundaries (referenced, not called live, in this story)

- **Tenants:** `TenantId` is a meaningful caller-supplied string on the envelope (not a ULID/GUID) — never parsed. AD-12 mandates tenant access from **Agents' own local Tenants projection**, which Agents builds by projecting Tenants events (the projection itself is deferred). Do NOT depend on `Hexalith.Conversations.Server.TenantAccess.IConversationTenantAccessService` — that is Conversations' private gate; Agents owns its own. Tenant roles (`TenantReader`/`TenantContributor`/`TenantOwner`) and `TenantStatus` (`Active`/`Disabled`) are the projection inputs the reader maps to `TenantAccess` outcomes. [Source: Hexalith.Tenants/_bmad-output/project-context.md; ARCHITECTURE-SPINE.md#AD-12]
- **Conversations:** access is verified through the PUBLIC `IConversationClient.GetConversationAsync(...)` authorized read seam (returns conversation detail + participants + freshness); `ConversationId` is opaque on the Agents side (a `string`, not the Conversations `record ConversationId(string Value)`). The AI author participant is `ParticipantType.AiAgent` (wire `"AIAgent"`) and the V1 conversation authority is `ParticipantRole.Facilitator` — relevant when the access reader checks the caller's role, and reused by Story 2.5 posting. No live `GetConversationAsync` call in THIS story (the reader is deferred); Story 2.3 wires it. [Source: Hexalith.Conversations/src/Hexalith.Conversations.Client/IConversationClient.cs; Contracts/Participants/ParticipantType.cs, ParticipantRole.cs; ARCHITECTURE-SPINE.md#AD-6; #AD-8]
- **Parties:** `PartyId` is GUID-validated in Parties but stored as an opaque `string` on the Agents side (never parsed, never carrying name/contact). Caller and Agent Party validity both go through `IAgentPartyDirectory.ValidateExistingPartyAsync`. Personal data is `[PersonalData]`/GDPR-protected and must NEVER appear on a gate verdict/event/view (AC4). [Source: Hexalith.Parties/_bmad-output/project-context.md; Hexalith.Parties/src/Hexalith.Parties.Contracts/PersonalDataAttribute.cs; ARCHITECTURE-SPINE.md#AD-7]
- **EventStore:** `CommandEnvelope.ToString()`/`EventEnvelope.ToString()` redact payloads with `[REDACTED]`; the gate adds no content-bearing fields anyway (only safe enums). `AggregateId` regex `^[a-zA-Z0-9]([a-zA-Z0-9._-]*[a-zA-Z0-9])?$`, ≤256, no colons — the gate command reuses the Story-2.1 deterministic `AgentInteractionId` as `AggregateId` (no new id derivation). `DomainResult.Success`/`Rejection`/`NoOp` are the only outcome shapes; a result cannot mix events and rejections. [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandEnvelope.cs; Identity/AggregateIdentity.cs; Results/DomainResult.cs; Hexalith.EventStore/_bmad-output/project-context.md]

### Latest Technical Information

- No new packages. The gate is pure domain + orchestration over existing ports; provider SDK / Agent Framework packages remain out of scope (provider INVOCATION is Story 2.4 — this story performs a catalog/projection READINESS read only). xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0` centrally managed; SDK pinned `10.0.301` (`rollForward: latestPatch`). [Source: Hexalith.Agents/Directory.Packages.props; global.json; ARCHITECTURE-SPINE.md#Stack; #Deferred]
- AD-18 durable-owner runtime (Microsoft Agent Framework workflow / Dapr Workflow) is the eventual caller of the gate step before provider invocation; its live topology stays deferred (as Story 2.1 deferred the AppHost). The gate orchestrator is implemented + unit-tested standalone now. [Source: ARCHITECTURE-SPINE.md#AD-18-Hybrid-Agent-Runtime-Ownership; #Deferred]

### Testing Requirements

- **Aggregate** (`Hexalith.Agents.Tests`): all-satisfied → `AgentInteractionAuthorized`/state `Authorized`; per authorization-class blocker → `AgentInteractionGateFailed(Denied)`; per readiness-class blocker → `AgentInteractionGateFailed(Blocked)`; mixed → `Denied` precedence; not-requested → `AgentInteractionGateNotEvaluableRejection(InteractionNotRequested)`; empty verdicts → `…(NoVerdictsProvided)`; idempotent re-evaluation on a gated interaction → `NoOp` (no decision flip); replay determinism over request+gate streams; persisted rejection replay-safe no-op; full `ProcessAsync` reflection-dispatch + JSON round-trip.
- **Contracts** (`Hexalith.Agents.Contracts.Tests`): round-trip all new types; marker conformance (outcome events `IEventPayload`, rejection `IRejectionEvent`); enum serialize-by-name + `Unknown` fallback; secret/PII guard stays green; explicit assertion that verdict/evidence types expose ONLY the safe enums and that the evidence result view is `null` on NotAuthorized/NotFound; `AgentInteractionStatus` additive extension does not perturb existing `Unknown`/`Requested` round-trips.
- **Server** (`Hexalith.Agents.Server.Tests`): per-check verdict mapping; fail-closed on reader throw/not-available → `Unavailable`; client-supplied verdicts discarded; reserved extensions stripped; all-deferred → `Denied`; **no provider invocation, no Conversations post, no proposal**; safe outcome (no stream names/provider detail/raw error); structural guard tests remain green.
- Build/test loop (run from `Hexalith.Agents/`): `dotnet restore Hexalith.Agents.slnx`; `dotnet build Hexalith.Agents.slnx --configuration Release` (0W/0E); then `dotnet test` each of `Hexalith.Agents.Tests`, `Hexalith.Agents.Contracts.Tests`, `Hexalith.Agents.Server.Tests` individually `--configuration Release`. Do not run solution-wide `dotnet test`. (Regression: `Hexalith.Agents.UI.Tests` consumes Contracts — confirm it still builds/passes after the additive enum change.)

### Project Structure Notes

New/changed code (no new projects — extend existing `Hexalith.Agents.Contracts`, `Hexalith.Agents`, `Hexalith.Agents.Server`, and the three test projects):

- `src/Hexalith.Agents.Contracts/AgentInteraction/` — `AgentInteractionGateCheck`, `AgentInteractionGateOutcome`, `AgentInvocationGateVerdict`, `AgentInteractionGateNotEvaluableReason`, `AgentInteractionGateInspectionStatus` (value objects/enums); `Commands/EvaluateAgentInteractionGate`; `Events/AgentInteractionAuthorized`, `Events/AgentInteractionGateFailed`; `Events/Rejections/AgentInteractionGateNotEvaluableRejection`; `Queries/GetAgentInteractionGateEvidenceQuery`, `Queries/AgentInteractionGateEvidenceView`, `Queries/AgentInteractionGateEvidenceResult`; **modify** `AgentInteractionStatus` (append `Authorized`/`Denied`/`Blocked`).
- `src/Hexalith.Agents/AgentInteraction/` — **modify** `AgentInteractionAggregate` (add gate `Handle`), `AgentInteractionState` (add `Status`/`GateVerdicts` + 3 `Apply`); **new** `AgentInvocationGatePolicy`.
- `src/Hexalith.Agents.Server/Ports/` — **new** `ITenantAccessReader`/`DeferredTenantAccessReader`/`TenantAccessReadResult`, `IConversationAccessReader`/`DeferredConversationAccessReader`/`ConversationAccessReadResult`, `IAgentInvocationReadinessReader`/`DeferredAgentInvocationReadinessReader`/`AgentInvocationReadiness`.
- `src/Hexalith.Agents.Server/Application/AgentInteractions/` — **new** `AgentInteractionGateOrchestrator`, `AgentInteractionGateRequest` (+ `AgentInteractionGateOutcomeResult`).
- `src/Hexalith.Agents.Server/Program.cs` — **modify** (register 3 readers + gate orchestrator).
- `tests/` — `Hexalith.Agents.Tests/AgentInteractionGateAggregateTests.cs` (+ extend `AgentInteractionTestData.cs`, `AgentInteractionStateReplayTests.cs`); `Hexalith.Agents.Contracts.Tests/AgentInteractionGateContractsTests.cs`; `Hexalith.Agents.Server.Tests/AgentInteractionGateOrchestratorTests.cs`; `_bmad-output/implementation-artifacts/tests/2-2-test-summary.md`.

Discovery loaded: root epics + nested planning artifacts (PRD `prd-agents-2026-06-23` FR-2/8/9/12/19/20/21/24 + cross-cutting NFRs, architecture spine `architecture-agents-2026-06-23-2` AD-2/3/4/6/7/8/9/12/13/14/18 + the interaction-lifecycle sequence), the as-built Agents module source (the Story-2.1 `AgentInteraction` aggregate/state/policy/contracts/orchestrator/ports, the Agent activation-gate verdict pattern, the existing reuse ports), and sibling `project-context.md` for EventStore, Conversations, Parties, Tenants. This story was prepared with parallel research subagents analyzing the architecture spine, PRD, as-built code, and cross-module boundaries simultaneously.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.2-Enforce-Invocation-Authorization-And-Dependency-Readiness]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic-2-Safe-Conversation-Invocation-And-Automatic-Replies]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-20-Enforce-Role-And-Policy-Authorization]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-21-Fail-Closed-On-Dependency-Uncertainty]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-19-Enforce-Tenant-Isolation]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-24-Capture-Agent-Audit-Evidence]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-12-Prevent-Automatic-Posting-When-Policy-Fails]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-8-Call-Agent-From-Conversation]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-2-Link-Agent-To-Party-Identity]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-12-Authorization-And-Dependency-Uncertainty]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-3-Pure-Aggregates-Side-Effects-Outside]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-2-Aggregate-Boundaries]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-4-Interaction-Snapshot]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-6-Conversations-Boundary]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-7-Agent-Party-Identity-And-Membership]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-8-Approver-Policy-Resolution]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-13-Idempotent-External-Effects]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-14-Sensitive-Content-And-Secret-Safety]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-18-Hybrid-Agent-Runtime-Ownership]
- [Source: Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionRequestPolicy.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents/Agent/AgentAggregate.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/AgentActivationBlocker.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/AgentInspectionResult.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionStatus.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionRequestOrchestrator.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Ports/IAgentPartyDirectory.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Ports/IProviderCatalogReader.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Ports/IApproverPolicyResolver.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Ports/DeferredAgentConfigurationSnapshotReader.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Program.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Commands/CommandEnvelope.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Results/DomainResult.cs]
- [Source: Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Identity/AggregateIdentity.cs]
- [Source: Hexalith.Conversations/src/Hexalith.Conversations.Client/IConversationClient.cs]
- [Source: Hexalith.Conversations/_bmad-output/project-context.md]
- [Source: Hexalith.Parties/_bmad-output/project-context.md]
- [Source: Hexalith.Tenants/_bmad-output/project-context.md]
- [Source: Hexalith.EventStore/_bmad-output/project-context.md]
- [Source: _bmad-output/implementation-artifacts/2-1-request-hexa-from-a-source-conversation.md]

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created. Story status is `ready-for-dev`.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (1M context) — `claude-opus-4-8[1m]`

### Debug Log References

- Initial domain build: `CA1062` on the gate `Handle` because the `state is not { IsRequested: true }` early-return negative pattern is not recognized as a null-guard for the later `state.Status` access. Resolved by inverting to the positive `state is { IsRequested: true } requested` pattern (mirroring the request handler), with the not-requested rejection as the fall-through — preserving the not-requested → empty-verdicts → idempotent → evaluate precedence.
- Initial domain-test build: `CS8122` (an expression tree may not contain an `is` pattern) on `ShouldNotContain(e => e is A or B)` — the disjunctive `or` pattern is not allowed in expression trees (a single-type `is` is). Replaced with `OfType<A>().ShouldBeEmpty()` + `OfType<B>().ShouldBeEmpty()`.
- Verification rerun: default parallel `dotnet restore`/`dotnet build` returned exit code 1 with no MSBuild diagnostics while traversing project references. Serialized Release build with `-m:1` passed with 0 warnings / 0 errors. `dotnet test` was blocked by the sandbox because VSTest could not create its local TCP listener (`SocketException (13): Permission denied`); the built xUnit v3 test executables were run directly instead and all required suites passed.

### Completion Notes List

- **Task 1** — Extended `AgentInteractionStatus` additively (`Authorized`/`Denied`/`Blocked`; existing `Unknown=0`/`Requested=1` ordinals untouched). Added the safe gate contracts: `AgentInteractionGateCheck` (nine checks in evaluation order), `AgentInteractionGateOutcome` (AD-12 fail-closed vocabulary), `AgentInvocationGateVerdict` (two safe enums only), the `EvaluateAgentInteractionGate` command, the `AgentInteractionAuthorized`/`AgentInteractionGateFailed` durable outcome events (`IEventPayload`, NOT rejections — they are the Audit Evidence), the `AgentInteractionGateNotEvaluableRejection` (`IRejectionEvent`) + `AgentInteractionGateNotEvaluableReason`, and the deferred inspection query/view/result (`null` view on NotAuthorized/NotFound).
- **Task 2** — Added the pure `AgentInvocationGatePolicy` (shared `Evaluate`/`Decide` + authorization/readiness classification with Denied precedence) and the second pure `Handle(EvaluateAgentInteractionGate, …)` on the aggregate (state precondition → empty-verdicts → idempotent terminal NoOp → evaluate). Extended `AgentInteractionState` additively with `Status` + `GateVerdicts` and the three new replay-safe `Apply` overloads (each non-`InteractionRequested` apply keeps the `IsRequested` guard). No new snapshot fields. Added `InternalsVisibleTo("Hexalith.Agents.Server")` so the orchestrator reuses the same policy (no drift) — the only project-file change beyond source.
- **Task 3** — Added the three deferred, fail-closed gate-readiness ports: `ITenantAccessReader`/`TenantAccessReadResult`/`DeferredTenantAccessReader` (Agents-local Tenants projection — not Conversations' internal service), `IConversationAccessReader`/`ConversationAccessReadResult`/`DeferredConversationAccessReader` (wraps the public `IConversationClient.GetConversationAsync`; live read deferred to Story 2.3), and `IAgentInvocationReadinessReader`/`AgentInvocationReadiness`/`DeferredAgentInvocationReadinessReader` (current Agent readiness). Each deferred impl fails closed (Unavailable / not-available). The reused `IAgentPartyDirectory`/`IProviderCatalogReader`/`IApproverPolicyResolver` are not duplicated.
- **Task 4** — Added `AgentInteractionGateOrchestrator` (reads every dependency through its port with `catch (Exception ex) when (ex is not OperationCanceledException)` fail-closed handling → `Unavailable`; maps each to one verdict in enum order; assembles the trusted command; strips reserved client extensions; dispatches; returns the safe outcome via the shared `AgentInvocationGatePolicy.Decide`). Added `AgentInteractionGateRequest`/`AgentInteractionGateOutcomeResult`. Registered the three new deferred readers + the orchestrator in `Program.cs` (the aggregate handler auto-registers via the existing assembly scan). No provider invocation / Conversations post / proposal on the path.
- **Design note** — `AgentInvocationReadiness` carries an `ApproverPolicy?` so the Confirmation-mode `ResponsePolicy` branch can call the reused `IApproverPolicyResolver` with a real policy (the readiness reader IS the Agent read; the resolver needs the policy). `ProviderModelReadiness` reads the snapshot-recorded provider/model id from the request (the deterministic call target) and checks its CURRENT catalog readiness; `ResponsePolicy`/`ContentSafetyPolicy`/lifecycle use current readiness (AD-12).
- **Task 5** — All tests green: domain **424/424**, contracts **119/119**, server **143/143**; `Hexalith.Agents.UI.Tests` regression **156/156**. Full `Hexalith.Agents.slnx` Release build **0 warnings / 0 errors** (`TreatWarningsAsErrors=true`). Test-summary artifact written. All pre-existing structural/guard tests stayed green. (Counts include the QA-automation gap-coverage cases captured in `2-2-qa-e2e-test-summary.md`.)
- **Verification rerun (2026-06-24)** — Revalidated the current workspace with serialized Release build (`dotnet build Hexalith.Agents.slnx --configuration Release -m:1`) and direct xUnit v3 executables for domain/contracts/server/UI regression. Results remained green: **424/424**, **119/119**, **143/143**, **156/156**.

### File List

**Contracts — `src/Hexalith.Agents.Contracts/AgentInteraction/`**
- `AgentInteractionStatus.cs` (modified — additive `Authorized`/`Denied`/`Blocked`)
- `AgentInteractionGateCheck.cs` (new)
- `AgentInteractionGateOutcome.cs` (new)
- `AgentInvocationGateVerdict.cs` (new)
- `AgentInteractionGateNotEvaluableReason.cs` (new)
- `AgentInteractionGateInspectionStatus.cs` (new)
- `Commands/EvaluateAgentInteractionGate.cs` (new)
- `Events/AgentInteractionAuthorized.cs` (new)
- `Events/AgentInteractionGateFailed.cs` (new)
- `Events/Rejections/AgentInteractionGateNotEvaluableRejection.cs` (new)
- `Queries/GetAgentInteractionGateEvidenceQuery.cs` (new)
- `Queries/AgentInteractionGateEvidenceView.cs` (new)
- `Queries/AgentInteractionGateEvidenceResult.cs` (new)

**Domain — `src/Hexalith.Agents/`**
- `AgentInteraction/AgentInteractionAggregate.cs` (modified — added gate `Handle`)
- `AgentInteraction/AgentInteractionState.cs` (modified — `Status`/`GateVerdicts` + 3 `Apply`)
- `AgentInteraction/AgentInvocationGatePolicy.cs` (new)
- `Hexalith.Agents.csproj` (modified — `InternalsVisibleTo("Hexalith.Agents.Server")`)

**Server — `src/Hexalith.Agents.Server/`**
- `Ports/ITenantAccessReader.cs` (new)
- `Ports/TenantAccessReadResult.cs` (new)
- `Ports/DeferredTenantAccessReader.cs` (new)
- `Ports/IConversationAccessReader.cs` (new)
- `Ports/ConversationAccessReadResult.cs` (new)
- `Ports/DeferredConversationAccessReader.cs` (new)
- `Ports/IAgentInvocationReadinessReader.cs` (new)
- `Ports/AgentInvocationReadiness.cs` (new)
- `Ports/DeferredAgentInvocationReadinessReader.cs` (new)
- `Application/AgentInteractions/AgentInteractionGateOrchestrator.cs` (new)
- `Application/AgentInteractions/AgentInteractionGateRequest.cs` (new)
- `Program.cs` (modified — register 3 readers + gate orchestrator)

**Tests**
- `tests/Hexalith.Agents.Tests/AgentInteractionTestData.cs` (modified — gate fixtures + `ApplyAll`)
- `tests/Hexalith.Agents.Tests/AgentInteractionGateAggregateTests.cs` (new)
- `tests/Hexalith.Agents.Tests/AgentInteractionStateReplayTests.cs` (modified — gate replay)
- `tests/Hexalith.Agents.Contracts.Tests/AgentInteractionGateContractsTests.cs` (new)
- `tests/Hexalith.Agents.Server.Tests/AgentInteractionGateOrchestratorTests.cs` (new)
- `_bmad-output/implementation-artifacts/tests/2-2-test-summary.md` (new)
- `_bmad-output/implementation-artifacts/tests/2-2-qa-e2e-test-summary.md` (new — QA-automation gap-coverage summary)

**Tracking**
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (modified — story status set to `review`)

## Change Log

| Date | Change |
| --- | --- |
| 2026-06-24 | Implemented Story 2.2 invocation authorization + dependency-readiness gate: additive `AgentInteractionStatus` extension; safe gate contracts (checks/outcome/verdict/command/outcome events/not-evaluable rejection/evidence query+view+result); pure aggregate gate evaluation + shared `AgentInvocationGatePolicy` + additive state; three deferred fail-closed gate-readiness ports; trusted-verdict gate orchestration with no side effects; full domain/contract/server tests (424/119/143) + UI regression (156); Release build 0W/0E. Status → review. |
| 2026-06-24 | Re-ran Story 2.2 verification in the current sandbox: serialized Release build passed 0W/0E; direct xUnit v3 executable runs passed for domain (424), contracts (119), server (143), and UI regression (156). |
| 2026-06-24 | Adversarial code review (story-automator): re-ran Release build (0W/0E) and all suites green (domain 424, contracts 119, server 143, UI 156); validated every `[x]` task and all four ACs against the implementation. 0 critical / 0 high / 0 medium findings; corrected stale test counts in the Dev Agent Record and added the QA gap-coverage summary to the File List. Status → done. |

## Senior Developer Review (AI)

_Reviewer: Administrator on 2026-06-24 — adversarial review via story-automator (auto-fix mode)._

**Outcome: Approved.** All 23 `[x]` subtasks were validated against the actual implementation and all four Acceptance Criteria are implemented and tested. The story File List matches git reality for every source file.

**Independently verified:**
- `dotnet build Hexalith.Agents.slnx --configuration Release` → **0 warnings / 0 errors** (`TreatWarningsAsErrors=true`).
- Test suites (xUnit v3, Release): domain **424/424**, contracts **119/119**, server **143/143**, UI regression **156/156** — all passing.

**AC validation:**
- **AC1** — All nine `AgentInteractionGateCheck` values are evaluated in enum order; any non-`Satisfied` verdict (including the `Unknown` sentinel) blocks; all-deferred bindings fail closed to `Denied`. Verified in `AgentInvocationGatePolicy`, the orchestrator's verdict assembly, and the aggregate/orchestrator test matrices.
- **AC2** — The gate orchestration's only collaborators are read ports + the command dispatcher (no provider adapter, no `IConversationClient.AppendMessageAsync`, no proposal); proven structurally and by `Only_reads_and_one_gate_dispatch_no_provider_invocation_or_conversation_post`.
- **AC3** — Tenant scope flows from the envelope; failed inspection returns a `null` evidence view; `Unavailable` is returned identically for absent vs cross-tenant. Verified by contract + orchestrator tests, including the AD-14 leak assertions (serialized outcome/verdicts contain no raw error text).
- **AC4** — `AgentInteractionGateFailed` records the safe blocker set as durable audit evidence; reflection guards (`Verdict_exposes_only_the_two_safe_gate_enums`, `Gate_evidence_and_outcome_types_carry_no_sensitive_members`) enforce no prompt/claims/tokens/PII/payload leakage.

**Findings:** 0 critical, 0 high, 0 medium. Three low-severity observations — (1) stale test counts in the Dev Agent Record and (2) the QA gap-coverage summary missing from the File List were auto-corrected; (3) `DependencyFreshness` aggregates only the three freshness-exposing reads (tenant/conversation/readiness), a documented and defensible limitation because the reused Parties/Provider/Approver result types carry no freshness channel and fold staleness into their own verdicts — no in-scope code change, revisit when those live bindings are wired.
