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

- `prds/prd-agents-2026-06-23/prd.md`
- `prds/prd-agents-2026-06-23/addendum.md`
- `prds/prd-agents-2026-06-23/reconcile-brief.md`

### Architecture

- `architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md`
- `architecture/architecture-agents-2026-06-23-2/IMPLEMENTATION-CONVENTIONS.md`

### Epics and Stories

- `epics.md`

### UX Design

- `ux-designs/ux-agents-2026-06-23/DESIGN.md`
- `ux-designs/ux-agents-2026-06-23/EXPERIENCE.md`

No whole-versus-sharded duplicates were found. The accepted PRD, architecture,
and UX inputs use grouped folders without `index.md`; review and validation
companions are retained as supporting artifacts but excluded from the canonical
assessment input set.

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
- Existing Agents using a disabled provider/model cannot be activated or called until reconfigured, unless a documented migration state allows temporary read-only inspection.
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
- When the Source Conversation exceeds the selected Provider/model's safe context budget, the system does not silently truncate context. It either fails closed or uses an explicitly approved bounded-context behavior defined by Conversation Context Policy.
- Agent Calls record whether full or bounded context was used, the Conversation Context Policy version or equivalent identifier, and enough context metadata for audit without leaking unrelated tenant data.
- If Conversation Context cannot be loaded or bounded safely, the Agent Call fails closed and no partial or misleading response is posted.

#### FR-10: Handle Generation Failure

The system handles Provider failures, timeout, disabled provider/model state, invalid context, Content Safety Policy failures, and policy failures without posting incomplete or unsafe Agent Responses.

**Consequences (testable):**
- Failed generation creates status and Audit Evidence visible to authorized administrators or callers.
- Failed generation does not create a Conversation Message.
- Failed generation in Confirmation Response Mode does not create an approvable Proposed Agent Reply unless generated content exists and is explicitly marked as failed or incomplete for audit only.

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
- V1 includes an in-product pending-proposal visibility surface for authorized Approvers, including proposal count or status indication.
- If active notifications are not included in the V1 launch, the launch-readiness review explicitly accepts that Approvers must rely on the in-product visibility surface.

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
- Expiry behavior is deterministic and visible through admin UI and API/client contracts. [ASSUMPTION: exact expiry duration is configurable or defined by deployment policy, not hardcoded in this PRD.]

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

#### FR-22: Provide Admin UI

The admin UI allows authorized administrators to manage Global Providers Aggregate entries, configure `hexa`, inspect lifecycle state, configure response and approver policy, and view Agent operation/proposal status.

**Consequences (testable):**
- Admin UI actions enforce the same authorization rules as API/client contracts.
- Admin UI never exposes Provider secrets.
- Admin UI clearly distinguishes active, disabled, invalid, pending proposal, failed call, and expired proposal states.

#### FR-23: Provide API And Client Contracts

The system exposes stable API/client contracts for Provider administration, Agent configuration, Agent Calls, proposal workflow, status inspection, and audit inspection.

**Consequences (testable):**
- API/client contracts do not require callers to use raw EventStore, internal aggregate, internal projection, or provider SDK details.
- API/client contracts return structured success and error results suitable for automation.
- Breaking contract changes are avoided during V1 unless explicitly versioned.

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

#### FR-27: Enforce Safety Before Conversation Side Effects

The system applies Content Safety Policy before generated content becomes a Conversation Message or an approvable Proposed Agent Reply.

**Consequences (testable):**
- Generated content that fails Content Safety Policy cannot be posted automatically.
- Generated content that fails Content Safety Policy cannot become an approvable Proposed Agent Reply.
- Safety failures create authorized status and Audit Evidence without exposing unsafe content in surfaces where policy forbids display.
- Approvers cannot override a Content Safety Policy failure unless the policy explicitly defines an auditable override path.

#### FR-28: Define Launch Readiness Controls

V1 launch readiness requires explicit metric thresholds, latency targets, context-bounding behavior, and cost-control posture.

**Consequences (testable):**
- SM-2 and SM-3 cannot be used for launch readiness until each defines its numerator, denominator, target, measurement window, and launch cohort.
- Launch readiness defines latency targets for Automatic Response Mode and Confirmation Response Mode before performance gates are accepted.
- Launch readiness defines whether cost control is enforced through quotas, budgets, provider/model limits, reporting-only monitoring, or an explicitly accepted launch risk.
- Production or production-like generation cannot be enabled until Content Safety Policy, Conversation Context Policy, launch metric thresholds, latency targets, and cost-control posture are recorded.

**Total FRs: 28**

### Non-Functional Requirements

NFR-1: **Security** — Agent configuration, Provider administration, Agent Calls, proposal actions, posting, and audit inspection must enforce tenant and Party authorization before side effects.

NFR-2: **Privacy** — Conversation Context, proposal content, and Audit Evidence must not leak across tenants or unauthorized Parties.

NFR-3: **Reliability** — Agent Calls must never create partial Conversation Messages on failure. Confirmation workflows must not lose generated or edited proposal versions.

NFR-4: **Observability** — The system must expose enough status to debug configuration errors, Provider failures, authorization denials, pending approval bottlenecks, and posting failures.

NFR-5: **Auditability** — Audit Evidence must preserve all generated and edited proposal versions and link final posted responses to their source call and approval path.

NFR-6: **Provider Safety** — Provider secrets must be write-only or secret-backed where applicable and must never appear in logs, status payloads, audit records, or UI display.

NFR-7: **Content Safety** — Agent generation must be governed by an active Content Safety Policy before generated content can create Conversation side effects.

NFR-8: **Context Bounds** — Conversation Context must not be silently truncated; oversized Conversations must fail closed or use approved bounded-context behavior.

NFR-9: **Performance** — V1 launch readiness must define latency targets for Automatic Response Mode and Confirmation Response Mode before production or production-like generation is enabled.

NFR-10: **Cost Control** — V1 launch readiness must define cost-control posture before production or production-like generation is enabled.

**Total NFRs: 10**

### Additional Requirements

The following source sections are retained verbatim because they constrain the
functional and non-functional requirements or establish downstream decision
gates.

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

#### 6. MVP Scope

##### 6.1 In Scope

- `hexa` as the first general-purpose Agent.
- Agent Party identity provisioning or linking through Hexalith.Parties.
- Global Providers Aggregate for governed Provider/model options.
- Per-Agent Provider/model selection.
- Agent Instructions, lifecycle, response mode, and Approver Policy configuration.
- Explicit Conversation-originated Agent Calls.
- Conversation Context Policy that uses full Source Conversation context when it fits and fails closed or uses approved bounded context when it does not.
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

##### 6.2 Out Of Scope For MVP

- Long-term memory through Hexalith.Memories, deferred to V2.
- Configured Agent tools, deferred until governed conversation participation is proven.
- Project/folder activation through future Hexalith.Projects or Hexalith.Folders integration.
- Automatic activation from Conversation changes.
- External channel bots or bridges.
- Business workflow actions beyond adding Agent responses to Conversations.
- Multiple named Agents beyond `hexa`, except where the data model intentionally avoids blocking future Agents. [ASSUMPTION: V1 product behavior exposes only `hexa`, even if implementation uses generalized Agent structures.]
- Fine-grained launch pricing, billing, or monetization.

#### 7. Cross-Cutting Non-Functional Requirements

#### 8. Integration And Dependencies

- **Hexalith.Conversations:** Source Conversation access, Conversation Context loading, and final Conversation Message posting depend on Conversations. Hexalith Agents must not treat unapproved proposals as Conversation Messages.
- **Hexalith.Parties:** Agent identity and Conversation Participant identity depend on Parties. `hexa` must post as a Party identity.
- **Provider Infrastructure:** Provider/model availability depends on the Global Providers Aggregate and the underlying provider integration selected per Agent.
- **Tenant Access:** Tenant isolation and authorization must align with existing Hexalith tenant access patterns and fail closed when tenant state is missing or unavailable.
- **Admin Surface:** The admin UI must use the same capability and authorization model as the API/client contracts.
- **Release Governance:** Production or production-like generation depends on recorded launch-readiness gates for safety, context, metrics, latency, cost, and audit governance.

#### 9. Data Governance And Audit

- Audit Evidence must be tenant-scoped and accessible only to authorized Parties or operators.
- Audit Evidence must preserve every generated, edited, and regenerated proposal version.
- Audit Evidence must link automatic posts and approved posts to the Agent Call, Source Conversation, caller, Agent, Provider/model, response mode, and final Conversation Message.
- Editing a Proposed Agent Reply must never overwrite the prior version.
- Regenerating a Proposed Agent Reply must never delete prior generated or edited versions.
- Rejected, abandoned, and expired proposals remain audit records and cannot later be posted.
- Provider secrets and raw credentials are never audit content.
- Agent audit implementation stories are blocked until retention period, legal hold, export behavior, and deletion behavior are explicitly bound to a named platform policy or a dedicated Agents governance decision.

#### 10. API Contracts And Public Surface

V1 must expose public API/client contracts for these capability areas:

- Provider administration: create/update/list/enable/disable Provider and model options where authorized.
- Agent administration: configure `hexa`, lifecycle, Party identity link, Agent Instructions, Provider/model selection, Response Policy, Approver Policy, Conversation Context Policy, and Content Safety Policy.
- Agent invocation: call `hexa` from a Source Conversation.
- Proposal workflow: list pending proposals, inspect proposal versions, edit, regenerate, approve, reject, abandon, and inspect expiry.
- Status: inspect Agent readiness, Provider readiness, context policy outcome, content safety outcome, Agent Call status, proposal state, and posting outcome.
- Audit: inspect authorized Audit Evidence for Agent Calls, proposal lifecycle, and posted responses.

The public surface must not require consumers to understand internal EventStore stream names, aggregate mechanics, projection internals, or provider SDK details.

#### 11. Success Metrics

**Primary**

- **SM-1: Active tenant adoption** - At least one launch tenant configures `hexa`, enables a Provider/model, satisfies launch-readiness gates, and records successful Agent Calls in production or production-like launch validation. Validates FR-1 through FR-12 and FR-22 through FR-28.
- **SM-2: Conversation adoption** - A defined share of eligible launch Conversations use at least one Agent Call after enablement. Before launch-readiness review, this metric must define eligible Conversation denominator, launch cohort, target percentage, and measurement window. Validates FR-8, FR-9, FR-11, FR-13, and FR-28.
- **SM-3: Approval workflow completion** - In Confirmation Response Mode, a defined share of Proposed Agent Replies reach an explicit terminal state: approved, rejected, abandoned, or expired. Before launch-readiness review, this metric must define numerator, denominator, target threshold, terminal-state inclusion rules, and measurement window. Validates FR-13 through FR-18, FR-24, and FR-28.

**Secondary**

- **SM-4: Unauthorized action prevention** - Authorization tests and launch telemetry show zero successful cross-tenant or unauthorized Agent Calls, proposal actions, or audit inspections. Validates FR-19 through FR-21.
- **SM-5: Audit completeness** - Every posted Agent Response has complete Audit Evidence linking caller, Agent, Source Conversation, Provider/model, proposal path when applicable, and final Conversation Message. Validates FR-11, FR-17, and FR-24.
- **SM-6: Admin/API parity** - Core administration and workflow operations are available through both admin UI and API/client contracts with the same authorization outcomes. Validates FR-22 and FR-23.

**Counter-Metrics (do not optimize blindly)**

- **SM-C1: Automatic post volume without review** - Do not maximize automatic posting if launch tenants choose confirmation for safety-sensitive contexts. Counterbalances SM-2.
- **SM-C2: Approval speed at the cost of audit quality** - Do not optimize approval completion time by dropping version preservation or approval evidence. Counterbalances SM-3.
- **SM-C3: Provider breadth before governance** - Do not optimize the number of Providers/models if Provider governance, secret safety, and per-Agent selection are not robust. Counterbalances SM-1.

#### 12. Open Questions And Deferred Decisions

These items are non-blocking for PRD finalization but must be revisited before the named downstream phase proceeds. Items tied to launch readiness or implementation acceptance are phase blockers for those downstream phases.

| ID | Question | Owner | Revisit Condition |
| --- | --- | --- | --- |
| OQ-1 | What exact UI pattern represents Conversation-originated invocation in V1: mention, command, action button, multiple entry points, participant membership, mention resolution, or a combination? | Product + UX | Before UX flow specification and API route naming. |
| OQ-2 | Which module owns Proposed Agent Reply runtime state and storage boundaries? | Architecture | Before aggregate and persistence design. |
| OQ-3 | What is the default proposal expiry duration, and can Agent Administrators configure it? | Product + Architecture | Before proposal lifecycle stories are created. |
| OQ-4 | Which active notification path, beyond the required in-product pending-proposal surface, tells Approvers that a Proposed Agent Reply is waiting? | Product + UX | Before notification stories are created or before launch readiness if active notifications are required. |
| OQ-5 | What latency target applies to Agent Calls in Automatic Response Mode and Confirmation Response Mode? | Architecture + Release PM | Before performance budgets or launch-readiness gates are accepted. |
| OQ-6 | What cost controls are required for launch: per-tenant quotas, per-Agent quotas, Provider/model budgets, or reporting-only monitoring with explicit risk acceptance? | Product + Architecture | Before Provider administration stories are accepted for implementation and before launch-readiness gates are accepted. |
| OQ-7 | What provider capability metadata is required in the Global Providers Aggregate for V1? | Architecture | Before provider configuration contract design. |
| OQ-8 | What is the exact audit retention period, legal hold, export behavior, and deletion behavior for generated proposal versions? | Product + Governance | Before data governance and audit implementation stories are accepted. |
| OQ-9 | What exact content categories, filters, prompt constraints, and override rules are included in the active Content Safety Policy? | Product + Security | Before generation can be enabled in production or production-like launch validation. |
| OQ-10 | Which approved bounded-context behavior, if any, is allowed when a Conversation is too large for the selected model, and what provider/model budgets define that branch? | Architecture | Before context-building implementation stories are accepted. |
| OQ-11 | What launch threshold should be attached to SM-2 and SM-3 once pilot tenant volume is known? | Product + Release PM | Before launch-readiness review. |

#### 13. Assumptions Index

- §3 Conversation Context - V1 Conversation Context excludes long-term memory, project content, folder content, external tools, and non-conversation retrieval.
- §3 Global Providers Aggregate - Hexalith Agents owns this domain concept unless architecture later assigns provider governance to a shared AI infrastructure module.
- §4.4 Explicit Conversation-Originated Invocation - Conversation-originated invocation may be implemented as a mention, command, or conversation action if the Source Conversation, caller, prompt, and authorization evidence are captured.
- §4.6 FR-18 - Exact proposal expiry duration is configurable or defined by deployment policy, not hardcoded in this PRD.
- §6.2 Out Of Scope For MVP - V1 product behavior exposes only `hexa`, even if implementation uses generalized Agent structures.

#### Companion Document Findings

- The addendum is explicitly contextual market and positioning research. It
  states that its notes inform positioning and risk awareness but do not
  override the product brief.
- The reconciliation companion confirms coverage of the brief's central promise.
  Its three pre-final gap notes are addressed in the final PRD: approver sources
  appear in FR-7, proposal runtime/storage ownership appears as OQ-2, and
  participant membership/mention resolution appears in OQ-1.

### PRD Completeness Assessment

The PRD is structurally strong and unusually testable: it defines 28 stable
functional requirements with explicit consequences, 10 cross-cutting
non-functional requirements, clear V1 scope and non-goals, integration
boundaries, audit constraints, public-surface expectations, success metrics,
and an assumptions index.

It is not independently ready to authorize all implementation work. Eleven open
questions remain. OQ-2 blocks aggregate and persistence design; OQ-3 blocks
proposal lifecycle stories; OQ-6 and OQ-7 block provider contract acceptance;
OQ-8 blocks audit implementation stories; OQ-9 blocks production-like
generation; and OQ-10 blocks context-building implementation stories. OQ-1,
OQ-4, OQ-5, and OQ-11 also gate UX/API naming, notification scope, performance
budgets, and launch thresholds respectively. Downstream architecture, UX, and
epic analysis must demonstrate that these decisions were resolved or that
affected implementation remains explicitly blocked.

## Epic Coverage Validation

### Coverage Matrix

| FR | PRD requirement | Epic and corroborating story coverage | Status |
| --- | --- | --- | --- |
| FR1 | Configure `hexa` | Epic 1 — Stories 1.3 and 1.8 | ✓ Covered |
| FR2 | Link Agent to Party identity | Epic 1 — Story 1.4; posting path in Story 2.5 | ✓ Covered |
| FR3 | Manage Agent lifecycle | Epic 1 — Stories 1.3 and 1.8 | ✓ Covered |
| FR4 | Manage Global Providers Aggregate | Epic 1 — Stories 1.2 and 1.8 | ✓ Covered |
| FR5 | Select Provider and model per Agent | Epic 1 — Stories 1.5 and 1.8 | ✓ Covered |
| FR6 | Configure response mode | Epic 1 — Stories 1.6 and 1.8 | ✓ Covered |
| FR7 | Configure Approver Policy | Epic 1 — Story 1.6; policy enforcement across Stories 3.3–3.6 | ✓ Covered |
| FR8 | Call Agent from Conversation | Epic 2 — Stories 2.1, 2.2, and 2.6 | ✓ Covered |
| FR9 | Build V1 Conversation Context | Epic 2 — Story 2.3 | ✓ Covered |
| FR10 | Handle generation failure | Epic 2 — Stories 2.4 and 2.5 | ✓ Covered |
| FR11 | Post automatic response | Epic 2 — Story 2.5 | ✓ Covered |
| FR12 | Prevent automatic posting when policy fails | Epic 2 — Stories 2.2, 2.4, and 2.5 | ✓ Covered |
| FR13 | Create Proposed Agent Reply | Epic 3 — Stories 3.1 and 3.2 | ✓ Covered |
| FR14 | Preserve all proposal versions | Epic 3 — Stories 3.3, 3.4, and 3.7 | ✓ Covered |
| FR15 | Edit Proposed Reply | Epic 3 — Stories 3.3 and 3.7 | ✓ Covered |
| FR16 | Regenerate Proposed Reply | Epic 3 — Stories 3.4 and 3.7 | ✓ Covered |
| FR17 | Approve Proposed Reply | Epic 3 — Stories 3.5 and 3.7 | ✓ Covered |
| FR18 | Reject, abandon, or expire Proposed Reply | Epic 3 — Stories 3.6 and 3.7 | ✓ Covered |
| FR19 | Enforce tenant isolation | Epic 2 primary mapping — Story 2.2; cross-cutting acceptance constraints and Story 4.5 verification | ✓ Covered |
| FR20 | Enforce role and policy authorization | Epic 2 primary mapping — Story 2.2; workflow enforcement in Epic 3 and Story 4.1 | ✓ Covered |
| FR21 | Fail closed on dependency uncertainty | Epic 2 — Stories 2.2–2.5; approval-posting enforcement in Story 3.5 | ✓ Covered |
| FR22 | Provide admin UI | Epic 4 complete mapping — incremental UI in Stories 1.8, 2.6, 3.2, 3.7, and 4.3 | ✓ Covered |
| FR23 | Provide API and client contracts | Epic 4 — Story 4.1 | ✓ Covered |
| FR24 | Capture Agent Audit Evidence | Epic 4 — Stories 4.2 and 4.5, with evidence emitted throughout earlier workflows | ✓ Covered |
| FR25 | Expose operational status | Epic 4 — Story 4.3 | ✓ Covered |
| FR26 | Configure Content Safety and prompt policy | Epic 1 — Story 1.7 | ✓ Covered |
| FR27 | Enforce safety before Conversation side effects | Epic 2 — Story 2.4; confirmation enforcement in Stories 3.1, 3.4, and 3.5; verification in Story 4.5 | ✓ Covered |
| FR28 | Define launch-readiness controls | Epic 4 — Stories 4.4 and 4.5 | ✓ Covered |

### Missing Requirements

No PRD Functional Requirements are missing from the epic coverage map. No
Functional Requirement identifiers appear in the epic map that are absent from
the PRD.

Coverage is explicit at epic level and corroborated by story acceptance
criteria. Individual stories do not declare their FR identifiers directly, so
the story references above are inferred from their acceptance criteria. Adding
explicit per-story FR tags would make later traceability maintenance less
error-prone, but this is not a coverage gap.

### Coverage Statistics

- Total PRD FRs: 28
- Unique PRD FRs claimed in the epic map: 28
- FRs corroborated by at least one story path: 28
- Missing PRD FRs: 0
- Epic FR identifiers absent from the PRD: 0
- Coverage percentage: 100%

## UX Alignment Assessment

### UX Document Status

UX documentation was found and read completely:

- `DESIGN.md` — visual semantics, components, layout, state colors, typography,
  and operational presentation; status `draft`, last document update
  2026-06-23.
- `EXPERIENCE.md` — behavior, information architecture, canonical states,
  accessibility, responsiveness, and four key journeys; status `draft`, last
  document update 2026-06-23.

The architecture spine is `final` and was updated 2026-08-01. Its
implementation-conventions companion was also reviewed.

### UX ↔ PRD Alignment

The UX spines preserve all four PRD journeys and the central product
distinctions: named Party attribution, explicit invocation, automatic versus
confirmation response modes, proposals remaining outside Conversations until
posted, immutable version history, tenant-safe authorization, provider-secret
safety, operational status, and audit evidence.

UX adds implementation-grade requirements not stated at the same granularity in
the PRD: FrontComposer capability selection, semantic status roles, localization
as whole strings, keyboard and focus behavior, live-region announcements,
reduced motion, list/grid state handling, desktop-first responsive behavior,
fail-closed high-impact actions on constrained viewports, and visual separation
between approved and posted states. These additions are represented in the
epics document as UX-DR1 through UX-DR41.

### UX ↔ Architecture Alignment

Architecture supports the core UX through:

- AD-15 public contract and UI parity with FrontComposer policy-gated navigation.
- AD-14 sensitive-content and provider-secret protections.
- AD-5 proposal lifecycle and immutable version history.
- AD-12 fail-closed authorization and dependency freshness.
- AD-17 FrontComposer UI/contract conformance tests.
- A dedicated UI project, client/API boundary, projections with
  freshness/degraded states, and the pinned Fluent UI Blazor/FrontComposer stack.
- Capability mappings for admin UI, audit/status evidence, proposal workflows,
  and automatic posting.

The implementation-conventions companion preserves the pure-domain boundary
behind UX commands and defines zero/one-dispatch behavior and cross-seam tests.

### Alignment Issues

1. **BLOCKING — Conversation invocation remains unresolved.** PRD OQ-1,
   `EXPERIENCE.md`, `DESIGN.md`, architecture Deferred decisions, and Story
   2.6 all retain mention/command/action/participant membership as alternatives.
   Architecture can normalize any choice, but the user-visible interaction,
   membership behavior, API route naming, focus flow, and acceptance evidence
   cannot be finalized until Product and UX select the V1 pattern.

2. **HIGH — Context and safety policy authoring flows are incomplete.** The PRD
   public surface requires administration of Conversation Context Policy and
   Content Safety Policy. UX specifies blocker/status presentation but no
   complete authoring interaction for context bounds, blocked categories,
   override rules, or policy versioning. Story 1.7 defines the safety command
   path and Story 1.8 shows safety state, but does not close the full policy
   authoring UX. Story 4.4 similarly exposes launch blockers without defining
   where release operators configure all readiness inputs.

3. **HIGH — Proposal expiry policy remains unbound.** UX requires expiry
   labels, queue filters, terminal behavior, and copy, while PRD OQ-3 and
   architecture Deferred decisions leave the default and allowed configuration
   unresolved. Story 3.6 is conditional on an expiry policy existing.

4. **HIGH — Quantitative performance targets remain absent.** UX defines
   responsive layouts and progress states but no response-time budget. PRD OQ-5
   and architecture Deferred decisions leave automatic and confirmation latency
   targets open, so FR28/NFR9 cannot pass a launch gate.

5. **MEDIUM — Active notification scope remains unresolved.** The in-product
   proposal queue satisfies the mandatory fallback, but PRD OQ-4 and UX UJ-3 do
   not select an active notification path. If active notification is excluded,
   the required launch-readiness acceptance must be recorded explicitly.

6. **MEDIUM — UX documents remain draft and predate material architecture
   reconciliation.** They should be ratified or updated against the 2026-08-01
   architecture, particularly AD-10 provider capability semantics and its
   explicit implementation gaps.

7. **MEDIUM — Architecture traceability metadata is stale.** The architecture
   frontmatter says it binds PRD FR-1..FR-25, while the accepted PRD contains
   FR-26..FR-28. Rules and stories address safety/readiness, but the authoritative
   binding declaration does not.

8. **LOW — UX closure inventory omits PRD OQ-11.** `EXPERIENCE.md` carries
   OQ-1 through OQ-10 but not the unresolved SM-2/SM-3 launch threshold decision,
   despite operational status and launch metrics being UX-visible.

### Warnings

- Architecture AD-10 explicitly states that current context, generation, and
  regeneration paths do not yet implement the required provider capability
  high-water mark, current-readiness revalidation, or effective-version
  evidence. UX readiness and audit surfaces must not present those paths as
  proven until that implementation gap is closed and tested.
- UX approval should not be treated as complete merely because the flows are
  documented: the invocation choice, expiry policy, context/safety authoring,
  latency targets, and launch-threshold decisions still require named owners and
  acceptance evidence.

## Epic Quality Review

### Review Scope

Four epics and 26 stories were reviewed for user value, independence, story
sizing, forward dependencies, BDD acceptance criteria, greenfield setup,
entity-creation timing, architecture prerequisites, and traceability.

### Epic Structure Assessment

| Epic | User-value focus | Independence | Assessment |
| --- | --- | --- | --- |
| Epic 1 — Tenant Agent Setup and Governance | Clear administrator outcome | Conditional | User-oriented, but activation/callability depends on later launch-gate work and its structural seed conflicts with current domain-module policy. |
| Epic 2 — Safe Conversation Invocation and Automatic Replies | Clear participant outcome | Fails as written | Depends on public contracts finalized in Epic 4, an absent Conversations membership API/client seam, and an unselected durable owner. |
| Epic 3 — Proposal Review and Approval Workflow | Clear approver outcome | Conditional | Correctly builds on Epics 1–2, but its UI relies on later public-contract work and expiry remains undefined. |
| Epic 4 — Operational Visibility, Audit, Integration, and Launch Readiness | Clear operator/integrator outcomes | Uses prior epics as allowed | It is an omnibus epic containing contract publication, audit, operations, launch policy, and cross-system conformance; several stories are too late or too large. |

No epic is merely titled as a database or infrastructure milestone. At the
story level, however, Story 1.1 is a broad technical foundation rather than a
direct user outcome.

### Story-by-Story Assessment

| Story | Verdict | Quality finding |
| --- | --- | --- |
| 1.1 Buildable module shell | Major revision | Necessary greenfield enabling work, but oversized and technical. It scaffolds many projects and boundaries at once, omits early CI/CD, and includes AppHost/Aspire/ServiceDefaults seams that conflict with current domain-module policy. |
| 1.2 Provider catalog | Ready with clarification | Cohesive user value and good failure/replay criteria; “optional safe capability flags” needs a closed contract or explicit extension rule. |
| 1.3 Configure lifecycle | Ready | Cohesive, independently testable, and includes invalid/unauthorized paths. |
| 1.4 Link Party identity | Major revision | “Link or provision” combines two integration paths; provisioning availability and replacement behavior should be bound explicitly. |
| 1.5 Select Provider/model | Ready | Cohesive selection/readiness slice with historical and tenant-denial paths. |
| 1.6 Response mode and Approver Policy | Ready with clarification | Coherent configuration slice, though it combines two substantial policy concepts and relies on the facilitator-as-owner architecture assumption. |
| 1.7 Content Safety and activation | Blocked | Exact safety categories, filters, and override rules remain OQ-9. It also declares the Agent callable before the complete FR28 launch gate exists. |
| 1.8 Admin setup UI | Epic-sized | Combines navigation, overview, provider grid, Agent configuration, approver policy, status semantics, localization, and accessibility across multiple screens. |
| 2.1 Request `hexa` | Major revision | Good interaction slice, but it requires public API/client contracts that Story 4.1 only publishes later. |
| 2.2 Invocation authorization | Ready | Clear fail-closed user value and strong negative criteria. |
| 2.3 Context bounds | Major revision | “Fresh enough” and the approved bounded-context branch are not measurable in this story. It also lacks the final AD-10 high-water/effective-version rules. |
| 2.4 Generate and safety-check | Blocked | The durable owner and concrete provider-adapter execution path are unselected; “duplicate provider attempts where avoidable” is non-testable. AD-10 retry-descriptor and effective-version requirements are absent. |
| 2.5 Post automatic response | Blocked | Requires an official Conversations `AddParticipant` API/client seam. Local inspection found the command and handler, but no public `AddParticipantAsync` client/API method. |
| 2.6 Invocation UX | Blocked | PRD OQ-1 still leaves mention, command, action, participant membership, or a combination unresolved. The story cannot finalize UI behavior or acceptance evidence while all alternatives remain valid. |
| 3.1 Create proposal | Ready | Cohesive, deterministic, and includes safety and replay paths. |
| 3.2 Discover proposals | Ready with clarification | Strong queue and denial criteria; active-notification exclusion still requires an explicit launch acceptance. |
| 3.3 Edit versions | Ready | Focused behavior with terminal-state, secrecy, and audit paths. |
| 3.4 Regenerate versions | Major revision | Cohesive workflow, but missing AD-10 fresh readiness, high-water mark, prepared-attempt fingerprint, and effective-version acceptance criteria. |
| 3.5 Approve and post | Ready with prerequisite | Strong selected-version and idempotency criteria once the Conversations membership seam and launch gates exist. |
| 3.6 Terminal proposal states | Blocked | OQ-3 leaves expiry default, configurability, and limits undefined; the AC begins with “Given proposal expiry policy exists.” |
| 3.7 Proposal detail/accessibility | Epic-sized | One cohesive screen, but combines full editor behavior, all version history, metadata, every action, keyboard flow, focus return, and live-region coverage. |
| 4.1 Stable API/client contracts | Critical reorder | This work is placed after earlier setup, invocation, and proposal UI/API stories already depend on it, creating a forbidden forward dependency. |
| 4.2 Audit evidence | Blocked | Explicitly blocks itself when retention, legal hold, export, or deletion is unresolved; OQ-8 remains open. The story also spans capture/query/content policy/status behavior across the entire product. |
| 4.3 Operational status | Major revision | Valuable but broad and overlaps status contracts/UI delivered in Stories 1.8, 2.6, 3.2, and 3.7. Boundaries between incremental and final work are unclear. |
| 4.4 Launch readiness gates | Critical reorder and blocked | OQ-5, OQ-6, and OQ-11 remain open. Placing the complete launch gate after Epic 1 activation and Epic 2 generation permits earlier stories to claim callability before required controls exist. |
| 4.5 End-to-end conformance | Epic-sized | Combines domain, runtime, MCP/A2A/tool, retry, tenant, safety, audit, UI, accessibility, localization, and release evidence. “Where applicable” makes protocol obligations non-deterministic. |

### Critical Violations

1. **Current architecture/project policy conflict.** Architecture AD-16,
   Structural Seed, the epics Additional Requirements, and Story 1.1 say the
   domain module owns `*.AppHost`, `*.Aspire`, and
   `*.ServiceDefaults`. The foundational Hexalith/EventStore instructions
   state that domain modules must not ship those projects and must consume the
   platform domain-service SDK. The structural seed cannot be implemented until
   one authority is changed explicitly.

2. **Forbidden forward dependency on Story 4.1.** Stories 1.8, 2.1, 2.6, and
   the Epic 3 UI/API workflows require stable public contracts before Story 4.1
   creates/publishes them.

3. **Forbidden forward dependency on Story 4.4.** Story 1.7 marks `hexa`
   active and callable and Epic 2 performs generation before the full safety,
   context, metric, latency, and cost launch gate is delivered.

4. **Unowned Conversations prerequisite.** Story 2.5 depends on a public
   membership command/API/client boundary. The local Conversations repository
   has `AddParticipantCommand` and a server handler, but no public
   `AddParticipantAsync` client/API method was found. No prerequisite story
   owns exposing it.

5. **Stories knowingly blocked by unresolved product decisions.** Stories 1.7,
   2.6, 3.6, 4.2, and 4.4 depend respectively on OQ-9, OQ-1, OQ-3, OQ-8, and
   OQ-5/OQ-6/OQ-11. These are not independently completable stories.

6. **Durable runtime ownership is not selected.** AD-18 allows three owners,
   while Story 2.4 says “the selected durable owner” without a decision or an
   earlier story that selects it. Generation/retry/session-restore design cannot
   be accepted against multiple incompatible owners.

7. **Epics are stale relative to final AD-10.** The August architecture requires
   a durable capability high-water mark, current readiness revalidation,
   effective capability-version evidence, and prepared-attempt fingerprint
   binding. Stories 2.3, 2.4, and 3.4 do not require these, and AD-10 explicitly
   records the current implementation as non-conformant.

### Major Issues

- Stories 1.8, 3.7, and 4.5 are epic-sized; Story 4.2 is also too broad once its
  governance blocker is resolved.
- Story 1.1 is a necessary greenfield foundation but bundles solution creation,
  all major project seams, packaging rules, and test boundaries without an
  early CI/CD or correct topology plan.
- No early story owns CI setup, package-only consumer validation, or a local
  integration topology under the correct platform host boundary.
- Current `IMPLEMENTATION-CONVENTIONS.md` requires the orchestrator →
  at-most-one trusted command → twin-policy pattern and specific cross-seam
  tests, but relevant stories do not carry that acceptance constraint.
- Several criteria are not measurable: “fresh enough” (2.3), “where avoidable”
  (2.4), “approved combination” and “enough context” (2.6), “where data exists”
  (3.2), and “where applicable” (4.5).
- Story-level FR/UX/architecture traceability is implicit. The epic map is
  complete, but individual stories do not declare the FR, NFR, UX-DR, and AD
  identifiers they own.

### Checks That Passed

- Epic titles and goals are framed as administrator, participant, approver, or
  operator outcomes rather than database/infrastructure phases.
- The intended epic order is conceptually incremental: setup → automatic
  invocation → confirmation workflow → operations/readiness.
- Most acceptance criteria use proper Given/When/Then structure and include
  negative, terminal, authorization, secrecy, and idempotency paths.
- Entity timing is sound for event sourcing: Story 1.1 explicitly avoids
  pre-creating future entities, tables, and events.
- Architecture specifies no external starter template; Story 1.1 correctly uses
  the architecture Structural Seed instead.
- Brownfield integration points with EventStore, Conversations, Parties,
  Tenants, and FrontComposer are identified.

### Required Remediation

1. Resolve the domain-module topology authority conflict and replace the
   Structural Seed/Story 1.1 project list with the approved platform model.
2. Move slice-specific public contracts into the stories that first need them,
   or move a minimal contract foundation ahead of all UI/API consumers; redefine
   Story 4.1 as additive versioning/package/consumer conformance.
3. Move the minimum complete production-like launch gate before activation and
   generation. Keep Story 4.4 for operator configuration/reporting only.
4. Add and own the Conversations public membership seam prerequisite before
   Story 2.5.
5. Close OQ-1, OQ-3, OQ-5, OQ-6, OQ-8, OQ-9, and OQ-11 with named decisions;
   select the V1 durable owner.
6. Reconcile stories 2.3, 2.4, and 3.4 with AD-10 and add the explicit
   runtime-reconciliation story/tests that AD-10 says are missing.
7. Split Stories 1.8, 3.7, 4.2, and 4.5 into independently testable UI,
   governance, runtime, and conformance slices.
8. Add early CI/package/topology validation under the approved hosting boundary.
9. Add explicit FR/NFR/UX-DR/AD tags to every story and replace conditional or
   subjective AC language with measurable contracts.

## Summary and Recommendations

### Overall Readiness Status

**NOT READY**

Functional traceability is complete—28 of 28 PRD Functional Requirements have
epic and story paths—but coverage is not the same as implementability. The
current package contains foundational authority conflicts, reverse
dependencies, unowned integration prerequisites, unresolved product/governance
decisions embedded inside stories, stale story acceptance criteria relative to
the final architecture, and several epic-sized stories.

Implementation should not continue from these artifacts as written. Work already
underway should be limited to remediation and evidence gathering until the
critical gates below are closed.

### Critical Issues Requiring Immediate Action

1. **Resolve the hosting/topology authority conflict.** Decide whether Agents is
   a domain-centric EventStore module using the platform host/SDK or an approved
   exception that owns AppHost/Aspire/ServiceDefaults. Update the architecture,
   Structural Seed, Story 1.1, and project instructions to one consistent rule.

2. **Remove reverse dependencies.** Deliver slice-specific public contracts
   before their UI/API consumers, and enforce the minimum complete FR28 launch
   gate before any story can mark `hexa` callable or invoke a provider.

3. **Own the Conversations membership prerequisite.** Add or expose a public,
   supported, idempotent `AddParticipant` API/client seam and validate the
   `ParticipantType.AiAgent` posting path before Story 2.5.

4. **Close blocking decisions.** Resolve OQ-1, OQ-3, OQ-5, OQ-6, OQ-8, OQ-9,
   and OQ-11, and select the single V1 durable runtime owner. Record owners,
   decisions, consequences, and acceptance evidence.

5. **Reconcile stories with final architecture.** Add AD-10 capability
   high-water/current-readiness/effective-version/prepared-attempt requirements
   and the command-step twin-policy convention to the affected stories and
   tests. The architecture explicitly says the current runtime paths are
   non-conformant.

6. **Ratify the UX package.** Select the Conversation invocation pattern, define
   context/safety/readiness policy authoring flows, bind expiry and notification
   behavior, and update the draft UX documents against the August architecture.

### Recommended Next Steps

1. Run a focused architecture correction that resolves the platform/domain
   boundary and selects the durable owner.
2. Run product/governance decision sessions for the seven blocking open
   questions; publish explicit decisions rather than leaving conditional ACs.
3. Create the Conversations membership-seam prerequisite in the owning module
   and attach contract/authorization/idempotency evidence.
4. Rework the epic order: contract slices and minimum launch gates first,
   workflow slices next, consolidated operations and package conformance last.
5. Split Stories 1.8, 3.7, 4.2, and 4.5; replace subjective/conditional criteria
   with measurable behavior and explicit FR/NFR/UX-DR/AD tags.
6. Add early CI, package-consumer, and topology validation under the corrected
   host model.
7. Re-run implementation-readiness validation after all critical items are
   updated; do not treat the current 100% FR mapping as a go decision.

### Assessment Record

- Assessment date: 2026-08-01
- Assessor: Codex using the BMad Implementation Readiness workflow
- Evidence basis: accepted PRD, architecture spine and conventions, epics and
  stories, UX design and experience spines, persistent Hexalith project context,
  and local verification of the Conversations public membership surface
- Primary findings: 21 documented issues—7 critical structural/dependency
  violations, 8 UX/alignment issues, and 6 major planning-quality issues—across
  requirements/governance, architecture policy, dependency ordering, UX, and
  story quality/traceability
- Open decisions tracked: 11 PRD open questions, with seven directly blocking
  implementation or launch acceptance

### Final Note

The planning package has strong requirement coverage, detailed BDD criteria,
clear user journeys, and sound security/audit intent. Those strengths make the
defects repairable, but they do not neutralize them. Address every critical item
before resuming feature implementation; then repeat this assessment against the
reconciled artifacts.
