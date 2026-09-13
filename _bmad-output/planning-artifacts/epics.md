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
  - /home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-09-prd-validation-follow-through.md
  - /home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-09-2.md
---

# Hexalith Agents - Epic Breakdown

## Overview

This document provides the complete epic and story breakdown for Hexalith Agents, decomposing the requirements from the PRD, UX Design if it exists, and Architecture requirements into implementable stories.

## Replacement Authority — 2026-09-09

The approved `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-09.md`, its approved PRD-validation follow-through proposal, approved `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/sprint-change-proposal-2026-09-09-2.md`, current PRD, Architecture Spine, UX spines, external-dependency register, launch-readiness register, and active Epics 5–8 are the machine-visible implementation authority. The 2026-08-02 proposal remains historical replacement evidence where it does not conflict with this correction. The second proposal's superseded 29-story count is historical; the later approved additions 5.9, 5.10, 6.8, and 7.7 remain active, for exactly 33 stories.

Epics 1–4 and every completed story beneath them remain unchanged historical delivery evidence with status `completed`. Their completion proves only the evidence recorded at the time; it does not establish live production conformance, current callability, or release readiness. Any conflicting historical criterion is `mustNotImplement` and resolves through the replacement-authority map in `epic-5-superseded-2026-08-01.md` without rewriting the completed story text.

The former 18-story Epic 5 is superseded and non-executable. Active forward work consists only of replacement Epics 5–8 and exactly 33 stories. `RQ-1` remains a non-estimated release gate outside the story backlog; it aggregates completed evidence and never owns missing implementation.

## Requirements Inventory

### Functional Requirements

FR1: The Platform Operator provisions `hexa` once per tenant at tenant enablement through an idempotent create-only operation that establishes tenant scope and an immutable Party identity owned by the Agents Service Principal and verified by id; `EXT-PARTIES-1` selects either Branch A with additional AI Party-type creation/verification or Branch B with the Product/Parties-approved Organization identity-by-id contract. Tenant Agent Administrators configure and activate `hexa`, and no tenant role may create a second Agent, delete it, or change its Party identity.

FR2: The provisioned Agent Party identity lets `hexa` participate as a known AI member; `ConversationAgentState` uses `NeverJoined`, `Joined`, `ExternallyRemoved`, `Blocked`, and `ReadmitPending`, and one versioned `CurrentMirror(BlockVersion, Direction, Outcome, AttemptId)` whose mutually exclusive current-outcome derivations are `MirrorPending` and `MirrorRefused`. Direction-aware remove/readmit mirroring is at least once, successor attempts supersede lower versions, and identity, membership, access, or block uncertainty prevents silent rejoin or posting.

FR3: Agent Administrators can activate, disable, and inspect `hexa` lifecycle state; disabled Agents cannot be called, disabling preserves prior evidence and messages, and lifecycle changes are auditable and visible through admin UI and API/client contracts.

FR4: Authorized administrators can configure the Global Providers Aggregate with provider records, model options, enabled/disabled state, and provider capability metadata; disabled providers/models cannot be selected for new active use, existing Agents using disabled providers/models cannot be activated or called until reconfigured, and provider changes are auditable without secret exposure.

FR5: Agent Administrators can select a Provider and model for `hexa` from the Global Providers Aggregate; selected provider/model state is validated before activation, enough provider/model identity is retained for audit, and selection changes affect only future Agent Calls.

FR6: Agent Administrators can configure `hexa` for Automatic Response Mode or Confirmation Response Mode; automatic mode posts successful responses directly after authorization and generation, confirmation mode creates Proposed Agent Replies outside the Conversation, and response mode changes affect only future Agent Calls.

FR7: Tenant Agent Administrators configure Approver Policy from Conversation Facilitator, predefined Parties, and tenant roles; `Caller` is deprecate-and-reject. One Eligible Approver predicate — current participant/read access, policy-resolved, not caller, not last editor, and not the requester of a regenerated version under decision — applies at configuration, call, edit, regeneration, approval, and scheduled re-check. Empty or unavailable resolution uses the typed PRD outcomes, and an edit or regeneration that would leave no other eligible Approver is refused before changing content or invoking a Provider.

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

FR18: A proposal uses exactly the ten recorded states `Pending`, `Edited`, `Regenerated`, `Approved`, `PostingPending`, `PostingFailed`, `Posted`, `Rejected`, `Abandoned`, and `Expired`; only the last four are terminal. `PostingPending` is uninterruptible, approval freezes expiry, retries are bounded and audited, removal may system-abandon `Approved` or `PostingFailed`, and the `MessageId` read produces `LateConfirmed` before any retry or exit from `PostingFailed`.

FR19: The system enforces tenant isolation across Agent configuration, Provider selection, Agent Calls, Conversation Context, Proposed Agent Replies, Conversation posting, and Audit Evidence; cross-tenant call/inspect/approve/post actions are impossible, provider and Agent configuration does not leak across tenants unless explicitly platform-scoped and authorized, and audit/status queries return only tenant-authorized records.

FR20: The system enforces authorization for Agent administration, provider administration, Agent calling, proposal discovery, editing, regeneration, approval, rejection, abandonment, posting, and audit inspection; authorization failures happen before provider invocation or Conversation posting, admin UI and API/client contracts use the same rules, and authorization decisions are auditable without sensitive leakage.

FR21: Dependency uncertainty fails closed. `Uncommitted` blocks consuming stories from `ready-for-dev`; completed consumption is recorded as non-conformance; `Committed` permits contract work only; runtime, test, or qualification seam execution requires `Available` and its exact compatibility command, otherwise `DependencyNotAvailable`; content-bearing paths additionally fail with `PayloadProtectionUnavailable` under FR34.

FR22: The admin UI allows authorized administrators to manage Global Providers Aggregate entries, configure `hexa`, inspect lifecycle state, configure response and approver policy, and view Agent operation/proposal status; UI actions enforce the same authorization rules as API/client contracts, never expose Provider secrets, distinguish active, disabled, invalid, pending proposal, failed call, and expired proposal states, and satisfy NFR-13 accessibility/localization/responsive safety plus NFR-14 interaction-performance evidence.

FR23: The system exposes stable API/client contracts for Provider administration, Agent configuration, Agent Calls, proposal workflow, status inspection, and audit inspection; callers are not required to use raw EventStore, internal aggregate, internal projection, or Provider SDK details; responses are structured for automation; JSON object evolution is additive within V1; public enums define `Unknown = 0` and may add but never reuse values; no public member or enum value is removed, renamed, or semantically reused within V1; and any breaking public change requires a new major package/API version plus package-consumer compatibility tests.

FR24: The system captures Audit Evidence for Agent configuration, Provider/model configuration, Agent Calls, generation attempts, proposal versions, edits, regenerations, approvals, rejections, abandonments, expirations, automatic posts, and final Conversation Messages; posted responses trace back to caller/Agent/source/provider/model/content/approval path, proposals preserve all versions, policy outcomes and identifiers are recorded where available, and audit is tenant-authorized without leaking unrelated tenant data or Provider secrets.

FR25: The system exposes status for Agent readiness, Provider/model readiness, recent Agent Call outcomes, proposal queues, generation failures, approval completion, and posting outcomes; authorized administrators can identify whether `hexa` is callable, distinguish key failure classes, and monitor launch adoption and approval workflow metrics.

FR26: The Platform Operator with Security approval publishes the versioned Content Safety Policy; a Tenant Agent Administrator may add restrictions only. Production or production-like enablement requires the active policy, all four safety application points use current/no-weaker rules, and Approver override remains prohibited.

FR27: The system applies Content Safety Policy to prompt plus complete Conversation Context before Provider invocation and to generated output before any proposal or Conversation side effect; failed content cannot be posted or approved, safety failures create authorized status and Audit Evidence without forbidden disclosure, and Approvers cannot override failures.

FR28: V1 launch readiness requires fixed metric thresholds, latency targets, full-context behavior, hard cost controls, audit governance, NFR-11 through NFR-14, accepted external-dependency commitments, and normative Levels 4–5 evidence. Controlled production-like qualification may run only after its safety, context, cost, audit, and dependency gates are active; production enablement remains blocked until release gate `RQ-1` records READY. Cost reserves atomically before Provider invocation and reconciles actual usage; runtime latency gates use their exact p95/p99 thresholds with at least 30 production-like executions; lower evidence, skips, placeholders, and conditional results cannot establish production readiness.

FR29: Administrative writes distinguish `Submitted`, `AuthoritativePending`, and `ProjectionConfirmed`; accepted identities, projection versions, and freshness are shared by API/UI, and an unconfirmed projection is never presented as confirmed configuration or callability.

FR30: Public governance operations cover budget policy, Content Safety Policy publication, legal hold, export, approved deletion, compliance inspection, kill switch, and launch-readiness inspection with typed blockers; every operation enforces the FR-33 scope, justification, approval, audit, and fail-closed rules.

FR31: Agent Instructions are system-level authority and Conversation Context is untrusted data; content can neither alter configuration/authorization/governance nor assert a decision or approval on another behalf, and control-bypass attempts are recorded as safety outcomes.

FR32: Platform or Release Operators set hard numeric tenant caps, per-Party/per-Conversation rates, and concurrent-interaction bounds with no implicit defaults; Tenant Agent Administrators may only lower them and separately configure the per-proposal regeneration ceiling, while reservation, consumption, override, and refusal states remain auditable and visible.

FR33: Every authorization rule maps to the six roles Platform Operator, Tenant Agent Administrator, Approver, Conversation Participant, Compliance Inspector, and Release Operator at the exact PRD operation and scope; Conversation Facilitator is external authority for the block/clear rows, Security approval is a condition, and role basis is recorded.

FR34: Every content-bearing runtime path fails closed with `PayloadProtectionUnavailable` until the exact `EXT-PROTECTION-1` target passes the production protection attestation; the no-op default cannot enable generation or produce production-like evidence, and protected replay yields `Erased` after key destruction.

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
- Hexalith Agents is a full EventStore-backed domain module. The V1 durable aggregate inventory is `Agent`, platform-scoped `ProviderCatalog` in reserved tenant `system`, `TenantProviderEnablement`, `AgentInteraction`, `RateLimitLedger`, `OpenInteractionLedger`, `BudgetLedger`, `TenantGovernancePolicy`, `ConversationAgentState`, `AuditInspection`, routed `SecurityEventLog`, reserved-system `TrustedEnvelopeReplay`, platform-scoped `ContentSafetyPolicy`, `SafetyVerdictEpoch`, `SafetyVerdictIndex`, `LaunchReadinessGate`, reserved-system `ArchitectureDecisionCatalog` and `ArchitectureDecisionRecord`, `LegalHold`, `ProtectionFence`, `AuditExport`, and `ProtectedDeletion`. Dapr Workflow history, the content-free operational security-audit spool, and optional framework session state never become business truth.
- Aggregate handlers are pure and emit events only. Provider calls, Conversations reads/posts, Parties validation, Tenants projection reads, safety checks, admission, expiry timers, notifications, and secret access execute outside aggregates and return through deterministic commands.
- `AgentInteraction` snapshots Agent/configuration/instructions/response/approver/provider/model/context/safety/caller/source versions at request time, while current provider readiness and safety may tighten or block later steps without retargeting the interaction.
- Proposal content is append-only and immutable across generated, edited, and regenerated versions. Approval selects exactly one version; rejected, abandoned, and expired proposals cannot later post. Provider/timeout failures create content-free `GenerationFailed` status and Audit Evidence on `AgentInteraction`; output-safety failure uses the Product-selected `OD-INITIAL-OUTPUT-SAFETY-STATUS-1` status. Failed/incomplete bytes are not retained in the V1 live path and no live `GenerationFailureRecord` exists; any future retention requires Product approval and a separate non-approvable owner/permit/protection/deletion protocol.
- Conversations context, AI membership, and final posting use supported Conversations client/API seams only. Direct stream writes are forbidden, and a Proposed Agent Reply is never a Conversation Message.
- Agent identity stores stable Party references only. Posting requires one valid current Agent Party identity verified by immutable id under `EXT-PARTIES-1`'s selected branch, plus limited Conversations membership as `ParticipantType.AiAgent`/`AIAgent` and `ParticipantRole.Member` through `EXT-CONV-AI-1`; AI Party-type verification is additional only under Branch A.
- Approver authority resolves from the snapshotted Agents policy plus current predefined-Party, tenant-role, and Conversation Facilitator evidence. `ApproverPolicySourceKind.Caller` is retired and rejected wherever presented. Missing, stale, ambiguous, revoked, or unavailable authority fails closed.
- Provider SDK and credential details remain adapter-local. `ProviderReadinessResult` exposes exactly `OperationalState`, `Callability`, `ReasonCode`, `CapabilityVersion`, `CurrentDataHandlingVersion`, `InForceDataHandlingVersion`, optional exclusive `GraceExpiresAt`, `ObservedAt`, exclusive `ValidUntil`, and discriminated `Freshness`; only the three architecture-valid state triples are accepted, and unknown or invalid combinations block.
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
- `OperationGateMatrixVersion = 7` is the single additive gate mapping consumed by API, BFF, UI, workflow, replay admission, and readiness projection; version 7 retains the v6 target-aware bootstrap/repair, split kill-switch, replay-admission, closed governance-workflow, and deletion-integrity variants, adds stable pre-seal destruction identity and replacement-key activation arbitration while retaining protection-owner block/reserve/re-attestation activation, signed-but-unissued recovery, and the non-public compromise registrar, and forbids code-local exceptions. Every observation carries exact `ScopeKind` and an `AuthorizedProducer`; a missing family/variant, unknown/old version where current is required, invalid scope/producer, missing record, or non-inventory gate blocks. The Live-Seam Matrix names the integration test required for every live claim.
- The authoritative projection/scope inventory is: `agent-setup`, `provider-catalog`, `tenant-provider-enablement`, `agent-interaction-status`, `proposal-detail`, `proposal-version-history`, `pending-proposal-queue`, `pending-proposal-count`, `audit-evidence`, `rate-limit-usage`, `open-interaction-leases`, `budget-reservation-usage`, `safety-verdict-status`, content-free `safety-verdict-directory`, `retention`, `legal-hold`, `export`, `export-artifact-index`, `export-artifact-store`, `deletion`, `launch-readiness`, `runtime-metrics`, `browser-ui-metrics`, `workflow-execution-state`, and `product-metrics`; the register marks the two non-projection scope items explicitly.
- NFR-11 recovery freezes the pre-fault eligible nonterminal cohort, uses one monotonic clock origin, proves EventStore RPO 0, restores processing within 15 minutes, preserves terminal decisions, and inventories duplicate-sensitive external effects.
- NFR-12 evidence records all numeric concurrency/queue values and proves exact weighted fairness, fencing, no starvation, replica/crash/cancel/expiry recovery, and coexistence with cost caps in the `EXT-TOPOLOGY-1` fixture.
- NFR-13 evidence covers every interactive V1 route and high-impact state for WCAG 2.2 AA, keyboard/focus/semantic/live-region behavior, whole-string English/French parity, FrontComposer/Fluent V5 inheritance, and restrictive-viewport blocking.
- NFR-14 uses authenticated browser-monotonic, kind-discriminated samples correlated to safe server evidence; each sample kind requires at least 30 production-like executions and missing/invalid ticks, correlation, localized live-region mutation, or samples yields `InsufficientEvidence`.
- Public contracts are versioned and additive-first. Evidence Levels retain PRD meanings: Level 1 structure, Level 2 pure behavior, Level 3 fail-closed deferred seam, Level 4 live component, and Level 5 production-like cross-system evidence.
- The active stack baseline is the Architecture Spine's current Stack table: SDK `10.0.401` with `latestPatch`, `net10.0`, C# 14, `.slnx`, Central Package Management, FluentValidation `12.1.1`, OpenTelemetry `1.18.0`, Fluent UI Blazor `5.0.0-rc.5-26219.1`, xUnit v3 `3.2.2`, Shouldly `4.3.0`, and bunit `2.9.0`; Hosting plus Provider/Agent Framework SDKs remain unselected until their dependency records commit them, and MediatR `14.2.0` is catalog-visible but absent from the current Agents runtime graph. The imported Dapr .NET/Workflow `1.18.5` family may not be adopted while ARCH-A-15 is open: the catalog and host must upgrade the family atomically to upstream `1.18.7` or later with compatibility evidence, or Security must record the bounded exception ARCH-A-15 defines.
- The external-dependency register is the only commitment authority for all twelve records: `EXT-CONV-AI-1`, `EXT-CONV-UI-1`, optional `EXT-CONV-RETRACTION-1`, `EXT-HOST-1`, `EXT-PROVIDER-1`, `EXT-SAFETY-1`, `EXT-TOKEN-1`, `EXT-SECRETS-1`, `EXT-EXPORT-STORE-1`, `EXT-PROTECTION-1`, `EXT-TOPOLOGY-1`, and `EXT-PARTIES-1`; any `Uncommitted` record blocks launch evidence for its consuming stories. Optional retraction has no consumer unless Product selects OQ-23's branch. The register's `EXT-PARTIES-1` development allowance permits its fourteen direct human/identity consumers — Stories 5.2, 5.4, 5.5, 6.6, 7.1–7.5, 8.1–8.4, and 8.8 — to build against Branch-B-compatible identity-by-id behavior where applicable, but cannot establish human actor-binding, separation-of-duty, launch evidence, or `RQ-1` until the record is `Available` and its compatibility command passes.
- The launch-readiness register is the machine-testable callability and qualification authority. `RQ-1` is a non-estimated release gate outside the development backlog and currently returns NOT READY.

### UX Design Requirements

UX-DR1: Register an Agents domain/category in the FrontComposer shell, with authorization-safe navigation and links for Agents overview, `hexa` configuration, Provider catalog, Approver policy, Conversation context policy, Content Safety policy, cost controls, Conversation-owned invocation, Proposal queue/detail, Launch readiness, Operational status, Audit governance, and Audit evidence.

UX-DR2: Implement the Agents overview as the default Agents navigation surface showing `hexa` lifecycle separately from authoritatively proven callability, response mode, Provider/model, pending proposal count, recent failures, evidence freshness, and safe tenant blockers.

UX-DR3: Implement `hexa` configuration as a constrained FrontComposer/Fluent form for identity, display metadata, instructions, provider/model, response mode, approver policy, lifecycle, activation blockers, and safe configuration-change visibility.

UX-DR4: Implement Provider catalog as a full-width FrontComposer FC-TBL/FluentDataGrid surface showing provider/model options, enabled state, capability metadata, readiness, and secret configured/not-configured state without exposing secret values.

UX-DR5: Implement Approver policy builder rows for Conversation Facilitator authority (legacy `ConversationOwner` wire value), predefined Parties, and tenant roles only, with readable policy basis and blocked state for missing or ambiguous sources; the retired caller source is never offered and is rejected if supplied by an old client.

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

UX-DR31: Editing is explicit, regeneration is distinct, and approval applies only to a selected version. The advisory pending lock permits at most one command for `(user session, resource identity, operation family)` across every lock-bearing family in `OperationGateMatrixVersion = 7`: `ProposalResolution`, `PolicyPublication`, `TenantBudgetUpdate`, `LegalHold`, `ExportRequest`, `DeletionRequest`, `ProviderCatalogMutation`, `AgentSetupMutation`, `AgentActivation`, and `DataHandlingAcceptance`; unrelated resources/families remain available and EventStore concurrency/idempotency remain authoritative.

UX-DR32: Keyboard and focus behavior must support `Esc` closing transient UI without committing, focus returning to the trigger, approval/rejection controls being keyboard reachable, and no required action or denial reason being hover-only.

UX-DR33: Every interactive V1 route and high-impact state must meet WCAG 2.2 AA behavior using FrontComposer FC-A11Y primitives including skip links, focus visibility, named navigation landmarks, keyboard shell controls, semantic labels, and status live regions.

UX-DR34: Proposal queue, provider catalog, and audit/status grids must expose table semantics, header relationships, sort/filter state, and row action names.

UX-DR35: Proposal editor must be fully keyboard operable for edit, selected-version choice, metadata comparison, regeneration, approval, rejection, abandonment, and exit without committing.

UX-DR36: Live regions must announce generation failed, proposal created, proposal expired, authoritative `posted`, posting failed, and permission denied using localized whole strings after render commit; ordinary pending progress is not assertive and the evidence proves announcement-ready DOM state rather than speech completion.

UX-DR37: Focus-trapped dialogs or confirmation panels must provide a safe non-committing escape and return focus to the triggering control.

UX-DR38: Reduced-motion users must not depend on animation to perceive generation, approval, or posting state changes.

UX-DR39: Responsive behavior must be desktop-first; phone may support read-only status/proposal reference/lightweight review, tablet stacks metadata/editor/version history and prioritizes grid columns, desktop is the primary mode, and wide desktop uses extra width for split views rather than decoration.

UX-DR40: At the most restrictive supported viewport, every lock-bearing family in `OperationGateMatrixVersion = 7` is unavailable with a visible reason whenever required decision context cannot be presented safely; the shared matrix fixture proves the same behavior for all ten families and review-only access remains available.

UX-DR41: Use FrontComposer capabilities intentionally: FC-LYT for FullWidth and Constrained layouts, FC-TBL for grids/filter summaries/row detail/empty/error states, FC-A11Y for shell and custom override accessibility, FC-L10N for domain labels and workflow copy, policy-gated navigation for authorization-safe entry visibility, and pending command/status patterns for generation/approval/posting transitions without promoting pending to success.

UX-DR42: Implement a read-only Conversation context policy surface showing complete-context-or-blocked behavior, effective model budget, policy version, and safe blocking reason; expose no bounded-context control.

UX-DR43: Implement Content Safety authoring for fixed blocked categories, restricted handling, policy validation/version, and explicit authorized publish with future-only and no-weaker-retry copy.

UX-DR44: Implement cost-control authoring/status for monthly tenant budget, per-call caps, usage/reservations, 80% warning, 100% block, and indeterminate fail-closed state.

UX-DR45: Implement Launch readiness showing `Pass`, `Block`, `InsufficientEvidence`, and `Stale`; current `ObservedAt`/exclusive `ValidUntil`; exact NFR-9 and NFR-14 gates; SM-1/SM-4/SM-5/SM-6 gate results; SM-2/SM-3/SM-7 launch-health results reported, not gated; SM-C1..SM-C5; sample sufficiency; Levels 4–5; and all safe blockers. Lower evidence, skips, missing timestamps/references, invalid browser samples, and insufficient samples cannot render as Pass.

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

FR18: Epics 7 and 8 - Implement the exact ten-state proposal lifecycle (`Pending`, `Edited`, `Regenerated`, `Approved`, `PostingPending`, `PostingFailed`, `Posted`, `Rejected`, `Abandoned`, `Expired`), bounded `PostingFailed` recovery and `LateConfirmed`, and retention/hold governance that preserves every version and terminal decision.

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

FR29: Epics 5–8 - Carry `Submitted`, `AuthoritativePending`, and `ProjectionConfirmed` plus revision/freshness through every administrative write and UI/API surface.

FR30: Epics 5 and 8 - Expose typed launch-readiness blockers and governed budget, safety, hold, export, deletion, compliance-inspection, and kill-switch operations.

FR31: Epics 6 and 7 - Treat Conversation content as untrusted input, preserve instruction authority, and block control bypass or Party impersonation before side effects.

FR32: Epics 6 and 8 - Configure and enforce numeric caps, rate/concurrency bounds, reservation/reconciliation, regeneration ceiling, and audited override behavior.

FR33: Epics 5–8 - Enforce the six-role operation/scope matrix, current authority resolution, trusted principals, and role-basis audit across API, UI, workflow, and governance.

FR34: Epics 5–8 - Gate every content-bearing path on attested payload protection, preserve sealed execution boundaries, and support hold-safe cryptographic erasure.

### OQ-24..OQ-30 Coverage Map

| OQ | Story references |
| --- | --- |
| OQ-24 | 5.5, 5.8, 8.7, and the RQ-1 section |
| OQ-25 | 5.2/5.4 identity deprecation and 6.6 state/mirroring |
| OQ-26 | 6.6, 7.4, and 7.5 lifecycle/existence-read behavior |
| OQ-27 | 5.5/5.7 readiness/suspension, 7.6 expiry, and 8.5 trigger metrics |
| OQ-28 | 5.2 provisioning and immutable Party identity |
| OQ-29 | 5.3 `DataHandlingVersion` governance |
| OQ-30 | 8.1 hold release, 8.2 export, and 8.8 inspection second party |

### Second-Update Story Criteria And Dependencies

These criteria are normative additions to the named story sections and preserve the later approved stories 5.9, 5.10, 6.8, and 7.7. They add no story and change no active-story order.

| Story | Required addition |
| --- | --- |
| 5.2 | Keep Party link/replace wire members additive, mark them obsolete, reject every request, and prove FR1 provisioning is the only identity-creation/link path. |
| 5.3 | Own `DataHandlingVersion`; preserve the 2026-09-10 Product ruling that historical Story 5.3 executed no `EXT-PROVIDER-1` seam, created no dependency non-conformance, and did not commit or advance that dependency. |
| 5.4 | Use predefined Party, tenant-role, or Conversation Facilitator sources; keep `ApproverPolicySourceKind.Caller` declared/deserializable but obsolete and server-rejected. |
| 5.5 and 5.7 | Reject `ReportingOnlyMonitoring` and `AcceptedLaunchRisk`, emit `ProhibitedCostControlPosture`, and refuse activation or enablement while it stands. |
| 5.6 and 5.8 | Own the host protection port/binding, `PayloadProtectionUnavailable`, canary self-test, engine identity/version check, no-op/pass-through-wrapper rejection, and content-path fail-closed evidence; consume `EXT-PROTECTION-1` according to its register status. |
| 6.1 | Add `EXT-CONV-AI-1` as the exact live Conversation-context consumer; content-bearing work refuses execution while payload protection is unavailable. |
| 6.2 | Own `AgentInteractionContextMode.Blocked`, Safe Context Budget terms, and prove `ContextUnavailable` exists on the exact required public surface; consume `EXT-CONV-AI-1`. |
| 6.4 | Own `AgentGenerationOutcome.Indeterminate` and `NotInvoked`. |
| 6.6 | Consume `EXT-CONV-AI-1` and the selected `EXT-PARTIES-1` branch; expose exactly the five membership states, `BlockVersion`, `CurrentMirror(BlockVersion, Direction, Outcome, AttemptId)`, its mutually exclusive `MirrorPending`/`MirrorRefused` derivations, and the non-terminal proposal index; implement every FR2 state/read pair, direction-aware at-least-once mirroring, supersession and late-outcome rules, refused-remove/refused-readmit remediation, clear-time authority, `ReadmitPending`, next-accepted-step rejoin, and delayed reconciliation of `PostingPending`. |
| 7.1 | Consume `EXT-CONV-AI-1`; own `NoEligibleApprover`, `ApproverResolutionUnavailable`, `ResolutionEmptyPending`, and `ResolutionUnavailable`. |
| 7.4 | Consume `EXT-CONV-AI-1` and own seam-2 lookup. Before every retry or `PostingFailed` exit: present records `Posted(LateConfirmed)` and rejects the action; unavailable refuses and remains `PostingFailed`; typed absence permits retry after full re-validation; deleted/removed permits an authorized abandon only. |
| 7.5 | Depend on Story 7.4's lookup capability; add `PostingFailed` abandon paths, own `SourceConversationUnavailable` and `LateConfirmed`, and never record `Abandoned` when the message is present. |
| 7.6 and 8.5 | Own `ExpiredWhileSuspended` and its SM-3/SM-C5 exclusion; Story 8.5 consumes `EXT-CONV-AI-1` for its Conversations event-feed measurements. |
| 8.1 | A Compliance Inspector submits release, but unpin/release waits for audited approval by a distinct second Compliance Inspector or Platform Operator; the same actor cannot satisfy both steps and partial approval/unpin remains restrictive. |
| 8.2 | Require prior second-party approval under FR24 before any export content read; never use post-hoc approval. |
| 8.4 | Retain `BlockWithAuditableOverride` additively but obsolete and server-reject it at every presentation; no Approver override is possible. |
| 8.7 | Own `UnretiredAssumption`, `GateOutOfScope`, `TriggerReviewOverdue`, and all register-defined blocker rendering alongside Story 5.5. |
| 8.8 | Compute the inspection subject set and eligible second party under FR24/OQ30, enforce the 30-day Inspector anti-collusion window and Platform approval for scope wider than one Conversation. |

Every debt row is owned by the **Agents Runtime Maintainer**. All additions preserve existing enum ordinals and serialized names; an existing narrower value is not duplicated, but its owning story proves the required public surface, semantics, serialization, UI/API parity, and fail-closed use.

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

**FRs covered:** FR1–FR7, FR19–FR25, FR28–FR30, FR32–FR34

**Primary NFR ownership:** NFR1–NFR6, NFR11, NFR12, NFR13

**Natural dependencies:** Story readiness is externally gated by `EXT-HOST-1`, `EXT-PROVIDER-1`, `EXT-SECRETS-1`, and `EXT-TOPOLOGY-1` only where declared; the epic does not depend on Epics 6–8.

### Epic 6: One Safe Automatic Conversation Response

A Conversation Participant can use the sole **Call hexa** action and receive exactly one safe attributed response, or a precise fail-closed outcome before any unsafe Provider or Conversation effect. The completed automatic path is usable without Epic 7.

**Status:** active forward backlog.

**FRs covered:** FR2, FR4, FR5, FR8–FR12, FR19–FR29, FR31–FR34

**Primary NFR ownership:** NFR1–NFR12, NFR13, NFR14

**Natural dependencies:** builds on Epic 5 setup/readiness; declared seams are `EXT-TOPOLOGY-1`, `EXT-TOKEN-1`, `EXT-SAFETY-1`, `EXT-PROVIDER-1`, and `EXT-CONV-AI-1`.

### Epic 7: Complete Confirmation And Approval

An Approver can discover, revise, regenerate, resolve, and post exactly one proposal version without losing history or bypassing current gates. The epic builds on the generation/posting foundation while delivering a complete confirmation-mode outcome.

**Status:** active forward backlog.

**FRs covered:** FR7, FR13–FR25, FR27–FR29, FR31–FR34

**Primary NFR ownership:** NFR1–NFR9, NFR11, NFR13, NFR14

**Natural dependencies:** builds on Epics 5–6; regeneration/posting declare `EXT-PROVIDER-1`, `EXT-TOKEN-1`, `EXT-SAFETY-1`, and `EXT-CONV-AI-1` where consumed.

### Epic 8: Governance Operations And Release Qualification

Governance and release operators can retain, export, delete, operate safety/cost/governance policy, calculate metrics, prove UI conformance/performance, and inspect current launch evidence without turning final assessment into an implementation story.

**Status:** active forward backlog.

**FRs covered:** FR4, FR18–FR26, FR28–FR30, FR32–FR34

**Primary NFR ownership:** NFR1–NFR14

**Natural dependencies:** governance operations build on Epic 5 durable/public foundations and may proceed alongside later runtime work when their declared seams are committed; evidence inspection consumes bounded outputs from Epics 5–8 but owns no missing implementation.

### Release Gate RQ-1: Final Operational Qualification

`RQ-1` is not an epic story, is not estimated, and is not part of the executable backlog. After Epics 5–8 and all consumed dependencies complete, it evaluates the 18 minimum readiness gates, current Levels 4–5 evidence, approved production-like samples, exactly SM-1/SM-4/SM-5/SM-6 plus NFR-9/NFR-14 and 100% audit completeness as its metric inputs, and the non-metric FR-28 blockers, then produces a dated READY/NOT READY report. SM-2, SM-3, and SM-7 are launch-health inputs only.

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

**Story count:** 10.

**Dependency topology:** 5.1 establishes the build/package/boundary baseline; 5.2 and reopened 5.3 add independently usable Agent and platform-catalog/tenant-enablement operations; 5.4 binds trusted principals, identity, authorization, and security evidence; 5.5 publishes matrix-v7 readiness truth and versioned routes; 5.6 proves the platform-hosted topology and live integration tier; 5.7 consumes only prior setup stories to gate activation; 5.8 binds payload protection after the integration tier and becomes the content-bearing prerequisite for Epics 6–8; 5.9 rejects prohibited cost-control postures after readiness contracts exist; 5.10 retires prohibited safety and caller-policy inputs after authority contracts exist. No story depends on a later story.

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
- **External:** `EXT-PARTIES-1` remains `Uncommitted`. This story may develop the create-only provisioner against Branch-B-compatible immutable identity-by-id behavior, but that work is not launch evidence and cannot satisfy `RQ-1`; no selected branch may be claimed until the register records owner acceptance.
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

**Given** a Party-link or Party-replacement wire member is presented after `hexa` has been provisioned
**When** any old or current client, migration, replay, API/client, UI, or direct command path handles it
**Then** the member remains declared and deserializable but obsolete and every request is server-rejected with a typed reason
**And** FR-1 provisioning remains the only identity creation/link path, no tenant role can change the immutable Party identity, identity-by-id is required under both `EXT-PARTIES-1` branches, and AI Party-type creation/verification is required only under selected Branch A.

**Given** a caller from another tenant or without Agent-administration authority
**When** the caller commands or queries Agent configuration
**Then** authorization denies before mutation or disclosure and produces no Provider, Party, Conversation, or secret side effect
**And** response bodies, counts, diagnostics, accessible names, and audit summaries reveal no target-tenant existence or sensitive instructions.

**Evidence Manifest:**

| Field | Story 5.2 evidence |
| --- | --- |
| Requirements | FR1, FR3, FR6, FR19-FR25; OQ-25, OQ-28; NFR1-NFR5; UX-DR1-UX-DR3, UX-DR11-UX-DR17, UX-DR20, UX-DR23, UX-DR25, UX-DR30, UX-DR41, UX-DR50; AD-1-AD-5, AD-12, AD-15, AD-17; EXT-PARTIES-1 |
| OwnedClauses | FR1.live-configure-and-query; FR1.immutable-provisioned-party-identity; FR3.lifecycle-preserves-history; FR6.future-only-response-mode; FR19.agent-config-isolation; FR20.admin-authorization; FR23.structured-public-result; FR23.party-link-replace-deprecate-and-reject; FR24.configuration-change-evidence; FR25.setup-status; NFR1.authorization-before-mutation; NFR3.replay-determinism; NFR4.current-status; NFR5.safe-change-audit; UX-DR20.lifecycle-not-callability; UX-DR50.command-truth-flow; AD-3.pure-aggregate; AD-4.lifecycle-configuration-version; AD-15.api-ui-parity; DW-4 |
| Dependencies | Story 5.1; `EXT-PARTIES-1` selected branch for launch evidence, with Branch-B-compatible identity-by-id development allowed while Uncommitted |
| EvidenceLevel | Levels 2 and 4: aggregate/replay behavior plus live EventStore command-query-projection path |
| TestOrArtifact | AgentConfigurationAggregateTests; AgentLifecycleConfigurationVersionTests; AgentConfigurationEventStoreIntegrationTests; AgentSetupQueryTests; AgentConfigurationUiTests; persisted read-model fixture |
| VerificationCommand | pwsh ./eng/verify-story-5.2.ps1 |
| NegativeEvidence | AgentConfigurationAuthorizationTests.CrossTenantCommandIsDeniedBeforeMutation; AgentConfigurationAuthorizationTests.CrossTenantQueryDisclosesNothing; duplicate/conflict/replay cases |
| Result | Not run — backlog; requires Story 5.1, and `EXT-PARTIES-1` remains Uncommitted for launch evidence and `RQ-1` |

### Story 5.3: Govern Provider Models And Pricing Through Live Operations

As a Platform Operator,
I want one platform Provider/model catalog with explicit tenant enablement,
So that catalog truth is administered once while every tenant sees and selects only entries enabled for it.

**Primary Demonstrable Outcome:** Shipped tenant-scoped catalog state is migrated idempotently into the platform `ProviderCatalog` under reserved tenant `system` plus tenant-scoped `TenantProviderEnablement`, and public tenant queries expose only the enabled safe join.

**Dependencies:**

- **Prior stories:** 5.1.
- **External:** None. Catalog governance and migration do not invoke the Provider; runtime use remains gated by `EXT-PROVIDER-1` in consuming stories.
- **Non-conformance:** `NC-5.3-PLATFORM-CATALOG-SCOPE` remains open in the external-dependency register until the shipped tenant-scoped catalog is migrated and frozen as historical evidence.
- **Resolved historical-consumption ruling:** Product selected Branch B on 2026-09-10: completed historical Story 5.3 executed no `EXT-PROVIDER-1` seam, so no dependency non-conformance was created. This ruling does not commit `EXT-PROVIDER-1`, advance its status, or change its current consumers.
- **Forward dependencies:** None; this story publishes catalog truth but does not invoke the Provider.

**Acceptance Criteria:**

**Given** an AD-30 `Platform` principal and valid Provider/model capability and pricing metadata
**When** a create, update, enable, or disable command is accepted
**Then** the command targets reserved tenant `system` and one `ProviderCatalog` stream per (`ProviderId`, `ModelId`) and durably records label, platform enablement, text-generation capability, positive context/output/timeout limits, secret reference/configured state, versioned pricing units/currency, and a non-reusable monotonic CapabilityVersion
**And** tenant principals cannot mutate platform catalog state and replay and duplicate delivery produce the same state.

**Given** Provider/model data-handling terms are created or changed
**When** platform catalog governance accepts the change
**Then** the catalog owns and advances `DataHandlingVersion` with the governed capability/pricing record
**And** replay, migration, API/client, UI, and evidence surfaces preserve that version without inferring Provider behavior or consuming `EXT-PROVIDER-1`.

**Given** an enabled Provider/model has a current data-handling record whose four governed fields are presented to an authorized Tenant Agent Administrator
**When** the administrator accepts or declines the named `DataHandlingVersion` with a non-whitespace justification at the expected tenant-enablement revision
**Then** one lock-bearing `DataHandlingAcceptance` command records the decision, actor and role basis, version, exact rendered confirmation set, and authoritative pending/projection-confirmed references without changing platform catalog truth
**And** a stale or superseded version, missing justification, unauthorized actor, duplicate divergent submission, or cross-tenant target is rejected before mutation or Provider work.

**Given** the Provider/model publishes a newer data-handling version
**When** its declared and structurally verified change is a tightening relative to the tenant's last accepted version
**Then** one cumulative grace deadline is derived from the first unaccepted tightening version, later unaccepted versions cannot extend it, and the tenant remains callable only until `EvaluatedAt >= GraceExpiresAt`
**And** a neutral/loosening change, explicit decline, lapsed grace, or missing current/in-force version blocks readiness and Provider invocation with the defined safe reason.

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
| Requirements | FR4, FR5, FR19-FR25, FR28; OQ-29; NFR1, NFR4, NFR6, NFR10; UX-DR4, UX-DR11-UX-DR17, UX-DR21, UX-DR26, UX-DR30, UX-DR41; AD-2, AD-9, AD-10, AD-12, AD-14, AD-15, AD-17, AD-21, AD-22, AD-28 |
| OwnedClauses | FR4.live-provider-catalog; FR4.disabled-not-usable; FR4.data-handling-version-and-governed-record; FR5.future-only-selection; FR5.tenant-data-handling-accept-decline; FR21.missing-or-unaccepted-provider-blocks; FR24.provider-and-acceptance-change-evidence; OQ29.DataHandlingVersion; NFR6.secret-nondisclosure; NFR10.current-pricing-input; UX-DR4.safe-provider-grid; UX-DR21.provider-result-not-inferred; AD-2.platform-catalog-and-tenant-enablement; AD-9.adapter-boundary; AD-10.capability-version-limits-and-data-handling-readiness; AD-12.DataHandlingAcceptance-lock; AD-14.secret-safety; AD-22.acceptance-audit; AD-28.grace-deadline-authority; AD-30.platform-principal |
| Dependencies | Story 5.1; no external dependency; closes `NC-5.3-PLATFORM-CATALOG-SCOPE` |
| EvidenceLevel | Levels 2 and 4: catalog aggregate behavior and live EventStore/query/UI component path |
| TestOrArtifact | ProviderCatalogMigrationTests; ProviderCatalogAggregateTests; TenantProviderEnablementAggregateTests; DataHandlingAcceptanceAggregateTests; DataHandlingGraceDeadlineTests; ProviderCatalogEventStoreIntegrationTests; ProviderCatalogQueryTests; ProviderCatalogUiTests; DataHandlingAcceptanceUiTests; secret poison-sweep report |
| VerificationCommand | pwsh ./eng/verify-story-5.3.ps1 |
| NegativeEvidence | ProviderCatalogAuthorizationTests.CrossTenantSelectionIsDenied; ProviderCatalogAuthorizationTests.TenantPrincipalCannotMutatePlatformCatalogOrEnablement; DataHandlingAcceptanceIsolationTests; StaleSupersededOrUnjustifiedDataHandlingDecisionTests; GraceCannotExtendAcrossUnacceptedVersionsTests; ProviderCatalogVersionRegressionTests; ProviderCatalogMigrationConflictTests; ProviderSecretLeakTests; invalid pricing/limit cases |
| Result | Reopened — backlog under `NC-5.3-PLATFORM-CATALOG-SCOPE`; shipped tenant-scoped implementation is migration source and historical evidence, not final conformance |

### Story 5.4: Prove Trusted Principal Tenant Party And Approver Readiness

As a Tenant Security Operator,
I want current tenant, Party, and Approver evidence bound into setup readiness,
So that revoked, ambiguous, stale, or cross-tenant authority can never make **hexa** ready.

**Primary Demonstrable Outcome:** A live readiness query proves one valid tenant-scoped Agent Party identity and resolvable approver bases, then immediately becomes blocked on revocation or uncertainty.

**Dependencies:**

- **Prior stories:** 5.1 and 5.2.
- **External:** `EXT-CONV-AI-1` must be `Available` for its Facilitator, roster, existence, and accessibility reads; the membership mutation seam is not used here. `EXT-SECRETS-1` must be `Available` for live trusted-envelope issuance, verification, rotation/revocation, replay-retention, and system security-digest evidence. `EXT-HOST-1` must be `Available` for the platform-owned replicated operational security-audit spool and its recovery worker. `EXT-PARTIES-1` remains `Uncommitted`: Branch-B-compatible identity-by-id readiness may be developed, but neither Party branch can provide launch evidence or satisfy `RQ-1` before owner acceptance and availability.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** duplicated, out-of-order, gapped, replayed, or revoking Tenants events
**When** the local tenant-access projection applies them
**Then** duplicate handling is idempotent, ordering/gap/freshness is explicit, and unknown, stale, disabled, unavailable, non-member, insufficient-role, or revoked state blocks readiness
**And** no downstream Provider, membership, posting, export, or deletion side effect is attempted.

**Given** the provisioned Agent Party identity
**When** the Parties adapter resolves it
**Then** exactly one stable tenant-scoped active Party reference with current posting eligibility is accepted and no Party PII is persisted
**And** identity-by-id is mandatory under both `EXT-PARTIES-1` branches, AI Party type is additionally required only under selected Branch A, and missing, disabled, ambiguous, unavailable, unauthorized, wrong-branch, or multiple identity state blocks setup with a typed safe reason.

**Given** predefined Party, tenant-role, or Conversation Facilitator approver sources
**When** an Approver Policy basis is resolved
**Then** the exact current basis and disclosure category are recorded, API/UI report the same safe basis, and dependency loss revokes readiness
**And** JWT-only, UI-only, or historical role evidence cannot grant authority.

**Given** `ApproverPolicySourceKind.Caller` is presented by an old or current client, migration, replay, API/client, or UI path
**When** policy validation runs
**Then** the member remains declared and deserializable for compatibility but is obsolete and server-rejected before mutation
**And** Caller is absent from new configuration choices and can never become an Eligible Approver of the same call.

**Given** any public Agents operation
**When** trusted ingress derives authorization context
**Then** it selects exactly one AD-30 principal kind from `User`, `Administrator`, `Platform`, or `Workflow` according to the operation family, resolves a user `PartyId` from the authenticated subject plus fresh Parties/Tenants evidence, strips all client-supplied reserved extension keys, and issues only allowlisted scope-bound extensions with an HMAC tag
**And** the command pipeline verifies the tag before aggregate dispatch and rejects an untagged, forged, wrong-family, wrong-principal-kind, stale-role, or wrong-scope context. This closes DW-2.

**Given** cryptographic/time verification succeeds for a trusted envelope
**When** replay admission runs before target-command construction
**Then** only the platform-composed non-public `TrustedEnvelopeVerifier` capability can conditionally create/read `TrustedEnvelopeReplay` in reserved tenant `system` keyed by (`Issuer`, `DeliveryNonce`), binding the authenticated target tenant and exact canonical/tag digest; exact lost-ack replay proceeds only to AD-29 idempotency, while changed-field or cross-tenant nonce reuse rejects and audits
**And** later equality compares only authenticated envelope fields and reuses the stored first-seen/retention times rather than recomputing verifier-local time; the registrar credential can append only the exact replay event/stream, carries no recursive Agents envelope or principal, fails closed when unavailable, and is inaccessible to API, human, Workflow, target-handler, or general-dispatcher paths.

**Given** an authorization or trusted-context denial
**When** the pipeline rejects the operation
**Then** the non-public security observation recorder computes `SecurityObservationId`, routes a User/Administrator/authenticated API subject to the actor tenant, Workflow to the authenticated target tenant, and Platform or no trusted actor to reserved tenant `system`; a cross-tenant request never writes the claimed target tenant's stream
**And** it durably accepts the content-free safe envelope into the replicated `security-audit-spool` before the denial is reported as processed, later appends exactly once to `SecurityEventLog(RoutingTenantId, UTC day)`, and records the exact EventStore acknowledgement before removing the spool item.

**Given** EventStore, replay admission, or security-log append is unavailable during a denial
**When** the denial and recorder recover
**Then** no target command is constructed, the stable observation stays pending in the spool, and the least-privilege recovery worker retries the same identity without any Agents principal or recursive envelope
**And** if the spool itself is unavailable, the pipeline fails closed before target-command construction and readiness/health remains blocked; no prompt, context, generated content, secret, Party PII, claimed target-tenant existence signal, or duplicate security record is emitted.

**Given** a caller from tenant B targets tenant A Agent, Party, policy, status, or audit data
**When** each focused public and application path is exercised
**Then** every path denies before lookup-dependent disclosure or side effect, including when identifiers collide
**And** counts, empty states, error classes, timing-safe messages, logs, and accessible output disclose no tenant A record.

**Evidence Manifest:**

| Field | Story 5.4 evidence |
| --- | --- |
| Requirements | FR2, FR7, FR19-FR21, FR23-FR25, FR33; OQ-25; NFR1, NFR2, NFR6, NFR11; UX-DR5, UX-DR20, UX-DR25, UX-DR30; AD-2-AD-4, AD-7, AD-8, AD-12, AD-17, AD-29, AD-30; EXT-CONV-AI-1, EXT-HOST-1, EXT-PARTIES-1, EXT-SECRETS-1 |
| OwnedClauses | FR2.exactly-one-party; FR7.current-approver-basis; FR7.caller-source-deprecate-and-reject; FR19.cross-tenant-denial; FR20.policy-authorization; FR21.stale-ambiguous-unavailable-block; NFR1.pre-side-effect-authorization; NFR2.no-cross-tenant-disclosure; NFR11.replay-first-seen-recovery; UX-DR5.blocked-policy-source; AD-7.party-reference-only; AD-8.approver-resolution; AD-12.current-fail-closed-gates; AD-30.principal-kind-ingress-HMAC-and-Story5.4-owned-replay-and-security-recorder-pre-command-capabilities; AD-2.SecurityEventLog-and-TrustedEnvelopeReplay; DW-2 |
| Dependencies | Stories 5.1 and 5.2; existing Tenants and Parties read contracts; EXT-CONV-AI-1, EXT-HOST-1, and EXT-SECRETS-1 Available; `EXT-PARTIES-1` selected branch for launch evidence, with Branch-B-compatible development allowed while Uncommitted |
| EvidenceLevel | Levels 2 and 4: projection/unit behavior and live dependency-backed authorization/identity reads |
| TestOrArtifact | TrustedPrincipalIngressTests; ReservedExtensionHmacVerificationTests; TrustedEnvelopeReplayAggregateTests; ReplayRegistrarLeastPrivilegeIntegrationTests; SecurityEventLogAggregateTests; SecurityObservationRoutingTests; SecurityAuditSpoolRecoveryIntegrationTests; TenantAccessProjectionTests; PartyReadinessIntegrationTests; ApproverPolicyResolutionIntegrationTests; revocation/replay/security-spool evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-5.4.ps1 |
| NegativeEvidence | TrustedPrincipalIngressTests.ClientReservedKeysAreStripped; ReservedExtensionHmacVerificationTests.ForgedUntaggedWrongFamilyAndWrongScopeAreDenied; ReplayRegistrarCannotMutateOtherStreamsOrBeCalledByPublicHumanWorkflowOrHandlerTests; CrossTenantChangedFieldNonceReuseTests; ReplayExactDuplicateReusesStoredFirstSeenAndRetentionTests; ReplayLedgerUnavailableFailsTargetDispatchTests; CrossTenantDenialNeverWritesClaimedTargetSecurityStreamTests; SecurityRecorderCannotMutateOtherStreamsOrDispatchCommandsTests; SecuritySpoolUnavailableFailsBeforeTargetCommandTests; TenantPartyApproverIsolationTests.CrossTenantIdentifiersAreDeniedBeforeSideEffects; TenantAccessRevocationTests.RevocationImmediatelyBlocksReadiness; gap/stale/ambiguous/PII poison cases |
| Result | Blocked — backlog; requires Stories 5.1 and 5.2, and EXT-CONV-AI-1, EXT-HOST-1, EXT-PARTIES-1, plus EXT-SECRETS-1 are currently Uncommitted |

### Story 5.5: Publish Authoritative Readiness And Provider-State Contracts

As an Agent Administrator,
I want one authoritative readiness result shared by API and UI,
So that callability, Provider degradation, evidence freshness, and blockers cannot be inferred differently by each surface.

**Primary Demonstrable Outcome:** The same tenant and registry checkpoint returns the same versioned setup/callability result through projection, API/client, and UI, including a valid callable-degraded Provider case and fail-closed stale cases.

**Dependencies:**

- **Prior stories:** 5.2, 5.3, and 5.4.
- **External:** EXT-PROVIDER-1 must be at least Committed. `EXT-SECRETS-1` must be `Available` before publishing or projecting any runtime architecture decision because it supplies only the independent decision authority's verification anchors and trust profile; approval signing keys remain outside Agents and outside the recorder path. `EXT-PARTIES-1` is required if the Product-selected recorder authority remains a Party-bearing tenant Release Operator; `OD-RELEASE-RECORDER-SCOPE-1` is Open and no recorder path is currently authorized.
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
**When** API, BFF, UI, workflow, or readiness projection evaluates OperationGateMatrixVersion 7
**Then** all use the same authoritative GateIds, one consistent registry checkpoint, matrix version, applicable records, and safe blockers
**And** a missing family, old or unknown matrix version, invalid `ScopeKind`, unauthorized producer, missing record, non-inventory gate, or checkpoint change blocks or retries without local fallback.

**Given** an Architecture Spine `OD-*` decision or binding deferred PRD decision is proposed, approved, superseded, or evaluated for readiness
**When** its authorized publication command executes
**Then** reserved-system `ArchitectureDecisionRecord(DecisionId)` appends an immutable pending contract or effective decision version with its predecessor/genesis governance reference, allowed outcomes, approver roles and quorum, `AffectedEvaluations`, effective instant, approver actor/role evidence, and exact independently signed decision-authority manifest digest/version verified through `EXT-SECRETS-1`
**And** only the Release Operator may record the exact package, the recorder may never approve or mint authority evidence, and unauthorized/unsigned/wrong-role/mismatched-manifest publication fails before append.

**Given** the fixed reserved-system `ArchitectureDecisionCatalog` stream is absent, invalid, or has a pending successor
**When** a decision-dependent operation or `RQ-1` is evaluated
**Then** absence/invalid effective authorization emits the existing `OpenDecision` blocker with safe detail `CatalogAbsentOrInvalid`; otherwise the effective/pending catalog union enumerates stable literal DecisionIds, minimum contract versions, and baseline affected evaluations and emits `OpenDecision` for every listed missing/mismatched record
**And** removal/narrowing requires explicit predecessor authorization and no runtime planning-document parse.

**Given** an independently authorized pending catalog and every required decision record at or above its exact minimum contract
**When** `ArchitectureDecision:ActivateCatalog` executes at the expected catalog revision
**Then** it records one immutable complete manifest of DecisionIds, record revisions, contract versions, and digests and appends `CatalogActivated` as the sole transition to effective
**And** an incomplete, extra, conflicting, unauthorized, wrong-minimum, or changed manifest fails without activation; exact replay or lost acknowledgement resolves from the catalog stream, while a concurrent changed activation conflicts.

**Given** a pending successor would narrow allowed outcomes, approver roles, quorum, or affected evaluations
**When** decision readiness or activation is evaluated
**Then** the still-effective predecessor contract governs authorization and explicitly approves the narrowing; pending state never supersedes effective state by itself
**And** the launch-readiness projection evaluates the union of effective and pending affected evaluations until independently authorized approval and activation, so a decision version cannot weaken its own gate.

**Given** deferred PRD rows OQ-18, OQ-23, and OQ-31 remain unresolved
**When** runtime eligibility is evaluated
**Then** `OD-PRD-OQ18-HISTORICAL-SAFETY-1` and `OD-PRD-OQ31-INSTRUCTION-PROTECTION-1` block `RQ-1`, while `OD-PRD-OQ23-AUTOMATIC-RETRACTION-1` blocks only `AutomaticModeEnablementEligibility`
**And** no planning-document parse, absent record, or story-local default can select an outcome.

**Given** `OD-RELEASE-RECORDER-SCOPE-1` remains Open
**When** catalog publication/activation, contract publication, or approval recording is attempted
**Then** every tenant Release Operator and every Platform principal is denied because Product has not selected the authoritative tenant/source assignment for this Platform-scoped operation
**And** no actor is inferred; these four normal operations remain blocked after Product decides until the exact choice is effective in runtime state.

**Given** Product + Release PM + Governance have independently signed an exact resolution of `OD-RELEASE-RECORDER-SCOPE-1`
**When** `ArchitectureDecision:BootstrapRecorderScope` is submitted by a Release Operator freshly proven under that exact proposed source
**Then** the target is only the fixed reserved-system recorder-scope decision stream and one expected-revision append records the externally decided effective contract/outcome with source/version, recorder actor, contract digest, exactly one initial catalog version/digest commitment, nonempty root authorization/approvals, expiry/revocation evidence, and recorder/approver/custodian separation
**And** the operation cannot choose the source, publish or activate a catalog, or touch another decision; exact replay/lost acknowledgement reads that stream, changed evidence conflicts, the first `PublishCatalog` must match and consume that exact initial version/digest, and only later successors use ordinary effective-predecessor authority.

**Given** public Agents routes or legacy Agent-owned launch-readiness state
**When** the versioned public contract and readiness migration are applied
**Then** authoritative routes live under `/api/v1/agents/...` and old unversioned routes are explicitly compatibility-handled rather than authoritative
**And** legacy `RecordAgentLaunchReadiness` state migrates idempotently to `LaunchReadinessGate`, retains history, and can no longer write readiness on `Agent`.

**Given** tenant Provider readiness is evaluated
**When** the current catalog and enablement projections are read
**Then** system-scoped `ProviderCatalog` state is joined with that tenant's `TenantProviderEnablement`
**And** platform and tenant blockers are evaluated at their declared matrix-v7 `ScopeKind` without leaking another tenant's enablement.

**Given** a tenant-scoped readiness or Provider-state query
**When** an unauthorized or cross-tenant caller requests it
**Then** no record, blocker detail, count, Provider identity, or existence signal crosses the boundary
**And** the denial is recorded with a support-safe reference.

**Evidence Manifest:**

| Field | Story 5.5 evidence |
| --- | --- |
| Requirements | FR4, FR5, FR19-FR21, FR23, FR25, FR28, FR33; OQ-33; NFR1, NFR2, NFR4, NFR6, NFR12; UX-DR2, UX-DR20, UX-DR21, UX-DR25, UX-DR26, UX-DR45; AD-2, AD-10, AD-12, AD-15, AD-17, AD-24, AD-29, AD-31; EXT-PROVIDER-1; EXT-SECRETS-1; conditional EXT-PARTIES-1; OD-RELEASE-RECORDER-SCOPE-1 |
| OwnedClauses | FR25.authoritative-callability-status; FR28.machine-readable-gates-and-runtime-decision-inputs; NFR4.safe-actionable-blockers; NFR12.profile-visible-in-registry; UX-DR20.current-callability-only; UX-DR21.valid-provider-triples; UX-DR45.pass-block-insufficient-stale; AD-2.LaunchReadinessGate-and-ArchitectureDecisionRecord; AD-10.provider-readiness-contract; AD-17.registry-revision-decision-supersession-and-matrix-v7; AD-29.observation-and-decision-identity; AD-31.api-v1-route |
| Dependencies | Stories 5.2-5.4; EXT-PROVIDER-1 at Committed or Available; EXT-SECRETS-1 Available for decision catalog/publication/projection. Bootstrap contract/test development requires no selected recorder outcome; executing the bootstrap requires an independently signed Product + Release PM + Governance resolution and its proposed authority source Available, and every normal recording/activation operation requires the resulting effective OD-RELEASE-RECORDER-SCOPE-1 version. |
| EvidenceLevel | Levels 1, 2, and 4: public contract, deterministic aggregate/projection logic, and live EventStore/API/UI path |
| TestOrArtifact | LaunchReadinessGateAggregateTests; ArchitectureDecisionCatalogAggregateTests; ArchitectureDecisionCatalogActivationTests; ArchitectureDecisionRecordAggregateTests; ArchitectureDecisionRecorderScopeBootstrapTests; ArchitectureDecisionAuthorityManifestIntegrationTests; ArchitectureDecisionPredecessorGovernanceTests; ArchitectureDecisionReadinessProjectionTests; RequiredDecisionMissingStreamTests; PrdDeferredDecisionMaterializationTests; LaunchReadinessMigrationTests; LaunchReadinessProjectionTests; ProviderReadinessContractTests; ApiV1AgentsRouteTests; OperationGateMatrixV7ParityTests; readiness UI contract snapshot |
| VerificationCommand | pwsh ./eng/verify-story-5.5.ps1 |
| NegativeEvidence | ReadinessIsolationTests.CrossTenantRegistryQueryDisclosesNothing; StaleNewestObservationDoesNotFallBackTests; UnknownProviderReasonAndOldMatrixVersionBlockTests; UnauthorizedProducerAndWrongScopeKindTests; MissingDecisionCatalogEmitsOpenDecisionCatalogDetailTests; MissingCatalogListedDecisionEmitsOpenDecisionTests; IncompleteWrongMinimumOrConflictingCatalogManifestCannotActivateTests; CatalogActivationLostAckAndConcurrentSuccessorTests; OpenRecorderScopeDeniesEveryNormalDecisionPrincipalTests; BootstrapCannotChooseSourcePublishCatalogOrTouchAnotherDecisionTests; BootstrapWrongProposedSourceSameActorChangedDigestAndLostAckTests; FirstCatalogMustEqualBootstrapCommittedVersionAndDigestTests; PlatformPrincipalCannotRecordArchitectureDecisionTests; ReleaseRecorderActorCannotAppearInAnyApprovalEvidenceTests; SuccessorCannotSelfReduceQuorumApproversOrAffectedEvaluationsTests; PendingDecisionCannotRemoveEffectiveBlockerTests; PendingSuccessorCannotBlockRecordedBranchRecoveryTests; UnsignedWrongRoleAndMismatchedDecisionManifestTests; PlanningDocumentCannotSupplyRuntimeDecisionTests |
| Result | Blocked — backlog; EXT-PROVIDER-1 and EXT-SECRETS-1 are currently Uncommitted, OD-RELEASE-RECORDER-SCOPE-1 is Open, and the selected identity dependency is unresolved |

### Story 5.6: Compose Agents In The Platform-Owned Production-Like Host

As a Platform Maintainer,
I want Agents composed in the committed platform-owned production-like host,
So that the domain service and UI run with their real platform dependencies without restoring forbidden module ownership.

**Primary Demonstrable Outcome:** The versioned production-like fixture starts the platform-owned topology, exposes healthy Agents service/UI endpoints, and captures safe correlated evidence while the Agents repository contains no hosting projects.

**Dependencies:**

- **Prior stories:** 5.1 and 5.5.
- **External:** EXT-HOST-1, EXT-SECRETS-1, EXT-PROTECTION-1, and EXT-TOPOLOGY-1 must be Available and their exact compatibility commands must pass before live execution.
- **Forward dependencies:** None; unavailable Provider, tokenizer, or safety execution remains blocked through readiness and is not simulated as available.

**Acceptance Criteria:**

**Given** the exact Available EXT-HOST-1 and EXT-TOPOLOGY-1 targets
**When** the fixture resets, seeds, and starts
**Then** the platform host composes Agents DomainService and UI with EventStore, Conversations, Parties, Tenants, Dapr Workflow, readiness registry, shared capacity seam, telemetry, health, identity, evidence ingress, and fail-closed Provider/safety ports
**And** component versions, endpoints, app IDs, Dapr resources, reset procedure, and capture procedure are recorded immutably.

**Given** the exact Available EXT-HOST-1 and EXT-PROTECTION-1 targets
**When** the platform-owned host binds the production payload-protection port
**Then** startup and every readiness evaluation can seal a canary, prove persisted plaintext absence, unseal it, destroy its DEK, replay typed `Erased`, and compare engine identity/version with the accepted protection target
**And** a missing port, no-op default, self-reporting pass-through wrapper, target mismatch, or failed attestation keeps `PayloadProtectionUnavailable` active and content-bearing execution disabled.

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
| Requirements | FR19-FR23, FR25, FR28, FR34; NFR1, NFR2, NFR4, NFR6, NFR11; UX-DR1, UX-DR12, UX-DR41; AD-1, AD-14, AD-16, AD-17, AD-18, AD-31; EXT-PROTECTION-1 |
| OwnedClauses | FR21.platform-dependency-uncertainty; FR23.platform-host-public-boundary; FR25.topology-status; FR34.host-protection-binding-and-attestation; NFR6.secret-no-leak; NFR11.production-like-recovery-fixture-prerequisite; UX-DR41.FrontComposer-platform-composition; AD-16.platform-host-composition; AD-17.EXT-TOPOLOGY-fixture; AD-18.dapr-workflow-runtime-presence |
| Dependencies | Stories 5.1 and 5.5; EXT-HOST-1, EXT-SECRETS-1, EXT-PROTECTION-1, EXT-TOPOLOGY-1 Available |
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
**When** AgentActivation evaluates OperationGateMatrixVersion 7 at one RegistryRevision
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
| TestOrArtifact | AgentActivationDecisionTests; AgentActivationEventStoreIntegrationTests; OperationGateMatrixV7ActivationTests; CallabilityRevocationTests; AgentActivationUiTests; fixture-vs-live evidence classification manifest |
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
- **External:** `EXT-PROTECTION-1` and `EXT-SECRETS-1` must both be `Available` with their exact compatibility commands passing before live content-bearing execution. `OD-PRD-OQ31-INSTRUCTION-PROTECTION-1` must be approved before Agent Instructions or their audit history are classified, sealed, disclosed, or erased by this story.
- **Forward dependencies:** Content-bearing Stories 6.1–6.4, 7.1–7.4, 8.1–8.3, and 8.8 consume this foundation.

**Acceptance Criteria:**

**Given** sensitive prompt, context, generated, edited, failed, or audit content
**When** the orchestrator prepares an EventStore write
**Then** only sensitive fields are sealed in `ProtectedContent` with a per-`AgentInteraction` DEK wrapped by the tenant KEK while all non-sensitive event fields remain plaintext
**And** a different tenant's keys, envelope metadata, or digests cannot unprotect or correlate the content.

**Given** `OD-PRD-OQ31-INSTRUCTION-PROTECTION-1` is open or its recorded version does not match the deployed protection contract
**When** Agent Instructions or their configuration audit would cross a protection, authorization, disclosure, or erasure boundary
**Then** the operation blocks without treating Instructions as either protected interaction content or exempt configuration content
**And** no implementation default chooses whether they move under an Agent-level key.

**Given** a sealed envelope
**When** it crosses pub/sub, read models, adapters, caches, or Dapr Workflow orchestration
**Then** the envelope remains sealed outside an authorized unprotect boundary and workflow history contains references only
**And** the shipped no-op protection service blocks live content-bearing work and cannot satisfy evidence.

**Given** an unpinned interaction DEK and an authorized deletion
**When** `DestroyDek` returns its irreversible receipt
**Then** replay materializes typed `Erased`, snapshots and restore caches cannot revive plaintext, and the receipt is retained as support-safe evidence
**And** an active Legal Hold pin rejects destruction before any partial erasure.

**Given** the host-supplied payload-protection availability signal and the exact `EXT-PROTECTION-1` target
**When** startup or any readiness evaluation runs the FR-34 canary attestation
**Then** the system seals, verifies persisted ciphertext, unseals, destroys the canary DEK, and verifies typed `Erased` replay before clearing `PayloadProtectionUnavailable`
**And** a host assertion alone, the no-op default, a wrapper around it, a target/version mismatch, or any failed step leaves the additive runtime blocker active and every content-bearing path disabled.

**Given** the AD-27 execution-state content sweep and live integration fixture
**When** protection conformance executes
**Then** protection, unprotection, sealed transport, hold pins, deletion, erased replay, restore, tenant isolation, and every workflow state shape are covered
**And** poison plaintext is absent from EventStore-visible non-envelope fields, state store, broker, projections, responses, logs, traces, metrics, and browser evidence.

**Given** a pre-cutover EventStore inventory may contain legacy content-bearing AgentInteraction or generation-failure events written before `ProtectedContent`
**When** payload-protection readiness is evaluated
**Then** no retroactive envelope or erasability is inferred: the finite checkpoint must prove zero such streams or prove every sensitive field already protected under the exact accepted engine/key contract; any legacy plaintext keeps `PayloadProtectionUnavailable`, call/directory/deletion readiness, and `RQ-1` blocked
**And** `OD-LEGACY-PLAINTEXT-DISPOSITION-1` materializes the unresolved Product + Governance + Security + EventStore-maintainer choice; removing the blocker requires its approved outcome plus an architecture-amended authorize/effect/result/recovery procedure with EventStore/snapshot/cache/backup receipts and replay/no-content verification. Exact-empty and already-protected inventories remain evaluable; this architecture update does not invent migration, eradication, an immutable-history exception, or an unsupported-deployment outcome.

**Evidence Manifest:**

| Field | Story 5.8 evidence |
| --- | --- |
| Requirements | FR10, FR14, FR19-FR21, FR23, FR24, FR28, FR30, FR34; OQ-31; NFR1-NFR7, NFR11; AD-14, AD-17, AD-22, AD-27; EXT-PROTECTION-1; EXT-SECRETS-1; OD-PRD-OQ31-INSTRUCTION-PROTECTION-1; conditional OD-LEGACY-PLAINTEXT-DISPOSITION-1 |
| OwnedClauses | FR34.host-supplied-canary-attestation; FR34.PayloadProtectionUnavailable-runtime-blocker; AD-22.field-level-protected-content; AD-22.per-interaction-DEK-and-tenant-KEK; AD-22.typed-erased-replay; AD-22.legacy-plaintext-fail-closed-cutover; AD-27.workflow-reference-only-state; NFR2.tenant-protected-content-isolation; NFR3.replay-after-erasure; NFR6.no-secret-or-content-leak |
| Dependencies | Stories 5.1 and 5.6; EXT-PROTECTION-1 and EXT-SECRETS-1 Available; approved OD-PRD-OQ31-INSTRUCTION-PROTECTION-1 version matches the deployed protection contract; when the deployment inventory contains legacy plaintext, OD-LEGACY-PLAINTEXT-DISPOSITION-1 remains an additional blocker until an outcome-specific architecture procedure and all-copy receipts exist |
| EvidenceLevel | Levels 4 and 5: live EventStore protection plus production-like transport, restore, and workflow-state evidence |
| TestOrArtifact | PayloadProtectionAvailabilityAttestationTests; PayloadProtectionLiveTests; ProtectedContentReplayTests; LegacyPlaintextInventoryAndCutoverBlockTests; HoldPinProtectionTests; WorkflowContentSweepTests; protection evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-5.8.ps1 |
| NegativeEvidence | PayloadProtectionIsolationTests.CrossTenantKeysCannotUnprotectOrCorrelate; OpenOrMismatchedInstructionProtectionDecisionTests; NoOpProtectionCannotSatisfyLiveEvidenceTests; PlaintextPoisonSweepTests; HeldDekCannotBeDestroyedTests |
| Result | Blocked — backlog; EXT-PROTECTION-1 and EXT-SECRETS-1 are Uncommitted, OD-PRD-OQ31-INSTRUCTION-PROTECTION-1 is Open, and any nonempty legacy-plaintext deployment is additionally blocked by Open OD-LEGACY-PLAINTEXT-DISPOSITION-1 |

### Story 5.9: Reject Prohibited Cost-Control Postures At Readiness Recording

As a Release Operator,
I want readiness recording to reject reporting-only or accepted-risk cost controls,
So that production-like enablement can never pass without hard enforcement.

**Owner:** Agents Runtime Maintainer.

**Primary Demonstrable Outcome:** Every configuration and readiness path rejects the two prohibited postures before mutation, while legacy state is surfaced with the distinct `ProhibitedCostControlPosture` blocker and cannot enable generation.

**Dependencies:**

- **Prior stories:** 5.5 and 5.7.
- **External:** None newly consumed; the story operates on the public readiness/configuration contracts and register vocabulary already owned by prior stories.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** `CostControlPosture.ReportingOnlyMonitoring` or `CostControlPosture.AcceptedLaunchRisk` arrives through configuration, readiness recording, migration, replay, API/client, or UI submission
**When** the authoritative policy evaluates it
**Then** the value remains declared and deserializable under FR-23 but is obsolete and rejected with a typed result before any mutation or readiness pass
**And** no legacy client, UI option, default, or compatibility path can convert it into an accepted hard-control posture.

**Given** persisted legacy state contains either prohibited posture
**When** Agent or launch readiness is inspected
**Then** it reports the additive `AgentLaunchReadinessBlocker.ProhibitedCostControlPosture` with safe migration context and blocks production-like enablement
**And** it is not collapsed into `MissingCostControlPosture`, `Unknown`, a warning, or accepted launch risk.

**Given** `Quotas`, `Budgets`, or `ProviderModelLimits`
**When** the same paths evaluate a fully configured policy
**Then** the posture is eligible to continue to the remaining gates without implying that those gates pass
**And** any unknown enum value fails closed.

**Given** API, UI, aggregate policy, projection, and replay tests
**When** the conformance matrix executes
**Then** every surface returns the same accepted or prohibited classification
**And** focused negative tests prove neither prohibited value can clear readiness or invoke a Provider.

**Evidence Manifest:**

| Field | Story 5.9 evidence |
| --- | --- |
| Owner | Agents Runtime Maintainer |
| Requirements | FR23, FR28, FR30, FR32; NFR1, NFR4, NFR10; UX-DR20, UX-DR44, UX-DR45; AD-12, AD-15, AD-17, AD-21; launch-readiness blocker vocabulary |
| OwnedClauses | FR23.deprecate-and-reject-cost-postures; FR28.only-hard-enforcement-postures; FR30.ProhibitedCostControlPosture-surface; NFR10.reporting-only-insufficient |
| Dependencies | Stories 5.5 and 5.7 |
| EvidenceLevel | Levels 2 and 4: policy/replay behavior and live public readiness path |
| TestOrArtifact | ProhibitedCostControlPosturePolicyTests; AgentLaunchReadinessBlockerTests; CostPostureMigrationReplayTests; ReadinessCostPostureApiUiParityTests |
| VerificationCommand | pwsh ./eng/verify-story-5.9.ps1 |
| NegativeEvidence | ReportingOnlyAndAcceptedRiskCannotEnableTests; UnknownCostPostureFailsClosedTests; ProhibitedPostureCannotInvokeProviderTests |
| Result | Not run — backlog; requires Stories 5.5 and 5.7 |

### Story 5.10: Retire Prohibited Safety And Caller Policy Inputs

As a Tenant Agent Administrator,
I want obsolete configuration values refused consistently,
So that old clients cannot reactivate removed safety-override or self-approval behavior.

**Owner:** Agents Runtime Maintainer.

**Primary Demonstrable Outcome:** Server, client, UI, replay, and migration paths retain wire compatibility while refusing `BlockWithAuditableOverride` and the caller Approver source everywhere they are presented.

**Dependencies:**

- **Prior stories:** 5.4 and 5.5.
- **External:** `EXT-CONV-AI-1` remains required by Story 5.4 for the three supported Approver sources; this story consumes no additional seam.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** `ContentSafetyFailureHandling.BlockWithAuditableOverride` or `ApproverPolicySourceKind.Caller`
**When** an old or current client supplies it through create, update, import, migration, replay, API/client, or UI paths
**Then** the public member remains declared/deserializable and is marked obsolete, but the server rejects it with a typed result before mutation
**And** no Approver can override a safety failure and a caller can never become Eligible Approver of their own call.

**Given** the Approver Policy UI and public supported-values metadata
**When** policy authoring renders
**Then** it offers only Conversation Facilitator, predefined Party, and tenant-role sources
**And** it labels the legacy `ConversationOwner` wire value as Conversation Facilitator without exposing Caller as an available choice.

**Given** persisted legacy policy or safety values
**When** they are replayed or inspected
**Then** they enter a safe non-active migration state and cannot authorize, activate, approve, or weaken safety
**And** evidence names the rejected legacy value without leaking Party membership or content.

**Given** package-consumer, aggregate, API/client, UI, replay, and authorization tests
**When** the compatibility suite runs
**Then** supported values retain additive V1 compatibility and rejected values have one typed behavior
**And** direct command construction, stale UI state, or forged policy metadata cannot bypass the rejection.

**Evidence Manifest:**

| Field | Story 5.10 evidence |
| --- | --- |
| Owner | Agents Runtime Maintainer |
| Requirements | FR7, FR20, FR23, FR26, FR27, FR33; NFR1, NFR7; UX-DR5, UX-DR43; AD-8, AD-15, AD-20 |
| OwnedClauses | FR7.no-caller-source; FR23.deprecate-and-reject-register; FR27.no-safety-override; FR33.approver-authority-matrix |
| Dependencies | Stories 5.4 and 5.5; EXT-CONV-AI-1 availability inherited from Story 5.4 |
| EvidenceLevel | Levels 2 and 4: validation/replay behavior and public API/UI compatibility |
| TestOrArtifact | DeprecatedConfigurationValueTests; ApproverPolicyCallerRejectionTests; SafetyOverrideRejectionTests; ApproverPolicyUiSupportedSourceTests; PackageConsumerCompatibilityTests |
| VerificationCommand | pwsh ./eng/verify-story-5.10.ps1 |
| NegativeEvidence | CallerCannotApproveOwnCallTests; OverrideCannotBypassSafetyTests; DirectLegacyCommandCannotMutateTests |
| Result | Blocked — backlog; requires Stories 5.4 and 5.5 and EXT-CONV-AI-1 is currently Uncommitted |

## Epic 6: One Safe Automatic Conversation Response

A Conversation Participant can use the sole **Call hexa** action and receive exactly one safe attributed response, or a precise fail-closed outcome before any unsafe Provider or Conversation effect.

**Status:** active forward backlog.

**Story count:** 8.

**Dependency topology:** 6.1 establishes durable execution and recovery with deterministic activities; 6.2 produces full-context-or-blocked input; 6.3 adds two-stage safety; 6.4 proves prepared Provider attempt and cost behavior through a trusted admission contract; 6.5 supplies the shared capacity allocator and live admission; 6.6 posts once through Conversations; 6.7 exposes the complete participant outcome; 6.8 closes additive runtime-contract and Safe Context Budget evidence drift before confirmation work consumes it. No story requires a later story to satisfy its own demonstrable outcome.

### Story 6.1: Start And Recover An Automatic Interaction

As a Runtime Operator,
I want each accepted automatic interaction owned durably by one Dapr Workflow,
So that restart and replay preserve EventStore truth without duplicating execution state.

**Primary Demonstrable Outcome:** One accepted automatic interaction survives injected restarts at each current workflow checkpoint and reaches the same persisted safe outcome with no duplicate interaction, command, timer, or version.

**Dependencies:**

- **Prior stories:** 5.5 through 5.8.
- **External:** EXT-HOST-1, EXT-PROTECTION-1, EXT-SECRETS-1, EXT-PARTIES-1, EXT-CONV-AI-1, and EXT-TOPOLOGY-1 must be Available for live Dapr composition, EventStore migration-write fencing and protected content, trusted capabilities/key alias custody, caller/Agent Party verification and membership, live Conversation context, and production-like restart evidence; deterministic no-content component tests may run without executing an unavailable external seam and cannot satisfy live evidence. `OD-DAPR-SECURITY-1` and `OD-INITIAL-OUTPUT-SAFETY-STATUS-1` must be approved at the exact runtime-bound decision versions before any live Dapr/Provider workload is enabled. A deployment inventory containing legacy plaintext is additionally blocked by `OD-LEGACY-PLAINTEXT-DISPOSITION-1` and the absence of an outcome-specific procedure.
- **Forward dependencies:** None; deterministic context, safety, generation, and posting activities prove orchestration before their live adapters are introduced.

**Acceptance Criteria:**

**Given** an automatic Agent Call candidate has passed pre-creation authorization, readiness, Source Conversation access, and payload-protection gates at their exact revisions, with no membership read or mutation yet performed
**When** the platform registers durable intake and starts the interaction-owned acceptance sequence
**Then** `AgentCallAcceptance:RegisterInteractionPermit` conditionally appends one content-free permit plus a target-limited durable creation outbox on the source `ConversationAgentState`; this is the directory-registration cut, not `AgentCallAccepted`. The permit names the deterministic AgentInteractionId/logical command, request fingerprint, source revision, outbox id, protection-key alias, and monotonic permit high-water, while the outbox payload is only the sealed AD-22 `ProtectedContent` request under that interaction's DEK and never a plaintext/directory projection field
**And** incomplete directory migration/write-fence readiness or a Closing/Effective Conversation-deletion barrier rejects before any interaction/workflow/provider content; otherwise the creation outbox idempotently creates exactly that `AgentInteraction`, whose first append stores the request/snapshot as business truth and atomically emits the directory acknowledgement plus the sole workflow-start outbox. Consuming that outbox reserves then same-owner commits `WorkflowStart`; if Closing wins, it atomically cancels the reservation, normal start is suppressed, and the interaction terminalizes safely, while a committed lease starts only the exact instance and reaches `Settled` at its first durable checkpoint
**And** the one AD-13 sequence preserves Product-fixed FR-8: joint rate/open admission is step 5, a committed `ApproverResolution` lease encloses Confirmation-only Conversations/Parties Eligible Approver resolution at step 6, a committed Context lease is step 7, descriptor/Budget reservation is step 8, prompt/context safety is step 9, and a separately reserved/committed `ConversationMembership` roster read/idempotent join is step 10 immediately before `AgentCallAccepted`. A failure through step 9 proves no membership effect occurred; any pre-acceptance failure records its typed owner outcome and aborts/settles every preparation and committed lease without removing the directory permit
**And** every effect lease follows `Reserved -> CommittedToEffect -> Settled` or `Reserved -> CancelledBeforeCommit` on the same `ConversationAgentState`; only the commit revision authorizes its exact recoverable target step, Closing races with commit at the same expected revision, and a stale worker cannot act after cancellation
**And** Budget reservation and capacity acquire/queue begin only from their exact interaction-owned `BudgetReservationAuthorized` and `CapacityAdmissionAuthorized` revisions; their results/acknowledgements are distinct, and the deletion admission or migration-repair fence serializes every such phase start. After capacity admission is durably recorded, the generic Provider lease commits using owner-local evidence only, without reading the non-owner output-status decision. The next interaction step records exactly one branch: an effective exact authority appends `ProviderInvocationAuthorized` binding the lease commit plus exact decision revision, contract digest, selected status/reason/metric/open-lease mapping, catalog activation, and observation before Provider work; Open/missing/malformed/unavailable authority appends content-free `ProviderInvocationNotAuthorized` plus exact NotInvoked Budget and CancelNoInvocation capacity dispositions, whose releases/acknowledgements and negative target result settle the committed lease without Provider work. A later decision/catalog successor cannot relabel or strand either recorded branch. Provider error/timeout records content-free `GenerationFailed`; output-safety denial/unavailability uses only the authorized branch's phase-pinned mapping. Both branches have proposal/version/content/failure-record identity fields absent
**And** if Closing wins after capacity queue/admission but before Provider authorization, exact allocator lookup plus proof of no authorization, begin, or active invocation permits only `PreProviderCapacityDispositionDecided(CancelNoInvocation)`, release of that existing identity, and acknowledgement; it never invents no-use or acquires/invokes. Only safety-allowed complete output requires a separate committed `ProposalMutation(RecordGeneratedVersion)` lease before persistence; if Closing wins, returned content is discarded while only the content-free Provider outcome, Budget/capacity settlement, and Provider-lease settlement are recorded. No current Product requirement authorizes retaining failed or incomplete output bytes in V1
**And** exact permit/outbox/interaction/workflow-start/lease retry or lost acknowledgement reads the owner streams/instance, changed payload conflicts, no permit disappears because a projection lags, and Microsoft Agent Framework workflows, Python DurableAgent, MCP, A2A, in-memory workers, and alternate durable owners are absent.

**Given** the current repository may contain legacy directly-created AgentInteraction or generation-failure streams
**When** Story 6.1 enables the Conversation interaction directory
**Then** a fresh Platform Operator starts one deterministic tenant-targeted `InteractionDirectoryMigration` Workflow; Begin retains `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, and `LR-SECRETS`, explicitly omits only the circular `LR-AUDIT-PROTECTION-DELETION` record, and directly proves exact principal, protection/attestation, audit-spool, topology, tenant, EventStore, inventory, and expected-revision evidence
**And** the Workflow quiesces call and legacy interaction writers, freezes the finite tenant checkpoint/count/hash/high-water inventory, installs the authoritative EventStore namespace migration-epoch guard, revokes the legacy direct-write credential/epoch, idempotently materializes every legacy stream as content-free `MigratedFrom(StreamId, Revision)` plus current owner/protection outcome, and requires two identical post-fence scans before `DirectoryReady`
**And** every later create or content-bearing append requires the current epoch plus an exact directory permit or committed-effect capability, so stale, disconnected, queued, and restored writers are rejected at the EventStore boundary. A rejected attempt is containment evidence; any accepted post-fence write invalidates readiness and proves the write-fence gate cannot pass
**And** repair first records only the old/successor epoch plan. Its committed authorization then lets `EXT-HOST-1` atomically install the directory-repair fence, revoke new permit, creation/workflow-start/User-action-intent outbox, lease-acquire/commit, and rate/open/Budget/capacity phase-authorization capability at EventStore, and return a directory/effect-authorization checkpoint; exact lookup resolves a lost acknowledgement. Only after recording that receipt may the Workflow freeze every pre-fence directory/effect high-water plus every active deletion admission ordinal, owner/ordinal cycle, continuous content-guard binding plus ordered invalidation-gap chain, legal-hold contender, barrier/containment observed-guard-revision authorization and stale result, destruction seal, and batch identity/capability-key/guard-issued state plus referenced protection-owner consumed-or-revoked outcome. A pre-fence winner is included; a stale/disconnected/restored post-fence append writes nothing
**And** the old-epoch bridge is limited to that finite cohort while preserving all those guard facts. It delivers pending creation outboxes, starts only an already-committed start, consumes reserved starts/intents after cancellation, drives committed User actions and unleased phases only to their exact result/acknowledgement, and never cancels/rebinds committed work. It then compares every checkpoint/high-water, atomically installs a successor epoch preserving and acknowledging every deletion/hold guard, compare-token, and batch guard-issued and protection-owner outcome fact, and revokes the old epoch, repair fence, and bridge before full reconciliation and two successor scans may restore `DirectoryReady`
**And** completion requires every legacy sensitive field already protected under the exact engine/key contract or a signed independently verified exact-empty checkpoint. Plaintext is a negative fixture that remains `PayloadProtectionUnavailable` under Open `OD-LEGACY-PLAINTEXT-DISPOSITION-1`; this story selects no migration, eradication, immutable-history exception, or unsupported-deployment outcome.

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
| Requirements | FR8, FR10, FR19-FR21, FR24, FR25, FR28, FR30, FR34; NFR1-NFR5, NFR11; UX-DR27, UX-DR48, UX-DR50; AD-2-AD-4, AD-7, AD-12, AD-13, AD-17, AD-18, AD-22, AD-23, AD-27, AD-29, AD-30; EXT-HOST-1; EXT-PROTECTION-1; EXT-SECRETS-1; EXT-PARTIES-1; EXT-CONV-AI-1; EXT-TOPOLOGY-1; OD-DAPR-SECURITY-1; OD-INITIAL-OUTPUT-SAFETY-STATUS-1; conditional OD-LEGACY-PLAINTEXT-DISPOSITION-1 |
| OwnedClauses | FR8.fixed-step-5-through-10-order-and-one-explicit-accepted-interaction; FR10.safe-workflow-failure; FR24.workflow-linked-audit; FR30.complete-Conversation-interaction-directory; NFR3.no-duplicate-business-state; NFR11.RPO-zero-and-restart; UX-DR27.authoritative-interaction-states; UX-DR48.failure-is-not-proposal; AD-2.ConversationAgentState-interaction-directory-and-effect-cutover; AD-2.InteractionDirectoryMigration-write-and-directory-repair-fences; AD-7.interaction-permit-create/start-and-step-10-membership-lease; AD-12.same-owner-effect-commit; AD-13.output-safety-phase-pin-and-pre-Provider-capacity-disposition; AD-18.single-durable-owner; AD-23.frozen-cohort-monotonic-recovery; AD-29.deterministic-interaction-id; AD-30.migration-principal-and-effect-capabilities |
| Dependencies | Stories 5.5-5.8; EXT-HOST-1, EXT-PROTECTION-1, EXT-SECRETS-1, EXT-PARTIES-1, EXT-CONV-AI-1, and EXT-TOPOLOGY-1 Available for live Level 4/5 evidence; approved OD-DAPR-SECURITY-1 and OD-INITIAL-OUTPUT-SAFETY-STATUS-1 versions exactly match the deployed runtime/status contract; when the frozen deployment inventory contains legacy plaintext, OD-LEGACY-PLAINTEXT-DISPOSITION-1 plus an outcome-specific architecture procedure remain mandatory blockers |
| EvidenceLevel | Levels 2, 4, and 5: replay-safe logic, live Dapr Workflow component, production-like failure injection |
| TestOrArtifact | ConversationInteractionDirectoryAggregateTests; InteractionDirectoryMigrationBootstrapPrincipalAndWriteFenceTests; InteractionDirectoryMigrationCutoverTests; InteractionDirectoryRepairFenceCheckpointRaceTests; InteractionDirectoryMigrationRepairEpochBridgeTests; InteractionDirectoryMigrationActiveDeletionScopeFencePreservationTests; InteractionPermitCreationOutboxTests; ConversationEffectLeaseCommitRaceTests; WorkflowStartOutboxTests; Fr8OrderedAcceptanceApproverResolutionAndMembershipLeaseTests; UserProposalActionIntentOutboxRecoveryTests; ProviderOutputSafetyDecisionPhasePinTests; PreProviderCapacityDeletionDispositionTests; GeneratedProposalMutationLeaseTests; GenerationFailureProviderLeaseTests; AutomaticInteractionWorkflowTests; AutomaticInteractionRestartIntegrationTests; RecoveryCohortManifest; LR-RECOVERY partial observation for workflow-owned identities |
| VerificationCommand | pwsh ./eng/verify-story-6.1.ps1 |
| NegativeEvidence | AutomaticInteractionIsolationTests.CrossTenantCallStartsNoWorkflow; ConversationDeletionBarrierRejectsBeforePermitInteractionWorkflowAndProviderTests; PermitAndCreationOutboxLostAckTests; WorkflowStartImmediatelyBeforeAndAfterLeaseCommitAndBarrierClosingTests; ApproverResolutionImmediatelyBeforeAndAfterLeaseCommitAndBarrierClosingTests; UserActionIntentImmediatelyBeforeAndAfterCommitClosingAndApiCrashTests; StaleReservedWorkerCannotActAfterClosingTests; Fr8RateApproverContextBudgetAndSafetyFailuresCreateNoMembershipEffectTests; MembershipCommitImmediatelyBeforeAndAfterClosingEffectiveTests; ProviderCannotStartWithoutPhasePinnedOutputSafetyDecisionTests; OutputSafetySuccessorCannotRelabelAuthorizedAttemptTests; PreProviderCapacityCannotRemainQueuedOrAdmittedAfterClosingTests; GeneratedOutputDiscardedWhenProposalMutationCommitLosesTests; GenerationFailureCannotInventProposalVersionOrRetainOutputTests; LegacyMigrationWrongPrincipalWrongTenantWrongStreamAndCircularGateTests; LegacyDirectoryMigrationStaleDisconnectedRestoredWriterAndProvedEmptyTests; MigrationRepairFenceMustPrecedeCohortFreezeTests; MigrationRepairOldEpochOutboxUserIntentCommittedLeaseReservedLeaseActiveDeletionFenceAndRestoreTests; LegacyPlaintextCannotReachDirectoryReadyTests; CrossTenantProtectionKeyAliasSubstitutionTests; ProjectionLagCannotErasePermitTests; AlternateDurableOwnerGuardTests; DuplicateCommandTimerAndVersionRecoveryTests; OpenOrMismatchedDaprSecurityDecisionBlocksWorkloadTests |
| Result | Blocked — backlog; EXT-HOST-1, EXT-PROTECTION-1, EXT-SECRETS-1, EXT-PARTIES-1, EXT-CONV-AI-1, and EXT-TOPOLOGY-1 are currently Uncommitted; OD-DAPR-SECURITY-1 and OD-INITIAL-OUTPUT-SAFETY-STATUS-1 are Open; any nonempty legacy-plaintext deployment is additionally blocked by OD-LEGACY-PLAINTEXT-DISPOSITION-1 and the absent outcome-specific procedure |

### Story 6.2: Use The Complete Authorized Conversation Or Block

As a Conversation Participant,
I want **hexa** to use the complete authorized Source Conversation or not run,
So that no partial or unauthorized context is represented as a complete answer.

**Primary Demonstrable Outcome:** A fresh authorized Conversation fixture below the exact model budget produces ContextReady, while oversized, partial, stale, unsupported-tokenizer, and cross-tenant fixtures produce ContextBlocked before Provider work.

**Dependencies:**

- **Prior stories:** 5.8, 6.1, and Epic 5 Provider/readiness contracts.
- **External:** `EXT-PROTECTION-1`, `EXT-TOKEN-1`, and `EXT-CONV-AI-1` must be Available before protected content, tokenization, or the complete tenant-scoped Conversations read seam is executed.
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
| Requirements | FR8-FR10, FR19-FR21, FR24, FR25, FR28, FR31, FR34; NFR1, NFR2, NFR4, NFR8, NFR9; UX-DR24, UX-DR27, UX-DR42; AD-6, AD-10-AD-14, AD-18; EXT-CONV-AI-1 |
| OwnedClauses | FR9.complete-authorized-context; FR9.no-bounded-fallback; FR9.fail-before-provider; FR10.invalid-context-safe-failure; NFR8.complete-or-blocked; NFR9.fast-pre-provider-context-rejection-source; UX-DR42.read-only-full-or-blocked; AD-10.capability-high-water; AD-11.exact-tokenizer-and-revalidation |
| Dependencies | Stories 5.8 and 6.1; EXT-PROTECTION-1, EXT-TOKEN-1, and EXT-CONV-AI-1 Available |
| EvidenceLevel | Levels 2 and 4: deterministic budget behavior and live Conversations/tokenizer integration |
| TestOrArtifact | CompleteConversationContextTests; ConversationContextIntegrationTests; ProviderTokenizerCompatibilityTests; ContextRevalidationTests; no-content evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-6.2.ps1 |
| NegativeEvidence | ConversationContextIsolationTests.CrossTenantContextIsDeniedBeforeReadDisclosure; NoTruncationSummaryWindowRetrievalTests; UnsupportedTokenizerAndStaleCapabilityTests |
| Result | Blocked — backlog; EXT-PROTECTION-1, EXT-TOKEN-1, and EXT-CONV-AI-1 are currently Uncommitted |

### Story 6.3: Block Unsafe Prompt Context Or Output

As a Security Operator,
I want fresh versioned safety decisions before Provider and Conversation side effects,
So that unsafe or unauthorized content cannot enter generation, proposal, or posting paths.

**Primary Demonstrable Outcome:** Versioned safety fixtures prove prompt/context denial prevents Provider work and output denial creates only the approved `OD-INITIAL-OUTPUT-SAFETY-STATUS-1` content-free status/Audit Evidence with no proposal, message, retained bytes, or failure-content record.

**Dependencies:**

- **Prior stories:** 5.8, 6.1, and 6.2.
- **External:** EXT-PROTECTION-1, EXT-SAFETY-1, EXT-SECRETS-1, and EXT-CONV-AI-1 must be Available before protected content, live safety execution, HMAC digest-key rotation/rescan evidence, or complete Conversation-index initialization. `OD-INITIAL-OUTPUT-SAFETY-STATUS-1` must be approved before the story or live Provider path is authorized; no story-local status mapping is permitted.
- **Forward dependencies:** None; deterministic generated-output fixtures exercise the second stage without requiring the later Provider story.

**Acceptance Criteria:**

**Given** a prompt plus complete authorized Conversation Context
**When** the live pre-Provider safety adapter evaluates the active policy
**Then** a fresh versioned Allow is required before progression, while missing, stale, unversioned, indeterminate, always-blocked, unauthorized-data, or control-bypass results stop before Provider work
**And** audit/status retain only policy/version, safe reason, timing, and no-content evidence permitted by policy.

**Given** deterministic complete generated output
**When** the live output-safety adapter evaluates it
**Then** a fresh versioned Allow is required before any proposal or Conversation side effect
**And** the generic Provider lease commits before the output-status decision read. Open/missing/malformed/unavailable authority records `ProviderInvocationNotAuthorized`, settles existing pre-invocation Budget/capacity obligations and the lease, and performs no Provider work. Otherwise `ProviderInvocationAuthorized` phase-pins the exact effective `OD-INITIAL-OUTPUT-SAFETY-STATUS-1` decision revision, contract digest, selected status/reason/metric/open-lease mapping, catalog activation, and observation. A Block or unavailable output result creates no Proposed Agent Reply, proposal version, queue entry, notification, editor, approval action, Conversation Message, retained failed bytes, or live `GenerationFailureRecord`; it records only that pinned content-free `GenerationFailed` or `SafetyFailed` status/reason and Audit Evidence under the committed Provider lease, and no later catalog/decision successor relabels either recorded branch.

**Given** restricted hate, harassment, sexual, violent, illegal-activity, or sensitive-personal content
**When** policy evaluates the tenant use case and response mode
**Then** it may proceed only under an explicitly permitted tenant use case and Confirmation Response Mode
**And** Automatic mode and absent/ambiguous permission block.

**Given** a retry after a safety decision
**When** the active policy changes
**Then** the retry uses a policy at least as restrictive as its initial attempt, an Approver cannot override a Block, and a failed attempt is never resurrected
**And** every always-blocked and restricted category is covered without storing prohibited raw content in diagnostics or evidence.

**Given** a new Content Safety Policy version or the `EXT-SECRETS-1` per-tenant digest-key version is published
**When** safety-verdict activation begins
**Then** EventStore appends `SafetyVerdictEpoch(PendingActivation)` with the new policy/key versions and a frozen global EventStore position; the active Agent cohort is enumerated from provision events at or before that position in deterministic (`TenantId`, `AgentId`) order with count, hash, and high-water evidence
**And** each tenant freezes its existing-Conversation cohort from content-free `safety-verdict-directory` keys at or before the same position, after that projection catches up, with its own count, hash, and high-water evidence; a per-tenant fenced coordinator lease processes deterministic bounded batches using positive versioned concurrency, batch-size, lease, and wait limits.

**Given** a tenant is provisioned or a Conversation is first encountered during a pending or active epoch
**When** `ProvisionHexa` or Agent Call evaluates current safety state
**Then** `ProvisionHexa` records the exact Content Safety Policy revision and re-reads or initializes the newest pending/active epoch before callability, so a concurrent tenant is either in the frozen cohort or owns a newer initialization
**And** an unindexed Conversation uses conditional create followed by a complete `EXT-CONV-AI-1` read and scan before the call; no absent directory key or checkpoint race is treated as already scanned.

**Given** an epoch is pending, the coordinator crashes, loses an acknowledgement, or its lease expires
**When** recovery resumes
**Then** a successor fence replays only unacknowledged deterministic batch identities, conflicting results fail closed, and the epoch becomes Active only after the finite active-tenant manifest and each tenant's finite indexed-Conversation manifest are durably acknowledged
**And** a tenant provisioned after the global checkpoint completes its own version-pinned handshake before local callability, while a Conversation absent from its tenant manifest initializes on demand before that Conversation's first call; neither post-checkpoint path extends or reopens the finite activation barrier, and status/projection evidence cannot infer completion from missing manifest work.

**Given** an Agent Call arrives while its Conversation lacks a current Active-epoch verdict
**When** the configured monotonic wait bound is exercised
**Then** the call never uses the old epoch, waits only within the frozen positive profile, and then fails closed as `ContextReadUnavailable(RescanPending)` if the exact current verdict is still unavailable
**And** no Provider, proposal, posting, or weaker-policy side effect occurs before the current verdict exists.

**Evidence Manifest:**

| Field | Story 6.3 evidence |
| --- | --- |
| Requirements | FR10, FR12, FR19-FR21, FR24-FR28; NFR1, NFR2, NFR4, NFR7, NFR11; UX-DR27, UX-DR36, UX-DR43, UX-DR48, UX-DR50; AD-2, AD-12, AD-14, AD-17, AD-20, AD-23, AD-29; EXT-SECRETS-1; EXT-CONV-AI-1; OD-INITIAL-OUTPUT-SAFETY-STATUS-1 |
| OwnedClauses | FR26.versioned-active-safety-policy; FR27.prompt-context-before-provider; FR27.output-before-side-effect; FR27.no-approver-override; FR27.policy-and-digest-key-rotation-rescan; NFR7.active-safety-before-side-effects; NFR11.rescan-fenced-recovery; UX-DR43.no-weaker-retry; UX-DR48.no-proposal-and-content-free-failure-status; AD-2.SafetyVerdictEpoch-and-SafetyVerdictIndex; AD-20.always-blocked-restricted-two-stage-and-rescan-activation-barrier |
| Dependencies | Stories 5.8 and 6.1-6.2; EXT-PROTECTION-1, EXT-SAFETY-1, EXT-SECRETS-1, and EXT-CONV-AI-1 Available; OD-INITIAL-OUTPUT-SAFETY-STATUS-1 approved at the runtime-bound version |
| EvidenceLevel | Levels 2 and 4: policy matrix behavior and live safety-adapter compatibility |
| TestOrArtifact | TwoStageSafetyPolicyTests; ContentSafetyAdapterIntegrationTests; SafetyVerdictEpochAggregateTests; SafetyVerdictIndexAggregateTests; SafetyVerdictDirectoryProjectionTests; ActiveTenantCohortManifestTests; ProvisionDuringSafetyEpochTests; UnindexedConversationSafetyInitializationTests; SafetyVerdictRescanRecoveryLiveTests; DigestKeyRotationRescanTests; NoWeakerRetryTests; ProviderAuthorizationOutputSafetyDecisionPhasePinTests; OutputSafetyDecisionSelectedStatusAndNoRetainedContentTests; content-safe rescan evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-6.3.ps1 |
| NegativeEvidence | SafetyTenantIsolationTests.CrossTenantAndUnauthorizedDataAreAlwaysBlocked; OldEpochVerdictCannotAuthorizeCallTests; MissingLostOrConflictingRescanAckTests; ProjectionNotCaughtUpCannotActivateEpochTests; ConcurrentTenantCannotEscapeSafetyInitializationTests; AbsentDirectoryKeyCannotAuthorizeUnscannedConversationTests; ExpiredLeaseCannotActivateEpochTests; UnboundedOrInvalidRescanProfileTests; OpenOrMismatchedOutputSafetyDecisionBlocksBeforeProviderTests; DecisionSuccessorCannotRelabelAuthorizedOutputSafetyFailureTests; ApproverOverrideDoesNotExistTests; AlwaysBlockedRestrictedCategoryMatrixTests; raw-content poison sweep |
| Result | Blocked — backlog; EXT-SAFETY-1, EXT-SECRETS-1, and EXT-CONV-AI-1 are currently Uncommitted, and OD-INITIAL-OUTPUT-SAFETY-STATUS-1 is Open |

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
**Then** the aggregate assigns the next `AttemptOrdinal`, the shared `AgentsIdentity` canonicalizer derives `AttemptId`, `ReservationId`, `AdmissionId`, and `QueueId` exactly per AD-29, and one deterministic descriptor binds them with ProviderId, ModelId, EffectiveProviderCapabilityVersion, `CurrentDataHandlingVersion`, `InForceDataHandlingVersion`, limits, timeout, policy versions, maximum estimated cost, and the shared canonical request fingerprint
**And** the descriptor contains no raw prompt, context, generated content, secret, or Provider payload.

**Given** an interaction reaches rolling-rate, original-caller concurrency, and monetary admission
**When** its owners prepare and recover the decisions
**Then** `RateLimitLedger(TenantId, ScopeKind, ScopeId)` owns immutable per-Party/per-Conversation rolling consumption through one `RateAdmissionAuthorized` deadline and mutually exclusive interaction-owned commit/abort decision, `OpenInteractionLedger(TenantId, CallerPartyId)` owns the original caller's nonterminal lease through its authorization/commit-or-abort/release decisions, and `BudgetLedger(TenantId, UTC BudgetPeriod)` owns only monetary reserve/release/settlement, `Unreconciled`, and period close
**And** every command carries its exact interaction decision revision, each owner acknowledges it idempotently, missing/conflicting evidence changes no state, and failure injection proves recovery cannot split the rate scopes, strand a pre-acceptance `SafetyFailed` lease, release money from absence, or let a released reservation authorize Provider invocation.

**Given** `OD-RATE-CONCURRENCY-CONSUMPTION-1` is Open or its recorded version does not match the admission implementation
**When** original call acceptance reaches joint rate/concurrency admission
**Then** the interaction blocks before either `RateLimitLedger` or `OpenInteractionLedger` is contacted and before any budget or Provider effect, with no inferred default about whether a concurrency rejection consumes rolling rate
**And** after Product and Architecture approve it, the exact recorded common owner order, consumption choice, and FR-25 attribution are applied under that DecisionVersion and no alternative sequence is accepted.

**Given** the current pricing, budget, rate, concurrency, and positive versioned admission-preparation profile plus a trusted current capacity identity/fence supplied by the focused test contract
**When** admission and budget authorization run
**Then** `RateAdmissionAuthorized` plus `OpenInteractionLeaseAuthorized` record the joint ledger capabilities, `AdmissionEvaluatedAt`, `PartyResetAt`, `ConversationResetAt`, preparation deadline/profile, and DecisionVersion; `BudgetReservationAuthorized` separately records the sole capability for the exact descriptor/period/amount. Both reset times derive once from `AdmissionEvaluatedAt` plus their frozen window definitions rather than receipt time, rate/open deadlines derive once from the same interaction/profile instant, and only the exact Budget authorization revision lets `BudgetLedger` reserve maximum estimated attempt cost under deterministic `ReservationId` before Provider invocation
**And** the preparation timeout is positive and strictly less than both frozen Party and Conversation rolling-window durations, `PreparationDeadline` is strictly earlier than both reset instants, and reaching either reset before both rate/open acknowledgements drives the same durable abort; missing/stale pricing, budget, or profile, deadline mismatch, indeterminate owner state, per-call excess, 100% monthly exhaustion, changed descriptor, or invalid admission blocks, while 80% emits only an authorized warning.

**Given** reservation, a generic committed Provider lease, and durable `ProviderInvocationAuthorized` evidence
**When** the committed Provider adapter is called
**Then** `AttemptId` is used verbatim as the Provider idempotency key, safe availability/error/timeout/usage contracts remain adapter-local, and a transport retry reuses the exact descriptor, ordinal, reservation, admission identity/fence, and policy floor
**And** any changed capability, limit, price, fingerprint, readiness, or admission evidence fails closed under that AttemptId.

**Given** success, safe failure, timeout, or crash after transport
**When** recovery queries the authoritative Provider outcome by AttemptId
**Then** actual usage is reconciled exactly once, unused reservation is released only after authoritative no-usage evidence, and eligible retries never double-charge or duplicate Provider work
**And** secret poison values and raw Provider errors appear nowhere outside the adapter.

**Given** an earlier transport attempt already fixed `BudgetDispositionDecided(InvocationSettlement)` and authoritative lookup confirms no usage
**When** the mandatory safety re-check blocks the otherwise eligible transport retry
**Then** the retry terminalizes without transport, preserves the existing `InvocationSettlement` disposition, and releases money only through the confirmed-no-use outcome revision already required by that disposition
**And** it never appends or infers `NotInvokedRelease`, because historical invocation authorization makes that branch permanently unavailable for the attempt.

**Given** reservation succeeded and the generic Provider lease committed but Provider invocation was not yet authorized
**When** the output-status authority is Open, missing, malformed, or unavailable, another pre-Provider decision fails, or recovery resumes concurrently
**Then** `AgentInteraction` records exactly one mutually exclusive authorization branch and `BudgetDispositionDecided(NotInvokedRelease|InvocationSettlement)`: `ProviderInvocationNotAuthorized` is batched with the NotInvoked disposition and exact pre-Provider capacity cancellation decision, while `ProviderInvocationAuthorized` is batched with `InvocationSettlement`
**And** only those exact decision revisions drive Budget/capacity release or settlement and Provider-lease result/settlement. Absence of either branch, timeout, or a recovery observation never releases money, and a recorded branch never re-reads a successor decision.

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
| Requirements | FR4, FR5, FR10, FR12, FR19-FR21, FR24, FR25, FR28; OQ-32; NFR1, NFR3, NFR4, NFR6, NFR9-NFR11; UX-DR21, UX-DR26, UX-DR44; AD-2, AD-9, AD-10, AD-13, AD-14, AD-18, AD-21, AD-22, AD-29; EXT-PROTECTION-1; OD-RATE-CONCURRENCY-CONSUMPTION-1 |
| OwnedClauses | FR5.provider-attempt-current-and-in-force-data-handling-versions; FR10.provider-timeout-safe-failure; FR19.ledger-tenant-isolation; FR20.admission-and-budget-authorization-before-ledger-or-provider; FR24.provider-attempt-data-handling-evidence; FR28.atomic-rate-open-budget-decisions-and-reconcile-retry; OQ32.no-inferred-rate-concurrency-consumption; NFR1.authorization-before-side-effect; NFR6.provider-secret-boundary; NFR9.generation-latency-source-events; NFR10.hard-caps-and-reservation; NFR11.no-duplicate-admission-lease-attempt-or-reservation; UX-DR44.warning-block-indeterminate-cost; AD-2.RateLimitLedger-OpenInteractionLedger-BudgetLedger; AD-13.prepared-attempt-and-joint-rate-open-decision-protocol; AD-21.rate-reset-and-budget-decision-protocols; AD-29.rate-attempt-reservation-admission-queue-identities |
| Dependencies | Stories 5.8 and 6.1-6.3; EXT-PROTECTION-1, EXT-PROVIDER-1, and EXT-SECRETS-1 Available; approved OD-RATE-CONCURRENCY-CONSUMPTION-1 version exactly matches the injected trusted admission contract for focused proof |
| EvidenceLevel | Levels 2 and 4: ledger/descriptor behavior and live Provider/secret adapter compatibility; no production callability claim |
| TestOrArtifact | AgentsIdentityAttemptTests; PreparedProviderAttemptTests; ProviderAttemptDataHandlingVersionEvidenceTests; JointRateConcurrencyDecisionVersionTests; RateAdmissionTwoScopeDecisionTests; RatePreparationBoundTests; RateResetDerivationTests; OpenInteractionLeaseDecisionTests; BudgetDispositionDecisionTests; RetryTimeSafetyFailurePreservesInvocationSettlementTests; BudgetLedgerAggregateTests; BudgetLedgerConcurrencyTests; BudgetReservationIsolationTests; ProviderAdapterIdempotencyIntegrationTests; ProviderOutcomeRecoveryTests; decision/ack failure-injection and usage/reconciliation manifests |
| VerificationCommand | pwsh ./eng/verify-story-6.4.ps1 |
| NegativeEvidence | LedgerIsolationTests.CrossTenantRateOpenBudgetPrepareCommitAbortReleaseSettleAndStatusAreDenied; OpenOrMismatchedRateConcurrencyDecisionTouchesNoLedgerTests; RateDeadlineMismatchTests; PreparationDeadlineAtOrAfterEitherResetTests; EitherResetBeforeBothAcknowledgementsAbortsTests; RateResetCannotUseLedgerReceiptTimeTests; PreAcceptanceSafetyFailedLeaseAbortTests; RetryTimeSafetyFailureCannotChangeBudgetDispositionTests; RecoveryCannotReleaseBudgetFromAbsentInvocationTests; ReleasedReservationCannotAuthorizeInvocationTests; ProviderInvocationWithoutAdmissionTests; ConcurrentBudgetOverspendTests; RetryDoubleChargeAndChangedFingerprintTests; ProviderSecretAndRawErrorPoisonSweepTests |
| Result | Blocked — backlog; EXT-PROVIDER-1 and EXT-SECRETS-1 are currently Uncommitted and OD-RATE-CONCURRENCY-CONSUMPTION-1 is Open |

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

**Given** a reserved deterministic AttemptId and its interaction-owned `CapacityAdmissionAuthorized` revision
**When** the shared allocator acquires capacity using that sole phase capability
**Then** one linearizable operation atomically enforces tenant and system scopes and returns Admitted with AdmissionId/fence, Queued with durable QueueId, or Rejected before `ProviderInvocationAuthorized`; `CapacityAdmissionRecorded` binds the exact result and a deletion/migration-repair fence rejects a losing authorization append before allocator contact
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
- **External:** EXT-CONV-AI-1 must be Available and its exact membership/posting compatibility command must pass. `EXT-PARTIES-1`'s selected branch must be Available for launch evidence; Branch-B-compatible identity-by-id development is permitted while it is Uncommitted but cannot satisfy `RQ-1`.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** the current Agent Party identity under the selected `EXT-PARTIES-1` branch, source Conversation authorization, allowed generated version, and Available EXT-CONV-AI-1 target
**When** Agents establishes membership
**Then** `ConversationAgentState(TenantId, ConversationId)` owns the five states `NeverJoined`, `Joined`, `ExternallyRemoved`, `Blocked`, and `ReadmitPending`, the block authority, `BlockVersion`, one `CurrentMirror(BlockVersion, Direction, Outcome, AttemptId)`, and the index of non-terminal proposals
**And** identity is always verified by immutable id and additionally by AI Party type only under Branch A; `NeverJoined` or `ReadmitPending` adds `hexa` through the limited EXT-CONV-AI-1 membership seam and records `Joined`; `Joined` verifies current presence; `ExternallyRemoved` or `Blocked` rejects; exact retries are idempotent no-ops and general participant administration is unavailable.

**Given** Conversations has externally removed the Agent participant
**When** acceptance or the mandatory pre-post revalidation detects the removal
**Then** only a prior `Joined` state can record `ExternallyRemoved` and `RemovedInConversations`, block silent rejoin, and abandon every indexed non-terminal proposal except uninterruptible `PostingPending`, without deleting versions
**And** clearing records `ReadmitPending`, so absence after a clear is re-admission rather than a fresh removal; a Tenant Agent Administrator or the current Conversation Facilitator may clear `ExternallyRemoved`, while an Administrator-set block cannot be cleared by a Facilitator.

**Given** a Tenant Agent Administrator or Conversation Facilitator sets or clears an Agents-owned block
**When** Conversations removal/addition mirroring is unavailable
**Then** the authoritative Agents state changes at the expected revision, increments monotonic `BlockVersion`, and creates a deterministic current mirror with `Direction = Remove` for set or `Readmit` for clear and `Outcome = Pending`; `MirrorPending` is the derived UI/status value while an idempotent at-least-once outbox retries that attempt
**And** a successor set or authorized clear/re-clear supersedes every lower-version attempt, whose late success or refusal is audit-only and cannot change current state.

**Given** the current remove or readmit mirror receives a typed permanent refusal
**When** the outcome is recorded
**Then** `Outcome = Refused` derives `MirrorRefused` and ends retry only for that attempt; refused remove leaves `Blocked`, refused readmit leaves `ReadmitPending`, and `MirrorPending` and `MirrorRefused` can never both render
**And** re-set starts a fresh remove, authorized clear starts or repeats readmit, no standalone action clears the error, and SM-C4 counts only the current Refused outcome.

**Given** any combination of the five membership states and a fresh participant existence result of present, typed absence, typed `ConversationDeleted`, typed `PrincipalRemovedFromConversation`, or unavailable
**When** acceptance, pre-post validation, block clearing, or delayed reconciliation executes
**Then** every FR-2 state/read pair has one deterministic fail-closed transition or rejection, clear authority is re-evaluated at clear time, and clearing records `ReadmitPending` without rejoining immediately
**And** rejoin occurs only at the next accepted membership step, while `PostingPending` remains uninterruptible and applies a pending removal or block on exit when no message was posted.

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
| Requirements | FR2, FR11, FR12, FR18-FR21, FR24, FR25, FR28, FR30, FR33; OQ-25, OQ-26; NFR1-NFR5, NFR11; UX-DR22, UX-DR27, UX-DR50; AD-2, AD-6, AD-7, AD-12-AD-14, AD-17, AD-18, AD-23, AD-31; EXT-PARTIES-1 |
| OwnedClauses | FR2.five-state-membership; FR2.BlockVersion; FR2.CurrentMirror-direction-outcome-attempt; FR2.mutually-exclusive-derived-mirror-status; FR2.mirror-supersession-and-late-outcome; FR2.direction-specific-refusal-remediation; FR2.current-refusal-metric; FR2.state-read-pair-reconciliation; FR2.clear-time-authority-and-readmission; FR2.agent-party-attribution; FR11.one-automatic-message; FR12.no-message-on-failed-gate; FR18.external-removal-abandons-nonterminal-proposals; FR18.PostingPending-delayed-reconciliation; FR19.membership-posting-isolation; FR24.final-message-link; NFR3.no-partial-or-duplicate-message; NFR11.no-duplicate-conversation-post; UX-DR22.posted-only-success; AD-2.ConversationAgentState; AD-6.conversations-client-only; AD-7.branch-selected-membership-and-current-mirror; AD-13.deterministic-message-id; AD-31.removal-block-and-readmission |
| Dependencies | Story 5.4; Stories 6.3-6.5; EXT-CONV-AI-1 Available; `EXT-PARTIES-1` selected branch Available for launch evidence, with Branch-B-compatible development allowed while Uncommitted |
| EvidenceLevel | Levels 2, 4, and 5: deterministic identity, live Conversations compatibility, production-like membership/post/recovery path |
| TestOrArtifact | ConversationAgentStateAggregateTests; ExternalRemovalAndReadmissionTests; CurrentMirrorDirectionOutcomeTests; MirrorSupersessionAndLateOutcomeTests; MirrorRefusalRemediationTests; MembershipMirrorOutboxTests; PartyIdentityBranchCompatibilityTests; AiMembershipCompatibilityTests; AutomaticPostingIntegrationTests; ConversationPostingRecoveryTests; AutomaticResponseAuditCompletenessTests; LR-CONVERSATIONS-MEMBERSHIP-POSTING observation |
| VerificationCommand | pwsh ./eng/verify-story-6.6.ps1 |
| NegativeEvidence | ConversationsPostingIsolationTests.CrossTenantMembershipAndAppendAreDenied; DuplicateMembershipAndMessageRetryTests; DirectConversationStreamWriteGuardTests |
| Result | Not run — backlog; EXT-CONV-AI-1 and EXT-PARTIES-1 are currently Uncommitted for launch evidence |

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

**Given** `EXT-CONV-UI-1` is bound
**When** the Conversations-owned host composes the Agents contribution
**Then** it consumes all four versioned artifact kinds: the action contribution with typed registration failure, the per-message decoration slot keyed by `MessageId` and sourced only from the Agents provenance accessor, `GetCallabilityAsync(tenant, conversation)`, and the Agents-owned persistent status region beside the trigger
**And** the persistent region is the sole live-region owner, the dialog panel owns no live-region node, and the seam returns focus to the trigger or its deterministic successor when that trigger no longer exists.

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
**Then** no proposal, proposal version, queue item, notification, editor, approval/posting control, retained failed bytes, or live failure-content record is created; authorized users may reach only the content-free failure status/reason and Audit Evidence on `AgentInteraction`, with output-safety status supplied by the approved Product decision
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
| OwnedClauses | FR8.sole-call-action; FR10.failure-status-no-proposal; FR11.posted-attribution-visible; FR22.call-ui-parity; NFR13.call-accessibility-localization-responsive; NFR14.instrumentation-seams-for-call-status; UX-DR24.sole-entry; UX-DR27.authoritative-states; UX-DR32.focus-return; UX-DR36.localized-live-region; UX-DR40.restrictive-viewport; UX-DR48.content-free-failure-status-only; UX-DR50.truth-flow; AD-26.browser-timing-seams; AD-31.four-artifact-conversation-ui-seam; AD-31.persistent-region-sole-live-region-owner |
| Dependencies | Stories 6.1-6.6; EXT-CONV-UI-1 Available |
| EvidenceLevel | Levels 2 and 4: component behavior and live public-contract/UI integration; performance attainment remains Story 8.6 |
| TestOrArtifact | CallHexaComponentTests; ConversationUiFourArtifactContractTests; MessageProvenanceDecorationTests; ConversationCallabilityIntegrationTests; PersistentStatusLiveRegionTests; FocusReturnContractTests; AutomaticCallStatusIntegrationTests; CallHexaAccessibilityTests; AgentsResourcesParityTests; AlternateInvocationGuardTests; browser timing instrumentation contract tests |
| VerificationCommand | pwsh ./eng/verify-story-6.7.ps1 |
| NegativeEvidence | CallHexaIsolationTests.CrossTenantActionAndStatusAreDenied; AlternateInvocationGuardTests; OptimisticPendingOrPostedStateTests; generation-failure proposal-absence tests |
| Result | Blocked — backlog; requires Stories 6.1-6.6 and EXT-CONV-UI-1 is Uncommitted |

### Story 6.8: Add Missing Fail-Closed Runtime Contract Members

As an Agents Runtime Maintainer,
I want public runtime vocabulary and Safe Context Budget evidence to match the authoritative PRD,
So that structural unavailability fails closed without generic fallbacks or incomplete audit evidence.

**Owner:** Agents Runtime Maintainer.

**Primary Demonstrable Outcome:** Contracts, aggregate policies, projections, API/client, UI, audit, and replay all preserve the new fail-closed states and complete budget terms, while register-only vocabulary stays outside runtime enums unless the PRD explicitly promotes it.

**Dependencies:**

- **Prior stories:** 5.8, 6.2, 6.3, and 6.6.
- **External:** `EXT-CONV-AI-1`, `EXT-TOKEN-1`, `EXT-SAFETY-1`, and `EXT-PROTECTION-1` must be Available for the live paths exercised by this story.
- **Forward dependencies:** Story 7.7 consumes the reconciled runtime and reason vocabulary.

**Acceptance Criteria:**

**Given** the public runtime contracts
**When** additive compatibility is inspected
**Then** they include `AgentInteractionContextMode.Blocked`, `AgentGenerationOutcome.Indeterminate`, the safe reasons `NoEligibleApprover`, `RemovedInConversations`, `SourceConversationUnavailable`, and `NotInvoked`, and a `Membership` gate check
**And** the Story 5.8/6.6 values `PayloadProtectionUnavailable`, `NeverJoined`, `Joined`, `ExternallyRemoved`, `ReadmitPending`, `CurrentMirror(BlockVersion, Direction, Outcome, AttemptId)`, and its mutually exclusive `MirrorPending`/`MirrorRefused` derivations are carried wherever their owning contract requires them.

**Given** a complete authorized context is measured
**When** Safe Context Budget evidence is recorded
**Then** it records model context limit, reserved output allowance, Agent Instructions, caller prompt plus system framing, configured margin percent, margin tokens, measured context tokens, and resulting available context budget
**And** omitting any term, using a tokenizer other than the selected Provider/model tokenizer, or exceeding the result records `Blocked` before Provider invocation.

**Given** a new, unknown, malformed, or incompatible enum, reason, or gate value
**When** aggregate, replay, projection, API/client, or UI code handles it
**Then** the value fails closed without being normalized to a successful or weaker known state
**And** safe status and Audit Evidence preserve the classification without raw prompt, context, output, Party PII, or Provider payloads.

**Given** readiness vocabulary is mapped to code
**When** contract ownership is checked
**Then** `UnretiredAssumption`, `DependencyNotAvailable`, `OpenDecision`, and `InsufficientEvidence` remain launch-readiness-register vocabulary rather than Agent aggregate members
**And** only the PRD-named runtime exceptions `ProhibitedCostControlPosture` and `PayloadProtectionUnavailable` extend `AgentLaunchReadinessBlocker`.

**Evidence Manifest:**

| Field | Story 6.8 evidence |
| --- | --- |
| Owner | Agents Runtime Maintainer |
| Requirements | FR2, FR7, FR9, FR10, FR12, FR18, FR21, FR23-FR25, FR28, FR30, FR31, FR34; NFR1-NFR8, NFR11; UX-DR25-UX-DR30, UX-DR42, UX-DR48; AD-7, AD-8, AD-11-AD-15, AD-17, AD-21, AD-27 |
| OwnedClauses | FR2.current-mirror-public-contract; FR9.Blocked-context-mode; FR10.Indeterminate-and-content-free-failure-status; FR12.additive-safe-reasons; FR21.fail-closed-membership; FR28.register-vs-runtime-vocabulary; AD-7.current-mirror-additive-contract; AD-11.complete-safe-context-budget |
| Dependencies | Stories 5.8, 6.2, 6.3, and 6.6; EXT-CONV-AI-1, EXT-TOKEN-1, EXT-SAFETY-1, and EXT-PROTECTION-1 Available |
| EvidenceLevel | Levels 2 and 4: additive compatibility/policy behavior and live fail-closed integration paths |
| TestOrArtifact | RuntimeContractAdditiveMemberTests; CurrentMirrorContractCompatibilityTests; SafeContextBudgetEvidenceTests; MembershipGateContractTests; UnknownValueFailClosedTests; RegisterRuntimeVocabularyBoundaryTests |
| VerificationCommand | pwsh ./eng/verify-story-6.8.ps1 |
| NegativeEvidence | MissingBudgetTermCannotBecomeContextReadyTests; UnknownRuntimeValueCannotPassTests; RegisterOnlyBlockerDoesNotLeakIntoAgentAggregateTests |
| Result | Blocked — backlog; required external seams are currently Uncommitted |

## Epic 7: Complete Confirmation And Approval

An Approver can discover, revise, regenerate, resolve, and post exactly one proposal version without losing history or bypassing current gates.

**Status:** active forward backlog.

**Story count:** 7.

**Dependency topology:** 7.1 creates the independently usable pending proposal and discovery surfaces; 7.2 adds immutable editing; 7.3 regenerates under current gates; 7.4 approves/posts one selected version; 7.5 resolves without posting; 7.6 owns deterministic expiry and races; 7.7 reconciles detail/queue behavior with the complete ten-state transition source and `PostingFailed` recovery. Each transition depends only on already available proposal state and earlier runtime foundations.

### Story 7.1: Create And Discover A Pending Proposal

As an Approver,
I want successful confirmation-mode generation to create one discoverable pending proposal,
So that generated content can wait outside the Conversation for authorized review.

**Primary Demonstrable Outcome:** One successful confirmation-mode generation creates exactly one pending proposal with an immutable initial version, pending count, queue entry, and Conversation status reference visible only to authorized Approvers.

**Dependencies:**

- **Prior stories:** 5.4, 5.8, 6.1 through 6.5, and 6.8.
- **External:** `EXT-PROTECTION-1` plus consumed Provider, tokenizer, safety, secret, and topology seams must be Available for live generation; `EXT-CONV-AI-1` must be Available for Facilitator/roster/existence/access discovery, although no posting seam is used; `EXT-PARTIES-1` must be Available for current human/liveness classification and historical `PartyId`-to-`AuthenticatedHumanActorId` binding.
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
**And** no failed bytes or live failure-content record is retained; authorized operational/audit surfaces expose only the content-free interaction status/reason and Audit Evidence, with output-safety status supplied by the approved Product decision.

**Given** current approval authority is missing, stale, ambiguous, revoked, unavailable, or belongs to another tenant
**When** proposal discovery executes
**Then** records and counts fail closed without disclosing proposal existence, content, source Conversation, caller, expiry, or policy basis
**And** every Party-bearing Approver principal resolves through `EXT-PARTIES-1` to one current human Party and the same stable authenticated actor, with the durable binding version recorded; missing, stale, ambiguous, overlapping, non-human, or mismatched evidence denies without leaking a target-tenant value.

**Evidence Manifest:**

| Field | Story 7.1 evidence |
| --- | --- |
| Requirements | FR7, FR13, FR14, FR18-FR25, FR27-FR29, FR33, FR34; NFR1-NFR5, NFR11, NFR13; UX-DR1, UX-DR2, UX-DR6, UX-DR9, UX-DR22, UX-DR28-UX-DR30, UX-DR34, UX-DR36, UX-DR47, UX-DR48, UX-DR50; AD-4, AD-5, AD-8, AD-12-AD-15, AD-17, AD-18, AD-22, AD-29; EXT-PROTECTION-1, EXT-CONV-AI-1, EXT-PARTIES-1 |
| OwnedClauses | FR13.success-only-proposal; FR13.in-product-discovery; FR14.initial-immutable-version; FR18.stored-expiry; FR20.discovery-authorization; NFR3.no-lost-or-duplicate-version; NFR13.accessible-localized-queue; UX-DR47.in-product-only-notification; UX-DR48.failed-generation-no-proposal; AD-5.append-only-proposal-state |
| Dependencies | Stories 5.4 and 5.8; Stories 6.1-6.5 and 6.8; EXT-PROTECTION-1, EXT-CONV-AI-1, EXT-PARTIES-1, and all executed external seams Available |
| EvidenceLevel | Levels 2 and 4: proposal/projection behavior and live confirmation-generation-to-queue path |
| TestOrArtifact | AgentsIdentityProposalTests; ProposalCreationAggregateTests; ProposalDiscoveryHumanActorBindingIntegrationTests; PendingProposalProjectionTests; ConfirmationProposalIntegrationTests; ProposalQueueComponentTests; initial proposal evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-7.1.ps1 |
| NegativeEvidence | ProposalDiscoveryIsolationTests.CrossTenantCountsRowsAndStatusDiscloseNothing; MissingStaleNonHumanOrMismatchedProposalActorBindingTests; FailedGenerationCreatesNoProposalTests; DuplicateProposalAndVersionReplayTests |
| Result | Blocked — backlog; requires prior runtime stories and EXT-PROTECTION-1/EXT-CONV-AI-1/EXT-PARTIES-1 plus all executed seams Available |

### Story 7.2: Edit An Immutable Proposal Version

As an Approver,
I want an edit to create a new immutable proposal version,
So that I can correct the draft without overwriting what **hexa** generated.

**Primary Demonstrable Outcome:** One authorized edit appends one new version with authorship and source linkage; the original generated version remains intact and selectable for authorized comparison.

**Dependencies:**

- **Prior stories:** 5.8 and 7.1.
- **External:** `EXT-PROTECTION-1` remains required wherever protected content is materialized, `EXT-CONV-AI-1` must be Available for current Approver roster/access resolution before edit, and `EXT-PARTIES-1` must be Available for current human/liveness classification and historical actor binding.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** a pending nonterminal proposal and current edit authority
**When** the Approver submits edited content at the expected proposal revision
**Then** after closed content-free ingress/owner checks, `ProposalMutation:ReserveUserActionIntent` atomically records the deterministic `Reserved` lease plus a target-limited immutable action-intent outbox carrying original signed human evidence, operation/source version/idempotency/revisions, and edited bytes sealed under the interaction DEK; same-owner commit wins before any protected version, current Approver roster/access, Parties, or other target dependency read
**And** only that committed intent permits the edit or its typed failure to append. The aggregate assigns the next `VersionOrdinal` and `AgentsIdentity` derives one immutable edited `ProposalVersionId` from (`AgentInteractionId`, `VersionOrdinal`, edited kind), with editor PartyId, timestamp, source VersionId, policy basis, and protected content
**And** `EXT-PARTIES-1` must resolve the Party-bearing Approver to one current human Party and the same stable authenticated actor, record its durable binding version, and fail closed on missing, stale, ambiguous, overlapping, non-human, or mismatched evidence; every prior generated/edited/regenerated version remains unchanged and addressable to authorized users.

**Given** the edit API crashes after intent commit but before target acknowledgement
**When** the exact Interaction Workflow recovers the outbox
**Then** it re-evaluates the same current action dependencies, records only the intent-bound edit or typed negative result, preserves the original human as initiator and itself as recovery executor, acknowledges the outbox, and settles that lease
**And** it cannot change content, source version, actor, operation, idempotency identity, or expected revisions, impersonate fresh human authority, or create a second version.

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
| Requirements | FR7, FR14, FR15, FR19-FR24, FR29, FR33, FR34; NFR1-NFR5, NFR13; UX-DR7, UX-DR8, UX-DR22, UX-DR28, UX-DR31-UX-DR35, UX-DR37-UX-DR40, UX-DR50; AD-4, AD-5, AD-8, AD-12-AD-15, AD-22, AD-29; EXT-PROTECTION-1, EXT-CONV-AI-1, EXT-PARTIES-1 |
| OwnedClauses | FR14.preserve-all-versions; FR15.authorized-edit-only; FR15.edit-remains-outside-conversation; FR20.current-edit-authority; FR24.edit-authorship-evidence; NFR3.version-not-overwritten; UX-DR8.complete-version-history; UX-DR35.keyboard-editor; AD-5.immutable-edit-version |
| Dependencies | Stories 5.8 and 7.1; EXT-PROTECTION-1, EXT-CONV-AI-1, and EXT-PARTIES-1 Available |
| EvidenceLevel | Levels 2 and 4: aggregate/concurrency behavior plus live editor/public-contract path |
| TestOrArtifact | AgentsIdentityProposalVersionTests; ProposalEditAggregateTests; ProposalEditHumanActorBindingIntegrationTests; ProposalEditIntentOutboxRecoveryTests; ProposalEditConcurrencyTests; ProposalVersionHistoryQueryTests; ProposalEditorComponentTests; edit evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-7.2.ps1 |
| NegativeEvidence | ProposalEditIsolationTests.CrossTenantEditAndHistoryAreDenied; MissingStaleNonHumanOrMismatchedEditActorBindingTests; ProtectedEditReadCannotPrecedeIntentLeaseCommitTests; EditApiCrashCannotLoseOrChangeCommittedIntentTests; ClosingBeforeAfterEditIntentCommitTests; TerminalProposalEditTests; PriorVersionOverwriteGuardTests; raw-content telemetry poison sweep |
| Result | Blocked — backlog; requires Story 7.1 and EXT-PROTECTION-1/EXT-CONV-AI-1/EXT-PARTIES-1 are currently Uncommitted |

### Story 7.3: Regenerate Under Fresh Gates

As an Approver,
I want regeneration to re-evaluate current context, safety, Provider, cost, and capacity gates,
So that a new version cannot reuse stale authority or changed inputs under an old attempt.

**Primary Demonstrable Outcome:** One authorized regeneration creates one new immutable generated version from a freshly authorized complete-context attempt, while any changed or failed gate leaves existing proposal history intact and performs no unsafe effect.

**Dependencies:**

- **Prior stories:** 5.8, 7.1, and 7.2.
- **External:** EXT-PROTECTION-1, EXT-PROVIDER-1, EXT-TOKEN-1, EXT-SAFETY-1, EXT-SECRETS-1, EXT-CONV-AI-1, EXT-PARTIES-1, and the production-like capacity seam must be Available when executed.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** a pending proposal and current regeneration authority
**When** regeneration is requested
**Then** after closed content-free ingress/owner checks, `ProposalMutation:ReserveUserActionIntent` atomically records the deterministic `Reserved` lease plus an immutable target-limited regeneration intent/outbox with original signed human evidence, base version, operation/idempotency identity, and expected revisions; same-owner commit wins before the first current Approver/Conversation/Parties or protected-version read. The original request or exact Interaction Workflow recovery consumes only that committed intent, records the original human plus recovery executor separately, and must acknowledge the outbox and settle the lease after the intent-bound positive/negative result
**And** before Provider work the aggregate resolves at least one currently eligible Approver other than the caller, last editor, and requesting Party; if none exists, regeneration is rejected without an attempt, reservation, admission, or Provider call
**And** `EXT-PARTIES-1` classifies each Party-bearing requester/candidate as current human, binds it to the same stable authenticated actor with a durable binding version, and rejects missing, stale, ambiguous, overlapping, non-human, or mismatched evidence; at the expected revision the aggregate atomically appends `RegenerationAuthorized` carrying `RequestingPartyId`, `BaseVersionId`, `AttemptId`, approver-policy version, and the resolved eligible Party set, and only that fact permits the workflow to repeat current tenant/Conversation authorization, complete Conversation read, exact token measurement, Provider capability high-water/readiness, prompt/context safety, pricing/budget reservation, shared capacity admission, and prepared-attempt authorization
**And** the aggregate assigns the next `AttemptOrdinal`, `AgentsIdentity` derives the new `AttemptId`, and no client-provided seed or local identity helper can affect it.

**Given** every fresh gate passes and the Provider output passes current output safety
**When** the result is recorded
**Then** the aggregate assigns the next `VersionOrdinal` and `AgentsIdentity` derives one immutable regenerated `ProposalVersionId` from (`AgentInteractionId`, `VersionOrdinal`, regenerated kind), with attempt, Provider/model/effective version, safety, cost, and source references
**And** the version records `RegenerationRequestedByPartyId`, all earlier generated and edited versions remain intact, and no Conversation Message is created.

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
| Requirements | FR7, FR9, FR10, FR14, FR16, FR19-FR21, FR24-FR29, FR31-FR34; NFR1-NFR12; UX-DR7, UX-DR8, UX-DR22, UX-DR27, UX-DR28, UX-DR31, UX-DR36, UX-DR43, UX-DR48, UX-DR50; AD-4, AD-5, AD-8-AD-14, AD-17, AD-18, AD-20-AD-22, AD-24, AD-29; EXT-PROTECTION-1, EXT-CONV-AI-1, EXT-PARTIES-1 |
| OwnedClauses | FR7.regeneration-requester-ineligible-for-resulting-version; FR7.second-eligible-approver-guard-before-provider; FR16.same-source-and-snapshot-provenance; FR16.new-version-preserves-history; FR16.terminal-block; FR27.fresh-no-weaker-safety; NFR8.complete-context-revalidation; NFR10.regeneration-reservation-reuse; NFR11.no-duplicate-attempt-or-version; UX-DR48.failed-regeneration-not-version; AD-8.atomic-regeneration-authorization; AD-10.high-water-recheck; AD-13.changed-input-fails-attempt |
| Dependencies | Stories 5.8 and 7.1-7.2; EXT-PROTECTION-1, EXT-PROVIDER-1, EXT-TOKEN-1, EXT-SAFETY-1, EXT-SECRETS-1, EXT-CONV-AI-1, EXT-PARTIES-1, and capacity seam Available |
| EvidenceLevel | Levels 2, 4, and 5: transition logic, live adapters, production-like retry/concurrency path |
| TestOrArtifact | AgentsIdentityRegenerationTests; ProposalRegenerationAggregateTests; RegenerationHumanActorBindingIntegrationTests; RegenerationUserActionIntentOutboxTests; RegenerationApproverSegregationTests; RegenerationAuthorizationAtomicityTests; RegenerationFreshGateIntegrationTests; RegenerationRetryRecoveryTests; ProposalVersionHistoryTests; regeneration evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-7.3.ps1 |
| NegativeEvidence | RegenerationIsolationTests.CrossTenantRegenerationIsDeniedBeforeProvider; MissingStaleNonHumanOrMismatchedRegenerationActorBindingTests; RegenerationDependencyReadCannotPrecedeIntentLeaseCommitTests; RegenerationApiCrashCannotLoseOrChangeCommittedIntentTests; ClosingBeforeAfterRegenerationIntentCommitTests; RegenerationRequesterCannotApproveGeneratedVersionTests; NoSecondEligibleApproverBlocksBeforeProviderTests; MissingRegenerationAuthorizedFactBlocksProviderTests; StaleChangedFingerprintAndWeakerPolicyTests; TerminalExpiryRaceAndDuplicateVersionTests |
| Result | Not run — backlog; required external seams are currently Uncommitted |

### Story 7.4: Approve And Post One Selected Version

As an Approver,
I want to approve and post exactly one selected proposal version,
So that only the reviewed response becomes a Conversation Message attributed to **hexa**.

**Primary Demonstrable Outcome:** One authorized approval selects one immutable VersionId, records a non-success posting-pending state, and produces exactly one authoritative posted Conversation Message with complete approval evidence.

**Dependencies:**

- **Prior stories:** 5.8 and 7.1; 7.2 and 7.3 versions are supported when present.
- **External:** EXT-PROTECTION-1, EXT-SAFETY-1, EXT-CONV-AI-1, and EXT-PARTIES-1 must be Available for live approval/posting and current/historical human actor binding. `ARCH-A-14` must be retired by a configured posting timeout no shorter than the exact committed seam-2 timeout before this story can become ready-for-dev.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** a pending proposal, selected existing VersionId, and current approval authority
**When** approval executes at the expected EventStore revision
**Then** closed content-free ingress and owner-local identity/shape/revision/barrier checks run first, after which `ProposalMutation:ReserveUserActionIntent` atomically appends a deterministic `Reserved` lease plus immutable target-limited approval intent/outbox with original signed human evidence, selected VersionId, operation/idempotency identity, and expected revisions. Same-owner commit wins before any protected version read, approval-safety call, current Conversation/Parties read, or other target dependency read. Current tenant, Party, approver-policy basis, Conversation access, output-safety floor, Agent identity, and operation-gate results are bound to that committed lease before the mutation or its typed failure is recorded
**And** `EXT-PARTIES-1` classifies the Party-bearing Approver as current human and binds it to the same stable authenticated actor with a durable binding version; missing, stale, ambiguous, overlapping, non-human, or mismatched evidence fails closed, a Party recorded as `RegenerationRequestedByPartyId` on the selected version is ineligible to approve it, and exactly one approval event binds selected VersionId, Approver PartyId, policy basis, timestamp, and `Approved` state only; no other version is eligible and this event never emits `PostingPending`.

**Given** the approval API crashes after the User-action intent commit and before the target result or directory acknowledgement
**When** the exact Interaction Workflow recovers
**Then** it re-evaluates the same current approval dependencies and records only that intent-bound `Approved` or typed negative result, preserving the original human as initiator and the Workflow as recovery executor; target acknowledgement then consumes the intent outbox and permits only that ProposalMutation lease to settle
**And** recovery cannot select another version, actor, action, safety result, idempotency identity, or expected revision, cannot manufacture fresh human eligibility, and never begins posting before the separately committed `PostingPending` protocol.

**Given** approval is accepted
**When** UI/API render the result
**Then** approved and posting pending remain non-success authoritative progress with accepted identity and projection/version
**And** only a later authoritative posted projection may use Success or claim a Conversation Message.

**Given** `Approved` is committed at the expected proposal revision
**When** the workflow is ready to validate and attempt the Conversations append
**Then** after closed content-free owner-local checks it first reserves and commits the deterministic `ConversationPosting` lease; full pre-post safety plus Conversation existence/access/membership reads occur only inside that committed lease and bind either a state-legal no-post result or permission to continue. When validation passes, a separate deterministic `BeginPosting` command commits `PostingPending` with `PostingAttemptId`, deterministic `MessageId`, immutable approved VersionId, stored deadline, and lease commit revision at the expected interaction revision
**And** only that committed fact authorizes `AppendMessage`; a later command records success/failure or the exact `MessageId` lost-ack lookup outcome, and no command or policy may emit approval, posting-pending, and result as one batch.

**Given** current limited AI membership and posting gates pass through EXT-CONV-AI-1
**When** the workflow appends the selected version
**Then** deterministic MessageId/idempotency derived from interaction plus selected VersionId produces exactly one message authored by the Agent Party identity
**And** audit evidence links caller, Agent, source, selected version, Provider/model, safety, Approver, policy basis, approval time, and final MessageId.

**Given** approval/posting is duplicated, replayed, times out, crashes, races another resolution, or the selected version/gate becomes invalid
**When** recovery executes
**Then** EventStore concurrency chooses one valid ordering, terminal or invalid state cannot post, authoritative Conversations outcome prevents duplicate messages, and failure remains distinct from approval
**And** no alternate version, caller-authored fallback, or direct Conversation stream write occurs.

**Given** a `PostingFailed` proposal before any retry or other exit
**When** Story 7.4's seam-2 lookup reads the deterministic `MessageId`
**Then** a present message records `Posted` with `LateConfirmed` and rejects the requested action; unavailable refuses the exit and leaves `PostingFailed`; typed absence permits retry only after full current re-validation
**And** typed `ConversationDeleted` or `PrincipalRemovedFromConversation` counts as no post only for an authorized abandon path.

**Given** tenant B or an unauthorized Party targets tenant A proposal
**When** approve, membership, posting, status, or audit paths execute
**Then** every path denies before mutation or disclosure
**And** proposal/version existence, policy basis, MessageId, Conversation membership, counts, and accessible output remain undisclosed.

**Evidence Manifest:**

| Field | Story 7.4 evidence |
| --- | --- |
| Requirements | FR2, FR7, FR14, FR17, FR19-FR25, FR27, FR28; OQ-26; NFR1-NFR7, NFR9, NFR11, NFR13, NFR14; UX-DR7, UX-DR8, UX-DR11, UX-DR12, UX-DR22, UX-DR28, UX-DR31-UX-DR40, UX-DR50; AD-4-AD-8, AD-12-AD-15, AD-17, AD-18, AD-20, AD-22, AD-23, AD-25, AD-26; EXT-PROTECTION-1; EXT-SAFETY-1; EXT-CONV-AI-1; EXT-PARTIES-1 |
| OwnedClauses | FR7.regeneration-requester-exclusion-at-approval; FR17.approve-exact-selected-version; FR17.agent-attribution; FR17.complete-approval-post-link; FR18.MessageId-check-before-postingfailed-exit; FR18.LateConfirmed; FR20.current-approval-authorization; NFR3.no-partial-or-duplicate-post; NFR5.approval-path-audit; NFR11.no-duplicate-post-on-recovery; UX-DR22.approved-not-posted; UX-DR50.approved-posting-posted-truth; AD-8.version-specific-eligible-approver; AD-13.deterministic-selected-version-post |
| Dependencies | Stories 5.8 and 7.1; optional earlier versions from 7.2-7.3; EXT-PROTECTION-1, EXT-SAFETY-1, EXT-CONV-AI-1, and EXT-PARTIES-1 Available |
| EvidenceLevel | Levels 2, 4, and 5: approval transition, live membership/posting, production-like race/recovery proof |
| TestOrArtifact | ProposalApprovalAggregateTests; ProposalApprovalHumanActorBindingIntegrationTests; ProposalApprovalIntentOutboxRecoveryTests; DurableBeforePostOrderingTests; ApprovalAndPostingEffectLeaseSequenceParityTests; BeginPostingAggregateTests; RegenerationRequesterApprovalExclusionTests; ApprovedVersionPostingIntegrationTests; ApprovalPostingRecoveryTests; ApprovalUiStateTests; approval/post audit-completeness manifest |
| VerificationCommand | pwsh ./eng/verify-story-7.4.ps1 |
| NegativeEvidence | ProposalApprovalIsolationTests.CrossTenantApprovalPostingAndAuditAreDenied; MissingStaleNonHumanOrMismatchedApprovalActorBindingTests; ApprovalApiCrashCannotLoseOrChangeCommittedIntentTests; ClosingEffectiveImmediatelyBeforeAndAfterApprovalIntentApprovalSafetyPrePostSafetyConversationReadBeginPostingAndAppendTests; ProtectedDependencyReadCannotPrecedeEffectCommitTests; ExternalAppendBeforePostingPendingTests; ApprovalPostingResultCannotShareOneCommandTests; TerminalRaceAfterPostingPendingTests; WrongVersionAndDuplicatePostTests; ApprovalExpiryConcurrencyTests; ApprovedIsNotPostedUiTests |
| Result | Blocked — backlog; EXT-PROTECTION-1, EXT-SAFETY-1, EXT-CONV-AI-1, and EXT-PARTIES-1 are currently Uncommitted, and ARCH-A-14 is unretired |

### Story 7.5: Reject Or Abandon A Proposal

As an Approver,
I want to resolve a proposal without posting it,
So that rejected or intentionally abandoned content becomes terminal and can never enter the Conversation.

**Primary Demonstrable Outcome:** One authorized non-posting resolution records either Rejected or Abandoned as the single terminal outcome, preserves all versions, and permanently disables edit, regenerate, approve, and post actions.

**Dependencies:**

- **Prior stories:** 7.1 and Story 7.4's seam-2 `MessageId` lookup capability.
- **External:** `EXT-CONV-AI-1` must be Available for current Approver roster/access resolution and, when a `PostingFailed` abandon is requested after any post attempt, the typed `MessageId` existence read; `EXT-PARTIES-1` must be Available for current/historical human actor binding.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** a pending proposal and current resolution authority
**When** the Approver rejects it with policy-required safe rationale metadata
**Then** after closed content-free ingress/owner checks, `ProposalMutation:ReserveUserActionIntent` atomically records the deterministic `Reserved` lease and immutable target-limited rejection intent/outbox with original signed human evidence, sealed protected rationale, operation/idempotency identity, and expected revisions; same-owner commit precedes current roster/access/Parties or protected-content reads, and EventStore then records one intent-bound Rejected terminal decision with actor, policy basis, timestamp, and protected evidence
**And** `EXT-PARTIES-1` classifies the Party-bearing actor as current human and binds it to the same stable authenticated actor with a durable binding version, failing closed on missing, stale, ambiguous, overlapping, non-human, or mismatched evidence; all proposal versions remain preserved while edit, regenerate, approve, membership, and posting become impossible.

**Given** a pending proposal and current resolution authority
**When** the Approver abandons it
**Then** the same reserve-intent/commit protocol records an immutable abandonment intent before dependency reads, and EventStore records one intent-bound Abandoned terminal decision with actor, policy basis, timestamp, and protected evidence
**And** the outcome is distinct from Rejected, Expired, generation failure, and posting failure.

**Given** the resolution API crashes after intent commit but before the target result or acknowledgement
**When** the exact Interaction Workflow recovers
**Then** it may record only the original intent-bound Rejected, Abandoned, or typed negative result, records original human initiator and Workflow executor separately, acknowledges that outbox, and settles only that lease
**And** it cannot switch reject to abandon, change rationale/actor/version/revisions, impersonate current human authority, bypass the `MessageId` lookup, or create a second terminal outcome.

**Given** an authorized abandon request for a `PostingFailed` proposal
**When** the Story 7.4 seam-2 lookup reads its deterministic `MessageId`
**Then** a present message records `Posted` with `LateConfirmed` and never `Abandoned`; unavailable refuses the exit and leaves `PostingFailed`; typed absence or a typed deleted/removed result permits only the PRD-authorized abandon path
**And** a reject or abandon re-validates the actor against the current `ProposalResolution` authorization contract and expected aggregate revision, but membership loss, a safety failure, disabled Agent, or pulled kill switch is evidence supporting a no-post terminal resolution rather than a success gate that can strand it; full Conversation-posting membership/safety revalidation applies only to a retry or post attempt.

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
| Requirements | FR7, FR14, FR18-FR25, FR29, FR33, FR34; OQ-26; NFR1-NFR5, NFR13, NFR14; UX-DR7, UX-DR8, UX-DR11, UX-DR12, UX-DR22, UX-DR28, UX-DR31-UX-DR40, UX-DR50; AD-5, AD-8, AD-12-AD-15, AD-17, AD-25, AD-26; EXT-CONV-AI-1; EXT-PARTIES-1 |
| OwnedClauses | FR18.rejected-terminal; FR18.abandoned-terminal; FR18.MessageId-check-before-postingfailed-exit; FR18.terminal-cannot-post; FR24.non-posting-resolution-evidence; NFR3.versions-preserved; NFR5.terminal-audit; NFR13.accessible-confirmed-resolution; UX-DR22.distinct-terminal-states; AD-5.single-terminal-order |
| Dependencies | Stories 7.1 and 7.4 lookup capability; EXT-CONV-AI-1 and EXT-PARTIES-1 Available |
| EvidenceLevel | Levels 2 and 4: terminal/concurrency behavior and live public-contract/UI path |
| TestOrArtifact | ProposalNonPostingResolutionTests; ProposalResolutionHumanActorBindingIntegrationTests; ProposalResolutionIntentOutboxRecoveryTests; ProposalResolutionConcurrencyTests; ProposalResolutionUiTests; SafetyFailureAbandonmentTests; MembershipLossAbandonmentTests; terminal evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-7.5.ps1 |
| NegativeEvidence | ProposalResolutionIsolationTests.CrossTenantRejectAbandonAndHistoryAreDenied; MissingStaleNonHumanOrMismatchedResolutionActorBindingTests; ResolutionDependencyReadCannotPrecedeIntentLeaseCommitTests; ResolutionApiCrashCannotLoseOrChangeCommittedIntentTests; ClosingBeforeAfterResolutionIntentCommitTests; TerminalMutationGuardTests; ConcurrentApproveRejectAbandonTests; SafetyOrMembershipFailureCannotStrandAbandonTests; RetryStillRequiresFullPostingRevalidationTests |
| Result | Blocked — backlog; requires Story 7.1 and EXT-CONV-AI-1/EXT-PARTIES-1 are currently Uncommitted |

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

**Given** an awaiting-decision proposal reaches `ExpiresAt` while the tenant kill switch is pulled
**When** the durable expiry transition wins
**Then** it records `Expired` with `ExpiredWhileSuspended`
**And** that proposal is excluded from the SM-3 and SM-C5 denominators exactly as the PRD measurement contract requires.

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
| Requirements | FR18-FR25, FR28; OQ-27; NFR1-NFR5, NFR11, NFR13, NFR14; UX-DR6-UX-DR8, UX-DR22, UX-DR28, UX-DR32-UX-DR40, UX-DR47, UX-DR50; AD-3-AD-5, AD-8, AD-12, AD-13, AD-17, AD-18, AD-23, AD-25, AD-26 |
| OwnedClauses | FR18.default-configurable-future-only-expiry; FR18.durable-timer-at-or-after-ExpiresAt; FR18.expired-cannot-post; FR18.ExpiredWhileSuspended-and-metric-exclusion; NFR3.versions-survive-expiry; NFR11.no-duplicate-timer-or-terminal-decision; NFR13.accessible-localized-expiry; UX-DR22.expired-distinct-terminal; AD-5.expiry-state; AD-23.timer-recovery-inventory |
| Dependencies | Stories 6.1 and 7.1; existing platform Dapr Workflow/topology |
| EvidenceLevel | Levels 2, 4, and 5: timer/concurrency logic, live Dapr Workflow, production-like restart/race proof |
| TestOrArtifact | ProposalExpiryPolicyTests; ProposalExpiryWorkflowIntegrationTests; ProposalExpiryRaceTests; ProposalExpiryUiTests; timer/recovery evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-7.6.ps1 |
| NegativeEvidence | ProposalExpiryIsolationTests.CrossTenantPolicyAndProposalDataAreDenied; DuplicateTimerAndPostAfterExpiryTests; ExpiryApproveRegenerateRaceMatrixTests |
| Result | Not run — backlog; requires Stories 6.1 and 7.1 |

### Story 7.7: Restore Posting-Failed Recovery And Proposal-State Parity

As an Approver,
I want proposal detail and queue actions to reflect the authoritative ten-state lifecycle,
So that a failed post can be resolved without misreporting a confirmed post or a terminal proposal.

**Owner:** Agents Runtime Maintainer.

**Primary Demonstrable Outcome:** Proposal detail, queue, domain policy, projections, and API/client use one FR-18 transition source in which `PostingFailed` is recoverable, `Posted` is confirmed and terminal, and no retry or abandon contradicts the Conversation.

**Dependencies:**

- **Prior stories:** 6.8 and 7.1 through 7.6.
- **External:** `EXT-CONV-AI-1`, `EXT-SAFETY-1`, and `EXT-PROTECTION-1` must be Available for live existence lookup, pre-post safety, and protected proposal materialization.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** any proposal projection or UI component
**When** its state contract is constructed
**Then** it uses exactly `Pending`, `Edited`, `Regenerated`, `Approved`, `PostingPending`, `PostingFailed`, `Posted`, `Rejected`, `Abandoned`, and `Expired`, with `Unknown` never recorded
**And** only `Posted`, `Rejected`, `Abandoned`, and `Expired` are terminal; the queue/detail terminal sets and pending/non-terminal indexes derive from the same source.

**Given** a `PostingFailed` proposal whose failure permits retry
**When** an authorized Tenant Agent Administrator requests administrative retry
**Then** API, BFF, and UI consume one server `CanAdministrativeRetry` verdict covering a remaining shared attempt, the adjusted 15-minute deadline, `PostingWindowElapsed`, non-safety failure reason, active Agent, unsuspended tenant, and usable latest `MessageId` lookup; a permitted action re-runs full current pre-post validation and consumes one audited attempt
**And** an administrator may consume any remaining shared attempt without waiting for an automatic sub-budget, while safety verdict, exhausted or elapsed bounds, disabled/suspended state, or unknown/unavailable lookup or validation leaves `PostingFailed` unchanged.

**Given** automatic and administrative retry are submitted concurrently
**When** both name the same expected proposal revision
**Then** EventStore concurrency accepts at most one retry authorization and only that winner consumes one shared attempt
**And** every loser consumes no attempt, performs no Conversation post, and refreshes `CanAdministrativeRetry` from authoritative state.

**Given** a retry or abandon after a post may have been attempted
**When** the existence read finds the deterministic `MessageId`
**Then** the proposal records `Posted` with `LateConfirmed`, rejects the requested retry/abandon, and displays authoritative confirmed-post status
**And** typed absence alone permits retry, while the PRD typed deleted/removed answers permit only the corresponding guarded abandon path.

**Given** the tenant becomes `Suspended`
**When** proposal actions and workflow continuations are evaluated
**Then** an `Approved` proposal remains `Approved` without starting a post, an existing `PostingPending` attempt completes or fails, `PostingFailed` retries pause, and awaiting proposals accept reject/abandon while `ExpiresAt` continues
**And** expiry during suspension records `ExpiredWhileSuspended`; the switch never system-abandons or deletes the proposal or evidence.

**Given** keyboard, localized, reduced-motion, restrictive-viewport, replay, concurrency, timeout, and cross-tenant cases
**When** detail and queue actions execute
**Then** permitted actions, badges, live regions, focus behavior, audit, and authoritative state remain consistent across UI/API/domain
**And** no stale terminal set, direct Conversation write, duplicate post, content leak, or unauthorized existence signal remains.

**Evidence Manifest:**

| Field | Story 7.7 evidence |
| --- | --- |
| Owner | Agents Runtime Maintainer |
| Requirements | FR7, FR13-FR25, FR27-FR31, FR33, FR34; NFR1-NFR7, NFR9, NFR11, NFR13, NFR14; UX-DR6-UX-DR8, UX-DR22, UX-DR28, UX-DR31-UX-DR40, UX-DR47, UX-DR50; AD-5, AD-8, AD-12-AD-15, AD-17, AD-20, AD-22, AD-25, AD-26; EXT-CONV-AI-1 |
| OwnedClauses | FR18.ten-recorded-states; FR18.PostingFailed-nonterminal-recovery; FR18.shared-automatic-administrative-retry-bound; FR18.server-CanAdministrativeRetry-verdict; FR18.concurrent-retry-single-winner; FR18.MessageId-before-exit; FR18.LateConfirmed; FR18.kill-switch-transition-behavior; UX-DR22.posted-only-success |
| Dependencies | Story 6.8 and Stories 7.1-7.6; EXT-CONV-AI-1, EXT-SAFETY-1, and EXT-PROTECTION-1 Available |
| EvidenceLevel | Levels 2, 4, and 5: transition/UI behavior, live Conversations lookup/posting, and production-like suspension/recovery proof |
| TestOrArtifact | ProposedAgentReplyTransitionTableTests; ProposalDetailPostingFailedRecoveryTests; CanAdministrativeRetryContractTests; AutomaticAdministrativeRetryConcurrencyTests; ProposalQueueStateParityTests; LateConfirmedMessageLookupTests; KillSwitchProposalStateTests; ProposalStateApiUiParityTests |
| VerificationCommand | pwsh ./eng/verify-story-7.7.ps1 |
| NegativeEvidence | PostingFailedIsNotTerminalTests; PostedCannotRetryOrAbandonTests; MissingOrUnavailableMessageLookupCannotExitTests; CrossTenantProposalRecoveryDisclosesNothingTests |
| Result | Blocked — backlog; required external seams are currently Uncommitted |

## Epic 8: Governance Operations And Release Qualification

Authorized governance and release operators can retain, hold, export, delete, and govern sensitive Agent data and policies, calculate qualification metrics under versioned contracts, and inspect current launch blockers without confusing deterministic implementation evidence with live release attainment.

**Status:** active forward backlog.

**Story count:** 8.

**Dependency topology:** Stories 8.1 and 8.4 begin from completed active capabilities in Epics 5–7; Stories 8.2 and 8.3 branch independently from 8.1; Story 8.5 consumes prior runtime/product evidence; Story 8.6 consumes the completed interactive surfaces; Story 8.7 inspects the bounded evidence produced through 8.6; Story 8.8 owns durable compliance inspection over protected evidence. `EXT-PROTECTION-1` gates 8.1–8.3 and 8.8, `EXT-SECRETS-1` gates 8.1–8.4, and `EXT-PARTIES-1` gates the human-identity evidence in 8.1–8.4 and 8.8. `EXT-CONV-AI-1` gates Story 8.3's mandatory Conversation-deletion propagation. `EXT-EXPORT-STORE-1` always gates Story 8.2 and gates Stories 8.1/8.3 only when their frozen sets contain committed/export lifecycle artifacts or copies; `EXT-TOPOLOGY-1` gates 8.5–8.8. No Epic 8 story depends on `RQ-1`.

### Story 8.1: Retain Sensitive Content And Apply Legal Holds

As a Compliance Inspector,
I want sensitive Agent content to follow one durable retention and legal-hold policy,
So that protected evidence remains available exactly while policy requires and can expire safely when no hold applies.

**Primary Demonstrable Outcome:** One terminal interaction receives an immutable 365-day sensitive-content retention deadline, and an authorized legal hold suspends expiry until its durable release without rewriting EventStore history.

**Dependencies:**

- **Prior stories:** 5.2 and 5.8 for authorized protected EventStore operations and 7.6 for deterministic proposal terminal timestamps.
- **External:** `EXT-HOST-1`, `EXT-PROTECTION-1`, `EXT-SECRETS-1`, and `EXT-PARTIES-1` must be `Available` for tenant scope-guard registration/release, live DEK pin/unpin, protected retention execution, and current/historical human actor-binding evidence. `GovernanceScopeV1:ClassUtcRange` additionally requires approved `OD-GOVERNANCE-CLASS-RANGE-SCOPE-1`; human-origin `ExactConversation` additionally requires approved `OD-HUMAN-EXACT-CONVERSATION-SCOPE-1`; exact-interaction scope remains evaluable. Only a frozen set containing committed export artifacts additionally requires `EXT-EXPORT-STORE-1` Available plus the exact approved `OD-EXPORT-LIFECYCLE-1` version for artifact pin/unpin; an armed-deletion overlap requires the exact approved `OD-HOLD-DELETION-PRECEDENCE-1` version.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** an interaction reaches an authoritative terminal state with protected prompt, context, generated, edited, proposal, or audit content
**When** retention is scheduled
**Then** the `retention` projection records an immutable deadline exactly 365 days after that terminal timestamp, the governing policy version, protected scope, and safe evidence references
**And** posted Conversation Messages remain under Hexalith.Conversations retention rather than Agents retention.

**Given** an authorized Compliance Inspector submits a legal hold for a named tenant-scoped protected scope
**When** EventStore accepts the deterministic `LegalHold` command
**Then** `LegalHold(TenantId, HoldId)` accepts canonical `GovernanceScopeV1` plus its predicate digest. Exact sorted interaction ids are computable from authoritative stream/directory owner facts; missing, malformed, cross-tenant, or unverifiable proof rejects. Human-origin `ExactConversation` rejects while `OD-HUMAN-EXACT-CONVERSATION-SCOPE-1` is Open because this story chooses no point-in-time/prospective cut, later-content treatment, fence lifetime, or public meaning. `ClassUtcRange` rejects while `OD-GOVERNANCE-CLASS-RANGE-SCOPE-1` is Open because this story chooses no class owner/vocabulary or time semantics. The hold appends a target-limited guard-registration authorization; only it may call `EXT-HOST-1` to conditionally record `HoldContenderRegistered` on `GovernanceScopeGuard(TenantId)`, serialized with every overlapping deletion seal. That guard commit—not request arrival or a projection—is the hold-contention instant. Exact lookup resolves loss and `RecordLegalHoldScopeGuardRegistration` mirrors either the registered contender or `DestructionAlreadySealed` to `ProtectionFence`
**And** a registered contender is immediately restrictive even while its mirror is missing. If no armed deletion overlaps, the hold resolves explicit interaction keys plus every committed artifact/key/index member, pins them, and becomes `Active` only after every acknowledgement. From the guard instant, expiry, purge, key destruction, provider TTL, backup expiry, and restore cleanup require current authority and retain bytes/keys on unavailable evidence; the append-only projection/UI follows `submitted -> authoritative pending -> projection-confirmed terminal`, exact duplicate is idempotent, and divergent duplicate conflicts.

**Given** the frozen hold set contains a committed export or overlaps `DeletionArmed`
**When** hold prepare or recovery is authorized
**Then** committed-artifact preparation requires the exact approved `OD-EXPORT-LIFECYCLE-1` version and exact `EXT-EXPORT-STORE-1` target, while a guard-registered armed-deletion overlap requires the exact approved `OD-HOLD-DELETION-PRECEDENCE-1` outcome and fence transition
**And** an open, missing, or mismatched decision leaves the guard contender restrictive and advances neither branch. A deletion-allowed outcome is consumed only in the same guard commit as `DestructionSealed`; a hold-allowed outcome leaves the contender in place. If sealing wins first, registration returns `DestructionAlreadySealed` and only the approved post-start outcome may advance. No story or adapter chooses precedence.

**Given** a hold attempt overlaps a deletion after `DestructionSealed` but before completion
**When** its guard registration executes
**Then** the guard appends authenticated `PostStartHoldContenderObserved(..., DestructionAlreadySealed)` at its expected revision and exact lookup/mirroring cannot turn that durable fact into absence
**And** while the exact post-start outcome/version of `OD-HOLD-DELETION-PRECEDENCE-1` is Open, missing, mismatched, unavailable, or not explicitly deletion-allowed for the step, every unconsumed accepted batch, new containment issue/consume, purge, and successful completion remains blocked. Already-consumed vectors remain immutable. A hold-allowed outcome grants no new destruction; no story chooses either Product result.

**Given** a Compliance Inspector submits release of an active hold at its expected revision
**When** a distinct second Compliance Inspector or the Platform Operator records audited approval under the AD-30 principal and role rules
**Then** only that approved release may unpin every named DEK and committed export artifact and append a release transition carrying submitter, approver, role bases, justification, scope, checkpoint, expected revision, and key outcomes. After every unpin and fence-release receipt is exact, a target-limited authorize/effect/result sequence releases the original `HoldContenderRegistered` fact at the same tenant guard; exact lookup resolves loss, and overlapping deletion remains blocked until `RecordLegalHoldScopeGuardReleased` mirrors the receipt
**And** the Party-bearing Compliance Inspector resolves through `EXT-PARTIES-1`; the only alternative approver is a `Platform` Operator proving current or historically recorded Hexalith.Tenants global-administrator authority without inventing a tenant Party. A Tenant Agent Administrator has no hold-release grant. Separation compares stable `AuthenticatedHumanActorId`; missing, stale, ambiguous, overlapping, non-human where a Party is required, unavailable, incompatible, same-actor, or wrong-role evidence blocks before approval or unpin, and partial failure remains restrictive.

**Given** a human hold-release decision is durable and one or more DEK/artifact unpin acknowledgements are missing after crash or lost acknowledgement
**When** recovery resumes
**Then** it is phase-pinned to the release decision revision, frozen set/token, and the effective `OD-EXPORT-LIFECYCLE-1` version plus exact store target recorded by that decision when committed artifacts overlap
**And** later pending/effective decision successors cannot strand, reopen, or retarget the release; direct dependency unavailability keeps the remaining members restrictive and retrying until exact outcome lookup and acknowledgements converge.

**Given** hold preparation remains reversible after crash or lost acknowledgement
**When** recovery is requested
**Then** the original still-valid hold intent, exact owner revision, same frozen set/token/versions, and absence of an authorized cancellation permit the owner to append only `PrepareRecoveryDispositionDecided(Resume)` and continue exact acknowledgements
**And** `Abort` requires the exact approved `OD-HOLD-PREPARE-CANCELLATION-1` version plus its recorded authorized cancellation fact; a pin, store, protection, timeout, or permanent failure leaves the hold restrictive pending and cannot unpin or release the fence.

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
| Requirements | FR18-FR25, FR28, FR30, FR33, FR34; OQ-30, OQ-34; NFR1-NFR5, NFR11, NFR13; UX-DR1, UX-DR9-UX-DR18, UX-DR29-UX-DR33, UX-DR36-UX-DR41, UX-DR46, UX-DR50; AD-1-AD-5, AD-8, AD-12, AD-13, AD-17, AD-20, AD-22, AD-23, AD-25, AD-26, AD-30; EXT-HOST-1; EXT-PROTECTION-1; EXT-SECRETS-1; EXT-PARTIES-1; conditional OD-GOVERNANCE-CLASS-RANGE-SCOPE-1 for class/range; conditional OD-HUMAN-EXACT-CONVERSATION-SCOPE-1 for human exact-Conversation; conditional EXT-EXPORT-STORE-1 and OD-EXPORT-LIFECYCLE-1 on committed-artifact overlap; conditional OD-HOLD-DELETION-PRECEDENCE-1 for either post-arm pre-seal contention or a post-start hold before completion; OD-HOLD-PREPARE-CANCELLATION-1 only for Abort |
| OwnedClauses | FR18.terminal-evidence-retained; FR19.retention-hold-tenant-isolation; FR20.current-governance-authorization; FR24.retention-hold-audit; FR24.legal-hold-release-second-party; FR28.audit-governance-active; PRD-OQ8.365-day-terminal-retention; PRD-OQ8.legal-hold-suspends-expiry; OQ30.distinct-release-approval; NFR11.timer-replay-no-duplicate-effect; UX-DR31.LegalHold-lock-scope; UX-DR40.LegalHold-restrictive-viewport-block; UX-DR46.retention-and-hold; AD-2.LegalHold; AD-22.two-phase-DEK-pinning; AD-30.platform-release-approval |
| Dependencies | Stories 5.2, 5.8, and 7.6; EXT-HOST-1, EXT-PROTECTION-1, EXT-SECRETS-1, and EXT-PARTIES-1 Available; tenant scope-guard registration/release exact lookup; OD-GOVERNANCE-CLASS-RANGE-SCOPE-1 only for class/range scope; OD-HUMAN-EXACT-CONVERSATION-SCOPE-1 only for human exact-Conversation scope; EXT-EXPORT-STORE-1 plus OD-EXPORT-LIFECYCLE-1 only when committed artifacts overlap; OD-HOLD-DELETION-PRECEDENCE-1 for post-arm pre-seal contention and every post-start hold branch through completion; OD-HOLD-PREPARE-CANCELLATION-1 only for Abort/unwind |
| EvidenceLevel | Levels 2 and 4: deterministic retention/hold behavior and live EventStore/Dapr component evidence |
| TestOrArtifact | RetentionPolicyTests; LegalHoldAggregateTests; LegalHoldScopeGuardAuthorizeEffectResultTests; HoldDeletionScopeGuardLinearizationTests; HoldCommittedExportArtifactPinIntegrationTests; HoldExpiryLinearizationFailureInjectionTests; HoldPrepareRecoveryDispositionTests; HoldReleasePhasePinnedRecoveryTests; LegalHoldScopeGuardReleaseRecoveryTests; GovernanceHumanActorBindingIntegrationTests; RetentionHoldWorkflowIntegrationTests; RetentionLegalHoldUiContractTests; retention/hold actor-binding and scope-guard receipt manifest |
| AdditionalV21Tests | PostStartHoldGuardFactAndExactLookupTests; PostStartHoldDispositionBoundaryTests; post-start hold before/at/after accepted-batch consume, containment issue/consume, purge, and completion; migration/restore preservation of post-start fact and applied decision version |
| V22DecisionScope | The runtime decision catalog includes StoryAuthorization 8.1 and every post-start deletion evaluation that this story can block; Open/missing/mismatched/hold-allowed evidence stays restrictive, and the story selects no Product outcome. |
| VerificationCommand | pwsh ./eng/verify-story-8.1.ps1 |
| NegativeEvidence | RetentionLegalHoldIsolationTests.CrossTenantInspectCreateReleaseAreDenied; GovernanceScopeMalformedUnknownCrossTenantAndUnverifiableMembershipTests; HumanExactConversationHoldCannotExecuteWhileDecisionOpenTests; ClassUtcRangeCannotExecuteWhileDecisionOpenTests; HoldCannotPrepareBeforeScopeGuardRegistrationTests; HoldRegistrationLostAckCannotDisappearFromBarrierTests; HoldRegistrationAndDestructionSealHaveExactlyOneWinnerTests; HoldAfterBarrierAuthorizationBeforeSealMakesStaleAuthorizationLoseTests; OpenArmedContentionAdvancesNeitherBranchTests; HoldCannotActivateWithUnpinnedCommittedArtifactTests; HoldIntentBeforeOrAtExpiryCannotLoseArtifactTests; FenceOrStoreControlOutageRetainsArtifactAndKeyTests; AutonomousTtlBackupAndRestoreCannotBypassFenceTests; OpenOrMismatchedHoldOverlapDecisionTests; HoldFailureCannotAuthorizeAbortOrUnpinTests; HoldCrashResumesOriginalIntentTests; HoldRecoveryCannotChooseOrSwitchDispositionTests; HoldGuardCannotReleaseBeforeEveryUnpinAndFenceReceiptTests; PendingLifecycleSuccessorCannotStrandRecordedHoldReleaseTests; HoldReleaseRecoveryCannotRetargetStoreOrDecisionVersionTests; TenantAdministratorCannotApproveHoldReleaseTests; MissingStaleNonHumanOrUnavailableHoldActorBindingTests; SameActorHoldReleaseApprovalTests; PlatformApproverDoesNotRequireInventedTenantPartyTests; RevokedGovernanceRoleTests; HoldExpiryReleaseRaceTests; DuplicateRetentionTimerTests |
| AdditionalV21NegativeEvidence | PostStartDestructionAlreadySealedCannotBeReadOnlyOrDisappearTests; OpenMissingMismatchedOrHoldAllowedPostStartOutcomeCannotGrantDestructionTests; PostStartHoldCannotBeBypassedByStaleContainmentOrCompletionAuthorizationTests |
| Result | Blocked — backlog; requires Stories 5.2, 5.8, 7.6 and Available host scope-guard, protection, secret, and Parties dependencies; human exact-Conversation, class/range, export-store, armed-deletion, and cancellation decisions apply only to their named branches |

### Story 8.2: Export Authorized Audit Content Securely

As an authorized governance operator,
I want a tenant-scoped encrypted and time-limited audit export,
So that approved evidence can be transferred without exposing secrets, unrelated tenant data, or an indefinitely usable artifact.

**Primary Demonstrable Outcome:** One authorized export request produces one encrypted manifested artifact with an enforced expiry and a tenant-scoped audit trail, while the service and UI never expose plaintext content or secret material.

**Dependencies:**

- **Prior stories:** 8.1 for governed protected scope and current legal-hold/retention status.
- **External:** `EXT-PROTECTION-1`, `EXT-SECRETS-1`, `EXT-EXPORT-STORE-1`, and `EXT-PARTIES-1` must be `Available` with exact targets and compatibility commands passing, and the store evidence must bind the approved `OD-EXPORT-LIFECYCLE-1` decision version, before protected-content selection, encryption-key resolution, second-party approval, or live export execution. `GovernanceScopeV1:ClassUtcRange` additionally requires approved `OD-GOVERNANCE-CLASS-RANGE-SCOPE-1`; human-origin `ExactConversation` additionally requires approved `OD-HUMAN-EXACT-CONVERSATION-SCOPE-1`; exact-interaction scope remains evaluable.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** an authorized export operator selects a canonical exact-interaction or human exact-Conversation scope, or requests the reserved class/range variant
**When** the request is accepted
**Then** EventStore records one deterministic export identity, canonical `GovernanceScopeV1` and predicate digest, freezes the authoritative interaction set, and acquires `ExportPreparing` on the tenant `ProtectionFence` with the exact approved lifecycle DecisionVersion, store target/contract version, frozen set, and partial-output inventory before any content read or artifact/key/index write. Unknown/malformed/cross-tenant membership rejects; human exact-Conversation rejects while its Product decision is Open; and `ClassUtcRange` rejects while its Product decision is Open
**And** the manifest names the canonical scope, immutable source revisions, item counts, hashes, encryption method reference, created time, exclusive expiry, and safe audit reference. Prepare-time lifecycle/store values phase-pin every byte, acknowledgement, commit, recovery lookup, and key delivery; a successor governs only a new prepare and switching requires cleanup-complete Abort plus a new preparation. The request never expands beyond its predicate or current authorization.

**Given** an export request is otherwise authorized
**When** FR-24 second-party governance is evaluated
**Then** the eligible second party records approval before any export content is read
**And** the Party-bearing Compliance Inspector requester resolves through `EXT-PARTIES-1`; a Party-bearing tenant approver uses the same seam, an `Administrator` alternative proves current tenant role plus durable actor/role evidence, and a `Platform` alternative proves current or historically recorded Hexalith.Tenants global-administrator authority without inventing a tenant Party. Separation compares stable `AuthenticatedHumanActorId`; missing, stale, ambiguous, overlapping, incompatible, non-human where a Party is required, unavailable, or same-actor evidence blocks before selection, while post-hoc approval is never accepted.

**Given** `EXT-SECRETS-1` is Available and key access succeeds through the platform host
**When** the export is materialized
**Then** `AuditExport(TenantId, ExportId)` encrypts the artifact under an export envelope key wrapped by the tenant KEK, appends `ManifestSealed` with manifest hash, signature reference, item hashes, stream/projection revision ranges, and key versions, and the `export` projection reaches the explicitly nonterminal `MaterializedAwaitingCommit` state only after artifact and manifest verification
**And** exact replay reuses the export identity rather than producing an untracked second artifact.

**Given** the immutable artifact, sealed manifest, and content-free interaction index have acknowledged the exact frozen inventory and no export key has been delivered
**When** export commit is decided
**Then** `GovernanceProtection:ExportCommitDecision` requires the lifecycle DecisionVersion, store target/contract version, frozen set, and inventory to equal `ExportPreparing`, appends `ExportCommitDecided`, and advances the committed-export index high-water in the same expected-revision `ProtectionFence` append, making that fence event the sole commit decision; any successor-version/target mismatch conflicts and never relabels prepared bytes
**And** `AuditExport` idempotently acknowledges the exact decision before state becomes `ExportCommitted` or key delivery is eligible; a crash before the fence decision retries after exact outcome lookup, a crash or lost acknowledgement after it completes only the secondary acknowledgement, and later deletion uses the fence high-water even while that acknowledgement is pending.

**Given** an authorized Compliance Inspector requests the sealed export key
**When** delivery is approved and unexpired
**Then** `ExportDownload:AuthorizeKeyDelivery` revalidates current Inspector authority and appends `ExportKeyDeliveryAuthorized` on `AuditExport` with deterministic `DeliveryId`, exact `ExportCommitAcknowledged` revision, requester `AuthenticatedHumanActorId`, purpose/audience, exclusive expiry, and prepare-pinned lifecycle/store/secrets contract versions before any custodian call
**And** only that fact permits `EXT-SECRETS-1` to deliver idempotently by `DeliveryId` directly to the authenticated principal and expose exact `Delivered(ObservedAt)`, `NotDelivered(Reason)`, or `Unknown/Unavailable` lookup; `RecordOrRecoverKeyDelivery` records only the safe result, never key material. Lost acknowledgement looks up the same id first; Delivered stays authoritative after later expiry/revocation, NotDelivered retries only while current actor authority and expiry still pass, expired/revoked records terminal non-delivery requiring a new audited export request, and unknown/unavailable remains pending without a second identity or inferred success
**And** no key or secret appears in an Agents response, manifest, projection, event, log, trace, metric, or browser surface.

**Given** the download authorization or artifact lifetime has expired
**When** any client requests the artifact
**Then** access fails with a safe typed outcome, no plaintext or renewed URL is inferred, and reauthorization requires a new audited export request
**And** incomplete, encryption-failed, manifest-mismatched, or partially published exports remain restrictive non-success states.

**Given** secret resolution is denied, stale, rotated incompatibly, or unavailable
**When** export processing reaches encryption
**Then** it fails closed before plaintext artifact publication, records only safe error classification and references, and can resume idempotently after an authorized recovery
**And** logs, traces, UI, API responses, events, and evidence contain no raw secret, prompt, context, proposal, Provider payload, or stack trace.

**Given** export preparation wrote no output or only part of its frozen artifact/manifest/index/key inventory
**When** abort or recovery runs
**Then** the original still-valid prepare intent at its exact revision and absence of cancellation/abort permit only `PrepareRecoveryDispositionDecided(Resume)`; an explicitly authorized cancellation or closed failure class mapped to Abort by the already-recorded recovery profile permits only `PrepareRecoveryDispositionDecided(Abort)`, which moves the fence to `ExportCleanupPending` until authenticated lookup proves each member never existed or durable physical-purge, index-removal, and irreversible key-destruction receipts prove every written and lifecycle-covered backup/restore copy is unreadable, with no key delivered before commit
**And** unknown/unavailable lookup cannot append `ExportAborted`, release the fence, or let overlapping deletion arm or complete; failure injection covers every write, acknowledgement, cleanup, lost-ack, and fence-release boundary.

**Given** an unauthorized Party or another tenant attempts to request, inspect, download, cancel, or replay an export
**When** any public or internal export path executes
**Then** current authorization denies before content selection, manifest disclosure, key resolution, or artifact access
**And** export existence, counts, hashes, scope, timing, and failure details remain hidden.

**Evidence Manifest:**

| Field | Story 8.2 evidence |
| --- | --- |
| Requirements | FR19-FR24, FR28; OQ-30; NFR1-NFR6, NFR11, NFR13; UX-DR1, UX-DR9-UX-DR18, UX-DR29-UX-DR33, UX-DR36-UX-DR41, UX-DR46, UX-DR50; AD-2, AD-8, AD-12, AD-13, AD-17, AD-20, AD-22, AD-23, AD-25, AD-26, AD-30; EXT-PROTECTION-1; EXT-SECRETS-1; EXT-EXPORT-STORE-1; EXT-PARTIES-1; OD-EXPORT-LIFECYCLE-1; conditional OD-GOVERNANCE-CLASS-RANGE-SCOPE-1 for class/range; conditional OD-HUMAN-EXACT-CONVERSATION-SCOPE-1 for human exact-Conversation |
| OwnedClauses | FR19.export-tenant-isolation; FR20.export-authorization-before-selection; FR23.export-public-contract; FR24.export-audit; FR24.export-prior-second-party-approval; FR28.audit-governance-active; PRD-OQ8.authorized-encrypted-time-limited-export; OQ30.no-post-hoc-export-approval; NFR2.no-unauthorized-audit-content; NFR6.secret-never-exposed; UX-DR31.ExportRequest-lock-scope; UX-DR40.ExportRequest-restrictive-viewport-block; UX-DR46.encrypted-time-limited-export; AD-2.AuditExport; AD-20.protected-content-and-secret-boundary; AD-22.manifest-sealing-and-durable-key-delivery; AD-23.export-projection; OperationGateMatrixV7.ExportDownload-authorize-effect-result-recovery |
| Dependencies | Story 8.1; EXT-PROTECTION-1, EXT-SECRETS-1, EXT-EXPORT-STORE-1, and EXT-PARTIES-1 Available with accepted exact targets and passing compatibility commands; approved OD-EXPORT-LIFECYCLE-1 DecisionVersion exactly matches the deployed store; OD-GOVERNANCE-CLASS-RANGE-SCOPE-1 only for class/range scope; OD-HUMAN-EXACT-CONVERSATION-SCOPE-1 only for human exact-Conversation scope |
| EvidenceLevel | Levels 2, 4, and 5: export logic, live secret/export components, and production-like encrypted-artifact proof |
| TestOrArtifact | AuditExportAggregateTests; GovernanceScopeV1CanonicalPredicateTests; ExportHumanActorBindingIntegrationTests; ExportPrepareRecoveryDispositionTests; ExportPrepareLifecycleVersionPinTests; EncryptedExportIntegrationTests; ExportProtectionFenceTests; ExportCommitDecisionAndAcknowledgementTests; ExportKeyDeliveryAuthorizeEffectResultRecoveryTests; ExportAbortCleanupFailureInjectionTests; ExportExpiryAndReplayTests; ExportUiContractTests; encrypted export manifest, actor-binding evidence, key-delivery outcome evidence, partial-output cleanup receipts, and no-leak scan |
| VerificationCommand | pwsh ./eng/verify-story-8.2.ps1 |
| NegativeEvidence | AuditExportIsolationTests.CrossTenantRequestInspectDownloadAndReplayAreDenied; GovernanceScopeMalformedUnknownCrossTenantAndUnverifiableMembershipTests; HumanExactConversationExportCannotExecuteWhileDecisionOpenTests; ClassUtcRangeCannotExecuteWhileDecisionOpenTests; MissingStaleNonHumanUnavailableOrSameActorExportApprovalTests; PlatformApproverDoesNotRequireInventedTenantPartyTests; RevokedExportRoleTests; OpenOrMismatchedExportLifecycleDecisionTests; LifecycleSuccessorCannotRelabelPreparedArtifactTests; LifecycleSwitchRequiresCleanupCompleteAbortAndNewPrepareTests; ExportRecoveryCannotChooseOrSwitchDispositionTests; ExportCannotReadBeforeFencePrepareTests; ExportCannotDeliverKeyBeforeFenceDecisionAndAuditExportAcknowledgementTests; ExportKeyDeliveryCrossTenantChangedIdentityExpiryRevocationAndLostAckTests; ExportKeyNeverTraversesAgentsTests; MissingAuditExportAcknowledgementCannotHideFenceCommittedCopyFromDeletionTests; ExportCommitLostAckAndConcurrentRevisionTests; PartialArtifactUnknownOrCleanupFailureRetainsFenceTests; SecretResolutionFailureTests; PlaintextArtifactPoisonScan; ManifestMismatchAndExpiredDownloadTests |
| Result | Blocked — backlog; EXT-PROTECTION-1, EXT-SECRETS-1, EXT-EXPORT-STORE-1, and EXT-PARTIES-1 are Uncommitted, OD-EXPORT-LIFECYCLE-1 is Open, Story 8.1 is required, and human exact-Conversation/class-range scopes retain only their named conditional blockers |

### Story 8.3: Delete Protected Content And Purge Projections

As an authorized governance operator,
I want approved protected content deleted through restrictive cryptographic erasure or redaction and named projection purges,
So that immutable history retains only a support-safe tombstone and no content-bearing copy survives the confirmed operation.

**Primary Demonstrable Outcome:** One authorized deletion reaches success only after protected EventStore payloads and every named content-bearing projection report restrictive completion, leaving a non-content tombstone and no recoverable sensitive payload.

**Dependencies:**

- **Prior stories:** 8.1 for retention eligibility, legal-hold enforcement, and protected scope.
- **External:** `EXT-HOST-1`, `EXT-PROTECTION-1`, `EXT-SECRETS-1`, `EXT-PARTIES-1`, and `EXT-CONV-AI-1` must be `Available` with exact targets and compatibility commands passing. Story authorization is blocked unconditionally while `OD-PRD-OQ31-INSTRUCTION-PROTECTION-1` is Open, as bound by PRD OQ-31. Both deletion origins require Story 6.1's current migration guard plus `EXT-HOST-1` ordinal admission fences, immutable owner cycles, one continuous predicate content guard with successor bindings, separate ledgers, and tenant scope-guard hold/seal serialization; `EXT-PROTECTION-1` must provide atomic single-use `DestroyDekManifest` consumption, ordered per-target receipts, exact lookup, and revocation. A deployment containing legacy plaintext is additionally blocked by its named decision. Human operator exact-Conversation, class/range, operator nonterminal convergence, and operator Abort remain blocked only by their named Open decisions; exact-interaction operator scope and PRD-fixed source deletion remain evaluable. Export lifecycle applies only to export-bearing sets. `OD-HOLD-DELETION-PRECEDENCE-1` applies conditionally both to a contender that registers after `DeletionArmed` but before `DestructionSealed` and to every post-start hold branch through completion; ordinary no-contender deletion remains evaluable, and this story chooses no outcome.
- **Forward dependencies:** None. A prior export request is not required, but every deletion must fence against and account for all overlapping committed, preparing, or cleanup-pending exports and every lifecycle-covered artifact copy.

**Acceptance Criteria:**

**Given** a hold, export, or deletion scope is submitted
**When** Story 8.3 validates and persists it
**Then** the scope is one versioned `GovernanceScopeV1` plus `ScopePredicateDigest = SHA-256(canonical length-prefixed scope bytes)`: either tenant-bound sorted-distinct exact interaction ids or one exact tenant Conversation. Membership is computed only from target stream identity, immutable first-event/directory-permit `SourceConversationId`, and permit/action/lease owner facts; caller metadata never classifies a resource, and missing, malformed, cross-tenant, or unverifiable proof fails closed
**And** `ClassUtcRange` is rejected before admission-fence, candidate, inventory, or effect while `OD-GOVERNANCE-CLASS-RANGE-SCOPE-1` is Open. This story chooses no content-class vocabulary, classification owner, clock field, range boundary, or public meaning.

**Given** the exact approved `OD-PRD-OQ31-INSTRUCTION-PROTECTION-1` version and a deletion request for sensitive Agent content
**When** current tenant authorization, deletion policy, retention eligibility, and legal-hold state are evaluated
**Then** `ProtectedDeletion(TenantId, DeletionId)` accepts a request only from a Platform Operator and becomes executable only after a distinct Compliance Inspector approval; the Platform requester proves current or historically recorded Hexalith.Tenants global-administrator authority without a tenant Party, the Party-bearing Inspector proves current/historical identity through `EXT-PARTIES-1`, and stable `AuthenticatedHumanActorId` separation fails closed on missing, stale, ambiguous, overlapping, incompatible, or same-actor evidence
**And** invalid or ambiguous scope, missing/stale authority, or an expected-revision mismatch rejects without state; otherwise the command stores one deterministic request identity, canonical predicate, and workflow-start revision. The target-limited `GovernanceProtection` Workflow has only the common admission/effect-cut grants and, after their global receipt, candidate/refreeze, acceptance/acknowledgement, content-fence, containment, preparation, and recovery grants bound to that request, requester, approval, tenant, predicate, and expected revisions. Refreeze may add newly discovered authoritative resources inside that predicate but can never expand it. `ProtectionFence:AcceptDeletionInventory` is the sole immutable-set decision for either origin and requires the global cut receipt; it compares the current hold/export/copy, migration, and namespace owners and records the accepted set/token/predicate/cut. A concurrent winner requires a superseding candidate under the same id and predicate; an export already `Preparing` or `CleanupPending` prevents acceptance/preparation until its branch closes.
**And** after first acceptance/acknowledgement but before preparation, either origin authorizes and installs one persistent content guard identified by `(DeletionRequestId, ScopePredicateDigest)` plus its first ordinal/global-cut/token binding. Conflict or migration invalidation installs nothing and permits only same-request/predicate supersession/refreeze; exact lookup resolves loss. If a later pre-seal admission recut produces a new accepted token, a separate successor-binding authorize/effect/result sequence proves the original fence was continuously installed, predecessor token invalidated, predicate unchanged, and current ordinal/zero proof exact, then appends the new binding without removal/reinstall. Prepare and barrier require that current binding; the guard rejects every later matching content write and survives migration repair.

**Given** that valid fence-owned accepted deletion set overlaps an active or preparing legal hold before `DeletionArmed`
**When** `GovernanceProtection:DeletionDeferByHold` wins its expected fence/owner revision
**Then** the gate-minimal deferral reads only recorded origin and fence owners and durably appends `DeletionDeferredByHold` with its origin/request revision, whole accepted set, exact hold ids/revisions, fence revision, and deterministic reconsideration identity; unrelated topology, secrets, audit, or lifecycle unavailability cannot hide that content-free fact, and it performs no preparation, erasure, purge, or key destruction
**And** release of the last overlapping hold triggers idempotent re-evaluation at a successor fence revision; a new hold or competing arm is serialized there, while a hold that first contends after `DeletionArmed` follows only the unresolved `OD-HOLD-DELETION-PRECEDENCE-1` branch and no story-local rule chooses its outcome.

**Given** `OD-PRD-OQ31-INSTRUCTION-PROTECTION-1` is open, missing, or mismatched
**When** Story 8.3 authorization or any deletion request/signal handling is attempted
**Then** it blocks before request acceptance, scope enumeration, or preparation, without classifying Agent Instructions/audit as either interaction-key protected or exempt configuration state
**And** no reduced interaction-only story completion, projection, protection adapter, or deletion worker chooses an Agent-level key placement; after approval, the exact decision version governs the complete deletion inventory even when one particular request contains no Agent Instructions.

**Given** the exact approved instruction-protection decision and an authenticated tenant-scoped `EXT-CONV-AI-1` Conversation-deletion signal with its source identity/revision
**When** the signal references a Conversation with derived Agent interactions
**Then** Conversations atomically owns a durable outbox/feed entry with the deletion approval, binds one immutable `ConversationDeletionSignalId` one-to-one to that logical decision and stable source revision, retains it through ordered reconnect/backfill and target rollover, and retries it until authenticated Agents acknowledgement; a delivery-attempt id may vary but never changes the logical signal or Agents deletion identity
**And** the closed signal envelope carries that id, `TenantId`, `ConversationId`, source stream/revision, approval reference, and source-contract version; platform composition derives a target-limited `ConversationDeletionPropagation` Workflow principal for `Intake` only, which verifies the committed source/target, recomputes `DeletionRequestId = H(conversation-deletion, TenantId, ConversationId, ConversationDeletionSignalId, SourceRevision)`, and records only `ConversationApprovedDeletion` plus its sole workflow-start outbox on the signal-derived `ProtectedDeletion` before returning `DeletionSignalAccepted` with the exact target revision
**And** exact retry/lost acknowledgement returns the same result, changed authenticated fields conflict and remain quarantined/unacknowledged without skipping the feed checkpoint, while unauthorized, cross-tenant, stale, forged, or unavailable evidence changes no state or disclosure and is retried by the durable source.

**Given** either the operator-approved or Conversation-approved deletion origin is durable with its canonical scope
**When** its origin-matching Workflow begins the common pre-candidate effect cut
**Then** it records `DeletionScopeAdmissionFenceAuthorized`; only that revision may call `EXT-HOST-1` to atomically install the canonical EventStore predicate at the current migration epoch and directory/effect-authorization high-waters. The guard rejects every later matching permit, protected User-action-intent, lease acquire/commit, `RateAdmissionAuthorized`, `OpenInteractionLeaseAuthorized`, `BudgetReservationAuthorized`, or `CapacityAdmissionAuthorized` append under old, bridged, current, restored, or successor authority, while exact lookup resolves a lost acknowledgement. A pre-fence winner precedes the authenticated checkpoint; a post-fence loser appends nothing
**And** from that receipt it freezes the finite authoritative `ConversationAgentState` cohort—one owner for source-approved exact Conversation, or exact interactions grouped by their stored source Conversation. Each owner appends a separately keyed `(DeletionRequestId, ScopePredicateDigest, AdmissionFenceOrdinal)` Closing cycle with the matching permit, creation/workflow-start/User-action-intent outbox, lease, rate/open/Budget/capacity phase authorization, decision, result, posting, and acknowledgement count/hash/high-water manifest. A successor ordinal carries every prior still-restrictive obligation plus each newly visible or violating winner into a new cycle; it never reopens, overwrites, or relabels an earlier Effective fact. Closing cancels and consumes reserved work, retains committed/authorized work as non-revocable, and permits only exact result/lookup/settlement/acknowledgement convergence. Queued creation may materialize its permitted owner; a cancelled start records `StartSuppressedByDeletion`; committed Provider/posting work reaches authenticated outcome. If capacity was queued/admitted before a Provider authorization branch, allocator lookup plus proof of no authorization/begin/active state records only `PreProviderCapacityDispositionDecided(CancelNoInvocation)`, releases the existing identity, and acknowledges it—never invocation or invented no-use
**And** an owner reaches `DeletionEffectCutEffective(AdmissionFenceOrdinal)` only after every carried-forward/current obligation and acknowledgement is exact. Operator nonterminal work stays Closing while its Product decision is Open. The append-only violation ledger is partitioned by request/ordinal: prior violations stay visible and only the current installed ordinal may attest `ZeroAcceptedViolationsSinceInstall`. Every same-ordinal owner receipt plus that proof is required for its global cut; candidate, acceptance, content binding, prepare, and barrier recheck it. Initial ordinal-one authorization explicitly requires no containment receipt or prior evidence. If an accepted violation appears before the first cut, containment explicitly proves no cut/candidate/token exists, records the violation, and assigns the next ordinal once; after a cut it invalidates only each exact artifact that exists. Concurrent detection/replay/lost acknowledgement returns the same receipt and ordinal. After the continuous content guard exists, every such receipt is an ordered invalidation-gap link; a binding authorization overtaken by a later violation terminates as `ObsoleteBindingAuthorization` without appending, and the next authorization starts at the latest installed binding plus the complete gap-free chain. No projection scan, request-lifetime “zero,” or absence proves completion.
**And** the EventStore guard assigns `AcceptedAtAdmissionFenceOrdinal` and `AcceptedAtGuardHighWater` at each matching accepted append's own linearization and atomically contaminates the partition current at that instant. Caller-presented stale ordinals are diagnostic only; successor installation serializes with assignment. Delayed detection uses that receipt, while missing/unverifiable acceptance-time evidence records `AdmissionIntegrityUnattributable`, invalidates current clean proof, and blocks deletion rather than guessing a partition.

**Given** an EventStore-accepted matching permit, intent, lease, or phase authorization is discovered after the admission-fence checkpoint
**When** `ContainDeletionScopeAdmissionFenceViolation` verifies its exact owner revision and canonical-scope membership
**Then** before `DestructionSealed` it uses the explicit no-artifact or exact-existing-artifact containment branch, preserves every fence/cycle, assigns one successor ordinal, and permits only that ordinal's admission-fence and separately keyed complete owner/global recut carrying prior obligations and the violator. If the content guard already exists, deletion cannot prepare until its continuous successor binding records the new token from the latest installed binding and complete gap-free invalidation chain. Another violation may extend the chain without waiting for an invalid token to bind; the overtaken authorization records terminal obsolete and exact lookup resolves it
**And** after sealing it records `DeletionIntegrityCompromised` and invokes `BlockDeletionBatchConsumption` for the deletion request/batch set to race `ReserveAndConsumeDeletionBatchEffect` at the protection owner. The exact blocked, reserved, revoked, or consumed result is mirrored; new batches, remaining destruction, purge, and completion block, and no possibly begun effect becomes content-only containment.

**Given** a source-approved deletion is preparing or recovering
**When** cancellation, failure, or an Abort disposition is proposed
**Then** no local operator, Inspector, workflow, timeout, or recovery profile can cancel or Abort it because seam 6 defines no authenticated source supersession; failures remain restrictive pending and permit only Resume/retry
**And** a future cancellation path requires a separately committed Conversations supersession signal and Product/Governance authority rather than borrowing the operator-origin branch; an active/preparing pre-arm hold instead records `DeletionDeferredByHold` with the complete set and deterministic reconsideration identity.

**Given** reversible operator-origin deletion preparation is interrupted before `DestructionStarted`
**When** recovery or cancellation is requested
**Then** while `OD-OPERATOR-DELETION-CANCELLATION-1` is Open, missing, mismatched, or forbids Abort, only the original exact prepare intent may Resume/retry; no timeout, dependency failure, operator, Inspector, Workflow, or recovery profile may infer Abort, unwind, remove an EventStore fence ordinal, release an owner/ordinal cycle, or publish cancellation
**And** only if an approved decision explicitly permits Abort may its named requester/approver evidence record that disposition. Cleanup of every reversible protection/export effect must complete first; then a `ProtectionFence` authorization that wins before any destruction-start authorization fixes the complete removal manifest. Its committed EventStore effect removes every admission-fence ordinal plus the continuous content guard and all ordinal/token bindings, exact lookup resolves the result, every owner/ordinal cycle records `ReleasedAfterApprovedAbort`, and only all those receipts permit final `DeletionAborted`. The guard rejects a destruction barrier after removal authorization; no step replays cancelled work or applies to Conversation-origin deletion.

**Given** an eligible accepted request and Available secret/erasure seam
**When** every reversible preparation acknowledges and deletion executes
**Then** before `DeletionPrepare`, the fence-owned accepted inventory either proves `NoLifecycleCoveredExportOrCopyInFrozenSet` without reading a lifecycle decision or binds the exact approved `OD-EXPORT-LIFECYCLE-1` version plus Available `EXT-EXPORT-STORE-1` target/contract; an export-bearing prepare phase-pins those values
**And** every authorized legal hold first registers its canonical scope on `GovernanceScopeGuard(TenantId)`; registration and deletion sealing serialize there. `AuthorizeDeletionDestructionStartBarrier` first observes and records the exact `GovernanceScopeGuard` revision/compare token; a contender registration, release, or other guard mutation after authorization makes its conditional seal append return authenticated stale. Only after that result is mirrored may a new authorization bind the returned current revision. While armed precedence is Open neither branch advances; only an exact effective deletion-allowed disposition may be consumed inside the seal commit, and a hold-allowed disposition remains restrictive. The accepted inventory assigns and persists one stable `DestructionSealId` from the accepted token, predicate digest, and accepted-manifest digest before the first barrier authorization; retries never derive it from a guard revision. The authorization binds that seal id, the accepted token, current ordinal/zero proof, continuous content binding/checkpoint, migration/fence receipts, sorted `(TenantId, AgentInteractionId, TargetProtectionKeyAlias)` manifest/digest, phase-pinned values, and any required disposition. `CommitDeletionDestructionStartBarrierEffect` conditionally appends at the observed guard revision and atomically rechecks those facts with admissions and hold registrations/releases, then commits `DestructionSealed`; that instant is `DestructionStarted`. It returns one deterministic non-expiring, revocable, single-use `DestroyDekManifest` capability for the exact accepted manifest, never a reusable barrier token. It has no `ExpiresAt` or renewal branch, its capability-key version remains verifiable for the full retention of the batch identity and terminal outcome, including exact retry/lookup after consumption or revocation, and outage/migration/restore reuse the same seal and batch ids. The authenticated successful guard result records its actual `CommittedDestructionSealGuardRevision` separately. `RecordDeletionDestructionStarted` mirrors it; `EXT-PROTECTION-1` atomically consumes the batch and destroys every target or none, returns the ordered per-target irreversible vector, and resolves loss by exact batch lookup. Changed manifests conflict and no second batch is minted. Each authoritative post-seal content violation is serialized on `ProtectionFence`, assigns one monotonic containment ordinal to its immutable singleton target manifest, then uses `AuthorizeDeletionContainmentBatch` → guard-revision-conditional `CommitDeletionContainmentBatchEffect` → `RecordDeletionContainmentBatchIssued` or `RecordDeletionContainmentBatchIssuanceStale` before protection consumption. Exact duplicates reuse the ordinal/result, distinct targets receive distinct ordinals, stale/lost issuance uses exact guard lookup, and no manifest is mutated or second batch minted. Unknown evidence starts nothing. EventStore replay produces typed `Erased` without rewriting history, and the support-safe tombstone records exact batch/vector evidence plus exactly one origin without Provider secrets, raw content, or invented human fields
**And** every accepted or containment batch carries a `DeletionBatchCapabilityV1` ES256 detached JWS over the closed RFC 8785 canonical issuer/audience/tenant/request/stable-destruction-seal-id/batch/manifest/guard/attestation/signing-attempt/intended-issue-revision/key-version fields; the actual successful issue revision is result evidence excluded from signed bytes and identity derivation. A target-limited authorize/sign/result step uses only the per-tenant `EXT-SECRETS-1` key family before guard issue; protection verifies the committed public anchor/audience and exact current guard-issued state, never a signature alone. A signed attempt whose issue loses terminalizes only after exact no-issue proof as `SignedAttestationObsoleteUnissued`; a successor retains seal id/batch/manifest/attestation ordinal, increments the signing attempt, and binds the new guard revision. Routine rotation retains old verifiers. Emergency compromise enters only through the non-public registrar and may replace only the attestation of the same actually issued protection-owner-blocked batch, with one active credential and no second batch.
**And** batch issue is followed by `AuthorizeDeletionBatchDispatch` → guard-revision-conditional `CommitDeletionBatchDispatchEffect` → issued-or-stale mirror before protection. The guard commit serializes with post-start hold and compromise facts; an earlier unresolved hold or intervening mutation produces stale and no dispatch. A winning `DeletionBatchDispatchAuthorized` is uninterruptible only against a later hold on that batch. Admission-integrity/key-compromise blocking and `ReserveAndConsumeDeletionBatchEffect` race the same `EXT-PROTECTION-1` state; `ConsumptionReserved` is their irreversible instant. Re-attestation requires a protection-owned capability-compromise block and stays blocked through guard replacement and successor dispatch. Exact protection-owner activation binds the replacement key, attestation, dispatch, expected batch revision, and tenant-key block-set revision and reopens only when that replacement key is unblocked in the same transaction; if its compromise exists or wins, activation returns a typed new-key-blocked outcome and leaves/places the batch in that exact block for another same-batch re-attestation.
**And** a post-seal violation is deduplicated by complete `(TenantId, AgentInteractionId, TargetProtectionKeyAlias)`. If the target already belongs to the accepted or a containment batch, the fence appends non-authorizing `ViolationCoveredByDeletionBatch` and links the resource to the original per-target outcome, including `AlreadyDestroyedByBatch` after consumption; it allocates no new ordinal/capability. Only an uncovered target gets a singleton containment ordinal. Same-target races converge, different uncovered targets remain distinct, and every new copy still requires purge evidence. Post-start hold facts and their exact applied disposition are checked at containment authorization, guard issue, protection consumption, purge, and completion.
**And** Provider secrets, raw payloads, Party PII, and deleted content never appear in the tombstone, status, audit, log, trace, or evidence.

**Given** projection purge begins
**When** each affected contract reports its restrictive result
**Then** completion explicitly accounts for every content-bearing member of the authoritative current inventory, including `agent-interaction-status`, `proposal-detail`, `proposal-version-history`, `pending-proposal-queue`, `pending-proposal-count`, `audit-evidence`, `retention`, `legal-hold`, `export`, `export-artifact-index`, every matching `export-artifact-store` and lifecycle-covered backup/restore copy, `deletion`, content-bearing metrics, and `workflow-execution-state`, with physical purge/destruction receipts
**And** `agent-setup`, `provider-catalog`, the content-free `conversation-interaction-directory` permit/tombstone/barrier evidence, `tenant-provider-enablement`, `rate-limit-usage`, `open-interaction-leases`, `budget-reservation-usage`, `safety-verdict-status`, content-free `safety-verdict-directory`, `launch-readiness`, and non-content metric records retain only policy-required support-safe references; deletion cannot complete while any matching committed, preparing, or cleanup-pending export is unaccounted for
**And** an exact export-free inventory neither reads nor fabricates lifecycle authority; an export-bearing set waits for its exact version/target/receipts. A content write after guard installation forces refreeze before sealing; after sealing an authoritative same-predicate resource is either linked through existing exact target coverage or receives its one new guard-issued containment batch. Unknown or outside-scope proof blocks without widening scope. Admission violations follow only ordinal recut/integrity-compromise recovery. Final success requires `AuthorizeDeletionCompletionBarrier` to freeze every admission acceptance/high-water, immutable owner cycle, continuous content binding/checkpoint, post-start hold disposition, target-coverage link, capability attestation/status, batch vector, and copy receipt plus the observed guard revision. `CommitDeletionCompletionBarrierEffect` conditionally appends at that revision and atomically rechecks the guard; any late admission/content/hold/release/coverage/containment/capability mutation returns authenticated stale and must be mirrored before reauthorization. Only `DeletionCompletionSealed` exact lookup may be mirrored as completed, and it rejects all later matching writes. No direct clean-read completion is legal.

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
| Requirements | FR18-FR24, FR28, FR30; OQ-31; A-16; NFR1-NFR6, NFR11, NFR13; UX-DR1, UX-DR9-UX-DR18, UX-DR29-UX-DR33, UX-DR36-UX-DR41, UX-DR46, UX-DR50; AD-2-AD-4, AD-6, AD-8, AD-12, AD-13, AD-17, AD-20, AD-22, AD-23, AD-25, AD-26, AD-29, AD-30; EXT-HOST-1; EXT-PROTECTION-1; EXT-SECRETS-1; EXT-PARTIES-1; EXT-CONV-AI-1; unconditional StoryAuthorization:8.3 OD-PRD-OQ31-INSTRUCTION-PROTECTION-1; conditional OD-GOVERNANCE-CLASS-RANGE-SCOPE-1 for class/range scope; conditional OD-HUMAN-EXACT-CONVERSATION-SCOPE-1 for human operator exact-Conversation scope; conditional OD-OPERATOR-DELETION-NONTERMINAL-DISPOSITION-1 for operator-origin nonterminal convergence; conditional OD-OPERATOR-DELETION-CANCELLATION-1 only for operator Abort/removal; conditional OD-LEGACY-PLAINTEXT-DISPOSITION-1 for nonempty legacy plaintext; conditional EXT-EXPORT-STORE-1 and OD-EXPORT-LIFECYCLE-1 for export-bearing accepted sets; conditional OD-HOLD-DELETION-PRECEDENCE-1 for post-arm pre-seal contention and every post-start hold branch through completion |
| OwnedClauses | FR18.terminal-history-preserved-after-protection; FR19.deletion-tenant-isolation; FR20.deletion-authorization-before-effect; FR23.deletion-status-contract; FR24.safe-deletion-audit; FR28.audit-governance-active; FR30.Conversation-deletion-derived-content-propagation; A16.Conversations-deletion-signal; PRD-OQ8.cryptographic-erasure-or-redaction; PRD-OQ8.named-projection-purge; PRD-OQ8.safe-tombstone; OQ31.complete-story-authorization-blocker; NFR11.restart-no-duplicate-erasure; UX-DR31.DeletionRequest-lock-scope; UX-DR40.DeletionRequest-restrictive-viewport-block; UX-DR46.restrictive-partial-failure; AD-2.canonical-scope-common-both-origin-admission-and-effect-cut; AD-6.durable-Conversation-deletion-delivery; AD-13.pre-Provider-capacity-disposition; AD-22.fence-owned-deletion-inventory-and-phase-pinned-destruction; AD-23.explicit-deletion-inventory; AD-29.signal-derived-deletion-id; AD-30.manifest-bound-both-origin-settlement-authority; OperationGateMatrixV7.acceptance-attribution-post-start-hold-target-coverage-capability-trust-owner-linearization-and-completion-barrier |
| Dependencies | Story 8.1 and Story 6.1 directory/migration/repair-fence contract for both deletion origins; EXT-HOST-1, EXT-PROTECTION-1, EXT-SECRETS-1, EXT-PARTIES-1, and EXT-CONV-AI-1 Available with passing ordinal admission, explicit pre/post-cut recovery, immutable owner cycles, repeated-recut gap-chain continuous content binding, revision-bound tenant scope-guard hold/seal serialization with stale recovery, separate ledgers, non-expiring accepted capability recovery, guard-owned target-deduplicated containment-batch issuance, stable destruction-seal/batch identity across signed-but-unissued retries, non-public compromise registration, and protection-owner block/reserve/re-attestation activation with replacement-key block arbitration; approved OD-PRD-OQ31-INSTRUCTION-PROTECTION-1 unconditionally; OD-GOVERNANCE-CLASS-RANGE-SCOPE-1 only for class/range variants; OD-HUMAN-EXACT-CONVERSATION-SCOPE-1 only for human operator exact-Conversation; OD-OPERATOR-DELETION-NONTERMINAL-DISPOSITION-1 only when operator-origin manifested work lacks an ordinary terminal transition; OD-OPERATOR-DELETION-CANCELLATION-1 only for operator Abort/removal; OD-LEGACY-PLAINTEXT-DISPOSITION-1 only for nonempty legacy plaintext; EXT-EXPORT-STORE-1/OD-EXPORT-LIFECYCLE-1 only at initial prepare for export-bearing accepted sets; OD-HOLD-DELETION-PRECEDENCE-1 for post-arm pre-seal contention and every post-start hold branch through completion |
| EvidenceLevel | Levels 2, 4, and 5: deletion state logic, live payload/projection components, and production-like erasure/purge proof |
| TestOrArtifact | ProtectedDeletionAggregateTests; GovernanceScopeV1CanonicalPredicateTests; ConversationInteractionDirectoryAggregateTests; DeletionHumanActorBindingIntegrationTests; OperatorDeletionWorkflowCandidateAuthorityTests; ConversationDeletionDurableSourceFeedIntegrationTests; ConversationDeletionSignalIntakeMatrixTests; BothOriginDeletionAdmissionFenceRaceTests; AdmissionFencePhaseAuthorizationRaceTests; AdmissionFenceAcceptedViolationRecutTests; AdmissionFencePreGlobalCutSuccessorTests; AdmissionFenceOrdinalPartitionAndCarryForwardTests; AdmissionFencePostDestructionIntegrityCompromisedTests; BothOriginDeletionOwnerCutFixedPointTests; ImmutableOwnerOrdinalCutCycleTests; ConversationDeletionDerivedInteractionConvergenceTests; OperatorNonterminalDeletionDispositionDecisionTests; DeletionManifestBoundRateOpenBudgetCapacitySettlementTests; PreProviderCapacityDeletionDispositionTests; ConversationDeletionPostingPendingLookupTests; DeletionCandidateRefreezeAndFenceAcceptanceTests; DeletionScopeWriteFenceAuthorizeEffectResultTests; ContinuousContentFenceSuccessorBindingTests; RepeatedAdmissionRecutGapChainAndObsoleteBindingTests; HoldDeletionScopeGuardLinearizationTests; DeletionDestructionStartBarrierAuthorizeEffectResultTests; DeletionDestructionStartBarrierAdmissionAndHoldRaceTests; GovernanceScopeGuardBarrierCompareTokenStaleReauthorizationTests; MultiInteractionDestroyDekManifestTests; NonExpiringDeletionBatchRestoreTests; GuardOwnedContainmentBatchIssuanceRecoveryTests; DeletionBatchRevocationRaceTests; DeletionScopeFenceMigrationRepairPreservationTests; DeletionScopeFenceViolationContainmentTests; OperatorDeletionCancellationDecisionAndFenceRemovalTests; ConversationDeletionSignalRecoveryTests; DeletionOriginTombstoneContractTests; DeletionPrepareRecoveryDispositionTests; DeletionDeferredByHoldTests; HoldReleaseDeletionReevaluationTests; ExportFreeDeletionCompletionTests; ExportBearingDeletionLifecyclePinTests; InstructionProtectionDecisionTests; CryptographicErasureIntegrationTests; ProtectionFenceExportDeletionRaceTests; NamedProjectionAndAllCopiesPurgeTests; DeletionRecoveryAndUiTests; deletion completion manifest, every fence-ordinal and continuous-binding receipt, owner/ordinal/global cut manifests, scope-guard contender/compare-authorization/stale/seal receipts, every accepted/containment capability-state and batch vector, physical purge/backup/restore receipts, immutable separate ledger checkpoints, and forensic no-content scan |
| AdditionalV21Tests | AdmissionAcceptanceTimeOrdinalAttributionRaceTests; AdmissionIntegrityUnattributableBlocksDeletionTests; PostStartHoldEveryDestructiveBoundaryTests; DeletionBatchDispatchGuardLinearizationTests; SameProtectionAliasViolationCoverageTests; DeletionBatchCapabilityCanonicalJwsTrustTests; DeletionBatchCapabilityRotationCompromiseReattestationTests; DeletionCompletionBarrierRaceAndRecoveryTests; migration/restore preservation of acceptance receipts, post-start holds/dispositions, target coverage, attestations/revocations/dispatch receipts, stale completion attempts, and completion seal |
| V21DecisionAndDependencyScope | `OD-HOLD-DELETION-PRECEDENCE-1` applies both to a contender registered after `DeletionArmed` but before seal and to a hold observed after seal but before completion; the story binds an approved result but selects none. `EXT-HOST-1`, `EXT-SECRETS-1`, and `EXT-PROTECTION-1` must satisfy their v21 admission-attribution, post-start-hold, target-coverage, signing/attestation, online validation, and completion-barrier extensions. |
| AdditionalV22Tests | ProtectionOwnerAdmissionCompromiseBlockVsReserveConsumeTests; ProtectionOwnerCapabilityKeyBlockVsReserveConsumeTests; CapabilityCompromiseRegistrarAuthorityReplayTenantAndLostAckTests; ReattestationRequiresProtectionOwnedBlockTests; ReattestationGuardReplacementSuccessorDispatchAndProtectionActivationTests; SignedAttestationObsoleteUnissuedAcceptedAndContainmentTests; SigningAttemptRevisionRotationCompromiseAndRestoreTests; migration/restore preservation of signing attempts, obsolete-unissued receipts, registrar deliveries, block-pending facts, and every protection-owner blocked/reserved/re-attested/terminal state |
| V22DecisionAndDependencyScope | `OD-HOLD-DELETION-PRECEDENCE-1` is present in the runtime catalog affected set for both pre-seal contention and every post-start containment, dispatch, protection-reservation, purge, completion, and StoryAuthorization 8.1/8.3 branch that consumes it; the story selects no outcome. `EXT-HOST-1`, `EXT-SECRETS-1`, and `EXT-PROTECTION-1` must satisfy their v22 signed-unissued, registrar, and protection-owner state-machine extensions. |
| AdditionalV23Tests | StableDestructionSealIdentityAcrossMultipleSignedObsoleteAttemptsTests; ActualCommittedSealAndContainmentIssueRevisionExcludedFromCapabilityIdentityTests; ReplacementKeySecondCompromiseBeforeReattestDispatchActivationAndReserveTests; ActivationBlockedByReplacementKeyCompromiseLostAckMigrationRestoreTests; activation identity and restore manifests preserve the replacement key, attestation, successor dispatch, expected batch revision, expected tenant-key block-set revision, and exact activated-or-new-key-blocked result |
| V23DecisionAndDependencyScope | Matrix v7 changes only technical identity and protection-owner ordering: one pre-seal `DestructionSealId` survives every stale signing retry, actual issue revisions are result evidence, and activation cannot clear a replacement-key block. `OD-HOLD-DELETION-PRECEDENCE-1` remains Open for both post-arm/pre-seal contention and every post-start branch; ordinary no-contender deletion remains evaluable and this story selects no Product outcome. |
| VerificationCommand | pwsh ./eng/verify-story-8.3.ps1 |
| NegativeEvidence | ProtectedDeletionIsolationTests.CrossTenantRequestConfirmInspectAndReplayAreDenied; GovernanceScopeMalformedUnknownCrossTenantAndUnverifiableMembershipTests; HumanExactConversationOperatorDeletionCannotExecuteWhileDecisionOpenTests; ClassUtcRangeCannotExecuteWhileDecisionOpenTests; OperatorDeletionWorkflowWrongRequestActorScopeCandidateOrdinalAndRevisionTests; OperatorDeletionRefreezeCannotExpandApprovedScopeTests; OperatorNonterminalOwnerCannotReachEffectiveWhileDispositionOpenTests; ConversationDeletionSourceOutageBackfillPoisonAndAckLossTests; ConversationDeletionSignalCrossTenantStaleChangedReplayAndLostAckTests; ConversationDeletionSignalCannotMintSecondLogicalIdTests; BothOriginPermitIntentAcquireCommitAndRateOpenBudgetCapacityAuthorizationImmediatelyBeforeAndAfterAdmissionFenceTests; PreGlobalCutViolationCannotWedgeOrInventArtifactInvalidationTests; ConcurrentViolationDetectionCannotAssignTwoSuccessorOrdinalsTests; MultipleViolationsBeforeSuccessorBindingCannotWedgeOrSkipGapTests; AcceptedAdmissionViolationRequiresHigherOrdinalFullOwnerRecutTests; SuccessorAdmissionOrdinalCannotHidePriorViolationOrReopenEffectiveTests; ExistingContentFenceCannotBeSilentlyReusedReinstalledOrLeftOnOldTokenTests; PostDestructionAdmissionViolationCannotEnterContentContainmentOrCompleteTests; CandidateCannotBuildBeforeEverySameOrdinalOwnerCutAndZeroSinceInstallProofTests; ApproverResolutionAndUserActionIntentImmediatelyBeforeAndAfterClosingTests; StaleCancelledWorkerCannotStartEffectTests; PreProviderCapacityCannotRemainManifestedAfterEffectiveTests; MembershipAndGeneratedOutputImmediatelyBeforeAndAfterEffectiveTests; CreationOutboxAndInteractionAppendLostAckTests; CommittedEffectLeaseAckLossAndUnknownOutcomeTests; ProjectionLagCannotOmitInteractionTests; DeletionWorkflowCannotReserveAcquireInvokeOrInventSettlementTests; CandidateCannotPrepareBeforeCurrentContentBindingTests; HoldAfterBarrierAuthorizationBeforeSealMakesAuthorizationLoseTests; BarrierAuthorizationWithoutExactGuardCompareTokenRejectedTests; BarrierReauthorizationBeforeStaleResultRecordedRejectedTests; OpenArmedHoldContenderAdvancesNeitherBranchTests; HoldRegistrationLostAckCannotDisappearTests; DirectLedgerReadThenProtectionFenceAppendCannotAuthorizeDestructionTests; AdmissionAppendRacingBarrierCommitHasExactlyOneWinnerTests; MultiKeyDeletionCannotReuseBarrierReceiptPerKeyTests; DestroyDekManifestCannotPartiallySucceedOrMintSecondBatchTests; LostManifestBatchOutcomeUsesExactLookupTests; SealedBatchCannotExpireOrRenewIntoSecondAuthorityTests; ContainmentProtectionEffectWithoutGuardIssuedCapabilityRejectedTests; ContainmentDuplicateConcurrentAndLostIssuanceTests; CompromiseRevocationAndBatchConsumptionHaveExactlyOneWinnerTests; FenceRemovalAuthorizationAndDestructionBarrierCannotBothWinTests; MigrationSuccessorCannotDropAdmissionOrdinalGapChainOwnerCycleContentBindingHoldContenderCompareAuthorizationStaleResultSealBatchGuardIssuedStateOrProtectionOwnerOutcomeTests; PostStartInScopeContentViolationRequiresDistinctContainmentBatchTests; OutsideScopeOrUnresolvableViolationBlocksCompletionTests; ConversationDeletionCannotUseLocalAbortOrHumanOriginFieldsTests; OperatorAbortFenceRemovalAndCutReleaseBlockedWhileDecisionOpenTests; MissingStaleOrSameActorDeletionApprovalTests; ActiveOrPreparingHoldCannotBeRejectedOrBypassedTests; DeferredDeletionCannotPrepareBeforeLastHoldGuardReleaseTests; OpenOrMismatchedInstructionProtectionDecisionBlocksAllStoryAuthorizationTests; LegacyPlaintextCannotEnterDeletionInventoryTests; ReducedInteractionOnlyDeletionCannotBypassOq31Tests; ExportFreeDeletionCannotReadOrFabricateLifecycleDecisionTests; ExportBearingDeletionCannotPrepareOrDestroyWithoutPhasePinnedLifecycleStoreTests; PreparingOrCleanupPendingExportBlocksDeletionTests; PartialProjectionFailureTests; DuplicateDeletionDeliveryTests; DeletedContentForensicScan |
| AdditionalV21NegativeEvidence | CallerPresentedStaleOrdinalCannotChooseViolationPartitionTests; MissingAcceptanceReceiptCannotLeaveCurrentZeroCleanTests; PostStartHoldOpenOrMismatchedCannotDispatchAcceptedBatchTests; PostStartHoldCannotIssueOrDispatchContainmentBatchOrCompleteTests; StaleOrBatchIssueOnlyCannotReachProtectionTests; SameAliasDistinctResourceCannotMintSecondCapabilityTests; WrongTenantAliasCannotUseAlreadyDestroyedReceiptTests; SignatureWithoutExactGuardIssuedStateRejectedTests; WrongAudienceOrRevokedCapabilityKeyRejectedTests; CompromisedKeyCannotRaceReattestationIntoTwoActiveCredentialsTests; DirectCompletionAppendRejectedTests; LateAdmissionContentHoldCoverageDispatchOrCapabilityMutationMakesCompletionAuthorizationStaleTests |
| Result | Blocked — backlog; common dependencies remain Uncommitted and OD-PRD-OQ31-INSTRUCTION-PROTECTION-1 blocks all Story 8.3 authorization. Class/range scope, human operator exact-Conversation scope, operator nonterminal convergence, and operator cancellation remain safely blocked only at their named Open decisions; exact-interaction and source-approved exact-Conversation eligible Resume/retry paths stay evaluable. Nonempty legacy plaintext and optional export/armed-contention branches retain their conditional blockers. Stories 8.1 and 6.1 plus the common admission/effect-cut/content-fence implementation are required |

### Story 8.4: Operate Safety Cost And Governance Policies

As an authorized Agent governance administrator,
I want to publish and inspect versioned safety, cost, and governance policy changes through one consistent high-impact command model,
So that future Agent Calls use explicit current controls and no UI or API path can weaken active protections.

**Primary Demonstrable Outcome:** One authorized policy publication becomes an immutable future-only version through EventStore and is rendered authoritatively across API/UI, while weaker, ambiguous, stale, unauthorized, or unsafe publication attempts have no runtime effect.

**Dependencies:**

- **Prior stories:** 5.3 and 5.5 for Provider/pricing/readiness contracts, 6.3 for enforced live safety, and 6.4 for hard budget reservation behavior.
- **External:** `EXT-SECRETS-1` must be `Available` for digest-key rotation and the policy/key-version safety activation barrier. `EXT-PARTIES-1` must be Available for the Party-bearing Release Operator review/pull branch. Other live adapter behavior remains proven by the dependencies declared in Stories 6.3 and 6.4.
- **Forward dependencies:** None.

**Acceptance Criteria:**

**Given** an authorized policy administrator authors a Content Safety Policy
**When** validation and publication succeed
**Then** platform-scoped `ContentSafetyPolicy(system)` appends a new immutable policy version defining prompt/context and output gates, fixed always-blocked categories, explicitly permitted restricted handling, failure/audit treatment, no Approver override, future-only effect, and no-weaker retry
**And** safety configuration is removed from `Agent`; each interaction snapshots the platform/tenant policy version pair and retains a safety high-water mark at least as restrictive as its initial pair.

**Given** a Content Safety Policy publication or per-tenant `EXT-SECRETS-1` digest-key rotation is durably accepted
**When** the new safety version is prepared for runtime use
**Then** publication/rotation starts the Story 6.3 `SafetyVerdictEpoch(PendingActivation)` protocol, binds the exact policy/key versions and rescan profile, and does not expose the version as Active until the finite active-tenant manifest and each tenant's finite indexed-Conversation manifest are complete
**And** post-checkpoint tenants and Conversations use local version-pinned initialization before their own callability and never extend that barrier; API/UI show authoritative pending with content-free epoch/checkpoint progress, while a crash, unavailable key/safety seam, lost acknowledgement, invalid bound, or incomplete manifest keeps the old version from authorizing affected calls and causes them to wait then fail `ContextReadUnavailable(RescanPending)` under the recorded bound.

**Given** an authorized tenant governance administrator publishes tenant restrictions
**When** validation succeeds
**Then** `TenantGovernancePolicy(TenantId)` owns only stricter safety restrictions, monthly/per-call caps, rate limits, calling restrictions, concurrency limit, durable SM-4 confirmations, trigger-review decisions, and tenant kill-switch state on one serialized governance revision
**And** `Agent` retains only proposal expiry, regeneration ceiling, context policy, response mode, and approver configuration.

**Given** a content-free `SecurityEventLog` observation classifies a target-tenant event as `CrossTenant` or `UnauthorizedAction`
**When** the Platform Operator confirms the event and immediately pulls containment
**Then** `TenantGovernancePolicy(TenantId)` first appends `Sm4IncidentConfirmed` with AD-29 `Sm4IncidentId`, exact `SecurityObservationId`/source revision, classification, confirming actor, and evidence digest, and a subsequent expected-revision command appends `TenantKillSwitchPulled` binding that confirmation
**And** both containment commands use their direct fail-closed EventStore paths without readiness dependencies, exact replay is idempotent, conflicting or missing evidence changes no state, and failure before or after either append can be retried without losing the confirmed fact or creating an ungrounded pull.

**Given** a launch-health trigger review reaches a Release Operator decision
**When** the reviewed branch pulls or a later authorized operator releases the switch
**Then** the Party-bearing Release Operator records `TriggerReviewDecisionRecorded` with the exact metric contract/window/cohort and may append `TenantKillSwitchPulled` only from that current decision; release appends `TenantKillSwitchReleased` only from the recorded containment/review basis and audited justification
**And** `EXT-PARTIES-1` binds the Release Operator Party to the same stable human actor, while a Platform principal uses Hexalith.Tenants global-authority evidence without an invented tenant Party; stale, cross-tenant, same-revision-conflicting, unavailable, or wrong-branch evidence is denied.

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
| Requirements | FR4-FR7, FR19-FR28, FR33; NFR1-NFR7, NFR10, NFR11, NFR13; UX-DR1-UX-DR5, UX-DR9-UX-DR18, UX-DR23, UX-DR26, UX-DR29-UX-DR33, UX-DR36-UX-DR46, UX-DR50; AD-2-AD-5, AD-8-AD-10, AD-12, AD-13, AD-17, AD-20-AD-23, AD-25, AD-26, AD-29, AD-30; EXT-SECRETS-1; EXT-PARTIES-1 |
| OwnedClauses | FR26.versioned-policy-publication; FR26.always-blocked-and-restricted-rules; FR26.future-only-and-no-weaker-retry; FR27.no-override-policy-use; FR27.policy-and-digest-key-rotation-activation-barrier; FR20.policy-admin-authorization; FR22.policy-authoring-parity; FR24.policy-change-audit; FR28.audit-governance-active; NFR7.active-safety-governance; NFR10.numeric-cap-policy; NFR11.safety-rescan-recovery; UX-DR31.PolicyPublication-and-TenantBudgetUpdate-lock-scope; UX-DR40.policy-and-budget-restrictive-viewport-block; UX-DR43.safety-authoring; UX-DR44.cost-authoring; AD-2.ContentSafetyPolicy-TenantGovernancePolicy-and-SafetyVerdictEpoch; AD-12.advisory-command-scope; AD-13.current-gates-may-tighten; AD-20.activation-barrier |
| Dependencies | Stories 5.3, 5.5, 6.3, and 6.4; EXT-SECRETS-1 Available; EXT-PARTIES-1 Available for the Release-Operator review/pull branch |
| EvidenceLevel | Levels 2 and 4: policy/concurrency logic and live EventStore/API/UI component evidence over already-proven runtime seams |
| TestOrArtifact | ContentSafetyPolicyAggregateTests; TenantGovernancePolicyAggregateTests; AgentSafetyConfigurationRemovalTests; GovernancePolicyApiUiParityTests; PolicyActivationAndSafetyEpochTests; SafetyVerdictRescanRecoveryLiveTests; DigestKeyRotationRescanTests; PolicyHighWaterMarkTests; Sm4IncidentConfirmationAndContainmentTests; TriggerReviewDecisionAndPullTests; KillSwitchRecoveryTests; governance-policy activation evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-8.4.ps1 |
| NegativeEvidence | GovernancePolicyIsolationTests.CrossTenantCreatePublishInspectAndReplayAreDenied; RevokedPolicyAdminTests; PolicyCannotActivateWithMissingFiniteManifestAckOrInvalidRescanProfileTests; PostCheckpointTenantOrConversationCannotExtendActivationBarrierTests; RotatedKeyCannotReuseOldVerdictTests; WeakerRetryAndApproverOverrideTests; InvalidBudgetPolicyTests; ConflictingPendingCommandTests; Sm4PullWithoutExactConfirmationIsDeniedTests; ReviewPullWithoutCurrentDecisionIsDeniedTests; PlatformAuthorityDoesNotInventTenantPartyTests; KillSwitchLostAckAndWrongRevisionTests |
| Result | Blocked — backlog; requires Stories 5.3, 5.5, 6.3, and 6.4, EXT-SECRETS-1 is currently Uncommitted, and the Release-Operator branch additionally needs EXT-PARTIES-1 |

### Story 8.5: Calculate Runtime And Product Metrics Deterministically

As a release operator,
I want versioned deterministic runtime and product metric calculations,
So that evidence is classified consistently without treating formula fixtures as proof of live attainment.

**Primary Demonstrable Outcome:** Approved fixtures deterministically calculate every NFR-9 and SM-1–SM-7 result, including insufficiency, pre-enablement versus launch-health classification, window, cohort, percentile, and late-data behavior, while explicitly producing no live READY claim.

**Dependencies:**

- **Prior stories:** 5.5 for readiness-record contracts and the authoritative runtime/product source events emitted by Stories 6.1–7.6.
- **External:** `EXT-TOPOLOGY-1` must be `Available` for live production-like source observations and qualification-session evidence, and `EXT-CONV-AI-1` must be `Available` for the SM-2 active-Conversation count or equivalent Conversations-side event feed; both are currently `Uncommitted`. Pure calculator fixtures do not bypass these live-evidence dependencies for story completion.
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
| Requirements | FR8-FR25, FR28, FR30, FR33; NFR1, NFR2, NFR4, NFR5, NFR9, NFR11; SM-1-SM-7, SM-C1-SM-C5; UX-DR9-UX-DR12, UX-DR25-UX-DR30, UX-DR45; AD-3, AD-4, AD-8, AD-17, AD-22-AD-26; EXT-CONV-AI-1, EXT-TOPOLOGY-1 |
| OwnedClauses | FR25.runtime-and-product-metric-status; FR28.fixed-metric-and-latency-controls; PRD-section11.versioned-measurement-contract; PRD-section11.deterministic-fixture-not-live-attainment; NFR9.all-four-threshold-families-and-30-sample-minimum; SM1.active-tenant-adoption-pre-enable; SM2.20-percent-50-conversation-30-day-launch-health; SM3.80-percent-human-decision-within-expiry-or-24h-launch-health; SM4.zero-successful-unauthorized-actions-pre-enable; SM5.complete-post-audit-links-pre-enable; SM6.admin-api-parity-pre-enable; SM7.substantive-review-band-launch-health; AD-24.metric-source-and-insufficiency-rules |
| Dependencies | Story 5.5 and source events from Stories 6.1-7.7; EXT-TOPOLOGY-1 and EXT-CONV-AI-1 Available for live observations/SM-2 denominator |
| EvidenceLevel | Level 2 proves calculators; Levels 4 and 5 are required separately for live production-like source observations and cannot be inferred from fixtures |
| TestOrArtifact | RuntimeMetricCalculatorTests; ProductMetricCalculatorTests; WindowCohortLateDataTests; MetricInsufficiencyTests; versioned measurement-contract and fixture manifests |
| VerificationCommand | pwsh ./eng/verify-story-8.5.ps1 |
| NegativeEvidence | MetricIsolationTests.CrossTenantSourceCohortAndDetailAccessAreDenied; FixtureCannotProduceLivePassTests; InsufficientSampleAndMissingTimestampTests; ConflictingDuplicateObservationTests; CounterMetricGuardTests |
| Result | Blocked — backlog; EXT-TOPOLOGY-1 and EXT-CONV-AI-1 are Uncommitted and prior runtime/product source events are required |

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

**Given** the OperationGateMatrixVersion 7 operation-family inventory
**When** route/state and parity coverage is enumerated
**Then** its public/UI-bearing family inventory covers `ProviderCatalogMutation`, `AgentSetupMutation`, `AgentActivation`, `AgentCallAcceptance`, `ConversationPosting`, `ProposalResolution`, `PolicyPublication`, `TenantBudgetUpdate`, `LegalHold`, `ExportRequest`, `DeletionRequest`, `ReadinessInspection`, `ProposalEdit`, `ProposalRegeneration`, `LegalHoldRelease`, `ExportDownload`, `AuditInspection`, `TenantKillSwitch`, `TenantProviderEnablement`, and `DataHandlingAcceptance`
**And** no public/UI-bearing v4 family/variant, exact `ScopeKind`, producer rule, route, or high-impact state can be omitted or covered only by an older matrix fixture; internal-only `ProviderInvocation`, `SystemTimer`, `ReadinessObservation`, replay/security recording, decision publication, and governance recovery variants remain exhaustively owned and tested by Stories 5.4, 5.5, 6.4, and 8.1-8.3 and are not invented as UI routes here.

**Given** English and French resources and supported desktop, tablet, phone, and wide-desktop profiles
**When** localization and responsive suites run
**Then** every label, state, denial, expiry, action, and announcement has whole-string key parity with named placeholders, tablet/desktop layouts preserve decision context, and phone/lightweight review follows the declared restrictions
**And** all ten matrix-v7 lock-bearing families — `ProposalResolution`, `PolicyPublication`, `TenantBudgetUpdate`, `LegalHold`, `ExportRequest`, `DeletionRequest`, `ProviderCatalogMutation`, `AgentSetupMutation`, `AgentActivation`, and `DataHandlingAcceptance` — fail closed with a visible localized reason at the most restrictive viewport whenever required context cannot be presented safely.

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
| OwnedClauses | FR22.NFR13-and-NFR14-launch-evidence; FR23.UI-API-contract-parity; FR28.normative-Level4-5-UI-evidence; NFR13.WCAG2.2AA-every-interactive-route-and-state; NFR13.whole-string-EN-FR-parity; NFR13.restrictive-viewport-high-impact-block; NFR14.page-p95-2.5s; NFR14.pending-p95-500ms; NFR14.terminal-render-announcement-p95-2s; NFR14.30-per-kind-and-InsufficientEvidence; UX-DR33-UX-DR41.accessibility-responsive-contract; UX-DR45.readiness-rendering; UX-DR49.kind-discriminated-browser-samples; UX-DR50.authoritative-truth-flows; AD-17.complete-public-UI-matrix-v7-family-conformance; AD-24.browser-ingress-and-sample-contract |
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
| Requirements | FR19-FR25, FR28; NFR1-NFR14; UX-DR1, UX-DR2, UX-DR9-UX-DR14, UX-DR20-UX-DR30, UX-DR33, UX-DR36, UX-DR39-UX-DR41, UX-DR45, UX-DR47-UX-DR50; AD-8, AD-10, AD-17, AD-22-AD-26; EXT-CONV-AI-1, EXT-CONV-UI-1, EXT-HOST-1, EXT-PROVIDER-1, EXT-SAFETY-1, EXT-TOKEN-1, EXT-SECRETS-1, EXT-PROTECTION-1, EXT-TOPOLOGY-1, EXT-PARTIES-1; LR-TOPOLOGY through LR-PRODUCT-METRICS |
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
- **External:** `EXT-PROTECTION-1`, `EXT-TOPOLOGY-1`, `EXT-CONV-AI-1`, and `EXT-PARTIES-1` must be `Available` for live disclosure, production-like isolation/review evidence, current Conversation access/existence decisions, and current/historical human actor-binding evidence.
- **Forward dependencies:** None. `RQ-1` may consume its bounded evidence but Story 8.8 does not execute the release gate.

**Acceptance Criteria:**

**Given** a current Participant with Source Conversation read access
**When** posted provenance is inspected
**Then** the public contract returns only the posted Message provenance allowed by current Conversation authorization
**And** unposted versions, rejected or failed content, context metadata, and protected payloads remain undisclosed.

**Given** a request for unposted content or context metadata
**When** disclosure is evaluated
**Then** it requires either the Party durably recorded as Eligible Approver for that proposal or `AuditInspection(TenantId, InspectionId)` scoped to a named Conversation or case with a nonblank justification
**And** the compliance path computes the subject set from callers, editors, Approvers, decision actors, Facilitators in scope, and the Tenant Agent Administrator whose configuration was in force; the second party must be outside that set and not the Inspector, using the current Tenant Agent Administrator when eligible and otherwise the Platform Operator or a second Compliance Inspector.

**Given** a second party is eligible for an inspection
**When** pre-approval or post-hoc review is recorded
**Then** two Compliance Inspectors cannot approve each other within 30 days, any scope wider than one Conversation requires Platform Operator approval, and post-hoc review is allowed only for a single proposal or single Conversation and must finish within 7 days
**And** the Party-bearing Inspector and Party-bearing tenant reviewer resolve current human/liveness and historical actor binding through `EXT-PARTIES-1`; an `Administrator` reviewer proves current tenant role plus durable actor/role evidence, and a `Platform` reviewer proves current or historically recorded Hexalith.Tenants global-administrator authority without inventing a tenant Party. Separation compares stable `AuthenticatedHumanActorId`; missing, stale, ambiguous, overlapping, non-human where a Party is required, unavailable, same-actor, or incompatible evidence blocks approval/disclosure, while a missed post-hoc deadline remains recorded and visible.

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
| Requirements | FR19-FR25, FR28-FR30, FR33, FR34; OQ-30; NFR1-NFR7, NFR11, NFR13; UX-DR9-UX-DR18, UX-DR29-UX-DR41, UX-DR46, UX-DR50; AD-2, AD-8, AD-12, AD-14, AD-17, AD-22, AD-27, AD-30; EXT-CONV-AI-1; EXT-PROTECTION-1; EXT-TOPOLOGY-1; EXT-PARTIES-1 |
| OwnedClauses | FR20.audit-inspection-authorization; FR23.audit-inspection-public-contract; FR24.inspection-self-audit; FR24.computed-subject-set-and-second-party; OQ30.inspector-anti-collusion-and-seven-day-review; NFR2.protected-evidence-disclosure; AD-2.AuditInspection; AD-22.two-level-inspection-and-surviving-evidence; AD-30.compliance-principal; matrix-v7.AuditInspection |
| Dependencies | Stories 5.4, 5.8, 7.1, and 8.1; EXT-PROTECTION-1, EXT-TOPOLOGY-1, EXT-CONV-AI-1, and EXT-PARTIES-1 Available |
| EvidenceLevel | Levels 2, 4, and 5: aggregate/authorization behavior, live protected disclosure, and production-like isolation/review proof |
| TestOrArtifact | AuditInspectionAggregateTests; AuditInspectionHumanActorBindingIntegrationTests; AuditInspectionApiUiParityTests; AuditInspectionLiveTests; InspectionReviewWindowTests; inspection actor-binding evidence manifest |
| VerificationCommand | pwsh ./eng/verify-story-8.8.ps1 |
| NegativeEvidence | AuditInspectionIsolationTests.CrossTenantWrongRoleUnapprovedAndOverRateReadsAreDenied; MissingStaleNonHumanUnavailableOrSameActorInspectionApprovalTests; PlatformApproverDoesNotRequireInventedTenantPartyTests; ForgedCompliancePrincipalTests; ErasedContentCannotBeRevivedTests; InspectionSurvivesSourceDeletionTests |
| Result | Blocked — backlog; EXT-PROTECTION-1, EXT-TOPOLOGY-1, EXT-CONV-AI-1, and EXT-PARTIES-1 are Uncommitted |

## Release Gate RQ-1 — Outside The Story Backlog

`RQ-1` is a non-estimated operational release gate, not an epic or story and not an implementation umbrella. It runs only after Epics 5–8 are complete, every consumed critical dependency is `Available`, and controlled production-like qualification has produced current qualifying Levels 4 and 5 evidence.

For one authorized `TenantScope`, versioned `EnvironmentProfile`, and single `launch-readiness` projection checkpoint, `RQ-1` evaluates all 18 minimum GateIds, real NFR-9/NFR-14 samples, pre-enablement SM-1/SM-4/SM-5/SM-6 qualification evidence, audit completeness, evidence freshness and contracts, and unresolved blockers. It returns a new dated READY or NOT READY report. SM-2/SM-3/SM-7 remain post-enablement launch-health metrics and are not `RQ-1` inputs. Any missing, stale, blocked, insufficient, lower-level, skipped, placeholder, conditional, contract-incompatible, or unavailable-dependency input yields NOT READY; deterministic calculator fixtures never prove live attainment. The gate aggregates completed evidence only and owns no missing source, package, runtime, governance, capacity, UI, test, or remediation implementation.
