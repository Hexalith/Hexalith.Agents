---
name: Hexalith Agents
description: FrontComposer web UI for governed AI participants in Hexalith Conversations. Fluent UI Blazor v5 is inherited; this spine specifies the Agents-specific semantic delta only.
status: final
created: 2026-06-23
updated: 2026-08-02
sources:
  - ../../briefs/brief-agents-2026-06-23/brief.md
  - ../../prds/prd-agents-2026-06-23/prd.md
  - ../../prds/prd-agents-2026-06-23/addendum.md
  - ../../prds/prd-agents-2026-06-23/reconcile-brief.md
  - ../../sprint-change-proposal-2026-08-02.md
  - ../../architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
  - ../../launch-readiness-register.md
  - ../../../../references/Hexalith.Tenants/_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md
  - ../../../../references/Hexalith.FrontComposer/_bmad-output/project-context.md
  - ../../../../references/Hexalith.FrontComposer/docs/reference/components/front-composer-shell.md
  - ../../../../references/Hexalith.FrontComposer/docs/reference/components/navigation.md
  - ../../../../references/Hexalith.FrontComposer/docs/reference/components/datagrid.md
colors:
  status-success:
    note: 'Inherit Fluent BadgeColor.Success. Reserved for authoritatively proven callable Agent readiness, posted Conversation Messages, current passing evidence, and projection-confirmed completed operations.'
  status-informative:
    note: 'Inherit Fluent BadgeColor.Informative. Used for submitted or authoritative-pending commands, generation in progress, proposal pending, approved, posting pending, readiness checking, and other nonterminal progress.'
  status-warning:
    note: 'Inherit Fluent BadgeColor.Warning. Used for expiring proposals, latency/cost warnings, and Provider Degraded only when the architecture result also reports Callability == Callable.'
  status-severe:
    note: 'Inherit Fluent BadgeColor.Severe. Used for blocked readiness, disabled provider/model, disabled Agent, expired proposal, unavailable dependency, or fail-closed call state.'
  status-danger:
    note: 'Inherit Fluent BadgeColor.Danger. Used sparingly for rejected proposals, failed generation, failed posting, denied authorization, and provider/runtime failures.'
  status-important:
    note: 'Inherit Fluent BadgeColor.Important. Used when state is uncertain or must be resolved before side effects: ambiguous Party identity, missing/stale policy basis, indeterminate budget reservation, or incomplete deletion confirmation.'
  status-subtle:
    note: 'Inherit Fluent BadgeColor.Subtle. Used for disabled but valid options, abandoned proposals, read-only inspection, no activity, and non-actionable history.'
  brand-accent:
    note: 'Inherit Fluent BadgeColor.Brand / FrontComposer theme accent. Used for primary eligible actions and selected navigation only, never as a status.'
typography:
  body:
    note: 'Inherit Fluent / system body ramp for operational copy and generated-response previews.'
  label:
    note: 'Inherit Fluent / system label ramp for field labels, policy labels, column headers, and status labels.'
  heading:
    note: 'Inherit Fluent / system heading ramp for page and panel titles. No hero/display scale in the admin surface.'
  caption:
    note: 'Inherit Fluent / system caption ramp for secondary metadata, provider/model details, trace references, and help text.'
  mono:
    note: 'Inherit Fluent / system monospace role for AgentId, PartyId, ConversationId, proposal references, provider/model identifiers, timestamps, and support-safe audit references.'
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
    color: '{colors.status-success} | {colors.status-informative} | {colors.status-warning} | {colors.status-severe} | {colors.status-important}'
    typography: '{typography.label}'
    gap: '{spacing.1}'
  provider-status-badge:
    color: '{colors.status-success} | {colors.status-warning} | {colors.status-severe} | {colors.status-danger} | {colors.status-subtle}'
    typography: '{typography.label}'
    gap: '{spacing.1}'
  proposal-state-badge:
    color: '{colors.status-success} | {colors.status-informative} | {colors.status-warning} | {colors.status-severe} | {colors.status-danger} | {colors.status-subtle}'
    typography: '{typography.label}'
    gap: '{spacing.1}'
  response-mode-toggle:
    base: 'Fluent segmented control or radio group'
    selected: '{colors.brand-accent}'
    gap: '{spacing.2}'
  agent-config-form:
    base: 'FrontComposer / Fluent form primitives'
    layout: 'Constrained page measure'
    fieldGap: '{spacing.4}'
    sectionGap: '{spacing.6}'
  approver-policy-builder:
    base: 'Fluent form controls plus policy-source rows'
    layout: 'Constrained page measure; row-based policy sources'
    rowGap: '{spacing.3}'
  provider-catalog-grid:
    base: 'FrontComposer FC-TBL / FluentDataGrid'
    pinnedColumns: 'Provider name, enabled state, capability status'
    rowPadding: '{spacing.3}'
  proposal-queue-grid:
    base: 'FrontComposer FC-TBL / FluentDataGrid'
    pinnedColumns: 'Proposal state, source conversation, caller, expiry'
    rowPadding: '{spacing.3}'
  proposal-editor:
    base: 'Fluent textarea/editor region with adjacent metadata'
    layout: 'Constrained editor region with version and action rail'
    padding: '{spacing.4}'
  version-history:
    base: 'Fluent list or DataGrid detail panel'
    timestamp: '{typography.mono}'
    rowGap: '{spacing.2}'
  conversation-agent-call:
    base: 'Conversation-owned Call hexa action plus Fluent prompt dialog/panel'
    selected: '{colors.brand-accent}'
    gap: '{spacing.2}'
  conversation-context-policy-panel:
    base: 'Read-only FluentAccordion when two or more titled sections are present'
    layout: 'Constrained page measure; effective full-or-blocked policy and model budget'
    sectionGap: '{spacing.4}'
  content-safety-policy-editor:
    base: 'Fluent form primitives in FluentAccordion when two or more titled sections are present'
    layout: 'Constrained page measure; fixed categories, restricted handling, version validation, publish'
    sectionGap: '{spacing.4}'
  cost-control-editor:
    base: 'Fluent numeric form controls and status regions in FluentAccordion when two or more titled sections are present'
    layout: 'Constrained authoring plus full-width usage and reservation evidence'
    sectionGap: '{spacing.4}'
  launch-readiness-panel:
    base: 'FluentDataGrid plus FluentAccordion for titled metric and evidence groups'
    layout: 'Full-width evidence view; blockers precede passing metrics'
    sectionGap: '{spacing.4}'
  audit-governance-panel:
    base: 'Fluent form and status primitives in FluentAccordion when two or more titled sections are present'
    layout: 'Constrained policy authoring plus full-width export/deletion evidence'
    sectionGap: '{spacing.4}'
  proposal-notification:
    base: 'FrontComposer in-product status/notification entry with Fluent badge and accessible text'
    gap: '{spacing.2}'
  operational-status-panel:
    base: 'Fluent MessageBar / status region'
    padding: '{spacing.4}'
    itemGap: '{spacing.3}'
  audit-evidence-panel:
    base: 'Fluent MessageBar or details panel'
    timestamp: '{typography.mono}'
    reference: '{typography.mono}'
    padding: '{spacing.4}'
---

## Brand & Style

Hexalith Agents is a governed operational tool, not a chat novelty and not a marketing surface. It should feel like the rest of the Hexalith admin ecosystem: calm, precise, dense enough for repeated use, and explicit about what the system knows before it allows side effects. The first visible Agent is `hexa`, but the visual system should not turn `hexa` into a mascot. The product signal is named, attributable AI participation inside Conversations.

This is a FrontComposer inheritance spec. Hexalith Agents inherits the FrontComposer shell and Microsoft Fluent UI Blazor v5 components. There is no bespoke palette, type ramp, shadow language, or custom shape system to invent. The delta is semantic: Agent readiness, provider readiness, proposal state, approval state, version history, posting outcome, and audit evidence need consistent meaning so administrators and approvers never confuse a draft with a Conversation Message.

The surface is not classified as regulated UX, but it is governed operational UX. That means provider secrets are never displayed, authorization failures are plain and safe, proposed replies remain visually distinct from Conversation Messages, and version/audit evidence is legible without adding compliance-heavy language beyond source requirements.

## Colors

Colors inherit Fluent semantic roles by name. Bind meaning to role, never to hex. `Brand` is chrome and eligible primary action only; it is not a state color.

- `{colors.status-success}` means authoritatively proven or complete: current callable Agent readiness, `Ready / Callable / None` Provider readiness, `posted` Conversation Message, current passing evidence, or projection-confirmed completed operation.
- `{colors.status-informative}` means in progress or waiting: submitted or authoritative-pending command, generation running, proposal pending, approval waiting, `approved`, `posting pending`, or readiness checking.
- `{colors.status-warning}` means attention soon: proposal nearing expiry, `Degraded / Callable / NonBlockingOperationalWarning` Provider readiness, context near model limit, or cost/latency warning where a launch policy defines one.
- `{colors.status-severe}` means blocked but not a runtime failure: disabled Agent, disabled provider/model, expired proposal, missing Party identity, missing Conversation access, unavailable dependency.
- `{colors.status-danger}` means failure or denial: authorization denied, generation failed, posting failed, proposal rejected, provider error.
- `{colors.status-important}` means state is uncertain and must be resolved before side effects: ambiguous identity, missing/stale policy basis, indeterminate budget reservation, or incomplete restrictive deletion confirmation.
- `{colors.status-subtle}` means quiet history or non-actionable state: abandoned proposal, disabled but valid option, no pending proposals, read-only inspection.

No-color-only is mandatory under the binding WCAG 2.2 AA floor. Every status appears as a Fluent semantic role plus icon plus visible text and accessible name. The icon vocabulary should be verified against the pinned Fluent UI package at build, following the Tenants precedent. Do not rely on provider logos or model brand colors to carry status.

## Typography

Typography inherits Fluent. Use `{typography.heading}` for page and panel titles, `{typography.body}` for operational copy and preview text, `{typography.label}` for fields and state labels, `{typography.caption}` for metadata, and `{typography.mono}` for exact identifiers and timestamps.

Generated reply content may be longer and more prose-like than the surrounding admin UI, but it should still use the inherited body role inside a bounded preview/editor region. Do not introduce editorial display typography for AI text; it would make generated content feel more authoritative than it is.

All labels, states, denial reasons, expiry notices, provider/model names, approval actions, and audit references must be localizable whole strings. English and French resource keys must remain at exact parity. Do not assemble approval, posting, failure, readiness, or audit sentences from fragments at runtime. Missing keys or conditional localization skips are conformance failures, not acceptable fallback behavior.

## Layout & Spacing

Use FrontComposer page measures deliberately:

- FullWidth for read-heavy grids: provider catalog, proposal queue, operational status lists, audit lists.
- Constrained for forms and judgment-heavy flows: `hexa` configuration, provider edit, approver policy, proposal editor, approval confirmation.

Spacing follows the Fluent-compatible 4px rhythm in frontmatter. Use `{spacing.4}` between related form fields, `{spacing.6}` between major sections, and `{spacing.8}` only for page-level separation. Avoid decorative card grids. These are admin workflows; dense tables, forms, panels, split views, tabs, and inline status regions should do the work.

Reserve stable space for status badges, action slots, expiry labels, and proposal state indicators so rows do not jump when a proposal changes state or a call transitions from generation to posting.

A page, dialog, or detail panel with two or more sibling titled content sections uses one `FluentAccordion` with one `FluentAccordionItem` per section and the primary item expanded by default. Keep page titles, breadcrumbs, toolbars, navigation chrome, and a single primary content region outside the accordion. Never hide the only primary content behind an accordion interaction.

## Elevation & Depth

Inherit Fluent and FrontComposer. Elevation exists for transient overlays, popovers, dialogs, and focus-trapped confirmation surfaces. It should not be used to imply audit certainty or proposal importance. Hierarchy comes from layout, labels, state badges, and section grouping.

## Shapes

Inherit Fluent shapes. Do not add custom radii for Agent surfaces. Status badges and chips follow Fluent badge shapes; forms, editors, panels, dialogs, and grids follow FrontComposer/Fluent defaults.

## Components

### agent-readiness-badge

Shows whether `hexa` is callable. It combines Agent lifecycle, Party identity, provider/model readiness, instructions validity, response mode, approver policy completeness, and authoritative gate evidence into a readable readiness indicator. Lifecycle `active` is not proof of callability. Use `{colors.status-success}` only when the current authoritative readiness projection proves the Agent callable; missing, blocked, stale, or `InsufficientEvidence` results never use Success.

### provider-status-badge

Shows the architecture-owned `ProviderReadinessResult` without exposing secrets. `Ready / Callable / None` may use `{colors.status-success}`. `Degraded / Callable / NonBlockingOperationalWarning` always uses `{colors.status-warning}` and remains callable only because the result says `Callability == Callable`; the UI never infers callability from `OperationalState`. `Blocked / Blocked / <defined blocker>` uses `{colors.status-severe}` or `{colors.status-danger}` according to the safe failure class. Disabled but valid historical selections can use `{colors.status-subtle}`.

### proposal-state-badge

Renders the proposal lifecycle: generated, edited, regenerated, pending approval, approved, rejected, abandoned, expired, posting pending, posted, posting failed. `Approved` and `posting pending` use progress semantics; only `posted` uses `{colors.status-success}` and proves a Conversation Message exists. Rejected, abandoned, expired, and posting failed remain visually distinct non-success terminal states.

### response-mode-toggle

Displays Automatic Response Mode and Confirmation Response Mode as a clear mutually exclusive choice. The selected mode can use `{colors.brand-accent}` as selection chrome. The control must not make automatic mode look "better" by visual weight alone; mode choice is policy, not a product upsell.

### agent-config-form

Constrained form for identity, display metadata, instructions, provider/model, response mode, approver policy, lifecycle, and readiness. Required fields and activation blockers are inline. Activation is a distinct action after validity is visible.

### approver-policy-builder

Structured builder for policy sources: conversation owner, caller, predefined Parties, tenant roles, and any later source architecture confirms. Each source row carries a readable basis and availability state. Missing or ambiguous policy sources render as blocked, not as empty success.

### provider-catalog-grid

Full-width grid for provider/model administration. Provider name, enabled state, model options, capability metadata, and status are visible without exposing secrets. Secret fields use write-only or configured-state presentation.

### proposal-queue-grid

Full-width grid for pending and historical proposals created after successful generation. Pinned columns should include proposal state, source conversation, caller, current approver responsibility, expiry, and age. The queue should make "needs my action" visually discoverable without hiding other authorized records. Separate generation-failure records never appear as proposal rows.

### proposal-editor

The editor is a bounded approval workspace created only after successful generation. It shows the current selected version, editable content where authorized, source metadata, version actions, and approval controls. Generated content and edited content are labeled distinctly. A proposed reply is never styled like an already-posted Conversation Message. Failed generation never opens this workspace; retained failure content belongs only to a separate non-approvable failure record.

### version-history

Shows every successfully generated, edited, and regenerated proposal version with author/source, timestamp, provider/model where applicable, and approval/posting markers. Prior versions remain visible after edit or regeneration. Retained generation-failure content is not a proposal version. Use `{typography.mono}` for timestamps and references.

### conversation-agent-call

The sole V1 invocation affordance is the Conversation-owned **Call hexa** action. It visibly names `hexa`, opens an accessible prompt surface, shows the effective response mode before submission, and presents `submitted`, `authoritative pending`, and `projection-confirmed terminal` as distinct states. It never implies that generated or proposed content is already a Conversation Message. Mentions, commands, ambient triggers, and alternate entry points are out of V1.

### conversation-context-policy-panel

Read-only complete-context-or-blocked behavior, effective model budget, policy version, and the safe reason an oversized call cannot proceed. The surface offers no truncation, summary, windowing, or retrieval control.

### content-safety-policy-editor

Fixed blocked categories, restricted handling, policy version, validation, and an explicit publish action. Publishing uses the pending-command treatment below and never permits a weaker retry.

### cost-control-editor

Monthly tenant budget, per-call caps, current usage/reservations, 80% warning, and 100% blocked state. Indeterminate reservations remain blocked and non-success.

### launch-readiness-panel

Shows `Pass`, `Block`, `InsufficientEvidence`, and `Stale`; exact NFR-14 thresholds; sample sufficiency; SM-2/SM-3 cohort/window; evidence levels; `ObservedAt` and exclusive `ValidUntil`; and every remaining blocker.

NFR-14 visual evidence must expose the three exact gates without turning browser-local progress into server truth: page usability p95 <= 2.5 seconds, authoritative pending acknowledgement p95 <= 500 milliseconds, and terminal render/live-region announcement p95 <= 2 seconds. Each gate requires at least 30 qualifying production-like executions of its own sample kind. Any missing authoritative tick or reference, absent localized live-region mutation, mixed clock origin, failed correlation, conflicting duplicate, or undersized sample set renders `InsufficientEvidence`, never Success.

### audit-governance-panel

The 365-day rule, legal hold, encrypted time-limited export, deletion progress, projection purge, and restrictive partial-failure states remain visible and support-safe.

Each multi-section policy, readiness, or governance surface follows the `FluentAccordion` rule above; all controls use FrontComposer and Fluent UI Blazor v5 primitives.

High-impact proposal resolution, policy publication, tenant budget update, legal hold, export, and deletion actions require explicit confirmation, current authorization, future-only effect copy where applicable, keyboard/focus/live-region behavior, and projection/evidence-confirmed success. Pending styling and disabled action treatment apply only to the same resource and operation family in the current session; unrelated resources and families remain available. A pending or partially applied result never receives success styling.

### proposal-notification

V1 notifications are in-product only: an accessible pending-count badge and proposal queue/status entry link authorized Approvers to work needing attention. Notification copy includes state and expiry without exposing proposal content. Email, push, and external-channel delivery are out of scope.

### operational-status-panel

Status region for readiness, recent call outcomes, generation failures, proposal bottlenecks, provider readiness, and posting outcomes. A generation failure appears as a separate non-approvable failure record and never as a Proposed Agent Reply. The panel uses MessageBar/status-region patterns and groups by recoverable action rather than raw subsystem.

### audit-evidence-panel

Support-safe evidence view linking caller, Agent, Source Conversation, provider/model, proposal versions when generation succeeded, separate failure record when generation failed, approver, approval timestamp, authoritative pending identity/projection version, projection-confirmed terminal timestamp, posted Conversation Message, and outcome. Never displays provider secrets, raw credentials, unrelated tenant data, raw payload dumps, or stack traces.

## Do's and Don'ts

| Do | Don't |
|---|---|
| Inherit FrontComposer and Fluent visual defaults | Invent a custom Agents design system |
| Make proposed replies visually distinct from Conversation Messages | Style generated draft content as if it has been posted |
| Use Success only for authoritatively proven callable, posted, current-pass, or projection-confirmed complete states | Use Success for lifecycle active, submitted, authoritative pending, approved, or posting pending |
| Render Provider Degraded as Warning and obey `Callability` from the readiness result | Infer callability from Provider enabled/degraded labels or UI policy |
| Keep generation failure in a separate non-approvable failure record | Create a Proposed Agent Reply from failed or incomplete generation |
| Show provider/model readiness without exposing secrets | Display provider credentials, raw secret values, or provider SDK details |
| Preserve version labels, timestamps, and approval basis | Let edits overwrite or visually hide generated versions |
| Keep `hexa` named and attributable | Make `hexa` a mascot or anonymous system voice |
| Keep English/French keys at parity and use whole localizable strings | Assemble approval/audit sentences from fragments or accept missing locale keys |
| Use Fluent semantic roles, Fluent v5 parameters, and Fluent 2 tokens | Define a custom theme, hard-code colors, or use Fluent v4/FAST tokens |
