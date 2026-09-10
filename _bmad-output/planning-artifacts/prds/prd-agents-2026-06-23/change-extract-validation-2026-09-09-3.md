# Change Extract — validation-report.md (run 2026-09-09T11:01Z, grade Poor)

Source: `validation-report.md` in this folder (8 critical / 24 high / 46 medium / 40 low), with the reviewer files `review-rubric.md`, `review-adversarial-general.md`, `review-consistency.md`, `review-implementation-drift.md`. Target: `prd.md` (1000 lines, line numbers below are current) and `addendum.md` (52 lines). Every `Current text` quote is verbatim from the file on disk at extraction time, for exact-match replacement. On-disk facts cited by the PRD-resync entries were re-verified against `external-dependency-register.md` (line 212: all nine records `Uncommitted`; line 57/60/65: seam-2 existence read and seam-4 feed present), `ARCHITECTURE-SPINE.md` (frontmatter `updated: 2026-09-09`, `architecture_assumption_index_version: 3`, `ARCH-A-INDEX-3`, rows through `ARCH-A-12`, nine `TBD — target milestone` dates; AD-13 line 201 still says "Eligible Approver resolution runs in every response mode"), the Spine `.memlog.md` (line 189 flags §8.1 as stale), and `sprint-change-proposal-2026-09-09-2.md` (`amends_if_approved` lines 30–35 omit `prd.md`; `preserves: prd.md without amendment`).

New identifiers proposed below (provisional, renumber on apply): `A-22` (AH-3), `A-23` (AH-8), `A-24` (AM-15), `A-25` (AL-5), `OQ-31` (AM-9), blocker/marker names `DeferredAssumption` (AC-1), `PostingWindowElapsed` (AH-4), `SuspensionReviewOverdue` (AM-2), `MirrorRefused` (AM-7), `SafetyFailed` proposal marker (AM-13), `ContextUnavailable` as a typed FR-9 reason (CI-7).

## Summary: counts per severity × target class

| Severity | PRD | PRD-resync | MIXED | OTHER-ARTIFACT | CODE | Total |
| --- | --- | --- | --- | --- | --- | --- |
| Critical | 1 (AC-1) | 0 | 1 (DC-2) | 1 (CX-1) | 5 (DC-1, DC-3..DC-6) | 8 |
| High | 7 (AH-1..AH-6, AH-8) | 6 (AH-9, CX-2, CX-3, CX-4, CX-5, Rubric-DR-HOST) | 1 (AH-7) | 1 (CX-6) | 9 (DH-1..DH-9) | 24 |
| Medium | 30 (AM-1..AM-10, AM-12..AM-15, CI-1..CI-10, Rubric ×6) | 1 (Rubric-DU-8.1) | 4 (AM-11, CX-7, DM-3, DM-7) | 5 (CX-8..CX-12) | 6 (DM-1, DM-2, DM-4..DM-6, DM-8) | 46 |
| Low | 29 (AL-1..AL-13, CI-11..CI-18, Rubric ×7, DL-1) | 2 (Rubric-SH-seams, Rubric-DU-count) | 0 | 7 (CX-13..CX-19) | 2 (DL-2, DL-3) | 40 |
| Low, extra (Mechanical notes) | 4 (MN-2, MN-3, MN-4, MN-6) | 2 (MN-1, MN-5) | 0 | 0 | 0 | 6 |
| **Total** | **71** | **11** | **6** | **14** | **22** | **124** |

Numbered PRD-touching edits (PRD + PRD-resync + PRD part of MIXED): 88. Decisions needed: 31 (listed at the end).

---

## Critical

### AC-1 — Critical — `RQ-1` fails open on a late `ARCH-A` row (Release PM acknowledgement is opt-in)
- Target: PRD
- Location: FR-28 item 9, line 688; §8.1 line 838
- Current text (line 688): "An `ARCH-A` row added after the `RQ-1` evaluation is scheduled blocks only once the Release PM records an acknowledgement of it, and every Architecture-owned row carries a target retirement date."
- Proposed text (line 688): "An `ARCH-A` row added after the `RQ-1` evaluation is scheduled blocks `RQ-1` unless the Release Operator records a deferral naming the row, its owner, the FR consequences the run will not exercise, and a revisit date; the deferral is recorded in the launch readiness register and is visible on the FR-30 surface as `DeferredAssumption`. Every Architecture-owned row carries a target retirement date; a row still unretired when that date passes escalates to Product for a recorded keep-or-retire decision, is reported on the FR-30 surface and at the next launch-health review, and keeps blocking `RQ-1` until it is retired or deferred (§8.1)."
- Current text (line 838): "An `ARCH-A` row whose retirement condition names Product, Governance, Security, or the Release PM is retired only by a recorded confirmation from that party, never by an Architecture edit alone; a row added after the `RQ-1` evaluation is scheduled blocks only once the Release PM records an acknowledgement, and every Architecture-owned row carries a target retirement date."
- Proposed text (line 838): "An `ARCH-A` row whose retirement condition names Product, Governance, Security, or the Release PM is retired only by a recorded confirmation from that party, never by an Architecture edit alone. A row added after the `RQ-1` evaluation is scheduled blocks `RQ-1` unless the Release Operator records a `DeferredAssumption` deferral naming the row, its owner, the FR consequences not exercised, and a revisit date (FR-28). Every Architecture-owned row carries a target retirement date — a literal date, not a milestone — and a passed date escalates the row to Product for a recorded keep-or-retire decision; Architecture is bounded by the date, the Release Operator is never unbounded by the calendar."
- Decision needed: yes — reverses the Gate H-6 decision of 2026-09-09 (`.memlog.md`: "late-added rows block on Release PM acknowledgement"); the actor name depends on the Release PM decision (CI-9 / Rubric-DR-PM).
- Co-located with: CX-4, CX-19, Rubric-DR-PM, CI-9, Rubric-SH-undated (lines 688, 838); FR-33 rows and FR-30 line 621 (`DeferredAssumption` exposure).

### CX-1 — Critical — Spine AD-13 requires Eligible Approver resolution and `NoEligibleApprover` in Automatic Response Mode
- Target: OTHER-ARTIFACT — `ARCHITECTURE-SPINE.md` AD-13 (line 201) and sequence diagram (line 408): limit Eligible Approver resolution to Confirmation Response Mode per PRD FR-8 step 8 / FR-7 / UJ-2. No PRD change (but see Rubric-DU-8.1 for how §8.1 records this divergence — decision).

### DC-1 — Critical — Readiness recording still accepts `ReportingOnlyMonitoring` / `AcceptedLaunchRisk`
- Target: CODE — `AgentAggregate.cs`, `AgentLaunchReadinessPolicy.cs`, `AgentLaunchReadinessBlocker.cs`, `CostControlPosture.cs`: reject both postures at `RecordAgentLaunchReadiness`, add `ProhibitedCostControlPosture`, emit it for a stored posture. No PRD change.

### DC-2 — Critical — FR-34 has no blocker, attestation, or fail-closed path; §0 describes a mechanism that does not exist
- Target: MIXED — CODE: additive `PayloadProtectionUnavailable` blocker, host-supplied attestation port, typed outcome on every FR-21 content-bearing path. PRD part: §0 status line.
- Location: §0 line 16 (whole status line is rewritten together with CX-2 / Rubric-DR-HOST / AH-9 / MN-5)
- Current text: "Status as of 2026-09-09: all critical external dependencies except `EXT-HOST-1` are `Uncommitted` — eight of the nine register entries (§8) — and none is `Available`, so live content-bearing execution stays disabled under FR-34. Every unretired assumption — the `A-n` rows in §8.1 and the `ARCH-A-n` rows in the Architecture Spine's own table — blocks `RQ-1` (FR-28). No consuming story is `ready-for-dev`."
- Proposed text: "Status as of 2026-09-09: at this revision every §8 register entry is `Uncommitted` and none is `Available`; the external dependency register is authoritative for current status and this PRD carries no count. `EXT-HOST-1`, previously `Committed`, returned to `Uncommitted` on 2026-09-09 when FR-34 added the production protection binding and attestation port to its required artifact, under the §8 rule that a changed artifact invalidates prior acceptance. Live content-bearing execution is disabled at this revision because no Provider adapter is bound in the host; FR-34 states the mechanism that must keep it disabled once one is. Every unretired assumption — the `A-n` rows in §8.1 and the `ARCH-A-n` rows in the Architecture Spine's own table — blocks `RQ-1` (FR-28). No consuming story is `ready-for-dev`."
- Decision needed: none.
- Co-located with: CX-2, Rubric-DR-HOST, AH-9, MN-5 (line 16).

### DC-3 — Critical — Approval enforces no segregation of duties
- Target: CODE — `AgentInteractionProposalApprovalOrchestrator.cs`, `AgentInteractionProposalApprovalRequest.cs`: carry caller and last-editor identities, resolve the Conversation-scoped Eligible Approver set, refuse the caller or the editor of the selected version. No PRD change.

### DC-4 — Critical — No approval-time or pre-post safety re-check; edited versions never scanned
- Target: CODE — approval and edit orchestrators: scan at edit time, re-scan the exact selected version at approval and immediately before append under the then-current policy. No PRD change (PRD-side edit-time scan wording is AM-13).

### DC-5 — Critical — No pre-Provider scan of prompt and Conversation Context
- Target: CODE — `AgentInteractionGenerationOrchestrator.cs`, regeneration orchestrator: add the pre-Provider scan before `InvokeProviderAsync`; keyed-hash cache as follow-on. No PRD change.

### DC-6 — Critical — No cost-cap / rate-limit / concurrency readiness blockers; no reservation step
- Target: CODE — `AgentLaunchReadinessPolicy.cs`, `AgentInteractionGateCheck.cs`, gate orchestrator: cap and rate-limit configuration, readiness blockers, FR-8 step-4/step-6 gate checks. No PRD change.

---

## High

### AH-1 — High — FR-2 says link/replace commands are on the FR-23 register; FR-23's register omits them
- Target: PRD
- Location: FR-23 line 557 (plus new consequence after it)
- Current text: "The V1 deprecate-and-reject register is: `CostControlPosture.ReportingOnlyMonitoring` and `CostControlPosture.AcceptedLaunchRisk` (FR-28, OQ-6), `ContentSafetyFailureHandling.BlockWithAuditableOverride` (FR-27; Approvers cannot override a safety failure), and `ApproverPolicySourceKind.Caller` (FR-7; the caller is never an Eligible Approver of their own call). Each remains declared and deserializable, is rejected server-side with a typed rejection wherever it is presented, and is listed here so that no consumer treats it as accepted behavior."
- Proposed text: "The V1 deprecate-and-reject register is: `CostControlPosture.ReportingOnlyMonitoring` and `CostControlPosture.AcceptedLaunchRisk` (FR-28, OQ-6), `ContentSafetyFailureHandling.BlockWithAuditableOverride` (FR-27; Approvers cannot override a safety failure), `ApproverPolicySourceKind.Caller` (FR-7; the caller is never an Eligible Approver of their own call), and the Agent Party link and replace commands (FR-2, Spine AD-7; provisioning is the only identity path, and no principal may re-point `hexa`'s Party identity). Each remains declared and deserializable, is rejected server-side with a typed rejection wherever it is presented, and is listed here so that no consumer treats it as accepted behavior."
- ADD after line 557 (new bullet): "- A public command or value that any FR names as deprecate-and-reject is on this register; the register and those FRs are reconciled in the same change, so the register is never narrower than the FRs that cite it."
- Decision needed: none.
- Co-located with: CI-2 (same sentence); Rubric-DN-AgentCall (adds `Proposal*` members to the same register sentence).

### AH-2 — High — `Joined` + absent + "clear is newer" names no outcome and never self-heals; predicate undefined when no mirror exists
- Target: PRD
- Location: FR-2 line 154 (last sentence) and line 158 (first sentence)
- Current text (line 154, last sentence): "An absence observed while a mirror is unconfirmed or a clear is newer is the mirror's late effect, not an external removal; the state is left as it is."
- Proposed text: "A Conversation with no mirror history satisfies "the last mirror is confirmed", so an external removal is detected in a never-blocked Conversation on these terms. `Joined`, the participant read confirms `hexa` is absent, and a clear is newer than the last mirror: the absence is the cleared mirror's late effect, not a removal; the step re-adds `hexa` idempotently under the standing admission (the clear, or the original join), stays `Joined`, and accepts; if Conversations refuses the add, the call is rejected with the typed reason `MembershipRejected` and the state is left as it is."
- Current text (line 158, first sentence): "A membership step that is only an idempotent join does not satisfy this requirement, because it would re-admit an Agent that a Facilitator removed; a step that treats absence after a cleared block as a fresh removal does not satisfy it either, because it could never re-admit `hexa`."
- Proposed text: "Negative tests: a step that re-admits `hexa` from `Joined` when the last mirror is confirmed and no clear is newer does not satisfy this requirement, because it would re-admit an Agent that a Facilitator removed; a step that records `ExternallyRemoved` when a clear is newer than the last mirror, or that fails to detect an external removal in a Conversation with no mirror history, does not satisfy it either."
- Decision needed: none.
- Co-located with: MN-3 (line 154), AL-11 (line 158), OQ-16 line 986 ("External removal is detected only from `Joined` with the last mirror confirmed and no newer clear" — append "; a Conversation with no mirror history counts as confirmed").

### AH-3 — High — `ExternallyRemoved` auto re-admission trusts whoever can add a participant; no assumption records who that is
- Target: PRD
- Location: FR-2 line 156 (second sentence); §8.1 new row `A-22`; §8 seam 1 line 817; OQ-16 line 986
- Current text (line 156, second sentence): "`ExternallyRemoved` and the read confirms `hexa` is present again: record `Joined` and accept, because re-adding `hexa` in Conversations is the removal authority's re-admission."
- Proposed text, option (a): "`ExternallyRemoved` and the read confirms `hexa` is present again: record `Joined` and accept, because Conversations restricts adding an `AiAgent` participant to `ParticipantRole.Facilitator` or the Conversation's administrative role, so presence again is the removal authority's re-admission [ASSUMPTION A-22]. Until A-22 is retired, presence again does not re-admit: the state stays `ExternallyRemoved`, the call is rejected on the same terms as `Blocked`, and only the FR-33 clear row re-admits."
- Proposed text, option (b): "`ExternallyRemoved` and the read confirms `hexa` is present again: the state stays `ExternallyRemoved` and the call is rejected on the same terms as `Blocked`; only the FR-33 clear row re-admits `hexa`, so a removal performed in Conversations has the same protection as a block set in Agents."
- ADD (option (a)) §8.1 row after A-21: "| A-22 | Conversations restricts adding an `AiAgent` participant to `ParticipantRole.Facilitator` or the Conversation's administrative role | FR-2, §8 | Conversations Maintainer | `EXT-CONV-AI-1` reaches `Committed` with that rule (register `TargetIntegrationDate`) |"
- Also align OQ-16 line 986 "or from `ExternallyRemoved` when `hexa` is found present again" (delete under (b); add "(A-22)" under (a)).
- Decision needed: yes — choose (a) assumption-gated auto re-admission or (b) clear-only; amends the Gate-mediums decision M-1/M-I2 of 2026-09-09 ("presence again re-admits").
- Co-located with: AH-2 (line 154–156 block), OQ-16, OQ-25.

### AH-4 — High — `Approved` has no human exit and no clock; `PostingFailed` never expires; administrative retry has no age bound
- Target: PRD
- Location: FR-18 table row line 431; line 438; line 435; row 433 guard; FR-33 row line 477; SM-3 line 947
- Current text (row 431): "| `PostingFailed` | `Abandoned` | Eligible Approver; Tenant Agent Administrator (FR-33, only when the Source Conversation is gone or inaccessible) | The `MessageId` lookup finds no posted message; permitted while `Disabled` or `Suspended` |"
- Proposed text: "| `Approved`, `PostingFailed` | `Abandoned` | Eligible Approver; Tenant Agent Administrator (FR-33, audited; for `PostingFailed` only when the Source Conversation is gone or inaccessible) | The `MessageId` lookup finds no posted message, skipped when no post was attempted; permitted while `Disabled` or `Suspended` |"
- Current text (line 438, last sentence): "Approval freezes expiry: `Approved`, `PostingPending`, and `PostingFailed` proposals never expire."
- Proposed text: "Approval freezes `ExpiresAt`: `Approved`, `PostingPending`, and `PostingFailed` proposals never reach `Expired`. A proposal that has been in `Approved` or `PostingFailed` for longer than its original expiry duration in total, excluding time paused under FR-3 and FR-28, is system-abandoned with reason `PostingWindowElapsed` once the `MessageId` lookup finds no posted message; it counts as decided for SM-3 (§12). An administrative retry re-validates against this bound and is refused with a typed reason once it has elapsed."
- Current text (line 435, opening): "A proposal abandoned by the system carries its typed reason (`NoEligibleApprover`, `SourceConversationUnavailable`, or `RemovedInConversations`) in Audit Evidence and in status,"
- Proposed text: "A proposal abandoned by the system carries its typed reason (`NoEligibleApprover`, `SourceConversationUnavailable`, `RemovedInConversations`, or `PostingWindowElapsed`) in Audit Evidence and in status,"
- Row 433 guard: append "; the `PostingWindowElapsed` bound has not elapsed" (see CI-3 for the same cell).
- FR-33 row 477 current: "| Abandon a `PostingFailed` proposal whose Source Conversation is gone or inaccessible (FR-18) | Tenant Agent Administrator; audited; no Conversation read access required | Tenant |" → proposed: "| Abandon an `Approved` proposal, or a `PostingFailed` proposal whose Source Conversation is gone or inaccessible (FR-18) | Tenant Agent Administrator; audited; no Conversation read access required | Tenant |"
- SM-3 line 947: "except that a system abandonment from `Approved` or `PostingFailed` follows a human decision and counts as decided" — unchanged (already covers `PostingWindowElapsed`).
- Decision needed: yes — revises the 2026-09-09 Conflict 10 decision ("approval freezes expiry so Approved/PostingPending/PostingFailed never expire"), conflicts with Spine AD-5 line 149 ("never a human abandon" from `Approved` — becomes a Spine defect to log), and reopens Product-deferred M-12.
- Co-located with: CI-3 (row 433), CI-10 / Rubric-DN-lookup (line 440), AH-5 (automatic posts inherit these rows).

### AH-5 — High — Automatic-mode posting has no lifecycle
- Target: PRD
- Location: FR-11, ADD after line 326; FR-25 line 609; FR-28 line 700 (posting-failure sentence, see Rubric-DN-double)
- Anchor (line 326): "- Posting re-reads the Conversation Agent State and does not post while the Conversation is `Blocked` or `ExternallyRemoved` (FR-2)."
- ADD after it: "- An automatic post follows the FR-18 posting rows from `Approved` onward: the generated version is the approved version and the generation success instant is the approval instant, so the pre-post re-validation, `PostingPending`, `PostingFailed` with the same retry bound, the `MessageId` lookup, `LateConfirmed`, and the same exits apply unchanged. A `PostingFailed` automatic post is abandonable by the Tenant Agent Administrator under the FR-33 row, and the posting-failure rate in FR-28 counts it on the same terms as a proposal."
- Current text (line 609): "- Status exposes posting failures by typed reason, including `MembershipUnavailable`, `MembershipRejected`, and safety verdicts (FR-2, FR-18)."
- Proposed text: "- Status exposes posting failures by typed reason, including `MembershipUnavailable`, `MembershipRejected`, and safety verdicts (FR-2, FR-18), and exposes the FR-18 posting state of every automatic post as the Agent Call's posting outcome (FR-11)."
- Decision needed: none.
- Co-located with: Rubric-DN-double and AL-3 (line 700), AH-4 (FR-18 rows).

### AH-6 — High — 30-day data-handling grace ships content under unaccepted terms; "version in force" ambiguous
- Target: PRD
- Location: FR-4 line 199 (last sentence); FR-24 line 596; A-10 row line 859; OQ-29 line 999; §9 line 884
- Current text (line 199, last sentence): "The Tenant Agent Administrator accepts a named `DataHandlingVersion` for the tenant before the first activation that selects the model, and again when the version changes; until re-acceptance, calls continue under the previously accepted version for 30 days, after which the model cannot be called for that tenant [ASSUMPTION A-10]."
- Proposed text: "The Tenant Agent Administrator accepts a named `DataHandlingVersion` for the tenant before the first activation that selects the model, and again when the version changes. A change that loosens any field — a longer retention term, training use no longer opted out, a new or broader processing region, or a weaker contractual reference — blocks the model for the tenant immediately with a typed reason until the Tenant Agent Administrator accepts the new version. A change that only tightens continues without re-acceptance for 30 days and is notified on the FR-25 surface; after 30 days without acceptance the model cannot be called for that tenant [ASSUMPTION A-10]."
- Current text (line 596): "- Audit Evidence records the Provider data-handling record version in force for each Provider attempt (FR-4)."
- Proposed text: "- Audit Evidence records, for each Provider attempt, both the Provider's current `DataHandlingVersion` and the version the tenant accepted (FR-4); the two differ only during the tightening-change grace."
- A-10 row 859: replace "accepts a named `DataHandlingVersion` with a 30-day grace on change" with "accepts a named `DataHandlingVersion`; a loosening change blocks immediately, a tightening change has a 30-day grace".
- OQ-29 line 999: replace "and the Tenant Agent Administrator accepts the version before first activation and on each change with a 30-day grace" with "and the Tenant Agent Administrator accepts the version before first activation and on each change — a loosening change blocks the model immediately, a tightening change runs a 30-day grace".
- Decision needed: yes — revises OQ-29 / A-10 and the 2026-09-09 H-3 / Gate M-3 decisions (30-day grace on every change).
- Co-located with: CX-6 (Spine/register side), DM-4 (code).

### AH-7 — High — FR-34 attestation compares against a value the same host supplies
- Target: MIXED — register: `EXT-SECRETS-1` scope gains the Release-Operator-owned committed-engine value; PRD part below.
- Location: FR-34 line 727 (after "…equal to the register's committed `EXT-PROTECTION-1` target."); line 730 test
- Anchor (line 727): "it passes only when every step succeeds and the engine reports an identity and version equal to the register's committed `EXT-PROTECTION-1` target."
- ADD after the anchor sentence: "The committed engine identity and version are recorded in the launch readiness register by the Release Operator and provisioned to the runtime as a Release-Operator-owned configuration value custodied through `EXT-SECRETS-1`, separate from host composition; a missing value, or a value that differs from the register, leaves the blocker standing, and the attestation records which principal supplied each of the two compared values."
- Current text (line 730, end): "the same run with a production engine bound and attested must clear the blocker with no other change."
- Proposed text: "the same run with a production engine bound and attested must clear the blocker with no other change; and a host that binds an engine reporting the committed identity while the Release-Operator-owned value is absent must still report `PayloadProtectionUnavailable`."
- Decision needed: yes — extends the Gate H-5 decision and the `EXT-SECRETS-1` register scope (correct-course item).
- Co-located with: AM-15 (line 727), CX-14 (register wording).

### AH-8 — High — Compliance inspection can be scope-split; unreviewed inspection has no consequence
- Target: PRD
- Location: FR-24 line 590 (after "…recorded as unreviewed on the audit surface."); OQ-30 line 1000; FR-33 row 476
- Anchor (line 590): "an inspection not reviewed within that window is recorded as unreviewed on the audit surface."
- ADD after the anchor: "Post-hoc inspections by one Inspector that touch more than 5 distinct Conversations in a rolling 30-day window are one wide inspection: the sixth requires prior Platform Operator approval [ASSUMPTION A-23]. An Inspector with an unreviewed inspection cannot open another post-hoc inspection until it is reviewed or the Platform Operator records a disposition. The inspection rate carries a threshold [ASSUMPTION A-23] above which the Platform Operator's review is mandatory at the next launch-health review."
- ADD §8.1 row: "| A-23 | Post-hoc inspection aggregation bound of 5 distinct Conversations per Inspector per rolling 30 days, and the inspection-rate review threshold | FR-24 | Product + Governance | Product confirms or retunes before enablement |"
- OQ-30 line 1000: append before "Export requires": "More than 5 distinct Conversations post hoc in 30 days is one wide inspection; an unreviewed inspection blocks its Inspector's next post-hoc inspection (A-23)."
- Decision needed: yes — the inspection-rate threshold value is not given by the report; Product sets it (A-23). Folds Product-deferred gate L-6.
- Co-located with: AM-8 (line 590), OQ-30.

### AH-9 — High — PRD statements about the Spine and register are false on disk
- Target: PRD-resync
- Location: §0 line 16; §8 line 814; §8 lines 818, 820; §8.1 lines 840–844
- Line 16: see DC-2 proposed text. Line 814: see CX-2/CX-7. Lines 818/820: see CX-3. Lines 840–844: see CX-5 / Rubric-DU-8.1.
- Closure rule (new, ADD at end of §8.1 line 840 paragraph, after the CX-5 text): "This PRD states no count, line number, or open-defect list for another document: the registers and the Spine are authoritative for their own current state, and a cross-document claim here is dated and points at the owning document's ledger."
- Decision needed: none for the resync; the AD-13 mention is decided under Rubric-DU-8.1.
- Co-located with: CX-2, CX-3, CX-4, CX-5, CX-7, Rubric-DR-HOST, Rubric-DU-8.1, Rubric-SH-seams, Rubric-DU-count, MN-1, MN-5.

### CX-2 — High — PRD reports `EXT-HOST-1` as `Committed`; register has all nine `Uncommitted`
- Target: PRD-resync
- Location: §0 line 16 (see DC-2 proposed text); §8 line 814
- Current text (line 814): "At the time of this revision, only `EXT-HOST-1` is `Committed`; the remaining entries are `Uncommitted`. Every consuming story therefore remains blocked from `ready-for-dev` under FR-21, and any story already completed against an `Uncommitted` entry carries a non-conformance record in the register."
- Proposed text (line 814, combined with CX-7): "At the time of this revision every entry is `Uncommitted`; `EXT-HOST-1`'s earlier target, date, and command are historical only, because its required artifact now includes the production `EXT-PROTECTION-1` binding and the FR-34 attestation port, and the register is authoritative for current status. Every consuming story therefore remains blocked from `ready-for-dev` under FR-21. The register carries an open Product ruling on whether Story 5.3 consumed `EXT-PROVIDER-1` while `Uncommitted` — Branch A records the non-conformance, Branch B records none — and neither disposition is recorded until Product selects a branch (OQ-17)."
- Decision needed: none.
- Co-located with: DC-2, Rubric-DR-HOST, AH-9, MN-5 (line 16); CX-7 (line 814).

### CX-3 — High — Seam-2 existence read and seam-4 event feed said to be pending; both landed
- Target: PRD-resync
- Location: §8 seam 2 line 818 (last sentence); seam 4 line 820 (last sentence)
- Current text (line 818): "The existence read is not yet in the register's seam-2 text; adding it is a pending register extension (correct-course, 2026-09-09), and `EXT-CONV-AI-1` cannot reach `Available` on a compatibility command that does not exercise it."
- Proposed text: "The register's seam-2 text and compatibility contract carry the existence read (scope extended 2026-09-09), and `EXT-CONV-AI-1` cannot reach `Available` on a compatibility command that does not exercise it."
- Current text (line 820): "The event-feed alternative is a pending register extension (correct-course, 2026-09-09)."
- Proposed text: "The event-feed alternative is carried in the register's seam-4 text (scope extended 2026-09-09)."
- Decision needed: none.
- Co-located with: Rubric-SH-seams (same sentences), AH-9.

### CX-4 — High — §8.1 misstates the Spine's assumption range and index
- Target: PRD-resync
- Location: §8.1 line 836; FR-18 line 441 and FR-3 line 176 (ARCH-A-11, via CI-8)
- Current text (line 836, parenthetical): "(frontmatter `updated: 2026-09-09`; at that date the table holds `ARCH-A-1` through `ARCH-A-10`, with `ARCH-A-5` retired)"
- Proposed text: "(frontmatter `updated: 2026-09-09`; the Spine's Architecture Assumption Index — `ARCH-A-INDEX-3` at that date — is authoritative for the row set at any moment, and this PRD states no row count)"
- Decision needed: none (the ARCH-A-11 confirmation is CI-8's decision).
- Co-located with: Rubric-DU-count, MN-1 (line 836); CI-8 (lines 176, 441).

### CX-5 — High — §8.1 lists AD-5, AD-22, AD-12 as open Spine defects; all three corrected
- Target: PRD-resync
- Location: §8.1 line 840 (from "three are open at this revision") and lines 842–844; OQ-26 line 996; OQ-27 line 997; addendum lines 48–49
- Current text (line 840, tail): "A Spine rule that narrows or contradicts an FR-18, FR-24, FR-28, or FR-33 rule is a Spine defect to be corrected before the consuming story is `ready-for-dev`; three are open at this revision, owned by Architecture:"
- Current text (lines 842–844): "- AD-5 forbids the system abandonment from `Approved` and `PostingFailed` on a detected removal that FR-18 requires, and allows human abandonment from `PostingFailed` only.\n- AD-22 names the Tenant Agent Administrator as the sole inspection second party and gives export and hold release none, against FR-24 and FR-33.\n- AD-12 states the Release Operator trigger without the review, minimum sample, and `InsufficientEvidence` rule of FR-28."
- Proposed text (replaces the tail of 840 and deletes 842–844): "A Spine rule that narrows or contradicts an FR-18, FR-24, FR-28, or FR-33 rule is a Spine defect to be corrected before the consuming story is `ready-for-dev`. Open Spine defects are tracked in the Spine's own reconciliation ledger (its `.memlog.md`), not restated here; the three recorded by the second 2026-09-09 update (AD-5, AD-12, AD-22) were corrected by the Spine the same day." (Rubric-DU-8.1 decides whether an AD-13 sentence follows.)
- Current text (OQ-26 line 996, last sentence): "The system abandonment from `Approved` and `PostingFailed` on a detected removal is a deliberate divergence from Spine AD-5, recorded in §8.1 as a Spine defect for Architecture to correct."
- Proposed text: "The system abandonment from `Approved` and `PostingFailed` on a detected removal was a deliberate divergence from Spine AD-5, corrected in the Spine on 2026-09-09."
- Current text (OQ-27 line 997, fragment): "(safety wins over "complete on their own terms", a Spine AD-12 correction owned by Architecture)"
- Proposed text: "(safety wins over "complete on their own terms"; Spine AD-12 was corrected to this rule on 2026-09-09)"
- addendum.md line 48 current (last sentence): "AD-5 is recorded in §8.1 as a Spine defect to correct." → proposed: "AD-5 was recorded in §8.1 as a Spine defect and corrected in the Spine on 2026-09-09."
- addendum.md line 49 current (last sentence): "AD-12 is listed for Architecture to correct." → proposed: "AD-12 was listed for Architecture and corrected in the Spine on 2026-09-09."
- Decision needed: none.
- Co-located with: Rubric-DU-8.1, AH-9, MN-1 (lines 840–844); AH-4 (re-opens AD-5 as a new defect if adopted).

### CX-6 — High — `DataHandlingVersion` gate absent from Spine AD-10 and the register's Provider Readiness Contract
- Target: OTHER-ARTIFACT — Spine AD-10 / AD-2 (`TenantProviderEnablement` holds the accepted version; data-handling record in the capability floor and hard gates); launch-readiness / dependency register Provider Readiness Contract reason codes (`DataHandlingUnrecorded`, `DataHandlingUnaccepted`). No PRD change (AH-6 changes the grace rule the Spine must mirror).

### Rubric-DR-HOST — High — [Rubric: Decision-readiness] §0 and §8 report `EXT-HOST-1` `Committed`; no proposal amends the PRD
- Target: PRD-resync
- Location: §0 line 16; §8 line 814 — proposed texts under DC-2 and CX-2 already use the register-keyed form and state that FR-34's attestation port changed the `EXT-HOST-1` artifact and, under the §8 change rule, its status.
- Decision needed: none.
- Co-located with: DC-2, CX-2, AH-9, MN-5.

### DH-1 — High — Configuration accepts `BlockWithAuditableOverride` and `Caller`; UI offers `Caller`
- Target: CODE — `AgentAggregate.cs`, `ApproverPolicy.razor`, tests. No PRD change.

### DH-2 — High — Party link/replace accepted; provisioning creates no AI Party; tenant admin can create `hexa`
- Target: CODE — `[Obsolete]` + typed rejection on both handlers, Platform-scoped provision command creating the Party, deny `CreateAgent` to tenant roles. No PRD change.

### DH-3 — High — `ProposalDetail.razor` treats `PostingFailed` as terminal
- Target: CODE — `ProposalDetail.razor`, `ProposalDetailTests.cs`. No PRD change.

### DH-4 — High — `PostingFailed` has no abandon exit, unbounded retry via re-approval, re-enters `PostingPending`
- Target: CODE — dedicated retry command with budget, abandon from `PostingFailed`, reject approve/retry while `PostingPending`, `MessageId` lookup on exit. No PRD change.

### DH-5 — High — Membership only at posting time; no Conversation Agent State
- Target: CODE — gate `Membership` check, `ConversationAgentState` machine, block/mirror/clear. No PRD change.

### DH-6 — High — Expiry enforced only by timer; default reader records no `ExpiresAt`
- Target: CODE — command-time `ExpiresAt` enforcement, 24-hour default. No PRD change.

### DH-7 — High — Safe Context Budget subtracts only the output allowance
- Target: CODE — `AgentInteractionContextPolicy.cs:77`. No PRD change.

### DH-8 — High — Indeterminate outcomes mapped to `AdapterFailure`; retry budget handed to adapter unconditionally
- Target: CODE — `AgentOutputGenerationResult.cs`, generation/regeneration orchestrators, `AgentGenerationProviderRequest.cs`. No PRD change.

### DH-9 — High — Lifecycle and suspension not re-validated on approve/edit/regenerate; no kill switch
- Target: CODE — approval/edit/regeneration orchestrators, `KillSwitch`/`Suspended`. No PRD change.

---

## Medium

### AM-1 — Medium — `ExpiredWhileSuspended` overlap rule launders SM-3 / SM-C5
- Target: PRD
- Location: FR-18 row line 424; FR-28 line 698 (report addition)
- Current text (row 424): "| `Pending`, `Edited`, `Regenerated` | `Expired` | System | Stored `ExpiresAt` passed; enforced on every read and command; recorded with reason `ExpiredWhileSuspended` when the awaiting window overlapped a `Suspended` period |"
- Proposed text: "| `Pending`, `Edited`, `Regenerated` | `Expired` | System | Stored `ExpiresAt` passed; enforced on every read and command; recorded with reason `ExpiredWhileSuspended` when `ExpiresAt` passed while the tenant was `Suspended` |"
- (AM-1's second condition — remaining awaiting time at pull shorter than the suspension — is the same condition stated from the pull instant; the `ExpiresAt`-instant wording from CI-4 and the rubric covers both.)
- Current text (line 698, fragment): "`ExpiresAt` keeps running — a proposal that expires during the suspension is recorded `ExpiredWhileSuspended` and leaves the SM-3 and SM-C5 denominators."
- Proposed text: "`ExpiresAt` keeps running — a proposal whose `ExpiresAt` passes during the suspension is recorded `ExpiredWhileSuspended` and leaves the SM-3 and SM-C5 denominators, and the count of such proposals is reported at each launch-health review next to the switch's pull history."
- Decision needed: none.
- Co-located with: CI-4, Rubric-DN-EWS (row 424); AM-2, AM-3, AM-4, AL-2, AL-3, AL-4, Rubric-DN-double (line 698–700).

### AM-2 — Medium — Release is weaker than pull
- Target: PRD
- Location: FR-28 line 698 (sentence "Release requires the same roles and an audited justification."); FR-33 row 473; FR-30 line 621
- Current text (line 698, sentence): "Release requires the same roles and an audited justification."
- Proposed text: "Release requires the same roles and an audited justification: release of a pull recorded on an SM-4 event requires the Platform Operator's recorded containment finding, and release of a review pull requires a recorded review decision. A pull older than 7 days without a recorded review is recorded as `SuspensionReviewOverdue` on the FR-30 surface."
- Current text (row 473): "| Release the per-tenant kill switch (FR-28) | Platform Operator or Release Operator, with audited justification | Tenant |"
- Proposed text: "| Release the per-tenant kill switch (FR-28) | Platform Operator or Release Operator, with audited justification; an SM-4 pull only on the Platform Operator's recorded containment finding, a review pull only on a recorded review decision | Tenant |"
- FR-30 line 621: replace "The surface also records `GateOutOfScope` (FR-28) and `TriggerReviewOverdue` (FR-28) conditions." with "The surface also records `GateOutOfScope`, `TriggerReviewOverdue`, `SuspensionReviewOverdue`, and `DeferredAssumption` conditions (FR-28)."
- Decision needed: none (tightens the Gate H-5 release rule without changing roles).
- Co-located with: AM-1, AM-3, AM-4 (line 698–700); AC-1, MN-2 (line 621).

### AM-3 — Medium — Trigger-review episode semantics undefined; failure share has no denominator
- Target: PRD
- Location: FR-28 line 700
- Current text (fragment): "the Provider-or-generation failure share above 20%, or the posting-failure rate above 10%. The Release Operator convenes and records the review within one business day of the condition; a review not convened in that time is recorded as `TriggerReviewOverdue` on the FR-30 surface."
- Proposed text: "the Provider-or-generation failure share — over Accepted Agent Calls that reached Provider invocation in the window — above 20%, or the posting-failure rate above 10%. The Release Operator convenes and records the review within one business day of the condition; a review not convened in that time is recorded as `TriggerReviewOverdue` on the FR-30 surface. A review decision is valid for 7 days or until the triggering rate rises by a further 10 percentage points, whichever comes first; a condition that persists beyond that requires a new review `[ASSUMPTION A-17]`."
- A-17 row 866: append "; a review decision is valid 7 days or +10 points".
- Decision needed: none (values are A-17 provisional, Product retunes).
- Co-located with: AM-4, AL-2, AL-3, AL-4, Rubric-DN-double, AH-5 (line 700).

### AM-4 — Medium — A tenant can force mandatory reviews on demand
- Target: PRD
- Location: FR-28 line 700
- Current text (fragment): "excluding only rejections at a rate limit set by the Platform Operator or Release Operator — a rejection at a limit the Tenant Agent Administrator lowered counts."
- Proposed text: "excluding only rejections at a rate limit set by the Platform Operator or Release Operator — a rejection at a limit or cap the Tenant Agent Administrator lowered counts, is attributed as tenant-caused on the FR-25 surface and the tenant's launch-health view, and a condition composed only of such rejections is closed by the Release Operator's recorded acknowledgement rather than a convened review; a condition with any platform-caused reason keeps the one-business-day convening bound."
- Decision needed: none (refines Gate H-2 without excluding tenant-lowered limits from the count).
- Co-located with: AM-3, AL-2, AL-3, AL-4 (line 700).

### AM-5 — Medium — FR-8 runs the classifier and reservation before two free local rejections
- Target: PRD
- Location: FR-8 lines 276–286; FR-32 line 716 ("FR-8 step 4"); FR-2 line 151 ("last pre-Provider step" — still true)
- Current text (lines 277–285): "  1. Caller authorization and Source Conversation access (FR-20, FR-33).\n  2. Agent lifecycle (FR-3).\n  3. Provider/model eligibility (FR-5).\n  4. Rate limits (FR-32).\n  5. Conversation Context Policy, which measures the context (FR-9).\n  6. Cost reservation of the estimated attempt cost — measured input tokens plus the reserved output allowance at the current pricing version (FR-28).\n  7. Pre-Provider Content Safety Policy (FR-27).\n  8. Eligible Approver resolution where Confirmation Response Mode applies (FR-7).\n  9. Agent membership in the Source Conversation (FR-2)."
- Proposed text: "  1. Caller authorization and Source Conversation access (FR-20, FR-33).\n  2. Agent lifecycle (FR-3).\n  3. Conversation Agent State: a `Blocked` or `ExternallyRemoved` record rejects the call on the local read alone (FR-2).\n  4. Provider/model eligibility (FR-5).\n  5. Rate limits and the per-Party concurrent bound (FR-32).\n  6. Eligible Approver resolution where Confirmation Response Mode applies (FR-7).\n  7. Conversation Context Policy, which measures the context (FR-9).\n  8. Cost reservation of the estimated attempt cost — measured input tokens plus the reserved output allowance at the current pricing version (FR-28).\n  9. Pre-Provider Content Safety Policy (FR-27).\n  10. Agent membership in the Source Conversation — the roster-dependent join (FR-2)."
- ADD after line 286: "- Steps 1 through 6 require no classifier call and no Provider reservation, so every rejection they produce is measured by the NFR-9 fast pre-Provider gate."
- FR-32 line 716 current: "and is enforced at FR-8 step 4 with a typed rejection." → proposed: "and is enforced at FR-8 step 5 with a typed rejection."
- Decision needed: yes — M-6 was deferred to Product on 2026-09-09; confirm the reorder (it renumbers FR-8 steps cited by FR-32, Rubric-DN-SMC4, AL-7, and the Spine AD-13 order).
- Co-located with: AL-7 (step range), Rubric-DN-SMC4 (steps 2–3 become 2–4), FR-2 line 151.

### AM-6 — Medium — SM-4 completeness demands denials for non-callable FR-33 rows
- Target: PRD
- Location: §12 SM-4 line 939
- Current text (sentence): "SM-4 is `InsufficientEvidence` unless every FR-33 row has at least one recorded denial in qualification telemetry and the forged-extension test named in the Architecture Spine has run."
- Proposed text: "SM-4 is `InsufficientEvidence` unless every FR-33 row that is a callable operation on the §10 public surface has at least one recorded denial in qualification telemetry and the forged-extension test named in the Architecture Spine has run. The exempt rows are the deployment and human-process rows — bind the protection engine, confirm an SM-4 event, convene and record the trigger review, and the Security approval condition on policy publication — whose evidence is the recorded artifact each row names."
- Decision needed: none.
- Co-located with: none.

### AM-7 — Medium — A permanently refused mirror makes a block unclearable and `MirrorPending` eternal
- Target: PRD
- Location: FR-2 line 164 and line 165; A-1 row line 850; §8 seam 1 line 817; FR-25 line 608
- Current text (line 164, sentence): "Until the mirror is confirmed the state carries a `MirrorPending` flag that FR-25 exposes, and the mirror is retried until it succeeds."
- Proposed text: "Until the mirror is confirmed the state carries a `MirrorPending` flag that FR-25 exposes. Only a transient failure is retried, each attempt under a timeout; a typed permanent refusal from Conversations confirms the mirror as `MirrorRefused`, a flag FR-25 exposes and SM-C4 counts as an unavailable-call signal for that Conversation, while the block itself stays in force."
- Current text (line 165, fragment): "A clear is refused with a typed reason while a mirror entry is sent and unacknowledged;"
- Proposed text: "A clear is refused with a typed reason only while a mirror attempt is in flight;"
- A-1 row 850 and §8 line 817: add "and authorizes the Agents Service Principal to remove the `AiAgent` participant" (see DL-1 for the full A-1 row).
- FR-25 line 608: replace "and any `MirrorPending` flag (FR-2)" with "and any `MirrorPending` or `MirrorRefused` flag (FR-2)".
- Decision needed: yes — OQ-25 / Gate M-2 (2026-09-09) say the mirror "is retried until it succeeds"; confirm the `MirrorRefused` terminal mirror outcome.
- Co-located with: AL-8 (line 164), AH-2/AH-3 (FR-2 block), DL-1 (A-1 row, line 817).

### AM-8 — Medium — Computed subject set makes the Administrator branch dead in single-Administrator tenants
- Target: PRD
- Location: FR-24 line 590; OQ-30 line 1000
- Current text (fragment): "plus the Tenant Agent Administrator whose configuration version was in force for any call in scope. The second party is the Tenant Agent Administrator when outside that set, and otherwise the Platform Operator or a second Compliance Inspector;"
- Proposed text: "plus the specific Tenant Agent Administrator principal(s) whose configuration version was in force for any call in scope. The second party is the current Tenant Agent Administrator when that principal is not in the set, and otherwise the Platform Operator or a second Compliance Inspector — so in a tenant whose one Administrator's configuration was in force for every call in scope, the second party is always the Platform Operator or a second Inspector;"
- OQ-30 line 1000: replace "and the Tenant Agent Administrator whose configuration was in force); where that set holds the Administrator," with "and the specific Administrator principal(s) whose configuration was in force); where the current Administrator is in that set,".
- Decision needed: none (adopts the Spine AD-22 wording).
- Co-located with: AH-8 (line 590, OQ-30).

### AM-9 — Medium — Agent Instructions unprotected, retained for the tenant's life, exempt from erasure; FR-21 phrase conflicts
- Target: PRD
- Location: §9 line 881; FR-21 line 526; §13 new row OQ-31
- Current text (line 881, sentence): "Configuration evidence, including Agent Instructions and their prior values, is not protected content and is retained for the life of the tenant plus 365 days."
- Proposed text (option keep-unprotected): "Configuration evidence, including Agent Instructions and their prior values, is not protected content and is retained for the life of the tenant plus 365 days, except that Agent Instructions and their audit history are erased on tenant offboarding or on an approved deletion naming the Agent; the exemption from interaction-key protection is a Governance decision (OQ-31) with an owner and a revisit date."
- Current text (line 526, fragment): "and the content fields of Audit Evidence" → proposed: "and the content fields of interaction Audit Evidence"
- ADD §13 row: "| OQ-31 | Agent Instructions and configuration audit are not protected content (FR-34) so `hexa` can be configured before the engine is `Available`; they are erased on tenant offboarding or approved deletion naming the Agent (§9). | Governance + Product | Deferred; revisit before enablement — whether instructions move under an Agent-level key |"
- Decision needed: yes — choose protect-under-Agent-key versus keep-unprotected-with-erasure; touches the Gate H-5 decision (M-10 folded).
- Co-located with: AM-15 / AH-7 (FR-34 block), §9.

### AM-10 — Medium — Any Participant can switch `hexa` off with one policy-failing message; block attributed to the caller
- Target: PRD
- Location: FR-27 line 669; SM-C4 line 957
- Anchor (line 669, end): "a Participant may edit or delete the message there and the next load re-scans the current history (OQ-18)."
- ADD after it (same bullet): "A history verdict failure records, as operator-visible Audit Evidence, the keyed content hash, author Party, and timestamp of each failing message so a Facilitator can act on it in Conversations; FR-25 and SM-C4 attribute the resulting Content Safety Policy block to the Conversation, not to the calling Party, for tuple counting."
- Decision needed: none (OQ-18's deferred remedy is untouched).
- Co-located with: AL-9 (line 668–669), Rubric-DN-SMC4 (line 957).

### AM-11 — Medium — FR-13's "Conversation status entry" is not an `EXT-CONV-UI-1` artifact
- Target: MIXED — register: fourth `EXT-CONV-UI-1` artifact kind (option a, correct-course). PRD part below.
- Location: FR-13 line 359; §8 line 824
- Current text (line 359): "- V1 uses in-product pending-proposal visibility only for authorized Approvers, including a pending count, a queue, and a Conversation status entry."
- Proposed text, option (a): unchanged at 359; §8 line 824 after "plus rendering of the AI-generated and human-edited markers wherever the message's provenance is disclosed" add ", plus a Conversation-level status entry showing the pending-proposal state for authorized Approvers (FR-13)".
- Proposed text, option (b): "- V1 uses in-product pending-proposal visibility only for authorized Approvers, including a pending count and a queue on the Agents admin surface; no Conversation-owned status entry is in V1."
- Decision needed: yes — M-13 was Product-deferred; choose (a) widen `EXT-CONV-UI-1` or (b) drop the Conversation entry.
- Co-located with: none.

### AM-12 — Medium — Segregation of duties is Party-granular; an organization Party can be the "second Approver"
- Target: PRD
- Location: FR-7, ADD after line 245; §6.2 line 779
- Anchor (line 245): "- Approver resolution is Conversation-scoped. … which is a resolution outcome, not a configuration error."
- ADD after it: "- Every Approver Policy source resolves to human Parties only: an organization or AI Party named as a predefined Party is a typed configuration rejection, and a Facilitator or tenant-role holder that resolves to a non-human Party contributes no Approver. Which human may act for an organization Party is out of V1 (§6.2)."
- ADD §6.2 bullet after line 779: "- Acting for an organization Party as an Approver; V1 Approvers are human Parties only (FR-7)."
- Decision needed: yes — M-1 was Product-deferred (organization Parties as Approvers).
- Co-located with: none.

### AM-13 — Medium — Unsafe human edits are stored unscanned until approval
- Target: PRD
- Location: FR-15, ADD after line 382; FR-27 line 667
- Anchor (line 382): "- Every edited version records the editing Party and edit timestamp as durable provenance, and an edited version is never presented as generated output."
- ADD after it: "- An edit is scanned under the active Content Safety Policy before it is recorded (FR-27): a version that fails an always-blocked category is rejected with a typed reason and not stored; a version that fails a restricted category is stored with a `SafetyFailed` marker, is not approvable, and is visible under FR-25. The approval-time re-scan remains, for policy drift."
- Current text (line 667): "- The exact version being approved passes the then-current active policy at approval time. A human-edited version is scanned on the same terms as generated output; no edit path can place unscanned content into a Conversation."
- Proposed text: "- The exact version being approved passes the then-current active policy at approval time (never weaker than the attempt's snapshot policy, FR-26). A human-edited version is scanned at edit time (FR-15) and again at approval on the same terms as generated output; no edit path can place unscanned content into the proposal store or a Conversation."
- Decision needed: yes — M-2 was Product-deferred (addendum line 46: "Deferred, not rejected").
- Co-located with: CI-1 (line 667), DC-4 (code).

### AM-14 — Medium — "Fresh and available" has no staleness bound
- Target: PRD
- Location: FR-7 line 253 (last sentence)
- Current text: "Every secondary input to the Eligible Approver predicate — the tenant-role projection and the per-Party access check — must report fresh and available, or the pass is unavailable as a whole."
- Proposed text: "Every secondary input to the Eligible Approver predicate — the tenant-role projection and the per-Party access check — must report fresh and available, fresh meaning the projection's recorded position is not older than one re-check cadence (A-20) at the moment of the pass; an older position is unavailable, and the pass is then unavailable as a whole."
- Decision needed: none.
- Co-located with: none.

### AM-15 — Medium — FR-34 attestation runs only at startup and readiness evaluation
- Target: PRD
- Location: FR-34 line 727
- Current text (fragment): "The attestation runs at startup and on every readiness evaluation:"
- Proposed text: "The attestation runs at startup, on every readiness evaluation, on a bounded cadence — hourly by default [ASSUMPTION A-24] — and on any host composition change, and the attested engine identity is pinned: every seal and unseal call verifies the engine instance against the pin and fails closed with `PayloadProtectionUnavailable` on a mismatch:"
- ADD §8.1 row: "| A-24 | FR-34 attestation re-run cadence defaults to hourly | FR-34 | Architecture + Security | Architecture confirms or retunes before enablement |"
- Decision needed: none.
- Co-located with: AH-7 (line 727).

### CI-1 — Medium — Safety re-check policy stated two ways (L-I3, open, real)
- Target: PRD
- Location: FR-26 line 640 and line 644; FR-17 line 405; FR-18 rows 421 and 442; FR-27 line 667; OQ-9 line 979
- Current text (line 640): "- Content Safety Policy changes are auditable and affect future Agent Calls only, except that the approval-time and pre-post re-checks required by FR-27 — the third and fourth of the four application points in OQ-9 — always evaluate the then-current active policy."
- Proposed text: "- Content Safety Policy changes are auditable and affect future Agent Calls only, except that the approval-time and pre-post re-checks required by FR-27 — the third and fourth of the four application points in OQ-9 — always evaluate the then-current active policy in addition to the attempt's snapshot policy, never weaker than it (below)."
- Current text (line 644): "- A policy version cannot weaken a retry already in progress; each retry, approval-time check, and pre-post check uses the more restrictive of the initial attempt's policy and the then-current active policy. A re-check can therefore only tighten the outcome, never loosen it."
- Proposed text (rubric's computable form): "- A policy version cannot weaken a retry already in progress: at each retry, approval-time check, and pre-post check the content must pass every applicable policy version — the initial attempt's snapshot policy and the then-current active policy, and a mode-specific policy in addition to, never instead of, the platform policy. A re-check can therefore only tighten the outcome, never loosen it."
- Line 405 current: "- The approved version passes the then-current Content Safety Policy at approval time before posting, whether it was generated or human-edited (FR-27)." → proposed: "- The approved version passes the then-current Content Safety Policy (never weaker than the attempt's snapshot policy, FR-26) at approval time before posting, whether it was generated or human-edited (FR-27)."
- Row 421 guard "Version passes the then-current Content Safety Policy (FR-27)" → "Version passes the then-current Content Safety Policy, never weaker than the attempt's snapshot policy (FR-26, FR-27)".
- Line 442 "and that the approved version still passes the then-current Content Safety Policy." → "and that the approved version still passes the then-current Content Safety Policy (never weaker than the attempt's snapshot policy, FR-26)."
- OQ-9 line 979 "The approval-time and pre-post checks always evaluate the then-current active policy." → "The approval-time and pre-post checks always evaluate the then-current active policy and the attempt's snapshot policy, and the content must pass both."
- Decision needed: yes — CI-1 keeps "more restrictive" and the rubric replaces it with "pass every applicable version"; the memlog (2026-09-08) recorded "the more restrictive of initial and then-current" and Spine AD-20 implements `LastLoosensVersion` ordering. Recommend the pass-both form (computable, same effect).
- Co-located with: Rubric-DN-restrictive (same lines), AM-13 (line 667), AH-4 (row 421 area).

### CI-2 — Medium — FR-23 register omits the link/replace commands
- Target: PRD — same edit as AH-1 (line 557). Decision: none. Co-located with: AH-1.

### CI-3 — Medium — Administrative retry: table allows it any time, FR-33 only after budget exhaustion
- Target: PRD
- Location: FR-18 row 433 guard
- Current text (row 433 guard cell): "Failure reason is not a safety verdict; tenant not `Suspended` and Agent `Active`; the `MessageId` lookup finds no posted message; full pre-post re-validation re-run"
- Proposed text: "Failure reason is not a safety verdict; tenant not `Suspended` and Agent `Active`; the `MessageId` lookup finds no posted message; full pre-post re-validation re-run; administrative retry only after the automatic budget is exhausted (FR-33) and while the `PostingWindowElapsed` bound has not elapsed (if AH-4 is adopted)"
- Decision needed: none.
- Co-located with: AH-4 (row 433), CI-8 (line 441).

### CI-4 — Medium — `ExpiredWhileSuspended` recorded on two conditions
- Target: PRD — same edit as AM-1 (row 424). Decision: none. Co-located with: AM-1, Rubric-DN-EWS.

### CI-5 — Medium — Two settlement instants for a held `Indeterminate` reservation
- Target: PRD
- Location: FR-28 line 692; OQ-6 line 976; (then Spine AD-21 / ARCH-A-6 — other artifact)
- Current text (line 692, fragment 1): "a reservation whose outcome is `Indeterminate` is held for a bounded period — default 24 hours, configurable from 1 through 72 hours [ASSUMPTION A-8] — and then conservatively settled at the estimated maximum, subject to the audited override in FR-32."
- Proposed text: "a reservation whose outcome is `Indeterminate` is held for a bounded period — default 24 hours, configurable from 1 through 72 hours [ASSUMPTION A-8] — during which only an audited operator command settles it, subject to the audited override in FR-32."
- Current text (line 692, fragment 2): "A reservation past its hold deadline with no authoritative outcome becomes `Unreconciled`:"
- Proposed text: "An `Indeterminate` reservation whose hold deadline passes without an operator settlement becomes `Unreconciled`:"
- Current text (OQ-6 line 976, sentence): "A reservation whose outcome is `Indeterminate` is held for a bounded period (default 24 hours, A-8) and then conservatively settled at the estimated maximum rather than held indefinitely, with an audited administrative override available to restore service."
- Proposed text: "A reservation whose outcome is `Indeterminate` is held for a bounded period (default 24 hours, A-8), is settled during the hold only by an audited operator command, and becomes `Unreconciled` when the hold passes — still counted against the cap and settled at period close at the estimated maximum — with an audited administrative override available to restore service."
- Decision needed: none (report's recommended branch).
- Co-located with: none in PRD; Spine AD-21 mirror is correct-course.

### CI-6 — Medium — FR-7 re-check abandons `PostingFailed` for `NoEligibleApprover`; FR-18 has no such row
- Target: PRD
- Location: FR-7 line 255
- Current text: "- The re-check moves a proposal to `Abandoned` with reason `NoEligibleApprover` only after two consecutive passes, at least one cadence apart, each of which returns a roster whose resolved Eligible Approver set is empty; the first such pass records the marker `ResolutionEmptyPending` on the proposal, visible under FR-25, so a momentary absence never abandons."
- Proposed text: "- The re-check moves an awaiting proposal to `Abandoned` with reason `NoEligibleApprover` only after two consecutive passes, at least one cadence apart, each of which returns a roster whose resolved Eligible Approver set is empty; the first such pass records the marker `ResolutionEmptyPending` on the proposal, visible under FR-25, so a momentary absence never abandons. A `PostingFailed` proposal receives the `ResolutionEmptyPending` marker only and is never abandoned for an empty roster, so the marker rule and the FR-18 table agree."
- Decision needed: none.
- Co-located with: AL-6 (same sentence).

### CI-7 — Medium — `ContextUnavailable` counted but never produced
- Target: PRD
- Location: FR-9 line 302
- Current text: "- If the complete Conversation Context cannot be loaded or sent safely, the Agent Call fails closed and creates neither Provider work, a Proposed Agent Reply, nor a Conversation Message."
- Proposed text: "- If the complete Conversation Context cannot be loaded or sent safely, the Agent Call fails closed with the typed reason `ContextUnavailable` — which FR-25 and SM-C4 count as an unavailable call, not a blocked one, and which is recorded as the context mode `Blocked` with that reason — and creates neither Provider work, a Proposed Agent Reply, nor a Conversation Message."
- Decision needed: none.
- Co-located with: DM-3 (classification), AL-9 (uses the reason).

### CI-8 — Medium — `PostingFailed` retry clock while the Agent is `Disabled` is unstated (Spine ARCH-A-11)
- Target: PRD
- Location: FR-18 line 441; FR-3 line 176
- Current text (line 441, opening): "- While the tenant kill switch is pulled (FR-28), the `PostingFailed` retry-window clock is paused and automatic and administrative retries are suspended;"
- Proposed text: "- While the tenant kill switch is pulled (FR-28) or the Agent is `Disabled` (FR-3), the `PostingFailed` retry-window clock is paused and automatic and administrative retries are suspended — paused time is the union of the two conditions, never double-counted;"
- Anchor (line 176, end): "a `PostingPending` attempt already in flight completes or fails on its own terms."
- ADD after it (same bullet): "A `PostingFailed` proposal's retry-window clock is paused and its retries suspended while the Agent is `Disabled`, on the same terms as the kill switch (FR-18); this is the Product confirmation that retires Spine `ARCH-A-11`."
- Decision needed: yes — a Product confirmation (retires `ARCH-A-11`, owner Architecture + Product).
- Co-located with: CX-4 (ARCH-A-11 mention), CI-3 / AH-4 (FR-18 rows).

### CI-9 — Medium — FR-28 assigns decisions to "Release PM" and "Product", neither mapped by FR-33
- Target: PRD — same edits as Rubric-DR-PM (glossary entry, FR-33 intro sentence, two FR-33 rows). Decision: yes (see Rubric-DR-PM). Co-located with: Rubric-DR-PM, AC-1.

### CI-10 — Medium — `Approved`-origin abandonment names a `MessageId` lookup the rule never performs from `Approved`
- Target: PRD
- Location: FR-18 line 440 (first two sentences)
- Current text: "Before every retry attempt and before any exit from `PostingFailed` — including abandonment by any actor — the system looks the message up by its `MessageId` through the seam-2 existence read. The lookup is skipped for a `PostingFailed` proposal whose failure was a pre-post re-validation that never attempted a post."
- Proposed text (rubric form, recommended): "Before every retry attempt and before any exit from `PostingFailed` — including abandonment by any actor — the system looks the message up by its `MessageId` through the seam-2 existence read. The lookup is skipped for any proposal for which no post was ever attempted — a `PostingFailed` proposal whose failure was a pre-post re-validation, and every `Approved` proposal, since posting has not started — so a removal detected against an `Approved` proposal abandons it directly."
- Alternative (CI-10 form, matches Spine AD-5): "…and before the system's `Approved` → `Abandoned` (`RemovedInConversations`) transition, the system looks the message up by its `MessageId` through the seam-2 existence read; an unavailable answer leaves an `Approved` proposal in `Approved`. The lookup is skipped for a `PostingFailed` proposal whose failure was a pre-post re-validation that never attempted a post."
- Row 429 guard "and the `MessageId` lookup finds no posted message" → under the rubric form: "and, for `PostingFailed`, the `MessageId` lookup finds no posted message".
- Decision needed: yes — CI-10 (perform the lookup from `Approved`; Spine AD-5 form) versus the rubric (skip it because no post was attempted); the two fixes are incompatible.
- Co-located with: Rubric-DN-lookup (same sentences), AH-4 (rows 429/431).

### CX-7 — Medium — PRD asserts a non-conformance record exists; register holds an open Product ruling
- Target: MIXED — register: Product selects Branch A or B (proposal §6 open item). PRD part: §8 line 814 — proposed text under CX-2 already replaces the non-conformance claim with the open-ruling statement. OQ-17 line 987 states the rule, not the record; unchanged.
- Decision needed: none for the PRD text (the branch choice is a register/Product decision listed for correct-course).
- Co-located with: CX-2 (line 814).

### CX-8 — Medium — Spine AD-2 first paragraph contradicts FR-2 on `MirrorPending`, `ReadmitPending`, `BlockVersion`
- Target: OTHER-ARTIFACT — Spine AD-2 line 125: replace the `ConversationAgentState` clause with the 127 wording. No PRD change.

### CX-9 — Medium — Register seam 1 lacks the typed "already absent" removal answer
- Target: OTHER-ARTIFACT — external-dependency-register.md seam (1) line 57 and contract line 60: add the typed already-absent confirmed no-op. No PRD change.

### CX-10 — Medium — Epics inventory rows FR4, FR5, FR12, FR22, FR25, FR27 summarise pre-2026-09-09 FRs
- Target: OTHER-ARTIFACT — epics.md rows 44, 46, 60, 80, 86, 90 rewritten from PRD lines 186–201, 209, 334, 542, 604–612, 658–670. No PRD change.

### CX-11 — Medium — Epics never name `Draft`, `MembershipUnavailable`, `MembershipRejected`
- Target: OTHER-ARTIFACT — epics.md FR3 row, UX-DR25, Stories 5.2/5.7/6.6/6.8. No PRD change.

### CX-12 — Medium — No epics story carries the `DataHandlingVersion` enablement block, acceptance, or grace
- Target: OTHER-ARTIFACT — epics.md Stories 5.3, 5.7, 6.4 (cite OQ-29; mirror AH-6's loosen/tighten rule if adopted). No PRD change.

### Rubric-DR-PM — Medium — [Rubric: Decision-readiness] "Release PM" holds two `RQ-1` powers but is not an FR-33 role and has no glossary entry
- Target: PRD
- Location: §3 ADD after line 114; FR-33 line 453; FR-33 ADD two rows after line 469; FR-28 lines 685, 688, 701
- ADD glossary entry after line 114: "- **Release PM** - The planning-time name of the Release Operator role, used in §8.1 and in §13 ownership cells; the two names denote the same role, and every Release PM decision this PRD names is recorded by the Release Operator in the launch readiness register."
- Current text (line 453, fragment): "The Conversation Facilitator is a Conversations role that the block and clear rows use as an authority, and Security approval is a condition on one row, not a role."
- Proposed text: "The Conversation Facilitator is a Conversations role that the block and clear rows use as an authority, and Security approval is a condition on one row, not a role. Product, Governance, and Security are planning parties, not Agents roles: a decision this PRD assigns to them is recorded in the launch readiness register by the Release Operator, who acts on it under the rows below; Release PM is the planning-time name of the Release Operator (§3)."
- ADD rows after line 469: "| Exclude a dependency from the `RQ-1` qualification profile, naming the FR consequences the run will not exercise (FR-28) | Release Operator; the exclusion is an `RQ-1` blocker until Product accepts it | Platform |" and "| Defer a late-added `ARCH-A` row for the scheduled `RQ-1` evaluation, recorded as `DeferredAssumption` (FR-28, §8.1) | Release Operator; recorded in the launch readiness register | Platform |"
- Line 685 "a recorded Release PM decision" → "a recorded Release Operator decision"; line 688 uses "Release Operator" (AC-1); line 701 unchanged (Product decision, recorded per the new 453 sentence).
- Decision needed: yes — option 1 (glossary equivalence + two FR-33 rows, above) or option 2 (rename the fourteen "Release PM" uses to Release Operator where they are authorization rules, keep "Release PM" in §13 ownership only).
- Co-located with: CI-9, AC-1 (lines 685, 688, 838), MN-6 (glossary).

### Rubric-DN-EWS — Medium — [Rubric: Done-ness clarity] `ExpiredWhileSuspended` has two definitions
- Target: PRD — same edit as AM-1 / CI-4 (row 424). Decision: none. Co-located with: AM-1, CI-4.

### Rubric-DN-restrictive — Medium — [Rubric: Done-ness clarity] "More restrictive of two policies" not computable (deferred, open)
- Target: PRD — same edit as CI-1 (lines 640, 644, and the five "then-current" sites). Decision: yes (see CI-1). Co-located with: CI-1.

### Rubric-DN-FR31 — Medium — [Rubric: Done-ness clarity] FR-31 blocks the reporting UJ-2 exists for; drifts from OQ-9 (deferred, open)
- Target: PRD
- Location: FR-31 line 653; OQ-9 line 979
- Current text (line 653): "- The active Content Safety Policy blocks generated content that impersonates a named Party, or that asserts a decision, commitment, or approval on a Party's behalf."
- Proposed text: "- The active Content Safety Policy blocks generated content that speaks in the first person as a named Party, or that presents a decision, commitment, or approval as a Party's own act when the Conversation does not record it; reporting a Party's position as the Conversation records it is permitted, because that interpretation is what UJ-2 exists for."
- Current text (line 979, sentence): "The policy also blocks generated content that impersonates a named Party or asserts a decision on a Party's behalf."
- Proposed text: "The policy also blocks generated content that speaks in the first person as a named Party, or that presents a decision, commitment, or approval as a Party's own act when the Conversation does not record it; reporting a recorded position is permitted (FR-31)."
- Decision needed: yes — Product-deferred item; amends OQ-9 in place.
- Co-located with: CI-1 (line 979).

### Rubric-DN-AgentCall — Medium — [Rubric: Done-ness clarity] The Agent Call has no public state contract (deferred, open)
- Target: PRD
- Location: FR-8 ADD after line 286; FR-23 line 557 (register sentence, with AH-1)
- ADD after line 286: "- The Agent Call's public state contract is `AgentInteractionStatus` (code), whose recorded members are `Requested`, `Authorized`, `Denied`, `Blocked`, `ContextReady`, `ContextBlocked`, `Generated`, `GenerationFailed`, `SafetyFailed`, `Posted`, `PostingFailed`, and `ProposalCreated`; its terminal members are exactly `Denied`, `Blocked`, `ContextBlocked`, `GenerationFailed`, `SafetyFailed`, `Posted`, and `ProposalCreated` (an automatic post that ends `PostingFailed` follows the FR-18 posting rows, FR-11). Proposal state is carried by `ProposedAgentReplyState` (FR-18) and never duplicated: the shipped `Proposal*` members of `AgentInteractionStatus` other than `ProposalCreated` are on the FR-23 deprecate-and-reject register and are never recorded, and the UI-side `AgentCallStatus` is a presentation projection, not a contract."
- Line 557 register sentence (with AH-1): append ", and the `Proposal*` members of `AgentInteractionStatus` other than `ProposalCreated` (FR-8; proposal state lives in `ProposedAgentReplyState`)".
- Decision needed: yes — Product + Architecture confirm the public enumeration and the terminal-member list (drafted from `AgentInteractionStatus.cs`; `ProposalCreationFailed` and the `*Failed` command outcomes may be wanted as terminal or as typed rejections instead).
- Co-located with: AH-1 / CI-2 (line 557), AM-5 (FR-8 block).

### Rubric-DN-p99 — Medium — [Rubric: Done-ness clarity] p99 over 30 executions is the sample maximum (deferred, open)
- Target: PRD
- Location: NFR-9 line 791 (last sentence); FR-28 item 3 line 682 and line 693; OQ-5 line 975; NFR-14 line 796
- Current text (line 791, last sentence): "Each gate requires at least 30 production-like executions."
- Proposed text: "Each gate is evaluated on its p95 target over at least 30 production-like executions using the nearest-rank percentile method; the p99 targets are reported as launch-health values and become `RQ-1` gates only once a gate has at least 300 executions."
- Current text (line 682): "  3. The NFR-9 and NFR-14 latency gates, each over at least 30 production-like executions." → proposed: "  3. The NFR-9 and NFR-14 latency gates, each on p95 over at least 30 production-like executions (p99 gates only at 300 or more, NFR-9)."
- Current text (line 693): "- The latency gates in NFR-9 are met, each over at least 30 production-like executions. NFR-9 states the thresholds; they are not restated here." → proposed: "- The latency gates in NFR-9 are met on the sample and percentile rule NFR-9 states; the thresholds are not restated here."
- OQ-5 line 975 "Each gate uses at least 30 production-like executions." → "Each gate uses p95 over at least 30 production-like executions, nearest-rank; p99 gates require at least 300 (amended 2026-09-09)." and Status cell "Resolved 2026-08-01" → "Resolved 2026-08-01; amended 2026-09-09".
- Decision needed: yes — Product-deferred item; amends OQ-5 (Architecture + Release PM). Alternative: keep p99 and require n ≥ 300 everywhere.
- Co-located with: none.

### Rubric-DU-8.1 — Medium — [Rubric: Downstream usability] §8.1 Spine-defect list stale in both directions (AD-13 divergence unlisted)
- Target: PRD-resync
- Location: §8.1 lines 840–844 (CX-5 text), plus the AD-13 sentence below; OQ-26, OQ-27 (CX-5)
- ADD after the CX-5 proposed sentence at line 840: "One divergence is recorded at this revision: Spine AD-13 requires Eligible Approver resolution, and a `NoEligibleApprover` rejection, in every response mode, against FR-8 step 8 and FR-7, which scope it to Confirmation Response Mode; under this PRD's rule FR-8 governs and AD-13 is a Spine defect until corrected, and this sentence is removed when the Spine ledger records the correction."
- Decision needed: yes — either record AD-13 as a Spine defect (CX-1's fix, Spine-side) or adopt its rule into FR-8 as a Product decision (an Automatic-mode tenant with no Approver has no human who could ever be resolved); AH-9 additionally prefers no enumerated list at all — choose "pointer only" or "pointer plus the one dated AD-13 sentence".
- Co-located with: CX-5, AH-9, MN-1 (lines 840–844); CX-1.

### DM-1 — Medium — Baseline additive members still missing
- Target: CODE — `AgentInteractionContextMode.Blocked`, `NoEligibleApprover`, `RemovedInConversations`, `SourceConversationUnavailable`, `NotInvoked`, `Membership` gate check, `ProhibitedCostControlPosture`, `PayloadProtectionUnavailable`, `Suspended`. No PRD change.

### DM-2 — Medium — Second-update vocabulary has no code footprint
- Target: CODE — fifteen typed names, the FR-7 re-check job, FR-30 fields for `GateOutOfScope` / `TriggerReviewOverdue`. No PRD change.

### DM-3 — Medium — `ContextUnavailable` also absorbs authorization denials in code
- Target: MIXED — CODE (preferred): split unauthorized from unavailable, report the latter as an unavailable call. PRD alternative if code is deferred:
- Location: FR-25 line 610 (append)
- ADD at end of line 610: "Until the shipped `ContextUnavailable` reason is split, a call it records for an authorization denial is transitional, is reclassified as an authorization denial for SM-C4, and is a non-conformance to be corrected additively before the first tenant is enabled."
- Decision needed: yes — code split versus PRD transitional acknowledgement.
- Co-located with: CI-7 (FR-9), Rubric-DN-SMC4 (line 960).

### DM-4 — Medium — Data-handling record, `DataHandlingVersion`, tenant acceptance, per-tenant enablement absent from code
- Target: CODE — `ProviderCatalogAggregate.cs`, `AgentActivationProviderRevalidation.cs`. No PRD change.

### DM-5 — Medium — Approver Policy structural rule and Conversation-scoped resolution absent
- Target: CODE — `AgentAggregate.cs`, gate orchestrator, `IApproverPolicyResolver`. No PRD change.

### DM-6 — Medium — No regeneration ceiling
- Target: CODE — regeneration orchestrator/aggregate. No PRD change.

### DM-7 — Medium — Pricing version: reused version accepted, caller-supplied version replaced
- Target: MIXED — CODE (reject an equal supplied version when units change) or PRD (align to server-assigned semantics):
- Location: FR-4 line 195
- Current text: "- Pricing metadata is required on model create and update: a well-formed currency, non-negative unit prices, and a pricing version that starts at 1 on create and strictly increases. A malformed currency, a negative unit price, or a decreased or reused pricing version is a typed rejection, not a silent acceptance."
- Proposed text (PRD branch): "- Pricing metadata is required on model create and update: a well-formed currency, non-negative unit prices, and a server-assigned pricing version that starts at 1 on create and increments on any unit-price or currency change. A malformed currency, a negative unit price, or a caller-supplied expected pricing version below the stored one is a typed rejection, not a silent acceptance; a caller-supplied version is never stored as given."
- Decision needed: yes — PRD branch (server-assigned) versus code branch (reject reuse).
- Co-located with: none.

### DM-8 — Medium — FR-25 counters and markers have no read-model fields
- Target: CODE — `AgentStatusView.cs`. No PRD change.

---

## Low

### AL-1 — Low — A-9's text omits the `ExternallyRemoved` clearing authority
- Target: PRD — same edit as CI-11 / MN-4 (row 858). Decision: none. Co-located with: CI-11, MN-4.

### AL-2 — Low — Minimum-sample discontinuity at five Parties
- Target: PRD
- Location: FR-28 line 700; A-17 row line 866
- Current text (line 700, fragment): "The minimum sample is 5 distinct calling Parties and 50 Agent Calls; for a tenant with fewer than 5 calling Parties it is every Party that called and 20 Agent Calls."
- Proposed text: "The minimum sample is 5 distinct calling Parties and 50 Agent Calls; for a tenant with fewer than 5 calling Parties it is every Party that called and 10 Agent Calls per calling Party, never fewer than 20 nor more than 50."
- A-17 row 866 "(every calling Party and 20 calls below 5 Parties)" → "(every calling Party and 10 calls per Party, 20–50, below 5 Parties)".
- Decision needed: yes — retunes the provisional A-17 sample (Product) and the Gate H-2 / addendum line 52 decision.
- Co-located with: AM-3, AM-4, AL-3, AL-4 (line 700).

### AL-3 — Low — Posting-failure rate can exceed 100% and miss cross-window failures
- Target: PRD
- Location: FR-28 line 700
- Current text (fragment): "The posting-failure rate is the share of proposals that entered `PostingFailed` at least once in the window over proposals that entered `Approved` in it, and automatic posts count as one approved and one failed on the same terms."
- Proposed text (with Rubric-DN-double and AH-5): "The posting-failure rate is measured over one cohort — the proposals that entered `Approved` in the window — as the share of that cohort that entered `PostingFailed` at least once within its retry bound, whenever that occurs; an automatic post (FR-11) counts as one entry into `Approved` and, if it entered `PostingFailed` at least once, one into the numerator."
- Decision needed: none.
- Co-located with: Rubric-DN-double, AH-5, AM-3, AM-4, AL-2, AL-4.

### AL-4 — Low — "Business day" undefined
- Target: PRD
- Location: FR-28 line 700; FR-33 row 471
- Current text (line 700, fragment): "within one business day of the condition;" → proposed: "within one business day — 24 hours excluding Saturday and Sunday in UTC — of the condition;"
- Current text (row 471): "| Convene and record the kill-switch trigger review (FR-28) | Release Operator, within one business day of the trigger condition | Tenant |" → proposed: "| Convene and record the kill-switch trigger review (FR-28) | Release Operator, within one business day (FR-28 defines it in UTC) of the trigger condition | Tenant |"
- Decision needed: none.
- Co-located with: AM-3 (line 700).

### AL-5 — Low — Cost-cap override "is itself bounded" with no bound
- Target: PRD
- Location: FR-32 line 719
- Current text: "- An audited administrative override can restore Agent Calls for a tenant that has reached its monthly cap; the override records actor, justification, scope, and expiry, and is itself bounded."
- Proposed text: "- An audited administrative override can restore Agent Calls for a tenant that has reached its monthly cap; the override records actor, justification, scope, and expiry, and is itself bounded to at most 25% of the monthly cap and 7 days per override; a second override for the same tenant in the same month requires Product's recorded approval [ASSUMPTION A-25]."
- ADD §8.1 row: "| A-25 | Cost-cap override bound: 25% of the monthly cap and 7 days per override; a second in the same month needs Product approval | FR-32 | Product + Release PM | Product confirms or retunes before enablement |"
- Decision needed: none (values are a tagged assumption).
- Co-located with: none.

### AL-6 — Low — FR-7 two-pass abandonment not scoped to awaiting states
- Target: PRD — same edit as CI-6 (line 255). Decision: none. Co-located with: CI-6.

### AL-7 — Low — Regeneration re-runs FR-9 and FR-27 but not Eligible Approver resolution or membership
- Target: PRD
- Location: FR-16 line 394 (second sentence)
- Current text: "It re-runs the FR-9 Safe Context Budget check and the FR-27 pre-Provider scan on the same terms as the original call, against the Conversation as it stands at regeneration time."
- Proposed text: "It re-runs FR-8 steps 2 through 9 (2 through 10 under the AM-5 order) — including the FR-9 Safe Context Budget check, the FR-27 pre-Provider scan, Eligible Approver resolution, and the membership step — on the same terms as the original call, against the Conversation as it stands at regeneration time."
- Decision needed: none (step range follows AM-5's decision).
- Co-located with: AM-5.

### AL-8 — Low — Block from `NeverJoined` confirmed without a seam call although `hexa` may be present
- Target: PRD
- Location: FR-2 line 164 (last clause)
- Current text (fragment): "and a block set from `NeverJoined` is confirmed without a seam call."
- Proposed text: "and a block set from `NeverJoined` is confirmed without a seam call when the participant read confirms `hexa` absent, and is otherwise mirrored."
- Decision needed: none.
- Co-located with: AM-7 (line 164).

### AL-9 — Low — Policy publication and HMAC rotation invalidate every cached verdict with no rescan budget
- Target: PRD
- Location: FR-27 line 668
- Anchor (fragment): "Publishing a policy version invalidates every cached verdict tenant-wide, and an edited or deleted message no longer matches its hash, so no Conversations notification is needed."
- ADD after it (same bullet): "Policy publication and HMAC secret rotation start a background re-scan under a per-tenant concurrency bound; an Agent Call during the re-scan waits for its Conversation's verdict or fails closed with `ContextUnavailable`, and never posts under a stale verdict."
- Decision needed: none.
- Co-located with: AM-10 (line 669), CI-7.

### AL-10 — Low — A caller rejected `NoEligibleApprover` gets no remediation hint (M-10)
- Target: PRD
- Location: FR-7 line 259
- Current text: "- `hexa` is unavailable, with a typed `NoEligibleApprover` rejection, in a Confirmation Response Mode Conversation whose Participants yield no Eligible Approver. That trade-off is accepted deliberately, on the same terms as OQ-10: FR-25 and SM-C4 count these rejections so the unavailability is visible rather than silent."
- Proposed text: "- `hexa` is unavailable, with a typed `NoEligibleApprover` rejection, in a Confirmation Response Mode Conversation whose Participants yield no Eligible Approver. The rejection carries a disclosure-category-safe remediation hint — "no Facilitator in this Conversation" or "the configured Approvers are not Participants here" — and never names a Party or role holder. That trade-off is accepted deliberately, on the same terms as OQ-10: FR-25 and SM-C4 count these rejections so the unavailability is visible rather than silent."
- Decision needed: none (M-10 was Product-deferred; the fix discloses nothing beyond the operator-only category).
- Co-located with: none.

### AL-11 — Low — FR-2 line 158's negative sentences are history
- Target: PRD — same edit as AH-2 (line 158). Decision: none. Co-located with: AH-2, MN-3.

### AL-12 — Low — Carried lows still open (L-1, L-4, L-5, L-6, L-7, L-9, L-11, gate L-2)
- Target: PRD
- Location: FR-16 line 394 (L-1), FR-27 line 668 (L-4), NFR-9 line 791 (L-5), SM-3 line 947 (L-6), FR-33 line 453 / row 468 (L-7), FR-26 line 634 (L-7), NFR-13 line 795 (L-9), UJ-1 line 50 (gate L-2)
- Gate L-2 — Current text (line 50, fragment): "enables the `hexa` that the Platform Operator provisioned at tenant enablement (FR-1)," → proposed: "activates the `hexa` that the Platform Operator provisioned at tenant enablement (FR-1, FR-3),"
- L-6 — Current text (line 947, fragment): "within the lesser of the Agent's configured expiry and 24 hours," → proposed: "within the lesser of the proposal's stored expiry duration at creation and 24 hours,"
- L-7 — Current text (row 468): "| Publish or version the Content Safety Policy (FR-26) | Platform Operator with Security approval; a tenant may only add restrictions through a stricter mode-specific policy | Platform |" → proposed: "| Publish or version the Content Safety Policy (FR-26) | Platform Operator with the recorded approval of the Security owner named in the launch readiness register, attached to the policy version as Audit Evidence; a tenant may only add restrictions through a stricter mode-specific policy | Platform |"
- L-1, L-4, L-5, L-9, L-11: each keeps the fix recorded at the run that raised it (`change-extract-validation-2026-09-08.md`, `reconcile-validation-2026-09-09-2.md`); no new wording from this report.
- Decision needed: yes — Product-deferred carry-overs; apply the three drafted here or carry again.
- Co-located with: AL-7 (394), AL-9 (668), Rubric-DN-p99 (791), AH-4 (947), Rubric-DR-PM (453).

### AL-13 — Low — A-21 can be retired by relocating the check to Agents with no FR requiring it
- Target: PRD
- Location: FR-2, ADD after line 159
- Anchor (line 159): "- Membership is added under the Agents Service Principal, not the caller's identity, and is idempotent: a repeated join for the same Agent and Conversation is a no-op that returns the existing membership."
- ADD after it: "- Where the register assigns the AI-type verification to Agents rather than Conversations (A-21), the membership step reads the Party type from Hexalith.Parties before the join and fails closed with `MembershipRejected` on a non-AI Party."
- Decision needed: none.
- Co-located with: AH-2 / AH-3 (FR-2 block).

### CI-11 — Low — A-9 index row lags the FR-2 / FR-33 clear rule
- Target: PRD
- Location: §8.1 row line 858; OQ-16 line 986
- Current text (row 858): "| A-9 | Block authority for `hexa` is Tenant Agent Administrator plus Conversation Facilitator, and a block is cleared only by the setting authority or the Tenant Agent Administrator | FR-2, FR-33 | Product | Product confirms the FR-33 rows |"
- Proposed text: "| A-9 | Block authority for `hexa` is Tenant Agent Administrator plus Conversation Facilitator; a block is cleared only by the setting authority or the Tenant Agent Administrator; an `ExternallyRemoved` record is cleared by the Tenant Agent Administrator or any current Conversation Facilitator | FR-2, FR-33 | Product | Product confirms the FR-33 rows |"
- OQ-16 line 986 fragment "and cleared only by the setting authority or the Tenant Agent Administrator (A-9)." → "and cleared only by the setting authority or the Tenant Agent Administrator, an `ExternallyRemoved` record by the Tenant Agent Administrator or any current Facilitator (A-9)."
- Decision needed: none.
- Co-located with: AL-1, MN-4 (row 858); AH-2/AH-3 (OQ-16).

### CI-12 — Low — §6.1 still lists "provisioning or linking"
- Target: PRD
- Location: §6.1 line 752
- Current text: "- Agent Party identity provisioning or linking through Hexalith.Parties." → Proposed: "- Agent Party identity provisioning through Hexalith.Parties at tenant enablement (FR-1)."
- Decision needed: none. Co-located with: none.

### CI-13 — Low — FR-12 names the block but not `ExternallyRemoved`
- Target: PRD
- Location: FR-12 line 334
- Current text (fragment): "the Agents-owned block (FR-2)," → Proposed: "the Agents-owned block or `ExternallyRemoved` record (FR-2),"
- Decision needed: none. Co-located with: none.

### CI-14 — Low — FR-22 lists "failed-call" among proposal states
- Target: PRD
- Location: FR-22 line 542
- Current text (fragment): "and the awaiting-decision, posting-failed, failed-call, and expired proposal states." → Proposed: "the awaiting-decision, posting-failed, and expired proposal states, and the failed-call outcome (FR-10)."
- Decision needed: none. Co-located with: none.

### CI-15 — Low — OQ-22 narrows the assumption owners that block `RQ-1`
- Target: PRD
- Location: OQ-22 line 992 (last sentence)
- Current text: "Unretired Product, Architecture, or Governance assumptions in §8.1, and unretired `ARCH-A` rows in the Architecture Spine, block `RQ-1`." → Proposed: "Every unretired §8.1 assumption, whatever its owner, and every unretired `ARCH-A` row in the Architecture Spine, blocks `RQ-1`."
- Decision needed: none. Co-located with: CX-13 (Spine AD-17 same wording).

### CI-16 — Low — `GateOutOfScope` and `TriggerReviewOverdue` classified neither as register vocabulary nor as blocker values
- Target: PRD
- Location: FR-28 line 690
- Anchor (fragment): "because the runtime must refuse enablement on them without consulting the register."
- ADD after it: "`GateOutOfScope`, `TriggerReviewOverdue`, `SuspensionReviewOverdue`, and `DeferredAssumption` are register vocabulary that the FR-30 surface mirrors."
- Decision needed: none. Co-located with: AM-2, AC-1 (names), FR-30 line 621.

### CI-17 — Low — Tenant-role calling restriction (A-11) has no configuration home
- Target: PRD — same edits as Rubric-DN-calling (row 461, §3 line 83, §10 line 896). Decision: none. Co-located with: Rubric-DN-calling, CI-18.

### CI-18 — Low — Glossary Agent Administrator omits lowering caps and limits
- Target: PRD
- Location: §3 line 83
- Current text (fragment): "lifecycle, expiry duration, regeneration ceiling, and acceptance of a Provider's data-handling record for the tenant." → Proposed (with CI-17): "lifecycle, expiry duration, regeneration ceiling, the tenant-role calling restriction (A-11), acceptance of a Provider's data-handling record for the tenant, and lowering — never raising — the tenant's cost caps, rate limits, and concurrency bound (FR-32)."
- Decision needed: none. Co-located with: CI-17, Rubric-DN-calling.

### CX-13 — Low — Spine AD-17 narrows the `UnretiredAssumption` owner set
- Target: OTHER-ARTIFACT — Spine AD-17 line 225: "whatever its owner". PRD side is CI-15.

### CX-14 — Low — Register vocabulary row describes `PayloadProtectionUnavailable` in host-report terms
- Target: OTHER-ARTIFACT — launch-readiness-register.md line 53: attestation wording (as its line 78). No PRD change.

### CX-15 — Low — Epics `AIAgent` spelling and unratified projection ids
- Target: OTHER-ARTIFACT — epics.md lines 144, 160. No PRD change.

### CX-16 — Low — Spine AD-14 attestation summary omits DEK-destroy and `Erased` replay
- Target: OTHER-ARTIFACT — Spine line 205. No PRD change.

### CX-17 — Low — Proposal says "29-story structure"; epics says 33
- Target: OTHER-ARTIFACT — sprint-change-proposal-2026-09-09-2.md one-line note. No PRD change.

### CX-18 — Low — Register `LR-PARTY-IDENTITY` invalidated by retired "identity-link changes"
- Target: OTHER-ARTIFACT — launch-readiness-register.md line 100: "Party identity or provisioning changes". No PRD change.

### CX-19 — Low — Every Architecture-owned open Spine row reads "TBD — target milestone" (requirement unmet, self-reported)
- Target: OTHER-ARTIFACT — Spine ARCH-A table: co-owners supply literal dates (proposal §6). No PRD text change beyond AC-1's "a literal date, not a milestone".

### Rubric-DN-double — Low — [Rubric: Done-ness clarity] Posting-failure rate counts every automatic post twice
- Target: PRD — same edit as AL-3 (line 700). Decision: none. Co-located with: AL-3, AH-5.

### Rubric-DN-lookup — Low — [Rubric: Done-ness clarity] `Approved → Abandoned` requires a lookup for a proposal that never posted
- Target: PRD — same sentences as CI-10 (line 440); the rubric's skip-for-`Approved` form is the recommended text there. Decision: yes (see CI-10). Co-located with: CI-10, AH-4.

### Rubric-DN-SMC4 — Low — [Rubric: Done-ness clarity] SM-C4's "exactly" list has no class for FR-8 step 2–3 rejections
- Target: PRD
- Location: SM-C4 lines 957 and 960
- Current text (line 957): "  - Blocked calls, whose reasons are exactly: Conversation Context Policy, Content Safety Policy, cost caps, rate limits, `NoEligibleApprover`, and `RemovedInConversations` or the Agents-owned block. §3 and FR-25 cite this list."
- Proposed text: "  - Blocked calls, whose reasons are exactly: Conversation Context Policy, Content Safety Policy, cost caps, rate limits, a lapsed `DataHandlingVersion` acceptance (FR-4), `NoEligibleApprover`, and `RemovedInConversations` or the Agents-owned block. §3 and FR-25 cite this list."
- Current text (line 960, opening): "Authorization denials, capacity queueing or rejection, `DependencyNotAvailable`, and `PayloadProtectionUnavailable` are in none of the three classes and are reported separately."
- Proposed text: "Authorization denials, Agent lifecycle and Provider/model eligibility rejections (FR-8 steps 2–3), capacity queueing or rejection, `DependencyNotAvailable`, and `PayloadProtectionUnavailable` are in none of the three classes and are reported separately."
- Current text (line 960, fragment): "the FR-28 trigger review uses this share over distinct (Party, Conversation, reason) tuples with rate-limit rejections excluded." → Proposed (MN-2): "the FR-28 trigger review uses this share over distinct (Party, Conversation, reason) tuples, excluding only rejections at a rate limit set by the Platform Operator or Release Operator (FR-28)."
- Decision needed: none (step numbers follow AM-5).
- Co-located with: MN-2 (line 960), AM-10 (957), AM-5 (step numbers), FR-25 line 610.

### Rubric-DN-adjectives — Low — [Rubric: Done-ness clarity] Residual adjective-only phrasing (deferred, open)
- Target: PRD
- Location: FR-4 line 193; FR-20 line 510; FR-25 line 606
- Current text (line 193): "- Existing Agents using a disabled Provider/model cannot be activated or called until reconfigured. A documented migration state may allow temporary read-only inspection." → Proposed: "- Existing Agents using a disabled Provider/model cannot be activated or called until reconfigured; their configuration remains inspectable read-only under the FR-33 configure row."
- Current text (line 510): "- Authorization decisions are auditable at a level sufficient to explain denial or approval without leaking sensitive content." → Proposed: "- Every authorization decision records the role basis, the FR-33 row, and the typed denial or approval reason, without leaking sensitive content."
- Current text (line 606): "- Status surfaces support launch monitoring of adoption and approval workflow metrics." → Proposed: "- Status surfaces expose the SM-2, SM-3, SM-7, SM-C4, and SM-C5 inputs defined in §12."
- Decision needed: none.
- Co-located with: DM-3 (FR-25).

### Rubric-DN-calling — Low — [Rubric: Done-ness clarity] Tenant-role calling restriction has no configuration home
- Target: PRD
- Location: FR-33 row 461; §10 line 896; §3 line 83 (CI-18)
- Current text (row 461, fragment): "Conversation Context Policy, lifecycle, proposal expiry duration, regeneration ceiling | Tenant Agent Administrator | Tenant |" → Proposed: "Conversation Context Policy, lifecycle, proposal expiry duration, regeneration ceiling, and the tenant-role calling restriction (A-11) | Tenant Agent Administrator | Tenant |"
- Current text (line 896, fragment): "proposal expiry duration, regeneration ceiling, and Conversation Context Policy;" → Proposed: "proposal expiry duration, regeneration ceiling, Conversation Context Policy, and the tenant-role calling restriction (A-11);"
- Decision needed: none. Co-located with: CI-17, CI-18.

### Rubric-SH-undated — Low — [Rubric: Scope honesty] Thirteen assumptions retire on a condition with no date
- Target: PRD
- Location: §8.1 rows A-9..A-12 (lines 858–861), A-1..A-4, A-15, A-16, A-18, A-21 (register-conditioned rows)
- Proposed: rows A-9, A-10, A-11, A-12 "Retired when" cells → append " by [date Product sets; single due date for the FR-33 confirmations, on the A-14 pattern]"; register-conditioned rows → append " (target: the register's `TargetIntegrationDate` for that entry)" to each "Retired when" cell, e.g. A-1: "`EXT-CONV-AI-1` reaches `Committed` with these names, or the register records the final names (target: the register's `TargetIntegrationDate`)".
- Decision needed: yes — Product sets the one due date for the FR-33 confirmations.
- Co-located with: AC-1 (line 838 date rule), DL-1 (A-1), AM-7 (A-1), AH-3 (A-22).

### Rubric-SH-seams — Low — [Rubric: Scope honesty] §8 says two seams are pending; register carries both
- Target: PRD-resync — same edit as CX-3 (lines 818, 820). Decision: none. Co-located with: CX-3, AH-9.

### Rubric-DU-count — Low — [Rubric: Downstream usability] §8.1 states a Spine row count the Spine does not have (reopened)
- Target: PRD-resync — same edit as CX-4 (line 836; no number). Decision: none. Co-located with: CX-4, MN-1.

### Rubric-DU-FR21 — Low — [Rubric: Downstream usability] Process gates remain inside FR-21 (deferred, open)
- Target: PRD
- Location: FR-21 lines 521–523 and 525 (first sentence); §8 after line 812
- Current text (lines 521–523): "- A critical external dependency is not implementation-ready until its external dependency register entry satisfies every commitment field defined in §8.\n- An `Uncommitted` dependency, missing target, or missing compatibility verification command blocks every consuming story from `ready-for-dev`.\n- A story completed while a dependency it consumes was `Uncommitted` is recorded as non-conformant against this gate, with the dependency, the story, and the date captured in the dependency register. Narrowing a story's scope to avoid executing the seam does not clear the non-conformance record."
- Proposed: delete the three bullets from FR-21 and ADD to §8 after line 812 as a paragraph: "Register gate rules (moved from FR-21): a critical external dependency is not implementation-ready until its register entry satisfies every commitment field above; an `Uncommitted` dependency, missing target, or missing compatibility verification command blocks every consuming story from `ready-for-dev`; a story completed while a dependency it consumes was `Uncommitted` is recorded as non-conformant against this gate, with the dependency, the story, and the date captured in the register, and narrowing a story's scope to avoid executing the seam does not clear that record (OQ-17)."
- Current text (line 525, first sentence): "- `Committed` unblocks `ready-for-dev` only." → keep in §8 (move); FR-21 line 525 keeps from "No runtime, test, or qualification path may execute…" onward, and line 526 stays.
- Decision needed: yes — structural move declined by earlier polish runs; Product accepts the relocation.
- Co-located with: CX-2 / CX-7 (line 814 follows the new paragraph).

### DL-1 — Low — Narrow A-1: `ParticipantType.AiAgent` is the compiled Conversations spelling
- Target: PRD
- Location: §8.1 row line 850; §8 seam 1 line 817 tag
- Current text (row 850): "| A-1 | Conversations publishes `IConversationClient.AddParticipantAsync`, the participants route, `ParticipantType.AiAgent`, and `ParticipantRole.Member` as named | §8 | Conversations Maintainer | `EXT-CONV-AI-1` reaches `Committed` with these names, or the register records the final names |"
- Proposed text (with AM-7): "| A-1 | Conversations publishes `IConversationClient.AddParticipantAsync`, the participants route, and `ParticipantRole.Member` as named, and authorizes the Agents Service Principal to remove the `AiAgent` participant; `ParticipantType.AiAgent` is already compiled against and is not assumed | §8 | Conversations Maintainer | `EXT-CONV-AI-1` reaches `Committed` with these names and the removal authorization, or the register records the final names |"
- Current text (line 817, tag): "[ASSUMPTION A-1: the register owns the final names; `AiAgent` is the spelling this PRD uses]" → Proposed: "[ASSUMPTION A-1: the register owns the final names of the add operation, route, and `Member` role, and the removal authorization for the Agents Service Principal; `ParticipantType.AiAgent` is the compiled Conversations spelling]"
- Decision needed: none (verdict given by the report).
- Co-located with: AM-7, Rubric-SH-undated (row 850, line 817).

### DL-2 — Low — `AgentSetupWriteStatus.Submitted = 0` remains, as the PRD records
- Target: CODE — before enablement; nothing for the PRD.

### DL-3 — Low — FR-26 "more restrictive" has no code and no retry path
- Target: CODE — with DH-4 and DC-4. No PRD change (wording is CI-1).

---

## Additional Low — Mechanical notes (MN-1..MN-6)

### MN-1 — Low — §8.1 versus Spine (row count stale, defect list stale, AD-13 unlisted)
- Target: PRD-resync — same edits as CX-4 (line 836), CX-5 and Rubric-DU-8.1 (lines 840–844), AH-9 closure rule. Decision: see Rubric-DU-8.1. Co-located with: CX-4, CX-5, AH-9, Rubric-DU-8.1.

### MN-2 — Low — Typed-name consistency (`MembershipUnavailable` at acceptance vs re-validation; `DeletionDeferredByHold` only in §9; SM-C4 rate-limit exclusion)
- Target: PRD
- Location: FR-2 line 160; FR-30 line 621; §10 line 903; SM-C4 line 960
- Current text (line 160, fragment): "`MembershipUnavailable` (the participant read is unavailable) and `MembershipRejected` (Conversations refused the Agents Service Principal for a reason that is not a removal) remain posting-failure reasons for that re-validation only;"
- Proposed text: "At the pre-post re-validation, `MembershipUnavailable` (the participant read is unavailable) and `MembershipRejected` (Conversations refused the Agents Service Principal for a reason that is not a removal) are posting-failure reasons; at acceptance, `MembershipUnavailable` is a call rejection (above);"
- Line 621 (with AM-2): the "surface also records" sentence adds `DeletionDeferredByHold` (§9): "The surface also records `GateOutOfScope`, `TriggerReviewOverdue`, `SuspensionReviewOverdue`, and `DeferredAssumption` conditions (FR-28) and any `DeletionDeferredByHold` signal awaiting hold release (§9)."
- Current text (line 903): "- Legal hold: apply, inspect, and release holds on retained Agent content." → Proposed: "- Legal hold: apply, inspect, and release holds on retained Agent content, including any `DeletionDeferredByHold` signal awaiting release (§9)."
- Line 960: see Rubric-DN-SMC4 proposed text (align to FR-28's Platform/Release-set exclusion).
- Decision needed: none.
- Co-located with: AM-2, AC-1, CI-16 (line 621); Rubric-DN-SMC4 (960); AH-2 (FR-2 block).

### MN-3 — Low — FR-2 `Joined` detection when no mirror ever existed
- Target: PRD — same edit as AH-2 (line 154: "A Conversation with no mirror history satisfies "the last mirror is confirmed""). Decision: none. Co-located with: AH-2, AL-11.

### MN-4 — Low — A-9 row versus FR-33 (index row lacks the `ExternallyRemoved` clause; OQ-16 too)
- Target: PRD — same edits as CI-11 / AL-1 (row 858, OQ-16 line 986). Decision: none. Co-located with: CI-11, AL-1.

### MN-5 — Low — §0 versus §8 versus register (internally consistent, both wrong)
- Target: PRD-resync — same edits as DC-2 / CX-2 (lines 16, 814). Decision: none. Co-located with: DC-2, CX-2, Rubric-DR-HOST, AH-9.

### MN-6 — Low — Glossary drift (lowercase "Agent response(s)" ×4, "Agent call" ×3, "Agent calling" ×1, "eligible Conversations" ×3, "Release PM" without entry)
- Target: PRD
- Location: §2.2 line 41, UJ-2 line 60, FR-19 line 498, §6.2 line 776 ("Agent responses"/"Agent response"); UJ-3 line 69, FR-8 line 273, FR-33 row 462 ("Agent call"); FR-20 line 504 ("Agent calling"); SM-2 line 949 ×2, OQ-11 line 981 ("eligible Conversations"); §3 (Release PM — see Rubric-DR-PM)
- Current → proposed: line 41 "adding Agent responses to Conversations" → "adding Agent Responses to Conversations"; line 60 "with the Agent response visible" → "with the Agent Response visible"; line 498 "or post Agent responses for another tenant" → "or post Agent Responses for another tenant"; line 776 "beyond adding Agent responses to Conversations" → "beyond adding Agent Responses to Conversations"; line 69 "a new Agent call is required" → "a new Agent Call is required"; line 273 "and Agent call permission" → "and Agent Call permission"; row 462 "is the Agent call permission that FR-8 and FR-20 enforce" → "is the Agent Call permission that FR-8 and FR-20 enforce"; line 504 "Agent calling, proposal discovery," → "Agent Calls, proposal discovery,"; lines 949 and 981 "eligible Conversations" → "Eligible Conversations". Glossary heading "Provider/Model Selection" (line 112) → "Provider/model selection" to match the seven body uses.
- Decision needed: none.
- Co-located with: Rubric-DR-PM (glossary), UJ-1 (AL-12 gate L-2, line 50).

---

## Entries with `Decision needed` ≠ none

1. AC-1 — invert the late-`ARCH-A` default (reverses Gate H-6 of 2026-09-09); actor name follows the Release PM decision.
2. AH-3 — (a) assumption-gated auto re-admission with `A-22` or (b) clear-only; amends Gate M-1/M-I2 "presence again re-admits".
3. AH-4 — human abandon from `Approved` and a `PostingWindowElapsed` staleness bound; revises Conflict 10 ("never expire"), conflicts with Spine AD-5 "never a human abandon", reopens Product-deferred M-12.
4. AH-6 — loosen-blocks-immediately / tighten-30-day-grace; revises OQ-29 / A-10 and the H-3 / Gate M-3 decisions.
5. AH-7 — Release-Operator-owned committed-engine value via `EXT-SECRETS-1`; extends Gate H-5 and the `EXT-SECRETS-1` register scope.
6. AH-8 — inspection aggregation bound and the unstated inspection-rate threshold value (A-23); folds gate L-6.
7. AM-5 — FR-8 reorder (renumbers steps cited by FR-32, SM-C4, AL-7, Spine AD-13); M-6 was Product-deferred.
8. AM-7 — `MirrorRefused` terminal mirror outcome versus OQ-25 / Gate M-2 "retried until it succeeds".
9. AM-9 — protect Agent Instructions under an Agent-level key, or keep unprotected with erasure on offboarding (new OQ-31); touches Gate H-5 (M-10 folded).
10. AM-11 — (a) fourth `EXT-CONV-UI-1` artifact kind or (b) drop the Conversation status entry; M-13 was Product-deferred.
11. AM-12 — human-only Approver sources; M-1 was Product-deferred.
12. AM-13 — scan at edit time with `SafetyFailed` marker; M-2 was Product-deferred ("deferred, not rejected").
13. CI-1 / Rubric-DN-restrictive — keep "more restrictive" (CI-1, memlog 2026-09-08, Spine AD-20 `LastLoosensVersion`) or adopt the computable "pass every applicable version" form (recommended).
14. CI-8 — Product confirms the `Disabled`-Agent retry pause (retires Spine `ARCH-A-11`).
15. CI-9 / Rubric-DR-PM — glossary equivalence Release PM = Release Operator plus two FR-33 rows, or rename fourteen uses.
16. CI-10 / Rubric-DN-lookup — perform the `MessageId` lookup from `Approved` (Spine AD-5 form) or skip it because no post was attempted (recommended); the two fixes are incompatible.
17. Rubric-DN-FR31 — narrow FR-31 / OQ-9 to first-person impersonation and unrecorded decisions; Product-deferred, amends OQ-9.
18. Rubric-DN-AgentCall — confirm `AgentInteractionStatus` as the public contract and its terminal-member list; `Proposal*` duplicates to the FR-23 register.
19. Rubric-DN-p99 — gate on p95 at n ≥ 30 (p99 at n ≥ 300) or require n ≥ 300 for p99; amends OQ-5.
20. Rubric-DU-8.1 — record AD-13 as a Spine defect (CX-1) or adopt its Automatic-mode rule into FR-8; and pointer-only (AH-9) versus pointer plus one dated AD-13 sentence.
21. DM-3 — code split of `ContextUnavailable` versus a PRD transitional acknowledgement.
22. DM-7 — PRD aligns to server-assigned pricing version, or code rejects an equal supplied version.
23. AL-2 — retune the A-17 small-tenant sample to 10 calls per Party (20–50); amends Gate H-2 / addendum line 52.
24. AL-12 — apply the three drafted carried lows (gate L-2, L-6, L-7) or carry again; Product-deferred.
25. Rubric-SH-undated — Product sets the single due date for the A-9..A-12 FR-33 confirmations.
26. Rubric-DU-FR21 — accept moving the register gate rules from FR-21 to §8 (previously declined as reorganization).
27. CX-7 (register side) — Product selects Branch A or B for Story 5.3 / `EXT-PROVIDER-1` (correct-course; PRD text needs no decision).
28. AM-3 — none required, but the 7-day / 10-point review validity values are new A-17 provisional numbers Product may retune.
29. AL-5 — none required, but the 25% / 7-day override bound is a new tagged assumption (A-25) Product may retune.
30. AM-15 — none required, but the hourly attestation cadence is a new tagged assumption (A-24).
31. AM-2 — none required, but `SuspensionReviewOverdue` and the containment-finding release condition tighten the Gate H-5 release rule.

## Findings not located in prd.md

None. Every PRD-targeted finding resolved to current text; the only off-document quotes are addendum.md lines 48–49 (CX-5), which were located. Report line references that no longer match exactly (the report cites FR-28 line 700 for the trigger review and FR-18 lines 425–433 for the table, both confirmed at those lines) needed no relocation.
