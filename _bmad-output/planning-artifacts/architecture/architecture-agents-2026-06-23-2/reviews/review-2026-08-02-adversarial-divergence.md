# Adversarial Divergence Review — 2026-08-02

## Lens

Attack the architecture spine by constructing two independently built units that obey every architecture decision literally yet can still disagree at a shared seam. A disagreement is a finding only when it can change public shape, ownership, mutation, callability, qualification, external effects, recovery, or evidence acceptance.

Reviewed authority:

- `ARCHITECTURE-SPINE.md` as updated on 2026-08-02
- `IMPLEMENTATION-CONVENTIONS.md`
- `external-dependency-register.md`
- `launch-readiness-register.md`

## Verdict

**Fail pending one critical and seven high-severity tightenings.** The spine is materially convergent on aggregate boundaries, public Provider readiness fields, gate inventories, external dependency ownership, platform hosting, and NFR-11 through NFR-14 thresholds. It still permits independently compliant implementations to disagree about which readiness observation is authoritative, when a committed-but-unavailable external dependency may participate in qualification, where a prepared Provider attempt and capacity admission are durably owned, how capability and fairness versions are scoped, and which browser/recovery evidence shape is valid. The first gap can produce a false `RQ-1 READY`; the others can produce incompatible runtime or evidence units.

## Critical Finding

### C-1 — Readiness observations have no authoritative identity, writer, or conflict-selection rule

**Location:** AD-1, AD-17, the launch-readiness diagram, and the `Mutation`/`Data planes` conventions.

**Compliant pair:**

- The gate-ingestion unit persists every observation through an EventStore command keyed by `(GateId, TenantScope, EnvironmentProfile, SourceVersion)` and considers the greatest source revision authoritative.
- The release evaluator also uses EventStore-backed state, but keys only by `(GateId, TenantScope, EnvironmentProfile)` and selects the record with the greatest `ObservedAt`, treating later ingestion as replacement.

Both units honor the exact schema, freshness inequality, EventStore-only mutation, immutable evidence references, and safe blocker rules. They disagree when a fresh `Pass` overlaps a later `Block`, when a corrected record keeps the same `SourceVersion`, or when observations arrive out of order. One evaluator can produce `READY` while the other produces `NOT READY`.

The register says corrected evidence is appended or republished and previous evidence remains auditable, but neither it nor AD-17 fixes record identity, observation revision, the sole command/write owner, allowed overlap, supersession, tie-breaking, or fail-closed precedence among concurrent records.

**Required tightening:** Amend AD-17 to bind one Agents-owned EventStore mutation seam for readiness observations; define the immutable observation identity and logical gate key; require an explicit monotonic observation/revision or supersedes reference; prohibit ambiguous overlapping authoritative records; and define deterministic selection with `Block`/`InsufficientEvidence`/`Stale` fail-closed on ambiguity. The `launch-readiness` projection must be derived from that state and must not accept direct multi-writer projection mutation.

**Disposition:** Autofix before finalization.

## High Findings

### H-1 — `Committed` is treated as executable by qualification although the dependency register says delivery may not exist

**Location:** AD-17 `QualificationExecutionGateSet`; external dependency status semantics.

**Compliant pair:**

- The qualification harness permits a controlled production-like run when every named external dependency is `Committed`, because AD-17 explicitly allows `Committed` or `Available`.
- The runtime adapter refuses the same run because the dependency register defines `Committed` as an accepted future target that “may still be in delivery,” while AD-6, AD-10, AD-11, AD-14, and AD-20 require the seam to be consumable/available before its effect.

Both units follow their governing text. The harness can start a run that the runtime must block, and teams can disagree whether the resulting negative evidence qualifies or merely proves that an unavailable artifact was invoked too early.

**Required tightening:** Separate development entry from executable qualification. `Committed` may unblock `ready-for-dev` and contract-fixture work; every controlled execution that touches an `EXT-*` seam must require that consumed record to be `Available` and its compatibility command passing. State explicitly which qualification activities may run without each unavailable seam.

**Disposition:** Autofix.

### H-2 — The operation-to-gate mapping covers Agent calls but leaves governance operations independently decidable

**Location:** AD-12 and AD-17.

**Compliant pair:**

- The governance API applies `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, and `LR-AUDIT-PROTECTION-DELETION` to `LegalHold`, `ExportRequest`, and `DeletionRequest`, adding `LR-SECRETS` for export encryption.
- The UI/BFF applies only tenant authorization and the advisory high-risk pending lock because AD-17's explicit subset clauses name call, posting, Provider preparation/invocation, and content persistence, but never bind gate subsets for `PolicyPublication`, `TenantBudgetUpdate`, `LegalHold`, `ExportRequest`, or `DeletionRequest`.

Both can claim compliance with “applicable subset.” They expose incompatible callability and blockers for the exact high-risk operation families that AD-12 makes public.

**Required tightening:** Add a normative operation-family-to-required-GateId matrix, including setup activation and all six high-risk families. Bind whether failure blocks command acceptance, only a later side effect, or only release qualification. Require API, BFF, UI, and readiness projection to consume the same matrix version.

**Disposition:** Autofix.

### H-3 — Provider capability versions and readiness reason codes lack a shared scope and vocabulary

**Location:** AD-9 and AD-10.

**Compliant pair:**

- `ProviderCatalog` increments `CapabilityVersion` globally for the tenant catalog and emits `SecretUnavailable`.
- The generation adapter increments it per `(TenantScope, ProviderId, ModelId)` and emits `ProviderSecretMissing`.

Both versions are monotonic, both reason codes are stable and support-safe, and both expose the required six Provider readiness fields. Their high-water comparisons become meaningless across scopes, and API/UI/evidence consumers cannot interpret or group reason codes consistently. A global version `42` compared with a per-model version `7` can block a healthy unchanged model forever.

The same ambiguity exists for the “no-blocker reason” on `Ready` (`null`, empty string, or a `None` code) and for which warning distinguishes `Ready` from callable `Degraded` after all hard gates pass.

**Required tightening:** Bind the `CapabilityVersion` monotonicity key—preferably the full tenant/provider/model catalog-entry identity, unless a single global catalog sequence is deliberately chosen—and forbid comparisons across keys. Define a versioned additive public `ReasonCode` vocabulary, including the exact no-blocker representation and the warning classes that may yield `Degraded`.

**Disposition:** Autofix.

### H-4 — Prepared attempt, reservation, admission, and queue state can have different durable owners and crash lifecycles

**Location:** AD-2, AD-13, AD-18, AD-21, AD-24, the runtime sequence, and `Data planes`.

**Compliant pair:**

- The AgentInteraction unit persists the prepared-attempt descriptor and budget reservation identity as business events, then expects an explicit EventStore-recorded capacity-admitted transition before Provider invocation.
- The Dapr Workflow/capacity unit stores the descriptor and capacity lease in workflow history because those are execution state, queues after cost reservation, and treats an expiring admission lease as safely reacquirable under the same attempt identity after restart.

Both can cite a “durable owner,” deterministic identity, retry reuse, EventStore business truth, and Dapr Workflow execution ownership. They disagree about whether lease expiry permits reacquisition, whether a queued attempt may hold budget indefinitely, whether a crash between admission and checkpoint leaks capacity, and which state NFR-11 recovery must inventory. This is exactly where duplicate Provider invocation or permanent capacity/budget leakage occurs.

**Required tightening:** Name the authoritative owner and mutation path for the prepared descriptor, budget reservation, queue entry, capacity admission, lease/release, and terminal reconciliation. Bind their state machine and ordering, including crash before/after each checkpoint, queue cancellation/expiry, reservation release while queued, admission lease expiry/renewal, restart reconciliation, and the one EventStore fact that must exist before Provider invocation. Workflow history may cache execution state but cannot independently redefine those identities or their terminality.

**Disposition:** Discuss the minimal state machine, then amend AD-13/21/24.

### H-5 — The fairness profile is syntactically complete but semantically non-interoperable

**Location:** AD-24 and `CapacityProfile`.

**Compliant pair:**

- The capacity scheduler interprets `TenantWeightOrMinimumServiceShare = 2` as a weight under weighted round-robin and `FairnessWindow = 60` as seconds.
- The evidence evaluator interprets the same scalar as a minimum service share percentage and the window as 60 scheduling decisions.

Both expose every mandatory field, positive numeric limits, a named fairness policy, and a no-starvation test across two tenants. They can calculate opposite pass/fail outcomes from the same trace. Queue ordering, tie-breaking, priority for retries, and the relationship between tenant weights and minimum shares are also unbound.

**Required tightening:** Make fairness a discriminated, versioned contract: `FairnessPolicyKind`, duration value plus unit, either a typed tenant-weight map or a bounded fractional minimum-service-share (not a union-shaped scalar), queue ordering/tie-break rules, retry priority, and the exact observed service-share formula. Scheduler and evaluator must reject unknown policy versions fail-closed.

**Disposition:** Autofix the contract shape; defer only the selected numeric profile values.

### H-6 — Browser evidence is drawn as one all-fields record while the register permits per-gate “relevant ticks”

**Location:** AD-26, `BrowserTimingSample`, and the launch-readiness NFR-14 contract.

**Compliant pair:**

- Browser instrumentation emits three discriminated samples: page usability contains only navigation/usable ticks, pending acknowledgement contains only submit/pending ticks, and terminal evidence contains receipt/render/live-region ticks.
- The `browser-ui-metrics` projection follows the architecture class diagram literally and requires all six tick fields on every `BrowserTimingSample`.

The register says each sample contains the “relevant monotonic ticks,” while the architecture diagram shows one unqualified record with every tick. One implementation accepts valid route-only samples; the other marks them insufficient. `LiveRegionAnnouncedTick` is also ambiguous: browser code can observe a live-region DOM mutation, not prove that assistive technology audibly announced it.

**Required tightening:** Define a `SampleKind` discriminated union with common fields and exact required/forbidden ticks per kind. Define `LiveRegionAnnouncedTick` as the monotonic tick captured after the instrumented live-region mutation is committed/observable (or rename it accordingly); do not claim direct assistive-technology observability. Bind duplicate SampleId handling and idempotent ingestion.

**Disposition:** Autofix.

### H-7 — Recovery eligibility and the 15-minute clock can be interpreted incompatibly

**Location:** AD-23 and the launch-readiness recovery contract.

**Compliant pair:**

- The workflow recovery unit re-evaluates current Provider/safety/cost gates and treats an interrupted interaction as no longer eligible, recording a safe terminal block.
- The recovery evaluator freezes eligibility at fault injection and requires that same interaction to resume; it treats the terminal block as changed behavior. A third compliant evaluator treats any new safe terminal result as success.

All preserve RPO 0, avoid duplicate effects, and preserve terminal decisions that existed before the fault. They still disagree on `LR-RECOVERY`. Separately, the injector may stamp `RecoveryStartedAt` and the verifier may stamp `RecoveryVerifiedAt` from different wall clocks; unlike NFR-14, the duration source is not bound.

**Required tightening:** Define the pre-fault interaction cohort, the exact eligibility snapshot, which current-gate changes may convert an interrupted interaction to a safe terminal result, and what counts as preserving versus changing a terminal decision. Capture both recovery duration ticks from one fixture-owned monotonic clock/origin while retaining UTC instants separately for audit.

**Disposition:** Autofix.

## Medium Findings

### M-1 — Logical projection IDs have no versioned binding authority to physical projections

**Compliant pair:** the proposal projection team maps `proposal-detail` to a new V2 store/type, while the deletion orchestrator retains an older static map and reports the logical ID purged after deleting only V1. Both name the required logical ID and obey the architecture text.

**Tightening:** Bind one versioned logical-to-physical projection registry owned by Agents contracts. Readiness, deletion, evidence, and projection deployment consume the same version; unknown or changed mappings invalidate `LR-AUDIT-PROTECTION-DELETION`.

### M-2 — Advisory high-risk session locking lacks a shared session and resource-key contract

**Compliant pair:** the browser keys a session by authentication cookie and a resource by route ID; the distributed BFF keys the session by subject/session claim and the resource by tenant-qualified aggregate ID. Both enforce one pending command per resource/family “per user session,” but users see inconsistent locks across tabs, token refresh, and BFF instances.

**Tightening:** Define the privacy-safe session key derivation, tenant-qualified resource key for each operation family, pending identity, and accepted/rejected/terminal statuses that release the advisory lock. Keep EventStore concurrency and idempotency authoritative as already stated.

## Covered Attacks — No Finding

- A Provider adapter returning `Degraded/Blocked` is forbidden by AD-10's valid pair inventory; `Degraded` remains callable only after every named hard gate passes.
- A module-owned AppHost claiming conformance is explicitly rejected by AD-16 and the Story 5.1/5.6 implementation-gap text.
- A gate implementation removing or renaming a minimum `LR-*` or projection ID is blocked by the explicit AD-17 inventories.
- A browser evaluator mixing server wall clock with browser monotonic ticks is directly forbidden by AD-26.
- A UI-only high-risk lock claiming cross-session correctness is rejected by AD-13; EventStore concurrency, deterministic identity, and idempotency remain authoritative.
- A Provider, safety, tokenizer, Conversations, host, secrets, or topology seam invented from local presence cannot pass its corresponding `EXT-*` register because all unknown targets and commands remain `Uncommitted` blockers.

## Recommended Fix Order

1. C-1 readiness observation identity, ownership, and deterministic selection.
2. H-1/H-2 qualification eligibility and complete operation-to-gate mapping.
3. H-4/H-5 prepared-attempt and capacity/fairness ownership/state contracts.
4. H-3 Provider version scope and public reason-code vocabulary.
5. H-6/H-7 browser sample variants and recovery eligibility/clock seams.
6. M-1/M-2 before deletion and distributed BFF implementation begins.

## Resolution Addendum — Delta Re-review

### Verdict

**Fail — no critical findings remain; two high findings remain.** Six of the eight prior critical/high divergences are fully resolved across the current spine and registers. Readiness observation authority, Available-only execution, Provider version/reason-code semantics, exact weighted fairness, discriminated browser samples, and frozen monotonic recovery now converge. The operation matrix and durable attempt/capacity lifecycle each retain one independently implementable clash.

### Resolution Status

| Prior finding | Result | Delta evidence |
| --- | --- | --- |
| C-1 Readiness authority | **Pass** | `LaunchReadinessGate` is the sole writer per logical key; deterministic `ObservationId`, EventStore revision ordering, newest-invalid fail-closed behavior, idempotent/conflicting duplicate handling, and one stable projection checkpoint remove record-selection ambiguity. |
| H-1 Available-only execution | **Pass** | Both registers and AD-17 now reserve `Committed` for story/contract work and require `Available` plus the passing compatibility command before any consumed seam executes. |
| H-2 Operation matrix | **Fail — High** | All six high-risk families are mapped, but Agent lifecycle/setup activation remains absent from `OperationGateMatrixVersion = 1`. |
| H-3 Provider version/reason codes | **Pass** | The global (`ProviderId`, `ModelId`) version key, non-reusable monotonic sequence, additive enum, exact valid triples, and unknown-code fail-closed rule are explicit and consistent. |
| H-4 Ordered durable attempt/capacity ownership | **Fail — High** | Owners and ordering are explicit, but `ProviderInvocationAuthorized` has no capacity fencing epoch/lease-validity condition preventing invocation after allocator reclamation. |
| H-5 Exact fairness | **Pass** | `WeightedRoundRobinV1`, integer tenant weights, persisted cursor, within-tenant ordering, complete-cycle formula, retry position, and crash/cancel/expiry evidence fix scheduler/evaluator semantics. |
| H-6 Discriminated browser samples | **Pass** | `SampleKind`, exact required/forbidden ticks, deterministic identity, attested ingress, duplicate behavior, and the observable live-region mutation seam are bound. |
| H-7 Frozen monotonic recovery | **Pass** | `RecoveryExerciseId`, frozen pre-fault cohort, one monotonic origin, current-gate terminal accounting, exclusions, and immutable pre-fault terminal decisions are bound. |

### Remaining High Findings

#### R-H-1 — Setup activation has no authoritative gate-matrix row

**Divergent pair:** The Agent lifecycle unit permits `ActivateAgent` after EventStore and tenant authorization because activation is not a named matrix operation; the setup-readiness/UI unit treats activation as callability publication and requires Party, Provider, secrets, safety, cost, capacity, topology, and audit gates. Both consume the same matrix exactly because it contains no activation family.

This leaves Story 5.7's “activate only when setup gates pass” invariant locally decidable even though every listed operation is convergent.

**Minimal tightening:** Add an immutable `AgentActivation` or `SetupActivation` row to `OperationGateMatrixVersion = 1` with the exact required setup GateIds, and state that missing/non-pass gates block the lifecycle command itself rather than only later calls.

#### R-H-2 — Capacity lease reclamation is not fenced against a previously authorized workflow

**Divergent pair:** The workflow unit sees committed `ProviderInvocationAuthorized` after restart and invokes the Provider; the capacity allocator has already reclaimed the expired lease after outcome lookup found no active invocation and admitted another attempt. Both obey AD-13/24: the first relies on the durable authorization fact, while the second follows the allowed reclamation rule.

The resulting late invocation can exceed tenant/system concurrency even though every identity is deterministic and the Provider request remains idempotent.

**Minimal tightening:** Bind a monotonic capacity fencing token/lease epoch and lease validity to `ProviderInvocationAuthorized`. Immediately before Provider dispatch, the workflow must atomically claim/validate that exact lease epoch as invocation-active; a reclaimed, expired, or mismatched epoch appends a durable blocked/expired result and permits no call. Reauthorization must occur at a new EventStore revision after reacquisition, never by reusing stale authorization.

### Final Closure

**Pass — all eight prior critical/high adversarial divergences are resolved; no critical or high findings remain from this lens.** `OperationGateMatrixVersion = 1` now binds `ProviderCatalogMutation`, `AgentSetupMutation`, and `AgentActivation` to exact fail-closed GateId sets, eliminating locally invented setup/activation readiness. Capacity admission now returns a monotonic `AdmissionFence` bound into `ProviderInvocationAuthorized`; linearizable `BeginInvocation(AttemptId, AdmissionId, AdmissionFence)` must transition the current unexpired lease from `Admitted` to `InvocationActive` immediately before Provider transport, while reclaimed/stale fences record `CapacityBlocked` and permit no call. These rules close R-H-1 and R-H-2 and supersede the preceding delta-fail verdict.
