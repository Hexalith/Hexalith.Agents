# Story 2.6 — Test Summary

**Story:** 2.6 Conversation Invocation UX And Call Status Feedback
**Date:** 2026-06-24
**Build:** `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` → **0 warnings / 0 errors** (`TreatWarningsAsErrors`).

## Per-project results (Release, `--no-build`)

| Project | Passed | Failed | Skipped | Total |
|---|---|---|---|---|
| `Hexalith.Agents.Contracts.Tests` | 186 | 0 | 0 | 186 |
| `Hexalith.Agents.UI.Tests` | 336 | 0 | 0 | 336 |
| `Hexalith.Agents.Server.Tests` (regression) | 232 | 0 | 0 | 232 |
| `Hexalith.Agents.Tests` (regression) | 527 | 0 | 0 | 527 |
| **Total** | **1281** | **0** | **0** | **1281** |

The two in-scope projects for this story are `Hexalith.Agents.UI.Tests` and
`Hexalith.Agents.Contracts.Tests`; the Server/Agents projects were run to confirm no
regression from the four new Contracts wrappers (the `ContractsSecretNonDisclosureTests`
and Server structural/contract conformance guards stay green).

## New / extended tests

### `Hexalith.Agents.Contracts.Tests`
- `AgentCallGatewayWrappersContractsTests` (new): JSON round-trip for
  `AgentInteractionInspectionResult`/`AgentInteractionInspectionStatus` and
  `AgentCallRequestResult`/`AgentCallRequestStatus`; status enums serialize by name and
  default to `Unknown`; data (view/reference) non-null only on the success path; no
  prompt/content member; serialized wrappers carry only safe references (no-leak); no
  Hexalith-namespaced domain attribute (scoped so compiler `Nullable*` attributes are ignored).
- `ContractsSecretNonDisclosureTests` (existing, assembly-wide): auto-covers the four new types.

### `Hexalith.Agents.UI.Tests`
- `AgentCallStatusPresentationTests` (new, pure): full durable→UX `MapStatus` table; transient
  `Derive` (`ContextLoading`/`Generating`/`PostingPending`); Success only for `Posted`; Danger /
  Severe / Informative / Subtle role bindings; Brand never a status; every state has a non-null
  icon + label key; every reason-enum value maps to a safe, content-free reason key.
- `AgentCallStatusBadgeTests` (new, bUnit): color + icon + visible text == accessible name == the
  whole-string key; `role="status"`; no-hex regex; Success only for `Posted`.
- `ConversationAgentCallPanelTests` (new, bUnit): names `hexa`; Fluent prompt input (no raw
  `<textarea>`); response-mode implication for both modes; never posted-message wording for a
  non-`Posted` status; submit calls `RequestCallAsync` with the safe inputs; prompt never echoed
  into a badge label/accessible name/`data-testid`; Esc/Cancel returns control to the trigger.
- `AgentCallStatusFeedbackTests` (new, bUnit): safe coarse reason for failure/blocked states (no
  raw error/prompt/content); polite live region for ordinary transitions, assertive only for
  denial/terminal-failure; constrained-viewport unavailable reason is focusable + `aria-describedby`
  with review-only status retained; stale read renders the `Stale` surface, never fresh.
- `ConversationCallTests` (new, bUnit): page is `[Authorize]`-gated by `Agents.Administrator`;
  deferred gateway renders the permission-denied surface; authorized caller sees the panel.
- `DeferredGatewayTests` (extended): `DeferredConversationAgentCallGateway` fails closed
  (`NotAuthorized`, null reference/view).
- `AgentsUiCompositionTests` (extended): `IConversationAgentCallGateway` registered scoped with the
  deferred placeholder.
- `AgentsNavigationTests` (extended): five ordered entries; the new "Conversation call" entry is
  gated by `Agents.Administrator`, hidden for unauthorized/authenticated-without-policy users.
- `LocalizationResourceTests` (extended): every new `Agents.CallStatus.*` /
  `Agents.ConversationCall.*` / `Agents.Navigation.ConversationCall` key exists in **both** en and fr.
- `AccessibilityTests` (extended): feedback exposes a named polite status live region perceivable
  without animation; constrained-viewport reason is focusable + `aria-describedby`-linked.
- `AgentsTestContext` (extended): substitutes `IConversationAgentCallGateway` (default fail-closed).

## Notes
- VSTest ran cleanly; the Story 2.2 `SocketException (13)` fallback (run the built xUnit v3
  executable directly) was not needed this run.
- No Verify snapshots were added, so `DiffEngine_Disabled=true` was not required.
