---
name: Hexalith Agents adversarial-divergence review v4
type: architecture-review
lens: adversarial-divergence
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
verdict: fail
critical: 1
high: 4
medium: 1
low: 0
lint_ok: true
---

# Adversarial Divergence Review — v4

## Verdict

**FAIL — the revised spine closes the validation report's original safety, identity, dependency-split, rate-admission, and recheck defects, but one irreversible export/deletion race and four High convergence gaps still let independently built units produce incompatible outcomes.**

This is a fresh downstream-team review of the frozen 2026-09-12 tree, not a closure checklist over v3. The deterministic spine linter passes with zero findings. The reviewed file hashes were:

- `ARCHITECTURE-SPINE.md`: `fbbf7a6cc33a40ad08735c6025ab2bc1d0aea21eec459168fa25caf9ad9cf82d`
- bound `prd.md`: `c24f560346598799fc759a29882d7e4d63e7306204f53a4f0ed6f8a10177d2c1`
- `external-dependency-register.md`: `86c0b1eaada959610efe763e35619c6aa941bf2e54717037db44115c30d044e0`
- `launch-readiness-register.md`: `20c8cd3f4e93cf1ca137c1fe37d2c792`

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 4 |
| Medium | 1 |
| Low | 0 |

## Critical

### C-1 — Export creation is not serialized with the hold/deletion fence, so a frozen deletion set can become incomplete

**Evidence.** AD-22 freezes a deletion's interaction-and-export set at a checkpoint, reserves every then-known export artifact before `DeletionArmed`, and later requires a receipt from **every matching** artifact before completion (`ARCHITECTURE-SPINE.md:322`). The same rule gives only `LegalHold` and `ProtectedDeletion` contention on `ProtectionFence`; `ExportRequest` reads policy/hold state and writes through the export port, but it neither acquires the fence nor rechecks an overlapping deletion intent. The projection inventory likewise uses the export index for frozen-set resolution but defines no creation fence (`launch-readiness-register.md:323-333`). `OD-EXPORT-LIFECYCLE-1` covers lifetime, later-hold treatment, restore, and provider, not export creation during an armed deletion (`ARCHITECTURE-SPINE.md:904`; `launch-readiness-register.md:96`).

**Two compliant units.** Team A freezes `{interaction I, exports E0}`, arms deletion, then accepts a concurrently authorized export `E1` for `I`; it purges the frozen set and can interpret “every matching” as the checkpoint-bounded set. Team B re-enumerates at completion and refuses to complete while `E1` exists. Both follow the literal frozen-set and export rules, but one can report deletion complete while an encrypted, still-decryptable copy remains and the other can be starved by continuing exports.

**Impact.** This defeats the restrictive all-copies deletion guarantee and can leave retained readable data after irreversible DEK destruction.

**Disposition — AUTOFIX architecture, DISCUSS only the user-visible policy.** Serialize every export creation that contains a protected resource through the same `ProtectionFence`, or bind an equivalent fence token/high-water protocol. At minimum, an overlapping preparing/armed/started deletion must fail `ExportRequest` closed or durably join the artifact to the deletion before creation becomes visible. Extend `OD-EXPORT-LIFECYCLE-1` to decide export-under-existing-hold behavior; do not infer it from the store adapter.

## High

### H-1 — Matrix-v4 `ProvisionHexa` rejects the exact replay that AD-2, AD-30, and FR-1 require

**Evidence.** Matrix v4 says every direct precondition must pass and gives `AgentSetupMutation:ProvisionHexa` the closed precondition `HexaIdentityAbsent` (`launch-readiness-register.md:188-198`). AD-2 and AD-30 require an exact repeated provision to be an idempotent no-op; PRD FR-1 requires a repeat to return the existing Agent and complete a recoverable partial provisioning case (`ARCHITECTURE-SPINE.md:156,376`; `prd.md:131-137`).

**Two compliant units.** A matrix-first gateway observes an existing exact identity, fails `HexaIdentityAbsent`, and never reaches aggregate idempotency. An aggregate-first gateway recognizes the exact replay and returns the existing result. A lost first response therefore either recovers or becomes a permanent bootstrap failure.

**Impact.** Tenant enablement and provisioning recovery cannot share one deterministic result.

**Disposition — AUTOFIX.** Replace the precondition with a discriminated authoritative result such as `Absent | ExactProvisionReplay | DivergentExisting`, make the first two admissible and the last blocking, and add lost-response plus partial-orchestration fixtures.

### H-2 — The umbrella audit gate and gate-level open-decision references overblock unrelated setup and runtime operations

**Evidence.** An open decision is emitted whenever `AffectedEvaluations` intersects any evaluated gate (`launch-readiness-register.md:72-78`). Both open decisions name `LR-AUDIT-PROTECTION-DELETION` (`:95-96`), while that umbrella gate appears in Agent setup, calls, posting, proposal resolution, tenant budget changes, tenant Provider enablement, policy publication, kill-switch, and inspection rows (`:165-186,198-204`). The decision rows simultaneously say hold creation and deletion-request recording remain callable, and the spine limits their safe states to destruction/export/deletion completion and `RQ-1` (`ARCHITECTURE-SPINE.md:903-904`). Independently, `AgentSetupMutation:ConfigureExisting` retains the umbrella gate, although PRD FR-34 expressly permits `hexa` configuration before payload protection is `Available` (`prd.md:772-785`).

**Two compliant units.** A generic matrix evaluator propagates either open OD through `LR-AUDIT-PROTECTION-DELETION` and blocks almost every family, including hold creation and pre-protection Agent configuration. A decision-aware evaluator applies only the named internal `ProtectedDeletion`/export operations and permits the safe operations the decision records promise. Both consume the normative register literally because `AffectedEvaluations` includes both the umbrella GateId and the narrower operations.

**Impact.** The supposedly narrow fail-closed open items can deadlock unrelated configuration, containment, proposal, and call paths; implementers must invent a local exception to match the spine and PRD.

**Disposition — AUTOFIX.** Do not use the umbrella GateId as an operation-level `AffectedEvaluation`. Bind each OD to exact concrete variants (`ProtectedDeletion:DestructionStarted`, `ProtectedDeletion:Complete`, export operations, and `RQ-1`) and split matrix variants where needed. Give non-content-bearing `ProvisionHexa`, `ConfigureExisting`, budget initialization/change, Provider enable/disable, hold creation, and deletion-request recording gate sets consistent with FR-34 and the stated safe states, while retaining all unrelated security/tenant gates.

### H-3 — The original-caller concurrency lease has no prepare/decision/recovery linearization

**Evidence.** AD-21 now gives the two rate ledgers a complete prepare/commit-or-abort decision protocol, but `OpenInteractionLedger` is only “prepared during call acceptance and committed with `AgentCallAccepted`”; it does not say whether a prepared lease counts against the bound, whether or when it expires, which interaction event is the mutually exclusive decision authority, or how a crash between either stream append recovers (`ARCHITECTURE-SPINE.md:316`). PRD FR-32 requires the per-Party concurrent-nonterminal bound to be enforced at acceptance (`prd.md:766-767`), and `LR-COST` asks for rollover/recovery evidence without supplying the missing protocol (`launch-readiness-register.md:318,351`).

**Two compliant units.** Team A counts only committed leases, allowing two concurrent calls to prepare against one remaining slot and both later append `AgentCallAccepted`. Team B counts preparations, but gives them an autonomous deadline; a crash after acceptance and before ledger commit can expire the preparation and admit another call while the first remains nonterminal. A third implementation retains abandoned preparations forever and wedges the caller.

**Impact.** The same traffic can exceed the denial-of-wallet concurrency control or permanently deny a caller.

**Disposition — AUTOFIX.** Bind a deterministic open-lease prepare identity; state that every unresolved preparation reserves capacity; record mutually exclusive accept/abort decisions on `AgentInteraction`; require decision revision on ledger commit/abort; forbid autonomous expiry; and define bounded recovery and release failure-injection cases, mirroring the now-complete rate-admission protocol.

### H-4 — Trusted-envelope replay detection has no durable owner, and `PrincipalIdentity` is not a canonical value

**Evidence.** AD-30 requires cross-delivery nonce reuse detection for the full replay-retention horizon, including rotation and emergency revocation, but AD-2 names no replay-ledger aggregate and AD-30 names no shared durable replay-store owner, partition key, write linearization, or behavior when that store is unavailable (`ARCHITECTURE-SPINE.md:156,370-376`). `EXT-SECRETS-1` fixes `ReplayRecordRetention` and asks for retained records, but describes secret custody rather than an authoritative nonce-state contract (`external-dependency-register.md:185-188`). In addition, AD-29 derives `LogicalCommandId` using undefined `PrincipalIdentity`, although `User` has both `PartyId` and `AuthenticatedHumanActorId` and `Workflow` has several identity components (`ARCHITECTURE-SPINE.md:370,376`).

**Two compliant units.** One host keeps nonce state in a process cache and derives a User logical id from `PartyId`; another uses a shared durable store and derives it from the human actor id plus Party id. Across replicas or restart the first accepts a changed-field nonce reuse the second rejects, while exact redispatches produce different logical ids across implementations.

**Impact.** Replay/rotation behavior and idempotency are not portable across hosts, and a security invariant silently degrades on outage or scale-out.

**Disposition — AUTOFIX.** Name one platform-owned durable, tenant-partitioned replay-record port/aggregate, its compare-and-create atomic key (`issuer`, `audience`, signing-key version, `DeliveryNonce`), stored canonical digest/tag, retention clock, and fail-closed unavailable behavior; bind it in `EXT-SECRETS-1`/`EXT-HOST-1` and cross-replica/restart tests. Replace `PrincipalIdentity` with a closed per-principal component tuple matching AD-30 and test each principal kind.

## Medium

### M-1 — New-month admission can either double-count or ignore prior-period outstanding obligations

**Evidence.** AD-21 correctly keeps reservation, settlement, and period close in the original UTC-month stream, but adds that a new-period admission reads prior settled/outstanding obligations “required by the configured cap policy” without defining such a policy field or its calculation (`ARCHITECTURE-SPINE.md:316`). PRD FR-28 says no reservation migrates between months and charges remain against the reserving month (`prd.md:733-738`).

**Divergence.** One budget unit includes prior-month unresolved obligations in the new month's 100% check; another evaluates only the new month. Both preserve original-stream accounting, but identical admissions differ.

**Disposition — DISCUSS, then amend AD-21 or Deferred.** Product/Architecture must either state that monthly caps are period-isolated and remove the phrase, or add a named cross-period exposure policy with a formula, owner, version, and UI/audit fields.

## Reassessed Areas That Now Converge

- **Conjunctive safety and retry:** AD-20 now requires snapshot-plus-current policy outcomes, formally bounded dominance, permanent attempt cancellation on failure, and matching `EXT-SAFETY-1` evidence. No Critical/High divergence remains there.
- **Hold/deletion core fence:** preparation, `DeletionArmed`, an explicit pre-destruction open decision, idempotent receipts, and all-or-nothing deferral now prevent the original partial-destruction race. C-1 is the remaining export-creation boundary, not a rejection of that repaired core.
- **Matrix scope/bootstrap shape:** platform versus tenant scope, direct-precondition result schema, unrelated-gate fixtures, and Provider/budget bootstrap variants are explicit. H-1/H-2 are the remaining literal conflicts.
- **Scheduled approver recheck:** ownership, cadence, evidence freshness, two consecutive authoritative-empty passes, interruption reset, and state-specific outcomes are now deterministic.
- **Distributed safety rescan:** EventStore epoch/index ownership, fenced coordinator, bounded profile, crash recovery, per-Conversation high-water, and exact `RescanPending` behavior converge.
- **Human approvers and separation identity:** human-only/liveness resolution, stable `AuthenticatedHumanActorId`, historical binding versions, and subject-set comparisons align across spine, PRD, Parties dependency, and readiness evidence.
- **Rate and budget owners:** the distinct aggregate lifetimes and the revised two-scope rate decision protocol converge. H-3 is specifically the still-shorter open-interaction prepare protocol; M-1 is a cross-period policy ambiguity.
- **Proposal index:** interaction truth, same-append source-revision outbox, idempotent high-water application, removal checkpoint/reconciliation/drain loop, and failure-injection duties now prevent the original crash gap.
- **Dependency split:** six core Conversations seams and optional `EXT-CONV-RETRACTION-1` are consistent in the spine, PRD, and dependency register.
- **Export fail-closed policy:** export request/download and deletion completion remain closed until the versioned lifecycle decision and `EXT-EXPORT-STORE-1` are approved/available. C-1 concerns the protocol after that policy becomes active.

## Current-Reality Classification

The parent-authoritative gitlinks remain Builds `a32cb422`, Conversations `73bcee6f`, EventStore `ce9e779a`, Parties `fa423985`, Tenants `2fac1839`, and FrontComposer `053b2008`. The committed Builds catalog pins `Dapr.Client`, `Dapr.AspNetCore`, and `Dapr.Workflow` `1.18.5`; the dirty Builds checkout is `fa647278` and pins `1.18.7`. EventStore Client/DomainService already reference Dapr Client/ASP.NET, while Agents has no Workflow reference. The spine's present-transitive-versus-future-Workflow distinction and `ARCH-A-15` are therefore accurate; the dirty checkout is not architecture authority.

Current source still lacks the safety epoch/index, three separate ledgers, proposal outbox/index reconciliation, stable human actor/envelope MAC/replay state, and hold/export/deletion fence. Existing contracts still collapse rescan to `ContextUnavailable`. These are the explicitly assigned delivery debts at `ARCHITECTURE-SPINE.md:910-924`, not additional findings and not evidence that an AD should be weakened.

## Required Closure Order

1. Close **C-1** before approving the export-lifecycle decision or allowing any destruction path.
2. Fix **H-1/H-2** together in matrix v4 and its open-decision evaluator fixtures.
3. Complete **H-3** using the rate-admission decision pattern already adopted.
4. Complete **H-4** before trusted envelopes are treated as cross-replica authorization evidence.
5. Record M-1 as an explicit Product/Architecture policy or period-isolated rule, then rerun lint and the full reviewer gate.
