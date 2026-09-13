---
name: Hexalith Agents adversarial-divergence review v6
type: architecture-review
lens: adversarial-divergence
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
verdict: fail
critical: 1
high: 8
medium: 2
low: 0
lint_ok: true
---

# Adversarial Divergence Review — v6

## Verdict

**FAIL — the current revision closes every v5 Critical/High at its intended boundary, but one irreversible-recovery deadlock and eight independent cross-contract divergences remain in the complete frozen spine/PRD/epics/register system.**

This is a fresh whole-artifact adversarial pass. It constructs independent downstream units from every literal current contract and does not treat v5 as a closure checklist. The deterministic spine linter reports zero findings. Frozen input hashes before and immediately before writing this review were identical:

- `ARCHITECTURE-SPINE.md`: `e3fc24de16eb03449eaaf3a1b495dfbb97e3e2915f696ba32817bf8fb739ce6a`
- bound `prd.md`: `aeac1bd0bff7abe009e6388d90da67e22bf70b50b04d6805f042241f372d0924`
- `epics.md`: `311b011c762c8482492701c10b0d844cf6747358b08799a702dd596550da796b`
- `external-dependency-register.md`: `89096ef5d57c315dbe87b4bdfda5a76250b7295f890467068168d73c4c4fe6e5`
- `launch-readiness-register.md`: `f49a2450994bed0621ab2c78ecc425ec04f4282bd50c927922dac436f51de644`
- `VALIDATION-REPORT-2026-09-12.md`: `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 8 |
| Medium | 2 |
| Low | 0 |

## Critical

### C-1 — Recovery of a recorded irreversible governance branch can be blocked by mutable readiness state

**Evidence.** AD-23 requires every recorded governance decision to converge after failure without duplicate or lost destruction/purge effects (`ARCHITECTURE-SPINE.md:334-340`). Matrix v4 instead combines initial execution and recovery in the same `GovernanceProtection:*OrRecover` rows and requires current `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, `LR-SECRETS`, and `LR-AUDIT-PROTECTION-DELETION` for export cleanup, hold recovery, deletion purge after `DestructionStarted`, and deletion completion (`launch-readiness-register.md:212-220`). AD-17 says a recovery dispatch named by a recorded decision still evaluates current matrix inputs (`launch-readiness-register.md:194`). A partial deletion/export/protection failure is itself allowed to invalidate the audit/protection gate, while a topology or tenant-access observation may become stale during the outage being recovered.

**Two literal units.** Team A re-evaluates every listed gate and wedges after the first irreversible DEK destruction because the newest audit/protection observation is now blocking or stale; it cannot record remaining receipts or complete recovery. Team B treats the already-recorded `DestructionStarted`/cleanup decision as authority, performs minimal direct dependency and expected-revision checks, and completes the frozen branch despite the failed gate. Both implement one half of the normative contracts, but Team A leaves a permanently split destructive result and Team B uses an unregistered recovery exception.

**Impact.** A crash or gate invalidation after the first irreversible effect can prevent the system from reaching either restrictive completion or a reversible safe state, contradicting NFR-11 and the deletion all-copies guarantee.

**Disposition — AUTOFIX.** Split initial authorization from recovery. Initial prepare/start rows retain normal gates and open-decision checks; once an immutable decision is recorded, dedicated recovery variants use only the frozen decision/policy versions, exact tenant/resource/fence revisions, least-privilege Workflow capability, and direct availability/outcome lookup for dependencies needed to finish that same branch. They must never select a new outcome, but mutable readiness observations must not block recording receipts or cleanup needed to restore a safe state.

## High

### H-1 — Replay equality includes verifier-local time, so an exact duplicate delivery becomes a conflict

**Evidence.** AD-30 promises byte-identical delivery replay may proceed to target idempotency, but the replay record's `FirstSeenAt` is each verifier invocation's `EvaluatedAt`, and an existing record is accepted only when the “retention tuple” also matches (`ARCHITECTURE-SPINE.md:386`). A later exact duplicate necessarily has a later verifier evaluation instant and therefore computes a different `FirstSeenAt`/`RetainUntil`, even though every authenticated envelope byte and tag is identical. Story 5.4 only explicitly names exact lost-ack replay, not a later exact duplicate (`epics.md:1454-1459`).

**Divergence.** One verifier compares only authenticated tenant/logical-id/key/digest fields and reuses the stored first-seen clock. Another follows the literal tuple comparison and rejects every later exact delivery. Client retry therefore either reaches AD-29 idempotency or fails as a security conflict.

**Disposition — AUTOFIX.** On an existing stream, compare only immutable authenticated delivery fields; `FirstSeenAt` and `RetainUntil` are authoritative values of the first record and are never recomputed equality inputs. Add separate exact duplicate, concurrent create, and lost-ack cases.

### H-2 — The matrix's universal idempotency-first rule contradicts the pre-command replay-admission row

**Evidence.** Matrix v4 states without exception that AD-29 target-aggregate idempotency precedes every mutable readiness or direct-precondition read (`launch-readiness-register.md:194`). `TrustedEnvelopeAdmission:RegisterFirstSeen` is itself a direct-precondition row that AD-30 requires before the target command is even constructed, so the target aggregate's AD-29 record cannot be read first (`ARCHITECTURE-SPINE.md:168,386`; `launch-readiness-register.md:211`).

**Divergence.** A generic matrix evaluator checks target idempotency before replay admission and exposes a target read/write path before nonce registration. A security-first evaluator special-cases the registrar and violates the register's explicit “not a local exception” sentence.

**Disposition — AUTOFIX.** State the registrar as the sole named ordering exception: cryptographic/time validation, replay conditional create/read, then target AD-29 idempotency, then every other matrix input. Give the registrar its own exact replay semantics rather than target idempotency.

### H-3 — Pre-authentication and cross-tenant security events have neither a safe tenant route nor an authorized command principal

**Evidence.** AD-30 requires every forged, wrong-audience, expired, replay-conflicting, and authorization denial to append to `SecurityEventLog(TenantId, UtcDay)` via an ordinary newly issued trusted command (`ARCHITECTURE-SPINE.md:156,386`). Before tag validation, the claimed target tenant is untrusted; on a cross-tenant attempt, actor tenant and target tenant differ. The four-principal allowlist grants neither `Platform` nor any Workflow kind a `SecurityEventLog` command, while the invalid caller cannot safely authorize it. Story 5.4 likewise names only an unqualified `TenantId` (`epics.md:1461-1464`).

**Divergence.** One host logs to the attacker-claimed target tenant, permitting forged traffic to create or flood another tenant's streams. Another logs to the authenticated actor tenant, and a third uses reserved `system`; each must also invent a principal/allowlist exemption. Audit location, ACL, and denial-of-wallet behavior differ.

**Disposition — AUTOFIX.** Define a least-privilege non-public security-recorder capability or closed Workflow kind, its non-recursive envelope behavior, and an exact routing grammar: authenticated actor-tenant events, authenticated target-tenant events when appropriate, and reserved-system pre-auth events carrying only a digest of the untrusted target. Bind quotas/ARCH-A-9 and cross-tenant non-disclosure tests.

### H-4 — Retry-time safety failure conflicts with the attempt's already-recorded budget disposition

**Evidence.** AD-20 applies its “before transport” `AttemptCancelled(SafetyFailed, NotInvoked)` and monetary release rule to every transport retry (`ARCHITECTURE-SPINE.md:314`). But any retry follows an earlier `ProviderInvocationAuthorized`; AD-13/AD-21 say that event atomically fixes `BudgetDispositionDecided(InvocationSettlement)` and permanently forbids the pre-invocation `NotInvokedRelease` branch (`ARCHITECTURE-SPINE.md:262,322`). Retry is allowed only after confirmed no-use lookup, but the named durable release authority still differs.

**Divergence.** A safety unit appends `NotInvokedRelease` because the retry transport did not occur; a budget unit rejects it because the attempt was historically invocation-authorized and settles/releases only from confirmed no-use evidence. Identical retry failure yields incompatible events and can wedge settlement.

**Disposition — AUTOFIX.** Split initial pre-invocation safety failure from retry recheck failure. The latter terminalizes/cancels the retry without transport but preserves the existing `InvocationSettlement` disposition and uses the already-required confirmed-no-use outcome revision for monetary release.

### H-5 — Rate admission and the open-interaction lease have no common decision order

**Evidence.** FR-8 step 5 and AD-13 group rolling-rate admission and the caller concurrency lease without ordering them (`prd.md:287-302`; `ARCHITECTURE-SPINE.md:262`). AD-21 lets rate consumption commit after its two scope preparations and says committed consumption is never released, while the open lease may still reject the same call for concurrency (`ARCHITECTURE-SPINE.md:322`). No shared interaction decision states whether a concurrency-rejected request consumes rate.

**Divergence.** Team A commits both rate scopes first; an open-limit rejection still consumes the Party and Conversation windows. Team B prepares or completes the open lease first and never contacts rate after a concurrency rejection. Repeated identical calls can exhaust rate only in Team A, changing denial-of-wallet and FR-25 results.

**Disposition — DISCUSS then AUTOFIX.** Product/Architecture must choose whether concurrency-rejected attempts consume rolling rate. Encode the chosen strict sequence or one common prepare/commit-or-abort decision and reflect the outcome in metrics/status and Story 6.4.

### H-6 — Rolling consumption has no authoritative `ResetAt` derivation

**Evidence.** AD-21 records Party and Conversation window lengths at authorization and says committed consumption remains until `ResetAt`, but records neither scope-specific `ResetAt` nor one authoritative admission instant (`ARCHITECTURE-SPINE.md:322`). The two ledger commit acknowledgements can occur at different times after recovery. AD-28 distinguishes command `EvaluatedAt` from EventStore commit instants but never selects one for rate reset (`ARCHITECTURE-SPINE.md:368-372`). The normative projection nevertheless requires exact `ResetAt` (`launch-readiness-register.md:336`).

**Divergence.** One ledger derives reset from rate authorization, another from interaction commit decision, and another from each ledger's own later commit. Identical admissions expire at different instants across scopes/replicas, changing admission and public status.

**Disposition — AUTOFIX.** Persist scope-specific reset instants once on `RateAdmissionAuthorized`, derived from its trusted `EvaluatedAt` plus each frozen window, and require both ledgers and projections to reuse them verbatim.

### H-7 — The hold/deletion OD blocks “every artifact purge” while its register explicitly permits export-abort cleanup

**Evidence.** AD-22 and the spine Open Decisions table say no artifact purge or `DestroyDek` may run until `OD-HOLD-DELETION-PRECEDENCE-1` is approved (`ARCHITECTURE-SPINE.md:330,929`). The launch register limits that OD to deletion start/purge variants and explicitly says mandatory cleanup of partial export output remains evaluable; `GovernanceProtection:ExportAbortCleanupOrRecover` is not an affected evaluation (`launch-readiness-register.md:95-96,215`).

**Divergence.** A spine-first workflow cannot purge a failed partial export while the hold/deletion policy is open, so it retains `ExportCleanupPending` indefinitely. A register-first workflow purges the partial export and releases its token. Both are fail-closed readings, but fence availability differs.

**Disposition — AUTOFIX.** Qualify the spine prohibition as deletion-directed destruction/purge after arm. Explicitly permit mandatory no-key-delivered cleanup of an uncommitted export under its existing fence token, independent of the unresolved hold-versus-deletion precedence choice.

### H-8 — Open-decision records expand the exact PRD `RQ-1` input list and have no durable writer/approval operation

**Evidence.** PRD FR-28 says its `RQ-1` list is complete and item 10 includes only Deferred PRD §13 decisions (`prd.md:714-730`). The launch register adds all applicable spine `OD-*` records to `RQ-1`, including `OD-DAPR-SECURITY-1`, and says approval “appends a successor,” but neither the aggregate inventory nor matrix defines the record owner, approval command, authorized producer, stream key, or projection rule (`launch-readiness-register.md:89-101`; `ARCHITECTURE-SPINE.md:156`). `LaunchReadinessGate` is defined only for (`GateId`, `TenantScope`, `EnvironmentProfile`) observations.

**Divergence.** One qualification unit treats document rows as static blockers despite the PRD rule that runtime never parses documents. Another invents LaunchReadinessGate streams and a Release-Operator approval command. A PRD-strict unit emits `GateOutOfScope` for the added inputs. The same approved planning decision can never clear, can clear by document edit, or can clear through invented runtime state.

**Disposition — DISCUSS/AUTOFIX.** Reconcile FR-28 explicitly if spine decisions are product-approved `RQ-1` inputs. Name the durable aggregate/key, command family/variant, recorder authority, approval-evidence validation, projection, supersession, and exact scoping for runtime versus story-only decisions; keep `OD-SPRINT-5.1-5.2-1` outside runtime as its record says.

## Medium

### M-1 — `GovernanceProtection` both may unpin a hold and “can never release” it

AD-30 allows `GovernanceProtection` to dispatch idempotent unpin/fence acknowledgement commands, while the next sentence says it can never “release”; matrix v4 names `HoldReleaseOrRecover` (`ARCHITECTURE-SPINE.md:386`; `launch-readiness-register.md:213`). Clarify that only the human release decision is forbidden and that the workflow may execute/finalize the already-recorded decision.

### M-2 — Epics promise complete matrix-v4 conformance while omitting every new internal service family

Story 8.6 enumerates the operation-family inventory and says no v4 family/variant may be omitted, but its list excludes `TrustedEnvelopeAdmission` and all `GovernanceProtection` variants (`epics.md:3011-3017`). These are intentionally non-interactive, so the story should limit its claim to public/UI-bearing families and assign internal matrix conformance to Stories 5.4/8.1-8.3, rather than claiming complete coverage.

## Reassessed Areas That Converge

- **V5 export-abort fence:** partial-output inventory, key non-delivery, cleanup-pending blocker, exact lookup, receipts, and release ordering now prevent an unindexed artifact escaping deletion. C-1 concerns recovery gate eligibility after a durable decision, not that repaired protocol.
- **Replay trust/ACL recursion:** reserved-system namespace, target-tenant binding, a single non-public registrar, least-privilege EventStore ACL, and no recursive principal close v5 C-2. H-1 through H-3 are later duplicate/order/audit edges.
- **Bootstrap and containment:** EventStore observation first use/repair, exact `ProvisionHexa` replay, platform/tenant scoping, confirmed SM-4 evidence, and reviewed release now converge.
- **Open lease and rate preparation:** the pre-acceptance safety abort set, frozen positive preparation profile, shared deadlines, decision revisions, no autonomous expiry, and failure-injection contracts close v5 H-1/H-4. H-5/H-6 concern the remaining cross-ledger order and committed-window clock.
- **Governance workflow authority:** the closed `GovernanceProtection` principal and matrix variants prevent human-authority borrowing; C-1 is a recovery-gating split and M-1 is terminology.
- **Conjunctive safety, approver recheck, distributed rescan, proposal-index crash consistency, human actor identity, dependency split, and exact status vocabulary** remain convergent across the spine, PRD, registers, and epics apart from H-4's retry-budget cross-contract.

## Current Reality Versus Architecture Debt

The parent-authoritative gitlinks remain the only repository authority. Builds is pinned by the root at `a32cb422` with Dapr Client/ASP.NET/Workflow `1.18.5`; its dirty checkout is now `cf52f74` and exposes `1.18.7`, but that does not commit the upgrade or retire `OD-DAPR-SECURITY-1`. Conversations, EventStore, FrontComposer, and Memories also have dirty checkouts, while Parties and Tenants match their gitlinks. Agents still has no Dapr Workflow reference and still contains legacy proposal-status members and the single legacy budget model. The revised replay registrar, three ledgers, safety epoch/index, proposal outbox, governance fence/export/deletion workflow, and public vocabulary remain unimplemented. These are correctly classified as implementation/tracking debt and are not additional architecture findings.

## Required Closure Order

1. Split irreversible decision authorization from recovery to close **C-1**.
2. Close replay admission/audit **H-1 through H-3** as one security-boundary pass.
3. Reconcile safety/budget **H-4**, then decide and encode ledger order/reset **H-5/H-6**.
4. Narrow exact OD applicability in **H-7** and reconcile the durable PRD/register OD model in **H-8**.
5. Apply M-1/M-2 wording/ownership corrections, lint, re-distill, and rerun the complete reviewer gate.
