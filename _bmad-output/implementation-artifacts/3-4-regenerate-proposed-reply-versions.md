---
baseline_commit: 2e14e68d7df54a82ddec747bb622c5f9fece5f0d
---

# Story 3.4: Regenerate Proposed Reply Versions

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an Approver,
I want to request regeneration before approval,
so that I can compare a new Agent version without losing earlier generated or edited content.

**Epic:** Epic 3 - Proposal Review And Approval Workflow.
**FR coverage:** FR14 (preserve every generated, edited, and regenerated version) and FR16 (authorized Approvers can regenerate a Proposed Agent Reply before approval).
**Position:** Fourth story in Epic 3. It builds directly on Story 2.4 generation/content-safety, Story 3.1 proposal creation, Story 3.2 proposal discovery, and Story 3.3 edit/version append.

## Acceptance Criteria

1. **Authorized regeneration creates a deterministic generation attempt.**
   - **Given** a proposal is pending and the current user is authorized to regenerate
   - **When** regeneration is requested
   - **Then** the system creates a new deterministic generation attempt linked to the same `AgentInteraction`
   - **And** the attempt uses the same Source Conversation and snapshotted Agent configuration unless an explicit configuration-version change is recorded.

2. **Successful safe regeneration appends a new immutable version.**
   - **Given** regeneration succeeds and passes Content Safety Policy
   - **When** the new output is recorded
   - **Then** a new immutable generated version is added to version history
   - **And** all prior generated and edited versions remain visible to authorized users.

3. **Failed regeneration is fail-closed and content-safe.**
   - **Given** regeneration fails, times out, is denied, or fails safety
   - **When** the proposal is inspected
   - **Then** the existing proposal remains pending unless policy moves it to a terminal state
   - **And** failure status is visible without exposing unsafe content, raw provider errors, or provider payloads.

4. **Terminal proposals cannot invoke the provider.**
   - **Given** a proposal is terminal
   - **When** regeneration is requested
   - **Then** regeneration is rejected
   - **And** no provider invocation occurs.

## Tasks / Subtasks

> Implement in the same order as Story 3.3: Contracts -> Domain aggregate + twin policy -> Server orchestrator -> UI gateway/control -> tests. The proposal remains on the existing `AgentInteraction` aggregate; do not create a new aggregate.

- [x] **Task 1 - Contracts: additive regeneration surface** (AC: #1, #2, #3, #4)
  - [x] Append `Regenerated` to `AgentGenerationKind` after `Edited`. Preserve existing ordinals and `[JsonConverter(typeof(JsonStringEnumConverter))]`.
  - [x] Append `Regenerated` to `ProposedAgentReplyState` after `Edited`. This state means the latest accepted proposal version was produced by a regeneration attempt and is still not posted.
  - [x] Append interaction statuses only if needed by the policy/status surface: recommended `ProposalRegenerated` and `ProposalRegenerationFailed`, appended after Story 3.3 values. Do not renumber existing statuses.
  - [x] Add regeneration result/outcome/failure enums mirroring Story 3.3 edit and Story 2.4 generation: `AgentProposalRegenerationOutcome` (`Unknown=0, Regenerated, ProviderTimeout, ProviderDisabled, ProviderUnavailable, AdapterFailure, InvalidContext, ContentSafetyBlocked, PolicyFailure`), `AgentProposalRegenerationFailureReason`, and `AgentProposedReplyNotRegeneratableReason` (`Unknown=0, InteractionNotProposed, ProposalNotPending, NotAuthorized`).
  - [x] Add `AgentProposalRegenerationResult`: server-to-aggregate carrier with the outcome, deterministic attempt id, safe provider/model/policy metadata, optional content-bearing `AgentGeneratedVersion` only for successful safe regeneration, approver-policy verdict input, and safe failure classification. This is a write-path type; regenerated content is allowed only on the command/result/event/version path.
  - [x] Add `AgentProposedReplyRegenerationEvidence`: safe ids only (`ProposalId`, `SourceConversationId`, `RegeneratedVersionId`, source/snapshot configuration ids, `RequesterPartyId`, `ApproverPolicyVersion`, provider/model/capability/policy versions, and policy basis). No generated content, no prompt, no raw provider payload.
  - [x] Add `Commands/RegenerateProposedAgentReply.cs`, `Events/ProposedAgentReplyRegenerated.cs : IEventPayload`, `Events/ProposedAgentReplyRegenerationFailed.cs : IEventPayload`, and `Events/Rejections/ProposedAgentReplyNotRegeneratableRejection.cs : IRejectionEvent`.
  - [x] Add an audit-evidence query contract for regeneration if needed by tests/future Epic 4 projection: result/view/query types expose attempt/version ids, requester, source conversation, provider/model/policy versions, and failure class only. Live projection/query handler remains deferred to Epic 4.

- [x] **Task 2 - Domain: policy, aggregate handler, and state replay** (AC: #1, #2, #3, #4)
  - [x] Add `AgentProposalRegenerationPolicy` (`internal static`) beside `AgentProposalEditPolicy`. It must expose `Evaluate(string interactionId, AgentProposalRegenerationResult result)` and `Decide(AgentProposalRegenerationResult result)`, both delegating to one private `Compute` so no-drift is impossible by construction.
  - [x] Success maps to `ProposedAgentReplyRegenerated` and a new immutable `AgentGeneratedVersion` with `Kind = Regenerated`; failure maps to `ProposedAgentReplyRegenerationFailed` with safe evidence and no content-bearing version.
  - [x] In `AgentInteractionState`, add regeneration failure/evidence fields if the existing edit/generation fields are insufficient. `Apply(ProposedAgentReplyRegenerated)` must append the new version using the existing list-copy pattern and set `ProposalState = Regenerated`. `Apply(ProposedAgentReplyRegenerationFailed)` must record safe failure metadata but keep the existing proposal state/version history.
  - [x] Add the aggregate `Handle(RegenerateProposedAgentReply command, AgentInteractionState? state, CommandEnvelope envelope)` after the Story 3.3 edit handler. Use the CA1062-safe positive bind (`state is { IsRequested: true } requested`).
  - [x] Preconditions: proposal must exist and `ProposalState` must be in the retryable/editable set `{ Pending, Edited, Regenerated }`. Terminal states owned by Stories 3.5/3.6 must reject with `ProposedAgentReplyNotRegeneratableRejection` and must not emit provider or regeneration events.
  - [x] Avoid the failure-status retry trap: after `ProposedAgentReplyRegenerationFailed`, the proposal should remain retryable while its `ProposalState` is still pending/edited/regenerated. Do not base regeneration eligibility solely on `AgentInteractionStatus` values that would exclude `ProposalRegenerationFailed`.
  - [x] Idempotency: if the regenerated version id from this deterministic attempt already exists in `GeneratedVersions`, return `DomainResult.NoOp()`; a second distinct regeneration attempt appends another version and preserves all prior ids.

- [x] **Task 3 - Server: regeneration orchestrator and deterministic identity** (AC: #1, #2, #3, #4)
  - [x] Add `AgentInteractionProposalRegenerationOrchestrator` in `src/Hexalith.Agents.Server/Application/AgentInteractions/`. It is the impure durable-owner step and dispatches exactly one `RegenerateProposedAgentReply` command after all I/O.
  - [x] Reuse existing ports from Story 2.4: `IConversationContextReader`, `IProviderCatalogReader`, `IAgentGenerationProvider`, `IAgentContentSafetyPolicyReader`, `IContentSafetyEvaluator`, and `IAgentCommandDispatcher`. Do not introduce provider SDK types in contracts or domain.
  - [x] Reuse Story 3.3 authorization shape: resolve the snapshotted approver policy using `IApproverPolicyResolver`, evaluate with `ApproverPolicyVerdict`, and fail closed with no dispatch/provider invocation on any non-`Valid` verdict. `OperationCanceledException` must propagate.
  - [x] Before invoking `IAgentGenerationProvider`, verify the proposal is non-terminal through a safe proposal/projection read or a request shape trusted by the server. AC4 requires no provider call for terminal proposals.
  - [x] Re-read the same Source Conversation and use the same snapshotted Agent/provider/model/capability/content-safety configuration unless the request explicitly records a configuration-version change. If explicit change is not implemented in this story, reject or ignore changes; do not silently use caller-supplied configuration.
  - [x] Add `AgentProposalRegenerationIdentity` with distinct SHA-256 purpose tags, for example `proposal-regeneration-attempt-id` and `proposal-regeneration-version-id`. Inputs should include `interactionId`, source conversation/snapshot ids, and deterministic `RegenerationAttemptId` so retries do not fork versions.
  - [x] Build a safe `AgentProposalRegenerationResult`: success includes a regenerated `AgentGeneratedVersion`; provider/safety/policy failures include safe evidence and no content. Return a safe outcome DTO with ids/status only.
  - [x] Register the orchestrator in `Program.cs` in a `// Story 3.4` block. The aggregate handler should be discovered by the EventStore domain scan.

- [x] **Task 4 - UI: fail-closed regeneration gateway and controls** (AC: #1, #3, #4)
  - [x] Add `IProposalRegenerationGateway`, `DeferredProposalRegenerationGateway`, request/result DTOs, and DI registration under `src/Hexalith.Agents.UI/Services/Gateways/`. Deferred gateway fails closed and never exposes content.
  - [x] Extend version presentation so `Generated`, `Edited`, and `Regenerated` labels are distinct whole-string localized values. Update `ProposedAgentReplyStatePresentation` for `Regenerated` with a curated icon/color and total switch coverage.
  - [x] Add a shared regenerate action/control suitable for Story 3.7 hosting. It should call the gateway, show distinct safe statuses (`Regenerated`, `NotAuthorized`, `Unavailable`, `NotPending`), and never style the proposal as a posted Conversation Message.
  - [x] Add English/French resource keys for regenerate action text, statuses, generated/edited/regenerated version labels, and safe denial/error copy. Keep en/fr parity; never assemble sentences from fragments.
  - [x] Scope boundary: do not build the full proposal detail workspace, version-history panel, or complete keyboard/focus contract here; Story 3.7 hosts the controls. Deliver the reusable gateway/control/presentation surface plus tests.

- [x] **Task 5 - Tests across all layers** (AC: #1, #2, #3, #4)
  - [x] Contracts tests: marker interfaces, JSON round trips, enum-by-name serialization, ordinal stability for appended values, backward-compatible `AgentGeneratedVersion` round trips, and AD-14 no-leak assertions for failure events/rejections/views/outcomes. Do not assert no-leak on the successful regenerated event/version; that is the legitimate content home.
  - [x] Domain tests: authorized regeneration appends a `Regenerated` version and preserves all prior version ids; provider/safety/policy failures keep proposal state retryable and append no version; terminal/non-pending proposals reject; retry of same deterministic version is `NoOp`; distinct second regeneration appends another version; `Evaluate`/`Decide` no-drift theory over every outcome.
  - [x] Server tests: happy path dispatches exactly one `RegenerateProposedAgentReply` with correct envelope; unauthorized/non-valid approver policy causes no dispatch and no provider call; terminal proposal causes no provider call; provider timeout/error/disabled/unavailable and safety blocked map safely; reserved trust keys stripped; deterministic ids stable; `OperationCanceledException` propagates.
  - [x] UI tests: gateway fail-closed behavior, regenerate control happy/failure statuses, no content in accessible names/test ids/status strings, presentation totality for `Regenerated`, localization parity, and component events for host refresh.
  - [x] Run from `Hexalith.Agents/`: `dotnet restore Hexalith.Agents.slnx`; `dotnet build Hexalith.Agents.slnx --configuration Release -m:1`; run each touched test project individually. If VSTest socket permission fails, run built xUnit v3 executables directly.

- [x] **Task 6 - Dev Agent Record accuracy** (recurring review guardrail)
  - [x] Regenerate test counts from actual output after the final test run. Story 3.3 ended at Contracts 246, Domain 576, Server 275, UI 462; expect counts to increase from that baseline.
  - [x] Diff the File List against `git status` before moving to review. Include every created/modified production file, test file, story file, and sprint-status/test-summary artifact.

## Dev Notes

### Critical guardrails

- **Reuse the existing aggregate.** Regeneration belongs on `AgentInteraction`; do not create a new proposal aggregate. Source: `ARCHITECTURE-SPINE.md#AD-2`; `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs`.
- **Side effects outside the aggregate.** Provider calls, conversation re-read, policy reads, safety evaluation, authorization resolution, and id derivation happen in the server orchestrator. The aggregate accepts one trusted command and emits events only. Source: `ARCHITECTURE-SPINE.md#AD-3`; Story 2.4 and Story 3.3 patterns.
- **No provider invocation before authorization/terminal checks.** AC4 is explicit: terminal proposals reject and no provider invocation occurs. The orchestrator must fail closed before `IAgentGenerationProvider.GenerateAsync` when authorization/proposal-state is uncertain.
- **Content confinement.** Regenerated content is sensitive. It may appear only on the successful write path (`AgentGeneratedVersion`, `RegenerateProposedAgentReply` command/result, `ProposedAgentReplyRegenerated` event, aggregate state). It must not appear on safe evidence, failure events, rejections, gateway/orchestrator outcome DTOs, status strings, logs, telemetry dimensions, accessible names, or read views.
- **Retryability after failure.** AC3 says the existing proposal remains pending unless policy moves it terminal. If you add `ProposalRegenerationFailed`, do not make that status block future regeneration while `ProposalState` remains retryable. This was a latent review note in Story 3.3 for edit-failed and must not be repeated in reachable regeneration failure paths.
- **Deterministic identity.** Use new purpose tags for regeneration ids; do not reuse `proposal-id`, `proposal-edit-version-id`, or Story 2.4 attempt tags. No `Guid.NewGuid`, ULID, wall-clock, or random ids in aggregate/domain code.

### Existing code to reuse

- Story 2.4 generation/content-safety orchestrator and ports:
  - `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionGenerationOrchestrator.cs`
  - `Hexalith.Agents/src/Hexalith.Agents.Server/Ports/IAgentGenerationProvider.cs`
  - `Hexalith.Agents/src/Hexalith.Agents.Server/Ports/IContentSafetyEvaluator.cs`
  - `Hexalith.Agents/src/Hexalith.Agents.Server/Ports/IAgentContentSafetyPolicyReader.cs`
  - `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentOutputGenerationPolicy.cs`
- Story 3.3 edit/version append and approver authorization patterns:
  - `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentProposalEditPolicy.cs`
  - `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs` `Handle(EditProposedAgentReply...)`
  - `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalEditOrchestrator.cs`
  - `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentProposalEditIdentity.cs`
  - `Hexalith.Agents/src/Hexalith.Agents.UI/Components/Shared/AgentGenerationKindPresentation.cs`
  - `Hexalith.Agents/src/Hexalith.Agents.UI/Components/Shared/ProposalEditor.razor`

### Project Structure Notes

- Production contracts go under `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/`, with commands in `Commands/`, events in `Events/`, rejections in `Events/Rejections/`, and read contracts in `Queries/`.
- Domain logic goes under `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/`.
- Server orchestrator/identity/request DTOs go under `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/`.
- Server ports should be reused when possible. Add a new safe proposal-state reader only if needed to satisfy AC4 before provider invocation.
- UI gateway/control/presentation changes go under `Hexalith.Agents/src/Hexalith.Agents.UI/Services/Gateways/` and `Components/Shared/`, with localization in both `AgentsResources.resx` files.

## Testing Standards

- Test stack: xUnit v3, Shouldly, NSubstitute in Server/UI tests only where existing patterns use it, bUnit for Blazor components.
- No raw `Assert.*`. Prefer existing fixture helpers in `AgentInteractionTestData`, `AgentsTestContext`, and `AgentUiTestData`.
- Run projects individually; do not rely on solution-wide `dotnet test`.
- Release build must be 0 warnings / 0 errors.

## References

- `_bmad-output/planning-artifacts/epics.md#Story 3.4: Regenerate Proposed Reply Versions`
- `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-16: Regenerate Proposed Reply`
- `_bmad-output/implementation-artifacts/2-4-generate-and-safety-check-agent-output.md`
- `_bmad-output/implementation-artifacts/3-3-edit-proposed-reply-versions.md`
- `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionGenerationOrchestrator.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalEditOrchestrator.cs`

## Create-Story Checklist Validation

- [x] Target story determined from sprint status and user request: `3-4-regenerate-proposed-reply-versions`.
- [x] Epics and PRD FR-16 loaded and reflected in ACs/tasks.
- [x] Previous-story intelligence from Stories 2.4 and 3.3 included.
- [x] Existing implementation anchors listed to prevent reinvention.
- [x] File locations, test projects, and build/test commands specified.
- [x] Scope boundaries called out for Story 3.7 and Epic 4.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (1M context) — `claude-opus-4-8[1m]`.

### Debug Log References

- Release build (`dotnet build Hexalith.Agents.slnx --configuration Release -m:1`): 0 warnings / 0 errors across the full solution.
- Per-project test runs (xUnit v3, Release, `--no-build`):
  - `Hexalith.Agents.Contracts.Tests`: 273 passed / 0 failed (Story 3.3 baseline 246; +27).
  - `Hexalith.Agents.Tests` (Domain): 601 passed / 0 failed (baseline 576; +25).
  - `Hexalith.Agents.Server.Tests`: 311 passed / 0 failed (baseline 275; +36).
  - `Hexalith.Agents.UI.Tests`: 484 passed / 0 failed (baseline 462; +22).
- One mid-implementation fix: the regeneration orchestrator referenced `ApproverPolicyVerdict` without the `Hexalith.Agents.Server.Application.Agents` using (CS0103); added the using (mirroring the edit orchestrator).
- QA `bmad-qa-generate-e2e-tests` pass (2026-06-24): coverage-gap analysis against the four ACs + Task 5; **+7 focused tests, tests-only, no production change**. New per-project totals (Release, `--no-build`): Contracts **274** (+1), Domain **603** (+2), Server **315** (+4), UI **484** (unchanged). Release build remained 0 warnings / 0 errors. Gaps closed: exhaustive `Evaluate`/`Decide` no-drift over every outcome×verdict; AC3 failure-path `ProcessAsync` round-trip; content-safety `Unknown`-verdict + throwing-evaluator fail-closed; provider not-text-capable → `ProviderDisabled`; blank provider id → `ProviderUnavailable`; failure-shaped result (null version) JSON round-trip. Summary: `_bmad-output/implementation-artifacts/tests/test-summary.md`.

### Completion Notes List

- **Reused the existing `AgentInteraction` aggregate (AD-2).** Regeneration is the 8th `Handle` on `AgentInteractionAggregate`; no new aggregate. Eligibility is keyed on the proposal SUB-STATE retryable set `{ Pending, Edited, Regenerated }`, NOT the coarse interaction status, so a prior `ProposalRegenerationFailed`/`ProposalEditFailed` status never blocks a retry (the failure-status retry trap is avoided; covered by a dedicated domain test).
- **Side effects outside the aggregate (AD-3).** The new `AgentInteractionProposalRegenerationOrchestrator` is the durable-owner step: it runs the AC4 terminal-proposal guard + fail-closed approver authorization BEFORE any conversation re-read or provider call, then re-reads the same Source Conversation, re-invokes the provider behind `IAgentGenerationProvider`, runs the content-safety gate, derives deterministic ids, and dispatches exactly one `RegenerateProposedAgentReply`. All Story 2.4 + 3.3 ports are reused — no new port introduced.
- **AC4 enforced before the provider.** The orchestrator denies a non-retryable proposal (trusted `ProposalState` on the request) with a no-dispatch `ProposalNotPending` and makes no conversation/provider call (asserted: `DidNotReceive` on context reader, provider, and dispatcher). The aggregate independently rejects terminal proposals with `ProposedAgentReplyNotRegeneratableRejection` and emits no provider/regeneration event.
- **Content confinement (AD-14).** Regenerated content rides only the success write path (`AgentGeneratedVersion` Kind=Regenerated → `RegenerateProposedAgentReply` → `ProposedAgentReplyRegenerated` → aggregate state). The failure event, rejection, safe evidence, evidence view, orchestrator outcome DTO, gateway request/result, and UI status strings are all content-free (contract no-leak tests + orchestrator envelope/outcome assertions).
- **Deterministic identity (AD-13).** `AgentProposalRegenerationIdentity` uses distinct SHA-256 purpose tags `proposal-regeneration-attempt-id` / `proposal-regeneration-version-id` over `(interaction, source conversation, regeneration attempt)`; retries reuse ids (aggregate no-op dedupe), a distinct attempt appends another version, and the ids never collide with the proposal/edit/generation id families.
- **Additive contracts only (AD-2).** Appended `AgentGenerationKind.Regenerated` (3), `ProposedAgentReplyState.Regenerated` (3), and `AgentInteractionStatus.ProposalRegenerated` (16) / `ProposalRegenerationFailed` (17); existing ordinals untouched (ordinal-stability tests). New enums serialize by name and fail safe to `Unknown`.
- **UI surface (Story 3.7 host-ready).** Added `IProposalRegenerationGateway` + deferred fail-closed placeholder + ids-only request/result DTOs, the reusable `ProposalRegenerator.razor` control (distinct safe statuses Regenerated/NotAuthorized/Unavailable/NotPending, never styled as a posted Conversation Message), distinct `Regenerated` version/state presentation, and en/fr resource parity. The full proposal-detail workspace / version-history panel / keyboard contract remain scoped to Story 3.7; the live read-model/BFF binding and the regeneration audit-evidence projection remain deferred to Epic 4.

### File List

**Production — Contracts (`src/Hexalith.Agents.Contracts/AgentInteraction/`):**
- `AgentGenerationKind.cs` (modified — appended `Regenerated`)
- `ProposedAgentReplyState.cs` (modified — appended `Regenerated`)
- `AgentInteractionStatus.cs` (modified — appended `ProposalRegenerated`, `ProposalRegenerationFailed`)
- `AgentProposalRegenerationOutcome.cs` (new)
- `AgentProposalRegenerationFailureReason.cs` (new)
- `AgentProposedReplyNotRegeneratableReason.cs` (new)
- `AgentProposalRegenerationResult.cs` (new)
- `AgentProposedReplyRegenerationEvidence.cs` (new)
- `Commands/RegenerateProposedAgentReply.cs` (new)
- `Events/ProposedAgentReplyRegenerated.cs` (new)
- `Events/ProposedAgentReplyRegenerationFailed.cs` (new)
- `Events/Rejections/ProposedAgentReplyNotRegeneratableRejection.cs` (new)
- `Queries/AgentProposalRegenerationEvidenceView.cs` (new)
- `Queries/AgentProposalRegenerationEvidenceResult.cs` (new)
- `Queries/GetAgentProposalRegenerationEvidenceQuery.cs` (new)

**Production — Domain (`src/Hexalith.Agents/AgentInteraction/`):**
- `AgentProposalRegenerationPolicy.cs` (new)
- `AgentInteractionState.cs` (modified — regeneration evidence/failure fields + 3 `Apply` handlers)
- `AgentInteractionAggregate.cs` (modified — `Handle(RegenerateProposedAgentReply…)` + `RegenerationAlreadyLanded`; review fix — widened the Story 3.3 edit-eligibility set to include `Regenerated`/`ProposalRegenerated` so a regenerated proposal stays editable)

**Production — Server (`src/Hexalith.Agents.Server/`):**
- `Application/AgentInteractions/AgentInteractionProposalRegenerationOrchestrator.cs` (new)
- `Application/AgentInteractions/AgentInteractionProposalRegenerationRequest.cs` (new — request + outcome result)
- `Application/AgentInteractions/AgentProposalRegenerationIdentity.cs` (new)
- `Program.cs` (modified — Story 3.4 orchestrator registration)

**Production — UI (`src/Hexalith.Agents.UI/`):**
- `Services/Gateways/IProposalRegenerationGateway.cs` (new)
- `Services/Gateways/DeferredProposalRegenerationGateway.cs` (new)
- `Services/Gateways/ProposalRegenerationRequest.cs` (new)
- `Services/Gateways/ProposalRegenerationResult.cs` (new)
- `Services/Gateways/AgentsUiServiceCollectionExtensions.cs` (modified — DI registration)
- `Components/Shared/ProposalRegenerator.razor` (new)
- `Components/Shared/AgentGenerationKindPresentation.cs` (modified — `Regenerated` label)
- `Components/Shared/ProposedAgentReplyStatePresentation.cs` (modified — `Regenerated` color/icon)
- `Resources/AgentsResources.resx` (modified — new en keys)
- `Resources/AgentsResources.fr.resx` (modified — new fr keys)

**Tests:**
- `tests/Hexalith.Agents.Contracts.Tests/AgentInteractionProposalRegenerationContractsTests.cs` (new)
- `tests/Hexalith.Agents.Tests/AgentInteractionProposalRegenerationAggregateTests.cs` (new)
- `tests/Hexalith.Agents.Tests/AgentInteractionProposalEditAggregateTests.cs` (modified — review fix regression test: editing a regenerated proposal is allowed and appends an edited version)
- `tests/Hexalith.Agents.Tests/AgentInteractionTestData.cs` (modified — regeneration fixtures + `ApplyAll` cases)
- `tests/Hexalith.Agents.Server.Tests/AgentInteractionProposalRegenerationOrchestratorTests.cs` (new)
- `tests/Hexalith.Agents.Server.Tests/AgentProposalRegenerationIdentityTests.cs` (new)
- `tests/Hexalith.Agents.UI.Tests/ProposalRegeneratorTests.cs` (new)
- `tests/Hexalith.Agents.UI.Tests/AgentsTestContext.cs` (modified — regeneration gateway substitute)
- `tests/Hexalith.Agents.UI.Tests/DeferredGatewayTests.cs` (modified — deferred regeneration gateway test)
- `tests/Hexalith.Agents.UI.Tests/AgentGenerationKindPresentationTests.cs` (modified — `Regenerated` cases)
- `tests/Hexalith.Agents.UI.Tests/ProposedAgentReplyStatePresentationTests.cs` (modified — `Regenerated` color)
- `tests/Hexalith.Agents.UI.Tests/LocalizationResourceTests.cs` (modified — regenerator keys)

**Artifacts:**
- `_bmad-output/implementation-artifacts/3-4-regenerate-proposed-reply-versions.md` (this story)
- `_bmad-output/implementation-artifacts/sprint-status.yaml` (3-4 → done)
- `_bmad-output/implementation-artifacts/tests/test-summary.md` (QA E2E gap-analysis summary)

**QA E2E gap tests (2026-06-24 — tests only, no production change):**
- `tests/Hexalith.Agents.Contracts.Tests/AgentInteractionProposalRegenerationContractsTests.cs` (modified — +1 failure-shaped result round-trip)
- `tests/Hexalith.Agents.Tests/AgentInteractionProposalRegenerationAggregateTests.cs` (modified — +`using System;`, +2 no-drift/failure-round-trip tests)
- `tests/Hexalith.Agents.Server.Tests/AgentInteractionProposalRegenerationOrchestratorTests.cs` (modified — +4 fail-closed edge tests, `Request` helper extended)

## Senior Developer Review (AI)

**Reviewer:** Administrator · **Date:** 2026-06-24 · **Outcome:** Approve (1 Medium auto-fixed; status → done)

### Scope & method

Adversarial review of every File-List file against the four ACs and Tasks 1–6. Git reality cross-checked against the File List (the only non-listed change is `_bmad-output/story-automator/orchestration-*.md`, an excluded automation artifact). Release build re-verified 0 warnings / 0 errors; all four touched test projects re-run and pass with counts matching the Dev Agent Record exactly (Contracts 274, Domain 603→604 after the review fix, Server 315, UI 484).

### Verification of claims

- **AC1–AC4 all implemented.** Terminal-proposal guard + fail-closed approver authorization run before any conversation re-read / provider call (orchestrator steps 1–2); deterministic ids via distinct SHA-256 purpose tags; success appends an immutable `Regenerated` version preserving prior versions; failures are content-free and keep the proposal retryable. The failure-status retry trap is correctly avoided (regeneration eligibility keys on the proposal sub-state, not the coarse status).
- **Content confinement (AD-14) holds** across the failure event, rejection, evidence, evidence view, orchestrator outcome DTO, gateway request/result, and UI status strings — asserted by contract no-leak tests and domain JSON round-trip tests.
- **All Tasks [x] are genuinely done**; File List is accurate and complete; en/fr resource parity confirmed; presentation switches total.

### Findings

- **[Medium · FIXED] Edit/regenerate interplay asymmetry.** The Story 3.4 regeneration handler treats an *edited* proposal as retryable (you may regenerate after editing), but the Story 3.3 edit handler was not updated symmetrically — after a successful regeneration (`ProposalState = Regenerated`) editing was rejected as `ProposalNotPending`, breaking FR-14's intent of freely mixing edit + regenerate before approval. **Fix:** widened the edit handler's eligibility to include `Regenerated`/`ProposalRegenerated` (`AgentInteractionAggregate.cs`), mirroring the regeneration handler's retryable set; added a domain regression test (`AgentInteractionProposalEditAggregateTests.cs`). Additive change — no existing test affected; Domain 603 → 604.
- **[Low · noted, not changed] Dispatched-failure outcome carries a version id.** On a *dispatched* regeneration failure (e.g. provider timeout) `AgentInteractionProposalRegenerationOutcomeResult.RegeneratedVersionId` is non-empty for a version that was never created. It is safe (a deterministic id, never content) and the decided status disambiguates; the live UI mapping is deferred to Epic 4. Left as-is to preserve the deterministic-id contract.
- **[Low · noted, not changed] `ProposalRegenerator.razor` has no in-flight submit guard.** A double-click could submit twice; the aggregate's deterministic-id no-op dedupes server-side, and Story 3.7 owns the hosted control. Out of scope here.
- **[Observation] Pre-existing edit-failed retry trap (Story 3.3 scope).** After `ProposalEditFailed`/`ProposalRegenerationFailed`, editing is still blocked while the proposal is retryable (the status-based `proposalExists` half of the edit precondition). The Dev Notes already document this as a latent Story 3.3 note; regeneration correctly avoids it. Left to its owning story to avoid reopening a deliberately-scoped decision.

## Change Log

| Date       | Version | Description                                                                                          |
| ---------- | ------- | ---------------------------------------------------------------------------------------------------- |
| 2026-06-24 | 0.1     | Implemented Story 3.4 (regenerate proposed reply versions) across Contracts, Domain, Server, UI, and tests. Release build clean (0/0); Contracts 273, Domain 601, Server 311, UI 484 passing. Status → review. |
| 2026-06-24 | 0.2     | QA `bmad-qa-generate-e2e-tests` pass: +7 focused gap tests (tests only, no production change). Release build clean (0/0); Contracts 274, Domain 603, Server 315, UI 484 passing (1676 total). |
| 2026-06-24 | 0.3     | Senior Developer Review (AI): 0 Critical. Auto-fixed 1 Medium — widened the edit handler's eligibility so a regenerated proposal stays editable (edit/regenerate symmetry, FR-14) + domain regression test. Release build clean (0/0); Domain 604, others unchanged (Contracts 274, Server 315, UI 484; 1677 total). Status → done. |
