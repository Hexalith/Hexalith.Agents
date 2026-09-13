---
name: Hexalith Agents good-spine rubric review v10
type: architecture-spine-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
intent: frozen-read-only-review
reviewer: good-spine rubric walker
lint_ok: true
---

# Good-Spine Rubric Walker — v10

## Verdict

**FAIL — 0 Critical, 1 High, 3 Medium, 0 Low.** The frozen revision closes every Critical and High finding in the authoritative `VALIDATION-REPORT-2026-09-12.md` and materially closes every v9 Critical/High finding across the rubric, verified-current, and adversarial reviews. A fresh whole-artifact walk finds one remaining High convergence defect: the export-lifecycle decision is still an unconditional deletion-completion prerequisite in the spine/register, contradicting the PRD and Story 8.3 rule that it applies only when the frozen set contains an export artifact or lifecycle-covered copy.

The defect fails closed rather than authorizing erasure, so it is High rather than Critical. It must be corrected without choosing `OD-EXPORT-LIFECYCLE-1`. Zero Critical/High is required for PASS; this frozen gate therefore fails.

## Frozen Input Snapshot And Method

The reviewer re-read the complete current Architecture Spine, authoritative validation report, implementation convention, bound PRD, active and historical epic authority, both registers, current memlog, repository instructions, relevant v9 review reports, and focused repository/package/gitlink reality. The review independently walked the complete Good-Spine checklist; prior findings were closure inputs, not substitutes for the checklist. The walk covered aggregate and mutation ownership, durable-before-effect ordering, decision-catalog bootstrap and supersession, recorder and approver authority, tenant routing, readiness self-bootstrap, operation-gate scope, public statuses, rate/open/budget races and time bounds, safety activation cohorts, trusted replay and denial recording, hold/export/deletion fencing and recovery, external dependency ownership, source traceability, structural handoff, and architecture-versus-delivery debt.

The hashes below were captured at the start and recomputed immediately before report creation and after the report write; all reviewed inputs were byte-identical.

| Frozen input | SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `50de644622287cb62bec31a56ee8dff3028e04c06221cd8d292e3b0cbdb40604` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |
| `IMPLEMENTATION-CONVENTIONS.md` | `a006a169b1b379df0ed41b3bea75eaf47859bf494fa3c0d6541ed8259d72a38c` |
| bound `prd.md` | `af43b92cf23c983ab31d85766cebb74b8ed884fdc4f7ed8c84b0f6aa5f4645f5` |
| `epics.md` | `687c4c20723e16c1bddc9a23ab30c10133dfbaec436e4eec2f6d43dc4fa5b906` |
| `external-dependency-register.md` | `cd6bd54ceceb147fe6f7c25190450ab27c4a45a6ca922baac93ddb98fd55b836` |
| `launch-readiness-register.md` | `f329e279b6e9675a930ccfd5d5e54ff4bdc87655b590e564a7303f4e9f2fd24f` |
| `.memlog.md` | `6f366ce5ca778ea66cde8cf4aba6a11161934a0998d5f4fe1f7e5fa2a9b75908` |
| repository instruction source | `bfbe399b567d852a74c7ee8c9217217a3ea5eb80ca591b5b8ac01acfbe838966` |
| reviewer-gate rubric | `d32e32a3c1d59b5612b947004f3f6fef1117a13ce9f1ffa428d616e0b5d4db69` |
| root `global.json` | `fc4602f9d88c9440f70f72732343a88c5e4190223fb9b77f9b8ac8eb1c4c8c2d` |

The parent-authoritative root commit is `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`; its gitlinks are Builds `a32cb422`, Conversations `73bcee6f`, EventStore `ce9e779a`, FrontComposer `053b2008`, Parties `fa423985`, and Tenants `2fac1839`. Initialized submodule working trees do not supersede those parent gitlinks.

## Deterministic Linter

Command:

```text
python3 .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: `ok: true`, `total_findings: 0`, no severity entries. The stable AD inventory is exactly one occurrence each of AD-1 through AD-31, with no missing, duplicate, or renumbered id. Mechanical validity does not close the semantic High below.

## Critical

None.

## High

### H-R10-1 — Export-lifecycle approval is still an unconditional deletion-completion prerequisite

**Evidence.** The bound Product/delivery rule is conditional: Story 8.3 requires `EXT-EXPORT-STORE-1` and `OD-EXPORT-LIFECYCLE-1` only when the frozen set contains an export artifact or lifecycle-covered copy (`epics.md:2929,2984-2986,2991`), and the register's decision contract says deletion cannot complete **across exports** until the exact decision/store binding exists (`launch-readiness-register.md:97`). The runtime matrix nevertheless gives every `GovernanceProtection:DeletionCompleteRecovery` the unconditional direct precondition `ApprovedExportLifecycleDecisionVersionMatchesRecordedBranch` (`launch-readiness-register.md:247`). AD-22 likewise says a missing lifecycle policy fails “deletion completion” without the export-bearing qualification (`ARCHITECTURE-SPINE.md:358`), and the distilled open-decision row says “deletion qualification” fails closed even though its owner/revisit cell limits Story 8.3 applicability to committed-artifact overlap (`ARCHITECTURE-SPINE.md:1006`). The external register lists Stories 8.1 and 8.3 as unqualified `EXT-EXPORT-STORE-1` consumers while their active story contracts are conditional (`external-dependency-register.md:205`).

**Two literal units.** A matrix/spine implementation blocks an interaction-only deletion forever while `OD-EXPORT-LIFECYCLE-1` is Open, even when the frozen set proves no export artifact, index member, wrapped export key, backup, restored copy, or other lifecycle-covered copy exists. A PRD/story implementation completes that same deletion after the ordinary OQ-31, protection, hold, fence, and all-copies receipts pass. Both retain data safely, but they disagree about whether a required V1 deletion can ever complete and about whether Stories 8.1/8.3 can be authorized without an export-bearing set.

**Impact.** This is a target architecture/register contradiction, not implementation debt. It can make ordinary approved deletion and Conversation-deletion propagation operationally impossible for tenants that have never produced an export, and it can make story/dependency readiness disagree across planning consumers. Because the overconstraint retains data and never grants irreversible authority, no Critical data-loss path was found.

**Disposition — AUTOFIX, no Product outcome required.** Qualify AD-22 and the Blocking Open Decisions row with “when the frozen set contains an export artifact or lifecycle-covered copy.” Change the matrix precondition to the closed disjunction `NoLifecycleCoveredExportOrCopyInFrozenSet` **or** `ApprovedExportLifecycleDecisionVersionMatchesRecordedBranch`, with the no-copy proof derived from the exact frozen set/index/store inventory at recorded revisions. Qualify `OD-EXPORT-LIFECYCLE-1`'s `AffectedEvaluations` entry and the external-register consumer mapping on the same condition. Preserve the `DecisionId`, keep its outcome Open, and add negative evidence that an export-free deletion neither reads nor fabricates an export-lifecycle decision while an export-bearing deletion remains blocked.

## Medium

### M-R10-1 — Ten Architecture-owned assumption rows still have no literal retirement date

The assumption table now names a `TargetRetirementDate`, but ARCH-A-1, ARCH-A-2, ARCH-A-3, the remaining test-stack portion of ARCH-A-4, ARCH-A-6, ARCH-A-7, ARCH-A-8, ARCH-A-11, ARCH-A-12, and ARCH-A-14 still say only “Unscheduled” plus a milestone (`ARCHITECTURE-SPINE.md:1063-1078`). Their owners, conditions, and blockers are explicit, so implementations fail closed and this is not High. Record calendar targets through the named owners; do not invent them in Architecture.

### M-R10-2 — `PostingPending` timeout remains intentionally unresolved

AD-5 fixes only “no shorter than” the Conversations seam timeout and ARCH-A-14 retains no duration/configuration authority (`ARCHITECTURE-SPINE.md:198,1078`). Story 7.4 now explicitly blocks `ready-for-dev` until the assumption is retired against the committed seam (`epics.md:2527`), which prevents divergent implementation and closes the former High handoff risk. The unresolved timeout remains Medium assumption debt requiring Architecture/Product confirmation before that story.

### M-R10-3 — Two declared local reviewer sources do not exist

The spine declares `reviews/review-2026-09-12-security-data-integrity-v3.md` and `reviews/review-2026-09-12-brownfield-drift-v3.md` as sources (`ARCHITECTURE-SPINE.md:89-90`), but neither file exists. All other 74 declared local sources resolve. Remove the two nonexistent declarations or add the actual authoritative reports; do not cite a nonexistent review as update evidence.

## Low

None.

## v9 Critical/High Closure Audit

All v9 Critical/High findings were rechecked against their original evidence, not merely marked closed from memlog entries.

| v9 finding(s) | Current disposition |
| --- | --- |
| Adversarial C-1 export commit owner/initial operation | **Closed.** `ProtectionFence` owns the single `ExportCommitDecided` append and high-water; matrix variants `ExportCommitDecision` and `ExportCommitRecovery` separate the fence decision from the idempotent `AuditExport` acknowledgement and key delivery (`ARCHITECTURE-SPINE.md:358`; register `:238-239`; Story 8.2 `:2875-2878`). |
| Adversarial H-1 / VC9-H2 posting validation and approval/`PostingPending` ambiguity | **Closed.** AD-5, the convention, both sequence branches, and Story 7.4 require `Approved` only, full pre-post validation, then separate durable `BeginPosting`, then the external append (`ARCHITECTURE-SPINE.md:200,579-596`; convention `:15`; epics `:2532-2545`). |
| H-R9-1 / adversarial H-2 catalog activation absent | **Closed.** AD-17, matrix, register authority prose, and Story 5.5 define `ArchitectureDecision:ActivateCatalog`, its exact manifest, expected revision, single activation append, concurrency, and lost-ack behavior (`ARCHITECTURE-SPINE.md:318-329`; register `:224`; epics `:1529-1532`). |
| H-R9-2 missing-catalog code outside vocabulary | **Closed.** Catalog absence/invalid authorization uses existing `OpenDecision` with safe detail `CatalogAbsentOrInvalid`; no peer blocker code was invented (`ARCHITECTURE-SPINE.md:325`; register `:91,112`). |
| H-R9-3 generic recovery / adversarial H-4 release-version pin | **Closed.** AD-17 and AD-23 distinguish crash Resume, authorized/profile-mapped Abort, immutable branch dispositions, human hold-release pinning, and dedicated gate-free recovery; matrix `HoldReleaseRecovery` carries the recorded lifecycle version/store target (`ARCHITECTURE-SPINE.md:327-329,372-378`; register `:229-247`). |
| H-R9-4 / adversarial H-3 / VC9-H4 no-hold destruction | **Closed.** Ordinary no-hold destruction is executable; only recorded armed contention needs the approved precedence outcome (`ARCHITECTURE-SPINE.md:358,1004`; register `:106,245`; epics `:2940-2942`). |
| H-R9-5 hold-prepare cancellation promoted to `RQ-1` | **Closed.** OQ-34/`OD-HOLD-PREPARE-CANCELLATION-1` affects only hold Abort/unwind and the Story 8.1 cancellation branch, not `RQ-1` (`ARCHITECTURE-SPINE.md:1005`; register `:96`; PRD `:1088`). |
| H-R9-6 OQ-31 weakened in Story 8.3 | **Closed.** OQ-31 unconditionally blocks all Story 8.3 authorization and signal handling while Open; no reduced interaction-only branch can bypass it (`ARCHITECTURE-SPINE.md:1012`; register `:103,240-247`; epics `:2929,2944-2953,2984-2991`). |
| VC9-H1 diagram chose rejected-call consumption | **Closed.** The sequence records and applies the exact approved joint disposition and does not assume rate abort/commit when an owner rejects (`ARCHITECTURE-SPINE.md:522-540`). |
| VC9-H3 Conversation-deletion propagation had no delivery owner | **Closed.** AD-6/AD-30 define the authenticated source signal and constrained workflow; Story 8.3 and `EXT-CONV-AI-1` now own the intake, checkpoint, terminalization, fence, recovery, and tests (`ARCHITECTURE-SPINE.md:212,426`; external register `:63-65`; epics `:2949-2953,2984-2990`; matrix `:240-241`). |

H-R10-1 is a later conditional-scope inconsistency at deletion completion; it does not reopen the v9 export-commit owner, no-hold destruction, OQ-31, or Conversation-signal corrections.

## Authoritative 2026-09-12 Critical/High Closure Audit

| Authoritative finding | Current disposition |
| --- | --- |
| C-1 weaker safety evaluation | **Closed.** AD-20 requires conjunctive snapshot-plus-current evaluation at retry, regeneration, approval, and pre-post, with only tested dominance equivalence allowed. |
| C-2 hold/deletion exclusion | **Closed.** The tenant `ProtectionFence`, frozen complete sets, prepare/arm/destruction states, export cleanup/commit high-water, and exact receipts prevent partial irreversible work. H-R10-1 over-blocks an export-free completion; it does not bypass the fence. |
| C-3 bootstrap/matrix deadlock | **Closed.** Matrix v4 provides target-aware bootstrap/repair variants, Platform/Tenant scopes, direct owner preconditions, an LR-EVENTSTORE self-bootstrap, and containment-safe kill-switch pull. |
| H-1 scheduled Approver re-check | **Closed.** AD-8/AD-5 bind single-flight cadence, two-pass empty evidence, state-specific outcomes, and unavailability retry. |
| H-2 safety rescan owner | **Closed.** `SafetyVerdictEpoch`, `SafetyVerdictIndex`, the finite frozen active-tenant and Conversation manifests, fenced coordinator, bounded profile, conditional initialization, and exact `RescanPending` behavior are explicit. |
| H-3 human-only Approvers | **Closed.** AD-8 and `EXT-PARTIES-1` require authoritative human classification, liveness, and stable actor binding at configuration and resolution. |
| H-4 conflated ledgers | **Closed architecturally.** Rate consumption, open-interaction leases, and monetary reservations have three owners, distinct partitions/lifetimes, interaction-owned decisions, reset/deadline rules, and recovery. Their code remains delivery debt. |
| H-5 discarded human identity | **Closed.** AD-30 carries stable `AuthenticatedHumanActorId` for every human principal; AD-22 uses the tagged User/Administrator/Platform evidence union for separation of duties. |
| H-6 crash-inconsistent proposal index | **Closed architecturally.** Interaction events/outbox are truth; the Conversation index has source-revision high-water and checkpoint reconciliation before removal. |
| H-7 indivisible Conversations dependency | **Closed.** Core six-seam `EXT-CONV-AI-1` and optional `EXT-CONV-RETRACTION-1` are separate, and the optional dependency affects only the selected Automatic-mode branch. |
| H-8 trusted-envelope replay/key lifecycle | **Closed.** Canonical authenticated fields, `LogicalCommandId`/`DeliveryNonce`, first-seen owner, retention, rotation, emergency revocation, ACL confinement, and durable denial recording are bound. |
| H-9 export bytes/lifecycle/signature | **Closed except H-R10-1's conditional deletion scope.** AD-22 and `EXT-EXPORT-STORE-1` bind store/index/fence, inventory, encryption, signed canonical manifest, key delivery, expiry/hold authorization, cleanup, and purge receipts while leaving provider/lifecycle choices Open. |
| H-10 current Dapr exposure | **Closed in architecture/current-reality wording.** Client/ASP.NET `1.18.5` exposure is current; Workflow remains future; ARCH-A-15 has a 2026-09-30 upgrade/exception target. |
| H-11 public parity overstated | **Closed.** AD-15 calls the target required-completion parity and assigns missing vocabulary to stories; current code is explicitly delivery debt. |
| H-12 sprint tracking contradiction | **Closed as architecture classification, still unresolved delivery history.** `OD-SPRINT-5.1-5.2-1` blocks dependent delivery authorization without changing architecture or rewriting history. |

## Requested Cross-Contract And Good-Spine Walk

| Dimension | Result |
| --- | --- |
| Real divergence points / enforceable AD rules | **Fail narrowly.** H-R10-1 leaves deletion completion and story/dependency authorization with two literal scopes. Other ownership, ordering, recovery, authorization, and public contract rules are enforceable. |
| Decision self-supersession and catalog bootstrap | **Pass.** Stable literal ids, root/predecessor authorization, nonempty quorum, effective/pending union, explicit catalog activation, narrowing authorization, actor separation, and phase-pinned recovery prevent self-weakening. |
| Release Operator recorder authority | **Pass as unresolved.** OQ-33 and `OD-RELEASE-RECORDER-SCOPE-1` deny every principal until Product selects the authority source; Architecture chose none. |
| Prepare recovery branch decisions | **Pass.** Resume and Abort sources, recorded dispositions, dedicated rows, unavailability, successor pinning, and first/last-ack recovery are explicit. |
| Readiness self-bootstrap / kill-switch pull and release | **Pass.** EventStore observation can bootstrap its own record; emergency containment is gate-free but evidence-bound; release is separately normally gated. |
| Public statuses and errors | **Pass.** Required completion vocabulary aligns with PRD; current missing symbols remain assigned debt. |
| Rate/open/budget ledger races and bounds | **Pass as unresolved where Product owns semantics.** No ledger is contacted before OQ-32 approval; preparations reserve capacity, derive deadlines/reset instants from one versioned profile, and converge through interaction-owned decisions. |
| Safety cohort enumeration | **Pass.** Frozen EventStore positions, finite count/hash/high-water manifests, concurrent tenant handshake, unindexed Conversation initialization, and bounded fenced workers close the global barrier. |
| Trusted replay / security recorder | **Pass.** First-seen registration precedes target idempotency and is capability/ACL confined; denial routing is caller-independent and durably spooled. |
| Human actor-evidence union | **Pass.** Party-bearing User history is not imposed on Party-free Administrator/Platform evidence, while stable actor comparison remains common. |
| Export/hold/deletion fence and all-copy preservation | **Fail only at H-R10-1.** Commit ownership, expiry/hold control, cleanup, armed contention, destruction, receipts, and recovery otherwise converge. |
| Deferred PRD decisions | **Pass.** OQ-18, OQ-23, OQ-31, OQ-32, OQ-33, and OQ-34 are materialized or explicitly scoped without selected outcomes. H-R10-1 corrects an overbroad application of the separately Open export-lifecycle decision. |
| Spec capability coverage | **Pass.** The capability map, AD binds, active Epics 5-8, matrix, projection inventory, and dependency register cover FR-1..FR-34 and NFR-1..NFR-14. Historical Epics 1-4 are explicitly non-executable replacement history. |
| Brownfield ratification / verified-current stack | **Pass.** Current gitlinks, package authority, Dapr exposure, absent host/Workflow, current legacy posting behavior, and target-versus-current status are distinguished. |
| Operational/environmental envelope | **Pass.** Platform host, EventStore, Dapr Workflow, provider/secrets/protection/export ports, recovery/restore, capacity, telemetry, evidence, environment profiles, and dependency commitment are decided or explicitly unresolved. |
| Deferred items | **Pass with Medium governance debt.** Nothing under Deferred silently selects runtime behavior; M-R10-1/M-R10-2 are explicit blockers. |
| Source traceability and mechanical handoff | **Pass mechanically; Medium source defect.** Lint is clean, AD ids are stable, decision catalogs match, and local sources resolve except M-R10-3. |

## Stable Decisions And Unresolved Product/Governance Choices

The spine and register contain the same ten stable `DecisionId` values. No outcome was selected by this review.

| DecisionId | Safe unresolved scope |
| --- | --- |
| `OD-HOLD-DELETION-PRECEDENCE-1` | Armed-deletion/later-hold contention and `RQ-1`; ordinary no-hold and pre-arm deferral remain executable. |
| `OD-HOLD-PREPARE-CANCELLATION-1` | Hold Abort/unwind and Story 8.1 cancellation branch only; no `RQ-1`. |
| `OD-EXPORT-LIFECYCLE-1` | Export creation/download/store activation, `RQ-1`, and hold/deletion operations only where lifecycle-covered copies overlap; H-R10-1 is the remaining scope correction. |
| `OD-RATE-CONCURRENCY-CONSUMPTION-1` | Joint original-call rate/open admission and Story 6.4 only; no `RQ-1`. |
| `OD-DAPR-SECURITY-1` | Package-family upgrade or bounded exception, Workflow adoption, and `RQ-1`. |
| `OD-SPRINT-5.1-5.2-1` | Delivery authorization only; no runtime or `RQ-1` input. |
| `OD-PRD-OQ18-HISTORICAL-SAFETY-1` | Whole-history safe default and `RQ-1`. |
| `OD-PRD-OQ23-AUTOMATIC-RETRACTION-1` | First Automatic-mode tenant eligibility only. |
| `OD-PRD-OQ31-INSTRUCTION-PROTECTION-1` | `RQ-1`, Story 5.8, and all Story 8.3 authorization. |
| `OD-RELEASE-RECORDER-SCOPE-1` | All decision recording/activation variants, Story 5.5, and `RQ-1`; every recorder remains denied. |

## Architecture Defects Versus Implementation And Delivery Debt

H-R10-1 is an architecture/register defect: two independently built evaluators can obey different authoritative scopes. M-R10-1 through M-R10-3 are assumption-governance/source-chain debt in the artifact, not missing code.

Focused source search still finds no target `ArchitectureDecisionCatalog`/`ArchitectureDecisionRecord`, split rate/open/budget aggregates, safety epoch/index, trusted replay registrar, security recorder/spool, common protection fence, export store, or Dapr Workflow owner. Current approval code still emits `PostingPending` from approval and current orchestration remains the documented legacy posting path. Those gaps are implementation debt assigned by the spine to Stories 5.4-5.8, 6.1, 6.3-6.6, 7.4, and 8.1-8.4 plus the external records; they are not extra architecture findings and no build/test success is claimed. The Story 5.1/5.2 sprint-status conflict remains the explicitly Open delivery-history decision, not an AD change.

## Required Correction Order

1. Correct H-R10-1 across AD-22, the open-decision distillation, `OD-EXPORT-LIFECYCLE-1` affected evaluations, `DeletionCompleteRecovery`, external dependency consumer wording, and Story 8.3 evidence without selecting the lifecycle outcome or changing its stable `DecisionId`.
2. Retain M-R10-1 and M-R10-2 as explicit blockers until their named owners supply dates/timeout authority; do not invent them.
3. Remove or supply the two missing M-R10-3 source files.
4. Re-distill, append the corrections to the memlog, rerun deterministic lint, freeze/hash, and rerun the complete reviewer gate. PASS requires zero Critical and zero High.

## Gate Conclusion

The frozen v10 artifacts do **not** pass: **0 Critical, 1 High, 3 Medium, 0 Low**. All authoritative and v9 Critical/High corrections remain materially intact, but export-lifecycle gating must be made conditional on an export-bearing deletion set so the matrix, spine, PRD, epics, and dependency register converge without choosing the unresolved Product policy.
