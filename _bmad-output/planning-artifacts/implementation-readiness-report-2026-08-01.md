---
stepsCompleted:
  - step-01-document-discovery
  - step-02-prd-analysis
  - step-03-epic-coverage-validation
  - step-04-ux-alignment
  - step-05-epic-quality-review
  - step-06-final-assessment
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
documentSelectionNotes:
  - Nonstandard grouped document folders were accepted despite having no index.md files.
  - Polish, reconciliation, review, validation, and .memlog files are supporting evidence rather than primary assessment inputs.
date: 2026-08-01
project_name: agents
assessmentStatus: NOT_READY
assessor: Codex using BMad Implementation Readiness
---

# Implementation Readiness Assessment Report

**Date:** 2026-08-01
**Project:** agents

## Document Discovery

### PRD Files Selected

- `prds/prd-agents-2026-06-23/prd.md` — 54,600 bytes; modified 2026-08-01
- `prds/prd-agents-2026-06-23/addendum.md` — 1,950 bytes; modified 2026-08-01

Potential PRD variants (`polish-prose-prd.md`, `polish-structure-prd.md`) and reconciliation, review, validation, and memory-log files are classified as supporting evidence rather than primary inputs.

### Architecture Files Selected

- `architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md` — 34,152 bytes; modified 2026-08-01
- `architecture/architecture-agents-2026-06-23-2/IMPLEMENTATION-CONVENTIONS.md` — 6,176 bytes; modified 2026-07-31

Architecture review and memory-log files are classified as supporting evidence.

### Epics and Stories File Selected

- `epics.md` — 108,178 bytes; modified 2026-08-01

### UX Files Selected

- `ux-designs/ux-agents-2026-06-23/DESIGN.md` — 18,908 bytes; modified 2026-08-01
- `ux-designs/ux-agents-2026-06-23/EXPERIENCE.md` — 28,451 bytes; modified 2026-08-01

UX review, validation, and memory-log files are classified as supporting evidence.

### Discovery Resolution

No whole-versus-sharded duplicate set was selected. PRD, architecture, and UX artifacts use nonstandard grouped folders without `index.md`; the user confirmed the primary files listed above. The previous same-day readiness report was preserved as `implementation-readiness-report-2026-08-01.pre-rerun-2.md`.

## PRD Analysis

### Functional Requirements

**FR-1: Configure `hexa`.** Agent Administrators can create or enable `hexa` with a stable Agent identity, display name, description, Agent Instructions, lifecycle state, and tenant scope.

- Prevent activation when required fields are missing or invalid.
- Expose current configuration through the admin UI and API/client contracts.
- Audit configuration changes with actor, timestamp, and safe prior/new values.

**FR-2: Link Agent To Party Identity.** Agent Administrators can provision or link the Agent's Party identity so `hexa` appears as a known AI participant when it posts to a Conversation.

- An active Agent has exactly one Party identity.
- Reject posting when that identity is missing, disabled, ambiguous, or unauthorized for the Source Conversation.
- Attribute posted messages to the Agent Party, never the caller or a generic system account.

**FR-3: Manage Agent Lifecycle.** Agent Administrators can activate, disable, and inspect `hexa` lifecycle state.

- Disabled Agents cannot be called.
- Disabling does not delete Audit Evidence, proposals, or Conversation Messages.
- Lifecycle changes are auditable and visible in UI and API/client contracts.

**FR-4: Manage Global Providers Aggregate.** Authorized administrators can configure provider records, model options, enablement, and provider capability metadata needed for Agent selection.

- Disabled providers/models cannot be newly selected.
- Existing Agents using a disabled selection cannot be activated or called until reconfigured, apart from documented read-only migration inspection.
- Provider changes are audited without exposing secrets in logs, APIs, UI, or Audit Evidence.

**FR-5: Select Provider And Model Per Agent.** Agent Administrators can select a Provider and model for `hexa` from the governed catalog.

- Validate enablement and usability before activation.
- Retain enough provider/model identity to explain every generated version.
- Selection changes affect future calls only and never rewrite history.

**FR-6: Configure Response Mode.** Agent Administrators can choose Automatic or Confirmation Response Mode.

- Automatic mode posts successful responses after authorization and generation.
- Confirmation mode creates proposals outside the Conversation and never posts unapproved content.
- Mode changes affect future calls only.

**FR-7: Configure Approver Policy.** Agent Administrators define all Approvers through policy sources such as Conversation owner, caller, predefined Parties, or tenant roles.

- Use the policy for edit, regeneration, approval, rejection, abandonment, and expiry-resolution authorization.
- Reject unauthorized approval actions.
- Disclose the authorizing policy source according to a defined user-visible, operator-only, redacted, or omitted category.
- Record the policy basis for each decision.
- Apply the same disclosure category through UI and API/client contracts.

**FR-8: Call Agent From Conversation.** Authorized Conversation Participants can explicitly call `hexa` from a Source Conversation with a prompt or request.

- Require Source Conversation access and Agent-call permission.
- Reject unauthorized calls before Provider invocation.
- Record caller, Agent, Source Conversation, request timestamp, and response mode.

**FR-9: Build V1 Conversation Context.** Supply the Agent with Conversation Context according to the configured policy.

- Use Conversation Context only; exclude long-term memory, project/folder content, tools, and external-channel content.
- Use the complete Source Conversation when it fits the safe model budget.
- Fail closed before Provider invocation when it does not fit; never truncate, summarize, window, or otherwise reduce it.
- Record complete-context use or blocking, the policy version/identifier, and safe context metadata.
- If complete context cannot be safely loaded or sent, create no Provider work, proposal, or Conversation Message.

**FR-10: Handle Generation Failure.** Handle Provider failure, timeout, disabled provider/model state, invalid context, Content Safety Policy failure, and policy failure without posting incomplete or unsafe content.

- Expose authorized failure status and Audit Evidence.
- Create no Conversation Message.
- In Confirmation Mode, create no proposal; retained incomplete content belongs only in a separate non-approvable failure record.

**FR-11: Post Automatic Response.** In Automatic Mode, post successful generated content to the Source Conversation as a message attributed to the Agent Party.

- Reference the Agent Call or equivalent trace identifier.
- Never attribute the message to the caller.
- Link caller, Agent, Provider/model, Conversation, generated content, and posted message in Audit Evidence.

**FR-12: Prevent Automatic Posting When Policy Fails.** Block automatic posting when authorization, lifecycle, provider/model, Party identity, Conversation access, context policy, content safety, or generation status is invalid.

- Create no message after a required policy or output-safety failure.
- Expose a safe authorized failure reason.
- Distinguish authorization, context-policy, content-safety, Provider/runtime, and posting failures in audit.

**FR-13: Create Proposed Agent Reply.** In Confirmation Mode, successful generation creates a proposal linked to the Source Conversation and Agent Call.

- A proposal is not a Conversation Message.
- Record caller, Agent, Conversation, generated version, Provider/model, response mode, and state.
- Let authorized Approvers discover pending work through an in-product count, queue, and Conversation status entry.
- Exclude email, push, and external-channel notifications from V1.

**FR-14: Preserve All Proposal Versions.** Preserve every generated, edited, and regenerated proposal-content version.

- Editing creates a new immutable version entry.
- Regeneration creates a new generated version without deleting history.
- Approval identifies exactly which version was posted.

**FR-15: Edit Proposed Reply.** Authorized Approvers can edit proposal content before approval.

- Only authorized Approvers may edit.
- Preserve the prior version and edit author.
- Keep edited content outside the Conversation until approval.

**FR-16: Regenerate Proposed Reply.** Authorized Approvers can request regeneration before approval.

- Use the same Source Conversation and Agent configuration unless an explicit configuration-version change is recorded.
- Preserve prior versions and add a new generated version.
- Block regeneration after a terminal state.

**FR-17: Approve Proposed Reply.** Authorized Approvers can approve a selected version, causing it to be posted as `hexa`.

- Post exactly the approved version.
- Attribute it to the Agent Party.
- Link version, Approver, timestamp, and posted message in Audit Evidence.

**FR-18: Reject, Abandon, Or Expire Proposed Reply.** Authorized Approvers or system policy can move a proposal to one of those terminal states.

- Terminal proposals cannot be approved or posted and retain all content versions.
- Default expiry is 24 hours; administrators can configure 1 hour through 30 days for future proposals only.
- A durable Dapr Workflow timer expires a non-terminal proposal at or after stored `ExpiresAt`; later policy changes do not alter it.
- Expose expiry behavior and `ExpiresAt` through UI and API/client contracts.

**FR-19: Enforce Tenant Isolation.** Enforce tenant isolation across Agent and Provider configuration, calls, context, proposals, posting, and Audit Evidence.

- A Party cannot call, inspect, approve, or post across tenants.
- Provider/model and Agent configuration cannot leak across tenants unless explicitly platform-scoped and authorized.
- Audit/status queries return tenant-authorized records only.

**FR-20: Enforce Role And Policy Authorization.** Authorize Agent/provider administration, calling, proposal discovery and actions, posting, and audit inspection.

- Fail authorization before Provider invocation or posting.
- Apply the same rules through UI and API/client contracts.
- Audit decisions sufficiently to explain denial/approval without leaking sensitive content.

**FR-21: Fail Closed On Dependency Uncertainty.** Fail closed when Party, Conversation, Provider, Agent, tenant-access, or approval-policy state is missing, stale, ambiguous, disabled, or unavailable.

- Missing/stale Conversation access prevents calls and approval posting.
- Missing/disabled Agent Party identity prevents posting; missing Provider/model state prevents generation.
- A critical dependency is not implementation-ready until all nine §8 commitment fields exist.
- `Uncommitted` status, a missing target, or a missing compatibility verification command blocks consuming stories from `ready-for-dev`.

**FR-22: Provide Admin UI.** Provide authorized management of Providers, `hexa`, lifecycle, response/approver policy, and operational/proposal status.

- Enforce the same authorization as API/client contracts.
- Never expose Provider secrets.
- Distinguish active, disabled, invalid, pending, failed, and expired states.
- Meet NFR-13 accessibility/localization/responsive safety and NFR-14 interaction performance.

**FR-23: Provide API And Client Contracts.** Expose stable contracts for Provider administration, Agent configuration/calls, proposal workflow, status, and audit.

- Hide raw EventStore, aggregate, projection, and provider-SDK mechanics.
- Return structured automation-suitable success/error results.
- Evolve JSON objects additively within V1.
- Public enums start with `Unknown = 0`; do not reuse an existing value's meaning.
- Remove, rename, or semantically reuse no public member/value within V1.
- Require a new major package/API version plus consumer-compatibility tests for breaking changes.

**FR-24: Capture Agent Audit Evidence.** Capture evidence for configuration, calls, generation, all proposal versions/actions/states, automatic posts, and final messages.

- Trace every posted response to caller, Agent, Conversation, Provider/model, generated content, and approval path.
- Preserve every proposal-content version.
- Record Content Safety and Conversation Context policy behavior and identifiers where available.
- Permit authorized queries without exposing other tenants or Provider secrets.

**FR-25: Expose Operational Status.** Expose Agent and Provider/model readiness, recent call outcomes, proposal queues, generation failures, approval completion, and posting outcomes.

- Show administrators whether `hexa` is callable.
- Distinguish configuration, authorization, context-policy, safety, Provider, generation, approval, and posting outcomes.
- Support launch monitoring for adoption and approval-workflow metrics.

**FR-26: Configure Content Safety And Prompt Policy.** Authorized administrators or release operators can define the active Content Safety Policy.

- Block production/production-like enablement without an active policy.
- Define prompt constraints, blocked/restricted output, failure handling, and audit treatment.
- Audit changes and apply them to future calls only.
- Apply the same policy to both response modes unless one is stricter.
- Always block child sexual abuse/exploitation; credible imminent-serious-harm threats or instructions; suicide/self-harm encouragement or instruction; credential theft, malware, unauthorized compromise; secrets/private credentials; unauthorized personal/Conversation data; and attempts to bypass tenant, authorization, audit, retention, or safety controls.
- Require an explicitly permitted tenant use case plus Confirmation Mode for restricted hate, harassment, sexual, violent, illegal-activity, or sensitive-personal content.
- Never weaken policy during an in-progress retry.

**FR-27: Enforce Safety Before Provider And Conversation Side Effects.** Check the prompt and complete authorized context before Provider invocation, then check output before proposal or Conversation side effects.

- Unsafe output cannot be posted or become approvable.
- Record safe authorized failure status/evidence without impermissible display.
- Approvers cannot override safety failure.
- Both input and output checks are mandatory at their respective boundaries.

**FR-28: Define Launch Readiness Controls.** Require fixed metric thresholds, latency targets, full-context behavior, cost controls, audit governance, NFR-11–NFR-14, dependency commitments, and §11 evidence authority.

- Permit controlled production-like qualification only after safety/context policies, cost controls, audit governance, and dependency commitments are active; qualification access does not authorize production.
- Block production until `RQ-1` records READY from live evidence and launch metrics.
- Enforce per-tenant monthly and per-call cost caps: warn at 80%, fail closed at 100%, atomically reserve maximum estimated cost before invocation, reconcile actual usage, release unused reservation only after proving no usage, reuse it for eligible retries, and block when pricing/budget state is missing.
- Meet automatic call-to-post and confirmation call-to-proposal p95 ≤ 60 s and p99 ≤ 120 s; approval-to-post p95 ≤ 10 s and p99 ≤ 30 s.
- Complete pre-Provider authorization/policy/budget/context rejection at p95 ≤ 2 s; each performance gate needs at least 30 production-like executions.
- Require live Evidence Levels 4 and 5; lower levels, skips, placeholders, and conditional results cannot independently establish readiness.

**Total FRs: 28**

### Non-Functional Requirements

**NFR-1 Security:** Agent configuration, Provider administration, Agent Calls, proposal actions, posting, and audit inspection must enforce tenant and Party authorization before side effects.

**NFR-2 Privacy:** Conversation Context, proposal content, and Audit Evidence must not leak across tenants or unauthorized Parties.

**NFR-3 Reliability:** Calls must never create partial Conversation Messages on failure; confirmation workflows must not lose generated or edited versions.

**NFR-4 Observability:** Expose enough status to debug configuration errors, Provider failures, authorization denials, pending-approval bottlenecks, and posting failures.

**NFR-5 Auditability:** Preserve every generated and edited proposal version and link final posts to their source call and approval path.

**NFR-6 Provider Safety:** Provider secrets must be write-only or secret-backed where applicable and never appear in logs, status payloads, audit records, or UI.

**NFR-7 Content Safety:** An active Content Safety Policy must govern generation before content can cause Conversation side effects.

**NFR-8 Context Bounds:** Never truncate, summarize, window, or otherwise reduce Conversation Context; oversized Conversations fail closed before Provider invocation.

**NFR-9 Performance:** Automatic call-to-post and confirmation call-to-proposal p95 ≤ 60 s/p99 ≤ 120 s; approval-to-post p95 ≤ 10 s/p99 ≤ 30 s; fast pre-Provider rejection p95 ≤ 2 s; at least 30 production-like executions per gate.

**NFR-10 Cost Control:** Hard per-tenant monthly and per-call caps warn at 80%, fail closed at 100%, and use atomic reservation plus reconciliation; reporting-only controls are insufficient.

**NFR-11 Availability And Recovery:** EventStore business state has RPO 0. Restart/replay cannot duplicate Provider attempts, proposal versions, timers, reservations, or posts. A production-like recovery exercise restores processing within 15 minutes and preserves every terminal decision.

**NFR-12 Capacity And Backpressure:** Before enablement, record numeric per-tenant/system concurrency, queue-depth, and backpressure limits. Exceeding them safely queues or rejects before Provider invocation while preserving tenant fairness and cost caps. The release profile and limits belong in the readiness registry.

**NFR-13 Accessible, Localizable, Responsive UI:** Binding UX spines require WCAG 2.2 AA behavior, whole-string localization with English/French key parity, and fail-closed high-impact actions when required context cannot fit the viewport.

**NFR-14 UI Interaction Performance:** In production-like conditions, usable non-loading page p95 ≤ 2.5 s; authoritative pending acknowledgement p95 ≤ 500 ms; projection-visible terminal change rendered and announced p95 ≤ 2 s. Each gate needs at least 30 executions and yields `InsufficientEvidence` when timestamps/samples are missing.

**Total NFRs: 14**

### Additional Requirements

- V1 exposes only `hexa`; generalized internal Agent structures are allowed, but multiple named Agents are not product scope.
- Invocation is exclusively the Conversation-owned **Call hexa** action; mentions, commands, ambient triggers, external channels, tools, memory, non-conversation retrieval, and business actions are excluded.
- Agents owns durable proposal state/read models; Dapr Workflow executes durable timers/work but is not the domain system of record.
- Proposal expiry defaults to 24 hours and is configurable from 1 hour through 30 days for future proposals.
- Sensitive Agent content is retained for 365 days after terminal state unless legal hold applies. Export must be tenant-scoped, encrypted, time-limited, manifested, and audited.
- EventStore history is immutable. Retention/deletion cryptographically erases or redacts protected payloads and purges affected projections while retaining a support-safe tombstone; completion requires restrictive confirmation across protection and projection surfaces.
- Posted Conversation Messages remain governed by Hexalith.Conversations retention.
- Critical external dependency records require owner, repository, artifact, target version/commit, integration date, compatibility contract/test plus command, Evidence Level, accepted status, and consuming stories.
- Initial critical dependency scope: `EXT-CONV-AI-1`, `EXT-HOST-1`, `EXT-PROVIDER-1`, `EXT-SAFETY-1`, `EXT-TOKEN-1`, `EXT-SECRETS-1`, and `EXT-TOPOLOGY-1`.
- `EXT-CONV-AI-1` requires Conversations to publish idempotent, typed, cross-tenant-safe AI-participant membership contracts; Agents must not write Conversation streams directly.
- The Global Providers Aggregate owns identifiers, enablement, capabilities/limits, secret references, versioned pricing metadata, and `CapabilityVersion`.
- Evidence Levels 1–5 are normative. Production-like readiness requires live Levels 4 and 5 for runtime, authorization, isolation, Provider, safety, Conversations, audit, and topology behavior.
- Every qualification metric needs a versioned measurement contract specifying authoritative events/timestamps, calculation method, sample/window/cohort rules, late/missing-data handling, and `InsufficientEvidence` conditions.
- Deterministic fixtures prove metric calculators only; rolling-window/cohort attainment and READY/NOT READY remain the post-implementation `RQ-1` gate.
- Binding launch metrics include at least one enabled launch tenant, ≥20% Agent-call adoption across ≥50 eligible Conversations over 30 days, and ≥95% proposal terminality within 26 hours with expiry ≤20%, posting failures ≤2%, human resolution ≥70%, and audit completeness 100%.
- The addendum is contextual landscape material only and cannot override the PRD.

### PRD Completeness Assessment

The PRD is structurally strong and unusually testable: it has 28 stable FRs, 14 quantified NFRs, explicit non-goals, scope boundaries, dependency-readiness rules, normative evidence levels, metric contracts, success and counter-metrics, and 13 resolved binding decisions. Functional and non-functional requirements are internally aligned around fail-closed authorization, immutable evidence, full-context behavior, safety gates, cost enforcement, and production-like proof.

Completeness is conditional on companion planning artifacts. The PRD intentionally delegates concrete dependency targets/status/verification commands to the external dependency register, numeric capacity/backpressure values to the readiness registry, detailed UI behavior to the final UX spines, and implementation mechanics to architecture. Any missing or unaccepted dependency commitment remains an explicit readiness blocker rather than a PRD ambiguity. These delegated obligations must be validated against the architecture and epics in subsequent steps.

## Epic Coverage Validation

The epics document explicitly maps every PRD FR to Epics 1–4 as historical delivery coverage and declares Epic 5 the sole forward implementation plan. Story 5.18 supplies final verification coverage for FR1–FR28; the matrix below also identifies substantive Epic 5 implementation stories rather than relying only on that umbrella verification story.

### Coverage Matrix

| FR | PRD Requirement | Epic Coverage | Forward Epic 5 Stories | Status |
| --- | --- | --- | --- | --- |
| FR-1 | Configure `hexa` | Epic 1 | 5.2, 5.3, 5.17, 5.18 | ✓ Covered |
| FR-2 | Link Agent to Party identity | Epic 1 | 5.4, 5.11, 5.18 | ✓ Covered |
| FR-3 | Manage Agent lifecycle | Epic 1 | 5.2, 5.3, 5.17, 5.18 | ✓ Covered |
| FR-4 | Manage Global Providers Aggregate | Epic 1 | 5.3, 5.6, 5.10, 5.17, 5.18 | ✓ Covered |
| FR-5 | Select Provider/model per Agent | Epic 1 | 5.3, 5.6, 5.10, 5.17, 5.18 | ✓ Covered |
| FR-6 | Configure response mode | Epic 1 | 5.3, 5.18 | ✓ Covered |
| FR-7 | Configure Approver Policy | Epic 1 | 5.4, 5.18 | ✓ Covered |
| FR-8 | Call Agent from Conversation | Epic 2 | 5.4, 5.5, 5.7, 5.8, 5.12, 5.18 | ✓ Covered |
| FR-9 | Build complete V1 Conversation Context | Epic 2 | 5.6, 5.7, 5.8, 5.18 | ✓ Covered |
| FR-10 | Handle generation failure | Epic 2 | 5.5–5.10, 5.12, 5.18 | ✓ Covered |
| FR-11 | Post automatic response | Epic 2 | 5.7, 5.11, 5.12, 5.18 | ✓ Covered |
| FR-12 | Prevent posting when policy fails | Epic 2 | 5.7, 5.9, 5.11, 5.12, 5.18 | ✓ Covered |
| FR-13 | Create Proposed Agent Reply | Epic 3 | 5.5, 5.7, 5.12–5.14, 5.18 | ✓ Covered |
| FR-14 | Preserve all proposal versions | Epic 3 | 5.5, 5.7, 5.14, 5.18 | ✓ Covered |
| FR-15 | Edit proposed reply | Epic 3 | 5.5, 5.7, 5.14, 5.18 | ✓ Covered |
| FR-16 | Regenerate proposed reply | Epic 3 | 5.5–5.7, 5.14, 5.18 | ✓ Covered |
| FR-17 | Approve selected proposal version | Epic 3 | 5.5, 5.7, 5.11, 5.14, 5.18 | ✓ Covered |
| FR-18 | Reject, abandon, or expire proposal | Epic 3 | 5.5, 5.7, 5.13–5.15, 5.17, 5.18 | ✓ Covered |
| FR-19 | Enforce tenant isolation | Epic 2 | 5.4, 5.8, 5.11, 5.12, 5.16, 5.18 | ✓ Covered |
| FR-20 | Enforce role/policy authorization | Epic 2 | 5.4, 5.11–5.14, 5.16, 5.18 | ✓ Covered |
| FR-21 | Fail closed on dependency uncertainty | Epic 2 | 5.4, 5.6, 5.8, 5.11, 5.18 | ✓ Covered |
| FR-22 | Provide admin UI | Epic 4 | 5.3, 5.12–5.14, 5.17, 5.18 | ✓ Covered |
| FR-23 | Provide stable API/client contracts | Epic 4 | 5.1, 5.3, 5.12, 5.14, 5.18 | ✓ Covered |
| FR-24 | Capture Agent Audit Evidence | Epic 4 | 5.5–5.7, 5.10–5.11, 5.14–5.16, 5.18 | ✓ Covered |
| FR-25 | Expose operational status | Epic 4 | 5.2–5.3, 5.5, 5.7, 5.10, 5.12–5.13, 5.15–5.18 | ✓ Covered |
| FR-26 | Configure Content Safety Policy | Epic 1 | 5.9, 5.17, 5.18 | ✓ Covered |
| FR-27 | Enforce safety before side effects | Epic 2 | 5.9, 5.17, 5.18 | ✓ Covered |
| FR-28 | Define launch-readiness controls | Epic 4 | 5.2, 5.9–5.10, 5.15–5.18 | ✓ Covered |

### Missing Requirements

No PRD Functional Requirement is absent from the epics document. No epic-only FR identifier exists outside the PRD's FR1–FR28 range.

Coverage is a traceability result only. The epics document itself states that Epics 1–4 are historical and do not establish production readiness; later steps must assess whether the authoritative Epic 5 stories are internally sound, sequenced correctly, and aligned with architecture and UX.

### Coverage Statistics

- Total PRD FRs: 28
- FRs represented in the epic coverage map: 28
- FRs with a forward Epic 5 path: 28
- Missing FRs: 0
- Extra epic-only FRs: 0
- Coverage: 100%

## UX Alignment Assessment

### UX Document Status

**Found.** The approved UX input consists of final `DESIGN.md` and `EXPERIENCE.md` spines, both updated 2026-08-01. They explicitly inherit FrontComposer and Fluent UI Blazor V5, separate visual and behavioral authority, and declare that the final spines win over mockups or source-derived sketches.

### UX ↔ PRD Alignment

Strong alignment exists across the four PRD journeys and the additional launch-governance journey:

- Setup covers Agent identity, provider/model, instructions, response mode, approver policy, lifecycle, content safety, cost, and callability.
- **Call hexa** is the sole V1 invocation path; mentions, commands, ambient triggers, tools, memory, project/folder retrieval, external channels, and other Agents remain excluded.
- Automatic and confirmation modes remain distinct; a proposal is never presented as a Conversation Message, approval remains distinct from posting, and immutable version history is visible.
- Tenant-safe denial, provider-secret safety, complete-context-or-blocked behavior, two-stage safety, hard budget state, proposal expiry, launch evidence, retention/legal hold/export/deletion, and authoritative projection confirmation are represented in UX.
- Responsive failure is restrictive, statuses combine color/icon/text, high-impact flows are keyboard-operable, and FrontComposer FC-LYT/FC-TBL/FC-A11Y/FC-L10N dependencies are identified.

### UX ↔ Architecture Alignment

Architecture substantially supports the UX through AD-11/AD-12 context and authorization gates, AD-15 public UI/API parity, AD-16 platform-owned composition, AD-17 UI/contract conformance, AD-20 safety, AD-21 cost controls, AD-22 audit governance, explicit UI/UI-test projects, and FrontComposer/Fluent inheritance. The capability map accounts for setup, invocation, proposals, status, audit, cost, safety, and readiness surfaces.

### Alignment Issues

1. **High — NFR-14 UI performance contract is absent from both UX spines and architecture.** The PRD requires page usability p95 ≤ 2.5 s, pending acknowledgement p95 ≤ 500 ms, and projection-visible terminal update/announcement p95 ≤ 2 s, with at least 30 executions and `InsufficientEvidence` behavior. UX refers generically to latency/sample sufficiency, and architecture has no browser timestamp, telemetry, measurement, or conformance seam for these UI-specific gates.

2. **High — NFR-13 normative accessibility/localization details are weakened in UX/architecture.** UX contains strong accessibility behavior and whole-string localization, but neither selected UX spine states WCAG 2.2 AA or English/French key parity. Architecture mentions FrontComposer conformance but does not bind these exact launch requirements or their verification path.

3. **Medium — failed-generation proposal wording is ambiguous.** `EXPERIENCE.md` says generation failure creates no approvable proposal “unless complete generated content exists and is explicitly marked audit-only.” The PRD requires failed/incomplete content to live only in a separate, non-approvable failure record. “Audit-only” must never be an approvable proposal state.

4. **Medium — Fluent UI baseline drift.** Architecture pins `5.0.0-rc.3-26138.1`, while the loaded FrontComposer project context requires exact `5.0.0-rc.4-26180.1`. Because UX inherits FrontComposer rather than directly owning the component package, architecture should bind to the current FrontComposer-compatible pin instead of the stale baseline.

5. **Low — high-risk action concurrency is not architecturally resolved.** UX defaults to one high-risk side effect per user/session unless architecture explicitly permits concurrency. Architecture covers durable concurrency/idempotency but does not decide the UI/session command policy. The restrictive UX default is implementable, but the decision and test boundary should be explicit.

6. **Low — approval Success semantics require care.** `DESIGN.md` permits Success for an approved version, while both PRD and UX behavior correctly distinguish `approved`, `posting pending`, and `posted`. Implementations must ensure Success on approval never implies that the Conversation Message exists.

### Warnings

- UX alignment is strong enough to guide implementation, but the missing NFR-13/NFR-14 normative gates prevent the UX package from independently proving launch readiness.
- Correct the failed-generation wording before using the UX state table as acceptance authority.
- Reconcile the FrontComposer/Fluent package baseline before UI implementation or package-consumer validation.

## Epic Quality Review

The epics document contains 5 epics and 44 stories. Its BDD formatting, deterministic/idempotent acceptance criteria, and explicit Epic 5 dependency declarations are generally strong. No Epic 5 story depends on a later-numbered internal story, database/table pre-creation is avoided, and Story 1.1 correctly uses the architecture Structural Seed because no starter template is prescribed. Those strengths do not cure the structural defects below.

### Epic Compliance Summary

| Epic | User-value focus | Standalone value as claimed | Story sizing | Forward-dependency result | Traceability |
| --- | --- | --- | --- | --- | --- |
| 1 — Tenant Agent Setup And Governance | Strong administrator outcome | Fails: live EventStore, access, and readiness bindings are deferred to Epic 5 | Mostly acceptable | Implicitly depends on later Epic 5 | FR coverage present |
| 2 — Safe Conversation Invocation And Automatic Replies | Strong participant outcome | Fails: live workflow/context/safety/provider/membership/posting arrive in Epic 5 | Mostly acceptable historically | Implicitly depends on later Epic 5 and `CONV-AI-1` | FR coverage present |
| 3 — Proposal Review And Approval Workflow | Strong Approver outcome | Fails: live proposal read models, workflow, posting, queue, and detail arrive in Epic 5 | Mostly acceptable historically | Implicitly depends on later Epic 5 | FR coverage present |
| 4 — Operational Visibility, Audit, Integration, And Launch Readiness | Strong operator/integrator outcome | Fails: final policies, governance, metrics, and conformance arrive in Epic 5 | Story 4.5 is broad | Implicitly depends on later Epic 5 | FR coverage present |
| 5 — Production Binding And Live Conformance | Mixed operator value and technical hardening | Depends only on prior work, but retrofits the value claimed by all earlier epics | Multiple oversized stories | Internal ordering is valid; external blocker unresolved | FR complete; NFR incomplete |

### 🔴 Critical Violations

#### C1. Earlier epics depend on a later technical correction epic to deliver their stated value

The Course Correction Authority says Epics 1–4 are historical skeleton evidence, do not establish production readiness, and must not be interpreted as implementing live seams; Epic 5 is the only forward plan. Consequently, Epic 1 setup is not durably usable until Stories 5.3–5.4, Epic 2 cannot execute live calls until Stories 5.7–5.12, Epic 3 cannot provide a live proposal workflow until Stories 5.5/5.7/5.11/5.13–5.14, and Epic 4 cannot provide final governance/readiness until Stories 5.17–5.18.

This violates epic independence: the user value claimed by Epics 1–4 requires Epic 5. Epic 5 is substantially a technical milestone (“binding and conformance”) spanning the entire system rather than one coherent increment of user value.

**Remediation:** Recast the forward plan as independently valuable production vertical slices. Either reopen/rebase Epics 1–4 with their required live binding/evidence stories, or split Epic 5 into outcome epics such as callable setup, safe automatic interaction, governed proposal resolution, and operable launch governance. Preserve the old text in an explicitly archived historical section rather than leaving it in the executable plan.

#### C2. Critical external dependencies have no complete commitment register

The PRD names seven initial critical dependency entries and requires nine fields for each. No external dependency register or readiness registry artifact was discovered. `CONV-AI-1` appears only as prose: owner repository and required API are named, but target version/commit, integration date, accepted status, complete compatibility command, Evidence Level commitment, and consuming-story acceptance are not recorded in a dedicated authoritative entry. Stories 5.11 and 5.18 are therefore blocked by the PRD's own FR-21 rule. The other `EXT-*` commitments are likewise not evidenced.

**Remediation:** Create the authoritative dependency register before marking any consuming story `ready-for-dev`. Populate all nine fields for `EXT-CONV-AI-1`, `EXT-HOST-1`, `EXT-PROVIDER-1`, `EXT-SAFETY-1`, `EXT-TOKEN-1`, `EXT-SECRETS-1`, and `EXT-TOPOLOGY-1`; bind each to exact Story 5.x consumers and executable compatibility commands.

#### C3. NFR-11 through NFR-14 are absent from formal epic/story traceability

The epics Requirements Inventory stops at NFR10, Epic 5 claims “NFR1–NFR10,” and Story 5.18 also verifies only NFR1–NFR10. The current PRD has 14 NFRs. This leaves no formal implementation/verification path for:

- NFR-11: RPO 0, no duplicate effects after restart/replay, and recovery within 15 minutes.
- NFR-12: numeric tenant/system concurrency, queue-depth and backpressure limits, typed pre-Provider queue/reject behavior, and cross-tenant fairness.
- NFR-13: WCAG 2.2 AA, English/French key parity, and restrictive viewport behavior.
- NFR-14: exact UI interaction p95 thresholds, ≥30-run evidence, authoritative timestamps, and `InsufficientEvidence`.

Some stories partially address recovery and accessibility, but the requirements are neither inventoried nor completely traced. Capacity/backpressure and UI timing evidence are substantively missing.

**Remediation:** Add NFR11–NFR14 to the inventory and traceability matrix. Strengthen/split stories so every clause has an owner, acceptance scenario, evidence level, and command. Story 5.18 must explicitly verify NFR1–NFR14.

#### C4. Story 5.18 is an epic-sized story

Story 5.18 asks one story to run a production-like topology across EventStore, workflow recovery, identity, complete context, safety, Provider/cost, Conversations, proposals, audit governance, UI accessibility, topology, numerous adversarial paths, all live metrics, and a new readiness report mapping every FR/NFR/UX-DR/AD/dependency/story. It cannot be independently estimated, implemented, reviewed, or failed locally as one normal story.

**Remediation:** Split it into bounded evidence stories: live component lanes, cross-system happy path, tenant/auth adversarial lane, retry/concurrency/cost lane, governance/deletion lane, UI accessibility/performance lane, metric qualification, and a final assessment-only story consuming those completed evidence packages.

### 🟠 Major Issues

#### M1. Multiple Epic 5 stories are oversized bundles

- **5.3** combines two aggregate command/query bindings, read models, authorization, replay, packaging, and integration evidence.
- **5.4** combines Tenants projection correctness, Parties identity binding, and Conversation authority/Approver resolution.
- **5.5** combines interaction, proposal, queue/status, immutable-version, and audit read-model families.
- **5.7** implements the entire automatic and confirmation Dapr Workflow lifecycle, all waits/timers/retries/restart behavior, and EventStore command coordination.
- **5.14** combines the proposal workspace, all proposal actions, posting integration, expiry races, and the full accessibility/responsive contract.
- **5.17** combines context, safety, cost, retention, legal hold, export, deletion, five operator surfaces, latency metrics, SM-2/SM-3, and launch status.

**Remediation:** Split along one durable behavior or one independently testable surface per story. Keep orchestration contracts separate from each live activity binding; keep policy authoring separate from measurement/readiness presentation.

#### M2. Historical acceptance criteria conflict with current binding authority

The front-matter course correction says later documents override conflicting Epics 1–4 criteria, but executable-looking stale criteria remain:

- Story 2.3 permits an “approved bounded-context behavior,” while V1 now requires full Conversation or fail closed.
- Story 2.6 permits mention, command, action, or participant affordance, while **Call hexa** is the sole entry.
- Story 4.4 permits reporting-only cost monitoring or accepted risk, while hard reservations/caps are mandatory.
- Story 4.5 asks for Agent Framework workflow/session restore, MCP/A2A/tool schemas, and “where applicable” evidence, all excluded or non-authoritative in the corrected V1.
- Story 4.2 treats retention/legal hold/export/deletion as unresolved even though the Decision Register resolves them.

**Remediation:** Move Epics 1–4 into a clearly non-executable historical appendix or attach machine-readable `superseded` markers and direct replacements to each conflicting story/criterion. Do not require implementers to reconcile authority from prose precedence.

#### M3. Launch gate criteria rely on an undiscovered readiness registry

Story 5.2 uses phrases such as “all current gates” and expects gate versions, timestamps, evidence references, and tenant scope, while the PRD delegates numeric capacity/backpressure and dependency commitments to registries that were not found. Without an authoritative schema/store, the criteria are not independently executable.

**Remediation:** Define the readiness-record contract and persistence/query authority before Story 5.2, including gate identity, version, freshness, tenant scope, required evidence level, evidence reference, evaluation time, state, owner, and invalidation rules.

#### M4. Quality/CI gates arrive too late for a greenfield module

Story 1.1 establishes build and placeholder tests, but package-consumer, topology, and CI quality gates are deferred to Story 5.1 after four historical epics. A greenfield module should establish the executable CI baseline before feature stories accumulate.

**Remediation:** Pull source build, package-consumer, architecture-boundary, and basic CI gates into the initial shell story; leave production topology conformance in the later platform-binding slice.

### 🟡 Minor Concerns

- Epics 1–4 do not declare explicit `Dependencies` and `Traceability` blocks per story, unlike Epic 5; dependency auditing relies on story order and narrative.
- Many acceptance scenarios contain several independently failing outcomes under long `And` chains. Split them into named scenarios or test cases so failures identify one contract.
- Requirement identifiers mix `FR1`/`FR-1` and `NFR1`/`NFR-1`. Normalize display while preserving stable IDs.
- The plan contains no relational database/table pre-creation violation; EventStore aggregates and read models are introduced near use, and Story 1.1 explicitly forbids pre-creating future entities/events.

### Dependency Assessment

- **Internal forward references:** none found in explicit Epic 5 dependency declarations.
- **Historical-to-future structural dependency:** critical; Epics 1–4 require Epic 5 for their stated live outcomes.
- **External dependency:** `CONV-AI-1` blocks Stories 5.11 and 5.18 and lacks a complete commitment record.
- **Story ordering:** Epic 5 declarations are topologically ordered; Story 5.13 intentionally depends only on earlier 5.5/5.7 even though it follows 5.12.

### Recommended Plan Repair Order

1. Create and accept the external dependency and readiness registries.
2. Add NFR11–NFR14 to formal traceability and assign complete stories/evidence paths.
3. Restructure Epic 5 into independently valuable production slices; split Stories 5.7, 5.17, and 5.18 first.
4. Isolate superseded Epics 1–4 criteria from the executable forward plan.
5. Move baseline CI/package/architecture gates to the first forward implementation slice.

## Summary and Recommendations

### Overall Readiness Status

## NOT READY

The planning package is **not ready to enter Phase 4 implementation**. Functional traceability is complete at 28/28 FRs, and the core PRD/UX/architecture direction is coherent, but the executable implementation plan does not yet satisfy its own dependency, NFR, sizing, and authority rules.

This status does not treat missing post-implementation Level 4–5 evidence as a reason to avoid all development. Live evidence and `RQ-1` belong to release qualification after the relevant implementation exists. The current blockers are earlier: stories cannot be responsibly admitted to implementation while critical dependency commitments, NFR11–NFR14 work, authoritative readiness records, and independently executable story boundaries are missing.

### Critical Issues Requiring Immediate Action

1. **Repair the forward-plan structure.** Epics 1–4 claim user outcomes whose live implementation is deferred to the later technical Epic 5. Rebase the forward plan into production vertical slices or reopen the earlier outcome epics.
2. **Commit every critical external dependency.** Create the required nine-field register for all seven `EXT-*` entries. `CONV-AI-1` currently blocks Stories 5.11 and 5.18.
3. **Restore NFR completeness.** Add NFR11–NFR14 to the epic inventory, story traceability, acceptance criteria, and Story 5.18 evidence scope.
4. **Decompose Story 5.18.** Its all-system conformance and assessment scope is an epic, not an independently completable story.

### Recommended Next Steps

1. Create an authoritative external dependency register and readiness registry with owners, targets, dates, commands, evidence levels, state/freshness, consumers, and invalidation rules.
2. Amend UX and architecture for exact WCAG 2.2 AA, English/French parity, UI p95 timing and evidence contracts, restrictive `InsufficientEvidence`, and the current FrontComposer/Fluent package baseline.
3. Correct the failed-generation audit-only wording and make approval-versus-posted status semantics unambiguous.
4. Add implementation/evidence stories for RPO 0 and ≤15-minute recovery, numeric capacity/backpressure/fairness, NFR-13 conformance, and NFR-14 browser interaction performance.
5. Split Epic 5 into independently valuable vertical slices; split Stories 5.3–5.5, 5.7, 5.14, 5.17, and 5.18 into bounded behaviors or evidence lanes.
6. Isolate superseded Epics 1–4 criteria in a clearly historical, non-executable section with direct replacement links.
7. Move baseline source build, package-consumer, architecture-boundary, and CI gates into the first forward implementation slice.
8. Re-run implementation readiness after the corrected artifacts and dependency commitments are approved. Run `RQ-1` only after implementation has produced the required live evidence.

### Final Note

This assessment records **17 issue entries across five categories**: planning authority/structure, external dependency readiness, requirement traceability, UX/architecture alignment, and epic/story quality. Several entries intentionally cross-reference the same underlying NFR or registry gap from different validation angles. Four are critical blockers.

Do not begin the current Phase 4 plan as written. The strongest assets—the final PRD, complete FR coverage, corrected architecture direction, and detailed UX spines—provide a solid repair base, but the critical issues above must be resolved before stories are admitted as implementation-ready.

**Assessment date:** 2026-08-01  
**Assessor:** Codex using the BMad Implementation Readiness workflow
