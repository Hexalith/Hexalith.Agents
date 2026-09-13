---
name: Hexalith Agents good-spine rubric review v13
type: architecture-spine-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
intent: review-read-only
lens: bmad-architecture good-spine rubric walker
verdict: fail
critical: 1
high: 0
medium: 3
low: 1
lint_ok: true
---

# Good-Spine Rubric Reviewer Gate — v13

## Verdict

**FAIL — 1 Critical, 0 High, 3 Medium, 1 Low.** The v13 candidate closes the v12 migration, plaintext-decision, deletion-inventory, generated-output, recorder-bootstrap, destruction-recovery, FR-8-order, and dependency-chain findings. It does not yet close the gate because the normative interaction sequence performs approval-time safety and pre-post safety/Conversation reads before acquiring and committing the effect lease that AD-6, AD-7, AD-12, the matrix, and the deletion barrier require to authorize those reads. A literal sequence implementation can process protected content after `ConversationDeletionBarrierEffective` and only then discover that lease acquisition is rejected.

## Frozen Inputs And Method

I read the complete current spine, implementation conventions, architecture memlog, authoritative validation report, bound PRD, epics, both registers, repository instructions, and focused current repository contracts. I walked every good-spine dimension: downward divergence, enforceability of every AD, PRD coverage, brownfield truth, ownership and recovery, security/data-loss boundaries, operational/environmental envelope, deferred/open dimensions, implementation-debt separation, source resolution, stable identifiers, and mechanical lint. The five v13/v5 reviewer paths named in frontmatter were treated as deliberate concurrent outputs, not missing sources; every other declared local source resolved.

The supplied hashes matched at intake and immediately before report creation:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `76adbcfe3dd7f07cc836f5c55b31d7b40945078171fcd53c9e30fb3929969efb` |
| `IMPLEMENTATION-CONVENTIONS.md` | `b619ea870627e299dbec0e82ecefde1b3c182bc0a27bb5a5c04653ab091f3b34` |
| architecture `.memlog.md` | `8128e951ca03c455af19965070932bffd37153c243d0afc387b64b76b43ca07d` |
| bound `prd.md` | `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb` |
| `epics.md` | `ddbf1fe179ee3dd49ece7cad6fa3f5f34814f21d6669c64d9a92458954beebe9` |
| `external-dependency-register.md` | `b1d839b2a55289676e8bf09b737d135fb9c02f610ddbc11131bb10294d91ea27` |
| `launch-readiness-register.md` | `ec6eb4cf624acb88c92a919bca594de3a8f7ffc7b8b77c76f65cde7fe3e17c7e` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

## Deterministic Linter

Command:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: `ok: true`, `total_findings: 0`, no duplicate AD IDs, placeholders, missing `Binds`/`Prevents`/`Rule`, or mechanically unpinned Stack entries.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 0 |
| Medium | 3 |
| Low | 1 |

## Critical

### C-R13-1 — Approval and posting dependency calls cross the deletion cut before their effect lease exists

**Classification:** target architecture consistency and protected-data/deletion-cutover defect; not implementation debt and not an unresolved Product choice.

**Conflicting authority.** AD-6 says every approval mutation and Conversation-posting phase must first reserve its operation lease, then win same-owner `CommitConversationEffect`, and only that commit revision may authorize the exact downstream append, dependency read, or external effect (`ARCHITECTURE-SPINE.md:232`). AD-12 repeats that reservation authorizes nothing and that only `CommittedToEffect` permits the exact dependency read/effect (`:300`). AD-7 says the pre-post participant-state/access/membership step runs *inside* the committed `ConversationPosting` lease (`:242`). Matrix v4 likewise says `Reserved` authorizes no target read/effect and requires a committed posting lease for `ConversationPosting:BeginUnderEffectLease` (`launch-readiness-register.md:241-249`).

The normative sequence orders the opposite twice. In Automatic mode it calls the safety adapter and Conversations existence/access/membership read before `reserve then commit ConversationPosting` (`ARCHITECTURE-SPINE.md:656-660`). In Confirmation mode it calls approval-time safety before `reserve then commit ProposalMutation` (`:666-669`), then later calls pre-post safety and Conversations existence/access/membership before `reserve then commit ConversationPosting` (`:672-676`). The conventions also say the lease commit occurs after the last mutable checks while simultaneously saying only that commit authorizes reads/effects (`IMPLEMENTATION-CONVENTIONS.md:15,19`), without partitioning which checks are safe before the cut and which are the committed target phase.

**Concrete race and impact.** A stale approval/post worker can pass or retain earlier state, `InstallBarrierClosing` can then cancel all visible reservations and reach `Effective`, and the worker following the sequence can still decrypt/read the proposal for an external safety call or query Conversations because it has not attempted lease acquisition yet. Only afterward does acquisition/commit reject. No Conversation message is appended, but protected content has already been processed outside the deletion manifest after the architecture's declared effective cut. Another team following AD-6/AD-7 will lease those calls first. These implementations diverge on a critical erasure and external-processing boundary.

**Required correction — AUTOFIX mechanics.** Define one executable order for approval-time safety and each pre-post phase across AD-6, AD-7, AD-12, AD-20, the sequence, conventions, matrix, and Story 7.4. Either (a) make the relevant safety/Conversation observations explicit target steps authorized by a committed `ProposalMutation`/`ConversationPosting` lease, allow their typed no-mutation/no-post result to settle that committed lease, and place acquisition/commit before those calls; or (b) introduce separately committed deterministic safety/membership-read leases and commit posting only after their recorded results. In either design, `Reserved` must continue authorizing no content/dependency I/O, no content-bearing adapter call may start before a same-owner commit, Closing must manifest every call that can still be in flight, and fixtures must race Closing/Effective immediately before and after approval safety, pre-post safety, roster/access read, `BeginPosting`, and `AppendMessage`.

## High

None beyond the Critical finding. All other inspected v12 Critical/High corrections are present and mutually consistent at this snapshot.

## Medium

### M-R13-1 — Architecture-owned `RQ-1` assumptions still have milestone text instead of literal retirement dates

PRD FR-28 requires every Architecture-owned `RQ-1` assumption to carry a co-owner-approved literal calendar date and says a milestone or unset date blocks on that ground alone (`prd.md:730,894-898`). `ARCH-A-1`, `-2`, `-3`, the test-stack portion of `-4`, `-6`, `-7`, `-8`, `-11`, `-12`, and `-14` remain `Unscheduled` or milestone-only (`ARCHITECTURE-SPINE.md:1173-1186`). The spine correctly emits `UnretiredAssumption`, so this is visible governance incompleteness rather than an unsafe runtime default. Obtain recorded dates or keep the affected qualification blocked; do not invent them in Architecture.

### M-R13-2 — The `PostingPending` attempt timeout remains intentionally unresolved

AD-5 fixes only a lower bound—no shorter than the committed Conversations posting timeout—while `ARCH-A-14` records no concrete duration or single configuration authority (`ARCHITECTURE-SPINE.md:214,1186`). Story 7.4 correctly stays blocked on retirement (`epics.md:2548,2599`), preventing code-local invention. Architecture/Product must approve a duration/range or an explicit seam-owned configuration rule before that story becomes ready.

### M-R13-3 — The final build substrate still carries accumulated correction history rather than a fully compact distillation

The 1,198-line spine contains amendment-shaped clauses such as `Effect-lease and migration capability correction`, `Platform approval boundary`, historical Stack correction narration, and long supersession explanations inside current Rules (`ARCHITECTURE-SPINE.md:472-478,731-753`). They are generally authoritative and do not create another C/H defect, but they make the current rule harder for a downstream builder to distinguish from the historical rationale that belongs in the memlog. After C-R13-1 is fixed, consolidate each AD to its final rule while preserving every stable AD/OD identifier and the memlog history.

## Low

### L-R13-1 — The root bUnit override remains behind the current workspace catalog

The root pins `bunit` `2.9.0`, while the current Builds checkout catalog pins `2.10.3` (`Directory.Packages.props:26`; `references/Hexalith.Builds/Props/Directory.Packages.props:317`). The spine reports this accurately and assigns test-stack alignment to Story 5.6. This is delivery maintenance, not an architecture contradiction.

## Authoritative 2026-09-12 Critical/High Closure Audit

| Authoritative finding | v13 disposition |
| --- | --- |
| C-1 weaker safety evaluation | **Closed.** AD-20 requires snapshot/current conjunction and permits collapse only with a machine-checkable semantic-dominance proof. |
| C-2 hold/deletion exclusion | **Not closed artifact-wide: C-R13-1.** The fence and lease state machine are sound, but the sequence leaves approval/pre-post safety and Conversation reads outside the Closing/Effective manifest. |
| C-3 bootstrap/matrix scope | **Closed.** Platform/Tenant scopes, typed direct preconditions, EventStore self-bootstrap, containment pull, recorder bootstrap, and migration bootstrap avoid circular reads. |
| H-1 scheduled Approver re-check | **Closed.** Owner, cadence, single-flight scheduling, two-pass empty evidence, state outcomes, and unavailable recovery are bound. |
| H-2 distributed safety rescan | **Closed.** Epoch/index ownership, frozen finite cohorts, fenced coordination, on-demand initialization, and `RescanPending` are explicit. |
| H-3 human-only Approvers | **Closed.** Current human classification/liveness and historical actor binding fail closed. |
| H-4 ledger lifetime conflation | **Closed architecturally.** Rate, open-interaction, and monthly monetary ledgers have separate owners, decisions, clocks, and recovery. The unresolved joint consumption outcome remains correctly Open. |
| H-5 human identity/separation | **Closed.** Stable `AuthenticatedHumanActorId` spans User, Administrator, and Platform evidence and drives separation. |
| H-6 proposal-index crash consistency | **Closed.** Interaction truth, same-append outbox, source-revision high-water, checkpoint reconciliation, and recovery are bound. |
| H-7 indivisible Conversations dependency | **Closed.** Six core seams and optional retraction are separate records with separate consumers. |
| H-8 trusted-envelope security | **Closed.** Canonical authenticated bytes, logical/delivery identities, reserved-system replay owner, lifetime/rotation/revocation, ACLs, and lost-ack recovery are explicit. |
| H-9 export ownership/lifecycle/signature | **Closed subject to the surfaced Product lifecycle choice.** Immutable encrypted storage, index, fence, phase pin, manifest signature, key-delivery recovery, hold/restore treatment, and all-copy purge receipts are bound. |
| H-10 current Dapr exposure | **Closed as architecture truth.** The spine accurately distinguishes root-authoritative transitive `1.18.5`, the non-authoritative Builds checkout at `1.18.7`, and future Workflow adoption. |
| H-11 public-contract parity overclaim | **Closed.** Required completion parity and current shipped contracts are explicitly separated. |
| H-12 sprint/evidence contradiction | **Closed as architecture-vs-delivery classification.** The discrepancy remains visible through `OD-SPRINT-5.1-5.2-1`; the spine does not rewrite delivery history. |

## v12 Critical/High Correction Audit

| v12 finding / correction | v13 disposition |
| --- | --- |
| C-R12-1 revocable effect lease | **Core state-machine correction present, but full closure fails at C-R13-1.** `Reserved`, `CommittedToEffect`, `Settled`, `CancelledBeforeCommit`, same-owner commit/Closing serialization, and non-revocable committed work are bound; the sequence still performs named protected dependency calls before the lease. |
| H-R12-1 circular/inaccessible migration bootstrap | **Closed.** A fresh Platform Operator starts one target-limited `InteractionDirectoryMigration` Workflow; Begin omits only the circular audit/protection/deletion gate and uses exact direct evidence. |
| H-R12-2 legacy plaintext choice absent | **Closed by surfacing, not selection.** `OD-LEGACY-PLAINTEXT-DISPOSITION-1` has owners, affected evaluations, safe state, Story 6.1 negative evidence, and runtime materialization; nonempty plaintext remains blocked. |
| H-R12-3 destruction re-entered mutable gates | **Closed.** `DeletionDestructionStarted` and purge/completion recovery are gate-free continuations pinned to the recorded Resume/armed/prepare decisions and direct target evidence. |
| PRD FR-8 order | **Closed.** Directory registration is intake only; rate/open step 5, Confirmation Approver step 6, Context 7, Budget 8, safety 9, and leased membership 10 precede acceptance consistently across AD-7, AD-13, Story 6.1, and the matrix. |
| Deletion inventory freeze/fence race | **Closed.** `ProtectedDeletion` stores reversible candidates; `ProtectionFence` owns the immutable accepted set, expected-revision conflict forces same-request refreeze, and acknowledgement loss reads the fence. |
| Generated-output mutation gap | **Closed.** A distinct committed `ProposalMutation` lease is mandatory before sealed Provider output persists; a losing Closing race discards content. |
| Deletion Workflow settlement authority | **Closed for the manifested Provider branch.** Its Budget/capacity variants are existing-state, manifest- and outcome-revision-bound and forbid reserve/acquire/invoke/outcome invention. |
| Bootstrap catalog digest informational only | **Closed.** The first catalog must match and consume the exact version/digest committed by recorder bootstrap; successors require ordinary predecessor authority. |
| Late legacy write cutover | **Closed.** The EventStore namespace epoch guard and revoked legacy capability cover disconnected/queued/restored writers; accepted post-fence legacy writes invalidate readiness and cannot coexist with passing evidence. |
| Story 6.1 dependency omissions | **Closed.** Story and register consumers now include host, protection, secrets, Parties, Conversations, topology, Dapr security, and conditional legacy-plaintext blockers. |

## AD IDs, Memlog, And Source Integrity

- The current spine contains exactly one each of `AD-1` through `AD-31`; no ID was renumbered, reused, or duplicated.
- Memlog lines 382-390 append the v13 decisions for effect phases, FR-8 order, migration authority/write fence, the surfaced legacy-plaintext OD, fence-owned deletion inventory, narrow settlement/generated-output authority, bootstrap catalog commitment, phase-pinned destruction, and Story/register reconciliation. Earlier history remains intact.
- `OD-LEGACY-PLAINTEXT-DISPOSITION-1` and all other unresolved Product/Governance choices remain Open with owners, affected evaluations, and fail-closed states; no outcome was inferred.
- Every declared local source resolved except the five deliberately anticipated concurrent v13/v5 review outputs identified in the task. This report creates only its own anticipated path.

## Whole Good-Spine Checklist

| Dimension | Result |
| --- | --- |
| Fixes downward divergence | **Fail at C-R13-1; otherwise pass.** Aggregate owners, effect identities, recovery, principal unions, decision authority, and data fences are explicit. |
| AD enforceability / prevents stated divergence | **Fail at C-R13-1.** The lease rule is enforceable, but the authoritative sequence orders protected calls outside it. Other ADs expose deterministic owners, expected revisions, typed failures, and fixtures. |
| Deferred/open decisions | **Pass.** Every unresolved Product/Governance choice has a safe state and revisit owner; Architecture selects none. Medium date/timeout work remains visibly blocked. |
| Named technology / repository truth | **Pass.** Root gitlinks, root overrides, current submodule drift, transitive Dapr use, unselected host/provider SDKs, and no-op/current missing capabilities are reported without claiming shipped target parity. |
| Bound PRD and epics coverage | **Pass except C-R13-1's deletion interaction.** FR-1..FR-34, NFR-1..NFR-14, OQ decisions, Story 6.1, and Story 8.3 are traced. |
| Brownfield ratification | **Pass.** Current direct/plaintext interaction state and effect-first approval/posting remain delivery debt; the target does not claim implementation. |
| Security, tenancy, data loss | **Fail at C-R13-1; otherwise pass.** Tenant hashing/scoping, actor evidence union, trusted replay, protected envelopes, target key alias, hold/export/deletion fence, durable source delivery, and all-copy receipts are explicit. |
| Recovery / operations / environment | **Pass.** Platform-host ownership, topology, RPO 0 recovery, branch-pinned governance recovery, durable spooling, qualification evidence, capacity, and deployment blockers cover the initiative envelope. |
| Sources / mechanics | **Pass.** Local-source resolution as scoped, unique AD IDs, pinned Stack values, and deterministic lint all pass. |

## Unresolved Product/Governance Decisions — Surfaced, Not Selected

`OD-HOLD-DELETION-PRECEDENCE-1`, `OD-HOLD-PREPARE-CANCELLATION-1`, `OD-EXPORT-LIFECYCLE-1`, `OD-RATE-CONCURRENCY-CONSUMPTION-1`, `OD-DAPR-SECURITY-1`, `OD-SPRINT-5.1-5.2-1`, `OD-PRD-OQ18-HISTORICAL-SAFETY-1`, `OD-PRD-OQ23-AUTOMATIC-RETRACTION-1`, `OD-PRD-OQ31-INSTRUCTION-PROTECTION-1`, `OD-LEGACY-PLAINTEXT-DISPOSITION-1`, and `OD-RELEASE-RECORDER-SCOPE-1` remain Open on their recorded scopes. Their safe states are coherent and no finding asks Architecture to choose their outcomes. The assumption retirement dates and posting timeout in Medium likewise require owner approval rather than reviewer invention.

## Architecture Defects Versus Delivery Debt

C-R13-1 is an architecture defect: the target documents contradict each other about which durable fact authorizes protected safety and Conversations reads. M-R13-1 through M-R13-3 are governance/distillation debt in the planning substrate. L-R13-1 is delivery maintenance.

The current repository's missing interaction directory/migration, effect leases/barrier, three-ledger protocol, safety epoch/index, decision catalog/recorder, trusted replay/security spool, protection fence/export store/key delivery, Conversation deletion source feed, and complete public vocabulary remain correctly classified as implementation debt. Current plaintext `InteractionRequested.Prompt`, direct AgentInteraction streams, effect-before-dispatch approval/posting, no-op payload protection, and incomplete external seams are not recast as architecture defects. All relevant external records are still `Uncommitted`, live content-bearing execution remains blocked, and `RQ-1` remains NOT READY.

## Required Correction Order

1. Fix C-R13-1 by placing every approval-time and pre-post content/dependency call behind a same-owner committed lease (or behind separately committed deterministic leases), and align the prose, sequence, conventions, matrix, Story 7.4, and race tests.
2. Re-run the complete reviewer gate against one frozen snapshot; PASS requires zero Critical and zero High.
3. Obtain owner-approved literal assumption retirement dates and the posting timeout/configuration authority without inventing Product outcomes.
4. Re-distill amendment history into final Rules after semantic closure, preserving AD/OD IDs and the append-only memlog.

## Gate Conclusion

The v13 candidate is materially stronger and all v12 High corrections are present, but C-R13-1 means the complete spine still permits two incompatible deletion-cut implementations, one of which can process protected content after `ConversationDeletionBarrierEffective`. Verdict: **FAIL** until the whole-artifact Critical/High count reaches zero.
