# Test Automation Summary — Story 1.6 (Configure Response Mode And Approver Policy)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-24 · **Engineer:** QA automation (Administrator)
**Framework:** xUnit v3 `3.2.2` + Shouldly `4.3.0` + NSubstitute `5.3.0` (the project's existing stack; warnings-as-errors).

This is a backend domain/contracts/server module — the admin setup **UI is deferred to Story 1.8**, so there is no
browser/HTTP E2E surface yet. The end-to-end coverage here is the aggregate command→event→replay pipeline, the pure
read path, and the server orchestration/activation seams (NSubstitute doubles for the deferred ports).

## Baseline (as-built, before this run)

| Project | Tests | Result |
|---|---|---|
| Hexalith.Agents.Tests | 225 | ✅ |
| Hexalith.Agents.Contracts.Tests | 72 | ✅ |
| Hexalith.Agents.Server.Tests | 88 | ✅ |
| **Total** | **385** | ✅ 0 warn / 0 err |

The story shipped with strong coverage already. This run was a **gap-fill**: it found untested branches/contracts and
added focused tests; it did not rewrite existing tests.

## Discovered gaps → tests added (auto-applied)

| # | AC | Gap (untested branch/contract) | Test added | File |
|---|----|--------------------------------|------------|------|
| 1 | AC2 | Null `Policy` on `ConfigureAgentApproverPolicy` → the `"Approver policy is required."` guard (NRE-safety) | `Configure_null_policy_is_rejected_as_invalid_configuration` | `AgentApproverPolicyTests.cs` |
| 2 | AC2 | Normalization: a whitespace-only *ignored* field (Caller `PartyId`) is blanked to null and **stored** | `Configure_caller_source_with_whitespace_reference_normalizes_it_away_and_stores_the_policy` | `AgentApproverPolicyTests.cs` |
| 3 | AC2 | Normalization: a whitespace-only *required* field (PredefinedParty `PartyId`) → **rejected** like literal null | `Configure_predefined_party_with_a_whitespace_party_id_is_rejected_after_normalization` | `AgentApproverPolicyTests.cs` |
| 4 | AC2 | Duplicate detection runs on the **normalized** form (blank-vs-null collapse) | `Configure_sources_equal_only_after_normalization_are_rejected_as_duplicates` | `AgentApproverPolicyTests.cs` |
| 5 | AC3 | Deterministic multi-blocker **order** incl. the new response-mode gate (party→provider→mode) | `Activation_blockers_are_reported_in_the_documented_order_including_the_response_mode_gate` | `AgentResponseApproverActivationTests.cs` |
| 6 | AC3 | Provider gate appended **before** the approver gate (approver never masks an unresolved provider) | `Confirmation_activation_reports_the_provider_gate_before_the_approver_gate` | `AgentResponseApproverActivationTests.cs` |
| 7 | AC1/AC3 | Inspection read path: Confirmation + no policy surfaces `MissingApproverPolicy` | `GetStatus_confirmation_mode_without_a_policy_reports_the_missing_approver_policy_blocker` | `AgentInspectionTests.cs` |
| 8 | AC3/AC4 | Inspection purity (AD-3): Confirmation-ready agent surfaces policy fields and **no** `ApproverPolicyUnresolvable` | `GetStatus_confirmation_ready_agent_surfaces_policy_fields_and_no_unresolvable_blocker` | `AgentInspectionTests.cs` |
| 9 | AC3 | Activation re-validation runs **both legs in one envelope** (provider verdict + approver verdict together) | `Confirmation_mode_with_a_recorded_selection_populates_both_provider_and_approver_verdicts` | `AgentActivationApproverRevalidationTests.cs` |

## Result (after gap-fill)

| Project | Tests | Δ | Result |
|---|---|---|---|
| Hexalith.Agents.Tests | 233 | +8 | ✅ |
| Hexalith.Agents.Contracts.Tests | 72 | +0 | ✅ |
| Hexalith.Agents.Server.Tests | 89 | +1 | ✅ |
| **Total** | **394** | **+9** | ✅ 0 warn / 0 err |

## Coverage vs Acceptance Criteria

- **AC1 (response mode recorded + version + future-only):** ✅ configure Automatic/Confirmation, reject `Unknown`,
  idempotent no-op, future-only change, not-found/unauthorized, replay, read-path surfacing.
- **AC2 (all V1 approver sources + facilitator owner resolver):** ✅ all four source kinds, structural rejects,
  empty-storable, idempotency, **+ null-policy, whitespace normalization, normalized-dedup (added)**.
- **AC3 (confirmation fails closed when a source is unresolvable):** ✅ each verdict state → `ApproverPolicyUnresolvable`,
  absent/numeric/aliased verdict fail-closed, verdict precedence (server), **+ deterministic blocker ordering and the
  both-legs revalidation envelope (added)**.
- **AC4 (policy version + disclosure category for basis reporting):** ✅ version bumps, prior events preserved,
  disclosure on event/state/status, JSON round-trip, secret/PII member guard.

## Checklist validation (`checklist.md`)

- [x] API/handler tests generated (aggregate handlers, server orchestrations, ports) — happy path + critical errors
- [x] E2E pipeline tests present (`ProcessAndApply` command→event→replay; lifecycle journey) — UI E2E deferred to 1.8
- [x] Standard framework APIs (xUnit v3 + Shouldly, no raw `Assert.*`)
- [x] Happy path covered · [x] critical error cases covered (fail-closed, null, normalization, ordering)
- [x] All generated tests run successfully (394/394)
- [x] Semantic, typed assertions (domain objects / enums / verdicts — not stringly-typed)
- [x] Clear PascalCase BDD descriptions · [x] no hardcoded waits/sleeps · [x] tests independent (no order dependency)
- [x] Summary created with coverage metrics; tests saved in the existing test projects

## How to run

From `Hexalith.Agents/`:

```bash
dotnet build Hexalith.Agents.slnx --configuration Release          # 0 warnings / 0 errors
dotnet test tests/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj --configuration Release
```

## Next steps

- The live `IApproverPolicyResolver` legs (Tenants projection / Conversations facilitator / Parties) stay **deferred**
  (`DeferredApproverPolicyResolver`); add integration tests against the real resolver in the read-model/topology story.
- Browser/UI E2E coverage for `response-mode-toggle` / `approver-policy-builder` lands with **Story 1.8** (FrontComposer).
