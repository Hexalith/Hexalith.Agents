# Verified-Current Reviewer Gate — 2026-09-12 v2

**Lens:** configured BMad Architecture current-technology / repository-reality reviewer  
**Target:** `ARCHITECTURE-SPINE.md`  
**Review date:** 2026-09-12  
**Verdict:** **CHANGES REQUIRED** — the public technology baselines and lifecycle claims are generally accurate, but the Dapr risk disposition is based on a false repository premise: Agents already compiles against Dapr packages transitively through its adopted EventStore dependencies.

**Finding count:** 0 critical · 1 high · 0 medium · 1 low

## Scope And Method

I checked every committed named technology, version, existence, and fit claim in the spine against one of:

- the root manifests and solution;
- the root Git index for submodule authority, with source inspected at the exact recorded gitlink rather than at dirty submodule working-tree HEADs;
- the authoritative dependency register for deliberately unselected technology;
- current official product documentation, official release notes, or the NuGet registry.

No spine, register, source, or existing review artifact was modified. The required SDK is not installed in this environment, so no restore/build/test success is claimed. Repository dependency-graph conclusions below come from the checked-in MSBuild graph and the exact gitlink objects.

## High Finding

### VC2-H1 — Dapr is already in the Agents compile/runtime graph, contradicting the “not referenced / adoption blocked” premise

**Spine locations:**

- `ARCHITECTURE-SPINE.md:562` — Dapr packages `1.18.5`, with “no Agents project references it until Story 6.1” and adoption blocked by `ARCH-A-15`.
- `ARCHITECTURE-SPINE.md:563` — Dapr Workflow `1.18.5`, with the same disposition.
- `ARCHITECTURE-SPINE.md:860` — `ARCH-A-15` says “Agents references neither package yet.”
- Related fit assertions: `ARCHITECTURE-SPINE.md:270`, `:284`, and `:523` make the EventStore DomainService and Dapr Workflow central runtime seams.

**Verified repository reality:**

1. `src/Hexalith.Agents/Hexalith.Agents.csproj:13-18` consumes `Hexalith.EventStore.Client` in both source and package modes. At the authoritative EventStore gitlink `ce9e779a3ec24d9b9a054afd0ceaa3c05b8adaaa`, `src/Hexalith.EventStore.Client/Hexalith.EventStore.Client.csproj:19` directly references `Dapr.Client`.
2. `src/Hexalith.Agents.Server/Hexalith.Agents.Server.csproj:17-22` consumes `Hexalith.EventStore.DomainService` in both source and package modes. At the same authoritative gitlink, `src/Hexalith.EventStore.DomainService/Hexalith.EventStore.DomainService.csproj:22` directly references `Dapr.AspNetCore`.
3. The imported Builds catalog currently pins both packages, and the rest of the Dapr family, to `1.18.5` (`references/Hexalith.Builds/Props/Directory.Packages.props:139-146`). Thus a clean Debug/source graph and the intended Release/package graph both adopt Dapr before Story 6.1; only the **Dapr Workflow** package and workflow implementation remain future work.
4. Official NuGet registry entries show listed `1.18.7` packages published on 2026-09-11 for [Dapr.Client](https://api.nuget.org/v3/registration5-semver1/dapr.client/1.18.7.json), [Dapr.AspNetCore](https://api.nuget.org/v3/registration5-semver1/dapr.aspnetcore/1.18.7.json), and [Dapr.Workflow](https://api.nuget.org/v3/registration5-semver1/dapr.workflow/1.18.7.json). The upstream servicing commit is explicitly titled a transient-security fix and changes the SourceLink build dependency ([official Dapr commit](https://github.com/dapr/dotnet-sdk/commit/deb05064a4eb4530b9f479ed6e0c6e16ebcc2440)). This supports the spine's statement that `1.18.7` supersedes `1.18.5`, but not its statement that Agents has not adopted Dapr yet.

**Impact:** The architecture postpones the Dapr upgrade-or-exception decision as though it governs only a future dependency. In fact, `Dapr.Client` and `Dapr.AspNetCore` are already part of the adopted EventStore surface used by Agents. Security/build ownership and compatibility evidence can therefore be deferred past the point where the packages are actually loaded. The distinction between the already-live Dapr client/ASP.NET dependency and the not-yet-adopted Dapr Workflow package is also lost.

**Action classification:** **DISCUSS (blocking)** — Builds Maintainer + Security must decide now whether the already-consumed Dapr family moves atomically to `1.18.7` or receives the bounded exception already described by `ARCH-A-15`. Then **AUTOFIX** the spine to state that `Dapr.Client`/`Dapr.AspNetCore` are already transitive through EventStore, while `Dapr.Workflow` remains unreferenced until its implementation story. Make `ARCH-A-15` cover both the current transitive exposure and the future Workflow adoption; do not describe Story 6.1 as first Dapr adoption.

## Low Finding

### VC2-L1 — bUnit `2.9.0` is a valid pin but is no longer the current stable maintenance release

**Spine location:** `ARCHITECTURE-SPINE.md:573`.

**Evidence:** The root and imported Builds catalog both pin `bunit` `2.9.0`, and `test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj:22` consumes it, so the spine accurately reports repository reality. However, official NuGet now lists `2.10.3`, published 2026-09-08, as the current stable version. It supports `net10.0` and fixes an `InvalidOperationException` during parallel parser teardown ([official NuGet package and release notes](https://www.nuget.org/packages/bunit)). Unlike the acknowledged xUnit/NSubstitute/Test SDK/runner deviations, this lag has no owner or revisit condition in the spine.

**Impact:** Non-blocking maintenance drift; the fixed teardown race could affect UI-test reliability, but no security issue or current incompatibility with `net10.0` was found.

**Action classification:** **DEFER** — either align root and Builds to `2.10.3` with focused UI-test evidence, or record an explicit owner/revisit point alongside the Story 5.6 test-stack alignment. Do not treat this as a launch blocker by itself.

## Verified-Current Matrix

| Spine claim and line | Verification | Result |
| --- | --- | --- |
| `.NET SDK 10.0.401`, runtime `10.0.12`, C# 14, CVE scope (`:551`, `:577`, `:849`) | Root `global.json:3-4` pins `10.0.401`/`latestPatch`; official download metadata identifies SDK `10.0.401`, runtime `10.0.12`, and C# 14 and calls `10.0.12` the current security patch ([official .NET 10 download](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)). Microsoft's advisory limits CVE-2026-69522 to Windows and affected `Microsoft.DiaSymReader.Native` versions ([official advisory](https://github.com/dotnet/runtime/issues/133437)). No direct package reference exists in the repository. Official `latestPatch` semantics match the spine ([Microsoft documentation](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)). | **Confirmed.** The patched-floor wording is properly limited to the present dependency graph. |
| Local SDK unavailable (`:577`) | `dotnet --version` fails because `10.0.401` is requested while only `10.0.302` and `10.0.400` are installed. | **Confirmed limitation.** No build evidence is claimed. |
| `net10.0`, C# 14, `.slnx`, CPM (`:552-555`) | `Directory.Build.props:3-4`, `Hexalith.Agents.slnx`, and `Directory.Packages.props:2-7` match exactly. | **Confirmed.** |
| Five Hexalith gitlinks (`:556-560`) | Root index records EventStore `ce9e779a`, Conversations `73bcee6f`, Parties `fa423985`, Tenants `2fac1839`, FrontComposer `053b2008`. Dirty EventStore/Conversations working-tree HEADs were not substituted for parent authority. | **Confirmed.** |
| Hosting unselected; historical Platform hash unusable (`:561`) | `external-dependency-register.md:105-119` keeps `EXT-HOST-1` target/date/command TBD and status `Uncommitted`; `a66cdf34...` is explicitly historical. | **Confirmed.** |
| Provider and Agent Framework SDKs unselected (`:574-575`) | `external-dependency-register.md:121-133` keeps `EXT-PROVIDER-1` target and command TBD and status `Uncommitted`. | **Confirmed.** |
| Microsoft Agent Framework and Dapr Agents are GA (`:208`) | Microsoft records Agent Framework 1.0 GA ([official Microsoft announcement](https://devblogs.microsoft.com/agent-framework/microsoft-agent-framework-at-build-2026-announce/)); Dapr documentation records Dapr Agents v1.0 GA and describes it as a Python framework ([official Dapr documentation](https://docs.dapr.io/developing-ai/dapr-agents/)). | **Confirmed.** Deferring SDK selection is a dependency decision, not an ecosystem-existence gap. |
| Dapr Workflow fit and 1.18 behavior (`:284`, `:342`) | Dapr's .NET SDK documentation supports .NET 10 and Workflow ([official SDK overview](https://docs.dapr.io/developing-applications/sdks/dotnet/)); the Workflow client supports external events, suspension/resumption, and purge after terminal status ([official v1.18 workflow management](https://v1-18.docs.dapr.io/developing-applications/sdks/dotnet/dotnet-workflow/dotnet-workflow-management-methods/)). Current Dapr docs require an existing instance and all child workflows to be terminal before instance-id reuse ([official workflow management](https://docs.dapr.io/developing-applications/building-blocks/workflow/howto-manage-workflow/)). | **Fit confirmed subject to `EXT-HOST-1`.** The platform cleanup job must perform purge only after terminal status, consistent with `ARCH-A-1`. |
| MediatR `14.2.0`, absent from Agents runtime (`:564`) | Catalog pin matches. At EventStore gitlink `ce9e779a`, MediatR appears only in EventStore projects outside the Agents-referenced `Client`, `DomainService`, and `ServiceDefaults` graph. The official package documents license-key configuration ([NuGet](https://www.nuget.org/packages/MediatR)). | **Confirmed.** |
| FluentValidation `12.1.1`; OpenTelemetry `1.18.0` (`:565-566`) | Imported catalog matches, and official NuGet lists these current versions ([FluentValidation](https://www.nuget.org/packages/FluentValidation), [OpenTelemetry](https://www.nuget.org/packages/OpenTelemetry)). | **Confirmed.** |
| Fluent UI Blazor `5.0.0-rc.5-26219.1`, RC not GA (`:567`, `:853`) | Root and catalog match. Official NuGet lists RC5 as the newest v5 prerelease and `4.14.4` as the current stable line ([NuGet](https://www.nuget.org/packages/Microsoft.FluentUI.AspNetCore.Components/)). | **Confirmed; tracked prerelease risk remains open.** |
| xUnit/NSubstitute/Test SDK/VS runner deviations (`:568`, `:570-572`, `:849`) | Root pins xUnit `3.2.2`, NSubstitute `5.3.0`, Test SDK `18.6.0`, runner `3.1.5`; imported catalog pins `4.0.0`, `6.2.0`, `18.9.0`, `4.0.0`. xUnit 4 defaults to Microsoft Testing Platform v2 and directs .NET 10 users to the `global.json` runner setting ([official xUnit 4 release](https://xunit.net/releases/v3/4.0.0)). The root lacks that setting; Builds, EventStore, Parties, Tenants, and FrontComposer carry it, Conversations does not. Official NuGet now lists Test SDK `18.10.0`, but the spine already classifies the whole root/catalog mismatch as deferred test-stack alignment. | **Confirmed tracked deviation.** |
| Shouldly `4.3.0`, NSubstitute catalog `6.2.0`, xUnit catalog `4.0.0` (`:568-570`) | Official package pages match ([Shouldly](https://www.nuget.org/packages/Shouldly), [NSubstitute](https://www.nuget.org/packages/NSubstitute), [xUnit v3](https://www.nuget.org/packages/xunit.v3)). | **Confirmed.** |
| Dapr Conversation API remains alpha (`:869`) | The current API is explicitly marked Alpha and uses the `v1.0-alpha2` route ([official Dapr API reference](https://docs.dapr.io/reference/api/conversation_api/)). | **Confirmed.** |

## Gate Conclusion

The spine's .NET security floor, target framework/language, UI prerelease status, framework lifecycle assertions, unselected host/provider status, package catalog values, and internal gitlink authority are current as of this review. The gate still fails because `ARCH-A-15`'s future-adoption premise is materially false for `Dapr.Client` and `Dapr.AspNetCore`. Resolve VC2-H1 and rerun this lens. VC2-L1 may be handled in the existing test-stack alignment work.
