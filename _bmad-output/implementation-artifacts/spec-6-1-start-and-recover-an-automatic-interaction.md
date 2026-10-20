---
title: '6.1 Start And Recover An Automatic Interaction'
type: 'feature'
created: '2026-10-19'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '{project-root}/_bmad-output/implementation-artifacts/epic-6-context.md'
  - '{project-root}/_bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/IMPLEMENTATION-CONVENTIONS.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** An accepted Agent Call has no durable owner. Interactions are created directly and carry plaintext prompts, and nothing serializes acceptance against Conversation deletion. No workflow survives a restart without duplicating commands, versions or posts.

**Approach:** Add the content-free `ConversationAgentState` interaction directory (permit, outboxes, effect leases, Closing barrier) and a permit-created `AgentInteraction` path. Add one Dapr Workflow per interaction (instance id `AgentInteractionId`) that runs the AD-13 step 5–10 sequence and the Provider branch through deterministic activities, and resumes from EventStore state. Prove all of it at Level 2.

## Boundaries & Constraints

**Always:**
- Decided 2026-10-19: this run is goal A only. Goal B (legacy `InteractionDirectoryMigration`, write fence, repair fence, epoch bridge) is deferred. The permit therefore stays fail-closed (`DirectoryNotReady`) unless a directory-readiness port supplies an epoch, which only test fixtures do.
- Decided 2026-10-19: proceed ahead of Stories 5.5–5.8. Every live seam stays closed and reports its exact `DependencyNotAvailable(record)`, and no evidence is labelled Level 4/5.
- EventStore is the truth. Each activity re-reads state, dispatches at most one trusted command, and returns the recorded outcome on replay.
- A lease reservation authorizes nothing. Only the same-owner commit revision authorizes its phase, and Closing races that commit at the same expected revision.
- Workflow inputs, outputs, custom status, timers and activity contracts hold only ids, revisions, digests, safe enums and `ProtectedContentReference`s.
- Request content exists only as EventStore `ProtectedContent` (`ProtectionTarget` with an explicit alias). Live intake rejects with `PayloadProtectionUnavailable` until `EXT-PROTECTION-1` is available.
- Ids come from AD-29 formulas through `AgentsSecurityIdentity.Derive`.
- New shapes are additive. Existing events, the `agent-interaction` domain and replay stay unchanged.

**Never:**
- Microsoft Agent Framework orchestration, DurableAgent, MCP, A2A, hosted services, in-memory workers, or any second durable owner.
- Registering the Dapr Workflow runtime in the default host while `OD-DAPR-SECURITY-1` is Open.
- A local output-safety status mapping (`OD-INITIAL-OUTPUT-SAFETY-STATUS-1`), or a local rate/concurrency policy (`OD-RATE-CONCURRENCY-CONSUMPTION-1`).
- Retaining failed or incomplete output, or a `GenerationFailureRecord`.
- The User proposal action-intent outbox (deferred 2026-10-20 to Epic 7).
- Modelling the FR-2 membership states (Story 6.6), deletion Effective or purge (Story 8.3), or real context, safety, Budget, capacity, Provider or posting adapters (Stories 6.2–6.6).

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Happy path | Gates pass; directory ready; deterministic all-allow activities | One permit, one interaction, one workflow. The lease chain settles, `AgentCallAccepted` is recorded, then `ProviderInvocationAuthorized`, the generated version, and the post | N/A |
| Restart | A fault before or after every command checkpoint | Re-running reaches the identical end state, with no duplicate interaction, command, timer, attempt, version or post | A lost acknowledgement is resolved by exact lookup |
| Pre-acceptance failure | Any of steps 5–9 blocks | A typed owner outcome is recorded, every preparation and lease is aborted or settled, and the permit is kept | The membership lease is never acquired |
| Output authority | Decision Open, missing, malformed or unavailable | `ProviderInvocationNotAuthorized` plus NotInvoked Budget and `CancelNoInvocation` capacity dispositions; the Provider lease settles | No Provider call |
| Closing races | Closing wins before a WorkflowStart, Membership, Provider-capacity or ProposalMutation commit | Reservations are cancelled; `StartSuppressedByDeletion`, `CancelNoInvocation`, or discarded output is recorded; committed work only settles | A stale worker's later acquire or commit is rejected |
| Retry or divergence | Same permit, outbox or lease, with the same or a changed payload | The same payload is a no-op; a changed payload is a conflict | A lagging projection never erases a permit |
| Unauthorized or cross-tenant | The caller fails pre-creation gates | Identical not-found before any permit, interaction or workflow | No tenant-revealing status, timing or dependency call |
| Plaintext in workflow state | Any non-id string in a workflow or activity contract | Rejected before persistence | Typed validation failure |

</frozen-after-approval>

## Code Map

Architecture references: `ARCHITECTURE-SPINE.md`, at the following lines:
- AD-2 directory and leases: L235
- AD-5 Closing: L314
- AD-7 permit and FR-8 order: L332–336
- AD-13 sequence and Provider branch: L409–420
- AD-18: L476
- AD-27: L560
- AD-29 identity: L574
- AD-30 principals: L586–591
- Sequence diagram: L672–861
- `ConversationAgentState` class: L1132

Code:
- `src/Hexalith.Agents/TenantProviderEnablement/*`, `TrustedEnvelopeReplay/*` -- templates for a new aggregate: `[EventStoreDomain]`, `ExpectedRevision` rejecting `StaleRevision`, exact retry as `NoOp`, divergent retry rejected, `[JsonInclude] private set` state.
- `src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs` and `AgentInteractionState.cs` -- add Handle/Apply only. Keep `InteractionRequested` (which carries a plaintext prompt), `SafetyFailed` and the `attempt-{id}` ids as legacy debt.
- `src/Hexalith.Agents.Contracts/AgentInteraction/{Commands,Events}` -- positional records implementing `IEventPayload` / `IRejectionEvent`. Enums use `JsonStringEnumConverter` with `Unknown = 0`. There are no polymorphic attributes. `AgentInteractionSnapshot` already freezes `ResponseMode`.
- `src/Hexalith.Agents.Contracts/Security/AgentsSecurityIdentity.cs` -- the canonical `Derive(purpose, …)` to reuse for every new id.
- `Hexalith.EventStore.Contracts.Security.ProtectedContent` / `ProtectionTarget` -- present in package 3.125.1. Do not re-implement the protection engine or the EventStore directory-epoch types.
- `src/Hexalith.Agents.Server/Application/AgentInteractions/*Orchestrator.cs` -- the intent, dispatch and strip-reserved-keys pattern. These classes have no runtime caller; do not rewire them.
- `src/Hexalith.Agents.Server/Ports/{IAgentCommandDispatcher,EventStoreAgentCommandDispatcher,IAgentDependencyAvailability,Deferred*}.cs` -- closed defaults. Live admission throws `EXT-HOST-1`.
- `src/Hexalith.Agents.EventStore/{AgentsEventStoreServiceCollectionExtensions,ProviderGovernanceIdempotencyIntentAdapter,AgentsTrustedCommandExtensionPolicy}.cs` -- each new command needs an idempotency adapter, and any reserved extension key needs a claim.
- `src/Hexalith.Agents.Server/Composition/AgentDomainHostComposition.cs` -- register closed defaults only.
- `references/Hexalith.Memories/src/Hexalith.Memories.Server/{Workflows,Hosting}` and `tests/.../Workflows` -- examples of Dapr Workflow structure, `AddDaprWorkflow`, and testing with an NSubstitute `WorkflowContext`.
- `references/Hexalith.Builds/Props/Directory.Packages.props` -- `Dapr.Workflow` 1.18.10 (not 1.18.7). Use a bare `PackageReference` in the Server csproj only.
- `test/Hexalith.Agents.Server.Tests/RuntimeOwnershipConformanceTests.cs`, `TraceabilityManifestTests.cs` and `Conformance/traceability-manifest.json` -- these currently forbid Dapr, `Application/Workflows/*.cs`, and Workflow-derived types.
- `eng/verify-story-5.4.ps1` -- template for the Local/Live verifier.

## Tasks & Acceptance

**Execution:**
- [ ] `src/Hexalith.Agents.Contracts/ConversationAgentState/**`, plus `Security/ConversationAgentStateIdentity.cs` -- add the following, keyed by (TenantId, ConversationId) with domain `conversation-agent-state`:
  - `RegisterInteractionPermit` → `InteractionCreationPermitted`. The event carries the permit fields, `PermitHighWater`, `OutboxId`, the directory epoch, and `ProtectedContent` Request.
  - `AcknowledgeInteractionCreation`.
  - `AcquireConversationEffectLease`, `CommitConversationEffect`, `SettleConversationEffectLease` and `CancelConversationEffectLease`. Lease kinds: WorkflowStart, ApproverResolution, ContextRead, ConversationMembership, ProviderInvocation, ProposalMutation, ConversationPosting.
  - `EnterConversationDeletionClosing`.
  - Typed rejections.
  - Purpose-tagged ids `interaction-permit`, `interaction-creation-outbox`, `interaction-workflow-start-outbox`, `interaction-directory-ack-outbox` and `conversation-effect-lease`.
  -- the directory contract.
- [ ] `src/Hexalith.Agents/ConversationAgentState/*` -- add the aggregate, its state, and an internal twin policy. A permit whose `ProtectionTarget` tenant or interaction differs from the permit rejects. Closing appends cancellations for every Reserved lease in the same decision, marks committed leases must-settle, and rejects any later permit, acquire or commit. -- serializes acceptance against deletion.
- [ ] `src/Hexalith.Agents.Contracts/AgentInteraction/**`, `src/Hexalith.Agents/AgentInteraction/*` -- add:
  - `CreatePermittedAgentInteraction`, whose first event carries the WorkflowStart and directory-acknowledgement outbox ids and rejects a legacy stream or a changed fingerprint.
  - The phase events `RateAdmissionAuthorized`, `OpenInteractionLeaseAuthorized`, `BudgetReservationAuthorized`, `CapacityAdmissionAuthorized` / `CapacityAdmissionRecorded`, `AgentCallAccepted`, `ProviderInvocationAuthorized` / `ProviderInvocationNotAuthorized`, `BudgetDispositionDecided`, `PreProviderCapacityDispositionDecided`, `GenerationFailed`, `GeneratedVersionRecorded`, `StartSuppressedByDeletion`, and `InteractionPreAcceptanceFailed`.
  - Each lease-bound command carries its exact `LeaseId` and commit revision.
  - `AttemptId`, `ReservationId`, `AdmissionId`, `ProposalVersionId` and `MessageId` follow the AD-29 formulas.
  -- business truth for the sequence.
- [ ] `src/Hexalith.Agents.Contracts/AgentInteraction/ProtectedContentReference.cs` -- add the (TenantId, AgentInteractionId, EventRevision, ContentDigest) reference. -- the only content handle allowed in workflow state.
- [ ] `src/Hexalith.Agents.Server/Application/AgentInteractions/AutomaticInteractionIntakeOrchestrator.cs` and `InteractionOutboxRecoveryOrchestrator.cs`, plus Ports for `IInteractionDirectoryReadiness`, `IConversationAgentStateReader`, `IAgentInteractionStateReader` and `IInteractionWorkflowScheduler` with closed defaults:
  - The intake runs the gates, then permit → creation → acknowledgement → acquire/commit WorkflowStart → schedule the exact instance.
  - Recovery re-drives pending outboxes, leases and starts from EventStore state. It is callable by the platform host and is not a hosted service.
  -- the durable start.
- [ ] `src/Hexalith.Agents.Server/Application/Workflows/AutomaticInteractionWorkflow.cs`, `Application/Activities/*`, `Application/Workflows/WorkflowExecutionState*.cs`, and `Composition/AgentInteractionWorkflowServiceCollectionExtensions.cs` (`AddAgentsInteractionWorkflow`, not called by default). Add `Dapr.Workflow` to the Server csproj.
  - Activities: settle WorkflowStart at the first checkpoint; steps 5–10 (step 6 only in Confirmation mode); acceptance; the Provider branch; ProposalMutation or GenerationFailed; and a posting lease in Automatic mode.
  - Confirmation mode ends after the version is recorded, because Epic 7 owns the rest.
  - Activities call deterministic step-effect ports, whose defaults map to the existing `Deferred*` ports.
  -- the single durable owner.
- [ ] `src/Hexalith.Agents.EventStore/*` -- add idempotency adapters and extension-policy claims for the new commands. -- gateway dispatchability.
- [ ] `test/Hexalith.Agents.Server.Tests/RuntimeOwnershipConformanceTests.cs`, `TraceabilityManifestTests.cs`, `Conformance/traceability-manifest.json`:
  - Allow `Dapr.Workflow` only in Server.
  - Allow exactly one `Workflow<,>` subtype, `AutomaticInteractionWorkflow`.
  - Keep the bans on hosted services, Agent Framework and `Tools`.
  - AD-18 stays a blocker, with its evidence text updated.
  -- deliberate guard revision.
- [ ] `test/Hexalith.Agents.Tests/*` and `test/Hexalith.Agents.Server.Tests/*` -- add the epics TestOrArtifact and NegativeEvidence classes that are in scope, including the I/O matrix, the Closing races, and a Level-2 restart harness that replays dispatched envelopes through `Handle`/`Apply` with fault injection. -- proof.
- [ ] `eng/verify-story-6.1.ps1` -- Local mode builds the five test projects with zero warnings, runs every test without skips, and requires the named story classes. Live mode fails closed, listing the unavailable EXT-*/OD-* records. -- one verification command.

**Acceptance Criteria:**
- Given the default host, when an Agent Call is submitted, then it fails closed with the named unavailable record before any permit, interaction or workflow exists.
- Given the full suite, when it runs in source and package modes, then all existing and new tests pass with no reduction in the minimum test counts.
- Given any workflow or activity contract type, when the AD-27 reflection sweep runs, then every member is an id, revision, digest, safe enum or `ProtectedContentReference`.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

Partial-host evidence model: Level 2 is deterministic replay through `Handle`/`Apply`, plus NSubstitute `WorkflowContext` ordering and fault injection. Live Dapr (Level 4) and production-like restart, frozen-cohort and RTO evidence (Level 5) stay `Blocked`. These are named in the verifier output and are never claimed.

Lease idempotency: `LeaseId = H(conversation-effect-lease, Tenant, Conversation, Interaction, Kind, OperationFamily, OperationIdentity)`. Acquiring the same id with identical fields is a no-op. Any field change is `DivergentDuplicate`. Committing a non-Reserved lease returns the recorded commit revision, or rejects if the lease was cancelled.

## Verification

**Commands:**
- `pwsh ./eng/verify-story-6.1.ps1 -Mode Local` -- expected: exit 0, all story classes pass, zero warnings.
- `pwsh ./eng/verify-story-6.1.ps1 -Mode Live` -- expected: non-zero exit naming EXT-HOST-1, EXT-PROTECTION-1, EXT-TOPOLOGY-1, OD-DAPR-SECURITY-1 and the other unavailable records.
- `pwsh ./eng/verify-story.ps1 -Story 5.1` -- expected: source and package lanes are green and the minimum counts are met.
