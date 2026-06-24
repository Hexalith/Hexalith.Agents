# Story 2.1 — QA E2E Test Automation Summary

**Workflow:** `bmad-qa-generate-e2e-tests`
**Story:** 2-1-request-hexa-from-a-source-conversation (status: review)
**Date:** 2026-06-24
**Engineer role:** QA automation (test generation only — no production code changed)

> Story 2.1 is a pure backend event-sourcing feature (aggregate + contracts + server
> orchestration). HTTP endpoints, the live command dispatch, the read-model binding, and the
> invocation UI are deferred to later Epic-2 stories, so there is **no API controller or UI surface
> to drive** in this story. The "E2E/automated" coverage is therefore the project's existing test
> tiers: the pure domain pipeline (`ProcessAsync` reflection-dispatch), the contract
> serialization surface, and the server request-orchestration seam — plus a new **cross-seam E2E**
> that runs the orchestrator's own dispatched command back through the real pure aggregate.

## Framework detected

xUnit v3 (`xunit.v3` 3.2.2) + Shouldly 4.3.0 assertions + NSubstitute 5.3.0 mocks — the project's
existing stack. New tests reuse the established fixtures (`AgentInteractionTestData`), snake_case
`[Fact]`/`[Theory]` naming, and Shouldly (no raw `Assert.*`). No new packages were added.

## Result

| Test project | Before | After | Δ | Failed | Skipped |
| --- | --- | --- | --- | --- | --- |
| `Hexalith.Agents.Tests` (domain) | 280 | **293** | +13 | 0 | 0 |
| `Hexalith.Agents.Contracts.Tests` | 96 | **99** | +3 | 0 | 0 |
| `Hexalith.Agents.Server.Tests` | 101 | **108** | +7 | 0 | 0 |
| `Hexalith.Agents.UI.Tests` (regression) | 156 | 156 | 0 | 0 | 0 |
| **Total** | 633 | **656** | **+23** | 0 | 0 |

Build: `dotnet build Hexalith.Agents.slnx --configuration Release` → **0 warnings / 0 errors**
(`TreatWarningsAsErrors=true`). All pre-existing structural/guard tests stayed green; every new test
is additive (no existing test was modified or deleted).

## Coverage gaps discovered and closed

The dev-story tests already covered the happy path and the headline rejections. The QA pass walked
each acceptance criterion and each non-trivial branch and found the following **untested gaps**,
all now auto-applied:

| # | Gap (untested behavior) | AC / Spine | Tier | Test(s) added |
| --- | --- | --- | --- | --- |
| 1 | `SnapshotsEqual` compared only 1 of 9 scalar fields on a same-id conflict — a dropped field would silently no-op | AC2 / AD-13 | Domain | `Request_conflicting_snapshot_scalar_on_same_id_produces_already_requested` (`[Theory]`, 8 fields) |
| 2 | `HasUsableSnapshot` guards `ModelId`, `ContextPolicyReference`, and `AgentId`, but only `ProviderId`/null were tested | AC1 / AC4 | Domain | `…_empty_snapshot_model_id…`, `…_blank_snapshot_context_policy_reference…`, `…_blank_agent_id…` |
| 3 | No replay over a **mixed** stream (pre-activation rejection → later successful request on the same id) | replay / AD-3 | Domain | `Replay_of_a_rejection_then_a_successful_request_rebuilds_the_requested_state` |
| 4 | Conflict Theory's chosen `ResponseMode` mutation could silently equal the seed (vacuous test) | AC2 | Domain | `Request_with_identical_snapshot_but_differing_response_mode_is_not_treated_as_a_duplicate` (guards the fixture) |
| 5 | Envelope's **trusted scope** (`TenantId`, `MessageId`, `CorrelationId`, `UserId`, null `CausationId`) was never asserted — AC4 says tenant scope comes from the envelope | AC2 / AC4 | Server | `Builds_the_envelope_with_the_trusted_scope_from_the_request` |
| 6 | The `extensions.Count > 0 ? … : null` branch (only-reserved / no client extensions → **null** map) was untested | AC4 | Server | `Supplying_only_reserved_extensions_yields_no_extension_map`, `No_client_extensions_yields_no_extension_map` |
| 7 | Deterministic id's claim that hashing **neutralizes illegal characters** (colons/slashes/whitespace) was untested | AD-13 | Server | `Derives_a_regex_valid_colon_free_id_even_when_components_contain_illegal_characters` |
| 8 | Deterministic id's **length-prefix framing** (so `("ab","c")` ≠ `("a","bc")`) was untested — naive concat would collide | AD-13 | Server | `Length_prefix_framing_makes_component_boundaries_unambiguous` |
| 9 | Orchestrator and aggregate were only tested in isolation — never **together** end-to-end | AC1 / AC2 | Server | `End_to_end_the_dispatched_command_drives_the_aggregate_to_interaction_requested` |
| 10 | The fail-closed precondition path was not proven end-to-end (not-available snapshot → `MissingAgentSnapshot`) | AC1 / AC3 | Server | `End_to_end_a_not_available_snapshot_drives_the_aggregate_to_a_missing_snapshot_rejection` |
| 11 | No test pinned the **no-wall-clock** invariant — nothing stopped a future timestamp field on the event/command | AD-3 | Contracts | `Request_surface_carries_no_wall_clock_field` |
| 12 | The V1 default context-policy reference value was unpinned (Story 2.3 should change it deliberately) | FR-9 | Contracts | `Default_context_policy_reference_is_the_pinned_v1_value` |
| 13 | The snapshot was only round-tripped nested inside the event, never standalone (nested enum by-name) | AC1 | Contracts | `Snapshot_round_trips_and_serializes_its_response_mode_by_name` |

## Acceptance-criteria coverage after the QA pass

- **AC1 — request record + AD-4 snapshot:** full-snapshot creation, every missing-scalar guard, and
  the end-to-end orchestrator→aggregate happy path are now covered.
- **AC2 — safe reference + dedupe:** safe-reference shape, deterministic-id stability/uniqueness,
  illegal-char neutralization, collision-resistant framing, exact-duplicate `NoOp`, and **every**
  snapshot-scalar conflict path are covered.
- **AC3 — explicit-only, no side effects:** orchestrator touches only reader + dispatcher; the
  fail-closed path is proven end-to-end to reject, never to create an interaction or call out.
- **AC4 — protect prompt/context + tenant boundaries:** prompt absence on every safe surface (pre-
  existing) plus the new no-wall-clock guard, trusted envelope scope (tenant from envelope), and the
  reserved-extension strip/null-map branch.

## Commands run (from `Hexalith.Agents/`)

```
dotnet restore Hexalith.Agents.slnx
dotnet build   Hexalith.Agents.slnx --configuration Release            # 0W / 0E
dotnet test tests/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj --configuration Release   # regression
```

## Files changed (tests only)

- `tests/Hexalith.Agents.Tests/AgentInteractionAggregateTests.cs` (+3 facts, +1 theory[8], +1 fixture-guard fact)
- `tests/Hexalith.Agents.Tests/AgentInteractionStateReplayTests.cs` (+1 mixed-stream replay fact)
- `tests/Hexalith.Agents.Contracts.Tests/AgentInteractionContractsTests.cs` (+3 facts)
- `tests/Hexalith.Agents.Server.Tests/AgentInteractionRequestOrchestratorTests.cs` (+7 facts)

## Notes / out of scope

- **No API/UI E2E:** there is intentionally no HTTP controller, live dispatcher binding, or
  invocation UI in Story 2.1 (deferred to Stories 2.2–2.6), so browser/API E2E is not yet
  applicable. The cross-seam orchestrator→aggregate test is the closest end-to-end available and is
  now in place.
- **Integration-tier assertion rule (EventStore retro R2-A6):** asserting persisted state-store
  end-state (Redis/CloudEvent body) requires the deferred live read-model binding; that belongs to
  the dedicated Agents read-model story, not here.
