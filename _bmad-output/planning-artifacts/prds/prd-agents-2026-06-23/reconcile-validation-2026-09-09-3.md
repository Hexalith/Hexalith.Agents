# Reconciliation — validation-report.md run 2026-09-09T11:01Z (third 2026-09-09 update)

Inputs: `change-extract-validation-2026-09-09-3.md` (124 findings), `reconcile-cross-artifact-2026-09-09-3.md` ("PRD sentences to change", items 1–16), and the product decisions recorded for every entry whose `Decision needed` was not `none`. Edited: `prd.md`, `addendum.md`. Not edited: any register, the Architecture Spine, the epics, or code.

Dispositions: `applied` (PRD text landed as drafted or merged with a co-located edit), `partial` (the PRD part of a MIXED entry landed; the other artifact's part is listed for correct-course), `diverged` (landed with wording that departs from the draft; the reason is in the note), `out-of-scope: <artifact>` (no PRD change; listed for correct-course), `code-side` (no PRD change; listed for correct-course).

## Findings

| ID | Severity | Target | Disposition | Where | Note |
| --- | --- | --- | --- | --- | --- |
| AC-1 | Critical | PRD | applied | FR-28 item 9, §8.1, FR-30, FR-33, §3 | Default inverted: a late `ARCH-A` row blocks unless the Release PM records a `DeferredAssumption` deferral; passed target date escalates to Product; literal-date rule; actor stays Release PM |
| CX-1 | Critical | OTHER-ARTIFACT | out-of-scope: Spine | §8.1 (dated sentence) | AD-13 recorded as the one open Spine divergence pending Architecture correction; FR-8 (now step 6) governs |
| DC-1 | Critical | CODE | code-side | — | Reject `ReportingOnlyMonitoring` / `AcceptedLaunchRisk` at readiness recording; `ProhibitedCostControlPosture` |
| DC-2 | Critical | MIXED | partial | §0 | Status line rewritten (register-keyed, no count, `EXT-HOST-1` acceptance lost on 2026-09-09, mechanism stated by FR-34); blocker, attestation port, and fail-closed paths are code-side |
| DC-3 | Critical | CODE | code-side | — | Segregation of duties in the approval orchestrator |
| DC-4 | Critical | CODE | code-side | — | Edit-time, approval-time, and pre-post re-scans (PRD wording is AM-13) |
| DC-5 | Critical | CODE | code-side | — | Pre-Provider scan before `InvokeProviderAsync` |
| DC-6 | Critical | CODE | code-side | — | Cap, rate-limit, concurrency readiness blockers and gate checks |
| AH-1 | High | PRD | applied | FR-23 | Link/replace commands and `Proposal*` members added to the register; reconciliation bullet added |
| AH-2 | High | PRD | applied | FR-2, OQ-16 | No-mirror-history rule; clear-newer case re-adds idempotently or rejects `MembershipRejected`; negative tests rewritten |
| AH-3 | High | PRD | applied | FR-2, §8 seam 1, §8.1 A-22, OQ-16 | Option (a): presence re-admits under A-22; until retired, only the clear row re-admits |
| AH-4 | High | PRD | applied | FR-18 table and rules, FR-33, OQ-3, §8.1, OQ-26 | `PostingWindowElapsed` bound for `Approved`/`PostingFailed`; human abandon from `Approved`; administrative retry age bound; AD-5 divergence recorded as one dated sentence |
| AH-5 | High | PRD | applied | FR-11, FR-25 | Automatic posts follow the FR-18 posting rows from `Approved` |
| AH-6 | High | PRD | applied | FR-4, FR-24, §9, A-10, OQ-29 | Loosening change blocks immediately; tightening keeps the 30-day grace; version in force = last accepted |
| AH-7 | High | MIXED | partial | FR-34 | Release-Operator-owned committed-engine value via `EXT-SECRETS-1`; test extended; register scope extension for correct-course |
| AH-8 | High | PRD | applied | FR-24, FR-33 row, §8.1 A-23, OQ-30 | Aggregation bound (5 Conversations / 30 days), unreviewed-inspection block and compliance finding, threshold tagged A-23 |
| AH-9 | High | PRD-resync | applied | §0, §8, §8.1 | Register-keyed status, seams re-synced, pointer-only §8.1, closure rule added |
| CX-2 | High | PRD-resync | applied | §0, §8 | No entry `Committed`; `EXT-HOST-1` earlier target historical; register authoritative |
| CX-3 | High | PRD-resync | applied | §8 seams 2 and 4 | Resync items 4 and 5 wording |
| CX-4 | High | PRD-resync | applied | §8.1 | Resync item 7: index-version pointer, no row count |
| CX-5 | High | PRD-resync | applied | §8.1, OQ-26, OQ-27, addendum | Defect list removed; AD-5/AD-12/AD-22 recorded as corrected the same day |
| CX-6 | High | OTHER-ARTIFACT | out-of-scope: Spine, launch-readiness-register, external-dependency-register | — | `DataHandlingVersion` gate in AD-10/AD-2 and Provider Readiness Contract reason codes; must mirror AH-6's loosen/tighten rule |
| Rubric-DR-HOST | High | PRD-resync | applied | §0, §8 | Same edits as DC-2 / CX-2 |
| DH-1 | High | CODE | code-side | — | Configuration rejects `BlockWithAuditableOverride` and `Caller`; UI stops offering `Caller` |
| DH-2 | High | CODE | code-side | — | Obsolete link/replace handlers; Platform-scoped provision creates the Party; deny `CreateAgent` to tenant roles |
| DH-3 | High | CODE | code-side | — | `ProposalDetail.razor` must not treat `PostingFailed` as terminal |
| DH-4 | High | CODE | code-side | — | Retry command with budget, abandon from `PostingFailed`, `MessageId` lookup on exit |
| DH-5 | High | CODE | code-side | — | Gate `Membership` check and `ConversationAgentState` machine |
| DH-6 | High | CODE | code-side | — | Command-time `ExpiresAt` enforcement, 24-hour default |
| DH-7 | High | CODE | code-side | — | Safe Context Budget formula |
| DH-8 | High | CODE | code-side | — | `Indeterminate` outcome mapping and retry budget gating |
| DH-9 | High | CODE | code-side | — | Lifecycle/suspension re-validation on approve/edit/regenerate; kill switch |
| AM-1 | Medium | PRD | applied | FR-18 row, FR-28 | `ExpiredWhileSuspended` = `ExpiresAt` passed while `Suspended`; count reported at launch-health review |
| AM-2 | Medium | PRD | applied | FR-28, FR-33 row, FR-30 | Release conditions tightened; `SuspensionReviewOverdue` |
| AM-3 | Medium | PRD | applied | FR-28, A-17 | Failure-share denominator; review validity 7 days / +10 points under A-17 |
| AM-4 | Medium | PRD | applied | FR-28 | Tenant-lowered rejections attributed tenant-caused; acknowledgement closes a tenant-only condition |
| AM-5 | Medium | PRD | diverged | FR-8, FR-16, FR-32, SM-C4, §8.1 | Reorder and renumbering applied; step 3 rejects `Blocked` locally but defers `ExternallyRemoved` to step 10 so AH-3 (a) re-admission stays possible; NFR-9 sentence covers steps 1–8 (no classifier) rather than 1–6 |
| AM-6 | Medium | PRD | applied | SM-4 | Callable-row scope; exempt rows named, including the two Release PM rows |
| AM-7 | Medium | PRD | diverged | FR-2, FR-25, §3, A-1, OQ-25, SM-C4 | `MirrorRefused` terminal outcome applied; instead of counting it as an unavailable-call signal (which would double-classify calls already rejected as blocked), SM-C4 tracks the count of Conversations carrying it |
| AM-8 | Medium | PRD | applied | FR-24, OQ-30 | Principal-specific subject set; single-Administrator tenant rule |
| AM-9 | Medium | PRD | applied | §9, FR-21, OQ-31 | Keep unprotected; erasure at offboarding; "interaction Audit Evidence"; new OQ-31 |
| AM-10 | Medium | PRD | applied | FR-27, SM-C4 | History-failure evidence; block attributed to the Conversation |
| AM-11 | Medium | MIXED | partial | §8 `EXT-CONV-UI-1` | Option (a): Conversation status entry stated as a required artifact kind; register extension for correct-course |
| AM-12 | Medium | PRD | applied | FR-7, §6.2 | Person-Party-only Approver sources |
| AM-13 | Medium | PRD | applied | FR-15, FR-27, FR-18 row | Edit-time scan with `SafetyFailed` marker; approval guard requires no marker |
| AM-14 | Medium | PRD | applied | FR-7 | Freshness bound = one re-check cadence (A-20) |
| AM-15 | Medium | PRD | applied | FR-34, A-24 | Cadence, composition-change trigger, engine pin |
| CI-1 | Medium | PRD | applied | FR-26, FR-17, FR-18, FR-27, OQ-9 | "Pass every applicable policy version" form everywhere |
| CI-2 | Medium | PRD | applied | FR-23 | Same edit as AH-1 |
| CI-3 | Medium | PRD | applied | FR-18 row | Administrative retry only after budget exhaustion and within the `PostingWindowElapsed` bound |
| CI-4 | Medium | PRD | applied | FR-18 row | Same edit as AM-1 |
| CI-5 | Medium | PRD | applied | FR-28, OQ-6 | Single settlement instant; `Unreconciled` on hold expiry |
| CI-6 | Medium | PRD | applied | FR-7 | Two-pass abandonment scoped to awaiting proposals; `PostingFailed` marker only |
| CI-7 | Medium | PRD | applied | FR-9 | `ContextUnavailable` defined as a typed context-read failure |
| CI-8 | Medium | PRD | applied | FR-3, FR-18, §8.1 | `Disabled`-Agent retry-clock pause; Product confirmation retiring `ARCH-A-11` |
| CI-9 | Medium | PRD | applied | §3, FR-33 | Same edits as Rubric-DR-PM |
| CI-10 | Medium | PRD | applied | FR-18 | No `MessageId` lookup from `Approved` (rubric form) |
| CX-7 | Medium | MIXED | partial | §8 | Resync item 2 wording: open Product approval blocks `ready-for-dev`; Branch A/B is a register decision |
| CX-8 | Medium | OTHER-ARTIFACT | out-of-scope: Spine | — | AD-2 first paragraph `ConversationAgentState` clause |
| CX-9 | Medium | OTHER-ARTIFACT | out-of-scope: external-dependency-register | — | Seam-1 typed "already absent" no-op |
| CX-10 | Medium | OTHER-ARTIFACT | out-of-scope: epics | — | Inventory rows FR4, FR5, FR12, FR22, FR25, FR27 |
| CX-11 | Medium | OTHER-ARTIFACT | out-of-scope: epics | — | `Draft`, `MembershipUnavailable`, `MembershipRejected` |
| CX-12 | Medium | OTHER-ARTIFACT | out-of-scope: epics | — | `DataHandlingVersion` block, acceptance, loosen/tighten grace in Stories 5.3, 5.7, 6.4 |
| Rubric-DR-PM | Medium | PRD | diverged | §3, FR-33 | Release PM kept as a distinct party (not equated with the Release Operator); glossary entry and two FR-33 rows added; FR-28 uses not renamed |
| Rubric-DN-EWS | Medium | PRD | applied | FR-18 row | Same edit as AM-1 |
| Rubric-DN-restrictive | Medium | PRD | applied | FR-26 | Same edit as CI-1 |
| Rubric-DN-FR31 | Medium | PRD | applied | FR-31, OQ-9 | First-person impersonation and unrecorded decisions only |
| Rubric-DN-AgentCall | Medium | PRD | applied | FR-8, FR-23 | `AgentInteractionStatus` contract with terminal members; `Proposal*` duplicates on the register |
| Rubric-DN-p99 | Medium | PRD | applied | NFR-9, NFR-14, FR-28, OQ-5 | p95 at n ≥ 30, nearest-rank; p99 gates at n ≥ 300 |
| Rubric-DU-8.1 | Medium | PRD-resync | applied | §8.1 | Pointer-only plus one dated AD-13 sentence (and one dated AD-5 amendment sentence from AH-4) |
| DM-1 | Medium | CODE | code-side | — | Baseline additive members |
| DM-2 | Medium | CODE | code-side | — | Second-update vocabulary, FR-7 re-check job, FR-30 fields |
| DM-3 | Medium | MIXED | partial | FR-9 | PRD defines `ContextUnavailable` as distinct from authorization denials; code split for correct-course; transitional line not added |
| DM-4 | Medium | CODE | code-side | — | Data-handling record and tenant acceptance |
| DM-5 | Medium | CODE | code-side | — | Approver Policy structural rule and Conversation-scoped resolution |
| DM-6 | Medium | CODE | code-side | — | Regeneration ceiling |
| DM-7 | Medium | MIXED | applied | FR-4 | Server-assigned pricing version; caller-supplied version never stored |
| DM-8 | Medium | CODE | code-side | — | FR-25 read-model fields |
| AL-1 | Low | PRD | applied | A-9 | Same edit as CI-11 |
| AL-2 | Low | PRD | applied | FR-28, A-17 | 10 calls per Party, bounded 20–50 |
| AL-3 | Low | PRD | applied | FR-28 | Single-cohort posting-failure rate |
| AL-4 | Low | PRD | applied | FR-28, FR-33 row | Business day defined in UTC |
| AL-5 | Low | PRD | applied | FR-32, A-25 | Override bound 25% / 7 days; second override needs Product |
| AL-6 | Low | PRD | applied | FR-7 | Same edit as CI-6 |
| AL-7 | Low | PRD | applied | FR-16 | Regeneration re-runs FR-8 steps 2 through 10 |
| AL-8 | Low | PRD | applied | FR-2 | `NeverJoined` block confirmed without a seam call only when absent |
| AL-9 | Low | PRD | applied | FR-27 | Bounded background re-scan on publication and rotation |
| AL-10 | Low | PRD | applied | FR-7 | Disclosure-safe remediation hint on `NoEligibleApprover` |
| AL-11 | Low | PRD | applied | FR-2 | Same edit as AH-2 |
| AL-12 | Low | PRD | applied | UJ-1, SM-3, FR-33 row, FR-26 | Three drafted carried lows (gate L-2, L-6, L-7); L-1/L-4/L-5/L-9/L-11 keep their earlier fixes |
| AL-13 | Low | PRD | applied | FR-2 | Agents-side AI-type read when the register assigns A-21 to Agents |
| CI-11 | Low | PRD | applied | A-9, OQ-16 | `ExternallyRemoved` clear authority in the row |
| CI-12 | Low | PRD | applied | §6.1 | "provisioning or linking" removed |
| CI-13 | Low | PRD | applied | FR-12 | `ExternallyRemoved` named |
| CI-14 | Low | PRD | applied | FR-22 | Failed-call is an outcome, not a proposal state |
| CI-15 | Low | PRD | applied | OQ-22 | Resync item 14 wording: every unretired row, whatever its owner |
| CI-16 | Low | PRD | applied | FR-28 | Four register-vocabulary conditions mirrored on FR-30 |
| CI-17 | Low | PRD | applied | FR-33 row, §3, §10 | Same edits as Rubric-DN-calling |
| CI-18 | Low | PRD | applied | §3 | Agent Administrator glossary lists lowering caps and limits |
| CX-13 | Low | OTHER-ARTIFACT | out-of-scope: Spine | — | AD-17 "whatever its owner" |
| CX-14 | Low | OTHER-ARTIFACT | out-of-scope: launch-readiness-register | — | `PayloadProtectionUnavailable` vocabulary row wording |
| CX-15 | Low | OTHER-ARTIFACT | out-of-scope: epics | — | `AIAgent` spelling and projection ids |
| CX-16 | Low | OTHER-ARTIFACT | out-of-scope: Spine | — | AD-14 attestation summary |
| CX-17 | Low | OTHER-ARTIFACT | out-of-scope: sprint change proposal | — | 29 vs 33 story count |
| CX-18 | Low | OTHER-ARTIFACT | out-of-scope: launch-readiness-register | — | `LR-PARTY-IDENTITY` wording |
| CX-19 | Low | OTHER-ARTIFACT | out-of-scope: Spine | — | Literal target retirement dates (PRD side is AC-1's date rule) |
| Rubric-DN-double | Low | PRD | applied | FR-28 | Same edit as AL-3 |
| Rubric-DN-lookup | Low | PRD | applied | FR-18 | Same edit as CI-10 |
| Rubric-DN-SMC4 | Low | PRD | applied | SM-C4 | Lapsed acceptance as a blocked reason; steps 2 and 4 reported separately; exclusion aligned to FR-28 |
| Rubric-DN-adjectives | Low | PRD | applied | FR-4, FR-20, FR-25 | Three adjective-only sentences replaced |
| Rubric-DN-calling | Low | PRD | applied | FR-33 row, §10, §3 | Calling restriction given a configuration home |
| Rubric-SH-undated | Low | PRD | applied | §8.1 | A-9..A-12 due 2026-09-30 under new row A-26; register-conditioned rows cite `TargetIntegrationDate` |
| Rubric-SH-seams | Low | PRD-resync | applied | §8 | Same edit as CX-3 |
| Rubric-DU-count | Low | PRD-resync | applied | §8.1 | Same edit as CX-4 |
| Rubric-DU-FR21 | Low | PRD | applied | FR-21, §8, OQ-17 | Gate rules moved to §8 (with the changed-artifact rule); FR-21 keeps a one-line pointer |
| DL-1 | Low | PRD | applied | A-1, §8 seam 1 | `ParticipantType.AiAgent` compiled, not assumed; removal authorization added |
| DL-2 | Low | CODE | code-side | — | `AgentSetupWriteStatus.Submitted = 0` |
| DL-3 | Low | CODE | code-side | — | FR-26 policy-version retry path |
| MN-1 | Low (MN) | PRD-resync | applied | §8.1 | Same edits as CX-4, CX-5, Rubric-DU-8.1, AH-9 |
| MN-2 | Low (MN) | PRD | applied | FR-2, FR-30, §10, SM-C4 | Typed-name consistency |
| MN-3 | Low (MN) | PRD | applied | FR-2 | Same edit as AH-2 |
| MN-4 | Low (MN) | PRD | applied | A-9, OQ-16 | Same edits as CI-11 |
| MN-5 | Low (MN) | PRD-resync | applied | §0, §8 | Same edits as DC-2 / CX-2 |
| MN-6 | Low (MN) | PRD | applied | §2, §3, FR-8, FR-19, FR-20, FR-33, §6.2, SM-2, OQ-11 | Glossary drift; Release PM entry |

## For correct-course

### Architecture Spine

- AD-13 (line 201) and the sequence diagram: limit Eligible Approver resolution and `NoEligibleApprover` to Confirmation Response Mode (CX-1); §8.1 carries the dated divergence sentence until the Spine ledger records the correction.
- AD-13 step citations: FR-8 is now a ten-step list (AM-5) — Conversation Agent State is step 3, rate limits and the concurrent bound step 5, Eligible Approver resolution step 6, context measurement step 7, reservation step 8, safety scan step 9, membership step 10.
- AD-5: PRD-originated amendment — human abandon from `Approved` (Eligible Approver or Tenant Agent Administrator) and the `PostingWindowElapsed` system abandonment from `Approved` and `PostingFailed`; administrative retry age bound; no `MessageId` lookup from `Approved` (AH-4, CI-10).
- `ARCH-A-11` retirement: FR-3 and FR-18 now state the `Disabled`-Agent retry-clock pause on the kill-switch terms (CI-8); record the Product confirmation and retire the row.
- AD-20 `LastLoosensVersion` ordering: the PRD now states the computable "passes every applicable policy version" form (CI-1); confirm the ordering implements exactly that.
- AD-10 / AD-2: `TenantProviderEnablement` holds the accepted `DataHandlingVersion`; mirror the loosen-blocks / tighten-grace rule (CX-6, AH-6).
- AD-2 first paragraph: replace the `ConversationAgentState` clause with the line-127 wording (CX-8); add `MirrorRefused` (AM-7) and the A-22-gated re-admission (AH-3).
- AD-17: "whatever its owner" (CX-13). AD-14: attestation summary includes DEK destroy and `Erased` replay (CX-16). AD-21 / `ARCH-A-6`: single settlement instant, `Unreconciled` on hold expiry (CI-5).
- ARCH-A table: literal target retirement dates, not milestones (CX-19); late-row `DeferredAssumption` rule and Product escalation on a passed date (AC-1).

### external-dependency-register

- `EXT-SECRETS-1` scope: add the Release-Operator-owned committed-engine identity and version value (AH-7).
- `EXT-CONV-UI-1`: fourth artifact kind — the Conversation-level pending-proposal status entry for authorized Approvers (AM-11).
- Seam 1: typed "already absent" confirmed no-op answer (CX-9); removal authorization for the Agents Service Principal and the removal's typed answers under A-1 (AM-7, DL-1); the A-22 Facilitator-only add restriction (AH-3).
- Provider Readiness Contract: `DataHandlingUnrecorded` / `DataHandlingUnaccepted` reason codes and the loosen/tighten rule (CX-6).
- Story 5.3 / `EXT-PROVIDER-1`: Product selects Branch A or B; the PRD states only that the open approval blocks `ready-for-dev` (CX-7).
- Changed-artifact rule: the PRD now states that a change to a required artifact after acceptance returns the entry to `Uncommitted` (DC-2, Rubric-DU-FR21); confirm the register states it.

### launch-readiness-register

- Vocabulary: `DeferredAssumption` and `SuspensionReviewOverdue` conditions (AC-1, AM-2); `PayloadProtectionUnavailable` row in attestation terms (CX-14); `LR-PARTY-IDENTITY` "Party identity or provisioning changes" (CX-18).
- Release PM rows: the two `RQ-1` powers (dependency exclusion, late-row deferral) are recorded here (Rubric-DR-PM, CI-9); the committed engine identity and version are recorded here by the Release Operator (AH-7).
- Provider Readiness Contract reason codes (CX-6).

### epics

- Inventory rows FR4, FR5, FR12, FR22, FR25, FR27 rewritten from the current FR text (CX-10); `Draft`, `MembershipUnavailable`, `MembershipRejected` named (CX-11); `DataHandlingVersion` block, acceptance, and loosen/tighten grace in Stories 5.3, 5.7, 6.4 (CX-12); `AiAgent` spelling and projection ids (CX-15).
- New PRD vocabulary to carry: `PostingWindowElapsed`, `MirrorRefused`, `SafetyFailed` marker, `ContextUnavailable` as a typed FR-9 reason, `DeferredAssumption`, `SuspensionReviewOverdue`, the FR-8 ten-step order, A-22 through A-26, OQ-31.

### sprint change proposal

- 29-story versus 33-story count (CX-17).
- `prd.md` must be listed under `amends_if_approved` for this update's PRD edits to be scheduled; the current proposal preserves `prd.md` unchanged (resync item 16).

### code

- DC-1, DC-3, DC-4, DC-5, DC-6 (critical); DH-1 through DH-9 (high); DM-1, DM-2, DM-4, DM-5, DM-6, DM-8 (medium); DL-2, DL-3 (low) as listed in the extract.
- DM-3: split `ContextUnavailable` from authorization denials; the PRD now defines it as a context-read failure (FR-9).
- DC-2 code part: `PayloadProtectionUnavailable` blocker, host-supplied attestation port, typed outcome on every FR-21 content-bearing path; plus the Release-Operator-owned engine value comparison (AH-7) and the pinned-engine cadence (AM-15).
- DM-7 resolved on the PRD branch (server-assigned pricing version); code aligns to it.

## Applied counts

| Severity | Total | applied | partial | diverged | out-of-scope | code-side |
| --- | --- | --- | --- | --- | --- | --- |
| Critical | 8 | 1 | 1 | 0 | 1 | 5 |
| High | 24 | 13 | 1 | 0 | 1 | 9 |
| Medium | 46 | 29 | 3 | 3 | 5 | 6 |
| Low | 40 | 31 | 0 | 0 | 7 | 2 |
| Low (MN) | 6 | 6 | 0 | 0 | 0 | 0 |
| **Total** | **124** | **80** | **5** | **3** | **14** | **22** |

Resync items: 1–7 and 9–15 applied (item 8 replaced by the AC-1 wording; items 10 and 11 applied as their optional clarifications; item 15 applied to `addendum.md`). Item 16 kept as constraints (no story count, no `EXT-HOST-1` historical target cited).

New identifiers: OQ-31; A-22 (Facilitator-only AI add restriction), A-23 (inspection aggregation bound and rate threshold), A-24 (attestation cadence), A-25 (cost-cap override bound), A-26 (2026-09-30 due date for the A-9..A-12 confirmations). New typed names: `DeferredAssumption`, `PostingWindowElapsed`, `SuspensionReviewOverdue`, `MirrorRefused`, `SafetyFailed`, `ContextUnavailable` (FR-9).

`prd.md` line count: 1000 before, 1016 after. `addendum.md`: 52 before, 83 after. Frontmatter `status: final` and `updated: 2026-09-09` unchanged. Consistency sweep: every `A-n` tag has a row and every row is cited; every OQ, FR, SM, NFR, and UJ reference resolves; FR-8 step citations (FR-16, FR-32, SM-C4, §8.1) match the renumbered list; no leftover "pending register extension", "three are open", "ARCH-A-1 through ARCH-A-10", "more restrictive of", or `Committed` claim for `EXT-HOST-1`.

## Reviewer gate (third update)

Inputs: `review-adversarial-general.md` (C-1, H-1..H-8, M-1..M-14, L-1..L-10), `review-rubric.md` (R-H-1, R-M-1..6, R-L-1..6), `review-consistency.md` (CI-1..CI-10, CX-1..CX-18), `review-implementation-drift.md` (D-H-1..3, D-M-1..6, D-L-1..4), all written against `prd.md` at 1016 lines. Edited: `prd.md`, `addendum.md`. Not edited: any register, the Architecture Spine, the epics, or code. Dispositions as in the table above; `deferred (Product)` means a reviewer finding left unapplied with owner Product, revisit at the next validate run.

| ID | Severity | Disposition | Where / note |
| --- | --- | --- | --- |
| C-1 | Critical | diverged | FR-2 membership step, clearing rule, OQ-16, OQ-25, §8.1 AD-7 entry. A clear issues its own seam-1 add as a mirror entry; the late-mirror branch ends when that mirror is confirmed; negative tests kept and extended. Reviewer's "post-clear read confirmed present" condition rejected (addendum) |
| H-1 | High | diverged | FR-28 item 9, §8.1, FR-33 row, §3 Release PM, §0, OQ-22, OQ-24. Product acceptance, 30-calendar-day revisit, recorded scheduling event, Security rows never deferrable; reviewer's first-launch-health-review bound rejected |
| H-2 | High | diverged | FR-2 `Joined`-and-absent branch, §8 seam 1, A-1, A-15. Roster version freshness instead of a two-read `RemovalPending` (addendum) |
| H-3 | High | applied | FR-18 `PostingPending` prose and the two `PostingPending` rows: stored deadline evaluated on read, command, and recovery; lookup then `LateConfirmed` or `PostingFailed` |
| H-4 | High | diverged | FR-11, FR-8, FR-33 new row, §3, §8.1 AD-5 entry. System-approved posting record; call terminal on record termination; Facilitator or Administrator human exit; reviewer's additive `Abandoned` member rejected (addendum) |
| H-5 | High | diverged | FR-4, FR-5, FR-33 new declare row, A-10, OQ-29. Platform-Operator-declared tightening with recorded diff, fail-closed default; computed direction rejected (addendum) |
| H-6 | High | diverged | FR-34 ownership, attestation, test; A-24; FR-33 new row; OQ-24. Security-qualified signed identity via `EXT-SECRETS-1`, canary as liveness, wrapper case a qualification test |
| H-7 | High | applied | FR-8 steps 1, 2, 4, 9 (no renumbering); `TenantSuspended` classified in FR-25 and SM-C4 |
| H-8 | High | diverged | FR-28 trigger review, A-17, SM-C4, OQ-27, FR-3. Distinct-Party numerator, 3-Party floor, `TriggerReviewOverdue` blocks enablement and activation; Platform Operator escalation rejected (addendum) |
| M-1 | Medium | applied | FR-2: the unconfirmed-mirror sub-branch is now the re-admission branch with its outcomes (re-add and accept, or `MembershipRejected`) |
| M-2 | Medium | deferred (Product) | FR-18 `PostingWindowElapsed` paused-time and 72-hour bound |
| M-3 | Medium | deferred (Product) | FR-24 Inspector-in-subject-set, per-tenant bound, chain reciprocity, threshold value |
| M-4 | Medium | deferred (Product) | Release PM versus Release Operator accountability; planning-party identity on register entries |
| M-5 | Medium | applied | SM-C4 and FR-25: lapsed acceptance is its own class (not blocked, per R-M-3 decision); `MembershipRejected` at acceptance reported separately (CI-9); per-Party concurrent bound named under rate limits |
| M-6 | Medium | applied | FR-27, FR-25, FR-28: `ContextReadUnavailable` with sub-reason `RescanPending`, closed by Platform Operator acknowledgement; on-demand scan within the fast gate not adopted |
| M-7 | Medium | applied | FR-3 (CI-1 wording), §8.1 AD-7 and AD-5 entries |
| M-8 | Medium | deferred (Product) | FR-31 grounding check as an `EXT-SAFETY-1` capability with fixtures |
| M-9 | Medium | applied | FR-1 atomic provisioning; `EXT-PARTIES-1` and A-27 |
| M-10 | Medium | deferred (Product) | NFR-9 sample and cohort rules |
| M-11 | Medium | deferred (Product) | Scanning Agent Instructions; protection of instruction values |
| M-12 | Medium | deferred (Product) | Decision channel for SM-3/SM-7; human-edited approved version |
| M-13 | Medium | deferred (Product) | Snapshot policy composition; OQ-9 fifth application point |
| M-14 | Medium | deferred (Product) | Non-excludable dependencies in FR-28 item 6 |
| L-1 | Low | applied | A-26: escalation to Product at the next `RQ-1` evaluation or weekly readiness review |
| L-2 | Low | applied | FR-2 mirroring and clearing: Administrator action on `MirrorRefused`; a clear starts the re-admission mirror |
| L-3 | Low | deferred (Product) | FR-32 which cap; third override |
| L-4 | Low | deferred (Product) | Conversation Context scoped to the caller's visibility |
| L-5 | Low | applied | FR-8 step 1: a non-person caller Party is a typed denial |
| L-6 | Low | deferred (Product) | Regeneration charged to the requesting Approver |
| L-7 | Low | deferred (Product) | "Restricted tenant role" definition |
| L-8 | Low | deferred (Product) | Per-Conversation limit attribution |
| L-9 | Low | deferred (Product) | Carried lows (reused-verdict record, approval-to-post classifier budget, NFR-13 "required context") |
| L-10 | Low | deferred (Product) | Pricing version above the stored one |
| R-H-1 | High | applied | §8 seam 7, A-28, OQ-23 dated 2026-09-30 (A-26) with three options; Automatic mode stays in V1 |
| R-M-1 | Medium | applied | §8.1 preamble rule; A-5, A-6, A-13, A-17, A-19, A-23, A-25 dated by A-26; A-7, A-8, A-20, A-24 carry the calendar-date rule; §12 and OQ-11 dating aligned |
| R-M-2 | Medium | applied | FR-11 and FR-33: automatic posting record abandon without accessibility condition; `PostingWindowElapsed` against the Agent's expiry duration at acceptance |
| R-M-3 | Medium | applied | FR-5, FR-25, SM-C4, FR-28: `DataHandlingAcceptanceLapsed` as its own class, excluded from the trigger shares |
| R-M-4 | Medium | deferred (Product) | A-23 inspection-rate threshold value |
| R-M-5 | Medium | deferred (Product) | Sub-identifiers for FR-2, FR-7, FR-18, FR-24, FR-28 |
| R-M-6 | Medium | deferred (Product) | UJ-5 Compliance Inspector journey |
| R-L-1 | Low | deferred (Product) | NFR-4 wording |
| R-L-2 | Low | applied | FR-8: steps 9 and 10 outside the NFR-9 fast gate (same edit as CI-3) |
| R-L-3 | Low | applied | FR-8: rejection-to-status mapping sentence |
| R-L-4 | Low | applied | FR-3: expiry while `Disabled` is an ordinary `Expired` |
| R-L-5 | Low | deferred (Product) | `OverrideApprovalRequired` on the FR-33 override row |
| R-L-6 | Low | applied | §8.1 list (AD-13 order, AD-21 step); OQ-26 "on the same terms except" |
| CI-1 | Medium | applied | FR-3 and §8.1: text on which Architecture may retire `ARCH-A-11` once Product's confirmation is recorded |
| CI-2 | Medium | applied | SM-C4, FR-5, FR-25 (R-M-3 decision) |
| CI-3 | Medium | applied | FR-8: steps 9 and 10 excluded from the fast gate |
| CI-4 | Medium | applied | FR-28: SM-4 pull followed by a recorded containment review within two business days; `SuspensionReviewOverdue` is its absence |
| CI-5 | Low | applied | OQ-17 status cell |
| CI-6 | Low | applied | A-23 "Where" = FR-24 |
| CI-7 | Low | applied | FR-11: no Eligible Approver in Automatic mode; Facilitator or Administrator exit |
| CI-8 | Low | applied | SM-4 exempt list: `RQ-1` recording within the readiness row (and the engine-identity record row) |
| CI-9 | Low | applied | SM-C4 and FR-25: `MembershipRejected` at acceptance reported separately |
| CI-10 | Low | applied | addendum: "every use" |
| CX-1 | High | applied (PRD side) | FR-28: `SuspensionReviewOverdue` and `DeferredAssumption` dated as PRD-declared, register extension pending; register and Spine AD-17 side for correct-course |
| CX-2 | High | partial | §8.1 list carries AD-13 order and AD-21 "step 4"; Spine side for correct-course |
| CX-3 | High | applied | §8.1 dated divergence list by AD number; "reconciliation ledger" pointer and "one divergence" removed; OQ-26 aligned |
| CX-4 | High | out-of-scope: Spine, launch-readiness-register | `DataHandlingVersion` gate; now also the declared-tightening rule and `DataHandlingAcceptanceLapsed` |
| CX-5 | Medium | out-of-scope: external-dependency-register | Seam 1: A-22 admission restriction, typed already-absent, and now the roster version |
| CX-6 | Medium | partial | OQ-26 and §8.1 AD-5 entry on the PRD side; Spine AD-5 for correct-course |
| CX-7 | Medium | out-of-scope: Spine | AD-12 small-tenant sample (listed in §8.1) |
| CX-8 | Medium | out-of-scope: Spine | AD-22 escalation and aggregation (listed in §8.1) |
| CX-9 | Medium | out-of-scope: Spine, external-dependency-register | AD-14 and `EXT-SECRETS-1` artifact; now the Security-qualified signed identity (listed in §8.1) |
| CX-10 | Medium | out-of-scope: external-dependency-register | `EXT-CONV-UI-1` fourth artifact kind; PRD hedge already defers to the register text |
| CX-11 | Medium | out-of-scope: Spine | AD-7 and AD-2 (listed in §8.1) |
| CX-12 | Medium | out-of-scope: Spine, epics | Third-update and reviewer-gate vocabulary |
| CX-13 | Low | out-of-scope: Spine, epics | `Draft` lifecycle state |
| CX-14 | Low | out-of-scope: Spine | Frontmatter `OQ-1..OQ-31` |
| CX-15 | Low | out-of-scope: Spine | AD-21 settlement instants |
| CX-16 | Low | out-of-scope: epics | p99 gate at 300 |
| CX-17 | Low | out-of-scope: launch-readiness-register | `PayloadProtectionUnavailable` wording; `LR-PARTY-IDENTITY` |
| CX-18 | Low | deferred (Product) | Optional "as read on" dating of quoted frontmatter dates (applied in OQ-26 only) |
| D-H-1 | High | applied | FR-8 and FR-23: nine state-duplicating members named as no longer emitted; `*Failed` outcomes kept; no member count; §10 note |
| D-H-2 | High | applied | `EXT-PARTIES-1` (§8), A-27, FR-1, FR-2, FR-23, A-21, seam 1, OQ-25, OQ-28, glossary; "AI-type Party" phrase gone |
| D-H-3 | High | diverged | FR-9, FR-25, SM-C4, FR-8, FR-27: `ContextUnavailable` keeps its coarse meaning and is counted blocked; additive `ContextReadUnavailable` is the unavailable class |
| D-M-1 | Medium | applied | FR-11 posting record (option b) |
| D-M-2 | Medium | applied | FR-15, FR-18 row: `RestrictedContent` version-record field |
| D-M-3 | Medium | code-side | Third-update vocabulary, now also `ContextReadUnavailable`, `RescanPending`, `RestrictedContent`, `TenantSuspended`, `DataHandlingAcceptanceLapsed`, the posting record, the `PostingPending` deadline, the roster version |
| D-M-4 | Medium | code-side | Person-only Approver rule |
| D-M-5 | Medium | code-side | Edit-time scan and `RestrictedContent` field |
| D-M-6 | Medium | code-side | FR-8 gate checks; the `Suspended`, `LaunchReadiness`, and policy-presence checks now have steps |
| D-L-1 | Low | applied | FR-23: four zero-valued enums; successor enum |
| D-L-2 | Low | applied | FR-8 steps 1, 2, 4, 9 |
| D-L-3 | Low | code-side | `EXT-SECRETS-1` custody value and HMAC secret; now the Security-qualified identity |
| D-L-4 | Low | code-side | p95 measurement shape |

Counts by reviewer — adversarial (33): applied 8, diverged 7, deferred (Product) 18; rubric (13): applied 8, deferred (Product) 5; consistency (28): applied 12 (CI-1..CI-10, CX-1, CX-3), partial 2 (CX-2, CX-6), out-of-scope 13, deferred 1 (CX-18); drift (13): applied 6, diverged 1, code-side 6.

### Correct-course additions from this gate

- **external-dependency-register:** new entry `EXT-PARTIES-1` (AI Party type in Hexalith.Parties; authorization for the Agents Service Principal to create `hexa`'s Party; A-27); `EXT-CONV-AI-1` seam 7 (message retraction, deletion, or flag signal for `AiAgent` posts; A-28); seam 1 participant-state read returns a roster version (A-1, A-15); `EXT-SECRETS-1` artifact custodies the Security-qualified signed engine build identity and version (replacing the Release-Operator-owned committed target value).
- **launch-readiness-register:** vocabulary and emitter rows for `DeferredAssumption` and `SuspensionReviewOverdue` (PRD-declared 2026-09-09, dated in FR-28); record kinds for the `RQ-1` evaluation scheduling event, the Product acceptance of a deferral, the two-business-day SM-4 containment review, the Security engine-build qualification, and the Platform Operator's tightening declaration with field-level diff; `TriggerReviewOverdue` as a block on production-like enablement and activation.
- **Architecture Spine:** the §8.1 divergence list — AD-5 (`MessageId` lookup from `Approved`; automatic-mode retry; human abandon from `Approved`; `PostingWindowElapsed`; `PostingPending` stored deadline), AD-7 (A-22 gate; re-admission mirror on clear; roster-freshness rule), AD-12 (small-tenant sample; distinct-Party share; 3-Party floor), AD-13 (Confirmation-mode-only Eligible Approver resolution; step sequence; no Eligible Approver in Automatic mode), AD-21 ("step 4" is now step 5), AD-14 (cadence, pin, Security-qualified signed identity), AD-17 (vocabulary; owners), AD-22 (escalation; aggregation bound); plus the posting record for automatic posts, `ContextReadUnavailable` and `RescanPending`, `RestrictedContent`, `TenantSuspended`, `DataHandlingAcceptanceLapsed` and the declared-tightening rule, the folded FR-8 checks, the FR-23 nine-member no-longer-emitted set and the `*Failed` outcomes kept, and the four zero-valued enums corrected by successor enums.
- **epics:** carry the same vocabulary; Story 6.6 (re-admission mirror on clear, roster version), 7.x (posting record, `PostingPending` deadline), 5.3/5.7 (declared tightening, `DataHandlingAcceptanceLapsed`), 8.x (`TriggerReviewOverdue` block, `DeferredAssumption` acceptance), and a provisioning story that re-homes the link command's provisioning leg before link is rejected.
- **code:** as D-M-3..D-M-6 and D-L-3..D-L-4 above, plus the posting-record shape for automatic calls, the `PostingPending` deadline, the successor enums, and the signed-identity verification in the FR-34 attestation.

`prd.md` line count: 1016 before, 1035 after. `addendum.md`: 83 before, 102 after. Frontmatter unchanged. Consistency sweep: A-1..A-28 roundtrip (every tag has a row, every row is tagged or cited); every FR, OQ, NFR, SM, SM-C, UJ, and A reference resolves; FR-8 step citations (FR-16 "2 through 10", FR-32 "step 5", SM-C4 "steps 2 and 4", FR-5 "step 4", §8.1 AD-13/AD-21 entries) match the unrenumbered list; FR-18 `PostingPending` rows match the deadline prose; no "AI-type Party", "exactly one open divergence", "One divergence is open", "reconciliation ledger", "six seams", "three classes", or tuple-counting leftovers; `SafetyFailed` appears only as the `AgentInteractionStatus` member; `ContextUnavailable` appears only with its coarse meaning.
