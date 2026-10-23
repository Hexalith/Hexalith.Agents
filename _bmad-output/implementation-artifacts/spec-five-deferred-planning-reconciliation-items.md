---
title: 'Reconcile five deferred planning decisions'
type: 'chore'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five planning entries still report unresolved decisions even though the current Architecture Spine, PRD and Experience Spine already specify the answers. The stale ledger can send subsequent implementation back through decisions that have landed.

**Approach:** Complete the requested five-item batch by reconciling Platform Operator authority, versioned DigestKey rotation, overlapping retry pauses, removal/block retry transitions, and administrative-retry operation-family locking against current authoritative planning evidence. Preserve the original deferral text, link each closure to a reviewable evidence record, independently review the changes, commit and push main. Closure establishes the documented contract; runtime delivery, Security qualification and production acceptance retain their existing gates.

</frozen-after-approval>

## Implementation Notes

- Baseline: clean main at `ece9cdbc2d4648b3c629e794197bc8a2194852ef`, obtained directly from Git. User explicitly requests five deferred items and pushing main; this is one ledger-reconciliation goal with five entries, not an epic story. No sprint story key applies.
- Route facts: small reversible documentation changes only, no unresolved intent choices, new API, migrations, deployment or runtime behavior. All selected decisions already exist in the governing documents. Preserve their wording and every submodule pointer.
- Evidence: current `ARCHITECTURE-SPINE.md` AD-30 distinguishes global-administrators from tenant-scoped tenants; AD-22/AD-29 and ARCH-A-12 define versioned digest keys; AD-12, PRD FR-18 and EXPERIENCE define union pause intervals; AD-5/AD-7, FR-18 and EXPERIENCE distinguish removal/block abandonment from transient posting failures; AD-5/AD-12 and EXPERIENCE classify public ProposalResolution separately from workflow-only ConversationPosting.
- Files: this spec, `_bmad-output/implementation-artifacts/deferred-work.md`, and a dated reconciliation record beside it. Record the full source revision and precise source sections/lines with links, retained assumptions and qualification limits. Close exactly the five matching entries with date, resolution and evidence links; leave all other ledger text intact.
- Verify each source claim in context, source provenance, link targets, exactly five status transitions and no unrelated edits. Documentation-only validation uses these checks and `git diff --check`; do not add tests that mirror prose or claim .NET/runtime verification. Run the workflow's context-free blind review, triage all findings and correct concrete documentation defects. Validate the exact Conventional Commit message with commitlint, fetch and inspect divergence before pushing main.

- Investigation found EXPERIENCE omitted two already-governing lookup refinements: pre-post-only failures skip lookup, and typed deleted/removed answers permit abandonment only. Applied that narrow PRD/AD-5 synchronization, updated UX source/date provenance and appended its existing memory log. Added the dated evidence record and exactly five ledger closures. ARCH-A-12 remains expressly unconfirmed before RQ-1; no runtime or global UX currency claim is made.

- Initial verification passed: exactly five new done statuses tied to this spec, every other byte of the ledger preserved, all 16 new relative Markdown links/anchors and the UX source reference resolve, exactly five intended files changed, and the recorded source revision is canonical. `git diff --check` and pinned commitlint pass. Fetch reports zero divergence.

## Review Triage Log

- R1 — medium / patch: the preceding CanAdministrativeRetry definition required usable lookup even for no-post failures. Qualified availability with the existing no-post exception, preserving all other guards.
- R2 — medium / patch: confirmation contents required a lookup outcome without defining the skipped case. Require an explicit skipped/no-post indication, without adding a public status or inferring an unavailable result.
- R3 — medium / patch: the UX wording omitted FR-18's retention qualification for a message that may remain unreadable. Preserve Conversations retention and scope the prohibition on abandonment to a positively found approved-version message.
- R4 — low / patch: the automatic posting summary retained the obsolete three-outcome count. Refer to governed outcomes and the no-post exception instead.

- Final verification passes after all corrections: the original ledger is preserved apart from exactly five closures; all 16 added relative links/anchors and the UX source resolve; only the five intended files change; new-file whitespace, `git diff --check` and pinned commitlint pass. Independent reviewer recheck confirms all four findings resolved with no remaining contradictions. No findings were deferred. No application tests, runtime qualification, Security approval, sprint change or submodule update is claimed.
