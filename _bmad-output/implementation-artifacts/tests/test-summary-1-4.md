# Test Automation Summary — Story 1.4: Link `hexa` To A Party Identity

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-24 · **Engineer:** QA automation (Administrator)
**Story:** `_bmad-output/implementation-artifacts/1-4-link-hexa-to-a-party-identity.md` (status: review)
**Framework (auto-detected):** xUnit v3 `3.2.2` + Shouldly `4.3.0` + NSubstitute `5.3.0` — `net10.0`, Central Package Management, warnings-as-errors. Used the project's existing test patterns.

## Scope & Method

Story 1.4 was already implemented with an extensive dev-authored suite (228 tests). This run is a **gap
analysis + auto-apply**: every acceptance criterion and each production branch of the as-built code was traced
against existing coverage, and the uncovered branches / unlocked invariants were filled with new tests.

This module has **no public HTTP API** (traffic enters through the EventStore gateway) and **no UI yet**, so:

- **"API tests"** map to the Server **adapter** (`PartiesAgentPartyDirectory`) and **orchestration**
  (`AgentPartyIdentityOrchestrator`) driven over substituted Parties clients / dispatch seam.
- **"E2E tests"** map to the pure-aggregate **`ProcessAndApplyAsync` journeys** (JSON envelope → reflection
  dispatch → typed handler → `Apply` replay → inspection read path), which already exist and stay green.

## Coverage Result

| Test project | Before | After | Δ |
|---|---|---|---|
| `Hexalith.Agents.Tests` (domain / aggregate E2E) | 142 | 144 | +2 |
| `Hexalith.Agents.Contracts.Tests` | 49 | 49 | 0 |
| `Hexalith.Agents.Server.Tests` (adapter / orchestration) | 37 | 44 | +7 |
| **Total** | **228** | **237** | **+9** |

`dotnet build Hexalith.Agents.slnx -c Release` → **0 warnings / 0 errors** (warnings-as-errors).
All three suites: **Passed! 237/237, 0 failed, 0 skipped.**

## Gaps Discovered & Filled (auto-applied)

### Domain — `tests/Hexalith.Agents.Tests/AgentPartyIdentityTests.cs`
- [x] **Deterministic blocker order (AC4).** `Activation_reports_the_party_gate_last_in_the_documented_deterministic_order` — when every gate fails, blockers are exactly `[MissingDisplayName, MissingInstructions, MissingPartyIdentity]`, locking the order the policy documents but no test asserted (prior tests used `ShouldContain`).
- [x] **Replace fail-closed symmetry (AC2).** `Replace_with_absent_verdict_fails_closed_to_unknown_and_keeps_the_existing_link` — Link had absent/unparseable-verdict tests; Replace only had a non-`Valid` case. The absent-verdict path is now covered for Replace too.

### Orchestration — `tests/Hexalith.Agents.Server.Tests/AgentPartyIdentityOrchestratorTests.cs`
- [x] **Auditable rejection carries the requested id (AC2).** `Non_valid_verdict_dispatches_a_well_formed_command_carrying_the_requested_party_id` — when validation yields no id (`Missing` → null), the `validation.PartyId ?? request.PartyId` fallback is exercised; the dispatched command payload + outcome carry `party-001`.
- [x] **Missing-id fail-fast.** `Link_existing_without_a_party_id_throws_and_dispatches_nothing` — the previously uncovered `RequireExistingPartyId` guard throws `ArgumentException` and dispatches nothing.
- [x] **Default org label (AD-7).** `Provision_new_without_a_label_defaults_the_label_from_the_agent_id` — the `DefaultOrganizationLabel(agentId)` branch is exercised; the provisioning request gets `Agent hexa` derived from the agent id, no PII. (`Request` test helper extended with an `organizationLabel` parameter.)

### Adapter — `tests/Hexalith.Agents.Server.Tests/PartiesAgentPartyDirectoryTests.cs`
- [x] **Provisioning transport failure → Unavailable (AD-12).** `Provisioning_unexpected_transport_failure_maps_to_unavailable` — validate-existing already had this symmetry; provisioning did not.
- [x] **Cancellation propagates, not swallowed (AD-12).** `Validate_existing_propagates_cancellation_instead_of_mapping_to_a_verdict` and `Provisioning_propagates_cancellation_instead_of_mapping_to_a_verdict` — lock the `when (ex is not OperationCanceledException)` filter so a caller-requested cancellation is never reclassified as a fail-closed `Unavailable` verdict.

### Deferred binding — `tests/Hexalith.Agents.Server.Tests/DeferredAgentCommandDispatcherTests.cs` (new file)
- [x] **Fail-loud placeholder.** `Dispatch_throws_not_supported_until_the_live_binding_is_wired` — the deferred dispatcher (live DAPR/EventStore binding deferred, mirroring Story 1.2/1.3) must throw `NotSupportedException` rather than silently swallow a command.

## Acceptance-Criteria Coverage (post-run)

- **AC1 — store only `PartyId`, never PII.** Aggregate link/replace store-id tests; contracts no-PII reflection guard + `HasPartyIdentity`-not-`PartyId` status view; adapter returns only `{ Status, PartyId }` with PII sentinels asserted absent. ✅
- **AC2 — fail closed on every non-`Valid`/absent/unparseable verdict.** All six verdicts + absent + unparseable for Link; absent + non-`Valid` for Replace; adapter verdict-mapping table incl. stale→`Unavailable`, not-found→`Missing`, 401/403→`Unauthorized`, transport→`Unavailable`; cancellation propagates. ✅
- **AC3 — exactly one active identity.** Idempotent re-link, distinct-link rejection, explicit replace (+ null previous), replace-same no-op, single-identity replay. ✅
- **AC4 — distinct readiness gate, presence without PII.** Blocked-then-unblocked activation, documented blocker order, `HasPartyIdentity` on the status view (no `PartyId`), gate serialization + preserved enum ordinals. ✅

## Notes / Out of Scope

- **No command-payload round-trip tests** were added to `Contracts.Tests`: the module's convention round-trips
  events/rejections/views only, and command deserialization is already exercised by the orchestration tests
  (`JsonSerializer.Deserialize<…>(dispatched.Payload)`). Adding them would deviate from house style.
- **`Ambiguous` verdict** is part of the vocabulary but unreachable via the current `GetPartyAsync(partyId)`
  adapter path (per Dev Notes); it is covered at the aggregate-rejection level, not the adapter level.
- **Live Parties-client + command-dispatch runtime binding** remains deferred (Story 1.4 completion note); the
  decision logic is fully unit-tested via substitutes, and the deferred dispatcher's fail-loud contract is now
  locked by a test.

## Validation (checklist)

- [x] API-equivalent (adapter/orchestration) tests generated · [x] E2E-equivalent (aggregate journey) tests present
- [x] Standard framework APIs (xUnit v3 / Shouldly / NSubstitute) · [x] Happy path + critical error cases
- [x] All generated tests run successfully (237/237) · [x] No hardcoded waits/sleeps · [x] Tests independent
- [x] Clear PascalCase BDD descriptions, no raw `Assert.*` · [x] Summary saved with coverage metrics

Run from `Hexalith.Agents/`:

```bash
dotnet build Hexalith.Agents.slnx --configuration Release        # 0 warnings / 0 errors
dotnet test tests/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj --configuration Release
```

**Expected:** all tests pass ✅ (237/237 verified this run).
