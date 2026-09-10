---
title: Sprint Change Proposal - Second 2026-09-09 PRD Downstream Reconciliation
status: approved
created: 2026-09-09
updated: 2026-09-10
mode: Batch
change_scope: moderate
recommended_path: direct-adjustment
project: agents
owner: Administrator
approval_required: false
approval_status: approved
approved_by: Administrator
approved_on: 2026-09-09
execution_status: approved-awaiting-implementation
routed_to:
  - Product Owner
  - Solution Architect
  - Release Operator
  - Agents Runtime Maintainer
  - Conversations Maintainer
  - Platform Maintainer
  - EventStore Maintainer
  - Test Architect
trigger_artifacts:
  - prds/prd-agents-2026-06-23/update-report-2026-09-09-2.md
  - prds/prd-agents-2026-06-23/review-consistency.md
governing_authority:
  - prds/prd-agents-2026-06-23/prd.md
amends_if_approved:
  - launch-readiness-register.md
  - external-dependency-register.md
  - architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
  - epics.md
  - prds/prd-agents-2026-06-23/update-report-2026-09-09.md
preserves:
  - prd.md without amendment
  - FR-1 through FR-34 and OQ-1 through OQ-30 as governing Product authority
  - active Epics 5 through 8 and their 29-story structure
  - completed Epics 1 through 4 as historical evidence
  - RQ-1 as an operational release gate outside the story backlog
---

# Sprint Change Proposal: Second 2026-09-09 PRD Downstream Reconciliation

## 1. Issue Summary

The second PRD update of 2026-09-09 made `prd.md` the governing authority for FR-1 through FR-34, OQ-1 through OQ-30, and A-1 through A-21. The launch-readiness register, external-dependency register, Architecture Spine, and executable epic inventory still encode the earlier authority. The original 2026-09-09 update report also lacks the trail line for two amendments applied under the first same-day sprint change proposal.

This drift is implementation-significant. A release evaluator can still gate `RQ-1` on post-enablement metrics, runtime paths can lack the exact dependency-availability and payload-protection outcomes the PRD requires, the Spine permits or prohibits proposal transitions differently from FR-18, and a developer can satisfy current story acceptance criteria while omitting new public values and fail-closed paths.

### Trigger Evidence

| Evidence | Current artifact state | Required consequence |
| --- | --- | --- |
| `review-consistency.md` F-LRR-1..3 | `LR-PRODUCT-METRICS` and `product-metrics` still say SM-1 through SM-6; no SM-7 contract | Split pre-enablement gates from launch health and add the complete blocker vocabulary |
| F-REG-1..4 and PRD §8 | `EXT-CONV-AI-1` omits the `MessageId` read details, event-feed alternative, typed existence/access answers, AI-Party verification, and four consumers | Amend the six-seam contract and compatibility summary without inventing final member names |
| FR-34 and register invalidation rule | `EXT-HOST-1` is `Committed` against a host artifact that has no protection binding or attestation port | Amend the required artifact and return `EXT-HOST-1` to `Uncommitted` |
| F-SPINE-1..4 and OQ-25..OQ-30 | Spine frontmatter stops at FR-33/OQ-23; AD-2/5/7/12/14/15/17/22/30 lag settled Product decisions | Reconcile the architectural contracts and assumption retirement schedule |
| F-EPICS-1 | Inventory rows FR1/7/18/21/26 are stale; FR29..FR34 and OQ-24..OQ-30 have no coverage | Update the executable inventory, maps, targeted story criteria, and UX-DR45 |
| F-UPD-1 | First same-day update report has no trail for the two applied proposal amendments | Add a factual provenance line |
| Source inspection | Deprecated values remain accepted; `ProposalDetail.razor` includes `PostingFailed` in `_terminalStates`; required public values and FR-34 paths are incomplete | Assign the debt to existing active stories under the Agents Runtime Maintainer |

### Problem Classification

This is a requirements-to-planning and architecture reconciliation discovered during sprint execution. It does not change the approved V1 thesis or add Product scope. It corrects downstream authorities and makes already-approved contracts executable.

## 2. Impact Analysis

### Epic Impact

| Epic | Impact | Viability |
| --- | --- | --- |
| Epic 5 — Live Governed Setup And Honest Readiness | Provisioning/Party immutability, data-handling version, readiness blockers, host attestation, and deprecate-and-reject behavior gain explicit ownership | Viable; 8 stories remain |
| Epic 6 — One Safe Automatic Conversation Response | Conversations consumers, five-state membership, block version/mirror, context-unavailable contract, and protection failure paths are aligned | Viable; 7 stories remain |
| Epic 7 — Complete Confirmation And Approval | Approver re-check markers, removal/deletion outcomes, `PostingFailed` lookup, `LateConfirmed`, and non-terminal UI behavior are added | Viable; 6 stories remain |
| Epic 8 — Governance Operations And Release Qualification | Second-party governance, metric classification, launch-health review, readiness blockers, and suspension/expiry reporting are aligned | Viable; 8 stories remain |

No epic or story is added, removed, renumbered, or made obsolete. Epic order remains valid. The active backlog remains 29 stories and `RQ-1` remains outside it. Story 7.5 may depend on the seam-2 lookup capability delivered by Story 7.4, but this does not resequence the epic.

### Story Impact

Substantive acceptance-criteria changes are required in Stories 5.2–5.6, 5.8, 6.1, 6.2, 6.6, 7.1, 7.4–7.6, 8.1, 8.4, 8.5, and 8.7. The user-visible outcomes and story identities remain stable; the changes add settled state, authorization, dependency, recovery, and evidence obligations.

### Artifact Conflicts

| Artifact | Conflict | Disposition |
| --- | --- | --- |
| `prd.md` | None for this correction; it is the governing authority | Preserve unchanged |
| `launch-readiness-register.md` | Metric and blocker semantics predate OQ-24/OQ-27 | Direct amendment |
| `external-dependency-register.md` | Conversations and protection/host commitments predate A-15/A-21 and FR-34 | Direct amendment; `EXT-HOST-1` becomes `Uncommitted` |
| `ARCHITECTURE-SPINE.md` | Binding range, lifecycle, membership, suspension, compliance, and blocker rules lag FR-1..FR-34/OQ-1..OQ-30 | Direct amendment |
| `epics.md` | Requirements inventory, coverage map, selected ACs, and code-debt ownership lag the PRD | Direct amendment without changing story count |
| `update-report-2026-09-09.md` | Applied-amendment trail is absent | Factual trail-line amendment |
| `sprint-status.yaml` | Story IDs and current statuses already match the 29-story active set | No structural/status change |

The UX spines were reviewed for impact because they are planning inputs. Their launch-readiness, suspension, proposal-lifecycle, and compliance wording also predates parts of OQ-24..OQ-30. They are not in the requested amendment set, so this proposal does not silently edit them; §6 records a UX reconciliation follow-up. Updating `epics.md` UX-DR45 remains in scope.

### Technical Impact

- Release qualification must distinguish gate attainment from post-enablement launch health and emit one stable reason per unresolved condition.
- Conversations integration gains typed negative answers and an outcome lookup that prevents duplicate posts and false abandonment after a lost acknowledgement.
- Platform host composition must bind the production protection engine and expose the FR-34 attestation path. Changing this accepted artifact invalidates the existing commitment.
- Proposal orchestration must treat `PostingPending` as uninterruptible, retain `PostingFailed` as non-terminal, look up `MessageId` before retry or exit, and record `LateConfirmed` when the post is found.
- Membership becomes an explicit five-state, versioned, mirrored state machine; Party link/replace operations remain public only as obsolete, deserializable, typed rejections.
- Public contracts evolve additively. Existing enum ordinals and member meanings remain unchanged.

## 3. Recommended Approach

Use **Direct Adjustment**. Amend the five named planning/authority artifacts, distribute implementation debt across the existing active stories, and preserve `prd.md`, the active epic structure, and completed historical evidence.

### Options Considered

| Option | Viability | Assessment |
| --- | --- | --- |
| Direct adjustment | Selected | Product decisions are already settled; targeted downstream edits restore one authority chain without changing scope |
| Roll back the first 2026-09-09 correction or completed work | Not viable | It would remove useful evidence and would not make the current PRD less binding |
| Add a new remediation epic | Not selected | Every obligation belongs to an existing Epic 5–8 outcome; a new epic would duplicate ownership |
| Reduce or redefine MVP | Not viable | FR-29..FR-34 and OQ-24..OQ-30 are governing launch requirements, not optional expansion |

### Effort, Risk, And Timeline

- Planning effort: medium; five authority artifacts and multiple story clauses change together.
- Implementation effort: high; orchestration, public contracts, UI behavior, protection attestation, and fail-closed tests all change.
- Risk before correction: high, because a false READY result, duplicate post, false abandonment, or unprotected content-bearing path is possible under the stale contracts.
- Risk after planning correction: medium until external owners commit targets and live verification commands.
- Calendar impact: not credibly estimable while all nine external records will be `Uncommitted` after the `EXT-HOST-1` amendment. No date is inferred.

## 4. Detailed Change Proposals

### 4.1 `launch-readiness-register.md`

#### Authority and `RQ-1` rule

**OLD**

> `RQ-1` returns READY only when every required record ... is `Pass`...

The text does not name the non-gate blockers and `LR-PRODUCT-METRICS` still makes real SM-1..SM-6 attainment one combined requirement.

**NEW**

Append the F-LRR-3 rule:

> `RQ-1` additionally records NOT READY when any `UnretiredAssumption`, `OpenDecision`, `DependencyNotAvailable`, `ProhibitedCostControlPosture`, or `PayloadProtectionUnavailable` blocker stands (PRD FR-28 input list).

Retain all 18 minimum GateIds, but define the `LR-PRODUCT-METRICS` pre-enablement `State` from SM-1, SM-4, SM-5, and SM-6 only. SM-2, SM-3, SM-7, and SM-C1..SM-C5 are reported under the same projection/contract family but never participate in `RQ-1` state aggregation.

#### Blocker vocabulary and emitter ownership

Add this table under “State, Freshness, And Invalidation”; it is additive to `EvidenceNotRecorded`, `DependencyUncommitted`, and `GateRecordMissing`.

| `BlockerCode` | Emitted by | Contract |
| --- | --- | --- |
| `DependencyNotAvailable` | `RQ-1` evaluation | One code per `EXT-*` record in the qualification profile that is not `Available`, or whose compatibility command did not pass against the exact target; names the record |
| `UnretiredAssumption` | `RQ-1` evaluation | One code per unretired PRD §8.1 `A-n` or Spine `ARCH-A-n` row; names the row and table, whatever its owner |
| `OpenDecision` | `RQ-1` evaluation | One code per *Deferred* PRD §13 row whose status says “before enablement”; names the OQ row |
| `ProhibitedCostControlPosture` | `RQ-1` evaluation | The recorded posture is `ReportingOnlyMonitoring` or `AcceptedLaunchRisk` |
| `PayloadProtectionUnavailable` | `RQ-1` evaluation, alongside `DependencyNotAvailable` for `EXT-PROTECTION-1` | The host cannot produce the FR-34 seal/unseal/erase identity-and-version attestation against the committed engine |
| `GateOutOfScope` | `RQ-1` evaluation | A register gate or evidence demand is not derivable from FR-28’s single input list; names the discrepant gate and remains NOT READY until reconciled |
| `TriggerReviewOverdue` | `LR-PRODUCT-METRICS` post-enablement trigger-review evaluation | A mandatory FR-28 trigger review was not convened and recorded within one business day; reported on the FR-30 surface and never treated as an `RQ-1` metric input |
| `InsufficientEvidence` | Any GateId; specifically `LR-PRODUCT-METRICS` for an unmet metric sample | Required timestamps, samples, live evidence, manifest fields, or evidence level are absent; `RQ-1` propagates NOT READY and names the gate/metric |

Use the exact F-LRR-1 semantics for the first five rows. The last three make the second-update additions explicit without turning `TriggerReviewOverdue` into a pre-enablement metric.

#### Metric inventory and projection contract

**OLD**

> `LR-PRODUCT-METRICS` | Versioned SM-1 through SM-6 calculation and real rolling-window/cohort attainment...
>
> `product-metrics` | SM-1 through SM-6 rolling-window/cohort calculations and insufficiency state.

**NEW**

> `LR-PRODUCT-METRICS` | Versioned calculation of the pre-enablement gate metrics SM-1, SM-4, SM-5, and SM-6 and their attainment on the qualification cohort (gate inputs), plus versioned calculation and reporting of the launch-health metrics SM-2, SM-3, SM-7 and counter-metrics SM-C1..SM-C5, which are never `RQ-1` inputs and are reviewed at 30 and 60 days after enablement and monthly thereafter; deterministic fixtures prove formulas only.
>
> `product-metrics` | SM-1/SM-4/SM-5/SM-6 gate calculations, SM-2/SM-3/SM-7 launch-health rolling-window/cohort calculations, SM-C1..SM-C5, and insufficiency state.

Update the initial `LR-PRODUCT-METRICS` record’s measurement-contract placeholder to name the same split.

Add the SM-7 measurement contract beside the other product-metric contracts:

- Numerator: distinct proposals edited, regenerated, or rejected before a human decision in the window.
- Denominator: distinct proposals reaching a human decision in the window.
- Attainment band: 10% through 60%, inclusive.
- Classification: launch health only; reviewed at 30 days, 60 days, and monthly thereafter.
- Insufficiency: missing authoritative decision/version events, incomplete window/cohort data, an empty denominator, or unmet contract-specific sample rules returns `InsufficientEvidence`; it never passes and never gates `RQ-1`.

### 4.2 `external-dependency-register.md`

#### Availability semantics

Add F-REG-2 after the Accepted Status table:

> A runtime, test, or qualification path that would execute a seam whose record is not `Available` fails closed with the typed outcome `DependencyNotAvailable` naming the record (PRD FR-21); `RQ-1` records the same code for every record in its qualification profile that is not `Available` (PRD FR-28).

#### `EXT-CONV-AI-1`

**OLD**

> Seam 1 accepts an AI participant; seam 2 posts idempotently; seam 4 supplies an active-Conversation count; seam 5 reads content/roster/existence. Consumers: 6.6, 7.4; `RQ-1`.

**NEW**

Amend `RequiredArtifact` while retaining exactly six seams:

1. Membership additionally verifies through Hexalith.Parties that the participant Party is AI type before accepting `ParticipantType.AiAgent`/`ParticipantRole.Member` (A-21). The record owner supplies final member and typed failure names.
2. Posting additionally exposes an existence read by deterministic `MessageId` that returns the message, typed absence, typed `ConversationDeleted`, typed `PrincipalRemovedFromConversation`, or unavailable. Agents uses it before retry or abandonment after a lost acknowledgement (FR-18, `LateConfirmed`).
3. Facilitator resolution remains `ParticipantRole.Facilitator`.
4. The Eligible Conversation denominator is supplied either as an active-Conversation count per tenant/window or as a Conversations-side event feed from which Agents computes it, never from Agent Calls (A-4).
5. Tenant-scoped content, roster, existence, and accessibility reads under the Agents Service Principal return `ConversationDeleted` and `PrincipalRemovedFromConversation` distinctly from transient denial/error; content contains the messages a Participant would currently see, including current edit/delete state (A-15).
6. The Conversation deletion signal remains unchanged.

Mirror all additions in `CompatibilityContractAndVerificationCommand`, including focused cross-tenant denial, and change:

> `ConsumingStories` | 6.1, 6.2, 6.6, 7.1, 7.4, 8.5; `RQ-1`

The record remains `Uncommitted`; no owner target, date, member name, or command is inferred.

#### Non-Conformance Records

Add the section and resolve it through one of these mutually exclusive Product-owned factual dispositions:

**Branch A — seam was consumed**

> `Story 5.3 | EXT-PROVIDER-1 | <verified completion date> | completed while Uncommitted; reopened by sprint-change-proposal-2026-09-09; reopening does not clear this record (PRD FR-21)`

**Branch B — seam was never consumed**

> None: Story 5.3 executed no `EXT-PROVIDER-1` seam (Product ruling `2026-09-10`).

At proposal approval on 2026-09-09, OQ-17 settled the rule but not the historical fact, so the non-conformance section remained an open approval condition. Product subsequently approved Branch B on 2026-09-10 from the implementation evidence. Substituted or deferred generation-provider ports were not treated as execution of a live external seam.

#### `EXT-HOST-1`

**OLD**

> Platform-owned host composition ... with EventStore, Conversations, Parties, Tenants, Provider and safety adapters, Dapr Workflow, secrets, health, identity, and telemetry.
>
> `AcceptedStatus` | `Committed`

**NEW**

The required artifact additionally binds the production `EXT-PROTECTION-1` engine and exposes the FR-34 attestation port used at startup and every readiness evaluation. The port must let Agents seal a canary, prove persisted bytes contain no plaintext, unseal it, destroy its DEK, replay `Erased`, and obtain engine identity/version for comparison with the committed protection target. The host must fail closed against the no-op default and a self-reporting wrapper around it.

Because `RequiredArtifact` and compatibility behavior changed, set `AcceptedStatus` to `Uncommitted` under the register’s own invalidation rule. Preserve the prior target and date as historical fields only if the Platform Maintainer re-accepts them for the expanded artifact; otherwise return them to `TBD`. The executable verification command is `TBD` until the owner supplies one that proves the expanded contract. Update the blocking summary to state that all nine records are `Uncommitted`.

#### `EXT-PROTECTION-1`

Replace the compatibility-command placeholder with an owner-supplied executable command that exercises, at minimum:

1. seal with plaintext-absence proof;
2. unseal and payload equality;
3. irreversible erase and typed `Erased` replay;
4. hold pin rejecting erase until authorized unpin;
5. engine identity and version reporting equal to the exact committed target.

Keep `AcceptedStatus: Uncommitted` and `Command: TBD` until the EventStore Maintainer supplies and accepts the command; the proposal does not invent a repository command.

### 4.3 `ARCHITECTURE-SPINE.md`

#### Frontmatter and aggregate model

**OLD**

> `PRD FR-1..FR-33`; `PRD OQ-1..OQ-23`
>
> `ConversationAgentState` holds membership-established fact, block, and non-terminal index.

**NEW**

- Bind `PRD FR-1..FR-34` and `PRD OQ-1..OQ-30`.
- AD-2 defines `ConversationAgentState` with exactly `NeverJoined`, `Joined`, `ExternallyRemoved`, `Blocked`, and `ReadmitPending`; a monotonic `BlockVersion`; a `MirrorPending` flag for the at-least-once removal outbox; and the non-terminal proposal index.

#### AD-5 and AD-7 lifecycle/membership reconciliation

**OLD**

> Abandon ... never from `Approved` or `PostingPending`...
>
> Pre-post removal records `PostingFailed` with `RemovedInConversations`.

**NEW**

- Human abandon remains legal from awaiting-decision states and `PostingFailed` where FR-18 authorizes it.
- System abandonment with `RemovedInConversations` is legal from `Approved` and `PostingFailed` when removal/block is detected and the seam-2 `MessageId` read finds no posted message. It is not legal from `PostingPending`.
- `PostingPending` is uninterruptible: only the posting attempt acknowledgement, typed failure, or bounded timeout exits it; a pending removal/block/suspension is applied on exit if the attempt did not post.
- Before every retry and every exit from `PostingFailed`, run the existence read. Present moves to `Posted` with `LateConfirmed`; unavailable refuses the exit; deleted/removed counts as no message for abandon only; retry requires typed absence.
- AD-7 moves a proposal to `Abandoned(RemovedInConversations)` on authoritative pre-post removal/block, not `PostingFailed`.
- `MembershipUnavailable` is also the acceptance-time rejection for an unavailable participant read; at acceptance it changes no state and abandons nothing.
- Block clearing follows FR-2/OQ-25: setting authority or Tenant Agent Administrator; Facilitator cannot clear an Administrator block; an `ExternallyRemoved` record may be cleared by the Tenant Agent Administrator or a current Facilitator; clearing records `ReadmitPending`, and rejoin occurs only at the next accepted membership step.
- Replace the Party link/replace implementation sentence with the FR-23 rule: both commands remain declared/deserializable, are obsolete, and reject every request with a typed reason. Provisioning creates the immutable AI-type Party identity.

This intentionally corrects F-SPINE-2’s over-broad shorthand: OQ-26 and FR-18 make `PostingPending` uninterruptible, so it is not added to the source states for system abandonment.

#### AD-12, AD-14, AD-15, and AD-17 readiness controls

- AD-12 adopts the mandatory rolling-seven-day trigger review over all four PRD rates: blocked-call share above 50%, unavailable-call share above 20%, Provider-or-generation failure share above 20%, or posting-failure rate above 10%.
- The blocked/unavailable numerators use distinct `(Party, Conversation, reason)` tuples over distinct `(Party, Conversation)` attempts, excluding only rate-limit rejections at Platform/Release-Operator limits; Tenant-Administrator-lowered-limit rejections count.
- The normal minimum sample is 5 distinct calling Parties and 50 Agent Calls; a tenant with fewer than 5 calling Parties uses every Party that called and 20 calls. Below it, record `InsufficientEvidence` and do not act.
- The Release Operator convenes and records the review within one business day, otherwise `TriggerReviewOverdue`; the switch is pulled only as the review’s recorded decision. A confirmed SM-4 event remains the Platform Operator’s immediate trigger.
- Under suspension, awaiting proposals remain human-rejectable/abandonable and continue toward expiry; `Approved` waits without posting or consuming a retry; an in-flight `PostingPending` attempt completes/fails; the `PostingFailed` retry clock pauses. `agent-setup` exposes `Suspended`.
- AD-14 names `PayloadProtectionUnavailable` as the typed runtime outcome and readiness blocker while the FR-34 attestation is absent or failing.
- AD-15 adds rate limits to blocked-call counters and exposes the second-update markers/statuses required by FR-25.
- AD-17 records `UnretiredAssumption`, `OpenDecision`, `DependencyNotAvailable`, `GateOutOfScope`, `TriggerReviewOverdue`, and `InsufficientEvidence` using the register’s emitter rules; `RQ-1` also emits `ProhibitedCostControlPosture` and `PayloadProtectionUnavailable` where applicable.

#### AD-22 and AD-30 governance authorization

Replace the compliance clause with FR-24/OQ-30:

- Compute the subject set from callers, editors, Approvers, decision actors, Facilitators in scope, and the Tenant Agent Administrator whose configuration was in force.
- The second party must be outside that set and not the Inspector. Use the Tenant Agent Administrator when eligible; otherwise the Platform Operator or a second Compliance Inspector. Two Inspectors cannot approve each other within 30 days. An inspection wider than one Conversation requires the Platform Operator.
- Post-hoc review is permitted only for single-proposal or single-Conversation scope and must finish within 7 days (A-19); otherwise record it as unreviewed. The rate/unreviewed surface is readable by the Tenant Agent Administrator and Platform Operator.
- `LegalHold` remains a Compliance Inspector command. `LegalHoldRelease` requires audited approval by a second Compliance Inspector or the Platform Operator. `ExportRequest` requires prior second-party approval under the FR-24 terms, never post hoc.
- AD-30 allows the `Platform` principal to dispatch `LegalHoldRelease` as approver for the named tenant.
- Delete “Two-person rule on legal-hold release” from Deferred Beyond V1.

#### Architecture assumption dates

Add `TargetRetirementDate` to the Architecture Assumptions table. Every unretired row whose `Owner` includes Architecture—currently ARCH-A-1, ARCH-A-2, ARCH-A-3, ARCH-A-4, ARCH-A-6, ARCH-A-7, and ARCH-A-8—must receive a date accepted by its named co-owners. ARCH-A-5 records `2026-09-09` as its actual retirement date. No date is invented by this proposal; the named owners must provide them before the Spine amendment is finalized.

### 4.4 `epics.md`

#### Requirements inventory

Replace the five stale rows with PRD-aligned summaries:

**FR1 NEW:** The Platform Operator provisions `hexa` once per tenant at tenant enablement through an idempotent create-only operation that establishes tenant scope and an immutable AI-type Party identity owned by the Agents Service Principal; Tenant Agent Administrators configure and activate it, and no tenant role may create a second Agent, delete `hexa`, or change its Party identity.

**FR7 NEW:** Tenant Agent Administrators configure Approver Policy from Conversation Facilitator, predefined Parties, and tenant roles; `Caller` is deprecate-and-reject. One Eligible Approver predicate—current participant/read access, policy-resolved, not caller, not last editor—applies at configuration, call, edit, action, and scheduled re-check. Empty/unavailable resolution uses the typed PRD outcomes and never strands or abandons on absent evidence.

**FR18 NEW:** A proposal uses exactly the ten recorded states `Pending`, `Edited`, `Regenerated`, `Approved`, `PostingPending`, `PostingFailed`, `Posted`, `Rejected`, `Abandoned`, and `Expired`; only the last four are terminal. `PostingPending` is uninterruptible, approval freezes expiry, retries are bounded/audited, removal may system-abandon `Approved`/`PostingFailed`, and the `MessageId` read produces `LateConfirmed` before any retry or exit from `PostingFailed`.

**FR21 NEW:** Dependency uncertainty fails closed. `Uncommitted` blocks consuming stories from `ready-for-dev`; completed consumption is recorded as non-conformance; `Committed` permits contract work only; runtime/test/qualification seam execution requires `Available` and its exact compatibility command, otherwise `DependencyNotAvailable`; content-bearing paths additionally fail with `PayloadProtectionUnavailable` under FR-34.

**FR26 NEW:** The Platform Operator with Security approval publishes the versioned Content Safety Policy; a Tenant Agent Administrator may add restrictions only. Production(-like) enablement requires the active policy, all four safety application points use current/no-weaker rules, and Approver override remains prohibited.

Add inventory rows:

| Requirement | PRD-aligned inventory text |
| --- | --- |
| FR29 | Administrative writes distinguish local `Submitted`, durable `AuthoritativePending`, and projection-confirmed `ProjectionConfirmed`; accepted writes are not resubmitted and reads expose projection version/freshness. |
| FR30 | Public governance operations cover readiness/blockers, safety/budget policy, legal hold, export, and deletion with FR-33 authorization, required justification, tenant isolation, and protection-engine fail-closed behavior. |
| FR31 | Conversation content is untrusted data and cannot alter Agent Instructions, policy, scope, or authority; impersonation/decision assertions and control-bypass attempts are safety outcomes. |
| FR32 | Platform/Release Operators configure numeric caps, rate limits, and per-Party concurrent-interaction bounds with no implicit default; tenant administrators may only lower; regeneration ceiling remains per Agent; cap override is bounded and Platform-only. |
| FR33 | Every operation maps to one of six roles and a stated scope, including Platform provisioning, readiness, suspension, compliance inspection, legal-hold release approval, export prior approval, and deletion approval. |
| FR34 | Content-bearing execution stays disabled until `EXT-PROTECTION-1` is `Available` and the host-bound engine passes the seal/unseal/erase identity/version attestation; otherwise readiness and runtime fail closed with `PayloadProtectionUnavailable`. |

#### Coverage and decision maps

Add:

- FR-29 → Epic 5
- FR-30 → Epic 8
- FR-31 → Epic 6
- FR-32 → Epic 8
- FR-33 → Epics 5–8
- FR-34 → Epics 5 and 8

Update each active Epic’s “FRs covered” list accordingly. Add explicit OQ references where the settled decision is implemented:

| OQ | Story references |
| --- | --- |
| OQ-24 | 5.5, 5.8, 8.7, and the RQ-1 section |
| OQ-25 | 5.2/5.4 identity deprecation and 6.6 state/mirroring |
| OQ-26 | 6.6, 7.4, and 7.5 lifecycle/existence-read behavior |
| OQ-27 | 5.5/5.7 readiness/suspension, 7.6 expiry, and 8.5 trigger metrics |
| OQ-28 | 5.2 provisioning and immutable Party identity |
| OQ-29 | 5.3 `DataHandlingVersion` governance |
| OQ-30 | 8.1 hold release, 8.2 export, and 8.8 inspection second party |

#### Targeted story changes

**Story 5.4 — OLD**

> Given caller, predefined Party, tenant-role, or Conversation Facilitator approver sources...

**NEW**

> Given predefined Party, tenant-role, or Conversation Facilitator approver sources...

Add a negative criterion that `ApproverPolicySourceKind.Caller` stays declared/deserializable but is obsolete and server-rejected.

**Story 6.6 — NEW membership/removal criterion**

`ConversationAgentState` exposes exactly the five states, `BlockVersion`, `MirrorPending`, and the non-terminal proposal index. It implements every state/read pair from FR-2; authoritative block set increments version and drives an at-least-once mirror; clear authority is evaluated at clear time; clearing records `ReadmitPending`; rejoin occurs only at the next accepted membership step. `PostingPending` is excluded from immediate abandonment and reconciled on exit.

**Story 8.1 — NEW hold-release criterion**

A Compliance Inspector submits release, but unpin/release cannot execute until a distinct second Compliance Inspector or Platform Operator records audited approval. The same actor cannot satisfy both steps, partial approval/unpin stays restrictive, and AD-30 principal/role evidence is preserved.

**Stories 7.4 and 7.5 — NEW `PostingFailed` exit protocol**

Story 7.4 owns the seam-2 lookup integration. Before every retry or exit from `PostingFailed`, lookup by deterministic `MessageId`: present → `Posted(LateConfirmed)` and reject the requested action; unavailable → refuse and remain `PostingFailed`; typed absence → retry may proceed after full re-validation; deleted/removed → counts as no post for an authorized abandon only. Story 7.5 adds `PostingFailed` abandonment paths and depends on the capability established by 7.4; it never records `Abandoned` when the message is present.

Align external dependencies with the register: add `EXT-CONV-AI-1` to Stories 6.1, 6.2, 6.6, 7.1, 7.4, and 8.5 as the exact owning/qualification consumers; do not add a contradictory consumer list elsewhere.

#### UX-DR45

**OLD**

> ... exact NFR-9 and NFR-14 gates; SM-2/SM-3 cohorts/windows; sample sufficiency...

**NEW**

> Implement Launch readiness showing `Pass`, `Block`, `InsufficientEvidence`, and `Stale`; current `ObservedAt`/exclusive `ValidUntil`; exact NFR-9 and NFR-14 gates; SM-1/SM-4/SM-5/SM-6 gate results; SM-2/SM-3/SM-7 launch-health results reported, not gated; SM-C1..SM-C5; sample sufficiency; Levels 4–5; and all safe blockers. Lower evidence, skips, missing timestamps/references, invalid browser samples, and insufficient samples cannot render as Pass.

#### Implementation debt folded into existing stories

Owner for every row: **Agents Runtime Maintainer**. These are acceptance/evidence obligations, not a new epic or story.

| Debt | Owning story changes |
| --- | --- |
| Readiness accepts `ReportingOnlyMonitoring` / `AcceptedLaunchRisk` | 5.5 rejects both at recording and emits `ProhibitedCostControlPosture`; 5.7 refuses activation/enablement while it stands |
| Configuration accepts `BlockWithAuditableOverride` | 8.4 retains the enum additively but marks it obsolete and server-rejects it at every presentation; tests prove no Approver override |
| Configuration/UI accepts `Caller` | 5.4 deprecate-and-rejects `ApproverPolicySourceKind.Caller`, removes it from new configuration UI, and migration/read surfaces remain safe |
| Party link/replace commands remain operative | 5.2/5.4 keep wire members, mark obsolete, reject every request, and prove FR-1 provisioning is the only identity creation/link path |
| `ProposalDetail.razor` treats `PostingFailed` as terminal | 7.4/7.5 remove it from terminal sets and expose only PRD-authorized retry/abandon/audit actions, guarded by the `MessageId` lookup |
| Earlier additive contract gaps | 6.2 owns `AgentInteractionContextMode.Blocked`, `ContextUnavailable`, and Safe Context Budget terms; 6.4 owns `AgentGenerationOutcome.Indeterminate` and `NotInvoked`; 6.6 owns the Membership gate, `RemovedInConversations`, `BlockVersion`, and `MirrorPending`; 7.1 owns `NoEligibleApprover`, `ApproverResolutionUnavailable`, `ResolutionEmptyPending`, and `ResolutionUnavailable`; 7.4/7.5 own `SourceConversationUnavailable` and `LateConfirmed`; 5.5/8.7 own `UnretiredAssumption` |
| Second-update additive values | 5.3 owns `DataHandlingVersion`; 7.6 and 8.5 own `ExpiredWhileSuspended` plus its SM-3/SM-C5 exclusion; 5.5/8.7 own `GateOutOfScope` and `TriggerReviewOverdue`; 6.2 verifies `ContextUnavailable` exists on the exact required public surface rather than assuming a narrower existing enum is sufficient |
| FR-34 blocker, attestation, and fail-closed paths | 5.6 owns the host port/binding; 5.8 owns `PayloadProtectionUnavailable`, the canary self-test, engine identity/version check, no-op and self-reporting-wrapper rejection, and content-path fail-closed integration coverage; 5.5/8.7 own readiness projection/rendering; Stories 6.1–7.4 and 8.1–8.3 prove their content-bearing paths refuse execution while the blocker stands |

All additions preserve existing enum ordinals and serialized names. A value already present in a narrower type is not duplicated blindly; the owning story proves the exact PRD-required public surface, semantics, serialization, UI/API parity, and fail-closed use.

### 4.5 `update-report-2026-09-09.md`

Add this trail line under “What changed” or immediately before “Reviewer gate”:

> **Proposal-amendment trail:** The two PRD amendments authorized by `sprint-change-proposal-2026-09-09.md` were applied: `EXT-PROTECTION-1` was added to §8 and the `ARCH-A` cross-index to §8.1. The second 2026-09-09 update then aligned FR-1/FR-33 to Spine AD-2/AD-30 under OQ-28 and brought `Unreconciled` into FR-28 from ARCH-A-6.

### 4.6 Explicit Non-Changes

- Do not amend `prd.md`.
- Do not add, remove, renumber, or re-estimate an epic or story.
- Do not mark any dependency `Committed` or `Available` without its owner accepting every field.
- Do not invent final external API member names, verification commands, target commits, integration dates, or Architecture assumption retirement dates.
- Do not resolve deferred OQ rows beyond their current PRD statuses.

## 5. Implementation Handoff

### Scope Classification

**Moderate — coordinated authority and backlog adjustment.** No fundamental Product or architecture replan is required, but register state, architectural contracts, story acceptance criteria, and implementation debt must land together.

### Sequencing

1. [x] Product resolved the Story 5.3 / `EXT-PROVIDER-1` historical-consumption fact as Branch B on 2026-09-10.
2. Architecture owners provide target retirement dates and amend the Spine.
3. Register owners amend both registers. `EXT-HOST-1` becomes `Uncommitted` immediately when its expanded artifact is recorded.
4. Product Owner updates the requirements inventory, coverage/OQ maps, targeted ACs, UX-DR45, and debt ownership in `epics.md`.
5. The PRD update report receives the provenance line.
6. Agents Runtime Maintainer implements in story dependency order. External commands and targets remain gated by their owners.
7. Test Architect runs document consistency checks, contract/API/UI parity tests, and the relevant live verification only after dependencies become `Available`.

### Recipients And Responsibilities

| Recipient | Responsibility |
| --- | --- |
| Product Owner | Select the evidence-backed Story 5.3 non-conformance disposition; approve inventory/map/AC changes; keep PRD unchanged |
| Solution Architect | Apply AD changes, obtain `TargetRetirementDate` values from co-owners, and preserve `PostingPending` uninterruptibility |
| Release Operator | Validate `RQ-1` emitter ownership, gate/launch-health separation, and trigger-review reporting |
| Conversations Maintainer | Accept or correct the expanded `EXT-CONV-AI-1` seams, target/date, and executable compatibility command |
| Platform Maintainer | Accept the expanded `EXT-HOST-1` artifact/attestation contract and supply a verifying target/date/command |
| EventStore Maintainer | Supply the `EXT-PROTECTION-1` compatibility command covering seal, unseal, erase, hold-pin, and identity/version |
| Agents Runtime Maintainer | Implement every assigned additive/deprecate-and-reject/orchestration/UI/fail-closed item without breaking wire compatibility |
| Test Architect | Prove exact serialization, transition, dependency, attestation, cross-tenant denial, metrics, and UI/API parity behavior |

### Success Criteria

1. The launch register can never require SM-2, SM-3, or SM-7 attainment for `RQ-1`; it reports them as launch health and includes a complete SM-7 contract.
2. Every requested blocker has one documented emitter; `RQ-1` returns NOT READY for unresolved gate-level or evaluation-level blockers.
3. `EXT-CONV-AI-1` contains all six amended seams and the exact consumer set; no transient outage is typed as deletion/removal.
4. `EXT-HOST-1` is honestly `Uncommitted` after its artifact changes, and neither host nor protection records can become `Available` without the required executable attestations.
5. Spine frontmatter, proposal lifecycle, membership state, suspension, compliance authorization, blocker vocabulary, and Architecture assumption dates match the PRD.
6. `epics.md` contains aligned FR1..FR34 inventory/coverage, OQ-24..OQ-30 citations, targeted story criteria, and code-debt ownership while retaining 29 active stories.
7. `ProposalDetail.razor` and API/UI contracts treat `PostingFailed` as non-terminal and can never abandon a confirmed post.
8. The original update report carries the applied-amendment trail, and `prd.md` has no diff.
9. No story becomes `ready-for-dev` or executes an external seam contrary to the register’s `Committed`/`Available` rules.

## 6. Open Items And Boundaries

### Product decision resolved

1. **Story 5.3 / `EXT-PROVIDER-1` historical fact.** Product approved Branch B on 2026-09-10: Story 5.3 executed no `EXT-PROVIDER-1` seam. The evidence distinguishes substituted and fail-closed deferred provider ports from execution of a live external Provider adapter. No non-conformance record is required for Story 5.3 under FR-21.

### Owner commitments required, not Product scope decisions

1. Architecture/co-owners must supply target retirement dates for every unretired Architecture-owned `ARCH-A` row.
2. Conversations, Platform, and EventStore maintainers must supply accepted immutable targets, integration dates, and executable verification commands. `TBD` remains blocking.
3. The `EXT-CONV-AI-1` owner chooses count versus event feed for seam 4 within the PRD-approved alternative and owns final member names.

### Observed follow-up outside this amendment set

`EXPERIENCE.md` still contains first-update wording for assumption ownership, kill-switch treatment of `Approved`, `PostingFailed` exits, compliance second parties, and hold/export approvals. The UX owner should run a separate reconciliation against OQ-24..OQ-30 before Stories 5.7, 7.4, 8.1, 8.2, 8.7, or 8.8 become `ready-for-dev`. No Product choice is needed where the PRD already settles the behavior; this proposal simply does not expand its amendment set silently.

## 7. Checklist Record

| Checklist item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [N/A] | Trigger is the second PRD update and its review report, not a story |
| 1.2 Core problem | [x] | Downstream authorities and implementation backlog lag the governing PRD |
| 1.3 Evidence | [x] | Update report, consistency findings, current artifacts, tracker, and focused source inspection agree |
| 2.1 Current epic viability | [x] | Epics 5–8 remain viable |
| 2.2 Required epic changes | [x] | No new/redefined epic; update inventory, mappings, ACs, and dependencies |
| 2.3 Remaining epic impact | [x] | Every active epic has named changes |
| 2.4 Obsolete/new epics | [N/A] | None |
| 2.5 Order/priority | [x] | Epic order stands; register/Spine correction precedes story readiness |
| 3.1 PRD conflict | [x] | PRD is authoritative and explicitly preserved |
| 3.2 Architecture conflict | [x] | AD-2/5/7/12/14/15/17/22/30 and assumptions table identified |
| 3.3 UI/UX conflict | [!] | UX-DR45 is in scope; residual UX Spine drift is a named follow-up, not silently amended |
| 3.4 Other artifacts | [x] | Both registers, update trail, source debt, tests, and tracker assessed |
| 4.1 Direct adjustment | [x] Viable | Planning effort medium; implementation effort/risk high |
| 4.2 Rollback | [x] Not viable | Removes evidence without resolving current authority |
| 4.3 MVP review | [x] Not viable | No scope reduction is warranted |
| 4.4 Recommended path | [x] | Direct adjustment |
| 5.1 Issue summary | [x] | Included |
| 5.2 Impact/artifact needs | [x] | Included |
| 5.3 Path and trade-offs | [x] | Included |
| 5.4 MVP/action plan | [x] | MVP unchanged; sequencing defined |
| 5.5 Handoff | [x] | Recipients and responsibilities defined |
| 6.1 Checklist review | [x] | All applicable items addressed; open items are explicit |
| 6.2 Proposal accuracy | [x] | Checked against PRD, F-LRR/F-REG/F-SPINE/F-EPICS/F-UPD, current artifacts, and source evidence |
| 6.3 User approval | [x] | Administrator explicitly approved the proposal on 2026-09-09 |
| 6.4 Sprint-status update | [N/A] | No epic/story IDs or statuses change |
| 6.5 Handoff confirmation | [x] | Moderate-scope handoff routed to the recipients and responsibilities in §5 |

## Approval Gate

Administrator explicitly approved this proposal on 2026-09-09. The coordinated changes listed under `amends_if_approved` are authorized for implementation through the handoff in §5. At that approval, the Story 5.3 historical-consumption fact and the owner commitments in §6 remained unresolved blockers; approval did not fabricate their answers or permit a dependency status to advance without its owner accepting every required field. Product subsequently resolved the Story 5.3 fact as Branch B on 2026-09-10; the external-owner commitments remain unresolved.

## 8. Approval And Workflow Execution Log

- 2026-09-09 — Correct Course activated in Batch mode for the second 2026-09-09 PRD downstream reconciliation.
- 2026-09-09 — The governing PRD, epics, Architecture Spine, UX spines, dependency and readiness registers, update reports, implementation tracker, and focused source evidence were assessed.
- 2026-09-09 — Direct adjustment was selected; rollback, a new remediation epic, and MVP reduction were rejected.
- 2026-09-09 — The complete Sprint Change Proposal was presented to Administrator without amending `prd.md` or applying downstream implementation changes.
- 2026-09-09 — Administrator continued the complete-proposal review and explicitly approved the proposal.
- 2026-09-09 — Moderate-scope handoff routed to Product Owner, Solution Architect, Release Operator, Agents Runtime Maintainer, Conversations Maintainer, Platform Maintainer, EventStore Maintainer, and Test Architect.
- 2026-09-10 — Administrator, acting as Product authority, approved Branch B: Story 5.3 executed no `EXT-PROVIDER-1` seam. The ruling was recorded without changing the dependency's `Uncommitted` status.

### Handoff Completion

The approved handoff package is §4's exact register, Spine, epics, update-trail, and implementation-debt edit set, sequenced by §5 and bounded by §6. Product determined the Story 5.3 historical-consumption fact as Branch B on 2026-09-10. Architecture and external owners must still supply the retirement dates and dependency commitments that the proposal deliberately leaves open. Implementation may begin only under the register's `Committed`/`Available` rules and must satisfy all nine success criteria before the course correction is complete.
