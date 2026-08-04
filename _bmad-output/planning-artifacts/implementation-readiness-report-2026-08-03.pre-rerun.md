---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
overallReadinessStatus: NOT READY
assessmentDate: 2026-08-03
documentsIncluded:
  prd:
    - _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md
    - _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/addendum.md
  architecture:
    - _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
    - _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/IMPLEMENTATION-CONVENTIONS.md
  epics:
    - _bmad-output/planning-artifacts/epics.md
  ux:
    - _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md
    - _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md
documentsExcluded:
  - _bmad-output/planning-artifacts/epic-5-superseded-2026-08-01.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-08-03
**Project:** agents

## Document Discovery

### PRD

- No whole PRD document was found at the planning-artifacts root.
- Selected `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md` (54,600 bytes; modified 2026-08-01).
- Selected `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/addendum.md` (1,950 bytes; modified 2026-08-01).
- The PRD folder has no `index.md`; its review, reconciliation, polishing, validation, and memory-log artifacts are not assessment inputs.

### Architecture

- No whole architecture document was found at the planning-artifacts root.
- Selected `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md` (58,356 bytes; modified 2026-08-02).
- Selected `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/IMPLEMENTATION-CONVENTIONS.md` (6,176 bytes; modified 2026-07-31).
- The architecture folder has no `index.md`; its review and memory-log artifacts are not assessment inputs.

### Epics and Stories

- Selected `_bmad-output/planning-artifacts/epics.md` (208,716 bytes; modified 2026-08-02).
- Excluded `_bmad-output/planning-artifacts/epic-5-superseded-2026-08-01.md` (32,080 bytes; modified 2026-08-02) because it is explicitly superseded.
- No sharded epic document was found.

### UX Design

- No whole UX document was found at the planning-artifacts root.
- Selected `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md` (23,049 bytes; modified 2026-08-02).
- Selected `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md` (39,907 bytes; modified 2026-08-02).
- The UX folder has no `index.md`; its review, validation, and memory-log artifacts are not assessment inputs.

### Discovery Resolution

- No whole-versus-sharded duplicate formats were found.
- The assessment input set was confirmed by the user on 2026-08-03.


## PRD Analysis

**Sources analyzed completely:**

- `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md`
- `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/addendum.md`

### Functional Requirements

#### FR-1: Configure `hexa`

Agent Administrators can create or enable `hexa` with a stable Agent identity, display name, description, Agent Instructions, lifecycle state, and tenant scope.

**Consequences (testable):**
- The system prevents activation when required Agent fields are missing or invalid.
- The system exposes the current Agent configuration through the admin UI and API/client contracts.
- The system records configuration changes in Audit Evidence with actor, timestamp, prior value where safe to expose, and new value.

#### FR-2: Link Agent To Party Identity

Agent Administrators can provision or link the Agent's Party identity so `hexa` appears as a known AI participant when it posts to a Conversation.

**Consequences (testable):**
- An active Agent has exactly one Party identity.
- The system rejects posting an Agent Response when the Agent Party identity is missing, disabled, ambiguous, or unauthorized for the Source Conversation.
- Conversation Messages posted by `hexa` are attributable to the Agent's Party identity, not to the caller or a generic system account.

#### FR-3: Manage Agent Lifecycle

Agent Administrators can activate, disable, and inspect `hexa` lifecycle state.

**Consequences (testable):**
- Disabled Agents cannot be called from Conversations.
- Disabling an Agent does not delete existing Audit Evidence, Proposed Agent Replies, or Conversation Messages.
- Lifecycle changes are auditable and visible through admin UI and API/client contracts.

### 4.2 Provider Governance And Per-Agent Model Selection

**Description:** Hexalith Agents uses a Global Providers Aggregate to govern available AI providers and models. Each Agent selects its Provider and model from that governed catalog rather than using an implicit hardcoded provider. This realizes UJ-1 and UJ-4.

**Functional Requirements:**

#### FR-4: Manage Global Providers Aggregate

Authorized administrators can configure the Global Providers Aggregate with provider records, model options, enabled/disabled state, and provider capability metadata needed for Agent selection.

**Consequences (testable):**
- Disabled providers or models cannot be selected for new Agent configuration.
- Existing Agents using a disabled provider/model cannot be activated or called until reconfigured. A documented migration state may allow temporary read-only inspection.
- Provider configuration changes are auditable without exposing secrets in logs, API responses, UI, or Audit Evidence.

#### FR-5: Select Provider And Model Per Agent

Agent Administrators can select a Provider and model for `hexa` from the Global Providers Aggregate.

**Consequences (testable):**
- The system validates that the selected Provider and model are enabled and usable before Agent activation.
- The system stores enough provider/model identity in Audit Evidence to explain which Provider and model produced each generated version.
- Changing provider/model selection affects future Agent Calls only and does not rewrite historical proposal or response evidence.

### 4.3 Response Policy And Approver Configuration

**Description:** Agent Administrators configure whether `hexa` posts automatically or creates proposals requiring confirmation. When confirmation is enabled, all approval authority comes from the Agent's Approver Policy. This realizes UJ-1 and UJ-3.

**Functional Requirements:**

#### FR-6: Configure Response Mode

Agent Administrators can configure `hexa` for Automatic Response Mode or Confirmation Response Mode.

**Consequences (testable):**
- Automatic Response Mode posts successful Agent Responses directly to the Source Conversation after authorization and generation complete.
- Confirmation Response Mode creates Proposed Agent Replies outside the Conversation and never posts unapproved generated content.
- Response mode changes affect future Agent Calls only.

#### FR-7: Configure Approver Policy

Agent Administrators can define all Approvers through the Agent's Approver Policy, including policy sources such as the Conversation owner, the caller, predefined Parties, or tenant roles.

**Consequences (testable):**
- The system authorizes proposal edit, regeneration, approval, rejection, abandonment, and expiry-resolution actions using the Approver Policy.
- The system rejects approval actions by Parties not authorized by the current policy for the proposal.
- The system exposes which configured policy source authorized the Approver according to a defined disclosure category: user-visible, operator-only, redacted, or omitted.
- The proposal records the policy basis used for each approval-related decision.
- API/client contracts and admin UI use the same disclosure category for the same approval-policy basis.

### 4.4 Explicit Conversation-Originated Invocation

**Description:** Conversation Participants call `hexa` through the Conversation-owned **Call hexa** action. V1 does not activate Agents automatically from Conversation changes and does not expose mention, command, or alternate invocation entry points. This realizes UJ-2 and UJ-3.

**Functional Requirements:**

#### FR-8: Call Agent From Conversation

Authorized Conversation Participants can explicitly call `hexa` from a Source Conversation with a user prompt or request.

**Consequences (testable):**
- Agent Calls require Source Conversation access and Agent call permission.
- Unauthorized calls fail before Provider invocation.
- Every Agent Call records caller, Agent, Source Conversation, request timestamp, and response mode.

#### FR-9: Build V1 Conversation Context

The system supplies the Agent with Conversation Context according to the configured Conversation Context Policy.

**Consequences (testable):**
- V1 Agent generation uses Conversation Context only.
- V1 generation does not include long-term memory, project content, folder content, external tool output, or external-channel content.
- When the full Source Conversation fits the selected Provider/model's safe context budget, V1 generation uses the full Source Conversation.
- When the Source Conversation exceeds the selected Provider/model's safe context budget, the call fails closed before Provider invocation; V1 never truncates, summarizes, windows, or otherwise reduces the Conversation.
- Agent Calls record that complete context was used or that the call was blocked, the Conversation Context Policy version or equivalent identifier, and enough context metadata for audit without leaking unrelated tenant data.
- If the complete Conversation Context cannot be loaded or sent safely, the Agent Call fails closed and creates neither Provider work, a Proposed Agent Reply, nor a Conversation Message.

#### FR-10: Handle Generation Failure

The system handles Provider failures, timeout, disabled provider/model state, invalid context, Content Safety Policy failures, and policy failures without posting incomplete or unsafe Agent Responses.

**Consequences (testable):**
- Failed generation creates status and Audit Evidence visible to authorized administrators or callers.
- Failed generation does not create a Conversation Message.
- Failed generation in Confirmation Response Mode does not create a Proposed Agent Reply. Any failed or incomplete generated content retained for authorized audit is stored only in a separate, non-approvable failure record.

### 4.5 Automatic Agent Responses

**Description:** In Automatic Response Mode, `hexa` posts the generated response directly into the Source Conversation as an attributed AI participant. This realizes UJ-2.

**Functional Requirements:**

#### FR-11: Post Automatic Response

When `hexa` is configured for Automatic Response Mode, the system posts successful generated content to the Source Conversation as a Conversation Message attributed to the Agent's Party identity.

**Consequences (testable):**
- The posted message references the Agent Call or equivalent trace identifier.
- The posted message does not appear as authored by the caller.
- The system records Audit Evidence linking caller, Agent, Provider/model, Source Conversation, generated content, and posted Conversation Message.

#### FR-12: Prevent Automatic Posting When Policy Fails

The system prevents automatic posting when authorization, Agent lifecycle, Provider/model, Party identity, Source Conversation access, Conversation Context Policy, Content Safety Policy, or generation status is invalid.

**Consequences (testable):**
- No Conversation Message is created when a required policy check fails.
- No Conversation Message is created when generated content fails the active Content Safety Policy.
- The failure reason is visible through authorized status surfaces without leaking secrets or unrelated tenant data.
- Audit Evidence distinguishes authorization, context policy, content safety, Provider/runtime, and posting failures.

### 4.6 Proposed Agent Reply Workflow

**Description:** In Confirmation Response Mode, generated Agent output is managed outside the Conversation as a Proposed Agent Reply. Approvers can edit, regenerate, approve, reject or abandon, and proposals can expire. Only an approved version is posted to the Source Conversation. This realizes UJ-3.

**Functional Requirements:**

#### FR-13: Create Proposed Agent Reply

When `hexa` is configured for Confirmation Response Mode, successful generation creates a Proposed Agent Reply linked to the Source Conversation and Agent Call.

**Consequences (testable):**
- A Proposed Agent Reply is not a Conversation Message.
- A Proposed Agent Reply records caller, Agent, Source Conversation, generated version, Provider/model, response mode, and current proposal state.
- Authorized Approvers can discover pending proposals requiring their action.
- V1 uses in-product pending-proposal visibility only for authorized Approvers, including a pending count, a queue, and a Conversation status entry.
- Email, push, and external-channel proposal notifications are not included in V1.

#### FR-14: Preserve All Proposal Versions

The system preserves every generated, edited, and regenerated content version for each Proposed Agent Reply.

**Consequences (testable):**
- Editing a proposal creates a new Versioned Proposal Content record or equivalent immutable version entry.
- Regeneration creates a new generated version without deleting prior generated or edited versions.
- Approval identifies exactly which version was approved and posted.

#### FR-15: Edit Proposed Reply

Authorized Approvers can edit Proposed Agent Reply content before approval.

**Consequences (testable):**
- Only authorized Approvers can edit proposal content.
- Edits preserve the prior version and author of the edit.
- Edited content remains outside the Conversation until approved.

#### FR-16: Regenerate Proposed Reply

Authorized Approvers can request regeneration of a Proposed Agent Reply before approval.

**Consequences (testable):**
- Regeneration uses the same Source Conversation and Agent configuration unless the system records an explicit configuration version change.
- Regeneration preserves prior versions and creates a new generated version.
- Regeneration is blocked after a proposal reaches a terminal state.

#### FR-17: Approve Proposed Reply

Authorized Approvers can approve a selected proposal version, causing it to be posted to the Source Conversation as `hexa`.

**Consequences (testable):**
- Approval posts exactly the approved version and no other proposal version.
- The Conversation Message is attributed to the Agent's Party identity.
- Audit Evidence links the approved version, Approver, approval timestamp, and posted Conversation Message.

#### FR-18: Reject, Abandon, Or Expire Proposed Reply

Authorized Approvers or system policy can move a Proposed Agent Reply to rejected, abandoned, or expired terminal states.

**Consequences (testable):**
- Terminal proposals cannot be approved or posted.
- Terminal proposals preserve all generated and edited versions for audit.
- The default expiry duration is 24 hours. An Agent Administrator may configure a duration from 1 hour through 30 days for future proposals only.
- A durable Dapr Workflow timer moves a non-terminal proposal to `Expired` at or after its stored `ExpiresAt`; an expiry policy change never changes an existing proposal's `ExpiresAt`.
- Expiry behavior and the stored `ExpiresAt` are visible through admin UI and API/client contracts.

### 4.7 Authorization, Tenant Isolation, And Governance

**Description:** Hexalith Agents must preserve Hexalith tenant isolation, Party identity boundaries, and fail-closed authorization. Agent configuration, calls, proposals, approval actions, posting, and audit inspection all require explicit permission. This realizes all journeys.

**Functional Requirements:**

#### FR-19: Enforce Tenant Isolation

The system enforces tenant isolation across Agent configuration, Provider selection, Agent Calls, Conversation Context, Proposed Agent Replies, Conversation posting, and Audit Evidence.

**Consequences (testable):**
- A Party from one tenant cannot call, inspect, approve, or post Agent responses for another tenant.
- Provider/model configuration and Agent configuration cannot leak across tenant boundaries unless explicitly platform-scoped and authorized.
- Audit/status queries return only tenant-authorized records.

#### FR-20: Enforce Role And Policy Authorization

The system enforces authorization for Agent administration, provider administration, Agent calling, proposal discovery, editing, regeneration, approval, rejection, abandonment, posting, and audit inspection.

**Consequences (testable):**
- Authorization failures occur before Provider invocation or Conversation posting.
- The same authorization rules apply through admin UI and API/client contracts.
- Authorization decisions are auditable at a level sufficient to explain denial or approval without leaking sensitive content.

#### FR-21: Fail Closed On Dependency Uncertainty

The system fails closed when required Party, Conversation, Provider, Agent, tenant access, or approval policy state is missing, stale, ambiguous, disabled, or unavailable.

**Consequences (testable):**
- Missing or stale Conversation access prevents Agent Calls and approval posting.
- Missing or disabled Agent Party identity prevents posting.
- Missing Provider/model state prevents generation.
- A critical external dependency is not implementation-ready until its external dependency register entry satisfies every commitment field defined in §8.
- An `Uncommitted` dependency, missing target, or missing compatibility verification command blocks every consuming story from `ready-for-dev`.

### 4.8 Admin UI And API/Client Contracts

**Description:** V1 includes both an admin web UI and public API/client contracts. These surfaces must expose the same governed capability without leaking internal implementation mechanics. This realizes UJ-1 and UJ-4.

**Functional Requirements:**

#### FR-22: Provide Admin UI

The admin UI allows authorized administrators to manage Global Providers Aggregate entries, configure `hexa`, inspect lifecycle state, configure response and approver policy, and view Agent operation and proposal status.

**Consequences (testable):**
- Admin UI actions enforce the same authorization rules as API/client contracts.
- Admin UI never exposes Provider secrets.
- Admin UI clearly distinguishes active, disabled, invalid, pending proposal, failed call, and expired proposal states.
- Admin UI satisfies the accessibility, localization, responsive safety, and interaction-performance requirements in NFR-13 and NFR-14.

#### FR-23: Provide API And Client Contracts

The system exposes stable API/client contracts for Provider administration, Agent configuration, Agent Calls, proposal workflow, status inspection, and audit inspection.

**Consequences (testable):**
- API/client contracts do not require callers to use raw EventStore, internal aggregate, internal projection, or provider SDK details.
- API/client contracts return structured success and error results suitable for automation.
- JSON object evolution is additive within V1.
- Public enums define `Unknown = 0`; new values may be added, but an existing value's meaning cannot be reused.
- No public member or enum value is removed, renamed, or semantically reused within V1.
- A breaking public change requires a new major package/API version and package-consumer compatibility tests.

### 4.9 Audit Evidence And Operational Visibility

**Description:** Hexalith Agents must provide durable proof of Agent behavior and enough operational status to run the launch safely. This realizes UJ-3 and UJ-4.

**Functional Requirements:**

#### FR-24: Capture Agent Audit Evidence

The system captures Audit Evidence for Agent configuration, Provider/model configuration, Agent Calls, generation attempts, proposal versions, edits, regenerations, approvals, rejections, abandonments, expirations, automatic posts, and final Conversation Messages.

**Consequences (testable):**
- Every posted Agent Response can be traced back to caller, Agent, Source Conversation, Provider/model, generated content, and approval path where applicable.
- Every Proposed Agent Reply preserves all Versioned Proposal Content.
- Audit Evidence records the Content Safety Policy decision, Conversation Context Policy behavior, and policy/version identifiers where available.
- Audit Evidence is queryable by authorized users without exposing unrelated tenant data or Provider secrets.

#### FR-25: Expose Operational Status

The system exposes status for Agent readiness, Provider/model readiness, recent Agent Call outcomes, proposal queues, generation failures, approval completion, and posting outcomes.

**Consequences (testable):**
- Authorized administrators can identify whether `hexa` is callable for a tenant.
- Authorized administrators can distinguish configuration errors, authorization failures, context policy failures, content safety failures, Provider failures, generation failures, pending approvals, and posting failures.
- Status surfaces support launch monitoring of adoption and approval workflow metrics.

### 4.10 Content Safety And Launch Readiness

**Description:** V1 generation must be gated by explicit safety, context, cost, performance, and metric decisions before production or production-like launch validation. This realizes UJ-1, UJ-2, UJ-3, and UJ-4.

**Functional Requirements:**

#### FR-26: Configure Content Safety And Prompt Policy

Authorized administrators or release operators can define the active Content Safety Policy for `hexa`.

**Consequences (testable):**
- `hexa` cannot be enabled for production or production-like launch validation without an active Content Safety Policy.
- The Content Safety Policy defines prompt constraints, blocked or restricted output categories, safety failure handling, and audit treatment.
- Content Safety Policy changes are auditable and affect future Agent Calls only.
- Automatic Response Mode and Confirmation Response Mode use the same active Content Safety Policy unless a stricter mode-specific policy is configured.
- The active policy always blocks child sexual abuse/exploitation; credible threats or instructions for imminent serious harm; encouragement or instruction for suicide/self-harm; credential theft, malware deployment, or unauthorized compromise; secrets/tokens/private credentials; cross-tenant or unauthorized personal/Conversation data; and attempts to bypass tenant, authorization, audit, retention, or safety controls.
- Restricted hate, harassment, sexual, violent, illegal-activity, or sensitive-personal content requires an explicitly permitted tenant use case and Confirmation Response Mode.
- A policy version cannot weaken a retry already in progress; each retry uses policy at least as restrictive as the initial attempt.

#### FR-27: Enforce Safety Before Provider And Conversation Side Effects

The system applies Content Safety Policy to the prompt and complete authorized Conversation Context before Provider invocation, then applies it to generated output before that output becomes a Conversation Message or an approvable Proposed Agent Reply.

**Consequences (testable):**
- Generated content that fails Content Safety Policy cannot be posted automatically.
- Generated content that fails Content Safety Policy cannot become an approvable Proposed Agent Reply.
- Safety failures create authorized status and Audit Evidence without exposing unsafe content in surfaces where policy forbids display.
- Approvers cannot override a Content Safety Policy failure.
- Prompt and complete Conversation Context pass the active policy before Provider invocation, and generated output passes the active policy before any proposal or Conversation side effect.

#### FR-28: Define Launch Readiness Controls

V1 launch readiness requires the fixed metric thresholds, latency targets, full-context behavior, cost controls, audit governance, NFR-11 through NFR-14, external dependency commitments, and the normative evidence authority in §11.

**Consequences (testable):**
- Controlled production-like qualification may collect live evidence only after Content Safety Policy, Conversation Context Policy, cost controls, audit governance, and the required external dependency commitments are active and recorded. Qualification access does not authorize production enablement.
- Production enablement remains blocked until `RQ-1` records READY from the required live evidence and launch metrics.
- Per-tenant monthly and per-call cost caps are hard enforcement boundaries: 80% emits a warning, 100% fails closed, and atomic reservation plus reconciliation prevents concurrent overspend. The system reserves the maximum estimated attempt cost before Provider invocation, reconciles actual usage, releases any unused reservation only after confirming that no usage occurred, and reuses the same reservation for eligible retries. Missing pricing or budget state blocks invocation; reporting-only monitoring is insufficient.
- Automatic accepted-call-to-post latency is p95 at most 60 seconds and p99 at most 120 seconds. Confirmation accepted-call-to-proposal latency uses the same thresholds; approval-to-post latency is p95 at most 10 seconds and p99 at most 30 seconds.
- Pre-Provider authorization, policy, budget, and context rejections complete at p95 at most 2 seconds. Each performance gate uses at least 30 production-like executions.
- Production readiness requires live Evidence Levels 4 and 5 as defined in §11; lower levels, skips, placeholders, or conditional results cannot independently establish launch readiness.

**Total FRs: 28**

### Non-Functional Requirements

- **NFR-1 Security:** Agent configuration, Provider administration, Agent Calls, proposal actions, posting, and audit inspection must enforce tenant and Party authorization before side effects.
- **NFR-2 Privacy:** Conversation Context, proposal content, and Audit Evidence must not leak across tenants or unauthorized Parties.
- **NFR-3 Reliability:** Agent Calls must never create partial Conversation Messages on failure. Confirmation workflows must not lose generated or edited proposal versions.
- **NFR-4 Observability:** The system must expose enough status to debug configuration errors, Provider failures, authorization denials, pending approval bottlenecks, and posting failures.
- **NFR-5 Auditability:** Audit Evidence must preserve all generated and edited proposal versions and link final posted responses to their source call and approval path.
- **NFR-6 Provider Safety:** Provider secrets must be write-only or secret-backed where applicable and must never appear in logs, status payloads, audit records, or UI display.
- **NFR-7 Content Safety:** Agent generation must be governed by an active Content Safety Policy before generated content can create Conversation side effects.
- **NFR-8 Context Bounds:** Conversation Context must not be truncated, summarized, windowed, or otherwise reduced; oversized Conversations fail closed before Provider invocation.
- **NFR-9 Performance:** Automatic accepted-call-to-post latency is p95 ≤ 60 seconds and p99 ≤ 120 seconds; confirmation accepted-call-to-proposal latency is p95 ≤ 60 seconds and p99 ≤ 120 seconds; approval-to-post latency is p95 ≤ 10 seconds and p99 ≤ 30 seconds; fast pre-Provider rejection latency is p95 ≤ 2 seconds. Each gate requires at least 30 production-like executions.
- **NFR-10 Cost Control:** Hard per-tenant monthly and per-call caps warn at 80%, fail closed at 100%, and use atomic reservation plus reconciliation. Reporting-only controls do not satisfy launch readiness.
- **NFR-11 Availability And Recovery:** EventStore business state has RPO 0. Restart or replay cannot duplicate Provider attempts, proposal versions, timers, reservations, or Conversation posts. A production-like recovery exercise restores interaction processing within 15 minutes and preserves every terminal decision.
- **NFR-12 Capacity And Backpressure:** Before enablement, each environment records numeric per-tenant and system-wide concurrency, queue-depth, and backpressure limits. Exceeding a limit queues or rejects with a safe typed outcome before Provider invocation. Cross-tenant fairness and cost caps remain enforced. The release profile and numeric limits are visible in the readiness registry, not hidden in host configuration.
- **NFR-13 Accessible, Localizable, Responsive UI:** The final UX spines are binding launch authority. Interactive V1 surfaces meet WCAG 2.2 AA behavior, use whole-string localization with English/French key parity, and fail closed for high-impact actions when the viewport cannot present required context.
- **NFR-14 UI Interaction Performance:** In the production-like profile, an authorized page reaches a usable non-loading state at p95 ≤ 2.5 seconds; a submitted command renders an authoritative pending acknowledgement at p95 ≤ 500 ms; a projection-visible terminal change renders and is announced at p95 ≤ 2 seconds. Each gate uses at least 30 executions and returns `InsufficientEvidence`, not pass, when timestamps or samples are missing.

**Total NFRs: 14**

### Additional Requirements

The following PRD sections contain additional binding scope, dependency, governance, public-surface, evidence, success-metric, and decision requirements. They are preserved here without requirement-level summarization.

## 5. Non-Goals

- V1 will not provide long-term Agent memory.
- V1 will not connect Agents to tools or allow Agents to perform business actions outside adding approved or automatic replies to Conversations.
- V1 will not retrieve from project content, folder content, file content, or external knowledge bases.
- V1 will not activate Agents automatically on every Conversation change.
- V1 will not support project-triggered or folder-triggered activation.
- V1 will not support agent-to-agent orchestration.
- V1 will not integrate with external channels such as Slack, Teams, email, or SMS.
- V1 will not make unapproved generated content a Conversation Message.
- V1 will not rewrite or delete historical generated versions when an Approver edits or regenerates content.
- V1 will not expose Provider secrets through UI, API/client contracts, logs, or Audit Evidence.
- V1 will not silently truncate Conversation Context to fit a Provider/model budget.

## 6. MVP Scope

### 6.1 In Scope

- `hexa` as the first general-purpose Agent.
- Agent Party identity provisioning or linking through Hexalith.Parties.
- Global Providers Aggregate for governed Provider/model options.
- Per-Agent Provider/model selection.
- Agent Instructions, lifecycle, response mode, and Approver Policy configuration.
- Explicit Conversation-originated Agent Calls.
- Conversation Context Policy that uses the complete Source Conversation when it fits and fails closed before Provider invocation when it does not.
- Content Safety Policy and safety enforcement before conversation side effects.
- Automatic Response Mode.
- Confirmation Response Mode.
- Proposed Agent Reply lifecycle: generated, edited, regenerated, approved, rejected, abandoned, and expired.
- Preservation of all generated, edited, and regenerated proposal versions.
- Posting approved replies to Hexalith.Conversations as the Agent Party identity.
- Admin UI for Agent and Provider administration.
- API/client contracts for configuration, invocation, proposal workflow, status, and audit.
- Strict tenant isolation, authorization, fail-closed dependency handling, and Audit Evidence.
- Launch metrics, latency targets, context policy, and cost-control posture required for launch readiness.

### 6.2 Out Of Scope For MVP

- Long-term memory through Hexalith.Memories, deferred to V2.
- Configured Agent tools, deferred until governed conversation participation is proven.
- Project/folder activation through future Hexalith.Projects or Hexalith.Folders integration.
- Automatic activation from Conversation changes.
- External channel bots or bridges.
- Business workflow actions beyond adding Agent responses to Conversations.
- Multiple named Agents beyond `hexa`. Generalized internal structures are permissible, but V1 product behavior exposes only `hexa`.
- Fine-grained launch pricing, billing, or monetization.

## 8. Integration And Dependencies

A critical external dependency is implementation-ready only when its external dependency register entry records all of the following:

1. Named owner.
2. Owning repository.
3. Required artifact.
4. Target version or commit.
5. Target integration date.
6. Compatibility contract or test and its verification command.
7. Required Evidence Level.
8. Accepted status.
9. Consuming stories.

An `Uncommitted` status, missing target, or missing compatibility verification command blocks every consuming story from `ready-for-dev`. The initial register scope includes `EXT-CONV-AI-1`, `EXT-HOST-1`, `EXT-PROVIDER-1`, `EXT-SAFETY-1`, `EXT-TOKEN-1`, `EXT-SECRETS-1`, and `EXT-TOPOLOGY-1`; concrete ownership, targets, commands, status, and story mappings remain in the external dependency register.

- **Hexalith.Conversations:** Source Conversation access, complete Conversation Context loading, AI membership, and final Conversation Message posting depend on Conversations. External prerequisite `CONV-AI-1`, tracked as `EXT-CONV-AI-1`, requires Conversations to publish `IConversationClient.AddParticipantAsync` and `POST /api/v1/conversations/{conversationId}/participants`. For Agents, that contract is limited to stable `ParticipantType.AiAgent`/`AIAgent` plus `ParticipantRole.Member` membership and must provide deterministic idempotency, typed conflicts, and cross-tenant denial. It is subject to the same commitment rule as every critical external dependency. Hexalith Agents must not treat unapproved proposals as Conversation Messages or write Conversation streams directly.
- **Hexalith.Parties:** Agent identity and Conversation Participant identity depend on Parties. `hexa` must post as a Party identity.
- **Provider Infrastructure:** Provider/model availability depends on the Global Providers Aggregate and the underlying provider integration selected per Agent.
- **Tenant Access:** Tenant isolation and authorization must align with existing Hexalith tenant access patterns and fail closed when tenant state is missing or unavailable.
- **Admin Surface:** The admin UI must use the same capability and authorization model as the API/client contracts.
- **Release Governance:** Production or production-like generation depends on recorded launch-readiness gates for safety, context, metrics, latency, cost, and audit governance.

## 9. Data Governance And Audit

- Audit Evidence must be tenant-scoped and accessible only to authorized Parties or operators.
- Audit Evidence must preserve every generated, edited, and regenerated proposal version.
- Audit Evidence must link automatic posts and approved posts to the Agent Call, Source Conversation, caller, Agent, Provider/model, response mode, and final Conversation Message.
- Editing a Proposed Agent Reply must never overwrite the prior version.
- Regenerating a Proposed Agent Reply must never delete prior generated or edited versions.
- Rejected, abandoned, and expired proposals remain audit records and cannot later be posted.
- Provider secrets and raw credentials are never audit content.
- Sensitive Agent content is retained for 365 days after the interaction reaches a terminal state unless legal hold suspends expiry. Authorized export is tenant-scoped, encrypted, time-limited, manifested, and audited.
- EventStore history is never rewritten. Retention expiry or approved deletion cryptographically erases or redacts protected sensitive payloads and purges affected projections while retaining only a support-safe non-content tombstone; completion requires restrictive confirmation from payload protection and every affected projection.
- Posted Conversation Messages remain governed by Hexalith.Conversations retention.

## 10. API Contracts And Public Surface

V1 must expose public API/client contracts for these capability areas:

- Provider administration: create, update, list, enable, or disable Provider and model options where authorized.
- Agent administration: configure `hexa`, lifecycle, Party identity link, Agent Instructions, Provider/model selection, Response Policy, Approver Policy, Conversation Context Policy, and Content Safety Policy.
- Agent invocation: call `hexa` from a Source Conversation.
- Proposal workflow: list pending proposals, inspect proposal versions, edit, regenerate, approve, reject, abandon, and inspect expiry.
- Status: inspect Agent readiness, Provider readiness, context policy outcome, content safety outcome, Agent Call status, proposal state, and posting outcome.
- Audit: inspect authorized Audit Evidence for Agent Calls, proposal lifecycle, and posted responses.

The public surface must not require consumers to understand internal EventStore stream names, aggregate mechanics, projection internals, or provider SDK details.

All public contracts follow the V1 compatibility rules in FR-23. Concrete package pins, SDK choices, transport mechanics, and compatibility commands remain downstream architecture or dependency-register concerns.

## 11. Evidence And Release Qualification

Evidence Levels are normative PRD authority:

| Level | Normative Evidence Meaning |
| --- | --- |
| 1 | Contract or structure evidence. |
| 2 | Pure domain or unit behavior. |
| 3 | Fail-closed deferred seam. |
| 4 | Live component integration. |
| 5 | Cross-system production-like evidence. |

Production-like readiness requires live Levels 4 and 5 for runtime behavior, authorization, tenant isolation, Provider integration, content safety, Conversations integration, audit behavior, and topology. Lower levels, skipped checks, placeholders, or conditional results such as “where applicable” remain visible blockers and cannot establish launch readiness.

Each qualification metric has a versioned measurement contract that identifies the authoritative source events and timestamps; the numerator and denominator or percentile method; sample, window, and cohort rules; late-data and missing-data handling; and `InsufficientEvidence` conditions.

Approved deterministic fixtures may prove metric formulas, sample, window and cohort handling, timestamp rules, and `InsufficientEvidence` behavior. Passing these fixtures completes calculator implementation; it does not prove live metric attainment. Real rolling-window and cohort attainment, required live Levels 4 and 5, and the final READY or NOT READY decision remain the operational `RQ-1` gate after implementation.

## 12. Success Metrics

**Primary**

- **SM-1: Active tenant adoption** - At least one launch tenant configures `hexa`, enables a Provider/model, satisfies launch-readiness gates, and records successful Agent Calls in production or production-like launch validation. Validates FR-1 through FR-12 and FR-22 through FR-28.
- **SM-2: Conversation adoption** - At least 20% of eligible Conversations in the enabled launch cohort record one or more accepted Agent Calls during a rolling 30-day window, measured only when the cohort contains at least 50 eligible Conversations. Validates FR-8, FR-9, FR-11, FR-13, and FR-28.
- **SM-3: Approval workflow completion** - At least 95% of Proposed Agent Replies created during a rolling 30-day window reach a terminal state within 26 hours. Within the measured cohort, the expiry rate is at most 20%, the posting-failure rate is at most 2%, at least 70% are resolved by a human decision (`Approved`, `Rejected`, or `Abandoned`), and 100% have complete Audit Evidence. Validates FR-13 through FR-18, FR-24, and FR-28.

**Secondary**

- **SM-4: Unauthorized action prevention** - Authorization tests and launch telemetry show zero successful cross-tenant or unauthorized Agent Calls, proposal actions, or audit inspections. Validates FR-19 through FR-21.
- **SM-5: Audit completeness** - Every posted Agent Response has complete Audit Evidence linking caller, Agent, Source Conversation, Provider/model, proposal path when applicable, and final Conversation Message. Validates FR-11, FR-17, and FR-24.
- **SM-6: Admin/API parity** - Core administration and workflow operations are available through both admin UI and API/client contracts with the same authorization outcomes. Validates FR-22 and FR-23.

**Counter-Metrics (do not optimize blindly)**

- **SM-C1: Automatic post volume without review** - Do not maximize automatic posting if launch tenants choose confirmation for safety-sensitive contexts. Counterbalances SM-2.
- **SM-C2: Approval speed at the cost of audit quality** - Do not optimize approval completion time by dropping version preservation or approval evidence. Counterbalances SM-3.
- **SM-C3: Provider breadth before governance** - Do not optimize the number of Providers/models if Provider governance, secret safety, and per-Agent selection are not robust. Counterbalances SM-1.

## 13. V1 Decision Register

All implementation-blocking product and governance questions were resolved by the approved Correct Course decisions on 2026-08-01. The Evidence Level taxonomy is normative in §11. Changes apply to future calls and proposals unless a row states otherwise.

| ID | Binding V1 Decision | Owner | Status |
| --- | --- | --- | --- |
| OQ-1 | The sole V1 invocation entry is a Conversation-owned **Call hexa** action. No mention, command, ambient trigger, or alternate entry point is in V1. | Product + UX | Resolved 2026-08-01 |
| OQ-2 | Hexalith Agents owns durable proposal state and all proposal read models. Dapr Workflow owns execution only and does not become a domain system of record. | Architecture | Resolved 2026-08-01 |
| OQ-3 | Proposal expiry defaults to 24 hours and is configurable per Agent from 1 hour through 30 days for future proposals only. Dapr Workflow expires a proposal at or after its stored `ExpiresAt`. | Product + Architecture | Resolved 2026-08-01 |
| OQ-4 | V1 uses in-product proposal notifications and pending-count surfaces only. Email, push, and external-channel notification delivery are out of scope. | Product + UX | Resolved 2026-08-01 |
| OQ-5 | Automatic accepted-call-to-post and confirmation accepted-call-to-proposal are p95 ≤ 60 s and p99 ≤ 120 s. Approval-to-post is p95 ≤ 10 s and p99 ≤ 30 s. Fast pre-Provider rejection is p95 ≤ 2 s. Each gate uses at least 30 production-like executions. | Architecture + Release PM | Resolved 2026-08-01 |
| OQ-6 | V1 enforces hard per-tenant monthly and per-call caps, warns at 80%, and fails closed at 100%. It atomically reserves the maximum estimated attempt cost before Provider invocation, reconciles actual usage, releases any unused reservation only after confirming that no usage occurred, and reuses the reservation for eligible retries. Missing pricing/budget state blocks invocation; reporting-only monitoring is insufficient. | Product + Architecture | Resolved 2026-08-01 |
| OQ-7 | The Agents-owned Global Providers Aggregate records provider/model identifiers, enablement, capabilities and limits, secret references, versioned pricing metadata, and a `CapabilityVersion`. | Architecture | Resolved 2026-08-01 |
| OQ-8 | Sensitive Agent content is retained for 365 days after the interaction reaches a terminal state unless legal hold applies. Authorized export is encrypted and time-limited. Deletion cryptographically erases or redacts sensitive content and purges projections while immutable EventStore history retains only a safe tombstone. | Product + Governance | Resolved 2026-08-01 |
| OQ-9 | Prompt and complete Conversation Context are checked before Provider invocation, and output is checked before proposal or Conversation side effects. The active policy always blocks these categories: child sexual abuse/exploitation; credible imminent serious-harm threats/instructions; encouragement/instruction for suicide/self-harm; credential theft, malware, or unauthorized compromise; secrets/private credentials; cross-tenant or unauthorized personal/Conversation data; and control-bypass attempts. Restricted hate, harassment, sexual, violent, illegal-activity, or sensitive-personal content requires an explicitly permitted tenant use case and Confirmation Response Mode. Approvers cannot override failures, and retries cannot use a weaker policy. | Product + Security | Resolved 2026-08-01 |
| OQ-10 | V1 uses the complete Source Conversation or fails closed before Provider invocation. Truncation, summarization, windowing, and other bounded-context modes are prohibited. | Architecture | Resolved 2026-08-01 |
| OQ-11 | SM-2 is ≥ 20% adoption over a rolling 30 days with at least 50 eligible Conversations. SM-3 is ≥ 95% terminal within 26 hours, with an expiry rate ≤ 20%, a posting-failure rate ≤ 2%, human resolution ≥ 70%, and audit completeness = 100%. | Product + Release PM | Resolved 2026-08-01 |
| OQ-12 | V1 Agent Calls use only the complete authorized Source Conversation. Long-term memory, project content, folder content, external tools, and non-conversation retrieval are excluded. | Product + Architecture | Resolved 2026-08-01 |
| OQ-13 | Generalized internal Agent structures are permissible, but V1 product behavior exposes only the named Agent `hexa`. | Product | Resolved 2026-08-01 |

The addendum is explicitly contextual: it supplies competitive positioning and risk-awareness notes but does not override the PRD or product brief. Its four research notes reinforce named participant identity, precise Source Conversation context and posting authority, strict separation of proposals from Conversation Messages, and V1 deferral of tools, long-term memory, ambient triggers, and project/folder activation.

### PRD Completeness Assessment

- The PRD is marked `final`, was updated on 2026-08-01, and provides globally stable identifiers for all 28 FRs and 14 NFRs.
- Every FR includes testable consequences, and the cross-cutting NFRs include measurable launch thresholds where measurement is material.
- Scope boundaries are explicit through non-goals, MVP inclusions/exclusions, and the binding V1 Decision Register. All thirteen listed product and governance questions are marked resolved on 2026-08-01.
- Authorization, tenant isolation, provider safety, content safety, full-context fail-closed behavior, cost enforcement, audit retention, recovery, capacity, accessibility, localization, responsiveness, and interaction performance are explicitly covered.
- The PRD deliberately delegates concrete dependency commitments and operational launch decisions to the external dependency register, readiness registry, and post-implementation `RQ-1` gate. Those delegated artifacts must be consistent and complete before implementation or launch readiness can be established.
- The addendum has clear non-normative authority and introduces no competing requirements.
- Initial assessment: the PRD itself is structurally complete and implementation-oriented. Remaining readiness risk is concentrated in downstream traceability, external dependency commitments, architecture alignment, story quality, and live-evidence gates rather than missing PRD requirement categories.

## Epic Coverage Validation

### Epic FR Coverage Extracted

- FR1: Epic 5 — Configure `hexa` through live authorized EventStore operations and expose current setup state.
- FR2: Epics 5 and 6 — Bind exactly one live Party identity, prove posting eligibility, and attribute the automatic response to `hexa`.
- FR3: Epic 5 — Manage lifecycle without conflating `active` with authoritative callability.
- FR4: Epics 5 and 8 — Govern Provider/model capability, enablement, pricing, secret-configured state, and operational policy.
- FR5: Epics 5 and 6 — Select an enabled model and carry its current safe capability version into automatic execution.
- FR6: Epic 5 — Configure future-only Automatic or Confirmation Response Mode through live public operations.
- FR7: Epics 5 and 7 — Configure, resolve, and audit current Approver Policy authority.
- FR8: Epic 6 — Provide the sole Conversation-owned **Call hexa** action and create one authorized interaction.
- FR9: Epic 6 — Load the complete authorized Conversation, measure it exactly, or block before Provider invocation.
- FR10: Epic 6 — Return precise fail-closed generation outcomes and keep failed content outside proposals/messages.
- FR11: Epic 6 — Post exactly one successful automatic response as `hexa` through Conversations.
- FR12: Epic 6 — Prevent automatic posting whenever any authorization, context, safety, budget, capacity, identity, or posting gate fails.
- FR13: Epic 7 — Create and expose a pending Proposed Agent Reply only after successful confirmation-mode generation.
- FR14: Epic 7 — Preserve every generated, edited, and regenerated proposal version immutably.
- FR15: Epic 7 — Allow one authorized edit transition that preserves the prior version and authorship.
- FR16: Epic 7 — Regenerate through fresh current gates without losing history or reusing changed attempt inputs.
- FR17: Epic 7 — Approve and post exactly one selected version as `hexa` with linked evidence.
- FR18: Epics 7 and 8 — Reject, abandon, or deterministically expire proposals while retention/hold governance preserves evidence.
- FR19: Epics 5–8 — Enforce tenant isolation on every setup, call, proposal, governance, status, and audit surface with focused cross-tenant denial evidence.
- FR20: Epics 5–8 — Enforce role/policy authorization before every side effect and keep API/UI outcomes aligned.
- FR21: Epics 5–8 — Fail closed on dependency uncertainty and block story readiness until every declared external dependency is committed or available.
- FR22: Epics 5–8 — Deliver live FrontComposer/Fluent V5 administration, call, proposal, governance, and evidence surfaces under NFR-13/NFR-14.
- FR23: Epics 5–8 — Publish stable additive public contracts for all active outcomes with package-consumer and API/UI parity evidence.
- FR24: Epics 5–8 — Capture tenant-scoped durable evidence for setup, attempts, proposal actions, posting, policy, governance, and final messages.
- FR25: Epics 5–8 — Expose authoritative readiness, runtime, proposal, governance, performance, and blocker status.
- FR26: Epics 6 and 8 — Publish and operate the versioned Content Safety Policy used by prompt/context and output gates.
- FR27: Epics 6 and 7 — Enforce fresh no-weaker safety before Provider, proposal, regeneration, approval, and posting side effects.
- FR28: Epics 5–8 — Implement every launch control and emit bounded evidence; release gate `RQ-1` aggregates live attainment outside the story backlog.

**Total FRs in the active epic coverage map: 28.**

### Coverage Matrix

| FR | PRD requirement | Active epic coverage | Status |
| --- | --- | --- | --- |
| FR1 | Configure `hexa` | Epic 5 | ✓ Covered |
| FR2 | Link Agent to Party identity | Epics 5, 6 | ✓ Covered |
| FR3 | Manage Agent lifecycle | Epic 5 | ✓ Covered |
| FR4 | Manage Global Providers Aggregate | Epics 5, 8 | ✓ Covered |
| FR5 | Select Provider and model per Agent | Epics 5, 6 | ✓ Covered |
| FR6 | Configure response mode | Epic 5 | ✓ Covered |
| FR7 | Configure Approver Policy | Epics 5, 7 | ✓ Covered |
| FR8 | Call Agent from Conversation | Epic 6 | ✓ Covered |
| FR9 | Build V1 Conversation Context | Epic 6 | ✓ Covered |
| FR10 | Handle generation failure | Epic 6 | ✓ Covered |
| FR11 | Post automatic response | Epic 6 | ✓ Covered |
| FR12 | Prevent automatic posting when policy fails | Epic 6 | ✓ Covered |
| FR13 | Create Proposed Agent Reply | Epic 7 | ✓ Covered |
| FR14 | Preserve all proposal versions | Epic 7 | ✓ Covered |
| FR15 | Edit Proposed Reply | Epic 7 | ✓ Covered |
| FR16 | Regenerate Proposed Reply | Epic 7 | ✓ Covered |
| FR17 | Approve Proposed Reply | Epic 7 | ✓ Covered |
| FR18 | Reject, abandon, or expire Proposed Reply | Epics 7, 8 | ✓ Covered |
| FR19 | Enforce tenant isolation | Epics 5–8 | ✓ Covered |
| FR20 | Enforce role and policy authorization | Epics 5–8 | ✓ Covered |
| FR21 | Fail closed on dependency uncertainty | Epics 5–8 | ✓ Covered |
| FR22 | Provide admin UI | Epics 5–8 | ✓ Covered |
| FR23 | Provide API and client contracts | Epics 5–8 | ✓ Covered |
| FR24 | Capture Agent Audit Evidence | Epics 5–8 | ✓ Covered |
| FR25 | Expose operational status | Epics 5–8 | ✓ Covered |
| FR26 | Configure Content Safety and prompt policy | Epics 6, 8 | ✓ Covered |
| FR27 | Enforce safety before Provider and Conversation side effects | Epics 6, 7 | ✓ Covered |
| FR28 | Define launch-readiness controls | Epics 5–8 | ✓ Covered |

### Missing Requirements

- No PRD Functional Requirement is absent from the active FR Coverage Map.
- No FR identifier appears in the active coverage map that is absent from the PRD.
- No critical or high-priority missing FR coverage was identified.

### Coverage Statistics

- Total PRD FRs: 28
- FRs covered in active epics: 28
- Missing PRD FRs: 0
- Extra epic FR identifiers: 0
- Documentary FR coverage: 100%

### Coverage Authority Note

- Replacement Epics 5–8 are the active forward implementation authority and contain exactly 27 active stories.
- Epics 1–4 are retained only as completed historical delivery evidence; their completion does not establish current production conformance or release readiness.
- The former 18-story Epic 5 is superseded and non-executable.
- `RQ-1` is correctly represented as a non-estimated release gate outside the implementation backlog; it aggregates evidence and owns no missing implementation.
- This step validates FR traceability only. Story quality, sequencing, dependency readiness, architecture alignment, and evidence sufficiency are assessed in later steps.

## UX Alignment Assessment

### UX Document Status

**Found and complete.** The assessment used both final UX spines:

- `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md` — final, updated 2026-08-02.
- `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md` — final, updated 2026-08-02.

Both documents were read completely. They explicitly make the UX spines authoritative over mockups, wireframes, imports, and source-derived sketches.

### UX ↔ PRD Alignment

| Area | Alignment finding | Status |
| --- | --- | --- |
| UJ-1 Agent administration | Configuration, Party identity, Provider/model, instructions, response mode, approver policy, lifecycle, activation blockers, and callability are represented consistently. | Aligned |
| UJ-2 automatic response | The sole Conversation-owned **Call hexa** action, full-context behavior, authorization-before-Provider, Agent attribution, posting-pending distinction, and authoritative `posted` outcome match the PRD. | Aligned |
| UJ-3 confirmation workflow | Proposal creation after successful generation, immutable edits/regenerations, selected-version approval, expiry, posting, and preserved history match FR13–FR18. | Aligned |
| UJ-4 API integration | The UX keeps API/client contracts outside internal EventStore, projection, workflow, and Provider SDK mechanics and requires API/UI authorization parity. | Aligned |
| UJ-5 launch governance | UX adds a dedicated operator journey for safety, cost, retention, evidence, and qualification. It is derived from FR26–FR28, NFR9–NFR14, PRD §§9–12, and OQ-6/OQ-8/OQ-9 rather than adding unsupported product scope. | Aligned extension |
| V1 scope | Both UX spines exclude memory, tools, project/folder content, ambient triggers, external channels, alternate invocation entry points, and multiple product-visible Agents. | Aligned |
| Truth-state semantics | `active` is not callable; `approved` is not posted; generation failure creates no proposal; pending and terminal states require authoritative evidence. | Aligned |
| Governance and privacy | Tenant isolation, Provider-secret safety, support-safe evidence, 365-day retention, legal hold, encrypted export, and restrictive deletion are consistently represented. | Aligned |
| NFR-13/NFR-14 | WCAG 2.2 AA, English/French whole-string parity, responsive fail-closed behavior, exact browser-monotonic timing seams, thresholds, and sample sufficiency match the PRD. | Aligned |

No UX capability was found that contradicts the current V1 scope. The UX documents elaborate the PRD into 50 implementation-facing UX requirements without introducing a new business capability.

### UX ↔ Architecture Alignment

| UX need | Architectural support | Status |
| --- | --- | --- |
| FrontComposer and Fluent UI Blazor V5 | AD-15, AD-25, the stack definition, and the UI consistency convention require the same public contracts, FrontComposer inheritance, Fluent V5 semantics, and policy-gated navigation. | Supported |
| Canonical readiness/proposal/call states | AD-5, AD-10, AD-12, AD-13, AD-17, and the explicit projection inventory provide authoritative state, freshness, idempotency, and no-fallback rules. | Supported |
| Full Conversation Context | AD-6 and AD-11 require fresh authorized complete context, exact tokenization, and fail-closed behavior with no bounded fallback. | Supported |
| Two-stage safety and no weaker retry | AD-20 matches the UX prompt/context-before-Provider and output-before-side-effect model. | Supported |
| Cost and capacity UI | AD-21 and AD-24 define hard caps, atomic reservation, current pricing, shared admission, numeric limits, and safe blocker states. | Supported |
| Proposal versioning and posting truth | AD-4, AD-5, AD-13, and AD-18 support immutable versions, selected-version approval, durable expiry, exactly-once posting, and distinct `approved`/`posting pending`/`posted` states. | Supported |
| Accessibility, localization, responsiveness | AD-25 binds every interactive route and high-impact state to WCAG 2.2 AA, English/French parity, FrontComposer/Fluent inheritance, and restrictive-viewport blocking. | Supported |
| UI performance evidence | AD-26 defines the three discriminated browser-monotonic sample kinds, exact tick seams, p95 targets, at-least-30 evidence rule, attestation, and `InsufficientEvidence`. | Supported |
| Audit governance | AD-14, AD-17, and AD-22 support content protection, explicit projection inventory, retention, holds, export, deletion, and restrictive completion. | Supported |
| Platform composition | AD-16 assigns Agents DomainService/UI composition to the platform-owned host and forbids module-owned AppHost/Aspire/ServiceDefaults projects. | Designed, implementation gap acknowledged |

### Alignment Issues

#### UX-ALIGN-1 — Conversation-owned invocation UI seam is not concretely bound

**Severity:** High

The PRD and both UX spines require the sole V1 invocation entry to be a **Call hexa** action inside the Source Conversation. Architecture maps this capability to “Agents API/client and Conversation-owned Call hexa action,” but it does not define the concrete UI extension/registration contract, ownership boundary, package dependency, or external commitment that inserts the action into a Hexalith.Conversations surface. `EXT-CONV-AI-1` covers AI membership and posting, not the Conversation UI extension seam, and active Story 6.7 declares no new external seam.

**Impact:** The design can be implemented as a separate Agents surface while still failing the product requirement that the sole entry be Conversation-owned, or implementation can become blocked late by an unavailable Conversations/FrontComposer extension point.

**Recommendation:** Before Story 6.7 is ready for development, bind the invocation surface to an explicit owned contract. Either extend an existing committed Conversations/FrontComposer extension seam and name its verification command, or add a tracked external dependency with owner, target, date, compatibility contract, Evidence Level, status, and consuming story.

#### UX-ALIGN-2 — “Conversation owner” is implemented as `Facilitator`

**Severity:** Medium

The PRD and UX use “Conversation owner” as an Approver Policy source. AD-8 states that current Conversations contracts expose no owner field and therefore treats `ParticipantRole.Facilitator` as owner authority in V1. The UX documents do not consistently explain this substitution to administrators, although the epic inventory sometimes says owner/facilitator.

**Impact:** Policy authors and auditors may believe they selected a distinct owner authority when the runtime is actually authorizing Facilitators. Approval-basis disclosure can therefore be semantically misleading even if authorization is technically fail-closed.

**Recommendation:** Make the substitution normative in the PRD/UX copy and public contract, or introduce an explicit Conversations owner resolver. Until then, label the policy basis accurately as Facilitator-derived authority wherever it is configured or disclosed.

### Warnings

- AD-10 explicitly records current implementation gaps in capability high-water/effective-version handling. Provider readiness, context, regeneration, and related UI status cannot yet conform to the UX state model.
- AD-16 explicitly records that the checked-in solution still contains forbidden module-owned AppHost, Aspire, and ServiceDefaults projects. Stories 5.1 and 5.6 own correction and platform-host proof.
- All seven external prerequisites are currently `Uncommitted`. This does not make the UX documents misaligned, but it blocks live implementation/evidence for major UX paths and prevents affected stories from becoming `ready-for-dev`.
- Historical Epics 1–4 contain criteria that predate the current full-context-only and invocation decisions. The active replacement authority marks conflicting historical criteria `mustNotImplement`; implementation must follow Epics 5–8 and the final PRD/UX/Architecture set.

### UX Alignment Conclusion

The final UX spines are comprehensive and substantially aligned with the final PRD and architecture. The architecture directly supports the difficult UX invariants—authoritative truth states, fail-closed responsiveness, accessibility/localization evidence, and browser-monotonic performance measurement. UX readiness is not complete until the Conversation-owned invocation integration seam and owner-versus-Facilitator semantics are explicitly resolved, and the acknowledged implementation/dependency blockers remain active.

## Epic Quality Review

### Review Scope And Authority

The complete `epics.md` was reviewed. Replacement Epics 5–8 and their 27 stories are the executable forward backlog. Epics 1–4 were reviewed only for historical-authority contamination because the document explicitly marks them completed historical evidence rather than current implementation instructions.

### Epic Best-Practice Compliance

| Epic | User-value outcome | Independence | Story structure | Verdict |
| --- | --- | --- | --- | --- |
| Epic 5 — Live Governed Setup And Honest Readiness | An administrator can configure `hexa` and understand authoritative setup/callability. | Does not require Epics 6–8. External commitments block execution but are declared. | Mostly sequenced correctly; Stories 5.5 and 5.6 are oversized. | Conditional pass |
| Epic 6 — One Safe Automatic Conversation Response | A participant receives exactly one safe attributed response or a precise fail-closed result. | Uses Epic 5 only, but Story 6.4 conceals a dependency on later Story 6.5. | Strong BDD and negative evidence; Stories 6.4/6.5 are oversized. | Fail until forward dependency is removed |
| Epic 7 — Complete Confirmation And Approval | An Approver can discover, revise, resolve, and post one proposal version. | Uses Epics 5–6 only; no future epic dependency. | Strongly decomposed by proposal transition. Story 7.3 has an unnecessary dependency on 7.2. | Pass with minor sequencing concern |
| Epic 8 — Governance Operations And Release Qualification | Governance/release operators can retain, export, delete, operate policy, calculate evidence, and inspect blockers. | Uses prior epics, but Story 8.1 delegates part of its promised outcome to later Story 8.3. | Bundles several unrelated governance and technical qualification outcomes; multiple stories are epic-sized. | Fail until decomposed and forward dependency removed |

### Active Story Review

| Story | Quality verdict | Main finding |
| --- | --- | --- |
| 5.1 | Conditional pass | Technical foundation story is justified by the corrected structural seed and explicitly avoids pre-creating future domain entities. Scope is broad but appropriate as the first corrective baseline. |
| 5.2 | Pass | Clear administrator outcome, prior-only dependency, replay/idempotency, authorization, and projection-confirmed UI criteria. |
| 5.3 | Pass | Cohesive Provider catalog outcome with safe secret boundaries and explicit external gate. |
| 5.4 | Pass | Cohesive identity/authority readiness outcome with focused revocation and cross-tenant negative evidence. |
| 5.5 | Major concern | Combines readiness aggregate/projection semantics, Provider readiness contract, operation-gate matrix parity, API/UI behavior, and tenant isolation in one story. |
| 5.6 | Major concern | Combines external host composition, secrets, Dapr access control, evidence ingress, topology capture, package ownership, and production-like proof across repository boundaries. |
| 5.7 | Pass | Clear activation/callability decision outcome with deterministic fixture limits and current-gate revocation. |
| 6.1 | Pass | Durable-workflow/recovery outcome is technically heavy but cohesive and independently demonstrable with deterministic activities. |
| 6.2 | Pass | Complete-context-or-blocked behavior is bounded and testable. |
| 6.3 | Pass | Two-stage safety is cohesive and includes complete negative-policy coverage. |
| 6.4 | Critical violation | Claims Provider invocation through an injected admission grant while stating production call acceptance cannot mint the grant until Story 6.5. This is a concealed forward dependency and weakens independent completion. |
| 6.5 | Major concern | Shared allocator configuration, linearizable admission, durable queues, weighted fairness, fencing, cancellation, crash recovery, and multi-replica qualification are epic-sized. |
| 6.6 | Pass | Exactly-once membership/posting outcome is cohesive and depends only on prior work plus `EXT-CONV-AI-1`. |
| 6.7 | Major concern | Functional UI story is well specified, but the concrete Conversation-owned UI extension/registration seam is not bound by architecture or an external dependency. |
| 7.1 | Pass | Produces one useful pending-proposal outcome and authorized discovery surfaces without needing later proposal actions. |
| 7.2 | Pass | One immutable edit transition is well bounded and independently testable. |
| 7.3 | Minor concern | Regeneration can operate on the initial generated version, so mandatory dependency on Story 7.2 editing is unnecessary and serializes otherwise independent work. |
| 7.4 | Pass | Correctly makes 7.2/7.3 versions optional and delivers one selected-version approval/posting outcome. |
| 7.5 | Pass | Rejection/abandonment is a cohesive non-posting terminal outcome. |
| 7.6 | Pass | Expiry policy, timer, and transition races form one cohesive deterministic-expiry outcome. |
| 8.1 | Critical violation | Promises content can “expire safely” but only emits an eligible-expiry request for future Story 8.3 processing. The user-visible lifecycle is incomplete until a later story. |
| 8.2 | Pass but blocked | Secure export is cohesive, testable, and correctly blocked by Story 8.1 and `EXT-SECRETS-1`. |
| 8.3 | Major concern | Combines cryptographic erasure/redaction, a large named projection-purge inventory, partial-failure orchestration, restart safety, UI, and forensic proof. |
| 8.4 | Major concern | Combines Content Safety policy, budget policy, retention/export/deletion command semantics, high-water rules, API/UI parity, and shared pending-command behavior. |
| 8.5 | Major concern | Primarily a technical verification milestone spanning every runtime and product metric; formula implementation, ingestion, projection, and live-evidence classification should be separated. |
| 8.6 | Major concern | Cross-cuts every interactive route and state and combines NFR-13 conformance with NFR-14 production-like timing. It is an epic-sized qualification story rather than a single independently deliverable slice. |
| 8.7 | Pass but blocked | Readiness inspection is a coherent operator-facing outcome and correctly refuses to manufacture evidence or execute `RQ-1`. |

### 🔴 Critical Violations

#### EQ-C1 — Story 6.4 depends on later Story 6.5

Story 6.4 says it has no forward dependency, yet its own dependency text states that production call acceptance cannot mint the live admission grant until Story 6.5 supplies the shared allocator. An injected trusted grant proves an isolated contract, not the story's live Provider-invocation outcome.

**Remediation:** Either move shared admission before Provider execution, or narrow Story 6.4 to prepared descriptor plus atomic reservation with no live Provider transport. A subsequent prior-ordered story should combine real admission, Provider invocation, recovery, and reconciliation.

#### EQ-C2 — Story 8.1 depends on later Story 8.3 for safe expiry

Story 8.1 promises sensitive content can expire safely, but its acceptance criteria stop at emitting an eligible-expiry request “for Story 8.3 processing.” The content remains unerased and unpurged until future work.

**Remediation:** Narrow Story 8.1 to durable retention deadlines and legal holds without claiming completed expiry, or move the complete expiry effect into Story 8.1. If deletion/purge remains separate, make the later transition explicit as a separate user outcome rather than completion of Story 8.1.

### 🟠 Major Issues

#### EQ-M1 — Epic 8 is an umbrella rather than one cohesive outcome

Epic 8 combines retention/holds, encrypted export, cryptographic deletion, safety and budget policy authoring, runtime/product metric calculators, full UI conformance/performance qualification, and readiness inspection. These serve different users, change different subsystems, and can be delivered independently.

**Remediation:** Split at least into governance operations, measurement/evidence production, and launch-evidence inspection epics. Keep `RQ-1` outside the backlog as currently specified.

#### EQ-M2 — Several stories are epic-sized

Stories 5.5, 5.6, 6.4, 6.5, 8.3, 8.4, 8.5, and 8.6 each combine multiple independently testable subsystems or qualification lanes. Their acceptance criteria are precise, but precision does not make the scope small enough for one story.

**Remediation examples:**

- Split 5.5 into readiness-record/projection semantics, Provider readiness, and operation-matrix/API/UI parity.
- Split 5.6 into host composition, secrets/access-control integration, and topology qualification.
- Split 6.5 into capacity profile/admission, weighted fairness, and fence/recovery qualification.
- Split 8.3 into protected-payload erasure and projection-purge/recovery confirmation.
- Split 8.4 into safety policy, budget policy, and shared high-impact command behavior.
- Split 8.5 into metric contracts/calculators and live observation ingestion/classification.
- Split 8.6 into NFR-13 conformance and NFR-14 browser-performance evidence.

#### EQ-M3 — Verification commands are not executable yet

All 27 active stories name `pwsh ./eng/verify-story-X.Y.ps1`, but the repository has no `eng/` directory and none of those scripts exists. The commands are therefore prospective labels rather than executable readiness evidence.

**Remediation:** Make creation of a common `eng/` verification harness an explicit Story 5.1 acceptance criterion and require each story to add its script before completion, or replace each manifest entry with an existing executable lane. Do not mark a story `ready-for-dev` on the assumption that the command already works.

#### EQ-M4 — Story 6.7 lacks a bound integration dependency

The story delivers the sole Conversation-owned invocation UI, but neither its dependencies nor architecture defines the concrete Conversations/FrontComposer extension seam required to render the action inside a Source Conversation.

**Remediation:** Add the explicit integration contract/dependency identified as `UX-ALIGN-1` before Story 6.7 becomes ready for development.

### 🟡 Minor Concerns

- Story 7.3 should not require Story 7.2; regeneration is valid from the initial generated version.
- Evidence-manifest `Result` vocabulary is inconsistent: some stories with uncommitted required dependencies say `Not run — backlog`, while others say `Blocked — backlog`. Normalize this for machine consumption.
- `epics.md` contains two stray literal `+` lines at lines 1511 and 1889.
- Historical Stories 1–4 retain superseded criteria, including bounded-context behavior and broader invocation choices. The replacement-authority header contains the conflict, but moving historical stories to a clearly non-executable archive would reduce accidental implementation risk.

### Best-Practice Strengths

- All 27 active stories use clear user or operator personas and explicit outcomes.
- Acceptance criteria consistently use Given/When/Then and cover happy paths, validation, replay/idempotency, failures, authorization, cross-tenant denial, and information disclosure.
- Every active story includes requirements, owned clauses, dependencies, Evidence Level, test/artifact targets, negative evidence, verification command, and current result.
- The dependency graph is acyclic apart from the two semantic forward-completion defects identified above.
- EventStore aggregates/projections and governance models are introduced when first needed; Story 5.1 explicitly forbids pre-creating future entities. There is no up-front database/table anti-pattern.
- Architecture specifies no external starter template, and Story 5.1 correctly uses the approved Structural Seed instead.
- The brownfield correction is recognized: package/boundary compatibility, platform-host integration, migration away from forbidden hosting ownership, and external commitments appear early.
- FR traceability remains complete at 28/28.

### Epic Quality Conclusion

The active backlog is substantially stronger than a typical pre-implementation epic set: it has excellent traceability, explicit negative evidence, and careful fail-closed behavior. It does not yet satisfy strict create-epics-and-stories quality standards because two stories rely on later work to complete their promised outcome, Epic 8 is not cohesive, several stories are too large, the Conversation invocation UI seam is unresolved, and every named story verification command is currently absent.

## Summary and Recommendations

### Overall Readiness Status

# NOT READY

Hexalith Agents is not ready to begin the active forward implementation backlog.

The product definition is strong: the PRD is final and structurally complete, all 28 Functional Requirements have active epic coverage, the 14 Non-Functional Requirements are measurable, and the final UX spines substantially align with the architecture. The blocking problem is execution readiness. Every active story chain begins behind uncommitted external dependencies, two stories violate the no-forward-dependency rule, one required Conversation-owned UI seam is undefined, several stories are too large, and none of the 27 named verification commands is currently executable.

### Readiness Evidence Summary

| Assessment area | Result |
| --- | --- |
| Document discovery | Complete; authoritative PRD, Architecture, Epics, and UX inputs confirmed. |
| PRD completeness | Strong; 28 FRs, 14 NFRs, resolved V1 decision register, explicit evidence and dependency rules. |
| FR traceability | 28/28 covered; 100% documentary coverage with no extra FR identifiers. |
| UX documentation | Complete and final; two integration/semantic alignment issues remain. |
| Architecture support | Conceptually strong; AD-10 and AD-16 explicitly acknowledge current implementation non-conformance. |
| Epic value and traceability | Strong overall; Epics 5–7 are user-value oriented and all active stories carry detailed evidence manifests. |
| Story independence | Failed by Stories 6.4 and 8.1. |
| Story sizing/cohesion | Failed for Epic 8 and several active stories. |
| Executable verification | Failed; 27/27 named `eng/verify-story-*.ps1` commands are absent. |
| External readiness | Failed; all seven critical external dependencies are recorded as `Uncommitted`. |

### Critical Issues Requiring Immediate Action

1. **No executable starting point:** `EXT-HOST-1` is required even for Story 5.1 and is `Uncommitted`; downstream stories inherit that block. The architecture states that all seven critical external dependencies are `Uncommitted`.
2. **Story 6.4 has a forbidden forward dependency:** it claims live Provider invocation but cannot mint the required admission grant until Story 6.5.
3. **Story 8.1 has a forbidden forward dependency:** it claims safe retention expiry but delegates actual expiry processing to Story 8.3.
4. **The sole V1 invocation surface is not architecturally bound:** **Call hexa** must live inside a Source Conversation, but no concrete Conversations/FrontComposer UI extension contract or tracked dependency owns that integration.
5. **Current topology violates the final architecture:** module-owned AppHost, Aspire, and ServiceDefaults projects still exist even though AD-16 assigns hosting to the platform.
6. **Current Provider capability handling violates AD-10:** durable high-water and effective-version behavior is explicitly missing from context, generation, and regeneration paths.
7. **Verification is prospective, not executable:** all 27 story manifests reference nonexistent scripts under `eng/`.

### Recommended Next Steps

1. **Repair the backlog dependency graph.** Remove Story 6.4's dependency on future live admission and Story 8.1's dependency on future expiry processing. Reorder, narrow, or split the affected stories so every promised outcome completes using prior work only.
2. **Decompose Epic 8 and oversized stories.** Separate governance operations, measurement/evidence production, and launch-evidence inspection. Split Stories 5.5, 5.6, 6.4, 6.5, 8.3, 8.4, 8.5, and 8.6 into independently demonstrable slices.
3. **Bind the Conversation invocation integration.** Define the concrete Conversations/FrontComposer extension point for **Call hexa**, identify its owner and repository, and add a committed compatibility contract and verification command before Story 6.7 can become ready.
4. **Resolve owner-versus-Facilitator semantics.** Either make Facilitator-derived authority normative in PRD/UX/public contracts and label it accurately, or obtain a real Conversations owner resolver.
5. **Commit the external dependency register.** For each of `EXT-CONV-AI-1`, `EXT-HOST-1`, `EXT-PROVIDER-1`, `EXT-SAFETY-1`, `EXT-TOKEN-1`, `EXT-SECRETS-1`, and `EXT-TOPOLOGY-1`, record the owner, repository, immutable target, integration date, executable compatibility command, Evidence Level, accepted status, and consuming stories. Do not promote a consuming story while its required record is `Uncommitted`.
6. **Create executable verification infrastructure.** Add an owned `eng/` harness in the structural baseline and make each story create or update its declared `verify-story-X.Y.ps1` command. Run the exact commands before changing story status.
7. **Reconcile current implementation with the final architecture.** Execute the Story 5.1/5.6 hosting-boundary correction and implement AD-10 high-water/effective-version behavior under appropriately decomposed stories.
8. **Clean machine-readable backlog hygiene.** Normalize `Blocked`/`Not run` result vocabulary, remove stray `+` lines, remove the unnecessary 7.3→7.2 dependency, and move superseded historical criteria to an unmistakably non-executable archive.
9. **Rerun implementation readiness.** Require zero forward dependencies, an explicit invocation UI seam, executable verification commands, and at least the external commitments needed for the first development slice before returning a ready-for-development decision.

### Final Note

This assessment identified **14 issue groups across five categories**: external dependency commitments, UX/architecture contracts, epic/story structure, executable verification, and artifact/current-implementation conformance. Seven are immediate implementation blockers. The planning set should be remediated before Phase 4 implementation begins; proceeding as-is would start work with no unblocked first story and would defer known integration and validation failures into implementation.

**Assessment date:** 2026-08-03  
**Assessor:** Codex — Implementation Readiness Product/Requirements Review
