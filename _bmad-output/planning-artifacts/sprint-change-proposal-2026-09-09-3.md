---
title: Sprint Change Proposal - Third 2026-09-09 PRD Downstream Reconciliation
status: approved
created: 2026-09-10
updated: 2026-09-11
mode: Batch
change_scope: moderate
recommended_path: direct-adjustment
project: agents
owner: Administrator
approval_required: false
approval_status: approved
approved_by: Administrator
approved_on: 2026-09-11
execution_status: approved-awaiting-implementation
routed_to:
  - Product Owner
  - Solution Architect
  - Release Operator
  - Release PM
  - Parties Maintainer
  - Conversations Maintainer
  - Platform Maintainer
  - EventStore Maintainer
  - Security Engineering
  - Agents Runtime Maintainer
  - Test Architect
  - UX Owner
trigger_artifacts:
  - prds/prd-agents-2026-06-23/update-report-2026-09-09-3.md
  - prds/prd-agents-2026-06-23/reconcile-validation-2026-09-09-3.md
  - prds/prd-agents-2026-06-23/review-implementation-drift.md
governing_authority:
  - prds/prd-agents-2026-06-23/prd.md
amends_if_approved:
  - prds/prd-agents-2026-06-23/prd.md
  - architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
  - external-dependency-register.md
  - launch-readiness-register.md
  - epics.md
  - sprint-change-proposal-2026-09-09-2.md
  - ../implementation-artifacts/sprint-status.yaml
preserves:
  - FR-1 through FR-34, OQ-1 through OQ-31, and A-1 through A-28 as governing Product authority; the PRD edits proposed here are cross-document resyncs, not requirement changes
  - the V1 thesis, MVP scope, and the Epic 5 through 8 outcomes
  - completed Epics 1 through 4 as historical evidence
  - the 33 active stories of Epics 5 through 8 as they stand, plus one added provisioning story
  - RQ-1 as an operational release gate outside the story backlog
  - the UX spines, which a concurrent UX Update run is amending
---

# Sprint Change Proposal: Third 2026-09-09 PRD Downstream Reconciliation

## 1. Issue Summary

The third 2026-09-09 update of the Hexalith Agents PRD (`prd.md`, `status: final`, `updated: 2026-09-09`, 1081 lines) made the PRD the authority for a change set that the other planning artifacts have not absorbed. The update report and its reconciliation record 124 findings: 80 applied to the PRD, 5 partially applied, 3 applied with deliberate divergence, 14 routed to other artifacts, and 22 routed to code. The reviewer gate that followed added further routing for the registers, the Spine, the epics, and code.

The change set introduces vocabulary and rules that downstream artifacts either lack or contradict: `DeferredAssumption`, `PostingWindowElapsed`, `MirrorRefused`, `RestrictedContent`, `ContextReadUnavailable`, `TenantSuspended`, `DataHandlingAcceptanceLapsed`, and `SuspensionReviewOverdue`; the Release PM as a distinct launch-governance party with two `RQ-1` powers; the FR-8 ten-step acceptance order; the system-approved posting record for automatic posts; the durable `PostingPending` deadline; the A-22 re-admission gate and roster-version freshness rule; the Security-qualified signed engine identity custodied through `EXT-SECRETS-1`; the declared-tightening rule for Provider data handling; and the new `EXT-PARTIES-1` dependency with its interim by-id identity rule.

Two facts changed after the update report was written and shape this proposal:

1. The Architecture Spine received an uncommitted round-5 update on 2026-09-10 (`updated: 2026-09-10`, `architecture_assumption_index_version` 3 to 5, AD-1..AD-31, `binds` OQ-1..OQ-31). It closed most of the PRD §8.1 divergence list by AD number. The PRD's own §8.1 pointer (`updated: 2026-09-09`) and divergence list are therefore stale in the other direction, which is one reason `prd.md` must be listed under `amends_if_approved`.
2. Product approved Branch B for Story 5.3 / `EXT-PROVIDER-1` on 2026-09-10, and the external-dependency register records it. The PRD §8 sentence that still calls the open approval a `ready-for-dev` blocker is stale.

### Trigger Evidence

| Evidence | Current artifact state (verified on disk 2026-09-10) | Required consequence |
| --- | --- | --- |
| PRD §8.1 divergence list (lines 897-908) versus the Spine round-5 update report | The Spine now carries AD-5 human abandon, `PostingWindowElapsed`, the `PostingPending` stored deadline, the automatic posting record, the AD-12 distinct-Party share and 3-Party floor, the AD-13 ten-step order scoped to Confirmation mode, the AD-14 signed-identity pin and A-24 cadence, the AD-21 "step 5" citation, seam 7, and the AD-17 vocabulary rows. The PRD still lists these as open and cites the Spine at `updated: 2026-09-09` | Resync the PRD §8.1 pointer and divergence list to what round 5 closed and what it left open |
| Spine residuals (agent verification against the PRD) | AD-7/AD-2 leave `MirrorRefused` undefined, say the acceptance path never yields `MembershipUnavailable`/`MembershipRejected`, conflate `ExternallyRemoved` with a mirrored block, and contradict FR-2's post-clear rule; AD-17 still limits `UnretiredAssumption` to Product, Architecture, and Governance owners; AD-20's lineage rule can skip the current policy; AD-21 lacks the single settlement instant; AD-22 says "no further automatic escalation"; AD-10/AD-2 carry no `DataHandlingVersion` model; the Conventions section says `AgentSetupWriteStatus` gains `Unknown = 0` additively, which FR-23 forbids; eleven Architecture-owned `ARCH-A` rows still carry `TBD` or a milestone | Amend the residual AD text, the Conventions register clause, and the Architecture Assumptions preamble; obtain literal dates from the co-owners |
| `external-dependency-register.md` (`updated: 2026-09-10`, uncommitted) | Only the Branch B ruling landed. `EXT-PARTIES-1` is absent; `EXT-CONV-AI-1` still says "Six seams" and verifies the Party "is AI type"; `EXT-SECRETS-1` custodies no engine identity value; `EXT-CONV-UI-1` still says "Three artifact kinds"; the register has no data-handling text at all | Add the entry, the seam-7 and seam-1 text, the `EXT-SECRETS-1` scope, the fourth artifact kind, and the OQ-17 non-clearing clause |
| `launch-readiness-register.md` (`updated: 2026-09-09`) | The 2026-09-09-2 proposal's edits are fully applied. `DeferredAssumption` and `SuspensionReviewOverdue` are absent and fail closed under the register's unknown-code rule; there is no record kind for the `RQ-1` scheduling event, deferral acceptance, SM-4 containment review, or engine qualification; the Release PM appears once, as a confirmer; `PayloadProtectionUnavailable` uses host-report wording the PRD rejects; `TriggerReviewOverdue` is report-only; `LR-PARTY-IDENTITY` still says "identity-link changes"; the Provider Readiness Contract has no data-handling gate; NFR-9 lacks the nearest-rank and p99 rules | Add the vocabulary, emitter, and record-kind rows; correct the attestation wording; add the data-handling gate; align NFR-9 |
| `epics.md` (`updated` 2026-09-09; 33 stories in Epics 5-8, 59 total, matching `sprint-status.yaml` exactly) | The 2026-09-09-2 proposal's edits are applied. Inventory rows FR4, FR5, FR12, FR22, FR25, and FR27 predate the current FR text; `Draft`, `MembershipUnavailable`, `MembershipRejected`, the posting record, the `PostingPending` deadline, `PostingWindowElapsed`, `TriggerReviewOverdue` as an activation block, `DeferredAssumption`, `SuspensionReviewOverdue`, A-22..A-28, OQ-31, and `EXT-PARTIES-1` have no story text; no story creates the Agent and its Party atomically since the link command's provisioning leg was retired | Rewrite the six rows, add the story criteria, and add one provisioning story |
| `sprint-change-proposal-2026-09-09-2.md` (`updated: 2026-09-10`, uncommitted) | Says "29-story structure" and "29 active stories" at four places although the first 2026-09-09 proposal had already added Stories 5.9, 5.10, 6.8, and 7.7 (33). It preserves `prd.md` unchanged, so no approved proposal schedules the PRD resync edits | Correct the count; schedule the PRD edits through this proposal |
| `review-implementation-drift.md` and the 2026-09-09-3 extract | 22 prior code-side drift findings (5 critical) remain open; the gate adds the posting-record shape, the `PostingPending` deadline, successor enums, signed-identity verification, the person-only Approver rule, the edit-time scan, `ContextReadUnavailable`, and the FR-8 gate checks | Carry as implementation follow-ups for the Agents Runtime Maintainer under existing stories, not as document amendments |

### Problem Classification

This is a requirements-to-planning reconciliation discovered during sprint execution, the third of its kind for 2026-09-09. It changes no Product scope. It restores one authority chain from the PRD through the Spine, the registers, and the executable backlog, and it schedules the PRD's own cross-document resync, which no earlier proposal could do because each preserved `prd.md` unchanged.

## 2. Impact Analysis

### Epic Impact

| Epic | Impact | Viability |
| --- | --- | --- |
| Epic 5 - Live Governed Setup And Honest Readiness | Gains the provisioning story, the `Draft` lifecycle value, the data-handling acceptance and tightening rules (5.3, 5.7), `TriggerReviewOverdue` as an activation block (5.7), successor enums for the zero-valued statuses (5.2, 5.3, 5.5), the A-24 attestation cadence and signed-identity pin (5.8), and `EXT-PARTIES-1` as a natural dependency | Viable; 10 stories become 11 |
| Epic 6 - One Safe Automatic Conversation Response | Gains the FR-8 ten-step order, the non-person caller denial and `TenantSuspended` (6.1), `ContextReadUnavailable`/`RescanPending` and the data-handling call-time check (6.2, 6.3), `SafetyFailed` (6.3), the automatic posting record (6.6), the roster-version, A-22, and `MirrorRefused` rules (6.6), and the extended contract-member list (6.8) | Viable; 8 stories |
| Epic 7 - Complete Confirmation And Approval | Gains `RestrictedContent` at edit time (7.2), the `PostingPending` deadline evaluation (7.4), human abandon from `Approved` and `PostingWindowElapsed` (7.5), and the administrative-retry age bound (7.7) | Viable; 7 stories |
| Epic 8 - Governance Operations And Release Qualification | Gains `SuspensionReviewOverdue` and the trigger-review block (8.4), the p99 sample rule and the retraction metric input (8.5), the inspection aggregation bound (8.8), and `DeferredAssumption` rendering and acceptance (8.7) | Viable; 8 stories |

One story is added (provisioning, Epic 5). No epic or story is removed, renumbered, or made obsolete. Epic order stands. `RQ-1` stays outside the backlog. The active backlog becomes 34 stories once the epics edit lands.

### Story Impact

Substantive acceptance-criteria changes are proposed for Stories 5.2, 5.3, 5.7, 6.1, 6.2, 6.6, 6.8, 7.4, 7.5, 7.7, 8.4, 8.5, and 8.7, plus one new story. Story identities and user-visible outcomes are stable; the changes add settled state, vocabulary, deadline, and evidence obligations from the current FR text.

### Artifact Conflicts

| Artifact | Conflict | Disposition |
| --- | --- | --- |
| `prd.md` | Cross-document pointers and the §8.1 divergence list lag the Spine round-5 update; §8 still calls the Story 5.3 approval open; §0 cites the register at `updated: 2026-09-09` | Resync amendment only, scheduled through this proposal; no FR text changes except where Product decides the ARCH-A-13 question (§7) |
| `ARCHITECTURE-SPINE.md` | Residual AD-7/AD-2, AD-17, AD-20, AD-21, AD-22, AD-10/AD-2, AD-8, AD-13, AD-28, Conventions, and Assumptions-table gaps | Direct amendment; index version increments to 6 |
| `external-dependency-register.md` | Six of seven requested edits missing | Direct amendment; ten records, all `Uncommitted` |
| `launch-readiness-register.md` | Every third-update extension missing | Direct amendment |
| `epics.md` | Six inventory rows, story vocabulary, posting and governance criteria, provisioning ownership | Direct amendment with one added story |
| `sprint-change-proposal-2026-09-09-2.md` | Stale 29-story count; `prd.md` not schedulable through it | Factual correction and cross-reference only |
| `sprint-status.yaml` | Needs the new story key after the epics edit lands | Additive entry |
| `DESIGN.md`, `EXPERIENCE.md` | Neither carries any of the eight new typed names; a UX Update run started 2026-09-10 is editing `EXPERIENCE.md` at the time of writing | Not amended here; recorded as concurrent activity (§7) |

### Technical Impact

- Release qualification gains four register record kinds and two conditions, and the Release PM becomes a recorded planning party with no runtime permission.
- Membership becomes a roster-versioned state machine with a terminal `MirrorRefused` outcome and an A-22-gated re-admission; the acceptance path yields typed call rejections.
- Automatic posts run through a system-approved posting record under the FR-18 rows, with a stored `PostingPending` deadline whose duration is still an open Architecture assumption (`ARCH-A-14`).
- Provider data handling becomes a versioned, tenant-accepted record with a declared-tightening grace.
- The FR-34 attestation verifies a Security-qualified signed identity provisioned through `EXT-SECRETS-1`, never an engine-reported string.
- Public contracts evolve additively except for the four zero-valued enums, which FR-23 corrects through successor enums under deprecate-and-reject.

## 3. Recommended Approach

Use **Direct Adjustment**. Amend the seven listed artifacts, add one provisioning story, carry the code-side list as follow-ups under existing stories, and leave every owner commitment, date, member name, and command to its owner.

### Options Considered

| Option | Viability | Assessment |
| --- | --- | --- |
| Direct adjustment | Selected | Product decisions are settled in the PRD; the Spine has already absorbed most of them; the remaining edits are bounded and mechanical |
| Roll back the Spine round-5 update or the PRD third update | Not viable | Both landed reviewer gates; rolling back would reopen closed divergences without resolving the residuals |
| Fold the provisioning leg into Story 5.2 | Not selected | 5.2 is in-progress with a fixed DW-4 scope, no external dependency, and a tenant-principal shape; provisioning is a Platform-principal create-only operation with a Parties write dependency and its own idempotency and repair semantics |
| Add a remediation epic | Not selected | Every obligation belongs to an existing Epic 5-8 outcome |
| Reduce or redefine MVP | Not viable | The change set is governing launch behaviour, not optional expansion |

### Effort, Risk, And Timeline

- Planning effort: medium. Seven artifacts change; most edits are quoted below.
- Implementation effort: high, unchanged from the prior proposal; the follow-up list in §5 is the same debt with eight additions.
- Risk before correction: high. A register that fails closed on `DeferredAssumption` cannot record the deferral the PRD permits; an acceptance path that never yields `MembershipUnavailable` cannot implement FR-8 step 10; a posting record with no story cannot be built.
- Risk after correction: medium until the external owners commit targets and the eleven Architecture dates are recorded.
- Calendar impact: not estimable. All ten dependency records will be `Uncommitted` after the register amendment. No date is inferred.

## 4. Detailed Change Proposals

Every OLD quotation was read from disk on 2026-09-10. Line numbers are as read and may shift as edits land.

### 4.1 `prd.md` (resync only)

These edits restate what other artifacts now record. They add no requirement and change no FR rule. They land only through a PRD Update run after approval, per the PRD's own §0 rule.

#### §0 status line (line 16)

OLD:

> (`_bmad-output/planning-artifacts/external-dependency-register.md`, `updated: 2026-09-09`)

NEW:

> (`_bmad-output/planning-artifacts/external-dependency-register.md`, `updated: 2026-09-10`)

#### §8 Story 5.3 sentence (line 868)

OLD:

> where the register carries an open Product approval instead of a dated record — as it does for Story 5.3 and `EXT-PROVIDER-1` — that open approval is itself a `ready-for-dev` blocker for the consuming stories until Product selects an evidence-backed branch.

NEW:

> where the register carries an open Product approval instead of a dated record, that open approval is itself a `ready-for-dev` blocker for the consuming stories until Product selects an evidence-backed branch; for Story 5.3 and `EXT-PROVIDER-1` Product ruled on 2026-09-10 that no seam was executed (register, "Resolved Branch B"), which leaves `EXT-PROVIDER-1` `Uncommitted` and its consumers blocked on that status alone.

#### §8.1 Spine pointer and divergence list (lines 893, 897-908)

OLD (line 893):

> (frontmatter `updated: 2026-09-09`, versioned by its `architecture_assumption_index_version`)

NEW:

> (frontmatter `updated: 2026-09-10`, versioned by its `architecture_assumption_index_version`)

OLD (line 897, closing sentence):

> The known Spine divergences as of 2026-09-09 (third update and its reviewer gate) are listed here by AD number, one clause each; `reconcile-validation-2026-09-09-3.md` in this PRD's folder is the authority for this list and its correct-course routing, and the FR text governs each item until the Spine's index version increments:

NEW:

> The Spine's round-5 update of 2026-09-10 (index version 5) corrected the AD-5, AD-12, AD-13, AD-14, and AD-21 divergences recorded on 2026-09-09 and added the AD-17 vocabulary; the divergences still known as of 2026-09-10 are listed here by AD number, one clause each; `sprint-change-proposal-2026-09-09-3.md` is the authority for this list and its routing, and the FR text governs each item until the Spine's index version increments past 5:

Replace the eight bullets (lines 899-906) with:

- AD-7 and AD-2: name `MirrorRefused` without defining it as the terminal outcome FR-2 states; say the acceptance path never yields `MembershipUnavailable` or `MembershipRejected`, which FR-8 step 10 does; treat an external removal as a mirrored block, whereas FR-2 records `ExternallyRemoved` with no mirror; and stop re-admission after a confirmed clear, whereas FR-2 treats a later absence as a removal.
- AD-17: limits `UnretiredAssumption` to Product, Architecture, and Governance owners, whereas FR-28 item 9 counts every unretired row whatever its owner.
- AD-20: selects one policy pair under the `LastLoosensVersion` ordering, whereas FR-26 requires the content to pass every applicable version.
- AD-21: defines no single period-close settlement instant for `Unreconciled` reservations (FR-28).
- AD-22: defines no launch-health compliance finding for an unreviewed inspection (FR-24).
- AD-10 and AD-2: carry no `DataHandlingVersion`, acceptance, or declared-tightening rule (FR-4, FR-5).
- Consistency Conventions: correct `AgentSetupWriteStatus` additively, whereas FR-23 requires successor enums for all four zero-valued members.

OLD (line 908, first sentence): `Every FR amended by the third 2026-09-09 update and its reviewer gate is a PRD-originated amendment pending in the Spine until its index version increments.`

NEW: `Every FR amended by the third 2026-09-09 update and its reviewer gate that the list above still names is a PRD-originated amendment pending in the Spine until its index version increments past 5.`

#### OQ-27 (line 1077)

OLD:

> Spine AD-12, `updated: 2026-09-09`, states the same rule

NEW:

> Spine AD-12, `updated: 2026-09-10`, states the same rule

#### Conditional FR-7 edit (Product decision, §7 item 1)

If Product confirms `ARCH-A-13`, FR-7's predicate gains a third exclusion after "not the Party that last edited the version under decision": "and not the Approver who requested the regeneration that produced the version under decision". If Product declines, no PRD change; the Spine removes the exclusion from AD-8 and retires `ARCH-A-13`. This proposal does not decide it.

### 4.2 `ARCHITECTURE-SPINE.md`

Round 5 landed most of the change set. The proposals below are the verified residuals. The index version increments to 6 when they land.

#### AD-5 (line 163)

OLD:

> System abandonment carries a typed reason (`NoEligibleApprover`, `SourceConversationUnavailable`, `RemovedInConversations`) and, per AD-7's membership re-check,

NEW:

> System abandonment carries a typed reason (`NoEligibleApprover`, `SourceConversationUnavailable`, `RemovedInConversations`, `PostingWindowElapsed`) and, per AD-7's membership re-check,

Add after the bounded-retry sentence: “A `PostingFailed` proposal whose failure reason is a safety verdict is not retryable, automatically or administratively, and accepts abandon only (FR-18).”

#### AD-7 and AD-2 membership (lines 143, 175, 177)

OLD (line 177):

> `Joined` plus absent, when the participant read is at least as fresh as the last confirmed add for that Conversation and the last removal mirror is confirmed with no newer clear, records `ExternallyRemoved`; … absence during an unconfirmed mirror or after a newer clear is the mirror's late effect and causes no state change.

NEW:

> `Joined` plus absent, when the participant read is at least as fresh as the last confirmed add for that Conversation — compared by the roster version the seam-1 read returns [ASSUMPTION A-15] — and the last mirror, block or re-admission, is confirmed (a Conversation with no mirror history counts as confirmed), records `ExternallyRemoved` with no mirror, because there is nothing to remove; a read older than the last confirmed add is not evidence of removal and instead rejects with `MembershipUnavailable`, changing no state; absence while the newest clear's re-admission mirror is unconfirmed is that mirror's late effect: the step re-adds `hexa` idempotently under the standing clear, stays `Joined`, and accepts, and once that mirror is confirmed any later absence is an external removal on the preceding terms.

OLD (line 177):

> under the same `MirrorPending`/`MirrorRefused` terms as a block mirror

NEW:

> under the same mirror terms as a block mirror: only a transient failure is retried, each attempt under a timeout; a typed permanent refusal from Conversations ends the retry and records `MirrorRefused`, a terminal mirror outcome and a flag exposed on the FR-25 surface for Tenant Agent Administrator action — re-set the block (a fresh block mirror) or clear it (a re-admission mirror) — while the block itself stays in force

OLD (line 175):

> `MembershipUnavailable` and `MembershipRejected` remain `PostingFailed` reasons, produced only by this re-validation for a pre-post failure that is not a removal or block, never by acceptance.

NEW:

> At acceptance (FR-8 step 10) `MembershipUnavailable` (unavailable participant read) and `MembershipRejected` (Conversations refused the add for a reason that is not a removal) are call rejections that change no state; at the pre-post re-validation the same two are `PostingFailed` reasons, produced for a failure that is not a removal or block.

OLD (line 175):

> record the external removal on that aggregate at its expected revision, which sets the block, abandon that Conversation's non-terminal proposals, and reject with `RemovedInConversations`

NEW:

> record `ExternallyRemoved` on that aggregate at its expected revision — a rejecting state on the same terms as `Blocked`, with no mirror — abandon that Conversation's non-terminal proposals, and reject with `RemovedInConversations`

OLD (line 143):

> `BlockVersion` — incremented on every set or clear … a `MirrorPending` flag set whenever a block, clear, or removal record is written

NEW:

> `BlockVersion` — incremented on every block set; a clear must name the current version and cancels every outbox entry of a lower version — a `MirrorPending` or terminal `MirrorRefused` flag carried while a block or re-admission mirror entry awaits confirmation (an `ExternallyRemoved` record has no mirror)

Add `Draft` to the `Agent` aggregate's lifecycle values in AD-2: exactly `Draft`, `Active`, `Disabled`, with `Unknown` as the fail-closed sentinel (FR-3).

#### AD-12 (line 209)

OLD:

> absence is `TriggerReviewOverdue` and blocks qualification.

NEW:

> absence is `TriggerReviewOverdue`, which, while it stands, refuses enablement of the tenant's production-like generation (FR-33) and refuses `hexa` activation in that tenant (FR-3), each a typed rejection; it suspends nothing and deletes nothing.

#### AD-13 step 1 and AD-28 fast gate (lines 215, 311)

AD-13 step 1 NEW: “caller authorization, caller Party state — a non-person caller Party, `hexa`'s own included, is a typed denial — and Source Conversation access”. Add `TenantSuspended` as the step-2 rejection reason name (FR-8, FR-25).

OLD (line 311):

> `InteractionRequested` to the first blocking event whose reason needed no content-safety classifier call, with safety-scan latency its own series

NEW:

> `InteractionRequested` to the first blocking event produced by FR-8 steps 1 through 8, with the step-9 safety scan its own latency series and a step-10 membership rejection reported with that series, never in the fast gate (FR-8, FR-27)

#### AD-8 human-only sources (line 183)

Add after "Resolution is Conversation-scoped: a source contributes only current Participants holding read access.": “Every source resolves to human Parties only: a predefined `PartyId` naming an organization or AI Party is a typed configuration rejection, and a Facilitator or tenant-role holder resolving to a non-human Party contributes no Approver (FR-7).”

#### AD-14 (line 223, optional clause)

After "failing closed with `PayloadProtectionUnavailable` on a missing value, an unsigned engine, or a mismatch", add: `, and the attestation record names which principal supplied each of the two compared values`.

#### AD-17 (line 243)

OLD:

> records an `UnretiredAssumption` blocker for every unretired Product, Architecture, or Governance assumption in PRD section 8.1 or in this spine's Architecture Assumptions index

NEW:

> records an `UnretiredAssumption` blocker for every unretired row in PRD section 8.1 and in this spine's Architecture Assumptions index, whatever its owner, unless the row carries a Product-accepted `DeferredAssumption` deferral (FR-28 item 9)

#### AD-20 (line 263)

OLD: `regeneration (a new descriptor), approval time, and pre-post evaluate under the current pair when it is at least as restrictive as the snapshot pair and under the snapshot pair exactly otherwise.`

NEW:

> regeneration (a new descriptor), approval time, and pre-post evaluate the content under every applicable version — the attempt's snapshot pair and the then-current pair, each (platform, tenant) component in addition to, never instead of, the other — and pass only when every evaluation passes; the `LastLoosensVersion` ordering serves only to skip the snapshot-pair evaluation when the current pair is provably at least as restrictive, never to skip the current pair (FR-26).

Add the edit-time stage after "generated output passes a fresh decision before proposal creation;": “a human edit is scanned under the effective policy before the edited version is recorded — an always-blocked failure rejects the edit with a typed reason and stores nothing, a restricted-category failure stores the version with a `RestrictedContent` marker, a field of the version record and never a call status, that makes it non-approvable and visible under FR-25 (FR-15);”. AD-29's `SafetyDecisionId` stage list admits an edit stage accordingly.

Add the re-scan wait: a bounded background re-scan after policy publication or HMAC rotation during which a call fails closed as `ContextReadUnavailable` with sub-reason `RescanPending`, closed by Platform Operator acknowledgement (FR-27); AD-11 names `ContextReadUnavailable` as the unavailable context-read class, additive to the coarse `ContextUnavailable` blocked reason (FR-9).

#### AD-21 (line 269)

OLD:

> an `Indeterminate` outcome holds the reservation for a configured period (default 24 hours, range 1 to 72) [ASSUMPTION A-8] and then settles `ChargedAtMaximum`, and is never retried. A reservation past its deadline with no authoritative outcome becomes `Unreconciled`, stays counted, and is settled only by an audited `TenantBudgetUpdate` operator command or at period close as `ChargedAtMaximum`;

NEW:

> an `Indeterminate` outcome holds the reservation for a configured period (default 24 hours, range 1 to 72) [ASSUMPTION A-8], during which only an audited `TenantBudgetUpdate` operator command settles it, and is never retried. A reservation whose hold deadline passes with no authoritative outcome becomes `Unreconciled`, stays counted, and is settled only by an audited `TenantBudgetUpdate` operator command or, at period close — the single instant at the end of the reserving UTC calendar month plus the maximum configured hold — as `ChargedAtMaximum` against the reserving period [ASSUMPTION ARCH-A-6];

#### AD-22 (line 275)

OLD: `but the unreviewed flag persists as a visible governance signal to the Tenant Agent Administrator and Platform Operator until a review is recorded; no further automatic escalation is defined for V1.`

NEW: `but the unreviewed flag persists as a visible governance signal to the Tenant Agent Administrator and Platform Operator until a review is recorded, and every unreviewed inspection is a compliance finding reported at the next launch-health review (FR-24).`

#### AD-10 and AD-2 data handling (lines 143, 195)

OLD (line 143):

> `TenantProviderEnablement` (`TenantId`; which platform entries the tenant may see and select, mutated by the Platform Operator [ASSUMPTION A-10])

NEW:

> `TenantProviderEnablement` (`TenantId`; which platform entries the tenant may see and select, mutated by the Platform Operator, and per entry the `DataHandlingVersion` the Tenant Agent Administrator last accepted — the version in force — recorded by the Tenant Agent Administrator's acceptance command [ASSUMPTION A-10])

Add to AD-10 after the capability floor: “Every catalog entry also carries a data-handling record (retention term, training-use status, `ProcessingRegion`, contractual reference) under its own `DataHandlingVersion`, incremented only when one of those four fields changes; an entry whose record is absent or incomplete cannot be enabled for any tenant. In the tenant join, a catalog `DataHandlingVersion` newer than the tenant's accepted version is `Blocked` with the tenant-scoped reason `DataHandlingAcceptanceLapsed` — its own FR-25/SM-C4 class, returned at FR-8 step 4 and at activation, and excluded from the FR-28 trigger shares — unless the Platform Operator declared the version a tightening change with a recorded field-level diff when recording it, in which case the entry stays callable under the version in force for 30 days from the declaration and then blocks with the same reason; an undeclared change is never tightening, a loosening field change may not be declared tightening, and a Tenant Agent Administrator decline blocks immediately. Each Provider attempt's evidence records both the current and the in-force `DataHandlingVersion` (FR-24).”

The placement of the acceptance fact on `TenantProviderEnablement` is one option; the PRD leaves aggregate placement to Architecture.

#### Consistency Conventions, contract versioning (line 494) and AD-15 (line 229)

OLD:

> `AgentSetupWriteStatus` gains `Unknown = 0` additively before the first tenant is enabled

NEW:

> the four zero-valued success members `AgentSetupWriteStatus.Submitted`, `AgentInspectionStatus.Success`, `AgentInteractionGateInspectionStatus.Success`, and `ProviderCatalogInspectionStatus.Success` are corrected by successor enums with `Unknown = 0` before the first tenant is enabled, each current enum remaining declared under deprecate-and-reject, since a zero value cannot be re-numbered additively; the nine state-duplicating `AgentInteractionStatus` members `ProposalEdited`, `ProposalRegenerated`, `ProposalApproved`, `ProposalPostingPending`, `ProposalPosted`, `ProposalPostingFailed`, `ProposalRejected`, `ProposalAbandoned`, and `ProposalExpired` are on the register as no longer emitted (a received one reads as `ProposalCreated` plus the current `ProposedAgentReplyState`), while the `*Failed` action outcomes stay recorded

Align AD-15's `AgentInteractionStatus` member list with FR-8: `SafetyFailed` is the PRD member; `SafetyBlocked` and `BudgetBlocked` are not PRD-named. Add `MirrorRefused`, `RestrictedContent`, `TenantSuspended`, `ContextReadUnavailable`, `RescanPending`, and `DataHandlingAcceptanceLapsed` to the AD-15 FR-25 parity list.

#### Architecture Assumptions preamble and table (lines 786-803)

Append to the preamble: “A row added after the recorded scheduling of the `RQ-1` evaluation blocks `RQ-1` unless the Release PM records a `DeferredAssumption` deferral (FR-28 item 9: row, owner, FR consequences not exercised, revisit date at most 30 days out; never for a row whose retirement condition names Security), itself a blocker until Product accepts it; every Architecture-owned row carries a literal calendar `TargetRetirementDate`, and a row still unretired when that date passes escalates to Product for a recorded keep-or-retire decision reported on the FR-30 surface and at the next launch-health review, blocking `RQ-1` until retired or deferred.”

Rows whose `TargetRetirementDate` is `TBD` or a milestone and must receive a literal date from the named co-owners: ARCH-A-1, ARCH-A-2, ARCH-A-7 (Architecture + Platform Maintainer); ARCH-A-3 (Architecture + Release PM); ARCH-A-4 test-stack sub-item and ARCH-A-14 (Architecture); ARCH-A-6, ARCH-A-11, ARCH-A-13 (Architecture + Product); ARCH-A-8 (Architecture + FrontComposer Maintainer); ARCH-A-12 (Architecture + Security). No date is invented here.

`ARCH-A-11`: no Product confirmation is recorded anywhere on disk. If the Administrator, acting as Product authority, confirms the `Disabled`-Agent retry-clock pause as FR-3 and FR-18 state it at approval of this proposal (§7 item 2), the Release Operator records that confirmation in the launch readiness register and Architecture retires the row with the actual date; otherwise the row keeps its date column and Product supplies a literal date.

#### Frontmatter

Add to `sources:`: `../../prds/prd-agents-2026-06-23/update-report-2026-09-09-3.md`, `../../prds/prd-agents-2026-06-23/reconcile-validation-2026-09-09-3.md`, and `../../sprint-change-proposal-2026-09-09-3.md`.

### 4.3 `external-dependency-register.md`

#### New entry `EXT-PARTIES-1` (insert after `EXT-PROTECTION-1`)

| Commitment field | Value |
| --- | --- |
| `Owner` | Parties Maintainer |
| `Repository` | `Hexalith.Parties` |
| `RequiredArtifact` | An AI Party type in Hexalith.Parties, and authorization for the Agents Service Principal to create `hexa`'s Party under that type as part of PRD FR-1 create-only provisioning (id derived from the Agent id, owned by the Agents Service Principal, immutable for the life of the tenant). Transitional rule until this record is `Committed`: the Organization-typed Party that create-only provisioning creates is `hexa`'s immutable provisioned Agent Party identity, verified by id — never by Party type — at `EXT-CONV-AI-1` seam 1 and wherever PRD FR-2 checks it; no Party may be linked or replaced (PRD FR-1, FR-2, FR-23, §8, §8.1 A-21, A-27). The record owner supplies the final type and member names. |
| `TargetVersionOrCommit` | `TBD` |
| `TargetIntegrationDate` | `TBD` |
| `CompatibilityContractAndVerificationCommand` | Contract: the Agents Service Principal can create exactly one AI-typed Party per tenant for `hexa` idempotently; a second create returns the existing Party; creation by any tenant role is a typed denial; the Party is resolvable by id with its type; no Party PII crosses the Agents boundary. Command: `TBD`. |
| `RequiredEvidenceLevel` | `TBD` (owner to confirm) |
| `AcceptedStatus` | `Uncommitted` |
| `ConsumingStories` | The provisioning story added by this proposal (AI-type leg only); its interim Organization-typed leg, and Stories 5.2, 5.4, and 6.6, consume the existing Parties create and read path and are not consumers of this record. `RQ-1` only if Product does not record the interim identity as the V1 launch identity (A-27). |

Provenance note: *Added 2026-09-10 from the PRD's 2026-09-09 request (PRD §8, A-27). Commitment fields are `TBD`; the interim by-id rule is PRD text, not a Parties commitment.*

Current Blocking Summary (line 209): OLD “All nine dependency records are `Uncommitted`” → NEW “All ten dependency records are `Uncommitted`”.

#### `EXT-CONV-AI-1` seam 7 (lines 57, 60)

OLD: `Six seams. (1) Membership:` → NEW: `Seven seams. (1) Membership:`

OLD: `(6) The Conversation deletion signal remains unchanged so an approved deletion in Conversations triggers PRD FR-30 deletion of derived Agent content. Final member and typed failure names are owned by this record (PRD §8.1 assumptions A-1 through A-4, A-15, A-16, A-21).`

NEW:

> (6) The Conversation deletion signal remains unchanged so an approved deletion in Conversations triggers PRD FR-30 deletion of derived Agent content. (7) Message retraction: a signal that a Conversation Message posted as the `AiAgent` participant was retracted, deleted, or flagged by a human, carrying the acting Party and the instant, for the PRD OQ-23 retraction metric (A-28); if Product decides OQ-23 without it by 2026-09-30, seam 7 is removed from this artifact by a dated amendment. Final member and typed failure names are owned by this record (PRD §8.1 assumptions A-1 through A-4, A-15, A-16, A-21, A-22, A-28).

Contract (line 60): OLD `and a deletion signal, each with focused cross-tenant denial.` → NEW “a deletion signal, and a retraction/deletion/flag signal for `AiAgent`-posted messages carrying acting Party and instant, each with focused cross-tenant denial.”

No story consumes seam 7 until OQ-23 is decided; `ConsumingStories` is unchanged.

#### `EXT-CONV-AI-1` seam 1 and seam 5 (lines 57, 60)

OLD (seam 1): `plus a participant-state read for the AI participant and a participant removal limited to the AI participant for the PRD FR-2 removal block. Before accepting AI membership, Conversations verifies through Hexalith.Parties that the participant Party is AI type; the record owner supplies the final member and typed failure names (A-21).`

NEW:

> plus a participant-state read for the AI participant that returns a roster version (so Agents can tell a read at least as fresh as its last confirmed add from an older one, PRD FR-2, A-1, A-15), and a participant removal limited to the AI participant for the PRD FR-2 removal block, authorized for the Agents Service Principal, whose typed answers include a typed "already absent" answer that Agents treats as confirmation of the pending mirror entry (PRD OQ-25). `ParticipantType.AiAgent` is the compiled Conversations spelling and is not assumed. Before accepting AI membership, Conversations verifies through Hexalith.Parties that the participant Party is the provisioned Agent Party identity — by id, and by AI Party type only once `EXT-PARTIES-1` is `Committed` (A-21, A-27) — and restricts adding an `AiAgent` participant to `ParticipantRole.Facilitator` or the Conversation's administrative role (A-22); the record owner supplies the final member and typed failure names.

The current "is AI type" wording contradicts the PRD's by-id rule and must not survive.

OLD (seam 5): `including current edit/delete state (A-15).` → NEW: `including current edit/delete state, and each roster read carries the roster version PRD FR-2 compares against (A-15).`

Contract (line 60): OLD `typed idempotent membership with participant-state read, AI-participant removal, and Hexalith.Parties AI-type verification;` → NEW “typed idempotent membership with a roster-versioned participant-state read, Service-Principal-authorized AI-participant removal with a typed already-absent answer, Facilitator/administrative-role-only `AiAgent` add, and Hexalith.Parties verification of the provisioned Party by id;”

#### `EXT-SECRETS-1` scope (lines 147, 150) and the two "committed target" phrases (lines 89, 178)

OLD (line 147): `Secret resolution, rotation, denial, and leak-evidence contract for Provider and export/deletion operations. Only secret references and configured/not-configured state cross Agents boundaries.`

NEW:

> Secret resolution, rotation, denial, and leak-evidence contract for Provider and export/deletion operations, plus custody of three Agents-consumed values: (a) the Security-qualified production protection engine's signed build identity and version, provisioned by the Release Operator from the launch readiness qualification record, which the PRD FR-34 attestation compares the loaded engine's signed identity against on every check (never a value the engine reports about itself) and pins; an absent value leaves `PayloadProtectionUnavailable` standing (A-24); (b) the per-tenant key-encryption key that wraps `EXT-PROTECTION-1` DEKs; (c) the per-tenant HMAC secret for the PRD FR-27 content-hash cache key, whose rotation starts the re-scan. Only secret references and configured/not-configured state cross Agents boundaries.

OLD (line 150): `and proof that values do not enter events, projections, responses, logs, traces, or evidence.` → NEW: `delivery of the qualified engine identity/version value to the runtime with the supplying principal recorded, KEK and HMAC-secret rotation, and proof that values do not enter events, projections, responses, logs, traces, or evidence.`

OLD (line 89, `EXT-HOST-1`): `for comparison with the committed protection target` → NEW:

> for comparison with the Security-qualified value custodied through `EXT-SECRETS-1`

. OLD (line 178, `EXT-PROTECTION-1`): `(5) engine identity/version reporting equal to the exact committed target` → NEW:

> (5) a signed engine identity verifiable against the Security-qualified value custodied through `EXT-SECRETS-1`, with a self-reporting wrapper rejected by signature

. Both are artifact changes for their owners to re-accept; both records are already `Uncommitted`.:

#### `EXT-CONV-UI-1` fourth artifact kind (lines 73, 76)

OLD: `Three artifact kinds.` → NEW: `Four artifact kinds.` Append after (3): `(4) A Conversation-level status entry through which Agents shows the pending-proposal state to authorized Approvers only (PRD FR-13), disclosing nothing to other Participants.`

OLD (line 76): `a callability gateway, and no cross-tenant exposure.` → NEW: `a callability gateway, an Approver-scoped pending-proposal status entry, and no cross-tenant exposure.`

`ConsumingStories` stays `6.7; RQ-1` unless the Product Owner binds the proposal-queue story to kind 4.

#### OQ-17 non-clearing clause (after line 43)

Add: “A story completed while a dependency it consumes was `Uncommitted` is recorded under Known Consumer Non-Conformance with the dependency, the story, and the date; narrowing the story's scope afterwards to avoid the seam does not clear that record (PRD §8, OQ-17).” The clause was lost when the Branch A template was removed.

#### Provider Readiness Contract data-handling codes

The Provider Readiness Contract lives in the launch readiness register, not here; see §4.4. This register needs no `EXT-PROVIDER-1` change for data handling because the record is Agents-owned catalog truth, not an adapter seam.

### 4.4 `launch-readiness-register.md`

#### Non-gate blocker sentence (line 19)

OLD:

> or `PayloadProtectionUnavailable` blocker stands.

NEW:

> or `PayloadProtectionUnavailable` blocker stands, or a `DeferredAssumption` deferral stands without Product's recorded acceptance.

#### Vocabulary table (append after line 54)

| Code | Meaning |
| --- | --- |
| `DeferredAssumption` | PRD-declared 2026-09-09 (FR-28 item 9, FR-33, §8.1). A Release PM deferral of an `ARCH-A-n` row added after the recorded `RQ-1` evaluation-scheduling event; the record names the row, its table and Spine index version, its owner, the FR consequences the run will not exercise, and a revisit date no more than 30 calendar days after the deferral. Visible on the FR-30 surface. An `RQ-1` blocker until Product's recorded acceptance; never available for a row whose retirement condition names Security. A passed revisit date without a recorded retirement or a new Product-accepted deferral returns the row to `UnretiredAssumption` and, after enablement, is a launch-health finding. |
| `SuspensionReviewOverdue` | PRD-declared 2026-09-09 (FR-28). An SM-4 kill-switch pull with no recorded containment review after two business days (24 hours excluding Saturday and Sunday in UTC), or a review pull older than 7 days without a recorded review decision. Reported on the FR-30 surface; never an `RQ-1` input. |

Line 56: extend to “`SuspensionReviewOverdue` and `DeferredAssumption`, PRD-declared 2026-09-09, are carried here from this revision and mirrored by FR-30 on the same terms.”

#### Emitter table (append after line 80)

| `BlockerCode` | Emitted by | Contract |
| --- | --- | --- |
| `DeferredAssumption` | `RQ-1` evaluation, over the Release PM's recorded deferrals | One code per deferred `ARCH-A-n` row lacking Product's recorded acceptance or whose revisit date has passed; names the row, table, index version, owner, FR consequences not exercised, and revisit date |
| `SuspensionReviewOverdue` | Post-enablement kill-switch evaluation (same emitter family as `TriggerReviewOverdue`) | Emitted when an SM-4 pull has no recorded containment review after two business days, or a review pull is older than 7 days without a recorded review decision; reported on the FR-30 surface; never an `RQ-1` input |

#### Launch Governance Records (new subsection after line 89)

| Record kind | Recorded by | Contents | Effect |
| --- | --- | --- | --- |
| `RQ-1` evaluation scheduling | Release Operator (FR-33 readiness row) | Evaluated `EnvironmentProfile`, Spine index version, recorded instant | The instant after which an added `ARCH-A-n` row is "late" (FR-28 item 9) |
| Dependency exclusion from the qualification profile | Release PM (FR-28 item 6, FR-33) | `EXT-*` entry excluded; FR consequences the run will not exercise | `RQ-1` blocker until Product's acceptance is recorded |
| Late-row deferral | Release PM (FR-28 item 9, FR-33) | Row, table, index version, owner, FR consequences not exercised, revisit date at most 30 calendar days out | Emits `DeferredAssumption` until Product's acceptance is recorded; bounded by the revisit date |
| Product acceptance of an exclusion or deferral | Product; recorded by the Release Operator (FR-33 preamble) | The exclusion or deferral it accepts | Clears that blocker; a deferral stays bounded by its revisit date |
| Product confirmation of a Product-owned assumption | Product; recorded by the Release Operator | The `A-n` or `ARCH-A-n` row and the confirmed value or rule | Retirement basis for the row (e.g. `ARCH-A-11`, A-26 confirmations) |
| SM-4 containment review | Platform Operator's recorded containment finding (FR-33 release row) | Pull reference, finding, review instant | Absent after two business days: `SuspensionReviewOverdue`; required to release an SM-4 pull |
| Kill-switch trigger review | Release Operator, within one business day in UTC (FR-33) | Rates, sample, decision, validity (7 days or +10 points) | Absent: `TriggerReviewOverdue`; a review pull is only this record's decision |
| Security engine-build qualification | Security qualifies; the Release Operator records the signed identity and version and provisions it through `EXT-SECRETS-1` (FR-33, FR-34) | Signed build identity, version, Security's recorded qualification | The value the FR-34 identity attestation verifies against; recording never clears `PayloadProtectionUnavailable` |

The Release PM is not an AD-30 principal kind; its records enter the register as documents through the Release Operator, which grants no runtime permission (PRD §3). The Platform Operator's tightening declaration is Audit Evidence on the Global Providers Aggregate (FR-4, FR-33), not a register record.

#### `PayloadProtectionUnavailable` and `TriggerReviewOverdue` (lines 52, 53, 78, 80)

OLD (line 53):

> The host has not reported the production payload-protection engine required by FR-34 as available; this is also an additive `AgentLaunchReadinessBlocker`.

NEW:

> The FR-34 attestation has not passed: the runtime cannot verify the loaded engine's signed build identity against the Security-qualified identity and version recorded in this register and custodied through `EXT-SECRETS-1` (never a value the engine reports about itself), the `EXT-SECRETS-1` value is absent, or the canary liveness check fails. Also an additive `AgentLaunchReadinessBlocker`; never cleared by a host-asserted signal, by the canary alone, or by recording the identity.

OLD (line 78): `Emit when the host cannot produce the FR-34 seal/unseal/erase identity-and-version attestation against the committed engine.`

NEW:

> Emit when the FR-34 two-part attestation does not pass — identity (loaded engine's signed identity verified and pinned against the Security-qualified value custodied through `EXT-SECRETS-1`) and liveness (seal, no-plaintext persisted bytes, unseal, DEK destroy, `Erased` replay) — at startup, on each readiness evaluation, on the bounded cadence (hourly by default, A-24), or on a host composition change; record which principal supplied each compared value.

OLD (line 52): `A required FR-28 kill-switch trigger review was not recorded within one business day.`

NEW:

> A required FR-28 kill-switch trigger review was not convened and recorded within one business day (24 hours excluding Saturday and Sunday in UTC). While it stands, the tenant's production-like generation cannot be enabled (FR-33) and `hexa` cannot be activated in it (FR-3), each a typed rejection; it suspends nothing and deletes nothing.

Line 80: before "and never treat it as an `RQ-1` metric input", insert “; while it stands, production-like enablement and `hexa` activation for that tenant are refused with a typed rejection (FR-33, FR-3)”.

#### `LR-PARTY-IDENTITY` (line 100)

OLD: `Invalidated by Party state, identity contract, or Agent identity-link changes.` → NEW: `Invalidated by Party state, identity contract, or Agent Party identity or provisioning changes (FR-1, FR-2; the link and replace commands are on the FR-23 deprecate-and-reject register).`

#### Provider Readiness Contract (lines 209, 211)

Hard gates (line 211): append “, and a complete data-handling record whose current `DataHandlingVersion` is accepted for the tenant or within a declared tightening grace (FR-4, FR-5)”.

Reason codes (line 209): append “and (added 2026-09-10) `DataHandlingAcceptanceLapsed` — the tenant has not accepted the current `DataHandlingVersion` and no declared tightening grace is in force (PRD FR-5; its own FR-25/SM-C4 class, excluded from the FR-28 trigger shares) — and `DataHandlingUnrecorded` — the data-handling record is absent or incomplete, a platform-only blocker that prevents enablement for any tenant and surfaces to tenants as `PlatformNotReady` (PRD FR-4)”. A version the Platform Operator declares a tightening change with its recorded field-level diff runs a 30-day grace under the last accepted version; an undeclared change blocks immediately.

`DataHandlingAcceptanceLapsed` is the PRD's name. `DataHandlingUnrecorded` is proposed here for the enablement-time case the PRD leaves unnamed; the Release Operator and Architecture (AD-17 owns the enum) confirm or replace it. The reviewer-suggested `DataHandlingUnaccepted` is not adopted because the PRD already names that case.

#### NFR-9 and business day (lines 112, and under State, Freshness, And Invalidation)

OLD (line 112): `percentile gates using at least 30 production-like executions each.` → NEW:

> gates, each evaluated on its p95 target over at least 30 production-like executions using the nearest-rank percentile method; p99 targets are reported as launch-health values and become `RQ-1` gates only once that gate has at least 300 executions (NFR-9).

Add: `A business day in this register is FR-28's: 24 hours excluding Saturday and Sunday in UTC.`

#### Attestation evidence on the readiness record (lines 106, 108, 202)

Line 108 (`LR-AUDIT-PROTECTION-DELETION`): append “Includes the FR-34 attestation evidence on the readiness record: the verified signed build identity and version against the Security-qualified value custodied through `EXT-SECRETS-1`, and the canary liveness result (seal, no-plaintext persisted bytes, unseal, DEK destroy, `Erased` replay), re-run at startup, on every readiness evaluation, hourly by default [A-24], and on host composition change. Invalidated additionally by engine identity/version, host composition, or attestation-cadence changes.” Line 106 (`LR-SECRETS`): append `and custody of the Security-qualified engine identity and version value the Release Operator provisions (FR-34)`. Line 202: add the attestation to the `PayloadProtectionLiveTests` scope. Placing the evidence under `LR-AUDIT-PROTECTION-DELETION` is the register's assignment; the PRD says only "on the readiness record".

### 4.5 `epics.md`

#### Inventory rows (lines 44, 46, 60, 80, 86, 90)

**FR4 NEW:** The Platform Operator configures the Global Providers Aggregate with Provider records, model options, enabled/disabled state, versioned pricing (server-assigned pricing version starting at 1, incremented on unit-price or currency change; malformed currency, negative price, or a regressed caller version is a typed rejection), positive capability limits, a per-model retry budget usable only after a confirmed no-usage outcome, secret reference and configured state, and a data-handling record (retention term, training-use status, processing region, contractual reference) under its own `DataHandlingVersion`; a model without a complete record cannot be enabled for any tenant; every new version blocks the model per tenant with `DataHandlingAcceptanceLapsed` until the Tenant Agent Administrator accepts it, except a Platform-declared tightening change with a recorded field-level diff, which runs a 30-day grace; `CapabilityVersion` is monotonic and the optimistic-concurrency token; disabled Providers and models cannot be selected, activated, or called; changes are auditable without secret exposure.

**FR5 NEW:** Agent Administrators select a Provider and model from the tenant-enabled Global Providers Aggregate; activation and every call validate the complete eligibility set (platform and tenant enablement, configured Provider, text-generation capability, valid limits and pricing, complete data-handling record with its current `DataHandlingVersion` accepted for the tenant or within a declared tightening grace, and non-regressed `CapabilityVersion`), failing closed with a typed reason, `DataHandlingAcceptanceLapsed` for the data-handling element (own FR-25/SM-C4 class, excluded from FR-28 trigger shares); enough Provider/model identity is retained for audit, and selection changes affect only future Agent Calls.

**FR12 NEW:** The system prevents automatic posting when authorization, Agent lifecycle, the tenant kill switch, the Conversation Agent State (`Blocked` or `ExternallyRemoved`), Provider/model, Party identity, Source Conversation access, Conversation Context Policy, Content Safety Policy, or generation status is invalid; no Conversation Message is created on a failed check or safety failure; authorized status and audit distinguish authorization, context-policy, content-safety, cost-cap, Provider/runtime, and posting failures without leaks; an undeterminable Provider attempt resolves to the typed additive `Indeterminate` outcome, never an implicit success or failure or a silent retry.

**FR22 NEW:** The admin UI lets the Platform Operator manage Global Providers Aggregate entries and tenant enablement and lets Tenant Agent Administrators configure `hexa`, inspect lifecycle state, configure Response and Approver Policy, and view Agent operation and proposal status within the FR-33 matrix; UI actions enforce the same authorization as API/client contracts, never expose Provider secrets, distinguish with distinct labels the `Draft`, `Active`, and `Disabled` lifecycle states and the `Suspended` tenant status, invalid configuration (a readiness classification, not a lifecycle state), the awaiting-decision, posting-failed, and expired proposal states, and the failed-call outcome, and satisfy NFR-13 and NFR-14.

**FR25 NEW:** The system exposes status for Agent readiness, Provider/model readiness, recent Agent Call outcomes, proposal queues, generation failures, approval completion, and posting outcomes so administrators can tell whether `hexa` is callable and distinguish failure classes; status exposes the SM-2, SM-3, SM-7, SM-C4, and SM-C5 inputs, blocked calls by SM-C4 reason and system-abandoned proposals by reason, per Conversation the Conversation Agent State with any `MirrorPending` or `MirrorRefused` flag, per proposal any `ResolutionUnavailable` or `ResolutionEmptyPending` marker and `LateConfirmed` flag, posting failures by typed reason (including `MembershipUnavailable`, `MembershipRejected`, and safety verdicts) and each automatic post's FR-18 posting state, unavailable calls by reason (`MembershipUnavailable`, `ApproverResolutionUnavailable`, `ContextReadUnavailable` with `RescanPending`), `DataHandlingAcceptanceLapsed` as its own class, and separately authorization denials, `TenantSuspended`, `MembershipRejected` at acceptance, capacity, `DependencyNotAvailable`, and `PayloadProtectionUnavailable`; reports `Suspended` distinct from `Disabled`; and shows the cost-cap 80% warning, 100% fail-closed state, and reserved-versus-settled spend.

**FR27 NEW:** The system applies Content Safety Policy to the prompt plus complete Conversation Context before Provider invocation, to generated output before any proposal or Conversation side effect, to a human edit at edit time (always-blocked content is a typed rejection and is not stored; restricted content is stored with the version-record field `RestrictedContent` and is not approvable), and to the exact version at approval and pre-post under every applicable policy version; failed content cannot be posted or approved and Approvers cannot override; the pre-Provider scan reuses a per-Conversation verdict cache keyed by Conversation, `EXT-SECRETS-1` HMAC content hash, and policy version, invalidated tenant-wide on publication, with a bounded background re-scan during which a call fails closed as `ContextReadUnavailable` with `RescanPending`; a failing history records keyed hash, author Party, and timestamp as operator-visible evidence and blocks generation attributed to the Conversation; the scan is excluded from the NFR-9 fast pre-Provider gate.

Also: FR1 row (line 38) and Story 5.2 (line 1318), OLD `an immutable AI-type Party identity owned by the Agents Service Principal` → NEW “an immutable provisioned Agent Party identity owned by the Agents Service Principal (AI-typed once `EXT-PARTIES-1` is `Committed`, Organization-typed until then, verified by id and never by type; A-27)”. Line 144: OLD “`ParticipantType.AiAgent`/`AIAgent`” → NEW “`ParticipantType.AiAgent`”. Line 160 projection ids: replace `agent-setup-readiness`, `provider-capability-pricing` with the register's ratified ids, confirmed against the launch readiness register before editing.

#### Coverage additions

- OQ map: add `OQ-31 | 5.8 protected-content boundary for Agent Instructions and configuration; 8.3 erasure on tenant offboarding or approved deletion naming the Agent`.
- Epic 5 natural dependencies: add `EXT-PARTIES-1`.
- Assumption references: A-22 to 6.6; A-23 to 8.8; A-24 to 5.8 and 5.6; A-25 to 8.4; A-26 to 8.7; A-27 to the new provisioning story and 5.4; A-28 to 8.5.

#### New story: Provision hexa Once Per Tenant (Epic 5)

Proposed id **5.11**, so that no existing story is renumbered; the Product Owner may choose another id. Executes before 5.4 despite the number. Owner: Agents Runtime Maintainer. Status `backlog`.

As a Platform Operator, I want to provision `hexa` for a tenant once, atomically with its Party identity, so that every tenant has exactly one immutable Agent identity that no tenant role can create, delete, or re-point.

Primary Demonstrable Outcome: one Platform-principal `ProvisionAgent` command creates the `Draft` Agent and its provisioned Agent Party identity together; a repeat returns the same Agent; every link and replace path is rejected.

Dependencies. Prior: 5.1, 5.2. External: `EXT-PARTIES-1` per its register status for the AI-type leg (A-27); until `Committed`, the Organization-typed Party path verified by id is the executable interim and consumes no `EXT-PARTIES-1` seam. Forward: 5.4 verifies the identity; 6.6 joins with it.

Acceptance criteria:

1. Given an AD-30 `Platform` principal and a tenant with no Agent, when `ProvisionAgent` is accepted, then EventStore records the Agent in `Draft` with tenant scope and a Party in Hexalith.Parties owned by the Agents Service Principal whose id derives from the Agent id (AI type once `EXT-PARTIES-1` is `Committed`, Organization type until then); the Agent record is durable only together with that Party; FR-24 evidence records actor, timestamp, and identities.
2. Given the tenant is already provisioned, when the command repeats, then it returns the existing Agent and creates nothing, except that it completes a Party a failed earlier provision left missing before returning; exact duplicates and replay are idempotent.
3. Given any tenant role, a `User` or `Administrator` principal, or a tenant-B principal targeting tenant A, when provisioning, deletion of `hexa`, or a second Agent is attempted, then a typed denial occurs before mutation or disclosure.
4. Given the legacy Party link and replace commands, when presented by any path after provisioning, then they are rejected per Story 5.2, and the provision command is proven to be the only identity path (FR-23 register).
5. Given the FR-2 verification points, when identity is checked, then it is verified by id against the provisioned identity and never by Party type (A-27); the `AiAgent` spelling is used everywhere.

Evidence: FR1, FR2, FR19, FR20, FR23, FR24, FR33; OQ-28; A-27; `EXT-PARTIES-1`; Levels 2 and 4; `AgentProvisioningAggregateTests`, `AgentProvisioningPartiesIntegrationTests`, `ProvisioningIdempotencyAndRepairTests`; negative `ProvisioningIsolationTests.TenantRolesAndCrossTenantAreDenied`.

#### Story 5.2 lifecycle read model (line 1302)

OLD: `exposes current identity reference, display metadata, instructions version, lifecycle, response mode` → NEW:

> exposes current identity reference, display metadata, instructions version, lifecycle as exactly `Draft`, `Active`, or `Disabled` (never `Unknown`), response mode

. Add: the `AgentSetupWriteStatus` successor enum with `Unknown = 0`, the current enum under deprecate-and-reject (FR-23, FR-29).:

#### Story 5.3 data handling (replace lines 1363-1364)

**Then** the catalog records the four data-handling fields (retention term, training-use status, processing region, contractual reference resolving to a retained document) under its own `DataHandlingVersion`, incremented only when one of them changes; a model whose record is absent or incomplete cannot be tenant-enabled; a Platform principal may declare a new version a tightening change only with the recorded field-level diff, a loosening never being declarable.
**And** `TenantProviderEnablement(TenantId)` records the Tenant Agent Administrator's acceptance of a named version (the version in force is the last accepted); every undeclared new version blocks the model for that tenant immediately with `DataHandlingAcceptanceLapsed` until accepted; a declared tightening continues under the version in force for 30 days, then blocks with the same reason; a declined declaration blocks immediately; replay, migration, API, UI, and evidence preserve version, declaration, diff, and acceptance without consuming `EXT-PROVIDER-1`.

Add: the `ProviderCatalogInspectionStatus` successor enum (FR-23).

#### Story 5.5 (successor enums)

Add: `AgentInspectionStatus` and `AgentInteractionGateInspectionStatus` successor enums with `Unknown = 0`, current enums under deprecate-and-reject, before the first tenant is enabled (FR-23).

#### Story 5.7 (add two criteria)

**Given** the selected model's current `DataHandlingVersion` is not accepted for the tenant and not within a declared tightening grace / **When** activation or callability is evaluated / **Then** the result is a typed `DataHandlingAcceptanceLapsed` rejection with the version, declaration, and diff visible to the Tenant Agent Administrator / **And** an activation from `Draft` requires acceptance before the first activation that selects the model.

**Given** the tenant carries a `TriggerReviewOverdue` condition on the FR-30 surface / **When** activation is requested / **Then** it is refused with a typed rejection naming the condition, no lifecycle change occurs, and nothing is suspended or deleted / **And** the rejection clears only when the review is recorded.

#### Story 6.1 (add the ten-step order)

**Then** acceptance runs exactly the ten FR-8 checks in order: 1 caller authorization, person-Party check (a non-person caller, `hexa`'s own identity included, is a typed denial), and Source Conversation access; 2 lifecycle, `TenantSuspended`, Party identity, and dependency freshness; 3 Conversation Agent State; 4 Provider/model eligibility and enablement; 5 rate limits and the per-Party concurrent bound; 6 Eligible Approver resolution in Confirmation mode only; 7 context measurement; 8 cost reservation; 9 safety policy presence and the pre-Provider scan; 10 membership. A reservation held by a later failure is released `NotInvoked`; steps 1-8 are measured by the NFR-9 fast gate and steps 9-10 by the safety-scan series.

#### Story 6.2 (line 1885)

OLD: `must be fresh, enabled, configured, priced, text-generation capable, non-regressed, and have valid positive limits before ContextReady` → NEW:

> must be fresh, platform- and tenant-enabled, configured, priced, text-generation capable, non-regressed, have valid positive limits, and carry a `DataHandlingVersion` accepted for the tenant or within a declared tightening grace (otherwise `DataHandlingAcceptanceLapsed`, its own FR-25 class, excluded from FR-28 trigger shares) before ContextReady

. Add: `ContextReadUnavailable` as the typed unavailable context-read outcome, additive to the coarse `ContextUnavailable` blocked reason (FR-9), with sub-reason `RescanPending` during a background re-scan (owned with 6.3).:

#### Story 6.3

Add: `SafetyFailed` as the terminal `AgentInteractionStatus` member for an output safety failure (FR-8); the HMAC-keyed per-Conversation verdict cache under the `EXT-SECRETS-1` secret, tenant-wide invalidation on publication, and the bounded background re-scan with `RescanPending` (FR-27).

#### Story 6.6 (add three criteria)

**Given** Automatic Response Mode generation succeeds / **When** the workflow posts / **Then** the post is carried by a system-approved posting record (not a Proposed Agent Reply, no human Approver) that reuses the `ProposedAgentReplyState` values from `Approved` onward with the generated version as the approved version and the success instant as the approval instant, applies the FR-18 pre-post re-validation, `PostingPending` stored deadline, `PostingFailed` retry bound, `MessageId` lookup, `LateConfirmed`, and `PostingWindowElapsed` (against the Agent's configured expiry at acceptance), substitutes any current Conversation Facilitator where FR-18 names an Eligible Approver, and lets the Tenant Agent Administrator abandon it without an accessibility condition / **And** the Agent Call becomes `Posted` when the record is `Posted` and `PostingFailed` when the record is `Abandoned`, releasing the per-Party concurrency slot and starting the retention clock; a non-terminal record keeps the call non-terminal.

**Given** a `Joined` state and a participant read / **When** the read is compared by the roster version the seam-1 read returns / **Then** only a read at least as fresh as the last confirmed add (join or re-admission) with the last mirror confirmed can record `ExternallyRemoved` (a Conversation with no mirror history counts as confirmed); an older read rejects with `MembershipUnavailable` and changes nothing; absence while the newest clear's re-admission mirror is unconfirmed re-adds idempotently under the standing admission and stays `Joined` / **And** from `ExternallyRemoved`, presence again re-admits only once A-22 is retired in the register; until then the state stays `ExternallyRemoved` and only the FR-33 clear row re-admits.

**Given** a block or re-admission mirror attempt / **When** Conversations answers / **Then** a transient failure is retried under a timeout, "already absent" confirms a block mirror, a block from `NeverJoined` is confirmed by an absent read without a seam call, and a typed permanent refusal ends retry and records `MirrorRefused` as a terminal flag while the authority stays in force / **And** FR-25 exposes `MirrorRefused` per Conversation for the Tenant Agent Administrator (re-set the block for a fresh block mirror, or clear it for a re-admission mirror); a clear is refused only while a mirror attempt is in flight and cancels every outbox entry of a lower `BlockVersion`; the clear's re-admission add is its own at-least-once seam-1 entry under the same `MirrorPending`/`MirrorRefused` terms. An unavailable participant read at acceptance rejects the call with `MembershipUnavailable`; a Conversations refusal of the add rejects with `MembershipRejected`; both are also posting-failure reasons at the pre-post re-validation.

Add A-1, A-15, A-22, and OQ-25 to the 6.6 manifest.

#### Story 6.8 (line 2221)

OLD:

> the safe reasons `NoEligibleApprover`, `RemovedInConversations`, `SourceConversationUnavailable`, and `NotInvoked`

NEW:

> the safe reasons `NoEligibleApprover`, `RemovedInConversations`, `SourceConversationUnavailable`, `NotInvoked`, `MembershipUnavailable`, `MembershipRejected`, `TenantSuspended`, `ContextReadUnavailable` (with `RescanPending`), `DataHandlingAcceptanceLapsed`, and `PostingWindowElapsed`, the `AgentInteractionStatus` member `SafetyFailed`, and the lifecycle value `Draft`

. Add: the nine `Proposal*` `AgentInteractionStatus` state members stay declared and deserializable, are emitted by no handler, and are read as `ProposalCreated` plus the current `ProposedAgentReplyState`; the six `Proposal*Failed` action outcomes remain recorded (FR-23).:

#### Story 7.2

Add: a human edit is scanned at edit time under the effective policy; always-blocked content is a typed rejection and is not stored; restricted content is stored with the version-record field `RestrictedContent`, which makes the version non-approvable and visible under FR-25 (FR-15, FR-27).

#### Story 7.4 (add after line 2444)

**Given** a `PostingPending` record whose stored attempt deadline (no shorter than the seam posting timeout, never extended) has elapsed / **When** any read, command, or post-loss recovery evaluates it, regardless of timer delivery / **Then** the attempt is concluded as timed out and the `MessageId` lookup runs: present, `Posted` with `LateConfirmed`; typed absence, `PostingFailed`; unavailable, stays `PostingPending` and the lookup repeats on the next read, command, or recovery / **And** nothing else leaves `PostingPending`; removal, block, suspension, or abandonment is applied on exit only if the attempt did not post. The approval guard requires the version to carry no `RestrictedContent` marker (FR-18).

#### Story 7.5 (line 2493 and additions)

OLD: `**Given** a pending proposal and current resolution authority / **When** the Approver abandons it` → NEW:

> **Given** a pending or `Approved` proposal and current resolution authority (an Eligible Approver, or for `Approved` the audited Tenant Agent Administrator row that needs no Conversation read access) / **When** the actor abandons it

. Add: **And** an `Approved` proposal is abandoned directly with no `MessageId` lookup because no post was attempted; a `PostingFailed` proposal whose failure was a pre-post re-validation is likewise abandoned without lookup.:

Add: **Given** a proposal has been in `Approved` or `PostingFailed` for longer than its original expiry duration in total, excluding time paused while `Suspended` or `Disabled` / **When** any read, command, or recovery evaluates it / **Then** it is system-abandoned with `PostingWindowElapsed` (for `PostingFailed`, once the lookup finds no message), counts as decided for SM-3, and is reported separately under FR-25/SM-C4.

#### Story 7.7 (line 2612)

OLD: `consumes one audited attempt within the three-attempt/15-minute bound` → NEW:

> consumes one audited attempt within the three-attempt/15-minute bound; an administrative retry is accepted only after the automatic budget is exhausted, re-validates against the `PostingWindowElapsed` bound, and is refused with a typed reason once it has elapsed; a `PostingFailed` proposal whose reason is a safety verdict is never retryable and accepts abandon only

.:

#### Story 8.4 (add)

**Given** the per-tenant kill switch is pulled / **When** the pull is an SM-4 pull with no containment review recorded after two business days (UTC), or a review pull older than 7 days without a recorded review decision / **Then** `SuspensionReviewOverdue` is recorded on the FR-30 surface as a PRD-declared condition / **And** release of an SM-4 pull requires the Platform Operator's containment finding and release of a review pull a recorded review decision, each with audited justification.

**Given** an FR-28 trigger condition with no review convened within one business day / **When** the FR-30 surface is evaluated / **Then** `TriggerReviewOverdue` is recorded and production-like generation cannot be enabled for that tenant until the review is recorded.

Add the A-25 cost-cap override bound (25% of the monthly cap and 7 days per override; a second override in the same month needs Product approval) and the FR-26 wording at line 2870: `must pass every applicable policy version — the attempt's snapshot, the then-current active policy, and any mode-specific policy in addition to the platform policy`.

#### Story 8.5 (line 2914)

OLD:

> each gate requires at least 30 qualifying production-like executions and otherwise returns `InsufficientEvidence`

NEW:

> each p95 gate requires at least 30 and each p99 gate at least 300 qualifying production-like executions, otherwise `InsufficientEvidence` for that gate

. Add the OQ-23 retraction-metric input from seam 7 (A-28) as a reported value pending the OQ-23 decision.:

#### Story 8.7 (add)

**Given** a Release PM `DeferredAssumption` deferral for an `ARCH-A` row added after the recorded `RQ-1` scheduling / **When** it is recorded in the launch readiness register / **Then** the surface shows the row, owner, unexercised FR consequences, and revisit date (at most 30 calendar days), treats it as an `RQ-1` blocker until Product's acceptance is recorded, and refuses it for any row whose retirement condition names Security / **And** a revisit date that passes without retirement or a new accepted deferral returns the row to `UnretiredAssumption`; `TriggerReviewOverdue`, `SuspensionReviewOverdue`, `GateOutOfScope`, and `DeferredAssumption` render as distinct safe conditions.

#### Story 8.8

Add the A-23 aggregation bound: post-hoc inspections by one Inspector touching more than 5 distinct Conversations in a rolling 30-day window are one wide inspection requiring prior Platform Operator approval; every unreviewed inspection is a compliance finding at the next launch-health review (FR-24).

### 4.6 `sprint-change-proposal-2026-09-09-2.md`

Factual corrections only; the approval it records is not reopened.

- `preserves` (line 39): OLD `active Epics 5 through 8 and their 29-story structure` → NEW `active Epics 5 through 8 and their 33-story structure (Stories 5.9, 5.10, 6.8, and 7.7 were added by sprint-change-proposal-2026-09-09-prd-validation-follow-through.md)`.
- Lines 79, 95, 476: replace "29 stories" / "29-story active set" / "29 active stories" with 33.
- Add to §8 (execution log): “2026-09-10 — The PRD resync edits this proposal could not schedule (it preserves `prd.md`) are scheduled by sprint-change-proposal-2026-09-09-3.md, which lists `prd.md` under `amends_if_approved`.”

Retroactively adding `prd.md` to this approved proposal's `amends_if_approved` would extend an approval the Administrator gave on 2026-09-09 to edits that did not exist then. This proposal carries the PRD edits instead.

### 4.7 `sprint-status.yaml`

After, and only after, the approved `epics.md` edit lands, add under `epic-5`:

```yaml
  5-11-provision-hexa-once-per-tenant: backlog
```

Preserve every existing status, comment, retrospective row, and action item. Validate bidirectional parity with the 34 active story headings and parse the YAML.

### 4.8 Explicit Non-Changes

- Do not change any FR, NFR, SM, OQ, or A row in `prd.md` beyond the resync sentences in §4.1 and the conditional FR-7 edit Product may choose.
- Do not remove, renumber, or re-estimate an epic or story.
- Do not mark any dependency `Committed` or `Available`; all ten records stay `Uncommitted`.
- Do not invent external member names, verification commands, target commits, integration dates, Architecture retirement dates, or the `PostingPending` deadline duration.
- Do not amend `DESIGN.md` or `EXPERIENCE.md`; a UX Update run is editing them concurrently.
- Do not treat the §5 code-side list as document amendments.

## 5. Implementation Follow-Ups (code, not document amendments)

Owner for every row: **Agents Runtime Maintainer**, with the Test Architect proving each. These rows are carried as obligations under the named existing stories. They are listed here so the handoff is complete; they do not change story text through this proposal beyond §4.5.

### 5.1 The 22 prior drift findings (5 critical) still open

| ID | Severity | Required code change | Component | PRD | Owning story |
| --- | --- | --- | --- | --- | --- |
| DC-1 | Critical | Reject `ReportingOnlyMonitoring` and `AcceptedLaunchRisk` at readiness recording; emit `ProhibitedCostControlPosture` for a stored posture | `AgentAggregate.cs`, `AgentLaunchReadinessPolicy.cs`, `AgentLaunchReadinessBlocker.cs`, `CostControlPosture.cs` | FR-28, FR-23 | 5.9 |
| DC-3 | Critical | Carry caller and last-editor identities on the approval request; resolve the Conversation-scoped Eligible Approver set; refuse the caller or the editor of the selected version | `AgentInteractionProposalApprovalOrchestrator.cs`, `AgentInteractionProposalApprovalRequest.cs` | FR-7, FR-17 | 7.4, 7.1 |
| DC-4 | Critical | Scan at edit time and re-scan the exact selected version at approval and immediately before append under the then-current policy | approval and edit orchestrators | FR-17, FR-27, FR-18 | 7.2, 7.4 |
| DC-5 | Critical | Pre-Provider scan of prompt and Conversation Context before `InvokeProviderAsync`, with the keyed-hash cache | generation and regeneration orchestrators | FR-27, FR-8 | 6.3, 7.3 |
| DC-6 | Critical | Cost-cap and rate-limit configuration, readiness blockers, and the FR-8 reservation and rate-limit gate checks | `AgentLaunchReadinessPolicy.cs`, `AgentInteractionGateCheck.cs`, `AgentInteractionGateOrchestrator.cs` | FR-28, FR-32, FR-8 | 8.4, 5.5, 6.4, 6.5 |
| DH-1 | High | Server-reject `BlockWithAuditableOverride` and `Caller` wherever presented; remove `Caller` from the configuration UI | `AgentAggregate.cs`, `ContentSafetyFailureHandling.cs`, `ApproverPolicySourceKind.cs`, `ApproverPolicy.razor` | FR-23, FR-7 | 5.10 |
| DH-2 | High | Mark both Party link and replace handlers obsolete with typed rejection; add the Platform-scoped provision command that creates the Party; deny `CreateAgent` to tenant roles | `AgentAggregate.cs`, `AgentPartyIdentityOrchestrator.cs`, `AgentsFrontComposerRegistration.cs` | FR-1, FR-2, FR-33 | 5.11 (new), 5.2 |
| DH-3 | High | Remove `PostingFailed` from the terminal set; expose only PRD-authorized actions guarded by the `MessageId` lookup | `ProposalDetail.razor`, `ProposalDetailTests.cs` | FR-18, FR-22 | 7.7 |
| DH-4 | High | Dedicated retry command with budget; abandon exit from `PostingFailed`; rejection of approve or retry while `PostingPending`; `MessageId` lookup on exit | `AgentInteractionAggregate.cs`, approval orchestrator, `AgentProposalApprovalPolicy.cs` | FR-18 | 7.7, 7.4, 7.5 |
| DH-5 | High | Gate `Membership` check, the `ConversationAgentState` machine, block/mirror/clear handling | gate orchestrator, `AgentInteractionGateCheck.cs`, posting orchestrator, `ConversationClientResponsePoster.cs` | FR-2, FR-8, FR-11 | 6.6 |
| DH-6 | High | Command-time `ExpiresAt` enforcement on approve, edit, regenerate; 24-hour default | `AgentInteractionAggregate.cs`, `AgentProposalExpiryPolicy.cs` | FR-18, OQ-3 | 7.6, 7.2, 7.3, 7.4 |
| DH-7 | High | Safe Context Budget subtracts Agent Instructions, caller prompt, system framing, and safety margin | `AgentInteractionContextPolicy.cs` | FR-9 | 6.2 |
| DH-8 | High | Typed `Indeterminate` terminal outcome; retry budget not handed to the adapter unconditionally | `AgentOutputGenerationResult.cs`, generation orchestrators | FR-12, FR-28 | 6.4 |
| DH-9 | High | Lifecycle and tenant-suspension re-validation on approve, edit, regenerate; kill switch and `Suspended` status | approval, edit, regeneration orchestrators | FR-3, FR-18, FR-28 | 7.4, 8.4 |
| DM-1 | Medium | Baseline additive members (`AgentInteractionContextMode.Blocked`, `NoEligibleApprover`, `RemovedInConversations`, `SourceConversationUnavailable`, `NotInvoked`, `Membership`, `ProhibitedCostControlPosture`, `PayloadProtectionUnavailable`, `Suspended`) | context mode, gate check, readiness blocker enums | FR-9, FR-7, FR-2, FR-8, FR-28 | 6.8 umbrella |
| DM-2 | Medium | Second-update typed names, the scheduled FR-7 re-check job, FR-30 fields for `GateOutOfScope` and `TriggerReviewOverdue` | `AgentLaunchReadinessView.cs` and new symbols | FR-7, FR-18, FR-25, FR-28, FR-30 | 6.8 umbrella; 5.3, 7.1, 7.4/7.5, 7.6/8.5, 5.5/8.7, 6.6 |
| DM-4 | Medium | Provider data-handling record, `DataHandlingVersion`, tenant acceptance, per-tenant enablement in the catalog and activation eligibility | `ProviderCatalogAggregate.cs`, `AgentActivationProviderRevalidation.cs` | FR-4, FR-5 | 5.3, 5.7 |
| DM-5 | Medium | Approver Policy structural rule at configuration; Conversation-scoped Eligible Approver resolution before Provider work | `AgentAggregate.cs`, gate orchestrator, `IApproverPolicyResolver.cs` | FR-7 | 7.1, 5.4 |
| DM-6 | Medium | Regeneration ceiling (default 3, range 1-10 per Agent) with typed rejection | regeneration orchestrator, `AgentInteractionAggregate.cs` | FR-16, FR-32 | 7.3 |
| DM-8 | Medium | FR-25 read-model fields: per-reason blocked-call counts, system-abandon counts, per-proposal markers, cost-cap consumption | `AgentStatusView.cs` | FR-25 | 8.5 |
| DL-2 | Low | Correct `AgentSetupWriteStatus.Submitted = 0` through a successor enum | `AgentSetupWriteResult.cs` | FR-23, FR-29 | 5.2 |
| DL-3 | Low | FR-26 policy-version rule on the retry path, now the "every applicable version" form | retry path per DH-4 | FR-26 | 7.7, 7.4 |

Code parts of the partially applied findings: DC-2 (`PayloadProtectionUnavailable` blocker, host-supplied attestation port, typed outcome on every FR-21 content-bearing path; 5.8, 5.6, 5.5/8.7) and DM-3 (split the unauthorized load outcome from the unavailable one; 6.2). AH-7's code part (Release-Operator-owned committed target) is superseded by row C-4 below.

### 5.2 Gate findings (review-implementation-drift.md) and reconcile additions

| ID | Severity | Required code change | Component | PRD | Owning story |
| --- | --- | --- | --- | --- | --- |
| D-H-3 / C-7 | High | Additive `ContextReadUnavailable` for the narrowed context-read-failure class; legacy `ContextUnavailable` keeps its coarse meaning; `RescanPending` sub-reason | `AgentInteractionContextBlockReason.cs`, `AgentInteractionContextPolicy.cs` | FR-9, FR-25, FR-27 | 6.2, 7.4 |
| D-M-3 | Medium | Third-update members and mechanisms: `PostingWindowElapsed`, `MirrorRefused`, `SuspensionReviewOverdue`, `DeferredAssumption`, A-22 re-admission gate, A-23 aggregation, A-24 cadence, A-25 override bound; plus `TenantSuspended` and `DataHandlingAcceptanceLapsed` with the declared-tightening rule | new symbols | FR-2, FR-11, FR-18, FR-28, FR-30 | 7.7, 6.6, 5.5/8.7, 8.8, 5.8, 8.4, 5.3/5.7 |
| D-M-4 / C-5 | Medium | Person-only Approver rule: read the Parties type at configuration and resolution | `AgentAggregate.cs`, `IApproverPolicyResolver.cs` | FR-7 | 5.4, 7.1 |
| D-M-5 / C-6 | Medium | Edit-time scan; `RestrictedContent` version-record field | `AgentInteractionProposalEditOrchestrator.cs`, version records | FR-15, FR-27 | 7.2 |
| D-M-6 / C-8 | Medium | Additive gate checks in FR-8 order for the seven steps without one | `AgentInteractionGateCheck.cs` | FR-8 | 6.8 umbrella; per step 6.6, 6.5, 7.1, 6.2, 6.4, 6.3 |
| D-L-3 | Low | `EXT-SECRETS-1` custody port for the engine identity value and the per-tenant HMAC secret | new port | FR-34, FR-27 | 5.8, 6.3 |
| D-L-4 | Low | Percentile, sample-floor, and method fields on the latency target; nearest-rank p95 over at least 30 executions; p99 at 300 | `ResponseModeLatencyTarget.cs` | FR-28, NFR-9 | 5.5, 8.5, 8.7 |
| C-1 | Medium | Posting-record shape for automatic calls, reusing `ProposedAgentReplyState` from `Approved` onward | `AgentInteractionState.cs`, `AgentInteractionAggregate.cs` | FR-11, FR-18 | 6.6, 7.7 |
| C-2 | Medium | Stored `PostingPending` deadline evaluated on read, command, and recovery | posting record | FR-18 | 7.4, 7.7; blocked on `ARCH-A-14` |
| C-3 | Low | Successor enums for the four zero-valued members; old enums under deprecate-and-reject | four status enums | FR-23 | 5.2, 5.3, 5.5 |
| C-4 | High | Verify the Security-qualified signed engine identity and version in the FR-34 attestation; record the supplying principal | attestation (with DC-2) | FR-34 | 5.8 |
| C-9 | Medium | Attestation at startup, every readiness evaluation, hourly cadence (A-24), and host composition change; pin; fail closed on mismatch | attestation | FR-34 | 5.8 |
| D-H-1 follow-on | Medium | Stop emitting the nine `Proposal*` statuses; a received one reads as `ProposalCreated` plus the proposal state | `AgentInteractionStatus` emitters | FR-8, FR-23 | 7.1, 7.7 |

### 5.3 Spine round-5 follow-ups

| ID | Required action | Owning story |
| --- | --- | --- |
| `ARCH-A-13` | Product confirms or declines the fourth Eligible Approver exclusion before code implements it | 7.1, 7.3 (decision-gated; §7 item 1) |
| `ARCH-A-14` | Architecture proposes a concrete `PostingPending` deadline duration, or defers explicitly to the seam's configured timeout, with Product confirmation | 7.4, 7.7 (prerequisite for C-2) |

Duplicates across sources point to the earliest id: the context split is DM-3 = D-H-3 = C-7; the edit-time scan is DC-4 (edit half) = D-M-5 = C-6; the FR-8 checks are DC-5, DC-6, DH-5, DM-5 = D-M-6 = C-8; the attestation value is DC-2 with AH-7 superseded by C-4; the cadence is AM-15 = D-M-3's A-24 item = C-9; the zero-valued enums are DL-2 extended by D-L-1 = C-3. DM-7 needs no code change; the gate confirmed the catalog already matches the server-assigned pricing version.

## 6. Implementation Handoff

### Scope Classification

**Moderate.** One story is added and the backlog reorganizes around it; the seven artifacts change together; no fundamental Product or architecture replan is required.

### Sequencing

1. Administrator reviews this proposal and records the three decisions in §7 (ARCH-A-13, ARCH-A-11 confirmation, the interim Party identity as the V1 launch identity).
2. PRD Update run applies §4.1 to `prd.md`; the `updated:` field records it.
3. Solution Architect applies §4.2; index version becomes 6; co-owners supply the eleven literal dates.
4. Register owners apply §4.3 and §4.4. `EXT-PARTIES-1` enters `Uncommitted`; every record stays `Uncommitted`.
5. Product Owner applies §4.5 and §4.6, then adds the §4.7 sprint-status key.
6. Agents Runtime Maintainer works §5 in story dependency order; external seam execution stays gated by the register's `Committed` and `Available` rules.
7. Test Architect runs document consistency checks (every new typed name has one definition and one emitter; A-1..A-28 round-trip; FR-8 step citations; story-count parity) and the contract, transition, attestation, and parity tests.
8. UX Owner reconciles `DESIGN.md` and `EXPERIENCE.md` against the eight new typed names and the Release PM after the concurrent UX Update run completes.

### Recipients And Responsibilities

| Recipient | Responsibility |
| --- | --- |
| Administrator (as Product authority) | Approve the proposal; decide §7 items 1-3; keep FR text otherwise unchanged |
| Product Owner | Apply the epics, proposal-file, and sprint-status edits; assign the provisioning story id; bind `EXT-CONV-UI-1` kind 4 to a story if warranted |
| Solution Architect | Apply the Spine residuals; obtain literal `TargetRetirementDate` values; propose the `ARCH-A-14` duration; retire `ARCH-A-11` on the recorded confirmation |
| Release Operator | Apply the launch-register edits; record the governance record kinds; record Product confirmations and acceptances; validate emitter ownership |
| Release PM | Own the dependency-exclusion and late-row-deferral records as defined; hold no runtime permission |
| Parties Maintainer | Accept or correct the `EXT-PARTIES-1` entry; supply target, date, command, and evidence level |
| Conversations Maintainer | Accept or correct seam 1, seam 5, and seam 7 wording; supply member names, roster-version shape, and command |
| Platform Maintainer | Accept the `EXT-SECRETS-1` custody scope and the `EXT-HOST-1` comparison-target change |
| EventStore Maintainer | Accept the `EXT-PROTECTION-1` signed-identity verification clause |
| Security Engineering | Qualify the production engine build and record its signed identity and version; confirm the `ARCH-A-12` date |
| Agents Runtime Maintainer | Implement §5 under the named stories without breaking wire compatibility |
| Test Architect | Prove serialization, transition, deadline, membership, attestation, cross-tenant denial, metrics, and UI/API parity behaviour |
| UX Owner | Reconcile the UX spines after the concurrent Update run |

### Success Criteria

1. `prd.md` §0, §8, §8.1, and OQ-27 cite the Spine at `updated: 2026-09-10` and the register at `updated: 2026-09-10`, list only the residual divergences, and no longer call the Story 5.3 approval open.
2. The Spine's index version is 6; AD-7/AD-2 define `MirrorRefused`, the roster-version rule, and the acceptance-time membership rejections; AD-17 counts every owner; AD-20 evaluates every applicable version; AD-21 has one settlement instant; AD-22 has the compliance finding; AD-10/AD-2 carry the data-handling model; the Conventions clause names successor enums; no Architecture-owned row carries `TBD` or a milestone.
3. The dependency register has ten `Uncommitted` records, seven seams on `EXT-CONV-AI-1`, by-id Party verification, the `EXT-SECRETS-1` custody scope, four `EXT-CONV-UI-1` artifact kinds, and the OQ-17 non-clearing clause.
4. The launch register defines and emits `DeferredAssumption` and `SuspensionReviewOverdue`, carries the governance record kinds, states the attestation in Security-qualified-identity terms, blocks enablement and activation on `TriggerReviewOverdue`, gates on the data-handling record, and states the NFR-9 percentile rules.
5. `epics.md` has 34 active stories with the provisioning story, six rewritten inventory rows, and every new typed name owned by exactly one story; `sprint-status.yaml` matches.
6. The 2026-09-09-2 proposal says 33 where it said 29 and points to this proposal for the PRD edits.
7. No dependency advanced in status and no owner value was invented.

## 7. Open Decisions And Boundaries

### Decisions requested from the Administrator as Product authority

1. **`ARCH-A-13`, fourth Eligible Approver exclusion.** The Spine's AD-8 excludes the Approver who requested the regeneration of the version under decision; FR-7 names only the caller and the last editor. Confirm (FR-7 gains the exclusion per §4.1) or decline (AD-8 drops it). Not decided here.
2. **`ARCH-A-11`, `Disabled`-Agent retry-clock pause.** FR-3 and FR-18 state the rule; the Spine row awaits a recorded Product confirmation. Confirming at approval lets the Release Operator record it and Architecture retire the row. Not decided here.
3. **Interim Party identity as the V1 launch identity (A-27).** The PRD allows V1 to launch on the Organization-typed Party verified by id, recorded in the register, which retires A-27 without `EXT-PARTIES-1` reaching `Committed`. If Product records that, `EXT-PARTIES-1` stops being an `RQ-1` input and the provisioning story's AI-type leg becomes post-V1. Not decided here.

### Owner commitments required, not Product scope decisions

1. Literal `TargetRetirementDate` values for `ARCH-A-1`, `-2`, `-3`, `-4` (test-stack item), `-6`, `-7`, `-8`, `-11`, `-12`, `-13`, `-14`. A Release PM `DeferredAssumption` deferral is not available for these rows: it applies only to a row added after the recorded `RQ-1` scheduling event, and all eleven predate any such event.
2. A concrete `PostingPending` deadline duration or an explicit deferral to the seam timeout (`ARCH-A-14`).
3. `EXT-PARTIES-1`, `EXT-CONV-AI-1`, `EXT-SECRETS-1`, `EXT-HOST-1`, and `EXT-PROTECTION-1` owners accepting the changed artifacts and supplying targets, dates, and commands. `TBD` remains blocking.
4. The name of the enablement-time data-handling reason code (`DataHandlingUnrecorded` is proposed).

### Concurrent activity and boundaries

- A UX Update run started 2026-09-10 is editing `EXPERIENCE.md` and the UX memlog while this proposal was written. Neither UX spine carries any of the eight new typed names or the Release PM. This proposal does not amend them; the UX Owner reconciles after that run completes, before Stories 5.7, 6.6, 7.4, 7.5, 8.4, or 8.7 become `ready-for-dev`.
- Reviewer findings the PRD update deferred to Product (adversarial M-2, M-3, M-4, M-8, M-10..M-14, L-3, L-4, L-6..L-10; rubric R-M-4..R-M-6, R-L-1, R-L-5; consistency CX-18) are not reopened here.
- The Story 5.3 `NC-5.3-PLATFORM-CATALOG-SCOPE` non-conformance record stays `Open`; Story 5.3 stays `backlog` with its 2026-09-09 annotation.

## 8. Checklist Record

| Checklist item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [N/A] | Trigger is the third PRD update and its reviewer gate, not a story |
| 1.2 Core problem | [x] | Downstream authorities lag the PRD; the PRD lags the Spine round-5 update and the Branch B ruling |
| 1.3 Evidence | [x] | Update report, reconciliation, drift review, Spine update report, and on-disk verification of all five artifacts |
| 2.1 Current epic viability | [x] | Epics 5-8 remain viable |
| 2.2 Required epic changes | [x] | One story added to Epic 5; criteria added in Epics 5-8 |
| 2.3 Remaining epic impact | [x] | Every active epic has named changes |
| 2.4 Obsolete/new epics | [N/A] | None |
| 2.5 Order/priority | [x] | Epic order stands; the provisioning story executes before 5.4 |
| 3.1 PRD conflict | [x] | Resync amendment only; scheduled through this proposal |
| 3.2 Architecture conflict | [x] | Residuals in AD-2, AD-5, AD-7, AD-8, AD-10, AD-12, AD-13, AD-14, AD-17, AD-20, AD-21, AD-22, AD-28, Conventions, and the Assumptions table |
| 3.3 UI/UX conflict | [!] | UX spines lack the new vocabulary; a concurrent UX Update run is in progress; not amended here |
| 3.4 Other artifacts | [x] | Both registers, the prior proposal, sprint status, and the code-side list assessed |
| 4.1 Direct adjustment | [x] Viable | Selected |
| 4.2 Rollback | [x] Not viable | Would reopen closed divergences |
| 4.3 MVP review | [x] Not viable | No scope reduction warranted |
| 4.4 Recommended path | [x] | Direct adjustment |
| 5.1 Issue summary | [x] | Included |
| 5.2 Impact/artifact needs | [x] | Included |
| 5.3 Path and trade-offs | [x] | Included |
| 5.4 MVP/action plan | [x] | MVP unchanged; sequencing defined |
| 5.5 Handoff | [x] | Recipients and responsibilities defined |
| 6.1 Checklist review | [x] | All applicable items addressed; open items explicit |
| 6.2 Proposal accuracy | [x] | Every OLD quotation verified on disk 2026-09-10 |
| 6.3 User approval | [x] | Administrator explicitly approved the proposal on 2026-09-11; the three §7 Product decisions were not decided by that approval |
| 6.4 Sprint-status update | [!] | Deferred until the epics edit lands (§4.7) |
| 6.5 Handoff confirmation | [x] | Moderate-scope handoff routed to the recipients and responsibilities in §6 |

## Approval Gate

This proposal was produced in an autonomous run on 2026-09-10 with no user present. Administrator explicitly approved it on 2026-09-11. The edits under `amends_if_approved` are authorized for implementation through the handoff in §6. The approval does not decide the three §7 Product decisions (`ARCH-A-13`, the `ARCH-A-11` confirmation, the interim Party identity as the V1 launch identity); each remains open until recorded separately, and no dependency status advances without its owner accepting every required field.

## 9. Workflow Execution Log

- 2026-09-10 — Correct Course activated in Batch mode for the third 2026-09-09 PRD downstream reconciliation; the run was autonomous.
- 2026-09-10 — The PRD, its update report and reconciliation, the drift review, the Spine and its round-5 update report, both registers, the epics, the sprint status, the two 2026-09-09 proposals, and the UX spines were assessed; five parallel verifications quoted current text against the PRD.
- 2026-09-10 — Direct adjustment with one added provisioning story was selected; rollback, folding provisioning into Story 5.2, a remediation epic, and MVP reduction were rejected.
- 2026-09-10 — The proposal was written to `sprint-change-proposal-2026-09-09-3.md` and presented for approval; no artifact under `amends_if_approved` was edited.
- 2026-09-10 — Appendix A (options analysis per proposed solution) was added at the Administrator's request; every solution was confirmed with the refinements listed in its summary table.
- 2026-09-11 — Administrator explicitly approved the proposal. Moderate-scope handoff routed to the recipients in §6. The three §7 Product decisions remain open.

### Handoff Completion

The approved handoff package is §4's exact PRD-resync, Spine, register, epics, prior-proposal, and sprint-status edit set, §5's implementation follow-ups, and Appendix A's refinements, sequenced by §6 and bounded by §7. Step 1 of the sequencing (the three Product decisions) is still pending; steps 2 through 8 may proceed in parallel where they do not depend on it. Implementation may begin only under the register's `Committed`/`Available` rules and must satisfy all seven success criteria before the course correction is complete.

## Appendix A. Options Analysis Per Proposed Solution

Each entry states how the proposed solution works, what it buys, what it costs, and the recommendation. Alternatives are the ones a reviewer would reasonably raise; none is a speculative redesign.

### A.1 Scheduling the PRD resync through this proposal (§4.1)

**How it works.** The PRD's §0 rule says a PRD edit lands only when a PRD Update run applies an approved proposal that lists `prd.md` under `amends_if_approved`. This proposal lists it and confines the edits to pointers, the divergence list, the Story 5.3 sentence, and OQ-27's date.

**Pros.** Keeps the PRD's own landing rule intact; the edits are mechanical and reviewable in one place; no FR changes ride along unnoticed.

**Cons.** One more approval and one more PRD Update run for text that is purely derivative. The `updated:` date in §8.1 will go stale again the next time the Spine increments, so this class of edit recurs.

**Alternatives.** Retroactively adding `prd.md` to the approved 2026-09-09-2 proposal would extend an approval to edits that did not exist when it was given. Editing the PRD without a proposal violates §0. Neither is acceptable.

**Recommendation.** Proceed as proposed. Separately, Product should consider, in a later PRD update, dropping the literal Spine `updated:` date from §8.1 and keying the pointer on `architecture_assumption_index_version` alone, which §8.1 already names as the version; that ends the date ping-pong without weakening the rule. It is not proposed here because it is a rule change, not a resync.

### A.2 Amending the Spine residuals in place, index version 6 (§4.2)

**How it works.** Round 5 already absorbed most of the change set. The proposal applies only the verified residuals as text edits and increments the index version, which by the PRD's rule ends the "FR text governs" period for every item it closes.

**Pros.** Smallest change that restores one authority chain; no re-validation round is needed for edits that quote the PRD; the index-version increment is the mechanism both documents already agree on.

**Cons.** The Spine's reviewer gate (five lenses) does not run on these edits unless Architecture chooses to; a small risk that a residual edit conflicts with a round-5 sentence not quoted here. Eleven rows still need dates from co-owners, which the edit itself cannot supply.

**Alternative.** Leave the Spine as is and let the FR text govern the residual items. That is legal under §8.1 but leaves contradictions (AD-7's "never by acceptance", AD-22's "no further escalation") in the document developers read first.

**Recommendation.** Apply the residuals; have Architecture run only the rubric and adversarial lenses on the diff, not a full round.

### A.3 Membership model: roster version, no-mirror `ExternallyRemoved`, terminal `MirrorRefused` (§4.2 AD-7/AD-2, §4.5 Story 6.6)

**How it works.** A removal is recorded only from a participant read whose roster version is at least as fresh as the last confirmed add; an older read rejects the call with `MembershipUnavailable` and changes nothing. `ExternallyRemoved` is a rejecting state with no outbound mirror, because there is nothing to remove. A clear issues its own re-admission add as a mirror entry; a permanent refusal from Conversations ends retries and records `MirrorRefused`, which the Tenant Agent Administrator resolves by re-setting the block or clearing it.

**Pros.** Closes the false-removal race the reviewers found (a stale read after a clear); no unbounded retry loop on a permanent refusal; every state change has one recorded cause.

**Cons.** The freshness comparison depends on A-15 (Conversations returns a roster version); until that seam is committed the only safe behaviour is to reject with `MembershipUnavailable` whenever no version is present, which will read as an outage in early integration. `MirrorRefused` adds one more administrator action to document and test.

**Alternative.** Two-read `RemovalPending` confirmation (reviewer H-2). Rejected by the PRD update because it doubles seam calls on every absent read and still cannot distinguish a stale read.

**Recommendation.** Proceed as proposed; make the "no roster version means `MembershipUnavailable`" fallback explicit in Story 6.6's tests so the fail-closed behaviour is intentional, not accidental.

### A.4 Content safety: evaluate every applicable policy version (§4.2 AD-20)

**How it works.** At regeneration, approval, and pre-post, the content is evaluated under the attempt's snapshot pair and the then-current pair and passes only if every evaluation passes. The `LastLoosensVersion` ordering remains as a shortcut to skip the snapshot evaluation when the current pair is provably at least as restrictive.

**Pros.** Matches FR-26 exactly; a re-check can only tighten; the shortcut keeps the common case (policy unchanged or provably tightened) at one evaluation.

**Cons.** When a policy changed in a mixed direction between snapshot and now, the check costs two classifier evaluations. The cost is bounded to that case and to approval and pre-post, both outside the NFR-9 fast gate.

**Alternative.** Keep the single-pair selection. It is cheaper but can skip the current policy in the "otherwise" branch, which is the defect.

**Recommendation.** Proceed as proposed.

### A.5 Data-handling placement on `TenantProviderEnablement` (§4.2 AD-10/AD-2)

**How it works.** The catalog entry carries the four-field record under `DataHandlingVersion`; the per-tenant enablement aggregate records the version the Tenant Agent Administrator last accepted; the tenant join blocks with `DataHandlingAcceptanceLapsed` when the catalog version is newer, except during a declared tightening grace.

**Pros.** No new aggregate; the acceptance is tenant-scoped exactly where tenant enablement already lives; the join rule is one comparison plus one date check.

**Cons.** Two writers on one aggregate (Platform Operator for enablement, Tenant Agent Administrator for acceptance), so the command authorization matrix on that aggregate must be per command, not per aggregate. The PRD leaves placement to Architecture, so Architecture may still choose differently.

**Alternative.** A separate `DataHandlingAcceptance` aggregate per tenant and entry. Cleaner authorization, one more aggregate, one more projection, no functional gain for V1.

**Recommendation.** Proceed with the enablement aggregate; state the per-command authorization in AD-30's matrix when the edit lands.

### A.6 `EXT-PARTIES-1` with the interim identity and a narrow consumer list (§4.3)

**How it works.** The entry enters `Uncommitted` with all owner fields `TBD`. Only the new provisioning story's AI-type leg consumes it; the interim Organization-typed Party verified by id is PRD text and consumes no seam, so Stories 5.2, 5.4, and 6.6 are not blocked by this record.

**Pros.** Honest register state without freezing three stories on a dependency the PRD explicitly allows V1 to launch without; A-27's retirement path (Product records the interim identity as the launch identity) stays open.

**Cons.** A reader could assume the Party type is checked somewhere; the by-id rule must be enforced in code and tested negatively (an AI-typed Party with the wrong id is rejected, an Organization-typed Party with the right id is accepted). If Product later insists on the AI type for V1, the three stories become consumers after all.

**Alternative.** List 5.2, 5.4, and 6.6 as consumers. It is the conservative reading of the gate rule but blocks `ready-for-dev` on a seam those stories do not execute, contradicting the PRD's own interim rule.

**Recommendation.** Proceed as proposed and ask Product to decide §7 item 3 at approval, because that decision determines whether `EXT-PARTIES-1` is an `RQ-1` input at all.

### A.7 `EXT-SECRETS-1` custodies the Security-qualified engine identity (§4.3, §4.4)

**How it works.** Security qualifies the build; the Release Operator records the signed identity and version in the launch register and provisions the value through the secrets seam; the runtime verifies the loaded engine's signed identity against that value on every attestation and pins it.

**Pros.** The engine can never attest itself; the comparison value has a custodian, a provenance record, and a rotation path; a missing value fails closed.

**Cons.** Adds a Security qualification step and a provisioning step before any production-like evidence can exist; the register and the seam must agree on one value, so the register record must carry the exact string the seam delivers.

**Alternative.** Host configuration or a launch-register-only value. Both leave the comparison value readable or writable outside the secrets boundary; the PRD names the seam explicitly, so there is no real choice.

**Recommendation.** Proceed; have the attestation record the supplying principal of each compared value (the optional AD-14 clause), since that is what makes a mismatch diagnosable.

### A.8 Launch governance records as register documents recorded by the Release Operator (§4.4)

**How it works.** The `RQ-1` scheduling event, exclusions, deferrals, acceptances, containment reviews, trigger reviews, and the engine qualification are rows in the register document, written by the Release Operator (or the Platform Operator for the containment finding). The Release PM holds no runtime permission and is not an AD-30 principal kind.

**Pros.** No new principal, command, aggregate, or UI; matches the PRD's statement that planning-party decisions are recorded in the register by the Release Operator; the `RQ-1` evaluation cites record ids, which is enough for audit.

**Cons.** Not machine-enforced: an `RQ-1` evaluation could omit a deferral row by mistake. The Release Operator becomes the recording hand for parties whose decisions it does not own, which needs a clear provenance column.

**Alternative.** Runtime commands and a `ReleasePM` principal kind. Over-engineering for V1: the PRD says the Release PM has no runtime permission, and the records change a handful of times per release.

**Recommendation.** Proceed; require every `RQ-1` evaluation record to list the ids of the deferral and exclusion records it considered, so omission is detectable.

### A.9 Data-handling reason codes: PRD name plus one proposed platform code (§4.4)

**How it works.** `DataHandlingAcceptanceLapsed` (the PRD's name) covers the tenant-facing case. `DataHandlingUnrecorded` is proposed for the platform-only case of an absent or incomplete record, surfacing to tenants as `PlatformNotReady`.

**Pros.** One code per distinct operator action (accept a version, complete a record); no invented vocabulary beyond one name the PRD leaves open; the reviewer's `DataHandlingUnaccepted` is dropped because the PRD already names that case.

**Cons.** One new enum member owned by Architecture (AD-17), which is why it is flagged as a name to confirm.

**Alternative.** Reuse `Unconfigured` for the absent-record case. Zero cost, but it merges "no secret" with "no data-handling record" on the Platform Operator's screen, which are different fixes.

**Recommendation.** Adopt the dedicated code unless the AD-17 owner objects; accept `Unconfigured` reuse as the fallback.

### A.10 `TriggerReviewOverdue` as an activation and enablement block (§4.4, Stories 5.7 and 8.4)

**How it works.** The condition is recorded on the tenant's FR-30 surface, owned by the aggregate Story 8.4 gives the kill switch. Activation (5.7) and production-like enablement (8.4) read it at command time and refuse with a typed rejection.

**Pros.** Exactly the PRD's consequence, no suspension, no deletion; two command guards and one condition, all in aggregates that already exist in the backlog.

**Cons.** A cross-aggregate read at command time; if the read is unavailable the guard must fail closed, which will refuse activation during a governance-store outage.

**Alternative.** Event-driven replication of the condition onto the Agent aggregate. More moving parts for the same outcome.

**Recommendation.** Proceed with the command-time read and state the fail-closed rule on unavailability in the story tests.

### A.11 A new provisioning story, id 5.11 (§4.5)

**How it works.** One Platform-principal create-only command creates the `Draft` Agent and its Party atomically, repairs a missing Party on repeat, and is the only identity path. It is numbered 5.11 to avoid renumbering but executes before 5.4.

**Pros.** Gives the FR-1 leg an owner with the right principal kind, seam, and evidence; keeps Story 5.2's fixed in-progress scope intact; a clean dependency target for 5.4 and 6.6.

**Cons.** The number does not match execution order, which the sprint status must carry as a comment; the active count becomes 34, so every "33" written today ages.

**Alternatives.** Fold into 5.2: mixes a Platform-principal write into a tenant-role story that is mid-implementation. Fold into 5.4: 5.4 verifies identity, it does not create it, and its dependency is the Parties adapter read, not a write.

**Recommendation.** Proceed with the new story; let the Product Owner choose the id but keep the "executes before 5.4" note wherever it is numbered.

### A.12 Posting record split across Stories 6.6, 7.4, 7.5, and 7.7 (§4.5)

**How it works.** 6.6 creates the system-approved posting record for the automatic path because it is the first story that posts; 7.4 adds the `PostingPending` deadline evaluation; 7.5 adds human abandon from `Approved` and `PostingWindowElapsed`; 7.7 adds the administrative-retry age bound and the one-state-source parity test.

**Pros.** Follows epic order (Epic 6 ships before Epic 7); each addition lands with the story that already owns the surrounding transition; no story waits on a later epic.

**Cons.** Four stories touch one mechanism; the risk is two implementations of the same transition table. `ARCH-A-14` (deadline duration) is still open, so 7.4's deadline criterion cannot be finished until Architecture supplies it.

**Alternative.** Concentrate everything in 7.7. Then 6.6 cannot post automatically without a record, or posts without the FR-18 rows, which is the defect the PRD corrected.

**Recommendation.** Proceed as proposed; make 7.7's existing "one state/transition source" criterion the acceptance test that catches a second implementation, and sequence the `ARCH-A-14` decision before 7.4 starts.

### A.13 Literal dates for the eleven `ARCH-A` rows, no deferral (§4.2, §7)

**How it works.** The co-owners named on each row supply a calendar date; until then each row blocks `RQ-1` on its own, as §8.1 states.

**Pros.** Nothing is invented; the PRD's rule is applied as written.

**Cons.** Eleven dates from six different co-owners is a coordination cost, and any date that later slips escalates to Product.

**Alternative.** A Release PM `DeferredAssumption` deferral. Not available: the deferral applies only to a row added after the recorded `RQ-1` scheduling event, and all eleven rows predate any such event. The only paths are a date or retirement.

**Recommendation.** Collect the dates in one batch when the Spine amendment lands, with Architecture proposing a default the co-owners confirm or change; treat any row still `TBD` after that batch as an escalation to Product at the next readiness review.

### A.14 Code follow-ups under existing stories, no new debt stories (§5)

**How it works.** Every code-side finding is assigned to a story that already owns the surrounding behaviour; most land in the four debt stories the first 2026-09-09 proposal created (5.9, 5.10, 6.8, 7.7) or in the feature story that owns the transition.

**Pros.** No further backlog growth; the debt stories were created for exactly this purpose; duplicates across the three sources collapse to one owner each.

**Cons.** Story 6.8 is becoming an umbrella for contract members; if it grows further it should be split by aggregate. Assignment is documented here, not in `epics.md`, so a developer reading only the story text will not see the full list.

**Alternative.** Four more debt stories. Rejected: the existing ones are not yet started, so adding parallel ones only fragments ownership.

**Recommendation.** Proceed; when the Product Owner applies §4.5, add one line per affected story pointing at this proposal's §5 so the follow-ups are discoverable from the story.

### A.15 Correcting the 2026-09-09-2 proposal rather than amending its approval (§4.6)

**How it works.** The stale "29" becomes "33" with the provenance of the four added stories, and an execution-log line points to this proposal for the PRD edits.

**Pros.** Factual, minimal, leaves the recorded approval untouched.

**Cons.** Two proposals now describe overlapping edit sets; a reader must follow the pointer.

**Alternative.** Add `prd.md` to its `amends_if_approved`. Rejected in A.1.

**Recommendation.** Proceed as proposed.

### A.16 Leaving the UX spines to the concurrent Update run (§7)

**How it works.** The UX Owner reconciles `DESIGN.md` and `EXPERIENCE.md` after the run in progress finishes; this proposal only records that neither spine carries the new vocabulary.

**Pros.** Avoids two writers on one file today; the UX run has its own memlog and gate.

**Cons.** The UX documents will lag this proposal by at least one more run, and the stories listed in §7 cannot be `ready-for-dev` until that reconciliation lands.

**Alternative.** Propose UX edits here. Rejected while another process is editing the file.

**Recommendation.** Proceed; hand the UX Owner the eight typed names and the Release PM as the checklist for that reconciliation.

### Summary of recommendations

| Solution | Recommendation | Refinement from this analysis |
| --- | --- | --- |
| A.1 PRD resync through this proposal | Proceed | Suggest Product later key §8.1 on the index version only |
| A.2 Spine residuals, index 6 | Proceed | Run two lenses on the diff, not a full round |
| A.3 Membership model | Proceed | Make the no-roster-version fallback explicit in 6.6 tests |
| A.4 Every applicable policy version | Proceed | None |
| A.5 Data handling on the enablement aggregate | Proceed | Per-command authorization in AD-30 |
| A.6 `EXT-PARTIES-1` narrow consumers | Proceed | Decide §7 item 3 at approval |
| A.7 `EXT-SECRETS-1` identity custody | Proceed | Adopt the optional AD-14 provenance clause |
| A.8 Governance records as documents | Proceed | `RQ-1` records cite the record ids considered |
| A.9 Data-handling codes | Proceed | `Unconfigured` reuse is the fallback |
| A.10 `TriggerReviewOverdue` block | Proceed | State fail-closed on unavailable read |
| A.11 New Story 5.11 | Proceed | Keep the "before 5.4" note under any id |
| A.12 Posting record split | Proceed | 7.7's one-source test is the guard; `ARCH-A-14` before 7.4 |
| A.13 Literal `ARCH-A` dates | Proceed | Deferral is not available for these rows; batch the dates |
| A.14 Follow-ups under existing stories | Proceed | One pointer line per story in `epics.md` |
| A.15 Correct the prior proposal | Proceed | None |
| A.16 UX deferred | Proceed | Hand over the vocabulary checklist |
