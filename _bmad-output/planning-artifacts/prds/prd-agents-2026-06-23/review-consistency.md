# Consistency Review — PRD Hexalith Agents (`prd.md`, second update of 2026-09-09, status final)

Scope: Part 1 internal consistency of `prd.md` (960 lines). Part 2 alignment with `architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md` (758 lines), `external-dependency-register.md` (183), `launch-readiness-register.md` (270), and `epics.md` (2780). Line numbers refer to each file as read on 2026-09-09. Baseline: `review-consistency-2026-09-09-validate.md` (17 findings), each re-verified below.

## Part 0 — Disposition of the 17 prior findings

| # | Prior finding (short) | Disposition | Evidence |
| --- | --- | --- | --- |
| 1 | Launch-readiness register gates `RQ-1` on SM-1..SM-6; no SM-7 contract | **Still open** (Critical, cross-artifact) | `launch-readiness-register.md` 79 "Versioned SM-1 through SM-6 calculation and real rolling-window/cohort attainment", 139, 239; `SM-7` occurs nowhere in the file. PRD FR-28 656/666, §11 894, §12 900–913, OQ-22 952 keep the split. Re-raised as C-X1. |
| 2 | §0 "seven of eight" | **Resolved** | `prd.md` 16: "eight of the nine register entries (§8)". |
| 3 | `epics.md` inventory stale (FR7 caller source, pre-2026-09-09 FR18, no FR-29..FR-33) | **Still open** (High, cross-artifact) | `epics.md` 48, 70 unchanged; `FR29`–`FR34` zero hits. Re-raised as H-X3 with additional stale rows (FR1, FR21, FR26). |
| 4 | Who creates `hexa` (PRD vs Spine AD-2/AD-30) | **Resolved** — PRD adopted the Spine model | `prd.md` FR-1 132, FR-33 443, UJ-1 50, §3 109, §4.1 126, §10 860, OQ-28 958. Confirmed consistent with Spine AD-2 109 and AD-30 277 (see "Confirmed intended alignments"). |
| 5 | FR-18 listed "kill switch" as a system-abandonment reason | **Resolved** | `prd.md` 419: three reasons only; "The kill switch (FR-28) is never a system-abandonment reason". |
| 6 | FR-28 named the wrong readiness authority | **Resolved** | `prd.md` 672: "The launch readiness register's gate records are the readiness authority; the external dependency register is the commitment authority". |
| 7 | Launch-readiness register lacks `UnretiredAssumption` / `ProhibitedCostControlPosture` | **Still open** (cross-artifact) | grep of `launch-readiness-register.md` returns 0 for both; only `EvidenceNotRecorded`, `DependencyUncommitted`, `GateRecordMissing` exist (46, 249–266). Folded into follow-up F-LRR-1 together with the three new codes. |
| 8 | Register has no non-conformance record for Story 5.3 | **Still open** (cross-artifact) | `external-dependency-register.md` contains no "non-conform"; 107 still narrows 5.3 out. PRD §8 789, FR-21 504, OQ-17 947 still assert the record exists. Re-raised as M-X8. |
| 9 | Register `EXT-CONV-AI-1` consumers omit seam 4/5 consumers | **Still open** (cross-artifact) | `external-dependency-register.md` 61: "6.6, 7.4; `RQ-1`". Re-raised as M-X7. |
| 10 | FR-33 "two rows" use the Facilitator | **Resolved as written, but re-opened in the opposite direction** | `prd.md` 437 now says "one row"; matrix rows 447 (block) and 448 (clear) both name the Conversation Facilitator as an authority. See L-I1. |
| 11 | OQ-18 status lacked "amended 2026-09-09" | **Resolved** | `prd.md` 948. |
| 12 | A-3 index row carried an unkeyed multiplicity clause | **Resolved** | `prd.md` 817: "(cited by OQ-14)" only; OQ-14 944 cites A-3. |
| 13 | Blocked-call lists differ in §3, FR-25, SM-C4 | **Resolved** | §3 104 and FR-25 584 cite "SM-C4 blocked-call reason"; SM-C4 920 holds the union and says "§3 and FR-25 cite this list". A residual classification gap is M-I1. |
| 14 | "then-current" vs "more restrictive of initial and then-current" | **Still open** (Low, internal) | `prd.md` 392, 408, 426, 615, 939 say "then-current"; 619 says "the more restrictive of". Re-raised as L-I3. |
| 15 | §10 grouped Conversation Context Policy / regeneration ceiling differently from FR-33 | **Resolved** | `prd.md` 860 (Agent administration lists both), 865 (budget policy no longer lists the ceiling), FR-33 444 lists both. |
| 16 | `Available` used without definition | **Resolved** | `prd.md` 787 defines the third status; consistent with register 41. |
| 17 | `update-report-2026-09-09.md` does not record the two proposal amendments | **Still open — out of scope** for this review's artifact set | grep of `update-report-2026-09-09.md` for `EXT-PROTECTION-1` / `ARCH-A` returns nothing. Listed under follow-ups. |

Tally: 9 resolved (2, 4, 5, 6, 11, 12, 13, 15, 16; 10 resolved-but-flipped), 7 still open (1, 3, 7, 8, 9, 14, 17 of which 17 is out of scope).

## Part 1 — Internal consistency of `prd.md`

### Checked clean

- Every `FR-1`..`FR-34`, `NFR-1`..`NFR-14`, `OQ-1`..`OQ-30`, `SM-1`..`SM-7`, `SM-C1`..`SM-C5`, `UJ-1`..`UJ-4`, `A-1`..`A-20`, `ARCH-A-1/6/7`, and `§0/§1/§3/§6.2/§8/§8.1/§9/§11/§12/§13` reference resolves; each series is contiguous (FR-33 is defined at 435, ahead of FR-19, which is intentional). 30 OQ rows exist.
- Every `[ASSUMPTION A-n]` tag (lines 100, 104, 160, 227, 246, 284, 380, 423, 442, 445, 447, 448, 464, 568, 667, 675, 676, 792–797, 800, 898, 911, 912, 948) maps to the "Where" cell of its §8.1 row (815–834), and every row's "Where" names only locations that carry the tag. A-6 "(restated in FR-32)" and A-13 "(restated in OQ-11)" are accurate; FR-32 691 and OQ-11 941 restate without a tag.
- `RQ-1` input list: §3 115, §11 894, OQ-22 952, OQ-24 954 all defer to FR-28 654–664 and do not restate it; the launch-health set (SM-2, SM-3, SM-7) is identical in §3 115, FR-28 666, §11 894, §12 907–913, OQ-22.
- Kill switch: FR-3 165/170, FR-18 419/425, FR-25 586, FR-28 673–677, FR-33 453–454, OQ-27 957 state one behavior: calls disabled tenant-wide; awaiting proposals reject/abandon only; `Approved`/`PostingPending` complete or fail; `PostingFailed` retry clock paused and retries suspended; `ExpiresAt` keeps running; nothing system-abandoned or deleted; `Suspended` reported; same roles release with audited justification.
- FR-2 five-state Conversation Agent State (§3 97, FR-2 150–161) agrees with OQ-16 946 and OQ-25 955 on detection-only-from-`Joined`, re-admission only from `ReadmitPending`, unavailable read changes nothing, block authoritative when written, `MirrorPending` outbox, and clearing → `ReadmitPending`.
- Compliance second party: FR-24 568, FR-33 457, OQ-21 951, OQ-30 960 agree (never the Inspector nor a subject; Platform Operator or a second Compliance Inspector where the case concerns the Administrator; post-hoc only for single-proposal/single-Conversation within the 7-day A-19 window; else recorded unreviewed).
- `hexa` provisioning: FR-1 132/136, FR-3 165 (`Draft` = provisioned), FR-33 443, UJ-1 50, §3 109, §4.1 126, §10 860, OQ-28 958 agree: Platform Operator, create-only, once per tenant, idempotent, tenant roles denied.
- `Uncommitted`/`Committed`/`Available`: §0 16, FR-21 503/506/507, §8 787/789, FR-28 653/660, FR-34 697, OQ-24 954 use one vocabulary with one definition (787).
- §13 preamble vs status cells: every OQ named as amended on 2026-09-08 (OQ-3, 6, 9, 10) or 2026-09-09 (OQ-3, 6, 9, 11, 14, 16, 18, 21, 22) carries the matching "amended" text; OQ-24..OQ-30 are all "Resolved 2026-09-09". Residual convention nits are L-I5.
- §0 status line 16 matches §8 787/789 (nine entries; only `EXT-HOST-1` `Committed`; none `Available`) and register 183.
- FR-8 nine-step order (264–273) matches the §3 "Accepted Agent Call" definition, FR-28 667 (reserve after context measurement), and FR-7 243/FR-2 150 (Eligible Approver resolution then membership last).
- FR-23 deprecate-and-reject register (538) names exactly the values FR-7 227, FR-27 640, FR-28 670, NFR-10 767, OQ-6, OQ-14 refer to.

### Findings

#### Critical

None.

#### High

**H-I1. A `PostingFailed` exit named in FR-18 prose has no transition-table row and no producing check.**
- `prd.md` 423: a `PostingFailed` proposal remains until "… or the system abandons it under FR-7 on an authoritative answer that the Source Conversation is gone."
- `prd.md` 402: "The allowed transitions are exactly the rows below; any other (from, to) pair is rejected with a typed reason." The only system row out of `PostingFailed` into `Abandoned` is 414, whose reason is fixed to `RemovedInConversations` and whose guard is "Removal or block detected (FR-2)". No row yields `Abandoned` (`SourceConversationUnavailable`) from `PostingFailed`.
- `prd.md` 246: the FR-7 scheduled re-check "runs over every awaiting proposal", i.e. `Pending`/`Edited`/`Regenerated` only, so nothing ever produces the authoritative "Conversation is gone" answer for a `PostingFailed` proposal; the pre-post re-validation at 426 turns an inaccessible Conversation into `PostingFailed`, not `Abandoned`.
- Impact: the exit promised at 423 ("therefore always has an exit") is unreachable as specified; an implementer following the table leaves such a proposal to the Tenant Agent Administrator row only.
- Fix (`prd.md`): add a row `PostingFailed | Abandoned (SourceConversationUnavailable) | System | Authoritative answer under FR-7 that the Conversation is deleted or the Agents Service Principal is denied; the MessageId lookup finds no posted message`, and change 246 to "runs over every awaiting proposal and every `PostingFailed` proposal". Alternatively drop the clause from 423 and from AD-5's mirror wording.

#### Medium

**M-I1. `MembershipUnavailable` is both an acceptance-time rejection and "a posting-failure reason for re-validation only", and the SM-C4 "exactly" list classifies neither it nor several other pre-Provider outcomes.**
- `prd.md` 155: "Any state when the participant read is unavailable: reject the call with the typed reason `MembershipUnavailable`".
- `prd.md` 158: "`MembershipUnavailable` (the participant read is unavailable) and `MembershipRejected` (…) remain posting-failure reasons for that re-validation only".
- `prd.md` 920 (SM-C4): "The blocked-call reasons are exactly: Conversation Context Policy, Content Safety Policy, cost caps, rate limits, `NoEligibleApprover`, and `RemovedInConversations` or the Agents-owned block; Provider and generation failures are failed calls". `MembershipUnavailable` (155), capacity queue/reject before Provider invocation (NFR-12 769), `DependencyNotAvailable` and `PayloadProtectionUnavailable` (FR-21 506–507), and authorization denials (FR-25 582) are pre-Provider outcomes in neither the blocked nor the failed list, so FR-25 584 ("by each SM-C4 blocked-call reason") cannot classify them.
- Fix (`prd.md`): reword 158 to "…are, as posting-failure reasons, produced only by that re-validation; `MembershipUnavailable` is additionally the acceptance-time rejection of the previous bullet", and add to SM-C4 a sentence "Authorization denials, `MembershipUnavailable`, capacity queueing or rejection, `DependencyNotAvailable`, and `PayloadProtectionUnavailable` are neither blocked nor failed calls for this share; FR-25 reports them separately."

**M-I2. Who may clear a block that the membership step set on an external removal is not stated.**
- `prd.md` 153/161: on detecting an external removal the membership step "record[s] `ExternallyRemoved`, set[s] the Agents-owned block" / "sets the block itself".
- `prd.md` 160 and FR-33 448: "A block is cleared only by the authority that set it or by the Tenant Agent Administrator"; the setting authority here is the system, so by the letter only the Tenant Agent Administrator can re-admit `hexa` after a Facilitator removed it in Conversations — the Facilitator who removed it cannot. OQ-16 946 and OQ-25 955 repeat the rule without covering this case; A-9 (823) covers only Administrator/Facilitator-set blocks.
- Impact: this is a Product-owned authorization rule (A-9) that the Spine reads the opposite way (M-X2).
- Fix (`prd.md` 160 and FR-33 448): state explicitly, e.g. "A block set by the membership step on a detected external removal is cleared by the Tenant Agent Administrator or the Conversation Facilitator", and extend the A-9 row text accordingly.

#### Low

**L-I1. FR-33 437 says the Conversation Facilitator is an authority in "one row"; rows 447 and 448 both name it** (447 "Tenant Agent Administrator or Conversation Facilitator"; 448 "The authority that set the block … a Conversation Facilitator cannot clear an Administrator's block"). Fix: "two rows".

**L-I2. FR-18 table guards omit conditions the prose states.** Row 410 (`Abandoned` by an Eligible Approver) lacks the "permitted while `Disabled` or `Suspended`" clause that row 409 (`Rejected`) carries, although FR-3 170 and FR-28 673 permit abandon in both conditions. Row 415 (`PostingFailed → Abandoned` by the Tenant Agent Administrator) lacks the FR-33 458 / 423 condition "Source Conversation gone or inaccessible". Fix: add both clauses to the table.

**L-I3. Safety re-check policy stated two ways (prior 14).** `prd.md` 392, 408, 426, 615 and OQ-9 939 say the approval-time and pre-post checks "evaluate the then-current active policy"; 619 says each "uses the more restrictive of the initial attempt's policy and the then-current active policy"; Spine AD-20 217 implements the latter. Fix: qualify the five "then-current" statements with "(never weaker than the attempt's snapshot policy, FR-26)".

**L-I4. OQ-16's state diagram is narrower than FR-2.** OQ-16 946 draws `NeverJoined → Joined → (ExternallyRemoved | Blocked) → ReadmitPending → Joined`, implying `Blocked` is entered only from `Joined`; FR-2 160 lets the Tenant Agent Administrator or Facilitator block `hexa` in any Conversation (including `NeverJoined`), and 154 treats `Blocked` as a reject regardless of prior state. Fix (OQ-16): add "`Blocked` may be set from any state".

**L-I5. §13 amendment-marking convention.** OQ-16 status reads "amended 2026-09-09 (twice)" while OQ-18, which the preamble (925) also lists under both 2026-09-09 passes, reads "amended 2026-09-09" once (948); OQ-12's "clarified 2026-09-08" (942) is not in the preamble's 2026-09-08 amended list. Fix: "(twice)" on OQ-18, or drop it from OQ-16; add OQ-12 to the 2026-09-08 clause as "clarified".

**L-I6. Glossary and UJ-1 lag FR-33.** "Agent Administrator" (83) omits Conversation Context Policy and the regeneration ceiling that FR-33 444 and §10 860 assign; "Platform Operator" (109) omits per-tenant Provider/model enablement (442), cap and rate-limit configuration (449), deletion request (464), and hold-release approval (462). UJ-1 49 gives Nora "permission to manage Agent configuration and Provider settings", but Provider settings are Platform Operator only (FR-4 182, FR-33 441); UJ-1 50 itself says she selects among what the Platform Operator enabled. Fix: align the two glossary entries with the matrix; change 49 to "and to select among tenant-enabled Provider/model options".

**L-I7. Launch-health review cadence.** §12 898 says launch-health metrics are "reviewed at 30 and 60 days after enablement"; FR-28 666/677, §12 909 and OQ-22 952 add "and monthly thereafter". Fix: add the clause at 898.

## Part 2 — Cross-artifact consistency

### Confirmed intended alignments and divergences (not re-discovered)

- **Removal detected after approval (intended divergence).** PRD FR-18 row 414 and FR-2 158/161 move `Approved`/`PostingPending`/`PostingFailed` proposals to `Abandoned` (`RemovedInConversations`) on a detected removal or block when the `MessageId` lookup finds no message; Spine AD-5 127 says abandon is "never from `Approved` or `PostingPending`" and AD-7 139 says the pre-post step "records `PostingFailed` with `RemovedInConversations`". Confirmed as the PRD's deliberate second-update decision (OQ-26 956). Target: Spine AD-5/AD-7 should be amended to the PRD rule (M-X1). PRD-side change: not needed.
- **`ExpiresAt` under the kill switch.** PRD FR-18 425, FR-28 673, OQ-27 957 keep `ExpiresAt` running, as Spine AD-12 169 does ("otherwise expiring"). Consistent.
- **`hexa` provisioning.** PRD FR-1/FR-33/OQ-28 now match Spine AD-2 109 and AD-30 277 (create-only `AgentSetupMutation` under the `Platform` principal). Consistent. `epics.md` has not followed (H-X3).
- **`Unreconciled`.** PRD FR-28 667 matches Spine AD-21 223 and ARCH-A-6 742 (past hold deadline → `Unreconciled`, stays counted, settled by audited operator command or at period close at the estimated maximum). Consistent.
- **Not yet defined by the registers** (confirmed by grep, zero hits in both): `DependencyNotAvailable`, `OpenDecision`, `PayloadProtectionUnavailable`, the `MessageId` existence read on seam 2, and the SM-1/4/5/6 gate versus SM-2/3/7 launch-health split (the launch-readiness register still says SM-1..SM-6). Listed with exact wording under "Follow-ups".

### Findings

#### Critical

**C-X1. Launch-readiness register gates `RQ-1` on SM-1..SM-6 real attainment and has no SM-7 contract (prior 1, still open).** Target: `launch-readiness-register.md`.
- `launch-readiness-register.md` 79: "`LR-PRODUCT-METRICS` | Versioned SM-1 through SM-6 calculation and real rolling-window/cohort attainment"; 139: "Only current `Pass` records for the complete set … allow `RQ-1` READY"; 239: "`product-metrics` | SM-1 through SM-6 rolling-window/cohort calculations"; 266 same; `SM-7` absent.
- `prd.md` 656: gate metrics are "SM-1, SM-4, SM-5, and SM-6"; 666: "SM-2, SM-3, and SM-7 are launch-health metrics … not `RQ-1` inputs"; §12 900–913; OQ-22 952. `epics.md` 2583–2584 and 2780 already follow the PRD.
- PRD-side change: not needed. Register wording in F-LRR-2.

#### High

**H-X1. Legal-hold release and export second-party rules: Spine defers what the PRD resolved.** Target: Spine AD-22, AD-30, "Deferred Beyond V1"; `epics.md` Story 8.1.
- `ARCHITECTURE-SPINE.md` 229: "`LegalHold`, `LegalHoldRelease`, and `ExportRequest` are Compliance Inspector commands"; 753: "Two-person rule on legal-hold release | … V1 releases a hold with a single Compliance Inspector command carrying typed justification"; 277: "every other family rejects a `Platform` principal" (so the Platform Operator cannot approve a release). `epics.md` 2350: "Given a Compliance Inspector releases an active hold at its expected revision / When every named DEK is unpinned successfully / Then `LegalHold` appends a release transition" — single actor.
- `prd.md` FR-33 462: "Release legal hold (FR-30) | Compliance Inspector, with the audited approval of a second Compliance Inspector or the Platform Operator"; 463: "Request authorized export … with prior approval by a second party on the FR-24 terms; never post hoc"; FR-30 597: "Export requires prior second-party approval and hold release a separate approver"; OQ-30 960 (Resolved 2026-09-09).
- PRD-side change: not needed. Spine and epics should adopt the two-party rule and allow `Platform` on `LegalHoldRelease` as approver.

**H-X2. Compliance-inspection second party: Spine names only the Tenant Agent Administrator and an unspecified window.** Target: Spine AD-22.
- `ARCHITECTURE-SPINE.md` 229: inspection is "either pre-approved by a distinct Tenant Agent Administrator principal before any content read or post-hoc reviewed by one within the tenant's review window, rate-visible on the `audit-evidence` projection the Tenant Agent Administrator can read".
- `prd.md` 568: second party is "the Tenant Agent Administrator when the case does not concern that Administrator's configuration or conduct, and otherwise the Platform Operator or a second Compliance Inspector. Post-hoc review … is permitted only for an inspection scoped to a single proposal or a single Conversation, and must be completed within 7 days of the access [ASSUMPTION A-19]"; the audit surface is readable by "the Tenant Agent Administrator and the Platform Operator". FR-33 457, OQ-21 951, OQ-30 960 agree.
- PRD-side change: not needed.

**H-X3. `epics.md` requirements inventory contradicts the current PRD (prior 3, still open, wider).** Target: `epics.md`.
- `epics.md` 36: "FR1: Agent Administrators can create or enable `hexa`" vs `prd.md` 132 (Platform Operator provisions; tenant provision attempt is a typed denial, 136).
- 48: "FR7: … including Conversation owner, caller, predefined Parties, or tenant roles" vs 227 (Facilitator, predefined Parties, tenant roles; caller retired; no owner resolved). Story 5.4 AC 1353 still reads "Given caller, predefined Party, tenant-role, or Conversation Facilitator approver sources"; 131 likewise names "current caller".
- 70: "FR18: … rejected, abandoned, or expired terminal states … executed by a Dapr Workflow timer at or after stored `ExpiresAt`" vs 397 (four terminal states incl. `Posted`; OQ-3 933 makes the mechanism an architecture concern).
- 76: FR21 row stops at the `ready-for-dev` rule; PRD 506–507 add the `Available`/`DependencyNotAvailable`/`PayloadProtectionUnavailable` fail-closed rules.
- 86: "FR26: Authorized administrators or release operators can define and publish the versioned Content Safety Policy" vs 609 (Platform Operator with Security approval; tenant may only add restrictions).
- No inventory or epic-map row for FR-29..FR-34; no reference to OQ-24..OQ-30. §0 18 makes the epic set "the single executable backlog".
- PRD-side change: not needed.

#### Medium

**M-X1. (Confirmed intended) Spine AD-5/AD-7 still say removal after approval is `PostingFailed`, never `Abandoned`.** Target: Spine. Quotes above under "Confirmed intended…". Also `epics.md` 1923 OwnedClause "FR18.external-removal-abandons-nonterminal-proposals" is already on the PRD side. PRD-side change: not needed.

**M-X2. Block clearing authority and the five-state Conversation Agent State.** Target: Spine AD-2, AD-7; `epics.md` Story 6.6.
- `ARCHITECTURE-SPINE.md` 139: the block "is set by the Tenant Agent Administrator, the Conversation Facilitator [A-9], or the membership step on detecting an external removal, is cleared only by the first two"; 109: `ConversationAgentState` holds "membership-established fact, the Agents-owned block, and the index of non-terminal proposals". `epics.md` 1901: "only a Tenant Agent Administrator or Conversation Facilitator can clear the block and begin explicit re-admission".
- `prd.md` 160: "A block is cleared only by the authority that set it or by the Tenant Agent Administrator; a Conversation Facilitator cannot clear a block the Tenant Agent Administrator set. Clearing records `ReadmitPending`, and `hexa` re-joins at the next Accepted Agent Call there, never at the moment of clearing"; §3 97 names five states; FR-25 585 exposes `MirrorPending`. `ReadmitPending`, `MirrorPending`, `ExternallyRemoved` have zero hits in the Spine and in `epics.md`.
- PRD-side change: needed only for the M-I2 clarification; otherwise Spine/epics should adopt the PRD rule and the five-state record.

**M-X3. `MembershipUnavailable` at acceptance.** Target: Spine AD-7 (and PRD 158 per M-I1).
- `ARCHITECTURE-SPINE.md` 139: "`MembershipUnavailable` and `MembershipRejected` are `PostingFailed` reasons produced only by this re-validation, never by acceptance".
- `prd.md` 155: at acceptance, when the participant read is unavailable, "reject the call with the typed reason `MembershipUnavailable`, change no state, and abandon nothing".
- PRD-side change: needed (reword 158 so the acceptance-time use is explicit).

**M-X4. PRD misstates the Spine's assumption range.** Target: `prd.md` §8.1 811.
- `prd.md` 811: the Spine "(frontmatter `updated: 2026-09-09`, at which version the table holds `ARCH-A-1` through `ARCH-A-7`)".
- `ARCHITECTURE-SPINE.md` 737–746 holds `ARCH-A-1` through `ARCH-A-10` (ARCH-A-5 retired; ARCH-A-8 Fluent UI RC pin, ARCH-A-9 `SecurityEventLog` volume, ARCH-A-10 posted-copy retention) at `updated: 2026-09-09` (10).
- The "range is open" clause keeps FR-28 item 9 correct, but the count is false. PRD-side change: needed — "holds `ARCH-A-1` through `ARCH-A-10`, `ARCH-A-5` retired".

**M-X5. Spine binds and vocabulary lag the second PRD update.** Target: Spine.
- `ARCHITECTURE-SPINE.md` 12–14: "binds: PRD FR-1..FR-33 / NFR-1..NFR-14 / OQ-1..OQ-23". PRD has FR-34 (695) and OQ-24..OQ-30 (954–960). Zero hits in the Spine for `FR-34`, `OQ-24`..`OQ-30`, `PayloadProtectionUnavailable`, `DependencyNotAvailable`, `OpenDecision`, `LateConfirmed`, `ResolutionUnavailable`, `MirrorPending`, `ReadmitPending`, `Suspended`. AD-14 181 states the FR-34 rule without the blocker name; AD-17 199 names `UnretiredAssumption` only; AD-5 127 has no `MessageId` lookup before an exit from `PostingFailed` (PRD 424, OQ-26).
- PRD-side change: not needed.

**M-X6. Kill-switch trigger: Spine lacks the mandatory review, minimum sample, and numerator definitions.** Target: Spine AD-12.
- `ARCHITECTURE-SPINE.md` 169: pulled "by the Release Operator only on the recorded triggers (blocked-call share above 50 percent or posting failures above 10 percent sustained seven days)".
- `prd.md` 675: "A trigger review, mandatory when, over a rolling seven-day window that contains at least 5 distinct calling Parties and at least 50 Agent Calls, either the blocked-call share (SM-C4) exceeds 50% — counted over distinct (Party, Conversation, reason) tuples, with rate-limit rejections and repeat rejections … excluded — or the posting-failure rate exceeds 10%, that rate being the share of proposals that entered `PostingFailed` at least once … over proposals that entered `Approved` … Below the minimum sample the trigger evaluates to `InsufficientEvidence` … The Release Operator pulls the switch only as the recorded decision of that review [A-17]"; OQ-27 957.
- PRD-side change: not needed.

**M-X7. Register consumers for `EXT-CONV-AI-1` (prior 9).** Target: `external-dependency-register.md` 61 ("6.6, 7.4; `RQ-1`"). `prd.md` 796: seam 5 is what "every Agent Call, the FR-7 resolution, and the FR-18 pre-post re-validation depend on"; 795: seam 4 feeds SM-2. `epics.md` 1677 (Story 6.2 needs the "supported Conversations read seam"), 7.1 (proposal creation resolves Approvers), 8.5 (SM-2 denominator). PRD-side change: not needed. Suggested value: "6.1, 6.2, 6.6, 7.1, 7.4, 8.5; `RQ-1`".

**M-X8. Non-conformance record (prior 8).** Target: `external-dependency-register.md`. `prd.md` 789: "any story already completed against an `Uncommitted` entry carries a non-conformance record in the register"; FR-21 504; OQ-17 947. Register: no such section; 107 narrows Story 5.3 out of `EXT-PROVIDER-1` instead. PRD-side change: not needed unless Product rules 5.3 never consumed the seam, in which case 789 should say "none is currently recorded".

**M-X9. Platform-scoped readiness observations cannot be recorded by the role the PRD names.** Target: `prd.md` FR-33 452 (small), or the launch-readiness register.
- `prd.md` 452: "Record cost-control posture and launch readiness; record the `RQ-1` READY or NOT READY decision …; enable production-like generation and production for a tenant | Release Operator | Tenant"; FR-34 704: "the Release Operator owns the readiness record".
- `launch-readiness-register.md` 85–101: `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-SECRETS`, `LR-RECOVERY`, `LR-CAPACITY-FAIRNESS`, `LR-UI-CONFORMANCE`, `LR-RUNTIME-PERFORMANCE`, `LR-UI-PERFORMANCE` have `AuthorizedProducer` "`Platform`" only; Spine AD-30 277: "Platform-kind gates accept only from `Platform`".
- Eight of the 18 gates a Release Operator must see `Pass` can be produced only by the Platform Operator, whom FR-33 never grants a readiness row. PRD-side change: needed — add to row 452 "Platform-scoped gate observations are recorded by the Platform Operator (launch readiness register); the `RQ-1` decision remains the Release Operator's".

#### Low

**L-X1. Spine AD-15 counter list omits rate limits.** `ARCHITECTURE-SPINE.md` 187: "calls blocked by context policy, safety, cost cap, `NoEligibleApprover`, `RemovedInConversations` or the block" vs `prd.md` 920 which includes "rate limits" (FR-28 675 excludes them from the trigger share only). Target: Spine. PRD-side change: not needed.

**L-X2. `MaxConcurrentNonterminalInteractionsPerParty` is a consumption bound absent from the PRD.** `ARCHITECTURE-SPINE.md` 223: `AdmitCall` "enforces the per-Party and per-Conversation windows and the `MaxConcurrentNonterminalInteractionsPerParty` set (a `TenantGovernancePolicy` field with no implicit default)". `prd.md` FR-32 682–693, FR-28 item 8 (662), FR-33 449 name only caps, rate limits, and the regeneration ceiling; §8.1 811 says a Spine rule defining an outcome absent from FR-28 is "a PRD amendment pending". An unconfigured bound with no default blocks acceptance without an `RQ-1` input naming it. Target: PRD FR-32 (add the bound, one clause) — PRD-side change: needed — or the Spine reclassifies it under NFR-12 capacity.

**L-X3. `MessageId` existence read and `LateConfirmed` (known follow-up).** `prd.md` 793 (seam 2 "together with an existence read by `MessageId`"), 424, OQ-26 956. `external-dependency-register.md` 55 seam (2) and Spine AD-6 133 name posting only; `LateConfirmed` has zero hits in the register, the Spine, and `epics.md`. Target: register, Spine AD-5/AD-6, epics Story 7.4/7.5. PRD-side change: not needed.

**L-X4. `epics.md` UX-DR45 (246) still says launch readiness shows "SM-2/SM-3 cohorts/windows"** as gate content; under the PRD split those are launch-health. (Line 1112 "Given SM-2 or SM-3 is used for launch readiness" is in a superseded epic and is history.) Target: epics. PRD-side change: not needed.

## Follow-ups for other artifacts (exact wording the artifact needs)

**F-LRR-1 — `launch-readiness-register.md`, blocker vocabulary (add under "State, Freshness, And Invalidation" after line 48):**
"`BlockerCode` values emitted by the `RQ-1` evaluation itself, in addition to gate-level codes: `DependencyNotAvailable` (an external dependency in the qualification profile is not `Available`, or its compatibility command did not pass against the exact target; names the `EXT-*` entry); `UnretiredAssumption` (an unretired PRD §8.1 `A-n` row or Architecture Spine `ARCH-A-n` row whose owner includes Product, Architecture, or Governance; names the row and its table); `OpenDecision` (a *Deferred* PRD §13 row whose status says 'before enablement' has not landed; names the row); `ProhibitedCostControlPosture` (a recorded posture is `ReportingOnlyMonitoring` or `AcceptedLaunchRisk`); `PayloadProtectionUnavailable` (the host reports no production payload-protection engine; emitted alongside `DependencyNotAvailable` naming `EXT-PROTECTION-1`). `RQ-1` records NOT READY whenever any of these stands."

**F-LRR-2 — `launch-readiness-register.md` 79 and 239:**
79: "`LR-PRODUCT-METRICS` | Versioned calculation of the pre-enablement gate metrics SM-1, SM-4, SM-5, and SM-6 and their attainment on the qualification cohort (gate inputs), plus versioned calculation and reporting of the launch-health metrics SM-2, SM-3, SM-7 and counter-metrics SM-C1..SM-C5, which are never `RQ-1` inputs and are reviewed at 30 and 60 days after enablement and monthly thereafter; deterministic fixtures prove formulas only."
239: "`product-metrics` | SM-1/SM-4/SM-5/SM-6 gate calculations, SM-2/SM-3/SM-7 launch-health rolling-window/cohort calculations, SM-C1..SM-C5, and insufficiency state." Add an SM-7 measurement contract (numerator: proposals edited, regenerated, or rejected before a human decision; denominator: proposals reaching a human decision in the window; band 10–60%; `InsufficientEvidence` rules) beside the SM-3 contract.

**F-LRR-3 — `launch-readiness-register.md` 17, append:** "`RQ-1` additionally records NOT READY when any `UnretiredAssumption`, `OpenDecision`, `DependencyNotAvailable`, `ProhibitedCostControlPosture`, or `PayloadProtectionUnavailable` blocker stands (PRD FR-28 input list)."

**F-REG-1 — `external-dependency-register.md` 55, seam (2), append:** "…so restart or replay cannot duplicate a post; plus an existence read by `MessageId` returning present/absent/unavailable, so Agents can confirm whether a post whose acknowledgement was lost landed before it retries or abandons (PRD FR-18, `LateConfirmed`)." Mirror in 58 ("…idempotent posting with trace and provenance metadata and a `MessageId` existence read…").

**F-REG-2 — `external-dependency-register.md`, under "Accepted Status Semantics" after line 45:** "A runtime, test, or qualification path that would execute a seam whose record is not `Available` fails closed with the typed outcome `DependencyNotAvailable` naming the record (PRD FR-21); `RQ-1` records the same code for every record in its qualification profile that is not `Available` (PRD FR-28)."

**F-REG-3 — `external-dependency-register.md`, new section "Non-Conformance Records":** one row `Story 5.3 | EXT-PROVIDER-1 | <completion date> | completed while Uncommitted; reopened by sprint-change-proposal-2026-09-09; reopening does not clear this record (PRD FR-21)`, or an explicit "None: Story 5.3 executed no `EXT-PROVIDER-1` seam (Product ruling <date>)".

**F-REG-4 — `external-dependency-register.md` 61:** "`ConsumingStories` | 6.1, 6.2, 6.6, 7.1, 7.4, 8.5; `RQ-1`".

**F-SPINE-1 — frontmatter 12–14:** "PRD FR-1..FR-34", "PRD OQ-1..OQ-30".

**F-SPINE-2 — AD-5 127:** replace "Abandon is legal from every awaiting-decision state and from `PostingFailed`, never from `Approved` or `PostingPending`, which complete or fail on their own terms" with "Human abandon is legal from every awaiting-decision state and from `PostingFailed`; system abandon with reason `RemovedInConversations` is additionally legal from `Approved`, `PostingPending`, and `PostingFailed` when a removal or block is detected and the `MessageId` existence read finds no posted message (PRD FR-18); `Approved` and `PostingPending` otherwise complete or fail on their own terms. Before every retry and every exit from `PostingFailed` the orchestrator runs the seam-2 existence read; a present message moves the proposal to `Posted` with `LateConfirmed`, and an unavailable read refuses the exit." AD-7 139: replace "records `PostingFailed` with `RemovedInConversations`" with "moves the proposal to `Abandoned` with `RemovedInConversations` (PRD FR-2, FR-18)"; replace "never by acceptance" with "`MembershipUnavailable` is also the acceptance-time rejection when the participant read is unavailable (PRD FR-2)"; replace "is cleared only by the first two" with "is cleared only by the authority that set it or by the Tenant Agent Administrator (a Facilitator cannot clear an Administrator's block); clearing records `ReadmitPending` and re-join occurs at the next Accepted Agent Call (PRD FR-2, OQ-25)". AD-2 109: `ConversationAgentState` "with exactly the five states `NeverJoined`, `Joined`, `ExternallyRemoved`, `Blocked`, `ReadmitPending`, a `MirrorPending` flag for the removal-seam outbox, and the index of non-terminal proposals".

**F-SPINE-3 — AD-22 229:** replace the inspection clause with the PRD 568 rule (Tenant Agent Administrator when the case does not concern that Administrator, otherwise Platform Operator or second Compliance Inspector; post-hoc only for single-proposal or single-Conversation scope within 7 days [A-19]; rate surface readable by the Tenant Agent Administrator and the Platform Operator). Replace "`LegalHold`, `LegalHoldRelease`, and `ExportRequest` are Compliance Inspector commands" with "`LegalHold` is a Compliance Inspector command; `LegalHoldRelease` requires the audited approval of a second Compliance Inspector or the Platform Operator; `ExportRequest` requires prior second-party approval on the FR-24 terms, never post hoc (PRD FR-33, OQ-30)". Delete the "Two-person rule on legal-hold release" row from "Deferred Beyond V1" (753). AD-30 277: add `LegalHoldRelease` (as approver) to the tenant rows a `Platform` principal may dispatch.

**F-SPINE-4 — AD-12 169:** replace "(blocked-call share above 50 percent or posting failures above 10 percent sustained seven days)" with "as the recorded decision of the mandatory trigger review PRD FR-28 defines (rolling seven-day window with at least 5 distinct calling Parties and 50 Agent Calls; SM-C4 share above 50 percent over distinct (Party, Conversation, reason) tuples with rate-limit and repeat rejections excluded, or posting-failure rate above 10 percent; `InsufficientEvidence` below the sample) [ASSUMPTION A-17]". AD-15 187: add "rate limits" to the blocked-call counter list. AD-14 181: name `PayloadProtectionUnavailable` as the typed outcome and readiness blocker (PRD FR-34). AD-17 199: "…records `UnretiredAssumption`, `OpenDecision`, and `DependencyNotAvailable` blockers per PRD FR-28". `agent-setup` (AD-12/AD-15): expose `Suspended` while the kill switch is pulled (PRD FR-3, FR-25).

**F-EPICS-1 — `epics.md` inventory:** rewrite rows FR1 (36), FR7 (48), FR18 (70), FR21 (76), FR26 (86) from `prd.md` 132, 227, 397–426, 495–507, 609–619; add FR29–FR34 rows and epic-map entries (FR-29 → Epic 5; FR-30 → Epic 8; FR-31 → Epic 6; FR-32 → Epic 8; FR-33 → Epics 5–8; FR-34 → Epics 5 and 8); reference OQ-24..OQ-30 where stories cite the decision register. Story 5.4 AC 1353: drop "caller". Story 6.6 AC 1901: adopt the PRD clearing rule and `ReadmitPending`/`MirrorPending`. Story 8.1 AC 2350: add the second-approver step. Story 7.4/7.5: add the `MessageId` existence read and `LateConfirmed` before any retry or exit from `PostingFailed`. UX-DR45 (246): "SM-1/SM-4/SM-5/SM-6 gate results; SM-2/SM-3/SM-7 launch-health results reported, not gated".

**F-UPD-1 — `update-report-2026-09-09.md` (prior 17):** add a line recording that the 2026-09-09 sprint change proposal's two PRD amendments (`EXT-PROTECTION-1` in §8; `ARCH-A` cross-index in §8.1) were applied, and that the second update aligned FR-1/FR-33 to Spine AD-2/AD-30 (OQ-28) and brought `Unreconciled` into FR-28 (ARCH-A-6).

## Summary

Prior findings: 9 resolved, 7 still open (one out of scope). New review: internal 0 Critical / 1 High / 2 Medium / 7 Low; cross-artifact 1 Critical / 3 High / 9 Medium / 4 Low. PRD-side changes needed: H-I1, M-I1, M-I2, L-I1..L-I7, M-X3 (line 158), M-X4, M-X9, L-X2. Other artifacts to change: `launch-readiness-register.md` (C-X1, F-LRR-1..3), `ARCHITECTURE-SPINE.md` (H-X1, H-X2, M-X1, M-X2, M-X3, M-X5, M-X6, L-X1, L-X3), `epics.md` (H-X3, M-X2, L-X3, L-X4), `external-dependency-register.md` (M-X7, M-X8, L-X3, F-REG-1..4), `update-report-2026-09-09.md` (F-UPD-1).
