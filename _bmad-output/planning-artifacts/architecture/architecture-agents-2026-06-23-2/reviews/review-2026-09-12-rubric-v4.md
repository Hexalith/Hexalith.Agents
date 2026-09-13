# Architecture Spine Rubric Review — 2026-09-12 (v4)

## Verdict

**FAIL — the revised spine closes the authoritative report's original Critical and High findings at their stated scope, and it closes both derivative High findings from rubric v3, but the complete current-tree walk exposes one control-plane bootstrap deadlock and two further cross-unit contract conflicts.** The readiness registry cannot create or repair its own `LR-EVENTSTORE` observation under matrix v4; the emergency kill-switch pull can be blocked by the conditions it must contain; and AD-15 contradicts the bound PRD's public `AgentInteractionStatus` shape.

Finding count: **1 Critical, 2 High, 3 Medium, 0 Low**.

## Review Scope And Evidence

- Target: `../ARCHITECTURE-SPINE.md` (`status: final`, `updated: 2026-09-12`, initiative altitude).
- Authoritative prior findings: `../VALIDATION-REPORT-2026-09-12.md` (3 Critical, 12 High, 4 Medium, 1 Low).
- Binding sources inspected: the current bound PRD, external-dependency register, launch-readiness register, architecture memlog, implementation conventions, current epics/story ownership, focused contracts/code, and the current spine.
- Frozen-input hashes: spine `fbbf7a6cc33a40ad08735c6025ab2bc1d0aea21eec459168fa25caf9ad9cf82d`; validation report `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`; PRD `c24f560346598799fc759a29882d7e4d63e7306204f53a4f0ed6f8a10177d2c1`; external register `86c0b1eaada959610efe763e35619c6aa941bf2e54717037db44115c30d044e0`; launch register `20c8cd3f4e93cf1ca1375725ee2fcbca0832f7fe5b5fd6b3c137c1fe37d2c792`.
- Deterministic check: `lint_spine.py` reports `ok: true`, zero findings on that frozen tree.
- No parent architecture spine is declared or inherited; inherited-invariant conflict review is not applicable.
- This review changes no architecture, PRD, register, memlog, code, or planning artifact other than this review file.

## Critical

### C-R4-1 — `ReadinessObservation` is gated by the `LR-EVENTSTORE` record it must bootstrap or repair

**Evidence**

- AD-17 makes `LaunchReadinessGate` the sole record writer, says evidence producers submit trusted observations, and requires every controlled execution to use `OperationGateMatrixVersion 4`; missing records and stale or blocking newest observations fail closed (`ARCHITECTURE-SPINE.md:286-292`).
- The register makes `ReadinessObservation` the operation family for every readiness-evidence submission and requires `LR-EVENTSTORE` for it (`launch-readiness-register.md:160`, `:184`).
- Matrix v4 leaves every unlisted family at its v3 set and gives `ReadinessObservation` only a dynamic target scope, not a bootstrap/repair variant or direct preconditions (`launch-readiness-register.md:188-207`).
- The same register says a missing gate record is an implicit block and a stale or blocking newest observation cannot fall back to an older pass (`launch-readiness-register.md:58-69`, `:100-104`). No seed, bypass, or second writer exists.

**Why this fails the rubric**

In an empty registry, recording the first `LR-EVENTSTORE` observation requires an already-passing `LR-EVENTSTORE` observation. After an established record becomes stale or blocking, recording its repair requires that same record to pass. A strict unit therefore can neither initialize nor recover the readiness control plane; a permissive unit must invent a bypass forbidden by AD-17's single-matrix and sole-writer rules.

**Impact**

No conforming implementation can reach a stable readiness state from an empty store or recover EventStore readiness after invalidation. Because every other observation also requires `LR-EVENTSTORE`, this can strand all gate evidence and make `RQ-1` permanently infeasible.

**Action: AUTOFIX ARCHITECTURE.** Add an immutable successor matrix version with an explicit `ReadinessObservation:RecordOrRepair` variant that does not consume the `LR-EVENTSTORE` readiness record. Bind a closed direct-precondition set for producer authorization, target GateId/scope/profile validity, evidence-schema/manifest validity, expected gate revision, and the live EventStore append/projection acknowledgement needed by the observation. Add empty-registry, stale-`LR-EVENTSTORE`, blocking-`LR-EVENTSTORE`, lost-ack, and unrelated-invalid-input fixtures. This is a control-plane architecture correction, not a Product decision and not implementation debt.

## High

### H-R4-1 — The emergency kill-switch pull can be blocked by the readiness failures it must contain

**Evidence**

- The bound PRD requires the Platform Operator to pull the per-tenant kill switch immediately after confirming a cross-tenant or unauthorized action; release has separate containment/review evidence (`prd.md:742-745`). AD-12 binds the same immediate containment rule (`ARCHITECTURE-SPINE.md:244-250`).
- Matrix v4 does not distinguish pull from release. `TenantKillSwitch` therefore inherits the v2 set `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, and `LR-AUDIT-PROTECTION-DELETION` (`launch-readiness-register.md:183`, `:205`).
- Missing, stale, unavailable, or blocking gate state rejects the operation, and AD-17 forbids code-local exceptions (`ARCHITECTURE-SPINE.md:290`; `launch-readiness-register.md:58-69`, `:188-207`).

**Why this fails the rubric**

Two literal units diverge at the containment boundary. A strict evaluator rejects the pull when tenant-access or audit evidence is blocking—even when that failure is the reason containment is required. A second evaluator special-cases pull so it can satisfy the PRD, thereby violating the normative matrix. Treating pull and release as one variant also lets implementations apply the release prerequisites to containment or weaken release to match containment.

**Impact**

A confirmed cross-tenant or unauthorized action can remain unsuspended because the defensive command is unavailable. That defeats the product's immediate containment control and makes recovery behavior depend on a private exception.

**Action: AUTOFIX ARCHITECTURE.** In the successor matrix version, split `TenantKillSwitch:Pull` from `TenantKillSwitch:Release`. The pull path must not depend on a gate whose failure it is intended to contain; bind the exact authorized trigger evidence, target tenant, safe expected-revision rule, and durable audit/change evidence as direct preconditions. Keep release gated by the PRD's recorded containment or review decision and the applicable restored readiness controls. Add fixtures proving a pull succeeds under each failed/stale containment-related gate while unauthorized, malformed, wrong-tenant, and stale-revision pulls fail closed. No Product choice is required.

### H-R4-2 — AD-15 prescribes an `AgentInteractionStatus` wire shape that the bound PRD expressly rejects

**Evidence**

- AD-15 says `AgentInteractionStatus` grows with `SafetyBlocked`, `BudgetBlocked`, `RateLimited`, `CapacityQueued`, `CapacityRejected`, `UnknownOutcome`, `PostingPending`, `Posted`, and `PostingFailed` (`ARCHITECTURE-SPINE.md:270-278`).
- The bound PRD defines the accepted call-status members, says every non-context pre-Provider rejection ends `Blocked` with a typed reason, keeps proposal/posting state in `ProposedAgentReplyState`, and states that UI-side call statuses are presentation projections rather than contract members (`prd.md:299-302`, `:433-463`, `:590`).
- Current repository reality already has `SafetyFailed`, `Posted`, and `PostingFailed` in `AgentInteractionStatus`; it does not have the six new UX outcome names or automatic `PostingPending` there (`src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionStatus.cs:18-120`). The UX source separately labels the six values as future UI status vocabulary, which does not override the bound PRD's contract-shape decision (`EXPERIENCE.md:678-690`, `:1018-1021`).

**Why this fails the rubric**

The contracts team can follow AD-15 and add new enum members, while the aggregate/API team follows the PRD and emits `Blocked` plus reason codes and derives presentation statuses. Both choices are literal but wire-incompatible. AD-15 also calls already-shipped `Posted`/`PostingFailed` growth, so its brownfield classification is inaccurate.

**Impact**

API, BFF, UI, metrics, and compatibility tests can compile against different public status grammars, recreate the proposal-state duplication the PRD retires, and disagree about terminality and unknown-value handling.

**Action: AUTOFIX ARCHITECTURE.** Re-distill AD-15 to the PRD's accepted `AgentInteractionStatus` and typed-reason model. Put UX-only `SafetyBlocked`, `BudgetBlocked`, `RateLimited`, `CapacityQueued`, `CapacityRejected`, `UnknownOutcome`, and automatic posting progress in one explicitly named presentation/read-model discriminant derived from authoritative interaction, attempt, capacity, and posting-record facts; do not add them to the domain wire enum. Mark existing `Posted` and `PostingFailed` as current brownfield facts, and keep removal of deprecated proposal-duplicating members as implementation debt under the PRD register. This is an architecture/PRD reconciliation, not a new Product decision.

## Medium

### M-R4-1 — `PrincipalIdentity` is not a closed canonical component of `LogicalCommandId`

**Evidence**

- AD-29 derives `LogicalCommandId` with one abstract `PrincipalIdentity` component, then describes API idempotency using potentially multiple fields: kind, `AuthenticatedHumanActorId`, `PartyId` when present, or Workflow instance/activity identity (`ARCHITECTURE-SPINE.md:366-370`).
- AD-30 defines four principal shapes and authenticates the concrete actor, Party, command, target, logical id, and nonce, but does not define the tagged byte/component expansion of AD-29's singular `PrincipalIdentity` (`ARCHITECTURE-SPINE.md:372-378`).

**Impact**

The common canonicalizer and complete MAC inventory limit this below High, but issuer, redispatch, and aggregate code can still choose actor-only, Party-only, actor-plus-Party, or differently framed Workflow components and derive different logical ids for the same command.

**Action: AUTOFIX.** Replace `PrincipalIdentity` with a closed, tagged per-kind component grammar and state that it is exactly the principal portion of the API idempotency tuple. Include fixtures for User actor-plus-Party, Administrator, Platform, Workflow instance/activity, actor turnover, and wrong-kind collisions.

### M-R4-2 — Architecture-owned assumptions still lack the PRD-required literal retirement dates

**Evidence**

- The PRD requires every Architecture-owned `ARCH-A` row to carry a literal calendar date and says a missing/milestone date independently blocks `RQ-1` (`prd.md:728`, `:892-896`).
- Architecture-owned or co-owned `ARCH-A-1` through `ARCH-A-4`, `ARCH-A-6` through `ARCH-A-8`, `ARCH-A-11`, `ARCH-A-12`, and `ARCH-A-14` remain `Unscheduled` with milestone-only revisits (`ARCHITECTURE-SPINE.md:949-969`).

**Impact**

The explicit `UnretiredAssumption` rule safely blocks qualification, so this is not a safety High. It remains a governance nonconformance and gives owners no bounded review date.

**Action: DISCUSS.** Architecture and each named co-owner must approve literal dates or retire/replace the assumptions. Do not invent dates. Retain the blocker until approved dates or retirement evidence exist.

### M-R4-3 — `PostingPending` still permits implementation-specific timeout duration and authority

**Evidence**

- AD-5 permits any stored deadline no shorter than the Conversations posting timeout and expressly fixes no duration or configuration authority (`ARCHITECTURE-SPINE.md:174-178`).
- `ARCH-A-14` confirms that neither the PRD nor spine selects the duration and schedules only a milestone revisit (`ARCHITECTURE-SPINE.md:968`). The PRD requires a stored deadline but likewise supplies only the lower bound (`prd.md:438-451`).

**Impact**

The deadline is durable once created and all later consumers share it, keeping this below High, but independent ingress/orchestration implementations can choose different creation values and change when lookup, `LateConfirmed`, and `PostingFailed` occur.

**Action: DISCUSS OR HARD-BLOCK THE STORY.** Architecture should propose one duration/configuration authority, snapshot rule, allowed range, and compatibility relationship with `EXT-CONV-AI-1`; Product must confirm where required. Until then, retain the explicit pre-story and `RQ-1` blocker rather than letting an implementation choose.

## Low

None.

## Authoritative Prior-Finding Closure Audit

| Prior finding | v4 disposition | Current evidence |
| --- | --- | --- |
| C-1 safety policy selection | **Closed** | AD-20 requires snapshot-plus-current conjunctive decisions for retry/regeneration/approval/pre-post; AD-13 keeps mutable current-policy evidence outside the immutable transport fingerprint; AD-29 now gives each evaluation a unique aggregate ordinal. |
| C-2 hold/deletion exclusion | **Closed at architecture scope, policy remains visibly blocked** | AD-2/AD-22 bind a tenant `ProtectionFence`, frozen sets, prepare/arm, and no destruction before the approved decision. `OD-HOLD-DELETION-PRECEDENCE-1` prevents irreversible work and `RQ-1` until Product/Governance/Security choose the late-hold policy. |
| C-3 bootstrap/repair matrix | **Original cases closed; derivative Critical C-R4-1** | Matrix v4 fixes catalog, policy scope, `ProvisionHexa`, budget, and tenant Provider enable/disable bootstrap. It still self-gates `ReadinessObservation` on `LR-EVENTSTORE`. |
| H-1 scheduled approver recheck | **Closed** | AD-8 binds owner, single flight, cadence, freshness, two-empty-pass behavior, marker-only posting failure, and unavailable evidence. |
| H-2 distributed safety rescan | **Closed** | AD-20 binds EventStore epoch/index truth, fenced coordination, environment bounds, exact wait/fail behavior, and `RescanPending`. |
| H-3 human-only Approvers | **Closed** | AD-8 and `EXT-PARTIES-1` require fresh active-human classification and historical actor bindings. |
| H-4 ledger lifetimes | **Closed, including rubric-v3 H-R3-2** | AD-21 separates rolling consumption, caller concurrency, and money; it now binds `RateAdmissionId`, two prepares, mutually exclusive interaction-owned commit/abort decisions, non-expiring preparations, and recovery to one decision. |
| H-5 human identity | **Closed** | AD-7/AD-22/AD-30 carry and compare stable `AuthenticatedHumanActorId`; Workflow is explicitly non-human. |
| H-6 proposal index consistency | **Closed** | Interaction events are truth; source-revision outbox application and reconciliation precede removal completion. |
| H-7 Conversations seam atomicity | **Closed** | The six core seams remain in `EXT-CONV-AI-1`; optional retraction is isolated in `EXT-CONV-RETRACTION-1`. |
| H-8 trusted-envelope authentication | **Closed at original scope** | AD-30 binds canonical authenticated fields, logical command vs delivery nonce, lifetime/skew, rotation/overlap/revocation, replay retention, and fail-closed verification; M-R4-1 is a narrower deterministic-component clarity issue. |
| H-9 export-package lifecycle | **Closed at architecture scope** | AD-22 and `EXT-EXPORT-STORE-1` bind owner, encryption, index, signed canonical manifest, trust anchor, hold/deletion purge receipts, and fail-closed availability. Product-owned lifetime choices remain `OD-EXPORT-LIFECYCLE-1`. |
| H-10 Dapr current reality | **Closed** | Stack and ARCH-A-15 distinguish root-authoritative transitive `1.18.5` from a dirty `1.18.7` checkout and block adoption/release until upgrade or exception. |
| H-11 current public parity | **Original current-vs-debt distinction closed; derivative High H-R4-2** | The delivery-debt table no longer claims future contracts are shipped, but AD-15 now conflicts with the PRD over the future public status grammar. |
| H-12 tracker/evidence contradiction | **Correctly surfaced, not invented** | `OD-SPRINT-5.1-5.2-1` preserves dependency authority and leaves the Delivery/Platform choice outside the architecture contract. |
| Rubric-v3 H-R3-1 safety-decision identity | **Closed** | AD-20 carries the evaluation ordinal and AD-29 derives ids from it, reusing only exact evaluation replay. |
| Validation M-1 status count | **Closed** | The PRD states no member count and the spine no longer asserts a conflicting total. |
| Validation M-2 mirror refusal | **Closed** | AD-7 binds current-mirror supersession, direction/outcome/attempt, and late completion/refusal behavior. |
| Validation M-3 posting timeout | **Remains Medium M-R4-3** | The ambiguity is explicit and fail-closed for qualification, but still needs a pre-story owner decision. |
| Validation M-4 legacy tests | **Implementation debt, correctly classified** | The delivery-debt table names legacy tests and bUnit without weakening the architecture. |
| Validation L-1 package verification caveat | **Closed/explicit** | The stack states the tested and untested package facts and retains Dapr as a blocker rather than a verified future claim. |

## Good-Spine Checklist Disposition

| Criterion | Result | Evidence |
| --- | --- | --- |
| Fixes real divergence points at the level below | **Fail** | C-R4-1, H-R4-1, and H-R4-2 permit strict and permissive control-plane or wire-contract implementations. |
| Every Rule enforceably prevents its stated divergence | **Fail** | AD-17 cannot initialize/repair its authority; the matrix defeats AD-12 containment; AD-15 creates the contract split it says it prevents. |
| Deferred/open items are safe to defer | **Pass with Medium governance debt** | Hold/deletion and export choices have versioned owners and irreversible work fails closed. Dapr and sprint decisions retain explicit blockers. M-R4-2/M-R4-3 need owners/dates but do not authorize unsafe launch behavior. |
| Named technology is verified-current | **Pass with explicit blocker** | Root-authoritative package/gitlink reality is distinguished from dirty checkouts and future adoption; exact pins and known verification limits are recorded. |
| Brownfield ratification | **Pass except H-R4-2's contract shape** | The delivery-debt table correctly separates absent rescan, ledgers, outbox, trusted principal, governance/export, Dapr, vocabulary, and legacy-test work from architecture. H-R4-2 is an architecture/PRD conflict, not missing implementation. |
| Binding PRD capability coverage | **Fail** | Capability breadth is present, but immediate containment and the public call-status contract do not conform to the PRD until both High findings are corrected. |
| Inherited parent constraints | **N/A** | Initiative spine; no parent spine or inherited AD set is declared. |
| Initiative structural breadth | **Pass** | Paradigm, ownership, state/mutation, APIs/UI, security/governance, integrations, deployment, recovery, capacity, evidence, provider strategy, and operational envelopes are decided, delegated to authoritative registers, or safely blocked. |
| Mechanical validity | **Pass** | Linter returned zero findings against the frozen input hashes. |

## Gate Recommendation

Do not ratify this revision. Correct C-R4-1 first, then H-R4-1 and H-R4-2, without renumbering ADs; append the architecture decisions to the memlog and re-distill the spine/register. Keep the three Medium items explicit unless their owners approve exact values. Then rerun the complete multi-lens Reviewer Gate against a newly frozen tree; this rubric lens cannot report a clean Critical/High gate while these findings remain.
