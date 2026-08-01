---
title: 'Restore all root test suites to green'
type: 'bugfix'
created: '2026-08-01'
status: 'done'
baseline_commit: '499ba3ee3bea4e28fe35abe3c557a27954408ab4'
context:
  - '{project-root}/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The current root has not been verified against its latest dependency pointers; its last green evidence predates recent submodule updates. Every configured root test lane must run from fresh Release output, and genuine failures must be fixed.

**Approach:** Establish a serialized Release baseline, run all five .NET projects individually plus the Python tooling suite, repair demonstrated root causes at their owning boundaries, then repeat focused and complete verification.

## Boundaries & Constraints

**Always:** Preserve warnings-as-errors, nullable analysis, test intent, API compatibility, security/tenant-isolation behavior, and per-project execution. Separate runner/environment failures from code failures; use direct xUnit v3 executables if VSTest is blocked. Make the smallest coherent fix and cover changed behavior.

**Ask First:** Before editing `references/`, changing a public/serialized contract, adding a dependency, updating a deliberate snapshot/baseline, or weakening an architecture/security gate.

**Never:** Run solution-level `dotnet test`; initialize nested submodules; globally suppress warnings; disable, delete, skip, quarantine, or loosen failing tests to get green; trust stale output; or edit generated files.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Build failure | Compiler, analyzer, restore, or reference error | Repair the owning root configuration/source boundary | Rebuild serially from the first failure |
| Test failure | Assertion, exception, or conformance failure | Fix violated behavior or a proven-wrong expectation | Run focused class/project before broader suites |
| Runner failure | Test-host failure without product evidence | Execute the built xUnit v3 assembly directly | Report a blocker only if fallback also fails |
| Dependency defect | Failure is proven inside a root-declared submodule | Stop before modifying that repository | Present evidence and request approval |

</frozen-after-approval>

## Code Map

- `Hexalith.Agents.slnx`, `Directory.Build.props`, `Directory.Packages.props` -- solution membership, source resolution, warning, and package gates.
- `src/Hexalith.Agents*/` -- contracts, domain, server, client, UI, hosting, and test-helper ownership boundaries.
- `test/Hexalith.Agents.*.Tests/` -- five xUnit v3 unit, component, and conformance projects.
- `tools/check-story-review-readiness.py`, `tests/tooling/story_review_readiness/story_review_readiness_test.py` -- Python production code and independent regression lane.

## Tasks & Acceptance

**Execution:**
- [x] `Hexalith.Agents.slnx`, `Directory.Build.props`, `Directory.Packages.props` -- restore/build fresh Release output with serialized MSBuild and shared catalog properties; diagnose the first failure without weakening gates.
- [x] `src/Hexalith.Agents*/`, `test/Hexalith.Agents.*.Tests/` -- fix each Agents-owned causal cluster and add/correct focused regression coverage while preserving conformance/security assertions.
- [x] `tools/check-story-review-readiness.py`, `tests/tooling/story_review_readiness/story_review_readiness_test.py` -- run and, if required, repair the Python lane without weakening its fail-closed contract.
- [x] `test/`, `tests/tooling/` -- rerun all root suites from repaired sources; confirm no failure, skip, pending result, or stale-output dependence.

**Acceptance Criteria:**
- Given initialized root submodules, when the solution is restored/built in serialized Release mode, then it finishes with zero warnings and errors.
- Given fresh outputs, when all five .NET projects and the Python suite run, then every discovered test passes with no failed, skipped, pending, or other outcome.
- Given a test-host transport failure, when the direct xUnit v3 fallback runs, then it yields complete evidence or an exact environment blocker.
- Given a repair, when its focused and dependent suites run, then the original failure is absent without weakened coverage.

## Spec Change Log

## Design Notes

Repair one causal cluster at a time. Adapt sibling API drift at an Agents-owned boundary; sibling source changes cross ownership and require approval.

## Verification

**Commands:**
- `BUILD_PROPS=(-p:Hexalith1BuildPackageProps="$PWD/references/Hexalith.Builds/Props/Directory.Packages.props" -p:Hexalith2BuildPackageProps="$PWD/references/Hexalith.Builds/Props/Directory.Packages.props" -p:Hexalith3BuildPackageProps="$PWD/references/Hexalith.Builds/Props/Directory.Packages.props" -p:Hexalith4BuildPackageProps="$PWD/references/Hexalith.Builds/Props/Directory.Packages.props")` -- expected: shared catalog arguments are defined.
- `dotnet restore Hexalith.Agents.slnx -m:1 --nologo "${BUILD_PROPS[@]}"` then `dotnet build Hexalith.Agents.slnx -c Release -m:1 --no-restore --nologo "${BUILD_PROPS[@]}"` -- expected: clean Release output.
- `for TEST_PROJECT in test/*/*.csproj; do DiffEngine_Disabled=true dotnet test "$TEST_PROJECT" -c Release --no-build -m:1 /nodeReuse:false --nologo || break; done` -- expected: all five projects pass; use built xUnit v3 assemblies if needed.
- `python3 -m unittest tests/tooling/story_review_readiness/story_review_readiness_test.py` -- expected: all tooling tests pass.

**Results:**
- Serialized restore passed; Release build passed with 0 warnings and 0 errors.
- Client 6, Contracts 327, Server 372, Domain 724, and UI 968: 2,397 passed, 0 failed, 0 skipped.
- Python tooling: 39 passed. UI-test vulnerability audit: no vulnerable packages.

## Suggested Review Order

**Catalog alignment**

- Shared catalog import aligns transitive versions while retaining an override seam.
  [`Directory.Packages.props:5`](../../Directory.Packages.props#L5)

- Explicit root overrides preserve intentional UI and test-tool versions.
  [`Directory.Packages.props:14`](../../Directory.Packages.props#L14)

**Fail-closed prerequisites**

- Build catalog joins the existing required-submodule validation gate.
  [`Directory.Build.props:38`](../../Directory.Build.props#L38)

- Conformance coverage prevents silently dropping the new build prerequisite.
  [`BuildContractConformanceTests.cs:40`](../../test/Hexalith.Agents.Server.Tests/BuildContractConformanceTests.cs#L40)

**Verification evidence**

- Final totals record every root .NET and Python lane after review fixes.
  [`spec-fix-all-test-failures.md:73`](spec-fix-all-test-failures.md#L73)
