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
completedDate: 2026-08-04
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
  - _bmad-output/planning-artifacts/implementation-readiness-report-2026-08-04.previous.md
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
- The standard filename scan did not expose the PRD, Architecture, or UX inputs because their authoritative files use nonstandard nested names without `index.md`; the confirmed prior input inventory was reused.
- `epic-5-superseded-2026-08-01.md` is excluded because its filename identifies it as superseded.
- The prior completed assessment was preserved as `implementation-readiness-report-2026-08-04.previous.md` and excluded from this assessment.

## PRD Analysis

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

- **Product boundary:** V1 exposes only the named Agent `hexa`, invoked solely through a Conversation-owned **Call hexa** action. Long-term memory, tools, project/folder/file retrieval, ambient triggers, agent-to-agent orchestration, external-channel bots, and business actions beyond Conversation replies are explicitly excluded.
- **Conversation context:** Every call uses the complete authorized Source Conversation or fails closed before Provider invocation. Truncation, summarization, windowing, and alternate retrieval modes are prohibited.
- **Proposal authority:** Hexalith Agents owns durable proposal state and read models; Dapr Workflow owns execution and durable expiry timing but is not the proposal system of record. Default proposal expiry is 24 hours, configurable from 1 hour through 30 days for future proposals only.
- **Data governance:** Sensitive Agent content is retained for 365 days after terminal state unless legal hold applies. Export must be tenant-scoped, encrypted, time-limited, manifested, and audited. EventStore history is never rewritten; expiry or approved deletion must cryptographically erase or redact protected content, purge affected projections, and retain only a support-safe tombstone.
- **External dependency commitment:** Every critical dependency entry must record owner, repository, artifact, target version/commit, target date, compatibility contract or test plus verification command, required Evidence Level, accepted status, and consuming stories. `Uncommitted`, missing-target, or missing-command entries block consuming stories from `ready-for-dev`.
- **Conversations prerequisite:** `EXT-CONV-AI-1` / `CONV-AI-1` requires `IConversationClient.AddParticipantAsync` and `POST /api/v1/conversations/{conversationId}/participants`, stable AI participant typing, member role, deterministic idempotency, typed conflicts, and cross-tenant denial. Agents may not write Conversation streams directly or treat proposals as messages.
- **Public compatibility:** JSON object evolution is additive within V1; public enums start with `Unknown = 0`; public members and enum meanings are not removed, renamed, or reused. Breaking changes require a new major package/API version plus package-consumer compatibility tests.
- **Evidence authority:** Evidence Levels 1–5 are normative. Runtime production-like readiness requires live Levels 4 and 5. Lower levels, skips, placeholders, and conditional results cannot establish readiness. Deterministic fixtures validate metric calculation only, not live attainment.
- **Measurement contracts:** Every qualification metric must version its authoritative events/timestamps, numerator/denominator or percentile method, sample/window/cohort rules, late/missing data handling, and `InsufficientEvidence` behavior.
- **Launch metrics:** SM-1 requires at least one enabled launch tenant with successful production-like calls. SM-2 requires at least 20% adoption across at least 50 eligible Conversations in a rolling 30-day cohort. SM-3 requires at least 95% terminal proposal completion within 26 hours, expiry ≤ 20%, posting failure ≤ 2%, human resolution ≥ 70%, and audit completeness = 100%.
- **Operational release gate:** Controlled production-like qualification does not authorize production. Production remains blocked until operational gate `RQ-1` records READY from required live evidence and metrics.
- **Addendum authority:** `addendum.md` is explicitly contextual landscape material and does not override the PRD or introduce normative requirements.

### PRD Completeness Assessment

The PRD is structurally strong and unusually explicit for implementation planning: it is marked final, contains 28 stable FR identifiers and 14 stable NFR identifiers, attaches testable consequences to every FR, separates non-goals from launch scope, defines public-compatibility rules, fixes quantitative thresholds, resolves all V1 decision-register questions, and establishes normative evidence levels and the post-implementation `RQ-1` gate.

The principal readiness dependency is external rather than an ambiguity in product intent: critical integrations remain implementation-blocking until their separate dependency-register entries contain accepted concrete targets and executable compatibility commands. The PRD is therefore complete enough for traceability assessment, but no consuming story can be considered implementation-ready merely because the product requirement is clear; the dependency commitments and required evidence must also be present.

## Epic Coverage Validation

The epic document's explicit FR Coverage Map declares FR1 through FR28 with no extra FR identifiers. Epics 1–4 and their stories are marked completed historical evidence only; the matrix below uses active replacement Epics 5–8 as the forward implementation path and names representative active stories that own the requirement clauses.

### Coverage Matrix

| FR Number | PRD Requirement | Active Epic/Story Coverage | Status |
| --- | --- | --- | --- |
| FR-1 | Configure `hexa` | Epic 5 — Stories 5.2, 5.7 | ✓ Covered |
| FR-2 | Link Agent to Party identity | Epics 5 and 6 — Stories 5.4, 6.6 | ✓ Covered |
| FR-3 | Manage Agent lifecycle | Epic 5 — Stories 5.2, 5.7 | ✓ Covered |
| FR-4 | Manage Global Providers Aggregate | Epics 5 and 8 — Stories 5.3, 5.5, 8.4 | ✓ Covered |
| FR-5 | Select Provider and model per Agent | Epics 5 and 6 — Stories 5.3, 5.5, 6.2, 6.4 | ✓ Covered |
| FR-6 | Configure response mode | Epic 5 — Story 5.2 | ✓ Covered |
| FR-7 | Configure Approver Policy | Epics 5 and 7 — Stories 5.4, 7.1, 7.4 | ✓ Covered |
| FR-8 | Call Agent from Conversation | Epic 6 — Stories 6.1, 6.2, 6.7 | ✓ Covered |
| FR-9 | Build complete-or-blocked V1 Conversation Context | Epic 6 — Story 6.2; revalidation in Story 7.3 | ✓ Covered |
| FR-10 | Handle generation failure | Epic 6 — Stories 6.1–6.4, 6.7 | ✓ Covered |
| FR-11 | Post automatic response | Epic 6 — Stories 6.6, 6.7 | ✓ Covered |
| FR-12 | Prevent automatic posting when any policy gate fails | Epic 6 — Stories 6.3–6.7 | ✓ Covered |
| FR-13 | Create Proposed Agent Reply | Epic 7 — Story 7.1 | ✓ Covered |
| FR-14 | Preserve all proposal versions | Epic 7 — Stories 7.1–7.5 | ✓ Covered |
| FR-15 | Edit Proposed Reply | Epic 7 — Story 7.2 | ✓ Covered |
| FR-16 | Regenerate Proposed Reply | Epic 7 — Story 7.3 | ✓ Covered |
| FR-17 | Approve selected proposal version | Epic 7 — Story 7.4 | ✓ Covered |
| FR-18 | Reject, abandon, or expire Proposed Reply | Epics 7 and 8 — Stories 7.5, 7.6, 8.1, 8.3 | ✓ Covered |
| FR-19 | Enforce tenant isolation | Epics 5–8 — focused negative evidence in every active story | ✓ Covered |
| FR-20 | Enforce role and policy authorization | Epics 5–8 — pre-side-effect authorization throughout active stories | ✓ Covered |
| FR-21 | Fail closed on dependency uncertainty | Epics 5–8 — dependency gates and explicit blocked results throughout | ✓ Covered |
| FR-22 | Provide Admin UI | Epics 5–8 — setup, call, proposal, governance, and qualification UI stories | ✓ Covered |
| FR-23 | Provide stable API/client contracts | Epics 5–8 — Stories 5.1, 5.2, 5.5, 5.6 and downstream parity stories | ✓ Covered |
| FR-24 | Capture Agent Audit Evidence | Epics 5–8 — evidence manifests across every active capability | ✓ Covered |
| FR-25 | Expose operational status | Epics 5–8 — readiness/runtime/proposal/governance/qualification status | ✓ Covered |
| FR-26 | Configure Content Safety and prompt policy | Epics 6 and 8 — Stories 6.3, 8.4 | ✓ Covered |
| FR-27 | Enforce safety before Provider and Conversation side effects | Epics 6 and 7 — Stories 6.3, 7.3, 7.4 | ✓ Covered |
| FR-28 | Define and enforce launch-readiness controls | Epics 5–8 — Stories 5.5, 5.7, 6.4–6.5, 8.5–8.7; `RQ-1` aggregates after implementation | ✓ Covered |

### Missing Requirements

No PRD Functional Requirement is absent from the epic coverage map or active replacement backlog. No epic-only FR identifier exists outside the PRD's FR1–FR28 range.

Coverage is not equivalent to story readiness: the epic document explicitly records active stories as backlog, blocked, or not run where their critical external dependencies remain `Uncommitted`. Those execution-readiness conditions will be assessed in later workflow steps rather than treated as FR coverage gaps.

### Coverage Statistics

- Total PRD FRs: 28
- FRs represented in the epic coverage map: 28
- FRs with an active Epic 5–8 implementation path: 28
- Missing PRD FRs: 0
- Epic-only FRs not present in the PRD: 0
- Coverage percentage: 100%

## UX Alignment Assessment

### UX Document Status

**Found and final.** The assessment used both authoritative UX spines:

- `DESIGN.md` — final; updated 2026-08-02; visual semantics, inherited FrontComposer/Fluent V5 system, components, layout, typography, state roles, and governance presentation.
- `EXPERIENCE.md` — final; updated 2026-08-02; surfaces, journeys, state machines, accessibility, localization, responsive behavior, performance evidence, and interaction flows.

The spines explicitly state that they supersede mockups, wireframes, imports, and source-derived sketches on conflict.

### UX ↔ PRD Alignment

- **User journeys align:** UX UJ-1 through UJ-4 preserve the PRD's administrator, participant, approver, and integration-developer journeys. UX UJ-5 adds a release/governance operator flow that is directly grounded in FR-26–FR-28, NFR-9–NFR-14, PRD data governance, and `RQ-1`; it does not introduce an unsupported product capability.
- **Invocation aligns:** Both require the Conversation-owned **Call hexa** action as the sole V1 entry. Mentions, commands, ambient triggers, project/folder triggers, and external channels are excluded.
- **Context aligns:** Both require complete authorized Conversation Context or a fail-closed pre-Provider block. UX exposes a read-only policy and no truncation, summarization, windowing, sampling, or alternate retrieval control.
- **Proposal truth aligns:** Both distinguish generated content from Conversation Messages, preserve immutable generated/edited/regenerated versions, and treat `approved` and `posting pending` as non-success. Only authoritative `posted` proves a message exists.
- **Failure behavior aligns:** Generation failure creates neither a proposal nor a message; any retained content is isolated in a separate authorized non-approvable failure record.
- **Governance aligns:** Safety publication, hard cost controls, 365-day retention, legal holds, encrypted time-limited export, restrictive deletion/projection purge, evidence levels, and final `RQ-1` separation are represented without weakening the PRD.
- **NFRs align:** WCAG 2.2 AA, English/French whole-string parity, fail-closed restrictive viewports, the exact NFR-9/NFR-14 thresholds, 30-sample minima, and `InsufficientEvidence` behavior are preserved.

No UX capability contradicts or materially exceeds the approved PRD scope.

### UX ↔ Architecture Alignment

- **UI substrate:** AD-15, AD-16, and AD-25 support the UX requirement for FrontComposer plus Fluent UI Blazor V5, policy-gated navigation, shared public contracts, WCAG behavior, localization parity, and restrictive viewport handling.
- **Truthful states:** AD-10, AD-12, AD-17, AD-25, and AD-26 provide the authoritative readiness, Provider-state, high-risk pending-command, projection, and browser-timing contracts needed by the UX badges, status panels, and command truth flows.
- **Conversation and proposal flows:** AD-4 through AD-8 and AD-13 support immutable proposal versions, selected-version approval, deterministic posting, Party attribution, Conversations-owned membership/posting, current Approver authority, and exact terminal-state behavior.
- **Complete context and safety:** AD-11 and AD-20 directly support the read-only complete-context-or-blocked surface and two-stage no-weaker safety behavior.
- **Cost, capacity, and operations:** AD-21 and AD-24 support the cost-control editor, reservation states, hard blocking, capacity/backpressure status, and fairness evidence.
- **Audit governance:** AD-14, AD-17, AD-22, and AD-23 support protected evidence, explicit projection inventory, retention/hold/export/deletion surfaces, restrictive partial failure, and recovery evidence.
- **Performance evidence:** AD-26 matches all three UX browser sample kinds, monotonic timing seams, thresholds, correlation rules, duplicate handling, and insufficient-sample behavior.

No UX component or behavior lacks an architectural owner or contract.

### Alignment Issues

No unresolved PRD↔UX or UX↔Architecture specification conflict was found in the authoritative documents.

### Warnings

- **External proof is not yet available:** UX conformance and browser-performance qualification depend on `EXT-HOST-1` and `EXT-TOPOLOGY-1`; the architecture states that all seven critical dependency records are currently `Uncommitted`. The UX is architecturally specified but cannot yet earn its required live Levels 4–5 evidence.
- **Current topology is non-conformant:** the Architecture Spine records that module-owned `Hexalith.Agents.AppHost`, `.Aspire`, and `.ServiceDefaults` projects still exist even though the required UX composition boundary is platform-owned. Active Stories 5.1 and 5.6 own correction and proof.
- **Provider truth is not yet conformant:** AD-10 records missing durable capability high-water and distinct effective-version behavior in current runtime paths. Until corrected, the UI cannot truthfully claim the fully specified provider readiness/effective-version evidence.
- These warnings are explicit implementation-readiness blockers already represented in Architecture and the active backlog; they are not gaps in UX documentation.

## Epic Quality Review

### Scope And Authority

The review evaluated all eight epics and all stories in `epics.md`. Epics 1–4 are explicitly historical completed evidence and are not treated as executable forward work. Epics 5–8 and their 27 stories are the active backlog. `RQ-1` is correctly separated as a non-estimated post-implementation release gate.

### Epic Structure Validation

| Epic | User-value outcome | Independent of later epics | Assessment |
| --- | --- | --- | --- |
| 1 — Tenant Agent Setup And Governance | Yes | Historical | User-centered outcome; historical only. |
| 2 — Safe Conversation Invocation And Automatic Replies | Yes | Historical | User-centered automatic-response outcome; historical only. |
| 3 — Proposal Review And Approval Workflow | Yes | Historical | User-centered approval outcome; historical only. |
| 4 — Operational Visibility, Audit, Integration, And Launch Readiness | Yes | Historical | Operator/integration outcome; historical only. |
| 5 — Live Governed Setup And Honest Readiness | Yes | Yes | Complete setup/readiness outcome, but its root story is externally blocked. |
| 6 — One Safe Automatic Conversation Response | Yes | Yes, at epic level | Complete automatic-response outcome without Epic 7, but its internal Story 6.4/6.5 decomposition is circular. |
| 7 — Complete Confirmation And Approval | Yes | Yes | Builds only on prior epics and provides a complete confirmation outcome. |
| 8 — Governance Operations And Release Qualification | Yes | Yes | Operator/governance outcome; correctly leaves the final decision to `RQ-1`, but several stories are oversized. |

No active epic is merely a technical milestone. The epic ordering has no dependency on a later epic and no epic-level cycle.

### 🔴 Critical Violations And Blockers

#### C1 — The active backlog has no executable starting story

Story 5.1 is the root of the active dependency graph. It requires `EXT-HOST-1` to be at least `Committed` before `ready-for-dev`, while the architecture and story record it as `Uncommitted`. Stories 5.2 and 5.3 depend on 5.1, and every later active story depends transitively on that chain.

**Impact:** No active Epic 5–8 story can legitimately enter implementation under the PRD's external-dependency commitment rule. The backlog is traceable but not startable.

**Remediation:** Commit `EXT-HOST-1` with its named owner, immutable target, integration date, compatibility contract, executable verification command, required evidence level, accepted status, and consuming stories. Do not bypass the gate with the historical module-owned host.

#### C2 — Stories 6.4 and 6.5 contain a forward/circular dependency

Story 6.4 declares no forward dependency but states that production call acceptance cannot mint a live admission grant until later Story 6.5 supplies the shared allocator. Story 6.5 in turn depends on Story 6.4 for the reserved deterministic attempt and cost-cap behavior.

**Impact:** Story 6.4 cannot deliver its real live Provider-attempt outcome without future work, while Story 6.5 cannot be implemented in its intended shape without Story 6.4. The injected admission/fence contract proves an isolated seam, not an independently usable vertical slice.

**Remediation:** Replace the two-story cycle with an ordered decomposition:

1. Prepare the deterministic attempt and reserve cost without Provider transport.
2. Implement the shared allocator/admission/fence for a reserved attempt.
3. Invoke and recover the Provider attempt using both completed prior capabilities, then reconcile cost.

Alternatively merge the tightly coupled work into one epic outcome and split it internally into implementation tasks rather than claiming independent stories.

#### C3 — Epic-sized active stories violate independent story sizing

At minimum, these stories combine too many independently failing subsystems and evidence lanes to be credible single stories:

- **6.4:** prepared-attempt canonicalization, budget ledger concurrency, secret resolution, live Provider adapter, transport idempotency, outcome recovery, usage reconciliation, retry policy, and tenant isolation.
- **6.5:** capacity-profile contracts, distributed allocator, durable queueing, weighted fairness, admission fencing, cancellation/expiry, replica/crash recovery, cost coexistence, and production-like qualification.
- **8.6:** every interactive V1 route and state across WCAG 2.2 AA, EN/FR parity, responsive safety, authenticated browser evidence ingress, three timing sample kinds, correlation, duplicate handling, and production-like threshold qualification.

**Impact:** Review, estimation, implementation, and failure attribution cannot remain story-scoped; partial completion is likely to be hidden behind a single status.

**Remediation:** Split each into ordered, demonstrable vertical stories whose acceptance criteria and evidence commands can pass independently. Keep final production-like qualification as the last story after component/contract and integration stories are complete.

### 🟠 Major Issues

#### M1 — Additional stories are too broad for reliable independent completion

- **5.6** combines cross-repository host composition, Dapr topology, access control, secret rotation, health/telemetry, browser evidence ingress, clean-checkout proof, and repository-boundary cleanup verification.
- **6.1** combines workflow ownership, every orchestration checkpoint, restart injection, RPO 0, 15-minute recovery, duplicate-effect inventories, and authorization isolation.
- **8.3** combines cryptographic erasure/redaction, tombstones, a large named projection purge inventory, restrictive partial failure, recovery, UI/API status, and forensic no-content proof.
- **8.5** combines all NFR-9 latency calculators and SM-1–SM-6 product metrics, including window/cohort/late-data/duplicate rules and live evidence classification.
- **8.7** combines checkpoint-consistent inspection of all 18 gates, seven dependency classes, evidence-level validation, supersession logic, UI behavior, and strict `RQ-1` separation.

**Remediation:** Separate contract/calculator or state-machine work from live integration and production-like qualification. Preserve one primary demonstrable outcome and one bounded verification command per story.

#### M2 — Historical stories with forbidden criteria remain inline with the active backlog

The document header correctly marks Epics 1–4 historical and declares conflicting criteria `mustNotImplement`, but their unchanged acceptance criteria remain in the same main file. Examples include historical Story 2.3 permitting a bounded-context behavior, Story 2.6 allowing multiple invocation affordances, Story 4.4 accepting reporting-only cost posture or accepted risk, and Story 4.5 mentioning MCP/A2A/tool or alternate framework evidence. All conflict with current PRD/Architecture authority.

**Impact:** A story-selection tool or implementer that lands below the replacement-authority header can execute forbidden historical criteria.

**Remediation:** Move Epics 1–4 to a clearly non-executable historical artifact or add machine-readable `status: historical`, `executable: false`, and replacement references at each epic/story boundary. Keep only active Epics 5–8 in the executable backlog view.

#### M3 — Story 7.3 has an unnecessary serial dependency on Story 7.2

Regeneration requires the initial immutable proposal/version from Story 7.1, but it does not inherently require the edit feature from Story 7.2. Requiring 7.2 delays a separate user action and weakens parallelism.

**Remediation:** Make Story 7.3 depend on 7.1 and a shared append-only version capability established there. Treat edited versions as optional prior history, as Story 7.4 already does for 7.2/7.3.

### 🟡 Minor Concerns

- Two standalone `+` lines remain between active epic sections, indicating patch residue and weakening document cleanliness.
- Requirement identifiers vary between PRD `FR-1`/`NFR-1` and epic `FR1`/`NFR1` forms. Normalize identifiers or document canonical normalization so automated traceability cannot split equivalent IDs.
- Story 5.1 is a technical-enabler story. It is acceptable for this brownfield boundary correction because it has an Integration Developer persona, a concrete package-consumer outcome, and explicitly avoids pre-creating future domain entities, but it should remain narrowly limited to that enabling outcome.

### Acceptance-Criteria Quality

The active stories are strong in form and specificity:

- Acceptance criteria consistently use Given/When/Then structure.
- Positive, denial, cross-tenant, replay, duplicate, stale, and failure cases are explicit.
- Every active story includes a primary demonstrable outcome, declared dependencies, evidence level, named artifacts/tests, an executable verification command, focused negative evidence, and an honest current result.
- Tenant-isolation changes attach named denial tests rather than relying on a broad build or happy-path suite.
- Pending, accepted, and terminal truth states are not collapsed.

The sizing findings above are therefore not caused by vague criteria; they arise because too many individually testable criteria and subsystem responsibilities have been packed into single stories.

### Dependency And Data-Timing Review

- Active epic ordering is acyclic: Epic 5 → Epic 6 → Epic 7 → Epic 8, with explicitly permitted parallel branches inside Epics 5, 7, and 8.
- Apart from the Story 6.4/6.5 cycle and the unnecessary 7.2 prerequisite for 7.3, declared within-epic dependencies point backward only.
- Stories create domain state and projections when first required; Story 5.1 explicitly forbids creating all future entities or storage structures up front.
- Architecture specifies no external starter template, so no missing starter-template story exists.
- The project is brownfield. Story 5.1 covers existing boundary/package correction; Story 5.6 covers platform-host compatibility. Both correctly recognize migration/integration work rather than assuming greenfield infrastructure.

### Best-Practices Compliance Summary

| Check | Epic 5 | Epic 6 | Epic 7 | Epic 8 |
| --- | --- | --- | --- | --- |
| Delivers user/operator value | ✓ | ✓ | ✓ | ✓ |
| Independent of later epics | ✓ | ✓ | ✓ | ✓ |
| Stories appropriately sized | ⚠ Some broad | ✗ 6.4–6.5 | ⚠ 7.3/7.4 broad | ✗ 8.6; others broad |
| No forward/circular story dependency | ✓ | ✗ 6.4↔6.5 | ⚠ unnecessary 7.2→7.3 | ✓ |
| Domain data introduced when needed | ✓ | ✓ | ✓ | ✓ |
| Clear, testable acceptance criteria | ✓ | ✓ | ✓ | ✓ |
| FR traceability maintained | ✓ | ✓ | ✓ | ✓ |
| Has an implementation-ready starting story | ✗ | Blocked transitively | Blocked transitively | Blocked transitively |

## Summary and Recommendations

### Overall Readiness Status

# NOT READY

The planning set is strong in product definition but not ready to begin the active implementation backlog. The PRD is complete, UX and Architecture are aligned, and all 28 FRs have active epic coverage. Those strengths do not overcome the absence of an executable starting story, a circular story decomposition, oversized stories, uncommitted critical dependencies, and explicit current architecture nonconformances.

### Critical Issues Requiring Immediate Action

1. **No active story can start under the binding dependency rule.** Story 5.1 requires `EXT-HOST-1` at least `Committed`; it is `Uncommitted`, and the entire active graph depends transitively on Story 5.1. The architecture also records all seven critical dependencies as `Uncommitted`.
2. **Stories 6.4 and 6.5 are circular in practical delivery order.** Story 6.4 needs the later allocator for a real admission grant, while Story 6.5 depends on Story 6.4's prepared/reserved attempt.
3. **Stories 6.4, 6.5, and 8.6 are epic-sized.** Each combines multiple independently failing components and live qualification lanes that cannot be credibly estimated, reviewed, or closed as one story.
4. **Current implementation does not conform to the approved architecture.** Module-owned AppHost/Aspire/ServiceDefaults projects still exist despite the platform-owned boundary, and current runtime paths lack the durable Provider capability high-water/effective-version behavior required for truthful readiness.
5. **Historical conflicting criteria remain in the main backlog artifact.** The replacement authority is stated, but unchanged historical stories still contain now-forbidden bounded-context, invocation, cost, and framework criteria.

### Recommended Next Steps

1. **Commit the dependency authority before implementation.** Complete `EXT-HOST-1` first so Story 5.1 can become eligible. Then commit each consumed Provider, safety, tokenizer, secrets, topology, and Conversations AI-membership dependency with immutable target, date, owner, compatibility command, evidence level, accepted status, and consuming stories.
2. **Repair the Epic 6 dependency graph.** Split the current 6.4/6.5 pair into: prepared attempt plus cost reservation; shared capacity admission/fencing; then Provider invocation, recovery, and reconciliation consuming both prior capabilities.
3. **Split oversized stories.** Separate contract/state-machine work, component integration, failure/recovery proof, and production-like qualification for 6.4, 6.5, 8.6, and the broad stories listed under M1. Each replacement story needs one primary outcome and a verification command that can pass independently.
4. **Remove unnecessary serialization.** Make Story 7.3 depend on Story 7.1 and the shared immutable-version capability, with Story 7.2 edits optional prior history.
5. **Make the executable backlog unambiguous.** Move Epics 1–4 into a non-executable historical artifact or add per-story machine-readable exclusion/replacement metadata. Remove patch residue and normalize `FR-1`/`NFR-1` identifier formatting.
6. **Close the stated architecture gaps through the corrected backlog.** Do not use the current module-owned host as dependency evidence and do not expose Provider callability/effective-version success before AD-10 behavior is implemented and verified.
7. **Re-run implementation readiness.** Reassess only after the dependency register has a committed root, the story graph is acyclic, the oversized stories are decomposed, and the executable backlog no longer mixes historical forbidden criteria with active work.

### What Does Not Need Rework

- The final PRD requirement set: 28 FRs and 14 NFRs are clear, stable, and testable.
- UX documentation: both final spines align with the PRD and Architecture.
- FR coverage: all 28 FRs have active Epic 5–8 paths.
- Acceptance-criteria style: active stories use specific BDD scenarios, named evidence, focused denial tests, and honest blocked/not-run results.
- Epic-level product outcomes: Epics 5–8 are user/operator-centered and ordered without a later-epic dependency.

### Final Note

This assessment records 3 critical, 3 major, and 2 minor planning defects across dependency readiness, story dependency/sizing, executable-backlog hygiene, and traceability formatting. It also confirms explicit topology and Provider-runtime implementation nonconformances. Address the critical defects before beginning active story implementation; proceeding as-is would violate the PRD's own dependency gate and create avoidable partial-story delivery.

**Assessment date:** 2026-08-04  
**Assessor:** Codex — BMAD Implementation Readiness workflow

