# Architecture Spine Rubric Review — 2026-09-12 (v2)

## Verdict

**FAIL — the spine is mechanically complete and covers the initiative's structural breadth, but it is not yet a safe convergence contract.** One critical content-safety rule contradicts the binding PRD, and three high-severity source capabilities remain insufficiently bound for independently implemented units.

Finding count: **1 Critical, 3 High, 1 Medium**.

## Review Scope And Evidence

- Target: `../ARCHITECTURE-SPINE.md` (`status: final`, `updated: 2026-09-12`, initiative altitude).
- Binding sources inspected: current PRD, external-dependency register, launch-readiness register, architecture memlog, implementation conventions, root build/package configuration, and focused brownfield contracts/aggregate code.
- Deterministic check: `lint_spine.py` reports `ok: true`, zero findings.
- No parent architecture spine is declared or inherited; inherited-invariant conflict review is therefore not applicable.
- This review did not modify the spine, sources, registers, or code.

## Critical

### C-1 — AD-20 permits weaker safety evaluation than the binding PRD

**Evidence**

- AD-20 says a Provider transport retry **never re-evaluates safety**, and says regeneration, approval, and pre-post evaluate under the current policy pair only when it is at least as restrictive as the snapshot pair, otherwise under the snapshot pair alone (`ARCHITECTURE-SPINE.md:296`).
- PRD FR-26 requires content to pass **every applicable policy version** at each retry, approval-time check, and pre-post check: both the attempt snapshot and the then-current active policy (`prd.md:680`, `prd.md:684`). FR-27 repeats the then-current requirement at approval (`prd.md:707`), and OQ-9 explicitly requires both checks to pass (`prd.md:1060`).

**Why this fails the rubric**

This is not only missing detail. It selects behavior the source forbids. A policy can be incomparable with the snapshot—looser in one category and tighter in another. AD-20's “snapshot exactly otherwise” branch then ignores the newly tightened category. A transport retry can likewise reach the Provider after the active policy changed without a fresh safety result. Separately built workflow, safety-adapter, approval, and posting units can all comply with the spine while violating FR-26/FR-27.

**Impact**

Unsafe content can reach a Provider or posting path under a policy that the current Product/Security contract requires to block. The audit trail can also claim the prescribed four-point/current-policy protection while recording only one selected policy pair.

**Action: AUTOFIX.** Amend AD-20 so every retry, approval-time check, and pre-post check passes both the snapshot pair and the then-current active pair (plus applicable stricter mode policy). If optimization may collapse provably ordered pairs into one evaluation, bind that only as an equivalence optimization whose result is identical to evaluating both; incomparable pairs must never select just one. Reconcile AD-13's fixed fingerprint/retry rule at the same time so a current-policy change has one unambiguous fail-closed outcome.

## High

### H-1 — The scheduled Eligible-Approver re-check has status tokens but no enforceable lifecycle rule

**Evidence**

- AD-8 binds configuration, call, edit, regeneration, approval, and interactive access checks, but it never binds the scheduled re-check owner, cadence, freshness condition, two-pass empty-set guard, or state-specific consequences (`ARCHITECTURE-SPINE.md:198-202`).
- AD-5 names `NoEligibleApprover` among system-abandonment reasons without limiting it to awaiting proposals or defining the evidence threshold (`ARCHITECTURE-SPINE.md:168`). AD-15 merely exposes `ResolutionEmptyPending` and `ResolutionUnavailable` tokens (`ARCHITECTURE-SPINE.md:262-264`).
- PRD FR-7 requires a single-flight scheduled pass over every awaiting and `PostingFailed` proposal; an hourly default configurable from 15 minutes to 24 hours; freshness no older than one cadence; two authoritative empty passes at least one cadence apart before abandoning an awaiting proposal; marker-only behavior for `PostingFailed`; and `ResolutionUnavailable` without abandonment on unavailable evidence (`prd.md:262-268`).

**Why this fails the rubric**

The source describes a load-bearing state transition intended to prevent proposals being stranded or destroyed on absent evidence. The spine fixes neither the owner nor the transition protocol. One workflow can abandon after one empty or unavailable lookup while another waits for two authoritative passes; both could point to the current spine.

**Impact**

Awaiting proposals can be abandoned prematurely, `PostingFailed` proposals can be terminalized contrary to FR-18, or the re-check may never run at all.

**Action: AUTOFIX.** Fold the PRD protocol into AD-8/AD-5, tagged with `[ASSUMPTION A-20]` for the still-unretired cadence value. Bind scheduling ownership, single-pass concurrency, accepted evidence, secondary-projection freshness, markers, state-specific transitions, and retry-on-unavailability.

### H-2 — Verdict-cache invalidation is named, but re-scan ownership and call behavior are unbound

**Evidence**

- AD-20 binds a cache key and invalidation on policy publication, but does not say whether the cache is shared/durable across replicas, who performs re-scan, how concurrency is bounded, or what an Agent Call does while its Conversation is pending re-scan (`ARCHITECTURE-SPINE.md:296`).
- AD-12 refers to `RescanPending` only when classifying an operational trigger (`ARCHITECTURE-SPINE.md:234`); AD-15 instead lists the public token `ContextUnavailable`, not the PRD's `ContextReadUnavailable` with `RescanPending` sub-reason (`ARCHITECTURE-SPINE.md:262`).
- PRD FR-27 requires policy publication and HMAC-key rotation to start a per-tenant-concurrency-bounded background re-scan; a call waits for its Conversation's result or fails closed as `ContextReadUnavailable(RescanPending)` and never uses a stale verdict (`prd.md:708`).
- Brownfield evidence confirms the distinction is not already supplied by code: the public enum currently has only `ContextUnavailable` (`src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionContextBlockReason.cs:22`), and current domain/server tests assert that coarse value.

**Why this fails the rubric**

Invalidation across a distributed host plus bounded background rebuilding is an ownership and coordination decision, not a detail two lower-level units can safely invent. A process-local invalidation implementation, a shared cache, and an EventStore-backed re-scan projection have materially different consistency behavior.

**Impact**

Calls can consume stale safety verdicts, remain blocked indefinitely, or expose incompatible error/status vocabularies across Server, API, UI, and product metrics.

**Action: DISCUSS.** Select and bind the authoritative cache/re-scan owner and cross-replica invalidation model, then state the bounded worker protocol and the exact `ContextReadUnavailable(RescanPending)` public/metric mapping. Keep implementation migration explicit because the current contract is coarse.

### H-3 — AD-8 omits the human-only Approver invariant

**Evidence**

- AD-8 says predefined `PartyId`s, tenant roles, and Conversation Facilitators contribute current Participants, but it does not require them to resolve to human Parties or reject a configured organization/AI Party (`ARCHITECTURE-SPINE.md:198`).
- PRD FR-7 requires every Approver source to resolve to human Parties only: organization or AI Parties are typed configuration rejections, and a Facilitator/role holder that resolves non-human contributes no Approver (`prd.md:254`).
- Brownfield public shape carries only `Kind`, `PartyId`, and `TenantRole` (`src/Hexalith.Agents.Contracts/Agent/ApproverPolicySource.cs:19-22`); aggregate validation is structural (`src/Hexalith.Agents/Agent/AgentAggregate.cs:729-786`). The missing invariant therefore cannot be inferred from compliant code and must be stated at the orchestration/Parties-adapter boundary.

**Why this fails the rubric**

Approver eligibility is a security/segregation boundary. Without the invariant, configuration, runtime resolution, UI validation, and audit can choose incompatible interpretations of an organization or AI Party.

**Impact**

A non-human Party can be accepted as an approval authority, or different surfaces can disagree about whether a policy is valid and whether a proposal has any Eligible Approver.

**Action: AUTOFIX.** Add the human-Party constraint to AD-8 at configuration and every resolution point, with typed safe failure behavior and a Parties-owned type/liveness lookup that fails closed when unavailable.

## Medium

### M-1 — The final spine still carries historical correction layers instead of one current rule

**Evidence**

- AD-2 first defines `MirrorPending` as an independent flag, then a later paragraph supersedes that shape with `CurrentMirror` (`ARCHITECTURE-SPINE.md:144-146`).
- AD-12 presents a base lock-family list and later appends a tenth family through a “third/fourth update” correction (`ARCHITECTURE-SPINE.md:232-238`).
- AD-22 describes all nine lock-bearing families and then supersedes that count with `DataHandlingAcceptance` as the tenth (`ARCHITECTURE-SPINE.md:308-310`). Similar “Second-update” and “Third-update” labels remain in AD-14 and AD-17 (`ARCHITECTURE-SPINE.md:254`, `ARCHITECTURE-SPINE.md:278`).

**Why this fails the rubric**

The supersession clauses make the intended answer recoverable, so this is not another semantic Critical/High. But the artifact is supposed to be a terse build substrate distilled from the memlog. Requiring builders to replay document history makes enforcement harder and creates avoidable divergence when a consumer reads the first normative-looking rule but misses its later amendment.

**Impact**

Generated implementation plans, tests, and contract inventories can reproduce obsolete shapes or counts even while citing the correct AD ID.

**Action: AUTOFIX.** Re-distill each affected AD into one present-tense Rule and leave change history in `.memlog.md`. Preserve AD IDs and current decisions; remove superseded sentences and update-round labels.

## Checklist Disposition

| Good-spine criterion | Result | Evidence |
| --- | --- | --- |
| Real divergence points at the level below | **Fail** | C-1 and H-1 through H-3 leave incompatible implementation choices in safety and approval boundaries. |
| Every Rule enforceably prevents its stated divergence | **Fail** | AD-20 permits source-forbidden safety behavior; AD-8 lacks the scheduled transition and human-Party constraints. |
| Deferred items are safe to defer | **Pass** | The five Deferred rows (`ARCHITECTURE-SPINE.md:862-871`) are outside V1, delegated to platform/governance, or fail closed on an uncommitted seam. None is needed to choose current V1 behavior once C-1/H-1/H-2/H-3 are fixed. |
| Named technology is verified-current or explicitly risk-flagged | **Pass with open blockers** | Exact checked-in versions are listed (`ARCHITECTURE-SPINE.md:547-577`); the superseded Dapr family is blocked by `ARCH-A-15`, Fluent UI RC by `ARCH-A-8`, and test-stack deviations by `ARCH-A-4`. No unflagged version drift was established by this rubric pass. |
| Brownfield ratification | **Pass with planned migration debt** | Root SDK/package facts and package direction match the Stack/conventions; aggregate code is in the domain assembly. Known tenant-catalog, deterministic-id, versioned-route, and retired-contract migrations are acknowledged by the schema/evolution and deprecate-and-reject conventions (`ARCHITECTURE-SPINE.md:516-533`). H-2/H-3 identify source rules that current code cannot supply implicitly. |
| Binding source capability coverage | **Fail** | PRD FR-26/FR-27 and FR-7 gaps are C-1 and H-1 through H-3. Other reviewed capability groups map through `ARCHITECTURE-SPINE.md:794-817`. |
| Inherited parent constraints | **N/A** | Initiative spine; no parent spine or inherited AD set is declared. |
| Initiative structural breadth | **Pass** | Paradigm/boundaries/state/mutation/data ownership, public surfaces, security/governance, integration, recovery/capacity/evidence, and operational/environment/provider envelopes are all decided, delegated to authoritative registers, or explicitly blocked. AD-16 covers platform hosting for local/test/deployed environments (`ARCHITECTURE-SPINE.md:266-270`); AD-23 covers restore (`ARCHITECTURE-SPINE.md:312-316`); Provider/host selection remains fail-closed through `EXT-*` prerequisites (`ARCHITECTURE-SPINE.md:819-836`). |
| Mechanical validity | **Pass** | Linter returned zero findings; AD IDs and Binds/Prevents/Rule structure are mechanically complete. |

## Gate Recommendation

Do not treat the spine as a final implementation substrate until C-1 and H-1 through H-3 are resolved. C-1, H-1, and H-3 can be folded directly from the binding PRD without a new Product decision. H-2 needs an architecture choice for distributed cache/re-scan ownership before its already-specified PRD behavior can be implemented consistently. M-1 can be handled in the same re-distillation pass.
