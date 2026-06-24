---
baseline_commit: 57f11a5997bbe4a90b2be2c16591466703809dd4
---

# Story 3.6: Reject, Abandon, And Expire Proposals

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an Approver or system policy,
I want proposals to reach explicit terminal states when they should not be posted,
so that stale or rejected generated content cannot later enter a Conversation.

**Epic:** Epic 3 - Proposal Review And Approval Workflow.
**FR coverage:** FR-18 (move a Proposed Agent Reply to rejected / abandoned / expired terminal states) plus FR-14 version preservation, FR-7 approver-policy authorization for terminal actions, FR-20 authorization before side effects, FR-21 fail-closed on dependency uncertainty, FR-24 audit evidence for rejections/abandonments/expirations, and FR-22/FR-23 admin-UI + API/client visibility of expiry.
**Position:** Sixth story in Epic 3. It depends on Story 3.1 proposal creation (which already records optional `ExpiresAt` metadata and ships the `IProposalExpiryPolicyReader` port), Story 3.3 edit, Story 3.4 regeneration, and Story 3.5 approval/posting (which added the prior terminal states and the approver-authorization + idempotency precedents). It completes the AD-5 proposal lifecycle; Story 3.7 then builds the detail workspace, version-history panel, and full accessibility over these states.

## Acceptance Criteria

1. **Authorized rejection moves the proposal to a rejected terminal state, preserving versions.**
   - **Given** a proposal is pending
   - **When** an authorized Approver rejects it
   - **Then** the proposal moves to rejected terminal state with rationale metadata where policy requires it
   - **And** all versions remain preserved for authorized audit.

2. **Authorized abandonment moves the proposal to an abandoned terminal state that can never act again.**
   - **Given** a proposal is pending
   - **When** an authorized Approver abandons it
   - **Then** the proposal moves to abandoned terminal state
   - **And** it cannot later be approved, edited, regenerated, or posted.

3. **Configured expiry moves the proposal deterministically to an expired terminal state, visible in UI and API.**
   - **Given** proposal expiry policy exists
   - **When** the configured expiry is reached
   - **Then** the proposal moves deterministically to expired terminal state
   - **And** expiry behavior is visible through admin UI and API/client contracts.

4. **Terminal proposals reject any approve/post attempt before side effects and route the user to a new call.**
   - **Given** a rejected, abandoned, or expired proposal exists
   - **When** a caller attempts to approve or post it
   - **Then** the action is rejected before Conversation side effects
   - **And** the UI/API routes the user to start a new Agent Call if a response is still needed.

## Tasks / Subtasks

> Implement in the same additive pattern as Stories 3.3, 3.4, and 3.5: Contracts -> Domain aggregate + twin policy -> Server orchestrator (Deferred-adapter wired) -> UI gateway/control + presentation -> tests. Reject/abandon/expire are **terminal state transitions, not new versions** — they append NO entry to `GeneratedVersions`, perform NO Conversations side effects, and reuse the existing `ProposalId`. Keep the aggregate pure (no wall-clock, no I/O).

- [x] **Task 1 - Contracts: reject / abandon / expire surface** (AC: #1, #2, #3, #4)
  - [x] Append to `ProposedAgentReplyState` (`src/Hexalith.Agents.Contracts/AgentInteraction/ProposedAgentReplyState.cs`) the three deferred values **in this order** after `PostingFailed (=7)`: `Rejected (=8)`, `Abandoned (=9)`, `Expired (=10)`. Preserve every existing ordinal. Update the enum `<remarks>` so it no longer says these are deferred — they are now owned and present. (The file's remarks at L14-18 explicitly reserved these for Story 3.6.)
  - [x] Append to `AgentInteractionStatus` (`.../AgentInteractionStatus.cs`) after `ProposalApprovalFailed (=22)`: `ProposalRejected`, `ProposalAbandoned`, `ProposalExpired`, and the fail-closed variants `ProposalRejectionFailed`, `ProposalAbandonmentFailed` (mirroring how 3.5 added `ProposalApprovalFailed`). Expiry is system-triggered and does not need a `ProposalExpiryFailed` status unless the policy reader is unavailable — add `ProposalExpiryFailed` only if you model that fail-closed path explicitly. Preserve all ordinals.
  - [x] Add reason enums mirroring `AgentProposedReplyNotApprovableReason.cs` / `AgentProposedReplyNotEditableReason.cs`: `AgentProposedReplyNotRejectableReason` and `AgentProposedReplyNotAbandonableReason` with `{ Unknown=0, InteractionNotProposed, ProposalNotPending, NotAuthorized }`; and `AgentProposedReplyNotExpirableReason` with `{ Unknown=0, InteractionNotProposed, ProposalNotPending, NoExpiryPolicy, ExpiryNotReached }`.
  - [x] Add outcome enums mirroring `AgentProposalApprovalOutcome.cs`: `AgentProposalRejectionOutcome`/`AgentProposalAbandonmentOutcome` `{ Unknown=0, Rejected|Abandoned, AlreadyTerminal, NotAuthorized, PolicyFailure, ProposalNotPending }`; and `AgentProposalExpiryOutcome` `{ Unknown=0, Expired, AlreadyTerminal, NoExpiryPolicy, ExpiryNotReached, ProposalNotPending }`.
  - [x] Add write-path result carriers mirroring `AgentProposalApprovalResult.cs` (safe ids only, never content): `AgentProposalRejectionResult` / `AgentProposalAbandonmentResult` carrying `Outcome, ProposalId, SourceConversationId, ActorPartyId, int ApproverPolicyVersion, ApproverPolicyValidationStatus AuthorizationVerdict, ApproverPolicyBasisDisclosure DisclosureCategory`, plus for rejection an optional safe `string? RationaleCode` (a policy-defined code/category, NOT free text and NOT generated content). `AgentProposalExpiryResult` carrying `Outcome, ProposalId, SourceConversationId, string? ExpiresAt` (no approver fields — expiry is system policy).
  - [x] Add content-free evidence records mirroring `AgentProposedReplyApprovalEvidence.cs` (AD-14): `AgentProposedReplyRejectionEvidence`, `AgentProposedReplyAbandonmentEvidence`, `AgentProposedReplyExpiryEvidence`. Carry proposal id, source conversation id, actor party id (null for system expiry), approver policy version + verdict + disclosure category (reject/abandon only), `ExpiresAt` (expiry only), and the safe `RationaleCode` (reject only). No version content, no payloads.
  - [x] Add commands under `Commands/` mirroring `ApproveProposedAgentReply.cs`: `RejectProposedAgentReply(string AgentInteractionId, AgentProposalRejectionResult Result)`, `AbandonProposedAgentReply(... AgentProposalAbandonmentResult Result)`, `ExpireProposedAgentReply(... AgentProposalExpiryResult Result)`.
  - [x] Add success events under `Events/` (`: IEventPayload`, no usings — `IEventPayload` is a global using): `ProposedAgentReplyRejected(string AgentInteractionId, AgentProposedReplyRejectionEvidence Evidence)`, `ProposedAgentReplyAbandoned(...)`, `ProposedAgentReplyExpired(...)`, plus fail-closed `ProposedAgentReplyRejectionFailed(string AgentInteractionId, AgentProposedReplyNotRejectableReason Reason, ...Evidence Evidence)` and `ProposedAgentReplyAbandonmentFailed(...)` mirroring `ProposedAgentReplyApprovalFailed.cs`.
  - [x] Add structural rejections under `Events/Rejections/` (`: IRejectionEvent`): `ProposedAgentReplyNotRejectableRejection(string AgentInteractionId, AgentProposedReplyNotRejectableReason Reason)`, `ProposedAgentReplyNotAbandonableRejection(...)`, `ProposedAgentReplyNotExpirableRejection(...)`.
  - [x] (Optional, Epic-4-deferred read contract) If you ship a stable terminal read surface, mirror the `Queries/AgentProposalApprovalEvidence*` trio (`Get...Query`, `...View` with `string? RejectedAt/AbandonedAt/ExpiredAt`, `...Result` with `Success/NotAuthorized/NotFound`). The live query handler stays Epic 4.

- [x] **Task 2 - Domain: terminal-state policies, aggregate handlers, and replay** (AC: #1, #2, #3, #4)
  - [x] Add twin Evaluate/Decide policies in `src/Hexalith.Agents/AgentInteraction/` mirroring `AgentProposalEditPolicy.cs` (the single-event template, NOT the multi-event approval one): `AgentProposalRejectionPolicy`, `AgentProposalAbandonmentPolicy`, `AgentProposalExpiryPolicy`. Each: `internal static DomainResult Evaluate(string interactionId, <Result> result)` (emits exactly one success or one `...Failed` event), `internal static AgentInteractionStatus Decide(<Result> result)`, and a shared `private static Compute(...)` so orchestrator status can never drift from the recorded decision. Reject/abandon `Compute` requires success outcome **AND** `AuthorizationVerdict == ApproverPolicyValidationStatus.Valid` (defense-in-depth; orchestrator also refuses). Expiry `Compute` does **NOT** gate on an approver verdict — its authority is the elapsed `ExpiresAt`; it maps `NoExpiryPolicy`/`ExpiryNotReached` to safe failures.
  - [x] Add three `Handle(...)` overloads to `AgentInteractionAggregate.cs` **between the end of the Approve handler (L537) and the first private helper (L539)**. Use the canonical body order from the Edit/Regenerate handlers: (1) `ArgumentNullException.ThrowIfNull(command/envelope)` + `string interactionId = envelope.AggregateId`; (2) `if (state is { IsRequested: true } requested)` positive-bind; (3) idempotency NoOp first — `requested.ProposalState is ProposedAgentReplyState.Rejected → DomainResult.NoOp()` (re-issuing a terminal command on an already-rejected/abandoned/expired proposal is a clean no-op per AD-13); (4) precondition guard on the **sub-state** `requested.ProposalState is Pending or Edited or Regenerated` else `DomainResult.Rejection([ new ...NotRejectableRejection(interactionId, ProposalNotPending) ])`; (5) tail `return <Policy>.Evaluate(interactionId, command.Result);`. The `if (state is...)` falls through to `DomainResult.Rejection([ new ...Rejection(interactionId, InteractionNotProposed) ])`.
  - [x] Reject/abandon/expire need NO version-id idempotency helper (no new version is appended); idempotency is keyed on the terminal sub-state only. Do not add to `GeneratedVersions`.
  - [x] Add replay handlers to `AgentInteractionState.cs` mirroring `Apply(ProposedAgentReplyApproved)` (L397): `Apply(ProposedAgentReplyRejected e)` / `Apply(ProposedAgentReplyAbandoned e)` / `Apply(ProposedAgentReplyExpired e)` — each guards `if (!IsRequested) return;`, sets `Status` (`ProposalRejected`/`ProposalAbandoned`/`ProposalExpired`) and the terminal `ProposalState`, and stores the new evidence on a new state field (`ProposalRejectionEvidence` / `ProposalAbandonmentEvidence` / `ProposalExpiryEvidence`). **Must NOT touch `GeneratedVersions`** (FR-14 / AD-5 version preservation). Add no-op `Apply(...)` for each new `...Rejection`/`...Failed` event using the `MarkReplayOnlyEventHandled()` pattern so replay stays total.
  - [x] **Extend the terminal-guard sets** so AC2/AC4 are structurally enforced everywhere: confirm the Edit `editable`, Regenerate `retryable`, and Approve `approvable` sets in the aggregate already exclude the new states. The Approve `approvable` allow-list (`{ Pending, Edited, Regenerated, Approved, PostingPending, PostingFailed }`) does NOT include `Rejected/Abandoned/Expired`, so approve already falls through to `ProposalNotPending` once the enum values exist — add a domain test proving it, and verify the edit/regenerate guards reject them too.

- [x] **Task 3 - Server: reject / abandon / expire orchestrators** (AC: #1, #2, #3, #4)
  - [x] Add `AgentInteractionProposalRejectionOrchestrator` and `AgentInteractionProposalAbandonmentOrchestrator` under `src/Hexalith.Agents.Server/Application/AgentInteractions/`, mirroring the **Edit** orchestrator (the minimal-deps template): inject only `IApproverPolicyResolver` + `IAgentCommandDispatcher`. Pipeline order (fail closed): structural sub-state guard → authorization (`ResolveAuthorizationAsync` → `ApproverPolicyVerdict.Evaluate`) → assemble the safe `<Action>Result` → dispatch one command. A non-`Valid` verdict still dispatches a `...Failed` command (audit), never a success. Do NOT inject Conversations/party/version/poster ports — terminal actions touch no Conversation.
  - [x] Add `AgentInteractionProposalExpiryOrchestrator` injecting `IProposalExpiryPolicyReader` + `IAgentCommandDispatcher`. It (a) reads the snapshotted/recorded `ExpiresAt` (from the proposal evidence / expiry reader for the tenant+agent), (b) compares it to a **trusted evaluation "now" supplied on the request** — NOT read inside the aggregate or via `DateTimeOffset.UtcNow` in the orchestrator's hot path; accept it as a parameter so determinism is unit-testable (AC3) — (c) if `ExpiresAt` is null → `NoExpiryPolicy` safe outcome, no dispatch; if `now < ExpiresAt` → `ExpiryNotReached` safe outcome, no dispatch; if `now >= ExpiresAt` → dispatch `ExpireProposedAgentReply`. Expiry requires no approver authorization (system policy) but stays tenant-scoped (FR-19).
  - [x] Request DTOs mirroring `AgentInteractionProposalApprovalRequest.cs`: `AgentInteractionProposalRejectionRequest` / `...AbandonmentRequest` carry the header block (`MessageId, CorrelationId, TenantId, AgentInteractionId, ProposalId`), `ProposalState` (for the guard), `ApproverPolicy`/`ApproverPolicyVersion`, the actor/approver `PartyId`, optional `RationaleCode` (reject), then `ActorUserId, string? ClientCorrelationId = null, IReadOnlyDictionary<string,string>? ClientSuppliedExtensions = null`. `AgentInteractionProposalExpiryRequest` carries ids + `ProposalState` + recorded `ExpiresAt` + the trusted evaluation timestamp; no approver fields. Each request file also declares the paired safe `...OutcomeResult`.
  - [x] Build command envelopes exactly like the approval orchestrator (`CommandEnvelope(MessageId, TenantId, "agent-interaction", AggregateId, nameof(command), JsonSerializer.SerializeToUtf8Bytes(command), CorrelationId, CausationId:null, ActorUserId, BuildTrustedExtensions(...))`) and strip reserved extension keys. Status returned to callers comes only from the shared `<Policy>.Decide(result)` (AD-3 no-drift).
  - [x] Every `catch` uses `when (ex is not OperationCanceledException)` so genuine cancellation propagates without dispatch.
  - [x] Register in `Program.cs` in a new `// Story 3.6:` comment block inserted **before the `app` build line (~L206)**, after the `// Story 3.5` block: `builder.Services.AddScoped<AgentInteractionProposalRejectionOrchestrator>();` `...AbandonmentOrchestrator`; `...ExpiryOrchestrator`. New aggregate `Handle` overloads and event types auto-register via the existing `AddEventStoreDomainService` assembly scan — no host change needed for them.
  - [x] **Expiry binding / trigger scope (read the Dev Note "Expiry mechanism decision"):** keep the registered `IProposalExpiryPolicyReader` as the `DeferredProposalExpiryPolicyReader` default (returns `None` = fail-closed no-expiry) and keep `IAgentCommandDispatcher` Deferred, consistent with every prior Epic-3 orchestrator. Do NOT introduce an `IHostedService`/timer/Dapr-reminder sweep — none exists in the module and creating the module's first scheduler is Epic-4 durable-owner scope (AD-18). This story ships the deterministic expiry *decision + transition*; the automatic *firing trigger* and the live reader/dispatcher bindings are Epic 4.

- [x] **Task 4 - UI: state presentation, gateways, and terminal actions** (AC: #1, #2, #3, #4)
  - [x] Extend `Components/Shared/ProposedAgentReplyStatePresentation.cs`: add `ColorFor` arms `Rejected => BadgeColor.Danger`, `Abandoned => BadgeColor.Subtle`, `Expired => BadgeColor.Severe` (per DESIGN `#Colors`); add `IconFor` arms using only the curated `FcFluentIcons` factory (e.g. `Dismiss16`/`DismissCircle16` for rejected, `Subtract16`/`Archive16` for abandoned, `Clock*`/`Warning16` for expired — verify names against the pinned Fluent package). The three states currently fall through the total default (`Subtle` + `QuestionCircle16` + missing label key); these arms replace that.
  - [x] Add resx label keys consumed by `LabelKeyFor` / queue `StateOptionLabel`: `Agents.ProposalState.Label.Rejected`, `.Abandoned`, `.Expired` in **both** `Resources/AgentsResources.resx` and `AgentsResources.fr.resx` (EN/FR parity is exact and required — currently 314 entries each, zero diff).
  - [x] Add gateway quartets under `Services/Gateways/` mirroring the approval set (`IProposalApprovalGateway` + `DeferredProposalApprovalGateway` + `ProposalApprovalRequest` + `ProposalApprovalResult`): `IProposalRejectionGateway`/`DeferredProposalRejectionGateway`/`ProposalRejectionRequest`/`ProposalRejectionResult` and the abandon equivalents. Deferred impls fail closed (return `...Result.NotAuthorized()`/`.Unavailable()`, no content). Register with `services.TryAddScoped<IProposalRejectionGateway, DeferredProposalRejectionGateway>();` (etc.) in `AgentsUiServiceCollectionExtensions.AddAgentsUi()`. There is **no** expire gateway — expiry is system-initiated, not a UI button.
  - [x] Add reusable `ProposalRejector` and `ProposalAbandoner` action controls under `Components/Shared/` mirroring `ProposalApprover.razor` / `ProposalRegenerator.razor`: ids-only requests (no content to the gateway), `CanReject`/`CanAbandon` gates, status switch over `Agents.ProposalRejector.Status.*` / `Agents.ProposalAbandoner.Status.*` keys, `EventCallback` host-refresh, accessible names from whole localized strings only (no id/content in `aria-label`/`data-testid`/status strings). Provide a non-committing escape (`Esc` cancels the confirm without acting) and keyboard-reachable controls. For reject, accept the optional policy-driven `RationaleCode` via a bounded selector, not free text. Note: the full keyboard-focus contract, version-history, and live-region a11y belong to **Story 3.7** — keep these controls minimal and hostable.
  - [x] Terminal visibility + "start a new Agent Call" routing (AC3, AC4): the `ProposalQueue` state filter auto-gains `Rejected`/`Abandoned`/`Expired` options from `Enum.GetNames<ProposedAgentReplyState>()` once the enum grows (each needs the resx label above); surface terminal proposals by passing `includeHistorical: true` to `IProposalQueueGateway.ListPendingProposalsAsync` (the flag is already reserved "Stories 3.5/3.6"). Add a safe "Start a new Agent Call" affordance/copy for terminal proposals per EXPERIENCE `#Voice-and-Tone` ("This proposal expired. Start a new Agent call."). Do not style any terminal proposal as a posted Conversation Message.

- [x] **Task 5 - Tests across all layers** (AC: #1, #2, #3, #4)
  - [x] Contracts tests (mirror `AgentInteractionProposalApprovalContractsTests.cs`): enum-by-name JSON round-trips; **ordinal stability** for `ProposedAgentReplyState` (`Rejected=8, Abandoned=9, Expired=10`) and `AgentInteractionStatus` (new tail values); marker-interface presence on events/rejections; no-content-bearing-member assertions on results/evidence; `RationaleCode` is a code not content.
  - [x] Domain tests (mirror `AgentInteractionProposalApprovalAggregateTests.cs` + lifecycle E2E): authorized reject records `Rejected` + rationale code and preserves all prior versions; authorized abandon records `Abandoned`; expire with `now >= ExpiresAt` records `Expired` deterministically and with `now < ExpiresAt` does not transition; non-pending/terminal proposal rejects with `ProposalNotPending`; non-requested interaction rejects with `InteractionNotProposed`; unauthorized reject/abandon maps to a `...Failed`/`NotAuthorized` decision and never a success; re-issuing a terminal command is `NoOp()`; **AC4 cross-guard** — a rejected/abandoned/expired proposal rejects subsequent approve, edit, and regenerate before any side effect; `GeneratedVersions` unchanged across every terminal transition; `Evaluate`/`Decide` no-drift over every outcome.
  - [x] Server tests (mirror `AgentInteractionProposalApprovalOrchestratorTests.cs`): reject/abandon happy path dispatches exactly one command; unauthorized/non-`Valid` policy dispatches the `...Failed` command and never a success; structural non-pending guard returns without dispatch; expiry orchestrator is deterministic for a fixed injected "now" (reached → dispatch, not-reached → no dispatch, no-policy → no dispatch); `OperationCanceledException` propagates without dispatch; Deferred dispatcher/reader fail-closed behavior holds.
  - [x] UI tests (mirror `ProposalApproverTests.cs` + `ProposedAgentReplyStatePresentationTests.cs` + `BadgeConformanceTests` + `LocalizationResourceTests`): deferred gateways fail closed; `ProposalRejector`/`ProposalAbandoner` happy/failure statuses; no id/content in accessible names/test ids/status strings; presentation totality for the three new states (color+icon+label, no raw hex); EN/FR resource parity for all new keys; queue surfaces terminal states via `includeHistorical` and offers the "start a new call" affordance.
  - [x] Run from `Hexalith.Agents/`: `dotnet restore Hexalith.Agents.slnx`; `dotnet build Hexalith.Agents.slnx --configuration Release -m:1`; run each touched test project individually. If VSTest socket permission fails, run the built xUnit v3 executables directly.

- [x] **Task 6 - Dev Agent Record accuracy**
  - [x] Regenerate test counts from actual run output. Story 3.5 ended (after QA automation) at Contracts 278, Domain 629, Server 325, UI 513; 0 errors, 0 failed, 0 skipped.
  - [x] Diff the File List against `git status` before review. Include every created/modified production file, test file, the story file, the `sprint-status.yaml` entry, and the test-summary artifact.

## Dev Notes

### Critical guardrails

- **Reuse the existing `AgentInteraction` aggregate (AD-2).** Reject/abandon/expire are transitions on the proposal lifecycle already implemented by Stories 3.1-3.5. Do not create a new aggregate, and reuse the existing `ProposalId` — these are not new versions.
- **Terminal = append-only and version-preserving (AD-5, FR-14, FR-24).** Rejected/abandoned/expired proposals preserve every generated/edited/regenerated version for authorized audit. The `Apply(...)` handlers must set only `Status` + `ProposalState` + new evidence and must never mutate or clear `GeneratedVersions`.
- **Pure aggregate, no wall-clock (AD-3, Time convention).** The aggregate must not read `DateTimeOffset.UtcNow`. Expiry "now" is decided outside the aggregate (orchestrator/durable owner) and passed in as a trusted value on the expire command's result, exactly as request time is server-stamped from EventStore metadata.
- **Fail closed before side effects (AD-12, FR-20, FR-21).** Reject/abandon authorize via the snapshotted Approver Policy (`IApproverPolicyResolver` → `ApproverPolicyVerdict`); a non-`Valid` verdict yields a safe `...Failed` decision, never a terminal success. Expiry is system policy (no approver gate) but stays tenant-scoped and fails closed to "no expiry" when the policy reader is unavailable.
- **Idempotent terminal transitions (AD-13).** Re-issuing reject/abandon/expire on an already-terminal proposal is a clean `NoOp()`; idempotency is keyed on the terminal sub-state, not on a new id.
- **AC4 is structural.** Once `Rejected/Abandoned/Expired` exist, the Approve/Edit/Regenerate guards already exclude them (approve falls through to `ProposalNotPending`). Add explicit tests proving approve/post/edit/regenerate are all rejected before side effects; the UI/API routes the user to start a new Agent Call.
- **Content confinement (AD-14).** Rationale on reject is a safe policy-defined `RationaleCode` (a code/category), never free text and never generated content. All evidence/status/UI strings carry ids + coarse reasons + whole localized strings only — no raw content, payloads, stack traces, or runtime-assembled sentences.

### Expiry mechanism decision (the one genuinely open design point — read before Task 3)

The codebase has **no scheduler of any kind**: a repo-wide search finds no `IHostedService`, `BackgroundService`, timer, Dapr reminder, cron, or sweep, and there is no server-side clock seam (`TimeProvider`/`UtcNow` appear only in the UI's age-bucket column). The aggregate explicitly forbids wall-clock reads. The only existing "expiry" today is a **lazy, read-time** display derivation in `ProposalQueue.razor` (`ExpiresAt < TimeProvider.GetUtcNow()` for the "Expired"/"ExpiringSoon" filters). Story 3.1 records optional `ExpiresAt` metadata; the `IProposalExpiryPolicyReader` port was shipped with its live binding **and** enforcement explicitly handed to this story.

**Decision for Story 3.6 (chosen to match the module's established Deferred-adapter precedent and AD-3/AD-18):**

1. **Implement the deterministic expiry decision + transition now**: the `ExpireProposedAgentReply` command, `AgentProposalExpiryPolicy`, the aggregate handler, the `Expired` state/event, and `AgentInteractionProposalExpiryOrchestrator`. The orchestrator compares the recorded `ExpiresAt` to a **trusted evaluation timestamp passed on the request** so determinism (AC3) is proven by unit tests with a fixed clock — no aggregate wall-clock, no real timer needed to demonstrate correctness.
2. **Do not build the module's first background sweep / hosted service / reminder.** Wiring *who calls the expiry orchestrator and when* (a durable sweep, a lazy read-time trigger, or an Agent-Framework/Dapr reminder) is a durable-owner concern that AD-18 places in application orchestration, and every prior Epic-3 orchestrator already ships against **Deferred** dispatcher/reader bindings with live wiring deferred to Epic 4. Keep `IProposalExpiryPolicyReader` → `DeferredProposalExpiryPolicyReader` (fail-closed `None`) and `IAgentCommandDispatcher` Deferred. Document this boundary in Completion Notes.
3. **Satisfy AC3 visibility without the live trigger:** the new `Expired` enum value gives the badge presentation (Severe + icon + text), the auto-populated queue filter, and the stable read contract; the existing lazy read-time "Expired/ExpiringSoon" display continues to surface impending/elapsed expiry. This makes expiry "visible through admin UI and API/client contracts" (AC3, FR-22/FR-23, AD-15) while the deterministic terminal-transition logic is implemented and tested.

If the reviewer or product owner wants the automatic firing trigger inside this story instead, that is a scope expansion (introduces the module's first scheduler) and should be called out explicitly — the safe, precedent-consistent default is to ship the transition + orchestrator + tests here and bind the trigger in Epic 4. (PRD OQ-3 — the exact expiry duration min/max — is also still open; never hardcode a duration, always read it via the port.)

### Existing implementation anchors (prevent reinvention)

- Proposal lifecycle aggregate/state/policies (Stories 3.1-3.5):
  - `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs` (Approve handler ends ~L537; insert new handlers L537-539)
  - `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs` (`Apply(ProposedAgentReplyApproved)` ~L397; `MarkReplayOnlyEventHandled()` ~L507)
  - `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentProposalEditPolicy.cs` (single-event twin-policy template)
  - `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentProposalApprovalPolicy.cs` (multi-event variant — reference, not the template here)
- Contracts templates:
  - `.../Contracts/AgentInteraction/ProposedAgentReplyState.cs` (append the 3 states), `AgentInteractionStatus.cs` (append the tail statuses)
  - `.../Contracts/AgentInteraction/AgentProposalApprovalResult.cs`, `AgentProposedReplyApprovalEvidence.cs`, `AgentProposedReplyNotApprovableReason.cs`, `AgentProposalApprovalOutcome.cs`
  - `.../Contracts/AgentInteraction/Commands/ApproveProposedAgentReply.cs`, `Events/ProposedAgentReplyApproved.cs`, `Events/ProposedAgentReplyApprovalFailed.cs`, `Events/Rejections/ProposedAgentReplyNotApprovableRejection.cs`
  - Expiry metadata already present: `AgentProposedReplyEvidence.cs` (`string? ExpiresAt`), `AgentProposalCreationResult.cs`, `PendingProposalView.cs`
- Server templates:
  - `.../Server/Application/AgentInteractions/AgentInteractionProposalEditOrchestrator.cs` (minimal-deps template for reject/abandon)
  - `.../Server/Application/AgentInteractions/AgentInteractionProposalApprovalOrchestrator.cs` + `AgentInteractionProposalApprovalRequest.cs` (envelope/extension/no-drift pattern)
  - `.../Server/Ports/IProposalExpiryPolicyReader.cs` + `DeferredProposalExpiryPolicyReader.cs` (`ProposalExpiryPolicyResult.None`)
  - `.../Server/Ports/IApproverPolicyResolver.cs`, `IAgentCommandDispatcher.cs`
  - `.../Server/Program.cs` (`// Story 3.5` block ~L200-204; insert `// Story 3.6` before the `app` build ~L206)
- UI templates:
  - `.../UI/Components/Shared/ProposalApprover.razor`, `ProposalRegenerator.razor`, `ProposedAgentReplyStatePresentation.cs`
  - `.../UI/Components/Pages/ProposalQueue.razor` (`includeHistorical`, state-filter auto-population, lazy expiry comparison)
  - `.../UI/Services/Gateways/IProposalApprovalGateway.cs`, `DeferredProposalApprovalGateway.cs`, `ProposalApprovalRequest.cs`, `ProposalApprovalResult.cs`, `AgentsUiServiceCollectionExtensions.cs`
  - `.../UI/Resources/AgentsResources.resx` + `AgentsResources.fr.resx` (314 entries each; maintain parity)

### Project Structure Notes

- Contracts under `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/` with commands/events/rejections/queries in the established subfolders. `IEventPayload`/`IRejectionEvent` are global usings — no per-file using, no polymorphic JSON attributes, no manual registration.
- Domain policy + aggregate/state logic under `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/`.
- Server orchestrators/request DTOs under `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/`; DI in `Program.cs`.
- UI gateways/controls/presentation/localization under `.../UI/Services/Gateways/`, `Components/Shared/`, `Components/Pages/`, and both `AgentsResources.resx` files.
- Use xUnit v3, Shouldly, NSubstitute, and bUnit (Blazor UI), matching existing tests. Avoid raw `Assert.*`.

### Previous Story Intelligence

- Story 3.5 added `Approved`/`PostingPending`/`Posted`/`PostingFailed` to `ProposedAgentReplyState` and `ProposalApproved`..`ProposalApprovalFailed` to `AgentInteractionStatus`; it also fixed XML-doc that wrongly said the 3.5 states "must NOT be added" — when you append the 3.6 states, **update `ProposedAgentReplyState`'s remarks** so it no longer lists `Rejected/Abandoned/Expired` as deferred (a 3.5-style doc-vs-enum review finding otherwise recurs).
- Story 3.5's review confirmed the orchestrator order structural-state → authorization → ... → append and `OperationCanceledException` propagation; reuse that exact discipline.
- The recurring Epic-2/Epic-3 review finding is **stale Dev Agent Record test counts and File List omissions** — Task 6 exists to prevent it; regenerate counts from a clean run and diff the File List against `git status`.
- Approver authorization, `ApproverPolicyVerdict.Evaluate` (Valid only when every source resolves), and deterministic envelopes are all established — do not re-derive them.

### References

- `_bmad-output/planning-artifacts/epics.md#Story 3.6: Reject, Abandon, And Expire Proposals` (ACs verbatim) and `#Epic 3: Proposal Review And Approval Workflow`.
- `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-18` (reject/abandon/expire), `#FR-14` (version preservation), `#FR-7` (approver policy authorizes rejection/abandonment/expiry-resolution), `#FR-20`/`#FR-21` (authorization + fail-closed), `#FR-24` (audit evidence for rejections/abandonments/expirations), `#FR-22`/`#FR-23` (admin UI + API visibility), `#OQ-3` (expiry duration open), `#SM-3` (terminal-state completion metric).
- `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-5` (append-only proposal lifecycle; rejected/abandoned/expired preserve versions, cannot post), `#AD-2`, `#AD-3` (pure aggregates, expiry timers outside), `#AD-12` (authorization + fail-closed), `#AD-13` (idempotent effects), `#AD-14` (content/secret safety), `#AD-15` (UI/API parity), `#AD-18` (durable owners coordinate expiry), `#Consistency-Conventions` (Time: injected time + snapshotted policy, no aggregate wall-clock).
- `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md#Colors` + `#proposal-state-badge` (rejected=Danger, abandoned=Subtle, expired=Severe, expiring=Warning; color+icon+text mandatory) and `EXPERIENCE.md#Voice-and-Tone` + `#Proposal-Lifecycle` ("start a new Agent Call" routing; terminal-state semantics).
- `_bmad-output/implementation-artifacts/3-5-approve-a-selected-version-and-post-it.md`, `3-3-edit-proposed-reply-versions.md`, `3-4-regenerate-proposed-reply-versions.md`, `3-1-create-proposed-agent-replies-in-confirmation-mode.md`.

### Scope boundaries

- **Story 3.7 (NOT here):** the proposal detail workspace, the version-history rendering component, the full keyboard-only navigation/focus contract, and live-region announcements for transitions. Story 3.6 ships the domain transitions, authorization, expiry decision/transition, presentation arms, minimal reject/abandon controls, and terminal visibility/routing.
- **Epic 4 (NOT here):** live `IAgentCommandDispatcher` binding, projections/read-model query handlers, the live `IProposalExpiryPolicyReader` source and the automatic expiry firing trigger (sweep/reminder/hosted service), and the audit-evidence query implementation. This story builds against the Deferred adapters, matching every prior Epic-3 story.
- **Gated (per PRD §9 / OQ-8):** audit retention / legal-hold / export / deletion behavior for preserved versions — out of scope; this story only preserves versions, it does not define retention.

## Create-Story Checklist Validation

- [x] Target story determined from sprint status: `3-6-reject-abandon-and-expire-proposals` (status backlog → ready-for-dev). Epic 3 already in-progress; story 6 is not the first, so no epic-status change.
- [x] Epic 3 ACs and PRD FR-18 (+ FR-7/14/20/21/22/23/24) reflected verbatim in ACs/tasks.
- [x] Architecture invariants AD-2/3/5/12/13/14/15/18 + Time convention mapped to guardrails.
- [x] UX badge semantics (Danger/Subtle/Severe), color+icon+text rule, and "start a new Agent Call" routing captured.
- [x] Exact enum ordinals (`Rejected=8/Abandoned=9/Expired=10`; status tail from 23) and aggregate insertion point (L537-539) specified.
- [x] Previous-story intelligence from Stories 3.1, 3.3, 3.4, 3.5 included (doc-vs-enum fix, stale-count discipline).
- [x] Existing implementation anchors listed to prevent reinvention; expiry port reuse called out.
- [x] The expiry-trigger open design point resolved with a precedent-consistent decision and a clear scope boundary.
- [x] File locations, test projects, and build/test commands specified.
- [x] Scope boundaries for Story 3.7 and Epic 4 stated.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- Activation/config: `resolve_customization.py --skill .agents/skills/bmad-dev-story --key workflow` returned no activation prepend/append steps and one persistent project-context glob; no `Hexalith.Agents` project-context file existed.
- Restore: `dotnet restore Hexalith.Agents.slnx --nologo` exited 1 with no console output in this sandbox. Fallback `dotnet msbuild src/Hexalith.Agents.Contracts/Hexalith.Agents.Contracts.csproj /t:Restore /v:diag /nologo` succeeded and reported the sampled restore graph up to date.
- Build: `dotnet msbuild Hexalith.Agents.slnx /t:Build /p:Configuration=Release /m:1 /v:minimal /nologo` passed with 0 errors; `dotnet build Hexalith.Agents.slnx --configuration Release -m:1 --no-restore --nologo` passed with 0 warnings and 0 errors.
- VSTest: `dotnet test ... --no-build` was blocked by sandbox socket permissions (`System.Net.Sockets.SocketException (13): Permission denied`), so xUnit v3 executables were run directly.
- Final xUnit counts (after QA-automation gap-fill): Contracts 282, Domain 651, Server 335, UI 561; all errors 0, failed 0, skipped 0. (The dev-story run recorded Contracts 282, Domain 648, Server 334, UI 525; the `bmad-qa-generate-e2e-tests` step then added Domain +3, Server +1, UI +36 tests — see `tests/3-6-qa-e2e-test-summary.md` — and these post-QA totals are the reviewable reality from `dotnet test`.)

### Completion Notes List

- Added Story 3.6 terminal proposal contract surface: `Rejected`, `Abandoned`, `Expired` states/statuses, safe reasons/outcomes/results/evidence, commands, success/fail-closed events, and structural rejections.
- Added pure aggregate terminal policies and handlers for reject, abandon, and expire. Terminal transitions preserve `GeneratedVersions`, append no new version, perform no Conversation side effects, and replay evidence onto state.
- Added server orchestrators and request DTOs for reject/abandon/expire with structural guards, approver-policy fail-closed behavior for human terminal actions, deterministic trusted-timestamp expiry, deferred adapter boundaries, and cancellation propagation.
- Added UI terminal presentation, localization parity, fail-closed reject/abandon gateways, minimal reusable `ProposalRejector`/`ProposalAbandoner` controls, historical proposal queue visibility, and "start a new Agent Call" routing copy for terminal proposals.
- Added focused Story 3.6 tests across contracts, domain, server, and UI. New coverage proves terminal idempotency, post-terminal approve/edit/regenerate guards, no-drift policy decisions, safe payloads, deterministic expiry, deferred fail-closed behavior, and UI control behavior.
- Documented Epic 4 boundary: this story ships deterministic expiry decision/transition and UI/API visibility, not the module's first scheduler or live expiry trigger.

### File List

- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionStatus.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/ProposedAgentReplyState.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposalAbandonmentOutcome.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposalAbandonmentResult.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposalExpiryOutcome.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposalExpiryResult.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposalRejectionOutcome.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposalRejectionResult.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposedReplyAbandonmentEvidence.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposedReplyExpiryEvidence.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposedReplyNotAbandonableReason.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposedReplyNotExpirableReason.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposedReplyNotRejectableReason.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/AgentProposedReplyRejectionEvidence.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Commands/AbandonProposedAgentReply.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Commands/ExpireProposedAgentReply.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Commands/RejectProposedAgentReply.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Events/ProposedAgentReplyAbandoned.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Events/ProposedAgentReplyAbandonmentFailed.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Events/ProposedAgentReplyExpired.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Events/ProposedAgentReplyRejected.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Events/ProposedAgentReplyRejectionFailed.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Events/Rejections/ProposedAgentReplyNotAbandonableRejection.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Events/Rejections/ProposedAgentReplyNotExpirableRejection.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/AgentInteraction/Events/Rejections/ProposedAgentReplyNotRejectableRejection.cs`
- `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs`
- `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs`
- `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentProposalAbandonmentPolicy.cs`
- `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentProposalExpiryPolicy.cs`
- `Hexalith.Agents/src/Hexalith.Agents/AgentInteraction/AgentProposalRejectionPolicy.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalAbandonmentOrchestrator.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalAbandonmentRequest.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalExpiryOrchestrator.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalExpiryRequest.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalRejectionOrchestrator.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalRejectionRequest.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Server/Program.cs`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Components/Pages/ProposalQueue.razor`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Components/Shared/ProposalAbandoner.razor`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Components/Shared/ProposalRejector.razor`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Components/Shared/ProposedAgentReplyStatePresentation.cs`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Resources/AgentsResources.fr.resx`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Resources/AgentsResources.resx`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Services/Gateways/AgentsUiServiceCollectionExtensions.cs`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Services/Gateways/DeferredProposalAbandonmentGateway.cs`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Services/Gateways/DeferredProposalRejectionGateway.cs`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Services/Gateways/IProposalAbandonmentGateway.cs`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Services/Gateways/IProposalRejectionGateway.cs`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Services/Gateways/ProposalAbandonmentRequest.cs`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Services/Gateways/ProposalAbandonmentResult.cs`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Services/Gateways/ProposalRejectionRequest.cs`
- `Hexalith.Agents/src/Hexalith.Agents.UI/Services/Gateways/ProposalRejectionResult.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Contracts.Tests/AgentInteractionProposalTerminalContractsTests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Server.Tests/AgentInteractionProposalTerminalOrchestratorTests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Tests/AgentInteractionProposalTerminalAggregateTests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Tests/AgentInteractionTestData.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.UI.Tests/AgentsTestContext.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.UI.Tests/ProposalQueueTests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.UI.Tests/ProposalTerminalActionTests.cs`
- `_bmad-output/implementation-artifacts/3-6-reject-abandon-and-expire-proposals.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/3-6-test-summary.md`
- `_bmad-output/implementation-artifacts/tests/3-6-qa-e2e-test-summary.md`

### Change Log

- 2026-06-25: Implemented Story 3.6 reject/abandon/expire terminal proposal workflow across contracts, domain, server, UI, tests, and BMAD tracking.
- 2026-06-25: Senior Developer Review (AI) — Approved. Auto-fixed two documentation-accuracy findings (stale "Final xUnit counts" → 282/651/335/561; File List missing `tests/3-6-qa-e2e-test-summary.md`). No production code changes required; status → done.

## Senior Developer Review (AI)

**Reviewer:** Administrator · **Date:** 2026-06-25 · **Outcome:** Approve (no critical issues)

Adversarial validation of every File-List file against git reality, all four ACs against implementation, and every `[x]` task against code. Build is clean (`dotnet build ... --configuration Release` → 0 warnings, 0 errors); the full suite passes (`dotnet test` → Contracts 282, Domain 651, Server 335, UI 561; 0 failed, 0 skipped).

**Verified strengths**
- **AC1/AC2 (reject/abandon):** Pure aggregate handlers + twin `Evaluate`/`Decide` policies require BOTH a success outcome AND `ApproverPolicyValidationStatus.Valid` (defense-in-depth); orchestrators fail closed (non-`Valid` verdict still dispatches a `...Failed` audit command, never a success) with the canonical pipeline order (structural guard → authorization → assemble → dispatch) and `catch ... when (ex is not OperationCanceledException)` cancellation propagation.
- **AC3 (expire):** Deterministic — the aggregate never reads the clock; "now" is a trusted request timestamp (`EvaluationTimestamp`) compared to the recorded `ExpiresAt`; `now < expiry → ExpiryNotReached`, null/unparseable → `NoExpiryPolicy`, both no-dispatch. Deferred reader/dispatcher boundary documented per AD-18 (Epic-4 firing trigger correctly out of scope).
- **AC4 (terminal guards):** Structurally enforced — `Rejected/Abandoned/Expired` excluded from the approve/edit/regenerate eligibility sets; the cross-terminal guard test proves a *different* terminal command on an already-terminal proposal returns `ProposalNotPending` (NOT a silent NoOp) with versions preserved.
- **Version preservation (FR-14/AD-5):** Every terminal `Apply(...)` sets only `Status` + `ProposalState` + evidence and never touches `GeneratedVersions`; verified by tests.
- **Content confinement (AD-14):** Evidence/results/UI carry ids + coarse reasons + whole localized strings only; reject rationale is a bounded policy CODE, not free text. Enum ordinals stable (`Rejected=8/Abandoned=9/Expired=10`; status tail 23–27). Presentation switches total, no raw hex. resx EN/FR parity exact (340/340).

**Findings (both MEDIUM/LOW, auto-fixed — the recurring Task-6 class):**
1. *Dev Agent Record "Final xUnit counts" stale* — recorded 648/334/525 (the pre-QA dev-run totals); actual post-QA totals are 651/335/561. Corrected with provenance.
2. *File List omitted* `tests/3-6-qa-e2e-test-summary.md` (the QA-automation artifact). Added.

No security, performance, or correctness defects found. The implementation matches the Story 3.3/3.4/3.5 additive precedent exactly.
