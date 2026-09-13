---
name: Hexalith Agents good-spine rubric review v21
type: architecture-spine-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-13
intent: review-read-only
lens: bmad-architecture good-spine rubric walker
verdict: fail
critical: 0
high: 1
medium: 2
low: 1
lint_ok: true
---

# Good-Spine Rubric Reviewer Gate — v21

## Verdict

**FAIL — 0 Critical, 1 High, 2 Medium, 1 Low.** V21 closes the v20 post-start-hold, acceptance-attribution, same-key-target coverage, and final-completion defects. A fresh complete rubric walk nevertheless finds one unresolved destructive-capability race: emergency capability re-attestation reads protection-owner status and then commits replacement on `GovernanceScopeGuard`, while the old-attestation consume reads the guard and then commits at the protection owner. Neither path reserves or conditionally changes both owners. Both can therefore succeed in one interleaving, contradicting the promised “old credential revoked before consume / re-attest only while unconsumed” contract. The immutable batch and manifest prevent scope expansion or a second physical erasure, so this is High rather than Critical.

## Frozen Inputs And Method

I read the complete repository and BMad architecture reviewer-gate instructions, current spine and conventions, architecture memlog, authoritative validation report, bound PRD, Epics 5–8, both registers, declared local sources, root manifests/gitlinks, and focused current source/submodule evidence. I re-walked the whole good-spine rubric rather than using prior findings as the checklist: decision enforceability; ownership and independently-buildable-unit convergence; authorization/effect/result ordering; concurrency, crash, replay, lost acknowledgement, migration and restore; security, tenancy and irreversible data boundaries; PRD/epic/register traceability; unresolved Product authority; present repository ratification; source integrity; architecture versus delivery debt; and mechanical consistency.

The five v21 report paths declared in the spine frontmatter were treated as concurrent anticipated gate outputs, not missing historical sources. Every earlier declared local source exists. The supplied frozen hashes matched before review:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `0c62966b3d91a7368d032a0f31f669d7fb0a54628e3e1675cc15e78080585448` |
| `IMPLEMENTATION-CONVENTIONS.md` | `c53767c011ce2d4312cfad5e565dc3851babfb9bd609b965017dfb352ec203cb` |
| architecture `.memlog.md` | `d379abad04e8f2c7d2a343c5438bb96fcb7d9c6ed7b7429230c6279aea9ae985` |
| bound `prd.md` | `a6e2fd051206c8547533558978c91cf9466ce05cb29997e60c16b6921b3d51a3` |
| `epics.md` | `41beb3fd70cd7f61f058fd62c5835782daee7ce89192a351d09d30456f0bb506` |
| `external-dependency-register.md` | `2da84f01e03e2966b2d2d56b354627c9cfc232648c74e5981f5c94c8b4537377` |
| `launch-readiness-register.md` | `a85a1070e8837ff3ffaaddff69c38c6702f792945a5054430cba413e0751c606` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

Focused repository inspection was read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`. The parent-authoritative Builds gitlink remains `a32cb422`; the internally clean checkout remains `cf52f74`. No reviewed source, planning artifact, memlog, or submodule was modified by this review.

## Deterministic Linter And AD Identity

Command:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: **PASS**, `ok: true`, `total_findings: 0`. A separate heading scan found exactly one each of AD-1 through AD-31, maximum AD-31, with no gap, duplicate, deletion, reuse, or renumbering.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 1 |
| Medium | 2 |
| Low | 1 |

## Critical

None.

## High

### H-R21-1 — Capability re-attestation and old-attestation consumption have no common linearization owner

**Classification:** target-architecture destructive-capability security/recovery defect; not implementation debt and not an unresolved Product choice.

**Disposition:** add an owner-linearized attestation-replacement-versus-consume protocol before implementation.

**Evidence.** AD-2 requires an emergency compromise to make an old key version unacceptable for an unconsumed effect, and permits re-attestation only after the protection owner proves the immutable batch unconsumed; the guard then revokes the old attestation and activates exactly one replacement (`ARCHITECTURE-SPINE.md:250,254-256`). The matrix implements that as two different read-then-write directions:

- `AuthorizeDeletionBatchCapabilityReattestation` obtains `ProtectionOwnerExactLookupProvesBatchUnconsumedAndNotRevoked`, and `CommitDeletionBatchCapabilityReattestationEffect` again requires `ProtectionOwnerStillUnconsumedExact` before appending the replacement on `GovernanceScopeGuard` (`launch-readiness-register.md:331,333`). Neither row holds a protection-owner reservation or supplies a condition that the guard append can atomically enforce at that owner.
- `DestroyDeletionDekManifestEffect` performs a linearizable guard lookup proving the old attestation active and its dispatch exact, then `EXT-PROTECTION-1` atomically consumes the batch at its own owner (`launch-readiness-register.md:349-353`). It does not reserve/commit consumption on the guard before the local irreversible transaction.
- The external contract repeats online guard lookup plus guard-side re-attestation, but names no prepare token, lock, conditional status transition, or single owner shared by those two operations (`external-dependency-register.md:237,240,245`; Story 8.3 at `epics.md:3036-3037`). Requiring race tests does not itself define the result they must enforce.

**Executable divergence.** Let batch B have active old attestation A1 and a valid A1 dispatch. A re-attestation worker first receives “B is unconsumed” from the protection owner. Before it appends the guard replacement, a protection worker receives an exact guard lookup showing A1 active and dispatched. The re-attestation worker then commits A1 revoked/A2 active on the guard; the protection worker then atomically consumes B under A1 at the protection owner. Both operations satisfy their local expected revision and the literal preconditions. There is no legal sequential order for the two successes: if consume linearizes first, re-attestation was required to return the consumed vector rather than activate A2; if replacement linearizes first, A1 consumption was required to reject. A successor dispatch may consequently exist for an already-consumed batch, and implementations can disagree whether to report A1 consumed, A2 active, replacement stale, or batch terminal.

**Impact.** The target cannot prove its stated emergency-compromise property or a unique credential status across guard, protection, recovery, migration, and restore. The batch id and manifest remain immutable and exact retry returns the same protection vector, so this race does not authorize another tenant, a wider target, or a second physical destruction; that limits severity to High.

**Required correction.** Give consume-versus-compromise/re-attestation one executable linearization protocol. For example, reserve/fence the exact batch and old attestation at `EXT-PROTECTION-1` before signing/replacement, condition the guard replacement on that durable reservation, and commit/release the protection reservation from the exact guard result; or make the protection owner the conditional authority for attestation replacement and mirror its outcome to the guard. Whichever design is chosen must define crash/lost-ack recovery, unknown status, stale replacement, dispatch invalidation, migration/restore preservation, and exactly one outcome when compromise/re-attestation races consumption before, at, and after the boundary. It must not mint a new batch, widen the manifest, or choose a Product hold outcome.

## Medium

### M-R21-1 — Architecture-owned `RQ-1` assumptions still lack literal retirement dates

**Classification:** planning-governance debt; safely blocking, not a target runtime defect.

PRD FR-28 requires every Architecture-owned assumption affecting `RQ-1` to carry a co-owner-approved literal calendar retirement date. `ARCH-A-1`, `ARCH-A-2`, `ARCH-A-3`, the remaining test-stack portion of `ARCH-A-4`, `ARCH-A-6`, `ARCH-A-7`, `ARCH-A-8`, `ARCH-A-11`, `ARCH-A-12`, and `ARCH-A-14` remain `Unscheduled` or milestone-only (`ARCHITECTURE-SPINE.md:1355-1377`). The spine correctly emits `UnretiredAssumption`, so no unsafe readiness pass results. Obtain approved dates; Architecture must not invent them.

### M-R21-2 — `PostingPending` still lacks one concrete timeout authority

**Classification:** unresolved Architecture/Product parameter; safely surfaced.

AD-5 fixes only a lower bound tied to the Conversations posting seam, while `ARCH-A-14` confirms that neither the PRD nor the spine selects a concrete duration (`ARCHITECTURE-SPINE.md:266,1376`). Two conforming workflow/recovery units can therefore persist different deadlines. Bind one literal duration or one exact versioned seam/profile value and obtain Product confirmation before the posting/recovery story; this review selects none.

## Low

### L-R21-1 — Root bUnit remains behind the current Builds-checkout catalog

**Classification:** implementation/build maintenance, not an architecture defect.

Root authority pins bUnit `2.9.0`, while the currently checked-out Builds catalog pins `2.10.3` (`Directory.Packages.props:26`; `references/Hexalith.Builds/Props/Directory.Packages.props:317`). The spine accurately reports the root/checkout distinction and assigns test-stack alignment to Story 5.6.

## V20 Critical/High Closure Audit

| V20 finding | V21 disposition |
| --- | --- |
| Rubric `C-R20-1` — final completion was not serialized with late admission/content mutation | **Closed.** `AuthorizeDeletionCompletionBarrier` freezes the complete manifest and observed guard revision; `CommitDeletionCompletionBarrierEffect` conditionally appends `DeletionCompletionSealed` on the guard; every relevant intervening guard mutation returns authenticated stale, and only exact guard lookup may be mirrored as complete (`ARCHITECTURE-SPINE.md:258,505`; matrix 356-359; Story 8.3 line 3045). |
| Adversarial/verified `C-v20-1` / `VC20-C1` — post-start hold could be ignored by new containment authority | **Closed.** A post-seal attempt now appends a durable guard fact and advances its revision. Open/missing/mismatched disposition blocks containment issue/dispatch/consume, accepted-batch dispatch, purge, and completion. Only an exact deletion-allowed outcome may bind a later step; hold-allowed grants no authority (`ARCHITECTURE-SPINE.md:248-250`; matrix 275-276, 324, 328, 349-355). |
| Adversarial `H-v20-1` — no capability signer/trust/rotation/compromise contract | **Static trust and ordinary lifecycle are closed; emergency replacement atomicity remains open as H-R21-1.** Closed JCS/ES256 fields, issuer/audience, secrets-owned key family, anchors, routine retention, compromise enumeration, one-active re-attestation, and online guard validation are explicit. The remaining defect is the cross-owner consume/re-attest race, not missing cryptographic metadata. |
| Adversarial `H-v20-2` — stale caller ordinal could contaminate the wrong partition | **Closed.** The EventStore guard assigns acceptance-time ordinal/high-water and atomically contaminates the then-current partition; caller ordinal is diagnostic only, successor installation serializes, and missing receipt is integrity-unattributable (`ARCHITECTURE-SPINE.md:246`; matrix 321; Story 8.3 line 3015). |
| Adversarial `H-v20-3` — same DEK alias could receive incompatible duplicate batches | **Closed.** Coverage is keyed by complete tenant/interaction/alias, every resource links to one original target/vector, `AlreadyDestroyedByBatch` is exact-target-only, and only an uncovered target receives a new ordinal/capability (`ARCHITECTURE-SPINE.md:252`; matrix 323, 352-353; external register line 245). |

The v20 Medium migration-list inconsistency is also closed: AD-30 now explicitly incorporates AD-17 and enumerates acceptance receipts, gap chains, compare/stale outcomes, post-start holds, coverage, attestations, dispatches, and the completion seal as mandatory preservation evidence (`ARCHITECTURE-SPINE.md:583`). Historical v18 reviewer paths that did not exist are no longer declared. Current artifact metadata is consistently dated 2026-09-13.

## Authoritative 2026-09-12 Critical/High Closure Audit

| Authoritative finding | V21 disposition |
| --- | --- |
| C-1 weaker safety evaluation | **Closed.** Snapshot and every then-current applicable policy are conjunctive; optimization requires tested equivalence. |
| C-2 hold/deletion exclusion | **Closed for hold ordering, inventory, batches, post-start containment, and guard-side completion.** H-R21-1 is a later credential-status race over the same immutable approved batch, not a reopened scope/hold exclusion gap. |
| C-3 bootstrap/matrix deadlock | **Closed.** Target-aware bootstrap and repair variants retain explicit scopes, omitted circular gates, and closed direct preconditions. |
| H-1 scheduled Approver re-check | **Closed.** Durable single-flight ownership, cadence/freshness, two-pass evidence, committed Approver-resolution lease, typed results, and recovery remain bound. |
| H-2 distributed safety rescan | **Closed.** Epoch/index ownership, finite cohort, bounded fenced coordination, activation, and `RescanPending` are explicit. |
| H-3 human-only Approvers | **Closed.** Configuration and runtime resolution require Parties-authoritative human type, liveness, and actor binding. |
| H-4 ledger lifetime conflation | **Closed.** Rate, open-interaction, Budget, and capacity have distinct owners, identities, lifetimes, decisions, and settlement. |
| H-5 human identity/separation | **Closed.** Stable `AuthenticatedHumanActorId` spans Party-bearing User, Administrator, and Platform evidence; Workflow remains non-human. |
| H-6 proposal-index crash consistency | **Closed.** Interaction truth, source-revision outboxes, directory high-water, leases, acknowledgement, and reconciliation are explicit. |
| H-7 indivisible Conversations dependency | **Closed.** Core `EXT-CONV-AI-1` and optional `EXT-CONV-RETRACTION-1` are independently governed. |
| H-8 trusted-envelope security | **Closed.** Canonical authenticated fields, logical/delivery split, replay owner, time/key lifecycle, ACL confinement, spool, and lost-ack recovery remain bound. |
| H-9 export ownership/lifecycle/signature | **Closed subject to the surfaced Product lifecycle decision.** Store/index ownership, JCS/ES256 manifest, direct key delivery, hold/expiry fencing, cleanup, restore, and all-copy completion are explicit. |
| H-10 Dapr current truth | **Closed as current-reality classification.** Parent-authoritative Client/ASP.NET `1.18.5`, non-authoritative checkout `1.18.7`, and future Workflow adoption remain distinguished. |
| H-11 public parity overclaim | **Closed.** Required-completion vocabulary and current delivery debt remain separate. |
| H-12 tracker/evidence contradiction | **Closed as surfaced delivery-governance debt.** The owner decision remains Open without rewriting tracker history or granting architecture authority. |

## AD IDs, Memlog, Decisions, Sources, And Repository Reality

- The spine contains exactly one each of AD-1 through AD-31. No AD was renumbered, reused, deleted, or silently superseded.
- The memlog remains append-only and records the v21 acceptance-attribution, post-start-hold, dispatch, capability-trust, target-coverage, and completion decisions after the earlier history.
- Product-owned decisions remain named, owned, scoped, and fail-closed: hold/deletion precedence, hold-prepare cancellation, operator cancellation/nonterminal treatment, export lifecycle, rate/concurrency consumption, Dapr security, sprint evidence, historical safety, automatic retraction, instruction protection, legacy plaintext, output-safety status, class/range scope, human exact-Conversation scope, and recorder scope. This review selects none. H-R21-1 is mechanical security ordering and needs no Product outcome.
- All twelve external dependencies remain `Uncommitted`; every consumer remains blocked from `ready-for-dev`, and `RQ-1` remains NOT READY. Current code has none of the new directory, lease, guard, signed-batch, completion, safety-epoch, decision-catalog, export-store, or ledger target mechanics. Those absences are delivery debt, not authority to weaken the target and not the cause of H-R21-1.
- Every declared historical local source exists. Only the five deliberately anticipated v21 gate paths were absent at review start.

## Whole Good-Spine Checklist

| Dimension | Result |
| --- | --- |
| Real divergence points and enforceable AD rules | **Fail High at H-R21-1.** Most rules identify owners, identities, expected revisions, result classes, and recovery; capability replacement and old-credential consumption can still both win. |
| Implementability and internal consistency | **Fail High.** The local operations are implementable, but their combined promised one-active/unconsumed invariant has no executable distributed ordering. |
| Ownership and concurrency | **Fail High only at re-attestation versus consume.** Directory cuts, ordinal ledgers, content bindings, hold/seal, batch dispatch, target coverage, and terminal completion otherwise have explicit owners and compare rules. |
| Recovery, replay, lost acknowledgement, migration, restore | **Pass except H-R21-1.** Exact lookup and immutable identities cover the other flows; ambiguous replacement/consume ordering would produce incompatible restore state. |
| Security, tenancy, and data loss | **Fail High, not Critical.** Emergency revocation semantics can be violated, but the immutable batch/manifest and exact terminal vector prevent widening target scope or repeating physical destruction. Tenant boundaries otherwise fail closed. |
| PRD, epic, register, and convention traceability | **Pass with two Medium planning parameters.** V21 mechanics are propagated; assumption dates and posting timeout remain safely unresolved. |
| Architecture versus implementation debt | **Pass.** H-R21-1 is a target-contract defect. Missing current mechanisms and bUnit lag are explicitly delivery/build debt. |
| Product decision discipline | **Pass.** Open outcomes remain surfaced and restrictive; no Product choice was invented. |
| Brownfield/current reality | **Pass with Low maintenance tail.** Root authority, non-authoritative checkout evidence, Dapr exposure, absent target mechanisms, and package drift are distinguished. |
| Sources and mechanics | **Pass.** Lint and AD identity scan pass; historical local sources resolve; anticipated v21 outputs are not misclassified as inputs. |

## Gate Result

The v21 good-spine rubric gate is **FAIL** because the High count is nonzero. Deterministic lint is **PASS** with zero findings. Correct H-R21-1, then rerun the complete reviewer gate; do not weaken emergency compromise behavior or choose an unresolved Product outcome as the repair.
