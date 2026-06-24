# Story 3.6 — QA Generate E2E Tests Summary

Date: 2026-06-25
Workflow: `bmad-qa-generate-e2e-tests`
Story: `3-6-reject-abandon-and-expire-proposals` (status: review)
Role: QA automation (gap detection + auto-apply). No production code changed; tests only.

## Test Framework Detected

- **xUnit v3** + **Shouldly** (assertions) + **NSubstitute** (mocks) + **bUnit** (Blazor component rendering).
- VSTest (`dotnet test`) is blocked by sandbox socket permissions, so the built xUnit v3 executables were run directly (matching the dev-story run).

## Gap Analysis

The dev-story already shipped tests across all four layers (contracts, domain, server, UI). QA reviewed coverage against the four ACs and the Task 5 sub-bullets and found six concrete gaps where Task 5 named coverage that was not actually asserted:

| # | Layer | Gap found | AC / Task |
|---|-------|-----------|-----------|
| 1 | Domain | Cross-terminal guard untested — issuing reject/abandon/expire on a *different* already-terminal state must be a structural `ProposalNotPending` rejection, **not** a silent NoOp. Only same-state idempotency was tested. | AC2/AC4 |
| 2 | UI presentation | `ColorFor`/`IconFor` never pinned the three terminal states to their exact role/glyph — only the "not Brand/not Success" totality was asserted, so a wrong role (e.g. `Expired => Important`) would pass. | Task 5 "presentation totality (color+icon+label)" |
| 3 | UI badge | `BadgeConformanceTests` proposal-state theory rendered only `Pending/Edited/Unknown` — the three terminal states never went through the real `ProposedAgentReplyStateBadge`. | Task 5 "mirror BadgeConformanceTests" |
| 4 | UI localization | The 23 new `ProposalRejector.*` / `ProposalAbandoner.*` / `ProposalQueue.StartNewCall` whole-string keys had no EN/FR parity test (only the 3 enum-derived label keys were covered). | Task 5 "EN/FR parity for all new keys" |
| 5 | UI queue | The "Start a new Agent Call" affordance (AC4 routing) for terminal proposals was never rendered/asserted; `includeHistorical` was the only queue coverage added. | AC4 / Task 5 |
| 6 | Server | Abandonment had no terminal structural-guard test, although rejection did (`Terminal_rejection_request_returns_not_pending_without_dispatch`). | AC2/AC4 |

All gaps were **auto-applied**.

## Generated Tests (gaps filled)

### Domain — `tests/Hexalith.Agents.Tests/AgentInteractionProposalTerminalAggregateTests.cs`
- `Terminal_proposals_reject_other_terminal_actions_as_not_pending_without_a_noop` (Theory ×3: Rejected/Abandoned/Expired) — a proposal in one terminal state rejects the *other* terminal commands with `ProposalNotPending` and `IsNoOp == false`; versions preserved.

### Server — `tests/Hexalith.Agents.Server.Tests/AgentInteractionProposalTerminalOrchestratorTests.cs`
- `Terminal_abandonment_request_returns_not_pending_without_dispatch` — non-pending abandonment returns `Unknown` + `NotAbandonableReason.ProposalNotPending`, dispatches nothing (mirrors the reject guard).

### UI presentation — `tests/Hexalith.Agents.UI.Tests/ProposedAgentReplyStatePresentationTests.cs`
- Extended `ColorFor_binds_each_shipped_state_to_its_role` with Rejected=Danger, Abandoned=Subtle, Expired=Severe.
- `IconFor_binds_each_terminal_state_to_a_dedicated_curated_glyph` (Theory ×3) — Rejected/Abandoned → `SubtractCircle`, Expired → `Warning`, each distinct from the `QuestionCircle` total default.

### UI badge — `tests/Hexalith.Agents.UI.Tests/BadgeConformanceTests.cs`
- Extended `Proposal_state_badge_renders_color_icon_and_localized_whole_string` with Rejected/Abandoned/Expired (color + icon + whole localized string + `role=status` + `aria-label` + no inline hex).

### UI localization — `tests/Hexalith.Agents.UI.Tests/LocalizationResourceTests.cs`
- Registered all 23 new whole-string keys so both `Every_enum_derived_key_resolves_to_a_non_empty_english_whole_string` and `Every_enum_derived_key_is_translated_in_french` now enforce EN + FR coverage.

### UI queue — `tests/Hexalith.Agents.UI.Tests/ProposalQueueTests.cs`
- `Terminal_proposal_row_offers_a_start_a_new_agent_call_affordance` (Theory ×3) — terminal rows render `agents-proposal-queue-start-new-call` with the `StartNewCall` copy.
- `Pending_proposal_row_does_not_offer_the_start_a_new_agent_call_affordance` — the affordance is terminal-only.

## Test Results (xUnit v3 executables, Release)

| Project | Before | After | Result |
|---------|-------:|------:|--------|
| Hexalith.Agents.Contracts.Tests | 282 | 282 | Pass (untouched) |
| Hexalith.Agents.Tests (domain) | 648 | 651 | Pass (+3) |
| Hexalith.Agents.Server.Tests | 334 | 335 | Pass (+1) |
| Hexalith.Agents.UI.Tests | 525 | 561 | Pass (+36) |

- Build: `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` → 0 warnings, 0 errors.
- All four projects: **Errors 0, Failed 0, Skipped 0, Not Run 0.**
- +40 new test cases total (UI +36 = +6 presentation, +3 badge, +4 queue, +23 localization English-theory cases; the French parity test is a single fact covering all keys).

## Coverage

- **AC1** (authorized rejection → terminal + versions preserved): covered (dev) + cross-terminal guard (QA).
- **AC2** (abandonment can never act again): covered (dev) + cross-terminal guard + server abandonment guard (QA).
- **AC3** (deterministic expiry, visible in UI/API): covered (dev) + terminal badge/presentation totality + start-new-call visibility (QA).
- **AC4** (terminal rejects approve/post before side effects, routes to new call): covered (dev) + queue start-new-call affordance + cross-terminal guards (QA).

## Next Steps

- Run in CI once VSTest socket permissions are available (`dotnet test Hexalith.Agents.slnx`).
- Epic 4 will add the live `IAgentCommandDispatcher`/`IProposalExpiryPolicyReader` bindings and the automatic expiry firing trigger; integration tests for the live trigger belong there (out of scope here per the story's Epic-4 boundary).
