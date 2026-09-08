# Verified-Current Review — Architecture Spine (Hexalith Agents)

**Date:** 2026-09-08
**Target:** `ARCHITECTURE-SPINE.md` (frontmatter `updated: 2026-08-02`; last `(version)` memlog entry 2026-08-02)
**Lens:** "Verify every committed decision was web-researched or reality-checked rather than asserted from training data: current library/framework versions, that each named technology still exists and fits, and — greenfield — the live defaults of any starter it leans on. Flag anything that could be out of date and wasn't confirmed against the web, the existing project, or the current starter."
**Method:** every value below was read from the checked-in files, the submodule checkouts, NuGet's flat-container/search APIs, the .NET release metadata feed, or the named docs/release pages today. No version or status is quoted from model memory.

## Verdict

**PASS WITH FINDINGS.** No architecture decision (AD-1..AD-26) is invalidated: every named technology still exists, Dapr Workflow remains the stable durable engine, the CommunityToolkit Dapr integration remains the going Aspire choice, Microsoft Agent Framework's own durable story is Durable-Task-based (which reinforces, not weakens, AD-18), and Dapr Agents/Conversation API status still matches AD-19 and Deferred. What has drifted is the *reality-checked* layer: the spine's local pins were correct on 2026-08-02 and have not been re-verified since, while the workspace catalog moved on nine of the Stack rows, all five sibling-source commits changed, Story 5.1 removed the module-owned hosting projects the spine still describes as present, and `EXT-HOST-1` became `Committed` while the spine still says all seven records are `Uncommitted`.

Counts: critical 0, high 0, medium 5, low 8, informational/verified 6.

## Findings

### Medium

#### V-1 — External V1 Prerequisites: "All seven records are currently `Uncommitted`" is stale
- **Severity:** medium
- **Section:** External V1 Prerequisites; AD-16 rule text ("platform-owned host committed as `EXT-HOST-1`")
- **Spine claim:** "All seven records are currently `Uncommitted`; their consumers remain blocked from `ready-for-dev`."
- **Verified current value:** `external-dependency-register.md` shows `EXT-HOST-1` `AcceptedStatus = Committed` (Repository `Hexalith.Platform`, `TargetVersionOrCommit a66cdf346e521ad147f442b686f301f0f59c525c`, `TargetIntegrationDate 2026-09-30`, verify command `./eng/verify-agents-host.sh`). Commit `90761e3` (2026-08-09, "docs: commit EXT-HOST-1 platform host for story 5.1"). The target commit resolves in the local `../platform` checkout (`a66cdf3 2026-08-09 feat: scaffold platform-owned Aspire host for Agents EXT-HOST-1`) and the verify script exists there. The other six records remain `Uncommitted`.
- **Impact:** The spine understates dependency status; AD-16's "committed as `EXT-HOST-1`" is now true, and Story 5.6's consumption path is unblocked at `Committed` level (contract/package work only; `Available` still pending). No AD rule changes, but the section is the spine's summary of the authoritative register and is wrong.
- **Disposition:** autofix — replace with "Six of seven records are `Uncommitted`; `EXT-HOST-1` is `Committed` (target `Hexalith.Platform@a66cdf34`, integration date 2026-09-30) but not `Available`."

#### V-2 — AD-16 and Structural Seed "implementation gap" notes describe removed projects as still present
- **Severity:** medium
- **Section:** AD-16 italic note (2026-08-02); Structural Seed *Implementation gap* paragraph; memlog constraint of 2026-08-02
- **Spine claim:** "the current solution still contains `Hexalith.Agents.AppHost`, `Hexalith.Agents.Aspire`, and `Hexalith.Agents.ServiceDefaults`… replacement Story 5.1 owns removal."
- **Verified current value:** `Hexalith.Agents.slnx` lists six `src/` projects (Client, Contracts, Server, Testing, UI, Hexalith.Agents) and five test projects; `src/` on disk matches. `git ls-tree 0375f8d src/` (2026-08-02) confirms the three projects existed then; they are gone now. `sprint-status.yaml`: `5-1-correct-platform-hosting-boundary-and-quality-gates: done` (commit `516e7b9`, 2026-08-04). No `Aspire.AppHost.Sdk` declaration remains in the Agents repo.
- **Impact:** The spine reports a non-conformance that has been resolved; readers (and Story 5.6/5.7 authors) may re-plan removal work. The conformant seed is now the actual tree. No AD rule change.
- **Disposition:** autofix — convert both notes to "Resolved by Story 5.1 (2026-08-04)"; Story 5.6 remains the consumption/proof owner.

#### V-3 — Aspire row pins a superseded catalog version and cites a basis that no longer exists
- **Severity:** medium (low on version; medium because the cited evidence basis is gone)
- **Section:** Stack — `.NET Aspire Hosting / AppHost SDK`
- **Spine claim:** `13.4.6` "from imported workspace catalog and local AppHost SDK declarations"
- **Verified current value:** imported catalog `references/Hexalith.Builds/Props/Directory.Packages.props` pins `Aspire.Hosting` and all `Aspire.*` rows at `13.5.3` (Builds commit `59d6992`, 2026-08-28, bumped `Aspire.AppHost.Sdk` to 13.5.3). NuGet flat-container latest `Aspire.Hosting`/`Aspire.AppHost.Sdk` = `13.5.3`; microsoft/aspire releases: 13.5.3 on 2026-08-25 is latest stable, no 13.6/14 preview. There are no "local AppHost SDK declarations" in this repo since Story 5.1 removed the AppHost.
- **Impact:** Seed-only drift, but the row's second evidence source is false today. Because AD-16 makes hosting platform-owned, the Agents spine arguably should not pin Aspire at all — the platform host (`Hexalith.Platform@a66cdf34`) owns that choice.
- **Disposition:** discuss — either drop the row (platform-owned per AD-16/`EXT-HOST-1`) or restate as "`13.5.3` from imported workspace catalog (platform-owned; not referenced by Agents projects)".

#### V-4 — xUnit v3 root override now diverges from the workspace catalog (4.0.0, breaking release) without saying so
- **Severity:** medium
- **Section:** Stack — `xUnit v3`; Consistency Conventions / AD-17 test gates rely on the test stack
- **Spine claim:** `3.2.2` "from root-selected catalog"
- **Verified current value:** root `Directory.Packages.props` still `Update`s `xunit.v3`/`xunit.v3.assert`/`xunit.v3.extensibility.core` to `3.2.2` and `xunit.runner.visualstudio` to `3.1.5`; the imported Builds catalog moved to `xunit.v3 4.0.0` and `xunit.runner.visualstudio 4.0.0` (Builds `fd606d5`, 2026-08-28). NuGet latest `xunit.v3` = `4.0.0`; xunit.net release notes (2026-08-14): 4.0.0 drops Microsoft Testing Platform v1 (defaults to MTP v2.3.3), requires a `global.json` test-runner update on .NET 10+, changes test ordering/parallelization APIs. Root also pins `Microsoft.NET.Test.Sdk 18.6.0` vs catalog `18.9.0`. FrontComposer.Testing (consumed by `Hexalith.Agents.UI.Tests`) builds from the shared catalog, i.e. against 4.0.0 in package mode.
- **Impact:** Effective version `3.2.2` is accurate, but the phrase "from root-selected catalog" now conceals a deliberate downgrade against the workspace baseline and a latent transitive-pin conflict (`CentralPackageTransitivePinningEnabled=true`) for Release/package-mode builds that pull `Hexalith.FrontComposer.Testing` built on 4.0.0. AD-17's replay/idempotency test obligations are unaffected in rule terms.
- **Disposition:** discuss — decide whether Agents follows the catalog to 4.0.0 (with the `global.json`/MTP change and sibling SDK 10.0.400, see V-5) or documents the 3.2.2 pin as an explicit override with its reason.

#### V-5 — .NET SDK pin is behind the workspace and the current 3xx/4xx bands
- **Severity:** medium
- **Section:** Stack — `.NET SDK`
- **Spine claim:** `10.0.301` with `rollForward: latestPatch` from root `global.json`
- **Verified current value:** `global.json` still says `10.0.301`/`latestPatch` (accurate). Locally resolves to `10.0.302` (installed: 10.0.302, 10.0.400). .NET release metadata (`builds.dotnet.microsoft.com/.../10.0/releases.json`): latest runtime `10.0.11`, latest SDK `10.0.400`, 3xx band latest `10.0.303` (2026-08-11). Sibling `global.json`: EventStore, Conversations, Parties, Tenants, Builds = `10.0.400`; FrontComposer = `10.0.302`. Builds commits 2026-08-31 reference an EventStore "SDK-10.0.400 rebuild".
- **Impact:** The row is truthful for this repo but the workspace baseline the spine calls "consistent" has moved to the 4xx band; `latestPatch` will never reach 10.0.400 from 10.0.301, and xUnit 4.0.0/MTP v2 (V-4) expects the newer toolchain. Not an AD change; a reproducibility/consistency drift.
- **Disposition:** discuss — align `global.json` with the sibling modules (`10.0.400`) or record the reason for staying on 3xx.

### Low

#### V-6 — All five "local sibling source commit" rows are stale
- **Severity:** low (pins), with one verified consequence (none) — see V-15
- **Section:** Stack
- **Spine claim → current checkout (= parent gitlink, so reproducible today):**

| Module | Spine | Current HEAD / gitlink | Date |
| --- | --- | --- | --- |
| Hexalith.EventStore | `30810727` | `f54d6048` (v3.103.0-9) | 2026-09-08 |
| Hexalith.Conversations | `331ec28e` | `73bcee6f` (98 commits ahead) | 2026-09-08 |
| Hexalith.Parties | `3295560a` | `fa423985` (v1.1.1-57) | 2026-09-08 |
| Hexalith.Tenants | `085e5021` | `54fc4040` (v5.7.0-32) | 2026-09-08 |
| Hexalith.FrontComposer | `62841406` | `d42e8312` (v4.1.1-111) | 2026-08-22 |

- **Impact:** Seed-only, and the 2026-08-02 caveat (checkouts not parent-pinned) is now resolved in the other direction — every HEAD equals its gitlink. The contract facts the spine asserts against Conversations were re-checked at `73bcee6f` and still hold (V-15).
- **Disposition:** autofix — refresh the five rows, or replace exact SHAs with "parent gitlink at spine `updated` date" to stop the row rotting weekly.

#### V-7 — CommunityToolkit Aspire Hosting Dapr version stale; choice re-verified
- **Severity:** low
- **Section:** Stack
- **Spine claim:** `13.4.1-beta.687`
- **Verified current value:** catalog `13.5.0-preview.1.260825-0345`; NuGet latest `13.5.1-beta.748`. The official `Aspire.Hosting.Dapr` package (last `9.1.0`) carries a NuGet deprecation notice ("We will no longer be publishing new versions of this package. We recommend using…" the CommunityToolkit package); microsoft/aspire 13.5.x release notes mention no Dapr integration. CommunityToolkit v13.5.0 bumped its Dapr dependency to 1.18.5.
- **Impact:** None on ADs; CommunityToolkit remains the correct, still-maintained choice. Row is platform-owned per AD-16 anyway.
- **Disposition:** autofix version (or drop with V-3).

#### V-8 — OpenTelemetry catalog moved to 1.18.0
- **Severity:** low
- **Spine claim:** `1.17.0`
- **Verified current value:** catalog `1.18.0` for all `OpenTelemetry.*` rows (instrumentation betas at `1.18.0-beta.1`); NuGet latest `OpenTelemetry` = `1.18.0`.
- **Disposition:** autofix.

#### V-9 — Fluent UI Blazor pin moved to rc.5; still no v5 GA
- **Severity:** low
- **Section:** Stack; AD-25; UI convention ("Fluent UI V5")
- **Spine claim:** `5.0.0-rc.4-26180.1` "from root-selected catalog"
- **Verified current value:** root override and imported catalog both `5.0.0-rc.5-26219.1`; NuGet latest = `5.0.0-rc.5-26219.1`; no `5.0.0` stable published (flat container ends at rc.5; GitHub releases page shows latest stable `4.14.4`). The Fluent UI Blazor MCP server installed here documents `5.0.0.26180` (rc.4 build), i.e. the MCP is one RC behind the pinned library.
- **Impact:** "Fluent UI Blazor V5" remains a release-candidate dependency for AD-25's conformance gate; the spine does not claim GA, so no AD change. Note the MCP/library skew for UI story authors.
- **Disposition:** autofix version; keep V5 wording.

#### V-10 — NSubstitute override now diverges from catalog 6.2.0
- **Severity:** low
- **Spine claim:** `5.3.0 root-selected override` (accurate)
- **Verified current value:** root override `5.3.0` still wins; catalog `6.2.0`; NuGet latest `6.2.0` (stable).
- **Disposition:** discuss with V-4 (test stack alignment) or ignore.

#### V-11 — Dapr rows verified current; note the 1.18.x instance-ID reuse breaking change
- **Severity:** low (informational)
- **Section:** Stack `Dapr .NET packages`/`Dapr Workflow`; AD-13, AD-18, AD-23
- **Spine claim:** `1.18.5`
- **Verified current value:** catalog `1.18.5` for all `Dapr.*` incl. `Dapr.Workflow`, `Dapr.AI`, `Dapr.AI.Microsoft.Extensions`; NuGet latest stable `1.18.5`, prerelease `1.19.0-preview.2` (dapr/dotnet-sdk: 1.19 preview headline is `Dapr.Actors.Next` rewrite). Runtime (dapr/dapr releases): latest stable `v1.18.3` (2026-08-14), `v1.18.4-rc.3` (2026-09-02), no 1.19 GA/RC. docs.dapr.io banners `v1.18 (latest)` with v1.19 preview docs. Dapr Workflow: stable since 1.15; no deprecation; `Dapr.Workflow` is a single meta-package since 1.18.1 with automatic workflow/activity registration. Breaking-changes page: 1.18.0 removed the workflow instance-ID *reuse policy* (creating a workflow whose instance ID belongs to a running workflow always conflicts); 1.18.2 requires the whole workflow tree to be terminal before an instance ID can be recreated.
- **Impact:** Dapr Workflow is still the recommended durable engine; AD-18 stands. The instance-ID rule is *compatible* with AD-13's deterministic identities but Story 5.7's restart/recovery design (AD-23 "resume or reach a safe durable blocked/terminal state") must not rely on re-creating a non-terminal deterministic workflow ID.
- **Disposition:** defer — add a one-line note to AD-18 or IMPLEMENTATION-CONVENTIONS for Story 5.7.

#### V-12 — Microsoft Agent Framework: GA at 1.20.0; its durable story is Durable Task, not Dapr — AD-18 call stands and could be sharpened
- **Severity:** low
- **Section:** Design Paradigm, AD-18, Consistency Conventions (Runtime orchestration), Stack `Agent Framework SDK Unselected`
- **Spine/memlog claim:** MAF "may run inside a generation activity… owns neither orchestration nor domain state"; memlog verified 1.10.0 (2026-06-23).
- **Verified current value:** `Microsoft.Agents.AI` / `.Workflows` / `.Abstractions` / `.OpenAI` = `1.20.0` stable (2026-08-31); GA 1.0 shipped 2026-04-03; overview page updated 2026-07-29 (adds Harness Agent, Go preview). The durable extension (learn.microsoft.com `agent-framework/integrations/durable-extension`, updated 2026-08-31) is built on Durable Task infrastructure (`Microsoft.Agents.AI.DurableTask`, `Microsoft.Agents.AI.Hosting.AzureFunctions`, Durable Task Scheduler); it mentions no Dapr host. Dapr docs list "Microsoft Agent Framework" under *Agent Integrations — Durable Execution for …*, i.e. Dapr Workflow wrapping MAF agents, which is exactly the AD-18 shape. MAF releases still bump `Dapr.AI.Microsoft.Extensions` as a dependency (Conversation API as `IChatClient`). No `Microsoft.Agents.AI.Hosting.Dapr` package exists on NuGet.
- **Impact:** No change to AD-18; if anything, MAF's own DurableTask/Azure Functions hosting is a concrete "alternate workflow owner" AD-18 already excludes but does not name.
- **Disposition:** autofix wording — name `Microsoft.Agents.AI.DurableTask`/`Hosting.AzureFunctions` as excluded alternate durable owners; refresh memlog `(version)` line to 1.20.0. Stack `Unselected` rows remain correct (`EXT-PROVIDER-1` still `Uncommitted`, no `Microsoft.Agents.*` reference in any Agents `.csproj`).

#### V-13 — Structural Seed names a test project that does not exist
- **Severity:** low
- **Spine claim:** `test/Hexalith.Agents.IntegrationTests/`
- **Verified current value:** the folder and `.slnx` entry are `test/Hexalith.Agents.Tests/` (also true at the 2026-08-02 commit); no IntegrationTests project exists.
- **Disposition:** autofix.

### Informational / verified with no drift

#### V-14 — MediatR, FluentValidation, Shouldly verified
`MediatR 14.2.0` (catalog and NuGet latest), `FluentValidation 12.1.1` (catalog and NuGet latest; `FluentValidation.AspNetCore` remains `11.3.1`), `Shouldly 4.3.0` (root, catalog, latest stable; `5.0.0-preview.2` exists). Pass.

#### V-15 — Conversations contract facts re-verified at `73bcee6f`
AD-8: `ParticipantRole` exposes `Member`, `Facilitator`, `Observer` — still no owner field in `Hexalith.Conversations.Contracts` (only a `BuyerAcceptanceEvidenceOwnership` demo type). AD-6/AD-7: `IConversationClient` still exposes `GetConversationAsync` and `AppendMessageAsync` but no `AddParticipant`; `AddParticipantCommand` exists in Contracts and a server-side handler, matching `EXT-CONV-AI-1` remaining `Uncommitted`. `ParticipantType.AiAgent` wire value is `"AIAgent"`. Pass.

#### V-16 — Dapr Agents and Dapr Conversation API status vs AD-19 / Deferred
Dapr Agents: docs and dapr/dapr-agents releases confirm a Python framework, v1.0 GA (latest `v1.0.5`), `DurableAgent` workflow-based, `Agent` class deprecated; no .NET SDK. Dapr Conversation API: **alpha** in v1.18 docs (tool calling, PII obfuscation, caching, JSON-schema output). The Deferred rows ("Python DurableAgent out of V1", "Conversation API remains an evolving capability") and AD-19 are current. MCPServer resource page still says service invocation is the default MCP path and MCPServer is workflow-centric. Pass.

#### V-17 — Frontmatter `sources` URLs
All seven resolve and still say what the spine relies on: `docs.dapr.io/developing-ai/dapr-agents/` (Python, v1.0), `…/mcp/mcp-server-resource/` (workflow-centric, non-default), `learn.microsoft.com/…/agent-framework/overview/` (updated 2026-07-29; .NET/Python/Go), `nuget.org/packages/Microsoft.Agents.AI/` and `…AI.Workflows/` (1.20.0 stable), `docs.dapr.io/…/dotnet-workflow/` and `…/dotnet-ai/` (v1.18 latest; Conversation API + Microsoft.Extensions.AI how-to). The 13 local relative sources all exist. Pass; the memlog `(version)` lines quoting MAF 1.10.0 and "Dapr 1.18.4" are dated but not contradicted.

#### V-18 — Rows verified unchanged
`net10.0` and `LangVersion 14` (root `Directory.Build.props`), `.slnx`, Central Package Management with `CentralPackageTransitivePinningEnabled` and the Builds import guard (`HexalithVersionsLoaded`), single nuget.org source with signature validation (`NuGet.config`), Provider SDK / Agent Framework SDK `Unselected` (no provider or `Microsoft.Agents.*` package reference in `src/` or `test/`; `EXT-PROVIDER-1` `Uncommitted`). Pass.

#### V-19 — Verification cadence
Frontmatter `updated: 2026-08-02` and the last memlog `(version)` entry are five weeks old; the imported Builds catalog has ~20 version commits since (2026-08-28 through 2026-09-08). The spine's Stack rows are reality-checked *at a date*, which is fine for a seed, but nothing in the spine says which date. Recommend adding "as of YYYY-MM-DD" to the Stack heading so drift is visible rather than silently wrong.

## Verification Tables

### A. Local reality (repo state on 2026-09-08, branch `main` @ `d7e9cda`)

| Stack row | Spine value | Verified current value | Source | Match |
| --- | --- | --- | --- | --- |
| .NET SDK | `10.0.301`, `latestPatch` | `global.json` = `10.0.301`/`latestPatch`; resolves `10.0.302`; installed 10.0.302, 10.0.400; siblings EventStore/Conversations/Parties/Tenants/Builds = `10.0.400`, FrontComposer `10.0.302` | `global.json`, `dotnet --list-sdks`, sibling `global.json` | pin yes; workspace drifted (V-5) |
| Target framework | `net10.0` | `net10.0` | `Directory.Build.props` | yes |
| C# | `14` | `14` | `Directory.Build.props` | yes |
| Solution | `.slnx` | `Hexalith.Agents.slnx`; 6 src + 5 test projects; no AppHost/Aspire/ServiceDefaults | `.slnx`, `src/`, `test/` | yes (V-2 note) |
| CPM | `Directory.Packages.props` | CPM + transitive pinning; imports `references/Hexalith.Builds/Props/Directory.Packages.props` unless `HexalithVersionsLoaded` | root props | yes |
| Hexalith.EventStore | `30810727` | `f54d6048` (= gitlink) | `git -C references/… rev-parse`, `git ls-tree HEAD` | no (V-6) |
| Hexalith.Conversations | `331ec28e` | `73bcee6f` (= gitlink) | same | no (V-6) |
| Hexalith.Parties | `3295560a` | `fa423985` (= gitlink) | same | no (V-6) |
| Hexalith.Tenants | `085e5021` | `54fc4040` (= gitlink) | same | no (V-6) |
| Hexalith.FrontComposer | `62841406` | `d42e8312` (= gitlink) | same | no (V-6) |
| Aspire Hosting / AppHost SDK | `13.4.6` + local AppHost SDK decl. | catalog `13.5.3`; no local AppHost declaration | Builds catalog L109-121; `.slnx` | no (V-3) |
| Dapr .NET packages | `1.18.5` | `1.18.5` (Client, AspNetCore, Actors*, AI, AI.Microsoft.Extensions) | Builds catalog L139-146 | yes |
| Dapr Workflow | `1.18.5` | `1.18.5`; no Agents `.csproj` references it yet (Story 5.7 backlog) | catalog; `grep` src/test | yes |
| CommunityToolkit Aspire Hosting Dapr | `13.4.1-beta.687` | `13.5.0-preview.1.260825-0345` | catalog L136 | no (V-7) |
| MediatR | `14.2.0` | `14.2.0` | catalog L170 | yes |
| FluentValidation | `12.1.1` | `12.1.1` (+ `.DependencyInjectionExtensions 12.1.1`, `.AspNetCore 11.3.1`) | catalog L151-153 | yes |
| OpenTelemetry | `1.17.0` | `1.18.0` | catalog L266-275 | no (V-8) |
| Fluent UI Blazor | `5.0.0-rc.4-26180.1` | `5.0.0-rc.5-26219.1` (root override = catalog) | root props; catalog L226 | no (V-9) |
| xUnit v3 | `3.2.2` root-selected | root `3.2.2` (effective); catalog `4.0.0`; `xunit.runner.visualstudio` root 3.1.5 / catalog 4.0.0; `Microsoft.NET.Test.Sdk` root 18.6.0 / catalog 18.9.0 | root props; catalog L237, 318-321 | effective yes; wording misleading (V-4) |
| Shouldly | `4.3.0` | `4.3.0` root and catalog | root props; catalog L294 | yes |
| NSubstitute | `5.3.0` override | root `5.3.0`; catalog `6.2.0` | root props; catalog L259 | yes (V-10) |
| Provider SDK | `Unselected` | no provider package; `EXT-PROVIDER-1` `Uncommitted` | csproj grep; register | yes |
| Agent Framework SDK | `Unselected` | no `Microsoft.Agents.*` package; only boundary tests mention the namespace | csproj/cs grep; register | yes |
| External prerequisites | all 7 `Uncommitted` | `EXT-HOST-1` `Committed`; 6 `Uncommitted` | register L60-149 | no (V-1) |
| AD-16 / Seed gap note | AppHost/Aspire/ServiceDefaults present | removed (Story 5.1 done 2026-08-04) | `.slnx`, `git ls-tree`, sprint-status | no (V-2) |
| Seed test tree | `Hexalith.Agents.IntegrationTests` | `Hexalith.Agents.Tests` | `test/` | no (V-13) |
| Frontmatter local sources (13) | exist | all exist | `ls` | yes |

Catalog delta since the spine's verification (Builds `Props/Directory.Packages.props` at 2026-08-02 vs `a32cb422`): Aspire 13.4.6→13.5.3, CT Dapr 13.4.1-beta.687→13.5.0-preview.1, OpenTelemetry 1.17.0→1.18.0, Fluent UI rc.4→rc.5, NSubstitute 6.0.0→6.2.0, xunit.v3 3.2.2→4.0.0; Dapr.Workflow unchanged at 1.18.5.

### B. Web currency (checked 2026-09-08)

| Item | Spine / memlog value | Current value | Source | AD impact |
| --- | --- | --- | --- | --- |
| .NET 10 SDK | `10.0.301` (+latestPatch) | runtime `10.0.11`, SDK `10.0.400`, 3xx band `10.0.303` (2026-08-11) | builds.dotnet.microsoft.com `release-metadata/10.0/releases.json` | none (V-5) |
| Dapr runtime | "1.18" | latest stable `v1.18.3` (2026-08-14); `v1.18.4-rc.3` (2026-09-02); no 1.19 GA/RC; docs banner v1.18 latest, v1.19 preview | github.com/dapr/dapr/releases; docs.dapr.io | none |
| Dapr .NET SDK / Dapr.Workflow | `1.18.5` | stable `1.18.5`; prerelease `1.19.0-preview.2` (Dapr.Actors.Next) | api.nuget.org flat container; github.com/dapr/dotnet-sdk/releases | none (V-11) |
| Dapr Workflow recommendation | sole durable owner | stable since 1.15; no deprecation; 1.18.0/1.18.2 instance-ID reuse rules | docs.dapr.io workflow-overview; breaking-changes page | none; note for Story 5.7 (V-11) |
| Dapr Conversation API | "evolving" (Deferred) | **alpha** in v1.18 docs | docs.dapr.io conversation-overview | none (V-16) |
| Dapr Agents / DurableAgent | Python, v1.0 GA, out of V1 | Python only, v1.0 GA, latest `v1.0.5`; `Agent` deprecated for `DurableAgent` | docs.dapr.io developing-ai/dapr-agents; github.com/dapr/dapr-agents/releases | none (V-16) |
| Dapr MCPServer | non-default, workflow-centric | unchanged | docs.dapr.io mcp-server-resource | none |
| .NET Aspire | `13.4.6` | `13.5.3` latest stable (2026-08-25); no 13.6/14 preview | github.com/microsoft/aspire/releases; NuGet | none (V-3) |
| Aspire Dapr integration | CommunityToolkit | `Aspire.Hosting.Dapr` 9.1.0 deprecated on NuGet in favour of CommunityToolkit; CT latest `13.5.1-beta.748` | azuresearch-usnc.nuget.org; NuGet | none (V-7) |
| Fluent UI Blazor v5 | rc.4 | rc.5 (`5.0.0-rc.5-26219.1`) is latest on NuGet; no 5.0.0 GA; latest stable line 4.14.4 | NuGet flat container; github.com/microsoft/fluentui-blazor/releases; Fluent UI MCP (documents 5.0.0.26180) | none (V-9) |
| Microsoft Agent Framework | 1.10.0 (memlog 2026-06-23); "Unselected" | 1.0 GA 2026-04-03; `Microsoft.Agents.AI`/`.Workflows` `1.20.0` stable (2026-08-31); `.Anthropic 1.1.0-rc1`, `.Foundry 1.5.0`; overview updated 2026-07-29 | nuget.org; learn.microsoft.com overview; github.com/microsoft/agent-framework/releases | none (V-12) |
| MAF durable/Dapr integration | MAF only inside generation activity | MAF durable = Durable Task (`Microsoft.Agents.AI.DurableTask`, Azure Functions/Durable Task Scheduler, prerelease); no Dapr host; Dapr docs list MAF under "Durable Execution for …" agent integrations; no `Microsoft.Agents.AI.Hosting.Dapr` package | learn.microsoft.com durable-extension (2026-08-31); docs.dapr.io developing-ai; NuGet search | none; sharpen AD-18 exclusion list (V-12) |
| xUnit v3 | `3.2.2` | `4.0.0` (2026-08-14; MTP v2 default, drops MTP v1/Mono, ordering/parallel API breaks) | xunit.net/releases/v3/4.0.0; NuGet | none (V-4) |
| Shouldly | `4.3.0` | `4.3.0` stable; `5.0.0-preview.2` | NuGet | none |
| NSubstitute | `5.3.0` | `6.2.0` stable | NuGet | none (V-10) |
| MediatR | `14.2.0` | `14.2.0` | NuGet | none |
| FluentValidation | `12.1.1` | `12.1.1` | NuGet | none |
| OpenTelemetry | `1.17.0` | `1.18.0` | NuGet | none (V-8) |
| Frontmatter URLs (7) | resolve / say X | all resolve; content consistent (see V-17) | WebFetch each | none |

## Recommended dispositions (summary)

- **Autofix (seed-only, no decision change):** V-1, V-2, V-6, V-7, V-8, V-9, V-12 wording, V-13; plus refresh frontmatter `updated`, memlog `(version)` line, and add an "as of" date to the Stack heading (V-19).
- **Discuss:** V-3 (drop or re-scope the Aspire row now that hosting is platform-owned), V-4/V-5/V-10 (test-stack and SDK alignment with the workspace catalog before Story 5.7/5.18 evidence runs).
- **Defer:** V-11 (Dapr 1.18.x instance-ID reuse note for Story 5.7).
- **Ignore:** none.
