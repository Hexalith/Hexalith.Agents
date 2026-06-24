# Test Automation Summary — Story 1.5: Select Provider And Model For `hexa`

**Workflow:** `bmad-qa-generate-e2e-tests`
**Date:** 2026-06-24
**Engineer:** QA automation (Administrator)
**Mode:** Auto-apply discovered gaps
**Story:** `_bmad-output/implementation-artifacts/1-5-select-provider-and-model-for-hexa.md` (status: review)
**Framework (reused):** xUnit v3 `3.2.2` + Shouldly `4.3.0` + NSubstitute `5.3.0` — `net10.0`, Central Package Management, warnings-as-errors.

> Per-story summary written alongside the Story 1.1 summary (`test-summary.md`) to preserve prior records.

## Nature of this feature

Backend event-sourcing + server-orchestration story. **No UI** (explicitly out of scope until Story 1.8) and no HTTP routes yet (the live read-model / command-gateway bindings are deferred). So conventional API/browser E2E tests do not apply — the project's analog is **aggregate-pipeline tests** (`AgentAggregate.Handle` + replay through `Apply`) and **server-orchestration tests** (`AgentProviderSelectionOrchestrator` / `AgentActivationProviderRevalidation` via a substituted `IProviderCatalogReader`/`IAgentCommandDispatcher`). These are the end-to-end automated tests for the feature.

## Coverage assessment

The dev-story suite (298 tests) already covered the four ACs well: AC1 selection recording + safe status surfacing, AC2 aggregate fail-closed on every non-`Valid` verdict (+ verdict-parse hardening), AC3 idempotent re-select / changed selection / append-only history, AC4 unauthorized + cross-tenant fail-closed reads, plus contracts round-trips for both extended views.

QA gap analysis (traced to ACs) found **5 genuine, uncovered branches**, all on the security-critical fail-closed verdict logic (AC2) and the idempotency key (AC3/AD-13). All were auto-applied.

## Gaps discovered & auto-applied

| # | AC | Gap (was uncovered) | Test(s) added |
|---|----|---------------------|---------------|
| 1 | AC2 | `Degraded`/`Failed`/`Unknown` provider status → `Disabled` verdict (only explicit `Disabled` was tested; AC2 + spine enumerate "missing, disabled, **or failed**") | `Any_non_enabled_entry_status_maps_to_disabled` (Theory ×3) |
| 2 | AC2 | `MaxOutputTokenLimit <= 0` → `MissingCapabilityMetadata` (only context=0 & output>context were tested — the output-floor clause itself was unproven) | `Non_positive_output_limit_maps_to_missing_capability_metadata` |
| 3 | AC2 | Invalid `TimeoutPolicy` (`RequestTimeoutMilliseconds <= 0`, `MaxRetries < 0`) → `MissingCapabilityMetadata` — the AC's "timeout metadata" clause was **wholly untested** | `Non_positive_request_timeout_maps_to_missing_capability_metadata`, `Negative_max_retries_maps_to_missing_capability_metadata` |
| 4 | AC2/AC4 | Activation-revalidation ("or activate" path) trust model: a client-forged `provider:selectionValidation`/`actor:agentsAdmin` is stripped and repopulated from the trusted verdict (proven only for the *select* path) | `Activation_revalidation_strips_client_forged_reserved_extensions_and_repopulates_the_trusted_verdict` |
| 5 | AC3/AD-13 | Single-field selection change emits a new event, not a NoOp — the existing changed-selection test mutates model+version together, leaving the per-field idempotency boundary unproven | `Reselect_same_provider_and_model_with_only_a_changed_capability_version_emits_a_new_event`, `Changing_only_the_provider_emits_a_new_event_and_bumps_version` |

### Generated tests

- [x] `tests/Hexalith.Agents.Server.Tests/AgentProviderSelectionOrchestratorTests.cs` — **+7** (gaps 1–4): every non-Enabled status (×3), non-positive output limit, two invalid-timeout-policy cases, and the activation-path reserved-extension stripping.
- [x] `tests/Hexalith.Agents.Tests/AgentProviderSelectionTests.cs` — **+2** (gap 5): capability-version-only change and provider-only change each emit a new `AgentProviderModelSelected` and bump `ConfigurationVersion`.

All additions reuse the existing fixtures/helpers (`RunVerdict`, `SuccessRead`, `ValidEntry`, `StateWithSelectedProvider`, `SelectEnvelope`) — no new abstractions.

## Coverage metrics

- **Acceptance criteria:** AC1 ✅, AC2 ✅, AC3 ✅, AC4 ✅.
- **Verdict precedence (AD-10/AD-12):** all 8 `ProviderSelectionValidationStatus` outcomes now exercised — `Missing`, `Unauthorized`, `Unavailable`, `Disabled` (now incl. Degraded/Failed/Unknown), `NotTextGenerationCapable`, `NotConfigured`, `MissingCapabilityMetadata` (now incl. context, output, **and** timeout sub-clauses), `Valid`.
- **Trust model (reserved-key stripping):** now proven on **both** the select and the activate paths.
- **Idempotency key (AD-13):** the full `(ProviderId, ModelId, ProviderCapabilityVersion)` triple now has per-field change coverage.

## Results

| Project | Before | After | Δ |
|---------|--------|-------|---|
| `Hexalith.Agents.Tests` (domain) | 178 | **180** | +2 |
| `Hexalith.Agents.Contracts.Tests` | 60 | 60 | — |
| `Hexalith.Agents.Server.Tests` | 60 | **67** | +7 |
| **Total** | **298** | **307** | **+9** |

- `dotnet build Hexalith.Agents.slnx --configuration Release` → **0 Warning(s) / 0 Error(s)** (warnings-as-errors).
- All **307** tests pass ✅ — 0 failed, 0 skipped.

## Checklist validation (`checklist.md`)

- [x] API/service tests generated (orchestration verdict-mapping — the API analog)
- [x] E2E tests present (aggregate-pipeline journeys; no UI in this story)
- [x] Tests use standard framework APIs (xUnit v3 + Shouldly + NSubstitute)
- [x] Happy path covered (pre-existing) + critical fail-closed error cases added
- [x] All generated tests run successfully
- [x] Clear PascalCase BDD names, AC-traced comments
- [x] No hardcoded waits/sleeps
- [x] Tests independent (no order dependency)
- [x] Summary created with coverage metrics

## Verification commands

```bash
# from Hexalith.Agents/
dotnet build Hexalith.Agents.slnx --configuration Release   # 0 Warning(s), 0 Error(s)
dotnet test  tests/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj                   --configuration Release
dotnet test  tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release
dotnet test  tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj     --configuration Release
```

## Next steps

- Run in CI (no new packages or config required).
- When the deferred DAPR read-model / command-gateway bindings land, add a live integration test driving `IProviderCatalogReader` against rehydrated `ProviderCatalogState` end-to-end (replacing the `DeferredProviderCatalogReader` placeholder).
