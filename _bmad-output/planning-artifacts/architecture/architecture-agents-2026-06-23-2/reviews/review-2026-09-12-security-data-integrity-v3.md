---
name: Hexalith Agents security and data-integrity review v3
type: architecture-spine-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
intent: frozen-read-only-specialist-review
reviewer: security / data-integrity lens
lint_ok: true
---

# Security And Data-Integrity Review — 2026-09-12 v3

## Verdict

**FAIL — 1 Critical, 1 High, 1 Medium, 0 Low.** The revised architecture materially closes the authoritative 2026-09-12 safety, common-fence, actor-identity, trusted-envelope, source-delivery, export-custody, and deletion-completeness defects. It does not yet make the new Conversation-deletion barrier an executable security cutover: permit creation and barrier installation serialize on `ConversationAgentState`, but already-permitted interaction effects only re-read that different aggregate before mutating `AgentInteraction` or invoking a Provider. That cross-stream check has a time-of-check/time-of-use window in which a Provider effect can begin after the barrier. The export path also lacks a durable authorize/effect/result and lost-ack protocol for the direct custodian key-delivery step.

PASS requires zero Critical and zero High, so this frozen revision does not pass.

## Frozen Inputs, Scope, And Method

This reviewer applied the BMad Architecture reviewer-gate security/data-integrity lens to the full current spine, implementation convention, authoritative validation report, bound PRD, active epics, both registers, current memlog, repository instructions, and focused repository contracts. The review traced deletion from source approval through durable delivery, acknowledgement, barrier installation, interaction convergence, frozen-set construction, fence preparation, irreversible erasure, every-copy receipts, tombstone completion, export commit/key release, and crash/lost-ack recovery. It separately checked trusted-envelope admission, denial recording, human actor evidence, recorder bootstrap, tenant routing, protected-content boundaries, deterministic identities, and current implementation debt.

The requested report was absent at review start. The following hashes were captured before analysis and matched the supplied frozen values:

| Frozen input | SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `8d122b1b8318edc92a40d291e4e9adf32bf22b125a16f73d594435818c64c026` |
| `IMPLEMENTATION-CONVENTIONS.md` | `c7464254b343cfd0caffcbbefe9224e1c518a3affe97e886f0fa1fcacb41dbc9` |
| bound `prd.md` | `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb` |
| `epics.md` | `3f5a9afc605a9352a5c43e505cce9c56fd347ea2c6f476ddf77be3c4729989ec` |
| `external-dependency-register.md` | `577a102a86b4b55af7b971789087594c1008d3231f45509c0ac690921474af63` |
| `launch-readiness-register.md` | `a864a07bbb31a627b24b2ae593cb3f84f404b5bde63b878f635d327d42107420` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

Additional reviewed context was `.memlog.md` at `fd9d213f4eb7deed29a78648cfcaab1120a2ffb10211f7fcf3355307cd8f0c0e`, repository instructions at `bfbe399b567d852a74c7ee8c9217217a3ea5eb80ca591b5b8ac01acfbe838966`, and parent repository commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`.

## Deterministic Linter

Command:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: **PASS** — `ok: true`, `total_findings: 0`, and no severity entries. Mechanical validity does not close the semantic findings below.

## Critical

### C-SDI3-1 — The Conversation-deletion barrier does not linearize already-permitted interaction effects

**Classification:** architecture security/data-integrity defect; not implementation debt and not an unresolved Product choice.

**Evidence**

- AD-7 makes only `AgentCallAcceptance:RegisterInteractionPermit` and `ConversationDeletionPropagation:InstallBarrier` contend at one `ConversationAgentState` expected revision. A winning permit then creates `AgentInteraction` and emits a separate workflow-start outbox; the document promises that a post-barrier request creates no workflow or Provider content (`ARCHITECTURE-SPINE.md:233`).
- For existing permitted interactions, AD-6 says invocation/result recording, regeneration, edit, approval, retry, and `BeginPosting` merely “re-read the barrier” before their content append or effect (`ARCHITECTURE-SPINE.md:221`). Those transitions are owned by `AgentInteraction`, the capacity allocator, or an external Provider/Conversations seam, not by the `ConversationAgentState` revision that the barrier changes. No atomic compare, consumable effect permit, lease, or common-fence acknowledgement joins the check to the later append/effect.
- The operation matrix confirms the split. `RegisterInteractionPermit` and `InstallBarrier` each have the Conversation-owner expected revision (`launch-readiness-register.md:231,245`), while normal `ProviderInvocation`, posting, edit, regeneration, and resolution rows contain no barrier revision/lease. Only the deletion workflow's later `ConvergeInteraction` row checks `BarrierStillEffective` while targeting the interaction (`launch-readiness-register.md:178-188,246`).
- The implementation convention nevertheless requires proof that “no post-barrier interaction/workflow/provider effect starts” (`IMPLEMENTATION-CONVENTIONS.md:32`). It defines no primitive that could make that assertion true across these owners. It also requires every pre-effect authorization to be durably recorded before I/O (`IMPLEMENTATION-CONVENTIONS.md:8-15`).
- The bound Story 8.3 repeats the same non-atomic “all re-read the barrier” rule and the no-post-barrier claim (`epics.md:2961-2965`). The bound interaction story simultaneously requires every pre-barrier permit's already-durable workflow-start outbox to be delivered (`epics.md:1886-1888`), but does not define whether or how that queued start is suppressed, consumed by deletion convergence, or started in a deletion-only mode after the barrier.
- AD-7 and AD-13 additionally disagree about the acceptance cut: AD-7 places membership and protection before the directory permit and rate/open/budget/capacity after it (`ARCHITECTURE-SPINE.md:229,233`); AD-13 still says the FR-8 acceptance sequence performs rolling-rate/open admission and cost reservation before the final membership step and only then records `AgentCallAccepted` (`ARCHITECTURE-SPINE.md:301`). This does not create the race by itself, but leaves builders with incompatible locations for the barrier-sensitive cutover.

**Failure scenario**

1. A pre-deletion interaction and workflow exist. Its Provider step reads `ConversationAgentState` revision `R` and observes no barrier.
2. Source deletion installs `ConversationDeletionBarrier` at `R+1`; the barrier workflow begins convergence.
3. The interaction workflow conditionally appends `ProviderInvocationAuthorized` to its still-current `AgentInteraction` revision, obtains/uses its independent allocator fence, and calls the Provider. All local expected-revision checks can succeed because none compares against `R+1` atomically.
4. The deletion workflow can later discard the returned Provider payload and erase Agents-owned copies, but it cannot retract the post-barrier disclosure or processing at the Provider. A similar race exists for regeneration/edit/approval/posting authorization, and a queued workflow-start outbox can be delivered after the barrier with no defined deletion-only consumption rule.

The source signal, barrier, and deletion workflow are durable, so eventual Agents-side erasure can still complete. That does not repair the architecture's claimed security cutover or the external disclosure after source-approved deletion.

**Required correction — architecture protocol, no Product outcome to invent.** Bind one executable cutover across every content-producing or external-effect transition for a permitted interaction. For example, use a Conversation-owned, expected-revision effect permit/lease whose acquisition or consumption contends with barrier installation, explicitly track outstanding pre-barrier effects in the barrier manifest, and define when the barrier becomes effective versus when those effects quiesce. Alternatively introduce an equivalently linearizable per-interaction stop fence whose complete acknowledgement is a prerequisite to the effective barrier. Whichever protocol is selected must define the disposition of already-durable creation/workflow-start outboxes, make Provider/posting authorization impossible after the effective cut, and add failure injection for barrier installation immediately before and after effect authorization, `BeginInvocation`, workflow start, result append, and posting begin. Reconcile AD-7/AD-13 and add the exact matrix preconditions/owner revisions; a projection read or unbound re-read is insufficient.

## High

### H-SDI3-1 — Export key delivery has no durable authorization, idempotency, outcome lookup, or lost-ack recovery protocol

**Classification:** architecture security/audit-integrity defect; it does not require selection of the open export provider or lifecycle outcome.

**Evidence**

- AD-22 correctly makes the `ProtectionFence` commit the only export commit decision, requires the secondary `AuditExport` acknowledgement before key delivery, and keeps the key out of Agents surfaces. It then jumps directly to “the custodian delivers” without naming a durable key-delivery authorization/result, deterministic delivery identity, idempotent custodian operation, or outcome lookup (`ARCHITECTURE-SPINE.md:381`).
- Story 8.2 has the same gap: after `ExportCommitted`, an approved and unexpired request causes `EXT-SECRETS-1` to deliver directly, with no durable before/effect/after protocol or recovery criterion (`epics.md:2881-2889`). Its earlier sentence that prepare-time versions phase-pin key delivery does not establish who records or verifies a particular delivery attempt (`epics.md:2866-2869`).
- `ExportDownload` is only a legacy matrix family with the `ExportRequest` gate set; matrix v4 defines no concrete authorize/deliver/result/recovery variants or direct preconditions binding the export revision, requester actor, exact lifecycle/store version, expiry, and delivery id (`launch-readiness-register.md:191`). `GovernanceProtection:ExportCommitRecovery` ends at “enables custodian key delivery” (`launch-readiness-register.md:243`).
- `EXT-SECRETS-1` owns envelope-key custody and generic wrap/unwrap/pin/unpin/destroy operations, but its required artifact and compatibility contract do not promise an idempotent principal-bound delivery operation or delivery-outcome lookup (`external-dependency-register.md:187-190`). `EXT-EXPORT-STORE-1` likewise says only that the commit acknowledgement enables delivery (`external-dependency-register.md:201,204`).
- The normative command convention requires prior durable authorization before an external effect and a later result command to record the observed outcome (`IMPLEMENTATION-CONVENTIONS.md:8-15`). Key release is the highest-sensitivity external effect in the export path, yet it has no matching protocol.

**Failure scenario**

The custodian releases the unwrapped/export envelope key to the approved Inspector, then the response or Agents acknowledgement is lost. A retry has no stable delivery id or authoritative lookup. One compliant implementation delivers again; another treats the uncertain response as failure; a third records success from request intent. If the retry crosses expiry, actor-role revocation, or lifecycle-version change, the artifacts give no common rule for whether the original authorization remains consumable. The audit trail cannot prove whether or when the concentrated export became decryptable, and a client can receive inconsistent download/key status despite all current commit invariants holding.

**Required correction — AUTOFIX the protocol without selecting Product policy.** Add a durable `ExportKeyDeliveryAuthorized` (or equivalent) decision before the custodian call, bound to deterministic `DeliveryId`, `ExportId`, exact `ExportCommitAcknowledged` revision, frozen requester `AuthenticatedHumanActorId`, effective lifecycle/store/contract versions, audience/purpose, and exclusive expiry. Require the custodian contract to deliver idempotently or expose authenticated exact-outcome lookup by that identity; append a safe success/failure/indeterminate result afterward and resolve lost acknowledgement from the custodian before retry. Give `ExportDownload` concrete initial, result, and recovery matrix variants. Keep the key itself outside Agents state and surfaces. If the eventual provider cannot offer idempotent delivery or lookup, that provider cannot satisfy `EXT-SECRETS-1`; do not invent a permissive fallback.

## Medium

### M-SDI3-1 — Story 8.2 names export “completed” before the architecture's commit point

Story 8.2 says the `export` projection “reaches completed” after artifact and manifest verification (`epics.md:2876-2879`), then later says it becomes `ExportCommitted` only after the fence decision and `AuditExport` acknowledgement (`epics.md:2881-2884`). AD-22 and the register clearly make the latter the authoritative commit boundary, so this is not currently an irreversible architecture hole. It can still lead a UI/projection builder to emit a terminal-looking success before the sole commit decision. Replace the earlier “completed” with an explicitly non-success prepared/materialized status and state that only `ExportCommitted` is terminal success. This is a source-reconciliation correction, not new implementation work.

## Low

None.

## Authoritative Critical/High Closure Audit

| Authoritative finding | Specialist disposition on the frozen revision |
| --- | --- |
| C-1 weaker safety evaluation | **Closed.** AD-20 requires conjunctive snapshot-plus-current checks and only a tested dominance equivalence may collapse them. |
| C-2 hold/deletion exclusion | **Closed for the common tenant fence.** Frozen sets, common `ProtectionFence`, prepare/arm/destruction separation, export cleanup/commit high-water, and exact receipts prevent the original partial irreversible hold/deletion race. C-SDI3-1 is a distinct Conversation-source barrier/effect race introduced by the new cross-stream cutover. |
| C-3 bootstrap/matrix deadlock | **Closed.** Matrix v4 has target-aware bootstrap/repair, direct owner prerequisites, EventStore self-bootstrap, and gate-free but evidence-bound containment. |
| H-1 scheduled Approver re-check | **Closed.** Durable single-flight, two-pass empty evidence, state-specific behavior, and unavailable retry are bound. |
| H-2 safety-rescan ownership | **Closed.** EventStore owners, finite manifests, fenced coordination, bounded profiles, conditional initialization, and `RescanPending` are explicit. |
| H-3 human-only Approvers | **Closed.** Parties-owned human/liveness classification and historical actor binding fail closed. |
| H-4 conflated ledgers | **Closed architecturally.** Rolling rate, open interaction, and monetary reservation have separate owners/lifetimes; their runtime implementation remains assigned debt. |
| H-5 missing human identity | **Closed.** Every human principal carries stable `AuthenticatedHumanActorId`; tagged User/Administrator/Platform evidence is compared without inventing Party bindings. |
| H-6 crash-inconsistent proposal index | **Closed.** Interaction truth, source-revision outbox, high-water, checkpoint reconciliation, and lost-ack recovery are explicit. |
| H-7 indivisible Conversations dependency | **Closed.** Six core seams and optional retraction have separate records and consumers. |
| H-8 trusted-envelope integrity/replay/key lifecycle | **Closed.** Canonical authenticated bytes, logical/delivery identities, reserved-system first-seen owner, rotation/revocation, ACL confinement, and durable denial recording are bound. |
| H-9 export custody/lifecycle/signature | **Partially closed; H-SDI3-1 remains.** Artifact owner, AEAD, immutable identity, signed canonical manifest, index, fence, hold/expiry/restore policy blockers, cleanup, and all-copy purge receipts are bound. The direct key-delivery effect still lacks its own durable/lost-ack contract. |
| H-10 current Dapr exposure | **Closed in architecture wording.** Current transitive Client/ASP.NET exposure is distinguished from future Workflow delivery. |
| H-11 public parity overstated | **Closed.** Target vocabulary is labeled required completion; absent code is assigned debt. |
| H-12 sprint tracking contradiction | **Closed as an architecture classification.** The unresolved delivery-history decision blocks dependent work without rewriting history. |

## Specialist Boundary Results

| Boundary | Result |
| --- | --- |
| Durable Conversations deletion source and acknowledgement | **Pass.** Source-atomic publication, stable logical id/revision, retained ordered backfill, retry through refusal/outage/rollover, poison no-skip, target durable-before-ack, exact replay, and changed-payload conflict are aligned across AD-6, `EXT-CONV-AI-1`, matrix, and Story 8.3. |
| Deletion origin authentication and scope | **Pass.** `ConversationApprovedDeletion` is a closed origin with authenticated source evidence and no human fields; its Workflow principal is target-limited, cannot expand scope, impersonate humans, cancel, Abort, or choose policy. Operator deletion retains distinct requester/Inspector evidence. |
| Permit/barrier serialization and fixed-point completeness | **Fail at C-SDI3-1.** Permit creation versus barrier is safe and manifest-complete; already-permitted effect authorization is not linearized with the effective cut. |
| Protection, erasure, deletion receipts, and restore | **Pass subject to the Critical cutover.** Shared interaction DEK, typed `Erased`, inherited snapshot/cache protection, fixed inventories, prepare/arm/destruction separation, named projection/workflow/artifact/backup receipts, and no-copy export-free completion are coherent. Posted Conversations copies remain an explicit, gated residual risk rather than a false Agents-owned claim. |
| Export prepare/commit/cleanup and hold coupling | **Pass through commit and cleanup.** The fence is sole commit owner, committed-export high-water survives missing secondary acknowledgement, pre-commit abort requires exhaustive no-write/purge/key-destruction proof, expiry/hold control retains on uncertainty, and recovery is phase-pinned. Key release fails separately at H-SDI3-1. |
| Human separation of duties | **Pass.** Stable actor identity, source-specific evidence union, subject-set computation, same-actor rejection, anti-collusion, and recorder/approver/custodian separation are explicit and fail closed. |
| Decision-recorder bootstrap | **Pass as deliberately unresolved.** Only an independently signed Product + Release PM + Governance resolution under the proposed authority source can bootstrap the exact recorder-scope decision. The operation cannot select the source, publish a catalog, or approve its own package. |
| Cross-tenant and protected-content leakage controls | **Pass except C-SDI3-1.** Tenant is present in aggregate/deterministic/idempotency keys; absent-key equivalence, authenticated source target, caller-independent security routing, sealed broker/projection/workflow boundaries, provider-body instrumentation exclusion, and no-secret surfaces are bound. C-SDI3-1 permits an external post-barrier Provider disclosure. |
| Trusted replay, security observation durability, and lost acknowledgements | **Pass.** First-seen registration precedes target idempotency, changed delivery evidence conflicts, denial recording is durably spooled before processed response, ACLs are capability-limited, and exact-id lookup resolves EventStore acknowledgements. |

## Unresolved Product/Governance Choices — Preserved, Not Selected

No outcome was invented by this review. The security/data-governance choices that must remain blockers are:

- `OD-HOLD-DELETION-PRECEDENCE-1`: only the hold-versus-armed-deletion race and its recovery/user outcome.
- `OD-HOLD-PREPARE-CANCELLATION-1`: only hold prepare cancellation/Abort authority and outcome.
- `OD-EXPORT-LIFECYCLE-1`: storage provider, lifetime, later-hold treatment, and restore behavior; export-free deletion remains independent.
- `OD-PRD-OQ31-INSTRUCTION-PROTECTION-1`: instruction/configuration protection placement; it blocks all Story 8.3 authorization while Open.
- `OD-RELEASE-RECORDER-SCOPE-1`: the Release Operator authority source for reserved-system decision recording; the bootstrap records only an externally made resolution.

The open rate/concurrency, Dapr security, historical-safety, automatic-retraction, and sprint-history decisions retain their separately recorded scopes. Neither C-SDI3-1 nor H-SDI3-1 requires a Product outcome: they require executable ordering, durable authority, and lost-ack mechanics around already-selected behavior.

## Architecture Defects Versus Implementation And Delivery Debt

C-SDI3-1 and H-SDI3-1 are architecture defects because two independently built components can obey every local owner revision yet produce incompatible and security-relevant outcomes. M-SDI3-1 is a planning-source status contradiction.

Focused repository search found no current implementation of `InteractionCreationPermitted`, `ConversationDeletionBarrier`, `ConversationDeletionPropagation`, `ExportCommitDecided`, `ExportCommitAcknowledged`, or a key-delivery contract. The spine and live-seam register already assign these absent targets to Stories 6.1 and 8.1–8.3 and keep the seams `Deferred`; `EXT-CONV-AI-1`, `EXT-SECRETS-1`, `EXT-EXPORT-STORE-1`, and `EXT-PROTECTION-1` remain `Uncommitted`. Those are visible implementation/delivery blockers and do not add to the finding counts. No build, integration, or release-evidence success is claimed.

## Required Correction Order

1. Close C-SDI3-1 by defining the one authoritative deletion cutover for every already-permitted interaction effect and queued creation/workflow-start outbox, then bind it in AD-6/AD-7/AD-12/AD-13, matrix variants, convention tests, and Stories 6.1/8.3.
2. Close H-SDI3-1 with a durable principal/expiry/version-bound key-delivery decision, deterministic delivery identity, idempotent custodian effect or exact outcome lookup, result event, and lost-ack recovery in AD-22, `EXT-SECRETS-1`, matrix, and Story 8.2.
3. Correct Story 8.2's premature “completed” wording without moving the fence commit point.
4. Re-distill, preserve AD ids and open-decision ids/outcomes, lint, freeze/hash, and rerun the complete reviewer gate. PASS requires zero Critical and zero High.

## Gate Conclusion

The frozen security/data-integrity gate is **FAIL: 1 Critical, 1 High, 1 Medium, 0 Low**. The durable source feed, origin separation, common hold/export/deletion fence, complete deletion receipts, trusted replay, recorder bootstrap, and tenant/content boundaries are substantially stronger and internally coherent. The remaining barrier-effect race and unauditable key-delivery effect must be made mechanically enforceable before the spine is a safe implementation handoff.
