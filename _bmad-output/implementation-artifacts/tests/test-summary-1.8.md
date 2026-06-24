# Test Automation Summary — Story 1.8 (Admin Setup UI And Readiness Overview)

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-24 · **Engineer:** QA automation (Administrator)
**Story:** `_bmad-output/implementation-artifacts/1-8-admin-setup-ui-and-readiness-overview.md`
**Framework detected & reused:** bUnit `2.8.4-preview` + xUnit v3 `3.2.2` + Shouldly `4.3.0` + NSubstitute (component / E2E-style tests for the Blazor + FrontComposer Razor library `Hexalith.Agents.UI`).

> Note: the workflow's default output path (`tests/test-summary.md`) already holds Story 1.1's summary; this run is
> written to a story-scoped file to preserve that record.

## Scope

Story 1.8 is the FrontComposer admin-setup UI only. There is **no live API / read path** (the four query contracts
have no live handler; `Hexalith.Agents.Client` is an empty shell — deferred to Epic 4 / 4.1 / 4.3). The UI binds to a
UI-side gateway seam (`IAgentSetupGateway` / `IProviderCatalogGateway`) with fail-closed `Deferred*` placeholders.
Therefore **no HTTP/API endpoint tests are applicable**; the gateway seam is exercised via NSubstitute in component
tests, and the deferred placeholders are covered by direct fail-closed unit tests.

## Result

| Suite | Tests | Result |
|---|---|---|
| `Hexalith.Agents.UI.Tests` (baseline before this run) | 56 | ✅ pass |
| **`Hexalith.Agents.UI.Tests` (after gap fill)** | **156** | ✅ **pass** |
| `Hexalith.Agents.Server.Tests` (boundary / direction / centralization / structural guards) | 93 | ✅ pass (no regression) |
| `Hexalith.Agents.Contracts.Tests` | 81 | ✅ pass (no regression) |

Build: `dotnet build … --configuration Release` — **0 warnings / 0 errors** (warnings-as-errors).

## Gaps Discovered & Auto-Applied

The existing 56-test suite (one suite per AC) covered the pure mapping logic, badge conformance, nav registration,
and the two simplest page paths. The following **coverage gaps** were discovered against the ACs/implementation and
**auto-applied** as new tests (+100 cases, 56 → 156):

### AC2 — Agents overview page (`AgentsOverviewTests.cs`, new)
The whole overview page had **no content test**. Added: callable agent → Success badge + "Yes" callability;
**Active-but-blocked agent → not callable + blockers listed grouped by recovery action** (the active≠callable crux,
UX-DR9/20); provider reference vs "none"; pending-proposals / recent-failures render "not available yet" (never
fabricated counts, UX-DR2-vs-AC2); permission-denied and agent-not-found / empty surfaces.

### AC4 — Approver policy page (`ApproverPolicyTests.cs`, new)
The entire page had **zero tests**. Added: Automatic mode → not-applicable (never blocked); **Confirmation + no
policy → fail-closed "blocked" state** (UX-DR5, AD-12); Confirmation + policy → presence/version/disclosure + all
four V1 source kinds (AD-8), `Unknown` never offered; permission-denied surface; constrained layout in shell.

### AC4 — Configuration form (`AgentConfigurationTests.cs`, extended)
Added: loaded response-mode reflected in the toggle; content-safety presence/version + both mode overrides rendered
(never policy content); approver summary + link to the builder; inline activation-blocker list; agent-not-found
empty surface.

### AC3 — Provider catalog (`ProviderCatalogTests.cs`, extended)
Added: safe capability labels + token/timeout columns (only set flags appear); no-capability entry → "None";
`ListEntriesAsync` requested with `includeDisabled: true`; **selectable-only filter → distinct filtered-empty surface
+ reset restores the grid** (UX-DR30, AC6).

### AC5 / Task 3 — Localization completeness (`LocalizationResourceTests.cs`, new)
The component tests use a key-returning stub localizer, so a missing `.resx` entry could not be detected. Added tests
that bind to the **real embedded resources** and assert a non-empty **whole string** exists for **every** enum value
(readiness states, provider states, all 11 activation blockers, lifecycle, response mode, disclosure, source kind,
model status, configuration state, capability flags) and every surface kind — in **both English and French** (fr
satellite parity). 69 keys verified per culture.

### AC1 — Navigation gating & metadata (`AgentsNavigationTests.cs`, extended)
Added: **authenticated-but-not-in-policy** user sees no setup links (proves genuine policy gating, not mere
authentication); manifest + every entry carry `Resource` / `TitleKey` localization metadata with the expected keys.

### AC6 — Accessibility across surfaces (`AccessibilityTests.cs`, extended)
Heading-focus/landmark a11y was only proven on the overview. Added focusable-heading (`tabindex="-1"`) checks for the
configuration, provider-catalog, and approver-policy pages inside the shell, plus the stale-surface refresh
affordance.

### AD-12 — Deferred gateway fail-closed (`DeferredGatewayTests.cs`, new)
The deferred placeholders are never exercised by the substituted component tests. Added direct unit tests asserting
they fail closed: `GetStatusAsync` / `GetConfigurationAsync` → `NotAuthorized` with null Agent;
`ListEntriesAsync(true|false)` → `NotAuthorized` with no entries.

## Coverage

- **Setup surfaces (UI features): 4 / 4** fully covered (Agents overview, hexa configuration, Provider catalog,
  Approver policy) — up from 2 / 4 with page-content tests.
- **Shared components: 5 / 5** (AgentReadinessBadge, ProviderStatusBadge, ResponseModeToggle, AgentSurfaceState,
  AgentReadiness mapping).
- **Acceptance criteria: AC1–AC6** each have dedicated tests + composition (`AddAgentsUi`) and deferred-gateway
  fail-closed coverage.
- **Localization: 69 enum-derived keys × 2 cultures** verified against the real resources.
- **API endpoints: N/A** (live read path deferred to Epic 4; gateway seam substituted / placeholder-tested).

## Generated / Modified Tests

New: `AgentsOverviewTests.cs`, `ApproverPolicyTests.cs`, `LocalizationResourceTests.cs`, `DeferredGatewayTests.cs`.
Extended: `ProviderCatalogTests.cs`, `AgentConfigurationTests.cs`, `AgentsNavigationTests.cs`,
`AccessibilityTests.cs`, `AgentUiTestData.cs` (optional fields for content-safety overrides, disclosure, and
`SupportsTextGeneration`).

## Verification commands

```bash
# from Hexalith.Agents/
dotnet build Hexalith.Agents.slnx --configuration Release   # 0 Warning(s), 0 Error(s)
DiffEngine_Disabled=true dotnet test tests/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj             --configuration Release   # 156 passed
DiffEngine_Disabled=true dotnet test tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj     --configuration Release   # 93 passed
DiffEngine_Disabled=true dotnet test tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release # 81 passed
```

## Next Steps

- Run in CI alongside the existing guard suites.
- When the live read path lands (Epic 4 / 4.1 / 4.3), add API/integration tests for the real
  `Hexalith.Agents.Client → BFF/API → read-model` binding behind the same gateway interfaces.

## Notes

- **Keep-it-simple:** semantic locators (`data-testid`, `role`/`aria`, FluentBadge component instances), no hardcoded
  waits/sleeps (bUnit `WaitForAssertion`), each test sets up its own gateway state (order-independent).
- **Standards:** PascalCase BDD test names, Shouldly assertions (no raw `Assert.*`), file-scoped namespaces, Allman
  braces, `_camelCase` private fields — consistent with the existing suite and `CLAUDE.md`.
