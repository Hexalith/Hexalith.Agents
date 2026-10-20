---
title: '6.1 Start And Recover An Automatic Interaction'
type: 'feature'
created: '2026-10-19'
status: 'done'
baseline_commit: '72d7b65bba7ccdb534d5e1c5f0be37573bcc848a'
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
- [x] `src/Hexalith.Agents.Contracts/ConversationAgentState/**`, plus `Security/ConversationAgentStateIdentity.cs` -- add the following, keyed by (TenantId, ConversationId) with domain `conversation-agent-state`:
  - `RegisterInteractionPermit` → `InteractionCreationPermitted`. The event carries the permit fields, `PermitHighWater`, `OutboxId`, the directory epoch, and `ProtectedContent` Request.
  - `AcknowledgeInteractionCreation`.
  - `AcquireConversationEffectLease`, `CommitConversationEffect`, `SettleConversationEffectLease` and `CancelConversationEffectLease`. Lease kinds: WorkflowStart, ApproverResolution, ContextRead, ConversationMembership, ProviderInvocation, ProposalMutation, ConversationPosting.
  - `EnterConversationDeletionClosing`.
  - Typed rejections.
  - Purpose-tagged ids `interaction-permit`, `interaction-creation-outbox`, `interaction-workflow-start-outbox`, `interaction-directory-ack-outbox` and `conversation-effect-lease`.
  -- the directory contract.
- [x] `src/Hexalith.Agents/ConversationAgentState/*` -- add the aggregate, its state, and an internal twin policy. A permit whose `ProtectionTarget` tenant or interaction differs from the permit rejects. Closing appends cancellations for every Reserved lease in the same decision, marks committed leases must-settle, and rejects any later permit, acquire or commit. -- serializes acceptance against deletion.
- [x] `src/Hexalith.Agents.Contracts/AgentInteraction/**`, `src/Hexalith.Agents/AgentInteraction/*` -- add:
  - `CreatePermittedAgentInteraction`, whose first event carries the WorkflowStart and directory-acknowledgement outbox ids and rejects a legacy stream or a changed fingerprint.
  - The phase events `RateAdmissionAuthorized`, `OpenInteractionLeaseAuthorized`, `BudgetReservationAuthorized`, `CapacityAdmissionAuthorized` / `CapacityAdmissionRecorded`, `AgentCallAccepted`, `ProviderInvocationAuthorized` / `ProviderInvocationNotAuthorized`, `BudgetDispositionDecided`, `PreProviderCapacityDispositionDecided`, `GenerationFailed`, `GeneratedVersionRecorded`, `StartSuppressedByDeletion`, and `InteractionPreAcceptanceFailed`.
  - Each lease-bound command carries its exact `LeaseId` and commit revision.
  - `AttemptId`, `ReservationId`, `AdmissionId`, `ProposalVersionId` and `MessageId` follow the AD-29 formulas.
  -- business truth for the sequence.
- [x] `src/Hexalith.Agents.Contracts/AgentInteraction/ProtectedContentReference.cs` -- add the (TenantId, AgentInteractionId, EventRevision, ContentDigest) reference. -- the only content handle allowed in workflow state.
- [x] `src/Hexalith.Agents.Server/Application/AgentInteractions/AutomaticInteractionIntakeOrchestrator.cs` and `InteractionOutboxRecoveryOrchestrator.cs`, plus Ports for `IInteractionDirectoryReadiness`, `IConversationAgentStateReader`, `IAgentInteractionStateReader` and `IInteractionWorkflowScheduler` with closed defaults:
  - The intake runs the gates, then permit → creation → acknowledgement → acquire/commit WorkflowStart → schedule the exact instance.
  - Recovery re-drives pending outboxes, leases and starts from EventStore state. It is callable by the platform host and is not a hosted service.
  -- the durable start.
- [x] `src/Hexalith.Agents.Server/Application/Workflows/AutomaticInteractionWorkflow.cs`, `Application/Activities/*`, `Application/Workflows/WorkflowExecutionState*.cs`, and `Composition/AgentInteractionWorkflowServiceCollectionExtensions.cs` (`AddAgentsInteractionWorkflow`, not called by default). Add `Dapr.Workflow` to the Server csproj.
  - Activities: settle WorkflowStart at the first checkpoint; steps 5–10 (step 6 only in Confirmation mode); acceptance; the Provider branch; ProposalMutation or GenerationFailed; and a posting lease in Automatic mode.
  - Confirmation mode ends after the version is recorded, because Epic 7 owns the rest.
  - Activities call deterministic step-effect ports, whose defaults map to the existing `Deferred*` ports.
  -- the single durable owner.
- [x] `src/Hexalith.Agents.EventStore/*` -- add idempotency adapters and extension-policy claims for the new commands. -- gateway dispatchability.
- [x] `test/Hexalith.Agents.Server.Tests/RuntimeOwnershipConformanceTests.cs`, `TraceabilityManifestTests.cs`, `Conformance/traceability-manifest.json`:
  - Allow `Dapr.Workflow` only in Server.
  - Allow exactly one `Workflow<,>` subtype, `AutomaticInteractionWorkflow`.
  - Keep the bans on hosted services, Agent Framework and `Tools`.
  - AD-18 stays a blocker, with its evidence text updated.
  -- deliberate guard revision.
- [x] `test/Hexalith.Agents.Tests/*` and `test/Hexalith.Agents.Server.Tests/*` -- add the epics TestOrArtifact and NegativeEvidence classes that are in scope, including the I/O matrix, the Closing races, and a Level-2 restart harness that replays dispatched envelopes through `Handle`/`Apply` with fault injection. -- proof.
- [x] `eng/verify-story-6.1.ps1` -- Local mode builds the five test projects with zero warnings, runs every test without skips, and requires the named story classes. Live mode fails closed, listing the unavailable EXT-*/OD-* records. -- one verification command.

**Acceptance Criteria:**
- Given the default host, when an Agent Call is submitted, then it fails closed with the named unavailable record before any permit, interaction or workflow exists.
- Given the full suite, when it runs in source and package modes, then all existing and new tests pass with no reduction in the minimum test counts.
- Given any workflow or activity contract type, when the AD-27 reflection sweep runs, then every member is an id, revision, digest, safe enum or `ProtectedContentReference`.

## Implementation Notes

- Added the `conversation-agent-state` owner with protected creation permits, reciprocal acknowledgements, deterministic leases, same-owner commit revisions, and atomic Closing cancellation/must-settle facts. Closing also works before the first permit. A separate admitted deletion assertion grants Closing without granting interaction effects.
- Added the permit-created interaction path and additive automatic phase facts. Legacy streams cannot be upgraded, and legacy handlers cannot advance a permit-created interaction. Request/output digests retain their DigestKey version; content stays under the explicit interaction protection target.
- Intake and platform-callable outbox recovery use authoritative owner reads. The default readiness, readers, effects and scheduler remain closed. `AddAgentsInteractionWorkflow` is an explicit opt-in guarded by `OD-DAPR-SECURITY-1`; the default host does not call it.
- The sole Dapr workflow uses one content-free checkpoint activity. Each checkpoint re-reads owner state and dispatches at most one trusted command. Provider authorization phase-pins the exact Product mapping; output-safety failures use that recorded mapping. Raw activity exceptions are replaced by a safe retry code before Dapr can persist failure details.
- Automatic mode records `ConversationPostingSuppressedByDeletion` when Closing wins the posting commit, including after a winning ProposalMutation commit. This terminal no-effect fact preserves the successful generated version and carries the immutable Closing request/revision; committed posting can only finish and settle. Adapter-thrown cancellation exceptions use the same content-free retry exception as other dependency failures.
- Review fixes preserve public signatures while adding revision-scoped submission identities inside the existing AD-29 tuple, bounded asynchronous owner observation, exact intake-intent checks, and explicit recovery-required reporting for failed runtime owners. Accepted-before-capacity Closing and failed lookups after mutation commit now terminate with every exact lease resolved.
- The Level-2 harness replays dispatched public envelopes through the real `Handle`/`Apply` seams and injects faults before and after every command append. It also covers lost scheduling acknowledgement, FR-8 order, lease/Closing races, malformed authority, failed/discarded output, and an AD-27 reflection/poison-input sweep.
- Goal B migration/repair, User action-intent outboxes, deletion Effective/purge, real adapters, and Level-4/5 qualification remain deferred exactly as approved. No AppHost exists in this domain repository, so no Aspire baseline could be run here.

## Spec Change Log

## Review Triage Log

| Finding | Verdict | Evidence | Route |
| --- | --- | --- | --- |
| BH-1 Closing before capacity | high | After acceptance, Closing selects a pre-Provider disposition, but its precondition requires CapacityAdmissionRecorded. Both immediate post-acceptance checkpoints reject indefinitely. | patch (group C1) |
| BH-2 failed output lookup after mutation commit | high | GenerationFailed binds the Provider lease; LeaseResult has no negative result for an already committed ProposalMutation, and terminal execution returns with that lease outstanding. | patch (group C2) |
| BH-3 divergent intake retry | high | SendAsync returns a submission receipt, not domain acceptance. Recovery checks only the original directory/interaction pair and never compares them to the newly submitted request. A rejected changed digest can therefore return Scheduled. | patch (group C3) |
| BH-4 cached stale revision | high | EventStore AggregateActor checks message-keyed terminal duplicates and completes domain rejections terminally; MessageId and gateway IdempotencyKey both remain unchanged when ExpectedRevision changes. A concurrent append can strand retries. | patch (group C4) |
| BH-5 uncreated permit after Closing | false | Closing is itself a durable owner fact. Recovery returns before creation whenever this fact exists, so the retained permit is deterministically suppressed on every replay without another dispatch. The intent explicitly keeps permits and defers deletion Effective/purge; an extra negative creation acknowledgement has no demonstrated consumer in this scope. | reject |
| BH-6 failed runtime reported Scheduled | medium | The scheduler returns on Exists without inspecting RuntimeStatus, including Failed/Terminated. Such an instance does not establish a running owner for unfinished business state. Preserve its exact identity and report a safe explicit recovery-required failure rather than a false successful schedule. | patch (group C5) |
| BH-7 unconsumable permit fields | medium | Registration omits the response-mode and nonblank identity guards already enforced by permitted creation. An accepted permit with these fields cannot be consumed. Apply the existing creation prerequisites at registration. | patch (group C6) |
| BH-8 altered reservation status retry | medium | Existing duplicate comparison assigns caller Status before checking Reserved, so an altered or undefined status can be accepted as NoOp. Normalize to the original Reserved shape and validate the submitted shape before duplicate comparison. | patch (group C7) |
| BH-9 misleading terminal public status | medium | ApplyAutomaticFact leaves pre-acceptance terminal failures Requested and unsuccessful posting Generated; AgentInteractionAuditInspection.ToStatusView returns Status directly. Existing Blocked/PostingFailed statuses can represent these failures without changing the phase-pinned output-safety mapping. | patch (group C8) |
| BH-10 fixture lookup count versus transport | false | IAutomaticInteractionStepEffects explicitly defines immutable-id execution or exact lookup. Multiple generation seam calls are therefore permitted lookups; the fixture models the external idempotent result by immutable identity. No live transport is implemented or claimed, and the approved intent explicitly defers real Provider/posting adapters. Three seam calls do not establish three transport executions. | reject |
| BH-11 asynchronous owner visibility | medium | Recovery counts immediate read/dispatch iterations, so an accepted asynchronous append can exhaust the loop before becoming observable. Add bounded cancellation-aware observation between unobserved submissions and a delayed-owner fixture. | patch (group C9) |
| EC-1 Closing before capacity | high | Same verified transition and precondition as BH-1; both newly accepted checkpoints can reach this rejection. | patch (group C1) |
| EC-2 cached stale revision | high | Same message-keyed terminal rejection path verified for BH-4; both source and packaged EventStore use this transport identity. | patch (group C4) |
| VG-1 canonicalization coverage | medium | Pre-verified: registration tests enumerate command names while the Handle/Apply harness bypasses the registered canonical-intent adapter. ExpectedRevision/Fact.Revision normalization and changed business intent lack executable assertions. | patch (group C10) |
| VG-2 authority policy coverage | medium | Pre-verified: existing Claims/Accepts matrices cover setup/governance, not the new workflow/deletion assertions. The direct fixture does not exercise gateway claim ownership or principal/proof admission. | patch (group C11) |
| VG-3 legacy posting regression coverage | medium | Pre-verified: legacy-to-permitted rejection is covered, but permitted Generated/Closing-suppressed streams are never submitted to legacy PostAgentResponse. Removing its new guard would bypass posting leases. | patch (group C12) |


All twelve surviving review groups were patched. Final Local verification executed the covering methods in both lanes (49 dedicated review regression cases per lane); machine-readable evidence is retained in `/tmp/agents-story-6-1-final-validation/review-patch-verification.json`. No intent/spec loopback or deferred finding was required.

## Design Notes

Partial-host evidence model: Level 2 is deterministic replay through `Handle`/`Apply`, plus NSubstitute `WorkflowContext` ordering and fault injection. Live Dapr (Level 4) and production-like restart, frozen-cohort and RTO evidence (Level 5) stay `Blocked`. These are named in the verifier output and are never claimed.

Lease idempotency: `LeaseId = H(conversation-effect-lease, Tenant, Conversation, Interaction, Kind, OperationFamily, OperationIdentity)`. Acquiring the same id with identical fields is a no-op. Any field change is `DivergentDuplicate`. Committing a non-Reserved lease returns the recorded commit revision, or rejects if the lease was cancelled.

## Verification

The following mapping is Level 2 only. Executed xUnit XML is retained in `/tmp/agents-story-6-1/{source,package}/Hexalith.Agents.Tests.xml` (domain suites) and `Hexalith.Agents.Server.Tests.xml` (Server suites). The verifier requires named suites to execute with no skips.

| Frozen I/O row | Passing-test evidence to inspect in retained XML |
| --- | --- |
| Happy path | `AutomaticInteractionWorkflowTests.SingleOwnerCallsActivitiesInFr8OrderAndConfirmationStopsAfterVersion`; `Fr8OrderedAcceptanceApproverResolutionAndMembershipLeaseTests.RecordedAcceptanceFollowsEveryOrderedPreparationAndMembershipResult`; `AutomaticInteractionCheckpointAgreementTests.EveryCheckpointReturnsTheRecordedOutcomeAndDispatchesAtMostOneCommand` |
| Restart | `AutomaticInteractionRestartIntegrationTests.CrashBeforeOrAfterEveryCommandCheckpointConvergesToTheIdenticalPersistedEndState`; `PermitAndCreationOutboxLostAckTests.ExactRetryRecoversTheOriginalPermitCreationAndAcknowledgement`; `WorkflowStartOutboxTests.LostScheduleAcknowledgementDoesNotCreateASecondInstance`; `DuplicateCommandTimerAndVersionRecoveryTests.TerminalReplaysAddNoCommandVersionAttemptOrPost` |
| Pre-acceptance failure | `Fr8RateApproverContextBudgetAndSafetyFailuresCreateNoMembershipEffectTests.TypedFailureSettlesAndNeverAcquiresMembership` (rate, open, Approver, context, Budget and safety variants); `AutomaticInteractionTwinPolicyTests.EvaluateAndDecideShareTheSameDecisionAndOrderedDispositionSequence` |
| Output authority | `ProviderOutputSafetyDecisionPhasePinTests.MissingAuthorityRecordsNotInvokedDispositionsAndSettlesProviderWithoutCallingIt`; `ProviderCannotStartWithoutPhasePinnedOutputSafetyDecisionTests.MalformedAuthorityNeverInvokesProvider`; `OutputSafetySuccessorCannotRelabelAuthorizedAttemptTests.ChangedAuthorityOnTheSameBranchIsADivergentDuplicate`; `InitialOutputSafetyUsesPhasePinnedStatusTests.SafetyFailureUsesOnlyTheExactProductSelectedStatusAndRetainsNoContent` |
| Closing races | `ConversationEffectLeaseCommitRaceTests.ClosingWinsOrCommitWinsWithoutRevokingCommittedWork` (all seven kinds); `WorkflowStartImmediatelyBeforeAndAfterLeaseCommitAndBarrierClosingTests.CommitAndClosingHaveOneWinnerEvenWhenTheAppendAcknowledgementIsLost`; `MembershipCommitImmediatelyBeforeAndAfterClosingEffectiveTests.MembershipReservationLosesOrCommittedMembershipSettlesWithoutLateAcceptance`; `PreProviderCapacityCannotRemainQueuedOrAdmittedAfterClosingTests.ClosingSettlesExistingCapacityAndNeverInvokesProvider`; `GeneratedOutputDiscardedWhenProposalMutationCommitLosesTests.OnlyTheWinningMutationCommitCanPersistOutputAfterClosing`; `StaleReservedWorkerCannotActAfterClosingTests.ExactAcquireAndCommitRetriesAreRejectedAfterCancellation`; `AutomaticPostingClosingRecoveryTests.PostingCommitAndClosingConvergeWithoutChangingTheGeneratedVersion`; `AutomaticPostingClosingRecoveryTests.AutomaticModeRecordsAWinningGeneratedMutationThenSuppressesPostingWithoutAcquiringANewLease`; `AutomaticPostingClosingRecoveryTests.RecordedPostSuppressionSurvivesLostAcknowledgementAndRestart`; `PostingSuppressionDecisionTests.ClosingBoundSuppressionIsIdempotentAndRejectsAnyLaterPostingResult`; `PostingSuppressionDecisionTests.SuppressionRequiresClosingEvidenceAndCannotCarryEffectAuthorityOrOutput` |
| Retry or divergence | `ConversationInteractionDirectoryAggregateTests.ExactRetryIsNoOpAndChangedProtectedIntentConflicts`; `InteractionPermitCreationOutboxTests.PermitCreatedFirstEventContainsTheSoleReciprocalOutboxesAndAd29Identities`; `StaleReservedWorkerCannotActAfterClosingTests.ChangedLeasePayloadIsAConflictAndAnExactReservationIsNoOp`; `ProjectionLagCannotErasePermitTests.RecoveryFindsThePermitWhenTheInteractionAndAcknowledgementAreStillAbsent` |
| Unauthorized or cross-tenant | `AutomaticInteractionIsolationTests.CrossTenantCallStartsNoWorkflow`; `AutomaticInteractionIsolationTests.FailedPreCreationGatesReturnTheSameNotFoundBeforeDependenciesOrOwners`; `CrossTenantProtectionKeyAliasSubstitutionTests.WrongProtectionTenantOrInteractionCannotCreatePermit` |
| Plaintext in workflow state | `WorkflowExecutionStateBoundaryTests.EveryWorkflowAndActivityContractContainsOnlyIdsRevisionsDigestsSafeEnumsAndProtectedReferences`; `WorkflowExecutionStateBoundaryTests.NonIdentityTextIsRejectedBeforeScheduling`; `WorkflowExecutionStateBoundaryTests.UnknownContentMembersAndCrossTenantReferencesCannotBeSilentlyIgnored`; `WorkflowActivityFailureSafetyTests.RawDependencyExceptionBecomesOnlyASafeRetryCode` |

**Commands:**
- `pwsh ./eng/verify-story-6.1.ps1 -Mode Local` -- expected: exit 0, all story classes pass, zero warnings.
- `pwsh ./eng/verify-story-6.1.ps1 -Mode Live` -- expected: non-zero exit naming EXT-HOST-1, EXT-PROTECTION-1, EXT-TOPOLOGY-1, OD-DAPR-SECURITY-1 and the other unavailable records.
- `pwsh ./eng/verify-story.ps1 -Story 5.1` -- expected: source and package lanes are green and the minimum counts are met.

**Executed local evidence (2026-10-20):**
- Final `pwsh ./eng/verify-story-6.1.ps1 -Mode Local -ArtifactsPath /tmp/agents-story-6-1-final-validation` exited 0. Source: 731 Contracts, 6 Client, 985 domain, 1,262 Server, 1,158 UI (4,142 total). Package: 731 Contracts, 6 Client, 985 domain, 1,224 Server, 1,158 UI (4,104 total). Every build reported zero warnings; every test passed with zero skips/errors/not-run. Logs, per-project xUnit XML, and `result.json` are retained in `/tmp/agents-story-6-1-final-validation/`.
- All eight frozen matrix rows and all twelve review patch groups matched passing methods in both lanes. Machine-readable evidence: `matrix-verification.json` and `review-patch-verification.json` in that final evidence directory.
- The original Closing/OCE regressions were reproduced before their fixes (five failures in eight cases). Red/green xUnit XML and logs remain in `/tmp/agents-story-6-1/review-fixes/`. The subsequent focused review run passed 50 domain cases and 190 Server cases; evidence remains in `/tmp/agents-story-6-1/review-round-2/`.
- Final `pwsh ./eng/verify-story-6.1.ps1 -Mode Live -ArtifactsPath /tmp/agents-story-6-1-final-validation` exited 1 as required. `live-result.json` and `live.log` name EXT-HOST-1, EXT-PROTECTION-1, EXT-TOPOLOGY-1, EXT-SECRETS-1, EXT-PARTIES-1, EXT-CONV-AI-1, EXT-PROVIDER-1, EXT-SAFETY-1, EXT-TOKEN-1, OD-DAPR-SECURITY-1, OD-INITIAL-OUTPUT-SAFETY-STATUS-1, and OD-RATE-CONCURRENCY-CONSUMPTION-1. External calls: zero; Level 4/5 remain Blocked.
- Final `pwsh ./eng/verify-story.ps1 -Story 5.1` exited 0 on the patched implementation, including shared package authority, source/package builds and tests, executable negative dependency-mode gates, exact inventory, and isolated consumers for all six packages. Log: `/tmp/agents-story-6-1-final-validation/verify-story-5.1.log`.
