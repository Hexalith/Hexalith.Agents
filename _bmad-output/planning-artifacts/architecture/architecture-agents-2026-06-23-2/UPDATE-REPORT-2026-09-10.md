# Architecture Spine Update Report - Hexalith Agents - 2026-09-10 (round 5)

- **Spine:** `ARCHITECTURE-SPINE.md` (status `final`, updated 2026-09-10, AD-1..AD-31; `architecture_assumption_index_version` 3 -> 5)
- **Trigger:** `VALIDATION-REPORT-2026-09-09-4.md` (round 5, v8 lenses) recommended rolling its findings into an Update pass, priority-ordered by the user.
- **Path:** autonomous application per the user's stated priority order; two genuine design calls (C-5, and the new AD-8 exclusion surfaced this round) are flagged rather than silently decided - both landed as `[ASSUMPTION ARCH-A-n]` rows for Product/Architecture review, not silent authority.

## What changed

### Pass 1 - closed round-5's own findings, in the user's stated priority order

| Fix | Closes |
| --- | --- |
| AD-5 rewritten: human abandon legal from `Approved` (Eligible Approver; TAA, audited, no Conversation read access), `MessageId` lookup always skipped for any `Approved`-origin exit, `PostingWindowElapsed` system-abandonment bound and `PostingPending` stored attempt deadline added as first-class rule text | C-1 (AD-5 vs FR-18, three points) |
| AD-12's flat-20/`(Party,Conversation,reason)`-tuple "Second-update" paragraph superseded by a "Third-update" paragraph adopting FR-28/A-17's distinct-calling-Party ratio, 3-Party convening floor, and scaled minimum sample verbatim | C-2 (kill-switch trigger formula) |
| AD-13's acceptance-order sentence rewritten to the current FR-8 ten-step order (restoring the step-3 block check); Eligible Approver resolution rescoped to Confirmation mode only, with the Automatic-mode Facilitator substitution stated explicitly | C-3 (FR-8 order, Approver scope) |
| AD-30's `Workflow`-principal envelope no longer fixes `OnBehalfOfPartyId` to "the snapshot caller"; it now points at the per-attempt-varying rule stated later in the same paragraph | C-4 (self-contradiction) |
| AD-11 rewritten: `ConversationContextPolicy` stays an inline, self-versioned `Agent` field (not a 15th AD-2 aggregate); "publishes... version[s]" framing struck | C-5 (aggregate-vs-field design call - flagged, not silently assumed) |
| AD-29/AD-13/AD-22 all gain `DigestKeyVersion`: every persisted digest (idempotency record, `ProviderAttempt` fingerprint, per-message `audit-evidence` digests) now carries the key version it was computed under; re-verification and idempotency-conflict comparison always recompute against the recorded version | C-6 (DigestKeyVersion binding) |
| AD-6 now names seven `EXT-CONV-AI-1` seams (seam 7: message retraction, A-28); Deferred Beyond V1 table's retraction row updated | H-1 |
| `AgentCallOperationStatus` renamed to `AgentInteractionStatus` everywhere (AD-5, AD-15), matching FR-8's actual contract name | H-2 |
| AD-7's Second-update membership-reconciliation paragraph gated `ExternallyRemoved` re-admission behind A-22, added the FR-2 roster-freshness condition, and stated the outbound re-admission mirror call a clear issues - also removing AD-7's direct `ExternallyRemoved -> Joined`-on-presence transition, so it now matches AD-2's graph exactly | H-3 and H-5 (closed together) |
| AD-14's attestation paragraph states the FR-34 signed-identity pin explicitly (identity vs. liveness split) alongside the canary | H-4 |
| AD-8's Eligible Approver predicate gains a fourth exclusion: not the Approver who requested a regeneration of the version under decision | H-6 |
| Frontmatter `binds:` line: `OQ-1..OQ-30` -> `OQ-1..OQ-31` | Medium |
| AD-17's blocker vocabulary gains `SuspensionReviewOverdue` (FR-28/FR-30) and `DeferredAssumption` (FR-28 item 9) | Medium |
| AD-21's stale "FR-8 step 4" citation corrected to "step 5" | Medium |
| AD-22 gains the FR-24/A-23 post-hoc compliance-inspection aggregation bound (5 Conversations/30 days) | Medium |
| Stack table's `Hexalith.EventStore` gitlink corrected `e302432c` -> `0994c378`, re-pulled fresh immediately before close | Medium |
| `global.json` SDK pin bumped past the exposed `10.0.3xx` band; Stack table and `ARCH-A-4` reworded (see Pass 2 - this needed a same-round correction) | Medium; separately-tracked `ARCH-A-4` urgency |
| `sprint-status.yaml` Story 5.3's `backlog` line gets a one-line annotation noting the completed prior implementation underneath it | Medium (brownfield M1, outside the spine) |
| AD-10's `EntryMissing`-wording vs. AD-2's carve-out wording | Deliberately deferred again (low-confidence, recurring since round 3; same revisit condition as round 4) |

### Pass 2 - reviewer gate (v9) on the pass-1 spine, then fixed what it found

Ran rubric, verified-current, adversarial-divergence, brownfield-drift, and security/data-integrity as five parallel subagents against the pass-1 spine - the same five lenses round 5's own validation used. None trusted this round's own "closed" claims either.

| Fix | Closes |
| --- | --- |
| The initial `10.0.400` `global.json` pin escaped the abandoned `10.0.3xx` band but, per live `dotnet/core` release notes, did not itself contain the `CVE-2026-69522` fix (that shipped one bundle later, in SDK `10.0.401`). `global.json` corrected to pin `10.0.401` directly; Stack table and `ARCH-A-4` reworded and re-dated | verified-current Critical (F1) |
| AD-22's short-lived-digest sentence still listed the `ProviderAttempt` fingerprint as "no rotation risk," contradicting AD-13's/AD-29's own new `DigestKeyVersion` persistence for that exact fingerprint; reworded so only an ephemeral, never-persisted verdict-cache key is rotation-risk-free | security-data-integrity Critical; adversarial Critical (same root cause, two lenses) |
| AD-29's digest rule reworded to separate "is HMAC-SHA-256" (all five digest kinds) from "carries `DigestKeyVersion`" (only the three persisted kinds); verdict-cache keys explicitly excluded | security-data-integrity Medium |
| `ProviderAttempt` mermaid class diagram and the Consistency Conventions "Provider attempt fingerprint" row gain the `DigestKeyVersion` field | security-data-integrity Medium |
| AD-5's automatic-mode posting paragraph no longer claims "no further automatic retry, only a new Agent Call" - it now states the automatic posting record shares FR-18's bounded retry exactly like a human-decided proposal, per FR-11's own text (which explicitly flagged this AD-5/FR-11 divergence) | rubric Critical |
| AD-14's attestation paragraph gains the FR-34/A-24 trigger list (startup, every readiness evaluation, bounded hourly-default cadence, any host composition change) | rubric High |
| AD-6 no longer calls all seven seams "committed" - only the original six are; seam 7 is named but explicitly not yet `Committed`, matching the Deferred Beyond V1 table instead of contradicting it | rubric High + adversarial High (self-inflicted by pass-1's own H-1 fix; caught by two lenses) |
| AD-13's sequence diagram relabelled and repositioned the `EligibleApprover resolution` step to match the pass-1 text fix (Confirmation mode only, positioned after rate limits and before context read) | adversarial Critical |
| AD-2's literal state-machine graph string gains a `NeverJoined -> Blocked` edge, matching AD-7's/FR-2's "a block may be set from any state" rule | adversarial High |
| New `ARCH-A-13` (AD-8's fourth Approver exclusion has no textual basis in FR-7 - flagged as Architecture-only tightening pending Product confirmation, cited inline in AD-8) and `ARCH-A-14` (the `PostingPending` deadline has no concrete duration fixed anywhere - cited inline in AD-5) | rubric Critical + adversarial Medium (closed by flagging, not silent invention) |
| Frontmatter `sources:` list gains `VALIDATION-REPORT-2026-09-09-4.md`, the round-4 `UPDATE-REPORT` files, and the v8/v9 review families | adversarial Low |

**Checked and not reproduced / already closed by pass 1, confirmed stale reads:** the rubric and adversarial lenses' "AD-22 ProviderAttempt short-lived" findings both matched the same issue the security-data-integrity lens found and pass 2 had already fixed by the time those two agents' reports landed - one fix, three lenses agreeing independently once re-verified against the live text.

## Reviewer gate (v9)

| Lens | Verdict before fixes | Critical / High / Medium / Low | Applied |
| --- | --- | --- | --- |
| Rubric walker | FAIL | 2 / 3 / 1 / 2 | all Critical/High; the two flagged items (H-6-derived AD-8 exclusion, `PostingPending` deadline) closed by flagging via new `ARCH-A` rows rather than silent design |
| Verified-current | FAIL | 1 / 0 / 1 / 1 | Critical + its direct Medium consequence; Low (no newer CVE found) needed no action |
| Adversarial divergence | FAIL | 2 / 1 / 2 / 1 | all Critical/High/Medium; Low (sources list) |
| Brownfield drift | FAILS TO RATIFY, fixable | 0 / 0 / 0 / 1 | no action needed - hygiene-only, every extra working-tree file traces to a documented memlog entry |
| Security / data integrity | PASS WITH FINDINGS | 1 / 0 / 2 / 0 | all |

Deterministic `lint_spine.py`: 12 findings, all literal `TBD` markers in the Architecture Assumptions table's `TargetRetirementDate` column - the same pre-existing tool/reporting discrepancy prior rounds flagged (10 pre-existing rows plus the two new `ARCH-A-13`/`ARCH-A-14` rows, both legitimately open). No duplicate `AD` ids, no missing Binds/Prevents/Rule, no unpinned Stack versions.

## Implementation follow-ups this update creates

1. Product should confirm whether FR-7's Eligible Approver definition should gain the fourth exclusion (regeneration-requesting Approver) this round added to AD-8 ahead of the PRD text (`ARCH-A-13`).
2. Architecture should propose a concrete `PostingPending` attempt-timeout duration (or explicitly defer to the seam's own configured timeout) before the story implementing `PostingPending` timeout/recovery handling (`ARCH-A-14`).
3. `global.json` now pins `10.0.401`, one patch ahead of every sibling repo's `10.0.400` pin - expected and temporary; siblings will catch up on their own schedule, not tracked as a new assumption.
4. Reconcile AD-10's `EntryMissing` carve-out wording against AD-2's unqualified "previously-snapshotted" language before Story 5.3/5.5 finalize (open question carried forward unchanged from round 4, re-flagged again this round, still not applied).
5. `lint_spine.py`'s placeholder scan and this project's validation-report convention still disagree on whether the Architecture Assumptions table's `TBD` cells count as findings; unresolved since round 4.
