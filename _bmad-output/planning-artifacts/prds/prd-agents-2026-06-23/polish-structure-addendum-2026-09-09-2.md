# Polish — Structure Lens — addendum.md — 2026-09-09 (second update)

- **Lens:** structure (bmad-review, `lenses=structure,prose`, structure first)
- **Target:** `addendum.md` only; attention on the Source Inputs bullet and the "Options Considered — Second 2026-09-09 Update" section added today
- **Purpose read:** this document exists to help downstream planning readers (humans and agents) understand the non-authoritative context behind `prd.md` — its inputs, comparative research, and rejected alternatives — without mistaking any of it for a requirement.
- **Audience / reader type:** humans (Product, Architecture, and downstream planning agents)
- **Structure model:** Strategic/Context (Pyramid); authority statement first, supporting context grouped below, decision-record bullets last
- **Length before:** 1,215 words (title 27; Source Inputs 42; Discovery Research Notes 190; Options Considered 2026-09-09 Reconciliation 361; Options Considered — Second 2026-09-09 Update 570). No length target given.

## Findings

| Pass | Original Text | Revised Text | Changes |
| --- | --- | --- | --- |
| structure | §Options Considered in the 2026-09-09 Reconciliation — SM-3 bullet, paragraph "All three left a metric…" immediately after the nested list | MOVE the paragraph out of the third nested item: insert a blank line before it | Without a blank line the paragraph is a lazy continuation of the nested bullet "cap the OQ-3 expiry range…", so it renders as part of that option rather than as the verdict on all three. Impact: 0 words. **Applied.** |
| structure | §Options Considered — Second 2026-09-09 Update — intro line "Rejected alternatives from the decisions OQ-24 through OQ-30 record." | QUESTION → add scaffolding: state provenance (validation-report pass vs reviewer gate), note the one deferral, and give a key for the identifiers this section introduces (Spine, AD-n, M-n) parallel to the first section's identifier key | The section uses "Spine", "AD-12/AD-28/AD-5", "M-2/M-12", and "the reviewer gate" with no definition in this file; the first Options Considered section defines its identifiers, this one did not. Impact: +55 words. **Applied** (wording in the prose report). |
| structure | §Options Considered — Second 2026-09-09 Update — bullet "Scanning human edits at edit time … Deferred, not rejected" | QUESTION: a deferred item sits in a list introduced as "Rejected alternatives"; either MOVE to the end or widen the intro | Widened the intro to "Rejected alternatives (and one deferral)" instead of moving the bullet, so the list keeps its decision order. Impact: +3 words. **Applied via intro.** |
| structure | §Source Inputs — bullet 2 names `review-rubric.md`, `review-adversarial-general.md`, `review-implementation-drift.md` (unsuffixed) as first-reconciliation inputs; bullet 3 says the second run's reviewer files were archived under `-2026-09-09-validate` suffixes | QUESTION for the author: do the unsuffixed names still hold the first reconciliation's reviewer output, or were they rotated by the second validate run? | A reader cannot tell which run the unsuffixed files now belong to, and `review-implementation-drift.md` is deleted in the working tree at polish time (`git status`), so that reference currently dangles. Not an editorial fix — it is a cross-reference to inputs. Impact: 0 words. **Rejected (out of polish scope: cross-references are frozen).** |
| structure | §Source Inputs — "in this folder" repeated in bullets 2 and 3 | CONDENSE: hoist "in this folder" into a lead-in | Saves ~3 words at the cost of reshaping the bullets. **Rejected (negligible gain).** |
| structure | Headings "Options Considered in the 2026-09-09 Reconciliation" vs "Options Considered — Second 2026-09-09 Update" | PRESERVE both headings as written | Parallel renaming was considered; rejected because other run artifacts cite the headings and the constraint keeps heading case and the two sections separate. Impact: 0 words. |
| structure | §Options Considered (both sections) | PRESERVE as two sections | Each is keyed to a distinct decision range (OQ-16..OQ-23 vs OQ-24..OQ-30) and a distinct run; merging would lose the provenance. Impact: 0 words. |
| structure | §Discovery Research Notes | PRESERVE | Comparative context per bullet with source; already polished in the prior run. Impact: 0 words. |

## Summary

- **Total recommendations:** 8 (3 applied, 2 rejected, 3 PRESERVE)
- **Estimated change if all applied:** +58 words (+4.8% of 1,215); no cuts recommended
- **Meets length target:** no target specified
- **Comprehension trade-offs:** none; the additions are scaffolding for identifiers the section already relies on
- **Constraints honored:** no rejected-alternative rationale, identifier, or `prd.md` cross-reference changed; spaced em dashes and US spelling kept; heading case unchanged; the two Options Considered sections not merged
