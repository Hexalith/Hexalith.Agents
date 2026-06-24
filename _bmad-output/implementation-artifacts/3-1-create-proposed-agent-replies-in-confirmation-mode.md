---
baseline_commit: b2a5a2d6570f29cbf1cd7d3013ae990b6370e5fe
---

# Story 3.1: Create Proposed Agent Replies In Confirmation Mode

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an Approver,
I want successful confirmation-mode generation to create a Proposed Agent Reply outside the Conversation,
so that generated content can be reviewed before it becomes durable Conversation content.

## Acceptance Criteria

**AC1 - Confirmation-mode generation creates a Proposed Agent Reply linked to the interaction + Source Conversation, recording the safe facts**
**Given** an AgentInteraction is in Confirmation Response Mode and generation plus safety checks pass
**When** the response-mode branch runs
**Then** the system creates a Proposed Agent Reply linked to the AgentInteraction and Source Conversation
**And** it records caller, Agent, Source Conversation, generated version, Provider/model, response mode, proposal state, expiry metadata where configured, and policy snapshots.

**AC2 - The proposal is never a Conversation Message; generated content is only on the authorized proposal workflow surfaces**
**Given** a Proposed Agent Reply exists
**When** Conversation content is inspected
**Then** the proposal is not present as a Conversation Message
**And** generated content is visible only through authorized proposal workflow surfaces.

**AC3 - Unsafe generated content never yields an approvable proposal; the safety failure is recorded safely**
**Given** generated content fails Content Safety Policy
**When** confirmation mode handles the output
**Then** no approvable Proposed Agent Reply is created
**And** authorized status/audit records a safety failure according to policy without exposing unsafe content where forbidden.

**AC4 - Retry creates no duplicate proposals or versions; replay is deterministic**
**Given** proposal creation is retried
**When** the same AgentInteraction and generated version are used
**Then** duplicate Proposed Agent Replies and duplicate generated versions are prevented
**And** replay produces deterministic proposal state.

[Source: _bmad-output/planning-artifacts/epics.md#Story-3.1-Create-Proposed-Agent-Replies-In-Confirmation-Mode; prds/prd-agents-2026-06-23/prd.md#4-6-proposed-agent-reply-workflow (FR-13, FR-14); prd.md#4-10-content-safety-and-launch-readiness (FR-27); architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-2-Aggregate-Boundaries; #AD-4-Interaction-Snapshot; #AD-5-Proposal-Lifecycle; #AD-6-Conversations-Boundary; #AD-13-Idempotent-External-Effects; #AD-14-Sensitive-Content-And-Secret-Safety]

## Tasks / Subtasks

- [x] **Task 1 - Add the proposal-creation public contracts to `Hexalith.Agents.Contracts/AgentInteraction/`** (AC: #1, #2, #3, #4)
  - [x] Add the enum `ProposedAgentReplyState` under `Hexalith.Agents.Contracts/AgentInteraction/` with values `Unknown = 0, Pending`. Decorate `[JsonConverter(typeof(JsonStringEnumConverter))]`; document `Unknown = 0` as the not-yet-known fail-safe sentinel and `Pending` as "the proposal was created and awaits authorized Approver action" (AD-5 *generated*/*pending approval*; FR-13). **Mirror `AgentGenerationKind.cs` exactly**, including its XML remark that the deferred lifecycle values are reserved additively: explicitly note in the doc that `Edited`, `Regenerated`, `Approved`, `Rejected`, `Abandoned`, `Expired`, `PostingPending`, `Posted`, `PostingFailed` are owned by Stories 3.3–3.6 and **must NOT be added in this story**.
  - [x] Add the enum `AgentProposalCreationOutcome` (the server→aggregate carrier discriminator, mirror `AgentResponsePostingOutcome`) with values `Unknown = 0, Created, GeneratedVersionUnavailable, AdapterFailure` (`[JsonStringEnumConverter]`, `Unknown = 0` fail-safe sentinel treated as a creation failure). `Created` is the only success outcome.
  - [x] Add the enum `AgentProposalCreationFailureReason` (recorded on the failure event; mirror `AgentResponsePostingFailureReason`) with values `Unknown = 0, GeneratedVersionUnavailable, AdapterFailure` (`[JsonStringEnumConverter]`, `Unknown = 0` sentinel). Coarse, content-free classifications — no provider/Conversations detail.
  - [x] Add the enum `AgentProposedReplyNotCreatableReason` (the structural-rejection reason; mirror `AgentResponseNotPostableReason`) with values `Unknown = 0, InteractionNotRequested, NotConfirmationResponseMode, OutputNotGenerated` (`[JsonStringEnumConverter]`, `Unknown = 0` sentinel). These are the three structural-precondition violations the pure handler rejects on.
  - [x] Add the safe evidence value object `AgentProposedReplyEvidence` (bare record; **mirror `AgentPostedMessageEvidence.cs`** — safe ids only, symmetric across success + failure): `AgentProposedReplyEvidence(string ProposalId, string SourceConversationId, string ProposedVersionId, int ApproverPolicyVersion, int ContentSafetyPolicyVersion, string? ExpiresAt)`. `ProposalId` is the deterministic proposal id derived from `(AgentInteractionId, ProposedVersionId)` (AD-13). `ExpiresAt` is the optional expiry metadata "where configured" (null when no expiry policy is configured — AC1; the default/min/max is a deferred product decision). Document that this carries NEVER the generated content, a raw provider/Conversations payload, a stack trace, or a secret (AD-14); `ProposedVersionId` is the *id* of the version held in the proposal, never its content.
  - [x] Add the server→aggregate carrier `AgentProposalCreationResult` (mirror `AgentResponsePostingResult` — the value the orchestrator puts on the command; safe ids only, no content): `AgentProposalCreationResult(AgentProposalCreationOutcome Outcome, string ProposalId, string SourceConversationId, string ProposedVersionId, int ApproverPolicyVersion, int ContentSafetyPolicyVersion, string? ExpiresAt)`. On a fail-closed outcome the ids carried are the ones that were attempted (`ProposedVersionId`/`ProposalId` empty when the version read failed before a version id was known).
  - [x] Add the aggregate command `AgentInteraction/Commands/CreateProposedAgentReply(string AgentInteractionId, AgentProposalCreationResult Result)`. **Mirror `Commands/PostAgentResponse.cs`**: bare record, the redundant `AgentInteractionId` mirrors the envelope aggregate id, the `Result` is server-assembled (any client value is stripped/overwritten by the orchestrator — AD-3 round-trip), and **the command carries NO generated content** (AD-14).
  - [x] Add the success event `AgentInteraction/Events/ProposedAgentReplyCreated(string AgentInteractionId, AgentProposedReplyEvidence Evidence) : IEventPayload`. **Mirror `Events/AgentResponsePosted.cs`**: a durable success event (NOT an `IRejectionEvent`) that transitions status to `ProposalCreated`; no wall-clock field (creation time is EventStore event metadata — AD-3); carries only the safe evidence, never content (AD-14). This event **is** the AC1 Audit Evidence that a proposal was created.
  - [x] Add the recorded-negative event `AgentInteraction/Events/ProposedAgentReplyCreationFailed(string AgentInteractionId, AgentProposalCreationFailureReason Reason, AgentProposedReplyEvidence Evidence) : IEventPayload`. **Mirror `Events/AgentResponsePostingFailed.cs`**: a durable success event (NOT an `IRejectionEvent`) — a successfully-recorded fail-closed decision that transitions status to `ProposalCreationFailed` with the safe reason + attempted evidence; distinct from the structural rejection below. No content, no raw payload, no stack trace (AD-14).
  - [x] Add the structural rejection `AgentInteraction/Events/Rejections/ProposedAgentReplyNotCreatableRejection(string AgentInteractionId, AgentProposedReplyNotCreatableReason Reason) : IRejectionEvent`. **Mirror `Rejections/AgentResponseNotPostableRejection.cs`**: used only when proposal creation cannot be evaluated at all (interaction not requested, not Confirmation mode, output not generated) — no state change, carries only the safe reason classification (AD-14).
  - [x] Extend `AgentInteractionStatus` by **appending** (preserve existing ordinals `Unknown=0 … Posted=10, PostingFailed=11`): `ProposalCreated = 12` (a Proposed Agent Reply was created and awaits Approver action — the Confirmation-mode counterpart to `Posted`) and `ProposalCreationFailed = 13` (fail-closed: a proposal could not be created; recorded as Audit Evidence; no approvable proposal exists). Do not reorder/renumber. Update the enum's `<summary>` to note Story 3.1 appends these two Confirmation-mode states, mirroring how Story 2.5 documented appending `Posted`/`PostingFailed`. Also refine the `Generated` member's XML doc cross-reference (it already names "Story 3.1 proposal — these consume this state").
  - [x] **Do NOT extend `AgentInteractionStatusView` in this story.** The `Status` transition to `ProposalCreated`/`ProposalCreationFailed` (already on the view) is the 3.1-visible signal the Story 2.6 call-status feedback surface polls — exactly as Story 2.5 surfaced `Posted`/`PostingFailed` via `Status` alone and added no posting-specific view fields. Surfacing `ProposalId`/proposal state on a read surface (the queue/detail) is owned by Stories 3.2/3.7; keep this story's read footprint to the `Status` value to avoid scope creep and a premature read-model field.
  - [x] Keep `Hexalith.Agents.Contracts` inward-only (no provider SDK, no `Microsoft.Agents.AI`, no Dapr, no server infra, no sibling-module types, no `Hexalith.Conversations`/`Hexalith.Parties` types). The Story 1.1 secret-non-disclosure guard (forbidden member tokens `Secret`/`ApiKey`/`Credential`/`Password`/`ConnectionString`) must stay green and unweakened — all new member names (`ProposalId`, `ProposedVersionId`, `ExpiresAt`, `ApproverPolicyVersion`, `ContentSafetyPolicyVersion`, etc.) are safe.

- [x] **Task 2 - Extend the pure `AgentInteraction` aggregate replay state** (AC: #1, #3, #4)
  - [x] In `Hexalith.Agents/AgentInteraction/AgentInteractionState.cs` add three new fields (all `null` until a proposal decision is recorded, mirroring the existing `PostingEvidence`/`PostingFailureReason` fields): `public ProposedAgentReplyState? ProposalState { get; set; }` (the proposal sub-state — `Pending` after creation), `public AgentProposedReplyEvidence? ProposalEvidence { get; set; }` (the safe proposal evidence — ids only, AD-14), and `public AgentProposalCreationFailureReason? ProposalCreationFailureReason { get; set; }` (set only on a creation-failed decision). These are the ONLY new durable fields — safe ids/enums, no content (AD-9, AD-14).
  - [x] Add `Apply(ProposedAgentReplyCreated e)` → guard `if (!IsRequested) return;` (exactly like the other update applies), then set `Status = AgentInteractionStatus.ProposalCreated`, `ProposalState = ProposedAgentReplyState.Pending`, `ProposalEvidence = e.Evidence`. The initial `Pending` state is set deterministically here (mirroring how `Apply(AgentResponsePosted)` sets `Status = Posted`) — it is NOT carried on the evidence.
  - [x] Add `Apply(ProposedAgentReplyCreationFailed e)` → same `IsRequested` guard, then set `Status = AgentInteractionStatus.ProposalCreationFailed`, `ProposalCreationFailureReason = e.Reason`, `ProposalEvidence = e.Evidence`. Do NOT set `ProposalState` (no proposal exists). Mirror `Apply(AgentResponsePostingFailed)`.
  - [x] Add `Apply(ProposedAgentReplyNotCreatableRejection e)` → replay-safe no-op via `MarkReplayOnlyEventHandled()` (mirror `Apply(AgentResponseNotPostableRejection)`). Confirm replay stays total.

- [x] **Task 3 - Implement the pure aggregate handler + the twin proposal-creation policy** (AC: #1, #2, #3, #4)
  - [x] In `Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs` add the 6th handler `public static DomainResult Handle(CreateProposedAgentReply command, AgentInteractionState? state, CommandEnvelope envelope)` **mirroring `Handle(PostAgentResponse, …)` exactly** in shape (it auto-registers via the EventStore convention scan — no host change). The guard cascade in order:
    - `ArgumentNullException.ThrowIfNull(command); ArgumentNullException.ThrowIfNull(envelope); string interactionId = envelope.AggregateId;`
    - Positive bind: `if (state is { IsRequested: true } requested) { … }` else fall through to `return DomainResult.Rejection([new ProposedAgentReplyNotCreatableRejection(interactionId, AgentProposedReplyNotCreatableReason.InteractionNotRequested)]);`
    - **Terminal idempotent no-op (AD-13):** `if (requested.Status is AgentInteractionStatus.ProposalCreated or AgentInteractionStatus.ProposalCreationFailed) return DomainResult.NoOp();` — a re-dispatched create command on an already-decided proposal preserves the recorded decision and never creates a duplicate proposal/version (AC4).
    - **Response-mode precondition (the Confirmation counterpart to PostAgentResponse's Automatic check):** `if (requested.Snapshot?.ResponseMode != AgentResponseMode.Confirmation) return DomainResult.Rejection([new ProposedAgentReplyNotCreatableRejection(interactionId, AgentProposedReplyNotCreatableReason.NotConfirmationResponseMode)]);` — Automatic mode posts via Story 2.5, never creates a proposal.
    - **Generation precondition (AD-12):** `if (requested.Status != AgentInteractionStatus.Generated) return DomainResult.Rejection([new ProposedAgentReplyNotCreatableRejection(interactionId, AgentProposedReplyNotCreatableReason.OutputNotGenerated)]);` — proposal creation only ever follows a successful, safety-passing generation. This is the structural enforcement of **AC3**: a `SafetyFailed`/`GenerationFailed` interaction never reaches proposal creation (no generated version exists to propose — AD-5).
    - Pure evaluation: `return AgentProposalCreationPolicy.Evaluate(interactionId, command.Result);`
  - [x] Add the pure twin policy `Hexalith.Agents/AgentInteraction/AgentProposalCreationPolicy.cs` as `internal static class` **mirroring `AgentResponsePostingPolicy.cs`**:
    - `internal static DomainResult Evaluate(string interactionId, AgentProposalCreationResult result)` → `Created` → `DomainResult.Success([new ProposedAgentReplyCreated(interactionId, evidence)])`; any other outcome → `DomainResult.Success([new ProposedAgentReplyCreationFailed(interactionId, mappedReason, evidence)])`.
    - `internal static AgentInteractionStatus Decide(AgentProposalCreationResult result)` → returns the same computed status the orchestrator reports (so it cannot drift from the aggregate's recorded decision — AD-3).
    - Private `Compute(result)` → a `readonly record struct ProposalDecision(AgentInteractionStatus Status, AgentProposalCreationFailureReason Reason, AgentProposedReplyEvidence Evidence)`. `Created` → `(ProposalCreated, Unknown, evidence)`; else → `(ProposalCreationFailed, MapFailureReason(outcome), evidence)`.
    - `Evidence(result)` builds `AgentProposedReplyEvidence` from the result's safe ids (`ProposalId`, `SourceConversationId`, `ProposedVersionId`, `ApproverPolicyVersion`, `ContentSafetyPolicyVersion`, `ExpiresAt`) — never content. `MapFailureReason`: `GeneratedVersionUnavailable → GeneratedVersionUnavailable`, `AdapterFailure → AdapterFailure`, `_ → AdapterFailure` (fail closed). No I/O, no time, no secrets (AD-3).
  - [x] Confirm this story touches NO Conversations/Parties/membership/append path: proposal creation makes no Conversation write and reads no Party identity (AD-6 — "a Proposed Agent Reply is never a Conversation Message"). That guarantees **AC2** by construction.

- [x] **Task 4 - Deterministic proposal-id helper + the expiry-policy port in `Hexalith.Agents.Server`** (AC: #1, #4)
  - [x] Add `Hexalith.Agents.Server/Application/AgentInteractions/AgentProposalIdentity.cs` (`internal static`) **mirroring `AgentResponsePostingIdentity.cs`**: `internal static string DeriveProposalId(string agentInteractionId, string versionId)` = lowercase-hex SHA-256 of the length-prefixed `(purposeTag, agentInteractionId, versionId)` with purpose tag `"proposal-id"`. A retried creation on the same `(interaction, selected version)` yields the same `ProposalId`, so the aggregate's terminal no-op + a future read-model dedupe make retries safe (AD-13; AC4). No ULID/`Guid.NewGuid`/wall-clock (it would defeat idempotency).
  - [x] Add the server port `Hexalith.Agents.Server/Ports/IProposalExpiryPolicyReader` with `Task<ProposalExpiryPolicyResult> ReadAsync(string tenantId, string agentId, CancellationToken ct)`, the server-internal result `ProposalExpiryPolicyResult(string? ExpiresAt)` (a safe optional ISO-8601 expiry timestamp; `null` = no expiry configured), and the deferred adapter `DeferredProposalExpiryPolicyReader : IProposalExpiryPolicyReader` returning `new ProposalExpiryPolicyResult(null)` — **mirror `DeferredAgentGeneratedVersionReader` / `DeferredProviderCatalogReader`**. Document that the live expiry-policy binding AND expiry **enforcement** (transition to an `Expired` terminal state) are deferred to Story 3.6; this story only records the optional `ExpiresAt` metadata "where configured" (AC1). The reader/result are server-internal, not public contracts.

- [x] **Task 5 - Proposal-creation orchestration + activation of the Confirmation branch in `Hexalith.Agents.Server`** (AC: #1, #2, #3, #4)
  - [x] Add `Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalRequest.cs` with the server-internal request `AgentInteractionProposalRequest(string MessageId, string CorrelationId, string TenantId, string AgentInteractionId, string AgentId, string SourceConversationId, AgentResponseMode ResponseMode, int ApproverPolicyVersion, int ContentSafetyPolicyVersion, string ActorUserId, string? ClientCorrelationId = null, IReadOnlyDictionary<string, string>? ClientSuppliedExtensions = null)` and the outcome `AgentInteractionProposalOutcomeResult(string AgentInteractionId, AgentInteractionStatus Status)` — **mirror `AgentInteractionPostingRequest.cs`** (carries the snapshot-recorded ids/mode + policy-snapshot versions + caller context; NO generated content — AD-14).
  - [x] Add `Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalOrchestrator.cs` (`sealed class`) **mirroring `AgentInteractionPostingOrchestrator.cs`** as the Confirmation-mode durable-owner step. Constructor deps (all fail-closed): `IAgentGeneratedVersionReader` (reuse — to read the selected version's authoritative `VersionId`), `IProposalExpiryPolicyReader` (new), `IAgentCommandDispatcher` (reuse). `public async Task<AgentInteractionProposalOutcomeResult> ExecuteAsync(AgentInteractionProposalRequest request, CancellationToken ct)`:
    - `ArgumentNullException.ThrowIfNull(request);`
    - **(1) Response-mode short-circuit (defensive — the durable owner only invokes this for Confirmation mode):** `if (request.ResponseMode != AgentResponseMode.Confirmation) return new AgentInteractionProposalOutcomeResult(request.AgentInteractionId, AgentInteractionStatus.Generated);` (Automatic mode posts via Story 2.5 — do not create a proposal; the aggregate would reject anyway, leaving the interaction `Generated`).
    - Assemble the server-trusted `AgentProposalCreationResult` via a private `CreateAsync(request, ct)` (each step fail-closed): **(2)** read the selected generated version via `_versionReader.ReadSelectedVersionAsync(request.TenantId, request.AgentInteractionId, ct)` inside a `try/catch (Exception ex) when (ex is not OperationCanceledException)` → fail closed to `AgentGeneratedVersionReadResult.NotAvailable`. If `Outcome != Available` → `FailedResult(AgentProposalCreationOutcome.GeneratedVersionUnavailable, request, versionId: string.Empty)`. **CRITICAL (AD-14): use only `version.VersionId`; the `GeneratedContent` from the read is DELIBERATELY IGNORED** — the content's sole durable home stays the Story 2.4 `AgentOutputGenerated` event/state; the proposal references the version *id* only.
    - **(3)** read the optional expiry via `_expiryReader.ReadAsync(request.TenantId, request.AgentId, ct)` (same fail-closed `try/catch` → `new ProposalExpiryPolicyResult(null)`); take `ExpiresAt` (null when unconfigured).
    - **(4)** derive `ProposalId = AgentProposalIdentity.DeriveProposalId(request.AgentInteractionId, versionId)`.
    - **(5)** assemble `new AgentProposalCreationResult(AgentProposalCreationOutcome.Created, ProposalId, request.SourceConversationId, versionId, request.ApproverPolicyVersion, request.ContentSafetyPolicyVersion, expiresAt)` — safe ids only.
    - Build the `CreateProposedAgentReply(request.AgentInteractionId, result)` command, build a `CommandEnvelope` (AggregateId = interaction id, Domain = `agent-interaction`, `nameof(CreateProposedAgentReply)`, JSON-serialized command, the request ids/actor), strip the reserved trust keys from `ClientSuppliedExtensions` via a `BuildTrustedExtensions` helper repopulating none (copy the `_reservedExtensionKeys` set + helper verbatim from the posting orchestrator).
    - `await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false);` then `return new AgentInteractionProposalOutcomeResult(request.AgentInteractionId, AgentProposalCreationPolicy.Decide(result));` — status via the SHARED policy so it cannot drift from the aggregate decision (AD-3).
    - `FailedResult(outcome, request, versionId)` builds a content-free `AgentProposalCreationResult` carrying the attempted safe ids (empty `ProposedVersionId`/`ProposalId` for a pre-version failure), mirroring the posting orchestrator's `FailedResult`. Add `.ConfigureAwait(false)` on every await (CA2007 is warnings-as-error).
  - [x] Register in `Hexalith.Agents.Server/Program.cs` under a Story 3.1 comment block (mirror the Story 2.5 block): `builder.Services.AddSingleton<IProposalExpiryPolicyReader, DeferredProposalExpiryPolicyReader>();` and `builder.Services.AddScoped<AgentInteractionProposalOrchestrator>();`. The new aggregate `Handle` auto-registers via the existing `AddEventStoreDomainService` assembly scan (no host change). Reuse the already-registered `IAgentGeneratedVersionReader` (Story 2.5, `DeferredAgentGeneratedVersionReader`) and `IAgentCommandDispatcher` (`DeferredAgentCommandDispatcher`) — add NO new sibling-module/provider references. Note in the comment that the live read-model/expiry-policy/command-dispatch/AppHost-topology bindings remain **deferred** (the default graph fails closed to `ProposalCreationFailed`), mirroring 1.2/1.4/1.5/1.6/1.7/2.1–2.5; and that the durable-owner chaining (generate → branch on response mode → post (2.5) / propose (3.1)) is wired in the operational-topology story (Epic 4), not here.

- [x] **Task 6 - Tests + narrow verification** (AC: #1, #2, #3, #4)
  - [x] **Domain (`tests/Hexalith.Agents.Tests`):** extend `AgentInteractionTestData.cs` (reuse the existing `ConfirmationSnapshot` and `StateGeneratedConfirmationMode()` helpers added in Story 2.5): add a `ProposalResult(AgentProposalCreationOutcome outcome = Created, …)` builder (mirror `PostingResult`) with safe defaults (`ProposalId` derived/sample, `ProposedVersionId = PostedVersionId`, `ApproverPolicyVersion`/`ContentSafetyPolicyVersion` from `ConfirmationSnapshot`, `ExpiresAt = null`), a `ProposalCommand(result)` helper (mirror `PostCommand`), and the three new events in the `ApplyAll` dispatch switch (`ProposedAgentReplyCreated`, `ProposedAgentReplyCreationFailed`, `ProposedAgentReplyNotCreatableRejection`). New `AgentInteractionProposalAggregateTests`: Confirmation + `Generated` + `Created` outcome → `ProposedAgentReplyCreated`, status `ProposalCreated`, `ProposalState == Pending`, evidence ids match, no content anywhere; **Automatic** mode (`StateGenerated()`) → `ProposedAgentReplyNotCreatableRejection(NotConfirmationResponseMode)`, no state change; wrong status (Requested/Authorized/Denied/Blocked/ContextReady/ContextBlocked/GenerationFailed/SafetyFailed) under Confirmation → `ProposedAgentReplyNotCreatableRejection(OutputNotGenerated)` (cover at least `SafetyFailed` and `GenerationFailed` to prove **AC3** — no approvable proposal from unsafe/failed generation); not-requested state → `ProposedAgentReplyNotCreatableRejection(InteractionNotRequested)`; creation-failed outcomes (`GeneratedVersionUnavailable`, `AdapterFailure`) → `ProposedAgentReplyCreationFailed` + status `ProposalCreationFailed` + reason, no content; terminal idempotent re-dispatch after `ProposalCreated` AND after `ProposalCreationFailed` → `DomainResult.NoOp()` (**AC4**); replay through the real `Apply` handlers (`ProcessAndApplyAsync`) leaves deterministic state. Add proposal-state replay coverage to `AgentInteractionStateReplayTests` (the two new success Apply + the no-op rejection Apply).
  - [x] **Policy no-drift (`tests/Hexalith.Agents.Tests`):** a theory asserting `AgentProposalCreationPolicy.Evaluate(...)`'s emitted status equals `AgentProposalCreationPolicy.Decide(...)` for every `AgentProposalCreationOutcome` value (mirror the posting policy's no-drift test) so the orchestrator's reported status can never diverge from the aggregate's recorded decision.
  - [x] **Contracts (`tests/Hexalith.Agents.Contracts.Tests`):** new `AgentInteractionProposalContractsTests`: marker interfaces (`ProposedAgentReplyCreated`/`ProposedAgentReplyCreationFailed` are `IEventPayload`; `ProposedAgentReplyNotCreatableRejection` is `IRejectionEvent`); System.Text.Json round-trip for the command, both events (incl. nested `AgentProposedReplyEvidence`), and `AgentProposalCreationResult`; `ProposedAgentReplyState`, `AgentProposalCreationOutcome`, `AgentProposalCreationFailureReason`, `AgentProposedReplyNotCreatableReason`, and the two new `AgentInteractionStatus` values serialize **by name** and unknown input deserializes to the `Unknown`/sentinel; assert ordinals are unchanged (`Posted=10`, `PostingFailed=11`, `ProposalCreated=12`, `ProposalCreationFailed=13`); AD-14 no-leak: serialize the new events/result/evidence and assert the generated-content sample (`AgentInteractionTestData.GeneratedContentText`) and no secret token appears. The assembly-wide `ContractsSecretNonDisclosureTests` auto-covers the new types — confirm it stays green and unweakened.
  - [x] **Server (`tests/Hexalith.Agents.Server.Tests`):** new `AgentInteractionProposalOrchestratorTests` (xUnit v3 + Shouldly + NSubstitute, substituting `IAgentGeneratedVersionReader`/`IProposalExpiryPolicyReader`/`IAgentCommandDispatcher`): happy path (version `Available` + Confirmation → dispatches `CreateProposedAgentReply` with the deterministic `ProposalId`, returns `ProposalCreated`); **content never on the result/envelope** (configure the version reader to return content, then assert the JSON-serialized dispatched envelope `.ShouldNotContain` the content sample — AD-14); version not-available → `ProposalCreationFailed` (`GeneratedVersionUnavailable`), no dispatch-with-content; reader throws → fail-closed `ProposalCreationFailed` (not `OperationCanceledException`, which must propagate); **all-deferred default graph** (`DeferredAgentGeneratedVersionReader` + `DeferredProposalExpiryPolicyReader` + `DeferredAgentCommandDispatcher`) → `ProposalCreationFailed` (fail closed); deterministic `ProposalId` equal across two invocations; reserved trust keys (`actor:agentsAdmin`/`provider:selectionValidation`/`approver:policyValidation`/`party:linkValidation`) stripped from client-supplied extensions; defensive non-Confirmation short-circuit returns unchanged `Generated` without dispatching; `expiresAt` from the reader flows onto the result. Add `AgentProposalIdentityTests` (determinism + distinct-from-message-id since the purpose tag differs) and a `DeferredProposalExpiryPolicyReaderTests` (returns `null` expiry). Keep the structural/boundary guard tests green: `ProjectReferenceDirectionTests`, `PublicContractPackageBoundaryTests`, `StructuralSeedConformanceTests`, `ModuleLayout`, `PackageVersionCentralizationTests`, `BuildContractConformanceTests`.
  - [x] **Regression sweep (additive-enum safety):** grep the existing tests for any exhaustive enumeration/count assertion over `AgentInteractionStatus` or the `ApplyAll` dispatch (e.g. a "all statuses round-trip" or status-count test in `AgentInteractionContractsTests`/`AgentInteractionStateReplayTests`) and update it to include the two new statuses + three new events. A previously-passing exhaustive test that now omits `ProposalCreated`/`ProposalCreationFailed` is a real gap to fix, not a spurious failure. No existing aggregate handler changes, so the 2.1–2.5 handler tests stay green unchanged.
  - [x] xUnit v3 + Shouldly; PascalCase BDD-style names matching the surrounding tests (`{Method}_with_{context}_{behavior}`); no raw `Assert.*`; no NSubstitute inside aggregate/policy tests (pure command/state/event). Run from `Hexalith.Agents/`: `dotnet restore Hexalith.Agents.slnx`, `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` (must be **0 warnings / 0 errors**, warnings-as-errors), then each touched test project individually with `dotnet test <project> --configuration Release`. **Before marking the story for review:** regenerate the Debug Log test counts from the actual run output and diff the File List against `git status` (Epic 2 retro Action Item 1 — the recurring stale-count/File-List-omission finding).

## Dev Notes

### Critical Guardrails

- This story is **the Confirmation-mode branch of the response-mode step: creating a Proposed Agent Reply on the existing `AgentInteraction` aggregate after a successful, safety-passing generation — ONLY.** Do NOT implement: proposal discovery/queue (Story 3.2), edit (3.3), regenerate (3.4), approve-then-post (3.5), reject/abandon/expire **enforcement** (3.6), proposal detail/version-history/accessibility UI (3.7), the live read-model/expiry-policy/command-dispatch/Conversations/Parties bindings, or any durable-owner workflow/AppHost topology. The aggregate makes **no dependency reads**; the only impure reads are the selected-version id and the optional expiry, both in the Server orchestration (AD-3). [Source: epics.md#Epic-3; ARCHITECTURE-SPINE.md#AD-5-Proposal-Lifecycle]
- **`AgentInteraction` owns the proposal — do NOT create a new aggregate (AD-2).** The architecture is explicit: `AgentInteraction` "owns each call, generation attempt, **proposal lifecycle when applicable**, version history, approval/rejection/abandonment/expiry, automatic-post evidence, and posting outcome." A `ProposedAgentReply` is a *component of `AgentInteraction` state* (the class diagram models `AgentInteraction → ProposedAgentReply → ProposalVersion`), not a separate aggregate or a separate EventStore stream. Layer the proposal onto the existing aggregate exactly as Stories 2.2–2.5 layered the gate/context/generation/posting steps. [Source: ARCHITECTURE-SPINE.md#AD-2-Aggregate-Boundaries; #classDiagram]
- **This is the Confirmation counterpart to Story 2.5's Automatic posting — same five-part pattern.** Story 2.5 posts automatically; Story 3.1 creates a proposal. The two diverge only at the response-mode branch: the durable owner invokes the posting orchestrator for `Automatic` and the new proposal orchestrator for `Confirmation`. Mirror the 2.5 structure exactly — command + server-assembled result (safe ids only) + twin `Evaluate`/`Decide` policy + 6th aggregate `Handle` + state fields/`Apply` + thin orchestrator + deterministic id helper. [Source: 2-5-post-automatic-responses-through-conversations.md; ARCHITECTURE-SPINE.md#sequenceDiagram (confirmation vs automatic branch)]
- **A Proposed Agent Reply is NEVER a Conversation Message (AD-6, AC2).** Proposal creation performs NO Conversations write and reads NO Party identity/membership — those are posting/approval concerns (Stories 2.5/3.5). The generated content's sole durable home stays the Story 2.4 `AgentOutputGenerated` event + `AgentInteractionState.GeneratedVersions`; the proposal references the version *id* only. The orchestrator reuses `IAgentGeneratedVersionReader` to obtain the authoritative `VersionId` and **deliberately ignores the returned `GeneratedContent`**. AC2 holds by construction. [Source: ARCHITECTURE-SPINE.md#AD-6-Conversations-Boundary; prd.md#8 ("must not treat unapproved proposals as Conversation Messages"); prd.md#5 (non-goal "will not make unapproved generated content a Conversation Message")]
- **Unsafe content can never become an approvable proposal — and this is structural, not a re-check (AC3, FR-27).** Story 2.4 already gates Content Safety BEFORE any artifact: a safety failure records `SafetyFailed` and creates NO `AgentGeneratedVersion`. Story 3.1's generation precondition (`requested.Status != AgentInteractionStatus.Generated → OutputNotGenerated` rejection) means a `SafetyFailed`/`GenerationFailed` interaction can never reach proposal creation — there is structurally nothing approvable to propose (AD-5: "nothing approvable on failure"). The safety failure was already recorded as fail-closed Audit Evidence by 2.4. **Do NOT re-evaluate safety in this story** and do NOT carry content to do so. [Source: prd.md#4-10 (FR-27 "Generated content that fails Content Safety Policy cannot become an approvable Proposed Agent Reply"); 2-4-generate-and-safety-check-agent-output.md; ARCHITECTURE-SPINE.md#AD-5; #AD-14]
- **Idempotency & determinism (AC4, AD-13).** The deterministic `ProposalId = SHA-256(interactionId, versionId)` plus the aggregate's terminal no-op (`ProposalCreated`/`ProposalCreationFailed` → `NoOp`) prevent duplicate proposals on retry; the deterministic `VersionId` (derived from the 2.4 attempt id) ensures the same proposal id for the same `(interaction, version)`. Replay is total — the two success `Apply` mutate under `if (!IsRequested) return;` and the rejection `Apply` is a no-op. `ExpiresAt` is excluded from the idempotency identity (only interaction+version derive `ProposalId`), so a differing expiry across retries cannot fork the proposal. No `UtcNow`/`Guid.NewGuid` in the aggregate (occurrence time = EventStore metadata). [Source: ARCHITECTURE-SPINE.md#AD-13-Idempotent-External-Effects; AgentResponsePostingIdentity.cs; AgentInteractionAggregate.cs (PostAgentResponse no-op precedent)]
- **Fail closed before any recorded proposal (AD-12, FR-21).** Every orchestration read is wrapped fail-closed (`try/catch` excluding `OperationCanceledException`, returning the not-available default). The all-deferred default DI graph (deferred version reader/expiry reader/dispatcher) resolves every path to `ProposalCreationFailed` — content-bearing proposal creation stays disabled until the live read-model + content protection are wired (AD-14). A direct `CreateProposedAgentReply` on a non-Confirmation or non-`Generated` interaction is a structural rejection, never a silent proposal. [Source: ARCHITECTURE-SPINE.md#AD-12; prd.md#4-7 (FR-21); 2-5 posting orchestrator fail-closed precedent]
- **Authorization / approver resolution is an *approval-time* concern (Story 3.5), not a *creation-time* one.** 3.1 creates the proposal so authorized Approvers can later discover and act on it; WHO may approve is enforced when the approval is attempted (3.5), using the snapshotted `ApproverPolicyVersion` (frozen at request time, AD-4) plus current dependency availability (AD-8/AD-12). The proposal evidence records `ApproverPolicyVersion` for that later policy-basis reporting (Story 1.6 stored it; Story 3.5 enforces it). Do NOT call `IApproverPolicyResolver` in this story. [Source: 1-6-configure-response-mode-and-approver-policy.md#Critical-Guardrails (config-time vs approval-time split); ARCHITECTURE-SPINE.md#AD-4; #AD-8; epics.md#Story-3.5]

### Design: Where Each Responsibility Lives (the AD-3 round-trip, Confirmation branch)

```
Durable owner (deferred; Epic 4) — after generation, branches on snapshot.ResponseMode:
   AUTOMATIC → AgentInteractionPostingOrchestrator      (Story 2.5)  → Posted / PostingFailed
   CONFIRMATION → AgentInteractionProposalOrchestrator   (THIS STORY) → ProposalCreated / ProposalCreationFailed

CONFIRMATION path (create proposal — no Conversations write, no content carried):
  AgentInteractionProposalOrchestrator.ExecuteAsync(request)
    ├─ guard: request.ResponseMode == Confirmation (else return unchanged Generated — defensive)
    ├─ IAgentGeneratedVersionReader.ReadSelectedVersionAsync → VersionId  (content IGNORED — AD-14)   [deferred → fail closed]
    ├─ IProposalExpiryPolicyReader.ReadAsync               → ExpiresAt?    (where configured — AC1)    [deferred → null]
    ├─ ProposalId = AgentProposalIdentity.DeriveProposalId(interactionId, VersionId)                    (AD-13)
    ├─ assemble AgentProposalCreationResult(Created, ProposalId, SourceConversationId, VersionId,
    │           ApproverPolicyVersion, ContentSafetyPolicyVersion, ExpiresAt)   ← safe ids only
    └─ dispatch CreateProposedAgentReply(interactionId, result)  via IAgentCommandDispatcher  [deferred]
          envelope.Extensions: reserved trust keys stripped, none repopulated
                │
  pure AgentInteractionAggregate.Handle(CreateProposedAgentReply) ─┘
        positive IsRequested bind → terminal no-op (ProposalCreated/ProposalCreationFailed)
        → response-mode precondition (Confirmation) → generation precondition (Generated)
        → AgentProposalCreationPolicy.Evaluate → ProposedAgentReplyCreated(evidence) | ProposedAgentReplyCreationFailed(reason, evidence)
  orchestrator returns AgentProposalCreationPolicy.Decide(result)   ← cannot drift from the aggregate (AD-3)
```

[Source: ARCHITECTURE-SPINE.md#AD-3-Pure-Aggregates-Side-Effects-Outside; #AD-6; #AD-13; AgentInteractionPostingOrchestrator.cs; AgentResponsePostingPolicy.cs]

### Current Code State To Preserve

Read these completely before editing — they are the exact patterns to extend (do NOT reshape the 2.1–2.5 events, commands, or handler structure):

- `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs` — `[EventStoreDomain("agent-interaction")]`; five static `Handle(command, AgentInteractionState?, CommandEnvelope) → DomainResult` (Request/EvaluateGate/BuildContext/Generate/Post). Each: `ThrowIfNull` → `interactionId = envelope.AggregateId` → positive `state is { IsRequested: true } requested` bind → terminal idempotent `NoOp` → precondition rejection(s) → pure policy `Evaluate`. **Add the 6th `Handle(CreateProposedAgentReply, …)` mirroring `Handle(PostAgentResponse, …)`** (the response-mode precondition is the exact inverse: it requires `Confirmation`, posting requires `Automatic`).
- `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs` — `IsRequested`, `Status` (default `Unknown`), the append-only `GeneratedVersions`, the `PostingEvidence`/`PostingFailureReason` fields; `Apply(success)` mutates under `if (!IsRequested) return;`, `Apply(rejection)` is a no-op via `MarkReplayOnlyEventHandled()`. **Add the 3 proposal fields + 2 success `Apply` + 1 rejection no-op `Apply`** (mirror the posting trio).
- `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentResponsePostingPolicy.cs` — the **exact twin-policy template** (`internal static`, `Evaluate` emits the event, `Decide` returns the status, private `Compute` + `record struct` decision + `MapFailureReason`). Copy its shape for `AgentProposalCreationPolicy`.
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionStatus.cs` (`Unknown=0 … PostingFailed=11`) — append `ProposalCreated=12`, `ProposalCreationFailed=13`, ordinals preserved, by-name serialization, `Unknown` sentinel.
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/{AgentResponsePostingResult,AgentPostedMessageEvidence,AgentResponsePostingFailureReason}.cs`, `Commands/PostAgentResponse.cs`, `Events/{AgentResponsePosted,AgentResponsePostingFailed}.cs`, `Events/Rejections/AgentResponseNotPostableRejection.cs`, `AgentResponseNotPostableReason.cs` — the bare-record + `IEventPayload`/`IRejectionEvent` marker style, no wall-clock, safe-ids-only evidence. Mirror these for the proposal command/result/evidence/events/rejection/reasons. `AgentGenerationKind.cs` is the template for the reserved-additive `ProposedAgentReplyState`.
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionStatusView.cs` + `AgentInteractionInspectionResult.cs` — the safe handle Story 2.6's feedback UI polls; it already carries `Status` (which becomes `ProposalCreated`/`ProposalCreationFailed`). **Not modified in this story** — proposal read fields belong to the 3.2/3.7 read surfaces.
- `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/{AgentInteractionPostingOrchestrator,AgentInteractionPostingRequest,AgentResponsePostingIdentity}.cs` — the exact orchestrator/request-outcome/deterministic-id templates. `Ports/{IAgentGeneratedVersionReader,AgentGeneratedVersionReadResult,DeferredAgentGeneratedVersionReader,DeferredAgentCommandDispatcher,IAgentCommandDispatcher}.cs` — the reused version reader + dispatcher and the deferred-reader template for `DeferredProposalExpiryPolicyReader`. `Program.cs` — the per-story registration block style (the aggregate `Handle` auto-registers via the assembly scan; only the orchestrator + new port need wiring).
- `Hexalith.Agents/tests/Hexalith.Agents.Tests/AgentInteractionTestData.cs` — `ConfirmationSnapshot` + `StateGenerated(snapshot)`/`StateGeneratedConfirmationMode()` already exist (Story 2.5). Add `ProposalResult`/`ProposalCommand` builders + the 3 new `ApplyAll` cases. `AgentInteractionPostingAggregateTests.cs` + `tests/Hexalith.Agents.Contracts.Tests/AgentInteractionPostingContractsTests.cs` + `tests/Hexalith.Agents.Server.Tests/AgentInteractionPostingOrchestratorTests.cs` are the per-layer test templates to mirror.

What must be preserved: `.slnx` only, `net10.0`, nullable, implicit usings, **warnings-as-errors** (CA2007 `ConfigureAwait(false)` on every await), Central Package Management (no inline versions), provider-SDK-free + secret-value-free + content-free + Party-PII-free public contracts, pure replay-safe aggregates, the **no-`UtcNow`/`Guid.NewGuid`-in-aggregates** convention, and ULID-not-GUID id handling. [Source: Hexalith.EventStore/_bmad-output/project-context.md; ARCHITECTURE-SPINE.md#Stack; #Consistency-Conventions; 1-6-…md#Current-Code-State-To-Preserve]

### Naming Map (architecture domain terms → V1 code names)

| Architecture / PRD term | This story's code name | Notes |
|---|---|---|
| Proposed Agent Reply (component of `AgentInteraction`) | proposal fields on `AgentInteractionState` + `AgentProposedReplyEvidence` | Not a new aggregate (AD-2). |
| Proposal current state | `ProposedAgentReplyState` (`Unknown=0, Pending`) | Reserve AD-5 lifecycle states for 3.3–3.6 (additive). |
| `ProposalVersionId` / `VersionId` | `ProposedVersionId` (= the 2.4 `AgentGeneratedVersion.VersionId`) | The proposal's first/only version in V1. |
| Deterministic proposal id | `ProposalId` (SHA-256 of interaction+version) | AD-13 idempotency. |
| Policy snapshots | `ApproverPolicyVersion`, `ContentSafetyPolicyVersion` (from the snapshot) | Already on `AgentInteractionSnapshot` (AD-4). |
| Expiry metadata | `ExpiresAt` (nullable) + `IProposalExpiryPolicyReader` (deferred) | "Where configured"; default/min/max + enforcement deferred to 3.6. |
| Create proposal command/event | `CreateProposedAgentReply` / `ProposedAgentReplyCreated` | Confirmation counterpart to `PostAgentResponse`/`AgentResponsePosted`. |

[Source: ARCHITECTURE-SPINE.md#Consistency-Conventions (Naming/Identity); #AD-5; #AD-4; prd.md#3 (glossary: Proposed Agent Reply, Versioned Proposal Content, Approver)]

### Sensitive-Data Handling (AD-9 / AD-14 / FR-27)

- The generated content is **conversation-derived sensitive content of the same class as the caller prompt** — it lives ONLY on the Story 2.4 `AgentOutputGenerated` event + `AgentInteractionState.GeneratedVersions` and must NEVER appear on the proposal command, result, evidence, events, status view, state proposal-fields, logs, telemetry, or audit summaries. The proposal transports only safe ids (`ProposalId`, `SourceConversationId`, `ProposedVersionId`, version/policy version numbers, optional `ExpiresAt`). The orchestrator reads the selected version but **discards `GeneratedContent`** (AD-14). [Source: ARCHITECTURE-SPINE.md#AD-14; AgentGeneratedVersion.cs; prd.md#4-9 (FR-24)]
- AC3 safety-failure recording: the safe failure record already exists (Story 2.4's `AgentOutputGenerationFailed` with `SafetyFailed` + reason `ContentSafetyBlocked`, no content). This story adds no new safety surface; it only refuses to create a proposal unless status is `Generated`. The unsafe content is never exposed where forbidden because it never entered the aggregate (2.4 dropped it at the gate). [Source: 2-4-generate-and-safety-check-agent-output.md; AgentOutputGenerationFailed.cs; prd.md#4-10 (FR-27)]
- The Story 1.1 secret-non-disclosure guard (forbidden member tokens `Secret`/`ApiKey`/`Credential`/`Password`/`ConnectionString`) still applies — none are needed; keep new member names clear of them and do not weaken the `ContractsSecretNonDisclosureTests` guard. [Source: 1-6-…md#Sensitive-Data-Handling]

### Idempotency (AD-13)

- Re-dispatching `CreateProposedAgentReply` after a recorded `ProposalCreated`/`ProposalCreationFailed` is a deterministic `DomainResult.NoOp()` — no duplicate proposal, no duplicate version (AC4).
- `ProposalId` derives only from `(AgentInteractionId, ProposedVersionId)` via SHA-256, so a retried creation on the same interaction+version yields the same id; `ExpiresAt` is intentionally excluded from the identity.
- Replay is total: the two success `Apply` mutate under `if (!IsRequested) return;`; the `ProposedAgentReplyNotCreatableRejection` `Apply` is a no-op. [Source: ARCHITECTURE-SPINE.md#AD-13; AgentResponsePostingIdentity.cs; AgentInteractionState.cs]

### Latest Technical Information

- **No new references or NuGet packages this story.** Reuse the already-registered `IAgentGeneratedVersionReader` (Story 2.5) and `IAgentCommandDispatcher`; the new `IProposalExpiryPolicyReader` is in-module + deferred. Do NOT add Agent Framework, a provider SDK, Dapr-runtime, Tenants/Conversations/Parties clients, or UI packages. The proposal path touches no Conversations client at all (AD-6). [Source: Program.cs; Hexalith.Agents/Directory.Packages.props; ARCHITECTURE-SPINE.md#Stack]
- Stack baseline unchanged: .NET `10.0.300`+, `net10.0`, `.slnx`, Central Package Management, xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0`, warnings-as-errors, `ConfigureAwait(false)` enforced (CA2007). Build serialized with `-m:1` if parallel build flakes (Epic 2 toolchain note); run test projects individually (never solution-level `dotnet test`). [Source: Hexalith.EventStore/_bmad-output/project-context.md; 2-4-…md#Debug-Log; 2-5-…md]

### Testing Requirements

- **Aggregate (proposal):** Confirmation + `Generated` + `Created` → `ProposedAgentReplyCreated`, status `ProposalCreated`, `ProposalState == Pending`, evidence ids; Automatic mode → `NotConfirmationResponseMode` rejection; non-`Generated` status (incl. `SafetyFailed`/`GenerationFailed` for AC3) → `OutputNotGenerated` rejection; not-requested → `InteractionNotRequested` rejection; `GeneratedVersionUnavailable`/`AdapterFailure` outcomes → `ProposedAgentReplyCreationFailed` + status `ProposalCreationFailed`; terminal idempotent `NoOp` after both terminal states; replay determinism; Evaluate/Decide no-drift.
- **Contracts:** marker interfaces; JSON round-trip for command + 2 events + evidence + result + extended status view (incl. null proposal case); new enums + 2 new status values serialize by name; ordinals unchanged; unknown → sentinel; AD-14 no-content-leak assertions; secret guard green.
- **Server:** orchestrator happy path (deterministic `ProposalId`, returns `ProposalCreated`); content never on result/envelope; version-unavailable / reader-throws → `ProposalCreationFailed` (and `OperationCanceledException` propagates); all-deferred graph fails closed; deterministic id across retries; reserved-key stripping; defensive non-Confirmation short-circuit returns `Generated`; expiry flows through; `AgentProposalIdentity` determinism; deferred expiry reader returns null; structural/boundary guards green.
- Build/test commands (run from `Hexalith.Agents/`):
  - `dotnet restore Hexalith.Agents.slnx`
  - `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` (0 warnings / 0 errors, warnings-as-errors)
  - `dotnet test tests/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj --configuration Release`
  - `dotnet test tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release`
  - `dotnet test tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj --configuration Release`
- **Dev Agent Record accuracy (Epic 2 retro Action Item 1):** regenerate the Debug Log test counts from the latest run and diff the File List against `git status` before moving the story to review — the recurring stale-count/File-List-omission finding across all of Epic 2.

### Project Structure Notes

- New/changed code:
  - `Hexalith.Agents.Contracts/AgentInteraction/`: `ProposedAgentReplyState.cs`; `AgentProposalCreationOutcome.cs`; `AgentProposalCreationFailureReason.cs`; `AgentProposedReplyNotCreatableReason.cs`; `AgentProposedReplyEvidence.cs`; `AgentProposalCreationResult.cs`; `Commands/CreateProposedAgentReply.cs`; `Events/ProposedAgentReplyCreated.cs`; `Events/ProposedAgentReplyCreationFailed.cs`; `Events/Rejections/ProposedAgentReplyNotCreatableRejection.cs`; edit to `AgentInteractionStatus.cs` (+`ProposalCreated=12`, +`ProposalCreationFailed=13`). `AgentInteractionStatusView.cs` is intentionally **unchanged** (proposal read fields are 3.2/3.7).
  - `Hexalith.Agents/AgentInteraction/`: edits to `AgentInteractionState.cs` (+3 fields, +2 success `Apply`, +1 rejection no-op `Apply`), `AgentInteractionAggregate.cs` (+6th `Handle(CreateProposedAgentReply)`); new `AgentProposalCreationPolicy.cs`.
  - `Hexalith.Agents.Server/`: `Application/AgentInteractions/{AgentProposalIdentity,AgentInteractionProposalOrchestrator,AgentInteractionProposalRequest}.cs`; `Ports/{IProposalExpiryPolicyReader,DeferredProposalExpiryPolicyReader}.cs` (with the `ProposalExpiryPolicyResult` record); edit to `Program.cs` (register the reader + orchestrator).
  - Tests across `Hexalith.Agents.Tests` (proposal aggregate + state replay + policy no-drift + `AgentInteractionTestData` extensions), `Hexalith.Agents.Contracts.Tests` (proposal contracts), `Hexalith.Agents.Server.Tests` (proposal orchestrator + identity + deferred reader).
- Discovery loaded: root `epics.md` (Epic 3 + Story 3.1 + cross-stories 3.2–3.7), PRD (FR-13/FR-14 + FR-19/FR-20/FR-21 + FR-24 + FR-27 + non-goals + glossary), architecture spine (AD-2/AD-3/AD-4/AD-5/AD-6/AD-7/AD-8/AD-12/AD-13/AD-14/AD-18 + Structural Seed + Consistency Conventions + Deferred + the confirmation/automatic sequence + class diagrams), UX DESIGN/EXPERIENCE (proposal-editor, proposal-queue-grid, version-history, the "proposal created" live-region announcement, "never styled like a posted Conversation Message"), and the as-built Story 1.6 + Story 2.1–2.6 AgentInteraction code (aggregate/state/policies/contracts + Server orchestrations/ports/identity + test fixtures). No root `project-context.md` exists for `agents`; sibling-module `project-context.md` files (EventStore especially) supply carry-forward rules (pure replay-safe aggregates, ULID-not-GUID, `ConfigureAwait(false)`, `.slnx`-only, run test projects individually, assert persisted state not status codes, deferred read-path/dispatch bindings).

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-3.1-Create-Proposed-Agent-Replies-In-Confirmation-Mode]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-3.2-Discover-Pending-Proposals-In-Product]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-3.5-Approve-A-Selected-Version-And-Post-It]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#4-6-proposed-agent-reply-workflow]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#4-7-authorization-tenant-isolation-and-governance]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#4-9-audit-evidence-and-operational-visibility]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#4-10-content-safety-and-launch-readiness]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#3-definitions (Proposed Agent Reply, Confirmation Response Mode, Versioned Proposal Content, Approver)]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#5-non-goals]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-2-Aggregate-Boundaries]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-3-Pure-Aggregates-Side-Effects-Outside]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-4-Interaction-Snapshot]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-5-Proposal-Lifecycle]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-6-Conversations-Boundary]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-8-Approver-Policy-Resolution]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-12-Authorization-And-Dependency-Uncertainty]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-13-Idempotent-External-Effects]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-14-Sensitive-Content-And-Secret-Safety]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#accessibility-floor (proposal created live-region announcement)]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#state-patterns-proposal-lifecycle]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md#proposal-editor (never styled as a posted Conversation Message)]
- [Source: _bmad-output/implementation-artifacts/2-4-generate-and-safety-check-agent-output.md]
- [Source: _bmad-output/implementation-artifacts/2-5-post-automatic-responses-through-conversations.md]
- [Source: _bmad-output/implementation-artifacts/1-6-configure-response-mode-and-approver-policy.md#Critical-Guardrails]
- [Source: Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentResponsePostingPolicy.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionStatus.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionSnapshot.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentGeneratedVersion.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentPostedMessageEvidence.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionPostingOrchestrator.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentResponsePostingIdentity.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Ports/IAgentGeneratedVersionReader.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Program.cs]
- [Source: Hexalith.Agents/tests/Hexalith.Agents.Tests/AgentInteractionTestData.cs]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (1M context) — `claude-opus-4-8[1m]`

### Debug Log References

Build/test run from `Hexalith.Agents/` (Release, warnings-as-errors, serialized `-m:1`):

- `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` → **Build succeeded, 0 Warning(s) / 0 Error(s)**.
- `dotnet test tests/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj --configuration Release` → **Passed! Failed: 0, Passed: 557, Skipped: 0** (incl. the new `AgentInteractionProposalAggregateTests` [12] + `AgentInteractionProposalLifecycleE2ETests` [2] + 6 proposal cases in `AgentInteractionStateReplayTests` + the Evaluate/Decide no-drift theory).
- `dotnet test tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release` → **Passed! Failed: 0, Passed: 204, Skipped: 0** (incl. the new `AgentInteractionProposalContractsTests` [18]).
- `dotnet test tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj --configuration Release` → **Passed! Failed: 0, Passed: 253, Skipped: 0** (incl. `AgentInteractionProposalOrchestratorTests` [15], `AgentProposalIdentityTests` [5], `DeferredProposalExpiryPolicyReaderTests` [1]).
- Regression guard (additive-enum sweep): `dotnet test tests/Hexalith.Agents.UI.Tests/...` → **Passed! Failed: 0, Passed: 352, Skipped: 0** — the exhaustive `MapStatus_is_total_over_every_durable_status` / `Posted_is_the_only_success_color` UI checks stay green: the two new `AgentInteractionStatus` values map through the total `_ => Unknown` default (UI proposal-state presentation is owned by Stories 3.2/3.7; no UI change in this story).

Notable fix during dev: one CS4014 (warnings-as-error) in the retry test from a bare `DispatchAsync(...)` NSubstitute setup inside an `async` test — discarded with `_ =`.

### Completion Notes List

- Implemented the Confirmation-mode branch of the response-mode step as the exact twin of Story 2.5's Automatic posting: command + server-assembled result (safe ids only) + twin `Evaluate`/`Decide` policy + 6th aggregate `Handle` + 3 state fields/`Apply` + thin orchestrator + deterministic id helper. No new aggregate (AD-2) — the proposal is layered onto the existing `AgentInteraction`.
- **AC1:** `ProposedAgentReplyCreated` records the safe `AgentProposedReplyEvidence` (proposal id, source conversation id, proposed version id, approver/content-safety policy versions, optional `ExpiresAt`) linked to the interaction + Source Conversation; status → `ProposalCreated`, proposal sub-state → `Pending`. Expiry recorded "where configured" (null when no policy bound).
- **AC2:** proposal is never a Conversation Message — the path makes NO Conversations write and reads NO Party identity; the orchestrator reuses `IAgentGeneratedVersionReader` only for the authoritative `VersionId` and **deliberately ignores** the returned `GeneratedContent`. Holds by construction; asserted by content-free contract guards + the orchestrator's no-leak tests.
- **AC3:** structural — the `requested.Status != Generated → OutputNotGenerated` precondition means a `SafetyFailed`/`GenerationFailed` interaction can never reach proposal creation (no generated version to propose). No safety re-check, no content carried. Covered for both `SafetyFailed` and `GenerationFailed`.
- **AC4:** deterministic `ProposalId = SHA-256("proposal-id", interactionId, versionId)` (distinct tag from the posting message-id/idempotency-key) + the aggregate's terminal no-op on `ProposalCreated`/`ProposalCreationFailed` prevent duplicates on retry; `ExpiresAt` excluded from the identity; replay is total.
- Fail-closed: every orchestration read wrapped (`try/catch` excluding `OperationCanceledException`); the all-deferred default DI graph resolves to `ProposalCreationFailed` (`GeneratedVersionUnavailable`). `OperationCanceledException` propagates without dispatch.
- Scope held: `AgentInteractionStatusView` NOT extended (read fields are 3.2/3.7); no new NuGet/provider/Conversations/Parties references; no UI change; secret-non-disclosure guard stays green and unweakened.
- `AgentInteractionStatus` extended additively: `ProposalCreated = 12`, `ProposalCreationFailed = 13` — existing ordinals 0–11 preserved (asserted).

### File List

**Contracts (`Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/`):**

- `ProposedAgentReplyState.cs` (new)
- `AgentProposalCreationOutcome.cs` (new)
- `AgentProposalCreationFailureReason.cs` (new)
- `AgentProposedReplyNotCreatableReason.cs` (new)
- `AgentProposedReplyEvidence.cs` (new)
- `AgentProposalCreationResult.cs` (new)
- `Commands/CreateProposedAgentReply.cs` (new)
- `Events/ProposedAgentReplyCreated.cs` (new)
- `Events/ProposedAgentReplyCreationFailed.cs` (new)
- `Events/Rejections/ProposedAgentReplyNotCreatableRejection.cs` (new)
- `AgentInteractionStatus.cs` (modified — +`ProposalCreated=12`, +`ProposalCreationFailed=13`, doc updates)

**Domain (`Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/`):**

- `AgentInteractionState.cs` (modified — +3 proposal fields, +2 success `Apply`, +1 rejection no-op `Apply`)
- `AgentInteractionAggregate.cs` (modified — +6th `Handle(CreateProposedAgentReply)`)
- `AgentProposalCreationPolicy.cs` (new)

**Server (`Hexalith.Agents/src/Hexalith.Agents.Server/`):**

- `Application/AgentInteractions/AgentProposalIdentity.cs` (new)
- `Application/AgentInteractions/AgentInteractionProposalRequest.cs` (new — request + outcome records)
- `Application/AgentInteractions/AgentInteractionProposalOrchestrator.cs` (new)
- `Ports/IProposalExpiryPolicyReader.cs` (new — interface + `ProposalExpiryPolicyResult` record)
- `Ports/DeferredProposalExpiryPolicyReader.cs` (new)
- `Program.cs` (modified — Story 3.1 registration block)

**Tests:**

- `tests/Hexalith.Agents.Tests/AgentInteractionTestData.cs` (modified — proposal fixtures + 3 `ApplyAll` cases)
- `tests/Hexalith.Agents.Tests/AgentInteractionProposalAggregateTests.cs` (new)
- `tests/Hexalith.Agents.Tests/AgentInteractionProposalLifecycleE2ETests.cs` (new — full request→gate→context→generation→proposal command-chain E2E + safety-blocked AC3 end-to-end)
- `tests/Hexalith.Agents.Tests/AgentInteractionStateReplayTests.cs` (modified — proposal replay coverage)
- `tests/Hexalith.Agents.Contracts.Tests/AgentInteractionProposalContractsTests.cs` (new)
- `tests/Hexalith.Agents.Server.Tests/AgentInteractionProposalOrchestratorTests.cs` (new)
- `tests/Hexalith.Agents.Server.Tests/AgentProposalIdentityTests.cs` (new)
- `tests/Hexalith.Agents.Server.Tests/DeferredProposalExpiryPolicyReaderTests.cs` (new)

## Senior Developer Review (AI)

**Reviewer:** Administrator — 2026-06-24
**Outcome:** Approve (auto-fix applied)

### Summary

Adversarial review of the Confirmation-mode Proposed Agent Reply creation against the story's claims. Re-built (Release, `-m:1`, warnings-as-errors → **0 warnings / 0 errors**) and re-ran every touched test project. All four ACs are genuinely implemented and structurally enforced; the implementation is a faithful twin of the Story 2.5 posting path.

- **AC1** — `ProposedAgentReplyCreated` records the safe `AgentProposedReplyEvidence` (proposal id, source conversation id, proposed version id, approver/content-safety policy versions, optional `ExpiresAt`); status → `ProposalCreated`, sub-state → `Pending`. Caller/Agent/provider/mode are recorded on the same aggregate (AD-2 component design), not duplicated onto the evidence — verified consistent with the architecture.
- **AC2** — Verified by construction: the orchestrator reads the selected version for its `VersionId` only and discards `GeneratedContent`; no Conversations write / no Party read. Confirmed by contract content-free guards (`The_proposal_surfaces_have_no_content_bearing_member`) and the orchestrator no-leak tests (dispatched envelope payload `ShouldNotContain` the content sample).
- **AC3** — Structural: the `Status != Generated → OutputNotGenerated` precondition blocks `SafetyFailed`/`GenerationFailed`; the new `AgentInteractionProposalLifecycleE2ETests` proves a safety-blocked interaction can never reach a proposal end-to-end. No safety re-check, no content carried.
- **AC4** — Deterministic `ProposalId = SHA-256("proposal-id", interaction, version)` (distinct purpose tag from posting ids, asserted) + aggregate terminal no-op on both terminal statuses; `ExpiresAt` excluded from the identity; replay is total (verified across rebuilds).

### Findings (all auto-fixed — documentation only; recurring Epic 2 retro Action Item 1)

1. **[MEDIUM] File List omission** — `tests/Hexalith.Agents.Tests/AgentInteractionProposalLifecycleE2ETests.cs` (2 tests) existed in the working tree but was absent from the File List. Added.
2. **[MEDIUM] Stale Debug Log test counts** — `Hexalith.Agents.Tests` claimed 555 (actual **557**; the E2E file's [2] was uncounted/unlisted) and `Hexalith.Agents.Server.Tests` claimed 252 (actual **253**; `AgentInteractionProposalOrchestratorTests` is [15], not [14]). Counts regenerated from the actual run; contracts (204) and UI guard (352) were already accurate.

No HIGH/CRITICAL issues. No code changes required — the only defects were in the story's own bookkeeping.

### Verification (re-run 2026-06-24)

- Build `Hexalith.Agents.slnx` Release `-m:1` → 0 warnings / 0 errors.
- `Hexalith.Agents.Tests` → 557 passed; `Hexalith.Agents.Contracts.Tests` → 204 passed; `Hexalith.Agents.Server.Tests` → 253 passed; `Hexalith.Agents.UI.Tests` (additive-enum regression guard) → 352 passed. **0 failed, 0 skipped** across all four.

## Change Log

| Date | Version | Description | Author |
|---|---|---|---|
| 2026-06-24 | 0.1 | Story 3.1 implemented: Confirmation-mode Proposed Agent Reply creation (contracts + aggregate handler/state/policy + Server orchestrator/identity/expiry port + wiring + tests across 3 projects). Build 0/0; 1011 tests pass across the touched projects (+352 UI regression guard). Status → review. | Amelia (Dev) |
| 2026-06-24 | 0.2 | Senior Developer Review (AI): build + all 4 test projects re-verified green (557/204/253/352). Auto-fixed 2 MEDIUM documentation findings — added the omitted `AgentInteractionProposalLifecycleE2ETests.cs` to the File List and corrected stale Debug Log counts (555→557, 252→253). No code defects. Status → done. | Administrator (Review) |
