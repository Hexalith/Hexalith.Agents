---
name: Hexalith Agents adversarial-divergence review v12
type: architecture-review
lens: adversarial-divergence
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
verdict: fail
critical: 2
high: 5
medium: 2
low: 0
lint_ok: true
---

# Adversarial Divergence Review — v12

## Verdict

**FAIL — 2 Critical and 5 High architecture divergences remain.** The frozen revision closes the authoritative validation report's original safety, shared protection-fence, matrix-v4 scope, Approver, three-ledger, proposal-index, dependency-split, trusted-envelope, Dapr-reality, public-parity, and tracking-language findings as they were stated. A fresh independent-implementation pass found two blocking defects in the newly added effect-cutover and brownfield-directory bootstrap protocols, plus five High gaps around deletion inventory fencing, deletion-workflow settlement authority, generated-version leasing, recorder bootstrap, and late legacy writes.

The deterministic architecture linter passed with `ok: true` and zero findings. Mechanical lint does not close the semantic findings.

## Frozen Snapshot And Method

The following inputs were SHA-256 checked before the review and again after this report was written:

- `ARCHITECTURE-SPINE.md`: `f77a939e0802e0c75375281a9f43a333df318de29f8b0fc4f0e0da8ba797bd25`
- `IMPLEMENTATION-CONVENTIONS.md`: `d40aa9745f80b9d0b6ac65a0e5b37f7fa7b5ed1b6f8ff19c11725192d23d325b`
- architecture `.memlog.md`: `63bf950c91f781760986d9bb0789af931396313305c1f64739435cfc7e3fbbec`
- bound `prd.md`: `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb`
- `epics.md`: `28b517b0d1d76463672e9852748a4f6d1e54362f827e38391ab0b57d0c1e13a2`
- `external-dependency-register.md`: `a63a8f69384ae3b67d4f324ccca8888195eb4caba1b415955af93f303acb66a5`
- `launch-readiness-register.md`: `164278a340b71b84831e67addd9d7c809032c157853a59167845bb4b2248bb32`
- authoritative `VALIDATION-REPORT-2026-09-12.md`: `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`

The review constructed separately implemented directory, interaction-workflow, Provider, Conversations, governance, protection-store, decision-recorder, and readiness units and asked whether each could obey every literal local contract yet produce a different global outcome under concurrency, crash, replay, lost acknowledgement, stale projection, cross-tenant substitution, or recovery. The cited pending v12/v4 reviewer deliverables were deliberately not treated as missing-source findings.

Repository reality was inspected at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2` without initializing, updating, or mutating a submodule. Parent-authoritative gitlinks remain Builds `a32cb422749352cce8dec948aa3e78c8f00eb4cf`, Conversations `73bcee6f04479d4743d5a65ce929728e22687d7d`, EventStore `ce9e779a3ec24d9b9a054afd0ceaa3c05b8adaaa`, FrontComposer `053b2008307d4e476c0d4329e6c47763c301d43e`, Memories `3644ef63da87b8c7ec9e17726fae7f534e642a1c`, Parties `fa42398552fba1c80eb2760791517659d6d1313a`, and Tenants `2fac18396ff11a4459de053b3ebb7ddfe7c13e30`. The first five checked-out submodules differ from their parent gitlinks; that local drift is not architecture authority.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 2 |
| High | 5 |
| Medium | 2 |
| Low | 0 |

## Critical

### C-1 — An `Active` effect lease can be revoked while its already-authorized worker begins the effect

**Classification:** target architecture safety/data-integrity defect.

**Evidence.** A `ConversationEffectLease` has only `Active`, `Settled`, and `RevokedBeforeEffect` outcomes (`ARCHITECTURE-SPINE.md:189`). Closing manifests every Active lease; a pre-Closing external lease may either complete or be marked `RevokedBeforeEffect` after an authoritative lookup says it never began (`ARCHITECTURE-SPINE.md:227`). Provider order separately appends authorization on `AgentInteraction`, transitions capacity to `InvocationActive`, and invokes only after checking that the lease is current (`ARCHITECTURE-SPINE.md:307`; sequence at `ARCHITECTURE-SPINE.md:611-626`). The recovery row permits `RevokedBeforeEffect` from outcome proof while the lease is still merely Active (`launch-readiness-register.md:242`). No same-owner event atomically commits that Active lease to an effect-begun/must-settle phase before the external call, and no Provider, Conversations, Dapr, or target aggregate conditionally consumes the directory revision.

**Two literal units.** Team A's deletion worker looks up `AttemptId`, gets authoritative “not begun,” and conditionally appends `RevokedBeforeEffect`. Team B's already-running interaction worker checked the Active lease immediately before A's append, already received `InvocationActive`, and then invokes the Provider with the durable `ProviderInvocationAuthorized` fact. Both obey their local ordering and expected revisions. The barrier can subsequently become Effective because A closed the manifest, while B starts processing after the claimed effective cut. The same check/revoke race exists for workflow start, local proposal mutation, and Conversations posting; an absence lookup or independent target revision cannot revoke an authorization already observed by a paused worker.

**Impact.** A Provider disclosure, post, workflow, or content-bearing append can begin after the lease was authoritatively revoked and even after deletion recorded its effective cut. Later cleanup cannot undo Provider processing or a Conversation disclosure. This defeats the central C-2 hold/deletion closure from the authoritative validation report.

**Required correction — AUTOFIX architecture mechanics.** Add one same-`ConversationAgentState` conditional transition between Active and effect execution, for example `EffectCommitted`/`MustSettle`, which contends at the lease revision with `RevokedBeforeEffect`. Only a committed-to-effect lease may cross the external/local-effect boundary; revocation is legal only while uncommitted, and the worker must conditionally win that transition after every last mutable check. Closing may allow a pre-Closing Active lease to choose exactly one of commit-to-effect or revoke, and Effective waits for every committed lease's authenticated outcome and settlement. Bind this phase and revision into Provider `BeginInvocation`, posting `BeginPosting`, workflow start, context materialization, proposal mutation, matrix variants, principal allowlists, and first/last failure-injection cases. A read-then-effect check is insufficient.

### C-2 — `InteractionDirectoryMigration` is both readiness-circular and principal-inaccessible

**Classification:** target architecture bootstrap/matrix defect.

**Evidence.** Calls and Conversation deletion require `DirectoryReady`; `BeginQuiescedCutover` is the only first migration operation (`ARCHITECTURE-SPINE.md:189,239`; `launch-readiness-register.md:231-235`). That Begin row requires `LR-AUDIT-PROTECTION-DELETION`, but the gate's own required evidence includes the legacy-directory cutover (`launch-readiness-register.md:231,426`). Matrix v4 permits omitting a circular gate only in an explicit target-aware bootstrap row (`launch-readiness-register.md:198-202`); this row omits none. Independently, AD-30's closed principal grants do not authorize any `InteractionDirectoryMigration` variant: Platform is rejected for every unlisted family, ordinary Workflow kinds are limited to their named operations, and no User/Administrator role receives a migration grant (`ARCHITECTURE-SPINE.md:455-457`). The migration rows contain no direct authorized-principal precondition (`launch-readiness-register.md:231-234`).

**Two literal units.** A strict matrix/principal implementation cannot issue the first migration command because the gate cannot pass until that command's protocol completes and no principal is eligible. A second team invents a Release-Operator or Platform maintenance exception and ignores/subtracts the gate subcondition. A third treats topology orchestration as an implicit Workflow principal. Only the first obeys the closed matrix and envelope rules, but it permanently blocks directory-first calls and source deletion; the others create incompatible, unaudited bootstrap authority.

**Impact.** No non-empty or proved-empty brownfield tenant can legally reach `DirectoryReady`, so the required architecture handoff is unusable. Invented exceptions also reopen cross-tenant migration and deletion-completeness risk.

**Required correction — AUTOFIX, no Product policy selection.** Add one explicit bootstrap/repair migration principal/capability and target scope, with a least-privilege allowlist for Begin, member backfill, complete, and late-write containment. Make Begin omit the whole circular `LR-AUDIT-PROTECTION-DELETION` record and replace it with closed direct protection, topology, tenant, EventStore, identity, and expected-revision preconditions; retain every unrelated required control. Record `RegistryCheckpoint.NotRead` only if the row is fully gate-free, otherwise the actual checkpoint. Add empty/non-empty, wrong-tenant, wrong-stream, unavailable-protection, concurrent Begin, and repair fixtures.

## High

### H-1 — The complete deletion set is frozen before the deletion owns a fence position, with no refreeze recovery

**Classification:** target architecture concurrency/recovery defect.

AD-22 says hold, export, and deletion first contend on `ProtectionFence`, yet Conversation deletion first appends an immutable complete set on `ProtectedDeletion` and only then submits it to fence evaluation (`ARCHITECTURE-SPINE.md:227,397`; `launch-readiness-register.md:263-265`). No earlier deletion-intent/fence-reservation variant exists. An export or hold may therefore win `ProtectionFence` after `FreezeDeletionSet` but before `DeletionPrepare`. The latter's `FrozenInteractionExportAndCopySetExact` correctly blocks the stale candidate, but `ConversationApprovedDeletion` cannot Abort, mint a new request id, or amend the immutable frozen set (`launch-readiness-register.md:264,266-268`).

Team A blocks forever on the stale set; Team B silently refreezes it; Team C evaluates “exact” at the earlier inventory revision and proceeds without the later export. These outcomes differ in completion and data coverage. Make the fence the owner of the accepted deletion inventory (then acknowledge it to `ProtectedDeletion`), or define a reversible candidate/refreeze loop that re-resolves after every fence conflict and becomes immutable only in the successful expected-revision fence append. Preserve the stable Conversation deletion identity and include export prepare/commit/cleanup and hold arrival at every boundary.

### H-2 — The deletion Workflow is required to settle Provider Budget/capacity state but lacks an exact downstream authority

**Classification:** target architecture authorization/recovery defect.

The Closing protocol requires Provider convergence to record usage/no-use/`Indeterminate`, settle or reconcile Budget, release capacity, and acknowledge the lease (`ARCHITECTURE-SPINE.md:227`; `launch-readiness-register.md:260-262`). AD-30's base Workflow grant permits Budget and ledger operations only “for its own interaction,” whereas `ConversationDeletionPropagation.WorkflowInstanceId` is the `DeletionRequestId`, not the target `AgentInteractionId`. Its special allowlist names `ConvergeInteraction` on the manifested interaction but no concrete BudgetLedger settle/reconcile or capacity-release command/variant; nevertheless prose says it may drive “ordinary Budget/capacity/posting settlement” (`ARCHITECTURE-SPINE.md:455-457`).

A strict ledger rejects the deletion principal; a permissive ledger treats any manifest target as “its own interaction”; another has the deletion workflow impersonate the Interaction workflow, which AD-30 forbids. The strict path leaves Active Provider leases permanently preventing Effective. Add exact manifest-bound, outcome-revision-bound settle/reconcile and release variants for this Workflow kind, limited to an existing prepared reservation/admission and forbidding reserve, acquire, invoke, post, or outcome invention. Reconcile both AD-30 allowlist clauses and the matrix.

### H-3 — Generated output has contradictory and incomplete `ProposalMutation` lease coverage

**Classification:** target architecture ordering/matrix defect.

AD-6 requires every generated mutation to acquire a `ProposalMutation` lease before its interaction authorization/effect (`ARCHITECTURE-SPINE.md:227`), but the normative sequence records `RecordGeneratedVersion` while only the earlier `ProviderInvocation` lease is held and never acquires `ProposalMutation` (`ARCHITECTURE-SPINE.md:611-626`). Matrix v4 has apply-under-lease variants only for edit, regeneration, and resolution, not generated-version/proposal creation (`launch-readiness-register.md:240`), while AD-17 says an absent concrete variant blocks (`ARCHITECTURE-SPINE.md:343`). AD-30's parenthetical lease-principal grant likewise names Interaction Workflow for Provider but not an explicit generated proposal-mutation operation (`ARCHITECTURE-SPINE.md:455`).

One team stores generated output under the Provider lease; another attempts a new ProposalMutation lease, which Closing may reject and the matrix cannot classify; a third stretches `ProposalRegeneration:ApplyUnderEffectLease` to initial generation. They disagree precisely when Provider output returns during deletion Closing. Add an exact generated-output/proposal-creation variant and principal rule, and state whether it uses a distinct ProposalMutation commit phase or is intentionally part of the committed Provider lease. Reconcile AD-6, AD-13, AD-27, diagram, matrix, and Story 6.1; under Closing, the specified discard-without-persistence outcome must be unique.

### H-4 — The bootstrap package's catalog digest is recorded but not enforced by catalog publication

**Classification:** target decision-governance defect.

`BootstrapRecorderScope` records the genesis/pending catalog digest that the independently signed package authorizes (`ARCHITECTURE-SPINE.md:351`; `epics.md:1549-1552`; `launch-readiness-register.md:224`). After bootstrap, `PublishCatalog` requires source/role and a root or predecessor authorization but has no precondition requiring the proposed catalog/version/digest to equal the catalog digest committed in the bootstrap record (`launch-readiness-register.md:226`). “Normal commands use the recorded source” constrains recorder authority, not the catalog bytes.

One team treats the bootstrap digest as a one-time genesis authorization; another accepts a different root-signed catalog from the same source; a third ignores the field entirely. The resulting required DecisionIds and affected evaluations can differ while every listed local check passes. Require the first catalog publication/activation to consume the exact bootstrapped catalog digest/version, or explicitly define the field as non-authoritative evidence and remove the claim that it is what the package authorizes. Successor catalogs must then use the ordinary effective-predecessor rule. This does not select `OD-RELEASE-RECORDER-SCOPE-1`'s Product outcome.

### H-5 — Late legacy writes are detected reactively and can appear after deletion freezes or destroys

**Classification:** target architecture migration/deletion integrity defect.

Completion requires a topology-cohort direct-create freeze and two identical inventory high-water passes, then a later legacy write merely appends `DirectoryInvalidated` and blocks future calls/deletions (`ARCHITECTURE-SPINE.md:189,239`; `launch-readiness-register.md:231-234`). `EXT-TOPOLOGY-1` is a qualification-fixture contract and does not supply an EventStore stream-prefix ACL, credential revocation, or conditional migration-epoch check that makes a legacy append impossible (`external-dependency-register.md:213-225`). `FreezeDeletionSet` checks migration readiness once; destruction and completion do not revalidate it (`launch-readiness-register.md:263,269-271`).

Team A implements “route frozen” as a feature flag on the recorded live cohort; a delayed old worker or credential can append a legacy direct stream after `DirectoryReady`. Team B requires EventStore ACL revocation. In A, the append can land after deletion set freeze or after destruction and before the observer records invalidation, leaving undeleted plaintext outside the immutable set. Make cutover an enforceable write fence: revoke/replace the legacy write capability at the authoritative EventStore boundary or require a current migration epoch/permit on every relevant append, including queued and restored writers. A late rejected attempt may trigger containment; an accepted post-ready legacy append must be impossible before any deletion is authorized. Add after-Ready, after-Effective, after-freeze, after-`DestructionStarted`, restore, disconnected-writer, and lost-observation fixtures.

## Medium

### M-1 — Legacy plaintext has a safe blocker but no materialized decision/procedure authority

The spine and Story 5.8 correctly refuse to call existing plaintext retroactively erasable and keep `PayloadProtectionUnavailable`, calls, deletion, and `RQ-1` blocked (`ARCHITECTURE-SPINE.md:189,239`; `epics.md:1741-1745`). However, the “separately authorized migration or eradication procedure” has no stable OD, owner aggregate, matrix family/variant, approval roles, inventory rewrite/destruction authority, or recovery contract. The current event vocabulary explicitly persists prompt/generated content (`src/Hexalith.Agents.Contracts/AgentInteraction/Events/InteractionRequested.cs:4-31`; `src/Hexalith.Agents.Contracts/AgentInteraction/Events/AgentOutputGenerated.cs:34-51`) and the root-authoritative EventStore default leaves bytes `Unprotected` (`references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/NoOpEventPayloadProtectionService.cs:7-30`), so this is a real brownfield branch, although actual deployed data is not asserted.

Keep the fail-closed behavior, but materialize the unresolved Product/Governance retention/disposition choice as a stable open decision with affected evaluations and define only the mechanics common to every possible outcome. Do not infer migration, wholesale tenant erasure, or accepted retention. Until decided, exact-empty or already-protected inventory remains the only readiness path.

### M-2 — Pre-arm hold deferral can be hidden by unrelated mutable gates

AD-22 says an active/preparing pre-arm hold produces durable, visible `DeletionDeferredByHold`, doing no preparation or irreversible work (`ARCHITECTURE-SPINE.md:397`; PRD `prd.md:957`). The dedicated row nonetheless inherits the full `DeletionPrepare` gate set, including `LR-SECRETS` and `LR-AUDIT-PROTECTION-DELETION` (`launch-readiness-register.md:264-265`). If either is unavailable, a strict implementation cannot record the content-free deferral even though the hold and frozen request are already authoritative; another follows the prose and records it. This remains safe because preparation is blocked, but status, acknowledgement, and recovery diverge. Give deferral its minimal direct recorded-origin/hold/fence checks and make unavailable ancillary services affect later preparation, not the durable fact that an existing hold deferred it.

## Authoritative Validation Critical/High Recheck

| Authoritative finding | v12 disposition |
| --- | --- |
| C-1 conjunctive safety | **Closed as stated.** Snapshot plus every current applicable version, dominance equivalence, retry dispositions, and audit evidence are explicit in AD-20. |
| C-2 legal-hold/deletion exclusion | **Closed for the shared `ProtectionFence` protocol.** C-1 and H-1/H-5 are newer cutover/inventory races introduced around Conversation deletion and migration, not recurrence of the original per-stream partial-erasure design. |
| C-3 bootstrap/matrix | **Closed for the original catalog/budget/provider/readiness operations.** C-2 is a new unbootstrappable family introduced after that correction. |
| H-1 scheduled Approver recheck | **Closed.** Durable scheduler, cadence/freshness, two-pass empty rule, state outcomes, and unavailable retry are present. |
| H-2 distributed safety rescan | **Closed.** Epoch/index owners, finite directory manifest, fenced coordinator, bounded workers, on-demand initialization, and `RescanPending` are explicit. |
| H-3 human Approvers | **Closed.** Parties-owned human/liveness checks and typed failures are explicit. |
| H-4 ledger lifetimes | **Closed architecturally.** Rate, original-caller open lease, and original-month Budget are distinct with exact decision/ack recovery. H-2 concerns the new deletion Workflow's authority to finish already-owned settlement. |
| H-5 stable human identity | **Closed.** Every human-originated principal carries and compares `AuthenticatedHumanActorId`; Party-free Administrator/Platform evidence is origin-tagged. |
| H-6 proposal index crash consistency | **Closed.** Interaction truth, source-revision outbox, high-water, reconciliation, and recovery are explicit. |
| H-7 dependency split | **Closed.** Core `EXT-CONV-AI-1` and optional `EXT-CONV-RETRACTION-1` remain separate. |
| H-8 trusted envelope | **Closed.** Canonical authenticated bytes, logical/delivery identities, reserved-system replay owner, retention, rotation/revocation, and ACL-confined durable denial recording are explicit. |
| H-9 export ownership/lifecycle | **Closed as originally stated.** Immutable encrypted store, index, signed manifest, common fence, lifecycle/open decision, purge receipts, restore/hold behavior, and principal-bound key-delivery recovery are bound. H-1 is the later deletion-candidate race before fence ownership. |
| H-10 current Dapr exposure | **Closed.** The spine accurately distinguishes parent-authoritative transitive Client/ASP.NET `1.18.5`, the non-authoritative Builds checkout at `1.18.7`, and future Workflow adoption. |
| H-11 public parity wording | **Closed.** Required completion parity is target work, not a shipped claim. |
| H-12 tracking/evidence contradiction | **Correctly separated as delivery-history debt.** `OD-SPRINT-5.1-5.2-1` preserves the unresolved owner choice and blocks dependent authorization without rewriting history. |

## Areas That Converge

- Posting retains durable `Approved`, then lease-bound durable `PostingPending`, then idempotent Conversations effect/result lookup; current effect-first code is explicitly debt.
- Export prepare phase-pins lifecycle/store/contract values; fence commit is singular; secondary acknowledgement alone enables the now-durable key-delivery protocol.
- Hold/deletion/export recovery dispositions are immutable and branch-pinned; source-approved deletion cannot borrow a local Abort.
- Rate admission uses one interaction-owned commit/abort authority across both rolling ledgers; reset instants are frozen; open-interaction and monthly Budget lifetimes remain separate. The open rate/concurrency Product decision correctly blocks before either ledger and was not selected here.
- Target interaction DEK alias behavior is now required by `EXT-PROTECTION-1` with dual-stream `Erased`, tenant-substitution, cache/snapshot, and restore-negative evidence.
- Conversation deletion source identity, durable delivery/backfill, acknowledgement, origin union, and stable logical id are mutually consistent across spine, PRD, epics, and register.

## Architecture Defects, Product Choices, And Implementation Debt

C-1, C-2, and H-1 through H-5 are target architecture defects requiring ownership, ordering, authorization, idempotency, or recovery mechanics; none requires choosing an unresolved Product outcome. M-1 is the one unresolved Product/Governance disposition that must be surfaced rather than invented. M-2 is a safe but divergent status/recovery rule.

No unresolved Product choice was selected. `OD-HOLD-DELETION-PRECEDENCE-1`, `OD-HOLD-PREPARE-CANCELLATION-1`, `OD-EXPORT-LIFECYCLE-1`, `OD-RATE-CONCURRENCY-CONSUMPTION-1`, `OD-DAPR-SECURITY-1`, `OD-SPRINT-5.1-5.2-1`, `OD-PRD-OQ18-HISTORICAL-SAFETY-1`, `OD-PRD-OQ23-AUTOMATIC-RETRACTION-1`, `OD-PRD-OQ31-INSTRUCTION-PROTECTION-1`, and `OD-RELEASE-RECORDER-SCOPE-1` remain Open on their recorded scopes.

Current implementation remains materially behind the target and is correctly described as delivery debt rather than evidence against an otherwise clear AD: no interaction directory/migration, permit/create/start outboxes, effect leases/barrier, Conversation deletion feed/workflow, three-ledger protocol, protection fence, audit export store/key delivery, decision catalog/recorder, trusted replay/security spool, safety epoch/index, or complete public vocabulary is shipped. Current proposal approval/posting still performs the external effect before the target durable authorization. The EventStore default remains no-op protection and the current Conversations client lacks the required membership/deletion seams. All dependency records remain `Uncommitted`, initial readiness is insufficient, and `RQ-1` is `NOT READY`. These facts do not inflate the findings above.

The root worktree and several submodule checkouts are dirty/parent-modified. Parent gitlinks, not checked-out descendants, remain dependency authority. Builds' parent gitlink exposes Dapr Client/ASP.NET `1.18.5`; its internally clean `cf52f74` checkout has `1.18.7` but is not a root-authoritative upgrade. No build, test, external-dependency availability, or release success is claimed.

## Required Correction Order

1. Add the same-owner effect commit/revoke race and bind every effect path before revisiting any deletion-completion claim (C-1).
2. Make directory migration bootstrappable and principal-authorized without subtracting readiness subconditions ad hoc (C-2).
3. Move accepted deletion inventory into a fence-owned/refreezable protocol, then make late legacy appends impossible rather than merely observable (H-1, H-5).
4. Close deletion-workflow Budget/capacity settlement authority and generated-version lease/matrix coverage (H-2, H-3).
5. Consume the bootstrap-authorized catalog digest at genesis (H-4), then materialize rather than select the legacy-plaintext Product/Governance decision (M-1) and minimize the deferral gate (M-2).
6. Re-distill, append architecture decisions to the memlog, rerun deterministic lint, freeze/hash, and rerun every configured reviewer lens.

## Lint And Final Integrity Result

`python3 .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2` returned `ok: true`, `total_findings: 0`. Final SHA-256 verification matched all eight frozen input hashes listed above; only this v12 report was created by this reviewer.

## Gate Conclusion

The complete adversarial-divergence gate is **FAIL: 2 Critical, 5 High, 2 Medium, 0 Low**. A PASS is not permitted until both Critical and all five High findings are closed and a fresh full gate returns zero Critical and zero High.
