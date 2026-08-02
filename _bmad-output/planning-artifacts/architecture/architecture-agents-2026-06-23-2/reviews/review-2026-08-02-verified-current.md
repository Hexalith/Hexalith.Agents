# Verified-Current Technology And Brownfield Review

**Review date:** 2026-08-02  
**Lens:** configured verified-current reviewer  
**Target:** `ARCHITECTURE-SPINE.md`  
**Verdict:** **PASS WITH CURRENT-WORKTREE CAVEATS.** No named package version in the Stack is invented or stale relative to the selected local catalog, and the spine does not falsely claim that a Provider SDK, Agent Framework SDK, or platform-owned host is already committed. The unresolved runtime and hosting fits are explicitly conditional. Two reproducibility nuances remain: the active SDK is the allowed `10.0.302` roll-forward rather than the `10.0.301` floor, and several source-commit rows describe checked-out submodule heads that are not yet recorded by the parent repository's gitlinks.

## Scope And Evidence Authority

This review uses current repository evidence rather than model memory or previously published version claims:

- root `global.json`, `Directory.Build.props`, `Directory.Packages.props`, and `Hexalith.Agents.slnx`;
- the actually imported `references/Hexalith.Builds/Props/Directory.Packages.props`;
- evaluated `PackageVersion` items from `Hexalith.Agents.Server.Tests.csproj`;
- checked-out submodule HEADs and the parent repository's recorded gitlinks;
- current Agents project references and source registration seams;
- current `Hexalith.Agents.AppHost`, `Hexalith.Agents.Aspire`, and `Hexalith.Agents.ServiceDefaults` projects;
- `external-dependency-register.md`, especially `EXT-HOST-1` and `EXT-PROVIDER-1`.

No web result is needed to validate these rows because the spine claims the selected local workspace versions, not the newest versions available on the market. The review therefore does not substitute web or training-data versions for the workspace catalog.

## Findings

### Medium — SDK row is an accurate floor/roll-forward policy, not the exact executing patch

The Stack says `.NET SDK 10.0.301` with `rollForward: latestPatch`. That exactly matches `global.json`, while `dotnet --version` in this workspace resolves to `10.0.302`. This is expected SDK resolution, not a stale spine claim, because the row includes the roll-forward policy. Evidence consumers must nevertheless record `10.0.302` when they mean the executable used for a particular run rather than the repository's SDK selection floor.

**Disposition:** keep the spine row. Operational evidence should capture both the `global.json` policy and the resolved `dotnet --version` value.

### Medium — local source commits are verified but not all are parent-pinned

The Stack's source rows exactly match the current checked-out submodule HEADs:

| Module | Stack/current checkout | Parent `HEAD` gitlink |
| --- | --- | --- |
| Hexalith.EventStore | `30810727` | `e92ae668` |
| Hexalith.Conversations | `331ec28e` | `331ec28e` |
| Hexalith.Parties | `3295560a` | `92ee9c1b` |
| Hexalith.Tenants | `085e5021` | `845a15e4` |
| Hexalith.FrontComposer | `62841406` | `01d589a2` |

The imported Builds catalog is likewise read from checked-out `e69891f6`, while the parent gitlink is `4132725d`. The versions cited by the Stack are also present at the parent-pinned Builds commit, so this does not create a package-version contradiction today. It does mean a clean checkout at current parent `HEAD` does not reproduce most source-commit rows.

**Disposition:** the phrase `local sibling source commit` is accurate for this requested local-workspace snapshot. Do not describe these values as the root repository's committed clean-checkout baseline until the gitlinks are intentionally updated in a separately authorized Git change.

### Verified conditional — Provider and Agent Framework SDKs remain genuinely Unselected

No Agents project or root package selection references `Microsoft.Agents.AI`, `Microsoft.Agents.AI.Workflows`, or a selected Provider SDK. The imported shared catalog contains `Microsoft.SemanticKernel 1.78.0`, but the Agents module does not reference it; a shared-catalog entry is not an Agents Provider/Agent Framework selection. Boundary tests explicitly prohibit `Microsoft.Agents` and provider/runtime SDK types from public contracts.

`EXT-PROVIDER-1` remains `Uncommitted`, with `TBD` repository, immutable target, date, and verification command. The spine's `Unselected until EXT-PROVIDER-1 is committed` rows and its adapter-local conditional fit are therefore current and fail closed.

**Disposition:** pass. Selecting or versioning either SDK before `EXT-PROVIDER-1` commits would contradict current authority.

### Verified implementation gap — Dapr Workflow and the platform-owned host are target architecture, not shipped brownfield behavior

The shared catalog pins `Dapr.Workflow 1.18.5`, and the spine's Dapr version row is current. No Agents `.csproj` presently references `Dapr.Workflow`, and the source comments/tests continue to classify live Dapr Workflow ownership as deferred. Thus the technology is catalog-selected for the target but its Agents integration fit is not yet demonstrated.

The checked-in solution still includes:

- `src/Hexalith.Agents.AppHost`, using `Aspire.AppHost.Sdk/13.4.6`;
- `src/Hexalith.Agents.Aspire`, currently an empty hosting-extension shell;
- `src/Hexalith.Agents.ServiceDefaults`, currently an empty Aspire shared-project shell.

The AppHost initializes only the shared EventStore security resource and does not compose the target production-like topology. `EXT-HOST-1` is `Uncommitted` with a `TBD` repository, target, date, and verification command. AD-16, the Structural Seed note, capability map, and external prerequisite table all explicitly classify these module-owned projects as non-conformant gaps owned by replacement Stories 5.1 and 5.6. The diagrams are therefore target diagrams, not claims about the current runnable resource graph.

**Disposition:** pass with gate retained. No platform-host or Dapr Workflow conformance claim is verified until `EXT-HOST-1`/`EXT-TOPOLOGY-1` become consumable and the replacement stories supply executable evidence.

### Verified fit — current Hexalith and UI boundary choices match source reality

- The domain project references the checked-out EventStore Client, and the Server references EventStore DomainService and calls `AddEventStoreDomainService`; the EventStore-backed domain-module fit is locally demonstrated.
- Server project references exist for Conversations Client and Parties Client/Contracts; current ports use supported public seams and preserve the documented missing Conversations membership dependency.
- Agents UI references the checked-out FrontComposer Contracts and Shell projects and directly consumes the centrally selected Fluent UI Blazor package.
- Root Central Package Management imports the checked-out Builds catalog. Evaluated items confirm the root overrides resolve to Fluent UI `5.0.0-rc.4-26180.1`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and NSubstitute `5.3.0`.

**Disposition:** pass. These are reality-checked brownfield ratifications, not inferred fits.

## Stack Verification Matrix

| Spine claim | Current local evidence | Result |
| --- | --- | --- |
| .NET SDK `10.0.301`, `latestPatch` | `global.json`; resolved CLI `10.0.302` | Verified policy; execution nuance above |
| `net10.0`, C# `14` | root `Directory.Build.props` | Verified |
| `.slnx` | `Hexalith.Agents.slnx` | Verified |
| Central Package Management | root import plus evaluated `PackageVersion` items | Verified |
| Aspire Hosting/AppHost `13.4.6` | imported catalog and current AppHost SDK declaration | Verified |
| Dapr packages/Workflow `1.18.5` | imported catalog | Verified selection; Agents runtime wiring deferred |
| CommunityToolkit Aspire Dapr `13.4.1-beta.687` | imported catalog | Verified selection |
| MediatR `14.2.0` | imported catalog | Verified catalog row; no direct-use claim |
| FluentValidation `12.1.1` | imported catalog | Verified catalog row; no direct-use claim |
| OpenTelemetry `1.17.0` | imported catalog | Verified catalog row; platform integration deferred |
| Fluent UI `5.0.0-rc.4-26180.1` | root override, evaluated item, UI reference | Verified |
| xUnit v3 `3.2.2` | root override and evaluated item | Verified |
| Shouldly `4.3.0` | root override and evaluated item | Verified |
| NSubstitute `5.3.0` | root override supersedes imported `6.0.0`; evaluated item is `5.3.0` | Verified |
| Provider SDK `Unselected` | no selected package/reference; `EXT-PROVIDER-1` uncommitted | Verified |
| Agent Framework SDK `Unselected` | no `Microsoft.Agents.*` package/reference; `EXT-PROVIDER-1` uncommitted | Verified |

## Stale Or Unverified Claim Summary

- **No stale named version was found** relative to the current selected local files.
- The exact SDK used by a run is `10.0.302`, within the recorded `10.0.301`/`latestPatch` policy.
- The source-commit rows are verified worktree state but are not yet a reproducible parent-gitlink baseline.
- Provider/Agent Framework fit is intentionally unverified and correctly blocked by `EXT-PROVIDER-1`.
- Dapr Workflow integration and platform-owned-host fit are intentionally unverified and correctly blocked by the external dependency/readiness gates and replacement Stories 5.1/5.6.

No spine or register edit is required by this lens.

## Resolution Addendum — Post-Hardening Delta Review

**Verdict:** **PASS. No new critical or high local-version, register, or implementation-convention divergence.**

- The hardened Provider contract remains consistent across AD-10 and the launch-readiness register: the public result has the same six fields, the additive reason-code enum fails closed, `Degraded` is callable only for the single non-blocking warning after every hard gate passes, and `EXT-PROVIDER-1` must be `Available`. Provider and Agent Framework SDKs remain absent from Agents package/project references and correctly `Unselected`.
- The `Committed`/`Available` distinction now agrees across both registers and the spine: `Committed` permits story/contract work, while execution of an external seam and qualification evidence require `Available` plus the exact-target compatibility command. All seven current records remain truthfully `Uncommitted`, and `RQ-1` remains NOT READY.
- AD-13's hardened prepared-attempt, budget, admission, authorization, invocation, and outcome phases do not weaken the companion convention. They are separate durable step decisions; each can still use the prescribed zero-or-one trusted command, pure aggregate guard/policy seam, deterministic identity, and EventStore-authoritative concurrency/idempotency. Dapr Workflow and the shared allocator remain execution/admission owners only, not alternate business-state mutation paths.
- AD-17's EventStore-serialized readiness observations, deterministic observation identity, revision supersession, one-checkpoint evaluation, gate matrix, and explicit projections are consistent with the same command convention and with the register's sole-writer rules; producers do not write projection state directly.
- Stack values, resolved SDK behavior (`10.0.302` under the `10.0.301` `latestPatch` policy), checked-out module commits, root package overrides, platform-host implementation gap, and previously recorded parent-gitlink caveat are unchanged.

The hardening introduced no critical/high correction for this lens. The earlier current-worktree caveats remain the complete version/reproducibility tail.
