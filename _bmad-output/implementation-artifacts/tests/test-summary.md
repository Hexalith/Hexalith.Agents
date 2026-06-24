# Test Automation Summary — Story 3.3: Edit Proposed Reply Versions

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-24 · **Engineer role:** QA automation (Administrator)
**Story:** `_bmad-output/implementation-artifacts/3-3-edit-proposed-reply-versions.md` (status: review)
**Test framework (detected):** .NET 10 / Blazor — xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0`, bUnit (no Playwright/Cypress JS stack). Run via the built executables directly (VSTest TCP socket unavailable in the sandbox). Used the existing framework; added no dependency.

## Scope

Story 3.3 shipped with a comprehensive existing suite across all four layers (Contracts, Domain, Server, UI). This QA pass did **not** regenerate that suite; it performed a coverage-gap analysis — every Story 3.3 production behavior, branch, enum, guard and fail-closed path cross-referenced against every existing test — and **auto-applied tests for the discovered gaps only**. No production code was changed.

## Baseline (before this pass)

| Project | Total | Failed |
| --- | --- | --- |
| Hexalith.Agents.Contracts.Tests | 242 | 0 |
| Hexalith.Agents.Server.Tests | 275 | 0 |
| Hexalith.Agents.Tests (domain) | 576 | 0 |
| Hexalith.Agents.UI.Tests | 459 | 0 |
| **Total** | **1552** | **0** |

Release build: **0 warnings / 0 errors** (warnings-as-errors).

## Coverage gap analysis — discovered gaps and the tests added

Four genuinely-uncovered behaviors were found. All are in Story 3.3 scope (the editor's shipped output/error surfaces and the AC4 audit read-result). The Story 3.7-owned Esc-without-commit / version-history accessibility was deliberately **not** duplicated.

### Contracts — `AgentInteractionProposalEditContractsTests.cs` (+4)

The AC4 audit read-result `AgentProposalEditEvidenceResult` had only its **view** round-trip tested; the result's own fail-closed factory contract (AD-12 — a denied/absent inspection must reveal nothing) was untested.

| Test | Gap closed | AC |
|------|-----------|----|
| `Edit_evidence_result_success_carries_the_safe_view` | `Success(view)` ⇒ `Status=Success`, evidence present | AC4 |
| `Edit_evidence_result_not_authorized_is_fail_closed_with_no_evidence` | `NotAuthorized()` ⇒ `Status=NotAuthorized`, `Evidence == null` | AC4 / AD-12 |
| `Edit_evidence_result_not_found_reveals_no_cross_tenant_existence` | `NotFound()` ⇒ `Status=NotFound`, `Evidence == null` (no cross-tenant disclosure) | AC4 / AD-12 |
| `Edit_evidence_result_round_trips_with_its_view` | result survives System.Text.Json (durable read contract) | AC4 |

### UI — `ProposalEditorTests.cs` (+3)

The editor's status branch only exercised `Edited` and `NotAuthorized`; its `Unavailable` error surface and both component output callbacks (`OnEdited`, `OnCancel`) were untested.

| Test | Gap closed | AC |
|------|-----------|----|
| `An_unavailable_gateway_shows_the_unavailable_status` | faulted seam (`ProposalEditResult.Unavailable()`) renders the distinct `Status.Unavailable`, never a fabricated success | AC3 (fail-closed surface) |
| `A_successful_save_raises_on_edited_with_the_result_so_the_host_can_refresh` | a successful save raises `OnEdited` with the safe result (id only, no content) — the component's output contract to its 3.7 host | AC1 / AC3 |
| `Clicking_cancel_invokes_the_on_cancel_callback_without_saving` | the Cancel button raises `OnCancel` and never calls the edit seam | AC3 |

### Already covered (no action needed)

Authorized edit appends an immutable version + prior versions preserved · every failure combination → `ProposalEditFailed` with the mapped reason · not-editable rejections (never-requested / no-pending-proposal, AC2) · idempotent terminal no-op + second distinct edit · `Evaluate`/`Decide` no-drift · reflection-dispatch + JSON round-trip · deterministic edited-version identity (distinct purpose tag, retry dedupe) · orchestrator fail-closed authorization (6 non-Resolved sources, null/empty policy, resolver-throws, all-deferred), `OperationCanceledException` propagation, reserved-trust-key stripping, content confinement · ordinal stability + nullable backward-compat · badge/presentation totality · en/fr localization parity · deferred-gateway fail-closed · accessibility (editor region name, polite live region) · editor render (editable vs read-only, distinct version labels, no content in accessible names/test ids, empty-edit guard).

## Result (after this pass)

| Project | Before | After | Δ |
| --- | --- | --- | --- |
| Hexalith.Agents.Contracts.Tests | 242 | **246** | +4 |
| Hexalith.Agents.Server.Tests | 275 | **275** | 0 (regression check) |
| Hexalith.Agents.Tests (domain) | 576 | **576** | 0 (regression check) |
| Hexalith.Agents.UI.Tests | 459 | **462** | +3 |
| **Total** | 1552 | **1559** | **+7** |

**Result: Passed — Failed: 0, Skipped: 0** across all four projects. Release build after additions: **0 warnings / 0 errors**. All 7 new tests verified passing under explicit `-method` filters.

## AC coverage after this pass

- **AC1** — authorized edit appends an immutable version, prior versions preserved: covered (aggregate, lifecycle E2E, orchestrator, editor save + new `OnEdited`). ✅
- **AC2** — terminal/non-pending proposals cannot be edited, no new version: covered (aggregate rejections, fail-closed orchestrator authorization, lifecycle E2E). ✅
- **AC3** — versions labeled distinctly, content never leaks, fail-closed UI surfaces: covered (presentation totality, badge, en/fr localization, editor render/labels/status incl. new `Unavailable` + `Cancel` paths, accessibility). ✅
- **AC4** — auditable without overwriting prior content, idempotent: covered (deterministic identity, idempotent no-op, content-confinement no-leak, and the now-covered audit read-result fail-closed factories). ✅

## Checklist validation (`bmad-qa-generate-e2e-tests/checklist.md`)

- [x] API / write-path tests generated (contract + orchestrator + aggregate command/event surface)
- [x] E2E / component tests generated (bUnit editor + lifecycle E2E)
- [x] Tests use standard framework APIs (xUnit v3 / Shouldly / NSubstitute / bUnit; no raw `Assert.*`)
- [x] Happy path covered (authorized edit + successful save)
- [x] 1–2 critical error cases covered (terminal rejection, fail-closed auth, Unavailable, NotAuthorized, NotFound)
- [x] All tests run successfully — **1559 total, 0 failed**
- [x] Proper locators (semantic `data-testid` / `role` / `aria` queries)
- [x] Clear descriptions (`Method_with_context_behavior` / `Subject_scenario_expectation`)
- [x] No hardcoded waits/sleeps (bUnit `WaitForAssertion`, no `Thread.Sleep`)
- [x] Tests are independent (each builds its own state/render; no order dependency)
- [x] Summary created with coverage metrics (this file)

## Files changed (tests only — no production change)

- `Hexalith.Agents/tests/Hexalith.Agents.Contracts.Tests/AgentInteractionProposalEditContractsTests.cs` (+1 `EditView` fixture, +4 tests)
- `Hexalith.Agents/tests/Hexalith.Agents.UI.Tests/ProposalEditorTests.cs` (+3 tests)

## Notes / boundaries

- No production code changed — the gaps were purely missing test coverage. The two write-side/regression suites (domain + server) were re-run to confirm (not modify) green status.
- The live server read-model / projection / query-handler / BFF binding for `AgentProposalEditEvidenceResult` remains **deferred to Epic 4**; the fail-closed factory tests added here lock its contract ahead of that binding.
- Story 3.7 will host the editor and complete the Esc-without-commit / version-history / live-region accessibility — intentionally not duplicated here.

## Next steps

- Run the four projects in CI individually (never solution-level `dotnet test`; build with `-m:1`).
- When Epic 4 wires the live edit-evidence read path, add integration/API tests against the real read-model + the tenant-isolation denial audit.
