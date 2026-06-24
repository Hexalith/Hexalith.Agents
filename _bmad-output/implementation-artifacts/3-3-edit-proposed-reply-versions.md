---
baseline_commit: 651921b35fd2d6fe6669fc13d695dfe63f1efbcd
---

# Story 3.3: Edit Proposed Reply Versions

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an Approver,
I want to edit a Proposed Agent Reply before approval,
so that the final posted answer can be corrected while preserving what the Agent generated.

**Epic:** Epic 3 — Proposal Review And Approval Workflow.
**FR coverage:** FR14 (preserve every generated, edited, and regenerated version) and FR15 (authorized Approvers can edit Proposed Agent Replies before approval).
**Position:** Third story in Epic 3, building directly on Story 3.1 (proposal creation write-side) and Story 3.2 (proposal queue read/UI). Story 3.4 (regenerate), 3.5 (approve+post), 3.6 (reject/abandon/expire), and 3.7 (proposal detail workspace + version history + accessibility) follow.

## Acceptance Criteria

(Verbatim from `_bmad-output/planning-artifacts/epics.md#Story 3.3` — already BDD-formatted.)

1. **Authorized edit creates a new immutable version.**
   - **Given** a proposal is pending and the current user is authorized by the snapshotted Approver Policy plus current dependencies
   - **When** the Approver edits the proposed content
   - **Then** the system creates a new immutable edited version with author, timestamp, source version, and safe metadata
   - **And** prior generated or edited versions remain preserved and inspectable by authorized users.

2. **Terminal proposals cannot be edited.**
   - **Given** a proposal has reached approved, rejected, abandoned, expired, posted, or another terminal state
   - **When** an Approver attempts to edit it
   - **Then** the edit is rejected
   - **And** no new version is created.

3. **Editor labels versions distinctly and never leaks content.**
   - **Given** the editor is displayed
   - **When** generated and edited content are shown
   - **Then** each version is labeled distinctly and the proposal is never styled as an already-posted Conversation Message
   - **And** generated/editor content is excluded from logs, telemetry dimensions, status badges, and unauthorized accessible names.

4. **Edit is auditable without overwriting prior content.**
   - **Given** an edit is saved
   - **When** audit evidence is queried
   - **Then** the edit version, editor, source version, timestamp, and policy basis are available to authorized users
   - **And** previous version content is not overwritten.

## Tasks / Subtasks

> Implement the slice in this order. It mirrors the Story 3.1 "create proposal" slice exactly (which itself mirrored Story 2.5 posting): **Contracts → Domain aggregate + twin-policy → Server orchestrator → UI → tests**. The proposal lives on the **existing `AgentInteraction` aggregate** — do NOT create a new aggregate (AD-2).

- [x] **Task 1 — Contracts: additive edit surface** (AC: #1, #2, #4) — folder `src/Hexalith.Agents.Contracts/AgentInteraction/`
  - [x] Add `Edited` to `AgentGenerationKind` (`{ Unknown=0, Generated, Edited }`) and `Edited` to `ProposedAgentReplyState` (`{ Unknown=0, Pending, Edited }`). **Append only** — preserve existing ordinals; keep the `Unknown=0` sentinel and the `[JsonConverter(typeof(JsonStringEnumConverter))]` by-name serialization. Update the XML remark that previously reserved `Edited` "for Story 3.3".
  - [x] Extend `AgentGeneratedVersion` **additively** with `string? SourceVersionId` and `string? EditorPartyId` (both `null` for `Generated` versions; set for `Edited` versions). See Dev Notes "Edited-version model decision" for rationale and the recommended values for the other fields on an edited version.
  - [x] New enums: `AgentProposalEditOutcome` (`Unknown=0, Edited, AdapterFailure` — mirror `AgentProposalCreationOutcome`), `AgentProposalEditFailureReason` (recorded-failure reason; fail-closed default), `AgentProposedReplyNotEditableReason` (`Unknown=0, InteractionNotProposed, ProposalNotPending, NotAuthorized` — structural rejection reasons; map the terminal/AC2 cases here).
  - [x] New carrier `AgentProposalEditResult` (server→aggregate; **carries the edited `AgentGeneratedVersion`** including edited content + the new edited `VersionId` + `Kind=Edited` + `SourceVersionId` + `EditorPartyId`, the authorization verdict input, and the outcome). This is a legitimate content-bearing write-path type (see Dev Notes "Content confinement").
  - [x] New safe evidence record (extend or add alongside `AgentProposedReplyEvidence`): records `ProposalId`, `SourceConversationId`, new edited `VersionId`, `SourceVersionId`, `EditorPartyId`, `ApproverPolicyVersion`, and the resolved **policy basis** (approver source + disclosure category). **No edited content** on the evidence.
  - [x] `Commands/EditProposedAgentReply.cs` — `record(string AgentInteractionId, AgentProposalEditResult Result)` (imperative command name; mirror `CreateProposedAgentReply`).
  - [x] `Events/ProposedAgentReplyEdited.cs : IEventPayload` — durable success event carrying the new `AgentGeneratedVersion` (content-bearing, like `AgentOutputGenerated`) + safe evidence. `Events/ProposedAgentReplyEditFailed.cs : IEventPayload` — recorded-negative decision + reason + safe evidence (no content). `Events/Rejections/ProposedAgentReplyNotEditableRejection.cs : IRejectionEvent` — structural rejection, no state change, no content.
  - [x] Append `ProposalEdited` / `ProposalEditFailed` ordinals to `AgentInteractionStatus` if a distinct interaction-status value is wanted (append-only; the proposal stays a sub-state via `ProposedAgentReplyState.Edited`). Confirm whether a new `AgentInteractionStatus` value is needed or whether `ProposedAgentReplyState.Edited` alone is sufficient (see Dev Notes).
  - [x] Audit-evidence **view contract** for the edit (read surface): a support-safe view exposing the edit version id, `EditorPartyId`, `SourceVersionId`, timestamp (from event metadata), and policy basis — **no raw content**. The live query-handler/projection binding is **deferred to Epic 4**; only the contract ships here (precedent: Story 3.2 shipped read contracts, deferred projection).

- [x] **Task 2 — Domain: aggregate state, Apply, 7th Handle, twin policy** (AC: #1, #2) — folder `src/Hexalith.Agents/AgentInteraction/`
  - [x] `AgentProposalEditPolicy.cs` (`internal static`, `InternalsVisibleTo` Server but **not** Server.Tests) — mirror `AgentProposalCreationPolicy`: `Evaluate(string interactionId, AgentProposalEditResult result)` (aggregate path → emits `ProposedAgentReplyEdited` or `ProposedAgentReplyEditFailed`), `Decide(AgentProposalEditResult result)` (orchestrator path → returns the status), private `Compute(...)` returning a `readonly record struct` decision + `Evidence(...)` + `MapFailureReason(...)` (fail-closed default). The two members must compute the decision once so they cannot drift (no-drift theory test in Task 5). No I/O, no time, no secrets.
  - [x] `AgentInteractionState.cs` — append the new edited version to `GeneratedVersions` using the existing list-copy idiom from `Apply(AgentOutputGenerated)` (lines ~171-185). Add `Apply(ProposedAgentReplyEdited)` (append edited version; set `ProposalState = Edited`; update `ProposalEvidence`), `Apply(ProposedAgentReplyEditFailed)` (record failure reason, no version change), and a replay-only no-op `Apply(ProposedAgentReplyNotEditableRejection)` via `MarkReplayOnlyEventHandled()`. Success applies keep the `if (!IsRequested) return;` guard.
  - [x] `AgentInteractionAggregate.cs` — add the **7th** `public static DomainResult Handle(EditProposedAgentReply command, AgentInteractionState? state, CommandEnvelope envelope)` after the create handler (~line 351). Guard cascade in order: `ArgumentNullException.ThrowIfNull(command)` + `(envelope)`; `interactionId = envelope.AggregateId`; **positive bind** `if (state is { IsRequested: true } requested) { ... }` else `InteractionNotProposed` rejection (CA1062 idiom — do NOT use the negative `is not` form); terminal **idempotent no-op** (`return DomainResult.NoOp();`) when the edit decision already landed for this idempotent command / proposal already in the matching edited state; precondition rejections — proposal must exist (`requested.Status == AgentInteractionStatus.ProposalCreated` OR `ProposalEdited`) AND `requested.ProposalState` is in the **editable set** `{ Pending, Edited }` else `ProposalNotPending` rejection (AC2: approved/rejected/abandoned/expired/posted ⇒ reject, no version); finally `return AgentProposalEditPolicy.Evaluate(interactionId, command.Result);`.

- [x] **Task 3 — Server: edit orchestrator, identity, edit-time approver authorization, ports, DI** (AC: #1, #2, #4) — folders `src/Hexalith.Agents.Server/Application/AgentInteractions/`, `Ports/`, `Program.cs`
  - [x] `AgentInteractionProposalEditOrchestrator.cs` (`public sealed class`, `AddScoped`) — mirror `AgentInteractionProposalOrchestrator`. Impure: it performs ALL I/O, then dispatches **exactly one** `EditProposedAgentReply` command. Steps: (a) **resolve edit-time authorization** = read the proposal's snapshotted `ApproverPolicyVersion`, resolve the corresponding `AgentApproverPolicy` via `IApproverPolicyResolver.ResolveAsync(...)`, then compute the verdict via `ApproverPolicyVerdict.Evaluate(policy, resolution)` — **fail closed** on any `NotAuthorized`/`Unavailable`/`Ambiguous`/`Missing`/`Disabled`/uncertain result; check freshness before trusting any projected dependency (`Freshness.AllowsTrustBearingDecision()` / `IsFresh` precedent). (b) derive the deterministic **edited version id** via a new `AgentProposalEditIdentity.DeriveEditedVersionId(...)` (new SHA-256 purpose tag, e.g. `"proposal-edit-version-id"`, distinct from `"proposal-id"`; inputs `interactionId` + `sourceVersionId` + a deterministic `EditAttemptId` carried on the request so retries do not fork — AD-13). (c) assemble the `AgentProposalEditResult` (new `AgentGeneratedVersion` with the edited content + ids + `Kind=Edited` + `SourceVersionId` + `EditorPartyId`, the verdict, the outcome). (d) build the `CommandEnvelope` (mirror create: `"agent-interaction"` domain, `nameof(EditProposedAgentReply)`, `SerializeToUtf8Bytes`, trusted extensions with reserved trust keys stripped). (e) `await _dispatcher.DispatchAsync(envelope, ct).ConfigureAwait(false)`. (f) `return new ...OutcomeResult(editedVersionId, AgentProposalEditPolicy.Decide(result))` — **safe ids + status only, no content**. Every external read wrapped in `try/catch (Exception ex) when (ex is not OperationCanceledException)` returning a fail-closed default; `OperationCanceledException` must propagate; every await `.ConfigureAwait(false)`.
  - [x] `AgentInteractionProposalEditRequest.cs` — server-internal `sealed record` request (tenant, interaction id, source version id, **edited content**, editor party id, deterministic edit attempt id, correlation/actor) + `AgentInteractionProposalEditOutcomeResult(string EditedVersionId, AgentInteractionStatus Status)`.
  - [x] `AgentProposalEditIdentity.cs` (`internal static`) — the deterministic id helper above. No ULID / `Guid.NewGuid` / wall-clock.
  - [x] Reuse `IApproverPolicyResolver` + `ApproverPolicyVerdict` (already exist in `Ports/` and `Application/Agents/`). Reuse `IAgentCommandDispatcher`. Add a new read port only if the orchestrator must load the current proposal/source-version state to validate the edit (e.g. an `IProposedReplyReader` returning safe proposal status + selected `SourceVersionId`), each with a `Deferred*` fail-closed implementation returning a safe denial.
  - [x] `Program.cs` — add a Story 3.3 registration block parallel to the 3.1 block: `AddScoped<AgentInteractionProposalEditOrchestrator>()` + any new `Deferred*` port binding. The aggregate `Handle` auto-registers via the EventStore domain-service scan — no host change for the handler.

- [x] **Task 4 — UI: fail-closed edit gateway + proposal-editor component + version labeling + localization** (AC: #1, #3) — folder `src/Hexalith.Agents.UI/`
  - [x] `Services/Gateways/IProposalEditGateway.cs` + `Services/Gateways/DeferredProposalEditGateway.cs` — fail-closed (returns a `NotAuthorized`/`Unavailable` edit result; never throws on the happy path; `OperationCanceledException` propagates). Register in `AgentsUiServiceCollectionExtensions.cs`. Mirror `IProposalQueueGateway`/`DeferredProposalQueueGateway`.
  - [x] `Components/Shared/` — version-labeling presentation helper (total switch over `AgentGenerationKind`, `_ =>` default) so `Generated` and `Edited` versions are labeled **distinctly**; reuse `ProposedAgentReplyStatePresentation` + `ProposedAgentReplyStateBadge` for the `Edited` state badge (add the `Edited` cases — totality required).
  - [x] `proposal-editor` component (DESIGN.md `proposal-editor`): a **Constrained** editor region with an editable content area (Fluent v5 component — verify the pinned RC name; the queue used `FluentTextInput` because `FluentTextField`/`FluentSearch` do not exist in `5.0.0-rc.3-26138.1`; for a multi-line editor confirm the correct Fluent v5 editor/textarea component before use), distinct generated-vs-edited version labels, a save action wired to `IProposalEditGateway`, and **read-only** rendering for non-authorized viewers. The proposal must **never** be styled like a posted Conversation Message; generated reply text uses the inherited `body` typography role (no display typography for AI text). **No content** in `aria-label`/`data-testid`/tooltips/diagnostics.
  - [x] Inject `TimeProvider` (never inline `DateTimeOffset.UtcNow`). Page-level `@attribute [Authorize(Policy = AgentsApproverPolicy)]` if a routable surface is added.
  - [x] `Resources/AgentsResources.resx` + `AgentsResources.fr.resx` — whole-string localized keys for editor labels, the `Edited` badge/state text, "edited version"/"generated version" labels, editor/author labels, and save/denial copy. **en + fr parity** (auto-enforced by `LocalizationResourceTests`). Never assemble sentences from fragments; never call unapproved content a "message".
  - [x] **Scope boundary (read this):** Task 4 delivers the editor *interaction* (edit + distinct version labeling + leak-safe save). The full **proposal detail workspace**, the **version-history list panel**, and the **complete keyboard/live-region/`Esc`-without-commit accessibility** are owned by **Story 3.7** (do not duplicate). If composing the editor into a routable detail page is not needed to satisfy AC3 at the component level, deliver the component + gateway + bUnit tests and let 3.7 host it.

- [x] **Task 5 — Tests (all layers)** (AC: #1, #2, #3, #4) — run each test project individually
  - [x] **Contracts** (`tests/Hexalith.Agents.Contracts.Tests/AgentInteractionProposalEditContractsTests.cs`): marker-interface asserts (`IEventPayload`/`IRejectionEvent`), JSON round-trip incl. the edited `AgentGeneratedVersion`, by-name enum + `Unknown`→sentinel for the new enums, **ordinal stability** for the appended `AgentGenerationKind.Edited`/`ProposedAgentReplyState.Edited` (and any new `AgentInteractionStatus` ordinals), backward-compat of the new nullable `AgentGeneratedVersion` fields (absent ⇒ null). **No-leak nuance:** assert the **evidence record, the orchestrator outcome result, the rejection, and any view** `ShouldNotContain(GeneratedContentText)` — but do **NOT** assert that on `ProposedAgentReplyEdited`/the edited `AgentGeneratedVersion`, which legitimately carry content (same as `AgentOutputGenerated`). The assembly-wide `ContractsSecretNonDisclosureTests` covers the new types automatically.
  - [x] **Aggregate** (`tests/Hexalith.Agents.Tests/AgentInteractionProposalEditAggregateTests.cs`): build state via a new `StateProposalCreated()` helper (extend `AgentInteractionTestData`); assert authorized edit emits `ProposedAgentReplyEdited` with the appended version + `ProposalState=Edited` and **prior versions preserved** (AC1); AC2 terminal/non-pending states ⇒ `ProposedAgentReplyNotEditableRejection`, **no new version**; idempotent terminal no-op after the edit decision landed; replay determinism through real `Apply`. Plus the **`Evaluate`/`Decide` no-drift theory** over every outcome value. Plus `AgentInteractionProposalEditLifecycleE2ETests.cs` (full request→…→generated→proposal-created→edit chain via `ProcessAndApplyAsync`).
  - [x] **Server** (`tests/Hexalith.Agents.Server.Tests/AgentInteractionProposalEditOrchestratorTests.cs`, `AgentProposalEditIdentityTests.cs`): NSubstitute the ports; capture the dispatched `CommandEnvelope` and assert `CommandType`/`Domain`/`AggregateId`; assert **fail-closed authorization** (each `ApproverSourceOutcome` non-`Resolved` ⇒ denied edit, no dispatch), `OperationCanceledException` propagates, all-deferred graph denies, reserved trust keys stripped, deterministic edited-version id (distinct purpose tag), and the outcome result carries **no content**. Avoid CS4014: discard bare NSubstitute `DispatchAsync(...)` setups with `_ =`.
  - [x] **UI** (`tests/Hexalith.Agents.UI.Tests/`): bUnit editor-component tests (renders editable region for authorized, read-only for unauthorized, distinct version labels, save calls the gateway, no content in accessible names); extend `BadgeConformanceTests`/`ProposedAgentReplyStatePresentationTests` for the `Edited` state (color+icon+text totality, no raw hex); `LocalizationResourceTests` en/fr parity for the new keys; `DeferredGatewayTests` for `DeferredProposalEditGateway`; `AccessibilityTests` for the editor surface. Extend `AgentUiTestData` with edit factories.

- [x] **Task 6 — Build, verify, and finalize the Dev Agent Record accurately** (recurring Epic-2 action item — do not skip)
  - [x] From `Hexalith.Agents/`: `dotnet restore Hexalith.Agents.slnx`; `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` → must be **0 warnings / 0 errors** (warnings-as-errors). Then run each touched test project individually.
  - [x] **Regenerate the Debug Log test counts from the actual run output** and **diff the File List against `git status`** before moving to review (this is the #1 recurring review finding across Epic 2 and stories 3.1/3.2 — stale counts + File List omissions). The File List must list every created/modified file, including test files.

## Dev Notes

### Critical guardrails (read before coding)

- **One aggregate (AD-2).** The proposal/version domain lives on the **existing `AgentInteraction` aggregate** (`[EventStoreDomain("agent-interaction")]`). Do not create a new aggregate. The edit is the 7th `Handle` on that aggregate. [Source: ARCHITECTURE-SPINE.md#AD-2 - Aggregate Boundaries; `src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs`]
- **Pure aggregate, side effects outside (AD-3).** The aggregate `Handle` emits events only — no I/O, no provider/Conversations/Tenants reads, no `UtcNow`, no `Guid.NewGuid`. All authorization resolution, id derivation, and content assembly happen in the **orchestrator** (Server, impure), which feeds the aggregate a trusted verdict + pre-derived ids via the single `EditProposedAgentReply` command. [Source: ARCHITECTURE-SPINE.md#AD-3 - Pure Aggregates, Side Effects Outside]
- **Append-only, immutable versions (AD-5).** Editing **appends** a new immutable `AgentGeneratedVersion` to `state.GeneratedVersions`; it never mutates or removes a prior version. Proposal state moves to `Edited`. AC2 terminal states (approved/rejected/abandoned/expired/posted — anything not in the editable `{ Pending, Edited }` set) cannot be edited because they cannot later post. [Source: ARCHITECTURE-SPINE.md#AD-5 - Proposal Lifecycle]
- **Fail closed (AD-12).** Edit-time authorization runs before the event is accepted and fails closed on missing/stale/ambiguous/disabled/unavailable approver-policy or dependency state. Editor rights = the **snapshotted `ApproverPolicyVersion`** (frozen at request time, AD-4) re-resolved against **current dependency availability** + freshness. [Source: ARCHITECTURE-SPINE.md#AD-12 - Authorization And Dependency Uncertainty; #AD-4 - Interaction Snapshot]
- **Idempotent edited version (AD-13).** Derive the edited version id deterministically from `(interactionId, sourceVersionId, EditAttemptId)` with a **new, distinct SHA-256 purpose tag** so a retried edit command does not create a duplicate version. Carry a deterministic `EditAttemptId` on the request. [Source: ARCHITECTURE-SPINE.md#AD-13 - Idempotent External Effects]
- **Sensitive content (AD-14).** Edited content is sensitive conversation-derived content. See "Content confinement" below for exactly where it may and may not appear. [Source: ARCHITECTURE-SPINE.md#AD-14 - Sensitive Content And Secret Safety]

### The step-pattern (mandatory — mirror Story 3.1's create slice exactly)

The epic-2 retrospective codified this as the load-bearing convention: **orchestrator → single command → twin-policy (`Evaluate`/`Decide`)**.

1. **Orchestrator** (impure, Server) does all I/O — authorization resolution, freshness checks, id derivation, content assembly — then dispatches **exactly one** command.
2. **Pure aggregate `Handle`** (the 7th) validates preconditions + the orchestrator-supplied verdict and emits events only.
3. **Twin-policy** `AgentProposalEditPolicy`: `Evaluate(...)` (aggregate emits the event) and `Decide(...)` (orchestrator returns the status) compute the decision **once** so they cannot drift. A **no-drift theory test** asserts `Evaluate`'s emitted status equals `Decide` for every outcome. The policy is `internal static` with `InternalsVisibleTo` the Server project but **not** Server.Tests (forces tests through the public surface).

The exact template to copy is the Story 3.1 create slice:
- Aggregate handler: `src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs` `Handle(CreateProposedAgentReply...)` (~lines 303-351).
- Twin policy: `src/Hexalith.Agents/AgentInteraction/AgentProposalCreationPolicy.cs`.
- State + Apply: `src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs` (`GeneratedVersions` append idiom in `Apply(AgentOutputGenerated)` ~lines 171-185).
- Orchestrator: `src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalOrchestrator.cs`.
- Identity: `src/Hexalith.Agents.Server/Application/AgentInteractions/AgentProposalIdentity.cs` (SHA-256, distinct purpose tags).
- DI block: `src/Hexalith.Agents.Server/Program.cs` (~lines 159-172).

[Source: `_bmad-output/implementation-artifacts/3-1-create-proposed-agent-replies-in-confirmation-mode.md`; `_bmad-output/implementation-artifacts/epic-2-retro-2026-06-24.md#Action Items]

### Aggregate handler guard cascade (the exact ordered idiom)

```
ArgumentNullException.ThrowIfNull(command);
ArgumentNullException.ThrowIfNull(envelope);
string interactionId = envelope.AggregateId;

// Positive bind — REQUIRED (CA1062). The negative `state is not { IsRequested: true }`
// is NOT recognised as a null-guard and fails warnings-as-errors.
if (state is { IsRequested: true } requested)
{
    // terminal idempotent no-op (AD-13) — return DomainResult.NoOp();
    // precondition: proposal exists  (Status is ProposalCreated/ProposalEdited)  else InteractionNotProposed
    // precondition: ProposalState in { Pending, Edited }  else ProposalNotPending   (AC2)
    return AgentProposalEditPolicy.Evaluate(interactionId, command.Result);
}

return DomainResult.Rejection([new ProposedAgentReplyNotEditableRejection(interactionId, InteractionNotProposed)]);
```

[Source: `src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs`; retro CA1062 idiom in `epic-2-retro-2026-06-24.md`]

### Edited-version model decision (resolve this in Task 1)

`AgentGeneratedVersion` today is `(VersionId, AttemptId, Kind, GeneratedContent, ProviderId, ModelId, ProviderCapabilityVersion, ContentSafetyPolicyVersion, PromptTokenCount, OutputTokenCount)` and has **no author or source-version fields**. AC1 requires the edited version to carry **author, timestamp, and source version**.

**Recommended (additive, additive-first per AD-17):** add two nullable fields to `AgentGeneratedVersion` — `string? SourceVersionId` and `string? EditorPartyId` — both `null` for `Generated` versions, set for `Edited` versions. For an edited version: `Kind = Edited`, `AttemptId` = the deterministic `EditAttemptId`, `ProviderId`/`ModelId`/`ProviderCapabilityVersion` inherited from the source version (so audit shows provenance) or empty, `PromptTokenCount`/`OutputTokenCount = 0` (no provider call), `ContentSafetyPolicyVersion` = the snapshot's value. **Timestamp is the EventStore event metadata** (AD-3 — no wall-clock field on the event/version); the audit view reads it from event metadata. This keeps version history self-describing for the 3.7 version-history panel and is backward compatible (absent fields deserialize to null). The alternative (modeling edit metadata only on the evidence record) is rejected: it leaves the version-history element unable to describe its own author/source. Confirm the contract tests assert ordinal stability and backward-compat of the new nullable fields.

### Content confinement (AD-14) — the key difference from Story 3.1

In the **create** slice the content was already durable from generation, so the orchestrator read it and **discarded** it (used `VersionId` only). In the **edit** slice the new content **originates from the user**, so it must travel the write path: **UI → `IProposalEditGateway` → `EditProposedAgentReply` command → `AgentProposalEditResult` → aggregate → `ProposedAgentReplyEdited` event → `state.GeneratedVersions`.** These are the **legitimate, payload-protected durable homes** for edited content — exactly analogous to how `AgentOutputGenerated` carries generated content.

Edited content **must NOT** appear on: the safe evidence record, the orchestrator outcome result returned to the UI, any rejection, any read view (`PendingProposalView`, audit-evidence view), `AgentInteractionStatus`/status views, logs, telemetry dimensions, status badges, `aria-label`/`data-testid`/tooltips/diagnostics/live-region announcements.

**Test implication:** the no-leak assertions (`ShouldNotContain(GeneratedContentText)`) apply to the **evidence, outcome result, rejection, and views** — NOT to `ProposedAgentReplyEdited` or the edited `AgentGeneratedVersion` (which correctly contain content). Writing the no-leak test against the edit event itself is a mistake that will produce a wrong/failing test. The assembly-wide `ContractsSecretNonDisclosureTests` (forbidden tokens `Secret`/`ApiKey`/`Credential`/`Password`/`ConnectionString`) still applies; use exact-name / namespace-scoped matching for content fields (substring guards trip on safe token-count fields). [Source: ARCHITECTURE-SPINE.md#AD-14; `3-1-...md`; `3-2-...md`]

### Authorization / approver policy (this is the first edit-time use)

Story 3.1 deliberately **did not** resolve the approver ("WHO may act is an approval-time concern"). Story 3.3 is where **edit-time** approver authorization begins, using the mechanism that already exists:

- **Snapshot side:** `AgentInteractionSnapshot.ApproverPolicyVersion` (int), also copied onto the proposal evidence. The snapshot freezes the version; the full policy is re-resolved live against current dependencies.
- **Resolver port:** `src/Hexalith.Agents.Server/Ports/IApproverPolicyResolver.cs` → `Task<ApproverPolicyResolutionResult> ResolveAsync(tenantId, AgentApproverPolicy policy, ct)`, per-source `ApproverSourceOutcome` (`Unknown=0, Resolved, Missing, Disabled, Ambiguous, Unavailable, Unauthorized`). Deferred fail-closed impl: `DeferredApproverPolicyResolver.cs`.
- **Pure verdict:** `src/Hexalith.Agents.Server/Application/Agents/ApproverPolicyVerdict.cs` → `Evaluate(AgentApproverPolicy policy, ApproverPolicyResolutionResult result)` with fail-closed precedence (empty→Incomplete, partial→Unavailable, any Unauthorized dominates, then Unavailable/Ambiguous/Disabled/Missing, else Valid).
- **Precedents to mirror:** `AgentInteractionGateOrchestrator.cs` (`ResolveApproverPolicyAsync` + freshness aggregation via `IsFresh`) and `AgentActivationProviderRevalidation.cs` (snapshot → re-resolve against current → verdict).
- **V1 owner authority** = `ParticipantRole.Facilitator` (Conversations exposes no owner field yet, AD-8).
- **Policy basis (AC4):** record the resolved approver source + the disclosure category (Story 1.6 stored a disclosure category for policy-basis reporting) on the safe edit evidence — user-visible/operator-only/redacted/omitted as configured. **No raw policy internals or claims.**

Editor rights live in the **orchestrator** (impure); the aggregate receives only a trusted verdict. Fail closed on any uncertainty. [Source: ARCHITECTURE-SPINE.md#AD-8, #AD-12; `src/Hexalith.Agents.Server/Ports/IApproverPolicyResolver.cs`; `.../Application/Agents/ApproverPolicyVerdict.cs`]

### Contracts conventions

Bare `record`s; events implement `IEventPayload`, rejections implement `IRejectionEvent`; enums `[JsonConverter(typeof(JsonStringEnumConverter))]` with `Unknown=0` fail-safe sentinel, serialized **by name**; **ordinals are append-only — never reorder/renumber** (asserted by tests). No wall-clock on events (occurrence time = EventStore metadata). The Contracts assembly is **inward-only**: no provider SDK, no `Microsoft.Agents.AI`, no Dapr, no server infra, no sibling-module types, no `Hexalith.Conversations`/`Hexalith.Parties` types. Naming: events past-tense (`ProposedAgentReplyEdited`, `...EditFailed`), commands imperative (`EditProposedAgentReply`), structural rejections `...NotEditableRejection : IRejectionEvent`, recorded-failure events `...EditFailed : IEventPayload` (distinct from rejections). [Source: `3-1-...md`; ARCHITECTURE-SPINE.md#Consistency Conventions]

### Read side / projections

`src/Hexalith.Agents.Server/Projections/` is still `.gitkeep`-only — **projection/query-handler binding is deferred to Epic 4** (precedent: stories 1.2, 2.6, 3.2 shipped read contracts and deferred the live binding). For Story 3.3, ship the **audit-evidence view contract** for the edit (Task 1) and the **fail-closed `IProposalEditGateway`** (Task 4); the live EventStore projection + audit query handler arrive in Epic 4. The editor must read version content for display only via an authorized reader port from the durable `AgentGeneratedVersion`/`GeneratedVersions` — never from a content-bearing projection (there is none; AD-14). [Source: `3-2-...md`; retro Epic-3 readiness notes]

### UI / FrontComposer conventions (UX guardrails)

- **proposal-editor** = a **Constrained** page measure (judgment-heavy), padding `{spacing.4}`, fields `{spacing.4}` / sections `{spacing.6}`. Editable content where authorized; **read-only** for non-authorized viewers; never styled like a posted Conversation Message. [Source: DESIGN.md#Components → proposal-editor; EXPERIENCE.md#Component Patterns]
- **Distinct version labeling** is the AC3 core: generated vs edited content labeled distinctly; "Prior generated versions are preserved." (not "Old draft replaced."). Generated/AI text uses the inherited `body` typography role — no display typography for AI text. Identifiers/timestamps use `{typography.mono}`. [Source: DESIGN.md#Components → version-history, #Typography; DESIGN.md#Do's and Don'ts]
- **proposal-state-badge** must render the `Edited` state. `Edited` is part of the in-progress/pending set → `BadgeColor.Informative` ("in progress or waiting"). Every status = semantic **color + icon + visible text** (no color-only). Icons come ONLY from the curated `FcFluentIcons` factory; verify the glyph exists in the pinned RC. `Brand` is chrome/primary-action only, never a status. [Source: DESIGN.md#Components → proposal-state-badge, #Colors; UX status-role semantics]
- **Fluent v5 reality (pinned `5.0.0-rc.3-26138.1`):** never raw `<button>/<input>/<select>/<textarea>` (raw `<a>` ok). The queue used `FluentTextInput` (`TextInputType.Search`) because `FluentSearch`/`FluentTextField` **do not exist** in this RC; pinned `FluentDataGrid` columns need explicit `Width`. **Verify the correct Fluent v5 multi-line editor/textarea component name against the pinned RC before using it.** [Source: `3-2-...md` Completion Notes]
- **Localization (whole strings, en+fr parity):** all labels, states, denial reasons, editor/author labels, and audit-basis text are localizable whole strings with named placeholders — never assembled from fragments; never call unapproved content a "message". Auto-enforced by `LocalizationResourceTests`. [Source: DESIGN.md#Typography; EXPERIENCE.md#Voice and Tone; FC-L10N]
- **Deterministic time:** inject `TimeProvider`, pass `GetUtcNow()` into pure helpers; tests use `FixedTimeProvider`/`FakeTimeProvider`. **Authorization:** page `@attribute [Authorize(Policy = AgentsApproverPolicy)]` (`"Agents.Approver"`, introduced by 3.2; the `AddPolicy` registration lives in the deferred host) — defense-in-depth; nav hiding is not authorization. UI depends only on the gateway abstraction + public contracts, never on `Hexalith.Agents.Server` (enforced by `ProjectReferenceDirectionTests`). [Source: `3-2-...md`; `Composition/AgentsFrontComposerRegistration.cs`]
- **Deferred to Story 3.7:** full proposal **detail workspace**, the **version-history list panel**, and complete **keyboard/live-region/`Esc`-without-commit** accessibility (UX-DR7/8/35). Story 3.3 delivers the editor interaction + distinct labeling + leak-safe save; 3.7 hosts and completes it. [Source: epics.md#Story 3.7]

### Project Structure Notes

- New Contracts files → `src/Hexalith.Agents.Contracts/AgentInteraction/` (+ `/Commands`, `/Events`, `/Events/Rejections`, `/Queries` for the audit view). Additive edits to `AgentGenerationKind.cs`, `ProposedAgentReplyState.cs`, `AgentGeneratedVersion.cs`, and (if needed) `AgentInteractionStatus.cs`.
- New Domain files → `src/Hexalith.Agents/AgentInteraction/` (`AgentProposalEditPolicy.cs`); edits to `AgentInteractionAggregate.cs`, `AgentInteractionState.cs`.
- New Server files → `src/Hexalith.Agents.Server/Application/AgentInteractions/` (`AgentInteractionProposalEditOrchestrator.cs`, `AgentInteractionProposalEditRequest.cs`, `AgentProposalEditIdentity.cs`); edits to `Program.cs`; new `Ports/` + `Deferred*` only if a proposal/source-version reader is required.
- New UI files → `src/Hexalith.Agents.UI/Services/Gateways/` (`IProposalEditGateway.cs`, `DeferredProposalEditGateway.cs`), `Components/Shared/` (editor + version-label presentation), edits to `Resources/AgentsResources.resx` + `.fr.resx`, `AgentsUiServiceCollectionExtensions.cs`.
- No conflicts detected with the existing structure; all changes are additive and follow the established folder conventions. Aggregate `Handle` auto-registers via the EventStore domain scan — no host wiring for the handler.

### Testing

- **Four test projects, run individually** (never solution-level `dotnet test`): `Hexalith.Agents.Tests` (domain, pure — no NSubstitute in aggregate/policy tests), `Hexalith.Agents.Contracts.Tests` (JSON/markers/enums/ordinals/no-leak), `Hexalith.Agents.Server.Tests` (orchestrator with NSubstituted ports, identity, deferred readers), `Hexalith.Agents.UI.Tests` (bUnit + presentation).
- xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0`, bUnit. **No raw `Assert.*`.** Names: `Method_with_context_behavior` (domain/server), `Subject_scenario_expectation` (UI).
- **Extend, don't reinvent fixtures:** `tests/Hexalith.Agents.Tests/AgentInteractionTestData.cs` (add `StateProposalCreated()`, `ProposalEditResult(...)`, `EditCommand(...)`, and the new `ApplyAll`/`ProcessAndApplyAsync` cases; reuse `GeneratedContentText` for no-leak); UI `AgentsTestContext`, `AgentUiTestData`, `StubAgentsLocalizer`, `FixedTimeProvider`.
- **Mandatory categories:** marker-interface asserts; JSON round-trip; by-name enum + unknown→sentinel; **ordinal-unchanged**; AD-14 no-leak (evidence/result/rejection/views only — see Content confinement); `Evaluate`/`Decide` **no-drift theory**; terminal idempotent no-op; replay determinism through real `Apply`; orchestrator fail-closed + `OperationCanceledException`-propagates + all-deferred-graph + reserved-trust-key-stripping; presentation **totality** over every enum value.
- **Build:** from `Hexalith.Agents/`: `dotnet restore Hexalith.Agents.slnx`; `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` (serialized; parallel build returns exit 1 with no diagnostics) → **0 warnings, warnings-as-errors**. If VSTest fails to open its TCP listener (`SocketException (13)`) in the sandbox, run the built xUnit v3 executables directly.

### Avoid these recurring review findings (from Epic 2 retro + stories 3.1/3.2)

1. **Dev Agent Record drift (the #1 finding, hit every Epic-2 story + 3.1 + 3.2):** regenerate test counts from the actual run and diff the File List against `git status` before review. List every file, including tests.
2. **CA1062:** use the positive `state is { IsRequested: true } requested` bind, not the negative `is not` form.
3. **CA2007:** `.ConfigureAwait(false)` on every await.
4. **CS4014:** discard bare NSubstitute `DispatchAsync(...)` setups in async tests with `_ =`.
5. **Contract-conformance edges:** "no custom attributes" asserts trip on compiler `Nullable*Attribute`; substring "no content member" guards trip safe token-count fields — use exact-name/namespace-scoped matching.
6. **Toolchain:** build with `-m:1`; run test executables directly if VSTest socket fails.
7. **Pinned-RC components:** verify Fluent v5 component names + icon glyphs against `5.0.0-rc.3-26138.1` before use.
8. **Fail-open-on-stale-projection (the dangerous bug class):** any trust-bearing read must check freshness (`Freshness.AllowsTrustBearingDecision()` / `IsFresh`) before acting. Relevant to the edit orchestrator's authorization reads.

### References

- [Source: `_bmad-output/planning-artifacts/epics.md#Story 3.3: Edit Proposed Reply Versions`] — story + AC.
- [Source: `_bmad-output/planning-artifacts/epics.md#Requirements Inventory`] — FR14, FR15, FR19, FR20, NFR3, NFR5; stack versions (Additional Requirements).
- [Source: `ARCHITECTURE-SPINE.md#AD-2 - Aggregate Boundaries`], [`#AD-3 - Pure Aggregates, Side Effects Outside`], [`#AD-4 - Interaction Snapshot`], [`#AD-5 - Proposal Lifecycle`], [`#AD-8 - Approver Policy Resolution`], [`#AD-12 - Authorization And Dependency Uncertainty`], [`#AD-13 - Idempotent External Effects`], [`#AD-14 - Sensitive Content And Secret Safety`], [`#AD-17 - Contract And Test Gates`], [`#Consistency Conventions`], [`#Structural Seed`] (path: `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md`).
- [Source: `ux-designs/ux-agents-2026-06-23/DESIGN.md#Components → proposal-editor / version-history / proposal-state-badge / audit-evidence-panel`, `#Colors`, `#Typography`, `#Layout & Spacing`, `#Do's and Don'ts`].
- [Source: `ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#Component Patterns`, `#State Patterns → Proposal Lifecycle`, `#Interaction Primitives`, `#Accessibility Floor`, `#Voice and Tone`, `#Responsive & Platform`, `#FrontComposer Readiness`, `#Key Flows → UJ-3`].
- [Source: `_bmad-output/implementation-artifacts/3-1-create-proposed-agent-replies-in-confirmation-mode.md`] — the create slice to mirror (commands/events/policy/orchestrator/identity/state/Apply, content-discard pattern, reserved `Edited` enum values).
- [Source: `_bmad-output/implementation-artifacts/3-2-discover-pending-proposals-in-product.md`] — read contracts + UI gateway + badge/presentation + Fluent v5 RC realities + `AgentsApproverPolicy` + row-actions-deferred-to-3.3.
- [Source: `_bmad-output/implementation-artifacts/epic-2-retro-2026-06-24.md`] — step-pattern convention, CA1062 idiom, Dev Agent Record drift action item, fail-open-on-stale-projection bug class.
- Code anchors: `src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs`, `.../AgentInteractionState.cs`, `.../AgentProposalCreationPolicy.cs`; `src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalOrchestrator.cs`, `.../AgentProposalIdentity.cs`; `src/Hexalith.Agents.Server/Ports/IApproverPolicyResolver.cs`; `src/Hexalith.Agents.Server/Application/Agents/ApproverPolicyVerdict.cs`; `src/Hexalith.Agents.Contracts/AgentInteraction/{AgentGeneratedVersion,AgentGenerationKind,ProposedAgentReplyState,AgentProposedReplyEvidence,AgentInteractionSnapshot,AgentInteractionStatus}.cs`; `src/Hexalith.Agents.UI/Services/Gateways/IProposalQueueGateway.cs`, `Components/Shared/ProposedAgentReplyStatePresentation.cs`.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (claude-opus-4-8, 1M context) — BMAD dev-story workflow.

### Debug Log References

Per-project Release builds: `dotnet build <project> --configuration Release -m:1` → **0 warnings / 0 errors** for every project (warnings-as-errors). Full-solution build `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` → **0 warnings / 0 errors**.

Tests were run from the built xUnit v3 executables directly (the VSTest socket path is unavailable in the sandbox). Counts regenerated from the actual run output (2026-06-24):

- `Hexalith.Agents.Contracts.Tests` — **246 total, 0 failed** (adds `AgentInteractionProposalEditContractsTests`).
- `Hexalith.Agents.Tests` (domain) — **576 total, 0 failed** (adds `AgentInteractionProposalEditAggregateTests` + `AgentInteractionProposalEditLifecycleE2ETests`).
- `Hexalith.Agents.Server.Tests` — **275 total, 0 failed** (adds `AgentInteractionProposalEditOrchestratorTests` + `AgentProposalEditIdentityTests`; `BuildContractConformanceTests`/`ProjectReferenceDirectionTests`/`StructuralSeedConformanceTests` still green over the new types).
- `Hexalith.Agents.UI.Tests` — **462 total, 0 failed** (adds `ProposalEditorTests` + `AgentGenerationKindPresentationTests`; extends `BadgeConformanceTests`, `ProposedAgentReplyStatePresentationTests`, `LocalizationResourceTests` (en+fr parity for the new keys), `DeferredGatewayTests`, `AccessibilityTests`).

Aggregate test total = **1559, 0 failed** (re-verified during the Story 3.3 senior review; the prior 1552 was a stale count).

### Completion Notes List

Implemented the full Story 3.3 vertical slice mirroring the Story 3.1 create slice (Contracts → Domain aggregate + twin-policy → Server orchestrator → UI → tests). All ACs satisfied; the proposal lives on the existing `AgentInteraction` aggregate (AD-2) — the edit is the **7th** `Handle`.

Key decisions resolved during dev:
- **Edited-version model (Task 1):** added two nullable fields to `AgentGeneratedVersion` (`SourceVersionId`, `EditorPartyId`), both `null` for `Generated`, set for `Edited`. Backward-compatible (absent ⇒ null; legacy-JSON test proves it). The edit timestamp is the EventStore event metadata (AD-3 — no wall-clock field).
- **New interaction statuses:** appended `AgentInteractionStatus.ProposalEdited` (14) / `ProposalEditFailed` (15) so the twin-policy `Decide`/`Apply` stay parallel to create; the proposal sub-state moves to `ProposedAgentReplyState.Edited` (AC1).
- **Authorization (AC1, first edit-time approver use):** the orchestrator re-resolves the snapshotted Approver Policy via `IApproverPolicyResolver` + `ApproverPolicyVerdict.Evaluate` and **fails closed with NO dispatch** on any non-`Valid` verdict (NotAuthorized/Unavailable/Ambiguous/Disabled/Missing/Incomplete/Unknown). Freshness is folded into the resolver's `Unavailable` outcome, which the verdict already fails closed on. No new read port was needed — the source version + edited content arrive on the request (mirrors `AgentActivationProviderRevalidation` carrying the recorded policy). The aggregate also defensively records `ProposalEditFailed`/`NotAuthorized` if a non-`Valid` verdict ever reaches it.
- **Content confinement (AD-14, the key difference from 3.1):** the user's edited content travels the write path on the edit command's `AgentGeneratedVersion` (its legitimate, payload-protected home, like `AgentOutputGenerated`). The no-leak assertions target the **evidence / failed event / rejection / view / orchestrator outcome** — NOT `ProposedAgentReplyEdited` / the edited version, which legitimately carry content.
- **Idempotency (AD-13):** the edited version id is derived deterministically from `(interaction, sourceVersion, editAttemptId)` with a distinct SHA-256 purpose tag (`proposal-edit-version-id`). The aggregate's terminal no-op is keyed on the edited version id already being in the history (not the status), so a retried edit dedupes while a second *distinct* edit on an `Edited` proposal still appends another version.
- **UI (Task 4):** delivered the `proposal-editor` component (editable for authorized, read-only for non-authorized; distinct generated-vs-edited version labels via `AgentGenerationKindPresentation`; save wired to the fail-closed `IProposalEditGateway`; no content in `aria-label`/`data-testid`), the `Edited` proposal-state badge case (`BadgeColor.Informative` + curated `Edit16` glyph), and en+fr localization. Used `FluentTextArea` (the confirmed pinned-RC multi-line editor). The full detail workspace + version-history panel + complete Esc-without-commit accessibility remain owned by Story 3.7 (not duplicated).
- **Read side:** shipped the edit audit-evidence **view contract** (`GetAgentProposalEditEvidenceQuery` + view + result); the live projection/query-handler binding is deferred to Epic 4 (precedent: Story 3.2).

### Change Log

| Date | Change |
| --- | --- |
| 2026-06-24 | Implemented Story 3.3 (Edit Proposed Reply Versions) — additive edit contracts, 7th aggregate handler + twin-policy + state Apply, fail-closed edit orchestrator + deterministic edited-version identity + DI, proposal-editor UI + edit gateway + version labeling + en/fr localization, and tests across all four layers (1552 total, 0 failed). Status → review. |
| 2026-06-24 | Senior Developer Review (AI, auto-fix): re-verified Release build (0 warnings / 0 errors) and all four test projects (Contracts 246, Domain 576, Server 275, UI 462 = **1559**, 0 failed). Corrected the stale Debug Log test counts (242→246 Contracts, 459→462 UI, 1552→1559 total). Strengthened the second-distinct-edit aggregate test to assert all prior versions are preserved by id (AC1). Simplified a redundant `MapFailureReason` switch in `AgentProposalEditPolicy`. No CRITICAL/HIGH findings; File List matches `git status`. Status → done. |

### File List

**Production — new (Contracts):**
- `src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposalEditOutcome.cs`
- `src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposalEditFailureReason.cs`
- `src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposedReplyNotEditableReason.cs`
- `src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposalEditResult.cs`
- `src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposedReplyEditEvidence.cs`
- `src/Hexalith.Agents.Contracts/AgentInteraction/Commands/EditProposedAgentReply.cs`
- `src/Hexalith.Agents.Contracts/AgentInteraction/Events/ProposedAgentReplyEdited.cs`
- `src/Hexalith.Agents.Contracts/AgentInteraction/Events/ProposedAgentReplyEditFailed.cs`
- `src/Hexalith.Agents.Contracts/AgentInteraction/Events/Rejections/ProposedAgentReplyNotEditableRejection.cs`
- `src/Hexalith.Agents.Contracts/AgentInteraction/Queries/GetAgentProposalEditEvidenceQuery.cs`
- `src/Hexalith.Agents.Contracts/AgentInteraction/Queries/AgentProposalEditEvidenceView.cs`
- `src/Hexalith.Agents.Contracts/AgentInteraction/Queries/AgentProposalEditEvidenceResult.cs`

**Production — modified (Contracts):**
- `src/Hexalith.Agents.Contracts/AgentInteraction/AgentGenerationKind.cs` (append `Edited`)
- `src/Hexalith.Agents.Contracts/AgentInteraction/ProposedAgentReplyState.cs` (append `Edited`)
- `src/Hexalith.Agents.Contracts/AgentInteraction/AgentGeneratedVersion.cs` (additive `SourceVersionId`/`EditorPartyId`)
- `src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionStatus.cs` (append `ProposalEdited`/`ProposalEditFailed`)

**Production — new/modified (Domain):**
- `src/Hexalith.Agents/AgentInteraction/AgentProposalEditPolicy.cs` (new)
- `src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs` (7th `Handle` + idempotency helper)
- `src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs` (edit fields + 3 `Apply` methods)

**Production — new/modified (Server):**
- `src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalEditOrchestrator.cs` (new)
- `src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalEditRequest.cs` (new — request + outcome)
- `src/Hexalith.Agents.Server/Application/AgentInteractions/AgentProposalEditIdentity.cs` (new)
- `src/Hexalith.Agents.Server/Program.cs` (Story 3.3 DI block)

**Production — new/modified (UI):**
- `src/Hexalith.Agents.UI/Services/Gateways/IProposalEditGateway.cs` (new)
- `src/Hexalith.Agents.UI/Services/Gateways/DeferredProposalEditGateway.cs` (new)
- `src/Hexalith.Agents.UI/Services/Gateways/ProposalEditRequest.cs` (new)
- `src/Hexalith.Agents.UI/Services/Gateways/ProposalEditResult.cs` (new — result + status enum)
- `src/Hexalith.Agents.UI/Components/Shared/AgentGenerationKindPresentation.cs` (new)
- `src/Hexalith.Agents.UI/Components/Shared/ProposalEditor.razor` (new)
- `src/Hexalith.Agents.UI/Components/Shared/ProposedAgentReplyStatePresentation.cs` (`Edited` color/icon)
- `src/Hexalith.Agents.UI/Services/Gateways/AgentsUiServiceCollectionExtensions.cs` (register edit gateway)
- `src/Hexalith.Agents.UI/Resources/AgentsResources.resx` (en keys)
- `src/Hexalith.Agents.UI/Resources/AgentsResources.fr.resx` (fr keys)

**Tests — new:**
- `tests/Hexalith.Agents.Contracts.Tests/AgentInteractionProposalEditContractsTests.cs`
- `tests/Hexalith.Agents.Tests/AgentInteractionProposalEditAggregateTests.cs`
- `tests/Hexalith.Agents.Tests/AgentInteractionProposalEditLifecycleE2ETests.cs`
- `tests/Hexalith.Agents.Server.Tests/AgentInteractionProposalEditOrchestratorTests.cs`
- `tests/Hexalith.Agents.Server.Tests/AgentProposalEditIdentityTests.cs`
- `tests/Hexalith.Agents.UI.Tests/ProposalEditorTests.cs`
- `tests/Hexalith.Agents.UI.Tests/AgentGenerationKindPresentationTests.cs`

**Tests — modified:**
- `tests/Hexalith.Agents.Tests/AgentInteractionTestData.cs` (Story 3.3 edit fixtures + `ApplyAll` cases)
- `tests/Hexalith.Agents.UI.Tests/AgentsTestContext.cs` (edit gateway substitute)
- `tests/Hexalith.Agents.UI.Tests/AgentUiTestData.cs` (edit factory)
- `tests/Hexalith.Agents.UI.Tests/BadgeConformanceTests.cs` (`Edited` badge case)
- `tests/Hexalith.Agents.UI.Tests/ProposedAgentReplyStatePresentationTests.cs` (`Edited` color/totality)
- `tests/Hexalith.Agents.UI.Tests/LocalizationResourceTests.cs` (new key coverage en+fr)
- `tests/Hexalith.Agents.UI.Tests/DeferredGatewayTests.cs` (`DeferredProposalEditGateway`)
- `tests/Hexalith.Agents.UI.Tests/AccessibilityTests.cs` (editor surface)

_Note: the story file (`_bmad-output/implementation-artifacts/3-3-edit-proposed-reply-versions.md`) and `sprint-status.yaml` live in the parent repo; the code above lives in the `Hexalith.Agents` directory (paths relative to it)._

## Senior Developer Review (AI)

**Reviewer:** Administrator (adversarial auto-fix review) · **Date:** 2026-06-24 · **Outcome:** Approve → done

### Verification performed
- **Release build** `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` → **0 warnings / 0 errors** (warnings-as-errors), re-run after fixes.
- **All four test projects** (built xUnit v3 executables, run individually): Contracts **246**, Domain **576**, Server **275**, UI **462** = **1559 total, 0 failed**.
- **File List vs `git status`**: every changed/added source file is documented; no undocumented source file and no listed-but-unchanged file. (`Hexalith.Agents` is a regular directory in this repo, not a submodule.)
- **AC trace**: AC1 (authorized edit appends an immutable version, prior versions preserved) ✔ aggregate + state Apply + tests; AC2 (terminal/non-pending → `ProposedAgentReplyNotEditableRejection`, no new version) ✔; AC3 (distinct generated-vs-edited labels, never styled as a posted Conversation Message, no content in accessible names) ✔ `ProposalEditor` + presentation + bUnit tests; AC4 (auditable, prior content not overwritten, content-free evidence/view/outcome/rejection) ✔ `AgentProposedReplyEditEvidence`/view + no-leak tests.
- **Cross-cutting**: AD-3 pure aggregate (no I/O), AD-12 fail-closed edit-time approver authorization with no-dispatch on any non-`Valid` verdict, AD-13 deterministic edited-version id (distinct `proposal-edit-version-id` SHA-256 purpose tag) + terminal no-op dedupe, AD-14 content confinement (edited content rides only the edit command/event/version) — all correctly implemented and tested.

### Findings and disposition
| Sev | Finding | Action |
| --- | --- | --- |
| MEDIUM | Dev Agent Record drift — stale test counts (Contracts 242→**246**, UI 459→**462**, total 1552→**1559**). The recurring #1 review finding. | **Fixed** — Debug Log References + Change Log corrected to actual run output. |
| LOW | Second-distinct-edit aggregate test asserted appended count but not that all prior versions coexist by id (AC1 "prior versions preserved"). | **Fixed** — added an explicit `[generated, edit-1, edit-2]` version-id assertion. |
| LOW | `AgentProposalEditPolicy.MapFailureReason` had a redundant switch (both arms returned `AdapterFailure`). | **Fixed** — simplified to a direct fail-closed return (behavior-preserving; test-covered). |
| LOW (noted) | `ProposalEditor` injects `TimeProvider` but renders no timestamp (the version-history/timestamp surface is Story 3.7). | Left as forward-compat scaffolding (Task 4 explicitly required the injection; 3.7 hosts the editor). |
| LOW (noted) | `Agents.ProposalEditor.Author.Label` resource key (en+fr) is defined but not yet referenced by the component. | Left for the Story 3.7 author label; harmless and parity-clean. |
| LOW (noted) | Latent: were `ProposedAgentReplyEditFailed` ever recorded, `Status = ProposalEditFailed` would fall outside the aggregate's editable `proposalExists` set. Unreachable in this slice — the orchestrator fails closed with **no dispatch** and only ever dispatches an authorized `Edited` outcome, so the failure event is pure defense-in-depth. | Left as-is; revisit if a future story makes the edit-failed event reachable. |

No CRITICAL or HIGH findings. The slice mirrors the Story 3.1 create slice faithfully (orchestrator → single command → twin `Evaluate`/`Decide` policy) and the implementation quality is high.

_Reviewer: Administrator on 2026-06-24_
