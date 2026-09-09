---
title: Hexalith Agents
status: final
created: 2026-06-23
updated: 2026-09-09
---

# PRD: Hexalith Agents

## 0. Document Purpose

This PRD defines the launch-level V1 requirements for Hexalith Agents, the governed AI participant capability for Hexalith Conversations. It is intended for product, UX, architecture, implementation, QA, and release-readiness workflows. The document uses glossary-anchored terms, grouped features, globally stable functional and non-functional requirement IDs, explicit non-goals, launch success metrics, normative evidence authority, and a binding V1 Decision Register. It builds on the product brief at `/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/briefs/brief-agents-2026-06-23/brief.md` and preserves external landscape notes in `addendum.md`.

The frontmatter `status: final` states that this document is the governing requirements authority. It does not state that the build is implementation-ready: build readiness is owned by the external dependency register (commitment authority for `EXT-*` entries), the launch readiness register (gate records), and the `RQ-1` gate (FR-28); §8 reports the current dependency status without softening it. Inferences about external contracts that no owner has yet confirmed carry an `[ASSUMPTION A-n]` key and are collected in the Assumptions Index at the end of §8.

Status as of 2026-09-09: all critical external dependencies except `EXT-HOST-1` are `Uncommitted` — eight of the nine register entries (§8) — and none is `Available`, so live content-bearing execution stays disabled under FR-34. Every unretired assumption — the `A-n` rows in §8.1 and the `ARCH-A-n` rows in the Architecture Spine's own table — blocks `RQ-1` (FR-28). No consuming story is `ready-for-dev`.

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
- As a Compliance Inspector — a tenant or compliance operator holding the compliance-inspection authorization in FR-33 — prove who called the Agent, what it generated, what changed during approval, who approved it, and what was finally posted, without being made a Participant of every Conversation.
- As an Integration Developer, use stable API/client contracts to configure Agents, call Agents, manage proposals, and inspect audit/status without depending on internal implementation details.

### 2.2 Non-Users In V1

- Users who want autonomous Agents that react to every project, folder, or Conversation change without explicit invocation.
- Users who want Agents to take business actions outside adding Agent responses to Conversations.
- Users who need external-channel bots for Slack, Teams, email, or other non-Hexalith channels.
- Users who need tool-using Agents, long-term memory, retrieval over project/folder content, or agent-to-agent orchestration in V1.

### 2.3 Key User Journeys

- **UJ-1. Nora configures `hexa` for a tenant launch.**
  - **Persona + context:** Nora is an Agent Administrator preparing a tenant to use governed AI in Conversations.
  - **Entry state:** Nora is authenticated in the admin surface with permission to manage Agent configuration and to select among the Provider/model options the Platform Operator enabled for her tenant.
  - **Path:** Nora opens the Agents admin area, enables the `hexa` that the Platform Operator provisioned at tenant enablement (FR-1), confirms the linked Party identity, selects a Provider/model from the options the Platform Operator has enabled for her tenant, enters instructions, chooses response mode, and defines who can approve proposed replies.
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
  - **Persona + context:** Anika is an Approver in a tenant whose `hexa` runs in Confirmation Response Mode, so every Agent Response in that tenant requires confirmation before it is posted (response mode is tenant-wide, FR-6).
  - **Entry state:** A Conversation Participant has invoked `hexa`, and the Agent is configured for Confirmation Response Mode with Anika in the Approver Policy.
  - **Path:** The system creates a Proposed Agent Reply outside the Conversation. Anika opens the proposal, reviews the generated content and context metadata, requests one regeneration, compares the versions, and approves the generated version she did not author. Had she instead edited the draft herself, a second authorized Approver would have to approve that edited version, because no Party may approve a version it last edited; had no second Approver been resolvable for this proposal, the system would have refused her edit rather than strand the proposal (FR-7).
  - **Climax:** The approved draft is posted into the source Conversation as `hexa`, with approval evidence linked to the posted message.
  - **Resolution:** The Conversation contains only the approved response, while the proposal record preserves every state it passed through — awaiting decision, approved, posting, posting failed, posted, rejected, abandoned, or expired — for audit.
  - **Edge case:** If the proposal expires before approval, it cannot be posted and a new Agent call is required. If Anika loses read access to the Conversation while the proposal is pending, she can still see that it exists and what state it is in, but not its content, and she can no longer approve it (FR-7, FR-13).

- **UJ-4. Omar integrates Agent operations through the API.**
  - **Persona + context:** Omar is an Integration Developer building tenant automation and operations checks around Hexalith Agents.
  - **Entry state:** Omar has API credentials with the appropriate tenant and Agent permissions.
  - **Path:** Omar uses public API/client contracts to list Provider options, configure an Agent, inspect proposal status, approve or reject proposals where authorized, and query audit evidence.
  - **Climax:** The integration can perform the same governed operations as the admin UI without using internal EventStore, Party, or Conversation implementation details.
  - **Resolution:** Tenant automation can onboard and monitor Agents while preserving the same authorization and audit guarantees as the first-party UI.
  - **Edge case:** If Omar's integration calls an operation his credentials do not authorize, the call is denied with a typed result before any side effect, and the denial is auditable without revealing the content he could not access.

## 3. Glossary

- **Accepted Agent Call** - An Agent Call that has passed every pre-Provider check in the order FR-8 lists. The acceptance timestamp is recorded when the last check passes; it starts the NFR-9 latency clocks. A regeneration is an Agent Call for cost-cap, rate-limit, and audit purposes but is not counted in the SM-2 numerator.
- **Agent** - A configured AI participant managed by Hexalith Agents. In V1, the first Agent is `hexa`, instantiated once per tenant (FR-6, OQ-20).
- **Agent Administrator** - A Party or operator role authorized to configure Agents: Provider/model selection among tenant-enabled options, Response Policy, Approver Policy, Conversation Context Policy, lifecycle, expiry duration, regeneration ceiling, and acceptance of a Provider's data-handling record for the tenant. FR-33 names this tenant-scoped role the Tenant Agent Administrator; the two names denote the same role.
- **Agent Call** - An explicit request from a Conversation Participant to an Agent from within a Conversation (code: `AgentInteraction`).
- **Agent Instructions** - Administrator-defined instructions that guide the Agent's behavior for generated replies.
- **Agent Response** - Content generated by an Agent for a source Conversation. It becomes a Conversation Message only after it is posted, automatically or following approval; approval alone does not make it a Conversation Message.
- **Agents Service Principal** - The platform-level identity under which Hexalith Agents establishes Agent membership and posts Agent Responses to Hexalith.Conversations. It acts for the Agent's Party identity, never for the caller or the Approver, and is the principal whose Conversation access is checked at membership and posting time (FR-2, FR-21).
- **Approved Bounded Context Behavior** - An explicitly declared, audited context-reduction behavior that a Conversation Context Policy may permit when the complete Source Conversation exceeds the Safe Context Budget. Its reference and bounds are recorded as Audit Evidence on every call that uses it, so bounded context is never a silent truncation. V1 declares no Approved Bounded Context Behavior.
- **Approver** - A Party authorized by the Agent's Approver Policy to edit, regenerate, approve, reject, or abandon Proposed Agent Replies.
- **Approver Policy** - Agent configuration that defines all Parties or roles allowed to approve Proposed Agent Replies, through the configured sources — Conversation Facilitator, predefined Parties, and tenant roles. The caller source is retired in V1 (FR-7, FR-23). V1 approval authority is fully defined by Agent configuration, subject to the Conversation-access and eligibility constraints in FR-7.
- **Audit Evidence** - Durable records connecting caller, Agent, Provider/model, Source Conversation, generated versions, edits, regenerations, approval decisions, posting outcome, timestamps, and authorization decisions.
- **Automatic Response Mode** - Agent Response mode where the generated response is posted directly to the Conversation as the Agent's Party identity after generation succeeds.
- **Compliance Inspector** - One of the six FR-33 roles: a tenant-scoped role holding the compliance-inspection authorization. It may inspect Audit Evidence that contains Conversation-derived content without being a Participant of the Source Conversation, subject to a recorded justification and audit of the inspection itself (FR-24).
- **Confirmation Response Mode** - Agent Response mode where generated output becomes a Proposed Agent Reply and requires approval before posting.
- **Content Safety Policy** - The configured launch policy that defines prompt constraints, blocked or restricted output categories, safety failure handling, and audit treatment for Agent generation.
- **Conversation** - A tenant-scoped Hexalith.Conversations record containing durable multi-party discussion content.
- **Conversation Agent State** - The Agents-owned per-Conversation record of `hexa`'s membership standing, with exactly five states: `NeverJoined`, `Joined`, `ExternallyRemoved`, `Blocked`, and `ReadmitPending` (FR-2), plus a `BlockVersion` counter and a `MirrorPending` flag. It is authoritative the moment it is written, independent of whether Conversations is reachable.
- **Conversation Context** - The Source Conversation content the caller is authorized to read, plus related participant/context metadata, supplied to the Agent for a V1 Agent Call. It is the complete Source Conversation unless the active Conversation Context Policy declares an Approved Bounded Context Behavior; V1 declares none, so it is always the complete Source Conversation. V1 Conversation Context excludes long-term memory, project content, folder content, external tools, and non-conversation retrieval.
- **Conversation Context Policy** - The versioned rule set that requires the complete Source Conversation to be sent to the Provider when it fits the selected Provider/model's Safe Context Budget, and otherwise either fails closed or applies an Approved Bounded Context Behavior the policy explicitly declares. V1 declares none, so the oversized case always fails closed. Silent truncation, summarization, or windowing is prohibited under every policy.
- **Conversation Facilitator** - The V1 Conversation authority used as an Approver Policy source, resolved normatively from `ParticipantRole.Facilitator` [ASSUMPTION A-3]. Its stable wire identifier in the Agents contract is `ApproverPolicySourceKind.ConversationOwner`, a legacy name that FR-23 forbids renaming within V1; every human-readable label calls it Conversation Facilitator. The Conversations contract exposes no owner field, so V1 resolves no distinct Conversation owner; a true-owner resolver is post-V1 (OQ-15).
- **Conversation Message** - Durable content posted to a Conversation through Hexalith.Conversations.
- **Conversation Participant** - A Party with access to a Conversation who may call `hexa` when authorized.
- **Eligible Approver** - For a given Proposed Agent Reply at a given moment, a Party that the Approver Policy resolves from the Source Conversation's current Participants, that holds current read access, that is not the caller of the Agent Call, and that is not the Party that last edited the version under decision. Every proposal action in FR-7 tests this one predicate.
- **Eligible Conversation** - For success-metric measurement, any tenant Conversation in the enabled launch cohort that was active during the measurement window — at least one Conversation Message posted by any Party — while `hexa` was active for that tenant and its Participants were not excluded from calling under FR-33. Eligibility holds whether or not a call was attempted. Eligibility measures reach, so SM-2 remains an adoption metric rather than an acceptance rate. [ASSUMPTION A-4] A Conversation in which a call was attempted but blocked for any SM-C4 blocked-call reason, or failed at the Provider, remains eligible and stays in the denominator; SM-C4 defines the blocked-call reasons and tracks their share directly.
- **Evidence Level** - A normative classification of proof strength from Level 1 contract/structure evidence through Level 5 cross-system production-like evidence, as defined in §11.
- **Global Providers Aggregate** - The Hexalith Agents-owned catalog of configured AI providers, models, capability metadata, data-handling records, versioned pricing metadata, and `CapabilityVersion` values available for per-Agent Provider/model selection (code: `ProviderCatalog`).
- **Launch Readiness Gate** - A product, governance, architecture, or release condition that must be resolved before production or production-like launch validation can pass.
- **Party** - A stable Hexalith.Parties identity representing a human, organization, or AI participant.
- **Platform Operator** - The one platform-scoped FR-33 role: administers the Global Providers Aggregate, per-tenant Provider/model enablement, tenant enablement (including provisioning `hexa` for the tenant, FR-1), the Content Safety Policy, initial caps, rate limits, and cap overrides, the kill switch, deletion requests, hold-release and compliance-inspection approvals where FR-24 and FR-33 name it, Platform-scoped readiness observations, and the host binding of the payload-protection engine (FR-34).
- **Provider** - An AI service provider available through the Global Providers Aggregate.
- **Proposed Agent Reply** - Generated Agent output held outside the Conversation until an Approver approves it.
- **Provider/Model Selection** - Per-Agent configuration choosing which Provider and model the Agent uses.
- **Regeneration** - A new generated version of a Proposed Agent Reply created before approval.
- **Release Operator** - The FR-33 role that configures caps and rate limits, records cost-control posture and launch readiness including the `RQ-1` decision, enables production-like generation and production, convenes the kill-switch trigger review and pulls the switch as its recorded decision, and releases it with audited justification.
- **Release Qualification Gate (`RQ-1`)** - The operational gate that, after implementation, evaluates the single `RQ-1` input list stated in FR-28 — and nothing outside it — and records the final READY or NOT READY launch decision in the launch readiness register. Launch-health metrics that only real usage can produce (SM-2, SM-3, SM-7) are reviewed after enablement and are not `RQ-1` inputs (OQ-22).
- **Response Policy** - Agent configuration that determines Automatic Response Mode or Confirmation Response Mode and the related approval behavior.
- **Safe Context Budget** - The token allowance available for Conversation Context on a given call, derived per call as FR-9 specifies (A-5) and recorded term by term in Audit Evidence.
- **Source Conversation** - The Conversation from which an Agent Call originated and to which an approved or automatic Agent Response is posted.
- **Tenant Agent Administrator** - The FR-33 name of the Agent Administrator role; see that entry.
- **Versioned Proposal Content** - Every generated, edited, or regenerated content version associated with a Proposed Agent Reply.

## 4. Features

### 4.1 Agent Identity, Configuration, And Lifecycle

**Description:** The Platform Operator provisions `hexa` for a tenant at tenant enablement; Tenant Agent Administrators then configure, activate, disable, and inspect it as the first governed Agent. An active Agent must have a durable Party identity, valid instructions, valid Provider/model selection, Response Policy, and Approver Policy where confirmation is enabled. This realizes UJ-1.

**Functional Requirements:**

#### FR-1: Configure `hexa`

The Platform Operator provisions `hexa` once per tenant at tenant enablement, as a create-only operation that establishes the stable Agent identity and tenant scope (FR-33, OQ-28). Provisioning also creates `hexa`'s Party identity in Hexalith.Parties as an AI-type Party owned by the Agents Service Principal; that identity is immutable for the life of the tenant. Tenant Agent Administrators then configure and activate it: display name, description, Agent Instructions, lifecycle state, and the selections and policies FR-5 through FR-7 define. No tenant role can create a second Agent, delete `hexa`, or change its Party identity in V1.

**Consequences (testable):**

- Provisioning is idempotent per tenant: a repeated provision for an already-provisioned tenant returns the existing Agent and creates nothing, and a provision attempted by any tenant role is a typed denial.
- The system prevents activation when required Agent fields are missing or invalid.
- The system exposes the current Agent configuration through the admin UI and API/client contracts.
- The system records configuration changes in Audit Evidence with actor, timestamp, prior value (excluding secret references, NFR-6), and new value.

#### FR-2: Link Agent To Party Identity

Tenant Agent Administrators can inspect, but not change, the Agent's Party identity, which FR-1 provisioning created so that `hexa` appears as a known AI participant when it posts to a Conversation.

**Consequences (testable):**

- An active Agent has exactly one Party identity, created at provisioning (FR-1).
- A Party of type human or organization can never be an Agent's Party identity, and no operation re-points an Agent at another Party: the shipped Party link and replace commands (Spine AD-7) are on the FR-23 deprecate-and-reject register, and a link or replace naming any Party is a typed rejection.
- The system rejects posting an Agent Response when the Agent Party identity is missing, disabled, ambiguous, or unauthorized for the Source Conversation.
- Conversation Messages posted by `hexa` are attributable to the Agent's Party identity, not to the caller or a generic system account.
- The Agent joins a Conversation as a participant during acceptance of the first Agent Call in that Conversation. Membership is the last pre-Provider step of an Accepted Agent Call (§3) and is decided by the Conversation Agent State (§3) together with `hexa`'s participant standing read from Conversations. The step reads both and acts on the pair:
  - `NeverJoined` or `ReadmitPending`: add `hexa` as a participant and record `Joined`. This step never abandons a proposal from `ReadmitPending`.
  - `Joined` and the participant read confirms `hexa` is present: accept; nothing is written.
  - `Joined`, the participant read confirms `hexa` is absent, the last mirror is confirmed, and no clear is newer than that mirror: record `ExternallyRemoved` — a rejecting state on the same terms as `Blocked`, with no mirror because there is nothing to remove — abandon the non-terminal proposals for that Conversation with the typed reason `RemovedInConversations` under the FR-18 rules, and reject the call with that reason. An absence observed while a mirror is unconfirmed or a clear is newer is the mirror's late effect, not an external removal; the state is left as it is.
  - `Blocked`: reject the call with the typed reason `RemovedInConversations` before any Provider work; no join is re-established.
  - `ExternallyRemoved` and the participant read confirms `hexa` is absent: reject on the same terms as `Blocked`. `ExternallyRemoved` and the read confirms `hexa` is present again: record `Joined` and accept, because re-adding `hexa` in Conversations is the removal authority's re-admission.
  - Any state when the participant read is unavailable: reject the call with the typed reason `MembershipUnavailable`, change no state, and abandon nothing, because an unavailable read is not evidence of removal.
  A membership step that is only an idempotent join does not satisfy this requirement, because it would re-admit an Agent that a Facilitator removed; a step that treats absence after a cleared block as a fresh removal does not satisfy it either, because it could never re-admit `hexa`. A call whose membership cannot be established is never accepted, spends no Provider budget, and never creates a proposal; membership is never a side effect of an unapproved proposal.
- Membership is added under the Agents Service Principal, not the caller's identity, and is idempotent: a repeated join for the same Agent and Conversation is a no-op that returns the existing membership.
- Membership failure fails the Agent Call closed and creates neither Provider work, a Proposed Agent Reply, nor a Conversation Message. Verifying membership only at posting time, after Provider work, does not satisfy this requirement; the pre-post re-validation in FR-18 is an additional check, not a substitute. `MembershipUnavailable` (the participant read is unavailable) and `MembershipRejected` (Conversations refused the Agents Service Principal for a reason that is not a removal) remain posting-failure reasons for that re-validation only; a removal or block detected at the pre-post check moves the proposal to `Abandoned` with reason `RemovedInConversations`, never to `PostingFailed` (FR-18).
- Agent membership is visible to Conversation Participants on the same terms as any other participant.
- A Tenant Agent Administrator or the Conversation Facilitator may block `hexa` in a Conversation [ASSUMPTION A-9]. The block is set, mirrored, and cleared as follows:
  - Setting: a block may be set from any state; setting it records `Blocked` and increments the `BlockVersion` in the Conversation Agent State. That record is authoritative and durable the moment it is written, whether or not Conversations is reachable, and every call to that Conversation is rejected on it alone.
  - Mirroring: the block is mirrored to the Conversation's participant list through the Conversations participant-removal seam (§8) as an at-least-once outbox carrying the `BlockVersion`, so removal is visible to Participants and not only to Agents. Until the mirror is confirmed the state carries a `MirrorPending` flag that FR-25 exposes, and the mirror is retried until it succeeds. A seam answer that `hexa` is already absent confirms the mirror, and a block set from `NeverJoined` is confirmed without a seam call.
  - Clearing: a block is cleared only by the authority that set it or by the Tenant Agent Administrator — clear authority is evaluated at clear time, so a Party that has since lost the Facilitator role cannot clear — and a Conversation Facilitator cannot clear a block the Tenant Agent Administrator set. An `ExternallyRemoved` record is cleared by the Tenant Agent Administrator or by any current Conversation Facilitator of that Conversation. A clear is refused with a typed reason while a mirror entry is sent and unacknowledged; a clear cancels every outbox entry of a lower `BlockVersion`. Clearing records `ReadmitPending`, and `hexa` re-joins at the membership step of the next Agent Call that reaches it, never at the moment of clearing.
  - Every set and clear is audited.
- Setting the block, or detecting an external removal, moves every non-terminal proposal for that Conversation to `Abandoned` with reason `RemovedInConversations` and its versions preserved, except a proposal in `PostingPending`, which is uninterruptible (FR-18): it is abandoned on exit if it did not post, and a proposal whose approved version is found in the Conversation is recorded `Posted`, never `Abandoned`. A removal performed directly in Hexalith.Conversations rather than through Agents is observed at the next membership step — at acceptance or before posting — which records `ExternallyRemoved`, a rejecting state on the same terms as the block, as required above; Conversations need not notify Agents.

#### FR-3: Manage Agent Lifecycle

Tenant Agent Administrators can activate, disable, and inspect `hexa` lifecycle state. The public lifecycle states are `Draft` (provisioned, not yet valid for activation), `Active`, and `Disabled`; `Unknown` is the FR-23 sentinel. The per-tenant kill switch (FR-28) does not change the lifecycle state: while it is pulled, FR-25 reports the tenant as `Suspended`, an additive status value under FR-23, so an operator can tell a suspended tenant from a disabled Agent.

**Consequences (testable):**

- Disabled Agents cannot be called from Conversations.
- While the Agent is `Disabled` or its tenant is `Suspended`, proposals awaiting a decision accept reject and abandon only; approval, edit, and regeneration are rejected with a typed reason, and `ExpiresAt` continues to run (FR-18). An `Approved` proposal does not start posting while either condition holds: it waits in `Approved`, consuming no retry attempt, and is re-validated when the condition lifts; a `PostingPending` attempt already in flight completes or fails on its own terms.
- Disabling an Agent does not delete existing Audit Evidence, Proposed Agent Replies, or Conversation Messages.
- Lifecycle changes are auditable and visible through admin UI and API/client contracts.

### 4.2 Provider Governance And Per-Agent Model Selection

**Description:** Hexalith Agents uses a Global Providers Aggregate to govern available AI providers and models. Each Agent selects its Provider and model from that governed catalog rather than using an implicit hardcoded Provider. This realizes UJ-1 and UJ-4.

**Functional Requirements:**

#### FR-4: Manage Global Providers Aggregate

The Platform Operator can configure the Global Providers Aggregate with Provider records, model options, enabled/disabled state, versioned pricing metadata, capability limits, and Provider capability metadata needed for Agent selection.

**Consequences (testable):**

- Disabled Providers or models cannot be selected for new Agent configuration.
- Existing Agents using a disabled Provider/model cannot be activated or called until reconfigured. A documented migration state may allow temporary read-only inspection.
- Provider configuration changes are auditable without exposing secrets in logs, API responses, UI, or Audit Evidence.
- Pricing metadata is required on model create and update: a well-formed currency, non-negative unit prices, and a pricing version that starts at 1 on create and strictly increases. A malformed currency, a negative unit price, or a decreased or reused pricing version is a typed rejection, not a silent acceptance.
- Capability limits are required and positive: context limit, output allowance, and request timeout.
- A per-model retry budget, where configured, authorizes re-invocation only for an attempt whose Provider outcome is a confirmed no-usage outcome, under the same cost reservation (FR-28). It never authorizes re-invocation after an `Indeterminate` outcome and is not a silent retry mechanism (NFR-11).
- Each model records its secret reference and whether the Provider is configured, without exposing the secret itself.
- Each Provider/model record carries a data-handling record: the Provider's data-retention term for submitted content, its training-use status (opted out or not), its processing region, and a reference to the contractual terms that establish them. The record carries its own `DataHandlingVersion`, incremented only when one of those four fields changes, and the contractual reference resolves to a retained document the Compliance Inspector can inspect. A model whose data-handling record is absent or incomplete cannot be enabled for any tenant (FR-5, FR-33), because every tenant's Conversation Context leaves the platform under the platform-owned Provider credential (§9). Platform enablement permits selection; it is not acceptance. The Tenant Agent Administrator accepts a named `DataHandlingVersion` for the tenant before the first activation that selects the model, and again when the version changes; until re-acceptance, calls continue under the previously accepted version for 30 days, after which the model cannot be called for that tenant [ASSUMPTION A-10].
- `CapabilityVersion` increases monotonically and is never reused. It acts as the optimistic-concurrency token for catalog writes: a caller-supplied expected version below the stored value is rejected as regressed, and one above it is rejected as stale.
- Pricing metadata is the input to the cost estimation that FR-28 and OQ-6 reserve against; absent or invalid pricing for the selected model blocks Provider invocation. It is not used for customer-facing billing or monetization in V1 (§6.2).

#### FR-5: Select Provider And Model Per Agent

Agent Administrators can select a Provider and model for `hexa` from the Global Providers Aggregate.

**Consequences (testable):**

- The system validates the complete selection-eligibility set before Agent activation: the Provider and model are enabled in the Global Providers Aggregate and enabled for this tenant (FR-33); the Provider is configured; the model supports text generation; its capability limits are valid; its pricing metadata is valid; its data-handling record is complete and its current `DataHandlingVersion` accepted for this tenant (FR-4); and its `CapabilityVersion` is not regressed. Failing any element blocks activation with a typed reason.
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
- `hexa` is instantiated per tenant: each tenant has its own `hexa` configuration, Party identity, Response Policy, Approver Policy, and consumption bounds (OQ-20).
- Response mode is tenant-wide in V1. It applies to every Conversation in the tenant, and no per-Conversation or per-context override exists; a tenant that wants confirmation for sensitive Conversations runs Confirmation Response Mode for all of them.

#### FR-7: Configure Approver Policy

Agent Administrators can define all Approvers through the Agent's Approver Policy, using the policy sources — Conversation Facilitator, predefined Parties, and tenant roles. The caller source is retired: `ApproverPolicySourceKind.Caller` is on the FR-23 deprecate-and-reject register, because the Eligible Approver predicate excludes the caller of every proposal the source could apply to. V1 resolves the Conversation Facilitator normatively from `ParticipantRole.Facilitator` [ASSUMPTION A-3].

**Consequences (testable):**

- The system authorizes proposal edit, regeneration, approval, rejection, and abandonment actions using the Approver Policy.
- The system rejects approval actions by Parties not authorized by the current policy for the proposal.
- The system exposes which configured policy source authorized the Approver according to a defined disclosure category: user-visible, operator-only, redacted, or omitted.
- The proposal records the policy basis used for each approval-related decision.
- Every configured Approver Policy source declares its disclosure category at configuration time. Absent an explicit choice, the category is operator-only, so a source is never disclosed more widely by omission; a source whose disclosure would reveal membership of a restricted tenant role cannot be configured as user-visible.
- API/client contracts and admin UI use the same disclosure category for the same approval-policy basis.
- No admin UI text, API documentation, or Audit Evidence narrative may state or imply that a distinct Conversation owner was resolved; the disclosed authority is the Conversation Facilitator. The stable wire identifier for this source is `ApproverPolicySourceKind.ConversationOwner`, a legacy name that FR-23 forbids renaming within V1. Admin UI and API documentation both label it Conversation Facilitator, so the parity rule is satisfied by the label, not by the identifier.
- Every Approver must hold current read access to the Source Conversation at the moment of each proposal action. The system re-checks that access at discovery, edit, regeneration, approval, rejection, abandonment, and audit-content inspection, and fails closed when it is absent, stale, or unavailable.
- Approver resolution is Conversation-scoped. A tenant-role source or predefined-Party source contributes only those holders or Parties who are current Participants of the Source Conversation at the moment of the action; a source with no such member for a given Conversation contributes no Approver there, which is a resolution outcome, not a configuration error.
- An Approver who has lost Conversation read access may see that a proposal exists and what state it is in, but no proposal content, context metadata, or generated or edited version. FR-13 states the matching discovery rule.
- Segregation of duties is one predicate, Eligible Approver (§3), applied at four moments:
  - At configuration time, the system rejects a policy that names no Conversation-dependent source (Conversation Facilitator or a tenant role) and names fewer than two predefined Parties, because such a policy could never yield an Eligible Approver once the caller is excluded. A Facilitator-only policy is valid, because the Facilitator is eligible whenever the Facilitator is not the caller.
  - At call time, the system resolves the Eligible Approver set for the proposal before any Provider work and rejects the call with the typed reason `NoEligibleApprover` when it is empty.
  - At edit time, an edit is rejected with a typed reason before it is recorded unless at least one Eligible Approver would remain after it, the editor and the caller both excluded.
  - At approval time, the approving Party must itself be an Eligible Approver for the version being approved, so a caller can never approve their own Agent Call and no Party can approve a version it last edited.
- Because the call-time check precedes Provider work, no proposal is created without an Eligible Approver at creation. A scheduled re-check runs over every awaiting proposal and every `PostingFailed` proposal at a bounded cadence — hourly by default, configurable per environment from 15 minutes through 24 hours, and bounded to one pass at a time [ASSUMPTION A-20] — and distinguishes an authoritative answer from an unavailable one:
  - An authoritative answer is exactly a typed Conversations answer of `ConversationDeleted` or `PrincipalRemovedFromConversation` (§8 seam 5), or a returned roster. A generic denial, timeout, or error is unavailable, so a rotated or misconfigured Agents Service Principal credential can never abandon anything. Every secondary input to the Eligible Approver predicate — the tenant-role projection and the per-Party access check — must report fresh and available, or the pass is unavailable as a whole.
  - On a `ConversationDeleted` or `PrincipalRemovedFromConversation` answer, the re-check moves the proposal to `Abandoned` with the typed system reason `SourceConversationUnavailable`, versions preserved.
  - The re-check moves a proposal to `Abandoned` with reason `NoEligibleApprover` only after two consecutive passes, at least one cadence apart, each of which returns a roster whose resolved Eligible Approver set is empty; the first such pass records the marker `ResolutionEmptyPending` on the proposal, visible under FR-25, so a momentary absence never abandons.
  - On an unavailable pass, the re-check records the non-terminal marker `ResolutionUnavailable`, visible under FR-25, leaves the state and `ExpiresAt` untouched, and tries again at the next pass; a transient outage never abandons a proposal.
  - Every later proposal action applies the same distinction: an unavailable answer fails that action closed with a typed reason and abandons nothing, and an unavailable roster at call time rejects the call with the typed reason `ApproverResolutionUnavailable`, never `NoEligibleApprover`.
  So no proposal is ever stranded, and none is abandoned on absent evidence (FR-18).
- `hexa` is unavailable, with a typed `NoEligibleApprover` rejection, in a Confirmation Response Mode Conversation whose Participants yield no Eligible Approver. That trade-off is accepted deliberately, on the same terms as OQ-10: FR-25 and SM-C4 count these rejections so the unavailability is visible rather than silent.

### 4.4 Explicit Conversation-Originated Invocation

**Description:** Conversation Participants call `hexa` through the Conversation-owned **Call hexa** action. V1 does not activate Agents automatically from Conversation changes and does not expose mention, command, or alternate invocation entry points. This realizes UJ-2 and UJ-3.

**Functional Requirements:**

#### FR-8: Call Agent From Conversation

Authorized Conversation Participants can explicitly call `hexa` from a Source Conversation with a user prompt or request.

**Consequences (testable):**

- Agent Calls require Source Conversation access and Agent call permission.
- Unauthorized calls fail before Provider invocation.
- Every Agent Call records caller, Agent, Source Conversation, request timestamp, and response mode.
- An Agent Call is accepted only after every pre-Provider check passes, in this order:
  1. Caller authorization and Source Conversation access (FR-20, FR-33).
  2. Agent lifecycle (FR-3).
  3. Provider/model eligibility (FR-5).
  4. Rate limits (FR-32).
  5. Conversation Context Policy, which measures the context (FR-9).
  6. Cost reservation of the estimated attempt cost — measured input tokens plus the reserved output allowance at the current pricing version (FR-28).
  7. Pre-Provider Content Safety Policy (FR-27).
  8. Eligible Approver resolution where Confirmation Response Mode applies (FR-7).
  9. Agent membership in the Source Conversation (FR-2).
- A reservation held by a call that fails any later check is released immediately with the reason `NotInvoked` and is never settled.

#### FR-9: Build V1 Conversation Context

The system supplies the Agent with Conversation Context according to the configured Conversation Context Policy.

**Consequences (testable):**

- V1 Agent generation uses Conversation Context only.
- V1 generation does not include long-term memory, project content, folder content, external tool output, or external-channel content.
- The Safe Context Budget is derived per call as the selected model's recorded input limit for the applicable `CapabilityVersion`, minus the reserved output allowance, the Agent Instructions, the caller prompt and any system framing the request adds, and a configured safety margin, counted with the Provider's tokenizer or a named documented approximation. The margin defaults to 10% of the input limit and is configurable from 5% through 25% [ASSUMPTION A-5]. A budget that subtracts only the output allowance does not satisfy this requirement, because a Conversation that fits such a budget can overflow the model once the prompt and framing are appended, after the fail-closed point this requirement promises.
- When the full Source Conversation fits the Safe Context Budget, V1 generation uses the full Source Conversation.
- When the Source Conversation exceeds the Safe Context Budget, the call fails closed before Provider invocation unless the active Conversation Context Policy declares an Approved Bounded Context Behavior that fits, in which case that behavior applies and its reference and bounds are recorded as Audit Evidence. V1 declares no Approved Bounded Context Behavior, so the oversized case always fails closed.
- Silent truncation, summarization, or windowing is prohibited under every policy. A bounded-context mode presented as an input without a matching declared Approved Bounded Context Behavior is a rejected input, not a fallback.
- The computed Safe Context Budget, each subtracted term, the measured context size, the `CapabilityVersion` used, and the resulting context mode are recorded in Audit Evidence for every call, including blocked calls.
- Agent Calls record the resolved context mode — `Full` (the complete Source Conversation), `Bounded` (an Approved Bounded Context Behavior), or `Blocked` with a typed block reason — together with the Conversation Context Policy version or equivalent identifier, and enough context metadata for audit without leaking unrelated tenant data. `Blocked` is an additive public value under FR-23; the `Unknown` sentinel is never a recorded context mode.
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

- Posting re-reads the Conversation Agent State and does not post while the Conversation is `Blocked` or `ExternallyRemoved` (FR-2).
- The posted message references the Agent Call or equivalent trace identifier.
- The posted message does not appear as authored by the caller.
- The system records Audit Evidence linking caller, Agent, Provider/model, Source Conversation, generated content, and posted Conversation Message.
- Every automatically posted Agent Response carries a participant-visible marker identifying it as AI-generated from Conversation Context and not human-verified.

#### FR-12: Prevent Automatic Posting When Policy Fails

The system prevents automatic posting when authorization, Agent lifecycle, the tenant kill switch, the Agents-owned block (FR-2), Provider/model, Party identity, Source Conversation access, Conversation Context Policy, Content Safety Policy, or generation status is invalid.

**Consequences (testable):**

- No Conversation Message is created when a required policy check fails.
- No Conversation Message is created when generated content fails the active Content Safety Policy.
- The failure reason is visible through authorized status surfaces without leaking secrets or unrelated tenant data.
- Audit Evidence distinguishes authorization, context policy, content safety, cost-cap, Provider/runtime, and posting failures.
- A Provider attempt whose outcome cannot be determined resolves to a typed `Indeterminate` terminal outcome — an additive public value distinct from the `Unknown = 0` sentinel that FR-23 reserves for unset values — rather than an implicit success or failure, and is never silently retried. Mapping an indeterminate outcome to an adapter or generation error does not satisfy this requirement.

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
- Proposal discovery returns full content only for proposals whose Source Conversation the requesting Approver can currently read. For a proposal on which the requester was previously resolved as an Approver but has since lost read access, discovery returns existence and state only, so the queue and pending count stay consistent with FR-7. Proposals on which the requester was never resolved as an Approver are not listed, counted, or otherwise disclosed.

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
- A configured per-proposal regeneration ceiling bounds how many regenerations a proposal may accumulate; exceeding it is a typed rejection. The ceiling defaults to 3 and is configurable per Agent from 1 through 10 [ASSUMPTION A-6].
- A regeneration is a chargeable Agent Call for cost-cap, rate-limit, and audit purposes. It re-runs the FR-9 Safe Context Budget check and the FR-27 pre-Provider scan on the same terms as the original call, against the Conversation as it stands at regeneration time.

#### FR-17: Approve Proposed Reply

Authorized Approvers can approve a selected proposal version, causing it to be posted to the Source Conversation as `hexa`.

**Consequences (testable):**

- Approval posts exactly the approved version and no other proposal version.
- The Conversation Message is attributed to the Agent's Party identity.
- Audit Evidence links the approved version, Approver, approval timestamp, and posted Conversation Message.
- The approved version passes the then-current Content Safety Policy at approval time before posting, whether it was generated or human-edited (FR-27).
- The posted Conversation Message's trace records whether the posted version was human-edited and by which Party, and that fact is exposed wherever the message's provenance is disclosed.

#### FR-18: Resolve Proposed Reply Through Its Lifecycle States

A Proposed Agent Reply occupies exactly one of the states of the public `ProposedAgentReplyState` contract. Three states denote a proposal awaiting an Approver decision and differ only in what the latest version is: `Pending` (only generated versions exist), `Edited` (the latest version is human-edited), and `Regenerated` (the latest version was regenerated). Three states follow a decision to post: `Approved` (a version is approved and posting has not started), `PostingPending` (posting is in progress), and `PostingFailed` (posting failed; bounded retry available). Four states are terminal: `Posted` (the approved version is confirmed in the Conversation), `Rejected` (an Approver declined the proposal), `Abandoned` (an Approver or system policy withdrew it), and `Expired` (its `ExpiresAt` passed while awaiting a decision). `Unknown` is the FR-23 sentinel and is never a recorded state. A proposal is moved between states by authorized Approvers or by system policy, and every surface uses this one terminal set.

**Consequences (testable):**

- Terminal proposals cannot be edited, regenerated, approved, or posted.
- The allowed transitions are exactly the rows below; any other (from, to) pair is rejected with a typed reason. Edit and regeneration are rejected from `Approved`, `PostingPending`, and `PostingFailed` as well as from every terminal state, so a version can never change after the decision to post it. `PostingPending` is uninterruptible: nothing leaves it except the posting attempt's own conclusion — an acknowledgement, a typed failure, or an attempt timeout no shorter than the seam's posting timeout — so no removal, block, suspension, or abandonment can act on a post in flight; each is applied on exit if the attempt did not post.

| From | To | Actor | Guard |
| --- | --- | --- | --- |
| `Pending`, `Edited`, `Regenerated` | `Edited` | Eligible Approver | An Eligible Approver would remain after the edit (FR-7); Agent `Active` and tenant not `Suspended` |
| `Pending`, `Edited`, `Regenerated` | `Regenerated` | Eligible Approver | Regeneration ceiling not reached (FR-16); Agent `Active` and tenant not `Suspended` |
| `Pending`, `Edited`, `Regenerated` | `Approved` | Eligible Approver for that version | Version passes the then-current Content Safety Policy (FR-27); Agent `Active` and tenant not `Suspended` |
| `Pending`, `Edited`, `Regenerated` | `Rejected` | Eligible Approver | None beyond eligibility; permitted while `Disabled` or `Suspended` |
| `Pending`, `Edited`, `Regenerated` | `Abandoned` | Eligible Approver; system with a typed reason | Human abandonment permitted while `Disabled` or `Suspended`; system abandonment only on an authoritative answer (FR-2, FR-7) |
| `Pending`, `Edited`, `Regenerated` | `Expired` | System | Stored `ExpiresAt` passed; enforced on every read and command; recorded with reason `ExpiredWhileSuspended` when the awaiting window overlapped a `Suspended` period |
| `Approved` | `PostingPending` | System | Pre-post re-validation passed; not evaluated while the tenant is `Suspended` or the Agent `Disabled` — the proposal waits in `Approved`, consuming no retry attempt |
| `Approved` | `PostingFailed` | System | Pre-post re-validation failed for a reason that is not a removal or block |
| `PostingPending` | `PostingFailed` | System | The posting attempt concluded with a typed failure or timed out |
| `PostingPending` | `Posted` | System | The posting attempt was acknowledged |
| `Approved`, `PostingFailed` | `Abandoned` (`RemovedInConversations`) | System | Removal or block detected (FR-2), and the `MessageId` lookup finds no posted message |
| `PostingFailed` | `Abandoned` (`SourceConversationUnavailable`) | System | Authoritative `ConversationDeleted` or `PrincipalRemovedFromConversation` answer (FR-7); the `MessageId` lookup finds no posted message |
| `PostingFailed` | `Abandoned` | Eligible Approver; Tenant Agent Administrator (FR-33, only when the Source Conversation is gone or inaccessible) | The `MessageId` lookup finds no posted message; permitted while `Disabled` or `Suspended` |
| `PostingFailed` | `Posted` | System | The `MessageId` lookup finds the message (`LateConfirmed`) |
| `PostingFailed` | `PostingPending` | System (automatic retry within budget); Tenant Agent Administrator (audited administrative retry) | Failure reason is not a safety verdict; tenant not `Suspended` and Agent `Active`; the `MessageId` lookup finds no posted message; full pre-post re-validation re-run |

- A proposal abandoned by the system carries its typed reason (`NoEligibleApprover`, `SourceConversationUnavailable`, or `RemovedInConversations`) in Audit Evidence and in status, is excluded from the SM-3 denominator unless it followed a human decision from `Approved` or `PostingFailed` (§12), and is reported separately under FR-25 and SM-C4. The kill switch (FR-28) is never a system-abandonment reason: while it is pulled, proposals awaiting a decision remain for humans to reject or abandon.
- Terminal proposals preserve all generated and edited versions for audit.
- The default expiry duration is 24 hours. An Agent Administrator may configure a duration from 1 hour through 30 days for future proposals only.
- `ExpiresAt` applies only while a proposal is awaiting a decision. A proposal whose stored `ExpiresAt` has passed while `Pending`, `Edited`, or `Regenerated` is treated as `Expired` on every read and every command, regardless of timer delivery; an expiry policy change never changes an existing proposal's `ExpiresAt`. Approval freezes expiry: `Approved`, `PostingPending`, and `PostingFailed` proposals never expire.
- `Approved`, `PostingPending`, `PostingFailed`, and `Posted` are distinct states. Approval does not imply posting; a failed post moves the proposal to `PostingFailed` with a typed reason and a bounded audited retry of at most 3 attempts over 15 minutes [ASSUMPTION A-7], and only a confirmed post reaches `Posted`. Every retry, automatic or administrative, re-runs the full pre-post re-validation below; a `PostingFailed` proposal whose reason is a safety verdict is not retryable and accepts abandon only. When the retry budget is exhausted, the proposal remains `PostingFailed`, visible in status under FR-25, until one of the following occurs: an Eligible Approver abandons it; the Tenant Agent Administrator abandons it under the audited FR-33 row that needs no Conversation read access; an audited administrative retry succeeds; or the FR-7 re-check, which covers `PostingFailed` proposals, abandons it on an authoritative `ConversationDeleted` or `PrincipalRemovedFromConversation` answer. Two further exits come from the table: the `MessageId` lookup finds the post (`Posted`, `LateConfirmed`), or a removal or block is detected (`Abandoned`, `RemovedInConversations`). A `PostingFailed` proposal therefore always has an exit and always reaches the §9 retention clock.
- Posting is at-least-once under an Agents-supplied `MessageId` and idempotency key (§8 seam 2), so a lost acknowledgement can leave a message in the Conversation that Agents has not confirmed. Before every retry attempt and before any exit from `PostingFailed` — including abandonment by any actor — the system looks the message up by its `MessageId` through the seam-2 existence read. The lookup is skipped for a `PostingFailed` proposal whose failure was a pre-post re-validation that never attempted a post. If the message is present, the proposal moves to `Posted` with the `LateConfirmed` flag recorded in Audit Evidence and status, and the requested retry or abandon is rejected with a typed reason; a proposal whose approved version is in the Conversation is never recorded `Abandoned`. The lookup's answers are classified as FR-7 classifies reads: a message, a typed absence, a typed `ConversationDeleted` or `PrincipalRemovedFromConversation` answer, or unavailable. For an abandon exit, a typed absence and a typed deleted or removed answer both count as no posted message — a message that may exist in a Conversation Agents can no longer read is governed by Conversations retention. For a retry exit, only a typed absence permits the retry. If the lookup is unavailable, the exit is refused with a typed reason and the proposal stays `PostingFailed`.
- While the tenant kill switch is pulled (FR-28), the `PostingFailed` retry-window clock is paused and automatic and administrative retries are suspended; `ExpiresAt` continues to run for awaiting proposals, so a proposal that was awaiting a decision when the switch was pulled is still decidable after release only if it has not expired.
- Before posting, the system re-validates that the Source Conversation still exists and is accessible to the Agents Service Principal, that the Agent is `Active`, its tenant not `Suspended`, and its Party identity valid (FR-2, FR-3, FR-28), that `hexa` is still a member of and not blocked in that Conversation, and that the approved version still passes the then-current Content Safety Policy. A failure moves the proposal to `PostingFailed` rather than posting stale content, except that a detected removal or Agents-owned block moves it to `Abandoned` with reason `RemovedInConversations` under FR-2, not to `PostingFailed`.
- Expiry behavior and the stored `ExpiresAt` are visible through admin UI and API/client contracts.

### 4.7 Authorization, Tenant Isolation, And Governance

**Description:** Hexalith Agents must preserve Hexalith tenant isolation, Party identity boundaries, and fail-closed authorization. Agent configuration, calls, proposals, approval actions, posting, and audit inspection all require explicit permission. FR-33 defines the six roles every rule in this PRD resolves to. This realizes all journeys.

**Functional Requirements:**

#### FR-33: Define Authorization Roles And Scopes

Every authorization rule in this PRD resolves to one of six Agents roles at a stated scope: Platform Operator, Tenant Agent Administrator, Approver, Conversation Participant, Compliance Inspector, and Release Operator. The Conversation Facilitator is a Conversations role that the block and clear rows use as an authority, and Security approval is a condition on one row, not a role. Rows carrying an `[ASSUMPTION A-n]` key were inferred by the 2026-09-09 reconciliation and are confirmed or corrected by Product before the first tenant is enabled (§8.1).

| Operation | Who may perform it | Scope |
| --- | --- | --- |
| Administer the Global Providers Aggregate: Providers, models, pricing, capability limits, secret references | Platform Operator | Platform |
| Enable a Provider/model for a tenant, which permits selection (FR-4) | Platform Operator | Tenant. A tenant sees and can select only Providers/models enabled for it; secret references and configured state are visible only to the Platform Operator. `[ASSUMPTION A-10]` |
| Accept a Provider/model's named `DataHandlingVersion` for the tenant, before first activation and on each change (FR-4) | Tenant Agent Administrator | Tenant `[ASSUMPTION A-10]` |
| Provision `hexa` at tenant enablement: create-only, once per tenant, establishing Agent identity and tenant scope (FR-1) | Platform Operator | Tenant |
| Configure `hexa`: display name and description, Agent Instructions, Provider/model selection among tenant-enabled options, Response Policy, Approver Policy, Conversation Context Policy, lifecycle, proposal expiry duration, regeneration ceiling | Tenant Agent Administrator | Tenant |
| Call `hexa` | Every Conversation Participant of a Conversation in a tenant where `hexa` is active, unless the Tenant Agent Administrator restricts calling to a tenant role. The restriction, when set, is the Agent call permission that FR-8 and FR-20 enforce. `[ASSUMPTION A-11]` | Tenant |
| Edit, regenerate, approve, reject, or abandon a proposal | Approvers resolved under FR-7 for that proposal | Proposal |
| Block `hexa` in a Conversation (FR-2) | Tenant Agent Administrator or Conversation Facilitator `[ASSUMPTION A-9]` | Conversation |
| Clear a block on `hexa` in a Conversation, which records `ReadmitPending` (FR-2) | The authority that set the block, evaluated at clear time, or the Tenant Agent Administrator; a Conversation Facilitator cannot clear an Administrator's block. An `ExternallyRemoved` record is cleared by the Tenant Agent Administrator or any current Conversation Facilitator `[ASSUMPTION A-9]` | Conversation |
| Configure cost caps and rate limits (FR-32) | Platform Operator or Release Operator sets them; the Tenant Agent Administrator may lower a cap or limit for their own tenant and never raise it, so the boundary is not self-set | Tenant |
| Override a reached cost cap (FR-32) | Platform Operator only; never the Tenant Agent Administrator | Tenant |
| Publish or version the Content Safety Policy (FR-26) | Platform Operator with Security approval; a tenant may only add restrictions through a stricter mode-specific policy | Platform |
| Record cost-control posture and launch readiness; record the `RQ-1` READY or NOT READY decision in the launch readiness register; enable production-like generation and production for a tenant (FR-28) | Release Operator; Platform-scoped gate observations are recorded by the Platform Operator, and the `RQ-1` decision remains the Release Operator's | Tenant |
| Confirm an SM-4 cross-tenant or unauthorized event (FR-28) | Platform Operator, recorded | Platform |
| Convene and record the kill-switch trigger review (FR-28) | Release Operator, within one business day of the trigger condition | Tenant |
| Pull the per-tenant kill switch (FR-28) | Platform Operator immediately on a confirmed SM-4 event; Release Operator as the recorded decision of a trigger review | Tenant |
| Release the per-tenant kill switch (FR-28) | Platform Operator or Release Operator, with audited justification | Tenant |
| Bind the production payload-protection engine in the host composition (FR-34) | Platform Operator through `EXT-HOST-1`; never a tenant role | Platform |
| Inspect the provenance of posted Agent Responses (FR-24) | A current Participant with read access to the Source Conversation | Conversation |
| Inspect unposted proposal versions, rejected content, and context metadata (FR-24) | A Party resolved as an Eligible Approver for that proposal, or a Compliance Inspector under a scoped, justified inspection approved — in advance, or post hoc within the FR-24 window for a single-proposal or single-Conversation scope — by a second party outside the computed subject set (FR-24); the Platform Operator approves any inspection wider than one Conversation | Proposal or case |
| Abandon a `PostingFailed` proposal whose Source Conversation is gone or inaccessible (FR-18) | Tenant Agent Administrator; audited; no Conversation read access required | Tenant |
| Retry a `PostingFailed` proposal administratively after its retry budget is exhausted (FR-18) | Tenant Agent Administrator; audited | Tenant |
| Inspect operational status and failed-call evidence (FR-10, FR-25) | Tenant Agent Administrator, Release Operator, and Platform Operator for the tenant; a caller sees the outcome of their own Agent Calls only | Tenant |
| Apply legal hold (FR-30) | Compliance Inspector | Tenant |
| Release legal hold (FR-30) | Compliance Inspector, with the audited approval of a second Compliance Inspector or the Platform Operator | Tenant |
| Request authorized export (FR-30) | Compliance Inspector, with prior approval by a second party on the FR-24 terms; never post hoc | Tenant |
| Request approved deletion (FR-30) | Platform Operator with Compliance Inspector approval `[ASSUMPTION A-12]` | Tenant |

**Consequences (testable):**

- Admin UI and API/client contracts apply the same matrix, and an operation not granted to the acting role fails closed with a typed denial before any side effect.
- Every role except Platform Operator is tenant-scoped; a role assignment in one tenant grants nothing in another (FR-19).
- A missing, stale, or unavailable role assignment fails closed rather than defaulting to the most permissive row.
- Every authorized operation records the role basis on which it was permitted, at the disclosure level FR-20 allows.

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
- Posting an approved Agent Response checks Conversation access for the named posting principal — the Agent's Party identity acting through the Agents Service Principal — not for the original caller or the Approver.
- `Committed` unblocks `ready-for-dev` only. No runtime, test, or qualification path may execute a consumed external seam until its register entry is `Available` (§8) and its compatibility command passes against the exact target used by that execution; a path that would execute a seam whose entry is not `Available` fails closed with the typed outcome `DependencyNotAvailable` naming the entry. The register's compatibility command is the one path permitted to execute a `Committed` seam, under the register owner's control, to establish `Available`.
- Content-bearing runtime paths — proposal versions, generated content, Conversation Context, and the content fields of Audit Evidence — fail closed with the typed outcome `PayloadProtectionUnavailable` while `EXT-PROTECTION-1` is not `Available` (FR-34).

### 4.8 Admin UI And API/Client Contracts

**Description:** V1 includes both an admin web UI and public API/client contracts. These surfaces must expose the same governed capability without leaking internal implementation mechanics. This realizes UJ-1 and UJ-4.

**Functional Requirements:**

#### FR-22: Provide Admin UI

The admin UI allows the Platform Operator to manage Global Providers Aggregate entries and tenant enablement, and allows Tenant Agent Administrators to configure `hexa`, inspect lifecycle state, configure Response Policy and Approver Policy, and view Agent operation and proposal status, each within the FR-33 matrix.

**Consequences (testable):**

- Admin UI actions enforce the same authorization rules as API/client contracts.
- Admin UI never exposes Provider secrets.
- Admin UI distinguishes, with distinct labels, the `Draft`, `Active`, and `Disabled` lifecycle states and the `Suspended` tenant status (FR-3), invalid configuration (a readiness classification, not a lifecycle state), and the awaiting-decision, posting-failed, failed-call, and expired proposal states.
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
- The V1 deprecate-and-reject register is: `CostControlPosture.ReportingOnlyMonitoring` and `CostControlPosture.AcceptedLaunchRisk` (FR-28, OQ-6), `ContentSafetyFailureHandling.BlockWithAuditableOverride` (FR-27; Approvers cannot override a safety failure), and `ApproverPolicySourceKind.Caller` (FR-7; the caller is never an Eligible Approver of their own call). Each remains declared and deserializable, is rejected server-side with a typed rejection wherever it is presented, and is listed here so that no consumer treats it as accepted behavior.
- `[Flags]` enums use `None = 0` and are exempt from the `Unknown = 0` rule; the rule applies to every other public enum. A public enum whose zero value currently carries meaning, such as `AgentSetupWriteStatus.Submitted` (FR-29), is non-conformant and is corrected additively before the first tenant is enabled.
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
- Audit Evidence is queryable by authorized users without exposing unrelated tenant data or Provider secrets. Inspection of Conversation-derived content follows these paths:
  - Participant-based inspection is limited to the provenance of posted Conversation Messages: a current Participant with read access may inspect who called, what was posted, and whether it was human-edited.
  - Unposted proposal versions, rejected content, and context metadata are inspectable only by a Party that was resolved as an Eligible Approver for that proposal or under compliance inspection, matching the FR-13 disclosure rule.
  - Compliance inspection is scoped to a named Conversation or case identifier, requires a recorded justification, and requires the approval of a second party outside the case's subject set. The subject set is computed, not declared: every Party recorded as caller, editor, Approver, decision actor, or Conversation Facilitator on any proposal or Conversation in scope, plus the Tenant Agent Administrator whose configuration version was in force for any call in scope. The second party is the Tenant Agent Administrator when outside that set, and otherwise the Platform Operator or a second Compliance Inspector; a second party inside the set, or the Inspector, is a typed rejection. Two Compliance Inspectors cannot approve each other's inspections within a rolling 30-day window, and any inspection wider than one Conversation requires Platform Operator approval. Post-hoc review by such a second party is permitted only for an inspection scoped to a single proposal or a single Conversation, and must be completed within 7 days of the access [ASSUMPTION A-19]; an inspection not reviewed within that window is recorded as unreviewed on the audit surface. The inspection rate and every unreviewed inspection are visible on an audit surface the Tenant Agent Administrator and the Platform Operator can read, and each inspection is itself recorded as Audit Evidence.
  Inspection fails closed outside these paths.
- When the Source Conversation no longer exists or is inaccessible, participant-based access is unavailable and the evidence remains inspectable under compliance inspection only. Retained evidence is never made uninspectable by the loss of its Conversation.
- Audit Evidence records the computed Safe Context Budget, measured context size, `CapabilityVersion`, and resolved context mode for every call.
- Audit Evidence records the editing Party for every edited version and the approving Party for every approval, so a human-edited posted message is always distinguishable from generated output.
- Every audited administrative override, including a cost-cap override, records actor, scope, justification, and expiry.
- Audit Evidence records the Provider data-handling record version in force for each Provider attempt (FR-4).

#### FR-25: Expose Operational Status

The system exposes status for Agent readiness, Provider/model readiness, recent Agent Call outcomes, proposal queues, generation failures, approval completion, and posting outcomes.

**Consequences (testable):**

- Authorized administrators can identify whether `hexa` is callable for a tenant.
- Authorized administrators can distinguish configuration errors, authorization failures, context policy failures, content safety failures, Provider failures, generation failures, pending approvals, and posting failures.
- Status surfaces support launch monitoring of adoption and approval workflow metrics.
- Status exposes the per-tenant count of Agent Calls blocked, by each SM-C4 blocked-call reason, together with the count of system-abandoned proposals by reason, so that structural unavailability is visible rather than silent.
- Status exposes, per Conversation, the Conversation Agent State and any `MirrorPending` flag (FR-2), and, per proposal, any `ResolutionUnavailable` or `ResolutionEmptyPending` marker (FR-7) and `LateConfirmed` flag (FR-18).
- Status exposes posting failures by typed reason, including `MembershipUnavailable`, `MembershipRejected`, and safety verdicts (FR-2, FR-18).
- Status exposes the per-tenant count of unavailable calls by reason (`MembershipUnavailable`, `ApproverResolutionUnavailable`, `ContextUnavailable`), and separately the counts of authorization denials, capacity queueing or rejection, `DependencyNotAvailable`, and `PayloadProtectionUnavailable` outcomes, which are neither blocked nor failed calls for SM-C4.
- Status reports `Suspended` for a tenant whose kill switch is pulled, distinct from a `Disabled` Agent (FR-3).
- Status exposes each tenant's cost-cap consumption, including the 80% warning and 100% fail-closed condition, and distinguishes reserved from settled spend.

#### FR-30: Provide Governance Operations

The governance rules in §9 are callable operations on the public surface, not documentation only.

**Consequences (testable):**

- Authorized operators can administer budget policy, publish and version Content Safety Policy, apply and release legal hold, request authorized export, and request approved deletion.
- Authorized operators can inspect launch-readiness state and its blockers, including `PayloadProtectionUnavailable` and `ProhibitedCostControlPosture` (FR-28, FR-34); the surface is the runtime record of each gate's status, and `RQ-1` itself is recorded in the launch readiness register. The surface also records `GateOutOfScope` (FR-28) and `TriggerReviewOverdue` (FR-28) conditions.
- Every governance operation enforces tenant scope and Party authorization, is audited with actor and justification, and fails closed when authorization or required state is missing. Export requires prior second-party approval and hold release a separate approver (FR-33).
- Legal hold, export, deletion, and the `Erased` replay semantics depend on the payload-protection engine (`EXT-PROTECTION-1`, §8); a governance operation that would rely on the no-op default fails closed with `PayloadProtectionUnavailable` (FR-34).
- Governance operations are covered by the FR-23 compatibility rules on the same terms as every other public contract.

### 4.10 Content Safety And Launch Readiness

**Description:** V1 generation must be gated by explicit safety, context, cost, performance, and metric decisions before production or production-like launch validation. This realizes UJ-1, UJ-2, UJ-3, and UJ-4.

**Functional Requirements:**

#### FR-26: Configure Content Safety And Prompt Policy

The Platform Operator, with Security approval, defines the active Content Safety Policy for `hexa` (FR-33); a Tenant Agent Administrator may only add restrictions through a stricter mode-specific policy.

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
- The pre-Provider scan covers the caller prompt and Conversation content not yet scanned for that Conversation, reusing a cached per-Conversation verdict for already-scanned history so the scan cost does not grow without bound. The cache is keyed by Conversation, an Agents-computed keyed content hash of each message taken on every load — an HMAC under a per-tenant secret custodied through `EXT-SECRETS-1`, so the retained hash is a cache key and not an oracle for guessed plaintext — and Content Safety Policy version. Publishing a policy version invalidates every cached verdict tenant-wide, and an edited or deleted message no longer matches its hash, so no Conversations notification is needed. A cached verdict is never reused under a policy version other than the one that produced it, and Audit Evidence records the policy version each reused verdict came from.
- A Conversation whose scanned history fails the active policy stays blocked for generation until that history has been re-evaluated under a policy it passes; V1 provides no Agents-side redaction or exclusion path for individual historical messages; where Conversations permits it, a Participant may edit or delete the message there and the next load re-scans the current history (OQ-18).
- The content safety scan is budgeted separately and is excluded from the pre-Provider rejection gate in NFR-9; that gate covers authorization, policy, budget, and context rejections that do not require a classifier call.

#### FR-28: Define Launch Readiness Controls

V1 launch readiness requires the fixed metric thresholds, latency targets, full-context behavior, cost controls, audit governance, NFR-11 through NFR-14, external dependency availability, and the normative evidence authority in §11. The `RQ-1` input list below is the single definition of what the gate evaluates; §3, §11, and OQ-22 cite it and do not restate it.

**Consequences (testable):**

- Controlled production-like qualification may collect live evidence only after Content Safety Policy, Conversation Context Policy, cost controls, and audit governance are active and recorded, and only through seams whose register entries are `Available` (FR-21). Qualification access does not authorize production enablement.
- Production enablement remains blocked until `RQ-1` records READY over this complete input list, and nothing outside it is an `RQ-1` input:
  1. Live Evidence Levels 4 and 5 for every area §11 names.
  2. The pre-enablement gate metrics SM-1, SM-4, SM-5, and SM-6 attained on the qualification cohort, with 100% Audit Evidence completeness.
  3. The NFR-9 and NFR-14 latency gates, each over at least 30 production-like executions.
  4. The NFR-11 production-like recovery exercise, completed within its 15-minute bound with every terminal decision preserved.
  5. The NFR-12 numeric concurrency, queue-depth, and backpressure limits recorded for the environment.
  6. Every external dependency in the qualification profile `Available`, with its compatibility command passing against the exact target the run used; otherwise the typed blocker `DependencyNotAvailable` naming the entry — for `EXT-PROTECTION-1`, alongside the FR-34 blocker `PayloadProtectionUnavailable`. The qualification profile is the full §8 register scope; a dependency may be excluded only by a recorded Release PM decision naming the FR consequences the run will not exercise, and that exclusion is itself an `RQ-1` blocker until Product accepts it.
  7. An active Content Safety Policy (FR-26) and Conversation Context Policy (FR-9), and a recorded cost-control posture of `Quotas`, `Budgets`, or `ProviderModelLimits`; otherwise `ProhibitedCostControlPosture`.
  8. Per-tenant caps, rate limits, and the per-Party concurrent-interaction bound configured under FR-32.
  9. No unretired assumption: every §8.1 `A-n` row and every `ARCH-A-n` row in the Architecture Spine table is retired, whatever its owner; otherwise `UnretiredAssumption` naming the row and its table. An `ARCH-A` row added after the `RQ-1` evaluation is scheduled blocks only once the Release PM records an acknowledgement of it, and every Architecture-owned row carries a target retirement date.
  10. No open decision: every *Deferred* §13 row whose status says "before enablement" has landed; otherwise `OpenDecision` naming the row. OQ-23 blocks the enablement of an Automatic Response Mode tenant specifically, not `RQ-1` as a whole.
- `UnretiredAssumption`, `DependencyNotAvailable`, `OpenDecision`, and `InsufficientEvidence` are launch readiness register vocabulary: `RQ-1` is evaluated and recorded in that register by the Release Operator, not in the Agent aggregate. `PayloadProtectionUnavailable` and `ProhibitedCostControlPosture` are also additive `AgentLaunchReadinessBlocker` values on the FR-30 readiness surface, because the runtime must refuse enablement on them without consulting the register. A launch readiness gate whose evidence requirement is not derivable from this input list is void for `RQ-1` purposes until reconciled, and the Release Operator records the discrepancy as `GateOutOfScope` on the FR-30 surface. READY cannot be recorded while the authorization model or a launch threshold is still hypothetical.
- SM-2, SM-3, and SM-7 are launch-health metrics. Only real usage can produce them, so they are not `RQ-1` inputs; they are reviewed at 30 and 60 days after enablement and monthly thereafter (§12), and a miss triggers the launch-health review and, where the trigger conditions below are met, the kill switch (OQ-22).
- Per-tenant monthly — the UTC calendar month of the reserving command — and per-call cost caps are hard enforcement boundaries: 80% emits a warning, 100% fails closed, and atomic reservation plus reconciliation prevents concurrent overspend. The system reserves the estimated attempt cost — measured input tokens plus the reserved output allowance, priced at the current pricing version — after the Conversation Context Policy has measured the context and before Provider invocation, and reconciles actual usage. A call that fails any pre-Provider check after reserving releases the reservation immediately with reason `NotInvoked`; such a reservation is never held or settled. A reservation is released when the Provider outcome confirms no usage occurred; a reservation whose outcome is `Indeterminate` is held for a bounded period — default 24 hours, configurable from 1 through 72 hours [ASSUMPTION A-8] — and then conservatively settled at the estimated maximum, subject to the audited override in FR-32. Only an attempt with a confirmed no-usage outcome is eligible for retry under the same reservation; an `Indeterminate` attempt is never retried. A reservation past its hold deadline with no authoritative outcome becomes `Unreconciled`: it stays counted against the cap and is settled only by an audited operator command or at period close at the estimated maximum (Spine `ARCH-A-6`, owned by Architecture + Product). Period close occurs at the end of the reserving month plus the maximum configured hold; a reservation still held at that instant is settled at the estimated maximum against the reserving month, and no reservation migrates between months. Missing pricing or budget state blocks invocation; reporting-only monitoring is insufficient.
- The latency gates in NFR-9 are met, each over at least 30 production-like executions. NFR-9 states the thresholds; they are not restated here.
- Production readiness requires live Evidence Levels 4 and 5 as defined in §11; lower levels, skips, placeholders, or conditional results cannot independently establish launch readiness.
- Only the hard-enforcement postures `Quotas`, `Budgets`, and `ProviderModelLimits` satisfy the gate. `ReportingOnlyMonitoring` and `AcceptedLaunchRisk` are on the FR-23 register: rejected at readiness recording with a typed rejection, and surfaced as the additive `ProhibitedCostControlPosture` blocker wherever a previously recorded posture still carries them (OQ-6). Rejecting only an absent posture does not satisfy this requirement.
- Per-tenant cost caps must be configured under FR-32 before production-like generation is enabled; an unconfigured cap is a readiness blocker, not a default.
- The launch readiness register's gate records are the readiness authority; the external dependency register is the commitment authority for `EXT-*` entries. The `LaunchReadinessGate` records that FR-30 exposes are the runtime record of each gate's current status and blockers. An assumption or decision is retired for `RQ-1` purposes by a recorded readiness observation naming the row, made in the same change that edits the keyed text (§8.1); the runtime never parses this document. Any per-Agent readiness record is transitional and must reconcile to the register before `RQ-1` is evaluated.
- Production enablement carries a per-tenant kill switch that the Platform Operator or Release Operator pulls and releases (FR-33). Pulling it disables Agent Calls tenant-wide. Proposals awaiting a decision can be rejected or abandoned but not approved, edited, or regenerated. `Approved` proposals are held in `Approved` without starting a post and without consuming a retry attempt, and a `PostingPending` attempt already in flight completes or fails on its own terms. The `PostingFailed` retry-window clock is paused and automatic and administrative retries are suspended. `ExpiresAt` keeps running — a proposal that expires during the suspension is recorded `ExpiredWhileSuspended` and leaves the SM-3 and SM-C5 denominators. The switch never system-abandons anything and deletes no proposal or Audit Evidence (FR-18). FR-25 reports the tenant as `Suspended` while it is pulled (FR-3). Release requires the same roles and an audited justification. Its trigger conditions are:
  - Any cross-tenant or unauthorized action (SM-4) that the Platform Operator confirms and records: the Platform Operator pulls it immediately.
  - A trigger review is mandatory when, over a rolling seven-day window, any of these rates exceeds its threshold: the blocked-call share (SM-C4) above 50%, the unavailable-call share (FR-25) above 20%, the Provider-or-generation failure share above 20%, or the posting-failure rate above 10%. The Release Operator convenes and records the review within one business day of the condition; a review not convened in that time is recorded as `TriggerReviewOverdue` on the FR-30 surface. The blocked-call and unavailable-call shares count distinct (Party, Conversation, reason) tuples over the distinct (Party, Conversation) tuples that attempted a call in the window, excluding only rejections at a rate limit set by the Platform Operator or Release Operator — a rejection at a limit the Tenant Agent Administrator lowered counts. The posting-failure rate is the share of proposals that entered `PostingFailed` at least once in the window over proposals that entered `Approved` in it, and automatic posts count as one approved and one failed on the same terms. The minimum sample is 5 distinct calling Parties and 50 Agent Calls; for a tenant with fewer than 5 calling Parties it is every Party that called and 20 Agent Calls. Below the minimum sample the trigger evaluates to `InsufficientEvidence` and is reported, not acted on. The Release Operator pulls the switch only as the recorded decision of that review `[ASSUMPTION A-17]`.
  - A miss of SM-3 or SM-7 at two consecutive launch-health reviews: Product records a disable-or-continue decision `[ASSUMPTION A-17]`.
  Launch-health reviews run at 30 and 60 days after enablement and monthly thereafter.
- When a pre-enablement gate metric evaluates to `InsufficientEvidence`, `RQ-1` records NOT READY rather than READY or an undecidable result, and names the metric and the missing evidence. A launch-health metric that evaluates to `InsufficientEvidence` after enablement is reported as such in the launch-health review and never silently passes.

#### FR-32: Configure Tenant Cost Caps And Consumption Bounds

The Platform Operator or Release Operator configures the per-tenant monthly and per-call cost caps and the per-Party and per-Conversation rate limits that FR-28 and NFR-10 enforce; the Tenant Agent Administrator may lower any of them for their own tenant but never raise them, and separately configures the per-proposal regeneration ceiling (FR-33).

**Consequences (testable):**

- Caps are configured per tenant with no implicit default; an unconfigured cap blocks production-like generation rather than resolving to an unlimited or sample value.
- Cap configuration records actor, timestamp, prior and new values, and is auditable.
- Cap changes apply to future Agent Calls only and never retroactively release or re-reserve settled spend.
- Admin UI and API/client contracts expose current caps, consumption, the 80% warning state, the 100% fail-closed state, and reserved-versus-settled spend.
- Rate limits are configured per tenant as a maximum number of Agent Calls per Party and per Conversation over a stated rolling window, with no implicit default; an unconfigured rate limit blocks production-like generation on the same terms as an unconfigured cap.
- A per-Party bound on concurrent non-terminal Agent Calls is configured per tenant on the same terms as a rate limit — no implicit default; the Platform Operator or Release Operator sets it; the Tenant Agent Administrator may only lower it — and is enforced at FR-8 step 4 with a typed rejection.
- The regeneration ceiling is configured per Agent as a maximum number of regenerations per proposal, with a default of 3 and a valid range of 1 through 10 (FR-16).
- Admin UI and API/client contracts expose the configured rate limits and regeneration ceiling, current consumption against them, and the reason when a call or regeneration is refused for exceeding one.
- An audited administrative override can restore Agent Calls for a tenant that has reached its monthly cap; the override records actor, justification, scope, and expiry, and is itself bounded.

#### FR-34: Gate Content-Bearing Execution On Payload Protection Availability

Live content-bearing execution must remain disabled while the `EXT-PROTECTION-1` register entry is not `Available`. The EventStore default protection service is a no-op, Agents registers no protector of its own, and the host composition (`EXT-HOST-1`) must bind a production engine before any content-bearing path runs or any production-like evidence is recorded. Ownership: the EventStore Maintainer delivers the engine (`EXT-PROTECTION-1`); the Platform Operator binds it in the host (`EXT-HOST-1`, FR-33); the Release Operator owns the readiness record and cannot clear the blocker by recording; the Agents Runtime Maintainer owns the blocker, the self-test, and the fail-closed paths.

**Consequences (testable):**

- Launch readiness emits the additive blocker `PayloadProtectionUnavailable` — a public `AgentLaunchReadinessBlocker` value under FR-23 — until a protection attestation passes, and production-like generation cannot be enabled while that blocker stands. The attestation runs at startup and on every readiness evaluation: Agents seals a canary payload, verifies that the persisted bytes contain no plaintext, unseals it, destroys its DEK, and verifies that the field replays as `Erased`; it passes only when every step succeeds and the engine reports an identity and version equal to the register's committed `EXT-PROTECTION-1` target. The attestation result, engine identity, and version are recorded as Audit Evidence on the readiness record. A readiness policy that lacks the blocker, that clears it on a host-asserted signal alone, or that can be satisfied against the no-op default does not satisfy this requirement.
- Every content-bearing runtime path named in FR-21 fails closed with the typed outcome `PayloadProtectionUnavailable` while the blocker stands, including in a host that binds a live Provider adapter before the protection engine.
- `RQ-1` records the condition as `DependencyNotAvailable` naming `EXT-PROTECTION-1` (FR-28); the no-op default is a readiness blocker, not a default.
- Test: a production-like run against the no-op default, and a run against a wrapper around the no-op default that reports itself as production, must both report `PayloadProtectionUnavailable` on the FR-30 readiness surface, refuse enablement of production-like generation, and refuse every content-bearing path with that typed outcome; the same run with a production engine bound and attested must clear the blocker with no other change.
- Protected content is every field that carries Conversation-derived or generated text under an interaction key: Versioned Proposal Content bodies, retained generation-failure content (FR-10), the caller prompt, and Conversation Context snapshots. Agent Instructions and configuration audit are not protected content and are not content-bearing paths under this requirement, so `hexa` can be configured before the engine is `Available`; §9 states their retention. Identities, versions, decisions, timestamps, policy versions, and keyed content hashes are never inside the protected envelope, so the FR-24 trace survives erasure (§9).

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
- Proposed Agent Reply lifecycle through the public `ProposedAgentReplyState` contract: pending, edited, regenerated, approved, posting pending, posted, posting failed, rejected, abandoned, and expired (FR-18).
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
- Tenant-supplied Provider credentials. Every tenant runs under the platform-owned Provider account in V1, governed by the recorded Provider data-handling record (FR-4, §9).

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
- **NFR-10 Cost Control:** Hard per-tenant monthly and per-call caps warn at 80%, fail closed at 100%, and use atomic reservation plus reconciliation. Per-Party and per-Conversation rate limits and a per-proposal regeneration ceiling bound the consumption a single Participant can cause. Only the hard-enforcement postures `Quotas`, `Budgets`, and `ProviderModelLimits` satisfy launch readiness; `ReportingOnlyMonitoring` and `AcceptedLaunchRisk` are rejected at readiness recording under FR-23's deprecate-and-reject rule.
- **NFR-11 Availability And Recovery:** EventStore business state has RPO 0. Restart or replay cannot duplicate proposal versions, timers, reservations, or Conversation posts. Provider attempts are at-most-once: an attempt whose outcome cannot be determined resolves to a typed `Indeterminate` terminal outcome and is never silently retried, and a per-model retry budget applies only to attempts with a confirmed no-usage outcome (FR-4). A production-like recovery exercise restores interaction processing within 15 minutes and preserves every terminal decision.
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

An `Uncommitted` status, missing target, or missing compatibility verification command blocks every consuming story from `ready-for-dev`. The register's third status, `Available`, means the committed target is installed and consumable and its executable compatibility command passes with live Level 4 evidence: `Committed` unblocks `ready-for-dev`, `Available` is required before any runtime, test, or qualification path executes the seam (FR-21), and `RQ-1` requires every dependency in its qualification profile to be `Available` (FR-28). The initial register scope includes `EXT-CONV-AI-1`, `EXT-CONV-UI-1`, `EXT-HOST-1`, `EXT-PROVIDER-1`, `EXT-SAFETY-1`, `EXT-TOKEN-1`, `EXT-SECRETS-1`, `EXT-PROTECTION-1`, and `EXT-TOPOLOGY-1`; concrete ownership, targets, commands, status, and story mappings remain in the external dependency register.

At the time of this revision, only `EXT-HOST-1` is `Committed`; the remaining entries are `Uncommitted`. Every consuming story therefore remains blocked from `ready-for-dev` under FR-21, and any story already completed against an `Uncommitted` entry carries a non-conformance record in the register.

- **Hexalith.Conversations:** Source Conversation access, complete Conversation Context loading, AI membership, final Conversation Message posting, and the SM-2 denominator depend on Conversations. External prerequisite `EXT-CONV-AI-1` requires Conversations to publish six seams:
  1. Membership: `IConversationClient.AddParticipantAsync` and `POST /api/v1/conversations/{conversationId}/participants`, limited for Agents to `ParticipantType.AiAgent` plus `ParticipantRole.Member`, with deterministic idempotency, typed conflicts, and cross-tenant denial, together with a participant-state read for `hexa` and a participant removal for the FR-2 block, the removal answering typed "already absent" as a confirmed no-op [ASSUMPTION A-1: the register owns the final names; `AiAgent` is the spelling this PRD uses]. Conversations verifies against Hexalith.Parties that an `AiAgent` participant's Party is of AI type [ASSUMPTION A-21].
  2. Posting: appending a Conversation Message as the `AiAgent` participant with an Agents-supplied idempotency key, so that restart or replay cannot duplicate a post (NFR-11), and with message metadata carrying the Agent Call trace reference and the provenance flags FR-11 and FR-17 require, together with an existence read by `MessageId` — answering a message, a typed absence, or a typed `ConversationDeleted` or `PrincipalRemovedFromConversation` — so that Agents can confirm whether a post whose acknowledgement was lost landed before it retries or abandons (FR-18) [ASSUMPTION A-2]. The existence read is not yet in the register's seam-2 text; adding it is a pending register extension (correct-course, 2026-09-09), and `EXT-CONV-AI-1` cannot reach `Available` on a compatibility command that does not exercise it.
  3. Facilitator resolution: `ParticipantRole.Facilitator` on the participant read model (FR-7) [ASSUMPTION A-3].
  4. An active-Conversation count per tenant and window for the Eligible Conversation denominator (§3, SM-2), or a Conversations-side event feed from which Agents computes that count — never from Agent Calls [ASSUMPTION A-4]. The event-feed alternative is a pending register extension (correct-course, 2026-09-09).
  5. Tenant-scoped reads, under the Agents Service Principal, of the complete Conversation content, the Participant roster with roles, and Conversation existence and accessibility, which every Agent Call, the FR-7 resolution, and the FR-18 pre-post re-validation depend on. The existence and accessibility reads answer with the typed values `ConversationDeleted` and `PrincipalRemovedFromConversation`, distinct from any transient denial or error, and the content read returns only the messages a Participant would currently see, with edited content in its current form, so a message deleted in Conversations leaves the scanned history (FR-27, OQ-18) [ASSUMPTION A-15].
  6. A Conversation deletion signal so that an approved deletion in Conversations triggers FR-30 approved deletion of the derived Agent content, leaving only the non-content tombstone [ASSUMPTION A-16].
  No message edit or deletion notification is needed: the FR-27 verdict cache keys on an Agents-computed content hash taken on each load. Removal of `hexa` needs no notification either: the FR-2 membership step detects an external removal and records `ExternallyRemoved`, which rejects calls on the same terms as the Agents-owned block. `EXT-CONV-AI-1` is subject to the same commitment rule as every critical external dependency. Hexalith Agents must not treat unapproved proposals as Conversation Messages or write Conversation streams directly.
- **Hexalith.Conversations UI:** Rendering the Conversation-owned **Call hexa** action and rendering the provenance markers carried in message metadata are a separate prerequisite tracked as `EXT-CONV-UI-1`: a versioned Conversation action contribution and registration contract that lets Agents contribute the invocation action into a Conversation-owned surface, plus rendering of the AI-generated and human-edited markers wherever the message's provenance is disclosed. It carries the same nine-field commitment record and the same fail-closed `ready-for-dev` rule as every other critical external dependency.
- **Hexalith.EventStore Payload Protection:** Field-level `ProtectedContent` envelopes, per-interaction DEK/per-tenant KEK handling, hold pinning, irreversible destruction receipts, and typed `Erased` replay depend on `EXT-PROTECTION-1` [ASSUMPTION A-18: the register owns the final names]. Live content-bearing execution must remain disabled while its register entry is not `Available` (FR-34): the EventStore default protection service is a no-op, Agents registers no protector, and the host (`EXT-HOST-1`) must bind a production engine before production-like evidence can be recorded. §9 retention and erasure, the FR-30 legal-hold, export, and deletion operations, and the FR-24 content fields all depend on this engine.
- **Hexalith.Parties:** Agent identity and Conversation Participant identity depend on Parties. `hexa` must post as a Party identity.
- **Provider Infrastructure:** Provider/model availability depends on the Global Providers Aggregate and the underlying Provider integration selected per Agent.
- **Tenant Access:** Tenant isolation and authorization must align with existing Hexalith tenant access patterns and fail closed when tenant state is missing or unavailable.
- **Admin Surface:** The admin UI must use the same capability and authorization model as the API/client contracts.
- **Release Governance:** Production or production-like generation depends on recorded launch-readiness gates for safety, context, metrics, latency, cost, and audit governance.

### 8.1 Assumptions Index

Every inline `[ASSUMPTION A-n]` key in this PRD resolves to a row here, which records the assumption's owner, provenance, and retirement condition. An assumption is retired by editing the keyed text and this index in the same change.

Architecture assumptions are indexed and governed in the Architecture Assumptions table of the Architecture Spine at `_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md` (frontmatter `updated: 2026-09-09`; at that date the table holds `ARCH-A-1` through `ARCH-A-10`, with `ARCH-A-5` retired) rather than duplicated here. The range is open: every unretired `ARCH-A-n` row in that table at the moment `RQ-1` is evaluated blocks `RQ-1` on the same terms as an `A-n` row, whatever its owner (FR-28), and the Spine's index is authoritative for each row's owner and retirement condition.

An `ARCH-A` row whose retirement condition names Product, Governance, Security, or the Release PM is retired only by a recorded confirmation from that party, never by an Architecture edit alone; a row added after the `RQ-1` evaluation is scheduled blocks only once the Release PM records an acknowledgement, and every Architecture-owned row carries a target retirement date.

Reconciliation runs in both directions. A Spine rule that grants a role an operation absent from FR-33, or that defines a reservation or proposal outcome absent from FR-28 or FR-18, is a PRD amendment pending under the §0 landing rule, not an architecture detail; the 2026-09-09 update brought two such rules into this PRD (`Unreconciled` settlement in FR-28, `hexa` provisioning in FR-1 and FR-33). A Spine rule that narrows or contradicts an FR-18, FR-24, FR-28, or FR-33 rule is a Spine defect to be corrected before the consuming story is `ready-for-dev`; three are open at this revision, owned by Architecture:

- AD-5 forbids the system abandonment from `Approved` and `PostingFailed` on a detected removal that FR-18 requires, and allows human abandonment from `PostingFailed` only.
- AD-22 names the Tenant Agent Administrator as the sole inspection second party and gives export and hold release none, against FR-24 and FR-33.
- AD-12 states the Release Operator trigger without the review, minimum sample, and `InsufficientEvidence` rule of FR-28.

The table below remains the authority for `A-1` through `A-21`.

| # | Assumption | Where | Owner | Retired when |
| --- | --- | --- | --- | --- |
| A-1 | Conversations publishes `IConversationClient.AddParticipantAsync`, the participants route, `ParticipantType.AiAgent`, and `ParticipantRole.Member` as named | §8 | Conversations Maintainer | `EXT-CONV-AI-1` reaches `Committed` with these names, or the register records the final names |
| A-2 | Conversations publishes idempotent posting as the `AiAgent` participant with trace and provenance metadata (serves FR-11, FR-17, NFR-11) | §8 | Conversations Maintainer | `EXT-CONV-AI-1` reaches `Committed` with the posting seam |
| A-3 | Conversations publishes `ParticipantRole.Facilitator` on the participant read model (cited by OQ-14) | §3, FR-7, §8 | Conversations Maintainer | `EXT-CONV-AI-1` reaches `Committed` with the role |
| A-4 | Conversations exposes an active-Conversation count per tenant and window for the SM-2 denominator | §3, §8 | Conversations Maintainer + Release PM | `EXT-CONV-AI-1` reaches `Committed` with the read. An Agents-owned substitute is acceptable only if it counts Conversations from a Conversations-side event feed, never from Agent Calls, which would make SM-2 measure itself |
| A-5 | Safe Context Budget margin default 10%, range 5–25% | FR-9 | Product + Architecture | Product confirms or retunes before enablement |
| A-6 | Regeneration ceiling default 3, range 1–10 (restated in FR-32) | FR-16 | Product | Product confirms or retunes before enablement |
| A-7 | `PostingFailed` retry bound of 3 attempts over 15 minutes; the Spine fixes the bound as non-configurable (AD-5), which this row does not contest | FR-18 | Architecture | Architecture confirms the value before enablement |
| A-8 | `Indeterminate` reservation hold default 24 hours, range 1–72 hours | FR-28 | Architecture | Architecture confirms or retunes before enablement |
| A-9 | Block authority for `hexa` is Tenant Agent Administrator plus Conversation Facilitator, and a block is cleared only by the setting authority or the Tenant Agent Administrator | FR-2, FR-33 | Product | Product confirms the FR-33 rows |
| A-10 | Per-tenant Provider/model enablement is a Platform Operator action that permits selection; the Tenant Agent Administrator accepts a named `DataHandlingVersion` with a 30-day grace on change; secret state is Platform-Operator-only | FR-4, FR-33 | Product + Platform Maintainer | Product confirms the FR-33 rows and the grace |
| A-11 | Every Conversation Participant may call `hexa` unless the Tenant Agent Administrator restricts calling to a tenant role (the permission FR-8 enforces) | FR-33 | Product | Product confirms the FR-33 row |
| A-12 | Approved deletion requires Platform Operator with Compliance Inspector approval | FR-33 | Product + Governance | Product confirms the FR-33 row |
| A-13 | SM-3 threshold 80% within the lesser of the configured expiry and 24 hours, over every proposal; SM-7 band 10–60% (restated in OQ-11) | §12 | Product + Release PM | Product confirms or retunes before enablement |
| A-14 | OQ-18 decision due 2026-10-15 | §13 | Product + Security | The OQ-18 decision lands; moving the date does not retire this row |
| A-15 | Conversations exposes tenant-scoped content, roster-with-roles, and existence/access reads under the Agents Service Principal | §8 | Conversations Maintainer | `EXT-CONV-AI-1` reaches `Committed` with the reads |
| A-16 | Conversations exposes a Conversation deletion signal that triggers FR-30 deletion of derived content | §8 | Conversations Maintainer + Governance | `EXT-CONV-AI-1` reaches `Committed` with the signal |
| A-17 | Kill-switch trigger-review thresholds over seven days: blocked-call share above 50%, unavailable-call share above 20%, Provider-or-generation failure share above 20%, posting-failure rate above 10%; minimum sample 5 distinct calling Parties and 50 Agent Calls (every calling Party and 20 calls below 5 Parties); two consecutive SM-3/SM-7 misses | FR-28 | Product + Release PM | Product confirms or retunes before enablement |
| A-18 | EventStore publishes a field-level `ProtectedContent` envelope and an `Erased` unprotection value under those names | §8 | EventStore Maintainer | `EXT-PROTECTION-1` reaches `Committed` with those names, or the register records the final names |
| A-19 | Post-hoc review window for a single-proposal or single-Conversation compliance inspection is 7 days | FR-24 | Product + Governance | Product confirms or retunes before enablement |
| A-20 | Scheduled Eligible Approver re-check cadence defaults to hourly, configurable from 15 minutes through 24 hours | FR-7 | Architecture + Product | Architecture confirms or retunes before enablement |
| A-21 | Conversations verifies against Hexalith.Parties that an `AiAgent` participant's Party is of AI type | §8 | Conversations Maintainer | `EXT-CONV-AI-1` reaches `Committed` with the check, or the register records that Agents must verify it before membership |

## 9. Data Governance And Audit

- Audit Evidence must be tenant-scoped and accessible only to authorized Parties or operators.
- Audit Evidence must preserve every generated, edited, and regenerated proposal version.
- Audit Evidence must link automatic posts and approved posts to the Agent Call, Source Conversation, caller, Agent, Provider/model, response mode, and final Conversation Message.
- Editing a Proposed Agent Reply must never overwrite the prior version.
- Regenerating a Proposed Agent Reply must never delete prior generated or edited versions.
- Rejected, abandoned, and expired proposals remain audit records and cannot later be posted.
- Provider secrets and raw credentials are never audit content.
- Sensitive Agent content is retained for 365 days after the interaction reaches a terminal state unless legal hold suspends the retention period; generation-failure records follow the same clock from the failed call. Configuration evidence, including Agent Instructions and their prior values, is not protected content and is retained for the life of the tenant plus 365 days. Authorized export is tenant-scoped, encrypted, time-limited, manifested, and audited.
- EventStore history is never rewritten. End of the retention period or approved deletion cryptographically erases or redacts protected sensitive payloads and purges affected projections while retaining only a support-safe non-content tombstone; completion requires restrictive confirmation from the payload-protection engine (`EXT-PROTECTION-1`, FR-34) and every affected projection. Identities, versions, decisions, timestamps, policy versions, and content hashes are outside the protected envelope and survive erasure, so the FR-24 trace remains provable without content.
- A legal hold takes precedence over every deletion signal, including retention expiry and a Conversations-side deletion (A-16): the signal is recorded as `DeletionDeferredByHold`, visible to the Compliance Inspector, and executes on hold release. Provenance metadata persisted into Conversations is governed by Conversations retention and carries no Agents-erasable content beyond Party identifiers and trace references.
- Conversation Context sent to a Provider is governed by the Provider's recorded data-handling record (FR-4); a model whose record is unrecorded cannot be enabled for any tenant, and the record version in force is part of each call's Audit Evidence (FR-24).
- Audit Evidence names the Conversation Facilitator as the resolved approval authority where that policy source applied, and never states or implies that a distinct Conversation owner was resolved.
- Audit Evidence distinguishes a human-edited posted version from generated output, and records the editing and approving Parties.
- Inspecting Audit Evidence that contains Conversation-derived content follows FR-24: posted provenance for current Participants, unposted content for resolved Eligible Approvers or scoped compliance inspection, and fail-closed denial otherwise. Retained evidence remains inspectable under compliance inspection after its Source Conversation is gone (FR-24).
- An approved deletion of a Conversation in Hexalith.Conversations triggers FR-30 approved deletion of the derived Agent content for that Conversation, retaining only the non-content tombstone, so erasure propagates across the two systems (§8, A-16).
- Posted Conversation Messages remain governed by Hexalith.Conversations retention.

## 10. API Contracts And Public Surface

V1 must expose public API/client contracts for these capability areas:

- Provider administration: create, update, list, enable, or disable Provider and model options, and administer versioned pricing metadata, capability limits, and the `CapabilityVersion` revision, freshness, and acceptance fields, where authorized.
- Agent administration: configure `hexa`, lifecycle, Party identity (read-only), Agent Instructions, Provider/model selection, Response Policy, Approver Policy, proposal expiry duration, regeneration ceiling, and Conversation Context Policy; the Platform Operator's create-only provisioning at tenant enablement (FR-1).
- Agent invocation: call `hexa` from a Source Conversation.
- Proposal workflow: list pending proposals, inspect proposal versions, edit, regenerate, approve, reject, abandon, and inspect expiry.
- Status: inspect Agent readiness, Provider readiness, context policy outcome, content safety outcome, Agent Call status, proposal state, and posting outcome.
- Audit: inspect authorized Audit Evidence for Agent Calls, proposal lifecycle, and posted responses.
- Budget policy: configure and inspect per-tenant cost caps, per-Party and per-Conversation rate limits, consumption against each, reserved-versus-settled spend, and audited overrides.
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

Approved deterministic fixtures may prove metric formulas, sample, window and cohort handling, timestamp rules, and `InsufficientEvidence` behavior. Passing these fixtures completes calculator implementation; it does not prove live metric attainment. The complete `RQ-1` input list in FR-28 and the final READY or NOT READY decision, recorded in the launch readiness register, remain the operational `RQ-1` gate after implementation. Rolling-window and cohort attainment of the launch-health metrics SM-2, SM-3, and SM-7 is measured after enablement and reviewed under OQ-22.

## 12. Success Metrics

Metrics fall into two groups. **Pre-enablement gate metrics** can be produced by a qualification cohort before any tenant is enabled and are the metric inputs to `RQ-1`. **Launch-health metrics** can only be produced by real usage, so they are reviewed at 30 and 60 days after enablement and monthly thereafter, and feed the launch-health review and kill-switch triggers instead of the gate (OQ-22). Thresholds keyed `[ASSUMPTION A-13]` were set by the 2026-09-09 reconciliation and are confirmed or retuned by Product before enablement.

**Pre-enablement gate metrics (evaluated by `RQ-1`)**

- **SM-1: Active tenant adoption** - At least one launch tenant configures `hexa`, has a Provider/model enabled for it, and records successful Agent Calls in production-like launch validation. Validates FR-1 through FR-6 and FR-8.
- **SM-4: Unauthorized action prevention** - Authorization tests and qualification telemetry show zero successful cross-tenant or unauthorized Agent Calls, proposal actions, or audit inspections. SM-4 is `InsufficientEvidence` unless every FR-33 row has at least one recorded denial in qualification telemetry and the forged-extension test named in the Architecture Spine has run. Validates FR-19 through FR-21 and FR-33.
- **SM-5: Audit completeness** - Every posted Agent Response in the qualification cohort has complete Audit Evidence linking caller, Agent, Source Conversation, Provider/model, proposal path when applicable, and final Conversation Message. Validates FR-11, FR-17, and FR-24.
- **SM-6: Admin/API parity** - Core administration and workflow operations are available through both admin UI and API/client contracts with the same authorization outcomes. This is a gate check on a requirement rather than an outcome measure, and is kept here so that `RQ-1` evaluates it explicitly. Validates FR-22 and FR-23.

**Launch-health metrics (reviewed after enablement)**

SM-3 and SM-7 apply to Confirmation Response Mode tenants. An Automatic Response Mode tenant is covered by SM-C4 and the posting-failure rate; a retraction metric for automatic posts is deferred (OQ-23). Reviews run at 30 and 60 days after enablement and monthly thereafter.

- **SM-3: Approval workflow completion** *(primary)* - At least 80% of Proposed Agent Replies created during a rolling 30-day window receive a human Approver decision — a transition from `Pending`, `Edited`, or `Regenerated` into `Approved`, `Rejected`, or `Abandoned` — within the lesser of the Agent's configured expiry and 24 hours, measured over every proposal so that no expiry setting excludes a tenant `[ASSUMPTION A-13; the prior threshold was 95% terminal within 26 hours]`. `Expired` and system abandonment are never decisions; system-abandoned proposals and proposals recorded `ExpiredWhileSuspended` leave the denominator, except that a system abandonment from `Approved` or `PostingFailed` follows a human decision and counts as decided. At the default 24-hour expiry this metric and SM-C5 are two views of the same proposals by design; they diverge for longer expiries. `Approved`, `PostingPending`, and `PostingFailed` count as decided but not yet posted. Within the measured cohort, the posting-failure rate is at most 2% and 100% have complete Audit Evidence; the expiry rate is tracked separately by SM-C5. Validates FR-13 through FR-18 and FR-24.
- **SM-7: Governed review is substantive** *(primary)* - Among proposals that reach a human decision in the window, between 10% and 60% are edited, regenerated, or rejected before that decision `[ASSUMPTION A-13]`. A share below the band signals that approval is rubber-stamping; a share above it signals that generation is not usable. This is the metric that could falsify the §1 thesis: governed participation is valued only if review changes outcomes without making the Agent unusable. Validates FR-14 through FR-17 and FR-27.
- **SM-2: Conversation adoption** *(secondary)* - At least 20% of eligible Conversations in the enabled launch cohort record one or more Accepted Agent Calls during a rolling 30-day window, measured only when the cohort contains at least 50 eligible Conversations; regenerations are excluded from the numerator. Validates FR-8, FR-9, FR-11, and FR-13.

**Counter-Metrics (do not optimize blindly)**

- **SM-C1: Automatic post volume without review** - Do not maximize automatic posting. A launch tenant may run Confirmation Response Mode tenant-wide for safety reasons, and such a tenant counts toward SM-2 on the same terms as one in Automatic Response Mode. Counterbalances SM-2.
- **SM-C2: Approval speed at the cost of audit quality** - Do not optimize approval completion time by dropping version preservation or approval evidence. Counterbalances SM-3.
- **SM-C3: Provider breadth before governance** - Do not optimize the number of Providers/models if Provider governance, secret safety, and per-Agent selection are not robust. Counterbalances SM-1.
- **SM-C4: Structural unavailability** - Track the share of Agent Calls blocked, per tenant, over the same rolling 30-day window. Calls fall into three classes:
  - Blocked calls, whose reasons are exactly: Conversation Context Policy, Content Safety Policy, cost caps, rate limits, `NoEligibleApprover`, and `RemovedInConversations` or the Agents-owned block. §3 and FR-25 cite this list.
  - Failed calls: Provider and generation failures, tracked separately and never counted as blocked calls.
  - Unavailable calls — `MembershipUnavailable`, `ApproverResolutionUnavailable`, `ContextUnavailable` — tracked per reason under FR-25 and included in the FR-28 trigger review as their own share.
  Authorization denials, capacity queueing or rejection, `DependencyNotAvailable`, and `PayloadProtectionUnavailable` are in none of the three classes and are reported separately. Also track the count of system-abandoned proposals by reason and the count of proposals carrying `ResolutionUnavailable`. A sustained blocked-call share above 10% is a launch-health signal that `hexa` is unavailable in the Conversations it is meant to serve, and is reviewed before and after enablement; the FR-28 trigger review uses this share over distinct (Party, Conversation, reason) tuples with rate-limit rejections excluded. Because Eligible Conversation includes attempted-but-blocked Conversations, these failures are never silently excluded from the SM-2 denominator. Counterbalances SM-1 and SM-2.
- **SM-C5: Expiry as the dominant terminal outcome** - Track, per tenant, the share of terminal proposals that ended `Expired` rather than by a human decision, over the same rolling 30-day window regardless of the configured expiry, excluding proposals recorded `ExpiredWhileSuspended`. A share above 20% is a launch-health signal that approval is not happening, and for tenants with an expiry beyond 24 hours it catches what SM-3's 24-hour cut cannot. Counterbalances SM-3.

## 13. V1 Decision Register

Rows OQ-1 through OQ-13 were resolved by the approved Correct Course decisions on 2026-08-01. Rows OQ-14 and OQ-15 apply the approved sprint change proposal of 2026-08-03. Rows OQ-16 through OQ-19 record decisions taken in the 2026-09-08 validation reconciliation, which also amended OQ-3, OQ-6, OQ-9, and OQ-10 in place. Rows OQ-20 through OQ-23 record decisions taken in the 2026-09-09 validation reconciliation, which Product approved as the separate change that reopened the success metrics; that reconciliation resolved OQ-19 and amended OQ-3, OQ-6, OQ-9, OQ-11, OQ-14, OQ-16, and OQ-18 in place. Rows OQ-24 through OQ-30 record decisions taken in the second 2026-09-09 update, which applied the validation report of that day and amended OQ-16, OQ-18, OQ-21, and OQ-22 in place; its reviewer gate amended OQ-16 and OQ-24 through OQ-30 again in place. OQ-12 was clarified on 2026-09-08. The Evidence Level taxonomy is normative in §11. Changes apply to future calls and proposals unless a row states otherwise.

Rows marked *Deferred* are open decisions with a named owner and a revisit condition; they are not resolved and must not be read as settled.

| ID | Binding V1 Decision | Owner | Status |
| --- | --- | --- | --- |
| OQ-1 | The sole V1 invocation entry is a Conversation-owned **Call hexa** action. No mention, command, ambient trigger, or alternate entry point is in V1. | Product + UX | Resolved 2026-08-01 |
| OQ-2 | Hexalith Agents owns durable proposal state and all proposal read models. Dapr Workflow owns execution only and does not become a domain system of record. | Architecture | Resolved 2026-08-01 |
| OQ-3 | Proposal expiry defaults to 24 hours and is configurable per Agent from 1 hour through 30 days for future proposals only. `ExpiresAt` applies only while a proposal awaits a decision: one whose stored `ExpiresAt` has passed while `Pending`, `Edited`, or `Regenerated` is treated as `Expired` on every read and command, regardless of timer delivery, and approval freezes expiry so `Approved`, `PostingPending`, and `PostingFailed` never expire. Execution mechanism is an architecture concern, not a product requirement. | Product + Architecture | Resolved 2026-08-01; amended 2026-09-08 and 2026-09-09 |
| OQ-4 | V1 uses in-product proposal notifications and pending-count surfaces only. Email, push, and external-channel notification delivery are out of scope. | Product + UX | Resolved 2026-08-01 |
| OQ-5 | Automatic accepted-call-to-post and confirmation accepted-call-to-proposal are p95 ≤ 60 s and p99 ≤ 120 s. Approval-to-post is p95 ≤ 10 s and p99 ≤ 30 s. Fast pre-Provider rejection is p95 ≤ 2 s. Each gate uses at least 30 production-like executions. | Architecture + Release PM | Resolved 2026-08-01 |
| OQ-6 | V1 enforces hard per-tenant monthly and per-call caps, warns at 80%, and fails closed at 100%. It atomically reserves the estimated attempt cost after context measurement and before Provider invocation, releases it with reason `NotInvoked` if a later pre-Provider check fails, reconciles actual usage, and reuses the reservation for eligible retries. A reservation whose Provider outcome is confirmed unused is released on confirmation. A reservation whose outcome is `Indeterminate` is held for a bounded period (default 24 hours, A-8) and then conservatively settled at the estimated maximum rather than held indefinitely, with an audited administrative override available to restore service. Caps are configured under FR-32 with no implicit default. Missing pricing/budget state blocks invocation. Only the hard-enforcement postures `Quotas`, `Budgets`, and `ProviderModelLimits` satisfy readiness; `ReportingOnlyMonitoring` and `AcceptedLaunchRisk` are on the FR-23 deprecate-and-reject register and are rejected at readiness recording. | Product + Architecture | Resolved 2026-08-01; amended 2026-09-08 and 2026-09-09 |
| OQ-7 | The Agents-owned Global Providers Aggregate records Provider/model identifiers, enablement, capabilities and limits, secret references, versioned pricing metadata, and a `CapabilityVersion`. | Architecture | Resolved 2026-08-01 |
| OQ-8 | Sensitive Agent content is retained for 365 days after the interaction reaches a terminal state unless legal hold applies. Authorized export is encrypted and time-limited. Deletion cryptographically erases or redacts sensitive content and purges projections while immutable EventStore history retains only a safe tombstone. | Product + Governance | Resolved 2026-08-01 |
| OQ-9 | Prompt and complete Conversation Context are checked before Provider invocation, and output is checked before proposal or Conversation side effects. The active policy always blocks these categories: child sexual abuse/exploitation; credible imminent serious-harm threats/instructions; encouragement/instruction for suicide/self-harm; credential theft, malware, or unauthorized compromise; secrets/private credentials; cross-tenant or unauthorized personal/Conversation data; and control-bypass attempts. Restricted hate, harassment, sexual, violent, illegal-activity, or sensitive-personal content requires an explicitly permitted tenant use case and Confirmation Response Mode. The policy also blocks generated content that impersonates a named Party or asserts a decision on a Party's behalf. Policy is applied at four points: prompt and Conversation Context before Provider invocation; generated output before any proposal or Conversation side effect; the exact version being approved — generated or human-edited — at approval time; and that same version again immediately before posting when approval and posting are not simultaneous. The approval-time and pre-post checks always evaluate the then-current active policy. The pre-Provider scan covers the caller prompt and not-yet-scanned Conversation content, reusing a cached per-Conversation verdict for scanned history; the cache is keyed by Conversation, an Agents-computed per-message content hash, and policy version, is invalidated by policy publication and misses on any edited or deleted message, and the policy version behind each reused verdict is recorded as Audit Evidence. Approvers cannot override failures, and retries cannot use a weaker policy. | Product + Security | Resolved 2026-08-01; amended 2026-09-08 and 2026-09-09 |
| OQ-10 | V1 uses the complete Source Conversation when it fits the Safe Context Budget; otherwise it fails closed before Provider invocation, or applies an Approved Bounded Context Behavior that the active Conversation Context Policy explicitly declares and that is recorded as Audit Evidence. Silent truncation, summarization, and windowing are prohibited under every policy. V1 declares no Approved Bounded Context Behavior, so the oversized case always fails closed in V1; the seam exists so a governed bounded behavior can be approved later without a contract change. The trade-off is accepted deliberately: `hexa` is unavailable in Conversations that outgrow the budget, in exchange for never sending a silently reduced context to a Provider. SM-C4 makes that unavailability visible. | Architecture | Resolved 2026-08-01; amended 2026-09-08 |
| OQ-11 | SM-2 is ≥ 20% adoption over a rolling 30 days with at least 50 eligible Conversations, regenerations excluded. SM-3 is ≥ 80% human decision within the lesser of the configured expiry and 24 hours, measured over every proposal, with a posting-failure rate ≤ 2% and audit completeness = 100%; the expiry rate ≤ 20% moves to counter-metric SM-C5. SM-7 is an edit-regenerate-or-reject share of 10–60% among decided proposals. The SM-3 and SM-7 numbers are provisional (A-13) until Product confirms them before enablement. | Product + Release PM | Resolved 2026-08-01; amended 2026-09-09 |
| OQ-12 | V1 Agent Calls draw only on the authorized Source Conversation — in V1 always the complete Source Conversation, subject to the context-extent rule in OQ-10. Long-term memory, project content, folder content, external tools, and non-conversation retrieval are excluded. This row governs which *sources* may be used, not how much of the Conversation fits. | Product + Architecture | Resolved 2026-08-01; clarified 2026-09-08 |
| OQ-13 | Generalized internal Agent structures are permissible, but V1 product behavior exposes only the named Agent `hexa`. | Product | Resolved 2026-08-01 |
| OQ-14 | Where the Approver Policy uses the Conversation-authority source, that source is the Conversation Facilitator, resolved normatively from `ParticipantRole.Facilitator`. Predefined Parties and tenant roles remain separately configurable sources under FR-7; the caller source is retired on the FR-23 register because the Eligible Approver predicate excludes the caller from every proposal the source could apply to (amended 2026-09-09). The Conversations contract exposes no owner field, so V1 resolves no distinct Conversation owner, and no UI text, API documentation, or evidence narrative may imply otherwise. The stable wire identifier for the source remains `ApproverPolicySourceKind.ConversationOwner`, which FR-23 forbids renaming; both surfaces label it Conversation Facilitator. `ParticipantRole.Facilitator` is assumption A-3 until Conversations commits it. | Architecture | Resolved 2026-08-03; amended 2026-09-09 |
| OQ-15 | A true Conversation-owner resolver is out of V1 and returns only if Hexalith.Conversations commits an explicit owner contract. | Product + Architecture | Deferred post-V1; revisit on a committed Conversations owner contract |
| OQ-16 | `hexa` joins a Conversation as the last pre-Provider step of the first Accepted Agent Call there, under the Agents Service Principal, driven by the Conversation Agent State machine `NeverJoined → Joined → (ExternallyRemoved | Blocked) → ReadmitPending → Joined`, where `Blocked` may be set from any state (FR-2, OQ-25). External removal is detected only from `Joined` with the last mirror confirmed and no newer clear; re-admission happens from `ReadmitPending`, or from `ExternallyRemoved` when `hexa` is found present again; an unavailable participant read rejects the call and changes nothing. Membership failure fails the call closed before Provider work; verifying membership only at posting time is non-conformant. The block is an Agents-owned per-Conversation record, authoritative when written and mirrored to the Conversations participant list by an at-least-once outbox with a visible `MirrorPending` flag; it is set by the Tenant Agent Administrator or Conversation Facilitator and cleared only by the setting authority or the Tenant Agent Administrator (A-9). While set, calls are rejected pre-Provider with a typed reason, no join is re-established, and non-terminal proposals are abandoned with versions preserved. | Product + Architecture | Resolved 2026-09-08; amended 2026-09-09 |
| OQ-17 | The FR-21 dependency gate stands as written: an `Uncommitted` entry blocks consuming stories from `ready-for-dev`. Work completed against an `Uncommitted` entry is recorded as non-conformant in the dependency register rather than retroactively legitimized. | Architecture + Release PM | Resolved 2026-09-08 |
| OQ-18 | Per-category handling of unsafe *historical* Conversation content is deferred. In V1 a single historical message that fails the active policy blocks generation in that Conversation until the history is re-evaluated under a policy it passes (FR-27 cache contract), with no Agents-side redaction or exclusion path; where Conversations permits it, a Participant may edit or delete the offending message there, and the next load re-scans the current history (§8 seam 5). | Product + Security | Deferred; amended 2026-09-09; decision due 2026-10-15 [ASSUMPTION A-14], and in any case before production enablement (`OpenDecision` blocker, FR-28) |
| OQ-19 | The 2026-09-08 escalation on success metrics is resolved by the Product-approved 2026-09-09 reconciliation. SM-3 now measures human-decision latency rather than the expiry setting and is attainable under every OQ-3 expiry; SM-7 adds a governance-value primary that could falsify the §1 thesis; SM-2 is secondary; SM-1 no longer depends on the gate it feeds. §12 separates pre-enablement gate metrics from launch-health metrics (OQ-22). The provisional thresholds are assumption A-13. | Product + Release PM | Resolved 2026-09-09 |
| OQ-20 | `hexa` is instantiated per tenant, with its own configuration, Party identity, policies, and consumption bounds. Response mode is tenant-wide in V1; no per-Conversation or per-context override exists. A Conversation-level override, if ever wanted, is a new FR and a post-V1 decision. | Product | Resolved 2026-09-09 |
| OQ-21 | Audit Evidence containing Conversation-derived content is inspectable at two levels: posted provenance by a current Participant with read access, and unposted content by a resolved Eligible Approver or under a scoped, justified compliance inspection held by the Compliance Inspector role and approved by a second party who is neither the Inspector nor a subject of the case — the Platform Operator or a second Compliance Inspector where the case concerns the Tenant Agent Administrator — with post-hoc review allowed only for single-proposal or single-Conversation scopes within the A-19 window (FR-24, OQ-30). Evidence remains inspectable under compliance inspection after its Source Conversation is deleted or inaccessible. FR-33 is the authoritative role matrix for every authorization rule; its `[ASSUMPTION]` rows are confirmed by Product before the first tenant is enabled. | Product + Governance | Resolved 2026-09-09; amended 2026-09-09 |
| OQ-22 | `RQ-1` evaluates exactly the input list stated in FR-28 — evidence a qualification cohort can produce before enablement, dependency availability, policies and postures, and the absence of unretired assumptions and open decisions — and nothing else. SM-2, SM-3, and SM-7 are launch-health metrics reviewed at 30 and 60 days after enablement and monthly thereafter; a miss triggers the launch-health review and, where the kill-switch trigger conditions stated in FR-28 are met, the kill switch. Unretired Product, Architecture, or Governance assumptions in §8.1, and unretired `ARCH-A` rows in the Architecture Spine, block `RQ-1`. | Product + Release PM | Resolved 2026-09-09; amended 2026-09-09 |
| OQ-23 | A launch-health metric for Automatic Response Mode tenants — the share of automatically posted Agent Responses retracted, deleted, or flagged by a human within seven days — is deferred because it needs a Conversations message-retraction seam that `EXT-CONV-AI-1` does not yet name. Until then Automatic Response Mode tenants are covered by SM-C4 and the posting-failure rate only. | Product + Conversations Maintainer | Deferred; revisit when `EXT-CONV-AI-1` is `Committed`, and before the first Automatic-mode tenant is enabled (`OpenDecision` blocker for that enablement, FR-28) |
| OQ-24 | `RQ-1` has one input list, stated in FR-28 and cited by §3, §11, and OQ-22. Dependency availability is an input: every dependency in the qualification profile must be `Available`, otherwise `DependencyNotAvailable`. The "disabled while not `Available`" rule for payload protection is FR-34, with the additive blocker `PayloadProtectionUnavailable` cleared only by a seal-unseal-erase attestation against the register's committed engine, named owners, and a test that a self-reporting wrapper around the no-op default still blocks. The qualification profile is the full §8 scope; a gate the register carries outside the FR-28 list is `GateOutOfScope`; the compatibility command is the one permitted execution of a `Committed` seam. `RQ-1` is recorded in the launch readiness register; the external dependency register is the commitment authority; the FR-30 surface is the runtime record. Every unretired `A-n` and `ARCH-A-n` row blocks `RQ-1` whatever its owner. | Product + Release PM + Architecture | Resolved 2026-09-09; amended 2026-09-09 |
| OQ-25 | The FR-2 membership step is the Conversation Agent State machine (OQ-16). The block is authoritative when written and versioned by `BlockVersion`; its mirror to Conversations is an at-least-once outbox with a `MirrorPending` flag, and a late mirror effect is never read as an external removal; a block is cleared only by the setting authority (evaluated at clear time) or the Tenant Agent Administrator, an `ExternallyRemoved` record by the Tenant Agent Administrator or any current Facilitator, and clearing records `ReadmitPending` rather than re-joining. `hexa`'s Party identity is created at provisioning as an AI-type Party and is immutable; the shipped link and replace commands are deprecate-and-reject (FR-2). | Product + Architecture | Resolved 2026-09-09; amended 2026-09-09 |
| OQ-26 | No transient outage moves a proposal to a terminal state. The FR-7 re-check, over awaiting and `PostingFailed` proposals, abandons only on a typed `ConversationDeleted` or `PrincipalRemovedFromConversation` answer, or on two consecutive authoritative-empty rosters, and otherwise records `ResolutionUnavailable` or `ResolutionEmptyPending`; before any exit from `PostingFailed`, the `MessageId` lookup decides whether the post landed, in which case the proposal is `Posted` with `LateConfirmed`, and a typed deleted or removed answer counts as no message for abandon exits only. `PostingPending` is uninterruptible. FR-18 carries the full transition table; edit and regeneration are rejected after approval; a safety-verdict `PostingFailed` accepts abandon only; every retry re-runs pre-post re-validation. The system abandonment from `Approved` and `PostingFailed` on a detected removal is a deliberate divergence from Spine AD-5, recorded in §8.1 as a Spine defect for Architecture to correct. | Product + Architecture | Resolved 2026-09-09; amended 2026-09-09 |
| OQ-27 | The FR-28 kill-switch semantics govern: it never system-abandons; awaiting proposals stay for humans to reject or abandon; `Approved` proposals wait without posting (safety wins over "complete on their own terms", a Spine AD-12 correction owned by Architecture); a `PostingPending` attempt in flight completes on its own terms; the `PostingFailed` retry clock pauses; `ExpiresAt` keeps running, with `ExpiredWhileSuspended` excluded from SM-3 and SM-C5 so the switch cannot manufacture its own launch-health miss. FR-25 reports `Suspended`. The same roles release it with audited justification. The Release Operator trigger is a review convened within one business day over four rates with a tuple denominator and a minimum sample scaled for small tenants; the pull is the review's recorded decision; the Platform Operator confirms SM-4 events. | Product + Release PM | Resolved 2026-09-09; amended 2026-09-09 |
| OQ-28 | The Platform Operator provisions `hexa` once per tenant at tenant enablement, create-only, as the Spine's AD-2/AD-30 model states; the Tenant Agent Administrator enables and configures it. FR-1 and FR-33 are aligned to that model rather than the Spine amended, because the Spine rule was approved after the FR-1 text and a Platform-scoped, idempotent provision is the simpler guarantee of one `hexa` per tenant (FR-6). Provisioning creates the AI-type Party identity; no tenant role changes it. | Product | Resolved 2026-09-09; amended 2026-09-09 |
| OQ-29 | Every Provider/model carries a data-handling record — retention term, training-use status, processing region, contractual reference — under its own `DataHandlingVersion`, with a retained contractual document; a model without one cannot be enabled for any tenant; Platform enablement permits selection and the Tenant Agent Administrator accepts the version before first activation and on each change with a 30-day grace; each Provider attempt's Audit Evidence names the record version. Tenant-supplied Provider credentials are out of V1. | Product + Governance | Resolved 2026-09-09; amended 2026-09-09 |
| OQ-30 | A compliance-inspection second party is outside a computed subject set (callers, editors, Approvers, decision actors, Facilitators in scope, and the Tenant Agent Administrator whose configuration was in force); where that set holds the Administrator, the Platform Operator or a second Compliance Inspector approves; two Inspectors cannot approve each other within 30 days; inspections wider than one Conversation need the Platform Operator. Post-hoc review is limited to single-proposal or single-Conversation scopes within the A-19 window, else recorded as unreviewed. Export requires prior second-party approval; hold release requires a separate approver. | Product + Governance | Resolved 2026-09-09; amended 2026-09-09 |
