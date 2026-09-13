---
name: Hexalith Agents good-spine rubric review v16
type: architecture-spine-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
intent: review-read-only
lens: bmad-architecture good-spine rubric walker
verdict: fail
critical: 0
high: 1
medium: 3
low: 1
lint_ok: true
---

# Good-Spine Rubric Reviewer Gate — v16

## Verdict

**FAIL — 0 Critical, 1 High, 3 Medium, 1 Low.** The v16 candidate closes the v15 operator-origin scope, migration-repair, pre-Provider-capacity recovery, and output-safety phase-pin findings. The complete independent walk nevertheless finds one remaining deletion-cut race: the EventStore admission fence blocks new directory permits, protected User-action intents, and effect-lease acquire/commit, but it does not block new rate/open or capacity phase starts that an already-started interaction Workflow can initiate after the fence. Those cross-owner records can therefore appear after the immutable Closing manifest and race the owner `Effective` append, invalidating the claimed global fixed point. No protected read or Provider call can escape because their lease commit is fenced, so this is High rather than Critical; it is still an architecture defect that prevents PASS.

## Frozen Inputs And Method

I read the full current architecture spine, implementation conventions, architecture memlog, authoritative validation report, bound PRD, epics, external-dependency register, launch-readiness register, repository instructions, declared local sources, and focused repository contracts/current package and submodule state. I re-walked the complete good-spine rubric rather than using v15 as the checklist: downward divergence, AD enforceability, owner/revision authority, effect ordering, recovery and lost acknowledgements, security and data-loss boundaries, PRD/epic traceability, unresolved decisions, brownfield truth, architecture-versus-delivery debt, source integrity, stable identifiers, and mechanical lint. The five v16/v5 review paths declared in frontmatter were treated as concurrent anticipated outputs; all other declared local sources resolved.

The supplied hashes matched at intake and immediately before report creation:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `33abd2ca65575b3c73b95827a0b2994cf7f1e909bcdd48a9a93f22403b4476f2` |
| `IMPLEMENTATION-CONVENTIONS.md` | `f30858efcfdb0439bcd3c2a92073061b8cecec35469c85f5503dbf32aeea160a` |
| architecture `.memlog.md` | `f47127fbac2f66bea528939c3fc55433487b9a0f539ab602956833324fe9240d` |
| bound `prd.md` | `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb` |
| `epics.md` | `e137a160dc9344d4dd2a5e5f1e8bd176ec81f5b721ee7fdcf0ab55c693040c8d` |
| `external-dependency-register.md` | `0a780c4c9cf77e8e9d1cf26f7003997e5e7f8a8fa7da61698299470ff7c8eb46` |
| `launch-readiness-register.md` | `7f5316db6d320ebd3f4b1e3eace38e7892c83533bb7b727fd3f4f2cbdf9f1cae` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

## Deterministic Linter

Command:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: `ok: true`, `total_findings: 0`; there are no duplicate AD IDs, placeholders, missing `Binds`/`Prevents`/`Rule` fields, or mechanically unpinned Stack entries. An independent ID scan found exactly one each of AD-1 through AD-31, maximum AD-31, with no gaps or reuse.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 1 |
| Medium | 3 |
| Low | 1 |

## Critical

None.

## High

### H-R16-1 — The deletion admission fence does not close post-fence rate/open or capacity phase starts

**Classification:** architecture recovery/data-integrity defect; not implementation debt and not an unresolved Product choice.

**Conflicting authority.** The common scope admission fence rejects exactly later matching `RegisterInteractionPermit`, `ReserveUserActionIntent`, `ConversationEffectLease:Acquire`, and `CommitConversationEffect` appends (`ARCHITECTURE-SPINE.md:209-211`; `IMPLEMENTATION-CONVENTIONS.md:23`; `launch-readiness-register.md:283-286`). That closes every protected read, target mutation, and Provider/posting effect that is covered by the enumerated leases. It does not close the unleased FR-8 rate/open phase or the capacity acquire/queue phase. The normative sequence settles the `WorkflowStart` lease at the first durable checkpoint, then authorizes and prepares RateLimit/OpenInteraction state before acquiring the next `ApproverResolution` or `ContextRead` lease (`ARCHITECTURE-SPINE.md:353,360,640-669`). After acceptance it acquires or queues capacity and records `CapacityAdmissionRecorded` before it reserves the `ProviderInvocation` lease (`ARCHITECTURE-SPINE.md:354,687-698`). Neither `AgentCallAcceptance:RateAndOpenAdmission` nor an allocator acquire/record variant carries `NoMatchingDeletionScopeAdmissionFenceAtEventStore`; only permit, intent, lease acquire, and lease commit rows do (`launch-readiness-register.md:236,251-259`).

**Concrete race.** An interaction can have an already-committed/start-settled Workflow lease when deletion installs its admission fence. The deletion Workflow reads current interaction/ledger/allocator high-waters and appends the directory owner's immutable Closing manifest. The interaction Workflow can then append `RateAdmissionAuthorized` and prepare Rate/Open owners, or—if already accepted—acquire/queue capacity and append `CapacityAdmissionRecorded`, because none of those phase starts is rejected by the scope fence. `ActivateBarrierEffective` checks the frozen manifest and appends only against the `ConversationAgentState` expected revision (`launch-readiness-register.md:286-296`); a target/ledger/allocator phase start after its last read does not conflict with that append. The global cut can consequently become Effective while a newly prepared rate/open record or capacity identity exists outside its manifest. The new `PreProviderCapacityDispositionDecided(CancelNoInvocation)` recovery is sound for an identity already captured by Closing, but its matrix precondition explicitly requires `RecordedClosingManifestQueuedOrAdmittedCapacityIdentityExact`, so it cannot recover the escaped identity (`launch-readiness-register.md:288-289`). The same manifest-bound limitation applies to rate/open settlement (`:290-291`). Story 8.3 repeats the frozen-manifest claim without racing these two phase starts (`epics.md:3005-3007,3049-3051`).

**Impact.** The lease fence still prevents protected reads, generated-content persistence, posting, and Provider invocation, so the race does not justify Critical severity. It can, however, let deletion report a false fixed point, leave rolling-rate/open-interaction or allocator state referring to an erased/terminalized interaction, consume tenant/user capacity indefinitely until separate timeout or recovery, and make two conforming implementations disagree on whether post-fence phase starts are allowed or manifestable.

**Disposition — AUTOFIX mechanics.** Make every new in-scope cross-owner phase causally pass through the deletion cut. One valid design is to add directory-owned phase leases for rate/open admission and capacity acquisition; another is to extend the EventStore admission guard and target-limited adapter capabilities so post-fence `RateAdmissionAuthorized`/`OpenInteractionLeaseAuthorized`, ledger prepare, capacity acquire/queue, and `CapacityAdmissionRecorded` cannot begin, while exact result/abort/release/acknowledgement for a pre-fence authorization remains legal. In either design, Closing must bind every pre-fence winner and `Effective` must verify an authenticated high-water/guard condition that cannot race a new target/ledger/allocator phase start. Race workflow-start settlement, rate/open authorization and first/last prepare/decision acknowledgement, `AgentCallAccepted`, capacity acquire/queue/record, Closing manifest creation, owner Effective, and global-cut recording on both deletion origins. Do not change Product consumption semantics or invent a cancellation outcome.

## Medium

### M-R16-1 — Architecture-owned `RQ-1` assumptions still lack literal retirement dates

**Classification:** planning-governance debt; safely blocked.

PRD §8.1 requires every Architecture-owned assumption affecting `RQ-1` to carry a co-owner-approved literal calendar date. `ARCH-A-1`, `ARCH-A-2`, `ARCH-A-3`, the test-stack portion of `ARCH-A-4`, `ARCH-A-6`, `ARCH-A-7`, `ARCH-A-8`, `ARCH-A-11`, `ARCH-A-12`, and `ARCH-A-14` remain `Unscheduled` or milestone-only (`ARCHITECTURE-SPINE.md:1277-1295`). `ARCH-A-INDEX-9` correctly emits `UnretiredAssumption`, so no unsafe release is authorized. Obtain owner-approved dates or retain the blockers; Architecture must not invent them.

### M-R16-2 — The `PostingPending` timeout remains intentionally unresolved

**Classification:** unresolved Architecture/Product parameter; safely surfaced.

AD-5 fixes only a lower bound tied to the Conversations seam and `ARCH-A-14` records that neither the PRD nor the spine supplies a concrete duration or one versioned configuration owner (`ARCHITECTURE-SPINE.md:239,1294`). This safely blocks implementation convergence for that timeout/recovery behavior. Architecture should propose a concrete value or explicitly delegate it to a versioned seam/profile for Product confirmation before the owning story is ready.

### M-R16-3 — Pre-commit control-plane observations still lack one closed vocabulary boundary

**Classification:** target-document clarity debt; not a current unsafe branch.

AD-2 and the conventions allow only closed content-free owner-local checks before a lease commit and broadly place dependency reads after commit (`ARCHITECTURE-SPINE.md:205`; `IMPLEMENTATION-CONVENTIONS.md:9,19`), while the lease-acquire row accepts `ClosedContentFreeOwnerAndPhasePinnedControlPlaneEvidenceExact` before commit (`launch-readiness-register.md:252-254`) and AD-12 requires authorization/readiness re-reads before effects (`ARCHITECTURE-SPINE.md:334-336`). The corrected target consistently commit-gates protected content, target mutation, safety, roster/Conversation reads, and external effects, but teams can still classify a content-free readiness observation differently. Define `OwnerLocalCheck`, `ControlPlaneObservation`, and `TargetDependencyRead` once without weakening the effect boundary.

## Low

### L-R16-1 — Root bUnit remains behind the current Builds-checkout catalog

**Classification:** implementation/build maintenance.

The root pins `bunit` `2.9.0`; the current Builds checkout pins `2.10.3` (`Directory.Packages.props:26`; `references/Hexalith.Builds/Props/Directory.Packages.props:317`). The spine accurately reports the root pin and assigns alignment to Story 5.6, so this is not an architecture contradiction.

## Authoritative 2026-09-12 Critical/High Closure Audit

| Authoritative finding | v16 disposition |
| --- | --- |
| C-1 weaker safety evaluation | **Closed.** AD-20 requires conjunctive snapshot/current evaluation and permits only formally equivalent, tested dominance. |
| C-2 hold/deletion exclusion | **Closed for the authoritative irreversible exclusion defect.** `ProtectionFence` owns one expected-revision set decision, hold/export/deletion contention, prepare, arm, and destruction authority; deletion has both admission/effect and content fences. H-R16-1 is a narrower content-free phase-start/fixed-point race and does not permit DEK destruction under a hold or an unfenced protected-content effect. |
| C-3 bootstrap/matrix scope | **Closed.** Matrix v4 has explicit Platform/Tenant scope, whole-gate omissions only for named bootstrap/repair variants, direct owner preconditions, and empty/broken-state fixtures. |
| H-1 scheduled Approver re-check | **Closed.** Durable single-flight, cadence/freshness, two-pass empty evidence, state-specific results, and the same-owner Approver lease are bound. |
| H-2 distributed safety rescan | **Closed.** EventStore epoch/index ownership, finite cohort, bounded coordinator, activation barrier, and call-time pending behavior are executable. |
| H-3 human-only Approvers | **Closed.** Configuration and runtime require Parties-authoritative human classification, liveness, and stable actor binding. |
| H-4 ledger lifetime conflation | **Closed as separate owners/lifetimes.** Rate, open-interaction, and budget state are distinct. H-R16-1 concerns their deletion-cut admission ordering, not their normal lifetime semantics. |
| H-5 human identity/separation | **Closed.** Stable `AuthenticatedHumanActorId` spans User, Administrator, and Platform evidence; Workflow remains non-human. |
| H-6 proposal-index crash consistency | **Closed.** Interaction truth, source-revision outbox, directory high-water, reconciliation, and removal fixed point are explicit. |
| H-7 indivisible Conversations dependency | **Closed.** Core `EXT-CONV-AI-1` and optional `EXT-CONV-RETRACTION-1` remain independently governed. |
| H-8 trusted-envelope security | **Closed.** Canonical authenticated fields, logical/delivery identities, issuer-wide replay owner, retention, key overlap/revocation, ACLs, and lost-ack recovery remain bound. |
| H-9 export ownership/lifecycle/signature | **Closed subject to the surfaced lifecycle decision.** Immutable store/index, prepare inventory, fence commit, key delivery, cleanup/all-copy recovery, JCS/ES256 manifest, and later-hold/restore safe state are bound. |
| H-10 current Dapr exposure | **Closed as current-reality classification.** Parent-authoritative 1.18.5 Client/ASP.NET exposure, dirty-checkout 1.18.7, and future Workflow adoption are distinguished. |
| H-11 public-contract parity overclaim | **Closed.** Required completion vocabulary and current implementation debt are explicitly separate. |
| H-12 sprint/evidence contradiction | **Closed as architecture-versus-delivery classification.** `OD-SPRINT-5.1-5.2-1` retains the owner decision without rewriting history. |

## v15 And Focused v16 Correction Audit

| Item | Disposition |
| --- | --- |
| v15 C-R15-1 operator deletion scope | **Substantially closed.** Both origins now install the same canonical admission fence, owner Closing/Effective cuts, global cut, and later content fence. H-R16-1 identifies the one remaining unguarded content-free phase-start race. |
| v15 H-R15-1 migration User-action intent | **Closed.** The spine, conventions, matrix, host record, and Story 6.1 all put User-action-intent outboxes in the post-directory-repair-fence cohort and bridge. |
| v15 H-R15-2 pre-Provider capacity recovery | **Closed for a pre-fence/manifested identity.** Exact allocator lookup, no-authorization/begin/active proof, interaction-owned disposition, release, and acknowledgement are bound. H-R16-1 covers a capacity identity first created after the manifest. |
| Canonical governance scope | **Pass.** Exact-interaction and exact-Conversation membership are EventStore-computable and fail closed; `ClassUtcRange` is correctly blocked by `OD-GOVERNANCE-CLASS-RANGE-SCOPE-1` instead of receiving invented class/time semantics. |
| Operator deletion cancellation | **Pass by surfacing.** No Abort or fence/cut removal is legal while `OD-OPERATOR-DELETION-CANCELLATION-1` is Open; a future approved branch has cleanup, both-fence removal, every-owner cut release, and finalization order. |
| Directory-repair boundary | **Pass.** Plan authorization precedes atomic repair-fence/checkpoint installation; the finite cohort freezes afterward; the bridge is exact and successor activation revokes old epoch/fence/bridge while preserving deletion fences. |
| Initial output-safety status | **Pass by phase pinning, not choosing.** Provider authorization binds the effective decision revision, digest, complete public/metric/open-lease mapping, and catalog activation before Provider work; a successor cannot relabel result/recovery. |
| User action, Approver, generated-output, posting and export-key effects | **Pass.** Durable authorization precedes protected/dependency/effect work; original-human versus Workflow evidence and lost-ack recovery are explicit. |

## AD IDs, Memlog, Open Decisions, Sources, And Repository Reality

- The spine contains exactly one each of AD-1 through AD-31. No ID was renumbered, reused, or deleted; the maximum remains AD-31.
- Memlog lines 404-409 are append-only decisions covering `GovernanceScopeV1`, the common two-origin cut, operator cancellation authority, repair fencing/User-action cohort, pre-Provider capacity disposition, and output-safety phase pinning. Earlier supersession history remains intact.
- The new `OD-GOVERNANCE-CLASS-RANGE-SCOPE-1` and `OD-OPERATOR-DELETION-CANCELLATION-1` correctly expose Product/Governance/Security choices without choosing them. Existing hold/deletion precedence, hold cancellation, export lifecycle, rate/concurrency, Dapr security, sprint evidence, historical safety, retraction, instruction protection, legacy plaintext, release-recorder scope, and initial-output status decisions retain explicit safe states and affected evaluations.
- All twelve external dependencies remain `Uncommitted`; the registers do not treat checkout presence, a historical target, or a partial adapter as availability. `RQ-1` remains NOT READY.
- Repository inspection confirms the root-authoritative Builds gitlink `a32cb422` versus the modified checkout `cf52f74`, the root bUnit `2.9.0` versus checkout `2.10.3`, absent Dapr Workflow adoption, current plaintext/direct interaction state, and missing directory/lease/fence/decision-catalog/spool/export protocols. The spine classifies these as delivery debt rather than claiming target compliance.
- Apart from the five concurrent v16/v5 review outputs, every declared local source resolved. The deterministic linter and direct AD/source scans passed.

## Whole Good-Spine Checklist

| Dimension | Result |
| --- | --- |
| Fixes downward divergence | **Fail at H-R16-1.** Builders can legitimately place rate/open and capacity phase starts on opposite sides of the deletion cut. |
| AD enforceability / prevents stated divergence | **Fail at H-R16-1.** The directory fence and immutable manifest do not share a serialization boundary with every cross-owner phase the manifest claims to close. Other reviewed ADs have named owners, revisions, variants, outcomes, and recovery evidence. |
| Deferred/open decisions | **Pass.** Product/Governance choices have owners, safe states, affected scopes, and revisit points; no outcome was invented. |
| Named technology / repository truth | **Pass.** Root authority, modified checkouts, exact pins, unselected components, and unavailable dependencies are distinguished. |
| Bound PRD and epics coverage | **Fail narrowly at H-R16-1.** The NFR-11/FR-30 fixed-point and recovery claim is not implementable for the post-fence phase-start race. Other authoritative capabilities trace through the map, matrix, and owning stories. |
| Brownfield ratification | **Pass.** Present direct/plaintext streams and missing target protocols are explicit debt, not ratified as target state. |
| Security, tenancy, data loss | **Pass at the Critical bar.** Canonical membership, actor separation, replay, protected-content, hold/export/deletion, and cross-tenant boundaries fail closed. H-R16-1 leaves content-free resource state rather than an unfenced protected-data effect. |
| Recovery / operations / environment | **Fail at H-R16-1.** Other effect authorization, lost acknowledgement, RPO-0, restore, decision, spool, export, migration and deletion recovery paths are explicit. |
| Sources / mechanics | **Pass.** Local sources resolve as scoped, AD IDs are stable, hashes match, and deterministic lint is clean. |

## Architecture Defects Versus Delivery Debt

H-R16-1 is a target architecture defect: implementing the documents literally leaves no common owner or guard that can prove the rate/open/capacity phase-start fixed point, so downstream implementation work cannot close it without making a new architecture choice. M-R16-1 through M-R16-3 are planning/clarity debt and L-R16-1 is build maintenance.

The absence of the target directory, leases, migration/repair guards, deletion fences, ledgers, decision catalog, security spool, export machinery, and public vocabulary in current code remains delivery debt. Those missing implementations do not create additional architecture findings where the target contract is complete, and they do not reduce H-R16-1's severity.

## Required Correction Order

1. Close H-R16-1 by serializing every new in-scope rate/open and capacity phase start with the admission fence and immutable Closing manifest, preserving only exact result/abort/release/acknowledgement for pre-fence winners.
2. Add failure-injection races at every authorization, first external prepare/acquire, interaction-owner record, Closing, owner Effective, and global-cut boundary for both deletion origins.
3. Re-distill the rule across AD-2/AD-12/AD-13/AD-21, conventions, matrix, host dependency, Story 6.1, and Story 8.3 without renumbering ADs; append the architecture decision to the memlog.
4. Keep Product-owned scope, cancellation, output-status, rate/concurrency and lifecycle choices Open until their owners act; the correction above chooses none of them.
5. Rerun the complete reviewer gate. PASS requires zero Critical and zero High.

## Post-Write Integrity Check

After creating this report, all eight reviewed inputs retained the frozen SHA-256 values listed above. The deterministic architecture linter was rerun read-only and again returned `ok: true`, `total_findings: 0`.

## Gate Conclusion

The v16 work closes the prior Critical boundary and all focused v15 High findings at their stated races. The whole-artifact gate remains **FAIL** because rate/open and capacity phase initiation are not yet inside the new admission/effect cut. Final counts: **0 Critical, 1 High, 3 Medium, 1 Low**.
