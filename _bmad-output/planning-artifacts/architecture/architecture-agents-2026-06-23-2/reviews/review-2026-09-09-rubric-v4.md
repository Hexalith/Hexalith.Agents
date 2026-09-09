---
review: rubric-v4
target: ARCHITECTURE-SPINE.md
target_updated: 2026-09-09 (file mtime 09:45:59)
reviewer: independent rubric walker (fourth pass — fresh read, not a delta from v3's assumed-clean baseline)
verdict: FAIL
---

# Rubric Review v4 — Hexalith Agents Architecture Spine (2026-09-09)

**Gate verdict: FAIL.** The driving PRD (`prd.md`, mtime 10:08:58 — 23 minutes *after* the spine's last
edit) was updated the same day with a second 2026-09-09 revision that explicitly names three "Spine
defects" in AD-5, AD-12, and AD-22 — places where the current spine text narrows or contradicts FR-18,
FR-24, FR-28, or FR-33 — and a companion punch list (`update-report-2026-09-09-2.md`, same mtime)
itemizes exactly what the spine still needs (`F-SPINE-1` through `F-SPINE-4` plus five more clauses).
None of this has been applied to `ARCHITECTURE-SPINE.md` yet. This is not a hypothetical or
speculative divergence risk: it is a documented, current contradiction between the spine and its own
cited, same-day-updated source of truth, sitting in the tree right now. Two independently-built story
teams — one implementing literally what AD-5/AD-12/AD-22 say, one implementing literally what
FR-18/FR-24/FR-28/FR-33 say — will diverge in exactly the ways the PRD itself documents. Checklist item
6 ("if a spec or PRD drove this spine, it covers that spec's capabilities") fails outright for this
revision of the spine.

## 0. Disposition of prior rounds' open findings (continuity check, not new findings)

Both LOW findings carried from `review-2026-09-09-rubric-v3.md` are verified **closed** in the current
text:

- AD-7's "every principal's `PartyId`" overclaim is now correctly scoped: "every principal-carried
  `PartyId` field (AD-30's `User.PartyId`, `CallerPartyId`, and `OnBehalfOfPartyId`; `Administrator`
  and `Platform` carry no `PartyId` at all)" (line 139). Fixed.
- AD-22's ARCH-A-10 citation now uses the document's own bracket convention: "...`launch-readiness-register.md` [ASSUMPTION ARCH-A-10] until a Conversations-owned preservation seam is committed..." (line 229). Fixed.

No action needed on either. The rest of this review is a fresh read against the rubric, not a re-walk
of what v1–v3 already covered — it surfaces a materially new fact (the PRD moved out from under the
spine after the spine's last edit) that no prior round could have caught, since all prior rounds ran
before this second 2026-09-09 PRD update landed.

## Critical

### C-1 — AD-5 contradicts FR-18 on system abandonment; PRD explicitly calls this a "Spine defect"

**Location:** `ARCHITECTURE-SPINE.md` AD-5 (lines 123–127).

**Evidence.** Current AD-5 text: "Abandon is legal from every awaiting-decision state and from
`PostingFailed`, never from `Approved` or `PostingPending`, which complete or fail on their own terms."

`prd.md` §8.1 (line 840, updated same day, 23 minutes after the spine): "A Spine rule that narrows or
contradicts an FR-18, FR-24, FR-28, or FR-33 rule is a Spine defect to be corrected before the
consuming story is `ready-for-dev`; three are open at this revision, owned by Architecture: ... AD-5
forbids the system abandonment from `Approved` and `PostingFailed` on a detected removal that FR-18
requires, and allows human abandonment from `PostingFailed` only."

`prd.md` OQ-26 (line 996, Resolved/amended 2026-09-09): "...before any exit from `PostingFailed`, the
`MessageId` lookup decides whether the post landed, in which case the proposal is `Posted` with
`LateConfirmed`... `PostingPending` is uninterruptible... The system abandonment from `Approved` and
`PostingFailed` on a detected removal is a deliberate divergence from Spine AD-5, recorded in §8.1 as a
Spine defect for Architecture to correct."

**Why this is a real divergence risk.** A story built against the current spine text must never
system-abandon an `Approved` proposal and must treat `PostingFailed` as human-abandon-only. A story
built against the PRD's FR-18 transition table (the actual functional requirement) must system-abandon
`Approved` and `PostingFailed` on a detected removal, run a `MessageId` lookup before any exit from
`PostingFailed` to catch a landed-but-unconfirmed post (`LateConfirmed`), and treat `PostingPending` as
uninterruptible — none of which AD-5 currently states. This is exactly the kind of invariant the spine
exists to fix so independently-built units don't diverge, and right now it fixes the wrong answer.

**Fix.** Amend AD-5 per the PRD's own punch list (`update-report-2026-09-09-2.md`, `F-SPINE-2`): allow
system abandonment from `Approved` and `PostingFailed` on a detected removal, add the pre-exit
`MessageId` lookup (`Posted`/`LateConfirmed` outcome), and state `PostingPending` is uninterruptible.

### C-2 — AD-12 contradicts FR-28 on kill-switch semantics and trigger rigor; PRD explicitly calls this a "Spine defect"

**Location:** `ARCHITECTURE-SPINE.md` AD-12 (lines 165–169).

**Evidence.** Current AD-12 text: "...lets `Approved` and `PostingPending` complete or fail on their
own terms..." and states the Release Operator trigger only as "the recorded triggers (blocked-call
share above 50 percent or posting failures above 10 percent sustained seven days)" with no review
process, minimum sample, or evidence-insufficiency rule.

`prd.md` §8.1 (line 844): "AD-12 states the Release Operator trigger without the review, minimum
sample, and `InsufficientEvidence` rule of FR-28."

`prd.md` OQ-27 (line 997, Resolved/amended 2026-09-09): "...`Approved` proposals wait without posting
(safety wins over "complete on their own terms", a Spine AD-12 correction owned by Architecture); a
`PostingPending` attempt in flight completes on its own terms; the `PostingFailed` retry clock pauses;
`ExpiresAt` keeps running, with `ExpiredWhileSuspended` excluded from SM-3 and SM-C5... The Release
Operator trigger is a review convened within one business day over four rates with a tuple denominator
and a minimum sample scaled for small tenants."

**Why this is a real divergence risk.** AD-12 currently tells an implementer that an `Approved`
proposal keeps posting under a pulled kill switch ("complete... on their own terms"); FR-28 requires the
opposite — it must wait, unposted, while the switch is pulled (only `PostingPending`, already in
flight, completes). AD-12 also omits the review/minimum-sample/`InsufficientEvidence` machinery FR-28
requires for the Release Operator trigger, so a story implementing AD-12 literally would pull the kill
switch on a raw threshold breach with no review gate — a materially different (and gameable) control
than FR-28 specifies, which is precisely the risk FR-28's H-2 fix (`update-report-2026-09-09-2.md`)
was written to close.

**Fix.** Amend AD-12 per `F-SPINE-4`: state that `Approved` proposals wait without posting under a
pulled kill switch (not "complete on their own terms" — that phrase now applies to `PostingPending`
only); add the trigger-review process (one-business-day convening, four-rate tuple denominator, minimum
sample scaled for small tenants, `InsufficientEvidence` rule).

### C-3 — AD-22 contradicts FR-24/FR-33 on the compliance-inspection second-party model; PRD explicitly calls this a "Spine defect"

**Location:** `ARCHITECTURE-SPINE.md` AD-22 (lines 225–229).

**Evidence.** Current AD-22 text: unposted content and context metadata are inspectable "under a
compliance inspection owned by `AuditInspection`, held by the Compliance Inspector, scoped to a named
Conversation or case, justified, either pre-approved by a distinct Tenant Agent Administrator principal
before any content read or post-hoc reviewed by one within the tenant's review window..." — i.e., the
Tenant Agent Administrator is the only second party the current AD-22 text names, and `LegalHold`
release and `ExportRequest` carry no second-party approval requirement at all in the current AD-22 text
("`LegalHold`, `LegalHoldRelease`, and `ExportRequest` are Compliance Inspector commands").

`prd.md` §8.1 (line 843): "AD-22 names the Tenant Agent Administrator as the sole inspection second
party and gives export and hold release none, against FR-24 and FR-33."

`prd.md` OQ-30 (line 1000, Resolved/amended 2026-09-09): "a compliance-inspection second party is
outside a computed subject set (callers, editors, Approvers, decision actors, Facilitators in scope,
and the Tenant Agent Administrator whose configuration was in force); where that set holds the
Administrator, the Platform Operator or a second Compliance Inspector approves; two Inspectors cannot
approve each other within 30 days; inspections wider than one Conversation need the Platform Operator...
Export requires prior second-party approval; hold release requires a separate approver."

**Why this is a real divergence risk.** The current AD-22 model is both narrower (single hard-coded
approver role, no computed-subject-set exclusion, no anti-collusion rule against two Inspectors
approving each other) and incomplete (no second-party gate at all for export or hold release) compared
to what FR-24/FR-33 require. A story built to AD-22 as written would ship a materially weaker
governance control than the PRD specifies — a genuine security/compliance gap, not a cosmetic one.

**Fix.** Amend AD-22 per `F-SPINE-3`: adopt the computed-subject-set second-party rule (excluding
callers, editors, Approvers, decision actors, in-scope Facilitators, and the presiding Tenant Agent
Administrator), route to the Platform Operator or a second Compliance Inspector when the Administrator
is in the subject set, forbid two Inspectors approving each other within 30 days, require the Platform
Operator for scopes wider than one Conversation, and add the missing prior-approval gate for
`ExportRequest` and a separate-approver gate for `LegalHoldRelease`.

## High

### H-1 — Frontmatter `binds` and `sources` are stale against the PRD's actual current scope (`F-SPINE-1`)

**Location:** `ARCHITECTURE-SPINE.md` frontmatter (lines 11–17).

**Evidence.** Frontmatter states:
```
binds:
  - PRD FR-1..FR-33
  - PRD NFR-1..NFR-14
  - PRD OQ-1..OQ-23
sources:
  - ../../prds/prd-agents-2026-06-23/prd.md
  - ../../prds/prd-agents-2026-06-23/update-report-2026-09-09.md
```
`prd.md` now contains FR-1 through **FR-34** (new §"FR-34: Gate Content-Bearing Execution On Payload
Protection Availability", line 721) and OQ-1 through **OQ-30** (rows OQ-24–OQ-30 added "in the second
2026-09-09 update," line 965). The `sources` list still cites `update-report-2026-09-09.md` (the
*first* same-day update, mtime 01:36), not `update-report-2026-09-09-2.md` (the second same-day update,
mtime 10:08, which is the one containing the "For correct-course... `ARCHITECTURE-SPINE.md`" punch
list this review is built on). `prd.md` §8.1 itself states the fix explicitly: "the 2026-09-09 update
brought two such rules into this PRD (`Unreconciled` settlement in FR-28, `hexa` provisioning in FR-1
and FR-33)" and `update-report-2026-09-09-2.md`'s own correct-course section says: "`ARCHITECTURE-SPINE.md` — frontmatter binds `FR-1..FR-34`, `OQ-1..OQ-30` (F-SPINE-1)."

**Why this is a real divergence risk.** A reader or a downstream tool trusting the frontmatter `binds`
range to enumerate everything the spine is answerable for will not know FR-34 or OQ-24–OQ-30 exist at
all — including FR-34's whole new capability (gating content-bearing execution on payload-protection
availability) and its typed blocker `PayloadProtectionUnavailable`, which appears nowhere in the spine
(checked by grep across the full document). AD-14 and AD-22 already imply the right fail-closed
*behavior* ("if protection is unavailable, content-bearing workflows stay disabled" / "content-bearing
workflows stay disabled while that record is not `Available`"), so this is a naming/traceability gap
rather than a behavioral one — but `RQ-1`'s blocker vocabulary in AD-17 never names
`PayloadProtectionUnavailable`, so a release-qualification implementation following AD-17 alone would
not know to surface this specific blocker code.

**Fix.** Update `binds` to `PRD FR-1..FR-34` / `PRD OQ-1..OQ-30`; update the `sources` entry to
`update-report-2026-09-09-2.md` (or list both, dated); add `PayloadProtectionUnavailable` to AD-17's
`RQ-1` blocker vocabulary and cite FR-34 in AD-14's/AD-17's `Binds` lines.

### H-2 — AD-2's `ConversationAgentState` definition is missing fields the PRD's resolved OQ-16/OQ-25 require

**Location:** `ARCHITECTURE-SPINE.md` AD-2 (line 109) and AD-7 (line 139).

**Evidence.** AD-2 defines `ConversationAgentState` as: "(`TenantId`, `ConversationId`;
membership-established fact, the Agents-owned block, and the index of non-terminal proposals for that
Conversation)". AD-7 describes membership as a three-part read/write step but never mentions a
`BlockVersion` or a `MirrorPending` outbox flag.

`prd.md` OQ-16 (line 986, Resolved 2026-09-08, amended 2026-09-09): "`hexa` joins a Conversation... under the... Conversation Agent State machine `NeverJoined → Joined → (ExternallyRemoved | Blocked) → ReadmitPending → Joined`... The block is an Agents-owned per-Conversation record, authoritative when written and **mirrored to the Conversations participant list by an at-least-once outbox with a visible `MirrorPending` flag**..."

`prd.md` OQ-25 (line 995): "The block is authoritative when written and **versioned by `BlockVersion`**;
its mirror to Conversations is an at-least-once outbox with a `MirrorPending` flag, and a late mirror
effect is never read as an external removal..."

`update-report-2026-09-09-2.md`'s correct-course list confirms this gap directly: "AD-2
`ConversationAgentState` gains `BlockVersion`, `MirrorPending`, and the five states."

**Why this is a real divergence risk.** Without `BlockVersion`, two concurrent clear/set operations on
the block cannot be ordered, and without an explicit five-state machine and `MirrorPending` flag, a
story implementing AD-7's membership step has no way to represent "block cleared locally but mirror to
Conversations not yet confirmed" — exactly the race OQ-25 was written to close ("a late mirror effect is
never read as an external removal"). Two teams building the aggregate and the membership workflow from
AD-2/AD-7 alone would very plausibly invent different state shapes for this.

**Fix.** Add `BlockVersion` and `MirrorPending` to AD-2's `ConversationAgentState` field list, and state
the five-state machine (`NeverJoined → Joined → (ExternallyRemoved | Blocked) → ReadmitPending →
Joined`) explicitly in AD-2 or AD-7 rather than leaving it implicit in the three-part prose description.

### H-3 — AD-7 still presents the Party link/replace commands as active, not deprecated

**Location:** `ARCHITECTURE-SPINE.md` AD-7 (line 139).

**Evidence.** Current AD-7: "Agents stores stable `PartyId` references only; **the shipped link and
replace commands validate or provision identity through Parties adapters**, and a missing identity is
the `MissingPartyIdentity` activation blocker."

`prd.md` OQ-25 (line 995): "`hexa`'s Party identity is created at provisioning as an AI-type Party and
is immutable; **the shipped link and replace commands are deprecate-and-reject** (FR-2)."

`update-report-2026-09-09-2.md` correct-course list: "AD-7 retire the Party link/replace commands."

**Why this is a real divergence risk.** AD-7's current wording still describes these commands as live,
functioning identity-provisioning paths ("validate or provision identity"). A story team reading only
the spine would keep them operative; the PRD (driven by OQ-28's create-only provisioning model — "the
Platform Operator provisions `hexa` once per tenant at tenant enablement, create-only... No tenant role
can... change its Party identity in V1") requires them to reject with a typed deprecation error instead.

**Fix.** Amend AD-7 to state the link/replace commands are deprecate-and-reject (retired), consistent
with `hexa`'s immutable, Platform-provisioned Party identity.

### H-4 — AD-15's FR-25 blocked-call counters omit rate limits

**Location:** `ARCHITECTURE-SPINE.md` AD-15 (line 187).

**Evidence.** Current AD-15: "`agent-setup` and `product-metrics` are the only sources of the per-tenant
FR-25 counters (calls blocked by **context policy, safety, cost cap, `NoEligibleApprover`,
`RemovedInConversations` or the block**; system-abandoned proposals by reason; reserved versus settled
spend with the 80 and 100 percent states)..." — rate-limit-blocked calls are not in this list, even
though AD-21 defines per-Party/per-Conversation rate limiting as a distinct acceptance-path rejection
reason.

`update-report-2026-09-09-2.md` correct-course list: "AD-15 add rate limits to the blocked-call
counters."

**Why this is a real divergence risk.** A UI/reporting story built strictly from AD-15's enumerated
counter list would have no contract for surfacing rate-limit-blocked call volume, even though AD-21
requires enforcing that block. This is a silent capability gap in the one place (AD-15) that is
supposed to be the exhaustive list of what the FR-25 counters cover.

**Fix.** Add a rate-limit-blocked counter to AD-15's enumerated FR-25 counter list.

## Medium

### M-1 — Architecture Assumptions table lacks target retirement dates for Architecture-owned rows

**Location:** `ARCHITECTURE-SPINE.md`, "Architecture Assumptions" table (lines 735–746).

**Evidence.** `prd.md` (line 837): "...every Architecture-owned row carries a target retirement date."
None of the current table's ten rows (ARCH-A-1 through ARCH-A-10) carries a date in its "Retired when"
column — all are stated as conditions ("Story 5.6 aligns them...", "`EXT-TOPOLOGY-1` recovery and
expiry evidence confirms...") without a target date, even for the rows Architecture solely or jointly
owns (ARCH-A-1, ARCH-A-2, ARCH-A-4, ARCH-A-6, ARCH-A-7, ARCH-A-9).

**Why this is a checklist-relevant gap.** This is exactly what checklist item 3 warns about: an
open-ended assumption with no forcing date is closer to a permanent, silently-diverging gap than a
tracked one — two teams can each assume it will "eventually" be resolved in their favor. The PRD has
already made this a binding cross-artifact rule for this spine as of the same-day update.

**Fix.** Add a target retirement date (or sprint/story milestone with a calendar date) to each
Architecture-owned row in the Architecture Assumptions table.

### M-2 — `sources` list points to a superseded PRD update report without a staleness signal

**Location:** `ARCHITECTURE-SPINE.md` frontmatter (line 17).

**Evidence.** As in H-1: `sources` cites `update-report-2026-09-09.md` (01:36) only, not
`update-report-2026-09-09-2.md` (10:08) — the second same-day PRD revision, which is the one that added
FR-34/OQ-24–30 and produced the explicit correct-course list this spine has not yet applied. Since the
spine's own frontmatter `updated: 2026-09-09` date cannot distinguish "before" from "after" a same-day
PRD revision, a reader has no signal from the frontmatter alone that the cited PRD moved out from under
the spine after the spine's last edit.

**Why this matters.** This is a narrower restatement of H-1 focused specifically on traceability: even
once H-1's `binds` range is corrected, the `sources` list should name the specific PRD revision (or a
PRD content hash / commit) the spine was verified against, so a future reviewer doesn't have to compare
file mtimes to detect this class of drift, as this review had to.

**Fix.** Either fold this into the H-1 fix (name the `-2` report) or add an explicit "PRD revision
verified against" marker distinct from the shared `updated:` date.

## Low

None beyond the closed items already logged in §0. No placeholder text, no unresolved template
comments (`<!-- -->` search returns zero hits), and no `[ASSUMPTION ...]` tag lacks a home in either
the PRD's §8.1 `A-n` table or the spine's own Architecture Assumptions `ARCH-A-n` table (all 19 inline
citations were cross-checked against both tables and resolve cleanly, aside from the M-1 missing-date
gap above, which is a content gap in an otherwise correctly-indexed row, not an orphaned tag).

## Notes on checklist items not otherwise covered above

- **Item 4 (technology currency):** spot-checked against the repo — `global.json` SDK `10.0.301`,
  `Directory.Packages.props` Fluent UI `5.0.0-rc.5-26219.1`, all matching the Stack table exactly; no
  stale or contradictory version claim found. (ARCH-A-8 already self-flags the Fluent UI RC-vs-GA risk,
  which is correct practice, not a finding.)
- **Item 5 (brownfield ratification):** spot-checked — no module-owned AppHost/Aspire/ServiceDefaults
  project exists in `src/` (only build-artifact `apphost` binaries and an imported
  `Hexalith.EventStore.ServiceDefaults.dll`), consistent with AD-16's "ships no module-owned AppHost..."
  claim. The `src/Hexalith.Agents/` folder currently has only 3 of the 14 aggregate folders AD-2/the
  Structural Seed name (`Agent`, `AgentInteraction`, `ProviderCatalog`) — expected for an in-progress
  build, not a contradiction (the seed states target structure, and the folders that do exist match
  exactly).
- **Item 7 (inherited invariants):** N/A — this is the top-of-tree initiative spine; no parent spine or
  "Inherited Invariants" section is expected or present.
- **Item 8 (operational envelope):** deployment/environments (AD-16), operations/recovery (AD-23),
  capacity (AD-24) are all explicitly owned or deferred to `EXT-HOST-1`; no silent dimension found.

## Summary table

| # | Checklist item | Result |
|---|---|---|
| 0 | Prior rounds' open findings actually closed | Both v3 LOWs verified closed |
| 1 | Fixes real divergence points, misses none | **FAIL** — AD-5/AD-12/AD-22 currently fix the *wrong* answer versus the PRD; FR-34 capability has zero AD coverage |
| 2 | Every AD's Rule enforceable and prevents its divergence | Mechanically fine elsewhere, but C-1/C-2/C-3 ADs actively prevent the *correct* behavior while permitting the one the PRD rejects |
| 3 | Deferred section couldn't hide a real divergence | Clean, but Architecture Assumptions table needs target dates (M-1) |
| 4 | Named tech verified-current | Clean — SDK/Fluent UI versions match repo exactly |
| 5 | Ratifies brownfield reality | Clean — no contradiction found in spot checks |
| 6 | Covers driving PRD's capabilities | **FAIL** — FR-34 uncovered, OQ-24–30 uncited, three explicit "Spine defect" contradictions open |
| 7 | Inherited invariants not weakened | N/A |
| 8 | Every structural dimension decided/deferred/open | Clean |
| 9 | Binds/Prevents/Rule structurally present and coherent | Present per the deterministic lint; content coherent everywhere except C-1/C-2/C-3 |
| 10 | No placeholders/unresolved assumption tags | Clean |

**Overall verdict: FAIL.** Three ADs (AD-5, AD-12, AD-22) currently state rules the spine's own cited,
same-day-updated PRD explicitly labels as defects to be corrected before any consuming story reaches
`ready-for-dev`, and one whole FR (FR-34) plus seven OQ rows (OQ-24–OQ-30) are outside the spine's
stated binds range entirely. This is fully actionable: `update-report-2026-09-09-2.md` already contains
a precise, itemized fix list (`F-SPINE-1` through `F-SPINE-4` plus five more named clauses) that maps
directly onto this review's Critical and High findings — applying it should be a same-day turnaround,
not a re-architecture.
