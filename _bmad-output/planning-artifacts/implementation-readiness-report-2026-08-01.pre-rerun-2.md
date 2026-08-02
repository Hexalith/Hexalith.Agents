---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
status: NOT_READY
assessedOn: 2026-08-01
assessor: Codex
issueEntries: 29
filesIncluded:
  prd:
    - prds/prd-agents-2026-06-23/prd.md
    - prds/prd-agents-2026-06-23/addendum.md
    - prds/prd-agents-2026-06-23/reconcile-brief.md
  architecture:
    - architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
    - architecture/architecture-agents-2026-06-23-2/IMPLEMENTATION-CONVENTIONS.md
  epics:
    - epics.md
  ux:
    - ux-designs/ux-agents-2026-06-23/DESIGN.md
    - ux-designs/ux-agents-2026-06-23/EXPERIENCE.md
---

# Implementation Readiness Assessment Report

**Date:** 2026-08-01
**Project:** agents

## Document Inventory

### PRD

Primary document set: `prds/prd-agents-2026-06-23/`

- `prd.md` — 48,765 bytes; modified 2026-08-01
- `addendum.md` — 1,912 bytes; modified 2026-06-23
- `reconcile-brief.md` — 1,710 bytes; modified 2026-06-23

Supporting files found but excluded from the canonical assessment input:

- `review-rubric.md` — 4,938 bytes; modified 2026-06-23
- `validation-report.md` — 1,944 bytes; modified 2026-06-23

No top-level whole document or indexed shard set was found.

### Architecture

Primary document set: `architecture/architecture-agents-2026-06-23-2/`

- `ARCHITECTURE-SPINE.md` — 34,152 bytes; modified 2026-08-01
- `IMPLEMENTATION-CONVENTIONS.md` — 6,176 bytes; modified 2026-07-31

Seven review documents under `reviews/` were found but excluded from the canonical assessment input. No top-level whole document or indexed shard set was found.

### Epics and Stories

- `epics.md` — 108,178 bytes; modified 2026-08-01

No sharded version was found.

### UX Design

Primary document set: `ux-designs/ux-agents-2026-06-23/`

- `DESIGN.md` — 18,908 bytes; modified 2026-08-01
- `EXPERIENCE.md` — 28,451 bytes; modified 2026-08-01

Five Markdown review and validation documents were found but excluded from the canonical assessment input. No top-level whole document or indexed shard set was found.

### Discovery Resolution

- No whole-versus-sharded duplicates were found.
- No required document category is missing.
- The versioned, non-indexed document bundles and canonical files listed above were confirmed for assessment.
- The previous same-day readiness report was preserved as `implementation-readiness-report-2026-08-01.pre-rerun.md` before this report was initialized.

## PRD Analysis

### Functional Requirements

#### FR-1: Configure `hexa`

Agent Administrators can create or enable `hexa` with a stable Agent identity, display name, description, Agent Instructions, lifecycle state, and tenant scope.

Testable consequences:

- The system prevents activation when required Agent fields are missing or invalid.
- The system exposes the current Agent configuration through the admin UI and API/client contracts.
- The system records configuration changes in Audit Evidence with actor, timestamp, prior value where safe to expose, and new value.

#### FR-2: Link Agent To Party Identity

Agent Administrators can provision or link the Agent's Party identity so `hexa` appears as a known AI participant when it posts to a Conversation.

Testable consequences:

- An active Agent has exactly one Party identity.
- The system rejects posting an Agent Response when the Agent Party identity is missing, disabled, ambiguous, or unauthorized for the Source Conversation.
- Conversation Messages posted by `hexa` are attributable to the Agent's Party identity, not to the caller or a generic system account.

#### FR-3: Manage Agent Lifecycle

Agent Administrators can activate, disable, and inspect `hexa` lifecycle state.

Testable consequences:

- Disabled Agents cannot be called from Conversations.
- Disabling an Agent does not delete existing Audit Evidence, Proposed Agent Replies, or Conversation Messages.
- Lifecycle changes are auditable and visible through admin UI and API/client contracts.

#### FR-4: Manage Global Providers Aggregate

Authorized administrators can configure the Global Providers Aggregate with provider records, model options, enabled/disabled state, and provider capability metadata needed for Agent selection.

Testable consequences:

- Disabled providers or models cannot be selected for new Agent configuration.
- Existing Agents using a disabled provider/model cannot be activated or called until reconfigured, unless a documented migration state allows temporary read-only inspection.
- Provider configuration changes are auditable without exposing secrets in logs, API responses, UI, or Audit Evidence.

#### FR-5: Select Provider And Model Per Agent

Agent Administrators can select a Provider and model for `hexa` from the Global Providers Aggregate.

Testable consequences:

- The system validates that the selected Provider and model are enabled and usable before Agent activation.
- The system stores enough provider/model identity in Audit Evidence to explain which Provider and model produced each generated version.
- Changing provider/model selection affects future Agent Calls only and does not rewrite historical proposal or response evidence.

#### FR-6: Configure Response Mode

Agent Administrators can configure `hexa` for Automatic Response Mode or Confirmation Response Mode.

Testable consequences:

- Automatic Response Mode posts successful Agent Responses directly to the Source Conversation after authorization and generation complete.
- Confirmation Response Mode creates Proposed Agent Replies outside the Conversation and never posts unapproved generated content.
- Response mode changes affect future Agent Calls only.

#### FR-7: Configure Approver Policy

Agent Administrators can define all Approvers through the Agent's Approver Policy, including policy sources such as the Conversation owner, the caller, predefined Parties, or tenant roles.

Testable consequences:

- The system authorizes proposal edit, regeneration, approval, rejection, abandonment, and expiry-resolution actions using the Approver Policy.
- The system rejects approval actions by Parties not authorized by the current policy for the proposal.
- The system exposes which configured policy source authorized the Approver according to a defined disclosure category: user-visible, operator-only, redacted, or omitted.
- The proposal records the policy basis used for each approval-related decision.
- API/client contracts and admin UI use the same disclosure category for the same approval-policy basis.

#### FR-8: Call Agent From Conversation

Authorized Conversation Participants can explicitly call `hexa` from a Source Conversation with a user prompt or request.

Testable consequences:

- Agent Calls require Source Conversation access and Agent call permission.
- Unauthorized calls fail before Provider invocation.
- Every Agent Call records caller, Agent, Source Conversation, request timestamp, and response mode.

#### FR-9: Build V1 Conversation Context

The system supplies the Agent with Conversation Context according to the configured Conversation Context Policy.

Testable consequences:

- V1 Agent generation uses Conversation Context only.
- V1 generation does not include long-term memory, project content, folder content, external tool output, or external-channel content.
- When the full Source Conversation fits the selected Provider/model's safe context budget, V1 generation uses the full Source Conversation.
- When the Source Conversation exceeds the selected Provider/model's safe context budget, the call fails closed before Provider invocation; V1 never truncates, summarizes, windows, or otherwise reduces the Conversation.
- Agent Calls record that complete context was used or that the call was blocked, the Conversation Context Policy version or equivalent identifier, and enough context metadata for audit without leaking unrelated tenant data.
- If the complete Conversation Context cannot be loaded or sent safely, the Agent Call fails closed and creates neither Provider work, a Proposed Agent Reply, nor a Conversation Message.

#### FR-10: Handle Generation Failure

The system handles Provider failures, timeout, disabled provider/model state, invalid context, Content Safety Policy failures, and policy failures without posting incomplete or unsafe Agent Responses.

Testable consequences:

- Failed generation creates status and Audit Evidence visible to authorized administrators or callers.
- Failed generation does not create a Conversation Message.
- Failed generation in Confirmation Response Mode does not create an approvable Proposed Agent Reply unless generated content exists and is explicitly marked as failed or incomplete for audit only.

#### FR-11: Post Automatic Response

When `hexa` is configured for Automatic Response Mode, the system posts successful generated content to the Source Conversation as a Conversation Message attributed to the Agent's Party identity.

Testable consequences:

- The posted message references the Agent Call or equivalent trace identifier.
- The posted message does not appear as authored by the caller.
- The system records Audit Evidence linking caller, Agent, Provider/model, Source Conversation, generated content, and posted Conversation Message.

#### FR-12: Prevent Automatic Posting When Policy Fails

The system prevents automatic posting when authorization, Agent lifecycle, Provider/model, Party identity, Source Conversation access, Conversation Context Policy, Content Safety Policy, or generation status is invalid.

Testable consequences:

- No Conversation Message is created when a required policy check fails.
- No Conversation Message is created when generated content fails the active Content Safety Policy.
- The failure reason is visible through authorized status surfaces without leaking secrets or unrelated tenant data.
- Audit Evidence distinguishes authorization, context policy, content safety, Provider/runtime, and posting failures.

#### FR-13: Create Proposed Agent Reply

When `hexa` is configured for Confirmation Response Mode, successful generation creates a Proposed Agent Reply linked to the Source Conversation and Agent Call.

Testable consequences:

- A Proposed Agent Reply is not a Conversation Message.
- A Proposed Agent Reply records caller, Agent, Source Conversation, generated version, Provider/model, response mode, and current proposal state.
- Authorized Approvers can discover pending proposals requiring their action.
- V1 uses in-product pending-proposal visibility only for authorized Approvers, including pending count, queue, and Conversation status entry.
- Email, push, and external-channel proposal notifications are not included in V1.

#### FR-14: Preserve All Proposal Versions

The system preserves every generated, edited, and regenerated content version for each Proposed Agent Reply.

Testable consequences:

- Editing a proposal creates a new Versioned Proposal Content record or equivalent immutable version entry.
- Regeneration creates a new generated version without deleting prior generated or edited versions.
- Approval identifies exactly which version was approved and posted.

#### FR-15: Edit Proposed Reply

Authorized Approvers can edit Proposed Agent Reply content before approval.

Testable consequences:

- Only authorized Approvers can edit proposal content.
- Edits preserve the prior version and author of the edit.
- Edited content remains outside the Conversation until approved.

#### FR-16: Regenerate Proposed Reply

Authorized Approvers can request regeneration of a Proposed Agent Reply before approval.

Testable consequences:

- Regeneration uses the same Source Conversation and Agent configuration unless the system records an explicit configuration version change.
- Regeneration preserves prior versions and creates a new generated version.
- Regeneration is blocked after a proposal reaches a terminal state.

#### FR-17: Approve Proposed Reply

Authorized Approvers can approve a selected proposal version, causing it to be posted to the Source Conversation as `hexa`.

Testable consequences:

- Approval posts exactly the approved version and no other proposal version.
- The Conversation Message is attributed to the Agent's Party identity.
- Audit Evidence links the approved version, Approver, approval timestamp, and posted Conversation Message.

#### FR-18: Reject, Abandon, Or Expire Proposed Reply

Authorized Approvers or system policy can move a Proposed Agent Reply to rejected, abandoned, or expired terminal states.

Testable consequences:

- Terminal proposals cannot be approved or posted.
- Terminal proposals preserve all generated and edited versions for audit.
- The default expiry duration is 24 hours. An Agent Administrator may configure a duration from 1 hour through 30 days for future proposals only.
- A durable Dapr Workflow timer moves a non-terminal proposal to `Expired` at or after its stored `ExpiresAt`; an expiry policy change never changes an existing proposal's `ExpiresAt`.
- Expiry behavior and the stored `ExpiresAt` are visible through admin UI and API/client contracts.

#### FR-19: Enforce Tenant Isolation

The system enforces tenant isolation across Agent configuration, Provider selection, Agent Calls, Conversation Context, Proposed Agent Replies, Conversation posting, and Audit Evidence.

Testable consequences:

- A Party from one tenant cannot call, inspect, approve, or post Agent responses for another tenant.
- Provider/model configuration and Agent configuration cannot leak across tenant boundaries unless explicitly platform-scoped and authorized.
- Audit/status queries return only tenant-authorized records.

#### FR-20: Enforce Role And Policy Authorization

The system enforces authorization for Agent administration, provider administration, Agent calling, proposal discovery, editing, regeneration, approval, rejection, abandonment, posting, and audit inspection.

Testable consequences:

- Authorization failures occur before Provider invocation or Conversation posting.
- The same authorization rules apply through admin UI and API/client contracts.
- Authorization decisions are auditable at a level sufficient to explain denial or approval without leaking sensitive content.

#### FR-21: Fail Closed On Dependency Uncertainty

The system fails closed when required Party, Conversation, Provider, Agent, tenant access, or approval policy state is missing, stale, ambiguous, disabled, or unavailable.

Testable consequences:

- Missing or stale Conversation access prevents Agent Calls and approval posting.
- Missing or disabled Agent Party identity prevents posting.
- Missing Provider/model state prevents generation.

#### FR-22: Provide Admin UI

The admin UI allows authorized administrators to manage Global Providers Aggregate entries, configure `hexa`, inspect lifecycle state, configure response and approver policy, and view Agent operation/proposal status.

Testable consequences:

- Admin UI actions enforce the same authorization rules as API/client contracts.
- Admin UI never exposes Provider secrets.
- Admin UI clearly distinguishes active, disabled, invalid, pending proposal, failed call, and expired proposal states.

#### FR-23: Provide API And Client Contracts

The system exposes stable API/client contracts for Provider administration, Agent configuration, Agent Calls, proposal workflow, status inspection, and audit inspection.

Testable consequences:

- API/client contracts do not require callers to use raw EventStore, internal aggregate, internal projection, or provider SDK details.
- API/client contracts return structured success and error results suitable for automation.
- Breaking contract changes are avoided during V1 unless explicitly versioned.

#### FR-24: Capture Agent Audit Evidence

The system captures Audit Evidence for Agent configuration, Provider/model configuration, Agent Calls, generation attempts, proposal versions, edits, regenerations, approvals, rejections, abandonments, expirations, automatic posts, and final Conversation Messages.

Testable consequences:

- Every posted Agent Response can be traced back to caller, Agent, Source Conversation, Provider/model, generated content, and approval path where applicable.
- Every Proposed Agent Reply preserves all Versioned Proposal Content.
- Audit Evidence records the Content Safety Policy decision, Conversation Context Policy behavior, and policy/version identifiers where available.
- Audit Evidence is queryable by authorized users without exposing unrelated tenant data or Provider secrets.

#### FR-25: Expose Operational Status

The system exposes status for Agent readiness, Provider/model readiness, recent Agent Call outcomes, proposal queues, generation failures, approval completion, and posting outcomes.

Testable consequences:

- Authorized administrators can identify whether `hexa` is callable for a tenant.
- Authorized administrators can distinguish configuration errors, authorization failures, context policy failures, content safety failures, Provider failures, generation failures, pending approvals, and posting failures.
- Status surfaces support launch monitoring of adoption and approval workflow metrics.

#### FR-26: Configure Content Safety And Prompt Policy

Authorized administrators or release operators can define the active Content Safety Policy for `hexa`.

Testable consequences:

- `hexa` cannot be enabled for production or production-like launch validation without an active Content Safety Policy.
- The Content Safety Policy defines prompt constraints, blocked or restricted output categories, safety failure handling, and audit treatment.
- Content Safety Policy changes are auditable and affect future Agent Calls only.
- Automatic Response Mode and Confirmation Response Mode use the same active Content Safety Policy unless a stricter mode-specific policy is configured.
- The active policy always blocks child sexual abuse/exploitation; credible threats or instructions for imminent serious harm; encouragement or instruction for suicide/self-harm; credential theft, malware deployment, or unauthorized compromise; secrets/tokens/private credentials; cross-tenant or unauthorized personal/Conversation data; and attempts to bypass tenant, authorization, audit, retention, or safety controls.
- Restricted hate, harassment, sexual, violent, illegal-activity, or sensitive-personal content requires an explicitly permitted tenant use case and Confirmation Response Mode.
- A policy version cannot weaken a retry already in progress; each retry uses policy at least as restrictive as the initial attempt.

#### FR-27: Enforce Safety Before Provider And Conversation Side Effects

The system applies Content Safety Policy to the prompt and complete authorized Conversation Context before Provider invocation, then applies it to generated output before that output becomes a Conversation Message or an approvable Proposed Agent Reply.

Testable consequences:

- Generated content that fails Content Safety Policy cannot be posted automatically.
- Generated content that fails Content Safety Policy cannot become an approvable Proposed Agent Reply.
- Safety failures create authorized status and Audit Evidence without exposing unsafe content in surfaces where policy forbids display.
- Approvers cannot override a Content Safety Policy failure.
- Prompt and complete Conversation Context pass the active policy before Provider invocation, and generated output passes the active policy before any proposal or Conversation side effect.

#### FR-28: Define Launch Readiness Controls

V1 launch readiness requires the fixed metric thresholds, latency targets, full-context behavior, cost controls, audit governance, and evidence levels in the V1 Decision Register.

Testable consequences:

- Production or production-like generation cannot be enabled until Content Safety Policy, Conversation Context Policy, cost controls, audit governance, launch metrics, latency targets, and live conformance evidence are active and recorded.
- Per-tenant monthly and per-call cost caps are hard enforcement boundaries: 80% emits a warning, 100% fails closed, and atomic reservation plus reconciliation prevents concurrent overspend. The system reserves maximum estimated attempt cost before Provider invocation, reconciles actual usage, releases unused reservation only after a confirmed no-usage failure, and reuses the same reservation for eligible retries. Missing pricing or budget state blocks invocation; reporting-only monitoring is insufficient.
- Automatic accepted-call-to-post latency is p95 at most 60 seconds and p99 at most 120 seconds. Confirmation accepted-call-to-proposal latency uses the same thresholds; approval-to-post latency is p95 at most 10 seconds and p99 at most 30 seconds.
- Pre-Provider authorization, policy, budget, and context rejections complete at p95 at most 2 seconds. Each performance gate uses at least 30 production-like executions.
- Production readiness requires live evidence levels 4 and 5 as defined by the V1 Decision Register; lower-level structural and fake-runtime evidence cannot independently establish launch readiness.

**Total FRs: 28**

### Non-Functional Requirements

#### NFR-1: Security

Agent configuration, Provider administration, Agent Calls, proposal actions, posting, and audit inspection must enforce tenant and Party authorization before side effects.

#### NFR-2: Privacy

Conversation Context, proposal content, and Audit Evidence must not leak across tenants or unauthorized Parties.

#### NFR-3: Reliability

Agent Calls must never create partial Conversation Messages on failure. Confirmation workflows must not lose generated or edited proposal versions.

#### NFR-4: Observability

The system must expose enough status to debug configuration errors, Provider failures, authorization denials, pending approval bottlenecks, and posting failures.

#### NFR-5: Auditability

Audit Evidence must preserve all generated and edited proposal versions and link final posted responses to their source call and approval path.

#### NFR-6: Provider Safety

Provider secrets must be write-only or secret-backed where applicable and must never appear in logs, status payloads, audit records, or UI display.

#### NFR-7: Content Safety

Agent generation must be governed by an active Content Safety Policy before generated content can create Conversation side effects.

#### NFR-8: Context Bounds

Conversation Context must not be truncated, summarized, windowed, or otherwise reduced; oversized Conversations fail closed before Provider invocation.

#### NFR-9: Performance

Automatic accepted-call-to-post latency is p95 ≤ 60 seconds and p99 ≤ 120 seconds; confirmation accepted-call-to-proposal latency is p95 ≤ 60 seconds and p99 ≤ 120 seconds; approval-to-post latency is p95 ≤ 10 seconds and p99 ≤ 30 seconds; fast pre-Provider rejection latency is p95 ≤ 2 seconds. Each gate requires at least 30 production-like executions.

#### NFR-10: Cost Control

Hard per-tenant monthly and per-call caps warn at 80%, fail closed at 100%, and use atomic reservation plus reconciliation. Reporting-only controls do not satisfy launch readiness.

**Total NFRs: 10**

### Additional Requirements

#### MVP scope constraints

The following capabilities are explicitly in scope:

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

The following constraints and non-goals are explicit:

- No long-term Agent memory in V1; Hexalith.Memories integration is deferred to V2.
- No Agent tools or business actions beyond adding approved or automatic replies to Conversations.
- No retrieval from project, folder, file, external knowledge-base, or external-channel content.
- No automatic activation from Conversation changes and no project- or folder-triggered activation.
- No agent-to-agent orchestration.
- No Slack, Teams, email, SMS, or other external-channel bot/bridge integration.
- No unapproved generated content may become a Conversation Message.
- Editing or regeneration must not rewrite or delete historical generated versions.
- Provider secrets must not appear through UI, API/client contracts, logs, or Audit Evidence.
- Conversation Context must never be silently truncated to fit a Provider/model budget.
- V1 product behavior exposes only `hexa`, although generalized data structures may avoid blocking future Agents.
- Fine-grained launch pricing, billing, and monetization are out of scope.

#### Integration requirements and dependencies

- **Hexalith.Conversations:** Source Conversation access, complete Conversation Context loading, AI membership, and final Conversation Message posting depend on Conversations. External prerequisite `CONV-AI-1` publishes `IConversationClient.AddParticipantAsync` and `POST /api/v1/conversations/{conversationId}/participants`, limited for Agents to stable `ParticipantType.AiAgent`/`AIAgent` plus `ParticipantRole.Member` membership with deterministic idempotency, typed conflicts, and cross-tenant denial. Hexalith Agents must not treat unapproved proposals as Conversation Messages or write Conversation streams directly.
- **Hexalith.Parties:** Agent identity and Conversation Participant identity depend on Parties. `hexa` must post as a Party identity.
- **Provider Infrastructure:** Provider/model availability depends on the Global Providers Aggregate and the underlying provider integration selected per Agent.
- **Tenant Access:** Tenant isolation and authorization must align with existing Hexalith tenant access patterns and fail closed when tenant state is missing or unavailable.
- **Admin Surface:** The admin UI must use the same capability and authorization model as the API/client contracts.
- **Release Governance:** Production or production-like generation depends on recorded launch-readiness gates for safety, context, metrics, latency, cost, and audit governance.

#### Data governance and audit requirements

- Audit Evidence must be tenant-scoped and accessible only to authorized Parties or operators.
- Audit Evidence must preserve every generated, edited, and regenerated proposal version.
- Audit Evidence must link automatic posts and approved posts to the Agent Call, Source Conversation, caller, Agent, Provider/model, response mode, and final Conversation Message.
- Editing a Proposed Agent Reply must never overwrite the prior version.
- Regenerating a Proposed Agent Reply must never delete prior generated or edited versions.
- Rejected, abandoned, and expired proposals remain audit records and cannot later be posted.
- Provider secrets and raw credentials are never audit content.
- Sensitive Agent content is retained for 365 days after terminal interaction unless legal hold suspends expiry. Authorized export is tenant-scoped, encrypted, time-limited, manifested, and audited.
- EventStore history is never rewritten. Retention expiry or approved deletion cryptographically erases or redacts protected sensitive payloads and purges affected projections while retaining only a support-safe non-content tombstone; completion requires restrictive confirmation from payload protection and every affected projection.
- Posted Conversation Messages remain governed by Hexalith.Conversations retention.

#### Public API and client surface requirements

V1 public API/client contracts must cover:

- Provider administration: create, update, list, enable, and disable Provider and model options where authorized.
- Agent administration: configure `hexa`, lifecycle, Party identity link, Agent Instructions, Provider/model selection, Response Policy, Approver Policy, Conversation Context Policy, and Content Safety Policy.
- Agent invocation from a Source Conversation.
- Proposal workflow: list pending proposals, inspect proposal versions, edit, regenerate, approve, reject, abandon, and inspect expiry.
- Status inspection for Agent readiness, Provider readiness, context policy outcome, content safety outcome, Agent Call status, proposal state, and posting outcome.
- Authorized Audit Evidence inspection for Agent Calls, proposal lifecycle, and posted responses.

The public surface must not require consumers to understand internal EventStore stream names, aggregate mechanics, projection internals, or provider SDK details.

#### Launch success and counter-metrics

- **SM-1: Active tenant adoption.** At least one launch tenant configures `hexa`, enables a Provider/model, satisfies launch-readiness gates, and records successful Agent Calls in production or production-like launch validation. This validates FR-1 through FR-12 and FR-22 through FR-28.
- **SM-2: Conversation adoption.** At least 20% of eligible Conversations in the enabled launch cohort record one or more accepted Agent Calls during a rolling 30-day window, measured only when the cohort contains at least 50 eligible Conversations. This validates FR-8, FR-9, FR-11, FR-13, and FR-28.
- **SM-3: Approval workflow completion.** At least 95% of Proposed Agent Replies created during a rolling 30-day window reach a terminal state within 26 hours. Within the cohort, expiry is at most 20%, posting failure is at most 2%, at least 70% are resolved by a human decision (`Approved`, `Rejected`, or `Abandoned`), and 100% have complete Audit Evidence. This validates FR-13 through FR-18, FR-24, and FR-28.
- **SM-4: Unauthorized action prevention.** Authorization tests and launch telemetry show zero successful cross-tenant or unauthorized Agent Calls, proposal actions, or audit inspections. This validates FR-19 through FR-21.
- **SM-5: Audit completeness.** Every posted Agent Response has complete Audit Evidence linking caller, Agent, Source Conversation, Provider/model, proposal path when applicable, and final Conversation Message. This validates FR-11, FR-17, and FR-24.
- **SM-6: Admin/API parity.** Core administration and workflow operations are available through both admin UI and API/client contracts with the same authorization outcomes. This validates FR-22 and FR-23.
- **SM-C1:** Automatic post volume must not be maximized at the expense of confirmation mode in safety-sensitive contexts.
- **SM-C2:** Approval completion time must not be optimized by dropping version preservation or approval evidence.
- **SM-C3:** Provider/model breadth must not be optimized before Provider governance, secret safety, and per-Agent selection are robust.

#### Binding V1 Decision Register

- **OQ-1:** The sole V1 invocation entry is a Conversation-owned **Call hexa** action. No mention, command, ambient trigger, or alternate entry point is in V1. Owner: Product + UX. Resolved 2026-08-01.
- **OQ-2:** Hexalith Agents owns durable proposal state and all proposal read models. Dapr Workflow owns execution only and does not become a domain system of record. Owner: Architecture. Resolved 2026-08-01.
- **OQ-3:** Proposal expiry defaults to 24 hours and is configurable per Agent from 1 hour through 30 days for future proposals only. Dapr Workflow expires a proposal at or after its stored `ExpiresAt`. Owner: Product + Architecture. Resolved 2026-08-01.
- **OQ-4:** V1 uses in-product proposal notifications and pending-count surfaces only. Email, push, and external-channel notification delivery are out of scope. Owner: Product + UX. Resolved 2026-08-01.
- **OQ-5:** Automatic accepted-call-to-post and confirmation accepted-call-to-proposal are p95 ≤ 60 seconds and p99 ≤ 120 seconds. Approval-to-post is p95 ≤ 10 seconds and p99 ≤ 30 seconds. Fast pre-Provider rejection is p95 ≤ 2 seconds. Each gate uses at least 30 production-like executions. Owner: Architecture + Release PM. Resolved 2026-08-01.
- **OQ-6:** V1 enforces hard per-tenant monthly and per-call caps, warns at 80%, and fails closed at 100%. It atomically reserves maximum estimated attempt cost before Provider invocation, reconciles actual usage, releases unused reservation only after confirmed no usage, and reuses the reservation for eligible retries. Missing pricing/budget state blocks invocation; reporting-only monitoring is insufficient. Owner: Product + Architecture. Resolved 2026-08-01.
- **OQ-7:** The Agents-owned Global Providers Aggregate records provider/model identifiers, enablement, capabilities and limits, secret references, versioned pricing metadata, and a `CapabilityVersion`. Owner: Architecture. Resolved 2026-08-01.
- **OQ-8:** Sensitive Agent content is retained for 365 days after terminal state unless legal hold applies. Authorized export is encrypted and time-limited. Deletion cryptographically erases or redacts sensitive content and purges projections while immutable EventStore history retains only a safe tombstone. Owner: Product + Governance. Resolved 2026-08-01.
- **OQ-9:** Prompt and complete Conversation Context are checked before Provider invocation; output is checked before proposal or Conversation side effects. Always blocked: child sexual abuse/exploitation; credible imminent serious-harm threats/instructions; encouragement/instruction for suicide/self-harm; credential theft, malware, or unauthorized compromise; secrets/private credentials; cross-tenant or unauthorized personal/Conversation data; and control-bypass attempts. Restricted hate, harassment, sexual, violent, illegal-activity, or sensitive-personal content requires an explicitly permitted tenant use case and Confirmation Response Mode. Approvers cannot override failures and retries cannot use a weaker policy. Owner: Product + Security. Resolved 2026-08-01.
- **OQ-10:** V1 uses the complete Source Conversation or fails closed before Provider invocation. Truncation, summarization, windowing, and other bounded-context modes are prohibited. Owner: Architecture. Resolved 2026-08-01.
- **OQ-11:** SM-2 is at least 20% adoption over a rolling 30 days with at least 50 eligible Conversations. SM-3 is at least 95% terminal within 26 hours, with expiry at most 20%, posting failures at most 2%, human resolution at least 70%, and audit completeness equal to 100%. Owner: Product + Release PM. Resolved 2026-08-01.

#### Residual assumptions

- V1 Conversation Context excludes long-term memory, project content, folder content, external tools, and non-conversation retrieval.
- V1 product behavior exposes only `hexa`, even if implementation uses generalized Agent structures.

#### Reconciliation companion status

The three gaps recorded in `reconcile-brief.md` are addressed in the final, 2026-08-01 PRD:

- FR-7 preserves the concrete approver-policy sources: Conversation owner, caller, predefined Parties, and tenant roles.
- OQ-2 assigns durable proposal state/read models to Hexalith Agents and execution to Dapr Workflow.
- OQ-1 defines the sole **Call hexa** invocation entry, while the Conversations integration requirement retains AI membership through external prerequisite `CONV-AI-1`.

The `addendum.md` research notes are contextual and do not override the PRD.

### PRD Completeness Assessment

The PRD is structurally strong and unusually testable: it has 28 stable functional requirement IDs, explicit consequences, 10 cross-cutting NFR categories, quantitative launch and performance gates, resolved implementation-blocking decisions, explicit non-goals, binding governance rules, and named external dependencies. The brief-reconciliation gaps have been incorporated into the final PRD.

Initial PRD-level gaps or risks to carry into readiness validation:

- **External prerequisite:** `CONV-AI-1` is required for AI participant membership and is outside this module's direct delivery boundary. Its availability and version compatibility must be evidenced before implementation slices that post Agent responses can be considered ready.
- **Residual assumptions:** the Conversation Context exclusions and single-exposed-Agent behavior are explicitly recorded but remain assumptions rather than acceptance-tested decisions.
- **Availability and recovery:** the NFRs do not define service availability, recovery-time objective, recovery-point objective, or disaster-recovery expectations.
- **Scalability and concurrency:** no explicit throughput, concurrent-call, queue-depth, or tenant-growth thresholds are defined beyond cost atomicity and latency samples.
- **Accessibility and localization:** the PRD requires an admin UI but does not state an accessibility conformance target or localization requirements; these may exist in UX artifacts and require downstream alignment.
- **Contract evolution precision:** FR-23 says breaking changes are avoided unless explicitly versioned but does not define the versioning or compatibility policy.
- **Live-evidence terminology:** FR-28 requires evidence levels 4 and 5 “as defined by the V1 Decision Register,” but the visible Decision Register rows do not define the evidence-level taxonomy. The authoritative definition must be traceable elsewhere before the launch gate can be implemented and audited consistently.

## Epic Coverage Validation

### Epic FR Coverage Extracted

- **Epic 1 — Tenant Agent Setup And Governance:** FR-1, FR-2, FR-3, FR-4, FR-5, FR-6, FR-7, FR-26.
- **Epic 2 — Safe Conversation Invocation And Automatic Replies:** FR-8, FR-9, FR-10, FR-11, FR-12, FR-19, FR-20, FR-21, FR-27.
- **Epic 3 — Proposal Review And Approval Workflow:** FR-13, FR-14, FR-15, FR-16, FR-17, FR-18.
- **Epic 4 — Operational Visibility, Audit, Integration, And Launch Readiness:** FR-22, FR-23, FR-24, FR-25, FR-28.
- **Epic 5 — Production Binding And Live Conformance:** explicitly reconciles FR-1 through FR-28 and supplies the only authoritative forward implementation plan. Story 5.18 supplies final live cross-system verification for the complete set.

**Total distinct PRD FRs claimed by the epics: 28.**

### Coverage Matrix

| FR | PRD requirement | Primary epic coverage | Authoritative Epic 5 implementation/conformance path | Status |
| --- | --- | --- | --- | --- |
| FR-1 | Configure `hexa` | Epic 1 | Stories 5.2, 5.3, 5.17, 5.18 | ✓ Covered |
| FR-2 | Link Agent to exactly one Party identity | Epic 1 | Stories 5.4, 5.11, 5.18 | ✓ Covered |
| FR-3 | Manage Agent lifecycle while preserving evidence | Epic 1 | Stories 5.2, 5.3, 5.17, 5.18 | ✓ Covered |
| FR-4 | Manage the Global Providers Aggregate | Epic 1 | Stories 5.3, 5.6, 5.10, 5.17, 5.18 | ✓ Covered |
| FR-5 | Select and validate Provider/model per Agent | Epic 1 | Stories 5.3, 5.6, 5.10, 5.17, 5.18 | ✓ Covered |
| FR-6 | Configure automatic or confirmation response mode | Epic 1 | Stories 5.3, 5.18 | ✓ Covered |
| FR-7 | Configure Approver Policy and auditable policy basis | Epic 1 | Stories 5.4, 5.18 | ✓ Covered |
| FR-8 | Explicitly call `hexa` from a Source Conversation | Epic 2 | Stories 5.4, 5.5, 5.7, 5.8, 5.12, 5.18 | ✓ Covered |
| FR-9 | Use complete Conversation Context or fail closed | Epic 2 | Stories 5.6, 5.7, 5.8, 5.18 | ✓ Covered |
| FR-10 | Handle generation and policy failures safely | Epic 2 | Stories 5.5–5.10, 5.12, 5.18 | ✓ Covered |
| FR-11 | Post successful automatic response as the Agent Party | Epic 2 | Stories 5.11, 5.12, 5.18 | ✓ Covered |
| FR-12 | Prevent automatic posting when any required gate fails | Epic 2 | Stories 5.9, 5.11, 5.12, 5.18 | ✓ Covered |
| FR-13 | Create a Proposed Agent Reply in confirmation mode | Epic 3 | Stories 5.5, 5.7, 5.12–5.14, 5.18 | ✓ Covered |
| FR-14 | Preserve every proposal content version | Epic 3 | Stories 5.5, 5.7, 5.14, 5.18 | ✓ Covered |
| FR-15 | Edit a proposed reply while preserving prior versions | Epic 3 | Stories 5.5, 5.7, 5.14, 5.18 | ✓ Covered |
| FR-16 | Regenerate a proposed reply before terminal state | Epic 3 | Stories 5.5–5.7, 5.14, 5.18 | ✓ Covered |
| FR-17 | Approve and post exactly one selected version | Epic 3 | Stories 5.5, 5.7, 5.11, 5.14, 5.18 | ✓ Covered |
| FR-18 | Reject, abandon, or expire proposals terminally | Epic 3 | Stories 5.5, 5.7, 5.13–5.15, 5.17, 5.18 | ✓ Covered |
| FR-19 | Enforce tenant isolation across all Agent surfaces | Epic 2, cross-cutting | Stories 5.4, 5.8, 5.11, 5.12, 5.16, 5.18 | ✓ Covered |
| FR-20 | Enforce role and policy authorization before side effects | Epic 2, cross-cutting | Stories 5.4, 5.12–5.14, 5.16, 5.18 | ✓ Covered |
| FR-21 | Fail closed on missing, stale, ambiguous, or unavailable dependencies | Epic 2, cross-cutting | Stories 5.4, 5.6, 5.8, 5.11, 5.18 | ✓ Covered |
| FR-22 | Provide authorized administration UI | Epic 4 | Stories 5.3, 5.12–5.14, 5.17, 5.18 | ✓ Covered |
| FR-23 | Provide stable public API/client contracts | Epic 4 | Stories 5.1, 5.3, 5.12, 5.14, 5.18 | ✓ Covered |
| FR-24 | Capture durable, tenant-safe Agent Audit Evidence | Epic 4 | Stories 5.5–5.7, 5.10, 5.11, 5.14–5.16, 5.18 | ✓ Covered |
| FR-25 | Expose operational and readiness status | Epic 4 | Stories 5.2, 5.3, 5.5, 5.7, 5.10, 5.12, 5.13, 5.15–5.17, 5.18 | ✓ Covered |
| FR-26 | Configure a versioned active Content Safety Policy | Epic 1 | Stories 5.9, 5.17, 5.18 | ✓ Covered |
| FR-27 | Enforce safety before Provider and Conversation side effects | Epic 2 | Stories 5.9, 5.17, 5.18 | ✓ Covered |
| FR-28 | Enforce fixed launch-readiness controls and live evidence | Epic 4 | Stories 5.2, 5.9, 5.10, 5.15–5.18 | ✓ Covered |

### Missing Requirements

No PRD functional requirement is absent from the epics coverage map or the authoritative Epic 5 forward plan.

- **Critical missing FRs:** None.
- **High-priority missing FRs:** None.
- **FR identifiers in epics but not in the PRD:** None.

### Coverage Caveat

The document's Course Correction Authority states that Epics 1–4 are historical delivery evidence and cannot establish production readiness. Several historical criteria preserve now-superseded alternatives—for example bounded-context behavior, multiple invocation affordances, reporting-only cost posture, and unresolved audit-governance decisions. Epic 5 and the corrected 2026-08-01 PRD explicitly supersede those paths. Therefore, the 100% result means every FR has a traceable current path; it does not mean the historical completed work already satisfies the FRs or that story quality is adequate.

### Coverage Statistics

- Total PRD FRs: 28
- FRs covered in the formal epic map: 28
- FRs with an authoritative Epic 5 path: 28
- Missing FRs: 0
- Extra FR identifiers: 0
- Coverage percentage: **100%**

## UX Alignment Assessment

### UX Document Status

**Found and final.** The assessment used:

- `ux-designs/ux-agents-2026-06-23/DESIGN.md` — final, updated 2026-08-01.
- `ux-designs/ux-agents-2026-06-23/EXPERIENCE.md` — final, updated 2026-08-01.

The two documents have an explicit ownership split: `DESIGN.md` owns visual semantics and components; `EXPERIENCE.md` owns behavior, surfaces, states, accessibility, responsive behavior, and flows.

### UX ↔ PRD Alignment

| Product area | PRD source | UX realization | Alignment |
| --- | --- | --- | --- |
| Tenant setup and activation | UJ-1; FR-1–FR-7, FR-22, FR-26 | Agents overview, `hexa` configuration, Provider catalog, Approver policy, safety policy, readiness badge | Aligned |
| Explicit automatic invocation | UJ-2; FR-8–FR-12, FR-19–FR-21, FR-27 | Sole Conversation-owned **Call hexa** action, complete-context status, generation/posting states | Aligned |
| Confirmation and approval | UJ-3; FR-13–FR-18, FR-24 | Pending counts, proposal queue, proposal editor, immutable version history, expiry and posting states | Aligned |
| API/client integration | UJ-4; FR-23–FR-25 | Developer-facing public contracts plus UI/API state and authorization parity | Aligned |
| Production-like governance | FR-28; OQ-5–OQ-11 | UX UJ-5, launch readiness, safety, cost, context, retention, export, deletion and evidence surfaces | Aligned; UX adds a derived operator journey |
| Non-goals | PRD Sections 5 and 6.2 | No memory, tools, ambient triggers, external channels, alternate invocation, or multiple exposed Agents | Aligned |

UX adds 47 explicitly numbered UX design requirements. Most elaborate PRD behavior rather than change product scope. Accessibility, localization, responsive behavior, grid-state semantics, focus management, live regions, and component-layout rules are material additional requirements that are not named as PRD NFRs but are carried into the epics.

### UX ↔ Architecture Alignment

Architecture support is strong in these areas:

- AD-15 requires UI/API contract and authorization parity and anchors UI composition in FrontComposer.
- AD-12 supplies fail-closed tenant, Party, Conversation, Agent, Provider, and approver-policy checks required by the UX.
- AD-4, AD-5, and AD-13 preserve interaction snapshots, immutable proposal versions, selected-version approval, and idempotent posting needed by the proposal workspace.
- AD-11 supports the UX's read-only complete-context-or-blocked surface.
- AD-20 and AD-21 support safety-policy and cost-control authoring/status.
- AD-17 defines FrontComposer/UI conformance and Level 4–5 launch evidence.
- AD-22 supports retention, legal hold, export, deletion, projection purge, and restrictive partial-failure states.
- AD-16 provides the platform-owned hosting boundary needed to compose Agents UI with runtime dependencies without module-owned AppHost/Aspire/ServiceDefaults projects.

### Alignment Issues

#### UX-A1 — Degraded Provider semantics are not contractually defined

The UX defines a canonical Provider state `degraded` and says calls may continue when policy allows. The PRD primarily defines enabled, disabled, unavailable, and failed states. AD-10 requires a fresh entry with `Status == Enabled`, configured state, valid capabilities, pricing, and limits, but does not define a `degraded` contract, its permitted causes, or whether it is call-blocking.

**Impact:** UI and runtime could disagree: the UI may present a recoverable warning while the Provider gate blocks, or the UI may allow a call whose runtime state should fail closed.

**Required resolution:** define `degraded` in the public Provider/readiness contract and AD-10, including its callability, policy inputs, safe reason categories, and UI/API parity—or remove it as a callable state and treat it as a display-only warning layered over an otherwise `Enabled` contract.

#### UX-A2 — High-risk command concurrency rule lacks an architecture decision

`EXPERIENCE.md` permits one high-risk side effect at a time per user/session unless architecture explicitly confirms a concurrent command policy. The Architecture Spine defines aggregate concurrency, idempotency, retry, and expiry-race behavior but does not decide the UI/session-level concurrency rule.

**Impact:** proposal approval, policy publishing, export, legal hold, and deletion controls may use inconsistent client-side locking and pending-command behavior.

**Required resolution:** add an architecture or interaction contract decision defining whether the one-active-high-risk-command rule is normative, which operation scopes share the lock, and how recovery from stale client state behaves.

#### UX-A3 — Accessibility, localization, and responsive requirements are absent from PRD NFRs

The UX defines keyboard operation, focus return, live regions, reduced motion, table semantics, localizable whole strings, breakpoint behavior, and viewport-based fail-closed actions. The PRD has no accessibility conformance target, localization requirement, or responsive-support statement.

**Impact:** these requirements are mapped in epics but lack product-level acceptance authority and release priority if schedule pressure creates a conflict.

**Required resolution:** either add explicit PRD NFRs or declare the final UX spine a binding launch source for these concerns in the PRD/readiness policy.

#### UX-A4 — No UI responsiveness or interaction-performance budget

PRD NFR-9 and AD-17 define end-to-end generation/posting latency and evidence levels, but no document defines page-load, status-refresh, command-acknowledgement, grid/filter, or live-region update targets.

**Impact:** architecture cannot be validated against the workflow responsiveness expected by administrators and approvers, particularly for proposal queues and high-impact confirmation flows.

**Required resolution:** define measurable UI/BFF responsiveness targets and evidence conditions, or explicitly accept that V1 has no UI performance release gate.

#### UX-A5 — Success semantics contain a small internal ambiguity

The DESIGN frontmatter and color prose include “active Agent” or “active configuration” among Success examples, while the `agent-readiness-badge` rule correctly reserves Success for all required readiness gates passing. PRD FR-3 and FR-28 permit an Agent to be active yet not callable.

**Impact:** a generic status component could render active lifecycle as success and visually contradict launch callability.

**Required resolution:** make “callable/proven ready” the only Agent-readiness Success meaning; render active-but-blocked lifecycle independently from readiness.

### Warnings

- **Dependency-version drift:** the Architecture Spine and epics baseline pin Fluent UI Blazor `5.0.0-rc.3-26138.1`, while the currently loaded FrontComposer project context pins `5.0.0-rc.4-26180.1`. Component/API and conformance assumptions must be revalidated against the actual selected FrontComposer dependency.
- **SDK-baseline drift:** the Architecture Spine lists .NET SDK `10.0.300`–`10.0.301`, while the loaded Hexalith repository contexts use `10.0.302`. The canonical build baseline should be reconciled before implementation gates are treated as reproducible.
- **External UI seam:** **Call hexa** depends on the external Conversations-owned `CONV-AI-1` membership seam. UX correctly identifies it as gated, but no Agents-only implementation can complete the posting journey without its Level 4–5 evidence.

### UX Alignment Verdict

The UX is comprehensive and substantially aligned with the final PRD and corrected architecture. It is suitable as a binding implementation input, but the degraded-provider contract, UI concurrency rule, product authority for accessibility/localization/responsive requirements, and version drift should be resolved before declaring the UX/architecture pair fully implementation-ready.

## Epic Quality Review

### Review Scope And Authority

All five epics and all 44 stories were reviewed. The document's Course Correction Authority makes Epics 1–4 historical evidence and Epic 5 the only forward implementation plan. Historical defects are still recorded because stale criteria can mislead implementation, but readiness severity is driven primarily by Epic 5.

### Epic Structure Assessment

| Epic | User-value focus | Independence and sequencing | Quality verdict |
| --- | --- | --- | --- |
| Epic 1 — Tenant Agent Setup And Governance | Administrator outcome is clear; Story 1.1 is a technical foundation within the epic | Can establish governed setup without later epics, although end users cannot invoke the Agent yet | Acceptable historical vertical slice with one technical setup story |
| Epic 2 — Safe Conversation Invocation And Automatic Replies | Clear participant outcome | Correctly builds on Epic 1; no dependency on Epics 3–4, but historical Stories 2.3 and 2.6 contain superseded behavior | User-centric, but historical acceptance criteria are not safe implementation authority |
| Epic 3 — Proposal Review And Approval Workflow | Clear Approver outcome | Correctly builds on Epics 1–2 and does not require Epic 4 | Strong user-value structure; several stories depend on runtime capabilities that were not live historically |
| Epic 4 — Operational Visibility, Audit, Integration, And Launch Readiness | Mixes operator/integration value with release and test milestones | Builds on prior epics, but Story 4.2 explicitly deferred governance decisions and Story 4.5 is a conformance milestone | Mixed-purpose and not independently launch-complete |
| Epic 5 — Production Binding And Live Conformance | Operator outcome exists, but the epic is primarily a technical integration/release milestone | Internally ordered with no numbered forward dependency; however, one unresolved external prerequisite and several unnamed external implementation owners block the chain | **Critical best-practice violation:** technical mega-epic rather than an independently valuable user capability |

### 🔴 Critical Violations

#### EQ-C1 — Epic 5 is a technical mega-epic

The title and decomposition are organized around “binding” technical layers and conformance: hosting boundary, EventStore, runtime contracts, Dapr Workflow, live adapters, projections, topology, and evidence. The stated operator outcome does not change that most stories are technical milestones rather than vertical user outcomes.

**Examples:** Stories 5.1, 5.3, 5.6, 5.7, and 5.18.

**Impact:** user-visible value is deferred until late in a long chain, integration risk accumulates, and individual stories can appear complete without producing a usable Agent capability.

**Recommendation:** replace Epic 5 with smaller user-outcome epics or slices such as live governed setup/callability, one safe automatic response end-to-end, one complete confirmation workflow end-to-end, and operator governance/release proof. Each slice should include its own domain, runtime, integration, UI, and focused evidence.

#### EQ-C2 — `CONV-AI-1` is an unresolved external blocker on the critical path

Story 5.11 explicitly cannot complete until Hexalith.Conversations delivers `IConversationClient.AddParticipantAsync` and the participant API with Level 4–5 evidence. Stories 5.12, 5.14, 5.17, and 5.18 depend directly or transitively on Story 5.11.

**Impact:** the forward plan cannot reach its final user journeys or readiness decision inside this repository unless another repository delivers an uncommitted prerequisite.

**Recommendation:** record a named owner, target version/commit, delivery story, compatibility contract, verification command, and integration date. Either deliver it before starting the blocked chain or re-plan the affected stories around an explicit external milestone.

#### EQ-C3 — Required external runtime implementations lack bounded ownership stories

The architecture requires a platform-owned host, a live Content Safety adapter/engine, a live Provider adapter, secret composition, token measurement, and production-like topology. Story 5.1 asserts a platform-owned consumer; Stories 5.9 and 5.10 assume live safety and Provider implementations. The plan does not identify the owning repository/artifact, selected implementation, version, or delivery contract for these dependencies.

**Impact:** “bind the live engine/provider/topology” stories may be impossible to estimate or complete, and their acceptance criteria may depend on work outside the story and repository.

**Recommendation:** add explicit prerequisite records or owned delivery stories for the platform host, selected Provider adapter, Content Safety engine, tokenizer/measurement strategy, secret provisioning, and production-like test topology. Each must state owner, repository, version, API/port contract, and evidence level.

#### EQ-C4 — Story 5.18 is epic-sized and cannot be independently completed

Story 5.18 depends on `CONV-AI-1` and all seventeen prior Epic 5 stories, then requires Level 4 and Level 5 evidence across every subsystem, security/concurrency failure path, metrics, budgets, accessibility, and a new readiness assessment.

**Impact:** it is a release program/gate disguised as one story. It cannot be estimated, delivered, or reviewed as an independently completable story, and any late failure reopens arbitrary earlier work.

**Recommendation:** turn cross-system conformance into explicit verification work attached incrementally to each vertical slice. Keep the final readiness assessment as a release gate/checklist that aggregates already-produced evidence rather than a development story.

### 🟠 Major Issues

#### EQ-M1 — Multiple Epic 5 stories are too large

At least Stories 5.3, 5.4, 5.5, 5.7, 5.10, 5.14, 5.17, and 5.18 combine several independently risky capabilities, layers, and test obligations.

- Story 5.3 combines commands, EventStore persistence, projections, queries, public API/client behavior, freshness, replay, and tenant isolation for both Agent and ProviderCatalog.
- Story 5.4 combines a Tenants event projection, Parties identity adapter, conversation-authority resolution, role revocation, and adversarial security evidence.
- Story 5.5 combines interaction, proposal, queue, status, immutable-version, and audit read models plus content protection and replay behavior.
- Story 5.7 implements the complete automatic and confirmation Dapr Workflow lifecycle, expiry, retries, replay, and restart recovery.
- Story 5.10 combines live Provider execution, pricing, two cap types, atomic reservation, reconciliation, retry charging, concurrency, secret boundaries, and performance sampling.
- Story 5.14 combines every proposal action, all version behavior, posting, expiry races, responsive fail-closed behavior, and full accessibility.
- Story 5.17 combines multiple policy-authoring surfaces, all launch metrics, budgets, retention/governance, and evidence reporting.

**Recommendation:** split by independently demonstrable user outcome and keep each story end-to-end. Avoid splitting only by technical layer.

#### EQ-M2 — The critical path is excessively serial

Epic 5 forms a deep chain: 5.1 → 5.3 → 5.4/5.5 → 5.6 → 5.7 → 5.8 → 5.9 → 5.10 → 5.11 → 5.12/5.14 → 5.17 → 5.18. A defect or external delay near 5.11 blocks most remaining user-visible and readiness work.

**Recommendation:** create independently testable vertical seams using deterministic adapters early, while separately tracking the live external binding milestone. Preserve final live evidence without making all UI and governance work depend on the entire runtime chain.

#### EQ-M3 — Some acceptance criteria are underspecified despite strong BDD form

Examples include “all current gates,” “freshness and version metadata,” “valid current state,” “restrictive state,” “every affected projection,” “production-like topology,” and “for every launch-critical seam.” These are testable only after authoritative registries, freshness thresholds, projection inventories, and environment definitions exist.

**Recommendation:** name the gate registry, freshness rules, projection inventory, topology fixture, authoritative timestamps, and exact pass/fail evidence in the relevant stories.

#### EQ-M4 — Quantitative success evidence may require elapsed time or adoption outside a story

Stories 5.17 and 5.18 require a rolling 30-day cohort with at least 50 eligible Conversations and at least 30 production-like executions per latency gate. The plan does not say whether synthetic production-like cohorts are valid, who supplies them, or how a normal sprint story completes before real adoption exists.

**Recommendation:** separate implementation of metric calculation from later release qualification. Define an approved deterministic fixture for calculation conformance and keep real cohort attainment as an operational launch gate.

#### EQ-M5 — Historical stories retain conflicting acceptance criteria

Although the Course Correction Authority establishes precedence, stale criteria remain executable-looking:

- Story 1.1 anticipates an Agents-owned AppHost extension point, conflicting with corrected AD-16.
- Story 2.3 permits an “approved bounded-context behavior,” conflicting with PRD OQ-10 and AD-11.
- Story 2.6 allows mention, command, action, participant affordance, or a combination, conflicting with the sole **Call hexa** decision.
- Story 4.2 treats retention/legal hold/export/deletion as unresolved, while OQ-8 and AD-22 now resolve them.
- Story 4.4 permits reporting-only cost monitoring or accepted risk, conflicting with hard enforcement in OQ-6 and AD-21.
- Story 4.5 references Agent Framework workflow/session restore and MCP/A2A/tool contracts “where applicable,” although AD-18 and AD-19 put those paths outside V1.

**Recommendation:** add machine-visible `supersededBy` metadata or a clearly bounded historical appendix. Do not leave contradictory acceptance criteria in the same active story namespace relying only on narrative precedence.

### 🟡 Minor Concerns

- Epics 1–4 do not carry explicit dependency metadata per story; dependencies must be inferred from order and acceptance criteria.
- Many acceptance scenarios join multiple independently testable outcomes with several `And` clauses, making failure ownership and completion evidence less precise.
- Several story titles use technical verbs such as “Bind,” “Persist,” “Reconcile,” and “Prove” even when the story persona/outcome is user-oriented.
- Story 4.5 uses “where applicable,” which weakens a launch-gate acceptance criterion.
- Version ranges and shorthand references such as FR ranges and “AD-1–AD-22” are concise but can hide whether every individual obligation has an executable verification path.

### Positive Compliance Findings

- All stories use a valid persona / intent / benefit structure and BDD-style acceptance criteria.
- Error, authorization, cross-tenant, replay, idempotency, and partial-failure paths are consistently represented.
- No numbered Epic 5 story depends on a later-numbered story; all declared within-epic dependencies point backward.
- Event-sourced models/read models are introduced when their capability needs them; no story creates all database tables or domain entities upfront.
- Architecture specifies no external starter template; historical Story 1.1 correctly used the Structural Seed and explicitly avoided pre-creating unrelated entities.
- The brownfield/corrective nature is explicit, and integration boundaries with EventStore, Conversations, Parties, Tenants, FrontComposer, Dapr Workflow, safety, and Providers are named.
- Every story maintains explicit FR/NFR/UX/AD traceability, especially in Epic 5.

### Story-By-Story Quality Assessment

#### Epic 1 stories

| Story | User value and independence | Sizing / AC quality | Verdict |
| --- | --- | --- | --- |
| 1.1 Buildable Agents Module Shell And Public Boundaries | Technical enabler, not direct user value; can precede all capability stories | Broad but bounded foundation criteria; stale AppHost extension-point wording conflicts with AD-16 | Major historical concern |
| 1.2 Govern Provider Catalog Entries | Clear administrator value; uses only 1.1 | Strong happy, denial, secret-safety, replay, and rejection criteria | Pass |
| 1.3 Configure And Manage `hexa` Lifecycle | Clear administrator value; independently demonstrates durable configuration | Specific activation, preservation, authorization, and audit criteria | Pass |
| 1.4 Link `hexa` To A Party Identity | Clear identity/attribution value; depends on existing Parties seam rather than future Agents story | Good fail-closed and PII-boundary criteria | Pass with external Parties dependency |
| 1.5 Select Provider And Model For `hexa` | Clear setup value; uses prior Provider catalog | Strong readiness, future-only, audit, and cross-tenant criteria | Pass |
| 1.6 Configure Response Mode And Approver Policy | Clear policy value; uses current setup outputs | Good future-only and fail-closed criteria; Conversation authority seam is external | Pass with external Conversations dependency |
| 1.7 Configure Content Safety Policy And Activation Gate | Clear safety/setup value | Good policy and activation criteria, but does not bind a live safety implementation | Pass as historical contract slice, not live readiness |
| 1.8 Admin Setup UI And Readiness Overview | Clear administrator UI outcome | Large but coherent; includes navigation, grids, forms, semantic states, accessibility, and error states | Minor sizing concern |

#### Epic 2 stories

| Story | User value and independence | Sizing / AC quality | Verdict |
| --- | --- | --- | --- |
| 2.1 Request `hexa` From A Source Conversation | Clear participant outcome; uses Epic 1 setup | Strong deterministic identity, snapshot, no-ambient-trigger, and secrecy criteria | Pass |
| 2.2 Enforce Invocation Authorization And Dependency Readiness | Clear safety value | Strong negative and safe-error paths | Pass |
| 2.3 Build Conversation Context With Safe Bounds | Clear grounding value | First three scenarios are strong; fourth permits future bounded-context behavior prohibited by final PRD | Major superseded-criteria defect |
| 2.4 Generate And Safety-Check Agent Output | Clear safe-generation outcome | Good adapter, failure, safety, and retry criteria; “selected durable owner” was historically underspecified | Minor historical ambiguity |
| 2.5 Post Automatic Responses Through Conversations | Clear participant value | Strong attribution and idempotency criteria; depends on external membership seam | Pass structurally; externally blocked for live completion |
| 2.6 Conversation Invocation UX And Call Status Feedback | Clear UI outcome | Good status/accessibility behavior, but first AC allows multiple invocation forms now forbidden | Major superseded-criteria defect |

#### Epic 3 stories

| Story | User value and independence | Sizing / AC quality | Verdict |
| --- | --- | --- | --- |
| 3.1 Create Proposed Agent Replies In Confirmation Mode | Clear Approver value and correct outside-Conversation boundary | Strong safety, visibility, retry, and deterministic-state criteria | Pass |
| 3.2 Discover Pending Proposals In Product | Clear discoverability value | Complete queue, filter, empty-state, notification, and denial scenarios | Pass |
| 3.3 Edit Proposed Reply Versions | Clear editing value | Strong immutability, terminal-state, disclosure, and audit criteria | Pass |
| 3.4 Regenerate Proposed Reply Versions | Clear comparison/retry value | Strong preserved-version, failure, safety, and terminal-state criteria | Pass |
| 3.5 Approve A Selected Version And Post It | Clear core Approver outcome | Strong selected-version, attribution, idempotency, and final-gate criteria; external posting seam remains required | Pass structurally; externally blocked for live completion |
| 3.6 Reject, Abandon, And Expire Proposals | Clear terminal-resolution value | Good terminal behavior; historical AC did not establish durable timer ownership or immutable `ExpiresAt` semantics now required | Major historical completeness gap |
| 3.7 Proposal Detail, Version History, And Accessibility | Clear complete workspace outcome | Broad but coherent UI story with detailed keyboard, live-region, and disclosure criteria | Minor sizing concern |

#### Epic 4 stories

| Story | User value and independence | Sizing / AC quality | Verdict |
| --- | --- | --- | --- |
| 4.1 Stable API And Client Contracts For Agent Operations | Clear integration-developer value | Strong surface, error, versioning, parity, and state criteria | Pass |
| 4.2 Query Audit Evidence Safely | Clear compliance-operator value | First scenarios are strong; final scenario explicitly blocks completion on unresolved governance decisions | Major historical incompleteness |
| 4.3 Expose Operational Status And Admin Workflows | Clear operator value | Broad but testable state, recovery, navigation, and metric criteria | Minor sizing concern |
| 4.4 Define And Enforce Launch Readiness Gates | Clear release-operator value | Metric structure is useful, but cost AC permits reporting-only controls or accepted risk contrary to final binding decisions | Major superseded-criteria defect |
| 4.5 Verify End-To-End Governance And Contract Conformance | Primarily a test/release milestone rather than a user story | Epic-sized matrix across all domains; includes out-of-scope/conditional framework and protocol paths | Critical story-quality violation |

#### Epic 5 stories

| Story | User value and dependency quality | Sizing / AC quality | Verdict |
| --- | --- | --- | --- |
| 5.1 Correct Platform Hosting Boundary And Quality Gates | Platform-maintainer value, but technical foundation; no forward story dependency | Combines project removal, package graph, external platform topology, CI, and checkout gates; platform host owner not named | Major |
| 5.2 Enforce Complete Launch Readiness Before Callability | Clear release safety value and can fail closed early | “Any/all current gates” needs a normative gate registry and freshness policy | Major specification gap |
| 5.3 Bind EventStore Operations And Setup Read Models | Administrator value, depends correctly on 5.1 | Commands, persistence, replay, two aggregate families, projections, queries, APIs, freshness, and tenant tests in one story | Major oversizing |
| 5.4 Bind Tenant Access Party Identity And Approver Resolution | Clear security/operator outcome; dependencies point backward | Three external identity/authorization seams plus event ordering and adversarial proof in one story | Major oversizing |
| 5.5 Persist Interaction Proposal Status And Audit Read Models | Clear Approver/operator read value | Too many read-model families, protection rules, replay/freshness states, and security cases | Major oversizing |
| 5.6 Reconcile Provider Capability Runtime Contracts | Runtime-maintainer persona; strongly technical | Detailed and testable, but spans context, generation, regeneration, high-water state, fingerprints, retries, and concurrency | Major technical/sizing concern |
| 5.7 Bind Dapr Workflow As The Sole V1 Durable Owner | Runtime-operator resilience value; backward dependencies are explicit | Implements nearly the entire automatic and confirmation orchestration lifecycle plus restart/replay | Critical oversizing |
| 5.8 Bind Authorized Complete Conversation Context | Clear participant trust outcome | Focused and testable; Provider tokenizer/measurement ownership is not defined | Major external-design gap |
| 5.9 Bind The Live Content Safety Engine | Clear security value | Strong policy cases, but no selected/owned live safety implementation or availability contract | Critical external-ownership gap |
| 5.10 Bind Provider Execution And Atomic Cost Reservations | Clear budget-owner value | Provider implementation, pricing, two caps, atomic reservation, reconciliation, concurrency, retries, secrecy, and performance proof are too broad | Critical oversizing/external dependency |
| 5.11 Establish AI Membership And Post Idempotently | Clear participant outcome | Focused ACs, but explicitly blocked by external `CONV-AI-1` | Critical blocker |
| 5.12 Expose The Conversation Owned Call Hexa Action | Clear participant UI outcome; depends on the complete call chain | Broad accessibility/status scope but coherent; transitively blocked by 5.11 | Major dependency risk |
| 5.13 Expose Pending Proposal Counts And Needs My Action Queue | Clear Approver discoverability value | Focused and well specified; dependencies point backward | Pass |
| 5.14 Expose Proposal Detail Actions And Accessible Expiry | Clear Approver outcome | Combines every proposal mutation, posting, concurrency race, responsive behavior, and accessibility suite | Critical oversizing |
| 5.15 Enforce Sensitive Retention And Legal Holds | Clear governance outcome | Large but separable retention/hold slice; “every affected path” and live protection environment need enumeration | Major |
| 5.16 Export And Cryptographically Delete Agent Audit Content | Clear governance outcome; correctly follows 5.15 | Export and deletion are two high-risk capabilities with different failure models; projection inventory is not explicit | Major oversizing |
| 5.17 Operate Policies Metrics Budgets And Launch Status | Clear operator outcome | Bundles all policy UIs, retention, budgets, latency, adoption, proposal metrics, and evidence levels; depends on almost all prior work | Critical oversizing |
| 5.18 Prove Live Conformance And Reassess Readiness | Release/test persona, not independently usable product value | Depends on every prior story and external seam; covers the entire system and elapsed/cohort evidence | Critical epic-sized story |

### Dependency Direction Summary

- **Numbered forward dependencies:** none found in Epic 5.
- **Circular dependencies:** none found.
- **External blocking dependency:** `CONV-AI-1`, affecting Stories 5.11, 5.12, 5.14, 5.17, and 5.18.
- **Unnamed external delivery dependencies:** platform-owned host/topology, selected live Content Safety engine, selected live Provider adapter, secret composition, and token measurement.
- **Longest declared internal chain:** 5.1 → 5.3 → 5.4/5.5 → 5.6 → 5.7 → 5.8 → 5.9 → 5.10 → 5.11 → 5.12/5.14 → 5.17 → 5.18.

### Epic Quality Verdict

Traceability and BDD discipline are strong, but the forward plan does not meet create-epics-and-stories quality standards. Epic 5 is a technical mega-epic with multiple epic-sized stories, a deep serial critical path, an unresolved external Conversations prerequisite, and several live runtime implementations without bounded ownership. It must be restructured or explicitly accepted as a release program rather than treated as an implementation-ready epic/story plan.

## Summary and Recommendations

### Overall Readiness Status

# NOT READY

The artifacts are not ready to authorize Phase 4 implementation as currently planned.

Requirements traceability is complete—28 of 28 FRs have current epic paths—but traceability is not the blocking problem. The forward Epic 5 plan is structured as a technical release program with several epic-sized stories, a deep serial critical path, an unresolved external Conversations prerequisite, and unnamed owners for required live runtime implementations. UX and architecture also retain unresolved contract semantics and dependency-version drift.

| Assessment area | Result | Evidence |
| --- | --- | --- |
| Document availability | Pass | Final PRD, Architecture, UX, and Epics artifacts were found and confirmed |
| PRD FR traceability | Pass | 28/28 FRs mapped; 100% coverage; no extra FR identifiers |
| PRD completeness | Needs work | Seven documented gaps/risks, including external prerequisites, missing operational NFRs, and undefined evidence taxonomy |
| UX ↔ PRD ↔ Architecture | Needs work | Five alignment issues and three warnings; no missing UX document |
| Epic/story quality | Fail | Four critical violations, five major issues, and five minor concerns |
| External delivery readiness | Fail | `CONV-AI-1` blocks the posting chain; platform host, Provider, safety, tokenizer, secrets, and topology ownership are not bounded |

### Critical Issues Requiring Immediate Action

1. **Replan Epic 5 as user-value delivery rather than a technical mega-epic.** Stories 5.1, 5.3, 5.6, 5.7, and 5.18 are technical milestones; Stories 5.3, 5.4, 5.5, 5.7, 5.10, 5.14, 5.17, and 5.18 are materially oversized.
2. **Resolve `CONV-AI-1` before committing to the blocked critical path.** Story 5.11 and all transitive posting/UI/readiness work require a live Conversations-owned membership seam with Level 4–5 evidence.
3. **Assign owners and contracts for every external runtime dependency.** The plan needs explicit delivery authority for the platform-owned host/topology, selected live Provider adapter, selected Content Safety engine, token measurement, secret composition, and production-like environment.
4. **Remove Story 5.18 as a single development story.** It is a whole-program conformance and release gate depending on all prior work, an external prerequisite, broad live evidence, elapsed/cohort metrics, and a new readiness assessment.
5. **Define the launch-readiness contract precisely.** Name the gate registry, freshness/staleness rules, authoritative timestamps, projection inventory, topology fixture, evidence-level taxonomy, and conditions under which an insufficient or missing sample blocks callability.
6. **Resolve UX/runtime semantic gaps.** Define Provider `degraded` behavior and callability, the one-active-high-risk-command concurrency rule, and the distinction between active lifecycle and proven callability in Success styling.
7. **Eliminate active-looking superseded criteria.** Historical Stories 1.1, 2.3, 2.6, 4.2, 4.4, and 4.5 conflict with the corrected PRD/Architecture and need machine-visible supersession or relocation to a historical appendix.

### Recommended Next Steps

1. Replace Epic 5 with smaller vertical outcome slices:
   - live governed setup and callability;
   - one safe automatic response end-to-end;
   - one complete confirmation/approval/posting flow;
   - governance, operations, and release qualification.
2. Split oversized stories so each produces one demonstrable outcome with its own domain, adapter, UI/API, and focused evidence rather than a technical layer or whole subsystem.
3. Create a dependency register for `CONV-AI-1`, platform hosting, Provider, safety, tokenization, secret provisioning, and topology. Record owner, repository, version/commit, contract, target date, compatibility test, and evidence level.
4. Update the architecture and public contracts for Provider degraded-state semantics, high-risk UI command concurrency, gate/freshness registries, projection enumeration, and UI responsiveness targets.
5. Promote accessibility, localization, responsive fail-closed behavior, and UI performance into binding PRD NFRs—or explicitly declare the final UX spine a co-equal launch authority.
6. Reconcile the build baseline with current dependencies: .NET SDK `10.0.302` and the actual FrontComposer/Fluent UI Blazor version, currently `5.0.0-rc.4-26180.1` in loaded project context rather than the architecture's `rc.3` pin.
7. Separate deterministic metric-calculation conformance from real launch qualification. Use approved fixtures for implementation tests; retain the rolling 30-day/50-Conversation cohort and production-like samples as later operational gates.
8. Attach Level 4 evidence incrementally to each vertical slice and reserve the final readiness assessment for evidence aggregation and the READY/NOT READY decision.
9. Rerun implementation readiness after the PRD/UX authority decisions, architecture contract updates, Epic 5 restructuring, and external dependency commitments are complete.

### Final Note

This assessment recorded **29 issue entries across three primary categories**: PRD completeness, UX/architecture alignment, and epic/story quality. Four epic-quality violations are critical, and the external `CONV-AI-1` seam independently blocks the posting critical path.

The work has a strong product model, comprehensive UX, explicit architecture invariants, complete FR coverage, and unusually good negative-path acceptance criteria. Those strengths do not offset the current delivery-plan defects. Proceeding as-is would create a high risk of partially completed technical layers, late external-integration failure, and a final conformance story that cannot close. Address the critical issues before authorizing implementation against the current plan.

**Assessment date:** 2026-08-01  
**Assessor:** Codex, acting as the BMAD implementation-readiness Product Manager
