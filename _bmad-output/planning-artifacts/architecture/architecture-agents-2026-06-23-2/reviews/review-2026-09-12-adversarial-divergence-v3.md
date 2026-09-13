# Adversarial-Divergence Review — 2026-09-12 v3

## Verdict

**CHANGES REQUIRED.** The update closes most of the 2026-09-12 v2 divergence findings, but two critical contradictions remain: the new hold/deletion fence chooses outcomes that conflict with the bound PRD's legal-hold precedence, and matrix v4 asks evaluators to remove subconditions that the normative readiness-record schema does not represent. Seven high-severity seams still permit incompatible safety-terminalization, identity, rate-admission, envelope-replay, dependency, and export-gating implementations.

## Scope And Method

This lens reviewed the current `ARCHITECTURE-SPINE.md` and its bound PRD, external-dependency register, launch-readiness register, and implementation convention. For each seam, it assumed two teams implement every literal invariant independently and retained a finding only where both teams can cite current normative text yet produce incompatible externally visible or safety-relevant outcomes.

The pass specifically attacked conjunctive safety and retry, legal-hold/deletion fencing, matrix-v4 bootstrap and scope, scheduled Approver re-check, distributed safety rescan, human-only Approvers and person identity, the three ledger lifetimes, proposal-index crash consistency, the Conversations dependency split, trusted-envelope replay/rotation, and the blocked export-lifecycle policy. The current dirty submodule state was treated as working-tree evidence, not commitment authority.

The deterministic spine lint passes with zero findings; the findings below are semantic.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 2 |
| High | 7 |
| Medium | 0 |
| Low | 0 |

## Critical

### C1 — The new deletion fence contradicts the PRD's legal-hold precedence and deferred-deletion outcome

**Precise references**

- Spine AD-22 rejects a class deletion before preparation when any member is held, rejects deletion against an active or preparing hold, and rejects every later hold once `DeletionArmed` is recorded (`ARCHITECTURE-SPINE.md:318-322`).
- PRD FR-30 requires the runtime surface to carry `DeletionDeferredByHold` (`prd.md:653-663`).
- PRD §9 says legal hold takes precedence over **every** deletion signal, records the signal as `DeletionDeferredByHold`, and executes it on hold release (`prd.md:953-955`, `975`).
- `LR-AUDIT-PROTECTION-DELETION` is meant to qualify the combined retention/hold/deletion contract (`launch-readiness-register.md:108`, `337`).

**Two-unit counterexamples**

1. A deletion request overlaps an already active hold. The spine-literal deletion team rejects the command in full and stores no deferred deletion. The PRD-literal governance team records `DeletionDeferredByHold`, exposes it, and automatically resumes deletion after release. Both fail closed while the hold exists, but after release one tenant deletes and the other does not.
2. A hold arrives after `DeletionArmed` but before the first irreversible destruction. The spine-literal fence team rejects the hold and finishes deletion. The PRD-literal hold team gives the hold precedence over the deletion signal and stops destruction. The same approved commands can therefore preserve or irreversibly erase the same content.

This is no longer the old “missing fence” defect: the fence now exists, but it silently made a Product/Governance choice the bound PRD does not make.

**Action classification: DISCUSS immediately, then reconcile both authorities.** Product and Governance must choose the linearization policy. If `DeletionArmed` is the cutoff after which a later hold cannot preempt, amend PRD §9/FR-30 to say so and define what happens to an already-recorded `DeletionDeferredByHold`. If every hold must win until actual destruction, amend AD-22's arm/recovery protocol accordingly. In either branch, define whether an overlapping deletion request is durably deferred or rejected; do not leave both outcomes normative.

### C2 — Matrix v4 cannot safely omit a readiness subcondition because readiness records have no subcondition state

**Precise references**

- The normative readiness record contains one aggregate `State`, one `BlockerCode`, and one opaque `ConfigurationOrMeasurementContract`; it has no typed condition-result collection (`launch-readiness-register.md:21-36`, `58-68`).
- Each GateId is defined as one indivisible control (`launch-readiness-register.md:91-114`). For example, `LR-PARTY-IDENTITY` combines identity presence and posting eligibility, while `LR-COST` combines pricing, caps, reservations, reconciliation, and retry reuse (`:99-107`).
- Matrix v4 asks evaluators to omit `LR-PARTY-IDENTITY.HexaIdentityPresent`, `LR-COST.NumericBudgetAndLimitsPresent`, or selected `LR-PROVIDER` conditions while preserving all other conditions in the same gate (`launch-readiness-register.md:173-192`).
- AD-17 requires all implementations to consume that matrix and forbids code-local exceptions (`ARCHITECTURE-SPINE.md:286-290`).

**Two-unit counterexample**

An `LR-COST` record is `Block` because both the initial numeric cap is missing and Provider pricing is stale. `TenantBudgetUpdate:InitializeOrRepair` may omit `NumericBudgetAndLimitsPresent`, but the record does not expose which condition produced its single `State` or how the remainder evaluates. One matrix team treats the whole blocked record as pass once the named omission applies, allowing initialization despite stale pricing. Another treats the indivisible `Block` as blocking forever, recreating the bootstrap deadlock. A third re-evaluates raw evidence with its own private condition vocabulary, violating the single-contract rule. The same ambiguity applies to `ProvisionHexa` and tenant Provider enablement.

**Action classification: AUTOFIX before matrix implementation.** Either make each omittable condition a normative typed result in the readiness observation/projection, with closed IDs, status, source version, and a deterministic “evaluate all except exactly these IDs” algorithm, or define a separate command-precondition matrix that does not attempt to subtract fields from aggregate readiness records. Add fixtures containing two simultaneous failures so omission is proven not to erase unrelated blockers.

## High

### H1 — A current-policy safety failure may either terminalize or merely block the same Provider attempt

**Precise references**

- AD-20 correctly requires snapshot-plus-current conjunctive evaluation, but explicitly says a new-current-policy failure “terminalizes or blocks” the existing attempt (`ARCHITECTURE-SPINE.md:306-310`).
- AD-13 says a retry reuses the descriptor/reservation/admission, that a changed readiness value fails closed, and that a trusted step which terminalizes before transport releases reservation and admission (`ARCHITECTURE-SPINE.md:254-260`).
- AD-21 gives monetary reservations, open-interaction leases, and rate consumption different exit rules (`ARCHITECTURE-SPINE.md:312-316`).
- PRD FR-26 requires the conjunctive no-weaker result, while FR-8 declares `SafetyFailed` terminal (`prd.md:680-684`, `299-301`, `696-710`).

**Two-unit counterexample**

On a retry, the snapshot policy passes and the newly active policy fails. Team A records terminal `SafetyFailed`, releases the monetary reservation/admission, and never retries that attempt. Team B records a nonterminal blocked attempt, retains some or all leases, and permits the same prepared attempt if a later policy passes. Both satisfy “terminalizes or blocks” and make no immediate Provider call, but accounting, terminal status, recovery, and later Provider eligibility diverge.

**Action classification: AUTOFIX.** Select one transition for each safety stage. For a pre-transport current-policy failure, bind the terminal/nonterminal state, whether future policy changes may revive the same `AttemptId`, and the exact rate/open/budget/capacity cleanup. Reconcile that transition with the PRD's terminal status vocabulary.

### H2 — Person-level separation of duties still lacks a Party-to-human identity bridge

**Precise references**

- AD-8 records resolved Approvers as `PartyId`s, including Facilitators who may never issue an Agents command (`ARCHITECTURE-SPINE.md:206-216`).
- AD-22's subject set includes callers, editors, Approvers, decision actors, and every Conversation Facilitator, but says separation is compared only by `AuthenticatedHumanActorId`, never principal kind or role (`ARCHITECTURE-SPINE.md:318-322`).
- AD-30 supplies `AuthenticatedHumanActorId` only on a human-originated command principal; caller mapping is ingress-time and the unresolved `ARCH-A-2` assumption covers only the subject-to-caller-Party direction (`ARCHITECTURE-SPINE.md:372-378`, `Architecture Assumptions` `ARCH-A-2`).
- `EXT-PARTIES-1` resolves Party classification and liveness but does not bind a stable authenticated-human subject for a Party (`external-dependency-register.md:105-119`).
- The PRD subject set is Party-based and likewise defines no comparison bridge (`prd.md:624-630`, `1081`).

**Two-unit counterexample**

Human H is the Facilitator Party P in an inspected Conversation but has never acted through Agents, so the subject set contains P and no recorded actor subject. H then authenticates as Platform Operator with `AuthenticatedHumanActorId=H` to approve the export. One implementation treats P and H as incomparable and rejects every such approval; another sees a distinct principal kind/id and accepts, permitting the same human to approve a case in which they are a subject.

**Action classification: DISCUSS identity ownership, then bind it.** Add an authoritative, non-PII stable Party-to-authenticated-human-subject mapping with historical/version semantics, or redefine and persist the subject set wholly in one comparable identity domain. Missing or ambiguous mapping must remain fail closed, but the valid Platform/Administrator approval paths must be provably usable. Extend `EXT-PARTIES-1` or the platform identity dependency and `LR-TENANT-ACCESS` evidence accordingly.

### H3 — Trusted-envelope redispatch contradicts its own nonce-conflict rule

**Precise references**

- AD-30 authenticates issued/expiry instants and signing-key version in the HMAC input; it then rejects the same logical-command nonce with **any** changed authenticated field (`ARCHITECTURE-SPINE.md:372-376`).
- The next sentence permits redispatch after expiry with the same logical nonce while issuing new issued/expiry values and a new tag; rotation may also change the signing-key version (`ARCHITECTURE-SPINE.md:376`).
- `EXT-SECRETS-1` requires both nonce-conflict rejection and exact-replay/rotation behavior without resolving that contradiction (`external-dependency-register.md:179-191`).

**Two-unit counterexample**

A workflow command expires before dispatch and is re-signed under the current key. Team A rejects it because issued time, expiry, tag, and possibly key version changed under the same nonce. Team B exempts those fields and accepts it through idempotency. Both implement literal adjacent clauses; one strands recovery and the other weakens the stated changed-field replay rule.

**Action classification: AUTOFIX.** Separate an immutable `LogicalCommandId` from a per-delivery nonce, or explicitly define the exact immutable field subset governed by the logical nonce and the refreshable authenticator fields. Bind replay-record retention, redispatch after expiry, rotation-overlap behavior, and emergency-revocation redispatch tests.

### H4 — The two-scope rolling-rate protocol has no deterministic `RateAdmissionId` or reserved-capacity rule

**Precise references**

- AD-21 introduces a deterministic `RateAdmissionId` spanning Party and Conversation ledgers, before context measurement and prepared-attempt creation, but does not define its components (`ARCHITECTURE-SPINE.md:312-316`).
- AD-29 says the shared canonicalizer derives every deterministic id but does not list `RateAdmissionId` (`ARCHITECTURE-SPINE.md:366-370`).
- FR-8 evaluates rolling rate/concurrency at step 5, while AD-13 creates the attempt descriptor only after context measurement; regenerations are also Agent Calls for rate purposes (`prd.md:81`, `286-297`; `ARCHITECTURE-SPINE.md:254-258`).
- AD-21 says both partitions “reserve” but does not state that prepared reservations count against the applicable limit before commit.

**Two-unit counterexample**

One ledger team uses `AgentInteractionId` as the rate-admission identity; every regeneration becomes the same consumption. Another uses `AttemptId`, even though it is not yet durably assigned at the rate gate. A third uses the proposal command's client key. Recovery cannot match Party and Conversation preparations across these implementations, and concurrent prepares can oversubscribe if one implementation counts only committed consumption while another counts prepared reservations.

**Action classification: AUTOFIX.** Add the exact purpose-tagged `RateAdmissionId` derivation and the durable ordinal/authorization event that exists before the two-scope prepare. State that live, unexpired preparations consume capacity for limit evaluation, define commit/abort/expiry ordering, and add crash/concurrency fixtures proving neither double charge nor oversubscription.

### H5 — The Conversations dependency is split in the spine/register but still indivisible in the bound PRD

**Precise references**

- AD-6 and the external register now correctly put the six core seams in `EXT-CONV-AI-1` and optional retraction in `EXT-CONV-RETRACTION-1` (`ARCHITECTURE-SPINE.md:184-188`; `external-dependency-register.md:51-85`).
- The PRD still calls retraction “seam 7” of `EXT-CONV-AI-1` (§8), makes A-28 retire when `EXT-CONV-AI-1` commits with that seam, and gives OQ-23 an option in which seam 7 lands in that same record (`prd.md:872-881`, `942`, `1074`).
- The PRD declares itself the governing requirements authority and the spine binds it (`prd.md:14-16`; `ARCHITECTURE-SPINE.md:12-22`).

**Two-unit counterexample**

The dependency-register team accepts the six core seams and makes core consumers eligible independently of retraction. The PRD/assumption evaluator keeps A-28 unretired or treats core `EXT-CONV-AI-1` as incomplete until seam 7 lands. Both follow an authoritative input, recreating the exact whole-record blockage the split was meant to remove.

**Action classification: AUTOFIX upstream wording without deciding OQ-23.** Replace the PRD's seam-7 references with `EXT-CONV-RETRACTION-1`, update A-28's retirement condition and OQ-23 option/revisit text, and preserve all three unresolved Product choices.

### H6 — Export runtime activation is gated at `Committed` in the spine but at `Available` in the dependency authority

**Precise references**

- AD-22 says export is disabled until `EXT-EXPORT-STORE-1` is `Committed`, then says “Once enabled” the port writes artifacts (`ARCHITECTURE-SPINE.md:318-322`).
- The external-dependency authority permits `Committed` for contract/package work only; every runtime, test, and qualification execution must wait for `Available` and a passing compatibility command (`external-dependency-register.md:35-47`).
- `EXT-EXPORT-STORE-1` is currently `Uncommitted`; its record says no runtime path may execute before the record and lifecycle policy are resolved (`external-dependency-register.md:193-207`).

**Two-unit counterexample**

After the target and command are accepted but before live compatibility passes, the export team follows AD-22 and enables runtime export at `Committed`. The dependency-gate team follows the register and returns `DependencyNotAvailable`. The first can write sensitive export bytes through an unqualified adapter while the second cannot exercise the path.

**Action classification: AUTOFIX.** Change the runtime threshold to `Available` with the compatibility command passing against the deployed target. Keep `Committed` solely as the ready-for-development threshold.

### H7 — `OD-EXPORT-LIFECYCLE-1` is fail-closed prose but is absent from the normative `OpenDecision` emitter

**Precise references**

- The spine adds `OD-EXPORT-LIFECYCLE-1` and says it blocks export, deletion qualification, and `RQ-1` (`ARCHITECTURE-SPINE.md:886-892`).
- AD-17 says the launch-readiness register is normative for blocker vocabulary and evaluation (`ARCHITECTURE-SPINE.md:286-290`).
- The register's `OpenDecision` emitter covers only a *Deferred PRD §13* row; it does not enumerate spine `OD-*` records or provide a typed observation for their approval/version (`launch-readiness-register.md:70-81`).
- The external-dependency record's note says the policy must be resolved but supplies no machine-readable policy decision field (`external-dependency-register.md:193-207`).
- PRD OQ-8 is already `Resolved` and does not contain the newly exposed lifetime/later-hold/restore/provider choice (`prd.md:1059`).

**Two-unit counterexample**

Once `EXT-EXPORT-STORE-1` becomes `Available`, a structured `RQ-1` evaluator follows the normative register and finds no Deferred PRD row corresponding to `OD-EXPORT-LIFECYCLE-1`; another evaluator parses the spine's prose table and emits `OpenDecision`. One can record READY while export and deletion qualification remain explicitly disabled; the other remains NOT READY.

**Action classification: AUTOFIX the fail-closed authority; DISCUSS only the policy answer.** Either add the unresolved export lifecycle as a Deferred PRD §13 decision without selecting an option, or extend the normative register schema/emitter to include versioned spine `OD-*` decisions with owner, approval evidence, and retirement state. Bind `EXT-EXPORT-STORE-1` availability to the approved policy version.

## Areas That Now Converge

The following v2 findings are materially closed and were not re-counted:

- AD-20 now binds snapshot-plus-current conjunctive safety, a staged platform-policy activation cohort, tenant epochs, an authoritative `SafetyVerdictEpoch`/per-Conversation index split, a fenced coordinator lease, bounded profile values, crash resumption, and exact `ContextReadUnavailable(RescanPending)` behavior. H1 above is only the remaining failed-retry transition ambiguity.
- AD-8 now binds human-only Party classification/liveness and the scheduled two-authoritative-empty protocol; an intervening nonempty or unavailable pass explicitly breaks the sequence. The residual identity issue is limited to cross-domain person comparison in H2.
- AD-7 now makes `AgentInteraction` the proposal truth, writes the outbox in the same append, carries source revisions, reconciles through a checkpoint before removal completion, and adds mirror initiation/lost-ack recovery. No Critical/High proposal-index divergence remains.
- AD-21 now separates rolling consumption, original-caller concurrency leases, and monthly monetary reservation/settlement. H4 concerns only the cross-scope rate-admission identity and reservation protocol.
- AD-30 now binds the HMAC field set, lifetime, audience, key version, overlap, emergency revocation, and constant-time verification. H3 is the remaining direct replay contradiction.
- The external dependency register now makes retraction independent, and the spine correctly distinguishes current Dapr Client/ASP.NET exposure at the parent-authoritative 1.18.5 catalog from the dirty Builds checkout's uncommitted 1.18.7 pin and future Workflow adoption. The remaining retraction issue is upstream PRD reconciliation, not register design.
- AD-22 now names an immutable export key, authenticated encryption, content-free index, JCS/JWS signature contract, trust anchor, physical purge receipts, and a fail-closed open policy. H6/H7 concern activation and blocker authority, not an invented policy answer.

## Consolidated Failure Scenarios

### Scenario A — The same hold/deletion commands preserve or erase content

1. A deletion is prepared and reaches `DeletionArmed`.
2. A valid legal hold arrives before destruction.
3. The spine implementation rejects the hold and completes deletion.
4. The PRD implementation records the hold as taking precedence and defers deletion.

### Scenario B — Bootstrap either deadlocks or erases an unrelated blocker

1. `LR-COST` is one `Block` covering missing numeric limits and stale pricing.
2. Matrix v4 omits only `NumericBudgetAndLimitsPresent` for initialization.
3. The record has no per-condition result to subtract.
4. One evaluator blocks forever; another ignores the whole gate and permits a write despite stale pricing.

### Scenario C — An expired trusted command is both required and forbidden to redispatch

1. A workflow command's HMAC envelope expires before delivery or its signing key rotates.
2. It is re-signed with the same logical nonce and idempotency tuple.
3. Issued/expiry/key/tag fields necessarily change.
4. One verifier rejects the changed authenticated fields; another accepts the explicitly permitted redispatch.

### Scenario D — Export qualifies without the policy that the spine says must block it

1. `EXT-EXPORT-STORE-1` eventually reaches `Available`.
2. `OD-EXPORT-LIFECYCLE-1` remains unresolved.
3. The normative register emitter sees no matching Deferred PRD §13 decision.
4. A structured evaluator can omit the blocker while a prose-aware evaluator emits it.

## Recommended Closure Order

1. Resolve C1's Product/Governance hold-precedence choice and align PRD, spine, runtime status, and fence tests before any irreversible operation.
2. Make matrix-v4 partial conditions machine-representable or replace them with command preconditions (C2).
3. Fix the safety terminal transition (H1), identity comparison domain (H2), and nonce/redispatch model (H3) before Provider retry or governed-audit implementation.
4. Complete the rate-admission identity/reservation protocol (H4).
5. Reconcile the PRD retraction references (H5), then repair export activation and open-decision authority without choosing the unresolved lifecycle policy (H6–H7).
