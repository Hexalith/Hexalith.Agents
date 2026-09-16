---
title: 'Story 5.1: Adopt Hexalith.Builds as the Sole Package Version Authority'
type: 'refactor'
created: '2026-09-16'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'a9ebd97c2a0e6d4a6a77725ea988c690db1efa67'
context:
  - '_bmad-output/implementation-artifacts/epic-5-context.md'
  - '_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-16.md'
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-git-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Agents imports Hexalith.Builds but still owns package-version properties and `PackageVersion` items, allowing its dependency graph to diverge from the shared catalog and masking Story 5.2's EventStore release dependency.

**Approach:** Advance the authoritative EventStore selection in Hexalith.Builds, update its generated audit, then make Agents an import-only consumer with executable guards and planning evidence that enforce shared ownership.

## Boundaries & Constraints

**Always:** Preserve Central Package Management, Debug source-reference and Release package-reference modes, the Story 5.2 `>=3.105.0` EventStore floor, root-only submodule handling, existing package IDs, and unrelated work. Support both the root checkout and established parent/sibling layouts; fail clearly when the Builds catalog is absent or unloaded.

**Never:** Add package versions, package-specific version properties, `Version`, or `VersionOverride` in Agents; hand-edit the generated Builds audit; update individual EventStore rows instead of their shared property; initialize nested submodules; weaken the Story 5.2 floor; change product or UX behavior; push remote branches without separate authorization.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Root checkout | `references/Hexalith.Builds` exists | Wrapper imports the shared catalog and sets `HexalithVersionsLoaded=true` | Build proceeds |
| Parent layout | Builds is a supported sibling/ancestor dependency | Wrapper resolves the same catalog | Build proceeds with identical effective versions |
| Missing catalog | No supported catalog path resolves | Restore/build fails before dependency use | Diagnostic names Hexalith.Builds and root-only initialization command |
| Local override | Agents adds a version property/item or project metadata | Authority validation and Story 5.1 tests fail | Diagnostic identifies the offending declaration |
| EventStore floor | Imported catalog selects `3.106.0` | Story 5.2 floor reads the effective shared version | Any lower value fails closed |

</frozen-after-approval>

## Code Map

- `references/Hexalith.Builds/Props/Directory.Packages.props` -- authoritative package catalog and EventStore family property.
- `references/Hexalith.Builds/Tools/package-version-audit.json` -- generated catalog audit bound to the catalog commit/hash.
- `Directory.Packages.props` and `Directory.Build.props` -- Agents import wrapper and early loaded-catalog guard.
- `test/Hexalith.Agents.Server.Tests/{PackageVersionCentralizationTests,BuildContractConformanceTests}.cs` -- negative and layout conformance gates.
- `eng/verify-story.ps1`, `eng/verify-story-5.2.ps1`, `.github/workflows/ci.yml` -- Story 5.1 verification, preserved package floor, and CI wiring.
- `_bmad-output/{planning-artifacts,implementation-artifacts}` -- approved architecture, epic, story, deferred-work, proposal, and sprint evidence.

## Tasks & Acceptance

**Execution:**
- [x] `references/Hexalith.Builds/Props/Directory.Packages.props` -- set `HexalithEventStoreVersion` to published `3.106.0`, validate and commit the catalog, regenerate/validate the audit, then commit the generated audit so Agents can record a clean authoritative gitlink.
- [x] `Directory.Packages.props` and `Directory.Build.props` -- adopt the reference-repository import resolution pattern, remove every local version declaration, and gate on `HexalithVersionsLoaded` rather than a fixed `.git` path.
- [x] Story 5.1 tests/verifier and CI -- invoke the shared consumer-authority validator and test root, parent-layout, missing-catalog, local-override, and effective-version behavior without duplicating its scanner.
- [x] Planning and implementation artifacts -- reconcile Stories 5.1/5.2/5.6, architecture assumptions/index, UX ownership wording, DW-20, proposal execution evidence, and sprint status with the landed commits and verification results.

**Acceptance Criteria:**
- Given any tracked Agents project/build file, when package authority is validated, then no local package version declaration exists and the imported Builds catalog is the sole effective source.
- Given supported checkout layouts, when restore/build evaluates the wrapper, then it loads the shared catalog; an unsupported or missing catalog fails with an actionable diagnostic.
- Given Release package mode, when Story 5.2 checks EventStore, then the effective catalog value is at least `3.105.0` and currently resolves to `3.106.0`.
- Given the completed change, when Builds validators, Story 5.1 verification, focused tests, Release restore/build, and the package-consumer lane run, then all pass and documentation cites actual commit/version evidence.

## Implementation Notes

- Hexalith.Builds catalog commit `dae84d5f96911517eb1e6f97e75eb88e759e6d8a` advances the EventStore family to published `3.106.0`; generated-audit commit `000abf867abc3a99cfa74d39b6e73af05c78a602` binds the regenerated audit to that catalog.
- The Agents wrapper now contains only CPM policy, supported-layout path selection, and guarded imports. The early build target checks `HexalithVersionsLoaded` and reports the exact root-only submodule initialization command when no catalog loads.
- Conformance tests use the shared consumer-authority validator for all declaration negatives. Separate fixture tests prove root, sibling, parent-references, missing-catalog, and effective EventStore-version behavior.
- Story verification and CI run shared authority validation before the EventStore floor and existing source/package lanes. The package validators were reconciled with the existing six-package release manifest, including the EventStore integration package and its isolated-consumer marker.
- Epics, Architecture Spine assumption index 11, UX package ownership, Story 5.2 evidence, DW-20, the approved proposal, and sprint status now distinguish the resolved package floor from Story 5.2's independent blockers.
- Integration risk: both Builds commits are local and `references/Hexalith.Builds` is two commits ahead of `origin/main`. Remote push was explicitly out of scope, so a fresh clone cannot resolve the proposed parent gitlink until the Builds maintainer publishes those commits.

## Spec Change Log

- 2026-09-16 — Implemented the approved shared package-authority correction, recorded exact Builds commits and local verification, closed DW-20, and retained Story 5.1 as `in-progress` pending review/integration.

## Review Triage Log

| ID | Verdict | Evidence | Route |
| --- | --- | --- | --- |
| BH-01 | high | The selected Builds gitlink `000abf867abc3a99cfa74d39b6e73af05c78a602` is not contained by an `origin` ref, so publishing the parent first would break fresh-clone submodule initialization. Remote publication is explicitly excluded by the frozen intent. | defer |
| BH-02 | medium | Reproduced: `Pack` with `NoBuild=true` and all four catalog paths missing exited 0 and created a package because `CheckBuildCatalog` runs only before `Restore;Build`. | patch |
| BH-03 | low | A caller can deliberately pass global `HexalithVersionsLoaded=true` to suppress imports and the guard. This is an uncommon explicit override, and preventing all command-line spoofing would add catalog provenance/state complexity disproportionate to the normal build risk. | reject |
| BH-04 | false | Supported builds use the standard first-existing shared-catalog resolution pattern, while the shared validator compares every effective `PackageVersion` against the authoritative catalog passed to it. The claim depends on deliberate path redirection or ambiguous unrelated checkouts, neither of which is a supported authoritative invocation. | reject |
| BH-05 | false | `3.106.0` is the frozen matrix's required current selection, while the separate floor verifier owns the durable `>=3.105.0` compatibility rule; an assertion is not package-version authority. | reject |
| BH-06 | false | Each import is atomic: the synthetic catalog isolates path selection, and the real-root shared validator already compares the complete effective catalog. Full catalog content cannot partially import because the fixture is minimal. | reject |
| BH-07 | false | “Import-only” is scoped to package-version authority. The wrapper and shared validator reject version-bearing declarations; unrelated future MSBuild plumbing would not create a second package authority. | reject |
| BH-08 | false | The direct test scans projects for inline metadata, but the production shared validator invoked by the verifier and CI scans tracked `.csproj`, `.props`, and `.targets` files and passed the real repository. | reject |
| BH-09 | false | The shared validator handles `Include`, `Update`, nested metadata, `GlobalPackageReference`, CPM disablement, and override enablement. Story tests intentionally exercise representative matrix categories without duplicating that shared scanner, as the task requires. | reject |
| BH-10 | false | The production shared validator scans `Directory.Build.props`; the unused fixture parameter does not leave the real repository unchecked, and the matrix requires a local-override case rather than duplicating every placement. | reject |
| BH-11 | false | The release pack step rebuilds in package mode after shared-authority validation, and inspected story packages contain exact catalog-derived EventStore, Dapr, and other external versions. No reachable stale-range path under the verified lane was demonstrated. | reject |
| BH-12 | false | An isolated downstream consumer is allowed to resolve compatible dependency ranges independently; sole catalog authority governs the Agents build. The produced nuspecs carry exact catalog-derived external versions and the consumer proves local Agents identities and source isolation. | reject |
| BH-13 | false | The complete Story 5.1 verifier runs Debug/source restore, build, and all five test projects before Release/package verification. The CI task added the required shared-authority/floor wiring; it did not require converting the pre-existing Release-only CI topology. | reject |
| BH-14 | false | Builds owns its catalog/audit validators, and both required Builds commits plus all three validators are recorded and green. The Agents CI obligation is the shared consumer-authority and effective-floor check, not duplicating the owning repository's audit lane. | reject |
| BH-15 | low | The new process helpers can wait until the enclosing CI job timeout if an SDK child hangs. Such hangs are uncommon, and adding timeout/kill/output-drain branches is more complex than a direct correction for this focused conformance test. | reject |
| BH-16 | false | `in-review` is the spec workflow state set immediately before review, while sprint status remains `in-progress` until review succeeds; this transient distinction is prescribed by the workflow rather than conflicting delivery evidence. | reject |
| ECH-01 | false | Build-package path properties intentionally support established layouts and test injection. Normal validation resolves the default wrapper import to the passed authoritative catalog and compares the complete effective version set; arbitrary caller redirection is not a supported catalog. | reject |
| ECH-02 | low | Confirmed as the same explicit global-marker spoof described by BH-03. It is possible only when the caller intentionally supplies the internal marker, and a robust anti-spoof design would add disproportionate provenance complexity. | reject |
| ECH-03 | false | A property becomes package-specific authority only when it feeds a package version; authoritative catalog properties are rejected directly, while local `PackageVersion` and `PackageReference` metadata are independently rejected. An unused arbitrary `*Version` property does not alter the package graph. | reject |
| ECH-04 | high | Confirmed as BH-01: the local gitlink is not yet fetchable from origin. Publishing it is external integration work that the frozen intent forbids this run from performing. | defer |
| ECH-05 | low | Confirmed as BH-15 for the `dotnet` helper: an indefinite child wait is possible but rare, and a safe timeout requires additional kill/output-drain control flow. | reject |
| ECH-06 | low | Confirmed as BH-15 for the `pwsh` helper: an indefinite child wait is possible but rare, and a safe timeout requires additional kill/output-drain control flow. | reject |
| VG-01 | medium | Pre-verified searches show the new fourth `../../references/...` fallback has no positive evaluation test; deleting or mistyping only that import leaves all current tests green while breaking the nested parent checkout. | patch |

## Verification

**Commands:**
- `pwsh -NoProfile -File ./Tools/validate-central-package-versions.ps1 && pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1` from `references/Hexalith.Builds` -- catalog and authority tests pass.
- `pwsh -NoProfile -File ./Tools/audit-central-package-versions.ps1 -PriorAuditPath ./Tools/package-version-audit.json -Family hexalith-eventstore` followed by `validate-package-version-audit.ps1` -- audit is regenerated and valid.
- `pwsh -NoProfile -File references/Hexalith.Builds/Tools/validate-consumer-package-authority.ps1 -RepositoryRoot . -CatalogPath references/Hexalith.Builds/Props/Directory.Packages.props` -- zero consumer violations.
- `pwsh -NoProfile -File ./eng/verify-story-5.2.ps1 -PackageFloorOnly` -- effective EventStore floor passes.
- `pwsh -NoProfile -File ./eng/verify-story.ps1 -Story 5.1` -- complete Story 5.1 lane passes.
- `dotnet restore Hexalith.Agents.slnx -p:UseHexalithProjectReferences=false && dotnet build Hexalith.Agents.slnx -c Release --no-restore -p:UseHexalithProjectReferences=false` -- package-mode solution is green.

**Results (2026-09-16):**

- Both Builds catalog validators passed for 286 entries; the generated audit passed with 286 packages, 141 families, and one source.
- Shared consumer-authority validation passed across 12 Agents projects; the effective package-floor check resolved EventStore `3.106.0`.
- Focused `BuildContractConformanceTests` passed 8/8 and `PackageVersionCentralizationTests` passed 4/4.
- The complete Story 5.1 verifier passed warning-free Debug/source and Release/package builds. Debug suites passed 2,956 tests and Release suites passed 2,932 tests, all with zero failures and zero skips.
- Release source-policy negative probes, exact six-package validation, and isolated six-package consumer validation all passed.
