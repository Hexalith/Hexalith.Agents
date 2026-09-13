---
name: Hexalith Agents architecture good-spine rubric review v9
type: architecture-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
lens: rubric-walker
intent: validate-read-only
verdict: fail
counts:
  critical: 0
  high: 6
  medium: 5
  low: 0
---

# Good-Spine Rubric Walker — v9

## Verdict

**FAIL — 0 Critical, 6 High, 5 Medium, 0 Low.** The frozen revision closes the authoritative 2026-09-12 report's three Critical and twelve High findings and materially closes every v8 Critical/High finding. A fresh whole-artifact walk nevertheless finds six current High convergence defects: the new decision catalog cannot be activated by any declared command, its missing-catalog result is outside the closed blocker vocabulary, one recovery paragraph still contradicts the branch-specific Resume rule, the deletion-start matrix still requires the hold/deletion decision even when no hold overlaps, and two deferred Product decisions have inconsistent affected scopes.

No current finding authorizes an unsafe Product choice. The affected operations remain fail-closed; that is why the catalog, recovery, and decision-scope defects are High rather than Critical. Zero Critical/High is required for PASS, so this gate fails.

## Frozen Input Snapshot And Method

The reviewer re-read the complete current spine, authoritative validation report, implementation convention, bound PRD, epics, both registers, current memlog, repository instructions, and focused repository/package/gitlink reality. The Good-Spine checklist was walked independently rather than treating v8 as the checklist. The walk covered aggregate and mutation ownership, durable-before-effect ordering, decision bootstrap/supersession, authorization and human-evidence separation, tenant routing, readiness bootstrap, operation gates, public statuses, rate/open/budget races, safety activation cohorts, trusted replay/security recording, hold/export/deletion preservation and recovery, external dependencies, source traceability, structural handoff, and architecture-versus-delivery debt.

| Frozen input | SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `f232325fb8fd05d040a0872a4b633fd4e2d4d717a3cc698e67fbbc4991d6064f` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |
| `IMPLEMENTATION-CONVENTIONS.md` | `a006a169b1b379df0ed41b3bea75eaf47859bf494fa3c0d6541ed8259d72a38c` |
| bound `prd.md` | `cd36ccef836ddc38db09d67c22013c266c86ccb360d34d3fb587ee0bfc8baf52` |
| `epics.md` | `755f908ab43a2c1c533d41c75e294d0c1937c22747d635b188362c567f75ecf6` |
| `external-dependency-register.md` | `97b420ea0e2c915b8757d7be3df853e7fdf55b7234bd6eba0936b01ce6278085` |
| `launch-readiness-register.md` | `5bee06e4303af18e280abe718fcc1b77e3df12d5e44a1360be413ed9e893f2c4` |
| `.memlog.md` | `f1716965f57a0923bc2cb56e63dcee74fb272621a4bd8cfb2d5031ee076a4ed1` |
| repository instruction source | `bfbe399b567d852a74c7ee8c9217217a3ea5eb80ca591b5b8ac01acfbe838966` |
| reviewer-gate rubric | `d32e32a3c1d59b5612b947004f3f6fef1117a13ce9f1ffa428d616e0b5d4db69` |
| root `global.json` | `fc4602f9d88c9440f70f72732343a88c5e4190223fb9b77f9b8ac8eb1c4c8c2d` |

The parent-authoritative root gitlinks are Builds `a32cb422`, Conversations `73bcee6f`, EventStore `ce9e779a`, FrontComposer `053b2008`, Parties `fa423985`, and Tenants `2fac1839`. The initialized Builds, Conversations, EventStore, and FrontComposer checkouts are ahead/dirty and do not supersede those parent gitlinks. Focused source search still finds no `ArchitectureDecisionCatalog` or `ArchitectureDecisionRecord`; current posting code/tests reflect the documented legacy implementation. Those are delivery facts, not extra architecture findings.

## Deterministic Linter

Command:

```text
python3 .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: `ok: true`, `total_findings: 0`, no severity entries. Mechanical validity does not close the semantic findings below.

## Critical

None.

## High

### H-R9-1 — The pending decision catalog has no executable activation operation

**Classification:** architecture decision-lifecycle defect; not implementation debt and not an unresolved Product outcome.

**Evidence**

- AD-17 requires a new catalog version to be appended as `PendingCatalog`, says it cannot become effective until every newly required decision stream exists at the exact minimum contract, and says activation is separately durable (`ARCHITECTURE-SPINE.md:323`).
- A new decision stream cannot be populated before the pending catalog entry exists because `ArchitectureDecision:PublishContract` requires an exact effective-or-pending catalog entry (`launch-readiness-register.md:222`). This correctly forces the sequence `PendingCatalog -> decision streams/contracts -> catalog activation`.
- The matrix declares only `PublishCatalog`, `PublishContract`, and `RecordApproval`. `PublishCatalog` says it appends `PendingCatalog`; no `ActivateCatalog` variant, complete required-stream checkpoint/manifest, or activation precondition exists (`launch-readiness-register.md:222-224`). `RecordApproval` can activate a decision contract, not the separate catalog stream.
- Matrix v4 says every command/activity must declare a concrete listed variant and an unrecognized variant blocks (`launch-readiness-register.md:168,247-251`). AD-17 also makes an exact replay a no-op, so replaying the original publish cannot be assumed to append a later activation (`ARCHITECTURE-SPINE.md:323`).

**Divergence / impact**

One implementation may invent a second `PublishCatalog` phase that reads cross-stream state and appends activation; another may activate automatically when the last decision record changes; a literal implementation leaves every genesis/successor catalog pending forever. They differ on command identity, expected revision, evidence checkpoint, and the moment blockers become effective. Since a pending or missing effective catalog blocks every decision-dependent operation and `RQ-1`, this is release-blocking but fail-closed.

**Required correction — AUTOFIX ARCHITECTURE.** Add a distinct catalog-activation variant (or explicitly define a two-phase ensure command) with one immutable complete required-record checkpoint/manifest, root/predecessor authorization, expected catalog revision, exact minimum-contract validation, idempotency/lost-ack behavior, and fixtures for genesis and successor activation. Preserve every existing `DecisionId`; do not select any decision outcome.

### H-R9-2 — `DecisionCatalogMissing` is emitted outside the closed blocker vocabulary

**Classification:** public readiness/status contract defect; not implementation debt.

**Evidence**

- The register declares its V1 `BlockerCode` table closed and says an unknown peer code fails closed (`launch-readiness-register.md:40-59`). `DecisionCatalogMissing` is absent from that table and from its emitter table (`:74-87`).
- AD-17, the register's decision section/projection, and Story 5.5 nevertheless require `DecisionCatalogMissing` to be emitted (`ARCHITECTURE-SPINE.md:323`; `launch-readiness-register.md:91,112`; `epics.md:1524-1527`).
- The PRD's FR-28 readiness vocabulary names `OpenDecision` for absent or unresolved materialized decisions but does not define a peer `DecisionCatalogMissing` blocker (`prd.md:721-743`).

**Divergence / impact**

An API/projection team can reject the new code as unknown, another can collapse it to `OpenDecision`, and another can add a new public enum member. All fail closed, but API, BFF, UI, audit, and `RQ-1` evidence no longer carry one interoperable status or Product-authorized vocabulary.

**Required correction — RECONCILE WITHOUT INVENTING PRODUCT.** Either map absent/invalid catalog to the existing `OpenDecision` code with a catalog-specific safe detail, or obtain the Product-contract amendment needed to add `DecisionCatalogMissing` to the closed public vocabulary; then synchronize AD-15, AD-17, the register, and Story 5.5.

### H-R9-3 — The NFR-11 recovery contract still excludes ordinary crash Resume and permits the wrong source for it

**Classification:** architecture recovery contradiction; residual from v8, not implementation debt.

**Evidence**

- Revised AD-17 correctly says `Resume` is authorized by the original still-valid prepare intent at its exact revision, identical frozen set/token/versions, and absence of cancellation/abort; an ordinary crash or lost acknowledgement therefore resumes without a fabricated failure (`ARCHITECTURE-SPINE.md:325`). The hold/export/deletion branch-decision matrix rows now encode that branch-specific split (`launch-readiness-register.md:228-241`).
- The register's normative NFR-11 paragraph still says an **authorized cancellation or closed permanent-failure fact must first produce** `PrepareRecoveryDispositionDecided(Resume|Abort)` before either recovery path (`launch-readiness-register.md:300`). That excludes an ordinary crash with neither fact and lets a permanent-failure fact appear to source `Resume`.
- Story 8.1 uses the corrected hold-specific rule, and Stories 8.2/8.3 use original-intent Resume versus mapped-cancellation/failure Abort (`epics.md:2801-2804,2880-2883,2934-2937`).

**Divergence / impact**

An NFR-11 implementation can strand a recoverable hold/export/deletion preparation because no cancellation/failure exists, while a matrix-led implementation records Resume from the original intent. A third can treat a permanent failure as sufficient source for Resume even when the versioned recovery profile maps it to Abort. The hold-specific matrix now prevents unauthorized unpin, keeping this below Critical, but the recovery evidence contract cannot be implemented consistently.

**Required correction — AUTOFIX REGISTER TEXT.** Replace the generic union with the exact branch-specific rule already present in AD-17/matrix: original still-valid intent and no cancellation/abort for Resume; authorized cancellation or profile-mapped closed failure for export/deletion Abort; exact approved cancellation decision plus durable cancellation fact for hold Abort. Keep phase pinning and expected-revision mutual exclusion.

### H-R9-4 — The deletion-start matrix unconditionally requires the hold/deletion decision on the no-hold path

**Classification:** architecture operation-gate contradiction; not the unresolved Product/Governance policy itself.

**Evidence**

- AD-22 says `OD-HOLD-DELETION-PRECEDENCE-1` applies only when a hold first contends after `DeletionArmed` and before `DestructionStarted`; a deletion with no overlapping hold may append `DestructionStarted` directly without that decision (`ARCHITECTURE-SPINE.md:357,365`).
- The register's decision record repeats that ordinary no-armed-overlap behavior remains evaluable (`launch-readiness-register.md:95`). Story 8.3 likewise permits direct no-hold start and requires the decision only for post-arm hold contention (`epics.md:2939-2942`).
- Matrix row `GovernanceProtection:DeletionDestructionStarted` conjunctively requires both `ApprovedHoldDeletionDecisionVersionMatches` and `NoOverlappingHoldOrExportCleanupPending` with no conditional variant (`launch-readiness-register.md:242`).

**Divergence / impact**

A matrix-literal worker blocks every destruction while the decision is Open, including ordinary no-hold deletion. A spine/story worker bypasses that precondition when no hold overlaps. The disagreement changes which deletion requests can complete and which decision version is recorded. It is fail-closed but prevents the Product/architecture-approved common path.

**Required correction — AUTOFIX MATRIX.** Split no-hold and armed-contention variants, or make the direct precondition a closed disjunction: exact proof of no overlapping hold at the expected fence revision, or the exact approved decision version/outcome for armed contention. Preserve the export-cleanup prohibition and add both-path fixtures.

### H-R9-5 — The hold-preparation cancellation decision is incorrectly promoted to a global `RQ-1` blocker

**Classification:** invented affected-scope expansion over an unresolved Product decision.

**Evidence**

- Bound PRD OQ-34 states that `OD-HOLD-PREPARE-CANCELLATION-1` blocks **only** Story 8.1's cancellation branch and its runtime branch selection, not ordinary hold preparation (`prd.md:1088`). FR-28 makes `RQ-1` consume only deferred rows whose recorded affected scope includes it (`prd.md:721-743`).
- The launch register instead adds `RQ-1` to this decision's `AffectedEvaluations` (`launch-readiness-register.md:96`).
- The spine's Blocking Open Decisions table says the choice must land “before `RQ-1`” (`ARCHITECTURE-SPINE.md:995`).

**Divergence / impact**

The PRD permits qualification using the safe state in which preparation can Resume and no cancellation/unwind branch exists. The spine/register make the same open choice block every qualification. This does not lose data, but architecture has changed Product scope while explicitly claiming not to choose the outcome.

**Required correction — RECONCILE TO PRODUCT.** Remove `RQ-1` from this decision's affected evaluations and spine revisit text unless Product explicitly amends OQ-34/FR-28. Keep `GovernanceProtection:HoldPrepareBranchDecision` for Abort, unwind recovery, and Story 8.1 cancellation authorization blocked.

### H-R9-6 — Story 8.3 conditionally weakens the PRD's instruction-protection decision blocker

**Classification:** bound-PRD versus executable-backlog authorization conflict; not implementation debt.

**Evidence**

- PRD OQ-31 states that while `OD-PRD-OQ31-INSTRUCTION-PROTECTION-1` is Open it blocks `RQ-1`, Story 5.8 authorization, and Story 8.3 deletion handling; no narrower Story 8.3 branch is granted (`prd.md:1085`).
- The register materializes the same unconditional `StoryAuthorization:8.3` affected evaluation (`launch-readiness-register.md:103`), and the spine says Stories 5.8 and 8.3 authorization fail closed (`ARCHITECTURE-SPINE.md:1002`).
- Story 8.3's dependency and evidence rows instead require the decision only when Agent Instructions or configuration audit are in the particular deletion scope, allowing other Story 8.3 work/authorization to proceed (`epics.md:2913-2915,2929-2932,2963-2970`).

**Divergence / impact**

One delivery gate blocks Story 8.3 until the Agent-level protection/all-copies inventory is decided; another can authorize or complete a reduced deletion story for interaction-only sets. That can let the deletion story claim readiness without the Product-required offboarding/instruction inventory and gives later builders two definitions of Story 8.3 completion.

**Required correction — RECONCILE EPICS TO THE BOUND PRD.** Make the Story 8.3 authorization dependency unconditional, or obtain a Product amendment that explicitly permits an independently completable interaction-only branch and then version the story/evidence boundary. Do not infer the narrower scope from current key placement.

## Medium

### M-R9-1 — Architecture-owned assumptions still lack literal retirement dates

**Classification:** unresolved governance input; safely fail-closed.

The PRD requires every Architecture-owned `RQ-1` assumption to have a co-owner-approved literal calendar date and says a non-date blocks on that ground (`prd.md:730`). `ARCH-A-1` through `ARCH-A-4`, `ARCH-A-6` through `ARCH-A-8`, `ARCH-A-11`, `ARCH-A-12`, and `ARCH-A-14` remain unscheduled or milestone-only (`ARCHITECTURE-SPINE.md:1054-1067`). Obtain approved dates or retire/replace the rows; do not invent dates or remove blockers.

### M-R9-2 — `PostingPending` timeout remains implementation-selected

**Classification:** explicit unresolved Architecture/Product input; safely visible.

AD-5 requires a stored attempt deadline no shorter than the Conversations posting timeout but fixes no duration, allowed range, configuration owner, or snapshot version. `ARCH-A-14` records that gap without a calendar retirement date (`ARCHITECTURE-SPINE.md:195,1067`). Each stored deadline is deterministic, keeping this below High, but different builders can choose different lookup/failure times. Resolve before the posting/recovery story; do not accept an implementation-local default as retirement evidence.

### M-R9-3 — Two cited 2026-09-12 reviewer sources remain absent

**Classification:** source reproducibility defect only.

Spine frontmatter cites `reviews/review-2026-09-12-security-data-integrity-v3.md` and `reviews/review-2026-09-12-brownfield-drift-v3.md` (`ARCHITECTURE-SPINE.md:79-80`), but neither file exists in the frozen reviews directory. Generate the cited reports or remove/replace the citations with artifacts that exist; do not infer their conclusions.

### M-R9-4 — Story 8.1 declares the export store both unconditional and conditional

**Classification:** executable-backlog dependency wording defect.

Epic 8's topology and Story 8.1 evidence manifest say `EXT-EXPORT-STORE-1` applies only when the frozen hold set contains committed export artifacts (`epics.md:2763,2825-2832`). The story's main dependency paragraph says the export store must be Available for all Story 8.1 live retention/hold work (`epics.md:2773-2777`). This safely over-blocks but gives planning tools two readiness answers. Make the dependency paragraph use the same conditional scope as the evidence manifest.

### M-R9-5 — Spine scope metadata and its handoff table omit the newly bound recorder decision

**Classification:** re-distillation and traceability defect.

The spine frontmatter still says it binds only PRD `OQ-1..OQ-32`, despite current AD-17 explicitly binding OQ-33 and the recovery rules binding OQ-34 (`ARCHITECTURE-SPINE.md:11-14`; `prd.md:1087-1088`). The dedicated Blocking Open Decisions table includes the OQ-34 hold-cancellation decision but omits `OD-RELEASE-RECORDER-SCOPE-1`, although AD-17, the PRD, register, epics, and memlog all name it (`ARCHITECTURE-SPINE.md:992-1004`). Update metadata to `OQ-1..OQ-34` and add the existing recorder decision/safe state to the table without changing its ID or choosing an authority source.

## Low

None.

## v8 Critical/High Closure Audit

| v8 finding | v9 disposition |
| --- | --- |
| C-R8-1 generic legal-hold abort/recovery source | **Materially closed; one High narrative residue.** AD-17, matrix rows, Story 8.1, and PRD OQ-34 now make ordinary Resume and hold Abort branch-specific. H-R9-3 is the remaining generic NFR-11 paragraph; the corrected hold matrix prevents the former fail-open unpin, so no Critical remains. |
| H-R8-1 missing runtime decision catalog / DecisionId rule | **Closed, with new lifecycle/status follow-through.** `ArchitectureDecisionCatalog` and literal stable `DecisionId` now exist. H-R9-1 and H-R9-2 concern activation and public blocker vocabulary, not inventory/key absence. |
| H-R8-2 Release Operator platform scope undefined | **Closed as a surfaced Product blocker.** PRD OQ-33 and `OD-RELEASE-RECORDER-SCOPE-1` deny every principal until Product selects the authority source; Story 5.5 and conditional Parties dependency are synchronized. M-R9-5 is summary traceability only. |
| H-R8-3 recorder could count as approver | **Closed.** Stable actor inequality is unconditional in AD-17, matrix, register, and Story 5.5. |
| H-R8-4 Party-free Administrator forced through Party history | **Closed.** AD-22 and AD-30 use the same tagged User/Administrator/Platform evidence union and prohibit invented Parties. |
| H-R8-5 export store lacked pin/unpin/expiry refusal seam | **Closed.** `EXT-EXPORT-STORE-1` now contracts exact idempotent pin/unpin/outcome lookup, fence/decision binding, lifecycle refusal, backup/restore, and lost-ack evidence. |

## Authoritative 2026-09-12 Critical/High Closure Audit

| Authoritative finding | v9 disposition |
| --- | --- |
| C-1 weaker safety-policy retry | **Closed.** Snapshot and current policies are conjunctive; dominance requires a machine-checkable equivalence proof. |
| C-2 hold/deletion exclusion | **Closed at common fence/data-loss boundary.** `ProtectionFence`, frozen sets, prepare/arm/destruction, export cleanup, and exact receipts prevent partial irreversible work. H-R9-4 is a fail-closed matrix scope contradiction on the ordinary path. |
| C-3 bootstrap/repair deadlock | **Closed.** EventStore observation bootstrap and target-aware direct preconditions are explicit. |
| H-1 Eligible-Approver recheck | **Closed.** Single-flight scheduled recheck, freshness, two-pass empty handling, and typed unavailable outcomes are bound. |
| H-2 safety-rescan ownership | **Closed.** Durable epoch/index, finite tenant/conversation manifests, fenced coordinator, bounded workers/call wait, and unindexed initialization are defined. |
| H-3 human-only Approvers | **Closed.** Parties-owned classification/liveness and actor binding fail closed. |
| H-4 conflated ledger lifetimes | **Closed.** Rate, original-caller open lease, and monthly money have distinct owners and terminal rules. |
| H-5 human separation identity | **Closed.** Stable actor identity and tagged historical authority evidence are consistent across principal kinds. |
| H-6 proposal-index crash consistency | **Closed.** Interaction source revision, outbox/index acknowledgement, high-water, freeze, and reconciliation are explicit. |
| H-7 indivisible Conversations dependency | **Closed.** Retraction is a separate optional dependency and Automatic-mode decision. |
| H-8 trusted-envelope cryptography/replay | **Closed.** Canonical MAC, logical/delivery ids, replay registrar, retention, rotation/revocation, security spool, and ACLs are bound. |
| H-9 export artifact lifecycle | **Closed at architecture/seam level.** Store, AEAD, canonical signed manifest, index, pin/unpin, expiry/hold fence, backup/restore, cleanup, and purge receipts are explicit; the Product lifecycle choice stays Open. |
| H-10 current Dapr reality | **Closed.** Parent `1.18.5`, dirty-checkout `1.18.7`, present transitive Client/ASP.NET exposure, and future Workflow adoption remain distinct. |
| H-11 public parity assertion | **Closed.** Target completion contracts and current implementation debt are separated. |
| H-12 sprint/dependency history | **Closed as visible delivery decision.** The discrepancy is not presented as architecture conformance. |

## Requested Cross-Contract Audit

| Boundary | Result | Evidence / disposition |
| --- | --- | --- |
| Decision self-supersession | **Fail at catalog activation only** | Effective/pending union, predecessor/root authorization, quorum, actor separation, and narrowing rules pass. H-R9-1 leaves no executable catalog activation transition; H-R9-2 leaves its absent status outside the public vocabulary. |
| Release Operator recorder authority | **Pass as unresolved** | OQ-33/OD scope denies all principals, preserves the six-role PRD, and makes Parties consumption conditional on the eventual selected branch. No tenant/source was invented. |
| Prepare recovery branch decisions | **Fail narrowly** | AD-17/matrix/story rules are branch-specific and phase-pinned; H-R9-3 is the stale contradictory NFR-11 paragraph. |
| Hold/export preservation and expiry | **Pass** | Hold intent/store linearization covers artifacts, manifests, indexes, wrapped keys, provider TTL, backups, restore cleanup, exact pin/unpin and restrictive unknown outcomes. |
| Actor-evidence union | **Pass** | Party-bearing User, Party-free Administrator, and Party-free Platform evidence use distinct authoritative sources and one stable actor comparison. |
| Deferred PRD decisions | **Fail at affected scopes** | OQ-18 and OQ-23 scopes pass; H-R9-5 incorrectly adds global `RQ-1` to OQ-34 and H-R9-6 weakens OQ-31's Story 8.3 block. Outcomes remain unchosen. |
| Rate/open/budget races | **Pass with Product blocker** | Rate and open preparation deadlines are bounded; interaction decisions and owner acknowledgements are immutable; OQ-32 blocks before either owner. |
| Posting authorization/effect order | **Pass as architecture** | Approval, committed `PostingPending`, Conversations append, and result are distinct durable steps. Current opposite ordering is named delivery debt. |
| Safety cohort enumeration | **Pass** | Policy publication freezes provision-event tenants; directory checkpoint/count/hash enumerates indexed Conversations; concurrent/new tenants and unindexed Conversations initialize deterministically. |
| Readiness self-bootstrap | **Pass** | EventStore observe/repair has a dedicated target-limited gate-free variant; other observations require EventStore but not their own target gate. |
| Kill-switch pull/release | **Pass** | Incident confirmation/review decision and switch transitions share one `TenantGovernancePolicy` revision; containment paths are direct and release remains separately reviewed/gated. |
| Public statuses | **Fail narrowly** | Proposal, membership, readiness, and operational vocabularies otherwise align; H-R9-2 is the one unregistered catalog blocker code. |
| Trusted replay/security recorder | **Pass** | First-seen registration precedes target idempotency and only the two ACL-confined pre-command capabilities exist. |
| Export/hold/deletion fence | **Fail at one matrix row** | Preservation, cleanup, receipts, and one-way destruction pass; H-R9-4 makes the no-hold start contradict the spine/story. |
| Architecture versus delivery debt | **Pass** | Absent target aggregates/contracts/seams are explicitly assigned work. No current implementation gap is counted again as an architecture defect. |

## Good-Spine Checklist

| Criterion | Result | Reason |
| --- | --- | --- |
| Fixes the real divergence points for the level below | **Fail** | Six High lifecycle, recovery, gate, status, and decision-scope contradictions remain. |
| Every AD Rule is enforceable and prevents divergence | **Fail** | Catalog activation has no declared command; register recovery and deletion-start rows conflict with the owning ADs. |
| Deferred/open items are safe to defer | **Fail at scope, not outcome** | Safe states are fail-closed and no choice is invented, but OQ-34 and OQ-31 affected scopes are inconsistent. |
| Named technology is verified-current | **Pass** | Root gitlinks/package authority, SDK/Dapr distinction, and unselected Provider/Agent Framework claims match repository authority. |
| Brownfield ratification | **Pass** | Target-versus-current gaps are named as delivery debt and assigned to stories/dependencies. |
| Bound PRD capability and authority coverage | **Fail** | H-R9-5/H-R9-6 conflict with explicit deferred-decision scope; M-R9-5 omits OQ-33/OQ-34 from metadata. |
| Inherited parent constraints | **N/A / no weakening found** | No parent architecture spine is declared; repository instructions and parent gitlink authority were respected. |
| State, mutation, and recovery ownership | **Fail** | Owners are named, but catalog activation and the generic recovery paragraph are not executable consistently. |
| Security, tenant isolation, audit, and data-loss boundaries | **Pass with fail-closed High defects** | No current finding authorizes unsafe data loss; human evidence, envelope security, hold pins, deletion receipts, and tenant routing are closed. H-R9-4 over-blocks rather than bypasses. |
| API/integration/operations/environment/provider dimensions | **Fail narrowly** | Coverage is complete, but H-R9-2 breaks blocker interoperability and H-R9-1 leaves the runtime decision lifecycle incomplete. |
| Structural handoff and source traceability | **Fail narrowly** | M-R9-3 and M-R9-5 leave absent sources and incomplete scope/open-decision summaries. Seed, diagrams, maps, and debt table otherwise align. |
| Mechanical validity | **Pass** | Deterministic lint returned zero findings. |

## Architecture Defects Versus Implementation Debt

The eleven counted findings are architecture/register/backlog/source-contract defects or explicitly unresolved governance inputs. Missing implementation is not counted as a second defect.

| Current repository reality | Classification / existing disposition |
| --- | --- |
| No runtime decision catalog/records/projection, safety epoch/index, trusted replay, security recorder/spool, split rate/open/budget ledgers, common protection fence, export store, or Dapr Workflow owner | **Implementation debt.** Assigned across Stories 5.4-5.8, 6.1, 6.3-6.4, and 8.1-8.4 plus external records. H-R9-1/H-R9-2 concern the target contract those builders need, not absent code. |
| Current approval flow appends to Conversations before EventStore authorization and batches proposal states | **Implementation debt.** Story 7.4 plus `IMPLEMENTATION-CONVENTIONS.md` owns the durable-before-post refactor and evidence. |
| Current public contract/status vocabulary is incomplete | **Implementation debt.** AD-15 and the owning stories call it out explicitly; H-R9-2 is separately a defect in the target vocabulary itself. |
| All twelve external dependency records are `Uncommitted` | **External delivery state and launch blocker.** Their unavailability is not an architecture defect. |
| Parent Dapr package family remains `1.18.5`; dirty Builds checkout is `1.18.7`; Workflow is absent | **Recorded delivery/security decision.** `ARCH-A-15` and `OD-DAPR-SECURITY-1` remain Open. |
| Story 5.1/5.2 tracking/evidence conflict and legacy conformance tests | **Delivery/history debt.** `OD-SPRINT-5.1-5.2-1` and Story 5.6 own correction; no AD change is inferred. |
| bUnit remains `2.9.0` and test-stack pins differ from the imported catalog | **Maintenance debt/assumption.** Recorded under ARCH-A-4/Story 5.6; no new compatibility or security failure was established by this review. |

## Unresolved Product / Governance / Security Decisions — No Outcome Selected

The following remain questions/blockers, not architecture-selected outcomes:

| Decision | Current safe scope |
| --- | --- |
| `OD-HOLD-DELETION-PRECEDENCE-1` | Armed-deletion/late-hold contention and `RQ-1`; ordinary pre-arm deferral/no-hold deletion remains available. |
| `OD-HOLD-PREPARE-CANCELLATION-1` / PRD OQ-34 | Hold-cancellation/Abort branch and Story 8.1 cancellation authorization only; H-R9-5 removes the unsupported global `RQ-1` expansion. |
| `OD-EXPORT-LIFECYCLE-1` | Export/committed-artifact hold/deletion lifecycle, store availability, and `RQ-1`. |
| `OD-RATE-CONCURRENCY-CONSUMPTION-1` / OQ-32 | Original-call joint rate/open admission and Story 6.4 only. |
| `OD-DAPR-SECURITY-1` | Dapr package-family upgrade/exception, Workflow adoption, and `RQ-1`. |
| `OD-SPRINT-5.1-5.2-1` | Delivery-history/dependent-story authorization only. |
| `OD-PRD-OQ18-HISTORICAL-SAFETY-1` | Historical-content treatment and `RQ-1`; current whole-history fail-closed behavior remains. |
| `OD-PRD-OQ23-AUTOMATIC-RETRACTION-1` | First Automatic-mode tenant only; no global `RQ-1` or Confirmation-mode block. |
| `OD-PRD-OQ31-INSTRUCTION-PROTECTION-1` | `RQ-1`, Story 5.8, and—per current bound PRD/register—Story 8.3 authorization. H-R9-6 requires the epics to match unless Product changes the scope. |
| `OD-RELEASE-RECORDER-SCOPE-1` / OQ-33 | Decision-recording principal/source, Story 5.5, and `RQ-1`; all principals remain denied. |

## Required Correction Order

1. Make the decision catalog executable and interoperable: add exact activation semantics and reconcile `DecisionCatalogMissing` to the closed Product/public vocabulary.
2. Repair the two current register contradictions: branch-specific NFR-11 recovery source and no-hold versus armed-contention deletion-start variants.
3. Reconcile decision affected scopes to the bound PRD: remove OQ-34's unsupported `RQ-1` scope and restore OQ-31's Story 8.3 authorization block, unless Product explicitly amends either decision.
4. Correct Story 8.1 dependency wording, update OQ binding/open-decision summaries, and restore source reproducibility.
5. Obtain real dates and resolve the posting-timeout authority without inventing values, rerun lint, and rerun the complete reviewer gate.

## Gate Conclusion

The frozen v9 artifacts do **not** pass: **0 Critical, 6 High, 5 Medium, 0 Low**. The architecture is substantially safer and all authoritative 2026-09-12 Critical/High findings are closed, but the current decision-catalog lifecycle, recovery text, deletion-start matrix, and Product-decision affected scopes still allow independently built units to diverge. Preserve all AD and `DecisionId` values and select none of the unresolved Product/Governance/Security outcomes while applying the corrections.
