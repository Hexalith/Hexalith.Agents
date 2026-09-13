---
review: verified-current-v20
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-13
verdict: FAIL
critical: 1
high: 0
medium: 5
low: 1
lint_ok: true
---

# Verified-Current / Bound-PRD Reviewer Gate — v20

## Verdict

**FAIL — 1 Critical, 0 High, 5 Medium, and 1 Low.** The v20 package closes the five v19 Critical/High mechanics requested for recheck: barrier authorization now carries the executable scope-guard compare revision and a durable stale branch; repeated recuts bind through a complete gap-free chain from the latest installed content binding; the ordinal-one matrix branch is explicitly grouped without successor-only evidence; accepted and containment capabilities are non-expiring with retained verification state; and containment capability issuance is an explicit guard-owned authorize/effect/result path. A fresh whole-path review nevertheless found one unresolved legal-hold branch at that new containment seam: after `DestructionSealed`, the hold path says only an approved post-start outcome may advance, but containment-batch authorization/issuance can still mint and consume a new irreversible capability without proving that outcome. That is an architecture/Product-authority contradiction, not current implementation debt.

## Frozen Inputs And Integrity

The reviewed inputs matched the supplied SHA-256 values before this report was written:

| Input | SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `ec53b5116c56af2bb87c52002b1906d60189d2e738cbc038d165a11aeeceb0fd` |
| `IMPLEMENTATION-CONVENTIONS.md` | `6f2503b8e31a357ee0d91b0bd754e067e7f364590b2ff741fa9a639d2856ad80` |
| `.memlog.md` | `e9da3dce567da7458dc499ca3c6a8b55891e864dafda09d6ef9a4084852d4510` |
| bound `prd.md` | `2653ad3e683ba2d4901f4e29c073c29603b6110be079cfc7c3c92b53a6c323a9` |
| `epics.md` | `533b33d9eaebd5a2ef3cdb6901237dff41a41c2ac44ca1326a69645e25c4c0ef` |
| `external-dependency-register.md` | `54fa2073f583bdaa444e3b193e188963547482a04c5e33fbe1fc9b3fb960a734` |
| `launch-readiness-register.md` | `ecb205005322a91aae24718c643861231d730f79a5fdb1676f42bb77ccc1e6f9` |
| authoritative `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

The repository was inspected read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`. No submodule was initialized, updated, or modified by this review.

## Method

I read the repository instructions, complete BMad architecture reviewer-gate instructions, current spine and conventions, authoritative validation report, bound PRD, active Epics 5–8 map, both registers, current memlog tail, root manifests/gitlinks, and relevant current source. I reconstructed the v19 fixes as separate API/workflow, `ProtectionFence`, `GovernanceScopeGuard`, EventStore-guard, protection-owner, hold, deletion, migration/restore, and exact-outcome-recovery units; then re-walked authoritative C-1..C-3 and H-1..H-12, PRD/epic/register authority, open decisions, current-versus-target claims, dependency consumers, and source existence.

The deterministic linter command was:

```text
python3 .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: **PASS**, `ok: true`, zero findings.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 0 |
| Medium | 5 |
| Low | 1 |

## Critical

### VC20-C1 — Post-seal containment can cross a post-start hold whose Product outcome is still unresolved

**Classification:** target-architecture legal-hold/deletion authority defect; not implementation debt and not permission to select the Product outcome.

**Evidence.** The hold contract says a registration that loses to an existing `DestructionSealed` returns `DestructionAlreadySealed`, and that this result may advance only under the approved post-start outcome (`ARCHITECTURE-SPINE.md:481`; `launch-readiness-register.md:275-277`; `epics.md:2834-2840`). The blocking-decision row scopes `OD-HOLD-DELETION-PRECEDENCE-1` to an armed but unsealed contention and gives no effective post-start outcome at this snapshot (`ARCHITECTURE-SPINE.md:1262`). The new containment path, however, requires only the seal, content-violation/ordinal/manifest evidence, content-guard binding, an observed `GovernanceScopeGuard` revision, no admission compromise, and batch idempotency. Neither authorization nor guard commit requires absence of a post-seal `DestructionAlreadySealed` hold result or the exact approved post-start disposition (`launch-readiness-register.md:323-327`). The protection dispatch likewise checks the issued batch and compromise state, not post-start hold authority (`launch-readiness-register.md:342-346`).

**Executable failure.** Deletion seals at guard revision R. A matching hold then attempts registration; because the seal exists, it receives `DestructionAlreadySealed` and does not append a restrictive contender to the guard. A later authoritative same-predicate content violation freezes containment ordinal N. One literal implementation follows the containment rows and issues/consumes the new batch at R; another follows the hold rule and blocks until an approved post-start outcome exists. The first can irreversibly destroy the new target while the Product-owned hold result is unresolved. A ProtectionFence mirror conflict does not close the race: the containment authorization can be retried at the newer fence revision, and its listed preconditions still do not consume or reject the hold result.

**Impact.** The newly added capability is fresh irreversible authority for a target outside the accepted manifest. Treating the earlier seal as an implicit decision over this later target silently chooses the deletion-wins post-start Product outcome; blocking it chooses the opposite behavior. Because the wrong choice can destroy content that a pending legal hold would preserve, this is Critical.

**Required correction.** Do not choose the post-start Product result. Make every post-seal hold attempt a durable scope-guard fact that changes the compare revision, or provide an equivalent guard-owned pending-outcome fact. Require `AuthorizeDeletionContainmentBatch` and `CommitDeletionContainmentBatchEffect` to prove either no overlapping post-start hold fact/result exists or the exact approved post-start deletion-allowed disposition is recorded and consumed for that containment ordinal. While the decision is Open/missing/mismatched, no new containment capability may be issued or consumed. Bind exact lookup, duplicate/lost acknowledgement, hold-versus-containment before/at/after races, migration/restore preservation, and completion to the same fact. If post-start semantics are not part of `OD-HOLD-DELETION-PRECEDENCE-1`, surface a separately owned decision; do not extend its outcome implicitly.

## High

None.

## v19 Critical/High Correction Audit

| v19 issue | v20 disposition |
| --- | --- |
| Barrier authorization lacked an executable guard compare revision/stale result | **Closed.** AD-2/AD-22, conventions, matrix rows 338–341, PRD §9, Story 8.3, and `EXT-HOST-1` now persist the exact observed `GovernanceScopeGuard` revision, conditionally append at it, return authenticated stale on any intervening mutation, and require that result before reauthorization. |
| Repeated recuts could outrun predecessor binding | **Closed.** Matrix rows 318–321 and the corresponding spine/convention/story/dependency text start from the latest actually installed binding, require a complete ordered gap-free invalidation/tokenless chain, and give an overtaken authorization terminal `ObsoleteBindingAuthorization` without retroactively binding an invalid token. |
| Ordinal-one matrix branch required successor containment evidence | **Closed.** Row 296 groups `InitialAdmissionFenceBranch(NoPrior..., NoContainmentReceiptExpected, NoPrior...Exists)` separately from the full successor branch. |
| Sealed batch could expire without legal recovery | **Closed.** Accepted and containment capabilities are expressly non-expiring, revocable, single-use, have no expiry/renewal branch, preserve their key version for the complete batch/outcome retention, and recover by exact immutable identity across outage/migration/restore. |
| Post-seal containment lacked guard-owned issuance | **Closed for ownership, identity, concurrency, and recovery.** Rows 323–327 now allocate one ProtectionFence ordinal and singleton manifest, authorize at an observed guard revision, conditionally record one guard-owned identity, return issued-or-stale, mirror the exact result, and require it before protection consumption. VC20-C1 is a fresh authority interaction at this seam, not a failure to add the requested issuer mechanics. |

## Authoritative 2026-09-12 Critical/High Closure Audit

| Authoritative finding | Current disposition |
| --- | --- |
| `C-1` weaker safety evaluation | **Closed.** AD-20 requires snapshot-plus-current conjunctive evaluation at retry, regeneration, approval, and pre-post; dominance is allowed only as tested equivalence. |
| `C-2` hold/deletion exclusion | **Closed on the ordinary accepted-manifest path; reopened at the later containment branch by VC20-C1.** Shared fence/guard ordering, complete preparation, all-or-none accepted batch, and restrictive completion remain sound. The new capability path does not yet respect the explicitly unresolved post-start hold result. |
| `C-3` matrix bootstrap/scope deadlock | **Closed.** Matrix v4 binds explicit target scope plus closed direct bootstrap/repair evidence, including the now-correct ordinal-one deletion branch. |
| `H-1` scheduled Approver lifecycle | **Closed.** Durable single-flight scheduling, cadence/freshness, two-pass empty proof, state-specific transitions, unavailable recovery, and lease-before-read are bound. |
| `H-2` safety rescan | **Closed.** Epoch/index ownership, finite manifests, bounded fenced coordination, `RescanPending`, invalidation, and recovery are explicit. |
| `H-3` human-only Approvers | **Closed.** Parties-authoritative human classification, liveness, and actor binding are required at configuration and resolution. |
| `H-4` ledger lifetime conflation | **Closed.** Rolling rate, nonterminal open-interaction, and UTC-month Budget owners/lifetimes are separate; the unresolved concurrency-rejection consumption choice remains safely isolated. |
| `H-5` human identity | **Closed.** Stable `AuthenticatedHumanActorId` and principal-specific authority evidence bind every human principal and second-party comparison. |
| `H-6` proposal/index crash consistency | **Closed.** Interaction truth, protected/source-revision outboxes, directory high-waters, effect leases, reconciliation, and deletion owner cuts are explicit. |
| `H-7` indivisible Conversations dependency | **Closed.** Core `EXT-CONV-AI-1` and conditional `EXT-CONV-RETRACTION-1` have independent authority. |
| `H-8` trusted-envelope cryptography/replay | **Closed.** Canonical authenticated fields, logical/delivery identity split, replay owner, time/key lifecycle, ACL confinement, security spool, and lost-ack recovery are bound. |
| `H-9` export bytes/lifecycle/signature | **Closed subject to the surfaced lifecycle decision.** Store/index ownership, preparation inventory, fence commit, ES256/JCS manifest, direct key delivery, cleanup, restore, and all-copy purge are explicit. |
| `H-10` Dapr exposure | **Closed as current-reality classification.** Parent-authoritative Client/ASP.NET `1.18.5`, non-authoritative checkout `1.18.7`, and future Workflow adoption are distinct. |
| `H-11` public contracts falsely current | **Closed.** Required-completion vocabulary is separated from the explicit current delivery-debt ledger. |
| `H-12` sprint/dependency contradiction | **Closed as surfaced delivery-governance debt.** `OD-SPRINT-5.1-5.2-1` keeps the discrepancy visible and does not grant architecture authority to rewrite history. |

## Medium

### VC20-M1 — Architecture-owned `RQ-1` assumptions still lack literal calendar retirement dates

PRD FR-28 item 9 and §8.1 require each Architecture-owned `RQ-1` row to carry a co-owner-approved literal date. `ARCH-A-1`, `ARCH-A-2`, `ARCH-A-3`, the remaining test-stack part of `ARCH-A-4`, `ARCH-A-6`, `ARCH-A-7`, `ARCH-A-8`, `ARCH-A-11`, `ARCH-A-12`, and `ARCH-A-14` remain `Unscheduled` or milestone-only (`ARCHITECTURE-SPINE.md:1327-1346`). The spine correctly emits `UnretiredAssumption`, so this is fail-closed planning-governance debt. Obtain approved dates; Architecture must not invent them.

### VC20-M2 — `PostingPending` still lacks one concrete timeout authority

FR-18 requires a stored deadline no shorter than the posting-seam timeout, but AD-5 and `ARCH-A-14` still select neither a literal duration nor one exact versioned seam/profile field as the authority (`prd.md:438`; `ARCHITECTURE-SPINE.md:260,1345`). Bind one source and obtain Product confirmation before the posting/recovery story is ready.

### VC20-M3 — Cross-artifact provenance metadata predates the actual authority snapshot

The spine says `updated: 2026-09-13`, while the bound PRD remains `updated: 2026-09-12` and §8.1 still calls the current spine `updated: 2026-09-12`. Both registers also remain `updated: 2026-09-12` although their frozen v20 bodies contain the new compare-token/gap-chain/non-expiring/containment-issuance contracts. Correct metadata in a coordinated artifact pass; no Product result needs to change.

### VC20-M4 — Two historical v18 reports named as immutable sources do not exist

The spine source list names `reviews/review-2026-09-12-security-data-integrity-v18.md` and `reviews/review-2026-09-12-brownfield-drift-v18.md`, but neither path exists. Remove those citations or materialize the exact immutable reports. The five v20 paths are concurrent anticipated reviewer outputs—including this report—and are not counted as missing historical sources.

### VC20-M5 — The SDK security row still overstates the advisory's version mapping

The Stack calls SDK `10.0.401` “the current Windows fixed floor” for `CVE-2026-69522`. Microsoft's advisory identifies the vulnerable component as `Microsoft.DiaSymReader.Native` and its patched package floor as `18.9.0-beta1.26405.2`; it does not define SDK `10.0.401` as that component floor. The official .NET 10 index confirms `10.0.12` is the current September release, and repository project/package/assets evidence inspected for this gate contains no `Microsoft.DiaSymReader.Native` reference. The scoped graph is therefore not shown vulnerable, but the wording and source chain remain imprecise. Reword the row as the current September SDK used with the inspected Windows graph, cite the [official advisory](https://github.com/dotnet/announcements/issues/439) alongside the [official .NET 10 release index](https://github.com/dotnet/core/blob/main/release-notes/10.0/README.md), and keep the independent component-floor rule.

## Low

### VC20-L1 — Root bUnit remains behind the current checkout catalog/current stable release

Root `Directory.Packages.props` pins bUnit `2.9.0`; the parent-authoritative Builds gitlink also pins `2.9.0`, while the separate clean Builds checkout and the official package feed expose `2.10.3`. The spine's current-root statement is truthful and Story 5.6 owns alignment. This is build maintenance, not an architecture contradiction.

## Verified Technology And Repository Reality

- Root authority remains commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`; exact gitlinks remain Builds `a32cb422`, Conversations `73bcee6f`, EventStore `ce9e779a`, FrontComposer `053b2008`, Memories `3644ef63`, Parties `fa423985`, and Tenants `2fac1839`.
- The checked-out Builds, Conversations, EventStore, FrontComposer, and Memories submodule HEADs differ from the root gitlinks; Parties and Tenants match. Those checkout revisions are implementation evidence, not parent authority.
- Root `global.json` pins SDK `10.0.401` with `latestPatch`; root build properties set `net10.0` and C# 14. The parent-authoritative Builds catalog pins Dapr Client/ASP.NET/Workflow `1.18.5`; the separate Builds checkout pins `1.18.7`. Official NuGet package indexes still expose `1.18.7` as the stable package-family version and only `1.19.0-preview.*` afterward.
- Current Agents source imports EventStore Client/DomainService and calls `AddDaprClient`; no Agents project references Dapr Workflow or Microsoft Agent Framework. The target Workflow/provider choices remain unselected/unimplemented exactly as the spine states.
- Current source contains none of the target directory/migration fences, three ledgers, safety epoch, decision catalog, scope guard, manifest-batch deletion, export store, or trusted-envelope replay protocol. Existing `SafetyFailed` and combined approval/posting behavior remain explicitly classified as implementation debt rather than architecture authority.
- All twelve external dependency records remain `Uncommitted` with `TBD` target/date/verification command. Their direct consumers include Story 6.1 for directory/migration authority, Story 8.1 for hold registration/release, and Story 8.3 for both deletion origins, recut/gap-chain mechanics, guard compare/stale recovery, non-expiring batches, containment issuance, and all-copy completion. No story or `RQ-1` path is made ready by the document update.

## AD IDs, Memlog, Open Decisions, And Debt Separation

The spine contains exactly one each of `AD-1` through `AD-31`, with no duplicate, missing, or renumbered ID. The memlog remains append-only and records the five v20 mechanics after the prior v19 decisions. Target contracts and current repository gaps remain separated in the delivery-debt table.

Armed hold precedence, hold-prepare cancellation, operator deletion cancellation/nonterminal treatment, export lifecycle, rate/concurrency consumption, Dapr security disposition, historical safety, automatic retraction, instruction protection, legacy plaintext, initial output-safety status, class/range scope, human exact-Conversation scope, recorder scope, and sprint evidence remain named with owners and restrictive safe states. VC20-C1 requires the post-start hold/containment boundary to consume a named approved outcome; it does not select that outcome.

## Gate Conclusion

The complete frozen v20 verified-current/bound-PRD gate is **FAIL**. Final counts: **Critical 1, High 0, Medium 5, Low 1**. Deterministic lint passed with zero findings. The gate can pass only after VC20-C1 is mechanically closed without inventing the post-start Product result, followed by a fresh complete review.
