---
name: Hexalith Agents good-spine rubric review v19
type: architecture-spine-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-13
intent: review-read-only
lens: bmad-architecture good-spine rubric walker
verdict: fail
critical: 0
high: 3
medium: 4
low: 1
lint_ok: true
---

# Good-Spine Rubric Reviewer Gate — v19

## Verdict

**FAIL — 0 Critical, 3 High, 4 Medium, 1 Low.** The frozen v19 package closes the v18 stale no-hold authorization race and materially implements the three v18 recovery/capability corrections. A complete rubric walk nevertheless finds three fail-closed convergence defects: the matrix accidentally requires successor containment evidence on the first admission-fence authorization; the accepted-set destruction capability is required to be unexpired without an expiry or renewal contract after irreversible start; and later containment batches have no guard-owned capability-issuance transition even though `GovernanceScopeGuard` owns their identities and the protection port requires an authenticated guard capability.

## Frozen Inputs And Method

I read the complete current spine, implementation conventions, architecture memlog, authoritative validation report, bound PRD, epics, external-dependency register, launch-readiness register, repository instructions, reviewer-gate rubric, declared local sources, and focused current repository/submodule/package evidence. I walked the whole good-spine checklist rather than treating v18 findings as the checklist: divergence prevention; AD enforceability; aggregate and revision ownership; authorization/effect/result order; race, crash, lost-ack, replay, restore, and cleanup behavior; security and irreversible data boundaries; PRD/epic/register traceability; open Product decisions; brownfield ratification; architecture-versus-delivery debt; stable IDs; source integrity; and mechanics.

The five v19 review paths in spine frontmatter were treated as concurrent gate outputs, not pre-existing source inputs. The supplied frozen hashes matched at intake:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `79ebd22bba3ec5c42eae31377a461f00a94035c1cef8422927562295d9a7e950` |
| `IMPLEMENTATION-CONVENTIONS.md` | `4686b409fce780443ec4d141e5306b7bf85183d23e31e6f6260ddc18a0ca2744` |
| architecture `.memlog.md` | `4df0405f18c54441f6d8ed20cfa5ac39798eba21620d2f731fefc9860f2caa01` |
| bound `prd.md` | `d715f54d76e58a9ba6183a0e04979779703476b855842513816c35b8655ab9b0` |
| `epics.md` | `ed9ccb70a25e80fa16833f09b2af6f219d8291e4d5bd5cb0aa17d6e9de59ad56` |
| `external-dependency-register.md` | `60ff899220f10f2227f420e9321f01328d5048481cf4cad55553b0f8e78b206c` |
| `launch-readiness-register.md` | `eca4fd6c787a7f3e65fea4570262c294895933a7114b3f262e4f1f340b6b6383` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

Focused repository inspection was read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`. The parent-authoritative Builds gitlink remains `a32cb422`; the internally clean checkout is `cf52f74`. The spine correctly treats that checkout as evidence rather than root authority.

## Deterministic Linter

Command:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: `ok: true`, `total_findings: 0`. An independent heading scan found exactly one each of AD-1 through AD-31, maximum AD-31, with no gap, duplicate, reuse, deletion, or renumbering.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 3 |
| Medium | 4 |
| Low | 1 |

## Critical

None.

## High

### H-R19-1 — Ordinal-one admission-fence authorization requires a containment receipt that cannot exist

**Classification:** target-architecture operation-matrix/state-machine defect; not implementation debt and not a Product decision.

**Disposition:** autofix the branch grouping.

**Evidence.** The spine's ordinary path authorizes and installs the first admission fence before any owner enumeration or candidate exists (`ARCHITECTURE-SPINE.md:223-225`). Matrix row 296 correctly begins with the alternative `NoPriorAdmissionFenceForRequestAndOrdinalOne` **or** `RecordedPreDestructionAdmissionFenceViolationContainedAndNextOrdinalAssignedOnce`, but then adds the successor-only predicate `ContainmentReceiptProvesEitherNoGlobalCutCandidateTokenExistedOrEachExactExistingArtifactWasInvalidated` as an unconditional following precondition (`launch-readiness-register.md:296`). Matrix preconditions are conjunctive except where the row explicitly groups alternatives. On ordinal one there is no violation and therefore no containment receipt to prove either branch.

**Divergence/impact.** A literal evaluator rejects every first deletion-fence authorization. A permissive evaluator treats the containment predicate as vacuously true, while another silently couples it only to the successor side of the prior OR. Only the latter two progress, but both invent matrix semantics. This blocks both deletion origins before their first cut and recreates a scoped form of authoritative C-3's bootstrap deadlock. It remains High rather than Critical because it fails closed without destructive work.

**Required correction.** Group the branch as `NoPriorAdmissionFenceForRequestAndOrdinalOne` **or** (`Recorded...Contained...` **and** `ContainmentReceiptProves...`), with prior-receipt retention similarly defined as vacuous only on the explicit initial branch. Add an empty-request/ordinal-one fixture plus pre-first-cut and post-cut successor fixtures that prove exactly one branch and no inferred evidence.

### H-R19-2 — The sole accepted-set batch can expire after `DestructionStarted` with no legal renewal

**Classification:** target-architecture irreversible-phase recovery defect; not Product policy and not merely implementation debt.

**Disposition:** discuss and fix the durable capability lifetime/renewal mechanics.

**Evidence.** The guard commit is `DestructionStarted`, returns one deterministic single-use accepted-manifest capability, and forbids minting a second batch; crash/lost acknowledgement recovers that exact batch (`ARCHITECTURE-SPINE.md:231,466`; `epics.md:3027`; `external-dependency-register.md:233,236`). Matrix row 337 additionally requires `BatchCapabilityAuthenticUnexpiredUnconsumedAndUnrevoked` before the protection owner can consume it (`launch-readiness-register.md:337`). No spine, register, convention, external contract, or story field defines the batch expiry instant, a phase-pinned lifetime, or a renewal/re-sign operation that preserves the immutable `BatchId` after expiration.

**Divergence/impact.** One implementation makes the capability non-expiring; another applies a normal signed-capability lifetime and permanently wedges a deletion if outage/recovery crosses it; a third mints a fresh capability despite the explicit no-second-batch rule. The wedge occurs after the architecture has durably crossed its irreversible start boundary and cannot legally Abort, so ordinary exact lookup cannot restore executability even though protected content remains. That contradicts the NFR-11 recovery contract and the matrix's recorded-branch recovery claim.

**Required correction.** Either make the guard capability durably non-expiring once `DestructionSealed` commits, or define an authenticated renewal operation that retains the same batch id, manifest digest, seal revision, target set, and revocation/consumption state and cannot expand authority. Bind AD-28 time semantics, exact lookup, outage recovery, and before/at/after expiry-versus-consume/revoke tests. Do not mint another logical batch.

### H-R19-3 — Post-start containment batches lack a guard-owned capability-issuance transition

**Classification:** target-architecture ownership/authorization and recovery defect; not Product policy or current-code debt.

**Disposition:** autofix with a closed guard authorize/effect/result path.

**Evidence.** AD-2 assigns accepted and containment manifest-batch identities to `GovernanceScopeGuard(TenantId)` (`ARCHITECTURE-SPINE.md:209`), and `EXT-PROTECTION-1` accepts `DestroyDekManifest(..., GuardCapability)` (`external-dependency-register.md:233`). The accepted-set batch is soundly created by the guard's atomic `DestructionSealed` commit. For a post-start content violation, however, matrix row 323 appends a containment manifest at a `ProtectionFence` expected revision and merely says it “derives” a distinct batch; row 338 says each containment ordinal uses the same protocol (`launch-readiness-register.md:323,338`). No matrix operation authorizes and commits the new batch identity on `GovernanceScopeGuard`, returns its authenticated guard capability, or resolves that issuance's conflict/lost acknowledgement. The AD-30 deletion capability allowlist names containment/batch recording but no guard effect that can mint the later capability (`ARCHITECTURE-SPINE.md:553`).

**Divergence/impact.** One team lets a `ProtectionFence` event stand in for a guard-signed capability, weakening the stated guard ownership. Another adds an undocumented guard append. A strict protection adapter rejects the batch because no authentic `GuardCapability` exists. The latter two paths can assign different batch ids or lose issuance acknowledgement; the strict path leaves newly discovered in-scope content readable forever after deletion start. Completion blocks rather than falsely succeeds, so the defect is High.

**Required correction.** Add a target-limited containment-batch authorize/effect/result transition on the existing `GovernanceScopeGuard`, bound to the immutable seal, continuous content-guard violation/containment ordinal, canonical predicate, exact target alias, and expected guard revision. It must record one deterministic identity, return/lookup one authentic capability, conflict on changed input, participate in migration preservation and revocation, and precede the protection-owner all-or-none consumption. Extend conventions, matrix, host/protection dependencies, Story 8.3, and lost-ack/concurrent-containment fixtures.

## Medium

### M-R19-1 — Architecture-owned `RQ-1` assumptions still lack literal retirement dates

**Classification:** planning-governance debt; safely blocking.

PRD FR-28 requires Architecture-owned `RQ-1` assumptions to carry a literal calendar target (`prd.md:730,894-898`). `ARCH-A-1`, `ARCH-A-2`, `ARCH-A-3`, the open test-stack portion of `ARCH-A-4`, `ARCH-A-6`, `ARCH-A-7`, `ARCH-A-8`, `ARCH-A-11`, `ARCH-A-12`, and `ARCH-A-14` remain `Unscheduled` or milestone-only (`ARCHITECTURE-SPINE.md:1329-1343`). The register correctly keeps them blocking. Obtain co-owner-approved dates; Architecture must not invent them.

### M-R19-2 — `PostingPending` still lacks one concrete timeout authority

**Classification:** unresolved Architecture/Product parameter; safely surfaced.

AD-5 fixes only a lower bound tied to the Conversations posting seam and says neither PRD nor spine fixes the concrete duration; `ARCH-A-14` retains the blocker (`ARCHITECTURE-SPINE.md:252,1342`). Bind one literal duration or one versioned seam/profile value and obtain Product confirmation before posting/recovery implementation.

### M-R19-3 — The pre-commit control-plane vocabulary remains broader than its closed examples

**Classification:** target-document clarity debt.

The normative rule permits only content-free owner-local identity, shape, principal, expected-revision, migration, and barrier checks before lease commit, and places protected content, target mutation, safety/roster/Conversation, and other dependency reads after commit (`ARCHITECTURE-SPINE.md:214,349`; `IMPLEMENTATION-CONVENTIONS.md:9,19`). Matrix predicates still use broader terms such as `ControlPlaneObservation` without one closed central definition. Define the vocabulary once so future phases cannot relabel non-owner dependency/readiness work as pre-commit evidence.

### M-R19-4 — Two historical v5 review sources remain declared but absent

**Classification:** source-chain/re-distillation debt.

Spine frontmatter declares `reviews/review-2026-09-12-security-data-integrity-v5.md` and `reviews/review-2026-09-12-brownfield-drift-v5.md` (`ARCHITECTURE-SPINE.md:110-111`), but neither file exists. Unlike the five v19 paths, these are not the current concurrent outputs. Produce them if they are genuine inputs or remove the declarations; do not substitute newer reports for nonexistent historical evidence.

## Low

### L-R19-1 — Root bUnit remains behind the current Builds-checkout catalog

**Classification:** implementation/build maintenance.

The root pins bUnit `2.9.0`, while the current Builds checkout catalog pins `2.10.3` (`Directory.Packages.props:26`; `references/Hexalith.Builds/Props/Directory.Packages.props:317`). The spine accurately reports root authority and assigns test-stack alignment to Story 5.6, so this is not an architecture contradiction.

## v18 Critical/High Closure Audit

| v18 finding | v19 disposition |
| --- | --- |
| `C-R18-1` / adversarial `C-v18-1` — stale no-hold authorization after a contender arrives | **Closed.** Every authorized hold first registers its predicate on `GovernanceScopeGuard(TenantId)`. Registration and `CommitDeletionDestructionStartBarrierEffect` serialize on that owner; a contender winning after authorization makes it stale, Open precedence advances neither branch, and only an exact effective deletion-allowed disposition can be consumed inside the seal (`ARCHITECTURE-SPINE.md:229-231,466,480`; matrix `:274-285,334-335`; PRD `:955-957`). |
| adversarial `H-v18-1` — pre-first-global-cut admission violation could not reach a successor | **Closed in the containment state machine.** The explicit no-artifact branch records the violation, invalidates nothing nonexistent, assigns one successor ordinal, and makes concurrent/replay/lost-ack detection converge (`ARCHITECTURE-SPINE.md:227`; matrix `:321`; Story 8.3 `:3007-3011`). H-R19-1 is a new branch-grouping defect in the shared authorization row that also affects the ordinary ordinal-one path. |
| adversarial `H-v18-2` — installed content fence could not bind a successor token | **Closed.** One `(DeletionRequestId, ScopePredicateDigest)` content guard remains continuously installed and gains a successor ordinal/global-cut/token only through explicit authorize/effect/result binding with exact lookup and no remove/reinstall gap (`ARCHITECTURE-SPINE.md:227,470`; matrix `:315-320`; external register `:129-132`). |
| adversarial `H-v18-3` — singular barrier token had undefined multi-DEK semantics | **Closed for the accepted set.** The seal returns one deterministic manifest-bound batch; the protection owner atomically consumes all sorted targets or none, returns an ordered per-target vector, conflicts changed input, and resolves retry/lost result by exact batch lookup (`ARCHITECTURE-SPINE.md:231,466`; matrix `:334-339`; external register `:233-236`). H-R19-2 and H-R19-3 are newly exposed lifetime and later-containment issuance gaps, not a reassertion of the original per-key-versus-batch ambiguity. |

## Authoritative 2026-09-12 Critical/High Closure Audit

| Authoritative finding | v19 disposition |
| --- | --- |
| C-1 weaker safety evaluation | **Closed.** Snapshot/current evaluation is conjunctive; a one-pass optimization requires machine-checkable tested dominance. |
| C-2 hold/deletion exclusion | **Closed for safety.** Shared fence/scope guard, accepted inventory, ordinal admission/effect cut, continuous content guard, manifest batches, export/copy accounting, and restrictive completion prevent partial-success claims. H-R19-2/H-R19-3 are fail-closed recovery/executability gaps after start, not authorization to report successful deletion. |
| C-3 bootstrap/matrix deadlock | **Reopened narrowly as H-R19-1.** General target-aware bootstrap/repair remains closed; only deletion admission ordinal one is accidentally coupled to successor containment evidence. |
| H-1 scheduled Approver re-check | **Closed.** Durable single-flight owner, cadence/freshness, two-pass empty evidence, leased reads, state-specific outcomes, and recovery remain bound. |
| H-2 distributed safety rescan | **Closed.** Epoch/index ownership, finite enumeration, bounded fenced coordinator, activation barrier, and `RescanPending` behavior remain executable. |
| H-3 human-only Approvers | **Closed.** Configuration/runtime require Parties-authoritative human/liveness and stable actor binding. |
| H-4 ledger lifetime conflation | **Closed.** Rate, open-interaction, Budget, and capacity have distinct owners, identities, lifetimes, and settlement. |
| H-5 human identity/separation | **Closed.** Stable `AuthenticatedHumanActorId` spans Party-bearing User, Administrator, and Platform evidence; Workflow remains non-human. |
| H-6 proposal-index crash consistency | **Closed.** Interaction truth, same-append source-revision outbox, directory high-water, reconciliation, and removal fixed point are explicit. |
| H-7 indivisible Conversations dependency | **Closed.** Core `EXT-CONV-AI-1` and optional `EXT-CONV-RETRACTION-1` are independently governed. |
| H-8 trusted-envelope security | **Closed.** Canonical authenticated fields, logical/delivery identity split, platform-wide replay owner, time/key lifecycle, ACLs, and lost-ack recovery are bound. |
| H-9 export ownership/lifecycle/signature | **Closed subject to the surfaced Product lifecycle decision.** Immutable store/index, partial-output inventory, fence commit, ES256/JCS manifest, direct key delivery, cleanup/all-copy recovery, and later-hold/restore safe state are bound. |
| H-10 current Dapr exposure | **Closed as current-reality classification.** Parent-authoritative Client/ASP.NET `1.18.5`, non-authoritative checkout `1.18.7`, and future Workflow adoption are distinguished. |
| H-11 public-contract parity overclaim | **Closed.** Target completion vocabulary and current implementation debt are separate. |
| H-12 sprint/evidence contradiction | **Closed as surfaced delivery-governance debt.** The owner decision remains Open without rewriting tracker history or granting architecture authority. |

## AD IDs, Memlog, Open Decisions, Sources, And Repository Reality

- The spine contains exactly one each of AD-1 through AD-31; no ID was renumbered, reused, or deleted.
- The memlog remains append-only. Its latest entries record the scope-guard hold/seal owner, explicit pre/post-cut containment, continuous content binding, and accepted-set manifest batch without rewriting earlier decisions.
- The PRD now explicitly binds legal-hold intent and irreversible deletion start to one durable guard and binds multi-DEK deletion to an all-or-none accepted-manifest capability. These are recorded requirements, not architecture-invented Product outcomes.
- `OD-HOLD-DELETION-PRECEDENCE-1`, `OD-HOLD-PREPARE-CANCELLATION-1`, `OD-OPERATOR-DELETION-CANCELLATION-1`, `OD-OPERATOR-DELETION-NONTERMINAL-DISPOSITION-1`, `OD-EXPORT-LIFECYCLE-1`, `OD-RATE-CONCURRENCY-CONSUMPTION-1`, `OD-DAPR-SECURITY-1`, `OD-SPRINT-5.1-5.2-1`, the materialized PRD decisions, and the scope/migration/output/recorder decisions retain named owners, safe states, affected evaluations, and revisit points. This review selects none of their outcomes.
- All twelve external dependency records remain `Uncommitted`; `RQ-1` remains NOT READY. The current repository lacks the target directory, lease, guard, decision, spool, protection-batch, and export protocols; the spine correctly labels those as assigned implementation debt rather than target compliance or optional architecture.
- The two absent historical v5 reviewer declarations produce M-R19-4. The five v19 review paths are concurrent anticipated outputs and were not treated as missing sources.

## Whole Good-Spine Checklist

| Dimension | Result |
| --- | --- |
| Fixes downward divergence | **Fail at H-R19-1 through H-R19-3.** Initial fence evaluation, capability lifetime, and later containment issuance admit incompatible literal implementations. |
| AD enforceability / stated prevention | **Fail at the matrix and containment/batch recovery seams.** Core AD rules are otherwise concrete, owner-bound, and testable. |
| Deferred/open decisions | **Pass.** Product/Governance/Security decisions are named with safe states and no local outcome is invented. |
| Named technology / repository truth | **Pass.** Root authority, modified checkout, exact pins, unavailable dependencies, and future adoption are distinguished. |
| Bound PRD and epics coverage | **Pass in capability coverage; fail in exact executable mechanics at the three Highs.** |
| Brownfield ratification | **Pass.** Current direct/plaintext and missing target protocols are delivery debt, not ratified target behavior. |
| Security, tenancy, data loss | **Pass for authorization and restrictive completion.** No new false-success/data-loss path was found; batch/containment gaps fail closed. |
| Recovery / operations / environment | **Fail at H-R19-2/H-R19-3.** Exact recovery cannot consume an expired accepted batch or recover an unissued containment capability. Other restore/migration/spool/export/deletion paths are explicit. |
| Sources / mechanics | **Pass lint/IDs/hashes; Medium source-chain debt at M-R19-4.** |

## Architecture Defects Versus Delivery Debt

H-R19-1 through H-R19-3 are target architecture defects. Downstream teams cannot resolve them locally without inventing matrix branching, batch lifetime/renewal, or guard capability issuance. They must be corrected in the spine and bound companions before the gate can pass.

M-R19-1 and M-R19-2 are safely blocking planning/parameter debt; M-R19-3 and M-R19-4 are clarity/source-redistillation debt; L-R19-1 is build maintenance. The missing implementation of the target directory, effect leases, phase authorization ledgers, migration/repair guards, deletion admission/content/scope guards, destruction batches, decision catalog, security spool, export store, and expanded public contracts remains assigned implementation debt and is not counted as a target-architecture finding.

## Required Corrections

1. Couple the containment-receipt predicate only to the successor side of matrix row 296 and add a strict ordinal-one fixture.
2. Define durable accepted/containment batch capability lifetime or same-identity renewal after `DestructionSealed`.
3. Add a guard-owned authorize/effect/result/lookup operation for each post-start containment batch before protection consumption.
4. Re-distill and rerun the complete reviewer gate.

## Post-Write Integrity Check

After writing this report, all eight reviewed inputs retained the frozen SHA-256 values listed above. The deterministic architecture linter was rerun read-only and again returned `ok: true`, `total_findings: 0`.

## Gate Conclusion

The complete v19 good-spine rubric reviewer gate is **FAIL**. Final counts: **0 Critical, 3 High, 4 Medium, 1 Low**.
