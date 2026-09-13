---
name: Hexalith Agents adversarial-divergence review v17
type: architecture-review
lens: adversarial-divergence
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
verdict: fail
critical: 1
high: 1
medium: 0
low: 0
lint_ok: true
---

# Adversarial Divergence Review — v17

## Verdict

**FAIL — 1 Critical, 1 High, 0 Medium, and 0 Low findings.** The generic Provider-lease branch, unleased phase authorizations, human exact-Conversation blocker, operator-nonterminal blocker, and expanded migration-repair cohort close their v16 defects as stated. The admission-fence correction is not yet a closed irreversible protocol: `DestructionStarted` still uses a read-then-append zero-violation check, and the prescribed higher-ordinal recut has neither a legal owner-cut transition nor an unambiguous per-ordinal zero ledger. The deterministic architecture linter passed with `ok: true` and zero findings.

## Frozen Snapshot And Method

The following inputs were SHA-256 checked before review, after the two non-semantic `epics.md` Result-line clarifications, immediately before report creation, and after this report was written:

- `ARCHITECTURE-SPINE.md`: `f834ae4e5d6c82b5581c55e4fa6ac06832cc758e6ff3218a227aaba0b631cd34`
- `IMPLEMENTATION-CONVENTIONS.md`: `accc861511863f03bf27f447d2bbc9d62862b793c954b3db98b64ef496931f76`
- architecture `.memlog.md`: `5cb60e74516a579cd131a48f9c63df11fdcc10cd278ab34899d41e4482c6b09b`
- bound `prd.md`: `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb`
- final `epics.md`: `6f9726d366d5957ea9180c9d4d59ecf47cc1cb8a8f13bb35625a49ebf46380e1`
- `external-dependency-register.md`: `aecbcdfa2538af9c62f70c4cb5a32690ea5811639671f681859e3203700d8788`
- `launch-readiness-register.md`: `31f937e7f1166a71d5e9180cfad570787cf57f315c65dea41ed9bd9ef21e0fbe`
- authoritative `VALIDATION-REPORT-2026-09-12.md`: `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`

The review constructed independent public-command, Interaction Workflow, directory/effect-authorization, rate/open/Budget/capacity, Provider-decision, migration/repair, EventStore-guard, protection-fence, both deletion-origin, and crash-recovery units. Each obeyed the literal spine, conventions, bound PRD, epics, dependency register, and operation matrix. The units were raced around every authorization/effect/result boundary, clean-checkpoint observation, expected-revision append, accepted guard violation, candidate/token invalidation, higher-ordinal recut, Provider decision activation/outage, deletion origin, migration epoch, crash/lost acknowledgement, restore, and cross-tenant input. Every authoritative Critical/High and v16 finding was rechecked before seeking new divergence.

Repository inspection was read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`; no submodule was initialized, updated, or mutated. Root gitlinks remain Builds `a32cb422749352cce8dec948aa3e78c8f00eb4cf`, Conversations `73bcee6f04479d4743d5a65ce929728e22687d7d`, EventStore `ce9e779a3ec24d9b9a054afd0ceaa3c05b8adaaa`, FrontComposer `053b2008307d4e476c0d4329e6c47763c301d43e`, Memories `3644ef63da87b8c7ec9e17726fae7f534e642a1c`, Parties `fa42398552fba1c80eb2760791517659d6d1313a`, and Tenants `2fac18396ff11a4459de053b3ebb7ddfe7c13e30`. The first five checked-out revisions differ from those root gitlinks; Parties and Tenants match. Dirty checked-out revisions remain implementation evidence only.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 1 |
| Medium | 0 |
| Low | 0 |

## Critical

### C-v17-1 — A clean admission-ledger read is not linearized with `DestructionStarted`, leaving a last-moment irreversible TOCTOU window

**Classification:** target-architecture irreversible-deletion, cross-owner linearization, and guard-integrity defect.

**Evidence.** The corrected contract requires candidate, acceptance, content-fence authorization, prepare, and destruction to “directly verify” that the append-only admission-violation ledger still equals the zero checkpoint bound to the global cut (`ARCHITECTURE-SPINE.md:215-217,268-272`; `IMPLEMENTATION-CONVENTIONS.md:23`; `launch-readiness-register.md:304-315,325`). `DestructionStarted` is then appended on the tenant `ProtectionFence` at its expected revision, while the violation ledger is exposed by the external EventStore guard (`ARCHITECTURE-SPINE.md:199,217,452,458`; `external-dependency-register.md:129`). The matrix requires both facts but specifies no compare-and-append, reservation, or authorize/effect/result seam that makes the clean guard checkpoint and the `ProtectionFence` append one linearization. The spine expressly rejects assumed multi-stream transactions (`ARCHITECTURE-SPINE.md:452`). “Immediately before” is ordering prose, not concurrency exclusion.

**Two literal units.** Team A adds an undocumented guard reservation: it freezes admission-ledger advancement while `ProtectionFence` appends `DestructionStarted`. Team B performs the literal direct read, receives a clean checkpoint, then appends `DestructionStarted` at the still-current fence revision. Between those operations a stale/restored or defective guard accepts a matching `CommitConversationEffect` or phase authorization and appends its violation. Team B's start event still phase-pins the earlier clean checkpoint. The later discovery is classified as post-start `DeletionIntegrityCompromised`, but the start event already authorizes irreversible DEK destruction and the newly committed effect may already authorize an external read/effect. Blocking later completion cannot restore a destroyed key or recall the external effect.

**Impact.** Both deletion origins can cross the irreversible boundary from a proof that was already stale when the start decision committed. This preserves the exact data-loss/external-effect race that the separate admission ledger was intended to close, so it is Critical.

**Required correction — architecture mechanics, not a Product choice.** Introduce a closed authorization/effect/result boundary owned across the two facts. One safe shape is: `ProtectionFence` first records a target-limited `DeletionDestructionStartCheckAuthorized`; the EventStore guard atomically compares the exact admission-fence ordinal/ledger high-water, installs a start barrier or returns conflict, and emits an authenticated one-shot result; `ProtectionFence` records that exact result and may append `DestructionStarted` only from it. The start barrier must ensure that any later accepted admission violation is classified before irreversible work and makes the result unusable; crash/lost acknowledgement resolves by exact guard lookup. Another implementable shape is acceptable, but the contract must name the single linearization owner, compare token/revision, expiry or non-expiry rule, cancellation/recovery, and the exact point that first authorizes key destruction. Add before/at/after tests against both fence and ledger owners; a direct read followed by an unrelated append is forbidden.

## High

### H-v17-1 — The higher-ordinal admission recut cannot legally re-close an `Effective` owner, and “zero violations” is undefined after an append-only prior violation

**Classification:** target-architecture recovery state-machine and evidence-identity defect.

**Evidence.** A pre-destruction admission violation must preserve every existing fence/cut, invalidate the global cut/candidate/token, install a higher-ordinal admission fence, and run a complete owner Closing/Effective/global-cut cycle that includes the violator (`ARCHITECTURE-SPINE.md:217,264-272,539`; `IMPLEMENTATION-CONVENTIONS.md:23`; `epics.md:3009-3012`). The owner cut identity is only (`DeletionRequestId`, `ScopePredicateDigest`) and its states are Closing then Effective; no cycle/ordinal is part of that owner identity (`ARCHITECTURE-SPINE.md:209`). The only matrix entry that installs Closing accepts `BarrierOpenOrExistingRequestScopedClosingExact`; it neither accepts an existing `Effective` cut nor defines a successor cut instance or an `Effective -> Closing` transition (`launch-readiness-register.md:293`). Yet the violation normally occurs after the first global cut, when every owner is already Effective. Separately, the admission ledger is append-only, but the successor global cut again requires `AuthenticatedCompleteAdmissionViolationLedgerCheckpointHasZeroAcceptedViolations` without saying whether this means all-time/request-wide zero or zero for the new fence ordinal (`ARCHITECTURE-SPINE.md:217`; `external-dependency-register.md:129`; `launch-readiness-register.md:304`).

**Two literal units.** Team A keys an undocumented new owner cut by `(DeletionRequestId, ScopePredicateDigest, FenceOrdinal)` and treats the violation ledger as per-ordinal, so it can recut and later prove the successor ordinal clean. Team B preserves the literal request-scoped `Effective` cut and the one append-only request ledger; `InstallBarrierClosing` rejects and the earlier accepted violation means the ledger can never again be “zero.” A third team mutates `Effective` back to Closing in place, losing the immutable provenance that the old global-cut receipt cites. All are plausible readings of “higher-ordinal complete recut,” but only the first makes progress and it invents two state identities absent from the contract.

**Impact.** The safe pre-destruction recovery branch is permanently wedged or loses auditable cut identity after exactly the guard-integrity event it is required to repair. Destruction remains blocked, so this is High rather than Critical, but two downstream teams cannot implement compatible state or evidence.

**Required correction.** Define an immutable per-cycle owner-cut identity that includes the admission-fence ordinal, or add an explicit `SupersededByAdmissionViolation` transition followed by a separately keyed successor Closing. The successor manifest must include every prior still-restrictive obligation plus the violating winner and its outcomes. Define the violation ledger/checkpoint key and predicate exactly: prior ordinals retain their accepted violations forever, while only the current successor ordinal may prove `ZeroAcceptedViolationsSinceInstall` at a named high-water. Update `InstallBarrierClosing`, Effective/global-cut receipts, migration preservation, operator-abort cut release, exact lookup, and failure-injection fixtures to carry the ordinal/cycle. Do not erase or relabel the old violation.

## v16 Finding Recheck

| v16 finding | v17 disposition |
| --- | --- |
| C-v16-1 accepted admission-fence violation lacked safe disposition | **Partially closed.** The package now has separate admission/content ledgers, zero-checkpoint gates at every reversible phase, pre-start invalidation/recut, and post-start integrity-compromised blocking. C-v17-1 is the remaining clean-read/start-append linearization gap; H-v17-1 is the missing concrete recut state/evidence identity. |
| H-v16-1 Provider decision pin contradicted precommit rules | **Closed.** The Provider lease commits generically before the reserved-system decision read; exactly one expected-revision `ProviderInvocationAuthorized` or content-free `ProviderInvocationNotAuthorized` branch follows. Only the positive branch carries the mapping pin and may begin Provider work; the negative branch records exact Budget/capacity release decisions and settles the committed lease (`ARCHITECTURE-SPINE.md:360-364`; `IMPLEMENTATION-CONVENTIONS.md:19`; `launch-readiness-register.md:265-269`). |
| M-v16-1 operator deletion lacked a legal nonterminal disposition | **Closed without inventing Product behavior.** `OD-OPERATOR-DELETION-NONTERMINAL-DISPOSITION-1` owns the status/reason/audit/metric choice; the affected owner remains restrictive Closing and cannot reach Effective while it is Open. Conversation-approved source deletion retains its existing transition (`ARCHITECTURE-SPINE.md:205,266,532-537,1235`; `launch-readiness-register.md:98,294`). |

## Authoritative Validation And Prior C/H Recheck

The authoritative report's C-1 through C-3 and H-1 through H-12, plus the v15 and earlier reviewer Critical/High seams, were rechecked. They remain closed as stated: safety is conjunctive; protection operations share the tenant fence; matrix-v4 bootstrap and recorder scope are target-aware; scheduled Approver resolution is leased; safety rescans have finite authoritative enumeration; stable human identity is principal-kind tagged; rate/open/Budget lifetimes have separate durable owners; proposal/User-action/source ingress is crash-consistent; Conversations core/retraction dependencies are split; trusted replay and denial recording are non-recursive and ACL-confined; Dapr exposure is current delivery debt; both deletion origins share canonical scope and the common cut; migration repair has an EventStore boundary; and current code is not described as the target implementation. No earlier finding is silently waived by an Open Decision or dirty checkout.

## Areas That Converge

- `RateAdmissionAuthorized`/`OpenInteractionLeaseAuthorized`, `BudgetReservationAuthorized`, and `CapacityAdmissionAuthorized` are interaction-owned, exact-revision capabilities. Deletion and repair guards block new authorizations, and every pre-boundary winner enters the owner/repair manifest with result, disposition, settlement, and acknowledgement.
- Generic Provider lease commit now precedes the decision read. The immutable authorized/not-authorized branch is expected-revision serialized; no open or unavailable decision authorizes Provider work, and a recorded branch never re-enters a successor union.
- `OD-HUMAN-EXACT-CONVERSATION-SCOPE-1` blocks only human hold/export/operator-deletion exact-Conversation variants while exact interactions and PRD-fixed source deletion remain evaluable. `OD-OPERATOR-DELETION-NONTERMINAL-DISPOSITION-1` blocks only the exact operator nonterminal convergence branch. Neither Product outcome is invented.
- Migration repair freezes the directory plus all unleased phase-authorization high-waters after the atomic repair fence, bridges only frozen work, preserves deletion fences, and revokes old authority on successor activation.
- Hold/export/deletion precedence, export commit/key delivery, replay/security recording, decision catalog/bootstrap, safety epochs, ledger reset/settlement, canonical tenant scope, and architecture-versus-delivery-debt wording remain convergent under their stated preconditions.
- AD-1 through AD-31 and all existing/new Open Decision ids are preserved. The review selects no unresolved Product, Governance, Security, dependency, or delivery outcome.

## Architecture Defects Versus Implementation Debt

C-v17-1 and H-v17-1 are target-architecture handoff defects. They define the irreversible linearization and recovery state identity already required by the chosen separate-ledger recut design; neither requires a Product choice.

The repository remains materially behind the target. No shipped directory/effect-authorization namespace, phase-authorization set, generic Provider branch, migration repair bridge, canonical two-origin deletion cut, separate fence ledgers/recut, decision catalog, three-ledger protocol, safety epoch, export store, or Conversations deletion feed exists. `EXT-HOST-1`, `EXT-CONV-AI-1`, `EXT-PARTIES-1`, protection, secrets, Provider, and export targets remain `Uncommitted`/TBD as recorded. Current output-safety mapping, approval/posting order, and checked-out submodule revisions are delivery reality, not architecture authority. These facts neither cause nor excuse the two target-contract defects.

## Gate Result

The v17 adversarial-divergence gate is **FAIL** because Critical and High counts are nonzero. Deterministic lint: **PASS**, `ok: true`, zero findings.
