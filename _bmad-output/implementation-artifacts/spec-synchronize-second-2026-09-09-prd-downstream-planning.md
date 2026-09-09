---
title: 'Synchronize Second 2026-09-09 PRD Downstream Planning'
type: 'chore'
created: '2026-09-09'
status: 'in-progress'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '4ec626d371e8ca52a85216d2d7b35dcb5b85e947'
context:
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-09-2.md'
  - '_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five downstream planning artifacts only partially reflect the governing PRD and the approved Sprint Change Proposal, while the working tree also contains overlapping Architecture and UX edits and a later approved 33-story backlog that postdates the proposal's historical 29-story count.

**Approach:** Reconcile only the named planning/register/backlog artifacts against proposal sections 4–6 and `prd.md`, applying missing clauses around readiness, dependencies, architecture, story ownership, and provenance while preserving later approved backlog additions and all unrelated working-tree changes.

## Boundaries & Constraints

**Always:** Treat `prd.md` as governing and leave it unchanged. Preserve the current 33 active stories (10/8/7/8 across Epics 5–8), exact tracker parity, Stories 5.9/5.10/6.8/7.7, their Agents Runtime Maintainer ownership, and the broader dependency consumers those approved stories require. Reconcile the existing uncommitted Spine changes additively. Keep Story 5.3 / `EXT-PROVIDER-1` open for Product because the evidence does not provide the required ruling/date. Keep dependencies uncommitted unless owner acceptance and required evidence exist.

**Never:** Do not edit runtime code, `prd.md`, `sprint-status.yaml`, UX artifacts, the approved proposal, or completed historical epics. Do not add, remove, renumber, or re-estimate stories. Do not invent Product decisions, dependency commitments, API names, targets, dates, commands, or Architecture retirement dates; do not narrow current consumer/coverage maps merely to reproduce the proposal's older inventory.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Dirty overlapping Spine | Existing uncommitted Architecture corrections | Add only missing approved clauses and retain the prior corrections byte-for-byte where not directly reconciled | Report every preserved overlap; never reset or overwrite |
| Inconclusive provider history | Adapter-free story intent plus tests touching substituted/deferred provider ports | Explicit open Product approval condition; neither Branch A nor B is recorded | No fabricated ruling or date |
| Historical count conflict | Proposal says 29; later approved epics/tracker contain 33 | Preserve 33 exact story identities and record the proposal wording as a historical authority conflict | No deletion or renumbering |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/launch-readiness-register.md` -- reuse the existing 18-gate and metric split; add blocker emitters, exact SM-7 contract, counter-metric/cadence language, and the complete initial measurement placeholder.
- `_bmad-output/planning-artifacts/external-dependency-register.md` -- complete availability semantics and six Conversations seams; invalidate expanded `EXT-HOST-1`; strengthen protection verification requirements; keep Story 5.3 disposition open and preserve actual consumers.
- `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md` -- preserve current uncommitted AD-5/AD-7/AD-22 and unrelated source/stack edits; add the remaining AD-7/12/14/15/17/22/30 and assumption-date gaps.
- `_bmad-output/planning-artifacts/epics.md` -- current executable backlog authority; preserve 33 stories and augment inventory, OQ map, UX-DR45, targeted criteria/dependencies, and explicit Agents Runtime Maintainer debt ownership.
- `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/update-report-2026-09-09.md` -- add the approved proposal-amendment trail only.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- read-only parity authority; currently 33/33 exact active-story slug parity.
- `_bmad-output/implementation-artifacts/spec-5-3-govern-provider-models-and-pricing-through-live-operations.md` and `src/Hexalith.Agents.Server/Ports/DeferredAgentGenerationProvider.cs` -- evidence that no live adapter shipped, but insufficient for a dated Product Branch B ruling.

## Tasks & Acceptance

**Execution:**

- [ ] `launch-readiness-register.md` -- finish the approved release-gate/blocker and metric-contract synchronization.
- [ ] `external-dependency-register.md` -- finish dependency semantics and artifacts, return the expanded host record to `Uncommitted`, and retain unresolved facts as blockers.
- [ ] `ARCHITECTURE-SPINE.md` -- merge the missing architectural clauses into the existing working-tree version without disturbing overlapping edits.
- [ ] `epics.md` -- align requirements, OQ citations, targeted story criteria, UX-DR45, dependencies, and debt ownership while preserving the 33-story backlog.
- [ ] `update-report-2026-09-09.md` -- append the approved factual provenance line.
- [ ] Run focused consistency, YAML, Markdown structure, story/tracker parity, stale-vocabulary, and final git-diff checks.

**Acceptance Criteria:**

- Given the governing PRD and approved proposal, when the five artifacts are inspected, then readiness, dependency, Architecture, backlog, and provenance contracts reflect sections 4–6 without a diff to `prd.md` or runtime code.
- Given missing owner acceptance or inconclusive Story 5.3 evidence, when dependency records are evaluated, then all nine remain `Uncommitted`, `EXT-HOST-1` has only historical prior commitment data, and the Branch A/B decision remains explicitly open.
- Given the later approved backlog, when `epics.md` is compared with `sprint-status.yaml`, then all 33 active story identities match bidirectionally and every implementation-debt story remains owned by the Agents Runtime Maintainer.
- Given the pre-existing dirty tree, when the final diff is reviewed, then unrelated UX edits and pre-existing Spine corrections remain present and every newly applied artifact change is distinguishable and reportable.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

The user's preservation instruction resolves execution in favor of the later approved 33-story backlog; the proposal's 29-story statement remains a historical authority conflict rather than a reason to rewrite current planning. Architecture target milestones already present in the overlapping edit are retained, while calendar retirement dates remain unset until owners accept them.

## Verification

**Commands:**

- `python3` YAML parsing plus a bidirectional `epics.md`/`sprint-status.yaml` story-key comparison -- expected: valid YAML and exact 33/33 parity.
- Focused Markdown heading, fence, and table-shape validation over the five edited artifacts -- expected: no structural errors introduced.
- `rg` checks for stale readiness metrics, old Spine binding ranges, committed host state, missing OQ-24..OQ-30 coverage, and unowned debt -- expected: only intentional historical references remain.
- `git diff --check` and scoped/full `git diff` review -- expected: no whitespace errors, no `prd.md`/runtime/tracker diff, and all baseline edits preserved.
