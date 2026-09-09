---
title: 'Synchronize Second 2026-09-09 PRD Downstream Planning'
type: 'chore'
created: '2026-09-09'
status: 'done'
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

- [x] `launch-readiness-register.md` -- finish the approved release-gate/blocker and metric-contract synchronization.
- [x] `external-dependency-register.md` -- finish dependency semantics and artifacts, return the expanded host record to `Uncommitted`, and retain unresolved facts as blockers.
- [x] `ARCHITECTURE-SPINE.md` -- merge the missing architectural clauses into the existing working-tree version without disturbing overlapping edits.
- [x] `epics.md` -- align requirements, OQ citations, targeted story criteria, UX-DR45, dependencies, and debt ownership while preserving the 33-story backlog.
- [x] `update-report-2026-09-09.md` -- append the approved factual provenance line.
- [x] Run focused consistency, YAML, Markdown structure, story/tracker parity, stale-vocabulary, and final git-diff checks.

**Acceptance Criteria:**

- Given the governing PRD and approved proposal, when the five artifacts are inspected, then readiness, dependency, Architecture, backlog, and provenance contracts reflect sections 4–6 without a diff to `prd.md` or runtime code.
- Given missing owner acceptance or inconclusive Story 5.3 evidence, when dependency records are evaluated, then all nine remain `Uncommitted`, `EXT-HOST-1` has only historical prior commitment data, and the Branch A/B decision remains explicitly open.
- Given the later approved backlog, when `epics.md` is compared with `sprint-status.yaml`, then all 33 active story identities match bidirectionally and every implementation-debt story remains owned by the Agents Runtime Maintainer.
- Given the pre-existing dirty tree, when the final diff is reviewed, then unrelated UX edits and pre-existing Spine corrections remain present and every newly applied artifact change is distinguishable and reportable.

## Implementation Notes

- Applied the approved sections 4–6 planning synchronization only; no runtime source, test, PRD, sprint-status, or UX content was changed by this task.
- Preserved the live 33-story backlog (10/8/7/8) and the exact 33/33 tracker slug set, including Stories 5.9, 5.10, 6.8, and 7.7 and their Agents Runtime Maintainer ownership.
- Kept the Story 5.3 / `EXT-PROVIDER-1` Branch A/Branch B disposition open because the inspected evidence does not contain the required Product ruling or date.
- Kept all nine dependency records `Uncommitted`; the expanded `EXT-HOST-1` target, date, and command are `TBD`, with the former commitment retained as historical evidence only.
- Reconciled overlapping Architecture work additively. A concurrent Architecture update supplied the symmetric `DeletionRequest` readiness evidence and advanced the assumption index; the stale in-paragraph index citation was corrected to `ARCH-A-INDEX-3`. Unrelated Architecture/UX/PRD validation work and staged report moves were left untouched.
- Review patched the dependency compatibility-command exception, exact current consumer inventories, and the AD-5 Conversations existence-read citation. Eleven unresolved Architecture, Product-metric, legal-hold, and concurrent UX/review root causes were recorded in `deferred-work.md` without selecting their design outcomes.

## Spec Change Log

- 2026-09-09: Approved planning synchronization implemented and focused verification completed.

## Review Triage Log

- PASS: five requested planning artifacts contain the approved readiness, dependency, Spine, backlog, and audit-trail updates.
- PASS: YAML loads; Markdown fence/heading/table checks pass; the 18-gate inventory remains unique; all nine dependency records remain `Uncommitted`; stale proposal vocabulary checks pass.
- PASS: `epics.md` and `sprint-status.yaml` have exact bidirectional parity for 33 active stories with distribution 10/8/7/8.
- OPEN: Product must select the evidence-backed Story 5.3 / `EXT-PROVIDER-1` historical-consumption branch.
- OPEN: dependency owners must supply and accept every commitment/availability field before any status advances.
- OPEN: Architecture/co-owners must provide accepted calendar retirement dates for unretired Architecture-owned assumptions; proposal historical count 29 remains inconsistent with the later approved 33-story authority and was not forced onto the backlog.

| Finding | Verdict | Evidence | Route |
| --- | --- | --- | --- |
| BH-1 | medium | PRD FR-21 expressly permits the register owner's compatibility command to execute a `Committed` seam to establish `Available`, but the register's absolute prohibition omits that exception and would prevent the required proof. | patch |
| BH-2 | false | PRD §8 and the approved proposal define exactly the existing commitment fields; all records remain `Uncommitted`, so no owner acceptance or availability is asserted that would require additional acceptance-result fields. | rejected |
| BH-3 | medium | `ConsumingStories` is defined as an exact machine-comparable inventory, while `EXT-HOST-1` says “5.6 onward”; the executable backlog names only Stories 5.1 and 5.6 as direct consumers. | patch |
| BH-4 | medium | Stories 5.6, 6.8, and 7.7 explicitly declare `EXT-PROTECTION-1`, but its record omits them, allowing readiness tooling to miss blocked consumers. | patch |
| BH-5 | medium | Stories 6.8 and 7.7 explicitly declare `EXT-SAFETY-1`, but its record omits them. | patch |
| BH-6 | medium | Story 6.8 explicitly declares `EXT-TOKEN-1`, but its record omits it. | patch |
| BH-7 | medium | Story 7.3 explicitly declares `EXT-SECRETS-1`, but its record omits it. | patch |
| BH-8 | high | The Tenants integration evidence separates the non-tenant-scoped `global-administrators` authority from the tenant-scoped `tenants` projection; AD-30 currently names the latter and can reject or mis-authorize Platform commands. | defer |
| BH-9 | high | AD-22/ARCH-A-12 permits `DigestKey` rotation only after full-tenant erasure or a waiver, which leaves no normal incident-response rotation path while retained or held content exists. | defer |
| BH-10 | high | AD-22 calls `DigestKey` rotation lock-bearing, but AD-12 and the readiness matrix close the lock-bearing inventory without that operation, creating divergent concurrency implementations. | defer |
| BH-11 | medium | AD-5/AD-12 use one `PausedDuration` for independent Agent-disabled and kill-switch intervals without saying whether overlap is unioned or double-counted, so retry deadlines can diverge. | defer |
| BH-12 | medium | AD-5 cites “AD-13 seam-2,” although the message-existence read is `EXT-CONV-AI-1` seam 2 governed by AD-6; a literal implementer could follow the wrong boundary. | patch |
| BH-13 | medium | PRD and AD-7 say the setting authority clears a Facilitator block but do not establish whether that means the same Party or any current holder of the Facilitator role. | defer |
| BH-14 | false | The governing PRD and approved proposal use the Tenant Agent Administrator role singularly as a generic role reference; the epic follows that authority and does not claim only one historical principal can exist. | rejected |
| BH-15 | high | Concurrent v7 Architecture reviews contain unadjudicated critical/high findings while the Spine remains marked final; the focused synchronization checks do not establish that those separate findings are resolved. | defer |
| EC-1 | high | The cited Tenants contract confirms the same Platform-authority mismatch as BH-8. | defer |
| EC-2 | medium | The clear rule requires current Facilitator authority but leaves same-setting-Party identity unresolved, matching BH-13. | defer |
| EC-3 | high | The closed operation-family inventory contains no `DigestKeyRotation`, confirming BH-10's concurrency ambiguity. | defer |
| EC-4 | medium | The message-existence read belongs to `EXT-CONV-AI-1` seam 2/AD-6, confirming BH-12's citation defect. | patch |
| EC-5 | medium | Simultaneous disabled-Agent and kill-switch pauses are reachable and the shared duration has no merge rule, confirming BH-11. | defer |
| EC-6 | high | A compromised key cannot rotate normally while any retained or held protected content exists, confirming BH-9. | defer |
| EC-7 | maybe-false | The documents do not settle how second-party approval resolves when no unique current Tenant Agent Administrator exists or that principal is the Inspector; Product/Architecture must define that edge to determine whether fail-closed stalling is intended. | defer (unverified medium) |
| EC-8 | false | Every clear increments `BlockVersion`, making the previously current unsent removal entry lower-version; AD-2/AD-7 already require lower-version pending entries to be cancelled and refuse a sent-but-unacknowledged clear. | rejected |
| EC-9 | false | The approved Story 6.6 criterion expressly requires every five-state/read-result pair to produce one deterministic fail-closed transition or rejection, so implementations cannot omit deletion/removal handling merely because the Spine does not enumerate all pairs. | rejected |
| EC-10 | medium | The governing PRD numerator counts distinct `(Party, Conversation, reason)` tuples over distinct `(Party, Conversation)` tuples, so multiple reasons can produce a value above 100% and an unnecessary trigger review; downstream artifacts cannot safely change that Product formula. | defer |
| EC-11 | high | Story 8.1 says partial unpin failure stays restrictive but does not define atomic unpin or compensation; a successfully unpinned subset could become erasable while release remains failed. | defer |
| EC-12 | false | The UX outcome row already requires focus to move to the authoritative item's status group before the stale focused control is removed, with next-row and page-heading fallbacks. | rejected |
| EC-13 | medium | The concurrent UX confirmation text says every pre-post validation failure remains `PostingFailed`, contradicting the PRD's `Abandoned(RemovedInConversations)` transition after absence is confirmed. | defer |
| EC-14 | high | The concurrent UX inspection path names only Tenant Agent Administrator approval/post-hoc review and omits the Platform Operator/second-Inspector eligibility branches, risking an invalid approval path for sensitive evidence. | defer |
| EC-15 | medium | The concurrent UX document places retry posting in `ConversationPosting` and treats its ten high-risk families as advisory-lock families, while AD-12/epics close the lock-bearing set without `ConversationPosting`. | defer |
| EC-16 | high | The concurrent verified-current review shows the Spine's own `10.0.3xx` advisory escalation condition has fired, but no accepted upgrade date or accepted-risk decision is recorded. | defer |

## Design Notes

The user's preservation instruction resolves execution in favor of the later approved 33-story backlog; the proposal's 29-story statement remains a historical authority conflict rather than a reason to rewrite current planning. Architecture target milestones already present in the overlapping edit are retained, while calendar retirement dates remain unset until owners accept them.

## Verification

**Commands:**

- `python3` YAML parsing plus a bidirectional `epics.md`/`sprint-status.yaml` story-key comparison -- expected: valid YAML and exact 33/33 parity.
- Focused Markdown heading, fence, and table-shape validation over the five edited artifacts -- expected: no structural errors introduced.
- `rg` checks for stale readiness metrics, old Spine binding ranges, committed host state, missing OQ-24..OQ-30 coverage, and unowned debt -- expected: only intentional historical references remain.
- `git diff --check` and scoped/full `git diff` review -- expected: no whitespace errors, no `prd.md`/runtime/tracker diff, and all baseline edits preserved.

**Results:** all listed checks passed after review patches against the final working tree: valid YAML; Markdown structure; 18 unique GateIds; nine dependency records all `Uncommitted`; exact dependency-consumer assertions; exact 33/33 story/tracker parity with distribution 10/8/7/8; required vocabulary and provenance assertions; and baseline/scoped whitespace checks. No `prd.md`, `sprint-status.yaml`, `src/`, `test/`, or `tests/` diff exists from the recorded baseline. The review produced no verification-gap finding, rejected five false findings, patched seven direct findings across three root causes, and deferred eleven decision-bearing root-cause groups.
