---
title: Hexalith Agents Launch Readiness Register
status: active
created: 2026-08-02
updated: 2026-09-09
project: agents
authority: sprint-change-proposal-2026-09-09-prd-validation-follow-through.md
release_gate: RQ-1
---

# Launch Readiness Register

## Authority And Evaluation Rule

This register is the authoritative machine-testable inventory for Hexalith Agents callability and release qualification. Host configuration, UI state, narrative evidence, or a passing deterministic calculator fixture cannot substitute for a current register record.

`RQ-1` returns READY only when every required record for the evaluated `TenantScope` and `EnvironmentProfile` exists, is fresh, is `Pass`, meets its `RequiredEvidenceLevel`, references qualifying evidence, and uses the current configuration or measurement contract. A missing, stale, blocked, or insufficient record makes the result NOT READY. `RQ-1` is a release gate, not a development story.

## Normative Record Schema

| Field | Contract |
| --- | --- |
| `GateId` | Stable identifier from the minimum inventory; never reused for another control. |
| `TenantScope` | Exact tenant or explicitly authorized cohort to which the evidence applies. |
| `EnvironmentProfile` | Versioned environment profile, including `production-like` when used for Levels 4/5 or `RQ-1`. |
| `State` | `Pass`, `Block`, `InsufficientEvidence`, or `Stale`. |
| `Owner` | Named role or person accountable for the gate and evidence source. |
| `SourceVersion` | Immutable configuration, component, policy, fixture, or measurement-contract version observed. |
| `ObservedAt` | UTC instant at which the authoritative observation completed. |
| `ValidUntil` | UTC exclusive upper bound chosen by this gate's contract. No global implicit freshness duration exists. |
| `RequiredEvidenceLevel` | Minimum normative PRD Evidence Level required for the evaluated profile. |
| `EvidenceReference` | Immutable, access-controlled reference to the evidence manifest or result. |
| `ConfigurationOrMeasurementContract` | Versioned configuration values or metric definition, including authoritative timestamps, calculation, sample/window/cohort, late/missing-data, and invalidation rules where measured. |
| `BlockerCode` | Stable support-safe code; never raw content, secrets, exceptions, or Provider payloads. |

### Blocker Code Vocabulary

The following support-safe codes are the closed cross-gate vocabulary for V1. A gate-specific contract may name a narrower reason underneath one of these codes, but a consumer may not invent a peer code. An unknown code fails closed.

| `BlockerCode` | Meaning and use |
| --- | --- |
| `GateRecordMissing` | No record exists for a required gate, scope, and profile. |
| `EvidenceNotRecorded` | The gate has no accepted evidence observation yet. |
| `DependencyUncommitted` | A dependency needed to begin compatibility work has not reached `Committed`. |
| `DependencyNotAvailable` | A dependency consumed by the evaluated qualification profile has not reached `Available`, or its exact compatibility command does not pass. |
| `InsufficientEvidence` | Required samples, timestamps, live evidence, manifest fields, or evidence level are absent. |
| `UnretiredAssumption` | An unretired PRD `A-n` row or Architecture `ARCH-A-n` row blocks qualification; the blocker names the row, table, and evaluated index version. |
| `OpenDecision` | A deferred PRD decision whose status requires resolution before enablement has not landed. |
| `GateOutOfScope` | A demanded gate or evidence requirement is not derivable from the authoritative FR-28 input list and is void for `RQ-1` until reconciled. |
| `TriggerReviewOverdue` | A required FR-28 kill-switch trigger review was not recorded within one business day. |
| `PayloadProtectionUnavailable` | The host has not reported the production payload-protection engine required by FR-34 as available; this is also an additive `AgentLaunchReadinessBlocker`. |
| `ProhibitedCostControlPosture` | A recorded posture is `ReportingOnlyMonitoring` or `AcceptedLaunchRisk`; this is also an additive `AgentLaunchReadinessBlocker` and is never collapsed into a missing-posture code. |

`UnretiredAssumption`, `DependencyNotAvailable`, `OpenDecision`, and `InsufficientEvidence` are register vocabulary evaluated by `RQ-1`, not Agent aggregate members. `PayloadProtectionUnavailable` and `ProhibitedCostControlPosture` additionally cross the runtime readiness boundary because FR-30 and FR-34 require enablement to fail closed before a document/register lookup.

## State, Freshness, And Invalidation

At an evaluation instant `T`, a record can contribute `Pass` only when `ObservedAt <= T < ValidUntil`, all required fields are populated, the evidence satisfies `RequiredEvidenceLevel`, and `SourceVersion` plus `ConfigurationOrMeasurementContract` still match the deployed/evaluated system.

| Condition | Effective state | Required blocker behavior |
| --- | --- | --- |
| Authoritative control and evidence meet the current contract | `Pass` | `BlockerCode` is empty. |
| The control proves a launch-blocking failure | `Block` | A stable safe reason is required. |
| Required timestamps, samples, live evidence, manifest fields, or evidence level are absent | `InsufficientEvidence` | The missing evidence class is named safely. |
| `T >= ValidUntil`, source/configuration/policy/fixture version changed, or the evidence contract was superseded | `Stale` | Re-observation under the current source and contract is required. |
| No record exists for a required gate/scope/profile | implicit `Block` | Evaluator emits `GateRecordMissing`; absence is never degraded or passing. |

`ObservedAt` and `ValidUntil` are supplied by each gate's authoritative source. Wall-clock observations from different systems are never subtracted to produce browser interaction durations. A corrected evidence record is appended or republished with a new `SourceVersion` or evidence reference; previous evidence remains auditable.

### Record Authority And Supersession

The EventStore `LaunchReadinessGate` aggregate is the sole readiness-record writer for each logical key (`GateId`, `TenantScope`, `EnvironmentProfile`). Evidence producers, the platform host, UI, and qualification runners submit server-trusted commands; none can assign `State` or update the projection directly. Each accepted immutable observation has a deterministic `ObservationId = H(observation, GateId, TenantScope, EnvironmentProfile, SourceVersion, EvidenceReference, ObservedAt)` computed by the submitting orchestrator with the shared Agents identity canonicalizer (architecture AD-29) and recomputed by the aggregate, which rejects a mismatch; re-observation with an unchanged `SourceVersion` is legal and produces a new revision. EventStore optimistic concurrency serializes each observation at a stream revision exposed as `RegistryRevision`. `TenantScope` is the closed grammar `tenant:<TenantId>` or `platform`; each `GateId` declares a `ScopeKind` below, and evaluation for a tenant reads `platform` records for `Platform` gates and `tenant:<TenantId>` records for `Tenant` gates; no other cohort syntax exists in V1. `EnvironmentProfile` is `<name>@<ProfileVersion>`; a profile version change invalidates every record of the old profile. Each `GateId` binds an `AuthorizedProducer` role; a submission whose principal lacks it is rejected before append, and a `GateId` outside the minimum inventory is rejected at submission.

The `launch-readiness` projection selects the observation at the greatest committed stream revision, never the greatest `ObservedAt`. That observation supersedes every lower revision. If it is incomplete, invalid, stale, or blocking, evaluation fails closed and never falls back to an older `Pass`. `RQ-1` reads every required logical key from one projection checkpoint; if the checkpoint changes during evaluation, it retries against one new checkpoint or returns NOT READY. Duplicate `ObservationId` with the same payload is an idempotent no-op; a conflicting payload is rejected and audited.

## Minimum Gate Inventory

The following IDs and meanings are normative and closed for V1: implementations may not add, remove, merge away, or weaken entries; a stricter control is expressed as stricter evidence under an existing GateId.

| `GateId` | Governing contract and invalidation trigger |
| --- | --- |
| `LR-TOPOLOGY` | Platform-owned production-like composition, exact component versions, reset/seed/capture/failure-injection procedures, and no conditional skips. Invalidated by host, fixture, component, or procedure changes. |
| `LR-EVENTSTORE` | Live EventStore command/event/projection path, business-state durability, and replay correctness. Invalidated by EventStore target, state-store, schema, subscription, or projection changes. |
| `LR-TENANT-ACCESS` | Fresh tenant access and focused cross-tenant denial for every affected public/runtime path. Invalidated by authorization policy, tenant projection, identity, or role mapping changes. |
| `LR-PARTY-IDENTITY` | Unique active Agent Party identity plus current posting eligibility. Invalidated by Party state, identity contract, or Agent identity-link changes. |
| `LR-CONVERSATION-CONTEXT` | Fresh complete authorized Source Conversation read, no truncation/summary/windowing, and exact current budget validation. Invalidated by Conversations read contract, context policy, Provider/model selection, capability version, or tokenizer changes. |
| `LR-CONVERSATIONS-MEMBERSHIP-POSTING` | `EXT-CONV-AI-1` membership plus append idempotency, typed conflicts, attribution, and cross-tenant denial. Invalidated by Conversations target or compatibility-contract changes. |
| `LR-PROVIDER` | Public Provider readiness result, selected adapter, current pricing/capabilities/limits, deterministic attempt, safe failure, and usage evidence. Invalidated by Provider/Agent Framework adapter target, catalog entry, secret state, pricing, limits, or capability version changes. |
| `LR-TOKENIZER` | Selected Provider/model tokenizer measures the exact prepared request and blocks unsupported/missing measurement. Invalidated by Provider/model, tokenizer, template, serialization, or context-policy changes. |
| `LR-SAFETY` | Fresh versioned prompt/context and output safety decisions, always-blocked/restricted policy, no override, and no-weaker retry. Invalidated by policy or adapter target/configuration changes. |
| `LR-SECRETS` | Resolution, rotation, denial, and no-leak evidence through the platform host. Invalidated by secret reference, store, host, access policy, or rotation-procedure changes. |
| `LR-COST` | Current pricing, numeric monthly tenant budget, numeric per-call cap, 80% warning, 100% block, atomic reservation, reconciliation, and retry reuse. Invalidated by pricing, currency/unit, budget, cap, reservation policy, or Provider/model changes. |
| `LR-AUDIT-PROTECTION-DELETION` | Payload protection, authorized audit, retention/legal hold/export, cryptographic erasure/redaction, named projection purge, and safe tombstone evidence. Includes evidence that a `LegalHold` either propagates to (or has a confirmed accepted-risk waiver for) the posted copy of held content in Conversations, since AD-22 concedes posted Conversation Messages are otherwise governed only by Conversations' own independent retention (spine assumption ARCH-A-10). Invalidated by protection, retention, export, deletion, propagation-seam, or projection-inventory changes. |
| `LR-RECOVERY` | NFR-11 production-like recovery with EventStore RPO 0, no duplicate effects, all terminal decisions preserved, and processing restored within 15 minutes. Invalidated by workflow, EventStore, Provider attempt, timer, reservation, posting, host, or recovery-procedure changes. |
| `LR-CAPACITY-FAIRNESS` | NFR-12 versioned numeric tenant/system concurrency and queue limits, queue-or-reject behavior before Provider invocation, fairness policy, and cost-cap coexistence. Invalidated by profile, scheduler/admission, Provider, or cost-policy changes. |
| `LR-UI-CONFORMANCE` | NFR-13 WCAG 2.2 AA behavior, whole-string English/French key parity, FrontComposer/Fluent V5 use, and restrictive-viewport blocking for high-impact actions. Invalidated by UI component, localization, route, interaction, or supported-viewport changes. |
| `LR-RUNTIME-PERFORMANCE` | NFR-9 accepted-call/post/proposal, approval/post, and fast pre-Provider rejection percentile gates using at least 30 production-like executions each. Invalidated by runtime path, profile, timestamp source, percentile, cohort, or threshold changes. |
| `LR-UI-PERFORMANCE` | NFR-14 browser-monotonic page usability, authoritative pending acknowledgement, and terminal render/live-region announcement gates using at least 30 production-like executions each. Invalidated by UI path, browser profile, instrumentation seam, threshold, percentile, or sample rules. |
| `LR-PRODUCT-METRICS` | Versioned pre-enablement SM-1, SM-4, SM-5, and SM-6 calculation and real qualification attainment; SM-5 requires exactly 100% audit completeness. SM-2, SM-3, and SM-7 are launch-health metrics and never contribute to `RQ-1`. Deterministic fixtures prove formulas only. Invalidated by source event, formula, threshold, cohort, late-data, or insufficiency rules. |

### Gate Scope Kinds And Authorized Producers

| `GateId` | `ScopeKind` | `AuthorizedProducer` (architecture AD-30 principal kind) |
| --- | --- | --- |
| `LR-TOPOLOGY` | Platform | `Platform` |
| `LR-EVENTSTORE` | Platform | `Platform` |
| `LR-TENANT-ACCESS` | Tenant | `User` holding Release Operator, or `Platform` |
| `LR-PARTY-IDENTITY` | Tenant | `User` holding Release Operator, or `Platform` |
| `LR-CONVERSATION-CONTEXT` | Tenant | `User` holding Release Operator, or `Platform` |
| `LR-CONVERSATIONS-MEMBERSHIP-POSTING` | Tenant | `User` holding Release Operator, or `Platform` |
| `LR-PROVIDER` | Tenant | `User` holding Release Operator, or `Platform` |
| `LR-TOKENIZER` | Tenant | `User` holding Release Operator, or `Platform` |
| `LR-SAFETY` | Tenant | `User` holding Release Operator, or `Platform` |
| `LR-SECRETS` | Platform | `Platform` |
| `LR-COST` | Tenant | `User` holding Release Operator, or `Platform` |
| `LR-AUDIT-PROTECTION-DELETION` | Tenant | `User` holding Release Operator, or `Platform` |
| `LR-RECOVERY` | Platform | `Platform` |
| `LR-CAPACITY-FAIRNESS` | Platform | `Platform` |
| `LR-UI-CONFORMANCE` | Platform | `Platform` |
| `LR-RUNTIME-PERFORMANCE` | Platform | `Platform` |
| `LR-UI-PERFORMANCE` | Platform | `Platform` |
| `LR-PRODUCT-METRICS` | Tenant | `User` holding Release Operator, or `Platform` |

The `Platform` assignment of `LR-UI-CONFORMANCE`, `LR-RUNTIME-PERFORMANCE`, and `LR-UI-PERFORMANCE` is an architecture assumption (spine AD-17, 2026-09-09) confirmed or retuned by Product and the Release PM before enablement.

### Gate Sets And Non-Circular Evaluation

The `QualificationExecutionGateSet` contains `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, `LR-PARTY-IDENTITY`, `LR-CONVERSATION-CONTEXT`, `LR-CONVERSATIONS-MEMBERSHIP-POSTING`, `LR-PROVIDER`, `LR-TOKENIZER`, `LR-SAFETY`, `LR-SECRETS`, `LR-COST`, `LR-AUDIT-PROTECTION-DELETION`, and `LR-CAPACITY-FAIRNESS`. It permits only explicitly authorized controlled production-like executions used to collect evidence, and only when every external seam executed by that operation is `Available` and its compatibility command passes against the deployed target. `Committed` permits development/contract work but never execution of the seam. This set does not authorize production enablement.

Every controlled execution re-evaluates the applicable operation subset from `OperationGateMatrixVersion = 2` (version 1 rows are unchanged; version 2 appends the rows marked `v2`). Every public command and every workflow activity declares exactly one operation family in its contract; a command with no declared family fails contract tests. This matrix is the single contract consumed by API, BFF, UI, workflow, and the `launch-readiness` projection. A missing operation family, unknown matrix version, missing record, or GateId outside the minimum inventory blocks with a safe code; consumers may not maintain local subsets.

| Operation family | Required GateIds |
| --- | --- |
| `ProviderCatalogMutation` | `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, `LR-SECRETS`, `LR-COST`, `LR-AUDIT-PROTECTION-DELETION` |
| `AgentSetupMutation` | `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, `LR-PARTY-IDENTITY`, `LR-AUDIT-PROTECTION-DELETION` |
| `AgentActivation` | `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, `LR-PARTY-IDENTITY`, `LR-PROVIDER`, `LR-TOKENIZER`, `LR-SAFETY`, `LR-SECRETS`, `LR-COST`, `LR-AUDIT-PROTECTION-DELETION`, `LR-CAPACITY-FAIRNESS` |
| `AgentCallAcceptance` | `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, `LR-PARTY-IDENTITY`, `LR-CONVERSATION-CONTEXT`, `LR-PROVIDER`, `LR-TOKENIZER`, `LR-SAFETY`, `LR-SECRETS`, `LR-COST`, `LR-AUDIT-PROTECTION-DELETION`, `LR-CAPACITY-FAIRNESS` |
| `ProviderInvocation` | `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, `LR-PARTY-IDENTITY`, `LR-CONVERSATION-CONTEXT`, `LR-PROVIDER`, `LR-TOKENIZER`, `LR-SAFETY`, `LR-SECRETS`, `LR-COST`, `LR-AUDIT-PROTECTION-DELETION`, `LR-CAPACITY-FAIRNESS` |
| `ConversationPosting` | `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, `LR-PARTY-IDENTITY`, `LR-CONVERSATIONS-MEMBERSHIP-POSTING`, `LR-SAFETY`, `LR-AUDIT-PROTECTION-DELETION` |
| `ProposalResolution` | `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, `LR-AUDIT-PROTECTION-DELETION` |
| `PolicyPublication` | `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, `LR-AUDIT-PROTECTION-DELETION` |
| `TenantBudgetUpdate` | `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, `LR-COST`, `LR-AUDIT-PROTECTION-DELETION` |
| `LegalHold` | `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, `LR-AUDIT-PROTECTION-DELETION` |
| `ExportRequest` | `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, `LR-SECRETS`, `LR-AUDIT-PROTECTION-DELETION` |
| `DeletionRequest` | `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, `LR-SECRETS`, `LR-AUDIT-PROTECTION-DELETION` |
| `ReadinessInspection` | `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS` |
| `ProposalEdit` (v2) | `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, `LR-AUDIT-PROTECTION-DELETION` |
| `ProposalRegeneration` (v2) | same set as `ProviderInvocation` |
| `SystemTimer` (v2) | `LR-EVENTSTORE`; used by expiry, retention-due, reservation-deadline, and queue-expiry activities, which need no human authorization but must not append when EventStore readiness is blocked |
| `LegalHoldRelease` (v2) | same set as `LegalHold` |
| `ExportDownload` (v2) | same set as `ExportRequest` |
| `AuditInspection` (v2) | `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, `LR-AUDIT-PROTECTION-DELETION`; covers posted-provenance and compliance inspection |
| `TenantKillSwitch` (v2) | `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, `LR-AUDIT-PROTECTION-DELETION` |
| `ReadinessObservation` (v2) | `LR-EVENTSTORE`; the family every readiness-evidence submission declares, accepted only from the gate's `AuthorizedProducer` principal kind |
| `TenantProviderEnablement` (v2) | `LR-TOPOLOGY`, `LR-EVENTSTORE`, `LR-TENANT-ACCESS`, `LR-PROVIDER`, `LR-AUDIT-PROTECTION-DELETION` |

The matrix is additive and immutable per version. The readiness decision exposes the matrix version, evaluated `RegistryRevision`, applicable GateIds, and safe blockers so every surface renders the same outcome.

The `ReleaseQualificationGateSet` contains all 18 minimum GateIds. Only current `Pass` records for the complete set plus `Available` consumed external dependencies allow `RQ-1` READY and production enablement. Its metric slice is exactly pre-enablement SM-1, SM-4, SM-5, and SM-6 through `LR-PRODUCT-METRICS`, NFR-9 through `LR-RUNTIME-PERFORMANCE`, and NFR-14 through `LR-UI-PERFORMANCE`; SM-5 supplies the 100% audit-completeness threshold. SM-2, SM-3, and SM-7 are never `RQ-1` inputs. The remaining GateIds enforce the non-metric safety, context, dependency, recovery, capacity, accessibility, payload-protection, and governance inputs that FR-28 also names. `LR-RECOVERY`, `LR-UI-CONFORMANCE`, `LR-RUNTIME-PERFORMANCE`, `LR-UI-PERFORMANCE`, and `LR-PRODUCT-METRICS` therefore qualify release without circularly blocking the controlled executions that produce their evidence.

## Launch Health Reviews

Launch health is recorded separately from `RQ-1`. It consumes real post-enablement SM-2, SM-3, and SM-7 results at 30 days, 60 days, and monthly thereafter. It never changes which inputs produced an earlier READY/NOT READY decision and it cannot be used to fill a missing pre-enablement gate.

| Metric | Launch-health contract |
| --- | --- |
| SM-2 | Eligible Conversation adoption over the PRD rolling window and cohort, using the `EXT-CONV-AI-1` active-Conversation count or Conversations-side event-feed seam; never an Agent Call denominator. |
| SM-3 | Human proposal-decision timeliness and the associated posting-failure/audit-completeness reporting defined by the PRD. Audit completeness remains independently required at 100% before enablement through SM-5. |
| SM-7 | Substantive human-review share for the PRD confirmation-mode cohort and band. |

Missing samples or an ineligible cohort produce `InsufficientEvidence` in the launch-health review and never silently pass. Two consecutive SM-3 or SM-7 misses require the recorded Product disable-or-continue decision and feed the FR-28 kill-switch trigger review. `product-metrics` is the only trigger source.

## Live-Seam Matrix

The matrix is the home of the AD-17 bind-and-test atomicity rule (approved sprint change proposal of 2026-08-04). Every row maps one seam to its current authority, its `BindingStatus` (`Deferred`, `DeferredOutOfV1`, or `Live`), and the `Hexalith.Agents.IntegrationTests` lane that must pass in the same change that flips it to `Live`. Story 5.1 owns the verifier that enforces the matrix; Story 5.6 creates the project and covers the projection and query seams that Stories 5.2 and 5.3 bound before it existed.

| Seam | Authority | `BindingStatus` | Required integration test |
| --- | --- | --- | --- |
| EventStore dispatch and setup projections (`agent-setup`, `provider-catalog`, `tenant-provider-enablement`) | AD-2, AD-3, AD-15, AD-17 | `Live` for shipped setup/catalog source; platform catalog migration and tenant enablement are `Deferred` (reopened Story 5.3; coverage owed by 5.6) | `SetupProjectionLiveTests`, `ProviderCatalogMigrationLiveTests`, `TenantProviderEnablementProjectionLiveTests` |
| Readiness observations and `launch-readiness` | AD-17 | `Deferred` (Story 5.5/5.6) | `LaunchReadinessProjectionLiveTests` |
| Dapr Workflow durable owner and restart recovery | AD-18, AD-23, AD-27 | `Deferred` (Story 6.1) | `InteractionWorkflowRecoveryLiveTests` |
| Provider invocation, reservation, admission | AD-13, AD-21, AD-24 | `Deferred` (Story 6.4, 6.5) | `ProviderAttemptProtocolLiveTests` |
| Conversations membership and posting | AD-6, AD-7 | `Deferred` (Story 6.6) | `ConversationPostingLiveTests` |
| Content safety adapter | AD-20 | `Deferred` (Story 6.3) | `ContentSafetyDecisionLiveTests` |
| EventStore payload protection, erased replay, and reference-only workflow state | AD-22, AD-27 | `Deferred` (Story 5.8) | `PayloadProtectionLiveTests`, `WorkflowContentSweepTests` |
| Legal hold, export, and protected deletion | AD-22 | `Deferred` (Stories 8.1 to 8.3) | `LegalHoldLiveTests`, `AuditExportLiveTests`, `ProtectedDeletionLiveTests` |
| Governed audit inspection | AD-22, AD-30 | `Deferred` (Story 8.8) | `AuditInspectionLiveTests` |
| Tools, MCP, A2A, remote agents | AD-19 | `DeferredOutOfV1` | none until a separately approved scope |

## Provider Readiness Contract

The public `ProviderReadinessResult` contains exactly `OperationalState`, `Callability`, `ReasonCode`, `CapabilityVersion`, `ObservedAt`, exclusive `ValidUntil`, and the discriminated `Freshness` value (architecture AD-17). The catalog is platform-scoped: `CapabilityVersion` is a non-reusable unsigned monotonic sequence per (`ProviderId`, `ModelId`) entry in the `ProviderCatalog` aggregate stored under the reserved EventStore tenant `system`; comparisons across keys are undefined. A tenant reads the platform entry joined with its `TenantProviderEnablement`; a by-key query for an entry not enabled for the caller's tenant, or absent, returns the not-found response, and `EntryMissing` is reported only for the Agent's own selected entry inside `AgentReadinessStatus`. `ProviderReadinessReasonCode` is versioned and additive: `Unknown = 0`, `None`, `NonBlockingOperationalWarning`, `DependencyUnavailable`, `EntryMissing`, `Stale`, `Disabled`, `Unconfigured`, `Unpriced`, `InvalidLimits`, `SecretUnavailable`, `CapabilityVersionRegressed`, `AdapterUnavailable`, `ProviderHealthFailed`, `Indeterminate`, and (added 2026-09-09) `PlatformNotReady`, the code a tenant-facing surface reports in place of any platform-only blocker (`Unconfigured`, `Unpriced`, `InvalidLimits`, `SecretUnavailable`, `AdapterUnavailable`, `ProviderHealthFailed`) whose detail is disclosed to the Platform Operator only.

Only (`Ready`, `Callable`, `None`), (`Degraded`, `Callable`, `NonBlockingOperationalWarning`), and (`Blocked`, `Blocked`, a defined blocker code) are valid. `Degraded` is callable only when `EXT-PROVIDER-1` is `Available` and verified and every hard gate passes: freshness, platform enabled state, tenant enablement, configured/text-generation state, current pricing, secret resolution, Provider health, non-regressed capability version, and valid positive limits. Unknown codes and missing, stale, disabled, unconfigured, unpriced, invalid, unavailable, failed, regressed, or indeterminate inputs are `Blocked`.

## NFR-11 Recovery Evidence Contract

`LR-RECOVERY` uses a production-like failure-injection exercise identified by `RecoveryExerciseId`. Immediately before injection it freezes the cohort of every durable nonterminal interaction eligible to run. Interactions created after injection, already terminal, or ineligible at the freeze instant are excluded and cannot improve the result.

One injected monotonic fixture clock records `RecoveryClockOriginId`, `RecoveryStartedTick` when the fault makes processing unavailable, and `RecoveryVerifiedTick` when every frozen member is accounted for, authoritative EventStore state and required projections are current, and terminal outcomes are observable. `RecoveryVerifiedTick - RecoveryStartedTick` must be at most 15 minutes. UTC `ObservedAt` and `ValidUntil` govern record freshness only; they are never subtracted for recovery duration.

After restart, current safety, authorization, cost, and capacity gates are re-evaluated. Each frozen member must resume or reach a safe durable blocked/terminal result that records the gate change; pre-fault terminal decisions remain immutable. The manifest compares pre/post EventStore revisions and inventories deterministic Provider attempts, proposal versions, timer/expiry decisions, budget reservations/ledger totals, Conversation `MessageId`/idempotency keys, and terminal decisions. Evidence passes only with RPO 0, no duplicated effects/versions, no changed terminal decisions, and every frozen member accounted for.

## NFR-12 Capacity And Fairness Profile

Every `LR-CAPACITY-FAIRNESS` record's `ConfigurationOrMeasurementContract` contains a versioned environment profile with these mandatory values:

```text
ProfileVersion
PerTenantConcurrencyLimit
SystemConcurrencyLimit
PerTenantQueueDepthLimit
SystemQueueDepthLimit
OverflowBehavior = Queue | Reject
FairnessPolicyKind = WeightedRoundRobinV1
TenantWeight[tenant] = integer 1..100
```

All limits are positive and test-visible. One platform-composed shared capacity allocator is the sole owner of tenant/system leases and queue order across replicas; process-local counters or queues are forbidden. A linearizable admission keyed by deterministic `AttemptId` atomically acquires both scopes and returns admitted with `AdmissionId` plus monotonically increasing `AdmissionFence`, queued with durable `QueueId`, or rejected before `ProviderInvocationAuthorized`. The authorization fact binds the fence. Immediately before Provider transport, a linearizable `BeginInvocation(AttemptId, AdmissionId, AdmissionFence)` must verify the unexpired current fence and transition `Admitted` to `InvocationActive`; reclamation or terminalization increments/invalidates the fence, so stale authorization can never invoke. Within a tenant, order is allocator sequence then `AttemptId`. Across eligible nonempty tenants, a persisted rotating cursor grants exactly `TenantWeight` slots per complete cycle of `sum(active TenantWeight)`, giving complete-cycle share `TenantWeight / sum(active TenantWeight)`. Retries retain their original `QueueId`/position and receive no additional weight.

Queue cancellation and expiry are durable terminal admission results and permit no Provider call. Leases are reclaimed only after a recorded terminal attempt or after expiry beyond the Provider timeout plus authoritative Provider outcome lookup proving no active invocation. The versioned test saturates at least two tenants across complete cycles and proves exact shares, no starvation, replica/crash/cancel/expiry recovery, and no duplicate admission while tenant/system concurrency, queue, and cost caps remain unbreached. Missing values, hidden host-only values, unbounded queues, process-local state, or insufficient evidence block the gate.

## NFR-13 UI Conformance Contract

`LR-UI-CONFORMANCE` evidence covers every interactive V1 route and high-impact state through the same public contracts used by API clients. The versioned browser/component suite proves WCAG 2.2 AA behavior, keyboard/focus order, semantic labels and live regions, whole-string localization, English/French key parity, and FrontComposer plus Fluent UI Blazor V5 inheritance. At the most restrictive supported viewport, proposal resolution, policy publication, tenant budget update, legal hold, export, and deletion remain blocked whenever required decision context cannot be presented safely. Missing routes, locale keys, viewport evidence, or conditional skips produce `InsufficientEvidence`.

## NFR-14 Browser Monotonic Timing Contract

Browser durations use one injected monotonic clock and one clock-origin identifier per page lifecycle. They never subtract browser time from server or projection wall-clock timestamps.

| Gate | Start seam | End seam | Threshold |
| --- | --- | --- | --- |
| Page usability | `NavigationStartedTick` captured when navigation begins | `PageUsableTick` captured after the authorized route renders a non-loading state and its required primary action/status is operable | p95 <= 2.5 seconds |
| Authoritative pending acknowledgement | `CommandSubmittedTick` captured immediately before dispatch | `AuthoritativePendingRenderedTick` captured after a server/EventStore-accepted pending identity and projection/version reference is received and the pending state is rendered | p95 <= 500 milliseconds |
| Terminal render and announcement | `AuthoritativeTerminalReceivedTick` captured when the client receives the authoritative terminal projection/version | both `TerminalRenderedTick` and `LiveRegionAnnouncedTick`; the sample duration uses the later tick | p95 <= 2 seconds |

Every versioned sample has common fields `SampleId`, `QualificationSessionId`, `ExecutionId`, `SampleKind`, `ClockOriginId`, route/operation family, safe trace reference, authoritative projection ID/version where applicable, viewport/profile, locale, and outcome. `SampleId` is deterministically derived from (`QualificationSessionId`, `ExecutionId`, `SampleKind`). An exact duplicate is an idempotent no-op; a conflicting duplicate is rejected and makes the evidence insufficient.

| `SampleKind` (`SampleId = H(sample, TenantScope, QualificationSessionId, ExecutionId, SampleKind)` per architecture AD-29) | Required ticks | Forbidden ticks |
| --- | --- | --- |
| `PageUsability` | `NavigationStartedTick`, `PageUsableTick` | pending and terminal ticks |
| `AuthoritativePending` | `CommandSubmittedTick`, `AuthoritativePendingRenderedTick` | page-usability and terminal ticks |
| `TerminalRenderAnnouncement` | `AuthoritativeTerminalReceivedTick`, `TerminalRenderedTick`, `LiveRegionAnnouncedTick` | page-usability and pending ticks |

`LiveRegionAnnouncedTick` is captured after render commit when the localized terminal-text mutation is observable in its `aria-live` or `role=status` node; it proves announcement-ready DOM state, not speech completion. Only samples from an authenticated versioned qualification session issued by `EXT-TOPOLOGY-1` enter `browser-ui-metrics`. The platform ingress validates kind-specific ticks, monotonic order, clock origin, session, and safe trace/projection against server evidence. Raw prompt, context, generated/proposal content, secrets, Provider payloads, and Party PII are forbidden. Unattested operational telemetry cannot qualify. Each kind requires at least 30 qualifying production-like executions. Missing/forbidden ticks, mixed origins, absent authoritative references/live-region mutation, failed correlation, conflicting duplicates, or too few samples yields `InsufficientEvidence`.

## Explicit Projection Inventory

These logical projection IDs are authoritative for readiness, deletion scope, and evidence manifests. Implementations bind storage/type names to these IDs; a story cannot replace the inventory with “all affected projections.”

| Projection ID | Owned truth exposed |
| --- | --- |
| `agent-setup` | Agent identity/configuration/lifecycle, kill-switch state, and gate-derived callability (shipped id, ratified 2026-09-09). |
| `provider-catalog` | Platform Provider/model capability, readiness, pricing, secret-configured state, freshness, and effective version (shipped id, ratified 2026-09-09). |
| `tenant-provider-enablement` | Per-tenant enabled entries joined with the platform catalog for tenant-facing readiness. |
| `agent-interaction-status` | Current interaction state, safe failure class, attempt/posting outcome, and authoritative version. |
| `proposal-detail` | Current proposal state, selected version, expiry, and authorized detail metadata. |
| `proposal-version-history` | Immutable generated, edited, and regenerated version history. |
| `pending-proposal-queue` | Authorized pending proposals ordered for action. |
| `pending-proposal-count` | Authorized pending counts for navigation and Conversation status. |
| `audit-evidence` | Tenant-scoped safe evidence graph linking calls, decisions, versions, and final posts. |
| `budget-reservation-usage` | Tenant/call limits, reservations, usage reconciliation, release, and warning/block state. |
| `retention` | Sensitive-content retention deadlines and expiry execution state. |
| `legal-hold` | Active/released legal holds and their protected scope. |
| `export` | Authorized export requests, manifests, expiry, and audit status. |
| `deletion` | Requested/confirmed payload protection, each named purge outcome, and safe tombstone completion. |
| `launch-readiness` | Current gate records and aggregate callability/READY blockers by scope/profile. |
| `runtime-metrics` | NFR-9 runtime latency source observations and qualification results. |
| `browser-ui-metrics` | NFR-14 browser-monotonic samples and qualification results. |
| `workflow-execution-state` | Not a projection: the Dapr Workflow state-store scope for terminal interaction instances, carrying references only (architecture AD-27); a named purge scope item whose confirmation deletion completion requires. |
| `product-metrics` | Versioned SM-1 through SM-7 and SM-C1 through SM-C5 calculations and insufficiency state, partitioned into the pre-enablement `RQ-1` set (SM-1/4/5/6) and post-enablement launch-health set (SM-2/3/7). |

Deletion completion names and confirms `provider-catalog` only when it contains protected content, `workflow-execution-state`, `agent-interaction-status`, `proposal-detail`, `proposal-version-history`, `pending-proposal-queue`, `pending-proposal-count`, `audit-evidence`, `retention`, `legal-hold`, `export`, `deletion`, and any content-bearing metric projection identified by its current projection contract. `agent-setup`, `tenant-provider-enablement`, `budget-reservation-usage`, `launch-readiness`, and non-content metric records retain only support-safe references required by policy.

## Initial Gate Records

No qualifying evidence was supplied when this register was created. The initial records therefore block readiness without inventing observations or validity periods.

| GateId | TenantScope | EnvironmentProfile | State | Owner | SourceVersion | ObservedAt | ValidUntil | RequiredEvidenceLevel | EvidenceReference | ConfigurationOrMeasurementContract | BlockerCode |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `LR-TOPOLOGY` | `TBD` | `production-like` | `InsufficientEvidence` | Platform Maintainer | `TBD` | `TBD` | `TBD` | Levels 4 and 5 | `TBD` | `EXT-TOPOLOGY-1`; exact fixture/profile contract required | `EvidenceNotRecorded` |
| `LR-EVENTSTORE` | `TBD` | `production-like` | `InsufficientEvidence` | `TBD` | `TBD` | `TBD` | `TBD` | Levels 4 and 5 | `TBD` | RPO 0, command/event/projection/replay contract required | `EvidenceNotRecorded` |
| `LR-TENANT-ACCESS` | `TBD` | `production-like` | `InsufficientEvidence` | `TBD` | `TBD` | `TBD` | `TBD` | Levels 4 and 5 | `TBD` | Fresh authorization and focused cross-tenant denial contract required | `EvidenceNotRecorded` |
| `LR-PARTY-IDENTITY` | `TBD` | `production-like` | `InsufficientEvidence` | `TBD` | `TBD` | `TBD` | `TBD` | Levels 4 and 5 | `TBD` | Party identity and posting-eligibility contract required | `EvidenceNotRecorded` |
| `LR-CONVERSATION-CONTEXT` | `TBD` | `production-like` | `InsufficientEvidence` | `TBD` | `TBD` | `TBD` | `TBD` | Levels 4 and 5 | `TBD` | Complete authorized context and exact budget contract required | `EvidenceNotRecorded` |
| `LR-CONVERSATIONS-MEMBERSHIP-POSTING` | `TBD` | `production-like` | `InsufficientEvidence` | Conversations Maintainer | `TBD` | `TBD` | `TBD` | Levels 4 and 5 | `TBD` | `EXT-CONV-AI-1` compatibility contract required | `DependencyUncommitted` |
| `LR-PROVIDER` | `TBD` | `production-like` | `InsufficientEvidence` | Agents Runtime Maintainer | `TBD` | `TBD` | `TBD` | Levels 4 and 5 | `TBD` | `EXT-PROVIDER-1` and public readiness contract required | `DependencyUncommitted` |
| `LR-TOKENIZER` | `TBD` | `production-like` | `InsufficientEvidence` | Agents Runtime Maintainer | `TBD` | `TBD` | `TBD` | Levels 4 and 5 | `TBD` | `EXT-TOKEN-1` exact measurement contract required | `DependencyUncommitted` |
| `LR-SAFETY` | `TBD` | `production-like` | `InsufficientEvidence` | Security Engineering | `TBD` | `TBD` | `TBD` | Levels 4 and 5 | `TBD` | `EXT-SAFETY-1` two-stage safety contract required | `DependencyUncommitted` |
| `LR-SECRETS` | `TBD` | `production-like` | `InsufficientEvidence` | Platform Maintainer | `TBD` | `TBD` | `TBD` | Levels 4 and 5 | `TBD` | `EXT-SECRETS-1` resolution/rotation/denial/no-leak contract required | `DependencyUncommitted` |
| `LR-COST` | `TBD` | `production-like` | `InsufficientEvidence` | Agents Runtime Maintainer | `TBD` | `TBD` | `TBD` | Levels 4 and 5 | `TBD` | Pricing, numeric caps, reservation, reconciliation contract required | `EvidenceNotRecorded` |
| `LR-AUDIT-PROTECTION-DELETION` | `TBD` | `production-like` | `InsufficientEvidence` | Security Engineering | `TBD` | `TBD` | `TBD` | Levels 4 and 5 | `TBD` | Protection/retention/hold/export/deletion/projection contract required | `EvidenceNotRecorded` |
| `LR-RECOVERY` | `TBD` | `production-like` | `InsufficientEvidence` | Test Architect | `TBD` | `TBD` | `TBD` | Levels 4 and 5 | `TBD` | NFR-11 recovery contract in this register | `EvidenceNotRecorded` |
| `LR-CAPACITY-FAIRNESS` | `TBD` | `production-like` | `InsufficientEvidence` | Test Architect | `TBD` | `TBD` | `TBD` | Levels 4 and 5 | `TBD` | NFR-12 profile and fairness contract in this register | `EvidenceNotRecorded` |
| `LR-UI-CONFORMANCE` | `TBD` | `production-like` | `InsufficientEvidence` | UX Designer | `TBD` | `TBD` | `TBD` | Levels 4 and 5 | `TBD` | NFR-13 conformance contract in this register | `EvidenceNotRecorded` |
| `LR-RUNTIME-PERFORMANCE` | `TBD` | `production-like` | `InsufficientEvidence` | Test Architect | `TBD` | `TBD` | `TBD` | Levels 4 and 5 | `TBD` | NFR-9 measurement contract required | `EvidenceNotRecorded` |
| `LR-UI-PERFORMANCE` | `TBD` | `production-like` | `InsufficientEvidence` | Test Architect | `TBD` | `TBD` | `TBD` | Levels 4 and 5 | `TBD` | NFR-14 browser timing contract in this register | `EvidenceNotRecorded` |
| `LR-PRODUCT-METRICS` | `TBD` | `production-like` | `InsufficientEvidence` | Test Architect | `TBD` | `TBD` | `TBD` | Levels 4 and 5 | `TBD` | Versioned SM-1/SM-4/SM-5/SM-6 measurement contracts and 100% SM-5 audit completeness required; SM-2/SM-3/SM-7 excluded | `EvidenceNotRecorded` |

## Current Release Decision

`RQ-1`: **NOT READY**. Every minimum gate is `InsufficientEvidence`, critical external dependencies are `Uncommitted`, and no production-like tenant scope, source versions, authoritative timestamps, validity periods, or qualifying Levels 4/5 evidence references have been accepted.
