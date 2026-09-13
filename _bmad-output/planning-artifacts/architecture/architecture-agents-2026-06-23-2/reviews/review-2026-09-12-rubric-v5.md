---
name: Hexalith Agents architecture good-spine rubric review v5
type: architecture-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
lens: rubric-walker
intent: validate-read-only
verdict: fail
counts:
  critical: 0
  high: 4
  medium: 2
  low: 0
---

# Good-Spine Rubric Walker — v5

## Verdict

**FAIL — 0 Critical, 4 High, 2 Medium, 0 Low.** The revised spine closes the authoritative validation report's Criticals and Highs, including the v4 derivative readiness, containment, and public-status defects. A fresh whole-spine walk nevertheless finds four independently implementable-unit divergences in the open-interaction, budget-recovery, and trusted-envelope replay protocols. These are architecture defects, not current implementation debt or unresolved Product choices.

The deterministic linter passes with zero findings. Mechanical validity does not close the semantic gate.

## Frozen Input Snapshot And Method

The complete good-spine checklist was reassessed against the current spine, bound PRD, both registers, the authoritative `VALIDATION-REPORT-2026-09-12.md`, the command-step convention, memlog, and focused repository reality. The review did not treat the prior finding list as the checklist. It specifically walked readiness self-bootstrap, kill-switch pull/release, public statuses, rate/open/budget ledger ownership and recovery races, trusted-envelope replay ownership, export/hold/deletion serialization, unresolved decisions, technology/repository ratification, capability coverage, and operational/recovery dimensions.

| Frozen input | SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `665611db80663133540e979332d54659bd00c96142d82b1bbdceebbc17667715` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |
| bound `prd.md` | `92585e96abf285e00d123bbadceaffd87004258448db5decd7b0e6cb7e5bc4a2` |
| `external-dependency-register.md` | `2c7438b9919d078023103046dedbebfbd7d2cc7e2d52e6b5a69d2a0488ef78fc` |
| `launch-readiness-register.md` | `25b9164c084159d46f9d66aa387da4dd5f450bc5fa7eae3bfae3064d86adde50` |
| `.memlog.md` | `aedf99bad237ccbadf87c2151bbcb7b649a82164eef8e336f491c48acce04aa7` |

`lint_spine.py` returned `ok: true`, `total_findings: 0` against this snapshot.

## Critical

None.

## High

### H-R5-1 — `SafetyFailed` is not an authorized abort decision for the prepared open-interaction lease

**Evidence**

- Acceptance prepares the caller's open lease before context, budget, pre-Provider safety, and membership, and commits only with `AgentCallAccepted` (`ARCHITECTURE-SPINE.md:260`, call flow `:486-508`).
- The open-lease protocol accepts only terminal pre-acceptance `Denied`, `Blocked`, or `ContextBlocked` as abort evidence (`ARCHITECTURE-SPINE.md:262`).
- AD-20 instead requires an initial pre-transport safety failure to terminalize as `SafetyFailed` and release the original caller's open-interaction lease (`ARCHITECTURE-SPINE.md:316`). The PRD also makes `SafetyFailed` a distinct terminal `AgentInteractionStatus`, not `Blocked` (`prd.md:299-301`).

**Divergence**

One ledger handler rejects `SafetyFailed` because it is not in the closed abort set and keeps the preparation counted forever. Another treats AD-20's release statement as sufficient abort authority. Both follow literal rules, but identical safety-blocked calls produce different caller-concurrency state.

**Impact**

A caller can permanently lose its only nonterminal-interaction slot after a pre-acceptance safety failure, blocking later calls without Provider work or a valid business reason.

**Action: AUTOFIX ARCHITECTURE.** Make every terminal pre-acceptance outcome, explicitly including `SafetyFailed`, an abort decision and distinguish prepared-lease abort from committed-lease terminal release. Bind the exact decision event and revision carried to the ledger, plus failure/recovery fixtures for safety failure before and after budget release. No Product decision is required.

### H-R5-2 — Budget recovery authorizes release from absence, without a durable interaction-owned decision

**Evidence**

- The attempt reserves money before pre-Provider safety and membership; only after `AgentCallAccepted` and capacity admission does the interaction append `ProviderInvocationAuthorized` (`ARCHITECTURE-SPINE.md:260`, call flow `:495-515`).
- AD-21 says recovery releases an orphan as `NotInvoked` when no `ProviderInvocationAuthorized` fact exists, but defines no `BudgetReleaseDecided` fact, owner lease/fence, or acknowledgement protocol corresponding to the rate/open ledgers (`ARCHITECTURE-SPINE.md:322`).
- The PRD requires a reservation to be released when a later pre-Provider check actually fails and never permits that released reservation to support a Provider invocation (`prd.md:720-728`). `LR-RECOVERY` promises immutable pending budget decisions and failure injection at every decision/ack boundary (`launch-readiness-register.md:257-263`).

**Divergence**

A recovery worker may observe a valid in-progress reservation before the workflow has appended `ProviderInvocationAuthorized` and release it, while the resumed workflow subsequently accepts and authorizes invocation. A stricter worker may refuse all absence-only releases and strand reservations after a crash. Nothing durable selects one outcome.

**Impact**

The same attempt can invoke after its reservation was released, or hold budget forever. Either breaks atomic cost enforcement and makes recovery behavior replica/timing dependent.

**Action: AUTOFIX ARCHITECTURE.** Give `AgentInteraction` a mutually exclusive durable budget disposition decision (`NotInvoked` release versus invocation/indeterminate settlement authority) and require the `BudgetLedger` command to carry that event revision. Absence of `ProviderInvocationAuthorized` alone must never release. Bind recovery, timeout, concurrent-resume, and lost-ack fixtures. No Product choice is required.

### H-R5-3 — Trusted replay registration has no non-recursive privileged mutation seam

**Evidence**

- AD-2 makes `TrustedEnvelopeReplay` an Agents EventStore aggregate, and AD-30 requires the verifier to append/read it after envelope verification but before target dispatch (`ARCHITECTURE-SPINE.md:156`, `:384`).
- AD-3 makes EventStore commands the only Agents-state mutation route. The normative command-step convention requires an audit-worthy durable result to be dispatched as a trusted command and envelope through the aggregate boundary (`IMPLEMENTATION-CONVENTIONS.md:7-20`).
- AD-30 requires every Agents command envelope to be authenticated and replay-verified before the aggregate, but neither its principal allowlists nor the operation matrix defines a replay-registration command, owner identity, or exception (`ARCHITECTURE-SPINE.md:384`; `launch-readiness-register.md:194-211`).

**Divergence**

If replay registration follows the normal trusted-envelope pipeline, its own append recursively requires another replay registration. If it bypasses that pipeline, an implementation must invent a privileged raw-append authority and its target restrictions. The two designs are security- and availability-incompatible.

**Impact**

Strict implementations can make every trusted command undispatchable; permissive implementations can create an unbounded privileged mutation path capable of nonce poisoning or other aggregate writes.

**Action: AUTOFIX ARCHITECTURE.** Define one narrowly scoped, non-recursive replay-registration seam outside ordinary target-command envelope verification. Bind its sole caller, exact aggregate/key, conditional-create/expected-revision behavior, authorization credential, audit behavior, and prohibition on mutating any other aggregate. Add first-use, concurrent-first-use, lost-ack, unavailable-ledger, and privilege-confusion fixtures. No Product decision is required.

### H-R5-4 — Tenant-scoped replay keys cannot enforce the stated changed-field nonce rule across tenants

**Evidence**

- The replay aggregate key is (`TenantId`, `Issuer`, `DeliveryNonce`) (`ARCHITECTURE-SPINE.md:156`, `:384`).
- The target tenant is an authenticated canonical-envelope field, and AD-30 says reuse of a delivery nonce with **any** changed authenticated field or tag is rejected and audited (`ARCHITECTURE-SPINE.md:384`).
- `EXT-SECRETS-1` repeats unique-per-delivery nonces and changed-field nonce-reuse rejection without narrowing that promise to one target tenant (`external-dependency-register.md:185-188`).

**Divergence**

The same issuer and nonce used in a valid envelope for tenant A and then in a differently authenticated valid envelope for tenant B addresses two different replay streams. A literal aggregate implementation accepts both as first-seen; a literal security-contract implementation requires the second to be rejected.

**Impact**

The stated cross-field replay defense fails exactly when the changed authenticated field is the tenant boundary. Issuer defects or compromise therefore receive less containment than the public security contract claims.

**Action: AUTOFIX ARCHITECTURE.** Either key nonce uniqueness by (`Issuer`, `DeliveryNonce`) in a platform/system replay namespace and keep target tenant in the bound record, or explicitly narrow nonce uniqueness and the changed-field promise to a tenant-scoped issuer domain with an independently enforced issuer/tenant binding. Preserve tenant in the canonical digest and add cross-tenant nonce-reuse fixtures. No Product decision is required.

## Medium

### M-R5-1 — Architecture-owned assumptions still have no literal retirement dates

**Evidence**

The PRD requires each Architecture-owned `ARCH-A` row to carry a literal calendar retirement date and treats a missing or milestone-only date as an `RQ-1` blocker (`prd.md:886-900`). Architecture-owned or co-owned rows including `ARCH-A-1` through `ARCH-A-4`, `ARCH-A-6` through `ARCH-A-8`, `ARCH-A-11`, `ARCH-A-12`, and `ARCH-A-14` remain `Unscheduled` with milestone-only revisit text (`ARCHITECTURE-SPINE.md:968-989`).

**Impact**

Qualification still fails closed through `UnretiredAssumption`, so this is not a safety High, but Architecture and co-owners remain outside the PRD's bounded governance rule.

**Action: DISCUSS.** Obtain owner-approved literal dates or retire/replace each assumption. Do not invent dates; keep the existing blockers until the approvals exist.

### M-R5-2 — `PostingPending` attempt timeout remains an implementation-selected value

**Evidence**

The PRD and AD-5 require a stored attempt deadline no shorter than the Conversations seam timeout, but neither selects a duration, range, configuration authority, or snapshot source (`prd.md:438-451`; `ARCHITECTURE-SPINE.md`, AD-5). `ARCH-A-14` explicitly records the missing value and only a milestone revisit (`ARCHITECTURE-SPINE.md:989`).

**Impact**

The stored value makes each individual attempt deterministic, keeping this below High, but separately built ingress/workflow units can choose different values and therefore disagree on when lookup, `LateConfirmed`, and `PostingFailed` occur.

**Action: DISCUSS OR HARD-BLOCK THE STORY.** Architecture should propose the duration/configuration authority, allowed range, snapshot rule, and compatibility rule with `EXT-CONV-AI-1`; Product confirms where required. Do not let an implementation choose while `ARCH-A-14` is open.

## Low

None.

## Requested Control Audit

| Area | Result | Evidence / disposition |
| --- | --- | --- |
| Readiness self-bootstrap and repair | **Pass** | Matrix v4 gives `ReadinessObservation:ObserveEventStoreBootstrapOrRepair` a gate-free, target-limited conditional append and makes every other observation require `LR-EVENTSTORE`; fixtures cover `NoStream`, stale/block repair, wrong target, and append failure (`launch-readiness-register.md:188-215`). |
| Kill-switch pull and release | **Pass** | `PullContainment` attempts the EventStore append without circular readiness dependencies and remains target/actor/revision/idempotency checked; `ReleaseReviewed` retains the normal gate and review evidence (`ARCHITECTURE-SPINE.md`, AD-12; `launch-readiness-register.md:207-208`). |
| Public statuses | **Pass** | AD-15 now exactly follows PRD FR-8 status membership, puts rate/budget/capacity/dependency data in typed reason/progress fields, and keeps proposal posting state in `ProposedAgentReplyState` (`ARCHITECTURE-SPINE.md:278`; `prd.md:299-302`). Deprecated shipped proposal-duplicating values remain explicitly classified as migration debt. |
| Rate admission | **Pass** | Interaction-owned ordinal/id, two prepares, mutually exclusive commit/abort facts, evidence revisions, non-expiring preparations, and recovery-to-one-decision remove the prior split-ledger race (AD-21). |
| Open-interaction admission | **Fail** | H-R5-1: the closed abort vocabulary omits the AD-20/PRD terminal `SafetyFailed` outcome. |
| Budget reservation/recovery | **Fail** | H-R5-2: absence of invocation authorization is treated as release authority without a durable disposition decision. |
| Trusted replay owner and key | **Fail** | H-R5-3 and H-R5-4: replay registration is not bound as a non-recursive privileged seam, and the tenant-scoped key contradicts cross-field nonce-reuse rejection. |
| Export/hold/deletion fence | **Pass at architecture scope** | AD-22 serializes hold, export, and deletion intent on `ProtectionFence`; export freezes before content reads, commit waits for artifact/manifest/index acknowledgements, and deletion includes committed export high-water. Failure remains closed while either decision below is open. |
| Unresolved decisions | **Pass as visibly blocked choices** | `OD-HOLD-DELETION-PRECEDENCE-1` affects only `RQ-1` and `ProtectedDeletion:DestructionStarted`; `OD-EXPORT-LIFECYCLE-1` affects export activation/operations, `RQ-1`, and deletion completion. The register schema versions decisions and requires exact approvals (`launch-readiness-register.md:89-98`). No Product outcome was invented. |

## Authoritative Prior-Finding Closure Audit

| Prior finding family | v5 disposition |
| --- | --- |
| C-1 safety weakening | **Closed.** AD-20 requires snapshot-plus-current conjunctive safety evaluation and durable unique evaluation identity. |
| C-2 hold/deletion exclusion | **Closed at architecture scope.** The common tenant `ProtectionFence` and fail-closed OD prevent irreversible work before the unresolved late-hold policy lands. |
| C-3 bootstrap/repair | **Closed.** Matrix v4 has target-aware variants and the EventStore observation bootstrap exception. |
| H-1 approver recheck; H-2 safety rescan; H-3 human-only Approvers | **Closed.** Ownership, durable coordination, fresh type/liveness, and unavailable-evidence behavior are bound. |
| H-4 mixed ledger lifetimes | **Original finding closed; new protocol defects H-R5-1/H-R5-2.** Rate, open-concurrency, and monetary state have distinct aggregates, but two disposition paths remain incomplete. |
| H-5 human identity; H-6 proposal index; H-7 Conversations seam atomicity | **Closed.** Stable human actor identity, source-revision high-water repair, and independent optional retraction are bound. |
| H-8 trusted-envelope authentication | **Original cryptographic fields closed; new ownership/key defects H-R5-3/H-R5-4.** MAC input, expiry, rotation, revocation, and retention are present, but the replay mutation path and cross-tenant nonce rule are not implementable as written. |
| H-9 export lifecycle | **Closed at architecture scope.** Store/fence/index/manifest/purge invariants are bound and the Product-owned policy remains an explicit blocker. |
| H-10 Dapr reality; H-11 parity wording; H-12 sprint tracking | **Closed or correctly surfaced.** Root-authoritative transitive Dapr exposure, future contract debt, and the delivery/evidence conflict are distinguished without changing architecture authority. |
| v4 C-R4-1 readiness self-bootstrap | **Closed.** The gate-free EventStore observation variant is target-limited and test-bound. |
| v4 H-R4-1 kill switch | **Closed.** Pull and release are distinct variants with containment-safe and normal-gate semantics. |
| v4 H-R4-2 status contract | **Closed.** AD-15 and the bound PRD now share one domain wire grammar. |
| v4 M-R4-1 principal identity | **Closed.** AD-30 defines the exact per-kind `PrincipalIdentity` components used by AD-29. |
| v4 M-R4-2 assumption dates | **Remains M-R5-1.** |
| v4 M-R4-3 posting timeout | **Remains M-R5-2.** |

## Good-Spine Checklist

| Criterion | Result | Reason |
| --- | --- | --- |
| Fixes real divergence points at the level below | **Fail** | H-R5-1 through H-R5-4 permit incompatible but literal implementations at three state-owning seams. |
| Every Rule enforceably prevents its stated divergence | **Fail** | AD-13/AD-21 do not close lease/reservation disposition; AD-30 does not close replay registration/key scope. |
| Deferred/open items are safe to defer | **Pass with Medium governance debt** | Hold/export ODs and all unavailable dependencies fail closed. M-R5-1/M-R5-2 remain explicit blockers rather than silent defaults. |
| Named technology is verified-current | **Pass with explicit blocker** | The spine distinguishes the root-authoritative transitive Dapr `1.18.5` exposure from the dirty `1.18.7` checkout and keeps upgrade/exception decision `OD-DAPR-SECURITY-1` open. No unverified future SDK is treated as shipped. |
| Brownfield ratification | **Pass** | Current source still lacks the new ledger/replay/fence workflows and retains deprecated status values; the delivery-debt table treats those as implementation work. The four High findings are defects in the target architecture contract, not absence of implementation. |
| Binding PRD capability coverage | **Fail** | Capability breadth and public status alignment pass, but concurrency, budget, and replay guarantees are not deterministic under recovery/concurrency. |
| Inherited parent constraints | **N/A** | This is the initiative spine; no parent spine or inherited AD set is declared. |
| Initiative structural breadth | **Pass** | Paradigm, ownership, state/mutation, APIs/UI, integration, security/governance, deployment, recovery, capacity, provider strategy, evidence, environment, and operations are all decided, delegated to authoritative registers, or blocked. |
| Mechanical validity | **Pass** | Deterministic linter: zero findings on the frozen input. |

## Repository Reality And Debt Classification

Focused inspection confirms that the source `AgentInteractionStatus` still contains the legacy proposal-state duplicates and lacks the future reconciliation work, while no shipped `TrustedEnvelopeReplay`, `OpenInteractionLedger`, rate-decision, or `ProtectionFence` implementation was found. These are already represented as implementation/delivery debt and do not lower the architecture findings above. The dirty `references/Hexalith.Builds` checkout contains Dapr `1.18.7`; the root gitlink remains the authority identified by `ARCH-A-15`, so the checkout is not accepted as retirement evidence.

## Gate Recommendation

Do not ratify v5. Correct H-R5-1 and H-R5-2 by giving open/budget ledgers complete interaction-owned decision-and-ack protocols. Correct H-R5-3 and H-R5-4 by defining a non-recursive, least-privilege replay-registration seam and one nonce-uniqueness domain consistent with the authenticated-field promise. Preserve AD ids and append decisions to the memlog. Keep the two Medium items and Product-owned ODs visibly blocked unless their owners decide them. Then re-distill and rerun the complete Reviewer Gate on a new frozen snapshot.
