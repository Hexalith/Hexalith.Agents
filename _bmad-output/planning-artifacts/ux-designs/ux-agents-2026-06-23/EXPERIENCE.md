---
name: Hexalith Agents
status: final
created: 2026-06-23
updated: 2026-08-01
sources:
  - ../../briefs/brief-agents-2026-06-23/brief.md
  - ../../prds/prd-agents-2026-06-23/prd.md
  - ../../prds/prd-agents-2026-06-23/addendum.md
  - ../../prds/prd-agents-2026-06-23/reconcile-brief.md
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

> Final spine reconciled through the approved 2026-08-01 Correct Course decision. `DESIGN.md` owns visuals; this file owns behavior, surfaces, states, accessibility, and flows. The spines win on conflict with mockups, wireframes, imports, and source-derived sketches.

## Foundation

Hexalith Agents is a desktop-first responsive web experience composed through **Hexalith.FrontComposer**. The shell owns header, navigation, account controls, theme/settings, command palette, skip links, keyboard shell behavior, shell localization, and global status chrome. The Agents domain owns registered navigation entries, page bodies, domain copy, Agent/proposal workflow behavior, and BFF/API-facing interaction.

The UI system is inherited: Microsoft Fluent UI Blazor v5 through FrontComposer. `DESIGN.md` is the visual identity reference. This experience spine references `DESIGN.md` component tokens by name and does not restate their visual styling.

Stakes are internal/governed operational, not regulated UX. Product-required authorization, tenant isolation, approval, proposal versioning, provider-secret safety, and audit evidence remain first-class. The copy should be plain and precise, without extra regulated-industry ceremony unless a later governance decision requires it.

V1 exposes only `hexa` as the product behavior, even if implementation uses generalized Agent structures. V1 excludes long-term memory, tools, project/folder content, ambient triggers, external channels, and business actions beyond posting automatic or approved replies to Conversations.

## Information Architecture

FrontComposer shell navigation should register an **Agents** domain/category. Candidate entries, ordered from operational setup to workflow handling:

| Surface | Reached from | Purpose |
|---|---|---|
| **Agents overview** | Agents nav default | See `hexa` readiness, lifecycle, response mode, provider/model, pending proposal count, recent failures, and callability for the tenant. |
| **`hexa` configuration** | Agents overview | Configure identity, display metadata, instructions, provider/model, response mode, approver policy, lifecycle, and activation blockers. |
| **Provider catalog** | Agents nav or `hexa` configuration | Manage provider/model records, enabled state, capability metadata, and secret-backed configuration without exposing secrets. |
| **Approver policy** | `hexa` configuration | Define who may edit, regenerate, approve, reject, abandon, or resolve proposals: conversation owner, caller, predefined Parties, tenant roles, or future confirmed policy sources. |
| **Conversation context policy** | `hexa` configuration, readiness | Read the effective complete-context-or-blocked rule, model budget, policy version, and blocking reason; V1 exposes no bounded-context authoring. |
| **Content safety policy** | Agents nav or `hexa` configuration | Author and publish versioned blocked/restricted handling, validate the policy, and inspect effective safety readiness. |
| **Cost controls** | Agents nav, provider catalog, readiness | Configure monthly tenant budget and per-call caps; inspect usage, reservations, 80% warning, and 100% blocked state. |
| **Conversation invocation** | Source Conversation | Party uses the sole Conversation-owned **Call hexa** action and submits a prompt through its accessible prompt surface. |
| **Proposal queue** | Agents nav, notification entry, Conversation status | Authorized approvers discover Proposed Agent Replies waiting for action and historical proposals they may inspect. |
| **Proposal detail/editor** | Proposal queue, notification, Conversation status | Review generated content, context metadata, version history, edits, regeneration, approval, rejection, abandonment, expiry, and posting outcome. |
| **Launch readiness** | Agents overview, operational status | Inspect latency gates, sample sufficiency, SM-2/SM-3 cohort and window, evidence levels, and every remaining blocker. |
| **Operational status** | Agents overview, proposal detail, provider catalog | Distinguish readiness, configuration errors, authorization failures, safety/budget blocks, provider failures, generation failures, pending approvals, posting failures, and successful posts. |
| **Audit governance** | Agents nav, audit evidence | Configure retention/legal hold and perform authorized export/deletion with restrictive progress and partial-failure states. |
| **Audit evidence** | Agents overview, proposal detail, status entry, posted response reference | Inspect support-safe evidence for configuration, provider/model changes, calls, versions, approvals, rejections, expirations, automatic posts, and final Conversation Messages. |
| **API/client contract reference** | Developer docs, not primary UI | Omar's integration journey needs stable public operations, but docs are not a FrontComposer admin screen unless product later asks for an in-app developer surface. |

Surface closure status: **final**. Admin/configuration, provider governance, full-context policy, safety, cost, **Call hexa**, in-product proposal notification, proposal workflow, launch readiness, audit governance, status, and evidence surfaces are defined. Implementation remains gated by Epic 5 and the external Conversations AI-membership prerequisite.

## Voice and Tone

Microcopy is operational, direct, and source-of-truth aware. Brand/aesthetic posture lives in `DESIGN.md`.

| Do | Don't |
|---|---|
| "`hexa` is active and callable." | "`hexa` is ready to help!" |
| "Generation failed. No Conversation Message was posted." | "Something went wrong." |
| "Proposal pending approval." | "Reply sent." |
| "Approved version posted to the Conversation." | "Approved successfully" when posting is still pending |
| "You do not have permission to approve this proposal." | "Forbidden 403" |
| "Provider is disabled. Calls are blocked until reconfigured." | "Provider unavailable" with no recovery path |
| "This proposal expired. Start a new Agent call." | "Try approving again" after terminal expiry |
| "Prior generated versions are preserved." | "Old draft replaced." |

Forbidden copy patterns:

- Do not call unapproved generated content a "message".
- Do not imply an automatic response or approved proposal was posted until posting is confirmed.
- Do not expose provider secrets, raw payloads, stack traces, raw tenant data from other scopes, or provider SDK errors.
- Do not use mascot-like copy for `hexa`; `hexa` is a named Agent participant.

All domain copy is localizable as whole strings with named placeholders. Shell chrome strings remain FrontComposer-owned per FC-L10N.

## Component Patterns

Behavioral rules only. Visual specs live in `DESIGN.md.Components`.

| Component | Behavioral rules |
|---|---|
| **agent-readiness-badge** | Summarizes whether `hexa` can be called. It must explain blockers, not hide them. Active lifecycle alone is insufficient if Party identity, provider/model, instructions, response policy, or approver policy is invalid. |
| **provider-status-badge** | Shows usable, disabled, degraded, failed, or historical state without revealing secrets. Provider failures and disabled states block generation before provider invocation. |
| **proposal-state-badge** | Renders proposal lifecycle states distinctly. Approved, posted, rejected, abandoned, and expired are terminal or near-terminal concepts and must not be confused with pending approval. |
| **response-mode-toggle** | Mutually exclusive automatic vs confirmation response mode. Mode changes apply to future Agent Calls only. The UI must make that future-only effect explicit. |
| **agent-config-form** | Validates required fields before activation. Activation blockers are inline and actionable. Configuration changes are auditable and should show what changed where safe to expose. |
| **approver-policy-builder** | Builds approval authority from configured sources. Each source row names the policy basis. Ambiguous or unavailable policy sources fail closed and block confirmation-mode activation. |
| **provider-catalog-grid** | Lists provider/model options, enabled state, capability metadata, and readiness. Secret-backed fields show configured/not configured, never secret values. |
| **proposal-queue-grid** | Shows pending proposals and authorized historical proposals. Filters should support "needs my action", state, Agent, source Conversation, caller, and expiry where data exists. Empty and filtered-empty are distinct. |
| **proposal-editor** | Lets authorized approvers edit content, request regeneration, approve selected version, reject, or abandon while non-authorized viewers see read-only state. Editing creates a new preserved version. |
| **version-history** | Lists every generated, edited, and regenerated version. Approval identifies exactly which version was approved and posted. Regeneration never deletes earlier versions. |
| **conversation-agent-call** | The Conversation-owned **Call hexa** action captures Source Conversation, caller, Agent, prompt, effective response mode, authorization decision, and timestamp. Mentions, commands, ambient triggers, and alternate V1 entry points are absent. |
| **conversation-context-policy-panel** | Read-only full-or-blocked policy, effective context/output budget, policy version, and safe block reason. It never offers truncation, summarization, windowing, or retrieval controls. |
| **content-safety-policy-editor** | Shows fixed blocked categories and restricted handling, validates draft versions, and requires authorized confirmation to publish a future-only version. Failed attempts cannot be retried through weaker policy. |
| **cost-control-editor** | Authors numeric monthly tenant budget and per-call caps. It exposes safe usage/reservation evidence, an 80% warning, 100% block, and indeterminate-state block without revealing Provider secrets. |
| **launch-readiness-panel** | Shows fixed latency/metric thresholds, cohort/window, at least-30-run sufficiency, Level 4/5 evidence, and blockers. Missing timestamps, insufficient samples, lower evidence, or skips cannot render as pass. |
| **audit-governance-panel** | Supports the 365-day policy, legal holds, encrypted time-limited export, and cryptographic deletion/projection purge. Success appears only after authoritative evidence; partial failure stays restrictive and actionable. |
| **proposal-notification** | In-product pending count and status entry only. It links authorized users to the queue, includes state/expiry without content, and never implies posting. Email, push, and external channels are absent. |
| **operational-status-panel** | Groups readiness and runtime failures by recovery: configure provider, fix policy, wait for approval, retry generation, inspect audit, start a new call. Avoid raw subsystem labels as the primary message. |
| **audit-evidence-panel** | Presents support-safe evidence and references. It links caller, Agent, Source Conversation, provider/model, response mode, versions, approver, approval/posting outcome, timestamps, and final Conversation Message where applicable. |

## State Patterns

Canonical states should be used consistently across surfaces. Exact implementation token names can be refined by architecture, but UX must preserve these distinctions.

### Agent Readiness

| State | Meaning | Treatment |
|---|---|---|
| `callable` | Agent lifecycle active and all required dependencies valid | Allow calls; show readiness as proven. |
| `checking` | Readiness is being evaluated | Calls are not confirmed ready; show progress. |
| `invalid configuration` | Required Agent fields, instructions, response mode, or policy missing | Block activation/calls with inline blockers. |
| `missing party identity` | Agent Party identity missing, disabled, ambiguous, or unauthorized | Block posting and calls that would need posting. |
| `provider unavailable` | Provider/model missing, disabled, or failed | Block provider invocation. |
| `disabled` | Agent disabled by lifecycle | Calls rejected before provider invocation. |

### Provider And Model

| State | Meaning | Treatment |
|---|---|---|
| `enabled` | Provider/model selectable and usable | Eligible for Agent configuration. |
| `disabled` | Provider/model cannot be selected for new active use | Existing Agents cannot be called until reconfigured unless read-only migration state is defined. |
| `degraded` | Provider is available with launch-defined warnings | Calls may continue only if policy allows; status remains visible. |
| `failed` | Provider/runtime failure | No Conversation Message or approvable proposal from incomplete generation. |
| `not configured` | Secret-backed or required provider data missing | Block selection/calls; never expose secret details. |

### Agent Call

| State | Meaning | Treatment |
|---|---|---|
| `requested` | Caller initiated explicit Agent Call | Capture Source Conversation, caller, prompt, Agent, timestamp. |
| `authorized` | Call permission and Conversation access passed | Continue to context build. |
| `denied` | Authorization failed | Stop before provider invocation; show safe reason. |
| `context loading` | V1 full Conversation Context loading | Show progress; do not invoke provider yet. |
| `context blocked` | Complete context cannot be loaded safely or exceeds the selected model's effective budget | Fail closed before Provider invocation; no truncation, summary, proposal, or message. |
| `generating` | Provider request in progress | Show in-flight state; no message/proposal yet. |
| `generation failed` | Provider/runtime/policy failure | No Conversation Message; no approvable proposal unless complete generated content exists and is explicitly marked audit-only. |
| `generated` | Complete Agent Response produced | Automatic mode proceeds to posting; confirmation mode creates Proposed Agent Reply. |

### Proposal Lifecycle

| State | Meaning | Treatment |
|---|---|---|
| `generated` | Initial generated version exists outside Conversation | Discoverable by authorized approvers. |
| `edited` | Approver created an edited version | Prior versions remain visible. |
| `regenerated` | New generated version created | Prior generated/edited versions remain visible. |
| `pending approval` | Awaiting authorized approver action | Queue-visible and notification-eligible. |
| `approved` | A selected version was approved | Posting may still be pending; do not call it posted yet. |
| `rejected` | Terminal; will not post | Preserve versions and evidence. |
| `abandoned` | Terminal manual end without posting | Preserve versions and evidence. |
| `expired` | Terminal policy expiry | Cannot post; start a new Agent Call. |
| `posting pending` | Approved version is being posted to Conversation | In-flight; not yet a Conversation Message. |
| `posted` | Final Conversation Message exists as `hexa` | Link approval/call evidence to posted message. |
| `posting failed` | Approved version did not become Conversation Message | Show recovery/status without fabricating success. |

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
- One high-risk side effect at a time per user/session unless architecture explicitly confirms a concurrent command policy. This follows the Tenants/FrontComposer precedent.
- Editing a proposal is explicit. Regeneration is a distinct action. Approval applies to a selected version only.
- `Esc` closes transient UI without committing. Focus returns to the triggering proposal row/action.
- Approval and rejection controls require keyboard reachability and clear focus order.
- Hover may reveal secondary actions on desktop, but no required action or denial reason is hover-only.
- **Call hexa** is the sole V1 invocation primitive. It shows the Agent name and effective response mode, captures the prompt, checks permission before Provider invocation, and never renders unapproved content as a Conversation Message.
- High-impact policy publish, legal-hold, export, deletion, and approval actions require current authorization, explicit confirmation, and projection/evidence-confirmed success. Policy changes state their future-only effect.
- Two or more titled sections use `FluentAccordion`; all custom surfaces remain FrontComposer/Fluent UI Blazor v5 components.

## Accessibility Floor

Behavioral accessibility. Visual contrast and icon/color pairing live in `DESIGN.md`.

- Use FrontComposer FC-A11Y primitives: skip links, focus visibility, named navigation landmarks, keyboard shell controls, and status live regions.
- Every status badge has visible text and an accessible name; color is never the sole signal.
- Proposal queue, provider catalog, and audit/status grids expose table semantics, header relationships, sort/filter state, and row action names.
- The proposal editor must be fully keyboard-operable: edit, select version, compare metadata, regenerate, approve, reject, abandon, and exit without committing.
- Live regions announce important transitions: generation failed, proposal created, proposal expired, approval posted, posting failed, permission denied. Avoid assertive announcements for ordinary pending progress.
- Focus-trapped dialogs or confirmation panels must provide a safe non-committing escape and return focus to the trigger.
- Reduced motion users must not depend on animation to perceive generation, approval, or posting state changes.
- Provider secrets, raw payloads, and unrelated tenant data must not appear in accessible names, tooltips, copied text, diagnostics, or announcements.

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

Fail-closed responsive rule: if a viewport cannot show enough context for an approval/posting decision, the high-impact action is unavailable with a visible reason. Review-only access remains available.

## FrontComposer Readiness

| Capability | UX dependency |
|---|---|
| FC-LYT | FullWidth for grids/status; Constrained for configuration, policy, and proposal editor flows. |
| FC-TBL | Provider catalog, proposal queue, status/audit grids, row detail, filter summaries, empty/filter-empty/error states. |
| FC-A11Y | Shell skip links, focus behavior, live-region primitives, override diagnostics. Required for all custom proposal/editor overrides. |
| FC-L10N | Shell/domain localization split. Agents owns domain labels and workflow copy; shell owns chrome strings. |
| Policy-gated nav | Provider/admin/proposal entries must hide or deny according to authorization without leaking records. |
| Pending command/status patterns | Use for generation, approval, and posting transitions where applicable; do not promote pending to success. |

## Key Flows

### UJ-1 - Nora configures `hexa` for a tenant launch

1. Nora opens the Agents overview from the FrontComposer navigation.
2. She sees `hexa` as not callable because identity, provider/model, instructions, response mode, or approver policy is incomplete.
3. She opens `hexa` configuration and confirms or links the Agent Party identity.
4. She selects an enabled provider/model from the governed provider catalog.
5. She enters Agent Instructions.
6. She chooses Automatic Response Mode or Confirmation Response Mode.
7. If confirmation is enabled, she configures the Approver Policy.
8. She reviews readiness blockers and activates `hexa`.
9. **Climax:** `hexa` becomes active and callable only after every required gate is valid.
10. Resolution: Nora returns to the overview and sees provider/model, lifecycle, response mode, and callability.

Failure path: no provider/model is enabled. Activation is blocked with a provider readiness reason; no Conversation user can call `hexa` until the provider issue is resolved.

### UJ-2 - Milan calls `hexa` from a Conversation and receives an automatic reply

1. Milan is in a Source Conversation where he has access.
2. He selects the Conversation-owned **Call hexa** action and submits a prompt after reviewing the effective response mode.
3. The system checks Conversation access and Agent call permission before provider invocation.
4. The system loads V1 Conversation Context.
5. Generation starts; Milan sees an in-flight state tied to the Source Conversation.
6. `hexa` generates a complete response.
7. Automatic Response Mode posts the response to the same Conversation as `hexa`'s Party identity.
8. **Climax:** the Agent response appears as durable Conversation content, attributed to `hexa`, not Milan or a generic system account.
9. Resolution: participants continue the Conversation with the AI participant's answer visible.

Failure path: Milan lacks permission. The call is rejected before provider invocation, no generated content exists, and no Conversation Message is posted.

### UJ-3 - Anika approves a proposed `hexa` response before it enters the Conversation

1. A participant invokes `hexa` in a Conversation configured for Confirmation Response Mode.
2. The system creates a Proposed Agent Reply outside the Conversation after successful generation.
3. Anika discovers the pending proposal through the authorized in-product pending count, queue, or Conversation status entry.
4. She opens proposal detail/editor and reviews generated content, Source Conversation metadata, caller, Agent, provider/model, response mode, and expiry.
5. She edits the draft, creating a preserved edited version.
6. She requests regeneration once, creating a new generated version while preserving prior versions.
7. She selects the version that should be posted.
8. She approves the selected version.
9. The system posts exactly that version to the Source Conversation as `hexa`.
10. **Climax:** the approved draft becomes a Conversation Message with approval evidence linked to the posted message.
11. Resolution: the Conversation contains only the approved response; proposal history preserves generated, edited, regenerated, and approved versions.

Failure path: the proposal expires before approval. It becomes terminal, cannot post, and the UI routes Anika to start a new Agent Call if a response is still needed.

### UJ-4 - Omar integrates Agent operations through the API

1. Omar reviews the public API/client contracts for Agents.
2. He lists provider options and Agent readiness for an authorized tenant.
3. He configures `hexa` or verifies configuration through stable contracts.
4. He inspects proposal status and audit evidence without using internal EventStore streams, projection internals, or provider SDK details.
5. He performs authorized proposal actions where policy allows.
6. **Climax:** Omar's integration can observe and operate governed Agent workflows with the same authorization outcomes as the admin UI.
7. Resolution: tenant automation can monitor readiness, failures, proposal queues, and audit completeness.

Failure path: credentials lack tenant or Agent permissions. API responses fail closed with structured errors that do not reveal unrelated tenant records.

### UJ-5 - Priya governs a production-like launch

1. Priya opens Launch readiness and sees every failed, insufficient, stale, and not-yet-evaluated gate before any passing summary.
2. She verifies the read-only complete-context-or-blocked policy and effective model budget.
3. She authors and validates a Content Safety policy version, confirms its future-only effect, and publishes it; the UI waits for authoritative projection evidence before showing success.
4. She configures numeric monthly tenant budget and per-call caps, then verifies usage/reservations, the 80% warning, and the 100% blocked state.
5. She inspects the 365-day retention rule, legal-hold status, and authorized export/deletion controls.
6. She reviews latency samples, SM-2/SM-3 cohort/window, and Level 4–5 evidence.
7. **Climax:** production-like callability appears only when every current gate is proven, sufficiently sampled, and version-linked.
8. Resolution: Priya can identify the exact owner and recovery action for any later regression without exposing sensitive content.

Failure path: policy publication, budget reservation, export, deletion, or projection confirmation partially fails. The surface remains pending or restrictive, announces the failure accessibly, and never presents partial application as success.

## UX Closure Decisions — 2026-08-01

- The Conversation-owned **Call hexa** action is the only V1 invocation entry.
- Proposal notification is in-product only through pending-count, queue, and status-entry surfaces.
- Proposal expiry defaults to 24 hours and may be configured from 1 hour through 30 days for future proposals; existing `ExpiresAt` values do not move.
- Complete Conversation Context is read-only policy: use all of it or fail closed. No bounded-context controls appear.
- Safety, cost, launch-readiness, and audit-governance authoring/inspection surfaces are required and follow the policy/evidence rules above.
- All high-impact outcomes remain pending or restrictive until authoritative projections/evidence confirm success.
