---
review: verified-current-v21
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-13
verdict: FAIL
critical: 1
high: 0
medium: 3
low: 1
lint_ok: true
---

# Verified-Current / Bound-PRD Reviewer Gate — v21

## Verdict

**FAIL — 1 Critical, 0 High, 3 Medium, and 1 Low.** The v21 mechanics close every v20 Critical/High failure at the executable guard, batch, protection, and completion rows: acceptance attribution is assigned at the write linearization instant; post-start holds are durable guard facts; batch dispatch orders hold/compromise before an irreversible effect; same-alias resources converge on one target batch; capability signing, trust, rotation, and compromise recovery are closed; and successful completion is a guard-owned terminal barrier. A fresh authority walk nevertheless found that the runtime decision-catalog seed still scopes `OD-HOLD-DELETION-PRECEDENCE-1` only to the pre-seal branch, while the PRD, spine's final decision row, story criteria, and matrix-v5 destructive rows require that same unresolved Product decision after the seal. The mismatch can cause independently built decision materialization and deletion execution units to disagree at an irreversible legal-hold boundary.

## Frozen Inputs And Integrity

The reviewed inputs matched the supplied SHA-256 values before this report was written:

| Input | SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `0c62966b3d91a7368d032a0f31f669d7fb0a54628e3e1675cc15e78080585448` |
| `IMPLEMENTATION-CONVENTIONS.md` | `c53767c011ce2d4312cfad5e565dc3851babfb9bd609b965017dfb352ec203cb` |
| `.memlog.md` | `d379abad04e8f2c7d2a343c5438bb96fcb7d9c6ed7b7429230c6279aea9ae985` |
| bound `prd.md` | `a6e2fd051206c8547533558978c91cf9466ce05cb29997e60c16b6921b3d51a3` |
| `epics.md` | `41beb3fd70cd7f61f058fd62c5835782daee7ce89192a351d09d30456f0bb506` |
| `external-dependency-register.md` | `2da84f01e03e2966b2d2d56b354627c9cfc232648c74e5981f5c94c8b4537377` |
| `launch-readiness-register.md` | `a85a1070e8837ff3ffaaddff69c38c6702f792945a5054430cba413e0751c606` |
| authoritative `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

The repository was inspected read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`. No source, reviewed artifact, or submodule was modified by this review. The five v21 review paths in spine frontmatter are concurrent outputs and were not treated as missing historical evidence.

## Method

I read the repository instructions and complete BMad reviewer-gate instructions, then reconciled the current spine and conventions with the authoritative validation report, bound PRD, active Epics 5–8 map, both registers, current memlog, exact root manifests/gitlinks, relevant current Agents source, and official primary release/package sources. I reconstructed the v20 findings across API/workflow, `ProtectionFence`, `GovernanceScopeGuard`, EventStore append guard, secrets signer, protection owner, migration/restore, decision catalog, and exact-result recovery, and then re-walked the original C-1..C-3 and H-1..H-12 rather than treating prior closure as proof.

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
| Medium | 3 |
| Low | 1 |

## Critical

### VC21-C1 — Runtime decision authority still excludes the post-start hold boundaries that now require the Product decision

**Classification:** target-architecture / Product-authority contradiction at an irreversible legal-hold boundary; not implementation debt and not permission to select an outcome.

**Evidence.** The bound PRD makes a post-start hold a durable restrictive fact and assigns every still-preventable accepted-batch consumption, containment issue/consume, purge, and completion to the exact still-unresolved `OD-HOLD-DELETION-PRECEDENCE-1` result (`prd.md:955-959`). The spine's normative refinements and final open-decision row say the same (`ARCHITECTURE-SPINE.md:248-258,499,1292`), and matrix-v5 makes those checks executable at containment issue, dispatch, protection, purge, and completion (`launch-readiness-register.md:324,328,349-359`). Story 8.1 and Story 8.3 also require the post-start version/outcome (`epics.md:2840-2845,3066-3067`).

The runtime seed for that decision has not been widened. Its authoritative `AffectedEvaluations` still lists `RQ-1`, legal-hold registration/prepare/recovery, and destruction-start authorization/commit **only when a registered contender overlaps an armed deletion**; neither the affected set nor its contract text names the post-start containment, dispatch, protection, purge, or completion evaluations (`launch-readiness-register.md:95,112`). Story 8.1 and Story 8.3 evidence metadata likewise retain only “armed-deletion overlap” / “armed-deletion/hold contention” wording despite their v21 criteria (`epics.md:2881-2883,3061-3063`). AD-22 also retains an earlier exclusive sentence saying the decision applies “only” after `DeletionArmed` and before `DestructionStarted`, in conflict with its later applicability paragraph and final decision row (`ARCHITECTURE-SPINE.md:485,499,1292`).

**Executable divergence.** The Story 5.5 decision-catalog implementation can legitimately seed and materialize the closed baseline affected set in register row 95. A separately built matrix/deletion implementation follows rows 324/328/349-359 and asks for the same decision at a post-start boundary. One integration reports that the operation is outside the record's affected set and proceeds or cannot bind the disposition; another applies the row-local check and blocks. If the permissive branch dispatches or consumes a batch, content can be destroyed while a legal-hold outcome that Product still owns is Open. If it fails closed forever, a later approved Product outcome still cannot be materialized consistently. Either behavior violates the single runtime decision authority.

**Required correction.** Without choosing hold-wins or deletion-wins, expand the exact `OD-HOLD-DELETION-PRECEDENCE-1` catalog/record baseline affected set and contract to the post-start evaluations that consume it: containment authorization/issue, batch dispatch, protection consumption, purge, and completion authorization/commit (plus the exact Story 8.1/8.3 branches). Reconcile the Story 8.1/8.3 evidence metadata and remove or qualify AD-22's pre-start-only “only” sentence so one current scope remains. Keep the restrictive state already defined by matrix-v5 while the decision is Open/missing/mismatched, and update the initial/successor catalog manifest under AD-25 rather than treating planning prose as runtime evidence.

## High

None.

## v20 Critical/High Correction Audit

| v20 finding | v21 disposition |
| --- | --- |
| `C-v20-1` / `VC20-C1` — post-start hold did not block new containment | **Mechanically closed; authority seed incomplete under VC21-C1.** `PostStartHoldContenderObserved` is now guard-owned and durable; containment issue, batch dispatch, protection consume, purge, and completion all require no contender or the exact deletion-allowed disposition, with stale/lookup/recovery behavior. VC21-C1 is the remaining catalog-scope contradiction for that same unresolved Product result. |
| `C-R20-1` — completion raced late admission/content | **Closed.** `AuthorizeDeletionCompletionBarrier` freezes complete evidence; `CommitDeletionCompletionBarrierEffect` conditionally appends at the exact guard revision, serializes with admissions/content/holds/containment/capability state, returns authenticated stale on change, and commits terminal `DeletionCompletionSealed`, which rejects later writes. |
| `H-v20-1` — destruction capability had no signer/trust/key lifecycle | **Closed.** `DeletionBatchCapabilityV1` is an ES256 detached JWS over closed RFC 8785 canonical fields; `EXT-SECRETS-1` owns the private family and public anchors; routine rotation retains verifiers; emergency compromise blocks unconsumed old attestations and permits only same-batch one-active-attestation replacement plus successor dispatch; protection validates current guard state online. |
| `H-v20-2` — stale caller ordinal could hide an accepted write | **Closed.** The EventStore guard assigns `AcceptedAtAdmissionFenceOrdinal` and `AcceptedAtGuardHighWater` at acceptance, records the receipt in the then-current partition, treats caller ordinal as diagnostic, and contaminates clean evidence on missing/unverifiable attribution. |
| `H-v20-3` — same DEK alias could mint repeated destructive grants | **Closed.** Coverage is keyed by complete tenant/interaction/key alias; every violation resource maps append-only to one original batch/target receipt; an already-covered target allocates no ordinal/capability and uses `AlreadyDestroyedByBatch` only for the exact target. |

## Authoritative 2026-09-12 Critical/High Closure Audit

| Authoritative finding | Current disposition |
| --- | --- |
| `C-1` weaker safety evaluation | **Closed.** AD-20 requires snapshot-plus-current conjunctive evaluation at retry, regeneration, approval, and pre-post; dominance is allowed only as tested equivalence. |
| `C-2` hold/deletion exclusion | **Mechanically closed but authority reconciliation remains Critical under VC21-C1.** Shared fence/guard ordering, all-or-none accepted batches, guarded containment, and terminal completion are executable; the runtime affected-evaluation seed does not yet express the post-start Product-decision scope those rows consume. |
| `C-3` matrix bootstrap/scope deadlock | **Closed.** Matrix v5 retains explicit target scope and closed direct bootstrap/repair preconditions; ordinal-one and successor deletion branches are separate and no code-local exception is permitted. |
| `H-1` scheduled Approver lifecycle | **Closed.** Durable single-flight scheduling, cadence/freshness, two-pass empty proof, state-specific transitions, unavailable recovery, and lease-before-read are bound. |
| `H-2` safety rescan | **Closed.** Epoch/index ownership, finite manifests, bounded fenced coordination, `RescanPending`, invalidation, and recovery are explicit. |
| `H-3` human-only Approvers | **Closed.** Parties-authoritative human classification, liveness, and actor binding are required at configuration and resolution. |
| `H-4` ledger lifetime conflation | **Closed.** Rolling-rate, open-interaction, and UTC-month Budget owners/lifetimes are separate; the unresolved concurrency-rejection choice remains safely isolated. |
| `H-5` human identity | **Closed.** Stable `AuthenticatedHumanActorId` plus principal-specific historical authority binds all human principal branches and second-party comparisons. |
| `H-6` proposal/index crash consistency | **Closed.** Interaction truth, protected/source-revision outboxes, directory high-waters, effect leases, reconciliation, and deletion owner cuts are explicit. |
| `H-7` indivisible Conversations dependency | **Closed.** Core `EXT-CONV-AI-1` and conditional `EXT-CONV-RETRACTION-1` have independent authority. |
| `H-8` trusted-envelope cryptography/replay | **Closed.** Canonical authenticated fields, logical/delivery identity split, replay owner, time/key lifecycle, ACL confinement, security spool, and lost-ack recovery are bound. |
| `H-9` export bytes/lifecycle/signature | **Closed subject to the surfaced Product lifecycle decision.** Store/index ownership, prepare/commit, ES256/JCS manifest, direct key delivery, cleanup, restore, and all-copy purge are explicit. |
| `H-10` Dapr exposure | **Closed as current-reality classification.** Parent-authoritative Client/ASP.NET `1.18.5`, non-authoritative checkout `1.18.7`, and future Workflow adoption are distinct. |
| `H-11` public contracts falsely current | **Closed.** Required-completion vocabulary is separated from the explicit delivery-debt ledger. |
| `H-12` sprint/dependency contradiction | **Closed as surfaced delivery-governance debt.** `OD-SPRINT-5.1-5.2-1` preserves the discrepancy without architecture rewriting history. |

## Medium

### VC21-M1 — Architecture-owned `RQ-1` assumptions still have no literal retirement dates

PRD FR-28 item 9 and §8.1 require every Architecture-owned `RQ-1` row to carry a co-owner-approved literal date; a milestone or unscheduled value blocks on that ground alone (`prd.md:730,894-898`). `ARCH-A-1`, `ARCH-A-2`, `ARCH-A-3`, the remaining test-stack portion of `ARCH-A-4`, `ARCH-A-6`, `ARCH-A-7`, `ARCH-A-8`, `ARCH-A-11`, `ARCH-A-12`, and `ARCH-A-14` remain `Unscheduled` or milestone-only (`ARCHITECTURE-SPINE.md:1363-1376`). The blocker is correctly surfaced. Obtain approved dates; Architecture must not invent them.

### VC21-M2 — `PostingPending` still has no single concrete timeout authority

FR-18 requires a stored deadline no shorter than the posting-seam timeout, but AD-5 and `ARCH-A-14` select neither a literal duration nor one exact versioned seam/profile field (`ARCHITECTURE-SPINE.md:276,1376`). This is safely surfaced Architecture/Product work. Bind one source and obtain Product confirmation before the posting/recovery story is ready.

### VC21-M3 — Current matrix-v5 authority remains layered over stale matrix-v4 trace text

The launch register and Epics correctly make matrix version 5 current (`launch-readiness-register.md:178,206,366`; `epics.md:159`). The spine's AD-17 Rule still instructs every controlled execution to use version 4, then a later “Current matrix binding” paragraph supersedes that sentence (`ARCHITECTURE-SPINE.md:433,451`); Story 5.5's evidence `OwnedClauses` also still says `matrix-v4` while its criteria and test name say v5 (`epics.md:1515-1517,1574-1577`). The explicit supersession prevents this from being a Critical/High ambiguity, but it is contrary to a re-distilled single current rule. Replace the stale base and evidence text with v5 rather than retaining an amendment layer.

## Low

### VC21-L1 — Root bUnit remains behind the current stable release

Root `Directory.Packages.props` and the parent-authoritative Builds gitlink pin bUnit `2.9.0`; the internally clean but parent-non-authoritative Builds checkout and the official NuGet feed expose `2.10.3`. The spine reports root authority accurately and Story 5.6 owns test-stack alignment, so this is build maintenance rather than an architecture contradiction.

## Verified Technology And Repository Reality

- Root authority remains commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`; exact gitlinks remain Builds `a32cb422`, Conversations `73bcee6f`, EventStore `ce9e779a`, FrontComposer `053b2008`, Memories `3644ef63`, Parties `fa423985`, and Tenants `2fac1839`.
- All seven inspected submodule worktrees are internally clean. Builds `cf52f74`, Conversations `64b05083`, EventStore `a568af4e`, FrontComposer `1b3608c9`, and Memories `42dfa26b` differ from parent gitlinks; Parties and Tenants match. Parent gitlinks remain repository authority.
- Root `global.json` pins SDK `10.0.401` with `latestPatch`; root build properties set `net10.0` and C# 14. Microsoft's official .NET 10 release metadata maps the 2026-09-08 `10.0.12` release to SDKs `10.0.401` and `10.0.112`.
- The parent-authoritative Builds catalog pins Dapr Client/ASP.NET/Workflow `1.18.5`; the separate Builds checkout pins `1.18.7`. Official NuGet package indexes expose stable `1.18.7` and only `1.19.0-preview.*` afterward. The Dapr security decision and ARCH-A-15 remain correctly blocking.
- The September advisory identifies `Microsoft.DiaSymReader.Native`, not an SDK number, and gives component floor `18.9.0-beta1.26405.2`; the inspected Agents source/project graph contains no such reference. The v21 Stack wording now separates the SDK observation from the component rule accurately.
- Current Agents source imports EventStore Client/DomainService and calls `AddDaprClient`; no Agents project references Dapr Workflow or Microsoft Agent Framework. Current source contains none of the target decision catalog, deletion guards/ledgers, signed batch capabilities, three admission ledgers, safety epoch, export store, or trusted-envelope replay mechanisms. Existing `SafetyFailed` and post-before-dispatch approval behavior remain truthfully classified as delivery debt.
- Every declared non-concurrent local source exists, every external primary source URL returned HTTP 200, and all twelve external dependency records remain `Uncommitted` with `TBD` targets/dates/commands. No current artifact update falsely makes a consumer ready.

## AD IDs, Memlog, Open Decisions, And Debt Separation

The spine contains exactly one each of `AD-1` through `AD-31`, with no duplicate, omission, or renumbering. The memlog remains append-only and records the v21 mechanics separately from the unresolved Product result. Target mechanisms and absent source implementation remain separated in the delivery-debt table.

The named hold-precedence, hold-prepare cancellation, operator deletion cancellation/nonterminal handling, export lifecycle, rate/concurrency consumption, Dapr security, historical safety, optional retraction, instruction protection, legacy plaintext, initial output-safety status, class/range scope, human exact-Conversation scope, recorder scope, and sprint-evidence decisions remain Open with owners and restrictive safe states. VC21-C1 asks only for one consistent affected-evaluation scope for an already surfaced Product decision; it does not choose any outcome.

## Gate Conclusion

The complete frozen v21 verified-current/bound-PRD gate is **FAIL**. Final counts: **Critical 1, High 0, Medium 3, Low 1**. Deterministic lint passed with zero findings. The gate can pass only after VC21-C1 is reconciled across the runtime decision catalog, current AD wording, and story metadata without inventing the hold/deletion Product outcome, followed by a fresh complete review.
