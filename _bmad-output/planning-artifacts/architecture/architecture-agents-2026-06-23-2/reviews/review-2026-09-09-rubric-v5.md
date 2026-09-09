---
review: rubric-v5
target: ARCHITECTURE-SPINE.md
target_updated: 2026-09-09 (Update pass rolling in VALIDATION-REPORT-2026-09-09-2.md, round 2)
reviewer: independent rubric walker (fifth pass — verifying the round-2 Update pass's claimed closures against current text, not trusting the memlog's own account)
verdict: FAIL
---

# Rubric Review v5 — Hexalith Agents Architecture Spine (2026-09-09, post round-2 Update)

**Gate verdict: FAIL.** The round-2 Update pass genuinely closed every claimed Critical and High item from
`VALIDATION-REPORT-2026-09-09-2.md` **except one**, and every claimed Medium was verified applied as
described. But re-deriving AD-5's fix against every AD that restates the same behavior — as the task
brief specifically asked — surfaces a fresh, self-contained contradiction the update pass did not touch:
**AD-7's own pre-post membership re-check still states the pre-amendment, PRD-contradicting outcome**
(`PostingFailed` with `RemovedInConversations`) for the exact scenario AD-5 was just corrected to handle
correctly (`Abandoned` with `RemovedInConversations`). AD-5 cites AD-7 as the source of this behavior;
AD-7 does not actually say what AD-5 believes it says. This is not a hypothetical: FR-2's own consequence
bullet (line ~160 of `prd.md`) states the required outcome in words as explicit as a spine document gets —
"a removal or block detected at the pre-post check moves the proposal to `Abandoned` with reason
`RemovedInConversations`, **never to `PostingFailed`**" — and AD-7 currently asserts precisely the
forbidden pairing.

## 0. Disposition of the round-2 Update pass's claimed closures (verified against current text, not the memlog)

Every one of the following was independently re-checked by reading the live `ARCHITECTURE-SPINE.md` text
(not the memlog's account of it) and is confirmed **genuinely closed**:

| Claim | Verified against | Result |
| --- | --- | --- |
| C-1 (AD-5 vs FR-18 system abandonment) | AD-5 lines ~131–135; FR-18 transition table | Closed *in AD-5 itself* — see Critical finding below for the AD-7 residue |
| C-2 (AD-12 vs FR-28 kill-switch) | AD-12 lines ~173–177; FR-28 §4.7/§9 | Closed — `Approved` now waits unposted; trigger-review/`InsufficientEvidence` machinery present |
| C-3 (AD-22 vs FR-24/FR-33 second-party model) | AD-22 line ~237; FR-24 consequence bullet | Closed — computed subject-set, anti-collusion, `ExportRequest`/`LegalHoldRelease` gates all present and match FR-24's wording near-verbatim |
| C-4 (sequence diagram mode qualifier) | diagram line 382 | Closed — `EligibleApprover resolution (both modes)`; no `(confirmation mode)` text remains anywhere in the file |
| C-5 (AD-21 "authorization" ambiguity) | AD-21 line ~231 | Closed — now names `ProviderInvocationAuthorized` explicitly, matching AD-13 step 4 in both prose and sequence diagram ordering |
| C-8 (AD-22 erasure/Conversations disclosure) | AD-22 line ~237; ARCH-A-10 row | Closed — explicit posted-copy disclosure, `ARCH-A-10` extended to `DeletionRequest` symmetrically |
| H-1 (frontmatter binds/sources, FR-34 citation) | frontmatter lines 13, 18–19, 31–36; AD-14/AD-17 Binds lines | Closed |
| H-2 (AD-2 `BlockVersion`/`MirrorPending`/5-state machine) | AD-2 line 117 | Closed |
| H-3 (AD-7 link/replace retirement) | AD-7 line 147 | Closed, and consistent with AD-30's `ProvisionHexa` restriction |
| H-4 (AD-15 rate-limit counter) | AD-15 line 195 | Closed |
| H-5 (.NET SDK servicing risk) | Stack table line 472 | Closed |
| H-7 (`CurrencyMismatch` cross-reference) | AD-10 line 165; AD-21 line 231 | Closed, cross-references resolve both ways |
| H-11 (Platform role-freshness) | AD-30 line 285 | Closed |
| C-6/C-7 (brownfield) | `sprint-status.yaml:88` | Closed — Story 5.2 corrected to `in-progress` with an explanatory comment naming DW-4 |
| Mediums (Target Retirement column, Stack table rows, AD-12 lock-family, AD-22 post-hoc consequence) | Architecture Assumptions table lines 747–758; Stack table lines 492–493; AD-12 line 177; AD-22 line 237 | All four confirmed applied as described |

**H-6 (AD-10/AD-15/AD-17 `AgentReadinessStatus` shape)** is only **partially** closed — see Medium finding
below.

## Critical

### C-1 (new this round) — AD-7's pre-post membership re-check still states the exact outcome AD-5/FR-2/FR-18 forbid

**Location:** `ARCHITECTURE-SPINE.md` AD-7 (the "Before every post..." sentence, in the paragraph
beginning "Membership is the last acceptance step of a call").

**Evidence.** Current AD-7 text: *"Before every post the same three-part step re-runs together with
block, Party state, Agent lifecycle, and Conversation accessibility checks; an external removal found
there sets the block, **abandons the Conversation's other non-terminal proposals, and records
`PostingFailed` with `RemovedInConversations`**, and `MembershipUnavailable` and `MembershipRejected` are
`PostingFailed` reasons produced only by this re-validation, never by acceptance."*

This sentence does two things wrong for the proposal currently undergoing the pre-post check:

1. It pairs the terminal outcome `PostingFailed` with the reason `RemovedInConversations`. `RemovedInConversations` is not a valid `PostingFailed` reason anywhere else in the spine or the PRD — it is exclusively an `Abandoned` reason.
2. It abandons only "the Conversation's **other** non-terminal proposals," implicitly excluding the proposal currently being posted from that abandonment, and instead routes it to `PostingFailed`.

**Direct contradiction with the PRD (not just the spine's own AD-5).** `prd.md` FR-2's consequence bullet
states, in nearly these exact words: *"a removal or block detected at the pre-post check moves the
proposal to `Abandoned` with reason `RemovedInConversations`, **never to `PostingFailed`** (FR-18)."*
FR-18's own transition table has a dedicated row for exactly this case: `Approved`, `PostingFailed` →
`Abandoned (RemovedInConversations)`, guard "Removal or block detected (FR-2), and the `MessageId`
lookup finds no posted message" — there is no `Approved`/`PostingFailed` → `PostingFailed` row for a
removal reason at all. `prd.md` also states plainly, in the FR-2 block-handling bullet: *"Setting the
block, or detecting an external removal, moves **every** non-terminal proposal for that Conversation to
`Abandoned` with reason `RemovedInConversations`... except a proposal in `PostingPending`"* — i.e. the
current proposal is not exempted from abandonment the way AD-7's "other" wording implies.

**Direct contradiction with the spine's own already-amended AD-5.** AD-5 (the AD this same Update pass
just fixed per C-1 of the prior round, `F-SPINE-2`) states: *"the `Approved`/`PostingFailed` →
`Abandoned` (`RemovedInConversations`) transition runs only after the `MessageId` lookup below finds no
posted message"* and explicitly attributes the trigger to *"AD-7's membership re-check."* AD-5 believes
AD-7 says what the PRD requires; AD-7's actual text says the opposite, and omits the `MessageId`-lookup
gate AD-5 makes mandatory before this transition can fire at all.

**Why this is a real divergence risk, not a wording nit.** This is exactly the scenario the prior round's
C-1 finding was raised to close, and the Update pass fixed it in one AD (AD-5) while leaving a second AD
(AD-7) asserting the pre-amendment, now-forbidden behavior for the identical trigger condition. A team
building the pre-post posting workflow from AD-7 alone — which is where the three-part membership step
and its outcomes are actually defined in detail — implements `PostingFailed`/`RemovedInConversations`
with no `MessageId` lookup and no abandonment of the current proposal: a materially different, PRD-
violating result from a team that reads AD-5 (or FR-2/FR-18) instead. The spine's job is precisely to
prevent this kind of AD-vs-AD fork, and right now it contains one on the very rule this round's Update
pass was supposed to have finished fixing.

**Fix.** Rewrite the "Before every post" sentence in AD-7 to match AD-5/FR-2/FR-18 exactly: an external
removal found during the pre-post re-check sets the block and, gated on the AD-5 `MessageId` lookup
finding no posted message, moves **the current proposal** (not just "other" ones) to `Abandoned`
(`RemovedInConversations`) — together with every other non-terminal proposal in that Conversation, except
one in `PostingPending`, which is untouched until it exits on its own terms. Reserve the `PostingFailed`
outcome in this sentence for `MembershipUnavailable`/`MembershipRejected` only, as the sentence's own
second clause already correctly states.

## High

None beyond the Medium below — every High-tier item from the prior validation round is independently
confirmed closed (see §0 table).

## Medium

### M-1 (partial closure, carried from H-6) — AD-15 still describes `AgentReadinessStatus` in bare-enum "growth" language that AD-10 just disclaimed

**Location:** AD-15 (line 195) vs AD-10 (line 165).

**Evidence.** AD-10 was correctly amended to state: *"`AgentReadinessStatus` (AD-15, AD-17) is a composite
wrapper, never a bare enum: it carries the selected entry's `ProviderReadinessResult` as one field and the
Agent-level growth states `ActiveNotProvenCallable`, `Stale`, and `AuthorityUnresolved` (AD-15) as sibling
fields alongside it, **not as alternate values of the same type**."* AD-15's own text, however, was left
unchanged and still lists, in the identical enumerated style used for a genuinely bare enum two clauses
earlier in the same sentence: *"`AgentCallOperationStatus` growth (`SafetyBlocked`, ... `PostingFailed`),
`AgentReadinessStatus` growth (`ActiveNotProvenCallable`, `Stale`, `AuthorityUnresolved`)..."* — presenting
`AgentReadinessStatus`'s three growth states exactly as it presents `AgentCallOperationStatus`'s growth
states, with nothing in AD-15's own text signaling the composite-wrapper/sibling-field shape AD-10 now
insists on.

**Why this still matters.** AD-10 is authoritative and resolves the ambiguity for a reader who consults
it, but H-6 was originally raised because *both* readings existed in the spine; the fix added the correct
reading to AD-10 without removing the misleading one from AD-15. A team building the UX/UI contract layer
from AD-15 alone (AD-15 is the AD that "owns" public surface/UI parity) still has textual grounds to model
`AgentReadinessStatus` as a bare three-value enum.

**Fix.** In AD-15, either drop the standalone `AgentReadinessStatus growth (...)` clause (since AD-10 now
owns the authoritative shape) or rephrase it as "the `AgentReadinessStatus.GrowthStates` sibling field
values (AD-10)" so it can't be read as a parallel bare enum to `AgentCallOperationStatus`.

## Low

### L-1 — AD-5's system-abandonment sentence is internally redundant in a way that could be misread

**Location:** AD-5, the sentence beginning "...after which the proposal persists until an Eligible
Approver abandons it..."

**Evidence.** *"...the Tenant Agent Administrator administratively retries it or, audited and without
Conversation read access, abandons it only when the Source Conversation is gone or inaccessible to the
Agents service principal, **the system abandons it, or the system system-abandons it on a detected
removal** (below)."* The doubled "the system abandons it, or the system system-abandons it" reads as two
different mechanisms but is describing two different reasons (`SourceConversationUnavailable` via the
FR-7/AD-7 re-check, and `RemovedInConversations`) for what is the same general mechanism (system
abandonment). This is confirmed correct in substance against FR-18's own parallel list, but the prose
itself invites a misreading that "the system abandons it" and "system-abandons it" name two distinct
processes.

**Fix.** Tighten to name both reasons explicitly, e.g. "...the system abandons it on `SourceConversationUnavailable`, or on a detected removal (`RemovedInConversations`, below)."

## Checklist summary

| # | Checklist item | Result |
|---|---|---|
| 0 | Round-2 Update pass's claimed closures actually closed | 8/9 Critical-tier and all High/Medium claims independently confirmed closed; one (C-1/AD-5) closed in the AD it targeted but left a contradiction in a sibling AD (AD-7) that states the same rule |
| 1 | Fixes real divergence points, misses none | **FAIL** — AD-7 still asserts a PRD-forbidden outcome for the pre-post removal case AD-5 was just fixed to handle correctly |
| 2 | Every AD's Rule enforceable and prevents its divergence | AD-5 now enforces the correct rule; AD-7, which independently restates the same trigger and outcome in more procedural detail, still enforces the wrong one — a builder following AD-7 diverges from a builder following AD-5 |
| 3 | Deferred section couldn't hide a real divergence | Clean — no change from v4 |
| 4 | Named tech verified-current | Clean — SDK servicing-risk framing (H-5) now explicit; no other drift found |
| 5 | Ratifies brownfield reality | Clean — `sprint-status.yaml` correction (C-7) confirmed applied |
| 6 | Covers driving PRD's capabilities | Clean at the frontmatter/binds level (H-1 closed); FR-2's exact pre-post-removal consequence is the one capability the amended AD-7 text still fails to cover correctly |
| 7 | Inherited invariants not weakened | N/A (top-of-tree initiative spine) |
| 8 | Every structural dimension decided/deferred/open | Clean |
| 9 | Binds/Prevents/Rule structurally present and coherent | Present; AD-7's Rule text is internally coherent with itself but incoherent with AD-5's Rule text on the same trigger |
| 10 | No placeholders/unresolved assumption tags | Clean |

**Overall verdict: FAIL.** One Critical finding: AD-7's pre-post membership re-check still directs a
detected removal to `PostingFailed`/`RemovedInConversations` and exempts the current proposal from
abandonment, which `prd.md` FR-2 states, almost verbatim, must never happen (`"never to PostingFailed"`)
and which the spine's own newly-amended AD-5 already contradicts. This is a same-class defect to the one
this Update pass just closed in AD-5 — a second AD stating the same rule was not updated to match — and
should be closed in the same way, by editing AD-7's "Before every post" sentence to route the current
proposal through the AD-5 `MessageId`-gated `Abandoned (RemovedInConversations)` transition rather than
`PostingFailed`. One Medium (AD-15's residual bare-enum-styled `AgentReadinessStatus` language) and one
Low (AD-5 prose redundancy) are also open; neither blocks a story on its own, but the Medium is a genuine
residual instance of the H-6 divergence risk this round believed it had closed.
