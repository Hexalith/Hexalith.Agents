---
name: Hexalith Agents good-spine rubric review v18
type: architecture-spine-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
intent: review-read-only
lens: bmad-architecture good-spine rubric walker
verdict: fail
critical: 1
high: 0
medium: 3
low: 2
lint_ok: true
---

# Good-Spine Rubric Reviewer Gate — v18

## Verdict

**FAIL — 1 Critical, 0 High, 3 Medium, 2 Low.** The v18 candidate closes both requested v17 admission-cut defects: owner cut cycles and zero-violation proofs are immutable and ordinal-keyed, and the EventStore guard now atomically serializes `DestructionSealed` with every matching admission append. A fresh whole-spine walk nevertheless finds one Critical adjacent race: destruction authorization is recorded on `ProtectionFence`, but the later guard commit that actually becomes `DestructionStarted` is not serialized with a legal-hold contention or a successor `ProtectionFence` revision. A hold can therefore arrive inside that interval while the guard consumes stale no-hold authorization and releases irreversible DEK-destruction authority.

## Frozen Inputs And Method

I read the current architecture spine, implementation conventions, architecture memlog, authoritative validation report, bound PRD, epics, external-dependency register, launch-readiness register, repository instructions, reviewer-gate rubric, declared local sources, and focused repository contracts/current package and submodule state. I re-walked the complete good-spine checklist: downward divergence, enforceable AD rules, owner/revision authority, effect ordering, recovery and lost acknowledgements, security/data-loss boundaries, PRD and epic traceability, unresolved Product decisions, current brownfield truth, architecture-versus-delivery debt, sources, stable identifiers, and mechanical lint. The five v18 review outputs named by the spine were treated as concurrent gate deliverables, not pre-existing source inputs.

The supplied frozen hashes matched at intake:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `ab7b4962d281a103dbe52bb3f2bbba2d34848d27718b7b8f83f999e1ff484d25` |
| `IMPLEMENTATION-CONVENTIONS.md` | `247ea972ca9c16c0366412dc338806ab7ddc69741a03307e37dc354ef7a4300c` |
| architecture `.memlog.md` | `828ed872d585f3ddf640d331d3172426f65cc9c48fcabc550689481206cb70d3` |
| bound `prd.md` | `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb` |
| `epics.md` | `e07a65002449503320f25495b6ffe1953fd786492435e0ddfccde14d0c620296` |
| `external-dependency-register.md` | `1353ef89e76f74c8257ce3b4cdefd0fd2a98ec33f530de0cfd1376bea73ed25a` |
| `launch-readiness-register.md` | `1010c0cbf5d03e5d65664c9bbb3aef4a8a1e734620a91b3a123d8bf98a43b4bd` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

Repository inspection was read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`. The parent-authoritative Builds gitlink remains `a32cb422`, while its modified checkout is `cf52f74`; the architecture continues to distinguish checkout evidence from root authority.

## Deterministic Linter

Command:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: `ok: true`, `total_findings: 0`. A separate ID scan found exactly one each of AD-1 through AD-31, maximum AD-31, with no gap, duplicate, reuse, deletion, or renumbering.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 0 |
| Medium | 3 |
| Low | 2 |

## Critical

### C-R18-1 — The destruction-start guard commit is not serialized with a hold arriving after barrier authorization

**Classification:** target-architecture irreversible-deletion and cross-owner linearization defect; not implementation debt and not an unresolved Product outcome.

**Disposition:** discuss the common linearization mechanism, then apply an architecture correction. Do not select the late-hold Product branch.

**Evidence.** AD-22 says `DeletionArmed` is reversible and `OD-HOLD-DELETION-PRECEDENCE-1` exclusively governs a hold that first contends after `DeletionArmed` and before `DestructionStarted`; while that decision is Open, neither the hold nor deletion may advance on the armed-contention branch (`ARCHITECTURE-SPINE.md:459,473`; bound PRD `prd.md:955,957`). Matrix row 325 appends `AuthorizeDeletionDestructionStartBarrier` on `ProtectionFence` after checking either no hold at the Armed/authorization revisions or the approved contention outcome. It explicitly says this authorization does **not** start destruction (`launch-readiness-register.md:325`). Row 326 later calls the EventStore guard; that separate operation becomes `DestructionStarted` and releases the sole irreversible capability (`:326`; `ARCHITECTURE-SPINE.md:224,465`; `IMPLEMENTATION-CONVENTIONS.md:25`).

The EventStore operation is correctly atomic with every matching permit, intent, lease, and phase-authorization append, but its closed preconditions compare admission/content ledgers, migration and fence receipts, removal, compromise, and barrier identity. It does not consume a common token serialized with later `ProtectionFence` hold writes, require that the authorization revision is still the current `ProtectionFence` revision, or prevent a hold intent/contention from winning that fence after authorization. A read/compare of the other aggregate would not be enough because the architecture assumes no multi-stream transaction.

**Literal race.** (1) Deletion is Armed and, while no hold is recorded, appends barrier authorization at `ProtectionFence` revision P. (2) Before the external guard commit, a Compliance Inspector submits an overlapping hold. By the spine's own time boundary this is a pre-`DestructionStarted` armed contention governed by the unresolved OD. Depending on the literal implementation, the hold records contention at P+1 or is denied/pended because the OD is Open. (3) The EventStore guard sees the still-exact recorded authorization and clean admission/content ledgers, commits `DestructionSealed`, and returns irreversible key-destruction authority. It never serializes with or observes the intervening hold contention. The deletion has consequently inferred the no-hold branch after a hold arrived in the interval that Product expressly left unresolved.

**Impact.** A legal-hold request can lose data that the unresolved precedence policy may require preserved. Later mirroring or compromise handling cannot recall a consumed key-destruction capability. This reopens authoritative finding C-2 at the final irreversible boundary and is Critical.

**Required correction.** Give legal-hold contention and `CommitDeletionDestructionStartBarrierEffect` one implementable linearization. For example, the common EventStore guard may admit a target-limited hold-intent reservation before `ProtectionFence` preparation and atomically conflict it with the destruction seal, or the destruction protocol may consume a fencing capability whose currentness every hold-intent append is also required to serialize against. Any safe shape is acceptable, but it must name: the single owner and compare token/revision; the exact before/at/after classification; crash/lost-ack recovery; how the open OD leaves armed contention restrictive; and how ordinary no-hold deletion remains executable. Bind the spine, conventions, matrix rows for hold prepare and destruction start, `EXT-HOST-1`, Story 8.1/8.3, and failure-injection tests. A stale `ProtectionFence` authorization must never remain usable after a hold contender wins. The correction is architecture mechanics and must preserve `OD-HOLD-DELETION-PRECEDENCE-1` as Open rather than choosing cancel/defer versus reject/defer.

## High

None.

## Medium

### M-R18-1 — Architecture-owned `RQ-1` assumptions still lack literal retirement dates

**Classification:** planning-governance debt; safely blocked, not an implementation defect.

PRD FR-28 requires every Architecture-owned `RQ-1` assumption to carry a literal calendar target (`prd.md:730,894-898`). `ARCH-A-1`, `ARCH-A-2`, `ARCH-A-3`, the open test-stack portion of `ARCH-A-4`, `ARCH-A-6`, `ARCH-A-7`, `ARCH-A-8`, `ARCH-A-11`, `ARCH-A-12`, and `ARCH-A-14` remain `Unscheduled` or milestone-only (`ARCHITECTURE-SPINE.md:1309-1323`). The spine correctly emits `UnretiredAssumption` and cannot report READY, so the gap is safely contained. Obtain co-owner-approved dates; Architecture must not invent them.

### M-R18-2 — `PostingPending` still lacks one concrete timeout authority

**Classification:** unresolved Architecture/Product parameter; safely surfaced.

AD-5 fixes only a lower bound tied to the Conversations posting seam and says neither PRD nor spine fixes the duration; `ARCH-A-14` retains that blocker (`ARCHITECTURE-SPINE.md:245,1322`). Two teams could otherwise choose incompatible recovery deadlines. Before its posting/recovery story becomes ready, bind one literal duration or one versioned seam/profile value and obtain Product confirmation.

### M-R18-3 — The pre-commit control-plane vocabulary remains broader than its closed examples

**Classification:** target-document clarity debt; no demonstrated unsafe current branch.

The normative rule permits only closed content-free owner-local checks before lease commit and places protected-content access, target mutation, safety/roster/Conversation or other dependency reads afterward (`ARCHITECTURE-SPINE.md:209,342`; `IMPLEMENTATION-CONVENTIONS.md:9,19`). The matrix uses predicates such as `ClosedContentFreeOwnerAndPhasePinnedControlPlaneEvidenceExact`, but `OwnerLocalCheck`, `ControlPlaneObservation`, and `TargetDependencyRead` are not defined once as a closed vocabulary. Centralize those definitions so a future phase cannot relabel a non-owner readiness/dependency read as pre-commit control-plane evidence.

## Low

### L-R18-1 — The Structural Seed still collapses the two violation ledgers

**Classification:** diagram/re-distillation debt.

The normative prose, conventions, and matrix correctly keep admission-fence and content-write-fence violation checkpoints separate. The `ProtectionFence` class still shows one generic `ViolationContainmentManifest` and only a `DeletionScopeWriteFenceVerificationCheckpoint` (`ARCHITECTURE-SPINE.md:1135-1151`). Mirror the admission ledger/checkpoint and current ordinal in the Structural Seed during the next redistillation; the normative protocol is otherwise explicit.

### L-R18-2 — Root bUnit remains behind the Builds-checkout catalog

**Classification:** implementation/build maintenance.

The root pins bUnit `2.9.0`, while the current Builds checkout catalog pins `2.10.3` (`Directory.Packages.props:26`; `references/Hexalith.Builds/Props/Directory.Packages.props:317`). The spine accurately reports root authority and assigns test-stack alignment to Story 5.6, so this is not an architecture contradiction.

## Requested v17 Critical/High Correction Audit

| v17 finding | v18 disposition |
| --- | --- |
| Adversarial `C-v17-1` / verified-current `VC17-C1` — clean admission proof was not atomic with irreversible start | **Closed for matching admission writes.** `CommitDeletionDestructionStartBarrierEffect` now runs on the EventStore guard, atomically checks the current ordinal and both separate ledgers, serializes with every matching permit/intent/lease/phase-authorization append, commits immutable `DestructionSealed`, and returns the one-shot sole destructive capability (`ARCHITECTURE-SPINE.md:224,465,546`; conventions `:25`; matrix `:325-327`). Lost acknowledgement uses exact lookup. C-R18-1 is a distinct adjacent `ProtectionFence` legal-hold race exposed by moving the true start instant to that guard. |
| Adversarial `H-v17-1` / verified-current `VC17-H1` — recut could not re-close an Effective owner and zero proof lacked ordinal identity | **Closed.** Each owner cycle is immutable and separately keyed by `(DeletionRequestId, ScopePredicateDigest, AdmissionFenceOrdinal)`; a successor never reopens prior Effective state, carries every still-restrictive obligation plus the new/violating winner, and produces a new same-ordinal fixed point (`ARCHITECTURE-SPINE.md:214,220,222,544`). The guard ledger is append-only per ordinal; prior violations remain visible and only the current ordinal can attest `ZeroAcceptedViolationsSinceInstall(InstallCheckpoint, VerifiedHighWater)`. Candidate, acceptance, preparation, barrier, migration preservation, Abort release, matrix, conventions, epics, registers, and memlog carry that identity. |

## Authoritative 2026-09-12 Critical/High Closure Audit

| Authoritative finding | v18 disposition |
| --- | --- |
| C-1 weaker safety evaluation | **Closed.** AD-20 requires conjunctive snapshot/current evaluation; one pass is allowed only with machine-checkable, tested dominance. |
| C-2 hold/deletion exclusion | **Reopened at C-R18-1 only.** The accepted set, ordinal admission/effect cut, content fence, export/copy accounting, prepare/arm receipts, and admission-atomic start barrier are explicit. The barrier still fails to serialize against a hold first contending after its `ProtectionFence` authorization but before actual `DestructionStarted`. |
| C-3 bootstrap/matrix deadlock | **Closed.** Matrix v4 contains target-aware Platform/Tenant bootstrap, repair, recorder, empty-state, and broken-state paths. |
| H-1 scheduled Approver re-check | **Closed.** Durable single-flight ownership, cadence/freshness, two-pass empty evidence, phase lease, typed outcomes, and recovery are bound. |
| H-2 distributed safety rescan | **Closed.** EventStore epoch/index ownership, finite cohort, bounded fenced coordinator, activation barrier, and `RescanPending` behavior are executable. |
| H-3 human-only Approvers | **Closed.** Configuration and runtime require Parties-authoritative human/liveness evidence and stable actor binding. |
| H-4 ledger lifetime conflation | **Closed.** Rate, open-interaction, Budget, and capacity have separate owners, identities, lifetimes, and settlement rules. |
| H-5 human identity/separation | **Closed.** Stable `AuthenticatedHumanActorId` spans Party-bearing User, Administrator, and Platform evidence; Workflow is non-human. |
| H-6 proposal-index crash consistency | **Closed.** Interaction truth, same-append source-revision outbox, directory high-water, reconciliation, and removal fixed point are explicit. |
| H-7 indivisible Conversations dependency | **Closed.** Core `EXT-CONV-AI-1` and optional `EXT-CONV-RETRACTION-1` remain independently governed. |
| H-8 trusted-envelope security | **Closed.** Canonical authenticated fields, logical/delivery identity split, platform-wide issuer replay owner, retention, rotation/revocation, ACLs, and lost-ack recovery remain bound. |
| H-9 export ownership/lifecycle/signature | **Closed subject to the surfaced Product lifecycle decision.** Immutable store/index, partial-output inventory, fence commit, ES256/JCS manifest, direct key delivery, cleanup/all-copy recovery, and later-hold/restore safe state are bound. |
| H-10 current Dapr exposure | **Closed as current-reality classification.** Parent-authoritative Client/ASP.NET `1.18.5`, non-authoritative checkout `1.18.7`, and future Workflow adoption remain distinguished. |
| H-11 public-contract parity overclaim | **Closed.** Target vocabulary and current implementation debt are explicit and separate. |
| H-12 sprint/evidence contradiction | **Closed as delivery-governance debt.** `OD-SPRINT-5.1-5.2-1` preserves the owner decision without rewriting tracker history or treating it as architecture authority. |

## AD IDs, Memlog, Open Decisions, Sources, And Repository Reality

- The spine contains exactly one each of AD-1 through AD-31. No ID was renumbered, reused, or deleted; maximum remains AD-31.
- The memlog remains append-only. Its v18 tail records ordinal-keyed immutable owner cycles, carried-forward obligations, current-ordinal zero proof, and the admission-atomic EventStore start barrier without rewriting earlier history.
- `OD-HOLD-DELETION-PRECEDENCE-1` remains correctly Open. C-R18-1 does not ask Architecture to select its outcome; it requires the technical serialization that preserves the Product choice. The other named Product/Governance/Security decisions continue to carry owners, affected scopes, safe states, and revisit points rather than hidden defaults.
- All twelve external-dependency entries remain `Uncommitted`; `RQ-1` remains NOT READY. No checkout, planned adapter, or partial implementation is mislabeled `Available`.
- Focused repository inspection still shows the parent-authoritative Builds gitlink `a32cb422` versus checkout `cf52f74`, root bUnit `2.9.0` versus checkout `2.10.3`, absent Agents Dapr Workflow adoption, current direct/plaintext interaction state, and absence of target directory/lease/fence/decision/export protocols. Those are delivery debt, not implicit architecture alternatives.
- Apart from the five concurrent v18 review outputs, every declared local source resolved.

## Whole Good-Spine Checklist

| Dimension | Result |
| --- | --- |
| Fixes downward divergence | **Fail at C-R18-1.** The hold-versus-destruction cut can be implemented as stale authorization consumption or as an invented cross-owner reservation. Other high-risk cuts are explicit. |
| AD enforceability / stated prevention | **Fail at AD-22's final boundary.** Admission serialization is enforceable, but the no-hold fact is not protected through the later irreversible commit. |
| Deferred/open decisions | **Pass in representation; fail in preservation at C-R18-1.** Decisions are named and blocked, but this race can bypass the open late-hold choice. |
| Named technology / repository truth | **Pass.** Root authority, modified checkouts, exact pins, unselected components, and unavailable dependencies are distinguished. |
| Bound PRD and epics coverage | **Fail only at the PRD's armed late-hold race.** Other FR/NFR capabilities trace through ADs, matrix, registers, and Stories 5-8. |
| Brownfield ratification | **Pass.** Present direct/plaintext streams and missing target protocols are classified as implementation debt, not ratified as target state. |
| Security, tenancy, data loss | **Fail at C-R18-1.** A hold may not participate in the same final irreversible linearization. Other tenant/predicate, actor, replay, protected-content, export, and deletion boundaries fail closed. |
| Recovery / operations / environment | **Pass except for C-R18-1's missing cross-owner race recovery.** Effect authorization, RPO-0, restore, spool, migration, ordinal recut, guard lookup, and two-origin deletion recovery are explicit. |
| Sources / mechanics | **Pass.** Local sources resolve as scoped, AD IDs remain stable, hashes match, and deterministic lint is clean. |

## Architecture Defects Versus Delivery Debt

C-R18-1 is a target architecture defect: downstream implementation cannot close it locally without inventing a cross-owner protocol or implicitly choosing how a late hold loses. It must be corrected in the spine, conventions, matrix, seam contract, epics, and memlog before the reviewer gate can pass.

M-R18-1 and M-R18-2 are safely blocking planning/parameter debt; M-R18-3 and L-R18-1 are clarity/redistillation debt; L-R18-2 is build maintenance. The absent target directory, effect leases, phase-authorization ledgers, migration/repair guards, deletion fences, decision catalog, security spool, export machinery, and public vocabulary in current code remain implementation debt already assigned by the spine and epics. Their absence does not authorize weakening the target contracts.

## Required Gate Correction

1. Serialize a legal-hold contender and the actual `DestructionSealed` commit through one authoritative token/owner, while preserving the unresolved Product outcome and ordinary no-hold deletion.
2. Add before/at/after authorization, hold-contention, guard-commit, lost-ack, and key-capability-consumption fixtures for both deletion origins.
3. Re-distill and rerun the complete gate after the correction. The Medium/Low tail may remain only if it is explicitly retained as safely blocking or maintenance debt.

## Post-Write Integrity Check

After creating this report, all eight reviewed inputs retained the frozen SHA-256 values listed above. The deterministic architecture linter was rerun read-only and again returned `ok: true`, `total_findings: 0`.

## Gate Conclusion

The complete v18 good-spine rubric reviewer gate is **FAIL**. Final counts: **1 Critical, 0 High, 3 Medium, 2 Low**.
