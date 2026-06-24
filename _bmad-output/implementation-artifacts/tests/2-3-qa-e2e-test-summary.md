# Story 2.3 — QA E2E Test Automation Summary

**Workflow:** `bmad-qa-generate-e2e-tests`
**Story:** 2-3-build-conversation-context-with-safe-bounds (status: review)
**Date:** 2026-06-24
**Engineer role:** QA automation (test generation only — no production code changed)

> Story 2.3 is a pure backend event-sourcing feature: the Conversation context-building step on the
> `AgentInteraction` aggregate (pure aggregate budget math + safe context contracts + server context
> orchestration over a live-conditional Conversations read and deferred fail-closed ports). The live
> command dispatch, the read-model/query binding, and the durable-owner runtime are deferred to later
> Epic-2/read-model stories, so there is **no API controller or UI surface to drive** in this story.
> The "E2E/automated" coverage is therefore the project's existing test tiers — the pure domain
> pipeline (`ProcessAsync` reflection-dispatch + replay), the contract serialization surface, the live
> Conversations read mapping, and the server context-orchestration seam — plus the **cross-seam E2E**
> that runs the orchestrator's own dispatched command back through the real pure aggregate (already
> present, kept green).

## Framework detected

xUnit v3 (`xunit.v3` 3.2.2) + Shouldly 4.3.0 assertions + NSubstitute 5.3.0 mocks — the project's
existing stack. New tests reuse the established fixtures (`AgentInteractionTestData`, the orchestrator
test's `StubLoaded`/`Entry`/`Request` helpers, the reader test's `Details`/`Message`/freshness
builders), snake_case `[Fact]`/`[Theory]` naming, and Shouldly (no raw `Assert.*`); aggregate/replay
tests stay pure command/state/event tests, NSubstitute only at the server seam. **No new packages were
added.**

> VSTest opened its local listener without the `SocketException (13)` the prior stories saw, so the
> standard `dotnet test --no-build` runner was used for all four suites; no fallback to the raw xUnit v3
> executables was needed this run.

## Result

| Test project | Before | After | Δ | Failed | Skipped |
| --- | --- | --- | --- | --- | --- |
| `Hexalith.Agents.Tests` (domain) | 451 | **456** | +5 | 0 | 0 |
| `Hexalith.Agents.Server.Tests` | 169 | **178** | +9 | 0 | 0 |
| `Hexalith.Agents.Contracts.Tests` | 140 | 140 | 0 | 0 | 0 |
| `Hexalith.Agents.UI.Tests` (regression) | 156 | 156 | 0 | 0 | 0 |
| **Total** | 916 | **930** | **+14** | 0 | 0 |

Build: `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` → **0 warnings / 0 errors**
(`TreatWarningsAsErrors=true`). All pre-existing structural/guard tests stayed green; every new test is
additive (no existing test was modified or deleted — only the orchestrator test's private `Request`
helper gained two optional defaulted parameters, fully backward-compatible).

> The server +9 is **5 new test methods**, one of which (`Unknown_or_unrecognized_policy_references…`)
> is a `[Theory]` expanding to 4 inline cases.

## Coverage gaps discovered and closed

The dev-story tests already covered the headline paths: the aggregate's full / bounded / oversized /
stale / unavailable / invalid-budget decision matrix, the not-buildable rejections, the idempotent
terminal transitions, the `Decide`/`Evaluate` no-drift, the contract round-trips and secret/PII/content
guards, the orchestrator's per-outcome mapping + fail-closed-on-throw + no-side-effects + raw-text
redaction, and the reader's Visible/Hidden/Redacted/Unavailable/Stale/exception mapping. The QA pass
walked each acceptance criterion, the **Context Budget Rules** table, and every non-trivial branch of
the policy / orchestrator / reader / resolver, and found the following **untested gaps** — primarily
exact budget boundaries and defensive fail-closed branches — all now auto-applied:

| # | Gap (untested behavior) | AC / Spine | Tier | Test added |
| --- | --- | --- | --- | --- |
| 1 | The **exact** fit boundary `FullContextTokenCount == window − reserved` (inclusive) → `ContextReady(Full)` was untested (dev used 1 000 ≪ budget) | AC2 | Domain | `Full_context_exactly_at_the_available_budget_records_context_ready_full` |
| 2 | One token past the boundary → `ContextBlocked(ExceedsModelBudget)`, never silently truncated — the off-by-one that separates fit from overflow was untested | AC3 | Domain | `Full_context_one_token_over_the_available_budget_blocks_and_never_silently_truncates` |
| 3 | An approved bounded behavior with a **non-positive** limit (the `BoundedContextTokenLimit > 0` guard) must NOT be used → block, never a meaningless zero-token bounded record | AC4 / AD-11 | Domain | `An_approved_bounded_behavior_with_a_non_positive_limit_is_not_usable_and_blocks` |
| 4 | A bounded limit that **equals** the available budget exactly (inclusive `<=`) → `ContextReady(Bounded)` with the bounds recorded was untested (dev used 50 000 ≪ and 130 000 ≫) | AC4 | Domain | `An_approved_bounded_behavior_whose_limit_equals_the_available_budget_is_used` |
| 5 | Replay determinism was pinned for the **ready** stream only; a request+authorize+**context-blocked** stream rehydrating an identical status/reason/evidence across rebuilds was untested | AC3 / FR-24 / AD-13 | Domain | `Replay_over_request_authorize_then_context_blocked_is_deterministic_across_rebuilds` |
| 6 | A snapshot recording an **empty provider/model id** must short-circuit to `ModelBudgetUnavailable` **without** calling the catalog (the `IsNullOrWhiteSpace` guard) — mirrors the Story 2.2 gate gap, untested here | AC2 / AD-9 | Server | `Empty_provider_or_model_id_short_circuits_to_context_blocked_without_reading_the_catalog` |
| 7 | A reader returning `Loaded` but with a **null messages** payload (degraded/contract-violating read) must fail closed to `Unavailable` and **not** measure a null payload — the defensive branch was untested | FR-21 / AD-12 | Server | `A_loaded_read_with_a_null_messages_payload_is_a_degraded_read_failing_closed_to_unavailable` |
| 8 | The reader's `Rebuilding` (no details, not an access denial) → `Unavailable` branch (documented `Unavailable/Rebuilding → Unavailable`) was untested (only `Unavailable`/`Redacted` were) | AC3 / AD-12 | Server | `Rebuilding_with_no_details_maps_to_unavailable` |
| 9 | An authorized, fresh, but **empty** Conversation → `Loaded` with an empty, **non-null** message list (count 0) — so the orchestrator measures zero tokens instead of tripping the null-payload guard — was untested | AC1 / AD-6 | Server | `Visible_and_fresh_with_no_messages_maps_to_loaded_with_an_empty_message_list` |
| 10 | `ContextPolicyResolution.Resolve` (the OQ-10 fail-closed resolver) had **no direct test**: the V1 default and any unknown reference must resolve to **no approved bounded behavior** (so V1 always blocks on overflow, never invents truncation) | AC4 / OQ-10 / AD-11 | Server | `ContextPolicyResolutionTests` (`V1_default…`, `Unknown_or_unrecognized…` `[Theory]`×4) |

## Acceptance-criteria coverage after the QA pass

- **AC1 — load only authorized Conversation detail/timeline; V1 excludes non-conversation sources:** the
  reader mapping now also covers the empty-but-authorized conversation (Loaded, count 0) and the
  Rebuilding degraded read; the orchestrator still proves the only seam is a *read* + one dispatch (no
  provider/post/proposal).
- **AC2 — full-context fit records full usage + budget metadata, raw context redacted:** the **exact
  inclusive fit boundary** is now pinned (at-budget → Full, +1 → block), and the empty-provider-id
  short-circuit to `ModelBudgetUnavailable` without a needless catalog read is covered.
- **AC3 — oversized / not-fresh fails closed with no side effects, never silently truncates:** the
  off-by-one overflow block and the degraded loaded-with-null-messages fail-closed branch are covered;
  the context-blocked stream now has a replay-determinism guard so the audit record is stable.
- **AC4 — approved bounded behavior records behavior + bounds; never silent truncation:** the bounded
  boundary cases (non-positive limit rejected; limit == budget used) and the OQ-10 resolver (V1 +
  unknown → no approved behavior) are now explicit, locking in that V1 always blocks on overflow rather
  than inventing a truncation.

## Commands run (from `Hexalith.Agents/`)

```
dotnet restore Hexalith.Agents.slnx
dotnet build   Hexalith.Agents.slnx --configuration Release -m:1        # 0W / 0E
dotnet test tests/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj                   --configuration Release --no-build   # 456 / 0 / 0
dotnet test tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj     --configuration Release --no-build   # 178 / 0 / 0
dotnet test tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release --no-build # 140 / 0 / 0
dotnet test tests/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj             --configuration Release --no-build   # 156 / 0 / 0 (regression)
```

## Files changed (tests only)

- `tests/Hexalith.Agents.Tests/AgentInteractionContextAggregateTests.cs` (+4 facts — budget/bounded boundaries)
- `tests/Hexalith.Agents.Tests/AgentInteractionStateReplayTests.cs` (+1 fact — blocked-stream replay determinism)
- `tests/Hexalith.Agents.Server.Tests/AgentInteractionContextOrchestratorTests.cs` (+2 facts; `Request` helper gained 2 optional defaulted params)
- `tests/Hexalith.Agents.Server.Tests/ConversationClientContextReaderTests.cs` (+2 facts — Rebuilding, empty-but-fresh)
- `tests/Hexalith.Agents.Server.Tests/ContextPolicyResolutionTests.cs` (**new** — OQ-10 resolver, 2 methods incl. `[Theory]`×4)

## Notes / out of scope

- **No API/UI E2E:** there is intentionally no HTTP controller, live dispatcher binding, query/read-model
  binding, or invocation UI in Story 2.3 (deferred to Stories 2.4–2.6 and the read-model story), so
  browser/API E2E is not yet applicable. The cross-seam orchestrator→aggregate test
  (`End_to_end_the_dispatched_context_command_drives_the_aggregate_to_the_same_decision`, dev-story) is
  the closest end-to-end available and remains green.
- **Deferred live bindings:** the new context reader/measurer ports plus the reused catalog/dispatcher
  ports are exercised through NSubstitute stubs (and their real `Deferred*` placeholders in the
  all-deferred fail-closed test). Asserting persisted state-store end-state requires the deferred live
  read-model binding, which belongs to the dedicated Agents read-model story.
- **Bounded path is test-only by design (OQ-10):** `ContextPolicyResolution.Resolve` wires no approved
  bounded behavior in V1, so the `Bounded` branch is reachable only by the aggregate/policy tests that
  supply an approved behavior directly — exactly as the story intends.

## Checklist validation (`bmad-qa-generate-e2e-tests/checklist.md`)

- [x] API tests generated (server context-orchestration + live Conversations-read seam — closest analogue to API for this backend story)
- [x] E2E tests generated (cross-seam orchestrator→aggregate present; no UI surface to drive)
- [x] Tests use standard test framework APIs (xUnit v3 + Shouldly + NSubstitute)
- [x] Tests cover happy path (full-fits → ContextReady; at-budget boundary → Full; bounded-at-budget → Bounded)
- [x] Tests cover critical error cases (overflow block, degraded/null-payload read, Rebuilding, empty-provider short-circuit, fail-closed resolver)
- [x] All generated tests run successfully (456 / 178 / 140 / 156, 0 failed, 0 skipped)
- [x] Tests use proper locators — N/A for backend; tests use typed fixtures and explicit evidence/measurement lookups
- [x] Tests have clear descriptions (snake_case behavioral names)
- [x] No hardcoded waits or sleeps
- [x] Tests are independent (no order dependency; per-test fixtures/substitutes)
- [x] Test summary created (this file)
- [x] Tests saved to the project's existing test directories
- [x] Summary includes coverage metrics (before/after table + gap table)
