# Test Automation Summary — Story 3.2 (Discover Pending Proposals In Product)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-24 · **Engineer role:** QA automation (Administrator)
**Story:** `_bmad-output/implementation-artifacts/3-2-discover-pending-proposals-in-product.md` (status: review)

## Framework Detected

The Agents module is a .NET 10 / Blazor (FluentUI v5 RC) project — **no Playwright/Cypress JS stack**. The
established automated-test stack (per `Hexalith.FrontComposer/_bmad-output/project-context.md` and the as-built suite)
is this project's "E2E/API" surface:

- **xUnit v3** + **Shouldly** (no raw `Assert.*`) + **NSubstitute**
- **bUnit** for Blazor component ("E2E" page) tests — substituting the read gateway seam
- **Contract** tests = the API-shape guards (System.Text.Json round-trip, fail-closed envelopes)

Story 3.2 is the read-side + UI discovery surface only (the live server read path is deferred to Epic 4), so the
testable surface is: the public READ contracts, the proposal-queue page, the state presentation/badge, the gateway
seam, nav/policy gating, and localization. Used the existing framework; added no dependency.

## Coverage Gap Analysis

The as-built suite was already strong. Auto-applied **15 net-new tests** filling the discovered gaps — all in
untested **branching logic** that maps directly to acceptance criteria. No production code was changed.

### UI page — `tests/Hexalith.Agents.UI.Tests/ProposalQueueTests.cs` (+12)

| Test | Gap closed | AC |
|------|-----------|----|
| `Expiry_filter_narrows_the_queue_to_the_matching_bucket` (Theory ×3) | `MatchesExpiry` — 4 branches, ISO parse + 24h window — had **0 coverage** | AC2 |
| `Agent_filter_narrows_the_queue_to_the_selected_agent` | `FluentSelect` over distinct AgentIds | AC2 |
| `Source_conversation_filter_keeps_only_contains_matches` | contains-match (`FluentTextInput`) | AC2 |
| `Caller_filter_keeps_only_contains_matches` | contains-match (`FluentTextInput`) | AC2 |
| `Responsibility_column_reads_you_for_actionable_rows_and_approver_otherwise` | derived "current responsibility" label (Task 1 note) | AC1 |
| `Age_column_renders_a_deterministic_bucket_from_the_injected_clock` | `TimeProvider` → age-column wiring (non-flaky) | AC1 |
| `Expiry_column_shows_the_no_expiry_label_and_a_time_element` | null-vs-`<time>` expiry-column branch | AC1 |
| `Count_indication_renders_on_a_stale_result` | count surfaces on **Stale** (only Success was tested) | AC3 |
| `Stale_result_still_renders_the_authorized_rows_behind_the_stale_notice` | degraded-data-with-rows coexists with the stale notice | AD-12 |
| `Unavailable_read_discloses_no_records_count_or_grid` | error/fault path fails closed — no grid/count/record leak | AC4 |

### Presentation — `tests/Hexalith.Agents.UI.Tests/ProposedAgentReplyStatePresentationTests.cs` (+1)

| Test | Gap closed | AC |
|------|-----------|----|
| `Age_helper_uses_inclusive_lower_bounds_at_each_bucket_boundary` | exact 1h / 1d / 7d boundaries (each belongs to the next bucket) | AC1 |

### Contracts — `tests/Hexalith.Agents.Contracts.Tests/PendingProposalsContractsTests.cs` (+2)

| Test | Gap closed | AC |
|------|-----------|----|
| `Stale_result_can_carry_no_trustworthy_rows` | `Stale([], 0)` fails safe like a denial | AC4 |
| `Success_result_round_trips_multiple_rows_in_order` | nested multi-row list serialization + ordering | AC1/AC4 |

### Already covered (no action)

Loading / PermissionDenied / Error / Stale / Empty / FilteredEmpty surfaces · needs-my-action + state filters ·
count-on-Success · no-leak-on-NotAuthorized · badge conformance (color+icon+text, no hex) · nav admin-vs-approver
gating · en/fr localization parity · deferred-gateway fail-closed · presentation totality over reserved states.

## Generated Tests

### Contract (API-shape) tests
- [x] `tests/Hexalith.Agents.Contracts.Tests/PendingProposalsContractsTests.cs` — Stale-empty fail-safe; multi-row round-trip + ordering

### E2E (bUnit page) + presentation tests
- [x] `tests/Hexalith.Agents.UI.Tests/ProposalQueueTests.cs` — agent/expiry/source/caller filters; responsibility/age/expiry columns; stale-count; stale-rows; unavailable no-leak
- [x] `tests/Hexalith.Agents.UI.Tests/ProposedAgentReplyStatePresentationTests.cs` — age-bucket boundary correctness

## Results (Release, warnings-as-errors, `-m:1`, run per project)

Build: `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` → **Build succeeded, 0 Warning(s) / 0 Error(s)**.

| Project | Before | After | Delta |
|---|---|---|---|
| `Hexalith.Agents.Contracts.Tests` | 215 | **217** | +2 |
| `Hexalith.Agents.UI.Tests` | 415 | **428** | +13 |
| `Hexalith.Agents.Tests` (aggregate) | 557 | **557** | 0 (regression check) |
| `Hexalith.Agents.Server.Tests` | 253 | **253** | 0 (regression check) |
| **Total** | 1440 | **1455** | **+15** |

**Result: Passed! Failed: 0, Skipped: 0** across all four projects. ✅

## AC Coverage After This Pass

- **AC1** — queue lists safe fields; responsibility/age/expiry columns + state badge now asserted at render; no content field by construction. ✅
- **AC2** — **all six filters** (needs-my-action, state, agent, source conversation, caller, expiry) now have narrowing tests; filtered-empty→reset covered. ✅
- **AC3** — count indication asserted on **both** Success and Stale; absent on denial. ✅
- **AC4** — denied **and** faulted (Unavailable) reads assert no grid/count/record disclosure; Stale-empty + denial envelopes carry empty list + zero count. ✅

## Notes / Boundaries

- No production code changed — gaps were purely missing test coverage. The two write-side/regression suites were
  re-run to confirm (not modify) green status.
- The live server read-model / projection / query-handler / BFF binding remains **deferred to Epic 4**; those paths
  are not yet testable here (the deferred gateway fails closed to `NotAuthorized`).
- Standards held: PascalCase BDD-style names, Shouldly assertions, `ConfigureAwait`-clean, deterministic clock
  (`FixedTimeProvider`) — no hardcoded waits, order-independent, consistent with the surrounding Story 3.2 tests.

## Next Steps

- Run the suite in CI alongside the existing Agents lanes (unit/bUnit; no Docker/Aspire required).
- When Epic 4 wires the live read path (`NeedsCurrentUserAction` / age / freshness computation + read-authorization
  audit), add integration/API tests against the real read-model and the tenant-isolation denial audit.
- Stories 3.3–3.7 add row-level actions (edit/regenerate/approve/reject) — extend the queue tests with focus-return,
  `Esc` semantics, and live-region politeness when those land.
