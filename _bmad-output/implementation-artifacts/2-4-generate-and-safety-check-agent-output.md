---
baseline_commit: c8b2f15ef679ecd8e737e7a24a77dc68ef6bb81c
---

# Story 2.4: Generate And Safety-Check Agent Output

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a Conversation Participant,
I want `hexa` generation to respect Provider and Content Safety policies,
so that unsafe or incomplete output cannot become a durable conversation artifact.

## Acceptance Criteria

**AC1 - Generation runs through an Agents-owned provider adapter behind a safe boundary**

**Given** context building succeeds (the interaction is at `AgentInteractionStatus.ContextReady`)
**When** the selected durable owner invokes generation through an Agents-owned Provider adapter
**Then** provider SDK types, credentials, raw payloads, and provider-specific errors stay behind adapter boundaries
**And** public contracts and durable events expose only safe Provider/model identity (`ProviderId`, `ModelId`, capability version), safe error classes, usage/status, and policy references.

**AC2 - Content Safety Policy gates generated output before any downstream artifact**

**Given** generation succeeds
**When** Content Safety Policy is evaluated against the generated content
**Then** generated content that passes policy is recorded as a generated version and the interaction reaches a `Generated` state that the response-mode branch (Story 2.5 automatic post / Story 3.1 proposal) can consume
**And** generated content that fails policy cannot be posted automatically or become an approvable Proposed Agent Reply — no generated version is created that any later story could approve.

**AC3 - All failure classes fail closed with safe audit, never a partial message**

**Given** provider timeout, disabled Provider/model state, adapter failure, invalid/unloadable context, safety failure, or policy failure occurs
**When** the interaction is updated
**Then** the system records a safe failure status (`GenerationFailed` or `SafetyFailed`) and Audit Evidence with a safe failure-classification reason
**And** no partial Conversation Message and no approvable Proposed Agent Reply is created, and no raw provider payload, provider-specific error text, stack trace, secret, or unsafe content is exposed in events meant for display, status, logs, telemetry, or audit summaries.

**AC4 - Retried generation is deterministic and auditable**

**Given** the same generation attempt is retried after a transient failure
**When** deterministic attempt identifiers are reused
**Then** duplicate generated versions, duplicate provider attempts where avoidable, and duplicate downstream effects are prevented or safely deduplicated (re-dispatching `GenerateAgentOutput` after a terminal `Generated`/`GenerationFailed`/`SafetyFailed` outcome returns a deterministic no-op that preserves version history)
**And** retry outcomes remain auditable.

[Source: _bmad-output/planning-artifacts/epics.md#Story-2.4; prd FR9, FR10, FR12, FR19, FR20, FR21, FR27, NFR3, NFR6, NFR7; ARCHITECTURE-SPINE.md#AD-3, #AD-5, #AD-9, #AD-13, #AD-14, #AD-17, #AD-18]

## Tasks / Subtasks

- [x] **Task 1 - Append generation states to `AgentInteractionStatus` (additive, AD-2)** (AC: #2, #3)
  - [x] Add three values AFTER `ContextBlocked` (ordinal 6), never reshaping 0–6: `Generated` (7), `GenerationFailed` (8), `SafetyFailed` (9), in `src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionStatus.cs`. Mirror the existing one-line XML doc per member and the `Unknown=0` sentinel rationale.
  - [x] `Generated` doc: generation succeeded and passed Content Safety Policy; the call may proceed to the response-mode branch (Story 2.5 automatic post / Story 3.1 proposal — these consume this state). `GenerationFailed` doc: provider timeout / disabled provider-model / adapter failure / invalid context / policy failure — recorded as fail-closed Audit Evidence, no Conversation Message, no approvable proposal. `SafetyFailed` doc: generated content failed Content Safety Policy — recorded as fail-closed Audit Evidence, content is non-postable and non-approvable.
  - [x] Update the enum's top summary comment that today says states are appended "by Stories 2.2–2.5" — it already anticipates this; keep it accurate.

- [x] **Task 2 - Add generation value objects + safe enums to Contracts** (AC: #1, #2, #3)
  - [x] `AgentOutputGenerationFailureReason` enum (`src/Hexalith.Agents.Contracts/AgentInteraction/`): `[JsonConverter(typeof(JsonStringEnumConverter))]`, `Unknown=0`, then `ProviderTimeout, ProviderDisabled, ProviderUnavailable, AdapterFailure, InvalidContext, GenerationError, ContentSafetyBlocked, PolicyFailure`. One safe XML doc line each; carries NO raw content. Mirror `AgentInteractionContextBlockReason.cs`.
  - [x] `AgentGenerationKind` enum: `Unknown=0, Generated` (additive room for `Edited`, `Regenerated` in Epic 3 — do NOT add those now). Mirror `AgentInteractionContextMode.cs`.
  - [x] `AgentGeneratedVersion` record (`src/Hexalith.Agents.Contracts/AgentInteraction/`): `string VersionId, string AttemptId, AgentGenerationKind Kind, string GeneratedContent, string ProviderId, string ModelId, int ProviderCapabilityVersion, int ContentSafetyPolicyVersion, int PromptTokenCount, int OutputTokenCount`. **`GeneratedContent` is sensitive conversation-derived content (same class as `Prompt`)** — it lives only on the durable success event + state, NEVER on commands/views/results/rejections/logs (AD-14). XML-doc this on the property.
  - [x] `AgentGenerationAttemptEvidence` record: `string AttemptId, string ProviderId, string ModelId, int ProviderCapabilityVersion, int PromptTokenCount, int OutputTokenCount` — safe numerics/ids only, for failure events. No raw payload/error text. Mirror `AgentInteractionContextEvidence.cs` (numbers + safe refs only).
  - [x] `AgentOutputGenerationResult` record (server-assembled command input mirroring `AgentInteractionContextMeasurement.cs`): an outcome discriminator `AgentGenerationOutcome Outcome` enum (`Unknown=0, Succeeded, ProviderTimeout, ProviderDisabled, ProviderUnavailable, AdapterFailure, InvalidContext, GenerationError, ContentSafetyBlocked, PolicyFailure`), plus `string AttemptId, string ProviderId, string ModelId, int ProviderCapabilityVersion, int ContentSafetyPolicyVersion, string? GeneratedContent, int PromptTokenCount, int OutputTokenCount`. The pure policy maps `Outcome` → event(s) + status; the orchestrator is the only producer.
  - [x] `AgentOutputNotGeneratableReason` enum: `Unknown=0, InteractionNotRequested, ContextNotReady`. Mirror `AgentInteractionContextNotBuildableReason.cs`.

- [x] **Task 3 - Add command, events, and rejection** (AC: #1, #2, #3, #4)
  - [x] Command `GenerateAgentOutput(string AgentInteractionId, AgentOutputGenerationResult Result)` in `…/AgentInteraction/Commands/`. Plain `public record` (NO `IEventPayload`, NO attribute). Redundant `AgentInteractionId` mirrors envelope `AggregateId`; `Result` is server-assembled. Mirror `BuildAgentInteractionContext.cs`.
  - [x] Success event `AgentOutputGenerated(string AgentInteractionId, AgentGeneratedVersion Version)` : `IEventPayload` in `…/Events/`. Sole durable home of the generated content. Mirror `AgentInteractionContextReady.cs`.
  - [x] Recorded-negative event `AgentOutputGenerationFailed(string AgentInteractionId, AgentInteractionStatus Decision, AgentOutputGenerationFailureReason Reason, AgentGenerationAttemptEvidence Evidence)` : `IEventPayload` in `…/Events/` — `Decision` is `GenerationFailed` or `SafetyFailed`. This is a SUCCESS event (durable audit), NOT a rejection. Mirror `AgentInteractionGateFailed.cs` (which carries `Decision` + blockers). Evidence carries safe metadata only; the unsafe/failed content is NEVER attached.
  - [x] Rejection `AgentOutputNotGeneratableRejection(string AgentInteractionId, AgentOutputNotGeneratableReason Reason)` : `IRejectionEvent` in `…/Events/Rejections/` — structural inability to evaluate (not requested, or context not ready), no state change. Mirror `AgentInteractionContextNotBuildableRejection.cs`.

- [x] **Task 4 - Pure generation policy** (AC: #2, #3, #4)
  - [x] `AgentOutputGenerationPolicy` (`internal static`) in `src/Hexalith.Agents/AgentInteraction/`, mirroring `AgentInteractionContextPolicy.cs`: `Evaluate(string interactionId, AgentOutputGenerationResult result) → DomainResult` and `Decide(AgentOutputGenerationResult result) → AgentInteractionStatus`, with a private `Compute` returning a `readonly record struct` decision.
  - [x] Mapping: `Succeeded` → `AgentOutputGenerated(version)` + status `Generated`; `ContentSafetyBlocked` → `AgentOutputGenerationFailed(Decision: SafetyFailed, Reason: ContentSafetyBlocked, evidence)` + status `SafetyFailed`; every other outcome → `AgentOutputGenerationFailed(Decision: GenerationFailed, Reason: mapped, evidence)` + status `GenerationFailed`.
  - [x] On safety/any failure, the policy emits NO `AgentGeneratedVersion` (nothing approvable, AD-5). On success it builds the version (VersionId derived deterministically from `AttemptId`) including the generated content.
  - [x] Confirm `Decide` and `Evaluate` agree on status for every outcome (no-drift), exactly as the context/gate policies do.

- [x] **Task 5 - Aggregate handler + state** (AC: #2, #3, #4)
  - [x] Add `public static DomainResult Handle(GenerateAgentOutput command, AgentInteractionState? state, CommandEnvelope envelope)` to `src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs` as the 4th handler. Mirror `Handle(BuildAgentInteractionContext)` exactly: `ArgumentNullException.ThrowIfNull(command/envelope)`; `string interactionId = envelope.AggregateId;`; positive bind `if (state is { IsRequested: true } requested)`; **terminal idempotent no-op** `if (requested.Status is AgentInteractionStatus.Generated or AgentInteractionStatus.GenerationFailed or AgentInteractionStatus.SafetyFailed) return DomainResult.NoOp();`; **precondition** `if (requested.Status != AgentInteractionStatus.ContextReady) return DomainResult.Rejection([new AgentOutputNotGeneratableRejection(interactionId, AgentOutputNotGeneratableReason.ContextNotReady)]);`; delegate to `AgentOutputGenerationPolicy.Evaluate(interactionId, command.Result)`; fall-through `return DomainResult.Rejection([new AgentOutputNotGeneratableRejection(interactionId, AgentOutputNotGeneratableReason.InteractionNotRequested)]);`.
  - [x] In `src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs` add additive nullable fields mirroring `ContextEvidence`/`ContextBlockReason`: `IReadOnlyList<AgentGeneratedVersion>? GeneratedVersions` (a list so Epic 3 regeneration can append) and `AgentOutputGenerationFailureReason? GenerationFailureReason`.
  - [x] Add `Apply(AgentOutputGenerated e)`: guard `if (!IsRequested) return;`, `Status = AgentInteractionStatus.Generated;`, append `e.Version` to `GeneratedVersions`. Add `Apply(AgentOutputGenerationFailed e)`: guard, `Status = e.Decision;`, `GenerationFailureReason = e.Reason;`. Add no-op `Apply(AgentOutputNotGeneratableRejection)` via `MarkReplayOnlyEventHandled()`.
  - [x] Use the CA1062-safe **positive** null-guard pattern (`state is { IsRequested: true } requested`); do NOT use the negative early-return form (warnings-as-errors will fail — Story 2.2 learning).

- [x] **Task 6 - Provider-generation port + content-safety port + reader (Server, all deferred/fail-closed)** (AC: #1, #2, #3)
  - [x] `IAgentGenerationProvider` in `src/Hexalith.Agents.Server/Ports/`: `Task<AgentGenerationProviderResult> GenerateAsync(AgentGenerationProviderRequest request, CancellationToken ct)`. Request carries safe inputs only (`ProviderId, ModelId, providerCapabilityVersion, prompt/context payload, MaxOutputTokenLimit, RequestTimeoutMilliseconds, MaxRetries, deterministic AttemptId`). Result carries safe outputs only (`AgentGenerationOutcome, GeneratedContent?, OutputTokenCount, PromptTokenCount`) — NO provider SDK types, credentials, raw payloads, or provider-specific errors (AD-9). **This is the FIRST real model-invocation seam in the module.**
  - [x] `DeferredAgentGenerationProvider` placeholder returning a fail-closed `AgentGenerationOutcome.ProviderUnavailable` result (never throws), so the default DI graph blocks generation safely (content-bearing workflows stay disabled until protection is wired — AD-14). The live provider-SDK adapter stays adapter-local and deferred (Stack: "Provider SDK Deferred; adapter-local when selected").
  - [x] `IContentSafetyEvaluator` in `…/Ports/`: `Task<ContentSafetyEvaluationResult> EvaluateAsync(ContentSafetyEvaluationRequest request, CancellationToken ct)`. Request carries generated content + the policy's `PromptConstraints/BlockedOutputCategories/RestrictedOutputCategories/FailureHandling`. Result is a safe verdict (`ContentSafetyVerdict Verdict` enum `Unknown=0, Passed, Blocked` + optional safe category label). `DeferredContentSafetyEvaluator` fails closed (`Blocked`/cannot-evaluate) since the concrete filter engine/taxonomy is deferred (PRD OQ-9). Implement the enforcement SHAPE, not the engine — exactly as Story 2.3 wired the bounded-context shape without a concrete bounded behavior.
  - [x] `IAgentContentSafetyPolicyReader` in `…/Ports/`: `Task<AgentContentSafetyPolicyReadResult> ReadAsync(string tenantId, string agentId, AgentResponseMode mode, CancellationToken ct)` returning the effective `AgentContentSafetyPolicy` (mode-specific override if present, else `ActivePolicy`) + its version, or a fail-closed not-available result. The `AgentInteractionSnapshot` only carries `ContentSafetyPolicyVersion`; the orchestrator needs the policy body (categories + `FailureHandling` + `AuditTreatment`) which lives on `AgentState.ContentSafety`. `DeferredAgentContentSafetyPolicyReader` returns not-available (fail closed).

- [x] **Task 7 - Generation orchestrator (durable-owner step, impure)** (AC: #1, #2, #3, #4)
  - [x] `AgentInteractionGenerationOrchestrator` (`public sealed class`) in `src/Hexalith.Agents.Server/Application/AgentInteractions/`. Deps (ctor `ArgumentNullException.ThrowIfNull` each): `IConversationContextReader`, `IProviderCatalogReader`, `IAgentGenerationProvider`, `IAgentContentSafetyPolicyReader`, `IContentSafetyEvaluator`, `IAgentCommandDispatcher`. Mirror `AgentInteractionContextOrchestrator.cs`.
  - [x] `ExecuteAsync(AgentInteractionGenerationRequest request, CancellationToken ct)`:
    1. **Re-read** Source Conversation content via `IConversationContextReader` — Story 2.3 measured tokens then DISCARDED the raw text; only counts are on the context evidence. 2.4 must reload the content fresh to build the model input. Fail-closed → `Outcome.InvalidContext`.
    2. Read `IProviderCatalogReader.GetEntryAsync` for `MaxOutputTokenLimit` + `ProviderModelTimeoutPolicy{RequestTimeoutMilliseconds, MaxRetries}` + `Status` + `SupportsTextGeneration`. `ProviderModelStatus.Disabled` (or not text-gen) → `Outcome.ProviderDisabled`; read failure/not-found → `Outcome.ProviderUnavailable`.
    3. Derive a deterministic `AttemptId` from the interaction id (so retries reuse it — AD-13). Invoke `IAgentGenerationProvider.GenerateAsync` honoring the timeout/retry budget. Wrap in the per-read fail-closed pattern `try { … } catch (Exception ex) when (ex is not OperationCanceledException) { → Outcome.AdapterFailure }`; never propagate provider error text (AD-14). Map a timeout outcome to `Outcome.ProviderTimeout`.
    4. On provider success, read the effective policy via `IAgentContentSafetyPolicyReader` (using snapshot `ResponseMode`); evaluate generated content via `IContentSafetyEvaluator`. `Blocked` → `Outcome.ContentSafetyBlocked`; reader not-available → `Outcome.PolicyFailure`.
    5. Assemble a server-trusted `AgentOutputGenerationResult` (strip client-supplied values via the standard `_reservedExtensionKeys` + `BuildTrustedExtensions`). Build `CommandEnvelope(request.MessageId, request.TenantId, InteractionDomain, request.AgentInteractionId, nameof(GenerateAgentOutput), JsonSerializer.SerializeToUtf8Bytes(command), request.CorrelationId, CausationId: null, request.ActorUserId, BuildTrustedExtensions(request.ClientSuppliedExtensions))`. `await _dispatcher.DispatchAsync(envelope, ct)`.
    6. Return `new AgentInteractionGenerationOutcomeResult(request.AgentInteractionId, AgentOutputGenerationPolicy.Decide(result))` — status from the SHARED policy so reported status can't drift. (`AgentOutputGenerationPolicy` is `internal` and visible to `…Server` via `InternalsVisibleTo`, but NOT to `…Server.Tests` — assert via the orchestrator's returned status, Story 2.3 learning.)
  - [x] `private const string InteractionDomain = "agent-interaction";` + copy the `_reservedExtensionKeys` array and `BuildTrustedExtensions` helper verbatim from `AgentInteractionContextOrchestrator`.
  - [x] DTOs in the same folder: `AgentInteractionGenerationRequest` (`sealed record`: `string MessageId, string CorrelationId, string TenantId, string AgentInteractionId, string AgentId, string SourceConversationId, string ProviderId, string ModelId, int ProviderCapabilityVersion, int ContentSafetyPolicyVersion, AgentResponseMode ResponseMode, string ActorUserId, string? ClientCorrelationId = null, IReadOnlyDictionary<string,string>? ClientSuppliedExtensions = null`) and `AgentInteractionGenerationOutcomeResult(string AgentInteractionId, AgentInteractionStatus Status)` — safe id + status only.

- [x] **Task 8 - DI registration (Program.cs)** (AC: #1, #3)
  - [x] In `src/Hexalith.Agents.Server/Program.cs` add a `// Story 2.4` block: `AddSingleton<IAgentGenerationProvider, DeferredAgentGenerationProvider>()`, `AddSingleton<IContentSafetyEvaluator, DeferredContentSafetyEvaluator>()`, `AddSingleton<IAgentContentSafetyPolicyReader, DeferredAgentContentSafetyPolicyReader>()`, `AddScoped<AgentInteractionGenerationOrchestrator>()`. Mirror the existing deferred-port + orchestrator registration pattern; gate any future live provider-adapter binding behind a config section like the `Conversations` block. The aggregate handler auto-registers (no host change — `AddEventStoreDomainService` scan).

- [x] **Task 9 - Tests (xUnit v3 + Shouldly; NSubstitute only in Server.Tests)** (AC: #1, #2, #3, #4)
  - [x] Extend `tests/Hexalith.Agents.Tests/AgentInteractionTestData.cs`: add `StateContextReady()` fixture, an `AgentOutputGenerationResult` builder (`SucceededGenerationResult`, `SafetyBlockedGenerationResult`, `ProviderTimeoutGenerationResult`, …), and new `Apply(AgentOutputGenerated)`/`Apply(AgentOutputGenerationFailed)` cases in the `ApplyAll` switch.
  - [x] `tests/Hexalith.Agents.Tests/AgentInteractionGenerationAggregateTests.cs`: ContextReady + Succeeded → `AgentOutputGenerated` + `Generated`; ContextReady + SafetyBlocked → `AgentOutputGenerationFailed(Decision=SafetyFailed)` + NO version; each provider/timeout/disabled/adapter/invalid-context/policy outcome → `AgentOutputGenerationFailed(Decision=GenerationFailed, Reason=…)`; wrong precondition (Authorized, Requested) → `AgentOutputNotGeneratableRejection(ContextNotReady)`; not-requested → `AgentOutputNotGeneratableRejection(InteractionNotRequested)`; **idempotent re-issue after each terminal status → `DomainResult.NoOp()`** (AC4); `Evaluate`/`Decide` no-drift theory.
  - [x] Extend `tests/Hexalith.Agents.Tests/AgentInteractionStateReplayTests.cs` with the new events.
  - [x] `tests/Hexalith.Agents.Contracts.Tests/AgentInteractionGenerationContractsTests.cs`: JSON round-trip for all new records/enums; assert `AgentInteractionStatus` ordinals 0–6 are unperturbed and 7–9 are the new values; **AD-14 no-leak**: serialize `AgentOutputGenerationFailed`/views/results/rejections and `.ShouldNotContain` any generated content; the `ContractsSecretNonDisclosureTests` auto-coverage must include the new types. When asserting "no Hexalith attribute," scope to `Hexalith`-namespaced attributes (compiler emits `Nullable*Attribute` on records with nullable members — Story 2.1 learning).
  - [x] `tests/Hexalith.Agents.Server.Tests/AgentInteractionGenerationOrchestratorTests.cs` (NSubstitute): happy path dispatches `GenerateAgentOutput` with correct domain/aggregateId and returns `Generated`; safety-blocked → `SafetyFailed`, no version content in the dispatched envelope or result; provider throw → fail-closed `GenerationFailed` (no raw error leaked); provider `Disabled`/not-found/timeout each map correctly; conversation re-read failure → `InvalidContext`/`GenerationFailed`; safety-policy reader not-available → `PolicyFailure`; **all-deferred default graph fails closed** (`ProviderUnavailable` → `GenerationFailed`); end-to-end into the real aggregate via the dispatcher substitute; `OperationCanceledException` propagates (not swallowed).
  - [x] Build/test loop from `/home/administrator/projects/hexalith/agents/Hexalith.Agents`: `dotnet restore Hexalith.Agents.slnx` → `dotnet build Hexalith.Agents.slnx --configuration Release` (require 0 warnings / 0 errors; add `-m:1` if the parallel build flakes — Story 2.2 learning) → `dotnet test` each test project individually (NOT solution-wide; if VSTest reports `SocketException (13): Permission denied`, run the built xUnit v3 test executables directly — Story 2.2 learning). Keep structural guards green: `ProjectReferenceDirectionTests`, `ModuleLayout`, `PublicContractPackageBoundaryTests`, `StructuralSeedConformanceTests`, `PackageVersionCentralizationTests`, `BuildContractConformanceTests`.
  - [x] Write `_bmad-output/implementation-artifacts/tests/2-4-test-summary.md` with per-project pass counts and list it in the File List.

## Dev Notes

### Critical Guardrails

- **This story is the GENERATION + CONTENT-SAFETY step ONLY.** Implement: the provider-generation port + deferred adapter, the content-safety evaluation port + deferred evaluator, the content-safety policy reader, the generation orchestrator, the `GenerateAgentOutput` command, the `AgentOutputGenerated`/`AgentOutputGenerationFailed` events, the `AgentOutputNotGeneratableRejection`, the generated-version record + failure enums, the pure generation policy, the aggregate handler + state fields, and the three new status values. **Do NOT implement:** posting a Conversation Message (Story 2.5), creating a Proposed Agent Reply (Story 3.1), the response-mode branch itself, edit/regenerate/approve (Epic 3), the concrete provider SDK (deferred/adapter-local), or the concrete content-safety filter engine/taxonomy (PRD OQ-9). [Source: _bmad-output/planning-artifacts/epics.md#Story-2.4, #Story-2.5, #Story-3.1]
- **AD-3 aggregate purity is the heart of this story.** The `AgentInteractionAggregate.Handle` methods emit events only — no provider calls, no HTTP, no timers, no `DateTimeOffset.UtcNow`, no `Guid.NewGuid`. The provider invocation, content-safety evaluation, conversation re-read, and catalog read all happen in the `AgentInteractionGenerationOrchestrator` (the durable-owner step) and return into the aggregate through the single `GenerateAgentOutput` command. [Source: ARCHITECTURE-SPINE.md#AD-3; src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs:22-37]
- **AD-9 provider-secret non-disclosure is the heart of this story.** This is the first real model invocation in the module. Provider SDK types, credentials, raw payloads, and provider-specific errors stay behind the `IAgentGenerationProvider` adapter. Public contracts and durable events expose only `ProviderId`, `ModelId`, capability version, safe error classes (`AgentOutputGenerationFailureReason`), and usage/status. [Source: ARCHITECTURE-SPINE.md#AD-9; #AD-14]
- **AD-14 redaction.** Generated content is sensitive conversation-derived content, same class as the `Prompt`. It lives only on `AgentOutputGenerated` + `AgentInteractionState.GeneratedVersions`. It must be absent from the status view, the outcome result, rejections, the failure event, logs, telemetry, and audit summaries. On a safety failure, default `ContentSafetyAuditTreatment.MetadataOnly` means the unsafe content is NOT persisted as an approvable version and NOT echoed into status/audit. [Source: ARCHITECTURE-SPINE.md#AD-14; src/Hexalith.Agents.Contracts/Agent/ContentSafetyAuditTreatment.cs]
- **AD-5 nothing-approvable-on-failure.** Approval (Epic 3) selects exactly one generated/edited/regenerated *version*. If safety or generation fails, the policy emits NO `AgentGeneratedVersion`, so there is structurally nothing a later story could approve or post. [Source: ARCHITECTURE-SPINE.md#AD-5]
- **AD-13 deterministic ids / idempotent retry is the heart of AC4.** Generation reuses the existing `AgentInteractionId` (no new aggregate identity). The deterministic `AttemptId` is derived from the interaction id so retries dedupe at the provider/adapter. The aggregate's terminal no-op on `Generated`/`GenerationFailed`/`SafetyFailed` makes re-dispatching `GenerateAgentOutput` a deterministic no-op that preserves version history. [Source: ARCHITECTURE-SPINE.md#AD-13]
- **AD-18 single durable owner.** The orchestrator IS the durable-owner generation step (Microsoft Agent Framework workflow / Dapr Workflow in the AgentHost). It coordinates the provider call + safety gate as replay/idempotency-safe steps and mutates Agents state ONLY through `GenerateAgentOutput`. Public contracts and EventStore aggregates must not depend on Microsoft Agent Framework, Dapr AI/Agents, provider SDK, or workflow SDK types. [Source: ARCHITECTURE-SPINE.md#AD-18]
- **Fail closed on every failure class (FR10/FR12).** Provider timeout, disabled provider/model, adapter failure, invalid context, safety failure, and policy failure ALL record `AgentOutputGenerationFailed` audit evidence with a safe reason and create no partial Conversation Message and no approvable proposal — exactly the shape `ContextBlocked`/`GateFailed` already use. [Source: prd FR10, FR12, FR27; ARCHITECTURE-SPINE.md#AD-11, #AD-12]

### Why No Persisted `Generating` State

UX-DR27 lists "generating" as a canonical Agent Call state, but V1 does NOT persist a transient in-flight status in the aggregate — consistent with how the gate (Story 2.2) and context (Story 2.3) steps work: the orchestrator does all impure work, then dispatches ONE command that records the terminal outcome (`Generated`/`GenerationFailed`/`SafetyFailed`). A "generating" indicator is a UI/status derivation for Story 2.6, not a durable aggregate state. Persisting a transient state would require a second command before invocation and break the single-command-per-step pattern. [Source: ARCHITECTURE-SPINE.md#AD-3 sequence; src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionContextOrchestrator.cs]

### Design: Where Each Responsibility Lives

```
Durable owner (AgentHost: Agent Framework / Dapr Workflow)   ← AD-18, replay/idempotency-safe
  └─ AgentInteractionGenerationOrchestrator.ExecuteAsync      ← IMPURE (Server.Application)
       ├─ IConversationContextReader   (RE-READ source conversation content — not carried from 2.3)
       ├─ IProviderCatalogReader       (MaxOutputTokenLimit, TimeoutPolicy{ms,MaxRetries}, Status)
       ├─ IAgentGenerationProvider     (the ONLY model invocation — provider SDK behind adapter)
       ├─ IAgentContentSafetyPolicyReader (effective policy body: categories + FailureHandling)
       ├─ IContentSafetyEvaluator      (safe Passed/Blocked verdict)
       │     → assemble server-trusted AgentOutputGenerationResult
       └─ IAgentCommandDispatcher.DispatchAsync(GenerateAgentOutput envelope)
            └─ AgentInteractionAggregate.Handle(GenerateAgentOutput, state, env)  ← PURE (AD-3)
                 └─ AgentOutputGenerationPolicy.Evaluate → DomainResult(events)   ← PURE, shared
                      └─ AgentInteractionState.Apply(AgentOutputGenerated|…Failed) ← PURE
  status returned to caller = AgentOutputGenerationPolicy.Decide(result)  ← shared, no drift
```

### Current Code State To Preserve

Read these files completely before editing — they are the exact templates to mirror:
- `src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionContextOrchestrator.cs` — the orchestrator template: `InteractionDomain` const, `_reservedExtensionKeys` + `BuildTrustedExtensions`, `CommandEnvelope` construction, per-read fail-closed `try/catch when (ex is not OperationCanceledException)`, return status via shared `Decide`. Its doc (lines 25-27) explicitly says it must NOT call a provider adapter because "generation is Story 2.4" — this story is that follow-on.
- `src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs:143-180` — `Handle(BuildAgentInteractionContext)`: the exact guard cascade (ThrowIfNull → positive bind → terminal NoOp → precondition rejection → policy Evaluate → fall-through rejection) to copy.
- `src/Hexalith.Agents/AgentInteraction/AgentInteractionContextPolicy.cs` — the `Evaluate`/`Decide`/`Compute` twin-policy template (internal, `InternalsVisibleTo` Server but not Server.Tests).
- `src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs` — mutable `sealed class`, nullable evidence fields, `Apply` overloads guarded by `if (!IsRequested) return;`, `MarkReplayOnlyEventHandled()` no-op for rejections.
- `src/Hexalith.Agents.Contracts/AgentInteraction/Events/AgentInteractionContextReady.cs` and `…/AgentInteractionGateFailed.cs` — the success-event and recorded-negative-event templates (the latter carries `Decision` + evidence, which `AgentOutputGenerationFailed` mirrors).
- `src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionContextEvidence.cs` and `…/AgentInteractionContextBlockReason.cs` — safe-evidence value-object and safe-reason-enum templates.
- `src/Hexalith.Agents.Contracts/Agent/AgentContentSafetyPolicy.cs`, `AgentContentSafetyConfiguration.cs`, `ContentSafetyFailureHandling.cs`, `ContentSafetyAuditTreatment.cs` — the safety contracts to ENFORCE (already created by Story 1.7; do not redefine).
- `src/Hexalith.Agents.Contracts/ProviderCatalog/ProviderCatalogEntryView.cs` + `ProviderModelTimeoutPolicy.cs` + `ProviderModelStatus.cs` — provider readiness/budget/timeout/retry source.

What must be preserved:
- `AgentInteractionStatus` ordinals 0–6 unchanged; append 7–9 only.
- `Hexalith.Agents.Contracts` stays inward-only — NO reference to `Hexalith.Conversations.Contracts` / `Hexalith.Parties.Contracts`; all cross-module ids are opaque `string`. Provider SDK must NOT enter Contracts, the domain, or the aggregate.
- The interaction is keyed by `AgentInteractionId` as `AggregateId` (Story 2.1 reconciliation) — generation does not introduce a new aggregate id.
- All existing structural/contract guard tests stay green; `net10.0`, `LangVersion 14`, nullable, warnings-as-errors, Central Package Management, `.slnx` only, SDK pinned `10.0.301`.
- The raw conversation context text from Story 2.3 was deliberately discarded (only token counts persisted) — 2.4 MUST re-read it; do not assume it is on the state/evidence.

### Generation Outcome → Event/Status Mapping

| Orchestrator `AgentGenerationOutcome` | Failure reason | Event emitted | Status |
|---|---|---|---|
| `Succeeded` (provider OK + safety `Passed`) | — | `AgentOutputGenerated(version)` | `Generated` (7) |
| `ContentSafetyBlocked` | `ContentSafetyBlocked` | `AgentOutputGenerationFailed(SafetyFailed, …)` | `SafetyFailed` (9) |
| `ProviderTimeout` | `ProviderTimeout` | `AgentOutputGenerationFailed(GenerationFailed, …)` | `GenerationFailed` (8) |
| `ProviderDisabled` | `ProviderDisabled` | `AgentOutputGenerationFailed(GenerationFailed, …)` | `GenerationFailed` (8) |
| `ProviderUnavailable` (incl. default deferred graph) | `ProviderUnavailable` | `AgentOutputGenerationFailed(GenerationFailed, …)` | `GenerationFailed` (8) |
| `AdapterFailure` | `AdapterFailure` | `AgentOutputGenerationFailed(GenerationFailed, …)` | `GenerationFailed` (8) |
| `InvalidContext` (re-read failed) | `InvalidContext` | `AgentOutputGenerationFailed(GenerationFailed, …)` | `GenerationFailed` (8) |
| `GenerationError` | `GenerationError` | `AgentOutputGenerationFailed(GenerationFailed, …)` | `GenerationFailed` (8) |
| `PolicyFailure` (safety policy not loadable) | `PolicyFailure` | `AgentOutputGenerationFailed(GenerationFailed, …)` | `GenerationFailed` (8) |

Wrong precondition (status ≠ `ContextReady`) → `AgentOutputNotGeneratableRejection` (no state change). [Source: prd FR10, FR27; ARCHITECTURE-SPINE.md#AD-5, #AD-9]

### Content Safety Policy Enforcement (the shape, not the engine)

- Story 1.7 created the safety CONTRACTS on the `Agent` aggregate: `AgentContentSafetyConfiguration{ActivePolicy, AutomaticModePolicy?, ConfirmationModePolicy?}` and `AgentContentSafetyPolicy{PromptConstraints, BlockedOutputCategories, RestrictedOutputCategories, FailureHandling, AuditTreatment}`, stored at `AgentState.ContentSafety` with monotonic `AgentState.ContentSafetyPolicyVersion`. Their docs explicitly defer ENFORCEMENT to "Story 2.4 / Story 3.5 / Story 4.2". This story implements the 2.4 enforcement point.
- The effective policy = mode-specific override (`AutomaticModePolicy`/`ConfirmationModePolicy` based on snapshot `ResponseMode`) if present, else `ActivePolicy` (FR26: both modes use the active policy unless a stricter mode-specific policy is configured).
- `ContentSafetyFailureHandling.BlockWithAuditableOverride` exists but its override path is enforced later (Story 3.5 approver override). In 2.4, every safety failure BLOCKS — fail closed. No automatic-post or auto-approve bypass exists (the architecture authorizes none). [Source: src/Hexalith.Agents.Contracts/Agent/ContentSafetyFailureHandling.cs; prd FR27; ARCHITECTURE-SPINE.md#AD-14]
- The concrete safety filter engine/taxonomy is deferred (PRD OQ-9 / ARCHITECTURE-SPINE.md "Safety filters/content policy provider" deferred row): `DeferredContentSafetyEvaluator` fails closed. Wire the SHAPE (block-before-side-effect, fail-closed, MetadataOnly audit), not the engine — mirroring how Story 2.3 wired the bounded-context shape without a concrete bounded behavior.

### Cross-Module Boundaries

- **Conversations:** Re-read Source Conversation content through the existing `IConversationContextReader` seam (live `ConversationClientContextReader` over `IConversationClient.GetConversationAsync`, config-gated behind the `Conversations` section). Do NOT add a new Conversations dependency or write to Conversations (posting is Story 2.5). [Source: src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionContextOrchestrator.cs; ARCHITECTURE-SPINE.md#AD-9]
- **ProviderCatalog (in-module):** read `ProviderCatalogEntryView` via `IProviderCatalogReader` for budget/timeout/status. The catalog is in-module and references no provider SDK; the actual provider invocation is the NEW `IAgentGenerationProvider` adapter. [Source: src/Hexalith.Agents.Contracts/ProviderCatalog/ProviderCatalogEntryView.cs]
- **Agent aggregate (in-module):** read the effective `AgentContentSafetyPolicy` via the new `IAgentContentSafetyPolicyReader` (the snapshot only carries the version). [Source: src/Hexalith.Agents/Agent/AgentState.cs:99-102]
- **Provider SDK (NEW, adapter-local, deferred):** first real model invocation — lives behind `IAgentGenerationProvider`, deferred/config-gated; never in Contracts/domain/aggregate. [Source: ARCHITECTURE-SPINE.md#AD-9, Stack "Provider SDK Deferred; adapter-local when selected"]
- **EventStore:** events round-trip via plain `System.Text.Json`; enums survive by name (`[JsonConverter(typeof(JsonStringEnumConverter))]`). No `[PolymorphicSerialization]`/`[JsonDerivedType]` in the module — do not introduce any. [Source: src/Hexalith.Agents.Contracts/Hexalith.Agents.Contracts.csproj global usings]
- **Tenants/Parties:** not touched by this story.

### Latest Technical Information

- Stack baseline (do not change versions; Central Package Management — no inline versions): .NET SDK `10.0.300`–`10.0.301`, `net10.0`, `LangVersion 14`; `Microsoft.Agents.AI` / `Microsoft.Agents.AI.Workflows` `1.10.0`; Dapr packages / Dapr Workflow / `Dapr.AI.Microsoft.Extensions` `1.18.4`; `ModelContextProtocol` `1.4.0`; MediatR `14.1.0`; FluentValidation `12.1.1`; OpenTelemetry `1.15.x`–`1.16.0`; xUnit v3 `3.2.2`; Shouldly `4.3.0`; NSubstitute `5.3.0`. Provider SDK: deferred, adapter-local when selected. [Source: ARCHITECTURE-SPINE.md#Stack]
- The durable-owner runtime wiring (Agent Framework workflow / Dapr Workflow execution host) is an orchestration concern; in this story the orchestrator is the owner step and the live provider adapter stays deferred behind config — keep the default DI graph fail-closed. [Source: ARCHITECTURE-SPINE.md#AD-18]

### Testing Requirements

- **`Hexalith.Agents.Tests` (domain):** pure aggregate tests via static `Handle` + the `ProcessAndApplyAsync` reflection-dispatch + JSON round-trip + idempotent-NoOp + `Evaluate`/`Decide` no-drift. NO NSubstitute in aggregate logic. Cover the full outcome→event/status table, both rejection reasons, and terminal idempotency for all three terminal statuses.
- **`Hexalith.Agents.Contracts.Tests`:** round-trip every new record/enum; assert ordinals 0–6 unchanged + 7–9 added; AD-14 no-content-leak serialization assertions; ensure `ContractsSecretNonDisclosureTests` covers the new types; scope any "no attribute" assertion to `Hexalith`-namespaced attributes.
- **`Hexalith.Agents.Server.Tests`:** orchestrator tests with NSubstitute ports — happy path, every failure class, all-deferred-fails-closed, no content/error leak in dispatched envelope or result, end-to-end into the real aggregate, cancellation propagation. Assert decisions via the orchestrator's returned status (the policy is not visible to Server.Tests).
- Build/test loop is in Task 9. Require 0 warnings / 0 errors; test each project individually; write `tests/2-4-test-summary.md`.

### Project Structure Notes

New/changed code (no new projects — extend existing module):
- **Contracts** (`src/Hexalith.Agents.Contracts/AgentInteraction/`): modify `AgentInteractionStatus.cs`; add `AgentOutputGenerationFailureReason.cs`, `AgentGenerationKind.cs`, `AgentGeneratedVersion.cs`, `AgentGenerationAttemptEvidence.cs`, `AgentOutputGenerationResult.cs` (+ `AgentGenerationOutcome` enum), `AgentOutputNotGeneratableReason.cs`; `Commands/GenerateAgentOutput.cs`; `Events/AgentOutputGenerated.cs`, `Events/AgentOutputGenerationFailed.cs`; `Events/Rejections/AgentOutputNotGeneratableRejection.cs`. (Optionally extend `AgentInteractionStatusView.cs` additively for generation status — keep it content-free.)
- **Domain** (`src/Hexalith.Agents/AgentInteraction/`): modify `AgentInteractionAggregate.cs` (+ `Handle`), `AgentInteractionState.cs` (+ fields/Apply); add `AgentOutputGenerationPolicy.cs`.
- **Server** (`src/Hexalith.Agents.Server/`): `Ports/IAgentGenerationProvider.cs` + `DeferredAgentGenerationProvider.cs` + request/result types; `Ports/IContentSafetyEvaluator.cs` + `DeferredContentSafetyEvaluator.cs` + request/result types; `Ports/IAgentContentSafetyPolicyReader.cs` + `DeferredAgentContentSafetyPolicyReader.cs` + result type; `Application/AgentInteractions/AgentInteractionGenerationOrchestrator.cs` + `AgentInteractionGenerationRequest.cs`/`AgentInteractionGenerationOutcomeResult.cs`; modify `Program.cs`.
- **Tests:** add `AgentInteractionGenerationAggregateTests.cs`, `AgentInteractionGenerationContractsTests.cs`, `AgentInteractionGenerationOrchestratorTests.cs`; extend `AgentInteractionTestData.cs` + `AgentInteractionStateReplayTests.cs`; write `tests/2-4-test-summary.md`.

Discovery loaded: epics.md (Epic 2 full, Stories 2.4/2.5/3.1 boundaries, FR coverage), prd FR8–FR12/FR19–FR21/FR26–FR27 + NFR3/NFR6/NFR7, ARCHITECTURE-SPINE.md (AD-2/3/5/9/11/12/13/14/17/18 + Stack + Structural Seed + sequence) and the adversarial-boundaries review, the live AgentInteraction aggregate/state/policies/contracts (Stories 2.1–2.3), the Story 1.7 content-safety contracts, the Story 1.5 provider-catalog contracts, the three Server orchestrators + ports + DI, and the xUnit v3/Shouldly/NSubstitute test patterns + shared `AgentInteractionTestData`.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.4-Generate-And-Safety-Check-Agent-Output]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.5] (downstream automatic posting — out of scope here)
- [Source: _bmad-output/planning-artifacts/epics.md#Story-3.1] (downstream confirmation-mode proposal — out of scope here)
- [Source: prd FR9, FR10, FR12, FR19, FR20, FR21, FR26, FR27; NFR3, NFR6, NFR7]
- [Source: ARCHITECTURE-SPINE.md#AD-2 (AgentInteraction ownership), #AD-3 (aggregate purity), #AD-5 (append-only version history / nothing approvable on failure), #AD-9 (provider adapter boundary), #AD-11 (no partial output), #AD-12 (fail closed), #AD-13 (deterministic ids / idempotent retry), #AD-14 (redaction & secret safety), #AD-17 (test gates), #AD-18 (single durable owner)]
- [Source: ARCHITECTURE-SPINE.md#Structural-Seed, #Stack, #sequence-diagram (RecordGeneratedVersion / GenerationFailed)]
- [Source: reviews/review-adversarial-boundaries.md (provider/workflow SDK as adapter leaves; denial/failure audit without cross-tenant leakage; task-owner observable)]
- [Source: src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionStatus.cs (ordinals 0–6 to preserve)]
- [Source: src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs:143-180 (handler template)]
- [Source: src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs (state + Apply pattern)]
- [Source: src/Hexalith.Agents/AgentInteraction/AgentInteractionContextPolicy.cs (Evaluate/Decide twin-policy)]
- [Source: src/Hexalith.Agents.Contracts/AgentInteraction/Events/AgentInteractionContextReady.cs, AgentInteractionGateFailed.cs (event templates)]
- [Source: src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionContextEvidence.cs, AgentInteractionContextBlockReason.cs (value-object/reason-enum templates)]
- [Source: src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionSnapshot.cs:38-55 (ContentSafetyPolicyVersion already snapshotted for this story)]
- [Source: src/Hexalith.Agents.Contracts/Agent/AgentContentSafetyPolicy.cs, AgentContentSafetyConfiguration.cs, ContentSafetyFailureHandling.cs, ContentSafetyAuditTreatment.cs (safety contracts to enforce — Story 1.7)]
- [Source: src/Hexalith.Agents/Agent/AgentState.cs:99-102 (ContentSafety + ContentSafetyPolicyVersion)]
- [Source: src/Hexalith.Agents.Contracts/ProviderCatalog/ProviderCatalogEntryView.cs, ProviderModelTimeoutPolicy.cs, ProviderModelStatus.cs (Story 1.5 provider readiness/budget/timeout)]
- [Source: src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionContextOrchestrator.cs (orchestrator template; "generation is Story 2.4")]
- [Source: src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionIdentity.cs (deterministic id — reused, not re-derived)]
- [Source: src/Hexalith.Agents.Server/Program.cs (deferred-port + orchestrator DI pattern)]
- [Source: tests/Hexalith.Agents.Tests/AgentInteractionTestData.cs (shared fixtures + ApplyAll switch)]
- [Source: tests/Hexalith.Agents.Server.Tests/AgentInteractionContextOrchestratorTests.cs (orchestrator test pattern)]
- [Source: _bmad-output/implementation-artifacts/2-3-build-conversation-context-with-safe-bounds.md, 2-2-...md, 2-1-...md (prior-story learnings: CA1062 positive guard, `-m:1` serialized build, VSTest socket fallback, Nullable attribute scoping, internal policy not visible to Server.Tests, context text not carried forward)]
- [Source: Hexalith.Conversations/_bmad-output/project-context.md, Hexalith.Parties/_bmad-output/project-context.md, Hexalith.EventStore/_bmad-output/project-context.md, Hexalith.Tenants/_bmad-output/project-context.md (sibling-module context)]

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created. Story status is `ready-for-dev`.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (1M context) — `claude-opus-4-8[1m]`

### Debug Log References

- `dotnet build src/Hexalith.Agents/Hexalith.Agents.csproj --configuration Release -m:1` → 0 warnings / 0 errors (domain + contracts, early gate).
- `dotnet build src/Hexalith.Agents.Server/Hexalith.Agents.Server.csproj --configuration Release -m:1` → 0 warnings / 0 errors (server, early gate).
- `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` → 0 warnings / 0 errors (full solution incl. all test projects).
- One test fix during the loop: the Contracts AD-14 "no content member" guard initially used substring matching and tripped the safe `PromptTokenCount` count; switched to exact-name matching (mirroring the Story 2.3 context-evidence guard). No production code changed for it.
- Tests run per-project (not solution-wide) per the Story 2.2 VSTest-socket learning; no `SocketException` occurred so the direct-executable fallback was not needed.

### Completion Notes List

- **Generation + content-safety step only.** Implemented the three additive `AgentInteractionStatus` values (7–9, ordinals 0–6 preserved), the generation value objects/enums, the `GenerateAgentOutput` command, the `AgentOutputGenerated`/`AgentOutputGenerationFailed` events + `AgentOutputNotGeneratableRejection`, the pure `AgentOutputGenerationPolicy` (Evaluate/Decide twin), the aggregate's 4th `Handle` + state fields/Apply, the provider-generation/content-safety-evaluator/content-safety-policy-reader ports (all deferred + fail-closed), the `AgentInteractionGenerationOrchestrator`, and the DI wiring. Did NOT implement Conversation posting (2.5), proposals (3.1), edit/regenerate/approve (Epic 3), a concrete provider SDK, or a concrete safety filter engine (deferred).
- **AD-3 purity:** the aggregate `Handle(GenerateAgentOutput)` emits events only; all impure work (conversation re-read, catalog read, provider invocation, safety eval) is in the orchestrator and returns through the single command.
- **AD-9/AD-14 secret + content safety:** provider SDK/credentials/payloads/errors stay behind `IAgentGenerationProvider`; generated content lives ONLY on `AgentOutputGenerated` + `AgentInteractionState.GeneratedVersions`; it is absent from the failure event, attempt evidence, rejection, outcome result, and the dispatched envelope on any failure (orchestrator carries content onto the result only on `Succeeded`). Verified by serialized no-leak assertions in all three test layers.
- **AD-5 nothing-approvable-on-failure:** the policy emits no `AgentGeneratedVersion` on any failure (incl. safety block) — structurally nothing approvable.
- **AD-13 deterministic/idempotent:** `AttemptId = attempt-{interactionId}`, `VersionId = version-{AttemptId}` (pure); re-dispatch after any terminal `Generated`/`GenerationFailed`/`SafetyFailed` is a `NoOp` preserving version history.
- **Fail closed:** the default all-deferred DI graph blocks generation (conversation re-read deferred → `InvalidContext`; with content loaded, the deferred provider → `ProviderUnavailable`; deferred safety evaluator → `Blocked`/`ContentSafetyBlocked`; deferred policy reader → `PolicyFailure`). All → `GenerationFailed`/`SafetyFailed`, never a partial message.
- **Shared-policy no-drift:** the orchestrator returns status via `AgentOutputGenerationPolicy.Decide` (internal, visible to `…Server` via `InternalsVisibleTo`, NOT to `…Server.Tests` — asserted via the orchestrator's returned status, Story 2.3 learning).
- **Tests:** domain 492, contracts 159, server 202, UI regression 156 — all green. 0 warnings / 0 errors. (Counts include the QA gap-fill pass.) See `tests/2-4-test-summary.md`.

### File List

**Contracts** (`src/Hexalith.Agents.Contracts/`)
- `AgentInteraction/AgentInteractionStatus.cs` (modified — appended `Generated`/`GenerationFailed`/`SafetyFailed`)
- `AgentInteraction/AgentOutputGenerationFailureReason.cs` (new)
- `AgentInteraction/AgentGenerationKind.cs` (new)
- `AgentInteraction/AgentGeneratedVersion.cs` (new)
- `AgentInteraction/AgentGenerationAttemptEvidence.cs` (new)
- `AgentInteraction/AgentOutputGenerationResult.cs` (new — `AgentGenerationOutcome` enum + result record)
- `AgentInteraction/AgentOutputNotGeneratableReason.cs` (new)
- `AgentInteraction/Commands/GenerateAgentOutput.cs` (new)
- `AgentInteraction/Events/AgentOutputGenerated.cs` (new)
- `AgentInteraction/Events/AgentOutputGenerationFailed.cs` (new)
- `AgentInteraction/Events/Rejections/AgentOutputNotGeneratableRejection.cs` (new)

**Domain** (`src/Hexalith.Agents/`)
- `AgentInteraction/AgentInteractionAggregate.cs` (modified — `Handle(GenerateAgentOutput)`)
- `AgentInteraction/AgentInteractionState.cs` (modified — `GeneratedVersions`/`GenerationFailureReason` + 3 `Apply`)
- `AgentInteraction/AgentOutputGenerationPolicy.cs` (new)

**Server** (`src/Hexalith.Agents.Server/`)
- `Ports/IAgentGenerationProvider.cs` (new)
- `Ports/AgentGenerationProviderRequest.cs` (new)
- `Ports/AgentGenerationProviderResult.cs` (new)
- `Ports/DeferredAgentGenerationProvider.cs` (new)
- `Ports/IContentSafetyEvaluator.cs` (new)
- `Ports/ContentSafetyEvaluationRequest.cs` (new)
- `Ports/ContentSafetyEvaluationResult.cs` (new — `ContentSafetyVerdict` enum + result record)
- `Ports/DeferredContentSafetyEvaluator.cs` (new)
- `Ports/IAgentContentSafetyPolicyReader.cs` (new)
- `Ports/AgentContentSafetyPolicyReadResult.cs` (new)
- `Ports/DeferredAgentContentSafetyPolicyReader.cs` (new)
- `Application/AgentInteractions/AgentInteractionGenerationOrchestrator.cs` (new)
- `Application/AgentInteractions/AgentInteractionGenerationRequest.cs` (new — request + outcome-result records)
- `Program.cs` (modified — Story 2.4 DI block)

**Tests**
- `tests/Hexalith.Agents.Tests/AgentInteractionTestData.cs` (modified — generation fixtures + `ApplyAll` cases)
- `tests/Hexalith.Agents.Tests/AgentInteractionGenerationAggregateTests.cs` (new)
- `tests/Hexalith.Agents.Tests/AgentInteractionStateReplayTests.cs` (modified — generation replay tests)
- `tests/Hexalith.Agents.Contracts.Tests/AgentInteractionGenerationContractsTests.cs` (new)
- `tests/Hexalith.Agents.Server.Tests/AgentInteractionGenerationOrchestratorTests.cs` (new)
- `_bmad-output/implementation-artifacts/tests/2-4-test-summary.md` (new)

### Change Log

| Date | Version | Description | Author |
|------|---------|-------------|--------|
| 2026-06-24 | 0.1 | Initial story context created (ready-for-dev) | Administrator |
| 2026-06-24 | 1.0 | Implemented generation + content-safety step (all 9 tasks); 0 warnings/0 errors; domain 489 / contracts 158 / server 197 tests green; status → review | Amelia (Dev Agent) |
| 2026-06-24 | 1.1 | Adversarial code review (auto-fix): verified all ACs/tasks against implementation; build 0/0 and all suites re-run green (domain 492 / contracts 159 / server 202 / UI 156); fixed `AgentGenerationProviderRequest.ContextPayload` doc accuracy and refreshed stale test counts in Completion Notes; no CRITICAL issues; status → done | Senior Developer Review (AI) |

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot (autonomous review) · **Date:** 2026-06-24 · **Outcome:** ✅ Approve

**Scope:** Full adversarial review of Story 2.4 against the four acceptance criteria, the nine tasks, and the architecture guardrails (AD-3/5/9/11/12/13/14/18). Read every file in the File List plus the templates each was meant to mirror; cross-referenced the git working tree against the claimed File List; rebuilt the solution and re-ran every test project individually.

**Verification performed:**
- **Build:** `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` → **0 warnings / 0 errors** (re-confirmed after the review edits).
- **Tests (re-run, not trusted from the summary):** domain **492/492**, contracts **159/159**, server **202/202**, UI regression **156/156** — all green.
- **Git vs File List:** exact match — every new file exists in the tree and every modified file is modified; no undocumented source changes, no phantom entries.

**AC validation:**
- **AC1 (provider behind safe boundary):** PASS — `IAgentGenerationProvider` is the sole model seam; no provider-SDK type/credential/payload reaches Contracts, the domain, or the aggregate; events expose only `ProviderId`/`ModelId`/capability+policy versions/usage. Provider-throw test proves raw errors never cross the boundary.
- **AC2 (safety gates before any artifact):** PASS — orchestrator evaluates content safety *before* dispatch; a block yields `SafetyFailed` and the policy emits **no** `AgentGeneratedVersion`.
- **AC3 (fail closed, never partial, no leak):** PASS — every failure class maps to `AgentOutputGenerationFailed` with safe evidence and no content; verified by no-leak serialization assertions across the failure event, evidence, rejection, dispatched envelope, and returned outcome.
- **AC4 (deterministic, idempotent, auditable):** PASS — `AttemptId = attempt-{interactionId}`, `VersionId = version-{AttemptId}` (pure); terminal `Generated`/`GenerationFailed`/`SafetyFailed` re-dispatch is a `NoOp` preserving version history.

**Findings (no Critical/High; auto-fixed):**
- **[Low — fixed] Doc accuracy:** `AgentGenerationProviderRequest.ContextPayload` documented the payload as "caller prompt + re-read content", but `BuildContextPayload` assembles only the re-read Source Conversation messages (the orchestrator carries no separate prompt field; the prompt is the latest conversation message). Corrected the summary + param docs to match the implementation.
- **[Low — fixed] Stale metrics:** the Completion Notes List quoted pre-gap-fill test counts (489/158/197); updated to the current 492/159/202 to match `tests/2-4-test-summary.md`.

**Observations (no change required):**
- **[Low — accepted]** `ReadBudgetAsync` maps any non-`Enabled` catalog status (incl. the reserved `Degraded`/`Failed`) to `ProviderDisabled` rather than `ProviderUnavailable`. Both terminate at `GenerationFailed` and fail closed, so there is no behavioural/audit-status difference in V1; `Degraded`/`Failed` are explicitly reserved for a later runtime-health story. Left as-is.
- Code quality is exemplary: faithful mirroring of the context-orchestrator/aggregate/policy templates, thorough fail-closed exception scoping (`when (ex is not OperationCanceledException)`), CA1062-safe positive guards, and high-signal tests (real assertions, cross-seam end-to-end into the live aggregate, AD-14 no-leak guards, idempotency, cancellation propagation).
