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
  - /home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/external-dependency-register.md
  - /home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/launch-readiness-register.md
  - /home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md
  - /home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-09.md
---

# Hexalith Agents - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Hexalith Agents, decomposing the requirements from the PRD, UX Design if it exists, and Architecture requirements into implementable stories.

## Replacement Authority — 2026-09-09

The approved `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-09.md`, current PRD, Architecture Spine, UX spines, external-dependency register, launch-readiness register, and active Epics 5–8 are the machine-visible implementation authority. The 2026-08-02 proposal remains historical replacement evidence where it does not conflict with this correction.

Epics 1–4 and every completed story beneath them remain unchanged historical delivery evidence with status `completed`. Their completion proves only the evidence recorded at the time; it does not establish live production conformance, current callability, or release readiness. Any conflicting historical criterion is `mustNotImplement` and resolves through the replacement-authority map in `epic-5-superseded-2026-08-01.md` without rewriting the completed story text.

The former 18-story Epic 5 is superseded and non-executable. Active forward work consists only of replacement Epics 5–8 and exactly 29 stories. `RQ-1` remains a non-estimated release gate outside the story backlog; it aggregates completed evidence and never owns missing implementation.

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

FR10: The system handles Provider failures, timeout, disabled provider/model state, invalid context, Content Safety Policy failures, and policy failures without posting incomplete or unsafe Agent Responses; failures create authorized status and Audit Evidence, do not create Conversation Messages, and in Confirmation Response Mode do not create Proposed Agent Replies. Any failed or incomplete generated content retained for authorized audit is stored only in a separate, non-approvable failure record.

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

FR21: The system fails closed when required Party, Conversation, Provider, Agent, tenant access, or approval policy state is missing, stale, ambiguous, disabled, or unavailable; missing or stale Conversation access blocks calls and approval posting, missing or disabled Agent Party identity blocks posting, and missing Provider/model state blocks generation. A critical external dependency is not implementation-ready until every PRD §8 commitment field is accepted; an `Uncommitted` record, missing target, or missing executable compatibility command blocks every consuming story from `ready-for-dev`.

FR22: The admin UI allows authorized administrators to manage Global Providers Aggregate entries, configure `hexa`, inspect lifecycle state, configure response and approver policy, and view Agent operation/proposal status; UI actions enforce the same authorization rules as API/client contracts, never expose Provider secrets, distinguish active, disabled, invalid, pending proposal, failed call, and expired proposal states, and satisfy NFR-13 accessibility/localization/responsive safety plus NFR-14 interaction-performance evidence.

FR23: The system exposes stable API/client contracts for Provider administration, Agent configuration, Agent Calls, proposal workflow, status inspection, and audit inspection; callers are not required to use raw EventStore, internal aggregate, internal projection, or Provider SDK details; responses are structured for automation; JSON object evolution is additive within V1; public enums define `Unknown = 0` and may add but never reuse values; no public member or enum value is removed, renamed, or semantically reused within V1; and any breaking public change requires a new major package/API version plus package-consumer compatibility tests.

FR24: The system captures Audit Evidence for Agent configuration, Provider/model configuration, Agent Calls, generation attempts, proposal versions, edits, regenerations, approvals, rejections, abandonments, expirations, automatic posts, and final Conversation Messages; posted responses trace back to caller/Agent/source/provider/model/content/approval path, proposals preserve all versions, policy outcomes and identifiers are recorded where available, and audit is tenant-authorized without leaking unrelated tenant data or Provider secrets.

FR25: The system exposes status for Agent readiness, Provider/model readiness, recent Agent Call outcomes, proposal queues, generation failures, approval completion, and posting outcomes; authorized administrators can identify whether `hexa` is callable, distinguish key failure classes, and monitor launch adoption and approval workflow metrics.

FR26: Authorized administrators or release operators can define and publish the versioned Content Safety Policy for `hexa`; production or production-like validation cannot enable `hexa` without one. The policy binds always-blocked and restricted categories, prompt/context and output gates, failure/audit treatment, no Approver override, future-only changes, and no weaker retry.

FR27: The system applies Content Safety Policy to prompt plus complete Conversation Context before Provider invocation and to generated output before any proposal or Conversation side effect; failed content cannot be posted or approved, safety failures create authorized status and Audit Evidence without forbidden disclosure, and Approvers cannot override failures.

FR28: V1 launch readiness requires fixed metric thresholds, latency targets, full-context behavior, hard cost controls, audit governance, NFR-11 through NFR-14, accepted external-dependency commitments, and normative Levels 4–5 evidence. Controlled production-like qualification may run only after its safety, context, cost, audit, and dependency gates are active; production enablement remains blocked until release gate `RQ-1` records READY. Cost reserves atomically before Provider invocation and reconciles actual usage; runtime latency gates use their exact p95/p99 thresholds with at least 30 production-like executions; lower evidence, skips, placeholders, and conditional results cannot establish production readiness.

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

- **NFR-11 Availability And Recovery:** EventStore business state has RPO 0. Restart or replay cannot duplicate Provider attempts, proposal versions, timers, reservations, or Conversation posts. A production-like recovery exercise restores interaction processing within 15 minutes and preserves every terminal decision.

- **NFR-12 Capacity And Backpressure:** Before enablement, each environment records numeric per-tenant and system-wide concurrency, queue-depth, and backpressure limits. Exceeding a limit queues or rejects with a safe typed outcome before Provider invocation. Cross-tenant fairness and cost caps remain enforced. The release profile and numeric limits are visible in the readiness registry, not hidden in host configuration.

- **NFR-13 Accessible, Localizable, Responsive UI:** The final UX spines are binding launch authority. Interactive V1 surfaces meet WCAG 2.2 AA behavior, use whole-string localization with English/French key parity, and fail closed for high-impact actions when the viewport cannot present required context.

- **NFR-14 UI Interaction Performance:** In the production-like profile, an authorized page reaches a usable non-loading state at p95 ≤ 2.5 seconds; a submitted command renders an authoritative pending acknowledgement at p95 ≤ 500 ms; a projection-visible terminal change renders and is announced at p95 ≤ 2 seconds. Each gate uses at least 30 executions and returns `InsufficientEvidence`, not pass, when timestamps or samples are missing.

### Additional Requirements

- Architecture specifies no external starter template. The forward baseline must use the corrected Structural Seed: `.slnx`, root build/package files, `Contracts`, `Client`, `Server`, `UI`, `Testing`, and focused test projects, with module-owned `AppHost`, `Aspire`, and `ServiceDefaults` absent.
- Hexalith Agents is a full EventStore-backed domain module. The V1 durable aggregate inventory is `Agent`, platform-scoped `ProviderCatalog` in reserved tenant `system`, `TenantProviderEnablement`, `AgentInteraction`, `BudgetLedger`, `TenantGovernancePolicy`, platform-scoped `ContentSafetyPolicy`, `ConversationAgentState`, `AuditInspection`, `SecurityEventLog`, `LaunchReadinessGate`, `LegalHold`, `AuditExport`, and `ProtectedDeletion`. Dapr Workflow history and optional framework session state never become business truth.
- Aggregate handlers are pure and emit events only. Provider calls, Conversations reads/posts, Parties validation, Tenants projection reads, safety checks, admission, expiry timers, notifications, and secret access execute outside aggregates and return through deterministic commands.
- `AgentInteraction` snapshots Agent/configuration/instructions/response/approver/provider/model/context/safety/caller/source versions at request time, while current provider readiness and safety may tighten or block later steps without retargeting the interaction.
- Proposal content is append-only and immutable across generated, edited, and regenerated versions. Approval selects exactly one version; rejected, abandoned, and expired proposals cannot later post; generation failures create only a separate non-approvable failure record.
- Conversations context, AI membership, and final posting use supported Conversations client/API seams only. Direct stream writes are forbidden, and a Proposed Agent Reply is never a Conversation Message.
- Agent identity stores stable Party references only. Posting requires one valid current Agent Party identity plus limited Conversations membership as `ParticipantType.AiAgent`/`AIAgent` and `ParticipantRole.Member` through `EXT-CONV-AI-1`.
- Approver authority resolves from the snapshotted Agents policy plus current caller, predefined Party, tenant-role, and Conversation Facilitator evidence. Missing, stale, ambiguous, revoked, or unavailable authority fails closed.
- Provider SDK and credential details remain adapter-local. `ProviderReadinessResult` exposes exactly `OperationalState`, `Callability`, `ReasonCode`, `CapabilityVersion`, `ObservedAt`, and exclusive `ValidUntil`; only the three architecture-valid state triples are accepted, and unknown or invalid combinations block.
- Every provider-dependent step maintains and advances the interaction's durable capability high-water mark, records the accepted `EffectiveProviderCapabilityVersion`, and rechecks live readiness and limits. Snapshot version remains provenance rather than current authority.
- Complete Conversation Context is freshly authorized and measured with the `EXT-TOKEN-1` provider/model tokenizer before initial generation and regeneration. No truncation, summary, window, sampling, substitute retrieval, or stale context reuse is permitted.
- Two-stage safety through `EXT-SAFETY-1` evaluates prompt plus complete context before Provider invocation and generated output before proposal/posting. Missing, stale, unversioned, indeterminate, always-blocked, or weaker-retry outcomes fail closed with no Approver override.
- External effects follow the AD-13 prepared-attempt state machine: deterministic descriptor and fingerprint, atomic budget reservation, durable capacity admission/fence, EventStore authorization fact, linearizable `BeginInvocation`, idempotent Provider call/outcome lookup, reconciliation, and release.
- Cost controls require current pricing, numeric monthly and per-call caps, an 80% warning, 100% fail-closed enforcement, atomic maximum-cost reservation, actual-usage reconciliation, and retry reuse. Reporting-only monitoring is insufficient.
- One shared platform-composed allocator owns numeric tenant/system concurrency and queue limits, durable queue/admission identities, fences, cancellation/expiry, and `WeightedRoundRobinV1` fairness. Process-local counters or queues are forbidden.
- Authorization gates precede every side effect and use current tenant, Party, Conversation, Agent, Provider, safety, cost, capacity, and policy evidence. UI/BFF high-risk pending locks are advisory and scoped to user session, resource, and operation family.
- Every tenant/auth change names focused cross-tenant denial and revocation tests, the executable verification command, and the result. Broad build or happy-path evidence alone cannot close such work.
- Sensitive prompt, context, generated, edited, and audit content uses the `EXT-PROTECTION-1` EventStore payload-protection engine and `EXT-SECRETS-1`; production-like content work fails closed until both are `Available`. Provider secrets resolve only through `EXT-SECRETS-1`; raw content, payloads, secrets, PII, and stack traces are forbidden from logs, telemetry, summaries, and browser evidence.
- The platform-owned host in `EXT-HOST-1` composes the Agents DomainService/UI with EventStore, Conversations, Parties, Tenants, Provider and safety adapters, Dapr Workflow, readiness, capacity, secrets, identity, health, telemetry, and browser-evidence ingress.
- Dapr Workflow is the sole V1 durable execution owner. Microsoft Agent Framework may exist only inside a generation activity after `EXT-PROVIDER-1` commits it. Python DurableAgent, MCP, A2A, tools, and alternate workflow owners are out of V1.
- The EventStore `LaunchReadinessGate` aggregate is the sole readiness-record writer. The `launch-readiness` projection selects greatest committed revision, never greatest `ObservedAt`, and never falls back to an older Pass when the newest record is invalid, incomplete, stale, or blocking.
- `OperationGateMatrixVersion = 2` is the single additive gate mapping consumed by API, BFF, UI, workflow, and readiness projection. Every observation carries exact `ScopeKind` and an `AuthorizedProducer`; a missing operation family, unknown/old version where current is required, invalid scope/producer, missing record, or non-inventory gate blocks. The Live-Seam Matrix names the integration test required for every live claim.
- The authoritative projection inventory is: `agent-setup-readiness`, `provider-capability-pricing`, `agent-interaction-status`, `proposal-detail`, `proposal-version-history`, `pending-proposal-queue`, `pending-proposal-count`, `audit-evidence`, `budget-reservation-usage`, `retention`, `legal-hold`, `export`, `deletion`, `launch-readiness`, `runtime-metrics`, `browser-ui-metrics`, and `product-metrics`.
- NFR-11 recovery freezes the pre-fault eligible nonterminal cohort, uses one monotonic clock origin, proves EventStore RPO 0, restores processing within 15 minutes, preserves terminal decisions, and inventories duplicate-sensitive external effects.
- NFR-12 evidence records all numeric concurrency/queue values and proves exact weighted fairness, fencing, no starvation, replica/crash/cancel/expiry recovery, and coexistence with cost caps in the `EXT-TOPOLOGY-1` fixture.
- NFR-13 evidence covers every interactive V1 route and high-impact state for WCAG 2.2 AA, keyboard/focus/semantic/live-region behavior, whole-string English/French parity, FrontComposer/Fluent V5 inheritance, and restrictive-viewport blocking.
- NFR-14 uses authenticated browser-monotonic, kind-discriminated samples correlated to safe server evidence; each sample kind requires at least 30 production-like executions and missing/invalid ticks, correlation, localized live-region mutation, or samples yields `InsufficientEvidence`.
- Public contracts are versioned and additive-first. Evidence Levels retain PRD meanings: Level 1 structure, Level 2 pure behavior, Level 3 fail-closed deferred seam, Level 4 live component, and Level 5 production-like cross-system evidence.
- The local stack baseline is SDK `10.0.301` with `latestPatch`, `net10.0`, C# 14, `.slnx`, Central Package Management, Aspire `13.4.6`, Dapr/Workflow `1.18.5`, CommunityToolkit Aspire Dapr `13.4.1-beta.687`, MediatR `14.2.0`, FluentValidation `12.1.1`, Fluent UI `5.0.0-rc.4-26180.1`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and unselected Provider/Agent Framework SDKs until `EXT-PROVIDER-1` commits them.
- The external-dependency register is the only commitment authority for `EXT-CONV-AI-1`, `EXT-CONV-UI-1`, `EXT-HOST-1`, `EXT-PROVIDER-1`, `EXT-SAFETY-1`, `EXT-TOKEN-1`, `EXT-SECRETS-1`, `EXT-PROTECTION-1`, and `EXT-TOPOLOGY-1`; any `Uncommitted` record blocks its consuming stories from `ready-for-dev`.
- The launch-readiness register is the machine-testable callability and qualification authority. `RQ-1` is a non-estimated release gate outside the development backlog and currently returns NOT READY.

### UX Design Requirements

UX-DR1: Register an Agents domain/category in the FrontComposer shell, with authorization-safe navigation and links for Agents overview, `hexa` configuration, Provider catalog, Approver policy, Conversation context policy, Content Safety policy, cost controls, Conversation-owned invocation, Proposal queue/detail, Launch readiness, Operational status, Audit governance, and Audit evidence.

UX-DR2: Implement the Agents overview as the default Agents navigation surface showing `hexa` lifecycle separately from authoritatively proven callability, response mode, Provider/model, pending proposal count, recent failures, evidence freshness, and safe tenant blockers.

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

UX-DR20: Implement `agent-readiness-badge` so lifecycle `active` is never treated as callability; Success requires a current authoritative readiness projection proving callable, while missing, blocked, stale, or `InsufficientEvidence` gates remain non-success blockers.

UX-DR21: Implement `provider-status-badge` directly from `ProviderReadinessResult`: `Ready / Callable / None` is Success, `Degraded / Callable / NonBlockingOperationalWarning` is Warning, and `Blocked / Blocked / <defined blocker>` is blocking; UI policy never infers or overrides callability and never exposes secrets.

UX-DR22: Implement `proposal-state-badge` with distinct generated, edited, regenerated, pending approval, approved, rejected, abandoned, expired, posting pending, posted, and posting failed states. `Approved` and `posting pending` are non-success progress; only authoritative `posted` proves a Conversation Message exists and may use Success.

UX-DR23: Implement response mode selection as a mutually exclusive Fluent segmented control or radio group for Automatic Response Mode and Confirmation Response Mode, with copy that makes future-only effect explicit and does not visually bias automatic mode.

UX-DR24: Implement the Conversation-owned **Call hexa** action as the sole V1 invocation entry; it visibly names `hexa`, captures Source Conversation, caller, Agent, prompt, response mode, authorization decision, and timestamp before Provider invocation, and never renders unapproved content as a Conversation Message.

UX-DR25: Preserve canonical Agent readiness states across surfaces: callable; active but not proven callable; checking; invalid configuration; missing Party identity; Provider unavailable; and disabled.

UX-DR26: Preserve the architecture-valid Provider/model readiness triples across surfaces and fail closed for missing, stale, disabled, unconfigured, unpriced, invalid-limit, unavailable, failed, regressed, unknown, or indeterminate state.

UX-DR27: Preserve canonical Agent Call states and truth transitions across surfaces: submitted; authoritative pending only after rendering a server/EventStore-accepted identity plus projection/version; authorized; denied; context loading; context blocked; generating; generation failed; generated; and projection-confirmed terminal. Optimistic state, timeout, SignalR nudge, or unrelated projection change proves neither pending nor terminal.

UX-DR28: Preserve canonical Proposal lifecycle states across surfaces: generated, edited, regenerated, pending approval, approved, rejected, abandoned, expired, posting pending, posted, and posting failed.

UX-DR29: Preserve audit availability states across surfaces: audit pending, audit available, audit delayed, and audit unavailable.

UX-DR30: Every grid/list surface must distinguish loading, empty, filtered-empty, error, permission-denied, and stale/degraded where relevant; empty states must not leak unauthorized records and filtered-empty states must offer filter reset.

UX-DR31: Editing is explicit, regeneration is distinct, and approval applies only to a selected version. The advisory pending lock permits at most one command for `(user session, resource identity, operation family)` across `ProposalResolution`, `PolicyPublication`, `TenantBudgetUpdate`, `LegalHold`, `ExportRequest`, and `DeletionRequest`; unrelated resources/families remain available and EventStore concurrency/idempotency remain authoritative.

UX-DR32: Keyboard and focus behavior must support `Esc` closing transient UI without committing, focus returning to the trigger, approval/rejection controls being keyboard reachable, and no required action or denial reason being hover-only.

UX-DR33: Every interactive V1 route and high-impact state must meet WCAG 2.2 AA behavior using FrontComposer FC-A11Y primitives including skip links, focus visibility, named navigation landmarks, keyboard shell controls, semantic labels, and status live regions.

UX-DR34: Proposal queue, provider catalog, and audit/status grids must expose table semantics, header relationships, sort/filter state, and row action names.

UX-DR35: Proposal editor must be fully keyboard operable for edit, selected-version choice, metadata comparison, regeneration, approval, rejection, abandonment, and exit without committing.

UX-DR36: Live regions must announce generation failed, proposal created, proposal expired, authoritative `posted`, posting failed, and permission denied using localized whole strings after render commit; ordinary pending progress is not assertive and the evidence proves announcement-ready DOM state rather than speech completion.

UX-DR37: Focus-trapped dialogs or confirmation panels must provide a safe non-committing escape and return focus to the triggering control.

UX-DR38: Reduced-motion users must not depend on animation to perceive generation, approval, or posting state changes.

UX-DR39: Responsive behavior must be desktop-first; phone may support read-only status/proposal reference/lightweight review, tablet stacks metadata/editor/version history and prioritizes grid columns, desktop is the primary mode, and wide desktop uses extra width for split views rather than decoration.

UX-DR40: At the most restrictive supported viewport, `ProposalResolution`, `PolicyPublication`, `TenantBudgetUpdate`, `LegalHold`, `ExportRequest`, and `DeletionRequest` are unavailable with a visible reason whenever required decision context cannot be presented safely; review-only access remains available.

UX-DR41: Use FrontComposer capabilities intentionally: FC-LYT for FullWidth and Constrained layouts, FC-TBL for grids/filter summaries/row detail/empty/error states, FC-A11Y for shell and custom override accessibility, FC-L10N for domain labels and workflow copy, policy-gated navigation for authorization-safe entry visibility, and pending command/status patterns for generation/approval/posting transitions without promoting pending to success.

UX-DR42: Implement a read-only Conversation context policy surface showing complete-context-or-blocked behavior, effective model budget, policy version, and safe blocking reason; expose no bounded-context control.

UX-DR43: Implement Content Safety authoring for fixed blocked categories, restricted handling, policy validation/version, and explicit authorized publish with future-only and no-weaker-retry copy.

UX-DR44: Implement cost-control authoring/status for monthly tenant budget, per-call caps, usage/reservations, 80% warning, 100% block, and indeterminate fail-closed state.

UX-DR45: Implement Launch readiness showing `Pass`, `Block`, `InsufficientEvidence`, and `Stale`; current `ObservedAt`/exclusive `ValidUntil`; exact NFR-9 and NFR-14 gates; SM-2/SM-3 cohorts/windows; sample sufficiency; Levels 4–5; and all safe blockers. Lower evidence, skips, missing timestamps/references, invalid browser samples, and insufficient samples cannot render as Pass.

UX-DR46: Implement audit governance for 365-day retention, legal hold, encrypted time-limited export, deletion/projection purge, and restrictive partial-failure state; high-impact actions require confirmation and authoritative evidence before success.

UX-DR47: Implement in-product proposal notifications only through authorized pending counts, queue links, and Conversation status entries; notification copy exposes state/expiry but not proposal content. Multi-section policy surfaces use `FluentAccordion` and all UI uses FrontComposer/Fluent UI Blazor v5.

UX-DR48: A generation failure must create no proposal, proposal version, proposal queue entry, proposal notification, editor, or approval/posting action. Any retained failed or incomplete content appears only in a separate authorized, non-approvable failure record under operational status/audit evidence.

UX-DR49: Collect NFR-14 evidence with one injected browser-monotonic clock origin and discriminated `PageUsability`, `AuthoritativePending`, and `TerminalRenderAnnouncement` samples. Each kind has only its permitted ticks, deterministic identity, authenticated qualification-session and safe server correlation, exact-duplicate idempotency, conflicting-duplicate rejection, and at least 30 qualifying production-like executions.

UX-DR50: Every high-impact accepted command renders the explicit truth flow `submitted -> authoritative pending -> projection-confirmed terminal`; approval/posting renders `approved -> posting pending -> posted`; pending or partial outcomes never receive success styling, and authoritative rejection before acceptance terminates without inventing an authoritative-pending state.

### FR Coverage Map

FR1: Epic 5 - Configure `hexa` through live authorized EventStore operations and expose current setup state.

FR2: Epics 5 and 6 - Bind exactly one live Party identity, prove posting eligibility, and attribute the automatic response to `hexa`.

FR3: Epic 5 - Manage lifecycle without conflating `active` with authoritative callability.

FR4: Epics 5 and 8 - Govern Provider/model capability, enablement, pricing, secret-configured state, and operational policy.

FR5: Epics 5 and 6 - Select an enabled model and carry its current safe capability version into automatic execution.

FR6: Epic 5 - Configure future-only Automatic or Confirmation Response Mode through live public operations.

FR7: Epics 5 and 7 - Configure, resolve, and audit current Approver Policy authority.

FR8: Epic 6 - Provide the sole Conversation-owned **Call hexa** action and create one authorized interaction.

FR9: Epic 6 - Load the complete authorized Conversation, measure it exactly, or block before Provider invocation.

FR10: Epic 6 - Return precise fail-closed generation outcomes and keep failed content outside proposals/messages.

FR11: Epic 6 - Post exactly one successful automatic response as `hexa` through Conversations.

FR12: Epic 6 - Prevent automatic posting whenever any authorization, context, safety, budget, capacity, identity, or posting gate fails.

FR13: Epic 7 - Create and expose a pending Proposed Agent Reply only after successful confirmation-mode generation.

FR14: Epic 7 - Preserve every generated, edited, and regenerated proposal version immutably.

FR15: Epic 7 - Allow one authorized edit transition that preserves the prior version and authorship.

FR16: Epic 7 - Regenerate through fresh current gates without losing history or reusing changed attempt inputs.

FR17: Epic 7 - Approve and post exactly one selected version as `hexa` with linked evidence.

FR18: Epics 7 and 8 - Reject, abandon, or deterministically expire proposals while retention/hold governance preserves evidence.

FR19: Epics 5–8 - Enforce tenant isolation on every setup, call, proposal, governance, status, and audit surface with focused cross-tenant denial evidence.

FR20: Epics 5–8 - Enforce role/policy authorization before every side effect and keep API/UI outcomes aligned.

FR21: Epics 5–8 - Fail closed on dependency uncertainty and block story readiness until every declared external dependency is committed or available.

FR22: Epics 5–8 - Deliver the live FrontComposer/Fluent V5 administration, call, proposal, governance, and evidence surfaces under NFR-13/NFR-14.

FR23: Epics 5–8 - Publish stable additive public contracts for all active outcomes with package-consumer and API/UI parity evidence.

FR24: Epics 5–8 - Capture tenant-scoped durable evidence for setup, attempts, proposal actions, posting, policy, governance, and final messages.

FR25: Epics 5–8 - Expose authoritative readiness, runtime, proposal, governance, performance, and blocker status.

FR26: Epics 6 and 8 - Publish and operate the versioned Content Safety Policy used by prompt/context and output gates.

FR27: Epics 6 and 7 - Enforce fresh no-weaker safety before Provider, proposal, regeneration, approval, and posting side effects.

FR28: Epics 5–8 - Implement every launch control and emit bounded evidence; release gate `RQ-1` aggregates live attainment outside the story backlog.

## Epic List

### Epic 1: Tenant Agent Setup And Governance

Agent Administrators can configure `hexa` for a tenant with Party identity, provider/model selection, response mode, approver policy, lifecycle, and active content safety policy.

**Status:** completed — historical evidence only; not live production conformance.

**FRs covered:** FR1, FR2, FR3, FR4, FR5, FR6, FR7, FR26

### Epic 2: Safe Conversation Invocation And Automatic Replies

Conversation Participants can explicitly call `hexa` from a Conversation and receive a governed automatic reply, with authorization, context bounds, safety, dependency uncertainty, and failure handling enforced before side effects.

**Status:** completed — historical evidence only; not live production conformance.

**FRs covered:** FR8, FR9, FR10, FR11, FR12, FR19, FR20, FR21, FR27

### Epic 3: Proposal Review And Approval Workflow

Approvers can discover, edit, regenerate, approve, reject, abandon, or let Proposed Agent Replies expire, while all versions remain preserved and only an approved selected version can be posted.

**Status:** completed — historical evidence only; not live production conformance.

**FRs covered:** FR13, FR14, FR15, FR16, FR17, FR18

### Epic 4: Operational Visibility, Audit, Integration, And Launch Readiness

Administrators, operators, and integration developers can manage Agents through UI/API contracts, inspect status and audit evidence, and enforce launch-readiness gates for metrics, latency, context, and cost posture.

**Status:** completed — historical evidence only; not live production conformance.

**FRs covered:** FR22, FR23, FR24, FR25, FR28

### Epic 5: Live Governed Setup And Honest Readiness

Agent Administrators can configure `hexa` through live public operations and receive an authoritative tenant-scoped explanation of lifecycle, setup validity, Provider state, and callability. The epic delivers a complete usable setup outcome and creates the safe foundation consumed by later outcomes.

**Status:** active forward backlog.

**FRs covered:** FR1–FR7, FR19–FR25, FR28

**Primary NFR ownership:** NFR1–NFR6, NFR11, NFR12, NFR13

**Natural dependencies:** Story readiness is externally gated by `EXT-HOST-1`, `EXT-PROVIDER-1`, `EXT-SECRETS-1`, and `EXT-TOPOLOGY-1` only where declared; the epic does not depend on Epics 6–8.

### Epic 6: One Safe Automatic Conversation Response

A Conversation Participant can use the sole **Call hexa** action and receive exactly one safe attributed response, or a precise fail-closed outcome before any unsafe Provider or Conversation effect. The completed automatic path is usable without Epic 7.

**Status:** active forward backlog.

**FRs covered:** FR2, FR4, FR5, FR8–FR12, FR19–FR28

**Primary NFR ownership:** NFR1–NFR12, NFR13, NFR14

**Natural dependencies:** builds on Epic 5 setup/readiness; declared seams are `EXT-TOPOLOGY-1`, `EXT-TOKEN-1`, `EXT-SAFETY-1`, `EXT-PROVIDER-1`, and `EXT-CONV-AI-1`.

### Epic 7: Complete Confirmation And Approval

An Approver can discover, revise, regenerate, resolve, and post exactly one proposal version without losing history or bypassing current gates. The epic builds on the generation/posting foundation while delivering a complete confirmation-mode outcome.

**Status:** active forward backlog.

**FRs covered:** FR7, FR13–FR25, FR27, FR28

**Primary NFR ownership:** NFR1–NFR9, NFR11, NFR13, NFR14

**Natural dependencies:** builds on Epics 5–6; regeneration/posting declare `EXT-PROVIDER-1`, `EXT-TOKEN-1`, `EXT-SAFETY-1`, and `EXT-CONV-AI-1` where consumed.

### Epic 8: Governance Operations And Release Qualification

Governance and release operators can retain, export, delete, operate safety/cost/governance policy, calculate metrics, prove UI conformance/performance, and inspect current launch evidence without turning final assessment into an implementation story.

**Status:** active forward backlog.

**FRs covered:** FR4, FR18–FR26, FR28

**Primary NFR ownership:** NFR1–NFR14

**Natural dependencies:** governance operations build on Epic 5 durable/public foundations and may proceed alongside later runtime work when their declared seams are committed; evidence inspection consumes bounded outputs from Epics 5–8 but owns no missing implementation.

### Release Gate RQ-1: Final Operational Qualification

`RQ-1` is not an epic story, is not estimated, and is not part of the executable backlog. After Epics 5–8 and all consumed dependencies complete, it evaluates the 18 minimum readiness gates, current Levels 4–5 evidence, approved production-like samples, real metric attainment, and blockers, then produces a dated READY/NOT READY report.

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

## Epic 5: Live Governed Setup And Honest Readiness

Agent Administrators can configure **hexa** through live public operations and receive an authoritative tenant-scoped explanation of lifecycle, setup validity, Provider state, and callability.

**Status:** active forward backlog.

**Story count:** 8.

**Dependency topology:** 5.1 establishes the build/package/boundary baseline; 5.2 and reopened 5.3 add independently usable Agent and platform-catalog/tenant-enablement operations; 5.4 binds trusted principals, identity, authorization, and security evidence; 5.5 publishes matrix-v2 readiness truth and versioned routes; 5.6 proves the platform-hosted topology and live integration tier; 5.7 consumes only prior setup stories to gate activation; 5.8 binds payload protection after the integration tier and becomes the content-bearing prerequisite for Epics 6–8. No story depends on a later story.

### Story 5.1: Establish Build Package Boundary And Basic CI Gates

As an Integration Developer,
I want a clean-checkout source, package, boundary, and basic CI baseline,
So that every later Agents change is built and consumed through the corrected platform boundary.

**Primary Demonstrable Outcome:** A clean checkout restores, builds, packages, validates a package consumer, and runs the basic boundary lane with no module-owned hosting project.

**Dependencies:**

- **Prior stories:** None.
- **External:** EXT-HOST-1 must be at least Committed before this story can enter ready-for-dev; its target contract is needed to remove the old ownership without inventing a replacement.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** the corrected Architecture Structural Seed and a clean checkout with only root-declared submodules initialized
**When** the Agents solution is restored and built
**Then** Hexalith.Agents.AppHost, Hexalith.Agents.Aspire, and Hexalith.Agents.ServiceDefaults projects, references, packages, and tests that enforce their ownership are absent
**And** Contracts, Client, Server, UI, Testing, and focused test projects retain the architecture dependency direction and build without warnings.

**Given** source-mode development and package-mode consumption
**When** the story verification lane runs
**Then** Debug/source use resolves only intentional local project references, while Release/package validation consumes the produced public packages without reverse references or Provider, workflow, Dapr-hosting, or UI implementation types in Contracts
**And** package inventory, public API, central package version, and solution-format checks fail on any drift.

**Given** the root CI workflow
**When** a change introduces a forbidden project/reference, inline package version, legacy solution, missing package consumer, or source-only Release dependency
**Then** a named source/package/boundary/basic test gate fails with a support-safe diagnostic
**And** the gate never weakens warnings-as-errors or initializes nested submodules.

**Given** EXT-HOST-1 is Uncommitted, missing an immutable target, date, or executable compatibility command
**When** readiness for this story is evaluated
**Then** the story remains backlog and cannot move to ready-for-dev
**And** no local path, package already present, or historical AppHost is inferred as the platform commitment.

**Evidence Manifest:**

| Field | Story 5.1 evidence |
| --- | --- |
| Requirements | FR21, FR23; NFR3, NFR4; UX: not applicable to this non-UI boundary outcome; AD-1, AD-15, AD-16, AD-17 |
| OwnedClauses | FR21.external-dependency-entry-gate; FR23.no-internal-contract-leak; FR23.package-consumer-compatibility; NFR3.clean-build-integrity; NFR4.actionable-ci-diagnostics; AD-1.domain-module-boundary; AD-15.public-surface-boundary; AD-16.platform-owned-host; AD-17.source-package-boundary-gates |
| Dependencies | EXT-HOST-1 at Committed or Available; no prior story |
| EvidenceLevel | Levels 1-3: structure, package-consumer behavior, and fail-closed deferred host seam |
| TestOrArtifact | ModuleBoundaryTests; PackageConsumerTests; CentralPackageAndSolutionFormatTests; clean-checkout CI transcript; produced package inventory |
| VerificationCommand | pwsh ./eng/verify-story-5.1.ps1 |
| NegativeEvidence | ForbiddenHostingOwnershipTests; ReleaseSourceReferenceLeakTests; NestedSubmoduleInitializationGuardTests |
| Result | Not run — backlog; EXT-HOST-1 is currently Uncommitted |

### Story 5.2: Configure hexa Through Live EventStore Operations

As an Agent Administrator,
I want to configure **hexa** through authorized live commands and queries,
So that the durable Agent state I see is replayable, current, and safe to automate.

**Primary Demonstrable Outcome:** An authorized administrator changes one Agent configuration and sees the same current state through public API/client and FrontComposer surfaces after EventStore replay.

**Dependencies:**

- **Prior stories:** 5.1.
- **External:** None beyond existing package/source dependencies; authorization fails closed through the current tenant-access seam.
- **Forward dependencies:** None; activation and integrated identity readiness are not claimed by this story.

**Acceptance Criteria:**

**Given** an authorized tenant administrator and a valid configure, response-mode, or lifecycle command
**When** the public client/API dispatches the command through the DomainService boundary
**Then** EventStore persists the resulting Agent events and replay reconstructs identical Agent state, configuration versions, safe change evidence, and future-only policy effect
**And** the command returns a structured accepted identity rather than stream, aggregate, workflow, or Provider SDK details.

**Given** Agent events have been accepted
**When** the Agent setup query and UI load
**Then** the authoritative Agent read model exposes current identity reference, display metadata, instructions version, lifecycle, response mode, approver-policy reference, configuration version, projection version, and freshness
**And** the FrontComposer view distinguishes submitted, authoritative pending, and projection-confirmed terminal state without treating lifecycle active as callable.

**Given** duplicate commands, stale expected revisions, invalid fields, or replayed projection deliveries
**When** focused tests execute
**Then** exact duplicates are idempotent, conflicts and validation failures are typed, prior state is not overwritten, and the persisted read-model end state remains deterministic
**And** no partial configuration or optimistic UI success is accepted as evidence.

**Given** an accepted Agent configuration or lifecycle write
**When** `ConfigureAgent`, `ActivateAgent`, or `DisableAgent` appends its event
**Then** `ConfigurationVersion` increments exactly once and the evolved `AgentActivated`/`AgentDisabled` schemas carry the new version
**And** legacy lifecycle events replay deterministically, exact duplicate commands do not increment, and a later interaction snapshots the updated version. This closes DW-4.

**Given** a caller from another tenant or without Agent-administration authority
**When** the caller commands or queries Agent configuration
**Then** authorization denies before mutation or disclosure and produces no Provider, Party, Conversation, or secret side effect
**And** response bodies, counts, diagnostics, accessible names, and audit summaries reveal no target-tenant existence or sensitive instructions.

**Evidence Manifest:**

| Field | Story 5.2 evidence |
| --- | --- |
| Requirements | FR1, FR3, FR6, FR19-FR25; NFR1-NFR5; UX-DR1-UX-DR3, UX-DR11-UX-DR17, UX-DR20, UX-DR23, UX-DR25, UX-DR30, UX-DR41, UX-DR50; AD-1-AD-5, AD-12, AD-15, AD-17 |
| OwnedClauses | FR1.live-configure-and-query; FR3.lifecycle-preserves-history; FR6.future-only-response-mode; FR19.agent-config-isolation; FR20.admin-authorization; FR23.structured-public-result; FR24.configuration-change-evidence; FR25.setup-status; NFR1.authorization-before-mutation; NFR3.replay-determinism; NFR4.current-status; NFR5.safe-change-audit; UX-DR20.lifecycle-not-callability; UX-DR50.command-truth-flow; AD-3.pure-aggregate; AD-4.lifecycle-configuration-version; AD-15.api-ui-parity; DW-4 |
| Dependencies | Story 5.1 |
| EvidenceLevel | Levels 2 and 4: aggregate/replay behavior plus live EventStore command-query-projection path |
| TestOrArtifact | AgentConfigurationAggregateTests; AgentLifecycleConfigurationVersionTests; AgentConfigurationEventStoreIntegrationTests; AgentSetupQueryTests; AgentConfigurationUiTests; persisted read-model fixture |
| VerificationCommand | pwsh ./eng/verify-story-5.2.ps1 |
| NegativeEvidence | AgentConfigurationAuthorizationTests.CrossTenantCommandIsDeniedBeforeMutation; AgentConfigurationAuthorizationTests.CrossTenantQueryDisclosesNothing; duplicate/conflict/replay cases |
| Result | Not run — backlog; requires Story 5.1 |

### Story 5.3: Govern Provider Models And Pricing Through Live Operations

As a Platform Operator,
I want one platform Provider/model catalog with explicit tenant enablement,
So that catalog truth is administered once while every tenant sees and selects only entries enabled for it.

**Primary Demonstrable Outcome:** Shipped tenant-scoped catalog state is migrated idempotently into the platform `ProviderCatalog` under reserved tenant `system` plus tenant-scoped `TenantProviderEnablement`, and public tenant queries expose only the enabled safe join.

**Dependencies:**

- **Prior stories:** 5.1.
- **External:** None. Catalog governance and migration do not invoke the Provider; runtime use remains gated by `EXT-PROVIDER-1` in consuming stories.
- **Forward dependencies:** None; this story publishes catalog truth but does not invoke the Provider.

**Acceptance Criteria:**

**Given** an AD-30 `Platform` principal and valid Provider/model capability and pricing metadata
**When** a create, update, enable, or disable command is accepted
**Then** the command targets reserved tenant `system` and one `ProviderCatalog` stream per (`ProviderId`, `ModelId`) and durably records label, platform enablement, text-generation capability, positive context/output/timeout limits, secret reference/configured state, versioned pricing units/currency, and a non-reusable monotonic CapabilityVersion
**And** tenant principals cannot mutate platform catalog state and replay and duplicate delivery produce the same state.

**Given** an authorized Platform Operator and a tenant
**When** a Provider/model entry is enabled or disabled for that tenant
**Then** `TenantProviderEnablement(TenantId)` records the system-catalog entry visibility independently of platform enablement
**And** tenant administrators cannot mutate enablement or infer any other tenant's enablement.

**Given** shipped tenant-scoped Provider catalog streams and read models
**When** the catalog migration runs
**Then** it writes deterministic platform-catalog and tenant-enablement targets carrying `MigratedFrom`, treats an exact repeat as a no-op, and reports a typed conflict for divergent duplicates
**And** legacy streams and projections are frozen for history rather than rewritten or deleted.

**Given** a Provider/model catalog query or Agent model selection
**When** the public client/API and Provider catalog UI consume the projection
**Then** tenant results join `provider-catalog` with `tenant-provider-enablement` and expose current safe capability, pricing, platform and tenant eligibility, configured-state, CapabilityVersion, projection version, and freshness without secret values or Provider SDK types
**And** absent and non-enabled entries return the identical not-found result while a platform-only blocker renders `PlatformNotReady`.

**Given** secret-backed configuration
**When** commands, events, projections, responses, logs, traces, audit summaries, browser output, or accessible names are inspected
**Then** only the secret reference and configured/not-configured state are present
**And** poison secret values are absent from every captured artifact.

**Given** unauthorized, wrong-principal-kind, cross-tenant Agent-selection, stale-revision, regressed-version, or invalid-pricing input
**When** the operation executes
**Then** it fails before durable mutation or secret resolution with a typed support-safe outcome
**And** existing Agents and historical evidence are not rewritten.

**Evidence Manifest:**

| Field | Story 5.3 evidence |
| --- | --- |
| Requirements | FR4, FR5, FR19-FR25, FR28; NFR1, NFR4, NFR6, NFR10; UX-DR4, UX-DR11-UX-DR17, UX-DR21, UX-DR26, UX-DR30, UX-DR41; AD-2, AD-9, AD-10, AD-14, AD-15, AD-17, AD-21 |
| OwnedClauses | FR4.live-provider-catalog; FR4.disabled-not-usable; FR5.future-only-selection; FR21.missing-provider-blocks; FR24.provider-change-evidence; NFR6.secret-nondisclosure; NFR10.current-pricing-input; UX-DR4.safe-provider-grid; UX-DR21.provider-result-not-inferred; AD-2.platform-catalog-and-tenant-enablement; AD-9.adapter-boundary; AD-10.capability-version-and-limits; AD-14.secret-safety; AD-30.platform-principal |
| Dependencies | Story 5.1; no external dependency |
| EvidenceLevel | Levels 2 and 4: catalog aggregate behavior and live EventStore/query/UI component path |
| TestOrArtifact | ProviderCatalogMigrationTests; ProviderCatalogAggregateTests; TenantProviderEnablementAggregateTests; ProviderCatalogEventStoreIntegrationTests; ProviderCatalogQueryTests; ProviderCatalogUiTests; secret poison-sweep report |
| VerificationCommand | pwsh ./eng/verify-story-5.3.ps1 |
| NegativeEvidence | ProviderCatalogAuthorizationTests.CrossTenantSelectionIsDenied; ProviderCatalogAuthorizationTests.TenantPrincipalCannotMutatePlatformCatalogOrEnablement; ProviderCatalogVersionRegressionTests; ProviderCatalogMigrationConflictTests; ProviderSecretLeakTests; invalid pricing/limit cases |
| Result | Reopened — backlog; shipped tenant-scoped implementation is migration source and historical evidence, not final conformance |

### Story 5.4: Prove Trusted Principal Tenant Party And Approver Readiness

As a Tenant Security Operator,
I want current tenant, Party, and Approver evidence bound into setup readiness,
So that revoked, ambiguous, stale, or cross-tenant authority can never make **hexa** ready.

**Primary Demonstrable Outcome:** A live readiness query proves one valid tenant-scoped Agent Party identity and resolvable approver bases, then immediately becomes blocked on revocation or uncertainty.

**Dependencies:**

- **Prior stories:** 5.1 and 5.2.
- **External:** Existing Tenants, Parties, and Conversations read contracts; no EXT-CONV-AI-1 membership mutation is used here.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** duplicated, out-of-order, gapped, replayed, or revoking Tenants events
**When** the local tenant-access projection applies them
**Then** duplicate handling is idempotent, ordering/gap/freshness is explicit, and unknown, stale, disabled, unavailable, non-member, insufficient-role, or revoked state blocks readiness
**And** no downstream Provider, membership, posting, export, or deletion side effect is attempted.

**Given** an Agent Party link
**When** the Parties adapter resolves it
**Then** exactly one stable tenant-scoped active Party reference with current posting eligibility is accepted and no Party PII is persisted
**And** missing, disabled, ambiguous, unavailable, unauthorized, or multiple identity state blocks setup with a typed safe reason.

**Given** caller, predefined Party, tenant-role, or Conversation Facilitator approver sources
**When** an Approver Policy basis is resolved
**Then** the exact current basis and disclosure category are recorded, API/UI report the same safe basis, and dependency loss revokes readiness
**And** JWT-only, UI-only, or historical role evidence cannot grant authority.

**Given** any public Agents operation
**When** trusted ingress derives authorization context
**Then** it selects exactly one AD-30 principal kind from `User`, `Administrator`, `Platform`, or `Workflow` according to the operation family, resolves a user `PartyId` from the authenticated subject plus fresh Parties/Tenants evidence, strips all client-supplied reserved extension keys, and issues only allowlisted scope-bound extensions with an HMAC tag
**And** the command pipeline verifies the tag before aggregate dispatch and rejects an untagged, forged, wrong-family, wrong-principal-kind, stale-role, or wrong-scope context. This closes DW-2.

**Given** an authorization or trusted-context denial
**When** the pipeline rejects the operation
**Then** a content-free record is appended to `SecurityEventLog(TenantId, UTC day)` with safe principal, family, scope, reason, and correlation evidence
**And** no prompt, context, generated content, secret, Party PII, or target-tenant existence signal is recorded.

**Given** a caller from tenant B targets tenant A Agent, Party, policy, status, or audit data
**When** each focused public and application path is exercised
**Then** every path denies before lookup-dependent disclosure or side effect, including when identifiers collide
**And** counts, empty states, error classes, timing-safe messages, logs, and accessible output disclose no tenant A record.

**Evidence Manifest:**

| Field | Story 5.4 evidence |
| --- | --- |
| Requirements | FR2, FR7, FR19-FR21, FR24, FR25; NFR1, NFR2; UX-DR5, UX-DR20, UX-DR25, UX-DR30; AD-4, AD-7, AD-8, AD-12, AD-17, AD-30 |
| OwnedClauses | FR2.exactly-one-party; FR7.current-approver-basis; FR19.cross-tenant-denial; FR20.policy-authorization; FR21.stale-ambiguous-unavailable-block; NFR1.pre-side-effect-authorization; NFR2.no-cross-tenant-disclosure; UX-DR5.blocked-policy-source; AD-7.party-reference-only; AD-8.approver-resolution; AD-12.current-fail-closed-gates; AD-30.principal-kind-and-ingress-HMAC; AD-2.SecurityEventLog; DW-2 |
| Dependencies | Stories 5.1 and 5.2; existing Tenants, Parties, and Conversations read contracts |
| EvidenceLevel | Levels 2 and 4: projection/unit behavior and live dependency-backed authorization/identity reads |
| TestOrArtifact | TrustedPrincipalIngressTests; ReservedExtensionHmacVerificationTests; SecurityEventLogAggregateTests; TenantAccessProjectionTests; PartyReadinessIntegrationTests; ApproverPolicyResolutionIntegrationTests; revocation evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-5.4.ps1 |
| NegativeEvidence | TrustedPrincipalIngressTests.ClientReservedKeysAreStripped; ReservedExtensionHmacVerificationTests.ForgedUntaggedWrongFamilyAndWrongScopeAreDenied; TenantPartyApproverIsolationTests.CrossTenantIdentifiersAreDeniedBeforeSideEffects; TenantAccessRevocationTests.RevocationImmediatelyBlocksReadiness; gap/stale/ambiguous/PII poison cases |
| Result | Not run — backlog; requires Stories 5.1 and 5.2 |

### Story 5.5: Publish Authoritative Readiness And Provider-State Contracts

As an Agent Administrator,
I want one authoritative readiness result shared by API and UI,
So that callability, Provider degradation, evidence freshness, and blockers cannot be inferred differently by each surface.

**Primary Demonstrable Outcome:** The same tenant and registry checkpoint returns the same versioned setup/callability result through projection, API/client, and UI, including a valid callable-degraded Provider case and fail-closed stale cases.

**Dependencies:**

- **Prior stories:** 5.2, 5.3, and 5.4.
- **External:** EXT-PROVIDER-1 must be at least Committed.
- **Forward dependencies:** None; later stories add evidence records but do not define alternative readiness semantics.

**Acceptance Criteria:**

**Given** a server-trusted readiness observation
**When** LaunchReadinessGate accepts it at the expected EventStore revision
**Then** `LaunchReadinessGate` records the complete normative schema, AD-29 `ObservationId`, logical GateId/TenantScope/EnvironmentProfile key, exact `ScopeKind`, `AuthorizedProducer`, and RegistryRevision
**And** the launch-readiness projection selects greatest committed revision, rejects conflicting duplicate identities, and never falls back to an older Pass.

**Given** Provider readiness is calculated
**When** current capability, pricing, secret-configured state, health, limits, adapter commitment, ObservedAt, and ValidUntil are evaluated
**Then** only Ready/Callable/None, Degraded/Callable/NonBlockingOperationalWarning, or Blocked/Blocked/defined-blocker is accepted
**And** missing, stale, unconfigured, unpriced, invalid-limit, unavailable, failed, regressed, unknown, or indeterminate state is Blocked.

**Given** an operation-family readiness request
**When** API, BFF, UI, workflow, or readiness projection evaluates OperationGateMatrixVersion 2
**Then** all use the same authoritative GateIds, one consistent registry checkpoint, matrix version, applicable records, and safe blockers
**And** a missing family, old or unknown matrix version, invalid `ScopeKind`, unauthorized producer, missing record, non-inventory gate, or checkpoint change blocks or retries without local fallback.

**Given** public Agents routes or legacy Agent-owned launch-readiness state
**When** the versioned public contract and readiness migration are applied
**Then** authoritative routes live under `/api/v1/agents/...` and old unversioned routes are explicitly compatibility-handled rather than authoritative
**And** legacy `RecordAgentLaunchReadiness` state migrates idempotently to `LaunchReadinessGate`, retains history, and can no longer write readiness on `Agent`.

**Given** tenant Provider readiness is evaluated
**When** the current catalog and enablement projections are read
**Then** system-scoped `ProviderCatalog` state is joined with that tenant's `TenantProviderEnablement`
**And** platform and tenant blockers are evaluated at their declared matrix-v2 `ScopeKind` without leaking another tenant's enablement.

**Given** a tenant-scoped readiness or Provider-state query
**When** an unauthorized or cross-tenant caller requests it
**Then** no record, blocker detail, count, Provider identity, or existence signal crosses the boundary
**And** the denial is recorded with a support-safe reference.

**Evidence Manifest:**

| Field | Story 5.5 evidence |
| --- | --- |
| Requirements | FR4, FR5, FR19-FR21, FR23, FR25, FR28; NFR1, NFR2, NFR4, NFR6, NFR12; UX-DR2, UX-DR20, UX-DR21, UX-DR25, UX-DR26, UX-DR45; AD-2, AD-10, AD-12, AD-15, AD-17, AD-24, AD-29, AD-31 |
| OwnedClauses | FR25.authoritative-callability-status; FR28.machine-readable-gates; NFR4.safe-actionable-blockers; NFR12.profile-visible-in-registry; UX-DR20.current-callability-only; UX-DR21.valid-provider-triples; UX-DR45.pass-block-insufficient-stale; AD-2.LaunchReadinessGate; AD-10.provider-readiness-contract; AD-17.registry-revision-supersession-and-matrix-v2; AD-29.observation-identity; AD-31.api-v1-route |
| Dependencies | Stories 5.2-5.4; EXT-PROVIDER-1 at Committed or Available |
| EvidenceLevel | Levels 1, 2, and 4: public contract, deterministic aggregate/projection logic, and live EventStore/API/UI path |
| TestOrArtifact | LaunchReadinessGateAggregateTests; LaunchReadinessMigrationTests; LaunchReadinessProjectionTests; ProviderReadinessContractTests; ApiV1AgentsRouteTests; OperationGateMatrixV2ParityTests; readiness UI contract snapshot |
| VerificationCommand | pwsh ./eng/verify-story-5.5.ps1 |
| NegativeEvidence | ReadinessIsolationTests.CrossTenantRegistryQueryDisclosesNothing; StaleNewestObservationDoesNotFallBackTests; UnknownProviderReasonAndOldMatrixVersionBlockTests; UnauthorizedProducerAndWrongScopeKindTests |
| Result | Not run — backlog; EXT-PROVIDER-1 is currently Uncommitted |

### Story 5.6: Compose Agents In The Platform-Owned Production-Like Host

As a Platform Maintainer,
I want Agents composed in the committed platform-owned production-like host,
So that the domain service and UI run with their real platform dependencies without restoring forbidden module ownership.

**Primary Demonstrable Outcome:** The versioned production-like fixture starts the platform-owned topology, exposes healthy Agents service/UI endpoints, and captures safe correlated evidence while the Agents repository contains no hosting projects.

**Dependencies:**

- **Prior stories:** 5.1 and 5.5.
- **External:** EXT-HOST-1, EXT-SECRETS-1, and EXT-TOPOLOGY-1 must be Available and their exact compatibility commands must pass before live execution.
- **Forward dependencies:** None; unavailable Provider, tokenizer, or safety execution remains blocked through readiness and is not simulated as available.

**Acceptance Criteria:**

**Given** the exact Available EXT-HOST-1 and EXT-TOPOLOGY-1 targets
**When** the fixture resets, seeds, and starts
**Then** the platform host composes Agents DomainService and UI with EventStore, Conversations, Parties, Tenants, Dapr Workflow, readiness registry, shared capacity seam, telemetry, health, identity, evidence ingress, and fail-closed Provider/safety ports
**And** component versions, endpoints, app IDs, Dapr resources, reset procedure, and capture procedure are recorded immutably.

**Given** platform secret resolution through the exact Available EXT-SECRETS-1 target
**When** configured, denied, missing, and rotated-secret cases run
**Then** only secret references and configured-state cross Agents boundaries, rotation recovery is observable, and denied/missing state blocks the relevant operation
**And** poison values are absent from events, projections, HTTP bodies, UI, logs, traces, metrics, and evidence.

**Given** platform access-control, tenant identity, and evidence-ingress routes
**When** authorized and cross-tenant requests execute
**Then** only declared app IDs, methods, topics, tenant scopes, and authenticated qualification sessions are accepted
**And** cross-tenant or unauthenticated evidence, DomainService, UI, and sidecar routes fail before disclosure or mutation.

**Given** a clean checkout after the platform fixture completes
**When** repository and package contents are inspected
**Then** no module-owned AppHost, Aspire, ServiceDefaults, hidden manual step, conditional skip, nested submodule, local absolute path, `src/Hexalith.Agents.Server/Aggregates`, or `src/Hexalith.Agents.Server/Application/Tools` is present or required
**And** failure to start any required resource makes LR-TOPOLOGY InsufficientEvidence or Block rather than a passing story result.

**Given** the corrected Structural Seed and Live-Seam Matrix
**When** repository conformance and test execution run
**Then** `global.json`, the workspace package catalog, xUnit v3/Microsoft Testing Platform v2 `test.runner`, Shouldly, and NSubstitute align; `test/Hexalith.Agents.IntegrationTests` exists in the `.slnx`; and the Live Story 5.2/5.3 dispatch, query, and projection seams assert persisted EventStore/state-store/read-model end state
**And** every live claim names and passes its registered integration test in the same change.

**Evidence Manifest:**

| Field | Story 5.6 evidence |
| --- | --- |
| Requirements | FR19-FR23, FR25, FR28; NFR1, NFR2, NFR4, NFR6, NFR11; UX-DR1, UX-DR12, UX-DR41; AD-1, AD-14, AD-16, AD-17, AD-18, AD-31 |
| OwnedClauses | FR21.platform-dependency-uncertainty; FR23.platform-host-public-boundary; FR25.topology-status; NFR6.secret-no-leak; NFR11.production-like-recovery-fixture-prerequisite; UX-DR41.FrontComposer-platform-composition; AD-16.platform-host-composition; AD-17.EXT-TOPOLOGY-fixture; AD-18.dapr-workflow-runtime-presence |
| Dependencies | Stories 5.1 and 5.5; EXT-HOST-1, EXT-SECRETS-1, EXT-TOPOLOGY-1 Available |
| EvidenceLevel | Levels 4 and 5: live component composition and reproducible production-like topology |
| TestOrArtifact | StructuralSeedConformanceTests; Hexalith.Agents.IntegrationTests; LiveSeamMatrixParityTests; PlatformHostTopologyTests; DaprAccessControlRouteTests; PlatformSecretResolutionTests; topology version/reset/seed/capture manifest; LR-TOPOLOGY observation |
| VerificationCommand | pwsh ./eng/verify-story-5.6.ps1 |
| NegativeEvidence | PlatformHostIsolationTests.CrossTenantRoutesAndEvidenceAreDenied; SecretPoisonSweepTests; ForbiddenModuleHostAndConditionalSkipTests; unavailable-resource fixture |
| Result | Not run — backlog; EXT-HOST-1, EXT-SECRETS-1, and EXT-TOPOLOGY-1 are currently Uncommitted |

### Story 5.7: Activate hexa Only When Setup Gates Pass

As an Agent Administrator,
I want activation and callability decided from current authoritative setup gates,
So that **hexa** can be active only under explicit policy and can never be presented as callable on stale or insufficient evidence.

**Primary Demonstrable Outcome:** The activation operation renders one authoritative result: a complete synthetic all-pass fixture can activate and prove callability, while every missing/stale/blocked fixture remains non-callable and the real register remains honestly blocked.

**Dependencies:**

- **Prior stories:** 5.2 through 5.6.
- **External:** Every external seam evaluated by AgentActivation must meet the current register status required for the chosen execution profile; controlled live execution requires Available, while deterministic decision fixtures execute no external seam.
- **Forward dependencies:** None; future runtime stories add qualifying evidence but do not change the activation decision contract.

**Acceptance Criteria:**

**Given** an authorized activation request
**When** AgentActivation evaluates OperationGateMatrixVersion 2 at one RegistryRevision
**Then** it checks the complete applicable setup gate set using exact `ScopeKind` and producer-valid observations and records lifecycle decision, callability, matrix version, registry revision, current safe blockers, and authoritative pending/terminal projection references
**And** lifecycle active and callability remain separate public/UI states.

**Given** any required record or dependency is missing, stale, Block, InsufficientEvidence, uncommitted, unavailable for executed work, unknown, or changes the registry checkpoint
**When** activation or callability is evaluated
**Then** activation is rejected or lifecycle remains visibly active-but-not-callable according to the command contract, no Provider side effect occurs, and Success is forbidden
**And** the API/UI identifies each safe recovery owner without leaking sensitive state.

**Given** a deterministic complete current all-pass fixture with no external execution
**When** activation decision logic is tested
**Then** exactly one accepted activation transition and callable result are produced at the expected revision
**And** the fixture proves calculation only and cannot create an RQ-1 Pass, production enablement, or live Evidence Level claim.

**Given** current tenant role, Party identity, Provider readiness, pricing, safety, secret, cost, capacity, or evidence state is revoked or superseded
**When** the next callability inspection or side-effect gate runs
**Then** callability is removed before Provider invocation and the newest restrictive record wins
**And** API, UI, keyboard, live-region, English/French, and restrictive-viewport states remain aligned.

**Given** a tenant B caller attempts to activate or inspect tenant A Agent
**When** the focused activation and readiness paths execute
**Then** each denies before mutation or disclosure and produces no side effect
**And** no blocker detail, gate existence, Agent state, count, timing text, or accessible output reveals tenant A data.

**Evidence Manifest:**

| Field | Story 5.7 evidence |
| --- | --- |
| Requirements | FR1, FR3, FR19-FR23, FR25, FR28; NFR1, NFR4, NFR7, NFR10, NFR12, NFR13; UX-DR2, UX-DR3, UX-DR20, UX-DR23, UX-DR25, UX-DR33, UX-DR36, UX-DR40, UX-DR41, UX-DR45, UX-DR50; AD-10, AD-12, AD-15, AD-17, AD-20, AD-21, AD-24, AD-25 |
| OwnedClauses | FR1.activation-blockers; FR3.lifecycle-not-callability; FR21.current-dependency-fail-closed; FR25.callability-and-blockers; FR28.RQ-1-separation; NFR1.activation-authorization; NFR12.capacity-gate-visible; NFR13.accessible-localized-restrictive-activation; UX-DR20.authoritative-success-only; UX-DR50.pending-to-terminal-truth; AD-17.consistent-checkpoint; AD-17.release-gate-separation |
| Dependencies | Stories 5.2-5.6; profile-specific external status from the authoritative register |
| EvidenceLevel | Levels 2 and 4 for decision and live setup surfaces; deterministic fixtures never claim live attainment |
| TestOrArtifact | AgentActivationDecisionTests; AgentActivationEventStoreIntegrationTests; OperationGateMatrixV2ActivationTests; CallabilityRevocationTests; AgentActivationUiTests; fixture-vs-live evidence classification manifest |
| VerificationCommand | pwsh ./eng/verify-story-5.7.ps1 |
| NegativeEvidence | AgentActivationIsolationTests.CrossTenantActivationAndInspectionAreDenied; MissingStaleInsufficientOldMatrixWrongScopeAndProducerCases; SyntheticFixtureCannotSetRq1ReadyTests |
| Result | Not run — backlog; prior stories and external commitments are incomplete |

### Story 5.8: Protect Sensitive Agent Content At The EventStore Boundary

As a Security Engineer,
I want Agent content protected before it enters durable or execution infrastructure,
So that erasure is enforceable without breaking EventStore replay.

**Primary Demonstrable Outcome:** A content-bearing interaction is sealed at the EventStore boundary, stays sealed through transport and projections, carries references only in workflow state, and replays as typed `Erased` after irreversible key destruction.

**Dependencies:**

- **Prior stories:** 5.1 and 5.6 for the package boundary, integration tier, and production-like host.
- **External:** `EXT-PROTECTION-1` and `EXT-SECRETS-1` must both be `Available` with their exact compatibility commands passing before live content-bearing execution.
- **Forward dependencies:** Content-bearing Stories 6.1–6.4, 7.1–7.4, 8.1–8.3, and 8.8 consume this foundation.

**Acceptance Criteria:**

**Given** sensitive prompt, context, generated, edited, failed, or audit content
**When** the orchestrator prepares an EventStore write
**Then** only sensitive fields are sealed in `ProtectedContent` with a per-`AgentInteraction` DEK wrapped by the tenant KEK while all non-sensitive event fields remain plaintext
**And** a different tenant's keys, envelope metadata, or digests cannot unprotect or correlate the content.

**Given** a sealed envelope
**When** it crosses pub/sub, read models, adapters, caches, or Dapr Workflow orchestration
**Then** the envelope remains sealed outside an authorized unprotect boundary and workflow history contains references only
**And** the shipped no-op protection service blocks live content-bearing work and cannot satisfy evidence.

**Given** an unpinned interaction DEK and an authorized deletion
**When** `DestroyDek` returns its irreversible receipt
**Then** replay materializes typed `Erased`, snapshots and restore caches cannot revive plaintext, and the receipt is retained as support-safe evidence
**And** an active Legal Hold pin rejects destruction before any partial erasure.

**Given** the AD-27 execution-state content sweep and live integration fixture
**When** protection conformance executes
**Then** protection, unprotection, sealed transport, hold pins, deletion, erased replay, restore, tenant isolation, and every workflow state shape are covered
**And** poison plaintext is absent from EventStore-visible non-envelope fields, state store, broker, projections, responses, logs, traces, metrics, and browser evidence.

**Evidence Manifest:**

| Field | Story 5.8 evidence |
| --- | --- |
| Requirements | FR10, FR14, FR19-FR21, FR24, FR28; NFR1-NFR7, NFR11; AD-14, AD-17, AD-22, AD-27; EXT-PROTECTION-1; EXT-SECRETS-1 |
| OwnedClauses | AD-22.field-level-protected-content; AD-22.per-interaction-DEK-and-tenant-KEK; AD-22.typed-erased-replay; AD-27.workflow-reference-only-state; NFR2.tenant-protected-content-isolation; NFR3.replay-after-erasure; NFR6.no-secret-or-content-leak |
| Dependencies | Stories 5.1 and 5.6; EXT-PROTECTION-1 and EXT-SECRETS-1 Available |
| EvidenceLevel | Levels 4 and 5: live EventStore protection plus production-like transport, restore, and workflow-state evidence |
| TestOrArtifact | PayloadProtectionLiveTests; ProtectedContentReplayTests; HoldPinProtectionTests; WorkflowContentSweepTests; protection evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-5.8.ps1 |
| NegativeEvidence | PayloadProtectionIsolationTests.CrossTenantKeysCannotUnprotectOrCorrelate; NoOpProtectionCannotSatisfyLiveEvidenceTests; PlaintextPoisonSweepTests; HeldDekCannotBeDestroyedTests |
| Result | Blocked — backlog; EXT-PROTECTION-1 and EXT-SECRETS-1 are Uncommitted |

## Epic 6: One Safe Automatic Conversation Response

A Conversation Participant can use the sole **Call hexa** action and receive exactly one safe attributed response, or a precise fail-closed outcome before any unsafe Provider or Conversation effect.

**Status:** active forward backlog.

**Story count:** 7.

**Dependency topology:** 6.1 establishes durable execution and recovery with deterministic activities; 6.2 produces full-context-or-blocked input; 6.3 adds two-stage safety; 6.4 proves prepared Provider attempt and cost behavior through a trusted admission contract; 6.5 supplies the shared capacity allocator and live admission; 6.6 posts once through Conversations; 6.7 exposes the complete participant outcome. No story requires a later story to satisfy its own demonstrable outcome.

### Story 6.1: Start And Recover An Automatic Interaction

As a Runtime Operator,
I want each accepted automatic interaction owned durably by one Dapr Workflow,
So that restart and replay preserve EventStore truth without duplicating execution state.

**Primary Demonstrable Outcome:** One accepted automatic interaction survives injected restarts at each current workflow checkpoint and reaches the same persisted safe outcome with no duplicate interaction, command, timer, or version.

**Dependencies:**

- **Prior stories:** 5.5 through 5.8.
- **External:** EXT-PROTECTION-1 and EXT-TOPOLOGY-1 must be Available for live content-bearing and production-like restart evidence; deterministic no-content component tests may run without executing an unavailable external seam.
- **Forward dependencies:** None; deterministic context, safety, generation, and posting activities prove orchestration before their live adapters are introduced.

**Acceptance Criteria:**

**Given** an authorized automatic Agent Call accepted at an expected EventStore revision
**When** the platform starts the deterministic interaction workflow
**Then** Dapr Workflow is the sole durable execution owner, EventStore records the request/snapshot as business truth, and workflow identity is derived deterministically from AgentInteractionId
**And** Microsoft Agent Framework workflows, Python DurableAgent, MCP, A2A, in-memory workers, and alternate durable owners are absent.

**Given** deterministic activity outcomes for context, safety, generation, and posting
**When** an activity succeeds, blocks, fails, times out, or is replayed
**Then** each durable step dispatches at most one server-trusted command, records its command/effect identity, and resumes from EventStore state rather than treating workflow history as proposal or interaction truth
**And** exact replay creates no duplicate interaction, attempt placeholder, proposal version, timer, or posting outcome.

**Given** any Dapr Workflow input, output, custom status, timer payload, retry state, or activity contract
**When** the AD-27 execution-state content sweep runs
**Then** workflow history contains protected EventStore references and safe enums/identities only
**And** prompt, context, generated, edited, failed, or audit plaintext is rejected before workflow persistence.

**Given** faults injected before and after each current command checkpoint
**When** the platform restarts
**Then** the frozen eligible interaction cohort is fully accounted for, accepted EventStore state has RPO 0, current projections catch up, and processing is restored within 15 monotonic minutes for the exercised path
**And** pre-fault terminal decisions remain unchanged and mixed clock origins cannot qualify.

**Given** an unauthorized or tenant-mismatched call
**When** call acceptance is evaluated
**Then** denial occurs before EventStore interaction creation and workflow start
**And** no workflow identity, timing distinction, status record, or downstream dependency call reveals the target tenant.

**Evidence Manifest:**

| Field | Story 6.1 evidence |
| --- | --- |
| Requirements | FR8, FR10, FR19-FR21, FR24, FR25, FR28; NFR1-NFR5, NFR11; UX-DR27, UX-DR48, UX-DR50; AD-3, AD-4, AD-13, AD-17, AD-18, AD-22, AD-23, AD-27; EXT-PROTECTION-1 |
| OwnedClauses | FR8.one-explicit-accepted-interaction; FR10.safe-workflow-failure; FR24.workflow-linked-audit; NFR3.no-duplicate-business-state; NFR11.RPO-zero-and-restart; UX-DR27.authoritative-interaction-states; UX-DR48.failure-is-not-proposal; AD-18.single-durable-owner; AD-23.frozen-cohort-monotonic-recovery |
| Dependencies | Stories 5.5-5.8; EXT-PROTECTION-1 and EXT-TOPOLOGY-1 Available for live content-bearing Level 4/5 evidence |
| EvidenceLevel | Levels 2, 4, and 5: replay-safe logic, live Dapr Workflow component, production-like failure injection |
| TestOrArtifact | AutomaticInteractionWorkflowTests; AutomaticInteractionRestartIntegrationTests; RecoveryCohortManifest; LR-RECOVERY partial observation for workflow-owned identities |
| VerificationCommand | pwsh ./eng/verify-story-6.1.ps1 |
| NegativeEvidence | AutomaticInteractionIsolationTests.CrossTenantCallStartsNoWorkflow; AlternateDurableOwnerGuardTests; DuplicateCommandTimerAndVersionRecoveryTests |
| Result | Not run — backlog; EXT-TOPOLOGY-1 is currently Uncommitted |

### Story 6.2: Use The Complete Authorized Conversation Or Block

As a Conversation Participant,
I want **hexa** to use the complete authorized Source Conversation or not run,
So that no partial or unauthorized context is represented as a complete answer.

**Primary Demonstrable Outcome:** A fresh authorized Conversation fixture below the exact model budget produces ContextReady, while oversized, partial, stale, unsupported-tokenizer, and cross-tenant fixtures produce ContextBlocked before Provider work.

**Dependencies:**

- **Prior stories:** 5.8, 6.1, and Epic 5 Provider/readiness contracts.
- **External:** EXT-PROTECTION-1 and EXT-TOKEN-1 must be Available before protected content or its tokenizer is executed; the supported Conversations read seam must be live.
- **Forward dependencies:** None; the story ends at a protected exact prepared-input/context result and performs no Provider call.

**Acceptance Criteria:**

**Given** an accepted interaction and current tenant/Conversation authorization
**When** the context activity loads the Source Conversation
**Then** it reads fresh authorized detail and the complete visible timeline through supported Conversations contracts, excludes every V1 non-goal, and records policy/freshness/source references without raw content in safe status
**And** missing, partial, stale, ambiguous, unavailable, unauthorized, or cross-tenant content becomes ContextBlocked.

**Given** the exact Provider/model tokenizer and current provider capability high-water mark
**When** the canonical prompt plus complete context and reserved output are measured
**Then** the current live capability entry must be fresh, enabled, configured, priced, text-generation capable, non-regressed, and have valid positive limits before ContextReady
**And** missing/unsupported tokenization, budget overflow, or any readiness failure blocks with no truncation, summarization, windowing, sampling, substitute retrieval, or Provider call.

**Given** ContextReady was recorded and Conversation content, authorization, tokenizer, or Provider capability later changes
**When** a caller requests provider-input revalidation
**Then** the same fresh complete read, exact measurement, high-water/readiness check, and full budget calculation execute again
**And** the result either produces a new trusted current input reference or blocks; prior context is never silently reused.

**Given** context authorization is denied
**When** public status and audit are queried
**Then** authorized viewers receive only a safe class, policy/version, timing reference, and no-content evidence
**And** unauthorized viewers cannot infer Conversation existence, size, token count, participants, or target tenant.

**Evidence Manifest:**

| Field | Story 6.2 evidence |
| --- | --- |
| Requirements | FR8-FR10, FR19-FR21, FR24, FR25, FR28; NFR1, NFR2, NFR4, NFR8, NFR9; UX-DR24, UX-DR27, UX-DR42; AD-6, AD-10-AD-14, AD-18 |
| OwnedClauses | FR9.complete-authorized-context; FR9.no-bounded-fallback; FR9.fail-before-provider; FR10.invalid-context-safe-failure; NFR8.complete-or-blocked; NFR9.fast-pre-provider-context-rejection-source; UX-DR42.read-only-full-or-blocked; AD-10.capability-high-water; AD-11.exact-tokenizer-and-revalidation |
| Dependencies | Stories 5.8 and 6.1; EXT-PROTECTION-1 and EXT-TOKEN-1 Available; supported Conversations read contract |
| EvidenceLevel | Levels 2 and 4: deterministic budget behavior and live Conversations/tokenizer integration |
| TestOrArtifact | CompleteConversationContextTests; ConversationContextIntegrationTests; ProviderTokenizerCompatibilityTests; ContextRevalidationTests; no-content evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-6.2.ps1 |
| NegativeEvidence | ConversationContextIsolationTests.CrossTenantContextIsDeniedBeforeReadDisclosure; NoTruncationSummaryWindowRetrievalTests; UnsupportedTokenizerAndStaleCapabilityTests |
| Result | Not run — backlog; EXT-TOKEN-1 is currently Uncommitted |

### Story 6.3: Block Unsafe Prompt Context Or Output

As a Security Operator,
I want fresh versioned safety decisions before Provider and Conversation side effects,
So that unsafe or unauthorized content cannot enter generation, proposal, or posting paths.

**Primary Demonstrable Outcome:** Versioned safety fixtures prove prompt/context denial prevents Provider work and output denial creates only a non-approvable failure record with no proposal or message.

**Dependencies:**

- **Prior stories:** 5.8, 6.1, and 6.2.
- **External:** EXT-PROTECTION-1 and EXT-SAFETY-1 must be Available before protected content or live safety execution.
- **Forward dependencies:** None; deterministic generated-output fixtures exercise the second stage without requiring the later Provider story.

**Acceptance Criteria:**

**Given** a prompt plus complete authorized Conversation Context
**When** the live pre-Provider safety adapter evaluates the active policy
**Then** a fresh versioned Allow is required before progression, while missing, stale, unversioned, indeterminate, always-blocked, unauthorized-data, or control-bypass results stop before Provider work
**And** audit/status retain only policy/version, safe reason, timing, and no-content evidence permitted by policy.

**Given** deterministic complete generated output
**When** the live output-safety adapter evaluates it
**Then** a fresh versioned Allow is required before any proposal or Conversation side effect
**And** a Block creates no Proposed Agent Reply, proposal version, queue entry, notification, editor, approval action, or Conversation Message.

**Given** restricted hate, harassment, sexual, violent, illegal-activity, or sensitive-personal content
**When** policy evaluates the tenant use case and response mode
**Then** it may proceed only under an explicitly permitted tenant use case and Confirmation Response Mode
**And** Automatic mode and absent/ambiguous permission block.

**Given** a retry after a safety decision
**When** the active policy changes
**Then** the retry uses a policy at least as restrictive as its initial attempt, an Approver cannot override a Block, and a failed attempt is never resurrected
**And** every always-blocked and restricted category is covered without storing prohibited raw content in diagnostics or evidence.

**Evidence Manifest:**

| Field | Story 6.3 evidence |
| --- | --- |
| Requirements | FR10, FR12, FR19-FR21, FR24-FR28; NFR1, NFR2, NFR4, NFR7; UX-DR27, UX-DR36, UX-DR43, UX-DR48, UX-DR50; AD-12, AD-14, AD-17, AD-20 |
| OwnedClauses | FR26.versioned-active-safety-policy; FR27.prompt-context-before-provider; FR27.output-before-side-effect; FR27.no-approver-override; NFR7.active-safety-before-side-effects; UX-DR43.no-weaker-retry; UX-DR48.separate-failure-record; AD-20.always-blocked-restricted-two-stage |
| Dependencies | Stories 5.8 and 6.1-6.2; EXT-PROTECTION-1 and EXT-SAFETY-1 Available |
| EvidenceLevel | Levels 2 and 4: policy matrix behavior and live safety-adapter compatibility |
| TestOrArtifact | TwoStageSafetyPolicyTests; ContentSafetyAdapterIntegrationTests; NoWeakerRetryTests; GenerationFailureRecordTests; content-safe evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-6.3.ps1 |
| NegativeEvidence | SafetyTenantIsolationTests.CrossTenantAndUnauthorizedDataAreAlwaysBlocked; ApproverOverrideDoesNotExistTests; AlwaysBlockedRestrictedCategoryMatrixTests; raw-content poison sweep |
| Result | Not run — backlog; EXT-SAFETY-1 is currently Uncommitted |

### Story 6.4: Generate Within Hard Cost Reservations

As a Tenant Budget Owner,
I want each prepared Provider attempt protected by an atomic hard-cost reservation,
So that retries and crashes cannot overspend or duplicate Provider work.

**Primary Demonstrable Outcome:** One exact prepared attempt invoked under an injected trusted admission grant reserves maximum cost, calls the committed adapter idempotently, reconciles actual usage, and recovers its outcome without double charge or duplicate generation.

**Dependencies:**

- **Prior stories:** 5.8, 6.1 through 6.3, and Epic 5 Provider/readiness contracts.
- **External:** EXT-PROTECTION-1, EXT-PROVIDER-1, and EXT-SECRETS-1 must be Available before live invocation.
- **Forward dependencies:** None; an injected trusted admission/fence contract proves this story. Production call acceptance cannot mint a live grant until Story 6.5 supplies the shared allocator.

**Acceptance Criteria:**

**Given** fresh context/safety/readiness and current pricing, monthly budget, per-call cap, and Provider limits
**When** the interaction prepares an attempt
**Then** the aggregate assigns the next `AttemptOrdinal`, the shared `AgentsIdentity` canonicalizer derives `AttemptId`, `ReservationId`, `AdmissionId`, and `QueueId` exactly per AD-29, and one deterministic descriptor binds them with ProviderId, ModelId, EffectiveProviderCapabilityVersion, limits, timeout, policy versions, maximum estimated cost, and the shared canonical request fingerprint
**And** the descriptor contains no raw prompt, context, generated content, secret, or Provider payload.

**Given** a trusted current admission identity and fence supplied by the focused test contract
**When** budget authorization runs
**Then** `BudgetLedger(TenantId, UTC BudgetPeriod)` atomically owns rate admission, open-interaction bounds, reservation of maximum estimated attempt cost under deterministic ReservationId, release, settlement, `Unreconciled`, period close, and recovery against both caps before Provider invocation
**And** missing/stale pricing or budget, indeterminate ledger state, per-call excess, 100% monthly exhaustion, changed descriptor, or invalid admission blocks; 80% emits only an authorized warning.

**Given** reservation and durable ProviderInvocationAuthorized evidence
**When** the committed Provider adapter is called
**Then** `AttemptId` is used verbatim as the Provider idempotency key, safe availability/error/timeout/usage contracts remain adapter-local, and a transport retry reuses the exact descriptor, ordinal, reservation, admission identity/fence, and policy floor
**And** any changed capability, limit, price, fingerprint, readiness, or admission evidence fails closed under that AttemptId.

**Given** success, safe failure, timeout, or crash after transport
**When** recovery queries the authoritative Provider outcome by AttemptId
**Then** actual usage is reconciled exactly once, unused reservation is released only after authoritative no-usage evidence, and eligible retries never double-charge or duplicate Provider work
**And** secret poison values and raw Provider errors appear nowhere outside the adapter.

**Given** no trusted admission grant exists
**When** any application, workflow, API, or test attempts live transport
**Then** Provider invocation is impossible
**And** a local boolean, process counter, forged fence, or direct adapter call cannot bypass the admission contract.

**Given** an unauthorized budget actor, a reservation owned by another tenant, or stale tenant authority
**When** attempt preparation, reservation, reconciliation, release, status, or recovery is requested
**Then** current tenant and Party authorization denies before ledger disclosure, mutation, secret resolution, or Provider transport
**And** budget existence, limits, warning state, usage, reservation, AttemptId, Provider/model, and target-tenant timing remain hidden.

**Evidence Manifest:**

| Field | Story 6.4 evidence |
| --- | --- |
| Requirements | FR4, FR5, FR10, FR12, FR19-FR21, FR24, FR25, FR28; NFR1, NFR3, NFR4, NFR6, NFR9-NFR11; UX-DR21, UX-DR26, UX-DR44; AD-2, AD-9, AD-10, AD-13, AD-14, AD-18, AD-21, AD-22, AD-29; EXT-PROTECTION-1 |
| OwnedClauses | FR10.provider-timeout-safe-failure; FR19.budget-reservation-tenant-isolation; FR20.budget-authorization-before-ledger-or-provider; FR28.atomic-reserve-reconcile-retry; NFR1.budget-authorization-before-side-effect; NFR6.provider-secret-boundary; NFR9.generation-latency-source-events; NFR10.hard-caps-and-reservation; NFR11.no-duplicate-provider-attempt-or-reservation; UX-DR44.warning-block-indeterminate-cost; AD-2.BudgetLedger; AD-13.prepared-attempt-order; AD-21.budget-ledger-authority; AD-29.attempt-reservation-admission-queue-identities |
| Dependencies | Stories 5.8 and 6.1-6.3; EXT-PROTECTION-1, EXT-PROVIDER-1, and EXT-SECRETS-1 Available; injected trusted admission contract for focused proof |
| EvidenceLevel | Levels 2 and 4: ledger/descriptor behavior and live Provider/secret adapter compatibility; no production callability claim |
| TestOrArtifact | AgentsIdentityAttemptTests; PreparedProviderAttemptTests; BudgetLedgerAggregateTests; BudgetLedgerConcurrencyTests; BudgetReservationIsolationTests; ProviderAdapterIdempotencyIntegrationTests; ProviderOutcomeRecoveryTests; usage/reconciliation manifest |
| VerificationCommand | pwsh ./eng/verify-story-6.4.ps1 |
| NegativeEvidence | BudgetReservationIsolationTests.CrossTenantPrepareReserveReconcileReleaseAndStatusAreDenied; ProviderInvocationWithoutAdmissionTests; ConcurrentBudgetOverspendTests; RetryDoubleChargeAndChangedFingerprintTests; ProviderSecretAndRawErrorPoisonSweepTests |
| Result | Not run — backlog; EXT-PROVIDER-1 and EXT-SECRETS-1 are currently Uncommitted |

### Story 6.5: Enforce Capacity Backpressure And Tenant Fairness

As a Platform Operator,
I want shared numeric capacity, backpressure, and tenant fairness enforced before Provider transport,
So that overload cannot breach limits, starve a tenant, or bypass cost controls.

**Primary Demonstrable Outcome:** A production-like two-tenant saturation run proves exact WeightedRoundRobinV1 shares, bounded queue/admission behavior, fencing, and no concurrency, queue, or cost-cap breach across replicas and failures.

**Dependencies:**

- **Prior stories:** 6.4.
- **External:** EXT-TOPOLOGY-1 must be Available for shared multi-replica production-like evidence.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** a versioned environment profile
**When** capacity configuration is validated and published
**Then** positive PerTenantConcurrencyLimit, SystemConcurrencyLimit, PerTenantQueueDepthLimit, SystemQueueDepthLimit, Queue-or-Reject overflow, WeightedRoundRobinV1, and each tenant weight from 1 through 100 are explicit, test-visible, and readiness-recorded
**And** missing, zero, negative, unbounded, hidden host-only, unknown-version, or process-local configuration blocks callability.

**Given** a reserved deterministic AttemptId
**When** the shared allocator acquires capacity
**Then** one linearizable operation atomically enforces tenant and system scopes and returns Admitted with AdmissionId/fence, Queued with durable QueueId, or Rejected before ProviderInvocationAuthorized
**And** retries retain QueueId/position and receive no extra fairness weight.

**Given** ProviderInvocationAuthorized binds an admission fence
**When** BeginInvocation runs immediately before transport
**Then** it verifies the current unexpired fence and transitions Admitted to InvocationActive exactly once
**And** reclamation, terminalization, cancellation, expiry, a stale/forged fence, or an absent lease prevents transport.

**Given** at least two continuously eligible nonempty tenants with configured weights
**When** the production-like fixture runs complete scheduling cycles across replicas
**Then** each tenant receives exactly TenantWeight divided by the active weight sum per complete cycle, within-tenant order is allocator sequence then AttemptId, and no tenant starves
**And** measured tenant/system concurrency, queue depth, and budget reservations never exceed their caps.

**Given** queue cancellation/expiry, replica crash, allocator restart, workflow replay, or InvocationActive lease recovery
**When** recovery executes
**Then** durable terminal admission results permit no Provider call, queue identity/order survives, and an active lease is reclaimed only after a terminal attempt or timeout plus authoritative no-active-invocation lookup
**And** no duplicate admission or Provider attempt occurs.

**Evidence Manifest:**

| Field | Story 6.5 evidence |
| --- | --- |
| Requirements | FR10, FR12, FR19-FR21, FR24, FR25, FR28; NFR1, NFR3, NFR4, NFR10-NFR12; UX-DR45; AD-13, AD-17, AD-21, AD-24 |
| OwnedClauses | NFR12.numeric-tenant-system-limits; NFR12.queue-or-reject-before-provider; NFR12.cross-tenant-fairness; NFR12.cost-cap-coexistence; FR12.capacity-blocks-posting-path; FR25.capacity-status; AD-24.shared-linearizable-allocator; AD-24.fencing; AD-24.weighted-round-robin-exact-share |
| Dependencies | Story 6.4; EXT-TOPOLOGY-1 Available |
| EvidenceLevel | Levels 2, 4, and 5: allocator model, live shared component, multi-replica production-like saturation/failure proof |
| TestOrArtifact | CapacityProfileContractTests; SharedCapacityAllocatorIntegrationTests; WeightedRoundRobinSaturationTests; AdmissionFenceRecoveryTests; LR-CAPACITY-FAIRNESS observation |
| VerificationCommand | pwsh ./eng/verify-story-6.5.ps1 |
| NegativeEvidence | CapacityIsolationTests.CrossTenantLoadCannotConsumeAnotherTenantReservedShare; StaleForgedFenceCannotInvokeTests; ProcessLocalOrUnboundedConfigurationGuardTests; starvation/oversubscription/double-admission cases |
| Result | Not run — backlog; EXT-TOPOLOGY-1 is currently Uncommitted |

### Story 6.6: Join And Post Exactly Once As hexa

As a Conversation Participant,
I want the allowed automatic response posted exactly once as **hexa**,
So that the Conversation contains one attributable AI message and no membership or retry ambiguity.

**Primary Demonstrable Outcome:** The exact generated version establishes limited AI membership and appends one deterministic Conversation Message; retries, replay, and crash recovery produce no duplicate membership or message.

**Dependencies:**

- **Prior stories:** 5.4 and 6.3 through 6.5.
- **External:** EXT-CONV-AI-1 must be Available and its exact membership/posting compatibility command must pass.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** the current Agent Party identity, source Conversation authorization, allowed generated version, and Available EXT-CONV-AI-1 target
**When** Agents establishes membership
**Then** `ConversationAgentState(TenantId, ConversationId)` owns membership-established state, an Agents block, and the index of non-terminal proposals; the three-part membership protocol uses only IConversationClient.AddParticipantAsync and the supported API with ParticipantType.AiAgent/AIAgent and ParticipantRole.Member
**And** exact retries are idempotent no-ops, type/role conflicts are typed, and general participant administration is unavailable.

**Given** Conversations has externally removed the Agent participant
**When** acceptance or the mandatory pre-post revalidation detects the removal
**Then** the state records `RemovedInConversations`, blocks silent rejoin, and abandons every indexed non-terminal proposal without deleting its versions
**And** only a Tenant Agent Administrator or Conversation Facilitator can clear the block and begin explicit re-admission.

**Given** current membership, output safety, authorization, capacity, and posting gates pass
**When** the workflow appends the automatic response
**Then** MessageId and idempotency key derive deterministically from AgentInteractionId plus generated VersionId, exactly that protected version is posted, and authorship is the Agent Party identity rather than caller/system
**And** Audit Evidence links caller, Agent, Provider/model/effective version, Source Conversation, generated version, safety result, and final MessageId.

**Given** membership or posting is unavailable, unauthorized, conflicting, timed out, replayed, or crashes before/after acceptance
**When** recovery queries the authoritative Conversations outcome
**Then** the interaction records pending, safe failure, or success exactly once, no direct Conversation stream/event write occurs, and no duplicate message is appended
**And** only authoritative posted evidence produces the posted terminal state.

**Given** tenant B credentials or Party identity target tenant A Conversation
**When** membership and append seams execute
**Then** both deny before membership change or message disclosure, including colliding identifiers and exact retry keys
**And** no Provider retry, proposal, caller-authored fallback, count, or existence signal is produced.

**Evidence Manifest:**

| Field | Story 6.6 evidence |
| --- | --- |
| Requirements | FR2, FR11, FR12, FR18-FR21, FR24, FR25, FR28; NFR1-NFR5, NFR11; UX-DR22, UX-DR27, UX-DR50; AD-2, AD-6, AD-7, AD-12-AD-14, AD-17, AD-18, AD-23, AD-31 |
| OwnedClauses | FR2.agent-party-attribution; FR11.one-automatic-message; FR12.no-message-on-failed-gate; FR18.external-removal-abandons-nonterminal-proposals; FR19.membership-posting-isolation; FR24.final-message-link; NFR3.no-partial-or-duplicate-message; NFR11.no-duplicate-conversation-post; UX-DR22.posted-only-success; AD-2.ConversationAgentState; AD-6.conversations-client-only; AD-7.limited-ai-membership; AD-13.deterministic-message-id; AD-31.removal-block-and-readmission |
| Dependencies | Story 5.4; Stories 6.3-6.5; EXT-CONV-AI-1 Available |
| EvidenceLevel | Levels 2, 4, and 5: deterministic identity, live Conversations compatibility, production-like membership/post/recovery path |
| TestOrArtifact | ConversationAgentStateAggregateTests; ExternalRemovalAndReadmissionTests; AiMembershipCompatibilityTests; AutomaticPostingIntegrationTests; ConversationPostingRecoveryTests; AutomaticResponseAuditCompletenessTests; LR-CONVERSATIONS-MEMBERSHIP-POSTING observation |
| VerificationCommand | pwsh ./eng/verify-story-6.6.ps1 |
| NegativeEvidence | ConversationsPostingIsolationTests.CrossTenantMembershipAndAppendAreDenied; DuplicateMembershipAndMessageRetryTests; DirectConversationStreamWriteGuardTests |
| Result | Not run — backlog; EXT-CONV-AI-1 is currently Uncommitted |

### Story 6.7: Call hexa And Follow Automatic Status Accessibly

As a Conversation Participant,
I want one accessible **Call hexa** action with authoritative automatic-response status,
So that I can request help and understand the exact outcome without mistaking progress or failure for a posted message.

**Primary Demonstrable Outcome:** An authorized keyboard user invokes **Call hexa** and follows submitted, authoritative-pending, and projection-confirmed posted status; denied and failed paths remain safe, localized, non-success outcomes.

**Dependencies:**

- **Prior stories:** 6.1 through 6.6.
- **External:** `EXT-CONV-UI-1` must be `Available` for the contributed Conversation action/decorator/callability seam.
- **Forward dependencies:** None; Story 8.6 later qualifies full UI conformance/performance but does not supply this functional UI.

**Acceptance Criteria:**

**Given** an authorized eligible Source Conversation
**When** its FrontComposer surface renders
**Then** the sole V1 invocation entry is a Conversation-owned **Call hexa** action that names the Agent, opens an accessible prompt surface, shows effective response mode, and captures Source Conversation/caller/Agent/prompt/timestamp/idempotency exactly once
**And** mentions, commands, ambient triggers, project/folder triggers, external channels, and alternate invocation entries are absent.

**Given** the pre-integration `/agents/conversation-call` harness
**When** Story 6.7 completes
**Then** the route is unregistered and its page, navigation entry, and harness-specific tests are removed
**And** `AlternateInvocationGuardTests` prove no routable alternate entry remains.

**Given** the user submits or cancels
**When** the public client processes the action
**Then** submitted is local progress, authoritative pending appears only after an accepted interaction identity plus projection/version is received and rendered, cancel commits nothing, and a timeout forces refresh without implying success
**And** duplicate submission is blocked only for the same user session, resource, and operation family while unrelated work remains available.

**Given** automatic execution progresses
**When** authoritative status changes
**Then** denied, context blocked, safety blocked, budget blocked, capacity queued/rejected, generating, posting pending, posted, and posting failed remain distinct with semantic role, icon, visible localized text, and safe accessible name
**And** only projection-confirmed posted uses Success or renders the durable Conversation Message.

**Given** generation fails
**When** operational status and audit render
**Then** no proposal, proposal version, queue item, notification, editor, or approval/posting control is created; authorized users may reach only the separate non-approvable failure record
**And** raw prompt/context/output, Provider payloads, secrets, Party PII, stack traces, and unrelated tenant data remain absent.

**Given** keyboard, screen-reader, reduced-motion, English/French, and restrictive viewport variants
**When** the call and status flow is exercised
**Then** focus order, Escape/cancel, focus return, status live regions, whole-string key parity, non-color cues, and announcement-ready terminal DOM state conform
**And** the action fails closed with a visible reason whenever required context cannot be presented safely.

**Given** tenant B or an unauthorized Party opens tenant A Conversation or status reference
**When** UI and API paths execute
**Then** the action is absent or safely denied before Provider/workflow work and status disclosure
**And** empty states, counts, timing, accessible names, and errors reveal no tenant A interaction or Conversation.

**Evidence Manifest:**

| Field | Story 6.7 evidence |
| --- | --- |
| Requirements | FR8, FR10-FR13, FR19-FR25, FR28; NFR1-NFR4, NFR13, NFR14; UX-DR1, UX-DR11-UX-DR19, UX-DR22, UX-DR24, UX-DR27, UX-DR30-UX-DR41, UX-DR48-UX-DR50; AD-6, AD-12, AD-15, AD-17, AD-25, AD-26, AD-31; EXT-CONV-UI-1 |
| OwnedClauses | FR8.sole-call-action; FR10.failure-status-no-proposal; FR11.posted-attribution-visible; FR22.call-ui-parity; NFR13.call-accessibility-localization-responsive; NFR14.instrumentation-seams-for-call-status; UX-DR24.sole-entry; UX-DR27.authoritative-states; UX-DR36.localized-live-region; UX-DR40.restrictive-viewport; UX-DR48.failure-record-only; UX-DR50.truth-flow; AD-26.browser-timing-seams |
| Dependencies | Stories 6.1-6.6; EXT-CONV-UI-1 Available |
| EvidenceLevel | Levels 2 and 4: component behavior and live public-contract/UI integration; performance attainment remains Story 8.6 |
| TestOrArtifact | CallHexaComponentTests; AutomaticCallStatusIntegrationTests; CallHexaAccessibilityTests; AgentsResourcesParityTests; AlternateInvocationGuardTests; browser timing instrumentation contract tests |
| VerificationCommand | pwsh ./eng/verify-story-6.7.ps1 |
| NegativeEvidence | CallHexaIsolationTests.CrossTenantActionAndStatusAreDenied; AlternateInvocationGuardTests; OptimisticPendingOrPostedStateTests; generation-failure proposal-absence tests |
| Result | Blocked — backlog; requires Stories 6.1-6.6 and EXT-CONV-UI-1 is Uncommitted |

## Epic 7: Complete Confirmation And Approval

An Approver can discover, revise, regenerate, resolve, and post exactly one proposal version without losing history or bypassing current gates.

**Status:** active forward backlog.

**Story count:** 6.

**Dependency topology:** 7.1 creates the independently usable pending proposal and discovery surfaces; 7.2 adds immutable editing; 7.3 regenerates under current gates; 7.4 approves/posts one selected version; 7.5 resolves without posting; 7.6 owns deterministic expiry and races. Each transition depends only on already available proposal state and earlier runtime foundations.

### Story 7.1: Create And Discover A Pending Proposal

As an Approver,
I want successful confirmation-mode generation to create one discoverable pending proposal,
So that generated content can wait outside the Conversation for authorized review.

**Primary Demonstrable Outcome:** One successful confirmation-mode generation creates exactly one pending proposal with an immutable initial version, pending count, queue entry, and Conversation status reference visible only to authorized Approvers.

**Dependencies:**

- **Prior stories:** 5.4, 5.8, and 6.1 through 6.5.
- **External:** EXT-PROTECTION-1 plus consumed Provider, tokenizer, safety, secret, and topology seams must be Available for live generation; no Conversations posting seam is used.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** a confirmation-mode interaction with successful allowed generation
**When** the workflow records the output
**Then** EventStore creates one Proposed Agent Reply with the architecture-defined identity relationship to AgentInteractionId and Source Conversation, assigns the next `VersionOrdinal`, and uses `AgentsIdentity` to derive one immutable generated `ProposalVersionId` from (`AgentInteractionId`, `VersionOrdinal`, generated kind), plus current proposal state, caller/Agent/Provider/model/effective-version evidence, snapshotted Approver Policy, and stored ExpiresAt
**And** no Conversation Message exists and exact replay creates no duplicate proposal or version.

**Given** the proposal event is projected
**When** an authorized Approver opens the in-product pending count, Needs my action queue, or Conversation status entry
**Then** all surfaces converge on the same proposal identity/state, source reference, caller, responsibility basis, age, and expiry without rendering proposal content in counts or notifications
**And** loading, empty, filtered-empty, stale, error, and permission-denied states remain distinct.

**Given** generation fails, is incomplete, times out, or fails output safety
**When** confirmation-mode orchestration completes
**Then** no proposal, version, pending count, queue row, notification, editor, approval action, or posting action is created
**And** authorized operational/audit surfaces may expose only the separate non-approvable failure record.

**Given** current approval authority is missing, stale, ambiguous, revoked, unavailable, or belongs to another tenant
**When** proposal discovery executes
**Then** records and counts fail closed without disclosing proposal existence, content, source Conversation, caller, expiry, or policy basis
**And** no status or accessible name leaks a target-tenant value.

**Evidence Manifest:**

| Field | Story 7.1 evidence |
| --- | --- |
| Requirements | FR7, FR13, FR14, FR18-FR25, FR27, FR28; NFR1-NFR5, NFR11, NFR13; UX-DR1, UX-DR2, UX-DR6, UX-DR9, UX-DR22, UX-DR28-UX-DR30, UX-DR34, UX-DR36, UX-DR47, UX-DR48, UX-DR50; AD-4, AD-5, AD-8, AD-12-AD-15, AD-17, AD-18, AD-22, AD-29; EXT-PROTECTION-1 |
| OwnedClauses | FR13.success-only-proposal; FR13.in-product-discovery; FR14.initial-immutable-version; FR18.stored-expiry; FR20.discovery-authorization; NFR3.no-lost-or-duplicate-version; NFR13.accessible-localized-queue; UX-DR47.in-product-only-notification; UX-DR48.failed-generation-no-proposal; AD-5.append-only-proposal-state |
| Dependencies | Stories 5.4 and 5.8; Stories 6.1-6.5; EXT-PROTECTION-1 and all executed external seams Available |
| EvidenceLevel | Levels 2 and 4: proposal/projection behavior and live confirmation-generation-to-queue path |
| TestOrArtifact | AgentsIdentityProposalTests; ProposalCreationAggregateTests; PendingProposalProjectionTests; ConfirmationProposalIntegrationTests; ProposalQueueComponentTests; initial proposal evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-7.1.ps1 |
| NegativeEvidence | ProposalDiscoveryIsolationTests.CrossTenantCountsRowsAndStatusDiscloseNothing; FailedGenerationCreatesNoProposalTests; DuplicateProposalAndVersionReplayTests |
| Result | Not run — backlog; requires prior runtime stories and Available executed seams |

### Story 7.2: Edit An Immutable Proposal Version

As an Approver,
I want an edit to create a new immutable proposal version,
So that I can correct the draft without overwriting what **hexa** generated.

**Primary Demonstrable Outcome:** One authorized edit appends one new version with authorship and source linkage; the original generated version remains intact and selectable for authorized comparison.

**Dependencies:**

- **Prior stories:** 5.8 and 7.1.
- **External:** EXT-PROTECTION-1 remains required wherever protected content is materialized.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** a pending nonterminal proposal and current edit authority
**When** the Approver submits edited content at the expected proposal revision
**Then** the aggregate assigns the next `VersionOrdinal` and `AgentsIdentity` derives one immutable edited `ProposalVersionId` from (`AgentInteractionId`, `VersionOrdinal`, edited kind), with editor PartyId, timestamp, source VersionId, policy basis, and protected content
**And** every prior generated/edited/regenerated version remains unchanged and addressable to authorized users.

**Given** duplicate submission, stale expected revision, concurrent edit, empty/invalid content, or a proposal already approved, rejected, abandoned, expired, posted, or otherwise terminal
**When** the edit command runs
**Then** an exact duplicate is idempotent while invalid/conflicting/terminal commands return typed outcomes and append no version
**And** optimistic client text never becomes authoritative state.

**Given** the proposal editor and version history
**When** generated and edited versions render
**Then** kind, author/source, timestamp, selected state, approval/posting markers, and safe metadata are distinct; a proposal is never styled as a posted Conversation Message
**And** keyboard edit/save/cancel, Escape, focus return, whole-string English/French copy, reduced motion, and restrictive viewport behavior conform.

**Given** tenant B or a Party without current edit authority targets tenant A proposal
**When** edit, detail, and history paths execute
**Then** denial occurs before content read or version append
**And** errors, timing text, history counts, accessible names, logs, and audit summaries reveal no target proposal or content.

**Evidence Manifest:**

| Field | Story 7.2 evidence |
| --- | --- |
| Requirements | FR14, FR15, FR19-FR24; NFR1-NFR5, NFR13; UX-DR7, UX-DR8, UX-DR22, UX-DR28, UX-DR31-UX-DR35, UX-DR37-UX-DR40, UX-DR50; AD-4, AD-5, AD-8, AD-12-AD-15, AD-22, AD-29; EXT-PROTECTION-1 |
| OwnedClauses | FR14.preserve-all-versions; FR15.authorized-edit-only; FR15.edit-remains-outside-conversation; FR20.current-edit-authority; FR24.edit-authorship-evidence; NFR3.version-not-overwritten; UX-DR8.complete-version-history; UX-DR35.keyboard-editor; AD-5.immutable-edit-version |
| Dependencies | Stories 5.8 and 7.1; EXT-PROTECTION-1 Available |
| EvidenceLevel | Levels 2 and 4: aggregate/concurrency behavior plus live editor/public-contract path |
| TestOrArtifact | AgentsIdentityProposalVersionTests; ProposalEditAggregateTests; ProposalEditConcurrencyTests; ProposalVersionHistoryQueryTests; ProposalEditorComponentTests; edit evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-7.2.ps1 |
| NegativeEvidence | ProposalEditIsolationTests.CrossTenantEditAndHistoryAreDenied; TerminalProposalEditTests; PriorVersionOverwriteGuardTests; raw-content telemetry poison sweep |
| Result | Not run — backlog; requires Story 7.1 |

### Story 7.3: Regenerate Under Fresh Gates

As an Approver,
I want regeneration to re-evaluate current context, safety, Provider, cost, and capacity gates,
So that a new version cannot reuse stale authority or changed inputs under an old attempt.

**Primary Demonstrable Outcome:** One authorized regeneration creates one new immutable generated version from a freshly authorized complete-context attempt, while any changed or failed gate leaves existing proposal history intact and performs no unsafe effect.

**Dependencies:**

- **Prior stories:** 5.8, 7.1, and 7.2.
- **External:** EXT-PROTECTION-1, EXT-PROVIDER-1, EXT-TOKEN-1, EXT-SAFETY-1, EXT-SECRETS-1, and the production-like capacity seam must be Available when executed.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** a pending proposal and current regeneration authority
**When** regeneration is requested
**Then** the workflow repeats current tenant/Conversation authorization, complete Conversation read, exact token measurement, Provider capability high-water/readiness, prompt/context safety, pricing/budget reservation, shared capacity admission, and prepared-attempt authorization
**And** the aggregate assigns the next `AttemptOrdinal`, `AgentsIdentity` derives the new `AttemptId`, and no client-provided seed or local identity helper can affect it.

**Given** every fresh gate passes and the Provider output passes current output safety
**When** the result is recorded
**Then** the aggregate assigns the next `VersionOrdinal` and `AgentsIdentity` derives one immutable regenerated `ProposalVersionId` from (`AgentInteractionId`, `VersionOrdinal`, regenerated kind), with attempt, Provider/model/effective version, safety, cost, and source references
**And** all earlier generated and edited versions remain intact and no Conversation Message is created.

**Given** any authorization, context, tokenizer, safety, Provider, secret, pricing, budget, capacity, timeout, or output gate blocks or changes the prepared fingerprint
**When** regeneration executes or retries
**Then** no new proposal version or Provider substitution occurs under the same AttemptId, the existing proposal remains pending unless separately terminalized, and failure evidence remains non-approvable
**And** retries use policy at least as restrictive as the initial attempt and reuse only an unchanged reservation/admission identity where eligible.

**Given** a terminal proposal, stale expected revision, duplicate request, concurrent expiry, or tenant-mismatched caller
**When** regeneration is attempted
**Then** the command is rejected or idempotently resolved before Provider transport and creates no duplicate attempt/version
**And** exact transport replay never increments either ordinal, a different payload fingerprint is a typed conflict, and cross-tenant status, version count, context, Provider, and failure details are not disclosed.

**Evidence Manifest:**

| Field | Story 7.3 evidence |
| --- | --- |
| Requirements | FR9, FR10, FR14, FR16, FR19-FR21, FR24-FR28; NFR1-NFR12; UX-DR7, UX-DR8, UX-DR22, UX-DR27, UX-DR28, UX-DR31, UX-DR36, UX-DR43, UX-DR48, UX-DR50; AD-4, AD-5, AD-8-AD-14, AD-17, AD-18, AD-20-AD-22, AD-24, AD-29; EXT-PROTECTION-1 |
| OwnedClauses | FR16.same-source-and-snapshot-provenance; FR16.new-version-preserves-history; FR16.terminal-block; FR27.fresh-no-weaker-safety; NFR8.complete-context-revalidation; NFR10.regeneration-reservation-reuse; NFR11.no-duplicate-attempt-or-version; UX-DR48.failed-regeneration-not-version; AD-10.high-water-recheck; AD-13.changed-input-fails-attempt |
| Dependencies | Stories 5.8 and 7.1-7.2; EXT-PROTECTION-1, EXT-PROVIDER-1, EXT-TOKEN-1, EXT-SAFETY-1, EXT-SECRETS-1 and capacity seam Available |
| EvidenceLevel | Levels 2, 4, and 5: transition logic, live adapters, production-like retry/concurrency path |
| TestOrArtifact | AgentsIdentityRegenerationTests; ProposalRegenerationAggregateTests; RegenerationFreshGateIntegrationTests; RegenerationRetryRecoveryTests; ProposalVersionHistoryTests; regeneration evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-7.3.ps1 |
| NegativeEvidence | RegenerationIsolationTests.CrossTenantRegenerationIsDeniedBeforeProvider; StaleChangedFingerprintAndWeakerPolicyTests; TerminalExpiryRaceAndDuplicateVersionTests |
| Result | Not run — backlog; required external seams are currently Uncommitted |

### Story 7.4: Approve And Post One Selected Version

As an Approver,
I want to approve and post exactly one selected proposal version,
So that only the reviewed response becomes a Conversation Message attributed to **hexa**.

**Primary Demonstrable Outcome:** One authorized approval selects one immutable VersionId, records a non-success posting-pending state, and produces exactly one authoritative posted Conversation Message with complete approval evidence.

**Dependencies:**

- **Prior stories:** 5.8 and 7.1; 7.2 and 7.3 versions are supported when present.
- **External:** EXT-PROTECTION-1, EXT-SAFETY-1, and EXT-CONV-AI-1 must be Available for live approval/posting.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** a pending proposal, selected existing VersionId, and current approval authority
**When** approval executes at the expected EventStore revision
**Then** current tenant, Party, approver-policy basis, Conversation access, output-safety floor, Agent identity, and operation-gate records are re-evaluated before side effects
**And** exactly one approval event binds selected VersionId, Approver PartyId, policy basis, timestamp, and posting-pending state; no other version is eligible.

**Given** approval is accepted
**When** UI/API render the result
**Then** approved and posting pending remain non-success authoritative progress with accepted identity and projection/version
**And** only a later authoritative posted projection may use Success or claim a Conversation Message.

**Given** current limited AI membership and posting gates pass through EXT-CONV-AI-1
**When** the workflow appends the selected version
**Then** deterministic MessageId/idempotency derived from interaction plus selected VersionId produces exactly one message authored by the Agent Party identity
**And** audit evidence links caller, Agent, source, selected version, Provider/model, safety, Approver, policy basis, approval time, and final MessageId.

**Given** approval/posting is duplicated, replayed, times out, crashes, races another resolution, or the selected version/gate becomes invalid
**When** recovery executes
**Then** EventStore concurrency chooses one valid ordering, terminal or invalid state cannot post, authoritative Conversations outcome prevents duplicate messages, and failure remains distinct from approval
**And** no alternate version, caller-authored fallback, or direct Conversation stream write occurs.

**Given** tenant B or an unauthorized Party targets tenant A proposal
**When** approve, membership, posting, status, or audit paths execute
**Then** every path denies before mutation or disclosure
**And** proposal/version existence, policy basis, MessageId, Conversation membership, counts, and accessible output remain undisclosed.

**Evidence Manifest:**

| Field | Story 7.4 evidence |
| --- | --- |
| Requirements | FR2, FR7, FR14, FR17, FR19-FR25, FR27, FR28; NFR1-NFR7, NFR9, NFR11, NFR13, NFR14; UX-DR7, UX-DR8, UX-DR11, UX-DR12, UX-DR22, UX-DR28, UX-DR31-UX-DR40, UX-DR50; AD-4-AD-8, AD-12-AD-15, AD-17, AD-18, AD-20, AD-22, AD-23, AD-25, AD-26; EXT-PROTECTION-1 |
| OwnedClauses | FR17.approve-exact-selected-version; FR17.agent-attribution; FR17.complete-approval-post-link; FR20.current-approval-authorization; NFR3.no-partial-or-duplicate-post; NFR5.approval-path-audit; NFR11.no-duplicate-post-on-recovery; UX-DR22.approved-not-posted; UX-DR50.approved-posting-posted-truth; AD-13.deterministic-selected-version-post |
| Dependencies | Stories 5.8 and 7.1; optional earlier versions from 7.2-7.3; EXT-PROTECTION-1, EXT-SAFETY-1 and EXT-CONV-AI-1 Available |
| EvidenceLevel | Levels 2, 4, and 5: approval transition, live membership/posting, production-like race/recovery proof |
| TestOrArtifact | ProposalApprovalAggregateTests; ApprovedVersionPostingIntegrationTests; ApprovalPostingRecoveryTests; ApprovalUiStateTests; approval/post audit-completeness manifest |
| VerificationCommand | pwsh ./eng/verify-story-7.4.ps1 |
| NegativeEvidence | ProposalApprovalIsolationTests.CrossTenantApprovalPostingAndAuditAreDenied; WrongVersionAndDuplicatePostTests; ApprovalExpiryConcurrencyTests; ApprovedIsNotPostedUiTests |
| Result | Blocked — backlog; EXT-PROTECTION-1, EXT-SAFETY-1 and EXT-CONV-AI-1 are currently Uncommitted |

### Story 7.5: Reject Or Abandon A Proposal

As an Approver,
I want to resolve a proposal without posting it,
So that rejected or intentionally abandoned content becomes terminal and can never enter the Conversation.

**Primary Demonstrable Outcome:** One authorized non-posting resolution records either Rejected or Abandoned as the single terminal outcome, preserves all versions, and permanently disables edit, regenerate, approve, and post actions.

**Dependencies:**

- **Prior stories:** 7.1.
- **External:** None newly consumed.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** a pending proposal and current resolution authority
**When** the Approver rejects it with policy-required safe rationale metadata
**Then** EventStore records one Rejected terminal decision with actor, policy basis, timestamp, and protected evidence
**And** all proposal versions remain preserved while edit, regenerate, approve, membership, and posting become impossible.

**Given** a pending proposal and current resolution authority
**When** the Approver abandons it
**Then** EventStore records one Abandoned terminal decision with actor, policy basis, timestamp, and protected evidence
**And** the outcome is distinct from Rejected, Expired, generation failure, and posting failure.

**Given** duplicate, stale-revision, concurrent approval/expiry, or already terminal resolution
**When** reject or abandon executes
**Then** an exact duplicate is idempotent, EventStore concurrency chooses one terminal ordering, and no second terminal decision or Conversation side effect occurs
**And** UI/API show only the authoritative terminal result.

**Given** keyboard, localized, reduced-motion, or restrictive-viewport operation
**When** reject/abandon confirmation renders and completes
**Then** the action requires current authorization and explicit confirmation, supports safe Escape/focus return/live-region announcement, and remains non-success
**And** inability to present required context blocks the action with review-only access.

**Given** a tenant-mismatched or unauthorized caller
**When** resolution, status, history, or audit paths execute
**Then** denial occurs before mutation or content/version disclosure
**And** no proposal existence, terminal reason, count, or accessible text leaks.

**Evidence Manifest:**

| Field | Story 7.5 evidence |
| --- | --- |
| Requirements | FR14, FR18-FR25; NFR1-NFR5, NFR13, NFR14; UX-DR7, UX-DR8, UX-DR11, UX-DR12, UX-DR22, UX-DR28, UX-DR31-UX-DR40, UX-DR50; AD-5, AD-8, AD-12-AD-15, AD-17, AD-25, AD-26 |
| OwnedClauses | FR18.rejected-terminal; FR18.abandoned-terminal; FR18.terminal-cannot-post; FR24.non-posting-resolution-evidence; NFR3.versions-preserved; NFR5.terminal-audit; NFR13.accessible-confirmed-resolution; UX-DR22.distinct-terminal-states; AD-5.single-terminal-order |
| Dependencies | Story 7.1 |
| EvidenceLevel | Levels 2 and 4: terminal/concurrency behavior and live public-contract/UI path |
| TestOrArtifact | ProposalNonPostingResolutionTests; ProposalResolutionConcurrencyTests; ProposalResolutionUiTests; terminal evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-7.5.ps1 |
| NegativeEvidence | ProposalResolutionIsolationTests.CrossTenantRejectAbandonAndHistoryAreDenied; TerminalMutationGuardTests; ConcurrentApproveRejectAbandonTests |
| Result | Not run — backlog; requires Story 7.1 |

### Story 7.6: Expire A Proposal Deterministically

As an Approver,
I want unresolved proposals to expire at their stored deadline,
So that stale generated content becomes terminal predictably and cannot later post.

**Primary Demonstrable Outcome:** A durable timer expires one still-pending proposal at or after its immutable ExpiresAt across restart, while every competing transition resolves to one EventStore-authoritative ordering with no duplicate timer or post.

**Dependencies:**

- **Prior stories:** 6.1 and 7.1.
- **External:** Uses the platform Dapr Workflow/topology established by Stories 5.6 and 6.1; no new external commitment.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** Agent expiry policy defaults to 24 hours and permits an authorized value from 1 hour through 30 days
**When** a future proposal is created
**Then** its exact ExpiresAt and policy version are stored once from injected time, and a later policy change affects only future proposals
**And** an existing proposal deadline is never moved or recomputed.

**Given** a nonterminal proposal and its deterministic workflow timer
**When** injected time reaches or passes stored ExpiresAt
**Then** EventStore records one Expired terminal transition at the expected revision, all versions remain preserved, pending counts/queues converge, and no approval or post can later succeed
**And** duplicate timer delivery, replay, and restart are idempotent.

**Given** edit, regeneration, approval, rejection, abandonment, or posting races expiry
**When** commands arrive at competing revisions
**Then** exactly one valid EventStore ordering wins, any losing command receives a typed authoritative outcome, and no duplicate version, terminal decision, Provider attempt, reservation, or Conversation Message is created
**And** pre-existing terminal decisions remain immutable through recovery.

**Given** expiry status renders
**When** API/UI, queue, Conversation status, and audit projections catch up
**Then** Expired is a non-success terminal state with stored deadline, safe evidence, localized live-region announcement, and a route to start a new Agent Call
**And** stale/pending projection state is never shown as authoritative expiry success.

**Given** unauthorized expiry-policy administration or cross-tenant proposal inspection
**When** policy, status, queue, history, or audit paths execute
**Then** denial occurs before mutation or disclosure
**And** deadline, proposal existence, version count, policy, caller, and Conversation data remain hidden.

**Evidence Manifest:**

| Field | Story 7.6 evidence |
| --- | --- |
| Requirements | FR18-FR25, FR28; NFR1-NFR5, NFR11, NFR13, NFR14; UX-DR6-UX-DR8, UX-DR22, UX-DR28, UX-DR32-UX-DR40, UX-DR47, UX-DR50; AD-3-AD-5, AD-8, AD-12, AD-13, AD-17, AD-18, AD-23, AD-25, AD-26 |
| OwnedClauses | FR18.default-configurable-future-only-expiry; FR18.durable-timer-at-or-after-ExpiresAt; FR18.expired-cannot-post; NFR3.versions-survive-expiry; NFR11.no-duplicate-timer-or-terminal-decision; NFR13.accessible-localized-expiry; UX-DR22.expired-distinct-terminal; AD-5.expiry-state; AD-23.timer-recovery-inventory |
| Dependencies | Stories 6.1 and 7.1; existing platform Dapr Workflow/topology |
| EvidenceLevel | Levels 2, 4, and 5: timer/concurrency logic, live Dapr Workflow, production-like restart/race proof |
| TestOrArtifact | ProposalExpiryPolicyTests; ProposalExpiryWorkflowIntegrationTests; ProposalExpiryRaceTests; ProposalExpiryUiTests; timer/recovery evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-7.6.ps1 |
| NegativeEvidence | ProposalExpiryIsolationTests.CrossTenantPolicyAndProposalDataAreDenied; DuplicateTimerAndPostAfterExpiryTests; ExpiryApproveRegenerateRaceMatrixTests |
| Result | Not run — backlog; requires Stories 6.1 and 7.1 |

## Epic 8: Governance Operations And Release Qualification

Authorized governance and release operators can retain, hold, export, delete, and govern sensitive Agent data and policies, calculate qualification metrics under versioned contracts, and inspect current launch blockers without confusing deterministic implementation evidence with live release attainment.

**Status:** active forward backlog.

**Story count:** 8.

**Dependency topology:** Stories 8.1 and 8.4 begin from completed active capabilities in Epics 5–7; Stories 8.2 and 8.3 branch independently from 8.1; Story 8.5 consumes prior runtime/product evidence; Story 8.6 consumes the completed interactive surfaces; Story 8.7 inspects the bounded evidence produced through 8.6; Story 8.8 owns durable compliance inspection over protected evidence. `EXT-PROTECTION-1` gates 8.1–8.3 and 8.8, `EXT-SECRETS-1` gates 8.1–8.3, and `EXT-TOPOLOGY-1` gates 8.5–8.8. No Epic 8 story depends on `RQ-1`.

### Story 8.1: Retain Sensitive Content And Apply Legal Holds

As a Compliance Inspector,
I want sensitive Agent content to follow one durable retention and legal-hold policy,
So that protected evidence remains available exactly while policy requires and can expire safely when no hold applies.

**Primary Demonstrable Outcome:** One terminal interaction receives an immutable 365-day sensitive-content retention deadline, and an authorized legal hold suspends expiry until its durable release without rewriting EventStore history.

**Dependencies:**

- **Prior stories:** 5.2 and 5.8 for authorized protected EventStore operations and 7.6 for deterministic proposal terminal timestamps.
- **External:** `EXT-PROTECTION-1` and `EXT-SECRETS-1` must be `Available` for live DEK pin/unpin and protected retention execution.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** an interaction reaches an authoritative terminal state with protected prompt, context, generated, edited, proposal, or audit content
**When** retention is scheduled
**Then** the `retention` projection records an immutable deadline exactly 365 days after that terminal timestamp, the governing policy version, protected scope, and safe evidence references
**And** posted Conversation Messages remain under Hexalith.Conversations retention rather than Agents retention.

**Given** an authorized operator submits a legal hold for a named tenant-scoped protected scope
**When** EventStore accepts the deterministic `LegalHold` command
**Then** `LegalHold(TenantId, HoldId)` accepts only `interaction:<id>` or `class:<class>` with an explicit UTC range, records expected source/protection revisions, resolves the scope to explicit interaction keys at a checkpoint, pins every DEK, and becomes `Active` only after all pins are confirmed
**And** the append-only `legal-hold` projection and high-impact UI follow `submitted -> authoritative pending -> projection-confirmed terminal`, while exact duplicate submission is idempotent and a divergent duplicate conflicts.

**Given** a Compliance Inspector releases an active hold at its expected revision
**When** every named DEK is unpinned successfully
**Then** `LegalHold` appends a release transition carrying actor, role basis, justification, scope, checkpoint, expected revision, and key outcomes
**And** history is never edited or deleted and partial unpin failure remains restrictive.

**Given** a retention timer becomes due while an active matching hold exists
**When** the durable expiry workflow rechecks current hold state
**Then** no payload protection, redaction, deletion, or projection purge occurs and the next authorized status exposes a safe held outcome
**And** replay, restart, duplicate timer delivery, or a release/expiry race cannot bypass the hold or duplicate an expiry effect.

**Given** the hold has been authoritatively released and the stored retention deadline has passed
**When** the workflow resumes
**Then** it emits exactly one eligible-expiry request for Story 8.3 processing while preserving immutable EventStore history and support-safe audit references
**And** the retention surface never calls an unconfirmed purge successful.

**Given** an unauthorized Party, stale role, or different tenant attempts to inspect or mutate retention or legal-hold state
**When** the API, client, UI, projection, or workflow command executes
**Then** current tenant and Party authorization denies before disclosure or mutation
**And** content existence, protected scope, deadlines, hold basis, actor, and evidence references remain hidden.

**Evidence Manifest:**

| Field | Story 8.1 evidence |
| --- | --- |
| Requirements | FR18-FR25, FR28; NFR1-NFR5, NFR11, NFR13; UX-DR1, UX-DR9-UX-DR18, UX-DR29-UX-DR33, UX-DR36-UX-DR41, UX-DR46, UX-DR50; AD-1-AD-5, AD-8, AD-12, AD-13, AD-17, AD-20, AD-22, AD-23, AD-25, AD-26; EXT-PROTECTION-1; EXT-SECRETS-1 |
| OwnedClauses | FR18.terminal-evidence-retained; FR19.retention-hold-tenant-isolation; FR20.current-governance-authorization; FR24.retention-hold-audit; FR28.audit-governance-active; PRD-OQ8.365-day-terminal-retention; PRD-OQ8.legal-hold-suspends-expiry; NFR11.timer-replay-no-duplicate-effect; UX-DR31.LegalHold-lock-scope; UX-DR40.LegalHold-restrictive-viewport-block; UX-DR46.retention-and-hold; AD-2.LegalHold; AD-22.two-phase-DEK-pinning; AD-23.retention-legal-hold-projections |
| Dependencies | Stories 5.2, 5.8, and 7.6; EXT-PROTECTION-1 and EXT-SECRETS-1 Available |
| EvidenceLevel | Levels 2 and 4: deterministic retention/hold behavior and live EventStore/Dapr component evidence |
| TestOrArtifact | RetentionPolicyTests; LegalHoldAggregateTests; RetentionHoldWorkflowIntegrationTests; RetentionLegalHoldUiContractTests; retention/hold evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-8.1.ps1 |
| NegativeEvidence | RetentionLegalHoldIsolationTests.CrossTenantInspectCreateReleaseAreDenied; RevokedGovernanceRoleTests; HoldExpiryReleaseRaceTests; DuplicateRetentionTimerTests |
| Result | Blocked — backlog; requires Stories 5.2, 5.8, 7.6 and Available protection/secret dependencies |

### Story 8.2: Export Authorized Audit Content Securely

As an authorized governance operator,
I want a tenant-scoped encrypted and time-limited audit export,
So that approved evidence can be transferred without exposing secrets, unrelated tenant data, or an indefinitely usable artifact.

**Primary Demonstrable Outcome:** One authorized export request produces one encrypted manifested artifact with an enforced expiry and a tenant-scoped audit trail, while the service and UI never expose plaintext content or secret material.

**Dependencies:**

- **Prior stories:** 8.1 for governed protected scope and current legal-hold/retention status.
- **External:** `EXT-PROTECTION-1` and `EXT-SECRETS-1` must be `Available` with exact targets and compatibility commands passing before protected-content selection, encryption-key resolution, or live export execution.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** an authorized export operator selects an allowed tenant scope, content classes, and time range
**When** the request is accepted
**Then** EventStore records one deterministic export identity and authoritative pending state before work begins, and the manifest names the authorized scope, immutable source revisions, item counts, hashes, encryption method reference, created time, exclusive expiry, and safe audit reference
**And** the request never expands beyond the approved tenant, time range, content class, or current authorization.

**Given** `EXT-SECRETS-1` is Available and key access succeeds through the platform host
**When** the export is materialized
**Then** `AuditExport(TenantId, ExportId)` encrypts the artifact under an export envelope key wrapped by the tenant KEK, appends `ManifestSealed` with manifest hash, signature reference, item hashes, stream/projection revision ranges, and key versions, and the `export` projection reaches completed only after artifact and manifest verification
**And** exact replay reuses the export identity rather than producing an untracked second artifact.

**Given** an authorized Compliance Inspector requests the sealed export key
**When** delivery is approved and unexpired
**Then** the `EXT-SECRETS-1` custodian delivers it directly to that principal outside Agents responses
**And** no key or secret appears in an Agents response, manifest, projection, event, log, trace, metric, or browser surface.

**Given** the download authorization or artifact lifetime has expired
**When** any client requests the artifact
**Then** access fails with a safe typed outcome, no plaintext or renewed URL is inferred, and reauthorization requires a new audited export request
**And** incomplete, encryption-failed, manifest-mismatched, or partially published exports remain restrictive non-success states.

**Given** secret resolution is denied, stale, rotated incompatibly, or unavailable
**When** export processing reaches encryption
**Then** it fails closed before plaintext artifact publication, records only safe error classification and references, and can resume idempotently after an authorized recovery
**And** logs, traces, UI, API responses, events, and evidence contain no raw secret, prompt, context, proposal, Provider payload, or stack trace.

**Given** an unauthorized Party or another tenant attempts to request, inspect, download, cancel, or replay an export
**When** any public or internal export path executes
**Then** current authorization denies before content selection, manifest disclosure, key resolution, or artifact access
**And** export existence, counts, hashes, scope, timing, and failure details remain hidden.

**Evidence Manifest:**

| Field | Story 8.2 evidence |
| --- | --- |
| Requirements | FR19-FR24, FR28; NFR1-NFR6, NFR13; UX-DR1, UX-DR9-UX-DR18, UX-DR29-UX-DR33, UX-DR36-UX-DR41, UX-DR46, UX-DR50; AD-2, AD-8, AD-12, AD-13, AD-17, AD-20, AD-22, AD-23, AD-25, AD-26; EXT-PROTECTION-1; EXT-SECRETS-1 |
| OwnedClauses | FR19.export-tenant-isolation; FR20.export-authorization-before-selection; FR23.export-public-contract; FR24.export-audit; FR28.audit-governance-active; PRD-OQ8.authorized-encrypted-time-limited-export; NFR2.no-unauthorized-audit-content; NFR6.secret-never-exposed; UX-DR31.ExportRequest-lock-scope; UX-DR40.ExportRequest-restrictive-viewport-block; UX-DR46.encrypted-time-limited-export; AD-2.AuditExport; AD-20.protected-content-and-secret-boundary; AD-22.manifest-sealing-and-key-delivery; AD-23.export-projection |
| Dependencies | Story 8.1; EXT-PROTECTION-1 and EXT-SECRETS-1 Available with accepted exact targets and passing compatibility commands |
| EvidenceLevel | Levels 2, 4, and 5: export logic, live secret/export components, and production-like encrypted-artifact proof |
| TestOrArtifact | AuditExportAggregateTests; EncryptedExportIntegrationTests; ExportExpiryAndReplayTests; ExportUiContractTests; encrypted export manifest and no-leak scan |
| VerificationCommand | pwsh ./eng/verify-story-8.2.ps1 |
| NegativeEvidence | AuditExportIsolationTests.CrossTenantRequestInspectDownloadAndReplayAreDenied; RevokedExportRoleTests; SecretResolutionFailureTests; PlaintextArtifactPoisonScan; ManifestMismatchAndExpiredDownloadTests |
| Result | Blocked — backlog; EXT-PROTECTION-1 and EXT-SECRETS-1 are Uncommitted and Story 8.1 is required |

### Story 8.3: Delete Protected Content And Purge Projections

As an authorized governance operator,
I want approved protected content deleted through restrictive cryptographic erasure or redaction and named projection purges,
So that immutable history retains only a support-safe tombstone and no content-bearing copy survives the confirmed operation.

**Primary Demonstrable Outcome:** One authorized deletion reaches success only after protected EventStore payloads and every named content-bearing projection report restrictive completion, leaving a non-content tombstone and no recoverable sensitive payload.

**Dependencies:**

- **Prior stories:** 8.1 for retention eligibility, legal-hold enforcement, and protected scope.
- **External:** `EXT-PROTECTION-1` and `EXT-SECRETS-1` must be `Available` with exact targets and compatibility commands passing for cryptographic erasure/key operations.
- **Forward dependencies:** None; export is an independent authorized operation and is not a deletion prerequisite.

**Acceptance Criteria:**

**Given** a deletion request for sensitive Agent content
**When** current tenant authorization, deletion policy, retention eligibility, and legal-hold state are evaluated
**Then** `ProtectedDeletion(TenantId, DeletionId)` accepts a request only from a Platform Operator and becomes executable only after a distinct Compliance Inspector approval; it rejects before erasure when scope is ambiguous, authority is missing/stale, expected revisions differ, or any active hold protects the content
**And** acceptance stores one deterministic request identity, immutable scope, expected source revisions, and authoritative pending state.

**Given** an eligible accepted request and Available secret/erasure seam
**When** deletion executes
**Then** the protection engine destroys every in-scope interaction DEK and returns an irreversible receipt, EventStore replay produces typed `Erased` without rewriting history, and a support-safe non-content tombstone records policy, scope reference, requester, approver, timestamps, and result
**And** Provider secrets, raw payloads, Party PII, and deleted content never appear in the tombstone, status, audit, log, trace, or evidence.

**Given** projection purge begins
**When** each affected contract reports its restrictive result
**Then** completion explicitly names `provider-capability-pricing` when content-bearing, `agent-interaction-status`, `proposal-detail`, `proposal-version-history`, `pending-proposal-queue`, `pending-proposal-count`, `audit-evidence`, `retention`, `legal-hold`, `export`, `deletion`, and every content-bearing metric projection identified by its current contract
**And** `agent-setup-readiness`, `budget-reservation-usage`, `launch-readiness`, and non-content metric records retain only policy-required support-safe references.

**Given** any named payload or projection step fails, times out, returns unknown, or cannot prove its expected revision
**When** deletion status is calculated
**Then** the result remains a restrictive partial-failure state, success is forbidden, restart resumes idempotently from durable per-target outcomes, and no older completion masks the newest failure
**And** duplicate delivery cannot repeat erasure unsafely or recreate purged content.

**Given** an unauthorized Party or another tenant attempts to request, confirm, inspect, cancel, or replay deletion
**When** the command, status, UI, projection, or worker path executes
**Then** current authorization denies before protected-scope lookup, secret access, mutation, or disclosure
**And** content existence, hold state, projection membership, progress, tombstone, and failure details remain hidden.

**Evidence Manifest:**

| Field | Story 8.3 evidence |
| --- | --- |
| Requirements | FR18-FR24, FR28; NFR1-NFR6, NFR11, NFR13; UX-DR1, UX-DR9-UX-DR18, UX-DR29-UX-DR33, UX-DR36-UX-DR41, UX-DR46, UX-DR50; AD-2-AD-4, AD-8, AD-12, AD-13, AD-17, AD-20, AD-22, AD-23, AD-25, AD-26; EXT-PROTECTION-1; EXT-SECRETS-1 |
| OwnedClauses | FR18.terminal-history-preserved-after-protection; FR19.deletion-tenant-isolation; FR20.deletion-authorization-before-effect; FR23.deletion-status-contract; FR24.safe-deletion-audit; FR28.audit-governance-active; PRD-OQ8.cryptographic-erasure-or-redaction; PRD-OQ8.named-projection-purge; PRD-OQ8.safe-tombstone; NFR11.restart-no-duplicate-erasure; UX-DR31.DeletionRequest-lock-scope; UX-DR40.DeletionRequest-restrictive-viewport-block; UX-DR46.restrictive-partial-failure; AD-2.ProtectedDeletion; AD-22.two-role-approval-and-DEK-destruction; AD-23.explicit-deletion-inventory |
| Dependencies | Story 8.1; EXT-PROTECTION-1 and EXT-SECRETS-1 Available with accepted exact targets and passing compatibility commands |
| EvidenceLevel | Levels 2, 4, and 5: deletion state logic, live payload/projection components, and production-like erasure/purge proof |
| TestOrArtifact | ProtectedDeletionAggregateTests; CryptographicErasureIntegrationTests; NamedProjectionPurgeTests; DeletionRecoveryAndUiTests; deletion completion manifest and forensic no-content scan |
| VerificationCommand | pwsh ./eng/verify-story-8.3.ps1 |
| NegativeEvidence | ProtectedDeletionIsolationTests.CrossTenantRequestConfirmInspectAndReplayAreDenied; LegalHoldBypassTests; PartialProjectionFailureTests; DuplicateDeletionDeliveryTests; DeletedContentForensicScan |
| Result | Blocked — backlog; EXT-PROTECTION-1 and EXT-SECRETS-1 are Uncommitted and Story 8.1 is required |

### Story 8.4: Operate Safety Cost And Governance Policies

As an authorized Agent governance administrator,
I want to publish and inspect versioned safety, cost, and governance policy changes through one consistent high-impact command model,
So that future Agent Calls use explicit current controls and no UI or API path can weaken active protections.

**Primary Demonstrable Outcome:** One authorized policy publication becomes an immutable future-only version through EventStore and is rendered authoritatively across API/UI, while weaker, ambiguous, stale, unauthorized, or unsafe publication attempts have no runtime effect.

**Dependencies:**

- **Prior stories:** 5.3 and 5.5 for Provider/pricing/readiness contracts, 6.3 for enforced live safety, and 6.4 for hard budget reservation behavior.
- **External:** No new external commitment; live adapter behavior remains proven by the dependencies declared in Stories 6.3 and 6.4.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** an authorized policy administrator authors a Content Safety Policy
**When** validation and publication succeed
**Then** platform-scoped `ContentSafetyPolicy(system)` appends a new immutable policy version defining prompt/context and output gates, fixed always-blocked categories, explicitly permitted restricted handling, failure/audit treatment, no Approver override, future-only effect, and no-weaker retry
**And** safety configuration is removed from `Agent`; each interaction snapshots the platform/tenant policy version pair and retains a safety high-water mark at least as restrictive as its initial pair.

**Given** an authorized tenant governance administrator publishes tenant restrictions
**When** validation succeeds
**Then** `TenantGovernancePolicy(TenantId)` owns only stricter safety restrictions, monthly/per-call caps, rate limits, calling restrictions, concurrency limit, and tenant kill switch
**And** `Agent` retains only proposal expiry, regeneration ceiling, context policy, response mode, and approver configuration.

**Given** an authorized tenant budget administrator publishes monthly and per-call controls
**When** the command is accepted
**Then** the version records numeric caps, currency/unit and pricing basis, 80% warning, 100% block, reservation/reconciliation/retry-reuse policy, effective time, actor, and evidence reference
**And** missing, nonnumeric, incompatible, or indeterminate values fail validation and cannot become active.

**Given** any safety, budget, retention, export, deletion, or other high-impact policy command is submitted through API or UI
**When** EventStore accepts or rejects it
**Then** API and UI share the same authorization and additive public contract, accepted work renders `submitted -> authoritative pending -> projection-confirmed terminal`, and advisory locking is scoped to `(user session, resource identity, operation family)`
**And** unrelated resources and operation families remain available while concurrency/idempotency remain authoritative.

**Given** a policy version is changed while interactions exist
**When** a later runtime step evaluates current gates
**Then** changes affect only future calls except that current Provider, safety, cost, authorization, and governance state may tighten or block a not-yet-completed effect without retargeting stored provenance
**And** no retry, approval, regeneration, or posting path can select a weaker policy than its durable high-water mark.

**Given** an unauthorized Party, revoked role, or other tenant attempts to create, publish, inspect, or replay a governance policy
**When** any admin UI, API/client, command, projection, or audit path executes
**Then** current authorization denies before policy disclosure or mutation
**And** policy content, category configuration, tenant usage, budget values, secret references, actor, and failure detail remain hidden.

**Evidence Manifest:**

| Field | Story 8.4 evidence |
| --- | --- |
| Requirements | FR4-FR7, FR19-FR28; NFR1-NFR7, NFR10, NFR13; UX-DR1-UX-DR5, UX-DR9-UX-DR18, UX-DR23, UX-DR26, UX-DR29-UX-DR33, UX-DR36-UX-DR46, UX-DR50; AD-2-AD-5, AD-8-AD-10, AD-12, AD-13, AD-17, AD-20-AD-22, AD-25, AD-26 |
| OwnedClauses | FR26.versioned-policy-publication; FR26.always-blocked-and-restricted-rules; FR26.future-only-and-no-weaker-retry; FR27.no-override-policy-use; FR20.policy-admin-authorization; FR22.policy-authoring-parity; FR24.policy-change-audit; FR28.audit-governance-active; NFR7.active-safety-governance; NFR10.numeric-cap-policy; UX-DR31.PolicyPublication-and-TenantBudgetUpdate-lock-scope; UX-DR40.policy-and-budget-restrictive-viewport-block; UX-DR43.safety-authoring; UX-DR44.cost-authoring; AD-2.ContentSafetyPolicy-and-TenantGovernancePolicy; AD-12.advisory-command-scope; AD-13.current-gates-may-tighten |
| Dependencies | Stories 5.3, 5.5, 6.3, and 6.4; no new external dependency |
| EvidenceLevel | Levels 2 and 4: policy/concurrency logic and live EventStore/API/UI component evidence over already-proven runtime seams |
| TestOrArtifact | ContentSafetyPolicyAggregateTests; TenantGovernancePolicyAggregateTests; AgentSafetyConfigurationRemovalTests; GovernancePolicyApiUiParityTests; PolicyHighWaterMarkTests; governance-policy evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-8.4.ps1 |
| NegativeEvidence | GovernancePolicyIsolationTests.CrossTenantCreatePublishInspectAndReplayAreDenied; RevokedPolicyAdminTests; WeakerRetryAndApproverOverrideTests; InvalidBudgetPolicyTests; ConflictingPendingCommandTests |
| Result | Not run — backlog; requires Stories 5.3, 5.5, 6.3, and 6.4 |

### Story 8.5: Calculate Runtime And Product Metrics Deterministically

As a release operator,
I want versioned deterministic runtime and product metric calculations,
So that evidence is classified consistently without treating formula fixtures as proof of live attainment.

**Primary Demonstrable Outcome:** Approved fixtures deterministically calculate every NFR-9 and SM-1–SM-7 result, including insufficiency, pre-enablement versus launch-health classification, window, cohort, percentile, and late-data behavior, while explicitly producing no live READY claim.

**Dependencies:**

- **Prior stories:** 5.5 for readiness-record contracts and the authoritative runtime/product source events emitted by Stories 6.1–7.6.
- **External:** `EXT-TOPOLOGY-1` must be `Available` for live production-like source observations and qualification-session evidence; it is currently `Uncommitted`. Pure calculator fixtures do not bypass this live-evidence dependency for story completion.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** a versioned metric contract
**When** a runtime or product metric is calculated
**Then** the contract identifies authoritative source events/timestamps, numerator and denominator or percentile method, exact sample/window/cohort rules, late/missing-data handling, configuration version, and `InsufficientEvidence` conditions
**And** duplicate source observations are idempotent while conflicting duplicates invalidate the affected result.

**Given** approved runtime-latency fixtures
**When** NFR-9 calculators run
**Then** they evaluate automatic accepted-call-to-post and confirmation accepted-call-to-proposal at p95 <= 60 seconds and p99 <= 120 seconds, approval-to-post at p95 <= 10 seconds and p99 <= 30 seconds, and known pre-Provider rejection at p95 <= 2 seconds
**And** each gate requires at least 30 qualifying production-like executions and otherwise returns `InsufficientEvidence`.

**Given** approved product-metric fixtures
**When** SM-1 through SM-7 calculators run
**Then** SM-1, SM-4, SM-5, and SM-6 are classified as pre-enablement gate metrics; SM-2 enforces at least 20% of at least 50 eligible Conversations over a rolling 30-day window; SM-3 enforces at least 80% human decision within the lesser of configured expiry and 24 hours over every proposal, posting failure at most 2%, and audit completeness exactly 100%; and SM-7 enforces a 10–60% edited, regenerated, or rejected share among human-decided proposals
**And** SM-2, SM-3, and SM-7 are launch-health metrics reviewed only after enablement, expiry share is tracked by SM-C5, blocked-call share by SM-C4, and SM-C1–SM-C5 are never optimized away.

**Given** a deterministic calculator fixture passes
**When** its result is published to `runtime-metrics` or `product-metrics`
**Then** it proves formula implementation only and is visibly distinct from a real rolling-window/cohort Level 4/5 observation
**And** fixture, synthetic, missing-session, stale-contract, insufficient-sample, or unattested data cannot create a live attainment Pass or an `RQ-1` decision.

**Given** a different tenant or unauthorized Party queries source observations, cohorts, or metric details
**When** metric ingestion, calculation, projection, API, UI, or evidence access executes
**Then** tenant authorization denies before disclosure or aggregation across unauthorized scope
**And** low-volume cohort data, content, Party identity, prompt/context, proposal data, and safe-denial details cannot be reverse inferred.

**Evidence Manifest:**

| Field | Story 8.5 evidence |
| --- | --- |
| Requirements | FR8-FR25, FR28; NFR1, NFR2, NFR4, NFR5, NFR9, NFR11; SM-1-SM-7, SM-C1-SM-C5; UX-DR9-UX-DR12, UX-DR25-UX-DR30, UX-DR45; AD-3, AD-4, AD-8, AD-17, AD-22-AD-26; EXT-TOPOLOGY-1 |
| OwnedClauses | FR25.runtime-and-product-metric-status; FR28.fixed-metric-and-latency-controls; PRD-section11.versioned-measurement-contract; PRD-section11.deterministic-fixture-not-live-attainment; NFR9.all-four-threshold-families-and-30-sample-minimum; SM1.active-tenant-adoption-pre-enable; SM2.20-percent-50-conversation-30-day-launch-health; SM3.80-percent-human-decision-within-expiry-or-24h-launch-health; SM4.zero-successful-unauthorized-actions-pre-enable; SM5.complete-post-audit-links-pre-enable; SM6.admin-api-parity-pre-enable; SM7.substantive-review-band-launch-health; AD-24.metric-source-and-insufficiency-rules |
| Dependencies | Story 5.5 and source events from Stories 6.1-7.6; EXT-TOPOLOGY-1 Available for live observations |
| EvidenceLevel | Level 2 proves calculators; Levels 4 and 5 are required separately for live production-like source observations and cannot be inferred from fixtures |
| TestOrArtifact | RuntimeMetricCalculatorTests; ProductMetricCalculatorTests; WindowCohortLateDataTests; MetricInsufficiencyTests; versioned measurement-contract and fixture manifests |
| VerificationCommand | pwsh ./eng/verify-story-8.5.ps1 |
| NegativeEvidence | MetricIsolationTests.CrossTenantSourceCohortAndDetailAccessAreDenied; FixtureCannotProduceLivePassTests; InsufficientSampleAndMissingTimestampTests; ConflictingDuplicateObservationTests; CounterMetricGuardTests |
| Result | Blocked — backlog; EXT-TOPOLOGY-1 is Uncommitted and prior runtime/product source events are required |

### Story 8.6: Prove Accessible Localized Responsive And Performant UI

As a release operator and UX owner,
I want dedicated production-like conformance and browser-timing evidence for every completed interactive V1 surface,
So that accessibility, English/French parity, responsive safety, and interaction performance are proven without hiding missing routes, samples, or timestamps.

**Primary Demonstrable Outcome:** One versioned qualification run covers every interactive V1 route and high-impact state and produces separate `LR-UI-CONFORMANCE` and `LR-UI-PERFORMANCE` evidence, returning `InsufficientEvidence` for any missing route, locale key, viewport, tick, correlation, or sample.

**Dependencies:**

- **Prior stories:** The completed interactive routes and high-impact states from Stories 5.2–5.7, 6.7, 7.1–7.6, and 8.1–8.4.
- **External:** `EXT-TOPOLOGY-1` must be `Available` with the versioned production-like browser fixture, authenticated qualification sessions, and safe evidence ingress; it is currently `Uncommitted`.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** the versioned V1 route/state inventory
**When** the NFR-13 conformance suite executes through the same public contracts used by API clients
**Then** every interactive route and high-impact state proves WCAG 2.2 AA behavior, skip links, landmarks, semantic labels/table relationships, keyboard/focus order, non-hover access, safe dialog escape/focus return, localized live regions, reduced-motion behavior, and color-plus-icon-plus-text status
**And** all surfaces inherit FrontComposer and Fluent UI Blazor V5 without conditional skips or custom semantics that weaken the spine.

**Given** the OperationGateMatrixVersion 2 operation-family inventory
**When** route/state and parity coverage is enumerated
**Then** it covers `ProviderCatalogMutation`, `AgentSetupMutation`, `AgentActivation`, `AgentCallAcceptance`, `ProviderInvocation`, `ConversationPosting`, `ProposalResolution`, `PolicyPublication`, `TenantBudgetUpdate`, `LegalHold`, `ExportRequest`, `DeletionRequest`, `ReadinessInspection`, `ProposalEdit`, `ProposalRegeneration`, `SystemTimer`, `LegalHoldRelease`, `ExportDownload`, `AuditInspection`, `TenantKillSwitch`, `ReadinessObservation`, and `TenantProviderEnablement`
**And** no v2 family, exact `ScopeKind`, producer rule, route, or high-impact state can be omitted or covered only by an older matrix fixture.

**Given** English and French resources and supported desktop, tablet, phone, and wide-desktop profiles
**When** localization and responsive suites run
**Then** every label, state, denial, expiry, action, and announcement has whole-string key parity with named placeholders, tablet/desktop layouts preserve decision context, and phone/lightweight review follows the declared restrictions
**And** ProposalResolution, PolicyPublication, TenantBudgetUpdate, LegalHold, ExportRequest, and DeletionRequest fail closed with a visible localized reason at the most restrictive viewport whenever required context cannot be presented safely.

**Given** an authenticated `EXT-TOPOLOGY-1` qualification session and one injected browser-monotonic clock origin
**When** timing evidence is collected
**Then** `PageUsability` contains only `NavigationStartedTick` and `PageUsableTick`; `AuthoritativePending` contains only `CommandSubmittedTick` and `AuthoritativePendingRenderedTick`; and `TerminalRenderAnnouncement` contains only `AuthoritativeTerminalReceivedTick`, `TerminalRenderedTick`, and post-render-commit `LiveRegionAnnouncedTick`
**And** each deterministic SampleId is derived from qualification session, execution, and kind, exact duplicates are idempotent, conflicting duplicates are rejected, and safe trace/projection references correlate to server evidence without content or PII.

**Given** qualifying production-like browser samples
**When** `browser-ui-metrics` calculates NFR-14
**Then** authorized page usability is p95 <= 2.5 seconds, authoritative pending render is p95 <= 500 milliseconds, and terminal render plus announcement-ready DOM mutation is p95 <= 2 seconds using the later terminal tick
**And** each sample kind has at least 30 qualifying executions; missing/forbidden ticks, mixed clocks, absent sessions/references/live-region mutation, failed correlation, stale contracts, or too few samples yields `InsufficientEvidence`, never Pass.

**Given** an unauthorized route, command, status, proposal, governance, audit, or evidence request, including a different tenant
**When** the conformance suite exercises UI and API parity
**Then** denial occurs before sensitive rendering or side effects, is keyboard reachable and announced as a safe localized whole string, and no optimistic pending or success state is invented
**And** focused cross-tenant denial results are attached per affected route/operation rather than inferred from a broad browser or build suite.

**Evidence Manifest:**

| Field | Story 8.6 evidence |
| --- | --- |
| Requirements | FR19-FR25, FR28; NFR1, NFR2, NFR4, NFR13, NFR14; UX-DR1-UX-DR50; AD-8, AD-12, AD-17, AD-20, AD-22, AD-24-AD-26; EXT-TOPOLOGY-1; LR-UI-CONFORMANCE; LR-UI-PERFORMANCE |
| OwnedClauses | FR22.NFR13-and-NFR14-launch-evidence; FR23.UI-API-contract-parity; FR28.normative-Level4-5-UI-evidence; NFR13.WCAG2.2AA-every-interactive-route-and-state; NFR13.whole-string-EN-FR-parity; NFR13.restrictive-viewport-high-impact-block; NFR14.page-p95-2.5s; NFR14.pending-p95-500ms; NFR14.terminal-render-announcement-p95-2s; NFR14.30-per-kind-and-InsufficientEvidence; UX-DR33-UX-DR41.accessibility-responsive-contract; UX-DR45.readiness-rendering; UX-DR49.kind-discriminated-browser-samples; UX-DR50.authoritative-truth-flows; AD-17.complete-matrix-v2-family-conformance; AD-24.browser-ingress-and-sample-contract |
| Dependencies | Completed interactive routes/states from Stories 5.2-8.4; EXT-TOPOLOGY-1 Available with authenticated browser qualification fixture |
| EvidenceLevel | Levels 4 and 5: live component/browser conformance and production-like cross-system timing evidence; deterministic component checks alone cannot close the story |
| TestOrArtifact | V1RouteStateInventory; Wcag22AaBrowserSuite; EnglishFrenchParityTests; RestrictiveViewportMatrix; BrowserMonotonicTimingSuite; UI conformance/performance evidence manifests |
| VerificationCommand | pwsh ./eng/verify-story-8.6.ps1 |
| NegativeEvidence | UiIsolationQualification.CrossTenantDenialPerAffectedRouteAndOperation; MissingRouteLocaleViewportTests; ForbiddenOrMissingTickTests; MixedClockAndFailedCorrelationTests; ConflictingBrowserSampleTests; ConditionalSkipGuard |
| Result | Blocked — backlog; EXT-TOPOLOGY-1 is Uncommitted and completed interactive routes through Story 8.4 are required |

### Story 8.7: Inspect Launch Evidence And Blockers

As an authorized release operator,
I want to inspect the current launch-readiness registry and its safe blockers,
So that I can identify missing, stale, insufficient, or failing evidence without turning inspection into an umbrella implementation or release decision.

**Primary Demonstrable Outcome:** One tenant/profile inspection returns a checkpoint-consistent view of all 18 required gate records, consumed dependency status, evidence levels, freshness, and safe blockers, while never filling a missing record, falling back to an older Pass, or issuing the `RQ-1` decision.

**Dependencies:**

- **Prior stories:** 5.5 for authoritative readiness records and the bounded evidence/status outputs of Stories 5.1–8.6.
- **External:** `EXT-TOPOLOGY-1` must be `Available` for production-like evidence references and inspection of the qualification profile; it is currently `Uncommitted`.
- **Forward dependencies:** None. `RQ-1` consumes this and other completed evidence later; Story 8.7 does not depend on or execute `RQ-1`.

**Acceptance Criteria:**

**Given** an authorized tenant scope and environment profile
**When** launch evidence is inspected
**Then** the response uses one `launch-readiness` projection checkpoint and lists every minimum GateId with State, Owner, SourceVersion, ObservedAt, exclusive ValidUntil, RequiredEvidenceLevel, EvidenceReference, current configuration/measurement contract, and safe BlockerCode
**And** the UI distinguishes `Pass`, `Block`, `InsufficientEvidence`, and `Stale` with current registry revision and never treats active lifecycle, host health, narrative evidence, or lower-level evidence as readiness.

**Given** the readiness aggregate contains multiple observations for a logical key
**When** the projection and inspection response are built
**Then** the observation at the greatest committed stream revision is authoritative regardless of ObservedAt, an incomplete/invalid/stale/blocking newest record cannot fall back to an older Pass, and exact duplicate ObservationId is idempotent while a conflicting duplicate is rejected and audited
**And** a checkpoint change causes one retry against a new single checkpoint or a safe non-ready inspection result.

**Given** any consumed critical dependency is Uncommitted/unavailable or a story evidence reference is missing, stale, lower-level, skipped, placeholder, conditional, or contract-incompatible
**When** blockers are calculated for inspection
**Then** the exact consuming gates/stories and safe blocker codes remain visible, Story 6.5 capacity/fairness evidence and Story 8.6 UI evidence stay separately owned, and deterministic Story 8.5 fixtures remain formula evidence rather than live attainment
**And** Story 8.7 creates no substitute test, implementation, observation, Pass record, metric attainment, or release decision.

**Given** all inspected records appear Pass
**When** an operator views the result
**Then** the surface states only that the current registry inspection has no visible blocker at its checkpoint and directs final eligibility to release gate `RQ-1`
**And** it never labels production enabled, READY, or released unless a separate current `RQ-1` report already records that decision.

**Given** an unauthorized Party or another tenant attempts to inspect gate state, evidence references, metrics, blockers, or dependency detail
**When** API, client, UI, projection, or evidence retrieval executes
**Then** current tenant/release authorization denies before record or cohort disclosure
**And** tenant existence, configuration, dependency target, evidence location, metric cohort, content, and failure detail remain hidden.

**Evidence Manifest:**

| Field | Story 8.7 evidence |
| --- | --- |
| Requirements | FR19-FR25, FR28; NFR1-NFR14; UX-DR1, UX-DR2, UX-DR9-UX-DR14, UX-DR20-UX-DR30, UX-DR33, UX-DR36, UX-DR39-UX-DR41, UX-DR45, UX-DR47-UX-DR50; AD-8, AD-10, AD-17, AD-22-AD-26; EXT-CONV-AI-1, EXT-CONV-UI-1, EXT-HOST-1, EXT-PROVIDER-1, EXT-SAFETY-1, EXT-TOKEN-1, EXT-SECRETS-1, EXT-PROTECTION-1, EXT-TOPOLOGY-1; LR-TOPOLOGY through LR-PRODUCT-METRICS |
| OwnedClauses | FR25.authoritative-launch-status-and-safe-blockers; FR28.inspect-fixed-controls-dependencies-and-evidence; launch-register.all-18-gates-at-one-checkpoint; launch-register.greatest-committed-revision-no-fallback; launch-register.missing-stale-block-insufficient-remain-blockers; launch-register.dependency-status-visible; NFR1-NFR14.evidence-inspection-only-not-implementation; UX-DR45.Pass-Block-InsufficientEvidence-Stale; UX-DR45.exact-gates-levels-samples-and-safe-blockers; AD-17.registry-schema-inventory-freshness; AD-22.single-readiness-writer; AD-26.RQ1-remains-separate |
| Dependencies | Story 5.5 and bounded outputs from Stories 5.1-8.6; EXT-TOPOLOGY-1 Available for production-like inspection |
| EvidenceLevel | Levels 2, 4, and 5: registry evaluation logic plus inspection of existing live component and production-like evidence; the story does not manufacture those levels |
| TestOrArtifact | LaunchEvidenceInspectionContractTests; RegistryCheckpointConsistencyTests; ReadinessBlockerUiTests; DependencyAndEvidenceClassificationTests; inspection evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-8.7.ps1 |
| NegativeEvidence | LaunchInspectionIsolationTests.CrossTenantGateEvidenceMetricAndBlockerAccessAreDenied; OlderPassFallbackTests; MissingGateAndDependencyTests; FixturePromotedToLivePassTests; InspectionCreatesNoObservationOrRQ1DecisionTests |
| Result | Blocked — backlog; EXT-TOPOLOGY-1 is Uncommitted and bounded evidence through Story 8.6 is required |

### Story 8.8: Inspect Audit Evidence Under Durable Compliance Governance

As a Compliance Inspector,
I want scoped inspection of protected Agent evidence recorded as its own governed case,
So that sensitive content can be examined without granting ambient Conversation participation.

**Primary Demonstrable Outcome:** One authorized inspection case discloses only the permitted posted provenance or protected unposted evidence, records justification and independent oversight durably, and remains auditable after source deletion or inspected-content erasure.

**Dependencies:**

- **Prior stories:** 5.4 for trusted principals, 5.8 for protected content, 7.1 for proposal evidence, and 8.1 for hold-aware audit governance.
- **External:** `EXT-PROTECTION-1` and `EXT-TOPOLOGY-1` must be `Available` for live disclosure and production-like isolation/review evidence.
- **Forward dependencies:** None. `RQ-1` may consume its bounded evidence but Story 8.8 does not execute the release gate.

**Acceptance Criteria:**

**Given** a current Participant with Source Conversation read access
**When** posted provenance is inspected
**Then** the public contract returns only the posted Message provenance allowed by current Conversation authorization
**And** unposted versions, rejected or failed content, context metadata, and protected payloads remain undisclosed.

**Given** a request for unposted content or context metadata
**When** disclosure is evaluated
**Then** it requires either the Party durably recorded as Eligible Approver for that proposal or `AuditInspection(TenantId, InspectionId)` scoped to a named Conversation or case with a nonblank justification
**And** the compliance path records a distinct Tenant Agent Administrator's pre-approval before content read or a required post-hoc review within the configured tenant window.

**Given** an AuditInspection is accepted
**When** content reads and review transitions occur
**Then** the aggregate owns request, scope, justification, approval/review state, rate accounting, every disclosure reference, and terminal outcome at expected revisions
**And** inspection rate and overdue review status are visible to Tenant Agent Administrators while protected content remains sealed until the authorized read boundary.

**Given** the Source Conversation is later deleted or inspected content is cryptographically erased
**When** audit evidence is replayed
**Then** the inspection's support-safe self-audit, actors, scope, justification, approval/review, rate, disclosure references, and terminal outcome survive
**And** erased content stays typed `Erased` and cannot be revived by the inspection record.

**Given** missing, stale, cross-tenant, wrong-role, unapproved, over-rate, wrong-scope, or forged principal evidence
**When** inspection is attempted
**Then** it fails closed before unprotection or disclosure and appends content-free security evidence
**And** API, UI, projection, and live tests prove the identical safe result and persisted end state.

**Evidence Manifest:**

| Field | Story 8.8 evidence |
| --- | --- |
| Requirements | FR19-FR25, FR28; NFR1-NFR7, NFR11, NFR13; UX-DR9-UX-DR18, UX-DR29-UX-DR41, UX-DR46, UX-DR50; AD-2, AD-8, AD-12, AD-14, AD-17, AD-22, AD-27, AD-30; EXT-PROTECTION-1; EXT-TOPOLOGY-1 |
| OwnedClauses | FR20.audit-inspection-authorization; FR23.audit-inspection-public-contract; FR24.inspection-self-audit; NFR2.protected-evidence-disclosure; AD-2.AuditInspection; AD-22.two-level-inspection-and-surviving-evidence; AD-30.compliance-principal; matrix-v2.AuditInspection |
| Dependencies | Stories 5.4, 5.8, 7.1, and 8.1; EXT-PROTECTION-1 and EXT-TOPOLOGY-1 Available |
| EvidenceLevel | Levels 2, 4, and 5: aggregate/authorization behavior, live protected disclosure, and production-like isolation/review proof |
| TestOrArtifact | AuditInspectionAggregateTests; AuditInspectionApiUiParityTests; AuditInspectionLiveTests; InspectionReviewWindowTests; inspection evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-8.8.ps1 |
| NegativeEvidence | AuditInspectionIsolationTests.CrossTenantWrongRoleUnapprovedAndOverRateReadsAreDenied; ForgedCompliancePrincipalTests; ErasedContentCannotBeRevivedTests; InspectionSurvivesSourceDeletionTests |
| Result | Blocked — backlog; EXT-PROTECTION-1 and EXT-TOPOLOGY-1 are Uncommitted |

## Release Gate RQ-1 — Outside The Story Backlog

`RQ-1` is a non-estimated operational release gate, not an epic or story and not an implementation umbrella. It runs only after Epics 5–8 are complete, every consumed critical dependency is `Available`, and controlled production-like qualification has produced current qualifying Levels 4 and 5 evidence.

For one authorized `TenantScope`, versioned `EnvironmentProfile`, and single `launch-readiness` projection checkpoint, `RQ-1` evaluates all 18 minimum GateIds, real NFR-9/NFR-14 samples, pre-enablement SM-1/SM-4/SM-5/SM-6 qualification evidence, audit completeness, evidence freshness and contracts, and unresolved blockers. It returns a new dated READY or NOT READY report. SM-2/SM-3/SM-7 remain post-enablement launch-health metrics and are not `RQ-1` inputs. Any missing, stale, blocked, insufficient, lower-level, skipped, placeholder, conditional, contract-incompatible, or unavailable-dependency input yields NOT READY; deterministic calculator fixtures never prove live attainment. The gate aggregates completed evidence only and owns no missing source, package, runtime, governance, capacity, UI, test, or remediation implementation.
