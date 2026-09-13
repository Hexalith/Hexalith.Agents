---
name: Hexalith Agents adversarial-divergence review v16
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

# Adversarial Divergence Review — v16

## Verdict

**FAIL — 1 Critical, 1 High, 1 Medium, and 0 Low findings.** The v16 candidate closes the two v15 Critical findings and the directory-repair High as stated. Its new Provider decision pin also preserves result-time meaning, but the pin is placed on a lease whose mandatory pre-commit boundary forbids obtaining or validating it. A fresh whole-protocol pass additionally found that the two deletion fences do not give an accepted admission-fence violation a safe pre-destruction disposition. The deterministic architecture linter passed with `ok: true` and zero findings; mechanical lint does not close the semantic findings.

## Frozen Snapshot And Method

The following inputs were SHA-256 checked before review, immediately before report creation, and after this report was written:

- `ARCHITECTURE-SPINE.md`: `33abd2ca65575b3c73b95827a0b2994cf7f1e909bcdd48a9a93f22403b4476f2`
- `IMPLEMENTATION-CONVENTIONS.md`: `f30858efcfdb0439bcd3c2a92073061b8cecec35469c85f5503dbf32aeea160a`
- architecture `.memlog.md`: `f47127fbac2f66bea528939c3fc55433487b9a0f539ab602956833324fe9240d`
- bound `prd.md`: `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb`
- `epics.md`: `e137a160dc9344d4dd2a5e5f1e8bd176ec81f5b721ee7fdcf0ab55c693040c8d`
- `external-dependency-register.md`: `0a780c4c9cf77e8e9d1cf26f7003997e5e7f8a8fa7da61698299470ff7c8eb46`
- `launch-readiness-register.md`: `7f5316db6d320ebd3f4b1e3eace38e7892c83533bb7b727fd3f4f2cbdf9f1cae`
- authoritative `VALIDATION-REPORT-2026-09-12.md`: `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`

The review constructed independent public-command, directory, Interaction Workflow, Provider/safety, migration/repair, EventStore-guard, protection-fence, operator-deletion, Conversation-deletion, ledger, and crash-recovery units. Each was made to obey the literal AD, convention, PRD, epic, dependency-register, and operation-matrix clauses. The units were then raced around authorization/effect/result commits, owner revisions, deletion-fence installation and violation, candidate acceptance, destruction start, Provider decision activation, dependency outage, crash/lost acknowledgement, restore, epoch repair, and cross-tenant inputs. Every authoritative Critical/High and every v15 finding was rechecked before seeking new divergence.

Repository inspection was read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`; no submodule was initialized, updated, or mutated. Root gitlinks remain Builds `a32cb422749352cce8dec948aa3e78c8f00eb4cf`, Conversations `73bcee6f04479d4743d5a65ce929728e22687d7d`, EventStore `ce9e779a3ec24d9b9a054afd0ceaa3c05b8adaaa`, FrontComposer `053b2008307d4e476c0d4329e6c47763c301d43e`, Memories `3644ef63da87b8c7ec9e17726fae7f534e642a1c`, Parties `fa42398552fba1c80eb2760791517659d6d1313a`, and Tenants `2fac18396ff11a4459de053b3ebb7ddfe7c13e30`. The first five checked-out revisions still differ from their root gitlinks; Parties and Tenants match. Those dirty checkouts are implementation evidence only, never planning authority.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 1 |
| Medium | 1 |
| Low | 0 |

## Critical

### C-v16-1 — An accepted admission-fence violation can authorize an unmanifested external effect while deletion still passes its destruction checks

**Classification:** target-architecture irreversible-deletion, effect-cut integrity, and recovery defect.

**Evidence.** The common two-origin cut relies on the admission fence to make each owner manifest finite: a pre-fence permit/intent/lease acquire or commit is included, while a post-fence loser appends nothing (`ARCHITECTURE-SPINE.md:205-211,258-268`; `launch-readiness-register.md:281-297`). The external contract nevertheless correctly requires both the admission and content fences to expose complete violation-ledger receipts and explicitly anticipates an accepted violation (`external-dependency-register.md:129,132`). The downstream architecture defines only `ContainDeletionScopeWriteFenceViolation`; its before-start arm refreezes and its after-start arm appends an in-scope resource to the destruction manifest (`ARCHITECTURE-SPINE.md:266,531`; `launch-readiness-register.md:306`). Immediately before `DestructionStarted`, however, the guard is only “both fences installed” plus **content/write-fence** violation-free, not admission-fence violation-free (`ARCHITECTURE-SPINE.md:266`; `epics.md:3022`; `launch-readiness-register.md:317`). Completion asks for a complete generic violation-ledger checkpoint and blocks an uncontained violation, but there is no admission-violation containment command, no rule invalidating the already-recorded global cut, and no phase that can add a late permit or committed lease to every affected owner manifest (`launch-readiness-register.md:318-319`).

**Two literal units.** Team A interprets any admission-fence violation as invalidating the global fixed point and blocks destruction forever until an explicit recut. Team B follows the named matrix: it verifies that both fences remain installed and that the content fence is violation-free, then starts destruction because no named admission-violation disposition is a start predicate. A faulty, stale, or restored EventStore guard may have accepted `CommitConversationEffect` after the affected owner became Effective. That lease is outside the frozen owner manifest but, by the lease contract, authorizes its exact dependency read or external effect and cannot be cancelled. A Context/roster read or membership effect can therefore begin without a later Agents content append; a content fence cannot undo it or add it to the already-accepted deletion inventory. If the violation is noticed only by the completion checkpoint, the architecture can block completion but cannot reconcile the already-started external effect or restore the destroyed DEK.

**Impact.** The deletion path can cross its irreversible boundary while its proof that no matching external effect escaped is false. Cryptographic erasure cannot recall Provider, Parties, membership, or Conversation effects. This defeats the common-cut invariant for both deletion origins and is Critical even though the expected production guard should reject the write: the architecture explicitly requires recovery and containment for accepted guard violations.

**Required correction — architecture mechanics, not a Product choice.** Give admission-fence violations a closed protocol distinct from content-write containment. Verification immediately before candidate acceptance, write-fence authorization, prepare, and `DestructionStarted` must bind a complete admission-fence violation checkpoint. Any accepted matching admission write before destruction invalidates the global effect-cut receipt and requires a new finite owner Closing/Effective cohort that includes the violating permit/intent/lease before refreeze/reacceptance. Any such violation discovered after `DestructionStarted` must block completion and further destructive authority; it cannot be treated as a resource-only append because a committed external effect may already have begun. Add exact matrix variants, principal grants, security observation, lost-ack lookup, and both-origin fixtures. Completion must bind the phase-pinned admission and content violation checkpoints independently.

## High

### H-v16-1 — The Provider output-status decision pin is required on `CommittedToEffect`, but obtaining or validating that pin before commit is forbidden

**Classification:** target-architecture authorization/effect sequencing and committed-lease recovery defect.

**Evidence.** Every effect lease must commit before the first safety, Conversation, roster, or other dependency read; before commit only closed content-free owner-local identity, shape, principal, revision, migration, and barrier checks are legal (`ARCHITECTURE-SPINE.md:205,521`; `IMPLEMENTATION-CONVENTIONS.md:9,19`; `launch-readiness-register.md:252-254`). `ArchitectureDecisionCatalog` and the per-decision `ArchitectureDecisionRecord` are different reserved-system aggregates, not `ConversationAgentState` owner-local facts (`ARCHITECTURE-SPINE.md:195`). The v16 Provider protocol now requires the committed Provider lease itself to carry the exact effective `OD-INITIAL-OUTPUT-SAFETY-STATUS-1` record revision, digest, selected mapping, and catalog activation, and says a successor governs only an authorization not yet committed (`ARCHITECTURE-SPINE.md:354,430`). Yet `ConversationEffectLease:CommitToEffect` has neither those fields nor authority to read that dependency; the decision is first required by the later `ProviderInvocation:AuthorizeUnderEffectLease` row (`launch-readiness-register.md:254,259`).

**Two literal units.** Team A reads the catalog/decision immediately before lease commit so that the lease really carries the current pin, violating the explicit no-dependency-read-before-commit boundary and allowing Closing to lose after an unauthorized dependency read. Team B commits a generic Provider lease first and then reads the decision as the later authorization row directs. If authority is Open, missing, malformed, unavailable, or changes across the commit/read window, B has a non-revocable committed lease but no named typed no-invocation result with which to settle it; deletion Closing can never reach Effective. A third team carries an older acquisition-time value into commit without a current authoritative read, while another uses the later effective value, contradicting the clause about which successor governs.

**Required correction — no Product outcome selected.** Keep `CommitToEffect` generic and owner-local. Only after it commits may the Workflow read the exact current catalog/decision owners and append one mutually exclusive interaction decision: `ProviderInvocationAuthorized` with the immutable mapping pin, or a typed `ProviderInvocationNotAuthorized`/equivalent no-invocation terminal result carrying the exact unavailable/Open/malformed/changed authority outcome. The negative decision must authorize Budget/capacity release, target-result acknowledgement, and Provider-lease settlement, and may never authorize `BeginInvocation`. Define which durable interaction decision wins a concurrent decision successor; result and recovery then use only that recorded branch. Alternatively introduce a separate durable pre-lease decision-authorization phase and explicitly exempt/serialize it against Closing, but do not silently perform a prohibited dependency read inside lease commit.

## Medium

### M-v16-1 — Deletion convergence does not define the legal terminal outcome for an operator deletion of an awaiting or `Approved` proposal

**Classification:** target-architecture public-state/audit completeness gap with an unresolved Product-visible outcome.

**Evidence.** Both origins must terminalize a nonterminal interaction before purge, and `ConvergeInteraction` may “terminalize awaiting/Approved” under `CurrentStateSpecificOutcomeExact`, but no table maps source state and origin to an event, public proposal state, typed reason, metrics, or open-lease result (`ARCHITECTURE-SPINE.md:444,529`; `launch-readiness-register.md:287`). The PRD's recorded proposal states and system-abandon reasons are closed: system abandonment carries only `NoEligibleApprover`, `SourceConversationUnavailable`, `RemovedInConversations`, or `PostingWindowElapsed`; deletion is not a reason, `Approved` cannot expire, and a human abandonment requires the applicable human authority (`prd.md:433-470`). Conversation-approved deletion can eventually use the already-defined authoritative `SourceConversationUnavailable` semantics after posting lookup where required, but an exact-interaction operator deletion need not mean that its Conversation is deleted, inaccessible, removed, or past the posting window.

**Divergence.** One implementation waits for natural expiry/posting-window exit before Effective. Another invents `Abandoned(DeletionRequested)`, while another records `Rejected` and falsely implies an Approver decision. The latter two violate the PRD vocabulary, but the matrix's circular `CurrentStateSpecificOutcomeExact` gives a downstream team no implementable closed result and no explicit instruction to remain pending. The defect is Medium rather than High because waiting remains safe and the architecture states no deletion-completion latency guarantee.

**Required correction — surface, do not invent, the Product choice.** For Conversation-origin deletion, explicitly bind existing Conversation-deleted/message-lookup transitions. For operator-origin deletion, either restrict acceptance/Closing to already terminal interactions and specify that the request remains restrictive pending until ordinary legal terminality, or add an Open Decision owned by Product + Governance for a deletion-specific terminal outcome, reason, audit and SM-3/SM-C4 treatment, with that branch blocked until approved. Then replace `CurrentStateSpecificOutcomeExact` with a closed origin/source-state table and exact ledger settlement evidence.

## v15 Finding Recheck

| v15 finding | v16 disposition |
| --- | --- |
| C-v15-1 operator deletion lacked an effect cut | **Closed as stated.** Both origins now install the same admission fence, owner request-scoped Closing-to-Effective cuts, manifest-bound convergence grants, and global fixed-point receipt before candidate construction (`ARCHITECTURE-SPINE.md:205-211,258-268,524-529`; `launch-readiness-register.md:281-297`). C-v16-1 is the distinct missing disposition when the EventStore reports that this new admission guard itself accepted a forbidden append. |
| C-v15-2 deletion fence lacked a canonical predicate | **Closed.** `GovernanceScopeV1` now defines exact-interaction and exact-Conversation wire membership from authoritative tenant/stream/directory facts, binds one digest throughout, fails unknown/cross-tenant/unverifiable membership closed, and explicitly blocks the unresolved class/range variant behind `OD-GOVERNANCE-CLASS-RANGE-SCOPE-1` (`ARCHITECTURE-SPINE.md:201-203`; `IMPLEMENTATION-CONVENTIONS.md:21`; `external-dependency-register.md:129-132`). |
| H-v15-1 repair froze without a directory-write boundary | **Closed.** `InstallDirectoryRepairFence` atomically revokes permit/outbox/lease acquire/commit capability and returns the checkpoint before cohort freeze; bridge and successor activation compare the resulting high-waters and preserve active deletion fences (`ARCHITECTURE-SPINE.md:215-217`; `launch-readiness-register.md:243-250`). |
| H-v15-2 output-status mapping was not phase-pinned | **Partially closed but still High as H-v16-1.** Authorization, result, and recovery now name the immutable decision revision/digest/mapping/catalog activation. The correction simultaneously says the earlier lease commit carries that non-owner decision while the universal precommit rule forbids obtaining it; the phase boundary therefore remains non-implementable without the typed branch described above. |

## Authoritative Validation Recheck

The authoritative report's C-1 through C-3 and H-1 through H-12 were rechecked. Their original defects remain closed as stated: safety is conjunctive over snapshot and current policy; protection operations share a tenant fence; matrix v4 bootstrap/repair is target-aware and excludes only its exact circular precondition; scheduled human Approver resolution has a committed effect lease; safety rescans use a finite authoritative directory and fenced coordinator; human actors are principal-kind-tagged and historically comparable; rate, open-interaction, and Budget lifetimes have distinct durable owners; proposal/source action indexing is crash-consistent; Conversations core and optional retraction seams are split; trusted envelopes register replay before dispatch and record denials without ACL recursion; Dapr exposure is current dependency debt; and the target architecture is not described as shipped. C-v16-1 and H-v16-1 are new independent-implementation contradictions introduced or exposed by the v16 deletion and Provider refinements, not verbatim reopenings of the authoritative findings.

## Areas That Converge

- The canonical exact-interaction/exact-Conversation membership contract, both deletion origins, request-scoped owner cuts, rate/open/Budget/capacity/posting settlement, and operator-cancellation blocking now agree across spine, conventions, epics, dependency register, and operation matrix under a guard that rejects every forbidden write.
- Directory migration and repair serialize new directory writes at the EventStore boundary, freeze only the resulting finite cohort, bridge already-authorized work, preserve deletion fences, and revoke old authority atomically at successor activation.
- Provider error/timeout is content-free; unsafe/incomplete output is discarded; allowed output requires a separate committed mutation lease. Once a Provider authorization with a valid decision pin exists, later result/recovery uses that immutable mapping consistently.
- Hold/export/deletion fencing, export commit and key-delivery recovery, decision governance/bootstrap, safety epochs, three ledger lifetimes, replay/security recording, and cross-tenant absent-key behavior remain internally convergent under their stated preconditions.
- AD-1 through AD-31 and existing Open Decision ids remain present. This review selects no outcome for `OD-INITIAL-OUTPUT-SAFETY-STATUS-1`, `OD-RATE-CONCURRENCY-CONSUMPTION-1`, `OD-HOLD-DELETION-PRECEDENCE-1`, `OD-HOLD-PREPARE-CANCELLATION-1`, `OD-OPERATOR-DELETION-CANCELLATION-1`, `OD-EXPORT-LIFECYCLE-1`, `OD-GOVERNANCE-CLASS-RANGE-SCOPE-1`, `OD-RELEASE-RECORDER-SCOPE-1`, `OD-LEGACY-PLAINTEXT-DISPOSITION-1`, or a deferred PRD outcome.

## Architecture Defects Versus Implementation And Tracking Debt

C-v16-1 and H-v16-1 are target-architecture handoff defects: they concern a mandatory guard-integrity recovery branch and a contradictory placement of durable decision authority. M-v16-1 is a target handoff gap whose Product-visible operator-deletion outcome must be either explicitly blocked or surfaced for Product/Governance discussion; the review does not choose it.

The current repository remains materially behind the target, and the spine correctly labels that state as delivery debt. There is no shipped canonical scope/directory, two-fence deletion cut, owner Closing fixed point, directory repair bridge, decision catalog, Provider decision pin, three-ledger protocol, safety epoch, export store, or Conversations deletion-feed/acknowledgement implementation. Current direct approval/posting and the existing `SafetyFailed` output mapping are implementation reality, not authority to weaken AD-5 or decide `OD-INITIAL-OUTPUT-SAFETY-STATUS-1`. `EXT-HOST-1`, `EXT-CONV-AI-1`, `EXT-PARTIES-1`, protection, secrets, Provider, and export seams remain `Uncommitted`/TBD as recorded.

The parent-authoritative root gitlinks remain the review baseline. The checked-out Builds, Conversations, EventStore, FrontComposer, and Memories revisions differ from those gitlinks; Parties and Tenants match. No dirty submodule checkout, local adapter, or uncommitted planning artifact is accepted as live commitment evidence. These delivery and tracking facts neither cause nor excuse the target-architecture findings above.

## Gate Result

The v16 adversarial-divergence gate is **FAIL** because Critical and High counts are nonzero. Deterministic lint: **PASS**, `ok: true`, zero findings.
