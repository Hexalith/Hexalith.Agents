---
name: Hexalith Agents adversarial-divergence review v14
type: architecture-review
lens: adversarial-divergence
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
verdict: fail
critical: 1
high: 1
medium: 1
low: 0
lint_ok: true
---

# Adversarial Divergence Review — v14

## Verdict

**FAIL — 1 Critical, 1 High, 1 Medium, and 0 Low findings.** The v14 candidate closes all four v13 findings as they were stated: protected target reads now follow the same-owner lease commit; generated success and failure are coherent separate variants; the operator-origin deletion Workflow has exact candidate/refreeze/fence authority; and repair has a finite old-epoch bridge with successor revocation. A fresh whole-artifact construction nevertheless found one irreversible deletion race with migration invalidation and one committed public-mutation recovery hole. Both permit independently literal implementations to diverge, so this gate cannot pass.

The deterministic architecture linter passed with `ok: true` and zero findings. Mechanical lint does not close the semantic findings.

## Frozen Snapshot And Method

The following inputs were SHA-256 checked before review, immediately before report creation, and after this report was written:

- `ARCHITECTURE-SPINE.md`: `64bb2d2ee93a41b81a2e80de9b5a34697935f8adec617d0aea1596e031fc47ed`
- `IMPLEMENTATION-CONVENTIONS.md`: `b87de892ed928681ef2060e2523c2da7c035b8ccb3339d154e6254345b03a7a3`
- architecture `.memlog.md`: `64587e2da955fd8bff0a00435bb736977a67784418cfc1145e4c0c91d14646b6`
- bound `prd.md`: `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb`
- `epics.md`: `3785263498741896fa7a3f00cf226efdf39f90edab45a237cff11569160f0369`
- `external-dependency-register.md`: `219ad266c8fe665f27f981a3992217d7c95c293a9297f742cfc904cc7cb3efbf`
- `launch-readiness-register.md`: `ef5de17f6fb76fe9b26656752723dbb3d2d132344e853e369564d34fc94bd3da`
- authoritative `VALIDATION-REPORT-2026-09-12.md`: `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`

The review constructed independent directory, interaction-workflow, public proposal-command, Provider, Conversations, EventStore-guard/migration, protection-fence, deletion-workflow, decision-recorder, and recovery units. Each was constrained to literal AD, matrix, convention, PRD, epic, and register clauses, then combined under expected-revision races, crash/lost acknowledgement, replay, restore, role revocation, cross-tenant input, Closing/Effective, migration invalidation, and irreversible deletion boundaries. The v13 findings were rechecked separately before seeking new divergence.

Repository inspection was read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`; no submodule was initialized, updated, or mutated. Root gitlinks remain Builds `a32cb422749352cce8dec948aa3e78c8f00eb4cf`, Conversations `73bcee6f04479d4743d5a65ce929728e22687d7d`, EventStore `ce9e779a3ec24d9b9a054afd0ceaa3c05b8adaaa`, FrontComposer `053b2008307d4e476c0d4329e6c47763c301d43e`, Memories `3644ef63da87b8c7ec9e17726fae7f534e642a1c`, Parties `fa42398552fba1c80eb2760791517659d6d1313a`, and Tenants `2fac18396ff11a4459de053b3ebb7ddfe7c13e30`. The first five checked-out submodules differ from their root gitlinks; Parties and Tenants match. Those checkouts are not planning authority.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 1 |
| Medium | 1 |
| Low | 0 |

## Critical

### C-v14-1 — An accepted late legacy append can race a prepared deletion and the gate-free destruction path no longer checks the migration owner

**Classification:** target-architecture irreversible-deletion and integrity-recovery defect.

**Evidence.** The directory contract explicitly says an accepted post-fence legacy append records `DirectoryInvalidated` and blocks deletion (`ARCHITECTURE-SPINE.md:197`). Candidate acceptance is supposed to compare the current migration-write-fence revision, and a winning migration epoch change is supposed to force same-request refreeze (`ARCHITECTURE-SPINE.md:238-240,409`). The concrete accept row, however, requires `MigrationWriteFenceEpochCurrent` only for `ConversationApprovedDeletion`, not for operator-origin deletion (`launch-readiness-register.md:279-282`). More importantly, after either origin has prepared, `DeletionDestructionStarted`, purge, and completion are deliberately gate-free and require the recorded accepted set and phase-pinned lifecycle/store authority but no current `InteractionDirectoryMigration` revision, no `DirectoryInvalidated` absence, and no post-acceptance namespace-write-fence receipt (`launch-readiness-register.md:283,288-290`; `ARCHITECTURE-SPINE.md:375,409,419`). Migration state and `ProtectionFence` are different aggregate owners, so the accepted-set append does not serialize a later migration invalidation.

**Two literal units.** Team A applies AD-2's “blocks deletion” rule, reads the migration owner immediately before `DestructionStarted`, and refuses/refreezes when its accepted epoch is no longer current. Team B applies the exact matrix-v4 recorded-branch row: after prepare/Resume/arm it reads no mutable readiness and proceeds under the accepted set even if an accepted stale writer has since appended an omitted interaction/failure stream and invalidated the directory. Team C applies the migration check only to the Conversation-origin row, as the matrix literally says, while allowing operator-origin acceptance against a stale epoch. All three can cite a normative clause. B can destroy every member of its immutable set and report completion while the newly accepted legacy member survives outside it.

**Impact.** The system can irreversibly claim a Conversation/class deletion complete while derived sensitive content accepted during the migration integrity incident remains undeleted. This reopens the authoritative report's hold/deletion Critical at a later cut even though ordinary candidate-versus-fence races are now sound.

**Required correction — architecture mechanics, not a Product decision.** Thread the exact migration epoch/guard receipt through candidate, fence acceptance, prepare, and the one `DestructionStarted` decision for both origins. Before irreversible start, read the authoritative migration owner directly: invalidated, changed, unknown, or repair-pending state must retain the deletion fence, forbid destruction, and refreeze/reaccept after repair under the same authorized scope/request. Define the EventStore-boundary rule that prevents any post-start legacy/current create or content-bearing append inside the accepted destructive scope; if that guard is ever violated, completion must stay impossible and the integrity-containment procedure must account for the new exact member rather than report success. Keep already-started destruction recovery phase-pinned; do not make it depend on a mutable catalog/readiness successor.

## High

### H-v14-1 — A committed User-owned proposal mutation has no durable payload/principal handoff that can recover after the request process dies

**Classification:** target-architecture authorization, crash-recovery, and deletion-progress defect.

**Evidence.** Edit, regeneration, and resolution now reserve and commit a content-free `ProposalMutation` lease before protected version/content access or safety/dependency reads; committed leases are non-revocable and must produce a target result and settle before Effective (`ARCHITECTURE-SPINE.md:195,327`; `IMPLEMENTATION-CONVENTIONS.md:19`). AD-30 says the same authenticated `User`, not the interaction Workflow, owns edit/regeneration/resolution leases, while `SettleOrRecover` accepts the interaction Workflow or a Closing-bound deletion Workflow (`ARCHITECTURE-SPINE.md:477,479`). Matrix v4 defines the post-commit apply result and generic settlement but no durable public-action intent/outbox, protected edited-payload location, or target-limited Workflow recovery principal (`launch-readiness-register.md:254,256`). Story 7.4 similarly commits the lease before the protected read and relies on generic recovery after crash (`epics.md:2554-2557,2574-2577`). Dapr Workflow is otherwise the required durable owner for restart recovery (`ARCHITECTURE-SPINE.md:377-381`), and workflow execution state may retain references only (`ARCHITECTURE-SPINE.md:451-455`).

**Two literal units.** Team A enforces the principal rule. If the API process crashes after lease commit but before the `AgentInteraction` append, there is no authenticated User ingress left to perform the result; for edit there is also no durable protected copy/reference for the edited bytes. The committed lease cannot be cancelled, so it can never settle and a later Conversation deletion can never reach Effective. Team B lets the Interaction Workflow replay the mutation from whatever request data it retained, contradicting the exact-User grant and, for edited content, the reference-only execution-state boundary. Team C fabricates a fresh User envelope from recorded actor metadata, contradicting fresh ingress identity/role binding. These teams disagree on both authority and state while each preserves a different literal invariant.

**Required correction.** Add one durable pre-effect handoff for every User-owned `ProposalMutation`: an immutable target-limited action intent/outbox at an authoritative owner, with original human identity/evidence, operation/version/idempotency, expected revisions, and any edited bytes sealed under the target interaction DEK or represented by a durable protected reference. The same-owner commit must bind that intent. Grant one named recovery principal/variant permission only to re-evaluate current authorization/dependencies and record the exact positive or typed negative result from that committed intent, then settle; it must not impersonate the human, broaden the action, or satisfy second-party authority. Crash before commit remains cancellable; crash after commit must converge even if the browser disappears or the actor is later revoked (revocation produces the typed negative result).

## Medium

### M-v14-1 — Effective-barrier matrix predicates do not enumerate rate/open and pre-Provider Budget acknowledgement even though the prose requires every ledger settlement

**Classification:** target handoff precision defect.

**Evidence.** Rate/open admission precedes the Context lease, and estimated Budget reservation occurs before safety (`ARCHITECTURE-SPINE.md:323-329,599-622`). The Closing contract says Effective waits for every required ledger settlement (`ARCHITECTURE-SPINE.md:238-240`). The concrete deletion Workflow grants only Provider-attempt Budget settlement and capacity release; directory acknowledgement calls out Budget/capacity/posting, while `ActivateBarrierEffective` enumerates permits/outboxes and effect leases but does not name pending rate/open decisions or a pre-Provider `NotInvokedRelease` acknowledgement (`ARCHITECTURE-SPINE.md:483`; `launch-readiness-register.md:274-278`).

**Divergence and impact.** One team waits for ordinary Interaction Workflow recovery to drive every rate/open/pre-Provider Budget decision and acknowledgement before Effective; another considers the interaction terminal outcome sufficient and lets a stale content-free ledger worker commit/abort after Effective. This does not expose protected content, so it is Medium, but it can leave quota, concurrency, cost status, and NFR-11 evidence inconsistent.

**Required correction.** Make `AcknowledgeDirectoryOutcome`/`ActivateBarrierEffective` enumerate every applicable rate, open, and Budget owner decision plus acknowledgement, and identify whether the ordinary Interaction Workflow or a narrowly manifest-bound deletion recovery variant drives each existing preparation/reservation. No variant may reserve new capacity or invent an owner outcome.

## v13 Critical/High Recheck

| v13 finding | v14 disposition |
| --- | --- |
| C-v13-1 protected reads before effect commit | **Closed as stated.** The general rule, convention, matrix, Story 7.4, and both posting branches now commit `ProposalMutation`/`ConversationPosting` before approval safety, protected-version access, pre-post safety, or Conversations reads; typed negative results are lease-bound and settle (`ARCHITECTURE-SPINE.md:195,327,656-710`; `IMPLEMENTATION-CONVENTIONS.md:19`; `launch-readiness-register.md:247,254-256`). H-v14-1 is the distinct crash handoff after a valid commit. |
| H-v13-1 impossible generated-failure row | **Closed.** Provider/timeout/output-safety negative outcomes use a content-free `GenerationFailed` result under the committed Provider lease with proposal/version/content/failure-record identity absent; only allowed complete output uses `ProposalMutation(RecordGeneratedVersion)` (`ARCHITECTURE-SPINE.md:327,653-672`; `launch-readiness-register.md:252-253`). |
| H-v13-2 missing operator deletion candidate authority | **Closed.** The exact operator-origin workflow, immutable human-approved scope, candidate/refreeze/accept/ack/defer allowlist, and matrix rows now exist (`ARCHITECTURE-SPINE.md:481`; `launch-readiness-register.md:279,281-285`). C-v14-1 concerns migration invalidation after/against that otherwise valid authority. |
| H-v13-3 unspecified old-epoch capability fate | **Closed as stated.** Repair freezes the complete permit/outbox/lease cohort, bridges only exact pending outboxes and committed leases, suppresses uncommitted starts, cancels other reservations, drains committed work, then atomically installs the successor and revokes old epoch/bridge before reconciliation (`ARCHITECTURE-SPINE.md:197-201,479`; `launch-readiness-register.md:240-244`; `epics.md:1900-1904`). |

## Authoritative Validation Recheck

The authoritative report's C-1 through C-3 and H-1 through H-12 were rechecked. Their original defects remain closed as stated: safety evaluation is conjunctive across snapshot and every then-current applicable version; `ProtectionFence` serializes ordinary hold/export/deletion decisions; matrix-v4 has target-aware bootstrap/repair; scheduled human Approver re-resolution and durable bounded rescan ownership are explicit; human identity is stable and tagged by principal kind; rolling rate, open-interaction, and monetary Budget lifetimes have separate owners; proposal indexing has owner outbox/high-water/reconciliation; optional retraction is split from the six core Conversations seams; trusted envelopes have canonical authentication, replay, rotation/revocation, and non-recursive durable security recording; Dapr Client/ASP.NET current exposure is separated from Workflow delivery; target public vocabulary is not called shipped; and story/dependency tracking debt is not treated as architecture evidence. C-v14-1 and H-v14-1 are later race/recovery defects and do not reinstate those findings verbatim.

## Areas That Converge

- The FR-8 order remains directory registration, rate/open step 5, Confirmation-only Approver step 6, committed Context step 7, Budget step 8, safety step 9, committed membership step 10, then acceptance. No pre-step-10 failure can cause a membership effect.
- Same-owner `Reserved -> CommittedToEffect -> Settled` versus `Reserved -> CancelledBeforeCommit` closes ordinary acquire/Closing races; committed Context, membership, Provider, generated-output, and posting steps have typed result/lookup settlement.
- Generated failure discards unapproved bytes and cannot mint a proposal/failure-content stream; generated success requires its own committed mutation lease.
- Operator and Conversation deletion origins remain a closed discriminated union with distinct authorities; candidate construction is reversible and fence acceptance alone owns the immutable set.
- Initial directory migration and invalidation repair have target-limited Workflow principals, finite manifests, EventStore boundary receipts, old-epoch bridge confinement, successor revocation, restore/lost-ack lookup, and plaintext fail-closed behavior.
- Decision bootstrap/catalog activation, replay/security recording, safety epochs, posting, rate/open/budget ownership, export prepare/commit/key release/cleanup, hold coupling, and phase-pinned recorded recovery remain internally convergent under their stated preconditions.
- AD-1 through AD-31 and every existing OD identifier remain present. Open Product/Governance choices remain explicit and fail closed; this review does not select `OD-RATE-CONCURRENCY-CONSUMPTION-1`, `OD-HOLD-DELETION-PRECEDENCE-1`, `OD-HOLD-PREPARE-CANCELLATION-1`, `OD-EXPORT-LIFECYCLE-1`, `OD-RELEASE-RECORDER-SCOPE-1`, `OD-LEGACY-PLAINTEXT-DISPOSITION-1`, or any deferred PRD outcome.

## Architecture Defects Versus Implementation And Tracking Debt

C-v14-1, H-v14-1, and M-v14-1 are target architecture/handoff defects. Their corrections define ordering, durable handoff, recovery authority, and evidence already required by approved deletion, authorization, and recovery requirements; none chooses an unresolved Product outcome.

The repository remains materially behind the target and the spine correctly calls that delivery debt. There is no shipped directory migration/write fence/bridge, interaction permit/create/start outbox, effect-lease/barrier protocol, three-ledger protocol, decision catalog/recorder, safety epoch, shared protection fence, Conversations deletion propagation, durable export-key protocol, or target public vocabulary. Current generation failure remains an `AgentInteraction` event with no separate proposal version/content stream (`src/Hexalith.Agents.Contracts/AgentInteraction/Events/AgentOutputGenerationFailed.cs:3-25`; `src/Hexalith.Agents/AgentInteraction/AgentOutputGenerationPolicy.cs:17-20,25-38,52-73`). Current direct approval/posting remains implementation debt, not permission to weaken AD-5.

The parent-authoritative Builds gitlink's catalog pins Dapr Client, ASP.NET, and Workflow `1.18.5`; the checked-out but non-authoritative Builds `cf52f74` catalog pins `1.18.7`. Agents consumes EventStore Client/DomainService and therefore already has transitive Dapr Client/ASP.NET exposure, while no Agents project references Dapr Workflow. The spine reports that split accurately. All named external seams remain `Uncommitted`/TBD where the registers say so, and no build, integration, release, or dependency-acceptance success is claimed.

## Gate Result

The v14 adversarial-divergence gate is **FAIL** because Critical and High counts are nonzero. Deterministic lint: **PASS**, `ok: true`, zero findings.
