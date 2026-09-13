# Architecture Spine Rubric Review — 2026-09-12 (v3)

## Verdict

**FAIL — the update closes the authoritative report's three Critical and twelve High findings at their original scope, but the current spine still permits two incompatible retry/recovery implementations.** Repeated pre-Provider safety decisions reuse one deterministic identity, and the two-stream rolling-rate admission has neither a derived identity nor a durable commit decision.

Finding count: **0 Critical, 2 High, 3 Medium, 0 Low**.

## Review Scope And Evidence

- Target: `../ARCHITECTURE-SPINE.md` (`status: final`, `updated: 2026-09-12`, initiative altitude).
- Authoritative prior findings: `../VALIDATION-REPORT-2026-09-12.md` (3 Critical, 12 High, 4 Medium, 1 Low).
- Binding sources inspected: current PRD, external-dependency register, launch-readiness register, architecture memlog, implementation conventions, current epics/story ownership, root package/build reality, and the current spine.
- Deterministic check: `lint_spine.py` reports `ok: true`, zero findings on the frozen current tree.
- No parent architecture spine is declared or inherited; inherited-invariant conflict review is therefore not applicable.
- This review modified no spine, register, memlog, source, or planning artifact other than this review file.

## High

### H-R3-1 — Attempt-bound `SafetyDecisionId` collides when a transport retry re-evaluates current policy

**Evidence**

- AD-20 now correctly requires a fresh conjunctive snapshot-plus-current decision for **every transport retry**, and requires each individual decision to be an `AgentInteraction` event carrying `SafetyDecisionId`, policy versions, outcome, and evidence (`ARCHITECTURE-SPINE.md:306-310`).
- AD-13 preserves `AttemptId` across an authorized transport retry while allowing the then-current policy/evidence to change (`ARCHITECTURE-SPINE.md:254-260`).
- AD-29 nevertheless derives every attempt-bound decision as `SafetyDecisionId = H(safety, AttemptId, Stage)`; only approval and pre-post decisions carry a `DecisionOrdinal` (`ARCHITECTURE-SPINE.md:366-370`). A retry does not increment `AttemptId`, and its pre-Provider `Stage` is unchanged.

**Why this fails the rubric**

The C-1 policy correction requires more than one durable pre-Provider decision for the same attempt/stage whenever a transport retry occurs. AD-29 gives all of those decisions the same identity even when their current-policy version or outcome differs. One aggregate can treat the second event as a conflicting replay and remain unavailable; another can deduplicate to the first pass and authorize under stale evidence; a third can allow duplicate ids. All three can cite the literal spine.

**Impact**

The audit graph cannot unambiguously represent the current-policy check that authorizes or rejects a retry. A stale-pass deduplication can weaken the newly corrected safety guarantee; strict conflict handling instead strands every otherwise-authorized retry after the first decision.

**Action: AUTOFIX.** Extend the attempt-bound form with an aggregate-assigned decision/evaluation ordinal (or an equivalently unique durable evaluation identity) and state that exact replay of one evaluation reuses the id while every new retry evaluation increments it. Keep `AttemptId`, the prepared request, Provider idempotency key, reservation, and transport fingerprint unchanged. Reconcile the identity convention, AD-20 event rule, and retry/audit fixtures together.

### H-R3-2 — Rolling-rate admission has no durable commit authority or exact deterministic identity

**Evidence**

- AD-21 splits rolling consumption into independent per-Party and per-Conversation `RateLimitLedger(TenantId, ScopeKind, ScopeId)` streams and introduces one deterministic `RateAdmissionId`, but gives no derivation for that id (`ARCHITECTURE-SPINE.md:312-316`). AD-29's exhaustive deterministic-id paragraph derives `AdmissionId` for capacity, but never `RateAdmissionId` (`ARCHITECTURE-SPINE.md:366-370`).
- The protocol says neither partition records consumption unless both preparations exist, then permits recovery to “complete or abort” the same id. It does not name the durable commit decision or say that, after either partition commits, recovery must complete rather than abort. Committed consumption is expressly never released (`ARCHITECTURE-SPINE.md:316`).
- The implementation convention permits only one trusted command per durable step, so the two ledger prepares and commits are distinct EventStore writes, not one implicit transaction (`IMPLEMENTATION-CONVENTIONS.md:7-20`). The main sequence compresses them into `prepare/commit Party + Conversation`, while the class diagram attaches rolling consumption to `ProviderAttempt` even though AD-13 performs rate admission before the attempt descriptor is appended (`ARCHITECTURE-SPINE.md:470`, `:258`, `:845`).
- The PRD treats a regeneration as a new Agent Call for rate-limit purposes, so an identity must distinguish the original call from each regeneration while remaining stable across retry/recovery (`prd.md:81`, `:417`, `:766-769`).

**Why this fails the rubric**

The H-4 lifetime split is present, but its cross-stream admission protocol is not independently implementable. After a crash between commits, one recovery unit can abort the still-prepared side and leave an irreversible one-sided charge; another can complete both. Without an authoritative derivation, units can also key by `AgentInteractionId`, capacity `AdmissionId`, `AttemptId`, or a process-local ordinal, producing duplicate or conflated consumption for regeneration.

**Impact**

Identical Agent Calls can receive different Party/Conversation rate decisions after retry or failover. A caller or Conversation can be permanently over-counted without a Provider call, or a regeneration can reuse prior consumption and bypass a configured limit.

**Action: AUTOFIX.** Add an exact AD-29 `RateAdmissionId` derivation from a durable Agent-Call identity/ordinal that exists before ledger preparation and distinguishes regenerations. Name one durable commit-decision owner/record: before that decision recovery may abort both preparations; after it, recovery must idempotently complete both and may not abort either. State that Provider work cannot progress until both commits acknowledge, bind preparation expiry to the pre-decision state only, and update the sequence/class diagram plus crash-boundary fixtures. This is an architecture/idempotency repair, not a new Product choice.

## Medium

### M-R3-1 — AD-22 retains the superseded nine-family count

**Evidence**

AD-12's distilled rule says the authoritative lock-bearing set contains exactly ten families (`ARCHITECTURE-SPINE.md:250`). AD-22 lists the same ten, but its final digest-key sentence says rotation “is not one of AD-12's nine lock-bearing families” (`ARCHITECTURE-SPINE.md:322`).

**Impact**

The complete list and AD-12's explicit count make the intended answer recoverable, so this does not create another High. It does contradict the claim that correction layers were fully re-distilled.

**Action: AUTOFIX.** Replace “nine” with “ten”, or say “the lock-bearing set” to avoid future count drift.

### M-R3-2 — The normative blocker register still omits two blocker codes emitted by AD-17

**Evidence**

- AD-17 tells authoritative producers to emit `SuspensionReviewOverdue` and `DeferredAssumption` and says all codes fail closed as specified by the launch-readiness register (`ARCHITECTURE-SPINE.md:286-292`).
- The register declares its cross-gate vocabulary closed and unknown values fail closed, but lists and defines neither code; its emitter table omits both (`launch-readiness-register.md:38-56`, `:70-81`).
- The PRD explicitly records this as a pending register extension and requires the FR-30 surface to carry the conditions during the transition (`prd.md:728-730`). The memlog instead says the earlier gap was closed by the AD-17 edit alone (`.memlog.md:224`).

**Impact**

Fail-closed unknown-code behavior prevents an unsafe READY decision, keeping severity below High, but register, projection, API, and UI builders still lack one authoritative wire vocabulary and emitter contract.

**Action: AUTOFIX.** Extend the register's closed vocabulary and emitter table with the PRD-defined semantics, then make AD-15 required-completion parity name those registered values. Preserve the PRD's transitional history; do not claim the extension already existed.

### M-R3-3 — Architecture-owned assumptions remain unscheduled despite the PRD's literal-date rule

**Evidence**

- The PRD requires every Architecture-owned `ARCH-A` row to carry a literal target retirement date and says a milestone or missing date remains an `RQ-1` blocker (`prd.md:892-896`, `:728`).
- The spine acknowledges the rule but leaves Architecture-owned or co-owned rows `ARCH-A-1` through `ARCH-A-4`, `ARCH-A-6` through `ARCH-A-8`, `ARCH-A-11`, `ARCH-A-12`, and `ARCH-A-14` as “Unscheduled” milestone revisits (`ARCHITECTURE-SPINE.md:937-955`).

**Impact**

The explicit `UnretiredAssumption` fail-closed rule prevents premature qualification, so this is not a safety High. It does leave architecture governance out of conformance and gives owners no bounded review date.

**Action: DISCUSS.** Architecture and each named co-owner must approve literal dates or retire/replace the assumptions. Do not invent those dates in the spine; until recorded, retain the explicit `RQ-1` blocker.

## Authoritative Prior-Finding Closure Audit

| Prior finding | v3 disposition | Current evidence |
| --- | --- | --- |
| C-1 safety policy selection | **Original Critical closed; derivative High H-R3-1** | AD-20 is conjunctive for retry/regeneration/approval/pre-post and AD-13 keeps current-policy evidence outside the immutable transport fingerprint. Decision identity still collides on retry. |
| C-2 hold/deletion exclusion | **Closed** | AD-2 adds `ProtectionFence`; AD-22 binds frozen sets, prepare/arm linearization, no irreversible work before arm, recovery, receipts, and failure injection. |
| C-3 bootstrap/repair matrix | **Closed** | AD-17 and matrix v4 bind Platform/Tenant scope, exact omitted conditions, and empty/broken-state fixtures. |
| H-1 scheduled approver recheck | **Closed** | AD-8 binds owner, single flight, cadence, evidence freshness, two empty passes, marker-only `PostingFailed`, and unavailable behavior. |
| H-2 distributed safety rescan | **Closed** | AD-20 binds EventStore epoch/index state, a fenced tenant coordinator, versioned positive profile bounds, wait/fail behavior, and exact public mapping; missing bounds fail closed. |
| H-3 human-only Approvers | **Closed** | AD-8 and `EXT-PARTIES-1` require fresh active human classification at configuration and every runtime resolution. |
| H-4 ledger lifetimes | **Original High closed; derivative High H-R3-2** | The three records and lifetimes are separated. The newly exposed two-stream rate commit/idempotency protocol remains incomplete. |
| H-5 human identity | **Closed** | AD-7/AD-22/AD-30 carry and compare stable `AuthenticatedHumanActorId`; non-human Workflow cannot satisfy a human second-party rule. |
| H-6 proposal index consistency | **Closed** | AD-7 makes interaction events truth, emits a source-revision outbox, reconciles before removal completion, and requires both failure boundaries. |
| H-7 Conversations seam atomicity | **Closed** | Core seams remain in `EXT-CONV-AI-1`; optional retraction is isolated as `EXT-CONV-RETRACTION-1`. |
| H-8 trusted-envelope authentication | **Closed** | AD-30 binds canonical fields, audience/lifetime/nonce, key version, rotation/overlap/revocation, constant-time verification, and fail-closed cases; `EXT-SECRETS-1` is extended. |
| H-9 export-package lifecycle | **Closed at architecture scope** | AD-22 and `EXT-EXPORT-STORE-1` bind store, encryption, index, canonical signed manifest, trust anchor, and purge receipts. Product/Governance/Security lifecycle choices remain visibly blocked as `OD-EXPORT-LIFECYCLE-1`. |
| H-10 Dapr current reality | **Closed** | Stack and ARCH-A-15 distinguish current transitive Client/ASP.NET `1.18.5` from future Workflow and block release pending upgrade/exception. |
| H-11 “current” public parity | **Closed** | AD-15 says required completion parity, names owning stories, and lists brownfield absence as delivery debt. |
| H-12 tracker/evidence contradiction | **Correctly surfaced, not invented** | `OD-SPRINT-5.1-5.2-1` and the delivery-debt table preserve architecture authority and require the Delivery/Platform owner choice. |

## Good-Spine Checklist Disposition

| Criterion | Result | Evidence |
| --- | --- | --- |
| Fixes real divergence points at the level below | **Fail** | H-R3-1 and H-R3-2 leave retry/recovery identity and commit behavior open to incompatible implementations. |
| Every Rule enforceably prevents its stated divergence | **Fail** | AD-20 cannot uniquely record every required retry decision; AD-21 lacks the recovery linearization rule needed to prevent duplicate or one-sided rate consumption. |
| Deferred/open items are safe to defer | **Pass** | Export lifecycle, Dapr security disposition, and sprint-history reconciliation all name owners and fail-closed states. Deferred-beyond-V1 items do not authorize V1 behavior. |
| Named technology is verified-current | **Pass with explicit blocker** | Current root-authoritative Dapr exposure and package authority are accurately represented; ARCH-A-15 blocks the known superseded family. Exact pinned stack facts and the unavailable SDK test limitation are stated. |
| Brownfield ratification | **Pass with owned implementation debt** | The spine no longer claims backlog contracts/runtime as shipped and assigns rescan, ledgers, outbox, trusted principal, governance/export, Dapr, vocabulary, and legacy-test gaps as delivery debt. H-R3-1/H-R3-2 are architecture defects, not missing implementation. |
| Binding PRD capability coverage | **Fail** | The original C/H capability gaps are covered, but the PRD's retry/regeneration rate semantics cannot converge without H-R3-2; the C-1 retry policy cannot produce unambiguous evidence without H-R3-1. |
| Inherited parent constraints | **N/A** | Initiative spine; no parent spine or inherited AD set is declared. |
| Initiative structural breadth | **Pass** | Paradigm, ownership, state/mutation, APIs/UI, security/governance, integrations, deployment, recovery, capacity, evidence, provider strategy, and operational envelopes are decided, delegated to authoritative registers, or explicitly blocked. |
| Mechanical validity | **Pass** | Linter returned zero findings on the final frozen tree. |

## Gate Recommendation

Do not ratify this revision yet. Apply H-R3-1 and H-R3-2 without renumbering ADs, append their decisions to the memlog, re-distill M-R3-1/M-R3-2, and keep M-R3-3 as an explicit owner discussion unless approved literal dates are available. Then rerun the complete multi-lens Reviewer Gate; this rubric lens cannot report a clean Critical/High gate while either High remains.
