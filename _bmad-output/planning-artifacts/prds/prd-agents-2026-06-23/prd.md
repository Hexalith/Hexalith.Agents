---
title: Hexalith Agents
status: final
created: 2026-06-23
updated: 2026-09-08
---

# PRD: Hexalith Agents

## 0. Document Purpose

This PRD defines the launch-level V1 requirements for Hexalith Agents, the governed AI participant capability for Hexalith Conversations. It is intended for product, UX, architecture, implementation, QA, and release-readiness workflows. The document uses glossary-anchored terms, grouped features, globally stable functional and non-functional requirement IDs, explicit non-goals, launch success metrics, normative evidence authority, and a binding V1 Decision Register. It builds on the product brief at `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/briefs/brief-agents-2026-06-23/brief.md` and preserves external landscape notes in `addendum.md`.

**Downstream authority.** The single executable backlog for this PRD is the epic and story set named by the current sprint status; where backlog documents disagree, that named set governs and the others are superseded history. Any approved proposal listing `prd.md` under `amends_if_approved` is not landed until a PRD Update run applies it, and the frontmatter `updated:` field records when that last occurred.

## 1. Vision

Hexalith Agents brings named, governed AI participants into tenant-scoped Hexalith conversations. A Party in a Conversation can explicitly call `hexa`, the first V1 Agent, and receive an answer that is attributable to a durable Party identity rather than anonymous system output. The answer is produced from Conversation Context and governed by Agent configuration, authorization, approval policy, and audit.

The core V1 bet is that AI assistance must become part of the conversation model without weakening the model's existing identity and governance guarantees. Hexalith already treats Conversations as durable multi-party records and Parties as stable participant identities. Hexalith Agents extends that system by making the AI assistant a governed participant with lifecycle, instructions, Provider/model configuration, invocation rules, response policy, and traceable evidence for how each response reached the Conversation.

V1 deliberately does less than a broad autonomous agent platform. `hexa` does not use long-term memory, external tools, project content, folder content, or ambient triggers. The launch proves one thing well: a participant can call a named AI assistant inside a Conversation, and the system can either post the answer automatically or route it through a complete approval workflow before it becomes conversation content.

## 2. Target Users

### 2.1 Jobs To Be Done

- As an Agent Administrator, configure `hexa` so it has a durable identity, safe instructions, an approved Provider/model, a response mode, and an Approver Policy before anyone can call it.
- As a Conversation Participant, call `hexa` from a Conversation when contextual help is needed without leaving the Conversation or losing attribution.
- As an Approver, review generated AI output, edit or regenerate it when needed, and approve only the response that should become part of the Conversation.
- As a Tenant or Compliance Operator, prove who called the Agent, what it generated, what changed during approval, who approved it, and what was finally posted.
- As an Integration Developer, use stable API/client contracts to configure Agents, call Agents, manage proposals, and inspect audit/status without depending on internal implementation details.

### 2.2 Non-Users In V1

- Users who want autonomous Agents that react to every project, folder, or Conversation change without explicit invocation.
- Users who want Agents to take business actions outside adding Agent responses to Conversations.
- Users who need external-channel bots for Slack, Teams, email, or other non-Hexalith channels.
- Users who need tool-using Agents, long-term memory, retrieval over project/folder content, or agent-to-agent orchestration in V1.

### 2.3 Key User Journeys

- **UJ-1. Nora configures `hexa` for a tenant launch.**
  - **Persona + context:** Nora is an Agent Administrator preparing a tenant to use governed AI in Conversations.
  - **Entry state:** Nora is authenticated in the admin surface with permission to manage Agent configuration and Provider settings.
  - **Path:** Nora opens the Agents admin area, creates or enables `hexa`, confirms the linked Party identity, selects a Provider/model from the global providers catalog, enters instructions, chooses response mode, and defines who can approve proposed replies.
  - **Climax:** The system marks `hexa` active and callable only after its identity, Provider/model, instructions, and Response Policy are valid and, in Confirmation Response Mode, its Approver Policy is valid.
  - **Resolution:** Conversation Participants can now call `hexa` under the configured policy, and Nora can inspect configuration and operational status.
  - **Edge case:** If no Provider/model is available or enabled for the tenant, `hexa` cannot be activated and the admin receives a configuration error.

- **UJ-2. Milan calls `hexa` from a Conversation and receives an automatic reply.**
  - **Persona + context:** Milan is a Party participating in a Conversation and needs help interpreting the prior discussion.
  - **Entry state:** Milan is authenticated, has access to the Conversation, and the Agent is configured for Automatic Response Mode.
  - **Path:** Milan invokes `hexa` from inside the Conversation, asks a question, and waits while the system builds the request from the complete Conversation Context.
  - **Climax:** `hexa` posts an attributed response into the same Conversation as the Agent's Party identity.
  - **Resolution:** Participants can continue the Conversation with the Agent response visible as durable Conversation content.
  - **Edge case:** If Milan lacks permission to call the Agent, the call is rejected and no Provider request is made.

- **UJ-3. Anika approves a proposed `hexa` response before it enters the Conversation.**
  - **Persona + context:** Anika is an Approver for a sensitive Conversation where Agent responses require confirmation.
  - **Entry state:** A Conversation Participant has invoked `hexa`, and the Agent is configured for Confirmation Response Mode with Anika in the Approver Policy.
  - **Path:** The system creates a Proposed Agent Reply outside the Conversation. Anika opens the proposal, reviews the generated content and context metadata, requests one regeneration, compares the versions, and approves the generated version she did not author. Had she instead edited the draft herself, a second authorized Approver would have to approve that edited version, because no Party may approve a version it last edited.
  - **Climax:** The approved draft is posted into the source Conversation as `hexa`, with approval evidence linked to the posted message.
  - **Resolution:** The Conversation contains only the approved response, while the proposal record preserves generated, edited, regenerated, approved, rejected, abandoned, or expired states for audit.
  - **Edge case:** If the proposal expires before approval, it cannot be posted and a new Agent call is required. If Anika loses read access to the Conversation while the proposal is pending, she can still see that it exists, but not its content, and she can no longer approve it.

- **UJ-4. Omar integrates Agent operations through the API.**
  - **Persona + context:** Omar is an Integration Developer building tenant automation and operations checks around Hexalith Agents.
  - **Entry state:** Omar has API credentials with the appropriate tenant and Agent permissions.
  - **Path:** Omar uses public API/client contracts to list Provider options, configure an Agent, inspect proposal status, approve or reject proposals where authorized, and query audit evidence.
  - **Climax:** The integration can perform the same governed operations as the admin UI without using internal EventStore, Party, or Conversation implementation details.
  - **Resolution:** Tenant automation can onboard and monitor Agents while preserving the same authorization and audit guarantees as the first-party UI.
  - **Edge case:** If Omar's integration calls an operation his credentials do not authorize, the call is denied with a typed result before any side effect, and the denial is auditable without revealing the content he could not access.

## 3. Glossary

- **Agent** - A configured AI participant managed by Hexalith Agents. In V1, the first Agent is `hexa`.
- **Agent Administrator** - A Party or operator role authorized to configure Agents, Provider/model selection, Response Policy, lifecycle, and Approver Policy.
- **Agent Call** - An explicit request from a Conversation Participant to an Agent from within a Conversation.
- **Agent Instructions** - Administrator-defined instructions that guide the Agent's behavior for generated replies.
- **Agent Response** - Content generated by an Agent for a source Conversation. It becomes a Conversation Message only after automatic posting or approval.
- **Approved Bounded Context Behavior** - An explicitly declared, audited context-reduction behavior that a Conversation Context Policy may permit when the complete Source Conversation exceeds the Safe Context Budget. Its reference and bounds are recorded as Audit Evidence on every call that uses it, so bounded context is never a silent truncation. V1 declares no Approved Bounded Context Behavior.
- **Approver** - A Party authorized by the Agent's Approver Policy to edit, regenerate, approve, reject, abandon, or otherwise resolve Proposed Agent Replies.
- **Approver Policy** - Agent configuration that defines all Parties or roles allowed to approve Proposed Agent Replies, including configured sources such as the Conversation Facilitator, the caller, predefined Parties, or tenant roles. V1 approval authority is fully defined by Agent configuration, subject to the Conversation-access and segregation-of-duties constraints in FR-7.
- **Audit Evidence** - Durable records connecting caller, Agent, Provider/model, Source Conversation, generated versions, edits, regenerations, approval decisions, posting outcome, timestamps, and authorization decisions.
- **Automatic Response Mode** - Agent Response mode where the generated response is posted directly to the Conversation as the Agent's Party identity after generation succeeds.
- **Confirmation Response Mode** - Agent Response mode where generated output becomes a Proposed Agent Reply and requires approval before posting.
- **Content Safety Policy** - The configured launch policy that defines prompt constraints, blocked or restricted output categories, safety failure handling, and audit treatment for Agent generation.
- **Conversation** - A tenant-scoped Hexalith.Conversations record containing durable multi-party discussion content.
- **Conversation Context** - The authorized Source Conversation content and related participant/context metadata supplied to the Agent for a V1 Agent Call. It is the complete Source Conversation unless the active Conversation Context Policy declares an Approved Bounded Context Behavior; V1 declares none, so it is always the complete Source Conversation. V1 Conversation Context excludes long-term memory, project content, folder content, external tools, and non-conversation retrieval.
- **Conversation Context Policy** - The versioned rule set that requires the complete Source Conversation to be sent to the Provider when it fits the selected Provider/model's Safe Context Budget, and otherwise either fails closed or applies an Approved Bounded Context Behavior the policy explicitly declares. V1 declares none, so the oversized case always fails closed. Silent truncation, summarization, or windowing is prohibited under every policy.
- **Conversation Facilitator** - The V1 Conversation authority used as an Approver Policy source, resolved normatively from `ParticipantRole.Facilitator`. The Conversations contract exposes no owner field, so V1 resolves no distinct Conversation owner; a true-owner resolver is post-V1 (OQ-15).
- **Conversation Message** - Durable content posted to a Conversation through Hexalith.Conversations.
- **Conversation Participant** - A Party with access to a Conversation who may call `hexa` when authorized.
- **Eligible Conversation** - For success-metric measurement, any tenant Conversation in the enabled launch cohort in which at least one Participant was authorized to call `hexa` at any point during the measurement window, whether or not a call was attempted. Eligibility measures reach, so SM-2 remains an adoption metric rather than an acceptance rate. A Conversation in which a call was attempted but blocked — by the Conversation Context Policy, Content Safety Policy, cost caps, rate limits, or Provider failure — remains eligible and stays in the denominator, and SM-C4 tracks their share directly.
- **Evidence Level** - A normative classification of proof strength from Level 1 contract/structure evidence through Level 5 cross-system production-like evidence, as defined in §11.
- **Global Providers Aggregate** - The Hexalith Agents-owned catalog of configured AI providers, models, capability metadata, versioned pricing metadata, and `CapabilityVersion` values available for per-Agent Provider/model selection.
- **Launch Readiness Gate** - A product, governance, architecture, or release condition that must be resolved before production or production-like launch validation can pass.
- **Party** - A stable Hexalith.Parties identity representing a human, organization, or AI participant.
- **Provider** - An AI service provider available through the Global Providers Aggregate.
- **Proposed Agent Reply** - Generated Agent output held outside the Conversation until an Approver approves it.
- **Provider/Model Selection** - Per-Agent configuration choosing which Provider and model the Agent uses.
- **Regeneration** - A new generated version of a Proposed Agent Reply created before approval.
- **Release Qualification Gate (`RQ-1`)** - The operational gate that evaluates live Evidence Levels 4 and 5, real rolling-window and cohort metrics, and the final READY or NOT READY launch decision after implementation.
- **Response Policy** - Agent configuration that determines Automatic Response Mode or Confirmation Response Mode and the related approval behavior.
- **Safe Context Budget** - The token allowance available for Conversation Context on a given call: the selected model's input limit recorded in the Global Providers Aggregate for the applicable `CapabilityVersion`, minus the reserved output allowance, the Agent Instructions, and a fixed configured safety margin. It is counted with the Provider's tokenizer or a named documented approximation, and the computed budget, the measured context size, and the `CapabilityVersion` used are recorded in Audit Evidence for every call.
- **Source Conversation** - The Conversation from which an Agent Call originated and to which an approved or automatic Agent Response is posted.
- **Versioned Proposal Content** - Every generated, edited, or regenerated content version associated with a Proposed Agent Reply.

## 4. Features

### 4.1 Agent Identity, Configuration, And Lifecycle

**Description:** Agent Administrators can create, configure, activate, disable, and inspect `hexa` as the first governed Agent. An active Agent must have a durable Party identity, valid instructions, valid Provider/model selection, Response Policy, and Approver Policy where confirmation is enabled. This realizes UJ-1.

**Functional Requirements:**

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
- The Agent joins a Conversation as a participant at first accepted Agent Call in that Conversation, before generation, and never as a side effect of an unapproved proposal.
- Membership is added under the platform Agents service principal, not the caller's identity, and is idempotent: a repeated join for the same Agent and Conversation is a no-op that returns the existing membership.
- Membership failure fails the Agent Call closed and creates neither Provider work, a Proposed Agent Reply, nor a Conversation Message.
- Agent membership is visible to Conversation Participants on the same terms as any other participant.
- An authorized Party may remove `hexa` from a Conversation; removal blocks further Agent Calls there and moves every non-terminal proposal for that Conversation to `Abandoned` with its versions preserved.

#### FR-3: Manage Agent Lifecycle

Agent Administrators can activate, disable, and inspect `hexa` lifecycle state.

**Consequences (testable):**

- Disabled Agents cannot be called from Conversations.
- Disabling an Agent does not delete existing Audit Evidence, Proposed Agent Replies, or Conversation Messages.
- Lifecycle changes are auditable and visible through admin UI and API/client contracts.

### 4.2 Provider Governance And Per-Agent Model Selection

**Description:** Hexalith Agents uses a Global Providers Aggregate to govern available AI providers and models. Each Agent selects its Provider and model from that governed catalog rather than using an implicit hardcoded Provider. This realizes UJ-1 and UJ-4.

**Functional Requirements:**

#### FR-4: Manage Global Providers Aggregate

Authorized administrators can configure the Global Providers Aggregate with Provider records, model options, enabled/disabled state, versioned pricing metadata, capability limits, and Provider capability metadata needed for Agent selection.

**Consequences (testable):**

- Disabled Providers or models cannot be selected for new Agent configuration.
- Existing Agents using a disabled Provider/model cannot be activated or called until reconfigured. A documented migration state may allow temporary read-only inspection.
- Provider configuration changes are auditable without exposing secrets in logs, API responses, UI, or Audit Evidence.
- Pricing metadata is required on model create and update: a well-formed currency, non-negative unit prices, and a pricing version that starts at 1 on create and strictly increases. A malformed currency, a negative unit price, or a decreased or reused pricing version is a typed rejection, not a silent acceptance.
- Capability limits are required and positive: context limit, output allowance, and request timeout.
- Each model records its secret reference and whether the Provider is configured, without exposing the secret itself.
- `CapabilityVersion` increases monotonically and is never reused. It acts as the optimistic-concurrency token for catalog writes: a caller-supplied expected version below the stored value is rejected as regressed, and one above it is rejected as stale.
- Pricing metadata is the input to the cost estimation that FR-32 and OQ-6 reserve against; absent or invalid pricing for the selected model blocks Provider invocation. It is not used for customer-facing billing or monetization in V1 (§6.2).

#### FR-5: Select Provider And Model Per Agent

Agent Administrators can select a Provider and model for `hexa` from the Global Providers Aggregate.

**Consequences (testable):**

- The system validates the complete selection-eligibility set before Agent activation: the Provider and model are enabled; the Provider is configured; the model supports text generation; its capability limits are valid; its pricing metadata is valid; and its `CapabilityVersion` is not regressed. Failing any element blocks activation with a typed reason.
- The system stores enough Provider/model identity in Audit Evidence to explain which Provider and model produced each generated version.
- Changing Provider/model selection affects future Agent Calls only and does not rewrite historical proposal or response evidence.

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

Agent Administrators can define all Approvers through the Agent's Approver Policy, including policy sources such as the Conversation Facilitator, the caller, predefined Parties, or tenant roles. V1 resolves the Conversation Facilitator normatively from `ParticipantRole.Facilitator`.

**Consequences (testable):**

- The system authorizes proposal edit, regeneration, approval, rejection, abandonment, and expiry-resolution actions using the Approver Policy.
- The system rejects approval actions by Parties not authorized by the current policy for the proposal.
- The system exposes which configured policy source authorized the Approver according to a defined disclosure category: user-visible, operator-only, redacted, or omitted.
- The proposal records the policy basis used for each approval-related decision.
- Every configured Approver Policy source declares its disclosure category at configuration time. Absent an explicit choice, the category is operator-only, so a source is never disclosed more widely by omission; a source whose disclosure would reveal membership of a restricted tenant role cannot be configured as user-visible.
- API/client contracts and admin UI use the same disclosure category for the same approval-policy basis.
- No admin UI text, API/client contract field, or Audit Evidence record may state or imply that a distinct Conversation owner was resolved; the disclosed authority is the Conversation Facilitator.
- Every Approver must hold current read access to the Source Conversation at the moment of each proposal action. The system re-checks that access at discovery, edit, regeneration, approval, and audit-content inspection, and fails closed when it is absent, stale, or unavailable.
- Approver Policy validation rejects a configured source that cannot satisfy the Conversation-access requirement at call time, including a bare tenant role whose holders are not guaranteed Conversation access.
- An Approver who has lost Conversation read access may see that a proposal exists and its state, but no proposal content, context metadata, or generated or edited version.
- The caller may not be the sole Approver of their own Agent Call, and the approving Party must differ from the Party that last edited the version being approved. A policy configuration that cannot satisfy this segregation of duties is rejected at configuration time.

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
- The Safe Context Budget is derived per call as the selected model's recorded input limit for the applicable `CapabilityVersion`, minus the reserved output allowance, the Agent Instructions, and a fixed configured safety margin, counted with the Provider's tokenizer or a named documented approximation.
- When the full Source Conversation fits the Safe Context Budget, V1 generation uses the full Source Conversation.
- When the Source Conversation exceeds the Safe Context Budget, the call fails closed before Provider invocation unless the active Conversation Context Policy declares an Approved Bounded Context Behavior that fits, in which case that behavior applies and its reference and bounds are recorded as Audit Evidence. V1 declares no Approved Bounded Context Behavior, so the oversized case always fails closed.
- Silent truncation, summarization, or windowing is prohibited under every policy. A bounded-context mode presented as an input without a matching declared Approved Bounded Context Behavior is a rejected input, not a fallback.
- The computed Safe Context Budget, the measured context size, the `CapabilityVersion` used, and the resulting context mode are recorded in Audit Evidence for every call, including blocked calls.
- Agent Calls record the resolved context mode — full Source Conversation, an Approved Bounded Context Behavior, or blocked — together with the Conversation Context Policy version or equivalent identifier, and enough context metadata for audit without leaking unrelated tenant data.
- If the complete Conversation Context cannot be loaded or sent safely, the Agent Call fails closed and creates neither Provider work, a Proposed Agent Reply, nor a Conversation Message.

#### FR-10: Handle Generation Failure

The system handles Provider failures, timeout, disabled Provider/model state, invalid context, Content Safety Policy failures, and policy failures without posting incomplete or unsafe Agent Responses.

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
- Every automatically posted Agent Response carries a participant-visible marker identifying it as AI-generated from Conversation Context and not human-verified.

#### FR-12: Prevent Automatic Posting When Policy Fails

The system prevents automatic posting when authorization, Agent lifecycle, Provider/model, Party identity, Source Conversation access, Conversation Context Policy, Content Safety Policy, or generation status is invalid.

**Consequences (testable):**

- No Conversation Message is created when a required policy check fails.
- No Conversation Message is created when generated content fails the active Content Safety Policy.
- The failure reason is visible through authorized status surfaces without leaking secrets or unrelated tenant data.
- Audit Evidence distinguishes authorization, context policy, content safety, cost-cap, Provider/runtime, and posting failures.
- A Provider attempt whose outcome cannot be determined resolves to a typed `Unknown` terminal outcome rather than an implicit success or failure, and is never silently retried.

### 4.6 Proposed Agent Reply Workflow

**Description:** In Confirmation Response Mode, generated Agent output is managed outside the Conversation as a Proposed Agent Reply. Approvers can edit, regenerate, approve, reject, or abandon a Proposed Agent Reply, and proposals can expire. Only an approved version is posted to the Source Conversation. This realizes UJ-3.

**Functional Requirements:**

#### FR-13: Create Proposed Agent Reply

When `hexa` is configured for Confirmation Response Mode, successful generation creates a Proposed Agent Reply linked to the Source Conversation and Agent Call.

**Consequences (testable):**

- A Proposed Agent Reply is not a Conversation Message.
- A Proposed Agent Reply records caller, Agent, Source Conversation, generated version, Provider/model, response mode, and current proposal state.
- Authorized Approvers can discover pending proposals requiring their action.
- V1 uses in-product pending-proposal visibility only for authorized Approvers, including a pending count, a queue, and a Conversation status entry.
- Email, push, and external-channel proposal notifications are not included in V1.
- Proposal discovery returns only proposals whose Source Conversation the requesting Approver can currently read; proposals for other Conversations are not listed, counted, or otherwise disclosed.

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
- Every edited version records the editing Party and edit timestamp as durable provenance, and an edited version is never presented as generated output.

#### FR-16: Regenerate Proposed Reply

Authorized Approvers can request regeneration of a Proposed Agent Reply before approval.

**Consequences (testable):**

- Regeneration uses the same Source Conversation and Agent configuration unless the system records an explicit configuration version change.
- Regeneration preserves prior versions and creates a new generated version.
- Regeneration is blocked after a proposal reaches a terminal state.
- A configured per-proposal regeneration ceiling bounds how many regenerations a proposal may accumulate; exceeding it is a typed rejection.
- A regeneration is a chargeable Agent Call for cost-cap, rate-limit, and audit purposes.

#### FR-17: Approve Proposed Reply

Authorized Approvers can approve a selected proposal version, causing it to be posted to the Source Conversation as `hexa`.

**Consequences (testable):**

- Approval posts exactly the approved version and no other proposal version.
- The Conversation Message is attributed to the Agent's Party identity.
- Audit Evidence links the approved version, Approver, approval timestamp, and posted Conversation Message.
- The approved version passes the then-current Content Safety Policy at approval time before posting, whether it was generated or human-edited (FR-27).
- The posted Conversation Message's trace records whether the posted version was human-edited and by which Party, and that fact is exposed wherever the message's provenance is disclosed.

#### FR-18: Resolve Proposed Reply Through Its Lifecycle States

A Proposed Agent Reply occupies exactly one of these states: `Pending` (awaiting an Approver decision), `Approved` (a version is approved but not yet posted), `Posted` (terminal — the approved version is confirmed in the Conversation), `PostFailed` (approved, posting failed, bounded retry available), `Rejected` (an Approver declined the proposal), `Abandoned` (an Approver or system policy withdrew it), and `Expired` (its `ExpiresAt` passed while non-terminal). `Posted`, `Rejected`, `Abandoned`, and `Expired` are terminal. A proposal is moved between them by authorized Approvers or by system policy.

**Consequences (testable):**

- Terminal proposals cannot be approved or posted.
- Terminal proposals preserve all generated and edited versions for audit.
- The default expiry duration is 24 hours. An Agent Administrator may configure a duration from 1 hour through 30 days for future proposals only.
- A proposal whose stored `ExpiresAt` has passed is treated as `Expired` on every read and every command, regardless of timer delivery; an expiry policy change never changes an existing proposal's `ExpiresAt`.
- `Approved`, `PostFailed`, and `Posted` are distinct states. Approval does not imply posting; a failed post moves the proposal to `PostFailed` with a typed reason and bounded audited retry, and only a confirmed post reaches `Posted`.
- Before posting, the system re-validates that the Source Conversation still exists and is accessible to the posting principal, and that the approved version still passes the then-current Content Safety Policy; a failure moves the proposal to `PostFailed` rather than posting stale content.
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

The system enforces authorization for Agent administration, Provider administration, Agent calling, proposal discovery, editing, regeneration, approval, rejection, abandonment, posting, and audit inspection.

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
- A story completed while a dependency it consumes was `Uncommitted` is recorded as non-conformant against this gate, with the dependency, the story, and the date captured in the dependency register. Narrowing a story's scope to avoid executing the seam does not clear the non-conformance record.
- Posting an approved Agent Response checks Conversation access for the named posting principal — the Agent's Party identity acting through the platform Agents service principal — not for the original caller or the Approver.

### 4.8 Admin UI And API/Client Contracts

**Description:** V1 includes both an admin web UI and public API/client contracts. These surfaces must expose the same governed capability without leaking internal implementation mechanics. This realizes UJ-1 and UJ-4.

**Functional Requirements:**

#### FR-22: Provide Admin UI

The admin UI allows authorized administrators to manage Global Providers Aggregate entries, configure `hexa`, inspect lifecycle state, configure Response Policy and Approver Policy, and view Agent operation and proposal status.

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
- A public member or enum value whose behavior V1 prohibits may be marked obsolete and rejected server-side with a typed error while remaining declared and deserializable. Deprecate-and-reject is the required mechanism for retiring a prohibited behavior within V1; it is not a removal and does not require a major version.
- A breaking public change requires a new major package/API version and package-consumer compatibility tests.

#### FR-29: Distinguish Accepted Configuration From Confirmed Configuration

Administrative writes to Agent and Provider configuration expose an authoritative acceptance stage distinct from read-model confirmation, so an administrator can tell a durable decision from a not-yet-visible projection.

**Consequences (testable):**

- A write reports a `Submitted` stage before the domain decision is durable, an `AuthoritativePending` stage once the decision is durable but not yet projected, and a `ProjectionConfirmed` stage once the read model reflects it.
- `AuthoritativePending` is an authoritative accepted outcome: the decision is durable and is never re-submitted or silently retried by the caller.
- Reads expose the projection version and freshness they were served at, so a caller can determine whether a prior accepted write is reflected.
- Admin UI and API/client contracts report the same stages and freshness for the same write, and never present an unconfirmed projection as confirmed.

### 4.9 Audit Evidence And Operational Visibility

**Description:** Hexalith Agents must provide durable proof of Agent behavior and enough operational status to run the launch safely. This realizes UJ-3 and UJ-4.

**Functional Requirements:**

#### FR-24: Capture Agent Audit Evidence

The system captures Audit Evidence for Agent configuration, Provider/model configuration, Agent Calls, generation attempts, proposal versions, edits, regenerations, approvals, rejections, abandonments, expirations, automatic posts, and final Conversation Messages.

**Consequences (testable):**

- Every posted Agent Response can be traced back to caller, Agent, Source Conversation, Provider/model, generated content, and approval path where applicable.
- Every Proposed Agent Reply preserves all Versioned Proposal Content.
- Audit Evidence records the Content Safety Policy decision, Conversation Context Policy behavior, and policy/version identifiers where available.
- Audit Evidence is queryable by authorized users without exposing unrelated tenant data or Provider secrets. Inspecting Audit Evidence that contains Conversation-derived content additionally requires current read access to that Source Conversation, and fails closed otherwise.
- Audit Evidence records the computed Safe Context Budget, measured context size, `CapabilityVersion`, and resolved context mode for every call.
- Audit Evidence records the editing Party for every edited version and the approving Party for every approval, so a human-edited posted message is always distinguishable from generated output.
- Every audited administrative override, including a cost-cap override, records actor, scope, justification, and expiry.

#### FR-25: Expose Operational Status

The system exposes status for Agent readiness, Provider/model readiness, recent Agent Call outcomes, proposal queues, generation failures, approval completion, and posting outcomes.

**Consequences (testable):**

- Authorized administrators can identify whether `hexa` is callable for a tenant.
- Authorized administrators can distinguish configuration errors, authorization failures, context policy failures, content safety failures, Provider failures, generation failures, pending approvals, and posting failures.
- Status surfaces support launch monitoring of adoption and approval workflow metrics.
- Status exposes the per-tenant count of Agent Calls blocked by the Conversation Context Policy, by Content Safety Policy, and by cost caps, so structural unavailability is visible rather than silent.
- Status exposes each tenant's cost-cap consumption, including the 80% warning and 100% fail-closed condition, and distinguishes reserved from settled spend.

#### FR-30: Provide Governance Operations

The governance rules in §9 are callable operations on the public surface, not documentation only.

**Consequences (testable):**

- Authorized operators can administer budget policy, publish and version Content Safety Policy, apply and release legal hold, request authorized export, and request approved deletion.
- Authorized operators can inspect launch-readiness state and its blockers.
- Every governance operation enforces tenant scope and Party authorization, is audited with actor and justification, and fails closed when authorization or required state is missing.
- Governance operations are covered by the FR-23 compatibility rules on the same terms as every other public contract.

### 4.10 Content Safety And Launch Readiness

**Description:** V1 generation must be gated by explicit safety, context, cost, performance, and metric decisions before production or production-like launch validation. This realizes UJ-1, UJ-2, UJ-3, and UJ-4.

**Functional Requirements:**

#### FR-26: Configure Content Safety And Prompt Policy

Authorized administrators or release operators can define the active Content Safety Policy for `hexa`.

**Consequences (testable):**

- `hexa` cannot be enabled for production or production-like launch validation without an active Content Safety Policy.
- The Content Safety Policy defines prompt constraints, blocked or restricted output categories, safety failure handling, and audit treatment.
- Content Safety Policy changes are auditable and affect future Agent Calls only, except that the approval-time and pre-post re-checks required by FR-27 — the third and fourth of the four application points in OQ-9 — always evaluate the then-current active policy.
- Automatic Response Mode and Confirmation Response Mode use the same active Content Safety Policy unless a stricter mode-specific policy is configured.
- The active policy always blocks child sexual abuse/exploitation; credible threats or instructions for imminent serious harm; encouragement or instruction for suicide/self-harm; credential theft, malware deployment, or unauthorized compromise; secrets/tokens/private credentials; cross-tenant or unauthorized personal/Conversation data; and attempts to bypass tenant, authorization, audit, retention, or safety controls.
- Restricted hate, harassment, sexual, violent, illegal-activity, or sensitive-personal content requires an explicitly permitted tenant use case and Confirmation Response Mode.
- A policy version cannot weaken a retry already in progress; each retry, approval-time check, and pre-post check uses the more restrictive of the initial attempt's policy and the then-current active policy. A re-check can therefore only tighten the outcome, never loosen it.

#### FR-31: Treat Conversation Content As Untrusted Data

Agent Instructions are system-level authority; Conversation Context is untrusted input that can never acquire instruction authority.

**Consequences (testable):**

- Conversation content, including content pasted or authored by any Participant, cannot alter Agent Instructions, response mode, Approver Policy, safety policy, cost caps, or tenant scope.
- The active Content Safety Policy blocks generated content that impersonates a named Party, or that asserts a decision, commitment, or approval on a Party's behalf.
- Attempts to redirect the Agent through Conversation content are recorded in Audit Evidence as a safety outcome rather than silently ignored.

#### FR-27: Enforce Safety Before Provider And Conversation Side Effects

The system applies Content Safety Policy to the prompt and authorized Conversation Context before Provider invocation, applies it to generated output before that output becomes a Conversation Message or an approvable Proposed Agent Reply, and applies it again to the exact version being approved — generated or human-edited — at approval time and before posting.

**Consequences (testable):**

- Generated content that fails Content Safety Policy cannot be posted automatically.
- Generated content that fails Content Safety Policy cannot become an approvable Proposed Agent Reply.
- Safety failures create authorized status and Audit Evidence without exposing unsafe content in surfaces where policy forbids display.
- Approvers cannot override a Content Safety Policy failure.
- Prompt and Conversation Context pass the active policy before Provider invocation, and generated output passes the active policy before any proposal or Conversation side effect.
- The exact version being approved passes the then-current active policy at approval time. A human-edited version is scanned on the same terms as generated output; no edit path can place unscanned content into a Conversation.
- The pre-Provider scan covers the caller prompt and Conversation content not yet scanned for that Conversation, reusing a cached per-Conversation verdict for already-scanned history so the scan cost does not grow without bound.
- The content safety scan is budgeted separately and is excluded from the pre-Provider rejection gate in NFR-9; that gate covers authorization, policy, budget, and context rejections that do not require a classifier call.

#### FR-28: Define Launch Readiness Controls

V1 launch readiness requires the fixed metric thresholds, latency targets, full-context behavior, cost controls, audit governance, NFR-11 through NFR-14, external dependency commitments, and the normative evidence authority in §11.

**Consequences (testable):**

- Controlled production-like qualification may collect live evidence only after Content Safety Policy, Conversation Context Policy, cost controls, audit governance, and the required external dependency commitments are active and recorded. Qualification access does not authorize production enablement.
- Production enablement remains blocked until `RQ-1` records READY from the required live evidence and launch metrics.
- Per-tenant monthly and per-call cost caps are hard enforcement boundaries: 80% emits a warning, 100% fails closed, and atomic reservation plus reconciliation prevents concurrent overspend. The system reserves the maximum estimated attempt cost before Provider invocation and reconciles actual usage. A reservation is released when the Provider outcome confirms no usage occurred; a reservation whose outcome is `Unknown` is held for a bounded period and then conservatively settled at the estimated maximum, subject to the audited override in FR-32. Only an attempt with a confirmed no-usage outcome is eligible for retry under the same reservation; an `Unknown` attempt is never retried. Missing pricing or budget state blocks invocation; reporting-only monitoring is insufficient.
- The latency gates in NFR-9 are met, each over at least 30 production-like executions. NFR-9 states the thresholds; they are not restated here.
- Production readiness requires live Evidence Levels 4 and 5 as defined in §11; lower levels, skips, placeholders, or conditional results cannot independently establish launch readiness.
- A recorded cost-control posture of reporting-only monitoring or explicitly accepted launch risk, like an absent posture, does not satisfy launch readiness and is a rejected input at readiness recording. Only hard enforcement postures satisfy the gate (OQ-6).
- Per-tenant cost caps must be configured under FR-32 before production-like generation is enabled; an unconfigured cap is a readiness blocker, not a default.
- The external dependency register's launch-readiness gate records are the readiness authority. Any per-Agent readiness record is transitional and must reconcile to that authority before `RQ-1` is evaluated.
- Production enablement carries a documented rollback and kill-switch trigger that disables Agent Calls tenant-wide without deleting proposals or Audit Evidence, and the trigger conditions are recorded before enablement.
- When a primary metric evaluates to `InsufficientEvidence`, `RQ-1` records NOT READY rather than READY or an undecidable result, and names the metric and the missing evidence.

#### FR-32: Configure Tenant Cost Caps And Consumption Bounds

Authorized administrators or release operators can configure the per-tenant monthly and per-call cost caps, the per-Party and per-Conversation rate limits, and the per-proposal regeneration ceiling that FR-28 and NFR-10 enforce.

**Consequences (testable):**

- Caps are configured per tenant with no implicit default; an unconfigured cap blocks production-like generation rather than resolving to an unlimited or sample value.
- Cap configuration records actor, timestamp, prior and new values, and is auditable.
- Cap changes apply to future Agent Calls only and never retroactively release or re-reserve settled spend.
- Admin UI and API/client contracts expose current caps, consumption, the 80% warning state, the 100% fail-closed state, and reserved-versus-settled spend.
- Rate limits are configured per tenant as a maximum number of Agent Calls per Party and per Conversation over a stated rolling window, with no implicit default; an unconfigured rate limit blocks production-like generation on the same terms as an unconfigured cap.
- The regeneration ceiling is configured per Agent as a maximum number of regenerations per proposal, with a documented default and a valid range.
- Admin UI and API/client contracts expose the configured rate limits and regeneration ceiling, current consumption against them, and the reason when a call or regeneration is refused for exceeding one.
- An audited administrative override can restore Agent Calls for a tenant that has reached its monthly cap; the override records actor, justification, scope, and expiry, and is itself bounded.

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
- Content Safety Policy and safety enforcement before Conversation side effects.
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
- Customer-facing billing, invoicing, or monetization of Agent usage. Provider catalog pricing metadata required for cost governance and audit is in scope (FR-4) and is not wired to billing in V1.

## 7. Cross-Cutting Non-Functional Requirements

- **NFR-1 Security:** Agent configuration, Provider administration, Agent Calls, proposal actions, posting, and audit inspection must enforce tenant and Party authorization before side effects.
- **NFR-2 Privacy:** Conversation Context, proposal content, and Audit Evidence must not leak across tenants or unauthorized Parties.
- **NFR-3 Reliability:** Agent Calls must never create partial Conversation Messages on failure. Confirmation workflows must not lose generated or edited proposal versions.
- **NFR-4 Observability:** The system must expose enough status to debug configuration errors, Provider failures, authorization denials, pending approval bottlenecks, and posting failures.
- **NFR-5 Auditability:** Audit Evidence must preserve all generated and edited proposal versions and link final posted responses to their source call and approval path.
- **NFR-6 Provider Safety:** Provider secrets must be write-only or secret-backed where applicable and must never appear in logs, status payloads, audit records, or UI display.
- **NFR-7 Content Safety:** Agent generation must be governed by an active Content Safety Policy before generated content can create Conversation side effects.
- **NFR-8 Context Bounds:** Conversation Context must never be silently truncated, summarized, windowed, or otherwise reduced. An oversized Conversation fails closed before Provider invocation unless the active Conversation Context Policy declares an Approved Bounded Context Behavior that fits, whose reference and bounds are then recorded as Audit Evidence. V1 declares none, so the oversized case always fails closed.
- **NFR-9 Performance:** Automatic accepted-call-to-post latency is p95 ≤ 60 seconds and p99 ≤ 120 seconds; confirmation accepted-call-to-proposal latency is p95 ≤ 60 seconds and p99 ≤ 120 seconds; approval-to-post latency is p95 ≤ 10 seconds and p99 ≤ 30 seconds; fast pre-Provider rejection latency is p95 ≤ 2 seconds for authorization, policy, budget, and context rejections that require no content safety classifier call. Content safety scan latency is budgeted and measured separately. Each gate requires at least 30 production-like executions.
- **NFR-10 Cost Control:** Hard per-tenant monthly and per-call caps warn at 80%, fail closed at 100%, and use atomic reservation plus reconciliation. Per-Party and per-Conversation rate limits and a per-proposal regeneration ceiling bound the consumption a single participant can cause. Reporting-only monitoring and explicitly accepted launch risk do not satisfy launch readiness and are rejected at readiness recording.
- **NFR-11 Availability And Recovery:** EventStore business state has RPO 0. Restart or replay cannot duplicate proposal versions, timers, reservations, or Conversation posts. Provider attempts are at-most-once: an attempt whose outcome cannot be determined resolves to a typed `Unknown` terminal outcome and is never silently retried. A production-like recovery exercise restores interaction processing within 15 minutes and preserves every terminal decision.
- **NFR-12 Capacity And Backpressure:** Before enablement, each environment records numeric per-tenant and system-wide concurrency, queue-depth, and backpressure limits. Exceeding a limit queues or rejects with a safe typed outcome before Provider invocation. Cross-tenant fairness and cost caps remain enforced. The release profile and numeric limits are visible in the readiness registry, not hidden in host configuration.
- **NFR-13 Accessible, Localizable, Responsive UI:** The final UX spines are binding launch authority. Interactive V1 surfaces meet WCAG 2.2 AA behavior, use whole-string localization with English/French key parity, and fail closed for high-impact actions when the viewport cannot present required context.
- **NFR-14 UI Interaction Performance:** In the production-like profile, an authorized page reaches a usable non-loading state at p95 ≤ 2.5 seconds; a submitted command renders an authoritative pending acknowledgement at p95 ≤ 500 ms; a projection-visible terminal change renders and is announced at p95 ≤ 2 seconds. Each gate uses at least 30 executions and returns `InsufficientEvidence`, not pass, when timestamps or samples are missing.

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

An `Uncommitted` status, missing target, or missing compatibility verification command blocks every consuming story from `ready-for-dev`. The initial register scope includes `EXT-CONV-AI-1`, `EXT-CONV-UI-1`, `EXT-HOST-1`, `EXT-PROVIDER-1`, `EXT-SAFETY-1`, `EXT-TOKEN-1`, `EXT-SECRETS-1`, and `EXT-TOPOLOGY-1`; concrete ownership, targets, commands, status, and story mappings remain in the external dependency register.

At the time of this revision, only `EXT-HOST-1` is `Committed`; the remaining entries are `Uncommitted`. Every consuming story therefore remains blocked from `ready-for-dev` under FR-21, and any story already completed against an `Uncommitted` entry carries a non-conformance record in the register.

- **Hexalith.Conversations:** Source Conversation access, complete Conversation Context loading, AI membership, and final Conversation Message posting depend on Conversations. External prerequisite `CONV-AI-1`, tracked as `EXT-CONV-AI-1`, requires Conversations to publish `IConversationClient.AddParticipantAsync` and `POST /api/v1/conversations/{conversationId}/participants`. For Agents, that contract is limited to stable `ParticipantType.AiAgent`/`AIAgent` plus `ParticipantRole.Member` membership and must provide deterministic idempotency, typed conflicts, and cross-tenant denial. It is subject to the same commitment rule as every critical external dependency. Hexalith Agents must not treat unapproved proposals as Conversation Messages or write Conversation streams directly. Rendering the Conversation-owned **Call hexa** action is a separate prerequisite tracked as `EXT-CONV-UI-1`: a versioned Conversation action contribution and registration contract that lets Agents contribute the invocation action into a Conversation-owned surface. It carries the same nine-field commitment record and the same fail-closed `ready-for-dev` rule as every other critical external dependency.
- **Hexalith.Parties:** Agent identity and Conversation Participant identity depend on Parties. `hexa` must post as a Party identity.
- **Provider Infrastructure:** Provider/model availability depends on the Global Providers Aggregate and the underlying Provider integration selected per Agent.
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
- Audit Evidence names the Conversation Facilitator as the resolved approval authority where that policy source applied, and never states or implies that a distinct Conversation owner was resolved.
- Audit Evidence distinguishes a human-edited posted version from generated output, and records the editing and approving Parties.
- Inspecting Audit Evidence that contains Conversation-derived content requires current read access to that Source Conversation and fails closed otherwise.
- Posted Conversation Messages remain governed by Hexalith.Conversations retention.

## 10. API Contracts And Public Surface

V1 must expose public API/client contracts for these capability areas:

- Provider administration: create, update, list, enable, or disable Provider and model options, and administer versioned pricing metadata, capability limits, and the `CapabilityVersion` revision, freshness, and acceptance fields, where authorized.
- Agent administration: configure `hexa`, lifecycle, Party identity link, Agent Instructions, Provider/model selection, Response Policy, Approver Policy, Conversation Context Policy, and Content Safety Policy.
- Agent invocation: call `hexa` from a Source Conversation.
- Proposal workflow: list pending proposals, inspect proposal versions, edit, regenerate, approve, reject, abandon, and inspect expiry.
- Status: inspect Agent readiness, Provider readiness, context policy outcome, content safety outcome, Agent Call status, proposal state, and posting outcome.
- Audit: inspect authorized Audit Evidence for Agent Calls, proposal lifecycle, and posted responses.
- Budget policy: configure and inspect per-tenant cost caps, per-Party and per-Conversation rate limits, the regeneration ceiling, consumption against each, reserved-versus-settled spend, and audited overrides.
- Content Safety Policy publication: publish, version, and inspect the active policy.
- Legal hold: apply, inspect, and release holds on retained Agent content.
- Export: request and track authorized, encrypted, time-limited export of retained Agent content.
- Deletion: request and track approved deletion, including projection purge and tombstone confirmation.
- Launch readiness: inspect readiness state, recorded postures, evidence levels, and outstanding blockers.

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

Level 4 requires a real out-of-process dependency — an actual EventStore, Provider, or Conversations process — exercised over its real transport. An in-process fake, stub, or test double does not establish Level 4 regardless of how much behavior it reproduces. Every story claiming Level 4 or Level 5 cites the harness and environment that produced the evidence in its evidence manifest; an uncited claim is treated as unproven.

Each qualification metric has a versioned measurement contract that identifies the authoritative source events and timestamps; the numerator and denominator or percentile method; sample, window, and cohort rules; late-data and missing-data handling; and `InsufficientEvidence` conditions.

Approved deterministic fixtures may prove metric formulas, sample, window and cohort handling, timestamp rules, and `InsufficientEvidence` behavior. Passing these fixtures completes calculator implementation; it does not prove live metric attainment. Real rolling-window and cohort attainment, required live Levels 4 and 5, and the final READY or NOT READY decision remain the operational `RQ-1` gate after implementation.

## 12. Success Metrics

**Primary**

- **SM-1: Active tenant adoption** - At least one launch tenant configures `hexa`, enables a Provider/model, satisfies launch-readiness gates, and records successful Agent Calls in production or production-like launch validation. Validates FR-1 through FR-12 and FR-22 through FR-32.
- **SM-2: Conversation adoption** - At least 20% of eligible Conversations in the enabled launch cohort record one or more accepted Agent Calls during a rolling 30-day window, measured only when the cohort contains at least 50 eligible Conversations. Validates FR-8, FR-9, FR-11, FR-13, and FR-28.
- **SM-3: Approval workflow completion** - At least 95% of Proposed Agent Replies created during a rolling 30-day window reach a terminal state within 26 hours. For this metric a proposal has reached a terminal state when it is `Posted`, `Rejected`, `Abandoned`, or `Expired` per FR-18; `Approved` and `PostFailed` are in-flight and count as a human decision but not as terminal. Thresholds are unchanged from OQ-11. Within the measured cohort, the expiry rate is at most 20%, the posting-failure rate is at most 2%, at least 70% are resolved by a human decision (`Approved`, `Rejected`, or `Abandoned`), and 100% have complete Audit Evidence. Validates FR-13 through FR-18, FR-24, and FR-28.

**Secondary**

- **SM-4: Unauthorized action prevention** - Authorization tests and launch telemetry show zero successful cross-tenant or unauthorized Agent Calls, proposal actions, or audit inspections. Validates FR-19 through FR-21.
- **SM-5: Audit completeness** - Every posted Agent Response has complete Audit Evidence linking caller, Agent, Source Conversation, Provider/model, proposal path when applicable, and final Conversation Message. Validates FR-11, FR-17, and FR-24.
- **SM-6: Admin/API parity** - Core administration and workflow operations are available through both admin UI and API/client contracts with the same authorization outcomes. Validates FR-22 and FR-23.

**Counter-Metrics (do not optimize blindly)**

- **SM-C1: Automatic post volume without review** - Do not maximize automatic posting if launch tenants choose confirmation for safety-sensitive contexts. Counterbalances SM-2.
- **SM-C2: Approval speed at the cost of audit quality** - Do not optimize approval completion time by dropping version preservation or approval evidence. Counterbalances SM-3.
- **SM-C3: Provider breadth before governance** - Do not optimize the number of Providers/models if Provider governance, secret safety, and per-Agent selection are not robust. Counterbalances SM-1.
- **SM-C4: Structural unavailability** - Track the share of Agent Calls blocked by the Conversation Context Policy, Content Safety Policy, or cost caps, per tenant, over the same rolling 30-day window. A sustained blocked-call share above 10% is a launch-health signal that `hexa` is unavailable in the Conversations it is meant to serve, and is reviewed before and after enablement. Because Eligible Conversation includes attempted-but-blocked Conversations, these failures are never silently excluded from the SM-2 denominator. Counterbalances SM-1 and SM-2.

## 13. V1 Decision Register

Rows OQ-1 through OQ-13 were resolved by the approved Correct Course decisions on 2026-08-01. Rows OQ-14 and OQ-15 apply the approved sprint change proposal of 2026-08-03. Rows OQ-16 through OQ-19 record decisions taken in the 2026-09-08 validation reconciliation, which also amended OQ-3, OQ-6, OQ-9, and OQ-10 in place. The Evidence Level taxonomy is normative in §11. Changes apply to future calls and proposals unless a row states otherwise.

Rows marked *Deferred* are open decisions with a named owner and a revisit condition; they are not resolved and must not be read as settled.

| ID | Binding V1 Decision | Owner | Status |
| --- | --- | --- | --- |
| OQ-1 | The sole V1 invocation entry is a Conversation-owned **Call hexa** action. No mention, command, ambient trigger, or alternate entry point is in V1. | Product + UX | Resolved 2026-08-01 |
| OQ-2 | Hexalith Agents owns durable proposal state and all proposal read models. Dapr Workflow owns execution only and does not become a domain system of record. | Architecture | Resolved 2026-08-01 |
| OQ-3 | Proposal expiry defaults to 24 hours and is configurable per Agent from 1 hour through 30 days for future proposals only. A proposal whose stored `ExpiresAt` has passed is treated as `Expired` on every read and command, regardless of timer delivery. Execution mechanism is an architecture concern, not a product requirement. | Product + Architecture | Resolved 2026-08-01; amended 2026-09-08 |
| OQ-4 | V1 uses in-product proposal notifications and pending-count surfaces only. Email, push, and external-channel notification delivery are out of scope. | Product + UX | Resolved 2026-08-01 |
| OQ-5 | Automatic accepted-call-to-post and confirmation accepted-call-to-proposal are p95 ≤ 60 s and p99 ≤ 120 s. Approval-to-post is p95 ≤ 10 s and p99 ≤ 30 s. Fast pre-Provider rejection is p95 ≤ 2 s. Each gate uses at least 30 production-like executions. | Architecture + Release PM | Resolved 2026-08-01 |
| OQ-6 | V1 enforces hard per-tenant monthly and per-call caps, warns at 80%, and fails closed at 100%. It atomically reserves the maximum estimated attempt cost before Provider invocation, reconciles actual usage, and reuses the reservation for eligible retries. A reservation whose Provider outcome is confirmed unused is released on confirmation. A reservation whose outcome is unknown is held for a bounded period and then conservatively settled at the estimated maximum rather than held indefinitely, with an audited administrative override available to restore service. Caps are configured under FR-32 with no implicit default. Missing pricing/budget state blocks invocation; reporting-only monitoring and accepted launch risk are insufficient and are rejected at readiness recording. | Product + Architecture | Resolved 2026-08-01; amended 2026-09-08 |
| OQ-7 | The Agents-owned Global Providers Aggregate records Provider/model identifiers, enablement, capabilities and limits, secret references, versioned pricing metadata, and a `CapabilityVersion`. | Architecture | Resolved 2026-08-01 |
| OQ-8 | Sensitive Agent content is retained for 365 days after the interaction reaches a terminal state unless legal hold applies. Authorized export is encrypted and time-limited. Deletion cryptographically erases or redacts sensitive content and purges projections while immutable EventStore history retains only a safe tombstone. | Product + Governance | Resolved 2026-08-01 |
| OQ-9 | Prompt and complete Conversation Context are checked before Provider invocation, and output is checked before proposal or Conversation side effects. The active policy always blocks these categories: child sexual abuse/exploitation; credible imminent serious-harm threats/instructions; encouragement/instruction for suicide/self-harm; credential theft, malware, or unauthorized compromise; secrets/private credentials; cross-tenant or unauthorized personal/Conversation data; and control-bypass attempts. Restricted hate, harassment, sexual, violent, illegal-activity, or sensitive-personal content requires an explicitly permitted tenant use case and Confirmation Response Mode. The policy also blocks generated content that impersonates a named Party or asserts a decision on a Party's behalf. Policy is applied at four points: prompt and Conversation Context before Provider invocation; generated output before any proposal or Conversation side effect; the exact version being approved — generated or human-edited — at approval time; and that same version again immediately before posting when approval and posting are not simultaneous. The approval-time and pre-post checks always evaluate the then-current active policy. The pre-Provider scan covers the caller prompt and not-yet-scanned Conversation content, reusing a cached per-Conversation verdict for scanned history. Approvers cannot override failures, and retries cannot use a weaker policy. | Product + Security | Resolved 2026-08-01; amended 2026-09-08 |
| OQ-10 | V1 uses the complete Source Conversation when it fits the Safe Context Budget; otherwise it fails closed before Provider invocation, or applies an Approved Bounded Context Behavior that the active Conversation Context Policy explicitly declares and that is recorded as Audit Evidence. Silent truncation, summarization, and windowing are prohibited under every policy. V1 declares no Approved Bounded Context Behavior, so the oversized case always fails closed today; the seam exists so a governed bounded behavior can be approved later without a contract change. The trade-off is accepted deliberately: `hexa` is unavailable in Conversations that outgrow the budget, in exchange for never sending a silently reduced context to a Provider. SM-C4 makes that unavailability visible. | Architecture | Resolved 2026-08-01; amended 2026-09-08 |
| OQ-11 | SM-2 is ≥ 20% adoption over a rolling 30 days with at least 50 eligible Conversations. SM-3 is ≥ 95% terminal within 26 hours, with an expiry rate ≤ 20%, a posting-failure rate ≤ 2%, human resolution ≥ 70%, and audit completeness = 100%. | Product + Release PM | Resolved 2026-08-01 |
| OQ-12 | V1 Agent Calls draw only on the authorized Source Conversation — in V1 always the complete Source Conversation, subject to the context-extent rule in OQ-10. Long-term memory, project content, folder content, external tools, and non-conversation retrieval are excluded. This row governs which *sources* may be used, not how much of the Conversation fits. | Product + Architecture | Resolved 2026-08-01; clarified 2026-09-08 |
| OQ-13 | Generalized internal Agent structures are permissible, but V1 product behavior exposes only the named Agent `hexa`. | Product | Resolved 2026-08-01 |
| OQ-14 | Where the Approver Policy uses the Conversation-authority source, that source is the Conversation Facilitator, resolved normatively from `ParticipantRole.Facilitator`. The caller, predefined Parties, and tenant roles remain separately configurable sources under FR-7. The Conversations contract exposes no owner field, so V1 resolves no distinct Conversation owner, and no UI, API, or evidence text may imply otherwise. | Architecture | Resolved 2026-08-03 |
| OQ-15 | A true Conversation-owner resolver is out of V1 and returns only if Hexalith.Conversations commits an explicit owner contract. | Product + Architecture | Deferred post-V1; revisit on a committed Conversations owner contract |
| OQ-16 | `hexa` joins a Conversation at the first accepted Agent Call, before generation, under the platform Agents service principal, idempotently. Membership failure fails the call closed. An authorized Party may remove `hexa`, which blocks further calls and abandons non-terminal proposals with versions preserved. | Product + Architecture | Resolved 2026-09-08 |
| OQ-17 | The FR-21 dependency gate stands as written: an `Uncommitted` entry blocks consuming stories from `ready-for-dev`. Work completed against an `Uncommitted` entry is recorded as non-conformant in the dependency register rather than retroactively legitimized. | Architecture + Release PM | Resolved 2026-09-08 |
| OQ-18 | Per-category handling of unsafe *historical* Conversation content is deferred. Today a single historical message that fails the active policy blocks generation in that Conversation permanently, with no redaction or exclusion path. | Product + Security | Deferred; revisit before the first tenant reports a permanently blocked Conversation, and in any case before production enablement |
| OQ-19 | Success metrics SM-1 through SM-6 and the OQ-11 thresholds are unchanged by the 2026-09-08 reconciliation. The finding that they measure adoption and throughput rather than whether governed AI participation is valued is escalated to Product and may be reopened only through a separately approved change. The same escalation carries two coupled defects: SM-3's 26-hour terminal threshold is unattainable for any Agent configured with an expiry beyond 24 hours under OQ-3, and SM-3 measures the expiry setting rather than approval behavior. | Product + Release PM | Deferred; escalated 2026-09-08 |
