---
name: Hexalith Agents adversarial-divergence review v15
type: architecture-review
lens: adversarial-divergence
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
verdict: fail
critical: 2
high: 2
medium: 0
low: 0
lint_ok: true
---

# Adversarial Divergence Review — v15

## Verdict

**FAIL — 2 Critical, 2 High, 0 Medium, and 0 Low findings.** The v15 candidate closes the v14 migration-verification, User-action recovery, and Closing-ledger findings as stated. A fresh whole-protocol construction nevertheless found that the operator-origin deletion path still has no effect-cut/fixed-point equivalent, and that the newly installed deletion-scope fence has no canonical predicate for Conversation or class/range scopes. Either defect can let destructive completion race an independently compliant writer or external effect. Two additional recovery ambiguities can strand repair or a committed Provider lease.

The deterministic architecture linter passed with `ok: true` and zero findings. Mechanical lint does not close the semantic findings.

## Frozen Snapshot And Method

The following inputs were SHA-256 checked before review, immediately before report creation, and after this report was written:

- `ARCHITECTURE-SPINE.md`: `2b64657e34ac93becc0bc7dbf5e2829c290ad581ee578e606053dabd1a68c5ad`
- `IMPLEMENTATION-CONVENTIONS.md`: `2a3c681658ab045de0302a9f20a527717172983619fd6e93e261bc7a07cd4a06`
- architecture `.memlog.md`: `1b65ce14c02a64b2abf5da7e9e225b10a2efc45b35f72da6112ce865158721d7`
- bound `prd.md`: `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb`
- `epics.md`: `19db22ebf78bf925b6b0906d6e31a086973459b5962c611fdd58972d3070f5b8`
- `external-dependency-register.md`: `0b85c8bdf893d9a0c38ea93b7f4beb8bf8871d8323f849d4a7fd03082488997d`
- `launch-readiness-register.md`: `d251642ea2ef765267a7a865a1109f19ded7bf8779ce09568f4b4fd069948090`
- authoritative `VALIDATION-REPORT-2026-09-12.md`: `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`

The review constructed independent public-command, directory, Interaction Workflow, Provider/safety, migration/repair, EventStore guard, protection-fence, operator deletion, Conversation deletion, and recovery units. Each unit obeyed the literal spine, conventions, PRD, epic, dependency, and matrix clauses; combinations were then raced at owner revisions, effect commits, topology quiescence, namespace-fence installation, destruction start, decision-version changes, crash/lost acknowledgement, restore, and cross-tenant boundaries. Every authoritative Critical/High and v14 finding was rechecked before seeking new divergence.

Repository inspection was read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`; no submodule was initialized, updated, or mutated. Root gitlinks remain Builds `a32cb422749352cce8dec948aa3e78c8f00eb4cf`, Conversations `73bcee6f04479d4743d5a65ce929728e22687d7d`, EventStore `ce9e779a3ec24d9b9a054afd0ceaa3c05b8adaaa`, FrontComposer `053b2008307d4e476c0d4329e6c47763c301d43e`, Memories `3644ef63da87b8c7ec9e17726fae7f534e642a1c`, Parties `fa42398552fba1c80eb2760791517659d6d1313a`, and Tenants `2fac18396ff11a4459de053b3ebb7ddfe7c13e30`. The first five checked-out submodules differ from those root gitlinks; Parties and Tenants match. Those dirty checkouts are implementation evidence only, never planning authority.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 2 |
| High | 2 |
| Medium | 0 |
| Low | 0 |

## Critical

### C-v15-1 — Operator-origin deletion can start destruction while a committed interaction effect is still able to read or export the target content

**Classification:** target-architecture irreversible-deletion and cross-owner effect-cut defect.

**Evidence.** The Conversation-origin path installs `ConversationDeletionBarrier(Closing)`, atomically rejects/cancels losing reservations, manifests every winning `CommittedToEffect` lease, and waits for all target, external, ledger, capacity, posting, outbox, and lease acknowledgements before `Effective` and inventory freeze (`ARCHITECTURE-SPINE.md:245-252`; `launch-readiness-register.md:275-286`). The same lease contract explicitly says a committed Provider, Conversation, safety, or target effect is non-revocable (`ARCHITECTURE-SPINE.md:198,245`). The operator-origin `GovernanceProtection` capability, however, starts at candidate construction and offers candidate/refreeze/accept/fence/prepare/destruction/purge/completion variants only; it has no same-owner effect cut, no per-interaction or per-Conversation Closing, and no manifest-bound convergence grant (`ARCHITECTURE-SPINE.md:502`; `launch-readiness-register.md:287-303`). `CandidateEqualsCompleteCurrentResourceSet` inventories workflow state but does not require that every effect reservation lose or every committed effect reach its fixed point before acceptance or destruction (`launch-readiness-register.md:287,289,296,301`). The EventStore scope fence prevents later interaction/failure content writes; it does not prevent an already committed Provider invocation, safety/Conversation read, Conversation post, or other external effect (`ARCHITECTURE-SPINE.md:248,426`).

**Two literal units.** Team A treats active workflow/lease state as a reason to wait until all effects settle before freezing an operator deletion. Team B includes the active workflow and lease in the complete inventory, accepts it as a resource, installs the write fence, and starts destruction because every explicit operator matrix predicate passes. A `ProviderInvocation` lease that committed before fence acceptance may then materialize the prompt/context or call the Provider after acceptance; a committed posting lease may append its already-authorized Conversation message. Its later Agents content append can be rejected by the scope fence while the external effect has already happened. Team B cannot use the Conversation-deletion settlement grants because those are signal/Closing-manifest-bound and the operator Workflow is not authorized to impersonate the Interaction Workflow.

**Impact.** Approved deletion can claim an irreversible all-copy outcome while new Provider-side or Conversations-side derived content/effects are created after its inventory authority. DEK destruction does not recall a Provider request or a Conversation append. This is a Critical deletion correctness and data-governance failure.

**Required correction — architecture mechanics, not a Product choice.** Give every operator scope a linearizable effect cut before candidate acceptance: define how the operator Workflow installs interaction- or Conversation-directory Closing barriers for every affected interaction, serializes them against permit/lease acquire/commit, and waits for the same closed outbox/lease/rate/open/Budget/capacity/posting fixed point. Bind the candidate and fence acceptance to the exact Effective receipts. For class/range scopes, freeze a finite directory cohort and acquire all member cuts before acceptance, with a namespace/scope guard closing later membership; partial acquisition remains reversible and cannot authorize destruction. The recovery principal may settle only existing manifested work and must not invent effects or human authority.

### C-v15-2 — The deletion-scope write fence has no canonical scope-membership predicate, so a same-Conversation or class/range late write can evade the violation ledger

**Classification:** target-architecture authorization-scope, cross-tenant guard, and irreversible-containment defect.

**Evidence.** `LegalHold` is the only aggregate that gives a literal grammar: an interaction-id list or `class:<ContentClass>` plus a UTC range, but neither the closed `ContentClass` vocabulary nor the authoritative instant used by that range is defined (`ARCHITECTURE-SPINE.md:192`; `epics.md:2830-2833`). A Conversation-approved deletion instead authorizes derived content of one exact Conversation (`ARCHITECTURE-SPINE.md:242,254`; PRD `prd.md:879-883,962-963`). The new guard is merely bound to an “approved immutable scope grammar” and must decide whether an `AgentInteraction`/`GenerationFailureRecord` create or content append is “inside” or “matching” that scope (`ARCHITECTURE-SPINE.md:248-250,424-428`; `external-dependency-register.md:123-132`; `launch-readiness-register.md:291-303`). No clause defines a `conversation:<ConversationId>` encoding, the authoritative field from which EventStore derives Conversation membership on create and append, the class taxonomy/classification owner, UTC boundary inclusivity, or whether the time is interaction creation, source event, attempt, proposal, terminal, or append time. Nor does it say that missing/ambiguous classification rejects rather than trusting caller-supplied metadata.

**Two literal units.** Team A expands the accepted current resource set into exact stream ids and rejects later writes only to those ids. Team B evaluates future writes by authenticated SourceConversationId. For a class/range request, Team C uses interaction creation time while Team D uses event occurrence/append time and maps generated proposals to different `ContentClass` values. All can claim to implement the literal “approved scope grammar.” After `DestructionStarted`, a stale/restored writer can create a new stream for the deleted Conversation, or append a record whose competing class/time mapping is outside one guard's predicate. The migration owner may become invalid, but phase-pinned deletion recovery intentionally no longer re-enters it and relies on the scope-fence violation ledger (`ARCHITECTURE-SPINE.md:250,428`; `launch-readiness-register.md:301-303`). The unclassified write is absent from that ledger and can survive successful completion.

**Required correction.** Define one versioned canonical deletion/hold scope schema shared by Agents and `EXT-HOST-1`: exact Conversation, exact interaction-id set, and any Product-authorized class/range form; a closed class vocabulary; inclusive/exclusive UTC semantics and authoritative timestamp; and an EventStore-computable membership proof for create and append derived from authenticated owner/permit facts, never caller assertions. Bind the schema/version and predicate digest to request approval, accepted set, fence authorization/receipt, every violation, and completion. Missing, malformed, unknown-class, cross-tenant, or unverifiable membership must reject the write and block verification. If the permitted class taxonomy or time meaning is a Product/Governance choice, surface a scoped Open Decision and block that scope variant; do not invent it. Exact Conversation and interaction scopes still need their architecture-defined wire predicates.

## High

### H-v15-1 — Migration repair freezes directory high-waters without an authoritative barrier against a previously authorized late directory append

**Classification:** target-architecture repair convergence and cross-owner TOCTOU defect.

**Evidence.** Migration invalidation is owned by `InteractionDirectoryMigration`, while permits, creation/workflow/User-action outboxes, and effect leases are appended on independent `ConversationAgentState` owners (`ARCHITECTURE-SPINE.md:192,198-204`). Begin repair quiesces a topology cohort and freezes directory count/hash/high-waters; the old-epoch bridge accepts only exact frozen pending outboxes and committed leases, and successor activation revokes the old bridge (`ARCHITECTURE-SPINE.md:204`; `launch-readiness-register.md:241-245`). New directory acquire/commit rows read migration state and a directory expected revision, but no EventStore boundary capability conditions a `ConversationAgentState` permit, action-intent reservation, lease acquisition, or commit on the still-current migration-owner revision (`launch-readiness-register.md:246-249`). The namespace epoch guard protects only `AgentInteraction`/`GenerationFailureRecord` create or content-bearing appends (`ARCHITECTURE-SPINE.md:202`; `external-dependency-register.md:129-132`).

**Two literal units.** Team A treats topology quiescence as sufficient and freezes the cohort. Team B has a disconnected request that already read `DirectoryReady` and the relevant directory expected revision; after `BeginRepairEpochTransition` freezes high-waters on the different migration owner, it appends a new permit or protected User-action-intent/lease to an unchanged `ConversationAgentState`. Its later target write is rejected by the namespace fence, but the directory member is outside the bridge manifest. One repair implementation detects it and remains invalidated forever; another completes from the frozen repair cohort and target-namespace scans, leaving the new directory outbox/lease outside its readiness proof. The current clauses require both finite repair and preservation of already-authorized work but do not select one safe result.

**Required correction.** Install a durable directory-repair Closing/fence at every affected `ConversationAgentState`, or make the EventStore conditional append for every permit/outbox/lease acquire/commit consume a current migration epoch capability whose revocation is atomic with repair freeze. The frozen repair manifest must be produced only after that boundary is installed and must include every pre-boundary winner. A post-boundary loser must append nothing; lost acknowledgement must resolve against the exact directory/repair owner. Successor activation must compare the directory high-waters or fence receipts, not topology quiescence alone.

### H-v15-2 — Output-safety status authority is checked at the result row but is not phase-pinned before Provider invocation

**Classification:** target-architecture decision-governance and committed-effect recovery defect.

**Evidence.** While `OD-INITIAL-OUTPUT-SAFETY-STATUS-1` is Open, live Provider work correctly blocks before invocation, and the negative result row later requires an approved matching decision version (`ARCHITECTURE-SPINE.md:340,408,1193`; `launch-readiness-register.md:105,256`). `ProviderInvocationAuthorized`, however, binds the descriptor, reservation, capacity fence, effect-lease commit, and evaluated readiness/matrix versions, not the exact approved output-status decision record/version or its mapping (`ARCHITECTURE-SPINE.md:338`; `launch-readiness-register.md:254`). The decision catalog preserves effective/pending unions, but the explicit phase-pin exemptions name governance prepare/disposition, hold release, export commit, and destruction—not an in-flight Provider invocation (`launch-readiness-register.md:108,360`). The output-safety result row is gate-free and says only `ApprovedInitialOutputSafetyStatusDecisionVersionMatches`, without stating whether that means the authorization-time version or the current union (`launch-readiness-register.md:256`).

**Two literal units.** Team A snapshots the approved status contract at `ProviderInvocationAuthorized` and uses it if output safety later denies. Team B re-evaluates the current catalog/record union at result time. If a successor is pending or Open after invocation, B cannot append either result, cannot cancel the committed Provider lease, and therefore can never settle it or let Closing reach Effective. If a successor is already approved with a different mapping, a third implementation uses the new mapping for an invocation admitted under the old one, producing different public status, metric, and open-lease handling for the same durable authorization.

**Required correction.** Require live Provider authorization to bind the exact effective `OD-INITIAL-OUTPUT-SAFETY-STATUS-1` record revision, contract digest, selected status/reason/metric/ledger mapping, and catalog activation revision. The committed Provider lease and `ProviderInvocationAuthorized` must carry those values. Negative-result recording and crash recovery must use only that phase-pinned mapping; a later decision successor governs only a Provider authorization not yet committed. This selects no Product outcome.

## v14 Finding Recheck

| v14 finding | v15 disposition |
| --- | --- |
| C-v14-1 migration invalidation after accepted inventory | **Closed as stated.** Both origins now accept against the exact Ready epoch/guard and namespace high-water, install a persistent deletion-scope fence before preparation, directly re-verify the migration owner and fence immediately before `DestructionStarted`, and route post-start accepted violations through an append-only containment manifest (`ARCHITECTURE-SPINE.md:246-250,424-428`; `launch-readiness-register.md:289-303`). C-v15-1 is the distinct missing operator effect cut; C-v15-2 attacks the scope predicate on which that corrected verification relies. |
| H-v14-1 committed User mutation has no durable recovery handoff | **Closed.** `ProposalMutation:ReserveUserActionIntent` atomically stores the immutable protected intent/outbox with `Reserved`; commit binds it, the exact Interaction Workflow has a target-limited non-impersonating recovery variant, and acknowledgement precedes settlement (`ARCHITECTURE-SPINE.md:198-200,501`; `IMPLEMENTATION-CONVENTIONS.md:19`; `launch-readiness-register.md:248-259`). H-v15-1 concerns an independently timed repair freeze, not ordinary action recovery. |
| M-v14-1 Closing omits rate/open/pre-Provider Budget | **Closed.** Closing completeness and `ActivateBarrierEffective` now enumerate rate/open and every Budget/capacity/posting decision and owner acknowledgement, with manifest-bound recovery grants that cannot reserve or invent outcomes (`ARCHITECTURE-SPINE.md:252,505`; `launch-readiness-register.md:279-286`). |

## Authoritative Validation Recheck

The authoritative report's C-1 through C-3 and H-1 through H-12 were rechecked. Their original defects remain closed as stated: safety is conjunctive over snapshot and current policy; hold/export/deletion share the fence; matrix-v4 bootstrap/repair is target-aware and excludes only its exact circular precondition; scheduled human Approver resolution now has its own committed effect lease; the safety rescan has finite authoritative enumeration and fenced workers; human actors are principal-kind-tagged and historically comparable; rolling rate, open-interaction, and Budget lifetimes have distinct owners; proposal indexing and User-action ingress have durable owner outboxes and recovery; Conversations retraction is split from the six core seams; trusted envelopes have pre-dispatch replay registration and non-recursive durable denial recording; Dapr package exposure is reported as current dependency debt; and the target architecture is not called shipped. The four v15 findings are later independent-implementation races or missing wire predicates, not reinstatements of those original findings verbatim.

## Areas That Converge

- FR-8 retains directory registration followed by rate/open step 5, Confirmation-only committed Approver resolution step 6, Context step 7, Budget step 8, safety step 9, committed membership step 10, and only then acceptance.
- Same-owner lease `Reserved -> CommittedToEffect -> Settled` versus `Reserved -> CancelledBeforeCommit` converges for the Conversation-origin barrier, including protected User intents, Provider failure, output persistence, and posting outcomes.
- Provider/timeout failure is content-free under the Provider lease; output-safety negative bytes are discarded; allowed output alone uses a separate generated-version mutation lease. The open status choice remains explicitly blocked rather than invented.
- Conversation-deletion source delivery, exact intake identity, Closing fixed point, accepted inventory, migration recheck, scope-fence effect/result, and in-scope containment are durable and replayable once the missing canonical predicate is supplied.
- Hold/export/deletion fence decisions, export commit/key delivery/cleanup, decision catalog/bootstrap, replay/security recording, rate/open/budget ownership, and safety epoch activation remain internally convergent under their stated preconditions.
- AD-1 through AD-31 and existing OD identifiers remain present. No finding selects `OD-INITIAL-OUTPUT-SAFETY-STATUS-1`, `OD-RATE-CONCURRENCY-CONSUMPTION-1`, `OD-HOLD-DELETION-PRECEDENCE-1`, `OD-HOLD-PREPARE-CANCELLATION-1`, `OD-EXPORT-LIFECYCLE-1`, `OD-RELEASE-RECORDER-SCOPE-1`, `OD-LEGACY-PLAINTEXT-DISPOSITION-1`, or a deferred PRD outcome.

## Architecture Defects Versus Implementation And Tracking Debt

C-v15-1, C-v15-2, H-v15-1, and H-v15-2 are target architecture/handoff defects. They define effect-cut ordering, scope-membership wire semantics, repair linearization, and recorded decision recovery already required by approved authorization/deletion/recovery outcomes. Only the allowed class taxonomy or its Product-visible meaning may need a surfaced Product/Governance decision; the review does not choose it.

The repository remains materially behind the target and the spine correctly calls that delivery debt. No shipped directory permit/migration/write-fence/repair bridge, effect-lease/barrier, three-ledger, decision catalog, safety epoch, protection-fence/deletion-scope guard, Conversations deletion propagation, or target public vocabulary exists. Current generation failure remains an `AgentInteraction` event under `src/Hexalith.Agents.Contracts/AgentInteraction/Events/AgentOutputGenerationFailed.cs:25` and `src/Hexalith.Agents/AgentInteraction/AgentOutputGenerationPolicy.cs:18-38`; it is not evidence for the target Provider/output-safety protocol. Current direct approval/posting remains implementation debt, not authority to weaken AD-5.

The parent-authoritative Builds gitlink still pins the reviewed dependency baseline, while the checked-out Builds, Conversations, EventStore, FrontComposer, and Memories revisions are dirty relative to their root gitlinks. The target directory, payload-protection, Conversations deletion-feed/acknowledgement, scope-guard, and Workflow seams remain `Uncommitted`/TBD in `external-dependency-register.md`; current checkouts cannot close those records. These delivery facts neither cause nor excuse the four target-architecture defects.

## Gate Result

The v15 adversarial-divergence gate is **FAIL** because Critical and High counts are nonzero. Deterministic lint: **PASS**, `ok: true`, zero findings.
