# Test Automation Summary — Story 3.4: Regenerate Proposed Reply Versions

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-24 · **Engineer role:** QA automation (Administrator)
**Story:** `_bmad-output/implementation-artifacts/3-4-regenerate-proposed-reply-versions.md` (status: review) · **FR coverage:** FR-14 / FR-16
**Test framework (detected):** .NET 10 / Blazor — xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0`, bUnit (no Playwright/Cypress JS stack). Run via the built xUnit v3 executables directly (VSTest TCP socket unavailable in the sandbox). Used the existing framework; added no dependency.

## Scope

Story 3.4 shipped with a comprehensive existing suite across all four layers. This QA pass did **not** regenerate that suite; it performed a coverage-gap analysis — every regeneration production behavior, branch, enum, guard and fail-closed path cross-referenced against the existing tests and Task 5's stated requirements — and **auto-applied tests for the discovered gaps only**. No production code was changed; gaps were filled additively inside the existing regeneration test classes (reusing established helpers/fixtures).

## Baseline (story Dev Agent Record)

| Project | Total | Failed |
| --- | --- | --- |
| Hexalith.Agents.Contracts.Tests | 273 | 0 |
| Hexalith.Agents.Tests (domain) | 601 | 0 |
| Hexalith.Agents.Server.Tests | 311 | 0 |
| Hexalith.Agents.UI.Tests | 484 | 0 |
| **Total** | **1669** | **0** |

Release build: **0 warnings / 0 errors** (warnings-as-errors).

## Coverage gap analysis — discovered gaps and the tests added

### Domain — `AgentInteractionProposalRegenerationAggregateTests.cs` (+2)

| Test | Gap closed | AC |
|------|-----------|----|
| `Decide_and_Evaluate_never_drift_across_every_outcome_and_verdict` | Task 5 asks for the `Evaluate`/`Decide` no-drift theory **over every outcome**; the shipped theory sampled only 5 of 11 outcome×verdict combos. Now exhaustive over all `AgentProposalRegenerationOutcome` × {Valid, Unauthorized, Unavailable, Incomplete}. | AC1–AC4 / AD-5 |
| `Process_async_round_trips_a_failed_regeneration_and_keeps_the_proposal_retryable` | No full-pipeline (reflection-dispatch + JSON) round-trip for the AC3 **failure** path; only the success path had one. Provider-timeout ⇒ `ProposedAgentReplyRegenerationFailed`, proposal stays `Pending` (retryable), no version appended. | AC3 |

### Server — `AgentInteractionProposalRegenerationOrchestratorTests.cs` (+4)

| Test | Gap closed | AC |
|------|-----------|----|
| `An_unrecognized_content_safety_verdict_fails_closed_to_regeneration_failed_content_safety_blocked` | Content-safety `Unknown` verdict — the gate is `!= Passed`, not `== Blocked`; untested fail-closed edge. | AC3 / AD-5 |
| `A_content_safety_evaluator_that_throws_fails_closed_to_content_safety_blocked_without_leaking` | A throwing evaluator must not skip the gate; also asserts no raw error / regenerated content leaks. | AC3 / AD-9 / AD-14 |
| `A_provider_entry_without_text_generation_returns_regeneration_failed_provider_disabled_without_invoking_the_provider` | Enabled-but-not-text-capable entry ⇒ `ProviderDisabled` before any provider call (distinct branch from a Disabled status). | AC3 / AC4 |
| `A_blank_provider_id_returns_regeneration_failed_provider_unavailable_without_reading_the_catalog_or_invoking_the_provider` | Missing provider/model id ⇒ `ProviderUnavailable`, short-circuiting before catalog read + provider call. | AC3 / AC4 |

### Contracts — `AgentInteractionProposalRegenerationContractsTests.cs` (+1)

| Test | Gap closed | AC |
|------|-----------|----|
| `A_failure_shaped_regeneration_result_round_trips_with_no_version_and_stays_content_free` | Only the success-shaped result (with version) was round-tripped; the failure-shaped carrier (null version) rides the same dispatched command and must survive JSON while staying content-free (AD-14). | AC3 / AD-14 |

### UI — no gap

Control statuses (Regenerated / NotAuthorized / Unavailable / NotPending), distinct generated/edited/regenerated version labels, fail-closed gateway, `OnRegenerated` host callback, presentation totality for `Regenerated`, and en/fr localization parity were already covered.

### Already covered (no action needed)

Authorized regeneration appends an immutable `Regenerated` version + priors preserved · every failure combination → `ProposalRegenerationFailed` with the mapped reason while the proposal stays retryable · not-regeneratable rejections (never-requested / no-pending-proposal, AC4 — no provider event) · idempotent terminal no-op + second distinct regeneration · failure-status retry trap · success-path reflection-dispatch + JSON round-trip · deterministic id derivation (distinct purpose tags, retry dedupe, no cross-family collision) · orchestrator AC4 terminal guard + fail-closed authorization (6 non-Resolved sources, null/empty policy, resolver-throws, all-deferred), `OperationCanceledException` propagation, reserved-trust-key stripping, envelope scope, deterministic-id reuse on retry, content confinement · provider timeout / adapter-throw / disabled-catalog / not-found-catalog / re-read failure / not-available-policy / degraded-success mappings · ordinal stability + enum-by-name + fail-safe-to-Unknown · marker interfaces · AD-14 no-leak on safe surfaces · presentation/badge totality · localization parity · deferred-gateway fail-closed.

## Result (after this pass)

| Project | Before | After | Δ |
| --- | --- | --- | --- |
| Hexalith.Agents.Contracts.Tests | 273 | **274** | +1 |
| Hexalith.Agents.Tests (domain) | 601 | **603** | +2 |
| Hexalith.Agents.Server.Tests | 311 | **315** | +4 |
| Hexalith.Agents.UI.Tests | 484 | **484** | 0 (regression check) |
| **Total** | 1669 | **1676** | **+7** |

**Result: Passed — Failed: 0, Skipped: 0** across all four projects. Release build after additions: **0 warnings / 0 errors**.

## AC coverage after this pass

- **AC1** — deterministic regeneration attempt on the same `AgentInteraction`, same source/snapshot: covered (domain, server, identity). ✅
- **AC2** — success appends a new immutable version, all priors preserved: covered; failure-shaped result round-trip added. ✅
- **AC3** — fail-closed, content-safe, proposal stays pending: **strengthened** — exhaustive `Evaluate`/`Decide` no-drift, failure-path round-trip, content-safety `Unknown`/throw fail-closed edges. ✅
- **AC4** — terminal proposals cannot invoke the provider: covered; provider not-text-capable + blank-id fail-closed branches added. ✅

## Checklist validation (`bmad-qa-generate-e2e-tests/checklist.md`)

- [x] API / write-path tests generated (contract + orchestrator + aggregate command/event surface)
- [x] E2E / component tests generated (bUnit regenerator + lifecycle via `ProcessAsync`)
- [x] Tests use standard framework APIs (xUnit v3 / Shouldly / NSubstitute / bUnit; no raw `Assert.*`)
- [x] Happy path covered (authorized regeneration + dispatch)
- [x] 1–2 critical error cases covered (terminal/not-pending, fail-closed auth, safety block/Unknown/throw, provider disabled/unavailable)
- [x] All tests run successfully — **1676 total, 0 failed**
- [x] Proper locators (semantic `data-testid` / `role` / `aria` queries in UI)
- [x] Clear descriptions (`Subject_scenario_expectation`)
- [x] No hardcoded waits/sleeps (bUnit `WaitForAssertion`, no `Thread.Sleep`)
- [x] Tests are independent (each builds its own state/render; no order dependency)
- [x] Summary created with coverage metrics (this file)

## Files changed (tests only — no production change)

- `Hexalith.Agents/tests/Hexalith.Agents.Contracts.Tests/AgentInteractionProposalRegenerationContractsTests.cs` (+1 test)
- `Hexalith.Agents/tests/Hexalith.Agents.Tests/AgentInteractionProposalRegenerationAggregateTests.cs` (+1 `using System;`, +2 tests)
- `Hexalith.Agents/tests/Hexalith.Agents.Server.Tests/AgentInteractionProposalRegenerationOrchestratorTests.cs` (+4 tests, `Request` helper extended with optional `providerId`/`modelId`)

## Notes / boundaries

- No production code changed — the gaps were purely missing test coverage. The UI suite was re-run to confirm (not modify) green status.
- The live server read-model / projection / query-handler / BFF binding for `AgentProposalRegenerationEvidenceResult` remains **deferred to Epic 4**; its integration/API tests belong with that work.
- Story 3.7 will host the regenerator control and complete the full proposal-detail workspace / version-history / keyboard-focus accessibility — intentionally not duplicated here.

## Next steps

- Run the four projects in CI individually (never solution-level `dotnet test`; build with `-m:1`).
- When Epic 4 wires the live regeneration-evidence read path, add integration/API tests against the real read-model + the tenant-isolation denial audit.
