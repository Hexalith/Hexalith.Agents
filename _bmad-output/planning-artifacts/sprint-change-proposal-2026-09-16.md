---
title: Sprint Change Proposal - Adopt Hexalith.Builds As The Sole Package Version Authority
status: approved
created: 2026-09-16
updated: 2026-09-17
mode: Batch
change_scope: moderate
recommended_path: direct-adjustment
project: agents
owner: Administrator
approval_required: false
approval_status: approved
approved_by: Administrator
approved_on: 2026-09-16
execution_status: implementation-verified-awaiting-story-review
routed_to:
  - Product Owner
  - Developer
  - Hexalith.Builds Maintainer
  - Solution Architect
trigger_story: 5.2
corrective_story: 5.1
trigger_artifacts:
  - ../implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md
  - ../implementation-artifacts/deferred-work.md
  - ../implementation-artifacts/sprint-status.yaml
amends_if_approved:
  - epics.md
  - architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
  - ux-designs/ux-agents-2026-06-23/DESIGN.md
  - ../implementation-artifacts/sprint-status.yaml
  - ../implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md
  - ../implementation-artifacts/deferred-work.md
implementation_repositories:
  - Hexalith.Builds
  - Hexalith.Agents
preserves:
  - V1 product scope and all functional requirements
  - Epics 5 through 8 outcomes and ordering
  - Debug source-reference and Release package-reference dependency modes
  - the Story 5.2 EventStore package floor
  - all existing user worktree changes until they are deliberately reconciled
---

# Sprint Change Proposal: Adopt Hexalith.Builds As The Sole Package Version Authority

## 1. Issue Summary

Hexalith Agents records `Hexalith.Builds` as a root-declared submodule and imports its package catalog, but the repository still maintains package versions in its own root `Directory.Packages.props`. The current working tree makes the divergence more visible: it adds an Agents-local `HexalithEventStoreVersion` override and updates several Agents-local test-package overrides even though those packages are already governed by `references/Hexalith.Builds/Props/Directory.Packages.props`.

That is not the ecosystem convention used by Hexalith.EventStore, Hexalith.FrontComposer, and Hexalith.Tenants. Their root `Directory.Packages.props` files are import wrappers for the shared Builds catalog; they do not maintain module-specific package-version lists.

The immediate trigger is Story 5.2 in review. Its Release/package lane needs Hexalith.EventStore `3.105.0` or later, while the current Builds catalog pins `3.104.0`. An Agents-local override to `3.106.0` makes the local gate pass, but transfers version ownership to the consumer and leaves the shared catalog stale. The correction is to advance the version in Hexalith.Builds first, then consume the updated Builds gitlink from Agents with no local override.

### Trigger Evidence

| Evidence | Observed state | Consequence |
| --- | --- | --- |
| `agents/Directory.Packages.props` | Imports the Builds catalog and then declares ten local `PackageVersion Update` entries; the working tree also adds `HexalithEventStoreVersion=3.106.0` | Agents can silently diverge from every other Hexalith repository using the catalog |
| `Hexalith.Builds/Props/Directory.Packages.props` at root-recorded gitlink `aee0132` | Owns every package currently referenced by Agents, but pins EventStore `3.104.0` | Story 5.2 package mode remains below its valid `3.105.0` floor unless the shared catalog advances |
| EventStore, FrontComposer, and Tenants root package files | Import the shared Builds catalog and declare no local package versions | Establishes the expected ecosystem pattern |
| Story 5.1 | Requires central-package drift checks and clean package-mode consumption, but its tests only reject inline `.csproj` versions | A root-local package override is currently accepted by the supposedly governing conformance gate |
| Story 5.2 / DW-20 | Explicitly records the post-`3.104.0` EventStore release as a promotion blocker | Confirms that the Builds catalog, not the Agents consumer, is the correct resolution point |
| Architecture Stack | Says package management imports the Hexalith.Builds catalog, while later rows describe root overrides and an older Builds gitlink | The architecture authority contradicts the desired version-ownership model and current repository state |

### Problem Classification

This is a technical-governance defect discovered during Story 5.2 implementation and review. It is not a product requirement change, strategic pivot, failed domain approach, or MVP scope change.

## 2. Impact Analysis

### Epic Impact

| Epic | Impact | Viability |
| --- | --- | --- |
| Epic 5 — Live Governed Setup And Honest Readiness | Reopen Story 5.1 to correct the package boundary and its guard tests; keep Story 5.2 in review until the shared EventStore version floor is met | Viable with no outcome or ordering change |
| Epics 6–8 | No story changes; future packages automatically inherit the shared catalog | Unaffected |

No new epic is required. No epic becomes obsolete, and no epic resequencing is needed. Story 5.1 remains the natural owner because its accepted scope already includes the build/package boundary, central-package drift, package-mode CI, and clean-checkout evidence.

### Story Impact

- **Story 5.1:** reopen from `done` to `backlog` after approval, add explicit shared-catalog ownership criteria, and strengthen its conformance tests and verifier.
- **Story 5.2:** preserve `review`; replace the local-version workaround with a dependency on an approved Builds catalog version that meets the existing EventStore floor. DW-20 closes only after the package is published, the Builds catalog is updated, the Agents gitlink is advanced, and the package lane passes.
- **Story 5.6:** clarify that test-stack alignment means alignment with the imported Builds catalog, not an Agents-local workspace catalog.

### Artifact Conflicts

| Artifact | Required adjustment |
| --- | --- |
| PRD | None. PRD §10 explicitly leaves concrete package pins to downstream architecture/dependency governance. |
| `epics.md` | Amend Stories 5.1, 5.2, and 5.6; remove stale duplicated package-version narration from the active stack baseline. |
| `ARCHITECTURE-SPINE.md` | Make the Builds catalog the sole version authority, refresh the Builds/Dapr observations, and remove language that treats root overrides as intended alignment. |
| UX `DESIGN.md` | Keep the same Fluent package and behavior, but identify the imported Builds catalog as its owner instead of an Agents-local pin. |
| UX `EXPERIENCE.md` | No change. No route, flow, interaction, accessibility, or state behavior changes. |
| Story 5.2 spec and `deferred-work.md` | State that DW-20 is resolved through Hexalith.Builds publication/catalog adoption only; an Agents-local override is not completion evidence. |
| `sprint-status.yaml` | Reopen Story 5.1 after approval; retain Story 5.2 in `review` until both DW-20 and its other promotion blockers are resolved. |

### Technical And Delivery Impact

1. Hexalith.Builds must publish the version choice needed by Agents before Agents removes its local override. At minimum, `HexalithEventStoreVersion` must advance from `3.104.0` to the published `3.106.0` line already represented by the Agents EventStore submodule, or to a later compatible release.
2. Agents then advances only the root-declared `references/Hexalith.Builds` gitlink and converts its root `Directory.Packages.props` into an import-only wrapper with the same supported-layout resolution pattern as the reference repositories.
3. Existing package references remain versionless. No project file gains `Version` or `VersionOverride` metadata.
4. Story 5.1 guards must reject package-specific properties and all `PackageVersion` items in the Agents wrapper, not merely inline project versions.
5. The current uncommitted Agents changes are not overwritten. The Developer reconciles them deliberately after the shared catalog update, preserving unrelated CI, verifier, and `global.json` work.

## 3. Recommended Approach

Use **Direct Adjustment**, executed in repository ownership order.

| Option | Decision | Effort | Risk | Rationale |
| --- | --- | --- | --- | --- |
| Direct adjustment | Selected | Medium | Low to medium | Corrects the ownership defect without changing product scope; cross-repository ordering and current worktree reconciliation are the only material coordination costs |
| Roll back Story 5.2 | Rejected | Medium | Medium | Story 5.2 correctly exposed the missing shared release; rolling back its implementation would not repair catalog ownership |
| MVP review/reduction | Rejected | Low analysis value | High opportunity cost | No product requirement or V1 outcome is threatened |

### Safe Implementation Order

1. In the Hexalith.Builds repository, update the shared EventStore selection to a published compatible release meeting Story 5.2's `>=3.105.0` gate; validate the complete catalog and publish/merge that Builds change.
2. In Agents, advance the root-declared Builds gitlink to that approved commit. Do not initialize or update nested submodules.
3. Replace the Agents root package file with import-policy/path plumbing only. Remove every local package-version declaration and package-specific version property.
4. Update Story 5.1 conformance tests and `eng/verify-story.ps1` so local version ownership fails with a clear diagnostic and effective Release versions are proven to come from the imported catalog.
5. Reconcile Story 5.2's current uncommitted override and run its effective package-floor check, Release restore/build, project-level tests, and package-consumer lane.
6. Update the architecture, epic, UX ownership sentence, DW-20 evidence, and sprint status from the actual landed commit/version evidence.

### Effort, Risk, And Timeline

- **Planning effort:** low; the owning story and desired convention are already clear.
- **Implementation effort:** medium because work spans two repositories and requires a published package/catalog/gitlink sequence.
- **Primary risk:** temporarily breaking Release/package mode if the Agents override is removed before the shared catalog advances.
- **Secondary risk:** losing unrelated uncommitted Agents work during reconciliation. This is controlled by editing only the intended hunks and reviewing the worktree diff before and after.
- **Timeline impact:** Story 5.2 remains in review until the shared-catalog sequence completes. No later epic is replanned.
- **MVP impact:** none.

## 4. Detailed Change Proposals

### 4.1 Story 5.1 — Package Authority Acceptance Criteria

**Story:** 5.1 Establish Build Package Boundary And Basic CI Gates  
**Section:** Acceptance Criteria

**OLD**

> Given source-mode development and package-mode consumption  
> When the story verification lane runs  
> Then Debug/source use resolves only intentional local project references, while Release/package validation consumes the produced public packages without reverse references or Provider, workflow, Dapr-hosting, or UI implementation types in Contracts  
> And package inventory, public API, central package version, and solution-format checks fail on any drift.

**NEW**

> Given the root `Directory.Packages.props` is evaluated in a standalone checkout or a supported parent/submodule layout  
> When Central Package Management resolves package versions  
> Then the file contains only Central Package Management policy plus path/import plumbing for `Hexalith.Builds/Props/Directory.Packages.props`, and the effective evaluation records `HexalithVersionsLoaded=true`  
> And the Agents wrapper contains no `PackageVersion` item, package-specific version property, `VersionOverride`, or fallback package-version list.
>
> Given a package version used by Agents must change  
> When the change is implemented  
> Then the version is changed and validated in the Hexalith.Builds owning repository first, and Agents consumes the resulting root-declared Builds gitlink  
> And an Agents-local override cannot satisfy package-floor, clean-checkout, package-consumer, or CI acceptance evidence.
>
> Given source-mode development and package-mode consumption  
> When the story verification lane runs  
> Then Debug/source use resolves only intentional local project references, while Release/package validation consumes the produced public packages and every effective package version comes from the imported Builds catalog  
> And package inventory, public API, shared-catalog ownership, effective central versions, solution format, and source/package dependency mode checks fail on any drift.

**Rationale:** the existing criterion prevents inline project versions but does not prevent the consumer repository from becoming a second central catalog.

### 4.2 Story 5.1 — Root CI Negative Gate

**Story:** 5.1 Establish Build Package Boundary And Basic CI Gates  
**Section:** Acceptance Criteria

**OLD**

> When a change introduces a forbidden project/reference, inline package version, legacy solution, missing package consumer, or source-only Release dependency  
> Then a named source/package/boundary/basic test gate fails with a support-safe diagnostic.

**NEW**

> When a change introduces a forbidden project/reference, inline project version, any Agents-local `PackageVersion`, any package-specific version override, a missing/unloaded Builds catalog, a legacy solution, a missing package consumer, or a source-only Release dependency  
> Then a named source/package/boundary/basic test gate fails with a support-safe diagnostic that identifies Hexalith.Builds as the package-version owner.

**Rationale:** this turns the desired repository convention into executable protection.

### 4.3 Story 5.2 — Shared EventStore Floor

**Story:** 5.2 Configure hexa Through Live EventStore Operations  
**Section:** Dependency / promotion evidence

**OLD**

> The Story 5.2 verifier requires EventStore `3.105.0` or later; package mode consumes `3.104.0`, and the story remains red until that package is published and imported.

The current worktree attempts to satisfy the floor with an Agents-local `HexalithEventStoreVersion=3.106.0` property.

**NEW**

> The Story 5.2 verifier continues to require EventStore `3.105.0` or later. The floor is satisfied only when the published compatible EventStore release is selected by `Hexalith.Builds/Props/Directory.Packages.props`, Agents records the approved Builds gitlink, and the effective Release/package evaluation reports that shared value. An Agents-local version property or `PackageVersion` update is a failing boundary condition, not completion evidence.

**Rationale:** DW-20 is an upstream catalog/release dependency; bypassing it in the consumer defeats the purpose of the dependency record.

### 4.4 Story 5.6 — Test Stack Alignment

**Story:** 5.6 Compose Agents In The Platform-Owned Production-Like Host  
**Section:** Acceptance Criteria

**OLD**

> Then `global.json`, the workspace package catalog, xUnit v3/Microsoft Testing Platform v2 `test.runner`, Shouldly, and NSubstitute align.

**NEW**

> Then `global.json`, the imported Hexalith.Builds package catalog, xUnit v3/Microsoft Testing Platform v2 `test.runner`, Shouldly, and NSubstitute align; Agents declares no local package versions.

**Rationale:** “workspace package catalog” is ambiguous and currently permits the duplicated root list.

### 4.5 Epics — Active Stack Baseline

**Artifact:** `epics.md`  
**Section:** Additional Requirements / active stack baseline

**OLD**

> The active stack baseline is the Architecture Spine's current Stack table, followed by a duplicated list of concrete package versions and a stale Dapr `1.18.5` catalog observation.

**NEW**

> The active stack baseline is the Architecture Spine's current Stack table. The root SDK/TFM/language/solution selections remain explicit; all NuGet versions come exclusively from the root-recorded Hexalith.Builds catalog imported through `Directory.Packages.props`. Story acceptance may state a compatibility floor, but may not create an Agents-local pin. The effective catalog commit and versions are recorded as delivery evidence rather than duplicated as an independently governed epic list.

**Rationale:** the epic list currently drifts from both the shared catalog and the architecture document.

### 4.6 Architecture — Package Management Rule

**Artifact:** `ARCHITECTURE-SPINE.md`  
**Section:** Stack / Package management

**OLD**

> Central Package Management via `Directory.Packages.props` importing the `Hexalith.Builds` catalog.

**NEW**

> Central Package Management via an import-only Agents `Directory.Packages.props` that resolves and imports `Hexalith.Builds/Props/Directory.Packages.props` for supported standalone and parent/submodule layouts. Hexalith.Builds is the sole package-version authority: Agents declares no `PackageVersion`, package-specific version property, or `VersionOverride`. A story compatibility floor is verified against the effective imported value and, when unmet, is resolved in Hexalith.Builds before the Agents gitlink advances.

**Rationale:** the existing sentence does not prohibit the contradictory local override layer.

### 4.7 Architecture — Builds And Package Observations

**Artifact:** `ARCHITECTURE-SPINE.md`  
**Sections:** Stack and Architecture Assumptions

**OLD**

> Dapr rows describe the parent-authoritative Builds catalog as `a32cb422` at `1.18.5` and a modified checkout at `cf52f74` as `1.18.7`; Fluent UI is described as a root pin matching the catalog; ARCH-A-4 assigns root test-package override alignment to Story 5.6.

**NEW**

> Record the actual post-change root Builds gitlink and its effective package selections. Describe Fluent UI and the test stack as shared-catalog selections, not root pins. Move the no-local-version conformance work to reopened Story 5.1. Re-evaluate ARCH-A-15 against the current Dapr family and required compatibility evidence; retire it only if every retirement condition is proven, otherwise preserve the remaining unmet condition precisely.

**Rationale:** these rows are evidence claims and must follow the root gitlink actually adopted by the correction.

### 4.8 UX Design — Fluent Package Ownership

**Artifact:** `DESIGN.md`  
**Section:** Foundations

**OLD**

> Microsoft Fluent UI Blazor v5, pinned as `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.5-26219.1` and owned by `Directory.Packages.props`.

**NEW**

> Microsoft Fluent UI Blazor v5, selected as `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.5-26219.1` by the Hexalith.Builds catalog imported through the Agents `Directory.Packages.props`. Agents owns no local Fluent package version.

**Rationale:** visual behavior does not change; the sentence must name the actual package authority.

### 4.9 Implementation Files And Guards

The Developer handoff includes these bounded changes after approval:

- `references/Hexalith.Builds/Props/Directory.Packages.props` in the Builds repository: advance the EventStore family to the approved published compatible version; do not add an Agents-specific branch or override.
- root Builds gitlink in Agents: advance only after the Builds change is committed and available.
- `Directory.Packages.props`: retain CPM properties and shared-catalog path/import declarations only; remove the local EventStore property and all local `PackageVersion` items.
- `Directory.Build.props`: make the catalog-availability guard accept the supported resolved catalog layouts and fail on an unloaded catalog, rather than treating one fixed nested `.git` path as the package authority.
- `PackageVersionCentralizationTests.cs`: reject `PackageVersion` items, `VersionOverride`, and package-specific version properties in the Agents wrapper; prove the expected Builds import and loaded marker.
- `BuildContractConformanceTests.cs`: prove the shared catalog is required and the wrapper is import-only.
- `eng/verify-story.ps1`: add the shared-catalog ownership gate to both source and package lanes.
- `eng/verify-story-5.2.ps1`: retain the effective MSBuild-property floor check; do not parse or require an Agents-local value.
- `.github/workflows/ci.yml`: retain root-declared-only submodule initialization and run the shared-catalog ownership gate before Release restore/build.

The shared catalog currently contains every PackageReference used by Agents. No new package entry is required except an approved EventStore family advance; the Builds Maintainer decides whether any unrelated test-package update is independently warranted.

## 5. Implementation Handoff

### Scope Classification

**Moderate.** The code change is mechanically small, but it reopens a completed boundary story, changes two owning repositories in sequence, updates the root submodule pointer, and gates a story already in review.

### Recipients And Responsibilities

| Recipient | Responsibility |
| --- | --- |
| Product Owner | Approve reopening Story 5.1 and retaining Story 5.2 in review during the correction |
| Hexalith.Builds Maintainer | Select, validate, commit, and make available the compatible EventStore version in the shared catalog |
| Developer | Update the Agents gitlink/import wrapper/guards, reconcile rather than overwrite the current worktree, and run all verification lanes |
| Solution Architect | Update the Stack evidence and disposition ARCH-A-4/ARCH-A-15 from actual post-change evidence |

### Success Criteria

1. Agents `Directory.Packages.props` contains no package version or package-specific version override.
2. A standalone Agents checkout and a supported parent/submodule layout both load the root-recorded Hexalith.Builds catalog; absence produces a clear failure.
3. Every Agents `PackageReference` remains versionless and resolves from the imported shared catalog.
4. The effective Release `HexalithEventStoreVersion` meets Story 5.2's existing `>=3.105.0` floor without a command-line or Agents-local override.
5. The clean Release restore/build, project-level test suites, package inventory, isolated package consumer, source/package boundary negatives, and shared-catalog ownership negatives pass.
6. Root-declared submodules alone are initialized; no nested submodule is initialized.
7. Existing unrelated worktree changes remain present and are reviewed explicitly.
8. Architecture, epics, UX package ownership, DW-20, and sprint status match the actual catalog commit and verification evidence.

### Sprint Status Change After Approval

```yaml
  5-1-establish-build-package-boundary-and-basic-ci-gates: backlog
  5-2-configure-hexa-through-live-eventstore-operations: review
```

Story 5.1 returns to `done` only after all success criteria above pass. Story 5.2 remains `review` until DW-20 and its other recorded promotion blockers are resolved; this proposal does not mark it complete.

## Appendix A — Change Analysis Checklist

### 1. Understand The Trigger And Context

- [x] 1.1 Trigger identified: Story 5.2 review exposed the shared EventStore package floor; Story 5.1 owns the defective package boundary.
- [x] 1.2 Core problem classified as a technical-governance defect: duplicated package-version authority.
- [x] 1.3 Evidence recorded from the root wrapper, Builds catalog, three reference repositories, Story 5.1 gates, Story 5.2, DW-20, and the architecture Stack.

### 2. Epic Impact Assessment

- [x] 2.1 Epic 5 remains completable with Story 5.1 reopened.
- [x] 2.2 No new epic, epic removal, or epic redefinition is required.
- [x] 2.3 Epics 6–8 inherit the correction and require no story changes.
- [N/A] 2.4 No future epic is invalidated and no new epic is needed.
- [N/A] 2.5 Epic order and priority remain unchanged.

### 3. Artifact Conflict And Impact Analysis

- [x] 3.1 PRD checked: no conflict or MVP change; package pins are downstream concerns.
- [!] 3.2 Architecture requires the Stack and assumption evidence changes in §4.6–4.7.
- [!] 3.3 UX behavior is unaffected; `DESIGN.md` requires only the package-ownership wording in §4.8.
- [!] 3.4 CI, conformance tests, verifier, Story 5.2 spec, DW-20, submodule pointer, and sprint status require the changes listed above.

### 4. Path Forward Evaluation

- [x] 4.1 Direct adjustment is viable; effort medium, risk low to medium.
- [N/A] 4.2 Rollback is not viable because it does not repair package ownership.
- [N/A] 4.3 MVP review is unnecessary; scope and goals are unchanged.
- [x] 4.4 Direct adjustment selected for maintainability, consistent governance, and minimum schedule disruption.

### 5. Proposal Components

- [x] 5.1 Issue summary and evidence completed.
- [x] 5.2 Epic, story, artifact, and technical impacts completed.
- [x] 5.3 Recommended path and alternatives completed.
- [x] 5.4 MVP impact and ordered action plan completed.
- [x] 5.5 Moderate-scope handoff assigned to Product Owner, Developer, Builds Maintainer, and Solution Architect.

### 6. Final Review And Handoff

- [x] 6.1 Applicable checklist sections addressed; action-needed items are explicit proposal edits.
- [x] 6.2 Proposal checked for consistency with the current repository, planning artifacts, and reference repositories.
- [x] 6.3 Administrator explicitly approved the complete proposal on 2026-09-16.
- [x] 6.4 Sprint status reopens Story 5.1 as `backlog` and preserves Story 5.2 as `review`.
- [x] 6.5 Moderate-scope handoff routed to Product Owner, Developer, Hexalith.Builds Maintainer, and Solution Architect.

## Approval Gate

Administrator explicitly approved this proposal on 2026-09-16. The changes listed under `amends_if_approved` and the two-repository implementation sequence in §3 are authorized for implementation through the handoff in §5. Approval does not mark the implementation complete, close DW-20, assert that an EventStore package has been published, or authorize overwriting unrelated worktree changes.

## Workflow Execution Log

- 2026-09-16 — Correct Course activated in Batch mode for shared package-version ownership in Hexalith Agents.
- 2026-09-16 — The PRD, active epics, Architecture Spine, UX spines, project context, sprint status, Story 5.2 artifacts, deferred-work ledger, current repository state, Hexalith.Builds catalog, and EventStore/FrontComposer/Tenants reference patterns were assessed.
- 2026-09-16 — Direct adjustment was selected; rollback, a new epic, and MVP reduction were rejected.
- 2026-09-16 — The complete proposal was written and presented without changing implementation files or overwriting the existing worktree.
- 2026-09-16 — Administrator continued the complete-proposal review and explicitly approved it.
- 2026-09-16 — Story 5.1 was reopened as `backlog`; Story 5.2 remained `review`.
- 2026-09-16 — Moderate-scope handoff routed to Product Owner, Developer, Hexalith.Builds Maintainer, and Solution Architect.

### Handoff Completion

The approved handoff package is §4's exact story, architecture, UX, implementation-file, and guard changes, sequenced by §3 and bounded by §5's success criteria. The implementation evidence below closes DW-20: Builds `origin/main` contains the catalog/audit target, Agents `origin/main` records its exact gitlink, and Story 5.1 remains subject only to its workflow review.

## Implementation Evidence — 2026-09-17

- Hexalith.Builds catalog commit `dae84d5f96911517eb1e6f97e75eb88e759e6d8a` selects published EventStore `3.106.0`; generated-audit commit `000abf867abc3a99cfa74d39b6e73af05c78a602` records the corresponding catalog evidence.
- Agents `Directory.Packages.props` is import-only and carries no package-specific version property, `PackageVersion`, or fallback list. The shared consumer-authority validator covers root, sibling, parent-references, missing-catalog, local-property override, local-item override, and `VersionOverride` cases.
- `pwsh -NoProfile -File ./eng/verify-story.ps1 -Story 5.1` passes the shared-authority and EventStore-floor gates, warning-free Debug/source and Release/package builds, all project-level test suites, source-policy negatives, the exact six-package inventory, and isolated package consumption.
- DW-20 is closed because the effective Release package graph now uses EventStore `3.106.0` from Hexalith.Builds; no Agents-local override is accepted as evidence.
- Builds `origin/main` contains catalog/audit target `000abf867abc3a99cfa74d39b6e73af05c78a602`, and Agents `origin/main` records that exact gitlink. The publication condition is satisfied and ARCH-A-15 is retired.
- Story 5.1's implementation and complete verifier are green; its workflow state remains governed by `sprint-status.yaml`. Story 5.2 remains `review` for independent blockers including DW-21; resolving its EventStore package floor does not establish historical completion.
