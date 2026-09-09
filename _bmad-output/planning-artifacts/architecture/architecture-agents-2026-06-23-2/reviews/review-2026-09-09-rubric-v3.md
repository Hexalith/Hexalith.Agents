---
review: rubric-v3
target: ARCHITECTURE-SPINE.md
target_updated: 2026-09-09
reviewer: independent rubric walker (third pass — delta/verification against rubric-v2)
verdict: PASS WITH FINDINGS (0 critical / 0 high / 0 medium / 2 low / 2 info)
---

# Rubric Review v3 — Hexalith Agents Architecture Spine (2026-09-09, delta pass)

Scope note: this is a delta/verification pass, not a first read. It (a) confirms whether the two
open findings from `review-2026-09-09-rubric-v2.md` (HIGH: AD-30 vocabulary drift into `epics.md`
Story 5.4; MEDIUM: `SecurityEventLog` "rate-bounded" with no mechanism) are actually closed by the
current spine text, (b) re-runs checklist items 1, 2, and 9 specifically against every AD amended
since v2 (AD-2, AD-4, AD-5, AD-7, AD-9, AD-12, AD-14, AD-17, AD-21, AD-22, AD-29, AD-30, plus new
rows ARCH-A-8/9/10), (c) checks that cross-references between amended ADs read as one coherent rule,
and (d) does a normal but effort-weighted fresh skim of the rest of the ~758-line document.

## 0. Disposition of rubric-v2's open findings

**HIGH (AD-30 principal-kind vocabulary contradicted in `epics.md` Story 5.4) — CLOSED.**
`epics.md` line 1360 now reads: "it selects exactly one AD-30 principal kind from `User`,
`Administrator`, `Platform`, or `Workflow` according to the operation family..." — this is character-
for-character the same four-way vocabulary AD-30 defines. A repository-wide grep for a `Service` or
`System` principal kind in both the spine and `epics.md` returns nothing. Closed cleanly; no residual
trace of the old four-way enum remains anywhere in either document.

**MEDIUM (`SecurityEventLog` "rate-bounded" with no defined mechanism or register cross-reference) —
CLOSED.** AD-2 no longer uses "rate-bounded." It now reads: `SecurityEventLog` (`TenantId`, `UtcDay`;
content-free authorization denials and security events, **daily-partitioned by the aggregate key as
the sole V1 volume mitigation [ASSUMPTION ARCH-A-9]**). The new Architecture Assumptions row is
concrete and owned: "`SecurityEventLog`'s daily `(TenantId, UtcDay)` partition is the only V1
mitigation against unbounded burst volume; no numeric rate bound is set," owner **Security**, retired
when "Security specifies a numeric limit or a register section." This is exactly the fix the v2
finding asked for (either cite a register number or drop the unsubstantiated qualifier and rely on
partitioning alone, tracked) — the memlog (`memlog.md` line 153) confirms this was a deliberate,
recorded decision, not an incidental rewrite. Closed.

Both of v2's headline findings are fully resolved in the current text with no new "reopening" residue
found anywhere else in the spine or in `epics.md`.

## 1. Does the spine fix the real divergence points one level down (stories/epics), missing none?

Re-checked specifically for the 12 amended ADs. All Binds lines are intact and each amendment reads as
a strict superset of the prior invariant (added detail/assumption tags, not removed coverage). Spot-
checked `epics.md` citations of the amended ADs (AD-4, AD-5, AD-7, AD-9, AD-30 all appear correctly in
Story 5.1/5.2/5.4/6.x Evidence Manifests with clause-level `OwnedClauses` such as
`AD-7.party-reference-only`, `AD-30.principal-kind-and-ingress-HMAC`, `AD-30.platform-principal`,
`AD-4.lifecycle-configuration-version`) — no story cites a vocabulary, field, or mechanism the amended
AD text no longer supports. No new divergence found. Clean.

## 2. Binds/Prevents/Rule triads — real teeth, not aspiration (amended ADs)

Walked all twelve amended ADs plus the three new ARCH-A rows:

- **AD-2**: teeth intact; the `SecurityEventLog` clause now states a concrete mechanism (daily
  partition) rather than an unbacked adjective, with the residual gap tracked as ARCH-A-9. Improvement
  over v2, no new gap.
- **AD-4**: three independently-defined monotonic counters (`ConfigurationVersion`,
  `InstructionsVersion`, `ApproverPolicyVersion`) with explicit increment triggers; concrete re-read
  requirement under AD-12. Teeth intact.
- **AD-5**: the amended `PausedDuration` clause gives a computable deadline formula
  (`origin + window + PausedDuration`) rather than a vague "pauses while stopped" — concrete and
  testable.
- **AD-7**: the new global "PartyId-reference-only binds every Party-adjacent field" sentence gives a
  structural enforcement claim (no caching without an AD-7 amendment) with three named anchor points
  (AD-2, AD-8, AD-30). See Finding (LOW) below on one imprecision in this sentence.
- **AD-9, AD-12, AD-14, AD-17, AD-21, AD-22, AD-29, AD-30**: each retains or strengthens concrete,
  testable mechanisms (e.g., AD-12's numeric kill-switch triggers — 50%/10%/7-day — unchanged; AD-22's
  nine named lock-bearing families now cross-cited identically from AD-12; AD-30's per-principal
  allowlisted command sets unchanged in rigor). No aspiration-only prose found in any amended clause.

No MEDIUM or HIGH finding in this section — the one MEDIUM open from v2 is closed (§0) and no
replacement gap of similar severity was introduced by the amendment set.

## 3. Deferred Beyond V1 — could any deferred item let two units diverge?

Unchanged from v2's clean read; the seven rows are unaffected by this amendment round. No finding.

## 4. Cross-reference coherence between amended ADs

Checked the three cross-references named in the task brief, plus the AD-12/AD-22 lock-bearing-family
list (itself a cross-reference introduced/confirmed by this amendment round):

- **AD-5 citing AD-12's `PausedDuration`**: AD-5 says the retry deadline is "origin plus window plus
  the AD-12 kill-switch `PausedDuration`... since AD-12 pauses this clock while the tenant kill switch
  is pulled." AD-12 independently states the identical formula: "AD-5's 15-minute retry-window deadline
  is `origin + window + PausedDuration` evaluated under AD-28." Same formula, same attribution
  direction, same units. Coherent — reads as one rule split across two ADs, not two conflicting ones.
- **AD-21 citing AD-30's `OnBehalfOfPartyId`**: AD-21 says accounting is "keyed by `OnBehalfOfPartyId`
  per attempt (AD-30) — the caller for the first generation, the requesting Approver for each
  regeneration." AD-30 defines `OnBehalfOfPartyId` identically: "the Party whose command initiated the
  current step (the caller for the first generation, the requesting Approver for a regeneration)."
  Word-for-word consistent. Coherent.
- **AD-12/AD-22 nine lock-bearing families**: AD-12 enumerates the lock-bearing subset as nine named
  families (`ProposalResolution` ... `AgentActivation`); AD-22 independently states "the ... rule
  applies to all nine AD-12 lock-bearing families" and repeats the identical nine-item list verbatim,
  including the same parenthetical aliases (`LegalHold` also `LegalHoldRelease`, etc.). Coherent.
- **AD-2's `Platform`-created `hexa`, AD-30's create-only `AgentSetupMutation`**: AD-2 states `hexa` is
  "created at tenant enablement under the `Platform` principal"; AD-30 independently names "the
  create-only `AgentSetupMutation` that provisions `hexa` at tenant enablement" as one of the
  operations the `Platform` principal may dispatch. Consistent both ways.

**Finding (LOW) — AD-7's new global sentence overclaims what AD-30 actually defines.** AD-7's added
sentence reads: "This PartyId-reference-only rule binds every Party-adjacent field anywhere in the
Agents domain and its projections — the `Agent` Party identity link (AD-2), `ApproverPolicy`'s
predefined Parties (AD-8), and every principal's `PartyId` (AD-30) alike." Read literally, this asserts
all four AD-30 principal kinds carry a `PartyId`-shaped field. But AD-30's own principal definitions
give an explicit field list only for `User` (`TenantId`, `PartyId`, roles...) and, in substance, for
`Workflow` (`OnBehalfOfPartyId` equal to the snapshot caller); `Administrator` is defined only as "a
reserved `actor:*` extension populated... after a fresh Tenants-projection role check" with no `PartyId`
field named, and `Platform` is defined only as "the `system`-tenant Platform Operator, carried as the
reserved `actor:agentsProviderAdmin` extension" — also no `PartyId` field. So "every principal's
`PartyId`" is not literally true of two of the four principal kinds as AD-30 currently defines them.
This does not break any enforceable rule (the constraint is vacuously satisfied for principals that
carry no such field), and it doesn't contradict AD-30 in a way that would misdirect an implementer —
but it is exactly the kind of "reads as bolted-together, not fully reconciled" imprecision the task
asked to check for. Tightening to something like "the `User` principal's `PartyId` and `Workflow`'s
`OnBehalfOfPartyId` (AD-30) alike" would remove the overclaim.

## 5. Ratifies rather than contradicts brownfield reality

Not re-walked in depth this pass (unaffected by the amendment set); v2's read stands. No finding.

## 6. PRD/epics scope coverage

Unaffected by this amendment round beyond the AD-30/epics.md fix already covered in §0. No finding.

## 7. Inherited Invariants (parent-spine check)

Unchanged; still correctly N/A as the top-of-tree initiative spine. No finding.

## 8. Structural dimensions the altitude owns

Unaffected by this amendment round. No finding.

## 9. Placeholders, template scaffolding, unresolved `[ASSUMPTION]` tags

A fresh search for `TBD`, `TODO`, `FIXME`, `<insert...>`, `lorem ipsum` again finds zero template
artifacts (the sole "placeholders" hit remains the substantive AD-17 usage about evidence quality, not
a leftover marker).

The document now carries 18 bracketed inline `[ASSUMPTION ...]` citations (one more than v2's 16 — the
new `[ASSUMPTION ARCH-A-9]` in AD-2) and ten total Architecture Assumptions rows (ARCH-A-1 through
ARCH-A-10, one of which, ARCH-A-5, is marked `**RETIRED 2026-09-09.**` with a named retiring document
that does exist on disk — `sprint-change-proposal-2026-09-09.md`).

**Finding (LOW/INFO) — ARCH-A-10 is referenced inline without the document's own bracket convention.**
Every other Architecture Assumption that is cited inline in an AD's prose uses the bracketed
`[ASSUMPTION ARCH-A-N]` tag (e.g. AD-2's `[ASSUMPTION ARCH-A-9]`, AD-30's `[ASSUMPTION ARCH-A-2]`,
AD-27's `[ASSUMPTION ARCH-A-1]`, AD-28's `[ASSUMPTION ARCH-A-7]`, the Stack table's
`[ASSUMPTION ARCH-A-4]`). AD-22's new residual-risk sentence instead reads "...tracked inside
`LR-AUDIT-PROTECTION-DELETION`'s evidence contract in `launch-readiness-register.md` and **as spine
assumption ARCH-A-10**..." — prose reference, no brackets. The assumption itself is substantively fine
(named, owned, retirement-conditioned, and doubly cross-referenced to the register), so this is a pure
tagging-convention nit, not a coverage gap — but a reader or tool grepping for `[ASSUMPTION` to enumerate
all live blockers (which is exactly what `RQ-1`'s `UnretiredAssumption` mechanism in AD-17 does by
description) would silently skip ARCH-A-10 unless it separately parses this prose form. Worth a
one-word fix (wrap it in brackets) for mechanical consistency with the rest of the document.

**Finding (INFO, carried forward from v2) — ARCH-A-2 remains the single highest-priority assumption to
retire.** Unchanged from v2: ARCH-A-2 (caller `PartyId` resolution at ingress) is load-bearing across
every command via AD-30 and is still open. The two new assumptions this round (ARCH-A-9 `SecurityEventLog`
volume bound, owned by Security; ARCH-A-10 Conversations-retention/hold gap, owned by Governance +
Compliance) are each scoped to a narrower blast radius (one aggregate's write volume; one cross-module
retention edge case, respectively) and do not change this priority ordering. No action needed beyond
what v2 already recommended; restating for continuity since this is a delta pass.

Also checked (per item 9's normal scope): the 14-aggregate inventory in AD-2 still matches the
`Hexalith.Agents/` folder list in the Structural Seed and the class diagram's 14 aggregate classes
exactly; no drift introduced by the amendment round.

## Summary table

| # | Checklist item | Result |
|---|---|---|
| 0 | v2's open findings actually closed | Both closed — HIGH (epics.md AD-30 vocabulary) and MEDIUM (`SecurityEventLog` rate-bounded) |
| 1 | Fixes real divergence points one level down (amended ADs) | Clean — no new divergence introduced |
| 2 | Binds/Prevents/Rule teeth (amended ADs) | Clean — all twelve amended ADs retain or strengthen concrete mechanisms |
| 3 | Deferred section materiality | Unaffected; clean (v2 finding stands) |
| 4 | Cross-AD coherence of amended prose | Mostly yes; **LOW**: AD-7's "every principal's `PartyId` (AD-30)" overclaims what AD-30 defines for `Administrator`/`Platform` |
| 5 | Brownfield ratification | Unaffected; not re-walked in depth |
| 6 | PRD/epics scope coverage | Unaffected beyond §0's fix |
| 7 | Inherited Invariants | N/A, unchanged |
| 8 | Structural dimensions decided/deferred/open | Unaffected |
| 9 | Placeholders / stale `[ASSUMPTION]` tags | Clean mechanism; **LOW/INFO**: ARCH-A-10 cited inline without the document's own bracket convention; **INFO**: ARCH-A-2 still the highest-priority assumption to retire, unchanged from v2 |

**Overall verdict: PASS WITH FINDINGS — 0 critical, 0 high, 0 medium, 2 low, 2 info.** Both findings
this round are cosmetic/precision issues in freshly-added prose, not gaps that would let two
independently-built stories diverge. The amendment round fully closes both substantive findings from
`review-2026-09-09-rubric-v2.md` (the AD-30 vocabulary drift into `epics.md`, and the unbacked
`SecurityEventLog` "rate-bounded" qualifier) with no new finding of comparable severity introduced.
