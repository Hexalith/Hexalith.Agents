---
baseline_commit: e3e4d1d28000d4bf5d648db48c57898657b06788
---

# Story 4.4: Define And Enforce Launch Readiness Gates

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a Release Operator,
I want explicit launch-readiness controls for metrics, latency, context, safety, and cost,
so that production-like generation cannot be enabled on implicit assumptions.

## Acceptance Criteria

**AC1 — Readiness decisions are recorded and gate production-like enablement**

**Given** production or production-like generation is requested
**When** launch readiness is evaluated
**Then** Content Safety Policy, Conversation Context Policy, launch metric thresholds, latency targets, and cost-control posture must be recorded
**And** missing readiness decisions block enablement.

**AC2 — Launch metric definitions are complete and classified**

**Given** SM-2 or SM-3 is used for launch readiness
**When** readiness configuration is saved
**Then** each metric defines numerator, denominator, target, measurement window, and launch cohort
**And** the system distinguishes primary metrics, secondary metrics, and counter-metrics.

**AC3 — Latency targets per response mode and cost-control posture are explicit**

**Given** latency and cost posture are configured
**When** readiness is inspected
**Then** Automatic Response Mode and Confirmation Response Mode have explicit latency targets
**And** cost controls are recorded as quotas, budgets, Provider/model limits, reporting-only monitoring, or explicitly accepted launch risk.

**AC4 — Failed gates keep generation disabled and surface safe blockers**

**Given** readiness gates fail
**When** administrators inspect status or attempt enablement
**Then** generation remains disabled for production-like launch validation
**And** blockers are visible through API/client contracts and admin UI without exposing secrets or unsafe content.

## Tasks / Subtasks

- [x] **Task 1 — Launch-readiness contracts (value objects + enums)** (AC: #1, #2, #3)
  - [x] Add `Contracts/Agent/LaunchMetricClassification.cs` enum: `Unknown = 0, Primary, Secondary, Counter` with `[JsonConverter(typeof(JsonStringEnumConverter))]`.
  - [x] Add `Contracts/Agent/LaunchMetricDefinition.cs` record carrying: `string MetricId` (e.g. `"SM-2"`), `LaunchMetricClassification Classification`, `string Numerator`, `string Denominator`, `decimal Target` (threshold value), `string MeasurementWindow`, `string LaunchCohort`. All string fields are non-empty governance descriptors.
  - [x] Add `Contracts/Agent/CostControlPosture.cs` enum: `Unknown = 0, Quotas, Budgets, ProviderModelLimits, ReportingOnlyMonitoring, AcceptedLaunchRisk`.
  - [x] Add `Contracts/Agent/ResponseModeLatencyTarget.cs` record: `AgentResponseMode Mode`, `int TargetMilliseconds` (single deterministic scalar — concrete SLO values are deferred per OQ-5, so do not over-engineer a percentile type). Reuse the existing `AgentResponseMode` enum (`Automatic`/`Confirmation`).
  - [x] Add `Contracts/Agent/AgentLaunchReadiness.cs` aggregate value object bundling: `IReadOnlyList<LaunchMetricDefinition> Metrics`, `IReadOnlyList<ResponseModeLatencyTarget> LatencyTargets`, `CostControlPosture CostPosture`, optional `string? CostPostureNote`, and `string ContextPolicyReference` (confirms the in-force context policy; V1 default `full-conversation-v1`). Model this value object on `Contracts/Agent/AgentContentSafetyConfiguration.cs`. Carry NO secrets, raw payloads, provider SDK types, or Party PII.
  - [x] Add `Contracts/Agent/AgentLaunchReadinessBlocker.cs` enum (parallel to `AgentActivationBlocker`): `Unknown = 0, MissingContentSafetyPolicy, MissingContextPolicy, MissingLaunchMetrics, IncompleteLaunchMetricDefinition, MissingAutomaticLatencyTarget, MissingConfirmationLatencyTarget, MissingCostControlPosture, UnresolvedAuditGovernance`. `[JsonStringEnumConverter]`, ordinals stable.

- [x] **Task 2 — Record-readiness command/event/rejection + aggregate handling** (AC: #1, #2, #3)
  - [x] Add command `Contracts/Agent/Commands/RecordAgentLaunchReadiness.cs`: `public record RecordAgentLaunchReadiness(AgentLaunchReadiness Readiness);` (bare record, no interface, AgentId from envelope). Mirror `Commands/ConfigureAgentContentSafetyPolicy.cs`.
  - [x] Add event `Contracts/Agent/Events/AgentLaunchReadinessRecorded.cs`: `public record AgentLaunchReadinessRecorded(string AgentId, AgentLaunchReadiness Readiness, int LaunchReadinessVersion, int ConfigurationVersion) : IEventPayload;`. Mirror `Events/AgentContentSafetyPolicyConfigured.cs`. NO wall-clock timestamp on the payload.
  - [x] Add rejection `Contracts/Agent/Events/Rejections/AgentLaunchReadinessRejection.cs` for validation failures (invalid/empty metric fields, malformed latency targets, etc.), implementing `IRejectionEvent`.
  - [x] Add `Handle(RecordAgentLaunchReadiness, AgentState?, CommandEnvelope)` to `Agent/AgentAggregate.cs`: guard cascade `IsAgentAdmin` → `Denied`; not-found → reject; validate + normalize the readiness payload (each metric has all 5 fields non-empty + classification set; latency list well-formed; cost posture not `Unknown`) → `Invalid` rejection on failure; idempotent re-assert of an equal value → `DomainResult.NoOp()` (compare list members element-wise with `SequenceEqual`, mirroring `ContentSafetyConfigurationsEqual`); on a genuine change emit one `AgentLaunchReadinessRecorded` bumping both `LaunchReadinessVersion + 1` and `ConfigurationVersion + 1`.
  - [x] Extend `Agent/AgentState.cs`: add `AgentLaunchReadiness? LaunchReadiness` + `int LaunchReadinessVersion` fields, one `Apply(AgentLaunchReadinessRecorded)` overload (guarded by `if (!IsCreated) return;`), and a no-op `Apply(AgentLaunchReadinessRejection)` via `MarkReplayOnlyEventHandled()`.

- [x] **Task 3 — Production-like enablement gate (the launch-readiness gate)** (AC: #1, #4)
  - [x] Add pure policy `Agent/AgentLaunchReadinessPolicy.cs` (`internal static`) with `ComputeLaunchReadinessBlockers(...)` returning an ordered `IReadOnlyList<AgentLaunchReadinessBlocker>`. Inputs are pure state values: `hasContentSafetyPolicy`, `hasContextPolicy`, the recorded `AgentLaunchReadiness?` (metrics/latency/cost), and the audit-governance-resolved flag. Append blockers in a stable deterministic order: content safety → context policy → launch metrics present → metric definitions complete → automatic latency target → confirmation latency target → cost posture → audit governance. These are **pure state checks** — no trusted verdict, no dependency read (the values are internal Agent state). Mirror `Agent/AgentConfigurationPolicy.cs` `ComputeActivationBlockers`.
  - [x] Add command `Contracts/Agent/Commands/EnableProductionLikeGeneration.cs` and event `Contracts/Agent/Events/AgentProductionLikeGenerationEnabled.cs : IEventPayload` + rejection `Contracts/Agent/Events/Rejections/AgentProductionLikeGenerationBlockedRejection.cs` carrying `string AgentId` + `IReadOnlyList<AgentLaunchReadinessBlocker> Blockers`.
  - [x] Add `Handle(EnableProductionLikeGeneration, AgentState?, CommandEnvelope)` to `AgentAggregate.cs`: `IsAgentAdmin` gate → `Denied`; not-found → reject; compute blockers via `AgentLaunchReadinessPolicy`; if any blocker → `DomainResult.Rejection([AgentProductionLikeGenerationBlockedRejection(...)])` (fail closed); else emit `AgentProductionLikeGenerationEnabled` and set `ProductionLikeGenerationEnabled = true` + bump `ConfigurationVersion`. Idempotent re-enable → `NoOp`.
  - [x] Extend `AgentState.cs` with `bool ProductionLikeGenerationEnabled` + `Apply(AgentProductionLikeGenerationEnabled)` + no-op rejection `Apply`.
  - [x] **Keep baseline `ActivateAgent` (Story 1.7) untouched.** Launch readiness is the higher, distinct gate for production-like generation — do not fold launch-readiness blockers into `ComputeActivationBlockers`; the two gates are separate (baseline activation = dev/staging; production-like enablement = this story).

- [x] **Task 4 — Surface readiness state through the status read path** (AC: #4)
  - [x] Add a pure read view `Contracts/Agent/AgentLaunchReadinessView.cs` (governance data + presence/blockers only): the recorded `Metrics`, `LatencyTargets`, `CostPosture`, `LaunchReadinessVersion`, `bool HasContentSafetyPolicy`, `bool HasContextPolicy`, `bool ProductionLikeGenerationEnabled`, and `IReadOnlyList<AgentLaunchReadinessBlocker> Blockers`. NO secrets/unsafe content/raw payloads; keep free-text descriptors out of any telemetry dimension.
  - [x] Build it via a pure inspection (`Agent/AgentLaunchReadinessInspection.cs` or extend `Agent/AgentInspection.cs`) `(state, bool isAuthorized, bool auditGovernanceResolved) → result`, fail closed (`!isAuthorized → NotAuthorized()`; null/missing state → `NotFound()`, indistinguishable). Reuse `ComputeLaunchReadinessBlockers` so the view's blockers stay in lock-step with what enablement would reject. **The `IAgentAuditGovernanceReadinessProvider` port is read in the Server orchestration/read layer and the resolved result is passed in as the `auditGovernanceResolved` bool — never read a port from the pure aggregate/policy (AD-3).**
  - [x] Additively extend `Contracts/Agent/AgentStatusView.cs`: append the two new fields **after** the existing last positional parameter `ActivationBlockers`, preserving all existing positional/JSON order (AD-17) — add `IReadOnlyList<AgentLaunchReadinessBlocker> LaunchReadinessBlockers` and `bool ProductionLikeGenerationEnabled`, populated in `AgentInspection.ToView` so the existing readiness badge reflects the gate state.
  - [x] Surface launch-readiness blockers in the operational status summary by **additively adding a new field** `IReadOnlyList<AgentLaunchReadinessBlocker> LaunchReadinessBlockers` (appended last, with round-trip/ordinal/no-leak tests) to `Contracts/Operations/AgentOperationalStatusSummaryView.cs`. Do **not** merge them into the existing `ReadinessBlockers` (typed `IReadOnlyList<AgentActivationBlocker>`) or `AuditGovernanceBlockers` (typed `IReadOnlyList<string>`) — the new blocker is a distinct enum and will not compile into either list. Also consume the already-published `AgentAuditGovernanceReadiness` blocker (built in Story 4.2 "so Story 4.4 can consume it") via the `UnresolvedAuditGovernance` launch-readiness blocker.

- [x] **Task 5 — Public API + client contracts** (AC: #1, #4)
  - [x] Add to `IAgentAdministrationOperations`: `RecordLaunchReadinessAsync(...)` and `EnableProductionLikeGenerationAsync(...)` returning `AgentOperationResult`/`AgentOperationResult<T>`. Mirror `ConfigureContentSafetyPolicyAsync`.
  - [x] Add to `IAgentStatusOperations`: `GetAgentLaunchReadinessAsync(string agentId, AgentOperationOptions? options = null, CancellationToken ct = default)` returning `AgentOperationResult<AgentLaunchReadinessView>`. Mirror `GetAgentReadinessAsync`.
  - [x] Add fail-closed stubs for **every** new method in `Client/UnavailableOperations.cs` (`UnavailableAgentAdministrationOperations` → `Unavailable.Command(...)`; `UnavailableAgentStatusOperations` → reuse the existing private `Id<T>(agentId)` helper in that class, which delegates to `Unavailable.Typed<T>()`) — the build breaks otherwise.
  - [x] Map the new routes in `Server/Api/AgentsOperationEndpoints.cs`: POST `RecordLaunchReadiness` + POST `EnableProductionLikeGeneration` under `MapAgentAdministration`; GET launch-readiness under `MapStatus`. Endpoints delegate to `IAgentsClient` only (no logic).
  - [x] On a blocked enablement, return `AgentOperationStatus.Blocked` / `AgentOperationErrorCode.Blocked` with a deterministic safe message — never raw exception/rejection text. `IsSuccess` must remain `Status == Succeeded` only; pending/checking/blocked is never success.

- [x] **Task 6 — Invocation-gate readiness check (additive, fail-closed; live binding deferred)** (AC: #4)
  - [x] Additively extend `Contracts/AgentInteraction/AgentInteractionGateCheck.cs` with a `LaunchReadiness` check value (append, preserve ordinals).
  - [x] Add a `ProductionLikeGenerationEnabled` field to `Server/Ports/AgentInvocationReadiness.cs`. In `Server/Application/AgentInteractions/AgentInteractionGateOrchestrator.cs`, add a 10th verdict `new(AgentInteractionGateCheck.LaunchReadiness, MapLaunchReadiness(readiness))` to the verdict list, where `MapLaunchReadiness` returns the fail-closed Unavailable outcome when `!readiness.IsAvailable`, the Denied/blocked outcome when `!readiness.ProductionLikeGenerationEnabled`, and Allowed otherwise — mirroring the existing `MapContentSafety`/`MapXxx` helpers. The pure `AgentInvocationGatePolicy.Decide` needs no change. `DeferredAgentInvocationReadinessReader` already returns `NotAvailable`, so the whole default DI graph keeps failing closed → `Denied`. **Live read-model binding stays deferred (AD-3 purity + AD-12 fail-closed; deferred per ARCHITECTURE-SPINE.md#Deferred); do not bind projections or a live readiness reader in this story.**

- [x] **Task 7 — Admin UI: Launch Readiness surface** (AC: #2, #3, #4)
  - [x] Add page `UI/Components/Pages/LaunchReadiness.razor` at `@page "/agents/launch-readiness"` with `@attribute [Authorize(Policy = AgentsFrontComposerRegistration.AgentsAdministratorPolicy)]`. Let a Release Operator view/record metric definitions (numerator/denominator/target/window/cohort + classification), per-mode latency targets (Automatic + Confirmation), and cost-control posture; show launch-readiness blockers grouped by recovery action and the production-like-enablement state. Mirror `Components/Pages/OperationalStatus.razor`.
  - [x] Add shared `UI/Components/Shared/LaunchReadinessPanel.razor` + pure presentation mapper `UI/Components/Shared/LaunchReadinessPresentation.cs` mapping `AgentLaunchReadinessBlocker` → resource keys + `RecoveryActionGroup`. `RecoveryActionGroup` is a closed enum (`ConfigureProvider, LinkPartyIdentity, FixPolicy, WaitForApproval, RetryGeneration, InspectAudit, StartNewCall, None`) — map every new blocker explicitly: `MissingContentSafetyPolicy`, `MissingContextPolicy`, `MissingLaunchMetrics`, `IncompleteLaunchMetricDefinition`, `MissingAutomaticLatencyTarget`, `MissingConfirmationLatencyTarget`, `MissingCostControlPosture` → `FixPolicy`; `UnresolvedAuditGovernance` → `InspectAudit`. Mirror the existing `OperationalStatusPresentation.GroupForBlocker(AgentActivationBlocker)` switch and reuse the `AgentReadiness.BlockerKeyFor` mapping idiom.
  - [x] Route all non-success states through `AgentSurfaceState` (all eight `AgentSurfaceKind`: Loading, Empty, FilteredEmpty, Error, PermissionDenied, Stale, Degraded, Unavailable). Use `FluentBadge` with semantic color + icon + visible text (never color-only); `status-success` ONLY when all gates pass; `status-severe` for blocked readiness; `status-warning` for latency/cost-attention.
  - [x] Add gateway `UI/Services/Gateways/ILaunchReadinessGateway.cs` + `UI/Services/Gateways/DeferredLaunchReadinessGateway.cs` (fail-closed: `NotAuthorized()`/`Unavailable()`) + a result wrapper modeled on `AgentOperationalStatusSummaryResult`. Register via `TryAddScoped` in `UI/Services/Gateways/AgentsUiServiceCollectionExtensions.cs` (`AddAgentsUi`) — note this file lives under `Services/Gateways/`, not `Services/`. UI references only `Contracts` + `Client`.
  - [x] Add nav entry **Order 8** in `UI/Composition/AgentsFrontComposerRegistration.cs` (gated by `AgentsAdministratorPolicy`), following the existing eight ordered entries.
  - [x] Add all new strings to **both** `UI/Resources/AgentsResources.resx` **and** `AgentsResources.fr.resx` (en/fr parity is auto-enforced): page title, field labels, metric-classification labels, cost-posture option labels, latency labels per mode, blocker messages, enable-button, status messages. Whole localized strings only — no runtime fragment assembly.

- [x] **Task 8 — Tests** (AC: #1, #2, #3, #4)
  - [x] Domain (`tests/Hexalith.Agents.Tests`): handler tests for `RecordAgentLaunchReadiness` and `EnableProductionLikeGeneration` — auth fail-closed (`IsAgentAdmin` → `Denied`), not-found, validation rejections (incomplete metric fields, missing latency target per mode, `Unknown` cost posture), idempotent `NoOp` on re-assert, version bumps, and blocker-computation purity (blockers derive from state with any/absent envelope verdicts). Add the new events to `AgentTestData.ApplyAll` (throwing `default:`) and add state builders (`StateWithLaunchReadiness`, `StateLaunchReady`). Mirror `AgentContentSafetyActivationTests.cs`.
  - [x] Contracts (`tests/Hexalith.Agents.Contracts.Tests`): add JSON round-trip cases for new events/rejections/value objects to `AgentContractsRoundTripTests.cs`; confirm `ContractsSecretNonDisclosureTests` passes (no member names containing `Secret`/`ApiKey`/`Credential`/`Password`/`ConnectionString`); add ordinal/additive tests for the new enums.
  - [x] Server (`tests/Hexalith.Agents.Server.Tests`): endpoint tests for the new routes (success + blocked + fail-closed), orchestrator test for the `LaunchReadiness` verdict mapping (NSubstitute on ports), and keep structural guards green (`ProjectReferenceDirectionTests`, `PublicContractPackageBoundaryTests`, `PackageVersionCentralizationTests`).
  - [x] UI (`tests/Hexalith.Agents.UI.Tests`): bUnit page tests — default DI graph renders `PermissionDenied`/`Unavailable` (fail-closed is correct, not a bug); all eight surface states render distinctly; accessibility (color + icon + visible text, accessible names, no secret/unsafe/cross-tenant leakage in markup/`aria-label`/`data-testid`/copied text); navigation test (Order 8 entry + policy gating); `LocalizationResourceTests` en/fr parity covers new keys; badge conformance. Substitute the gateway with NSubstitute (the `Deferred*` placeholder is not exercised).

- [x] **Task 9 — Build, run, and reconcile Dev Agent Record** (AC: all)
  - [x] `dotnet build Hexalith.Agents.slnx -c Release -m:1` → **0 warnings / 0 errors** (warnings-as-errors; `CA2007` `ConfigureAwait(false)` is an error; nullable clean).
  - [x] Run each touched test project **individually** (not solution-level `dotnet test`). On VSTest `SocketException (13)`, run the built xUnit v3 executables directly.
  - [x] **Regenerate test counts from the actual Release run and diff the File List against `git status --short` before marking review** (recurring Epic 2/3/4 finding — stale counts / File List omissions).

## Dev Notes

This is a **record-and-gate** story modeled almost exactly on **Story 1.7 (Content Safety Policy + activation gate)**: add safe launch-readiness contracts, record them on the `Agent` aggregate via a command/event + state field + monotonic version, gate production-like enablement with a **pure state check** (no resolver, no trusted verdict), and surface presence/blockers through the existing status/API/UI machinery. It does **not** enforce quotas/latency at runtime (deferred) and does **not** re-implement context bounding (already done in Story 2.3). [Source: epics.md#Story-4.4; ARCHITECTURE-SPINE.md#Deferred]

### Critical context — extend, do not reinvent

- **Content Safety Policy is already recorded** on the Agent aggregate as `state.ContentSafety` (Story 1.7) and is already an activation blocker (`AgentActivationBlocker.MissingContentSafetyPolicy = 10`). The launch-readiness gate **references/confirms** it (`state.ContentSafety is not null`); it does not redefine it. [Source: Hexalith.Agents/Agent/AgentAggregate.cs; AgentConfigurationPolicy.cs]
- **Conversation Context Policy is already recorded** as `ContextPolicyReference` (V1 default `full-conversation-v1`) per AD-4/AD-11 and carried on `AgentInteractionContextEvidence`. The readiness gate confirms a context policy is in force; it does not re-implement context bounding (Story 2.3). [Source: ARCHITECTURE-SPINE.md#AD-4 line 94; AgentInteractionContextPolicy.cs]
- **The "named launch-readiness blocker" precedent already exists**: `Contracts/Operations/AgentAuditGovernanceReadiness.cs` (`IReadOnlyList<string> Blockers` + const `RetentionLegalHoldExportDeletionPolicyUnresolved` + static `MetadataOnlyBlocked`), with port `Server/Ports/IAgentAuditGovernanceReadinessProvider.cs`. It was built in Story 4.2 explicitly "so Story 4.4 can consume it" — consume it as the `UnresolvedAuditGovernance` launch-readiness blocker. [Source: 4-2 dev notes; Contracts/Operations/AgentAuditGovernanceReadiness.cs]
- **The activation-gate machinery to mirror**: `AgentConfigurationPolicy.ComputeActivationBlockers(...)` is a pure `internal static` method shared by the write path (`AgentAggregate.Handle(ActivateAgent)`) and the read path (`AgentInspection.ToView`) so the status view's blockers always match what enablement would reject. Build the launch-readiness gate the same way (one pure policy, called from both the enablement handler and the read view). [Source: Hexalith.Agents/Agent/AgentConfigurationPolicy.cs lines 108–178; AgentInspection.cs]
- **No new aggregate** — launch readiness gates *this Agent's* production-like enablement, so it attaches to the existing `Agent` aggregate alongside content-safety/response-mode/approver-policy. Do not create a tenant-wide aggregate or fold onto `ProviderCatalog`/`AgentInteraction`. [Source: ARCHITECTURE-SPINE.md#AD-2]

### Architecture invariants (non-negotiable)

- **Pure aggregates (AD-3):** `Handle` emits events only — no I/O, no `DateTimeOffset.UtcNow`, no `Guid.NewGuid`, no dependency reads. Events carry no wall-clock timestamp (occurrence time comes from EventStore metadata; actor from `envelope.UserId`). [Source: ARCHITECTURE-SPINE.md#AD-3, #Consistency-Conventions line 272]
- **Self-contained config model (the 1.7 model):** launch-readiness values are *internal Agent state*, so the gate is a pure state check that **cannot be bypassed by a direct-gateway enablement command** (an empty/invalid readiness is rejected at recording time, so "present ≡ valid"). Do **not** add a resolver port or a `…:validation` envelope extension for these values. [Source: 4-2/1.7 dev notes]
- **Authorization:** gate every command with `IsAgentAdmin` (`actor:agentsAdmin`, fail closed to `Unknown`). The "Release Operator" role folds under `actor:agentsAdmin` for V1 (the distinct release-operator authorization model is itself deferred). Strip client-supplied envelope extension keys at the entry point; repopulate from trusted claims server-side. [Source: AgentAggregate.cs IsAgentAdmin gate; 1.7 dev notes line 103]
- **Idempotency (AD-13):** re-asserting an equal value → `DomainResult.NoOp()`. Record value-equality does **not** deep-compare `IReadOnlyList<>` members — compare lists element-wise with `SequenceEqual` (see `ContentSafetyConfigurationsEqual`). [Source: ARCHITECTURE-SPINE.md#AD-13; AgentAggregate.cs ~lines 793–810]
- **Versioning:** a genuine change bumps a feature-specific monotonic version (`LaunchReadinessVersion`) **and** `ConfigurationVersion`. A readiness change should bump `ConfigurationVersion` so future `AgentInteraction` snapshots pick up the new posture; historical interactions are unaffected. [Source: ARCHITECTURE-SPINE.md#AD-4]
- **Additive-first (AD-17):** append enum values preserving ordinals; append new record fields **after** all existing positional parameters (so the new fields land after `AgentStatusView.ActivationBlockers`, preserving every existing parameter's order). Every additive contract gets round-trip + ordinal + no-leak tests. [Source: ARCHITECTURE-SPINE.md#AD-17]
- **Fail-closed everywhere (AD-12):** missing readiness → enablement blocked; deferred readers/gateways → `NotAvailable`/`Unavailable`/`NotAuthorized`. Any readiness signal with no live source renders "not available yet" / blocked, never affirmatively ready (Story 1.8 HIGH-finding lesson). [Source: ARCHITECTURE-SPINE.md#AD-12; 1.8 dev notes]
- **Secret & content safety (AD-14):** no secrets, raw provider payloads, raw content, stack traces, Party PII, or cross-tenant data in contracts, status, blockers, telemetry dimensions, `aria-label`, tooltips, copied text, or diagnostics. Metric/latency/cost values are safe governance data, but keep free-text descriptors out of telemetry dimensions. [Source: ARCHITECTURE-SPINE.md#AD-14; DESIGN.md; EXPERIENCE.md Voice and Tone]
- **UI/API parity (AD-15):** admin UI and API/client share the same public contracts and authorization outcomes; the readiness blocker surface is identical across both. [Source: ARCHITECTURE-SPINE.md#AD-15]
- **Never collapse status / never pending-as-success (AD-5):** `Checking ≠ Callable`; blocked readiness ≠ ready; `ProductionLikeGenerationEnabled` only when all gates pass.

### Canonical status vocabulary (reuse 1:1; the UX uses named states, not UX-DR-N IDs)

- **Agent Readiness states:** `callable, checking, invalid configuration, missing party identity, provider unavailable, disabled`. The readiness badge shows `status-success` **only when all required gates pass**; it must **explain blockers, not hide them**, grouped by recovery action (not raw subsystem). [Source: EXPERIENCE.md State Patterns; DESIGN.md agent-readiness-badge]
- **Semantic color roles (color + icon + visible text — color is never the sole signal):** `status-success` only when all gates pass; `status-informative` = checking/in-progress; `status-warning` = latency/cost attention; `status-severe` = **blocked readiness** / disabled; `status-danger` = failure/denial. Bind to the role, never a hex value. [Source: DESIGN.md Colors]
- **List/data surface states (8):** `loading, empty, filtered-empty, error, permission-denied, stale, degraded, unavailable` — every grid/panel distinguishes these; empty must not leak unauthorized records. [Source: EXPERIENCE.md State Patterns]
- **Operations envelope status:** `IsSuccess ⇒ Status == Succeeded` only; safe error codes only (`NotAuthorized, ValidationFailed, NotFound, Conflict, Stale, Unavailable, Rejected, Blocked, Unknown`); a blocked gate → `Blocked`. [Source: Contracts/Operations/AgentOperationResult.cs]

### Requirement anchors

- **FR-28 (the spine):** "V1 launch readiness requires explicit metric thresholds, latency targets, context-bounding behavior, and cost-control posture." Four testable consequences map 1:1 to AC1–AC4: (a) SM-2/SM-3 need numerator/denominator/target/measurement-window/launch-cohort; (b) latency targets per response mode; (c) cost posture enum; (d) production-like generation cannot be enabled until Content Safety Policy + Context Policy + metric thresholds + latency targets + cost posture are recorded. [Source: prd.md §4.10 FR-28]
- **NFR Performance / Cost Control:** there are **no numeric NFR9/NFR10 IDs** — these map to PRD §7 named NFRs "Performance" (latency targets per mode) and "Cost Control" (cost posture before enablement). [Source: prd.md §7]
- **Success metrics** [Source: prd.md §11]: **Primary** = SM-1, SM-2, SM-3; **Secondary** = SM-4, SM-5, SM-6; **Counter-metrics** = SM-C1, SM-C2, SM-C3. SM-2 = "share of eligible launch Conversations using ≥1 Agent Call after enablement" (define eligible-Conversation denominator, launch cohort, target %, measurement window). SM-3 = "share of Proposed Agent Replies reaching a terminal state in Confirmation Mode" (define numerator, denominator, target threshold, terminal-state inclusion rules, measurement window). The `LaunchMetricClassification` enum (Primary/Secondary/Counter) satisfies AC2's "distinguish primary/secondary/counter-metrics."
- **Scope note:** concrete SM-2/SM-3 threshold *values* and OQ-5/OQ-6/OQ-11 (latency SLO numbers, cost-control choice, launch thresholds) are accepted **downstream governance blockers** — this story records the *structure and mechanism*, not provider-specific numbers. [Source: prd.md §12 OQ-5/6/11; implementation-readiness-report lines 192–193]

### Project Structure Notes

- **Module root:** `Hexalith.Agents/` (the sibling `Hexalith.Conversations`/`Parties`/`Tenants`/etc. are source dependencies, not this module). Solution: `Hexalith.Agents.slnx`. Stack: `net10.0`, C# `14`, nullable enabled, implicit usings, `TreatWarningsAsErrors=true`, Central Package Management (`Directory.Packages.props` — **no new packages are needed**; add versions there only if one is). [Source: Directory.Build.props; ARCHITECTURE-SPINE.md#Stack]
- **Base types:** `EventStoreAggregate<TState>` + `[EventStoreDomain("agent")]`; `CommandEnvelope`, `DomainResult` (Success/Rejection/NoOp); `IEventPayload` / `IRejectionEvent` (global-used). Commands/events/queries auto-register via assembly scan of `AgentsAssemblyMarker` + `ServerAssemblyMarker` — **no manual serializer registration and no host wiring for a new `Handle`.** No PolymorphicSerialization in this module; enums use `[JsonStringEnumConverter]` + `Unknown = 0`. [Source: 4-x codebase map; ARCHITECTURE-SPINE.md#Stack]
- **Project map & InternalsVisibleTo:** `Hexalith.Agents` (domain) → `Hexalith.Agents.Tests` + `Hexalith.Agents.Server` (so the Server can reuse internal pure policies and stay in lock-step); `Hexalith.Agents.Server` → `Hexalith.Agents.Server.Tests`; `Hexalith.Agents.UI` → `Hexalith.Agents.UI.Tests`. UI references only `Contracts` + `Client` (AD-15). [Source: Hexalith.Agents.csproj]
- **Read-model binding stays deferred** (AD-3 purity + AD-12 fail-closed; deferred per ARCHITECTURE-SPINE.md#Deferred): `Server/Projections/` is `.gitkeep`-only; do not add live projections. Read paths are pure functions over rehydrated state + fail-closed deferred gateways/ports. Cross-process dispatch goes through the deferred `IAgentCommandDispatcher`/`DeferredAgentCommandDispatcher` (still throws — public API must map deferred/unavailable to structured results; leaking the deferred exception is a contract bug). [Source: 4-1/4-2/4-3 dev notes]
- **FrontComposer:** Fluent UI Blazor `5.0.0-rc.3`; `Fc*` shell components (`FcPageLayout`, `FcPageHeader`, `FcContentLabel`); `Fluent*` widgets (`FluentStack`, `FluentBadge`, `FluentButton`, `FluentRadioGroup`); status surfaces use semantic HTML + `<AgentSurfaceState>` for non-success states; every interactive element gets `data-testid="agents-..."`. Policy constants in `AgentsFrontComposerRegistration.cs`: `AgentsAdministratorPolicy`, `AgentsOperatorPolicy`, etc.; the existing nav has eight ordered entries (Order 0–7) — the launch-readiness page is Order 8. [Source: 4-3 codebase map; AgentsFrontComposerRegistration.cs]

### Testing standards

- **Frameworks:** xUnit v3 `3.2.2` + Shouldly `4.3.0` (no raw `Assert.*`) + NSubstitute `5.3.0`; UI adds bUnit `2.8.4-preview` + AngleSharp. `tests/Directory.Build.props` provides global `using Xunit;` and `NoWarn IDE1006;CA2007;xUnit1051`. [Source: 4-x test conventions]
- **Style:** `public sealed class …Tests`; `snake_case_descriptive` method names; build aggregate state by **applying production events** (never setting properties); `StubAgentsLocalizer` returns keys so assertions compare resource keys. UI harness: `AgentsTestContext` (extends `FrontComposerTestBase`, NSubstitute fail-closed gateways, `FixedTimeProvider`, `RenderInShellWithNavigation<T>()`); extend the reusable `AgentsNavigationTests`, `BadgeConformanceTests`, `AccessibilityTests`, `LocalizationResourceTests`, `DeferredGatewayTests`, `AgentUiTestData`.
- **What to prove:** aggregate purity (replay-through-`Apply`, no `UtcNow`, gate derives from state with any/absent verdicts); fail-closed authorization (auth runs before state access; `NotAuthorized`/`NotFound` indistinguishable; default deferred gateway → `PermissionDenied`/`Unavailable`); no-leak (sentinel secret/content/cross-tenant id absent from markup/`aria-label`/`data-testid`/copied text — `ContractsSecretNonDisclosureTests` auto-scans new public types); structural guards stay green.
- **Build/run protocol:** `dotnet build Hexalith.Agents.slnx -c Release -m:1` → 0 warnings/0 errors; run touched test projects **individually**; on VSTest `SocketException (13)` run built xUnit v3 executables directly; `DiffEngine_Disabled=true` if a Verify snapshot is added.

### References

- [Source: epics.md#Story-4.4-Define-And-Enforce-Launch-Readiness-Gates (lines 994–1020)] — acceptance criteria (verbatim above).
- [Source: prd.md §4.10 FR-26/FR-27/FR-28] — content safety + launch readiness feature; FR-28 four consequences.
- [Source: prd.md §7] — NFR "Performance" (latency targets per mode) + "Cost Control" (cost posture before enablement).
- [Source: prd.md §11 SM-1…SM-6, SM-C1…SM-C3] — metric definitions + primary/secondary/counter classification.
- [Source: prd.md §12 OQ-5/OQ-6/OQ-11] — deferred governance values (latency SLOs, cost-control choice, launch thresholds).
- [Source: ARCHITECTURE-SPINE.md#AD-2/#AD-3/#AD-4/#AD-12/#AD-13/#AD-14/#AD-15/#AD-17/#Deferred/#Stack/#Consistency-Conventions] — aggregate boundaries, purity, snapshot versioning, fail-closed, idempotency, secret safety, parity, additive contracts, deferred quota/latency enforcement, tech stack.
- [Source: DESIGN.md agent-readiness-badge / operational-status-panel / Colors] + [EXPERIENCE.md State Patterns / Accessibility Floor / Voice and Tone] — readiness display, canonical states, color+icon+text rule, no-leak.
- [Source: Hexalith.Agents/Agent/AgentAggregate.cs, AgentState.cs, AgentConfigurationPolicy.cs, AgentInspection.cs] — the content-safety record-and-gate precedent (Story 1.7) to mirror.
- [Source: Contracts/Agent/AgentContentSafetyConfiguration.cs, Commands/ConfigureAgentContentSafetyPolicy.cs, Events/AgentContentSafetyPolicyConfigured.cs, AgentActivationBlocker.cs, AgentResponseMode.cs] — value-object / command / event / enum templates.
- [Source: Contracts/Operations/AgentAuditGovernanceReadiness.cs; Server/Ports/IAgentAuditGovernanceReadinessProvider.cs, AgentInvocationReadiness.cs; Contracts/AgentInteraction/AgentInteractionGateCheck.cs] — named-blocker model + invocation-gate extension points.
- [Source: Contracts/Operations/AgentOperationResult.cs, AgentOperationalStatusSummaryView.cs; Client/IAgentsClient.cs, UnavailableOperations.cs; Server/Api/AgentsOperationEndpoints.cs] — operations envelope, status summary, client facade + fail-closed stubs, endpoint mapping.
- [Source: UI/Components/Pages/OperationalStatus.razor, Components/Shared/OperationalStatusPanel.razor + OperationalStatusPresentation.cs + RecoveryActionGroup.cs + AgentReadiness.cs + AgentSurfaceState.razor; Services/Gateways/*; Composition/AgentsFrontComposerRegistration.cs; Resources/AgentsResources.{resx,fr.resx}] — UI page/panel/gateway/nav/resource patterns to mirror.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (1M context) — claude-opus-4-8[1m]

### Debug Log References

- `dotnet build Hexalith.Agents.slnx -c Release -m:1` → **0 warnings / 0 errors** (warnings-as-errors; CA2007/nullable clean).
- Touched test projects run individually (Release, `--no-build`) — all green:
  - `Hexalith.Agents.Tests` — 716 passed / 0 failed
  - `Hexalith.Agents.Contracts.Tests` — 327 passed / 0 failed
  - `Hexalith.Agents.Server.Tests` — 355 passed / 0 failed
  - `Hexalith.Agents.UI.Tests` — 961 passed / 0 failed
  - `Hexalith.Agents.Client.Tests` — 6 passed / 0 failed
  - **Total: 2,365 passed / 0 failed.** No VSTest SocketException encountered (standard `dotnet test` runner worked).

### Completion Notes List

- **Record-and-gate, modeled on Story 1.7.** Launch readiness is recorded on the existing `Agent` aggregate (no new aggregate, AD-2) via `RecordAgentLaunchReadiness` → `AgentLaunchReadinessRecorded` + a `LaunchReadiness`/`LaunchReadinessVersion` state field. Production-like enablement is a separate, higher gate (`EnableProductionLikeGeneration` → `AgentProductionLikeGenerationEnabled` / `AgentProductionLikeGenerationBlockedRejection`) over a pure `AgentLaunchReadinessPolicy.ComputeLaunchReadinessBlockers` shared by the write path, the read inspection, and the status view (lock-step, mirroring `ComputeActivationBlockers`).
- **Baseline `ActivateAgent` untouched.** Launch-readiness blockers are a distinct enum (`AgentLaunchReadinessBlocker`) and never folded into `ComputeActivationBlockers`; the two gates remain separate (baseline activation = dev/staging; production-like enablement = this story).
- **Recording validates well-formedness; the gate validates presence.** Recording rejects incomplete metrics, malformed latency targets (Unknown mode / non-positive ms / duplicate modes), and an `Unknown` cost posture. Presence of ≥1 metric, both per-mode latency targets, the in-force context policy, content safety, and resolved audit governance are checked by the gate (so the blocker enum can surface each one). "Present ≡ valid" holds for a recorded readiness.
- **Audit-governance consumption (Story 4.2).** The aggregate reads the trusted, server-populated `audit:governanceResolved` envelope extension (same fail-closed `"true"`-only model as `actor:agentsAdmin`); the inspection takes it as a bool resolved in the Server layer from `IAgentAuditGovernanceReadinessProvider` (AD-3 — never read a port from the pure aggregate/policy). It surfaces as the `UnresolvedAuditGovernance` blocker. Since V1 audit governance is `MetadataOnlyBlocked` (unresolved), production-like enablement fails closed by default — the intended behavior.
- **Additive contracts (AD-17).** `AgentStatusView` gained `LaunchReadinessBlockers` + `ProductionLikeGenerationEnabled` (appended after `ActivationBlockers`); `AgentOperationalStatusSummaryView` gained `LaunchReadinessBlockers` (appended last, a distinct typed list — not merged into `ReadinessBlockers`/`AuditGovernanceBlockers`); `AgentInteractionGateCheck` gained `LaunchReadiness` (ordinal 10); `AgentInvocationReadiness` (Server port) gained `ProductionLikeGenerationEnabled`. All carry round-trip + ordinal + no-leak tests.
- **Invocation gate (Task 6) live-binding deferred.** The orchestrator adds a 10th `LaunchReadiness` verdict via `MapLaunchReadiness` (mirrors `MapContentSafety`); `DeferredAgentInvocationReadinessReader` still returns `NotAvailable`, so the default DI graph keeps failing closed → `Denied`. No projections or live readiness reader bound (AD-3 purity + AD-12 fail-closed; deferred per ARCHITECTURE-SPINE.md#Deferred). `AgentInvocationGatePolicy.Decide` needed no change.
- **API/UI parity (AD-15).** `IAgentAdministrationOperations.RecordLaunchReadinessAsync`/`EnableProductionLikeGenerationAsync` and `IAgentStatusOperations.GetAgentLaunchReadinessAsync` added with fail-closed `Unavailable*` stubs and POST/POST/GET routes. The `Blocked` operation status/error code already exists (Story 4.1) so a blocked enablement maps to `Blocked`; the live dispatch mapping remains deferred (`DeferredAgentCommandDispatcher`).
- **Admin UI.** `LaunchReadiness.razor` (`/agents/launch-readiness`, `AgentsAdministratorPolicy`, nav Order 8) + `LaunchReadinessPanel.razor` + pure `LaunchReadinessPresentation` (blocker → `RecoveryActionGroup`: `FixPolicy`/`InspectAudit`). Fail-closed `ILaunchReadinessGateway`/`DeferredLaunchReadinessGateway` + `LaunchReadinessResult` wrapper, registered via `TryAddScoped`. All non-success states route through `AgentSurfaceState`; badges use semantic color + icon + visible text (`status-success` only when enabled). New en/fr resource strings added (parity auto-enforced).
- **Deferred (unchanged scope):** runtime quota/latency enforcement, live read-model/projection binding, the live invocation-gate readiness reader, concrete SM-2/SM-3 threshold values + OQ-5/6/11 governance numbers, and the distinct release-operator authorization model (folds under `actor:agentsAdmin` for V1).

### File List

**Added — Contracts (`Hexalith.Agents/src/Hexalith.Agents.Contracts/`)**
- `Agent/LaunchMetricClassification.cs`
- `Agent/LaunchMetricDefinition.cs`
- `Agent/CostControlPosture.cs`
- `Agent/ResponseModeLatencyTarget.cs`
- `Agent/AgentLaunchReadiness.cs`
- `Agent/AgentLaunchReadinessBlocker.cs`
- `Agent/AgentLaunchReadinessView.cs`
- `Agent/AgentLaunchReadinessInspectionResult.cs`
- `Agent/Commands/RecordAgentLaunchReadiness.cs`
- `Agent/Commands/EnableProductionLikeGeneration.cs`
- `Agent/Events/AgentLaunchReadinessRecorded.cs`
- `Agent/Events/AgentProductionLikeGenerationEnabled.cs`
- `Agent/Events/Rejections/AgentLaunchReadinessRejection.cs`
- `Agent/Events/Rejections/AgentProductionLikeGenerationBlockedRejection.cs`

**Added — Domain (`Hexalith.Agents/src/Hexalith.Agents/`)**
- `Agent/AgentLaunchReadinessPolicy.cs`
- `Agent/AgentLaunchReadinessInspection.cs`

**Added — UI (`Hexalith.Agents/src/Hexalith.Agents.UI/`)**
- `Components/Pages/LaunchReadiness.razor`
- `Components/Shared/LaunchReadinessPanel.razor`
- `Components/Shared/LaunchReadinessPresentation.cs`
- `Services/Gateways/ILaunchReadinessGateway.cs`
- `Services/Gateways/DeferredLaunchReadinessGateway.cs`
- `Services/Gateways/LaunchReadinessResult.cs`

**Added — Tests**
- `tests/Hexalith.Agents.Tests/AgentLaunchReadinessTests.cs`
- `tests/Hexalith.Agents.UI.Tests/LaunchReadinessPresentationTests.cs`
- `tests/Hexalith.Agents.UI.Tests/LaunchReadinessSurfaceTests.cs`

**Modified — Domain**
- `src/Hexalith.Agents/Agent/AgentAggregate.cs` (Record/Enable handlers, validation/equality helpers, audit-governance reader, empty-readiness fallback)
- `src/Hexalith.Agents/Agent/AgentState.cs` (LaunchReadiness/LaunchReadinessVersion/ProductionLikeGenerationEnabled fields + Apply overloads)
- `src/Hexalith.Agents/Agent/AgentInspection.cs` (status view populates the two new fields)

**Modified — Contracts**
- `src/Hexalith.Agents.Contracts/Agent/AgentStatusView.cs` (additive: LaunchReadinessBlockers + ProductionLikeGenerationEnabled)
- `src/Hexalith.Agents.Contracts/Operations/AgentOperationalStatusSummaryView.cs` (additive: LaunchReadinessBlockers)
- `src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionGateCheck.cs` (additive: LaunchReadiness)

**Modified — Server**
- `src/Hexalith.Agents.Server/Ports/AgentInvocationReadiness.cs` (additive: ProductionLikeGenerationEnabled)
- `src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionGateOrchestrator.cs` (10th verdict + MapLaunchReadiness)
- `src/Hexalith.Agents.Server/Api/AgentsOperationEndpoints.cs` (record/enable POST + launch-readiness GET routes)

**Modified — Client**
- `src/Hexalith.Agents.Client/IAgentAdministrationOperations.cs`
- `src/Hexalith.Agents.Client/IAgentStatusOperations.cs`
- `src/Hexalith.Agents.Client/UnavailableOperations.cs`

**Modified — UI**
- `src/Hexalith.Agents.UI/Composition/AgentsFrontComposerRegistration.cs` (nav Order 8)
- `src/Hexalith.Agents.UI/Services/Gateways/AgentsUiServiceCollectionExtensions.cs` (TryAddScoped gateway)
- `src/Hexalith.Agents.UI/Resources/AgentsResources.resx`
- `src/Hexalith.Agents.UI/Resources/AgentsResources.fr.resx`

**Modified — Tests**
- `tests/Hexalith.Agents.Tests/AgentTestData.cs`
- `tests/Hexalith.Agents.Contracts.Tests/AgentContractsRoundTripTests.cs`
- `tests/Hexalith.Agents.Contracts.Tests/AgentOperationalStatusSummaryContractsTests.cs`
- `tests/Hexalith.Agents.Server.Tests/AgentInteractionGateOrchestratorTests.cs`
- `tests/Hexalith.Agents.Server.Tests/AgentsOperationEndpointsTests.cs`
- `tests/Hexalith.Agents.UI.Tests/AgentsTestContext.cs`
- `tests/Hexalith.Agents.UI.Tests/AgentUiTestData.cs`
- `tests/Hexalith.Agents.UI.Tests/AgentsNavigationTests.cs`
- `tests/Hexalith.Agents.UI.Tests/AgentsUiCompositionTests.cs`
- `tests/Hexalith.Agents.UI.Tests/DeferredGatewayTests.cs`
- `tests/Hexalith.Agents.UI.Tests/LocalizationResourceTests.cs`

**Story tracking**
- `_bmad-output/implementation-artifacts/4-4-define-and-enforce-launch-readiness-gates.md` (frontmatter baseline_commit, checkboxes, Dev Agent Record, Status)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (4.4 → in-progress → review)

### Change Log

- 2026-06-25 — Implemented Story 4.4 (launch-readiness record-and-gate): contracts + aggregate command/event/state, pure launch-readiness policy + production-like enablement gate, read-path view/inspection + additive status surfaces, public API/client + endpoints, additive fail-closed invocation-gate check, Admin UI launch-readiness surface, and full test coverage (domain/contracts/server/UI). Release build 0/0; 2,365 tests pass. Status → review.
- 2026-06-25 — Senior Developer Review (AI, auto-fix) by Administrator. Verified all 9 tasks and AC1–AC4 against the implementation; Release build 0/0; 2,365 tests green. Auto-fixed: (1) [Security/defense-in-depth] added the Story-4.4 reserved trust key `audit:governanceResolved` to `AgentInteractionGateOrchestrator._reservedExtensionKeys` (its three sibling server-populated keys were already stripped) + extended the stripping test; (2) [Doc] refreshed stale Debug Log test counts (708/324/941, total 2,334 → 716/327/961, total 2,365); (3) [Doc] added omitted `LaunchReadinessPresentationTests.cs` to the File List. No CRITICAL/HIGH findings. Status → done.
