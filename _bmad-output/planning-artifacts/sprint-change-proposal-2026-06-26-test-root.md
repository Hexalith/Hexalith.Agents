# Sprint Change Proposal - Move Agents Tests To Root Test Folder

Date: 2026-06-26 13:14:17 CEST (+0200)
Project: agents
Mode: Batch
Status: Implemented

## 1. Issue Summary

Trigger: Administrator requested `$bmad-correct-course move Hexalith.Agents/test to test`.

The live folder was `Hexalith.Agents/tests` rather than `Hexalith.Agents/test`. The requested correction is therefore interpreted as moving the existing Agents test projects from the nested module folder to the workspace-root `test/` folder. This is a repository-layout correction, not a product-scope change.

Evidence:

- `Hexalith.Agents/tests` existed and contained the Agents test projects.
- Root `test/` did not exist before the correction.
- The then-nested Agents solution referenced nested `tests/...` project paths.
- Test project references used `..\..\..\src\...`, which only works from the nested test location.
- Architecture and Epic 1 Story 1.1 still described the prior `tests/` layout.

## 2. Impact Analysis

Epic impact: Epic 1 Story 1.1 is affected because it defines the buildable module shell and structural seed. No functional epic scope changes.

Story impact: Story 1.1 structural acceptance text and the Architecture Structural Seed needed a layout correction. Existing feature stories remain historically valid.

Artifact conflicts:

- Architecture Structural Seed showed test projects under `Hexalith.Agents/tests`.
- Epics Story 1.1 described module `tests/` projects.
- Live conformance helpers assumed tests were under the module root.

Technical impact:

- Move tracked test files from `Hexalith.Agents/tests` to workspace-root `test`.
- Update `.slnx` test project paths to workspace-root `test/...`.
- Update test project references from `..\..\..\src\...` to `..\..\src\...`.
- Update layout conformance helpers/tests to resolve `test/` from the workspace root.
- Preserve ignored `bin/` and `obj/` output as non-source build artifacts and do not move them into the new layout.

PRD impact: None.

UX impact: None.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale: The change is a contained repository-layout correction. It does not alter product behavior, public contracts, domain logic, or MVP scope.

Effort: Low.

Risk: Low to Medium. The main risk is broken solution/test discovery after moving test projects outside the `Hexalith.Agents` folder. Updating the solution, project references, and layout helper addresses that risk.

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
  NuGet.config
  tests/
```

NEW:

```text
agents/
  Hexalith.Agents.slnx
  global.json
  Directory.Build.props
  Directory.Packages.props
  NuGet.config
  src/
test/
```

Rationale: The structural seed now reflects the requested workspace-root solution, build-file, source, and test tree.

### Epics

Artifact: `_bmad-output/planning-artifacts/epics.md`

OLD:

```text
... workspace-root `src/` projects, and module `tests/` projects matching the architecture Structural Seed
```

NEW:

```text
... workspace-root `src/` projects, and workspace-root `test/` projects matching the architecture Structural Seed
```

Rationale: Story 1.1 now matches the implemented root-level test layout.

### Implementation

OLD:

```text
Hexalith.Agents/tests/Hexalith.Agents.*.Tests
```

NEW:

```text
test/Hexalith.Agents.*.Tests
```

Rationale: Satisfies the requested move directly.

Supporting edits:

- Updated `Hexalith.Agents.slnx`.
- Updated all moved test project references to workspace-root `src`.
- Updated `ModuleLayout` and structural conformance tests to resolve `test/`.
- Updated traceability verification paths to `test/...`.

## 5. Implementation Handoff

Scope classification: Minor.

Routed to: Developer agent for direct implementation.

Success criteria:

- `test/` exists at the repository root.
- `Hexalith.Agents/tests` no longer contains source-controlled test projects.
- The Agents solution restores/builds against the moved test projects.
- Focused test project verification passes.
- Architecture and epics reflect the new root-level `test/` layout.

## 6. Checklist Summary

- [x] 1.1 Triggering story/context identified: structural correction to Story 1.1 module shell.
- [x] 1.2 Core problem defined: nested Agents tests must move to workspace-root `test/`.
- [x] 1.3 Evidence gathered: solution, project references, architecture seed, and layout tests referenced old assumptions.
- [x] 2.1-2.5 Epic impact assessed: no epic resequencing or scope change required.
- [x] 3.1 PRD conflicts checked: none.
- [x] 3.2 Architecture conflicts checked: Structural Seed updated.
- [x] 3.3 UX conflicts checked: none.
- [x] 3.4 Secondary artifacts checked: solution, test projects, traceability paths, and layout tests updated.
- [x] 4.1 Direct Adjustment evaluated: viable.
- [N/A] 4.2 Rollback evaluated: not needed.
- [N/A] 4.3 MVP review evaluated: not needed.
- [x] 4.4 Recommended path selected: Direct Adjustment.
- [x] 5.1-5.5 Proposal components completed.
- [x] 6.1-6.2 Final review completed through restore/build/test verification.
- [x] 6.3 Approval: direct user command treated as approval for this minor implementation.
- [N/A] 6.4 Sprint status update: no epic/story status changes required.
- [x] 6.5 Handoff complete: implemented directly.

## 7. Verification

- `dotnet restore Hexalith.Agents.slnx` passed.
- `dotnet build Hexalith.Agents.slnx -c Release -m:1 --no-restore` passed with 0 warnings and 0 errors.
- `DiffEngine_Disabled=true dotnet test test/Hexalith.Agents.Client.Tests/Hexalith.Agents.Client.Tests.csproj -c Release --no-build` passed: 6 tests.
- `DiffEngine_Disabled=true dotnet test test/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj -c Release --no-build` passed: 327 tests.
- `DiffEngine_Disabled=true dotnet test test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj -c Release --no-build` passed: 367 tests.
- `DiffEngine_Disabled=true dotnet test test/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj -c Release --no-build` passed: 724 tests.
- `DiffEngine_Disabled=true dotnet test test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj -c Release --no-build` passed: 968 tests.
