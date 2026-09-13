---
name: Hexalith Agents
description: FrontComposer web UI for governed AI participants in Hexalith Conversations. Fluent UI Blazor v5 is inherited; this spine specifies the Agents-specific semantic delta only.
status: final
created: 2026-06-23
updated: 2026-09-12
sources:
  - ../../briefs/brief-agents-2026-06-23/brief.md
  - ../../prds/prd-agents-2026-06-23/prd.md
  - ../../prds/prd-agents-2026-06-23/addendum.md
  - ../../prds/prd-agents-2026-06-23/reconcile-brief.md
  - ../../sprint-change-proposal-2026-08-02.md
  - ../../architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
  - ../../launch-readiness-register.md
  - ../../external-dependency-register.md
  - ../../epics.md
  - ./reconcile-validation-2026-09-08.md
  - ./reconcile-validation-2026-09-09.md
  - ./reconcile-validation-2026-09-09-2.md
  - ./reconcile-validation-2026-09-10.md
  - ./reconcile-validation-2026-09-12.md
  - ../../../../references/Hexalith.Tenants/_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md
  - ../../../../references/Hexalith.FrontComposer/_bmad-output/project-context.md
  - ../../../../references/Hexalith.FrontComposer/docs/reference/components/front-composer-shell.md
  - ../../../../references/Hexalith.FrontComposer/docs/reference/components/navigation.md
  - ../../../../references/Hexalith.FrontComposer/docs/reference/components/datagrid.md
colors:
  status-success:
    note: 'Inherit Fluent BadgeColor.Success. Proven truth only; see Colors.'
  status-informative:
    note: 'Inherit Fluent BadgeColor.Informative. In progress or waiting; see Colors.'
  status-warning:
    note: 'Inherit Fluent BadgeColor.Warning. Attention soon; see Colors.'
  status-severe:
    note: 'Inherit Fluent BadgeColor.Severe. Blocked, not a runtime failure; see Colors.'
  status-danger:
    note: 'Inherit Fluent BadgeColor.Danger. Failure or denial; see Colors.'
  status-important:
    note: 'Inherit Fluent BadgeColor.Important. Uncertain, must resolve before side effects; see Colors.'
  status-subtle:
    note: 'Inherit Fluent BadgeColor.Subtle. Quiet history or non-actionable; see Colors.'
  brand-accent:
    note: 'Inherit the Fluent theme brand tokens as rendered by ButtonAppearance.Primary and radio selection chrome; BadgeColor.Brand for badges only. Chrome and primary action only, never a status.'
typography:
  body:
    note: 'Inherit Fluent / system body ramp.'
  label:
    note: 'Inherit Fluent / system label ramp.'
  heading:
    note: 'Inherit Fluent / system heading ramp. No hero/display scale.'
  caption:
    note: 'Inherit Fluent / system caption ramp.'
  mono:
    note: 'Inherit Fluent / system monospace role for identifiers, versions, and timestamps.'
rounded:
  note: 'Inherit Fluent UI Blazor v5 and FrontComposer shape defaults. Do not invent custom radii.'
spacing:
  '1': 4px
  '2': 8px
  '3': 12px
  '4': 16px
  '6': 24px
  '8': 32px
components:
  agent-readiness-badge:
    color: '{colors.status-success} | {colors.status-informative} | {colors.status-severe} | {colors.status-important} | {colors.status-subtle}'
    appearance: 'FluentBadge BadgeAppearance.Tint; Filled for Severe'
    typography: '{typography.label}'
    gap: '{spacing.1}'
  provider-status-badge:
    color: '{colors.status-success} | {colors.status-warning} | {colors.status-severe} | {colors.status-danger} | {colors.status-important} | {colors.status-subtle}'
    appearance: 'FluentBadge BadgeAppearance.Tint; Filled for Danger and Severe'
    typography: '{typography.label}'
    gap: '{spacing.1}'
  proposal-state-badge:
    color: '{colors.status-success} | {colors.status-informative} | {colors.status-warning} | {colors.status-severe} | {colors.status-danger} | {colors.status-important} | {colors.status-subtle}'
    appearance: 'FluentBadge BadgeAppearance.Tint; Filled for Danger and Severe'
    typography: '{typography.label}'
    gap: '{spacing.1}'
  response-mode-toggle:
    base: 'FluentRadioGroup with two FluentRadio options'
    selected: '{colors.brand-accent}'
    gap: '{spacing.2}'
  agent-config-form:
    base: 'FrontComposer / Fluent form primitives inside FcAggregateDetailPage'
    layout: 'Constrained'
    fieldGap: '{spacing.4}'
    sectionGap: '{spacing.6}'
  approver-policy-builder:
    base: 'Fluent form controls plus policy-source rows with FluentButton move up/down'
    layout: 'Constrained; row-based policy sources'
    rowGap: '{spacing.3}'
  provider-catalog-grid:
    base: 'Hand-authored FluentDataGrid inside FcAggregateListPage'
    pinnedColumns: 'Split by reader tier; see the provider-catalog-grid section'
    rowPadding: '{spacing.3}'
  proposal-queue-grid:
    base: 'Hand-authored FluentDataGrid inside FcAggregateListPage'
    pinnedColumns: 'Proposal state, Source Conversation, caller, approver responsibility, expiry, age'
    rowPadding: '{spacing.3}'
  proposal-editor:
    base: 'FluentTextArea editor region with metadata and action rail'
    layout: 'Constrained; stacked; editor is the primary region'
    padding: '{spacing.4}'
  version-history:
    base: 'FluentRadioGroup version list inside a FluentAccordionItem'
    timestamp: '{typography.mono}'
    rowGap: '{spacing.2}'
  conversation-agent-call:
    base: 'Conversation-owned Call hexa FluentButton opening a FluentDialog prompt surface'
    selected: '{colors.brand-accent}'
    gap: '{spacing.2}'
  agent-response-marker:
    base: 'Inline FluentBadge rendered beside a posted hexa Conversation Message'
    color: '{colors.status-subtle}'
    appearance: 'FluentBadge BadgeAppearance.Tint'
    typography: '{typography.caption}'
    gap: '{spacing.1}'
  conversation-context-policy-panel:
    base: 'Read-only definition list inside FluentAccordion'
    layout: 'Constrained; effective policy is the primary region'
    sectionGap: '{spacing.4}'
  content-safety-policy-editor:
    base: 'Fluent form primitives inside FluentAccordion'
    layout: 'Constrained; current effective policy is the primary region'
    sectionGap: '{spacing.4}'
  cost-control-editor:
    base: 'FluentNumberInput and FluentSelect controls with FluentBadge consumption rows inside FluentAccordion'
    layout: 'Constrained; current caps and consumption are the primary region'
    sectionGap: '{spacing.4}'
  launch-readiness-panel:
    base: 'FluentDataGrid of gate records plus FluentAccordion for metric and evidence groups'
    layout: 'FullWidth; blockers precede passing records'
    sectionGap: '{spacing.4}'
  audit-governance-panel:
    base: 'Fluent form and FluentBadge status primitives inside FluentAccordion'
    layout: 'Constrained; retention and hold state is the primary region'
    sectionGap: '{spacing.4}'
  proposal-notification:
    base: 'Domain-rendered count in the Agents overview page body readiness summary, wrapped by a domain-named label'
    color: '{colors.status-informative}'
    appearance: 'FluentBadge BadgeAppearance.Tint'
    gap: '{spacing.2}'
  operational-status-panel:
    base: 'FluentBadge rows grouped by recovery; FluentMessageBar for page-level notices only'
    padding: '{spacing.4}'
    itemGap: '{spacing.3}'
  audit-evidence-panel:
    base: 'Definition list and FluentBadge rows on the detail; hand-authored FluentDataGrid for the change-and-inspection evidence list; FluentMessageBar for page-level notices only'
    timestamp: '{typography.mono}'
    reference: '{typography.mono}'
    padding: '{spacing.4}'
    rowPadding: '{spacing.3}'
  high-impact-confirmation:
    base: 'Focus-trapped FluentDialog'
    layout: 'Constrained dialog; one localized body string; cancel receives initial focus'
    confirm: 'FluentButton ButtonAppearance.Primary using {colors.brand-accent}, never the default button'
    decline: 'Second affirmative FluentButton at default appearance, never Primary and never a destructive red; DataHandlingAcceptance only'
    padding: '{spacing.4}'
  pending-command-indicator:
    base: 'Activated FluentButton with DisabledFocusable="true" plus adjacent FluentBadge status text'
    color: '{colors.status-informative} | {colors.status-severe}'
    appearance: 'FluentBadge BadgeAppearance.Tint'
    typography: '{typography.label}'
    gap: '{spacing.2}'
---

## Brand & Style

Hexalith Agents is a governed operational tool, not a chat novelty and not a marketing surface. It should feel like the rest of the Hexalith admin ecosystem: calm, precise, dense enough for repeated use, and explicit about what the system knows before it allows side effects. The first visible Agent is `hexa`, but the visual system does not turn `hexa` into a mascot. The product signal is named, attributable AI participation inside Conversations.

This is a FrontComposer inheritance spec. Hexalith Agents inherits the FrontComposer shell and Microsoft Fluent UI Blazor v5, pinned as `Microsoft.FluentUI.AspNetCore.Components` `5.0.0-rc.5-26219.1` and owned by `Directory.Packages.props`. Every inherit-by-name role below is verified against that pin at build and re-verified when the pin changes. There is no bespoke palette, type ramp, shadow language, or custom shape system. The delta is semantic: Agent readiness, Provider readiness, proposal state, approval state, version history, posting outcome, and audit evidence need consistent meaning so administrators and approvers never confuse a draft with a Conversation Message.

Icons come from the curated inline-SVG `FcFluentIcons` factory only, never from the Fluent icons NuGet. The factory has two access paths and they are not interchangeable.

- **String contract**, `FcFluentIcons.TryCreate("Variant.SizeNN.Name", out icon)`, used by nav-entry registration. It accepts exactly twelve names: `Regular.Size16.Play`, `Regular.Size20.Apps`, `Regular.Size20.ChevronRight`, `Regular.Size20.DevMode`, `Regular.Size20.Search`, `Regular.Size20.Settings`, `Regular.Size20.Navigation`, `Regular.Size20.BuildingPeople`, `Regular.Size20.People`, `Regular.Size20.PersonBoard`, `Regular.Size32.DocumentSearch`, `Regular.Size48.DocumentSearch`. `DevMode20` renders the Fluent `DeveloperBoard` glyph: one glyph under two names, and `DeveloperBoard` is not a `TryCreate` name.
- **Typed 16 px factories**, methods only and not reachable through `TryCreate`: `Checkmark16`, `CheckmarkCircle16`, `DismissCircle16`, `InfoCircle16`, `SubtractCircle16`, `QuestionCircle16`, `Warning16`, `ArrowSync16`, `Star16`, `Edit16`, `Eye16`, `Key16`, `Copy16`. Every status role below binds to one of these, so no status badge waits on a FrontComposer request.

A glyph the curated set lacks becomes a FrontComposer request; it is never silently replaced by a nearby glyph. The same rule governs any shell capability the curated set lacks: it is requested, never worked around. Requests that are not glyphs are recorded where the constraint bites rather than in the table below — the **nav-entry `RequiredPolicy` duplication request** is a registration-contract request and is filed in `EXPERIENCE.md § Information Architecture`, cross-indexed on the Policy-gated nav row of `EXPERIENCE.md § FrontComposer Readiness`.

Nav-entry glyph requests (string contract only):

| Nav entry | Glyph today | Request |
|---|---|---|
| Overview | `Regular.Size20.Apps` | none |
| `hexa` configuration | `Regular.Size20.Settings` | none |
| Provider catalog | `Regular.Size20.DevMode` | none |
| Approver policy | `Regular.Size20.People` | none |
| Proposal queue | `Regular.Size20.Navigation` | inbox or queue |
| Operational status | `Regular.Size20.Search` | none |
| Conversation context policy | none | document |
| Content safety policy | none | shield |
| Cost controls | none | budget |
| Launch readiness | none | launch |
| Audit evidence | none | history |
| Audit governance | none | history |

Until a request lands, the nav entry keeps the glyph listed under "Glyph today". **There is no no-glyph path**: `FrontComposerNavigation.ResolveNavEntryIcon` falls back to `Apps20` whenever `TryCreate` fails, including for an unset key, and `Apps20` is the Agents overview glyph — so the six entries listed with no glyph today render a duplicate of Overview, and in the icon-only rail seven Agents tiles are distinguishable only by their localized `aria-label`. The duplication is accepted until the requests land rather than papered over, because the alternative would be to hold those entries out of navigation entirely. What the spine forbids is a *deliberate* reuse of another entry's glyph. Audit evidence and Audit governance therefore render label-only until the history glyph lands, rather than borrowing Operational status's `Search`.

**There is no mockup, wireframe, or other composition reference to look for.** By decision (2026-08-02, reaffirmed 2026-09-09), all 15 Information Architecture surfaces are spine-only: FrontComposer and Fluent v5 inheritance plus the per-surface primary-region and accordion tables below make layout derivable, and a visual reference would restate inherited chrome. These two spines are the sole reference, and they win on conflict with any mock, sketch, import, or shipped code.

The surface is governed operational UX, not regulated UX. Provider secrets are never displayed or entered, authorization failures are plain and safe, proposed replies stay visually distinct from Conversation Messages, and version and audit evidence is legible without compliance-heavy language beyond source requirements.

## Colors

Colors inherit Fluent semantic roles by name. Bind meaning to role, never to hex. `{colors.brand-accent}` is chrome and eligible primary action only; it is not a state color.

- `{colors.status-success}` means proven: authoritatively callable Agent readiness at the current `RegistryRevision`, `Ready / Callable / None` Provider readiness within `ValidUntil`, `Posted`, current passing evidence, or a `ProjectionConfirmed` completed operation.
- `{colors.status-informative}` means in progress or waiting: `Submitted`, `AuthoritativePending`, `awaiting projection`, `catching up`, generating, `Pending`, `Edited`, `Regenerated`, `Approved`, `PostingPending`, capacity queued, checking.
- `{colors.status-warning}` means attention soon: nearing expiry, `Degraded / Callable / NonBlockingOperationalWarning`, 80% budget consumption, a zero-priced model, a latency warning defined by launch policy.
- `{colors.status-severe}` means blocked but not a runtime failure: disabled Agent or Provider/model, tenant `Suspended`, `Expired`, **`ExpiredWhileSuspended`**, **`EntryMissing`**, `active, not proven callable`, context blocked, safety blocked, budget blocked, **rate limited**, capacity rejected, **`NoEligibleApprover`**, **conversation blocked (`RemovedInConversations`)**, **`MirrorRefused`**, **`DataHandlingAcceptanceLapsed`**, **`TriggerReviewOverdue`**, unconfigured cap or rate limit, `Stale` evidence, missing Party identity, unavailable dependency, pending in another session.
- `{colors.status-subtle}` also carries lifecycle **`Draft`** — provisioned, not yet valid for activation — which is never Success and never a blocker.
- `{colors.status-danger}` means failure or denial: denied, generation failed, `PostingFailed`, `Rejected`, Provider error, superseded by another decision.
- `{colors.status-important}` means uncertain and must resolve before side effects: `authority unresolved`, ambiguous Party identity, missing or outdated policy basis, unknown outcome with a held reservation, a pricing currency mismatch under the named `ProviderReadinessReasonCode` **`CurrencyMismatch`**, incomplete restrictive deletion confirmation.
- `{colors.status-subtle}` means quiet history or non-actionable: `Abandoned`, **`LateConfirmed`** recorded beside a `Posted` proposal, disabled but valid historical option, read-only inspection, existence only, deferred metric, no activity, and `Unknown` Provider readiness rendered before Story 5.5 ships `ProviderReadinessResult`.

`Stale` is reserved for readiness or Provider evidence whose `ValidUntil` has passed. Projection lag is `catching up` in `{colors.status-informative}` and never borrows the `Stale` role.

Contrast targets are WCAG 2.2 AA: 4.5:1 for text, 3:1 for non-text and focus indicators, in Light, Dark, and forced-colors. Inherited Fluent role pairs are assumed AA at the pinned package and re-verified when the pin changes; the Agents-owned axe lane in `EXPERIENCE.md § Accessibility Floor` proves it per route. In forced-colors the OS may override badge fill, so icon plus text carry the meaning alone.

No-color-only is mandatory. Every status renders as a Fluent semantic role plus one glyph plus visible whole-string text and an accessible name. The glyph is decorative whenever the visible text is present: the badge's accessible name is the text alone, and `FluentBadge.IconLabel` is not set, so an assistive technology never reads "checkmark circle, Posted". `BadgeAppearance.Tint` is the calm default; `BadgeAppearance.Filled` is reserved for Danger and Severe as emphasis and does not count toward no-color-only. Provider logos and model brand colors never carry status.

One glyph per role, from the `FcFluentIcons` 16 px factory methods. Every role is satisfied today; none is a FrontComposer request.

| Role | Glyph |
|---|---|
| `{colors.status-success}` | `FcFluentIcons.CheckmarkCircle16()` |
| `{colors.status-informative}` | `FcFluentIcons.ArrowSync16()` |
| `{colors.status-warning}` | `FcFluentIcons.Warning16()` |
| `{colors.status-severe}` | `FcFluentIcons.SubtractCircle16()` |
| `{colors.status-danger}` | `FcFluentIcons.DismissCircle16()` |
| `{colors.status-important}` | `FcFluentIcons.QuestionCircle16()` |
| `{colors.status-subtle}` | `FcFluentIcons.InfoCircle16()` |

`FluentMessageBar` carries only Success, Warning, Error, and Info, so it is restricted to page-level notices. Per-item state uses `FluentBadge`, which carries the full role palette.

## Typography

Typography inherits Fluent. Use `{typography.heading}` for page and panel titles, `{typography.body}` for operational copy and preview text, `{typography.label}` for fields and state labels, `{typography.caption}` for metadata, and `{typography.mono}` for identifiers, projection ids and versions, `RegistryRevision`, `CapabilityVersion`, pricing versions, and timestamps.

Generated reply content may be longer and more prose-like than the surrounding admin UI, but it uses the inherited body role inside a bounded preview or editor region. No editorial display typography for AI text; it would make generated content feel more authoritative than it is.

Every visible string is a localizable whole string with named placeholders; the resource type, key convention, parity gate, and formatting rules are in `EXPERIENCE.md § Voice and Tone`.

## Layout & Spacing

One `FcPageLayoutMode` per route; hybrid measures do not exist in FC-LYT.

- FullWidth: Agents overview, Provider catalog, Proposal queue, Operational status, Launch readiness, Audit evidence list. The Audit evidence list carries **two regions, not one**. Id entry serves the Conversation-derived classes, which declare no list contract: it takes **five** reference kinds — interaction, proposal, governance-operation, Conversation, and inspection-case — and routes to the authorized detail. Its visible label names all five and its `aria-describedby` format hint states how the kinds are distinguished, since one input accepting five heterogeneous formats is otherwise unusable. The route **additionally carries the enumerable, tenant-scoped change-and-inspection evidence list** for the two classes that hold no Conversation content, and per `EXPERIENCE.md § Information Architecture` the Agents grid rules govern that list, so it is a **fourth `FluentDataGrid`** and not a special case. There are therefore four grids: the Agents-owned required **filter** component set (FC-TBL itself defines a frozen public surface and generated-grid envelope rules, not a usage mandate) applies to the Provider catalog, Proposal queue, and Audit evidence lists; **`FcExpandInRowDetail` additionally governs the Launch readiness gate grid**, which is read-only with no filters and carries its own `agents.launch-readiness` view key and localized detail-panel label. The audit list's per-grid values — view key, localized `DetailPanelAriaLabel`, 320 CSS px surviving column set, default sort, and page size — are carried by `EXPERIENCE.md § Grid rules` and `§ Audit evidence rules`, which enumerate all four grids; they are not invented here.
- Constrained: `hexa` configuration, Approver policy, Conversation context policy, Content safety policy, Cost controls, Proposal detail/editor, Audit evidence detail, Audit governance. Evidence lists for cost and governance live on the FullWidth status and audit routes.

Spacing follows the Fluent-compatible 4px rhythm in frontmatter: `{spacing.4}` between related fields, `{spacing.6}` between major sections, `{spacing.8}` only for page-level separation. Each step inherits by name rather than by value, so the scale binds like every other token group: `{spacing.1}`–`{spacing.8}` are `--spacingHorizontal`/`--spacingVerticalXS` through `XXL` on the Fluent 2 ramp, and a gap between siblings is expressed as a `FluentStack` `HorizontalGap`/`VerticalGap` in preference to a hand-authored `gap`. Per `hexalith-ux-instructions.md § No theme redefinition`, hand-authored CSS is allowed only for layout the design system does not own; since `src/Hexalith.Agents.UI` currently ships **no CSS at all** behind roughly 115 BEM class names, that inventory is a tracked divergence in `EXPERIENCE.md § Shipped-code corrections` with a named delivery decision and absorbing stories. No decorative card grids; dense tables, forms, panels, and inline status regions do the work.

A route with two or more sibling titled content sections uses one `FluentAccordion` with one `FluentAccordionItem` per section, the primary item expanded. Page title, breadcrumbs, toolbar, navigation chrome, and the single primary content region stay outside the accordion; the primary content is never hidden behind an accordion interaction. Accordion titles are h2, in-panel sections h3.

| Surface | Primary region (outside the accordion) | Accordion items |
|---|---|---|
| Agents overview | Readiness summary | Blockers; Recent activity |
| Provider catalog | The grid | Editor |
| Proposal detail | The editor | Metadata; Version history; Audit link |
| Conversation context policy | The current effective policy (read-only; no control of any kind) | History |
| Content safety policy | The current effective policy pair | Tenant stricter delta; Platform draft and publication; History |
| Cost controls | Current caps, limits, and consumption | Lower-only editor; Operator editor; Overrides; History. **The Operator editor and Overrides items are absent for a Tenant Agent Administrator**, not rendered with hidden buttons |
| `hexa` configuration | The configuration form | Instructions; Provider and model with its data-handling record and `DataHandlingVersion` acceptance; response mode and Approver policy; expiry and regeneration ceiling; lifecycle and activation |
| Approver policy | The source list | Validation results; pending proposals by prior policy version |
| Operational status | Readiness and outcomes grouped by recovery | Blocked calls; Denials; Per-Conversation history and block; Cost consumption; Failure records |
| Launch readiness | The gate grid | Kill switch; Trigger review; NFR-14 evidence; Consumed dependencies |
| Audit governance | Hold and retention state | Export; Deletion; Pending approvals; History. **The Pending approvals item renders expanded whenever its count is non-zero**, and its title carries that count |
| Audit evidence list | The id-entry form for the five reference kinds | Change and inspection evidence (the fourth grid). The persistent `UnreviewedInspection` signal and the inspection rate render beside the primary region, never inside the accordion, because a signal that must stay visible cannot sit behind an accordion interaction |
| Audit evidence detail | The evidence record | Versions; Safety decisions; Governance changes |

Layout keeps focus reachable and mirrors correctly: sticky headers, pinned columns, and action rails never overlap the focused row, so scroll padding equals the sticky heights; rails, pinned columns, and version lists use logical `inline-start` and `inline-end` for RTL locales. Reflow, target size, and the other WCAG criteria are in `EXPERIENCE.md § Accessibility Floor`.

Reserve stable space for status badges, action slots, expiry labels, and pending indicators so rows do not jump when a proposal changes state or a call moves from generating to posting.

## Elevation & Depth

Inherit Fluent and FrontComposer. Elevation exists for transient overlays, popovers, and the focus-trapped `high-impact-confirmation` dialog. It never implies audit certainty or proposal importance; hierarchy comes from layout, labels, state badges, and section grouping.

## Shapes

Inherit Fluent shapes. No custom radii for Agent surfaces. Status badges and chips follow Fluent badge shapes; forms, editors, panels, dialogs, and grids follow FrontComposer and Fluent defaults.

## Components

Each section is the visual binding only. Behavior, inputs, and events are in the matching `EXPERIENCE.md § Component Patterns` row.

The 22 components are named and ordered identically in three places: this file's `components:` frontmatter, the `###` sections below, and `EXPERIENCE.md § Component Patterns`. A component added, renamed, or reordered in one must be changed in all three, or the pair stops working as one contract.

The Agents-owned live-region carrier `AgentsPageStatusRegion` is **infrastructure outside that 22-component contract and is not a component entry below**: it renders no visible text, badge, border, or spacing, so it has no visual specification to bind here; its one rule is behavioral and lives in `EXPERIENCE.md § Live regions`.

### agent-readiness-badge

`FluentBadge` with `{typography.mono}` chips beside it for `EnvironmentProfile`, gate-set name, matrix version, and `RegistryRevision`. `callable` renders `{colors.status-success}`; `checking` renders `{colors.status-informative}`; `active, not proven callable` and `stale` render `{colors.status-severe}`; `authority unresolved` renders `{colors.status-important}`. Lifecycle `active` is a separate `{colors.status-subtle}` chip. Production enablement is a separate indicator, Success only when `RQ-1` records READY.

### provider-status-badge

`FluentBadge` for the `ProviderReadinessResult` triple. `Ready / Callable / None` is `{colors.status-success}`; `Degraded / Callable / NonBlockingOperationalWarning` is `{colors.status-warning}`; `Blocked / Blocked / <ReasonCode>` is `{colors.status-severe}` or `{colors.status-danger}` by failure class; a pricing currency mismatch is the named reason code **`CurrencyMismatch`** in `{colors.status-important}`; disabled but valid historical selections and the interim `Unknown` are `{colors.status-subtle}`; past `ValidUntil` is `Stale` in `{colors.status-severe}`. **`EntryMissing`** is a `Blocked / Blocked` reason code in `{colors.status-severe}` and renders in this badge only, and only for the Agent's currently-selected entry or an in-flight interaction's previously-snapshotted entry; no other surface renders it, and a terminal historical record never does — see `version-history` and `audit-evidence-panel`.

### proposal-state-badge

`FluentBadge` for the ten `ProposedAgentReplyState` values (the PRD alias `PostFailed` maps to `PostingFailed`). The **nearing-expiry chip carries visible text**, not color and a glyph alone: `Expires in {duration}`, culture-formatted, with `Warning16` decorative beside it, because this file's own rule is that every status renders as a semantic role plus one glyph plus visible whole-string text, and in forced-colors the icon and text carry the meaning by themselves. The announced string is worded separately from the chip and both live in `EXPERIENCE.md § Voice and Tone`. Only `Posted` is `{colors.status-success}`; `Pending`, `Edited`, `Regenerated`, `Approved`, and `PostingPending` are `{colors.status-informative}`; `PostingFailed` and `Rejected` are `{colors.status-danger}`; `Expired` is `{colors.status-severe}`; `Abandoned` is `{colors.status-subtle}`; nearing expiry adds a `{colors.status-warning}` chip. When nearing expiry begins is in `EXPERIENCE.md § Proposal lifecycle`. Display-only outcomes (`superseded by another decision`, `authority unresolved`, `expired at approval`, `duplicate submission (idempotent)`, `existence only`, **`LateConfirmed`** in `{colors.status-subtle}` beside `Posted`, and **`ExpiredWhileSuspended`** in `{colors.status-severe}` beside `Expired`) render as a second badge beside the state and never replace it.

The **retry window has the same chip as expiry, on the same terms**: `{colors.status-warning}`, `Warning16` decorative, and visible whole-string text carrying the remaining duration, whose copy and its separately worded announcement are in `EXPERIENCE.md § Voice and Tone`. The remaining time renders **on the proposal detail itself**, not only inside the retry confirmation, because an Approver who never opens that dialog would otherwise lose the exit with no signal. When the retry clock is paused — the Agent `Disabled` or the tenant `Suspended` — the chip **renders as paused rather than counting down**: it shows no decreasing duration and no relative label while `PausedDuration` accrues, and the expiry chip beside it keeps counting, because `ExpiresAt` never pauses. An `Approved` proposal held under either stop condition renders a **waiting, not posting** treatment that is visibly distinct from `PostingPending`: both keep `{colors.status-informative}` and its one role glyph, and the distinction is carried by the whole-string state text plus a second `{colors.status-severe}` chip naming the stop condition — the same second-badge device the display-only outcomes use. `PostingPending` never carries that chip.

### response-mode-toggle

`FluentRadioGroup` with Automatic Response Mode and Confirmation Response Mode as equal-weight options. Selection chrome uses `{colors.brand-accent}`; neither option gets more visual weight. The radio group edits a draft value only; a separate **Apply** `FluentButton`, `DisabledFocusable` until the draft differs from the published mode, opens the confirmation. Helper text carries the future-only effect line.

### agent-config-form

Constrained form inside `FcAggregateDetailPage`, with a domain-supplied `FcPageHeader` above it. Field inventory per the EXPERIENCE row; blockers render inline beside the field in `{colors.status-severe}`. Enable and Disable are distinct `FluentButton`s in the action rail, rendered after validity is visible, both `AgentActivation`, both using `pending-command-indicator`; only the action that would change the current lifecycle state is present. Variants: `Submitted`, `AuthoritativePending`, `ProjectionConfirmed`, `awaiting projection`, read-only for non-administrators.

### approver-policy-builder

Row-based builder for the closed `ApproverPolicySourceKind` list; the `ConversationOwner` value is always labeled Conversation Facilitator. Each row shows its basis, disclosure category, and availability. Row order changes through move up/down `FluentButton`s. Those buttons carry **row-identity names** rather than bare direction labels, and render `DisabledFocusable` — never absent — at the first and last row, so the control set never changes shape under the keyboard as a row travels. They are the sole reorder mechanism, so no pointer-only drag affordance substitutes for them. Their names, focus behavior, and announcement are in `EXPERIENCE.md § Approver policy rules`. **Add source** is a single `FluentButton` below the last row, appending a row whose kind is chosen in the row itself and not in a dialog; **Remove** is a per-row `FluentButton` carrying the row identity in its name on the same terms as the reorder buttons, never a bare repeated label. Neither is `ButtonAppearance.Primary`: both edit the draft only, and Publish is the write.

The basis control differs by kind. `PredefinedParty` renders a **tenant-scoped search-and-select** over Parties — a search field with a suggestion list, **never a dropdown enumerating the tenant's Party inventory and never a free-text Party id** — storing the immutable Party id and rendering the resolved display name in the row. `TenantRole` renders a `FluentSelect` over the tenant role list on the existing Tenants read contract, never typed and never free text. `ConversationOwner`, always labeled Conversation Facilitator, carries **no basis control at all**, so its row shows a basis of none rather than an empty picker. A basis that no longer resolves renders `{colors.status-severe}` with its typed reason rather than a blank, and while the two read contracts are absent both basis controls render the `{colors.status-subtle}` deferred treatment with the row `DisabledFocusable`. Rows that fail validation render `{colors.status-severe}` with the reason; nothing renders as empty success.

### provider-catalog-grid

Hand-authored `FluentDataGrid` inside `FcAggregateListPage`, FullWidth. Pinned columns are **split by reader tier**, because configured state is Platform-Operator-only and per-tenant enablement is a first-class column. A **tenant reader** sees Provider name, model, **per-tenant enablement**, `provider-status-badge`, pricing version, and `CapabilityVersion`; a secret-derived readiness reason collapses to one undifferentiated `Blocked` for that reader, and platform configuration problems surface solely as the `PlatformNotReady` readiness code. A **Platform Operator** additionally sees platform enabled state, configured state, the masked `ConfigurationReferenceId`, and the data-handling record with its `DataHandlingVersion`. Identifiers render in `{typography.mono}`. The editor `FluentAccordionItem` uses an ISO 4217 `FluentSelect` for currency, `FluentNumberInput` for unit prices and capability limits, and a masked input with autocomplete off for the secret reference. The pricing version is server-assigned and read-only; the confirmation renders it as `next pricing version {n}`. Authorization behavior is in `EXPERIENCE.md` § Provider catalog rules; its visual consequence is the **`read-only (no mutation policy)`** variant, in which the editor `FluentAccordionItem` is itself absent rather than present with hidden buttons, so the field inventory and the secret-reference control are never rendered. Variants: loading, empty, filtered-empty, error, `catching up`, `superseded by another decision`, `read-only (no mutation policy)`, and **`tenant-scoped read`**, in which the Platform-Operator-only columns are absent rather than blank.

### proposal-queue-grid

Hand-authored `FluentDataGrid` inside `FcAggregateListPage`, FullWidth. Pinned columns: `proposal-state-badge`, Source Conversation, caller, approver responsibility, expiry as an absolute culture-formatted timestamp plus static relative label, age. Status cells show a visible text column plus glyph, never `FcStatusIcon` alone. At 320 CSS px the surviving column set is `proposal-state-badge`, expiry, and the open action; the remaining columns move into `FcExpandInRowDetail`. The grid scrolls inside its own container under the WCAG 1.4.10 data-table exception; the page itself never scrolls horizontally. Variants: loading, empty, filtered-empty, error, permission-denied, `catching up`.

### proposal-editor

Constrained, stacked workspace whose primary region is a `FluentTextArea` holding the selected version, labeled generated or human-edited with author and timestamp. Metadata, `version-history`, and audit link are `FluentAccordionItem`s. The action rail renders as `FluentButton`s, its membership and availability per `EXPERIENCE.md § Proposal editor action rail`; Approve is the only `ButtonAppearance.Primary` button and uses `pending-command-indicator` when activated. Once `Approved` is projection-confirmed, Edit, Regenerate, and Approve are absent and the editor pins the approved version read-only. A proposed reply never looks like a Conversation Message: no message bubble, no avatar treatment. Variants: dirty, regeneration ceiling reached, `authority unresolved`, `stale proposal`, terminal read-only, `existence only` (no content rendered), and **`PostingFailed`**, in which the rail renders the nearing-retry-window chip and the remaining time beside the state, and `administrative retry` renders as an ordinary rail `FluentButton` with `pending-command-indicator` when activated — never as `ButtonAppearance.Primary`, which this file reserves for Approve and which is absent from this state — and is absent, not disabled, when the server verdict withholds it.

### version-history

`FluentRadioGroup` inside a `FluentAccordionItem`, one radio per version labeled with kind, author, and timestamp in `{typography.mono}`. Approval and posting markers sit beside the version they identify; the policy basis renders per its disclosure category. The Provider and model on a **terminal** interaction's version rows render from that interaction's **immutable snapshot**, in `{typography.mono}`, whether or not the catalog entry still exists: a terminal record never renders **`EntryMissing`**, a catalog-existence state, or an absent-entry placeholder, and it never degrades to `not available` because the entry is gone. `EntryMissing` belongs to `provider-status-badge` alone.

### conversation-agent-call

The Conversation-owned **Call hexa** `FluentButton`, contributed through `EXT-CONV-UI-1`, opening a focus-trapped `FluentDialog` with the Agent name, effective response mode, a required prompt `FluentTextArea`, and Submit. In **automatic** response mode the dialog additionally renders the **irreversibility line** above Submit, in `{colors.status-severe}`, because that mode posts to the durable Conversation record with no approval step between the call and the post; its string is in `EXPERIENCE.md § Voice and Tone`. When `hexa` is not callable the button stays visible, `aria-disabled`, with the safe blocker text. Status renders as `FluentBadge` rows **in the persistent contributed region beside Call hexa, never inside the dialog body**: `submitted`, `authoritative pending`, and the terminal outcome. The dialog closes on Submit, so a badge rendered inside it would be destroyed at the moment it has something to say.

### agent-response-marker

Inline `FluentBadge` in `{colors.status-subtle}` beside every posted `hexa` Conversation Message, whose two copy strings live in `EXPERIENCE.md § Voice and Tone`, one for automatic posts and `AI-generated, edited by {party}` when the posted version was human-edited. Rendered by Conversations, contributed by Agents.

### conversation-context-policy-panel

Read-only definition list, Constrained; the effective rule is the primary region and the Safe Context Budget components, declared behaviors, and block reason are items. No control of any kind.

### content-safety-policy-editor

Constrained `FluentAccordion`; the current effective policy is primary. Always-blocked categories render as a fixed read-only list; restricted categories render as editable rows; draft, validation, and Publish use `high-impact-confirmation` and `pending-command-indicator`, with Publish `DisabledFocusable` until the required **Security approval reference** is present. Each restricted row carries a `FluentSelect` over the three values `blocked`, `restricted`, and `permitted`, visibly labeled with its category name, never a radio group, which would move selection on the first arrow key across a governed value; selecting `restricted` or `permitted` reveals the **required use-case `FluentTextArea`** on that row, whose submit-time error renders as a whole string beside the field. On the tenant editor the **looser options are absent from the select** rather than present and disabled, so the surface never renders a relaxation it would refuse. Until the three-value set and the use-case field have a contract, the rows render read-only in the `{colors.status-subtle}` deferred treatment rather than a control with nowhere to write. Category lists are in the EXPERIENCE row. Variants: **`tenant delta only`**, in which the platform draft and publication item is absent rather than present and disabled, and the editor renders no control that could move a category from blocked to restricted.

### cost-control-editor

Constrained `FluentAccordion`; current caps and consumption are primary. `FluentNumberInput` fields with explicit units, currency shown by a `FluentSelect`; an unconfigured value renders `{colors.status-severe}`. The surface carries **two editors, not one**, matching its two authorization tiers: an Operator editor for initial configuration and raises, and a tenant lower-only editor. Consumption rows are `FluentBadge`s, one per reservation disposition; 80% is `{colors.status-warning}` and 100% is `{colors.status-severe}`. The dispositions and the basis both figures are evaluated on are in the `cost-control-editor` row of `EXPERIENCE.md § Component Patterns`. The audited override is a separate item carrying its numeric ceiling and expiry, lapsing at the earlier of the two; while one is active, Operational status shows it with remaining amount and remaining time. Variants: `Submitted`, `AuthoritativePending`, `ProjectionConfirmed`, `awaiting projection`, override active, **`read-only (lower-only)`** — the Operator editor and the Overrides item are **absent**, not present with hidden buttons, so the field inventory never leaks — and **`raise refused`**, which renders the submit-time validation error carrying the current boundary rather than silently clamping the typed value.

### launch-readiness-panel

FullWidth `FluentDataGrid` of gate records, blockers first, identifiers in `{typography.mono}`; `FluentAccordionItem`s group the **kill switch**, the **trigger review**, NFR-14 evidence, and consumed dependencies, matching the per-surface accordion table. Blocker chips render in visually distinguishable provenance classes, with a visible qualifier on the PRD-declared ones; the classes, their codes and the per-chip field sets are in `EXPERIENCE.md § Launch readiness rules`. The **Trigger review** item is where the kill-switch trigger review is read: it renders the four rates against their thresholds, the distinct-calling-Party denominators, the convening floor, the scaled minimum sample, and the decision validity, as labelled rows with the same visible whole-string treatment every status on this panel uses. A **Record trigger review** `FluentButton` sits inside that item, and a **posture-recording control** renders beside the posture field; both are specified on the same visual terms — an ordinary `FluentButton`, never `ButtonAppearance.Primary`, **absent** rather than disabled for a reader without its constant, and `pending-command-indicator` when activated. The rates, the constants that gate each control, and the confirmation contents are in the recording-acts table of `EXPERIENCE.md § Launch readiness rules` and are not restated here. Production enablement is **not** a control on this panel: it renders as an indicator only, and `EXPERIENCE.md § Agent readiness` states why. The single UX-owned NFR-14 statement is in the EXPERIENCE row; thresholds and seams are per `launch-readiness-register.md § NFR-14 Browser Monotonic Timing Contract` and are never restated here.

### audit-governance-panel

Constrained `FluentAccordion`; retention and legal-hold state is primary, with legal hold, export, and deletion as items. Each governance write carries a required justification `FluentTextArea` inside its confirmation. Export and deletion render `DisabledFocusable` with `authority unresolved` treatment naming `EXT-SECRETS-1` while that dependency is not `Available`, and deletion is additionally blocked while a matching legal hold is active, or while its recorded second-party approval is absent — a third `DisabledFocusable` cause with its own reason string, which hold release and export carry too. The staging and its tokens are in `EXPERIENCE.md § Audit governance rules`. Export rows show manifest state, artifact expiry, and download availability; the panel never renders artifact contents or keys. Partial failure renders `{colors.status-important}` or `{colors.status-severe}`.

A **Pending approvals** `FluentAccordionItem` carries the second-party half of hold release, export, and deletion. **Its title carries the count** of requests awaiting this viewer's approval as one whole string, and the item renders **expanded whenever that count is non-zero**, so it is seen without being looked for; the count is never rendered in the shell nav rail, on the same terms `proposal-notification` states. Each pending row shows requester, scope and counts, and time requested, and carries one **Approve** `FluentButton` — ordinary appearance, never `ButtonAppearance.Primary` — using `pending-command-indicator` when activated. Where the server refuses the act to this viewer, the typed refusal reason renders **in place of the control** rather than as a disabled button, so the refusal is legible instead of an inert button. No Decline control renders. While the pending-approval read contract is absent the item renders the `{colors.status-subtle}` deferred treatment rather than an empty list. Authorities, staging, and confirmation contents are in `EXPERIENCE.md § Audit governance rules`.

### proposal-notification

A domain-rendered `FluentBadge` in `{colors.status-informative}` in the **Agents overview page body's readiness summary**, policy-gated, with the whole-string label `{count} proposals pending approval`. It is not in the shell nav link: `FrontComposerNavigation` exposes no `[Parameter]`, so there is no seam by which the domain renders inside a nav entry. The shell nav count stays suppressed; why, and what would change it, is in the matching EXPERIENCE row. Zero renders no badge, so there is no `.Zero` plural key. Placement, source, and empty state are in `EXPERIENCE.md § Agents overview rules`.

### operational-status-panel

FullWidth `FluentBadge` rows grouped by recovery action. The per-Conversation rows carry the **block and clear command controls**, each opening a `high-impact-confirmation` and then a `pending-command-indicator`, plus the Conversation Agent State badge and the `MirrorPending` / `MirrorRefused` flags. Deferred metrics render as a `{colors.status-subtle}` whole-string label. Every authoritative row shows projection id and version in `{typography.mono}`. The **denial rows in the Denials item carry the Confirm unauthorized event control**: an ordinary `FluentButton`, never `ButtonAppearance.Primary`, **absent** rather than disabled for a reader without its constant, opening a `high-impact-confirmation` and then a `pending-command-indicator`. It sits on the denial row because the row is the evidence being confirmed.

**The counts and the rows are one arrangement, not two.** The primary region outside the accordion is a **six-group recovery index of counts** — one group per recovery action, each count linking into the accordion item that holds its rows — and the accordion items carry the **subject detail rows**, each row rendering **its recovery group as a visible label** so a row read in isolation still names its recovery. Nothing in the primary region is other than a count, and nothing in an accordion item is other than a row. Which class is counted in which group, which item holds its rows, and the subjects that contribute rows and no count are in `EXPERIENCE.md § Operational status rules`. Variants: `opaque Conversation reference` — a reference and a count only, for a viewer without current Conversation read access — and `MirrorRefused`, which renders the two remediation controls.

### audit-evidence-panel

Two visual shapes on one route. The **detail** is a definition list plus `FluentBadge` rows; timestamps and references use `{typography.mono}`. The **change-and-inspection evidence list** is the fourth hand-authored `FluentDataGrid` inside `FcAggregateListPage`, FullWidth, `{spacing.3}` row padding, bound by the same grid visual rules as the other three: status cells carry visible text plus glyph and never `FcStatusIcon` alone, identifiers and timestamps render in `{typography.mono}`, the grid scrolls inside its own container under the WCAG 1.4.10 data-table exception while the page never scrolls horizontally, and stable space is reserved for status and action slots. The `UnreviewedInspection` signal and the inspection rate render outside the accordion beside the id-entry form, per the per-surface table. Its view key, localized `DetailPanelAriaLabel`, surviving column set at 320 CSS px, default sort, and page size are owed by `EXPERIENCE.md § Grid rules` and are not set here. Evidence for a **terminal** interaction renders its Provider and model from the immutable snapshot plus authorized Audit Evidence, never as **`EntryMissing`** and never as `not available` on the ground that the catalog entry is gone. Never displays secrets, raw payloads, unrelated tenant data, or stack traces. Variants: loading, empty, filtered-empty, error, permission-denied, `catching up`.

### high-impact-confirmation

Focus-trapped `FluentDialog`. Confirm is `FluentButton` `ButtonAppearance.Primary` in `{colors.brand-accent}` and is never the default button. The body is a `FluentDialogBody` with `FixedHeaderFooter="true"` and a focusable scroll region, so Confirm and Cancel stay reachable at 320 CSS px even when the body renders a full proposal version. Padding `{spacing.4}`, no destructive red button. Focus placement, focus restoration, Esc wiring, and body contents are behavior; they are in the matching EXPERIENCE row and `EXPERIENCE.md` § Confirmation contents by family.

Variant **`DataHandlingAcceptance`**, the one family in the roster carrying **two affirmative actions** rather than one. The footer renders three `FluentButton`s in one row: **Accept** is the single `ButtonAppearance.Primary` confirm in `{colors.brand-accent}`; **Decline** is a second affirmative at default appearance, never Primary and never a destructive red, because declining blocks the model for the tenant immediately and this file's rule is that the consequential action is never the default button; **Cancel** keeps initial focus and commits nothing. Decline is distinguished from Cancel by its own whole-string label and by a `{colors.status-severe}` consequence line in the body naming `DataHandlingAcceptanceLapsed` as the state a decline produces, so the two are never told apart by position alone. Both affirmatives use `pending-command-indicator` when activated, and both are `DisabledFocusable` while the required justification is empty. The body renders the named `DataHandlingVersion` and the four record fields in `{typography.mono}` where they are identifiers, and for a declared tightening change the recorded field-level diff; the live grace renders as `{colors.status-warning}` with its countdown, not as a blocker. Contents are in `EXPERIENCE.md` § Confirmation contents by family.

### pending-command-indicator

The activated `FluentButton` switches to `DisabledFocusable="true"`; `Loading` and `Disabled` are forbidden on an activated control because both remove focusability. An adjacent `FluentBadge` in `{colors.status-informative}` reads `Submitted`, then `AuthoritativePending`, then `awaiting projection`. Variant `pending in another session` renders `{colors.status-severe}` on resolution controls in a second tab. It never becomes Success.

## Do's and Don'ts

| Do | Don't |
|---|---|
| Inherit FrontComposer and Fluent visual defaults at the pinned package | Invent a custom Agents design system or a v4/FAST token |
| Make proposed replies visually distinct from Conversation Messages | Style generated draft content as if it has been posted |
| Use Success only for callable, Posted, current-pass, or ProjectionConfirmed states | Use Success for lifecycle active, Submitted, AuthoritativePending, Approved, or PostingPending |
| Render Provider Degraded as Warning and obey `Callability` | Infer callability from enabled or degraded labels |
| Show `catching up` for projection lag | Borrow `Stale`, which means evidence past `ValidUntil` |
| Label the Conversation-authority source Conversation Facilitator | Show or imply a Conversation owner anywhere |
| Label `Agents.AuditOperator` Compliance Inspector on every surface, exactly as `ApproverPolicySourceKind.ConversationOwner` is labeled Conversation Facilitator | Render either contract identifier as its own visible label |
| Render a terminal interaction's Provider and model from its immutable snapshot | Render `EntryMissing` or `not available` on history because the catalog entry is gone |
| Leave every pricing field empty until typed and pin the pricing version column | Default price or currency, or hide a zero price |
| Accept a secret reference only, masked, operator-visible | Accept, echo, or display a secret value |
| Resolve a Party basis through a tenant-scoped search returning only Parties the requester may read | Enumerate the tenant's Party inventory in a dropdown, or accept a free-text Party id |
| Reflow every route to 320 CSS px and keep actions operable | Lock out approval at narrow widths or high zoom |
| Take icons from `FcFluentIcons` and request missing glyphs | Reuse a nearby glyph or pull the icons NuGet |
| Use `FluentRadioGroup` for mode and version choice, with an explicit Apply for mode | Open a confirmation on a radio change, which is the first arrow key |
| Name the v5 binding (`ButtonAppearance.Primary`, `DisabledFocusable`) | Reference a v4 name such as `Appearance.Accent`, or use `Disabled`/`Loading` where focus must survive |
| Keep EN/FR keys at parity as whole strings | Assemble sentences from fragments or reuse shell dialogs with unlocalized literals |
