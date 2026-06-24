# Test Automation Summary — Story 1.1: Buildable Agents Module Shell And Public Boundaries

**Workflow:** `bmad-qa-generate-e2e-tests`
**Date:** 2026-06-23
**Engineer:** QA automation (Administrator)
**Module under test:** `Hexalith.Agents/`
**Framework (auto-detected):** xUnit v3 `3.2.2` + Shouldly `4.3.0` + NSubstitute `5.3.0` + coverlet — `net10.0`, Central Package Management, warnings-as-errors.

## Nature of this feature

Story 1.1 is the **greenfield foundation story** — a *buildable module shell* with public boundaries and
guard tests. It deliberately ships **no runtime surface**: no API endpoints (`Server` is a minimal
`WebApplication` with no routes), no UI pages, and no aggregates/events/commands. Conventional API/E2E
HTTP/browser tests therefore do not apply; the architecture-conformance **guard tests** are the
end-to-end automated tests for this feature (the story explicitly frames its tests as
"architecture-conformance guard tests", mirroring the Conversations `*.Conformance.Tests` precedent).

- **API tests:** N/A — no HTTP endpoints exist in the shell (deferred to Stories 1.2+).
- **E2E/UI tests:** N/A — no UI pages exist yet (deferred to Story 1.8). FrontComposer nav + FluentUI pages arrive later.
- **Delivered instead:** structural / boundary / build-contract **guard tests** that verify the acceptance criteria.

## Coverage by Acceptance Criterion

| AC | Concern | Test(s) | Status |
| --- | --- | --- | --- |
| AC1 | Buildable structural seed (files, project set, `src/`+`tests/`) | `StructuralSeedConformanceTests` **(new)** | ✅ |
| AC1 | Build contract: `net10.0` / C#14 / nullable / implicit usings / warnings-as-errors / CPM / pinned SDK | `BuildContractConformanceTests` **(new)** | ✅ |
| AC2 | Public contracts assembly references no forbidden assembly | `ContractsBoundaryTests` (pre-existing) | ✅ |
| AC2 | Dependency **direction** (AD-15): Contracts references nothing; in-module edges respect the matrix | `ProjectReferenceDirectionTests` **(new)** | ✅ |
| AC2 | Public contract projects (`Contracts` + `Client`) declare no forbidden **package** | `PublicContractPackageBoundaryTests` **(new)** | ✅ |
| AC3 | Clean build, zero warnings | `dotnet build … -c Release` (warnings-as-errors) | ✅ |
| AC3 | Package-version centralization (no inline `PackageReference Version`) | `PackageVersionCentralizationTests` (pre-existing) | ✅ |
| AC3 | Secret / provider non-leakage in the public contracts surface | `ContractsSecretNonDisclosureTests` (pre-existing) | ✅ |
| AC4 | Named extension points (project set + `Server/` folders) exist | `StructuralSeedConformanceTests` **(new)** | ✅ |
| AC4 | No premature domain entities | Honored by construction; structural test asserts folders are present as named extension points¹ | ✅ |

¹ A perpetual "folders must stay empty" assertion was deliberately **not** added: it would fail by design the
moment Story 1.2 adds the first aggregate. Per the story's guidance, guards "grow with the contracts" — the
no-premature-entities clause is a one-time scope check confirmed at review, not a brittle standing test.

## Gaps Discovered & Auto-Applied

The pre-existing tests covered AC2 (assembly boundary), AC3 (centralization + secret non-leakage). The
following **coverage gaps** were discovered against the acceptance criteria and auto-applied:

### Generated Tests

- [x] `tests/Hexalith.Agents.Server.Tests/StructuralSeedConformanceTests.cs` — **AC1/AC4**: root seed files, named `src/` + `tests/` project set, and `Server/` extension folders (`Aggregates`, `Application/{Agents,Workflows,Activities,Tools}`, `Ports`, `Projections`) all exist (subset checks — later stories may add more).
- [x] `tests/Hexalith.Agents.Server.Tests/BuildContractConformanceTests.cs` — **AC1**: root `Directory.Build.props` enforces `net10.0` / `LangVersion 14` / `Nullable enable` / `ImplicitUsings enable` / `TreatWarningsAsErrors true`; `Directory.Packages.props` enables CPM; `global.json` pins the SDK + `rollForward`. Catches a silent regression (e.g. disabling warnings-as-errors) that a clean build alone would not surface.
- [x] `tests/Hexalith.Agents.Server.Tests/ProjectReferenceDirectionTests.cs` — **AC2/AD-15**: `Hexalith.Agents.Contracts` references no other project (inward-most), and every `src/` project only references projects allowed by the dependency-direction matrix. Catches a wrong-direction *project* edge the assembly-reference scan cannot.
- [x] `tests/Hexalith.Agents.Server.Tests/PublicContractPackageBoundaryTests.cs` — **AC2**: the public contract projects (`Contracts`, `Client`) declare no forbidden `PackageReference` (server infra, Aspire host stack, Dapr runtime, provider SDKs, EventStore *server* internals, UI shell). Complements the compiled-assembly test by catching a declared-but-unused forbidden package.

### Supporting refactor

- [x] `tests/Hexalith.Agents.Server.Tests/ModuleLayout.cs` — extracted shared module-root discovery + project-file enumeration; `PackageVersionCentralizationTests` was refactored to reuse it (removed duplicated `FindModuleRoot`/`IsUnderBuildOutput`).

## Test Inventory & Results

Run per-project (never solution-level `dotnet test`), per `CLAUDE.md#Testing`.

### `tests/Hexalith.Agents.Contracts.Tests` — Passed 3/3
- `ContractsBoundaryTests` (1) — pre-existing
- `ContractsSecretNonDisclosureTests` (2) — pre-existing

### `tests/Hexalith.Agents.Server.Tests` — Passed 12/12
- `PackageVersionCentralizationTests` (1) — pre-existing (refactored to use `ModuleLayout`)
- `StructuralSeedConformanceTests` (5) — **new**
- `BuildContractConformanceTests` (3) — **new**
- `ProjectReferenceDirectionTests` (2) — **new**
- `PublicContractPackageBoundaryTests` (1) — **new**

**Total: 15 tests, 15 passed, 0 failed, 0 skipped.** (11 new + 4 pre-existing.)

## Coverage metrics

- **Acceptance criteria with automated guard coverage:** 4/4 (AC1, AC2, AC3, AC4).
- **AC concerns with a dedicated test:** 9/9 distinct concerns (see table above).
- **API endpoints covered:** 0/0 (none exist in the shell).
- **UI features covered:** 0/0 (none exist in the shell).
- **Guard-test count:** 15 (was 4 → +11).

## Verification commands

```bash
# from Hexalith.Agents/
dotnet restore Hexalith.Agents.slnx
dotnet build   Hexalith.Agents.slnx --configuration Release   # 0 Warning(s), 0 Error(s)
dotnet test    tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release
dotnet test    tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj       --configuration Release
```

## Next Steps

- Run these guard tests in CI once the deferred CI quality gate lands (readiness-report Concern #5; AppHost topology + CI are deferred past Story 1.1).
- The boundary / package / secret guards are designed to **grow with the contracts** — as Stories 1.2–1.8 add real contract types, provider adapters, and UI, these tests start exercising non-empty surfaces automatically.
- When Story 1.2 introduces the first aggregate, expect the structural test's extension folders to gain real `.cs` files (no test change needed); add aggregate-level behavior tests at that point.

## Notes

- **Keep-it-simple:** all guards are file-system/reflection scans following the established `PackageVersionCentralizationTests` pattern — no fixtures, no mocks, no hardcoded waits, fully order-independent.
- **Standards:** PascalCase test names, Shouldly assertions (no raw `Assert.*`), file-scoped namespaces, Allman braces, `_camelCase` private fields — consistent with the pre-existing guard tests and `CLAUDE.md`.
