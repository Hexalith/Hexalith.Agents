---
name: Hexalith Agents adversarial-divergence review v10
type: architecture-review
lens: adversarial-divergence
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
verdict: fail
critical: 1
high: 5
medium: 2
low: 0
lint_ok: true
---

# Adversarial Divergence Review — v10

## Verdict

**FAIL — one Critical and five High divergences remain.** The current revision closes every v9 Critical/High as originally stated: export commit now has one fence owner, posting validation precedes `BeginPosting`, catalog activation is explicit, no-hold destruction is decision-independent, and hold-release recovery is phase-pinned. A fresh whole-artifact pass found a new irreversible deletion-completeness gap in the newly specified Conversation-deletion path, plus five High protocol/authority gaps around executing that path, durable pre-arm deferral, decision-recorder bootstrap, export lifecycle-version selection, and source-deletion cancellation.

The deterministic spine linter passed with `ok: true` and zero findings. Mechanical correctness does not close the semantic findings.

## Frozen Snapshot

The inputs were SHA-256 checked before review and again after this report was written:

- `ARCHITECTURE-SPINE.md`: `50de644622287cb62bec31a56ee8dff3028e04c06221cd8d292e3b0cbdb40604`
- authoritative `VALIDATION-REPORT-2026-09-12.md`: `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`
- `IMPLEMENTATION-CONVENTIONS.md`: `a006a169b1b379df0ed41b3bea75eaf47859bf494fa3c0d6541ed8259d72a38c`
- bound `prd.md`: `af43b92cf23c983ab31d85766cebb74b8ed884fdc4f7ed8c84b0f6aa5f4645f5`
- `epics.md`: `687c4c20723e16c1bddc9a23ab30c10133dfbaec436e4eec2f6d43dc4fa5b906`
- `external-dependency-register.md`: `cd6bd54ceceb147fe6f7c25190450ab27c4a45a6ca922baac93ddb98fd55b836`
- `launch-readiness-register.md`: `f329e279b6e9675a930ccfd5d5e54ff4bdc87655b590e564a7303f4e9f2fd24f`
- architecture `.memlog.md`: `6f366ce5ca778ea66cde8cf4aba6a11161934a0998d5f4fe1f7e5fa2a9b75908`
- repository instructions: `bfbe399b567d852a74c7ee8c9217217a3ea5eb80ca591b5b8ac01acfbe838966`
- Reviewer Gate instructions: `d32e32a3c1d59b5612b947004f3f6fef1117a13ce9f1ffa428d616e0b5d4db69`

Repository reality was inspected at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`. No root or nested submodule was initialized, updated, or mutated.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 5 |
| Medium | 2 |
| Low | 0 |

## Critical

### C-1 — Conversation deletion has no authoritative complete interaction directory or quiescence fence

**Evidence.** AD-6 says intake freezes an “authoritative content-free by-Conversation interaction checkpoint,” terminalizes every derived nonterminal interaction through it, reconciles late indexed records, and only then freezes the deletion set (`ARCHITECTURE-SPINE.md:212`). The matrix merely restates `ConversationInteractionCheckpointAndIndexSourceExact` without naming its owner, event protocol, completeness high-water, or creation fence (`launch-readiness-register.md:240`). AD-2's exact aggregates contain no complete by-Conversation interaction directory: `ConversationAgentState` holds only a nonterminal-proposal materialization/high-water, while `AgentInteraction` streams are independently keyed (`ARCHITECTURE-SPINE.md:174,218`). The explicit projection inventory has rebuildable `agent-interaction-status`, but no authoritative complete interaction-directory contract (`launch-readiness-register.md:349-381`). Story 8.3 repeats “authoritative checkpoint” and “reconciles late indexed records” without supplying a finite manifest or a condition that prevents a concurrent interaction from becoming content-bearing after the frozen set is chosen (`epics.md:2949-2953`).

**Two literal units.** Team A enumerates `agent-interaction-status` at its current projection position. Team B scans EventStore interaction streams through a global position. A call that registered or created its `AgentInteraction` after Team A's projection checkpoint but from a Conversation read that preceded deletion can append protected context or an outcome after intake; Team A can see “no late indexed records” and freeze a set that omits it. Team B may catch it, but no contract prevents another pre-intake in-flight creation after Team B's scan. The existing proposal index cannot close this race because it covers nonterminal proposals, not every content-bearing interaction or generation-failure record.

**Impact.** The deletion may reach restrictive success while protected Agent content derived from the deleted Conversation survives outside its frozen set. Because completion then permits key/projection cleanup and records a tombstone, this is an irreversible false-deletion/data-integrity failure, not merely delayed cleanup.

**Disposition — AUTOFIX, no Product choice required.** Name one EventStore-authoritative, content-free Conversation interaction directory/permit owner, naturally an explicit extension of `ConversationAgentState` or a separately listed aggregate. Before any content-bearing `AgentInteraction` append, a deterministic interaction permit/id must register on that Conversation owner. Conversation-deletion intake must serialize a deletion marker against new permits, freeze a finite count/hash/high-water manifest of all registered interaction and failure-record ids, wait for every pre-marker permit to close or be terminalized, and forbid any post-marker permit. A crash between permit and interaction creation must resolve as a named no-stream member, not disappear; a call already holding a permit must be included even if its stream appears after the marker. Only the complete acknowledged manifest may feed `ProtectionFence`. Add first/last-permit, projection lag, call-read-before-delete, call-create-after-delete, lost-ack, and replay failure injection. Do not use a rebuildable projection as the deletion authority.

## High

### H-1 — The Conversation-deletion workflow has no closed matrix variants for terminalization or reconciliation

AD-17 requires every workflow activity to declare one concrete matrix family/variant and blocks an absent variant (`ARCHITECTURE-SPINE.md:322`). The only `ConversationDeletionPropagation` row is `Intake`, and it may append **only** the signal/checkpoint to `ProtectedDeletion` (`launch-readiness-register.md:240`). AD-30 separately authorizes the workflow to terminalize many `AgentInteraction` streams, reconcile the index, and later dispatch ordinary deletion commands (`ARCHITECTURE-SPINE.md:424,426`), but the matrix has no `TerminalizeInteraction`, `ReconcileDirectory`, or `FreezeDeletionSet` variants. `GovernanceProtection:DeletionPrepare` begins only after those steps (`launch-readiness-register.md:241`), and the catch-all cannot rescue an absent family/variant (`launch-readiness-register.md:250-252`).

One team reuses `Intake` for terminalization despite its only-append restriction; another correctly blocks each activity; a third borrows `SystemTimer` or `DeletionPrepare`. They produce different gate sets, principals, and mutation scopes. Add exact variants for each durable step with target aggregate, source-signal/request revision, directory manifest/high-water, expected revision, permitted terminal event, idempotency tuple, and lost-ack recovery. Keep the workflow incapable of expanding tenant/Conversation scope.

### H-2 — The closed matrix cannot record the required pre-arm `DeletionDeferredByHold` state

AD-22 and Story 8.3 require a request that overlaps an active/preparing hold before `DeletionArmed` to be durably accepted as `DeletionDeferredByHold`, with its whole frozen set, no preparation, and automatic successor-fence reconsideration (`ARCHITECTURE-SPINE.md:362`; `epics.md:2939-2942,2949-2953`). The only initial governance row, however, requires `NoOverlappingHoldOrExportCleanupPending`; when the hold exists it blocks and authorizes no deferral append (`launch-readiness-register.md:241`). There is no `DeletionDeferByHold` variant, and `Intake` is restricted to signal/checkpoint fields.

A story-literal team records deferral; a matrix-literal team makes no durable state change and may require signal redelivery or operator resubmission. Add a `GovernanceProtection:DeletionDeferByHold` decision variant (or a closed mutually exclusive result on an explicitly renamed initial-decision row) that records exact request/signal revision, whole frozen set, overlapping hold ids/revisions, fence revision, and reconsideration identity. It must be legal while the armed-contention OD is Open and must never prepare or destroy.

### H-3 — `OD-RELEASE-RECORDER-SCOPE-1` blocks the only command that could materialize its own resolution

All four decision publication/activation rows are “blocked while `OD-RELEASE-RECORDER-SCOPE-1` is Open” (`launch-readiness-register.md:222-225`). The spine says every principal remains denied until Product selects and the PRD records the authority source (`ARCHITECTURE-SPINE.md:328`), and the runtime never parses planning documents. After Product makes that external choice, its runtime `ArchitectureDecisionRecord` is still Open or absent; `RecordApproval` is therefore barred from recording the signed choice that would close the blocker. The same cycle prevents genesis catalog/contract materialization.

One team treats the amended PRD as sufficient and bypasses the runtime OpenDecision check; another honors runtime authority and can never bootstrap; a third directly seeds EventStore outside the command convention. Define a narrow self-bootstrap rule without choosing the Product outcome: once an independently signed Product/Release-PM/Governance package identifies the source, a Release Operator freshly proven under that exact proposed source may record only the matching `OD-RELEASE-RECORDER-SCOPE-1` package despite that target record's current Open state. Bind root authorization, expected no-stream/current revision, exact contract/catalog digest, recorder/approver separation, replay, and conflict. Normal decision commands remain blocked until that fact is effective.

### H-4 — Export commit does not say whether it is pinned to prepare-time lifecycle policy or re-evaluates a successor

`ExportPreparing` records `LifecyclePolicyVersion`; the commit event also records a lifecycle decision version (`ARCHITECTURE-SPINE.md:362`). The new matrix row calls commit an initial single-owner decision that “reads no mutable readiness record after export prepare,” but requires an `ApprovedExportLifecycleDecisionVersionAndExactStoreTargetMatch` without saying it must equal the prepare token's recorded version (`launch-readiness-register.md:238`). AD-17 says effective/pending unions govern initial branch selection and only the **commit decision** starts phase-pinned recovery (`ARCHITECTURE-SPINE.md:328`).

If lifecycle v2 becomes pending/effective after artifacts were prepared under v1 but before `ExportCommitDecided`, one team commits under frozen v1, another blocks/reprepares under the v1/v2 union, and another records v2 against v1-created bytes. Later hold/expiry/restore behavior can then disagree with the store contract that produced the artifact. Specify one rule: either commit must match the exact prepare-time decision/version/target and that prepare is the branch-selection pin, or any successor before commit invalidates reversible preparation and requires a recorded abort/reprepare under the new union. Explicitly forbid relabelling v1 artifacts as v2. This is architecture lifecycle mechanics, not selection of the unresolved lifecycle outcome.

### H-5 — A source-approved Conversation deletion can be locally cancelled through the generic deletion-abort rule

AD-6 says the authenticated source signal is authority for deletion of exact derived content and cannot impersonate the operator/Inspector branch (`ARCHITECTURE-SPINE.md:212`). The matrix nevertheless gives every deletion prepare the same Abort source, `RecordedAuthorizedDeletionCancellationOrProfileMappedClosedFailureFactValid`, without discriminating operator-origin from Conversations-origin (`launch-readiness-register.md:242`). Story 8.3 applies the generic authorized-cancellation criterion immediately after the source-signal criterion and provides no source-specific cancellation authority (`epics.md:2949-2958`). The committed `EXT-CONV-AI-1` shape has no cancellation/supersession signal (`external-dependency-register.md:57-60`).

One team lets a Platform Operator/Inspector cancel the source-derived request using the human path; another refuses because Conversations' approved deletion remains authoritative; a third treats a local permanent failure as a terminal Abort. The first two disagree about FR-30 erasure, and the third can leave data permanently undeleted after an upstream approved deletion. Close the origin union: with the currently specified seam, a `ConversationDeletion` origin may Resume/retry but cannot take a locally authorized Abort/cancel branch. If Product/Conversations later require cancellation, add an authenticated source supersession signal and explicit authority as a new external contract rather than borrowing human roles.

## Medium

### M-1 — The deletion tombstone schema assumes human requester/approver fields for a non-human source origin

Story 8.3 requires the support-safe tombstone to record “requester, approver” (`epics.md:2960-2963`) and says source deletion follows the same tombstone contract, while AD-6/AD-30 explicitly forbid the source workflow from carrying or inventing a human principal (`ARCHITECTURE-SPINE.md:212,426`). Teams can omit those fields, insert the Conversations approval reference, or fabricate service/human actor values. Define a discriminated `DeletionOrigin`: `OperatorRequest` carries Platform requester plus Inspector approval evidence; `ConversationDeletion` carries authenticated source identity, signal id/revision, source approval reference/contract version, and explicitly absent human fields.

### M-2 — Logical deletion idempotency is stable only for byte-identical signal replay, not for source re-emission

`DeletionRequestId` includes both `ConversationDeletionSignalId` and `SourceRevision` (`ARCHITECTURE-SPINE.md:418`; `epics.md:2951`). `EXT-CONV-AI-1` requires exact replay for the same signal id to be idempotent but never states that one durable Conversations deletion decision has exactly one stable signal id/revision across publisher recovery or re-emission (`external-dependency-register.md:57-60`). Two conforming source publishers can redeliver the same deletion decision with a new delivery event identity, creating two Agents deletion requests. Require a stable logical source-deletion decision id independent of delivery attempts, or bind `ConversationDeletionSignalId` one-to-one to the immutable approval/source decision and state that retransmission cannot mint a new id/revision. Delivery identity may vary separately if needed.

## Recheck Of v9 Critical/High Findings

| v9 finding | v10 disposition |
| --- | --- |
| C-1 export commit lacked one owner/initial operation | **Closed as stated:** `ProtectionFence` now owns `ExportCommitDecided` and advances the high-water in one append; `AuditExport` acknowledgement is secondary and key-gating. v10 H-4 is the narrower successor-version boundary before that decision. |
| H-1 posting story/diagram ordering | **Closed:** approval records only `Approved`; both sequence branches perform current safety/lifecycle/tenant/kill-switch/block/Party and Conversation existence/access/membership reads before `BeginPosting`; current effect-first code remains declared debt. |
| H-2 catalog activation missing | **Closed:** `ArchitectureDecision:ActivateCatalog` owns one expected-revision `CatalogActivated` append with an exact record revision/version/digest manifest and lost-ack lookup. v10 H-3 concerns recorder-source self-bootstrap, not activation mechanics. |
| H-3 no-hold deletion overblocked | **Closed:** `DeletionDestructionStarted` now uses the exact disjunction between no overlapping hold and approved armed-contention outcome. |
| H-4 hold-release successor pin omitted | **Closed:** the phase-pin list and recovery row bind human release revision, lifecycle version, frozen set/token, and exact store target. |

## Recheck Of The Authoritative Validation Findings

The authoritative report's C-1 through C-3 and H-1 through H-12 were re-evaluated, not presumed closed. The current artifacts converge on conjunctive snapshot/current safety and retry dispositions; shared hold/export/deletion fencing; matrix-v4 scope/bootstrap; periodic approver recheck; distributed finite rescan; stable human identity; three ledger owners/lifetimes; proposal-index crash recovery; dependency split; replay/rotation/security recording; status semantics; exact current Dapr exposure; and sprint/dependency debt separation. None of those original findings recurs unchanged. C-1/H-1/H-2/H-5 above arise from the newly added Conversation-deletion protocol; H-3/H-4 are deeper governance boundary cases in the revised contracts.

## Areas That Converge

- Posting has durable `Approved`, durable `PostingPending`, effect, and result/lost-ack steps; the sequence and convention agree.
- Decision catalog publication and activation are separate, expected-revision operations with a complete activation manifest.
- Export commit has one irreversible fence decision; deletion observes it even if the secondary export acknowledgement is absent.
- Hold release, prepare dispositions, export commit, and destruction recovery are phase-pinned against later decision successors.
- No-hold deletion remains executable while armed hold/deletion precedence is unresolved; pre-arm hold is specified as deferral rather than rejection.
- Rate/open/budget ownership and reset/lifetime semantics remain explicit. `OD-RATE-CONCURRENCY-CONSUMPTION-1` correctly surfaces the unresolved Product/Architecture choice and blocks before either ledger; this review does not select it.
- Trusted-envelope logical/delivery identity, tenant routing, replay registration, rotation/revocation, and ACL-confined security recording remain convergent.

## Architecture Defects, Product Choices, And Delivery Debt

C-1 and H-1 through H-5 are target architecture/handoff defects. Their closure requires owner/ordering/variant rules, not invention of an unresolved Product outcome. `OD-HOLD-DELETION-PRECEDENCE-1`, `OD-HOLD-PREPARE-CANCELLATION-1`, `OD-EXPORT-LIFECYCLE-1`, `OD-RATE-CONCURRENCY-CONSUMPTION-1`, `OD-RELEASE-RECORDER-SCOPE-1`, and the materialized PRD decisions remain legitimate open choices; H-3 defines how a future signed recorder-scope choice can be recorded, and H-4 defines the version boundary without selecting the lifecycle contents.

Current implementation is materially behind the target and is correctly labelled as debt. There is no shipped Conversation-deletion intake, complete interaction directory, governance fence/deletion/export implementation, decision catalog/record, three-ledger implementation, safety epoch, trusted replay/security recorder, or required public vocabulary. The checked-in posting orchestrators/policies still implement effect-first/combined behavior; that is Story 7.4 delivery debt, not authority to weaken AD-5.

The root-authoritative gitlinks remain Builds `a32cb422749352cce8dec948aa3e78c8f00eb4cf`, Conversations `73bcee6f04479d4743d5a65ce929728e22687d7d`, EventStore `ce9e779a3ec24d9b9a054afd0ceaa3c05b8adaaa`, FrontComposer `053b2008307d4e476c0d4329e6c47763c301d43e`, Memories `3644ef63da87b8c7ec9e17726fae7f534e642a1c`, Parties `fa42398552fba1c80eb2760791517659d6d1313a`, and Tenants `2fac18396ff11a4459de053b3ebb7ddfe7c13e30`. Builds, Conversations, EventStore, FrontComposer, and Memories have clean nested worktrees at different non-authoritative commits; Parties and Tenants match. Parent-authoritative Builds pins Dapr Client/ASP.NET/Workflow `1.18.5`; Agents consumes EventStore Client/DomainService, whose parent-authoritative projects reference Dapr Client/ASP.NET, and Agents has no Dapr Workflow reference. The dirty Builds worktree's `1.18.7` is not a parent upgrade. This remains ARCH-A-15/dependency debt, not a new architecture finding. Sprint 5.1/5.2 remains tracking debt under its explicit open decision. No build, integration, or release success is claimed.

## Required Closure Order

1. Add an authoritative complete Conversation interaction directory/permit/quiescence protocol (C-1).
2. Add the exact deletion workflow activity variants and pre-arm deferral variant (H-1/H-2).
3. Break the recorder-scope self-bootstrap cycle without choosing the Product source (H-3).
4. Fix the export prepare-to-commit lifecycle-version boundary (H-4).
5. Close source-deletion cancellation/abort authority and provenance/idempotency shapes (H-5, M-1, M-2).
6. Re-distill, run deterministic lint, and rerun the full reviewer gate.
