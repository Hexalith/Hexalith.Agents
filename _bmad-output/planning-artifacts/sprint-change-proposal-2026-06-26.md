# Sprint Change Proposal - Move Agents Source Tree

Date: 2026-06-26 13:00:46 CEST (+0200)
Project: agents
Mode: Batch
Status: Implemented

## 1. Issue Summary

Trigger: Administrator requested `$bmad-correct-course move Hexalith.Agents/src to src`.

The implementation source tree for Hexalith Agents lived under `Hexalith.Agents/src`, while the requested workspace layout places production source projects at the repository root `src`. This is a structural correction, not a product-scope change.

Evidence:

- Existing live source projects were under `Hexalith.Agents/src`.
- `Hexalith.Agents/Hexalith.Agents.slnx` referenced those projects with `src/...` paths.
- Test project references and layout conformance tests assumed `src` was inside the `Hexalith.Agents` module directory.
- Build discovery for moved projects would lose `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `global.json`, and `NuGet.config` unless root-level files were added.

## 2. Impact Analysis

Epic impact: Epic 1 Story 1.1 is affected because it defines the buildable module shell and structural seed. No functional epic scope changes.

Story impact: Story 1.1 structural acceptance text and the Architecture Structural Seed needed a layout clarification. Existing implementation stories remain historically valid and were not mass-edited.

Artifact conflicts:

- Architecture Structural Seed previously showed `Hexalith.Agents/src`.
- Epics Story 1.1 previously described `src/` and `tests/` without noting the split root source tree.
- Live conformance tests assumed `ModuleRoot/src`.

Technical impact:

- Move tracked source files from `Hexalith.Agents/src` to workspace-root `src`.
- Update `.slnx` source project paths to `../src/...`.
- Update test project references to `../../../src/...`.
- Add root-level build discovery files for moved projects.
- Update layout helpers/tests to resolve `SourceRoot` separately from `ModuleRoot`.

PRD impact: None.

UX impact: None.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale: The change is a repository-layout correction with contained build/test fallout. It does not change product behavior, domain contracts, UI behavior, or MVP scope.

Effort: Low.

Risk: Low to Medium. The main risk was losing MSBuild/editorconfig/package discovery after moving projects outside the solution directory. Root-level discovery files and verification addressed that risk.

Timeline impact: No sprint replanning required.

## 4. Detailed Change Proposals

### Architecture

Artifact: `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md`

OLD:

```text
Hexalith.Agents/
  Hexalith.Agents.slnx
  global.json
  Directory.Build.props
  Directory.Packages.props
  src/
  tests/
```

NEW:

```text
agents/
  global.json
  Directory.Build.props
  Directory.Packages.props
  NuGet.config
  src/
  Hexalith.Agents/
    Hexalith.Agents.slnx
    global.json
    Directory.Build.props
    Directory.Packages.props
    NuGet.config
    tests/
```

Rationale: The structural seed now reflects the requested workspace-root source tree while preserving the module solution and tests.

### Epics

Artifact: `_bmad-output/planning-artifacts/epics.md`

OLD:

```text
... initial `src/` and `tests/` projects matching the architecture Structural Seed
```

NEW:

```text
... workspace-root `src/` projects, and module `tests/` projects matching the architecture Structural Seed
```

Rationale: Story 1.1 now matches the implemented split layout.

### Implementation

OLD:

```text
Hexalith.Agents/src/Hexalith.Agents.*
```

NEW:

```text
src/Hexalith.Agents.*
```

Rationale: Satisfies the requested move directly.

Supporting edits:

- Added root `.editorconfig`, `global.json`, `NuGet.config`, `Directory.Build.props`, and `Directory.Packages.props`.
- Updated `Hexalith.Agents/Hexalith.Agents.slnx`.
- Updated all `Hexalith.Agents/tests/*/*.csproj` source project references.
- Updated `ModuleLayout` and dependent conformance tests to use `SourceRoot`.
- Updated UI floor source-file discovery to use workspace-root `src`.

## 5. Implementation Handoff

Scope classification: Minor.

Routed to: Developer agent for direct implementation.

Success criteria:

- `src/` exists at the repository root.
- `Hexalith.Agents/src` no longer exists.
- The Agents solution restores and builds.
- All Agents test projects pass.
- Architecture and epics reflect the new split source/test layout.

## 6. Checklist Summary

- [x] 1.1 Triggering story/context identified: structural correction to Story 1.1 module shell.
- [x] 1.2 Core problem defined: source tree must move to workspace-root `src`.
- [x] 1.3 Evidence gathered: solution, project references, build props, and layout tests referenced old assumptions.
- [x] 2.1-2.5 Epic impact assessed: no epic resequencing or scope change required.
- [x] 3.1 PRD conflicts checked: none.
- [x] 3.2 Architecture conflicts checked: Structural Seed updated.
- [x] 3.3 UX conflicts checked: none.
- [x] 3.4 Secondary artifacts checked: solution, test projects, MSBuild discovery, layout tests updated.
- [x] 4.1 Direct Adjustment evaluated: viable.
- [N/A] 4.2 Rollback evaluated: not needed.
- [N/A] 4.3 MVP review evaluated: not needed.
- [x] 4.4 Recommended path selected: Direct Adjustment.
- [x] 5.1-5.5 Proposal components completed.
- [x] 6.1-6.2 Final review completed through restore, build, and tests.
- [x] 6.3 Approval: direct user command treated as approval for this minor implementation.
- [N/A] 6.4 Sprint status update: no epic/story status changes required.
- [x] 6.5 Handoff complete: implemented directly.

## 7. Verification

- `dotnet restore Hexalith.Agents/Hexalith.Agents.slnx` passed.
- `dotnet build Hexalith.Agents/Hexalith.Agents.slnx -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- `dotnet test Hexalith.Agents/tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj -c Release --no-build` passed: 367 tests.
- `dotnet test Hexalith.Agents/tests/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj -c Release --no-build` passed: 968 tests.
- `dotnet test Hexalith.Agents/tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj -c Release --no-build` passed: 327 tests.
- `dotnet test Hexalith.Agents/tests/Hexalith.Agents.Client.Tests/Hexalith.Agents.Client.Tests.csproj -c Release --no-build` passed: 6 tests.
- `dotnet test Hexalith.Agents/tests/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj -c Release --no-build` passed: 724 tests.
