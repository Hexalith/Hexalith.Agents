# Reconciliation — Validation Change Extract 2026-09-08 vs Updated PRD (2026-09-09)

- **Change extract:** `change-extract-validation-2026-09-08.md` (C1–C6, H1–H15, cross-finding conflicts 1–10, plus the 17 co-located M/L IDs the update run committed to apply)
- **PRD reviewed:** `prd.md` (frontmatter `status: final`, `updated: 2026-09-08`; body cites the "2026-09-09 reconciliation" throughout)
- **Register reviewed:** `external-dependency-register.md`, records `EXT-CONV-AI-1` and `EXT-CONV-UI-1` only
- **Method:** each finding's recommended fix was compared against the PRD text; line numbers below are `prd.md` line numbers at the time of this reconciliation. Status vocabulary: FULLY / PARTIALLY / NOT ADDRESSED / DELIBERATELY DIVERGED.

## Summary

| Status | C/H (21) | M/L (17) | Total (38) |
| --- | --- | --- | --- |
| FULLY | 17 | 14 | 31 |
| PARTIALLY | 0 | 3 | 3 |
| NOT ADDRESSED | 0 | 0 | 0 |
| DELIBERATELY DIVERGED | 4 | 0 | 4 |

Partial: M-A8, L-A2, L-D1. Diverged: H2, H13, H14, H15 (the last only on the name choice; substance is met). No C/H finding is unaddressed.

Mechanical checks: no `PostFailed`, no "platform Agents service principal", and no typed `Unknown` outcome survive. Assumption round-trip: every substantive `[ASSUMPTION]` tag maps to an §8.1 row; one index row (A-14) has no tag anywhere, and six rows cite "Where" locations that carry no tag (details in §3).

---

## 1. Critical findings

### C1 — SM-3 known-unattainable under permitted expiry range — **FULLY**

- **PRD evidence:** §12 SM-3 (line 791): "At least 80% of Proposed Agent Replies created during a rolling 30-day window receive a human Approver decision — a transition from `Pending`, `Edited`, or `Regenerated` into `Approved`, `Rejected`, or `Abandoned` — within 24 hours of creation, measured on proposals whose configured expiry is at least 24 hours `[ASSUMPTION: …]`. `Expired` is never a decision." Expiry rate moved to SM-C5 (line 801). SM-3 is a launch-health metric, not an `RQ-1` input (lines 109, 586, 780). OQ-11 amended (line 821); OQ-19 Resolved 2026-09-09 (line 829); provisional status carried by A-13 (line 718).
- **Choice made:** reviewer option (a) (human-decision latency on proposals with expiry ≥ N) combined with C3's removal from the gate. The threshold itself was retuned from 95%/26h to 80%/24h and flagged as an assumption rather than "marked provisional in §12" in prose; A-13 and the §12 preamble (line 780) do the same job.
- **Uses C5 state names:** yes (`Pending`/`Edited`/`Regenerated`, `Approved`/`PostingPending`/`PostingFailed`).

### C2 — FR-7 segregation/access rules make Confirmation Mode unconfigurable — **FULLY**

- **PRD evidence:** FR-7 (line 225): "Segregation of duties is enforced at three moments. At configuration time, the system rejects a policy whose sources can never resolve two distinct Parties … At call time, the system resolves the Approver set for the proposal before any Provider work and rejects the call with the typed reason `NoEligibleApprover` when that set, excluding the caller, is empty … At edit time, an edit that would leave no Approver other than the editor is rejected with a typed reason before the edit is recorded". Line 226: "no proposal can exist that nobody may approve." "Bare tenant role" is deleted (grep: 0 hits); replaced by the Conversation-scoped resolution rule (line 223): a tenant-role or predefined-Party source contributes only current Participants, and an empty contribution "is a resolution outcome, not a configuration error" — this also settles that predefined Parties are permitted. UJ-3 rewritten (line 64) to show the edit-refusal path.
- **Residual (not a gap against the recommendation):** whether a Facilitator-only policy passes the configuration-time test depends on whether `ParticipantRole.Facilitator` can have more than one holder per Conversation; the PRD does not say, but the call-time `NoEligibleApprover` rejection makes this safe either way.

### C3 — Adoption metrics gate enablement but need enablement first — **FULLY**

- **PRD evidence:** FR-28 (line 585): `RQ-1` inputs are "SM-1, SM-4, SM-5, SM-6, the NFR-9 and NFR-14 latency gates, and 100% Audit Evidence completeness on the qualification cohort"; line 586: "SM-2, SM-3, and SM-7 are launch-health metrics … not `RQ-1` inputs; they are reviewed at 30 and 60 days after enablement". §12 split into the two groups (lines 780–793). SM-1 (line 784) no longer says "satisfies launch-readiness gates" (grep: 0 hits). Glossary `RQ-1` (line 109) and §11 (line 776) restated; OQ-22 (line 832) records the decision. FR-28 last consequence (line 594) now distinguishes gate-metric `InsufficientEvidence` (NOT READY) from launch-health `InsufficientEvidence` (reported, never silently passes).

### C4 — FR-24 audit access rule defeats Compliance Operator — **FULLY**

- **PRD evidence:** FR-24 (line 503): "requires either current read access to that Source Conversation or the compliance-inspection authorization in FR-33 … Compliance inspection is tenant-scoped, requires a recorded justification, and is itself recorded as Audit Evidence." Line 504: "When the Source Conversation no longer exists or is inaccessible, participant-based access is unavailable and the evidence remains inspectable under compliance inspection only. Retained evidence is never made uninspectable by the loss of its Conversation." Glossary "Compliance Inspector" (line 91); §2.1 JTBD rewritten (line 33); FR-33 rows (lines 433–435) for inspection, legal hold/export, deletion; §9 (line 734); OQ-21 (line 831).

### C5 — Proposal state names/count contradict public enum — **FULLY**

- **PRD evidence:** FR-18 (line 365) adopts the ten public values with a three-way partition: awaiting decision (`Pending`, `Edited`, `Regenerated`), post-decision (`Approved`, `PostingPending`, `PostingFailed`), terminal (`Posted`, `Rejected`, `Abandoned`, `Expired`); "`Unknown` is the FR-23 sentinel and is never a recorded state." SM-3 (line 791) counts `Approved`/`PostingPending`/`PostingFailed` as "decided but not yet posted". §6.1 (line 639), FR-22 (line 458), UJ-3 resolution (line 66) updated. `PostFailed`: 0 occurrences.

### C6 — Forbidden cost postures pass readiness gate today — **FULLY**

- **PRD evidence:** FR-28 (line 590): "Only the hard-enforcement postures `Quotas`, `Budgets`, and `ProviderModelLimits` satisfy the gate. A recorded posture of `ReportingOnlyMonitoring` or `AcceptedLaunchRisk` … both values are on the FR-23 deprecate-and-reject register, are rejected at readiness recording with a typed rejection, and surface as a `ProhibitedCostControlPosture` readiness blocker — an additive blocker value … Rejecting only an absent posture does not satisfy this requirement." FR-23 register (line 473); NFR-10 (line 669); OQ-6 (line 816).

## 2. High findings

### H1 — Primary metrics measure activity, not thesis — **FULLY**

- **PRD evidence:** SM-7 (line 792) is a new primary: "between 10% and 60% are edited, regenerated, or rejected before that decision … This is the metric the §1 thesis would falsify". SM-2 demoted to *(secondary)* (line 793). OQ-19 (line 829) records the resolution. Uses C5 state names.

### H2 — `status: final` contradicted by own gate state — **DELIBERATELY DIVERGED**

- **What the PRD chose:** keeps `status: final` (line 3) and instead (a) resolves OQ-19 — the alternative path the reviewer accepted — and (b) adds a §0 paragraph (line 14): "The frontmatter `status: final` states that this document is the governing requirements authority. It does not state that the build is implementation-ready: build readiness is owned by the external dependency register and the `RQ-1` gate, and §8 reports the current dependency status without softening it." §8 honesty retained (line 691).
- **What still does not match the reviewer's framing:** OQ-18 remains Deferred (now with a date, line 828) and seven of eight dependencies remain `Uncommitted`, so the "three blockers" note the reviewer wanted in frontmatter is absent by design.
- **Residual defect to fix:** frontmatter `updated: 2026-09-08` (line 5) is stale — the body records a "2026-09-09 reconciliation" in OQ-3, OQ-6, OQ-9, OQ-11, OQ-14, OQ-16, OQ-18–OQ-22, §8.1, and every new `[ASSUMPTION]` tag, and §0 (line 16) says the `updated:` field "records when that last occurred".

### H3 — External contract members lack `[ASSUMPTION]` tags — **FULLY**

- **PRD evidence:** tags at glossary (line 97), FR-7 (line 211), §8 Conversations bullet (line 693: four tags covering the membership members, idempotent posting, `ParticipantRole.Facilitator`, and the active-Conversation count); §8.1 Assumptions Index (lines 700–719). One spelling chosen: `AiAgent` (line 693: "`AiAgent` is the spelling this PRD uses"; `AIAgent`: 0 hits). `CONV-AI-1` without the `EXT-` prefix: 0 hits. The register record says "Final member names are owned by this record (PRD §8.1 assumptions A-1 through A-4)."

### H4 — Conversations dependency surface cannot deliver guarantees — **FULLY**

- **PRD evidence:** §8 (line 693) names four seams under `EXT-CONV-AI-1` — membership, idempotent posting with trace + provenance metadata (NFR-11, FR-11, FR-17), Facilitator resolution, active-Conversation count — each tagged; `EXT-CONV-UI-1` extended to "rendering of the AI-generated and human-edited markers wherever the message's provenance is disclosed". Removal (item 4) downgraded to the Agents-owned block: "Removal of `hexa` needs no Conversations notification … a removal performed directly in Conversations is caught at the next membership check" (line 693, FR-2 line 147).
- **Register evidence:** `EXT-CONV-AI-1` `RequiredArtifact` now lists the four seams with the 2026-09-09 scope-extension note; `EXT-CONV-UI-1` `RequiredArtifact` adds provenance-marker rendering; both remain `Uncommitted` with the same nine fields.

### H5 — FR-13 contradicts FR-7 on access-loss visibility — **FULLY**

- **PRD evidence:** FR-13 (line 316): "Proposal discovery returns full content only for proposals whose Source Conversation the requesting Approver can currently read. For a proposal on which the requester was previously resolved as an Approver but has since lost read access, discovery returns existence and state only … Proposals on which the requester was never resolved as an Approver are not listed, counted, or otherwise disclosed." FR-7 (line 224) cross-references FR-13; UJ-3 edge case (line 67) cites both. "Previously authorized" is defined as "previously resolved as an Approver".

### H6 — Response mode per Agent vs per-Conversation journeys — **FULLY**

- **PRD evidence:** FR-6 (lines 206–207): "`hexa` is instantiated per tenant … Response mode is tenant-wide in V1 … no per-Conversation or per-context override exists". Glossary "Agent" (line 80); UJ-3 persona rewritten (line 62); SM-C1 rewritten (line 797: "A launch tenant may run Confirmation Response Mode tenant-wide"); OQ-20 (line 830).

### H7 — Permission model for all authorization FRs undefined — **FULLY**

- **PRD evidence:** new FR-33 "Define Authorization Roles And Scopes" (lines 417–442), a 13-row matrix covering Provider administration, per-tenant Provider enablement (secret state Platform-Operator-only), `hexa` configuration, Agent call permission (every Participant unless restricted to a tenant role), proposal actions, removal/block, cap configuration (Tenant Admin may lower, never raise — "so the boundary is not self-set"), cap override (Platform Operator only), Content Safety Policy publication, readiness recording, compliance inspection, legal hold/export, deletion. OQ-21 declares FR-33 authoritative.
- **Residual (cosmetic):** the lead sentences of FR-4 ("Authorized administrators"), FR-26 and FR-32 ("Authorized administrators or release operators") were not harmonized to FR-33 vocabulary; FR-33's "Every authorization rule in this PRD resolves to one of six roles" governs, so this is a wording inconsistency rather than a gap.

### H8 — Safe Context Budget omits caller's prompt — **FULLY**

- **PRD evidence:** glossary (line 111) and FR-9 (line 252): "minus the reserved output allowance, the Agent Instructions, the caller prompt and any system framing the request adds, and a configured safety margin … The margin defaults to 10% of the input limit and is configurable from 5% through 25%". FR-9 adds the negative test: "A budget that subtracts only the output allowance does not satisfy this requirement".
- **Choice made:** margin numbers follow M-R3 (10%, 5–25%) rather than H8's example (5%, 1–25%); cross-finding conflict 3 resolved in favour of H14 option (a) + H8 extension.

### H9 — Safety-verdict cache unversioned; conflicts with FR-26 — **FULLY**

- **PRD evidence:** FR-27 (line 574): "The cache is keyed by Conversation, message content version, and Content Safety Policy version: publishing a policy version invalidates every cached verdict tenant-wide, and editing or deleting a Conversation Message invalidates that message's verdict. A cached verdict is never reused under a policy version other than the one that produced it, and Audit Evidence records the policy version each reused verdict came from." OQ-18 restated as "until the history is re-evaluated under a policy it passes" (FR-27 line 575; OQ-18 line 828). FR-26 (line 546) reconciled with the approval-time/pre-post re-checks. OQ-9 (line 819) mirrors the contract.

### H10 — Defective/deferred items on `final` release path — **FULLY**

- **PRD evidence:** OQ-19 resolved now (line 829) — the reviewer's first option — so SM-3 no longer depends on a fixed 26h threshold (line 791). OQ-18 has an owner date: "decision due 2026-10-15 (A-14)" (line 828); the "in any case before production enablement" condition is retained alongside the date, which is a stricter-not-weaker combination. The `status: draft` alternative was not taken (see H2).

### H11 — Removal of `hexa` underspecified and self-defeating — **FULLY**

- **PRD evidence:** FR-2 (line 146): "Removal is an Agents-owned per-Conversation block: it is set and cleared only by those authorities, every set and clear is audited, and while it is set an Agent Call to that Conversation is rejected with a typed reason before any Provider work and no join is re-established." Authority named (Agent Administrator or Conversation Facilitator, A-9); FR-33 row "Remove, block, and re-admit `hexa`" (line 428); OQ-16 amended (line 826).

### H12 — `ConversationOwner` wire value vs "no owner implied" — **FULLY**

- **PRD evidence:** option (a) adopted. Glossary (line 97): "Its stable wire identifier in the Agents contract is `ApproverPolicySourceKind.ConversationOwner`, a legacy name that FR-23 forbids renaming within V1; every human-readable label calls it Conversation Facilitator." FR-7 (line 221) scopes the no-owner rule to "admin UI text, API documentation, or Audit Evidence narrative" (the earlier "API/client contract field" wording is gone) and states "the parity rule is satisfied by the label, not by the identifier." OQ-14 amended (line 824); `ParticipantRole.Facilitator` tagged (A-3).

### H13 — Membership verified at posting, not first call — **DELIBERATELY DIVERGED**

- **What the PRD chose:** it states the target sequence normatively and declares the shipped behaviour non-conformant rather than transitional. FR-2 (lines 142–144): "Membership is the last pre-Provider step of an Accepted Agent Call (§3) … Verifying membership only at posting time, after Provider work, does not satisfy this requirement; the pre-post re-validation in FR-18 is an additional check, not a substitute, and `MembershipUnavailable` and `MembershipRejected` remain posting-failure reasons for that re-validation only." OQ-16 (line 826): "verifying membership only at posting time is non-conformant." Glossary "Accepted Agent Call" (line 79) fixes membership as the last check. "Platform Agents service principal" replaced by the glossary term "Agents Service Principal" (line 85), which explicitly acts for the Agent's Party identity, "never for the caller" — addressing the `ActorUserId` observation.
- **Where it diverges:** the reviewer asked the PRD to "state that until EXT-CONV-AI-1 lands, membership is verified (not established) at posting" as an acknowledged transitional state (cross-finding conflict 7). The PRD contains no transitional clause; the only "transitional" in the document is FR-28's per-Agent readiness record. The consequence is that today's code is out of conformance on FR-2 until the gate gains a membership check — a defensible product position, but downstream drift reviews will keep reporting it.

### H14 — Budget omits Instructions/margin; blocked mode is `Unknown` — **DELIBERATELY DIVERGED**

- **Formula (follows reviewer option (a)):** the PRD keeps Instructions and margin as required policy content and extends the formula per H8; evidence fields are named generically — "the computed budget, each subtracted term, the measured context size, and the `CapabilityVersion` used are recorded in Audit Evidence for every call" (glossary line 111; FR-9 line 256; FR-24 line 505).
- **Blocked mode (diverges):** the reviewer recommended describing the blocked case as "mode `Unknown` with a recorded block reason"; the PRD instead requires an additive value (FR-9 line 257): "`Full` … `Bounded` … or `Blocked` with a typed block reason … `Blocked` is an additive public value under FR-23; the `Unknown` sentinel is never a recorded context mode." This resolves cross-finding conflict 4 in favour of H15/M-A3 and obliges Architecture to add `AgentInteractionContextMode.Blocked`.

### H15 — `Unknown` outcome collides with sentinel; `MaxRetries` vs at-most-once — **DELIBERATELY DIVERGED** (name only; substance FULLY)

- **PRD evidence:** FR-12 (line 297): "resolves to a typed `Indeterminate` terminal outcome — an additive public value distinct from the `Unknown = 0` sentinel that FR-23 reserves for unset values … Mapping an indeterminate outcome to an adapter or generation error does not satisfy this requirement." NFR-11 (line 670) and FR-28 (line 587) use the same name. `MaxRetries` constrained rather than dropped — FR-4 (line 176): "A per-model retry budget, where configured, authorizes re-invocation only for an attempt whose Provider outcome is a confirmed no-usage outcome, under the same cost reservation (FR-28). It never authorizes re-invocation after an `Indeterminate` outcome and is not a silent retry mechanism."
- **Divergence:** cross-finding conflict 5 resolved with M-A3's `Indeterminate`, not drift's `OutcomeIndeterminate` (0 hits). Both were offered as examples, so this is a recorded choice rather than a gap.

## 3. Co-located medium/low findings applied by the update run

| ID | Status | PRD evidence | Remaining gap |
| --- | --- | --- | --- |
| M-R1 | FULLY | FR-7 line 225: call-time typed reason `NoEligibleApprover`, resolved before Provider work; edit-time typed rejection. | — |
| M-R2 | FULLY | Glossary "Accepted Agent Call" line 79: ordered pre-Provider checks; "The acceptance timestamp is recorded when the last check passes and starts the NFR-9 latency clocks." SM-2 line 793 uses the term. | — |
| M-R3 | FULLY | Margin default 10%, 5–25% (lines 111, 252; A-5); `Indeterminate` hold default 24h, 1–72h (FR-28 line 587; OQ-6 line 816; A-8). | — |
| M-R4 | FULLY | Regeneration ceiling default 3, range 1–10 (FR-16 line 348; FR-32 line 607; A-6); `PostingFailed` retry "at most 3 attempts over 15 minutes" with post-exhaustion behaviour (FR-18 line 373; A-7). | — |
| M-R5 | FULLY | FR-18 line 372: "Approval freezes expiry: `Approved`, `PostingPending`, and `PostingFailed` proposals never expire." OQ-3 line 813 matches. SM-3 line 791 propagates. | — |
| M-A3 | FULLY | FR-12 line 297, NFR-11 line 670: `Indeterminate`, additive, distinct from the sentinel. | — |
| M-A7 | FULLY | FR-18 line 374: pre-post re-validation "that the Agent is active and its Party identity valid (FR-2, FR-3), that `hexa` is still a member of and not blocked in that Conversation". | — |
| M-A8 | PARTIALLY | SM-2 exclusion done (glossary line 79; FR-16 line 349; SM-2 line 793: "regenerations are excluded from the numerator"). | The PRD still does not state whether a regeneration re-runs the FR-9 Safe Context Budget check and the FR-27 pre-Provider scan (FR-16 line 345 says only "uses the same Source Conversation and Agent configuration"). |
| M-D2 | FULLY | Default 24h expiry stated in FR-18 line 371 and OQ-3; `IsExpirable` exclusion of `Approved`/`PostingPending`/`PostingFailed` now matches the PRD's freeze rule (line 372). | — |
| M-D6 | FULLY | FR-23 line 473: `ContentSafetyFailureHandling.BlockWithAuditableOverride` on the deprecate-and-reject register "(FR-27; Approvers cannot override a safety failure)". | — |
| L-A1 | FULLY | Glossary "Agent Response" line 84: "becomes a Conversation Message only after it is posted … approval alone does not make it a Conversation Message." | — |
| L-A2 | PARTIALLY | §9 line 730 now says "unless legal hold suspends the retention period". | §9 line 731 still opens "Retention expiry or approved deletion …"; the reviewer asked for "retention period" throughout §9 so "expiry" is reserved for proposal `ExpiresAt`. One-word edit. |
| L-A3 | FULLY | "expiry-resolution" 0 hits; FR-7 line 215 lists "edit, regeneration, approval, rejection, and abandonment". | — |
| L-A5 | FULLY | FR-7 line 222 re-check list: "discovery, edit, regeneration, approval, rejection, abandonment, and audit-content inspection". | — |
| L-A8 | FULLY | SM-1 line 784: "Validates FR-1 through FR-6 and FR-8." | — |
| L-R4 | FULLY | Glossary "Agents Service Principal" line 85; FR-21 line 415 defines "the named posting principal — the Agent's Party identity acting through the Agents Service Principal". | — |
| L-D1 | PARTIALLY | FR-23 line 474: "`[Flags]` enums use `None = 0` and are exempt from the `Unknown = 0` rule". | `AgentSetupWriteStatus.Submitted = 0` is still a live violation: FR-29 line 483 keeps `Submitted` as a reported stage with no `Unknown = 0` requirement, exemption, or deprecate-and-reject note for that enum. |

## 4. Cross-finding conflicts — how each was resolved

1. SM-3/`RQ-1`: C3's gate removal adopted; C1 definitional repair applied; H1's governance metric added as SM-7; C5 names used.
2. Status label: neither `approved-pending` nor `draft`; `final` kept with a §0 authority-vs-readiness clause and OQ-19 resolved (H2 diverged).
3. Budget formula: extended (H8) with H14 option (a); margin per M-R3.
4. `Unknown` as recorded value: additive `Blocked` context mode required; sentinel never recorded.
5. Indeterminate name: `Indeterminate`.
6. Cache vs OQ-18: cache contract written in FR-27/OQ-9; OQ-18 block restated as "until re-evaluated under a policy it passes".
7. Membership timing: target sequence stated; no transitional clause (H13 diverged).
8. `ConversationOwner`: legacy-alias option (a); no new deprecate-and-reject entry.
9. FR-7: single rewrite covering C2, M-R1, and the per-source disclosure category (line 219: "Every configured Approver Policy source declares its disclosure category at configuration time. Absent an explicit choice, the category is operator-only").
10. Expiry of post-decision states: frozen at `Approved`, decided once in FR-18 and propagated to OQ-3 and SM-3.

## 5. Assumption round-trip (§8.1)

24 `[ASSUMPTION` strings occur in `prd.md`. Six are meta-references to the tag mechanism, not assumptions (lines 14, 419, 702, 780, 831, and the FR-33 preamble's "Rows marked `[ASSUMPTION]`"). The 18 substantive tags map as follows.

| Tag location | Index row | Match |
| --- | --- | --- |
| §3 Conversation Facilitator (line 97) | A-3 | yes |
| §3 Eligible Conversation count (line 100) | A-4 | yes |
| §3 Safe Context Budget margin (line 111) | A-5 | yes |
| FR-2 removal authority (line 146) | A-9 | yes (tag says "Agent Administrator"; row says "Tenant Agent Administrator" — same role) |
| FR-7 Facilitator (line 211) | A-3 | yes |
| FR-9 margin (line 252) | A-5 | yes |
| FR-16 regeneration ceiling (line 348) | A-6 | yes |
| FR-18 retry bound (line 373) | A-7 | yes |
| FR-33 Provider enablement row (line 424) | A-10 | yes |
| FR-33 Call `hexa` row (line 426) | A-11 | yes |
| FR-33 removal row (line 428) | A-9 | yes |
| FR-33 deletion row (line 435) | A-12 | yes |
| FR-28 hold period (line 587) | A-8 | yes |
| §8 membership members (line 693) | A-1 | yes |
| §8 idempotent posting (line 693) | A-2 | yes |
| §8 `ParticipantRole.Facilitator` (line 693) | A-3 | yes |
| §8 active-Conversation count (line 693) | A-4 | yes |
| §12 SM-3 threshold (line 791) and SM-7 band (line 792) | A-13 | yes |

**Every substantive tag has an index row.** Mismatches in the other direction:

- **A-14 (OQ-18 decision due 2026-10-15) has no `[ASSUMPTION]` tag anywhere.** OQ-18 (line 828) cites "(A-14)" in parentheses but carries no tag, so the round-trip fails for this row. Fix: add the tag in the OQ-18 row or drop A-14 and express the date as a plain decision.
- **Six rows cite "Where" locations that carry no tag** (the assumption is tagged elsewhere, so these are location over-citations, not missing tags): A-2 cites FR-11, FR-17, NFR-11 (tag only in §8); A-3 cites OQ-14 (row references A-3 by ID only); A-4 cites SM-2 (tag only in §3 and §8); A-6 cites FR-32 (tag only in FR-16); A-11 cites FR-8 (tag only in FR-33); A-13 cites OQ-11 (row references A-13 by ID only). Either add the tag at each cited location or trim "Where" to the tagged locations.

## 6. Forbidden-string checks

| Check | Result |
| --- | --- |
| `PostFailed` | 0 occurrences (only `PostingFailed`, 12 occurrences) |
| "platform Agents service principal" (case-insensitive) | 0 occurrences; replaced by the glossary term "Agents Service Principal" (lines 85, 143, 374, 415, 826) |
| Typed `Unknown` outcome | none survives. Remaining `Unknown` mentions (lines 257, 297, 365, 470, 474) all describe the FR-23 sentinel and state that it is never a recorded context mode, outcome, or proposal state |
| `AIAgent` spelling / bare `CONV-AI-1` | 0 / 0 |
| "bare tenant role" / "expiry-resolution" / "satisfies launch-readiness" / `MaxRetries` | 0 each |

## 7. Residual items for the next PRD edit

1. Frontmatter `updated:` still reads `2026-09-08`; body is dated 2026-09-09 (H2 residual).
2. Add the A-14 tag to OQ-18 or retire the row (§5).
3. Trim or tag the six over-cited "Where" locations (§5).
4. M-A8: state whether regeneration re-runs the FR-9 budget and FR-27 pre-Provider checks.
5. L-A2: "Retention expiry" → "Retention-period end" (or equivalent) in §9 line 731.
6. L-D1: decide the `AgentSetupWriteStatus.Submitted = 0` case in FR-23/FR-29.
7. Optional: harmonize FR-4/FR-26/FR-32 lead sentences to FR-33 role names (H7 residual).
