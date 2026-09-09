# Polish — Structure Lens (bmad-review) — `prd.md`, second 2026-09-09 update

- **Target:** `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md` (971 lines, 23,299 words before this pass; 999 lines, 23,343 words after both passes — the growth is bullet scaffolding, not new content)
- **Scope:** material added or changed on 2026-09-09 per the memlog's last ~25 entries and `review-rubric.md`, `review-adversarial-general.md`, `review-consistency.md`: §0, §3 new/amended glossary entries, FR-1, FR-2, FR-3, FR-4, FR-7 re-check bullet, FR-11/FR-12, FR-18 (transition table and new bullets), FR-21, FR-24, FR-25, FR-27, FR-28, FR-30, FR-32, FR-33, FR-34, §8 preamble and seams, §8.1, §9, §12, §13 rows OQ-16..OQ-30.
- **Constraints honored:** no requirement meaning, typed identifier, number, ID, or section order changed; FR-18 table and §13 rows not condensed; every `[ASSUMPTION A-n]` tag left in place; spaced em dashes and US spelling kept.
- **Purpose/audience read:** this document exists to help product, UX, architecture, implementation, QA, and release-readiness readers agree on and build the governed V1 `hexa` capability, with the launch gate and dependency status stated up front.
- **Structure model:** Strategic/Context (Pyramid). The document already conforms: status and readiness verdict lead (§0), grouping is by feature area, MECE holds across §4 groups, and evidence (§11, §12) follows rather than leads. No model-fit finding.

## Findings

| Pass | Original Text | Revised Text | Changes |
| --- | --- | --- | --- |
| structure | FR-2 block bullet — one 230-word bullet covering set, mirror, clear, and audit rules | CONDENSE (shape only): lead sentence with `[ASSUMPTION A-9]` kept, then four sub-bullets — Setting, Mirroring, Clearing, audit | Applied. Reader can find the clear rules without re-reading the mirror rules; zero words dropped (+12 for labels) |
| structure | FR-7 scheduled re-check bullet — one 330-word bullet mixing cadence, authoritative/unavailable classification, three abandonment outcomes, unavailable-pass handling, and the later-action rule | CONDENSE (shape only): lead sentence with `[ASSUMPTION A-20]` kept; five sub-bullets (classification; `SourceConversationUnavailable`; `NoEligibleApprover` two-pass rule; unavailable pass; later actions); closing sentence kept | Applied. Each outcome now has its own line, matching the FR-2 membership sub-bullet style; no rule removed |
| structure | FR-24 inspection bullet — one 300-word bullet covering three inspection paths, the computed subject set, second-party rules, post-hoc window, and audit surface | CONDENSE (shape only): lead sentence, then three sub-bullets (participant-based; Eligible Approver/compliance; compliance inspection rules with `[ASSUMPTION A-19]` in place); "Inspection fails closed outside these paths" kept as the closing line | Applied. The three paths §9 cross-references are now visibly three |
| structure | §8.1 preamble — one 330-word paragraph: Spine citation, open-range rule, retirement rule, bidirectional reconciliation, three Spine defects, table authority | Split into four paragraphs and a three-item list for the AD-5 / AD-22 / AD-12 defects; text verbatim | Applied. Defects are scannable; Architecture can act on them as a list |
| structure | §12 SM-C4 — one 200-word bullet defining blocked, failed, and unavailable call classes plus tracking and signal rules | CONDENSE (shape only): lead, three class sub-bullets, then the exclusions, tracking, signal, and counterbalance sentences | Applied. "§3 and FR-25 cite this list" now sits beside the blocked-reason list it refers to; "not blocked calls" restated as "never counted as blocked calls" on the failed-calls line |
| structure | FR-18 bullet "When the retry budget is exhausted … until one of the following occurs …" duplicates guards carried by the four `PostingFailed` exit rows of the transition table | CONDENSE to a reference to the table rows | **Rejected.** The prose list is the budget-exhausted subset (four paths), while the table also carries `RemovedInConversations` and `LateConfirmed` exits; a reference would either misstate the count or drop the "no Conversation read access" and "re-check covers `PostingFailed`" conditions. PRESERVE |
| structure | FR-2 negative-test sentences ("A membership step that is only an idempotent join does not satisfy …") | MOVE to memlog (adversarial L-7) | **Rejected.** Cutting drops a testable non-conformance rule |
| structure | Suspension semantics stated in FR-3, the FR-18 guard cells and bullet, and FR-28 kill-switch bullet | MERGE into FR-28 with pointers | **Rejected.** Each restatement is the consequence in its own FR; merging would drop the FR-3 and FR-18 rules from where their tests are keyed |
| structure | Pre-post removal outcome stated in FR-2 (membership-failure bullet and block bullet) and FR-18 re-validation bullet | MERGE | **Rejected.** Same reason; cross-referenced restatements, not redundancy |
| structure | §13 preamble (amendment history) and the OQ-24..OQ-30 rows | CONDENSE | **Rejected** by constraint (rows must not be condensed; the intro cut was declined on 2026-09-08 and again on 2026-09-09) |
| structure | FR-28 kill-switch effects clause chain and trigger-review sub-bullet | List the effects as sub-bullets | Not applied as structure (would add a third nesting level under a bullet that already carries trigger sub-bullets); handed to the prose lens as sentence splits |
| structure | Glossary "Proposed Agent Reply" is filed between "Provider" and "Provider/Model Selection" | MOVE before "Provider" | Not applied: entry is unchanged 2026-09-09 material, outside scope. Recorded as an inconsistency for the caller |

## Summary

- Recommendations: 12 (5 applied, 5 rejected, 2 not applied/out of scope).
- Estimated reduction if all accepted: 0 words (all applied items are shape-only); the applied set added 44 words (+0.2%) of bullet labels and connective sentences.
- No length target was provided.
- Comprehension trade-offs: none; every applied change adds whitespace and scannability to human-read rule sets and removes nothing.
