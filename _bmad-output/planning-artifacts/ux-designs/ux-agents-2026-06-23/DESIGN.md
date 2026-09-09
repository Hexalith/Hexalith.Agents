---
name: Hexalith Agents
description: FrontComposer web UI for governed AI participants in Hexalith Conversations. Fluent UI Blazor v5 is inherited; this spine specifies the Agents-specific semantic delta only.
status: final
created: 2026-06-23
updated: 2026-09-09
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
    pinnedColumns: 'Provider name, model, enabled state, configured state, readiness, pricing version, CapabilityVersion'
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
    base: 'Domain-rendered count inside the Agents overview link, wrapped by a domain-named label'
    color: '{colors.status-informative}'
    appearance: 'FluentBadge BadgeAppearance.Tint'
    gap: '{spacing.2}'
  operational-status-panel:
    base: 'FluentBadge rows grouped by recovery; FluentMessageBar for page-level notices only'
    padding: '{spacing.4}'
    itemGap: '{spacing.3}'
  audit-evidence-panel:
    base: 'Definition list and FluentBadge rows; FluentMessageBar for page-level notices only'
    timestamp: '{typography.mono}'
    reference: '{typography.mono}'
    padding: '{spacing.4}'
  high-impact-confirmation:
    base: 'Focus-trapped FluentDialog'
    layout: 'Constrained dialog; one localized body string; cancel receives initial focus'
    confirm: 'FluentButton ButtonAppearance.Primary using {colors.brand-accent}, never the default button'
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

A glyph the curated set lacks becomes a FrontComposer request; it is never silently replaced by a nearby glyph.

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

Until a request lands, the nav entry keeps the glyph listed under "Glyph today" or renders without a glyph; it never reuses another entry's glyph. Audit evidence and Audit governance therefore render label-only until the history glyph lands, rather than borrowing Operational status's `Search`.

The surface is governed operational UX, not regulated UX. Provider secrets are never displayed or entered, authorization failures are plain and safe, proposed replies stay visually distinct from Conversation Messages, and version and audit evidence is legible without compliance-heavy language beyond source requirements.

## Colors

Colors inherit Fluent semantic roles by name. Bind meaning to role, never to hex. `{colors.brand-accent}` is chrome and eligible primary action only; it is not a state color.

- `{colors.status-success}` means proven: authoritatively callable Agent readiness at the current `RegistryRevision`, `Ready / Callable / None` Provider readiness within `ValidUntil`, `Posted`, current passing evidence, or a `ProjectionConfirmed` completed operation.
- `{colors.status-informative}` means in progress or waiting: `Submitted`, `AuthoritativePending`, `awaiting projection`, `catching up`, generating, `Pending`, `Edited`, `Regenerated`, `Approved`, `PostingPending`, capacity queued, checking.
- `{colors.status-warning}` means attention soon: nearing expiry, `Degraded / Callable / NonBlockingOperationalWarning`, 80% budget consumption, a zero-priced model, a latency warning defined by launch policy.
- `{colors.status-severe}` means blocked but not a runtime failure: disabled Agent or Provider/model, `Expired`, `active, not proven callable`, context blocked, safety blocked, budget blocked, capacity rejected, unconfigured cap or rate limit, `Stale` evidence, missing Party identity, unavailable dependency, pending in another session.
- `{colors.status-danger}` means failure or denial: denied, generation failed, `PostingFailed`, `Rejected`, Provider error, superseded by another decision.
- `{colors.status-important}` means uncertain and must resolve before side effects: `authority unresolved`, ambiguous Party identity, missing or outdated policy basis, unknown outcome with a held reservation, pricing currency mismatch, incomplete restrictive deletion confirmation.
- `{colors.status-subtle}` means quiet history or non-actionable: `Abandoned`, disabled but valid historical option, read-only inspection, existence only, deferred metric, no activity, and `Unknown` Provider readiness rendered before Story 5.5 ships `ProviderReadinessResult`.

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

- FullWidth: Agents overview, Provider catalog, Proposal queue, Operational status, Launch readiness, Audit evidence list. The Audit evidence list is an id-entry surface, not a grid: it takes an interaction, proposal, or governance-operation reference and routes to the detail. The mandatory FC-TBL component set therefore applies to the Provider catalog and Proposal queue grids only.
- Constrained: `hexa` configuration, Approver policy, Conversation context policy, Content safety policy, Cost controls, Proposal detail/editor, Audit evidence detail, Audit governance. Evidence lists for cost and governance live on the FullWidth status and audit routes.

Spacing follows the Fluent-compatible 4px rhythm in frontmatter: `{spacing.4}` between related fields, `{spacing.6}` between major sections, `{spacing.8}` only for page-level separation. No decorative card grids; dense tables, forms, panels, and inline status regions do the work.

A route with two or more sibling titled content sections uses one `FluentAccordion` with one `FluentAccordionItem` per section, the primary item expanded. Page title, breadcrumbs, toolbar, navigation chrome, and the single primary content region stay outside the accordion; the primary content is never hidden behind an accordion interaction. Accordion titles are h2, in-panel sections h3.

| Surface | Primary region (outside the accordion) | Accordion items |
|---|---|---|
| Agents overview | Readiness summary | Blockers; Recent activity |
| Provider catalog | The grid | Editor |
| Proposal detail | The editor | Metadata; Version history; Audit link |
| Policy surfaces | The current effective policy | Draft; Authoring; History |

Layout keeps focus reachable and mirrors correctly: sticky headers, pinned columns, and action rails never overlap the focused row, so scroll padding equals the sticky heights; rails, pinned columns, and version lists use logical `inline-start` and `inline-end` for RTL locales. Reflow, target size, and the other WCAG criteria are in `EXPERIENCE.md § Accessibility Floor`.

Reserve stable space for status badges, action slots, expiry labels, and pending indicators so rows do not jump when a proposal changes state or a call moves from generating to posting.

## Elevation & Depth

Inherit Fluent and FrontComposer. Elevation exists for transient overlays, popovers, and the focus-trapped `high-impact-confirmation` dialog. It never implies audit certainty or proposal importance; hierarchy comes from layout, labels, state badges, and section grouping.

## Shapes

Inherit Fluent shapes. No custom radii for Agent surfaces. Status badges and chips follow Fluent badge shapes; forms, editors, panels, dialogs, and grids follow FrontComposer and Fluent defaults.

## Components

Each section is the visual binding only. Behavior, inputs, and events are in the matching `EXPERIENCE.md § Component Patterns` row.

The 22 components are named and ordered identically in three places: this file's `components:` frontmatter, the `###` sections below, and `EXPERIENCE.md § Component Patterns`. A component added, renamed, or reordered in one must be changed in all three, or the pair stops working as one contract.

### agent-readiness-badge

`FluentBadge` with `{typography.mono}` chips beside it for `EnvironmentProfile`, gate-set name, matrix version, and `RegistryRevision`. `callable` renders `{colors.status-success}`; `checking` renders `{colors.status-informative}`; `active, not proven callable` and `stale` render `{colors.status-severe}`; `authority unresolved` renders `{colors.status-important}`. Lifecycle `active` is a separate `{colors.status-subtle}` chip. Production enablement is a separate indicator, Success only when `RQ-1` records READY.

### provider-status-badge

`FluentBadge` for the `ProviderReadinessResult` triple. `Ready / Callable / None` is `{colors.status-success}`; `Degraded / Callable / NonBlockingOperationalWarning` is `{colors.status-warning}`; `Blocked / Blocked / <ReasonCode>` is `{colors.status-severe}` or `{colors.status-danger}` by failure class; a pricing currency mismatch is `{colors.status-important}`; disabled but valid historical selections and the interim `Unknown` are `{colors.status-subtle}`; past `ValidUntil` is `Stale` in `{colors.status-severe}`.

### proposal-state-badge

`FluentBadge` for the ten `ProposedAgentReplyState` values (the PRD alias `PostFailed` maps to `PostingFailed`). Only `Posted` is `{colors.status-success}`; `Pending`, `Edited`, `Regenerated`, `Approved`, and `PostingPending` are `{colors.status-informative}`; `PostingFailed` and `Rejected` are `{colors.status-danger}`; `Expired` is `{colors.status-severe}`; `Abandoned` is `{colors.status-subtle}`; nearing expiry adds a `{colors.status-warning}` chip. When nearing expiry begins is in `EXPERIENCE.md § Proposal lifecycle`. Display-only outcomes (`superseded by another decision`, `authority unresolved`, `expired at approval`, `duplicate submission (idempotent)`, `existence only`) render as a second badge beside the state and never replace it.

### response-mode-toggle

`FluentRadioGroup` with Automatic Response Mode and Confirmation Response Mode as equal-weight options. Selection chrome uses `{colors.brand-accent}`; neither option gets more visual weight. The radio group edits a draft value only; a separate **Apply** `FluentButton`, `DisabledFocusable` until the draft differs from the published mode, opens the confirmation. Helper text carries the future-only effect line.

### agent-config-form

Constrained form inside `FcAggregateDetailPage`, with a domain-supplied `FcPageHeader` above it. Field inventory per the EXPERIENCE row; blockers render inline beside the field in `{colors.status-severe}`. Enable and Disable are distinct `FluentButton`s in the action rail, rendered after validity is visible, both `AgentActivation`, both using `pending-command-indicator`; only the action that would change the current lifecycle state is present. Variants: `Submitted`, `AuthoritativePending`, `ProjectionConfirmed`, `awaiting projection`, read-only for non-administrators.

### approver-policy-builder

Row-based builder for the closed `ApproverPolicySourceKind` list; the `ConversationOwner` value is always labeled Conversation Facilitator. Each row shows its basis, disclosure category, and availability. Row order changes through move up/down `FluentButton`s. Rows that fail validation render `{colors.status-severe}` with the reason; nothing renders as empty success.

### provider-catalog-grid

Hand-authored `FluentDataGrid` inside `FcAggregateListPage`, FullWidth. Pinned columns: Provider name, model, enabled state, configured state, `provider-status-badge`, pricing version, `CapabilityVersion`, in `{typography.mono}` where they are identifiers. The editor `FluentAccordionItem` uses an ISO 4217 `FluentSelect` for currency, `FluentNumberInput` for unit prices and capability limits, and a masked input with autocomplete off for the secret reference. The pricing version is server-assigned and read-only; the confirmation renders it as `next pricing version {n}`. Mutation controls are absent, not disabled, for an administrator without the platform-scoped mutation policy. Variants: loading, empty, filtered-empty, error, `catching up`, `superseded by another decision`.

### proposal-queue-grid

Hand-authored `FluentDataGrid` inside `FcAggregateListPage`, FullWidth. Pinned columns: `proposal-state-badge`, Source Conversation, caller, approver responsibility, expiry as an absolute culture-formatted timestamp plus static relative label, age. Status cells show a visible text column plus glyph, never `FcStatusIcon` alone. At 320 CSS px the surviving column set is `proposal-state-badge`, expiry, and the open action; the remaining columns move into `FcExpandInRowDetail`. The grid scrolls inside its own container under the WCAG 1.4.10 data-table exception; the page itself never scrolls horizontally. Variants: loading, empty, filtered-empty, error, permission-denied, `catching up`.

### proposal-editor

Constrained, stacked workspace whose primary region is a `FluentTextArea` holding the selected version, labeled generated or human-edited with author and timestamp. Metadata, `version-history`, and audit link are `FluentAccordionItem`s. The action rail holds Edit, Regenerate, Approve, Reject, Abandon, and Retry posting as `FluentButton`s; Approve is the only `ButtonAppearance.Primary` button and uses `pending-command-indicator` when activated, and Regenerate carries `DisabledFocusable` plus a local pending badge while a generation is in flight. Once `Approved` is projection-confirmed, Edit, Regenerate, and Approve are absent and the editor pins the approved version read-only. A proposed reply never looks like a Conversation Message: no message bubble, no avatar treatment. Variants: dirty, regeneration ceiling reached, `authority unresolved`, `stale proposal`, terminal read-only, `existence only` (no content rendered).

### version-history

`FluentRadioGroup` inside a `FluentAccordionItem`, one radio per version labeled with kind, author, and timestamp in `{typography.mono}`. Approval and posting markers sit beside the version they identify; the policy basis renders per its disclosure category.

### conversation-agent-call

The Conversation-owned **Call hexa** `FluentButton`, contributed through `EXT-CONV-UI-1`, opening a focus-trapped `FluentDialog` with the Agent name, effective response mode, a required prompt `FluentTextArea`, and Submit. When `hexa` is not callable the button stays visible, `aria-disabled`, with the safe blocker text. Status renders as `FluentBadge` rows: `submitted`, `authoritative pending`, and the terminal outcome.

### agent-response-marker

Inline `FluentBadge` in `{colors.status-subtle}` beside every posted `hexa` Conversation Message, reading `AI-generated from Conversation Context, not human-verified` for automatic posts and `AI-generated, edited by {party}` when the posted version was human-edited. Rendered by Conversations, contributed by Agents.

### conversation-context-policy-panel

Read-only definition list, Constrained; the effective rule is the primary region and the Safe Context Budget components, declared behaviors, and block reason are items. No control of any kind.

### content-safety-policy-editor

Constrained `FluentAccordion`; the current effective policy is primary. Always-blocked categories render as a fixed read-only list; restricted categories render as editable rows; draft, validation, and Publish use `high-impact-confirmation` and `pending-command-indicator`. Category lists are in the EXPERIENCE row.

### cost-control-editor

Constrained `FluentAccordion`; current caps and consumption are primary. `FluentNumberInput` fields with explicit units, currency shown by a `FluentSelect`; an unconfigured value renders `{colors.status-severe}`. Consumption rows are `FluentBadge`s for settled spend, outstanding reservations, and held unknown-outcome reservations; 80% is `{colors.status-warning}`, 100% is `{colors.status-severe}`. The audited override is a separate item carrying its numeric ceiling and expiry; while one is active, Operational status shows it with remaining amount and remaining time. Variants: `Submitted`, `AuthoritativePending`, `ProjectionConfirmed`, `awaiting projection`, override active.

### launch-readiness-panel

FullWidth `FluentDataGrid` of gate records, blockers first, identifiers in `{typography.mono}`; `FluentAccordionItem`s group metric cohorts, sample sufficiency, and evidence references. The single UX-owned NFR-14 statement is in the EXPERIENCE row; thresholds and seams are per `launch-readiness-register.md § NFR-14 Browser Monotonic Timing Contract` and are never restated here.

### audit-governance-panel

Constrained `FluentAccordion`; retention and legal-hold state is primary, with legal hold, export, and deletion as items. Each governance write carries a required justification `FluentTextArea` inside its confirmation. Export and deletion render `DisabledFocusable` with `authority unresolved` treatment naming `EXT-SECRETS-1` while that dependency is not `Available`, and deletion is additionally blocked while a matching legal hold is active. Export rows show manifest state, artifact expiry, and download availability; the panel never renders artifact contents or keys. Partial failure renders `{colors.status-important}` or `{colors.status-severe}`.

### proposal-notification

A domain-rendered `FluentBadge` in `{colors.status-informative}` inside the Agents overview link, policy-gated, with the whole-string label `{count} proposals pending approval`. The shell nav count stays suppressed; why, and what would change it, is in the matching EXPERIENCE row.

### operational-status-panel

FullWidth `FluentBadge` rows grouped by recovery action. Deferred metrics render as a `{colors.status-subtle}` whole-string label. Every authoritative row shows projection id and version in `{typography.mono}`.

### audit-evidence-panel

Definition list plus `FluentBadge` rows; timestamps and references use `{typography.mono}`. Never displays secrets, raw payloads, unrelated tenant data, or stack traces.

### high-impact-confirmation

Focus-trapped `FluentDialog`. Confirm is `FluentButton` `ButtonAppearance.Primary` in `{colors.brand-accent}` and is never the default button. The body is a `FluentDialogBody` with `FixedHeaderFooter="true"` and a focusable scroll region, so Confirm and Cancel stay reachable at 320 CSS px even when the body renders a full proposal version. Padding `{spacing.4}`, no destructive red button. Focus placement, focus restoration, Esc wiring, and body contents are behavior; they are in the matching EXPERIENCE row and § Confirmation contents by family.

### pending-command-indicator

The activated `FluentButton` switches to `DisabledFocusable="true"`, which is the v5 binding that emits `aria-disabled` while keeping the control focusable; `Loading` and `Disabled` are forbidden on an activated control because both remove focusability. An adjacent `FluentBadge` in `{colors.status-informative}` reads `Submitted`, then `AuthoritativePending`, then `awaiting projection`. Variant `pending in another session` renders `{colors.status-severe}` on resolution controls in a second tab. It never becomes Success.

## Do's and Don'ts

| Do | Don't |
|---|---|
| Inherit FrontComposer and Fluent visual defaults at the pinned package | Invent a custom Agents design system or a v4/FAST token |
| Make proposed replies visually distinct from Conversation Messages | Style generated draft content as if it has been posted |
| Use Success only for callable, Posted, current-pass, or ProjectionConfirmed states | Use Success for lifecycle active, Submitted, AuthoritativePending, Approved, or PostingPending |
| Render Provider Degraded as Warning and obey `Callability` | Infer callability from enabled or degraded labels |
| Show `catching up` for projection lag | Borrow `Stale`, which means evidence past `ValidUntil` |
| Label the Conversation-authority source Conversation Facilitator | Show or imply a Conversation owner anywhere |
| Leave every pricing field empty until typed and pin the pricing version column | Default price or currency, or hide a zero price |
| Accept a secret reference only, masked, operator-visible | Accept, echo, or display a secret value |
| Reflow every route to 320 CSS px and keep actions operable | Lock out approval at narrow widths or high zoom |
| Take icons from `FcFluentIcons` and request missing glyphs | Reuse a nearby glyph or pull the icons NuGet |
| Use `FluentRadioGroup` for mode and version choice, with an explicit Apply for mode | Open a confirmation on a radio change, which is the first arrow key |
| Name the v5 binding (`ButtonAppearance.Primary`, `DisabledFocusable`) | Reference a v4 name such as `Appearance.Accent`, or use `Disabled`/`Loading` where focus must survive |
| Keep EN/FR keys at parity as whole strings | Assemble sentences from fragments or reuse shell dialogs with unlocalized literals |
