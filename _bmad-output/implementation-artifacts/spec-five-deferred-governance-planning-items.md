---
title: 'Reconcile five deferred governance planning items'
type: 'chore'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five governance planning entries still describe unresolved policy or missing UX branches that the current PRD, Architecture Spine and epics already govern. Their stale status can cause implementation to reopen settled authority, metric and hold-release decisions.

**Approach:** Complete the requested five-item batch by reconciling Facilitator block-clearing authority, inspection refusal when no eligible Administrator can be established, blocked/unavailable metric counting, restrictive partial hold-release recovery, and the UX's alternate inspection approvers. Preserve the original deferrals, document precise supporting evidence, clarify the inspection UX without granting new authority, independently review, commit and push main. Closure establishes planning rules; runtime implementation, dependency acceptance and production qualification keep their existing gates.

</frozen-after-approval>

## Implementation Notes

- Baseline: clean main at `da7b1075bedae3b3f821888e410a6e8274e8b2a8`, obtained directly from Git. User requests five deferred items and pushing main. One ledger-reconciliation goal; no epic story key or sprint-status change applies.
- Route facts: small reversible documentation changes, no unresolved product choice, new API, migration, deployment or runtime side effect. Three rules are already explicit; inspection refusal follows existing fail-closed authority and self-approval rules, while partial unpin uses the existing staged guard-release protocol rather than a new all-key atomic operation or re-pin policy.
- Code map: PRD FR-24 and FR-28; Architecture AD-7, AD-12 trigger calculation, AD-2 guard release, AD-22 inspection/expiry/hold release and AD-30 actor separation; epics Story 8.1 release/recovery; EXPERIENCE Operational status and Audit evidence rules. Keep assumption rows, runtime sources and every submodule pointer unchanged.
- Files: this spec, a dated governance reconciliation record beside it, deferred-work.md, and the existing EXPERIENCE.md plus its append-only .memlog.md. Close exactly the five selected entries while preserving their original summary/evidence. Clarify server-computed approver selection, current Administrator turnover versus historical subjects, same-human refusal and missing/ambiguous authority refusal in UX. Preserve prior/post-hoc scope and anti-collusion limits.
- Verification: compare each closure with governing clauses in context, check provenance and relative links/anchors, prove exactly five new done statuses with every other ledger byte preserved, and run git diff --check. Documentation changes need no application tests. Run the workflow's context-free blind review and triage every finding; validate the exact Conventional Commit message with commitlint, fetch and inspect divergence, then push main as requested.

- Initial validation passed: exactly five three-line ledger insertions and every original ledger byte retained; all 17 added relative links/anchors plus the UX source resolve; canonical baseline matches; exactly five intended documentation files changed with clean whitespace. No application/runtime tests apply. The documented closures preserve existing Product assumptions and owner qualification gates.

- Blind review exposed two derived-document conflicts with governing PRD FR-24: Story 8.8 used an unrestricted otherwise branch, while AD-22 limited subject membership to historical Administrator roles. Synchronized those clauses to full PRD subject-set membership; unresolved authority and Inspector-self alone remain refusals. The same UX branch correction now specifies discovery/approval/review on the existing audit list/detail and assigns missing public result fields to Story 8.8, with deferred rendering until implemented. The footprint is seven documentation files, adding epics.md and ARCHITECTURE-SPINE.md; no runtime API or new product authority is introduced. Architecture updated/source provenance is refreshed without changing its assumption index or decisions.

## Review Triage Log

- R1 — medium / patch: Story 8.8 selected alternate approvers for any ineligibility. Replaced otherwise with the governing PRD Administrator-in-subject condition; authority uncertainty and same-human approval remain refusals.
- R2 — medium / patch: AD-22 considered only historical TAA subject membership. Matched FR-24 full computed subject membership, including a current Administrator recorded earlier as caller; UX and story now use the same predicate.
- R3 — medium / patch: alternate reviewers lacked discovery and an approval/review interaction. Added viewer-scoped rows and prior-approval/post-hoc controls on the existing audit list/detail under AuditInspection, with no unrelated case inventory or protected content and an explicit Story 8.8 handoff.
- R4 — medium / patch: server-computed eligibility lacked a contract handoff. Added required result/list/command information and ownership to the UX contract table and Story 8.8 acceptance criteria; absent contracts render deferred.
- R5 — low / patch at finalization: verification provenance must include actual review and final checks before publication. Initial document results were already recorded; this log records all five findings and final verification follows before marking done/committing. The oneshot route leaves review_loop_iteration at zero by design.
- R6 — medium / patch: the new generic review-window recheck could refuse late review and strand the persistent unreviewed signal. Story 8.8 and UX now explicitly permit eligible late review, clear only the current unreviewed signal, preserve the missed-window finding and grant no new content read. The contract handoff records that timing evidence.

Final verification: independent reviewer recheck confirms all six findings resolved with no further concrete contradiction. The final document gate verifies exactly five closures with every original ledger byte retained, all 18 added relative links/anchors and both source references, the seven intended documentation files, append-only UX memory, canonical baseline and clean whitespace. Pinned commitlint passed for the exact commit message; fetch found zero divergence. No review findings were deferred. No application test, runtime delivery, custodian qualification, dependency acceptance or new Product decision is claimed.
