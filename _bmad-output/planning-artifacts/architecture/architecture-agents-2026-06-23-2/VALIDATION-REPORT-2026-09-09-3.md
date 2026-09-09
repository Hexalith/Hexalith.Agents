---
name: Hexalith Agents spine validation (round 3)
type: architecture-spine-validation
target: ARCHITECTURE-SPINE.md (updated 2026-09-09, status final, AD-1..AD-31; round-2 Update pass applied same day, uncommitted in the working tree)
repository_state: main @ 4ec626d (2026-09-09), working tree carries uncommitted edits to ARCHITECTURE-SPINE.md and .memlog.md from the round-2 Update pass
date: 2026-09-09
lint: 0 findings
lenses:
  - rubric walker v6 (good-spine checklist)
  - verified-current v6 (configured)
  - adversarial divergence v6 (configured)
  - brownfield drift v6 (ad hoc)
  - security / data integrity v6 (ad hoc)
---

# Spine Validation Report — 2026-09-09 (round 3)

## Gate verdict

**FAIL.** Deterministic lint is clean (0 findings). This round independently re-derived — rather than
trusted — every fix the round-5 gate (inside the round-2 Update pass) claimed had closed the prior
round's findings. Most held: the AD-5 illegal-transition Critical, the AD-22 unreachable-TAA-branch
High, the AD-15 bare-enum residue, and the AD-5 prose redundancy are all confirmed genuinely fixed
against the live text. But this round's real news is a recurring pattern — **fixing the citation, not
the defect** — plus two fresh, self-contained contradictions the round-5 pass never touched:

1. **Two of round 5's fixes only close one level up, while the rule they cite one level down still
   doesn't deliver what the fixed text now claims.** AD-7's fix for `hexa`'s own Party-deactivation
   detection asserts that AD-12 "explicitly" re-checks Party state on every side-effecting step — but
   AD-12's own operative enumeration (the one sentence that actually lists what every side-effecting
   step re-reads: Agent lifecycle, tenant enablement, per-Conversation block, kill switch) never
   mentions Party state at all, so a literal, compliant implementation still never detects `hexa`'s own
   deactivated identity. Separately, AD-22's erasure-completeness fix now correctly names the
   Conversations-posted-copy gap as a residual risk "tracked inside `LR-AUDIT-PROTECTION-DELETION`'s
   evidence contract" — but `launch-readiness-register.md` itself was never edited, and still asks only
   for `LegalHold` propagation evidence, not the symmetric `DeletionRequest` evidence the spine claims
   the register now requires. Both are the same failure mode one layer removed: a defect closed in prose
   by pointing at a sibling document/AD that doesn't actually say what the fix claims it says.
2. **A stale `Deferred Beyond V1` table row directly contradicts AD-22's own current Rule** — the
   Deferred table still tells a reader `LegalHoldRelease` ships as a single-Compliance-Inspector command
   in V1, while AD-22's Rule (amended in an earlier round) already requires a second Compliance Inspector
   or the Platform Operator to approve it.
3. **AD-7's block-clear rule is under-restrictive against FR-2, the FR-33 role/scope table, and OQ-25
   alike** — all three tie clear authority to *whoever set the block* (re-evaluated at clear time) and
   forbid a Conversation Facilitator from clearing a Tenant Agent Administrator's block; AD-7 merely says
   a block is "cleared only by the first two [TAA or Facilitator]," with no such restriction — a real
   authorization-bypass gap inside AD-7's own declared FR-2/FR-33 scope.
4. **A new AD-12/AD-5 gap on disabled-Agent retry semantics**: AD-12's disabled-Agent consequence list
   names `Approved`, `PostingPending`, and awaiting-decision states but is silent on `PostingFailed`
   (unlike its parallel kill-switch clause, which explicitly pauses the retry-window clock), and AD-5's
   `PausedDuration` deadline formula names only the kill switch as a pause source — leaving it unstated
   whether a routine Agent disable/enable cycle silently exhausts a `PostingFailed` proposal's retry
   budget or lets it keep posting under a disabled Agent.

Brownfield drift is otherwise clean news: the one genuine tracking-integrity defect this lens exists to
catch — `sprint-status.yaml` marking Story 5.2 "done" against unmet amended acceptance criteria — is
now correctly corrected to "in-progress," and no source code has changed since the last check. All
previously-tracked implementation debt (AD-2, AD-4, AD-29, AD-30, structural seed) remains open exactly
as expected, now with one item additionally logged in `external-dependency-register.md` as a formal
non-conformance record — a tracking-hygiene improvement, not new drift.

| Lens | Verdict | Critical / High / Medium / Low | Review file |
| --- | --- | --- | --- |
| Rubric walker | **FAIL** | 2 / 0 / 0 / 1 | `reviews/review-2026-09-09-rubric-v6.md` |
| Verified-current | PASS WITH FINDINGS | 0 / 0 / 1 / 4 | `reviews/review-2026-09-09-verified-current-v6.md` |
| Adversarial divergence | **FAIL** | 0 / 2 / 1 / 1 | `reviews/review-2026-09-09-adversarial-divergence-v6.md` |
| Brownfield drift | FAILS TO RATIFY, fixable | 0† / 5 / 3 / 2 | `reviews/review-2026-09-09-brownfield-drift-v6.md` |
| Security / data integrity | PASS WITH FINDINGS | 1 / 1 / 2 / 1 | `reviews/review-2026-09-09-security-data-integrity-v6.md` |
| **Total** | | **3 / 8 / 7 / 9** (27) | |

† Brownfield's one Critical (the `sprint-status.yaml` Story 5.2 tracking-integrity problem) was resolved
this round and is not counted as open; see "What's already closed" below. Brownfield's 5 Highs are all
already-tracked implementation debt requiring no spine amendment, carried in this table for the same
reason the round-2 report carried them.

Deterministic lint (`lint_spine.py`): 0 findings — no placeholders, no duplicate `AD` ids, every `AD`
carries Binds/Prevents/Rule, Stack versions pinned.

## Critical — close before any new command handler is built against AD-7, AD-22, or `launch-readiness-register.md`

### C-1 (Rubric) — `Deferred Beyond V1` table contradicts AD-22's own current `LegalHoldRelease` second-party rule

The Deferred table currently states: *"Two-person rule on legal-hold release | Product and Security own
it; V1 releases a hold with a single Compliance Inspector command carrying typed justification, while
deletion already requires two roles under AD-22."* AD-22's own Rule, in the same document, states:
*"`ExportRequest` requires this same second-party approval before any content read, never post hoc;
`LegalHoldRelease` requires the audited approval of a second Compliance Inspector or the Platform
Operator on the same subject-set and anti-collusion terms."* These describe mutually exclusive worlds
for the identical command: the AD-22 second-party gate on `LegalHoldRelease` was added in an earlier
round's Update pass, but the pre-existing Deferred-table row describing the old single-command behavior
was never revisited. A team scoping the legal-hold release story from the Deferred table alone would
ship a single-approver release path that AD-22 — and PRD OQ-30 — forbid.
**Fix:** remove the "Two-person rule on legal-hold release" row from `Deferred Beyond V1` (it is not
deferred — AD-22 already resolves it), or rewrite it to name a narrower residual gap precisely if one
still exists.

### C-2 (Rubric) — AD-7's block-clear rule omits the setting-authority restriction FR-2, FR-33, and OQ-25 all state

Current AD-7: *"...is cleared only by the first two [Tenant Agent Administrator or Conversation
Facilitator], is audited, and is mirrored..."* Three independent PRD sources state a more restrictive
rule AD-7 does not capture, even though AD-7's own Binds line cites FR-2 and FR-33 by number: FR-2's
consequence bullet, the FR-33 role/scope table, and OQ-25 all say a block is cleared only by *the
authority that set it* (re-evaluated as currently held at clear time — a Party who has since lost the
Facilitator role cannot clear) or the Tenant Agent Administrator, and explicitly that **a Conversation
Facilitator cannot clear a block the Tenant Agent Administrator set**. As worded, AD-7 would let a team
legitimately implement "any current Facilitator, or the TAA, may clear any block" — letting an ordinary
Facilitator undo a block the TAA deliberately imposed, directly contradicting FR-2/FR-33/OQ-25 on the
exact capability AD-7 exists to govern.
**Fix:** amend AD-7's clearing clause to state the setting-authority-bound rule verbatim: a `Blocked`
record is cleared only by the specific authority that set it (re-evaluated at clear time) or by the
Tenant Agent Administrator; a Facilitator can never clear a TAA-set block; an `ExternallyRemoved` record,
by contrast, is clearable by the TAA or any current Facilitator (broader, since no person "set" it).
Consider naming this in AD-2's `ConversationAgentState` description too, since that aggregate enforces
it.

### C-3 (Security) — the spine claims `LR-AUDIT-PROTECTION-DELETION` was extended to cover `DeletionRequest` symmetrically with `LegalHold`; the register itself was never edited

AD-22 and `ARCH-A-10` both now assert erasure's Conversations-posted-copy residual risk is *"tracked
inside `LR-AUDIT-PROTECTION-DELETION`'s evidence contract in `launch-readiness-register.md`"*
symmetrically with the `LegalHold` case. The actual register text (`launch-readiness-register.md` line
93, confirmed unchanged via `git log`/`git diff`) still reads: *"Includes evidence that a `LegalHold`
either propagates to (or has a confirmed accepted-risk waiver for) the posted copy of held content in
Conversations..."* — naming only `LegalHold`, nothing about `DeletionRequest`/erasure. Security
Engineering, the register's named evidence owner, would build exactly what the register's own text asks
for (`LegalHold` evidence only), record `Pass`, and the erasure-direction residual risk — the exact gap
a prior round rated Critical — would be disclosed in the spine's prose but never become an actual gate
requirement in the document that actually gates release.
**Fix:** edit `launch-readiness-register.md` line 93 to add the symmetric clause the spine already
claims exists (`"...a `LegalHold` **or a `DeletionRequest`**... the posted copy of held **or erased**
content..."`), or correct the spine's cross-reference to say the register update is still pending until
that edit lands.

## High

| # | Finding | Source |
| --- | --- | --- |
| H-1 | AD-7's fix for `hexa`'s own Party-deactivation detection claims AD-12 "explicitly" re-checks Party state on every side-effecting step; AD-12's own four-item enumerated re-read list (Agent lifecycle, tenant enablement, per-Conversation block, kill switch) never names Party state at all — a compliant, literal implementation of AD-12's own words never detects a deactivated `hexa` identity, reproducing the exact attribution-integrity gap the fix was meant to close. | Adversarial |
| H-2 | AD-12's disabled-Agent consequence clause is silent on `PostingFailed` (unlike its parallel kill-switch clause, which explicitly pauses the retry-window clock via `PausedDuration`); AD-5's `PausedDuration` formula names only the kill switch — a routine Agent disable/enable cycle's effect on in-flight `PostingFailed` retries is unstated, and two literal implementations diverge on whether the retry budget silently exhausts or a disabled Agent keeps posting. | Adversarial |
| H-3 | AD-30's new `Platform` freshness re-check names "the Tenants-projection" as its source — the same tenant-scoped wording used for `User`/`Administrator` — for a role FR-19 explicitly defines as the one non-tenant-scoped role in the system; the authoritative source/scope key is left unnamed, reintroducing the exact ambiguity a prior round's fix was supposed to close. | Security |
| H-4 | AD-2's platform-scoped `ProviderCatalog`/`TenantProviderEnablement` split still doesn't exist in code (`ProviderCatalogAggregate.cs` still documents itself "tenant-scoped"); now also formally logged as `NC-5.3-PLATFORM-CATALOG-SCOPE` in `external-dependency-register.md`. | Brownfield (already-tracked, Story 5.3) |
| H-5 | AD-4's `ConfigurationVersion` still not bumped by `AgentActivated`/`AgentDisabled` in `AgentAggregate.cs`. | Brownfield (already-tracked, DW-4/Story 5.2) |
| H-6 | AD-29's single `AgentsIdentity` canonicalizer still absent; five separate identity helper classes remain. | Brownfield (already-tracked, Stories 6.4/7.1–7.3) |
| H-7 | Structural seed still carries `Server/Aggregates` and `Application/Tools` placeholders, and `Hexalith.Agents.IntegrationTests` is still absent from `test/`. | Brownfield (already-tracked, Story 5.6) |
| H-8 | AD-30's HMAC-tagged trusted-extension requirement has no code counterpart (plain string-equality gate); `Agents.PlatformOperator` FrontComposer policy still absent from code. | Brownfield (already-tracked, Stories 5.3/5.4/5.5) |

## Medium (7 total — see review files for full detail)

- Stack table's `Hexalith.EventStore` gitlink (`1b6f08d4`) is stale against the actual committed
  submodule (`12d2dfc1`, bumped the same day as the spine's own `updated` stamp); the other four gitlink
  rows are exact matches (Verified-current).
- AD-10's widened `EntryMissing` carve-out claims exact equivalence with AD-2's carve-out while adding an
  "in-flight interaction's" restriction AD-2's own unqualified "previously-snapshotted" wording doesn't
  contain (Adversarial).
- The Consistency Conventions table's Provider-attempt fingerprint row states plain SHA-256 while AD-29's
  blanket rule requires HMAC-SHA-256 for "evidence fingerprints"/"per-message digests" — an unresolved
  textual ambiguity over whether the embedded per-message digests are separately keyed (Security).
- Keyed digests/fingerprints of erased content survive erasure indefinitely under a long-lived,
  non-rotated per-tenant `DigestKey`, with no stated key-lifecycle bound or residual-risk framing in
  AD-22 (Security).
- This round's three targeted prose edits (AD-16 composite-wrapper cross-ref, AD-22 case-scope/TAA
  historical-fact clarification, SDK-band dating) are internally consistent — confirmed, no residual
  defect (Brownfield).
- `EXT-CONV-AI-1` is more architecturally scaffolded in Conversations than "Uncommitted" might suggest
  (membership/role types exist), but the removal and typed-deletion-signal seam pieces are genuinely
  absent — matches its correctly-`Uncommitted` register status, not a contradiction (Brownfield).
- Sibling module `project-context.md` files carry no Agents-specific seam content to conflict with;
  `EXT-SECRETS-1` correctly Platform-owned rather than EventStore-owned (Brownfield).

## Low (9 total)

`AuditInspection`'s class-diagram `Mode` attribute is an orphaned leftover with no corresponding concept
in AD-22's current prose ("scope: named Conversation or case") — replace with `Scope` (Rubric). The two
Stack-table rows added last round for `Microsoft.NET.Test.Sdk`/`xunit.runner.visualstudio` still omit
actual version-pair detail their sibling rows carry (real values: `18.6.0`→`18.9.0` catalog and
`3.1.5`→`4.0.0` catalog); the SDK-servicing claim's supporting URLs still aren't cited in frontmatter
`sources`; MediatR's now-commercial license remains unremarked (unused by Agents, no action needed);
`bunit` has drifted one more patch behind (`2.9.0` vs. current `2.10.3`) — Verified-current (4 items).
`AuditInspection`'s "case" now has a prose definition but still no `Scope`/`Case` field in the
classDiagram or Naming table (Adversarial). AD-22's "the tenant's FR-24 review window" isn't inline-tagged
`[ASSUMPTION A-19]`, unlike every other PRD-sourced numeric bound in the document (Security). Confirmed
unchanged/no-new-drift entries (submodule pins beyond EventStore, gate ids, matrix version; code
byte-identical since the last brownfield check) — Brownfield (2 items).

## What's already closed (re-verified, not re-litigated)

This round independently re-derived, rather than trusted, every claim the round-5 gate made: AD-5's
illegal `Approved -> PostingFailed` transition fix, AD-22's previously-unreachable TAA-turnover branch,
AD-15's bare-enum-styled `AgentReadinessStatus` language, AD-5's redundant system-abandon phrasing, the
`.NET SDK` servicing-lapse date and its `ARCH-A-4` mirroring, and the `sprint-status.yaml` Story 5.2
tracking-integrity correction — all confirmed genuinely closed against the live text. Two of these
closures (AD-7's Party-deactivation claim and AD-22's erasure/register claim) are reopened above at C-1/
H-1/C-3 because the fix's own cited authority — a sibling AD or an external register — doesn't actually
say what the fix now claims it says; this is a distinct, narrower defect than the original gap, not a
regression back to the original one.

## Recommendation

Roll this report into an Update pass. Priority order:
1. **C-1/C-2/C-3** — the Deferred-table/AD-22 contradiction, AD-7's block-clear authority gap, and the
   `launch-readiness-register.md` edit — all spine-text-only or register-only fixes, no PRD dependency,
   no fresh design work.
2. **H-1/H-2 (adversarial)** and **H-3 (security)** — close the "fixed the citation, not the defect"
   pattern by adding Party state to AD-12's own enumerated re-read list, stating the `PostingFailed`
   consequence of Agent disablement explicitly, and naming AD-30's `Platform` role source unambiguously
   as the `system`-tenant Tenants-projection.
3. **H-4..H-8 (brownfield)** — implementation debt already tracked in Stories 5.2–5.6 and DW-4; no
   spine amendment needed, only code catching up.
4. Medium-tier items, same pass, especially the AD-13/AD-29 fingerprint-keying ambiguity (a live security
   question, not a documentation nit) and the `DigestKey` rotation/lifecycle gap.

Spine and memlog were **not modified** by this run (Validate intent — report only).
