---
baseline_commit: b8821a09a5ff98aeaf7173627dde9ba485504e8a
---

# Story 2.5: Post Automatic Responses Through Conversations

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a Conversation Participant,
I want successful automatic responses posted as `hexa`,
so that the Conversation contains an attributed AI response only after all gates pass.

## Acceptance Criteria

**AC1 - Posting verifies the Agent is an AI-Agent participant through a Conversations-owned membership boundary, or fails closed**

**Given** an AgentInteraction is in Automatic Response Mode and generation plus safety checks pass (the interaction is at `AgentInteractionStatus.Generated`)
**When** posting begins
**Then** the system verifies the Agent `PartyId` is valid and present in the Source Conversation as an AI Agent participant (`ParticipantType.AiAgent`) through a Conversations-owned membership command/API/client boundary
**And** posting fails closed (`PostingFailed` with a safe membership/party reason) if Party identity, membership, or the Conversations membership seam is missing, disabled, ambiguous, unauthorized, or unavailable, and no Conversation Message is created.

**AC2 - The posted message is authored by the Agent Party identity and references the interaction**

**Given** membership and posting prerequisites pass
**When** the system appends the response to the Source Conversation
**Then** the Conversation Message is authored by the Agent Party identity (`AppendMessageCommand.AuthorPartyId = AgentPartyId`), not the caller or a system account
**And** the message references the AgentInteraction or an equivalent trace identifier (the deterministic `MessageId` derived from `AgentInteractionId` + `VersionId`, plus the interaction `CorrelationId` on the command metadata).

**AC3 - Posting is idempotent on retry — no duplicate Conversation Message**

**Given** posting is retried
**When** the same AgentInteraction and generated content version are used
**Then** the Conversations append uses a deterministic `MessageId` and idempotency key derived from interaction/version context (`AgentInteractionId` + selected `VersionId`)
**And** no duplicate Conversation Message is created (Conversations dedupes on the caller-supplied `MessageId`/`IdempotencyKey`, and the aggregate's terminal `Posted`/`PostingFailed` re-dispatch is a deterministic no-op preserving the recorded outcome).

**AC4 - Posting failure is a distinct, safe, auditable status that never leaks content**

**Given** posting fails after generation succeeds
**When** status is inspected
**Then** the system distinguishes posting failure (`PostingFailed`) from generation failure (`GenerationFailed`), authorization failure (`Denied`), context failure (`ContextBlocked`), and safety failure (`SafetyFailed`)
**And** generated content is not exposed through unauthorized status or logs, and the posting failure records only a safe reason classification plus safe ids (no generated content, raw provider/Conversations payload, stack trace, or secret).

[Source: _bmad-output/planning-artifacts/epics.md#Story-2.5; prd FR11, FR12, FR2, FR19, FR20, FR21, NFR1, NFR3; ARCHITECTURE-SPINE.md#AD-3, #AD-6, #AD-7, #AD-12, #AD-13, #AD-14, #AD-17, #AD-18; _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-23.md (Story 2.0a membership-seam prerequisite)]

## Tasks / Subtasks

- [x] **Task 1 - Append posting states to `AgentInteractionStatus` (additive, AD-2)** (AC: #1, #4)
  - [x] Add two values AFTER `SafetyFailed` (ordinal 9), never reshaping ordinals 0–9, in `src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionStatus.cs`: `Posted` (10), `PostingFailed` (11). Mirror the one-line XML-doc-per-member style and the `Unknown=0` sentinel rationale.
  - [x] `Posted` doc: the generated version was appended to the Source Conversation as a Conversation Message authored by the Agent Party identity (Story 2.5 automatic mode); this is the terminal success state for an automatic interaction. `PostingFailed` doc: posting failed closed AFTER successful generation — membership/Party/Conversation/append failure — recorded as fail-closed Audit Evidence; no Conversation Message exists; distinct from generation/auth/context/safety failure (AC4).
  - [x] Update the enum's top summary comment that lists states appended "by Stories 2.2–2.5" so it stays accurate (it already anticipates "posted").
  - [x] Do NOT add a transient `PostingPending` aggregate state — see Dev Notes "Why No Persisted `PostingPending` State". (Leave additive room for Epic 3 proposal-posting states; do not pre-create them.)

- [x] **Task 2 - Add posting value objects + safe enums to Contracts** (AC: #1, #2, #3, #4)
  - [x] `AgentResponsePostingFailureReason` enum (`src/Hexalith.Agents.Contracts/AgentInteraction/`): `[JsonConverter(typeof(JsonStringEnumConverter))]`, `Unknown=0`, then `PartyIdentityUnavailable, MembershipUnavailable, MembershipRejected, ConversationUnavailable, PostRejected, AdapterFailure`. One safe XML-doc line each; carries NO raw content/payload. Mirror `AgentOutputGenerationFailureReason.cs`.
  - [x] `AgentResponsePostingOutcome` enum (the orchestrator's outcome discriminator): `Unknown=0, Posted, PartyIdentityUnavailable, MembershipUnavailable, MembershipRejected, ConversationUnavailable, PostRejected, AdapterFailure`. Mirror `AgentGenerationOutcome`. (`MembershipUnavailable` is the fail-closed value the deferred/seam-absent membership path returns — see Dev Notes.)
  - [x] `AgentPostedMessageEvidence` record (`src/Hexalith.Agents.Contracts/AgentInteraction/`): `string MessageId, string SourceConversationId, string AgentPartyId, string PostedVersionId` — safe ids only (Conversations-owned `MessageId`/`ConversationId`, the Agent's stable `PartyId` reference per AD-7, and the selected `VersionId`). **No generated content, no provider/Conversations payload, no error text.** Mirror `AgentGenerationAttemptEvidence.cs` (safe refs only). XML-doc that `AgentPartyId` is a stable reference, not PII (AD-7), and that no content is carried (AD-14).
  - [x] `AgentResponsePostingResult` record (server-assembled command input, mirroring `AgentOutputGenerationResult.cs`): `AgentResponsePostingOutcome Outcome, string MessageId, string SourceConversationId, string AgentPartyId, string PostedVersionId`. The pure policy maps `Outcome` → event + status; the orchestrator is the only producer. **Crucially, it carries NO generated content** — the content was already durably recorded on `AgentOutputGenerated`/state by Story 2.4; posting transports only safe ids into the aggregate (AD-14).
  - [x] `AgentResponseNotPostableReason` enum: `Unknown=0, InteractionNotRequested, OutputNotGenerated, NotAutomaticResponseMode`. Mirror `AgentOutputNotGeneratableReason.cs`. (`OutputNotGenerated` = status is not `Generated`; `NotAutomaticResponseMode` = the snapshot response mode is Confirmation, which posts via Epic 3 approval, not automatic posting.)

- [x] **Task 3 - Add command, events, and rejection** (AC: #1, #2, #3, #4)
  - [x] Command `PostAgentResponse(string AgentInteractionId, AgentResponsePostingResult Result)` in `…/AgentInteraction/Commands/`. Plain `public record` (NO `IEventPayload`, NO attribute). Redundant `AgentInteractionId` mirrors envelope `AggregateId`; `Result` is server-assembled. Mirror `GenerateAgentOutput.cs`.
  - [x] Success event `AgentResponsePosted(string AgentInteractionId, AgentPostedMessageEvidence Evidence)` : `IEventPayload` in `…/Events/`. Status → `Posted`. Mirror `AgentOutputGenerated.cs` (no wall-clock field — post time is EventStore event metadata, AD-3).
  - [x] Recorded-negative event `AgentResponsePostingFailed(string AgentInteractionId, AgentResponsePostingFailureReason Reason, AgentPostedMessageEvidence Evidence)` : `IEventPayload` in `…/Events/`. Status → `PostingFailed`. This is a SUCCESS event (durable fail-closed Audit Evidence), NOT a rejection — mirror `AgentOutputGenerationFailed.cs`. Evidence carries the safe ids that were attempted (the deterministic `MessageId`, `SourceConversationId`, `AgentPartyId`, `PostedVersionId`); never content, payloads, or error text (AD-14).
  - [x] Rejection `AgentResponseNotPostableRejection(string AgentInteractionId, AgentResponseNotPostableReason Reason)` : `IRejectionEvent` in `…/Events/Rejections/` — structural inability to evaluate posting (not requested, not generated, or not automatic mode); no state change. Mirror `AgentOutputNotGeneratableRejection.cs`.

- [x] **Task 4 - Pure posting policy** (AC: #1, #2, #4)
  - [x] `AgentResponsePostingPolicy` (`internal static`) in `src/Hexalith.Agents/AgentInteraction/`, mirroring `AgentOutputGenerationPolicy.cs`: `Evaluate(string interactionId, AgentResponsePostingResult result) → DomainResult`, `Decide(AgentResponsePostingResult result) → AgentInteractionStatus`, and a private `Compute` returning a `readonly record struct` decision.
  - [x] Mapping: `Posted` → `AgentResponsePosted(evidence)` + status `Posted`; every other outcome → `AgentResponsePostingFailed(MapFailureReason(outcome), evidence)` + status `PostingFailed`. Build `AgentPostedMessageEvidence` from the result's safe ids in both branches.
  - [x] `MapFailureReason`: `PartyIdentityUnavailable → PartyIdentityUnavailable`, `MembershipUnavailable → MembershipUnavailable`, `MembershipRejected → MembershipRejected`, `ConversationUnavailable → ConversationUnavailable`, `PostRejected → PostRejected`, default/`AdapterFailure → AdapterFailure`.
  - [x] Confirm `Decide` and `Evaluate` agree on status for every outcome (no-drift), exactly as the generation/context/gate policies do.

- [x] **Task 5 - Aggregate handler + state** (AC: #1, #2, #3, #4)
  - [x] Add `public static DomainResult Handle(PostAgentResponse command, AgentInteractionState? state, CommandEnvelope envelope)` to `src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs` as the 5th handler. Mirror `Handle(GenerateAgentOutput)` exactly:
    - `ArgumentNullException.ThrowIfNull(command/envelope)`; `string interactionId = envelope.AggregateId;`
    - positive bind `if (state is { IsRequested: true } requested)`
    - **terminal idempotent no-op**: `if (requested.Status is AgentInteractionStatus.Posted or AgentInteractionStatus.PostingFailed) return DomainResult.NoOp();`
    - **precondition (response mode)**: `if (requested.Snapshot?.ResponseMode != AgentResponseMode.Automatic) return DomainResult.Rejection([new AgentResponseNotPostableRejection(interactionId, AgentResponseNotPostableReason.NotAutomaticResponseMode)]);` (confirmation-mode posting is Epic 3 / Story 3.5, not this path)
    - **precondition (status)**: `if (requested.Status != AgentInteractionStatus.Generated) return DomainResult.Rejection([new AgentResponseNotPostableRejection(interactionId, AgentResponseNotPostableReason.OutputNotGenerated)]);`
    - delegate to `AgentResponsePostingPolicy.Evaluate(interactionId, command.Result)`
    - fall-through `return DomainResult.Rejection([new AgentResponseNotPostableRejection(interactionId, AgentResponseNotPostableReason.InteractionNotRequested)]);`
  - [x] In `src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs` add additive nullable fields mirroring `ContextEvidence`/`GenerationFailureReason`: `AgentPostedMessageEvidence? PostingEvidence` and `AgentResponsePostingFailureReason? PostingFailureReason`.
  - [x] Add `Apply(AgentResponsePosted e)`: guard `if (!IsRequested) return;`, `Status = AgentInteractionStatus.Posted;`, `PostingEvidence = e.Evidence;`. Add `Apply(AgentResponsePostingFailed e)`: guard, `Status = AgentInteractionStatus.PostingFailed;`, `PostingFailureReason = e.Reason;`, `PostingEvidence = e.Evidence;`. Add no-op `Apply(AgentResponseNotPostableRejection)` via `MarkReplayOnlyEventHandled()`.
  - [x] Use the CA1062-safe **positive** null-guard pattern (`state is { IsRequested: true } requested`); do NOT use the negative early-return form (warnings-as-errors will fail — Story 2.2 learning).

- [x] **Task 6 - In-module readers: Agent Party + selected generated version (Server, deferred/fail-closed)** (AC: #1, #2)
  - [x] `IAgentPartyReader` in `src/Hexalith.Agents.Server/Ports/`: `Task<AgentPartyReadResult> ReadAsync(string tenantId, string agentId, CancellationToken ct)` returning the Agent's linked `PartyId` + a valid/enabled/available state (the snapshot does NOT carry the Agent's `PartyId` — only `ProviderId`/`ModelId`/versions/`ResponseMode`; AD-7 requires a posting-time Party-validity gate, so read it live, do not trust a stale snapshot). `AgentPartyReadResult` carries `AgentPartyReadOutcome { Unknown=0, Available, NotLinked, Disabled, Unavailable }` + `string? PartyId`. `DeferredAgentPartyReader` returns not-available (fail closed). Mirror the deferred-reader shape of `DeferredConversationContextReader`/`DeferredAgentContentSafetyPolicyReader`.
  - [x] `IAgentGeneratedVersionReader` in `…/Ports/`: `Task<AgentGeneratedVersionReadResult> ReadSelectedVersionAsync(string tenantId, string agentInteractionId, CancellationToken ct)` returning the version to post — its `VersionId` and `GeneratedContent` (the latest/only generated version in V1 automatic mode) + an outcome (`Available`/`NotAvailable`). **`GeneratedContent` is sensitive (AD-14)** — it is read ONLY here, handed to the message poster, and NEVER placed on the posting command/result/event/state-posting-fields/logs. `DeferredAgentGeneratedVersionReader` returns not-available (fail closed) so the default graph cannot read content and therefore cannot post (AD-14: content-bearing workflows stay disabled until protection is wired). XML-doc this content-safety rationale.

- [x] **Task 7 - Conversation posting + membership port (Server adapter over `Hexalith.Conversations.Client`, deferred/fail-closed)** (AC: #1, #2, #3)
  - [x] `IConversationResponsePoster` in `…/Ports/`: two operations wrapping the Conversations public client —
    - `Task<ConversationMembershipResult> EnsureAiAgentParticipantAsync(ConversationMembershipRequest request, CancellationToken ct)` — verifies (and, when the seam exists, establishes) the Agent `PartyId` as `ParticipantType.AiAgent` in the Source Conversation. Result `ConversationMembershipOutcome { Unknown=0, Present, Established, MembershipRejected, ConversationUnavailable, SeamUnavailable }`.
    - `Task<ConversationAppendResult> AppendAgentMessageAsync(ConversationAppendRequest request, CancellationToken ct)` — appends the message authored by the Agent `PartyId` with the deterministic `MessageId`/idempotency key. Result `ConversationAppendOutcome { Unknown=0, Posted, PostRejected, ConversationUnavailable, AdapterFailure }`.
    - Requests carry safe inputs only on the membership side (`tenantId, sourceConversationId, agentPartyId, actorPrincipalId`); the append request additionally carries the generated `text` (sensitive — stays inside the adapter), `messageId`, `idempotencyKey`, `authorPartyId`, `correlationId`.
  - [x] Live `ConversationClientResponsePoster` (config-gated behind the existing `Conversations` section, reusing `AddHexalithConversationsClient`, exactly like `ConversationClientContextReader`):
    - **Membership verify** via `IConversationClient.GetConversationAsync(GetConversationQuery)` → inspect `ConversationDetailsV1.Participants` for an entry whose `ParticipantPartyId == agentPartyId` and `ParticipantType == ParticipantType.AiAgent` → return `Present`; on missing/unauthorized/stale conversation → `ConversationUnavailable`.
    - **Membership establish**: `IConversationClient` does **NOT** expose `AddParticipantAsync` and no API route maps `AddParticipantCommand` (verified — see Dev Notes "Conversations Membership Seam Reality"). Therefore, when the Agent is NOT already a participant, return `SeamUnavailable` (fail closed). Do **not** add the seam to the sibling Conversations module from this story (Conversations project-context: keep changes scoped to Agents). Leave a clearly-marked extension point + comment: when Conversations exposes a public `AddParticipantAsync(AddParticipantCommand,…)` (AD-6 / proposed Story 2.0a), wire the establish path here to return `Established`.
    - **Append** via `IConversationClient.AppendMessageAsync(new AppendMessageCommand(metadata, ConversationId, MessageId, AuthorPartyId: agentPartyId, Text: generatedContent, …))` with `ConversationCommandMetadata{ TenantId, ActorPartyId: agentPartyId, CorrelationId, IdempotencyKey }`. Map a successful `ConversationClientResult<ConversationCommandAcceptedResult>` → `Posted`; a rejection result → `PostRejected`; transport/unknown failure → `AdapterFailure`/`ConversationUnavailable`. Wrap in the per-read fail-closed `try { … } catch (Exception ex) when (ex is not OperationCanceledException) { return …Unavailable/AdapterFailure }`; never propagate Conversations error text/payload (AD-14).
  - [x] `DeferredConversationResponsePoster` fails closed: membership → `SeamUnavailable`, append → `ConversationUnavailable`. This is the default binding when no `Conversations` config exists (mirrors `DeferredConversationContextReader`).

- [x] **Task 8 - Posting orchestrator (durable-owner step, impure)** (AC: #1, #2, #3, #4)
  - [x] `AgentInteractionPostingOrchestrator` (`public sealed class`) in `src/Hexalith.Agents.Server/Application/AgentInteractions/`. Deps (ctor `ArgumentNullException.ThrowIfNull` each): `IAgentPartyReader`, `IAgentGeneratedVersionReader`, `IConversationResponsePoster`, `IAgentCommandDispatcher`. Mirror `AgentInteractionGenerationOrchestrator.cs`.
  - [x] `ExecuteAsync(AgentInteractionPostingRequest request, CancellationToken ct)`:
    1. **Response-mode short-circuit**: if `request.ResponseMode != AgentResponseMode.Automatic`, do NOT post — return the decided status without dispatch (the aggregate would reject it anyway). (Defensive; the durable owner only invokes this for automatic mode.)
    2. **Read the Agent Party** via `IAgentPartyReader`. Not-available/disabled/not-linked → `Outcome.PartyIdentityUnavailable`. (Wrap in per-read fail-closed try/catch.)
    3. **Read the selected generated version** (content + `VersionId`) via `IAgentGeneratedVersionReader`. Not-available → fail closed → `Outcome.AdapterFailure` (cannot post without the durable version; the aggregate precondition already guarantees `Generated`, so this only fails if the content read is unavailable — e.g., deferred reader / protection unavailable).
    4. **Ensure membership** via `IConversationResponsePoster.EnsureAiAgentParticipantAsync`. `Present`/`Established` → proceed; `SeamUnavailable` → `Outcome.MembershipUnavailable`; `MembershipRejected` → `Outcome.MembershipRejected`; `ConversationUnavailable` → `Outcome.ConversationUnavailable`. (Wrap fail-closed.)
    5. **Derive deterministic ids** (AD-13): `messageId = AgentResponsePostingIdentity.DeriveMessageId(request.AgentInteractionId, versionId)` and `idempotencyKey = AgentResponsePostingIdentity.DeriveIdempotencyKey(request.AgentInteractionId, versionId)` — pure SHA-256-hex derivation mirroring `AgentInteractionIdentity.Derive` (regex-valid, colon-free, deterministic so a retry reuses the same id).
    6. **Append** via `IConversationResponsePoster.AppendAgentMessageAsync` with `authorPartyId = agentPartyId`, `text = generatedContent`, the derived `messageId`/`idempotencyKey`, and `request.CorrelationId`. `Posted` → `Outcome.Posted`; `PostRejected` → `Outcome.PostRejected`; else `Outcome.ConversationUnavailable`/`Outcome.AdapterFailure`. (Wrap fail-closed; no raw error leak.)
    7. **Assemble a server-trusted `AgentResponsePostingResult`** with the outcome + safe ids (`messageId`, `sourceConversationId`, `agentPartyId`, `versionId`) — **never the content**. Strip client-supplied values via the standard `_reservedExtensionKeys` + `BuildTrustedExtensions`. Build `CommandEnvelope(request.MessageId, request.TenantId, InteractionDomain, request.AgentInteractionId, nameof(PostAgentResponse), JsonSerializer.SerializeToUtf8Bytes(command), request.CorrelationId, CausationId: null, request.ActorUserId, BuildTrustedExtensions(request.ClientSuppliedExtensions))`. `await _dispatcher.DispatchAsync(envelope, ct)`.
    8. Return `new AgentInteractionPostingOutcomeResult(request.AgentInteractionId, AgentResponsePostingPolicy.Decide(result))` — status from the SHARED policy so reported status can't drift. (`AgentResponsePostingPolicy` is `internal`, visible to `…Server` via `InternalsVisibleTo`, NOT to `…Server.Tests` — assert via the orchestrator's returned status, Story 2.3 learning.)
  - [x] `private const string InteractionDomain = "agent-interaction";` + copy the `_reservedExtensionKeys` array and `BuildTrustedExtensions` helper verbatim from `AgentInteractionGenerationOrchestrator`.
  - [x] `AgentResponsePostingIdentity` (`internal static`) in the same `Application/AgentInteractions/` folder: `DeriveMessageId(string agentInteractionId, string versionId)` and `DeriveIdempotencyKey(string agentInteractionId, string versionId)`, each a length-prefixed SHA-256 lowercase-hex digest mirroring `AgentInteractionIdentity`. The `MessageId` must satisfy the Conversations `MessageId` validation (non-empty; lowercase hex is safe).
  - [x] DTOs in the same folder: `AgentInteractionPostingRequest` (`sealed record`: `string MessageId, string CorrelationId, string TenantId, string AgentInteractionId, string AgentId, string SourceConversationId, AgentResponseMode ResponseMode, string ActorUserId, string? ClientCorrelationId = null, IReadOnlyDictionary<string,string>? ClientSuppliedExtensions = null`) and `AgentInteractionPostingOutcomeResult(string AgentInteractionId, AgentInteractionStatus Status)` — safe id + status only (no content, stream names, payloads).

- [x] **Task 9 - DI registration (Program.cs)** (AC: #1, #3)
  - [x] In `src/Hexalith.Agents.Server/Program.cs` add a `// Story 2.5` block: `AddSingleton<IAgentPartyReader, DeferredAgentPartyReader>()`, `AddSingleton<IAgentGeneratedVersionReader, DeferredAgentGeneratedVersionReader>()`, and register `IConversationResponsePoster` config-gated **inside the existing `Conversations` section branch** (`ConversationClientResponsePoster` when the section exists, else `DeferredConversationResponsePoster`), then `AddScoped<AgentInteractionPostingOrchestrator>()`. Mirror the Story 2.4 deferred-port block and the existing config-gated Conversations `if/else`. The aggregate handler auto-registers (no host change — `AddEventStoreDomainService` scan).
  - [x] Keep the default DI graph FAIL-CLOSED: with no `Conversations` config and the deferred in-module readers, the orchestrator cannot read content, cannot prove membership, and cannot post — every path resolves to `PostingFailed`. A future live `IAgentPartyReader`/`IAgentGeneratedVersionReader` binding (over the in-module Agent/AgentInteraction read models) and the live Conversations poster are wired only when their dependencies/config are present.

- [x] **Task 10 - Tests (xUnit v3 + Shouldly; NSubstitute only in Server.Tests)** (AC: #1, #2, #3, #4)
  - [x] Extend `tests/Hexalith.Agents.Tests/AgentInteractionTestData.cs`: add a `StateGenerated()` fixture (status `Generated`, snapshot `ResponseMode = Automatic`, one `AgentGeneratedVersion` in `GeneratedVersions`), a `StateGeneratedConfirmationMode()` fixture (snapshot `ResponseMode = Confirmation`), an `AgentResponsePostingResult` builder (`PostedResult`, `MembershipUnavailableResult`, `PartyIdentityUnavailableResult`, `PostRejectedResult`, …), and new `Apply(AgentResponsePosted)`/`Apply(AgentResponsePostingFailed)` cases in the `ApplyAll` switch.
  - [x] `tests/Hexalith.Agents.Tests/AgentInteractionPostingAggregateTests.cs`: Generated + Automatic + `Posted` outcome → `AgentResponsePosted` + status `Posted`; Generated + Automatic + each failure outcome → `AgentResponsePostingFailed(Reason=…)` + status `PostingFailed`; Generated + **Confirmation** mode → `AgentResponseNotPostableRejection(NotAutomaticResponseMode)` (no state change); wrong status (Authorized/ContextReady/GenerationFailed/SafetyFailed) → `AgentResponseNotPostableRejection(OutputNotGenerated)`; not-requested → `AgentResponseNotPostableRejection(InteractionNotRequested)`; **idempotent re-issue after `Posted` and after `PostingFailed` → `DomainResult.NoOp()`** (AC3); `Evaluate`/`Decide` no-drift theory.
  - [x] Extend `tests/Hexalith.Agents.Tests/AgentInteractionStateReplayTests.cs` with the new posting events.
  - [x] `tests/Hexalith.Agents.Contracts.Tests/AgentInteractionPostingContractsTests.cs`: JSON round-trip for all new records/enums; assert `AgentInteractionStatus` ordinals 0–9 are unperturbed and 10–11 are the new values; **AD-14 no-leak**: serialize `AgentResponsePosted`/`AgentResponsePostingFailed`/`AgentResponsePostingResult`/views/rejections and `.ShouldNotContain` any generated-content sample string; the `ContractsSecretNonDisclosureTests` auto-coverage must include the new types. When asserting "no Hexalith attribute," scope to `Hexalith`-namespaced attributes (compiler emits `Nullable*Attribute` on records with nullable members — Story 2.1 learning).
  - [x] `tests/Hexalith.Agents.Server.Tests/AgentInteractionPostingOrchestratorTests.cs` (NSubstitute): happy path (party available + version available + membership `Present` + append `Posted`) dispatches `PostAgentResponse` with correct domain/aggregateId, `AuthorPartyId == agentPartyId`, deterministic `MessageId` derived from interaction+version, and returns `Posted`; **content never appears in the dispatched envelope or the returned result** (assert the serialized envelope `.ShouldNotContain` the generated text); membership `SeamUnavailable` → `PostingFailed`/`MembershipUnavailable` and **AppendMessageAsync is never called**; party not-available → `PartyIdentityUnavailable`; append `PostRejected` → `PostRejected`; Conversations throw → fail-closed `AdapterFailure`/`ConversationUnavailable` (no raw error leaked); **all-deferred default graph fails closed** (deferred readers + deferred poster → `PostingFailed`); confirmation-mode request short-circuits without dispatch; retried append reuses the same deterministic `MessageId`/idempotency key (assert equality across two invocations); end-to-end into the real aggregate via the dispatcher substitute; `OperationCanceledException` propagates (not swallowed).
  - [x] Build/test loop from `/home/administrator/projects/hexalith/agents/Hexalith.Agents`: `dotnet restore Hexalith.Agents.slnx` → `dotnet build Hexalith.Agents.slnx --configuration Release` (require 0 warnings / 0 errors; add `-m:1` if the parallel build flakes — Story 2.2 learning) → `dotnet test` each test project individually (NOT solution-wide; if VSTest reports `SocketException (13): Permission denied`, run the built xUnit v3 test executables directly — Story 2.2 learning). Keep structural guards green: `ProjectReferenceDirectionTests`, `ModuleLayout`, `PublicContractPackageBoundaryTests`, `StructuralSeedConformanceTests`, `PackageVersionCentralizationTests`, `BuildContractConformanceTests`.
  - [x] Write `_bmad-output/implementation-artifacts/tests/2-5-test-summary.md` with per-project pass counts and list it in the File List.

## Dev Notes

### Critical Guardrails

- **This story is the AUTOMATIC-MODE POSTING step ONLY.** It runs after Story 2.4 reaches `AgentInteractionStatus.Generated` for an interaction whose snapshot `ResponseMode == Automatic`. Implement: the two new status values, the posting value objects/enums, the `PostAgentResponse` command, the `AgentResponsePosted`/`AgentResponsePostingFailed` events, the `AgentResponseNotPostableRejection`, the pure `AgentResponsePostingPolicy`, the aggregate's 5th `Handle` + state fields/Apply, the two in-module readers (Agent Party + selected version), the Conversation posting+membership port (live config-gated + deferred fail-closed), the posting orchestrator + deterministic-id helper, and the DI wiring. **Do NOT implement:** confirmation-mode proposal creation (Story 3.1), approval-then-post (Story 3.5), edit/regenerate (Epic 3), the Conversation invocation/call-status UX (Story 2.6), exposing a new Conversations `AddParticipant` public seam (sibling module — out of scope, see below), or any operational-status projection/UI (Epic 4). [Source: epics.md#Story-2.5, #Story-2.6, #Story-3.1, #Story-3.5]
- **AD-3 aggregate purity.** `AgentInteractionAggregate.Handle(PostAgentResponse)` emits events only — no Conversations calls, no Party reads, no HTTP, no timers, no `DateTimeOffset.UtcNow`, no `Guid.NewGuid`. The Party read, version read, membership ensure, message append, and deterministic-id derivation all happen in `AgentInteractionPostingOrchestrator` (the durable-owner step) and return into the aggregate through the single `PostAgentResponse` command. [Source: ARCHITECTURE-SPINE.md#AD-3; src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs (Handle(GenerateAgentOutput) template)]
- **AD-6 / AD-7 Conversations boundary + Agent membership is the heart of this story (AC1/AC2).** Agents posts ONLY through the supported `Hexalith.Conversations.Client` boundary (`AppendMessageAsync`) and verifies membership ONLY through a Conversations-owned seam (`GetConversationAsync` participants today; `AddParticipant` when exposed). Agents NEVER writes Conversation streams/events directly. Before posting, ensure the Agent `PartyId` is valid and present as `ParticipantType.AiAgent`; **fail closed** (`PostingFailed`) if Party state, membership state, or the membership seam is missing/disabled/ambiguous/unauthorized/unavailable. [Source: ARCHITECTURE-SPINE.md#AD-6, #AD-7]
- **AD-13 deterministic ids / idempotent retry is the heart of AC3.** Conversation posting uses a deterministic `MessageId` and idempotency key derived from `AgentInteractionId` + selected `VersionId`. A retry reuses the same `MessageId`/key, so Conversations dedupes the append; the aggregate's terminal no-op on `Posted`/`PostingFailed` makes re-dispatching `PostAgentResponse` a deterministic no-op. No new aggregate identity is introduced (reuse `AgentInteractionId` as `AggregateId`). [Source: ARCHITECTURE-SPINE.md#AD-13]
- **AD-14 redaction is the heart of AC4.** The generated content is sensitive conversation-derived content. In this story it flows ONLY: in-module version reader → orchestrator (in-memory) → `IConversationResponsePoster.AppendAgentMessageAsync` (inside the adapter) → Conversations. It is ABSENT from the `PostAgentResponse` command, `AgentResponsePostingResult`, both posting events, the rejection, the new `AgentInteractionState` posting fields, the outcome result, logs, telemetry, and audit summaries. The posting evidence carries only safe ids: `MessageId`, `SourceConversationId`, `AgentPartyId` (a stable Party reference, not PII — AD-7), and `PostedVersionId`. Never persist Conversations error text/payloads or stack traces. [Source: ARCHITECTURE-SPINE.md#AD-14]
- **AD-12 / FR-12 fail closed on every dependency class.** Posting is prevented when Party identity, membership, Source Conversation access, or generation status is invalid. Each failure records `AgentResponsePostingFailed` audit evidence with a safe reason and creates no Conversation Message — exactly the shape `GenerationFailed`/`ContextBlocked`/`GateFailed` already use. [Source: prd FR12, FR21; ARCHITECTURE-SPINE.md#AD-12]
- **AD-18 single durable owner.** The orchestrator IS the durable-owner posting step (Microsoft Agent Framework workflow / Dapr Workflow in the AgentHost). It coordinates membership + append as replay/idempotency-safe steps and mutates Agents state ONLY through `PostAgentResponse`. Public contracts and EventStore aggregates must not depend on Microsoft Agent Framework, Dapr AI/Agents, provider SDK, workflow SDK, or `Hexalith.Conversations.*` types. [Source: ARCHITECTURE-SPINE.md#AD-18]

### Conversations Membership Seam Reality (verified — drives the deferred design)

A current-reality review of `Hexalith.Conversations` (sibling source commit `46df0cd`) confirmed the planning concern recorded in `sprint-change-proposal-2026-06-23.md` (proposed Story 2.0a) and `ARCHITECTURE-SPINE.md` (Deferred row "Public Conversations `AddParticipant` client/API seam if absent"):

- **`IConversationClient`** (`Hexalith.Conversations.Client`) exposes `CreateConversationAsync`, `AppendMessageAsync`, `ReassignConversationProjectAsync`, `GetConversationAsync`, `ListConversationsAsync`. **It does NOT expose `AddParticipantAsync`.**
- `AddParticipantCommand` (`Hexalith.Conversations.Contracts.Commands`) and a server-side `AddParticipantCommandHandler` **exist**, but the command is **not mapped to any HTTP API route** (`ConversationCommandApi` maps only create / append-message / reassign-project) and not surfaced on the client. So there is **no public membership-establish seam** Agents can call today.
- **What IS available for membership:** `GetConversationAsync` returns `ConversationDetailsV1.Participants` — a list of `ConversationParticipantProjectionV1(PartyId ParticipantPartyId, ParticipantType ParticipantType, ParticipantRole ParticipantRole, DateTimeOffset? OccurredAt)`. This lets Agents **verify** whether the Agent `PartyId` is already present as `ParticipantType.AiAgent`.

**Design consequence (and why this story is self-contained without Story 2.0a):** The sprint plan (`sprint-status.yaml`) does **not** contain a `2-0a` entry, and `epics.md` Story 2.5 retains its original AC1, so 2.5 is the next backlog story. Rather than block on a cross-module change (Conversations project-context forbids editing sibling modules from an Agents story), this story applies the **exact deferred/config-gated/fail-closed port pattern** the module already uses for the Conversations dependency in Stories 2.3/2.4 (and for the provider SDK in 2.4):

- **Membership VERIFY** is wired live over `GetConversationAsync` (the seam exists).
- **Membership ESTABLISH** has no public seam → the live port returns `SeamUnavailable` (→ `PostingFailed`/`MembershipUnavailable`) when the Agent is absent, and the deferred default returns `SeamUnavailable` for everything. This satisfies AD-7 "fails closed if the Conversations membership seam is missing/unavailable" by construction.
- A clearly-marked extension point + comment documents that when Conversations exposes a public `AddParticipantAsync(AddParticipantCommand,…)` (AD-6 / Story 2.0a), the establish path is wired to return `Established` — no Agents contract change required.

This keeps Story 2.5 entirely within `Hexalith.Agents`, fail-closed by default, and unblocked. [Source: sprint-change-proposal-2026-06-23.md (Story 2.0a, Modify Story 2.5); ARCHITECTURE-SPINE.md#AD-6 + Deferred; implementation-readiness-report-2026-06-23.md#2 (Conversations membership boundary)]

### Why No Persisted `PostingPending` State

UX-DR27/UX-DR28 list "posting pending" as a canonical state, but V1 does NOT persist a transient in-flight posting status in the aggregate — consistent with how the gate (2.2), context (2.3), and generation (2.4) steps work: the orchestrator does all impure work, then dispatches ONE command that records the terminal outcome (`Posted`/`PostingFailed`). A "posting pending" indicator is a UI/status derivation for Story 2.6, not a durable aggregate state. Persisting a transient state would require a second command before the append and break the single-command-per-step pattern. (Epic 3 proposal posting may introduce its own pending states; do not pre-create them here — leave additive ordinal room.) [Source: ARCHITECTURE-SPINE.md#AD-3 sequence; 2-4 story "Why No Persisted Generating State"]

### Design: Where Each Responsibility Lives

```
Durable owner (AgentHost: Agent Framework / Dapr Workflow)        ← AD-18, replay/idempotency-safe
  └─ AgentInteractionPostingOrchestrator.ExecuteAsync             ← IMPURE (Server.Application)
       ├─ IAgentPartyReader               (Agent's linked PartyId + valid/enabled — live posting-time gate, AD-7)
       ├─ IAgentGeneratedVersionReader    (selected VersionId + GeneratedContent — sensitive, stays server-side)
       ├─ IConversationResponsePoster
       │     ├─ EnsureAiAgentParticipantAsync  (verify via GetConversation participants; establish = SeamUnavailable today)
       │     └─ AppendAgentMessageAsync         (AppendMessageCommand: AuthorPartyId=agentPartyId, deterministic MessageId, IdempotencyKey)
       │   → derive deterministic MessageId/idempotency key (AgentResponsePostingIdentity, AD-13)
       │   → assemble server-trusted AgentResponsePostingResult (SAFE IDS ONLY — no content)
       └─ IAgentCommandDispatcher.DispatchAsync(PostAgentResponse envelope)
            └─ AgentInteractionAggregate.Handle(PostAgentResponse, state, env)   ← PURE (AD-3)
                 └─ AgentResponsePostingPolicy.Evaluate → DomainResult(events)    ← PURE, shared
                      └─ AgentInteractionState.Apply(AgentResponsePosted|…Failed) ← PURE
  status returned to caller = AgentResponsePostingPolicy.Decide(result)  ← shared, no drift
```

### Current Code State To Preserve

Read these files completely before editing — they are the exact templates to mirror:
- `src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionGenerationOrchestrator.cs` — the orchestrator template: `InteractionDomain` const, `_reservedExtensionKeys` + `BuildTrustedExtensions`, `CommandEnvelope` construction, per-read fail-closed `try/catch when (ex is not OperationCanceledException)`, return status via shared `Decide`.
- `src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionIdentity.cs` — the deterministic-id template (length-prefixed SHA-256 lowercase-hex). `AgentResponsePostingIdentity` mirrors it for `MessageId`/idempotency key.
- `src/Hexalith.Agents.Server/Ports/IConversationContextReader.cs` + `ConversationClientContextReader.cs` + `DeferredConversationContextReader.cs` — the Conversations-client wrapping port + live (config-gated over `IConversationClient`) + deferred fail-closed templates. `IConversationResponsePoster` mirrors this shape, reusing the same `IConversationClient` (already referenced by `Hexalith.Agents.Server`).
- `src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs` — `Handle(GenerateAgentOutput)`: the exact guard cascade (ThrowIfNull → positive bind → terminal NoOp → precondition rejection(s) → policy Evaluate → fall-through rejection) to copy.
- `src/Hexalith.Agents/AgentInteraction/AgentOutputGenerationPolicy.cs` — the `Evaluate`/`Decide`/`Compute` twin-policy template (internal; `InternalsVisibleTo` Server but NOT Server.Tests).
- `src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs` — mutable `sealed class`, nullable evidence fields, `Apply` overloads guarded by `if (!IsRequested) return;`, `MarkReplayOnlyEventHandled()` no-op for rejections.
- `src/Hexalith.Agents.Contracts/AgentInteraction/Events/AgentOutputGenerated.cs` and `…/AgentOutputGenerationFailed.cs` — success-event and recorded-negative-event templates (the latter carries `Decision`/`Reason` + evidence; `AgentResponsePostingFailed` mirrors it but with `Reason` + safe-id `Evidence`).
- `src/Hexalith.Agents.Contracts/AgentInteraction/AgentGenerationAttemptEvidence.cs` and `…/AgentOutputGenerationFailureReason.cs` — safe-evidence value-object and safe-reason-enum templates.
- `src/Hexalith.Agents.Contracts/AgentInteraction/AgentGeneratedVersion.cs` — the version the reader returns (its `VersionId`/`GeneratedContent` drive the post; content is sensitive — AD-14).
- `src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionSnapshot.cs` — carries `ResponseMode` (drives the automatic-mode precondition) but NOT the Agent's `PartyId` (hence `IAgentPartyReader`).
- `src/Hexalith.Agents/Agent/AgentState.cs` — `string? PartyId` is the Agent's linked Party reference the live `IAgentPartyReader` returns.
- `src/Hexalith.Agents.Server/Program.cs` — the deferred-port + orchestrator DI pattern and the config-gated `Conversations` `if/else` block to extend.

What must be preserved:
- `AgentInteractionStatus` ordinals 0–9 unchanged; append `Posted` (10) / `PostingFailed` (11) only.
- `Hexalith.Agents.Contracts` stays inward-only — NO reference to `Hexalith.Conversations.Contracts`/`.Client`/`Hexalith.Parties.Contracts`; all cross-module ids are opaque `string`. Conversations types (`AppendMessageCommand`, `ParticipantType`, `ConversationDetailsV1`, etc.) appear ONLY in the `Hexalith.Agents.Server` adapter, never in Contracts/domain/aggregate.
- The interaction is keyed by `AgentInteractionId` as `AggregateId` — posting does not introduce a new aggregate id.
- All existing structural/contract guard tests stay green; `net10.0`, `LangVersion 14`, nullable, warnings-as-errors, Central Package Management, `.slnx` only, SDK pinned `10.0.301`.
- EventStore events round-trip via plain `System.Text.Json`; enums survive by name (`[JsonConverter(typeof(JsonStringEnumConverter))]`). No `[PolymorphicSerialization]`/`[JsonDerivedType]` in the module.

### Posting Outcome → Event/Status Mapping

| Orchestrator `AgentResponsePostingOutcome` | Source | Event emitted | Status |
|---|---|---|---|
| `Posted` (membership Present/Established + append Posted) | — | `AgentResponsePosted(evidence)` | `Posted` (10) |
| `PartyIdentityUnavailable` | Agent Party not linked/disabled/unavailable | `AgentResponsePostingFailed(PartyIdentityUnavailable, …)` | `PostingFailed` (11) |
| `MembershipUnavailable` (seam absent/deferred, agent not a participant) | membership establish unavailable | `AgentResponsePostingFailed(MembershipUnavailable, …)` | `PostingFailed` (11) |
| `MembershipRejected` | Conversations rejected membership | `AgentResponsePostingFailed(MembershipRejected, …)` | `PostingFailed` (11) |
| `ConversationUnavailable` | conversation missing/unauthorized/stale | `AgentResponsePostingFailed(ConversationUnavailable, …)` | `PostingFailed` (11) |
| `PostRejected` | Conversations rejected the append | `AgentResponsePostingFailed(PostRejected, …)` | `PostingFailed` (11) |
| `AdapterFailure` (incl. default deferred graph, version read unavailable, transport throw) | fail-closed | `AgentResponsePostingFailed(AdapterFailure, …)` | `PostingFailed` (11) |

Wrong response mode (snapshot `ResponseMode != Automatic`) → `AgentResponseNotPostableRejection(NotAutomaticResponseMode)` (no state change). Wrong status (≠ `Generated`) → `AgentResponseNotPostableRejection(OutputNotGenerated)`. Never requested → `AgentResponseNotPostableRejection(InteractionNotRequested)`. [Source: prd FR11, FR12; ARCHITECTURE-SPINE.md#AD-7, #AD-12]

### Cross-Module Boundaries

- **Conversations (post + membership-verify, live behind `Conversations` config):** `IConversationResponsePoster` wraps `IConversationClient` — `GetConversationAsync` for membership verification (`ConversationDetailsV1.Participants` → `ParticipantType.AiAgent`) and `AppendMessageAsync(AppendMessageCommand{ Metadata: ConversationCommandMetadata{ TenantId, ActorPartyId: agentPartyId, CorrelationId, IdempotencyKey }, ConversationId, MessageId (deterministic), AuthorPartyId: agentPartyId, Text: generatedContent })`. Reuse the already-referenced `Hexalith.Conversations.Client` + `AddHexalithConversationsClient`. The Conversations identifiers (`PartyId`, `ConversationId`, `MessageId`) are Conversations value types in the adapter; Agents passes opaque `string`s across the port boundary. [Source: Hexalith.Conversations.Client/IConversationClient.cs; Conversations.Contracts/Commands/AppendMessageCommand.cs, .../ConversationCommandMetadata.cs; Conversations.Contracts/Participants/ParticipantType.cs; src/Hexalith.Agents.Server/Ports/ConversationClientContextReader.cs]
- **Conversations (membership-establish):** no public seam (see "Conversations Membership Seam Reality") — establish path returns `SeamUnavailable` (fail closed) and is documented as the AD-6/Story-2.0a prerequisite. Do NOT modify the sibling Conversations module.
- **Agent aggregate (in-module):** read the Agent's linked `PartyId` + valid/enabled state via the new `IAgentPartyReader` (the snapshot does not carry it). [Source: src/Hexalith.Agents/Agent/AgentState.cs `string? PartyId`]
- **AgentInteraction (in-module):** read the selected generated version (content + `VersionId`) via the new `IAgentGeneratedVersionReader` from the interaction's append-only `GeneratedVersions`. Content is sensitive (AD-14) — read once, hand to the poster, never persist/return/log. [Source: src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs `GeneratedVersions`; AgentGeneratedVersion.cs]
- **Parties:** not called directly — the Agent's `PartyId` is read from the Agent aggregate; AD-7 posting-time validity is the Agent-Party read's outcome plus the Conversations membership check. (Provisioning/linking was Story 1.4's `IAgentPartyDirectory`; not used here.)
- **Tenants:** tenant scope flows through `request.TenantId` → command envelope `TenantId` and every read's tenant; cross-tenant posting is impossible by construction (the orchestrator reads/posts only within `request.TenantId`). [Source: ARCHITECTURE-SPINE.md#AD-12; prd FR19]

### Latest Technical Information

- Stack baseline (do not change versions; Central Package Management — no inline versions): .NET SDK `10.0.300`–`10.0.301`, `net10.0`, `LangVersion 14`; `Microsoft.Agents.AI` / `Microsoft.Agents.AI.Workflows` `1.10.0`; Dapr packages / Dapr Workflow `1.18.4`; `ModelContextProtocol` `1.4.0`; MediatR `14.1.0`; FluentValidation `12.1.1`; OpenTelemetry `1.15.x`–`1.16.0`; xUnit v3 `3.2.2`; Shouldly `4.3.0`; NSubstitute `5.3.0`. `Hexalith.Conversations` sibling source commit `46df0cd` (the inspected seam). [Source: ARCHITECTURE-SPINE.md#Stack]
- The durable-owner runtime wiring (Agent Framework workflow / Dapr Workflow execution host) is an orchestration concern; in this story the orchestrator is the owner step and the live Conversations poster stays gated behind the `Conversations` config section — keep the default DI graph fail-closed. [Source: ARCHITECTURE-SPINE.md#AD-18]

### Testing Requirements

- **`Hexalith.Agents.Tests` (domain):** pure aggregate tests via static `Handle` + the `ProcessAndApplyAsync` reflection-dispatch + JSON round-trip + idempotent-NoOp + `Evaluate`/`Decide` no-drift. NO NSubstitute in aggregate logic. Cover the full outcome→event/status table, all three rejection reasons (`NotAutomaticResponseMode`, `OutputNotGenerated`, `InteractionNotRequested`), and terminal idempotency for both `Posted` and `PostingFailed`.
- **`Hexalith.Agents.Contracts.Tests`:** round-trip every new record/enum; assert ordinals 0–9 unchanged + 10–11 added; AD-14 no-content-leak serialization assertions across the events/result/rejection; ensure `ContractsSecretNonDisclosureTests` covers the new types; scope any "no attribute" assertion to `Hexalith`-namespaced attributes.
- **`Hexalith.Agents.Server.Tests`:** orchestrator tests with NSubstitute ports — happy path, every failure class, membership-seam-unavailable skips append, all-deferred-fails-closed, no content/error leak in dispatched envelope or result, deterministic-MessageId reuse on retry, confirmation-mode short-circuit, end-to-end into the real aggregate, cancellation propagation. Assert decisions via the orchestrator's returned status (the policy is not visible to Server.Tests).
- Build/test loop is in Task 10. Require 0 warnings / 0 errors; test each project individually; write `tests/2-5-test-summary.md`.

### Project Structure Notes

New/changed code (no new projects — extend existing module):
- **Contracts** (`src/Hexalith.Agents.Contracts/AgentInteraction/`): modify `AgentInteractionStatus.cs`; add `AgentResponsePostingFailureReason.cs`, `AgentResponsePostingResult.cs` (+ `AgentResponsePostingOutcome` enum), `AgentPostedMessageEvidence.cs`, `AgentResponseNotPostableReason.cs`; `Commands/PostAgentResponse.cs`; `Events/AgentResponsePosted.cs`, `Events/AgentResponsePostingFailed.cs`; `Events/Rejections/AgentResponseNotPostableRejection.cs`. (Optionally extend `AgentInteractionStatusView.cs` additively for posting status — keep it content-free.)
- **Domain** (`src/Hexalith.Agents/AgentInteraction/`): modify `AgentInteractionAggregate.cs` (+ `Handle`), `AgentInteractionState.cs` (+ posting fields/Apply); add `AgentResponsePostingPolicy.cs`.
- **Server** (`src/Hexalith.Agents.Server/`): `Ports/IAgentPartyReader.cs` + `AgentPartyReadResult.cs` + `DeferredAgentPartyReader.cs`; `Ports/IAgentGeneratedVersionReader.cs` + `AgentGeneratedVersionReadResult.cs` + `DeferredAgentGeneratedVersionReader.cs`; `Ports/IConversationResponsePoster.cs` + request/result types + `ConversationClientResponsePoster.cs` + `DeferredConversationResponsePoster.cs`; `Application/AgentInteractions/AgentInteractionPostingOrchestrator.cs` + `AgentInteractionPostingRequest.cs`/`AgentInteractionPostingOutcomeResult.cs` + `AgentResponsePostingIdentity.cs`; modify `Program.cs`.
- **Tests:** add `AgentInteractionPostingAggregateTests.cs`, `AgentInteractionPostingContractsTests.cs`, `AgentInteractionPostingOrchestratorTests.cs`; extend `AgentInteractionTestData.cs` + `AgentInteractionStateReplayTests.cs`; write `tests/2-5-test-summary.md`.

Discovery loaded: epics.md (Epic 2 full, Stories 2.4/2.5/2.6/3.1/3.5 boundaries, FR coverage), prd FR2/FR11/FR12/FR19–FR21 + NFR1/NFR3, ARCHITECTURE-SPINE.md (AD-2/3/6/7/12/13/14/17/18 + Stack + Structural Seed + automatic-post sequence + Deferred AddParticipant row), sprint-change-proposal-2026-06-23.md (Story 2.0a membership prerequisite; Modify Story 2.5) and implementation-readiness-report-2026-06-23.md (Conversations membership boundary risk), the live AgentInteraction aggregate/state/policies/contracts (Stories 2.1–2.4), the Story 1.4 Agent Party link + Story 1.6 response mode, the Conversations public client/contracts (commit `46df0cd`: `IConversationClient`, `AppendMessageCommand`, `AddParticipantCommand` [unexposed], `ParticipantType`, `ConversationDetailsV1`), the Server orchestrators + ports + DI, and the xUnit v3/Shouldly/NSubstitute test patterns + shared `AgentInteractionTestData`.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.5-Post-Automatic-Responses-Through-Conversations]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.4] (upstream `Generated` precondition) , [#Story-2.6] (call-status UX — out of scope), [#Story-3.1, #Story-3.5] (confirmation-mode proposal/approval posting — out of scope)
- [Source: prd FR2, FR11, FR12, FR19, FR20, FR21; NFR1, NFR3]
- [Source: ARCHITECTURE-SPINE.md#AD-2 (AgentInteraction ownership / additive status), #AD-3 (aggregate purity), #AD-6 (Conversations boundary + AddParticipant prerequisite), #AD-7 (Agent Party identity + AiAgent membership + fail closed), #AD-12 (fail closed on dependency uncertainty), #AD-13 (deterministic MessageId/idempotency key), #AD-14 (sensitive content & secret safety), #AD-17 (test gates), #AD-18 (single durable owner)]
- [Source: ARCHITECTURE-SPINE.md#sequence-diagram (automatic: "ensure AIAgent participant + AppendMessage" → "RecordPostingSucceeded/Failed"); #Deferred ("Public Conversations AddParticipant client/API seam if absent"); #Stack (Conversations commit 46df0cd)]
- [Source: _bmad-output/planning-artifacts/sprint-change-proposal-2026-06-23.md (Story 2.0a: Verify Or Expose Conversations AI Agent Membership Boundary; Modify Story 2.5)]
- [Source: _bmad-output/planning-artifacts/implementation-readiness-report-2026-06-23.md (#2 Conversations membership boundary may block posting)]
- [Source: src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionStatus.cs (ordinals 0–9 to preserve; appends Posted/PostingFailed)]
- [Source: src/Hexalith.Agents.Contracts/Agent/AgentResponseMode.cs (Automatic/Confirmation precondition)]
- [Source: src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionSnapshot.cs (ResponseMode snapshot; no Agent PartyId)]
- [Source: src/Hexalith.Agents.Contracts/AgentInteraction/AgentGeneratedVersion.cs (VersionId + sensitive GeneratedContent)]
- [Source: src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs (Handle(GenerateAgentOutput) handler template)]
- [Source: src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs (state + Apply pattern)]
- [Source: src/Hexalith.Agents/AgentInteraction/AgentOutputGenerationPolicy.cs (Evaluate/Decide twin-policy)]
- [Source: src/Hexalith.Agents.Contracts/AgentInteraction/Events/AgentOutputGenerated.cs, AgentOutputGenerationFailed.cs, Events/Rejections/AgentOutputNotGeneratableRejection.cs, Commands/GenerateAgentOutput.cs (event/command/rejection templates)]
- [Source: src/Hexalith.Agents.Contracts/AgentInteraction/AgentGenerationAttemptEvidence.cs, AgentOutputGenerationFailureReason.cs (safe value-object/reason-enum templates)]
- [Source: src/Hexalith.Agents/Agent/AgentState.cs (string? PartyId — the Agent's linked Party reference)]
- [Source: src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionGenerationOrchestrator.cs (orchestrator template), AgentInteractionIdentity.cs (deterministic-id template)]
- [Source: src/Hexalith.Agents.Server/Ports/IConversationContextReader.cs, ConversationClientContextReader.cs, DeferredConversationContextReader.cs (Conversations-client port: interface + live config-gated + deferred fail-closed)]
- [Source: src/Hexalith.Agents.Server/Program.cs (deferred-port + orchestrator DI; config-gated Conversations if/else)]
- [Source: Hexalith.Conversations/src/Hexalith.Conversations.Client/IConversationClient.cs (AppendMessageAsync, GetConversationAsync; no AddParticipantAsync)]
- [Source: Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Commands/AppendMessageCommand.cs, ConversationCommandMetadata.cs (deterministic MessageId, settable AuthorPartyId, IdempotencyKey)]
- [Source: Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Commands/AddParticipantCommand.cs + Server/CommandHandlers/AddParticipantCommandHandler.cs (exist but not exposed on client/API)]
- [Source: Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Participants/ParticipantType.cs (AiAgent → "AIAgent"), ParticipantRole.cs (Member/Facilitator/Observer)]
- [Source: Hexalith.Conversations/src/Hexalith.Conversations.Contracts/Queries/ConversationDetailsV1.cs + Projections/ConversationParticipantProjectionV1.cs (membership verification source)]
- [Source: tests/Hexalith.Agents.Tests/AgentInteractionTestData.cs (shared fixtures + ApplyAll switch); tests/Hexalith.Agents.Server.Tests/AgentInteractionGenerationOrchestratorTests.cs (orchestrator test pattern)]
- [Source: _bmad-output/implementation-artifacts/2-4-generate-and-safety-check-agent-output.md, 2-3-...md, 2-2-...md, 2-1-...md (prior-story learnings: CA1062 positive guard, `-m:1` serialized build, VSTest socket fallback, Nullable attribute scoping, internal policy not visible to Server.Tests, re-read via port not carried state, no transient in-flight aggregate state)]
- [Source: Hexalith.Conversations/_bmad-output/project-context.md (do not edit sibling module; participant membership owned by Conversations), Hexalith.Parties/_bmad-output/project-context.md, Hexalith.EventStore/_bmad-output/project-context.md, Hexalith.Tenants/_bmad-output/project-context.md]

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created. Story status is `ready-for-dev`.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (1M context) — `claude-opus-4-8[1m]`

### Debug Log References

- `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` → **0 warnings / 0 errors**.
- `dotnet test` per project (Release, `--no-build`): domain 527, contracts 175, server 232, UI 156 → **1090 passed / 0 failed / 0 skipped**. No VSTest socket fallback needed. (Server count includes the live `ConversationClientResponsePosterTests` adapter coverage + the AI-review stale-projection regression.)

### Completion Notes List

Implemented the automatic-mode posting step on the existing `AgentInteraction` aggregate, fail-closed by default and entirely within `Hexalith.Agents`:

- **Contracts (additive, AD-2):** appended `AgentInteractionStatus.Posted` (10) / `PostingFailed` (11) — ordinals 0–9 unperturbed. Added `AgentResponsePostingFailureReason`, `AgentResponsePostingOutcome` (+ `AgentResponsePostingResult`), `AgentPostedMessageEvidence` (safe ids only), `AgentResponseNotPostableReason`, the `PostAgentResponse` command, the `AgentResponsePosted`/`AgentResponsePostingFailed` events, and the `AgentResponseNotPostableRejection`. Every posting surface is structurally content-free (AD-14); Contracts stays inward-only (no Conversations/Parties reference).
- **Domain:** added the pure twin-policy `AgentResponsePostingPolicy` (`Evaluate`/`Decide`/`Compute`, internal — visible to Server, not Server.Tests), the aggregate's 5th `Handle(PostAgentResponse)` (ThrowIfNull → positive bind → terminal NoOp on Posted/PostingFailed → not-automatic rejection → not-generated rejection → policy Evaluate → fall-through not-requested rejection), and the additive state fields `PostingEvidence`/`PostingFailureReason` + `Apply(AgentResponsePosted|…Failed)` + no-op `Apply(rejection)`. Used the CA1062-safe positive null-guard (Story 2.2 learning).
- **Server (deferred/fail-closed):** `IAgentPartyReader` (+ deferred) reads the Agent's linked `PartyId` + posting-time validity live (snapshot does not carry it — AD-7); `IAgentGeneratedVersionReader` (+ deferred) reads the selected version + sensitive content (content read once, handed to the poster, never persisted/returned — AD-14); `IConversationResponsePoster` over `IConversationClient` — live `ConversationClientResponsePoster` (membership VERIFY via `GetConversationAsync` participants; ESTABLISH returns `SeamUnavailable` since no public `AddParticipantAsync` exists — documented extension point for Story 2.0a; append via `AppendMessageAsync` authored by the Agent Party) + deferred fail-closed. The `AgentInteractionPostingOrchestrator` is the durable-owner step: reads party → reads version → ensures membership → derives deterministic `MessageId`/idempotency key (`AgentResponsePostingIdentity`, SHA-256) → appends → assembles a server-trusted result (safe ids only) → dispatches `PostAgentResponse`; returns status via the shared `Decide` (no drift).
- **DI (Program.cs):** Story 2.5 block registers the two deferred in-module readers + the config-gated poster (live inside the existing `Conversations` `if`, deferred in the `else`) + the orchestrator. The default DI graph is fail-closed: no party, no content, no membership → every path resolves to `PostingFailed`.
- **AC coverage:** AC1 membership boundary or fail-closed (party + membership reads, no message on failure); AC2 message authored by the Agent `PartyId` + deterministic `MessageId` from interaction+version + `CorrelationId`; AC3 deterministic ids + terminal idempotent NoOp; AC4 `PostingFailed` distinct from generation/auth/context/safety failure, safe-id-only evidence, no content/payload/error leak (asserted by serialization no-leak tests).

### File List

**Added — Contracts (`Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/`):**
- `AgentResponsePostingFailureReason.cs`
- `AgentResponsePostingResult.cs` (+ `AgentResponsePostingOutcome` enum)
- `AgentPostedMessageEvidence.cs`
- `AgentResponseNotPostableReason.cs`
- `Commands/PostAgentResponse.cs`
- `Events/AgentResponsePosted.cs`
- `Events/AgentResponsePostingFailed.cs`
- `Events/Rejections/AgentResponseNotPostableRejection.cs`

**Added — Domain (`Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/`):**
- `AgentResponsePostingPolicy.cs`

**Added — Server (`Hexalith.Agents/src/Hexalith.Agents.Server/`):**
- `Ports/IAgentPartyReader.cs`, `Ports/AgentPartyReadResult.cs`, `Ports/DeferredAgentPartyReader.cs`
- `Ports/IAgentGeneratedVersionReader.cs`, `Ports/AgentGeneratedVersionReadResult.cs`, `Ports/DeferredAgentGeneratedVersionReader.cs`
- `Ports/IConversationResponsePoster.cs`, `Ports/ConversationMembershipRequest.cs`, `Ports/ConversationMembershipResult.cs`, `Ports/ConversationAppendRequest.cs`, `Ports/ConversationAppendResult.cs`, `Ports/ConversationClientResponsePoster.cs`, `Ports/DeferredConversationResponsePoster.cs`
- `Application/AgentInteractions/AgentInteractionPostingOrchestrator.cs`, `Application/AgentInteractions/AgentInteractionPostingRequest.cs` (+ `AgentInteractionPostingOutcomeResult`), `Application/AgentInteractions/AgentResponsePostingIdentity.cs`

**Modified:**
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionStatus.cs` (appended `Posted`/`PostingFailed`; summary comment)
- `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs` (`Handle(PostAgentResponse)` + `using Hexalith.Agents.Contracts.Agent`)
- `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs` (posting fields + `Apply` overloads)
- `Hexalith.Agents/src/Hexalith.Agents.Server/Program.cs` (Story 2.5 DI block + config-gated poster)

**Added — Tests:**
- `Hexalith.Agents/tests/Hexalith.Agents.Tests/AgentInteractionPostingAggregateTests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Contracts.Tests/AgentInteractionPostingContractsTests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Server.Tests/AgentInteractionPostingOrchestratorTests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Server.Tests/ConversationClientResponsePosterTests.cs` (live Conversations adapter: membership verify type-discrimination + trust-bearing/stale fail-closed + append authorship)
- `_bmad-output/implementation-artifacts/tests/2-5-test-summary.md`

**Modified — Tests:**
- `Hexalith.Agents/tests/Hexalith.Agents.Tests/AgentInteractionTestData.cs` (Story 2.5 fixtures + `ApplyAll` cases)
- `Hexalith.Agents/tests/Hexalith.Agents.Tests/AgentInteractionStateReplayTests.cs` (posting replay tests)

### Senior Developer Review (AI)

**Reviewer:** Administrator · **Date:** 2026-06-24 · **Outcome:** Approve (after auto-fix)

Adversarial review of AC1–AC4 against the implementation, with build + per-project test re-run (0 warnings / 0 errors; 1090 tests pass). Two issues found and auto-fixed; no Critical issues remain.

- **[HIGH — fixed] Live membership verify failed open on a stale projection (AC1, AD-7, Task 7).** `ConversationClientResponsePoster.EnsureAiAgentParticipantAsync` inspected `ConversationDetailsV1.Participants` without checking `details.Freshness.AllowsTrustBearingDecision()`. A `Visible`-but-stale read still carries the participant list, so a revoked-but-not-yet-projected membership could read as `Present` and the post would proceed. Task 7 explicitly requires "stale conversation → ConversationUnavailable", and the sibling `ConversationClientContextReader` already enforces this. **Fix:** added the trust-bearing guard (→ `ConversationUnavailable`) before participant inspection + a regression test (`A_visible_but_stale_projection_fails_closed_to_conversation_unavailable_even_when_the_agent_is_present`).
- **[MEDIUM — fixed] File List + test counts were stale (documentation).** `ConversationClientResponsePosterTests.cs` (live-adapter coverage) was present in git but absent from the File List, and the recorded counts (526/218/1075) were stale. **Fix:** File List + Debug Log counts corrected to the actual 527/175/232/156 = 1090; test-summary doc updated.

### Change Log

| Date | Version | Description | Author |
|------|---------|-------------|--------|
| 2026-06-24 | 0.1 | Initial story context created (ready-for-dev) | Administrator |
| 2026-06-24 | 1.0 | Implemented automatic-mode posting (Tasks 1–10): status states, contracts, pure policy, aggregate handler + state, in-module readers, Conversations posting/membership port, orchestrator + deterministic ids, DI, tests. Build 0/0; 1075 tests pass. Status → review | Amelia (Dev Agent) |
| 2026-06-24 | 1.1 | Senior Developer Review (AI): fixed live membership verify to fail closed on a stale/non-trust-bearing projection (AD-7) + regression test; corrected File List + test counts (1090 pass). Build 0/0. Status → done | Administrator |
