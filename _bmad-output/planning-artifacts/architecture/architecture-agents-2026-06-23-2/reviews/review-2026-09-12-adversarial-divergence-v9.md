---
name: Hexalith Agents adversarial-divergence review v9
type: architecture-review
lens: adversarial-divergence
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
verdict: fail
critical: 1
high: 4
medium: 2
low: 0
lint_ok: true
---

# Adversarial Divergence Review — v9

## Verdict

**FAIL — one Critical and four High divergences remain.** The v8 corrections close the former expiry/hold, role, finite-rescan, deferral, abandon, and kill-switch defects. A fresh independent-implementation pass nevertheless finds that export commit has no executable single-owner linearization, the posting handoff still disagrees about when all pre-post validation occurs, catalog activation has no command variant, ordinary no-hold deletion is blocked by a decision that is explicitly inapplicable, and hold-release recovery is omitted from the immutable decision-version pin.

The deterministic spine linter passed with `ok: true` and zero findings. Mechanical validity does not close the semantic divergences.

## Frozen Snapshot

The reviewed snapshot was SHA-256 checked before analysis and again after writing this report:

- `ARCHITECTURE-SPINE.md`: `f232325fb8fd05d040a0872a4b633fd4e2d4d717a3cc698e67fbbc4991d6064f`
- authoritative `VALIDATION-REPORT-2026-09-12.md`: `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`
- `IMPLEMENTATION-CONVENTIONS.md`: `a006a169b1b379df0ed41b3bea75eaf47859bf494fa3c0d6541ed8259d72a38c`
- bound `prd.md`: `cd36ccef836ddc38db09d67c22013c266c86ccb360d34d3fb587ee0bfc8baf52`
- `epics.md`: `755f908ab43a2c1c533d41c75e294d0c1937c22747d635b188362c567f75ecf6`
- `external-dependency-register.md`: `97b420ea0e2c915b8757d7be3df853e7fdf55b7234bd6eba0936b01ce6278085`
- `launch-readiness-register.md`: `5bee06e4303af18e280abe718fcc1b77e3df12d5e44a1360be413ed9e893f2c4`
- architecture `.memlog.md`: `f1716965f57a0923bc2cb56e63dcee74fb272621a4bd8cfb2d5031ee076a4ed1`
- repository instructions: `bfbe399b567d852a74c7ee8c9217217a3ea5eb80ca591b5b8ac01acfbe838966`
- Reviewer Gate instructions: `d32e32a3c1d59b5612b947004f3f6fef1117a13ce9f1ffa428d616e0b5d4db69`

Repository reality was inspected at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`. No submodule was initialized, updated, or mutated.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 4 |
| Medium | 2 |
| Low | 0 |

## Critical

### C-1 — Export commit requires cross-stream atomicity but defines neither the commit-decision owner nor an initial commit operation

**Evidence.** AD-2 assigns artifact/manifest outcome to `AuditExport` while `ProtectionFence` owns irreversible-operation linearization and the committed-export index high-water (`ARCHITECTURE-SPINE.md:171`). AD-22 then requires `ExportCommitted` only after artifact, manifest, and index acknowledgements and says the fence **atomically** advances that high-water (`ARCHITECTURE-SPINE.md:357`). It also explicitly says no read-model transaction is a multi-stream domain transaction. The operation matrix contains only `GovernanceProtection:ExportCommitRecovery`, whose precondition is an already-recorded `RecordedExportCommitDecisionAndLifecycleVersionMatch`; there is no initial `ExportCommitDecision` or equivalent variant that can create that fact (`launch-readiness-register.md:233-237`). AD-23 says an unlisted workflow activity blocks rather than inheriting another row (`ARCHITECTURE-SPINE.md:365`).

**Two literal units.** Team A appends `ExportCommitted` to `AuditExport`, then advances the fence index; a crash between the writes leaves a readable committed artifact absent from a deletion's fence snapshot. Team B advances the fence first, then acknowledges commit on `AuditExport`; a crash leaves the artifact protected but the export/key-delivery lifecycle indefinitely disagrees. Team C attempts an invented combined command, but the closed matrix has no such variant. None has a specified lost-ack rule that identifies which stream's event is the irreversible decision and which write is merely an idempotent acknowledgement.

**Impact.** The Team-A outcome permits deletion to omit an exported copy, causing an irreversible governance/data-protection violation. The other outcomes can strand committed bytes or expose incompatible status/key-delivery behavior. This is architecture and operation-matrix debt, not current implementation debt; no export store/fence implementation exists yet.

**Disposition — AUTOFIX, no Product choice required.** Make `ProtectionFence` the sole commit-decision owner (or name another single owner explicitly). Add a closed `GovernanceProtection:ExportCommitDecision` variant that, at one expected fence revision and after the exact artifact/manifest/index acknowledgements, appends the immutable commit decision and advances the committed-export high-water in the same aggregate event/batch. Make `AuditExport`'s committed state and key-delivery eligibility idempotent acknowledgements of that recorded decision. Define recovery before decision, after decision/before acknowledgement, and after lost acknowledgement; later deletion must use the fence decision even if the secondary status write is missing. Do not claim atomicity across two aggregate streams.

## High

### H-1 — The posting story and interaction diagram disagree with AD-5 about validation before `BeginPosting`

AD-5 and the normative convention say human approval appends only `Approved`; **after every required pre-post validation passes**, a separate `BeginPosting` appends only `PostingPending`, which alone authorizes the external append (`ARCHITECTURE-SPINE.md:197`; `IMPLEMENTATION-CONVENTIONS.md:9,15,29,44`). Story 7.4 first says one approval event binds the “posting-pending state,” then later requires a separate `BeginPosting` (`epics.md:2527-2540`). Both automatic and confirmation branches in the sequence append `PostingPending` and only afterwards “re-validate membership + AppendMessage” (`ARCHITECTURE-SPINE.md:571-586`).

One team commits `Approved`, performs all current safety/lifecycle/kill-switch/block/membership checks, then commits `PostingPending`. Another treats approval as already posting-pending or commits `PostingPending` before the final membership check, making the attempt uninterruptible before the complete validation set has passed. Those teams disagree on which concurrent disable, block, removal, or policy change may prevent an external message. The architecture target is clear enough to fix mechanically: Story 7.4 must say the approval event records `Approved` only, and each diagram branch must place the full named pre-post revalidation before `BeginPosting`; post-commit work may perform only the required effect-time seam authorization that cannot reopen the already-linearized domain decision. Current post-before-dispatch code is separately tracked delivery debt at `ARCHITECTURE-SPINE.md:1019`.

### H-2 — A pending decision catalog has no executable activation operation

AD-17 requires a successor to remain `PendingCatalog` until all newly required decision streams exist at exact minimum contracts, after which an independently authorized successor activates (`ARCHITECTURE-SPINE.md:323`). The register repeats that state machine (`launch-readiness-register.md:91,106,112`). The closed matrix exposes `PublishCatalog`, which appends `PendingCatalog`, plus decision `PublishContract` and `RecordApproval`; it exposes no `ActivateCatalog` operation (`launch-readiness-register.md:222-224`). Replaying `PublishCatalog` is also constrained by exact-replay/idempotency and monotonic-version rules, so activation cannot safely be inferred as a duplicate publish.

One team automatically activates from a projection when it observes all streams, another requires a second publish, and a third keeps the catalog pending because no allowed command owns activation. They can disagree on the effective decision set and every affected blocker. Add an explicit gate-free `ArchitectureDecision:ActivateCatalog` variant naming the exact pending catalog version/digest, root/predecessor authorization, required record/minimum-version manifest, expected catalog revision, recorder authority, idempotency identity, and concurrent/lost-ack behavior. Alternatively define those exact follow-up semantics on `PublishCatalog`; an implicit observer transition is insufficient for an EventStore authority.

### H-3 — The matrix blocks ordinary no-hold destruction on the hold/deletion decision that AD-22 says is inapplicable

AD-22 and Story 8.3 explicitly permit a deletion with no overlapping hold to append `DestructionStarted` directly at the expected fence revision; `OD-HOLD-DELETION-PRECEDENCE-1` applies only when a hold first contends after `DeletionArmed` (`ARCHITECTURE-SPINE.md:357`; `epics.md:2939-2942`). The decision row likewise says ordinary no-hold deletion remains evaluable (`launch-readiness-register.md:95`). But the register's global prose says the fence cannot append `DestructionStarted` without the approved decision, and the matrix row unconditionally requires `ApprovedHoldDeletionDecisionVersionMatches` (`launch-readiness-register.md:106,242`).

An AD/story team runs ordinary deletion while the unrelated decision is Open; a matrix team blocks every deletion indefinitely. This is conservative but materially incompatible and contradicts the declared affected scope. Change the direct precondition to the closed disjunction `NoOverlappingHoldAtArmedAndStart OR ApprovedHoldDeletionDecisionVersionMatchesForRecordedArmedContention`, and qualify the global sentence to the armed-contention branch. The unresolved Product outcome remains untouched.

### H-4 — Hold release is a durable branch decision but is omitted from the successor-version recovery pin

`OD-EXPORT-LIFECYCLE-1` affects `HoldRelease` and `HoldReleaseRecovery` when committed artifacts overlap (`launch-readiness-register.md:97`). `HoldRelease` records a human decision at an owner revision and then may require many artifact/DEK unpin acknowledgements (`launch-readiness-register.md:231-232`). Yet the phase-pin exception names only prepare disposition, export commit, and `DestructionStarted` in the spine/register prose (`ARCHITECTURE-SPINE.md:323`; `launch-readiness-register.md:106,112`), and the recovery row does not require the export-lifecycle version recorded by the human release decision.

If release is authorized under lifecycle v1, one artifact unpins, and v2 becomes pending before the remaining acknowledgements, one team applies the effective/pending union and strands a partial release; another treats `RecordedHumanReleaseDecisionAndRevisionMatch` as phase-pinned and finishes under v1. Add the immutable human hold-release decision to the phase-pin list and require `HoldReleaseRecovery` to match its recorded effective export-lifecycle version and exact store target whenever committed artifacts overlap. A successor governs only a new release decision, not acknowledgement completion for one already made.

## Medium

### M-1 — The spine frontmatter omits the two bound PRD decisions added by this update

The spine declares `binds: PRD OQ-1..OQ-32` (`ARCHITECTURE-SPINE.md:12-15`), while the bound PRD now contains OQ-33 (decision-recorder authority) and OQ-34 (hold-prepare cancellation), and the spine/register operationalize both (`prd.md:1087-1088`; `launch-readiness-register.md:96,104`). A tool or team honoring only frontmatter can omit them while a prose-aware team blocks correctly. Extend the binding range through OQ-34.

### M-2 — Story 8.1 still makes the export store globally mandatory while the matrix makes it overlap-conditional

The Story 8.1 dependency list says `EXT-EXPORT-STORE-1` “must be Available” for the story, then makes only the lifecycle decision conditional on committed-artifact overlap (`epics.md:2775-2777`). The matrix requires the store directly only when the frozen hold/release set contains committed artifacts (`launch-readiness-register.md:227,229-232`). One team blocks interaction-only DEK holds during an export-store outage; another permits them. This is fail-closed but operationally divergent. Make export-store availability conditional on a committed-artifact member, while retaining unconditional `EXT-PROTECTION-1`, secrets, Parties, and fence requirements.

## Recheck Of v8 Critical/High Findings

| v8 finding | v9 disposition |
| --- | --- |
| C-1 durable-before-post | **Substantially closed:** AD-5, conventions, and the debt table now agree that posting requires prior durable `PostingPending`. v9 H-1 is the narrower residual story/diagram disagreement over approval state and the placement of complete pre-post validation. |
| C-2 autonomous artifact expiry during pending hold | **Closed:** the external-store contract and Story 8.1 now require fence/store linearization for every expiry/purge/key/TTL/backup/restore action and retain bytes/keys on missing control evidence. |
| H-1 unauthorized Tenant Administrator hold-release approval | **Closed:** Story 8.1 permits only a distinct Compliance Inspector or Platform Operator and explicitly denies the Tenant Agent Administrator. |
| H-2 later decision successor strands recovery | **Closed for prepare, export commit, and destruction:** initial selection versus recorded-branch recovery is explicit. v9 H-4 identifies the one omitted durable hold-release branch. |
| H-3 unbounded safety activation | **Closed:** Stories 6.3/8.4 use finite manifests and make post-checkpoint initialization call-local. |
| H-4 held deletion rejected rather than deferred | **Closed:** Story 8.3 records `DeletionDeferredByHold`, visibility, successor-fence reconsideration, and race evidence. |
| H-5 PRD pre-decides armed-deletion race | **Closed:** PRD §9 now limits hold-wins to pre-arm and leaves post-arm contention solely to the unresolved decision. |
| H-6 abandon requires posting safety/membership success | **Closed:** Story 7.5 now separates current resolution authorization from posting gates. |
| H-7 kill-switch incident owner/story absent | **Closed:** `TenantGovernancePolicy` owns incident confirmations/review decisions/switch state; Story 8.4 and matrix define confirmation, pull, release, revisions, identities, and recovery. |

The authoritative validation report's original three Critical and twelve High findings were independently rechecked. Conjunctive snapshot/current safety, the shared fence, matrix bootstrap/scope, periodic recheck, distributed finite rescan, tagged human identity, three separate ledgers, proposal-index recovery, dependency split, envelope replay/rotation, status semantics, and export fail-closed policy are now explicit. None of the original findings recurs unchanged.

## Areas That Converge

- Safety retry is conjunctive and phase-pinned; a current weaker or unavailable policy cannot revive an attempt.
- Rate, original-caller concurrency, and original-month budget lifetimes remain separate and have bounded prepare/reset semantics.
- Replay registration, logical-command identity, delivery nonce, rotation, revocation, ACL-limited pre-command capabilities, and security-spool routing converge.
- Hold intent versus physical expiry now has an exact retain-on-unknown safety envelope; pre-arm deletion deferral and tagged human evidence converge.
- Finite safety manifests, post-checkpoint call-local initialization, rescan leases/cursors, and repeated crash recovery converge.
- Emergency containment facts are now owned by `TenantGovernancePolicy`, with direct confirmation/pull commands and an executable Story 8.4 handoff.

## Architecture Defects Versus Implementation And Tracking Debt

C-1 and H-1 through H-4 are architecture/handoff defects. C-1 concerns a target protocol not yet implemented. H-1 additionally intersects known implementation debt: `AgentInteractionProposalApprovalOrchestrator.cs:95-96,158-167` calls the Conversations append before EventStore dispatch, and `AgentProposalApprovalPolicy.cs:20-33` emits approval, posting-pending, and result together. The spine correctly labels that as Story 7.4 delivery debt (`ARCHITECTURE-SPINE.md:1019`); current code is not authority for weakening AD-5.

The root-authoritative Builds gitlink remains `a32cb422749352cce8dec948aa3e78c8f00eb4cf`, whose package catalog pins Dapr Client/ASP.NET/Workflow `1.18.5`. Its clean worktree is at non-authoritative `cf52f74c983bf88496cf0280cd9788b5ebcf50de` and pins `1.18.7`; Conversations, EventStore, FrontComposer, and Memories also have clean worktree HEADs different from their parent gitlinks, while Parties and Tenants match. Agents consumes parent-authoritative EventStore Client/DomainService references and has no direct Dapr Workflow reference. This is the already-recorded ARCH-A-15/dependency debt, not a new architecture finding.

The repository still lacks the target ledgers, safety epoch/index, proposal-index recovery, decision catalog/records, replay/security recorder, governance fence/export/deletion, and complete public contracts. Those are assigned implementation debt. The sprint 5.1/5.2 discrepancy is tracking debt under its open decision. This review claims no build, integration, or release success.

## Required Closure Order

1. Define the single-owner export commit decision and closed initial/recovery variants (C-1).
2. Reconcile Story 7.4 and the diagram with the AD-5 pre-post linearization (H-1).
3. Add the explicit decision-catalog activation operation (H-2).
4. Make deletion decision applicability conditional on armed hold contention (H-3).
5. Phase-pin recorded hold-release recovery (H-4).
6. Correct the two Medium scope/binding handoffs, re-distill, lint, and rerun the complete gate.
