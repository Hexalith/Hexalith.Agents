---
title: '5.1 Correct Platform Hosting Boundary and Quality Gates'
type: 'feature'
created: '2026-08-04'
status: 'done'
baseline_revision: 'bc3fd4c4dd98cbd4a662c5d7e242343d1cf2985e'
review_loop_iteration: 0
followup_review_recommended: true
operator_actions: []
context:
  - '_bmad-output/implementation-artifacts/epic-5-context.md'
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
warnings:
  - 'oversized'
deferred:
  - summary: >-
      Validate Conversations:BaseUrl before constructing the source-mode Conversations client endpoint.
    evidence: |-
      The pre-existing source-mode registration treats any Conversations section as sufficient, then passes Conversations:BaseUrl directly to new Uri. A present section with a missing or malformed BaseUrl can therefore fail during startup without a focused configuration diagnostic. This behavior was moved behind a composition seam by this story but was not introduced by it.
    location: >-
      src/Hexalith.Agents.Server/Composition/ConversationServiceCollectionExtensions.cs:31
    severity: medium
  - summary: >-
      Reconcile the persisted Epic 5 sprint story map with the current approved Epic 5 planning artifact.
    evidence: |-
      The requested and sprint-tracked Story 5.1 slug combines platform-hosting correction with quality gates, while the current Epic 5 planning artifact separates the build/package boundary from later live platform-topology proof. This story followed the executable build-boundary scope and preserved the platform obligation as EXT-HOST-1.
    location: >-
      _bmad-output/implementation-artifacts/sprint-status.yaml:86
    severity: medium
---

<intent-contract>

## Intent

**Problem:** Agents still owns placeholder AppHost, Aspire, and ServiceDefaults projects, while external Hexalith dependencies are source-only and the repository lacks package-consumer and CI gates. This contradicts the platform-owned hosting boundary and cannot prove that shipped packages are usable independently.

**Approach:** Remove module-owned platform hosting, retain the reusable EventStore DomainService host, introduce explicit source and package dependency modes, and gate the repository with negative boundary tests, exact package inventory, an isolated package consumer, and clean-checkout CI. Treat the current Epic 5 boundary as executable authority; do not recreate the superseded local topology fixture.

## Boundaries & Constraints

**Always:** Use `Hexalith.Agents.slnx`, central package versions, warnings-as-errors, root-declared submodules only, per-project test execution, and the existing EventStore DomainService composition in `Hexalith.Agents.Server`. Debug/source mode may consume checked-out sibling projects; Release/package and consumer lanes must consume packages and disclose no repository source path. Complete and commit every repository-local task before recording any platform-host operator obligation.

**Block If:** Stop only for a non-operator impediment such as conflicting user-owned changes, unwritable repository metadata, or an unavailable dependency with no committed package or deterministic local-package route. An uncommitted external platform host is not a blocked condition; finish locally and use `awaiting-operator`.

**Never:** Do not ship an Agents-owned AppHost, Aspire, or ServiceDefaults project; initialize nested submodules; add inline package versions; publish/deploy; fabricate live-host evidence; move platform topology into Server; or implement the archived local topology fixture.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Source development | Debug restore with source mode enabled and root submodules present | External assets resolve as projects and the `.slnx` builds warning-free | Missing required source dependency fails with its root-declared path and recovery command |
| Package validation | Release restore/build with source mode disabled | External assets resolve as packages and no source-only edge survives | Source-reference leakage fails before pack with the offending project/reference |
| Isolated consumer | Exact produced Agents package set in an isolated feed | Consumer restores/builds without repository `ProjectReference`s and package metadata matches inventory | Missing/extra package, reverse dependency, or infrastructure leak fails with package/type evidence |
| Boundary regression | A hosting project/reference/SDK, legacy `.sln`, inline version, or recursive submodule command is introduced | Focused governance tests reject the change | Diagnostic names the forbidden artifact and governing rule |

</intent-contract>

## Code Map

- `Hexalith.Agents.slnx` and `src/Hexalith.Agents.{AppHost,Aspire,ServiceDefaults}/` -- remove the three forbidden projects and solution entries; do not relocate their topology.
- `src/Hexalith.Agents.Server/Program.cs` -- retained reusable DomainService host; preserve `AddEventStoreDomainService`/`UseEventStoreDomainService` and module application registrations.
- `Directory.Build.props`, `Directory.Packages.props`, `src/Hexalith.Agents.Contracts/Hexalith.Agents.Contracts.csproj`, `src/Hexalith.Agents/Hexalith.Agents.csproj`, `src/Hexalith.Agents.Server/Hexalith.Agents.Server.csproj`, `src/Hexalith.Agents.UI/Hexalith.Agents.UI.csproj`, and `test/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj` -- define source/package selection, source-aware submodule checks, central dependency versions, and conditional external edges.
- `src/Hexalith.Agents.Testing/Hexalith.Agents.Testing.csproj` -- remove the non-packable Server dependency from the public testing package.
- `test/Hexalith.Agents.Server.Tests/{StructuralSeedConformanceTests,AppHostSecurityTopologyTests,ProjectReferenceDirectionTests}.cs` -- replace obsolete positive host ownership with fail-closed absence and direction guards.
- `test/Hexalith.Agents.Server.Tests/ForbiddenHostingOwnershipTests.cs`, `PackageDependencyModeTests.cs`, and `PackageInventoryTests.cs` -- add hosting, solution-format, dependency-mode, inventory, and consumer-negative coverage beside the existing build/public-contract/runtime guards.
- `eng/release-packages.json`, `eng/verify-story.ps1`, `scripts/pack-release-packages.py`, `scripts/validate-nuget-packages.py`, `scripts/validate-consumer-package-references.py`, and `.github/workflows/ci.yml` -- add deterministic package inventory/validation and named clean-checkout source/package/boundary gates.
- `references/Hexalith.Memories/Directory.Build.props` and `references/Hexalith.Builds/.github/workflows/domain-ci.yml` -- read-only patterns for dependency-mode and consumer-validation behavior.
- `_bmad-output/planning-artifacts/external-dependency-register.md` -- read-only source of the operator-owned `EXT-HOST-1` commitment; never invent its repository, revision, date, or command.

## Tasks & Acceptance

**Execution:**
- [x] `Hexalith.Agents.slnx`, `src/Hexalith.Agents.AppHost/`, `src/Hexalith.Agents.Aspire/`, `src/Hexalith.Agents.ServiceDefaults/`, `test/Hexalith.Agents.Server.Tests/StructuralSeedConformanceTests.cs`, `test/Hexalith.Agents.Server.Tests/AppHostSecurityTopologyTests.cs`, and `test/Hexalith.Agents.Server.Tests/ProjectReferenceDirectionTests.cs` -- remove module-owned topology and replace obsolete positive ownership assertions with exhaustive negative checks.
- [x] `Directory.Build.props`, `Directory.Packages.props`, `src/Hexalith.Agents.Contracts/Hexalith.Agents.Contracts.csproj`, `src/Hexalith.Agents/Hexalith.Agents.csproj`, `src/Hexalith.Agents.Server/Hexalith.Agents.Server.csproj`, `src/Hexalith.Agents.UI/Hexalith.Agents.UI.csproj`, `src/Hexalith.Agents.Testing/Hexalith.Agents.Testing.csproj`, and `test/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj` -- implement explicit source/package modes, package-only Release behavior, and a valid packable dependency graph.
- [x] `test/Hexalith.Agents.Server.Tests/ForbiddenHostingOwnershipTests.cs`, `PackageDependencyModeTests.cs`, and `PackageInventoryTests.cs` -- make every prohibited ownership, solution, dependency-mode, and package-inventory regression fail with concrete evidence.
- [x] `eng/release-packages.json`, `eng/verify-story.ps1`, `scripts/pack-release-packages.py`, `scripts/validate-nuget-packages.py`, and `scripts/validate-consumer-package-references.py` -- declare the exact public package set, pack it, inspect metadata/assets, build an isolated consumer, and expose one deterministic Story 5.1 verification command.
- [x] `.github/workflows/ci.yml` -- run root-only checkout/init, Release/package build, relevant package-mode tests per project, boundary checks, and package-consumer validation as named blocking gates; keep sibling-source validation local.
- [x] `_bmad-output/implementation-artifacts/spec-5-1-correct-platform-hosting-boundary-and-quality-gates.md` -- record executed evidence; after commit, set `awaiting-operator` with imperative `operator_actions` when `EXT-HOST-1` remains operator-owned.

**Acceptance Criteria:**
- Given a clean checkout with only root-declared dependencies initialized, when the source lane runs, then the `.slnx` builds warning-free and no Agents-owned AppHost, Aspire, ServiceDefaults, legacy `.sln`, or recursive submodule path exists.
- Given Release/package mode, when Agents packages are produced and consumed from an isolated feed, then the exact inventory restores/builds without repository project references, reverse dependencies, implementation types, secrets, or hosting SDK leakage.
- Given any forbidden hosting ownership, inline package version, unexpected package, source leak, or invalid dependency direction, when focused gates run, then they fail closed with the offending path or package.
- Given a CI run, when all named gates execute, then Release/package build, five package-mode test projects, boundary checks, and package-consumer validation are independently visible and required.
- Given all repository-local work is green and committed while `EXT-HOST-1` is still uncommitted, when the story is finalized, then its frontmatter is `status: awaiting-operator` with a non-empty imperative `operator_actions` list and is never marked blocked for that external action.

## Spec Change Log

### 2026-08-09 — Operator EXT-HOST-1 commitment
- Created `Hexalith/Hexalith.Platform` as the platform-owned host repository (no prior Agents platform host existed).
- Accepted `EXT-HOST-1` as `Committed` at commit `a66cdf346e521ad147f442b686f301f0f59c525c`, target integration `2026-09-30`, verify command `./eng/verify-agents-host.sh` on that clean checkout.
- Cleared `operator_actions` and set story + sprint status to `done`. Live Agents composition remains Story 5.6 (`Available` upgrade).

## Review Triage Log

### 2026-08-04 — Review pass
- intent_gap: 0
- bad_spec: 0
- patch: 14: (high 9, medium 5, low 0)
- defer: 2: (high 0, medium 2, low 0)
- reject: 2: (high 0, medium 0, low 2)
- addressed_findings:
  - `[high]` `[patch]` Replaced the unsupported CI class-filter command with an executable package-mode Server boundary suite.
  - `[high]` `[patch]` Converted every CI build and test lane to Release/package mode and exposed the five test projects, boundary suite, and isolated consumer as named jobs.
  - `[high]` `[patch]` Made dependency-mode selection authoritative and rejected contradictory flags, forced source properties in package mode, Release source mode, and pack source mode.
  - `[high]` `[patch]` Extracted conversation composition and exercised both source and package compilation branches without leaking source-only client types.
  - `[medium]` `[patch]` Expanded dependency-mode tests across external project/package edges instead of sampling only a subset of the graph.
  - `[medium]` `[patch]` Hardened hosting, legacy-solution, recursive-submodule, and forbidden-SDK scans against nested and renamed artifacts.
  - `[high]` `[patch]` Limited package cleanup to manifest-owned archives and verified unrelated output artifacts survive repacking.
  - `[high]` `[patch]` Passed the requested package version through both build and pack so assembly and package outputs are aligned.
  - `[medium]` `[patch]` Enforced an exact manifest package-id-to-project mapping before packing.
  - `[high]` `[patch]` Required each package to contain only its declared implementation assembly and the intentional UI satellite resource.
  - `[high]` `[patch]` Validated duplicate, missing, and mismatched internal dependency version declarations.
  - `[high]` `[patch]` Proved every isolated consumer dependency resolves from the local feed with exact identities and no normalized repository source path disclosure.
  - `[medium]` `[patch]` Made project-reference direction checks exhaustive over repository projects instead of relying on a fixed project list.
  - `[medium]` `[patch]` Corrected the documented source verification command so restore and build both select project-reference mode.

## Design Notes

The invocation uses the earlier combined title, while the current Epic 5 plan separates live platform topology proof. This story owns removal and local verification only; the external platform host remains the sole owner of production-like composition.

`Hexalith.Conversations.Client` has no committed NuGet package. Debug/source mode retains its adapter for local development; Release/package mode excludes those two source-only adapter files and registers the existing fail-closed deferred ports. This keeps Release free of repository project paths without inventing or publishing an external package.

## Verification

**Commands:**
- `dotnet restore Hexalith.Agents.slnx -p:Configuration=Debug -p:UseHexalithProjectReferences=true && dotnet build Hexalith.Agents.slnx -c Debug --no-restore -warnaserror -p:UseHexalithProjectReferences=true` -- expected: source lane succeeds.
- `dotnet restore Hexalith.Agents.slnx -p:Configuration=Release -p:UseHexalithProjectReferences=false && dotnet build Hexalith.Agents.slnx -c Release --no-restore -warnaserror` -- expected: package lane succeeds without source leakage.
- `pwsh ./eng/verify-story.ps1 -Story 5.1` -- expected: exact inventory, focused per-project tests, boundary negatives, and isolated consumer all pass.

**Executed evidence (2026-08-04):**

- `pwsh ./eng/verify-story.ps1 -Story 5.1` -- passed end to end: Debug/source restore and warning-free build; Contracts 327/327, Client 6/6, Domain 724/724, Server 385/385, and UI 968/968 tests (2,410 total); Release/package restore and warning-free build; Contracts 327/327, Client 6/6, Domain 724/724, Server 361/361, and UI 968/968 tests (2,386 total).
- The verifier's executable negative gates rejected Release+source mode, pack+source mode, and a missing EventStore source root with exit code 1 and their intended diagnostics.
- Release packaging rebuilt every manifest project at version `0.0.0-story-5-1`, preserved the unrelated output sentinel, produced and inspected the exact five-package inventory, and built the isolated consumer with every local Agents package proven to originate from the isolated feed and no project/source-path resolution.
- `dotnet build` and the complete package-mode Server boundary suite -- passed warning-free with 361/361 tests; Python syntax and package/consumer validation checks also passed.
- Aspire pre-change baseline did not produce live evidence: `aspire start --apphost src/Hexalith.Agents.AppHost/Hexalith.Agents.AppHost.csproj --format Json --non-interactive` timed out after 120 seconds during restore with a local OpenSSL certificate-trust warning.

## Auto Run Result

**Summary:** Removed Agents-owned AppHost, Aspire, and ServiceDefaults ownership; preserved the reusable EventStore DomainService host; added explicit Debug/source and Release/package dependency modes; and established exact-package, isolated-consumer, boundary, and CI quality gates.

**Files changed:**

- `Directory.Build.props` and the affected source/test project files define and enforce source versus package dependency modes and a packable public graph.
- `Hexalith.Agents.slnx` and the removed `src/Hexalith.Agents.{AppHost,Aspire,ServiceDefaults}/` projects correct the platform hosting boundary.
- `src/Hexalith.Agents.Server/Program.cs` and `Composition/ConversationServiceCollectionExtensions.cs` retain DomainService composition while isolating the source-only Conversations adapter branch.
- `test/Hexalith.Agents.Server.Tests/` adds exhaustive ownership, direction, dependency-mode, package-inventory, and composition checks.
- `eng/release-packages.json`, `eng/verify-story.ps1`, and `scripts/{pack-release-packages,validate-nuget-packages,validate-consumer-package-references}.py` implement the deterministic five-package producer and consumer gate.
- `.github/workflows/ci.yml` exposes Release/package build, five test projects, boundary checks, and package-consumer validation as named jobs.
- `_bmad-output/implementation-artifacts/epic-5-context.md`, this spec, and `sprint-status.yaml` record the reconciled scope and delivery state.

**Review findings:** Applied 14 patches (high 9, medium 5), deferred 2 pre-existing medium findings, and rejected 2 low-value redundant probes already covered by exact metadata and compiled-consumer evidence.

**Follow-up review recommendation:** `true`; patched findings were high severity (high 9, medium 5, low 0; weighted medium/low score 15, with the high-severity rule independently requiring follow-up).

**Verification:** `pwsh ./eng/verify-story.ps1 -Story 5.1` passed independently after review fixes: warning-free Debug/source and Release/package builds; 2,410 source-mode tests; 2,386 package-mode tests; three intended negative dependency-mode failures; exact five-package validation at `0.0.0-story-5-1`; unrelated-output preservation; and an isolated local-feed consumer build with no project or source-path resolution. `git diff --check` also passed.

**Residual risks:** The new GitHub Actions workflow has not run on the remote service in this local execution. The pre-change Aspire host could not provide live baseline evidence before its removal because restore timed out. Platform-host composition remains an external operator commitment under `EXT-HOST-1`; the frontmatter records the required operator handoff without marking the story blocked.
