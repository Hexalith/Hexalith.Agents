# Architecture Reviewer Gate — Rubric Walker

- **Reviewed:** `ARCHITECTURE-SPINE.md` (frontmatter `updated: 2026-08-02`, `status: final`)
- **Date:** 2026-09-08
- **Lens:** BMad good-spine checklist (independent re-walk; prior 2026-08-02 reviews skimmed only to avoid re-reporting resolved items)
- **Inputs:** spine, `.memlog.md`, `IMPLEMENTATION-CONVENTIONS.md`, PRD (FR-1..FR-28, NFR-1..NFR-14, OQ-1..OQ-13), `epics.md`, `external-dependency-register.md` (updated 2026-08-09), `launch-readiness-register.md`, `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`, `Hexalith.Agents.slnx` and `src/`/`test/` directory listing (for stale-note checks only; no code inspection)

## Gate Verdict

**PASS WITH FINDINGS.** No critical finding. Three high findings: the aggregate inventory in AD-2 is now incomplete relative to the aggregates the epics have already invented; the Conversations-side **Call hexa** UI seam (the sole V1 entry point, OQ-1) has no AD and no external-dependency record; and five ADs restate the launch-readiness register verbatim, sinking the spine to spec altitude and creating a two-source drift risk. Several status notes embedded in AD rules are now factually stale.

## Deterministic Gate

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py \
  --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: `{"ok": true, "total_findings": 0}` — no placeholders, duplicate AD IDs, malformed ADs, or unpinned Stack rows.

## Tiered Findings

### Critical

None.

### High

#### R-1 — AD-2 aggregate inventory is incomplete; the epics have already invented aggregates the spine does not own

- **Severity:** High
- **Where:** AD-2 (Rule names exactly three aggregates: `Agent`, `ProviderCatalog`, `AgentInteraction`); AD-17 introduces `LaunchReadinessGate`; AD-21 introduces "the EventStore budget-ledger aggregate"; the Identity convention row; the class diagram.
- **Problem:** The spine names five aggregates across four places but AD-2, the AD whose stated purpose is aggregate boundaries, lists three. `epics.md` line 124 already lists "the budget ledger, and `LaunchReadinessGate`" as truth owners, and Stories 8.1–8.4 name `LegalHoldAggregateTests`, `AuditExportAggregateTests`, `ProtectedDeletionAggregateTests`, `ContentSafetyPolicyAggregateTests`, `TenantBudgetPolicyAggregateTests` — five further aggregates with no spine owner, no identity key, and no tenant scope. For the budget ledger specifically, its aggregate key is unbound (per tenant? per tenant-month? per tenant-agent?), yet AD-21 makes it "the single authority for balances", AD-13 step 2 reserves against it, and the `TenantBudgetUpdate` family mutates it. AD-2's own *Prevents* ("one tenant-wide hot aggregate") is in tension with a tenant-scoped ledger that every attempt writes to, and the spine never resolves that trade-off.
- **Why it matters:** Story 6.4 (reserve) and Story 8.4 (`TenantBudgetPolicy`) can independently choose different ledger keys; two ledgers claiming the same monthly cap is exactly the concurrent overspend NFR-10 forbids. Stories 8.1–8.3 can hang legal-hold/export/deletion state on `AgentInteraction` or on separate aggregates, which changes stream shape, deletion scope, and the `LR-AUDIT-PROTECTION-DELETION` purge inventory. Content Safety Policy (FR-26, `PolicyPublication` family, snapshotted as `ContentSafetyPolicyVersion` in AD-4) has no owning aggregate at all — the snapshot references a version of something nobody owns.
- **Disposition:** **Autofix (AD-2 amendment).** Extend AD-2's Rule to the full V1 aggregate inventory with the identity key and tenant scope of each: `Agent` (tenant, `AgentId`), `ProviderCatalog` (platform-scoped, `(ProviderId, ModelId)` — see R-9), `AgentInteraction` (tenant, `AgentInteractionId`), `BudgetLedger` (tenant, one stream per `(TenantId, BudgetPeriod)` or per `TenantId` — decide, and state that per-call caps are checked against the same stream), `ContentSafetyPolicy`/`TenantBudgetPolicy` (tenant, or fold into `Agent`/`BudgetLedger` — decide), `LaunchReadinessGate` (`(GateId, TenantScope, EnvironmentProfile)`), and governance aggregates for `LegalHold`, `Export`, `Deletion` (tenant; state whether they are separate aggregates or `AgentInteraction` sub-lifecycles). Add a sentence to *Prevents* acknowledging the accepted tenant-hot ledger stream and why it is bounded (one reservation per attempt, not per event).

#### R-2 — The sole V1 invocation entry ("Call hexa" on the Conversations surface) has no AD and no external-dependency record

- **Severity:** High
- **Where:** Capability map row "Explicit conversation invocation — Agents API/client and Conversation-owned **Call hexa** action — AD-3, AD-6, AD-11, AD-12, AD-18"; AD-6; AD-15; External V1 Prerequisites table; PRD OQ-1, FR-8; UX-DR24; Story 6.7 ("Conversation-owned **Call hexa** action … no new external seam").
- **Problem:** Every AD cited for this row governs Agents calling *into* Conversations (reads, membership, posting). None binds the reverse seam: how a Conversations-owned FrontComposer surface renders an action that submits an Agents command. AD-15 binds Agents UI to FrontComposer and to Agents client boundaries but says nothing about Agents UI contributing into another module's surface. The dependency direction is unbound: if Conversations references `Hexalith.Agents.Client`/`UI` to render the action, and Agents references `Hexalith.Conversations.Client` (AD-6), the two domain modules form a cycle; if instead Agents injects a component through a FrontComposer extension point, that extension point is a contract owned by FrontComposer or Conversations and is a critical external dependency under PRD §8 — yet there is no `EXT-*` record for it and Story 6.7 asserts "no new external seam".
- **Why it matters:** The Conversations maintainer and the Agents Story 6.7 implementer are two units that can each satisfy every AD literally and produce incompatible mechanisms (module dependency cycle vs. shell extension slot vs. deep link), and the choice changes package references, authorization flow (who evaluates call permission before the prompt surface opens), and whether the entry is testable in `LR-UI-CONFORMANCE` "every interactive V1 route".
- **Disposition:** **Discuss, then autofix.** Add one AD (or extend AD-15): "Conversations never references Agents packages. The **Call hexa** action is an Agents-owned UI contribution rendered into the Conversation surface through a FrontComposer/Conversations-owned extension seam; that seam is a critical external dependency (`EXT-CONV-UI-1` or fold into `EXT-CONV-AI-1`) and the action submits only the public Agents call command with the caller's principal, `ConversationId`, and idempotency metadata." Update the capability map row and the prerequisites table; correct Story 6.7's "no new external seam" through the sprint-change path, not the spine.

#### R-3 — Five ADs restate the launch-readiness register verbatim; the spine has sunk to spec altitude and now has two normative sources for the same text

- **Severity:** High
- **Where:** AD-10 (mirrors register "Provider Readiness Contract"), AD-17 (mirrors "Normative Record Schema", "Record Authority", "Gate Sets", "Explicit Projection Inventory"), AD-23 (mirrors "NFR-11 Recovery Evidence Contract"), AD-24 (mirrors "NFR-12 Capacity And Fairness Profile"), AD-26 (mirrors "NFR-14 Browser Monotonic Timing Contract"). Also AD-13 steps 3–5 restate AD-24's fence protocol, and AD-13 step 2 restates AD-21's reservation.
- **Problem:** AD-17 itself declares "`launch-readiness-register.md` is the normative readiness authority" and then re-enumerates its twelve record fields, four states, eighteen GateIds, seventeen projection IDs, gate-set semantics, and evidence levels. AD-10 re-enumerates fourteen enum members and three valid triples. AD-24 re-enumerates seven profile fields, the fence handshake, and the weighted-round-robin formula. AD-26 re-enumerates three sample kinds with their required ticks and three thresholds. These are field lists, enum vocabularies, thresholds, and step-by-step protocols — spec material, not invariants. The invariant content of each AD is one to three sentences (single writer, ordering, fail-closed rule, fence-before-transport). Drift has already begun: AD-24 says "Dapr Workflow checkpoints and queries the identity", which the register does not; AD-13 says the allocator "owns only admission leases and queue order", the register says "sole owner of tenant/system leases and queue order across replicas" — compatible today, but each edit now has to be made twice.
- **Why it matters:** A reader implementing from the spine copy after the register changes (the register is `status: active` and was edited on 2026-08-09; the spine was not) builds to a stale vocabulary. Two units — one reading the spine, one reading the register — are the divergence pair. It also breaks the spine's own test: "If two units built this independently, could they choose incompatibly?" applies to *which document is authoritative*, and the spine currently answers "both".
- **Disposition:** **Discuss (scope), then autofix.** For each of AD-10/17/23/24/26 keep the invariant sentences (owner, ordering, fail-closed precedence, "unknown fails closed", "one matrix version per decision", "browser ticks never mixed with wall clock", "fence validated immediately before transport") and replace every enumeration with a pointer: "field set, enum vocabulary, GateId/projection inventories, thresholds, and sample kinds are bound by `launch-readiness-register.md` §X, version N; the spine does not restate them." Collapse AD-13 steps 3–5 to "acquire admission and validate the fence per AD-24 immediately before transport". Expected result: AD-10/17/24/26 each shrink to roughly a quarter of their current length with no loss of binding force.

### Medium

#### R-4 — Status notes embedded in AD rules and the seed are now factually stale and assert non-conformance that no longer exists

- **Severity:** Medium
- **Where:** AD-16 italic "Implementation gap (2026-08-02): the current solution still contains `Hexalith.Agents.AppHost`, `Hexalith.Agents.Aspire`, and `Hexalith.Agents.ServiceDefaults`"; Structural Seed "*Implementation gap:* the checked-in solution still includes …"; AD-10 italic "Implementation gaps (2026-08-01) …"; AD-4 italic "Epic 2 reconciliation (2026-06-24) …"; External V1 Prerequisites "All seven records are currently `Uncommitted`"; AD title tags `[ADOPTED]`, `[CORRECTED 2026-08-01]`, `[AMENDED 2026-08-02]`; frontmatter `updated: 2026-08-02`.
- **Problem:** `Hexalith.Agents.slnx` today contains six `src/` projects (`Client`, `Contracts`, `Server`, `Testing`, `UI`, `Hexalith.Agents`) and no AppHost/Aspire/ServiceDefaults (removed in commit `516e7b9`, Story 5.1). `EXT-HOST-1` has been `Committed` since 2026-08-09 (owner Platform Maintainer, repo `Hexalith.Platform`, immutable commit, date 2026-09-30, verify script). The spine still says the opposite in two places. The AD-10 and AD-4 notes are change-log entries. None of these are invariants — they are memlog or readiness-report material — and because they carry dates inside a document whose frontmatter says `final`, a reader cannot tell which is current.
- **Why it matters:** A story writer citing AD-16 today would create work to remove projects that are already gone, or block on "`EXT-HOST-1` Uncommitted" that is not. Status inside a rule makes the rule's enforceability time-dependent.
- **Disposition:** **Autofix.** Delete the four italic notes and the "All seven records are currently `Uncommitted`" sentence (replace with "commitment status is read from the register at evaluation time; the spine does not track it"). Move the history to `.memlog.md` (it is already there). Strip the bracketed status tags from AD titles or move them to a one-line "Amended:" footer per AD. Bump `updated`.

#### R-5 — Structural Seed contradicts the conventions companion and the checked-in tree on where the domain lives

- **Severity:** Medium
- **Where:** Structural Seed (`Hexalith.Agents.Server/Aggregates/`, `Hexalith.Agents/` with no role, `test/Hexalith.Agents.IntegrationTests/`); `IMPLEMENTATION-CONVENTIONS.md` §Visibility Boundary ("The domain assembly … canonical boundary is `Hexalith.Agents.csproj`", policies under `src/Hexalith.Agents/AgentInteraction/`); memlog "Hexalith.Agents for host composition".
- **Problem:** The companion (which the spine declares binding under AD-3) places aggregates and twin-policies in `Hexalith.Agents` (the domain assembly, with `InternalsVisibleTo` rules keyed on it). The seed places `Aggregates/` under `Server` and gives `Hexalith.Agents/` no role. The memlog's original decision calls `Hexalith.Agents` host composition. The tree today has `src/Hexalith.Agents/{Agent,AgentInteraction,ProviderCatalog}` and `src/Hexalith.Agents.Server/Aggregates/README.md` only. The seed also names `test/Hexalith.Agents.IntegrationTests/`, which does not exist, and omits `test/Hexalith.Agents.Tests/`, which does.
- **Why it matters:** The `InternalsVisibleTo` friend boundary in the conventions is a real invariant (Server may call `Decide`, Server.Tests may not). It only holds if everybody agrees which assembly is "the domain". A unit following the seed would put a new aggregate in `Server/Aggregates` and lose the friend boundary without violating any sentence of the spine.
- **Disposition:** **Autofix.** Rewrite the seed to `Hexalith.Agents/ (domain: aggregates, states, internal twin-policies)`, `Hexalith.Agents.Server/ (Api, Application/{Agents,AgentInteractions,Queries,Workflows,Activities}, Composition, Ports, Projections)`, drop `Server/Aggregates`, replace `IntegrationTests` with `Hexalith.Agents.Tests` (or state that integration tests live in `Server.Tests`). Add one sentence to AD-3 or the conventions row: "Aggregates and policies live only in the `Hexalith.Agents` domain assembly."

#### R-6 — The spine neither ratifies nor declares its deviation from the platform reference layout in `hexalith-llm-instructions.md`

- **Severity:** Medium
- **Where:** Structural Seed; frontmatter `sources` (does not list `references/Hexalith.AI.Tools/hexalith-llm-instructions.md`); platform doc §"Domain-Driven Design Architecture" (per-layer NuGet packages `Hexalith.{Module}`, `.Abstractions`, `.Events`, `.Commands`, `.Requests`, `.Application`, `.Projections`, `.Servers`, `.ApiServer`, `.WebServer`, `.WebApp`, `.UI.Components`, `.UI.Pages`, `.Localizations` under `src/libraries/{Domain,Application,Infrastructure,Presentation}`; tests organised `test/Hexalith.{Module}.Tests/{Aggregate}/{Command}Tests.cs`).
- **Problem:** The spine's seed (`Contracts`, `Client`, `Server`, `UI`, `Testing`, `Hexalith.Agents`) is the sibling-module shape (memlog: "follows sibling Hexalith module shape"), not the platform reference layout. That is probably the right call — it ratifies brownfield siblings — but the spine does not say so, does not cite the platform instruction file it deviates from, and does not carry over the parts of the platform doc that *do* apply and that two epics could diverge on: the domain-module boundary rule ("domain module contains only domain code; shared boilerplate goes to the technical module"), which governs what `Hexalith.Agents.Testing` may contain, and the test file organisation by aggregate.
- **Why it matters:** Item 5 of the checklist is "ratifies rather than contradicts platform conventions". A silent contradiction lets one epic author reach for the reference tree (e.g., add `Hexalith.Agents.Events` or `Hexalith.Agents.Projections` packages) while another extends `Contracts`/`Server`. `Hexalith.Agents.Testing` is a candidate for the "test harness helpers belong in the technical module" rule; nothing in the spine says whether it holds domain-specific fixtures only.
- **Disposition:** **Discuss, then autofix.** Add the platform instruction file to `sources`. Add one consistency-convention row: "Package layout follows the sibling Hexalith module shape (Contracts/Client/Server/UI/Testing + domain assembly) as an accepted deviation from the reference per-layer layout; the domain-module boundary rule and the aggregate-organised test layout from `hexalith-llm-instructions.md` apply unchanged. `Hexalith.Agents.Testing` holds Agents-specific fixtures only; reusable harness code goes to the technical module."

#### R-7 — NFR-9 runtime latency and SM-1..SM-6 product metrics have no AD binding their authoritative event/timestamp source

- **Severity:** Medium
- **Where:** NFR-9, OQ-5, OQ-11, FR-28 latency consequences; AD-17 names `LR-RUNTIME-PERFORMANCE`, `LR-PRODUCT-METRICS`, `runtime-metrics`, `product-metrics` but binds no rule; capability map has no runtime-performance row; register says "NFR-9 measurement contract required" (TBD). Compare AD-23 (NFR-11) and AD-26 (NFR-14), which each bind a clock origin and start/end seams.
- **Problem:** "Accepted-call-to-post p95 ≤ 60 s" needs a defined start event (API receipt? `InteractionRequested` append? workflow start?) and end event (`AppendMessageAsync` accepted? `PostingSucceeded` appended? projection visible?), a clock (EventStore server timestamp vs. workflow history vs. host telemetry), and a late/missing-data rule. Story 6.1/6.4 emit the events; Story 8.5 calculates. Nothing in the spine prevents 6.x from stamping wall-clock in activities while 8.5 reads EventStore append timestamps, or 8.5 from computing from Dapr Workflow history, which AD-18 says is "execution state only".
- **Why it matters:** This is the same divergence class the prior review found for NFR-14 and the spine fixed with AD-26. Two units, two clocks, one false `Pass`.
- **Disposition:** **Autofix (small AD or AD-17 extension).** "Runtime latency and product-metric durations derive only from EventStore-committed timestamps of named Agents events (`InteractionRequested` → `PostingSucceeded`/`ProposalCreated`; `ProposalApproved` → `PostingSucceeded`; `InteractionRequested` → first blocked/rejected event for the fast-rejection gate). Workflow history, activity-local clocks, and browser clocks are never sources. `runtime-metrics` and `product-metrics` are the sole calculators; the versioned measurement contract lives in the register." Add a capability-map row.

#### R-8 — The runtime sequence diagram releases the capacity lease before the outcome is recorded and omits membership on the approval path

- **Severity:** Medium
- **Where:** Sequence diagram lines "Provider-->>Workflow: generated content or safe failure" → "Workflow->>Capacity: release AttemptId lease" → "Workflow->>Safety: validate generated output" → "Workflow->>Interaction: RecordGeneratedVersion or GenerationFailed"; and confirmation branch "Workflow->>Conv: Append approved version" with no membership step, while the automatic branch shows "ensure AIAgent member + AppendMessage".
- **Problem:** AD-24: "An `InvocationActive` lease is reclaimed only after a recorded terminal attempt". AD-13 step 7: "append the outcome, reconcile/release the reservation, and release admission" — outcome first. The diagram releases the lease two steps before the outcome is appended. AD-7 requires membership "before posting" with no mode qualifier; the diagram shows it only for automatic posting.
- **Why it matters:** The spine says shape lives in diagrams. A workflow author following the picture releases capacity while the outcome is still unrecorded; a crash in that window leaves an attempt with no terminal fact and no lease — exactly the state AD-13's recovery clause cannot reconcile. The approval-path omission invites Story 7.4 to skip the membership command that Story 6.6 performs.
- **Disposition:** **Autofix.** Move "release AttemptId lease" after "RecordGeneratedVersion or GenerationFailed"; add "ensure AIAgent member" before "Append approved version" (or factor both posts into one `Conv: ensure member + Append`).

#### R-9 — `ProviderCatalog` is platform-scoped but its tenant key, mutation authority, and read scope are unbound

- **Severity:** Medium
- **Where:** AD-10 ("scoped to the global catalog key (`ProviderId`, `ModelId`)"); PRD glossary "Global Providers Aggregate"; FR-19 ("cannot leak across tenant boundaries unless explicitly platform-scoped and authorized"); AD-12 (tenant access from the local Tenants projection); platform doc ("Multi-tenancy — built in at the contract level (Domain + AggregateId + TenantId)"); register (`TenantScope` on every readiness record; `provider-capability-pricing` projection scope unstated).
- **Problem:** A global catalog on a platform whose every command envelope carries `TenantId` needs a decision: which `TenantId` the catalog stream uses (a reserved platform tenant? empty?), which principal/role may mutate it (`ProviderCatalogMutation` family requires `LR-TENANT-ACCESS`, but tenant access to *which* tenant?), and whether tenants see the whole catalog or a tenant-enabled subset. AD-2 says `ProviderCatalog` owns "enablement" — global enablement, tenant enablement, or both is unstated.
- **Why it matters:** Story 5.3 (catalog mutation) and Story 5.5 (readiness/pricing projection consumed per `TenantScope`) can choose incompatibly: one keys the stream by platform tenant and authorises platform operators; the other keys per tenant and lets tenant admins enable models — and then `CapabilityVersion` monotonicity "scoped to the global key" is silently violated by per-tenant streams (the prior adversarial H-3 in a new coat).
- **Disposition:** **Autofix (AD-2/AD-10 sentence).** "`ProviderCatalog` is one platform-scoped aggregate per `(ProviderId, ModelId)` stored under the reserved platform tenant id `<X>`; only the platform-operator role resolved from the Tenants projection may mutate it; tenant-level control is limited to `Agent` provider/model selection; every tenant reads the same catalog and readiness result." (Pick the actual reserved id with the platform maintainer.)

#### R-10 — AD-8 carries a conditional escape hatch that is not enforceable

- **Severity:** Medium
- **Where:** AD-8 Rule: "V1 treats product 'conversation owner' authority as Conversation Facilitator **unless Conversations adds an explicit owner resolver before implementation**"; Deferred table row 1.
- **Problem:** Whether "Conversations added an owner resolver before implementation" is a fact two units can evaluate differently at different times (Story 7.1 implements approver resolution in September; Conversations ships an owner field in October; Story 7.4 implements posting authorisation and reads the new field). The rule then binds two different semantics for "owner" inside one interaction lifetime, which is precisely what AD-8's *Prevents* forbids.
- **Why it matters:** Approver identity is authorisation; a silent switch is an authorisation change without an AD amendment or a `ApproverPolicyVersion` bump.
- **Disposition:** **Autofix.** Replace the clause with: "V1 resolves 'conversation owner' to `ParticipantRole.Facilitator`. Adopting a Conversations owner field requires an AD-8 amendment and a new `ApproverPolicyVersion`; in-flight interactions keep their snapshot." Keep the Deferred row.

### Low

#### R-11 — Frontmatter `binds` lists non-capabilities; AD titles carry change-log tags

- **Severity:** Low. **Where:** frontmatter `binds: - PRD FR-1..FR-28 / - Hexalith Agents V1 / - hexa`; titles of AD-1, AD-16, AD-17, AD-18, AD-19.
- **Problem:** "Hexalith Agents V1" is the product and "hexa" is a configured instance; neither is a capability the spine binds. `[ADOPTED]`, `[CORRECTED 2026-08-01]`, `[AMENDED 2026-08-02]`, `[DEFERRED OUT OF V1]` are history, and AD-19's tag is misleading — its Rule ("V1 exposes no tools, MCP servers/clients, A2A agents …") is a live V1 negative invariant, not a deferral.
- **Disposition:** **Autofix.** `binds: [PRD FR-1..FR-28, NFR-1..NFR-14, OQ-1..OQ-13]`; strip title tags; retitle AD-19 "No Tool Or Remote-Agent Surface In V1".

#### R-12 — Design Paradigm paragraph duplicates AD-18/AD-19 scope and drifts toward description

- **Severity:** Low. **Where:** Design Paradigm second paragraph.
- **Problem:** The paragraph restates the module contents, the readiness registry, browser telemetry, Agent Framework conditions, and the out-of-V1 list — all of which are ADs. The paradigm should be one sentence plus the diagram.
- **Disposition:** **Autofix.** Keep sentence one; delete the rest (the flowchart already carries the shape).

#### R-13 — AD-12 "Normative families are …" can be read as the complete family list

- **Severity:** Low. **Where:** AD-12 (six families); AD-17/register `OperationGateMatrixVersion = 1` (thirteen families).
- **Problem:** Two family vocabularies; AD-12's are the lock-bearing subset of the matrix families, but the word "normative" invites treating six as the universe.
- **Disposition:** **Autofix.** "The high-risk (lock-bearing) subset of the register's operation families is …; the family vocabulary itself is owned by the `OperationGateMatrix` version."

#### R-14 — Public-contract versioning mechanism is bound only as "versioned and additive-first"

- **Severity:** Low. **Where:** AD-17 last sentences; FR-23; `EXT-CONV-AI-1` cites `/api/v1/...` for Conversations.
- **Problem:** FR-23 fixes the rules (`Unknown = 0`, no removal/rename, major bump on break); the spine does not fix the mechanism two units could choose differently — API path prefix (`/api/v1/agents`), package major, contract-assembly version. Adequate for V1 because FR-23 is explicit and only additive changes are allowed, but the first breaking change will hit it.
- **Disposition:** **Defer** (record in Deferred with the reason "V1 is additive-only; mechanism chosen at first major bump"), or autofix one sentence naming the path-prefix and package-major convention used by sibling modules.

#### R-15 — Authenticated principal → caller `PartyId` resolution seam is unbound

- **Severity:** Low. **Where:** AD-12 (Party state "from Parties adapters/projections"); memlog invocation decision ("caller PartyId/principal"); AD-4 snapshots "caller `PartyId`".
- **Problem:** Which module maps the authenticated subject to a `PartyId` (Tenants membership? Parties by external subject? platform identity in `EXT-HOST-1`?) is unstated. Both the API (6.1) and the UI (6.7) go through the same command, so divergence is contained, but the seam is a security decision the spine's Security dimension should name.
- **Disposition:** **Discuss** with platform; then one sentence in AD-12.

#### R-16 — OQ-4 notification posture is decided in the memlog but absent from the spine

- **Severity:** Low. **Where:** memlog "Approver notifications are non-authoritative adapters …"; FR-13 (in-product visibility only); spine has no row.
- **Problem:** Story 7.1 could add a notification adapter that another story treats as authoritative. Small, but it was a decision and it fell out.
- **Disposition:** **Autofix.** Add a Consistency Conventions row: "Notifications: in-product only (queue, count, Conversation status entry); any notification is a non-authoritative adapter over projections and never grants or removes approval rights."

#### R-17 — Notes for the other lenses (no spine change requested here)

- **Severity:** Low / informational.
- Verified-current lens: Stack rows are workspace-catalog pins verified on 2026-08-02; re-verify `Fluent UI Blazor 5.0.0-rc.4-26180.1`, `Aspire 13.4.6`, `Dapr 1.18.5`, `CommunityToolkit Aspire Dapr 13.4.1-beta.687`, `NSubstitute 5.3.0` (root override below the imported 6.0.0), and the five sibling commit pins (prior review noted they are worktree heads, not parent gitlinks).
- Code lens: `src/Hexalith.Agents.Server/Application/Tools/` exists (empty) despite AD-19; `src/Hexalith.Agents.Server/Application/Workflows/` is empty despite AD-18; both are worth a look.

## Good-Spine Checklist Walk

### 1. Fixes the real divergence points for the level below and misses none

**Verdict: Conditional pass.** Fixed: aggregate purity (AD-3), interaction snapshot (AD-4), proposal lifecycle (AD-5), Conversations/Parties boundaries (AD-6/7), provider boundary and readiness (AD-9/10), context bounds (AD-11), authorization posture (AD-12), external-effect ordering (AD-13), content/secret safety (AD-14), surface parity (AD-15), hosting ownership (AD-16), readiness authority (AD-17), execution owner (AD-18), safety stages (AD-20), cost (AD-21), data lifecycle (AD-22), NFR-11..14 contracts (AD-23..26). Divergence points two epics could hit with no AD: (a) aggregate inventory and keys for the budget ledger, policy, and governance aggregates that Epics 6 and 8 both touch — R-1; (b) the Conversations-surface **Call hexa** seam between the Conversations module and Epic 6 — R-2; (c) NFR-9/SM timestamp sources between Epics 6/7 (emitters) and 8.5 (calculator) — R-7; (d) `ProviderCatalog` tenant scope between 5.3 and 5.5 — R-9.

### 2. Every AD's Rule is enforceable and prevents its stated divergence

**Verdict: Conditional pass.** Coherent Binds/Prevents/Rule: AD-1, AD-3, AD-4, AD-5, AD-6, AD-7, AD-9, AD-11, AD-12, AD-13, AD-14, AD-15, AD-16, AD-18, AD-19, AD-20, AD-21, AD-22. Flags:
- AD-2: Rule enumerates three aggregates; Prevents "one tenant-wide hot aggregate" is contradicted by AD-21's tenant ledger with no reconciliation — R-1.
- AD-8: Rule contains a time-dependent conditional ("unless Conversations adds … before implementation") — not enforceable as written — R-10.
- AD-10, AD-17, AD-23, AD-24, AD-26: Rules are enforceable but are largely spec restatements; the enforceable kernel is buried — R-3. AD-17's "Tests cover aggregate purity, …" sentence is descriptive (a list of subjects), not a rule; acceptable as a test-scope floor but it belongs in the conventions companion.
- AD-16 and AD-10 Rules are followed by dated status notes that assert facts now false — R-4.
- AD-24 sentence "Dapr Workflow checkpoints and queries the identity" is a description of an implementation, not a constraint; delete or turn into "the workflow may cache but never redefines admission identity".

### 3. Nothing under Deferred could let two units diverge

**Verdict: Pass with one caveat.** Row 1 (owner field) is safe only if R-10 is applied — the AD-8 escape hatch is what makes it unsafe, not the Deferred row. Row 2 (Dapr Conversation API) and row 3 (memory, tools, MCP, A2A, DurableAgent, triggers, channels, multiple Agents) are hard-excluded by AD-18/19 and cannot be picked up by a V1 unit. The Deferred table names what it will not decide; it should additionally carry R-14 (versioning mechanism) if that disposition is chosen.

### 4. Named technology is verified-current

**Verdict: Not re-verified here (separate lens).** Gaps to note only: all Stack rows are "from imported workspace catalog" pins dated 2026-08-02; the register moved on 2026-08-09 and the codebase on 2026-08-04+ (Story 5.1). Rows most likely to have moved: Fluent UI Blazor rc, Aspire 13.4.x, Dapr 1.18.x, NSubstitute override, five sibling commit pins. `Provider SDK`/`Agent Framework SDK` = `Unselected` remains correct while `EXT-PROVIDER-1` is `Uncommitted`.

### 5. Ratifies rather than contradicts the brownfield codebase and platform conventions

**Verdict: Conditional pass.** Ratified: EventStore domain module, DomainService SDK host (state-instructions "two-line host"), no module-owned AppHost/ServiceDefaults (platform boundary rule — AD-16), FrontComposer + Fluent UI V5 (AD-15/25), xUnit v3/Shouldly/NSubstitute, `.slnx`, CPM, ULID identity implied by deterministic ids. Contradictions visible from documents: (a) seed vs conventions vs tree on the domain assembly — R-5; (b) silent deviation from the platform reference layout and omission of the platform instruction file from `sources` — R-6; (c) stale AppHost/`EXT-HOST-1` claims — R-4; (d) `TenantId`-on-every-contract convention vs unstated `ProviderCatalog` tenant key — R-9.

### 6. Covers the driving spec's capabilities

**Verdict: Pass with three gaps (FR-26 owner, NFR-9, NFR-4 partially).**

| Requirement | Binding ADs | Note |
| --- | --- | --- |
| FR-1 | AD-1, AD-2, AD-15 | |
| FR-2 | AD-2, AD-7 | |
| FR-3 | AD-2, AD-17 (`AgentActivation` family) | |
| FR-4 | AD-2, AD-9, AD-10, AD-14, AD-21 | catalog scope — R-9 |
| FR-5 | AD-2, AD-4, AD-9, AD-10 | |
| FR-6 | AD-2, AD-4 | |
| FR-7 | AD-2, AD-8 | escape hatch — R-10 |
| FR-8 | AD-6, AD-12, AD-18 | Conversations-surface entry unbound — R-2 |
| FR-9 | AD-10, AD-11, AD-18 | |
| FR-10 | AD-9, AD-10, AD-13, AD-20, AD-21, AD-24 | |
| FR-11 | AD-6, AD-7, AD-13 | |
| FR-12 | AD-6, AD-12, AD-20 | |
| FR-13 | AD-4, AD-5, AD-13, AD-18 | notification posture — R-16 |
| FR-14 | AD-5 | |
| FR-15 | AD-5, AD-8, AD-12 | |
| FR-16 | AD-5, AD-8, AD-10, AD-11, AD-13 | |
| FR-17 | AD-5, AD-6, AD-7, AD-8, AD-13 | |
| FR-18 | AD-5, AD-18, Time convention (24 h / 1 h–30 d) | |
| FR-19 | AD-12, AD-17 (`LR-TENANT-ACCESS`) | tenant key of platform-scoped aggregates — R-9 |
| FR-20 | AD-8, AD-12, AD-15 | |
| FR-21 | AD-6, AD-7, AD-9, AD-11, AD-12, AD-14, AD-20, AD-21 | |
| FR-22 | AD-14, AD-15, AD-25, AD-26 | |
| FR-23 | AD-9, AD-15, AD-17 | mechanism — R-14 |
| FR-24 | AD-1, AD-4, AD-5, AD-13, AD-14, AD-22, AD-23 | |
| FR-25 | AD-9, AD-10, AD-17, AD-21, AD-24, AD-26 | |
| FR-26 | AD-20 | **no owning aggregate for the policy itself** — R-1 |
| FR-27 | AD-20 | |
| FR-28 | AD-17, AD-20–AD-24, AD-26 | NFR-9 latency part unbound — R-7 |
| NFR-1 Security | AD-12, AD-15 | |
| NFR-2 Privacy | AD-12, AD-14, AD-22 | |
| NFR-3 Reliability | AD-5, AD-13, AD-18 | |
| NFR-4 Observability | AD-14, AD-16, AD-17 (projection inventory) | no AD names NFR-4 in Binds; adequate via projections |
| NFR-5 Auditability | AD-5, AD-14, AD-22 | |
| NFR-6 Provider safety | AD-9, AD-14 | |
| NFR-7 Content safety | AD-20 | |
| NFR-8 Context bounds | AD-11 | |
| NFR-9 Performance | **none** (AD-17 names the gate only) | R-7 |
| NFR-10 Cost | AD-21 | ledger key — R-1 |
| NFR-11 Recovery | AD-23 | |
| NFR-12 Capacity | AD-24 | |
| NFR-13 UI conformance | AD-25 | |
| NFR-14 UI performance | AD-26 | |
| OQ-1 | — | Conversations-surface seam — R-2 |
| OQ-2, OQ-3, OQ-6, OQ-7, OQ-8, OQ-9, OQ-10, OQ-12, OQ-13 | AD-18; Time convention; AD-21; AD-10; AD-22; AD-20; AD-11; AD-11; Naming convention | |
| OQ-4 | — | R-16 |
| OQ-5, OQ-11 | — | R-7 |

### 7. Parent-spine inheritance

**Verdict: N/A.** No parent spine declared; AD IDs are stable and no earlier valid AD is weakened by a later one.

### 8. Every altitude-owned dimension is decided, deferred, or open

**Verdict: Conditional pass — no dimension is silent; four are partially decided.**

| Dimension | Status | Where / gap |
| --- | --- | --- |
| Deployment & environments | Decided/gated | AD-16 (`EXT-HOST-1`), `EnvironmentProfile` in AD-17; stale gap note — R-4 |
| Infra / provider strategy | Decided/gated | AD-9, AD-16, Stack `Unselected` rows |
| Operations / observability | Partially decided | AD-14, AD-16, AD-17 projections; NFR-9 source unbound — R-7 |
| Security / identity | Partially decided | AD-12, AD-14, AD-20; principal→PartyId seam — R-15; catalog mutation authority — R-9 |
| Data lifecycle | Decided | AD-22, AD-14 payload protection |
| Testing strategy | Decided | AD-17 test floor, conventions companion, Evidence Levels |
| UI composition | Partially decided | AD-15, AD-25; cross-module **Call hexa** seam — R-2 |
| Versioning / compatibility | Decided (rules), mechanism open | AD-17 + FR-23; R-14 |
| Multi-tenancy | Partially decided | AD-12; platform-scoped aggregate tenant key — R-9 |
| Paradigm / decomposition | Decided | Design Paradigm, AD-1..AD-3; inventory incomplete — R-1 |
| Mutation / consistency | Decided | AD-3, AD-13, conventions |
| Runtime orchestration / time | Decided | AD-18, Time convention |
| External boundaries | Decided/gated | AD-6/7/9/11/20 + register |
| Cost / capacity / recovery | Decided | AD-21/23/24 |
| Beyond-V1 | Deferred safely | AD-19, Deferred table |

### 9. Spine hygiene

**Verdict: Fail on altitude and status hygiene; pass on lint, diagrams, and placeholders.**
- **Altitude:** AD-10, AD-13, AD-17, AD-23, AD-24, AD-26 read as implementation specs (field lists, enum members, thresholds, seven-step protocols, sample kinds). The invariant in each is real (single owner, ordering, fail-closed, fence-before-transport, no mixed clocks) and must stay; the enumerations are register/spec material and belong in the register the spine already names as normative — R-3.
- **Rationale leaking:** minimal; Design Paradigm paragraph 2 is descriptive rather than rationale — R-12. AD-8's "Current Conversations contracts expose … but no owner field" is rationale — fold into R-10.
- **Template comments / placeholders:** none (lint clean).
- **Diagrams:** all four mermaid blocks parse by inspection (balanced `alt/else/opt/end`, valid class and flowchart syntax). Semantic defect in the sequence diagram — R-8. The class diagram carries `LaunchReadinessRecord` with twelve attributes and three sample subclasses — same altitude concern as R-3 but acceptable as seed.
- **Stale "implementation gap" annotations inside AD rules:** AD-4, AD-10, AD-16, Structural Seed — status notes, not invariants, and now partly false — R-4.
- **Frontmatter oddities:** `binds: - hexa`, `- Hexalith Agents V1` — R-11; `updated: 2026-08-02` while the register it calls normative changed 2026-08-09 — R-4; `sources` omits the platform instruction file — R-6.
- **Deferred names what it won't decide:** yes, three rows with reasons; add R-14 if deferred.

## Disposition Summary

| ID | Severity | AD / section | Disposition |
| --- | --- | --- | --- |
| R-1 | High | AD-2, AD-21, AD-17 | Autofix (AD-2 inventory + keys + tenant scope) |
| R-2 | High | Capability map, AD-15, prerequisites table | Discuss, then autofix (new AD or AD-15 extension + `EXT-*` record) |
| R-3 | High | AD-10, AD-13, AD-17, AD-23, AD-24, AD-26 | Discuss scope, then autofix (keep invariants, point to register) |
| R-4 | Medium | AD-4, AD-10, AD-16, seed, prerequisites, frontmatter | Autofix (remove status notes; bump `updated`) |
| R-5 | Medium | Structural Seed vs conventions | Autofix seed |
| R-6 | Medium | Seed, `sources`, conventions table | Discuss, then autofix one convention row |
| R-7 | Medium | NFR-9 / SM metrics | Autofix (AD-17 extension or small AD) |
| R-8 | Medium | Sequence diagram | Autofix |
| R-9 | Medium | AD-2 / AD-10 catalog scope | Autofix after one platform answer |
| R-10 | Medium | AD-8 | Autofix |
| R-11 | Low | Frontmatter, AD titles | Autofix |
| R-12 | Low | Design Paradigm | Autofix |
| R-13 | Low | AD-12 | Autofix |
| R-14 | Low | AD-17 / FR-23 | Defer (record) or one-sentence autofix |
| R-15 | Low | AD-12 | Discuss |
| R-16 | Low | Conventions table | Autofix |
| R-17 | Info | Stack, code lens | Ignore here; other lenses |

After R-1, R-2, R-3 and the R-4/R-5/R-8/R-10 autofixes, no rubric item should block handoff.
