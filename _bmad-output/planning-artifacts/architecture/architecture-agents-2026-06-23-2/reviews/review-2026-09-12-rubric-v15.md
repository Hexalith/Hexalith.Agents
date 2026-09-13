---
name: Hexalith Agents good-spine rubric review v15
type: architecture-spine-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
intent: review-read-only
lens: bmad-architecture good-spine rubric walker
verdict: fail
critical: 1
high: 2
medium: 3
low: 1
lint_ok: true
---

# Good-Spine Rubric Reviewer Gate — v15

## Verdict

**FAIL — 1 Critical, 2 High, 3 Medium, 1 Low.** The v15 candidate correctly introduces a Product-owned initial-output-safety status decision, leases call-time and scheduled Approver resolution, makes User proposal actions recoverable from a durable intent, extends Conversation-origin Closing completeness, and adds an EventStore deletion-scope fence. The complete walk nevertheless finds that the scope fence is not an effect/admission fence for operator-origin deletion, so directory writes and lease commits remain possible outside the accepted inventory and a class-scoped deletion can report success without accounting for new protected directory payloads. Two recovery cohorts are also incomplete: the spine's migration-repair rule omits the new User-action-intent outbox that the register requires, and the Closing recovery grant cannot release a capacity admission acquired before a Provider lease commits.

## Frozen Inputs And Method

I read the full current spine, implementation conventions, architecture memlog, authoritative validation report, bound PRD, epics, external-dependency register, launch-readiness register, repository instructions, declared local sources, and focused repository contracts. I re-walked every good-spine dimension rather than treating v14 as the checklist: downward divergence, implementability, owner and revision authority, recovery/lost acknowledgements, security and data-loss boundaries, PRD traceability, open/deferred decisions, brownfield truth, architecture-versus-delivery debt, source integrity, stable identifiers, and mechanical lint. The five v15/v5 review paths declared in frontmatter were treated as concurrent anticipated outputs; every other local source resolved.

The supplied hashes matched at intake and immediately before report creation:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `2b64657e34ac93becc0bc7dbf5e2829c290ad581ee578e606053dabd1a68c5ad` |
| `IMPLEMENTATION-CONVENTIONS.md` | `2a3c681658ab045de0302a9f20a527717172983619fd6e93e261bc7a07cd4a06` |
| architecture `.memlog.md` | `1b65ce14c02a64b2abf5da7e9e225b10a2efc45b35f72da6112ce865158721d7` |
| bound `prd.md` | `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb` |
| `epics.md` | `19db22ebf78bf925b6b0906d6e31a086973459b5962c611fdd58972d3070f5b8` |
| `external-dependency-register.md` | `0b85c8bdf893d9a0c38ea93b7f4beb8bf8871d8323f849d4a7fd03082488997d` |
| `launch-readiness-register.md` | `d251642ea2ef765267a7a865a1109f19ded7bf8779ce09568f4b4fd069948090` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

## Deterministic Linter

Command:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: `ok: true`, `total_findings: 0`; no duplicate AD IDs, placeholders, missing `Binds`/`Prevents`/`Rule`, or mechanically unpinned Stack entries.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 2 |
| Medium | 3 |
| Low | 1 |

## Critical

### C-R15-1 — Operator deletion's scope fence does not close directory admission or effect commits

**Classification:** architecture security/data-integrity defect; not implementation debt and not an unresolved Product choice.

**Conflicting authority.** The new EventStore fence applies to later in-scope `AgentInteraction`/`GenerationFailureRecord` creates and content-bearing appends (`ARCHITECTURE-SPINE.md:248,426,507`; `launch-readiness-register.md:291-295`). It does not apply to `ConversationAgentState`, whose permit creation can store a protected prompt in a target-limited creation outbox under a new interaction DEK, whose User-action-intent outbox can store protected edit bytes, and whose lease acquire/commit is the sole authority for protected reads and external effects (`ARCHITECTURE-SPINE.md:198-200,499`). The same-owner Closing barrier that rejects these directory operations and drains committed effects exists only in `ConversationDeletionPropagation`, after the source-system Conversation origin (`ARCHITECTURE-SPINE.md:242,245-252`; matrix rows 278-286). The operator-origin `GovernanceProtection` allowlist has inventory/fence/deletion commands but no directory Closing/effect-cut variant (`ARCHITECTURE-SPINE.md:502,507`). Story 8.3 likewise requires Closing only in the Conversation-origin branch (`epics.md:2990-3000`).

**Concrete failure.** After an operator deletion accepts its inventory and installs the scope fence, a directory-first worker can still append a new permit plus sealed creation outbox or reserve/commit a mutation/posting lease because neither write targets the guarded stream. If target delivery has not yet attempted the now-rejected `AgentInteraction` create, the EventStore violation ledger has no accepted target write to report. For a class/conversation-scoped request this can leave a new protected directory payload under a DEK absent from the accepted set while deletion verification sees the target namespace fence as installed and violation-free. Separately, a pre-existing committed Provider/posting phase has no operator-origin must-settle manifest comparable to the Conversation-origin barrier. The general statement that a deletion terminalizes a nonterminal interaction before arming (`ARCHITECTURE-SPINE.md:422`) does not supply an owner, same-revision cut, finite cohort, or recovery authority and cannot prevent a stale directory worker from starting after inventory acceptance. A builder following the new scope-fence rows can therefore return successful deletion while work or protected directory state remains outside its proof.

**Required correction — AUTOFIX mechanics.** Give both deletion origins one executable admission/effect cut before the inventory can become preparation-eligible. Either extend the persistent EventStore guard to the exact in-scope `ConversationAgentState` permit, protected outbox, acquire, and commit operations and freeze/drain every already committed lease/owner decision, or install a scope-aware same-owner Closing barrier on every authoritative directory owner in a finite checkpointed cohort. Bind operator Workflow authority for exact cancellation, outcome lookup, ledger/capacity settlement, outbox acknowledgement, and Effective/fixed-point proof without granting new effects or wider scope. The destructive branch must not start until no new in-scope directory content/effect can begin and every prior committed phase is settled. Race permit creation, action intent, every lease acquire/commit, Provider invocation, `PostingPending`/Conversation append, scope-fence install, migration successor, and destruction verification for both deletion origins.

## High

### H-R15-1 — Migration repair omits the new User-action-intent outbox in the authoritative spine cohort

**Classification:** cross-artifact recovery-contract defect; not implementation debt.

The spine's repair rule freezes “every directory permit, creation/workflow-start outbox, and effect-lease count/hash/high-water,” and the old-epoch bridge accepts only those frozen pending outboxes and committed leases (`ARCHITECTURE-SPINE.md:204`). It does not name the protected User-action-intent outbox introduced at `:200`. The matrix and Story 6.1 do name it explicitly in the repair cohort and bridge/drain alternatives (`launch-readiness-register.md:241-245`; `epics.md:1903`), while the conventions use the less precise “all current permit/outbox/lease high-waters” (`IMPLEMENTATION-CONVENTIONS.md:23`). Because the conventions declare the spine authoritative, two conforming teams can build different bridge manifests. Omitting an intent can revoke the old epoch with a committed non-revocable action awaiting its target result/acknowledgement or with a reserved intent never consumed, stranding protected content and preventing exact reconciliation.

**Required correction — AUTOFIX mechanics.** Amend the AD-2 repair rule and AD-30 migration capability to enumerate creation, workflow-start, and User-action-intent outboxes consistently with matrix rows 241-245 and Story 6.1. Define the reserved-intent cancellation/consumption and committed-intent target-result/acknowledgement path as explicit bridge members; successor installation must require every one acknowledged.

### H-R15-2 — Closing cannot recover a capacity admission that precedes Provider-lease commit

**Classification:** architecture recovery/availability defect; not implementation debt.

The normative sequence acquires a capacity admission before reserving/committing `ProviderInvocation` (`ARCHITECTURE-SPINE.md:667-675`). Closing may therefore win after capacity returns admitted/queued but before a Provider lease commits. The v15 completeness rule correctly requires applicable capacity settlement before Effective (`ARCHITECTURE-SPINE.md:252,320`; `launch-readiness-register.md:286`), but its deletion Workflow grant `ReleaseManifestedCapacity` requires a `RecordedClosingManifestCommittedProviderLease...` (`launch-readiness-register.md:284`). No committed Provider lease exists in this race. The prose says the ordinary Interaction Workflow drives existing rate/open/pre-Provider Budget state, but omits pre-Provider capacity (`ARCHITECTURE-SPINE.md:252`), and the deletion Workflow is forbidden to acquire or invent another state (`:505`). If the ordinary Workflow crashes at this boundary, no authorized actor can release the admission, so the exact finite manifest can never reach Effective.

**Required correction — AUTOFIX mechanics.** Add a distinct manifest-bound pre-Provider capacity cancellation/release variant authorized by the existing `AgentInteraction` admission identity/fence plus authoritative proof that no `ProviderInvocationAuthorized`/`BeginInvocation` exists. It must forbid acquire/queue/invoke/outcome invention, handle admitted and queued states idempotently, record the interaction decision and allocator acknowledgement, and be included in Effective. Race Closing/lost acknowledgement immediately after capacity acquire/queue and before Provider-lease reserve/commit.

## Medium

### M-R15-1 — Architecture-owned `RQ-1` assumptions still lack literal retirement dates

**Classification:** planning-governance debt; safely blocked.

PRD FR-28 requires every Architecture-owned `RQ-1` assumption to carry a co-owner-approved literal date. `ARCH-A-1`, `-2`, `-3`, the test-stack part of `-4`, `-6`, `-7`, `-8`, `-11`, `-12`, and `-14` remain `Unscheduled` or milestone-only (`ARCHITECTURE-SPINE.md:1248-1261`). `ARCH-A-INDEX-9` correctly emits `UnretiredAssumption`, so this does not authorize release. Obtain owner-approved dates or retain the blockers; Architecture must not invent them.

### M-R15-2 — The `PostingPending` timeout remains intentionally unresolved

**Classification:** unresolved Architecture/Product parameter, safely surfaced.

AD-5 fixes only a lower bound tied to the Conversations seam and `ARCH-A-14` records no duration or single configuration authority (`ARCHITECTURE-SPINE.md:224,1261`). Story 7.4 remains blocked. Architecture should propose a concrete bound or explicit seam-owned versioned configuration for Product confirmation before the timeout/recovery work becomes ready.

### M-R15-3 — Pre-commit control-plane observations still lack a single vocabulary boundary

**Classification:** target-document clarity debt; not a current unsafe branch.

AD-2/conventions permit only content-free owner-local checks before commit and broadly place every “other dependency read” after commit (`ARCHITECTURE-SPINE.md:198`; `IMPLEMENTATION-CONVENTIONS.md:19`), while AD-12 places commit after the last mutable authorization/readiness checks and the acquire row accepts phase-pinned control-plane evidence (`ARCHITECTURE-SPINE.md:320`; `launch-readiness-register.md:247`). The v15 target consistently commit-gates protected-version, safety, roster/Conversation, target and effect I/O, but builders can still differ on whether content-free readiness observations count as dependency reads. Define `OwnerLocalCheck`, `ControlPlaneObservation`, and `TargetDependencyRead` once without weakening the committed boundary.

## Low

### L-R15-1 — Root bUnit remains behind the current Builds-checkout catalog

**Classification:** implementation/build maintenance.

The root pins `bunit` `2.9.0`; the current Builds checkout pins `2.10.3` (`Directory.Packages.props:26`; `references/Hexalith.Builds/Props/Directory.Packages.props:317`). The spine accurately reports the root pin and assigns alignment to Story 5.6, so this is not an architecture contradiction.

## Authoritative 2026-09-12 Critical/High Closure Audit

| Authoritative finding | v15 disposition |
| --- | --- |
| C-1 weaker safety evaluation | **Closed.** AD-20 retains snapshot/current conjunction and machine-checkable dominance only. The new initial-output public-status choice does not weaken safety and fails Provider work closed while Open. |
| C-2 hold/deletion exclusion | **Reopened in the operator-origin scope at C-R15-1.** The tenant protection fence and Conversation-origin Closing are sound, but operator-origin deletion has no equivalent directory/effect cut. |
| C-3 bootstrap/matrix scope | **Closed.** Platform/Tenant scopes and typed direct bootstrap/repair/containment/recorder variants remain non-circular. |
| H-1 scheduled Approver re-check | **Closed.** v15 strengthens it with a committed `ApproverResolution` lease for both call-time and scheduled reads. |
| H-2 distributed safety rescan | **Closed.** Epoch/index ownership, finite cohorts, fenced coordination, and on-demand initialization remain explicit. |
| H-3 human-only Approvers | **Closed.** Current human/liveness and historical actor binding fail closed. |
| H-4 ledger lifetime conflation | **Closed as owner separation; incomplete at H-R15-2 only for deletion recovery of a pre-Provider capacity admission.** |
| H-5 human identity/separation | **Closed.** Stable actor identity spans User, Administrator, and Platform evidence. |
| H-6 proposal-index crash consistency | **Closed.** Source-revision outbox/high-water/checkpoint reconciliation remains bound. |
| H-7 indivisible Conversations dependency | **Closed.** Core and optional seams retain separate records and consumers. |
| H-8 trusted-envelope security | **Closed.** Canonicalization, logical/delivery identity, replay owner, key lifecycle, ACLs, and recovery remain explicit. |
| H-9 export ownership/lifecycle/signature | **Closed subject to surfaced Product lifecycle authority.** Fence/index/store/signature/key-delivery/all-copy recovery remains bound. |
| H-10 current Dapr exposure | **Closed as repository truth.** Root-authoritative `1.18.5`, checkout `1.18.7`, and future Workflow adoption remain distinct. |
| H-11 public-contract parity overclaim | **Closed.** Target vocabulary and current shipped parity remain separate. |
| H-12 sprint/evidence contradiction | **Closed as architecture-versus-delivery classification.** The Open delivery decision remains visible. |

## v14 And Focused v15 Audit

| Item | Disposition |
| --- | --- |
| v14 Critical/High gate | v14 had no C/H. Its approval/pre-post lease closure remains intact. |
| Deletion-scope EventStore fence | **Fails at C-R15-1.** The target-stream fence and violation ledger are sound for their stated streams, but they do not fence the directory owner or active effects for operator-origin deletion. |
| User-action handoff | **Pass for ordinary and Conversation-Closing execution.** Intent, protected payload, original-human evidence, committed lease, exact Workflow recovery, result acknowledgement, and anti-impersonation are bound. Migration repair remains inconsistent at H-R15-1. |
| ApproverResolution leasing | **Pass.** Call-time step 6 and scheduled recheck commit before Conversations/Parties reads, bind typed results, settle before Context, and race Closing at the same owner. |
| Initial output-safety status | **Pass by surfacing, not choosing.** `OD-INITIAL-OUTPUT-SAFETY-STATUS-1` has Product + Security owners, exact affected operations/stories/evaluations, a no-content safe state, and a runtime fail-closed rule before Provider work. |
| Complete Closing manifest | **Fails recovery completeness at H-R15-2.** Permit/outbox/lease/rate/open/Budget rows are otherwise materially complete and narrowly authorized. |
| Migration successor and active deletion fence | **Pass.** Bridge/successor rows preserve and acknowledge every active scope fence, and destruction uses direct verification. |
| Post-start violation handling | **Pass within the declared EventStore fence.** Same-scope resources join an append-only receipt manifest; outside-scope/unresolvable evidence blocks and needs new human authority. |

## AD IDs, Memlog, Open Decisions, And Repository Reality

- The spine contains exactly one each of AD-1 through AD-31; no ID was renumbered or reused and the maximum remains AD-31.
- Memlog lines 398-403 append the output-safety OD, Approver-resolution lease, User-action intent, complete-owner manifest, deletion-scope fence, and violation-containment decisions; earlier history is preserved.
- `OD-INITIAL-OUTPUT-SAFETY-STATUS-1` is correctly added without choosing `GenerationFailed` versus `SafetyFailed`. All prior open Product/Governance decisions remain Open only on their named scopes.
- Every external record remains `Uncommitted`; `EXT-HOST-1` accurately carries the target namespace/deletion-fence behavior but no unavailable seam is treated as delivered.
- The repository's absent directory, effects, fences, ledgers, decision catalog, security spool, export and public parity remain correctly classified as implementation debt. Current plaintext/direct streams and effect-before-dispatch approval/posting are not presented as compliant target state.

## Whole Good-Spine Checklist

| Dimension | Result |
| --- | --- |
| Fixes downward divergence | **Fail.** C-R15-1, H-R15-1, and H-R15-2 permit materially different scope-fence and recovery implementations. |
| AD enforceability / prevents stated divergence | **Fail.** The operator deletion cut and two finite recovery cohorts lack executable complete authority. Other ADs have named owners, revisions, variants, and evidence. |
| Deferred/open decisions | **Pass.** Product/Governance choices have owners, safe states, affected scopes, and revisit points; no outcome was invented. |
| Named technology / repository truth | **Pass.** Pins, root/submodule authority, unselected components, and unavailable seams are accurately represented. |
| Bound PRD and epics coverage | **Fail at C-R15-1/H-R15-2.** The target deletion success and restart/fixed-point requirements cannot be proven for operator scope or the pre-Provider capacity race. |
| Brownfield ratification | **Pass.** Present implementation is debt, not falsely declared target parity. |
| Security, tenancy, data loss | **Fail at C-R15-1.** Tenant and actor boundaries otherwise remain strong. |
| Recovery / operations / environment | **Fail at H-R15-1/H-R15-2.** Other branch-pinned, lost-ack, RPO-0, restore, spool, export and migration recovery remains explicit. |
| Sources / mechanics | **Pass.** Sources resolve as scoped, AD IDs are stable, and deterministic lint passes. |

## Architecture Defects Versus Delivery Debt

C-R15-1 and H-R15-1/H-R15-2 are target architecture defects: downstream builders cannot repair them by simply implementing the documented target because the required cut/authority/cohort is absent or contradictory. M-R15-1 through M-R15-3 are planning/clarity debt and L-R15-1 is build maintenance.

The missing implementation of every new target protocol remains delivery debt and does not reduce these architecture severities. Conversely, all `Uncommitted` seams, current direct/plaintext interaction state, no-op protection, missing public vocabulary, Dapr/root pin drift, and sprint-evidence discrepancy remain visible and safely blocked rather than being misclassified as target defects.

## Required Correction Order

1. Close C-R15-1 by serializing both deletion origins with directory permits/protected outboxes/effect acquire-and-commit and draining every committed target/external effect before destructive eligibility.
2. Close H-R15-1 by adding User-action-intent outboxes to the authoritative spine migration-repair cohort and bridge/drain/revocation proof.
3. Close H-R15-2 with a narrow pre-Provider capacity cancellation/release decision, owner acknowledgement, deletion recovery grant, and Closing race fixtures.
4. Re-distill the resulting current rules without renumbering ADs, append decisions to the memlog, freeze hashes, and rerun the complete gate. PASS requires zero Critical and zero High.
5. Retain the owner-blocked assumption dates, posting timeout, and Product decisions until their owners act; do not invent outcomes.

## Gate Conclusion

The v15 additions are substantive and the v14 effect-order fix remains closed, but the new deletion-scope fence is not yet the complete admission/effect cut that both deletion origins require, and two finite recovery cohorts remain incomplete. Verdict: **FAIL** until the whole-artifact Critical/High count reaches zero.
