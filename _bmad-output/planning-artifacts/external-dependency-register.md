---
title: Hexalith Agents External Dependency Register
status: active
created: 2026-08-02
updated: 2026-09-09
project: agents
authority: sprint-change-proposal-2026-09-09-2.md
---

# External Dependency Register

## Authority And Entry Gate

This register is the authoritative delivery contract for critical Hexalith Agents dependencies delegated by the PRD and the approved Sprint Change Proposal. A consuming story cannot move to `ready-for-dev` unless every dependency it names is `Committed` or `Available` and has no `TBD` target, integration date, or executable verification command.

Unknown values remain `TBD` and the record remains `Uncommitted`. They are blockers, not inferred commitments. A repository path, package version, branch, or local working-tree state is not a commitment unless its owner accepts it in this register.

## Record Schema

`ID` is the stable dependency key. Each record then contains the nine commitment fields required by PRD §8.

| Field | Contract |
| --- | --- |
| `ID` | Stable `EXT-*` identifier; never reused for a different compatibility seam. |
| `Owner` | Named role or person accountable for the artifact and commitment. |
| `Repository` | Owning repository, not the consuming checkout path unless they are the same authority. |
| `RequiredArtifact` | Public package, API, host composition, adapter, fixture, or other deliverable the consumer requires. |
| `TargetVersionOrCommit` | Exact package version or immutable commit. A branch, range, or `latest` is not accepted. |
| `TargetIntegrationDate` | Accepted date by which the target and compatibility evidence will be consumable. |
| `CompatibilityContractAndVerificationCommand` | Stable behavioral contract plus an executable command that proves the target against the consumer. Narrative-only verification is insufficient. |
| `RequiredEvidenceLevel` | Normative PRD Evidence Level required from the dependency. |
| `AcceptedStatus` | `Uncommitted`, `Committed`, or `Available`, using the semantics below. |
| `ConsumingStories` | Exact replacement stories or release gate blocked by this record. |

### Accepted Status Semantics

| Status | Meaning |
| --- | --- |
| `Uncommitted` | One or more commitment fields are unknown or have not been accepted. Every consumer is blocked from `ready-for-dev`. |
| `Committed` | Owner, repository, artifact, immutable target, integration date, compatibility contract and executable command, evidence level, and consumers are accepted. The artifact may still be in delivery. |
| `Available` | The committed target is installed/consumable and its executable compatibility command passes with live Level 4 component evidence. Where `RequiredEvidenceLevel` also names Level 5, cross-system attainment is completed by the consuming launch-readiness gate and `RQ-1`; it is not a prerequisite for beginning that same qualification run. |

Changing an owner, artifact, target, compatibility behavior, verification command, or required evidence level invalidates prior acceptance and returns the record to `Uncommitted` until the changed commitment is accepted. A failed compatibility command makes the dependency unavailable and blocks consumers even if the record was previously `Available`.

`Committed` permits story readiness and contract/package work only. No runtime, test, or qualification path may execute a consumed external seam until the record is `Available` and its compatibility command passes against the exact target used by that execution.

A runtime, test, or qualification path that would execute a seam whose record is not `Available` fails closed with the typed outcome `DependencyNotAvailable` naming the record (PRD FR-21). `RQ-1` records the same code for every record in its qualification profile that is not `Available` (PRD FR-28).

## Critical Dependency Records

### EXT-CONV-AI-1 — Conversations AI Membership And Posting

| Commitment field | Value |
| --- | --- |
| `Owner` | Conversations Maintainer |
| `Repository` | `Hexalith.Conversations` |
| `RequiredArtifact` | Six seams. (1) Membership: public `IConversationClient.AddParticipantAsync` and `POST /api/v1/conversations/{conversationId}/participants`, limited for Agents to stable AI Party identity, `ParticipantType.AiAgent`, and `ParticipantRole.Member`; exact retries are idempotent no-ops, conflicting type/role is a typed conflict, and cross-tenant or general participant management is denied; plus a participant-state read for the AI participant and a participant removal limited to the AI participant for the PRD FR-2 removal block. Before accepting AI membership, Conversations verifies through Hexalith.Parties that the participant Party is AI type; the record owner supplies the final member and typed failure names (A-21). (2) Posting: append a Conversation Message as the `AiAgent` participant with an Agents-supplied deterministic `MessageId` and idempotency key, persisted verbatim or rejected (a differing returned id is a typed incompatibility that blocks posting), and message metadata carrying the Agent Call trace reference and provenance flags (AI-generated; human-edited and by which Party), so restart or replay cannot duplicate a post; plus an existence read by `MessageId` returning the message, typed absence, typed `ConversationDeleted`, typed `PrincipalRemovedFromConversation`, or unavailable, so Agents can check before retry or abandonment after a lost acknowledgement and record `LateConfirmed` rather than contradict an already-posted message. (3) Facilitator resolution: `ParticipantRole.Facilitator` on the participant read model. (4) The Eligible Conversation denominator is supplied either as an active-Conversation count per tenant/window or as a Conversations-side tenant event feed from which Agents computes the same count, never from Agent Calls (A-4). (5) Tenant-scoped reads under the Agents Service Principal of complete Conversation content, the Participant roster with roles, and Conversation existence/accessibility; they return typed `ConversationDeleted` and `PrincipalRemovedFromConversation` distinctly from transient denial or error, and content contains the messages a Participant would currently see, including current edit/delete state (A-15). (6) The Conversation deletion signal remains unchanged so an approved deletion in Conversations triggers PRD FR-30 deletion of derived Agent content. Final member and typed failure names are owned by this record (PRD §8.1 assumptions A-1 through A-4, A-15, A-16, A-21). |
| `TargetVersionOrCommit` | `TBD` |
| `TargetIntegrationDate` | `TBD` |
| `CompatibilityContractAndVerificationCommand` | Contract: typed idempotent membership with participant-state read, AI-participant removal, and Hexalith.Parties AI-type verification; idempotent posting with trace/provenance metadata and a typed `MessageId` existence read covering message, absence, deletion, principal removal, and unavailable; Facilitator role exposure; active-Conversation count or equivalent Conversations-side tenant event feed; tenant-scoped current content/roster/existence/accessibility reads that distinguish deletion/removal from transient denial/error; and a deletion signal, each with focused cross-tenant denial. Command: `TBD`. |
| `RequiredEvidenceLevel` | Levels 4 and 5 |
| `AcceptedStatus` | `Uncommitted` |
| `ConsumingStories` | 5.4, 6.1, 6.2, 6.6, 6.8, 7.1–7.5, 7.7, 8.5, 8.8; `RQ-1` |

*Scope extended 2026-09-09 by the PRD validation reconciliation and follow-through proposal: the posting existence read, Facilitator resolution, active-count/event-feed denominator, and tenant-scoped content/roster/existence/accessibility consumers are explicit. The record was already `Uncommitted`; the extension changes no status and blocks every newly listed consumer from `ready-for-dev` until accepted.*

### EXT-CONV-UI-1 — Conversation Action Contribution For Call hexa

| Commitment field | Value |
| --- | --- |
| `Owner` | `TBD` |
| `Repository` | `Hexalith.Conversations` |
| `RequiredArtifact` | Three artifact kinds. (1) A versioned Conversation action contribution and registration contract allowing Agents to contribute the Conversation-owned **Call hexa** action into a Conversation surface, with tenant-scoped authorization and typed failure when the action cannot be registered; Conversations owns the trigger and Agents owns the self-contained `ConversationAgentCallPanel` dialog body. (2) A per-message decoration slot keyed by `MessageId` through which Agents renders the AI-generated and human-edited provenance markers carried in message metadata (PRD FR-11, FR-17) wherever a message's provenance is disclosed, backed by an Agents-side provenance accessor. (3) `GetCallabilityAsync(tenant, conversation)` so the trigger can reflect Agents callability before the dialog opens. Conversations never references Agents packages (architecture AD-31). |
| `TargetVersionOrCommit` | `TBD` |
| `TargetIntegrationDate` | `TBD` |
| `CompatibilityContractAndVerificationCommand` | Contract: authorized, tenant-scoped action contribution with typed registration failure, a `MessageId`-keyed decoration slot, a callability gateway, and no cross-tenant exposure. Command: `TBD`. |
| `RequiredEvidenceLevel` | Levels 4 and 5 |
| `AcceptedStatus` | `Uncommitted` |
| `ConsumingStories` | 6.7; `RQ-1` |

*Added 2026-09-08 by the PRD update applying the approved 2026-08-03 sprint change proposal (PRD §8, AM-5); provenance-marker rendering added 2026-09-09 (PRD finding H4); decoration slot, callability gateway, and consuming Story 6.7 bound 2026-09-09 by the architecture update (AD-31). Commitment fields remain `TBD`, so under PRD FR-21 every consuming story stays blocked from `ready-for-dev` until an owner accepts them.*

### EXT-HOST-1 — Platform-Owned Agents Host Composition

| Commitment field | Value |
| --- | --- |
| `Owner` | Platform Maintainer |
| `Repository` | `Hexalith.Platform` |
| `RequiredArtifact` | Platform-owned host composition for the Agents DomainService and UI with EventStore, Conversations, Parties, Tenants, Provider and safety adapters, Dapr Workflow, secrets, health, identity, and telemetry. It additionally binds the production `EXT-PROTECTION-1` engine and exposes the FR-34 attestation port used at startup and every readiness evaluation. The port lets Agents seal a canary, prove persisted bytes contain no plaintext, unseal it, destroy its DEK, replay `Erased`, and obtain engine identity/version for comparison with the committed protection target; the host fails closed against both the no-op default and a self-reporting wrapper around it. The artifact replaces module-owned AppHost, Aspire, and ServiceDefaults ownership. |
| `TargetVersionOrCommit` | `TBD` |
| `TargetIntegrationDate` | `TBD` |
| `CompatibilityContractAndVerificationCommand` | Contract: clean-checkout composition through the platform host without module-owned hosting infrastructure, including production protection-engine binding and the complete FR-34 canary/identity/version attestation. Command: `TBD`. |
| `RequiredEvidenceLevel` | Levels 4 and 5 |
| `AcceptedStatus` | `Uncommitted` |
| `ConsumingStories` | 5.1, 5.6 onward; `RQ-1` |

*Historical prior commitment only: target `a66cdf346e521ad147f442b686f301f0f59c525c`, integration date `2026-09-30`, and `./eng/verify-agents-host.sh` covered the earlier clean-checkout host artifact. The expanded protection binding and attestation artifact has not been re-accepted by the Platform Maintainer, so those values are not current commitment fields.*

### EXT-PROVIDER-1 — Provider Generation Adapter

| Commitment field | Value |
| --- | --- |
| `Owner` | Agents Runtime Maintainer |
| `Repository` | `TBD` |
| `RequiredArtifact` | Selected Provider adapter and any selected Provider or Agent Framework SDK, with safe availability, error, timeout, usage, pricing, capability-version, deterministic idempotent invocation, and outcome lookup by `AttemptId`. Provider SDK types and raw errors do not cross the Agents public boundary. |
| `TargetVersionOrCommit` | `TBD` |
| `TargetIntegrationDate` | `TBD` |
| `CompatibilityContractAndVerificationCommand` | Contract: the adapter implements the public Provider readiness and prepared-attempt contracts without owning orchestration or domain state, and crash recovery can resolve/reuse an `AttemptId` without duplicate Provider work. Command: `TBD`. |
| `RequiredEvidenceLevel` | Levels 4 and 5 |
| `AcceptedStatus` | `Uncommitted` |
| `ConsumingStories` | 5.5, 6.4, 7.3; `RQ-1` (narrowed 2026-09-09 by the architecture update: Story 5.3 published catalog truth adapter-free and is not a consumer) |

### EXT-SAFETY-1 — Versioned Content Safety Adapter

| Commitment field | Value |
| --- | --- |
| `Owner` | Security Engineering |
| `Repository` | `TBD` |
| `RequiredArtifact` | Versioned prompt, complete-context, and generated-output safety adapter with safe reason codes, always-blocked/restricted-category enforcement, no Approver override, and no-weaker-retry behavior. |
| `TargetVersionOrCommit` | `TBD` |
| `TargetIntegrationDate` | `TBD` |
| `CompatibilityContractAndVerificationCommand` | Contract: fresh deterministic safety decisions before Provider and before proposal/posting side effects, including fail-closed missing, stale, unversioned, and indeterminate outcomes. Command: `TBD`. |
| `RequiredEvidenceLevel` | Levels 4 and 5 |
| `AcceptedStatus` | `Uncommitted` |
| `ConsumingStories` | 6.3, 7.3, 7.4; `RQ-1` |

### EXT-TOKEN-1 — Provider/Model Tokenizer

| Commitment field | Value |
| --- | --- |
| `Owner` | Agents Runtime Maintainer |
| `Repository` | `TBD` |
| `RequiredArtifact` | Provider/model-specific tokenizer that measures the exact prepared prompt and complete authorized Conversation Context against the selected model budget. |
| `TargetVersionOrCommit` | `TBD` |
| `TargetIntegrationDate` | `TBD` |
| `CompatibilityContractAndVerificationCommand` | Contract: deterministic token counts for the exact prepared request; missing or unsupported Provider/model tokenization fails closed before Provider invocation. Command: `TBD`. |
| `RequiredEvidenceLevel` | Levels 4 and 5 |
| `AcceptedStatus` | `Uncommitted` |
| `ConsumingStories` | 6.2, 7.3; `RQ-1` |

### EXT-SECRETS-1 — Platform Secret Resolution

| Commitment field | Value |
| --- | --- |
| `Owner` | Platform Maintainer |
| `Repository` | `TBD` |
| `RequiredArtifact` | Secret resolution, rotation, denial, and leak-evidence contract for Provider and export/deletion operations. Only secret references and configured/not-configured state cross Agents boundaries. |
| `TargetVersionOrCommit` | `TBD` |
| `TargetIntegrationDate` | `TBD` |
| `CompatibilityContractAndVerificationCommand` | Contract: platform-hosted secret access, rotation recovery, denied access, and proof that values do not enter events, projections, responses, logs, traces, or evidence. Command: `TBD`. |
| `RequiredEvidenceLevel` | Levels 4 and 5 |
| `AcceptedStatus` | `Uncommitted` |
| `ConsumingStories` | 5.6, 5.8, 6.4, 8.1, 8.2, 8.3; `RQ-1` |

### EXT-TOPOLOGY-1 — Production-Like Qualification Fixture

| Commitment field | Value |
| --- | --- |
| `Owner` | Platform Maintainer |
| `Repository` | `TBD` |
| `RequiredArtifact` | Versioned production-like fixture with component versions, reset/seed procedure, evidence capture, failure injection, browser execution, authenticated qualification sessions, and safe trace/projection correlation through the platform evidence ingress. |
| `TargetVersionOrCommit` | `TBD` |
| `TargetIntegrationDate` | `TBD` |
| `CompatibilityContractAndVerificationCommand` | Contract: reproducible Level 5 topology and browser/runtime evidence collection without hidden manual setup or conditional skips. Command: `TBD`. |
| `RequiredEvidenceLevel` | Levels 4 and 5 |
| `AcceptedStatus` | `Uncommitted` |
| `ConsumingStories` | 5.6, 6.1, 6.5, 8.5, 8.6, 8.7, 8.8; `RQ-1` |

### EXT-PROTECTION-1 — EventStore Payload-Protection Engine

| Commitment field | Value |
| --- | --- |
| `Owner` | EventStore Maintainer |
| `Repository` | `Hexalith.EventStore` |
| `RequiredArtifact` | A production payload-protection engine behind the existing EventStore hooks, applied field-level to a `ProtectedContent` envelope so every other event field stays plaintext and a destroyed-key field unprotects as the typed value `Erased` without breaking replay: one data-encryption key per `AgentInteraction` wrapped by a per-tenant key-encryption key custodied through `EXT-SECRETS-1`, the custodian operations `WrapDek`, `UnwrapDek`, `PinDek`, `UnpinDek`, and `DestroyDek` with an irreversible destruction receipt, cryptographic erasure by DEK destruction, redaction of read-model copies, hold pinning that rejects DEK destruction, sealed envelopes on the pub/sub broker and in read models, `PayloadUnprotectionOutcome` unreadable after erasure, and snapshot/replay caches inheriting the DEK (architecture AD-22, AD-27). The shipped default is a no-op service and is not acceptable for content-bearing workflows (AD-14). |
| `TargetVersionOrCommit` | `TBD` |
| `TargetIntegrationDate` | `TBD` |
| `CompatibilityContractAndVerificationCommand` | Contract: an owner-supplied executable command must prove at minimum (1) seal with plaintext-absence proof, (2) unseal with payload equality, (3) irreversible erase with typed `Erased` replay, (4) hold pin rejecting erase until authorized unpin, and (5) engine identity/version reporting equal to the exact committed target, with focused per-tenant key isolation and restore-cannot-revive behavior. Command: `TBD`. |
| `RequiredEvidenceLevel` | Levels 4 and 5 |
| `AcceptedStatus` | `Uncommitted` |
| `ConsumingStories` | 5.8, 6.1–6.4, 7.1–7.4, 8.1–8.3, 8.8; `RQ-1` |

*Added 2026-09-09 by the architecture update: the 2026-09-09 verified-current review found that the EventStore checkout carries only the protection hooks and a no-op default, so the AD-22 key hierarchy has no owner without this record.*

## Known Consumer Non-Conformance

### Story 5.3 / EXT-PROVIDER-1 Historical Consumption — Open Product Approval

OQ-17 settles the rule for consumption of an `Uncommitted` seam but does not establish whether Story 5.3 actually executed `EXT-PROVIDER-1`. The inspected Story 5.3 specification and fail-closed deferred provider show that no live adapter shipped, but they are insufficient evidence for a dated Product ruling. Until Product selects one evidence-backed branch, neither disposition is recorded:

- Branch A — seam was consumed: `Story 5.3 | EXT-PROVIDER-1 | <verified completion date> | completed while Uncommitted; reopened by sprint-change-proposal-2026-09-09; reopening does not clear this record (PRD FR-21)`.
- Branch B — seam was never consumed: `None: Story 5.3 executed no EXT-PROVIDER-1 seam (Product ruling <date>)`.

This open approval condition is blocking provenance, not a fabricated non-conformance finding or dependency commitment.

### NC-5.3-PLATFORM-CATALOG-SCOPE — Story 5.3 Platform Catalog Migration

| Field | Value |
| --- | --- |
| `Status` | Open |
| `Owner` | Agents Runtime Maintainer |
| `AffectedStory` | 5.3 — Govern Provider Models And Pricing Through Live Operations |
| `Finding` | The shipped implementation uses tenant-scoped Provider catalog streams and read models. It does not yet implement AD-2's platform `system` catalog plus tenant-scoped `TenantProviderEnablement` split. |
| `Disposition` | Retain the shipped implementation as migration source; migrate idempotently into platform catalog and tenant-enablement streams with `MigratedFrom`; freeze legacy streams/read models without rewriting or deleting history. |
| `ClosureEvidence` | Revised Story 5.3 verification passes platform/tenant aggregate, migration/replay, projection, authorization, and cross-tenant non-disclosure tests, with Story 5.6 live-seam evidence where assigned. |
| `DependencyEffect` | This record does not make `EXT-PROVIDER-1` a Story 5.3 consumer, does not alter any dependency status, and cannot be cited as current architecture or release conformance. |

The record closes only when its named evidence is accepted and this table is amended in the same change. A completed historical Story 5.3 specification or the existing tenant-scoped implementation does not close it.

## Current Blocking Summary

All nine dependency records are `Uncommitted`; every consumer remains blocked from `ready-for-dev` until the fields required for `Committed` are accepted. `EXT-HOST-1`'s earlier target/date/command are historical only because the required artifact now includes the production `EXT-PROTECTION-1` binding and FR-34 attestation port. `RQ-1` additionally requires every dependency in its qualification profile to be `Available` with the exact compatibility command passing against the target used by the run.
