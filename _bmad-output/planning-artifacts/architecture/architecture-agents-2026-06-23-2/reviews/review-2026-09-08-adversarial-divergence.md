# Adversarial Divergence Review — 2026-09-08

## Verdict

**FAIL — 1 critical and 9 high findings require new or tightened ADs before Epics 6–8 are built on this spine; Epic 5 stories can proceed once A-2, A-5 and A-6 are closed.**

## Lens

Construct two units one level down (Epic 5–8 stories) that each obey every AD to the letter yet build incompatibly: clashing shared-data shapes, two owners of one entity, or conflicting state-mutation paths. Each pair is a hole to close with a new or tightened AD.

Reviewed authority: `ARCHITECTURE-SPINE.md` (updated 2026-08-02), `IMPLEMENTATION-CONVENTIONS.md`, `epics.md` (Epics 5–8, `RQ-1`), `launch-readiness-register.md`, `external-dependency-register.md` (updated 2026-08-09), and the prior review `review-2026-08-02-adversarial-divergence.md`. Where the shipped code already exhibits a permitted divergence, the file is cited as evidence that the hole is real, not as the target of the review.

## Relationship To The 2026-08-02 Review

The eight prior critical/high closures hold for the seams they named: readiness record selection, `Available`-only execution, the `(ProviderId, ModelId)` version key, the ordered attempt/admission state machine with `AdmissionFence`, `WeightedRoundRobinV1`, discriminated browser samples, frozen monotonic recovery, and the activation matrix row. This review does not repeat them. Three closures are shown to be incomplete one seam further in: A-2 (H-3's "global" key collides with the tenant-scoped catalog aggregate), A-5 (C-1's deterministic `ObservationId` has no derivation, so re-observation is either impossible or unbounded), and A-12 (H-4's state machine never assigns release/cancellation for attempts that terminalize before step 6). Prior M-1 (projection registry) and M-2 (session key) remain open and are not re-scored.

---

## Critical

### A-1 — Sensitive content can live in Dapr Workflow history and Agent Framework session state, outside every protection and deletion rule

- **Severity:** Critical
- **Units:** Story 6.2 context activity / Story 6.4 generation activity (Epic 6) vs Story 8.3 deletion and purge (Epic 8).
- **ADs obeyed by both:** AD-3 (side effects outside aggregates), AD-13 (descriptor stores digest, never raw context), AD-14 (no content in logs, telemetry, status, audit summaries), AD-18 (workflow history is execution state only), AD-22 (erase EventStore payloads and the named projection IDs), Consistency `Data planes`.
- **Incompatible choices:**
  - Unit 1 returns the complete authorized Conversation context and the prepared prompt as the *output* of the context activity and passes it as the *input* of the safety and generation activities. This is the natural Dapr Workflow shape; every activity input/output is persisted verbatim in the workflow state store as history, and any Agent Framework session/checkpoint inside the generation activity persists the same content. AD-14 lists logs, telemetry, browser measurements, status, and audit summaries; it never names workflow history or SDK session state. AD-13 forbids raw context only in the EventStore descriptor.
  - Unit 2 executes deletion exactly as AD-22 and the register require: cryptographic erasure of protected EventStore payloads plus restrictive completion for each named projection ID. Workflow history and session state are neither an EventStore payload nor a projection ID, so deletion reports `Complete` and `LR-AUDIT-PROTECTION-DELETION` can `Pass`.
- **Failure:** After a confirmed deletion (or 365-day retention expiry), the raw prompt, complete Conversation context, and generated text remain readable in the Dapr state store for every completed workflow, unprotected by EventStore payload protection, indefinitely (Dapr does not purge completed workflow state unless the application purges it). This is exactly what AD-22 "Prevents" (apparent deletion that leaves readable copies), and it is silent: no gate observes it.
- **Closing AD (new AD-27 — Execution-State Content Boundary):**
  - **Binds:** every Dapr Workflow activity, workflow input, timer payload, external-event payload, and any Agent Framework session/checkpoint used under AD-18.
  - **Prevents:** sensitive conversation-derived content persisting in workflow history or SDK session state where AD-14 protection and AD-22 erasure cannot reach it.
  - **Rule:** Workflow orchestrator inputs, activity inputs/outputs, timer and external-event payloads, and Agent Framework session/checkpoint state carry only identities, versions, digests, safe classifications, and protected-content *references* (`ProtectedContentReference` resolvable only through the EventStore payload-protection store). Content-bearing material is materialized inside an activity from the reference, used, and discarded; it never crosses an activity boundary. Agent Framework session persistence is disabled or backed by the same protected store keyed by `AgentInteractionId`/`AttemptId` and named in the AD-17 deletion inventory as `workflow-execution-state`. Completed or terminal interaction workflows are purged from the workflow state store within the interaction's terminal handling, and AD-22 deletion completion additionally confirms `workflow-execution-state` purge for the scope. A content-bearing activity contract is a boundary test failure.
- **Disposition:** Autofix (the AD text), then a Story 6.1 acceptance line proving no activity input/output contains raw content.

---

## High

### A-2 — `ProviderCatalog` is a tenant-scoped aggregate but `CapabilityVersion` and readiness are keyed globally by (`ProviderId`, `ModelId`)

- **Severity:** High (contains a cross-tenant disclosure path)
- **Units:** Story 5.3 catalog governance vs Story 5.5 readiness/provider-state contracts (consumed by 6.2, 6.4, 7.3).
- **ADs obeyed by both:** AD-2 (`ProviderCatalog` owns provider/model records), AD-9, AD-10 ("`CapabilityVersion` is a non-reusable unsigned monotonic sequence scoped to the global catalog key (`ProviderId`, `ModelId`)"), AD-12 (tenant isolation), AD-17 (`provider-capability-pricing` projection).
- **Incompatible choices:**
  - Unit 1 builds one `ProviderCatalog` aggregate stream per tenant (this is what ships today: `src/Hexalith.Agents/ProviderCatalog/ProviderCatalogState.cs` calls the aggregate id "the tenant's catalog id"), with the monotonic sequence advanced per tenant stream. Story 5.3's "cross-tenant Agent-selection is denied" and its isolation tests only make sense under this reading.
  - Unit 2 builds `ProviderReadinessResult` and the `provider-capability-pricing` projection keyed by the AD-10 "global catalog key" (`ProviderId`, `ModelId`) with one `CapabilityVersion` per key, because AD-10 and the register both say "global".
- **Failure:** Tenant A's entry `openai/gpt-x` is at version 4; tenant B updates its own `openai/gpt-x` entry to version 9. The global projection row now shows version 9 (tenant B's pricing, secret-configured state, and limits are visible to tenant A's readiness surface — a disclosure). Tenant A's next context build observes live version 9, advances its high-water mark to 9, and a later read of tenant A's own catalog returns 4: "any observed decrease is a blocker", so every tenant A interaction is `Blocked/CapabilityVersionRegressed` permanently. Alternatively, if unit 1 is global and unit 2 tenant-scoped, one tenant's administrator disables a model for every tenant.
- **Closing AD text (tighten AD-2 and AD-10):**
  - AD-2 Rule, add: "`ProviderCatalog` is tenant-scoped: one aggregate stream per `TenantId`; its entries are keyed (`TenantId`, `ProviderId`, `ModelId`). No platform-wide catalog exists in V1."
  - AD-10 Rule, replace "scoped to the global catalog key (`ProviderId`, `ModelId`)" with "scoped to the catalog-entry key (`TenantId`, `ProviderId`, `ModelId`); comparisons across keys are undefined and forbidden. `provider-capability-pricing`, `ProviderReadinessResult`, the interaction snapshot, the high-water mark, and `EffectiveProviderCapabilityVersion` all carry the full entry key."
  - Register "Provider Readiness Contract": same replacement.
- **Disposition:** Autofix. (Also reconcile the 2026-08-02 review's H-3 closure note, which chose "global".)

### A-3 — Three legal time authorities for one durable instant: `ExpiresAt`, terminal timestamps, and request time

- **Severity:** High
- **Units:** Story 7.1 proposal creation (stores `ExpiresAt`) vs Story 7.6 expiry timer (fires and expires); same pattern for Story 8.1 retention (365 days "after that terminal timestamp").
- **ADs obeyed by both:** AD-3 (no timers in aggregates), AD-5, AD-18 (workflow owns timers), Consistency `Time` ("injected time and stored policy … no aggregate wall-clock reads"), IMPLEMENTATION-CONVENTIONS (trusted result supplied by the orchestrator).
- **Incompatible choices:**
  - Unit 1 computes `ExpiresAt = EventStore commit timestamp of ProposedAgentReplyCreated + lifetime` (the shipped aggregate already declares "request time is the EventStore event-metadata timestamp, server-stamped at persist", `AgentInteractionAggregate.cs`).
  - Unit 2 (the workflow) computes the same deadline as `context.CurrentUtcDateTime + lifetime` inside the create activity — the only replay-safe clock a Dapr orchestrator may read — and schedules its durable timer on it. A third compliant unit uses the Server `TimeProvider` at command handling (the UI already registers `TimeProvider.System`, `AgentsUiServiceCollectionExtensions.cs`).
- **Failure:** The timer's instant precedes the stored `ExpiresAt` by the command/commit latency. The timer fires, the expiry orchestrator evaluates "elapsed?" against the stored deadline (as the shipped `AgentInteractionProposalExpiryOrchestrator` does with its request-supplied "now"), finds it not elapsed, dispatches nothing — and the timer is consumed. Unless a re-arm rule exists (none is bound), the proposal never expires; AD-5's "expired cannot post" and SM-3's 26-hour terminal rate silently degrade. With the opposite skew the proposal expires before its published deadline. Retention has the same ambiguity (terminal event payload timestamp vs metadata vs workflow clock), producing deadlines that differ by seconds and audit disputes about "exactly 365 days".
- **Closing AD text (new AD-28 — Time Authorities):**
  - **Binds:** every stored instant, deadline, freshness evaluation, and timer.
  - **Prevents:** the same deadline being computed from different clocks by the aggregate, the projection, the workflow, and the UI.
  - **Rule:** (a) `DomainInstant` — every durable business instant (request time, version creation, terminal decision time, `ExpiresAt` base, retention base) is the EventStore event-metadata commit timestamp of the event that creates it; the aggregate stores the derived deadline in the event payload once. (b) `EvaluationInstant` — every server-side comparison against a stored instant or `ValidUntil` uses one injected `TimeProvider` supplied to the orchestrator and stamped into the trusted command/result as `EvaluatedAt`; aggregates compare only supplied instants. (c) `SchedulingInstant` — Dapr Workflow uses `CurrentUtcDateTime` only to schedule; a timer activity re-reads the stored deadline, and if `EvaluatedAt < ExpiresAt` it re-arms for the remainder (bounded to at most three re-arms, then records a safe `TimerDrift` blocker). (d) Monotonic fixture/browser clocks (AD-23, AD-26) measure durations only and never produce a `DomainInstant`. (e) The UI never re-evaluates deadlines or `ValidUntil` against a browser clock; it renders the server's `EvaluatedAt`-relative result.
- **Disposition:** Autofix.

### A-4 — "Fresh" and "stale" have two definitions and no single clock authority across projections, API, and UI

- **Severity:** High
- **Units:** Story 5.5 readiness projection / provider-state (validity-window freshness) vs Story 5.2 setup read model and Story 8.7 inspection UI (revision-lag freshness), plus Story 6.7 UI rendering.
- **ADs obeyed by both:** AD-10 (`ObservedAt <= now < ValidUntil`), AD-12 (fail closed on stale projections), AD-17 (each gate declares its own validity; no global implicit freshness), Consistency `Projections` ("stale state must not be rendered or treated as fresh").
- **Incompatible choices:**
  - Unit 1 defines stale as time-based: `T >= ValidUntil` for readiness records and Provider readiness, with `T` taken from its own server clock at query time.
  - Unit 2 defines stale as revision-based: a projection whose version is behind the caller's `expectedProjectionVersion`/`expectedCapabilityVersion` (shipped `ProviderCatalogViewFactory` does exactly this) and exposes a `Freshness` field on that basis; Story 5.2's read model "freshness" has no `ValidUntil` at all. A third unit (UI) takes `ValidUntil` from the API payload and compares it with the browser clock to decide whether to render `Stale`.
- **Failure:** The same record is `Fresh` to the API (version matches) and `Stale` to readiness (window expired), or `Pass` on the server and `Stale` in the browser two seconds later because of clock skew, violating Story 5.5's "same result through projection, API/client, and UI". `ProviderReadinessReasonCode.Stale` (AD-10) and `LaunchReadinessRecord.State = Stale` (AD-17) are different enums for different conditions with no mapping rule, so `agent-setup-readiness` cannot compose them deterministically.
- **Closing AD text (tighten AD-17; cross-reference AD-28):** "Freshness is a versioned discriminated value `Freshness { Basis: ValidityWindow | RevisionLag; EvaluatedAt; ExpectedRevision?; ObservedRevision?; ValidUntil? }` evaluated once on the server at `EvaluatedAt` (AD-28 b) and carried unchanged to API, BFF, and UI, which render it and never recompute it. Readiness records and Provider readiness use `ValidityWindow`; read-your-writes queries use `RevisionLag`; a surface that combines both reports `Stale` if either is stale. `ProviderReadinessReasonCode.Stale` maps to `LaunchReadinessRecord.State = Stale` on `LR-PROVIDER` and to `Blocked` callability; no other mapping exists."
- **Disposition:** Autofix.

### A-5 — `ObservationId` has no derivation, so periodic re-observation is either a rejected conflict or unbounded replacement (prior C-1 closure incomplete)

- **Severity:** High
- **Units:** Story 5.6 / 6.5 evidence producers (re-observe `LR-TOPOLOGY`, `LR-PROVIDER`, `LR-CAPACITY-FAIRNESS` on a schedule to renew `ValidUntil`) vs Story 5.5 `LaunchReadinessGate` aggregate (idempotent exact duplicate, reject conflicting duplicate).
- **ADs obeyed by both:** AD-17 ("Immutable observations carry deterministic `ObservationId`"; "Duplicate `ObservationId` with the same payload is an idempotent no-op; a conflicting payload is rejected and audited"), register "Record Authority And Supersession".
- **Incompatible choices:**
  - Unit 1 (aggregate) derives `ObservationId = hash(GateId, TenantScope, EnvironmentProfile, SourceVersion)` — deterministic per source version, as C-1's closure implied.
  - Unit 2 (producer) submits a fresh observation every hour with the same `SourceVersion` (nothing changed) and a new `ObservedAt`/`ValidUntil`/`EvidenceReference`.
- **Failure:** Under unit 1 the second submission has the same id and a different payload → rejected as a conflicting duplicate → the gate can never renew freshness without inventing a new `SourceVersion` (which the register defines as the immutable observed component version — falsifying it). The gate goes `Stale` forever and `AgentActivation`/`AgentCallAcceptance` block. If instead the producer includes `ObservedAt` in its id and the aggregate does not, nothing is ever a duplicate and a producer retry storm creates unbounded revisions with no idempotency.
- **Closing AD text (tighten AD-17):** "`ObservationId = SHA-256(length-prefixed GateId, TenantScope, EnvironmentProfile, SourceVersion, EvidenceReference, ObservedAt)` computed by the submitting orchestrator with the shared canonicalizer; the aggregate recomputes and rejects a mismatch. Re-observation with unchanged `SourceVersion` is legal and produces a new revision; an exact payload duplicate is a no-op; the same id with a different payload is a conflict."
- **Disposition:** Autofix.

### A-6 — `TenantScope` and `EnvironmentProfile` identifiers are unbound, so platform-wide gates can never satisfy a tenant-scoped evaluation

- **Severity:** High
- **Units:** Story 5.6 topology / Story 6.5 capacity / Story 6.1 recovery producers (platform-wide evidence) vs Story 5.7 `AgentActivation` and Story 8.7 inspection (tenant-scoped evaluation).
- **ADs obeyed by both:** AD-17 (logical key is (`GateId`, `TenantScope`, `EnvironmentProfile`); "Missing records block"), register schema (`TenantScope`: "Exact tenant or explicitly authorized cohort"), matrix rows requiring `LR-TOPOLOGY` for every family.
- **Incompatible choices:**
  - Unit 1 records `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-SECRETS`, `LR-RECOVERY`, `LR-CAPACITY-FAIRNESS` once with `TenantScope = "platform"` (or `"*"`), because those controls are not tenant-specific and re-running a production-like failure injection per tenant is absurd.
  - Unit 2 evaluates `AgentActivation` for tenant A by reading the exact key (`LR-TOPOLOGY`, `A`, `production-like`) — "exact tenant" — and emits `GateRecordMissing`.
- **Failure:** Activation and every call are blocked for every tenant although all evidence exists; or, if unit 2 implements cohort matching with its own syntax (`"*"`, `"platform"`, `"cohort:eu"`), two consumers disagree on which record applies. `EnvironmentProfile` has the same gap (`production-like` vs `production-like@3`): a fixture version bump makes every record `Stale` for one consumer and current for another. Separately, the aggregate accepts any server-trusted command for any `GateId`, so a Story 8.6 UI-evidence producer can (accidentally) write an `LR-PROVIDER` observation; the `Owner` field is descriptive only.
- **Closing AD text (tighten AD-17):** "`TenantScope` is a closed grammar: `tenant:<TenantId>` or `platform`. Each minimum `GateId` declares `ScopeKind ∈ {Tenant, Platform}` in the register: `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-SECRETS`, `LR-RECOVERY`, `LR-CAPACITY-FAIRNESS`, `LR-UI-CONFORMANCE`, `LR-UI-PERFORMANCE`, `LR-RUNTIME-PERFORMANCE` are `Platform`; all others are `Tenant`. Evaluation for a tenant reads `platform` records for `Platform` gates and `tenant:<id>` records for `Tenant` gates; no other cohort syntax exists in V1. `EnvironmentProfile` is `<name>@<ProfileVersion>`; a profile version change invalidates all records of the old profile. A `GateId` outside the minimum inventory is rejected at submission. Each `GateId` binds an `AuthorizedProducer` role; the submitting orchestrator's principal must hold it or the command is rejected before append."
- **Disposition:** Autofix (grammar and scope kinds); discuss the exact `Platform`/`Tenant` assignment for `LR-UI-*`.

### A-7 — `OperationGateMatrixVersion = 1` has no family for edit, expiry, retention, hold release, export download, or audit inspection, and "missing family blocks"

- **Severity:** High
- **Units:** Story 7.2 edit / Story 7.6 expiry activity / Story 8.1 retention timer (commands with no matrix row) vs Story 5.5 matrix consumer ("a missing operation family … blocks with a safe code; consumers may not maintain local subsets").
- **ADs obeyed by both:** AD-12, AD-17 (single matrix), AD-18 ("Each replay-safe activity re-evaluates the applicable authorization/policy gates").
- **Incompatible choices:**
  - Unit 1 maps edit to `ProposalResolution` (nearest fit) and treats timer-initiated expiry/retention as exempt from the matrix because no human "operation" occurs.
  - Unit 2 applies the text literally: edit has no family → blocked; the expiry activity re-evaluates gates (AD-18) → no family → blocked; proposals never expire and edits are impossible. A third unit invents `ProposalEdit` locally, which AD-17 forbids.
- **Failure:** Either a whole story is undeliverable under the letter of AD-17, or each consumer silently picks a different family, so the advisory lock, the gate set, and the readiness blocker shown to the user differ between API and UI for the same command.
- **Closing AD text (tighten AD-17 and the register matrix):** "`OperationGateMatrixVersion = 2` adds immutable rows: `ProposalEdit` (`LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, `LR-AUDIT-PROTECTION-DELETION`), `ProposalRegeneration` = the `ProviderInvocation` set, `SystemTimer` (`LR-EVENTSTORE` only; used by expiry, retention-due, and queue-expiry activities, which require no human authorization but must not append when EventStore readiness is blocked), `LegalHoldRelease` = `LegalHold` set, `ExportDownload` = `ExportRequest` set, `AuditInspection` = `ReadinessInspection` set. Every public command and every workflow activity declares exactly one family in its contract; a command with no declared family fails contract tests, not runtime."
- **Disposition:** Autofix.

### A-8 — Deterministic ids are named but not derived: `AttemptId`, `ReservationId`, `AdmissionId`, `QueueId`, `ProviderIdempotencyKey`

- **Severity:** High
- **Units:** Story 6.4 generation attempt vs Story 7.3 regeneration attempt; Story 6.4 vs the `EXT-PROVIDER-1` adapter's "outcome lookup by `AttemptId`".
- **ADs obeyed by both:** AD-13 (deterministic ids, retry reuses the exact identity, "never substitutes a replacement attempt"), AD-21 (deterministic `ReservationId`), AD-24 (admission keyed by `AttemptId`), Consistency `Idempotency` ("derive deterministic ids from interaction/version context").
- **Incompatible choices:**
  - Unit 1 derives `AttemptId = "attempt-" + AgentInteractionId` (shipped: `AgentInteractionGenerationOrchestrator.cs:339`); unit 2 derives `AttemptId = SHA-256(AgentInteractionId, SourceConversationId, RegenerationAttemptId)` (shipped: `AgentProposalRegenerationIdentity.cs:36`). Both are "deterministic from interaction context".
  - A third compliant unit derives `AttemptId = hash(AgentInteractionId, RequestFingerprint)`: a regeneration whose fingerprint equals the original (unchanged conversation, same prompt) then *is* the original attempt — the Provider idempotently returns the old output and no regeneration happens, or the aggregate rejects a "duplicate attempt".
  - `ProviderIdempotencyKey` is listed separately from `AttemptId` in the `ProviderAttempt` class; Story 6.4 says "`AttemptId` is the Provider idempotency key". If the adapter is called with a derived key but recovery looks up by `AttemptId`, recovery misses and re-invokes.
  - `ReservationId`/`AdmissionId`/`QueueId`: one unit uses `AttemptId` verbatim, another hashes `(AttemptId, ReservationId)`; the allocator's "no duplicate admission for the same `AttemptId`" and the ledger's "retries query and reuse" then look up different keys.
- **Failure:** Duplicate Provider work or double reservation under retry/recovery, or a regeneration that can never produce a new version — all while every unit passes its own determinism test.
- **Closing AD text (new AD-29 — Deterministic Identity Derivation):**
  - **Binds:** every id in the Consistency `Identity` and `Idempotency` rows and every id in `ProviderAttempt`, `ReadinessObservation`, `BrowserTimingSample`.
  - **Prevents:** two derivations of the same identity.
  - **Rule:** One shared `AgentsIdentity` canonicalizer (the AD-13 fingerprint canonicalizer: UTF-8, length-prefixed, `U+001F` separators, SHA-256, lowercase hex) with a purpose tag per id kind. `AgentInteractionId = H("interaction", TenantId, AgentId, SourceConversationId, CallerPartyId, IdempotencyKey)`. `AttemptId = H("attempt", AgentInteractionId, AttemptOrdinal)` where `AttemptOrdinal` is the aggregate-assigned 1-based count of prepared descriptors on the interaction (generation = 1; each regeneration increments); a transport retry never increments. `ProviderIdempotencyKey = AttemptId` exactly; `EXT-PROVIDER-1` lookup and invocation use it verbatim. `ReservationId = H("reservation", AttemptId)`; `AdmissionId = H("admission", AttemptId)`; `QueueId = AdmissionId`. `ProposalVersionId = H("version", AgentInteractionId, VersionOrdinal, Kind)`. `MessageId = H("message-id", AgentInteractionId, VersionId)` and posting `IdempotencyKey = H("idempotency-key", AgentInteractionId, VersionId)` (already shipped in `AgentResponsePostingIdentity.cs`; the spine now binds it). `ObservationId` per A-5, `SampleId` per AD-26. Ids never embed raw tenant text; tenant disjointness is inside the hash input.
- **Disposition:** Autofix; then align `AgentInteractionGenerationOrchestrator.DeriveAttemptId` (deferred-work item).

### A-9 — "Policy at least as restrictive" has no defined order, so a version comparison legally weakens a retry

- **Severity:** High
- **Units:** Story 6.3 retry path ("policy at least as restrictive as its initial attempt") vs Story 8.4 policy publication (future-only, loosening permitted for future calls).
- **ADs obeyed by both:** AD-20, Consistency `Safety` ("retries cannot weaken policy"), Story 8.4's "safety high-water mark".
- **Incompatible choices:**
  - Unit 1 implements the floor as `CurrentPolicyVersion >= SnapshotPolicyVersion` — the only comparable scalar the snapshot carries (AD-4 `ContentSafetyPolicyVersion`).
  - Unit 2 publishes version 5 that removes a restricted category from "requires confirmation" (a legal future-only loosening under AD-20).
- **Failure:** A transient-retry of an attempt snapshotted at version 4 evaluates under version 5, passes a category that version 4 would have blocked, and posts — the exact "weaker retry path" AD-20 prevents — while both units pass their tests. A second unit that compares category sets semantically reaches the opposite decision, so evidence for `LR-SAFETY` differs by implementer.
- **Closing AD text (tighten AD-20):** "Restrictiveness is not derivable from `PolicyVersion`. Each published policy version carries an aggregate-assigned non-decreasing `RestrictivenessRank` and an explicit `ChangeKind ∈ {Tightens, Loosens, Neutral}` declared by the publisher and validated against the category matrix; `Loosens` keeps the previous rank, `Tightens` increments it. A retry evaluates under the snapshot version, or under the current version only if `current.RestrictivenessRank >= snapshot.RestrictivenessRank`; otherwise it evaluates under the snapshot version exactly. The evaluated `(PolicyVersion, RestrictivenessRank)` is recorded on the attempt outcome."
- **Disposition:** Autofix.

### A-10 — Governance aggregates are unnamed: policy, budget policy, legal hold, export, deletion, and retention have no owner, identity, or authority-vs-projection rule

- **Severity:** High
- **Units:** Story 8.1 legal hold (creates `LegalHoldAggregate`) vs Story 8.3 deletion ("legal-hold state is evaluated"; "any active hold protects the content") and Story 8.1 retention timer ("rechecks current hold state").
- **ADs obeyed by both:** AD-1 (Agents owns governance state), AD-2 (names only `Agent`, `ProviderCatalog`, `AgentInteraction`), AD-13/AD-21 (budget ledger aggregate), AD-17 (`LaunchReadinessGate` aggregate), AD-22.
- **Incompatible choices:**
  - Unit 1 makes `LegalHold` its own aggregate keyed by a hold id with a "protected scope" expression (tenant + content class + time range), and Story 8.4 makes `ContentSafetyPolicy` and `TenantBudgetPolicy` tenant-level aggregates (the story's test names say so).
  - Unit 2 evaluates "active hold" by reading the `legal-hold` projection (the only named surface), and Story 5.2/1.7 kept the content-safety policy on the `Agent` aggregate, so `ContentSafetyPolicyVersion` in the AD-4 snapshot is an Agent-scoped counter in one unit and a tenant-scoped counter in the other — two different numbers under one field name, which A-9's floor then compares.
- **Failure:** A hold accepted at revision *n* is not yet in the projection when the deletion command is evaluated; deletion proceeds and erases held content (unrecoverable) while both units honored "fail closed on stale projections" only where they thought a projection was involved. Scope-expression semantics ("matching retention expiry") differ between the hold writer and the retention evaluator. The safety floor compares versions from different scopes.
- **Closing AD text (tighten AD-2):** "V1 aggregates are exactly: `Agent` (per `AgentId`), `ProviderCatalog` (per `TenantId`), `AgentInteraction` (per `AgentInteractionId`), `BudgetLedger` (per A-11 key), `LaunchReadinessGate` (per logical gate key), `TenantGovernancePolicy` (per `TenantId`; owns content-safety policy versions, budget policy versions, retention/expiry policy versions with one `RestrictivenessRank` per safety version), `LegalHold` (per `HoldId`; scope = closed grammar `tenant:<id>` + `interaction:<AgentInteractionId>` list or `class:<ContentClass>` + `[from,to)` UTC range), `AuditExport` (per `ExportId`), `ProtectedDeletion` (per `DeletionRequestId`). `ContentSafetyPolicyVersion` in the AD-4 snapshot is the `TenantGovernancePolicy` version; the `Agent` aggregate stores no safety policy. Any decision whose wrong answer is unrecoverable (deletion under hold, erasure, export scope) reads hold/policy state from the owning aggregate at an expected revision, never from a projection; projections serve inspection only."
- **Disposition:** Discuss the aggregate list (one meeting), then autofix.

---

## Medium

### A-11 — Budget ledger period identity and month boundary are unbound

- **Severity:** Medium
- **Units:** Story 6.4 reservation (reserve against "monthly tenant budget") vs Story 6.4/7.3 reconciliation and the `budget-reservation-usage` projection.
- **ADs obeyed:** AD-21 ("single authority for balances and deterministic `ReservationId`"; "numeric monthly tenant budget"), AD-13, AD-28-candidate `Time`.
- **Incompatibility:** Unit 1 keys the ledger stream (`TenantId`, `YYYY-MM` of the reservation's `DomainInstant`) and books reconciliation to the reservation's period; unit 2 keys by (`TenantId`) with a rolling window or books reconciliation to the usage instant's month. A reservation at 23:59:30 UTC on the 31st reconciled at 00:00:10 on the 1st counts in different months for each; 80%/100% evaluation differs; a monthly budget update (Story 8.4 `TenantBudgetUpdate`) "effective time" applies to a different period.
- **Failure:** Overspend by up to one reservation at each month boundary, or a spurious 100% block on the 1st; `LR-COST` evidence non-reproducible.
- **Closing AD text (tighten AD-21):** "`BudgetLedger` aggregate id = (`TenantId`, `BudgetPeriod`) where `BudgetPeriod` is the UTC calendar month of the reservation's `DomainInstant`. Reconciliation, release, and warnings always post to the reservation's period. A budget policy version applies to periods starting at or after its effective `DomainInstant`; the current period keeps the cap it started with unless the new cap is lower (tightening applies immediately)."
- **Disposition:** Autofix.

### A-12 — No owner releases the reservation or cancels the queue entry when an attempt terminalizes before step 6 (prior H-4 closure incomplete)

- **Severity:** Medium
- **Units:** Story 7.6 expiry / Story 7.5 reject (terminalize the proposal) vs Story 6.5 allocator and Story 6.4 ledger holding a queued regeneration attempt's `QueueId` and `ReservationId`.
- **ADs obeyed:** AD-13 (steps 1–7; "crash before step 4 permits no Provider call"), AD-21 ("unused reservation is released only after an authoritative no-usage result"), AD-24 ("Cancellation or queue expiry is a durable terminal admission result").
- **Incompatibility:** The AD-13 machine defines release only at step 7 (after an outcome). When the interaction becomes terminal from a different path (expiry timer, rejection, abandonment) while an attempt sits at steps 2–3, unit 1 (proposal terminalization) records `Expired` and does nothing else (its only obligation is AD-5). Unit 2 (allocator) keeps the queue entry until queue expiry and later admits the `AttemptId`; the workflow then attempts step 4, is rejected by the terminal guard, and — because "authoritative no-usage result" is undefined for a never-invoked attempt — neither unit issues the ledger release. The reservation is held until an operator intervenes; monthly budget is consumed by phantom reservations.
- **Failure:** Permanent budget leakage and stale queue occupancy counting toward `PerTenantQueueDepthLimit`.
- **Closing AD text (tighten AD-13):** "Terminalization of an `AgentInteraction` with a non-terminal attempt at steps 1–5 emits `AttemptCancelled(AttemptId, Reason)` in the same command result. The workflow, on observing it (or on recovery, by inventory of attempts without outcome), performs in order: allocator `Cancel(AttemptId)` (durable terminal admission result), ledger `Release(ReservationId, NoUsage: attempt never authorized/invoked)` — this event is the authoritative no-usage result for steps 1–5 — and appends `AttemptReleased`. Reservations older than the interaction's maximum lifetime without `AttemptReleased` are inventoried by the AD-23 recovery manifest as leaked."
- **Disposition:** Autofix.

### A-13 — Public error/rejection contract shape and HTTP mapping are unbound; cross-tenant indistinguishability is implemented per story

- **Severity:** Medium
- **Units:** Story 5.4 cross-tenant denial ("timing-safe … no existence signal") vs Story 7.2 unauthorized edit ("denial before content read"); the Client (Story 4.1 lineage) and UI gateways that map results.
- **ADs obeyed:** AD-12, AD-15 (shared contracts and authorization outcomes), Consistency `Errors` ("typed rejection/status events or structured public errors").
- **Incompatibility:** Unit 1 collapses unauthorized, cross-tenant, and missing to one `NotAccessible` result with HTTP 404; unit 2 returns 403 for "no edit authority" and 404 for "no such proposal". The shipped contracts already carry 20+ per-operation enums (`AgentProposalEditFailureReason`, `AgentProposedReplyNotEditableReason`, `ProposalRejectionStatus` in UI gateways, …) with no shared envelope, retryability flag, or correlation reference.
- **Failure:** The UI's generic mapping shows "you lack permission" on one route (leaking existence) and "not found" on another; a BFF retry policy retries 409 from one story and 422 from another; SM-4 ("zero successful unauthorized actions") evidence cannot be computed uniformly; audit references differ.
- **Closing AD text (new AD-30 — Public Outcome Envelope):** "Every public command/query result is `AgentsOutcome { Status ∈ {Accepted, Completed, Rejected, NotAccessible, Conflict, Blocked, Unavailable}, Code (versioned additive enum per operation), Retryable: bool, CorrelationReference, ExpectedRevision?, ObservedRevision?, Freshness (A-4), Blockers[] (AD-17 safe codes) }`. HTTP mapping is fixed: `NotAccessible` → 404 for both unauthorized and missing (never 403 on tenant-scoped resources), `Conflict` → 409, `Rejected` → 422, `Blocked` → 423, `Unavailable` → 503. UI gateways may not define their own status enums; they render `AgentsOutcome`."
- **Disposition:** Autofix the envelope; discuss the exact status-code table.

### A-14 — Command idempotency metadata semantics are asserted in the conventions table but backed by no AD

- **Severity:** Medium
- **Units:** Story 5.2 configuration commands ("exact duplicates are idempotent, conflicts typed") vs Story 6.7 `Call hexa` ("captures … idempotency exactly once").
- **ADs obeyed:** Consistency `Idempotency` ("API commands accept idempotency metadata"), AD-13 (deterministic ids), AD-29-candidate.
- **Incompatibility:** Unit 1 defines "exact duplicate" as same `IdempotencyKey` within (`TenantId`, command type) regardless of payload and returns the first result; unit 2 compares a payload fingerprint and returns `Conflict` on mismatch; retention of keys is 24 h in one and unbounded in the other; scope includes the user session in one and not the other.
- **Failure:** A client retry with a corrected payload silently applies the old payload (unit 1) or is rejected (unit 2); a key reused after 24 h creates a second interaction in one unit and a no-op in the other.
- **Closing AD text (tighten AD-13):** "Idempotency scope is (`TenantId`, `CommandType`, `IdempotencyKey`). The aggregate stores `PayloadFingerprint` (AD-13 canonicalizer) with the first acceptance; same key + same fingerprint → idempotent replay of the recorded outcome; same key + different fingerprint → `Conflict`. Keys are retained for the aggregate's lifetime (they are events), so there is no expiry window. Where the key derives the aggregate id (A-8), the aggregate stream is the idempotency record."
- **Disposition:** Autofix.

### A-15 — "Versioned and additive-first" has no rule two epics apply the same way

- **Severity:** Medium
- **Units:** Story 7.3 (adds a `ProposalVersionKind` member or a new field to `ProposalVersion`) vs Story 8.2 export manifest / Story 8.5 metric calculators consuming those events and projections.
- **ADs obeyed:** AD-17 ("Public contracts are versioned and additive-first"), AD-10 (reason-code enum with `Unknown = 0`, unknown fails closed).
- **Incompatibility:** Unit 1 adds an enum member additively; unit 2 follows the AD-10 precedent (unknown enum → fail closed) and marks every proposal with the new kind as invalid, so export and SM-3 audit completeness drop to non-100%. Conversely a projection fold that ignores unknown members silently misclassifies. Event evolution (new event type `…V2` vs in-place optional field) and fold rebuild obligations are unspecified.
- **Failure:** A legal additive change in one epic breaks evidence in another with no compile-time signal.
- **Closing AD text (new AD-31 — Contract Evolution):** "Additive = new optional field with a defined default, or new enum member above the last. Every public enum has `Unknown = 0`. Consumers that *decide* (readiness, safety, callability, deletion) fail closed on `Unknown`; consumers that *record or count* (audit, export, metrics) pass the raw value through under an explicit `UnrecognizedValue` classification and never drop the record. Breaking changes ship as a new type with a `V<n>` suffix and a new event name; folds declare the event versions they accept and a projection is rebuilt when its fold version changes. API routes are versioned by path (`/api/v1`) and are additive within a major."
- **Disposition:** Autofix.

### A-16 — The prepared-request fingerprint's field inventory is unspecified, so a policy or catalog change either kills every in-flight retry or none

- **Severity:** Medium
- **Units:** Story 6.4 descriptor/fingerprint builder vs Story 8.4 policy publication and Story 6.3 no-weaker retry.
- **ADs obeyed:** AD-13 (fingerprint change fails closed under the attempt), Consistency `Provider attempt fingerprint` ("length-prefixed, stable-order prepared provider request"), AD-20 (retry may use at-least-as-restrictive policy).
- **Incompatibility:** Unit 1 includes `ContentSafetyPolicyVersion`, `EffectiveProviderCapabilityVersion`, and pricing version in the hashed request (they are "prepared request" inputs); unit 2 hashes only what is sent to the Provider (model, messages, parameters). Under unit 1 any tightening publication changes the fingerprint and AD-13 forbids the retry that AD-20 permits; under unit 2 the retry proceeds. "Stable order" (declaration vs ordinal), Unicode normalization of prompt text, and line-ending handling are also unbound, so two canonicalizers hash the same request differently.
- **Failure:** Divergent retry eligibility and non-reproducible fingerprints across Server and Testing projects.
- **Closing AD text (tighten the `Provider attempt fingerprint` convention into AD-13):** "The fingerprint covers exactly the Provider-bound request: `ProviderId`, `ModelId`, ordered message roles and NFC-normalized UTF-8 text with `\n` line endings, generation parameters, `MaxOutputTokens`, timeout. It excludes policy versions, capability version, pricing, and identities, which are bound separately on the descriptor and compared by their own rules (AD-10, AD-20, A-9). Field order is the descriptor's declared order; the canonicalizer is a single shared type in `Hexalith.Agents` and is the only implementation."
- **Disposition:** Autofix.

### A-17 — Identity table says Conversations owns `MessageId`; AD-13 says Agents derives it

- **Severity:** Medium
- **Units:** Story 6.6 posting (supplies derived `MessageId` and idempotency key) vs the `EXT-CONV-AI-1` append seam (Conversations-owned identity).
- **ADs obeyed:** Consistency `Identity` ("Conversations owns `ConversationId` and `MessageId`"), AD-13 ("posting uses deterministic `MessageId` … derived from `AgentInteractionId` plus … `VersionId`"), AD-6.
- **Incompatibility:** If the committed append seam assigns its own `MessageId` (as an owner would), the "final `MessageId`" in audit evidence is whatever Conversations returned, and the derived id is only a client hint; recovery "by deterministic `MessageId`" then queries an id Conversations never stored. The register's `EXT-CONV-AI-1` artifact does not require caller-supplied ids to be honored.
- **Failure:** Recovery after crash-after-append cannot find the message by the derived id, appends again with the same idempotency key (safe only if Conversations dedupes on the key, also unrequired), or records a `MessageId` that differs from the audit link.
- **Closing AD text (tighten AD-13 and `EXT-CONV-AI-1`):** "Agents *derives* and *supplies* `MessageId` and idempotency key; Conversations *owns* the stored message and must (per `EXT-CONV-AI-1` contract) either persist the supplied `MessageId` verbatim or reject the append; a differing returned id is a typed incompatibility that blocks posting. Recovery looks up by the supplied idempotency key, not by content."
- **Disposition:** Autofix the AD; add the clause to the register's `EXT-CONV-AI-1` `RequiredArtifact` (owner acceptance needed → discuss).

---

## Low

### A-18 — Two "operation family" vocabularies and a register/AD contradiction on stricter gates

- **Severity:** Low
- **Units:** UI advisory lock (Story 8.4 uses "any high-impact command") vs API lock (AD-12's six families); Story 8.7 inspection vs a stricter-gate producer.
- **ADs obeyed:** AD-12 (six normative families), AD-17 (thirteen matrix families; "gate outside the minimum inventory blocks"), register ("Implementations may add stricter gates").
- **Incompatibility:** One unit locks `AgentActivation` and `ProviderCatalogMutation` per session, the other does not; a stricter gate can be recorded but can never appear in a matrix row, so it is never consumed — the register's permission is dead text.
- **Closing AD text:** "The matrix's family list is the only family vocabulary; AD-12's advisory lock applies to every family flagged `HighRisk = true` in the matrix row. Stricter gates are out of V1; the register sentence is removed."
- **Disposition:** Autofix.

### A-19 — Agent version semantics and the snapshot's source for `ProviderCapabilityVersion`

- **Severity:** Low
- **Units:** Story 5.2 read model (`ConfigurationVersion` as a domain counter, shipped) vs a query client treating `expectedConfigurationVersion` as the stream revision; Story 6.1 snapshot taking the `Agent`'s selection-time `ProviderCapabilityVersion` vs the live catalog version at request time.
- **ADs obeyed:** AD-4, AD-10 ("high-water mark starts at the snapshot `ProviderCapabilityVersion`").
- **Incompatibility:** Provenance differs (selection-time vs request-time); both pass the `>=` gate because versions are monotonic, so the failure is only evidence drift and confusing `expectedConfigurationVersion` waits. Response mode has no version of its own; the snapshot stores its value, which is adequate.
- **Closing AD text (tighten AD-4):** "`ConfigurationVersion`, `InstructionsVersion`, and `ApproverPolicyVersion` are aggregate-assigned counters incremented by the events that change them, independent of stream revision. The snapshot's `ProviderCapabilityVersion` is the live catalog entry version read at request time (the AD-10 fresh read), not the `Agent`'s selection-time value; the selection-time value remains on the `Agent` for audit."
- **Disposition:** Autofix.

### A-20 — Naming drift for the version identity

- **Severity:** Low
- **Units:** Consistency `Identity` (`ProposalVersionId`) vs AD-13 / class diagram / stories (`VersionId`, `SelectedVersionId`).
- **Closing text:** Use `ProposalVersionId` everywhere; `SelectedProposalVersionId` for the approved selection.
- **Disposition:** Autofix.

---

## Contradictions Found Between ADs Or With The Companion

| Pair | Contradiction | Resolved by |
| --- | --- | --- |
| AD-2 / AD-10 | Tenant-scoped catalog aggregate vs "global catalog key" | A-2 |
| Consistency `Identity` / AD-13 | Conversations owns `MessageId` vs Agents derives it | A-17 |
| AD-17 / register | "Gate outside the minimum inventory blocks" vs "implementations may add stricter gates" | A-18 |
| AD-12 / AD-17 | Six lock families vs thirteen matrix families | A-18 |
| AD-13 / AD-20 | Fingerprint change fails closed vs retry may use a tighter policy | A-16 |
| AD-14 / AD-18 / AD-22 | Content protection and deletion inventories vs workflow history as unlisted content store | A-1 |
| IMPLEMENTATION-CONVENTIONS | No contradiction found. Its "trusted result supplied by the orchestrator" pattern is what A-3 relies on; the conventions should cite AD-28 once adopted. | — |

## Covered Attacks — No Finding

- Two owners of `Callability`: AD-10 forbids UI/host override; AD-17 makes `LaunchReadinessGate` the sole record writer. Convergent.
- Approver policy frozen vs live roles: AD-4 freezes the policy version, AD-8/AD-12 resolve membership live. Both units reach the same decision once the policy list is frozen.
- Capacity fence reuse after reclamation: closed by the 2026-08-02 addendum (`BeginInvocation` with `AdmissionFence`).
- Terminal-decision immutability under recovery: AD-23's frozen cohort and immutable pre-fault decisions are convergent.
- Browser sample shape: AD-26's discriminated kinds are convergent.

## Recommended Fix Order

1. A-1 (execution-state content boundary) — before any Epic 6 activity contract is written.
2. A-2, A-5, A-6 — before Story 5.3/5.5/5.6 acceptance; they decide catalog identity and gate keys.
3. A-3, A-4, A-8 — before Story 6.1/6.4/7.6; they decide clocks and ids.
4. A-7, A-9, A-10 — before Epic 7/8 stories enter ready-for-dev.
5. A-11 through A-17 — before their consuming stories; most are a paragraph each.
6. A-18 through A-20 — editorial pass.
