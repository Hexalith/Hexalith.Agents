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

FR9: The system supplies the Agent with Conversation Context according to the configured Conversation Context Policy; V1 uses only Conversation Context, excludes long-term memory/project/folder/tool/external-channel content, uses full context when it fits, never silently truncates oversized context, records context behavior and policy identity, and fails closed when context cannot be loaded or bounded safely.

FR10: The system handles provider failures, timeout, disabled provider/model state, invalid context, Content Safety Policy failures, and policy failures without posting incomplete or unsafe Agent Responses; failures create authorized status and Audit Evidence, do not create Conversation Messages, and do not create approvable proposals unless complete generated content exists and is explicitly audit-only.

FR11: In Automatic Response Mode, the system posts successful generated content to the Source Conversation as a Conversation Message attributed to the Agent's Party identity; the posted message references the Agent Call or trace identifier, is not authored by the caller, and is linked to Audit Evidence.

FR12: The system prevents automatic posting when authorization, Agent lifecycle, Provider/model, Party identity, Source Conversation access, Conversation Context Policy, Content Safety Policy, or generation status is invalid; no Conversation Message is created on failed checks or safety failure, and authorized status/audit distinguish failure classes without leaks.

FR13: In Confirmation Response Mode, successful generation creates a Proposed Agent Reply linked to the Source Conversation and Agent Call; it is not a Conversation Message, records caller/Agent/source/generated version/provider/model/response mode/current state, is discoverable by authorized Approvers, and V1 includes an in-product pending-proposal visibility surface.

FR14: The system preserves every generated, edited, and regenerated content version for each Proposed Agent Reply; edits and regenerations create immutable version entries and approval identifies exactly which version was approved and posted.

FR15: Authorized Approvers can edit Proposed Agent Reply content before approval; only authorized Approvers can edit, edits preserve prior versions and edit authorship, and edited content remains outside the Conversation until approved.

FR16: Authorized Approvers can request regeneration of a Proposed Agent Reply before approval; regeneration uses the same Source Conversation and Agent configuration unless an explicit configuration-version change is recorded, preserves prior versions, creates a new generated version, and is blocked after terminal states.

FR17: Authorized Approvers can approve a selected proposal version, causing exactly that version to be posted to the Source Conversation as `hexa`; the message is attributed to the Agent's Party identity and Audit Evidence links the approved version, Approver, approval timestamp, and posted Conversation Message.

FR18: Authorized Approvers or system policy can move a Proposed Agent Reply to rejected, abandoned, or expired terminal states; terminal proposals cannot be approved or posted, preserve all versions for audit, and expose deterministic expiry behavior through admin UI and API/client contracts.

FR19: The system enforces tenant isolation across Agent configuration, Provider selection, Agent Calls, Conversation Context, Proposed Agent Replies, Conversation posting, and Audit Evidence; cross-tenant call/inspect/approve/post actions are impossible, provider and Agent configuration does not leak across tenants unless explicitly platform-scoped and authorized, and audit/status queries return only tenant-authorized records.

FR20: The system enforces authorization for Agent administration, provider administration, Agent calling, proposal discovery, editing, regeneration, approval, rejection, abandonment, posting, and audit inspection; authorization failures happen before provider invocation or Conversation posting, admin UI and API/client contracts use the same rules, and authorization decisions are auditable without sensitive leakage.

FR21: The system fails closed when required Party, Conversation, Provider, Agent, tenant access, or approval policy state is missing, stale, ambiguous, disabled, or unavailable; missing or stale Conversation access blocks calls and approval posting, missing or disabled Agent Party identity blocks posting, and missing Provider/model state blocks generation.

FR22: The admin UI allows authorized administrators to manage Global Providers Aggregate entries, configure `hexa`, inspect lifecycle state, configure response and approver policy, and view Agent operation/proposal status; UI actions enforce the same authorization rules as API/client contracts, never expose Provider secrets, and distinguish active, disabled, invalid, pending proposal, failed call, and expired proposal states.

FR23: The system exposes stable API/client contracts for Provider administration, Agent configuration, Agent Calls, proposal workflow, status inspection, and audit inspection; callers are not required to use raw EventStore/internal aggregate/internal projection/provider SDK details, responses are structured for automation, and breaking changes are avoided unless explicitly versioned.

FR24: The system captures Audit Evidence for Agent configuration, Provider/model configuration, Agent Calls, generation attempts, proposal versions, edits, regenerations, approvals, rejections, abandonments, expirations, automatic posts, and final Conversation Messages; posted responses trace back to caller/Agent/source/provider/model/content/approval path, proposals preserve all versions, policy outcomes and identifiers are recorded where available, and audit is tenant-authorized without leaking unrelated tenant data or Provider secrets.

FR25: The system exposes status for Agent readiness, Provider/model readiness, recent Agent Call outcomes, proposal queues, generation failures, approval completion, and posting outcomes; authorized administrators can identify whether `hexa` is callable, distinguish key failure classes, and monitor launch adoption and approval workflow metrics.

FR26: Authorized administrators or release operators can define the active Content Safety Policy for `hexa`; production or production-like launch validation cannot enable `hexa` without one, the policy defines prompt constraints, restricted/blocked output categories, safety failure handling, and audit treatment, changes are auditable and future-only, and both response modes use the active policy unless a stricter mode-specific policy is configured.

FR27: The system applies Content Safety Policy before generated content becomes a Conversation Message or approvable Proposed Agent Reply; failed content cannot be posted or approved, safety failures create authorized status and Audit Evidence without exposing unsafe content where forbidden, and Approvers cannot override failures unless the policy defines an auditable override path.

FR28: V1 launch readiness requires explicit metric thresholds, latency targets, context-bounding behavior, and cost-control posture; SM-2/SM-3 must define numerator, denominator, target, measurement window, and cohort, latency targets must be defined for both response modes, cost controls or accepted risks must be recorded, and production-like generation cannot be enabled until safety, context, metric, latency, and cost controls are recorded.

### NonFunctional Requirements

NFR1: Security - Agent configuration, Provider administration, Agent Calls, proposal actions, posting, and audit inspection must enforce tenant and Party authorization before side effects.

NFR2: Privacy - Conversation Context, proposal content, and Audit Evidence must not leak across tenants or unauthorized Parties.

NFR3: Reliability - Agent Calls must never create partial Conversation Messages on failure, and Confirmation workflows must not lose generated or edited proposal versions.

NFR4: Observability - The system must expose enough status to debug configuration errors, Provider failures, authorization denials, pending approval bottlenecks, and posting failures.

NFR5: Auditability - Audit Evidence must preserve all generated and edited proposal versions and link final posted responses to their source call and approval path.

NFR6: Provider Safety - Provider secrets must be write-only or secret-backed where applicable and must never appear in logs, status payloads, audit records, or UI display.

NFR7: Content Safety - Agent generation must be governed by an active Content Safety Policy before generated content can create Conversation side effects.

NFR8: Context Bounds - Conversation Context must not be silently truncated; oversized Conversations must fail closed or use approved bounded-context behavior.

NFR9: Performance - V1 launch readiness must define latency targets for Automatic Response Mode and Confirmation Response Mode before production or production-like generation is enabled.

NFR10: Cost Control - V1 launch readiness must define cost-control posture before production or production-like generation is enabled.

### Additional Requirements

- Architecture does not specify an external starter template; Epic 1 Story 1 should scaffold the `Hexalith.Agents` module from the architecture Structural Seed using `.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, and the planned `src/` and `tests/` project layout.
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
- ProviderCatalog V1 capability metadata must include `ProviderId`, `ModelId`, display label, enabled state, secret reference/configured state, text-generation capability, context-window token limit, max-output token limit, timeout policy, and optional safe capability flags.
- Build V1 context only from authorized Conversations detail and visible timeline content; if full source context cannot be loaded fresh enough or fit the selected model context budget after reserving output tokens, record a context-blocked failure and create no provider call, proposal, or Conversation Message.
- Run authorization gates before every side effect and fail closed on missing, stale, ambiguous, disabled, or unavailable dependency state. Tenant access comes from Agents' local Tenants projection; conversation access/context from Conversations authorized queries; Party state from Parties adapters/projections; provider readiness from ProviderCatalog projections; approver rights from the snapshotted policy plus current dependencies.
- Make external effects idempotent: generation attempts use deterministic attempt ids, Conversation posting uses deterministic `MessageId` and idempotency key derived from `AgentInteractionId` plus selected `VersionId`, and retries must not duplicate messages or versions.
- Treat prompt, generated, edited, and context-derived content as sensitive conversation-derived content. Use EventStore payload-protection/redaction conventions before production; disable content-bearing workflows if protection is unavailable. Logs, telemetry, status, and audit summaries must not contain raw content, provider payloads, stack traces, or secrets.
- Admin UI and API/client surfaces must share the same public Agents contracts and authorization outcomes. FrontComposer UI registers an Agents domain/navigation like Tenants and calls Agents API/BFF/client boundaries, not EventStore streams, provider SDKs, or aggregate internals.
- Hexalith.Agents owns its own AppHost/local orchestration and deployable workloads; the root `agents` workspace remains a coordination/super-repo.
- Public contracts must be versioned and additive-first, with test gates for aggregate purity, fail-closed authorization, proposal immutability, replay/idempotency, Agent Framework workflow/session restore, MCP/A2A/tool schema contracts, idempotent generation/posting retries, tenant isolation, context-too-large blocking, provider-secret non-disclosure, FrontComposer UI/contract conformance, and audit completeness.
- Every agent task must have exactly one durable owner: Microsoft Agent Framework workflow in the .NET AgentHost, Dapr Workflow for deterministic long-running process orchestration, or optional Python Dapr Agents `DurableAgent` worker for bounded autonomous workloads. Public contracts and EventStore aggregates must not depend on Microsoft Agent Framework, Dapr AI, Dapr Agents, provider SDK, or workflow SDK types.
- Same-process tools use Microsoft Agent Framework function tools; reusable cross-process tools use MCP over Dapr service invocation by default; Dapr `MCPServer` is reserved for argument-aware RBAC, audit, redaction, durable retry, or per-tool observability; A2A is only for independently deployed remote agents; domain mutations remain domain commands.
- Use the local Hexalith stack baseline: .NET `10.0.300`-`10.0.301`, `net10.0`, `.slnx`, Central Package Management, sibling source dependencies for EventStore/Conversations/Parties/Tenants/FrontComposer, Dapr `1.18.4`, Aspire `13.4.6`, MediatR `14.1.0`, FluentValidation `12.1.1`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, xUnit v3 `3.2.2`, and Shouldly `4.3.0`.

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

UX-DR24: Implement the unresolved Conversation Agent Call affordance so any chosen mention/command/action/participant pattern visibly names `hexa`, captures Source Conversation, caller, Agent, prompt, response mode, authorization decision, and timestamp before provider invocation, and never renders unapproved content as a Conversation Message.

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

Cross-cutting implementation note: FR19, FR20, and FR21 are mapped to Epic 2 for primary coverage, but tenant isolation, authorization, and fail-closed dependency handling are acceptance constraints for every epic. FR22 is mapped to Epic 4 for complete admin UI coverage, but UI slices are delivered incrementally in the epics where their user workflows first appear.

## Epic 1: Tenant Agent Setup And Governance

Agent Administrators can configure `hexa` for a tenant with Party identity, provider/model selection, response mode, approver policy, lifecycle, and active content safety policy.

### Story 1.1: Buildable Agents Module Shell And Public Boundaries

As an Integration Developer,
I want a buildable `Hexalith.Agents` module shell with public contracts and project boundaries,
So that governed Agent setup can be implemented through stable Hexalith conventions without leaking infrastructure details.

**Acceptance Criteria:**

**Given** the agents workspace has no completed `Hexalith.Agents` module
**When** the story is implemented
**Then** the solution contains a buildable `Hexalith.Agents.slnx`, `global.json`, `Directory.Build.props`, `Directory.Packages.props`, workspace-root `src/` projects, and module `tests/` projects matching the architecture Structural Seed
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
