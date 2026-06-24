# Test Automation Summary — Story 3.7 (Proposal Detail, Version History, and Accessibility)

**Workflow:** `bmad-qa-generate-e2e-tests`
**Date:** 2026-06-25
**Engineer:** QA automation (Administrator)
**Scope:** Generate/close automated-test gaps for the implemented Story 3.7 feature. Tests only — no code review or story validation.

## Framework Detected

- **.NET 10 / Blazor** component feature → bUnit 2.8.4-preview + **xUnit v3 3.2.2** + Shouldly 4.3.0 + NSubstitute 5.3.0 + AngleSharp.
- No JS/Playwright/Cypress stack — the project's "E2E/component" layer is bUnit rendering inside the real `FrontComposerShell`. Used the existing harness (`AgentsTestContext`, `AgentUiTestData`, `StubAgentsLocalizer`, `FixedTimeProvider`) and conventions (`Subject_scenario_expectation`, no raw `Assert.*`).
- Build: `dotnet build Hexalith.Agents.slnx -c Release -m:1` (warnings-as-errors). VSTest hits `SocketException (13)` in this environment, so tests run via the built xUnit v3 executables directly (as the story prescribes).

## Coverage Gaps Found & Closed

The feature shipped with substantial tests already. Cross-referencing AC1–AC4 + Tasks 1–8 against the existing suites surfaced these **real gaps** — several were marked done in the story's task list but had no corresponding test. All were auto-applied.

| # | Gap (AC / task) | Why it mattered | Where closed |
|---|-----------------|-----------------|--------------|
| 1 | `ProposalTransitionPresentation` had **no test** | New pure politeness/label map (AC4 matrix); total switch + assertive-only-for-stale rule unverified | **new** `ProposalTransitionPresentationTests.cs` |
| 2 | `ProposalTransitionAnnouncer` had **no component test** (Task 6 claimed it) | The live-region politeness matrix (polite ×6 vs assertive ×1), `None`-silent, `aria-atomic`, and safe-text were unexercised | **new** `ProposalTransitionAnnouncerTests.cs` |
| 3 | Detail-page **focusable heading** untested (Task 5 claimed it) | AC3 keyboard reachability; every other page asserts a `tabindex=-1` heading | `AccessibilityTests.cs` |
| 4 | Detail-page **page-level safe-text no-leak** untested (Task 6 claimed it) | AD-14 highest a11y risk — no content may reach any `aria-live`/accessible name | `AccessibilityTests.cs` |
| 5 | **Compare-metadata panel reachable before approve** untested | review-accessibility requirement (compare before approval) | `ProposalDetailTests.cs` |
| 6 | **Esc closes compare without committing/re-reading** untested | AC3 Esc-without-commit contract | `ProposalDetailTests.cs` |
| 7 | **Approval summary names the exact selected version** untested | review-accessibility — approver must see id/timestamp/source before posting | `ProposalDetailTests.cs` |
| 8 | **Expiry "none" branch** untested | AC1 — display snapshot, never fabricate a default expiry | `ProposalDetailTests.cs` |
| 9 | **Posting-outcome distinction** (Posted/Failed/Approved/None) untested | AC1/AC4 — "approved" ≠ "posted" | `ProposalDetailTests.cs` (Theory) |
| 10 | **Load-time announcer wiring** + pending-silent + stale-assertive untested at page level | AC4 — page maps state→transition politeness | `ProposalDetailTests.cs` |
| 11 | Version-history **Space-key** selection + re-select no-op untested | AC3 — only Enter was covered; component also handles `" "`/`"Spacebar"` | `ProposalVersionHistoryTests.cs` |

## Generated Tests

### New files
- [x] `tests/Hexalith.Agents.UI.Tests/ProposalTransitionPresentationTests.cs` — pure live-region politeness/label matrix (label keys, totality, distinctness, polite ×6, assertive-only-for-stale).
- [x] `tests/Hexalith.Agents.UI.Tests/ProposalTransitionAnnouncerTests.cs` — announcer component: polite/assertive roles, `None`-silent region, `aria-atomic`, safe whole-string text.

### Extended files
- [x] `tests/Hexalith.Agents.UI.Tests/ProposalDetailTests.cs` — compare-before-approve, Esc-close, approval-summary names version, expiry-none, posting-outcome Theory, load-time announce Theory, pending-not-assertive, stale-block-assertive.
- [x] `tests/Hexalith.Agents.UI.Tests/ProposalVersionHistoryTests.cs` — Space-key selection (Theory), re-select-is-no-op.
- [x] `tests/Hexalith.Agents.UI.Tests/AccessibilityTests.cs` — detail focusable heading, detail-page content-safe live-regions/accessible-names.

## Test Results (xUnit v3 executables, Release)

| Project | Total | Failed | Notes |
|---------|-------|--------|-------|
| Hexalith.Agents.Contracts.Tests | 297 | 0 | unchanged (detail read-contract coverage already complete) |
| **Hexalith.Agents.UI.Tests** | **682** | 0 | **+45** new cases (was 637) |
| Hexalith.Agents.Tests | 651 | 0 | regression, untouched |
| Hexalith.Agents.Server.Tests | 335 | 0 | regression, untouched |

Release build: **0 warnings / 0 errors** (warnings-as-errors; `ConfigureAwait`/CA2007 clean). One xUnit1031 blocking-call issue was found in a new test and fixed by moving the store init into a private helper.

## Coverage (Story 3.7)

- **AC1 detail content** — metadata block, provider/model, response mode, expiry (set + none), posting outcome (4 variants), no-leak: ✅
- **AC2 version history** — listbox, kind labels, selected state, click/Enter/Space select, prior-versions-preserved, approval/posting markers, no-leak: ✅
- **AC3 keyboard/focus/Esc** — focusable heading, version-option key activation, compare reachable before approve, Esc-closes-compare-without-commit: ✅
- **AC4 live regions** — politeness matrix (polite ×6, assertive only for destructive-prevention), ordinary-pending-silent, load-time wiring, safe-text separation: ✅
- Fail-closed gateway (AD-12) + content-free contracts (AD-14): ✅ (pre-existing + reinforced)

## Next Steps

- Run the four suites in CI once the VSTest socket issue is resolved (currently driven via the xUnit v3 executables).
- When Epic 4 wires the live read path (`IProposalDetailGateway`), add an integration test exercising the real projection → detail view (the deferred gateway fail-closed contract stays as the unit guard).
