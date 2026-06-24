---
baseline_commit: 7d720606d81bca5a958d146828cea219fd54a6ff
---

# Story 1.8: Admin Setup UI And Readiness Overview

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an Agent Administrator,
I want a FrontComposer setup experience for provider governance and `hexa` readiness,
so that I can configure and activate `hexa` without using internal EventStore or provider details.

## Acceptance Criteria

**AC1 - The FrontComposer shell registers an Agents domain/category with policy-gated setup navigation**
**Given** the Agents setup contracts exist
**When** an authorized administrator opens the FrontComposer shell
**Then** an Agents domain/category is registered with setup-oriented navigation entries for Agents overview, `hexa` configuration, Provider catalog, and Approver policy
**And** policy-gated navigation hides or denies entries without leaking unauthorized records.

**AC2 - The Agents overview renders `hexa` readiness, and the readiness badge separates active lifecycle from callability**
**Given** the administrator opens the Agents overview
**When** readiness data is loaded
**Then** the view shows `hexa` readiness, lifecycle, response mode, Provider/model, activation blockers, and callability for the tenant
**And** `agent-readiness-badge` distinguishes active lifecycle from callable readiness.

**AC3 - The Provider catalog is a full-width grid that surfaces capability/readiness/configured state but never secrets**
**Given** the administrator opens Provider catalog
**When** Provider/model records are loaded
**Then** a full-width FrontComposer FC-TBL/FluentDataGrid surface shows Provider/model options, enabled state, capability metadata, readiness, and secret configured/not-configured state
**And** secret values, raw provider payloads, and provider SDK details are never displayed, logged, copied, or placed in accessible names.

**AC4 - `hexa` configuration and Approver policy use constrained Fluent forms with a future-only response-mode choice**
**Given** the administrator opens `hexa` configuration or Approver policy
**When** form controls render
**Then** constrained FrontComposer/Fluent layouts are used for identity, instructions, Provider/model, response mode, approver policy, lifecycle, activation blockers, and content safety state
**And** response mode uses a mutually exclusive Fluent segmented control or radio group whose copy states that changes affect future Agent Calls only.

**AC5 - Status indicators use consistent semantic Fluent roles (color + icon + text), enforced by conformance tests**
**Given** setup surfaces display statuses
**When** statuses are rendered
**Then** semantic Fluent status roles, icons, and visible text are used consistently
**And** color-only status, custom radii, non-localizable sentence fragments, and layout shifts from changing badges/action slots are prevented by component or conformance tests.

**AC6 - Setup pages pass accessibility tests across all surface states**
**Given** setup pages are tested for accessibility
**When** keyboard, focus, loading, empty, filtered-empty, error, permission-denied, and stale/degraded states are exercised
**Then** FC-A11Y primitives, named navigation landmarks, focus visibility, table semantics, live-region status behavior, and safe accessible names pass the relevant UI tests.

[Source: _bmad-output/planning-artifacts/epics.md#Story-1.8-Admin-Setup-UI-And-Readiness-Overview; epics.md#UX-Design-Requirements (UX-DR1–UX-DR5, UX-DR11–UX-DR20, UX-DR23, UX-DR25, UX-DR26, UX-DR30, UX-DR33, UX-DR34, UX-DR41); architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-15-Public-Surface-And-UI-Parity; #AD-12-Authorization-And-Dependency-Uncertainty; #AD-14-Sensitive-Content-And-Secret-Safety; #AD-9-Provider-Adapter-And-Catalog-Boundary; #AD-10-Provider-Capability-Floor; #AD-8-Approver-Policy-Resolution; #AD-17-Contract-And-Test-Gates; ux-designs/ux-agents-2026-06-23/DESIGN.md; ux-designs/ux-agents-2026-06-23/EXPERIENCE.md]

## Tasks / Subtasks

- [x] **Task 1 - Turn `Hexalith.Agents.UI` into a FrontComposer Razor component library** (AC: #1, #2, #3, #4, #5, #6)
  - [x] Change `src/Hexalith.Agents.UI/Hexalith.Agents.UI.csproj` SDK from `Microsoft.NET.Sdk` to **`Microsoft.NET.Sdk.Razor`** (a Razor Class Library — keep `IsPackable=true`/`PackageId=Hexalith.Agents.UI` so the AppHost composes it, mirroring the Structural Seed where `Hexalith.Agents.AppHost` references `Hexalith.Agents.UI`). Do NOT make it `Microsoft.NET.Sdk.Web` — runtime hosting (a runnable BFF host + `app.UseRequestLocalization()` + shell quickstart wiring) is part of the **deferred AppHost topology** (see Scope guardrails). Keep the existing `ProjectReference`s to `Hexalith.Agents.Contracts` and `Hexalith.Agents.Client`.
  - [x] Add the two FrontComposer **project** references (out-of-module sibling source via the already-resolved `$(HexalithFrontComposerRoot)` MSBuild var in `Directory.Build.props`): `$(HexalithFrontComposerRoot)\src\Hexalith.FrontComposer.Contracts\Hexalith.FrontComposer.Contracts.csproj` and `$(HexalithFrontComposerRoot)\src\Hexalith.FrontComposer.Shell\Hexalith.FrontComposer.Shell.csproj`. Add `<PackageReference Include="Microsoft.FluentUI.AspNetCore.Components" />` (no inline version — CPM). Add `<InternalsVisibleTo Include="Hexalith.Agents.UI.Tests" />`. Mirror `Hexalith.Tenants/src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj` exactly for structure.
  - [x] Add the FluentUI package version to **central package management** in `Hexalith.Agents/Directory.Packages.props`: `<PackageVersion Include="Microsoft.FluentUI.AspNetCore.Components" Version="5.0.0-rc.3-26138.1" />` (the FrontComposer-governed baseline — same version as `Hexalith.Tenants/Directory.Packages.props`). Do not pin a different version; the version is governed by the FrontComposer baseline (commit `eee5e6b`). `PackageVersionCentralizationTests` requires every referenced package to have a central version.
  - [x] Add `src/Hexalith.Agents.UI/Components/_Imports.razor` mirroring `Hexalith.Tenants.UI/Components/_Imports.razor` global usings: `Microsoft.AspNetCore.Components.*` (Web, Forms, Routing, Authorization), `Microsoft.FluentUI.AspNetCore.Components`, `Microsoft.Extensions.Localization`, `Hexalith.FrontComposer.Contracts.Rendering`, `Hexalith.FrontComposer.Contracts.Registration`, `Hexalith.FrontComposer.Shell.Components.Layout`, `Hexalith.FrontComposer.Shell.Components.Icons`, `Hexalith.Agents.Contracts.Agent`, `Hexalith.Agents.Contracts.ProviderCatalog`, and the UI's own `Components`/`Resources`/`Services` namespaces.
  - [x] Add `Components/Layout/MainLayout.razor` = `<FrontComposerShell>@Body</FrontComposerShell>` (mirror `Hexalith.Tenants.UI/Components/Layout/MainLayout.razor`). Do **not** add custom skip links/landmarks — the shell owns them (see FC-A11Y note). Add `Components/Routes.razor` + `Components/App.razor` only if needed for component-test hosting; the runnable host's `Program.cs` is deferred.
  - [x] Add `Hexalith.Agents.UI` to `Hexalith.Agents.slnx` if any new top-level project is created (the UI project is already in the slnx; only the new **test** project needs adding — Task 11). Keep `ProjectReferenceDirectionTests` green: `Hexalith.Agents.UI` is already allow-listed for `["Hexalith.Agents.Contracts","Hexalith.Agents.Client"]`; the FrontComposer/FluentUI references are **out-of-module** and are not checked by the in-module direction guard. `PublicContractPackageBoundaryTests` forbids FluentUI/FrontComposer only in `Contracts`/`Client`, never in `UI` — do not add UI packages to those two projects.

- [x] **Task 2 - Add the UI-side read gateway abstraction (live binding deferred)** (AC: #2, #3, #4, #6)
  - [x] The read path is **not wired today**: the four query contracts exist but have no live handler, and `Hexalith.Agents.Client` is an empty shell (no proxies). Mirror the project's established deferral convention (1.2–1.7 deferred live command dispatch via `DeferredAgentCommandDispatcher`) and the `Hexalith.Tenants.UI/Services/Gateways/` pattern. Add UI-side gateway interfaces under `src/Hexalith.Agents.UI/Services/Gateways/`:
    - `IAgentSetupGateway` with `Task<AgentInspectionResult> GetStatusAsync(CancellationToken)` (backs the overview + config form — both read `AgentStatusView`) and `Task<AgentInspectionResult> GetConfigurationAsync(CancellationToken)`.
    - `IProviderCatalogGateway` with `Task<ProviderCatalogInspectionResult> ListEntriesAsync(bool includeDisabled, CancellationToken)`.
    - These return the **existing** Contracts result wrappers (`AgentInspectionResult`/`AgentInspectionStatus`, `ProviderCatalogInspectionResult`/`ProviderCatalogInspectionStatus`) and views (`AgentStatusView`, `ProviderCatalogEntryView`). Do NOT add new view/DTO types — the display contracts already exist and are stable + serializable.
  - [x] Add a **deferred placeholder** implementation (e.g. `DeferredAgentSetupGateway`/`DeferredProviderCatalogGateway`) whose methods return the fail-closed result (`AgentInspectionResult.NotAuthorized()` / `ProviderCatalogInspectionResult.NotAuthorized()`) or throw a clearly-documented `NotSupportedException("Live Agents read path is wired by the dedicated Agents read-model / BFF story — Epic 4 (4.1/4.3).")`, with an XML-doc note that the real implementation calls `Hexalith.Agents.Client` → BFF/API → read-model and is deferred. This keeps the DI graph complete and the project buildable, exactly like the deferred ports in `Hexalith.Agents.Server`. Add an `AddAgentsUi(this IServiceCollection)` extension that registers the gateways (scoped) so the future host and the bUnit tests share one registration seam. The bUnit tests substitute the gateways with NSubstitute (Task 11) — the deferred impl is never exercised in tests.
  - [x] **Fail-closed rendering contract:** every page consumes a gateway result and renders a distinct surface state per status — `Success` → data; `NotAuthorized` → permission-denied state (no record fingerprinting); `AgentNotFound`/`EntryNotFound` → empty state; plus loading and error states for the in-flight/faulted cases (AC6, AD-12). A null `Agent`/empty `Entries` on a non-success result must never be rendered as "ready/healthy".

- [x] **Task 3 - Add localization resources (whole strings, named placeholders)** (AC: #1, #4, #5)
  - [x] Add `src/Hexalith.Agents.UI/Resources/AgentsResources.cs` = `namespace Hexalith.Agents.UI.Resources; public sealed class AgentsResources;` (empty strongly-typed marker — mirror `TenantsResources.cs`).
  - [x] Add `Resources/AgentsResources.resx` (en) and `Resources/AgentsResources.fr.resx` (fr). Auto-embedded by the Razor/Web SDK — **no csproj `<EmbeddedResource>` entries needed**. Use hierarchical dotted keys `Agents.Area.Element[.Variant]` (e.g. `Agents.Navigation.Agents`, `Agents.Overview.Title`, `Agents.Overview.Readiness.Callable`, `Agents.ProviderCatalog.Column.Status`, `Agents.Config.ResponseMode.FutureOnlyNote`, `Agents.Readiness.Blocker.MissingPartyIdentity`). Every status label, blocker reason, denial reason, provider/model state, response-mode label, and column header is **one complete localizable string** with named/positional placeholders — never assembled from runtime fragments (UX-DR14, AC5).
  - [x] Add a key for **every** `AgentActivationBlocker` value, **every** canonical readiness state, **every** `ProviderModelStatus`/`ProviderConfigurationState` value, both `AgentResponseMode` values, and both empty-vs-filtered-empty + permission-denied + error + stale surface states. Consume via `@inject IStringLocalizer<AgentsResources> Localizer` and `@Localizer["key"]` / `@Localizer["key", arg]`. Dynamic-suffix lookup (`Localizer[$"Agents.Readiness.Blocker.{blocker}"]`) is allowed for enum-keyed whole strings (mirror Tenants).

- [x] **Task 4 - Register the Agents domain/category and policy-gated navigation** (AC: #1)
  - [x] Add `src/Hexalith.Agents.UI/Composition/AgentsFrontComposerDomain.cs` = `namespace Hexalith.Agents.UI.Composition; public sealed class AgentsFrontComposerDomain;` (empty marker; the shell discovers a static `*Registration` with a static `Manifest` + `RegisterDomain`). Mirror `TenantsFrontComposerDomain.cs`.
  - [x] Add `Composition/AgentsFrontComposerRegistration.cs` (static) mirroring `TenantsFrontComposerRegistration.cs`:
    - `public static DomainManifest Manifest { get; } = new("Agents", "agents", [], [], Icon: "<a pinned Fluent Regular.Size20.* glyph>", NameKey: "Agents.Navigation.Agents", Resource: typeof(AgentsResources));` — verify the icon name against the pinned FluentUI package at build (UX-DR icon-vocabulary rule; follow the Tenants precedent of using existing `Regular.Size20.*` names).
    - `public static void RegisterDomain(IFrontComposerRegistry registry)` calling `registry.RegisterDomain(Manifest)` then **four** `registry.AddNavEntry(new FrontComposerNavEntry("agents", <invariant English Title>, <href>, Order: n, TitleKey: <key>, Resource: typeof(AgentsResources), RequiredPolicy: <policy>))` entries ordered from operational setup to workflow handling (UX-DR1):
      - Order 0 — `"Agents overview"` → `/agents` (default surface; UX-DR2).
      - Order 1 — `"hexa configuration"` → `/agents/configuration`.
      - Order 2 — `"Provider catalog"` → `/agents/providers`.
      - Order 3 — `"Approver policy"` → `/agents/approver-policy`.
    - The invariant `Title` is the English fallback and drives stable test ids + sort; `TitleKey`+`Resource` localize the label. Make the category title and the first child distinct words (Tenants uses "All tenants" under the "Tenants" category) — e.g. category "Agents" / child "Agents overview".
  - [x] **Policy-gated navigation (AC1, AD-15):** declare an Agents admin policy constant (e.g. `public const string AgentsAdministratorPolicy = "Agents.Administrator";`) and set `RequiredPolicy: AgentsAdministratorPolicy` on the provider/config/approver admin entries. The shell wraps a `RequiredPolicy` entry in `<AuthorizeView Policy="...">` so unauthorized users never see (or can navigate to) the link — no record leak (UX-DR41 policy-gated nav). The overview may stay ungated or gated per the same admin policy (gate it — setup is admin-only). Also apply `@attribute [Authorize(Policy = AgentsAdministratorPolicy)]` on the page components themselves (defense in depth — nav hiding alone is not authorization; AD-12 "no UI-only authorization"). Document that registering the actual `AddAuthorizationCore(...AddPolicy(...))` lives in the deferred host `Program.cs`; the constant + entry wiring is in scope.

- [x] **Task 5 - Implement the readiness mapping + `agent-readiness-badge` + `provider-status-badge`** (AC: #2, #3, #5)
  - [x] Add a pure mapping helper `Components/Shared/AgentReadiness.cs` (or `State/AgentReadinessPresentation.cs`) that maps an `AgentStatusView` to the canonical readiness state and to a (semantic role, icon, label-key) triple. **Mapping (do not collapse active vs callable — UX-DR20, AC2):**
    - `callable` ⟺ `Lifecycle == AgentLifecycleStatus.Active` **AND** `ActivationBlockers` is empty → `BadgeColor.Success` (use Success **only** here — UX-DR11/DESIGN "Success only when all required readiness gates pass").
    - `disabled` ⟺ `Lifecycle == AgentLifecycleStatus.Disabled` → `BadgeColor.Severe`.
    - `missing party identity` ⟺ blockers contain `MissingPartyIdentity` → `BadgeColor.Severe`.
    - `provider unavailable` ⟺ blockers contain `MissingProviderSelection` or `ProviderUnavailable` → `BadgeColor.Severe`.
    - `invalid configuration` ⟺ blockers contain `MissingDisplayName`/`MissingInstructions`/`InvalidInstructions`/`MissingResponseMode`/`MissingApproverPolicy`/`ApproverPolicyUnresolvable`/`MissingContentSafetyPolicy` → `BadgeColor.Severe` (and `BadgeColor.Important` for ambiguous/unresolved cases if a verdict surfaces one; UX-DR11 Important = "uncertain, resolve before side effects").
    - `checking` ⟺ readiness is being (re)evaluated — a **UI-only transient** state while a gateway call is in flight (there is no contract field for it) → `BadgeColor.Informative`.
    - The badge always renders **color + icon + visible text** (UX-DR12; never color-only); accessible name is a safe whole string and never includes ids/secrets/PII (AC3, AD-14). The badge must **explain blockers, not hide them** — render the activation-blocker list inline near the badge as localized whole strings (one key per blocker), not a generic "not ready".
  - [x] Add `Components/Shared/AgentReadinessBadge.razor` and `Components/Shared/ProviderStatusBadge.razor`. Follow the domain-wrapper pattern (`Hexalith.Tenants.UI/Components/Shared/TruthStateBadge.razor`, `Hexalith.Parties.UI/Components/Shared/PartyStateBadge.razor`): `<FluentBadge Appearance="BadgeAppearance.Tint" Color="@color" IconStart="@icon" role="status" aria-label="@label" data-testid="@testId">@label</FluentBadge>` with a `switch` mapping enum→`BadgeColor`/→`Icon`. Do not invent custom radii (UX-DR19) or use elevation for emphasis (UX-DR18).
    - **Icons — `FcFluentIcons` is a SMALL curated set, not a full icon map.** Its actual public glyphs are `Checkmark16/SubtractCircle16/QuestionCircle16/Warning16/ArrowSync16/Star16/Edit16/Eye16/Key16/Copy16` (+ some Size20/Size32/Size48). So use: `FcFluentIcons.Checkmark16()` (Success/callable), `FcFluentIcons.Warning16()` (Warning/Severe and `Important`), `FcFluentIcons.SubtractCircle16()` (blocked/disabled), `FcFluentIcons.QuestionCircle16()` (unknown), `FcFluentIcons.ArrowSync16()` (checking). **There is NO `DismissCircle16()` / no dedicated Danger glyph on `FcFluentIcons`** — for states the curated set doesn't cover (e.g. `provider-status-badge` Failed→Danger) use a FluentUI icon directly (the package is referenced): `new Icons.Regular.Size16.DismissCircle()` or `new Icons.Regular.Size16.ErrorCircle()`. Verify every chosen glyph compiles against the pinned FluentUI package (warnings-as-errors).
    - **`BadgeColor.Important` is valid but has NO in-repo precedent** — the Tenants/Parties badges you mirror use only Success/Informative/Warning/Severe/Subtle/Danger/Brand, never `Important`. Wire `Important` deliberately (it's the readiness "ambiguous/unresolved" role); pair it with `Warning16()` or a FluentUI `QuestionCircle`/`Info` glyph. Do not assume it appears in the template you copy.
  - [x] **`provider-status-badge` mapping (UX-DR21/26):** `ProviderModelStatus.Enabled` + `ProviderConfigurationState.Configured` → `Success`; `ProviderModelStatus.Disabled`/`ProviderConfigurationState.NotConfigured` → `Severe`; `ProviderModelStatus.Degraded` → `Warning`; `ProviderModelStatus.Failed` → `Danger`; a disabled-but-valid historical selection → `Subtle`. Each shows color + icon + a localized whole-string label. **Never** render `ConfigurationReferenceId` as a value beyond a safe "Configured/Not configured" indicator, and never put any provider secret/payload/SDK detail in text, tooltip, `aria-label`, `data-testid`, or copied content (AC3, AD-9, AD-14).

- [x] **Task 6 - Build the Agents overview page** (AC: #2, #5, #6)
  - [x] Add `Components/Pages/AgentsOverview.razor` (`@page "/agents"`, `@attribute [Authorize(Policy = AgentsFrontComposerRegistration.AgentsAdministratorPolicy)]`, the default Agents surface — UX-DR2). Use `FcAggregateListPage`/`FcPageLayout` with `LayoutMode="FcPageLayoutMode.FullWidth"` for the read-heavy readiness/status content (UX-DR15), `HeadingId` + `HeadingTabIndex="-1"` and `FcPageHeader.FocusHeadingAsync()` in `OnAfterRenderAsync` for heading focus on navigation (FC-A11Y).
  - [x] Inject `IAgentSetupGateway`; load via `GetStatusAsync`. Render: `agent-readiness-badge` (callability vs active lifecycle), lifecycle, response mode (`AgentResponseMode`), Provider/model (`SelectedProviderId`/`SelectedModelId` as `{typography.mono}` safe references), the inline activation-blocker list, and tenant callability. **Pending proposal count and recent failures** (UX-DR2) have no contract source in Epic 1 and depend on Epic 2/3 interaction data — render them as an explicit "not available yet" affordance or omit with a documented note; do NOT fabricate counts (see Scope guardrails).
  - [x] Use the `operational-status-panel` grouping idea (Fluent `MessageBar`/status region) to group readiness/blockers **by recovery action** (configure provider, link party identity, fix policy, define content safety) rather than raw subsystem labels (UX-DR9). Reserve stable space for badges/action slots so rows do not shift when state changes (UX-DR17, AC5).

- [x] **Task 7 - Build the Provider catalog grid page** (AC: #3, #5, #6)
  - [x] Add `Components/Pages/ProviderCatalog.razor` (`@page "/agents/providers"`, admin-policy gated). Use `FcAggregateListPage` with `LayoutMode="FcPageLayoutMode.FullWidth"` (UX-DR4/UX-DR15). Inject `IProviderCatalogGateway`; load via `ListEntriesAsync(includeDisabled: true, ...)`.
  - [x] Render a `FluentDataGrid Items="@rows.AsQueryable()"` (sticky header, `ItemKey="@(r => r.ProviderId + "/" + r.ModelId)"`, `ResizableColumns`) with `TemplateColumn`s for: Provider (`ProviderId`, pinned `DataGridColumnPin.Start`, `Sortable` via a static `GridSort<ProviderCatalogEntryView>`), Model (`ModelId`), display label (`DisplayLabel`), enabled/status (`provider-status-badge` from `Status`), capability metadata (`SupportsTextGeneration`, `ContextWindowTokenLimit`, `MaxOutputTokenLimit`, `SafeCapabilityFlags`, `TimeoutPolicy`), readiness/selectable (`IsSelectableForNewActiveUse`), and **secret configured state** (`ConfigurationState` → "Configured"/"Not configured" whole strings — **never** the `ConfigurationReferenceId` value or any secret; AC3, AD-9, AD-10).
  - [x] Render the grid `Body` only when there is data; route loading/empty/filtered-empty/error/permission-denied/stale through the page `States` slot using a surface-state component (mirror `Hexalith.Tenants.UI/Components/.../ListSurfaceStates.razor`: `role="alert"`+`aria-live="assertive"` for errors, `role="status"`+`polite` otherwise; localized title/message; inline reset/refresh). Empty must not leak unauthorized records; filtered-empty must offer a filter reset (UX-DR30, AC6). Grid exposes table semantics, header relationships, sort/filter state, and row-action names (UX-DR34, AC6).

- [x] **Task 8 - Build the `hexa` configuration form + response-mode segmented control** (AC: #4, #5, #6)
  - [x] Add `Components/Pages/AgentConfiguration.razor` (`@page "/agents/configuration"`, admin-policy gated). Use `FcAggregateListPage`/`FcPageLayout` with `LayoutMode="FcPageLayoutMode.Constrained"` (UX-DR15). Inject `IAgentSetupGateway`; load via `GetConfigurationAsync`.
  - [x] Lay out constrained Fluent form sections (`fieldGap {spacing.4}` between related fields, `sectionGap {spacing.6}` between sections — UX-DR16) for: identity (display name, description), instructions (presence/validity/version only — never the instruction text; bind `HasInstructions`/`InstructionsValid`/`InstructionsVersion` from `AgentStatusView`, AD-14), Provider/model (selected refs + provider-status-badge), response mode, approver policy summary (links to Task 9), lifecycle (`agent-readiness-badge` + lifecycle), activation blockers (inline, actionable — UX-DR3), and content-safety state (`HasContentSafetyPolicy`/`ContentSafetyPolicyVersion` + the two mode-override booleans — presence only, never policy content; AC4, 1.7 AC2). Activation is a **distinct action** shown after validity is visible (DESIGN agent-config-form) — the activation **command** dispatch is deferred (this story renders the affordance + blocker state; see Scope).
  - [x] Add `Components/Shared/ResponseModeToggle.razor` using `FluentRadioGroup` + `FluentRadio` inside `<fieldset><legend>` (the project convention for mutually-exclusive choice — `Hexalith.Parties.AdminPortal/Components/CreateEditPartyPage.razor`; there is no bespoke segmented-control component). Two options bound to `AgentResponseMode.Automatic`/`AgentResponseMode.Confirmation`. **Copy must state the change affects future Agent Calls only** (UX-DR23, AC4 — a localized whole string, e.g. key `Agents.Config.ResponseMode.FutureOnlyNote`). Selected chrome may use `{colors.brand-accent}` but must **not** make Automatic look "better" by visual weight (DESIGN response-mode-toggle); never use `Brand` as a status color (UX-DR11).

- [x] **Task 9 - Build the Approver policy builder** (AC: #4, #5, #6)
  - [x] Add `Components/Pages/ApproverPolicy.razor` (`@page "/agents/approver-policy"`, admin-policy gated, `LayoutMode="FcPageLayoutMode.Constrained"`, row-based — UX-DR5/UX-DR15). Render `AgentApproverPolicy` from `AgentStatusView`-adjacent config (note: `AgentStatusView` exposes only `HasApproverPolicy`/`ApproverPolicyDisclosure`/`ApproverPolicyVersion` — the **source rows** themselves come from the configuration read path; for this story render the **presence/disclosure/version** + a structured builder scaffold for the four V1 source kinds, and surface "blocked" when a source is missing/ambiguous).
  - [x] Source-row kinds are exactly the four V1 sources (AD-8, `ApproverPolicySourceKind`): `Caller`, `PredefinedParty` (a safe `PartyId` reference — never Party PII), `TenantRole` (from the local Tenants projection), and `ConversationOwner` (= Conversation **Facilitator** per AD-8 — surface it as the facilitator-resolved authority, not a separate "owner" concept). Each row shows a **readable policy basis** and an **availability state**; missing/ambiguous/unavailable sources render as **blocked** (status-severe/important), not as empty success (UX-DR5, AD-12 fail-closed). Respect the `ApproverPolicyBasisDisclosure` category (UserVisible/OperatorOnly/Redacted/Omitted) when rendering basis text.
  - [x] The approver builder is only required when response mode is Confirmation; when Automatic, show it as not-applicable rather than blocked.

- [x] **Task 10 - Enforce status semantics + surface states + accessibility across all pages** (AC: #5, #6)
  - [x] Apply Fluent **semantic status roles** consistently everywhere (UX-DR11): Success = proven callable/enabled/configured only; Informative = checking/in-progress; Warning = degraded/attention-soon; Severe = blocked/disabled/missing-dependency; Danger = failure/denial; Important = unresolved uncertainty; Subtle = quiet history/non-actionable; Brand = chrome/primary-action only, never a status. Bind to role, never to hex (DESIGN Colors).
  - [x] **Inherit Fluent/FrontComposer typography (UX-DR13), 4px spacing rhythm (UX-DR16), shapes/no-custom-radii (UX-DR19), elevation only for overlays (UX-DR18).** Use `{typography.mono}` for ids/versions/timestamps, `{typography.label}` for field/state labels, `{typography.heading}` for page/panel titles, `{typography.body}` for operational copy.
  - [x] **FC-A11Y (do NOT duplicate shell-owned primitives):** the `FrontComposerShell` already provides skip links (`#fc-main-content`/`#fc-nav`), the `role="main"` landmark, and the nav landmark — `MainLayout` is just `<FrontComposerShell>@Body`. Per page: set `HeadingId`/`HeadingTabIndex="-1"`, call `FcPageHeader.FocusHeadingAsync()` on navigation, add `<FcContentLabel LabelledBy="@HeadingId" />` to name the main landmark. Use a status live-region primitive (mirror `Hexalith.Parties.UI/Components/Shared/StatusLiveRegion.razor` + `StatusPresentation.PolitenessFor(...)`): `polite` for ordinary status, `assertive`/`alert` for errors and permission-denied — **never** assertive for ordinary pending progress (UX-DR36, AC6). `Esc` closes any transient UI without committing and returns focus to the trigger (UX-DR32/37). Reduced-motion users must not depend on animation to perceive state (UX-DR38).
  - [x] **Responsive (UX-DR39/40):** desktop-first. Forms/grids degrade per FrontComposer breakpoints; if a viewport cannot show enough context for a high-impact action, make that action unavailable with a visible reason while read-only review remains (fail-closed — AC6). Setup surfaces are read/config-heavy, so the main fail-closed concern is the (deferred) activation action affordance.
  - [x] **Secret/PII non-disclosure sweep (AC3, AD-14):** audit every `aria-label`, tooltip, `data-testid`, copied text, and rendered string across all pages/badges to confirm no provider secret, raw provider payload, provider SDK type, raw instruction text, content-safety policy content, Party PII, or unrelated-tenant record can appear. Status badges carry no raw content (Architecture "Content" convention).

- [x] **Task 11 - Add the `Hexalith.Agents.UI.Tests` bUnit project + conformance/a11y tests + narrow verification** (AC: #1, #2, #3, #4, #5, #6)
  - [x] Create `tests/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj` (`Microsoft.NET.Sdk.Razor`) referencing `Hexalith.Agents.UI` + FrontComposer `Contracts`/`Shell` (+ `Hexalith.FrontComposer.Testing` if helpers are reused). Add it to `Hexalith.Agents.slnx`. Add central package versions to `Directory.Packages.props`: `<PackageVersion Include="bunit" Version="2.8.4-preview" />` (same as `Hexalith.Tenants/Directory.Packages.props`); reuse the already-central `xunit.v3` `3.2.2`, `Shouldly` `4.3.0`, `NSubstitute` `5.3.0`, `Microsoft.NET.Test.Sdk`, `coverlet.collector`. The shared `tests/Directory.Build.props` already sets `IsTestProject`, the `Xunit` global using, and test `NoWarn` — inherit it.
  - [x] Base setup: inherit `BunitContext`, `JSInterop.Mode = JSRuntimeMode.Loose`, `Services.AddFluentUIComponents()`, register a stub/`NSubstitute` `IStringLocalizer<AgentsResources>` (return the key or a fixed string), substitute `IAgentSetupGateway`/`IProviderCatalogGateway` with NSubstitute, `AddAuthorization()`. Mirror `Hexalith.Tenants.UI.Tests` / `Hexalith.Parties.UI.Tests` harness.
  - [x] **AC1 nav registration + policy-gating tests** (mirror `PartiesUiNavEntryGatingTests`): assert `AgentsFrontComposerRegistration.RegisterDomain` registers the "agents" domain + four ordered entries with the right hrefs/Order; assert the admin entries carry `RequiredPolicy`; render nav with `AddAuthorization().SetAuthorized(...)/SetPolicies(...)` vs `SetNotAuthorized()` and assert gated hrefs appear/are omitted (no leak).
  - [x] **AC2 readiness mapping + badge tests:** `[Theory]` over `AgentStatusView` fixtures asserting the canonical readiness state + `BadgeColor` (callable→Success only when Active + no blockers; Disabled→Severe; MissingPartyIdentity→Severe; provider→Severe; invalid-config→Severe). Assert the badge renders visible text AND `FindComponent<FluentBadge>().Instance.Color`/`.Appearance` (color+text together, not color-only). Assert active-but-blocked never renders Success.
  - [x] **AC3 provider grid + secret-safety tests:** render `ProviderCatalog` with substituted entries (configured + not-configured + disabled + degraded + failed); assert grid columns/semantics, `provider-status-badge` colors, "Configured/Not configured" text, and **assert no rendered markup / `aria-label` / `data-testid` contains any secret/reference value** (feed a sentinel `ConfigurationReferenceId` and assert it is absent from output). Assert empty vs filtered-empty vs permission-denied surface states.
  - [x] **AC4 form + response-mode tests:** assert constrained layout (`data-fc-page-layout="constrained"` via `PageLayoutDeclarationTests` pattern — render in `FrontComposerShell`, `WaitForAssertion`), the `FluentRadioGroup` renders both modes mutually-exclusively, and the future-only note string is present. Assert instruction **text** is never rendered (only presence/validity/version) and content-safety policy content is never rendered.
  - [x] **AC5 status-conformance tests:** color+icon+text present on every status badge; no custom radius/inline hex; status strings resolve through the localizer (whole strings) — assert badges call the localizer rather than concatenating fragments; assert stable badge/action slots (no layout shift) where feasible. Mirror the governance `DomainUiFluentConformanceTests` precedent in the Tenants UI tests.
  - [x] **AC6 a11y tests** (mirror `MainLayoutAccessibilityTests`): skip links are the first two `a[href]`, `role="main"`/nav landmark present with names, heading focus targets have `tabindex="-1"`, live regions carry the right `aria-live` politeness (polite for status, assertive/alert for error + permission-denied), grids expose table semantics + row-action names. Exercise loading/empty/filtered-empty/error/permission-denied/stale states per surface.
  - [x] **Keep all existing guard tests green:** `ProjectReferenceDirectionTests`, `PublicContractPackageBoundaryTests`, `PackageVersionCentralizationTests`, `StructuralSeedConformanceTests`, `ModuleLayout` (a new UI test project is an additive subset — confirm the conformance guards still pass with the added project).
  - [x] Build/test from `Hexalith.Agents/`: `dotnet restore Hexalith.Agents.slnx`; `dotnet build Hexalith.Agents.slnx --configuration Release` (**0 warnings / 0 errors**, warnings-as-errors); then `dotnet test tests/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj --configuration Release` and re-run `tests/Hexalith.Agents.Server.Tests` (boundary/conformance guards) + `tests/Hexalith.Agents.Contracts.Tests`. xUnit v3 + Shouldly + bUnit; PascalCase BDD-style names; no raw `Assert.*`.

## Dev Notes

### Critical Guardrails

- **This story is the FrontComposer/Blazor Admin Setup UI in `Hexalith.Agents.UI`, ONLY** — the navigation registration, the four setup surfaces (Agents overview, `hexa` configuration, Provider catalog grid, Approver policy builder), the `agent-readiness-badge`/`provider-status-badge`, the response-mode segmented control, status semantics, localization, accessibility, and the bUnit conformance/a11y tests. Do NOT implement: the **live read path** (binding the four queries to an EventStore/Dapr read-model, and the `Hexalith.Agents.Client` proxies / BFF/API endpoints) — that is the dedicated Agents read-model + API story (**Epic 4 / 4.1 / 4.3**); the **runnable UI host** (`Program.cs` with `AddHexalithFrontComposerQuickstart`/`AddHexalithDomain`/`AddAuthorizationCore`/`UseRequestLocalization`) and **AppHost runtime topology** — deferred exactly as 1.2–1.7 deferred live dispatch/AppHost; any **command dispatch** from the UI (create/activate/select/configure) — this story renders the affordances + blocker/readiness state, not the write path; the **Conversation invocation / proposal / operational-status / audit surfaces** (Epics 2–4, and `conversation-agent-call` is blocked on PRD OQ-1). Bind the UI to the existing display **contracts** through a UI-side **gateway abstraction** with a deferred placeholder implementation, so the project builds and the components are bUnit-testable now. [Source: epics.md#Story-1.8; ARCHITECTURE-SPINE.md#AD-15; #AD-16-Module-Local-Operational-Topology; #Capability-To-Architecture-Map (Admin UI/API contracts → AD-15/AD-17); Hexalith.Agents/src/Hexalith.Agents/Agent/AgentInspection.cs (read-model binding deferred); 1-7-configure-content-safety-policy-and-activation-gate.md#Critical-Guardrails (deferred-binding convention)]
- **Dependency direction is the load-bearing invariant (AD-15).** `Hexalith.Agents.UI` may reference **only** `Hexalith.Agents.Contracts` and `Hexalith.Agents.Client` in-module, plus out-of-module FrontComposer `Contracts`/`Shell` (source) and the FluentUI package. It must **never** reference `Hexalith.Agents.Server`, the AgentHost, provider adapters, Dapr runtime, or EventStore streams. The UI calls API/BFF/client boundaries, never EventStore streams, provider SDKs, or aggregate internals. UI and API/client share the **same public contracts and authorization outcomes** (UI parity). `ProjectReferenceDirectionTests` already encodes the in-module allow-list (`UI = [Contracts, Client]`); the FrontComposer/FluentUI references are out-of-module and intentionally uncovered by that guard. [Source: ARCHITECTURE-SPINE.md#AD-15-Public-Surface-And-UI-Parity; #Invariants-And-Rules (UI→Client, UI→Contracts); tests/Hexalith.Agents.Server.Tests/ProjectReferenceDirectionTests.cs]
- **Authorization fails closed and is never UI-only (AD-12).** Policy-gated nav (hide/deny) is a usability layer, not authorization — every page also carries `[Authorize(Policy = …)]`, and the real authorization decision is server-side and shared with the API (UI parity). Render explicit not-ready/degraded/blocked/permission-denied states whenever a gateway result is non-success, dependency state is missing/stale/ambiguous/disabled/unavailable, or a projection reports stale/degraded — **never** render uncertain or stale state as ready/fresh. Empty/permission-denied states must not leak or fingerprint unauthorized records. [Source: ARCHITECTURE-SPINE.md#AD-12-Authorization-And-Dependency-Uncertainty; #Consistency-Conventions (Projections: stale must not be rendered as fresh); EXPERIENCE.md#State-Patterns (List And Detail Surfaces)]
- **Secret/PII/content non-disclosure is absolute (AD-14, AD-9).** No provider secret value, secret reference value, raw provider payload, provider SDK type/error, raw Agent Instruction text, Content Safety Policy content, Party PII, or unrelated-tenant record may appear in any rendered text, badge, tooltip, `aria-label`, `data-testid`, copied content, log, or live-region announcement. The Provider catalog shows only **safe normalized capability metadata** (AD-10 field list) + **Configured/Not-configured** (`ProviderConfigurationState`) — never the `ConfigurationReferenceId` value or any secret. The contracts already enforce this (the views expose only safe presence/version/reference fields) — the UI must not re-derive or re-expose anything unsafe. [Source: ARCHITECTURE-SPINE.md#AD-14-Sensitive-Content-And-Secret-Safety; #AD-9-Provider-Adapter-And-Catalog-Boundary; #AD-10-Provider-Capability-Floor; DESIGN.md#Do's-and-Don'ts; EXPERIENCE.md#Accessibility-Floor; Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/AgentStatusView.cs (XML docs)]
- **`agent-readiness-badge` must not collapse active lifecycle and callability (AC2, UX-DR20).** Lifecycle (`AgentLifecycleStatus`) and `ActivationBlockers` are deliberately distinct on `AgentStatusView`: an Agent can be `Active` yet not callable if a dependency is missing/stale, and a non-empty `ActivationBlockers` means "not activatable as configured." Use `BadgeColor.Success` **only** when `Lifecycle == Active` AND `ActivationBlockers` is empty (all required gates pass). The badge must **explain blockers, not hide them** — render the blocker list inline as localized whole strings. [Source: DESIGN.md#agent-readiness-badge; EXPERIENCE.md#State-Patterns (Agent Readiness); epics.md#Story-1.8 AC2; AgentStatusView.cs]
- **Inherit FrontComposer + Fluent; do not invent a design system (UX-DR11–UX-DR19).** Bind status to Fluent **semantic roles** (Success/Informative/Warning/Severe/Danger/Important/Subtle/Brand), never to hex; every status is **color + icon + visible text** (no color-only — UX-DR12); inherit Fluent typography ramps and shapes (no custom radii — UX-DR19); use elevation only for transient overlays (UX-DR18); follow the 4px spacing rhythm (UX-DR16); reserve stable space for badges/action slots so rows do not jump (UX-DR17). All copy is **whole localizable strings with named placeholders** — never runtime-assembled sentence fragments (UX-DR14). `Brand` is chrome/primary-action only, never a status. Never turn `hexa` into a mascot. [Source: DESIGN.md#Colors; #Typography; #Shapes; #Elevation-And-Depth; #Layout-And-Spacing; #Brand-And-Style; ARCHITECTURE-SPINE.md#Consistency-Conventions (UI)]
- **`hexa` is the first configured Agent instance, not a type name** — use it as the configured Agent in copy/ids, never as a class/type identifier. [Source: ARCHITECTURE-SPINE.md#Consistency-Conventions (Naming)]

### Design: Where Each Responsibility Lives (UI binds to existing display contracts; live read path deferred)

```
FrontComposer Shell (owns: skip links, landmarks, nav rendering, theme, request culture)
   │  discovers AgentsFrontComposerRegistration (static Manifest + RegisterDomain) → "agents" domain + 4 policy-gated nav entries
   ▼
Hexalith.Agents.UI (Razor component library; AD-15: → Contracts + Client only)
   Pages: AgentsOverview (/agents, FullWidth) · ProviderCatalog (/agents/providers, FullWidth)
          AgentConfiguration (/agents/configuration, Constrained) · ApproverPolicy (/agents/approver-policy, Constrained)
   Shared: AgentReadinessBadge · ProviderStatusBadge · ResponseModeToggle · surface-state component
   Services/Gateways:  IAgentSetupGateway / IProviderCatalogGateway  ── return existing Contracts views ──┐
                                                                                                          │
   ── DEFERRED (Epic 4 / read-model story): DeferredAgentSetupGateway etc. → Client proxy → BFF/API → read-model ──┘
        (bUnit tests substitute the gateways with NSubstitute; deferred impl never exercised in tests)

Display contracts already shipped (1.3–1.7) — DO NOT redefine:
   AgentStatusView (overview + config)   ← GetAgentStatusQuery / GetAgentConfigurationQuery
   ProviderCatalogEntryView (grid)       ← ListProviderCatalogEntriesQuery / GetProviderCatalogEntryQuery
   AgentInspectionResult / ProviderCatalogInspectionResult (fail-closed wrappers)
```

Why a UI-side gateway with a deferred impl (not a direct Client call now): the query contracts have **no live handler** and `Hexalith.Agents.Client` is an empty shell — the read-model/BFF binding is a dedicated downstream story (architecture: read-model binding "deferred"; Epic 4 owns "stable API/client contracts" 4.1 and "operational status/admin workflows" 4.3). The gateway seam lets the UI render and be fully component-tested now (bUnit substitutes it), while the live wiring lands later behind the same interface — exactly mirroring how 1.2–1.7 kept the DI graph complete with `Deferred*` placeholders. [Source: ARCHITECTURE-SPINE.md#AD-15; #Design-Paradigm (Proj → UI, UI → Client → API); epics.md#Epic-4 (4.1, 4.3); AgentInspection.cs; 1-7-…#Latest-Technical-Information (deferred dispatch convention)]

### Current Code State To Preserve

Read these before editing — they are the patterns to mirror and the contracts to bind to (do not reshape the shipped contracts):

- **`Hexalith.Agents/src/Hexalith.Agents.UI/`** — currently a bare 2-file shell: `Hexalith.Agents.UI.csproj` (`Microsoft.NET.Sdk`, `IsPackable`, refs `Contracts`+`Client` only, no FluentUI/FrontComposer) and `AgentsUIAssemblyMarker.cs`. **No** `.razor`/`_Imports.razor`/`.resx`/Components/Composition exist anywhere in the module yet. This story builds them.
- **`Hexalith.Agents/src/Hexalith.Agents.Client/`** — bare shell (`AgentsClientAssemblyMarker.cs`, refs `Contracts` only). No proxies. Client proxies are deferred (Epic 4); this story does not add them — it adds the UI gateway seam instead.
- **`Hexalith.Agents.Contracts/Agent/AgentStatusView.cs`** — the single safe view for overview + config. Exact constructor order: `AgentId, TenantId, DisplayName, Description?, Lifecycle, ConfigurationVersion, HasInstructions, InstructionsValid, InstructionsVersion, HasPartyIdentity, HasProviderSelection, SelectedProviderId?, SelectedModelId?, ResponseMode, HasApproverPolicy, ApproverPolicyDisclosure, ApproverPolicyVersion, HasContentSafetyPolicy, ContentSafetyPolicyVersion, HasAutomaticContentSafetyOverride, HasConfirmationContentSafetyOverride, IReadOnlyList<AgentActivationBlocker> ActivationBlockers`. **Instruction text / Party PII / provider secrets / policy content are deliberately absent** — bind only the safe presence/validity/version/reference fields.
- **`Hexalith.Agents.Contracts/Agent/`** enums: `AgentLifecycleStatus` (`Unknown=0, Draft, Active, Disabled`); `AgentActivationBlocker` (`Unknown=0, MissingDisplayName, MissingInstructions, InvalidInstructions, MissingPartyIdentity, MissingProviderSelection, ProviderUnavailable, MissingResponseMode, MissingApproverPolicy, ApproverPolicyUnresolvable, MissingContentSafetyPolicy=10`); `AgentResponseMode` (`Unknown=0, Automatic, Confirmation` — XML-doc states changes apply to future Agent Calls only); `ApproverPolicyBasisDisclosure` (`Unknown=0, UserVisible, OperatorOnly, Redacted, Omitted`); `ApproverPolicySourceKind` (`Unknown=0, Caller, PredefinedParty, TenantRole, ConversationOwner`). Result wrappers: `AgentInspectionResult(AgentInspectionStatus Status, AgentStatusView? Agent)` (factories `Success/NotAuthorized/NotFound`; `Agent` null on non-success) + `AgentInspectionStatus` (`Success=0, NotAuthorized, AgentNotFound`).
- **`Hexalith.Agents.Contracts/ProviderCatalog/`** — `ProviderCatalogEntryView(ProviderId, ModelId, DisplayLabel, ProviderModelStatus Status, bool SupportsTextGeneration, int ContextWindowTokenLimit, int MaxOutputTokenLimit, ProviderModelTimeoutPolicy TimeoutPolicy, ProviderModelCapabilityFlags SafeCapabilityFlags, ProviderConfigurationState ConfigurationState, string? ConfigurationReferenceId, bool IsSelectableForNewActiveUse, int CapabilityVersion)`; `ProviderModelStatus` (`Unknown=0, Enabled, Disabled, Degraded, Failed`); `ProviderConfigurationState` (`Unknown=0, NotConfigured, Configured`); `ProviderModelCapabilityFlags` `[Flags]` (`None=0, Streaming=1, ToolCalling=2, Vision=4, StructuredOutput=8`); `ProviderModelTimeoutPolicy(int RequestTimeoutMilliseconds, int MaxRetries)`; `ProviderCatalogInspectionResult(Status, IReadOnlyList<ProviderCatalogEntryView> Entries)` + `ProviderCatalogInspectionStatus` (`Success=0, NotAuthorized, EntryNotFound`).
- **Query contracts (parameterless except where noted):** `Agent/Queries/GetAgentStatusQuery`, `GetAgentConfigurationQuery` (→ `AgentStatusView`); `ProviderCatalog/Queries/ListProviderCatalogEntriesQuery(bool IncludeDisabled)`, `GetProviderCatalogEntryQuery(string ProviderId, string ModelId)`.
- **Reference UI template — `Hexalith.Tenants/src/Hexalith.Tenants.UI/`** (the canonical pattern, "register an Agents domain/nav like Tenants" — AD-15): `Composition/TenantsFrontComposerDomain.cs` + `TenantsFrontComposerRegistration.cs` (static `Manifest`/`RegisterDomain`, `DomainManifest`/`FrontComposerNavEntry`/`IFrontComposerRegistry`, `RequiredPolicy` gating); `Components/Layout/MainLayout.razor` (`<FrontComposerShell>@Body`); `Components/Tenants/TenantDataGrid.razor` (FluentDataGrid + `GridSort`); `Components/.../ListSurfaceStates.razor` (surface states); `Components/Shared/TruthStateBadge.razor` (color+icon+text badge); `Components/Tenants/CreateTenantFlow.razor` (EditForm + FluentInputs + inline validation/focus); `Resources/TenantsResources.{cs,resx,fr.resx}`; `Services/Gateways/`. Secondary: `Hexalith.Parties.UI` (`StatusLiveRegion.razor`/`StatusPresentation`, `PartyStateBadge.razor`, `FluentRadioGroup` form) and `Hexalith.Parties.AdminPortal/Components/CreateEditPartyPage.razor` (fieldset/legend radio group).
- **FrontComposer framework (sibling source, commit `eee5e6b`)** — reuse, don't reinvent: `FcPageLayoutMode {FullWidth, Constrained}` + `FcPageLayout` + `FcAggregateListPage` (slots: Toolbar/HeaderMetadata/Filters/Commands/States/Body/Pager) + `FcPageHeader.FocusHeadingAsync()` + `FcContentLabel`; `FcStatusBadge`/`BadgeSlot`; `FcFluentIcons.*`; `DomainManifest`/`FrontComposerNavEntry`/`IFrontComposerRegistry`/`[BoundedContext("…")]`/`[RequiresPolicy]` in `Hexalith.FrontComposer.Contracts`; the shell `FrontComposerShell.razor` (skip links + landmarks) + `FrontComposerNavigation.razor` (AuthorizeView gating).

What must be preserved: `.slnx` only; `net10.0`, C# 14, nullable, implicit usings, **warnings-as-errors**; **Central Package Management** (add new versions to `Directory.Packages.props`, never inline); the AD-15 dependency direction; provider-SDK-free + secret-value-free + Party-PII-free surfaces; the FluentUI version governed by the FrontComposer baseline (`5.0.0-rc.3-26138.1`); and all existing structural/boundary guard tests green. [Source: ARCHITECTURE-SPINE.md#Stack; #Structural-Seed; Directory.Build.props; Directory.Packages.props; 1-7-…#Current-Code-State-To-Preserve]

### Readiness / Status Mapping (AC2, AC3, AC5)

`agent-readiness-badge` (canonical UX readiness state ← `AgentStatusView`; UX-DR20/25):

| Canonical state | Condition | Fluent role |
|---|---|---|
| callable | `Lifecycle == Active` AND `ActivationBlockers` empty | Success (only here) |
| disabled | `Lifecycle == Disabled` | Severe |
| missing party identity | blockers ∋ `MissingPartyIdentity` | Severe |
| provider unavailable | blockers ∋ `MissingProviderSelection`/`ProviderUnavailable` | Severe |
| invalid configuration | blockers ∋ `MissingDisplayName`/`MissingInstructions`/`InvalidInstructions`/`MissingResponseMode`/`MissingApproverPolicy`/`ApproverPolicyUnresolvable`/`MissingContentSafetyPolicy` | Severe (Important for ambiguous/unresolved) |
| checking | gateway call in flight (UI-only transient; no contract field) | Informative |

`provider-status-badge` (← `ProviderCatalogEntryView`; UX-DR21/26):

| State | Condition | Fluent role |
|---|---|---|
| enabled | `Status==Enabled` AND `ConfigurationState==Configured` | Success |
| not configured | `ConfigurationState==NotConfigured` | Severe |
| disabled | `Status==Disabled` | Severe (Subtle for valid historical) |
| degraded | `Status==Degraded` | Warning |
| failed | `Status==Failed` | Danger |

Every badge: color + icon + a localized whole-string label; accessible name carries no id/secret/PII. [Source: DESIGN.md#agent-readiness-badge; #provider-status-badge; #Colors; EXPERIENCE.md#State-Patterns; AgentStatusView.cs; ProviderCatalogEntryView.cs]

### Latest Technical Information

- **New packages this story (via CPM):** `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1` (production, governed by the FrontComposer baseline — match `Hexalith.Tenants/Directory.Packages.props`) and `bunit` `2.8.4-preview` (test). Test stack otherwise reuses already-central `xunit.v3 3.2.2`, `Shouldly 4.3.0`, `NSubstitute 5.3.0`, `Microsoft.NET.Test.Sdk 18.6.0`, `coverlet.collector`. FrontComposer is referenced as **sibling source projects** (`Hexalith.FrontComposer.Contracts`/`.Shell`/`.Testing`), not packages. Do not add Agent Framework, provider/Dapr/EventStore packages to the UI. [Source: Hexalith.Tenants/Directory.Packages.props; Hexalith.Tenants/src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj; ARCHITECTURE-SPINE.md#Stack]
- **Fluent UI Blazor v5 is inherited** — the Agents UX spine specifies only the Agents-specific semantic delta; bind to Fluent semantic roles and components, do not restyle. Verify the chosen Fluent icon names against the pinned FluentUI package at build (UX icon-vocabulary rule, Tenants precedent). [Source: DESIGN.md#frontmatter; #Colors]
- **Pending proposal count + recent failures (UX-DR2)** and **live `checking`/`ProviderUnavailable`/`ApproverPolicyUnresolvable` verdicts** have no Epic-1 contract source (they need Epic 2/3 interaction data and the live revalidation/read path). Surface them as explicit "not available yet" affordances or omit with a documented note — do not fabricate values. This is a deliberate **UX-DR2-vs-AC2 reconciliation:** the binding acceptance gate is **AC2**, which requires only "readiness, lifecycle, response mode, Provider/model, activation blockers, callability" — none of those two extra UX-DR2 fields. The proposal count + recent failures land with the operational-status surface (**Epic 4.3**) and Epic 2/3 interaction data; their absence here is not missing AC coverage. [Source: epics.md#Story-1.8 AC2 (line 510-513) vs UX-DR2 (line 131); epics.md#Epic-4 (4.3); AgentInspection.cs (pure inspection trusts last verdict; live verdicts come from the deferred revalidation path); EXPERIENCE.md#Open-Questions]

### Testing Requirements

- **Framework:** bUnit `2.8.4-preview` + xUnit v3 + Shouldly + NSubstitute, in a new `tests/Hexalith.Agents.UI.Tests` (`Microsoft.NET.Sdk.Razor`, added to the slnx, `InternalsVisibleTo` from the UI csproj). Harness mirrors `Hexalith.Tenants.UI.Tests`/`Hexalith.Parties.UI.Tests`: `BunitContext`, loose JSInterop, `AddFluentUIComponents()`, stub `IStringLocalizer<AgentsResources>`, NSubstitute gateways, `AddAuthorization()`.
- **Coverage (one per AC):** AC1 nav registration order/hrefs + policy-gated visibility (authorized vs not); AC2 readiness mapping `[Theory]` + badge color+text (active≠callable; Success only when Active+no-blockers); AC3 grid columns/semantics + provider-status colors + **secret-absence assertion** (sentinel reference never in markup/aria/testid) + empty/filtered-empty/permission-denied; AC4 constrained `data-fc-page-layout` + mutually-exclusive radio modes + future-only copy + instruction-text/policy-content never rendered; AC5 color+icon+text conformance + localizer whole-strings (no fragment assembly) + stable slots; AC6 a11y (skip links first, landmarks named, heading `tabindex=-1`, live-region politeness, table semantics) across loading/empty/filtered-empty/error/permission-denied/stale.
- **Guards stay green:** `ProjectReferenceDirectionTests`, `PublicContractPackageBoundaryTests`, `PackageVersionCentralizationTests`, `StructuralSeedConformanceTests`, `ModuleLayout`.
- **Commands (from `Hexalith.Agents/`):**
  - `dotnet restore Hexalith.Agents.slnx`
  - `dotnet build Hexalith.Agents.slnx --configuration Release` (0 warnings / 0 errors)
  - `dotnet test tests/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj --configuration Release`
  - `dotnet test tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj --configuration Release`
  - `dotnet test tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release`

### Project Structure Notes

- New/changed code:
  - `Hexalith.Agents.UI.csproj` (SDK → `Microsoft.NET.Sdk.Razor`; +FrontComposer `Contracts`/`Shell` refs; +FluentUI package; +`InternalsVisibleTo`).
  - `Directory.Packages.props` (+`Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`; +`bunit` `2.8.4-preview`).
  - `Hexalith.Agents.slnx` (+ `tests/Hexalith.Agents.UI.Tests`).
  - `Hexalith.Agents.UI/Components/`: `_Imports.razor`, `Layout/MainLayout.razor`; `Pages/{AgentsOverview,ProviderCatalog,AgentConfiguration,ApproverPolicy}.razor`; `Shared/{AgentReadinessBadge,ProviderStatusBadge,ResponseModeToggle}.razor` + a surface-state component; `Shared/AgentReadiness.cs` mapping helper.
  - `Hexalith.Agents.UI/Composition/`: `AgentsFrontComposerDomain.cs`, `AgentsFrontComposerRegistration.cs`.
  - `Hexalith.Agents.UI/Resources/`: `AgentsResources.cs`, `AgentsResources.resx`, `AgentsResources.fr.resx`.
  - `Hexalith.Agents.UI/Services/Gateways/`: `IAgentSetupGateway.cs`, `IProviderCatalogGateway.cs`, `DeferredAgentSetupGateway.cs`, `DeferredProviderCatalogGateway.cs`, `AgentsUiServiceCollectionExtensions.cs` (`AddAgentsUi`).
  - `tests/Hexalith.Agents.UI.Tests/`: csproj + bUnit tests (one suite per AC) + harness base.
- Discovery loaded: root `epics.md` (Epic 1, Story 1.8 ACs + UX-DR1–UX-DR41 + cross-stories), the UX `DESIGN.md`/`EXPERIENCE.md` spines (component/status/typography/spacing/a11y/l10n/responsive/nav specs + UJ-1), the architecture `ARCHITECTURE-SPINE.md` (AD-8/AD-9/AD-10/AD-12/AD-14/AD-15/AD-16/AD-17/AD-18 + Structural Seed + Stack + Consistency Conventions) and `reviews/review-adversarial-boundaries.md`, the as-built Agents contracts/enums/queries (1.3–1.7), the prior story `1-7-…` (format + deferral convention), and the sibling `Hexalith.Tenants.UI`/`Hexalith.Parties.UI` modules + FrontComposer framework as the implementation template. No root `project-context.md` exists for `agents`; sibling `project-context.md` files supply carry-forward UI conventions (FrontComposer/Fluent inherited, deferred live bindings).

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.8-Admin-Setup-UI-And-Readiness-Overview]
- [Source: _bmad-output/planning-artifacts/epics.md#UX-Design-Requirements (UX-DR1–UX-DR41)]
- [Source: _bmad-output/planning-artifacts/epics.md#Epic-4 (4.1 stable API/client contracts; 4.3 operational status/admin workflows — deferred read path)]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-15-Public-Surface-And-UI-Parity]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-12-Authorization-And-Dependency-Uncertainty]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-14-Sensitive-Content-And-Secret-Safety]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-9-Provider-Adapter-And-Catalog-Boundary]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-10-Provider-Capability-Floor]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-8-Approver-Policy-Resolution]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-16-Module-Local-Operational-Topology]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-17-Contract-And-Test-Gates]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#Structural-Seed]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#Stack]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md#agent-readiness-badge]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md#provider-status-badge]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md#provider-catalog-grid]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md#agent-config-form]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md#response-mode-toggle]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md#Colors]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md#Layout-And-Spacing]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#Information-Architecture]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#State-Patterns]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#Accessibility-Floor]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#FrontComposer-Readiness]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#uj-1---nora-configures-hexa-for-a-tenant-launch]
- [Source: _bmad-output/implementation-artifacts/1-7-configure-content-safety-policy-and-activation-gate.md (story format + deferred-binding convention)]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/AgentStatusView.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/AgentActivationBlocker.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/AgentLifecycleStatus.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/AgentResponseMode.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/AgentInspectionResult.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/Queries/GetAgentStatusQuery.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/Queries/GetAgentConfigurationQuery.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/ProviderCatalog/ProviderCatalogEntryView.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/ProviderCatalog/Queries/ListProviderCatalogEntriesQuery.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.UI/Hexalith.Agents.UI.csproj]
- [Source: Hexalith.Agents/tests/Hexalith.Agents.Server.Tests/ProjectReferenceDirectionTests.cs]
- [Source: Hexalith.Tenants/src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs]
- [Source: Hexalith.Tenants/src/Hexalith.Tenants.UI/Hexalith.Tenants.UI.csproj]
- [Source: Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/Tenants/TenantDataGrid.razor]
- [Source: Hexalith.Parties/src/Hexalith.Parties.UI/Components/Shared/StatusLiveRegion.razor]
- [Source: Hexalith.Parties/src/Hexalith.Parties.AdminPortal/Components/CreateEditPartyPage.razor]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (claude-opus-4-8[1m])

### Debug Log References

- `dotnet restore Hexalith.Agents.slnx` — restored (FluentUI `5.0.0-rc.3-26138.1` + bunit `2.8.4-preview` resolved via CPM).
- `dotnet build Hexalith.Agents.slnx --configuration Release` — **Build succeeded, 0 Warning(s), 0 Error(s)** (warnings-as-errors).
- `dotnet test tests/Hexalith.Agents.UI.Tests` — **56 passed, 0 failed**.
- `dotnet test tests/Hexalith.Agents.Server.Tests` — **93 passed** (boundary/direction/centralization/structural guards green).
- `dotnet test tests/Hexalith.Agents.Contracts.Tests` — **81 passed**.
- `dotnet test tests/Hexalith.Agents.Tests` — **262 passed** (no regressions).
- Fix during dev: FluentDataGrid requires an explicit `Width` on a pinned column — added `Width="200px"` to the pinned Provider column in `ProviderCatalog.razor`.

### Completion Notes List

- Turned `Hexalith.Agents.UI` into a FrontComposer Razor Class Library (`Microsoft.NET.Sdk.Razor`, kept `IsPackable`/`PackageId`), added the FrontComposer `Contracts`/`Shell` project refs + FluentUI package + `InternalsVisibleTo`. Added the FluentUI + bunit central package versions (no inline versions — `PackageVersionCentralizationTests` green).
- Bound the four setup surfaces to the existing display contracts through a UI-side gateway seam (`IAgentSetupGateway`/`IProviderCatalogGateway`) with fail-closed `Deferred*` placeholders + `AddAgentsUi` (scoped). The live read path (Client → BFF/API → read-model) is deferred to Epic 4 (4.1/4.3), mirroring the 1.2–1.7 deferred-binding convention; bUnit substitutes the seam with NSubstitute so the placeholders are never exercised.
- `agent-readiness-badge` keeps active lifecycle and callability distinct (`AgentReadiness.MapState`): Success only when `Lifecycle == Active` AND no blockers; blockers are explained inline (one localized whole string per blocker), grouped by recovery action on the overview. `provider-status-badge` maps `ProviderModelStatus` + `ProviderConfigurationState` to the canonical states; the curated `FcFluentIcons` set has no Danger glyph so Failed uses `Warning16()` with the Danger role + "Failed" text (the FluentUI Icons package is intentionally not referenced — domain modules use only `FcFluentIcons`, like Tenants).
- Provider catalog is a full-width `FluentDataGrid` showing capability/readiness/configured state and never the `ConfigurationReferenceId`/secrets (AC3 sentinel test asserts absence from markup, `aria-label`, `data-testid`, and visible text). Config form is a constrained Fluent layout (presence/validity/version only — never instruction text or content-safety policy content) with a `FluentRadioGroup` response-mode toggle whose copy states changes affect future Agent Calls only. Approver-policy builder renders presence/disclosure/version + a scaffold of the four V1 source kinds and a fail-closed Blocked state in confirmation mode (Not-applicable in automatic mode).
- All setup nav entries + page components are gated by the `Agents.Administrator` policy (nav hiding via `AuthorizeView`; `[Authorize(Policy=…)]` on the pages as defense-in-depth — registering the policy lives in the deferred host `Program.cs`). Pending-proposal count and recent failures (UX-DR2) have no Epic-1 contract source, so they render an explicit "Not available yet" affordance rather than fabricated counts (UX-DR2-vs-AC2 reconciliation; the binding gate is AC2).
- Tests cover one suite per AC (AC1 nav registration + policy-gated visibility; AC2 readiness mapping `[Theory]` + badge color+text; AC3 grid + secret-absence + surface states; AC4 constrained layout + mutually-exclusive radio + future-only note + presence-only; AC5 color+icon+text conformance + localizer whole strings + no inline hex; AC6 skip links/landmarks/heading focus/live-region politeness) plus `AddAgentsUi` composition. Shell-rendering tests reuse `Hexalith.FrontComposer.Testing` (`FrontComposerTestBase`).
- Not implemented by design (Critical Guardrails): the live read path / Client proxies / BFF-API endpoints (Epic 4), the runnable UI host (`Program.cs`/AppHost topology), and any command dispatch (create/activate/select/configure) — this story renders affordances + readiness/blocker state only.

### Change Log

| Date | Change |
|---|---|
| 2026-06-24 | Implemented Story 1.8 Admin Setup UI: FrontComposer Razor library, Agents domain/policy-gated nav, four setup surfaces (overview, configuration, provider catalog, approver policy), readiness/provider status badges, response-mode toggle, surface-state component, localization (en/fr), UI-side gateway seam with deferred placeholders, and a full bUnit conformance/a11y test suite (one per AC). Status → review. |
| 2026-06-24 | Senior Developer Review (AI): adversarial review against all 6 ACs + 11 tasks. Build 0 warnings; UI 156 / Server 93 / Contracts 81 tests pass. Fixed the Approver-policy source rows that asserted an affirmative "Available" availability for dependencies whose state cannot be known without the deferred live read path (rendered with a contradictory "question" icon) — now a fail-closed neutral "not yet evaluated" state (AD-12) with a matching icon/colour/text (AC5) and a regression test. Documented the 4 previously-unlisted test files in the File List. Status → done. |

### File List

New — `Hexalith.Agents/src/Hexalith.Agents.UI/`:
- `Components/_Imports.razor`
- `Components/Layout/MainLayout.razor`
- `Components/Pages/AgentsOverview.razor`
- `Components/Pages/ProviderCatalog.razor`
- `Components/Pages/AgentConfiguration.razor`
- `Components/Pages/ApproverPolicy.razor`
- `Components/Shared/AgentReadiness.cs`
- `Components/Shared/AgentReadinessBadge.razor`
- `Components/Shared/ProviderStatusBadge.razor`
- `Components/Shared/ResponseModeToggle.razor`
- `Components/Shared/AgentSurfaceKind.cs`
- `Components/Shared/AgentSurfaceState.razor`
- `Composition/AgentsFrontComposerDomain.cs`
- `Composition/AgentsFrontComposerRegistration.cs`
- `Resources/AgentsResources.cs`
- `Resources/AgentsResources.resx`
- `Resources/AgentsResources.fr.resx`
- `Services/Gateways/IAgentSetupGateway.cs`
- `Services/Gateways/IProviderCatalogGateway.cs`
- `Services/Gateways/DeferredAgentSetupGateway.cs`
- `Services/Gateways/DeferredProviderCatalogGateway.cs`
- `Services/Gateways/AgentsUiServiceCollectionExtensions.cs`

New — `Hexalith.Agents/tests/Hexalith.Agents.UI.Tests/`:
- `Hexalith.Agents.UI.Tests.csproj`
- `_Imports.razor`
- `AgentsTestContext.cs`
- `StubAgentsLocalizer.cs`
- `RenderedFragmentTextExtensions.cs`
- `CapturingFrontComposerRegistry.cs`
- `NavEntryGatingHarness.razor`
- `AgentUiTestData.cs`
- `AgentsNavigationTests.cs`
- `AgentReadinessMappingTests.cs`
- `BadgeConformanceTests.cs`
- `ProviderCatalogTests.cs`
- `AgentConfigurationTests.cs`
- `AccessibilityTests.cs`
- `AgentsUiCompositionTests.cs`
- `AgentsOverviewTests.cs`
- `ApproverPolicyTests.cs`
- `DeferredGatewayTests.cs`
- `LocalizationResourceTests.cs`

Modified:
- `Hexalith.Agents/src/Hexalith.Agents.UI/Hexalith.Agents.UI.csproj` (SDK → `Microsoft.NET.Sdk.Razor`; +FrontComposer `Contracts`/`Shell` refs; +FluentUI package; +`InternalsVisibleTo`)
- `Hexalith.Agents/Directory.Packages.props` (+`Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.3-26138.1`; +`bunit` `2.8.4-preview`)
- `Hexalith.Agents/Hexalith.Agents.slnx` (+`tests/Hexalith.Agents.UI.Tests`)

Modified during Senior Developer Review (AI):
- `Components/Pages/ApproverPolicy.razor` (per-source availability → fail-closed neutral "not yet evaluated" instead of an affirmative "Available"; AD-12/AC5)
- `Resources/AgentsResources.resx` / `Resources/AgentsResources.fr.resx` (+`Agents.ApproverPolicy.SourceAvailability.Unknown` whole string, en + fr)
- `tests/Hexalith.Agents.UI.Tests/ApproverPolicyTests.cs` (regression assertion: source rows render `Unknown`, never `Available`)

## Senior Developer Review (AI)

**Reviewer:** Administrator (automated adversarial review) · **Date:** 2026-06-24 · **Outcome:** Approve (fixes applied)

**Scope reviewed:** all 6 ACs, all 11 tasks (every `[x]` audited against source), the full File List, git-vs-story reconciliation, secret/PII non-disclosure, status semantics, accessibility, and test quality.

**Verification evidence:**
- `dotnet build Hexalith.Agents.slnx -c Release` → **Build succeeded, 0 Warning(s), 0 Error(s)** (warnings-as-errors).
- `Hexalith.Agents.UI.Tests` → **156 passed / 0 failed**; `Hexalith.Agents.Server.Tests` → **93 passed** (boundary/direction/centralization/structural guards green); `Hexalith.Agents.Contracts.Tests` → **81 passed**.
- FrontComposer `DomainManifest` / `FrontComposerNavEntry` constructor signatures and the four manifest/nav icon strings (`Regular.Size20.Apps/Settings/DevMode/People`) verified resolvable via `FcFluentIcons.TryCreate` against the pinned package.

**Findings & resolution:**
- 🔴 CRITICAL: none. No task marked `[x]` was found undone; every AC has implementation + a passing test mapped to it.
- 🟠 HIGH (fixed): `ApproverPolicy.razor` rendered every approver source row as availability **"Available"** (affirmative, `Informative`) while pairing it with a `QuestionCircle` "unknown" icon. Per-source availability is unknowable in Epic 1 (the live configuration read path is deferred), so an affirmative "Available" renders an unknown dependency as ready — an AD-12 fail-closed violation — and the icon/text disagreed (AC5). Fixed to a neutral **"not yet evaluated"** whole string (en + fr) where icon + colour + text agree; added a regression assertion. The previously dead `SourceAvailability.Blocked` key is retained for the live path.
- 🟡 MEDIUM (fixed): File List omitted four shipped test files (`AgentsOverviewTests`, `ApproverPolicyTests`, `DeferredGatewayTests`, `LocalizationResourceTests`) — added.
- 🟢 LOW (no change, documented): `provider-status-badge` Failed reuses `FcFluentIcons.Warning16()` rather than a dedicated Danger glyph. This matches the canonical `Hexalith.Tenants` `TruthStateBadge` precedent (which reuses `Warning16()` across distinct colour roles); the Danger colour + "Failed" text keep AC5's color+icon+text triple intact. The config-form response-mode toggle is interactive while the write path is deferred (selection is local-only) — consistent with the story's "render affordances" scope.

**Confirmed strengths:** active-lifecycle-vs-callable never collapses (Success only when `Active` + no blockers; theory-tested); AC3 secret-absence proven with a sentinel `ConfigurationReferenceId`; AD-12 nav gating tested for unauthenticated, authenticated-non-admin, and admin; AC6 skip-links/landmarks/heading-focus/live-region politeness all asserted; localization parity (en/fr) enforced for every enum-derived key by `LocalizationResourceTests`.

_Reviewer: Administrator on 2026-06-24_
