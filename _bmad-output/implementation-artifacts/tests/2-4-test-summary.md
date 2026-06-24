# Story 2.4 — Test Summary: Generate And Safety-Check Agent Output

**Story:** 2-4-generate-and-safety-check-agent-output
**Date:** 2026-06-24
**Build:** `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` → **0 warnings / 0 errors** (`TreatWarningsAsErrors=true`)

## Result

| Test project | Tests | Passed | Failed | Skipped |
| --- | --- | --- | --- | --- |
| `Hexalith.Agents.Tests` (domain) | 492 | 492 | 0 | 0 |
| `Hexalith.Agents.Contracts.Tests` | 159 | 159 | 0 | 0 |
| `Hexalith.Agents.Server.Tests` | 202 | 202 | 0 | 0 |

> **Counts updated 2026-06-24 by the `bmad-qa-generate-e2e-tests` gap-fill pass** (domain 489→492, contracts 158→159, server 197→202; **+9 tests**). See [QA gap-fill pass](#qa-gap-fill-pass-2026-06-24) below.

Regression: `Hexalith.Agents.UI.Tests` (consumes Contracts after the additive `AgentInteractionStatus` extension — ordinals 7/8/9 added) → **156/156**. All pre-existing structural/guard tests stayed green (`StructuralSeedConformanceTests`, `ProjectReferenceDirectionTests`, `PublicContractPackageBoundaryTests`, `ContractsBoundaryTests`, `ContractsSecretNonDisclosureTests` — auto-covers the new generation contract types, `ModuleLayout`, `PackageVersionCentralizationTests`, `BuildContractConformanceTests`). New tests are additive — no existing test was modified (only `AgentInteractionTestData.cs` was extended, the three generation test classes had cases appended, and `AgentInteractionStateReplayTests.cs` appended to).

## Acceptance-criteria coverage

- **AC1** (generation runs through an Agents-owned provider adapter behind a safe boundary; only safe Provider/model identity, safe error classes, usage/status, policy references on public contracts/events) — the model invocation lives behind `IAgentGenerationProvider` (the first real model seam); `ContractsSecretNonDisclosureTests` auto-covers the new types (no provider-SDK type / secret-bearing member on the contracts surface); orchestrator `A_provider_that_throws_fails_closed_to_generation_failed_without_leaking_the_error` proves the raw provider error never crosses the boundary (AD-9, AD-14); the generated version/evidence expose only `ProviderId`/`ModelId`/capability version + token usage.
- **AC2** (Content Safety Policy gates output before any downstream artifact; passing content becomes a generated version + `Generated` state, failing content cannot become an approvable version) — aggregate `Succeeded_generation_records_generated_with_the_version` (status `Generated`, version appended); `Content_safety_blocked_records_safety_failed_and_never_emits_a_version_or_the_content` (status `SafetyFailed`, **no** `AgentOutputGenerated`); orchestrator evaluates safety **before** dispatch (`Content_safety_blocked_returns_safety_failed_with_no_content_in_the_envelope_or_result`).
- **AC3** (all failure classes fail closed with safe audit, never a partial message; no raw provider payload/error/secret/unsafe content in any display/status/log/audit surface) — the outcome→reason theory (`Each_failure_outcome_records_generation_failed_with_the_mapped_reason`) over provider-timeout/disabled/unavailable/adapter/invalid-context/generation-error/policy-failure → `GenerationFailed` with the mapped reason and **no** version; orchestrator fail-closed paths for re-read failure (`InvalidContext`), disabled/not-found/throwing catalog (`ProviderDisabled`/`ProviderUnavailable`), throwing provider (`AdapterFailure`), not-available policy (`PolicyFailure`); AD-14 no-leak assertions on the failure event, attempt evidence, rejection, dispatched envelope, and returned outcome.
- **AC4** (retried generation is deterministic and auditable; re-dispatch after a terminal outcome is a deterministic no-op preserving version history) — `VersionId == version-{AttemptId}` derived deterministically from the interaction-derived `AttemptId`; the three idempotent-terminal tests (`Re_generate_after_generated/generation_failed/safety_failed_is_a_noop…`) prove the recorded decision never flips and no duplicate version is appended; `Decide`/`Evaluate` no-drift theory; replay determinism across rebuilds.

## New tests added

### Domain — `Hexalith.Agents.Tests`
- `AgentInteractionTestData.cs` — extended with Story-2.4 generation fixtures: `StateContextReady()` (request → authorize → context-ready, the generation precondition), `GenerationResult(outcome, …)` + `SucceededGenerationResult()`/`SafetyBlockedGenerationResult()` (deliberately carries the unsafe content to prove the policy drops it)/`ProviderTimeoutGenerationResult()`, `GenerateCommand(result)`, and the `GeneratedContentText`/`GenerationAttemptId`/token-count/policy-version constants; `ApplyAll` now also dispatches `AgentOutputGenerated`/`AgentOutputGenerationFailed`/`AgentOutputNotGeneratableRejection`.
- `AgentInteractionGenerationAggregateTests.cs` — succeeded → `AgentOutputGenerated` with the version (deterministic `VersionId`, content + usage) + state `Generated`; **content-safety blocked → `AgentOutputGenerationFailed(Decision=SafetyFailed, Reason=ContentSafetyBlocked)` asserting no version is emitted and the unsafe content never rides on the failure event (AD-5, AD-14)**; the failure-outcome theory (timeout/disabled/unavailable/adapter/invalid-context/generation-error/policy-failure → `GenerationFailed` + mapped reason, no version); not-requested (null + rejection-only stream) → `…NotGeneratableRejection(InteractionNotRequested)`; requested / authorized but not context-ready → `…(ContextNotReady)`; idempotent re-generate on already `Generated`/`GenerationFailed`/`SafetyFailed` → `NoOp` (no flip, no duplicate version); `Decide`/`Evaluate` no-drift across every outcome; full reflection-dispatch + JSON round-trip through `ProcessAsync` (success content + failure reason survive by name).
- `AgentInteractionStateReplayTests.cs` — `Apply(AgentOutputGenerated)` → status `Generated` + appended version; `Apply` twice → append-only version history (Epic 3 forward-compat); `Apply(AgentOutputGenerationFailed)` → records decision + reason, no version; `Apply(AgentOutputNotGeneratableRejection)` is a replay-safe no-op; generation-outcome `Apply` only over a requested stream (the `IsRequested` guard); replay determinism over request+authorize+context+generated streams.

### Contracts — `Hexalith.Agents.Contracts.Tests`
- `AgentInteractionGenerationContractsTests.cs` — marker conformance (`AgentOutputGenerated`/`AgentOutputGenerationFailed` are `IEventPayload` and **not** `IRejectionEvent`; `AgentOutputNotGeneratableRejection` is `IRejectionEvent`); plain-record command (no `Hexalith`-namespaced attribute — scoped to avoid the compiler's `Nullable*` attributes per the Story 2.1 learning); System.Text.Json round-trips for the generated version (content preserved), attempt evidence, generation result (success + failure), command, both outcome events, and the rejection; the new enums (`AgentGenerationOutcome`/`AgentOutputGenerationFailureReason`/`AgentGenerationKind`/`AgentOutputNotGeneratableReason`) serialize by name + `Unknown` fallback; the additive `AgentInteractionStatus` extension serializes `Generated`/`GenerationFailed`/`SafetyFailed` by name **and preserves ordinals 0–6 while adding 7–9**; AD-14 — the generated content appears ONLY on the success version/event and never on the failure event, attempt evidence, or rejection (serialized no-leak + exact-name "no content member" reflection guard that does not trip the safe `PromptTokenCount`/`OutputTokenCount` counts).

### Server — `Hexalith.Agents.Server.Tests`
- `AgentInteractionGenerationOrchestratorTests.cs` (NSubstitute) — happy path dispatches `GenerateAgentOutput` with the correct domain/aggregate id + deterministic `attempt-{interactionId}` and returns `Generated`; safety-blocked → `SafetyFailed` with **no content** in the dispatched envelope or returned outcome; a provider that **throws** → fail-closed `GenerationFailed`/`AdapterFailure` with **no raw error** leaked; disabled / non-text-capable entry → `ProviderDisabled` (provider not invoked); not-found / throwing catalog → `ProviderUnavailable`; provider-timeout outcome → `ProviderTimeout`; conversation re-read failure / throw → `InvalidContext` (provider not invoked); not-available safety policy → `PolicyFailure` with no content dispatched and the evaluator not invoked; **all-deferred default graph fails closed** (first deferred seam = conversation re-read → `InvalidContext`); the deferred provider with conversation loaded + catalog enabled → `ProviderUnavailable` (provider SDK never enters the graph); reserved client extensions stripped (benign preserved); trusted envelope scope from the request; only read ports + the provider invocation + one dispatch — no Conversations post / no proposal (AC3); the dispatched command carries no raw source-conversation text (AD-14); a genuine `OperationCanceledException` propagates and dispatches nothing; cross-seam end-to-end (the dispatched command drives the real pure aggregate to the **same** decision — shared `AgentOutputGenerationPolicy`, no drift; the policy is `internal` and not visible to `Server.Tests`, so the decision is asserted via the orchestrator's returned status).

## Commands run (from `Hexalith.Agents/`)

```
dotnet build Hexalith.Agents.slnx --configuration Release -m:1
dotnet test tests/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj --configuration Release --no-build
dotnet test tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release --no-build
dotnet test tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj --configuration Release --no-build
dotnet test tests/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj --configuration Release --no-build
```

Test projects were run individually (not solution-wide) per the Story 2.2 VSTest-socket learning. No `SocketException` occurred, so the direct-executable fallback was not needed.

## QA gap-fill pass (2026-06-24)

Ran the `bmad-qa-generate-e2e-tests` workflow over the already-implemented Story 2.4 feature. The dev-authored suite was already broad; gap analysis compared **production branches vs. existing assertions** and found a set of untested fail-closed paths — the heart of AC3 ("all failure classes fail closed with safe audit"). All discovered gaps were auto-applied as additive tests (no production code changed; build stayed **0 warnings / 0 errors**).

**Gaps found and filled (+9 tests):**

| # | Layer | Untested production branch | Test added | AC |
| --- | --- | --- | --- | --- |
| 1 | Domain | `AgentOutputGenerationPolicy.MapFailureReason` **`default`** branch — an unmapped/garbage `AgentGenerationOutcome.Unknown` must fail closed to `GenerationFailed`/`GenerationError`, never `Generated` | `Unknown` case added to `Each_failure_outcome_records_generation_failed_with_the_mapped_reason` **and** `Decide_matches_the_aggregate_recorded_decision_for_each_outcome` (no-drift) | AC3 |
| 2 | Domain | Handler precondition rejection from the **`ContextBlocked`** adjacent status (only `Requested`/`Authorized` were covered) | `Generation_on_a_context_blocked_interaction_is_not_generatable_context_not_ready` (+ `StateContextBlocked()` fixture) | AC3 (AD-11) |
| 3 | Orchestrator | Provider returns **`Succeeded` with `null` content** → degraded result mapped to `GenerationError`, safety gate skipped | `A_provider_success_with_no_content_returns_generation_failed_generation_error_and_skips_safety` | AC3 (AD-11) |
| 4 | Orchestrator | Provider returns a **non-timeout failure outcome** (`GenerationError`) → straight-through `GenerationFailed` | `A_provider_generation_error_outcome_returns_generation_failed_generation_error` | AC3 |
| 5 | Orchestrator | **Safety evaluator throws** → `EvaluateSafetyAsync` catch fails closed to `Blocked`/`SafetyFailed` (only the `Blocked` *return* was covered) | `A_safety_evaluator_that_throws_fails_closed_to_safety_failed_without_leaking_content_or_the_error` | AC2/AC3 (AD-12, AD-14) |
| 6 | Orchestrator | **Policy reader throws** → `ReadPolicyAsync` catch fails closed to `NotAvailable`/`PolicyFailure`, evaluator never reached (only the `NotAvailable` *return* was covered) | `A_safety_policy_reader_that_throws_fails_closed_to_policy_failure_without_evaluating_or_leaking` | AC2/AC3 (AD-12, AD-14) |
| 7 | Orchestrator | **Empty `ProviderId`** request → `ReadBudgetAsync` guard short-circuits to `ProviderUnavailable` without reading the catalog or invoking the provider | `An_empty_provider_id_returns_provider_unavailable_without_reading_the_catalog_or_invoking_the_provider` | AC3 |
| 8 | Contracts | `AgentOutputGenerationFailed` round-trip with the **`GenerationFailed`** decision (only the `SafetyFailed` decision was round-tripped) | `Generation_failed_event_round_trips_with_the_generation_failed_decision` | AC3 (AD-2) |

The two throw-path tests (#5, #6) additionally assert the raw exception text never crosses the boundary (AD-14), matching the existing provider-throw no-leak guard.

**Checklist validation (`bmad-qa-generate-e2e-tests/checklist.md`):** API/orchestration tests generated ✅; failure/error cases covered (every fail-closed class now exercised) ✅; standard framework APIs (xUnit v3 + Shouldly + NSubstitute) ✅; all tests pass ✅; clear descriptions, no hardcoded waits/sleeps, independent tests ✅; summary updated with coverage ✅. (No UI surface in this story — generation is a server/domain step; "E2E" here = orchestrator → real aggregate cross-seam, already covered by `End_to_end_the_dispatched_generate_command_drives_the_aggregate_to_the_same_decision`.)
