# Verified-Current Review — Architecture Spine (Hexalith Agents)

**Date:** 2026-09-09
**Target:** `ARCHITECTURE-SPINE.md` (frontmatter `updated: 2026-09-09`, uncommitted working-tree edit on `main` @ `1c40632`; memlog `(version)` line dated 2026-09-09)
**Prior review:** `review-2026-09-08-verified-current.md` (regression baseline)
**Lens:** "Verify every committed decision was web-researched or reality-checked rather than asserted from training data: current library/framework versions, that each named technology still exists and fits, and — greenfield — the live defaults of any starter it leans on. Flag anything that could be out of date and wasn't confirmed against the web, the existing project, or the current starter."
**Method:** every value below was read today from the checked-in files, the submodule checkouts at their parent gitlinks, the `../platform` checkout at the `EXT-HOST-1` target commit, NuGet's flat-container / nuspec / search endpoints, the .NET release-metadata feed, the GitHub releases API, and the named docs pages. Nothing is quoted from model memory.

## Verdict

**PASS WITH FINDINGS.** No architecture decision (AD-1..AD-31) is invalidated. Every named technology still exists at the version the spine states; Dapr Workflow is still the stable durable engine and its 1.18 instance-id rule is quoted correctly; Microsoft Agent Framework's durable story is still Durable-Task/Azure-Functions-based with no Dapr host, so the AD-18 exclusion list is accurate; Dapr Agents is still Python-only and the Conversation API is still alpha; all seven frontmatter URLs resolve and say what the spine relies on; the sibling contract facts (Conversations, EventStore `system` tenant, FrontComposer policy-gated nav) hold at the parent gitlinks.

All 13 findings from the 2026-09-08 review are closed in this re-distill (V-1..V-13 — see Regression section). What remains is narrower: one Stack rationale that is asserted rather than evidenced and is contradicted by the package metadata (H-1); the AD-22 key hierarchy leaning on an encryption engine the EventStore checkout explicitly says it does not ship and no external record owns (M-1); the live defaults of the `EXT-HOST-1` starter, which is an empty file-based AppHost on older pins than anything the spine names (M-2); and the Structural Seed still disagreeing with the tree and its own conformance test (M-3).

Counts: critical 0, high 1, medium 3, low 8 (of which 5 informational / verified-with-note).

## Findings

### Critical

None.

### High

#### H-1 — xUnit/NSubstitute deviation rationale is asserted, not evidenced, and contradicted by the package metadata
- **Section:** Stack rows `xUnit v3` and `NSubstitute`; memlog `(decision) V-3, V-4, V-5, V-7, V-10 closed`
- **Spine claim:** `xunit.v3 3.2.2 root override; catalog 4.0.0; accepted deviation for bunit/FrontComposer.Testing alignment until Story 5.6 aligns [ASSUMPTION]`; NSubstitute `5.3.0 root override; catalog 6.2.0; same deviation as xUnit`.
- **Verified current value:**
  - `bunit 2.9.0` nuspec (`api.nuget.org/v3-flatcontainer/bunit/2.9.0/bunit.nuspec`) declares **no** dependency on any `xunit*` or `NSubstitute` package (net10.0 group: AngleSharp 1.7.0, AngleSharp.Css, AngleSharp.Diffing, Microsoft.AspNetCore.Components 10.0.10, Extensions.*). bunit 2.10.3 (NuGet latest) likewise. bunit therefore imposes no xunit version.
  - `references/Hexalith.FrontComposer/src/Hexalith.FrontComposer.Testing/Hexalith.FrontComposer.Testing.csproj` references only `bunit`, `Fluxor.Blazor.Web`, `Microsoft.FluentUI.AspNetCore.Components` plus two project references — **no xunit dependency**. FrontComposer has no root `Directory.Packages.props` override, so its own test projects build against the catalog `xunit.v3 4.0.0`. "Alignment with FrontComposer.Testing" argues for 4.0.0, not 3.2.2.
  - `git log -S` shows the `3.2.2` pin entered `Directory.Packages.props` in commit `84f6156` (2026-06-26) — seven weeks before `xunit.v3 4.0.0` was published (xunit.net release notes 2026-08-14; GitHub tag `v3-4.0.0` 2026-08-15). The pin is the value that was current when written, not a considered deviation.
  - The real cost of moving to 4.0.0 is the one the prior review named: 4.0.0 drops MTP v1 and requires `global.json` `"test": {"runner": "Microsoft.Testing.Platform"}` on .NET 10 SDK. All seven sibling `global.json` files carry that key today (EventStore, Parties, Tenants, FrontComposer, Builds; Conversations and Commons pin 10.0.400 without it); Agents' `global.json` does not.
- **Impact:** The Stack table now records a rationale that nothing in the repository or package graph supports. A Story 5.6 author following it would look for a bunit/FrontComposer constraint that does not exist and might keep 3.2.2 for the wrong reason, while the actual blocker (MTP v2 runner setting + SDK band) is unstated. AD-17's test-gate obligations are unaffected in rule terms.
- **Disposition:** autofix wording — "`3.2.2` root override, pinned 2026-06-26 before 4.0.0 existed; catalog `4.0.0`; moving requires the MTP v2 `global.json` test-runner setting the siblings already carry [ASSUMPTION: Story 5.6]". Same for NSubstitute (`5.3.0` pinned pre-6.x; no bunit/FrontComposer constraint).

### Medium

#### M-1 — AD-22 / AD-14 key hierarchy relies on an encryption engine EventStore states it does not ship, and no external record owns it
- **Section:** AD-14 ("EventStore payload protection under the AD-22 key hierarchy"), AD-22 ("per-`AgentInteraction` DEK wrapped by a per-tenant KEK custodied through `EXT-SECRETS-1`; no deployment-wide key protects content"), AD-27 (`ProtectedContentReference` resolvable only through the EventStore payload-protection store)
- **Spine claim:** protected payloads use a per-interaction DEK under a per-tenant KEK; cryptographic erasure destroys the DEK; snapshots and replay caches inherit the DEK.
- **Verified current value (EventStore @ `1b6f08d4`, 2026-09-08):**
  - `Hexalith.EventStore.Server/Configuration/ServiceCollectionExtensions.cs:69` registers `NoOpEventPayloadProtectionService` as the default `IEventPayloadProtectionService`; the only other implementation is the Testing `FakeUnreadableProtectionService`.
  - `docs/guides/payload-protection-and-crypto-shredding.md` "Delivery boundary": Stories 22.7a-d "did **not** deliver a real encryption engine, `pdenc-v2`, personal-data policy seams, reusable key storage/wrapping/rotation mechanics, KMS/HSM/secret-store integration, or a production backend. Those capabilities remain unavailable until post-MVP Epic 8 … Without explicit engine registration, the no-op provider remains the default."
  - `Hexalith.EventStore.DomainService/EventStoreDataProtectionServiceCollectionExtensions.cs:72` wires ASP.NET Core Data Protection with `SetApplicationName(applicationName)` and a key ring persisted to a Dapr state store (`EventStoreDataProtectionOptions`: `PersistToStateStore`, `StateStoreName`, `StateKey`, `OperationTimeout`) — an application-wide key ring, i.e. exactly the "deployment-wide key" AD-22 forbids for content; it is for cursor/host purposes, not payloads.
  - What *does* fit: `EventStorePayloadProtectionMetadata` carries a per-envelope `KeyAlias`/`Scheme`; `CryptoShreddingWorkflowScope` includes `Tenant`, `Domain`, `Aggregate`, `Stream`, `Range`; `UnreadableProtectedDataReason.KeyInvalidatedOrDeleted` exists. So a DEK-per-`AgentInteraction` aggregate is expressible through the hooks.
  - `external-dependency-register.md` `EXT-SECRETS-1` `RequiredArtifact`: "Secret resolution, rotation, denial, and leak-evidence contract for Provider and export/deletion operations" — secret custody, not payload encryption, DEK generation, or KEK wrapping. No register record names an encryption engine.
- **Impact:** The decision is structurally consistent with EventStore's hooks, and AD-14's fail-closed clause ("if protection is unavailable, content-bearing workflows stay disabled") keeps it safe. But production-like enablement, AD-22 erasure, and AD-27 references all depend on an engine that (a) EventStore says is post-MVP Epic 8, (b) `EXT-SECRETS-1` does not cover, and (c) the spine does not assign to Agents. The spine reads as if the hierarchy exists.
- **Disposition:** discuss — name the owner (EventStore Epic 8 engine vs an Agents-owned `IEventPayloadProtectionService` adapter) and either widen `EXT-SECRETS-1` to include DEK/KEK custody and wrapping or add a register record; add one clause to AD-14 stating that EventStore ships hooks and a no-op provider today.

#### M-2 — Live defaults of the `EXT-HOST-1` starter are not recorded and are behind everything the spine names
- **Section:** Stack row `Hosting (Aspire, Dapr hosting integrations)`; AD-16; memlog "Story 5.6 aligns … SDK 10.0.400 when it consumes EXT-HOST-1"
- **Spine claim:** hosting is "platform-owned through `EXT-HOST-1` (`Hexalith.Platform@a66cdf34`); not pinned by Agents"; AD-16 says the platform host "composes that service and UI with EventStore, Conversations, Parties, Tenants, Provider adapters, the Content Safety adapter, the readiness registry, capacity admission, browser evidence ingestion, and Dapr Workflow".
- **Verified current value (`../platform` @ `a66cdf3`, the register's `TargetVersionOrCommit`):**
  - `apphost.cs` is a file-based AppHost: `#:sdk Aspire.AppHost.Sdk@13.4.6` and a body of `var builder = DistributedApplication.CreateBuilder(args); builder.Build().Run();` — no Dapr, no `CommunityToolkit.Aspire.Hosting.Dapr`, no EventStore/Agents/Conversations resources.
  - `global.json` pins SDK `10.0.302` (`latestPatch`) — below the sibling baseline `10.0.400` and the Agents target the memlog names for Story 5.6.
  - `eng/verify-agents-host.sh` proves only that the AppHost builds in Release and that no Agents hosting folders exist; it prints "live Agents DomainService/UI composition remains Story 5.6 work".
  - Workspace catalog (`Hexalith.Builds@a32cb422`) pins `Aspire.Hosting 13.5.3`; NuGet/GitHub latest is `13.5.3` (2026-08-25). The starter is two minor releases behind (13.4.6, 2026-06-20).
  - Register status is `Committed`, not `Available` (register L92, L167) — the spine correctly defers status to the register.
- **Impact:** The spine names the starter as the hosting authority but records none of its live defaults, and AD-16 describes composition in the present tense that the starter does not yet perform. Story 5.6's stated alignment target (10.0.400) is not the platform host's pin (10.0.302), so "aligning with EXT-HOST-1" and "aligning with the catalog" are different actions. No AD rule changes.
- **Disposition:** autofix — annotate the Hosting row: "scaffold at `a66cdf34`: file-based AppHost, `Aspire.AppHost.Sdk 13.4.6`, SDK `10.0.302`, no Dapr composition yet; register status read at evaluation time"; discuss whether Story 5.6 aligns Agents to the platform pin or the platform to the catalog.

#### M-3 — Structural Seed disagrees with the checked-in tree and with `StructuralSeedConformanceTests`
- **Section:** Structural Seed; memlog "R-5, R-6, B-10, V-13 closed … Server/Aggregates and Application/Tools are dropped … StructuralSeedConformanceTests must stop requiring it"
- **Spine claim:** `Hexalith.Agents.Server` contains `Api/ Application/{Agents,AgentInteractions,Queries,Workflows,Activities} Composition/ Ports/ Projections/` (no `Aggregates/`, no `Application/Tools/`); `Hexalith.Agents/` contains eleven aggregate folders.
- **Verified current value (working tree @ `1c40632`):**
  - `src/Hexalith.Agents.Server/Aggregates/README.md` exists ("structural-seed placeholder — unused … can be removed"); `src/Hexalith.Agents.Server/Application/Tools/.gitkeep` exists.
  - `test/Hexalith.Agents.Server.Tests/StructuralSeedConformanceTests.cs` L42-48 still requires `"Aggregates"`, `"Application/Tools"` among the Server folders, so removing them to match the seed would fail the shipped test.
  - `src/Hexalith.Agents/` holds `Agent/`, `AgentInteraction/`, `ProviderCatalog/` only (3 of the 11 seed folders); the other eight are unbuilt and the seed does not mark them as such.
  - `Hexalith.Agents.slnx` lists 6 src + 5 test projects; no `Hexalith.Agents.IntegrationTests` (the seed correctly marks it "created by Story 5.6"; `sprint-status.yaml` L137 carries the matching action item; Story 5.6 is `backlog`).
- **Impact:** The seed is a target picture, which is legitimate, but it reads as the current tree and is contradicted by a passing test that the spine says must change without naming the owning story. Low risk to decisions; real risk of a dev session "fixing" the tree and breaking the test or vice versa.
- **Disposition:** autofix — mark the seed heading "target tree; shipped today: Agent, AgentInteraction, ProviderCatalog; `Server/Aggregates` and `Application/Tools` are removed by the story that updates `StructuralSeedConformanceTests` [ASSUMPTION owner]".

### Low

#### L-1 — .NET 10 SDK band moved again (informational; row accurate)
- **Spine claim:** `10.0.301` with `latestPatch`; "siblings are on `10.0.400`".
- **Verified:** `global.json` = `10.0.301`/`latestPatch` (resolves to installed `10.0.302`; installed SDKs 10.0.302, 10.0.400). `builds.dotnet.microsoft.com/…/10.0/releases.json`: latest runtime `10.0.12`, latest SDK **`10.0.401`** (2026-09-08, yesterday); 3xx band latest `10.0.303` (2026-08-11). All seven sibling `global.json` files = `10.0.400` (FrontComposer moved from 10.0.302 since the 2026-09-08 review). Platform starter = `10.0.302` (M-2).
- **Impact:** none; `latestPatch` from 10.0.400 reaches 10.0.401 automatically. Note that 10.0.303 exists and is not installed, so the Agents pin resolves one patch behind its own band.
- **Disposition:** ignore (or fold into M-2's alignment discussion).

#### L-2 — Dapr instance-id rule verified; docs tightened on 2026-09-08 and the terminal/purged path deserves one line
- **Section:** AD-18 ("because Dapr 1.18 never re-creates a non-terminal instance id, recovery resumes or terminalizes the existing instance and never re-creates it"); AD-27 (terminal instances purged within 7 days)
- **Verified:** docs.dapr.io breaking-changes: 1.18.0 removed the reuse policy ("creating a workflow whose instance ID belongs to a running workflow always fails with a conflict"); 1.18.2 "requires the whole workflow tree to be terminal". `workflow-features-concepts` and `howto-manage-workflow` (v1.18 latest) were amended by dapr/docs PR #5306 "Workflow: warn to not reuse instance IDs", merged into `v1.18` on 2026-09-08 — the day before the re-distill. They now state: "Once the entire workflow tree is terminal, creating a new instance with the same ID starts a fresh execution that replaces the previous one" and recommend against reuse (audit-trail loss, misrouted events). Dapr runtime latest stable `v1.18.3` (2026-08-14), `v1.18.4-rc.4` (2026-09-08); .NET SDK stable `1.18.5` (2026-07-25), `1.19.0-preview.2` (`Dapr.Actors.Next` rewrite only).
- **Impact:** AD-18's sentence is accurate for non-terminal instances. After AD-27 purge (or once terminal) the same `AgentInteractionId` *can* be scheduled again and would replace history; the spine relies on AD-29 API idempotency and the aggregate's terminal state to prevent a second start but does not say so at the workflow boundary.
- **Disposition:** defer — one clause in AD-18 or IMPLEMENTATION-CONVENTIONS for Story 5.7: "a terminal or purged instance id is never re-scheduled; the aggregate's terminal state, not the workflow engine, is the guard".

#### L-3 — bunit row wording implies a deviation where there is none
- **Spine claim:** `bunit 2.9.0 root pin`.
- **Verified:** root `Directory.Packages.props` pins `2.9.0`; the imported catalog also pins `2.9.0` (Builds L317, GHSA-pgww-w46g-26qg fix comment); NuGet latest `2.10.3`.
- **Disposition:** autofix — "`2.9.0` root pin matching the catalog".

#### L-4 — Fluent UI Blazor v5 still release-candidate; MCP one RC behind (informational)
- **Verified:** root pin = catalog = NuGet latest `5.0.0-rc.5-26219.1`; no `5.0.0` stable in the flat container; GitHub releases latest `v4.14.4` (2026-07-29). The Fluent UI Blazor MCP server installed here reports `5.0.0.26180` (rc.4 build). AD-25 "Fluent UI Blazor V5" remains an RC dependency; the spine does not claim GA.
- **Disposition:** ignore; UI story authors should know the MCP/library skew.

#### L-5 — Microsoft Agent Framework and its durable hosting verified (informational)
- **Verified:** `Microsoft.Agents.AI` and `.Workflows` `1.20.0` stable (NuGet, 2026-08-31; GitHub `dotnet-1.20.0`); `.Abstractions ≥ 1.20.0`, `Microsoft.Extensions.AI ≥ 10.9.0`; no Dapr dependency listed. `Microsoft.Agents.AI.DurableTask` and `Microsoft.Agents.AI.Hosting.AzureFunctions` exist only as prerelease (latest `1.16.0-preview.260730.1`). `Microsoft.Agents.AI.Hosting.Dapr` → 404; NuGet search for `Microsoft.Agents.AI.Hosting` returns nothing Dapr-related. learn.microsoft.com durable-extension page (updated 2026-08-31) builds on Durable Task / Durable Task Scheduler / Azure Functions / bring-your-own compute and never mentions Dapr. Overview page updated 2026-07-29 (.NET, Python, Go preview). The spine's AD-18 exclusion list ("Durable Task and Azure Functions hosting") names real packages. No `Microsoft.Agents.*` reference in any Agents `.csproj`.
- **Note:** NuGet search surfaced a third-party `Diagrid.AI.Microsoft.AgentFramework 1.1.1`; not inspected, not referenced, not named by the spine.
- **Disposition:** ignore.

#### L-6 — Dapr Agents (Python-only) and Conversation API (alpha) verified (informational)
- **Verified:** docs.dapr.io/developing-ai/dapr-agents describes "a Python framework for building LLM-powered autonomous agentic applications", v1.0 GA; dapr/dapr-agents latest `v1.0.5` (2026-06-15); NuGet search `Dapr.Agents` returns no Dapr-published .NET package. conversation-overview (v1.18 latest): "The conversation API is currently in alpha". MCPServer page: "not the default MCP integration in Dapr", workflow-per-tool. Deferred rows and AD-19 remain current.
- **Disposition:** ignore.

#### L-7 — Frontmatter URLs and local sources (informational)
- **Verified:** all seven URLs resolve and support the claims the spine hangs on them (see table B). `…/dotnet-workflow/` last-modified note now reads "Merge pull request #5306 … workflow-instance-id-reuse" (2026-09-08) — the cited page changed the day before the re-distill and still supports AD-18. All 18 local relative `sources` and the `IMPLEMENTATION-CONVENTIONS.md` companion exist.
- **Disposition:** ignore.

#### L-8 — Test-runner setting is the concrete Story 5.6 alignment item the spine leaves implicit
- **Verified:** sibling `global.json` files (EventStore, Parties, Tenants, FrontComposer, Builds) carry `"test": {"runner": "Microsoft.Testing.Platform"}`; Agents' does not; `test/Directory.Build.props` still references `Microsoft.NET.Test.Sdk` + `xunit.runner.visualstudio` (VSTest path). xunit.net 4.0.0 notes: "Testing with VSTest target is no longer supported by Microsoft.Testing.Platform on .NET 10 SDK and later — update `global.json`".
- **Disposition:** autofix — fold into H-1's corrected rationale.

## Regression against review-2026-09-08-verified-current

| Prior finding | Status today | Evidence |
| --- | --- | --- |
| V-1 "all seven records Uncommitted" | closed | prerequisites section defers status to the register; register L92 `EXT-HOST-1 = Committed` |
| V-2 removed hosting projects described as present | closed | AD-16 rule and seed carry no dated gap note; `.slnx` has no AppHost/Aspire/ServiceDefaults |
| V-3 Aspire row cites a non-existent basis | closed | Aspire and CommunityToolkit rows dropped; hosting row points to `EXT-HOST-1` (see M-2 for what that starter actually pins) |
| V-4 xUnit wording conceals a downgrade | closed as stated, reopened as H-1 | deviation now explicit, but its rationale is unevidenced |
| V-5 SDK behind workspace | closed | row states siblings on 10.0.400 (L-1 notes 10.0.401) |
| V-6 five stale sibling SHAs | closed | rows = parent gitlinks = checkouts (`git submodule status` shows no `+`/`-`) |
| V-7 CommunityToolkit version | closed | row dropped |
| V-8 OpenTelemetry 1.17.0 | closed | `1.18.0` |
| V-9 Fluent UI rc.4 | closed | `5.0.0-rc.5-26219.1` |
| V-10 NSubstitute wording | closed as stated, reopened as H-1 | same rationale defect |
| V-11 Dapr instance-id note | closed | AD-18 carries the rule (L-2 refines the terminal/purged path) |
| V-12 MAF durable alternates unnamed | closed | AD-18 names Durable Task and Azure Functions hosting; memlog 1.20.0 |
| V-13 `IntegrationTests` named but absent | closed | seed marks it "created by Story 5.6" |
| V-19 no verification date on Stack | closed | memlog `(version)` line dated 2026-09-09; rows say "at the spine `updated` date" |

## Verification Tables

### A. Local reality (Agents `main` @ `1c40632`, working tree, 2026-09-09)

| Stack row / claim | Spine value | Verified current value | Source | Drift |
| --- | --- | --- | --- | --- |
| .NET SDK | `10.0.301` + `latestPatch`; siblings `10.0.400` | `global.json` = `10.0.301`/`latestPatch`; resolves 10.0.302; 7/7 siblings `10.0.400`; platform host `10.0.302` | `global.json`, `dotnet --list-sdks`, sibling/platform `global.json` | none (L-1, M-2) |
| Target framework | `net10.0` | `net10.0` | `Directory.Build.props` | none |
| C# | `14` | `LangVersion 14` | `Directory.Build.props` | none |
| Solution | `.slnx` | `Hexalith.Agents.slnx`, 6 src + 5 test | `.slnx` | none |
| CPM | imports Builds catalog | `ManagePackageVersionsCentrally`, transitive pinning, import guarded by `HexalithVersionsLoaded` | root `Directory.Packages.props` | none |
| Hexalith.EventStore | gitlink `1b6f08d4` | `1b6f08d4` (v3.103.0-19, 2026-09-08) = checkout | `git submodule status` | none |
| Hexalith.Conversations | `73bcee6f` | `73bcee6f` (2026-09-08) = checkout | same | none |
| Hexalith.Parties | `fa423985` | `fa423985` = checkout | same | none |
| Hexalith.Tenants | `54fc4040` | `54fc4040` = checkout | same | none |
| Hexalith.FrontComposer | `053b2008` | `053b2008` (v4.4.0-5, 2026-09-08) = checkout | same | none |
| Hosting | platform-owned, `Hexalith.Platform@a66cdf34` | commit exists in `../platform`; file-based AppHost `Aspire.AppHost.Sdk@13.4.6`, SDK 10.0.302, empty builder; verify script builds only | `../platform` | M-2 |
| Dapr .NET packages | `1.18.5` | `Dapr.Client/AspNetCore/Actors*/AI/AI.Microsoft.Extensions 1.18.5` | Builds catalog | none |
| Dapr Workflow | `1.18.5` | `Dapr.Workflow 1.18.5`; no Agents `.csproj` references it yet | catalog; csproj grep | none |
| MediatR | `14.2.0` | `14.2.0` | catalog | none |
| FluentValidation | `12.1.1` | `12.1.1` (+ DI ext 12.1.1; AspNetCore 11.3.1) | catalog | none |
| OpenTelemetry | `1.18.0` | `1.18.0` (instrumentation betas `1.18.0-beta.1`) | catalog | none |
| Fluent UI Blazor | `5.0.0-rc.5-26219.1` root = catalog | root override and catalog both `5.0.0-rc.5-26219.1` | root props; catalog | none |
| xUnit v3 | `3.2.2` root; catalog `4.0.0`; bunit/FC alignment | root `3.2.2` (+ assert/extensibility 3.2.2, runner.visualstudio 3.1.5, Test.Sdk 18.6.0); catalog `4.0.0`/18.9.0; rationale unsupported | root props; catalog; bunit nuspec; FC.Testing csproj; `git log -S` | H-1 |
| Shouldly | `4.3.0` | `4.3.0` root and catalog | same | none |
| NSubstitute | `5.3.0` root; catalog `6.2.0` | as stated; rationale unsupported | same | H-1 |
| bunit | `2.9.0` root pin | root `2.9.0` = catalog `2.9.0` | root props; catalog L317 | L-3 |
| Provider SDK / Agent Framework SDK | `Unselected` | no provider, `Microsoft.Agents.*`, or `Dapr.*` package in any `src/`/`test/` csproj; `EXT-PROVIDER-1` `Uncommitted` | csproj grep; register | none |
| EventStore reserved tenant `system` (AD-2, AD-30) | reserved id | `RestTenantSource.System` ("Use the fixed \"system\" tenant"); `AggregateIdentity.PubSubTopic` special-cases `"system"`; `NamingConventionEngine` "platform tenant \"system\""; `ClaimsTenantValidator` allows global admins "any tenant (including \"system\")" | EventStore src | none |
| EventStore payload protection (AD-14/22/27) | DEK/KEK hierarchy | hooks + metadata + shredding scopes exist; default provider is no-op; guide says no encryption engine until Epic 8 | EventStore src + guide | M-1 |
| `Hexalith.EventStore.DomainService` host (AD-16) | shared SDK host | package in catalog (3.103.0); `Hexalith.Agents.Server.csproj` references it | catalog; csproj | none |
| Conversations `ParticipantRole.Facilitator` (AD-8) | exists | `Member`, `Facilitator`, `Observer`; no owner field | `ParticipantRole.cs` | none |
| Conversations `ParticipantType.AiAgent` (AD-7) | exists | wire value `"AIAgent"` (also `Human`, `Llm`) | `ParticipantType.cs` | none |
| `IConversationClient` has no `AddParticipant` (AD-6) | true | `CreateConversationAsync`, `AppendMessageAsync`, `ReassignConversationProjectAsync`, `GetConversationAsync`, `ListConversationsAsync`; `AddParticipantCommand` exists server-side only | `IConversationClient.cs`; grep | none |
| Conversations `api/v1` prefix (convention row) | matches sibling | `MapGroup("/api/v1/conversations")`; client routes `api/v1/conversations/...` | Conversations Server/Client | none |
| FrontComposer policy-gated nav (AD-15) | policy-gated entries like Tenants | `FrontComposerNavEntry.RequiredPolicy`; `IFrontComposerNavEntryRegistry`; Tenants UI `registry.AddNavEntry(...)`; Agents UI `AgentsFrontComposerRegistration` sets `RequiredPolicy` | FrontComposer Contracts; Tenants/Agents UI | none |
| AD-16 guard tests | exist | `StructuralSeedConformanceTests`, `RuntimeOwnershipConformanceTests` | Server.Tests | none (M-3 on content) |
| Structural Seed | target tree | `Server/Aggregates` + `Application/Tools` present; 3/11 aggregate folders; no IntegrationTests | tree; conformance test | M-3 |
| Frontmatter local sources (18) + companion | exist | all exist | `ls` | none |
| Memlog `(version)` 2026-09-09 line | values | every value matches the files above | memlog | none |

### B. Web currency (checked 2026-09-09)

| Item | Spine / memlog value | Current value | Source | AD impact |
| --- | --- | --- | --- | --- |
| .NET 10 | `10.0.301`; siblings `10.0.400` | SDK `10.0.401`, runtime `10.0.12` (2026-09-08); 3xx band `10.0.303` | builds.dotnet.microsoft.com releases.json | none (L-1) |
| Dapr runtime | "Dapr 1.18" | stable `v1.18.3` (2026-08-14); `v1.18.4-rc.4` (2026-09-08); no 1.19 GA/RC; docs banner v1.18 latest, v1.19 preview | github.com/dapr/dapr/releases; docs.dapr.io | none |
| Dapr .NET SDK / `Dapr.Workflow` | `1.18.5` | stable `1.18.5` (2026-07-25); `1.19.0-preview.2` = `Dapr.Actors.Next` preview, no workflow change | api.nuget.org flat container; dapr/dotnet-sdk releases | none |
| Workflow instance-id reuse (AD-18) | 1.18 never re-creates non-terminal id | 1.18.0 reuse policy removed; 1.18.2 whole tree must be terminal; docs PR #5306 (2026-09-08) warns against any reuse; terminal id re-creation "starts a fresh execution that replaces the previous one" | docs.dapr.io breaking-changes, workflow-features-concepts, howto-manage-workflow; github.com/dapr/docs/pull/5306 | none; L-2 note |
| Dapr Conversation API | alpha (Deferred) | "currently in alpha" (v1.18 docs) | docs.dapr.io conversation-overview | none |
| Dapr Agents | Python `DurableAgent`, out of V1 | "a Python framework"; v1.0 GA; latest `v1.0.5` (2026-06-15); no Dapr-published .NET package | docs.dapr.io developing-ai/dapr-agents; dapr-agents releases; NuGet search | none |
| Dapr MCPServer | non-default, workflow-centric | "not the default MCP integration"; workflow per tool; API `v1.0-beta1` | docs.dapr.io mcp-server-resource | none |
| Microsoft Agent Framework | 1.20.0 GA | `Microsoft.Agents.AI` / `.Workflows` `1.20.0` stable (2026-08-31); overview updated 2026-07-29 (.NET, Python, Go preview) | nuget.org; learn.microsoft.com; microsoft/agent-framework releases | none |
| MAF durable hosting (AD-18 exclusions) | Durable Task + Azure Functions excluded | `Microsoft.Agents.AI.DurableTask`, `.Hosting.AzureFunctions` prerelease only (`1.16.0-preview.260730.1`); durable-extension page (2026-08-31) = Durable Task / DTS / Functions / BYO compute, no Dapr; `.Hosting.Dapr` does not exist | NuGet flat container + search; learn.microsoft.com durable-extension | none (L-5) |
| Fluent UI Blazor v5 | rc.5 | rc.5 = NuGet latest; no 5.0.0; GitHub latest `v4.14.4` (2026-07-29); MCP documents 5.0.0.26180 | NuGet; github.com/microsoft/fluentui-blazor/releases; Fluent UI MCP | none (L-4) |
| xUnit v3 | catalog `4.0.0` | `4.0.0` (2026-08-14; MTP v2 default, VSTest unsupported on .NET 10 SDK, ordering/parallel API changes) | xunit.net/releases/v3/4.0.0; NuGet | none; H-1 rationale |
| bunit | `2.9.0` | latest `2.10.3`; neither 2.9.0 nor 2.10.3 depends on xunit | NuGet nuspec | H-1 |
| Shouldly / NSubstitute / MediatR / FluentValidation / OpenTelemetry | `4.3.0` / `6.2.0` catalog / `14.2.0` / `12.1.1` / `1.18.0` | `4.3.0` stable (5.0.0-preview.2 exists) / `6.2.0` / `14.2.0` / `12.1.1` / `1.18.0` | NuGet flat container | none |
| Aspire (context for M-2 only) | not pinned by Agents | catalog `13.5.3` = latest (2026-08-25); starter 13.4.6; `CommunityToolkit.Aspire.Hosting.Dapr` latest `13.5.1-beta.748`; `Aspire.Hosting.Dapr` still capped at 9.1.0 | NuGet; microsoft/aspire releases | none (M-2) |
| Frontmatter URLs (7) | resolve / say X | all resolve; contents consistent; dotnet-workflow page modified 2026-09-08 | WebFetch each | none (L-7) |

## Recommended dispositions (summary)

- **Autofix (no decision change):** H-1 rationale wording (+ L-8), M-2 Hosting-row annotation, M-3 seed "target vs shipped" note, L-3 bunit wording.
- **Discuss:** M-1 (owner of the DEK/KEK engine; widen `EXT-SECRETS-1` or add a record), M-2 (which pin Story 5.6 aligns to).
- **Defer:** L-2 (terminal/purged instance-id clause for Story 5.7).
- **Ignore:** L-1, L-4, L-5, L-6, L-7.
