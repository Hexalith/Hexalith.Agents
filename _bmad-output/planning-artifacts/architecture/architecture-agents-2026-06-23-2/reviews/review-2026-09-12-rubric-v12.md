---
name: Hexalith Agents good-spine rubric reviewer gate v12
type: architecture-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
lens: complete good-spine rubric walker
intent: validate-read-only
verdict: fail
critical: 1
high: 3
medium: 2
low: 0
lint_ok: true
---

# Good-Spine Rubric Walker — v12

## Verdict

**FAIL — 1 Critical, 3 High, 2 Medium, 0 Low.** The re-distilled workspace preserves the original AD ids, closes the authoritative validation report's original Critical/High defects on their stated terms, and materially lands most v10/v11/specialist corrections. It does not yet provide a safe implementation handoff. The new Conversation-owned effect lease can be revoked on one stream while the original worker subsequently authorizes or begins its effect on another stream, so `Effective` is not yet a linearizable no-new-effect cut. Three fail-closed High defects also remain in migration bootstrap, unresolved legacy-plaintext disposition, and export-bearing deletion phase pinning.

PASS requires zero Critical and zero High.

## Frozen Inputs And Method

The following reviewed inputs were hashed before review, re-read from the frozen workspace, and verified unchanged after the report was written:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `f77a939e0802e0c75375281a9f43a333df318de29f8b0fc4f0e0da8ba797bd25` |
| `IMPLEMENTATION-CONVENTIONS.md` | `d40aa9745f80b9d0b6ac65a0e5b37f7fa7b5ed1b6f8ff19c11725192d23d325b` |
| `.memlog.md` | `63bf950c91f781760986d9bb0789af931396313305c1f64739435cfc7e3fbbec` |
| bound PRD | `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb` |
| `epics.md` | `28b517b0d1d76463672e9852748a4f6d1e54362f827e38391ab0b57d0c1e13a2` |
| `external-dependency-register.md` | `a63a8f69384ae3b67d4f324ccca8888195eb4caba1b415955af93f303acb66a5` |
| `launch-readiness-register.md` | `164278a340b71b84831e67addd9d7c809032c157853a59167845bb4b2248bb32` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

The review walked the full good-spine checklist rather than treating prior findings as the checklist. It inspected all ADs, conventions, source and capability maps, open/deferred/assumption registers, bound PRD, replacement epics, dependency and readiness contracts, the read-only memlog, v10/v11/specialist findings, and focused repository reality. The five v12 reviewer/specialist paths intentionally cited in frontmatter are concurrent gate deliverables and were not treated as missing-source defects; every other declared local source resolves.

## Deterministic Linter

Command:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: **PASS** — `ok: true`, `total_findings: 0`, no severity entries. AD-1 through AD-31 each occur once; no AD was renumbered. Mechanical correctness does not close the semantic findings below.

## Critical

### C-R12-1 — `RevokedBeforeEffect` does not linearize with the effect it claims never began

**Classification:** architecture/data-integrity defect; recurrence of the v11/specialist effect-cutover failure in the new lease protocol, not implementation debt.

**Evidence:** AD-2 gives `ConversationAgentState` the `Active|Settled|RevokedBeforeEffect` lease and says `Effective` may follow when manifested leases are closed (`ARCHITECTURE-SPINE.md:189`). AD-6 allows a pre-Closing lease either to complete or to record `RevokedBeforeEffect` after an authoritative lookup says the effect did not begin (`:227`). AD-12 claims this makes `Effective` unable to race (`:291`). In the normative matrix, however, acquisition/revocation mutates `ConversationAgentState`, while `StartWorkflowUnderEffectLease`, `ContextAndSafetyUnderEffectLease`, `ProviderInvocation:AuthorizeUnderEffectLease`, proposal mutation, and `ConversationPosting:BeginUnderEffectLease` authorize or mutate a different owner using only an `Active...RevisionExact` observation (`launch-readiness-register.md:236-242`). `ActivateBarrierEffective` accepts a revoked lease (`:262`). No same-owner `BeginEffect`/lease-consumption transition or acknowledgement from the original worker makes future use impossible.

**Race:** worker A acquires a lease; Closing manifests it; deletion worker B observes that the workflow/provider/post/mutation has not begun and appends `RevokedBeforeEffect`; B then appends `Effective`; A, holding previously valid Active evidence, appends the interaction authorization or begins the external effect. A fresh re-read does not solve this because the re-read and downstream append/effect are again on different owners. The result violates the promised post-`Effective` cut and can create content or a Provider/Conversation side effect after the deletion inventory has frozen and erasure has started.

**Disposition — AUTOFIX before Story 6.1/8.3 handoff.** Add one same-owner transition that makes effect start and revocation mutually exclusive at a `ConversationAgentState` expected revision, for example `Acquired -> EffectCommitted` versus `Acquired -> RevokedBeforeEffect`. Only `EffectCommitted` may authorize the exact downstream append/effect; deletion may never revoke it and must wait for its result/lookup/settlement/acknowledgement. For internal mutations and workflow start, the committed lease must carry a recoverable deterministic target step so a crash is driven to its one outcome. For Provider/posting, preserve the existing authoritative lookup rules. Add failure injection between Active observation, the same-owner commit/revoke race, target authorization, effect start, result, and `Effective`. An unbound read or momentary absence is not sufficient.

## High

### H-R12-1 — The directory migration's first command depends on the gate that migration itself must establish

**Classification:** architecture implementability defect; fail-closed bootstrap deadlock.

**Evidence:** `LR-AUDIT-PROTECTION-DELETION` explicitly includes legacy-directory migration/readiness and is invalidated by directory migration changes (`launch-readiness-register.md:133`). `InteractionDirectoryMigration:BeginQuiescedCutover` nevertheless requires that complete gate (`:231`). AD-17's bootstrap rule says a command must omit the whole circular gate and replace it with closed direct preconditions (`ARCHITECTURE-SPINE.md:343`), but the new migration row does not do so.

**Impact:** after deploying the new profile, the gate cannot pass until `DirectoryReady`, while `DirectoryReady` cannot be reached because `BeginQuiescedCutover` cannot run. A strict implementation blocks all directory-first calls and Conversation deletion indefinitely; a permissive implementation invents a code-local exception and violates the normative matrix.

**Disposition — AUTOFIX.** Make `BeginQuiescedCutover` an explicit bootstrap variant: omit the whole circular `LR-AUDIT-PROTECTION-DELETION` gate, retain every unrelated gate, and replace the omitted gate with a closed set of direct owner/revision preconditions covering the protection, audit/spool, topology, and legacy-inventory conditions actually required to begin safely. Add empty, stale, blocked, repair, and unrelated-gate failure fixtures. This selects no Product outcome.

### H-R12-2 — Legacy plaintext has a blocker but no materialized disposition decision or executable owner

**Classification:** unresolved Product/Governance choice that is not yet surfaced through the architecture's own decision mechanism.

**Evidence:** AD-2/AD-7 say legacy plaintext blocks calls and deletion pending a “separately authorized migration or eradication procedure” (`ARCHITECTURE-SPINE.md:189,239`); the completion row repeats that blocker but defines no operation family for resolving it (`launch-readiness-register.md:233`). Story 5.8 says Architecture deliberately did not choose migration versus eradication (`epics.md:1741-1744`). The spine's Blocking Open Decisions table has no corresponding decision (`ARCHITECTURE-SPINE.md:1072-1086`), so AD-17's runtime decision catalog cannot name it, and Stories 5.8/6.1 have no authorized procedure, owner, version, or completion evidence that can remove the blocker.

**Impact:** any tenant whose finite inventory is not empty and contains legacy plaintext is permanently unable to complete directory migration, accept calls, propagate Conversation deletion, or qualify release. Separate teams may invent re-encryption, eradication, or an unsupported “grandfathered” exception despite materially different retention and audit consequences.

**Disposition — DISCUSS, then materialize without choosing the outcome in Architecture.** Add a new stable open-decision record, preserving every existing AD/OD id, owned by Product + Governance + Security with the EventStore/Platform maintainer as technical co-owner. Its safe state remains the current blocker. Bind its affected evaluations to legacy-protection remediation, directory completion, Stories 5.8/6.1/8.3 as applicable, and `RQ-1`; add closed authorize/execute/result/recovery variants only after the owners select migration, eradication, or another compliant outcome, with all-copy/snapshot/cache/backup receipts and replay proof. Do not infer that current checked-in code implies an empty deployed inventory.

### H-R12-3 — Export-bearing deletion is declared phase-pinned but `DestructionStarted` re-enters mutable initial gates and decision unions

**Classification:** architecture consistency/recovery defect; safe over-block rather than an unsafe deletion grant.

**Evidence:** AD-17 says an immutable prepare disposition phase-pins recorded branch recovery and later decision successors cannot block it (`ARCHITECTURE-SPINE.md:349,355`; `launch-readiness-register.md:106,114`). AD-22 and Story 8.3 say an export-bearing deletion pins the lifecycle/store values through prepare, arm, destruction, purge, and completion (`ARCHITECTURE-SPINE.md:397`; `epics.md:2989-2992`). `DeletionPrepareBranchDecision` and `DeletionPrepareResumeRecovery` are correctly gate-free (`launch-readiness-register.md:266-267`), yet `GovernanceProtection:DeletionDestructionStarted` reuses the full mutable `DeletionPrepare` GateId set (`:269`) and `OD-EXPORT-LIFECYCLE-1` still lists that evaluation (`:97`). A later pending/effective decision successor or an unrelated mutable gate can therefore block the already-recorded branch before its irreversible linearization despite the promised phase pin.

**Impact:** a deletion that has already frozen its export-bearing set, phase-pinned the exact lifecycle/store contract, recorded `Resume`, and acquired reversible preparations can be stranded indefinitely or behave differently depending on whether an implementation follows the direct recorded-version precondition or the current gate/decision union.

**Disposition — AUTOFIX without selecting `OD-EXPORT-LIFECYCLE-1`.** Make `DeletionDestructionStarted` the dedicated continuation of the recorded Resume/prepare branch with `RegistryCheckpoint.NotRead`, exact frozen owner/disposition/fence revisions, recorded no-copy proof or prepare-pinned lifecycle/store contract, direct dependency availability, and the conditional armed-contention decision only. Remove it from later export-lifecycle successor evaluation once that branch is pinned, while retaining the approved recorded version. If current mutable reauthorization is instead intended, remove the phase-pin promise and surface the resulting Product/Governance recovery semantics for discussion; the present documents cannot assert both.

## Medium

### M-R12-1 — Architecture-owned `RQ-1` assumptions still lack literal calendar retirement dates

PRD FR-28 requires each Architecture-owned release-blocking assumption to have a co-owner-approved calendar target. ARCH-A-1, -2, -3, the remaining test-stack portion of -4, -6, -7, -8, -11, -12, and -14 remain “Unscheduled” or milestone-only (`ARCHITECTURE-SPINE.md:1137-1152`). Owners, retirement conditions, and blockers are explicit, so the system fails closed and this is Medium governance debt. Obtain dates from the named co-owners; Architecture must not invent them.

### M-R12-2 — The `PostingPending` timeout remains intentionally unresolved

AD-5 fixes only a stored deadline “no shorter than” the Conversations seam timeout, and ARCH-A-14 has no concrete duration or final configuration authority (`ARCHITECTURE-SPINE.md:209,1152`). Story 7.4 blocks its handoff until retirement, so this no longer permits silent implementation divergence; it remains a Medium Architecture/Product decision to resolve before that story.

## Low

None.

## Authoritative 2026-09-12 Critical/High Closure Audit

| Authoritative finding | v12 disposition |
| --- | --- |
| C-1 weaker safety evaluation | **Closed.** AD-20 requires conjunctive snapshot-plus-current evaluation, permits only proved-equivalent dominance, and defines deterministic pre/post-authorization failure settlement. |
| C-2 hold/deletion exclusion | **Closed.** `ProtectionFence` owns prepare, arm, destruction and export commit ordering with complete frozen sets and recovery receipts. |
| C-3 bootstrap/matrix scope | **Closed for the original families.** Matrix v4 has target-aware platform/tenant bootstrap and self-bootstrap rows. H-R12-1 is a newly introduced migration row that regresses the same rule; it does not erase the original correction. |
| H-1 scheduled Approver recheck | **Closed.** AD-8 binds owner, single-flight cadence, authoritative evidence, two-pass empty handling, state-specific outcomes, and unavailable retry. |
| H-2 distributed safety rescan | **Closed.** `SafetyVerdictEpoch`/`SafetyVerdictIndex`, frozen cohorts, bounded fenced coordinator, on-demand initialization, and `RescanPending` behavior are explicit. |
| H-3 human-only Approvers | **Closed.** Configuration and each runtime resolution require fresh active human Parties evidence. |
| H-4 ledger lifetime conflation | **Closed.** Rate, open-interaction, and monetary-budget ledgers have distinct owners, partitions, decisions, expiry/release, and recovery. The still-open rate/concurrency consumption outcome is correctly surfaced. |
| H-5 human identity/separation | **Closed.** Every human principal carries stable `AuthenticatedHumanActorId`; evidence is a closed User/Administrator/Platform union and separation is actor-based. |
| H-6 proposal index crash consistency | **Closed.** `AgentInteraction` is truth, with source-revision outbox, high-water materialization, removal checkpoint/reconciliation, and recovery. |
| H-7 indivisible Conversations/retraction seam | **Closed.** Core `EXT-CONV-AI-1` and optional `EXT-CONV-RETRACTION-1` are separate records with separate consumers. |
| H-8 trusted-envelope HMAC/replay | **Closed.** Canonical fields, lifetime, key rotation/revocation, logical/delivery identities, reserved-system first-seen owner, ACL, and lost-ack recovery are bound. |
| H-9 export artifact lifecycle/signature | **Closed subject to the intentionally Open lifecycle decision.** The store owner, key hierarchy, immutable target, index, fence, purge/restore/hold contract, RFC 8785 + ES256 manifest, and exact compatibility evidence are explicit. |
| H-10 current Dapr exposure | **Closed as architecture truth.** The spine distinguishes current transitive `1.18.5` exposure from future Workflow adoption and surfaces the upgrade/exception decision. |
| H-11 public-contract parity overclaim | **Closed.** AD-15 and the debt table distinguish required completion parity from shipped repository contracts. |
| H-12 sprint/evidence contradiction | **Closed as architecture-vs-delivery classification.** The discrepancy remains visible under `OD-SPRINT-5.1-5.2-1`; no false completion is inferred. |

## v10, v11, And Specialist Critical/High Correction Audit

| Correction area | Result |
| --- | --- |
| v10 conditional export-free deletion | **Closed.** The no-copy proof branch neither reads nor fabricates the lifecycle decision; export-bearing sets remain conditional. |
| v10 Conversation source feed, origin union, permanent directory/barrier, closed workflow scope, pre-arm deferral, recorder bootstrap, export prepare pin | **Closed.** Source acknowledgement, stable signal id, discriminated origin, target-limited workflow, gate-free bootstrap, and prepare version/store pin are consistent across spine, registers, and epics. |
| v11/specialist linearizable effect cutover | **Not closed: C-R12-1.** Acquisition races with Closing, but revocation still does not race with downstream start on the same owner. |
| v11 export-key authorize/effect/result/recovery | **Closed.** AD-22, AD-29, AD-30, matrix rows, `EXT-SECRETS-1`, and Story 8.2 bind requester, commit revision, purpose/audience, expiry, phase-pinned versions, deterministic id, direct delivery, outcome lookup, and lost acknowledgement. |
| v11 export-bearing deletion precondition and phase pin | **Partially closed.** Prepare and purge/completion are conditional and pinned; H-R12-3 identifies the remaining `DestructionStarted` contradiction. |
| v11 legacy directory rollout | **Partially closed.** Finite quiesced migration, backfill, direct-create freeze, high-water reconciliation and late-write invalidation exist; H-R12-1/H-R12-2 block executable completion. |
| brownfield assumption-index, AD-7/AD-13 order, target-key alias, export-key delivery | **Closed.** The index is singular at 9; intake is directory-first; the provider-neutral alias binds the source outbox to the target interaction DEK and requires dual-stream `Erased`/cross-tenant negative evidence; delivery is durable. |

## Requested Adversarial Challenge Results

| Focus | Result |
| --- | --- |
| Effect-lease Closing/Effective cutover | **Critical finding C-R12-1.** Closing linearizes acquisitions, but start/authorization and revocation remain cross-owner. |
| Legacy directory/protection migration | **High findings H-R12-1/H-R12-2.** The inventory/backfill mechanics are strong, but bootstrap is circular and plaintext disposition is not materialized. |
| AD-7/AD-13 order | **Pass.** Both define membership as the last pre-permit step, permit/outbox registration, interaction creation, workflow-start lease, rate/open admission, Confirmation-only Approver resolution, context/token/safety, reservation, acceptance, and then Provider authorization. |
| Target-key-alias contract | **Pass at architecture altitude.** AD-2/AD-7, the permit row, `EXT-PROTECTION-1`, Story 6.1, and repository `KeyAlias` metadata converge on an explicit tenant-bound interaction-DEK alias with dual-stream erased replay and substitution/lost-ack tests. The concrete engine remains an Uncommitted external dependency rather than hidden shipped behavior. |
| Export-key delivery | **Pass.** Authorization, effect, result, identity, direct custodian delivery, expiry/revocation, and lost-ack branches are closed without exposing key bytes. |
| Export-bearing deletion phase pin | **High finding H-R12-3.** Prepare/recovery is pinned, but `DestructionStarted` still re-enters mutable initial evaluation. |

## Whole Good-Spine Checklist

| Dimension | Result |
| --- | --- |
| Real downstream divergence points | **Fail.** C-R12-1 and H-R12-1..H-R12-3 still permit unsafe or incompatible implementations. |
| AD enforceability / stated prevention | **Fail.** The lease's revoke/start rule is not mechanically enforceable across its two owners. Other AD rules are specific and testable. |
| Ownership, recovery, security, data loss | **Fail.** The common fence, delivery, source acknowledgement, identity, erasure and replay boundaries are strong, but C-R12-1 can cross the effective deletion cut. |
| PRD capability traceability | **Pass subject to findings.** FR-1..FR-34 and NFR-1..NFR-14 are mapped; no Product outcome was invented. The legacy plaintext choice must be surfaced rather than selected. |
| Brownfield ratification | **Pass.** Root gitlink authority versus modified submodule heads, no-op EventStore protection, existing direct interactions, absent Dapr Workflow/effect leases/fence/decision records, incomplete public vocabulary, and current transitive Dapr exposure are accurately distinguished as delivery state. |
| Named technology / source currency | **Pass for the rubric scope.** Exact root pins and parent gitlinks match the stated authority; external targets remain Uncommitted/Unselected where no accepted implementation exists. Concurrent v12 review outputs are intentionally pending; every other local source resolves. |
| Deployment, environments, infrastructure, operations | **Pass.** Platform host ownership, Dapr runtime boundary, readiness profiles, capacity, telemetry, backup/restore, recovery clocks, and external ownership are explicit. |
| Deferred/open dimensions | **Fail only for H-R12-2; Medium governance debt otherwise.** Existing ODs are owner-scoped and fail closed. No Deferred item silently enables V1 divergence. The legacy plaintext disposition is acknowledged but not entered into that mechanism. |
| Architecture versus implementation debt | **Pass.** The debt table does not weaken the target; no build/test/live-seam completion is claimed. |
| Mechanical handoff | **Pass.** Lint clean, stable AD inventory, source paths resolved under the stated exception. |

## Unresolved Product/Governance Decisions

The following existing decisions remain deliberately unresolved and were **not** selected by this review: `OD-HOLD-DELETION-PRECEDENCE-1`, `OD-HOLD-PREPARE-CANCELLATION-1`, `OD-EXPORT-LIFECYCLE-1`, `OD-RATE-CONCURRENCY-CONSUMPTION-1`, `OD-DAPR-SECURITY-1`, `OD-SPRINT-5.1-5.2-1`, `OD-PRD-OQ18-HISTORICAL-SAFETY-1`, `OD-PRD-OQ23-AUTOMATIC-RETRACTION-1`, `OD-PRD-OQ31-INSTRUCTION-PROTECTION-1`, and `OD-RELEASE-RECORDER-SCOPE-1`. Their safe states, owners, and affected evaluations are explicit.

H-R12-2 is the additional unresolved choice that must be surfaced for discussion: how deployed legacy plaintext is lawfully migrated, eradicated, or otherwise disposed of. The current safe outcome remains `PayloadProtectionUnavailable`; the reviewer does not choose a branch.

## Architecture Defects Versus Delivery Debt

- **Architecture defects requiring correction:** C-R12-1 and H-R12-1/H-R12-3 are protocol/matrix contradictions; H-R12-2 is missing decision materialization; M-R12-1/M-R12-2 are explicit governance debt.
- **Existing implementation/delivery debt, not extra findings:** the repository has no directory migration, permit/outbox/effect lease/barrier, protection fence, architecture-decision records, durable export/key-delivery protocol, safety epoch, or three-ledger implementation; EventStore still supplies a no-op default protection service and only metadata hooks; Dapr Workflow is absent; current public vocabulary is incomplete; the root Builds gitlink remains at the `1.18.5` family while the modified checkout carries `1.18.7`; the sprint 5.1/5.2 evidence discrepancy remains tracked. These are already assigned in `Architecture Contract Versus Delivery Debt` and the replacement epics.
- **External blockers, not architecture ambiguity:** the relevant `EXT-*` records remain `Uncommitted`, and no Live or build-success claim is made.

## Required Correction Order

1. Close C-R12-1 by making effect commit and revocation contend on one authoritative lease revision; then rerun every before/after Closing/Effective failure injection.
2. Remove H-R12-1's circular migration gate and add its complete direct bootstrap preconditions and fixtures.
3. Surface H-R12-2 for Product/Governance/Security decision without selecting an outcome; bind the approved future procedure into the catalog, matrix, epics, and all-copy evidence.
4. Reconcile H-R12-3 so an export-bearing deletion's recorded Resume branch either remains truly phase-pinned through `DestructionStarted` or explicitly surfaces different policy semantics.
5. Preserve AD-1..AD-31 and every existing OD id, append rather than rewrite the memlog, re-distill, lint, freeze, and rerun the complete reviewer gate. PASS requires zero Critical and zero High.

## Gate Conclusion

The frozen v12 artifacts do **not** pass: **1 Critical, 3 High, 2 Medium, 0 Low**. Most authoritative and v10/v11/specialist corrections are materially present, including AD-7/AD-13 ordering, the target-key alias, export-key delivery, source acknowledgement, origin separation, and conditional export-free deletion. The lease revocation race must be eliminated, and the migration and deletion phase-pin defects must converge before this spine can safely govern independent implementation.
