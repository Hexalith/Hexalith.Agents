---
title: Sprint Change Proposal - 2026-09-09 PRD Validation Follow-Through
status: approved
created: 2026-09-09
updated: 2026-09-09
mode: Batch
change_scope: moderate
recommended_path: direct-adjustment
project: agents
owner: Administrator
approval_required: false
approval_status: approved
approved_by: Administrator
approved_on: 2026-09-09
execution_status: complete
routed_to:
  - Product Owner
  - Developer
  - Solution Architect
  - Agents Runtime Maintainer
  - Conversations Maintainer
  - Release Operator
  - Test Architect
  - UX Owner
trigger_artifacts:
  - prds/prd-agents-2026-06-23/reconcile-validation-2026-09-09-2.md
  - prds/prd-agents-2026-06-23/prd.md
  - prds/prd-agents-2026-06-23/review-implementation-drift-2026-09-09-validate.md
amends_if_approved:
  - launch-readiness-register.md
  - external-dependency-register.md
  - epics.md
  - architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
  - ../implementation-artifacts/sprint-status.yaml
preserves:
  - the final updated PRD as product authority
  - the V1 vision, MVP scope, and Epics 5 through 8 outcomes
  - completed Epics 1 through 4 as historical evidence
  - the approved 2026-09-09 architecture-backlog reconciliation
  - RQ-1 as an operational release gate outside the story backlog
---

# Sprint Change Proposal: 2026-09-09 PRD Validation Follow-Through

## 1. Issue Summary

The 2026-09-09 PRD validation report identified planning, readiness-register, dependency-register, and Architecture Spine drift. The PRD update that followed resolved the product decisions, but the governed downstream artifacts still encode the pre-update rules.

The authoritative PRD now requires:

- `RQ-1` to use SM-1, SM-4, SM-5, and SM-6 as its pre-enablement product-metric inputs, with NFR-9, NFR-14, and 100% audit completeness; SM-2, SM-3, and SM-7 are post-enablement launch-health metrics.
- `UnretiredAssumption` and `ProhibitedCostControlPosture` to be explicit support-safe blocker vocabulary, with the latter also represented on the runtime readiness surface.
- Approver Policy sources to be Conversation Facilitator, predefined Parties, and tenant roles only; `ApproverPolicySourceKind.Caller` remains deserializable but is rejected wherever presented.
- the ten recorded proposal states `Pending`, `Edited`, `Regenerated`, `Approved`, `PostingPending`, `PostingFailed`, `Posted`, `Rejected`, `Abandoned`, and `Expired`.
- FR-29 through FR-33, plus the later FR-34 payload-protection requirement, to be represented by the active delivery plan.
- Platform-Operator, create-only provisioning of one `hexa` and its immutable AI Party identity at tenant enablement; Tenant Agent Administrators configure and operate the provisioned Agent but cannot create another Agent or replace its Party identity.
- kill-switch suspension to hold `Approved`, allow an already-started `PostingPending` attempt to finish, pause `PostingFailed` retries, keep awaiting-proposal expiry running, and never system-abandon a proposal.

The implementation-drift review separately confirms four open code-debt clusters owned by the Agents Runtime Maintainer. None currently has an explicit remediation story.

### Trigger Evidence

| Evidence | Current downstream drift | Consequence |
| --- | --- | --- |
| Validation `CONS-1` | `LR-PRODUCT-METRICS` and `product-metrics` still say SM-1 through SM-6 | SM-2 and SM-3 are incorrectly treated as pre-enablement inputs and SM-7 has no launch-health home |
| Validation `CONS-3` | `epics.md` still names caller as an Approver source, summarizes the old FR-18 lifecycle, and stops at FR-28 | A story can implement a retired authority source or incomplete state/requirement contract |
| Validation `CONS-7` | The readiness register has no closed blocker-code vocabulary containing the new blockers | `RQ-1` and runtime surfaces can disagree on why enablement is blocked |
| Validation `CONS-8` | Story 5.3 is reopened, but the dependency register has no explicit non-conformance record | Shipped tenant-scoped catalog work can be mistaken for current architecture conformance |
| Validation `CONS-9` | `EXT-CONV-AI-1` consumers list only Stories 6.6 and 7.4 | Stories consuming Facilitator, context/roster/existence, MessageId lookup, and metric-denominator seams can enter readiness prematurely |
| Architecture AD-12 | It currently lets both `Approved` and `PostingPending` complete while suspended | A new post may begin after the kill switch is pulled, contrary to FR-28 |
| Architecture assumptions table | The open `ARCH-A-n` index has no explicit index version | An `RQ-1` evaluation cannot bind its assumption scan to a stable index revision |
| Implementation-drift review | Four debt clusters remain open | The accepted PRD is not yet represented by owned executable work |

### Problem Classification

This is a requirements-synchronization and implementation-debt ownership correction. It is not a strategic pivot, failed product approach, or MVP scope change.

## 2. Impact Analysis

### Epic Impact

| Epic | Change | Viability |
| --- | --- | --- |
| Epic 5 — Live Governed Setup And Honest Readiness | Correct Approver-source/readiness criteria and add two code-debt remediation stories | Viable; grows from 8 to 10 stories |
| Epic 6 — One Safe Automatic Conversation Response | Extend Conversations dependencies and add one additive-contract remediation story | Viable; grows from 7 to 8 stories |
| Epic 7 — Complete Confirmation And Approval | Align the ten-state lifecycle and add one posting-failure UI/queue remediation story | Viable; grows from 6 to 7 stories |
| Epic 8 — Governance Operations And Release Qualification | Keep metric calculators for all seven SMs but classify only SM-1/4/5/6 as pre-enablement and SM-2/3/7 as launch-health | Viable; remains 8 stories |

The active backlog grows from 29 to 33 stories. No new epic, rollback, or epic resequencing is required. `RQ-1` remains outside the backlog.

### Artifact Impact

| Artifact | Required adjustment |
| --- | --- |
| `prd.md` | None. It is the authority being propagated. |
| `launch-readiness-register.md` | Correct metric ownership, introduce launch-health reporting, add closed blocker vocabulary, and update the projection/initial-record descriptions. |
| `epics.md` | Refresh the requirements inventory and coverage map; remove active caller-source requirements; add four remediation stories and collateral acceptance criteria. |
| `sprint-status.yaml` | Add the four exact backlog story slugs after `epics.md` changes; preserve all current statuses and action items. |
| `external-dependency-register.md` | Add the Story 5.3 non-conformance record and expand `EXT-CONV-AI-1` consumers to every seam consumer. |
| `ARCHITECTURE-SPINE.md` | Clarify provisioning authority, correct kill-switch state behavior and trigger review, and explicitly version the ARCH-A index. |
| UX `DESIGN.md` / `EXPERIENCE.md` | No route, layout, component, or user-flow change. Existing UX already distinguishes the ten proposal states; active epic wording is the remaining planning drift. |

### Technical Impact

- The four new stories authorize additive contract, aggregate-policy, API/UI, and test changes; this proposal does not implement those code changes.
- Caller and prohibited safety/cost values remain wire-compatible under FR-23 but become typed server-side rejections and are removed from configuration UI choices.
- `PostingFailed` becomes actionable/non-terminal in proposal detail and queue behavior; `Posted` alone is the confirmed-post terminal outcome.
- Register-only blocker vocabulary remains outside the Agent aggregate unless the PRD explicitly names a runtime `AgentLaunchReadinessBlocker` value.
- Existing Story 5.3 code remains a migration source, not conformance evidence for the platform catalog plus tenant enablement architecture.

## 3. Recommended Approach

Use **Direct Adjustment**. Amend the five governed downstream artifacts, retain Epics 5–8, and add four focused remediation stories under the existing owning epics.

| Option | Decision | Rationale |
| --- | --- | --- |
| Direct adjustment | Selected | The PRD decision is complete, existing epic outcomes remain correct, and every change has a natural current owner. |
| Roll back Story 5.3 or other completed work | Rejected | The already-approved migration approach preserves the useful implementation as migration source; rollback would not fix the registers or code-debt clusters. |
| Reduce/redefine MVP | Rejected | These changes enforce already-approved launch, safety, authorization, and lifecycle requirements. |

- Planning/document effort: medium.
- Implementation effort represented by the new stories: medium to high.
- Risk before correction: high for readiness integrity and posting behavior; medium for additive contract drift.
- Schedule impact: four backlog stories. A calendar estimate remains dependent on the uncommitted external seams.

## 4. Detailed Change Proposals

### 4.1 Launch Readiness Register

#### RQ-1 metric slice and launch health

**OLD**

> `LR-PRODUCT-METRICS` — Versioned SM-1 through SM-6 calculation and real rolling-window/cohort attainment.
>
> `product-metrics` — SM-1 through SM-6 rolling-window/cohort calculations and insufficiency state.

**NEW**

> `LR-PRODUCT-METRICS` — Current pre-enablement qualification for SM-1, SM-4, SM-5, and SM-6 only. SM-5 supplies the 100% audit-completeness requirement. NFR-9 is evaluated by `LR-RUNTIME-PERFORMANCE` and NFR-14 by `LR-UI-PERFORMANCE`. SM-2, SM-3, and SM-7 never contribute to `RQ-1`.
>
> `product-metrics` — Versioned calculations for SM-1 through SM-7 and SM-C1 through SM-C5, explicitly partitioned into the pre-enablement `RQ-1` set (SM-1/4/5/6) and post-enablement launch-health set (SM-2/3/7).

Add a **Launch Health Reviews** section: review SM-2, SM-3, and SM-7 at 30 days, 60 days, and monthly thereafter; report `InsufficientEvidence` without silently passing; route two consecutive SM-3 or SM-7 misses into the FR-28 Product decision and kill-switch trigger review. This section reports health and cannot mutate an already-recorded `RQ-1` result.

The `ReleaseQualificationGateSet` retains the non-metric safety, dependency, recovery, capacity, accessibility, and governance controls that FR-28 also names. The change narrows the product-metric inputs; it does not delete those controls.

#### Blocker vocabulary

**OLD**

> `BlockerCode` — Stable support-safe code.

**NEW**

Add a closed V1 vocabulary table that includes at least:

- register-evaluated `UnretiredAssumption`, `DependencyNotAvailable`, `OpenDecision`, `InsufficientEvidence`, `GateOutOfScope`, `TriggerReviewOverdue`, `GateRecordMissing`, and `EvidenceNotRecorded`;
- runtime-and-register-visible `PayloadProtectionUnavailable` and `ProhibitedCostControlPosture`;
- `DependencyUncommitted` only for the initial compatibility/readiness evidence condition already represented by the register.

Unknown blocker codes fail closed. `UnretiredAssumption` names the exact `A-n` or `ARCH-A-n` row and the versioned table/index evaluated. `ProhibitedCostControlPosture` is emitted for `ReportingOnlyMonitoring` or `AcceptedLaunchRisk`, never collapsed into a missing-posture code.

Update the initial `LR-PRODUCT-METRICS` row to require the versioned SM-1/4/5/6 contract and 100% SM-5 audit completeness.

### 4.2 Epics And Requirements Traceability

#### Retired caller source

**OLD active planning text**

> FR7 includes Conversation owner, caller, predefined Parties, or tenant roles.
>
> Approver authority resolves from current caller, predefined Party, tenant-role, and Conversation Facilitator evidence.
>
> UX-DR5 includes caller as an Approver-policy builder row.
>
> Story 5.4 resolves caller, predefined Party, tenant-role, or Conversation Facilitator approver sources.

**NEW**

Use exactly the three sources: Conversation Facilitator (legacy wire value `ConversationOwner`), predefined Parties, and tenant roles. Remove caller from the active Requirements Inventory, Additional Requirements, UX Design Requirements, Story 5.4 criteria/evidence, and any forward dependency text. Retain completed Epics 1–4 verbatim as historical evidence under the existing replacement-authority rule; their conflicting caller criterion remains `mustNotImplement`.

#### FR-18 ten-state contract

**OLD**

> FR18 describes rejected, abandoned, and expired terminal states and omits the complete posting lifecycle.

**NEW**

Replace the inventory row with the ten recorded states: `Pending`, `Edited`, `Regenerated`, `Approved`, `PostingPending`, `PostingFailed`, `Posted`, `Rejected`, `Abandoned`, and `Expired`; `Unknown` is a never-recorded sentinel. State that only `Posted`, `Rejected`, `Abandoned`, and `Expired` are terminal; `PostingFailed` permits bounded audited recovery; and only confirmed message existence reaches `Posted`. Add the PRD transition-table and marker obligations (`ResolutionUnavailable`, `LateConfirmed`, `ExpiredWhileSuspended`) to the applicable Stories 7.4–7.7 without duplicating product rules.

#### FR-29 through FR-34 inventory and coverage

Add current condensed Requirements Inventory rows and FR Coverage Map rows:

| Requirement | Active delivery coverage |
| --- | --- |
| FR-29 accepted-write truth stages | Epics 5–8 public write/API/UI stories; `Submitted`, `AuthoritativePending`, `ProjectionConfirmed` |
| FR-30 governance operations and readiness surface | Epics 5 and 8; readiness blockers, legal hold, export, deletion, compliance inspection, kill switch |
| FR-31 untrusted Conversation content | Epics 6 and 7; role-separated input, control-bypass handling, no authority from content |
| FR-32 tenant caps and consumption bounds | Epics 6 and 8; numeric caps, rates, concurrency, reservation, regeneration ceiling |
| FR-33 six-role authorization matrix | Epics 5–8; exact operation/role/scope parity and role-basis audit |
| FR-34 payload protection | Epics 5–8, owned principally by Story 5.8 and governance stories 8.1–8.3/8.8 |

FR-34 is included as required collateral because it is present in the updated PRD and is already partially assigned to Story 5.8; omitting it would leave the inventory knowingly incomplete.

#### Collateral existing-story patches

- Story 5.8 explicitly owns the host-supplied protection-availability signal and additive `PayloadProtectionUnavailable` runtime blocker.
- Story 6.6 consumes the full five-state membership contract (`NeverJoined`, `Joined`, `ExternallyRemoved`, `Blocked`, `ReadmitPending`) and exposes `MirrorPending` without treating an at-least-once mirror as authoritative state.
- Stories 7.4–7.7 own the PRD transition table, `MessageId` existence check, `LateConfirmed`, `ResolutionUnavailable`, suspended-state restrictions, and the fact that `PostingFailed` is non-terminal.
- Story 8.5 retains calculators for SM-1 through SM-7 but never publishes SM-2/3/7 as `RQ-1` inputs.
- Story 8.7 renders `UnretiredAssumption`, `ProhibitedCostControlPosture`, and the other closed blocker codes from the register without inventing UI-local vocabulary.

### 4.3 Four New Code-Debt Stories

Each story is owned by **Agents Runtime Maintainer** and starts as `backlog`.

#### Story 5.9: Reject Prohibited Cost-Control Postures At Readiness Recording

As a Release Operator, I want readiness recording to reject reporting-only or accepted-risk cost controls, so that production-like enablement can never pass without hard enforcement.

Acceptance summary:

1. `ReportingOnlyMonitoring` and `AcceptedLaunchRisk` remain deserializable under FR-23 but are deprecated and rejected with a typed response before mutation on every readiness/configuration path.
2. Previously recorded prohibited values surface `ProhibitedCostControlPosture`; `Unknown` or absent posture retains its distinct missing/unknown result.
3. `Quotas`, `Budgets`, and `ProviderModelLimits` are the only acceptable postures.
4. API, UI, aggregate policy, replay, and readiness projection agree; focused tests prove no prohibited posture can enable generation.

Dependencies: Stories 5.5 and 5.7. Owner: Agents Runtime Maintainer.

#### Story 5.10: Retire Prohibited Safety And Caller Policy Inputs

As a Tenant Agent Administrator, I want obsolete configuration values refused consistently, so that old clients cannot reactivate removed governance behavior.

Acceptance summary:

1. `ContentSafetyFailureHandling.BlockWithAuditableOverride` and `ApproverPolicySourceKind.Caller` remain declared/deserializable but are marked deprecated and rejected server-side with typed errors wherever supplied.
2. The Approver Policy UI offers only Conversation Facilitator, predefined Party, and tenant-role sources and labels the legacy `ConversationOwner` wire value as Conversation Facilitator.
3. Persisted legacy values are inspectable in a safe migration state but cannot activate, authorize, or mutate policy.
4. Package-consumer, aggregate, API/client, UI, replay, and negative authorization tests lock the behavior.

Dependencies: Stories 5.4 and 5.5. Owner: Agents Runtime Maintainer.

#### Story 6.8: Add Missing Fail-Closed Runtime Contract Members

As an Agents Runtime Maintainer, I want the public runtime vocabulary and context-budget evidence to match the authoritative PRD, so that unknown or structurally unavailable states fail closed without generic fallbacks.

Acceptance summary:

1. Add the runtime members required by the PRD: `AgentInteractionContextMode.Blocked`, `AgentGenerationOutcome.Indeterminate`, `NoEligibleApprover`, `RemovedInConversations`, `SourceConversationUnavailable`, `NotInvoked`, and the `Membership` gate check, plus any updated-PRD additive markers owned by Stories 5.8/6.6.
2. Record every Safe Context Budget term: model context limit, reserved output allowance, Agent Instructions, caller prompt plus system framing, and safety margin.
3. Unknown enum/reason/gate values fail closed; additive members are carried consistently through contract, aggregate, projection, API/client, UI, audit, and replay paths.
4. `UnretiredAssumption`, `DependencyNotAvailable`, `OpenDecision`, and `InsufficientEvidence` remain register vocabulary unless another PRD requirement explicitly promotes them to runtime contract members; `ProhibitedCostControlPosture` and `PayloadProtectionUnavailable` are the named runtime exceptions.

Dependencies: Stories 5.8, 6.2, 6.3, and 6.6. Owner: Agents Runtime Maintainer.

#### Story 7.7: Restore Posting-Failed Recovery And Proposal-State Parity

As an Approver, I want proposal detail and queue actions to reflect the authoritative ten-state lifecycle, so that a failed post can be resolved without misreporting a confirmed post or a terminal proposal.

Acceptance summary:

1. Proposal detail and queue share the ten-state contract. `PostingFailed` is non-terminal and exposes only the actions allowed by the PRD; `Posted` is terminal and never retryable.
2. Retry and abandon first perform the required `MessageId` existence lookup when a post may have been attempted; a found message records `Posted` plus `LateConfirmed` and rejects the requested action.
3. Retry uses the bounded audited attempt/window rules, re-runs full pre-post validation, and is suspended while the kill switch is pulled.
4. UI actions, terminal filters, pending/non-terminal indexes, state badges, status announcements, domain policy, and tests use one state/transition source.

Dependencies: Stories 6.8 and 7.1–7.6. Owner: Agents Runtime Maintainer.

### 4.4 Sprint Status

After, and only after, the approved `epics.md` edit lands, add:

```yaml
  5-9-reject-prohibited-cost-control-postures-at-readiness-recording: backlog
  5-10-retire-prohibited-safety-and-caller-policy-inputs: backlog
  6-8-add-missing-fail-closed-runtime-contract-members: backlog
  7-7-restore-posting-failed-recovery-and-proposal-state-parity: backlog
```

Preserve every existing epic/story status, retrospective row, and action item. Update comments only where the new story owns the open debt. Validate exact bidirectional parity with the 33 active story headings in `epics.md` and parse the YAML.

### 4.5 External Dependency Register

#### Story 5.3 non-conformance record

Add a **Known Consumer Non-Conformance** section with stable record `NC-5.3-PLATFORM-CATALOG-SCOPE`:

- status: Open;
- owner: Agents Runtime Maintainer;
- finding: shipped Story 5.3 uses tenant-scoped provider catalog streams/read models and does not implement the platform `system` catalog plus `TenantProviderEnablement` split required by AD-2;
- disposition: retain as migration source, freeze legacy streams, migrate idempotently with `MigratedFrom`, and close only on Story 5.3's revised verification evidence;
- dependency meaning: it does not make `EXT-PROVIDER-1` a Story 5.3 consumer, does not change that record's status, and cannot be cited as current conformance or release evidence.

#### `EXT-CONV-AI-1` scope and consumers

Extend seam 2 to include the `MessageId` existence read and seam 4 to allow either the active-Conversation count or the Conversations-side event feed defined by the PRD. Preserve all six seams.

**OLD consumers**

> 6.6, 7.4; `RQ-1`

**NEW consumers**

> 5.4, 6.2, 6.6, 6.8, 7.1–7.5, 7.7, 8.5, 8.8; `RQ-1`

Consumer rationale:

- 5.4 and 7.1–7.5/7.7 consume Facilitator/roster/existence/access resolution;
- 6.2 consumes complete content and accessibility reads;
- 6.6/6.8 and 7.4/7.7 consume membership, posting, and `MessageId` lookup or its additive runtime vocabulary;
- 8.5 consumes the active-Conversation count/event-feed denominator;
- 8.8 consumes current read access and Conversation existence for protected inspection.

Because the record remains `Uncommitted`, every newly listed consumer remains blocked from `ready-for-dev` until the exact target and compatibility command are accepted.

### 4.6 Architecture Spine

#### AD-2 and AD-30 provisioning

**OLD**

> AD-2 says `hexa` is created at tenant enablement under `Platform`; AD-30 allows a create-only `AgentSetupMutation`, but neither decision fully distinguishes provisioning from tenant configuration or states the Party-identity consequence.

**NEW**

- AD-2 states that one idempotent, create-only provisioning orchestration runs once per tenant at tenant enablement under the Platform principal, obtains/creates an AI-type Party owned by the Agents Service Principal through Parties, and creates the stable `hexa` Agent identity linked to that immutable Party.
- AD-2 states that exact replay is a no-op, a divergent second provision is a conflict, and no tenant role may create a second Agent, delete `hexa`, or replace the Party identity.
- AD-30 allowlists Platform only for the create-only provisioning command inside `AgentSetupMutation`; Platform cannot use tenant configuration commands through that allowance. A Tenant Agent Administrator may configure, activate, disable, and inspect the already-provisioned Agent but cannot invoke provisioning.

No new public role or operation-family vocabulary is introduced.

#### AD-12 kill switch

**OLD**

> Pulling the kill switch “lets `Approved` and `PostingPending` complete or fail on their own terms”.

**NEW**

> Pulling the kill switch blocks call acceptance, Provider invocation, new descriptors, edit, regeneration, and approval. An `Approved` proposal stays `Approved`; no post begins and no retry attempt is consumed. A `PostingPending` attempt already in flight is uninterruptible and completes or fails. `PostingFailed` automatic and administrative retries are suspended and their retry-window clock is paused. Awaiting proposals remain rejectable/abandonable, their `ExpiresAt` continues, and expiry while suspended records `ExpiredWhileSuspended`. The switch never system-abandons or deletes proposals/evidence. Release is by Platform Operator or Release Operator with audited justification.

Also align AD-12's trigger review with FR-28: confirmed SM-4 breach causes immediate Platform action; the seven-day operational-rate review uses the PRD thresholds/minimum sample and reports `InsufficientEvidence` below it; two consecutive SM-3/SM-7 launch-health misses require the recorded Product decision. `product-metrics` is the only trigger source.

#### Versioned ARCH-A index

Retain the now-current `architecture_assumption_index_version: 2` frontmatter and rendered `ARCH-A-INDEX-2` immediately above the Architecture Assumptions table. Version 1 was established before execution; an intervening Architecture Spine reconciliation advanced the index for its own assumption-row changes, so this follow-through must not regress it. Any later row addition, retirement, owner/condition change, or target-date change increments the version. An `RQ-1` record cites the exact index version it evaluated; `UnretiredAssumption` names the row and index version. Keep the open range semantics: every unretired row in the cited index blocks.

Update the spine frontmatter/bindings to include FR-34 where payload protection is already governed. No existing assumption is silently retired by this change.

## 5. Implementation Handoff

### Scope Classification

**Moderate — governed artifact synchronization plus backlog growth.** Product direction is settled; Product Owner/Developer coordination is required because four stories and their tracker rows are added, while the Solution Architect owns the spine correction.

### Recipients And Responsibilities

| Recipient | Responsibility |
| --- | --- |
| Product Owner / planning owner | Apply requirements/coverage updates, add four stories, and keep 33-story tracker parity |
| Solution Architect | Amend AD-2, AD-12, AD-30, FR-34 binding, and current `ARCH-A-INDEX-2` governance |
| Agents Runtime Maintainer | Own Stories 5.9, 5.10, 6.8, and 7.7 and the open Story 5.3 non-conformance |
| Conversations Maintainer | Accept or correct the expanded `EXT-CONV-AI-1` seams, consumer set, target, and compatibility command |
| Release Operator / Test Architect | Implement the RQ-1 versus launch-health split, blocker-code tests, and evidence classification |
| UX owner | No design work; verify story-level UI tests retain the existing ten-state and accessibility behavior |

### Sequencing

1. Apply register and Architecture corrections together so story readiness evaluates the intended authority.
2. Update `epics.md`, including the four stories and collateral criteria.
3. Regenerate `sprint-status.yaml` from the updated active story headings.
4. Renegotiate/create executable specs for 5.9, 5.10, 6.8, and 7.7; keep them `backlog` until their declared prior stories and external dependencies permit readiness.

### Success Criteria

1. `LR-PRODUCT-METRICS` gates only SM-1/4/5/6; NFR-9, NFR-14, and SM-5 audit completeness retain their named gate/evidence homes; SM-2/3/7 appear only under launch health.
2. The register's blocker vocabulary includes `UnretiredAssumption` and `ProhibitedCostControlPosture` with the PRD distinctions intact.
3. Active planning text contains no caller Approver source and records the ten-state FR-18 lifecycle including `Posted`.
4. FR-29 through FR-34 have inventory and coverage rows, and active epic/story manifests resolve their references.
5. `EXT-CONV-AI-1` names all six current seams and every exact consuming story; Story 5.3 has an open non-conformance record.
6. AD-2/AD-30 distinguish Platform provisioning from tenant administration; AD-12 holds `Approved` during suspension and preserves all other PRD effects.
7. The current `ARCH-A-INDEX-2` is explicit and `RQ-1` can cite the exact version evaluated.
8. `epics.md` and `sprint-status.yaml` have exact parity over 33 active stories; all four new stories are `backlog` and owned by Agents Runtime Maintainer.
9. Markdown link/reference checks, YAML parsing, story-count/parity checks, stale-vocabulary sweeps, and `git diff --check` pass after implementation.

## 6. Checklist Record

| Checklist section | Status | Finding |
| --- | --- | --- |
| 1. Trigger and context | [x] | The validation report, updated PRD, and reverified implementation-drift report provide concrete evidence. |
| 2. Epic impact | [x] | Epics 5–8 remain viable; four stories are added without a new epic or rollback. |
| 3. Artifact conflicts | [x] | PRD unchanged; register, epics, tracker, dependency register, and spine edits are identified; UX is N/A. |
| 4. Path evaluation | [x] | Direct adjustment selected; rollback and MVP reduction are not viable. |
| 5. Proposal components | [x] | Issue, impacts, old/new edits, dependencies, ownership, sequencing, risk, and success criteria are specified. |
| 6.1 Proposal review | [x] | Cross-artifact story counts, owners, metric split, and authority boundaries reviewed. |
| 6.2 Proposal accuracy | [x] | Changes are derived from the final updated PRD and the named validation/drift evidence. |
| 6.3 User approval | [x] | Administrator explicitly approved the proposal on 2026-09-09. |
| 6.4 Sprint-status update | [x] | The four approved story slugs were added as `backlog` after `epics.md`; active story parity is 33. |
| 6.5 Handoff | [x] | The completed artifacts route the four implementation stories to Agents Runtime Maintainer and retain the named architecture, dependency, release, test, and UX responsibilities. |

## Approval Gate

Administrator approved this proposal on 2026-09-09. The coordinated changes listed under `amends_if_approved` are authorized for implementation.

## Execution Record — 2026-09-09

Execution completed after explicit Administrator approval:

- verified the approved `RQ-1` SM-1/SM-4/SM-5/SM-6, NFR-9/NFR-14, audit-completeness, launch-health, and blocker-vocabulary reconciliation in `launch-readiness-register.md`;
- reconciled `epics.md` to FR-1 through FR-34, the ten-state FR-18 lifecycle, the three supported Approver sources, the Story 5.3 non-conformance reference, and all direct `EXT-CONV-AI-1` consumers;
- added Stories 5.9, 5.10, 6.8, and 7.7 with owner Agents Runtime Maintainer and matching `backlog` rows in `sprint-status.yaml`;
- aligned AD-2/AD-30 provisioning and AD-12 kill-switch behavior while preserving the intervening Architecture Spine assumption update at `ARCH-A-INDEX-2`;
- verified `NC-5.3-PLATFORM-CATALOG-SCOPE` remains open and added Story 6.8 to the dependency register's exact consumer set;
- passed YAML parsing, 33-story count and exact epic/tracker slug parity, required-owner, FR coverage, proposal-state, retired-vocabulary, metric-partition, blocker-code, dependency-consumer, provisioning, kill-switch, assumption-index, local Markdown-link, and whitespace-error checks.

No PRD or UX artifact was changed. Implementation of the four newly opened stories remains future development work under their recorded dependencies and evidence manifests.
