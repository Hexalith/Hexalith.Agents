---
name: Hexalith Agents good-spine rubric review v17
type: architecture-spine-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
intent: review-read-only
lens: bmad-architecture good-spine rubric walker
verdict: pass
critical: 0
high: 0
medium: 3
low: 2
lint_ok: true
---

# Good-Spine Rubric Reviewer Gate — v17

## Verdict

**PASS — 0 Critical, 0 High, 3 Medium, 2 Low.** The v17 candidate closes the remaining deletion-cut phase-start defect and the other focused v16 Critical/High findings without weakening the authoritative Product contract or selecting an unresolved Product outcome. A complete independent walk found no Critical or High architecture defect. The remaining findings are safely blocked planning/clarity debt and minor structural/build maintenance; none authorizes divergent or unsafe V1 execution.

## Frozen Inputs And Method

I read the full current architecture spine, implementation conventions, architecture memlog, authoritative validation report, bound PRD, epics, external-dependency register, launch-readiness register, repository instructions, declared local sources, and focused repository contracts/current package and submodule state. I re-walked the complete good-spine rubric rather than treating v16 as the checklist: downward divergence, AD enforceability, owner/revision authority, effect ordering, recovery and lost acknowledgements, security and data-loss boundaries, PRD/epic traceability, unresolved decisions, brownfield truth, architecture-versus-delivery debt, source integrity, stable identifiers, and mechanical lint.

The five v17/v5 review paths declared in frontmatter were treated as concurrent anticipated outputs. All other declared local sources resolved. The supplied hashes matched at intake; the final `epics.md` hash below is the corrected frozen value supplied after two non-semantic Result-line clarifications:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `f834ae4e5d6c82b5581c55e4fa6ac06832cc758e6ff3218a227aaba0b631cd34` |
| `IMPLEMENTATION-CONVENTIONS.md` | `accc861511863f03bf27f447d2bbc9d62862b793c954b3db98b64ef496931f76` |
| architecture `.memlog.md` | `5cb60e74516a579cd131a48f9c63df11fdcc10cd278ab34899d41e4482c6b09b` |
| bound `prd.md` | `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb` |
| `epics.md` | `6f9726d366d5957ea9180c9d4d59ecf47cc1cb8a8f13bb35625a49ebf46380e1` |
| `external-dependency-register.md` | `aecbcdfa2538af9c62f70c4cb5a32690ea5811639671f681859e3203700d8788` |
| `launch-readiness-register.md` | `31f937e7f1166a71d5e9180cfad570787cf57f315c65dea41ed9bd9ef21e0fbe` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

Repository inspection was read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`. The parent-authoritative Builds gitlink remains `a32cb422`, while the modified checkout is `cf52f74`; the architecture correctly treats the checkout as evidence rather than root authority.

## Deterministic Linter

Command:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: `ok: true`, `total_findings: 0`. A separate ID scan found exactly one each of AD-1 through AD-31, maximum AD-31, with no gap, duplicate, reuse, or renumbering.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 0 |
| Medium | 3 |
| Low | 2 |

## Critical

None.

## High

None.

## Medium

### M-R17-1 — Architecture-owned `RQ-1` assumptions still lack literal retirement dates

**Classification:** planning-governance debt; safely blocked, not an implementation defect.

PRD FR-28 requires each Architecture-owned `RQ-1` assumption to carry a literal calendar retirement date. `ARCH-A-1`, `ARCH-A-2`, `ARCH-A-3`, the open test-stack portion of `ARCH-A-4`, `ARCH-A-6`, `ARCH-A-7`, `ARCH-A-8`, `ARCH-A-11`, `ARCH-A-12`, and `ARCH-A-14` remain `Unscheduled` or milestone-only (`ARCHITECTURE-SPINE.md:1298-1315`). The spine correctly emits `UnretiredAssumption` and does not permit READY, so this is non-blocking for the architecture safety gate. Obtain co-owner-approved dates; Architecture must not invent them.

### M-R17-2 — The `PostingPending` timeout still has no single concrete authority

**Classification:** unresolved Architecture/Product parameter; safely surfaced.

AD-5 fixes only a lower bound tied to the Conversations posting seam and explicitly says neither the PRD nor the spine fixes the duration (`ARCHITECTURE-SPINE.md:245`). `ARCH-A-14` preserves the blocker (`:1315`). This prevents silent implementation authority, but two teams could otherwise choose different recovery deadlines. Before the posting/recovery story is ready, Architecture should propose one literal duration or bind one versioned seam/profile value and obtain Product confirmation.

### M-R17-3 — The pre-commit control-plane vocabulary remains broader than its closed examples

**Classification:** target-document clarity debt; no demonstrated unsafe execution branch.

The authoritative effect rule allows only closed, content-free, owner-local checks before lease commit and places protected content, target mutation, safety, roster, Conversation, and other dependency reads after commit (`ARCHITECTURE-SPINE.md:209,342`; `IMPLEMENTATION-CONVENTIONS.md:9,19`). The matrix accepts `ClosedContentFreeOwnerAndPhasePinnedControlPlaneEvidenceExact` at lease acquisition (`launch-readiness-register.md:254`). The concrete phase rows are consistently ordered and fail closed, but `OwnerLocalCheck`, `ControlPlaneObservation`, and `TargetDependencyRead` are not defined once as a closed vocabulary. Define those terms centrally so a future phase cannot reclassify a non-owner readiness/dependency read as pre-commit control-plane evidence.

## Low

### L-R17-1 — The Structural Seed does not show the separate admission-fence violation ledger

**Classification:** diagram/re-distillation debt.

The normative AD and matrix correctly distinguish the admission-fence violation ledger/checkpoint from the content-write-fence violation ledger/checkpoint (`ARCHITECTURE-SPINE.md:217`; `launch-readiness-register.md:304-310`). The `ProtectionFence` class in the Structural Seed still shows one generic `ViolationContainmentManifest` and only a `DeletionScopeWriteFenceVerificationCheckpoint` (`ARCHITECTURE-SPINE.md:1126-1144`). Add the admission ledger/checkpoint field in a later redistillation so the diagram mirrors the closed protocol; the prose and matrix are already unambiguous.

### L-R17-2 — Root bUnit remains behind the current Builds-checkout catalog

**Classification:** implementation/build maintenance.

The root pins bUnit `2.9.0`, while the current Builds checkout catalog pins `2.10.3` (`Directory.Packages.props:26`; `references/Hexalith.Builds/Props/Directory.Packages.props:317`). The spine accurately reports the root pin and assigns catalog alignment to Story 5.6, so this is not an architecture contradiction.

## Authoritative 2026-09-12 Critical/High Closure Audit

| Authoritative finding | v17 disposition |
| --- | --- |
| C-1 weaker safety evaluation | **Closed.** AD-20 requires conjunctive snapshot/current evaluation and permits a single pass only with machine-checkable, tested dominance. |
| C-2 hold/deletion exclusion | **Closed.** `ProtectionFence` owns the shared expected-revision decision, immutable accepted resource set, prepare/arm/destruction authority, and both deletion origins now have admission/effect and content cuts with independent violation recovery. |
| C-3 bootstrap/matrix deadlock | **Closed.** Matrix v4 has explicit Platform/Tenant scope, closed target-specific bootstrap/repair variants, direct-owner preconditions, and empty/broken-state fixtures. |
| H-1 scheduled Approver re-check | **Closed.** Durable single-flight, cadence/freshness, two-pass empty evidence, state-specific results, and the directory-owned Approver lease are bound. |
| H-2 distributed safety rescan | **Closed.** EventStore epoch/index ownership, finite cohort, bounded fenced coordinator, activation barrier, and `RescanPending` behavior are executable. |
| H-3 human-only Approvers | **Closed.** Configuration and runtime require Parties-authoritative human classification, liveness, and stable actor binding. |
| H-4 ledger lifetime conflation | **Closed.** Rate, open-interaction, Budget, and capacity state have distinct owners, identities, lifetimes, and settlement rules. |
| H-5 human identity/separation | **Closed.** Stable `AuthenticatedHumanActorId` spans Party-bearing User, Administrator, and Platform evidence; Workflow remains non-human. |
| H-6 proposal-index crash consistency | **Closed.** Interaction truth, same-append source-revision outbox, directory high-water, reconciliation, and removal fixed point are explicit. |
| H-7 indivisible Conversations dependency | **Closed.** Core `EXT-CONV-AI-1` and optional `EXT-CONV-RETRACTION-1` remain independently governed. |
| H-8 trusted-envelope security | **Closed.** Canonical authenticated fields, logical/delivery identity split, platform-wide issuer replay owner, retention, rotation/revocation, ACLs, and lost-ack recovery remain bound. |
| H-9 export ownership/lifecycle/signature | **Closed subject to the surfaced Product lifecycle decision.** Immutable store/index, prepare inventory, fence commit, ES256/JCS manifest, direct key delivery, cleanup/all-copy recovery, and later-hold/restore safe state are bound. |
| H-10 current Dapr exposure | **Closed as current-reality classification.** Parent-authoritative Client/ASP.NET `1.18.5`, non-authoritative checkout `1.18.7`, and future Workflow adoption are distinguished. |
| H-11 public-contract parity overclaim | **Closed.** Required completion vocabulary and current implementation debt are explicitly separate. |
| H-12 sprint/evidence contradiction | **Closed as delivery governance debt.** `OD-SPRINT-5.1-5.2-1` preserves the owner decision without rewriting status history or treating it as architecture authority. |

## Focused v16/v17 Correction Audit

| Prior item | v17 disposition |
| --- | --- |
| Rubric v16 H-R16-1 — post-fence rate/open and capacity phase starts | **Closed.** Rate/open, Budget reservation, and capacity each require an interaction-owned authorization revision before external contact; the EventStore admission and migration-repair guards reject every later matching authorization append (`ARCHITECTURE-SPINE.md:213,221,360`; `launch-readiness-register.md:238,261-264,291`). |
| Adversarial v16 C-v16-1 — accepted admission-fence violation | **Closed.** A separate append-only admission-violation ledger is checked at the global cut, candidate, acceptance, content-fence authorization, prepare, and destruction boundaries. Pre-destruction acceptance invalidates the cut/token and requires a higher-ordinal full recut; post-destruction acceptance records permanent integrity compromise and blocks further destructive authority/completion (`ARCHITECTURE-SPINE.md:217`; matrix `:304-310`). |
| Adversarial v16 H-v16-1 — Provider decision pin before prohibited read | **Closed.** The Provider lease commits generically from owner-local facts; only afterward does the interaction record exactly one `ProviderInvocationAuthorized` or `ProviderInvocationNotAuthorized` branch. The negative branch has exact Budget/capacity release and lease-settlement authority without Provider work (`ARCHITECTURE-SPINE.md:362-364`; matrix `:265-266`). |
| Verified-current v16 VC16-H1 — permit/lease bootstrap cycle | **Closed.** AD-12 now says explicitly that permit registration has no permit-bound lease; later phases alone acquire permit-bound leases (`ARCHITECTURE-SPINE.md:342`). Conventions and matrix retain the same order. |
| Verified-current v16 VC16-H2 — human exact-Conversation semantics selected implicitly | **Closed by surfacing.** `OD-HUMAN-EXACT-CONVERSATION-SCOPE-1` blocks human hold/export/operator-deletion exact-Conversation variants before a fence or inventory exists, while exact interactions and PRD-fixed source deletion remain executable (`ARCHITECTURE-SPINE.md:205,1246`; register `:109`). |
| Operator nonterminal disposition | **Closed by surfacing.** `OD-OPERATOR-DELETION-NONTERMINAL-DISPOSITION-1` leaves the affected owner in restrictive Closing and prevents invented rejection, abandonment, Conversation-unavailable, or public status semantics (`ARCHITECTURE-SPINE.md:1235`; register `:98`). |

The phase-start correction is internally complete at the architecture level. Each authorization is the sole external-owner capability; a pre-fence winner is returned inside the authenticated EventStore checkpoint and becomes a Closing/repair manifest obligation, while a post-fence loser appends nothing. Result, lookup, disposition, settlement, and acknowledgement can continue only for that exact winner. `ActivateBarrierEffective` requires the corresponding rate/open/Budget/capacity decisions and acknowledgements, so no new cross-owner phase can appear outside the fixed point. Failure-injection obligations cover the authorization/fence/Closing/Effective/global-cut boundaries in conventions and Story 8.3.

## AD IDs, Memlog, Open Decisions, Sources, And Repository Reality

- The spine contains exactly one each of AD-1 through AD-31. No ID was renumbered, reused, or deleted; the maximum remains AD-31.
- The memlog remains append-only. Its latest decisions record phase-authorization serialization, the separate admission-violation recovery protocol, generic Provider branch ownership, and the newly surfaced human exact-Conversation/operator-nonterminal Product decisions without rewriting earlier history.
- `OD-HOLD-DELETION-PRECEDENCE-1`, `OD-HOLD-PREPARE-CANCELLATION-1`, `OD-OPERATOR-DELETION-CANCELLATION-1`, `OD-OPERATOR-DELETION-NONTERMINAL-DISPOSITION-1`, `OD-EXPORT-LIFECYCLE-1`, `OD-RATE-CONCURRENCY-CONSUMPTION-1`, `OD-DAPR-SECURITY-1`, `OD-SPRINT-5.1-5.2-1`, the three materialized PRD decisions, `OD-LEGACY-PLAINTEXT-DISPOSITION-1`, `OD-INITIAL-OUTPUT-SAFETY-STATUS-1`, `OD-GOVERNANCE-CLASS-RANGE-SCOPE-1`, `OD-HUMAN-EXACT-CONVERSATION-SCOPE-1`, and `OD-RELEASE-RECORDER-SCOPE-1` retain named owners, safe states, affected evaluations, and revisit boundaries. The spine selects none of their outcomes.
- All twelve external dependency records remain `Uncommitted`; the registers do not mistake checkout presence, historical targets, configuration, or partial adapters for `Available`. `RQ-1` remains NOT READY.
- Focused repository inspection confirms the root-authoritative Builds gitlink `a32cb422` versus the modified checkout `cf52f74`, root bUnit `2.9.0` versus checkout `2.10.3`, absent Agents Dapr Workflow adoption, current direct/plaintext interaction state, and absence of the target directory/lease/fence/decision-catalog/spool/export protocols. The spine classifies those as delivery debt rather than claiming target compliance.
- Apart from the five concurrent v17/v5 review outputs, every declared local source resolved. `../../epics.md` is now present in frontmatter, closing the earlier source-chain omission.

## Whole Good-Spine Checklist

| Dimension | Result |
| --- | --- |
| Fixes downward divergence | **Pass.** Durable owners, identities, effect capabilities, ordering, and typed recovery outcomes close the implementation choices that could otherwise diverge. |
| AD enforceability / stated prevention | **Pass.** Each AD has an enforceable Rule and the high-risk cross-owner protocols bind expected revisions, exact capabilities, failure branches, and tests. |
| Deferred/open decisions | **Pass.** Product/Governance/Security choices have named owners, safe states, affected scopes, and revisit points; no outcome is invented. |
| Named technology / repository truth | **Pass.** Root authority, modified checkouts, exact pins, unselected components, and unavailable dependencies are distinguished. |
| Bound PRD and epics coverage | **Pass.** FR/NFR capabilities trace through the ADs, matrix, registers, and replacement Stories 5-8. The remaining Open Decisions block only their named branches/evaluations. |
| Brownfield ratification | **Pass.** Present direct/plaintext streams and missing target protocols are explicit delivery debt, not ratified as target state. |
| Security, tenancy, data loss | **Pass.** Tenant/predicate membership, actor separation, replay, protected content, hold/export/deletion, and admission/content violation boundaries fail closed. |
| Recovery / operations / environment | **Pass.** Effect authorization, lost acknowledgement, RPO-0, restore, spool, decision, export, migration, and two-origin deletion recovery are explicit. |
| Sources / mechanics | **Pass.** Local sources resolve as scoped, AD IDs are stable, hashes match, and deterministic lint is clean. |

## Architecture Defects Versus Delivery Debt

No Critical or High target-architecture defect remains in this lens. M-R17-1 and M-R17-2 are safely blocking planning/parameter debt; M-R17-3 and L-R17-1 are clarity/redistillation debt; L-R17-2 is build maintenance.

The missing target directory, effect leases, phase-authorization ledgers, migration/repair guards, deletion fences, decision catalog, security spool, export machinery, and public vocabulary in current code remain implementation debt assigned in the spine and epics. Their absence is not evidence that the architecture boundaries may be weakened, and no review finding recasts those implementation gaps as an unresolved Product choice.

## Recommended Non-Blocking Follow-Up

1. Obtain literal co-owner dates for Architecture-owned `RQ-1` assumptions.
2. Fix a single versioned `PostingPending` timeout authority before its implementation story becomes ready.
3. Close the pre-commit control-plane vocabulary and mirror the separate admission-violation ledger/checkpoint in the Structural Seed during the next redistillation.
4. Retain every named Product/Governance/Security decision as Open until its owners approve a contract; do not implement a local outcome.

## Post-Write Integrity Check

After creating this report, all eight reviewed inputs retained the frozen SHA-256 values listed above. The deterministic architecture linter was rerun read-only and again returned `ok: true`, `total_findings: 0`.

## Gate Conclusion

The complete v17 good-spine reviewer gate is **PASS**. Final counts: **0 Critical, 0 High, 3 Medium, 2 Low**.
