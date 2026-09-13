---
name: Hexalith Agents adversarial-divergence review v8
type: architecture-review
lens: adversarial-divergence
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
verdict: fail
critical: 2
high: 7
medium: 2
low: 0
lint_ok: true
---

# Adversarial Divergence Review — v8

## Verdict

**FAIL — two Critical and seven High divergences remain.** The current revision closes the named v7 defects, but a fresh whole-artifact pass found that the posting handoff can still place a Conversation effect before its durable owner decision, and the export-store contract cannot yet guarantee that a hold arriving near artifact expiry preserves the bytes while the store/control path is unavailable. The other High findings are authorization, decision-recovery, rescan-barrier, deletion-deferral, unresolved-policy, abandonment, and emergency-containment handoff defects.

The deterministic spine linter passed with `ok: true` and zero findings. Mechanical correctness does not offset the semantic findings.

## Frozen Snapshot

The reviewed files were SHA-256 checked before analysis and again after this report was written:

- `ARCHITECTURE-SPINE.md`: `8ce81ba273d0cd9185f74886513eeccf5e5e6a732313eec3c2aeaa0c311569f4`
- authoritative `VALIDATION-REPORT-2026-09-12.md`: `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`
- `IMPLEMENTATION-CONVENTIONS.md`: `31190a6a351a0710878d8f00d2a1a4c79e15e37a644dbfb6526b2d0d462cba73`
- bound `prd.md`: `3028350705ee22365f67669732b961ee8a7671a36851deb11a6026477d3b1279`
- `epics.md`: `40dc6bee2bfeeb4fa9f594bd73a09f28dbd779fccd09b74682b10c0231bc6315`
- `external-dependency-register.md`: `46009827b0010a40c5432c2b5f49dc9a5f78580e82b60b9760e2a4ef6547eba8`
- `launch-readiness-register.md`: `5603d65672dfa03da67b1437800bbb912f962335756c576655d2105dfc2625c2`
- architecture `.memlog.md`: `69ee2dd3d6440f6eeca5fb26c4d7bd566a0effec26e3b11a766ed8e906a0f8f8`
- repository instructions: `bfbe399b567d852a74c7ee8c9217217a3ea5eb80ca591b5b8ac01acfbe838966`
- Reviewer Gate instructions: `d32e32a3c1d59b5612b947004f3f6fef1117a13ce9f1ffa428d616e0b5d4db69`

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 2 |
| High | 7 |
| Medium | 2 |
| Low | 0 |

## Critical

### C-1 — The normative command convention and sequence still permit posting before durable approval/post-attempt ownership

**Evidence.** AD-5 makes `Approved`, `PostingPending`, and the posting result distinct authoritative states, and makes `PostingPending` the uninterruptible attempt with the stored deadline (`ARCHITECTURE-SPINE.md:188-196`). The interaction diagram nevertheless performs the Conversations append and only then records posting success/failure, with no `PostingPending` append before either the automatic or confirmation-mode effect (`ARCHITECTURE-SPINE.md:562-574`). The normative companion is more explicit: one `ApproveProposedAgentReply` command may emit `Approved -> PostingPending -> Posted|PostingFailed` as one EventStore result after orchestration has completed its external calls (`IMPLEMENTATION-CONVENTIONS.md:7-17,31-35`). Story 7.4 likewise says one approval event binds a “posting-pending state” rather than naming the durable begin-post decision and later result commands (`epics.md:2518-2523`).

Repository reality proves this is not hypothetical. `AgentInteractionProposalApprovalOrchestrator` calls `ApproveAndPostAsync`, including `AppendAgentMessageAsync`, before `DispatchAsync` (`src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalApprovalOrchestrator.cs:75-100,133-167`); `AgentProposalApprovalPolicy` then emits all three events in one result (`src/Hexalith.Agents/AgentInteraction/AgentProposalApprovalPolicy.cs:14-37`).

**Two literal units.** A state-machine team appends approval at the expected revision, revalidates, appends `PostingPending` with its immutable attempt/deadline, posts, and records the result. A convention/current-code team posts first and dispatches one aggregate command afterwards. In the second unit a concurrent rejection, abandonment, expiry, or stale expected-revision failure can win after the external append but before the command: the Agent stream records a non-posted terminal outcome while the Conversation already contains the message. Idempotent `MessageId` prevents a duplicate append but cannot undo that contradictory terminal decision or retroactively establish the missing authorization fact.

**Impact.** An irreversible externally visible message can exist without a durable approval/posting-attempt owner and can contradict the authoritative proposal state and audit trail. This is an architecture/companion contradiction. The checked-in ordering is separate implementation debt and must not be described as a compliant shipped example.

**Disposition — AUTOFIX, no Product choice required.** Preserve AD IDs and make one protocol literal everywhere: (1) approval selects and durably records `Approved` at the expected revision; (2) after full pre-post validation, a separate deterministic command durably records `PostingPending` with the attempt identity, `MessageId`, and deadline; (3) only that fact authorizes the external append; (4) a later command records success/failure or lost-ack lookup. Correct both diagram branches, Story 7.4, and the convention; remove the current approval policy from “Representative Shipped Examples” until refactored, and add this exact ordering to the delivery-debt table and Story 7.4 evidence.

### C-2 — A pending legal hold cannot actually prevent autonomous export expiry during store/control-path outage

**Evidence.** AD-22 says a committed-artifact hold remains restrictive pending when the decision is unavailable or the exact store target is unavailable, and during that interval the artifact “cannot expire, purge, or report an unpin” (`ARCHITECTURE-SPINE.md:346-356`). The store record provides expiry/purge operations and generically requires later-hold behavior, but it does not require every expiry/purge to obtain a current fence authorization, define a linearization point against hold prepare, or require a disconnected store to retain bytes by default (`external-dependency-register.md:193-207`). Story 8.1 waits for pin acknowledgement before reporting Active, but it likewise has no rule that stops the artifact's already-scheduled expiry while the pin request cannot reach the store (`epics.md:2771-2779,2791-2799`).

**Two literal units.** A blob-store team implements the approved exact lifetime with an autonomous provider TTL and supports pin/unpin when reachable. A fence team appends the hold prepare token just before expiry, then loses store connectivity and correctly leaves the hold restrictive pending. The autonomous TTL still purges the artifact because it never observes the token. Another store team makes every expiry an Agents-orchestrated, fence-authorized operation and retains on control-plane failure. Both can satisfy the present “expiry/purge,” “later-hold behavior,” and acknowledgement prose; only the second satisfies the asserted preservation invariant.

**Impact.** The only lifecycle-covered exported copy can be irreversibly destroyed after a valid hold intent has won the Agents fence. A later Active state can never restore it. This is an architecture/external-contract defect, not current implementation debt (the export store is not implemented).

**Disposition — AUTOFIX the safety envelope without choosing the open later-hold outcome.** Require the approved lifecycle contract to define one atomic expiry-versus-hold linearization through `ProtectionFence` (or an equivalently linearizable store reservation) and require every physical expiry, purge, key destruction, provider TTL, backup expiry, and restore cleanup to carry that authorization. Loss of fence/store-control evidence must retain bytes and keys. Add failure injection for hold intent immediately before/at expiry, lost pin acknowledgement, store/control outage, autonomous lifecycle workers, and restore. `OD-EXPORT-LIFECYCLE-1` still owns whether the winning later hold extends or supersedes expiry; the architecture must only make either approved result executable without silent destruction.

## High

### H-1 — Story 8.1 grants a Tenant Administrator a hold-release approval role that the PRD and ADs forbid

PRD FR-33 permits release only from a Compliance Inspector with approval by a **second Compliance Inspector or Platform Operator** (`prd.md:516-519`). AD-22 repeats that closed set, and AD-30 adds only the narrow Platform second-party grant (`ARCHITECTURE-SPINE.md:350,410-414`). Story 8.1 instead includes “a tenant `Administrator` alternative” in the release-approval evidence union (`epics.md:2781-2784`). One story implementation therefore accepts a Tenant Agent Administrator as second party; an AD/PRD implementation rejects it. Unauthorized unpin can expose held evidence to expiry/deletion. Remove the Administrator alternative from the hold-release criterion and add an explicit wrong-role TAA denial test. Keep Administrator evidence only where FR-24 actually permits a current TAA second party (inspection/export).

### H-2 — A pending decision successor can strand a branch that recovery is required to finish

AD-17 says every pending successor preserves the union of previous/candidate affected evaluations and emits `OpenDecision` for every intersecting operation (`ARCHITECTURE-SPINE.md:316`). The register's decision rows include hold resume/unwind recovery, deletion purge recovery, export commit recovery, and deletion completion recovery in `AffectedEvaluations` (`launch-readiness-register.md:78,89-104`). But AD-17/AD-23 and the matrix say a recorded branch's dedicated recovery is gate-free, reads no mutable readiness record, validates the recorded policy version, and must converge despite later blockers (`ARCHITECTURE-SPINE.md:318,358-364`; `launch-readiness-register.md:221-243`).

After a branch starts under approved v1, a v2 `PendingContract` gives one evaluator an `OpenDecision` blocker while another follows the frozen-v1 recovery row and completes. For `DestructionStarted`, the former leaves a partially erased tenant indefinitely. Make affected-evaluation applicability phase-aware: current decision union governs initial work and branch selection; after the immutable disposition/commit/destruction decision, recovery evaluates only the recorded effective version and direct outcome evidence. A later decision can govern only a new branch. Add successor-publication failure injection at every first/last acknowledgement.

### H-3 — Stories 6.3 and 8.4 turn an on-demand safety initialization into an unbounded activation barrier

AD-20 freezes the finite existing-index manifest and activates after every manifest member acknowledges; a Conversation absent from that checkpoint initializes on demand before its first call (`ARCHITECTURE-SPINE.md:332-336`). Story 6.3 says activation also waits for “every post-checkpoint initialization,” and Story 8.4 repeats the same barrier (`epics.md:1992-2000,2973-2976`). There is no second cutoff, membership rule, or high-water for that continuously growing set. One coordinator activates after the frozen manifest and uses conditional on-demand initialization; another continuously extends the barrier and can be starved forever by newly encountered Conversations. Align both stories to AD-20. If post-checkpoint work is intended to join activation, define a second finite checkpoint and exact ownership handshake; otherwise state explicitly that it is call-local and not an epoch-activation acknowledgement.

### H-4 — Story 8.3 rejects a held deletion instead of recording the required durable deferral lifecycle

AD-22 requires a request overlapping any active/preparing hold to be recorded as `DeletionDeferredByHold` with the whole frozen set, remain visible, and be automatically reconsidered after the last hold releases (`ARCHITECTURE-SPINE.md:346-350`). PRD §9 exposes that signal and lifecycle (`prd.md:663,955-957,977`). Story 8.3 says acceptance “rejects” when a hold protects the content and contains no deferral event or automatic reconsideration criterion (`epics.md:2904-2907`). One implementation discards the request and requires a human resubmission; another records and later executes it. Split the story outcomes: invalid scope/authority/revision rejects; hold overlap accepts the deterministic request into durable deferred state, performs no prepare/destruction, and re-enters eligibility through the fence after the last release. Add crash/release/new-hold races and UI visibility evidence.

### H-5 — The PRD still pre-decides the armed-deletion race that its preceding sentence leaves open

PRD §9 first says a later hold overlapping an already armed deletion is governed by `OD-HOLD-DELETION-PRECEDENCE-1`, not an adapter default (`prd.md:955`). Two lines later it says a legal hold takes precedence over **every deletion signal** and executes the deferred deletion on release (`prd.md:957`). AD-22 correctly leaves accept-and-cancel versus defer/reject unresolved (`ARCHITECTURE-SPINE.md:350,356`). A PRD-literal team therefore implements hold-wins for `DeletionArmed`; an OD-aware team blocks pending the recorded outcome. This is an unresolved Product decision, not an architecture autofix. Product/Governance/Security must qualify the broad PRD sentence to pre-arm/hold-won signals or explicitly resolve and materialize the armed case; do not infer the outcome during architecture update.

### H-6 — Story 7.5's universal safety/membership gates can make the safety-failure abandon path impossible

PRD FR-18 says a safety-verdict `PostingFailed` proposal is not retryable and “accepts abandon only” (`prd.md:463`; reiterated at `prd.md:1080`). AD-5 makes abandon a non-posting terminal action, including while disabled/suspended, and its administrative/TAA path may deliberately require no Conversation read (`ARCHITECTURE-SPINE.md:192-196`). Story 7.5 instead requires every permitted `PostingFailed` exit to rerun current authorization, membership, safety, and operation gates before terminal transition (`epics.md:2586-2589`). A gate-literal implementation can never abandon content that still fails safety, and may also strand the administrator path when Conversation access is unavailable; another applies only authorization plus the `MessageId` absence/deletion rule. State that abandon validates the current actor and `ProposalResolution` contract and never reauthorizes content for posting. Membership/safety are evidence/reasons for no-post disposition, not success gates; only retry/posting enters full `ConversationPosting` revalidation.

### H-7 — The emergency kill-switch fact has neither a named durable owner nor an executable story owner

AD-12 requires `TenantKillSwitch:PullContainment` to bind a durable `ConfirmedSm4Incident` fact and its source revision but never names its aggregate/stream, append authority, or idempotency/recovery identity (`ARCHITECTURE-SPINE.md:260-264`). The matrix repeats only an abstract `ConfirmedSm4IncidentEvidenceValid` direct precondition (`launch-readiness-register.md:213,243`). `epics.md` advertises matrix-v4 split kill-switch behavior and FR-30 coverage (`epics.md:96,159,332`), but no active story acceptance criterion names `ConfirmedSm4Incident`, the direct-append containment path, trigger-review pull, or release; Story 8.4 merely lists the kill switch as a `TenantGovernancePolicy` field (`epics.md:2978-2981`). One team records confirmation in `SecurityEventLog`, one in `TenantGovernancePolicy`, and a third treats a metric projection row as evidence; each gets a different revision, authority, and replay behavior, and the backlog can be declared complete without building the emergency path. Name one EventStore owner and exact confirmation command/event/idempotency tuple, then add the entire pull/release protocol and failure-injection evidence to a preserved story, naturally Story 8.4 unless another existing owner is chosen.

## Medium

### M-1 — Epic 8 makes conditional export dependencies and decisions unconditional

The matrix lets an interaction-only hold proceed without the export store/lifecycle decision and requires them only when committed artifacts overlap (`launch-readiness-register.md:95-96,221-226`). Story 8.1 and its evidence status require `EXT-EXPORT-STORE-1` for the entire story (`epics.md:2758-2762,2810-2817`). Story 8.3 similarly requires the export-lifecycle and instruction-protection decisions before all irreversible work even where its frozen set contains neither exports nor Agent Instructions (`epics.md:2896-2900,2941-2950`). These conservative implementations remain safe but produce incompatible readiness and unnecessary operational blockage. Make each dependency/decision conditional on the named frozen-set branch while retaining the common protection/fence requirements.

### M-2 — The global decision recorder has no closed tenant-role source

The decision matrix is Platform-scoped and targets reserved tenant `system`, while AD-30 admits a tenant-scoped `User` holding Release Operator as the sole recorder exception (`ARCHITECTURE-SPINE.md:310,316,410`; `launch-readiness-register.md:217-218`). PRD says every role except Platform Operator is tenant-scoped, but assigns planning-decision recording Platform scope (`prd.md:500,524`). The signed package prevents the recorder from inventing an outcome, so this is not a Critical integrity hole, but teams can still allow any tenant's Release Operator, require a special governance tenant, or reject every User-to-system target. Define the authoritative tenant/role assignment from which recorder authority is read and its audit attribution; do not substitute Platform Operator approval.

## Recheck Of v7 Critical/High Findings

| v7 finding | v8 disposition |
| --- | --- |
| C-1 committed-export hold omission | **Closed as written:** hold prepare/release, decision applicability, store dependency, exact frozen artifact set, and pin/unpin acknowledgements are now wired. v8 C-2 is a deeper physical-expiry linearization gap, not the former omission. |
| H-1 Platform actor required to be a Party | **Closed in the spine/PRD:** Platform uses Tenants global-administrator evidence and stable `AuthenticatedHumanActorId` without an invented Party. v8 H-1 is the separate unauthorized Administrator role accidentally added to Story 8.1. |
| H-2 incomplete `EXT-PARTIES-1` consumers | **Closed:** the register lists Stories 5.2, 5.4, 6.6, 7.1–7.5, 8.1–8.3, and 8.8 plus `RQ-1`. |
| H-3 deferred PRD decisions not materialized | **Closed:** OQ-18/OQ-23/OQ-31 have stable runtime decision ids, versions, scopes, and initially Open records. |
| H-4 rate preparation may commit after reset | **Closed:** the timeout is strictly below both windows and either reset before both acks drives abort. |
| H-5 safety enumeration/tenant handshake | **Closed in AD-20/register:** Agents provision events and `safety-verdict-directory` provide finite manifests; `ProvisionHexa` and unindexed Conversations have explicit conditional initialization. v8 H-3 is the remaining contradictory story barrier. |
| H-6 hold/deletion OD omitted from hold command | **Closed:** the OD applies to hold acceptance and recorded recovery when `DeletionArmed` overlaps, without selecting the Product outcome. |

The authoritative validation's original C-1..C-3 and H-1..H-12 were also rechecked. Conjunctive safety/no-weaker retry, the shared hold/export/deletion fence, matrix-v4 bootstrap/scope, scheduled approver recheck, three ledger lifetimes, stable human identity, crash-consistent proposal index, dependency split, trusted-envelope replay/rotation, export ownership/signature, Dapr exposure wording, required-versus-current status wording, and sprint-decision tracking now have explicit architecture or debt dispositions. None of those original findings recurs unchanged; the findings above arise from the revised cross-artifact handoff.

## Areas That Converge In v8

- Snapshot and current safety decisions are conjunctive; retry-time failure preserves the immutable budget disposition and cannot revive an attempt.
- Rate, original-caller open-concurrency, and original-month monetary ledgers have distinct ownership and lifetimes; preparation deadlines precede both rolling resets.
- Matrix-v4 EventStore self-bootstrap, gate-free containment intent, replay first-seen registration, security-spool recording, exact scope, and missing-variant behavior are closed. H-7 concerns the missing owner/story for one direct fact, not the matrix's fail-closed evaluation rule.
- Trusted-envelope logical identity, delivery nonce, canonical authentication, replay-first ordering, retention, rotation, emergency revocation, ACL-confined registrar, and caller-independent security routing converge.
- Proposal-index outbox/high-water reconciliation and repeated removal scan converge under crash and lost acknowledgement.
- Export abort inventory, cleanup receipts, fence retention, and no-key-before-commit converge. C-2 concerns committed-artifact expiry racing a later hold, not uncommitted-export cleanup.
- Human actor evidence converges across Party-bearing User, Party-free Administrator, and Platform principals; operation-specific role grants must still stay closed as H-1 requires.

## Architecture Defects Versus Delivery Debt

The two Critical and seven High findings are cross-artifact architecture/handoff defects. C-1 additionally exposes existing implementation debt: the checked-in approval orchestrator posts before EventStore dispatch and the current policy emits all lifecycle events together. That code must be refactored after the contract is corrected; pointing to current behavior cannot close the architecture defect.

Current repository reality otherwise remains consistent with the existing debt table. The root commit is `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`. Its authoritative Builds gitlink is `a32cb422749352cce8dec948aa3e78c8f00eb4cf`, whose package catalog pins Dapr Client/ASP.NET/Workflow `1.18.5`; the clean Builds submodule worktree is checked out at non-authoritative `cf52f74c983b` and exposes `1.18.7`. Conversations, EventStore, FrontComposer, and Memories are likewise clean worktrees checked out at commits different from their parent gitlinks; Parties and Tenants match. No nested submodule was initialized or mutated during review.

Agents consumes EventStore Client/DomainService, and the parent-authoritative EventStore projects reference Dapr Client/ASP.NET; Agents has no direct Dapr Workflow project reference. The repository still lacks the three ledgers, trusted replay/decision/security aggregates, safety epoch/index, governance fence/export/deletion implementation, and required public vocabulary. Those are already assigned delivery debt and add no findings. The Dapr `1.18.5` decision, dirty-gitlink reality, and sprint 5.1/5.2 evidence discrepancy remain Open/tracking debt exactly as recorded; this review claims no build, integration, or release success.

## Required Closure Order

1. Correct the durable-before-post protocol in C-1 and the physical expiry/hold linearization in C-2.
2. Remove the unauthorized hold-release role and make recorded-branch recovery immune to later decision-union blockers (H-1/H-2).
3. Reconcile the finite safety barrier and durable deletion deferral (H-3/H-4).
4. Surface the PRD armed-deletion choice for Product discussion; do not infer it (H-5).
5. Correct abandon gating and bind the emergency-incident/story owner (H-6/H-7).
6. Tighten the two Medium handoff ambiguities, re-distill, lint, and rerun the full reviewer gate.
