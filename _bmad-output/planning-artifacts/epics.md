---
stepsCompleted:
  - step-01-validate-prerequisites
  - step-02-design-epics
  - step-03-create-stories
  - step-04-final-validation
inputDocuments:
  - /home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md
  - /home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
  - /home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md
  - /home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md
---

# Hexalith Agents - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Hexalith Agents, decomposing the requirements from the PRD, UX Design if it exists, and Architecture requirements into implementable stories.

## Course Correction Authority — 2026-08-01

Epics 1–4 and their completed stories remain unchanged below as historical delivery evidence. They do not establish production readiness and must not be reinterpreted as implementing later-bound live seams. The approved 2026-08-01 PRD Decision Register, corrected Architecture Spine (including AD-10, AD-16, and AD-18), final UX spines, and Epic 5 take precedence over any conflicting wording in those historical story acceptance criteria. Epic 5 is the only forward implementation plan; Story 5.18 must create a new implementation-readiness assessment while the 2026-08-01 `NOT READY` report remains immutable evidence.

## Requirements Inventory

### Functional Requirements

FR1: Agent Administrators can create or enable `hexa` with a stable Agent identity, display name, description, Agent Instructions, lifecycle state, and tenant scope; activation is blocked when required fields are missing or invalid, configuration is exposed through admin UI and API/client contracts, and configuration changes are audited with safe prior/new values.

FR2: Agent Administrators can provision or link the Agent's Party identity so `hexa` appears as a known AI participant when posting to a Conversation; an active Agent has exactly one Party identity, posting is rejected when the identity is missing, disabled, ambiguous, or unauthorized, and posted messages are attributed to the Agent's Party identity.

FR3: Agent Administrators can activate, disable, and inspect `hexa` lifecycle state; disabled Agents cannot be called, disabling preserves prior evidence and messages, and lifecycle changes are auditable and visible through admin UI and API/client contracts.

FR4: Authorized administrators can configure the Global Providers Aggregate with provider records, model options, enabled/disabled state, and provider capability metadata; disabled providers/models cannot be selected for new active use, existing Agents using disabled providers/models cannot be activated or called until reconfigured, and provider changes are auditable without secret exposure.

FR5: Agent Administrators can select a Provider and model for `hexa` from the Global Providers Aggregate; selected provider/model state is validated before activation, enough provider/model identity is retained for audit, and selection changes affect only future Agent Calls.

FR6: Agent Administrators can configure `hexa` for Automatic Response Mode or Confirmation Response Mode; automatic mode posts successful responses directly after authorization and generation, confirmation mode creates Proposed Agent Replies outside the Conversation, and response mode changes affect only future Agent Calls.

FR7: Agent Administrators can define all Approvers through the Agent's Approver Policy, including Conversation owner, caller, predefined Parties, or tenant roles; proposal actions are authorized through the policy, unauthorized actions are rejected, policy basis disclosure is categorized consistently, and each approval-related decision records its policy basis.

FR8: Authorized Conversation Participants can explicitly call `hexa` from a Source Conversation with a prompt or request; calls require Source Conversation access and Agent call permission, unauthorized calls fail before provider invocation, and each call records caller, Agent, Source Conversation, request timestamp, and response mode.

FR9: The system supplies the Agent with the complete authorized Source Conversation or fails closed before Provider invocation; V1 excludes long-term memory/project/folder/tool/external-channel content, never truncates, summarizes, windows, samples, or substitutes retrieval, records policy identity and the full-or-blocked outcome, and creates no proposal or message when complete context cannot be loaded or fit safely.

FR10: The system handles provider failures, timeout, disabled provider/model state, invalid context, Content Safety Policy failures, and policy failures without posting incomplete or unsafe Agent Responses; failures create authorized status and Audit Evidence, do not create Conversation Messages, and do not create approvable proposals unless complete generated content exists and is explicitly audit-only.

FR11: In Automatic Response Mode, the system posts successful generated content to the Source Conversation as a Conversation Message attributed to the Agent's Party identity; the posted message references the Agent Call or trace identifier, is not authored by the caller, and is linked to Audit Evidence.

FR12: The system prevents automatic posting when authorization, Agent lifecycle, Provider/model, Party identity, Source Conversation access, Conversation Context Policy, Content Safety Policy, or generation status is invalid; no Conversation Message is created on failed checks or safety failure, and authorized status/audit distinguish failure classes without leaks.

FR13: In Confirmation Response Mode, successful generation creates a Proposed Agent Reply linked to the Source Conversation and Agent Call; it is not a Conversation Message, records caller/Agent/source/generated version/provider/model/response mode/current state, is discoverable by authorized Approvers, and V1 includes an in-product pending-proposal visibility surface.

FR14: The system preserves every generated, edited, and regenerated content version for each Proposed Agent Reply; edits and regenerations create immutable version entries and approval identifies exactly which version was approved and posted.

FR15: Authorized Approvers can edit Proposed Agent Reply content before approval; only authorized Approvers can edit, edits preserve prior versions and edit authorship, and edited content remains outside the Conversation until approved.

FR16: Authorized Approvers can request regeneration of a Proposed Agent Reply before approval; regeneration uses the same Source Conversation and Agent configuration unless an explicit configuration-version change is recorded, preserves prior versions, creates a new generated version, and is blocked after terminal states.

FR17: Authorized Approvers can approve a selected proposal version, causing exactly that version to be posted to the Source Conversation as `hexa`; the message is attributed to the Agent's Party identity and Audit Evidence links the approved version, Approver, approval timestamp, and posted Conversation Message.

FR18: Authorized Approvers or system policy can move a Proposed Agent Reply to rejected, abandoned, or expired terminal states; terminal proposals cannot be approved or posted, preserve all versions for audit, and expose deterministic expiry through UI/API. Expiry defaults to 24 hours, is configurable per Agent from 1 hour through 30 days for future proposals only, and is executed by a Dapr Workflow timer at or after stored `ExpiresAt`.

FR19: The system enforces tenant isolation across Agent configuration, Provider selection, Agent Calls, Conversation Context, Proposed Agent Replies, Conversation posting, and Audit Evidence; cross-tenant call/inspect/approve/post actions are impossible, provider and Agent configuration does not leak across tenants unless explicitly platform-scoped and authorized, and audit/status queries return only tenant-authorized records.

FR20: The system enforces authorization for Agent administration, provider administration, Agent calling, proposal discovery, editing, regeneration, approval, rejection, abandonment, posting, and audit inspection; authorization failures happen before provider invocation or Conversation posting, admin UI and API/client contracts use the same rules, and authorization decisions are auditable without sensitive leakage.

FR21: The system fails closed when required Party, Conversation, Provider, Agent, tenant access, or approval policy state is missing, stale, ambiguous, disabled, or unavailable; missing or stale Conversation access blocks calls and approval posting, missing or disabled Agent Party identity blocks posting, and missing Provider/model state blocks generation.

FR22: The admin UI allows authorized administrators to manage Global Providers Aggregate entries, configure `hexa`, inspect lifecycle state, configure response and approver policy, and view Agent operation/proposal status; UI actions enforce the same authorization rules as API/client contracts, never expose Provider secrets, and distinguish active, disabled, invalid, pending proposal, failed call, and expired proposal states.

FR23: The system exposes stable API/client contracts for Provider administration, Agent configuration, Agent Calls, proposal workflow, status inspection, and audit inspection; callers are not required to use raw EventStore/internal aggregate/internal projection/provider SDK details, responses are structured for automation, and breaking changes are avoided unless explicitly versioned.

FR24: The system captures Audit Evidence for Agent configuration, Provider/model configuration, Agent Calls, generation attempts, proposal versions, edits, regenerations, approvals, rejections, abandonments, expirations, automatic posts, and final Conversation Messages; posted responses trace back to caller/Agent/source/provider/model/content/approval path, proposals preserve all versions, policy outcomes and identifiers are recorded where available, and audit is tenant-authorized without leaking unrelated tenant data or Provider secrets.

FR25: The system exposes status for Agent readiness, Provider/model readiness, recent Agent Call outcomes, proposal queues, generation failures, approval completion, and posting outcomes; authorized administrators can identify whether `hexa` is callable, distinguish key failure classes, and monitor launch adoption and approval workflow metrics.

FR26: Authorized administrators or release operators can define and publish the versioned Content Safety Policy for `hexa`; production or production-like validation cannot enable `hexa` without one. The policy binds always-blocked and restricted categories, prompt/context and output gates, failure/audit treatment, no Approver override, future-only changes, and no weaker retry.

FR27: The system applies Content Safety Policy to prompt plus complete Conversation Context before Provider invocation and to generated output before any proposal or Conversation side effect; failed content cannot be posted or approved, safety failures create authorized status and Audit Evidence without forbidden disclosure, and Approvers cannot override failures.

FR28: V1 launch readiness requires the fixed SM-2/SM-3 thresholds, latency targets, full-context behavior, hard cost controls, audit governance, and Level 4–5 live evidence recorded in the PRD Decision Register and Architecture Spine. Production-like generation remains disabled when a gate is missing, stale, indeterminate, insufficiently sampled, skipped, or supported only by lower-level evidence.

### NonFunctional Requirements

NFR1: Security - Agent configuration, Provider administration, Agent Calls, proposal actions, posting, and audit inspection must enforce tenant and Party authorization before side effects.

NFR2: Privacy - Conversation Context, proposal content, and Audit Evidence must not leak across tenants or unauthorized Parties.

NFR3: Reliability - Agent Calls must never create partial Conversation Messages on failure, and Confirmation workflows must not lose generated or edited proposal versions.

NFR4: Observability - The system must expose enough status to debug configuration errors, Provider failures, authorization denials, pending approval bottlenecks, and posting failures.

NFR5: Auditability - Audit Evidence must preserve all generated and edited proposal versions and link final posted responses to their source call and approval path.

NFR6: Provider Safety - Provider secrets must be write-only or secret-backed where applicable and must never appear in logs, status payloads, audit records, or UI display.

NFR7: Content Safety - Agent generation must be governed by an active Content Safety Policy before generated content can create Conversation side effects.

NFR8: Context Bounds - Conversation Context is complete or blocked; truncation, summarization, windowing, sampling, and substitute retrieval are prohibited in V1.

NFR9: Performance - Automatic accepted-call-to-post and confirmation accepted-call-to-proposal are p95 ≤ 60 s and p99 ≤ 120 s; approval-to-post is p95 ≤ 10 s and p99 ≤ 30 s; known pre-Provider rejections are p95 ≤ 2 s; each supported gate requires at least 30 production-like executions.

NFR10: Cost Control - Hard numeric per-tenant monthly and per-call caps warn at 80%, fail closed at 100%, and use atomic maximum-cost reservation, actual-usage reconciliation, and retry-safe reservation reuse; reporting-only monitoring is insufficient.

### Additional Requirements

- Architecture does not specify an external starter template; Epic 1 Story 1 should scaffold the `Hexalith.Agents` module from the architecture Structural Seed using `.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, and the planned workspace-root `src/` and `test/` project layout.
- Hexalith Agents must be implemented as a full EventStore-backed Hexalith domain module owning durable Agent configuration, provider governance, Agent interactions, proposal versions, approval decisions, posting outcomes, audit evidence, and operational status.
- Use separate aggregate boundaries: `Agent` for identity/lifecycle/instructions/provider selection/response and approver policy; `ProviderCatalog` for provider/model records/capability metadata/enablement/secret references; `AgentInteraction` for each call, generation attempt, proposal lifecycle, version history, approval/rejection/abandonment/expiry, automatic-post evidence, and posting outcome.
- Keep aggregates pure: aggregate handlers emit events only, while provider calls, Conversations reads/posts, Parties validation/provisioning, Tenants projection reads, expiry timers, and notifications run in application orchestration/adapters and return through commands.
- Snapshot the Agent configuration version, instructions version, response mode, approver policy version, `ProviderId`, `ModelId`, provider capability version, caller `PartyId`, source `ConversationId`, and context-build policy at request time for every `AgentInteraction`.
- Enforce append-only proposal lifecycle: generated, edited, regenerated, approved, rejected, abandoned, expired, posting pending, posted, and posting failed states preserve immutable version history; approval selects exactly one version; rejected/abandoned/expired interactions cannot later post.
- Interact with Conversations only through supported `Hexalith.Conversations.Client` or API boundaries, especially context loading and final message posting. Proposed Agent Replies must never be treated as Conversation Messages, and direct Conversation stream/event writes are forbidden.
- Establish Agent membership only through an official Conversations `AddParticipant` command/API/client boundary; if absent at implementation start, exposing that Conversations-owned membership boundary is a prerequisite.
- Store stable `PartyId` references only, validate/provision identity through Parties adapters, and fail closed before posting when Party or membership state is missing, disabled, ambiguous, unavailable, or unauthorized.
- Resolve ApproverPolicy through Agents-owned configuration sources: caller `PartyId`, predefined `PartyId`s, tenant roles from local Tenants projection, and conversation authority from Conversations detail. Until Conversations exposes an explicit owner field, V1 treats Conversation Facilitator as owner authority.
- Hide provider SDKs and credentials behind Agents-owned generation adapters; public contracts and durable events expose only safe provider/model identifiers, capability metadata, usage/status, safe error classes, and secret reference/configured state.
- ProviderCatalog V1 capability metadata must include `ProviderId`, `ModelId`, display label, enabled state, secret reference/configured state, text-generation capability, context-window token limit, max-output token limit, timeout policy, optional safe capability flags, versioned pricing metadata, and monotonic `CapabilityVersion`.
- Build V1 context only from authorized Conversations detail and visible timeline content; if full source context cannot be loaded fresh enough or fit the selected model context budget after reserving output tokens, record a context-blocked failure and create no provider call, proposal, or Conversation Message.
- Run authorization gates before every side effect and fail closed on missing, stale, ambiguous, disabled, or unavailable dependency state. Tenant access comes from Agents' local Tenants projection; conversation access/context from Conversations authorized queries; Party state from Parties adapters/projections; provider readiness from ProviderCatalog projections; approver rights from the snapshotted policy plus current dependencies.
- Make external effects idempotent: generation attempts use deterministic attempt ids, Conversation posting uses deterministic `MessageId` and idempotency key derived from `AgentInteractionId` plus selected `VersionId`, and retries must not duplicate messages or versions.
- Treat prompt, generated, edited, and context-derived content as sensitive conversation-derived content. Use EventStore payload-protection/redaction conventions before production; disable content-bearing workflows if protection is unavailable. Logs, telemetry, status, and audit summaries must not contain raw content, provider payloads, stack traces, or secrets.
- Admin UI and API/client surfaces must share the same public Agents contracts and authorization outcomes. FrontComposer UI registers an Agents domain/navigation like Tenants and calls Agents API/BFF/client boundaries, not EventStore streams, provider SDKs, or aggregate internals.
- Hexalith Agents does not ship module-owned AppHost, Aspire, or ServiceDefaults projects. A platform-owned host composes the reusable EventStore DomainService SDK host, UI, dependencies, Provider/safety adapters, and Dapr Workflow.
- Public contracts must be versioned and additive-first, with evidence classified as Level 1 contract/structure, Level 2 pure domain/unit, Level 3 fail-closed deferred seam, Level 4 live component integration, and Level 5 cross-system production-like. Production readiness requires Levels 4–5 for every launch-critical seam.
- Dapr Workflow is the sole V1 durable owner for context preparation, generation, confirmation waits, proposal expiry, posting, retries, and recovery. Microsoft Agent Framework may be internal to a generation activity only. Python DurableAgent, MCP, A2A, tools, and alternative workflow owners are out of V1.
- Use the local Hexalith stack baseline: .NET `10.0.300`-`10.0.301`, `net10.0`, `.slnx`, Central Package Management, sibling source dependencies for EventStore/Conversations/Parties/Tenants/FrontComposer, Dapr Workflow `1.18.4`, MediatR `14.1.0`, FluentValidation `12.1.1`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, xUnit v3 `3.2.2`, and Shouldly `4.3.0`.

### UX Design Requirements

UX-DR1: Register an Agents domain/category in the FrontComposer shell, with navigation entries ordered from operational setup to workflow handling: Agents overview, `hexa` configuration, Provider catalog, Approver policy, Conversation invocation entry, Proposal queue, Proposal detail/editor, Operational status, and Audit evidence.

UX-DR2: Implement the Agents overview as the default Agents navigation surface showing `hexa` readiness, lifecycle, response mode, provider/model, pending proposal count, recent failures, and tenant callability.

UX-DR3: Implement `hexa` configuration as a constrained FrontComposer/Fluent form for identity, display metadata, instructions, provider/model, response mode, approver policy, lifecycle, activation blockers, and safe configuration-change visibility.

UX-DR4: Implement Provider catalog as a full-width FrontComposer FC-TBL/FluentDataGrid surface showing provider/model options, enabled state, capability metadata, readiness, and secret configured/not-configured state without exposing secret values.

UX-DR5: Implement Approver policy builder rows for conversation owner/facilitator authority, caller, predefined Parties, tenant roles, and future confirmed sources, with readable policy basis and blocked state for missing or ambiguous sources.

UX-DR6: Implement Proposal queue as a full-width grid with pinned or priority columns for proposal state, Source Conversation, caller, current approver responsibility, expiry, and age; support "needs my action", state, Agent, source Conversation, caller, and expiry filters; distinguish empty from filtered-empty.

UX-DR7: Implement Proposal detail/editor as a bounded approval workspace with current selected version, editable content where authorized, source metadata, version actions, regeneration, approval, rejection, abandonment, expiry, and posting outcome controls.

UX-DR8: Implement Version history listing every generated, edited, and regenerated version with author/source, timestamp, provider/model where applicable, approval/posting markers, and clear preservation of prior versions after edit or regeneration.

UX-DR9: Implement Operational status panels that group readiness, recent call outcomes, generation failures, proposal bottlenecks, provider readiness, and posting outcomes by recovery action rather than raw subsystem labels.

UX-DR10: Implement Audit evidence panels that provide support-safe references linking caller, Agent, Source Conversation, provider/model, response mode, versions, approver, approval/posting outcome, timestamps, and final Conversation Message where applicable, without raw payloads, secrets, stack traces, or unrelated tenant data.

UX-DR11: Use Fluent semantic status roles consistently: Success only for proven usable/complete states; Informative for progress/waiting; Warning for attention soon; Severe for blocked but non-runtime-failure states; Danger for failure/denial; Important for unresolved uncertainty; Subtle for quiet history or non-actionable state; Brand only for chrome/eligible primary action and never as a status.

UX-DR12: Every status indicator must combine semantic color, icon, and visible text; color-only status is forbidden and status accessible names must not reveal secrets or unrelated tenant data.

UX-DR13: Inherit Fluent typography roles: heading for page/panel titles, body for operational copy and generated-response previews, label for fields/state labels, caption for metadata, and monospace for exact identifiers/timestamps/references. Do not introduce display typography for generated AI text.

UX-DR14: All labels, states, denial reasons, expiry notices, provider/model names, approval actions, and audit references must be localizable whole strings with named placeholders rather than runtime sentence fragments.

UX-DR15: Use FrontComposer page measures deliberately: FullWidth for provider catalog, proposal queue, status lists, and audit lists; Constrained for `hexa` configuration, provider edit, approver policy, proposal editor, and approval confirmation.

UX-DR16: Follow the 4px spacing rhythm from the UX frontmatter: spacing 4 between related form fields, spacing 6 between major sections, and spacing 8 only for page-level separation.

UX-DR17: Reserve stable space for status badges, action slots, expiry labels, and proposal state indicators so rows do not jump when proposals or calls change state.

UX-DR18: Use Fluent/FrontComposer elevation only for transient overlays, popovers, dialogs, and focus-trapped confirmation surfaces; do not use elevation to imply audit certainty or proposal importance.

UX-DR19: Inherit Fluent shapes and do not introduce custom radii for Agents surfaces; badges, chips, forms, editors, panels, dialogs, and grids follow Fluent/FrontComposer defaults.

UX-DR20: Implement `agent-readiness-badge` so it does not collapse active lifecycle and callability; it must combine lifecycle, Party identity, provider/model readiness, instruction validity, response mode, and approver policy completeness, and explain blockers.

UX-DR21: Implement `provider-status-badge` with usable, disabled, degraded, failed, not configured, and historical states, blocking generation before provider invocation when provider state requires it and never exposing secrets.

UX-DR22: Implement `proposal-state-badge` with distinct generated, edited, regenerated, pending approval, approved, rejected, abandoned, expired, posting pending, posted, and posting failed states. Approved must not be visually or textually treated as posted.

UX-DR23: Implement response mode selection as a mutually exclusive Fluent segmented control or radio group for Automatic Response Mode and Confirmation Response Mode, with copy that makes future-only effect explicit and does not visually bias automatic mode.

UX-DR24: Implement the Conversation-owned **Call hexa** action as the sole V1 invocation entry; it visibly names `hexa`, captures Source Conversation, caller, Agent, prompt, response mode, authorization decision, and timestamp before Provider invocation, and never renders unapproved content as a Conversation Message.

UX-DR25: Preserve canonical Agent readiness states across surfaces: callable, checking, invalid configuration, missing party identity, provider unavailable, and disabled.

UX-DR26: Preserve canonical Provider/model states across surfaces: enabled, disabled, degraded, failed, and not configured.

UX-DR27: Preserve canonical Agent Call states across surfaces: requested, authorized, denied, context loading, context blocked, generating, generation failed, and generated.

UX-DR28: Preserve canonical Proposal lifecycle states across surfaces: generated, edited, regenerated, pending approval, approved, rejected, abandoned, expired, posting pending, posted, and posting failed.

UX-DR29: Preserve audit availability states across surfaces: audit pending, audit available, audit delayed, and audit unavailable.

UX-DR30: Every grid/list surface must distinguish loading, empty, filtered-empty, error, permission-denied, and stale/degraded where relevant; empty states must not leak unauthorized records and filtered-empty states must offer filter reset.

UX-DR31: Editing a proposal must be explicit; regeneration must be a distinct action; approval applies only to a selected version; high-risk side effects should be limited to one active command per user/session unless architecture confirms a concurrent command policy.

UX-DR32: Keyboard and focus behavior must support `Esc` closing transient UI without committing, focus returning to the trigger, approval/rejection controls being keyboard reachable, and no required action or denial reason being hover-only.

UX-DR33: Accessibility must use FrontComposer FC-A11Y primitives including skip links, focus visibility, named navigation landmarks, keyboard shell controls, and status live regions.

UX-DR34: Proposal queue, provider catalog, and audit/status grids must expose table semantics, header relationships, sort/filter state, and row action names.

UX-DR35: Proposal editor must be fully keyboard operable for edit, selected-version choice, metadata comparison, regeneration, approval, rejection, abandonment, and exit without committing.

UX-DR36: Live regions must announce important transitions such as generation failed, proposal created, proposal expired, approval posted, posting failed, and permission denied, while avoiding assertive announcements for ordinary pending progress.

UX-DR37: Focus-trapped dialogs or confirmation panels must provide a safe non-committing escape and return focus to the triggering control.

UX-DR38: Reduced-motion users must not depend on animation to perceive generation, approval, or posting state changes.

UX-DR39: Responsive behavior must be desktop-first; phone may support read-only status/proposal reference/lightweight review, tablet stacks metadata/editor/version history and prioritizes grid columns, desktop is the primary mode, and wide desktop uses extra width for split views rather than decoration.

UX-DR40: Fail closed on constrained viewports: if a viewport cannot show enough context for approval/posting, the high-impact action is unavailable with a visible reason while review-only access remains available.

UX-DR41: Use FrontComposer capabilities intentionally: FC-LYT for FullWidth and Constrained layouts, FC-TBL for grids/filter summaries/row detail/empty/error states, FC-A11Y for shell and custom override accessibility, FC-L10N for domain labels and workflow copy, policy-gated navigation for authorization-safe entry visibility, and pending command/status patterns for generation/approval/posting transitions without promoting pending to success.

UX-DR42: Implement a read-only Conversation context policy surface showing complete-context-or-blocked behavior, effective model budget, policy version, and safe blocking reason; expose no bounded-context control.

UX-DR43: Implement Content Safety authoring for fixed blocked categories, restricted handling, policy validation/version, and explicit authorized publish with future-only and no-weaker-retry copy.

UX-DR44: Implement cost-control authoring/status for monthly tenant budget, per-call caps, usage/reservations, 80% warning, 100% block, and indeterminate fail-closed state.

UX-DR45: Implement launch readiness showing latency gates, SM-2/SM-3 cohort/window, sample sufficiency, evidence levels, and visible blockers; lower evidence, skips, and insufficient samples cannot render as pass.

UX-DR46: Implement audit governance for 365-day retention, legal hold, encrypted time-limited export, deletion/projection purge, and restrictive partial-failure state; high-impact actions require confirmation and authoritative evidence before success.

UX-DR47: Implement in-product proposal notifications only through authorized pending counts, queue links, and Conversation status entries; notification copy exposes state/expiry but not proposal content. Multi-section policy surfaces use `FluentAccordion` and all UI uses FrontComposer/Fluent UI Blazor v5.

### FR Coverage Map

FR1: Epic 1 - Configure `hexa` with stable identity, display metadata, instructions, lifecycle, and tenant scope.

FR2: Epic 1 - Link or provision the Agent Party identity used for attributed Conversation posting.

FR3: Epic 1 - Manage `hexa` lifecycle state and preserve prior evidence.

FR4: Epic 1 - Govern provider/model records through the Global Providers Aggregate.

FR5: Epic 1 - Select and validate the Provider/model used by `hexa`.

FR6: Epic 1 - Configure Automatic Response Mode or Confirmation Response Mode.

FR7: Epic 1 - Configure Approver Policy sources and policy-basis disclosure.

FR8: Epic 2 - Explicitly call `hexa` from a Source Conversation with authorization evidence.

FR9: Epic 2 - Build V1 Conversation Context according to context policy and fail closed on unsafe bounds.

FR10: Epic 2 - Handle generation, provider, context, timeout, safety, and policy failures without unsafe posting.

FR11: Epic 2 - Post successful automatic responses as `hexa` through Conversations.

FR12: Epic 2 - Prevent automatic posting when any policy or dependency gate fails.

FR13: Epic 3 - Create Proposed Agent Replies for Confirmation Response Mode.

FR14: Epic 3 - Preserve every generated, edited, and regenerated proposal version.

FR15: Epic 3 - Allow authorized Approvers to edit Proposed Agent Replies before approval.

FR16: Epic 3 - Allow authorized Approvers to regenerate Proposed Agent Replies before terminal states.

FR17: Epic 3 - Approve exactly one selected proposal version for posting as `hexa`.

FR18: Epic 3 - Reject, abandon, or expire Proposed Agent Replies as terminal states.

FR19: Epic 2 - Enforce tenant isolation across configuration, invocation, context, proposals, posting, and audit.

FR20: Epic 2 - Enforce role and policy authorization before provider invocation or Conversation posting.

FR21: Epic 2 - Fail closed when Party, Conversation, Provider, Agent, tenant access, or approval policy state is uncertain.

FR22: Epic 4 - Provide admin UI for provider administration, `hexa` configuration, lifecycle, policy, status, and proposal operations.

FR23: Epic 4 - Provide stable API/client contracts for administration, invocation, proposal workflow, status, and audit.

FR24: Epic 4 - Capture durable Audit Evidence for configuration, generation, proposals, approvals, posting, and final messages.

FR25: Epic 4 - Expose operational status for readiness, provider/model, calls, queues, failures, approvals, and posting outcomes.

FR26: Epic 1 - Configure active Content Safety and prompt policy before launch enablement.

FR27: Epic 2 - Enforce Content Safety Policy before Conversation Messages or approvable proposals exist.

FR28: Epic 4 - Define launch readiness controls for metrics, latency, context bounds, and cost posture.

## Epic List

### Epic 1: Tenant Agent Setup And Governance

Agent Administrators can configure `hexa` for a tenant with Party identity, provider/model selection, response mode, approver policy, lifecycle, and active content safety policy.

**FRs covered:** FR1, FR2, FR3, FR4, FR5, FR6, FR7, FR26

### Epic 2: Safe Conversation Invocation And Automatic Replies

Conversation Participants can explicitly call `hexa` from a Conversation and receive a governed automatic reply, with authorization, context bounds, safety, dependency uncertainty, and failure handling enforced before side effects.

**FRs covered:** FR8, FR9, FR10, FR11, FR12, FR19, FR20, FR21, FR27

### Epic 3: Proposal Review And Approval Workflow

Approvers can discover, edit, regenerate, approve, reject, abandon, or let Proposed Agent Replies expire, while all versions remain preserved and only an approved selected version can be posted.

**FRs covered:** FR13, FR14, FR15, FR16, FR17, FR18

### Epic 4: Operational Visibility, Audit, Integration, And Launch Readiness

Administrators, operators, and integration developers can manage Agents through UI/API contracts, inspect status and audit evidence, and enforce launch-readiness gates for metrics, latency, context, and cost posture.

**FRs covered:** FR22, FR23, FR24, FR25, FR28

### Epic 5: Production Binding And Live Conformance

Operators can enable `hexa` only after platform topology, EventStore, identity/access, Dapr Workflow, full context, safety, Provider/cost, Conversations membership/posting, UX, governance, metrics, and cross-system evidence are live and conformant.

**FRs reconciled:** FR1–FR28; **NFRs reconciled:** NFR1–NFR10; **UX:** UX-DR1–UX-DR47; **Architecture:** AD-1–AD-22

**External prerequisite:** `CONV-AI-1` must be delivered by Hexalith.Conversations before Story 5.11 can complete.

Cross-cutting implementation note: FR19, FR20, and FR21 are mapped to Epic 2 for primary historical coverage, but tenant isolation, authorization, and fail-closed dependency handling constrain every Epic 5 story. FR22 is mapped to Epic 4 for historical UI coverage, while Epic 5 provides the final live policy/readiness surfaces and conformance. No Epic 1–4 `done` state substitutes for Epic 5 Level 4–5 evidence.

## Epic 1: Tenant Agent Setup And Governance

Agent Administrators can configure `hexa` for a tenant with Party identity, provider/model selection, response mode, approver policy, lifecycle, and active content safety policy.

### Story 1.1: Buildable Agents Module Shell And Public Boundaries

As an Integration Developer,
I want a buildable `Hexalith.Agents` module shell with public contracts and project boundaries,
So that governed Agent setup can be implemented through stable Hexalith conventions without leaking infrastructure details.

**Acceptance Criteria:**

**Given** the agents workspace has no completed `Hexalith.Agents` module
**When** the story is implemented
**Then** the workspace root contains a buildable `Hexalith.Agents.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, workspace-root `src/` projects, and workspace-root `test/` projects matching the architecture Structural Seed
**And** projects target `net10.0`, use Central Package Management, nullable, implicit usings, and warnings as errors.

**Given** the module shell exists
**When** package and project references are inspected
**Then** public contract projects do not reference server infrastructure, provider SDKs, raw EventStore server internals, Dapr runtime implementation packages, or UI shell packages
**And** dependency direction follows the architecture rule: client/UI/server consume contracts, not the reverse.

**Given** a developer builds the module
**When** the narrow build command for the new Agents solution is run
**Then** it succeeds without warnings
**And** placeholder tests verify project boundaries, package-version centralization, and absence of direct provider secret/configuration leakage in public contracts.

**Given** future Agent setup stories will add aggregates and UI
**When** the module shell is reviewed
**Then** it exposes named extension points or folders for `Agent`, `ProviderCatalog`, `AgentInteraction`, application orchestration, ports, projections, UI, AppHost, testing, and client contracts
**And** it does not pre-create unrelated domain entities, storage tables, or all future events ahead of the story that needs them.

### Story 1.2: Govern Provider Catalog Entries

As an Agent Administrator,
I want to create, inspect, enable, disable, and update safe Provider/model catalog entries,
So that `hexa` can only use governed Provider/model options without exposing secrets.

**Acceptance Criteria:**

**Given** the Agents module shell exists
**When** an authorized administrator creates a Provider/model catalog entry
**Then** the system records a `ProviderCatalog` state change with `ProviderId`, `ModelId`, display label, enabled state, text-generation capability, context-window token limit, max-output token limit, timeout policy, optional safe capability flags, and secret reference/configured state
**And** raw provider credentials or secret values are never accepted into public read contracts, events meant for display, logs, telemetry, or audit summaries.

**Given** a Provider/model exists
**When** an authorized administrator disables it
**Then** the Provider/model is no longer selectable for new active Agent configuration
**And** historical catalog state remains inspectable without exposing secrets.

**Given** a caller is not authorized for provider administration
**When** the caller attempts to create, update, enable, disable, or inspect non-public Provider/model catalog details
**Then** the request fails before mutation
**And** the failure response does not reveal unrelated tenant records or provider secrets.

**Given** provider catalog commands are replayed or delivered more than once
**When** the `ProviderCatalog` aggregate is rehydrated
**Then** state is deterministic and duplicate/idempotent command behavior is covered by aggregate tests
**And** all business failures are represented as typed rejections or structured results rather than unhandled exceptions.

### Story 1.3: Configure And Manage `hexa` Lifecycle

As an Agent Administrator,
I want to configure `hexa` with identity metadata, instructions, tenant scope, and lifecycle state,
So that the tenant has a durable governed Agent record before anyone can call it.

**Acceptance Criteria:**

**Given** the Agents module has an `Agent` aggregate
**When** an authorized administrator creates or updates `hexa`
**Then** the Agent record stores stable `AgentId`, tenant scope, display name, description, Agent Instructions, lifecycle state, and configuration version
**And** configuration changes record safe audit facts including actor, timestamp, and prior/new values where safe to expose.

**Given** required `hexa` fields are missing or invalid
**When** an administrator attempts to activate `hexa`
**Then** activation is rejected with specific activation blockers
**And** the rejected activation does not make `hexa` callable.

**Given** `hexa` is disabled
**When** a caller or administrator inspects lifecycle state
**Then** the disabled state is visible through public Agent status contracts
**And** prior Audit Evidence, Proposed Agent Replies, and Conversation Messages are not deleted or rewritten.

**Given** a caller is not authorized to administer Agents for the tenant
**When** the caller attempts to configure or change lifecycle state
**Then** the system fails closed before mutation
**And** authorization failure is auditable without leaking sensitive Agent instructions or unrelated tenant data.

### Story 1.4: Link `hexa` To A Party Identity

As an Agent Administrator,
I want to link or provision `hexa` with exactly one Party identity,
So that future Agent responses are attributable to a known AI participant rather than a caller or generic system account.

**Acceptance Criteria:**

**Given** `hexa` exists for a tenant
**When** an authorized administrator links an existing Party identity or provisions a new Agent Party identity through the Parties adapter
**Then** `hexa` stores only the stable `PartyId` reference
**And** no Party display names, contact values, personal identifiers, or Parties personal-data objects are persisted in Agents durable events.

**Given** Party validation returns missing, disabled, ambiguous, unavailable, or unauthorized state
**When** the administrator attempts to link that Party identity
**Then** the link is rejected
**And** `hexa` remains not callable for posting-dependent workflows.

**Given** `hexa` already has a linked Party identity
**When** an administrator attempts to link a second active Party identity
**Then** the system rejects the operation or requires an explicit replacement command
**And** the Agent can never have more than one active Party identity.

**Given** Party identity linking succeeds
**When** Agent readiness is evaluated
**Then** readiness includes Party identity state as a distinct gate
**And** audit/status output identifies the presence of a valid Party reference without exposing personal Party data.

### Story 1.5: Select Provider And Model For `hexa`

As an Agent Administrator,
I want to select an enabled Provider/model from the governed catalog for `hexa`,
So that future Agent calls use an approved model choice with explainable readiness and audit evidence.

**Acceptance Criteria:**

**Given** enabled Provider/model catalog entries exist
**When** an authorized administrator selects a Provider/model for `hexa`
**Then** the Agent configuration records `ProviderId`, `ModelId`, provider capability version, and configuration version
**And** enough safe Provider/model identity is available for future Audit Evidence.

**Given** the selected Provider/model is disabled, missing, not configured, not text-generation capable, or lacks required context/output/timeout metadata
**When** the administrator attempts to select it or activate `hexa`
**Then** the system rejects the selection or activation with a Provider readiness blocker
**And** no provider SDK call or credential access occurs.

**Given** an existing Agent selection is changed
**When** future Agent configuration is inspected
**Then** the new Provider/model applies only to future Agent Calls
**And** prior configuration versions and historical evidence are not rewritten.

**Given** provider/model status is displayed through setup status contracts
**When** a caller lacks authorization or tenant access
**Then** the response fails closed
**And** does not reveal Provider/model records from another tenant.

### Story 1.6: Configure Response Mode And Approver Policy

As an Agent Administrator,
I want to configure response mode and approval authority for `hexa`,
So that the tenant can choose automatic posting or governed confirmation before generated content reaches a Conversation.

**Acceptance Criteria:**

**Given** `hexa` exists with base configuration
**When** an authorized administrator chooses Automatic Response Mode
**Then** the Agent records the mode and configuration version
**And** the system makes clear that mode changes apply only to future Agent Calls.

**Given** an administrator chooses Confirmation Response Mode
**When** the administrator configures Approver Policy sources
**Then** the policy can include caller `PartyId`, predefined `PartyId`s, tenant roles from the local Tenants projection, and conversation authority resolved from Conversations detail
**And** Conversation owner authority uses the V1 facilitator-based resolver unless an explicit Conversations owner resolver exists.

**Given** an Approver Policy source is missing, stale, ambiguous, disabled, or unavailable
**When** readiness is evaluated for confirmation mode
**Then** activation is blocked with a policy readiness reason
**And** the system fails closed rather than treating missing policy state as permissive.

**Given** approval policy decisions will later be used in proposal workflows
**When** the policy is stored
**Then** the system records a policy version and disclosure category for policy-basis reporting
**And** API/client contracts and future UI surfaces can expose user-visible, operator-only, redacted, or omitted basis consistently.

### Story 1.7: Configure Content Safety Policy And Activation Gate

As an Agent Administrator or release operator,
I want to configure the active Content Safety and prompt policy for `hexa`,
So that production or production-like enablement cannot proceed without explicit safety rules.

**Acceptance Criteria:**

**Given** `hexa` has identity, Provider/model, response mode, and policy configuration
**When** an authorized administrator or release operator defines Content Safety Policy
**Then** the policy records prompt constraints, blocked or restricted output categories, safety failure handling, audit treatment, and policy version
**And** changes are auditable and affect only future Agent Calls.

**Given** no active Content Safety Policy exists
**When** an administrator attempts to activate `hexa` for production or production-like launch validation
**Then** activation is rejected
**And** status identifies Content Safety Policy as a blocker without exposing unsafe policy content.

**Given** both Automatic Response Mode and Confirmation Response Mode are available
**When** Content Safety Policy is evaluated during setup
**Then** both modes use the same active policy unless a stricter mode-specific policy is configured
**And** mode-specific differences are visible in safe setup status.

**Given** Agent readiness is evaluated
**When** identity, Provider/model, response mode, approver policy where applicable, lifecycle, and Content Safety Policy are all valid
**Then** `hexa` can be marked active and callable in setup readiness
**And** any missing, stale, ambiguous, disabled, or unavailable dependency blocks activation.

### Story 1.8: Admin Setup UI And Readiness Overview

As an Agent Administrator,
I want a FrontComposer setup experience for provider governance and `hexa` readiness,
So that I can configure and activate `hexa` without using internal EventStore or provider details.

**Acceptance Criteria:**

**Given** the Agents setup contracts exist
**When** an authorized administrator opens the FrontComposer shell
**Then** an Agents domain/category is registered with setup-oriented navigation entries for Agents overview, `hexa` configuration, Provider catalog, and Approver policy
**And** policy-gated navigation hides or denies entries without leaking unauthorized records.

**Given** the administrator opens the Agents overview
**When** readiness data is loaded
**Then** the view shows `hexa` readiness, lifecycle, response mode, Provider/model, activation blockers, and callability for the tenant
**And** `agent-readiness-badge` distinguishes active lifecycle from callable readiness.

**Given** the administrator opens Provider catalog
**When** Provider/model records are loaded
**Then** a full-width FrontComposer FC-TBL/FluentDataGrid surface shows Provider/model options, enabled state, capability metadata, readiness, and secret configured/not-configured state
**And** secret values, raw provider payloads, and provider SDK details are never displayed, logged, copied, or placed in accessible names.

**Given** the administrator opens `hexa` configuration or Approver policy
**When** form controls render
**Then** constrained FrontComposer/Fluent layouts are used for identity, instructions, Provider/model, response mode, approver policy, lifecycle, activation blockers, and content safety state
**And** response mode uses a mutually exclusive Fluent segmented control or radio group whose copy states that changes affect future Agent Calls only.

**Given** setup surfaces display statuses
**When** statuses are rendered
**Then** semantic Fluent status roles, icons, and visible text are used consistently
**And** color-only status, custom radii, non-localizable sentence fragments, and layout shifts from changing badges/action slots are prevented by component or conformance tests.

**Given** setup pages are tested for accessibility
**When** keyboard, focus, loading, empty, filtered-empty, error, permission-denied, and stale/degraded states are exercised
**Then** FC-A11Y primitives, named navigation landmarks, focus visibility, table semantics, live-region status behavior, and safe accessible names pass the relevant UI tests.

## Epic 2: Safe Conversation Invocation And Automatic Replies

Conversation Participants can explicitly call `hexa` from a Conversation and receive a governed automatic reply, with authorization, context bounds, safety, dependency uncertainty, and failure handling enforced before side effects.

### Story 2.1: Request `hexa` From A Source Conversation

As a Conversation Participant,
I want to explicitly call `hexa` from a Source Conversation with a prompt,
So that I can request contextual help without leaving the Conversation or creating an anonymous AI response.

**Acceptance Criteria:**

**Given** `hexa` is active in setup readiness
**When** an authorized Conversation Participant submits an Agent Call request with tenant, Source Conversation, Agent, caller, prompt, and idempotency metadata
**Then** the system creates an `AgentInteraction` request record with deterministic identity and response mode snapshot
**And** the request captures caller `PartyId`, source `ConversationId`, Agent configuration version, instructions version, approver policy version, Provider/model identity, Provider capability version, context policy, and request timestamp.

**Given** the request is accepted
**When** public API/client contracts return the result
**Then** callers receive a structured Agent Call status reference rather than raw EventStore stream names, provider SDK details, or internal projection identifiers
**And** repeated requests with the same idempotency metadata do not create duplicate interactions.

**Given** V1 excludes ambient or external triggers
**When** Conversation state changes without an explicit Agent Call
**Then** no AgentInteraction is created
**And** no provider invocation, proposal, or Conversation Message side effect occurs.

**Given** prompt and Conversation-derived data are sensitive
**When** the interaction is logged, traced, returned in status summaries, or represented in audit summaries
**Then** raw prompt/context content is excluded or protected according to EventStore payload-protection/redaction conventions
**And** no unrelated tenant data is exposed.

### Story 2.2: Enforce Invocation Authorization And Dependency Readiness

As a Conversation Participant,
I want unauthorized or unsafe Agent Calls to be rejected before generation,
So that `hexa` cannot leak tenant data or act when required state is uncertain.

**Acceptance Criteria:**

**Given** an Agent Call request exists
**When** invocation gating runs
**Then** tenant access, caller Party state, Source Conversation access, Agent lifecycle, Agent Party identity, Provider/model readiness, response policy, Content Safety Policy, and dependency freshness are checked before any provider invocation
**And** missing, stale, ambiguous, disabled, or unavailable state fails closed.

**Given** caller access to the Source Conversation is missing or stale
**When** the call is evaluated
**Then** the system records a denied or blocked interaction status
**And** no provider invocation, Proposed Agent Reply, or Conversation Message is created.

**Given** tenant access or Party state is unavailable
**When** the system cannot prove authorization
**Then** the request fails closed with a safe structured error
**And** the response does not reveal whether records exist in another tenant.

**Given** authorization failure occurs
**When** authorized administrators inspect status or audit
**Then** the system exposes enough safe evidence to distinguish authorization, dependency, Agent readiness, Provider readiness, and policy failures
**And** raw claims, tokens, Party personal data, provider payloads, and stack traces are not displayed.

### Story 2.3: Build Conversation Context With Safe Bounds

As a Conversation Participant,
I want `hexa` to use the authorized Conversation context safely,
So that the answer is grounded in the Conversation without silent truncation or unrelated data exposure.

**Acceptance Criteria:**

**Given** an AgentInteraction passes invocation gates
**When** context building starts
**Then** the system loads only authorized Conversations detail and visible timeline content through supported Conversations client/API boundaries
**And** V1 excludes long-term memory, project content, folder content, file content, tool output, external-channel content, and non-conversation retrieval.

**Given** the full Source Conversation fits the selected Provider/model context budget after reserving configured output tokens
**When** context building completes
**Then** the interaction records full-context usage, context policy version, model budget metadata, and safe context evidence
**And** raw context is protected or redacted from logs, telemetry, queue summaries, and status badges.

**Given** the Source Conversation exceeds the safe context budget or cannot be loaded fresh enough
**When** no approved bounded-context behavior is configured
**Then** the system records a context-blocked failure
**And** no provider call, Proposed Agent Reply, or Conversation Message is created.

**Given** an approved bounded-context behavior exists
**When** it is used
**Then** the behavior, policy version, bounds, and audit-safe metadata are recorded
**And** the system never silently truncates context without explicit policy evidence.

### Story 2.4: Generate And Safety-Check Agent Output

As a Conversation Participant,
I want `hexa` generation to respect Provider and Content Safety policies,
So that unsafe or incomplete output cannot become a durable conversation artifact.

**Acceptance Criteria:**

**Given** context building succeeds
**When** the selected durable owner invokes generation through an Agents-owned Provider adapter
**Then** provider SDK types, credentials, raw payloads, and provider-specific errors stay behind adapter boundaries
**And** public contracts and durable events expose only safe Provider/model identity, safe error classes, usage/status, and policy references.

**Given** generation succeeds
**When** Content Safety Policy is evaluated
**Then** generated content that passes policy can proceed to the response-mode branch
**And** generated content that fails policy cannot be posted automatically or become an approvable Proposed Agent Reply.

**Given** provider timeout, disabled Provider/model state, adapter failure, invalid context, safety failure, or policy failure occurs
**When** the interaction is updated
**Then** the system records a safe failure status and Audit Evidence
**And** no partial Conversation Message is created.

**Given** the same generation attempt is retried after a transient failure
**When** deterministic attempt identifiers are reused
**Then** duplicate generated versions, duplicate provider attempts where avoidable, and duplicate downstream effects are prevented or safely deduplicated
**And** retry outcomes remain auditable.

### Story 2.5: Post Automatic Responses Through Conversations

As a Conversation Participant,
I want successful automatic responses posted as `hexa`,
So that the Conversation contains an attributed AI response only after all gates pass.

**Acceptance Criteria:**

**Given** an AgentInteraction is in Automatic Response Mode and generation plus safety checks pass
**When** posting begins
**Then** the system verifies the Agent `PartyId` is valid and present in the Source Conversation as an AI Agent participant through a Conversations-owned membership command/API/client boundary
**And** posting fails closed if membership cannot be proven or established safely.

**Given** membership and posting prerequisites pass
**When** the system appends the response to the Source Conversation
**Then** the Conversation Message is authored by the Agent Party identity, not the caller or a system account
**And** the message references the AgentInteraction or equivalent trace identifier.

**Given** posting is retried
**When** the same AgentInteraction and generated content version are used
**Then** the Conversations append uses deterministic `MessageId` and idempotency key derived from interaction/version context
**And** no duplicate Conversation Message is created.

**Given** posting fails after generation succeeds
**When** status is inspected
**Then** the system distinguishes posting failure from generation failure, authorization failure, context failure, and safety failure
**And** generated content is not exposed through unauthorized status or logs.

### Story 2.6: Conversation Invocation UX And Call Status Feedback

As a Conversation Participant,
I want clear in-product feedback when I call `hexa`,
So that I understand whether the request is pending, blocked, failed, or posted without mistaking drafts for Conversation Messages.

**Acceptance Criteria:**

**Given** Conversation-originated invocation is exposed through a mention, command, action, participant affordance, or approved combination
**When** the participant starts an Agent Call
**Then** the UI visibly names `hexa`, captures prompt and Source Conversation context, and shows the response-mode implication before provider invocation
**And** the UI does not imply an automatic response has posted until Conversations confirms the final message.

**Given** an Agent Call transitions through requested, authorized, denied, context loading, context blocked, generating, generation failed, generated, posting pending, posted, or posting failed states
**When** the UI renders status
**Then** semantic color, icon, visible text, and accessible names distinguish each state
**And** color-only status and raw subsystem/provider error text are forbidden.

**Given** the participant lacks permission or dependency state is uncertain
**When** the call is denied or blocked
**Then** the UI presents a safe reason such as permission denied, provider unavailable, context blocked, safety failed, or posting failed
**And** it does not leak unauthorized Conversation, Party, tenant, provider, prompt, or generated content details.

**Given** the UI runs on constrained viewports or reduced-motion settings
**When** a high-impact automatic call action cannot show enough context or status
**Then** the action is unavailable with a visible reason or downgraded to safe review-only behavior
**And** state changes remain perceivable without relying on animation.

## Epic 3: Proposal Review And Approval Workflow

Approvers can discover, edit, regenerate, approve, reject, abandon, or let Proposed Agent Replies expire, while all versions remain preserved and only an approved selected version can be posted.

### Story 3.1: Create Proposed Agent Replies In Confirmation Mode

As an Approver,
I want successful confirmation-mode generation to create a Proposed Agent Reply outside the Conversation,
So that generated content can be reviewed before it becomes durable Conversation content.

**Acceptance Criteria:**

**Given** an AgentInteraction is in Confirmation Response Mode and generation plus safety checks pass
**When** the response-mode branch runs
**Then** the system creates a Proposed Agent Reply linked to the AgentInteraction and Source Conversation
**And** it records caller, Agent, Source Conversation, generated version, Provider/model, response mode, proposal state, expiry metadata where configured, and policy snapshots.

**Given** a Proposed Agent Reply exists
**When** Conversation content is inspected
**Then** the proposal is not present as a Conversation Message
**And** generated content is visible only through authorized proposal workflow surfaces.

**Given** generated content fails Content Safety Policy
**When** confirmation mode handles the output
**Then** no approvable Proposed Agent Reply is created
**And** authorized status/audit records a safety failure according to policy without exposing unsafe content where forbidden.

**Given** proposal creation is retried
**When** the same AgentInteraction and generated version are used
**Then** duplicate Proposed Agent Replies and duplicate generated versions are prevented
**And** replay produces deterministic proposal state.

### Story 3.2: Discover Pending Proposals In Product

As an Approver,
I want to find Proposed Agent Replies that need my action,
So that confirmation-mode responses do not stall silently.

**Acceptance Criteria:**

**Given** pending proposals exist for an authorized Approver
**When** the Approver opens the proposal queue
**Then** the queue lists pending and authorized historical proposals with proposal state, Source Conversation, caller, Agent, current responsibility, expiry, and age
**And** unapproved generated content is not rendered as a Conversation Message.

**Given** an Approver filters the queue
**When** filters for "needs my action", state, Agent, Source Conversation, caller, or expiry are applied
**Then** the grid distinguishes loading, empty, filtered-empty, error, permission-denied, and stale/degraded states
**And** filtered-empty offers a clear filter reset.

**Given** active notifications are not included in V1 launch
**When** launch readiness or product status is inspected
**Then** the system records that Approvers rely on the in-product pending-proposal visibility surface
**And** proposal count or status indication is available to authorized Approvers.

**Given** an unauthorized caller opens the proposal queue
**When** proposal records exist in another tenant or outside their policy
**Then** those records are not disclosed by counts, empty states, filters, accessible names, or error details
**And** the authorization denial is safe and auditable.

### Story 3.3: Edit Proposed Reply Versions

As an Approver,
I want to edit a Proposed Agent Reply before approval,
So that the final posted answer can be corrected while preserving what the Agent generated.

**Acceptance Criteria:**

**Given** a proposal is pending and the current user is authorized by the snapshotted Approver Policy plus current dependencies
**When** the Approver edits the proposed content
**Then** the system creates a new immutable edited version with author, timestamp, source version, and safe metadata
**And** prior generated or edited versions remain preserved and inspectable by authorized users.

**Given** a proposal has reached approved, rejected, abandoned, expired, posted, or another terminal state
**When** an Approver attempts to edit it
**Then** the edit is rejected
**And** no new version is created.

**Given** the editor is displayed
**When** generated and edited content are shown
**Then** each version is labeled distinctly and the proposal is never styled as an already-posted Conversation Message
**And** generated/editor content is excluded from logs, telemetry dimensions, status badges, and unauthorized accessible names.

**Given** an edit is saved
**When** audit evidence is queried
**Then** the edit version, editor, source version, timestamp, and policy basis are available to authorized users
**And** previous version content is not overwritten.

### Story 3.4: Regenerate Proposed Reply Versions

As an Approver,
I want to request regeneration before approval,
So that I can compare a new Agent version without losing earlier generated or edited content.

**Acceptance Criteria:**

**Given** a proposal is pending and the current user is authorized to regenerate
**When** regeneration is requested
**Then** the system creates a new deterministic generation attempt linked to the same AgentInteraction
**And** the attempt uses the same Source Conversation and snapshotted Agent configuration unless an explicit configuration-version change is recorded.

**Given** regeneration succeeds and passes Content Safety Policy
**When** the new output is recorded
**Then** a new immutable generated version is added to version history
**And** all prior generated and edited versions remain visible to authorized users.

**Given** regeneration fails, times out, is denied, or fails safety
**When** the proposal is inspected
**Then** the existing proposal remains pending unless policy moves it to a terminal state
**And** failure status is visible without exposing unsafe content, raw provider errors, or provider payloads.

**Given** a proposal is terminal
**When** regeneration is requested
**Then** regeneration is rejected
**And** no provider invocation occurs.

### Story 3.5: Approve A Selected Version And Post It

As an Approver,
I want to approve exactly one selected proposal version,
So that only the reviewed response becomes a Conversation Message as `hexa`.

**Acceptance Criteria:**

**Given** a proposal is pending and contains one or more preserved versions
**When** an authorized Approver selects a version and approves it
**Then** the system records the approved `VersionId`, Approver, approval timestamp, policy basis, and posting-pending state
**And** no other proposal version is eligible to post for that approval.

**Given** approval is recorded
**When** the approved version is posted to Conversations
**Then** the Conversation Message is attributed to the Agent Party identity
**And** Audit Evidence links the approved version, Approver, approval timestamp, AgentInteraction, Source Conversation, Provider/model, and posted Conversation Message.

**Given** posting is retried
**When** the same approved version is used
**Then** deterministic `MessageId` and idempotency key prevent duplicate Conversation Messages
**And** posting outcome remains auditable.

**Given** the selected version fails final authorization, safety, Party, Conversation, Provider, tenant, or membership checks
**When** approval or posting is attempted
**Then** the system fails closed before Conversation side effects
**And** status distinguishes approval failure from posting failure where applicable.

### Story 3.6: Reject, Abandon, And Expire Proposals

As an Approver or system policy,
I want proposals to reach explicit terminal states when they should not be posted,
So that stale or rejected generated content cannot later enter a Conversation.

**Acceptance Criteria:**

**Given** a proposal is pending
**When** an authorized Approver rejects it
**Then** the proposal moves to rejected terminal state with rationale metadata where policy requires it
**And** all versions remain preserved for authorized audit.

**Given** a proposal is pending
**When** an authorized Approver abandons it
**Then** the proposal moves to abandoned terminal state
**And** it cannot later be approved, edited, regenerated, or posted.

**Given** proposal expiry policy exists
**When** the configured expiry is reached
**Then** the proposal moves deterministically to expired terminal state
**And** expiry behavior is visible through admin UI and API/client contracts.

**Given** a rejected, abandoned, or expired proposal exists
**When** a caller attempts to approve or post it
**Then** the action is rejected before Conversation side effects
**And** the UI/API routes the user to start a new Agent Call if a response is still needed.

### Story 3.7: Proposal Detail, Version History, And Accessibility

As an Approver,
I want a proposal detail workspace with version history and accessible controls,
So that I can make safe approval decisions with full context and without hidden actions.

**Acceptance Criteria:**

**Given** an authorized Approver opens proposal detail
**When** the workspace loads
**Then** it shows current selected version, editable content where authorized, Source Conversation metadata, caller, Agent, Provider/model, response mode, expiry, state, version actions, and posting outcome
**And** no provider secrets, raw payloads, stack traces, or unrelated tenant data are shown.

**Given** a proposal has multiple generated, edited, or regenerated versions
**When** version history renders
**Then** every version lists source/author, timestamp, kind, Provider/model where applicable, and approval/posting markers
**And** prior versions remain accessible after edit, regeneration, approval, rejection, abandonment, or expiry.

**Given** an Approver uses keyboard-only navigation
**When** they edit, select a version, compare metadata, regenerate, approve, reject, abandon, or exit
**Then** all controls are reachable in a clear focus order
**And** `Esc` closes transient UI without committing and returns focus to the triggering row/action.

**Given** proposal state changes
**When** generation fails, proposal is created, proposal expires, approval posts, posting fails, or permission is denied
**Then** live regions announce the important transition with safe text
**And** ordinary pending progress does not use disruptive assertive announcements.

## Epic 4: Operational Visibility, Audit, Integration, And Launch Readiness

Administrators, operators, and integration developers can manage Agents through UI/API contracts, inspect status and audit evidence, and enforce launch-readiness gates for metrics, latency, context, and cost posture.

### Story 4.1: Stable API And Client Contracts For Agent Operations

As an Integration Developer,
I want stable public API/client contracts for Agent operations,
So that automation can manage and monitor governed Agent workflows without depending on internals.

**Acceptance Criteria:**

**Given** setup, invocation, and proposal workflows exist
**When** public API/client contracts are published
**Then** contracts cover Provider administration, Agent administration, Agent invocation, proposal workflow, status inspection, and audit inspection
**And** consumers do not need raw EventStore stream names, aggregate mechanics, projection internals, provider SDK details, or workflow SDK types.

**Given** an integration caller submits an operation
**When** authorization or validation fails
**Then** the contract returns structured success/error results suitable for automation
**And** errors do not leak provider secrets, raw payloads, stack traces, or unrelated tenant records.

**Given** public contract changes are made during V1
**When** contract tests and public API baselines run
**Then** changes are additive-first or explicitly versioned when breaking
**And** admin UI and client contracts share the same authorization outcomes.

**Given** the API/client contracts expose operation status
**When** callers inspect setup, invocation, proposal, posting, audit, or launch readiness state
**Then** status terms align with the UX canonical states
**And** pending states are not promoted to success.

### Story 4.2: Query Audit Evidence Safely

As a Tenant or Compliance Operator,
I want to inspect support-safe Audit Evidence for Agent behavior,
So that I can prove who called, generated, edited, approved, rejected, posted, or blocked a response.

**Acceptance Criteria:**

**Given** Agent configuration, Provider/model configuration, Agent Calls, generation attempts, proposal versions, edits, regenerations, approvals, rejections, abandonments, expirations, automatic posts, or final Conversation Messages occur
**When** Audit Evidence is captured
**Then** evidence links caller, Agent, Source Conversation, Provider/model, response mode, context policy behavior, Content Safety Policy decision, proposal path where applicable, and final Conversation Message where applicable
**And** every posted Agent Response can be traced back to its source interaction.

**Given** an authorized user queries audit evidence
**When** the evidence contains prompt, context-derived, generated, or edited content
**Then** content is shown only according to authorization, retention, redaction, and policy rules
**And** summaries never include provider secrets, raw credentials, raw provider payloads, stack traces, or unrelated tenant data.

**Given** audit evidence is delayed, unavailable, pending, or available
**When** audit status is rendered through API or UI
**Then** the state is distinguished as audit pending, audit available, audit delayed, or audit unavailable
**And** audit pending/delayed/unavailable is never displayed as success.

**Given** retention period, legal hold, export behavior, or deletion behavior is unresolved
**When** content-bearing audit implementation is attempted
**Then** the story or feature is blocked until a named platform policy or dedicated Agents governance decision exists
**And** the blocker is visible in launch readiness status.

### Story 4.3: Expose Operational Status And Admin Workflows

As an Agent Administrator or Operator,
I want operational status surfaces for readiness, calls, proposals, failures, and posting outcomes,
So that I can diagnose and operate `hexa` safely after enablement.

**Acceptance Criteria:**

**Given** Agents setup, invocation, and proposal workflows emit status
**When** an authorized administrator opens operational status
**Then** the UI/API distinguishes Agent readiness, Provider/model readiness, configuration errors, authorization failures, context policy failures, content safety failures, Provider failures, generation failures, pending approvals, approval completion, posting pending, posting failures, and successful posts
**And** recovery guidance is grouped by action rather than raw subsystem names.

**Given** status data is loading, empty, filtered-empty, stale, degraded, unavailable, error, or permission-denied
**When** operational grids and panels render
**Then** each state is visibly and accessibly distinct
**And** empty and error states do not leak unauthorized records.

**Given** admin UI slices were delivered in earlier epics
**When** Epic 4 completes the operational surface
**Then** provider administration, `hexa` configuration, lifecycle, policy, proposal operations, status, and audit entry points are coherent through the Agents domain navigation
**And** the UI uses public Agents contracts instead of raw EventStore streams, provider SDKs, or aggregate internals.

**Given** launch monitoring needs adoption and approval workflow metrics
**When** status projections are queried
**Then** authorized users can inspect recent Agent Call outcomes, proposal queues, terminal-state rates, posting outcomes, and readiness blockers
**And** raw prompt/generated content is not used as telemetry dimensions or list-summary text.

### Story 4.4: Define And Enforce Launch Readiness Gates

As a Release Operator,
I want explicit launch-readiness controls for metrics, latency, context, safety, and cost,
So that production-like generation cannot be enabled on implicit assumptions.

**Acceptance Criteria:**

**Given** production or production-like generation is requested
**When** launch readiness is evaluated
**Then** Content Safety Policy, Conversation Context Policy, launch metric thresholds, latency targets, and cost-control posture must be recorded
**And** missing readiness decisions block enablement.

**Given** SM-2 or SM-3 is used for launch readiness
**When** readiness configuration is saved
**Then** each metric defines numerator, denominator, target, measurement window, and launch cohort
**And** the system distinguishes primary metrics, secondary metrics, and counter-metrics.

**Given** latency and cost posture are configured
**When** readiness is inspected
**Then** Automatic Response Mode and Confirmation Response Mode have explicit latency targets
**And** cost controls are recorded as quotas, budgets, Provider/model limits, reporting-only monitoring, or explicitly accepted launch risk.

**Given** readiness gates fail
**When** administrators inspect status or attempt enablement
**Then** generation remains disabled for production-like launch validation
**And** blockers are visible through API/client contracts and admin UI without exposing secrets or unsafe content.

### Story 4.5: Verify End-To-End Governance And Contract Conformance

As a Master Test Architect,
I want end-to-end governance and contract evidence for `hexa`,
So that launch-critical behavior is proven across UI, API, domain, runtime, and integration boundaries.

**Acceptance Criteria:**

**Given** all Agent setup, invocation, proposal, audit, and readiness workflows are implemented
**When** the conformance test suite runs
**Then** it verifies aggregate transition purity, authorization fail-closed paths, proposal version immutability, replay/idempotency, tenant isolation, context-too-large blocking, provider-secret non-disclosure, Content Safety enforcement, and audit completeness for every posted response
**And** failures identify the relevant FR, NFR, or UX-DR.

**Given** runtime orchestration is used
**When** Agent Framework workflow/session restore, Dapr Workflow ownership, MCP/A2A/tool schema contracts, generation retries, and posting retries are tested where applicable
**Then** every agent task has exactly one durable owner
**And** public contracts and EventStore aggregates remain free of framework/provider SDK types.

**Given** FrontComposer UI surfaces are tested
**When** layout, status semantics, keyboard flow, live regions, localization, policy-gated navigation, grid state handling, and accessible names are evaluated
**Then** UI tests prove setup, invocation, proposal, status, and audit workflows meet the UX Design Requirements
**And** high-impact actions fail closed when context is insufficient on constrained viewports.

**Given** release evidence is collected
**When** the final readiness report is produced
**Then** it maps each FR, NFR, architecture requirement, and UX-DR to at least one story and verification path
**And** unresolved governance decisions such as audit retention/legal hold/export/deletion remain explicit launch blockers rather than hidden assumptions.

## Epic 5: Production Binding And Live Conformance

This epic converts the completed fail-closed skeleton into a production-bound V1 without rewriting Epics 1–4. Its sequence is authoritative: topology and gates precede EventStore/identity bindings; Dapr Workflow is proven with deterministic fake activities; complete context and safety precede live Provider invocation; Conversations membership precedes posting; operator surfaces and final cross-system evidence come last.

### Story 5.1: Correct Platform Hosting Boundary And Quality Gates

As a Platform Maintainer,
I want Agents to remain a reusable domain module composed by a platform-owned host,
So that the module does not duplicate platform hosting or create forbidden dependency directions.

**Dependencies:** approved AD-16 correction.

**Traceability:** FR23; NFR3, NFR4; AD-1, AD-15, AD-16, AD-17.

**Acceptance Criteria:**

**Given** the Agents solution and repository projects
**When** module boundaries are inspected and built
**Then** Hexalith.Agents.AppHost, Hexalith.Agents.Aspire, and Hexalith.Agents.ServiceDefaults projects, references, packages, and enforcing tests are absent
**And** Hexalith.Agents.Server remains the reusable shared EventStore DomainService SDK host with no duplicated platform Dapr, telemetry, health, identity, or EventStore plumbing.

**Given** a platform-owned consumer composes Agents
**When** its topology conformance fixture starts
**Then** it composes the Agents service and UI with EventStore, Conversations, Parties, Tenants, Dapr Workflow, Provider, and safety dependencies
**And** the Agents package graph contains no reverse or forward dependency forbidden by the corrected architecture.

**Given** CI evaluates source, package-consumer, and platform-topology gates
**When** a forbidden hosting project/reference or missing consumer contract is introduced
**Then** the relevant gate fails with AD-16 evidence
**And** a clean checkout succeeds without relying on nested submodule initialization.

### Story 5.2: Enforce Complete Launch Readiness Before Callability

As a Release Operator,
I want a complete authoritative launch-readiness record to gate callability,
So that a partial or merely configured Agent cannot invoke a Provider.

**Dependencies:** approved V1 Decision Register.

**Traceability:** FR1, FR3, FR25, FR28; NFR4, NFR7, NFR9, NFR10; UX-DR20, UX-DR45; AD-12, AD-15, AD-17, AD-20–AD-22.

**Acceptance Criteria:**

**Given** any required topology, identity/access, Provider/pricing, complete-context, safety, budget, audit-governance, metric, latency, or Level 4–5 evidence gate is missing, stale, indeterminate, or failed
**When** Agent readiness is evaluated
**Then** hexa is not callable and production-like generation remains disabled
**And** API/UI status names each safe actionable blocker without exposing secrets or sensitive content.

**Given** structural, unit, or deferred-seam evidence exists without live evidence
**When** readiness is evaluated
**Then** Levels 1–3 remain visible but cannot satisfy a gate that requires Levels 4–5
**And** skips, placeholders, where-applicable results, and insufficient samples are not reported as pass.

**Given** all current gates pass
**When** callability is calculated
**Then** the result records the evaluated gate versions, timestamps, evidence references, and tenant scope
**And** later revocation or staleness removes callability before the next Provider side effect.

### Story 5.3: Bind EventStore Operations And Setup Read Models

As an Agent Administrator,
I want live EventStore command/query operations and readiness read models,
So that Agent and Provider setup is durable, queryable, and authorized.

**Dependencies:** Story 5.1.

**Traceability:** FR1, FR3–FR6, FR22, FR23, FR25; NFR1–NFR6; UX-DR1–UX-DR4, UX-DR20, UX-DR21; AD-1–AD-4, AD-9, AD-10, AD-12, AD-15–AD-17.

**Acceptance Criteria:**

**Given** authorized Agent or ProviderCatalog commands
**When** they pass through the public client/API and DomainService command boundary
**Then** EventStore persists the resulting events and replay reconstructs the same aggregate state
**And** rejected commands produce typed safe outcomes without infrastructure or Provider SDK leakage.

**Given** Agent and Provider events are projected
**When** public IDomainQueryHandler queries execute
**Then** Agent configuration/readiness and Provider capability/pricing read models are returned through IReadModelStore with freshness and version metadata
**And** UI/API consumers never query EventStore streams or aggregate internals directly.

**Given** duplicate, out-of-order, failed, or replayed setup operations
**When** integration tests read the persisted end state
**Then** idempotent duplicates create no extra state, conflicts are typed, and projection status remains explicit
**And** cross-tenant commands and queries are denied before mutation or disclosure.

### Story 5.4: Bind Tenant Access Party Identity And Approver Resolution

As a Tenant Security Operator,
I want live tenant, Party, and Approver authority resolution,
So that every Agent side effect uses current least-privilege identity evidence.

**Dependencies:** Stories 5.1 and 5.3.

**Traceability:** FR2, FR7, FR8, FR19–FR21; NFR1, NFR2; UX-DR5, UX-DR25; AD-4, AD-7, AD-8, AD-12.

**Acceptance Criteria:**

**Given** Tenants integration events arrive duplicated, out of order, with a gap, or after role/access revocation
**When** the local access projection applies them
**Then** duplicates are idempotent, order/gap/freshness is explicit, and uncertain or revoked access fails closed
**And** focused tests prove cross-tenant denial and event-driven revocation before any Provider, membership, posting, export, or deletion side effect.

**Given** an Agent, caller, predefined Approver, or AI posting identity is resolved
**When** the Parties-backed adapter evaluates it
**Then** only a stable tenant-scoped Party reference in valid current state is accepted
**And** missing, disabled, ambiguous, unavailable, or unauthorized identity blocks the operation with a typed safe reason.

**Given** a proposal action requires approval authority
**When** the snapshotted policy is resolved against caller identity, predefined Parties, tenant roles, and Conversation Facilitator authority
**Then** the exact policy basis is auditable and current dependency loss fails closed
**And** no JWT-only, UI-only, or stale-projection authorization can grant the action.

### Story 5.5: Persist Interaction Proposal Status And Audit Read Models

As an Approver or Operator,
I want live AgentInteraction, proposal, status, and audit read models,
So that workflow state is durable and inspectable without treating execution history as business truth.

**Dependencies:** Stories 5.1 and 5.3.

**Traceability:** FR8, FR10, FR13–FR18, FR24, FR25; NFR3–NFR5; UX-DR6–UX-DR10, UX-DR22, UX-DR27–UX-DR30; AD-1–AD-5, AD-14, AD-22.

**Acceptance Criteria:**

**Given** AgentInteraction events for request, failure, generated/edited/regenerated versions, approval, rejection, abandonment, expiry, posting, or posting failure
**When** projections handle them
**Then** the interaction, proposal, queue/status, immutable version, and audit read models converge to the persisted EventStore end state
**And** approved, posted, terminal, pending, and failed states remain distinct.

**Given** an edit or regeneration
**When** a new version is persisted and projected
**Then** every prior content version and authorship/source reference remains addressable to authorized users
**And** approval selects exactly one immutable version.

**Given** projection replay, duplicate delivery, gap, content protection, or an unauthorized query
**When** integration tests execute
**Then** projection behavior is idempotent and restrictive, safe freshness/degradation is visible, and no unrelated tenant data or raw sensitive list content leaks
**And** Dapr Workflow history is never queried as proposal business state.

### Story 5.6: Reconcile Provider Capability Runtime Contracts

As a Runtime Maintainer,
I want AD-10, AD-11, and AD-13 enforced by one prepared-attempt contract,
So that stale capabilities or changed requests cannot cross a Provider boundary.

**Dependencies:** Stories 5.3–5.5.

**Traceability:** FR4, FR5, FR9, FR10, FR16, FR21, FR24; NFR3, NFR6, NFR8; AD-4, AD-9–AD-14.

**Acceptance Criteria:**

**Given** an interaction snapshots Provider/model and ProviderCapabilityVersion
**When** context, generation, or regeneration observes a live catalog entry
**Then** the durable high-water mark advances to every identified version and each step independently requires current enabled/configured/text-generation/priced state and valid positive limits
**And** missing, lower, stale, unpriced, or not-ready state fails closed before Provider invocation.

**Given** a Provider attempt is ready
**When** the durable owner prepares it
**Then** one deterministic descriptor binds attempt id, Provider/model, EffectiveProviderCapabilityVersion, limits, timeout, cost inputs, safety policy versions, and canonical request fingerprint
**And** no raw context or sensitive content is stored in the descriptor.

**Given** a retry or regeneration observes changed capability, readiness, policy strictness, limits, pricing, or request fingerprint
**When** it compares with the prepared descriptor
**Then** the attempt fails closed rather than substituting new inputs under the same attempt id
**And** lower/equal/higher version, disablement, timeout, and concurrent retry tests prove the behavior.

### Story 5.7: Bind Dapr Workflow As The Sole V1 Durable Owner

As a Runtime Operator,
I want every V1 interaction coordinated by Dapr Workflow,
So that waits, expiry, retries, and recovery survive restart without double orchestration.

**Dependencies:** Stories 5.2 and 5.6.

**Traceability:** FR8–FR18, FR24, FR25; NFR3, NFR4; AD-3, AD-5, AD-13, AD-17, AD-18; implementation command-step convention.

**Acceptance Criteria:**

**Given** a requested automatic or confirmation interaction
**When** the platform starts its workflow
**Then** Dapr Workflow alone coordinates deterministic fake context, safety, generation, proposal wait, expiry, and posting activities
**And** Microsoft Agent Framework workflow, Python DurableAgent, MCP, A2A, in-memory workers, and alternate durable owners are absent from the V1 execution path.

**Given** an activity completes, fails, times out, or is replayed
**When** orchestration resumes
**Then** the step dispatches at most one deterministic server-trusted command, re-evaluates required gates, and produces no duplicate version, attempt, timer, or post
**And** EventStore remains authoritative for every business transition.

**Given** the host restarts during generation, confirmation wait, expiry, or posting
**When** the workflow recovers
**Then** the same interaction reaches the same persisted outcome as uninterrupted execution
**And** a proposal expires at or after stored ExpiresAt using its snapshotted duration, default 24 hours and configurable 1 hour through 30 days for future proposals only.

### Story 5.8: Bind Authorized Complete Conversation Context

As a Conversation Participant,
I want hexa to use the complete authorized Conversation or not run,
So that a partial context is never presented as a complete answer.

**Dependencies:** Stories 5.4 and 5.6–5.7.

**Traceability:** FR8–FR10, FR19–FR21; NFR1, NFR2, NFR8; UX-DR24, UX-DR27, UX-DR42; AD-6, AD-10–AD-13, AD-18.

**Acceptance Criteria:**

**Given** an authorized Agent Call
**When** the context activity reads Conversations
**Then** it obtains fresh authorized detail and the complete visible timeline, excludes V1 non-goals, and records policy/version and safe measurement evidence
**And** unavailable, stale, partial, unauthorized, or cross-tenant content fails closed.

**Given** the complete Conversation plus reserved output exceeds the current safe model budget
**When** context is evaluated or revalidated immediately before a Provider attempt
**Then** the interaction records ContextBlocked and creates no Provider call, proposal, or Conversation Message
**And** no truncation, summarization, windowing, sampling, retrieval substitution, or prior-context reuse occurs.

**Given** Conversation content or capability changes after ContextReady
**When** generation or regeneration prepares its exact input
**Then** it repeats authorization, complete read, token measurement, capability high-water/readiness, and full budget calculation
**And** the UI/API shows a safe full-or-blocked outcome.

### Story 5.9: Bind The Live Content Safety Engine

As a Security Operator,
I want versioned safety enforced before Provider and Conversation side effects,
So that unsafe or unauthorized content cannot be approved around the policy.

**Dependencies:** Stories 5.2 and 5.7–5.8.

**Traceability:** FR10, FR12, FR26–FR28; NFR1, NFR2, NFR7; UX-DR43; AD-12, AD-14, AD-20.

**Acceptance Criteria:**

**Given** prompt plus complete authorized Conversation Context
**When** a Provider attempt is considered
**Then** the live safety adapter returns a current versioned allow/block decision before invocation
**And** missing, stale, unversioned, indeterminate, always-blocked, or unauthorized-data results fail closed with content-safe audit evidence.

**Given** generated output
**When** proposal creation or automatic posting is considered
**Then** output passes the live current safety gate first
**And** failed output creates neither an approvable proposal nor a Conversation Message and no Approver override is available.

**Given** restricted or retried content
**When** policy is evaluated
**Then** restricted content requires an explicitly permitted tenant use case and Confirmation Response Mode, while a retry uses policy at least as restrictive as its first attempt
**And** tests cover every AD-20 always-blocked/restricted class without storing forbidden raw content in diagnostics.

### Story 5.10: Bind Provider Execution And Atomic Cost Reservations

As a Tenant Budget Owner,
I want live generation bounded by hard budget reservations,
So that concurrent calls and retries cannot exceed tenant or per-call limits.

**Dependencies:** Stories 5.2 and 5.6–5.9.

**Traceability:** FR4, FR5, FR10, FR24, FR25, FR28; NFR4, NFR6, NFR9, NFR10; UX-DR44; AD-9, AD-10, AD-13, AD-14, AD-18, AD-21.

**Acceptance Criteria:**

**Given** a Provider invocation is ready after context and safety gates
**When** cost is evaluated
**Then** current versioned pricing, numeric monthly tenant budget, numeric per-call caps, valid limits, and an atomic maximum estimated cost reservation are required before invocation
**And** missing, stale, indeterminate, unpriced, or 100%-exhausted state blocks the Provider while 80% produces an authorized warning.

**Given** a live Provider response or safe failure
**When** usage is known
**Then** actual usage is reconciled, unused reservation is released only after confirmed no usage, and eligible retries reuse the same reservation
**And** concurrent execution cannot over-reserve either cap or double-charge a retry.

**Given** the generation activity uses Microsoft Agent Framework or a direct adapter
**When** contracts, logs, telemetry, events, and API/UI outputs are inspected
**Then** the framework and Provider SDK remain adapter-local and secrets/raw payloads never cross public or durable safe boundaries
**And** at least 30 production-like executions supply the generation portions of the latency evidence.

### Story 5.11: Establish AI Membership And Post Idempotently

As a Conversation Participant,
I want automatic and approved replies posted once as hexa,
So that AI attribution and Conversation membership remain Conversations-owned.

**Dependencies:** external CONV-AI-1 and Stories 5.4–5.10.

**Traceability:** FR2, FR11, FR12, FR17, FR19–FR21, FR24; NFR1–NFR3; AD-6, AD-7, AD-12–AD-14, AD-18.

**Acceptance Criteria:**

**Given** CONV-AI-1 is available
**When** Agents establishes membership through IConversationClient.AddParticipantAsync and POST /api/v1/conversations/{conversationId}/participants
**Then** its delegated capability is limited to stable AI Party identity, ParticipantType.AiAgent/AIAgent, and ParticipantRole.Member
**And** exact retries are no-ops, type/role conflicts are typed, and cross-tenant or general participant administration is denied.

**Given** an automatic generated version or approved selected version has passed all current gates
**When** Dapr Workflow posts through Conversations
**Then** membership is confirmed first and AppendMessage uses deterministic MessageId/idempotency derived from interaction and version
**And** exactly the selected content is attributed to hexa's Party identity.

**Given** membership/posting is unavailable, unauthorized, conflicting, timed out, or replayed
**When** recovery executes
**Then** the interaction records safe pending/failure/success evidence without duplicate Conversation Messages
**And** no direct Conversation stream/event write or caller-authored AI message occurs.

### Story 5.12: Expose The Conversation Owned Call Hexa Action

As a Conversation Participant,
I want one clear Call hexa action with live status,
So that I can explicitly request governed help without ambiguous entry paths.

**Dependencies:** Stories 5.3–5.11.

**Traceability:** FR8, FR10–FR13, FR19–FR23, FR25; NFR1–NFR4; UX-DR24, UX-DR27, UX-DR32–UX-DR41, UX-DR47; AD-6, AD-12, AD-15, AD-17.

**Acceptance Criteria:**

**Given** an authorized eligible Conversation
**When** its FrontComposer surface renders
**Then** one Conversation-owned Call hexa action names hexa, opens an accessible prompt surface, and shows effective response mode before submission
**And** mention, command, ambient trigger, and alternate invocation entry points are absent.

**Given** the user submits or cancels
**When** the UI/API processes the action
**Then** Source Conversation, caller, Agent, prompt, response mode, authorization evidence, timestamp, and idempotency are captured exactly once, while cancel commits nothing
**And** focus, keyboard, Esc, localization, live-region, reduced-motion, and constrained-viewport behavior conform to FrontComposer/Fluent V5.

**Given** the call progresses or fails
**When** live status updates
**Then** requested, denied, context blocked, safety blocked, budget blocked, generating, proposal available, posting, posted, and failed states remain distinct
**And** unapproved or failed content never renders as a Conversation Message.

### Story 5.13: Expose Pending Proposal Counts And Needs My Action Queue

As an Approver,
I want in-product pending counts and a Needs my action queue,
So that I can discover proposals without an external notification channel.

**Dependencies:** Stories 5.5 and 5.7.

**Traceability:** FR13, FR18, FR20, FR22, FR25; NFR1, NFR2, NFR4; UX-DR1, UX-DR2, UX-DR6, UX-DR9, UX-DR29, UX-DR30, UX-DR34, UX-DR47; AD-8, AD-12, AD-15.

**Acceptance Criteria:**

**Given** authorized pending proposals
**When** count/status and queue projections update
**Then** the FrontComposer Agents entry, overview, and proposal queue show a consistent authorized pending count and Needs my action filter
**And** state, Source Conversation, caller, responsibility, ExpiresAt, and age are visible without proposal content.

**Given** proposals expire, terminate, change authority, or post
**When** projections catch up
**Then** counts and queue membership converge idempotently and expose pending/stale/degraded state before claiming success
**And** in-product entries link to proposal detail while email, push, and external-channel delivery remain absent.

**Given** loading, empty, filtered-empty, stale, error, denied, or cross-tenant access
**When** the surfaces render
**Then** each state is accessible and distinct, filter reset is offered where relevant, and counts disclose no unauthorized records
**And** keyboard/table semantics and whole-string localization pass UI conformance tests.

### Story 5.14: Expose Proposal Detail Actions And Accessible Expiry

As an Approver,
I want an accessible proposal workspace with immutable versions and deterministic expiry,
So that I can resolve exactly one version before it becomes terminal.

**Dependencies:** Stories 5.5, 5.7, and 5.11.

**Traceability:** FR13–FR18, FR20, FR22–FR24; NFR1–NFR5; UX-DR7, UX-DR8, UX-DR22, UX-DR28, UX-DR31–UX-DR40; AD-4, AD-5, AD-8, AD-13–AD-15.

**Acceptance Criteria:**

**Given** an authorized non-terminal proposal
**When** detail opens
**Then** current state, Source Conversation metadata, caller, Agent, Provider/model, response mode, policy references, exact ExpiresAt, selected version, and complete authorized version history are visible
**And** generated, edited, regenerated, approved, posting, posted, rejected, abandoned, expired, and failed states are not conflated.

**Given** an authorized edit, regeneration, approve, reject, or abandon action
**When** the command is accepted
**Then** it creates the exact durable transition/version, preserves all earlier versions, and posts only the selected approved version through Story 5.11
**And** current authorization and policy/safety gates are re-evaluated before their side effects.

**Given** expiry races an edit, regeneration, approval, or posting operation
**When** EventStore concurrency resolves the commands
**Then** exactly one valid ordering wins, a terminal proposal cannot later post, and the UI displays the authoritative result
**And** keyboard, focus return, live regions, confirmation, reduced motion, localization, and constrained-viewport fail-closed behavior pass.

### Story 5.15: Enforce Sensitive Retention And Legal Holds

As a Governance Operator,
I want finite retention and legal holds applied to sensitive Agent content,
So that protected data is not retained indefinitely or erased while held.

**Dependencies:** Stories 5.5 and 5.7.

**Traceability:** FR18, FR24, FR25, FR28; NFR2, NFR5; UX-DR46; AD-14, AD-22.

**Acceptance Criteria:**

**Given** an AgentInteraction reaches terminal state
**When** retention is scheduled
**Then** sensitive Agent content is eligible for expiry 365 days after the terminal timestamp while support-safe non-content evidence follows the approved tombstone contract
**And** posted Conversation Messages remain under Conversations retention.

**Given** an authorized legal hold is active
**When** retention expiry arrives
**Then** payload erasure/redaction and projection purge are suspended, hold scope/reason/actor/time are auditable, and the UI/API shows held state without sensitive disclosure
**And** release of the hold resumes the original policy calculation rather than rewriting history.

**Given** duplicate timers, restart, projection lag, unauthorized hold actions, or cross-tenant requests
**When** enforcement executes
**Then** behavior is idempotent and fail closed, EventStore history is never rewritten, and Level 4 integration evidence proves the actual protection/projection path
**And** partial state remains restrictive and visible.

### Story 5.16: Export And Cryptographically Delete Agent Audit Content

As an Authorized Governance Operator,
I want secure export and verifiable deletion of Agent audit content,
So that tenant governance requests have bounded, auditable outcomes.

**Dependencies:** Story 5.15.

**Traceability:** FR19, FR20, FR24, FR25, FR28; NFR1, NFR2, NFR5, NFR6; UX-DR10, UX-DR29, UX-DR46; AD-12, AD-14, AD-22.

**Acceptance Criteria:**

**Given** an authorized tenant-scoped export request
**When** export completes
**Then** the package is encrypted, time-limited, manifested, scoped to authorized records, and linked to auditable requester/purpose/time/evidence
**And** secrets, unrelated tenant content, raw stack traces, and prohibited unsafe content are absent.

**Given** an approved retention expiry or deletion request without legal hold
**When** deletion executes
**Then** protected content is cryptographically erased or redacted and affected projections are purged while immutable EventStore history retains only a support-safe non-content tombstone
**And** success is recorded only after payload protection and every affected projection confirm restrictive state.

**Given** authorization loss, legal hold, cross-tenant scope, partial failure, replay, or retry
**When** export/deletion is evaluated
**Then** the operation denies or remains restrictive and idempotent, with safe recovery evidence
**And** focused live tests prove no readable deleted content remains in public queries, projections, export staging, logs, or telemetry.

### Story 5.17: Operate Policies Metrics Budgets And Launch Status

As a Release and Governance Operator,
I want final policy authoring and measurable launch status,
So that every launch gate is operable and evidence-backed from one coherent surface.

**Dependencies:** Stories 5.2–5.16.

**Traceability:** FR1, FR3–FR5, FR18, FR22, FR25–FR28; NFR4–NFR10; UX-DR1–UX-DR5, UX-DR9–UX-DR23, UX-DR41–UX-DR47; AD-10, AD-15, AD-17, AD-20–AD-22.

**Acceptance Criteria:**

**Given** authorized operators open Agents policy/readiness surfaces
**When** they inspect or author configuration
**Then** read-only full-context policy, versioned safety policy, monthly/per-call cost controls, 365-day retention/legal hold/export/deletion, and launch readiness are present through FrontComposer/Fluent V5
**And** two or more titled sections use FluentAccordion, high-impact actions require confirmation, and future-only effects are explicit.

**Given** operational evidence is measured
**When** latency and success metrics are calculated
**Then** automatic accepted-to-post and confirmation accepted-to-proposal use p95 60 s/p99 120 s, approval-to-post uses p95 10 s/p99 30 s, and known pre-Provider rejection uses p95 2 s with at least 30 production-like executions per supported gate
**And** missing timestamps or insufficient samples report insufficient evidence.

**Given** the rolling 30-day launch cohort
**When** SM-2 and SM-3 are calculated
**Then** SM-2 passes only at 20% or greater with at least 50 eligible Conversations, while SM-3 passes only at 95% or greater terminal within 26 hours, expiry at most 20%, posting failure at most 2%, human resolution at least 70%, and audit completeness 100%
**And** budget warnings/blocks, evidence levels, all gate versions, and every remaining blocker are visible without sensitive leakage.

### Story 5.18: Prove Live Conformance And Reassess Readiness

As a Master Test Architect,
I want live cross-system evidence and a new readiness assessment,
So that production readiness is decided from actual bindings rather than historical story status.

**Dependencies:** CONV-AI-1 and Stories 5.1–5.17.

**Traceability:** FR1–FR28; NFR1–NFR10; UX-DR1–UX-DR47; AD-1–AD-22.

**Acceptance Criteria:**

**Given** all prior Epic 5 stories are complete
**When** the conformance suite runs through a platform-owned production-like topology
**Then** it produces Level 4 live component and Level 5 cross-system evidence for EventStore, Dapr Workflow restart/replay, tenant/Party authorization, complete Conversations context, safety, Provider/cost, AI membership/posting, proposals, audit governance, UI accessibility, and topology
**And** focused denial/revocation, cross-tenant, concurrency, idempotency, expiry race, stale capability, weaker retry, overspend, and restrictive deletion paths pass.

**Given** live performance and metric gates
**When** evidence is evaluated
**Then** the fixed sample, latency, SM-2, SM-3, audit-completeness, safety, budget, and no-secret criteria are calculated from authoritative timestamps/records
**And** skips, placeholders, lower evidence, insufficient cohorts, and where-applicable omissions remain explicit blockers.

**Given** the evidence package is complete
**When** implementation readiness is reassessed
**Then** a new dated report maps every FR, NFR, UX-DR, AD, external prerequisite, and Story 5.x to its verification evidence and independently declares READY or NOT READY
**And** the 2026-08-01 NOT READY report and Epics 1–4 remain unchanged as historical evidence.
