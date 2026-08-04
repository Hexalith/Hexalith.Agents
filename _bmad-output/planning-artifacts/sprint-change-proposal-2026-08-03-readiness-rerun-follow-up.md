---
title: Sprint Change Proposal - Implementation Readiness Rerun Follow-Up
status: approved
created: 2026-08-03
updated: 2026-08-04
mode: Batch
change_scope: major
recommended_path: direct-adjustment-and-approved-plan-synchronization
project: agents
owner: Administrator
approval_required: false
approved_by: Administrator
approved_on: 2026-08-04
routed_to:
  - Product Manager
  - Solution Architect
  - Product Owner / Scrum Master
  - Test Architect
trigger_report: implementation-readiness-report-2026-08-03.md
compares_with: implementation-readiness-report-2026-08-03.pre-rerun.md
amends_if_approved:
  - sprint-change-proposal-2026-08-03.md
  - epics.md
  - external-dependency-register.md
  - launch-readiness-register.md
  - ../implementation-artifacts/sprint-status.yaml
preserves:
  - sprint-change-proposal-2026-08-03.md as the approved major remediation authority
  - sprint-change-proposal-2026-08-02-readiness-remediation.md
  - completed Epics 1-4 as non-executable historical evidence
---

# Sprint Change Proposal: Implementation Readiness Rerun Follow-Up

## 1. Issue Summary

The 2026-08-03 implementation-readiness rerun remains **NOT READY**. Product definition is not the problem: all 28 Functional Requirements are covered, all 14 Non-Functional Requirements remain explicit, and the PRD, architecture, and UX spines describe the same V1 outcome.

The remaining failure is execution readiness:

1. All seven currently registered critical external dependencies remain `Uncommitted`.
2. Current Story 7.4 omits the shared Conversations membership/posting foundation from its prior-story dependencies.
3. Current Story 8.1 does not declare every terminal-event/status family enrolled in retention.
4. Conflicting criteria in historical Epics 1-4 remain co-located with executable work.
5. Eight current stories require decomposition or explicit iteration-sized validation: 5.4, 5.6, 6.1, 6.4, 6.5, 8.4, 8.5, and 8.6.
6. Conversation-owner terminology still needs consistent Facilitator-based V1 disclosure.
7. Two stray `+` lines and inconsistent FR/NFR identifiers reduce machine traceability.

### Discovery Context

The approved `sprint-change-proposal-2026-08-03.md` already defines a 44-story remediation across Epics 5-10. It was produced from the more severe report now preserved as `implementation-readiness-report-2026-08-03.pre-rerun.md`. The later rerun replaced the unsuffixed report path, so the approved proposal's current `trigger_report` metadata no longer identifies the evidence whose findings it describes.

This follow-up does not create a competing backlog. It corrects that provenance, reconciles the rerun findings with the approved 44-story plan, and adds the one missing sizing gate.

## 2. Impact Analysis

### Epic Impact

- Epics 1-4 remain completed historical evidence and must become mechanically non-executable.
- The approved target remains 44 active stories across Epics 5-10; no new product epic or MVP capability is introduced.
- Target Story 7.4 must explicitly depend on the target Conversations membership/posting foundation, Story 6.9.
- Target Story 8.1 remains narrowed to retention deadlines, legal holds, and durable disposition eligibility; completed erasure/expiry remains in later prior-ordered stories.
- Seven of the eight high-risk current stories are explicitly decomposed by the approved 44-story graph. Current Story 5.4 maps substantially to target Story 5.5 and still needs explicit one-iteration sizing validation or a split before promotion.

### Artifact Impact

| Artifact | Required change | Scope effect |
| --- | --- | --- |
| Approved 2026-08-03 proposal | Correct trigger provenance and add the rerun reconciliation reference | No decision reversal |
| Epics | Materialize the approved graph; add the explicit 7.4 -> 6.9 dependency; validate target 5.5 sizing | No FR/NFR change |
| External dependency register | Preserve all seven current records as `Uncommitted` until fully accepted; add approved `EXT-CONV-UI-1` during canonical synchronization | Adds no inferred commitment |
| Launch-readiness register | Reconcile consumer IDs and dependency evidence references to the approved graph | No gate weakening |
| Sprint status | Replace superseded executable Epic 5 rows with the approved active graph after canonical epic synchronization | Tracker-only change |
| Historical epic artifact | Archive completed Epics 1-4 or attach local machine-visible non-executable markers | Evidence preserved |
| PRD / Architecture / UX | Apply already approved Facilitator terminology and Conversation-owned invocation precision edits | No MVP change |

### Technical And Delivery Impact

No implementation code changes are authorized by this proposal. Subsequent implementation remains blocked by the applicable dependency records, exact verification lanes, and Definition of Ready. Schedule confidence remains low until owners accept immutable targets, integration dates, and compatibility commands.

## 3. Recommended Approach

Use **direct adjustment plus synchronization of the already approved major remediation**:

1. Preserve the approved 44-story plan as the sole target backlog.
2. Correct its evidence provenance before canonical synchronization.
3. Apply the rerun's exact dependency fixes and historical-authority controls.
4. Require an explicit sizing decision for target Story 5.5; split only if the team cannot show it fits one implementation iteration.
5. Keep every unknown external target `Uncommitted`; no planning edit may fabricate acceptance or evidence.
6. Rerun implementation readiness after the canonical artifacts and tracker agree.

### Options Evaluated

| Option | Verdict | Effort | Risk | Rationale |
| --- | --- | --- | --- | --- |
| Direct adjustment and approved-plan synchronization | Recommended | Medium planning; high later implementation | Medium after commitments, high before | Resolves the actual readiness defects without changing product scope |
| Roll back completed Epics 1-4 | Not viable | High | High | Historical delivery is evidence, not the cause of current readiness failure |
| Reduce or redefine MVP | Not viable | High | High | Requirements already align; scope reduction does not commit dependencies or repair backlog authority |
| Ignore the rerun and start under waivers | Rejected | Low initially | Critical | Violates fail-closed dependency and evidence rules |

## 4. Detailed Change Proposals

### 4.1 Approved Proposal Provenance

**Artifact:** `sprint-change-proposal-2026-08-03.md`  
**Section:** Frontmatter and Issue Summary

**OLD:**

```yaml
trigger_report: implementation-readiness-report-2026-08-03.md
```

**NEW:**

```yaml
trigger_report: implementation-readiness-report-2026-08-03.pre-rerun.md
followup_report: implementation-readiness-report-2026-08-03.md
```

Add a short note that the unsuffixed rerun reduced the assessment from 14 issue groups to 7 but remained `NOT READY`.

**Rationale:** The approved proposal describes pre-rerun findings such as the Story 6.4 forward dependency, missing verification infrastructure, and the unbound Conversation UI seam. Correct metadata prevents the later rerun from being misquoted as evidence it did not contain.

### 4.2 Story 7.4 Posting Dependency

**Artifact:** `epics.md`  
**Story:** 7.4, `Dependencies`

**CURRENT OLD:**

> Prior stories: 7.1; 7.2 and 7.3 versions are supported when present.

**CURRENT NEW (if the 27-story graph were retained):**

> Prior stories: 6.6 and 7.1; 7.2 and 7.3 versions are supported when present. Story 7.4 reuses Story 6.6 membership/posting service, deterministic MessageId/idempotency, recovery, and negative-isolation evidence.

**APPROVED-TARGET NEW:**

> Prior stories: 6.9 and 7.1; 7.2 and 7.3 versions are supported when present. Story 7.4 reuses Story 6.9 membership/posting service, deterministic MessageId/idempotency, recovery, and negative-isolation evidence.

**Rationale:** Approval posting must not create a second or divergent Conversations integration path. The target dependency is 6.9 after approved renumbering.

### 4.3 Story 8.1 Retention Scope

**Artifact:** `epics.md`  
**Story:** 8.1, Dependencies and Primary Demonstrable Outcome

**OLD:**

> Prior stories: 5.2 for authorized EventStore-backed operations and 7.6 for deterministic proposal terminal timestamps.

**NEW:**

Under the approved graph, keep target Story 8.1 limited to immutable retention deadlines, legal holds, and durable disposition eligibility. Its manifest must name the exact automatic, failure, proposal approval/rejection/abandonment/expiry, and posting terminal contracts it enrolls. If any terminal family is not yet available, list it as `BlockedBy` and defer its enrollment to the first story that owns that terminal contract. Do not claim completed erasure or expiry in Story 8.1.

**Rationale:** This makes retention enrollment complete and auditable without recreating the forward dependency that the approved plan already removed.

### 4.4 High-Risk Story Sizing

**Artifact:** Approved 44-story target graph and resulting story manifests

**OLD:**

Current Stories 5.4, 5.6, 6.1, 6.4, 6.5, 8.4, 8.5, and 8.6 combine multiple capability and qualification outcomes without an explicit team sizing record.

**NEW:**

- Preserve the approved decompositions for current 5.6, 6.1, 6.4, 6.5, 8.4, 8.5, and 8.6.
- Before target Story 5.5 can become `ready-for-dev`, record `Estimate`, `SizingMethod`, `SizedBy`, `SizedAt`, assumptions, and the one-iteration capacity limit.
- If target Story 5.5 exceeds that limit, split it into:
  1. tenant-access plus Party-identity readiness; and
  2. Approver Policy resolution, disclosure, revocation, and API/UI parity.
- Revalidate all dependency and FR/NFR mappings after any split.

**Rationale:** The rerun explicitly permits either decomposition or sizing validation. This closes the only flagged area not unambiguously split by the approved graph without forcing premature renumbering.

### 4.5 Historical Authority

**Artifact:** `epics.md` and `epics-completed-1-4.md`

**OLD:**

Historical Stories 2.3, 2.6, 4.4, and 4.5 remain beside active stories and contain criteria that conflict with full-context-only invocation, sole **Call hexa** entry, hard cost enforcement, and V1 tool/MCP/A2A exclusions.

**NEW:**

Apply the approved archive plan: preserve the completed text verbatim in `epics-completed-1-4.md` with `status: completed-historical` and `executable: false`; keep only a concise history/link section in canonical `epics.md`. If archiving cannot occur atomically, attach local `historical: true` and `mustNotImplement: true` markers next to every conflicting criterion before any implementation agent consumes the file.

**Rationale:** A global preamble is insufficient for machine consumers when contradictory acceptance criteria remain in the executable artifact.

### 4.6 External Dependencies

**Artifact:** `external-dependency-register.md`

**OLD:**

All seven current records are `Uncommitted`, with `TBD` immutable targets, dates, and verification commands.

**NEW:**

For each existing record, require owner, repository, artifact, immutable target, integration date, compatibility contract plus executable command, Evidence Level, accepted status, and reconciled consumers. Keep the record `Uncommitted` until every field is accepted. During approved canonical synchronization, add `EXT-CONV-UI-1` as an eighth `Uncommitted` record; do not count it as committed merely because the planning proposal names it.

**Rationale:** External commitment is coordination work owned by the named maintainers. Planning can define the gate but cannot supply acceptance on their behalf.

### 4.7 Tracker And Traceability Hygiene

**Artifacts:** `epics.md`, `sprint-status.yaml`

**OLD:**

- `sprint-status.yaml` still exposes the superseded 18-story Epic 5.
- Two standalone `+` separators remain before active epic headings.
- `FR1`/`NFR1` and `FR-1`/`NFR-1` forms are mixed.

**NEW:**

- After canonical epic synchronization, replace superseded executable rows with the approved 44-story Epic 5-10 graph, all initially `backlog` unless verified state maps forward.
- Do not add `RQ-1` as a development story.
- Remove both stray `+` lines.
- Normalize machine-facing identifiers to `FR-#` and `NFR-#`, and use the heading `Non-Functional Requirements`.

**Rationale:** Canonical backlog, tracker, and machine traceability must describe one execution authority.

## 5. Implementation Handoff

### Scope Classification

**Major.** The product scope is unchanged, but the already approved correction reorganizes the active backlog, changes dependency topology, archives historical execution criteria, and synchronizes multiple planning authorities.

### Recipients And Responsibilities

- **Product Manager:** approve the provenance correction, preserve 28/28 FR coverage, and confirm no MVP change.
- **Solution Architect:** validate the 7.4 -> 6.9 dependency, Story 8.1 terminal-contract inventory, approved external seam mappings, and Facilitator terminology.
- **Product Owner / Scrum Master:** materialize the approved story graph, archive historical epics, and synchronize `sprint-status.yaml` atomically.
- **Test Architect:** validate the sizing record for target Story 5.5 and the executable story-verification catalog.
- **External dependency owners:** supply and accept immutable targets, dates, compatibility commands, and evidence; no other role may fabricate them.
- **Developer agent:** begin only with a story that passes the resulting Definition of Ready and its exact verification lane.

### Success Criteria

1. The approved proposal points to the pre-rerun report and names the rerun as follow-up evidence.
2. Canonical artifacts expose one active approved graph and one synchronized tracker.
3. Story 7.4 explicitly reuses the prior posting foundation.
4. Story 8.1 names every enrolled terminal contract and claims no incomplete expiry effect.
5. All eight sizing concerns are closed by decomposition or an accepted one-iteration sizing record.
6. Historical contradictory criteria are mechanically non-executable.
7. Every dependency record remains honest and mechanically gates consumers.
8. A readiness rerun finds no active-story dependency, historical-authority, or sizing blocker; remaining `Uncommitted` external records remain visible blockers rather than planning defects.

## 6. Change Navigation Checklist

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [N/A] | Formal readiness rerun is the trigger; findings span Stories 7.4, 8.1, and systemic artifacts |
| 1.2 Core problem | [x] | Backlog dependency accuracy, external commitment, historical authority, and sizing—not a product pivot |
| 1.3 Evidence | [x] | Current and pre-rerun reports, PRD, architecture, UX, epics, registers, approved proposal, and tracker inspected |
| 2.1 Current epic viability | [x] | Epics 5-8 outcomes remain viable with approved decomposition and explicit fixes |
| 2.2 Epic changes | [x] | Preserve approved Epics 5-10 target; no competing graph |
| 2.3 Remaining epics | [x] | All active epics remain dependency-gated; consumer mappings must be reconciled |
| 2.4 New/obsolete epics | [x] | No new product epic; approved outcome Epics 9-10 remain decomposition artifacts |
| 2.5 Order/priority | [x] | 7.4 depends on 6.9; Story 8.1 remains eligibility-only; dependency-light verification begins first |
| 3.1 PRD | [x] | MVP unchanged; approved terminology/dependency precision edits remain sufficient |
| 3.2 Architecture | [x] | No replacement; approved seam and ownership precision applies |
| 3.3 UX | [x] | No flow expansion; actual Facilitator authority and Conversation-owned placement remain explicit |
| 3.4 Other artifacts | [!] | Registers, historical archive, verifier catalog, and sprint tracker require synchronized changes |
| 4.1 Direct adjustment | Viable | Medium planning effort; high later implementation effort |
| 4.2 Rollback | Not viable | Completed evidence is preserved |
| 4.3 MVP review | Not selected | Scope reduction would not resolve blockers |
| 4.4 Recommended path | [x] | Direct adjustment plus approved-plan synchronization |
| 5.1-5.5 Proposal components | [x] | Issue, impact, recommendation, MVP impact, and handoff are defined above |
| 6.1 Checklist review | [x] | All applicable items addressed; action-needed artifacts are named |
| 6.2 Proposal accuracy | [x] | Reconciled against both report snapshots and current canonical artifacts |
| 6.3 Explicit approval | [x] | Administrator approved the proposal on 2026-08-04 |
| 6.4 Sprint status | [N/A] | This follow-up adds no immediate tracker row; synchronization of the already approved 44-story graph remains atomic handoff work after canonical epic materialization |
| 6.5 Handoff | [x] | Major-scope route confirmed to Product Manager, Solution Architect, Product Owner / Scrum Master, and Test Architect |

## 7. Approval Record

- Review mode: Batch.
- Complete-proposal review: Continued by Administrator on 2026-08-04.
- Proposal state: Approved.
- Approval: Explicitly approved by Administrator on 2026-08-04.
- Scope classification: Major.
- Route: Product Manager and Solution Architect for plan/architecture acceptance; Product Owner / Scrum Master for canonical backlog and tracker synchronization; Test Architect for sizing and verification-catalog validation.
- Authorization boundary: Approval authorizes the planning-artifact handoff described by this proposal. It does not fabricate external commitments or evidence, bypass Definition of Ready, execute `RQ-1`, or authorize broad feature implementation.
- Canonical PRD, architecture, UX, epics, registers, tracker, and implementation files were not edited by this workflow.

## 8. Handoff And Workflow Execution Log

- 2026-08-03 — Correct Course activated for the implementation-readiness rerun; Batch review mode selected.
- 2026-08-03 — Repository instructions, workflow customization, persistent project contexts, canonical PRD, architecture, UX, epics, dependency and launch registers, sprint tracker, both readiness snapshots, and the approved same-date proposal were assessed.
- 2026-08-03 — The approved same-date proposal was traced to the pre-rerun report; a follow-up amendment was drafted to correct provenance and reconcile the reduced but still blocking rerun findings.
- 2026-08-03 — Direct adjustment plus approved-plan synchronization selected; rollback and MVP reduction rejected.
- 2026-08-03 — Batch proposal written without overwriting the approved same-date proposal or changing canonical planning/implementation artifacts.
- 2026-08-04 — Administrator continued the complete-proposal review.
- 2026-08-04 — Administrator explicitly approved implementation of this proposal.
- 2026-08-04 — Major-scope handoff routed to Product Manager, Solution Architect, Product Owner / Scrum Master, and Test Architect.

### Handoff Completion

The handoff package contains the approved follow-up proposal, evidence-provenance correction, exact Story 7.4 posting dependency, Story 8.1 retention-enrollment constraint, target Story 5.5 sizing gate, historical-authority control, external-dependency rules, tracker synchronization condition, success criteria, and role ownership. The first checkpoint is joint Product Manager/Solution Architect acceptance of the canonical artifact edits. Developer work begins only after the selected story satisfies the synchronized Definition of Ready and its exact verification lane.
