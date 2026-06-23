# Story 1.1: Buildable Agents Module Shell And Public Boundaries

Status: ready-for-dev

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an Integration Developer,
I want a buildable `Hexalith.Agents` module shell with public contracts and project boundaries,
so that governed Agent setup can be implemented through stable Hexalith conventions without leaking infrastructure details.

## Acceptance Criteria

**AC1 — Buildable structural seed**
**Given** the agents workspace has no completed `Hexalith.Agents` module
**When** the story is implemented
**Then** the solution contains a buildable `Hexalith.Agents.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, and initial `src/` and `tests/` projects matching the architecture Structural Seed
**And** projects target `net10.0`, use Central Package Management, nullable, implicit usings, and warnings as errors.

**AC2 — Enforced dependency direction**
**Given** the module shell exists
**When** package and project references are inspected
**Then** public contract projects do not reference server infrastructure, provider SDKs, raw EventStore server internals, Dapr runtime implementation packages, or UI shell packages
**And** dependency direction follows the architecture rule: client/UI/server consume contracts, not the reverse.

**AC3 — Clean build + boundary guard tests**
**Given** a developer builds the module
**When** the narrow build command for the new Agents solution is run
**Then** it succeeds without warnings
**And** placeholder tests verify project boundaries, package-version centralization, and absence of direct provider secret/configuration leakage in public contracts.

**AC4 — Named extension points without premature entities**
**Given** future Agent setup stories will add aggregates and UI
**When** the module shell is reviewed
**Then** it exposes named extension points or folders for `Agent`, `ProviderCatalog`, `AgentInteraction`, application orchestration, ports, projections, UI, AppHost, testing, and client contracts
**And** it does not pre-create unrelated domain entities, storage tables, or all future events ahead of the story that needs them.

[Source: epics.md#Story-1.1; architecture ARCHITECTURE-SPINE.md#Structural-Seed; AD-15, AD-16, AD-17]

## Tasks / Subtasks

- [ ] **Task 1 — Create module root + build files** (AC: #1)
  - [ ] Create the module directory `Hexalith.Agents/` at the `agents` workspace root (`/home/administrator/projects/hexalith/agents/Hexalith.Agents/`). The root `agents` repo stays a coordination/super-repo [AD-16]. See **Project Structure Notes** for the submodule-vs-folder decision.
  - [ ] `global.json`: pin SDK `10.0.301`, `rollForward: latestPatch` (matches Tenants exactly; architecture permits `10.0.300`–`10.0.301`. For reference, Conversations uses `10.0.300`/`latestPatch` and Memories `10.0.301`/`latestFeature`).
  - [ ] Root `Directory.Build.props`: set `<TargetFramework>net10.0</TargetFramework>`, `<LangVersion>14</LangVersion>`, `<Nullable>enable</Nullable>`, `<ImplicitUsings>enable</ImplicitUsings>`, `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`. Add the `Hexalith*Root` sibling-resolution property blocks and a `CheckSubmodules` target (see **Build-File Templates** in Dev Notes — copy the sibling pattern exactly, adapted to the dependencies Agents actually consumes: EventStore, Conversations, Parties, Tenants, FrontComposer, Commons).
  - [ ] Root `Directory.Packages.props`: `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>` and `<CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>` (transitive pinning follows the Tenants/Parties precedent — not universal across siblings, but it strengthens the centralization guard this story tests). Add ONLY the package versions the shell + tests actually consume now (do not pre-list every future Agent/provider package — that violates AC4). Use the architecture Stack table as the authoritative version source when adding a package.
  - [ ] `NuGet.config`: copy the **Memories** supply-chain hardening config (signature validation `require`, single `nuget.org` source with `<clear/>`, package source mapping, and the three `nuget.org` `trustedSigners` certificate fingerprints). This is the Memories precedent specifically (Conversations ships no NuGet.config, EventStore a bare one) — adopt it as the new-module baseline. [Source: Hexalith.Memories/NuGet.config]
  - [ ] `tests/Directory.Build.props`: import the root props via `GetPathOfFileAbove`, set `IsTestProject=true`, `IsPackable=false`, `NoWarn=$(NoWarn);IDE1006;CA2007;xUnit1051`, and add the shared test PackageReferences + `<Using Include="Xunit" />`. [Source: Hexalith.Tenants/tests/Directory.Build.props]

- [ ] **Task 2 — Scaffold `src/` projects per the Structural Seed** (AC: #1, #4)
  - [ ] Create the project tree exactly as the architecture Structural Seed lists (see Dev Notes **Project Layout**): `Hexalith.Agents.Contracts`, `Hexalith.Agents.Client`, `Hexalith.Agents.Server` (with empty extension folders `Aggregates/`, `Application/{Agents,Workflows,Activities,Tools}/`, `Ports/`, `Projections/`), `Hexalith.Agents` (main domain library), `Hexalith.Agents.UI`, `Hexalith.Agents.AppHost`, `Hexalith.Agents.Aspire`, `Hexalith.Agents.ServiceDefaults`, `Hexalith.Agents.Testing`.
  - [ ] Use the correct SDK per project type: `Microsoft.NET.Sdk` (Contracts/Client/domain/UI library/Testing/Aspire/ServiceDefaults), `Microsoft.NET.Sdk.Web` (Server), and the Aspire AppHost MSBuild SDK for AppHost (`OutputType=Exe`, `UserSecretsId=hexalith-agents`). **Note two distinct version knobs:** the `Aspire.AppHost.Sdk` MSBuild SDK string vs the `Aspire.Hosting` NuGet package. Siblings pin the AppHost SDK at `13.4.6` (Parties, newest), `13.4.2` (Tenants/Conversations), `13.3.3` (Memories); the `Aspire.Hosting` *package* is `13.4.6` across siblings. Use `Aspire.AppHost.Sdk/13.4.6` and `Aspire.Hosting` `13.4.6` (align both on the newest = Parties). ServiceDefaults sets `<IsAspireSharedProject>true</IsAspireSharedProject>`.
  - [ ] Mark packable libraries `<IsPackable>true</IsPackable>` with `PackageId`/`Description`/`PackageTags` (Contracts/Client/domain/Testing/Aspire); mark hosts `<IsPackable>false</IsPackable>` (Server/AppHost/ServiceDefaults).
  - [ ] Keep folders as named extension points only — **add no aggregates, events, commands, or projections** in this story (deferred to Stories 1.2–1.8). Add a short `README.md` or empty `.gitkeep` per extension folder so the structure is reviewable and committed.

- [ ] **Task 3 — Wire minimal, boundary-correct project references** (AC: #2)
  - [ ] Dependency direction: `Client → Contracts`; `Server → Contracts (+ Client)`; `UI → Contracts (+ Client)`; `Testing → Contracts (+ Server)`; `AppHost → Server (+ UI)`. **Contracts references nothing outward** (no Client/Server/UI/EventStore-server/provider/Dapr-runtime/UI-shell). [Source: architecture AD-15; sibling dependency tables]
  - [ ] Use the conditional sibling-source ProjectReference pattern for any cross-module reference (`$(HexalithXxxRoot)\src\...` with `Condition="'$(HexalithXxxRoot)' != ''"` plus the `..\..\Hexalith.Xxx\src\...` fallback with `Condition="'$(HexalithXxxRoot)' == ''"`). Only add a cross-module reference when the shell genuinely needs it to build — do not wire EventStore/Conversations/Parties/Tenants domain references that no shell code uses yet.
  - [ ] `Hexalith.Agents.Server` `Program.cs`: a minimal buildable host is sufficient for this story. Prefer the EventStore domain-service shape (`builder.AddEventStoreDomainService(...); … app.UseEventStoreDomainService();`) **only if** it builds cleanly with the referenced EventStore source; otherwise a minimal `WebApplication` stub is acceptable. Note the real-world form uses the explicit-assemblies overload (`AddEventStoreDomainService(typeof(AgentsAssemblyMarker).Assembly, …)` plus `AddDaprClient()`/tenant-access registrations), per Conversations' `Server/Program.cs` — the parameterless "two-liner" is the canonical *shape*, not the literal call. A *functioning* AppHost/topology is explicitly **not required** by this story (see **Scope Guardrails**).

- [ ] **Task 4 — Author `Hexalith.Agents.slnx`** (AC: #1)
  - [ ] Create `Hexalith.Agents.slnx` (modern XML format — never `.sln`) with `/src/`, `/tests/`, and a `/Solution Items/` folder listing the root build/docs files. List every project created in Tasks 2 & 5. [Source: Hexalith.Tenants/Hexalith.Tenants.slnx]

- [ ] **Task 5 — Add boundary-guard placeholder tests** (AC: #3)
  - [ ] Create test projects `tests/Hexalith.Agents.Contracts.Tests` and `tests/Hexalith.Agents.Server.Tests` (and `tests/Hexalith.Agents.Client.Tests` if it adds value). Each: `Microsoft.NET.Sdk`, `IsPackable=false`, ProjectReference to the project under test; PackageReferences inherited from `tests/Directory.Build.props`.
  - [ ] **Boundary test:** assert the `Hexalith.Agents.Contracts` assembly's referenced-assembly set contains no forbidden assembly (server-infra, provider SDKs, EventStore *server* internals, Dapr *runtime implementation* packages, UI-shell). Drive it from `Assembly.GetReferencedAssemblies()` against a denylist of name prefixes.
  - [ ] **Centralization test:** scan every `*.csproj` under the module and fail if any `<PackageReference>` carries an inline `Version=` attribute (CPM must own all versions). [Source: CLAUDE.md#Build — "no inline `<PackageReference Version>`"]
  - [ ] **Secret/config non-leakage test:** assert no public type in `Hexalith.Agents.Contracts` exposes a provider-SDK type or a secret-bearing member (e.g. members named `*Secret*`/`*ApiKey*`/`*Credential*` or typed from a provider SDK namespace). For the empty shell this passes trivially and stands as a guard that grows with the contracts. [Source: architecture AD-9, AD-14]
  - [ ] Name tests PascalCase, assert with Shouldly (never raw `Assert.*`).

- [ ] **Task 6 — Verify the narrow build + tests** (AC: #1, #3)
  - [ ] Run `dotnet restore Hexalith.Agents.slnx` then `dotnet build Hexalith.Agents.slnx --configuration Release`; confirm zero warnings/errors (TreatWarningsAsErrors makes any warning fatal).
  - [ ] Run each test project individually (`dotnet test tests/Hexalith.Agents.Contracts.Tests/ …`) — never solution-level `dotnet test`. All tests pass. [Source: CLAUDE.md#Testing]
  - [ ] Confirm the `CheckSubmodules` target passes (siblings already present at the workspace root).

## Dev Notes

### Scope Guardrails (read first)
- This is the **greenfield foundation story** — its job is a *buildable shell*, public boundaries, and guard tests. It is explicitly acceptable as technical setup and must **avoid creating future domain entities upfront**. [Source: implementation-readiness-report-2026-06-23.md#Story-1.1]
- **Do NOT** implement aggregates, events, commands, projections, provider adapters, UI pages, FrontComposer nav registration, or a functioning AppHost topology. Those belong to Stories 1.2–1.8. A *functioning* AppHost / CI quality gate is a known gap deliberately deferred (readiness report Concern #5) — create the project so the solution builds, but do not wire a runnable topology.
- Add packages, references, and folders **only as the shell + guard tests need them**. Empty named folders/projects are the deliverable for extension points (AC4).

### Authoritative coding standards (MUST follow — these are the dev-agent guardrails)
Read before coding: `Hexalith.AI.Tools/CLAUDE.md`, `Hexalith.AI.Tools/hexalith-llm-instructions.md`, `Hexalith.AI.Tools/hexalith-state-instructions.md`, `Hexalith.AI.Tools/hexalith-ux-instructions.md`.
- **Solutions:** `.slnx` only — never create/use `.sln`.
- **Language/build:** `net10.0`, C# 14 / `LangVersion=14`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`, Central Package Management (no inline `PackageReference Version`).
- **Style:** file-scoped namespaces (namespace = folder path); Allman braces; `_camelCase` private fields; `I`-prefixed interfaces; `Async` suffix on async methods; 4-space indent, CRLF, UTF-8; primary constructors preferred; XML docs on all public/protected/internal members. Enforced by `.editorconfig` + warnings-as-errors. **Copyright headers are optional and unenforced** — `.editorconfig` has no header rule, so warnings-as-errors won't trip either way. Sibling practice is split (Tenants omits them; Memories/Conversations carry ITANEO headers). Do not block on header presence/absence; if you copy a sibling's `Program.cs` that carries a header, leaving it is fine.
- **Async:** `ConfigureAwait(false)` on every awaited call in library/client code (CA2007 enforced); test projects opt out via `NoWarn` for CA2007.
- **Identifiers are ULIDs** — never `Guid.TryParse` on `messageId`/`aggregateId`/`correlationId`/`causationId`. (No IDs are created in this story, but keep the rule in mind for the shape of any sample.)
- **Persistence:** domain modules persist only through `Hexalith.EventStore` (no EF Core, no raw DAPR state calls, no other store). Not exercised in this story, but it constrains how the Server is wired.
- **Commit messages:** Conventional Commits (`feat(...)`, `fix(...)`, `chore(...)`); branch `feat/<desc>`.
[Source: Hexalith.AI.Tools/CLAUDE.md; hexalith-*-instructions.md]

### Project Layout (architecture Structural Seed — authoritative for this module)
```text
Hexalith.Agents/
  Hexalith.Agents.slnx
  global.json
  Directory.Build.props
  Directory.Packages.props
  NuGet.config
  src/
    Hexalith.Agents.Contracts/       # public contracts only; references nothing outward
    Hexalith.Agents.Client/          # → Contracts
    Hexalith.Agents.Server/          # → Contracts (+Client); SDK=Microsoft.NET.Sdk.Web
      Aggregates/                    # (empty extension point — Agent/ProviderCatalog/AgentInteraction later)
      Application/
        Agents/
        Workflows/
        Activities/
        Tools/
      Ports/
      Projections/
    Hexalith.Agents/                 # main domain library (aggregate roots/state later)
    Hexalith.Agents.UI/              # → Contracts (+Client); FrontComposer registration later
    Hexalith.Agents.AppHost/         # SDK=Aspire.AppHost.Sdk; OutputType=Exe (not a runnable topology this story)
    Hexalith.Agents.Aspire/          # Aspire hosting extensions (packable)
    Hexalith.Agents.ServiceDefaults/ # IsAspireSharedProject=true
    Hexalith.Agents.Testing/         # shared test helpers (→ Contracts +Server)
  tests/
    Directory.Build.props
    Hexalith.Agents.Contracts.Tests/
    Hexalith.Agents.Server.Tests/
    Hexalith.Agents.Client.Tests/    # optional
    Hexalith.Agents.UI.Tests/        # optional, defer if no UI code yet
    Hexalith.Agents.IntegrationTests/ # optional shell; defer DAPR/Aspire wiring
```
[Source: ARCHITECTURE-SPINE.md#Structural-Seed]

### Build-File Templates (copy the sibling patterns verbatim, adapt names)

`Directory.Build.props` sibling-resolution + submodule-guard pattern (adapt the module list to Agents' real deps — EventStore, Conversations, Parties, Tenants, FrontComposer, Commons). The new module sits at the workspace root next to the siblings, so the `..\Hexalith.Xxx\...` branch resolves them:
```xml
<HexalithEventStoreRoot Condition="'$(HexalithEventStoreRoot)' == '' and Exists('$(MSBuildThisFileDirectory)..\Hexalith.EventStore\src\Hexalith.EventStore.Contracts')">$(MSBuildThisFileDirectory)..\Hexalith.EventStore</HexalithEventStoreRoot>
<HexalithEventStoreRoot Condition="'$(HexalithEventStoreRoot)' == '' and Exists('$(MSBuildThisFileDirectory)Hexalith.EventStore\src\Hexalith.EventStore.Contracts')">$(MSBuildThisFileDirectory)Hexalith.EventStore</HexalithEventStoreRoot>
<!-- repeat for Tenants, Parties, Conversations, FrontComposer, Commons -->
```
```xml
<ItemGroup>
  <RequiredRootSubmodule Include="Hexalith.Commons" />
  <RequiredRootSubmodule Include="Hexalith.EventStore" />
  <RequiredRootSubmodule Include="Hexalith.AI.Tools" />
  <RequiredRootSubmodule Include="Hexalith.Conversations" />
  <RequiredRootSubmodule Include="Hexalith.Parties" />
  <RequiredRootSubmodule Include="Hexalith.Tenants" />
  <RequiredRootSubmodule Include="Hexalith.FrontComposer" />
</ItemGroup>
<Target Name="CheckSubmodules" BeforeTargets="Restore;Build">
  <Error Condition="!Exists('$(MSBuildThisFileDirectory)%(RequiredRootSubmodule.Identity)/.git') and !Exists('$(MSBuildThisFileDirectory)../%(RequiredRootSubmodule.Identity)/.git')"
    Text="Git submodule '%(RequiredRootSubmodule.Identity)' is missing. Run: git submodule update --init %(RequiredRootSubmodule.Identity) from the repository root." />
</Target>
```
[Source: Hexalith.Memories/Directory.Build.props — the `RequiredRootSubmodule`/`CheckSubmodules` target is the Memories pattern; Conversations/Tenants use root-detection without that exact target]

Conditional cross-module ProjectReference pattern (only where actually needed to build):
```xml
<ProjectReference Include="$(HexalithEventStoreRoot)\src\Hexalith.EventStore.Contracts\Hexalith.EventStore.Contracts.csproj" Condition="'$(HexalithEventStoreRoot)' != ''" />
<ProjectReference Include="..\..\Hexalith.EventStore\src\Hexalith.EventStore.Contracts\Hexalith.EventStore.Contracts.csproj" Condition="'$(HexalithEventStoreRoot)' == ''" />
```
[Source: Hexalith.Conversations/src/Hexalith.Conversations/Hexalith.Conversations.csproj]

Test `.csproj` shape (minimal because `tests/Directory.Build.props` supplies the rest):
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><IsPackable>false</IsPackable></PropertyGroup>
  <ItemGroup>
    <ProjectReference Include="..\..\src\Hexalith.Agents.Contracts\Hexalith.Agents.Contracts.csproj" />
  </ItemGroup>
</Project>
```
[Source: Hexalith.Memories/tests/Hexalith.Memories.Contracts.Tests; Hexalith.Tenants/tests/Directory.Build.props]

### Versions (use these when a package is genuinely added)
.NET SDK `10.0.301` (`net10.0`) · MediatR `14.1.0` · FluentValidation `12.1.1` · Dapr `1.18.4` · Aspire Hosting `13.4.6` (`Aspire.AppHost.Sdk/13.4.6`) · Microsoft.Agents.AI / .Workflows `1.10.0` · ModelContextProtocol `1.4.0` · Fluent UI Blazor `5.0.0-rc.3-26138.1` · xUnit v3 `3.2.2` · xunit.runner.visualstudio `3.1.5` · Shouldly `4.3.0` · NSubstitute `5.3.0` · Microsoft.NET.Test.Sdk `18.6.0` · coverlet.collector `10.0.1` · ByteAether.Ulid `1.3.7`. Provider SDKs are deferred/adapter-local — do **not** add any in this story. [Source: ARCHITECTURE-SPINE.md#Stack]

### Testing standards
- xUnit v3 + Shouldly + NSubstitute + coverlet. Test method names PascalCase. **Run test projects individually** (use `.slnx` for restore/build only). Organize by aggregate as code grows. Integration tests (when they exist) must assert state-store end-state, not just status codes. [Source: CLAUDE.md#Testing]
- This story's tests are architecture-conformance guard tests (the Conversations module's `*.Conformance.Tests` project is precedent for this pattern).

### Project Structure Notes

**Alignment:** The architecture Structural Seed is the **authoritative** layout for `Hexalith.Agents`. No single sibling matches the seed 1:1 (Tenants ships `.UI`/`.Testing` but no `.ServiceDefaults`; Memories ships `.ServiceDefaults`/`.Web` but no `.UI` or bare main lib, plus extras like `.Cli`/`.Mcp`/`.Redis`) — so follow the seed, not any one sibling. The seed is nonetheless consistent with the ecosystem's conventions: `.Contracts / .Client / .Server / main library / .UI / .AppHost / .Aspire / .ServiceDefaults / .Testing`, `src/` + `tests/`, `.slnx`, `global.json`, root `Directory.Build.props` + `Directory.Packages.props`, hardened `NuGet.config`, and the `Hexalith*Root` sibling-source resolution with a `CheckSubmodules` guard.

**Variance 1 — AppHost/Aspire/ServiceDefaults in a domain module.** `Hexalith.AI.Tools/CLAUDE.md` (and `hexalith-state-instructions.md`) state a *pure* domain module "must not ship its own `*.AppHost`, `*.Aspire`, or `*.ServiceDefaults`." The Agents architecture **deliberately overrides this**: AD-16 ("Hexalith.Agents owns its own AppHost/local orchestration and deployable workloads") and AD-18 (hybrid agent-runtime: .NET AgentHost + Dapr Workflow + optional Python Dapr Agents worker) make Agents a *hybrid agent-runtime* module, not a pure event-sourced domain. These project types each appear across the sibling ecosystem (AppHost in Tenants/Conversations/Memories; Aspire in Tenants/Memories; ServiceDefaults in Conversations/Memories/Parties) — no single sibling ships all three, but each is sanctioned and in use — so following the Structural Seed is both architecture-compliant and consistent with real practice. **Resolution:** include `.AppHost`/`.Aspire`/`.ServiceDefaults` per the Structural Seed (AppHost not yet a runnable topology — see Scope Guardrails). [Source: AD-16, AD-18; sibling `.slnx` files]

**Variance 2 — consolidated vs granular vertical-slice layout.** `CLAUDE.md` documents a finely-split layout (`src/libraries/Domain/…`, `Application/…`, `Infrastructure/…`, `Presentation/…` with many projects). The Agents architecture intentionally specifies a **consolidated** seed (a single `Hexalith.Agents.Server` with internal `Aggregates/`, `Application/`, `Ports/`, `Projections/` folders) and defers finer splitting. **Resolution:** follow the architecture Structural Seed; do not pre-split into the full granular project set. [Source: ARCHITECTURE-SPINE.md#Structural-Seed vs CLAUDE.md#DDD-Architecture]

**Variance 3 — `Hexalith.Builds` import.** `CLAUDE.md` says centralized build config "comes from `Hexalith.Builds`," but the sibling modules' root `Directory.Build.props` are in fact **self-contained** and do not import `Hexalith.Build.props`/`Hexalith.Package.props`; the Structural Seed likewise lists only self-contained build files. **Resolution:** author self-contained build files (Memories/Tenants/Conversations precedent). Do not require importing `Hexalith.Builds` for V1. [Source: Hexalith.Memories/Directory.Build.props]

**Decision — module placement (submodule vs folder).** The siblings are git submodules of the `agents` super-repo. Creating `Hexalith.Agents` as a *submodule* requires a separate GitHub repository, which is out of scope for this story. **Recommendation:** scaffold `Hexalith.Agents/` as a regular top-level directory within the `agents` workspace for now; promotion to its own submodule/repo can follow once the module stabilizes. Surface this in Dev Agent Record if the team prefers otherwise. [Source: AD-16; .gitmodules]

### References
- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.1-Buildable-Agents-Module-Shell-And-Public-Boundaries]
- [Source: _bmad-output/planning-artifacts/epics.md#Additional-Requirements] — scaffold from the Structural Seed using `.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `src/`+`tests/`.
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#Structural-Seed]
- [Source: ARCHITECTURE-SPINE.md#Stack] — pinned versions and local Hexalith baseline.
- [Source: ARCHITECTURE-SPINE.md] AD-15 (Public Surface & UI Parity), AD-16 (Module-Local Operational Topology), AD-17 (Contract & Test Gates), AD-18 (Hybrid Agent Runtime Ownership), AD-9/AD-14 (provider/secret non-disclosure).
- [Source: Hexalith.AI.Tools/CLAUDE.md] — authoritative coding/build/test/commit standards.
- [Source: Hexalith.AI.Tools/hexalith-llm-instructions.md, hexalith-state-instructions.md, hexalith-ux-instructions.md]
- [Source: Hexalith.Memories/{Directory.Build.props, Directory.Packages.props, NuGet.config, global.json, .slnx, src/*, tests/*}] — verbatim build-file precedents.
- [Source: Hexalith.Tenants/{Directory.Build.props, Directory.Packages.props, .slnx, tests/Directory.Build.props}] — domain-module precedents.
- [Source: Hexalith.Conversations/{Directory.Build.props, src/*/*.csproj}] — conditional cross-module source-reference precedents.
- [Source: _bmad-output/planning-artifacts/implementation-readiness-report-2026-06-23.md#Story-1.1] — foundation-story scope and the deferred AppHost/CI gap.

## Dev Agent Record

### Agent Model Used

(to be filled by dev agent)

### Debug Log References

### Completion Notes List

- Story context created by exhaustive create-story analysis: epics Story 1.1, architecture spine (Structural Seed + AD-9/14/15/16/17/18 + Stack), implementation-readiness report, and verbatim build/domain conventions extracted from sibling modules Memories, Tenants, and Conversations plus the authoritative `Hexalith.AI.Tools` instruction set. Three architectural variances (AppHost-in-domain-module, consolidated layout, Hexalith.Builds import) and the module-placement decision are documented in Project Structure Notes to prevent implementation drift.

### File List
