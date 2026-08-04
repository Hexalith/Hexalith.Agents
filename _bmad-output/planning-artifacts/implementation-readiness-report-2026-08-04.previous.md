---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
assessmentStatus: NOT READY
assessor: Codex — BMAD Implementation Readiness workflow
inputDocuments:
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
excludedDocuments:
  - _bmad-output/planning-artifacts/epic-5-superseded-2026-08-01.md
  - PRD polish, reconciliation, review, and validation artifacts
  - Architecture and UX review and validation artifacts
  - Previous implementation-readiness reports
---

# Implementation Readiness Assessment Report

**Date:** 2026-08-04
**Project:** agents

## Document Discovery

### Authoritative Assessment Inputs

**PRD**

- `prds/prd-agents-2026-06-23/prd.md` — 54,600 bytes; modified 2026-08-01 19:17
- `prds/prd-agents-2026-06-23/addendum.md` — 1,950 bytes; modified 2026-08-01 19:13

**Architecture**

- `architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md` — 58,356 bytes; modified 2026-08-02 11:34
- `architecture/architecture-agents-2026-06-23-2/IMPLEMENTATION-CONVENTIONS.md` — 6,176 bytes; modified 2026-07-31 22:36

**Epics and Stories**

- `epics.md` — 208,716 bytes; modified 2026-08-02 19:10

**UX Design**

- `ux-designs/ux-agents-2026-06-23/DESIGN.md` — 23,049 bytes; modified 2026-08-02 12:04
- `ux-designs/ux-agents-2026-06-23/EXPERIENCE.md` — 39,907 bytes; modified 2026-08-02 12:06

### Discovery Resolution

- No whole-document versus indexed-shard duplicate formats were found.
- The authoritative assessment set is the set confirmed above.
- `epic-5-superseded-2026-08-01.md` is excluded because it is explicitly superseded.
- PRD polish, reconciliation, review, and validation files are excluded as intermediate or supporting evidence.
- Architecture and UX review and validation reports are excluded as supporting evidence.
- Earlier readiness reports are excluded because they are outputs of previous assessments.
- No required document category is missing.

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
- Existing Agents using a disabled provider/model cannot be activated or called until reconfigured. A documented migration state may allow temporary read-only inspection.
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
- Failed generation in Confirmation Response Mode does not create a Proposed Agent Reply. Any failed or incomplete generated content retained for authorized audit is stored only in a separate, non-approvable failure record.

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
- V1 uses in-product pending-proposal visibility only for authorized Approvers, including a pending count, a queue, and a Conversation status entry.
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
- A critical external dependency is not implementation-ready until its external dependency register entry satisfies every commitment field defined in PRD §8.
- An `Uncommitted` dependency, missing target, or missing compatibility verification command blocks every consuming story from `ready-for-dev`.

#### FR-22: Provide Admin UI

The admin UI allows authorized administrators to manage Global Providers Aggregate entries, configure `hexa`, inspect lifecycle state, configure response and approver policy, and view Agent operation and proposal status.

Testable consequences:

- Admin UI actions enforce the same authorization rules as API/client contracts.
- Admin UI never exposes Provider secrets.
- Admin UI clearly distinguishes active, disabled, invalid, pending proposal, failed call, and expired proposal states.
- Admin UI satisfies the accessibility, localization, responsive safety, and interaction-performance requirements in NFR-13 and NFR-14.

#### FR-23: Provide API And Client Contracts

The system exposes stable API/client contracts for Provider administration, Agent configuration, Agent Calls, proposal workflow, status inspection, and audit inspection.

Testable consequences:

- API/client contracts do not require callers to use raw EventStore, internal aggregate, internal projection, or provider SDK details.
- API/client contracts return structured success and error results suitable for automation.
- JSON object evolution is additive within V1.
- Public enums define `Unknown = 0`; new values may be added, but an existing value's meaning cannot be reused.
- No public member or enum value is removed, renamed, or semantically reused within V1.
- A breaking public change requires a new major package/API version and package-consumer compatibility tests.

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

V1 launch readiness requires the fixed metric thresholds, latency targets, full-context behavior, cost controls, audit governance, NFR-11 through NFR-14, external dependency commitments, and the normative evidence authority in PRD §11.

Testable consequences:

- Controlled production-like qualification may collect live evidence only after Content Safety Policy, Conversation Context Policy, cost controls, audit governance, and the required external dependency commitments are active and recorded. Qualification access does not authorize production enablement.
- Production enablement remains blocked until `RQ-1` records READY from the required live evidence and launch metrics.
- Per-tenant monthly and per-call cost caps are hard enforcement boundaries: 80% emits a warning, 100% fails closed, and atomic reservation plus reconciliation prevents concurrent overspend. The system reserves the maximum estimated attempt cost before Provider invocation, reconciles actual usage, releases any unused reservation only after confirming that no usage occurred, and reuses the same reservation for eligible retries. Missing pricing or budget state blocks invocation; reporting-only monitoring is insufficient.
- Automatic accepted-call-to-post latency is p95 at most 60 seconds and p99 at most 120 seconds. Confirmation accepted-call-to-proposal latency uses the same thresholds; approval-to-post latency is p95 at most 10 seconds and p99 at most 30 seconds.
- Pre-Provider authorization, policy, budget, and context rejections complete at p95 at most 2 seconds. Each performance gate uses at least 30 production-like executions.
- Production readiness requires live Evidence Levels 4 and 5 as defined in PRD §11; lower levels, skips, placeholders, or conditional results cannot independently establish launch readiness.

**Total Functional Requirements: 28**

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

**Total Non-Functional Requirements: 14**

### Additional Requirements

#### Scope Constraints

- V1 exposes only `hexa`, although generalized internal structures are permitted.
- V1 has no long-term memory, Agent tools, project/folder/file retrieval, ambient triggers, project/folder activation, agent-to-agent orchestration, external-channel bots, or business actions beyond adding Agent responses to Conversations.
- The only invocation entry is the Conversation-owned **Call hexa** action; mention, command, and alternate invocation paths are excluded.
- Unapproved output never becomes a Conversation Message, and historical proposal versions are never rewritten or deleted.
- Provider secrets are never exposed, and Conversation Context is never silently reduced.

#### External Dependency Commitments

A critical external dependency is implementation-ready only when its register entry includes a named owner, owning repository, required artifact, target version or commit, target integration date, compatibility contract or test plus verification command, required Evidence Level, accepted status, and consuming stories. `Uncommitted`, a missing target, or a missing verification command blocks consuming stories from `ready-for-dev`.

The initial critical dependency set is `EXT-CONV-AI-1`, `EXT-HOST-1`, `EXT-PROVIDER-1`, `EXT-SAFETY-1`, `EXT-TOKEN-1`, `EXT-SECRETS-1`, and `EXT-TOPOLOGY-1`.

`EXT-CONV-AI-1` requires Conversations to publish `IConversationClient.AddParticipantAsync` and `POST /api/v1/conversations/{conversationId}/participants`, limited to stable `ParticipantType.AiAgent`/`AIAgent` plus `ParticipantRole.Member`, with deterministic idempotency, typed conflicts, and cross-tenant denial. Agents must not write Conversation streams directly or treat unapproved proposals as Conversation Messages.

#### Data Governance And Retention

- Audit Evidence is tenant-scoped and restricted to authorized Parties or operators.
- Every generated, edited, and regenerated proposal version is preserved.
- Automatic and approved posts link to the Agent Call, Source Conversation, caller, Agent, Provider/model, response mode, and final Conversation Message.
- Rejected, abandoned, and expired proposals remain non-postable audit records.
- Provider secrets and raw credentials are never audit content.
- Sensitive Agent content is retained for 365 days after terminal state unless legal hold applies.
- Authorized export is tenant-scoped, encrypted, time-limited, manifested, and audited.
- EventStore history is never rewritten. Approved expiry/deletion cryptographically erases or redacts protected payloads and purges affected projections while preserving only a support-safe non-content tombstone; completion requires restrictive confirmation from payload protection and every affected projection.
- Posted Conversation Messages remain governed by Hexalith.Conversations retention.

#### Public Contract Compatibility

- Public capabilities cover Provider administration, Agent administration, Agent invocation, proposal workflow, status, and audit.
- Consumers are insulated from internal EventStore stream names, aggregate mechanics, projection internals, and provider SDK details.
- V1 JSON evolution is additive; public enums begin with `Unknown = 0`; existing public members and meanings cannot be removed, renamed, or reused.
- Breaking changes require a new major package/API version and package-consumer compatibility tests.

#### Evidence And Qualification

- Evidence Level 1 is contract/structure evidence; Level 2 is pure domain/unit behavior; Level 3 is a fail-closed deferred seam; Level 4 is live component integration; Level 5 is cross-system production-like evidence.
- Production-like readiness requires live Levels 4 and 5 for runtime behavior, authorization, tenant isolation, Provider integration, content safety, Conversations integration, audit behavior, and topology.
- Lower-level evidence, skips, placeholders, or conditional results cannot establish launch readiness.
- Each metric requires a versioned measurement contract defining authoritative events/timestamps, calculation, cohort/window/sample rules, late/missing-data handling, and `InsufficientEvidence` conditions.
- Deterministic fixtures prove calculator behavior only. Real rolling-window/cohort attainment and the final READY/NOT READY decision remain the operational `RQ-1` gate.

#### Success And Counter-Metrics

- **SM-1:** At least one launch tenant configures and enables `hexa`, satisfies launch gates, and records successful production or production-like Agent Calls.
- **SM-2:** At least 20% of eligible Conversations in a cohort of at least 50 record accepted Agent Calls over a rolling 30 days.
- **SM-3:** At least 95% of proposals reach terminal state within 26 hours over a rolling 30 days; expiry ≤ 20%; posting failures ≤ 2%; human resolution ≥ 70%; audit completeness = 100%.
- **SM-4:** Zero successful cross-tenant or unauthorized calls, proposal actions, or audit inspections.
- **SM-5:** Every posted response has complete linked Audit Evidence.
- **SM-6:** Core operations have admin/API parity with identical authorization outcomes.
- Do not optimize automatic-post volume at the expense of confirmation safety, approval speed at the expense of audit quality, or Provider breadth ahead of governance.

#### Binding V1 Decisions

- **OQ-1:** The sole invocation entry is Conversation-owned **Call hexa**.
- **OQ-2:** Agents owns durable proposal state/read models; Dapr Workflow owns execution only.
- **OQ-3:** Expiry defaults to 24 hours and is configurable from 1 hour to 30 days for future proposals only.
- **OQ-4:** Notifications are in-product pending-count/queue/status surfaces only.
- **OQ-5:** Latency thresholds and minimum 30-execution evidence gates are fixed as stated in NFR-9.
- **OQ-6:** Hard per-tenant monthly/per-call caps use warning, fail-closed, atomic reservation, reconciliation, and retry reuse rules.
- **OQ-7:** The Global Providers Aggregate owns provider/model identity, enablement, capabilities/limits, secret references, versioned pricing, and `CapabilityVersion`.
- **OQ-8:** Sensitive content retention is 365 days, with legal hold, encrypted export, cryptographic erasure/redaction, projection purge, and safe tombstones.
- **OQ-9:** Prompt/context pre-check and output post-check categories are fixed; Approvers cannot override safety failures; retries cannot weaken policy.
- **OQ-10:** Full Source Conversation or fail closed; no truncation, summarization, or windowing.
- **OQ-11:** SM-2 and SM-3 thresholds are binding.
- **OQ-12:** Only the complete authorized Source Conversation is used; memory, projects, folders, tools, and non-conversation retrieval are excluded.
- **OQ-13:** Internal generalization is allowed, but V1 exposes only `hexa`.

#### Contextual Addendum

The addendum is explicitly contextual and does not override the product brief or PRD. It positions Hexalith Agents against Slack AI, Microsoft 365 Copilot in Teams, Zoom AI Companion, and Atlassian Rovo while reinforcing the differentiated constraints: durable named identity, tenant-safe attribution, proposal/approval separation, asynchronous Conversation participation, and deliberate exclusion of broad tools, memory, ambient triggers, and project/folder activation.

### PRD Completeness Assessment

The PRD is structurally strong and sufficiently explicit for traceability analysis: it contains 28 stable functional requirements, 14 stable non-functional requirements, testable consequences, explicit scope and non-goals, quantitative performance/cost/recovery/capacity/UI gates, public compatibility rules, normative evidence levels, success metrics, and 13 resolved V1 decisions. Remaining targets and acceptance details for critical external dependencies are intentionally delegated to the external dependency register, while live Level 4/5 attainment and the final READY/NOT READY decision remain intentionally deferred to the post-implementation `RQ-1` operational gate. No unresolved product decision is declared within the PRD itself.

## Epic Coverage Validation

The epics document declares the current PRD, Architecture Spine, UX spines, dependency/readiness registers, and active Epics 5–8 as the replacement implementation authority. Completed Epics 1–4 are historical evidence only, and the former Epic 5 is explicitly superseded and non-executable. This matrix therefore evaluates the active Epics 5–8 and their 27 stories.

### Coverage Matrix

| FR | PRD requirement | Active epic coverage | Principal active story path | Status |
| --- | --- | --- | --- | --- |
| FR-1 | Configure `hexa` with stable identity, metadata, instructions, lifecycle, and tenant scope. | Epic 5 | 5.2, 5.7 | Covered |
| FR-2 | Link exactly one valid Agent Party identity and attribute posts to it. | Epics 5 and 6 | 5.4, 6.6 | Covered |
| FR-3 | Activate, disable, and inspect lifecycle without deleting history. | Epic 5 | 5.2, 5.7 | Covered |
| FR-4 | Govern Provider/model catalog entries, enablement, capabilities, and safe configuration. | Epics 5 and 8 | 5.3, 5.5, 8.4 | Covered |
| FR-5 | Select and validate an enabled Provider/model per Agent for future calls. | Epics 5 and 6 | 5.3, 5.5, 6.2, 6.4 | Covered |
| FR-6 | Configure future-only Automatic or Confirmation Response Mode. | Epic 5 | 5.2, 5.7 | Covered |
| FR-7 | Configure, resolve, enforce, disclose, and audit Approver Policy authority. | Epics 5 and 7 | 5.4, 7.1, 7.4 | Covered |
| FR-8 | Explicitly call `hexa` from the Source Conversation under authorization. | Epic 6 | 6.1, 6.7 | Covered |
| FR-9 | Use the complete authorized Conversation or block before Provider invocation. | Epic 6 | 6.2 | Covered |
| FR-10 | Handle generation, Provider, context, safety, timeout, and policy failures safely. | Epic 6 | 6.1–6.4, 6.7 | Covered |
| FR-11 | Post exactly one successful automatic response as the Agent Party identity. | Epic 6 | 6.6, 6.7 | Covered |
| FR-12 | Prevent automatic posting whenever any required gate fails. | Epic 6 | 6.2–6.6 | Covered |
| FR-13 | Create and expose a proposal only after successful Confirmation-mode generation. | Epic 7 | 7.1 | Covered |
| FR-14 | Preserve every generated, edited, and regenerated proposal version immutably. | Epic 7 | 7.1–7.4 | Covered |
| FR-15 | Permit authorized editing while preserving prior version and authorship. | Epic 7 | 7.2 | Covered |
| FR-16 | Regenerate under fresh gates without losing history or mutating terminal proposals. | Epic 7 | 7.3 | Covered |
| FR-17 | Approve and post exactly one selected proposal version as `hexa`. | Epic 7 | 7.4 | Covered |
| FR-18 | Reject, abandon, or deterministically expire proposals while preserving evidence. | Epics 7 and 8 | 7.5, 7.6, 8.1 | Covered |
| FR-19 | Enforce tenant isolation across every setup, runtime, proposal, governance, status, and audit path. | Epics 5–8 | Cross-cutting negative evidence in every active story | Covered |
| FR-20 | Enforce current role/policy authorization before every side effect with API/UI parity. | Epics 5–8 | Cross-cutting authorization criteria in every active story | Covered |
| FR-21 | Fail closed on dependency uncertainty and gate story readiness on accepted commitments. | Epics 5–8 | 5.1, 5.4–5.7, dependency clauses throughout | Covered |
| FR-22 | Deliver authorized FrontComposer/Fluent administration and workflow UI satisfying NFR-13/14. | Epics 5–8 | 5.2–5.7, 6.7, 7.1–7.6, 8.1–8.7 | Covered |
| FR-23 | Publish stable additive public API/client contracts without leaking internals. | Epics 5–8 | 5.1–5.5 and public-contract clauses throughout | Covered |
| FR-24 | Capture tenant-scoped durable audit evidence for every governed action and outcome. | Epics 5–8 | Evidence manifests and audit clauses throughout | Covered |
| FR-25 | Expose authoritative readiness, runtime, proposal, governance, performance, and blocker status. | Epics 5–8 | 5.5, 6.7, 7.1–7.6, 8.5–8.7 | Covered |
| FR-26 | Publish and operate a versioned Content Safety Policy. | Epics 6 and 8 | 6.3, 8.4 | Covered |
| FR-27 | Enforce fresh, no-weaker safety before Provider, proposal, regeneration, approval, and posting effects. | Epics 6 and 7 | 6.3, 7.3, 7.4 | Covered |
| FR-28 | Implement launch controls and bounded evidence while keeping `RQ-1` outside implementation. | Epics 5–8 | 5.5–5.7, 6.1–6.7, 7.1–7.6, 8.1–8.7 | Covered |

### Missing Requirements

No PRD functional requirement is missing from the active epic coverage map. No functional-requirement identifier appears in the active epics that is absent from the PRD.

This finding concerns declared traceability only. Story quality, sequencing, acceptance-criteria sufficiency, architecture alignment, and whether dependency gates currently permit implementation are evaluated in later workflow steps.

### Coverage Statistics

- Total PRD FRs: 28
- FRs claimed by active Epics 5–8: 28
- Missing FRs: 0
- Extra epic FR identifiers not present in the PRD: 0
- Declared FR coverage: 100%

## UX Alignment Assessment

### UX Document Status

**Found and final.** The authoritative UX set consists of `DESIGN.md` and `EXPERIENCE.md`, both updated 2026-08-02. `DESIGN.md` owns visual semantics and component styling; `EXPERIENCE.md` owns behavior, surfaces, state distinctions, accessibility, responsiveness, performance evidence, and user flows. Both declare the PRD, Architecture Spine, launch-readiness register, and approved 2026-08-02 change authority as sources.

### UX ↔ PRD Alignment

- PRD journeys UJ-1 through UJ-4 map directly to UX journeys for Agent configuration, automatic invocation, confirmation/approval, and API integration.
- UX adds UJ-5 for governed production-like launch; it is a direct elaboration of FR-28, NFR-9 through NFR-14, PRD §11 evidence rules, and the launch-readiness controls rather than new product scope.
- Every user-facing PRD capability has a defined UX surface: setup/readiness, Provider catalog, Approver policy, full-context policy, Content Safety policy, cost controls, Conversation-owned **Call hexa**, proposal queue/detail/history, operational status, audit evidence, audit governance, and launch readiness.
- The UX preserves the PRD's critical truth boundaries: lifecycle active is not callability; submitted is not authoritative pending; approved/posting-pending is not posted; failed generation is not a proposal; and only authoritative `posted` proves a Conversation Message exists.
- Scope exclusions align: no memory, tools, project/folder content, ambient triggers, alternate invocation entries, external channels, or multiple named V1 Agents.
- PRD NFR-13 is elaborated into WCAG 2.2 AA behavior, whole-string English/French parity, FrontComposer/Fluent inheritance, and restrictive-viewport fail-closed rules.
- PRD NFR-14 is elaborated without threshold drift: page usability p95 ≤ 2.5 seconds, authoritative-pending render p95 ≤ 500 ms, and terminal render/live-region mutation p95 ≤ 2 seconds, with at least 30 qualifying executions per sample kind and `InsufficientEvidence` on invalid or missing evidence.

### UX ↔ Architecture Alignment

- Architecture AD-15 explicitly binds Admin UI/API parity to the UX spine and requires the UI to use public Agents boundaries rather than EventStore, aggregate, workflow, or Provider internals.
- AD-25 supports the complete accessibility, localization, FrontComposer/Fluent V5, route/state inventory, and restrictive-viewport contract.
- AD-26 supports the exact browser-monotonic timing model, discriminated sample kinds, authenticated qualification sessions, safe server correlation, idempotent duplicates, conflicting-duplicate rejection, thresholds, and sample sufficiency required by UX.
- AD-12 supports the UX high-risk pending-command scope of user session + resource + operation family while preserving EventStore concurrency/idempotency as authority.
- AD-10 supports the UX Provider readiness triples and prohibits UI inference or override of callability.
- AD-17 defines the projections, readiness states, freshness semantics, gate inventory, browser metrics, and single-checkpoint evaluation needed by readiness and status surfaces.
- AD-5, AD-13, and AD-18 support proposal immutability, exact selected-version posting, durable timers, replay/recovery, and the explicit `submitted -> authoritative pending -> projection-confirmed terminal` truth flow.
- AD-14 and AD-22 support secret/content safety, audit-governance, retention, legal hold, encrypted export, deletion/purge, and restrictive partial-failure UX states.
- The structural seed includes the UI and UI test projects; the platform-owned host in AD-16 supplies the composition and authenticated evidence ingress needed for live UX qualification.
- FrontComposer FC-LYT, FC-TBL, FC-A11Y, FC-L10N, policy-gated navigation, FluentAccordion usage, Fluent semantic roles, and Fluent UI Blazor V5 are consistently specified across UX, architecture, epics, and persistent project context.

### Alignment Issues

No material UX↔PRD or UX↔architecture specification mismatch was found in the authoritative current documents.

Historical Epics 1–4 contain criteria that conflict with the current full-context-only and sole-**Call hexa** UX rules, but the epics replacement authority explicitly marks those stories historical, unchanged, and `mustNotImplement` where conflicting. Active Epics 5–8 use the current UX rules.

### Warnings

- UX implementation and production-like conformance remain blocked by the same external commitments acknowledged by architecture. In particular, `EXT-HOST-1` and `EXT-TOPOLOGY-1` are required for platform composition and live browser/accessibility/performance evidence; all seven critical dependencies are currently described by architecture as `Uncommitted`.
- Architecture explicitly records that the checked-in solution still contains module-owned AppHost/Aspire/ServiceDefaults projects contrary to AD-16; Stories 5.1 and 5.6 own correction and platform-hosted proof.
- Architecture explicitly records missing Provider capability high-water/effective-version conformance in current context, generation, and regeneration paths; active stories own remediation before affected UX can truthfully show callable/ready states.
- These are implementation-readiness blockers and known implementation gaps, not missing UX documentation or cross-document UX design gaps.

## Epic Quality Review

### Active Scope Reviewed

The review applies to active replacement Epics 5–8 and their declared 27 stories. Historical Epics 1–4 and the superseded former Epic 5 are not treated as executable work.

### Epic Structure Assessment

| Epic | User outcome | Independence | Finding |
| --- | --- | --- | --- |
| Epic 5 — Live Governed Setup And Honest Readiness | An Agent Administrator can configure `hexa` and receive an authoritative callability explanation. | Stands alone as a setup/readiness outcome and does not require Epics 6–8. | Compliant; Story 5.6 is oversized. |
| Epic 6 — One Safe Automatic Conversation Response | A Conversation Participant receives one safe attributed automatic response or a precise fail-closed result. | Depends only on prior Epic 5; explicitly usable without Epic 7. | Compliant epic; Stories 6.4 and 6.5 need restructuring. |
| Epic 7 — Complete Confirmation And Approval | An Approver can discover, revise, resolve, and post one selected proposal version. | Depends only on prior Epics 5–6; no later-epic dependency. | Compliant. |
| Epic 8 — Governance Operations And Release Qualification | Governance/release operators can manage retention/export/deletion/policies, calculate evidence, and inspect blockers. | Depends only on prior delivered capabilities and keeps `RQ-1` outside the backlog. | Compliant epic; Stories 8.3 and 8.6 are oversized. |

No active epic is merely a database, API, infrastructure, or model-construction milestone. Epic 5 includes technical foundation work, but its epic-level outcome is an administrator-visible setup/readiness capability. Epic 8 is operational/governance value rather than an implementation umbrella; `RQ-1` correctly aggregates completed evidence without owning missing implementation.

### Story-by-Story Assessment

| Story | Value and dependency assessment | Quality result |
| --- | --- | --- |
| 5.1 | Brownfield Integration Developer foundation; no prior/future story dependency; explicitly avoids pre-creating future entities. | Compliant foundation story. |
| 5.2 | Live, replayable Agent configuration through public UI/API; depends only on 5.1. | Compliant. |
| 5.3 | Live Provider catalog/pricing outcome; depends only on 5.1 and a declared external commitment. | Compliant. |
| 5.4 | Current tenant/Party/Approver readiness outcome; uses only 5.1–5.2 and existing read seams. | Compliant. |
| 5.5 | One authoritative readiness contract shared by API/UI; uses only 5.2–5.4. | Compliant. |
| 5.6 | Platform-hosted topology, secrets, route authorization, access control, evidence ingress, clean-checkout proof, and forbidden-host removal verification in one story. | **Major sizing concern.** |
| 5.7 | Activation/callability decision uses only 5.2–5.6; later runtime evidence does not redefine its contract. | Compliant. |
| 6.1 | Durable workflow/restart foundation uses deterministic activities and prior Epic 5 outputs; later stories add live adapters while owning their own retry/recovery cases. | Compliant technical-runtime slice, though broad. |
| 6.2 | Complete authorized context or block; ends before Provider work. | Compliant. |
| 6.3 | Two-stage safety with deterministic output fixtures; ends before live Provider generation. | Compliant. |
| 6.4 | Prepared attempt, budget ledger, live Provider idempotency, recovery, secrets, and cross-tenant budget protection; claims no forward dependency but says production call acceptance cannot mint a live admission grant until 6.5. | **Major forward-dependency and sizing defect.** |
| 6.5 | Capacity profile, shared allocator, queue/admission identity, fencing, weighted fairness, cancellation/expiry, crash recovery, and multi-replica qualification. | **Major sizing concern.** |
| 6.6 | Exactly-once membership/posting through Conversations; consumes prior completed runtime slices. | Compliant. |
| 6.7 | Functional, accessible **Call hexa** UI; later 8.6 qualifies it but does not supply its functionality. | Compliant. |
| 7.1 | Successful-generation-only proposal creation and discovery; no later-story dependency. | Compliant. |
| 7.2 | Immutable edit transition and editor/history behavior; depends only on 7.1. | Compliant. |
| 7.3 | Fresh-gate regeneration; depends only on 7.1–7.2 and declared external seams. | Compliant. |
| 7.4 | Approval/posting works from the initial version; 7.2/7.3 versions are optional inputs, not forward dependencies. | Compliant. |
| 7.5 | Independent non-posting terminal outcomes from 7.1. | Compliant. |
| 7.6 | Deterministic expiry/timer/race outcome using prior workflow/proposal foundations. | Compliant. |
| 8.1 | Retention/legal-hold outcome from prior terminal timestamps. | Compliant. |
| 8.2 | Secure export is independently usable after 8.1 and an Available secrets seam. | Compliant; externally blocked, not structurally defective. |
| 8.3 | Deletion authorization/state, cryptographic erasure/redaction, tombstone, exhaustive projection purge, partial-failure recovery, UI, and forensic proof in one story. | **Major sizing concern.** |
| 8.4 | Future-only safety/cost/governance policy publication over prior runtime contracts. | Compliant. |
| 8.5 | Versioned runtime/product metric calculators and evidence classification from already-prior source events. | Compliant but large; preserve the explicit fixture-versus-live distinction. |
| 8.6 | Every route/state's WCAG behavior, EN/FR parity, responsive matrix, browser telemetry schema/ingress, three performance calculators, and two production-like readiness gates in one story. | **Major sizing concern.** |
| 8.7 | Checkpoint-consistent inspection only; consumes prior evidence and explicitly cannot create observations or an `RQ-1` decision. | Compliant. |

### Acceptance Criteria Quality

- All active stories use explicit persona/value statements and Given/When/Then acceptance criteria.
- Happy paths, authorization failures, cross-tenant denial, stale/ambiguous dependency state, replay/idempotency, failure/recovery, safe disclosure, and evidence obligations are consistently present.
- Every active story has a structured evidence manifest with requirements, owned clauses, dependencies, evidence level, test/artifact names, one verification command, negative evidence, and current result.
- Acceptance criteria are generally specific and testable. The oversized stories bundle multiple independently verifiable systems into single Given/When/Then blocks; this is a sizing/decomposition problem rather than vagueness.
- Traceability is unusually strong: the story manifests preserve FR/NFR/UX/architecture ownership and distinguish deterministic, component, and production-like evidence.

### Dependency And Data-Timing Review

- Epic order is valid: Epic 5 stands alone; Epic 6 uses Epic 5; Epic 7 uses Epics 5–6; Epic 8 uses prior outputs only.
- Within-epic dependencies are backward-only except for Story 6.4's explicit dependence on Story 6.5 for production admission despite declaring `Forward dependencies: None`.
- No circular dependency was found.
- External dependencies are named and status-gated rather than silently assumed.
- The architecture specifies no starter template, so no missing starter-template story exists.
- This is brownfield integration work; Story 5.1 provides the early source/package/CI/boundary correction, and later stories explicitly integrate EventStore, Conversations, Parties, Tenants, FrontComposer, Dapr Workflow, and the platform-owned host.
- The module is event-sourced; there is no upfront database/table-creation story. Story 5.1 explicitly prohibits pre-creating unrelated future entities, and each aggregate/projection is introduced with the capability that needs it.

### Critical Violations

No epic-level critical violation was found. There is no technical-only active epic, no circular dependency, and no Epic N dependency on Epic N+1.

### Major Issues And Remediation

1. **Story 6.4 contains a concealed forward dependency on Story 6.5.** Its dependency section says `Forward dependencies: None`, while the same section states that production call acceptance cannot mint a live grant until Story 6.5 supplies the shared allocator. The story's primary outcome also invokes the committed Provider adapter under an injected trusted grant, so it proves a test-only seam rather than a production-usable generation outcome.

   **Remediation:** either move the allocator/admission contract before live Provider invocation, or narrow 6.4 to prepared-attempt descriptor + atomic budget reservation/reconciliation behind an admission port. Move live transport/idempotent recovery to a subsequent story after the real allocator exists. Update the dependency topology and demonstrable outcome accordingly.

2. **Several stories are too large for reliable independent completion.** The clearest cases are 5.6, 6.4, 6.5, 8.3, and 8.6.

   **Remediation:** split them along independently testable outcome boundaries:

   - 5.6: platform-host consumption; secrets/access-control isolation; production-like topology evidence.
   - 6.4: prepared descriptor/ledger; Provider idempotency/outcome recovery after real admission.
   - 6.5: capacity profile/contract; durable admission/fencing/queue; weighted-fairness and multi-replica recovery qualification.
   - 8.3: deletion request/authorization state; cryptographic protection/tombstone; projection purge/recovery/forensic proof.
   - 8.6: NFR-13 accessibility/localization/responsive conformance; NFR-14 browser telemetry/metric implementation; production-like `LR-UI-CONFORMANCE` and `LR-UI-PERFORMANCE` qualification.

### Minor Concerns

- Two literal standalone `+` lines remain before Epic 6 and Epic 7. Remove them to avoid Markdown/parser noise.
- Historical Epics 1–4 remain in the same document as active work and contain criteria superseded by current UX/PRD rules. The replacement authority is explicit, but automation must honor epic status and `mustNotImplement`. Prefer a mechanically enforced executable-status field per story or archive the historical body outside the active backlog view.
- The planned `pwsh ./eng/verify-story-*.ps1` commands are specific but do not yet exist in the current `eng/` tree. That is consistent with backlog status, but each script must be created and made executable as part of its story before the story can claim verified completion.

### Best-Practices Result

- User-value epics: 4/4 compliant.
- Epic independence: 4/4 compliant.
- FR traceability: 28/28 covered.
- Active stories reviewed: 27/27.
- Story-level forward-dependency defects: 1.
- Stories requiring decomposition before `ready-for-dev`: 5.
- Acceptance-criteria format: consistently BDD and testable, subject to the sizing findings above.

## Summary and Recommendations

### Overall Readiness Status

**NOT READY** for Phase 4 implementation.

The product specification is strong: the PRD is complete, declared FR coverage is 100%, UX and architecture are aligned, and active epics are user-outcome oriented with unusually strong evidence manifests. Readiness nevertheless fails because every critical external dependency is still `Uncommitted`, the architecture records two material current implementation non-conformities, and the active backlog contains one forbidden forward dependency plus five stories that require decomposition before `ready-for-dev`.

This is not a launch-readiness judgment. `RQ-1` remains the later operational READY/NOT READY gate after implementation. This assessment determines that the current planning package cannot safely begin its active Phase 4 sequence as written.

### Critical Issues Requiring Immediate Action

1. **All seven critical external dependencies are `Uncommitted`:** `EXT-CONV-AI-1`, `EXT-HOST-1`, `EXT-PROVIDER-1`, `EXT-SAFETY-1`, `EXT-TOKEN-1`, `EXT-SECRETS-1`, and `EXT-TOPOLOGY-1`. Under FR-21 and the architecture's dependency rules, a missing accepted target or executable compatibility command blocks every consuming story from `ready-for-dev`. Story 5.1 is itself blocked by `EXT-HOST-1`, so the active dependency chain has no safe starting point.

2. **Current implementation contradicts the architecture's platform boundary.** AD-16 records that module-owned `Hexalith.Agents.AppHost`, `Hexalith.Agents.Aspire`, and `Hexalith.Agents.ServiceDefaults` remain checked in even though the conformant domain module must consume a platform-owned host. Stories 5.1 and 5.6 can correct this only after the host commitment is concrete.

3. **Current runtime paths do not meet AD-10.** Architecture records that context, generation, and regeneration do not maintain the durable Provider capability high-water mark or distinct `EffectiveProviderCapabilityVersion`, and do not perform every required current-readiness check. Affected setup/runtime claims cannot be considered conformant until remediated.

4. **Story 6.4 has a prohibited forward dependency.** It declares no forward dependency while stating that production call acceptance cannot create a live admission grant until Story 6.5. Its live Provider outcome is therefore proved through an injected test grant rather than a production-usable admission boundary.

5. **Five stories are oversized:** 5.6, 6.4, 6.5, 8.3, and 8.6. Each combines multiple independently implementable subsystems or qualification gates, raising completion, review, sequencing, and rollback risk.

### Recommended Next Steps

1. Complete the external dependency register for all seven records with the PRD-required owner, repository, artifact, immutable target, integration date, compatibility contract/command, evidence level, accepted status, and consuming-story mapping. Do not advance a consuming story until its required status and command actually qualify.

2. Rework Epic 6 sequencing. Narrow Story 6.4 to prepared-attempt and atomic budget-ledger behavior behind an admission port, establish the real allocator/admission/fence before live Provider transport, and move Provider invocation/outcome recovery after that boundary exists.

3. Split Stories 5.6, 6.4, 6.5, 8.3, and 8.6 along the outcome boundaries documented in the quality review. Update the declared 27-story count, dependency topology, FR/UX/architecture mappings, evidence manifests, and sprint tracking atomically.

4. Once `EXT-HOST-1` is committed, implement and verify the AD-16 boundary correction: remove module-owned hosting projects and references, preserve the DomainService/UI package boundary, and prove package-mode and platform-hosted clean-checkout composition.

5. Implement the AD-10 capability high-water/effective-version contract and fresh readiness/limit revalidation across context build, initial generation, and regeneration. Add lower/equal/higher version, disabled/unconfigured, retry-fingerprint, and evidence-provenance tests.

6. Create each planned `eng/verify-story-*.ps1` lane as part of its owning story. Make historical/non-executable story status machine-enforceable, remove the two stray `+` lines, and prevent automation from scheduling superseded criteria.

7. Rerun implementation readiness after dependency commitments and epic/story restructuring. Only then begin Phase 4, starting from the corrected Story 5.1 foundation.

### Final Note

This assessment identified **18 actionable findings across four categories**:

- 7 uncommitted critical external dependencies.
- 2 architecture-recorded implementation non-conformities.
- 6 backlog-quality defects: 1 forward dependency and 5 oversized stories.
- 3 documentation/verification hygiene concerns.

The planning package's strengths should be preserved during remediation: 28/28 FR coverage, authoritative PRD/UX/architecture alignment, explicit fail-closed dependency handling, rigorous BDD criteria, and evidence-level discipline. Address the blocking dependency and backlog-structure issues before proceeding to implementation.

**Assessment date:** 2026-08-04  
**Assessor:** Codex — BMAD Implementation Readiness workflow
