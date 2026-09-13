---
name: Hexalith Agents adversarial-divergence review v5
type: architecture-review
lens: adversarial-divergence
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
verdict: fail
critical: 2
high: 5
medium: 1
low: 0
lint_ok: true
---

# Adversarial Divergence Review — v5

## Verdict

**FAIL — the frozen revision closes the prior bootstrap, status-vocabulary, committed-export, open-lease, and governance-principal defects, but two security/data-loss boundaries and five High cross-unit contracts still admit incompatible literal implementations.**

This is a fresh downstream-unit review of the frozen current tree. It constructs independent implementations from the current ADs rather than treating v4 findings as a closure checklist. The deterministic spine linter passes with zero findings. The input hashes before review were:

- `ARCHITECTURE-SPINE.md`: `665611db80663133540e979332d54659bd00c96142d82b1bbdceebbc17667715`
- bound `prd.md`: `92585e96abf285e00d123bbadceaffd87004258448db5decd7b0e6cb7e5bc4a2`
- `external-dependency-register.md`: `2c7438b9919d078023103046dedbebfbd7d2cc7e2d52e6b5a69d2a0488ef78fc`
- `launch-readiness-register.md`: `25b9164c084159d46f9d66aa387da4dd5f450bc5fa7eae3bfae3064d86adde50`
- `VALIDATION-REPORT-2026-09-12.md`: `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 2 |
| High | 5 |
| Medium | 1 |
| Low | 0 |

## Critical

### C-1 — `ExportAborted` may release the fence while an unindexed readable partial artifact survives

**Evidence.** AD-22 correctly acquires `ExportPreparing` before content read and permits a later deletion to arm only after `ExportAborted` or `ExportCommitted` (`ARCHITECTURE-SPINE.md:324`). It makes commit wait for artifact, manifest, and index acknowledgements, but gives abort no symmetric precondition: it does not require physical purge/destruction receipts for a partially written artifact, manifest, index entry, or export envelope key before the fence token is released. The deletion freezes matching committed exports at the committed-index high-water, so a failed export that never committed need not enter its frozen set. `EXT-EXPORT-STORE-1` repeats the prepare/abort/commit fence but likewise gives abort no cleanup ordering (`external-dependency-register.md:199`).

**Two literal units.** Team A appends `ExportAborted` immediately after a failed index acknowledgement, releases the fence, and asynchronously retries cleanup of an already immutable encrypted artifact. Team B retains `ExportPreparing` until every partial copy and key is proved unreadable or physically purged, then appends `ExportAborted`. Both implement the stated abort branch, but only Team B prevents an overlapping deletion from completing while a readable, unindexed copy survives.

**Impact.** The all-Agents-copies deletion guarantee can be reported complete after irreversible DEK destruction while an export copy remains decryptable.

**Disposition — AUTOFIX.** Make `ExportAborted` a terminal cleanup fact allowed only after a frozen partial-output inventory has an irreversible key-destruction or physical-purge receipt for every artifact/manifest/index copy and proof the export key was never delivered or is destroyed. Keep the fence token until those acknowledgements are durable; recovery must resolve lost acknowledgements by lookup and failure-inject every boundary.

### C-2 — The trusted-envelope replay aggregate has no authorized, non-recursive admission path

**Evidence.** AD-3 requires external results and mutations to feed aggregates through commands (`ARCHITECTURE-SPINE.md:166`). AD-30 says every Agents command envelope has exactly one of four principals, every reserved extension is aggregate-verified, and the verifier must append/read `TrustedEnvelopeReplay` before target dispatch (`ARCHITECTURE-SPINE.md:380`). It defines no verifier principal, verifier-only command, trusted internal append primitive, or explicit exemption from envelope replay admission for the command that creates the replay record. The operation matrix likewise names no replay-admission family/variant (`launch-readiness-register.md:160-211`).

**Two literal units.** Team A treats AD-30's specific wording as permission for the verifier to low-level append a replay event without a domain command. Team B follows AD-3 and dispatches a replay-admission command carrying the original principal; that command itself must pass envelope replay admission before dispatch and either recurses or is rejected because the principal's target allowlist does not grant mutation of `TrustedEnvelopeReplay`. A third implementation can silently special-case the aggregate with an unauthenticated path. All choices invent a security boundary absent from the spine.

**Impact.** Hosts can deadlock every authenticated command or bypass the uniform command/authorization invariant at the replay ledger, exactly where cross-replica replay resistance depends on one atomic decision.

**Disposition — AUTOFIX.** Define one closed verifier-admission primitive and trust boundary: its caller identity, exact EventStore compare-and-create command/event shape, authorization, tenant/issuer key, expected-revision behavior, audit behavior, and explicit single exemption from recursive envelope verification. Add its concrete matrix treatment and tests proving no public, human, Workflow, or target-handler path can invoke it directly.

## High

### H-1 — Initial safety failure is terminal but is not an authorized open-lease abort decision

**Evidence.** AD-13 says the only durable pre-acceptance lease-abort decisions are terminal `Denied`, `Blocked`, or `ContextBlocked` events (`ARCHITECTURE-SPINE.md:262`). The open lease is prepared at FR-8 step 5, while the initial pre-Provider safety scan occurs later at step 9. AD-20 expressly makes that failure terminal `SafetyFailed` and requires release of the original-caller lease (`ARCHITECTURE-SPINE.md:312`); FR-8 also declares `SafetyFailed` terminal (`prd.md:299-302`).

**Divergence.** One ledger rejects release because `SafetyFailed` is not an AD-13 abort decision; another follows AD-20 and releases it. The first permanently consumes concurrency after a legitimate failed acceptance, while the second accepts evidence the ledger protocol does not authorize.

**Disposition — AUTOFIX.** Make the abort-decision set exactly every terminal pre-`AgentCallAccepted` outcome, explicitly including `SafetyFailed`, and use the authoritative decision revision in the existing abort/ack recovery protocol.

### H-2 — Platform containment pull does not require the confirmed SM-4 fact that authorizes it

**Evidence.** AD-12 and PRD FR-28/FR-33 permit immediate Platform-Operator pull only on a confirmed and recorded cross-tenant or unauthorized action (`ARCHITECTURE-SPINE.md:246-250`; `prd.md:503-506,746-750`). Matrix v4's Platform branch for `TenantKillSwitch:PullContainment` requires only fresh Platform authority, target, revision, and idempotency; only the Release-Operator branch requires a recorded trigger-review decision (`launch-readiness-register.md:207`).

**Divergence.** A matrix-first unit lets any fresh Platform Operator pull any tenant switch without incident evidence. A PRD-first unit requires the recorded confirmed SM-4 fact. Both can claim to follow their normative authority, but tenant state and audit outcomes differ.

**Disposition — AUTOFIX.** Add `ConfirmedSm4EventRecorded` with immutable incident identity and expected revision to the Platform branch's direct preconditions, bind the pull event to it, and test missing, stale, cross-tenant, and exact-replay cases.

### H-3 — Governance workflows are authorized by AD-30 but lack closed matrix variants for their irreversible steps

**Evidence.** AD-30 now defines `GovernanceProtection` and a sound target-scoped allowlist for prepare/pin/unpin, artifact/index/manifest, destruction/purge, and fence acknowledgement commands (`ARCHITECTURE-SPINE.md:380`). AD-17 simultaneously requires every workflow activity to declare exactly one known operation family and concrete variant (`ARCHITECTURE-SPINE.md:290`). Matrix v4 has no closed rows for `GovernanceProtection`, `ProtectionFence`, `AuditExport`, or `ProtectedDeletion`; its open decisions nevertheless target internal `ProtectedDeletion:DestructionStarted` and `ProtectedDeletion:Complete` evaluations that the matrix does not define (`launch-readiness-register.md:91-97,188-211`).

**Divergence.** One team labels all recovery steps with the initiating public family and misses the internal open-decision evaluation. Another invents `ProtectedDeletion:*` matrix families, which the unknown-family rule says must block. A third uses `SystemTimer`, whose AD-30 allowlist does not grant destruction. The authorized workflow therefore either wedges or locally broadens its gates.

**Disposition — AUTOFIX.** Add a closed set of governance workflow family/variant rows, including prepare, abort, commit, `DestructionStarted`, per-copy purge/destruction, completion, and recovery. Bind each open decision to the exact row and retain all unrelated gates and direct owner revisions.

### H-4 — Two-scope rate preparation uses an undefined stored deadline

**Evidence.** AD-21's authoritative `RateAdmissionAuthorized` fact stores ordinal/id, party/conversation, and both rolling windows, but no preparation deadline (`ARCHITECTURE-SPINE.md:318`). It later says a failed or deadline-expired preparation causes abort and that each ledger acts at “its stored deadline.” AD-28 requires durable decision deadlines to be derived once and stored, but does not identify an owner or derivation for this deadline (`ARCHITECTURE-SPINE.md:368`).

**Divergence.** Independently built Party and Conversation ledgers can derive different deadlines from receipt time, workflow time, or their rolling windows. Either ledger can cause an earlier interaction abort, or both can retain reservations for different periods after recovery; exact API replay does not carry one authoritative value.

**Disposition — AUTOFIX.** Add one `PreparationDeadline` to `RateAdmissionAuthorized`, derive it once from the trusted `EvaluatedAt` and a versioned bounded profile, require both prepare commands to match it, and make a mismatch a no-state-change conflict.

### H-5 — Two spine `OD-*` blockers are absent from the register that claims to materialize every applicable spine decision

**Evidence.** The spine's Blocking Open Decisions table names `OD-DAPR-SECURITY-1` as a release/Workflow-adoption blocker and `OD-SPRINT-5.1-5.2-1` as a dependency-consumer blocker (`ARCHITECTURE-SPINE.md:920-929`). The launch register states it materializes every spine `OD-*` that can block runtime, dependency activation, an operation, or `RQ-1`, and rejects missing records, but its immutable decision table contains only the hold/deletion and export decisions (`launch-readiness-register.md:91-98`).

**Divergence.** A register-only evaluator cannot emit either blocker. A spine-aware release process blocks on prose that has no version, approval evidence, or `AffectedEvaluations` schema. Teams therefore disagree on whether Workflow adoption/release and affected dependency work are permitted.

**Disposition — AUTOFIX/TRACKING SPLIT.** Materialize `OD-DAPR-SECURITY-1` with exact affected release/readiness evaluations and approval evidence. Either materialize the sprint decision with its delivery-only affected evaluations or rename it as tracking debt rather than an `OD-*` runtime/readiness record; do not leave it in both categories implicitly.

## Medium

### M-1 — Cross-period budget exposure remains delegated to a nonexistent configured policy

AD-21 says a new-period admission reads prior settled/outstanding obligations “required by the configured cap policy,” but neither the aggregate fields nor PRD FR-28 defines that policy or formula (`ARCHITECTURE-SPINE.md:318`; `prd.md:729-738`). One unit can isolate UTC months and another can carry prior outstanding exposure into the new cap. Product/Architecture should select period-isolated enforcement or define a versioned cross-period exposure field and formula.

## Reassessed Areas That Converge

- **Conjunctive safety/retry:** snapshot and current policies, dominance proof, immutable transport fingerprint, terminal failed attempt, and stage-specific proposal outcome now agree. H-1 is solely the lease evidence-set omission.
- **Hold/deletion core:** reversible arm, blocking open decision before destruction, expected-revision fence, and all-or-nothing completion converge. C-1 is the export-abort cleanup edge, not the core precedence policy.
- **Matrix bootstrap/scope:** the EventStore self-bootstrap, other-gate correction path, target-aware platform/tenant scope, exact `ProvisionHexa` replay, and gate-free containment recording now converge.
- **Approver recheck, distributed rescan, human identity, proposal index, dependency split, and public status:** the revised ownership, recovery, and vocabulary contracts close the validation report's Critical/High divergences.
- **Three ledgers:** ownership and lifetimes are distinct; rate and open ledgers use interaction-owned decisions. H-1 and H-4 are bounded evidence-schema omissions within those otherwise converged protocols.
- **Export fail-closed open policy:** request/download and deletion completion remain closed until the approved lifecycle policy and an `Available` store. C-1 must be closed in the policy-independent protocol before enabling that branch.

## Current-Reality Classification

The repository still lacks the revised replay ledger, three-ledger protocols, governance fence/export store, safety rescan, proposal-index reconciliation, and complete public status vocabulary. Those are implementation and tracking debt already assigned in the spine; they do not justify weakening the ADs and are not additional architecture findings. The parent-authoritative Builds gitlink remains the release authority; a dirty nested checkout cannot resolve `OD-DAPR-SECURITY-1` or change dependency status.

## Required Closure Order

1. Close **C-1** before any export artifact can be produced and **C-2** before trusted envelopes become an authorization boundary.
2. Close **H-1/H-4** together in the interaction-owned lease/admission decision schemas.
3. Close **H-2** in matrix-v4 containment evidence.
4. Close **H-3** before implementing governance recovery or approving either destructive open decision.
5. Reconcile **H-5** between the spine and launch register, then resolve or explicitly defer M-1 and rerun the complete gate.
