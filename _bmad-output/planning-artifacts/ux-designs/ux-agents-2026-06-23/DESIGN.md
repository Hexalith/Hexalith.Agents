---
name: Hexalith Agents
description: FrontComposer web UI for governed AI participants in Hexalith Conversations. Fluent UI Blazor v5 is inherited; this spine specifies the Agents-specific semantic delta only.
status: draft
created: 2026-06-23
updated: 2026-06-23
sources:
  - ../../briefs/brief-agents-2026-06-23/brief.md
  - ../../prds/prd-agents-2026-06-23/prd.md
  - ../../prds/prd-agents-2026-06-23/addendum.md
  - ../../prds/prd-agents-2026-06-23/reconcile-brief.md
  - ../../../../Hexalith.Tenants/_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/DESIGN.md
  - ../../../../Hexalith.FrontComposer/_bmad-output/project-context.md
  - ../../../../Hexalith.FrontComposer/docs/reference/components/front-composer-shell.md
  - ../../../../Hexalith.FrontComposer/docs/reference/components/navigation.md
  - ../../../../Hexalith.FrontComposer/docs/reference/components/datagrid.md
colors:
  status-success:
    note: 'Inherit Fluent BadgeColor.Success. Reserved for proven readiness, posted response, approved version, audit available, and active configuration.'
  status-informative:
    note: 'Inherit Fluent BadgeColor.Informative. Used for generation in progress, proposal pending, approval waiting, posting pending, readiness checking, and neutral operational progress.'
  status-warning:
    note: 'Inherit Fluent BadgeColor.Warning. Used for expiring proposals, latency/cost warnings, degraded provider capacity, and states that need attention but remain recoverable.'
  status-severe:
    note: 'Inherit Fluent BadgeColor.Severe. Used for blocked readiness, disabled provider/model, disabled Agent, expired proposal, unavailable dependency, or fail-closed call state.'
  status-danger:
    note: 'Inherit Fluent BadgeColor.Danger. Used sparingly for rejected proposals, failed generation, failed posting, denied authorization, and provider/runtime failures.'
  status-important:
    note: 'Inherit Fluent BadgeColor.Important. Used when state is uncertain or must be resolved before side effects: ambiguous Party identity, missing policy basis, oversized context decision pending.'
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
    base: 'Conversation-owned invocation affordance; exact pattern unresolved'
    selected: '{colors.brand-accent}'
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

- `{colors.status-success}` means proven usable or complete: active Agent, enabled provider/model, approved version, posted response, audit available.
- `{colors.status-informative}` means in progress or waiting: generation running, proposal pending, approval waiting, posting pending, readiness checking.
- `{colors.status-warning}` means attention soon: proposal nearing expiry, provider degraded, context near model limit, cost/latency warning where a launch policy defines one.
- `{colors.status-severe}` means blocked but not a runtime failure: disabled Agent, disabled provider/model, expired proposal, missing Party identity, missing Conversation access, unavailable dependency.
- `{colors.status-danger}` means failure or denial: authorization denied, generation failed, posting failed, proposal rejected, provider error.
- `{colors.status-important}` means state is uncertain and must be resolved before side effects: ambiguous identity, missing policy basis, oversized Conversation Context handling not decided.
- `{colors.status-subtle}` means quiet history or non-actionable state: abandoned proposal, disabled but valid option, no pending proposals, read-only inspection.

No-color-only is mandatory. Every status appears as semantic color plus icon plus visible text. The icon vocabulary should be verified against the pinned Fluent UI package at build, following the Tenants precedent. Do not rely on provider logos or model brand colors to carry status.

## Typography

Typography inherits Fluent. Use `{typography.heading}` for page and panel titles, `{typography.body}` for operational copy and preview text, `{typography.label}` for fields and state labels, `{typography.caption}` for metadata, and `{typography.mono}` for exact identifiers and timestamps.

Generated reply content may be longer and more prose-like than the surrounding admin UI, but it should still use the inherited body role inside a bounded preview/editor region. Do not introduce editorial display typography for AI text; it would make generated content feel more authoritative than it is.

All labels, states, denial reasons, expiry notices, provider/model names, approval actions, and audit references must be localizable whole strings. Do not assemble approval or audit sentences from fragments at runtime.

## Layout & Spacing

Use FrontComposer page measures deliberately:

- FullWidth for read-heavy grids: provider catalog, proposal queue, operational status lists, audit lists.
- Constrained for forms and judgment-heavy flows: `hexa` configuration, provider edit, approver policy, proposal editor, approval confirmation.

Spacing follows the Fluent-compatible 4px rhythm in frontmatter. Use `{spacing.4}` between related form fields, `{spacing.6}` between major sections, and `{spacing.8}` only for page-level separation. Avoid decorative card grids. These are admin workflows; dense tables, forms, panels, split views, tabs, and inline status regions should do the work.

Reserve stable space for status badges, action slots, expiry labels, and proposal state indicators so rows do not jump when a proposal changes state or a call transitions from generation to posting.

## Elevation & Depth

Inherit Fluent and FrontComposer. Elevation exists for transient overlays, popovers, dialogs, and focus-trapped confirmation surfaces. It should not be used to imply audit certainty or proposal importance. Hierarchy comes from layout, labels, state badges, and section grouping.

## Shapes

Inherit Fluent shapes. Do not add custom radii for Agent surfaces. Status badges and chips follow Fluent badge shapes; forms, editors, panels, dialogs, and grids follow FrontComposer/Fluent defaults.

## Components

### agent-readiness-badge

Shows whether `hexa` is callable. It combines Agent lifecycle, Party identity, provider/model readiness, instructions validity, response mode, and approver policy completeness into a readable readiness indicator. The badge must not collapse "active" and "callable" if dependencies are missing. Use `{colors.status-success}` only when all required readiness gates pass.

### provider-status-badge

Shows provider/model availability without exposing secrets. Enabled/usable is `{colors.status-success}`; disabled or unavailable is `{colors.status-severe}`; provider/runtime failure is `{colors.status-danger}`; degraded capacity or policy warning is `{colors.status-warning}`; disabled but valid historical selections can use `{colors.status-subtle}`.

### proposal-state-badge

Renders the proposal lifecycle: generated, edited, regenerated, pending approval, approved, rejected, abandoned, expired, posting pending, posted, posting failed. Approved is not the same as posted. Expired and terminal states must be visually distinct from pending states.

### response-mode-toggle

Displays Automatic Response Mode and Confirmation Response Mode as a clear mutually exclusive choice. The selected mode can use `{colors.brand-accent}` as selection chrome. The control must not make automatic mode look "better" by visual weight alone; mode choice is policy, not a product upsell.

### agent-config-form

Constrained form for identity, display metadata, instructions, provider/model, response mode, approver policy, lifecycle, and readiness. Required fields and activation blockers are inline. Activation is a distinct action after validity is visible.

### approver-policy-builder

Structured builder for policy sources: conversation owner, caller, predefined Parties, tenant roles, and any later source architecture confirms. Each source row carries a readable basis and availability state. Missing or ambiguous policy sources render as blocked, not as empty success.

### provider-catalog-grid

Full-width grid for provider/model administration. Provider name, enabled state, model options, capability metadata, and status are visible without exposing secrets. Secret fields use write-only or configured-state presentation.

### proposal-queue-grid

Full-width grid for pending and historical proposals. Pinned columns should include proposal state, source conversation, caller, current approver responsibility, expiry, and age. The queue should make "needs my action" visually discoverable without hiding other authorized records.

### proposal-editor

The editor is a bounded approval workspace. It shows the current selected version, editable content where authorized, source metadata, version actions, and approval controls. Generated content and edited content are labeled distinctly. A proposed reply is never styled like an already-posted Conversation Message.

### version-history

Shows every generated, edited, and regenerated version with author/source, timestamp, provider/model where applicable, and approval/posting markers. Prior versions remain visible after edit or regeneration. Use `{typography.mono}` for timestamps and references.

### conversation-agent-call

The visual affordance for calling `hexa` from a Conversation is intentionally unresolved. Whether it becomes a mention, command, action button, participant affordance, or a combination, it must visibly name `hexa`, show the selected response mode when relevant, and avoid implying that a proposal is already a Conversation Message.

### operational-status-panel

Status region for readiness, recent call outcomes, generation failures, proposal bottlenecks, provider readiness, and posting outcomes. It uses MessageBar/status-region patterns and should group by recoverable action rather than raw subsystem.

### audit-evidence-panel

Support-safe evidence view linking caller, Agent, Source Conversation, provider/model, proposal versions, approver, approval timestamp, posted Conversation Message, and outcome. Never displays provider secrets, raw credentials, unrelated tenant data, raw payload dumps, or stack traces.

## Do's and Don'ts

| Do | Don't |
|---|---|
| Inherit FrontComposer and Fluent visual defaults | Invent a custom Agents design system |
| Make proposed replies visually distinct from Conversation Messages | Style generated draft content as if it has been posted |
| Use Success only for active/callable/proven/posted/audit-available states | Use Success for generation started, proposal pending, or approval waiting |
| Show provider/model readiness without exposing secrets | Display provider credentials, raw secret values, or provider SDK details |
| Preserve version labels, timestamps, and approval basis | Let edits overwrite or visually hide generated versions |
| Keep `hexa` named and attributable | Make `hexa` a mascot or anonymous system voice |
| Use whole localizable strings | Assemble approval/audit sentences from fragments |
