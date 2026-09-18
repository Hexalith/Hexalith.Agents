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
| Local override | Agents adds a version property/item or project metadata | Authority validation and Story 5.1 verification fail | Diagnostic identifies the offending declaration |
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
- [x] Story 5.1 verification and CI -- invoke the shared consumer-authority validator and test root, parent-layout, missing-catalog, local-override, and effective-version behavior without duplicating its scanner.
- [x] Planning and implementation artifacts -- reconcile Stories 5.1/5.2/5.6, architecture assumptions/index, UX ownership wording, DW-20, proposal execution evidence, and sprint status with the landed commits and verification results.

**Acceptance Criteria:**
- Given any tracked Agents project/build file, when package authority is validated, then no local package version declaration exists and the imported Builds catalog is the sole effective source.
- Given supported checkout layouts, when restore/build evaluates the wrapper, then it loads the shared catalog; an unsupported or missing catalog fails with an actionable diagnostic.
- Given Release package mode, when Story 5.2 checks EventStore, then the effective catalog value is at least `3.105.0` and currently resolves to `3.106.0`.
- Given the completed change, when Builds validators, Story 5.1 verification, focused tests, Release restore/build, and the package-consumer lane run, then all pass and documentation cites actual commit/version evidence.

### Review Findings

- [x] [Review][Patch] Nested parent-layout test never selects the Hexalith4 catalog import [test/Hexalith.Agents.Server.Tests/BuildContractConformanceTests.cs:105]
- [x] [Review][Patch] Pack/Publish unloaded-catalog guard is only asserted as MSBuild XML text [Directory.Build.props:78]
- [x] [Review][Patch] Remaining-work still claims unpublished Builds commits and dumps that claim as a key-less DW-21 trailer [_bmad-output/implementation-artifacts/deferred-work.md:243]
- [x] [Review][Patch] Epics Story 5.1 evidence still says verification was not run / backlog [_bmad-output/planning-artifacts/epics.md:1287]
- [x] [Review][Patch] RequiredRootSubmodule item is unused after the catalog guard moved to HexalithVersionsLoaded [Directory.Build.props:74]
- [x] [Review][Patch] Architecture delivery-debt still records Story 5.1 as in-progress [_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md:1355]
- [x] [Review][Patch] Key-less remaining-work trailer is still dumped onto DW-21 [_bmad-output/implementation-artifacts/deferred-work.md:243]
- [x] [Review][Patch] Sprint status claims Story 5.1 workflow review is already green [_bmad-output/implementation-artifacts/sprint-status.yaml:87]
- [x] [Review][Patch] Epics evidence table still names superseded Story 5.1's test classes [_bmad-output/planning-artifacts/epics.md:1284]
- [x] [Review][Patch] ARCH-A-15 is retired while its authoritative `OD-DAPR-SECURITY-1` approval remains open [_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md:1400]
- [x] [Review][Patch] Story 5.1 compatibility evidence overclaims Dapr Workflow coverage even though only Client/ASP.NET are consumed [_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md:911]
- [x] [Review][Patch] Triage presents mutable Agents `origin/main` as immutable commit `01ead38` [_bmad-output/implementation-artifacts/spec-5-1-adopt-hexalith-builds-as-the-sole-package-version-authority.md:89]
- [x] [Review][Defer] Packed EventStore dependency asset-exclusion metadata is not retained [scripts/validate-nuget-packages.py:197] — deferred: maybe-false; a mutated-package restore and `project.assets.json` comparison would settle whether omitting generated exclusions creates a consumer leak
- [x] [Review][Patch] Sprint status still claims Story 5.1 workflow review is complete [_bmad-output/implementation-artifacts/sprint-status.yaml:87]
- [x] [Review][Patch] PackageInventoryTests does not lock the new CI authority and EventStore-floor steps [test/Hexalith.Agents.Server.Tests/PackageInventoryTests.cs:82]
- [x] [Review][Patch] Architecture assumption index jumped 10 to 13 with no memlog entries [_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/.memlog.md:433]
- [x] [Review][Defer] GlobalJsonShouldPinTheSdk does not assert test.runner [test/Hexalith.Agents.Server.Tests/BuildContractConformanceTests.cs:222] — deferred: pre-existing; global.json is unchanged in this diff and already contains Microsoft.Testing.Platform

#### Rejected

- false: Uninitialized root submodule loading a sibling/parent catalog, and the missing-catalog diagnostic always naming the root-only init command — supported layouts and the frozen I/O matrix require first-existing catalog resolution and that exact diagnostic.
- false: Hexalith.Agents.EventStore nuspec flattening (EventStore.Server/ServiceDefaults) plus no catalog-version compare — the EventStore integration package's restore graph is what pack emits; inspected nuspec versions already match the catalog, and this story's authority AC is consumer-declaration absence, not Gateway graph reshaping.
- false: RuntimeOwnershipConformanceTests no longer scanning wrapper PackageVersion items for Dapr.Workflow — import-only wrappers have no PackageVersion items; project PackageReference scanning remains, and Builds catalog entries are not Agents pins.
- false: This diff moves Story 5.2 from done to review without product change — the approved proposal keeps 5.2 in review for independent blockers after DW-20 closed.
- false: Story 5.1 advances Dapr to 1.18.7 — gitlink `aee0132` already selected Dapr 1.18.7; this range only changes EventStore `3.104.0` → `3.106.0`.
- false: Removing local `Microsoft.NET.Test.Sdk` `18.10.1` silently downgrades to catalog `18.10.0` — the story forbids Agents-local version authority; `18.10.0` is the imported catalog selection already recorded in the spine.
- false: Shared-validator tests never pass a non-empty `buildDeclaration` / never assert the real repo — the production validator is invoked on the real tree by `eng/verify-story.ps1` and CI; the unused fixture parameter does not leave Directory.Build.props unscanned in those lanes.
- false: Test/consumer CI jobs restore without the new authority YAML anchors, so the gate is skippable — `package-build` runs the validator and EventStore floor; a failure there fails the workflow.
- false: `EvaluateMsBuild` throws JsonException when stderr is non-empty — the cited `-getProperty` invocation writes JSON only to stdout (stderr 0 bytes) and the tests pass.
- false: CI cannot fetch gitlink `000abf867abc3a99cfa74d39b6e73af05c78a602` from origin — `origin/main` in Hexalith.Builds contains that commit, and Agents commit `01ead38`, published on `origin/main`, records it.
- false (spec edit): Spec YAML `status: done` versus changelog `in-progress` — fixing that edits the spec under review; sprint-status already records `review`.
- low: `RunDotNet` / pwsh `WaitForExit()` with no timeout — a hung SDK child is uncommon, and a safe timeout needs kill plus stream-drain branches beyond a direct correction.
- low: `PackageInventoryTests` does not require the CI file to contain `validate-consumer-package-authority.ps1` — the step exists on `package-build`, and pinning every new CI line in that string test is not a defect developers hit in everyday use.
- false: Directory.Build.props source roots omit the Hexalith4 nested-parent path — wrapper ACs and layout tests cover catalog import only; Debug/source still requires the Agents root-declared or sibling EventStore checkout and fails closed when it is missing.
- false: CheckBuildCatalog treats `HexalithVersionsLoaded=true` as catalog proof — that is the spec's loaded-catalog contract; a marker-only stub still fails CPM restore without `PackageVersion` rows.
- false: EventStore floor prefix-matches extra family packages, has a dead unsuffixed id, and tests hardcode `3.106.0` — evaluated `PackageVersion` rows are the restore source, the family shares `3.106.0`, and that pin is the frozen matrix's current selection.
- false: Hexalith.Agents.EventStore nuspec flattening plus no catalog-version compare — pack emits the restore graph; this story's authority AC is consumer-declaration absence, not Gateway graph reshaping.
- false: Missing-catalog Pack/Publish tests never assert empty output directories — `CheckBuildCatalog` errors before those targets, and the tests already require a non-zero exit plus the catalog diagnostic.
- false: Epics still require Story 5.1 to remain backlog while `EXT-HOST-1` is Uncommitted — that clause is a ready-for-dev entry gate; this reopen is package-authority only, and `EXT-HOST-1` continues to gate later host/launch stories.
- false: Story 5.1 CI AC says "inline project version" — that is the approved proposal wording, next to separate `PackageVersion` and override clauses.
- false: Shared-validator negatives never pass `buildDeclaration` / never cover `Update` / `GlobalPackageReference` / child `Version`; new CI steps are not anchors and are absent from `package-consumer` — the production validator scans the real tree; `ci.yml` already uses YAML anchors; a `package-build` failure fails the workflow.
- false: Floor reads `PackageVersion` items rather than `PackageReference` Version — under CPM those items are the restore versions; project `Version` / `VersionOverride` is already rejected by centralization tests and the shared validator.
- false (spec edit): Spec YAML `status: done` / `review_loop_iteration: 0` versus changelog `in-progress` — fixing that edits the spec under review; sprint-status already records `review`.
- false: DW-20 `done 2026-09-16` versus 2026-09-17 verification — the status date is the close date; the resolution already cites the later verifier evidence.
- low: `RunDotNet` / pwsh `WaitForExit()` with no timeout — a hung SDK child is uncommon, and a safe timeout needs kill plus stream-drain branches beyond a direct correction.
- false: Architecture Assumptions preamble still treats ARCH-A-4 as a live scoping rule — the table marks it **RETIRED 2026-09-16**; the leftover sentence restates the Stack Windows/graph security observation, it does not emit `UnretiredAssumption`.
- false: ARCH-A-8 still says Fluent UI is "pinned" — that row is the RC-versus-GA production risk; Stack already records catalog ownership and "Agents owns no local pin."
- false: `OD-SPRINT-5.1-5.2-1` remaining Open blocks dependent-story authorization — that is the recorded safe state until Delivery owner + Platform Maintainer choose; this change does not authorize closing it.
- false: EXPERIENCE.md still cites `reconcile-validation-2026-09-12.md` after a 2026-09-17 pass — the banner states the 2026-09-17 work is dependency-ownership only; 2026-09-12 remains the last UX-behavior reconciliation, and DESIGN.md is not a currency denominator in that banner.
- false: Floor tests assert `HexalithEventStoreVersion` while the gate reads `PackageVersion` rows, with no mixed-row case — CI and `PackageFloorOnly` already fail closed on evaluated EventStore family items; the catalog binds that family to one property at `3.106.0`.
- false: Hexalith4 nested-parent catalog resolution has no matching Debug source roots — wrapper ACs and layout tests cover catalog import; missing EventStore source still fails closed.
- false: `validate-nuget-packages.py` requires a flattened EventStore host graph and skips catalog-version compare — pack emits the restore graph; this story's authority AC is consumer-declaration absence, not Gateway graph reshaping.
- low: `eng/verify-story.ps1` and CI always pass `references/Hexalith.Builds/Props/Directory.Packages.props` — everyday GitHub and root-submodule checkouts use that path; a sibling/parent resolver would add layout branches this repo's CI never runs.
- false: The approved proposal still tells implementers to retain an MSBuild-property floor check — §4.9 is the pre-implementation handoff; the implementation-evidence section records the landed `PackageVersion`/gitlink facts.
- false: Story 5.2's verification section still records the 2026-09-15 suite after the floor rewrite — Story 5.1 cites 2026-09-17 evidence and dedicated floor tests; the 5.2 spec already cross-links that later floor result.
- false: `AssertSharedValidatorRejects` always passes an empty `buildDeclaration`, and the Agents wrapper never sets `CentralPackageVersionOverrideEnabled=false` — `eng/verify-story.ps1` and CI invoke the shared validator on the real tree, and the imported catalog already evaluates the override to `false`.
- false (spec edit): Spec implementation notes still say Client/ASP.NET-only compatibility evidence — spine, epics, and launch-readiness already say Client/ASP.NET/Actors; fixing the leftover notes would edit the spec under review.

## Implementation Notes

- Hexalith.Builds catalog commit `dae84d5f96911517eb1e6f97e75eb88e759e6d8a` advances the EventStore family to published `3.106.0`; generated-audit commit `000abf867abc3a99cfa74d39b6e73af05c78a602` binds the regenerated audit to that catalog.
- The Agents wrapper now contains only CPM policy, supported-layout path selection, and guarded imports. The early build target checks `HexalithVersionsLoaded` and reports the exact root-only submodule initialization command when no catalog loads.
- Conformance tests use the shared consumer-authority validator for all declaration negatives. Separate fixture tests prove root, sibling, parent-references, missing-catalog, and effective EventStore-version behavior.
- Story verification and CI run shared authority validation before the EventStore floor and existing source/package lanes. The package validators were reconciled with the existing six-package release manifest, including the EventStore integration package and its isolated-consumer marker.
- Epics, Architecture Spine assumption index 13, UX package ownership, Story 5.2 evidence, DW-20, the approved proposal, and sprint status now distinguish the resolved package floor from Story 5.2's independent blockers.
- Builds `origin/main` contains catalog/audit target `000abf867abc3a99cfa74d39b6e73af05c78a602`, and Agents commit `01ead38` published on `origin/main` records that exact gitlink. The former fresh-clone integration risk is resolved, while `ARCH-A-15` remains open pending approval of `OD-DAPR-SECURITY-1`; current compatibility evidence covers only consumed Client/ASP.NET, and Workflow remains Story 6.1 work.
- Follow-up review now locks both package-authority CI commands in `PackageInventoryTests`, records each ARCH-A index transition from 10 through 13 in the architecture memlog, and keeps the sprint comment aligned with the in-progress workflow state.

## Spec Change Log

- 2026-09-16 — Implemented the approved shared package-authority correction, recorded exact Builds commits and local verification, closed DW-20, and retained Story 5.1 as `in-progress` pending review/integration.
- 2026-09-17 — Patched all six review findings, added behavioral Pack/Publish missing-catalog coverage, reconciled publication evidence, and kept `ARCH-A-15` open for the authoritative Security + Builds approval after the Builds and parent gitlinks became upstream-fetchable.
- 2026-09-18 — Patched the remaining review findings for sprint-status accuracy, CI gate regression coverage, and architecture assumption-index history; reran focused and complete Story 5.1 verification.

## Review Triage Log

| ID | Verdict | Evidence | Route |
| --- | --- | --- | --- |
| BH-01 | resolved | Builds `origin/main` now contains selected target `000abf867abc3a99cfa74d39b6e73af05c78a602`, and Agents `origin/main` records that exact gitlink. | close |
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
| ECH-04 | resolved | Confirmed as BH-01: both the Builds target and the exact parent gitlink are now present on their locally known `origin/main` refs. | close |
| ECH-05 | low | Confirmed as BH-15 for the `dotnet` helper: an indefinite child wait is possible but rare, and a safe timeout requires additional kill/output-drain control flow. | reject |
| ECH-06 | low | Confirmed as BH-15 for the `pwsh` helper: an indefinite child wait is possible but rare, and a safe timeout requires additional kill/output-drain control flow. | reject |
| VG-01 | medium | Pre-verified searches show the new fourth `../../references/...` fallback has no positive evaluation test; deleting or mistyping only that import leaves all current tests green while breaking the nested parent checkout. | patch |
| R2-ECH-01 | false | carried: ECH-01 already established that catalog-path properties intentionally support the approved layouts and test injection; arbitrary caller redirection is not a supported authoritative invocation. | reject |
| R2-ECH-02 | false | carried: BH-06 already rejected the same partial-import claim because the supported synthetic and real catalogs import atomically and the real-root validator compares the complete effective catalog. | reject |
| R2-ECH-03 | low | The helper combines stdout and stderr, but the exercised successful `-getProperty` calls emit JSON only on stdout and the full suite passes. Handling a hypothetical successful SDK diagnostic stream requires restructuring the helper for an uncommon condition. | reject |
| R2-ECH-04 | low | carried: ECH-05/BH-15 already established that an SDK child can wait until the enclosing job timeout, but safe kill and stream-drain handling is disproportionate for this uncommon test-helper failure. | reject |
| R2-ECH-05 | low | carried: ECH-06/BH-15 already established the same uncommon validator-child timeout risk and rejected the nontrivial kill/output-drain machinery. | reject |
| R2-ECH-06 | low | carried: ECH-02/BH-03 already established that an explicit global `HexalithVersionsLoaded=true` can spoof the internal marker; preventing deliberate command-line spoofing adds disproportionate provenance state. | reject |
| R2-BH-01 | medium | The floor gate reads the family property rather than the evaluated `PackageVersion` rows restore consumes. A stale or literal EventStore row could therefore pass the floor while selecting an older shipped package, contrary to the effective-selection acceptance criterion. | patch |
| R2-BH-02 | low | `[Version]` rejects valid SemVer labels, but the frozen floor is a stable published `3.106.0` selection. Supporting hypothetical prerelease catalog values requires defining NuGet precedence and adding parser dependencies beyond this direct stable-version gate. | reject |
| R2-BH-03 | false | Story 5.1 explicitly owns preservation of Story 5.2's EventStore release floor; invoking the later verifier's isolated `-PackageFloorOnly` mode implements that requirement without depending on Story 5.2's wider behavior. | reject |
| R2-BH-04 | low | `InitialTargets` also blocks `Clean` when the catalog is deliberately absent, but the approved boundary requires unloaded catalogs to fail clearly and normal initialized checkouts are unaffected. Narrowing target selection risks weakening the proven Pack/Publish guard for an uncommon recovery state. | reject |
| R2-BH-05 | false | The shared validator scans all tracked project/build XML for forbidden version declarations and compares each project's complete evaluated `PackageVersion` set with the catalog even in Debug; the full Release/package restore and build separately exercise conditional package references. | reject |
| R2-BH-06 | false | The verified lane deletes and rebuilds the packages after shared-authority validation, so MSBuild generates external dependency versions from the sole effective catalog. No reachable source path that can produce a divergent external nuspec version under that lane was demonstrated. | reject |
| R2-BH-07 | maybe-false | The validator does not retain nuspec dependency asset metadata, but the review did not establish that the current EventStore dependencies expose build/analyzer assets or that removing generated exclusions changes a consumer asset graph. A mutated-package restore/assets comparison would settle the claimed medium leak. | defer |
| R2-BH-08 | false | The isolated consumer proves every package identity and assembly loads from the package graph, while exact dependency IDs are validated separately and `AddAgentsEventStore` behavior is exercised by the integration suite. The frozen package-version-authority goal does not require duplicating that API behavior in the consumer probe. | reject |
| R2-BH-09 | medium | The architecture and epics claim MediatR is absent from the current runtime dependency graph while the produced EventStore integration package declares it transitively. That wording can misroute the recorded license-compliance obligation. | patch |
| R2-BH-10 | low | Sprint status still describes the six prior review patches as open action items even though the spec and current diff mark all six resolved. | patch |
| R2-BH-11 | low | The verification results heading says 2026-09-16 while the recorded complete run immediately below is timestamped 2026-09-17. | patch |
| R2-BH-12 | low | Story 5.2's release table remains the 2026-09-15 story-specific run and does not point readers to the later 2026-09-17 post-catalog full-project package evidence recorded by Story 5.1. | patch |
| R3-BH-01 | low | A deliberate command-line `HexalithEventStoreVersion` global property can override the catalog default. This is the same uncommon explicit-spoof class as BH-03/ECH-02, and making shared defaults immutable would add disproportionate provenance machinery. | reject |
| R3-BH-02 | false | carried: ECH-01/BH-04 already established that the path properties support approved layout resolution and test injection; arbitrary or ambiguous catalog redirection is not a supported authoritative invocation. | reject |
| R3-BH-03 | low | A project could deliberately redefine the uniquely named `CheckBuildCatalog` target, but no current project does so and preventing hostile target shadowing requires a new shared-scanner rule. This is not an everyday developer failure. | reject |
| R3-BH-04 | low | The shared scanner's XML-name comparisons are case-sensitive even though contrived casing variants can be evaluated by MSBuild. Repository declarations use canonical casing, and hardening the owning Builds validator plus its published gitlink is disproportionate for this deliberate-evasion case. | reject |
| R3-BH-05 | false | No tracked Agents `.proj` or imported `.xml` build file exists; all current project/build XML is covered by the validator's `.csproj`, `.props`, and `.targets` scan. The finding demonstrates only a hypothetical future import. | reject |
| R3-BH-06 | false | carried: BH-06/R2-ECH-02 already established that the synthetic layout fixtures isolate path selection while the real-repository shared validator compares every effective `PackageVersion` row. | reject |
| R3-BH-07 | false | The frozen floor criterion checks the effective shared EventStore selection. Removing or redirecting the actual EventStore references would independently fail the build, package inventory, and integration suites; the floor-only gate is not their replacement. | reject |
| R3-BH-08 | false | The authoritative EventStore family rows use one shared property and the Builds catalog validators enforce that catalog. The floor gate owns only the durable minimum and correctly accepts every effective row at or above it. | reject |
| R3-BH-09 | false | carried: BH-11/R2-BH-06 already established that the verified lane deletes and rebuilds packages from the validated catalog and no reachable source path to a stale external nuspec version was shown. | reject |
| R3-BH-10 | false | The generated audit explicitly records the EventStore family as retained pending consumer acceptance; the story claims catalog provenance binding, not that the audit itself contains downstream Agents compatibility evidence. | reject |
| R3-BH-11 | medium | The Architecture Spine retires ARCH-A-15 while the active launch-readiness register still records `OD-DAPR-SECURITY-1` as Open and names its approved outcome as a prerequisite to retirement. That contradiction can incorrectly authorize Story 6.1 Dapr Workflow adoption. | patch |
| R3-BH-12 | medium | The Spine's retirement wording implies compatibility evidence for the complete Dapr family even though the verified Agents graph consumes only Client/ASP.NET and explicitly leaves Workflow to Story 6.1. The evidence scope must remain precise while `OD-DAPR-SECURITY-1` is Open. | patch |
| R3-BH-13 | false | Story 5.2 remains in review, its command list states an expected result rather than a newly recorded run, and the post-catalog link explicitly says it is not Story 5.2-specific evidence. | reject |
| R3-BH-14 | false | `review_loop_iteration` counts this workflow's loopbacks, not every historical review label, and the change-log statement accurately refers to the original six findings patched in that recorded round. | reject |
| R3-BH-15 | low | The triage sentence presents mutable `origin/main` as if it still equals `01ead38`; that branch has advanced even though commit `01ead38` remains the commit that recorded the gitlink. | patch |
| R3-ECH-01 | low | carried: ECH-02/BH-03 already established that an explicit global `HexalithVersionsLoaded=true` can spoof the internal marker, but robust anti-spoof provenance is disproportionate for deliberate command-line override. | reject |
| R3-ECH-02 | false | carried: ECH-01/BH-04 already established that first-existing path selection is the approved supported-layout behavior; ambiguous unrelated checkouts are not supported authoritative invocations. | reject |
| R3-ECH-03 | low | carried: R2-ECH-03 established that exercised successful MSBuild evaluation emits JSON on stdout without stderr; restructuring stream capture for a hypothetical successful diagnostic is disproportionate. | reject |
| R3-ECH-04 | low | carried: ECH-05/BH-15 and R2-ECH-04 established that an SDK child can wait until the enclosing job timeout, but safe timeout, kill, and stream-drain handling is disproportionate for this uncommon verification-helper failure. | reject |
| R3-ECH-05 | low | carried: R2-ECH-03/R2-ECH-04 established the same combined-output and unbounded-wait risks in `RunDotNet`; the exercised calls pass and the robust process-control fix is nontrivial. | reject |
| R3-ECH-06 | low | carried: ECH-06/BH-15 and R2-ECH-05 established the same uncommon validator-child timeout risk and rejected the nontrivial kill/output-drain machinery. | reject |
| R3-ECH-07 | maybe-false | carried: R2-BH-07 already deferred the unproven dependency asset-exclusion leak and recorded the exact mutated-package `project.assets.json` comparison needed to settle it as DW-22. | defer |
| R3-VG-01 | false | carried: BH-11/R2-BH-06 already rejected the stale external-version claim because the verified lane rebuilds packages after catalog validation. The mutation proves validators accept externally rewritten artifacts but still does not establish a reachable production path that rewrites those nuspecs. | reject |
| R4-BH-01 | false | carried: BH-06/R2-ECH-02/R3-BH-06 already established that supported layout fixtures isolate one atomic catalog import while real-root authority validation compares the complete effective catalog. | reject |
| R4-BH-02 | false | carried: R3-BH-06 already established that the synthetic fixtures test path selection and the real repository validator supplies the complete effective-version comparison. | reject |
| R4-BH-03 | false | The root wrapper asserts transitive pinning and no current project or later import disables it. Disabling a policy in a hypothetical future project would not itself create a second package-version declaration in the current tree. | reject |
| R4-BH-04 | false | No tracked Agents build file contains `PackageDownload`; the shared validator proves authority for the package-reference forms the repository actually consumes. The finding demonstrates only a hypothetical future item type. | reject |
| R4-BH-05 | medium | Pre-verified searches show the changed EventStore floor gate has only current-catalog happy-path coverage; below-floor, malformed, and absent rows are executable branches with no regression test. A broken comparison could therefore allow an incompatible EventStore package into Release mode. | patch |
| R4-BH-06 | medium | The packed EventStore integration graph demonstrably contains `Dapr.Actors` and `Dapr.Actors.AspNetCore`, so describing compatibility evidence as Client/ASP.NET-only understates the dependency/security scope. | patch |
| R4-BH-07 | medium | The Spine removed `OD-DAPR-SECURITY-1` from its blocking-open-decision table while retaining that decision as the retirement condition for `ARCH-A-15`, leaving a dangling architecture reference. | patch |
| R4-BH-08 | medium | The launch-readiness decision contract still frames the already-landed `1.18.7` upgrade as an unmade choice and cites the obsolete dirty checkout, which misstates the remaining Security/Builds approval obligation. | patch |
| R4-BH-09 | low | `EXPERIENCE.md` explicitly claims a checkable `ARCH-A-INDEX-7` currency baseline after this change advances the authoritative Spine to index 13. The stale banner can misroute later UX reconciliation. | patch |
| R4-BH-10 | low | Sprint status is `review` and the proposal says review is pending, but the sprint comment says workflow review is complete. That contradiction is a direct wording error. | patch |
| R4-BH-11 | low | The proposal attributes layout and missing-catalog coverage to the shared validator even though `BuildContractConformanceTests` owns those cases; the inaccurate attribution can cause maintainers to preserve the wrong guard. | patch |
| R4-BH-12 | false | The persisted table is explicitly Release evidence; the adjacent results record the Debug/source count, and the full verifier rerun independently produced all 2,962 passing Debug/source tests. No second table is required by the acceptance contract. | reject |
| R4-BH-13 | maybe-false | carried: R2-BH-07/R3-ECH-07 already recorded the unproven dependency asset-exclusion concern as DW-22 with the exact experiment needed to settle it. A checked deferred finding means triage completed, not that the deferred concern was resolved. | defer |
| R4-BH-14 | low | The changed epic wording says every NuGet version comes from Builds even though Agents' own package identity/version is intentionally supplied by the pack lane. The statement must be narrowed to dependency/package-reference versions. | patch |
| R4-ECH-01 | low | carried: ECH-02/BH-03/R3-ECH-01 already established that an explicit global marker can spoof the guard, but robust anti-spoof provenance is disproportionate for a deliberate command-line override. | reject |
| R4-ECH-02 | false | carried: BH-06/R2-ECH-02/R3-BH-06 already rejected the same partial-catalog merge claim for the approved atomic supported-layout resolution and complete real-root validation. | reject |
| R4-ECH-03 | low | carried: R2-ECH-03/R3-ECH-03 already established that exercised successful MSBuild evaluation emits JSON on stdout without stderr; restructuring capture for a hypothetical successful diagnostic is disproportionate. | reject |
| R4-ECH-04 | low | carried: R2-BH-02 already established that the frozen floor uses stable published `3.106.0`; introducing NuGet semantic-version parsing for hypothetical prerelease catalog values is disproportionate. | reject |
| R4-ECH-05 | low | carried: BH-15/R2-ECH-04 already established that an SDK child may wait for the enclosing job timeout, but safe timeout, kill, and stream-drain control is disproportionate for this uncommon gate-helper failure. | reject |
| R4-ECH-06 | low | carried: R2-ECH-03/R3-ECH-05 already established the same combined-output risk in `RunDotNet`; exercised calls pass and robust stream separation is nontrivial. | reject |
| R4-ECH-07 | low | carried: BH-15/R2-ECH-04/R3-ECH-04 already established the uncommon unbounded `dotnet` child wait and rejected the nontrivial process-control machinery. | reject |
| R4-ECH-08 | low | carried: ECH-06/R2-ECH-05/R3-ECH-06 already established the uncommon validator-child timeout risk and rejected the nontrivial kill/output-drain machinery. | reject |
| R4-ECH-09 | maybe-false | carried: R2-BH-07/R3-ECH-07 already deferred the unproven dependency asset-exclusion leak as DW-22; no duplicate deferral is created. | defer |
| R4-VG-01 | medium | Pre-verified: searches and mutation reasoning show the floor gate's below-floor, malformed, and missing-row failure branches have no executable negative regression test; current `3.106.0` happy-path runs cannot expose a removed comparison. | patch |
| R4-VG-02 | false | carried: BH-11/R2-BH-06/R3-VG-01 already rejected the stale external-nuspec-version claim because the verified lane rebuilds packages from the validated catalog and no reachable production rewrite path was shown. | reject |
| R4-VG-03 | false | carried: BH-06/R2-ECH-02/R3-BH-06 already rejected the same partial-catalog merge claim for supported layouts and complete real-root validation. | reject |
| R4-VG-04 | medium | Verified: the produced `Hexalith.Agents.EventStore` nuspec includes `Dapr.Actors` and `Dapr.Actors.AspNetCore` `1.18.7`, so the changed Client/ASP.NET-only compatibility narrative is incomplete. | patch |
| R5-BH-01 | low | carried: BH-03/R2-ECH-06/R3-ECH-01 already established that a caller can deliberately spoof `HexalithVersionsLoaded=true`; robust catalog provenance is disproportionate for this uncommon explicit override. | reject |
| R5-BH-02 | false | carried: BH-06/R2-ECH-02/R4-ECH-02 already established that supported catalog resolution is atomic and the real-root validator compares the complete effective catalog; the claim depends on an unsupported marker-less catalog. | reject |
| R5-BH-03 | low | carried: R2-BH-04 already established that `InitialTargets` also blocks `Clean` without a catalog, but the approved fail-closed boundary applies to unloaded catalogs and narrowing it risks the proven Pack/Publish guard. | reject |
| R5-BH-04 | false | carried: R3-BH-06/R4-BH-01 already established that layout fixtures isolate path selection while the real repository validator proves the complete effective `PackageVersion` set. | reject |
| R5-BH-05 | low | `PackageFloorProjectPath` can alter the floor target only when a caller deliberately supplies the non-default test seam; the recorded evidence command, Story 5.1 verifier, and CI never pass it, and preventing misuse would add a special-case verifier branch for an uncommon explicit override. | reject |
| R5-BH-06 | false | carried: R3-BH-07 already established that the floor gate checks the effective shared selection and is not a replacement for build, package inventory, or integration gates, all of which fail if real EventStore references disappear. | reject |
| R5-BH-07 | low | carried: R2-BH-02/R4-ECH-04 already established that the frozen floor uses stable published `3.106.0`; adding NuGet prerelease precedence and parsing is disproportionate to this stable-version gate. | reject |
| R5-BH-08 | false | The production validator scans the real repository, including `Directory.Build.props`, and the imported catalog evaluates `CentralPackageVersionOverrideEnabled=false`; the unused fixture parameter does not leave consumer authority unverified. | reject |
| R5-BH-09 | low | Raw-text assertions could be satisfied by commented YAML, but the current commands are executable in `package-build` and both ran in the complete verifier; structurally parsing Actions YAML would add disproportionate test machinery for a future deliberate/comment-only bypass. | reject |
| R5-BH-10 | false | carried: BH-11/R2-BH-06/R3-VG-01 already established that the verified lane rebuilds packages after catalog validation and inspected nuspec versions match the catalog; no reachable production path to rewritten external versions was shown. | reject |
| R5-BH-11 | maybe-false | carried: R2-BH-07/R3-ECH-07/R4-BH-13 already recorded the unproven dependency asset-metadata concern as DW-22 with the mutated-package `project.assets.json` experiment needed to settle it. | defer |
| R5-BH-12 | low | carried: BH-15/R3-ECH-04/R4-ECH-07 already established that SDK or validator children can wait for the enclosing job timeout, but safe timeout, kill, and stream-drain handling is disproportionate for this uncommon helper failure. | reject |
| R5-BH-13 | medium | The implementation note at line 132 understates the exercised dependency graph by omitting Dapr Actors, but the architecture, epic, and readiness artifacts carry the corrected Client/ASP.NET/Actors claim; the only remaining fix would edit this build's spec, which review findings must reject. | reject |
| R5-BH-14 | false | `deferred-work-archive.md` was a pre-existing unrelated worktree change preserved by the implementation handoff, not a Story 5.1 implementation file; the Dev Agent Record correctly lists the story-owned files. | reject |
| R5-BH-15 | false | Repeated findings are intentionally retained as separate carried rows because the review workflow requires one verdict for every finding in every loop and forbids silently deduplicating them. | reject |
| R5-BH-16 | false | The unkeyed deferred bullet exactly follows the review workflow's required append-only format for a pre-existing issue; it is not a malformed `DW-*` record. | reject |
| R5-BH-17 | false | The `555c9047` sentence sits inside the dated Round 2 implementation history and records the pointer produced by that round; later paragraphs separately record the current package/catalog promotion, so it does not claim to be the current EventStore gitlink. | reject |
| R5-ECH-01 | low | carried: R3-BH-01 already established that an explicit global `HexalithEventStoreVersion` override can spoof the catalog default, but immutable shared defaults would add disproportionate provenance machinery for deliberate command-line tampering. | reject |
| R5-ECH-02 | low | carried: R2-BH-02/R4-ECH-04 already established that NuGet prerelease parsing is unnecessary for the frozen stable `3.106.0` selection and would add disproportionate precedence machinery. | reject |
| R5-ECH-03 | low | carried: BH-03/R2-ECH-06/R3-ECH-01 already established the deliberate global-marker spoof and rejected the disproportionate anti-spoof provenance machinery. | reject |
| R5-ECH-04 | low | carried: BH-15/R2-ECH-04/R4-ECH-07 already established the uncommon unbounded `dotnet` child wait and rejected the nontrivial timeout, kill, and stream-drain branches. | reject |
| R5-ECH-05 | low | carried: BH-15/R2-ECH-05/R3-ECH-06 already established the uncommon unbounded validator-child wait and rejected the nontrivial timeout, kill, and stream-drain branches. | reject |

## Verification

**Commands:**
- `pwsh -NoProfile -File ./Tools/validate-central-package-versions.ps1 && pwsh -NoProfile -File ./Tools/test-authoritative-package-catalog.ps1` from `references/Hexalith.Builds` -- catalog and authority tests pass.
- `pwsh -NoProfile -File ./Tools/audit-central-package-versions.ps1 -PriorAuditPath ./Tools/package-version-audit.json -Family hexalith-eventstore` followed by `validate-package-version-audit.ps1` -- audit is regenerated and valid.
- `pwsh -NoProfile -File references/Hexalith.Builds/Tools/validate-consumer-package-authority.ps1 -RepositoryRoot . -CatalogPath references/Hexalith.Builds/Props/Directory.Packages.props` -- zero consumer violations.
- `pwsh -NoProfile -File ./eng/verify-story-5.2.ps1 -PackageFloorOnly` -- effective EventStore floor passes.
- `pwsh -NoProfile -File ./eng/verify-story.ps1 -Story 5.1` -- complete Story 5.1 lane passes.
- `dotnet restore Hexalith.Agents.slnx -p:UseHexalithProjectReferences=false && dotnet build Hexalith.Agents.slnx -c Release --no-restore -p:UseHexalithProjectReferences=false` -- package-mode solution is green.

**Results (2026-09-17):**

- Both Builds catalog validators passed for 286 entries; the generated audit passed with 286 packages, 141 families, and one source.
- Shared consumer-authority validation passed across 12 Agents projects; the effective package-floor check resolved EventStore `3.106.0`.
- Focused `BuildContractConformanceTests` and `PackageVersionCentralizationTests` passed; see the generated release-test evidence in the Dev Agent Record.
- The complete Story 5.1 verifier passed warning-free Debug/source and Release/package builds, with 2,962 Debug/source tests and the 2,938 Release/package tests recorded below.
- Release source-policy negative probes, exact six-package validation, and isolated six-package consumer validation all passed.

**Follow-up results (2026-09-18):**

- The focused Release/package build passed with zero warnings and errors; `PackageInventoryTests` (3), `PackageVersionCentralizationTests` (4), and `BuildContractConformanceTests` (14) all passed.
- Builds central-version, authoritative-catalog, and audit validators passed; Agents consumer-authority validation passed for 12 projects; the EventStore floor resolved 13 rows at `3.106.0` against the `3.105.0` minimum.
- The complete Story 5.1 verifier again passed both builds, all 5,900 tests, source-policy negatives, exact six-package validation, and isolated package consumption.

## Dev Agent Record

<!-- dev-agent-test-evidence:start -->
### Latest Release Test Evidence

Run (UTC): 2026-09-17T21:01:40Z

| Test project | Total | Passed | Failed | Skipped | Pending | Other |
|---|---:|---:|---:|---:|---:|---:|
| Hexalith.Agents.Client.Tests | 6 | 6 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.Contracts.Tests | 529 | 529 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.Server.Tests | 543 | 543 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.Tests | 787 | 787 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.UI.Tests | 1073 | 1073 | 0 | 0 | 0 | 0 |
| **Total** | 2938 | 2938 | 0 | 0 | 0 | 0 |

Result: PASS
<!-- dev-agent-test-evidence:end -->
### File List

- `.github/workflows/ci.yml`
- `Directory.Build.props`
- `Directory.Packages.props`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-5-1-adopt-hexalith-builds-as-the-sole-package-version-authority.md`
- `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/.memlog.md`
- `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md`
- `_bmad-output/planning-artifacts/epics.md`
- `_bmad-output/planning-artifacts/launch-readiness-register.md`
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-16.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md`
- `eng/verify-story-5.2.ps1`
- `eng/verify-story.ps1`
- `references/Hexalith.Builds`
- `scripts/validate-consumer-package-references.py`
- `scripts/validate-nuget-packages.py`
- `test/Hexalith.Agents.Server.Tests/BuildContractConformanceTests.cs`
- `test/Hexalith.Agents.Server.Tests/PackageInventoryTests.cs`
- `test/Hexalith.Agents.Server.Tests/PackageVersionCentralizationTests.cs`
