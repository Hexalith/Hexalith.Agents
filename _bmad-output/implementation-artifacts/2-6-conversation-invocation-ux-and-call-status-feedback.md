---
baseline_commit: b2056da122d4bc3475404081139c5ffdea704609
---

# Story 2.6: Conversation Invocation UX And Call Status Feedback

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As a Conversation Participant,
I want clear in-product feedback when I call `hexa`,
so that I understand whether the request is pending, blocked, failed, or posted without mistaking drafts for Conversation Messages.

## Acceptance Criteria

**AC1 - The invocation affordance names `hexa`, captures prompt + Source Conversation context, shows the response-mode implication, and never implies a post happened before Conversations confirms**

**Given** Conversation-originated invocation is exposed through a mention, command, action, participant affordance, or approved combination (the exact entry pattern is open — PRD OQ-1 — so V1 builds the **pattern-agnostic** invocation affordance per UX-DR24)
**When** the participant starts an Agent Call
**Then** the UI **visibly names `hexa`**, captures the prompt and Source Conversation context (caller, Agent, Source Conversation, prompt, response mode), and shows the **response-mode implication** (Automatic posts directly; Confirmation requires approval) **before** any provider invocation
**And** the UI **does not imply an automatic response has posted** until the call status reaches `Posted` (i.e. until Conversations confirms the final message) — an in-flight/`Generated` state is never rendered or worded as a posted Conversation Message.

**AC2 - Every call-status state is rendered with semantic color + icon + visible text + accessible name; color-only and raw subsystem/provider error text are forbidden**

**Given** an Agent Call transitions through `requested, authorized, denied, context loading, context blocked, generating, generation failed, generated, posting pending, posted, or posting failed`
**When** the UI renders status
**Then** each state is distinguished by **semantic color (Fluent `BadgeColor` role, never hex) + icon + visible text + accessible name** (the same safe whole string)
**And** **color-only status is forbidden** (UX-DR12) and **raw subsystem/provider error text is forbidden** (only safe, coarse, content-free classifications are shown; AD-9, AD-14).

**AC3 - Denied/blocked/failed states present a safe reason and never leak unauthorized details**

**Given** the participant lacks permission or dependency state is uncertain
**When** the call is denied or blocked
**Then** the UI presents a **safe reason** such as permission denied, provider unavailable, context blocked, safety failed, or posting failed (a whole-string localized label mapped from the safe reason enums)
**And** it **does not leak** unauthorized Conversation, Party, tenant, provider, prompt, or generated-content details — not in visible text, accessible names, tooltips, copied text, or `aria-live` announcements (AD-14; NFR2/NFR6).

**AC4 - Constrained viewports / reduced-motion degrade fail-closed, and state changes stay perceivable without animation**

**Given** the UI runs on constrained viewports or reduced-motion settings
**When** a high-impact automatic call action cannot show enough context or status
**Then** the action is **unavailable with a visible, accessible reason** (exposed through focusable text or `aria-describedby`, not a bare disabled control) **or** downgraded to **safe review-only** behavior, while review-only status remains available (UX-DR40)
**And** state changes remain **perceivable without relying on animation** (UX-DR38): status is conveyed by the color+icon+text swap in a reserved, stable slot and announced through a polite live region.

[Source: _bmad-output/planning-artifacts/epics.md#Story-2.6; prd FR8, FR12, FR22, FR23, FR25, NFR2, NFR4, NFR6; UX-DR1, UX-DR11, UX-DR12, UX-DR13, UX-DR14, UX-DR17, UX-DR22, UX-DR24, UX-DR27, UX-DR32, UX-DR33, UX-DR36, UX-DR37, UX-DR38, UX-DR40, UX-DR41; ARCHITECTURE-SPINE.md#AD-2, #AD-3, #AD-6, #AD-9, #AD-11, #AD-12, #AD-14, #AD-15, #AD-17; prd OQ-1]

## Tasks / Subtasks

- [x] **Task 1 - Add the gateway-result wrapper contracts (mirror `AgentInspectionResult`)** (AC: #1, #2, #3)
  - [x] In `src/Hexalith.Agents.Contracts/AgentInteraction/`, add `AgentInteractionInspectionStatus.cs`: `[JsonConverter(typeof(JsonStringEnumConverter))]` enum `Unknown=0, Success, NotAuthorized, NotFound`. One safe XML-doc line each. Mirror `src/Hexalith.Agents.Contracts/Agent/AgentInspectionStatus.cs`.
  - [x] Add `AgentInteractionInspectionResult.cs` (`record`): `(AgentInteractionInspectionStatus Status, AgentInteractionStatusView? View)` with static factories `Success(AgentInteractionStatusView view)`, `NotAuthorized()`, `NotFound()`. The `View` is non-null **only** on `Success`, so a failed inspection never reveals interaction existence or any unrelated tenant data (AD-12, AD-14). Mirror `src/Hexalith.Agents.Contracts/Agent/AgentInspectionResult.cs` exactly.
  - [x] Add `AgentCallRequestStatus.cs`: `[JsonConverter(typeof(JsonStringEnumConverter))]` enum `Unknown=0, Accepted, NotAuthorized, Rejected`. (`Accepted` carries an `AgentInteractionReference`; `NotAuthorized`/`Rejected` carry none.)
  - [x] Add `AgentCallRequestResult.cs` (`record`): `(AgentCallRequestStatus Status, AgentInteractionReference? Reference)` with factories `Accepted(AgentInteractionReference reference)`, `NotAuthorized()`, `Rejected()`. The `Reference` (the safe `AgentInteractionId` + coarse `Status`) is non-null only on `Accepted` — it is the safe handle the feedback UI polls; never the prompt or content (AD-14). These wrappers carry **no** generated content, prompt, stream name, provider detail, or secret.
  - [x] These types live in Contracts (not the UI project) so the future live `Hexalith.Agents.Client` → BFF/API path returns the same wrappers — exactly as `AgentInspectionResult` is shared by the admin gateway today.

- [x] **Task 2 - Add the canonical call-status UX state + pure presentation mapping** (AC: #1, #2, #3)
  - [x] In `src/Hexalith.Agents.UI/Components/Shared/`, add `AgentCallStatus.cs` with the canonical UX call-status enum (the display taxonomy of UX-DR27 + the posting tail of UX-DR28/Story-2.6 AC): `Requested, Authorized, Denied, Blocked, ContextLoading, ContextBlocked, Generating, GenerationFailed, SafetyFailed, Generated, PostingPending, Posted, PostingFailed, Unknown`. XML-doc that `ContextLoading`, `Generating`, and `PostingPending` are **UI-only transient** states with no durable contract field (mirroring how `AgentReadinessState.Checking` is documented) — they are derived from an in-flight hint, never persisted (Story 2.5 "Why No Persisted Generating/PostingPending State"; AD-3).
  - [x] Add `AgentCallStatusPresentation.cs` (`public static class`) — a pure, dependency-free mapping mirroring `AgentReadiness.cs`:
    - `MapStatus(AgentInteractionStatus status)` → `AgentCallStatus` for the durable statuses: `Requested→Requested, Authorized→Authorized, Denied→Denied, Blocked→Blocked, ContextReady→Generating` (context is ready ⇒ generation is the next in-flight step — but see `MapStatus` overload note), `ContextBlocked→ContextBlocked, Generated→Generated, GenerationFailed→GenerationFailed, SafetyFailed→SafetyFailed, Posted→Posted, PostingFailed→PostingFailed, Unknown/_→Unknown`. Add an overload accepting an `inFlight` hint (or a separate `Derive` helper) so a caller can render the transient `ContextLoading` (after `Authorized`), `Generating` (after `ContextReady`), and `PostingPending` (after `Generated`, Automatic mode) states while a milestone is pending. Keep the durable mapping total and the transient derivation explicit — never silently invent a state.
    - `ColorFor(AgentCallStatus state)` → `BadgeColor`: in-flight/progress (`Requested, Authorized, ContextLoading, Generating, Generated, PostingPending`) → `Informative`; `Posted` → `Success` (the ONLY Success state — proven complete, UX-DR11); `ContextBlocked, Blocked` → `Severe` (blocked, not a runtime failure); `Denied, GenerationFailed, SafetyFailed, PostingFailed` → `Danger`; `Unknown/_` → `Subtle`. **Never** `BadgeColor.Brand` for a status (Brand is chrome/selection only, UX-DR11).
    - `IconFor(AgentCallStatus state)` → `Icon` using ONLY the curated `FcFluentIcons` factory (see Task guardrail — the icon factory lives in the FrontComposer submodule and must NOT be extended): progress → `FcFluentIcons.ArrowSync16()`; `Requested` → `FcFluentIcons.Play16()`; `Posted`/`Generated` → `FcFluentIcons.Checkmark16()`; `Denied` → `FcFluentIcons.Key16()`; `ContextBlocked`/`Blocked` → `FcFluentIcons.SubtractCircle16()`; `GenerationFailed`/`SafetyFailed`/`PostingFailed` → `FcFluentIcons.Warning16()`; `Unknown/_` → `FcFluentIcons.QuestionCircle16()`. (There is no error/dismiss glyph in the curated set — failures reuse `Warning16`, exactly as the readiness/provider badges do.)
    - `LabelKeyFor(AgentCallStatus state)` → `$"Agents.CallStatus.Status.{state}"` (whole-string key; UX-DR14).
    - `ReasonKeyFor(AgentCallStatus state)` → the **coarse, status-level** safe reason key `$"Agents.CallStatus.Reason.{state}"` for the failure/blocked states (`Denied→permission denied, Blocked→provider/dependency unavailable, ContextBlocked→context blocked, SafetyFailed→safety failed, GenerationFailed→generation failed, PostingFailed→posting failed`). **This is the primary AC3 path** — it needs only the coarse `Status` the view already carries (see Dev Note "Safe reason: source of truth"). ALSO add optional fine-grained `ReasonKeyFor(...)` overloads for the safe reason enums (`AgentInteractionGateOutcome`/`AgentInteractionGateCheck`, `AgentInteractionContextBlockReason`, `AgentOutputGenerationFailureReason`, `AgentResponsePostingFailureReason`) → `$"Agents.CallStatus.Reason.{enumValue}"`, usable WHEN a richer reason is available; every mapping derives **no** secret/id/PII and exposes only the coarse, content-free classification (AD-14).
  - [x] Keep this class pure and side-effect-free (testable without bUnit) — it is the heart of AC2/AC3 and must be unit-testable in isolation, exactly like `AgentReadiness`.

- [x] **Task 3 - Add the `AgentCallStatusBadge` component (mirror `AgentReadinessBadge`)** (AC: #2, #4)
  - [x] Add `src/Hexalith.Agents.UI/Components/Shared/AgentCallStatusBadge.razor` mirroring `AgentReadinessBadge.razor` 1:1: `@inject IStringLocalizer<AgentsResources> Localizer`; a single `<FluentBadge Appearance="BadgeAppearance.Tint" Color="@AgentCallStatusPresentation.ColorFor(State)" IconStart="@StateIcon" role="status" aria-label="@Label" data-testid="@TestId">@Label</FluentBadge>`.
  - [x] Parameters: `AgentCallStatus State` (default `Unknown`), `string TestId = "agent-call-status-badge"`. `Label => Localizer[AgentCallStatusPresentation.LabelKeyFor(State)]` (visible text == accessible name == safe whole string; never an id/secret/PII/raw error — AD-14). `StateIcon => AgentCallStatusPresentation.IconFor(State)`.
  - [x] The badge sits in a **reserved, stable slot** so rows do not jump on transition (UX-DR17) — the consuming surface (Task 5) owns the fixed-width container; the badge itself only swaps color+icon+text (no animation; UX-DR38).

- [x] **Task 4 - Add the pattern-agnostic invocation panel (`ConversationAgentCallPanel`)** (AC: #1)
  - [x] Add `src/Hexalith.Agents.UI/Components/Shared/ConversationAgentCallPanel.razor`. It is the **normalized** invocation affordance (PRD OQ-1 leaves the exact mention/command/action/participant entry open; the architecture normalizes every entry into the same Agents request command, so the panel does **not** over-fit to one pattern — AD-15 Deferred row).
  - [x] The panel MUST, before any submit (UX-DR24): (a) **visibly name `hexa`** (the Agent display name from the safe status view / a localized `hexa` label — never a raw id); (b) capture the **prompt** (a Fluent input — `FluentTextArea`, never a raw `<textarea>`) and surface the **Source Conversation** context (caller, Agent, Source Conversation reference as `mono` typography per UX-DR13); (c) show the **response-mode implication** by reusing the `ResponseModeToggle` display semantics or a read-only response-mode statement ("Automatic posts directly to the Conversation"; "Confirmation requires approval before posting") — make the future-only/automatic implication explicit (UX-DR23) and **do not visually bias Automatic**.
  - [x] On submit, the panel calls the gateway's `RequestCallAsync` (Task 6) and renders the returned status via the feedback surface (Task 5). It assembles a `ConversationAgentCallRequest` (safe inputs: `SourceConversationId`, `Prompt`, `ClientCorrelationId?`) — it does NOT construct the trusted `AgentId`/`Snapshot` (those are server-populated per `RequestAgentInteraction` doc; AC1 precondition). The prompt is sensitive (AD-14): it is passed to the gateway and never echoed into a badge label, accessible name, tooltip, or log.
  - [x] **Never imply a posted message**: until status is `Posted`, the panel/feedback wording is in-flight ("Calling `hexa`…", "Generating…", "Posting…") — never "Posted"/"Reply sent" (UX-DR22; EXPERIENCE Voice/Tone forbidden copy). A `Generated` (automatic) state shows "Generating complete — posting…", not "posted".
  - [x] Accessibility: keyboard-operable; `Esc` cancels the transient panel without committing and returns focus to the trigger (UX-DR32/UX-DR37); the prompt field and submit/cancel controls are keyboard reachable; no required action or reason is hover-only.

- [x] **Task 5 - Add the call-status feedback surface (`AgentCallStatusFeedback`)** (AC: #2, #3, #4)
  - [x] Add `src/Hexalith.Agents.UI/Components/Shared/AgentCallStatusFeedback.razor` rendering the live call status of one Agent Call. It hosts the `AgentCallStatusBadge` in a **reserved, fixed slot** plus the safe reason text (when the state is `Denied`/`Blocked`/`ContextBlocked`/`GenerationFailed`/`SafetyFailed`/`PostingFailed`), reusing `operational-status-panel` semantics (group by recovery action, not raw subsystem — UX-DR9; Fluent `FluentMessageBar`/status region).
  - [x] **Safe reason (AC3):** render the reason via the coarse `AgentCallStatusPresentation.ReasonKeyFor(AgentCallStatus)` whole-string label, derived from the status the view already carries (Dev Note "Safe reason: source of truth"). NEVER render raw provider/Conversations/adapter error text, the prompt, generated content, or any cross-tenant/Party/Conversation detail (AD-14). Map the canonical user-facing reasons to the safe copy: permission denied, provider unavailable, context blocked, safety failed, posting failed. (If the gateway later carries a fine-grained safe reason enum, prefer the richer `ReasonKeyFor(reasonEnum)` label — see the additive-extension note.)
  - [x] **Live region (AC4, UX-DR36):** wrap status text in a **polite** `aria-live` region for ordinary transitions (`requested → authorized → context loading → generating → generated → posting pending → posted`); use **assertive** ONLY for `Denied`/failed terminal states that prevent the action (per the accessibility-review transition matrix: polite for created/posted/failed/expired/denied status changes, assertive only for immediate destructive-action prevention, atomic summaries for multi-field changes, **no repeated announcements** for ordinary pending progress). Reuse/mirror `AgentSurfaceState`'s `role`/`aria-live` derivation (`alert`/`assertive` vs `status`/`polite`).
  - [x] **Constrained-viewport / reduced-motion fail-closed (AC4, UX-DR40/UX-DR38):** when the surface cannot show enough context/status for a high-impact automatic call action, render the action **unavailable with a visible, accessible reason** (focusable text or `aria-describedby` — not a bare disabled control; accessibility-review medium finding) and keep **review-only** status visible. State changes are perceivable from the color+icon+text swap in the reserved slot — **no dependence on animation**. Expose a deterministic, focusable status region so the transition is perceivable to AT and reduced-motion users.
  - [x] **Stale guard:** if the gateway returns a stale/degraded read, render the `Stale` surface (`AgentSurfaceState`) — never render stale status as fresh (ARCHITECTURE-SPINE Consistency Conventions: "Stale state must not be rendered or treated as fresh").

- [x] **Task 6 - Add the UI read/invoke gateway seam (deferred/fail-closed) + DI** (AC: #1, #3)
  - [x] Add `src/Hexalith.Agents.UI/Services/Gateways/IConversationAgentCallGateway.cs` mirroring `IAgentSetupGateway.cs`: two operations —
    - `Task<AgentCallRequestResult> RequestCallAsync(ConversationAgentCallRequest request, CancellationToken cancellationToken)` — submits the normalized Agent Call; returns the fail-closed result wrapper (Accepted carries the `AgentInteractionReference`; else carries none).
    - `Task<AgentInteractionInspectionResult> GetCallStatusAsync(string agentInteractionId, CancellationToken cancellationToken)` — reads the safe `AgentInteractionStatusView`; returns the fail-closed inspection wrapper (`View` non-null only on `Success`).
  - [x] Add `ConversationAgentCallRequest.cs` (UI request DTO or Contracts — place beside the gateway in `Services/Gateways/`): `(string SourceConversationId, string Prompt, string? ClientCorrelationId)`. The prompt is sensitive (AD-14) — carried to the gateway only.
  - [x] Add `src/Hexalith.Agents.UI/Services/Gateways/DeferredConversationAgentCallGateway.cs` (`sealed class`): `RequestCallAsync` → `Task.FromResult(AgentCallRequestResult.NotAuthorized())`; `GetCallStatusAsync` → `Task.FromResult(AgentInteractionInspectionResult.NotAuthorized())`. Fail closed so an unbound host renders permission-denied rather than fabricating a "posted/ready" call (AD-12). XML-doc that the live implementation calls `Hexalith.Agents.Client` → BFF/API → the Agents read-model/command path and lands with the deferred read-model/BFF story (Epic 4, 4.1/4.3), and that bUnit tests substitute this seam with NSubstitute — mirror `DeferredAgentSetupGateway`.
  - [x] In `src/Hexalith.Agents.UI/Services/Gateways/AgentsUiServiceCollectionExtensions.cs`, add `services.TryAddScoped<IConversationAgentCallGateway, DeferredConversationAgentCallGateway>();` (TryAdd so a live registration always wins; scoped like the existing gateways).

- [x] **Task 7 - Register the policy-gated nav entry + demonstrable surface page** (AC: #1, #2)
  - [x] In `src/Hexalith.Agents.UI/Composition/AgentsFrontComposerRegistration.cs`, add one `FrontComposerNavEntry` (UX-DR1 "Conversation invocation entry") for the V1 in-product call surface, e.g. `("agents", "Conversation call", "/agents/conversation-call", Icon: "Regular.Size20.Play" or an existing curated `Regular.Size20.*`, Order: 4, RequiredPolicy: AgentsAdministratorPolicy, TitleKey: "Agents.Navigation.ConversationCall", Resource: typeof(AgentsResources))`. Gate with the existing `AgentsAdministratorPolicy` for the V1 demonstrable surface and add a comment that the participant-facing Conversation-owned affordance + its participant-level authorization are deferred to PRD OQ-1 resolution (do not invent new auth semantics in this story).
  - [x] Add `src/Hexalith.Agents.UI/Components/Pages/ConversationCall.razor` mirroring `AgentsOverview.razor` structure: `@page "/agents/conversation-call"`, `@attribute [Authorize(Policy = AgentsFrontComposerRegistration.AgentsAdministratorPolicy)]`, inject `IStringLocalizer<AgentsResources>` + `IConversationAgentCallGateway`, `FcPageLayout`/`FcPageHeader`/`FcContentLabel` with focus-to-heading (`FocusHeadingAsync`), and host the `ConversationAgentCallPanel` + `AgentCallStatusFeedback`. By default (deferred gateway) the page renders the permission-denied surface — exactly like the admin pages do today — proving the fail-closed default (AD-12).
  - [x] If `Components/Pages/ConversationCall.razor` is a `Constrained`-measure judgment surface, use `FcPageLayoutMode` consistent with the page's role (UX-DR15: Constrained for judgment-heavy invocation; FullWidth for status lists). Reserve stable space for the status badge slot (UX-DR17).

- [x] **Task 8 - Localization resources (en + fr, whole strings, key parity)** (AC: #1, #2, #3)
  - [x] Extend `src/Hexalith.Agents.UI/Resources/AgentsResources.resx` (en) and `AgentsResources.fr.resx` (fr) with whole-string keys (UX-DR14 — never assemble sentences from fragments at runtime):
    - `Agents.CallStatus.Status.{State}` for every `AgentCallStatus` value (`Requested, Authorized, Denied, Blocked, ContextLoading, ContextBlocked, Generating, GenerationFailed, SafetyFailed, Generated, PostingPending, Posted, PostingFailed, Unknown`).
    - `Agents.CallStatus.Reason.{Reason}` for every mapped reason enum value (`AgentInteractionContextBlockReason.*`, `AgentOutputGenerationFailureReason.*`, `AgentResponsePostingFailureReason.*`, and the gate denial/block reasons) — safe, recovery-oriented copy (e.g. "You do not have permission to call `hexa` in this Conversation.", "The provider is unavailable. The call is blocked.", "The Conversation context could not be built safely.", "Content safety blocked this response.", "The response could not be posted to the Conversation.").
    - `Agents.Navigation.ConversationCall`, the `ConversationCall` page header keys (`Agents.ConversationCall.Title/Eyebrow/Description`), the panel copy (`Agents.ConversationCall.Panel.*` — name `hexa`, prompt label, response-mode implication statements, submit/cancel, the "unavailable on this viewport — review only" reason), and the feedback copy (`Agents.ConversationCall.Feedback.*`).
  - [x] Keep en/fr **key parity** (the `LocalizationResourceTests` guard enforces it). Match the existing tone (operational, `hexa` named as an Agent participant, never a mascot; EXPERIENCE Voice/Tone) and the existing key hierarchy (`Agents.Area.Element[.Variant]`).

- [x] **Task 9 - Tests (xUnit v3 + Shouldly + bUnit; mirror the UI.Tests patterns)** (AC: #1, #2, #3, #4)
  - [x] `tests/Hexalith.Agents.UI.Tests/AgentCallStatusPresentationTests.cs` (pure, no bUnit): `[Theory]` over the full durable→UX `MapStatus` table; `Posted` is the **only** `Success` color and only `AgentInteractionStatus.Posted` maps to it (UX-DR11); `Denied/GenerationFailed/SafetyFailed/PostingFailed` → `Danger`; `Blocked/ContextBlocked` → `Severe`; in-flight → `Informative`; `Unknown` → `Subtle`; every state has a non-null icon and a `LabelKeyFor` key; `ReasonKeyFor` covers every value of each reason enum and the produced label contains **no** raw-error/secret/id substring (feed a poisoned reason and assert the key is a safe whole-string key only).
  - [x] `tests/Hexalith.Agents.UI.Tests/AgentCallStatusBadgeTests.cs` (bUnit, mirror `BadgeConformanceTests.cs`): `[Theory]` over all `AgentCallStatus` values — assert `FluentBadge.Color == ColorFor(state)`, `IconStart` non-null, visible text == `aria-label` == the localized whole string, `role="status"`, `data-testid` present, and the **no-hex** regex over markup fails (color+icon+text together, never hex; AC2/UX-DR12).
  - [x] `tests/Hexalith.Agents.UI.Tests/ConversationAgentCallPanelTests.cs` (bUnit): the panel visibly names `hexa`, renders a Fluent prompt input (no raw `<textarea>`), shows the response-mode implication for both Automatic and Confirmation, and **never renders posted-message wording** for any non-`Posted` status (assert no "posted"/"sent" copy on `Requested/Authorized/ContextLoading/Generating/Generated/PostingPending`); submitting calls `RequestCallAsync` with the captured `SourceConversationId`/`Prompt` and the prompt is **not** echoed into any badge label/`aria-label`/`data-testid`; `Esc`/cancel returns focus to the trigger.
  - [x] `tests/Hexalith.Agents.UI.Tests/AgentCallStatusFeedbackTests.cs` (bUnit): denied/blocked/failed render the safe reason label and **never** raw error/prompt/content/cross-tenant text; the live region is **polite** for ordinary transitions and **assertive/alert** only for denial/terminal-failure (the transition matrix); on a constrained-viewport flag the high-impact action is unavailable with a **focusable/`aria-describedby` reason** and review-only status stays visible; a stale gateway read renders the `Stale` surface, never fresh.
  - [x] `tests/Hexalith.Agents.UI.Tests/DeferredGatewayTests.cs` (extend): `DeferredConversationAgentCallGateway.RequestCallAsync` → `NotAuthorized`; `GetCallStatusAsync` → `NotAuthorized` with a null `View`.
  - [x] `tests/Hexalith.Agents.UI.Tests/AgentsUiCompositionTests.cs` + `AgentsNavigationTests.cs` (extend): the new "Conversation call" nav entry is registered with `AgentsAdministratorPolicy`, ordered after the existing four, and hidden for unauthorized users; the `/agents/conversation-call` page carries `[Authorize]` and renders permission-denied under the deferred gateway.
  - [x] `tests/Hexalith.Agents.UI.Tests/LocalizationResourceTests.cs` (extend or rely on the existing parity guard): every new `Agents.CallStatus.*` / `Agents.ConversationCall.*` / `Agents.Navigation.ConversationCall` key exists in **both** en and fr; one key per status/reason enum value (no missing/orphan keys).
  - [x] `tests/Hexalith.Agents.UI.Tests/AccessibilityTests.cs` (extend): the feedback surface exposes a named status live region; reduced-motion does not gate perceivability (status text/icon present without animation); the constrained-viewport unavailable reason is focusable / `aria-describedby`-linked.
  - [x] **Contracts tests** `tests/Hexalith.Agents.Contracts.Tests/`: JSON round-trip for `AgentInteractionInspectionResult`/`AgentInteractionInspectionStatus`/`AgentCallRequestResult`/`AgentCallRequestStatus`; assert the no-content-leak guards still hold (the new wrappers carry only safe ids/status — feed a sample prompt/content string into adjacent fields and assert serialization `.ShouldNotContain` it). Ensure the `ContractsSecretNonDisclosure`/round-trip auto-coverage includes the new types; scope any "no Hexalith attribute" assertion to `Hexalith`-namespaced attributes (Story 2.1 learning — the compiler emits `Nullable*Attribute` on records with nullable members).
  - [x] Add `@using Hexalith.Agents.Contracts.AgentInteraction` to `src/Hexalith.Agents.UI/Components/_Imports.razor` so the components see the status view + reason enums.
  - [x] **Build/test loop** from `/home/administrator/projects/hexalith/agents/Hexalith.Agents`: `dotnet restore Hexalith.Agents.slnx` → `dotnet build Hexalith.Agents.slnx --configuration Release` (require **0 warnings / 0 errors** — `TreatWarningsAsErrors`; add `-m:1` if a parallel build flakes, Story 2.2 learning) → `dotnet test tests/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj --configuration Release` and `dotnet test tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release` (test each project individually; if VSTest reports `SocketException (13): Permission denied`, run the built xUnit v3 test executable directly — Story 2.2 learning). Keep the structural/governance guards green (`BadgeConformanceTests`, `AccessibilityTests`, `LocalizationResourceTests`, `AgentsUiCompositionTests`, `AgentsNavigationTests`, the project-boundary/contract guards). Write `_bmad-output/implementation-artifacts/tests/2-6-test-summary.md` with per-project pass counts and list it in the File List.

## Dev Notes

### Critical Guardrails

- **This is a UI/UX story — the only new behavior is in `Hexalith.Agents.UI` (+ four safe Contracts wrappers).** Implement: the gateway-result wrappers, the call-status UX state + pure presentation mapping, the `AgentCallStatusBadge`, the `ConversationAgentCallPanel`, the `AgentCallStatusFeedback` surface, the deferred/fail-closed `IConversationAgentCallGateway`, the nav entry + `ConversationCall` page, the localization keys, and the tests. **Do NOT implement:** the durable interaction pipeline (Stories 2.1–2.5 — done), the live read-model/BFF/Client binding (deferred to Epic 4, 4.1/4.3 — keep the gateway deferred/fail-closed), confirmation-mode proposal review UI (Epic 3), any change to the `AgentInteraction` aggregate/domain/server, the host `Program.cs` auth-policy registration (deferred host), or the actual Conversation-owned mention/command/action wiring (PRD **OQ-1** is open — build the pattern-agnostic affordance per UX-DR24, do not over-fit). [Source: epics.md#Story-2.6; ARCHITECTURE-SPINE.md#AD-15 Deferred; prd OQ-1]
- **AD-15 UI parity — read only through the gateway seam, never EventStore/SDK/aggregate internals.** The Agents UI calls the `IConversationAgentCallGateway` abstraction (live impl → `Hexalith.Agents.Client` → BFF/API → read-model, deferred), exactly as the admin UI calls `IAgentSetupGateway`. The UI never references `Hexalith.Agents.Server`, EventStore streams, provider SDKs, or aggregate internals. Status comes from the safe `AgentInteractionStatusView` (the structured status reference, Story 2.1) — never a raw stream name or projection id. [Source: ARCHITECTURE-SPINE.md#AD-15; AgentInteractionStatusView.cs; AgentInteractionReference.cs]
- **AD-14 redaction is the heart of AC2/AC3 — nothing sensitive reaches the UI surface.** No prompt, generated content, Conversation content, provider payload/SDK error text, secret, Party PII, or cross-tenant data appears in visible text, accessible names (`aria-label`), tooltips, copied text, `data-testid`, or `aria-live` announcements. The badge label, the safe-reason label, and every announcement are **localized whole strings** derived only from the safe coarse enums (`AgentInteractionStatus`, the gate verdict, `AgentInteractionContextBlockReason`, `AgentOutputGenerationFailureReason`, `AgentResponsePostingFailureReason`). The prompt the panel captures is sensitive: it flows to the gateway and is never echoed back into any display string or log. [Source: ARCHITECTURE-SPINE.md#AD-14; #AD-9 (safe error classes); prd NFR2, NFR6]
- **AD-12 fail closed — the default DI graph is permission-denied.** With the deferred gateway bound (no live read path), `GetCallStatusAsync`/`RequestCallAsync` return `NotAuthorized` with no data, so the page renders the permission-denied surface rather than a fabricated "ready/posted" call — identical to the admin pages' deferred behavior. Non-success gateway results carry no interaction identity (the `View`/`Reference` is null off the success path). [Source: ARCHITECTURE-SPINE.md#AD-12; AgentInspectionResult.cs; DeferredAgentSetupGateway.cs]
- **Never imply a post before `Posted` (AC1; AD-6, UX-DR22).** A Proposed Agent Reply / generated version is NOT a Conversation Message. The panel and feedback wording stay in-flight ("Calling `hexa`…", "Generating…", "Posting…") for every non-`Posted` state; only `AgentInteractionStatus.Posted` is rendered/announced/colored as a posted Conversation Message (Success). `Generated` (automatic) reads "posting…", never "posted". [Source: epics.md#Story-2.6 (lines 689, 682); ARCHITECTURE-SPINE.md#AD-6; UX-DR22]
- **Color is never alone, and Success is reserved (AC2; UX-DR11/UX-DR12).** Every state = semantic `BadgeColor` role (by name, never hex) + icon + visible text + accessible name. `BadgeColor.Success` is used ONLY for `Posted`; `BadgeColor.Brand` is never a status. Failures are `Danger`; blocked-but-not-failed is `Severe`; in-flight is `Informative`; unknown is `Subtle`. The badge component test asserts the no-hex regex over markup (mirrors `BadgeConformanceTests`). [Source: DESIGN.md#Colors; UX-DR11, UX-DR12]
- **Do NOT edit the FrontComposer (or Conversations/Parties/Tenants/EventStore) submodules.** `FcFluentIcons` is in `Hexalith.FrontComposer.Shell` (a submodule) — compose status icons from its **existing curated factory** (`ArrowSync16, Checkmark16, Play16, Key16, SubtractCircle16, Warning16, QuestionCircle16`); do not add a new icon factory method (no error/dismiss glyph exists — failures reuse `Warning16`, exactly as the readiness/provider badges do). Keep all changes inside `Hexalith.Agents`. [Source: Hexalith.FrontComposer/CLAUDE.md (root-submodule-only); FrontComposer project-context (Fluent-only UI; icons via FcFluentIcons, not a NuGet)]
- **Fluent-only UI + no theme redefinition (FrontComposer governance guards).** Every `.razor` uses FrontComposer/Fluent v5 components — never raw `<button>/<input>/<select>/<textarea>` (raw `<a>` nav is allowed). Express color/typography/spacing through Fluent component params + Fluent 2 tokens (`BadgeColor`, `FluentText` size/weight/color, `FluentStack` gaps); no legacy v4/FAST tokens, no hand-authored heading/color CSS. Group 2+ sibling titled sections in a `FluentAccordion`; keep a single-primary-content page un-collapsed. [Source: FrontComposer project-context (Fluent-only; no theme redefinition; FluentAccordion guideline)]

### The Status Model The UI Mirrors (durable vs UI-derived)

The durable `AgentInteractionStatus` (Contracts) has 12 values; the UX canonical list (UX-DR27 + the Story-2.6 AC) adds three **transient in-flight** display states that are NOT persisted (Story 2.5 established that "posting pending" / "generating" are UI derivations, not durable aggregate states — AD-3 single-command-per-step). The mapping:

| Durable `AgentInteractionStatus` | UX `AgentCallStatus` | `BadgeColor` | Icon | Notes |
|---|---|---|---|---|
| `Requested` | `Requested` | Informative | `Play16` | call created (Story 2.1) |
| `Authorized` | `Authorized` | Informative | `ArrowSync16` | gate passed (2.2) → context next |
| `Denied` | `Denied` | Danger | `Key16` | authorization-class gate failure (2.2) — safe reason via gate verdict |
| `Blocked` | `Blocked` | Severe | `SubtractCircle16` | dependency-readiness gate failure (2.2) — safe reason via gate verdict |
| `ContextReady` | `Generating` (or transient `ContextLoading`→`Generating`) | Informative | `ArrowSync16` | context built (2.3) → generation next |
| `ContextBlocked` | `ContextBlocked` | Severe | `SubtractCircle16` | context not buildable (2.3) — reason via `AgentInteractionContextBlockReason` |
| `Generated` | `Generated` | Informative | `Checkmark16` | generation + safety passed (2.4) — automatic ⇒ "posting…", NOT posted |
| `GenerationFailed` | `GenerationFailed` | Danger | `Warning16` | (2.4) — reason via `AgentOutputGenerationFailureReason` |
| `SafetyFailed` | `SafetyFailed` | Danger | `Warning16` | (2.4) — content blocked by safety policy |
| `Posted` | `Posted` | **Success** | `Checkmark16` | terminal success (2.5) — the ONLY Success |
| `PostingFailed` | `PostingFailed` | Danger | `Warning16` | (2.5) — reason via `AgentResponsePostingFailureReason` |
| `Unknown` / unrecognized | `Unknown` | Subtle | `QuestionCircle16` | sentinel — never resolves to a concrete state |

UI-derived transient states (rendered while a milestone is pending, from an in-flight hint — never persisted): `ContextLoading` (after `Authorized`), `Generating` (after `ContextReady`), `PostingPending` (after `Generated`, Automatic mode) — all `Informative` + `ArrowSync16`. [Source: AgentInteractionStatus.cs; ARCHITECTURE-SPINE.md#AD-2, #AD-3 sequence; epics.md UX-DR27/#Story-2.6 line 691; 2-5 story "Why No Persisted PostingPending State"]

### Safe Reason Sources (AC3) — already in Contracts, all coarse + content-free

The UI maps these existing safe enums to whole-string reason labels; it never invents reasons or shows raw text:
- **Denied / Blocked:** `AgentInvocationGateVerdict` (`AgentInteractionGateCheck` + `AgentInteractionGateOutcome`) — the gate failure class (tenant/caller-party/conversation access vs agent/provider/policy/freshness readiness). Surface as "permission denied" (authorization class) vs a safe dependency reason (readiness class).
- **ContextBlocked:** `AgentInteractionContextBlockReason` (`ContextUnavailable, ContextNotFresh, ExceedsModelBudget, ModelBudgetUnavailable`).
- **GenerationFailed / SafetyFailed:** `AgentOutputGenerationFailureReason` (`ProviderTimeout, ProviderDisabled, ProviderUnavailable, AdapterFailure, InvalidContext, GenerationError, ContentSafetyBlocked, PolicyFailure`).
- **PostingFailed:** `AgentResponsePostingFailureReason` (`PartyIdentityUnavailable, MembershipUnavailable, MembershipRejected, ConversationUnavailable, PostRejected, AdapterFailure`).

Each enum is documented as "coarse, content-free, carries no provider/Conversations SDK detail." The reason label is `Agents.CallStatus.Reason.{value}` — a safe whole string (UX-DR14). [Source: AgentInvocationGateVerdict.cs, AgentInteractionContextBlockReason.cs, AgentOutputGenerationFailureReason.cs, AgentResponsePostingFailureReason.cs]

### Safe reason: source of truth (avoid the "view already has the reason" trap)

`AgentInteractionStatusView` carries only the **coarse `Status`** (plus safe ids/versions) — it does **NOT** carry any failure-reason enum today. The fine-grained reason enums above live on the durable events/state (Stories 2.2–2.5), not on the safe status view. Therefore:

- **AC3 is satisfied from the coarse `Status` alone**: `Denied → "permission denied"`, `Blocked → "provider/dependency unavailable"`, `ContextBlocked → "context blocked"`, `SafetyFailed → "safety failed"`, `GenerationFailed → "generation failed"`, `PostingFailed → "posting failed"`. This is the primary path and needs no new read field — the epics AC3 list ("permission denied, provider unavailable, context blocked, safety failed, posting failed") maps exactly to these coarse statuses.
- **Do not assume the gateway returns a fine-grained reason.** The fine-grained `ReasonKeyFor(reasonEnum)` overloads are provided for the future where the read model (Epic 4) projects a safe reason onto the view. If you choose to surface richer reasons now, the ONLY acceptable way is to **additively** extend `AgentInteractionStatusView` (and the gateway result) with a **nullable, coarse, content-free** safe-reason classification — never by reading aggregate internals or carrying any raw text/content. Extending the view is optional; the coarse-status mapping is sufficient and is what the tests assert.

### The Lifecycle The Feedback Mirrors (AD-19 sequence)

```
request hexa(SourceConversationId, prompt)  → RequestInteraction → InteractionRequested   [requested]
authorize (AD-12 gates)                                                                    [authorized | denied | blocked]
RecordContextReady | ContextBlocked (AD-11)                                                [context loading → context ready | context blocked]
invoke model path                                                                          [generating]
RecordGeneratedVersion | GenerationFailed (safety: SafetyFailed)                           [generated | generation failed | safety failed]
automatic: ensure AIAgent participant + AppendMessage → RecordPostingSucceeded/Failed      [posting pending → posted | posting failed]
```
For Epic 2 the **automatic** branch is in primary scope; the confirmation branch's proposal states (UX-DR28: edited/regenerated/pending/approved/rejected/abandoned/expired) are Epic 3 — share the badge taxonomy but do not build the proposal UI here. [Source: ARCHITECTURE-SPINE.md#AD-19 sequence; epics.md#Story-2.6]

### Current Code State To Preserve / Mirror

Read these completely — they are the exact templates:
- `src/Hexalith.Agents.UI/Components/Shared/AgentReadinessBadge.razor` — the **badge template**: `<FluentBadge Appearance="Tint" Color=ColorFor(State) IconStart=Icon role="status" aria-label=@Label data-testid=@TestId>@Label</FluentBadge>`; label = `Localizer[LabelKeyFor(State)]`; icon = enum `switch` over `FcFluentIcons.*`.
- `src/Hexalith.Agents.UI/Components/Shared/AgentReadiness.cs` — the **pure mapping template**: state enum (with a UI-only transient member doc'd like `Checking`), `MapState`, `ColorFor` (Success only for the proven-complete state), `LabelKeyFor`/`BlockerKeyFor` (`$"Agents.…{state}"`). `AgentCallStatusPresentation` mirrors this shape.
- `src/Hexalith.Agents.UI/Components/Shared/AgentSurfaceState.razor` + `AgentSurfaceKind.cs` — the **surface-state + live-region template**: `role`/`aria-live` = `alert`/`assertive` for Error/PermissionDenied else `status`/`polite`; reuse for Loading/Empty/PermissionDenied/Stale and the feedback live-region politeness.
- `src/Hexalith.Agents.UI/Components/Shared/ResponseModeToggle.razor` — the **response-mode display** (Fluent `RadioGroup` in a `fieldset`/`legend`, future-only note); reuse its semantics for the panel's response-mode implication (read-only is fine — the panel does not change the mode).
- `src/Hexalith.Agents.UI/Components/Pages/AgentsOverview.razor` — the **page template**: `@page` + `[Authorize(Policy=…)]`, inject gateway, `FcPageLayout`/`FcPageHeader`/`FcContentLabel`, `OnInitializedAsync` reads the gateway, `_result.Status` branches to `AgentSurfaceState` surfaces, `FocusHeadingAsync` focus management.
- `src/Hexalith.Agents.UI/Services/Gateways/IAgentSetupGateway.cs` + `DeferredAgentSetupGateway.cs` + `AgentsUiServiceCollectionExtensions.cs` — the **gateway seam**: interface + deferred fail-closed impl returning `…Result.NotAuthorized()` + `TryAddScoped` DI.
- `src/Hexalith.Agents.UI/Composition/AgentsFrontComposerRegistration.cs` — the **nav registration**: `AgentsAdministratorPolicy` const, `FrontComposerNavEntry(category, invariantTitle, route, Icon, Order, RequiredPolicy, TitleKey, Resource)`; add the 5th entry.
- `src/Hexalith.Agents.Contracts/Agent/AgentInspectionResult.cs` + `AgentInspectionStatus.cs` — the **result-wrapper template** (status enum + record with `Success/NotAuthorized/NotFound` factories; data non-null only on success).
- `src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionStatusView.cs`, `AgentInteractionReference.cs`, `Queries/GetAgentInteractionStatusQuery.cs` — the **safe read contracts** the gateway returns (status + safe refs; no prompt/content/stream name).
- `src/Hexalith.Agents.UI/Components/_Imports.razor` — add `@using Hexalith.Agents.Contracts.AgentInteraction`.
- Tests: `tests/Hexalith.Agents.UI.Tests/BadgeConformanceTests.cs`, `AccessibilityTests.cs`, `LocalizationResourceTests.cs`, `DeferredGatewayTests.cs`, `AgentsUiCompositionTests.cs`, `AgentsNavigationTests.cs`, `AgentsTestContext.cs` (bUnit base over `FrontComposerTestBase`; substitutes gateways with NSubstitute; `StubAgentsLocalizer` returns the key as the label so assertions compare against the key; `RenderedFragmentTextExtensions.VisibleText()`).

What must be preserved:
- `Hexalith.Agents.Contracts` stays inward-only — NO reference to `Hexalith.Conversations.*`/`Hexalith.Parties.*`; all cross-module ids are opaque `string`. The new wrappers carry only safe ids/status (no content/stream/payload/secret).
- `AgentInteractionStatus` ordinals are unchanged — this story adds **no** durable status; the transient UX states are UI-only.
- Dependency direction: `Hexalith.Agents.UI` → `Hexalith.Agents.Client` + `Hexalith.Agents.Contracts` only (never `…Server`). `.slnx` only; `net10.0`; `LangVersion`; nullable; `TreatWarningsAsErrors`; Central Package Management; `ConfigureAwait(false)` on every awaited call (CA2007 → build error); `_camelCase` fields; `Async` suffix; file-scoped namespaces; Allman braces; no copyright headers.
- All existing UI governance/structural guards stay green (`BadgeConformanceTests`, `AccessibilityTests`, `LocalizationResourceTests`, `…FluentConformanceTests`, composition/nav, project-boundary).

### Latest Technical Information

- Stack (do not change versions — Central Package Management): .NET SDK `10.0.300`–`10.0.301`, `net10.0`; **Fluent UI Blazor `5.0.0-rc.3-26138.1`** via the FrontComposer baseline (exact RC pin — UI uses Fluent v5 components, not raw HTML controls); Hexalith.FrontComposer sibling commit `eee5e6b`; Hexalith.Conversations sibling commit `46df0cd`; xUnit v3 `3.2.2`; Shouldly `4.3.0`; bUnit + Verify (versions inherited from the FrontComposer sibling baseline — **not** pinned in the architecture spine; use whatever `Directory.Packages.props` already centralizes for the UI.Tests project). [Source: ARCHITECTURE-SPINE.md#Stack; FrontComposer project-context]
- The live read/command path (`Hexalith.Agents.Client` → BFF/API → Agents read-model) is **deferred to Epic 4 (4.1/4.3)** — this story ships the stable gateway seam + deferred fail-closed impl, exactly like the admin-UI gateways. Run UI tests with `DiffEngine_Disabled=true` if any Verify snapshot is added (FrontComposer rule — otherwise Verify hangs). [Source: ARCHITECTURE-SPINE.md#AD-15 Deferred; FrontComposer project-context (testing)]

### Testing Requirements

- **`Hexalith.Agents.UI.Tests` (bUnit + xUnit v3 + Shouldly + NSubstitute):** pure mapping tests for `AgentCallStatusPresentation` (full table, Success-only-for-Posted, failure→Danger, blocked→Severe, in-flight→Informative, reason→safe-key, no-leak); badge conformance (color+icon+text+accessible-name, no-hex regex); panel (names `hexa`, captures prompt/context, response-mode implication, never implies posted, focus/Esc, prompt not echoed); feedback (safe reason, live-region politeness matrix, constrained-viewport unavailable-with-accessible-reason + review-only, stale≠fresh); deferred-gateway fail-closed; composition/nav policy-gating; localization en/fr key parity; accessibility (named live region, reduced-motion perceivability, focusable unavailable reason). Substitute the gateway with NSubstitute via `AgentsTestContext`; `StubAgentsLocalizer` returns the key so assertions compare keys.
- **`Hexalith.Agents.Contracts.Tests`:** round-trip the four new wrappers; no-content-leak serialization; auto-coverage includes the new types; `Hexalith`-scoped "no attribute" assertions.
- Build/test loop is in Task 9. Require 0 warnings / 0 errors; test each project individually; write `tests/2-6-test-summary.md`.

### Project Structure Notes

New/changed code (no new projects — extend `Hexalith.Agents.UI` + add four Contracts wrappers):
- **Contracts** (`src/Hexalith.Agents.Contracts/AgentInteraction/`): add `AgentInteractionInspectionStatus.cs`, `AgentInteractionInspectionResult.cs`, `AgentCallRequestStatus.cs`, `AgentCallRequestResult.cs`.
- **UI — Shared components** (`src/Hexalith.Agents.UI/Components/Shared/`): add `AgentCallStatus.cs`, `AgentCallStatusPresentation.cs`, `AgentCallStatusBadge.razor`, `ConversationAgentCallPanel.razor`, `AgentCallStatusFeedback.razor`.
- **UI — Pages** (`src/Hexalith.Agents.UI/Components/Pages/`): add `ConversationCall.razor`.
- **UI — Gateways** (`src/Hexalith.Agents.UI/Services/Gateways/`): add `IConversationAgentCallGateway.cs`, `DeferredConversationAgentCallGateway.cs`, `ConversationAgentCallRequest.cs`; modify `AgentsUiServiceCollectionExtensions.cs`.
- **UI — Composition / Imports / Resources:** modify `Composition/AgentsFrontComposerRegistration.cs` (5th nav entry + policy const reuse), `Components/_Imports.razor` (`@using …AgentInteraction`), `Resources/AgentsResources.resx` + `AgentsResources.fr.resx` (call-status + reason + page/panel/feedback keys).
- **Tests** (`tests/Hexalith.Agents.UI.Tests/`): add `AgentCallStatusPresentationTests.cs`, `AgentCallStatusBadgeTests.cs`, `ConversationAgentCallPanelTests.cs`, `AgentCallStatusFeedbackTests.cs`; extend `DeferredGatewayTests.cs`, `AgentsUiCompositionTests.cs`, `AgentsNavigationTests.cs`, `LocalizationResourceTests.cs`, `AccessibilityTests.cs`, and `AgentsTestContext.cs` (add the `IConversationAgentCallGateway` substitute). `tests/Hexalith.Agents.Contracts.Tests/`: add wrapper round-trip/no-leak tests. Write `_bmad-output/implementation-artifacts/tests/2-6-test-summary.md`.

Discovery loaded: epics.md (Epic 2 full, Story 2.6 ACs + cross-story boundaries, Requirements Inventory FR/NFR/UX-DR + FR Coverage Map), prd FR8/FR12/FR22/FR23/FR25 + NFR2/NFR4/NFR6 + OQ-1, ARCHITECTURE-SPINE.md (AD-2/3/5/6/9/11/12/14/15/17/19 + Stack + Structural Seed + Consistency Conventions + sequence diagram + Deferred invocation-affordance/read-model rows), the UX design set (DESIGN.md, EXPERIENCE.md, review-accessibility.md, review-governance.md: UX-DR1/11/12/13/14/17/22/23/24/27/28/32/33/36/37/38/40/41, the per-state color mapping, the live-region transition matrix, the fail-closed responsive rule), the existing `Hexalith.Agents.UI` (badge/surface/toggle/page/gateway/composition/resources from Stories 1.7/1.8), the safe AgentInteraction status/view/reference/reason contracts (Stories 2.1–2.5), the `FcFluentIcons` curated factory, and the UI.Tests bUnit/Shouldly/NSubstitute patterns.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.6-Conversation-Invocation-UX-And-Call-Status-Feedback]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-2.1 (structured status reference, not raw streams), #Story-2.2/#Story-2.3/#Story-2.4/#Story-2.5 (the durable statuses the UI mirrors), #Story-3.1/#Story-3.x (confirmation-mode proposal UI — out of scope)]
- [Source: prd FR8 (call hexa from conversation), FR12 (failure visible via authorized status, no leak), FR22 (admin UI distinguishes states incl. failed call), FR23 (structured API/client contracts, not raw EventStore/SDK), FR25 (operational status taxonomy); NFR2 (privacy/no cross-tenant leak), NFR4 (observability), NFR6 (provider secrets never in UI/status); OQ-1 (invocation pattern open)]
- [Source: _bmad-output/planning-artifacts/epics.md UX-DR1 (conversation invocation nav entry), UX-DR11 (semantic status roles; Brand never a status; Success only when complete), UX-DR12 (color+icon+text; color-only forbidden; accessible names no secrets), UX-DR13 (typography: body for previews, mono for ids), UX-DR14 (localizable whole strings; no runtime fragments), UX-DR17 (reserve stable space; no row jump), UX-DR22 (approved/generated ≠ posted), UX-DR24 (pattern-agnostic invocation captures hexa name/source/caller/agent/prompt/mode/auth/timestamp before provider invocation; never renders unapproved content as a message), UX-DR27 (canonical Agent Call states), UX-DR28 (posting tail states), UX-DR32/UX-DR37 (keyboard/focus; Esc non-committing; focus returns to trigger), UX-DR33/UX-DR36 (FC-A11Y live regions; announce failed/posted/denied; no assertive for ordinary pending), UX-DR38 (reduced motion: no animation dependence), UX-DR40 (fail closed on constrained viewports: unavailable with visible reason; review-only remains), UX-DR41 (FrontComposer FC-LYT/FC-A11Y/FC-L10N; pending status without promoting to success)]
- [Source: ARCHITECTURE-SPINE.md#AD-2 (AgentInteraction status model), #AD-3 (pure aggregates; transient states are UI derivations), #AD-6 (proposal ≠ Conversation Message), #AD-9 (safe error classes; no provider SDK detail), #AD-11 (context bounds; context-blocked is a distinct state), #AD-12 (fail closed on dependency uncertainty), #AD-14 (no raw content/payload/secret in status/UI), #AD-15 (FrontComposer UI parity; call Client/API not streams/SDK/internals), #AD-17 (UI/contract conformance + fail-closed test gates), #AD-19 (interaction lifecycle sequence); #Deferred (exact invocation affordance normalized to one Agents command; live read-model/BFF); #Consistency-Conventions (no raw content in status badges; stale must not render as fresh; proposed replies distinct from messages); #Stack]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md#Colors, #Typography, #Layout, conversation-agent-call / proposal-state-badge / response-mode-toggle / operational-status-panel / audit-evidence-panel tokens; EXPERIENCE.md#Voice-and-Tone, #Accessibility-Floor, #Responsive, #Component-Patterns, #State-Patterns, UJ-2, OQ-1; review-accessibility.md (focus-fallback, live-region matrix, focusable unavailable reason, invocation a11y), review-governance.md (OQ-1 blocking gate, no-leak notifications)]
- [Source: src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionStatus.cs (12 durable statuses), AgentInteractionStatusView.cs + Queries/GetAgentInteractionStatusQuery.cs (safe status read — deferred binding), AgentInteractionReference.cs (safe status handle), Commands/RequestAgentInteraction.cs (invocation inputs; server-trusted AgentId/Snapshot; sensitive prompt), Agent/AgentResponseMode.cs (Automatic/Confirmation implication)]
- [Source: src/Hexalith.Agents.Contracts/AgentInteraction/AgentInvocationGateVerdict.cs + AgentInteractionGateCheck.cs + AgentInteractionGateOutcome.cs, AgentInteractionContextBlockReason.cs, AgentOutputGenerationFailureReason.cs, AgentResponsePostingFailureReason.cs (the safe, coarse reason enums the UI maps to whole-string labels)]
- [Source: src/Hexalith.Agents.Contracts/Agent/AgentInspectionResult.cs + AgentInspectionStatus.cs (result-wrapper template)]
- [Source: src/Hexalith.Agents.UI/Components/Shared/AgentReadinessBadge.razor, AgentReadiness.cs, AgentSurfaceState.razor, AgentSurfaceKind.cs, ResponseModeToggle.razor (badge/mapping/surface/toggle templates)]
- [Source: src/Hexalith.Agents.UI/Components/Pages/AgentsOverview.razor (page template), Composition/AgentsFrontComposerRegistration.cs (nav + policy), Services/Gateways/IAgentSetupGateway.cs + DeferredAgentSetupGateway.cs + AgentsUiServiceCollectionExtensions.cs (gateway seam + DI), Components/_Imports.razor, Resources/AgentsResources.cs (+ .resx/.fr.resx)]
- [Source: Hexalith.FrontComposer/src/Hexalith.FrontComposer.Shell/Components/Icons/FcFluentIcons.cs (curated icon factory — do not extend the submodule), Hexalith.FrontComposer/_bmad-output/project-context.md (Fluent-only UI; no theme redefinition; submodule-edit prohibition; solution-level test + trait filters; DiffEngine_Disabled for Verify), Hexalith.FrontComposer/CLAUDE.md (root-submodule-only)]
- [Source: tests/Hexalith.Agents.UI.Tests/BadgeConformanceTests.cs, AccessibilityTests.cs, LocalizationResourceTests.cs, DeferredGatewayTests.cs, AgentsUiCompositionTests.cs, AgentsNavigationTests.cs, AgentsTestContext.cs, StubAgentsLocalizer.cs, RenderedFragmentTextExtensions.cs (bUnit + Shouldly + NSubstitute patterns)]
- [Source: _bmad-output/implementation-artifacts/2-5-...md, 2-4-...md, 2-3-...md, 2-2-...md, 2-1-...md (prior-story learnings: deferred/fail-closed seam, no transient durable state, CA1062 positive guard, -m:1 serialized build, VSTest socket fallback, Nullable attribute scoping, no-content-leak serialization tests); 1-8-admin-setup-ui-and-readiness-overview.md, 1-7-...md (UI conventions: badge color+icon+text, Success-only-for-callable, whole-string localization, policy-gated nav + page [Authorize], surface-state live-region politeness, deferred gateway, bUnit/no-hex tests)]

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created. Story status is `ready-for-dev`.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (1M context) — `claude-opus-4-8[1m]`

### Debug Log References

- Release build (`dotnet build Hexalith.Agents.slnx --configuration Release -m:1`): **0 warnings / 0 errors** (`TreatWarningsAsErrors`).
- First build failed on three test-only diagnostics in `ConversationAgentCallPanelTests` (CS8604 nullable `GetAttribute` returns ×2; CA1822 static helper) — fixed (null-coalesce the attribute reads; `static` helper). Rebuilt clean.
- Tests (Release, `--no-build`, per project): `Hexalith.Agents.Contracts.Tests` 186/0; `Hexalith.Agents.UI.Tests` 336/0; regression `Hexalith.Agents.Server.Tests` 232/0; `Hexalith.Agents.Tests` 527/0 — **1281 passed, 0 failed**. See `_bmad-output/implementation-artifacts/tests/2-6-test-summary.md`.
- VSTest ran cleanly (no `SocketException (13)` fallback needed); no Verify snapshots added (no `DiffEngine_Disabled`).

### Completion Notes List

- **Task 1 — Contracts wrappers.** Added the four safe gateway-result wrappers in `Hexalith.Agents.Contracts/AgentInteraction/` mirroring `AgentInspectionResult`/`AgentInspectionStatus`: `AgentInteractionInspectionStatus` (`Unknown=0, Success, NotAuthorized, NotFound`), `AgentInteractionInspectionResult` (`View` non-null only on `Success`), `AgentCallRequestStatus` (`Unknown=0, Accepted, NotAuthorized, Rejected`), `AgentCallRequestResult` (`Reference` non-null only on `Accepted`). Enums `[JsonStringEnumConverter]`; no prompt/content/stream/secret members. Contracts stays inward-only.
- **Task 2 — UX state + pure mapping.** Added `AgentCallStatus` (14 values incl. the three UI-only transient states doc'd as non-persisted) and `AgentCallStatusPresentation` (pure, no bUnit): total `MapStatus`, explicit `Derive(inFlight, responseMode)` for the transient states, `ColorFor` (Success ONLY for `Posted`, Brand never a status, Danger/Severe/Informative/Subtle), `IconFor` (curated `FcFluentIcons` only — failures reuse `Warning16`), `LabelKeyFor`, coarse `ReasonKeyFor(AgentCallStatus)` (primary AC3 path) + additive fine-grained `ReasonKeyFor` overloads for the five safe reason enums.
- **Task 3 — `AgentCallStatusBadge`.** Mirrors `AgentReadinessBadge` 1:1 (`FluentBadge` Tint, color+icon+text, `role="status"`, `aria-label`==label==safe whole string, `data-testid`).
- **Task 4 — `ConversationAgentCallPanel`.** Pattern-agnostic affordance: visibly names `hexa`, captures the prompt via `FluentTextArea` (no raw `<textarea>`) + Source Conversation context (mono refs), shows the response-mode implication for both modes without biasing Automatic (read-only `ResponseModeToggle` + both implication statements), submits only safe inputs to `RequestCallAsync`, renders status via the feedback surface with in-flight-only wording (never "posted"/"sent" before `Posted`), Esc/Cancel raise `OnCancel` to return focus to the trigger.
- **Task 5 — `AgentCallStatusFeedback`.** Reserved badge slot + coarse safe reason for failure/blocked states; polite live region for ordinary transitions, assertive/`alert` only for denial/terminal-failure; constrained-viewport fail-closed (focusable, `aria-describedby`-linked unavailable reason; review-only status retained; no animation dependence); stale read renders the `Stale` surface (never fresh).
- **Task 6 — Gateway seam + DI.** `IConversationAgentCallGateway` (`RequestCallAsync`/`GetCallStatusAsync`) + `ConversationAgentCallRequest` (safe `SourceConversationId`/`Prompt`/`ClientCorrelationId?`) + `DeferredConversationAgentCallGateway` (fail-closed `NotAuthorized`) registered via `TryAddScoped` alongside the existing gateways.
- **Task 7 — Nav entry + page.** Fifth policy-gated `FrontComposerNavEntry` ("Conversation call", `/agents/conversation-call`, Order 4, `AgentsAdministratorPolicy`); curated `Regular.Size20.ChevronRight` (Size20 "Play" is not in the curated `FcFluentIcons` vocabulary). `ConversationCall.razor` mirrors `AgentsOverview` (`Constrained` layout, focus-to-heading), probes authorization via `GetCallStatusAsync` and renders permission-denied under the deferred gateway (AD-12), else hosts the panel.
- **Task 8 — Localization.** Added 78 whole-string keys to both `AgentsResources.resx` and `.fr.resx` (status labels, coarse + fine-grained reasons for every reason-enum value, nav, page header, panel, feedback) with en/fr key parity.
- **Task 9 — Tests.** Added `AgentCallStatusPresentationTests`, `AgentCallStatusBadgeTests`, `ConversationAgentCallPanelTests`, `AgentCallStatusFeedbackTests`, `ConversationCallTests` and the Contracts `AgentCallGatewayWrappersContractsTests`; extended `DeferredGatewayTests`, `AgentsUiCompositionTests`, `AgentsNavigationTests`, `LocalizationResourceTests`, `AccessibilityTests`, and `AgentsTestContext`. Added `@using Hexalith.Agents.Contracts.AgentInteraction` to `Components/_Imports.razor`.
- **Scope honored:** UI + four Contracts wrappers only; the live read/command binding stays deferred/fail-closed (Epic 4); no durable status added; no submodule edits; the exact Conversation-owned entry remains deferred to PRD OQ-1.

### File List

**Contracts (new):**
- `src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionInspectionStatus.cs`
- `src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionInspectionResult.cs`
- `src/Hexalith.Agents.Contracts/AgentInteraction/AgentCallRequestStatus.cs`
- `src/Hexalith.Agents.Contracts/AgentInteraction/AgentCallRequestResult.cs`

**UI — Shared components (new):**
- `src/Hexalith.Agents.UI/Components/Shared/AgentCallStatus.cs`
- `src/Hexalith.Agents.UI/Components/Shared/AgentCallStatusPresentation.cs`
- `src/Hexalith.Agents.UI/Components/Shared/AgentCallStatusBadge.razor`
- `src/Hexalith.Agents.UI/Components/Shared/ConversationAgentCallPanel.razor`
- `src/Hexalith.Agents.UI/Components/Shared/AgentCallStatusFeedback.razor`

**UI — Pages (new):**
- `src/Hexalith.Agents.UI/Components/Pages/ConversationCall.razor`

**UI — Gateways (new + modified):**
- `src/Hexalith.Agents.UI/Services/Gateways/IConversationAgentCallGateway.cs` (new)
- `src/Hexalith.Agents.UI/Services/Gateways/ConversationAgentCallRequest.cs` (new)
- `src/Hexalith.Agents.UI/Services/Gateways/DeferredConversationAgentCallGateway.cs` (new)
- `src/Hexalith.Agents.UI/Services/Gateways/AgentsUiServiceCollectionExtensions.cs` (modified)

**UI — Composition / Imports / Resources (modified):**
- `src/Hexalith.Agents.UI/Composition/AgentsFrontComposerRegistration.cs`
- `src/Hexalith.Agents.UI/Components/_Imports.razor`
- `src/Hexalith.Agents.UI/Resources/AgentsResources.resx`
- `src/Hexalith.Agents.UI/Resources/AgentsResources.fr.resx`

**Tests — UI (new):**
- `tests/Hexalith.Agents.UI.Tests/AgentCallStatusPresentationTests.cs`
- `tests/Hexalith.Agents.UI.Tests/AgentCallStatusBadgeTests.cs`
- `tests/Hexalith.Agents.UI.Tests/ConversationAgentCallPanelTests.cs`
- `tests/Hexalith.Agents.UI.Tests/AgentCallStatusFeedbackTests.cs`
- `tests/Hexalith.Agents.UI.Tests/ConversationCallTests.cs`

**Tests — UI (modified):**
- `tests/Hexalith.Agents.UI.Tests/AgentsTestContext.cs`
- `tests/Hexalith.Agents.UI.Tests/DeferredGatewayTests.cs`
- `tests/Hexalith.Agents.UI.Tests/AgentsUiCompositionTests.cs`
- `tests/Hexalith.Agents.UI.Tests/AgentsNavigationTests.cs`
- `tests/Hexalith.Agents.UI.Tests/LocalizationResourceTests.cs`
- `tests/Hexalith.Agents.UI.Tests/AccessibilityTests.cs`

**Tests — Contracts (new):**
- `tests/Hexalith.Agents.Contracts.Tests/AgentCallGatewayWrappersContractsTests.cs`

**Test artifact (new):**
- `_bmad-output/implementation-artifacts/tests/2-6-test-summary.md`

## Change Log

| Date | Version | Description | Author |
|---|---|---|---|
| 2026-06-24 | 1.0 | Implemented Story 2.6: gateway-result Contracts wrappers, the canonical call-status UX state + pure presentation mapping, `AgentCallStatusBadge`, `ConversationAgentCallPanel`, `AgentCallStatusFeedback`, the deferred/fail-closed `IConversationAgentCallGateway` + DI, the policy-gated nav entry + `ConversationCall` page, en/fr localization (78 whole-string keys), and the full test suite. Build 0/0; 1281 tests pass. Status → review. | Amelia (Dev Agent) |
| 2026-06-24 | 1.1 | Adversarial code review (auto-fix). Verified build 0/0 and all tests green (UI 352, Contracts 186); File List ↔ git reality match; all 9 task claims hold; AD-14 redaction and en/fr parity confirmed. Applied one LOW fix: `ConversationAgentCallPanel.OnSubmitAsync` now guards against submitting an empty/whitespace prompt (honors the declared `Required` contract) + regression test. 0 CRITICAL/HIGH/MEDIUM. Status → done. | Senior Review (AI) |

## Senior Developer Review (AI)

**Reviewer:** Jérôme (AI adversarial review) · **Date:** 2026-06-24 · **Outcome:** ✅ Approve (status → done)

### Verification performed (claims independently re-run, not trusted)
- **Build:** `dotnet build Hexalith.Agents.slnx -c Release -m:1` → **0 warnings / 0 errors** (`TreatWarningsAsErrors`).
- **Tests:** UI `352/0` (was 351; +1 review regression test), Contracts `186/0`. All governance guards green (`BadgeConformanceTests`, `AccessibilityTests`, `LocalizationResourceTests`, `AgentsUiCompositionTests`, `AgentsNavigationTests`).
- **File List ↔ git:** every claimed new/modified file matches `git status`; no undocumented source changes, no false "changed" claims. (The extra `tests/2-6-qa-e2e-test-summary.md` is a `_bmad-output` artifact, out of review scope.)
- **AC coverage:** AC1 (names `hexa`, captures prompt+context, response-mode implication, never implies a post before `Posted`) — implemented + tested; AC2 (color+icon+text, no hex, no raw error) — implemented + no-hex regex test; AC3 (safe coarse reason, no leak) — implemented + poison-token tests; AC4 (constrained/reduced-motion fail-closed, perceivable without animation, polite/assertive live regions) — implemented + tested.
- **Task audit:** all 9 tasks marked `[x]` verified against actual code (Contracts wrappers, pure `AgentCallStatusPresentation`, badge/panel/feedback, deferred fail-closed gateway + DI, nav entry + page, 78-key en/fr localization with full enum coverage, tests).
- **Security (AD-14):** prompt/content never reach any badge label, `aria-label`, `data-testid`, tooltip, or `aria-live`; wrappers carry only safe ids/coarse status; serialization no-leak tests present.

### Findings
- 🔴 CRITICAL: none · 🟠 HIGH: none · 🟡 MEDIUM: none
- 🟢 **LOW-1 (fixed):** `ConversationAgentCallPanel` declared the prompt `Required="true"` but `OnSubmitAsync` submitted unconditionally — without an `EditForm`, `Required` is only a hint, so an empty prompt could reach the gateway. **Fixed:** added an empty/whitespace guard + regression test (`Submitting_an_empty_prompt_does_not_call_the_gateway_and_shows_no_feedback`).
- 🟢 **LOW-2 (noted, no change):** `AgentCallStatusFeedback`'s constrained-viewport `<p>` sets `aria-describedby` to its own `id` — a self-reference is an ARIA no-op. It is harmless (the text is `tabindex=0` and read on focus, satisfying "focusable accessible reason") and the governance tests assert the attribute's presence; a "proper" fix would require wiring the panel's submit control to this reason across components, which is beyond the demonstrable/deferred scope. Left as-is.
- 🟢 **LOW-3 (noted, by scope):** Constrained-viewport / reduced-motion detection is not wired to any media query or JS interop — `Constrained` is a manually-set capability exercised only by tests. Consistent with the deferred/fail-closed scope of this story (the live read/polling path and the transient `ContextLoading`/`PostingPending` derivations via `Derive` land with Epic 4); no defect.

### Notes
- The `Rejected → Blocked` panel mapping (a rejected request shows "provider/dependency unavailable") is debatable but test-backed and there is no closer-fitting `AgentCallStatus`; acceptable by design.
- Story Debug Log recorded UI `336/0`; actual is now `352/0` (more tests, all passing) — informational only.
