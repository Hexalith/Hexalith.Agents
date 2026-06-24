---
baseline_commit: 1f9b149158ea789f540498df8e81ef2ca5fd7a9f
---

# Story 3.5: Approve A Selected Version And Post It

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an Approver,
I want to approve exactly one selected proposal version,
so that only the reviewed response becomes a Conversation Message as `hexa`.

**Epic:** Epic 3 - Proposal Review And Approval Workflow.
**FR coverage:** FR17 (approve exactly one selected proposal version for posting as `hexa`) plus FR14 version preservation, FR20 authorization before side effects, FR21 fail-closed dependency handling, and FR24 audit evidence linkage.
**Position:** Fifth story in Epic 3. It depends on Story 3.1 proposal creation, Story 3.2 discovery, Story 3.3 edit versions, Story 3.4 regeneration versions, and Story 2.5 automatic posting infrastructure.

## Acceptance Criteria

1. **Approval records exactly one selected version.**
   - **Given** a proposal is pending and contains one or more preserved versions
   - **When** an authorized Approver selects a version and approves it
   - **Then** the system records the approved `VersionId`, Approver, approval timestamp, policy basis, and posting-pending state
   - **And** no other proposal version is eligible to post for that approval.

2. **The approved version is posted as `hexa`.**
   - **Given** approval is recorded
   - **When** the approved version is posted to Conversations
   - **Then** the Conversation Message is attributed to the Agent Party identity
   - **And** Audit Evidence links the approved version, Approver, approval timestamp, AgentInteraction, Source Conversation, Provider/model, and posted Conversation Message.

3. **Posting retries are idempotent.**
   - **Given** posting is retried
   - **When** the same approved version is used
   - **Then** deterministic `MessageId` and idempotency key prevent duplicate Conversation Messages
   - **And** posting outcome remains auditable.

4. **Approval and posting fail closed before side effects.**
   - **Given** the selected version fails final authorization, safety, Party, Conversation, Provider, tenant, or membership checks
   - **When** approval or posting is attempted
   - **Then** the system fails closed before Conversation side effects
   - **And** status distinguishes approval failure from posting failure where applicable.

## Tasks / Subtasks

> Implement in the same additive pattern as Stories 3.3 and 3.4: Contracts -> Domain aggregate + twin policy -> Server orchestrator -> UI gateway/control -> tests. Reuse Story 2.5 posting ports and idempotency; do not write directly to Conversation streams.

- [x] **Task 1 - Contracts: approval/posting surface** (AC: #1, #2, #3, #4)
  - [x] Append approval/posting statuses after `ProposalRegenerationFailed` in `AgentInteractionStatus`: recommended `ProposalApproved`, `ProposalPostingPending`, `ProposalPosted`, `ProposalPostingFailed`, and `ProposalApprovalFailed`. Preserve all existing ordinals.
  - [x] Append `Approved`, `PostingPending`, `Posted`, and `PostingFailed` after `Regenerated` in `ProposedAgentReplyState`. Approved is not posted; keep the distinction visible in names and documentation.
  - [x] Add approval outcome/failure/rejection enums mirroring edit/regeneration conventions: include safe `Unknown=0`, `Approved`, authorization/policy failures, selected-version invalid/missing, posting dependency failures, posting adapter failure, and idempotent retry states.
  - [x] Add a write-path approval result/carrier that identifies the selected `VersionId`, Approver `PartyId`, approval policy version/basis, source conversation, Agent Party identity, deterministic message/idempotency keys, and safe posting outcome fields. Generated content must not appear in safe evidence or status DTOs.
  - [x] Add `Commands/ApproveProposedAgentReply.cs`, approval/posting events, and rejection events. Successful posting event may reference the posted Conversation Message id but must not embed provider payloads or unrelated Conversation data.
  - [x] Add safe audit-evidence query/view contracts for approval and posting. Live projection/query handler remains Epic 4 scope.

- [x] **Task 2 - Domain: approval policy, aggregate handler, and replay** (AC: #1, #3, #4)
  - [x] Add an approval policy twin beside `AgentProposalEditPolicy` and `AgentProposalRegenerationPolicy`. `Evaluate` and `Decide` must share one private computation path.
  - [x] Add the aggregate handler after the Story 3.4 regeneration handler. Use the same CA1062-safe positive bind pattern.
  - [x] Preconditions: proposal must exist and `ProposalState` must be retryable/approvable `{ Pending, Edited, Regenerated }`; terminal states reject and must not emit posting events.
  - [x] Validate the selected `VersionId` exists in `GeneratedVersions` and is one of the preserved generated/edited/regenerated versions. Approval freezes that exact version for posting; later versions are not eligible for that approval.
  - [x] State replay must record selected approved version id, Approver party, policy basis/version, posting status, deterministic message id/idempotency key, posted Conversation Message id when available, and safe posting failure reason.
  - [x] Idempotency: approving the same already-approved version with the same deterministic approval/posting identity should be `NoOp()` or an idempotent success; a different selected version after approval must reject unless the proposal is explicitly reopened by a future story.

- [x] **Task 3 - Server: approval and posting orchestrator** (AC: #1, #2, #3, #4)
  - [x] Add `AgentInteractionProposalApprovalOrchestrator` under `src/Hexalith.Agents.Server/Application/AgentInteractions/`.
  - [x] Reuse Story 3.3/3.4 authorization: resolve the snapshotted Approver Policy with `IApproverPolicyResolver`, evaluate `ApproverPolicyVerdict`, and fail closed before Conversation side effects on non-`Valid` verdicts. `OperationCanceledException` must propagate.
  - [x] Reuse Story 2.5 posting infrastructure and ports for Agent Party identity, membership/conversation posting, deterministic message id, and idempotency key. Do not introduce provider SDK details into contracts/domain.
  - [x] Resolve/read the exact selected version. Current code intelligence says `IAgentGeneratedVersionReader` is oriented to latest/only generated output from the automatic path; Story 3.5 must either extend/reuse a version reader to select by version id or introduce an Agents-owned safe version resolver. Do not silently post "latest" when a selected version id was approved.
  - [x] Posting must be two-phase from the user's perspective: approval recorded -> posting pending -> posted or posting failed. Approved must never be treated as already posted.
  - [x] Build safe outcome DTOs only: ids, status, reason, and safe references. No generated content in UI/status/audit-safe DTOs.
  - [x] Register the orchestrator in `Program.cs` in a `// Story 3.5` block.

- [x] **Task 4 - UI: approval gateway and reusable control** (AC: #1, #2, #4)
  - [x] Add approval gateway/request/result DTOs under `src/Hexalith.Agents.UI/Services/Gateways/` following `ProposalEditor` and `ProposalRegenerator` patterns. Deferred gateway must fail closed and not expose content.
  - [x] Add presentation labels/badges for `Approved`, `PostingPending`, `Posted`, and `PostingFailed`. Approved must have distinct copy from posted.
  - [x] Add a reusable approval action/control suitable for Story 3.7 hosting. It accepts a selected version id, calls the gateway, exposes safe statuses, supports host refresh callbacks, and never styles a proposal as a Conversation Message until posting is confirmed.
  - [x] Add English/French whole-string resource keys for approval action, selected-version copy, posting-pending/posted/posting-failed states, and denial/error states.
  - [x] Scope boundary: do not build the full proposal detail workspace, version-history panel, keyboard focus contract, or audit evidence page here; Story 3.7 and Epic 4 host those surfaces.

- [x] **Task 5 - Tests across all layers** (AC: #1, #2, #3, #4)
  - [x] Contracts tests: marker interfaces, JSON round trips, enum-by-name serialization, ordinal stability, no-leak assertions for failures/rejections/evidence/outcomes, and selected-version id preservation.
  - [x] Domain tests: authorized approval records selected version and posting-pending state; terminal/non-pending proposals reject; missing/unknown version rejects; prior versions remain preserved; approval of one version prevents a different version from posting; same deterministic retry is idempotent; `Evaluate`/`Decide` no-drift over every outcome.
  - [x] Server tests: happy path records approval and dispatches/posting through Conversations exactly once; unauthorized/non-valid policy fails before posting; missing/disabled Agent Party identity, membership/conversation failure, tenant mismatch, selected-version missing, posting adapter failure, and retry all map safely; `OperationCanceledException` propagates.
  - [x] UI tests: deferred gateway fail-closed, approval control happy/failure statuses, selected-version id passed through, no content in accessible names/test ids/status strings, presentation totality, localization parity, and host refresh events.
  - [x] Run from `Hexalith.Agents/`: `dotnet restore Hexalith.Agents.slnx`; `dotnet build Hexalith.Agents.slnx --configuration Release -m:1`; run each touched test project individually. If VSTest socket permission fails, run built xUnit v3 executables directly.

- [x] **Task 6 - Dev Agent Record accuracy**
  - [x] Regenerate test counts from actual output. Story 3.4 ended at Contracts 274, Domain 604, Server 315, UI 484 after review.
  - [x] Diff the File List against `git status` before review. Include every created/modified production file, test file, story file, sprint-status entry, and test-summary artifact.

## Dev Notes

### Critical guardrails

- **Reuse the existing `AgentInteraction` aggregate.** Approval belongs on the proposal lifecycle already implemented by Stories 3.1 through 3.4; do not create a new proposal aggregate.
- **Approved is not posted.** UX and PRD explicitly distinguish approved, posting pending, posted, and posting failed. Do not surface approval as successful Conversation posting until Conversations returns the final message id.
- **Post exactly the selected version.** The approved `VersionId` is the unit of approval. Do not post the latest version by convenience if the approved id is older.
- **Side effects stay in server orchestration.** Aggregate/domain remains pure. Conversation membership checks, Agent Party identity checks, selected-version reads, and posting happen outside the aggregate, then one trusted command/result is dispatched.
- **Fail closed before Conversation side effects.** Authorization, tenant, Party identity, membership, selected-version existence, and Conversation access must pass before posting. Unsafe or uncertain states emit safe failure/rejection outcomes only.
- **Use deterministic posting identity.** Follow Story 2.5: message id and idempotency key derive from stable AgentInteraction/approved-version identity. Retries must not duplicate Conversation messages.
- **Content confinement.** Approved/generated content may only travel through the write path needed to post. Safe evidence/status/UI strings must contain ids and coarse reasons only.

### Existing implementation anchors

- Proposal creation/discovery/edit/regeneration:
  - `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs`
  - `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs`
  - `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentProposalEditPolicy.cs`
  - `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentProposalRegenerationPolicy.cs`
  - `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalEditOrchestrator.cs`
  - `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalRegenerationOrchestrator.cs`
- Automatic posting infrastructure from Story 2.5:
  - `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/ConversationClientResponsePoster.cs`
  - `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentResponseMessageIdentity.cs`
  - `Hexalith.Agents/src/Hexalith.Agents.Server/Ports/IAgentCommandDispatcher.cs`
  - `Hexalith.Agents/src/Hexalith.Agents.Server/Ports/IAgentGeneratedVersionReader.cs`
- UI patterns:
  - `Hexalith.Agents/src/Hexalith.Agents.UI/Components/Shared/ProposalEditor.razor`
  - `Hexalith.Agents/src/Hexalith.Agents.UI/Components/Shared/ProposalRegenerator.razor`
  - `Hexalith.Agents/src/Hexalith.Agents.UI/Services/Gateways/DeferredProposalEditGateway.cs`
  - `Hexalith.Agents/src/Hexalith.Agents.UI/Services/Gateways/DeferredProposalRegenerationGateway.cs`

### Project Structure Notes

- Contracts go under `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/` with commands/events/rejections/queries in the established subfolders.
- Domain policy and aggregate logic go under `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/`.
- Server orchestrator/identity/request DTOs go under `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/`.
- UI gateway/control/presentation/localization changes go under `Hexalith.Agents/src/Hexalith.Agents.UI/Services/Gateways/`, `Components/Shared/`, and both `AgentsResources.resx` files.
- Use xUnit v3, Shouldly, NSubstitute where existing tests use it, and bUnit for Blazor UI tests. Avoid raw `Assert.*`.

### Previous Story Intelligence

- Story 3.4 review fixed edit/regenerate symmetry: a regenerated proposal remains editable before approval. Preserve that invariant while adding approval terminal/posting states.
- Story 3.4 ended with `AgentInteractionStatus` values through `ProposalRegenerationFailed` and `ProposedAgentReplyState` through `Regenerated`; append new enum values only.
- Story 3.4 test counts after review: Contracts 274, Domain 604, Server 315, UI 484.
- Story 2.5 established deterministic Conversation posting and idempotency. Reuse those patterns rather than inventing new Conversation write mechanics.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 3.5: Approve A Selected Version And Post It`
- `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-17: Approve Proposed Reply`
- `_bmad-output/implementation-artifacts/2-5-post-automatic-responses-through-conversations.md`
- `_bmad-output/implementation-artifacts/3-1-create-proposed-agent-replies-in-confirmation-mode.md`
- `_bmad-output/implementation-artifacts/3-3-edit-proposed-reply-versions.md`
- `_bmad-output/implementation-artifacts/3-4-regenerate-proposed-reply-versions.md`

## Create-Story Checklist Validation

- [x] Target story determined from sprint status and orchestration: `3-5-approve-a-selected-version-and-post-it`.
- [x] Epic 3 and PRD FR-17 reflected in ACs/tasks.
- [x] Previous-story intelligence from Stories 2.5, 3.3, and 3.4 included.
- [x] Existing implementation anchors listed to prevent reinvention.
- [x] File locations, test projects, and build/test commands specified.
- [x] Scope boundaries called out for Story 3.7 and Epic 4.

## Dev Agent Record

### Agent Model Used

Codex fallback dev session (`model_reasoning_effort=high`) after the primary Claude dev attempt stalled before implementation; orchestrator recovery completed story bookkeeping after source, build, and tests passed.

### Debug Log References

- Build verification: `dotnet build` for `Hexalith.Agents.Contracts`, `Hexalith.Agents`, `Hexalith.Agents.Server`, `Hexalith.Agents.UI`, and all four touched test projects with `--configuration Release --no-restore -m:1` (0 warnings, 0 errors).
- Test verification: xUnit v3 executables run directly from `Hexalith.Agents/`.
- Dev counts before QA automation: Contracts 278, Domain 609, Server 318, UI 498; all errors 0, failed 0, skipped 0.
- QA automation added 42 focused gap tests and re-ran the same build/test pass.
- Final counts after QA automation: Contracts 278, Domain 629, Server 325, UI 513; all errors 0, failed 0, skipped 0.

### Completion Notes List

- Added approval/posting contract surface, safe evidence query/view contracts, approval outcomes, failure reasons, command, events, and structural rejection.
- Added aggregate approval handling and replay with exact selected-version validation, frozen approved version state, posting pending/posted/failed state transitions, idempotent retry behavior, and no-op same-post retry.
- Added server approval orchestrator that validates approver policy before side effects, reads the exact selected version, reuses deterministic Conversation posting identity/infrastructure, and maps failures safely.
- Added UI approval gateway, deferred fail-closed gateway, reusable `ProposalApprover` control, proposal-state badge presentation for approved/posting states, and English/French resource keys.
- Added focused contracts, domain, server, and UI tests covering selected-version preservation, fail-closed paths, idempotency, safe serialization/accessibility, and localization/presentation parity.
- QA automation added approval lifecycle E2E coverage, aggregate no-drift/posting-pending gaps, server fail-closed/OCE/idempotency gaps, UI deferred gateway and localization parity gaps, and wrote `_bmad-output/implementation-artifacts/tests/test-summary.md`.

### File List

- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionStatus.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/ProposedAgentReplyState.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposalApprovalFailureReason.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposalApprovalOutcome.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposalApprovalResult.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposedReplyApprovalEvidence.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposedReplyNotApprovableReason.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Commands/ApproveProposedAgentReply.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Events/ProposedAgentReplyApprovalFailed.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Events/ProposedAgentReplyApproved.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Events/ProposedAgentReplyPosted.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Events/ProposedAgentReplyPostingFailed.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Events/ProposedAgentReplyPostingPending.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Events/Rejections/ProposedAgentReplyNotApprovableRejection.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Queries/AgentProposalApprovalEvidenceResult.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Queries/AgentProposalApprovalEvidenceView.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Queries/GetAgentProposalApprovalEvidenceQuery.cs`
- `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs`
- `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs`
- `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentProposalApprovalPolicy.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalApprovalOrchestrator.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalApprovalRequest.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Server/Ports/DeferredAgentGeneratedVersionReader.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Server/Ports/IAgentGeneratedVersionReader.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Server/Program.cs`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Components/Shared/ProposalApprover.razor`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Components/Shared/ProposedAgentReplyStatePresentation.cs`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Resources/AgentsResources.fr.resx`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Resources/AgentsResources.resx`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Services/Gateways/AgentsUiServiceCollectionExtensions.cs`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Services/Gateways/DeferredProposalApprovalGateway.cs`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Services/Gateways/IProposalApprovalGateway.cs`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Services/Gateways/ProposalApprovalRequest.cs`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Services/Gateways/ProposalApprovalResult.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Contracts.Tests/AgentInteractionProposalApprovalContractsTests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Server.Tests/AgentInteractionProposalApprovalOrchestratorTests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Tests/AgentInteractionProposalApprovalAggregateTests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Tests/AgentInteractionProposalApprovalLifecycleE2ETests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Tests/AgentInteractionTestData.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.UI.Tests/AgentsTestContext.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.UI.Tests/BadgeConformanceTests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.UI.Tests/DeferredGatewayTests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.UI.Tests/LocalizationResourceTests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.UI.Tests/ProposalApproverTests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.UI.Tests/ProposedAgentReplyStatePresentationTests.cs`
- `_bmad-output/implementation-artifacts/3-5-approve-a-selected-version-and-post-it.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/story-automator/orchestration-1-20260623-194909.md`

## Senior Developer Review (AI)

**Reviewer:** Administrator (story-automator adversarial review) on 2026-06-24
**Outcome:** Approve — auto-fix applied.

### Verification performed

- **File List vs git reality:** Exact match. Every git-changed file (modified + untracked) appears in the Dev Agent Record File List, and every File List entry maps to a real git change. No undocumented changes, no phantom claims.
- **Acceptance Criteria:** All four ACs implemented and verified against code.
  - AC1 (approve exactly one version): `AgentInteractionAggregate.Handle(ApproveProposedAgentReply)` freezes `ApprovedVersionId`; `AlreadyApprovedDifferentVersion` rejects any other version (`DifferentVersionAlreadyApproved`).
  - AC2 (post as `hexa` + audit evidence): orchestrator reads the exact selected version, posts via the Agent Party identity, and records `AgentProposedReplyApprovalEvidence` (approved version, approver, source conversation, agent party, message id, posted message id). Approval/posting timestamps surface from EventStore metadata on `AgentProposalApprovalEvidenceView` (`ApprovedAt`/`PostedAt`). Provider/model are linked transitively via the AgentInteraction snapshot — consistent with the Story 2.5 posting-evidence precedent (`AgentPostedMessageEvidence`).
  - AC3 (idempotent retries): deterministic `MessageId`/`IdempotencyKey` derived from interaction + selected version; aggregate no-ops a re-approved posted version; orchestrator returns the same message id on retry.
  - AC4 (fail closed before side effects): orchestrator order is structural-state → authorization → version read → party identity → membership → append; every pre-append failure returns a safe outcome and never appends. `OperationCanceledException` propagates without dispatch.
- **Task audit:** All `[x]` tasks confirmed done. Test counts reproduced from a clean Release build + direct xUnit v3 runs: Contracts 278, Domain 629, Server 325, UI 513; 0 errors, 0 failed, 0 skipped — matching the Dev Agent Record.
- **Safety/no-leak:** Contracts/aggregate/orchestrator/UI surfaces carry ids + coarse reasons only; verified by contracts no-content-bearing-member test, the orchestrator `payload.ShouldNotContain(content)` assertion, and the E2E JSON round-trip no-leak check.
- **Localization:** EN/FR parity holds for all new `Agents.ProposalApprover.*` and `Agents.ProposalState.Label.*` keys.
- **DI:** Orchestrator and all five dependencies registered in the `// Story 3.5` block of `Program.cs`; UI gateway registered via `AddAgentsUi`.

### Findings and fixes (auto-fixed)

| # | Severity | Finding | Fix |
|---|----------|---------|-----|
| 1 | Low | `ProposedAgentReplyState` `<remarks>` stated the 3.5 states (`Approved`/`PostingPending`/`Posted`/`PostingFailed`) "must NOT be added here", directly contradicting the enum members this story added. | Updated the summary/remarks to record the Story 3.5 additions and narrow the deferral to the remaining Story 3.6 states (`Rejected`/`Abandoned`/`Expired`). |
| 2 | Low | `ProposedAgentReplyStatePresentation` XML docs (class summary + `ColorFor` + `IconFor`) claimed the 3.5 states "map through the Subtle/question-mark total default", but the switches now map them to explicit badge roles and icons. | Rewrote the three doc blocks to describe the explicit Story 3.5 role/icon mappings; only the `Unknown` sentinel/future-reserved states fall through the total default. |

No CRITICAL/HIGH/MEDIUM issues found. Both fixes are documentation-only (no behavioral change); solution rebuilds clean (0 warnings/0 errors) and all four suites remain green after the fix.
