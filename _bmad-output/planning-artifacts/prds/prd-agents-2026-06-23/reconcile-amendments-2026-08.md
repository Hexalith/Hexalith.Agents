---
title: PRD Amendment Reconciliation - 2026-08-02 and later
created: 2026-09-08
scope: prds/prd-agents-2026-06-23/prd.md
prd_frontmatter_updated: 2026-08-01
purpose: Identify approved post-2026-08-01 amendments that mandate PRD changes and whether they have landed
---

# PRD Amendment Reconciliation (sources dated 2026-08-02 and later)

## 1. Source Survey

Glob over `_bmad-output/planning-artifacts/` found eight candidate docs dated on/after
2026-08-02 (seven named in the task plus two additional found by glob:
`sprint-change-proposal-2026-08-02.md` and
`sprint-change-proposal-2026-08-04-dev-agent-record-gate.md`).
`implementation-readiness-report-2026-08-04.md` was also present and reviewed for completeness.

| # | Source | Approved? | Approval evidence (quoted) | Mandates a PRD change? |
| --- | --- | --- | --- | --- |
| S1 | `sprint-change-proposal-2026-08-02.md` | YES | `status: approved` / `approved_by: Administrator` / `approved_on: 2026-08-02` | **NO.** §4.9 PRD Disposition: "No PRD text or MVP scope change is proposed. The current PRD already owns..."; impact table: "\| PRD \| No remaining scope conflict... \| Preserve the PRD and create the delegated artifacts \|" |
| S2 | `sprint-change-proposal-2026-08-02-readiness-remediation.md` | YES | `status: approved` / `approved_on: 2026-08-02` / `revalidated_on: 2026-08-03` | **NO.** Frontmatter `preserves: - prds/prd-agents-2026-06-23/prd.md`; impact table "\| PRD \| No conflict \| Preserve unchanged; MVP and FR/NFR inventory remain authoritative \|"; checklist "\| 3.1 \| Done \| PRD goals and MVP remain achievable without edits \|" |
| S3 | `sprint-change-proposal-2026-08-03.md` | YES | `status: approved` / `approved_by: Administrator` / `approved_on: 2026-08-03` | **YES.** Frontmatter `amends_if_approved:` includes `prds/prd-agents-2026-06-23/prd.md`; §4.2 "PRD Precision Edits"; checklist "\| 3.1 PRD conflict \| Action required \| Precision terminology and dependency edit; MVP unchanged \|" |
| S4 | `sprint-change-proposal-2026-08-03-readiness-rerun-follow-up.md` | YES | `status: approved` / `approved_by: Administrator` / `approved_on: 2026-08-04` | **REAFFIRMS S3 ONLY** (no new PRD requirement). Impact table: "\| PRD / Architecture / UX \| Apply already approved Facilitator terminology and Conversation-owned invocation precision edits \| No MVP change \|"; checklist "\| 3.1 PRD \| [x] \| MVP unchanged; approved terminology/dependency precision edits remain sufficient \|" |
| S5 | `sprint-change-proposal-2026-08-04.md` | YES | `status: approved` / `approved_on: 2026-08-04` | **NO.** §4.11: "**PRD — OLD -> NEW:** no normative change. Preserve V1 scope, all 28 Functional Requirements, NFR-1 through NFR-14, the Decision Register, and `RQ-1` separation."; impact table "\| PRD \| No conflict \| No product or MVP edit \|" |
| S6 | `sprint-change-proposal-2026-08-04-dev-agent-record-gate.md` | YES | `status: approved` / `approved_on: 2026-08-04` | **NO.** "\| PRD impact \| None \| Product requirements and MVP remain achievable. \|" (workflow/tooling scope only) |
| S7 | `sprint-change-proposal-2026-08-04-live-integration-tier.md` | YES | `status: approved` / `approved_on: 2026-08-04` | **NO.** `preserves: - PRD V1 scope and 28/28 Functional Requirement coverage`; "\| PRD \| No conflict \| No change \|"; "\| 4.3 PRD MVP review \| Not selected \| MVP scope is unchanged \|" |
| S8 | `implementation-readiness-report-2026-08-02.md` | N/A (assessment, `status: NOT_READY`) | Not an approval artifact; no approval markers | **NO.** Findings route to epics/registers/dependency commitments; no PRD text change requested |
| S9 | `implementation-readiness-report-2026-08-03.md` | N/A (assessment, `assessmentStatus: NOT READY`) | Not an approval artifact | **INDIRECT.** Finding #1 (line 232) raised the Conversation-owner/Facilitator terminology conflict that S3 then converted into an approved PRD amendment. No independent PRD mandate beyond S3. |
| S10 | `implementation-readiness-report-2026-08-04.md` | N/A (assessment) | Not an approval artifact | **NO.** Confirms 28/28 FR coverage and aligned PRD |

**Conclusion of survey:** exactly one approved source (`sprint-change-proposal-2026-08-03.md`,
§4.2, reaffirmed by S4) mandates changes to `prd.md`. It decomposes into five discrete
amendments below.

## 2. Consolidated PRD-Affecting Approved Amendments

### AM-1 — Rename the Approver Policy authority source to "Conversation Facilitator"

- **Source + approval evidence:** `sprint-change-proposal-2026-08-03.md` (`status: approved`,
  `approved_on: 2026-08-03`, `amends_if_approved` includes `prds/prd-agents-2026-06-23/prd.md`).
  §4.2 → Approver Authority → After: *"In the glossary, FR-7, policy examples, audit disclosure,
  and Decision Register, use: **Conversation Facilitator (the V1 Conversation authority)**."*
- **What it requires:** Replace the phrase "Conversation owner" with
  "Conversation Facilitator (the V1 Conversation authority)" everywhere the PRD names the
  Approver Policy source.
- **PRD landing sites:**
  - §3 Glossary → **Approver Policy** entry (`prd.md:80`) — currently reads
    "...configured sources such as Conversation owner, the caller, predefined Parties, or tenant roles."
  - §4.3 → **FR-7: Configure Approver Policy** (`prd.md:177-187`, statement at `prd.md:179`).
  - §9 Data Governance And Audit (`prd.md:530-541`) — audit-disclosure bullets referencing the
    authorizing policy source.
  - §13 V1 Decision Register (`prd.md:596-614`).
- **Already present?** **NO.** `grep -n "Facilitator" prd.md` returns nothing (zero hits in the
  entire PRD). `grep -ni "conversation owner" prd.md` still returns lines 80 and 179.
- **Conflicts:** None with a recorded PRD decision. §13 has no row on approver-authority
  resolution, so no register entry contradicts the rename. The rename does contradict the
  *current* PRD wording only, which is what the amendment corrects.

### AM-2 — State normatively that V1 resolves the authority from `ParticipantRole.Facilitator`

- **Source + approval evidence:** Same source/approval as AM-1. §4.2 → Approver Authority → After:
  *"State normatively that V1 resolves this authority from `ParticipantRole.Facilitator`."*
  Rationale in the same proposal (§4.2 Before): *"The PRD and UX use "Conversation owner," while
  AD-8 implements that source as `ParticipantRole.Facilitator` because the current Conversations
  contract exposes no owner field."*
- **What it requires:** A normative sentence binding the V1 Approver Policy authority source to
  the Conversations `ParticipantRole.Facilitator` role value.
- **PRD landing sites:** §4.3 FR-7 body or its **Consequences (testable)** list (`prd.md:181-187`);
  optionally mirrored in the §3 Glossary **Approver Policy** entry (`prd.md:80`).
- **Already present?** **NO.** `grep -n "ParticipantRole" prd.md` returns a single hit at
  `prd.md:523` — the §8 `EXT-CONV-AI-1` membership contract (`ParticipantRole.Member`), which is
  about AI-participant membership, not approver authority. No FR-7 or glossary reference exists.
- **Conflicts:** None. FR-7 currently leaves the source abstract ("Conversation owner"), so adding
  the normative resolver narrows rather than reverses a decision.

### AM-3 — Prohibit text implying a distinct Conversation owner was resolved

- **Source + approval evidence:** Same source/approval as AM-1. §4.2 → Approver Authority → After:
  *"Prohibit UI/API/evidence text from implying that a distinct Conversation owner was resolved."*
- **What it requires:** A normative prohibition that admin UI, API/client contracts, and audit
  evidence must not present the authorizing source as a distinct "Conversation owner".
- **PRD landing sites:**
  - §4.3 FR-7 **Consequences (testable)** (`prd.md:181-187`) — extends the existing bullets
    "The system exposes which configured policy source authorized the Approver..." and
    "API/client contracts and admin UI use the same disclosure category for the same
    approval-policy basis."
  - §9 Data Governance And Audit (`prd.md:530-541`) for the evidence-text half.
  - Optionally §10 API Contracts And Public Surface (`prd.md:543-557`).
- **Already present?** **NO.** No prohibition text exists; the PRD instead still uses the
  forbidden term at `prd.md:80` and `prd.md:179`. No hit for "must not imply", "distinct
  Conversation owner", or "Conversation authority".
- **Conflicts:** None recorded in §13.

### AM-4 — Record a future true-owner resolver as a separate post-V1 decision

- **Source + approval evidence:** Same source/approval as AM-1. §4.2 → Approver Authority → After:
  *"Record a future true-owner resolver as a separate post-V1 decision unless a committed
  Conversations contract is introduced."*
- **What it requires:** A new deferred/post-V1 decision entry stating that a genuine
  Conversation-owner resolver is out of V1 and returns only if Conversations commits an explicit
  owner contract.
- **PRD landing sites:** §13 V1 Decision Register (`prd.md:596-614`) — a new row after `OQ-13`
  (next free id `OQ-14`); alternatively §5 Non-Goals (`prd.md:443-456`) or
  §6.2 Out Of Scope For MVP (`prd.md:479-489`) as a supporting mention.
- **Already present?** **NO.** The Decision Register runs `OQ-1` … `OQ-13` with no
  approver-authority row. `grep -ni "post-V1\|true-owner\|owner resolver\|resolver" prd.md`
  returns zero hits.
- **Conflicts:** None — the register currently has no row that this would supersede. Note the §13
  preamble ("All implementation-blocking product and governance questions were resolved by the
  approved Correct Course decisions on 2026-08-01") will need its date/provenance touched if a
  2026-08-03-sourced row is added, otherwise the preamble understates the register's provenance.

### AM-5 — Add `EXT-CONV-UI-1` as an eighth critical external dependency in §8

- **Source + approval evidence:** `sprint-change-proposal-2026-08-03.md` §4.2 → Integration
  Dependencies → Before: *"PRD §8 lists seven critical dependencies. `EXT-CONV-AI-1` covers AI
  membership and posting but not rendering a module action in a Conversation-owned surface."*
  After: *"Add `EXT-CONV-UI-1` to PRD §8 with the same commitment and fail-closed rules as the
  existing prerequisites."* Reaffirmed by `sprint-change-proposal-2026-08-03-readiness-rerun-follow-up.md`
  (`approved_on: 2026-08-04`): *"add approved `EXT-CONV-UI-1` during canonical synchronization"*.
- **What it requires:** `EXT-CONV-UI-1` — a versioned Conversation action contribution/registration
  contract letting Agents render the **Call hexa** action inside the Conversation-owned surface —
  added to the §8 initial register scope and governed by the identical nine-field commitment record
  and `Uncommitted`-blocks-`ready-for-dev` rule.
- **PRD landing sites:**
  - §8 Integration And Dependencies (`prd.md:507-529`) — the register-scope sentence at
    `prd.md:521` currently enumerates exactly seven ids
    (`EXT-CONV-AI-1`, `EXT-HOST-1`, `EXT-PROVIDER-1`, `EXT-SAFETY-1`, `EXT-TOKEN-1`,
    `EXT-SECRETS-1`, `EXT-TOPOLOGY-1`).
  - §8 **Hexalith.Conversations** bullet (`prd.md:523`) — extend to name the UI-surface prerequisite
    alongside `EXT-CONV-AI-1`.
  - Cross-check §4.4 FR-8 (`prd.md:194-202`) and §13 `OQ-1` (Conversation-owned **Call hexa**
    action), which now depend on an unnamed prerequisite.
- **Already present?** **NO.** `grep -n "EXT-CONV-UI-1" prd.md` returns zero hits; the §8 list at
  `prd.md:521` is still the seven-id enumeration.
- **Conflicts:** Does not reverse `OQ-1`; it supplies the missing prerequisite that `OQ-1` implies.
  Note a downstream consistency issue outside this file's scope: a repository-wide grep shows
  `EXT-CONV-UI-1` is also absent from `external-dependency-register.md`, so landing AM-5 in the PRD
  alone would create a PRD→register dangling reference. The two edits should be sequenced together.

## 3. Summary

| Amendment | PRD landing site | Status |
| --- | --- | --- |
| AM-1 Facilitator terminology | §3 Glossary (L80), FR-7 (L177-187), §9, §13 | **MISSING** |
| AM-2 `ParticipantRole.Facilitator` normative resolver | FR-7 (L181-187) | **MISSING** |
| AM-3 Prohibit "distinct Conversation owner" phrasing | FR-7 consequences, §9, §10 | **MISSING** |
| AM-4 Post-V1 true-owner resolver decision | §13 Decision Register (new OQ-14) | **MISSING** |
| AM-5 `EXT-CONV-UI-1` dependency | §8 (L521, L523) | **MISSING** |

- Approved PRD-affecting amendments: **5** (all from one approved source, `sprint-change-proposal-2026-08-03.md` §4.2, reaffirmed 2026-08-04).
- Already landed in `prd.md`: **0**.
- Genuinely missing: **5**.
- Non-PRD approved changes correctly excluded: architecture-spine edits (§4.3 of S3: AD-6/AD-8/AD-10/AD-15/AD-16), UX edits (§4.4 of S3), the 44-story epic graph, dependency/launch registers, sprint status, and the dev-agent-record workflow gate.

**Corroborating signal:** the PRD frontmatter still reads `updated: 2026-08-01`, consistent with
none of the 2026-08-03 amendments having been applied. Applying AM-1 … AM-5 should also bump
`updated:` to the application date and note the amending proposal.
