---
name: Hexalith Agents architecture good-spine rubric review v6
type: architecture-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
lens: rubric-walker
intent: validate-read-only
verdict: fail
counts:
  critical: 0
  high: 2
  medium: 4
  low: 1
---

# Good-Spine Rubric Walker — v6

## Verdict

**FAIL — 0 Critical, 2 High, 4 Medium, 1 Low.** The frozen revision closes every Critical and High finding in the authoritative `VALIDATION-REPORT-2026-09-12.md` and closes the four derivative v5 Highs. A fresh whole-spine walk nevertheless finds two new trusted-envelope boundary contradictions: the matrix can run target idempotency before nonce registration even though AD-30 requires the opposite, and denial audit commands have no valid principal or outage-safe durable writer. These are architecture-contract defects, not missing implementation and not unresolved Product choices.

The deterministic linter passes with zero findings. Mechanical validity does not close the semantic gate.

## Frozen Input Snapshot And Method

The complete Good-Spine checklist was reassessed independently against the current spine, authoritative validation report, bound PRD, epics, both registers, memlog, command-step convention, repository instructions, and focused current source/package/gitlink reality. Prior findings were verified for closure but were not used as the checklist. The walk covered state and mutation ownership, recovery decisions and acknowledgements, authorization and audit boundaries, tenant isolation, content/data-loss fences, open-decision fail-closed behavior, external seams, public contracts, operational/environment/provider dimensions, implementation-debt separation, source traceability, and mechanical validity.

| Frozen input | SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `e3fc24de16eb03449eaaf3a1b495dfbb97e3e2915f696ba32817bf8fb739ce6a` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |
| bound `prd.md` | `aeac1bd0bff7abe009e6388d90da67e22bf70b50b04d6805f042241f372d0924` |
| `epics.md` | `311b011c762c8482492701c10b0d844cf6747358b08799a702dd596550da796b` |
| `external-dependency-register.md` | `89096ef5d57c315dbe87b4bdfda5a76250b7295f890467068168d73c4c4fe6e5` |
| `launch-readiness-register.md` | `f49a2450994bed0621ab2c78ecc425ec04f4282bd50c927922dac436f51de644` |
| `.memlog.md` | `d5181fbd5730c4c31f7312e9f3438c17f05974e6f1b8c1bed4609419c9d63547` |
| `IMPLEMENTATION-CONVENTIONS.md` | `2ebba31863470f233aeeeccca45283542be2b6e84e3253323b97e7437bf1e7fd` |

`lint_spine.py` returned `ok: true`, `total_findings: 0` against this snapshot.

## Critical

None.

## High

### H-R6-1 — Matrix-v4 ordering can bypass AD-30's first-seen nonce decision

**Evidence**

- Matrix v4 says AD-29 target-aggregate idempotency precedes every mutable readiness or direct-precondition read, that an exact replay returns immediately, and that this ordering is part of **every** row with no local exception (`launch-readiness-register.md:194`).
- The matrix includes `TrustedEnvelopeAdmission:RegisterFirstSeen`; its direct preconditions include replay-stream expected revision and it performs the conditional replay append (`launch-readiness-register.md:211`).
- AD-30 instead requires first-seen registration after cryptographic/time validation and **before** constructing or dispatching the target command. Only an identical replay-record acknowledgement is routed onward to AD-29; changed authenticated fields or tag under the same nonce must reject and audit (`ARCHITECTURE-SPINE.md`, AD-30).
- AD-29's target idempotency fingerprint authenticates the logical command payload and recorded digest-key version, not the delivery nonce, issued/expiry instants, signing-key version, or envelope tag (`ARCHITECTURE-SPINE.md`, AD-29).

**Divergence**

A shared matrix evaluator that follows line 194 may return an existing target-command outcome before consulting `TrustedEnvelopeReplay`; the verifier implementation that follows AD-30 must register/read the nonce first. With the same logical command and payload, but a reused nonce whose delivery-only authenticated fields or tag changed, the former returns the old outcome while the latter rejects and audits nonce reuse.

**Impact**

The architecture's cross-field replay-detection and audit guarantee depends on which literal ordering an independently built verifier/matrix evaluator follows. Target business mutation remains protected by AD-29 idempotency, keeping this below Critical, but the security contract and qualification fixtures can disagree in production.

**Action: AUTOFIX ARCHITECTURE.** Make `TrustedEnvelopeAdmission:RegisterFirstSeen` the single explicit pre-command exception to matrix line 194: execute its intrinsic conditional-create/exact-existing decision first, then run AD-29 target idempotency. Keep AD-29 first for every ordinary command row. Bind a fixture where the target idempotency record already exists but the same delivery nonce has changed expiry, signing-key version, or tag; it must still reject and audit. No Product decision is required.

### H-R6-2 — Trusted-envelope denials have no authorized, non-recursive audit writer

**Evidence**

- AD-30 closes every command principal to `User`, `Administrator`, `Platform`, or `Workflow`. `Platform` is a freshly authenticated human Platform Operator; `Workflow` is narrowly allowlisted and may not mutate `SecurityEventLog`; other families reject `Platform` unless expressly granted (`ARCHITECTURE-SPINE.md`, AD-30).
- Forged, expired, revoked, wrong-audience, replay-conflicting, and other trusted-envelope failures occur before any trustworthy target principal reaches an aggregate. AD-30 nevertheless requires changed-nonce rejection to audit through a newly issued content-free `SecurityEventLog` command with its own nonce and states that every denial and security event is appended.
- The only non-recursive pre-command capability is `ITrustedEnvelopeReplayRegistrar`, whose ACL permits only `TrustedEnvelopeFirstSeen` on the reserved replay stream. AD-3 says no other aggregate mutation is exempt from the ordinary command convention (`ARCHITECTURE-SPINE.md`, AD-3 and AD-30).
- If the replay registrar itself is unavailable, a normally enveloped security-audit command also cannot register its own nonce; the architecture defines no durable audit spool, recovery owner, or safe acknowledgement protocol for that path.

**Divergence**

One implementation can borrow the rejected envelope's untrusted or no-longer-valid principal, one can invent a service/system principal absent from the closed grammar, one can bypass the trusted-command boundary for `SecurityEventLog`, and one can fail closed without recording the denial. None implements all literal rules, and outage recovery differs again.

**Impact**

Security denial evidence can be absent exactly during forgery, replay, revocation, or replay-ledger incidents, while another implementation may create an over-privileged mutation bypass to preserve the audit claim. This breaks a security/operations boundary and prevents one qualification interpretation.

**Action: AUTOFIX ARCHITECTURE.** Define one least-privilege, non-human audit authority and durable write/recovery protocol. It must be closed to content-free `SecurityEventLog` events and the correct tenant/day stream, have explicit identity/idempotency/ack semantics, remain non-recursive for failures before trusted-envelope admission, and specify what happens while its store is unavailable. Add forged-envelope, changed-nonce, revoked-key, lost-ack, audit-store outage, and replay-store outage fixtures. If the intended choice is instead to weaken “every denial is appended,” Security/Product approval is required; do not infer that choice.

## Medium

### M-R6-1 — Architecture-owned assumptions still lack literal retirement dates

**Evidence:** The PRD requires every Architecture-owned `ARCH-A` row to carry a literal calendar retirement date and says a milestone or `TBD` remains an `RQ-1` blocker (`prd.md:891-895`). Architecture-owned/co-owned open rows including `ARCH-A-1` through `ARCH-A-4`, `ARCH-A-6` through `ARCH-A-8`, `ARCH-A-11`, `ARCH-A-12`, and `ARCH-A-14` remain `Unscheduled` or milestone-only (`ARCHITECTURE-SPINE.md:979-995`).

**Impact:** `RQ-1` still fails closed, so this is governance incompleteness rather than a safety High; co-owners nevertheless have no bounded retirement calendar.

**Action: DISCUSS.** Obtain owner-approved literal dates or retire/replace each row. Do not invent dates, and retain `UnretiredAssumption` blockers until approval exists.

### M-R6-2 — `PostingPending` timeout remains implementation-selected

**Evidence:** AD-5 requires a stored posting-attempt deadline no shorter than the Conversations posting timeout, but fixes no duration, range, configuration owner, or snapshot version. `ARCH-A-14` explicitly records the missing decision and has no literal retirement date (`ARCHITECTURE-SPINE.md`, AD-5 and `ARCH-A-14`; bound PRD FR-18).

**Impact:** Each stored attempt is deterministic, keeping this below High, but separately built ingress/workflow units can choose different deadlines and therefore disagree on lookup, `LateConfirmed`, and `PostingFailed` timing.

**Action: DISCUSS OR HARD-BLOCK THE STORY.** Architecture proposes the duration/configuration authority, allowed range, snapshot rule, and `EXT-CONV-AI-1` compatibility rule; Product confirms where needed. Do not let implementation select it while `ARCH-A-14` is open.

### M-R6-3 — Two evidence manifests still ask for matrix-v3 tests while their stories require v4

**Evidence:** Story 5.5 requires `OperationGateMatrixVersion 4` but names `OperationGateMatrixV3ParityTests`; Story 5.7 also evaluates version 4 but names `OperationGateMatrixV3ActivationTests` (`epics.md:1509-1512`, `1537`, `1618`, `1650`). The global epic rule says old/unknown matrix versions block and matrix v4 is the single authority (`epics.md:159`).

**Impact:** Acceptance prose remains correct and blocks old versions, so this is not an architecture High; however, implementers may produce the named evidence against the historical matrix and leave v4 bootstrap/repair, kill-switch, replay, or governance rows unproved.

**Action: AUTOFIX EPICS.** Rename and expand both manifests to explicit v4 contract tests and require the current row/variant inventory plus old-version rejection. Do not change the immutable historical v1-v3 tables.

### M-R6-4 — Story 6.1's local dependency manifest omits its explicit Dapr security decision blocker

**Evidence:** `OD-DAPR-SECURITY-1` directly affects `StoryAuthorization:6.1` and Dapr Workflow adoption (`launch-readiness-register.md:97`); the spine forbids Workflow adoption until a parent-authoritative `1.18.7+` package-family upgrade or bounded Security exception (`ARCHITECTURE-SPINE.md:931`, `948`, `995`). The epics' global rule repeats that constraint (`epics.md:166`), but Story 6.1's Dependencies, Requirements, and Result name only story/external seams and `EXT-TOPOLOGY-1`, not the open decision (`epics.md:1831-1835`, `1866-1875`).

**Impact:** The register and global epic rule still block authorization, so this is not a High. The story-local manifest can nevertheless be read as complete and allow its Dapr Workflow evidence to be attempted before the security decision closes.

**Action: AUTOFIX EPICS.** Add `OD-DAPR-SECURITY-1` to Story 6.1's Dependencies and evidence Result/negative evidence as an explicit story-authorization blocker. Keep it open; do not treat the dirty `1.18.7` submodule checkout as approval.

## Low

### L-R6-1 — The spine's source list stops before the review rounds that produced its current amendments

**Evidence:** The frontmatter enumerates 2026-09-12 v3 review files but not the v4/v5 rubric reviews whose findings directly drove the current replay, rate/open/budget, readiness, and governance refinements. The memlog does preserve the amendment history.

**Impact:** Decision provenance remains reconstructible, but the curated source list does not point a later reviewer to the most immediate inputs for the current revision.

**Action: AUTOFIX DOCUMENTATION.** On the next architecture edit, add the material v4/v5 review inputs or replace the enumerated review list with a stable review-index reference. Do not list this v6 review as a source for the snapshot it reviews.

## Requested Control Audit

| Area | Result | Evidence / disposition |
| --- | --- | --- |
| Readiness self-bootstrap and repair | **Pass** | Matrix v4 gives EventStore observation a gate-free target-limited create/repair row, all other observations require `LR-EVENTSTORE`, and fixtures cover `NoStream`, stale/block repair, wrong target, and append failure. |
| Kill-switch pull and release | **Pass** | `PullContainment` can append during readiness failure only with target-bound confirmed SM-4 evidence or a recorded Release review decision; `ReleaseReviewed` stays normally gated and review-bound. |
| Public statuses | **Pass** | AD-15 and the PRD use the same interaction/proposal/readiness vocabularies; shipped legacy proposal-duplicating values are explicitly delivery debt, not asserted as compliant. |
| Open-interaction lease | **Pass** | The interaction-owned decision protocol now includes all terminal pre-acceptance outcomes, including `SafetyFailed`, distinguishes abort from committed release, carries exact revisions/acknowledgements, and has a profile-derived preparation deadline. |
| Rate admission | **Pass** | One interaction-owned ordinal/id, two preparations with one shared stored deadline/profile, mutually exclusive commit/abort decisions, ledger acknowledgements, and recovery to the recorded decision close the split-ledger race. |
| Budget reservation/recovery | **Pass** | `BudgetDispositionDecided` is mutually exclusive; invocation settlement is atomic with invocation authorization; absence never authorizes release; ledgers receive event revisions and recovery follows the durable decision. |
| Trusted replay owner/key | **Fail** | The system-tenant issuer/nonce key, target binding, non-public registrar, conditional-create conflict handling, rotation-stable logical id, and lost-ack recovery are bound, but H-R6-1 leaves ordering contradictory and H-R6-2 leaves denial auditing without an authorized writer. |
| Export/hold/deletion fence | **Pass at architecture scope** | `ProtectionFence` serializes intent; partial export inventory is frozen before writes; commit waits for artifact/manifest/index acknowledgement; cleanup holds the fence until every possible copy is purged/key-destroyed/proven absent; deletion remains blocked by overlapping hold/export state and open policy decisions. |
| Unresolved decisions | **Pass as visible blockers** | Hold/deletion precedence, export lifecycle, Dapr security, and sprint-history choices are materialized as versioned open records with closed affected evaluations. No Product/Governance/Security/Delivery outcome is invented. |
| Implementation-debt separation | **Pass with M-R6-4** | The spine correctly describes absent runtime, replay, ledger, fence, status, Dapr, and legacy-test work as delivery debt. The Story 6.1 local manifest should repeat its already-authoritative decision blocker. |

## Authoritative Prior-Finding Closure Audit

| Authoritative finding family | v6 disposition |
| --- | --- |
| C-1 weaker safety policy | **Closed.** AD-20 requires conjunctive current-plus-snapshot evaluation, stable evaluation identities, and fail-closed policy/version evidence. |
| C-2 hold/deletion exclusion | **Closed at architecture scope.** The tenant `ProtectionFence` and open decision record prevent irreversible work until the late-hold policy is approved. |
| C-3 bootstrap/repair | **Closed.** Matrix v4 supplies target-aware bootstrap/repair and a fail-closed EventStore observation row. |
| H-1 approver recheck; H-2 safety rescan; H-3 human-only approvers | **Closed.** Durable single-flight ownership, epochs/cursors/leases, fresh human binding/liveness, and unavailable-evidence behavior are bound. |
| H-4 mixed ledger lifetimes | **Closed.** Rate, open-concurrency, and budget state have distinct owners and complete interaction-owned decision/ack/recovery protocols. |
| H-5 human identity; H-6 proposal index; H-7 Conversations seam atomicity | **Closed.** Stable human actor identity, source-revision high-water repair, and the independently optional retraction seam are bound. |
| H-8 envelope cryptography/replay | **Original finding closed; new contradictions H-R6-1/H-R6-2.** Canonical input, expiry, rotation, revocation, platform-wide issuer/nonce uniqueness, registrar scope, and retention exist; ordering and denial-audit authority remain inconsistent. |
| H-9 export lifecycle | **Closed at architecture scope.** Immutable storage/key/index/manifest/purge invariants and fence serialization are binding; Product-owned lifetime choices remain explicit blockers. |
| H-10 Dapr reality | **Closed.** Root-authoritative transitive `1.18.5` exposure is distinguished from the dirty `1.18.7` checkout and future Workflow adoption; `OD-DAPR-SECURITY-1` remains open. |
| H-11 public parity claim | **Closed.** Missing current vocabulary is classified as delivery debt rather than claimed shipped. |
| H-12 sprint/dependency history | **Closed as a visible Delivery decision.** `OD-SPRINT-5.1-5.2-1` blocks affected story authorization without changing architecture or dependency commitment. |
| v5 H-R5-1 open-lease abort | **Closed.** `SafetyFailed` and every other terminal pre-acceptance outcome are in the abort set; abort and committed release are distinct. |
| v5 H-R5-2 absence-based budget release | **Closed.** Mutually exclusive durable disposition facts replace absence as authority. |
| v5 H-R5-3 recursive replay registration | **Closed.** One platform-composed registrar is non-public, target/event/operation limited, and outside ordinary command envelopes. |
| v5 H-R5-4 tenant-scoped replay key | **Closed.** Replay lives under reserved tenant `system`, keyed by issuer/nonce, with target tenant bound in the record. |

## Good-Spine Checklist

| Criterion | Result | Reason |
| --- | --- | --- |
| Fixes the real divergence points at the level below | **Fail** | H-R6-1 and H-R6-2 permit incompatible literal implementations at the security admission/audit boundary. |
| Every AD Rule is enforceable and prevents its stated divergence | **Fail** | AD-30's required pre-idempotency nonce registration conflicts with the matrix's no-exception order, and its denial-audit command has no principal/writer that can obey the same closed rule. |
| Deferred/open items are safe to defer | **Pass with Medium governance debt** | All four ODs fail closed at the affected operation/authorization/RQ evaluations. Assumption dates and the posting timeout remain visible blockers, not defaults. |
| Named technology is verified-current | **Pass with explicit blocker** | The spine accurately distinguishes the authoritative Dapr `1.18.5` exposure from the dirty `1.18.7` checkout, records the `10.0.401` SDK and package deviations, and makes no build-success claim. |
| Brownfield ratification | **Pass** | Exact root gitlinks match the documented modules. Missing replay/ledger/fence/workflow/status behavior is classified as delivery debt; present source and package reality are not described as already conformant. |
| Binding PRD capability coverage | **Fail narrowly** | Product capabilities, safety, governance, recovery, statuses, and data-loss boundaries are covered, but the security admission/audit guarantee cannot be implemented consistently until the two Highs close. |
| Inherited parent constraints | **N/A** | No parent architecture spine or inherited AD set is declared; repository/project-context constraints are reflected. |
| Initiative structural breadth | **Pass** | Paradigm, boundaries, state/mutation, API/UI, integration, security/data, environment, deployment, operations, recovery, capacity, provider strategy, evidence, and open decisions are all decided or explicitly blocked. |
| Mechanical validity | **Pass** | Deterministic linter: zero findings on the frozen input. |

## Repository Reality And Debt Classification

Focused inspection confirms the parent repository's exact submodule gitlinks match the spine, while the dirty Builds checkout's Dapr `1.18.7` catalog is not parent authority. EventStore supplies the existing domain-command/aggregate substrate but no shipped Agents `TrustedEnvelopeReplay`, `OpenInteractionLedger`, split rate protocol, protection fence, or Dapr Workflow adoption was found. Current interaction-status contracts still include superseded proposal-duplicating values. Those absences are implementation/delivery debt already named by the spine; they are not the two High architecture contradictions above.

## Gate Recommendation

Do not ratify v6. Correct H-R6-1 by making nonce registration the explicit pre-target-idempotency exception while preserving idempotency-first for ordinary command rows. Correct H-R6-2 by binding one least-privilege non-human audit writer and durable outage/recovery semantics, or surface a Product/Security decision if the universal audit promise is to change. Preserve all AD ids and append amendments to the memlog. Keep the Medium/open-decision items visibly blocked, re-distill, and rerun the complete Reviewer Gate on a new frozen snapshot.
