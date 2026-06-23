---
project: agents
date: 2026-06-23
workflow: bmad-correct-course
trigger: _bmad-output/planning-artifacts/implementation-readiness-report-2026-06-23.md
mode: Batch
status: Approved
scope_classification: Moderate
approved_by: Administrator
approved_on: 2026-06-23
---

# Sprint Change Proposal - Hexalith Agents

## 1. Issue Summary

The implementation readiness report dated 2026-06-23 concluded that the Hexalith Agents planning package is **NEEDS WORK** before full Phase 4 implementation.

The triggering issue is not missing PRD coverage. The PRD is validated as Excellent, FR coverage in `epics.md` is 100%, and architecture is final. The course correction is needed because several stories still combine implementation with unresolved decisions, stale UX assumptions, and cross-module prerequisites.

Primary evidence:

- Story 2.6 is not independently implementable while PRD/UX OQ-1, the exact Conversation invocation affordance, remains unresolved.
- Story 2.5 and Story 3.5 depend on a Conversations-owned `AddParticipant` command/API/client boundary for AI Agent membership; architecture review found the command exists but the inspected public Conversations client does not expose it.
- UX `EXPERIENCE.md` still treats Proposed Agent Reply ownership and provider capability metadata as open, although architecture closes them through AD-1, AD-2, AD-5, and AD-10.
- Story 1.8 and Story 4.5 are too broad to implement and review predictably.
- Story 3.6 includes expiry behavior while OQ-3 remains unresolved.
- Story 4.2 includes content-bearing audit behavior while audit retention, legal hold, export, and deletion behavior remain unresolved under OQ-8.
- AppHost/local orchestration and CI/build gate readiness are architecture requirements but are not clearly carried by an early story.

This is a planning and backlog correction, not an MVP scope reduction. No PRD requirement should be removed.

## 2. Change Analysis Checklist

| ID | Status | Finding |
| --- | --- | --- |
| 1.1 | [x] Done | Triggering source is the implementation readiness report. The most directly blocked story is Story 2.6; additional impacted stories are 1.8, 2.5, 3.6, 4.2, 4.5, and early setup/AppHost coverage. |
| 1.2 | [x] Done | Issue type: failed implementation readiness due to unresolved downstream decisions, story slicing problems, and cross-module prerequisite ambiguity. |
| 1.3 | [x] Done | Evidence collected from readiness report, PRD, epics, architecture spine, UX validation/review files, and sibling project contexts. |
| 2.1 | [x] Done | Current epics remain valid, but several stories need splitting or gating. |
| 2.2 | [x] Done | Epic-level scope remains; add prerequisite and decision stories rather than redefining epics. |
| 2.3 | [x] Done | Future epics impacted: Epic 2 invocation/posting, Epic 3 proposal expiry, Epic 4 audit/conformance. |
| 2.4 | [x] Done | No epic is obsolete. No new product epic is required. |
| 2.5 | [x] Done | Epic order remains sound. Insert cleanup stories before affected runtime/UI stories. |
| 3.1 | [x] Done | PRD MVP remains achievable. Proposed PRD update is status/traceability only, not scope change. |
| 3.2 | [x] Done | Architecture is aligned. Use AD-6, AD-16, and AD-17 to drive story prerequisites; no core architecture rewrite required. |
| 3.3 | [x] Done | UX source needs targeted amendments for stale OQs, parity/component contracts, accessibility, and governance states. |
| 3.4 | [x] Done | Secondary artifacts impacted: future sprint-status file, implementation readiness rerun, and story handoff notes. |
| 4.1 | [x] Viable | Direct adjustment is viable by splitting/gating stories and patching UX/traceability artifacts. Effort: Medium. Risk: Low/Medium. |
| 4.2 | [x] Not viable | Rollback is not useful; no completed implementation needs reverting. |
| 4.3 | [x] Not viable | MVP review/reduction is not needed. Requirements are covered; readiness issues are planning hygiene and decision gates. |
| 4.4 | [x] Done | Recommended path: Direct Adjustment with moderate backlog reorganization. |
| 5.1 | [x] Done | Issue summary provided in this proposal. |
| 5.2 | [x] Done | Epic and artifact impacts documented below. |
| 5.3 | [x] Done | Recommended path and rationale documented below. |
| 5.4 | [x] Done | MVP impact: no reduction. Action plan: split/gate stories, patch UX source, rerun readiness. |
| 5.5 | [x] Done | Handoff recipients: Product/UX, Architect, Product Owner, Developer agent. |
| 6.1 | [x] Done | Applicable checklist items addressed. |
| 6.2 | [x] Done | Proposal reviewed for consistency against PRD, architecture, UX, and readiness report. |
| 6.3 | [!] Action-needed | User approval is pending. |
| 6.4 | [!] Action-needed | No Agents sprint-status file exists yet; update once proposal is approved and stories are revised. |
| 6.5 | [!] Action-needed | Final handoff confirmation depends on user approval. |

## 3. Impact Analysis

### Epic Impact

Epic 1 remains valid but needs cleanup before UI-heavy setup work:

- Add an early AppHost/local topology and CI/build-gate story after Story 1.1.
- Split Story 1.8 into smaller UI slices for navigation/overview, provider catalog, `hexa` configuration, approver policy builder, and setup conformance.

Epic 2 remains valid but needs decision and cross-module prerequisite work:

- Add a prerequisite story to verify or expose the Conversations AI Agent membership seam before automatic or approved posting stories.
- Split Story 2.6 into a decision story for the V1 invocation affordance and an implementation story for the selected UX/status feedback.

Epic 3 remains valid but needs proposal lifecycle cleanup:

- Split Story 3.6 so reject/abandon can proceed independently from expiry.
- Gate expiry implementation behind OQ-3.
- Add UX/accessibility amendments for version selection, focus fallback, notification payload safety, and expiry announcements.

Epic 4 remains valid but needs audit/conformance cleanup:

- Split Story 4.2 into metadata-only audit query and content-bearing audit governance.
- Move most Story 4.5 conformance checks into the stories that introduce each invariant; keep Story 4.5 as final traceability/evidence packaging.

### Story Impact

Affected existing stories:

- Story 1.1: add or follow with AppHost/local topology and CI/build-gate coverage.
- Story 1.8: split into focused UI stories.
- Story 2.5: depends on verified/exposed Conversations membership boundary.
- Story 2.6: split into invocation decision and implementation.
- Story 3.2/3.7: add proposal discovery payload, accessibility, focus, and version-selection constraints.
- Story 3.6: split reject/abandon from expiry.
- Story 4.2: split metadata audit from content-bearing audit governance.
- Story 4.5: narrow to final evidence and readiness mapping.

New proposed stories:

- Story 1.1a: Establish AppHost, Local Topology, And CI Build Gate.
- Story 2.0a: Verify Or Expose Conversations AI Agent Membership Boundary.
- Story 2.6a: Define V1 Conversation Invocation Affordance.
- Story 2.6b: Implement Selected Invocation UX And Call Status Feedback.
- Story 3.6a: Reject And Abandon Proposals.
- Story 3.6b: Define And Implement Proposal Expiry Policy.
- Story 4.2a: Query Metadata-Only Audit Evidence Safely.
- Story 4.2b: Implement Content-Bearing Audit Evidence After Governance Decision.
- Revised Story 4.5: Produce Final Governance, Contract, And Readiness Evidence.

### Artifact Conflicts

PRD:

- PRD scope does not need to change.
- Section 12 should optionally gain a status column or downstream-decision addendum marking OQ-2 and OQ-7 as resolved by architecture while leaving OQ-1, OQ-3, OQ-4, OQ-5, OQ-6, OQ-8, OQ-9, OQ-10, and OQ-11 as active gates.

Architecture:

- No core architecture rewrite is required.
- AD-6 already defines the Conversations membership prerequisite.
- AD-10 resolves OQ-7 provider capability metadata.
- AD-16 and AD-17 should be traced into early setup and conformance stories.

UX:

- `EXPERIENCE.md` and `DESIGN.md` need amendments before story-dev relies on them as implementation contracts.
- Update stale OQ ownership, add blocker/carry-forward triage, Admin/API parity matrix, component contracts, surface state matrix, accessibility matrices, governance reauthorization rules, secret field lifecycle, automatic-mode confirmation, and FrontComposer implementation appendix.

Implementation artifacts:

- No Agents `sprint-status.yaml` exists yet under `_bmad-output/implementation-artifacts`. Create/update it after story revisions are approved.

### Technical Impact

- No rollback or code change is required now.
- Cross-module verification is required for Conversations membership support.
- Future implementation should not start Story 2.5, Story 2.6b, expiry, or content-bearing audit until their gates are closed.
- Foundational Story 1.1 can proceed only if the team explicitly accepts the known blockers and keeps runtime/UI blocked stories out of development until cleanup lands.

## 4. Recommended Approach

Recommended path: **Direct Adjustment with Moderate backlog reorganization**.

Rationale:

- PRD goals, FR coverage, and architecture are sound.
- The issues are concentrated and actionable.
- Splitting/gating stories preserves momentum without hiding unresolved decisions.
- No MVP reduction is justified.
- No implementation rollback is useful.

Effort estimate: Medium.

Risk level after changes: Low/Medium. Remaining risk is mostly OQ-1, OQ-3, OQ-8, and Conversations public membership seam availability.

Timeline impact:

- Add one short product/UX/architecture decision pass before Story 2.6 implementation.
- Add one cross-module verification/prerequisite story before posting stories.
- Add one UX patch pass before UI-heavy development.
- Rerun implementation readiness after `epics.md`, `EXPERIENCE.md`, and `DESIGN.md` are revised.

## 5. Detailed Change Proposals

### PRD

Artifact: `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md`

Section: `12. Open Questions And Deferred Decisions`

OLD:

```markdown
| OQ-2 | Which module owns Proposed Agent Reply runtime state and storage boundaries? | Architecture | Before aggregate and persistence design. |
| OQ-7 | What provider capability metadata is required in the Global Providers Aggregate for V1? | Architecture | Before provider configuration contract design. |
```

NEW:

```markdown
| OQ-2 | Which module owns Proposed Agent Reply runtime state and storage boundaries? | Architecture | Resolved by Architecture AD-1, AD-2, AD-5, and AD-18: Hexalith Agents owns proposal state and version history through `AgentInteraction`. |
| OQ-7 | What provider capability metadata is required in the Global Providers Aggregate for V1? | Architecture | Resolved by Architecture AD-10: V1 requires provider/model identity, display label, enabled state, secret reference/configured state, text-generation capability, context-window token limit, max-output token limit, timeout policy, and optional safe capability flags. |
```

Rationale: Architecture has resolved these downstream decisions. Keeping them flat-open causes UX and story-dev to treat closed architecture decisions as unresolved.

MVP impact: None.

### Architecture

Artifact: `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md`

Recommended edit: no core architecture rewrite. Use existing AD-6, AD-16, and AD-17 as source for story changes.

Optional traceability note:

```markdown
Implementation planning note: AD-6 requires a verified public Conversations AI Agent membership boundary before Stories 2.5 and 3.5 can complete. AD-16 requires module-local AppHost/local topology coverage before runtime stories depend on local orchestration. AD-17 conformance checks should be attached to the stories that introduce each invariant, with a final evidence story retained for traceability.
```

Rationale: The architecture already contains the necessary decisions; the backlog needs to carry them explicitly.

### UX Design And Experience

Artifacts:

- `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md`

Section: `Open Questions For UX Closure`

OLD:

```markdown
| OQ-2 | Module ownership for Proposed Agent Reply runtime state and storage boundaries. |
| OQ-7 | Provider capability metadata required in V1. |
```

NEW:

```markdown
| OQ-2 | Resolved by architecture: Hexalith Agents owns Proposed Agent Reply runtime state, version history, authorization decisions, expiry, posting outcome, and audit evidence through `AgentInteraction`. UX should use this as the owner for proposal queue/detail/editor/status. |
| OQ-7 | Resolved by architecture: ProviderCatalog V1 capability metadata is defined by AD-10. UX provider catalog and provider-status components should consume those fields and avoid provider-specific adapter knobs. |
```

Add a triage table:

```markdown
| ID | Status | Owner | Downstream gate | UX treatment |
| --- | --- | --- | --- | --- |
| OQ-1 | Blocker | Product + UX | Before Story 2.6b and API route naming | Must choose V1 invocation affordance or constrained set. |
| OQ-3 | Blocker | Product + Architecture | Before expiry implementation | Reject/abandon can proceed; expiry cannot. |
| OQ-4 | Carry-forward unless active notifications are in launch | Product + UX | Before notification stories or launch-readiness exception | Queue-only discovery is accepted only when launch readiness records it. |
| OQ-8 | Blocker | Product + Governance | Before content-bearing audit | Metadata-only audit can proceed. |
| OQ-11 | Launch blocker | Product + Release PM | Before launch-readiness review | Operational status must not hide missing thresholds. |
```

Add UX implementation contracts:

- Admin/API parity matrix for provider administration, Agent configuration, invocation, proposal workflow, status, audit, and launch readiness.
- Component contract table for each named component with owner, consumed data, emitted commands, states, auth/read-only variants, accessible-name rules, and linked API/status dependencies.
- Per-surface state matrix covering loading, empty, filtered-empty, permission denied, stale/degraded, error, in-flight, read-only, and terminal states.
- Governance rules requiring both Approver Policy authorization and current Source Conversation access before proposal discovery, detail, edit, regenerate, approval, rejection, abandonment, and audit inspection.
- Action-time reauthorization states: `revalidating`, `authorization stale`, and `denied after revalidation`.
- Proposal discovery/notification payload tiers: opaque reference until authorization is proven; no generated snippets in notifications by default; denial-safe counts.
- Audit success invariant: side-effecting success states show a durable audit/correlation reference, or an explicit `posted with audit pending` incident state.
- Proposal version selection model: immutable `VersionId`, source, timestamp, selected/current state, stale-selection protection, and approval confirmation naming the exact version.
- Focus fallback rules when a row/action disappears after approval, rejection, abandonment, expiry, or filtering.
- Live-region transition matrix and localized assistive text requirements.
- Secret field states: `not configured`, `configured`, `replace pending`, `rotation failed`, and `cleared`, with no secret values in DOM text, tooltips, copy/export, logs, audit, or accessible names.
- Automatic Response Mode activation review naming tenant, Agent Party, provider/model, call eligibility, future-only effect, and "posts without approval" consequence.
- FrontComposer implementation appendix: Fluent UI Blazor `5.0.0-rc.3-26138.1`, no raw interactive HTML controls, use `FcFluentIcons` factory, respect FluentAccordion guidance for multi-section pages, use FrontComposer/Fluent primitives and policy-gated navigation.

Rationale: UX currently contains strong direction but not enough implementation contracts for story-dev on governance, accessibility, parity, and stale decisions.

### Epic And Story Changes

Artifact: `_bmad-output/planning-artifacts/epics.md`

#### Add Story 1.1a

NEW:

```markdown
### Story 1.1a: Establish AppHost, Local Topology, And CI Build Gate

As an Integration Developer,
I want a minimal module-local AppHost, local topology, and CI build gate for Hexalith.Agents,
So that runtime stories can depend on a verified local orchestration and warning-clean build baseline.

**Acceptance Criteria:**

**Given** the Agents module shell exists
**When** local orchestration is inspected
**Then** `Hexalith.Agents.AppHost` composes the Agents service/UI with EventStore, Conversations, Parties, Tenants, and provider-adapter placeholders needed for local/dev/test
**And** the root `agents` workspace remains only a coordination/super-repo.

**Given** the AppHost model is built
**When** topology tests run without requiring production secrets
**Then** the test proves service names, dependency wiring, Dapr sidecar expectations, health endpoints, and explicit-start resources where applicable
**And** provider credentials are represented only by secret references or safe dev placeholders.

**Given** the Agents solution is built in CI/local validation
**When** restore, build, and the narrow test lane run
**Then** `.slnx`, Central Package Management, nullable, warnings-as-errors, project-boundary tests, and package-version centralization all pass without warnings.
```

Rationale: Architecture AD-16 requires module-local operational topology, and readiness found no early story clearly carrying it.

#### Replace Story 1.8 With Smaller UI Stories

OLD:

```markdown
### Story 1.8: Admin Setup UI And Readiness Overview
...
```

NEW:

```markdown
### Story 1.8a: Register Agents Navigation And Setup Overview
Focus: Agents domain registration, policy-gated nav, overview readiness, lifecycle, response mode, provider/model, blockers, and callability.

### Story 1.8b: Build Provider Catalog UI
Focus: full-width FC-TBL/FluentDataGrid provider/model records, capability metadata, secret configured/not-configured states, no secret exposure, loading/empty/error/denied/stale states.

### Story 1.8c: Build `hexa` Configuration And Activation Review
Focus: constrained Fluent form for identity, instructions, provider/model selection, response mode, content safety, lifecycle, activation blockers, automatic-mode confirmation, and future-only mode changes.

### Story 1.8d: Build Approver Policy Builder UI
Focus: policy source rows for caller, predefined Parties, tenant roles, and Conversation facilitator/owner mapping; blocked states for ambiguous/unavailable policy sources; policy-basis disclosure category.

### Story 1.8e: Setup UI Accessibility And Conformance
Focus: status semantics, keyboard/focus behavior, table semantics, localized visible and assistive text, no raw controls, no layout shifts, no custom radii, and FrontComposer implementation appendix conformance.
```

Rationale: The original Story 1.8 bundles too many UI surfaces and test obligations into one review unit.

#### Add Story 2.0a

NEW:

```markdown
### Story 2.0a: Verify Or Expose Conversations AI Agent Membership Boundary

As an Agents integrator,
I want an official Conversations membership command/API/client seam for AI Agent participants,
So that automatic and approved Agent responses can be posted without direct Conversation stream writes.

**Acceptance Criteria:**

**Given** Agents needs to post as `hexa`
**When** Conversations public contracts are inspected
**Then** the team verifies whether an official command/API/client operation exists to add an AI Agent participant with `ParticipantType.AiAgent`
**And** the result is documented as available or missing before Story 2.5 or Story 3.5 starts.

**Given** the public seam is missing
**When** this prerequisite is implemented
**Then** a Conversations-owned client/API boundary is exposed or a tracked cross-module story is created
**And** Agents still does not write Conversation streams/events directly.

**Given** membership establishment is retried
**When** the same Agent Party and Source Conversation are used
**Then** the operation is idempotent or safely rejected as already present
**And** missing, disabled, ambiguous, unauthorized, or unavailable Party/Conversation state fails closed.
```

Rationale: Architecture AD-6 makes this a prerequisite, and current-reality review found `AddParticipantCommand` exists but public client exposure is not confirmed.

#### Split Story 2.6

OLD:

```markdown
### Story 2.6: Conversation Invocation UX And Call Status Feedback
```

NEW:

```markdown
### Story 2.6a: Define V1 Conversation Invocation Affordance

As a Product/UX/Architecture decision owner,
I want the V1 Conversation invocation affordance selected and specified,
So that implementation can build one coherent keyboard, focus, permission, API, and no-post flow.

**Acceptance Criteria:**

**Given** OQ-1 is unresolved
**When** the decision story completes
**Then** it selects mention, command, action button, participant affordance, or a constrained combination for V1
**And** it defines owning surface, required fields, visible `hexa` identity, Source Conversation binding, prompt capture, response-mode disclosure, permission check, no-post states, API/client operation naming impact, low-fi Conversation wireframe, keyboard path, focus return, accessible names, and safe denial/status copy.

### Story 2.6b: Implement Selected Invocation UX And Call Status Feedback

As a Conversation Participant,
I want the selected `hexa` invocation UX and status feedback implemented,
So that I can call `hexa` without mistaking pending, failed, proposed, or posted states.

Precondition: Story 2.6a is approved.
```

Rationale: Implementation cannot choose among open invocation patterns without creating hidden product decisions.

#### Modify Story 2.5

OLD:

```markdown
**Given** an AgentInteraction is in Automatic Response Mode and generation plus safety checks pass
**When** posting begins
**Then** the system verifies the Agent `PartyId` is valid and present in the Source Conversation as an AI Agent participant through a Conversations-owned membership command/API/client boundary
```

NEW:

```markdown
**Given** Story 2.0a has verified or exposed the official Conversations AI Agent membership boundary
**And** an AgentInteraction is in Automatic Response Mode and generation plus safety checks pass
**When** posting begins
**Then** the system verifies the Agent `PartyId` is valid and present in the Source Conversation as an AI Agent participant through that Conversations-owned boundary
```

Rationale: Make the external prerequisite explicit before implementation starts.

#### Split Story 3.6

OLD:

```markdown
### Story 3.6: Reject, Abandon, And Expire Proposals
```

NEW:

```markdown
### Story 3.6a: Reject And Abandon Proposals
Focus: authorized rejection and abandonment terminal states, rationale metadata where policy requires it, preserved versions, no later approval/posting, safe UI/API routing.

### Story 3.6b: Define And Implement Proposal Expiry Policy
Precondition: OQ-3 is resolved.
Focus: expiry default, configurability, min/max if configurable, warning thresholds, deterministic timer ownership, injected time, terminal transition, queue/detail/status display, API/client contract behavior, accessibility announcements, and audit evidence.
```

Rationale: Reject/abandon are implementable now; expiry is blocked by an unresolved product/architecture decision.

#### Modify Story 3.2 And Story 3.7

Add to Story 3.2:

```markdown
**Given** proposal queue rows or notifications are shown
**When** authorization has not yet been proven for a user/surface
**Then** the surface exposes only opaque proposal references or safe counts
**And** generated content, Conversation snippets, caller personal data, unauthorized tenant data, and denial-sensitive counts are not disclosed.
```

Add to Story 3.7:

```markdown
**Given** multiple proposal versions exist
**When** an Approver selects a version
**Then** the selection model exposes stable `VersionId`, source, timestamp, selected/current state, and metadata before approval
**And** approval is blocked if a newer version appears after selection until the Approver revalidates the selected version.

**Given** a row or action trigger disappears after approval, rejection, abandonment, expiry, or filtering
**When** focus would otherwise return to a missing element
**Then** focus moves to the next eligible row, queue status summary, filter reset control, or proposal-detail heading
**And** a polite live-region announcement explains the item left the current view.
```

Rationale: Accessibility and governance reviews require deterministic version selection and focus recovery.

#### Split Story 4.2

OLD:

```markdown
### Story 4.2: Query Audit Evidence Safely
...
**Given** retention period, legal hold, export behavior, or deletion behavior is unresolved
**When** content-bearing audit implementation is attempted
**Then** the story or feature is blocked until a named platform policy or dedicated Agents governance decision exists
```

NEW:

```markdown
### Story 4.2a: Query Metadata-Only Audit Evidence Safely
Focus: support-safe metadata links for caller, Agent, Source Conversation, Provider/model, response mode, policy decisions, proposal path, posting outcome, timestamps, final Conversation Message reference, status, and correlation/audit references. No prompt/context/generated/edited content is displayed.

### Story 4.2b: Implement Content-Bearing Audit Evidence After Governance Decision
Precondition: OQ-8 is resolved by a named platform policy or dedicated Agents governance decision.
Focus: authorized content display, retention period, legal hold, export behavior, deletion behavior, redaction, payload protection, audit query behavior, and launch readiness blockers.
```

Rationale: Metadata-only audit can proceed without pretending content-governance is solved.

#### Replace Story 4.5 With A Narrow Final Evidence Story

OLD:

```markdown
### Story 4.5: Verify End-To-End Governance And Contract Conformance
```

NEW:

```markdown
### Story 4.5: Produce Final Governance, Contract, And Readiness Evidence

As a Master Test Architect,
I want final evidence that each launch-critical invariant is covered by the story that introduced it,
So that the readiness report maps requirements to verification without deferring most tests to the end.

**Acceptance Criteria:**

**Given** setup, invocation, proposal, posting, status, audit, and readiness stories are complete
**When** final evidence is collected
**Then** each FR, NFR, architecture decision, and UX-DR maps to the story and test path that introduced or verifies it
**And** this story does not become the first place where aggregate purity, authorization, proposal immutability, idempotency, tenant isolation, context-too-large blocking, provider-secret non-disclosure, FrontComposer conformance, or audit completeness is tested.

**Given** unresolved launch or governance decisions remain
**When** the final readiness report is produced
**Then** the decisions remain explicit blockers or accepted risks with owners
**And** no pending or missing evidence is reported as launch success.
```

Rationale: Conformance tests should live near the behavior they verify; the final story should package evidence and catch traceability gaps.

## 6. Implementation Handoff

Scope classification: **Moderate**.

Reason: This requires backlog reorganization, UX amendments, and cross-module prerequisite verification, but not PRD scope reduction, architecture rework, or implementation rollback.

Handoff recipients:

- Product + UX: resolve OQ-1; decide OQ-3; update UX triage, component contracts, parity matrix, accessibility/governance rules, and automatic-mode review.
- Architect: confirm no architecture changes beyond traceability note; verify AD-6 membership prerequisite and AD-16/AD-17 story mapping.
- Product Owner / Developer agent: revise `epics.md` with story splits and prerequisites.
- Governance/Product: resolve OQ-8 before content-bearing audit work.
- Developer agent: implement only stories whose prerequisites are closed; keep blocked stories out of active sprint execution.

Success criteria:

- `epics.md` contains the new prerequisite/decision/split stories.
- UX source no longer treats architecture-resolved OQ-2/OQ-7 as open.
- Story 2.6b is not implementable until OQ-1 is closed.
- Posting stories cannot start until Conversations membership seam is verified/exposed.
- Expiry and content-bearing audit are explicitly gated.
- Story 4.5 is narrowed to final evidence instead of holding most conformance work.
- Implementation readiness is rerun and returns READY or READY WITH KNOWN EXCEPTIONS.

## 7. Approval Status

This proposal was approved by Administrator on 2026-06-23.

Approved routing:

- Change scope: Moderate.
- Route to: Product Owner / Developer agents for backlog reorganization, with Product/UX, Architect, and Governance participation on the gated decisions.
- Deliverables: this approved Sprint Change Proposal plus the backlog reorganization plan described in Sections 5 and 6.
- Sprint-status note: no Agents `sprint-status.yaml` exists yet under `_bmad-output/implementation-artifacts`; create or update it after the revised stories are accepted.
