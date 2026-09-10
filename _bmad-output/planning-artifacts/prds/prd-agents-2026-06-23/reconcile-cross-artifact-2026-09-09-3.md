# Cross-Artifact Reconciliation Extract — 2026-09-09 (run 3)

- **Purpose:** record, from the documents as they are on disk, the facts the PRD must state so that `prd.md` can be re-synced in one pass. Companion to `validation-report.md` (run at 2026-09-09T11:01:49Z), which found the PRD's cross-document claims stale.
- **Extracted at:** 2026-09-09 13:43Z (15:43 local, +0200). All mtimes below are local time (+0200).
- **Rule for the proposed sentences:** cite documents by path and frontmatter `updated` date, describe rules rather than row counts or line numbers, and defer enumerations to the owning document (validation report, finding at its line 539: "a PRD sentence that pins a Spine line count or row count will be wrong within the hour").

## Files read

| File | mtime (local +0200) | Lines | Frontmatter `updated` | Notes |
| --- | --- | --- | --- | --- |
| `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md` | 2026-09-09 10:08:58 | 1000 | 2026-09-09 | the document to re-sync |
| `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/addendum.md` | 2026-09-09 10:04:58 | 52 | — | lines 40-52 carry Spine-defect wording |
| `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md` | 2026-09-09 15:42:23 | 797 | 2026-09-09 | **uncommitted working-tree edits** (`git diff --stat`: 16 insertions, 12 deletions: v7 review sources, AD-7 Prevents, AD-12/AD-13/AD-22/AD-30 rule text, conventions Identity row, .NET SDK row, ARCH-A-4 and ARCH-A-12 rows) |
| `_bmad-output/planning-artifacts/external-dependency-register.md` | 2026-09-09 13:28:06 | 212 | 2026-09-09 | `authority: sprint-change-proposal-2026-09-09-2.md` |
| `_bmad-output/planning-artifacts/launch-readiness-register.md` | 2026-09-09 12:49:11 | 319 | 2026-09-09 | `authority: sprint-change-proposal-2026-09-09-2.md` |
| `_bmad-output/planning-artifacts/epics.md` | 2026-09-09 12:44:27 | 3120 | — | read only the parts named in the task |
| `_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-09-2.md` | 2026-09-09 11:24:15 | 543 | 2026-09-09 | `status: approved` |
| `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/validation-report.md` | 2026-09-09 13:21:50 | — | — | run at 2026-09-09T11:01:49Z, grade Poor |

---

## A. Architecture Spine

Path: `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md`, mtime 2026-09-09 15:42:23 (+0200), uncommitted edits present.

### A.1 Frontmatter date and version

On disk (lines 8-10, 15):

```
status: final
created: 2026-06-23
updated: 2026-09-09
architecture_assumption_index_version: 3
binds:
  - PRD FR-1..FR-34
  - PRD NFR-1..NFR-14
  - PRD OQ-1..OQ-30
```

There is no separate `version` key; the version the Spine exposes is `architecture_assumption_index_version: 3`, rendered as `ARCH-A-INDEX-3` (line 769). The frontmatter `updated` date is unchanged from what the PRD cites, so an `updated`-date citation alone does not distinguish the 10:08 Spine from the 15:42 Spine.

### A.2 The full `ARCH-A` table (lines 773-786)

Governing paragraph (line 771, verbatim): "Spine-originated assumptions are keyed so `RQ-1` can name them as `UnretiredAssumption` blockers alongside the PRD section 8.1 rows the ADs cite inline. An `RQ-1` decision records the exact index version it evaluated and each blocker names both the row and `ARCH-A-INDEX-3`. Adding or retiring a row, or changing an assumption, owner, or retirement condition, increments `architecture_assumption_index_version` and the rendered index identifier in the same change. Every unretired row in the cited version blocks; versioning never narrows the PRD's open-range rule. This reconciliation retires no assumption, preserves milestone descriptions, and records an explicit calendar `TargetRetirementDate`; Architecture-owned open rows remain `TBD` until their co-owners approve a date and therefore block `RQ-1` in the meantime."

| Key | Where | Owner | Status | Retired when | `TargetRetirementDate` (verbatim) |
| --- | --- | --- | --- | --- | --- |
| ARCH-A-1 | AD-27 | Architecture + Platform Maintainer | Open | `EXT-HOST-1` confirms the purge job and window | `TBD — target milestone: `EXT-HOST-1` commitment` |
| ARCH-A-2 | AD-30 | Architecture + Platform Maintainer | Open | Platform identity contract confirms the subject-to-Party mapping | `TBD — target milestone: `EXT-HOST-1` identity contract` |
| ARCH-A-3 | AD-17, register | Architecture + Release PM | Open | Product and the Release PM confirm before enablement | `TBD — target milestone: before `RQ-1` evaluation` |
| ARCH-A-4 | Stack | Architecture | Open | SDK pin bumped past the capped `10.0.3xx` band immediately; test-stack catalog alignment still targets Story 5.6 | `SDK: **immediate**, dated now; test-stack catalog alignment: TBD — target milestone: Story 5.6` |
| ARCH-A-5 | AD-2, AD-10, conventions | Architecture + Epics | **RETIRED 2026-09-09** | Retired by approved `sprint-change-proposal-2026-09-09.md` and the corresponding `epics.md` assignment | `2026-09-09` |
| ARCH-A-6 | AD-21 | Architecture + Product | Open | Product confirms or replaces the settlement default | `TBD — target milestone: before `RQ-1` evaluation` |
| ARCH-A-7 | AD-28 | Architecture + Platform Maintainer | Open | `EXT-TOPOLOGY-1` recovery and expiry evidence confirms or retunes it | `TBD — target milestone: before `RQ-1` evaluation` |
| ARCH-A-8 | Stack | Architecture + FrontComposer Maintainer | Open | v5 reaches GA, or the RC pin is confirmed as an accepted production risk before the story that first ships UI | `TBD — target milestone: v5 GA or first UI story` |
| ARCH-A-9 | AD-2 | Security | Open | Security specifies a numeric limit or a register section | `Not Architecture-owned — no target here` |
| ARCH-A-10 | AD-22 | Governance + Compliance | Open | A Conversations-owned preservation-or-purge seam is committed, or Legal confirms the residual risk is accepted (tracked inside `LR-AUDIT-PROTECTION-DELETION`) | `Not Architecture-owned — no target here` |
| ARCH-A-11 | AD-5, AD-12 | Architecture + Product | Open | Product confirms this rule or specifies a different disabled-Agent retry behavior | `TBD — target milestone: before the Story implementing Agent disable/enable retry handling` |
| ARCH-A-12 | AD-13, AD-22, AD-29 | Architecture + Security | Open | Security confirms the versioned-key rotation policy or a different key-lifecycle procedure supersedes it | `TBD — target milestone: before `RQ-1` evaluation` |

Assumption text for the rows the PRD does not know about:
- ARCH-A-11: "A disabled Agent suspends its proposals' `PostingFailed` retry-window clock, accruing `PausedDuration` on the same terms as the AD-12 kill switch, rather than letting the retry budget keep running or silently exhaust".
- ARCH-A-12 (uncommitted edit, replaced the earlier "never rotated while the tenant holds unerased protected content" wording): "The per-tenant `DigestKey` is versioned rather than singular, every issued version is custodied indefinitely by `EXT-SECRETS-1` while any digest under it survives, and Platform Operator-initiated rotation is unrestricted and content-free".

Facts against the PRD:
- Twelve rows (`ARCH-A-1`..`ARCH-A-12`), one retired (`ARCH-A-5`). PRD line 836 says "at that date the table holds `ARCH-A-1` through `ARCH-A-10`, with `ARCH-A-5` retired".
- **No Architecture-owned row carries a calendar target retirement date.** Every Architecture-owned open row is `TBD — target milestone: ...`; the only calendar date is the retired row's `2026-09-09`, and ARCH-A-4's SDK half says "immediate, dated now" without a date. The Spine's own paragraph says these `TBD` rows "therefore block `RQ-1` in the meantime". PRD FR-28 item 9 (line 688) and §8.1 (line 838) state "every Architecture-owned row carries a target retirement date" as a fact; on disk it is a column with `TBD` values.
- The Spine defines no "Release PM acknowledgement" rule for rows added after `RQ-1` is scheduled; the Spine's rule is index-version based ("An `RQ-1` decision records the exact index version it evaluated" and "Every unretired row in the cited version blocks; versioning never narrows the PRD's open-range rule"). The PRD's acknowledgement rule (FR-28 item 9, §8.1 line 838) is PRD-only; nothing on disk contradicts it, but no other document carries it. "Release PM" appears in the Spine only as the co-owner of `ARCH-A-3`.
- Owners present in the table: Architecture, Platform Maintainer, Release PM, Epics, Product, FrontComposer Maintainer, Security, Governance + Compliance. PRD line 838 names "Product, Governance, Security, or the Release PM" as the parties whose confirmation retires a row; the table also has rows retired by Platform Maintainer confirmations (`EXT-HOST-1`, `EXT-TOPOLOGY-1` evidence), FrontComposer Maintainer, and "Legal".

### A.3 AD-5, AD-12, AD-22 versus the PRD's "three Spine defects"

PRD claim (lines 840-844, verbatim):
> 840: "... A Spine rule that narrows or contradicts an FR-18, FR-24, FR-28, or FR-33 rule is a Spine defect to be corrected before the consuming story is `ready-for-dev`; three are open at this revision, owned by Architecture:"
> 842: "- AD-5 forbids the system abandonment from `Approved` and `PostingFailed` on a detected removal that FR-18 requires, and allows human abandonment from `PostingFailed` only."
> 843: "- AD-22 names the Tenant Agent Administrator as the sole inspection second party and gives export and hold release none, against FR-24 and FR-33."
> 844: "- AD-12 states the Release Operator trigger without the review, minimum sample, and `InsufficientEvidence` rule of FR-28."

On disk, all three are corrected:

**AD-5 (line 149)** now matches FR-18's transition table (PRD lines 421-435):
- "System abandonment carries a typed reason (`NoEligibleApprover`, `SourceConversationUnavailable`, `RemovedInConversations`) and, per AD-7's membership re-check, now also fires from `Approved` and `PostingFailed` on a detected removal or block, not from `PostingFailed` alone: the `Approved`/`PostingFailed` -> `Abandoned` (`RemovedInConversations`) transition runs only after the `MessageId` lookup below finds no posted message."
- "Abandon is legal from every awaiting-decision state and from `PostingFailed`; from `Approved` it is legal only as the system's own `RemovedInConversations` abandonment below, never a human abandon; `PostingPending` accepts no abandon at all and completes or fails on its own terms." (This matches FR-18: human abandon rows exist only from awaiting states and `PostingFailed`.)
- "`PostingPending` is uninterruptible: nothing leaves it except the posting attempt's own conclusion, so no removal, block, suspension, or abandonment can act on a post in flight; each is applied on exit if the attempt did not post."
- "An unavailable lookup refuses the exit and the proposal stays in its pre-check state — `PostingFailed` for a `PostingFailed`-origin exit, `Approved` for the `Approved`-origin system-abandon check — so a removal detected against an `Approved` proposal can never force the illegal `Approved` -> `PostingFailed` transition".
- A-7 remains cited: "`PostingFailed` allows a bounded audited retry of at most 3 attempts within 15 minutes of the first `PostingFailed` command's `EvaluatedAt` [ASSUMPTION A-7] — a deadline computed as origin plus window plus AD-12's `PausedDuration`".

**AD-12 (lines 193-195)** now carries the review, minimum sample and `InsufficientEvidence` rule of FR-28 and the `Approved`-waits rule of OQ-27:
- "A Release Operator trigger review is mandatory when an FR-28 operational rate exceeds its threshold over the rolling seven-day window — blocked-call share above 50 percent, unavailable-call share above 20 percent, Provider-or-generation failure share above 20 percent, or posting-failure rate above 10 percent — using the PRD numerator/denominator exclusions and minimum sample; below that sample the trigger is `InsufficientEvidence` and is reported rather than acted on, and an overdue review is `TriggerReviewOverdue`. The Release Operator pulls only as the recorded decision of that review."
- "An `Approved` proposal stays `Approved` without starting a post or consuming a retry attempt. A `PostingPending` attempt already in flight is uninterruptible and completes or fails."
- "The switch never system-abandons or deletes a proposal or Audit Evidence."
- Second-update paragraph (line 195): "The ordinary minimum sample is 5 distinct calling Parties and 50 Agent Calls; a tenant with fewer than 5 calling Parties uses every Party that called and 20 calls. A smaller sample is `InsufficientEvidence`, not a pass. Exceeding a threshold requires a Release Operator review within one business day; absence is `TriggerReviewOverdue` and blocks qualification."
- New Spine rule with no PRD counterpart (`[ASSUMPTION ARCH-A-11]`): the disabled-Agent suspension pauses the `PostingFailed` retry clock on the same terms as the kill switch, accruing one shared `PausedDuration`. PRD FR-3 (line 176) says an `Approved` proposal waits while `Disabled` and FR-18 says the clock pauses while `Suspended`; the PRD does not say the clock pauses while the Agent is `Disabled`. This is the kind of rule PRD line 840 calls "a PRD amendment pending under the §0 landing rule".

**AD-22 (line 261)** now matches FR-24/OQ-30 and FR-33:
- "The second party is computed, not declared: the subject set is every Party recorded as caller, editor, Approver, decision actor, or Conversation Facilitator on any proposal or Conversation in scope, plus the specific Tenant Agent Administrator principal(s) whose `ConfigurationVersion` was in force for any in-scope call ... when the current TAA is one of those recorded principals ... the second party is instead the Platform Operator or a second Compliance Inspector, either pre-approving before any content read or, for a single-proposal or single-Conversation scope only, reviewing post hoc within 7 days [ASSUMPTION A-19]".
- "Two Compliance Inspectors cannot approve each other's inspections within a rolling 30-day window."
- "`ExportRequest` requires this same second-party approval before any content read, never post hoc; `LegalHoldRelease` requires the audited approval of a second Compliance Inspector or the Platform Operator on the same subject-set and anti-collusion terms."
- "`LegalHold`, `LegalHoldRelease`, and `ExportRequest` are Compliance Inspector commands; a `DeletionRequest` is submitted by the Platform Operator and becomes executable only after a recorded Compliance Inspector approval [ASSUMPTION A-12]."

Consequence: PRD lines 840-844, OQ-26 ("a deliberate divergence from Spine AD-5, recorded in §8.1 as a Spine defect for Architecture to correct"), OQ-27 ("a Spine AD-12 correction owned by Architecture"), A-7 ("the Spine fixes the bound as non-configurable (AD-5), which this row does not contest" — still true, AD-5 fixes 3/15 min and cites A-7), and addendum lines 49-50 ("AD-5 is recorded in §8.1 as a Spine defect to correct", "AD-12 is listed for Architecture to correct") describe corrections that have landed.

### A.4 AD-13 — Eligible Approver resolution in Automatic mode (live divergence)

Spine line 201 (verbatim): "Acceptance follows the FR-8 order (authorization, lifecycle, provider eligibility, rate limits, context measurement, reservation, pre-Provider safety, Eligible Approver resolution, membership) and ends with one `AgentCallAccepted` event; Eligible Approver resolution runs in every response mode, not Confirmation mode only — an Automatic-mode Agent with zero configured Approvers is rejected up front with `NoEligibleApprover` before any Provider call, on the same terms as Confirmation mode whose commit instant starts the NFR-9 clocks and counts the SM-2 numerator."

PRD FR-8 line 284 (verbatim): "  8. Eligible Approver resolution where Confirmation Response Mode applies (FR-7)." PRD FR-7 line 259: "`hexa` is unavailable, with a typed `NoEligibleApprover` rejection, in a Confirmation Response Mode Conversation whose Participants yield no Eligible Approver."

This is an unlisted PRD-vs-Spine divergence (the validation report names it at its line 539). The Spine narrows FR-8 (rejects Automatic-mode calls with no Approver); the PRD scopes the step to Confirmation mode. Product must either amend FR-8/FR-7 or record AD-13 as a Spine defect; the PRD's §8.1 defect list should not list it as settled either way without a Product decision.

### A.5 AD-17 — owner set for `UnretiredAssumption`

Spine line 229 (verbatim excerpt): "`RQ-1` evaluates one consistent checkpoint across all keys, reads the `ReleaseQualificationGateSet`, admits only pre-enablement evidence a qualification cohort can produce, and records an `UnretiredAssumption` blocker for every unretired Product, Architecture, or Governance assumption in PRD section 8.1 or in this spine's Architecture Assumptions index."

Second-update paragraph (line 231): "authoritative producers emit `UnretiredAssumption`, `OpenDecision`, `DependencyNotAvailable`, `GateOutOfScope`, `TriggerReviewOverdue`, and `InsufficientEvidence`; `RQ-1` additionally names `ProhibitedCostControlPosture` and `PayloadProtectionUnavailable` when those conditions stand. Any one blocks or fails closed exactly as specified by `launch-readiness-register.md`."

Note: AD-17's sentence names "Product, Architecture, or Governance" while the table has rows owned by Security (`ARCH-A-9`), Platform Maintainer, FrontComposer Maintainer and Governance + Compliance; the table paragraph (line 771) says "Every unretired row in the cited version blocks", and the launch register (line 75) says "whatever its owner". The PRD's "whatever its owner" (FR-28 item 9, §8.1 line 836, OQ-24) is consistent with the table paragraph and the register, and broader than AD-17's sentence — a minor Spine wording gap, not a PRD change.

### A.6 AD-2 — `MirrorPending` / `ReadmitPending` / `BlockVersion`

Spine line 129 (excerpt): "`ConversationAgentState` (`TenantId`, `ConversationId`; membership-established fact, the Agents-owned block with its `BlockVersion` — incremented on every set or clear so a stale clear can never silently no-op against a newer block — a `MirrorPending` flag set whenever a block, clear, or removal record is written and cleared only when the Conversations-side mirror confirms it, and the index of non-terminal proposals for that Conversation; its membership state machine is exactly `NeverJoined -> Joined -> (ExternallyRemoved | Blocked) -> ReadmitPending -> Joined`, where `ExternallyRemoved` and `Blocked` are rejecting states on the same terms per AD-7, `ReadmitPending` is entered on clear and holds until the Conversations mirror confirms re-admission, and a clear received while already `MirrorPending` re-clears idempotently rather than double-mirroring)".

Second-update paragraph (line 131, verbatim): "`ConversationAgentState` carries exactly one of `NeverJoined`, `Joined`, `ExternallyRemoved`, `Blocked`, and `ReadmitPending`, a monotonic `BlockVersion` incremented on every block set, `MirrorPending` while an at-least-once participant-removal outbox entry awaits confirmation, and the non-terminal proposal index. This is the normative state contract where the earlier AD-2 shorthand differs. Clearing a block at the current authorized version cancels every pending removal entry with a lower `BlockVersion`, records `ReadmitPending`, and never rejoins; only the next accepted membership step moves it to `Joined`."

Matches PRD OQ-16 / OQ-25 (lines 986, 995). Internal Spine nuance: the first sentence says `BlockVersion` increments "on every set or clear", the normative paragraph says "on every block set"; the PRD (OQ-25) says only "versioned by `BlockVersion`" and is not contradicted.

### A.7 AD-7 — `ExternallyRemoved` wording

Spine line 161 (excerpt): "if `ConversationAgentState` records membership as established there and `hexa` is now absent, record the external removal on that aggregate at its expected revision, which sets the block, abandon that Conversation's non-terminal proposals, and reject with `RemovedInConversations`"; "A system-set `ExternallyRemoved` block, having no setting Party, is clearable by the current Tenant Agent Administrator or any current Facilitator."; "`MembershipUnavailable` and `MembershipRejected` remain `PostingFailed` reasons, produced only by this re-validation for a pre-post failure that is not a removal or block, never by acceptance."

Second-update paragraph (line 163, verbatim): "The Parties seam verifies that the linked Party remains AI-type. `NeverJoined` or `ReadmitPending` adds the participant; `Joined` plus present succeeds without writing; `Joined` plus absent records `ExternallyRemoved` only when the last removal mirror is confirmed and no newer clear exists, while absence during an unconfirmed mirror or after a newer clear is a late effect and causes no state change; `Blocked` rejects; `ExternallyRemoved` plus absent rejects and plus present records `Joined`; an unavailable read returns `MembershipUnavailable` without changing or abandoning state. A block set from any state increments `BlockVersion`, writes an at-least-once removal outbox entry, and sets `MirrorPending`; an already-absent participant confirms the entry, while `NeverJoined` causes no Conversations call. An authorized clear must name the current version; the Facilitator cannot clear an administrative block, and `ExternallyRemoved` may be cleared only by the Tenant Agent Administrator or current Facilitator. Clear is refused when the matching removal was sent but is not acknowledged, cancels lower-version pending entries, records `ReadmitPending`, and does not rejoin."

Matches PRD FR-2 (lines 154, 160) and OQ-16/OQ-25. The uncommitted AD-7 edit adds "or a Facilitator clearing a block the Tenant Agent Administrator set" to Prevents.

### A.8 AD-14 — attestation summary

Spine line 209 (verbatim): "`EXT-HOST-1` binds the `EXT-PROTECTION-1` engine at startup and on every readiness evaluation, records a canary protect/unprotect result plus engine identity and version, and refuses content-bearing activation or workflow progress on missing, stale, incompatible, no-op, or pass-through-wrapper evidence. This is the FR-34 `PayloadProtectionUnavailable` readiness and runtime gate; its integration fixture proves plaintext absence in persisted event, broker, projection, and `workflow-execution-state` payloads."

PRD FR-34 (line 727) names five steps: seal a canary, verify no plaintext, unseal, destroy its DEK, replay as `Erased`. AD-14's summary says "canary protect/unprotect"; the erase/`Erased` replay step is carried by the register's `EXT-HOST-1` artifact ("seal a canary, prove persisted bytes contain no plaintext, unseal it, destroy its DEK, replay `Erased`, and obtain engine identity/version") and by the launch register (line 78: "the FR-34 seal/unseal/erase identity-and-version attestation"). Not a PRD change; a Spine wording gap worth noting to Architecture.

### A.9 Divergences / Open section

The Spine has **no** section listing PRD-vs-Spine differences. Headings are: Design Paradigm, Invariants & Rules (AD-1..AD-31), Consistency Conventions, Stack, Structural Seed, Capability To Architecture Map, External V1 Prerequisites, Architecture Assumptions, Deferred Beyond V1. The words "divergence"/"defect" occur only in review-file names in `sources:` (`reviews/review-2026-09-09-adversarial-divergence-v4/v6/v7.md`). The External V1 Prerequisites section (line 751) says: "The external dependency register ... is authoritative for commitment status and consuming stories, which are read from it at evaluation time; the spine does not track them."

---

## B. External dependency register

Path: `_bmad-output/planning-artifacts/external-dependency-register.md`, mtime 2026-09-09 13:28:06, `updated: 2026-09-09`.

### B.1 Status of every `EXT-*` entry

| Entry | `AcceptedStatus` (line) | Owner | Target / date / command |
| --- | --- | --- | --- |
| `EXT-CONV-AI-1` | `Uncommitted` (62) | Conversations Maintainer | all `TBD` |
| `EXT-CONV-UI-1` | `Uncommitted` (78) | `TBD` | all `TBD` |
| `EXT-HOST-1` | `Uncommitted` (94) | Platform Maintainer | all `TBD` |
| `EXT-PROVIDER-1` | `Uncommitted` (110) | Agents Runtime Maintainer | repository `TBD`, all `TBD` |
| `EXT-SAFETY-1` | `Uncommitted` (124) | — | — |
| `EXT-TOKEN-1` | `Uncommitted` (138) | — | — |
| `EXT-SECRETS-1` | `Uncommitted` (152) | — | — |
| `EXT-TOPOLOGY-1` | `Uncommitted` (166) | — | — |
| `EXT-PROTECTION-1` | `Uncommitted` (180) | EventStore Maintainer | all `TBD` |

Count: **9 `Uncommitted`, 0 `Committed`, 0 `Available`.**

Current Blocking Summary (line 212, verbatim): "All nine dependency records are `Uncommitted`; every consumer remains blocked from `ready-for-dev` until the fields required for `Committed` are accepted. `EXT-HOST-1`'s earlier target/date/command are historical only because the required artifact now includes the production `EXT-PROTECTION-1` binding and FR-34 attestation port. `RQ-1` additionally requires every dependency in its qualification profile to be `Available` with the exact compatibility command passing against the target used by the run."

`EXT-HOST-1` note (line 97, verbatim): "*Historical prior commitment only: target `a66cdf346e521ad147f442b686f301f0f59c525c`, integration date `2026-09-30`, and `./eng/verify-agents-host.sh` covered the earlier clean-checkout host artifact. The expanded protection binding and attestation artifact has not been re-accepted by the Platform Maintainer, so those values are not current commitment fields.*" The SCP's artifact table (line 91) records the disposition: "Direct amendment; `EXT-HOST-1` becomes `Uncommitted`".

PRD claims contradicted: line 16 ("all critical external dependencies except `EXT-HOST-1` are `Uncommitted` — eight of the nine register entries (§8)") and line 814 ("At the time of this revision, only `EXT-HOST-1` is `Committed`; the remaining entries are `Uncommitted`").

`Available` definition (line 41, verbatim): "The committed target is installed/consumable and its executable compatibility command passes with live Level 4 component evidence. Where `RequiredEvidenceLevel` also names Level 5, cross-system attainment is completed by the consuming launch-readiness gate and `RQ-1`; it is not a prerequisite for beginning that same qualification run." Line 45: "The register owner's compatibility command against the exact committed target is the sole permitted execution of a `Committed` seam, and only to establish `Available`". PRD line 812's definition ("installed and consumable and its executable compatibility command passes with live Level 4 evidence") is consistent; the register adds the Level-5 clarification.

### B.2 `EXT-CONV-AI-1` seams 1-6 (line 56, `RequiredArtifact`)

- Seam 1 (membership): "public `IConversationClient.AddParticipantAsync` and `POST /api/v1/conversations/{conversationId}/participants`, limited for Agents to stable AI Party identity, `ParticipantType.AiAgent`, and `ParticipantRole.Member`; exact retries are idempotent no-ops, conflicting type/role is a typed conflict, and cross-tenant or general participant management is denied; plus a participant-state read for the AI participant and a participant removal limited to the AI participant for the PRD FR-2 removal block. Before accepting AI membership, Conversations verifies through Hexalith.Parties that the participant Party is AI type; the record owner supplies the final member and typed failure names (A-21)." **No typed "already absent" no-op answer for the removal** is stated in seam 1 (the AD-7 rule "an already-absent participant confirms the entry" is Spine-side). PRD line 817 states it as a seam-1 requirement.
- Seam 2 (posting): "append a Conversation Message as the `AiAgent` participant with an Agents-supplied deterministic `MessageId` and idempotency key, persisted verbatim or rejected (a differing returned id is a typed incompatibility that blocks posting), and message metadata carrying the Agent Call trace reference and provenance flags (AI-generated; human-edited and by which Party), so restart or replay cannot duplicate a post; plus an existence read by `MessageId` returning the message, typed absence, typed `ConversationDeleted`, typed `PrincipalRemovedFromConversation`, or unavailable, so Agents can check before retry or abandonment after a lost acknowledgement and record `LateConfirmed` rather than contradict an already-posted message." **Yes: the `MessageId` existence read with `ConversationDeleted`/`PrincipalRemovedFromConversation` is in seam 2.** PRD line 818 ("The existence read is not yet in the register's seam-2 text; adding it is a pending register extension (correct-course, 2026-09-09)") is stale.
- Seam 3: "`ParticipantRole.Facilitator` on the participant read model."
- Seam 4: "The Eligible Conversation denominator is supplied either as an active-Conversation count per tenant/window or as a Conversations-side tenant event feed from which Agents computes the same count, never from Agent Calls (A-4)." **Yes: the event feed is in seam 4.** PRD line 820 ("The event-feed alternative is a pending register extension (correct-course, 2026-09-09)") is stale.
- Seam 5: tenant-scoped reads under the Agents Service Principal "of complete Conversation content, the Participant roster with roles, and Conversation existence/accessibility; they return typed `ConversationDeleted` and `PrincipalRemovedFromConversation` distinctly from transient denial or error, and content contains the messages a Participant would currently see, including current edit/delete state (A-15)."
- Seam 6: "The Conversation deletion signal remains unchanged so an approved deletion in Conversations triggers PRD FR-30 deletion of derived Agent content."
- Compatibility contract (line 59) covers all six including "a typed `MessageId` existence read covering message, absence, deletion, principal removal, and unavailable" and "active-Conversation count or equivalent Conversations-side tenant event feed"; Command: `TBD`.
- Extension note (line 65): "*Scope extended 2026-09-09 by the PRD validation reconciliation and follow-through proposal: the posting existence read, Facilitator resolution, active-count/event-feed denominator, and tenant-scoped content/roster/existence/accessibility consumers are explicit. The record was already `Uncommitted`; the extension changes no status ...*"
- Consuming stories (line 63): "5.4, 6.1, 6.2, 6.6, 6.8, 7.1–7.5, 7.7, 8.5, 8.8; `RQ-1`".

### B.3 Non-Conformance Records (lines 185-208)

Section title: "Known Consumer Non-Conformance". Subsection "Story 5.3 / EXT-PROVIDER-1 Historical Consumption — Open Product Approval" (line 189, verbatim): "OQ-17 settles the rule for consumption of an `Uncommitted` seam but does not establish whether Story 5.3 actually executed `EXT-PROVIDER-1`. The inspected Story 5.3 specification and fail-closed deferred provider show that no live adapter shipped, but they are insufficient evidence for a dated Product ruling. Until Product selects one evidence-backed branch, neither disposition is recorded:" Branch A (line 191): "seam was consumed: `Story 5.3 | EXT-PROVIDER-1 | <verified completion date> | completed while Uncommitted; reopened by sprint-change-proposal-2026-09-09; reopening does not clear this record (PRD FR-21)`." Branch B (line 192): "seam was never consumed: `None: Story 5.3 executed no EXT-PROVIDER-1 seam (Product ruling <date>)`." Line 194: "This open approval condition is blocking provenance, not a fabricated non-conformance finding or dependency commitment."

`EXT-PROVIDER-1` consuming stories (line 111): "5.5, 6.4, 7.3; `RQ-1` (narrowed 2026-09-09 by the architecture update: Story 5.3 published catalog truth adapter-free and is not a consumer)".

Second record `NC-5.3-PLATFORM-CATALOG-SCOPE` (lines 196-208): `Status` Open; `DependencyEffect`: "This record does not make `EXT-PROVIDER-1` a Story 5.3 consumer, does not alter any dependency status, and cannot be cited as current architecture or release conformance."

PRD line 814 ("any story already completed against an `Uncommitted` entry carries a non-conformance record in the register") overstates: the register carries no completed-consumption record; it carries an open Product approval with two unselected branches, plus a scope-migration non-conformance that explicitly is not a dependency consumption record.

### B.4 `EXT-CONV-UI-1` RequiredArtifact kinds (line 72)

"Three artifact kinds. (1) A versioned Conversation action contribution and registration contract allowing Agents to contribute the Conversation-owned **Call hexa** action into a Conversation surface, with tenant-scoped authorization and typed failure when the action cannot be registered; Conversations owns the trigger and Agents owns the self-contained `ConversationAgentCallPanel` dialog body. (2) A per-message decoration slot keyed by `MessageId` through which Agents renders the AI-generated and human-edited provenance markers carried in message metadata (PRD FR-11, FR-17) wherever a message's provenance is disclosed, backed by an Agents-side provenance accessor. (3) `GetCallabilityAsync(tenant, conversation)` so the trigger can reflect Agents callability before the dialog opens. Conversations never references Agents packages (architecture AD-31)."

**No Conversation status entry** is among the three kinds. Owner `TBD`; consuming story 6.7. PRD line 826 names two things (action contribution contract; provenance-marker rendering) and omits the callability gateway; not contradicted, but incomplete relative to the register.

### B.5 Provider Readiness Contract and `DataHandlingVersion`

`grep -i 'DataHandlingVersion|data-handling|tenant accept'` over both registers returns **nothing**. The launch register's Provider Readiness Contract (lines 207-211) lists `ProviderReadinessResult` members (`OperationalState`, `Callability`, `ReasonCode`, `CapabilityVersion`, `ObservedAt`, `ValidUntil`, `Freshness`) and the reason codes (`Unknown`, `None`, `NonBlockingOperationalWarning`, `DependencyUnavailable`, `EntryMissing`, `Stale`, `Disabled`, `Unconfigured`, `Unpriced`, `InvalidLimits`, `SecretUnavailable`, `CapabilityVersionRegressed`, `AdapterUnavailable`, `ProviderHealthFailed`, `Indeterminate`, `PlatformNotReady`); its hard gates are "freshness, platform enabled state, tenant enablement, configured/text-generation state, current pricing, secret resolution, Provider health, non-regressed capability version, and valid positive limits". No data-handling record, `DataHandlingVersion`, tenant acceptance, or 30-day grace appears. The `EXT-PROVIDER-1` record likewise does not mention them. The PRD's FR-4/OQ-29 rule (a model without a data-handling record cannot be enabled; Tenant Agent Administrator accepts the version; 30-day grace) has no counterpart in either register; only `epics.md` carries `DataHandlingVersion` (see D).

---

## C. Launch-readiness register

Path: `_bmad-output/planning-artifacts/launch-readiness-register.md`, mtime 2026-09-09 12:49:11, `updated: 2026-09-09`, `release_gate: RQ-1`.

### C.1 `RQ-1` definition and inputs

Line 17 (verbatim): "`RQ-1` returns READY only when every required record for the evaluated `TenantScope` and `EnvironmentProfile` exists, is fresh, is `Pass`, meets its `RequiredEvidenceLevel`, references qualifying evidence, and uses the current configuration or measurement contract. A missing, stale, blocked, or insufficient record makes the result NOT READY. `RQ-1` is a release gate, not a development story."

Line 19 (verbatim): "`RQ-1` additionally records NOT READY when any `UnretiredAssumption`, `OpenDecision`, `DependencyNotAvailable`, `ProhibitedCostControlPosture`, or `PayloadProtectionUnavailable` blocker stands. These blockers implement the single PRD FR-28 input list; they do not add an independent gate or metric."

Line 174 (excerpt): "The `ReleaseQualificationGateSet` contains all 18 minimum GateIds. Only current `Pass` records for the complete set plus `Available` consumed external dependencies allow `RQ-1` READY and production enablement. Its metric slice is exactly pre-enablement SM-1, SM-4, SM-5, and SM-6 through `LR-PRODUCT-METRICS`, NFR-9 through `LR-RUNTIME-PERFORMANCE`, and NFR-14 through `LR-UI-PERFORMANCE`; SM-5 supplies the 100% audit-completeness threshold. SM-2, SM-3, and SM-7 are never `RQ-1` inputs."

Current Release Decision (line 319): "`RQ-1`: **NOT READY**. Every minimum gate is `InsufficientEvidence`, critical external dependencies are `Uncommitted`, and no production-like tenant scope, source versions, authoritative timestamps, validity periods, or qualifying Levels 4/5 evidence references have been accepted."

Consistent with PRD FR-28 / OQ-22 / OQ-24.

### C.2 Blocker vocabulary (lines 42-54) — all codes

`GateRecordMissing`, `EvidenceNotRecorded`, `DependencyUncommitted`, `DependencyNotAvailable`, `InsufficientEvidence`, `UnretiredAssumption`, `OpenDecision`, `GateOutOfScope`, `TriggerReviewOverdue`, `PayloadProtectionUnavailable`, `ProhibitedCostControlPosture`.

`PayloadProtectionUnavailable` row (line 53, verbatim): "| `PayloadProtectionUnavailable` | The host has not reported the production payload-protection engine required by FR-34 as available; this is also an additive `AgentLaunchReadinessBlocker`. |" — **phrased in host-report terms** ("the host has not reported ... as available"). The emitter contract (line 78) is phrased in attestation terms: "Emit when the host cannot produce the FR-34 seal/unseal/erase identity-and-version attestation against the committed engine." PRD FR-34 (line 727) rejects "a readiness policy ... that clears it on a host-asserted signal alone"; addendum line 50 records the rejection of "A host-asserted availability signal for FR-34". The vocabulary row's wording is a register defect against FR-34, not a PRD change; the emitter row is aligned.

`UnretiredAssumption` row (line 49): "An unretired PRD `A-n` row or Architecture `ARCH-A-n` row blocks qualification; the blocker names the row, table, and evaluated index version." Emitter (line 75): "Emit one code per unretired PRD §8.1 `A-n` or Spine `ARCH-A-n` row; name the row and table, whatever its owner."

`GateOutOfScope` (line 51) and `TriggerReviewOverdue` (line 52) are **present**; `TriggerReviewOverdue` is emitted by "`LR-PRODUCT-METRICS` post-enablement trigger-review evaluation" and "never treat[ed] ... as an `RQ-1` metric input" (line 80).

### C.3 `LR-PARTY-IDENTITY`

Line 100 (verbatim): "| `LR-PARTY-IDENTITY` | Unique active Agent Party identity plus current posting eligibility. Invalidated by Party state, identity contract, or Agent identity-link changes. |" — "Agent identity-link changes" survives although PRD FR-2/OQ-25/OQ-28 and Spine AD-7 make the identity link immutable after provisioning (link/replace commands are deprecate-and-reject). A register wording lag, not a PRD change.

### C.4 `LR-PRODUCT-METRICS` / `product-metrics` split

Line 114 (verbatim): "| `LR-PRODUCT-METRICS` | Versioned calculation of the pre-enablement gate metrics SM-1, SM-4, SM-5, and SM-6 and their attainment on the qualification cohort (gate inputs), plus versioned calculation and reporting of the launch-health metrics SM-2, SM-3, SM-7 and counter-metrics SM-C1..SM-C5, which are never `RQ-1` inputs and are reviewed at 30 and 60 days after enablement and monthly thereafter; deterministic fixtures prove formulas only. Invalidated by source event, formula, threshold, cohort, late-data, or insufficiency rules. |"

Line 288 (projection inventory): "| `product-metrics` | SM-1/SM-4/SM-5/SM-6 gate calculations, SM-2/SM-3/SM-7 launch-health rolling-window/cohort calculations, SM-C1..SM-C5, and insufficiency state. |". Line 188: "`product-metrics` is the only trigger source." Launch Health Reviews (line 178): "Launch health is recorded separately from `RQ-1`. It consumes real post-enablement SM-2, SM-3, and SM-7 results at 30 days, 60 days, and monthly thereafter." SM-7 band "10% through 60%, inclusive" (line 184). Consistent with PRD §12 / OQ-22 / A-13.

### C.5 "Release PM" presence

One occurrence (line 139, verbatim): "The `Platform` assignment of `LR-UI-CONFORMANCE`, `LR-RUNTIME-PERFORMANCE`, and `LR-UI-PERFORMANCE` is an architecture assumption (spine AD-17, 2026-09-09) confirmed or retuned by Product and the Release PM before enablement." No "Release PM acknowledgement" rule for late-added `ARCH-A` rows exists in the register; the register's `AuthorizedProducer` roles are `User` holding Release Operator, or `Platform` (e.g. lines 123, 137). "Release PM" is therefore not a mapped FR-33 role anywhere downstream; the PRD's FR-28 item 9 acknowledgement rule has no register or Spine implementation.

---

## D. Epics

Path: `_bmad-output/planning-artifacts/epics.md`, mtime 2026-09-09 12:44:27.

### D.1 Story-count statement

Line 28 (excerpt): "The second proposal's superseded 29-story count is historical; the later approved additions 5.9, 5.10, 6.8, and 7.7 remain active, for exactly 33 stories." Line 32 (verbatim): "The former 18-story Epic 5 is superseded and non-executable. Active forward work consists only of replacement Epics 5–8 and exactly 33 stories. `RQ-1` remains a non-estimated release gate outside the story backlog; it aggregates completed evidence and never owns missing implementation." Line 355: "These criteria are normative additions to the named story sections and preserve the later approved stories 5.9, 5.10, 6.8, and 7.7. They add no story and change no active-story order."

The PRD does not state a story count (no change needed there); the SCP does (see E) and disagrees.

### D.2 Term occurrences (grep counts, line numbers)

| Term | Count | Lines |
| --- | --- | --- |
| `Draft` (lifecycle state, case-sensitive) | **0** | — (only "draft" lower-case in Story 3.3 prose, line 2316) |
| `MembershipUnavailable` | **0** | — |
| `MembershipRejected` | **0** | — |
| `DataHandlingVersion` | 4 | 351 (OQ-29 coverage row "5.3 `DataHandlingVersion` governance"), 361 (Story 5.3 debt row "Own `DataHandlingVersion`"), 1363 (Story 5.3 AC "the catalog owns and advances `DataHandlingVersion`"), 1396 (OwnedClauses) |
| "cannot be enabled" enablement block for a model without a data-handling record | not found in the `DataHandlingVersion` contexts (1358-1366 describe ownership/advancement only) | — |
| `30-day` | 3 | 377 (Inspector anti-collusion window), 2918, 2936 (unrelated) — **no 30-day acceptance grace**; `grep -i grace` = 0 |
| `Suspended` | 1 | 2620 (plus `ExpiredWhileSuspended` at 372, 2561, 2579, 2623) |
| `AiAgent` | 1 | 144: "limited Conversations membership as `ParticipantType.AiAgent`/`AIAgent` and `ParticipantRole.Member` through `EXT-CONV-AI-1`" — both spellings side by side |
| `ReadmitPending`/`MirrorPending`/`BlockVersion` | 40, 368 | present |
| `LateConfirmed` | 72, 308, 370, 371, 2461, 2497, 2617, 2636, 2639 | present |

### D.3 Requirements-inventory rows still summarising pre-2026-09-09 text (lines 36-105)

- Line 42, **FR3** (verbatim): "FR3: Agent Administrators can activate, disable, and inspect `hexa` lifecycle state; disabled Agents cannot be called, disabling preserves prior evidence and messages, and lifecycle changes are auditable and visible through admin UI and API/client contracts." — no `Draft`/`Active`/`Disabled` states, no `Suspended` status, no `Approved`-waits rule (PRD FR-3 lines 171-176).
- Line 44, **FR4** (verbatim): "FR4: Authorized administrators can configure the Global Providers Aggregate with provider records, model options, enabled/disabled state, and provider capability metadata; disabled providers/models cannot be selected for new active use, existing Agents using disabled providers/models cannot be activated or called until reconfigured, and provider changes are auditable without secret exposure." — no data-handling record, `DataHandlingVersion`, enablement block, tenant acceptance, or 30-day grace (PRD FR-4 line 199 region).
- Line 46, **FR5** (verbatim): "FR5: Agent Administrators can select a Provider and model for `hexa` from the Global Providers Aggregate; selected provider/model state is validated before activation, enough provider/model identity is retained for audit, and selection changes affect only future Agent Calls." — no tenant-enablement precondition or `DataHandlingVersion` acceptance.
- Line 60, **FR12** (verbatim): "FR12: The system prevents automatic posting when authorization, Agent lifecycle, Provider/model, Party identity, Source Conversation access, Conversation Context Policy, Content Safety Policy, or generation status is invalid; no Conversation Message is created on failed checks or safety failure, and authorized status/audit distinguish failure classes without leaks." — omits "the tenant kill switch, the Agents-owned block (FR-2)" that PRD FR-12 (line 334) adds. (Coverage-map line 296 does list budget/capacity/identity/posting gates.)
- Line 80, **FR22** (verbatim): "FR22: The admin UI allows authorized administrators to manage Global Providers Aggregate entries, configure `hexa`, inspect lifecycle state, configure response and approver policy, and view Agent operation/proposal status; UI actions enforce the same authorization rules as API/client contracts, never expose Provider secrets, distinguish active, disabled, invalid, pending proposal, failed call, and expired proposal states, and satisfy NFR-13 accessibility/localization/responsive safety plus NFR-14 interaction-performance evidence." — "authorized administrators" instead of the PRD's Platform Operator (catalog and tenant enablement) / Tenant Agent Administrator split within the FR-33 matrix (PRD FR-22 line 536).
- Line 86, **FR25** (verbatim): "FR25: The system exposes status for Agent readiness, Provider/model readiness, recent Agent Call outcomes, proposal queues, generation failures, approval completion, and posting outcomes; authorized administrators can identify whether `hexa` is callable, distinguish key failure classes, and monitor launch adoption and approval workflow metrics." — no `Suspended` status, no `LateConfirmed`, no blocker surface. (Coverage-map line 322 adds "blocker status".)
- Line 72, **FR18**, and line 40, **FR2**, are already updated (ten states, `PostingPending` uninterruptible, `LateConfirmed`, five membership states, `MirrorPending`).

---

## E. Sprint change proposal 2026-09-09-2

Path: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-09-2.md`, mtime 2026-09-09 11:24:15, `status: approved`, `approved_on: 2026-09-09`, `execution_status: approved-awaiting-implementation`.

`amends_if_approved` (lines 30-35, verbatim):
```
amends_if_approved:
  - launch-readiness-register.md
  - external-dependency-register.md
  - architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
  - epics.md
  - prds/prd-agents-2026-06-23/update-report-2026-09-09.md
```
**`prd.md` is not listed.** `governing_authority` (line 28) is `prds/prd-agents-2026-06-23/prd.md`; `preserves` (line 37) says "prd.md without amendment"; artifact table line 89: "| `prd.md` | None for this correction; it is the governing authority | Preserve unchanged |"; line 434: "Do not amend `prd.md`."; line 478: "`prd.md` has no diff."

Story structure preserved (line 39): "active Epics 5 through 8 and their 29-story structure"; line 79: "The active backlog remains 29 stories and `RQ-1` remains outside it."; line 95: "Story IDs and current statuses already match the 29-story active set"; line 476: "retaining 29 active stories". Epic viability rows (lines 74-77) count 8 + 7 + 6 + 8 = 29. `epics.md` (lines 28, 32) says the 29 count is "historical" and the active set is "exactly 33 stories" (5.9, 5.10, 6.8, 7.7 added). The two documents disagree; the PRD does not state a count and should not adopt either number.

---

## PRD sentences to change

Each entry: the current PRD sentence (verbatim, line), then the proposed replacement. Replacements cite documents by path and `updated` date and describe rules, never counts or line numbers.

### 1. §0, line 16 (status line)

Current:
> Status as of 2026-09-09: all critical external dependencies except `EXT-HOST-1` are `Uncommitted` — eight of the nine register entries (§8) — and none is `Available`, so live content-bearing execution stays disabled under FR-34. Every unretired assumption — the `A-n` rows in §8.1 and the `ARCH-A-n` rows in the Architecture Spine's own table — blocks `RQ-1` (FR-28). No consuming story is `ready-for-dev`.

Proposed:
> Status as of 2026-09-09: no critical external dependency in the external dependency register (`_bmad-output/planning-artifacts/external-dependency-register.md`, `updated: 2026-09-09`) is `Committed` or `Available`; the register's Current Blocking Summary is authoritative for each entry's status and this PRD does not restate it. Because none is `Available`, live content-bearing execution stays disabled under FR-34. Every unretired assumption — the `A-n` rows in §8.1 and the `ARCH-A-n` rows in the Architecture Spine's own table — blocks `RQ-1` (FR-28). No consuming story is `ready-for-dev`.

### 2. §8, line 814 (register status and non-conformance)

Current:
> At the time of this revision, only `EXT-HOST-1` is `Committed`; the remaining entries are `Uncommitted`. Every consuming story therefore remains blocked from `ready-for-dev` under FR-21, and any story already completed against an `Uncommitted` entry carries a non-conformance record in the register.

Proposed:
> The register is the sole authority for each entry's `AcceptedStatus`; at this revision its Current Blocking Summary reports no entry `Committed` or `Available`, so every consuming story remains blocked from `ready-for-dev` under FR-21. The register's Known Consumer Non-Conformance section is the record of any consumption of an `Uncommitted` entry (OQ-17); where the register carries an open Product approval instead of a dated record — as it does for Story 5.3 and `EXT-PROVIDER-1` — that open approval is itself a `ready-for-dev` blocker for the consuming stories until Product selects an evidence-backed branch.

### 3. §8, line 817, seam 1 ("already absent" no-op)

Current (excerpt):
> ... together with a participant-state read for `hexa` and a participant removal for the FR-2 block, the removal answering typed "already absent" as a confirmed no-op [ASSUMPTION A-1: the register owns the final names; `AiAgent` is the spelling this PRD uses].

Proposed:
> ... together with a participant-state read for `hexa` and a participant removal limited to the AI participant for the FR-2 block; the register's seam text governs the removal's typed answers, and Agents treats a typed "already absent" answer as confirmation of the pending mirror entry (OQ-25) [ASSUMPTION A-1: the register owns the final member and typed failure names; `AiAgent` is the spelling this PRD uses].

### 4. §8, line 818, seam 2 (existence read)

Current (final sentence):
> The existence read is not yet in the register's seam-2 text; adding it is a pending register extension (correct-course, 2026-09-09), and `EXT-CONV-AI-1` cannot reach `Available` on a compatibility command that does not exercise it.

Proposed:
> The register's seam-2 text governs the existence read's exact answers (message, typed absence, typed `ConversationDeleted`, typed `PrincipalRemovedFromConversation`, or unavailable); `EXT-CONV-AI-1` cannot reach `Available` on a compatibility command that does not exercise it.

### 5. §8, line 820, seam 4 (event feed)

Current (final sentence):
> The event-feed alternative is a pending register extension (correct-course, 2026-09-09).

Proposed:
> The register's seam-4 text governs which of the two forms Conversations commits; either satisfies A-4.

### 6. §8, line 826, `EXT-CONV-UI-1`

Current (excerpt):
> ... tracked as `EXT-CONV-UI-1`: a versioned Conversation action contribution and registration contract that lets Agents contribute the invocation action into a Conversation-owned surface, plus rendering of the AI-generated and human-edited markers wherever the message's provenance is disclosed.

Proposed:
> ... tracked as `EXT-CONV-UI-1`: a versioned Conversation action contribution and registration contract that lets Agents contribute the invocation action into a Conversation-owned surface, rendering of the AI-generated and human-edited markers wherever the message's provenance is disclosed, and a callability read the trigger consults before the dialog opens; the register's `RequiredArtifact` text governs the exact artifact kinds.

### 7. §8.1, line 836 (Spine citation with row range)

Current:
> Architecture assumptions are indexed and governed in the Architecture Assumptions table of the Architecture Spine at `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md` (frontmatter `updated: 2026-09-09`; at that date the table holds `ARCH-A-1` through `ARCH-A-10`, with `ARCH-A-5` retired) rather than duplicated here. The range is open: every unretired `ARCH-A-n` row in that table at the moment `RQ-1` is evaluated blocks `RQ-1` on the same terms as an `A-n` row, whatever its owner (FR-28), and the Spine's index is authoritative for each row's owner and retirement condition.

Proposed:
> Architecture assumptions are indexed and governed in the Architecture Assumptions table of the Architecture Spine at `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md` (frontmatter `updated: 2026-09-09`, versioned by its `architecture_assumption_index_version`) rather than duplicated here; this PRD states no row count, because the Spine increments its index version whenever a row is added, retired, or changed. The range is open: every unretired `ARCH-A-n` row in the index version current at the moment `RQ-1` is evaluated blocks `RQ-1` on the same terms as an `A-n` row, whatever its owner (FR-28), the `UnretiredAssumption` blocker names the row, its table, and the evaluated index version, and the Spine's index is authoritative for each row's owner, retirement condition, and target retirement date.

### 8. §8.1, line 838 (retirement by party; late rows; target dates)

Current:
> An `ARCH-A` row whose retirement condition names Product, Governance, Security, or the Release PM is retired only by a recorded confirmation from that party, never by an Architecture edit alone; a row added after the `RQ-1` evaluation is scheduled blocks only once the Release PM records an acknowledgement, and every Architecture-owned row carries a target retirement date.

Proposed:
> An `ARCH-A` row whose retirement condition names a party other than Architecture — Product, Governance, Security, the Release PM, or an external maintainer — is retired only by a recorded confirmation from that party, never by an Architecture edit alone. A row added after the `RQ-1` evaluation is scheduled blocks only once the Release PM records an acknowledgement of it. Every Architecture-owned row carries a `TargetRetirementDate` column; a value that is not a calendar date (`TBD`, or a milestone name) leaves the row blocking `RQ-1` until its co-owners approve a date, as the Spine's own table rule states.

(Same change to FR-28 item 9, line 688, final sentence: "... and every Architecture-owned row carries a target retirement date." → "... and an Architecture-owned row whose `TargetRetirementDate` is not yet a calendar date blocks on that ground alone.")

### 9. §8.1, lines 840-844 (three Spine defects)

Current (line 840, final clause, and lines 842-844):
> ... A Spine rule that narrows or contradicts an FR-18, FR-24, FR-28, or FR-33 rule is a Spine defect to be corrected before the consuming story is `ready-for-dev`; three are open at this revision, owned by Architecture:
> - AD-5 forbids the system abandonment from `Approved` and `PostingFailed` on a detected removal that FR-18 requires, and allows human abandonment from `PostingFailed` only.
> - AD-22 names the Tenant Agent Administrator as the sole inspection second party and gives export and hold release none, against FR-24 and FR-33.
> - AD-12 states the Release Operator trigger without the review, minimum sample, and `InsufficientEvidence` rule of FR-28.

Proposed (replace the clause and delete the three bullets):
> ... A Spine rule that narrows or contradicts an FR-18, FR-24, FR-28, or FR-33 rule is a Spine defect to be corrected before the consuming story is `ready-for-dev`. Open defects are tracked in the Spine's own reconciliation record and review ledger, not enumerated here; the Spine defects this PRD's 2026-09-09 update named against AD-5, AD-12, and AD-22 were absorbed by the Spine's second 2026-09-09 update (`updated: 2026-09-09`), which now states the `Approved`/`PostingFailed` system abandonment, the `Approved`-waits kill-switch rule with the trigger review and minimum sample, and the computed inspection second party with export and hold-release approvals. One divergence is open for a Product decision rather than an Architecture correction: Spine AD-13 runs Eligible Approver resolution in every response mode and rejects an Automatic-mode call with no configured Approver as `NoEligibleApprover`, while FR-8 scopes that step to Confirmation Response Mode; until Product rules, FR-8 governs and AD-13 is recorded as pending reconciliation.

Also add, in the same paragraph or as a new bullet under the "two such rules" sentence: the Spine's disabled-Agent retry-clock pause (`ARCH-A-11`: a `Disabled` Agent pauses the `PostingFailed` retry-window clock on the same terms as the kill switch) is a proposal-outcome rule absent from FR-3/FR-18 and is a PRD amendment pending under the §0 landing rule.

### 10. A-7, line 856

Current:
> | A-7 | `PostingFailed` retry bound of 3 attempts over 15 minutes; the Spine fixes the bound as non-configurable (AD-5), which this row does not contest | FR-18 | Architecture | Architecture confirms the value before enablement |

No factual change required (AD-5 still fixes 3 attempts within 15 minutes of the first `PostingFailed` `EvaluatedAt` and cites A-7). Optional clarification: append "measured as origin plus window plus any `PausedDuration` accrued while the tenant was `Suspended` (AD-5, AD-12)".

### 11. OQ-24, line 994

Current (excerpt):
> ... the compatibility command is the one permitted execution of a `Committed` seam. `RQ-1` is recorded in the launch readiness register; the external dependency register is the commitment authority; the FR-30 surface is the runtime record. Every unretired `A-n` and `ARCH-A-n` row blocks `RQ-1` whatever its owner.

Proposed: no factual change; the register's `Available` semantics add that where Level 5 is also required, cross-system attainment is completed by the consuming gate and `RQ-1` rather than being a prerequisite for starting the run. Optionally append: "The register's `Available` semantics govern how Level 5 attainment is completed for a seam whose `RequiredEvidenceLevel` names both levels."

### 12. OQ-26, line 996 (final sentence)

Current:
> The system abandonment from `Approved` and `PostingFailed` on a detected removal is a deliberate divergence from Spine AD-5, recorded in §8.1 as a Spine defect for Architecture to correct.

Proposed:
> The system abandonment from `Approved` and `PostingFailed` on a detected removal is stated by FR-18 and by Spine AD-5 (`updated: 2026-09-09`) on the same terms.

### 13. OQ-27, line 997

Current (excerpt):
> ... `Approved` proposals wait without posting (safety wins over "complete on their own terms", a Spine AD-12 correction owned by Architecture); ...

Proposed:
> ... `Approved` proposals wait without posting (safety wins over "complete on their own terms"; Spine AD-12, `updated: 2026-09-09`, states the same rule); ...

### 14. OQ-22, line 992

Current (final sentence):
> Unretired Product, Architecture, or Governance assumptions in §8.1, and unretired `ARCH-A` rows in the Architecture Spine, block `RQ-1`.

Proposed:
> Every unretired §8.1 `A-n` row and every unretired `ARCH-A-n` row in the Architecture Spine's current index version blocks `RQ-1`, whatever its owner.

### 15. addendum.md, lines 49-50

Current (line 49, final sentence): "AD-5 is recorded in §8.1 as a Spine defect to correct." Current (line 50, final sentence): "AD-12 is listed for Architecture to correct."

Proposed (line 49): "AD-5 was corrected in the Spine's second 2026-09-09 update to state the same transition." Proposed (line 50): "AD-12 was corrected in the Spine's second 2026-09-09 update to state the same rule."

### 16. Items the PRD need not change but should not adopt

- Story count: `epics.md` says "exactly 33 stories"; the SCP says 29. The PRD states none; keep it that way.
- `EXT-HOST-1` historical target `a66cdf34`/`2026-09-30`/`./eng/verify-agents-host.sh`: historical only per the register; the PRD does not cite them; keep it that way.
- Register defects to raise with owners rather than absorb into the PRD: the `PayloadProtectionUnavailable` vocabulary row's host-report phrasing (launch register line 53) versus FR-34; `LR-PARTY-IDENTITY`'s "Agent identity-link changes" (line 100) versus the immutable identity; no `DataHandlingVersion`/tenant-acceptance rule in the Provider Readiness Contract or `EXT-PROVIDER-1`; `epics.md` inventory rows FR3/FR4/FR5/FR12/FR22/FR25 and its absence of `Draft`, `MembershipUnavailable`, `MembershipRejected`, and the 30-day grace; the `AiAgent`/`AIAgent` double spelling at `epics.md` line 144.
- A future amendment of the SCP or a new proposal must list `prd.md` under `amends_if_approved` for any of the above PRD edits to be scheduled; the current proposal preserves `prd.md` unchanged.
