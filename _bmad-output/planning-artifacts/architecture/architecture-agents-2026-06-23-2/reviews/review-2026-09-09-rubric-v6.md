---
review: rubric-v6
target: ARCHITECTURE-SPINE.md
target_updated: 2026-09-09 (post round-2 Update pass; v5's in-place fixes applied same day)
reviewer: independent rubric walker (sixth pass — fresh judgment against current text; v5's claimed
  in-place fixes independently re-verified, not trusted from the memlog)
verdict: FAIL
---

# Rubric Review v6 — Hexalith Agents Architecture Spine (2026-09-09, round 6)

**Gate verdict: FAIL.** All of round 5's findings (the AD-7 `PostingFailed`/`RemovedInConversations`
contradiction, the AD-15 bare-enum residue, and the AD-5 prose redundancy) are confirmed genuinely fixed
in the current text. But walking every AD against its own cited PRD rows and against every other section
that restates the same rule surfaces two fresh, self-contained Critical contradictions this round's
update pass did not touch: (1) the **Deferred Beyond V1** table still describes `LegalHoldRelease` as a
single-Compliance-Inspector command with no second-party gate — directly contradicted by AD-22's own
current Rule, which already requires a second Compliance Inspector or the Platform Operator to approve
every `LegalHoldRelease`; and (2) **AD-7's block-clear rule is under-restrictive against FR-2, the FR-33
role/scope table, and OQ-25 alike** — all three say clear authority is tied to *whoever set the block*
(evaluated at clear time) and that a Facilitator can never clear a Tenant Agent Administrator's block,
while AD-7 merely says a block is "cleared only by the first two" (TAA or Facilitator), with no such
restriction.

## 0. Disposition of round 5's fixes (verified against current text)

| Claim | Verified against | Result |
| --- | --- | --- |
| C-1/v5 (AD-7 pre-post re-check routes removal to `Abandoned`, not `PostingFailed`) | AD-7 "Before every post..." sentence | Closed — now reads "moves the proposal being posted to `Abandoned` with reason `RemovedInConversations` once AD-5's `MessageId` lookup confirms no posted message exists, never to `PostingFailed`," matching AD-5 and FR-2/FR-18 verbatim |
| M-1/v5 (AD-15 bare-enum-styled `AgentReadinessStatus` language) | AD-15, `AgentReadinessStatus` clause | Closed — now reads "`AgentReadinessStatus`'s Agent-level sibling fields growing to add ... — not alternate values of one enum, per AD-10's composite-wrapper fix" |
| L-1/v5 (AD-5 "system abandons it, or the system system-abandons it" redundancy) | AD-5, system-abandonment sentence | Closed — now reads "...or the system abandons it on a detected removal or an unresolvable Source Conversation (below)" |

No regressions found in the areas v5 touched. This round's findings are new and independent of v5's.

## Critical

### C-1 — `Deferred Beyond V1` table contradicts AD-22's own current `LegalHoldRelease` second-party rule

**Location:** `ARCHITECTURE-SPINE.md`, `## Deferred Beyond V1` table, row "Two-person rule on
legal-hold release"; vs. AD-22 (the sentence "`ExportRequest` requires this same second-party
approval...").

**Evidence.** The Deferred table currently states:

> "Two-person rule on legal-hold release | Product and Security own it; V1 releases a hold with a
> single Compliance Inspector command carrying typed justification, while deletion already requires two
> roles under AD-22."

AD-22's own Rule, in the same document, states:

> "`ExportRequest` requires this same second-party approval before any content read, never post hoc;
> `LegalHoldRelease` requires the audited approval of a second Compliance Inspector or the Platform
> Operator on the same subject-set and anti-collusion terms."

These describe mutually exclusive worlds for the identical command (`LegalHoldRelease`): the Deferred
table says V1 ships it as a single-actor operation and that only `DeletionRequest` currently needs two
roles; AD-22 says `LegalHoldRelease` already requires a second, independent approver (a second
Compliance Inspector or the Platform Operator) on the same anti-collusion terms as the audit-inspection
and export second-party gates.

**Why this happened.** AD-22's `LegalHoldRelease` second-party gate was added in the round-4 Update pass
(closing that round's C-3, itself driven by PRD OQ-30: "hold release requires a separate approver") but
the pre-existing Deferred-table row describing the old single-command behavior was never revisited —
exactly the "amended in one place, stale cross-reference in a sibling section" defect class this gate
exists to catch, just landing in the Deferred table instead of a sibling AD this time.

**Why it matters.** A reader who consults the Deferred table (the natural place to check "what's still
missing for V1") is told `LegalHoldRelease` ships without a second-party control and that closing that
gap is future Product/Security work; a reader who consults AD-22 is told the opposite is already true
and enforced. A team scoping the legal-hold release story from the Deferred table alone would ship a
single-approver release path that AD-22 — and PRD OQ-30 — forbid.

**Fix.** Remove the "Two-person rule on legal-hold release" row from `Deferred Beyond V1` (it is not
deferred — AD-22 already resolves it), or, if some narrower two-person nuance is still genuinely open
(e.g., requiring two *Compliance Inspectors* specifically rather than one Inspector plus the Platform
Operator), rewrite the row to name that narrower residual gap precisely instead of asserting `V1
releases a hold with a single Compliance Inspector command`, which is no longer true of the spine's own
Rule text.

### C-2 — AD-7's block-clear rule omits the asymmetric setting-authority restriction that FR-2, the FR-33 role table, and OQ-25 all state

**Location:** `ARCHITECTURE-SPINE.md` AD-7, the sentence "The block is set by the Tenant Agent
Administrator, the Conversation Facilitator [ASSUMPTION A-9], or the membership step on detecting an
external removal, is cleared only by the first two, is audited, and is mirrored..."

**Evidence — three independent PRD sources state the same, more restrictive rule that AD-7 does not
capture, even though AD-7's own Binds line cites FR-2 and FR-33 by number:**

- `prd.md` FR-2 consequence bullet: *"Clearing: a block is cleared only by **the authority that set
  it** or by the Tenant Agent Administrator — clear authority is evaluated at clear time, so a Party
  that has since lost the Facilitator role cannot clear — **and a Conversation Facilitator cannot clear
  a block the Tenant Agent Administrator set.** An `ExternallyRemoved` record is cleared by the Tenant
  Agent Administrator or by any current Conversation Facilitator of that Conversation."
- `prd.md` FR-33 role/scope table, row "Clear a block on `hexa` in a Conversation": *"The authority that
  set the block, evaluated at clear time, or the Tenant Agent Administrator; **a Conversation
  Facilitator cannot clear an Administrator's block.** An `ExternallyRemoved` record is cleared by the
  Tenant Agent Administrator or any current Conversation Facilitator."*
- `prd.md` OQ-25 (Resolved/amended 2026-09-09 — the same row the AD-2 `BlockVersion`/`MirrorPending`
  amendment cites as its driver): *"a block is cleared only by the setting authority (evaluated at
  clear time) or the Tenant Agent Administrator, an `ExternallyRemoved` record by the Tenant Agent
  Administrator or any current Facilitator..."*

Current AD-7 text collapses all of this to: *"...is cleared only by the first two [TAA or Facilitator],
is audited, and is mirrored..."* — with no restriction tying clearance to *which* Facilitator set the
block, and no statement that a Facilitator can never clear a TAA-set block. AD-2's parallel description
of `ConversationAgentState` (the aggregate that actually stores the block) is equally silent on this:
it names `BlockVersion` and `MirrorPending` but not the setting-authority-bound clear rule.

**Why this is a real divergence risk, not a wording nit.** This is a security-relevant authorization
boundary, not a display nuance: as AD-7 is currently worded, a team could legitimately implement "any
current Facilitator, or the TAA, may clear any block" — which would let an ordinary Conversation
Facilitator undo a block the Tenant Agent Administrator deliberately imposed, directly contradicting
FR-2, the FR-33 authorization table (the exact table AD-30 says it carries: "role rights from the FR-33
matrix carried by the AD-30 principal"), and OQ-25. AD-7 explicitly binds both FR-2 and FR-33 by number,
so this is not an out-of-scope omission — it is a gap inside the AD's own declared coverage on the exact
capability (block clearing) that AD-7 exists to govern.

**Fix.** Amend AD-7's clearing clause to state the setting-authority-bound rule verbatim: a `Blocked`
record is cleared only by the specific authority that set it (re-evaluated as currently held at clear
time — a Party who has since lost the Facilitator role cannot clear) or by the Tenant Agent
Administrator; a Conversation Facilitator can never clear a block the Tenant Agent Administrator set; an
`ExternallyRemoved` record, by contrast, is clearable by the Tenant Agent Administrator or *any* current
Conversation Facilitator of that Conversation (broader, since it was not "set" by a person). Consider
also naming this restriction in AD-2's `ConversationAgentState` description, since that is the aggregate
enforcing it.

## High

None beyond the two Criticals above — every other AD-vs-AD and AD-vs-diagram cross-reference spot-checked
this round (AD-2/AD-10/AD-17 `EntryMissing` carve-out three-way agreement; AD-9/AD-10/AD-21
`CurrencyMismatch`; AD-5/AD-12 `PausedDuration` deadline formula; AD-22's computed-subject-set/TAA-
turnover branch; AD-30's `ProvisionHexa` restriction vs AD-2/AD-7) held up as internally consistent.

## Medium

None found this round beyond the Low below.

## Low

### L-1 — Class diagram's `AuditInspection` still carries an unexplained `Mode` attribute

**Location:** `ARCHITECTURE-SPINE.md`, the `classDiagram` block, `class AuditInspection { TenantId
InspectionId Mode }`.

**Evidence.** AD-22's current prose describes `AuditInspection` as "scoped to a named Conversation or a
case" (a case being "an Inspector-assigned free-text identifier grouping multiple named Conversations")
— there is no concept named `Mode` anywhere in AD-22's text, the Structural Seed, or the Naming
convention row. The diagram's `Mode` field does not correspond to any field the prose defines, and reads
as a leftover from an earlier draft of the aggregate's shape.

**Why this matters.** Low impact — a builder would consult AD-22's prose (the normative Rule) rather
than the class diagram for field-level shape, and the diagram is explicitly a sketch, not a contract.
Still, it is exactly the kind of "orphaned/renamed component" the Design Paradigm rubric item warns
about, just in the domain-model diagram rather than the Design Paradigm diagram itself.

**Fix.** Replace `Mode` with `Scope` (matching AD-22's "named Conversation or case" language) or remove
it if the diagram is meant to stay attribute-sparse.

## Checklist summary

| # | Checklist item | Result |
|---|---|---|
| 0 | Round-5 fixes actually closed | All three (C-1, M-1, L-1 of v5) independently confirmed closed, no regressions |
| 1 | Fixes real divergence points, misses none | **FAIL** — two fresh, unrelated Critical gaps (Deferred-table/AD-22 contradiction on `LegalHoldRelease`; AD-7's under-restrictive block-clear rule) |
| 2 | Every AD's Rule enforceable and prevents its divergence | AD-22's `LegalHoldRelease` rule is itself fine, but the Deferred table gives a builder who reads it instead a materially weaker (and wrong) instruction; AD-7's clear rule is enforceable but enforces less than FR-2/FR-33/OQ-25 require |
| 3 | Deferred section couldn't hide a real divergence | **FAIL** — the "Two-person rule on legal-hold release" Deferred row is not actually deferred and states the wrong current behavior |
| 4 | Named tech verified-current | Clean — no drift found in Stack table this round |
| 5 | Ratifies brownfield reality | Clean — no new brownfield claim touched this round |
| 6 | Covers driving PRD's capabilities | FR-2/FR-33/OQ-25's block-clear-authority nuance is the one capability AD-7 fails to carry through correctly |
| 7 | Inherited invariants not weakened | N/A (top-of-tree initiative spine) |
| 8 | Every structural dimension decided/deferred/open | Clean |
| 9 | Binds/Prevents/Rule structurally present and coherent | AD-7's Rule is internally coherent with itself but incoherent with the FR-2/FR-33/OQ-25 rows it binds; the Deferred table is incoherent with AD-22's Rule for the same command |
| 10 | No placeholders/unresolved assumption tags | Clean |

**Overall verdict: FAIL.** Two Critical findings, independent of each other and of everything v5 closed:
(1) the `Deferred Beyond V1` table's `LegalHoldRelease` row asserts a single-approver behavior that
AD-22's own current Rule directly contradicts (AD-22 already requires a second Compliance Inspector or
the Platform Operator to approve every `LegalHoldRelease`); (2) AD-7's block-clear rule ("cleared only by
the first two") is less restrictive than FR-2, the FR-33 role/scope table, and OQ-25 all require — none
of the three allow a Conversation Facilitator to clear a block the Tenant Agent Administrator set, and
none of the three treat clear authority as anything but tied to whoever specifically set the block,
re-checked at clear time. One Low (an orphaned `Mode` attribute on `AuditInspection` in the class
diagram) is also open but blocks nothing on its own.
