# PRD Update Report — Hexalith Agents (second update, 2026-09-09)

- **PRD:** `prd.md` (879 → 971 lines; `status: final`, `updated: 2026-09-09`)
- **Change signal:** `validation-report.md` (validate run 2026-09-09T07:17Z, grade Poor: 4 critical / 9 high / 26 medium / 30 low)
- **Scope given:** apply every critical and high finding whose fix targets the PRD; fold mediums and lows only where co-located; list register-, epics-, and Spine-targeted findings for correct-course; re-run the reviewer gate before finalize.
- **Reconciliation of the signal:** `reconcile-validation-2026-09-09-2.md` — 38 applied, 2 partial, 3 diverged, 7 out of scope, 19 deferred.
- **Reviewer gate (post-update):** `review-rubric.md` (Good, 0C/0H/13M/9L), `review-adversarial-general.md` (2C/6H/11M/10L new; 15 prior findings resolved), `review-consistency.md` (internal 0C/1H/2M/7L; cross-artifact 1C/3H/9M/4L). Every gate critical and high was applied in a second batch; the prior gate's files are archived with the `-2026-09-09-validate` suffix.

## Applied from the change signal

| Finding | Where it landed |
| --- | --- |
| C-1 `EXT-PROTECTION-1` orphan, three `RQ-1` definitions | FR-28 single ten-item `RQ-1` input list cited by §3, §11, OQ-22; new **FR-34** with blocker `PayloadProtectionUnavailable`, owners, and test; FR-21 `Available` and `DependencyNotAvailable` consequences; §8 `Available` defined; §9 and FR-30 cite the engine; §0 "eight of nine" |
| C-2 FR-2 cannot re-admit `hexa`; mirror undefined | Conversation Agent State machine (`NeverJoined`, `Joined`, `ExternallyRemoved`, `Blocked`, `ReadmitPending`) with `BlockVersion` and `MirrorPending` outbox; clear authority stated; OQ-25 |
| C-3 transient faults become terminal; abandon after a landed post | FR-7 re-check with typed authoritative answers, `ResolutionUnavailable`, `ResolutionEmptyPending`, cadence A-20; FR-18 transition table, `MessageId` lookup, `LateConfirmed`, uninterruptible `PostingPending`; seam 2 existence read; OQ-26 |
| H-1 `ARCH-A` delegation unbounded | §8.1 cites the Spine by path and date, open range, owner-independent blocking, bidirectional reconciliation rule, three named Spine defects; `Unreconciled` in FR-28 |
| H-2 kill-switch trigger gameable | FR-28 trigger review over four rates with tuple denominator, minimum sample scaled for small tenants, one-business-day convening, `TriggerReviewOverdue`; A-17 |
| H-3 Provider data handling | FR-4 data-handling record with `DataHandlingVersion`; FR-5 eligibility; Tenant Agent Administrator acceptance (FR-33, A-10); FR-24 evidence; §9; §6.2 excludes tenant credentials; OQ-29 |
| H-4 compliance second party | FR-24 computed subject set, no reciprocal Inspector approvals, Platform Operator for wide scope, post-hoc window A-19; FR-33 hold-release and export rows; OQ-30 |
| H-5 FR-18 vs FR-28 kill switch | FR-28 semantics govern; `Suspended` is an FR-25 status; `Approved` proposals wait; retry clock paused; `ExpiresAt` runs with `ExpiredWhileSuspended` excluded from SM-3/SM-C5; release row; OQ-27 |
| Drift high "disabled while not `Available`" has no code | FR-34 as a requirement with an attestation-cleared additive blocker; the code side is listed below |
| Consistency 2 §0 count | §0 rewritten so it cannot drift |
| Consistency 4 who creates `hexa` | Platform Operator provisions at tenant enablement, create-only, with an immutable AI-type Party identity; FR-1, FR-2, FR-33, UJ-1, §3, §10; OQ-28 |

Co-located mediums and lows folded: see `.memlog.md` entries of 2026-09-09 (two "folded" decisions). Assumptions A-18 through A-21 added; A-3, A-7, A-9, A-10, A-14, A-17 amended.

## Deliberate divergences from the reviewers' suggested fixes

- `Suspended` is a status reported by FR-25, not a lifecycle state.
- `ExpiresAt` keeps running under the kill switch (Spine AD-12), with `ExpiredWhileSuspended` excluded from the launch-health denominators.
- System abandonment from `Approved` and `PostingFailed` on a detected removal is kept, against Spine AD-5; `PostingPending` is uninterruptible instead.
- The SM-2 event feed is an alternative form of seam 4, not a seventh seam.
- The kill-switch minimum sample scales down for small tenants rather than evaluating over the platform cohort.

Rationale for each is in `addendum.md`, "Options Considered — Second 2026-09-09 Update".

## Deferred (owner Product, revisit at the next validate run)

Rubric mediums carried by decision: FR-26 "more restrictive of two policies" (with consistency L-I3), FR-31 assertion category vs OQ-9, public Agent Call state contract, p99 at n = 30. Adversarial: M-1 organization Parties as Approvers, M-2 scan at edit time, M-6 FR-8 classifier ordering, M-10 `NoEligibleApprover` remediation hint, M-12 staleness bound on `Approved`, M-13 status entry in `EXT-CONV-UI-1`, predecessor M-3/M-4/M-5/M-8/M-11; lows L-1, L-4..L-9, L-11 of the signal and L-6, L-7, L-10 of the gate. Rubric lows: undated assumptions, process gates inside FR-21, residual adjectives, glossary drift, Eligible Conversation rationale placement. Drift low: narrowing A-1.

## For correct-course (fix targets another artifact)

**`launch-readiness-register.md`** — still gates `RQ-1` on SM-1..SM-6 real attainment and has no SM-7 contract (signal Critical, consistency C-X1); needs the blocker vocabulary `DependencyNotAvailable`, `UnretiredAssumption`, `OpenDecision`, `ProhibitedCostControlPosture`, `PayloadProtectionUnavailable`, `GateOutOfScope`, `TriggerReviewOverdue`, `InsufficientEvidence` (F-LRR-1, F-LRR-3); `LR-PRODUCT-METRICS` and `product-metrics` split into gate and launch-health metrics (F-LRR-2).

**`external-dependency-register.md`** — seam 2 must carry the `MessageId` existence read with typed `ConversationDeleted` / `PrincipalRemovedFromConversation` answers, and seam 4 the event-feed alternative (F-REG-1; the PRD marks both as pending); seam 5 typed existence/access answers (A-15); seam 1 AI-type Party verification (A-21); `DependencyNotAvailable` semantics (F-REG-2); Non-Conformance Records section for Story 5.3 / `EXT-PROVIDER-1` (F-REG-3); `EXT-CONV-AI-1` consumers 6.1, 6.2, 6.6, 7.1, 7.4, 8.5 (F-REG-4); `EXT-HOST-1` required artifact gains the FR-34 attestation port and the payload-protection binding, which returns the record to `Uncommitted` under the register's change rule; `EXT-PROTECTION-1` compatibility command must exercise seal, unseal, erase, hold-pin, and identity/version reporting.

**`ARCHITECTURE-SPINE.md`** — frontmatter binds `FR-1..FR-34`, `OQ-1..OQ-30` (F-SPINE-1); AD-5 allow system abandonment from `Approved`/`PostingFailed` on removal and add the `MessageId` lookup and uninterruptible `PostingPending` (F-SPINE-2); AD-22 adopt the computed-subject-set second party, hold-release approver, export prior approval (F-SPINE-3); AD-12 adopt the trigger review, minimum sample, `InsufficientEvidence`, and that `Approved` proposals wait under suspension (F-SPINE-4); AD-7 retire the Party link/replace commands; AD-14 name `PayloadProtectionUnavailable`; AD-15 add rate limits to the blocked-call counters; AD-2 `ConversationAgentState` gains `BlockVersion`, `MirrorPending`, and the five states; ARCH-A table: every Architecture-owned row carries a target retirement date.

**`epics.md`** — requirements inventory stale (signal High, consistency H-X3): rewrite FR1, FR7, FR18, FR21, FR26 rows; add FR29–FR34 and OQ-24..OQ-30; Story 5.4 AC drop "caller"; Story 6.6 adopt the state machine and clearing rule; Story 8.1 add the second-approver step; Stories 7.4/7.5 add the `MessageId` lookup and `LateConfirmed`; UX-DR45 launch-readiness content (F-EPICS-1).

**`update-report-2026-09-09.md`** — record the two proposal amendments applied under the 2026-09-09 sprint change proposal (F-UPD-1).

**Code side (owner Agents Runtime Maintainer, unchanged from the update gate)** — readiness recording still accepts `ReportingOnlyMonitoring` / `AcceptedLaunchRisk` (critical); configuration still accepts `BlockWithAuditableOverride` and `Caller` (high); `ProposalDetail.razor` treats `PostingFailed` as terminal (high); additive members missing (medium); plus, new: the FR-34 blocker, attestation, and fail-closed paths; the Party link/replace deprecate-and-reject; `ExpiredWhileSuspended`, `ResolutionEmptyPending`, `ApproverResolutionUnavailable`, `ContextUnavailable`, `BlockVersion`, `DataHandlingVersion`, `GateOutOfScope`, `TriggerReviewOverdue`.

## Artifacts

- `prd.md`, `addendum.md` (updated)
- `.memlog.md` (decisions, changes, events appended)
- `reconcile-validation-2026-09-09-2.md`
- `review-rubric.md`, `review-adversarial-general.md`, `review-consistency.md` (this run's gate); `review-*-2026-09-09-validate.md` (the signal's reviewers, archived)
- `polish-structure-prd-2026-09-09-2.md`, `polish-prose-prd-2026-09-09-2.md`, `polish-structure-addendum-2026-09-09-2.md`, `polish-prose-addendum-2026-09-09-2.md`
