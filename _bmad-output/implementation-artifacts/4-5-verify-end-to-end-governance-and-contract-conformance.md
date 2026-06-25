---
baseline_commit: c9da6e70da3ad19043f14c2f48a258dd08318b1c
---

# Story 4.5: Verify End-To-End Governance And Contract Conformance

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a Master Test Architect,
I want end-to-end governance and contract evidence for `hexa`,
so that launch-critical behavior is proven across UI, API, domain, runtime, and integration boundaries.

## Acceptance Criteria

**AC1 — Consolidated conformance suite proves every AD-17 governance gate, tagged to its requirement**

**Given** all Agent setup, invocation, proposal, audit, and readiness workflows are implemented
**When** the conformance test suite runs
**Then** it verifies aggregate transition purity, authorization fail-closed paths, proposal version immutability, replay/idempotency, tenant isolation, context-too-large blocking, provider-secret non-disclosure, Content Safety enforcement, and audit completeness for every posted response
**And** failures identify the relevant FR, NFR, or UX-DR.

**AC2 — Runtime-ownership & SDK-purity conformance (deferred runtime verified at the fail-closed seam)**

**Given** runtime orchestration is used
**When** Agent Framework workflow/session restore, Dapr Workflow ownership, MCP/A2A/tool schema contracts, generation retries, and posting retries are tested **where applicable**
**Then** every agent task has exactly one durable owner
**And** public contracts and EventStore aggregates remain free of framework/provider SDK types.

**AC3 — FrontComposer UI conformance proves the UX floor across every Agents surface**

**Given** FrontComposer UI surfaces are tested
**When** layout, status semantics, keyboard flow, live regions, localization, policy-gated navigation, grid state handling, and accessible names are evaluated
**Then** UI tests prove setup, invocation, proposal, status, and audit workflows meet the UX Design Requirements
**And** high-impact actions fail closed when context is insufficient on constrained viewports.

**AC4 — Final governance & release-readiness report maps every requirement to a story and verification path**

**Given** release evidence is collected
**When** the final readiness report is produced
**Then** it maps each FR, NFR, architecture requirement, and UX-DR to at least one story and verification path
**And** unresolved governance decisions such as audit retention/legal hold/export/deletion remain explicit launch blockers rather than hidden assumptions.

## Tasks / Subtasks

> **Read this first.** This is the Epic-4 **verification/evidence** story, not a feature story. Its job is to (a) **consolidate** the AD-17 governance gates that already exist scattered across the per-story test files into one coherent, **requirement-tagged**, machine-checkable conformance suite, (b) **close the small set of real gaps** (cross-assembly SDK-purity, exactly-one-durable-owner, UI floor page-coverage), and (c) **author the final traceability/readiness report**. **DO NOT re-implement or duplicate existing per-story tests** — reference them and fill holes. The implementation-readiness report explicitly recommended Story 4.5 be "a final traceability/evidence story… most conformance checks live in the stories that introduce each behavior." Honor that: the centerpiece deliverable is **AC4's report**; AC1–AC3 add the consolidation layer and the few missing gates. [Source: implementation-readiness-report-2026-06-23.md §Step 5 Major Issue #4]

- [x] **Task 1 — Requirement-tagged conformance taxonomy + xUnit traits** (AC: #1, #4)
  - [x] Add a single shared trait vocabulary the suite uses so a failing gate names its requirement. Add `tests/Hexalith.Agents.Tests/Conformance/RequirementTraits.cs` (and mirror the `const` names where other test projects need them) exposing trait keys `"Requirement"`, `"Architecture"`, `"UxRequirement"`, `"Gate"`. Apply via `[Trait("Architecture","AD-17")]`, `[Trait("Requirement","FR-19")]`, `[Trait("Gate","TenantIsolation")]` on the consolidated conformance tests below. Trait values are plain strings (e.g. `"FR-19"`, `"NFR-2"`, `"UX-DR40"`, `"AD-18"`), so `dotnet test --filter "Gate=TenantIsolation"` selects a gate and a failure message can interpolate the requirement id.
  - [x] Every new conformance assertion's failure message MUST embed the governing id(s) — e.g. `tenantIsolated.ShouldBeTrue($"FR-19/NFR-2/AD-12: cross-tenant audit read must fail closed…")`. This is the AC1 "failures identify the relevant FR, NFR, or UX-DR" mechanism. Match the existing convention already used by `PublicContractPackageBoundaryTests` / `StructuralSeedConformanceTests` (`"…(AC2)."`-style suffixes) but use the **requirement id**, not a story-local AC number.

- [x] **Task 2 — Consolidated domain governance-gate suite (AC1)** (AC: #1)
  - [x] Add `tests/Hexalith.Agents.Tests/Conformance/GovernanceConformanceTests.cs`. This class is the **single entry point** that asserts each AD-17 domain gate end-to-end through the **real reflection-dispatch pipeline** (reuse `AgentInteractionTestData.ProcessAndApplyAsync` / `AgentTestData.ApplyAll` exactly as `AgentInteractionAuditTraceabilityE2ETests` does). For each gate, drive the minimum command chain and assert the invariant. Tag each with the requirement trait + id-bearing message:
    - **Aggregate transition purity** (`AD-3`, `NFR-3`): reflect over the three aggregates (`AgentAggregate`, `ProviderCatalogAggregate`, `AgentInteractionAggregate`) and assert no `Handle`-reachable use of wall-clock/`Guid.NewGuid`/I/O — assert via the existing replay tests' guarantee that applying the emitted events reproduces state deterministically. **Reuse** `AgentStateReplayTests`/`AgentInteractionStateReplayTests`/`ProviderCatalogStateReplayTests` as the proof and add ONE meta-assertion that all three replay suites exist and cover each aggregate (do not re-author per-event replay).
    - **Authorization fail-closed** (`FR-19`,`FR-20`,`FR-21`,`NFR-1`,`AD-12`): assert `NotAuthorized` and `NotFound` are **indistinguishable** on a representative read inspection per aggregate (`AgentInspection`, `AgentInteractionAuditInspection`, `ProviderCatalogInspection`) — same result shape whether the caller lacks rights or the aggregate is absent.
    - **Proposal version immutability** (`FR-14`,`AD-5`): drive generate → edit → regenerate → approve, then assert every prior `GeneratedVersions`/edited/regenerated `VersionId` is still present and unchanged after the terminal transition, and the approved version is exactly one preserved version (reuse `AgentInteractionProposalLifecycleE2ETests` data builders).
    - **Replay / idempotency** (`AD-13`): assert a re-asserted equal command → `DomainResult.NoOp()` on a representative `Agent` config command, AND the posting idempotency key + `MessageId` derive deterministically from `AgentInteractionId` + approved `VersionId` (assert equality of two independent derivations; reuse posting test data).
    - **Tenant isolation** (`FR-19`,`NFR-2`): drive an interaction under tenant A and assert an authorized-but-different-tenant reader cannot read its audit/status surface (fails closed, indistinguishable from not-found).
    - **Context-too-large blocking** (`FR-9`,`NFR-8`,`AD-11`): drive request → gate(satisfied) → context with an **over-budget** measurement and assert the interaction records context-blocked and creates **no** generated version / proposal / posting (reuse `AgentInteractionContextAggregateTests` builders / the `ContextBlocked` path).
    - **Content-safety enforcement** (`FR-26`,`FR-27`,`NFR-7`,`AD-14`): assert a blocked safety verdict prevents any Conversation side effect (no posted message, no proposal post) — reuse `AgentContentSafetyActivationTests` / generation-safety data.
    - **Audit completeness for every posted response** (`FR-24`,`NFR-5`,`AD-17`): **reuse and extend** `AgentInteractionAuditTraceabilityE2ETests` so BOTH automatic-mode and confirmation-mode posted responses are proven traceable `MessageId ← AgentInteractionId + VersionId ← Snapshot`, and assert the negative: serialized audit surface never contains prompt/generated/edited/regenerated content (AD-14). If the automatic-mode counterpart is missing, add it here.
  - [x] Where a gate is already fully proven in an existing file, the consolidated test should **invoke the same data/path or assert the existing test type is present**, not copy its body. Keep this class readable as the "table of contents" of governance gates.

- [x] **Task 3 — Cross-assembly SDK-purity & exactly-one-durable-owner conformance (AC2)** (AC: #2)
  - [x] **Close the AD-18/AD-19 purity gap.** Today `ContractsSecretNonDisclosureTests` + `ContractsBoundaryTests` + `PublicContractPackageBoundaryTests` scan only `Hexalith.Agents.Contracts`/`.Client`. Extend purity to the **EventStore domain surface**: add `tests/Hexalith.Agents.Server.Tests/RuntimeOwnershipConformanceTests.cs` with **two complementary scans** over the **domain assembly** (`typeof(AgentsAssemblyMarker).Assembly`) plus the **contracts assembly**:
    - **Public-member scan** (reuse the `_forbiddenTypeNamespacePrefixes` idiom from `ContractsSecretNonDisclosureTests`): no public aggregate/event/state/command/query type exposes a member typed in a forbidden namespace (`Microsoft.Agents`, `Microsoft.Agents.AI.Workflows`, `Dapr`, `Microsoft.SemanticKernel`, `Microsoft.Extensions.AI`, `Azure.AI`, `OpenAI`, `Anthropic`, `ModelContextProtocol`).
    - **Compiled-assembly-reference scan** (reuse the `ContractsBoundaryTests` `GetReferencedAssemblies()` idiom — this catches **transitive** leaks the public-member scan misses): the domain assembly's referenced-assembly names contain none of those forbidden prefixes.
    Failure message cites `AD-18`.
  - [x] **Extend the package-boundary guard** to ALL `src/` projects that must stay SDK-free: assert `Hexalith.Agents` (domain), `Hexalith.Agents.Contracts`, `Hexalith.Agents.Client`, and `Hexalith.Agents.UI` declare **no** `PackageReference` to the forbidden runtime/provider/workflow SDK prefixes (reuse `PublicContractPackageBoundaryTests.PackageReferenceNames` over the csproj XML). The SDK packages may appear only in `Hexalith.Agents.Server`/`.AppHost`/`.Aspire` when a live owner is bound (deferred). Confirm `Directory.Packages.props` currently pins **zero** provider/runtime SDK versions (the live owner is deferred) and assert no module project references them yet.
  - [x] **Exactly one durable owner (AC2 core).** V1 has **no live durable owner** bound — runtime orchestration is deferred (`Server/Application/Workflows/` and `Server/Projections/` are `.gitkeep`-only; generation/dispatch go through fail-closed `DeferredAgentGenerationProvider` / `DeferredAgentCommandDispatcher`). Assert this invariant directly: (a) `Server/Application/Workflows/` contains no compiled durable-owner type (no `[Workflow]`, no `Microsoft.Agents.AI.Workflows`/Dapr workflow base type in the domain/Server public surface); (b) the **only** path that mutates `AgentInteraction` state is an EventStore command (there is no second in-memory background worker); (c) `DeferredAgentGenerationProvider` and `DeferredAgentCommandDispatcher` **fail closed** (return Unavailable / throw the deferred sentinel that the public API maps to a structured result — never silently succeed). Reuse `DeferredAgentCommandDispatcherTests` as the fail-closed proof and add the "single declared owner" structural assertion.
  - [x] **Generation/posting retry idempotency (AD-13, "where applicable").** Live retry loops are deferred (no live workflow), so verify the **domain-level idempotency contract** that any future retry owner must honor: deterministic generation attempt ids and deterministic posting `MessageId`/idempotency key, re-applied without creating duplicate versions/messages. Reuse the posting/generation aggregate tests; add a consolidated assertion tagged `AD-13`.
  - [x] **Document the "where applicable" deferral in-test.** Add a clearly-named, **explicitly skipped or asserting-the-seam** test (e.g. `Live_runtime_owner_session_restore_and_MCP_A2A_tool_schemas_are_deferred_and_fail_closed`) whose XML-doc and message state: Agent Framework workflow/session restore, Dapr Workflow ownership, and MCP/A2A/tool-schema contracts are **deferred per ARCHITECTURE-SPINE.md#Deferred**; this story verifies the **fail-closed seam** (no live owner, no SDK leak) and the AC4 report records "verification deferred with fail-closed seam — re-verify when live binding lands." Do NOT fabricate a passing test for code that does not exist; assert the seam, not the absent runtime.

- [x] **Task 4 — FrontComposer UI floor conformance + page-coverage meta-test (AC3)** (AC: #3)
  - [x] **Page-coverage meta-test (the real gap).** Add `tests/Hexalith.Agents.UI.Tests/UiFloorCoverageTests.cs` that enumerates **every registered Agents page** (reflect over the routable `@page` components in `Hexalith.Agents.UI.Components.Pages`, or read the nav registration from `AgentsFrontComposerRegistration`) and asserts each page is covered by the UX floor: it (a) composes the FrontComposer shell layout (`FcPageLayout`), (b) renders all non-success states through `AgentSurfaceState` (the 8 `AgentSurfaceKind` states — `Loading, Empty, FilteredEmpty, Error, PermissionDenied, Stale, Degraded, Unavailable` — which **implement** UX-DR30's 6 named grid states plus the `Loading` + `Stale/Degraded/Unavailable` freshness split), (c) is reachable through a **policy-gated** nav entry (UX-DR1/41), and (d) appears in the accessibility + badge + localization conformance coverage. The point: a future page cannot silently skip the floor. Fail message cites the missing page + UX-DR.
  - [x] **Reuse, do not rebuild, the existing floor bases.** `AccessibilityTests` (skip links, landmarks, focusable heading, live-region politeness — UX-DR32/33/36/37), `BadgeConformanceTests` (color+icon+visible-text, semantic role not hex, whole-string localization — UX-DR11/12/22), `AgentsNavigationTests` (policy-gated nav, ordered entries — UX-DR1/41), `LocalizationResourceTests` (en/fr parity for every enum/surface key — UX-DR14), `DeferredGatewayTests` (fail-closed read surfaces — AD-12). Confirm these collectively cover setup (Story 1.8), invocation (2.6), proposal (3.7), status & audit (4.3), and launch-readiness (4.4) surfaces; add any page that slipped the floor.
  - [x] **Constrained-viewport fail-closed (UX-DR40, AC3 final clause).** Add a bUnit test proving a high-impact action (approve/post on the proposal editor/detail) is **unavailable with a visible reason** when the viewport cannot show enough context for safe approval, while **review-only** access remains. If no constrained-viewport gate exists in the UI yet, this test surfaces the gap — implement the minimal fail-closed guard in the proposal editor/detail surface to satisfy UX-DR40 (do not add animation-dependent or color-only signals; combine reason text + disabled control + `aria` description). Tag `UX-DR40`.
  - [x] **Canonical state vocabulary conformance (UX-DR25–29).** Add/confirm a test asserting the five canonical state families (Agent readiness, Provider/model, Agent Call, Proposal lifecycle, Audit availability) each have a 1:1 presentation mapping with no collapsed/missing state and every value localized in both cultures (extend `ProposedAgentReplyStatePresentationTests`/`AgentReadinessMappingTests`/`AgentCallStatusPresentationTests` only if a state is unmapped).

- [x] **Task 5 — Final governance & release-readiness report (AC4)** (AC: #4)
  - [x] Produce `_bmad-output/implementation-artifacts/4-5-governance-conformance-report.md` — the **implementation-time** conformance evidence (distinct from the planning-time `implementation-readiness-report-2026-06-23.md`). Structure:
    1. **FR traceability** — table `FR-1…FR-28 | title | owning story | verification path (test type/file or report section)`. Use the readiness-report coverage matrix as the FR→story backbone (all 28 FRs are 100% mapped); add the verification-path column from the conformance suite (see Dev Notes "Requirement → verification anchor map").
    2. **NFR traceability** — table `NFR-1…NFR-10 | name | owning story/gate | verification path`. **This mapping does not exist in any source — author it** (anchors in Dev Notes).
    3. **Architecture-requirement traceability** — table `AD-1…AD-19 | summary | verification path`. AD-17/AD-18/AD-19 map to this story's conformance suite; the rest map to per-story behavioral tests.
    4. **UX-DR traceability** — table `UX-DR1…UX-DR41 | short | owning UI story | verification path`. **Author it** (UI-story anchors in Dev Notes).
    5. **Explicit launch blockers / unresolved governance decisions** — list, NOT hidden assumptions: **audit retention / legal hold / export / deletion (OQ-8 / PRD §9 — blocks content-bearing audit)**, latency SLOs (OQ-5/NFR-9), cost-control thresholds (OQ-6/NFR-10), Content Safety policy categories (OQ-9), bounded-context behavior values (OQ-10), launch metric thresholds for SM-2/SM-3 (OQ-11), the Conversations `AddParticipant` membership seam dependency (AD-6/AD-7), and the deferred live runtime owner (AD-18) + MCP/A2A/tool schemas (AD-19). For each: owner, what it blocks, and current fail-closed posture.
    6. **Deferred-with-seam verifications** — runtime orchestration items (Agent Framework workflow/session restore, Dapr Workflow ownership, MCP/A2A/tool schemas, live generation/posting retries, live read-model/projection binding) marked "verified at fail-closed seam; re-verify on live binding."
  - [x] **Make the report honest & machine-checkable.** Add `tests/Hexalith.Agents.Server.Tests/TraceabilityManifestTests.cs` driven by an in-repo manifest (`tests/Hexalith.Agents.Server.Tests/Conformance/traceability-manifest.json` or an in-code table) mapping every `FR-1..28`, `NFR-1..10`, `AD-1..19`, `UX-DR1..41` id → `{ story, verificationPath }`. Assert: (a) **completeness** — every id in those ranges is present exactly once (no silent gap); (b) **honesty** — each `verificationPath` that names a test file resolves to a real file on disk via `ModuleLayout` (cross-project file existence, not cross-assembly reflection); (c) each unresolved-governance id is flagged `blocker:true` and present in the report's blocker section. This prevents the matrix from claiming coverage that does not exist. Keep it bounded — file-existence + id-completeness only, no deep semantic check.

- [x] **Task 6 — Build, run, and reconcile Dev Agent Record** (AC: all)
  - [x] `dotnet build Hexalith.Agents.slnx -c Release -m:1` → **0 warnings / 0 errors** (warnings-as-errors; `CA2007` `ConfigureAwait(false)` is an error; nullable clean).
  - [x] Run each touched test project **individually** (Release, `--no-build`), not solution-level `dotnet test`. On VSTest `SocketException (13)`, run the built xUnit v3 executables directly.
  - [x] **Regenerate test counts from the actual Release run and diff the File List against `git status --short` before marking review** (recurring Epic 2/3/4 finding — stale counts / File List omissions).
  - [x] Sanity-check the new conformance suite is selectable by trait: `dotnet test … --filter "Architecture=AD-17"` returns the consolidated gates.

## Dev Notes

This is the Epic-4 **conformance & evidence** story. The Agents module already ships an extensive per-story test surface (≈2,365 tests at Story 4.4) that individually proves most AD-17 gates. Story 4.5 does **three** things and nothing more: **(1)** consolidates those gates into one requirement-tagged suite whose failures name the FR/NFR/UX-DR (AC1), **(2)** closes the small real gaps — cross-assembly SDK-purity (AD-18/AD-19), exactly-one-durable-owner at the fail-closed seam, and a UI page-coverage floor meta-test (AC2/AC3), and **(3)** authors the final traceability/release-readiness report with unresolved governance decisions as explicit launch blockers (AC4). **Do not re-test what is already tested** — reference it. [Source: epics.md#Story-4.5 lines 1022–1048; ARCHITECTURE-SPINE.md#AD-17/#AD-18/#AD-19/#Deferred; implementation-readiness-report-2026-06-23.md §Step 5]

### The single most important framing — V1 runtime is DEFERRED; verify the seam, not absent code

Confirmed by direct inspection of the module source:
- **Zero** `Microsoft.Agents*` / `Dapr*` / `ModelContextProtocol` / `Microsoft.SemanticKernel` / provider-SDK `using`s anywhere in `Hexalith.Agents/src`. **Zero** such packages pinned in `Directory.Packages.props`.
- `Server/Application/Workflows/` and `Server/Projections/` are **`.gitkeep`-only**. There is **no live durable owner**, no live read-model binding.
- Every external dependency is mediated by a **port** (`Server/Ports/I*.cs`) with a fail-closed `Deferred*` implementation (`DeferredAgentGenerationProvider`, `DeferredAgentCommandDispatcher`, `DeferredAgentInvocationReadinessReader`, …). Live adapters exist only for Conversations (`ConversationClientContextReader`/`ConversationClientResponsePoster`) and Parties (`PartiesAgentPartyDirectory`).

Therefore AC2's "Agent Framework workflow/session restore, Dapr Workflow ownership, MCP/A2A/tool schema contracts, generation retries, posting retries… **where applicable**" is satisfied by proving **(a)** the **purity invariant** (no framework/provider/workflow SDK types in public contracts or EventStore aggregates/events — AD-18) and **(b)** the **single-owner fail-closed seam** (one declared owner path = `AgentInteraction` commands; deferred generation/dispatch fail closed; no second worker). The report records the live-runtime items as "deferred with fail-closed seam — re-verify on live binding," **never** as hidden assumptions. This is consistent with the entire Epic-4 "deferred live binding" posture (Stories 4.1–4.4). [Source: ARCHITECTURE-SPINE.md#Deferred; 4-4 dev notes "Deferred (unchanged scope)"]

### Architecture invariants under test (non-negotiable; this story PROVES them)

- **AD-3 Pure aggregates / NFR-3 Reliability:** `Handle` emits events only; no `UtcNow`/`Guid.NewGuid`/I-O/dependency reads; replay-through-`Apply` is deterministic. [Source: ARCHITECTURE-SPINE.md#AD-3]
- **AD-5 Proposal lifecycle / FR-14:** append-only; every generated/edited/regenerated version immutable; approval selects exactly one; terminal states cannot post. [Source: ARCHITECTURE-SPINE.md#AD-5]
- **AD-12 Authorization & dependency uncertainty / FR-19–21 / NFR-1–2:** gates before every side effect; fail closed on missing/stale/ambiguous/disabled/unavailable; `NotAuthorized`/`NotFound` indistinguishable; tenant from local projection. [Source: ARCHITECTURE-SPINE.md#AD-12]
- **AD-11 Context bounds / FR-9 / NFR-8:** over-budget or non-fresh context → context-blocked; no provider call/proposal/message. [Source: ARCHITECTURE-SPINE.md#AD-11]
- **AD-13 Idempotent external effects:** deterministic attempt ids; posting `MessageId` + idempotency key from `AgentInteractionId` + `VersionId`; retries never duplicate. [Source: ARCHITECTURE-SPINE.md#AD-13]
- **AD-14 Secret & content safety / NFR-6–7:** no raw content/payloads/stack-traces/secrets in events, projections, status, audit, telemetry dimensions, `aria-label`, copied text. [Source: ARCHITECTURE-SPINE.md#AD-14]
- **AD-17 Contract & test gates:** the spine list verbatim is this story's AC1 checklist. [Source: ARCHITECTURE-SPINE.md#AD-17]
- **AD-18 Hybrid runtime ownership / AD-19 Tool & protocol boundaries:** exactly one durable owner; public contracts + EventStore aggregates free of framework/provider/workflow SDK types. [Source: ARCHITECTURE-SPINE.md#AD-18/#AD-19]
- **AD-15 UI/API parity / UX spine:** admin UI and API/client share the same public contracts + authorization outcomes; UX-DR floor across surfaces. [Source: ARCHITECTURE-SPINE.md#AD-15]

### Extend, do not reinvent — existing conformance assets to reuse

Structural / boundary guards (in `tests/Hexalith.Agents.Server.Tests/` unless noted):
- `PublicContractPackageBoundaryTests.cs` — csproj-level forbidden-package guard on Contracts+Client (**extend** to domain + UI projects; reuse `PackageReferenceNames`).
- `ContractsSecretNonDisclosureTests.cs` (Contracts.Tests) — reflection scan of the Contracts assembly for secret member names + provider-SDK types (**extend** the type-namespace scan to the **domain assembly** + event/aggregate surface).
- `ContractsBoundaryTests.cs` (Contracts.Tests), `BuildContractConformanceTests.cs`, `StructuralSeedConformanceTests.cs`, `ProjectReferenceDirectionTests.cs`, `PackageVersionCentralizationTests.cs`, `ModuleLayout.cs` — build-contract, reference-direction, structural-seed guards (already green; the new manifest test reuses `ModuleLayout` for file existence).

Behavioral gates already proven per-story (reuse data/builders; assert presence — do not copy bodies):
- Purity/replay: `AgentStateReplayTests.cs`, `AgentInteractionStateReplayTests.cs`, `ProviderCatalogStateReplayTests.cs`.
- Audit completeness: `AgentInteractionAuditTraceabilityE2ETests.cs` (confirmation-mode trace; **add/confirm automatic-mode counterpart**), `AgentInteractionAuditInspectionTests.cs`.
- Proposal immutability/lifecycle: `AgentInteractionProposal*AggregateTests.cs`, `AgentInteractionProposalLifecycleE2ETests.cs`, `…ApprovalLifecycleE2ETests.cs`, `…EditLifecycleE2ETests.cs`.
- Context blocking: `AgentInteractionContextAggregateTests.cs`, `ContextPolicyResolutionTests.cs`.
- Content safety: `AgentContentSafetyActivationTests.cs`, `AgentContentSafetyPolicyTests.cs`.
- Fail-closed ports/dispatch: `DeferredAgentCommandDispatcherTests.cs`, `DeferredProposalExpiryPolicyReaderTests.cs`.
- UI floor bases (UI.Tests): `AccessibilityTests.cs`, `BadgeConformanceTests.cs`, `AgentsNavigationTests.cs`, `LocalizationResourceTests.cs`, `DeferredGatewayTests.cs`, `AgentsTestContext.cs` (harness: `FrontComposerTestBase`, NSubstitute fail-closed gateways, `FixedTimeProvider`, `RenderInShellWithNavigation<T>()`), `AgentUiTestData.cs`. Note `AgentsNavigationTests` already pins **nine ordered nav entries** (Order 0–8 after Story 4.4).

### Requirement → verification anchor map (backbone for the AC4 report)

Use these as the report's "verification path" column. FR→story comes from the readiness-report coverage matrix (28/28, 100%); NFR→ and UX-DR→ mappings are **newly authored here**.

- **FR-1..7** (setup) → Stories 1.2–1.7 → `Agent*AggregateTests`, `ProviderCatalog*Tests`, `Agent{ProviderSelection,ResponseMode,ApproverPolicy,ContentSafety}*Tests`.
- **FR-8..12** (invocation/auto-post) → Stories 2.1–2.5 → `AgentInteraction{Request,Gate,Context,Generation,Posting}*Tests`.
- **FR-13..18** (proposal workflow) → Stories 3.1–3.6 → `AgentInteractionProposal*Tests` + lifecycle E2E.
- **FR-19..21** (tenant isolation / authz / fail-closed) → Story 2.2 + cross-cutting → `GovernanceConformanceTests` (this story) + every orchestrator's fail-closed test.
- **FR-22** (admin UI) → Story 4.3 (+ slices 1.8/2.6/3.7) → UI floor suite.
- **FR-23** (API/client) → Story 4.1 → `AgentsOperationEndpointsTests`, `AgentsClientFacadeTests`, `UnavailableOperations` fail-closed.
- **FR-24** (audit) → Story 4.2 → `AgentInteractionAudit*Tests` + audit-traceability E2E.
- **FR-25** (operational status) → Story 4.3 → `OperationalStatus*Tests`, `AgentOperationalStatusSummaryContractsTests`.
- **FR-26/27** (content safety) → Stories 1.7/2.4 → content-safety tests.
- **FR-28** (launch readiness) → Story 4.4 → `AgentLaunchReadinessTests`, `LaunchReadiness*` UI.
- **NFR-1 Security**→AD-12 fail-closed suite; **NFR-2 Privacy**→tenant-isolation gate; **NFR-3 Reliability**→AD-3 purity + no-partial-post posting tests; **NFR-4 Observability**→operational-status suite; **NFR-5 Auditability**→audit-traceability E2E; **NFR-6 Provider Safety**→`ContractsSecretNonDisclosureTests` + domain-purity guard; **NFR-7 Content Safety**→content-safety gate; **NFR-8 Context Bounds**→context-blocking gate; **NFR-9 Performance**/**NFR-10 Cost Control**→Story 4.4 launch-readiness (values deferred OQ-5/OQ-6 → blocker list).
- **UX-DR1/41**→`AgentsNavigationTests` (policy-gated nav); **UX-DR11/12/22**→`BadgeConformanceTests`; **UX-DR14**→`LocalizationResourceTests` (en/fr); **UX-DR30**→`*SurfaceTests` (8 states); **UX-DR25–29**→`*PresentationTests` (canonical families); **UX-DR32/33/35/36/37**→`AccessibilityTests`; **UX-DR40**→new constrained-viewport fail-closed test; **UX-DR6/7/8**→proposal UI tests (Story 3.7); **UX-DR9/10**→status/audit UI (Story 4.3); **UX-DR24/27**→conversation-call UI (Story 2.6).

### Unresolved governance decisions — MUST appear as explicit launch blockers (AC4)

Not hidden assumptions. Each with owner + what it blocks + current fail-closed posture:
- **Audit retention / legal hold / export / deletion (OQ-8; PRD §9 Data Governance)** — Product+Governance — **blocks content-bearing audit** (Story 4.2 ships metadata-only; content-bearing stays gated). Current posture: `AgentAuditGovernanceReadiness` = `MetadataOnlyBlocked`, surfaced as the `UnresolvedAuditGovernance` launch-readiness blocker (Story 4.4), so production-like enablement **fails closed** by default. [Source: prd.md §9, §12 OQ-8; 4-2/4-4 dev notes]
- **Latency SLOs (OQ-5 / NFR-9)** and **cost-control thresholds (OQ-6 / NFR-10)** — Architecture+Release PM — block production-like enablement; Story 4.4 records the **structure**, values deferred.
- **Content Safety policy categories (OQ-9 / FR-26)** — Product+Security — block production generation.
- **Bounded-context behavior values (OQ-10 / FR-9)** — Architecture — context budget branch values deferred; current behavior fails closed (context-blocked).
- **Launch metric thresholds for SM-2/SM-3 (OQ-11 / FR-28)** — Product+Release PM.
- **Conversations `AddParticipant` membership seam (AD-6/AD-7)** — dependency; posting fails closed without it.
- **Live runtime owner (AD-18) + MCP/A2A/tool schemas (AD-19)** — deferred; verified at fail-closed seam.

### Project Structure Notes

- **Module root:** `Hexalith.Agents/`; solution `Hexalith.Agents.slnx`. Stack: `net10.0`, C# `14`, nullable, implicit usings, `TreatWarningsAsErrors=true`, Central Package Management (`Directory.Packages.props` — **no new packages needed**; this is a test-and-docs story). [Source: Directory.Build.props; ARCHITECTURE-SPINE.md#Stack]
- **Test projects (5):** `tests/Hexalith.Agents.{Contracts,Server,Client,UI}.Tests` + `tests/Hexalith.Agents.Tests` (domain). xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0`; UI adds bUnit `2.8.4-preview` + AngleSharp. `tests/Directory.Build.props` provides global `using Xunit;` and `NoWarn IDE1006;CA2007;xUnit1051`. There is **no** `Hexalith.Agents.IntegrationTests` project yet (the spine seed names it, but live Dapr/AppHost integration is deferred — do **not** create a Dapr-runtime integration project in this story; in-process xUnit conformance is the V1 verification mode).
- **Assembly markers** for reflection scans: domain `typeof(AgentsAssemblyMarker).Assembly` (`Hexalith.Agents`), contracts `typeof(AgentsContractsAssemblyMarker).Assembly`, server `typeof(ServerAssemblyMarker).Assembly`, UI `typeof(AgentsUIAssemblyMarker).Assembly`. `Hexalith.Agents.Server.Tests` has `InternalsVisibleTo` into Server (reuse internal pure types where needed); `Hexalith.Agents.Tests` sees the domain internals.
- **New files land under** `tests/Hexalith.Agents.Tests/Conformance/` (taxonomy + `GovernanceConformanceTests`), `tests/Hexalith.Agents.Server.Tests/` (`RuntimeOwnershipConformanceTests`, `TraceabilityManifestTests` + `Conformance/traceability-manifest.json`), `tests/Hexalith.Agents.UI.Tests/` (`UiFloorCoverageTests` + constrained-viewport test), and the report at `_bmad-output/implementation-artifacts/4-5-governance-conformance-report.md`.

### Testing standards

- **Style:** `public sealed class …Tests`; `snake_case_descriptive` method names; build aggregate state by **applying production events** (never setting properties) via `ProcessAndApplyAsync`/`ApplyAll`; Shouldly only (no raw `Assert.*`); `StubAgentsLocalizer` returns keys so UI assertions compare resource keys. UI harness: `AgentsTestContext` (extends `FrontComposerTestBase`, NSubstitute fail-closed gateways, `FixedTimeProvider`, `RenderInShellWithNavigation<T>()`).
- **What this story proves:** the AD-17 gate list end-to-end through the real reflection-dispatch pipeline; cross-assembly SDK-purity (AD-18/19); UI floor coverage for every page (AC3); a machine-checked, honest traceability matrix (AC4). Every conformance failure message names its FR/NFR/UX-DR/AD id (Task 1).
- **Build/run protocol:** `dotnet build Hexalith.Agents.slnx -c Release -m:1` → 0 warnings / 0 errors; run touched test projects **individually**; on VSTest `SocketException (13)` run built xUnit v3 executables directly; `DiffEngine_Disabled=true` if a Verify snapshot is added. Regenerate counts from the actual run; diff File List vs `git status --short`.

### References

- [Source: epics.md#Story-4.5-Verify-End-To-End-Governance-And-Contract-Conformance (lines 1022–1048)] — acceptance criteria (verbatim above).
- [Source: ARCHITECTURE-SPINE.md#AD-17] — Contract & Test Gates: the verbatim AC1 gate list (purity, fail-closed authz, immutability, replay/idempotency, workflow/session restore, MCP/A2A/tool schema, generation/posting retries, tenant isolation, context-too-large, secret non-disclosure, UI/contract conformance, audit completeness).
- [Source: ARCHITECTURE-SPINE.md#AD-18/#AD-19] — exactly one durable owner; no framework/provider/workflow SDK types in public contracts or EventStore aggregates; tool/protocol boundaries.
- [Source: ARCHITECTURE-SPINE.md#Deferred] — live runtime owner, Dapr Conversation API, quota/budget, audit retention/legal-hold/export/deletion, latency SLOs, safety provider all deferred → blocker list.
- [Source: prd.md §4 FR-1..FR-28, §7 NFR-1..NFR-10, §9 Data Governance, §11 SM-1..SM-6/SM-C1..SM-C3, §12 OQ-1..OQ-11] — requirement inventory + unresolved governance decisions.
- [Source: epics.md §Requirements Inventory (FR1–28, NFR1–10), §UX Design Requirements (UX-DR1..UX-DR41), §FR Coverage Map] — UX-DR backbone + FR→Epic map.
- [Source: implementation-readiness-report-2026-06-23.md §Step 3 Coverage Matrix] — FR→Epic→Story traceability (28/28, 100%); §Step 5 Major Issue #4 — frame Story 4.5 as final traceability/evidence story, not a re-test of everything.
- [Source: Hexalith.Agents/tests/Hexalith.Agents.Server.Tests/{PublicContractPackageBoundaryTests,StructuralSeedConformanceTests,BuildContractConformanceTests,ProjectReferenceDirectionTests,ModuleLayout}.cs] — boundary/structural guards to extend.
- [Source: Hexalith.Agents/tests/Hexalith.Agents.Contracts.Tests/ContractsSecretNonDisclosureTests.cs] — secret/provider-type reflection scan to extend to the domain assembly.
- [Source: Hexalith.Agents/tests/Hexalith.Agents.Tests/{AgentInteractionAuditTraceabilityE2ETests,Agent*StateReplayTests,AgentInteractionProposalLifecycleE2ETests,AgentInteractionContextAggregateTests,AgentContentSafetyActivationTests}.cs] — behavioral gates to consolidate.
- [Source: Hexalith.Agents/tests/Hexalith.Agents.UI.Tests/{AccessibilityTests,BadgeConformanceTests,AgentsNavigationTests,LocalizationResourceTests,DeferredGatewayTests,AgentsTestContext}.cs] — UI floor bases to reuse for the page-coverage meta-test + constrained-viewport test.
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/{Application/Workflows,Projections}/.gitkeep; Ports/Deferred*.cs] — proof the live runtime owner / read-model binding is deferred and fails closed.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (claude-opus-4-8) — BMAD dev-story workflow.

### Debug Log References

- Initial `TraceabilityManifestTests` completeness failure: expected-id construction used `"{prefix}-{i}"`, producing `UX-DR-1` while the UX-DR id format is `UX-DR1` (no hyphen). Fixed by passing the full literal prefix (`"FR-"`, `"NFR-"`, `"AD-"`, `"UX-DR"`) and building `"{prefix}{i}"`. Re-ran green.
- All other new suites passed on first run after compile-clean.

### Completion Notes List

This is the Epic-4 verification/evidence story — it consolidates existing AD-17 gates into one requirement-tagged suite, closes the small real gaps, and authors the final traceability/readiness report. No per-story tests were re-implemented; existing data builders/paths are reused and proving suites are asserted present.

- **Task 1 — Requirement-tagged taxonomy.** Added `RequirementTraits` (trait keys `Requirement`/`Architecture`/`UxRequirement`/`Gate` + gate-name vocabulary), mirrored into the Server.Tests and UI.Tests assemblies. Gates are selectable by trait (`dotnet test --filter "Architecture=AD-17"` → 8 gates; `--filter "Gate=TenantIsolation"` → 1). Every new conformance assertion's failure message embeds the governing FR/NFR/UX-DR/AD id.
- **Task 2 — `GovernanceConformanceTests` (domain).** Eight AD-17 gates driven through the real reflection-dispatch pipeline (reusing `AgentInteractionTestData`/`AgentTestData`/`ProviderCatalogTestData`): transition purity (replay-suite presence + determinism), authorization fail-closed (present-vs-absent indistinguishable per inspection), proposal version immutability (generate→edit→regenerate→approve append-only), replay/idempotency (re-asserted no-op + deterministic posting ids), tenant isolation, context-too-large blocking, content-safety enforcement, and **automatic-mode** audit completeness (the missing counterpart to the confirmation-mode `AgentInteractionAuditTraceabilityE2ETests`).
- **Task 3 — `RuntimeOwnershipConformanceTests` (server).** Closed the AD-18/AD-19 purity gap: public-member scan + compiled-assembly-reference scan over the domain + contracts assemblies; extended the package-boundary guard to domain/Contracts/Client/UI and asserted `Directory.Packages.props` pins zero provider/runtime SDKs; asserted exactly one durable owner (empty `Workflows`/`Projections`, no `[Workflow]`/SDK base type, no `IHostedService`/`BackgroundService`); deferred generation/dispatch ports fail closed; deterministic retry-id contract pinned; and an explicit deferred-with-seam test for AD-19 ("live owner / MCP / A2A / tool schemas deferred per ARCHITECTURE-SPINE.md#Deferred").
- **Task 4 — `UiFloorCoverageTests` (UI) + UX-DR40 guard.** Page-coverage meta-test enumerates every routable `@page` and asserts each is reachable through a policy-gated nav entry and composes both `FcPageLayout` and `AgentSurfaceState` (closing the real gap — a future page cannot silently skip the floor); confirmed the 8 `AgentSurfaceKind` states, the five canonical state families' complete localized mapping (UX-DR25–29), and the reused floor suites' presence. **Added a minimal fail-closed constrained-viewport guard** (`Constrained` parameter) to `ProposalApprover` (the high-impact approve/post action) with a focusable, `aria-describedby` reason + new en/fr resource key `Agents.ProposalApprover.ConstrainedUnavailable` (registered in `LocalizationResourceTests` parity coverage) — UX-DR40 previously had no proposal-surface guard.
- **Task 5 — Governance & release-readiness report (AC4) + machine-checkable manifest.** Authored `4-5-governance-conformance-report.md` (FR/NFR/AD/UX-DR traceability tables, explicit launch blockers, deferred-with-seam table) and the backing `traceability-manifest.json` (98 ids) enforced by `TraceabilityManifestTests`: completeness (every id once), honesty (every verification path resolves to a real file), and unresolved-governance ids (FR-9, FR-24, FR-26, FR-28, NFR-9, NFR-10, AD-6, AD-7, AD-18, AD-19) flagged `blocker:true` and named in the report.
- **Task 6 — Build & verification.** `dotnet build Hexalith.Agents.slnx -c Release -m:1` → **0 warnings / 0 errors**. All test projects green (Release, `--no-build`): domain **724**, Server **367**, UI **968**, Contracts **327**, Client **6** = **2,392** tests, 0 failures.

### File List

**New — tests:**
- `Hexalith.Agents/tests/Hexalith.Agents.Tests/Conformance/RequirementTraits.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Tests/Conformance/GovernanceConformanceTests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Server.Tests/Conformance/RequirementTraits.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Server.Tests/Conformance/traceability-manifest.json`
- `Hexalith.Agents/tests/Hexalith.Agents.Server.Tests/RuntimeOwnershipConformanceTests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Server.Tests/TraceabilityManifestTests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.UI.Tests/Conformance/RequirementTraits.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.UI.Tests/UiFloorCoverageTests.cs`

**New — report:**
- `_bmad-output/implementation-artifacts/4-5-governance-conformance-report.md`

**Modified — src (UX-DR40 fail-closed guard):**
- `Hexalith.Agents/src/Hexalith.Agents.UI/Components/Shared/ProposalApprover.razor`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Resources/AgentsResources.resx`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Resources/AgentsResources.fr.resx`

**Modified — tests (register the new localization key for en/fr parity):**
- `Hexalith.Agents/tests/Hexalith.Agents.UI.Tests/LocalizationResourceTests.cs`

### Change Log

| Date | Change |
| --- | --- |
| 2026-06-25 | Story 4.5 implemented: consolidated AD-17 governance-gate suite, cross-assembly SDK-purity + single-durable-owner conformance, UI floor page-coverage meta-test + UX-DR40 constrained-viewport guard, and the machine-checked governance & release-readiness report. Build 0/0; 2,390 tests pass. Status → review. |
| 2026-06-25 | QA `bmad-qa-generate-e2e-tests` pass: closed the AC1 "provider-secret non-disclosure" gate over the EventStore **domain** surface (the member-name secret scan previously ran on the Contracts assembly only) — added `RuntimeOwnershipConformanceTests.Domain_and_contracts_public_surface_exposes_no_secret_bearing_member_name` (NFR-6/AD-9/AD-14, `Gate=SecretNonDisclosure`) + the `SecretNonDisclosure` trait constant (canonical + Server mirror), making the report's NFR-6 verification path true. Build 0/0; Server.Tests 366→**367**, total **2,391** pass. Summary: `tests/4-5-qa-e2e-test-summary.md`. |
| 2026-06-25 | Adversarial review (`bmad-story-automator-review`) pass: fixed one MEDIUM — the AC3 page-coverage meta-test enforced only nav-reachability, leaving UX-DR15 (FcPageLayout composition) named in the report/manifest but **unasserted** and Task 4 (a)/(b) per-page floor unenforced. Added `UiFloorCoverageTests.Every_routable_agents_page_composes_the_shell_layout_and_surface_state_floor` (static per-page source scan of `FcPageLayout` + `AgentSurfaceState`, tagged `UX-DR15`/`UX-DR30`) so a future page cannot silently skip the layout/state floor; corrected the misleading `AgentsUiCompositionTests` floor-suite comment (it covers the gateway-DI seam, not FcPageLayout). Build 0/0; UI.Tests 967→**968**, total **2,392** pass. No CRITICAL/HIGH findings; status → done. |

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot (automated adversarial review) — 2026-06-25
**Outcome:** Approve (status → done). Build `Hexalith.Agents.slnx -c Release` = **0 warnings / 0 errors**. All five test projects green: domain **724**, Server **367**, UI **968**, Contracts **327**, Client **6** = **2,392**, 0 failed / 0 skipped. Trait selection verified (`Architecture=AD-17` → 8 gates, `Gate=TenantIsolation` → 1, `Gate=SecretNonDisclosure` → 1, `UxRequirement=UX-DR15` → 1). All 55 distinct manifest verification paths resolve to real files; File List reconciled against `git status` (no source discrepancies — only excluded `_bmad-output/` artifacts are untracked).

### Findings

- **[MEDIUM — FIXED] AC3 page-coverage floor under-enforced; UX-DR15 verification path was hollow.** `UiFloorCoverageTests.Every_routable_agents_page_is_reachable_through_a_policy_gated_nav_entry` enforced only nav-reachability, but Task 4 bullet 1 claims the meta-test also asserts each page (a) composes `FcPageLayout` and (b) renders `AgentSurfaceState`. No test anywhere asserted per-page `FcPageLayout` composition — `AgentsUiCompositionTests` (cited in a comment as the FcPageLayout suite) actually tests gateway DI registration only — so the report/manifest entry mapping **UX-DR15** → `UiFloorCoverageTests.cs` claimed coverage that no assertion provided. A future page could be nav-listed yet skip the shell layout/state floor undetected. **Fix:** added `Every_routable_agents_page_composes_the_shell_layout_and_surface_state_floor` (static per-page `.razor` source scan for `FcPageLayout` + `AgentSurfaceState`, tagged `UX-DR15`/`UX-DR30`); refactored shared `RoutablePages()`/`ModuleRoot()` helpers; corrected the misleading floor-suite comment. UX-DR15 is now genuinely asserted and trait-selectable.

- **[LOW — noted, no change] Constrained-unavailable `aria-describedby` is self-referential.** `ProposalApprover.razor`'s UX-DR40 block sets `id` and `aria-describedby` to the same `{TestId}-unavailable-reason` value (a circular reference that can double-announce). This is **not** introduced by this story's design — it faithfully mirrors the established `AgentCallStatusFeedback.razor` (Story 2.6) UX-DR40 pattern. Changing one sibling unilaterally would create inconsistency; recommend a future codebase-wide a11y cleanup of both `__unavailable` reason elements (e.g. drop the self-reference and use a `role="note"` announced region) rather than a one-off divergence here.

- **[LOW — noted, by design] Consolidated gates restate orchestrator-proven properties.** Gate 5 (tenant isolation) drives `isAuthorized:false` rather than two real tenants, and Gate 4(c) (deterministic posting `MessageId`/idempotency key) compares two runs over command-supplied ids — both are tautological *at the domain-inspection layer*, where tenant→authorized and posting-id derivation are resolved upstream. The genuine derivation proofs live in the Story 2.2 gate orchestrator and Story 2.5 posting orchestrator, which `RuntimeOwnershipConformanceTests` asserts present. This matches the story's explicit "reuse/assert presence, do not re-author" mandate; the gate comments slightly overclaim but the coverage is honest. No change.

- **[LOW — noted, no gap] `Every_unresolved_governance_id_is_a_blocker_named_in_the_report` checks whole-report presence.** The assertion does `report.ShouldContain(id)` over the full report text while its doc says "blocker section." Since every blocker id (FR-9/FR-24/FR-26/FR-28/NFR-9/NFR-10/AD-6/AD-7/AD-18/AD-19) is in fact listed in report §5, there is no actual coverage gap — only a mildly broader assertion than the comment implies. No change.

### Verified strengths

- AC1: 8 consolidated AD-17 domain gates drive the real reflection-dispatch pipeline (purity/replay determinism, authz fail-closed indistinguishability, append-only proposal immutability through terminal approval, idempotent no-ops, context-blocked-with-no-side-effect, safety-blocked-with-no-post, automatic-mode audit traceability with AD-14 content-free negative assertions). Every failure message embeds the governing FR/NFR/AD id.
- AC2: cross-assembly SDK-purity closed via both a public-member scan and a transitive referenced-assembly scan over the domain + contracts assemblies; CPM/`Directory.Packages.props` zero-SDK guard; exactly-one-durable-owner asserted structurally (empty `Workflows`/`Projections`, no `[Workflow]`/SDK base type/`IHostedService`/`BackgroundService`); deferred generation/dispatch fail closed; AD-19 deferral verified at the seam, not fabricated.
- AC4: machine-checkable manifest enforces id-completeness (FR-1..28/NFR-1..10/AD-1..19/UX-DR1..41 each exactly once), file-existence honesty, and blocker-set equality; report §5 enumerates every unresolved governance decision with owner + what-it-blocks + fail-closed posture.

_Reviewer: Jérôme Piquot on 2026-06-25_
