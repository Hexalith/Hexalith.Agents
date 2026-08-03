---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
status: NOT_READY
assessor: Codex
completedAt: 2026-08-02
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
---

# Implementation Readiness Assessment Report

**Date:** 2026-08-02
**Project:** agents

## Document Inventory

### PRD

- `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md`
- `_bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/addendum.md`

### Architecture

- `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md`
- `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/IMPLEMENTATION-CONVENTIONS.md`

### Epics and Stories

- `_bmad-output/planning-artifacts/epics.md`

### UX Design

- `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md`

### Excluded

- `_bmad-output/planning-artifacts/epic-5-superseded-2026-08-01.md` — explicitly superseded.

No required document category is missing and no whole-versus-sharded duplicate conflict was found. The PRD, architecture, and UX document sets do not contain conventional `index.md` files.

## PRD Analysis

### Functional Requirements

#### FR-1: Configure hexa

Agent Administrators can create or enable `hexa` with a stable Agent identity, display name, description, Agent Instructions, lifecycle state, and tenant scope.

- The system prevents activation when required Agent fields are missing or invalid.
- The system exposes the current Agent configuration through the admin UI and API/client contracts.
- The system records configuration changes in Audit Evidence with actor, timestamp, prior value where safe to expose, and new value.

#### FR-2: Link Agent To Party Identity

Agent Administrators can provision or link the Agent's Party identity so `hexa` appears as a known AI participant when it posts to a Conversation.

- An active Agent has exactly one Party identity.
- The system rejects posting an Agent Response when the Agent Party identity is missing, disabled, ambiguous, or unauthorized for the Source Conversation.
- Conversation Messages posted by `hexa` are attributable to the Agent's Party identity, not to the caller or a generic system account.

#### FR-3: Manage Agent Lifecycle

Agent Administrators can activate, disable, and inspect `hexa` lifecycle state.

- Disabled Agents cannot be called from Conversations.
- Disabling an Agent does not delete existing Audit Evidence, Proposed Agent Replies, or Conversation Messages.
- Lifecycle changes are auditable and visible through admin UI and API/client contracts.

#### FR-4: Manage Global Providers Aggregate

Authorized administrators can configure the Global Providers Aggregate with provider records, model options, enabled/disabled state, and provider capability metadata needed for Agent selection.

- Disabled providers or models cannot be selected for new Agent configuration.
- Existing Agents using a disabled provider/model cannot be activated or called until reconfigured. A documented migration state may allow temporary read-only inspection.
- Provider configuration changes are auditable without exposing secrets in logs, API responses, UI, or Audit Evidence.

#### FR-5: Select Provider And Model Per Agent

Agent Administrators can select a Provider and model for `hexa` from the Global Providers Aggregate.

- The system validates that the selected Provider and model are enabled and usable before Agent activation.
- The system stores enough provider/model identity in Audit Evidence to explain which Provider and model produced each generated version.
- Changing provider/model selection affects future Agent Calls only and does not rewrite historical proposal or response evidence.

#### FR-6: Configure Response Mode

Agent Administrators can configure `hexa` for Automatic Response Mode or Confirmation Response Mode.

- Automatic Response Mode posts successful Agent Responses directly to the Source Conversation after authorization and generation complete.
- Confirmation Response Mode creates Proposed Agent Replies outside the Conversation and never posts unapproved generated content.
- Response mode changes affect future Agent Calls only.

#### FR-7: Configure Approver Policy

Agent Administrators can define all Approvers through the Agent's Approver Policy, including policy sources such as the Conversation owner, the caller, predefined Parties, or tenant roles.

- The system authorizes proposal edit, regeneration, approval, rejection, abandonment, and expiry-resolution actions using the Approver Policy.
- The system rejects approval actions by Parties not authorized by the current policy for the proposal.
- The system exposes which configured policy source authorized the Approver according to a defined disclosure category: user-visible, operator-only, redacted, or omitted.
- The proposal records the policy basis used for each approval-related decision.
- API/client contracts and admin UI use the same disclosure category for the same approval-policy basis.

#### FR-8: Call Agent From Conversation

Authorized Conversation Participants can explicitly call `hexa` from a Source Conversation with a user prompt or request.

- Agent Calls require Source Conversation access and Agent call permission.
- Unauthorized calls fail before Provider invocation.
- Every Agent Call records caller, Agent, Source Conversation, request timestamp, and response mode.

#### FR-9: Build V1 Conversation Context

The system supplies the Agent with Conversation Context according to the configured Conversation Context Policy.

- V1 Agent generation uses Conversation Context only.
- V1 generation does not include long-term memory, project content, folder content, external tool output, or external-channel content.
- When the full Source Conversation fits the selected Provider/model's safe context budget, V1 generation uses the full Source Conversation.
- When the Source Conversation exceeds the selected Provider/model's safe context budget, the call fails closed before Provider invocation; V1 never truncates, summarizes, windows, or otherwise reduces the Conversation.
- Agent Calls record that complete context was used or that the call was blocked, the Conversation Context Policy version or equivalent identifier, and enough context metadata for audit without leaking unrelated tenant data.
- If the complete Conversation Context cannot be loaded or sent safely, the Agent Call fails closed and creates neither Provider work, a Proposed Agent Reply, nor a Conversation Message.

#### FR-10: Handle Generation Failure

The system handles Provider failures, timeout, disabled provider/model state, invalid context, Content Safety Policy failures, and policy failures without posting incomplete or unsafe Agent Responses.

- Failed generation creates status and Audit Evidence visible to authorized administrators or callers.
- Failed generation does not create a Conversation Message.
- Failed generation in Confirmation Response Mode does not create a Proposed Agent Reply. Any failed or incomplete generated content retained for authorized audit is stored only in a separate, non-approvable failure record.

#### FR-11: Post Automatic Response

When `hexa` is configured for Automatic Response Mode, the system posts successful generated content to the Source Conversation as a Conversation Message attributed to the Agent's Party identity.

- The posted message references the Agent Call or equivalent trace identifier.
- The posted message does not appear as authored by the caller.
- The system records Audit Evidence linking caller, Agent, Provider/model, Source Conversation, generated content, and posted Conversation Message.

#### FR-12: Prevent Automatic Posting When Policy Fails

The system prevents automatic posting when authorization, Agent lifecycle, Provider/model, Party identity, Source Conversation access, Conversation Context Policy, Content Safety Policy, or generation status is invalid.

- No Conversation Message is created when a required policy check fails.
- No Conversation Message is created when generated content fails the active Content Safety Policy.
- The failure reason is visible through authorized status surfaces without leaking secrets or unrelated tenant data.
- Audit Evidence distinguishes authorization, context policy, content safety, Provider/runtime, and posting failures.

#### FR-13: Create Proposed Agent Reply

When `hexa` is configured for Confirmation Response Mode, successful generation creates a Proposed Agent Reply linked to the Source Conversation and Agent Call.

- A Proposed Agent Reply is not a Conversation Message.
- A Proposed Agent Reply records caller, Agent, Source Conversation, generated version, Provider/model, response mode, and current proposal state.
- Authorized Approvers can discover pending proposals requiring their action.
- V1 uses in-product pending-proposal visibility only for authorized Approvers, including a pending count, a queue, and a Conversation status entry.
- Email, push, and external-channel proposal notifications are not included in V1.

#### FR-14: Preserve All Proposal Versions

The system preserves every generated, edited, and regenerated content version for each Proposed Agent Reply.

- Editing a proposal creates a new Versioned Proposal Content record or equivalent immutable version entry.
- Regeneration creates a new generated version without deleting prior generated or edited versions.
- Approval identifies exactly which version was approved and posted.

#### FR-15: Edit Proposed Reply

Authorized Approvers can edit Proposed Agent Reply content before approval.

- Only authorized Approvers can edit proposal content.
- Edits preserve the prior version and author of the edit.
- Edited content remains outside the Conversation until approved.

#### FR-16: Regenerate Proposed Reply

Authorized Approvers can request regeneration of a Proposed Agent Reply before approval.

- Regeneration uses the same Source Conversation and Agent configuration unless the system records an explicit configuration version change.
- Regeneration preserves prior versions and creates a new generated version.
- Regeneration is blocked after a proposal reaches a terminal state.

#### FR-17: Approve Proposed Reply

Authorized Approvers can approve a selected proposal version, causing it to be posted to the Source Conversation as `hexa`.

- Approval posts exactly the approved version and no other proposal version.
- The Conversation Message is attributed to the Agent's Party identity.
- Audit Evidence links the approved version, Approver, approval timestamp, and posted Conversation Message.

#### FR-18: Reject, Abandon, Or Expire Proposed Reply

Authorized Approvers or system policy can move a Proposed Agent Reply to rejected, abandoned, or expired terminal states.

- Terminal proposals cannot be approved or posted.
- Terminal proposals preserve all generated and edited versions for audit.
- The default expiry duration is 24 hours. An Agent Administrator may configure a duration from 1 hour through 30 days for future proposals only.
- A durable Dapr Workflow timer moves a non-terminal proposal to `Expired` at or after its stored `ExpiresAt`; an expiry policy change never changes an existing proposal's `ExpiresAt`.
- Expiry behavior and the stored `ExpiresAt` are visible through admin UI and API/client contracts.

#### FR-19: Enforce Tenant Isolation

The system enforces tenant isolation across Agent configuration, Provider selection, Agent Calls, Conversation Context, Proposed Agent Replies, Conversation posting, and Audit Evidence.

- A Party from one tenant cannot call, inspect, approve, or post Agent responses for another tenant.
- Provider/model configuration and Agent configuration cannot leak across tenant boundaries unless explicitly platform-scoped and authorized.
- Audit/status queries return only tenant-authorized records.

#### FR-20: Enforce Role And Policy Authorization

The system enforces authorization for Agent administration, provider administration, Agent calling, proposal discovery, editing, regeneration, approval, rejection, abandonment, posting, and audit inspection.

- Authorization failures occur before Provider invocation or Conversation posting.
- The same authorization rules apply through admin UI and API/client contracts.
- Authorization decisions are auditable at a level sufficient to explain denial or approval without leaking sensitive content.

#### FR-21: Fail Closed On Dependency Uncertainty

The system fails closed when required Party, Conversation, Provider, Agent, tenant access, or approval policy state is missing, stale, ambiguous, disabled, or unavailable.

- Missing or stale Conversation access prevents Agent Calls and approval posting.
- Missing or disabled Agent Party identity prevents posting.
- Missing Provider/model state prevents generation.
- A critical external dependency is not implementation-ready until its external dependency register entry satisfies every commitment field defined in PRD §8.
- An `Uncommitted` dependency, missing target, or missing compatibility verification command blocks every consuming story from `ready-for-dev`.

#### FR-22: Provide Admin UI

The admin UI allows authorized administrators to manage Global Providers Aggregate entries, configure `hexa`, inspect lifecycle state, configure response and approver policy, and view Agent operation and proposal status.

- Admin UI actions enforce the same authorization rules as API/client contracts.
- Admin UI never exposes Provider secrets.
- Admin UI clearly distinguishes active, disabled, invalid, pending proposal, failed call, and expired proposal states.
- Admin UI satisfies the accessibility, localization, responsive safety, and interaction-performance requirements in NFR-13 and NFR-14.

#### FR-23: Provide API And Client Contracts

The system exposes stable API/client contracts for Provider administration, Agent configuration, Agent Calls, proposal workflow, status inspection, and audit inspection.

- API/client contracts do not require callers to use raw EventStore, internal aggregate, internal projection, or provider SDK details.
- API/client contracts return structured success and error results suitable for automation.
- JSON object evolution is additive within V1.
- Public enums define `Unknown = 0`; new values may be added, but an existing value's meaning cannot be reused.
- No public member or enum value is removed, renamed, or semantically reused within V1.
- A breaking public change requires a new major package/API version and package-consumer compatibility tests.

#### FR-24: Capture Agent Audit Evidence

The system captures Audit Evidence for Agent configuration, Provider/model configuration, Agent Calls, generation attempts, proposal versions, edits, regenerations, approvals, rejections, abandonments, expirations, automatic posts, and final Conversation Messages.

- Every posted Agent Response can be traced back to caller, Agent, Source Conversation, Provider/model, generated content, and approval path where applicable.
- Every Proposed Agent Reply preserves all Versioned Proposal Content.
- Audit Evidence records the Content Safety Policy decision, Conversation Context Policy behavior, and policy/version identifiers where available.
- Audit Evidence is queryable by authorized users without exposing unrelated tenant data or Provider secrets.

#### FR-25: Expose Operational Status

The system exposes status for Agent readiness, Provider/model readiness, recent Agent Call outcomes, proposal queues, generation failures, approval completion, and posting outcomes.

- Authorized administrators can identify whether `hexa` is callable for a tenant.
- Authorized administrators can distinguish configuration errors, authorization failures, context policy failures, content safety failures, Provider failures, generation failures, pending approvals, and posting failures.
- Status surfaces support launch monitoring of adoption and approval workflow metrics.

#### FR-26: Configure Content Safety And Prompt Policy

Authorized administrators or release operators can define the active Content Safety Policy for `hexa`.

- `hexa` cannot be enabled for production or production-like launch validation without an active Content Safety Policy.
- The Content Safety Policy defines prompt constraints, blocked or restricted output categories, safety failure handling, and audit treatment.
- Content Safety Policy changes are auditable and affect future Agent Calls only.
- Automatic Response Mode and Confirmation Response Mode use the same active Content Safety Policy unless a stricter mode-specific policy is configured.
- The active policy always blocks child sexual abuse/exploitation; credible threats or instructions for imminent serious harm; encouragement or instruction for suicide/self-harm; credential theft, malware deployment, or unauthorized compromise; secrets/tokens/private credentials; cross-tenant or unauthorized personal/Conversation data; and attempts to bypass tenant, authorization, audit, retention, or safety controls.
- Restricted hate, harassment, sexual, violent, illegal-activity, or sensitive-personal content requires an explicitly permitted tenant use case and Confirmation Response Mode.
- A policy version cannot weaken a retry already in progress; each retry uses policy at least as restrictive as the initial attempt.

#### FR-27: Enforce Safety Before Provider And Conversation Side Effects

The system applies Content Safety Policy to the prompt and complete authorized Conversation Context before Provider invocation, then applies it to generated output before that output becomes a Conversation Message or an approvable Proposed Agent Reply.

- Generated content that fails Content Safety Policy cannot be posted automatically.
- Generated content that fails Content Safety Policy cannot become an approvable Proposed Agent Reply.
- Safety failures create authorized status and Audit Evidence without exposing unsafe content in surfaces where policy forbids display.
- Approvers cannot override a Content Safety Policy failure.
- Prompt and complete Conversation Context pass the active policy before Provider invocation, and generated output passes the active policy before any proposal or Conversation side effect.

#### FR-28: Define Launch Readiness Controls

V1 launch readiness requires the fixed metric thresholds, latency targets, full-context behavior, cost controls, audit governance, NFR-11 through NFR-14, external dependency commitments, and the normative evidence authority in PRD §11.

- Controlled production-like qualification may collect live evidence only after Content Safety Policy, Conversation Context Policy, cost controls, audit governance, and the required external dependency commitments are active and recorded. Qualification access does not authorize production enablement.
- Production enablement remains blocked until `RQ-1` records READY from the required live evidence and launch metrics.
- Per-tenant monthly and per-call cost caps are hard enforcement boundaries: 80% emits a warning, 100% fails closed, and atomic reservation plus reconciliation prevents concurrent overspend. The system reserves the maximum estimated attempt cost before Provider invocation, reconciles actual usage, releases any unused reservation only after confirming that no usage occurred, and reuses the same reservation for eligible retries. Missing pricing or budget state blocks invocation; reporting-only monitoring is insufficient.
- Automatic accepted-call-to-post latency is p95 at most 60 seconds and p99 at most 120 seconds. Confirmation accepted-call-to-proposal latency uses the same thresholds; approval-to-post latency is p95 at most 10 seconds and p99 at most 30 seconds.
- Pre-Provider authorization, policy, budget, and context rejections complete at p95 at most 2 seconds. Each performance gate uses at least 30 production-like executions.
- Production readiness requires live Evidence Levels 4 and 5 as defined in PRD §11; lower levels, skips, placeholders, or conditional results cannot independently establish launch readiness.

**Total functional requirements: 28**

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

#### NFR-11: Availability And Recovery

EventStore business state has RPO 0. Restart or replay cannot duplicate Provider attempts, proposal versions, timers, reservations, or Conversation posts. A production-like recovery exercise restores interaction processing within 15 minutes and preserves every terminal decision.

#### NFR-12: Capacity And Backpressure

Before enablement, each environment records numeric per-tenant and system-wide concurrency, queue-depth, and backpressure limits. Exceeding a limit queues or rejects with a safe typed outcome before Provider invocation. Cross-tenant fairness and cost caps remain enforced. The release profile and numeric limits are visible in the readiness registry, not hidden in host configuration.

#### NFR-13: Accessible, Localizable, Responsive UI

The final UX spines are binding launch authority. Interactive V1 surfaces meet WCAG 2.2 AA behavior, use whole-string localization with English/French key parity, and fail closed for high-impact actions when the viewport cannot present required context.

#### NFR-14: UI Interaction Performance

In the production-like profile, an authorized page reaches a usable non-loading state at p95 ≤ 2.5 seconds; a submitted command renders an authoritative pending acknowledgement at p95 ≤ 500 ms; a projection-visible terminal change renders and is announced at p95 ≤ 2 seconds. Each gate uses at least 30 executions and returns `InsufficientEvidence`, not pass, when timestamps or samples are missing.

**Total non-functional requirements: 14**

### Additional Requirements

#### Scope and product constraints

- V1 exposes only the named Agent `hexa`; generalized internal structures are permitted.
- Invocation is exclusively the Conversation-owned **Call hexa** action. Mentions, commands, ambient triggers, project/folder triggers, external channels, tools, long-term memory, retrieval, and agent-to-agent orchestration are outside V1.
- V1 uses only the complete authorized Source Conversation. If it cannot be loaded or fit safely, processing fails before Provider invocation; truncation, summarization, and windowing are prohibited.
- Unapproved generated content is never a Conversation Message, and proposal history is immutable across editing and regeneration.
- Proposal expiry defaults to 24 hours, is configurable from 1 hour through 30 days for future proposals, and is executed through a durable timer against the stored `ExpiresAt`.
- Dapr Workflow owns execution, but Hexalith Agents owns durable proposal state and proposal read models.

#### External dependency constraints

- A critical dependency is implementation-ready only when its register entry contains a named owner, owning repository, required artifact, target version or commit, target integration date, compatibility contract/test and verification command, required Evidence Level, accepted status, and consuming stories.
- `Uncommitted` status, a missing target, or a missing compatibility command blocks every consuming story from `ready-for-dev`.
- The initial critical set is `EXT-CONV-AI-1`, `EXT-HOST-1`, `EXT-PROVIDER-1`, `EXT-SAFETY-1`, `EXT-TOKEN-1`, `EXT-SECRETS-1`, and `EXT-TOPOLOGY-1`.
- Conversations must provide AI-agent participant membership and final message posting with deterministic idempotency, typed conflicts, and cross-tenant denial; Agents must not write Conversation streams directly.
- Parties is authoritative for Agent and human Party identity; tenant state and authorization fail closed when missing, stale, ambiguous, disabled, or unavailable.

#### Data governance and retention

- Audit Evidence is tenant-scoped and access-controlled, preserves every generated, edited, and regenerated version, and links calls and posts to caller, Agent, Conversation, Provider/model, response mode, and approval path.
- Sensitive Agent content is retained for 365 days after terminal state unless legal hold applies.
- Authorized export must be tenant-scoped, encrypted, time-limited, manifested, and audited.
- EventStore history is never rewritten. Expiry or approved deletion cryptographically erases or redacts protected content, purges affected projections, and retains only a support-safe non-content tombstone after restrictive confirmation.
- Provider secrets and credentials never appear in logs, API/client output, UI, status, or Audit Evidence.

#### Public-contract constraints

- Public capabilities cover Provider and Agent administration, invocation, proposal workflow, status, and audit without exposing EventStore or provider SDK internals.
- V1 JSON evolution is additive. Public enums use `Unknown = 0`; members and values cannot be removed, renamed, or semantically reused.
- Breaking public changes require a new major package/API version and package-consumer compatibility tests.
- Admin UI and API/client contracts apply equivalent capability and authorization outcomes.

#### Evidence and release constraints

- Evidence Levels are normative: Level 1 contract/structure, Level 2 pure domain/unit behavior, Level 3 fail-closed deferred seam, Level 4 live component integration, and Level 5 cross-system production-like evidence.
- Production-like readiness requires live Levels 4 and 5 for runtime behavior, authorization, tenant isolation, Provider integration, content safety, Conversations integration, audit behavior, and topology.
- Each qualification metric requires a versioned measurement contract defining source events and timestamps, calculation method, sample/window/cohort rules, late/missing-data handling, and `InsufficientEvidence` conditions.
- Deterministic fixtures may prove calculator behavior but cannot prove live attainment. Final production enablement requires `RQ-1` to record READY from live evidence and launch metrics.
- Success thresholds include at least one qualified launch tenant; at least 20% Conversation adoption across at least 50 eligible Conversations in a rolling 30-day cohort; and at least 95% of proposals terminal within 26 hours, with expiry ≤ 20%, posting failure ≤ 2%, human resolution ≥ 70%, and audit completeness = 100%.

### PRD Completeness Assessment

The PRD is highly complete for product scope and requirement extraction: it has contiguous FR-1 through FR-28 and NFR-1 through NFR-14, explicit V1 boundaries, resolved binding decisions, measurable latency/recovery/cost targets, fail-closed dependency rules, compatibility rules, and normative evidence levels.

Its remaining readiness risks are deliberately downstream-owned rather than hidden: operational Evidence Level manifests still need environment, producer/approver, artifact, freshness, verification, and pass/fail definitions; the metric measurement contracts still need concrete formulas and cohort semantics; the privacy-safe causal audit envelope remains underspecified; canonical companion-artifact bindings require verification; and the preserved success metrics do not directly measure response utility or overall call reliability. These items must be checked against Architecture, UX, Epics, and the readiness/dependency registers before implementation can be declared ready.

## Epic Coverage Validation

The current epic document explicitly marks Epics 1–4 as completed historical evidence and identifies replacement Epics 5–8 as the active forward implementation authority. The matrix below therefore uses the active FR Coverage Map rather than treating superseded or historical work as current conformance.

### Coverage Matrix

| FR | PRD requirement | Active epic coverage | Status |
| --- | --- | --- | --- |
| FR-1 | Configure `hexa` | Epic 5 | Covered |
| FR-2 | Link Agent to exactly one Party identity | Epics 5 and 6 | Covered |
| FR-3 | Manage Agent lifecycle | Epic 5 | Covered |
| FR-4 | Manage Global Providers Aggregate | Epics 5 and 8 | Covered |
| FR-5 | Select Provider and model per Agent | Epics 5 and 6 | Covered |
| FR-6 | Configure future-only response mode | Epic 5 | Covered |
| FR-7 | Configure, resolve, and audit Approver Policy | Epics 5 and 7 | Covered |
| FR-8 | Call Agent from the Conversation-owned entry | Epic 6 | Covered |
| FR-9 | Use complete authorized Conversation Context or block | Epic 6 | Covered |
| FR-10 | Handle generation failure without proposal/message leakage | Epic 6 | Covered |
| FR-11 | Post one automatic response as `hexa` | Epic 6 | Covered |
| FR-12 | Prevent automatic posting when any required gate fails | Epic 6 | Covered |
| FR-13 | Create and expose a pending Proposed Agent Reply | Epic 7 | Covered |
| FR-14 | Preserve every proposal version immutably | Epic 7 | Covered |
| FR-15 | Edit a proposed reply without overwriting history | Epic 7 | Covered |
| FR-16 | Regenerate under current gates while preserving history | Epic 7 | Covered |
| FR-17 | Approve and post exactly one selected version | Epic 7 | Covered |
| FR-18 | Reject, abandon, or deterministically expire proposals | Epics 7 and 8 | Covered |
| FR-19 | Enforce tenant isolation across every surface and side effect | Epics 5–8 | Covered |
| FR-20 | Enforce role and policy authorization before side effects | Epics 5–8 | Covered |
| FR-21 | Fail closed on dependency uncertainty and block story readiness | Epics 5–8 | Covered |
| FR-22 | Deliver authorized FrontComposer/Fluent administration and workflow UI | Epics 5–8 | Covered |
| FR-23 | Publish stable additive API/client contracts | Epics 5–8 | Covered |
| FR-24 | Capture tenant-scoped durable Audit Evidence | Epics 5–8 | Covered |
| FR-25 | Expose authoritative readiness and operational status | Epics 5–8 | Covered |
| FR-26 | Publish and operate versioned Content Safety Policy | Epics 6 and 8 | Covered |
| FR-27 | Enforce fresh, no-weaker safety before every relevant side effect | Epics 6 and 7 | Covered |
| FR-28 | Implement launch controls and bounded evidence; keep `RQ-1` outside backlog | Epics 5–8 | Covered |

### Missing Requirements

No PRD Functional Requirement is absent from the active epic coverage map. No Functional Requirement identifier appears in the epics without a corresponding PRD requirement.

### Coverage Statistics

- Total PRD FRs: 28
- FRs claimed covered by active epics: 28
- Missing FRs: 0
- Epic-only FR identifiers: 0
- Coverage: 100%

## UX Alignment Assessment

### UX Document Status

Found and fully reviewed:

- `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md`
- `_bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md`

The UX spines are final and dated 2026-08-02. They explicitly source the current PRD, architecture spine, launch-readiness register, and approved change proposal.

### UX ↔ PRD Alignment

The current UX spines align strongly with the PRD:

- PRD UJ-1 through UJ-4 are preserved as configuration, automatic invocation, confirmation/approval, and API integration flows.
- UX adds UJ-5 for production-like launch governance, but it is traceable to FR-22, FR-25, FR-28, NFR-10 through NFR-14, and the PRD's data-governance and release-qualification rules rather than adding unrelated product scope.
- The Conversation-owned **Call hexa** action remains the sole V1 invocation entry.
- Complete authorized Conversation Context is read-only and full-or-blocked; the UX offers no truncation, summary, windowing, or retrieval control.
- Lifecycle `active` is separate from authoritative callability.
- `submitted -> authoritative pending -> projection-confirmed terminal` and `approved -> posting pending -> posted` prevent optimistic or partial states from being shown as success.
- Failed generation creates only a separate non-approvable failure record and never a proposal, proposal version, queue row, notification, editor, approval action, or Conversation Message.
- All generated, edited, and regenerated proposal versions remain distinct and immutable; only the selected approved version may post.
- Provider secrets, raw payloads, PII, unsafe content, stack traces, and unrelated tenant data are excluded from visible and accessible output.
- The UX implements the PRD's 365-day retention, legal hold, encrypted export, deletion/purge, cost, safety, evidence, and launch-readiness requirements.
- WCAG 2.2 AA behavior, whole-string English/French parity, fail-closed responsive behavior, and exact NFR-14 thresholds and sample rules are preserved.

No material UX requirement contradicts the current PRD.

### UX ↔ Architecture Alignment

The architecture contains direct support for the UX:

- AD-15 binds API/UI parity and FrontComposer composition through shared public contracts.
- AD-10 defines the exact `ProviderReadinessResult` consumed without UI inference.
- AD-12 defines the resource-and-operation-family pending lock and authoritative completion behavior.
- AD-17 defines readiness states, freshness, gate inventory, projections, and checkpoint semantics consumed by the launch-readiness UI.
- AD-22 supports retention, hold, export, deletion, and restrictive completion.
- AD-25 binds WCAG 2.2 AA, localization parity, FrontComposer/Fluent V5 inheritance, and restrictive viewport behavior.
- AD-26 defines the browser-monotonic `PageUsability`, `AuthoritativePending`, and `TerminalRenderAnnouncement` samples with the exact NFR-14 thresholds and `InsufficientEvidence` rules.
- The authoritative projection inventory includes setup readiness, provider capability/pricing, interaction status, proposal detail/history/queues/counts, audit, governance, runtime metrics, browser metrics, and product metrics required by the UX surfaces.

No unresolved semantic contradiction was found between the current UX spines and the current architecture spine.

### Alignment Issues And Warnings

1. **Platform host and topology are specified but not committed.** UX composition and production-like browser evidence depend on `EXT-HOST-1` and `EXT-TOPOLOGY-1`; the architecture states that all seven external prerequisites are currently `Uncommitted`. UI implementation or component tests may progress only where a story's declared dependency permits, while live UX conformance and performance evidence remain blocked.

2. **The checked-in hosting shape is explicitly non-conformant.** Architecture records that module-owned `Hexalith.Agents.AppHost`, `Hexalith.Agents.Aspire`, and `Hexalith.Agents.ServiceDefaults` still exist even though the conformant UX/runtime model requires platform-owned hosting. Stories 5.1 and 5.6 own correction and proof.

3. **Provider readiness semantics are architecturally defined but not fully implemented.** AD-10 records missing durable capability high-water, effective-version, and fresh readiness enforcement in current context/generation/regeneration paths. UX components such as `provider-status-badge`, `agent-readiness-badge`, and launch readiness cannot claim live conformance until this gap is closed.

4. **Live UI evidence remains intentionally unavailable.** `LR-UI-CONFORMANCE` and `LR-UI-PERFORMANCE` require the versioned `EXT-TOPOLOGY-1` fixture, authenticated qualification sessions, all completed interactive routes/states, and at least 30 qualifying samples per timing kind. Missing or unavailable prerequisites must render `InsufficientEvidence`, not Pass.

## Epic Quality Review

### Review Scope

All four active replacement epics and all 27 active stories were reviewed. Epics 1–4 were treated as explicitly marked historical evidence, not current implementation authority. The non-estimated `RQ-1` release gate was correctly treated as outside the executable backlog.

### Epic Structure Assessment

| Epic | User-value focus | Independence | Quality result |
| --- | --- | --- | --- |
| Epic 5 — Live Governed Setup And Honest Readiness | Clear administrator outcome: configure `hexa` and understand authoritative callability | Does not depend on Epics 6–8 | Pass with sizing concern in Story 5.6 |
| Epic 6 — One Safe Automatic Conversation Response | Clear participant outcome: one attributed response or precise fail-closed result | Does not require Epic 7 | Fails strict independence because Stories 6.4 and 6.5 form a latent forward dependency |
| Epic 7 — Complete Confirmation And Approval | Clear Approver outcome: create, revise, resolve, and post exactly one proposal version | Uses prior Epics 5–6 only; no later-epic dependency | Pass |
| Epic 8 — Governance Operations And Release Qualification | Clear governance/release-operator outcomes; `RQ-1` remains separate | Uses completed prior capabilities and earlier stories only | Pass with several oversized stories |

### Dependency Analysis

The declared ordering is otherwise sound:

- Epic 5 progresses from boundary baseline to live Agent/Provider operations, identity/policy readiness, shared readiness contracts, platform composition, and activation.
- Epic 6 progresses from durable interaction ownership through context, safety, cost, capacity, posting, and participant UX.
- Epic 7 branches safely from a created proposal: edit, regenerate, approve/post, reject/abandon, and expiry depend only on prior state.
- Epic 8 uses earlier governance/runtime outputs; 8.2 and 8.3 branch from 8.1, 8.4 uses already implemented controls, and 8.5–8.7 consume prior evidence.
- No active story declares a dependency on a later epic.

### Critical Violations

#### CQ-1 — Stories 6.4 and 6.5 contain a latent forward-dependency cycle

Story 6.4 declares **Forward dependencies: None**, but its own dependency text says production call acceptance cannot mint a live admission grant until Story 6.5 supplies the shared allocator. Its primary demonstrable outcome invokes the Provider only by using an injected trusted admission grant. Story 6.5, in turn, depends on Story 6.4 for the reserved deterministic attempt.

This means Story 6.4's stated live user outcome—generation under hard cost reservation—is not independently usable without future Story 6.5, while 6.5 cannot precede 6.4 as written. The fixture seam demonstrates a contract but does not remove the product dependency.

**Recommendation:** Reslice the work into:

1. prepared attempt plus atomic reservation, ending before Provider transport;
2. shared durable capacity admission/fencing using that prepared attempt;
3. Provider invocation, recovery, and cost reconciliation after both controls exist.

Alternatively, combine the cost and capacity stories if the boundary cannot produce independently valuable, executable outcomes. Update the story count, dependency topology, and evidence ownership accordingly.

### Major Issues

#### MQ-1 — Story 5.6 is larger than a normal story

It composes Agents with EventStore, Conversations, Parties, Tenants, Dapr Workflow, readiness, capacity, telemetry, identity, evidence ingress, and fail-closed Provider/safety ports; validates secrets across configured/denied/missing/rotation states; proves Dapr route/access-control isolation; and audits repository/package ownership.

**Recommendation:** Split platform composition/health from secrets and access-control qualification, while retaining one explicit platform-hosted end-to-end checkpoint.

#### MQ-2 — Story 6.1 combines workflow construction with full recovery qualification

It establishes the sole durable workflow owner, command/checkpoint idempotency, replay behavior for all current activities, cross-tenant denial, EventStore RPO 0, a frozen cohort, and 15-minute production-like recovery across injected faults.

**Recommendation:** Separate deterministic workflow ownership/replay from production-like recovery qualification. The latter can consume the completed workflow checkpoints without becoming their implementation owner.

#### MQ-3 — Story 6.5 is an allocator subsystem rather than a story-sized slice

It owns configuration contracts, a shared linearizable allocator, durable queues, weighted round-robin fairness, admission fencing, multi-replica saturation, crash/cancel/expiry recovery, and coexistence with cost caps.

**Recommendation:** Split capacity profile/admission, fairness scheduling, and recovery/fencing qualification into sequential independently testable slices.

#### MQ-4 — Story 8.3 combines deletion policy, cryptographic erasure, a broad projection purge, recovery, authorization, UI, and forensic proof

The acceptance boundary spans protected EventStore payloads and a named inventory of content-bearing and non-content projections.

**Recommendation:** Separate request/eligibility and legal-hold gating, payload erasure/redaction, projection purge/recovery, and operator UI/evidence—while keeping restrictive completion as the final integrative story.

#### MQ-5 — Story 8.5 owns too many independent metric families

It implements all NFR-9 latency calculators and SM-1 through SM-6 product calculators, together with cohort/window/late-data behavior, insufficiency semantics, isolation, publication, and counter-metric safeguards.

**Recommendation:** Split runtime latency measurement from product/adoption/workflow metrics, with a small shared versioned measurement-contract foundation.

#### MQ-6 — Story 8.6 is an epic-sized UX qualification matrix

It covers every interactive route and high-impact state, WCAG 2.2 AA, English/French parity, four responsive profiles, restrictive actions, API/UI authorization parity, three browser-timing sample kinds, server correlation, duplicate handling, and cross-tenant denial.

**Recommendation:** Split NFR-13 conformance from NFR-14 browser performance, and consider a separate route/operation isolation qualification slice. Retain one final completeness gate over the versioned route/state inventory.

### Minor Concerns

1. Literal standalone `+` lines exist before the Epic 6 and Epic 7 headings (current lines 1511 and 1889). Remove them to avoid parser and rendering noise.
2. Requirement notation varies between `FR1`/`FR-1`, `NFR1`/`NFR-1`, and the heading `NonFunctional Requirements`. Normalize identifiers so traceability automation has one canonical form.
3. Historical Epics 1–4 remain in the same file and contain criteria superseded by the current PRD, including bounded-context and alternate-invocation language. The replacement-authority warning is clear, but all automation must prove that historical stories are excluded from executable backlog generation.

### Positive Compliance Findings

- Active epic titles and goals describe administrator, participant, Approver, governance, or operator outcomes rather than database/API milestones.
- The active backlog has the declared 7 + 7 + 6 + 7 = 27 stories.
- Story dependencies are explicit, and—with CQ-1 excepted—point only to earlier work or named external prerequisites.
- Acceptance criteria consistently use Given/When/Then, include happy paths, denial/error paths, idempotency/concurrency behavior, safe disclosure, and evidence commands.
- Aggregates, projections, and other durable structures are introduced when their story needs them. Story 5.1 explicitly forbids pre-creating unrelated future entities/events.
- Architecture specifies no external starter template; Story 5.1 correctly uses the approved Structural Seed and includes clean-checkout, source/package, compatibility, and CI concerns appropriate to this brownfield correction.
- Every active story contains a requirement/evidence manifest and an executable verification-command placeholder.

## Summary and Recommendations

### Overall Readiness Status

**NOT READY**

The artifacts are coherent enough to show what must be built, but the active backlog is not ready to enter Phase 4 implementation as declared.

Three conditions drive this decision:

1. The architecture and epics state that all seven critical external dependencies are currently `Uncommitted`. The PRD makes that status, a missing immutable target, or a missing executable compatibility command an explicit blocker for every consuming story's transition to `ready-for-dev`.
2. Story 6.4 has a hidden forward dependency on Story 6.5 while Story 6.5 depends on Story 6.4, violating the required independently completable story sequence.
3. The current `_bmad-output/implementation-artifacts/sprint-status.yaml` is stale: it still tracks the superseded 18-story Epic 5 plan, while `epics.md` declares replacement Epics 5–8 with 27 active stories. An implementation agent following sprint status would execute the wrong backlog.

This is not a requirements-coverage failure. FR coverage is 100%, the current PRD/UX/Architecture documents are materially aligned, and the active epics are organized around recognizable user and operator outcomes. The decision is caused by dependency commitment and executable-backlog quality.

### Critical Issues Requiring Immediate Action

1. **Resolve the external dependency gate.** Complete the required commitment fields for `EXT-CONV-AI-1`, `EXT-HOST-1`, `EXT-PROVIDER-1`, `EXT-SAFETY-1`, `EXT-TOKEN-1`, `EXT-SECRETS-1`, and `EXT-TOPOLOGY-1`. Prioritize `EXT-HOST-1`, because Story 5.1 is the first active story and cannot enter `ready-for-dev` while that record is uncommitted.
2. **Remove the Story 6.4/6.5 dependency cycle.** Reslice prepared attempt/reservation, capacity admission/fencing, and live Provider invocation/reconciliation so every story ends in an independently demonstrable outcome without a future story supplying a required runtime capability.
3. **Reconcile the active story graph after reslicing.** Update story numbering/counts, dependency topology, owned clauses, evidence manifests, verification commands, and sprint-status inputs before any affected story is created for implementation.
4. **Replace the stale sprint plan.** Do not create or develop a story from the current sprint-status Epic 5 rows. Regenerate sprint planning from the corrected replacement Epics 5–8 only after implementation readiness passes.

### Recommended Next Steps

1. Run the external-dependency commitment workflow with the named owners and obtain immutable targets, dates, executable compatibility commands, required evidence levels, and accepted statuses.
2. Correct Stories 6.4 and 6.5 using the proposed three-slice sequence, or combine them if no independently useful boundary exists.
3. Decompose oversized Stories 5.6, 6.1, 6.5, 8.3, 8.5, and 8.6 into smaller sequential outcomes while preserving their final integration gates.
4. Remove the two stray `+` lines, normalize `FR-`/`NFR-` identifier syntax, and enforce machine exclusion of historical Epics 1–4 from executable backlog generation.
5. Keep `LR-UI-CONFORMANCE`, `LR-UI-PERFORMANCE`, and other live qualification gates at `InsufficientEvidence` until their declared platform topology, routes, sessions, correlations, and sample counts genuinely exist.
6. Rerun implementation-readiness validation after the dependency register and epic/story corrections. Only then move the first unblocked active story to `ready-for-dev`.

### Final Note

This assessment identified **15 actionable issues across five categories**:

- 4 architecture/UX execution-readiness warnings
- 1 critical story-independence violation
- 6 major story-sizing issues
- 3 minor document/tooling concerns
- 1 implementation-artifact synchronization defect

The strongest parts of the package are its contiguous 28-FR/14-NFR inventory, 100% active-epic FR mapping, explicit fail-closed rules, UX/architecture traceability, and unusually concrete evidence manifests. Preserve those strengths while fixing the blocking dependency and story-structure defects.

**Assessment date:** 2026-08-02  
**Assessor:** Codex — BMAD Implementation Readiness workflow
