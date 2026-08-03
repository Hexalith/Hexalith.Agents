---
title: Sprint Change Proposal - Implementation Readiness Remediation
status: approved
created: 2026-08-02
updated: 2026-08-03
mode: Batch
change_scope: major
recommended_path: direct-adjustment-with-dependency-freeze-and-story-reslicing
project: agents
owner: Administrator
approval_required: false
approved_by: Administrator
approved_on: 2026-08-02
revalidated_by: Administrator
revalidated_on: 2026-08-03
routed_to:
  - Product Manager
  - Solution Architect
  - Product Owner
trigger_report: implementation-readiness-report-2026-08-02.md
amends_if_approved:
  - sprint-change-proposal-2026-08-02.md
  - epics.md
  - external-dependency-register.md
  - ARCHITECTURE-SPINE.md
  - sprint-status.yaml
preserves:
  - prds/prd-agents-2026-06-23/prd.md
  - architecture/architecture-agents-2026-06-23-2/IMPLEMENTATION-CONVENTIONS.md
  - ux-designs/ux-agents-2026-06-23/DESIGN.md
  - ux-designs/ux-agents-2026-06-23/EXPERIENCE.md
  - launch-readiness-register.md
  - sprint-change-proposal-2026-08-02.md
  - epic-5-superseded-2026-08-01.md
  - completed Epics 1-4
---

# Sprint Change Proposal: Implementation Readiness Remediation

## 1. Issue Summary

The 2026-08-02 implementation-readiness assessment declared the Hexalith Agents planning package **NOT READY** for Phase 4 implementation. Requirements coverage is complete: all 28 Functional Requirements are covered, and the PRD, Architecture, and UX spines are materially aligned. The blockers are delivery commitments, story independence, and executable-backlog synchronization.

Three findings prevent implementation from starting:

1. All seven critical external dependencies remain `Uncommitted`; every consuming story is therefore blocked from `ready-for-dev` by PRD §8 and FR-21.
2. Stories 6.4 and 6.5 contain a latent dependency cycle. Story 6.4 claims a Provider invocation by using a trusted admission grant supplied only by future Story 6.5, while Story 6.5 depends on the reserved attempt produced by Story 6.4.
3. `_bmad-output/implementation-artifacts/sprint-status.yaml` still exposes the superseded 18-story Epic 5 as executable work instead of the active replacement Epics 5-8.

The assessment also identified six oversized stories, three document/tooling concerns, and four architecture/UX execution-readiness warnings. These do not change V1 product scope, but they must be resolved before the active story graph is safe for implementation agents.

### Trigger And Evidence

- Formal trigger: `implementation-readiness-report-2026-08-02.md`, status `NOT_READY`.
- Requirements evidence: 28/28 FRs covered; NFR-1 through NFR-14 remain represented.
- Dependency evidence: all records in `external-dependency-register.md` are `Uncommitted`, with `TBD` targets, dates, and executable commands.
- Story evidence: the dependency text and demonstrable outcomes of Stories 6.4 and 6.5 contradict their declared `Forward dependencies: None` statements.
- Sizing evidence: Stories 5.6, 6.1, 6.5, 8.3, 8.5, and 8.6 span multiple independently testable subsystems or qualification lanes.
- Tracker evidence: `sprint-status.yaml` still contains the former 18-story Epic 5 rows.
- Artifact evidence: the PRD, Architecture, UX, dependency register, readiness register, and active Epics 5-8 otherwise express the same V1 product and governance model.

### Issue Classification

This is a failed story decomposition, unresolved external-commitment, and implementation-artifact synchronization problem. It is not a product pivot, missing requirement, architecture replacement, UX redesign, or reason to roll back completed work.

## 2. Impact Analysis

### Epic Impact

- Epics 1-4 remain completed historical evidence and non-executable authority.
- Active Epics 5-8 remain the correct four outcome epics; no epic is removed or added.
- Epic 5 grows from 7 to 8 stories by separating platform composition/health from secrets and Dapr access-control qualification.
- Epic 6 grows from 7 to 11 stories by separating durable workflow ownership, prepared attempt/cost reservation, capacity admission, fairness, fencing/recovery, Provider invocation/reconciliation, full-path recovery, posting, and participant UX.
- Epic 7 remains 6 stories; only dependency references are reconciled.
- Epic 8 grows from 7 to 14 stories by separating deletion stages, measurement-contract foundations, runtime and product metrics, NFR-13 conformance, UI isolation, and NFR-14 performance.
- The active backlog changes from 27 to 39 stories. `RQ-1` remains outside the executable backlog.

### Story Impact

| Existing story | Disposition | Replacement outcome |
| --- | --- | --- |
| 5.6 | Split | 5.6 platform composition/health; 5.7 secrets and access control |
| 5.7 | Renumber | 5.8 activation/callability |
| 6.1 | Split | 6.1 durable owner/replay; 6.10 full-path recovery qualification |
| 6.4 and 6.5 | Replace | 6.4 prepare/reserve; 6.5 atomic admission; 6.6 fairness; 6.7 fencing/lease recovery; 6.8 Provider invocation/reconciliation |
| 6.6 | Renumber | 6.9 membership and exactly-once posting |
| 6.7 | Renumber | 6.11 participant UI/status |
| 8.3 | Split | 8.3 authorization/hold gate; 8.4 payload erasure; 8.5 projection purge/recovery; 8.6 restrictive completion/UI |
| 8.4 | Renumber | 8.7 policy operations |
| 8.5 | Split | 8.8 measurement contracts; 8.9 runtime latency; 8.10 product/workflow metrics |
| 8.6 | Split | 8.11 NFR-13 conformance; 8.12 UI/API isolation; 8.13 NFR-14 browser performance |
| 8.7 | Renumber | 8.14 launch-evidence inspection and completeness gate |

### Artifact Conflicts

| Artifact | Impact | Required disposition |
| --- | --- | --- |
| PRD | No conflict | Preserve unchanged; MVP and FR/NFR inventory remain authoritative |
| Epics | Cycle, oversized stories, count mismatch, stray `+` lines, inconsistent requirement notation, historical automation risk | Apply the 39-story graph, dependencies, evidence ownership, active-backlog metadata, and cleanup |
| Architecture | Core decisions remain valid; story references and direct dependency-consumer mappings become stale | Update Story 5.6/5.7/5.8, Epic 6, and Epic 8 references only; preserve AD-1 through AD-26 semantics |
| UX | No semantic conflict | Preserve both UX spines unchanged; new stories divide existing UX obligations without changing them |
| External dependency register | All commitments unresolved; consumer story numbers will change | Owners complete the existing nine fields; update exact consumer IDs after reslicing |
| Launch readiness register | Correct authority and initial restrictive state | Preserve unchanged; continue showing `InsufficientEvidence` until real observations exist |
| Sprint status | Tracks superseded 18-story plan | Replace forward rows with the approved 39-story graph, all initially `backlog` |
| Existing 2026-08-02 proposal | Approved authority for the four-epic replan, but its 27-story count is superseded by this assessment | Preserve and amend only its story-count, story-map, dependency-map, and tracker handoff clauses |

### Technical And Delivery Impact

- No source code, EventStore history, completed story evidence, or V1 contract is rolled back.
- No external dependency is inferred as committed by this proposal. Named owners must provide and accept immutable targets, dates, compatibility contracts, executable verification commands, evidence levels, and statuses.
- No story may move to `ready-for-dev` while any declared dependency remains `Uncommitted`, has a `TBD` target/date/command, or fails its compatibility command.
- Live UI and release gates remain `InsufficientEvidence`; deterministic fixtures cannot be promoted to Level 4/5 attainment.

## 3. Recommended Approach

### Selected Path

Use a **Direct Adjustment with a dependency freeze and story reslicing**:

1. Preserve the PRD, Architecture decisions, UX spines, launch-readiness register, prior approved proposal, and completed Epics 1-4.
2. Freeze story creation and development from the current `sprint-status.yaml`.
3. Obtain explicit commitments for the seven external dependencies; prioritize `EXT-HOST-1`, because Story 5.1 is the first active story.
4. Replace the active 27-story graph with the 39-story graph below.
5. Update dependency consumer mappings and story evidence manifests.
6. Regenerate sprint status from corrected Epics 5-8 only after this proposal and the epic edits are approved.
7. Rerun implementation readiness. No story becomes `ready-for-dev` until the rerun passes and its own dependency gate is satisfied.

### Alternatives Considered

| Option | Verdict | Rationale |
| --- | --- | --- |
| Keep 27 stories and edit only dependency wording | Rejected | Leaves six epic-sized stories and cannot make 6.4/6.5 independently completable |
| Combine 6.4 and 6.5 into one story | Rejected | Removes the formal cycle but creates a larger cost/capacity/Provider subsystem story |
| Roll back completed Epics 1-4 or the approved replan | Rejected | Historical evidence and the four outcome epics remain valid |
| Reduce MVP scope | Rejected | Scope reduction does not create external commitments or repair tracker authority |
| Reslice within Epics 5-8 | Recommended | Preserves product intent while giving every implementation and qualification concern a bounded owner |

### Effort, Risk, And Timeline

- **Scope:** Major planning correction; no product-scope change.
- **Planning effort:** Medium.
- **Implementation effort:** High and materially unchanged; the work is redistributed, not expanded semantically.
- **Residual risk after approval:** Medium, dominated by external commitment dates and cross-system evidence availability.
- **Calendar impact:** cannot be estimated responsibly until all seven dependency records reach at least `Committed`.

## 4. Detailed Change Proposals

### 4.1 External Dependency Commitments

**Artifact:** `external-dependency-register.md`

**OLD:**

> All seven records are `Uncommitted`; targets, dates, and executable verification commands remain `TBD`.

**NEW:**

Retain the existing schema and status semantics. Require each named owner to replace every `TBD` commitment field and explicitly accept the record. Update direct consuming stories to:

| Dependency | Revised direct consumers |
| --- | --- |
| `EXT-CONV-AI-1` | 6.9, 7.4; `RQ-1` |
| `EXT-HOST-1` | 5.1, 5.6, 5.7; `RQ-1` |
| `EXT-PROVIDER-1` | 5.3, 5.5, 6.4 at `Committed`, 6.8 and 7.3 at `Available`; `RQ-1` |
| `EXT-SAFETY-1` | 6.3, 7.3, 7.4; `RQ-1` |
| `EXT-TOKEN-1` | 6.2, 7.3; `RQ-1` |
| `EXT-SECRETS-1` | 5.7, 6.8, 8.2, 8.4; `RQ-1` |
| `EXT-TOPOLOGY-1` | 5.6, 5.7, 6.1, 6.5-6.7, 6.10, 8.4-8.6, 8.9-8.14; `RQ-1` |

`Committed` permits story readiness and contract work only. Any live execution of a seam still requires `Available` plus the exact passing compatibility command.

**Rationale:** This resolves the primary readiness gate without inventing delivery commitments or allowing a local checkout to stand in for owner acceptance.

### 4.2 Epic 5 - Split Platform Composition From Security Qualification

**Artifact:** `epics.md`

**OLD:**

> Story 5.6 composes the complete platform topology, validates secret configured/denied/missing/rotation behavior, proves Dapr access-control isolation, captures evidence, and audits repository/package ownership. Story 5.7 then activates `hexa`.

**NEW:**

| ID | Story | Primary demonstrable outcome | Dependencies |
| --- | --- | --- | --- |
| 5.6 | Compose Agents In The Platform Host And Prove Health | Exact platform fixture starts Agents DomainService/UI, EventStore, dependencies, Workflow, readiness, capacity seam, telemetry, identity, and evidence ingress; health/topology evidence is immutable and no module-owned host remains | 5.1, 5.5; `EXT-HOST-1` and `EXT-TOPOLOGY-1` Available |
| 5.7 | Qualify Secrets And Dapr Access Control | Configured, denied, missing, and rotated secrets plus declared app IDs/routes/topics are proven live with poison sweeps and focused cross-tenant/unauthenticated denial | 5.6; `EXT-SECRETS-1` Available |
| 5.8 | Activate hexa Only When Setup Gates Pass | Existing 5.7 outcome, renumbered; consumes the completed composition and security qualification | 5.2-5.7; profile-specific dependency status |

**Rationale:** Platform boot/health and security/secret qualification have different owners, failure modes, and evidence. The end-to-end checkpoint remains explicit in 5.7.

### 4.3 Epic 6 - Remove The Cycle And Separate Runtime Subsystems

**Artifact:** `epics.md`

**OLD:**

> Story 6.4 invokes the Provider under an injected admission grant while stating that live grants cannot exist until Story 6.5. Story 6.5 depends on the reserved attempt from Story 6.4 and owns profile, admission, queues, fairness, fencing, crash/cancel/expiry recovery, and saturation evidence. Story 6.1 also owns both workflow construction and complete production-like recovery qualification.

**NEW:**

| ID | Story | Primary demonstrable outcome | Dependencies |
| --- | --- | --- | --- |
| 6.1 | Start A Durable Automatic Interaction | One accepted call creates one EventStore interaction and one deterministic Dapr Workflow owner; activity replay is idempotent, but the 15-minute full-path recovery claim is deferred | Epic 5 through 5.8; `EXT-TOPOLOGY-1` Available for live Workflow evidence |
| 6.2 | Use The Complete Authorized Conversation Or Block | Existing outcome unchanged | 6.1; `EXT-TOKEN-1` Available |
| 6.3 | Block Unsafe Prompt Context Or Output | Existing outcome unchanged | 6.1-6.2; `EXT-SAFETY-1` Available |
| 6.4 | Prepare An Attempt And Reserve Maximum Cost | One immutable prepared descriptor and atomic maximum-cost reservation are recorded; the story ends before capacity admission and Provider transport | 6.1-6.3; 5.3/5.5; `EXT-PROVIDER-1` at least Committed |
| 6.5 | Publish Capacity Profiles And Admit Atomically | A versioned numeric profile and one shared linearizable acquire yield admitted, queued, or rejected without fairness or crash-recovery claims | 6.4; `EXT-TOPOLOGY-1` Available |
| 6.6 | Schedule Queues Fairly Across Tenants | Complete weighted cycles prove exact `WeightedRoundRobinV1` share, stable within-tenant order, retry position reuse, bounded queues, and no starvation | 6.5; `EXT-TOPOLOGY-1` Available |
| 6.7 | Fence And Recover Capacity Leases | Monotonic fences, `BeginInvocation`, reclamation, cancel, expiry, allocator restart, and stale/forged-fence denial are proven without invoking a live Provider | 6.5-6.6; `EXT-TOPOLOGY-1` Available |
| 6.8 | Invoke Provider And Reconcile Cost | After 6.4-6.7, one authorized/fenced attempt invokes the committed Provider idempotently, resolves crash outcome, and reconciles/releases cost exactly once | 6.4-6.7; `EXT-PROVIDER-1` and `EXT-SECRETS-1` Available |
| 6.9 | Join And Post Exactly Once As hexa | Existing 6.6 outcome, renumbered | 5.4, 6.3-6.8; `EXT-CONV-AI-1` Available |
| 6.10 | Qualify Automatic Interaction Recovery | Frozen full-path cohorts across context, safety, reservation, admission, Provider, posting, and timers prove EventStore RPO 0, no duplicate effect, unchanged terminal decisions, and recovery within 15 monotonic minutes | 6.1-6.9; `EXT-TOPOLOGY-1` and every executed seam Available |
| 6.11 | Call hexa And Follow Automatic Status Accessibly | Existing 6.7 participant UX outcome, renumbered; functional UI can proceed after 6.9 while 6.10 qualifies recovery | 6.1-6.9 |

The new dependency chain is acyclic:

```text
prepared attempt + reservation
  -> atomic admission
  -> fairness
  -> fencing/lease recovery
  -> Provider invocation + reconciliation
  -> membership + posting
  -> full-path recovery qualification
```

**Rationale:** Every slice now ends with an independently executable outcome. No fixture-supplied capability is represented as a live product dependency.

### 4.4 Epic 7 - Preserve Outcomes And Reconcile Dependencies

**Artifact:** `epics.md`

**OLD:** Story 7.1 depends on Stories 6.1-6.5; regeneration and posting refer to the old cost/capacity/posting story numbers.

**NEW:** Preserve all six Story 7 outcomes and IDs. Update dependencies and evidence references:

- 7.1 consumes 6.1-6.8 for successful confirmation-mode generation and does not require 6.9 posting.
- 7.3 consumes the new 6.4-6.8 preparation/admission/Provider path.
- 7.4 consumes `EXT-CONV-AI-1` directly and retains its current ID.
- 7.6 continues to consume 6.1 and 7.1 for durable expiry.

**Rationale:** Epic 7 already consists of independently bounded proposal transitions; renumbering it would create unnecessary churn.

### 4.5 Epic 8 - Separate Governance, Metrics, And UI Qualification

**Artifact:** `epics.md`

**OLD:**

> Story 8.3 combines deletion eligibility, holds, cryptographic erasure, projection purge, recovery, UI, and forensic proof. Story 8.5 owns every runtime and product metric family. Story 8.6 owns the complete NFR-13/NFR-14 route, localization, viewport, isolation, timing, correlation, and sampling matrix.

**NEW:**

| ID | Story | Primary demonstrable outcome | Dependencies |
| --- | --- | --- | --- |
| 8.1 | Retain Sensitive Content And Apply Legal Holds | Existing outcome unchanged | 5.2, 7.6 |
| 8.2 | Export Authorized Audit Content Securely | Existing outcome unchanged | 8.1; `EXT-SECRETS-1` Available |
| 8.3 | Authorize Deletion And Enforce Legal Holds | One deterministic request records exact scope/revisions only after current authorization, eligibility, and no-hold proof; no erasure occurs | 8.1 |
| 8.4 | Erase Protected Event Payloads | Protected payloads are cryptographically erased/redacted without rewriting EventStore history; a safe non-content tombstone is produced | 8.3; `EXT-SECRETS-1` and `EXT-TOPOLOGY-1` Available |
| 8.5 | Purge Named Projections And Recover | Every content-bearing projection ID reports restrictive completion; partial failure, restart, timeout, and duplicate delivery remain idempotent and non-success | 8.4; `EXT-TOPOLOGY-1` Available |
| 8.6 | Confirm Restrictive Deletion And Operator Evidence | UI/API show success only after payload protection and every named purge are restrictive; focused authorization and forensic no-content evidence are linked | 8.3-8.5; `EXT-TOPOLOGY-1` Available |
| 8.7 | Operate Safety Cost And Governance Policies | Existing 8.4 outcome, renumbered | 5.3, 5.5, 6.3, 6.4, 6.8 |
| 8.8 | Define Versioned Measurement Contracts | Shared schema owns source/timestamp, percentile or numerator/denominator, sample/window/cohort, late/missing data, duplicate, invalidation, and insufficiency rules without live-attainment claims | 5.5 and authoritative source-event contracts |
| 8.9 | Calculate Runtime Latency Metrics | NFR-9 calculators and live observations own the four runtime latency families and 30-sample minimums | 8.8; runtime sources through 7.6; `EXT-TOPOLOGY-1` Available for live evidence |
| 8.10 | Calculate Product And Workflow Metrics | SM-1-SM-6 and SM-C1-SM-C3 own adoption, proposal, authorization, audit, and API/UI-parity cohorts without cross-tenant inference | 8.8; product sources through 8.7; `EXT-TOPOLOGY-1` Available for live evidence |
| 8.11 | Prove Accessible Localized And Responsive UI | NFR-13 route/state inventory proves WCAG 2.2 AA, English/French parity, FrontComposer/Fluent V5, focus/live regions, reduced motion, and restrictive viewports | Completed interactive routes through 8.7; `EXT-TOPOLOGY-1` Available |
| 8.12 | Qualify UI Tenant And Operation Isolation | Every affected route/operation proves API/UI authorization parity, cross-tenant denial before render/effect, and safe accessible output with no broad-suite inference | 8.11; completed public operations; `EXT-TOPOLOGY-1` Available |
| 8.13 | Measure Browser Interaction Performance | NFR-14 discriminated browser-monotonic samples, attestation/correlation, duplicate rules, thresholds, and at least 30 executions per kind are proven | 8.11-8.12; `EXT-TOPOLOGY-1` Available |
| 8.14 | Inspect Launch Evidence And Blockers | Existing 8.7 outcome, renumbered; serves as the final inventory-completeness inspection without issuing `RQ-1` | 5.5 and bounded evidence through 8.13; `EXT-TOPOLOGY-1` Available |

**Rationale:** Deletion safety, metric correctness, UI conformance, UI isolation, and UI performance now have separate owners and failure evidence. Story 8.14 remains the completeness gate; `RQ-1` remains the later release decision.

### 4.6 Epics Metadata And Document Cleanup

**Artifact:** `epics.md`

**OLD:**

- Active backlog declares exactly 27 stories.
- Requirement notation mixes `FR1`/`FR-1`, `NFR1`/`NFR-1`, and `NonFunctional Requirements`.
- Standalone `+` lines precede Epic 6 and Epic 7.
- Historical Epics 1-4 rely mainly on narrative warnings for exclusion.

**NEW:**

- Declare active counts as 8 + 11 + 6 + 14 = 39.
- Add machine-readable active authority metadata listing Epics 5-8, Story IDs 5.1-8.14, and `RQ-1` as a non-story release gate.
- Mark Epics 1-4 and `epic-5-superseded-2026-08-01.md` as excluded inputs for story generation and sprint planning.
- Normalize identifiers to `FR-1` through `FR-28`, `NFR-1` through `NFR-14`, and heading `Non-Functional Requirements`.
- Remove both standalone `+` lines.
- Update story-count assertions, dependency topology, OwnedClauses, evidence manifests, verification commands, and negative-evidence ownership for every split or renumbered story.

**Rationale:** The active story graph becomes parseable and unambiguous to automation.

### 4.7 Architecture And UX Disposition

**Architecture artifact:** `ARCHITECTURE-SPINE.md`

**OLD:** Architecture references current Story 5.6, 6.4/6.5, and 8.3/8.5/8.6 ownership.

**NEW:** Preserve AD-1 through AD-26 unchanged. Update only:

- implementation-gap ownership from 5.1/5.6 to 5.1/5.6/5.7;
- the External V1 Prerequisites direct-consumer table to the IDs in §4.1;
- story references for AD-13, AD-21, AD-23, AD-24, AD-25, and AD-26 to the new bounded owners;
- source provenance to include this proposal after approval.

**UX artifacts:** `DESIGN.md` and `EXPERIENCE.md`

No text changes are proposed. Existing UX semantics already require authoritative callability, full-or-blocked context, no proposal on generation failure, non-success pending/approved states, NFR-13, NFR-14, isolation, and `InsufficientEvidence`. The new story graph divides ownership of those requirements without changing the experience.

### 4.8 Sprint Status Synchronization

**Artifact:** `_bmad-output/implementation-artifacts/sprint-status.yaml`

**OLD:**

> `epic-5` plus 18 superseded story rows are `backlog`; Epics 6-8 are absent.

**NEW:**

After approval and after `epics.md` is updated, remove the 18 superseded forward rows and add these active rows as `backlog`:

```yaml
  epic-5: backlog
  5-1-establish-build-package-boundary-and-basic-ci-gates: backlog
  5-2-configure-hexa-through-live-eventstore-operations: backlog
  5-3-govern-provider-models-and-pricing-through-live-operations: backlog
  5-4-prove-tenant-party-and-approver-readiness: backlog
  5-5-publish-authoritative-readiness-and-provider-state-contracts: backlog
  5-6-compose-agents-in-the-platform-host-and-prove-health: backlog
  5-7-qualify-secrets-and-dapr-access-control: backlog
  5-8-activate-hexa-only-when-setup-gates-pass: backlog
  epic-5-retrospective: optional
  epic-6: backlog
  6-1-start-a-durable-automatic-interaction: backlog
  6-2-use-the-complete-authorized-conversation-or-block: backlog
  6-3-block-unsafe-prompt-context-or-output: backlog
  6-4-prepare-an-attempt-and-reserve-maximum-cost: backlog
  6-5-publish-capacity-profiles-and-admit-atomically: backlog
  6-6-schedule-queues-fairly-across-tenants: backlog
  6-7-fence-and-recover-capacity-leases: backlog
  6-8-invoke-provider-and-reconcile-cost: backlog
  6-9-join-and-post-exactly-once-as-hexa: backlog
  6-10-qualify-automatic-interaction-recovery: backlog
  6-11-call-hexa-and-follow-automatic-status-accessibly: backlog
  epic-6-retrospective: optional
  epic-7: backlog
  7-1-create-and-discover-a-pending-proposal: backlog
  7-2-edit-an-immutable-proposal-version: backlog
  7-3-regenerate-under-fresh-gates: backlog
  7-4-approve-and-post-one-selected-version: backlog
  7-5-reject-or-abandon-a-proposal: backlog
  7-6-expire-a-proposal-deterministically: backlog
  epic-7-retrospective: optional
  epic-8: backlog
  8-1-retain-sensitive-content-and-apply-legal-holds: backlog
  8-2-export-authorized-audit-content-securely: backlog
  8-3-authorize-deletion-and-enforce-legal-holds: backlog
  8-4-erase-protected-event-payloads: backlog
  8-5-purge-named-projections-and-recover: backlog
  8-6-confirm-restrictive-deletion-and-operator-evidence: backlog
  8-7-operate-safety-cost-and-governance-policies: backlog
  8-8-define-versioned-measurement-contracts: backlog
  8-9-calculate-runtime-latency-metrics: backlog
  8-10-calculate-product-and-workflow-metrics: backlog
  8-11-prove-accessible-localized-and-responsive-ui: backlog
  8-12-qualify-ui-tenant-and-operation-isolation: backlog
  8-13-measure-browser-interaction-performance: backlog
  8-14-inspect-launch-evidence-and-blockers: backlog
  epic-8-retrospective: optional
```

Preserve all Epic 1-4 statuses and action items. Do not add `RQ-1` to `development_status`. Do not create story files or move any row to `ready-for-dev` as part of synchronization.

**Rationale:** Sprint status will contain executable work only and will not bypass the external dependency gate.

### 4.9 PRD And Launch-Readiness Disposition

**PRD:** No edit. V1 scope, FR-1 through FR-28, NFR-1 through NFR-14, dependency commitment rules, and Evidence Levels remain unchanged.

**Launch readiness register:** No semantic edit. Initial records remain `InsufficientEvidence`; `RQ-1` remains `NOT READY`. Story reslicing changes evidence producers, not gate meaning or current evidence state.

## 5. Implementation Handoff

### Scope Classification

**Major.** The executable backlog grows from 27 to 39 stories, exact dependency consumers change, and sprint tracking must be regenerated. Product scope, architecture invariants, UX semantics, and completed history remain unchanged.

### Routing And Responsibilities

| Recipient | Responsibility |
| --- | --- |
| Product Manager | Approve unchanged MVP, four-epic structure, 39-story refinement, and continued `RQ-1` separation |
| Solution Architect | Validate resliced dependencies against AD-13, AD-21, and AD-23-AD-26; update architecture story references |
| Product Owner | Apply exact story edits, counts, dependency topology, evidence manifests, and sprint-status synchronization |
| Conversations Maintainer | Commit `EXT-CONV-AI-1` target, date, command, and evidence contract |
| Platform Maintainer | Commit `EXT-HOST-1`, `EXT-SECRETS-1`, and `EXT-TOPOLOGY-1`; provide exact host, secret, and fixture targets |
| Agents Runtime Maintainer | Commit `EXT-PROVIDER-1` and `EXT-TOKEN-1`; validate the prepared-attempt/admission/Provider split |
| Security Engineering | Commit `EXT-SAFETY-1`; validate secret, safety, and deletion slices |
| UX Designer | Confirm existing UX spines need no semantic edit and accept the NFR-13/isolation/NFR-14 split |
| Test Architect | Bind verification commands and focused negative evidence to each new story; keep fixtures distinct from live attainment |
| Developer agents | Implement only an approved, readiness-passed story whose declared dependencies satisfy the register gate |

### Definition Of Ready

A replacement story may move to `ready-for-dev` only when:

1. this proposal and the corrected epic graph are approved;
2. every declared external dependency is `Committed` or `Available` as required, with no `TBD` target, date, or verification command;
3. any executed external seam is `Available` and its exact command passes against the consumed target;
4. the story has one primary demonstrable outcome, complete dependencies, clause-level ownership, and an executable verification command;
5. focused happy, failure, replay/concurrency, and cross-tenant denial evidence is named where applicable;
6. sprint status was generated from the corrected active story graph and historical/superseded material is mechanically excluded;
7. implementation-readiness validation has been rerun successfully.

### Success Criteria

1. All seven external dependency records contain accepted owner, repository, artifact, immutable target, date, contract/command, evidence level, status, and exact consumers.
2. Stories 6.4 and 6.5 no longer depend on each other directly or implicitly.
3. Epics 5-8 contain exactly 39 independently demonstrable stories with counts 8, 11, 6, and 14.
4. The six oversized stories are replaced by the bounded outcomes in this proposal.
5. PRD scope, Architecture invariants, UX semantics, completed Epics 1-4, and the prior approved proposal are preserved.
6. Requirement notation and parser noise in `epics.md` are normalized.
7. `sprint-status.yaml` tracks only the corrected 39-story forward backlog and creates no story file or `ready-for-dev` transition.
8. Live evidence remains restrictive until its declared Level 4/5 prerequisites actually exist.
9. A fresh implementation-readiness assessment passes before Phase 4 begins.

## 6. Checklist Progress

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 | Done | Trigger is the 2026-08-02 readiness report; directly affected stories include 6.4 and 6.5 |
| 1.2 | Done | Delivery-decomposition, dependency-commitment, and tracker-authority failure; no product pivot |
| 1.3 | Done | Report, registers, active epics, architecture/UX, prior proposal, and sprint status provide concrete evidence |
| 2.1 | Action-needed | Epic 6 cannot complete as declared while 6.4/6.5 remain cyclic |
| 2.2 | Action-needed | Reslice Epics 5, 6, and 8; preserve Epic 7 |
| 2.3 | Done | All active and historical epics were reviewed for ripple effects |
| 2.4 | Done | No epic becomes obsolete and no new epic is required |
| 2.5 | Action-needed | Story ordering/counts and exact dependency consumers change |
| 3.1 | Done | PRD goals and MVP remain achievable without edits |
| 3.2 | Action-needed | Architecture story references and dependency consumers require reconciliation; decisions remain valid |
| 3.3 | N/A | UX spines require no semantic edit; existing obligations are redistributed across stories |
| 3.4 | Action-needed | Dependency register, epics, architecture references, sprint status, and readiness rerun require follow-up |
| 4.1 | Viable | Direct adjustment; Medium planning effort, High implementation effort, Medium residual risk |
| 4.2 | Not viable | Rollback provides no simplification benefit |
| 4.3 | Not selected | MVP reduction does not resolve blockers |
| 4.4 | Done | Dependency freeze plus 39-story reslice selected |
| 5.1 | Done | Issue summary and evidence recorded |
| 5.2 | Done | Epic, story, artifact, and technical impacts recorded |
| 5.3 | Done | Recommended path, trade-offs, effort, risk, and timeline impact recorded |
| 5.4 | Done | MVP unchanged; sequencing and entry gates defined |
| 5.5 | Done | Major-scope recipients and responsibilities defined |
| 6.1 | Done | All applicable checklist items are represented; action-needed items are routed |
| 6.2 | Done | Draft proposal checked against the report and canonical planning artifacts |
| 6.3 | Done | Administrator explicitly approved implementation on 2026-08-02 and revalidated approval on 2026-08-03 |
| 6.4 | Action-needed | Sprint status changes only after approval and canonical epic edits |
| 6.5 | Done | Major-scope handoff routed to Product Manager, Solution Architect, and Product Owner |

## 7. Approval Record

- Review mode: Batch.
- Complete-proposal review: continued by Administrator.
- Implementation approval: explicitly approved by Administrator on 2026-08-02.
- Current-run revalidation: explicitly approved by Administrator on 2026-08-03
  after reconfirming the trigger, epic impact, artifact impact, recommended path,
  and complete Batch proposal.
- Scope classification: Major.
- Route: Product Manager and Solution Architect for replan/architecture acceptance; Product Owner for backlog and sprint-status synchronization.
- Proposal state: Approved; no canonical PRD, epic, architecture, UX, dependency-register, launch-register, or sprint-status edits were applied by this workflow.
- Existing approved `sprint-change-proposal-2026-08-02.md` remains intact.
- Developer agents remain gated by the Definition of Ready in §5 and the external dependency register.

## 8. Handoff And Workflow Execution Log

- 2026-08-03 - Correct Course reactivated in Batch mode for the same
  `implementation-readiness-report-2026-08-02.md` trigger.
- 2026-08-03 - The current PRD, active 27-story epic graph, Architecture and UX
  spines, dependency/readiness registers, sprint status, readiness report, and
  this existing remediation proposal were revalidated.
- 2026-08-03 - Administrator confirmed the trigger/context, epic impact,
  artifact impact, and direct-adjustment path, continued the complete proposal,
  and explicitly approved implementation.
- 2026-08-03 - Major-scope handoff reconfirmed for Product Manager, Solution
  Architect, and Product Owner; Developer agents remain gated by the Definition
  of Ready and external dependency commitments.
- 2026-08-02 - Correct Course activated in Batch mode for `implementation-readiness-report-2026-08-02.md`.
- 2026-08-02 - Repository instructions, resolved workflow customization, persistent project contexts, BMAD configuration, checklist, PRD, Epics, Architecture, UX, dependency/readiness registers, prior approved proposal, and sprint status were assessed.
- 2026-08-02 - Checklist Sections 1-4 completed: trigger/context, epic impact, artifact conflict, and path-forward evaluation.
- 2026-08-02 - Direct adjustment selected: dependency freeze plus a 39-story reslice across unchanged Epics 5-8.
- 2026-08-02 - Batch proposal compiled with old-to-new edits, exact story/tracker map, dependency-consumer changes, success criteria, and implementation handoff.
- 2026-08-02 - Administrator continued the complete-proposal review and explicitly approved implementation.
- 2026-08-02 - Major-scope handoff routed to Product Manager and Solution Architect; Product Owner owns canonical backlog and sprint-status synchronization.

### Handoff Completion

The handoff package contains the approved Sprint Change Proposal, artifact-specific old-to-new edits, the exact 39-story forward map, dependency commitment gates, tracker rows, and success criteria. The first implementation checkpoint is owner acceptance of all seven external dependency records, beginning with `EXT-HOST-1`; canonical epic and tracker changes follow through the Product Manager, Solution Architect, and Product Owner handoff.
