---
title: Sprint Change Proposal - Implementation Readiness Closure Amendment
status: approved
created: 2026-08-03
updated: 2026-08-03
mode: Batch
change_scope: major
recommended_path: hybrid-direct-adjustment-and-artifact-synchronization
project: agents
owner: Administrator
approval_required: false
approved_by: Administrator
approved_on: 2026-08-03
routed_to:
  - Product Manager
  - Solution Architect
  - Product Owner
trigger_report: implementation-readiness-report-2026-08-03.md
amends_if_approved:
  - sprint-change-proposal-2026-08-02-readiness-remediation.md
  - prds/prd-agents-2026-06-23/prd.md
  - architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
  - ux-designs/ux-agents-2026-06-23/DESIGN.md
  - ux-designs/ux-agents-2026-06-23/EXPERIENCE.md
  - epics.md
  - external-dependency-register.md
  - launch-readiness-register.md
  - sprint-status.yaml
preserves:
  - prds/prd-agents-2026-06-23/prd-addendum-2026-07-29.md
  - architecture/architecture-agents-2026-06-23-2/IMPLEMENTATION-CONVENTIONS.md
  - sprint-change-proposal-2026-08-02.md
  - epic-5-superseded-2026-08-01.md
  - completed Epics 1-4 as non-executable historical evidence
---

# Sprint Change Proposal: Implementation Readiness Closure Amendment

## 1. Issue Summary

The 2026-08-03 implementation-readiness assessment declares the Hexalith Agents planning package **NOT READY**. The product definition remains strong and complete: all 28 Functional Requirements are covered, all 14 Non-Functional Requirements remain measurable, and the PRD, UX, and architecture describe the same V1 product. The failure is execution readiness.

This proposal amends, rather than replaces, the approved `sprint-change-proposal-2026-08-02-readiness-remediation.md`. That proposal authorized dependency freezing and a 27-to-39-story reslice, but its canonical `epics.md` and `sprint-status.yaml` changes were not applied. The new assessment found additional issues that the approved reslice did not fully close: the Conversation-owned UI seam, owner-versus-Facilitator terminology, a retention forward dependency, absent verification infrastructure, current implementation non-conformance, and remaining oversized/umbrella work.

### Trigger And Evidence

- Formal trigger: `implementation-readiness-report-2026-08-03.md`, status `NOT READY`.
- External readiness: all seven declared critical external dependencies remain `Uncommitted`.
- Story graph: current Stories 6.4 and 8.1 promise outcomes completed only by later stories.
- UI integration: no committed Conversation-specific action contribution contract binds **Call hexa** to the Source Conversation. Generic FrontComposer projection customization exists, but its applicability and ownership for this action are not defined.
- Architecture conformance: module-owned AppHost, Aspire, and ServiceDefaults projects remain checked in despite AD-16; AD-10 provider capability high-water/effective-version behavior remains absent.
- Verification: all 27 active story manifests reference absent `eng/verify-story-*.ps1` scripts, and the repository has no `eng/` directory.
- Backlog hygiene: Story 7.3 is over-serialized, evidence-result vocabulary is inconsistent, two stray `+` lines remain, historical criteria are still co-located with executable work, and `sprint-status.yaml` still exposes the superseded 18-story Epic 5.

### Issue Classification

This is a failed decomposition, unresolved external-commitment, undefined integration-seam, implementation-conformance, and planning-artifact synchronization problem. It is not a product pivot, an MVP reduction, or a reason to roll back completed Epics 1-4.

## 2. Impact Analysis

### Immediate Blocker Disposition

| Immediate blocker | Proposed closure | Closure gate |
| --- | --- | --- |
| Seven external prerequisites are `Uncommitted` | Preserve the dependency freeze; update consumers after renumbering; require owners to supply immutable targets, dates, commands, and accepted evidence | No consuming story can become `ready-for-dev` until every required record is at least `Committed` |
| Story 6.4 depends on later admission work | Narrow preparation/reservation to no Provider transport; place admission, fairness, fencing, then Provider execution in prior-only order | No live invocation claim before a real admission grant exists |
| Story 8.1 delegates safe expiry to later deletion | Narrow Story 8.1 to retention deadlines, legal holds, and durable disposition eligibility; reserve completed expiry for Story 8.6 | Story 8.1 makes no erasure or completed-expiry claim |
| Conversation-owned **Call hexa** seam is undefined | Add `EXT-CONV-UI-1` and bind Story 6.11 to a committed Conversations/FrontComposer action registration contract | No separate Agents page counts as production conformance |
| Current hosting topology contradicts AD-16 | Split package boundary, platform composition, secrets/access control, and topology qualification into Stories 5.2 and 5.9-5.11 | Forbidden module-owned hosting projects are removed only against a committed replacement contract and verified platform host |
| Current Provider capability handling contradicts AD-10 | Add a provider readiness/capability progression story and consume its high-water/effective version in context, execution, regeneration, and status | Durable monotonic capability tests pass across replay and stale observations |
| All story verification scripts are absent | Make verification infrastructure the first dependency-light story and require every later story to add/update a registered executable lane | No story promotion based on a prospective command |

### Epic Impact

- Epics 1-4 remain completed, preserved, and explicitly non-executable.
- Epic 5 grows to 12 stories to establish executable verification, correct boundaries, separate readiness contracts, and qualify platform hosting.
- Epic 6 retains the approved 11-story decomposition and gains the explicit UI seam dependency.
- Epic 7 remains 6 stories; Story 7.3 no longer depends on Story 7.2.
- Current Epic 8 is split into three outcome epics:
  - Epic 8: governance operations and policy controls, 8 stories.
  - Epic 9: measurement and quality evidence production, 6 stories.
  - Epic 10: launch-evidence inspection, 1 story.
- The target active backlog becomes **44 stories across Epics 5-10**. `RQ-1` remains a release qualification activity outside the executable story backlog.

### Artifact Impact

| Artifact | Impact | Product-scope effect |
| --- | --- | --- |
| PRD | Precision edits for Facilitator-derived authority and the UI dependency | No FR, NFR, or MVP change |
| Architecture spine | Bind the Conversation action seam, dependency record, terminology, provider capability ownership, and updated story references | No architectural replacement |
| UX DESIGN/EXPERIENCE | Accurately label Facilitator-derived authority and prohibit a standalone Agents surface as the production invocation entry | No flow or IA expansion |
| Epics | Apply the approved reslice plus five additional slices and two new outcome epics | Same product outcomes, safer delivery graph |
| External dependency register | Add `EXT-CONV-UI-1`; reconcile all consumer IDs | One new external prerequisite |
| Launch readiness register | Add the Conversation UI conformance dependency/evidence link and normalized status semantics | No new launch goal |
| Sprint status | Replace obsolete executable rows with the 44-story active graph | Tracker synchronization only |
| Repository implementation | Later story work must reconcile AD-10/AD-16; this proposal does not perform implementation | No code changes in this correction step |

## 3. Recommended Path

Use a **hybrid direct adjustment and artifact synchronization** path:

1. Apply the already approved 39-story remediation as the baseline authority.
2. Amend it to 44 stories to close the newly confirmed sizing, verification, UI-seam, retention, and umbrella-epic defects.
3. Make targeted PRD/UX/architecture precision edits without changing V1 scope.
4. Preserve the external dependency freeze and add the missing UI dependency instead of inventing commitments.
5. Synchronize the dependency register, launch register, epics, historical archive, and sprint tracker atomically.
6. Rerun implementation readiness only after the first executable verification path and required first-slice external commitments exist.

### Options Considered

| Option | Verdict | Reason |
| --- | --- | --- |
| Direct adjustment plus synchronization | Recommended | Preserves approved scope and decisions while repairing execution authority |
| Roll back completed work | Rejected | Completed Epics 1-4 are evidence, not the cause of the readiness failure |
| Reduce the MVP | Rejected | Requirements coverage and product alignment are already complete; cutting scope would not commit dependencies or create verification |
| Begin implementation under waivers | Rejected | Would violate fail-closed dependency rules and allow prospective commands to masquerade as evidence |

### Effort And Risk

- Planning/document synchronization: medium.
- Subsequent implementation: high, consistent with the existing V1.
- Schedule confidence: low until external owners commit targets and integration dates.
- Residual readiness risk after planning edits: high while required dependencies remain `Uncommitted`; medium after first-slice commitments and executable verification exist.

## 4. Detailed Change Proposals

### 4.1 Prior Approved Proposal

**Before**

The approved 2026-08-02 remediation defines a 39-story target, but the canonical epics and sprint tracker still contain the pre-remediation active graph.

**After**

- Keep that proposal `approved` and historically authoritative.
- Mark this proposal as its amendment, not a superseding alternative.
- Apply only the amended 44-story target to canonical artifacts; do not first materialize an intermediate 39-story tracker.
- Preserve all dependency-freeze and no-evidence-fabrication clauses from the approved proposal.

**Rationale**

This prevents two competing replacement authorities and avoids unnecessary renumbering churn.

### 4.2 PRD Precision Edits

#### Approver Authority

**Before**

The PRD and UX use “Conversation owner,” while AD-8 implements that source as `ParticipantRole.Facilitator` because the current Conversations contract exposes no owner field.

**After**

- In the glossary, FR-7, policy examples, audit disclosure, and Decision Register, use: **Conversation Facilitator (the V1 Conversation authority)**.
- State normatively that V1 resolves this authority from `ParticipantRole.Facilitator`.
- Prohibit UI/API/evidence text from implying that a distinct Conversation owner was resolved.
- Record a future true-owner resolver as a separate post-V1 decision unless a committed Conversations contract is introduced.

#### Integration Dependencies

**Before**

PRD §8 lists seven critical dependencies. `EXT-CONV-AI-1` covers AI membership and posting but not rendering a module action in a Conversation-owned surface.

**After**

Add `EXT-CONV-UI-1` to PRD §8 with the same commitment and fail-closed rules as the existing prerequisites.

**Rationale**

These edits resolve semantic ambiguity and expose a real prerequisite without changing any V1 capability.

### 4.3 Architecture Spine Edits

#### Conversation-Owned Action Seam

**Before**

AD-6/AD-15 describe an Agents API/client and Conversation-owned **Call hexa** action but do not name a concrete registration contract, package ownership boundary, or compatibility proof.

**After**

- Add `EXT-CONV-UI-1` to the external prerequisites table.
- Define the required artifact as a versioned Conversation action contribution/registration contract that:
  - renders an Agents-supplied action inside the authorized Source Conversation surface;
  - supplies Conversation identity, caller identity, tenant, authorization context, and current availability without trusting browser-supplied authority;
  - preserves Conversation ownership of placement, navigation, and accessibility semantics;
  - allows Agents to own action behavior and status presentation without owning the Conversation shell;
  - has a committed compatibility command and Evidence Level 2 minimum before Story 6.11 is promoted.
- Treat existing generic FrontComposer projection slots as candidate infrastructure only; they do not satisfy the dependency until Conversations and FrontComposer owners accept the binding and executable proof.
- State that `/agents/conversation-call` or any separate Agents route can be a specimen/diagnostic surface only and cannot satisfy the production entry requirement.

#### Provider Capability Progression

**Before**

AD-10 requires monotonic high-water and effective provider capability versions but records current implementation gaps.

**After**

- Assign publication and persistence of provider readiness, observed version, durable high-water, and effective version to Story 5.7.
- Require Stories 6.2, 6.8, and 7.3 to consume the effective version and reject stale/decreasing observations.
- Require UI/status consumers to disclose blocked or stale truth without manufacturing readiness.

#### Hosting Boundary

**Before**

AD-16 prohibits module-owned AppHost/Aspire/ServiceDefaults ownership, while those projects remain present.

**After**

- Retain AD-16 unchanged as the final invariant.
- Map structural correction to Story 5.2, platform composition to 5.9, secrets/access control to 5.10, and production-like topology/ownership proof to 5.11.
- Do not remove the current projects until `EXT-HOST-1` supplies the committed replacement target and compatibility proof.

### 4.4 UX Design And Experience Edits

#### Authority Label

**Before**

Approver-policy controls and disclosures can say “Conversation owner.”

**After**

All policy builders, summaries, confirmation views, proposal audit details, and authorization explanations say **Conversation Facilitator (V1 Conversation authority)** when that source is selected. The public value remains stable; only the displayed and audited label is corrected.

#### Invocation Placement

**Before**

UX requires **Call hexa** inside the Source Conversation, but the technical contribution seam is unspecified.

**After**

- Add `EXT-CONV-UI-1` as a design dependency for the sole production invocation entry.
- Require the action to inherit Conversation navigation, focus order, localization, authorization presentation, responsive behavior, and error/status placement.
- Require Story 6.11 evidence from the real Conversation-owned surface.
- A standalone Agents page may support component development or diagnostics but is explicitly non-conformant as the production invocation entry.

### 4.5 Target Epic And Story Graph

#### Epic 5: Live Governed Setup And Honest Readiness — 12 Stories

| ID | Target story | Main dependency/owned outcome |
| --- | --- | --- |
| 5.1 | Establish Executable Verification Harness And Backlog Gates | Common verifier, story catalog, status vocabulary, completeness gate |
| 5.2 | Correct Domain Package And Hosting Boundary | Structural seed and AD-16 correction under committed host contract |
| 5.3 | Configure `hexa` Through Live EventStore Operations | Current 5.2 outcome |
| 5.4 | Govern Provider Models And Pricing Through Live Operations | Current 5.3 outcome |
| 5.5 | Prove Tenant Party And Approver Readiness | Current 5.4 outcome with Facilitator-derived authority |
| 5.6 | Persist Readiness Observations And Authoritative Projection | Readiness record, replay, stale/unknown behavior, projection truth |
| 5.7 | Publish Provider Readiness And Capability Progression Contracts | Provider readiness result, observed/high-water/effective version |
| 5.8 | Publish Operation Gate Matrix API/UI Parity | Shared high-impact command truth flow and API/UI parity |
| 5.9 | Compose Agents In The Platform Host And Prove Health | Platform-owned composition and health only |
| 5.10 | Qualify Secrets And Dapr Access Control | Secret references, scopes, negative access evidence |
| 5.11 | Qualify Production-Like Topology And Ownership | Multi-resource topology, evidence ingress, package ownership |
| 5.12 | Activate `hexa` Only When Setup Gates Pass | Current 5.7 outcome after all setup gates |

All Stories 5.2-10.1 depend on Story 5.1 for the common verification contract. This is a tooling dependency, not a forward product dependency.

#### Epic 6: One Safe Automatic Conversation Response — 11 Stories

| ID | Target story | Independence correction |
| --- | --- | --- |
| 6.1 | Start Durable Automatic Interaction | Durable owner, deterministic activities, replay |
| 6.2 | Use The Complete Authorized Conversation Or Block | Consumes effective provider capability version |
| 6.3 | Block Unsafe Prompt Context Or Output | Two-stage safety and negative evidence |
| 6.4 | Prepare An Attempt And Reserve Hard Cost | Ends before Provider transport; no admission grant required |
| 6.5 | Admit Work Against A Versioned Capacity Profile | Atomic admission after a prepared reservation |
| 6.6 | Schedule Admitted Work With Tenant Fairness | Weighted fairness separated from admission correctness |
| 6.7 | Fence Leases And Recover Abandoned Work | Lease/fence/recovery separated from Provider execution |
| 6.8 | Invoke The Provider And Reconcile The Reservation | Uses prior real admission/fence; handles capability high-water |
| 6.9 | Join And Post Exactly Once As `hexa` | Membership/posting under `EXT-CONV-AI-1` |
| 6.10 | Qualify Full-Path Recovery And Multi-Replica Safety | Crash/replay/concurrency evidence across completed prior slices |
| 6.11 | Call `hexa` And Follow Automatic Status Accessibly | Real Conversation-owned surface under `EXT-CONV-UI-1` |

No story invokes a Provider before 6.5-6.7 have supplied a valid admission and fencing path. Story 6.11 cannot be promoted on evidence from a standalone Agents route.

#### Epic 7: Complete Confirmation And Approval — 6 Stories

| ID | Target story | Dependency correction |
| --- | --- | --- |
| 7.1 | Create And Discover A Pending Proposal | Uses prior generation path |
| 7.2 | Edit An Immutable Proposal Version | Depends on 7.1 |
| 7.3 | Regenerate Under Fresh Gates | Depends on 7.1 and prior runtime gates, not 7.2 |
| 7.4 | Approve And Post One Selected Version | Accepts optional versions from 7.2/7.3 |
| 7.5 | Reject Or Abandon A Proposal | Independent terminal transition after 7.1 |
| 7.6 | Expire A Proposal Deterministically | Timer and race-safe transition after 7.1 |

#### Epic 8: Govern Sensitive Content And Runtime Policy — 8 Stories

| ID | Target story | Independence correction |
| --- | --- | --- |
| 8.1 | Record Retention Deadlines And Apply Legal Holds | Persists deadlines/holds and emits durable disposition eligibility; does not claim completed expiry |
| 8.2 | Export Authorized Audit Content Securely | Encrypted, authorized export |
| 8.3 | Authorize Deletion And Enforce Legal Holds | Accepts manual or retention-triggered disposition request |
| 8.4 | Erase Protected Event Payloads | Payload erasure/redaction only |
| 8.5 | Purge Named Projections And Recover | Named purge inventory, partial failure, replay/restart |
| 8.6 | Confirm Restrictive Deletion And Retention Expiry | Completes both manual deletion and due-retention outcomes with UI/evidence |
| 8.7 | Publish Versioned Content Safety Policy | Safety policy lifecycle only |
| 8.8 | Publish Versioned Tenant Budget Policy | Budget policy lifecycle only |

Shared pending-command, authorization, high-water, stale-state, and API/UI truth behavior comes from prior Story 5.8. Story 8.1 is independently valuable because it records and enforces retention intent without claiming physical expiry.

#### Epic 9: Produce Measurement And Quality Evidence — 6 Stories

| ID | Target story | Outcome |
| --- | --- | --- |
| 9.1 | Define Versioned Measurement Contracts | Formula/version contracts, observation schema, live/synthetic classification |
| 9.2 | Calculate Runtime Latency Metrics | Runtime timing ingestion and deterministic calculators |
| 9.3 | Calculate Product And Workflow Metrics | Product/workflow observation and calculators |
| 9.4 | Prove Accessible Localized And Responsive UI | NFR-13 conformance only |
| 9.5 | Qualify UI Tenant And Operation Isolation | Cross-tenant, cross-operation, stale-response, and multi-tab evidence |
| 9.6 | Measure Browser Interaction Performance | NFR-14 monotonic production-like timings only |

#### Epic 10: Inspect Launch Evidence And Blockers — 1 Story

| ID | Target story | Outcome |
| --- | --- | --- |
| 10.1 | Inspect Launch Evidence And Blockers | Operator view over evidence and blockers; refuses to manufacture evidence or execute `RQ-1` |

### 4.6 Verification Bootstrap And Evidence Semantics

#### Story 5.1 Bootstrap

**Before**

Each current story names a unique script under `eng/`, but none of the 27 files or the directory exists. The first current story is itself blocked by `EXT-HOST-1`, so there is no executable starting point.

**After**

Story 5.1 is dependency-light and creates:

- `eng/verify-story.ps1`, a common deterministic runner accepting `-Story <id>`;
- a versioned machine-readable catalog covering all 44 active story IDs;
- schema validation that fails on an unknown story, missing lane, missing artifact target, or missing negative-evidence target;
- a catalog audit that prevents an active story from declaring a nonexistent command;
- a status model separating readiness from execution evidence.

Story 5.1 uses existing repository commands as its bootstrap acceptance path before the new runner can be authoritative: solution restore/build plus focused existing test-project execution. At completion, `pwsh ./eng/verify-story.ps1 -Story 5.1` must run successfully and become the canonical command.

Every later story must add or update its registered lane and tests before completion. A catalog entry may begin blocked, but its command path must exist and fail honestly when prerequisites are absent.

#### Normalized Vocabulary

Replace ambiguous free-text `Result` values with separate fields:

- `ReadinessStatus`: `Blocked`, `Candidate`, `ReadyForDev`, `InProgress`, `Review`, `Done`.
- `VerificationStatus`: `NotRun`, `Passed`, `Failed`.
- `BlockedBy`: explicit dependency IDs or artifact gaps.
- `VerifiedAt`, `VerificationCommand`, and `EvidenceRefs`: populated only from actual execution/evidence.

`Blocked` is not a test result, and `NotRun` is not proof of readiness.

### 4.7 External Dependency Register

#### New Record

Add the following record without inventing a commitment:

| Field | Value |
| --- | --- |
| Dependency ID | `EXT-CONV-UI-1` |
| Capability | Conversation-owned action contribution/registration seam for **Call hexa** |
| Owner | Hexalith.Conversations maintainer with Hexalith.FrontComposer maintainer |
| Repository | Primary: `Hexalith.Conversations`; supporting: `Hexalith.FrontComposer` |
| Immutable target | `TBD` |
| Required artifact | Versioned action contract, server-trusted context/authorization binding, package registration, accessible placement, compatibility tests |
| Integration date | `TBD` |
| Compatibility command | `TBD` |
| Evidence level | Level 2 minimum before Story 6.11 promotion |
| Status | `Uncommitted` |
| Consumers | Story 6.11; `RQ-1`; launch UI-conformance evidence |

#### Existing Records

Preserve all seven existing dependencies as `Uncommitted` until their owners provide and accept all required fields. Reconcile consumer IDs to the 44-story graph. At minimum:

- `EXT-HOST-1`: Stories 5.2, 5.9, 5.11 and downstream host qualification.
- `EXT-PROVIDER-1`: Stories 5.4, 5.7, 6.4-6.8, 7.3, and qualification.
- `EXT-CONV-AI-1`: Stories 6.9, 7.4, and qualification.
- `EXT-SAFETY-1`: Stories 6.3, 6.8, 7.3, 7.4, 8.7, and qualification.
- `EXT-TOKEN-1`: Stories 6.2, 6.4, 6.8, 7.3, and qualification.
- `EXT-SECRETS-1`: Stories 5.10, 6.8, 8.2, 8.4-8.6, and qualification.
- `EXT-TOPOLOGY-1`: Stories 5.9-5.11, 6.1, 6.5-6.7, 6.10, 8.4-8.6, 9.1-9.6, 10.1, and qualification.

The exact consumer matrix must be validated during canonical synchronization; omission never weakens a story's declared prerequisite.

### 4.8 Sprint Status Synchronization

After approval and canonical epic synchronization:

- Preserve completed Epic 1-4 status and retrospective/evidence entries.
- Remove the superseded 18-story Epic 5 executable rows.
- Add Epic 5-10 rows and the following 44 story slugs, initially `backlog` unless an existing verified state legitimately maps forward:

```yaml
5-1-establish-executable-verification-harness-and-backlog-gates: backlog
5-2-correct-domain-package-and-hosting-boundary: backlog
5-3-configure-hexa-through-live-eventstore-operations: backlog
5-4-govern-provider-models-and-pricing-through-live-operations: backlog
5-5-prove-tenant-party-and-approver-readiness: backlog
5-6-persist-readiness-observations-and-authoritative-projection: backlog
5-7-publish-provider-readiness-and-capability-progression-contracts: backlog
5-8-publish-operation-gate-matrix-api-ui-parity: backlog
5-9-compose-agents-in-the-platform-host-and-prove-health: backlog
5-10-qualify-secrets-and-dapr-access-control: backlog
5-11-qualify-production-like-topology-and-ownership: backlog
5-12-activate-hexa-only-when-setup-gates-pass: backlog
6-1-start-durable-automatic-interaction: backlog
6-2-use-the-complete-authorized-conversation-or-block: backlog
6-3-block-unsafe-prompt-context-or-output: backlog
6-4-prepare-an-attempt-and-reserve-hard-cost: backlog
6-5-admit-work-against-a-versioned-capacity-profile: backlog
6-6-schedule-admitted-work-with-tenant-fairness: backlog
6-7-fence-leases-and-recover-abandoned-work: backlog
6-8-invoke-the-provider-and-reconcile-the-reservation: backlog
6-9-join-and-post-exactly-once-as-hexa: backlog
6-10-qualify-full-path-recovery-and-multi-replica-safety: backlog
6-11-call-hexa-and-follow-automatic-status-accessibly: backlog
7-1-create-and-discover-a-pending-proposal: backlog
7-2-edit-an-immutable-proposal-version: backlog
7-3-regenerate-under-fresh-gates: backlog
7-4-approve-and-post-one-selected-version: backlog
7-5-reject-or-abandon-a-proposal: backlog
7-6-expire-a-proposal-deterministically: backlog
8-1-record-retention-deadlines-and-apply-legal-holds: backlog
8-2-export-authorized-audit-content-securely: backlog
8-3-authorize-deletion-and-enforce-legal-holds: backlog
8-4-erase-protected-event-payloads: backlog
8-5-purge-named-projections-and-recover: backlog
8-6-confirm-restrictive-deletion-and-retention-expiry: backlog
8-7-publish-versioned-content-safety-policy: backlog
8-8-publish-versioned-tenant-budget-policy: backlog
9-1-define-versioned-measurement-contracts: backlog
9-2-calculate-runtime-latency-metrics: backlog
9-3-calculate-product-and-workflow-metrics: backlog
9-4-prove-accessible-localized-and-responsive-ui: backlog
9-5-qualify-ui-tenant-and-operation-isolation: backlog
9-6-measure-browser-interaction-performance: backlog
10-1-inspect-launch-evidence-and-blockers: backlog
```

Do not add `RQ-1` as a story row.

### 4.9 Historical Authority And Backlog Hygiene

**Before**

Completed historical Epics 1-4 and current replacement Epics 5-8 coexist in `epics.md`; the replacement header limits authority, but superseded criteria remain easy for an implementation agent to consume accidentally.

**After**

- Move the full completed Epic 1-4 story bodies to `epics-completed-1-4.md`.
- Add frontmatter `status: completed-historical` and `executable: false`.
- Preserve all historical acceptance/evidence text verbatim except navigation links and an explicit supersession banner.
- Keep a concise non-executable history/link section in canonical `epics.md`.
- Remove the two stray `+` lines.
- Normalize FR/NFR references and the new readiness/verification vocabulary.
- Keep `epic-5-superseded-2026-08-01.md` unchanged as historical evidence.

### 4.10 Implementation Non-Conformance

This proposal records but does not directly edit code. Subsequent implementation stories must:

- reconcile the checked-in module-owned AppHost/Aspire/ServiceDefaults projects under Stories 5.2 and 5.9-5.11;
- implement AD-10 durable capability high-water/effective-version behavior under Stories 5.7, 6.2, 6.8, and 7.3;
- replace or explicitly demote the separate Agents conversation-call surface once the real Conversation-owned seam is committed and Story 6.11 executes;
- preserve current user changes and remove hosting artifacts only when the committed platform replacement is verifiably available.

## 5. Implementation Plan

### Phase A — Canonical Planning Synchronization

Owner: Product Manager + Solution Architect + Product Owner

1. Approve this amendment.
2. Apply PRD, architecture, UX, epic, dependency, launch-register, history, and sprint-status edits atomically.
3. Validate 28/28 FR coverage and NFR-1 through NFR-14 coverage against the 44-story graph.
4. Validate no forward story dependency and no executable historical story leakage.
5. Record all eight dependency records with complete fields; keep unknown commitments `Uncommitted`.

### Phase B — Establish The Executable Starting Point

Owner: Engineering + Test Architecture

1. Create and execute Story 5.1 using existing bootstrap build/test commands.
2. Establish the common `eng/` runner, catalog, schema, and negative completeness tests.
3. Run `pwsh ./eng/verify-story.ps1 -Story 5.1` before promoting the story.
4. Do not promote Story 5.2 until `EXT-HOST-1` is `Committed` and its compatibility command has passed.

### Phase C — Commit External Prerequisites

Owner: named maintainers in the dependency register

1. Supply immutable targets, integration dates, commands, Evidence Levels, and accepted compatibility evidence.
2. Resolve `EXT-CONV-UI-1` jointly with Conversations and FrontComposer ownership.
3. Update story readiness mechanically from the dependency graph; do not use schedule pressure as a waiver.

### Phase D — Implement In Prior-Only Order

1. Complete Epic 5 readiness/hosting foundations.
2. Implement Epic 6 in the 6.4 → 6.5 → 6.6 → 6.7 → 6.8 order for cost/admission/fairness/fencing/provider transport.
3. Execute Story 6.11 only through the committed Conversation-owned seam.
4. Execute Epic 7 transitions with 7.2 and 7.3 parallelizable after 7.1.
5. Execute Epic 8 deletion/expiry as 8.1 → 8.3 → 8.4 → 8.5 → 8.6; other governance slices may proceed when their dependencies permit.
6. Produce measurement evidence in Epic 9, then inspect it in Epic 10.

### Phase E — Reassess Readiness

Rerun implementation readiness after canonical synchronization and before broad feature implementation. The assessment may return `READY` only when:

- there are zero semantic forward dependencies;
- all active verification commands resolve to executable lanes;
- the first implementation slice has every required external dependency `Committed` with passing compatibility evidence;
- `EXT-CONV-UI-1` has an owned contract before Story 6.11 promotion;
- sprint status exposes only the active 44-story graph;
- historical criteria are mechanically non-executable;
- current implementation gaps are explicitly owned by prior-ordered stories and no artifact claims they are already conformant.

## 6. Change Navigation Checklist Results

| Checklist item | Status | Result |
| --- | --- | --- |
| 1.1 Triggering story identified | Done | Readiness assessment traced directly to Stories 6.4, 8.1, 6.7/target 6.11 and systemic artifacts |
| 1.2 Core problem defined | Done | Decomposition, commitment, integration, conformance, verification, synchronization |
| 1.3 Evidence gathered | Done | PRD, architecture, UX, epics, registers, tracker, prior proposal, and repository state inspected |
| 2.1 Current epic impact | Action required | Epics 6 and 8 fail independence/cohesion |
| 2.2 Future epic impact | Action required | Apply 44-story graph across Epics 5-10 |
| 2.3 Epic invalidation | Done | No product epic outcome is invalidated |
| 2.4 New/obsolete epic need | Action required | Add Epics 9-10; no product outcome becomes obsolete |
| 2.5 Sequence/priority impact | Action required | Prior-only runtime, retention, verification, and qualification order specified |
| 3.1 PRD conflict | Action required | Precision terminology and dependency edit; MVP unchanged |
| 3.2 Architecture conflict | Action required | Bind UI seam and map AD-10/AD-16 ownership |
| 3.3 UX conflict | Action required | Facilitator label and real Conversation placement |
| 3.4 Other artifacts | Action required | Dependency/launch registers, history, verifier catalog, sprint tracker |
| 4.1 Direct adjustment viability | Viable | Medium planning effort; high implementation effort; preserves scope |
| 4.2 Rollback viability | Not viable | Completed evidence is unaffected |
| 4.3 MVP re-scope viability | Not selected | Does not solve readiness failure |
| 4.4 Recommended path selected | Done | Hybrid direct adjustment and synchronization |
| 5.1 Issue summary | Done | Section 1 |
| 5.2 Epic/story impact | Done | Sections 2 and 4.5 |
| 5.3 Artifact conflict summary | Done | Sections 4.2-4.10 |
| 5.4 Recommended path/rationale | Done | Section 3 |
| 5.5 MVP impact/action plan | Done | No MVP change; Sections 2 and 5 |
| 5.6 Handoff plan | Done | Section 7 |
| 6.1 Checklist review | Done | This table |
| 6.2 Proposal accuracy | Done | Final proposal validated against current artifacts and approved |
| 6.3 Explicit approval | Done | Administrator explicitly approved the complete proposal on 2026-08-03 |
| 6.4 Approved proposal finalized | Done | Approval metadata and execution log recorded |
| 6.5 Handoff confirmation | Done | Major-scope package routed as defined below |

## 7. Handoff And Ownership

Approved handoff:

- **Product Manager:** apply PRD precision edits, preserve scope/coverage, reconcile outcome epics.
- **Solution Architect:** update AD-6/AD-8/AD-10/AD-15/AD-16 references, define `EXT-CONV-UI-1`, and validate the dependency graph.
- **UX Designer:** update Facilitator-derived authority language and the Conversation-owned action seam requirements.
- **Product Owner / Scrum Master:** materialize the 44-story graph, archive completed historical stories, and synchronize sprint status.
- **Test Architect:** define Story 5.1 verifier/catalog acceptance and normalized evidence semantics.
- **External dependency owners:** commit and prove their records; no planning role may fabricate these commitments.
- **Engineering:** begin only with an actually ready slice, execute exact verification commands, and own AD-10/AD-16 conformance through the assigned stories.

## 8. Success Criteria

This course correction is successful when:

1. Canonical artifacts expose one active, prior-only 44-story graph.
2. Epics 8-10 each have one coherent operator outcome.
3. Story 6.4 stops before Provider transport and Story 8.1 stops before completed expiry.
4. `EXT-CONV-UI-1` makes the production **Call hexa** placement an explicit owned prerequisite.
5. Facilitator-derived authority is named consistently in PRD, UX, architecture, APIs, UI, and evidence.
6. Every active story has an executable registered verification lane, and actual verification is distinct from readiness.
7. External dependency status is complete, honest, and mechanically gates consumers.
8. Sprint status contains no superseded executable rows.
9. Historical criteria are preserved but mechanically excluded from implementation authority.
10. A rerun of implementation readiness finds no critical story-graph, integration-seam, verification, or tracker blocker.

## 9. Approval Record

- Review mode: Batch.
- Complete-proposal review: continued by Administrator.
- Implementation approval: explicitly approved by Administrator on 2026-08-03.
- Scope classification: Major.
- Route: Product Manager and Solution Architect for replan and architecture acceptance; Product Owner for backlog and sprint-status synchronization.
- Proposal state: Approved; no canonical PRD, architecture, UX, epic, dependency-register, launch-register, sprint-status, or implementation edits were applied by this workflow.
- Prior authority: the approved `sprint-change-proposal-2026-08-02-readiness-remediation.md` remains intact and is amended by this proposal.
- Authorization boundary: approval authorizes the planning-artifact changes and handoff described here. It does **not** authorize fabricated external commitments, evidence claims, destructive removal of hosting projects before their replacement is committed, broad feature implementation, or execution of `RQ-1`.
- Developer agents remain gated by the Definition of Ready, executable verification, and the external dependency register.

## 10. Handoff And Workflow Execution Log

- 2026-08-03 — Correct Course activated in Batch mode for `implementation-readiness-report-2026-08-03.md`.
- 2026-08-03 — Repository instructions, workflow customization, persistent project contexts, PRD, architecture, UX, active epics, registers, tracker, prior approved proposal, readiness report, and current implementation indicators were assessed.
- 2026-08-03 — The repository check confirmed generic FrontComposer projection customization but no committed Conversation-specific action registration contract; `EXT-CONV-UI-1` was therefore proposed without inventing a commitment.
- 2026-08-03 — Checklist Sections 1-4 completed: trigger/context, epic impact, artifact conflict, and path evaluation.
- 2026-08-03 — Hybrid direct adjustment and artifact synchronization selected: preserve the approved remediation, amend the target to 44 stories across Epics 5-10, and keep V1 scope unchanged.
- 2026-08-03 — Batch proposal compiled with before/after artifact edits, exact tracker rows, dependency gates, verification bootstrap, success criteria, and ownership.
- 2026-08-03 — Administrator continued the complete-proposal review and explicitly approved implementation.
- 2026-08-03 — Major-scope handoff routed to Product Manager and Solution Architect, with Product Owner ownership of canonical backlog and sprint-status synchronization.

### Handoff Completion

The handoff package contains the approved Sprint Change Proposal, artifact-specific before/after edits, the exact 44-story active map, eight dependency records/gates, verification-bootstrap requirements, tracker rows, implementation sequencing, and success criteria. The first planning checkpoint is joint Product Manager/Solution Architect acceptance of the amendment. The first executable implementation checkpoint is Story 5.1; Story 5.2 remains blocked until `EXT-HOST-1` is committed and its compatibility command passes.
