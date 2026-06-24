# Story 2.6 — QA E2E Test Automation Summary

**Workflow:** `bmad-qa-generate-e2e-tests`
**Story:** 2-6-conversation-invocation-ux-and-call-status-feedback (status: review)
**Date:** 2026-06-24
**Engineer role:** QA automation (test generation only — no production code changed)

> Story 2.6 is a UI/UX story: the Conversation-originated Agent Call (`hexa`) invocation affordance,
> the canonical call-status UX state + pure presentation mapping, the `AgentCallStatusBadge`, the
> `ConversationAgentCallPanel`, the `AgentCallStatusFeedback` surface, the deferred/fail-closed
> `IConversationAgentCallGateway`, the policy-gated nav entry + `ConversationCall` page, en/fr
> localization, plus four safe Contracts gateway-result wrappers. The live read/command path
> (`Hexalith.Agents.Client` → BFF/API → read-model) is **deferred to Epic 4 (4.1/4.3)**, so there is no
> live HTTP/SignalR endpoint or real navigable browser host to drive in this story. The "E2E/automated"
> coverage is therefore the project's existing tiers — **bUnit component E2E** (render the panel/feedback/
> page through the real FrontComposer test host and drive the user flow: type a prompt → submit → observe
> the rendered status, reason, live region, focus, and fail-closed surfaces), the **pure presentation
> mapping** unit tier, and the **Contracts serialization** tier — exactly as Stories 1.7/1.8 established for
> the admin UI.

## Framework detected

xUnit v3 (`xunit.v3` 3.2.2) + Shouldly 4.3.0 + NSubstitute mocks + **bUnit 2.8.4-preview** for Blazor
components, over the FrontComposer test host (`FrontComposerTestBase` via the shared `AgentsTestContext`).
The new tests reuse the established fixtures and conventions: the `AgentsTestContext` NSubstitute gateways
(default fail-closed), the key-returning `StubAgentsLocalizer` (a value equal to the key proves a single
whole string resolved through the localizer — UX-DR14), `RenderedFragmentTextExtensions.VisibleText()`,
`Subject_Scenario_Expectation` snake_case naming, and Shouldly (no raw `Assert.*`). **No new packages,
fixtures, or test helpers were added** beyond two private helpers inside the panel test class
(`SubmitWithResultAsync`, `CaptionOf`).

> VSTest opened its local listener cleanly — the Story 2.2 `SocketException (13)` fallback (run the built
> xUnit v3 executable directly) was not needed. No Verify snapshots were added, but tests were run with
> `DiffEngine_Disabled=true` per the FrontComposer rule.

## Result

| Test project | Before | After | Δ | Failed | Skipped |
| --- | --- | --- | --- | --- | --- |
| `Hexalith.Agents.UI.Tests` | 336 | **351** | +15 | 0 | 0 |
| `Hexalith.Agents.Contracts.Tests` | 186 | 186 | 0 | 0 | 0 |
| `Hexalith.Agents.Server.Tests` (regression) | 232 | 232 | 0 | 0 | 0 |
| `Hexalith.Agents.Tests` (domain regression) | 527 | 527 | 0 | 0 | 0 |
| **Total** | 1281 | **1296** | **+15** | 0 | 0 |

Build: `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` → **0 warnings / 0 errors**
(`TreatWarningsAsErrors=true`). All pre-existing structural/governance guards (`BadgeConformanceTests`,
`AccessibilityTests`, `LocalizationResourceTests`, `AgentsUiCompositionTests`, `AgentsNavigationTests`,
the Contracts boundary/no-leak guards) stayed green; every new test is **additive** — no existing test
was modified or deleted.

## Coverage gaps discovered and closed

The dev-story suite already covered the headline paths: the full durable→UX `MapStatus` table + transient
`Derive`, Success-only-for-`Posted` / Brand-never / Danger·Severe·Informative·Subtle role bindings, the
badge color+icon+text + no-hex conformance, the panel's name-`hexa` / Fluent-input / response-mode-
implication / never-posted-wording / prompt-not-echoed / Esc-cancel happy paths, the feedback safe-reason /
live-region-politeness-matrix / constrained-viewport / stale-guard, the deferred-gateway fail-closed, the
nav policy-gating, en/fr key parity, and the Contracts round-trip + no-content-leak guards. The QA pass
walked each acceptance criterion and every non-trivial **branch** of the panel `OnSubmitAsync` mapping, the
panel `StatusCaptionKey` taxonomy, the agent-name/caller fallbacks, the page authorization-probe outcomes,
and the `Derive` default — and found the following **untested gaps**, all now auto-applied:

| # | Gap (untested behavior) | AC / Spine | Tier | Test added |
| --- | --- | --- | --- | --- |
| 1 | The panel's **`AgentCallRequestStatus.NotAuthorized → Denied`** submit branch was untested — a denied submit must render a safe failure feedback (coarse reason, assertive region, `Failed` caption) and **never** imply a post. Only the `Accepted` branch was covered. | AC1, AC3 / AD-12, AD-14, UX-DR22 | UI (bUnit) | `A_denied_gateway_outcome_renders_a_failure_feedback_with_a_safe_reason_and_never_implies_a_post` |
| 2 | The panel's **`Rejected → Blocked`** submit branch was untested — a rejected request must render the Blocked (dependency-class) failure with its safe coarse reason. | AC1, AC3 | UI (bUnit) | `A_rejected_gateway_outcome_renders_a_blocked_failure_feedback_with_a_safe_reason` |
| 3 | The panel's **`_ => Unknown`** default submit branch was untested — an unrecognized outcome resolves to the Unknown sentinel with **no reason** and never reads as posted (the `Calling` caption). | AC1 | UI (bUnit) | `An_unknown_gateway_outcome_renders_the_unknown_state_with_no_reason_and_no_posted_wording` |
| 4 | **AC1's explicit "`Generated` (automatic) reads *posting…*, never *posted*"** guard was only asserted **negatively** (absence of "posted"). The **positive** caption (`...Status.Posting`) for a `Generated`/Automatic state was unverified. | AC1 / AD-6, UX-DR22 | UI (bUnit) | `A_generated_automatic_call_reads_posting_not_posted` |
| 5 | The panel `StatusCaptionKey` **in-flight taxonomy** (`Requested → Calling`, `ContextReady → Generating`) was unverified — the in-flight wording AC1 requires ("Calling `hexa`…", "Generating…"). | AC1 / UX-DR22 | UI (bUnit) | `Accepted_in_flight_states_read_with_their_in_flight_caption` (Theory ×2) |
| 6 | The **single legitimate `Posted` path** was untested — no test proved that a `Posted` result *does* read "posted" **and** renders the `Success` badge (the inverse of the never-posted guard; the only Success state, UX-DR11). | AC1, AC2 / UX-DR11, UX-DR22 | UI (bUnit) | `A_posted_result_is_the_only_state_that_reads_posted_and_renders_the_success_badge` |
| 7 | The panel **`AgentName` fallback** (empty `AgentDisplayName` → the localized `hexa` whole string) was untested — the affordance must always visibly name the Agent, never render an empty name or a raw id. | AC1 / AD-14, UX-DR24 | UI (bUnit) | `Panel_falls_back_to_the_localized_hexa_label_when_no_agent_display_name_is_supplied` |
| 8 | The panel **caller-context** slot was untested — neither the `CallerReference` empty → localized "you" fallback nor a supplied safe caller reference shown in the mono slot (AC1 "captures … caller"). | AC1 / AD-7, UX-DR13 | UI (bUnit) | `Panel_falls_back_to_the_localized_you_caller_label_when_no_caller_reference_is_supplied`, `Panel_surfaces_a_supplied_caller_reference_in_the_mono_context_slot` |
| 9 | The **`ConversationCall` page had no focusable-heading test** — the other four setup pages each assert `tabindex=-1` on their route `<h1>`; the new page (keyboard-reachable even when it fails closed to permission-denied) was the lone gap. | AC4 / UX-DR32, UX-DR37 | UI (bUnit) | `Conversation_call_in_shell_exposes_a_focusable_heading` |
| 10 | The page authorization probe's **`Success` inspection outcome** was untested — only `NotFound` was proven to unlock the panel; a `Success` (authorized caller with an existing interaction) is the other non-`NotAuthorized` branch and must also unlock the panel. | AC1 / AD-12 | UI (bUnit) | `Authorized_caller_with_a_current_interaction_also_sees_the_invocation_panel` |
| 11 | `Derive` was untested for **statuses with no transient successor while in flight** (the `_ => MapStatus` default — e.g. `Requested`/`Denied` + `inFlight`) and for **response-mode independence** of the non-`Generated` derivations (`ContextReady` is `Generating` in either mode). | AC1, AC2 / AD-3 | UI (pure) | `Derive_renders_transient_states_only_while_a_milestone_is_pending` (+3 Theory rows) |

## Acceptance-criteria coverage after the QA pass

- **AC1 — the affordance names `hexa`, captures prompt + Source Conversation context (caller/agent/
  conversation), shows the response-mode implication, and never implies a post before `Posted`:** the panel
  flow is now driven end-to-end through every submit outcome — `Accepted` across `Requested`/`ContextReady`/
  `Generated`/`Posted`, plus `NotAuthorized`/`Rejected`/`Unknown` — with the **caption taxonomy positively
  pinned** (Calling/Generating/Posting/Posted/Failed), the **`Generated`-automatic → "posting…" not
  "posted"** guard asserted positively, the **only-`Posted`-reads-posted + Success badge** path proven, and
  the **agent-name + caller-context fallbacks** covered. The page now proves **both** non-`NotAuthorized`
  probe outcomes (`NotFound` *and* `Success`) unlock the panel.
- **AC2 — every call-status state is color + icon + visible text + accessible name; color-only and raw
  error text forbidden:** unchanged headline coverage stays green; the new `Posted`-path test re-pins the
  `Success`-only binding at the **rendered** panel level (not just the pure mapping), and the `Unknown`/
  failure submits confirm the rendered badge state for previously-unreachable panel branches.
- **AC3 — denied/blocked/failed present a safe reason and never leak:** the panel's **denied and rejected
  submits** now render the coarse, whole-string safe reason (`Agents.CallStatus.Reason.Denied` /
  `...Blocked`) with no raw/exception/content text and no posted wording — closing the gap where AC3 was
  proven on the standalone feedback surface but never through the panel's own gateway-outcome mapping.
- **AC4 — constrained viewports / reduced-motion degrade fail-closed and state changes stay perceivable
  without animation:** the feedback-surface coverage (live-region politeness, focusable constrained reason,
  stale-guard, perceivable-without-animation) stays green, and the **`ConversationCall` page focusable-
  heading** parity gap is closed so the invocation surface is keyboard-reachable in every authorization
  state.

## Commands run (from `Hexalith.Agents/`)

```
dotnet restore Hexalith.Agents.slnx
dotnet build   Hexalith.Agents.slnx --configuration Release -m:1                                              # 0W / 0E
DiffEngine_Disabled=true dotnet test tests/Hexalith.Agents.UI.Tests/...        --configuration Release --no-build  # 351 / 0 / 0
DiffEngine_Disabled=true dotnet test tests/Hexalith.Agents.Contracts.Tests/... --configuration Release --no-build  # 186 / 0 / 0
DiffEngine_Disabled=true dotnet test tests/Hexalith.Agents.Server.Tests/...    --configuration Release --no-build  # 232 / 0 / 0 (regression)
DiffEngine_Disabled=true dotnet test tests/Hexalith.Agents.Tests/...           --configuration Release --no-build  # 527 / 0 / 0 (regression)
```

## Files changed (tests only)

- `tests/Hexalith.Agents.UI.Tests/ConversationAgentCallPanelTests.cs` (+10 cases — denied/rejected/unknown
  submit feedback, in-flight + `Posted` caption taxonomy, agent-name + caller fallbacks; +2 private helpers)
- `tests/Hexalith.Agents.UI.Tests/AgentCallStatusPresentationTests.cs` (+3 `Derive` Theory rows — durable-map
  default + response-mode independence)
- `tests/Hexalith.Agents.UI.Tests/AccessibilityTests.cs` (+1 — `ConversationCall` focusable-heading parity)
- `tests/Hexalith.Agents.UI.Tests/ConversationCallTests.cs` (+1 — `Success` inspection also unlocks the panel)

## Notes / out of scope

- **No browser/HTTP E2E:** there is intentionally no live HTTP controller, dispatcher binding, query/
  read-model binding, or real navigable host in Story 2.6 (the live `Hexalith.Agents.Client` → BFF/API path
  is deferred to Epic 4, 4.1/4.3). The bUnit component tests render the real components through the
  FrontComposer test host and drive the user flow, which is the closest end-to-end available; the deferred
  gateway stays fail-closed by design (`DeferredConversationAgentCallGateway` → `NotAuthorized`).
- **Pattern-agnostic affordance (PRD OQ-1 open):** the exact Conversation-owned mention/command/action entry
  is unresolved, so the panel is the normalized invocation affordance (UX-DR24); the QA tests drive that
  normalized surface and do not over-fit to one entry pattern.
- **No production code changed and no submodules touched:** all edits are additive test code inside
  `Hexalith.Agents.UI.Tests`; `Hexalith.FrontComposer` and the sibling modules are untouched.

## Checklist validation (`bmad-qa-generate-e2e-tests/checklist.md`)

- [x] API tests generated — N/A for this UI story (no live API surface; the safe Contracts gateway wrappers
      keep their round-trip/no-leak coverage); the gateway **seam** is exercised through the deferred
      fail-closed contract and the NSubstitute-driven panel/page flows
- [x] E2E tests generated (bUnit component E2E: type prompt → submit → observe rendered status/reason/live
      region/focus/fail-closed surfaces, across every gateway outcome)
- [x] Tests use standard test framework APIs (xUnit v3 + Shouldly + NSubstitute + bUnit)
- [x] Tests cover happy path (Accepted → in-flight captions; `Posted` → posted wording + Success badge;
      authorized probe → panel)
- [x] Tests cover critical error cases (denied/rejected/unknown submit → safe coarse reason, never posted;
      constrained-viewport + stale already green)
- [x] All generated tests run successfully (351 / 186 / 232 / 527 — 1296 total, 0 failed, 0 skipped)
- [x] Tests use proper locators (semantic `data-testid`, `role`, `aria-label`, component instances — no
      brittle text/CSS coupling; visible text == accessible name via the key-returning stub localizer)
- [x] Tests have clear descriptions (`Subject_Scenario_Expectation` snake_case behavioral names)
- [x] No hardcoded waits or sleeps (`WaitForAssertion` for async page render; no `Task.Delay`/`Thread.Sleep`)
- [x] Tests are independent (per-test NSubstitute substitutes; no shared mutable state or order dependency)
- [x] Test summary created (this file)
- [x] Tests saved to the project's existing test directory (`tests/Hexalith.Agents.UI.Tests/`)
- [x] Summary includes coverage metrics (before/after table + gap table + per-AC coverage)
