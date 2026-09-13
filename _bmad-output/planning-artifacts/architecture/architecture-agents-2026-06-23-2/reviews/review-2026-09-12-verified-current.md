# Verified-Current Reviewer Gate — 2026-09-12

**Verdict:** **CHANGES REQUIRED** — the selected public technology baselines are real and mostly current, but the spine is not safe to hand off while its local dependency provenance and operation-matrix version contradict the current repository/register state.

**Finding count:** 0 critical · 3 high · 4 medium · 1 low

## Scope And Method

Reviewed `ARCHITECTURE-SPINE.md` against the root manifests, root gitlinks, the current tracked source at those gitlinks, `external-dependency-register.md`, `launch-readiness-register.md`, current UX/epics consumers, and official primary release sources. No deliverable was edited by this reviewer.

Current upstream checks that passed:

- Root `.NET SDK` `10.0.401` is the SDK in the official .NET `10.0.12` servicing release, and the release carries the cited security update: [official .NET 10.0.12 release](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md), [official CVE-2026-69522 advisory](https://github.com/dotnet/runtime/issues/133437).
- Fluent UI Blazor `5.0.0-rc.5-26219.1` remains the latest v5 prerelease while `4.14.4` is the current stable line, so `ARCH-A-8` correctly tracks rather than hides the production prerelease risk: [official NuGet package](https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components/).
- Dapr Agents is now v1.0 GA and the Dapr Conversation API remains alpha, matching the spine's two lifecycle claims: [official Dapr Agents documentation](https://docs.dapr.io/developing-ai/dapr-agents/), [official Conversation API reference](https://docs.dapr.io/reference/api/conversation_api/).
- Local manifest claims for `net10.0`, C# 14, Central Package Management, Dapr `1.18.5`, MediatR `14.2.0`, FluentValidation `12.1.1`, OpenTelemetry `1.18.0`, Fluent UI RC5, xUnit `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0`, and bUnit `2.9.0` match the files from which the spine says they are read.

## High Findings

### VC-H1 — Two “parent gitlink at updated date” pins are false

**Evidence:** The Stack table names `Hexalith.EventStore@0994c378` and `Hexalith.Tenants@54fc4040` at the spine's `updated: 2026-09-12` date (`ARCHITECTURE-SPINE.md:536,539`). The root index currently records `Hexalith.EventStore@ce9e779a3ec24d9b9a054afd0ceaa3c05b8adaaa` and `Hexalith.Tenants@2fac18396ff11a4459de053b3ebb7ddfe7c13e30` (`git ls-files --stage ...`). The EventStore working tree is separately at `6b0247ac...` as an unstaged user-owned submodule change; that does not make either the spine pin or the parent gitlink correct.

**Impact:** A developer following the build substrate cannot reproduce the exact sibling API/source surface the architecture claims to have verified. The “re-pulled fresh” wording is especially misleading because it describes an earlier update state as current.

**Actionable fix:** At finalization, resolve each value from the root gitlink, not the submodule working-tree HEAD, and update the two rows to `ce9e779a...` and `2fac1839...`. Do not absorb or reset the unrelated EventStore working-tree change. Prefer wording such as “root gitlink at finalization” and re-run the source/seam check against those exact commits.

### VC-H2 — The Stack table promotes a historical host target to a current one

**Evidence:** The Stack row says hosting is platform-owned through `EXT-HOST-1 (Hexalith.Platform@a66cdf34)` (`ARCHITECTURE-SPINE.md:541`). The authority the row cites has `TargetVersionOrCommit = TBD`, `AcceptedStatus = Uncommitted`, and explicitly says `a66cdf34...` is a “Historical prior commitment only” that is not a current commitment field (`external-dependency-register.md:103-117`). The register's closing status says all dependency records remain Uncommitted.

**Impact:** The parenthetical can be consumed as an approved deployment target even though the current FR-34 protection/attestation contract has never been accepted for that commit. That bypasses the fail-closed dependency rule the spine otherwise enforces.

**Actionable fix:** Remove the hash from the Stack row or label it unambiguously as historical and unusable. Render the current target as `Unselected/TBD until EXT-HOST-1 is Committed`; keep hosting package versions platform-owned.

### VC-H3 — `OperationGateMatrixVersion = 3` is not current across its declared consumers

**Evidence:** The launch-readiness authority was advanced to version 3 and adds `DataHandlingAcceptance (v3)` (`launch-readiness-register.md:145,171`). The current UX still binds `high-impact-confirmation` and its family roster to matrix version 2 in three normative places (`EXPERIENCE.md:240,283,579`), even though its confirmation-content table contains `DataHandlingAcceptance` (`EXPERIENCE.md:307`). The implementation plan likewise requires version 2 in the baseline and Stories 5.5/5.7/8.7, and its parity inventory omits `DataHandlingAcceptance` (`epics.md:159,1495,1603,2965-2973`). The repository contains no implemented `OperationGateMatrix` yet, so these planning artifacts are the only implementation contract.

**Impact:** Two compliant units can target different “current” matrix versions; the UI/epic implementation can reject v3 or omit the new acceptance family while the register requires it. That is exactly the cross-unit divergence the versioned single-authority rule is meant to prevent.

**Actionable fix:** Propagate matrix v3 to every normative UX and epics citation, add `DataHandlingAcceptance` to the parity/evidence inventory and test names, and search the workspace for all remaining `OperationGateMatrixVersion 2` / `= 2` references before finalizing. If downstream planning is intentionally not being updated in this pass, record an explicit blocking handoff rather than describing the architecture as reconciled/current.

## Medium Findings

### VC-M1 — The MediatR licensing rationale does not match the tracked Agents dependency graph

**Evidence:** The Stack row says MediatR is “already operationally live for Agents through” the EventStore DomainService host (`ARCHITECTURE-SPINE.md:544`). At the tracked EventStore gitlink `ce9e779a...`, `Hexalith.EventStore.DomainService` references `Dapr.AspNetCore`, `Hexalith.EventStore.Client`, and `Hexalith.EventStore.ServiceDefaults`; neither it nor the current Agents projects references MediatR. MediatR is referenced by the separate `Hexalith.EventStore.Server` project, which Agents does not compile into `Hexalith.Agents.Server`. MediatR 14.2.0 does expose license-key configuration, but that does not prove it is in the Agents process: [official NuGet package/readme](https://www.nuget.org/packages/MediatR/14.2.0).

**Impact:** The spine assigns a commercial-license operational obligation to the wrong runtime boundary and can trigger unnecessary or misowned compliance work.

**Actionable fix:** Change the row to say the version is catalog-pinned but absent from the current Agents compile/runtime dependency graph. Assign any MediatR license decision to the actual service that loads it (currently the EventStore Server/platform owner), and reclassify only if a future Agents dependency graph introduces it.

### VC-M2 — The imported Dapr pin is no longer the current 1.18 servicing patch

**Evidence:** `Hexalith.Builds` and the spine pin Dapr .NET/Workflow packages to `1.18.5`. NuGet published listed `1.18.7` packages on 2026-09-11, including Dapr.Workflow: [official NuGet registration](https://api.nuget.org/v3/registration5-semver1/dapr.workflow/1.18.7.json). The package provenance points to Dapr commit `deb05064...`, whose only change updates `Microsoft.SourceLink.GitHub` in response to a transitive security vulnerability: [official Dapr commit](https://github.com/dapr/dotnet-sdk/commit/deb05064a4eb4530b9f479ed6e0c6e16ebcc2440).

**Impact:** `1.18.5` remains real and usable, but a “verified-current” close one day after a security-motivated servicing rebuild should not silently present it as the current patch. The observed change is build/supply-chain tooling rather than evidence of a Dapr runtime CVE, so this is not classified High without further owner assessment.

**Actionable fix:** Ask the `Hexalith.Builds` owner to assess and, if accepted, update the shared Dapr family atomically to `1.18.7`, then refresh the imported catalog/gitlink and Stack rows. Otherwise record the explicit accepted reason for remaining on `1.18.5` and a revisit owner/date.

### VC-M3 — The required SDK is official but unavailable in the current development environment

**Evidence:** `global.json` requires SDK `10.0.401` with `latestPatch`. `dotnet --version` from the repository fails with “A compatible .NET SDK was not found”; only `10.0.302` and `10.0.400` are installed. `latestPatch` will not select an older patch: [official `global.json` roll-forward semantics](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json). CI uses `actions/setup-dotnet` with `global-json-file`, so this is a local environment readiness gap, not proof that the public SDK is unavailable.

**Impact:** No restore/build/test command can execute in the current workspace, so the architecture's newly pinned substrate cannot be locally validated here.

**Actionable fix:** Install/provision SDK `10.0.401` in the development image and rerun the repository's focused validation. Keep the pin; do not downgrade to the vulnerable/older bundle merely to satisfy this machine.

### VC-M4 — The CVE closure wording is broader than the official advisory

**Evidence:** The Stack and `ARCH-A-4` say the SDK pin “closes the exposure regardless of what else is installed.” The official advisory scopes CVE-2026-69522 to Windows and to affected `Microsoft.DiaSymReader.Native` package versions, and separately instructs applications with an explicit package reference to update that package to `18.9.0-beta1.26405.2` or later. This repository currently has no explicit `Microsoft.DiaSymReader.Native` reference. The .NET 10.0.12 release does establish that SDK `10.0.401` is in the servicing bundle, but it does not support the unqualified “regardless of what else” statement: [official advisory](https://github.com/dotnet/runtime/issues/133437), [official release](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md).

**Impact:** A future direct vulnerable package reference could be incorrectly treated as remediated solely by the SDK pin.

**Actionable fix:** Scope the statement to the repository's current dependency graph and Windows exposure: the SDK baseline is updated and no direct affected package is present; any future direct `Microsoft.DiaSymReader.Native` reference must independently meet the patched-version floor.

## Low Finding

### VC-L1 — “siblings carry” the MTP runner is not universally true

**Evidence:** The xUnit row says sibling `global.json` files carry `test.runner = Microsoft.Testing.Platform`; `Hexalith.Conversations/global.json` does not, while Builds, EventStore, FrontComposer, Parties, and Tenants do.

**Actionable fix:** Replace “siblings carry” with “most inspected siblings carry” or name the five exact repositories.

## Gate Conclusion

Re-run this lens after the three High findings are corrected. The official lifecycle assertions for .NET 10, Fluent UI v5 RC, Dapr Agents GA, and Dapr Conversation API alpha do not themselves block finalization. The EventStore working-tree change is user-owned and must not be reset or silently adopted while correcting the root-gitlink evidence.

## Post-Fix Rerun — 2026-09-12

**Verdict:** **PASS WITH ONE NON-BLOCKING MEDIUM CORRECTION**

**Remaining finding count:** 0 critical · 0 high · 1 medium · 0 low

Resolved on reinspection:

- VC-H1: the Stack now records root gitlinks `ce9e779a` and `2fac1839` and explicitly excludes the separate dirty EventStore working-tree HEAD from architecture authority.
- VC-H2: the host row is `Unselected` until `EXT-HOST-1` is Committed and labels `a66cdf34` historical/unusable.
- VC-H3: the register, UX, and epics now consistently require matrix v3; epics includes `DataHandlingAcceptance` and v3 parity/activation evidence names.
- VC-M1: the MediatR row now says it is absent from the current Agents runtime graph and assigns compliance to the first loading service.
- VC-M2: dated `ARCH-A-15` now owns the Dapr `1.18.5` to `1.18.7` upgrade-or-exception decision with a 2026-09-30 target.
- VC-M3: the document explicitly records that SDK `10.0.401` is not installed in this review environment and makes no build-execution claim; the unresolved environment provision is evidence status, not a false architecture assertion.
- VC-L1: the xUnit row now says most inspected siblings carry the MTP runner and names Conversations as the exception.

### Remaining VC-M4 — One contradictory absolute remains in the SDK row

The new “Verified-current security scope” paragraph correctly limits CVE-2026-69522 to Windows and the present dependency graph and independently governs future `Microsoft.DiaSymReader.Native` references. However, the preceding Stack row still says, “Pinning `10.0.401` directly closes the exposure regardless of what else is installed.” That absolute contradicts the new qualification and the official advisory's direct-package remediation path.

**Required final correction:** Replace that sentence with: “For this repository's present dependency graph, which has no direct `Microsoft.DiaSymReader.Native` reference, selecting SDK `10.0.401` establishes the Windows patched floor; any future direct reference must independently meet the advisory's patched package floor.” Official source: [CVE-2026-69522 advisory](https://github.com/dotnet/runtime/issues/133437).

## Focused Post-Fix Recheck — 2026-09-12

**Verdict:** **PASS**

**Remaining finding count:** 0 critical · 0 high · 0 medium · 0 low

VC-M4 is resolved. The .NET SDK Stack row now limits the `10.0.401` patched-floor claim to the present Windows dependency graph, and the verified-current qualification explicitly requires every future direct vulnerable-component reference to meet its own patched floor. The spine also continues to state accurately that SDK `10.0.401` was unavailable in the review environment and that no build execution is claimed.

No verified-current issues remain from this review pass.

## Final Current-Tree Recheck — 2026-09-12

**Verdict:** **FAIL**

**Remaining finding count:** 0 critical · 1 high · 1 medium · 0 low

The architecture's previously corrected technology claims remain internally accurate, its root-gitlink rows still match the parent index, and the PRD/external-dependency current-status claims still match the registers. Two contradictions remain in the reconciled downstream implementation/evidence contracts.

### VC-F-H1 — The active epics stack baseline can roll implementation back across four corrected decisions

**Evidence:** The current epics implementation authority still calls SDK `10.0.301`, Aspire `13.4.6`, CommunityToolkit Aspire Dapr `13.4.1-beta.687`, Dapr/Workflow `1.18.5`, and Fluent UI `5.0.0-rc.4-26180.1` the local baseline (`epics.md:166`). In the current tree, `global.json` and the spine pin SDK `10.0.401`; root and imported catalogs plus the spine pin Fluent UI `5.0.0-rc.5-26219.1`; the imported catalog carries Aspire `13.5.3` and CommunityToolkit Aspire Dapr `13.5.0-preview.1.260825-0345`; and AD-16 deliberately leaves hosting versions `Unselected` and platform-owned until `EXT-HOST-1` commits them. Dapr `1.18.5` is only a catalog pin whose adoption is blocked by dated `ARCH-A-15` pending an atomic `1.18.7`-or-later upgrade or a Security-approved exception. The security relevance of the SDK and Dapr corrections remains supported by the [official CVE-2026-69522 advisory](https://github.com/dotnet/runtime/issues/133437), [.NET 10.0.12 release record](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md), [official Dapr 1.18.7 NuGet registration](https://api.nuget.org/v3/registration5-semver1/dapr.workflow/1.18.7.json), and [official Dapr servicing commit](https://github.com/dapr/dotnet-sdk/commit/deb05064a4eb4530b9f479ed6e0c6e16ebcc2440).

**Impact:** An implementer following the active epics artifact can restore the superseded SDK and UI RC, invent a module-local hosting baseline that the spine forbids, or adopt Dapr `1.18.5` while its explicit security disposition is still open.

**Actionable fix:** Replace the epics baseline with the current root-owned SDK/UI/test pins; remove Aspire and CommunityToolkit hosting versions in favor of `Hosting: Unselected and platform-owned until EXT-HOST-1 is Committed`; and qualify Dapr `1.18.5` as catalog-only and blocked from adoption by `ARCH-A-15` until upgrade or approved exception.

### VC-F-M1 — The authoritative UI evidence contract enumerates only seven of ten matrix-v3 lock-bearing families

**Evidence:** `launch-readiness-register.md:243` requires restrictive-viewport blocking for proposal resolution, policy publication, tenant budget update, data-handling acceptance/decline, legal hold, export, and deletion, but omits `ProviderCatalogMutation`, `AgentSetupMutation`, and `AgentActivation`. The spine, UX, and Story 8.6 consistently require all ten lock-bearing families, and `epics.md:2999` names the complete roster.

**Impact:** The register is the evidence authority, so its narrower explicit inventory can permit `LR-UI-CONFORMANCE` evidence that does not demonstrate the three omitted high-impact families even though the implementation story requires them.

**Actionable fix:** Make the register cite `OperationGateMatrixVersion = 3` and explicitly name all ten lock-bearing families, matching Story 8.6 and the UX roster; retain `InsufficientEvidence` for any omitted family, route, viewport, or localized reason.

## Post-Correction Current-Tree Recheck — 2026-09-12

**Verdict:** **FAIL**

**Remaining finding count:** 0 critical · 0 high · 1 medium · 0 low

VC-F-H1 is resolved: `epics.md:166` now selects SDK `10.0.401`, Fluent UI `5.0.0-rc.5-26219.1`, and the other current root-owned stack values; leaves Hosting and Provider/Agent Framework SDKs unselected behind their dependency commitments; accurately describes MediatR as catalog-visible but absent from the Agents runtime graph; and carries the `ARCH-A-15` block on adopting Dapr `1.18.5`.

### Remaining VC-F-M1 — The launch-readiness UI evidence correction is not present

`launch-readiness-register.md:243` still enumerates only proposal resolution, policy publication, tenant budget update, data-handling acceptance/decline, legal hold, export, and deletion for restrictive-viewport evidence. It still omits `ProviderCatalogMutation`, `AgentSetupMutation`, and `AgentActivation`, while the spine, UX, and Story 8.6 require all ten matrix-v3 lock-bearing families.

**Required correction:** Amend the NFR-13 UI Conformance Contract to cite `OperationGateMatrixVersion = 3`, name all ten lock-bearing families, and make any omitted family produce `InsufficientEvidence`.

## Final Post-Fix Recheck — 2026-09-12

**Verdict:** **PASS**

**Remaining finding count:** 0 critical · 0 high · 0 medium · 0 low

VC-F-M1 is resolved. The authoritative NFR-13 UI Conformance Contract now names all ten matrix-v3 lock-bearing families, including `ProviderCatalogMutation`, `AgentSetupMutation`, and `AgentActivation`, and preserves `InsufficientEvidence` for missing routes, locale keys, viewport evidence, or conditional skips. Its roster matches the matrix, UX, and Story 8.6.

No factual, technology-version, dependency-status, gitlink-authority, or evidence-contract contradiction remains from the verified-current review pass.

## Data-Handling Wording Recheck — 2026-09-12

**Verdict:** **PASS**

**Remaining finding count:** 0 critical · 0 high · 0 medium · 0 low

The revised PRD and UX contracts now align with AD-10, the launch-readiness Provider contract, and Stories 5.3/5.7: acceptance is mandatory before first activation; after a version change, activation and callability may continue only while a valid declared-tightening grace remains; tightening is evaluated cumulatively against the tenant's last accepted record; later unaccepted versions do not extend the deadline; and contractual-reference changes, loosening, undeclared changes, incomparable values, decline, or expiry block with `DataHandlingAcceptanceLapsed`. The UX renders a live valid grace as a warning rather than an activation blocker.

No new factual, technology-version, dependency-status, gitlink-authority, or evidence-contract contradiction was introduced.
