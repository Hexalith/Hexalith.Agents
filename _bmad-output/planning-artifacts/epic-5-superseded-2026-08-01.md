---
title: Superseded Epic 5 - Production Binding And Live Conformance
status: superseded
mustNotImplement: true
supersededOn: 2026-08-02
approvedProposal: /home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md
supersededBy: /home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/sprint-change-proposal-2026-08-02.md
sourceDocument: /home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/epics.md
sourceEpic: 5
archiveContainsVerbatimEpic: true
verbatimSha256: 003a0b482ba1d9cb5e72d7eaf94c745a64afb94a622e778997514d05243ba45f
replacementMap:
  "5.1": ["5.1", "5.6"]
  "5.2": ["5.5", "5.7", "8.7", "RQ-1"]
  "5.3": ["5.2", "5.3", "5.5"]
  "5.4": ["5.4", "7.1", "7.2", "7.3", "7.4", "7.5", "7.6", "8.2", "8.3"]
  "5.5": ["5.5", "6.1", "7.1", "7.2", "7.3", "7.4", "7.5", "7.6", "8.7"]
  "5.6": ["5.3", "5.5", "6.2", "6.4", "7.3"]
  "5.7": ["6.1", "7.1", "7.3", "7.4", "7.6"]
  "5.8": ["6.2", "7.3"]
  "5.9": ["6.3", "7.3", "7.4", "8.4"]
  "5.10": ["5.3", "6.4", "8.4", "8.5"]
  "5.11": ["6.6", "7.4"]
  "5.12": ["6.7", "8.6"]
  "5.13": ["7.1", "8.6"]
  "5.14": ["7.2", "7.3", "7.4", "7.5", "7.6", "8.6"]
  "5.15": ["8.1"]
  "5.16": ["8.2", "8.3"]
  "5.17": ["8.4", "8.5", "8.7"]
  "5.18": ["5.1", "5.2", "5.3", "5.4", "5.5", "5.6", "5.7", "6.1", "6.2", "6.3", "6.4", "6.5", "6.6", "6.7", "7.1", "7.2", "7.3", "7.4", "7.5", "7.6", "8.1", "8.2", "8.3", "8.4", "8.5", "8.6", "8.7", "RQ-1"]
historicalCriterionReplacements:
  - criterion: "Story 1.1 Agents-owned AppHost"
    mustNotImplement: true
    replacements: ["AD-16", "5.1", "5.6", "EXT-HOST-1"]
  - criterion: "Story 2.3 bounded context"
    mustNotImplement: true
    replacements: ["PRD OQ-10", "AD-11", "6.2"]
  - criterion: "Story 2.6 alternate invocation"
    mustNotImplement: true
    replacements: ["PRD OQ-1", "UX-DR24", "6.7"]
  - criterion: "Story 4.2 unresolved governance"
    mustNotImplement: true
    replacements: ["PRD OQ-8", "AD-22", "8.1", "8.2", "8.3"]
  - criterion: "Story 4.4 reporting-only cost"
    mustNotImplement: true
    replacements: ["PRD OQ-6", "AD-21", "6.4", "8.4"]
  - criterion: "Story 4.5 alternate workflow/MCP/A2A"
    mustNotImplement: true
    replacements: ["AD-18", "AD-19", "5.1-8.7 focused evidence", "RQ-1"]
---

# Superseded Epic 5 Archive

This file preserves the former active Epic 5 verbatim as historical planning evidence. It is not executable authority. Every archived story and criterion is `mustNotImplement`; use the approved proposal, current canonical spines/registers, active replacement stories, and the maps below.

## Complete Old-To-New Mapping

| Superseded story | Replacement authority |
| --- | --- |
| 5.1 | 5.1, 5.6 |
| 5.2 | 5.5, 5.7, 8.7, RQ-1 |
| 5.3 | 5.2, 5.3, 5.5 |
| 5.4 | 5.4, 7.1-7.6, 8.2, 8.3 |
| 5.5 | 5.5, 6.1, 7.1-7.6, 8.7 |
| 5.6 | 5.3, 5.5, 6.2, 6.4, 7.3 |
| 5.7 | 6.1, 7.1, 7.3, 7.4, 7.6 |
| 5.8 | 6.2, 7.3 |
| 5.9 | 6.3, 7.3, 7.4, 8.4 |
| 5.10 | 5.3, 6.4, 8.4, 8.5 |
| 5.11 | 6.6, 7.4 |
| 5.12 | 6.7, 8.6 |
| 5.13 | 7.1, 8.6 |
| 5.14 | 7.2-7.6, 8.6 |
| 5.15 | 8.1 |
| 5.16 | 8.2, 8.3 |
| 5.17 | 8.4, 8.5, 8.7 |
| 5.18 | Focused evidence in every replacement Story 5.1-8.7; final aggregation only in RQ-1 |

## Conflicting Historical Criteria

| Historical criterion | mustNotImplement | Replacement |
| --- | --- | --- |
| Story 1.1 Agents-owned AppHost | true | AD-16, 5.1, 5.6, EXT-HOST-1 |
| Story 2.3 bounded context | true | PRD OQ-10, AD-11, 6.2 |
| Story 2.6 alternate invocation | true | PRD OQ-1, UX-DR24, 6.7 |
| Story 4.2 unresolved governance | true | PRD OQ-8, AD-22, 8.1-8.3 |
| Story 4.4 reporting-only cost | true | PRD OQ-6, AD-21, 6.4, 8.4 |
| Story 4.5 alternate workflow/MCP/A2A | true | AD-18/AD-19, per-story focused evidence, RQ-1 |

## Verbatim Superseded Epic

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
