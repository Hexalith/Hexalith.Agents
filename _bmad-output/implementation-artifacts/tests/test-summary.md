# Test Automation Summary — Story 4.3: Expose Operational Status And Admin Workflows

**Workflow:** `bmad-qa-generate-e2e-tests`
**Date:** 2026-06-25
**Engineer:** QA automation (Administrator)
**Story:** `_bmad-output/implementation-artifacts/4-3-expose-operational-status-and-admin-workflows.md`
**Mode:** Auto-apply all discovered coverage gaps. **No production code changed** — test-only additions.

## Framework Detected

- .NET 10 Blazor/FrontComposer UI module. UI tests: bUnit `2.8.4-preview` + xUnit v3 `3.2.2` + Shouldly `4.3.0` +
  NSubstitute `5.3.0` (AngleSharp). Contracts tests: xUnit v3 + Shouldly. No new framework introduced — reused
  `AgentsTestContext`, `AgentUiTestData`, and `StubAgentsLocalizer`.
- `dotnet test` (VSTest) hits the known sandbox failure `SocketException (13)`; validation used serialized
  `dotnet build … -c Release -m:1` + running the built xUnit v3 executables directly (`DiffEngine_Disabled=true`).

## Scope

Story 4.3 is the consolidated **operational-status + audit UI** story (read-only surfaces over the fail-closed UI
gateway seam; the live read-model binding is deferred — AD-16, `Server/Projections/` is `.gitkeep`-only). It shipped with
a "one suite per AC" set (UI 840, Contracts 316, all green at baseline). This QA pass audited the new surface against the
ACs and the checklist, then generated tests for behaviour no existing test asserted.

## Coverage gaps discovered & closed

| # | Gap (AC / guardrail) | Closed by |
|---|---|---|
| G1 | AC1/UX-DR9: `OperationalStatusPresentation.GroupForBlocker` only exercised indirectly via one panel case — no per-blocker group assertion | **new theory** `Activation_blocker_maps_to_its_recovery_action_group` (11 cases) |
| G2 | AC1: public `ToReadinessState` mapping had **zero** coverage | **new theory** `Agent_readiness_status_maps_to_the_canonical_ui_readiness_state` (7 cases) |
| G3 | AD-14 totality: the "every switch is total" / `Unknown=0` safe-default claim was untested (`Subtle` + non-null icon, audit `Unknown`→`None`) | **new fact** `Unknown_sentinels_resolve_to_a_safe_subtle_default_with_a_non_null_icon` |
| G4 | AC1: several canonical states the story enumerates were unasserted (readiness `Disabled`/`Checking`, call `Requested`/`Generated`, provider `Disabled`/`Degraded`, proposal `Abandoned`/`Generated`) | extended the 4 mapping theories |
| G5 | AC4: the panel renders a visible `(n)` count, and zero-count outcomes are omitted — neither was asserted | **new** `Panel_renders_outcome_counts_and_omits_zero_count_outcomes` |
| G6 | AC2: the page `Empty` branch (`HasSignals=false`) and the `Stale(null)`→`Stale`-surface branch (distinct from `Degraded`) were untested | **new** `Page_renders_empty_surface_…` + `Page_renders_stale_surface_with_refresh_…` |
| G7 | AC1/AD-5: the audit `PostingOutcomeKey` never-collapse (Posted vs Failed) was untested at the field level | **new** `Panel_renders_posting_outcome_distinctly_for_posted_and_posting_failed` |
| G8 | AC1: the `DisplayId` "None" fallback and the approval-row omission (no approval recorded) were untested | **new** `Panel_renders_the_none_affordance_for_absent_optional_ids_and_omits_the_approver_rows` |
| G9 | AC3: the panel-level governance-blocker section (consumed by Story 4.4) was untested (only the landing copy was) | **new** `Panel_surfaces_the_named_governance_blocker_metadata_only` |
| G10 | AC2/AD-12: the audit `NotFound`→`Empty` branch (no cross-tenant existence leak) was untested | **new** `Detail_renders_empty_surface_when_the_evidence_is_not_found` |
| G11 | AC2: the `Degraded`→refresh affordance and the surface-state `Detail` message override were untested | **new** `Degraded_surface_offers_a_refresh` + `Surface_state_detail_overrides_the_default_message` |

## Generated / extended tests (UI) — `tests/Hexalith.Agents.UI.Tests/`

### AC1 — presentation mapper — `OperationalStatusPresentationTests.cs`
- [x] `Activation_blocker_maps_to_its_recovery_action_group` (**new**, 11 cases) — every blocker groups by recovery
  **action** (party-identity → link, provider → configure, all policy blockers → fix policy), incl. the `Unknown` default.
- [x] `Agent_readiness_status_maps_to_the_canonical_ui_readiness_state` (**new**, 7 cases) — each status → its UI readiness
  state; `Unknown`→`Unknown`.
- [x] `Unknown_sentinels_resolve_to_a_safe_subtle_default_with_a_non_null_icon` (**new**) — totality / safe default.
- [x] Extended the four `…maps_to_action_group_and_role` theories with the unasserted canonical AC1 states.

### AC2 / AC4 — panel + page — `OperationalStatusSurfaceTests.cs`
- [x] `Panel_renders_outcome_counts_and_omits_zero_count_outcomes` (**new**) — visible `(n)` suffix; zero-count omitted;
  readiness + audit chips always present.
- [x] `Page_renders_empty_surface_when_the_authorized_summary_has_no_signals` (**new**).
- [x] `Page_renders_stale_surface_with_refresh_when_no_trustworthy_summary` (**new**).

### AC1 / AC2 / AC3 — audit panel + page — `AuditEvidenceSurfaceTests.cs`
- [x] `Panel_renders_posting_outcome_distinctly_for_posted_and_posting_failed` (**new**) — AD-5 never-collapse.
- [x] `Panel_renders_the_none_affordance_for_absent_optional_ids_and_omits_the_approver_rows` (**new**).
- [x] `Panel_surfaces_the_named_governance_blocker_metadata_only` (**new**).
- [x] `Detail_renders_empty_surface_when_the_evidence_is_not_found` (**new**).

### AC2 — shared surface state — `AccessibilityTests.cs`
- [x] `Degraded_surface_offers_a_refresh` (**new**).
- [x] `Surface_state_detail_overrides_the_default_message` (**new**).

## Coverage

| Project | Before | After | Added | Result |
|---|---|---|---|---|
| UI (`Hexalith.Agents.UI.Tests`) | 840 | **876** | **+36** | ✅ 0 failed |
| Contracts (`Hexalith.Agents.Contracts.Tests`) | 316 | 316 | 0 | ✅ already complete (round-trip / ordinal / no-leak / fail-closed / query discriminators) |
| Server (`Hexalith.Agents.Server.Tests`) | 351 | 351 | 0 | ✅ no server code touched |

- **Presentation mapper:** partial → every canonical status + every blocker + every `Unknown` sentinel + both public
  helpers (`ToReadinessState`, `GroupForBlocker`).
- **UI data-states (AgentSurfaceKind):** all eight kinds incl. the `Empty` / `Stale(null)` / `Degraded` page branches,
  the `Degraded` refresh, and the `Detail` override.
- **Audit-evidence panel:** posting-outcome non-collapse, "None" fallback, approval-row omission, governance blocker,
  `NotFound`→`Empty`.

## Build & run

- `dotnet build Hexalith.Agents.slnx -c Release -m:1` → **Build succeeded, 0 Warning(s), 0 Error(s)** (warnings-as-errors;
  CA2007/nullable clean).
- `Hexalith.Agents.UI.Tests` built xUnit v3 executable run directly → **Total: 876, Failed: 0**.

## Validation checklist (`bmad-qa-generate-e2e-tests/checklist.md`)

- [x] API/contract tests — already complete for the additive summary contract; UI gateway seam covered fail-closed.
- [x] E2E (bUnit) tests generated for the operational-status + audit UI.
- [x] Tests use standard framework APIs (bUnit + xUnit v3 + Shouldly + NSubstitute).
- [x] Happy path covered; [x] critical error cases (permission-denied / unavailable / stale / not-found / empty).
- [x] All generated tests run successfully (0 failed).
- [x] Semantic/accessible locators (`data-testid`, `role`, `aria-live`, visible text); clear `Subject_scenario_expectation`
  names; no hardcoded waits (uses `WaitForAssertion`); tests order-independent.
- [x] Test summary created; tests saved to the established suites; coverage metrics included.

## Notes & next steps

- Surfaces are tested against the fail-closed default DI graph (the live operational read-model binding is deferred —
  AD-16). When that binding lands, add integration tests over the live `IOperationalStatusGateway`/`IAuditEvidenceGateway`
  → BFF/API → read-model path.
- Story 4.4 consumes the named launch-readiness governance blocker surfaced here (now panel-level tested).
- Run the suite in CI alongside the existing per-AC suites.
