---
title: Sprint Change Proposal - Live Integration-Test Tier Activation
status: approved
created: 2026-08-04
updated: 2026-08-04
mode: Batch
change_scope: moderate
recommended_path: direct-adjustment
project: agents
owner: Administrator
approval_required: false
approved_by: Administrator
approved_on: 2026-08-04
builds_on:
  - sprint-change-proposal-2026-08-03.md
  - sprint-change-proposal-2026-08-04.md
trigger_artifacts:
  - ../implementation-artifacts/epic-4-retro-2026-06-25.md
  - ../implementation-artifacts/4-5-governance-conformance-report.md
proposed_artifact_changes:
  - epics.md during the approved 44-story canonical materialization
  - architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
  - ../implementation-artifacts/sprint-status.yaml
  - eng verification catalog created by target Story 5.1
  - test/Hexalith.Agents.IntegrationTests created by target Story 5.6
preserves:
  - PRD V1 scope and 28/28 Functional Requirement coverage
  - approved 44-story target graph across Epics 5-10
  - Dapr Workflow as the sole V1 durable execution owner
  - MCP, A2A, tools, and tool schemas as deferred beyond V1
  - completed Epic 4 report and retrospective as historical evidence
---

# Sprint Change Proposal: Live Integration-Test Tier Activation

## 1. Issue Summary

Epic 4 retrospective Action 4 requires `Hexalith.Agents.IntegrationTests` to be created when the first deferred seam becomes live and requires every Story 4.5 report section 6 row to receive live integration coverage when its seam binds. The action remains open, the project does not exist, and `src/Hexalith.Agents.Server/Application/Workflows` plus `src/Hexalith.Agents.Server/Projections` still contain only `.gitkeep` placeholders.

The action is directionally correct but is not executable against current planning authority:

- `sprint-status.yaml` says it is tracked across superseded Stories 5.3-5.18.
- The approved target is the 44-story Epic 5-10 graph in `sprint-change-proposal-2026-08-03.md`, pending canonical materialization through the approved 2026-08-04 recovery proposal.
- The 2026-06-25 report predates the corrected architecture. AD-18 now assigns sole V1 durable ownership to Dapr Workflow, not Agent Framework workflow/session state. AD-19 now places MCP, A2A, tools, and tool schemas outside V1.
- The architecture Structural Seed already names `test/Hexalith.Agents.IntegrationTests/`, but no story states exactly when the project appears, which row each story closes, or which persisted end state each live test must assert.

### Trigger Evidence

| Evidence | Current fact | Consequence |
| --- | --- | --- |
| Epic 4 retro Action 4 | Create the live tier on the first binding and cover every section 6 row | The obligation is accepted and still open |
| Story 4.5 report section 6 | Five deferred-with-seam rows name the required re-verification | These rows need a current-authority mapping, not deletion or silent reinterpretation |
| Current repository | No IntegrationTests project; workflow and projection implementation folders are placeholders | No live seam is currently being claimed, so creating empty or mocked live tests now would be false evidence |
| Approved target Story 5.6 | Persists readiness observations and binds the first authoritative projection | This is the first section 6 seam that becomes live and therefore the project-creation trigger |
| AD-17 and repository test rules | Live evidence must be explicit; integration tests assert persisted state-store/read-model end state | HTTP status, mock calls, or project existence alone cannot close the action |

### Problem Classification

This is a verification-planning gap discovered during sprint execution. It is not a new product requirement, MVP change, architecture pivot, or request to implement the live seams now.

## 2. Impact Analysis

### Epic And Story Impact

No new epic or story is required. The approved target graph remains viable and gains explicit live-test obligations:

| Target story | Live seam / obligation | Integration-tier effect |
| --- | --- | --- |
| 5.1 Establish Executable Verification Harness And Backlog Gates | Verification catalog only; no live seam | Add the deferred/live seam matrix and enforcement rule, but do not create an empty integration project |
| 5.6 Persist Readiness Observations And Authoritative Projection | First live read-model/projection binding | Create `test/Hexalith.Agents.IntegrationTests`, add it to the `.slnx`, and land projection freshness/stale/degraded tests |
| 6.1 Start Durable Automatic Interaction | Dapr Workflow ownership, restart, replay | Add single-owner and persisted restart/replay integration tests; reinterpret the historical workflow/session row under current AD-18 authority |
| 6.8 Invoke The Provider And Reconcile The Reservation | Live generation retry and Provider outcome recovery | Add no-duplicate Provider attempt/version/reservation/reconciliation tests; add Agent Framework session-restore coverage only if the committed adapter actually binds session state |
| 6.9 Join And Post Exactly Once As `hexa` | Live membership/posting retry | Add no-duplicate membership/message tests and assert Conversations plus Agents persisted end state |
| 6.10 Qualify Full-Path Recovery And Multi-Replica Safety | Cross-slice regression | Run the full integration project across crash/replay/replica cases; it does not replace the story-local tests above |
| Future approved AD-19 story | MCP/A2A/tool-schema seam | Flip the preserved matrix row to live and add real `tools/list`, schema, authorization, and side-effect conformance tests in the same change |

### Artifact Conflicts

| Artifact | Current conflict | Required adjustment |
| --- | --- | --- |
| Approved 44-story target graph | No explicit section 6 activation map | Add acceptance/evidence obligations to target Stories 5.1, 5.6, 6.1, 6.8, 6.9, and 6.10 during canonical materialization |
| `ARCHITECTURE-SPINE.md` AD-17 | Names the project and broad test areas but not the bind-and-test atomicity rule | Add one invariant requiring a live seam and its integration test to land together |
| `sprint-status.yaml` Action 4 | References superseded Story 5.3-5.18 range | Replace the comment with exact target-story ownership and completion conditions |
| Story 4.5 report / Epic 4 retro | Historical wording assumes possible Agent Framework workflow ownership and V1 protocols | Preserve both artifacts unchanged; map them through the current matrix rather than rewriting historical evidence |
| PRD | No conflict | No change |
| UX spines | No conflict | No change |

### Technical Impact

The integration tier must:

- use xUnit v3 and Shouldly and run as its own test project;
- consume shared EventStore/platform testing capabilities without creating a module-owned AppHost, Aspire, or ServiceDefaults project;
- exercise real live component boundaries for the seam under test, not only substitutes or in-memory call counts;
- assert persisted EventStore/state-store/read-model end state, plus contract headers or topology state where relevant;
- keep tenant-isolation negative evidence attached to every affected live path;
- fail honestly when required live dependencies are unavailable; a conditional skip, placeholder, or mocked seam cannot satisfy a story's live Evidence Level;
- remain outside package output and production runtime dependencies.

## 3. Recommended Approach

Use a direct adjustment to the already approved 44-story plan.

Story 5.1 establishes a machine-readable seam matrix and verifier rule. Story 5.6 creates the project because it is the first report row to become live. Later binding stories add their live tests atomically. The historical tool-schema row remains `DeferredOutOfV1`; a guard keeps it visible and fails any future tool-surface change that omits the matching live tests.

### Options Considered

| Option | Viability | Assessment |
| --- | --- | --- |
| Direct adjustment | Selected | No product or story-graph change; adds precise test ownership at the correct binding stories |
| Create the project immediately | Not selected | Would create an empty or substitute-backed tier before any live seam exists and could be mistaken for Evidence Level 4 |
| Add a standalone test story | Not selected | Separates the seam from its proof and violates the requirement that the test lands the day the seam goes live |
| Rewrite the historical report | Not selected | Would erase accurate historical evidence and obscure the later AD-18/AD-19 corrections |
| MVP review or rollback | Not viable | Product scope is unaffected and no completed implementation must be reverted |

### Effort, Risk, And Timeline

- Planning effort: low; focused story/evidence amendments during the already approved canonical synchronization.
- Implementation effort: medium and distributed across the binding stories rather than added as a new story.
- Technical risk: medium because live Dapr/EventStore/Provider/Conversations fixtures are environment-sensitive.
- Timeline impact: no new story count; each binding story must include its integration-test work in its estimate and cannot complete on unit or contract evidence alone.

## 4. Detailed Change Proposals

### 4.1 Target Story 5.1 — Verification Harness

**Section:** Story 5.1 Bootstrap / verification catalog

**OLD:**

> Create a versioned machine-readable catalog covering all 44 active story IDs and fail on unknown stories, missing lanes, missing artifact targets, or missing negative-evidence targets.

**NEW:**

> Extend the catalog with a versioned `liveSeams` matrix containing every Story 4.5 report section 6 row. Each entry records `SeamId`, historical row, current architecture authority, `BindingStatus` (`Deferred`, `DeferredOutOfV1`, or `Live`), owning target story, required test names, required live fixture, persisted end-state oracle, and evidence reference. A `Deferred` entry may omit a test file while the seam remains absent. Any change that makes a seam `Live` must in the same change create or update `test/Hexalith.Agents.IntegrationTests`, register and run the named lane, and provide a passing evidence reference. A detected live implementation with a deferred row, a live row without a test, an unknown row, or a passing result based on skip/placeholder/mock-only evidence fails verification.

**Rationale:** Establishes enforceable timing without manufacturing a live test project before the first binding.

### 4.2 Target Story 5.6 — First Binding And Project Creation

**Section:** Acceptance Criteria and Evidence Manifest

**OLD:**

> Persist readiness observations and publish an authoritative replay-safe projection with stale/unknown behavior and projection truth.

**NEW:**

> Because this story binds the first live section 6 seam, create `test/Hexalith.Agents.IntegrationTests/Hexalith.Agents.IntegrationTests.csproj`, add it to `Hexalith.Agents.slnx`, and register its project-level lane in the Story 5.1 verifier catalog. Through a live EventStore/read-model fixture, persist a readiness observation, project it, query it through the supported public boundary, and assert the persisted read-model value, greatest committed revision, projection version, and freshness metadata. Cover current, stale, unknown/degraded, exact duplicate, conflicting duplicate, and replay delivery. HTTP success or handler call count alone is insufficient. Flip `LIVE-PROJECTION` to `Live` with the named passing tests and evidence reference.

**Rationale:** Story 5.6 is the earliest target story that changes a report row from deferred to live.

### 4.3 Target Story 6.1 — Durable Owner And Restore/Replay

**Section:** Acceptance Criteria and Evidence Manifest

**OLD:**

> Start one Dapr Workflow as the sole durable owner and prove deterministic activities and replay.

**NEW:**

> Add live integration tests that start the same accepted interaction identity through the platform-composed Dapr Workflow boundary, inject restart before and after each owned checkpoint, restore from persisted execution plus EventStore state, and prove one interaction, one workflow owner, one command/effect identity per step, and no duplicate timer/version/terminal outcome. Assert persisted Agents business state and required projection end state after recovery. Prove no `IHostedService`, `BackgroundService`, Microsoft Agent Framework workflow, Python DurableAgent, MCP/A2A worker, or second Dapr workflow can claim the same lifecycle. Flip `LIVE-DURABLE-OWNER` and the current Dapr restart/replay interpretation of `LIVE-SESSION-RESTORE` to `Live`.

> If `EXT-PROVIDER-1` later selects Microsoft Agent Framework session persistence inside the generation activity, Story 6.8 must add a separate live Agent Framework session checkpoint/restore test before that optional session seam is considered bound. Dapr Workflow remains the durable owner regardless.

**Rationale:** Preserves the historical session-restore intent while obeying corrected AD-18 ownership.

### 4.4 Target Story 6.8 — Generation Retry Idempotency

**Section:** Acceptance Criteria and Evidence Manifest

**OLD:**

> Invoke the Provider after real admission/fencing and reconcile the reservation exactly once.

**NEW:**

> Add live retry tests for failures before transport, after Provider acceptance but before outcome persistence, and during outcome recovery. Reuse the same AttemptId, descriptor fingerprint, reservation, admission/fence, Provider idempotency key, and safety floor; assert exactly one Provider attempt, one generated version or safe terminal failure, one reservation, one reconciliation, and no double charge in persisted state. Any capability/readiness/fingerprint/fence change must block under the existing AttemptId. When Agent Framework session state is actually selected and bound, restore its checkpoint in the same fixture and prove it cannot duplicate Provider work or become business truth. Update the retry and optional session rows in the seam matrix.

**Rationale:** Converts the historical generation-retry seam contract into live external-effect evidence.

### 4.5 Target Story 6.9 — Posting Retry Idempotency

**Section:** Acceptance Criteria and Evidence Manifest

**OLD:**

> Establish limited AI membership and post exactly one deterministic Conversation Message as `hexa`.

**NEW:**

> Add live integration tests for retry/replay and crash before/after membership and append acceptance. Assert exactly one limited `AiAgent`/`Member` membership result, exactly one persisted Conversation Message with deterministic MessageId/idempotency key, exactly one Agents posting outcome, and complete persisted audit linkage. Cross-tenant retries must deny before membership/message mutation and disclose no target existence. Flip the posting half of `LIVE-RETRY-IDEMPOTENCY` to `Live` only when these tests pass against the Available `EXT-CONV-AI-1` target.

**Rationale:** A successful HTTP response or mocked append is not proof that retry/recovery avoided duplicate durable Conversation state.

### 4.6 Target Story 6.10 — Full-Tier Regression

**Section:** Evidence Manifest

**OLD:**

> Qualify full-path recovery and multi-replica safety across completed prior slices.

**NEW:**

> Run the complete `Hexalith.Agents.IntegrationTests` non-performance lane across the production-like restart, replay, multi-replica, reservation, generation, projection, membership, and posting paths. Verify every matrix entry whose `BindingStatus` is `Live` has a passing current evidence reference. This regression supplements but never substitutes for the atomic story-local tests that first made each seam live.

**Rationale:** Prevents later topology work from silently invalidating the first binding evidence.

### 4.7 AD-17 — Bind-And-Test Atomicity

**Artifact:** `ARCHITECTURE-SPINE.md`

**OLD:**

> Tests cover aggregate purity, authorization fail-closed paths, immutable proposal versions, Dapr Workflow replay/restart/idempotency, generation/posting retry identity, tenant isolation, complete-context blocking, Provider/tokenizer/safety/secrets/cost/capacity gates, FrontComposer UI conformance and browser timing, retention/export/deletion, recovery, and audit completeness.

**NEW:**

> Preserve that sentence and append: “The versioned live-seam matrix maps every Story 4.5 report section 6 row to its current authority and binding status. The story that changes a seam from deferred to live must atomically add and execute the corresponding `Hexalith.Agents.IntegrationTests` coverage and assert persisted state-store/read-model end state. A live seam without its named passing test is non-conformant and blocks story completion. AD-19 protocol rows remain `DeferredOutOfV1` until a separately approved scope binds them; detecting a tool/protocol surface without flipping the row and adding its live conformance tests fails the gate.”

**Rationale:** Makes the retrospective action a lasting architecture/test invariant without bringing tools into V1.

### 4.8 Sprint Tracker Action

**Artifact:** `sprint-status.yaml`

**OLD:**

> `status: open  # Tracked across Stories 5.3-5.18; V1 excludes tool-schema conformance.`

**NEW:**

> `status: open  # Target Story 5.1 creates the enforced seam matrix; target Story 5.6 creates Hexalith.Agents.IntegrationTests on the first live projection binding; Stories 6.1, 6.8, 6.9, and 6.10 add/run durable-owner, restart/replay, retry, posting, and full-regression evidence. AD-19 remains DeferredOutOfV1 until separately approved. Mark done only after the project exists, every currently Live row passes with persisted end-state evidence, and the verifier blocks any future untested binding.`

**Rationale:** Replaces stale story ownership and defines an objective closure condition.

### 4.9 Protocol Row Disposition

The historical MCP/A2A/tool-schema row is not removed, waived, or marked covered. Its current matrix record is:

| Field | Value |
| --- | --- |
| SeamId | `FUTURE-TOOL-PROTOCOL-CONFORMANCE` |
| Current authority | AD-19 - Future Tool And Remote-Agent Protocol Boundaries |
| BindingStatus | `DeferredOutOfV1` |
| V1 owner | None |
| Trigger | A separately approved story adds MCP, A2A, tools, or a tool-schema surface |
| Same-change obligation | Add real tool discovery/schema serialization, authorization/tenant gate, idempotency/correlation, malformed-input, compatibility, and prohibited-side-effect integration tests |
| Guard | Existing/runtime scan fails if a tool/protocol surface appears while this row remains deferred |

## 5. Implementation Handoff

### Scope Classification

**Moderate.** The adjustment does not change MVP scope, epic outcomes, story count, or architecture ownership. It changes acceptance/evidence obligations across multiple target stories and requires Product Owner, Test Architect, Developer, and Architect coordination during the already approved canonical materialization.

### Recipients And Responsibilities

- **Product Owner / Scrum Master:** apply these amendments while materializing the approved 44-story graph; do not create a third graph or renumber the target stories.
- **Test Architect (Murat):** own the seam matrix, live-fixture definition, test names, persisted end-state oracles, and evidence classification.
- **Developer agent:** create the project in Story 5.6 and add each later live test atomically with its binding implementation.
- **Solution Architect:** validate the historical-to-current AD-18/AD-19 mapping and prevent Agent Framework session state from becoming orchestration or business truth.
- **External dependency owners:** provide Available targets and passing compatibility commands before their seams execute; missing dependencies block stories rather than causing skipped passing tests.

### Sequenced Handoff

1. Include the matrix schema and story amendments in the approved canonical planning synchronization.
2. Story 5.1 implements the verifier/matrix rules without creating an empty live tier.
3. Story 5.6 creates the integration project and closes the first live projection row.
4. Stories 6.1, 6.8, and 6.9 add their tests in the same changes that bind their seams.
5. Story 6.10 runs the complete current live-row regression.
6. A future AD-19 scope must flip and test the protocol row atomically; it is not part of V1.

### Success Criteria

1. The approved 44-story graph contains the exact target-story amendments above and no additional story.
2. Story 5.1's verifier rejects live-without-test, unknown-row, missing-oracle, skipped-pass, and mock-only-live evidence.
3. `Hexalith.Agents.IntegrationTests` is absent before the first binding and is created in Story 5.6 with a passing live projection test.
4. Every currently live matrix row has at least one passing named integration test and a persisted end-state assertion.
5. Dapr Workflow is proven as the sole V1 durable owner across restart/replay; optional Agent Framework session state, if selected, is tested without becoming business truth.
6. Live generation and posting retries produce no duplicate Provider attempt, version, reservation/reconciliation, membership, Message, or terminal outcome.
7. The tool-schema row remains visibly deferred and the guard prevents any future protocol surface from landing without its live tests.
8. Integration lanes run per project and no skip, placeholder, HTTP-only assertion, or mock call count is accepted as live evidence.

## 6. Change Navigation Checklist

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 Triggering story | [x] | Epic 4 retrospective Action 4, originating from Story 4.5 report section 6 |
| 1.2 Core problem | [x] | Accepted quality action lacks current target-story ownership and conflicts with corrected AD-18/AD-19 terminology |
| 1.3 Evidence | [x] | Historical report/retro, current repository placeholders, missing test project, current architecture, approved 44-story proposal, tracker comment |
| 2.1 Current epic viability | [x] | Approved Epic 5-10 outcomes remain viable |
| 2.2 Required epic changes | [!] | Amend evidence obligations in Stories 5.1, 5.6, 6.1, 6.8, 6.9, and 6.10 |
| 2.3 Remaining epic impact | [x] | No effect beyond those existing stories and future AD-19 trigger enforcement |
| 2.4 New/obsolete epics | [N/A] | No new epic or story; no approved outcome becomes obsolete |
| 2.5 Order/priority | [x] | Matrix first, project on first binding, later tests atomic with later bindings |
| 3.1 PRD conflict | [N/A] | No product or MVP edit |
| 3.2 Architecture conflict | [x] | Current AD-18/AD-19 authority resolves historical wording; AD-17 gains bind-and-test atomicity |
| 3.3 UX conflict | [N/A] | No UX change |
| 3.4 Other artifacts | [!] | Target epics, tracker action, verifier catalog, solution, and new test project require coordinated changes |
| 4.1 Direct adjustment | Viable / selected | Low planning effort, medium distributed implementation effort, medium fixture risk |
| 4.2 Rollback | Not viable | No completed work resolves the missing live tier by rollback |
| 4.3 PRD MVP review | Not selected | MVP scope is unchanged |
| 4.4 Recommended path | [x] | Amend the approved target graph and enforce atomic live binding plus test |
| 5.1 Issue summary | [x] | Section 1 |
| 5.2 Epic/artifact impact | [x] | Sections 2 and 4 |
| 5.3 Recommended path | [x] | Section 3 |
| 5.4 MVP impact/action plan | [x] | No MVP impact; sequenced handoff in Section 5 |
| 5.5 Agent handoff | [x] | PO/SM, Test Architect, Developer, Architect, external owners |
| 6.1 Checklist review | [x] | All applicable analysis items are addressed |
| 6.2 Proposal accuracy | [x] | Reconciled against current repository evidence and approved 2026-08-03/08-04 planning authority |
| 6.3 Explicit approval | [x] | Administrator approved the complete proposal on 2026-08-04 |
| 6.4 Sprint status | [N/A] | Tracker edit is proposed, not applied before approval/canonical synchronization |
| 6.5 Handoff confirmation | [x] | Moderate-scope implementation handoff is routed to Product Owner / Scrum Master, Test Architect, Developer, and Solution Architect |

## 7. Approval Record

- Review mode: Batch.
- Complete-proposal review: Continued by Administrator on 2026-08-04.
- Proposal state: Approved.
- Approval: Explicitly granted by Administrator on 2026-08-04.
- Scope classification: Moderate.
- Routed to: Product Owner / Scrum Master, Test Architect, Developer, and Solution Architect.
- Authorization boundary: Approval authorizes planning-artifact synchronization and later story-scoped implementation. It does not bind an external dependency, fabricate live evidence, add V1 tool/protocol scope, or authorize immediate broad implementation.

## 8. Workflow Execution Log

- 2026-08-04 - Correct Course activated in Batch mode; repository instructions, customized workflow, persistent project facts, and BMM configuration loaded.
- 2026-08-04 - Canonical PRD, Epics, Architecture Spine/conventions, UX spines, Story 4.5 report, Epic 4 retrospective, sprint tracker, approved target-graph proposal, and current recovery proposal assessed.
- 2026-08-04 - Repository inspection confirmed the IntegrationTests project is absent and live workflow/projection implementations remain deferred.
- 2026-08-04 - Direct adjustment selected; immediate empty-project creation, standalone test story, historical rewrite, rollback, and MVP review rejected.
- 2026-08-04 - Batch proposal written to a unique file because the date-default proposal path already contains a separate approved recovery proposal.
- 2026-08-04 - Administrator continued the complete-proposal review and explicitly approved the proposal for implementation.
- 2026-08-04 - Moderate-scope handoff routed to Product Owner / Scrum Master, Test Architect, Developer, and Solution Architect; canonical synchronization and story-scoped implementation remain the recipients' next actions.
