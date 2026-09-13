---
name: Hexalith Agents good-spine rubric review v20
type: architecture-spine-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-13
intent: review-read-only
lens: bmad-architecture good-spine rubric walker
verdict: fail
critical: 1
high: 0
medium: 3
low: 1
lint_ok: true
---

# Good-Spine Rubric Reviewer Gate — v20

## Verdict

**FAIL — 1 Critical, 0 High, 3 Medium, 1 Low.** The frozen v20 package closes all three v19 High findings: ordinal one is an explicit alternative to successor containment; accepted and containment capabilities are non-expiring, revocable, and verifiable through their terminal outcomes; and post-seal containment now has a guard-owned authorize/effect/issued-or-stale path. Repeated recuts also form a gap-free chain from the latest installed binding and make overtaken authorizations terminal-obsolete. A fresh whole-spine walk nevertheless finds one later false-success race: final deletion completion is still a direct guard verification followed by a different-owner append, rather than an operation serialized on `GovernanceScopeGuard`. An accepted admission or content violation can therefore win after the last clean checkpoint but before completion is recorded, leaving a successful deletion claim even though newly admitted work or readable content survives.

## Frozen Inputs And Method

I read the complete current spine, implementation conventions, architecture memlog, authoritative validation report, bound PRD, epics, external-dependency register, launch-readiness register, repository instructions, reviewer-gate rubric, declared local sources, and focused current repository/submodule/package evidence. I walked the whole good-spine checklist rather than treating v19 findings as the checklist: decision divergence prevention; AD enforceability; ownership and expected-revision boundaries; authorization/effect/result order; concurrent write, crash, replay, lost-acknowledgement, restore, and cleanup behavior; tenant/security/data-loss boundaries; PRD and epic traceability; unresolved Product authority; brownfield ratification; target architecture versus delivery debt; source integrity; and mechanics.

The five v20 review paths in the spine frontmatter were treated as concurrent gate outputs, not pre-existing source inputs. The supplied frozen hashes matched at intake:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `ec53b5116c56af2bb87c52002b1906d60189d2e738cbc038d165a11aeeceb0fd` |
| `IMPLEMENTATION-CONVENTIONS.md` | `6f2503b8e31a357ee0d91b0bd754e067e7f364590b2ff741fa9a639d2856ad80` |
| architecture `.memlog.md` | `e9da3dce567da7458dc499ca3c6a8b55891e864dafda09d6ef9a4084852d4510` |
| bound `prd.md` | `2653ad3e683ba2d4901f4e29c073c29603b6110be079cfc7c3c92b53a6c323a9` |
| `epics.md` | `533b33d9eaebd5a2ef3cdb6901237dff41a41c2ac44ca1326a69645e25c4c0ef` |
| `external-dependency-register.md` | `54fa2073f583bdaa444e3b193e188963547482a04c5e33fbe1fc9b3fb960a734` |
| `launch-readiness-register.md` | `ecb205005322a91aae24718c643861231d730f79a5fdb1676f42bb77ccc1e6f9` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

Focused repository inspection was read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`. The parent-authoritative Builds gitlink remains `a32cb422`; the internally clean checkout is `cf52f74`. The spine correctly treats the latter as evidence rather than root authority.

## Deterministic Linter

Command:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: `ok: true`, `total_findings: 0`. An independent heading scan found exactly one each of AD-1 through AD-31, maximum AD-31, with no gap, duplicate, reuse, deletion, or renumbering.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 0 |
| Medium | 3 |
| Low | 1 |

## Critical

### C-R20-1 — Final deletion completion is not serialized with late admission/content violations

**Classification:** target-architecture irreversible-deletion and data-integrity defect; not implementation debt and not a Product decision.

**Disposition:** add an owner-linearized completion authorize/effect/result boundary.

**Evidence.** The package correctly makes `GovernanceScopeGuard(TenantId)` the owner that serializes admission/content guard state, hold contenders, the destruction seal, and containment batch issuance. Destruction start is deliberately not a read-then-append check: a `ProtectionFence` authorization captures the observed guard revision, and `CommitDeletionDestructionStartBarrierEffect` conditionally appends the seal on the guard so an admission/hold/guard mutation wins or loses atomically (`ARCHITECTURE-SPINE.md:228-232,473,554`; matrix rows 338-341). The package also promises that an accepted post-seal admission is `DeletionIntegrityCompromised`, that a post-seal content violation enters containment, and that either condition prevents successful completion (`ARCHITECTURE-SPINE.md:232,287,473`; `external-dependency-register.md:129`; matrix rows 321, 323-327, 344-346).

Final completion does not use the same ownership rule. `GovernanceProtection:DeletionCompleteRecovery` directly checks complete EventStore admission/content checkpoints, absence of compromise/unresolved violations, all batch vectors, all-copy receipts, and the `ProtectionFence` expected revision, then claims that no completion can hide a late authorization or write (matrix row 346). `ProtectionFence`/`ProtectedDeletion` is not the owner that serializes those guard-ledger mutations. The closed EventStore capability list has install, successor binding, destruction seal, and containment-batch commit operations, but no completion authorization or guard-side conditional completion operation/result (`ARCHITECTURE-SPINE.md:554`). No spine, convention, epic, dependency, or matrix clause makes the last clean guard verification atomic with the terminal completion fact.

**Divergence/impact.** A literal workflow can read clean checkpoints and complete batch/copy receipts, then pause. A stale, restored, or defective writer can next win a matching admission append or content append; the guard records its accepted violation and advances the guard revision. The workflow can still append successful completion at the unchanged protection/deletion revision. If the late event is an admission, an unmanifested committed effect can now run; if it is content, newly readable in-scope content exists without a containment batch. The later integrity event cannot make the already-published successful deletion claim never have occurred, and no terminal-to-compromised recovery/public contract is defined. This reopens authoritative C-2 at the final cut and is Critical because the system may attest complete erasure while protected data or an authorized external effect survives.

**Required correction.** Mirror the sound destruction-start pattern at completion without choosing Product policy. After all batch vectors, physical purge/all-copy receipts, hold/export state, current ordinal/binding, and separate ledger checkpoints are exact, append a target-limited completion authorization on `ProtectionFence` that records the observed `GovernanceScopeGuard` revision and full completion manifest. Only that revision may invoke a guard-owned `CommitDeletionCompletionBarrierEffect` (or equivalently named operation) that conditionally appends at the observed guard revision and atomically verifies no accepted/unresolved admission or content violation, no compromise, every issued batch terminal vector, and every current fence/binding. It must serialize with every guarded admission/content write and containment/compromise mutation. If a mutation wins, return authenticated stale and require a recorded stale result before reauthorization; if completion wins, the terminal guard seal makes every later matching write reject. Record/mirror the exact guard result on the deletion owner, with exact outcome lookup for crash/lost acknowledgement. Extend AD-2/AD-22/AD-30, conventions, the matrix, `EXT-HOST-1`, Story 8.3, completion manifests, and race/restore fixtures.

## High

None.

## Medium

### M-R20-1 — Architecture-owned `RQ-1` assumptions still lack literal retirement dates

**Classification:** planning-governance debt; safely blocking.

PRD FR-28 requires every Architecture-owned `RQ-1` assumption to have a literal calendar retirement date and says a milestone or non-date blocks on that ground alone (`prd.md:730,894-898`). `ARCH-A-1`, `ARCH-A-2`, `ARCH-A-3`, the open test-stack portion of `ARCH-A-4`, `ARCH-A-6`, `ARCH-A-7`, `ARCH-A-8`, `ARCH-A-11`, `ARCH-A-12`, and `ARCH-A-14` remain `Unscheduled` or milestone-only (`ARCHITECTURE-SPINE.md:1328-1345`). Obtain co-owner-approved dates; Architecture must not invent them.

### M-R20-2 — `PostingPending` still lacks one concrete timeout authority

**Classification:** unresolved Architecture/Product parameter; safely surfaced.

AD-5 fixes only a lower bound tied to the Conversations posting seam and says neither the PRD nor spine fixes the concrete stored duration; `ARCH-A-14` retains the blocker (`ARCHITECTURE-SPINE.md:256-266,1345`). Bind one literal duration or one versioned seam/profile value and obtain Product confirmation before the posting/recovery story. This review does not choose it.

### M-R20-3 — Two declared historical v18 reviewer sources are absent

**Classification:** source-chain/re-distillation debt.

Spine frontmatter declares `reviews/review-2026-09-12-security-data-integrity-v18.md` and `reviews/review-2026-09-12-brownfield-drift-v18.md`, but neither file exists. Unlike the five v20 paths, these are historical, not the current concurrent outputs. Produce them if they are genuine inputs or remove the declarations; do not use newer reviews as silent substitutes.

## Low

### L-R20-1 — Root bUnit remains behind the current Builds-checkout catalog

**Classification:** implementation/build maintenance.

The root pins bUnit `2.9.0`, while the current Builds checkout catalog pins `2.10.3` (`Directory.Packages.props:26`; `references/Hexalith.Builds/Props/Directory.Packages.props:317`). The spine accurately reports root authority and assigns test-stack alignment to Story 5.6, so this is not a target-architecture contradiction.

## v19 Critical/High Closure Audit

| v19 finding | v20 disposition |
| --- | --- |
| `H-R19-1` — ordinal-one authorization required impossible containment evidence | **Closed.** Matrix row 296 now explicitly groups `InitialAdmissionFenceBranch(NoPriorAdmissionFenceForRequestAndOrdinalOne, NoContainmentReceiptExpected, NoPriorFenceOwnerCycleOrViolationReceiptExists)` as an alternative to the conjunctive successor branch. Story 8.3 states the same initial rule. No vacuous or inferred containment proof is needed. |
| `H-R19-2` / adversarial `H-v19-3` — accepted batch expiry could wedge recovery after start | **Closed.** The seal returns a non-expiring, revocable, single-use capability with no `ExpiresAt` or renewal. Its signing-key version remains verifiable for the full retained batch identity and terminal outcome, including lookup after consumption/revocation and restore (`ARCHITECTURE-SPINE.md:232,467,473,554`; matrix 339,342; `EXT-PROTECTION-1`; PRD 957; Story 8.3). |
| `H-R19-3` — containment batch lacked guard-owned issuance | **Closed.** A post-seal violation receives a ProtectionFence ordinal/singleton manifest, then `AuthorizeDeletionContainmentBatch` records an observed guard revision, `CommitDeletionContainmentBatchEffect` conditionally records the deterministic identity on `GovernanceScopeGuard`, and issued/stale results are mirrored before protection consumption with exact lookup (`ARCHITECTURE-SPINE.md:232,473,554`; matrix 323-327,342-343; `EXT-HOST-1`; Story 8.3). |
| adversarial `H-v19-2` — repeated recuts could strand or skip successor bindings | **Closed.** Every containment receipt is an ordered gap link; authorization starts from the latest actually installed binding and the complete gap-free intervening chain. A newer violation makes an in-flight authorization terminal-obsolete without installing an invalid token, and exact lookup resolves either outcome (spine 228,285,471; matrix 318-321; Story 8.3). |

The new guard-revision tokens are correctly narrow where stale authority matters. Destruction and containment authorization record the observed tenant-guard revision; any intervening guard mutation yields an authenticated stale result and a new attempt is legal only after that result is mirrored. Concurrent containment targets serialize into distinct ProtectionFence ordinals, share no manifest, and cannot mint two batch identities. C-R20-1 is at the later completion boundary, not a failure of those v19 fixes.

## Authoritative 2026-09-12 Critical/High Closure Audit

| Authoritative finding | v20 disposition |
| --- | --- |
| C-1 weaker safety evaluation | **Closed.** Snapshot and every then-current applicable policy are conjunctive; optimization requires tested equivalence. |
| C-2 hold/deletion exclusion | **Reopened at final completion as C-R20-1.** Shared fences, guard-linearized start, accepted inventory, atomic batches, and containment are sound before the last cut, but the terminal successful-completion append is not serialized with the guard evidence it claims. |
| C-3 bootstrap/matrix deadlock | **Closed.** Target-aware condition exclusion remains explicit, and deletion ordinal one now has a non-circular initial branch. |
| H-1 scheduled Approver re-check | **Closed.** Durable single-flight owner, cadence/freshness, two-pass empty evidence, committed lease, typed outcomes, and recovery remain bound. |
| H-2 distributed safety rescan | **Closed.** Epoch/index ownership, finite enumeration, bounded fenced coordinator, activation barrier, and `RescanPending` behavior remain executable. |
| H-3 human-only Approvers | **Closed.** Configuration and runtime resolution require Parties-authoritative human type/liveness and stable actor evidence. |
| H-4 ledger lifetime conflation | **Closed.** Rate, open-interaction, Budget, and capacity have distinct owners, identities, lifetimes, decisions, and settlement. |
| H-5 human identity/separation | **Closed.** Stable `AuthenticatedHumanActorId` spans Party-bearing User, Administrator, and Platform evidence; Workflow remains non-human. |
| H-6 proposal-index crash consistency | **Closed.** Interaction truth, same-append source-revision outbox, directory high-water, reconciliation, and removal fixed point are explicit. |
| H-7 indivisible Conversations dependency | **Closed.** Core `EXT-CONV-AI-1` and optional `EXT-CONV-RETRACTION-1` are independently governed. |
| H-8 trusted-envelope security | **Closed.** Canonical authenticated fields, logical/delivery identity split, platform-wide replay owner, key/time lifecycle, ACLs, and lost-ack recovery remain bound. |
| H-9 export ownership/lifecycle/signature | **Closed subject to the surfaced Product lifecycle decision.** Immutable store/index, partial-output inventory, fence commit, ES256/JCS manifest, direct key delivery, cleanup, all-copy recovery, and export-free safe path are explicit. |
| H-10 Dapr current truth | **Closed as current-reality classification.** Parent-authoritative Client/ASP.NET `1.18.5`, non-authoritative checkout `1.18.7`, and future Workflow adoption are distinguished. |
| H-11 public parity overclaim | **Closed.** Target completion vocabulary and current delivery debt remain separate. |
| H-12 tracker/evidence contradiction | **Closed as surfaced delivery-governance debt.** The owner decision remains Open without rewriting tracker history or granting architecture authority. |

## AD IDs, Memlog, Open Decisions, Sources, And Repository Reality

- The spine contains exactly one each of AD-1 through AD-31. No AD was renumbered, reused, deleted, or silently superseded.
- The memlog remains append-only. Its tail records the initial/successor admission branches, continuous gap-chain binding, guard-revision barrier/containment issuance, non-expiring capability lifetime, and post-terminal key-verification clarification without rewriting earlier decisions.
- Unresolved Product choices remain named and fail closed: armed hold/deletion precedence, hold-prepare cancellation, operator cancellation/nonterminal treatment, export lifecycle, rate/concurrency consumption, Dapr security exception/upgrade, tracking exception, historical-safety treatment, automatic retraction, instruction protection, legacy plaintext, initial output-safety status, class/range scope, human exact-Conversation scope, and recorder scope. This review selects none.
- All external dependency records remain `Uncommitted`; `RQ-1` remains NOT READY. The current repository does not implement the target directory, leases, admission/content guards, scope guard, decision catalog, safety epoch, ledgers, protection batches, export store, or deletion completion barrier. Existing absence is delivery debt, not authority to weaken the target or a reason to reclassify C-R20-1 as implementation work.
- The two absent historical v18 review declarations cause M-R20-3. The five v20 paths are concurrent anticipated outputs and were not treated as missing sources.

## Whole Good-Spine Checklist

| Dimension | Result |
| --- | --- |
| Decisions prevent downstream divergence | **Fail at one boundary.** Most rules fix ownership, identities, ordering, result recovery, and negative outcomes; final completion still permits two literal owner-order implementations. |
| Implementability and internal consistency | **Fail at C-R20-1.** The completion row promises no hidden late write without an operation that can establish that fact atomically. All three v19 repair protocols are otherwise implementable. |
| Ownership and concurrency | **Fail only at completion.** Directory/effect cuts, ordinal partitions, gap chains, hold/seal, containment issuance, ledgers, export commit, and decision publication have owners and revision rules. Completion alone verifies one owner and appends to another. |
| Recovery, replay, and lost acknowledgement | **Pass except final terminalization.** Exact lookup and immutable identities cover the repaired batch and recut flows; the missing completion effect/result also means no exact guard-side terminal lookup exists. |
| Security, tenancy, and data loss | **Fail Critical.** Tenant/scope/capability boundaries are otherwise closed, but a successful erasure claim can race a newly accepted in-scope write or effect authorization. |
| PRD, epic, register, and convention traceability | **Pass with Medium governance/source tail.** Mandatory behaviors and open decisions reconcile; literal assumption dates and two historical source artifacts remain incomplete. |
| Architecture versus implementation debt | **Pass.** The spine names absent current mechanisms as delivery debt and does not claim the repository ships the target. C-R20-1 is a target-contract defect, not missing code. |
| Product decision discipline | **Pass.** Every unresolved outcome remains named, owned, scoped, and restrictive; no Product choice was invented by this review. |
| Brownfield/current reality | **Pass with Low maintenance tail.** Root authority, dirty checkout evidence, absent target mechanics, Dapr exposure, tracker inconsistency, and package lag are distinguished. |
| Sources and mechanics | **Pass lint/IDs; Medium source debt.** Deterministic lint and AD scan pass. Two declared historical reviewer sources are absent. |

## Gate Result

The v20 good-spine rubric gate is **FAIL** because the Critical count is nonzero. Deterministic lint is **PASS** with zero findings. Correct C-R20-1 before a fresh complete gate; do not treat closure of the three v19 High findings as closure of the final completion race.
