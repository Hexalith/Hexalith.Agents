# Story 2.2 — QA E2E Test Automation Summary

**Workflow:** `bmad-qa-generate-e2e-tests`
**Story:** 2-2-enforce-invocation-authorization-and-dependency-readiness (status: review)
**Date:** 2026-06-24
**Engineer role:** QA automation (test generation only — no production code changed)

> Story 2.2 is a pure backend event-sourcing feature: the invocation authorization +
> dependency-readiness GATE step on the `AgentInteraction` aggregate (pure aggregate + safe gate
> contracts + server gate orchestration over deferred fail-closed ports). HTTP endpoints, the live
> command dispatch, the read-model/query binding, and the durable-owner runtime are deferred to later
> Epic-2/read-model stories, so there is **no API controller or UI surface to drive** in this story.
> The "E2E/automated" coverage is therefore the project's existing test tiers — the pure domain
> pipeline (`ProcessAsync` reflection-dispatch), the contract serialization surface, and the server
> gate-orchestration seam — plus the **cross-seam E2E** that runs the orchestrator's own dispatched
> command back through the real pure aggregate (already present, kept green).

## Framework detected

xUnit v3 (`xunit.v3` 3.2.2) + Shouldly 4.3.0 assertions + NSubstitute 5.3.0 mocks — the project's
existing stack. New tests reuse the established fixtures (`AgentInteractionTestData`, the orchestrator
test's `StubAllReady`/`Readiness`/`Entry` helpers), snake_case `[Fact]`/`[Theory]` naming, and Shouldly
(no raw `Assert.*`); aggregate tests stay pure command/state/event tests, NSubstitute only at the
server seam. No new packages were added.

> Sandbox note: VSTest could not open its local TCP listener (`SocketException (13): Permission
> denied`), exactly as the dev-story recorded. The built xUnit v3 test executables were run directly
> (`./tests/<proj>/bin/Release/net10.0/<proj>`); all suites executed fully.

## Result

| Test project | Before | After | Δ | Failed | Skipped |
| --- | --- | --- | --- | --- | --- |
| `Hexalith.Agents.Tests` (domain) | 365 | **424** | +59 | 0 | 0 |
| `Hexalith.Agents.Contracts.Tests` | 118 | **119** | +1 | 0 | 0 |
| `Hexalith.Agents.Server.Tests` | 131 | **143** | +12 | 0 | 0 |
| `Hexalith.Agents.UI.Tests` (regression) | 156 | 156 | 0 | 0 | 0 |
| **Total** | 770 | **842** | **+72** | 0 | 0 |

Build: `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` → **0 warnings / 0 errors**
(`TreatWarningsAsErrors=true`). All pre-existing structural/guard tests stayed green; every new test
is additive (no existing test was modified or deleted).

> The domain +59 is **5 new test methods**, one of which (`Decide_matches_the_aggregate_recorded_decision`)
> is a `[Theory]` expanding to all 9 checks × 6 blocking outcomes = 54 cases.

## Coverage gaps discovered and closed

The dev-story tests already covered the headline paths: the aggregate's full check×outcome decision
matrix, the authorized/denied/blocked/not-evaluable/idempotent transitions, the contract round-trips
and marker/secret guards, and the orchestrator's happy path + the main per-reader denial mappings. The
QA pass walked each acceptance criterion, the **Gate Evaluation Rules** table (all nine checks), and
every non-trivial branch of the aggregate/policy/orchestrator, and found the following **untested
gaps**, all now auto-applied:

| # | Gap (untested behavior) | AC / Spine | Tier | Test(s) added |
| --- | --- | --- | --- | --- |
| 1 | `ReadProviderReadinessAsync` short-circuits to `Missing` when the snapshot recorded no provider/model id — and must **not** call the catalog — was untested | AC1 / AD-9 | Server | `Empty_provider_or_model_id_short_circuits_to_missing_without_reading_the_catalog` |
| 2 | Confirmation-mode with a **null** approver policy → `Missing` (the `policy is null` branch) — and must not call the resolver — was untested | AC1 / AD-8 | Server | `Confirmation_mode_with_no_approver_policy_maps_to_missing_blocked` |
| 3 | The caller-party `MapPartyVerdict` mapping was tested only for `Missing`; `Unauthorized` (caller not authorized → **Denied**) was untested | AC3 / FR-20 | Server | `Caller_party_unauthorized_maps_to_denied` |
| 4 | An `Unauthorized` **outcome** on a readiness-class **check** (the Agent's own Party) must still decide **Blocked**, not Denied — the class follows the check, not the outcome | AC1 / AD-7 | Server | `Agent_party_unauthorized_maps_to_blocked_not_denied` |
| 5 | `MapProviderReadiness` default branch — a `NotAuthorized` provider read must fail closed to `Unavailable` **without leaking cross-tenant existence** — was untested | AC3 / AD-12 | Server | `Provider_read_not_authorized_fails_closed_to_unavailable_without_leaking_existence` |
| 6 | `MapProviderReadiness` `{ Success, Entry: null }` degraded read → `Unavailable` (not "ready") was untested | FR-21 / AD-12 | Server | `Provider_read_success_with_a_null_entry_is_a_degraded_read_failing_closed_to_unavailable` |
| 7 | `DependencyFreshness` was only proven via the **readiness** reader; a stale **Tenants** projection (Satisfied outcome, `IsFresh:false`) blocking on freshness while tenant access stays Satisfied was untested | AC1 / AD-12 | Server | `Stale_tenant_projection_maps_dependency_freshness_to_stale_while_tenant_access_stays_satisfied` |
| 8 | Same for a stale **Conversation** read — freshness aggregates **every** consulted projection, not just readiness | AC1 / AD-12 | Server | `Stale_conversation_read_maps_dependency_freshness_to_stale_while_conversation_access_stays_satisfied` |
| 9 | Only the **tenant** reader's fail-closed catch was tested; the **conversation** reader's `try/catch → Unavailable` (and no raw error text on the wire) was untested | AC2 / FR-21 / AD-14 | Server | `A_conversation_reader_that_throws_fails_closed_to_unavailable_denied` |
| 10 | The **provider** reader's `try/catch → Unavailable` (distinct catch) was untested | FR-21 / AD-12 | Server | `A_provider_catalog_reader_that_throws_fails_closed_to_unavailable_blocked` |
| 11 | The **approver-policy resolver's** `try/catch → Unavailable` (Confirmation branch) was untested | FR-21 / AD-8 | Server | `An_approver_policy_resolver_that_throws_fails_closed_to_unavailable_blocked` |
| 12 | The `when (ex is not OperationCanceledException)` filter — a **genuine cancellation must propagate**, never be masked as an `Unavailable` verdict, and **no** gate command is dispatched — was untested | correctness / FR-21 | Server | `A_genuine_cancellation_propagates_and_is_never_masked_as_a_fail_closed_verdict` |
| 13 | The aggregate's `_blockingOutcomes` matrix deliberately omits the `Unknown` sentinel; that `Unknown` (the fail-safe sentinel) is itself a blocker → **Denied** on an authorization check was untested | FR-21 / AD-12 | Domain | `Unknown_outcome_on_an_authorization_check_is_a_blocker_and_decides_denied` |
| 14 | Same `Unknown` sentinel on a readiness check → **Blocked** was untested | FR-21 / AD-12 | Domain | `Unknown_outcome_on_a_readiness_check_is_a_blocker_and_decides_blocked` |
| 15 | A blocking verdict on an **unrecognized/future check** (`AgentInteractionGateCheck.Unknown`) must still fail the gate and be classified non-authorization (Blocked), never silently dropped | FR-21 / AD-2 | Domain | `An_unrecognized_check_still_blocks_and_is_classified_readiness_not_authorization` |
| 16 | The full-failure audit path — **every** check blocking records all nine blockers as evidence and decides Denied — was untested (max-evidence AC4 case) | AC4 / FR-24 | Domain | `Every_check_blocking_records_all_nine_blockers_as_evidence_and_decides_denied` |
| 17 | The "cannot drift" invariant (`AgentInvocationGatePolicy.Decide` used by the orchestrator vs `Evaluate` used by the aggregate) was proven only by one integration case; pinned at the unit level across all 54 single-blocker sets + the authorized path | AC1 / AD-12 | Domain | `Decide_matches_the_aggregate_recorded_decision` (`[Theory]`, 54), `Decide_matches_the_aggregate_on_the_all_satisfied_authorized_path` |
| 18 | The failed-inspection result was tested for `null` view in-memory only; its **wire form** (JSON round-trip preserving the null view so a cross-tenant probe learns nothing) was untested | AC3 / AD-12 | Contracts | `Failed_inspection_results_round_trip_with_a_null_evidence_view` |

## Acceptance-criteria coverage after the QA pass

- **AC1 — gate every dependency, fail closed on uncertainty:** the aggregate's full check×outcome
  matrix plus the new `Unknown`-sentinel and unrecognized-check fail-closed cases; the orchestrator's
  per-check mapping now covers the previously-untested branches (empty provider id, null approver
  policy, degraded/not-authorized provider read, and `DependencyFreshness` driven by the tenant and
  conversation projections — not just readiness).
- **AC2 — missing/stale caller access → denied/blocked, no side effects:** the conversation reader's
  fail-closed catch is now covered, and the no-side-effects/single-dispatch and cancellation-no-dispatch
  invariants are pinned.
- **AC3 — unprovable authz fails closed without revealing cross-tenant existence:** caller-party
  `Unauthorized` → Denied, provider `NotAuthorized` → `Unavailable` (identical to a degraded read), and
  the failed-inspection result's **null view survives serialization**.
- **AC4 — safe audit evidence distinguishes failure classes:** the max-evidence path (all nine blockers
  recorded, Denied) is now covered, alongside the existing safe-by-construction/secret-disclosure
  guards; no raw error text crosses the boundary on a reader throw.

## Commands run (from `Hexalith.Agents/`)

```
dotnet restore Hexalith.Agents.slnx
dotnet build   Hexalith.Agents.slnx --configuration Release -m:1        # 0W / 0E
# VSTest socket blocked in sandbox → run the built xUnit v3 executables directly:
./tests/Hexalith.Agents.Tests/bin/Release/net10.0/Hexalith.Agents.Tests
./tests/Hexalith.Agents.Contracts.Tests/bin/Release/net10.0/Hexalith.Agents.Contracts.Tests
./tests/Hexalith.Agents.Server.Tests/bin/Release/net10.0/Hexalith.Agents.Server.Tests
./tests/Hexalith.Agents.UI.Tests/bin/Release/net10.0/Hexalith.Agents.UI.Tests   # regression
```

## Files changed (tests only)

- `tests/Hexalith.Agents.Tests/AgentInteractionGateAggregateTests.cs` (+5 methods incl. 1 `[Theory]`×54)
- `tests/Hexalith.Agents.Contracts.Tests/AgentInteractionGateContractsTests.cs` (+1 fact)
- `tests/Hexalith.Agents.Server.Tests/AgentInteractionGateOrchestratorTests.cs` (+12 facts)

## Notes / out of scope

- **No API/UI E2E:** there is intentionally no HTTP controller, live dispatcher binding, query/read-model
  binding, or invocation UI in Story 2.2 (deferred to Stories 2.3–2.6 and the read-model story), so
  browser/API E2E is not yet applicable. The cross-seam orchestrator→aggregate test
  (`End_to_end_the_dispatched_gate_command_drives_the_aggregate_to_the_same_decision`, dev-story) is the
  closest end-to-end available and remains green.
- **Deferred live bindings:** the three new gate-readiness ports plus the reused ports are exercised
  through NSubstitute stubs (and their real `Deferred*` placeholders in the all-deferred fail-closed
  test). Asserting persisted state-store end-state requires the deferred live read-model binding, which
  belongs to the dedicated Agents read-model story.

## Checklist validation (`bmad-qa-generate-e2e-tests/checklist.md`)

- [x] API tests generated (server gate-orchestration seam — closest analogue to API for this backend story)
- [x] E2E tests generated (cross-seam orchestrator→aggregate present; no UI surface to drive)
- [x] Tests use standard test framework APIs (xUnit v3 + Shouldly + NSubstitute)
- [x] Tests cover happy path (all-ready → Authorized)
- [x] Tests cover critical error cases (denied/blocked/fail-closed/cancellation)
- [x] All generated tests run successfully (424 / 119 / 143, 0 failed, 0 skipped; UI regression 156)
- [x] Tests use proper locators — N/A for backend; tests use typed fixtures and explicit verdict lookups
- [x] Tests have clear descriptions (snake_case behavioral names)
- [x] No hardcoded waits or sleeps
- [x] Tests are independent (no order dependency; per-test fixtures/substitutes)
- [x] Test summary created (this file)
- [x] Tests saved to the project's existing test directories
- [x] Summary includes coverage metrics (before/after table + gap table)
