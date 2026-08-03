---
name: Hexalith Agents
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
  - ../../../../references/Hexalith.Tenants/src/Hexalith.Tenants.UI/Composition/TenantsFrontComposerRegistration.cs
  - ../../../../references/Hexalith.Tenants/src/Hexalith.Tenants.UI/Program.cs
  - ../../../../references/Hexalith.Tenants/src/Hexalith.Tenants.UI/Components/Layout/MainLayout.razor
  - ../../../../references/Hexalith.Tenants/_bmad-output/planning-artifacts/ux-designs/ux-tenants-2026-06-02/EXPERIENCE.md
  - ../../../../references/Hexalith.FrontComposer/_bmad-output/project-context.md
  - ../../../../references/Hexalith.FrontComposer/docs/reference/components/front-composer-shell.md
  - ../../../../references/Hexalith.FrontComposer/docs/reference/components/navigation.md
  - ../../../../references/Hexalith.FrontComposer/docs/reference/components/datagrid.md
---

# Hexalith Agents - Experience Spine

> Final spine reconciled through the approved 2026-08-02 Correct Course decision. `DESIGN.md` owns visuals; this file owns behavior, surfaces, states, accessibility, and flows. The spines win on conflict with mockups, wireframes, imports, and source-derived sketches.

## Foundation

Hexalith Agents is a desktop-first responsive web experience composed through **Hexalith.FrontComposer**. The shell owns header, navigation, account controls, theme/settings, command palette, skip links, keyboard shell behavior, shell localization, and global status chrome. The Agents domain owns registered navigation entries, page bodies, domain copy, Agent/proposal workflow behavior, and BFF/API-facing interaction.

The UI system is inherited: Microsoft Fluent UI Blazor v5 through FrontComposer. `DESIGN.md` is the visual identity reference. This experience spine references `DESIGN.md` component tokens by name and does not restate their visual styling. Agents defines no custom theme and uses Fluent semantic roles, Fluent v5 component parameters, and Fluent 2 tokens only.

Stakes are internal/governed operational, not regulated UX. Product-required authorization, tenant isolation, approval, proposal versioning, provider-secret safety, and audit evidence remain first-class. The copy should be plain and precise, without extra regulated-industry ceremony unless a later governance decision requires it.

V1 exposes only `hexa` as the product behavior, even if implementation uses generalized Agent structures. V1 excludes long-term memory, tools, project/folder content, ambient triggers, external channels, and business actions beyond posting automatic or approved replies to Conversations.

## Information Architecture

FrontComposer shell navigation should register an **Agents** domain/category. Candidate entries, ordered from operational setup to workflow handling:

| Surface | Reached from | Purpose |
|---|---|---|
| **Agents overview** | Agents nav default | See `hexa` lifecycle separately from authoritatively proven callability, response mode, provider/model, pending proposal count, recent failures, and safe blockers for the tenant. |
| **`hexa` configuration** | Agents overview | Configure identity, display metadata, instructions, provider/model, response mode, approver policy, lifecycle, and activation blockers. |
| **Provider catalog** | Agents nav or `hexa` configuration | Manage provider/model records and inspect the architecture result's `OperationalState`, `Callability`, `ReasonCode`, capability version, and freshness without exposing secrets. |
| **Approver policy** | `hexa` configuration | Define who may edit, regenerate, approve, reject, abandon, or resolve proposals: conversation owner, caller, predefined Parties, tenant roles, or future confirmed policy sources. |
| **Conversation context policy** | `hexa` configuration, readiness | Read the effective complete-context-or-blocked rule, model budget, policy version, and blocking reason; V1 exposes no bounded-context authoring. |
| **Content safety policy** | Agents nav or `hexa` configuration | Author and publish versioned blocked/restricted handling, validate the policy, and inspect effective safety readiness. |
| **Cost controls** | Agents nav, provider catalog, readiness | Configure monthly tenant budget and per-call caps; inspect usage, reservations, 80% warning, and 100% blocked state. |
| **Conversation invocation** | Source Conversation | Party uses the sole Conversation-owned **Call hexa** action and submits a prompt through its accessible prompt surface. |
| **Proposal queue** | Agents nav, notification entry, Conversation status | Authorized approvers discover successfully generated Proposed Agent Replies waiting for action and historical proposals they may inspect; generation-failure records never enter this queue. |
| **Proposal detail/editor** | Proposal queue, notification, Conversation status | Review generated content, context metadata, version history, edits, regeneration, approval, rejection, abandonment, expiry, and posting outcome. |
| **Launch readiness** | Agents overview, operational status | Inspect `Pass`, `Block`, `InsufficientEvidence`, and `Stale`; exact NFR-14 timing gates; at least-30-per-kind sample sufficiency; authoritative timestamps; SM-2/SM-3 cohort/window; evidence levels; and every remaining blocker. |
| **Operational status** | Agents overview, proposal detail, provider catalog | Distinguish readiness, configuration errors, authorization failures, safety/budget blocks, provider failures, separate non-approvable generation-failure records, pending approvals, posting failures, and projection-confirmed posts. |
| **Audit governance** | Agents nav, audit evidence | Configure retention/legal hold and perform authorized export/deletion with restrictive progress and partial-failure states. |
| **Audit evidence** | Agents overview, proposal detail, status entry, posted response reference | Inspect support-safe evidence for configuration, provider/model changes, calls, versions, approvals, rejections, expirations, automatic posts, and final Conversation Messages. |
| **API/client contract reference** | Developer docs, not primary UI | Omar's integration journey needs stable public operations, but docs are not a FrontComposer admin screen unless product later asks for an in-app developer surface. |

Surface closure status: **final**. Admin/configuration, provider governance, full-context policy, safety, cost, **Call hexa**, in-product proposal notification, proposal workflow, generation-failure evidence, launch readiness, audit governance, status, and evidence surfaces are defined. Implementation remains gated by the approved forward backlog and the external Conversations AI-membership prerequisite.

## Voice and Tone

Microcopy is operational, direct, and source-of-truth aware. Brand/aesthetic posture lives in `DESIGN.md`.

| Do | Don't |
|---|---|
| "`hexa` is active. Callability is not yet proven." | "`hexa` is callable" based only on lifecycle. |
| "`hexa` is callable." after authoritative readiness proof | "`hexa` is ready to help!" |
| "Generation failed. No proposal or Conversation Message was created." | "Something went wrong." |
| "Proposal pending approval." | "Reply sent." |
| "Approved. Posting is pending." | "Approved successfully" as a completed-message state |
| "Approved version posted to the Conversation." after `posted` proof | "Posted" before the projection confirms the Conversation Message |
| "You do not have permission to approve this proposal." | "Forbidden 403" |
| "Provider is disabled. Calls are blocked until reconfigured." | "Provider unavailable" with no recovery path |
| "This proposal expired. Start a new Agent call." | "Try approving again" after terminal expiry |
| "Prior generated versions are preserved." | "Old draft replaced." |

Forbidden copy patterns:

- Do not call unapproved generated content a "message".
- Do not imply an automatic response or approved proposal was posted until posting is confirmed.
- Do not expose provider secrets, raw payloads, stack traces, raw tenant data from other scopes, or provider SDK errors.
- Do not use mascot-like copy for `hexa`; `hexa` is a named Agent participant.

All domain copy is localizable as whole strings with named placeholders. English and French resource keys must stay at exact parity across every interactive V1 route and high-impact state. Missing keys, runtime sentence-fragment assembly, and conditional locale skips are conformance failures. Shell chrome strings remain FrontComposer-owned per FC-L10N.

## Component Patterns

Behavioral rules only. Visual specs live in `DESIGN.md.Components`.

| Component | Behavioral rules |
|---|---|
| **agent-readiness-badge** | Summarizes authoritative Agent callability and explains blockers. Lifecycle `active` is separate and never receives Success by itself. Success requires current authoritative callability proof; missing, blocked, stale, or `InsufficientEvidence` results block calls and remain non-success. |
| **provider-status-badge** | Consumes `ProviderReadinessResult` without inference. `Degraded` is Warning and allows calling only when the result says `Callability == Callable`; missing, stale, disabled, unconfigured, failed, regressed, unknown, or indeterminate inputs are blocked. |
| **proposal-state-badge** | Renders proposal lifecycle states distinctly. `Approved` and `posting pending` are progress; only `posted` is Success and proves the Conversation Message exists. Rejected, abandoned, expired, and posting failed are non-success terminal states. |
| **response-mode-toggle** | Mutually exclusive automatic vs confirmation response mode. Mode changes apply to future Agent Calls only. The UI must make that future-only effect explicit. |
| **agent-config-form** | Validates required fields before activation. Activation blockers are inline and actionable. Configuration changes are auditable and should show what changed where safe to expose. |
| **approver-policy-builder** | Builds approval authority from configured sources. Each source row names the policy basis. Ambiguous or unavailable policy sources fail closed and block confirmation-mode activation. |
| **provider-catalog-grid** | Lists provider/model options, enabled state, capability metadata, and readiness. Secret-backed fields show configured/not configured, never secret values. |
| **proposal-queue-grid** | Shows pending proposals and authorized historical proposals. Filters should support "needs my action", state, Agent, source Conversation, caller, and expiry where data exists. Empty and filtered-empty are distinct. |
| **proposal-editor** | Exists only after successful generation. It lets authorized approvers edit content, request regeneration, approve a selected version, reject, or abandon while non-authorized viewers see read-only state. Editing creates a new preserved version. Generation failure creates no editor or approval actions. |
| **version-history** | Lists every successfully generated, edited, and regenerated proposal version. Approval identifies the selected version; posting evidence later identifies whether that version became a Conversation Message. Retained failure content is not a proposal version and remains only in its separate non-approvable failure record. |
| **conversation-agent-call** | The Conversation-owned **Call hexa** action captures Source Conversation, caller, Agent, prompt, effective response mode, authorization decision, and timestamp. Mentions, commands, ambient triggers, and alternate V1 entry points are absent. |
| **conversation-context-policy-panel** | Read-only full-or-blocked policy, effective context/output budget, policy version, and safe block reason. It never offers truncation, summarization, windowing, or retrieval controls. |
| **content-safety-policy-editor** | Shows fixed blocked categories and restricted handling, validates draft versions, and requires authorized confirmation to publish a future-only version. Failed attempts cannot be retried through weaker policy. |
| **cost-control-editor** | Authors numeric monthly tenant budget and per-call caps. It exposes safe usage/reservation evidence, an 80% warning, 100% block, and indeterminate-state block without revealing Provider secrets. |
| **launch-readiness-panel** | Shows `Pass`, `Block`, `InsufficientEvidence`, and `Stale`; exact p95 thresholds and timestamp seams; at least 30 qualifying production-like executions per NFR-14 sample kind; Level 4/5 evidence; `ObservedAt`/exclusive `ValidUntil`; and blockers. Missing/invalid timestamps, authoritative references, localized live-region mutation, samples, evidence, routes, locale keys, viewport proof, or unconditional execution cannot render as Pass. |
| **audit-governance-panel** | Supports the 365-day policy, legal holds, encrypted time-limited export, and cryptographic deletion/projection purge. Success appears only after authoritative evidence; partial failure stays restrictive and actionable. |
| **proposal-notification** | In-product pending count and status entry only. It links authorized users to the queue, includes state/expiry without content, and never implies posting. Email, push, and external channels are absent. |
| **operational-status-panel** | Groups readiness and runtime failures by recovery: configure provider, fix policy, wait for approval, retry through an allowed new attempt, inspect audit, start a new call. A failed generation is represented only by its separate non-approvable failure record and never enters the proposal queue/editor. Avoid raw subsystem labels as the primary message. |
| **audit-evidence-panel** | Presents support-safe evidence and references. It links caller, Agent, Source Conversation, provider/model, response mode, successful proposal versions, a separate non-approvable failure record when generation failed, approver, authoritative pending and terminal projection/version references, approval/posting outcome, timestamps, and final Conversation Message where applicable. |

## State Patterns

Canonical states should be used consistently across surfaces. Exact implementation token names can be refined by architecture, but UX must preserve these distinctions and the following explicit flows:

```text
submitted -> authoritative pending -> projection-confirmed terminal
approved -> posting pending -> posted
generation failed -> separate failure record; no proposal
```

`Authoritative pending` exists only after the UI receives and renders a server/EventStore-accepted pending identity plus projection/version reference. `Projection-confirmed terminal` exists only after the authoritative terminal projection/version is received and rendered. Optimistic client state, a timeout, a SignalR nudge, or an unrelated projection change proves neither state.

### Agent Readiness

| State | Meaning | Treatment |
|---|---|---|
| `callable` | The current authoritative Agent readiness projection proves callability | Allow calls and use Success. Lifecycle `active` is necessary but not sufficient. |
| `active, not proven callable` | Lifecycle is active but a required gate is missing, blocked, stale, or `InsufficientEvidence` | Keep lifecycle visible, block calls, and show the authoritative safe blocker without Success. |
| `checking` | Readiness is being evaluated | Calls are not confirmed ready; show progress. |
| `invalid configuration` | Required Agent fields, instructions, response mode, or policy missing | Block activation/calls with inline blockers. |
| `missing party identity` | Agent Party identity missing, disabled, ambiguous, or unauthorized | Block posting and calls that would need posting. |
| `provider unavailable` | Provider/model missing, disabled, or failed | Block provider invocation. |
| `disabled` | Agent disabled by lifecycle | Calls rejected before provider invocation. |

### Provider And Model

| State | Meaning | Treatment |
|---|---|---|
| `Ready / Callable / None` | Every architecture hard gate passes without warning | Eligible for configuration and calling; use Success. |
| `Degraded / Callable / NonBlockingOperationalWarning` | Every hard gate passes with the single defined non-blocking warning | Use Warning. Calls remain available only because `Callability == Callable`; the UI cannot override or infer this result. |
| `Blocked / Blocked / <defined blocker>` | A hard gate is missing, stale, disabled, unconfigured, unpriced, invalid, unavailable, failed, regressed, unknown, or indeterminate | Block selection or invocation as the safe reason requires; never relabel as Degraded. |

### Agent Call

| State | Meaning | Treatment |
|---|---|---|
| `submitted` | Caller dispatched an explicit Agent Call | Show local progress without Success; do not claim server acceptance. |
| `authoritative pending` | Server/EventStore accepted the call and the UI rendered its pending identity plus projection/version | Keep progress visible and follow the authoritative status projection. |
| `authorized` | Call permission and Conversation access passed | Continue to context build. |
| `denied` | Authorization failed | Stop before provider invocation; show safe reason. |
| `context loading` | V1 full Conversation Context loading | Show progress; do not invoke provider yet. |
| `context blocked` | Complete context cannot be loaded safely or exceeds the selected model's effective budget | Fail closed before Provider invocation; no truncation, summary, proposal, or message. |
| `generating` | Provider request in progress | Show in-flight state; no message/proposal yet. |
| `generation failed` | Provider/runtime/policy failure | Create no Proposed Agent Reply and no Conversation Message. Any retained content belongs only to a separate non-approvable failure record. |
| `generated` | Complete Agent Response produced | Automatic mode proceeds to posting; confirmation mode creates Proposed Agent Reply. |
| `projection-confirmed terminal` | The authoritative terminal projection/version was received and rendered | Render the exact terminal outcome; only a `posted` outcome proves a Conversation Message exists. |

### Proposal Lifecycle

| State | Meaning | Treatment |
|---|---|---|
| `generated` | Initial generated version exists outside Conversation | Discoverable by authorized approvers. |
| `edited` | Approver created an edited version | Prior versions remain visible. |
| `regenerated` | New generated version created | Prior generated/edited versions remain visible. |
| `pending approval` | Awaiting authorized approver action | Queue-visible and notification-eligible. |
| `approved` | A selected version was approved | Non-success progress. Begin or await posting; do not imply a Conversation Message exists. |
| `rejected` | Terminal; will not post | Preserve versions and evidence. |
| `abandoned` | Terminal manual end without posting | Preserve versions and evidence. |
| `expired` | Terminal policy expiry | Cannot post; start a new Agent Call. |
| `posting pending` | Approved version is being posted to Conversation | Non-success progress; not yet a Conversation Message. |
| `posted` | The authoritative projection proves the final Conversation Message exists as `hexa` | Use Success and link approval/call evidence to the posted message. |
| `posting failed` | Approved version did not become Conversation Message | Show recovery/status without fabricating success. |

### Generation Failure Record

A generation failure never creates a proposal, proposal version, proposal queue entry, proposal notification, editor, or approval/posting action. If generated or incomplete content is retained, it appears only within a separate non-approvable failure record under operational status/audit evidence, subject to the same authorization and sensitive-content rules. The UX does not invent a failure-record schema or turn retained content into a recoverable draft.

### High-Risk Pending Commands

The advisory UI/BFF pending lock is scoped to `(user session, resource identity, operation family)`. At most one pending command is allowed for the same resource and family. The normative families are `ProposalResolution`, `PolicyPublication`, `TenantBudgetUpdate`, `LegalHold`, `ExportRequest`, and `DeletionRequest`; unrelated resources or families may proceed concurrently.

The lock begins on submission. It clears only when authoritative status reports rejection before acceptance or a terminal result. An accepted authoritative-pending acknowledgement keeps the lock held. A client timeout forces refresh and never implies success. EventStore optimistic concurrency, deterministic command/effect identity, and idempotency remain authoritative across tabs, sessions, retries, replay, and restarts.

### Launch Readiness Evidence

| State | Treatment |
|---|---|
| `Pass` | Use passing semantics only for the current source and measurement contract when all required fields/evidence qualify and `ObservedAt <= T < ValidUntil`. |
| `Block` | Show the stable support-safe blocker and owning recovery path. |
| `InsufficientEvidence` | Name the missing evidence class safely; never degrade or render as Pass. |
| `Stale` | Require re-observation under the current source/contract; never fall back visually to an older Pass. |

A missing required readiness record is an implicit block. `ObservedAt` and exclusive `ValidUntil` govern evidence freshness; browser interaction durations use their separate monotonic timing contract.

### List And Detail Surfaces

Every grid/list surface distinguishes: loading, empty, filtered-empty, error, permission-denied, stale/degraded where relevant. Empty must not leak unauthorized records. Filtered-empty offers a clear filter reset.

### Audit Availability

| State | Treatment |
|---|---|
| `audit pending` | Evidence expected but not available yet; never success. |
| `audit available` | Evidence is queryable and linked. |
| `audit delayed` | Evidence path exists but is late; show wait/retry/escalate. |
| `audit unavailable` | Evidence cannot be loaded; show safe reference and recovery. |

## Interaction Primitives

- FrontComposer shell shortcuts and command palette remain available. Domain shortcuts must not conflict with shell shortcuts.
- Grid surfaces use FC-TBL patterns for search, filtering, status chips, column prioritization, row detail, and accessible status notices.
- For `ProposalResolution`, `PolicyPublication`, `TenantBudgetUpdate`, `LegalHold`, `ExportRequest`, and `DeletionRequest`, disable only the same resource-and-operation-family action while it is pending in the current session. Do not impose a session-wide lock; unrelated resources and families may proceed.
- Editing a proposal is explicit. Regeneration is a distinct action. Approval applies to a selected version only.
- `Esc` closes transient UI without committing. Focus returns to the triggering proposal row/action.
- Approval and rejection controls require keyboard reachability and clear focus order.
- Hover may reveal secondary actions on desktop, but no required action or denial reason is hover-only.
- **Call hexa** is the sole V1 invocation primitive. It shows the Agent name and effective response mode, captures the prompt, checks permission before Provider invocation, and never renders unapproved content as a Conversation Message.
- High-impact proposal resolution, policy publication, tenant budget update, legal hold, export, and deletion actions require current authorization, explicit confirmation, authoritative-pending feedback, and projection/evidence-confirmed terminal results. Policy changes state their future-only effect.
- A page, dialog, or detail panel with two or more sibling titled content sections uses one `FluentAccordion` and one `FluentAccordionItem` per section, with the primary item expanded by default. Page titles, breadcrumbs, toolbars, navigation chrome, and a single primary content region stay outside. Never hide the only primary content. All custom surfaces remain FrontComposer/Fluent UI Blazor v5 components.

## Accessibility Floor

WCAG 2.2 AA is the binding behavioral floor across every interactive V1 route and high-impact state. Visual contrast and icon/color pairing live in `DESIGN.md`.

- Use FrontComposer FC-A11Y primitives: skip links, focus visibility, named navigation landmarks, keyboard shell controls, and status live regions.
- Every status badge has visible whole-string text, a semantic role, and an accessible name; color is never the sole signal.
- Proposal queue, provider catalog, and audit/status grids expose table semantics, header relationships, sort/filter state, and row action names.
- The proposal editor must be fully keyboard-operable: edit, select version, compare metadata, regenerate, approve, reject, abandon, and exit without committing.
- Live regions announce important transitions: generation failed, proposal created, proposal expired, posting became `posted`, posting failed, and permission denied. Terminal measurement observes the localized mutation after render commit in its `aria-live` or `role=status` node; it proves announcement-ready DOM state, not speech completion. Avoid assertive announcements for ordinary pending progress.
- Focus-trapped dialogs or confirmation panels must provide a safe non-committing escape and return focus to the trigger.
- Reduced motion users must not depend on animation to perceive generation, approval, or posting state changes.
- Provider secrets, raw payloads, and unrelated tenant data must not appear in accessible names, tooltips, copied text, diagnostics, or announcements.
- Whole-string English/French key parity is required for every route and high-impact state. Missing routes, locale keys, semantic/live-region evidence, viewport evidence, or conditional skips make `LR-UI-CONFORMANCE` `InsufficientEvidence`.

## Inspiration & Anti-patterns

The PRD addendum positions Hexalith Agents against Slack AI, Microsoft 365 Copilot in Teams, Zoom AI Companion, and Atlassian Rovo Agents.

- Lifted: clear admin control over AI availability and caller access.
- Lifted: using conversation context to answer in-place rather than forcing users into a separate AI workspace.
- Rejected: generic summarization as the primary product promise. Hexalith Agents is governed participation by a named Party identity.
- Rejected: unapproved generated content appearing in the durable conversation record.
- Rejected: broad autonomous agent behavior in V1. No tools, long-term memory, project/folder activation, or ambient triggers.
- Rejected: model/provider opacity. Provider/model identity must be available in audit evidence without exposing secrets.

## Responsive & Platform

Desktop/laptop is primary. The product is a web admin/workflow surface, not a native mobile app.

| Breakpoint | Behavior |
|---|---|
| Phone | Read-only status, proposal reference, and lightweight review where safe. Complex proposal editing/approval is unavailable unless the full context, version history, and confirmation controls remain legible. |
| Tablet | Navigation collapses per FrontComposer. Grids preserve critical columns through FC-TBL prioritization and row detail. Proposal detail may stack metadata, editor, and version history. |
| Desktop | Primary operating mode: full-width grids, constrained forms/editors, side-by-side proposal metadata and version history where space allows. |
| Wide desktop | Use extra width for split views, not decorative panels. |

Fail-closed responsive rule: at the most restrictive supported viewport, `ProposalResolution`, `PolicyPublication`, `TenantBudgetUpdate`, `LegalHold`, `ExportRequest`, and `DeletionRequest` are unavailable with a visible reason whenever their required decision context cannot be presented safely. Review-only access remains available.

## FrontComposer Readiness

| Capability | UX dependency |
|---|---|
| FC-LYT | FullWidth for grids/status; Constrained for configuration, policy, and proposal editor flows. |
| FC-TBL | Provider catalog, proposal queue, status/audit grids, row detail, filter summaries, empty/filter-empty/error states. |
| FC-A11Y | WCAG 2.2 AA shell skip links, focus behavior, semantic labels/roles, live-region primitives, and override diagnostics. Required for all custom proposal/editor overrides. |
| FC-L10N | Shell/domain localization split. Agents owns whole-string domain labels/workflow copy with English/French key parity; shell owns chrome strings. |
| Policy-gated nav | Provider/admin/proposal entries must hide or deny according to authorization without leaking records. |
| Pending command/status patterns | Preserve `submitted -> authoritative pending -> projection-confirmed terminal`; scope advisory high-risk locks to session + resource + operation family; do not promote pending to Success. |

## Browser Performance Evidence

NFR-14 uses one injected browser-monotonic clock and one `ClockOriginId` per page lifecycle. Browser ticks are never subtracted from server or projection wall-clock timestamps.

| Sample kind | Authoritative start and end seams | Threshold |
|---|---|---|
| `PageUsability` | `NavigationStartedTick` when navigation begins -> `PageUsableTick` after the authorized route renders a non-loading state and its required primary action/status is operable | p95 <= 2.5 seconds |
| `AuthoritativePending` | `CommandSubmittedTick` immediately before dispatch -> `AuthoritativePendingRenderedTick` after receipt and render of a server/EventStore-accepted pending identity plus projection/version | p95 <= 500 milliseconds |
| `TerminalRenderAnnouncement` | `AuthoritativeTerminalReceivedTick` when the client receives the authoritative terminal projection/version -> the later of `TerminalRenderedTick` and `LiveRegionAnnouncedTick` | p95 <= 2 seconds |

Each sample kind requires at least 30 qualifying production-like executions. Only authenticated versioned qualification-session samples correlated to safe trace/projection server evidence qualify. An exact duplicate sample is an idempotent no-op; a conflicting duplicate is rejected. Any missing or forbidden tick, mixed origin, invalid ordering, non-authoritative state, absent localized live-region mutation, failed attestation/correlation, conflicting duplicate, or sample set with fewer than 30 qualifying executions yields `InsufficientEvidence`.

## Key Flows

### UJ-1 - Nora configures `hexa` for a tenant launch

1. Nora opens the Agents overview from the FrontComposer navigation.
2. She sees `hexa` as not callable because identity, provider/model, instructions, response mode, or approver policy is incomplete.
3. She opens `hexa` configuration and confirms or links the Agent Party identity.
4. She selects a provider/model whose authoritative readiness result says `Callability == Callable`; a callable `Degraded` result remains visibly Warning.
5. She enters Agent Instructions.
6. She chooses Automatic Response Mode or Confirmation Response Mode.
7. If confirmation is enabled, she configures the Approver Policy.
8. She reviews readiness blockers and submits activation; the UI moves from `submitted` to `authoritative pending` only after rendering the accepted identity and projection/version.
9. The projection-confirmed activation result shows lifecycle `active` separately from callability.
10. **Climax:** the readiness badge uses Success only when the current authoritative readiness projection proves `hexa` callable; active alone remains non-success.
11. Resolution: Nora returns to the overview and sees provider/model, lifecycle, response mode, callability, evidence freshness, and any safe blocker.

Failure path: no provider/model is enabled. Activation is blocked with a provider readiness reason; no Conversation user can call `hexa` until the provider issue is resolved.

### UJ-2 - Milan calls `hexa` from a Conversation and receives an automatic reply

1. Milan is in a Source Conversation where he has access.
2. He selects the Conversation-owned **Call hexa** action and submits a prompt after reviewing the effective response mode; the UI shows `submitted`, not Success.
3. The system checks Conversation access and Agent call permission. The UI shows `authoritative pending` only after it receives and renders the server/EventStore-accepted interaction identity plus projection/version.
4. The system loads V1 Conversation Context.
5. Generation starts; Milan sees authoritative in-flight state tied to the Source Conversation.
6. `hexa` generates a complete response.
7. Automatic Response Mode enters `posting pending`; the response is still not a Conversation Message.
8. The client receives and renders the authoritative projection/version whose terminal outcome is `posted`.
9. **Climax:** the Agent response now appears as durable Conversation content, attributed to `hexa`, not Milan or a generic system account; only this `posted` proof uses Success.
10. Resolution: participants continue the Conversation with the AI participant's answer visible.

Failure paths: Milan lacks permission -> authoritative rejection before Provider invocation, no generated content, proposal, or Conversation Message. Generation fails -> a separate non-approvable failure record is available to authorized viewers; no proposal or Conversation Message exists.

### UJ-3 - Anika approves a proposed `hexa` response before it enters the Conversation

1. A participant invokes `hexa` in a Conversation configured for Confirmation Response Mode.
2. The system creates a Proposed Agent Reply outside the Conversation after successful generation.
3. Anika discovers the pending proposal through the authorized in-product pending count, queue, or Conversation status entry.
4. She opens proposal detail/editor and reviews generated content, Source Conversation metadata, caller, Agent, provider/model, response mode, and expiry.
5. She edits the draft, creating a preserved edited version.
6. She requests regeneration once, creating a new generated version while preserving prior versions.
7. She selects the version that should be posted.
8. She approves the selected version; the proposal becomes `approved`, a non-success progress state.
9. The system begins posting exactly that version; the UI renders `posting pending`, still not a Conversation Message.
10. The client receives and renders the authoritative projection/version whose proposal outcome is `posted`.
11. **Climax:** the approved draft becomes a Conversation Message with approval evidence linked to the posted message; only now does the proposal use Success.
12. Resolution: the Conversation contains only the approved response; proposal history preserves generated, edited, regenerated, and approved versions.

Failure paths: the proposal expires before approval -> it becomes terminal, cannot post, and the UI routes Anika to start a new Agent Call. Generation fails before proposal creation -> only a separate non-approvable failure record exists; there is no proposal to discover, edit, regenerate, approve, or post.

### UJ-4 - Omar integrates Agent operations through the API

1. Omar reviews the public API/client contracts for Agents.
2. He lists provider options and Agent readiness for an authorized tenant.
3. He configures `hexa` or verifies configuration through stable contracts.
4. He inspects proposal status and audit evidence without using internal EventStore streams, projection internals, or provider SDK details.
5. He performs authorized proposal actions where policy allows and observes the same resource-and-operation-family pending scope and authoritative status flow as the UI.
6. **Climax:** Omar's integration can observe and operate governed Agent workflows with the same authorization outcomes as the admin UI.
7. Resolution: tenant automation can monitor readiness, failures, proposal queues, and audit completeness.

Failure path: credentials lack tenant or Agent permissions. API responses fail closed with structured errors that do not reveal unrelated tenant records.

### UJ-5 - Priya governs a production-like launch

1. Priya opens Launch readiness and sees every failed, insufficient, stale, and not-yet-evaluated gate before any passing summary.
2. She verifies the read-only complete-context-or-blocked policy and effective model budget.
3. She authors and validates a Content Safety policy version, confirms its future-only effect, and publishes it; the UI renders `submitted`, then `authoritative pending`, and waits for a projection-confirmed terminal result before showing Success.
4. She configures numeric monthly tenant budget and per-call caps, then verifies usage/reservations, the 80% warning, and the 100% blocked state.
5. She inspects the 365-day retention rule, legal-hold status, and authorized export/deletion controls.
6. She reviews the exact NFR-14 gates: page usability p95 <= 2.5 seconds, authoritative pending acknowledgement p95 <= 500 milliseconds, and terminal render/live-region announcement p95 <= 2 seconds, using their authoritative monotonic timestamp seams.
7. She verifies at least 30 qualifying production-like executions for each sample kind, SM-2/SM-3 cohort/window, Level 4–5 evidence, and current `ObservedAt`/exclusive `ValidUntil`; missing timestamps, references, live-region mutation, correlation, or samples remain `InsufficientEvidence`.
8. **Climax:** production-like callability appears only when every applicable current gate is proven, sufficiently sampled, and version-linked.
9. Resolution: Priya can identify the exact owner and recovery action for any later regression without exposing sensitive content.

Failure path: policy publication, budget reservation, export, deletion, or projection confirmation partially fails. The surface remains pending or restrictive, announces the failure accessibly, and never presents partial application as success.

## UX Closure Decisions — 2026-08-02

- The Conversation-owned **Call hexa** action is the only V1 invocation entry.
- Proposal notification is in-product only through pending-count, queue, and status-entry surfaces.
- Proposal expiry defaults to 24 hours and may be configured from 1 hour through 30 days for future proposals; existing `ExpiresAt` values do not move.
- Complete Conversation Context is read-only policy: use all of it or fail closed. No bounded-context controls appear.
- Safety, cost, launch-readiness, and audit-governance authoring/inspection surfaces are required and follow the policy/evidence rules above.
- Agent lifecycle `active` is separate from callability; Agent-readiness Success requires current authoritative proof that the Agent is callable.
- Provider `Degraded` is Warning and permits calls only when the architecture result says `Callability == Callable`.
- `Approved` and `posting pending` remain progress states; only `posted` proves the Conversation Message exists.
- Failed generation creates no Proposed Agent Reply. Retained content belongs only to a separate non-approvable failure record.
- High-risk pending-command locks are advisory and scoped to user session + resource + operation family, never the whole session.
- WCAG 2.2 AA, whole-string localization, English/French key parity, exact NFR-14 thresholds, authoritative timestamp seams, at least 30 qualifying executions per sample kind, and `InsufficientEvidence` behavior are binding launch evidence.
- All accepted high-impact commands follow `submitted -> authoritative pending -> projection-confirmed terminal` and remain pending or restrictive until authoritative projections/evidence confirm success; authoritative rejection before acceptance is terminal without an authoritative-pending state.
