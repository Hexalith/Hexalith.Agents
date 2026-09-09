---
name: Hexalith Agents spine validation (round 2)
type: architecture-spine-validation
target: ARCHITECTURE-SPINE.md (updated 2026-09-09 09:45:59, status final, AD-1..AD-31)
repository_state: main @ 611bc5a (2026-09-09)
date: 2026-09-09
lint: 0 findings
lenses:
  - rubric walker v4 (good-spine checklist)
  - verified-current v4 (configured)
  - adversarial divergence v4 (configured)
  - brownfield drift v3 (ad hoc)
  - security / data integrity v4 (ad hoc)
---

# Spine Validation Report — 2026-09-09 (round 2)

## Gate verdict

**FAIL.** Deterministic lint is clean (0 findings), and this run independently re-verified — rather than
trusted — every fix the same-day "v3" update pass claimed had closed the 09:15 validation's findings;
those closures largely held (AD-29 fingerprinting, PartyId discipline, justification-gate coverage, the
prior LOWs). But two things happened since the spine was last finalized at **09:45:59** that this run
surfaces for the first time:

1. **The PRD moved out from under the spine.** `prd.md` was updated again at **10:08:58** — 23 minutes
   after the spine's last edit — and that update's own §8.1 explicitly names **three current spine
   rules (AD-5, AD-12, AD-22) as unresolved "Spine defects,"** with a precise fix list already written
   in `update-report-2026-09-09-2.md` (`F-SPINE-1..F-SPINE-4` and others). This is a moving-target
   problem, not sloppy work — the spine correctly answered the PRD as it stood at 09:45; it no longer
   does at 10:08. It is now the loudest, most actionable signal in this report.
2. **Two lenses found genuinely new problems the v3 rounds didn't catch**: adversarial review found the
   spine's own sequence diagram contradicts AD-8/AD-13 on whether Eligible-Approver resolution runs in
   Automatic mode, and a self-contradictory ordering clause in AD-21 that reuses the word
   "authorization" for a term AD-12/AD-13 already fixed to an earlier step; security review found
   AD-22's erasure ("Restrictive completion") claim never addresses the posted copy of erased content in
   Conversations — the same class of gap that was tracked for `LegalHold` (`ARCH-A-10`) was never
   extended to deletion.

Brownfield drift is unchanged in substance from the last check (all previously-tracked implementation
debt remains open, exactly as expected since no source code has been touched since `a191d24`), with one
new tracking-integrity problem: `sprint-status.yaml` marks Story 5.2 "done" while its own amended
acceptance criteria and required test are unmet in code.

| Lens | Verdict | Critical / High / Medium / Low | Review file |
| --- | --- | --- | --- |
| Rubric walker | **FAIL** | 3 / 4 / 2 / 0 | `reviews/review-2026-09-09-rubric-v4.md` |
| Verified-current | PASS WITH FINDINGS | 0 / 1 / 1 / 3 | `reviews/review-2026-09-09-verified-current-v4.md` |
| Adversarial divergence | **FAIL** | 2 / 2 / 1 / 2 | `reviews/review-2026-09-09-adversarial-divergence-v4.md` |
| Brownfield drift | FAIL TO RATIFY, fixable | 2 / 3 / 3 / 2 | `reviews/review-2026-09-09-brownfield-drift-v3.md` |
| Security / data integrity | PASS WITH FINDINGS | 1 / 1 / 2 / 0 | `reviews/review-2026-09-09-security-data-integrity-v4.md` |
| **Total** | | **8 / 11 / 9 / 7** (35) | |

Deterministic lint (`lint_spine.py`): 0 findings — no placeholders, no duplicate `AD` ids, every `AD`
carries Binds/Prevents/Rule, Stack versions pinned.

## Critical — close before any new command handler is built against AD-5, AD-12, AD-21, or AD-22

### C-1 (Rubric) — AD-5 contradicts FR-18 on system abandonment; the PRD calls this a "Spine defect"

Current AD-5: *"Abandon is legal from every awaiting-decision state and from `PostingFailed`, never from
`Approved` or `PostingPending`, which complete or fail on their own terms."* The 10:08 PRD update's §8.1
states this is a defect: FR-18 requires system abandonment from `Approved` and `PostingFailed` on a
detected removal, a `MessageId` lookup before any exit from `PostingFailed` (deciding `Posted`/
`LateConfirmed`), and an uninterruptible `PostingPending`.
**Fix:** amend AD-5 per `update-report-2026-09-09-2.md` `F-SPINE-2`.

### C-2 (Rubric) — AD-12 contradicts FR-28 on kill-switch semantics and trigger rigor; PRD calls this a "Spine defect"

Current AD-12 lets `Approved` proposals "complete... on their own terms" under a pulled kill switch and
states the Release Operator trigger as a raw threshold with no review/minimum-sample/
`InsufficientEvidence` machinery. FR-28 requires `Approved` proposals to wait unposted (only
`PostingPending` completes), plus a one-business-day trigger review over four rates with a minimum
sample scaled for small tenants.
**Fix:** amend AD-12 per `F-SPINE-4`.

### C-3 (Rubric) — AD-22 contradicts FR-24/FR-33 on the compliance-inspection second-party model; PRD calls this a "Spine defect"

Current AD-22 names the Tenant Agent Administrator as the sole inspection second party and gives
`ExportRequest`/`LegalHoldRelease` no second-party gate at all. FR-24/FR-33 require a computed
subject-set exclusion model with anti-collusion (no two Inspectors approving each other within 30 days),
Platform-Operator escalation when the Administrator is in the subject set, and prior-approval gates for
export and hold release.
**Fix:** amend AD-22 per `F-SPINE-3`.

### C-4 (Adversarial) — "Eligible Approver resolution": universal acceptance step, or Confirmation-mode-only?

AD-13's canonical FR-8 acceptance order lists Eligible-Approver resolution with no mode qualifier; AD-8
binds the predicate to "call time" unconditionally; AD-5 implies automatic-mode proposals need one too.
But the spine's own sequence diagram (line 374) scopes the step to `(confirmation mode)` only — a
genuine, business-behavior-changing fork, not a diagram slip. Two independently-built acceptance
pipelines diverge on whether an Automatic-mode Agent with zero configured Approvers gets every call
rejected up front (`NoEligibleApprover`) or runs freely with failures piling into `PostingFailed`.
**Fix:** add an explicit mode qualifier to AD-13's order (or strike "(confirmation mode)" from the
diagram) — either resolution works, the spine currently asserts both.

### C-5 (Adversarial) — AD-21 reuses "authorization" for a term AD-12/AD-13 already pinned to an earlier step

AD-13 fixes "authorization" as FR-8 acceptance step 1, before reservation (step 6); AD-12 states
"authorization gates run before every side effect." AD-21 nonetheless says budget reservation happens
"before safety, admission, and authorization" — self-contradictory on its face unless "authorization"
here silently means the later `ProviderInvocationAuthorized` event, which AD-21 never says. Two
compliant `BudgetLedger` handler implementations diverge on a security-relevant question: can an
unauthorized caller trigger a durable reservation side effect before being authorized at all?
**Fix:** in AD-21, replace "before safety, admission, and authorization" with the exact event name
`ProviderInvocationAuthorized`, not the bare word already claimed by AD-12/AD-13's step 1.

### C-6 (Brownfield, already-tracked) — AD-2's platform-scoped `ProviderCatalog`/`TenantProviderEnablement` still doesn't exist in code

`ProviderCatalogAggregate.cs`'s own doc comment still says "tenant-scoped," with a raw string-equality
admin check; `TenantProviderEnablement` has zero hits anywhere in `src/`/`test/`. Tracked as Story 5.3
(backlog) in the approved sprint-change proposal; unchanged since commit `a191d24` (2026-09-08).
**Fix:** implementation debt, already tracked — no spine amendment needed.

### C-7 (Brownfield, already-tracked + new tracking-integrity angle) — AD-4's `ConfigurationVersion` still not bumped by lifecycle events, and the sprint tracker now falsely claims it's done

`AgentActivated`/`AgentDisabled` in `AgentAggregate.cs` still carry no `ConfigurationVersion` argument.
**New this round:** Story 5.2's own amended acceptance criteria (`epics.md:1247-1248`) now require this
exact fix and name `AgentLifecycleConfigurationVersionTests` as required evidence — but
`sprint-status.yaml:88` marks Story 5.2 **`done`**, and no such test exists anywhere in `test/`. The two
tracking artifacts (`sprint-status.yaml` and `deferred-work.md` DW-4, which correctly keeps this open)
now disagree with each other, not just with the code.
**Fix:** implementation debt, already tracked (DW-4) — additionally, correct `sprint-status.yaml` so it
doesn't assert `5.2: done` against unmet current acceptance criteria.

### C-8 (Security) — AD-22's erasure completeness claim never addresses the posted copy in Conversations, and — unlike LegalHold — this gap isn't even tracked

AD-22's "Restrictive completion" bar defines erasure as complete once every protected event unprotects,
projections purge, and a tombstone remains — and never mentions Conversations at all. AD-6 confirms
Agents cannot write to Conversations streams, so Agents has no mechanism to reach a posted copy even if
it wanted to. Unlike the `LegalHold` case (which at least has `ARCH-A-10` and a register-gated evidence
clause), the erasure direction has no tracked residual risk anywhere — a builder or auditor can
correctly satisfy AD-22's completeness bar to the letter while the exact erased content remains
permanently plaintext-readable as a posted Conversation Message.
**Fix:** extend AD-22's completion definition to explicitly disclose the posted-copy exception, and
extend `ARCH-A-10`/`LR-AUDIT-PROTECTION-DELETION` to cover `DeletionRequest` symmetrically with
`LegalHold`.

## High

| # | Finding | Source |
| --- | --- | --- |
| H-1 | Frontmatter `binds`/`sources` are stale: PRD now runs FR-1..**FR-34**/OQ-1..**OQ-30**, spine still cites FR-33/OQ-23 and the superseded first same-day update report; FR-34 (payload-protection gating) has zero AD coverage though AD-14/AD-17 already imply the right fail-closed behavior. | Rubric (`F-SPINE-1`) |
| H-2 | AD-2's `ConversationAgentState` is missing `BlockVersion` and `MirrorPending`, and the five-state machine (`NeverJoined → Joined → (ExternallyRemoved\|Blocked) → ReadmitPending → Joined`) required by the PRD's resolved OQ-16/OQ-25 stays implicit rather than stated. | Rubric |
| H-3 | AD-7 still presents the Party link/replace commands as live identity-provisioning paths; PRD's OQ-25/OQ-28 require deprecate-and-reject (create-only provisioning). | Rubric |
| H-4 | AD-15's FR-25 blocked-call counter list omits rate-limit-blocked calls, though AD-21 defines rate limiting as a distinct rejection reason. | Rubric |
| H-5 | The pinned `.NET SDK 10.0.301` feature band appears to have just fallen out of active servicing (last monthly bundle with no `10.0.3xx` entry shipped 2026-09-08, the day before the spine's own "updated" stamp); `ARCH-A-4` bundles this with low-urgency test-stack debt rather than flagging it as a build-supportability risk. | Verified-current |
| H-6 | `AgentReadinessStatus` has no fixed shape — AD-15 describes it as a bare enum, AD-10/AD-17 describe it as a composite wrapper containing `ProviderReadinessResult`; incompatible contract shapes for two independently-built teams. | Adversarial |
| H-7 | AD-21 claims `CurrencyMismatch` "lives in the AD-10 tenant join," but AD-10's own blocker enumeration never mentions currency at all — a stale cross-reference two teams would resolve differently (readiness-time vs. reservation-time rejection). | Adversarial |
| H-8 | AD-29's single `AgentsIdentity` canonicalizer still doesn't exist in code; five-plus separate identity helper classes remain. | Brownfield (already-tracked, Stories 6.4/7.1-7.3) |
| H-9 | Structural seed still lists no `Server/Aggregates`/`Application/Tools`, but both exist on disk (placeholder-only); `Hexalith.Agents.IntegrationTests` still absent from `test/` and the `.slnx`. | Brownfield (already-tracked, Story 5.6) |
| H-10 | AD-30's HMAC-tagged trusted-extension requirement has no code counterpart — the provider-admin gate is still a plain string-equality check the aggregate's own doc comment calls "transitional." | Brownfield (already-tracked, Story 5.4/DW-2) |
| H-11 | The `Platform` principal (Platform Operator — the widest cross-tenant powers of any principal kind) has no stated role-freshness re-check, unlike `Administrator` and (as of this round's earlier fix) `User`. | Security |

## Medium (9 total — see review files for full detail)

- Architecture Assumptions table lacks target retirement dates for Architecture-owned rows (Rubric).
- `sources` list points to the superseded first same-day PRD update report without a staleness signal (Rubric).
- Stack table's deviation tracking is incomplete: `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio` also diverge from the catalog and aren't named (Verified-current).
- Conversation-block set/clear command has no declared lock-bearing-family membership in AD-12/AD-22's closed-form taxonomy (Adversarial).
- AD-19's "reserves no folder" rule is directly contradicted by the existing (empty) `Application/Tools/` folder (Brownfield, new framing).
- `IProjectionChangeDetailNotifier` required by AD-15 as the projection-confirmation nudge is referenced nowhere in code; shipped polling is a bare 250ms timer loop (Brownfield, new).
- AD-30's five-policy FR-33 mapping is missing `Agents.PlatformOperator` entirely from the codebase (Brownfield, new).
- Legal hold vs. Conversations retention — re-verified still genuinely open, honestly tracked as a residual risk, unchanged from v3 (Security).
- `AuditInspection` permits "read now, justify later" access with an undefined review-window length and no defined consequence for a missed/failed post-hoc review (Security).

## Low (7 total)

`bunit` pin one minor release behind current (carryover); MediatR's dual license undocumented at
catalog level (informational); Platform host's Aspire pin trails the workspace catalog by one patch
(informational, outside spine authority) — Verified-current. `AgentCall`/`AgentResponse` named but
never bound by any Rule; `GenerationFailureRecord` has no AD-29 identity derivation — Adversarial.
Confirmation-only entries (submodule pins, register gate ids, matrix v2 all verified correct; no further
new drift found) — Brownfield.

## What's already closed (re-verified, not re-litigated)

This round independently re-checked rather than trusted every claim from the 09:15 validation and the
subsequent "v3" fix round: AD-29's command-payload fingerprint keying, PartyId-reference discipline
(AD-7/AD-2/AD-8/AD-30), the AD-22 justification-gate family coverage (all 9 lock-bearing families),
`ARCH-A-8`'s Fluent UI Blazor v5-RC tracking, AD-9's Agent Framework/Dapr Agents GA claims, and the two
prior LOW rubric findings — all confirmed genuinely closed against the current text.

## Recommendation

Roll this report into an Update pass. Priority order:
1. **C-1/C-2/C-3 (AD-5, AD-12, AD-22 vs. the 10:08 PRD update)** — `update-report-2026-09-09-2.md`
   already contains the exact fix text (`F-SPINE-1..4` and named clauses); this is closing a gap the
   PRD itself already specified, not fresh design work.
2. **C-4/C-5 (adversarial contradictions)** and **C-8 (erasure/Conversations gap)** — spine-text-only
   fixes, no PRD dependency.
3. **C-6/C-7 (brownfield)** — implementation debt already tracked in Stories 5.2–5.6; the new
   `sprint-status.yaml` correction should land alongside.
4. High-tier items, same pass.
