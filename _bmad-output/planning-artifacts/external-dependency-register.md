---
title: Hexalith Agents External Dependency Register
status: active
created: 2026-08-02
updated: 2026-08-09
project: agents
authority: sprint-change-proposal-2026-08-02.md
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

## Critical Dependency Records

### EXT-CONV-AI-1 — Conversations AI Membership And Posting

| Commitment field | Value |
| --- | --- |
| `Owner` | Conversations Maintainer |
| `Repository` | `Hexalith.Conversations` |
| `RequiredArtifact` | Public `IConversationClient.AddParticipantAsync` and `POST /api/v1/conversations/{conversationId}/participants`, plus the supported final-message append seam. Agents use is limited to stable AI Party identity, `ParticipantType.AiAgent` (`AIAgent`), and `ParticipantRole.Member`; exact retries are idempotent no-ops, conflicting type/role is a typed conflict, and cross-tenant or general participant management is denied. |
| `TargetVersionOrCommit` | `TBD` |
| `TargetIntegrationDate` | `TBD` |
| `CompatibilityContractAndVerificationCommand` | Contract: typed idempotent membership and posting with focused cross-tenant denial. Command: `TBD`. |
| `RequiredEvidenceLevel` | Levels 4 and 5 |
| `AcceptedStatus` | `Uncommitted` |
| `ConsumingStories` | 6.6, 7.4; `RQ-1` |

### EXT-HOST-1 — Platform-Owned Agents Host Composition

| Commitment field | Value |
| --- | --- |
| `Owner` | Platform Maintainer |
| `Repository` | `Hexalith.Platform` |
| `RequiredArtifact` | Platform-owned host composition for the Agents DomainService and UI with EventStore, Conversations, Parties, Tenants, Provider and safety adapters, Dapr Workflow, secrets, health, identity, and telemetry. The artifact replaces module-owned AppHost, Aspire, and ServiceDefaults ownership. |
| `TargetVersionOrCommit` | `a66cdf346e521ad147f442b686f301f0f59c525c` |
| `TargetIntegrationDate` | `2026-09-30` |
| `CompatibilityContractAndVerificationCommand` | Contract: clean-checkout composition through the platform host without module-owned hosting infrastructure. Command: `git clone https://github.com/Hexalith/Hexalith.Platform.git && cd Hexalith.Platform && git checkout a66cdf346e521ad147f442b686f301f0f59c525c && ./eng/verify-agents-host.sh`. |
| `RequiredEvidenceLevel` | Levels 4 and 5 |
| `AcceptedStatus` | `Committed` |
| `ConsumingStories` | 5.1, 5.6 onward; `RQ-1` |

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
| `ConsumingStories` | 5.3, 5.5, 6.4, 7.3; `RQ-1` |

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
| `ConsumingStories` | 5.6, 6.4, 8.2, 8.3; `RQ-1` |

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
| `ConsumingStories` | 5.6, 6.1, 6.5, 8.5, 8.6, 8.7; `RQ-1` |

## Current Blocking Summary

`EXT-HOST-1` is `Committed` (scaffold + clean-checkout AppHost build gate). The remaining six records are `Uncommitted`; their consumers remain blocked from `ready-for-dev` until the fields required for `Committed` are accepted. `EXT-HOST-1` stays short of `Available` until Agents Story 5.6 wires live composition and the verify command is upgraded with Level 4 evidence. `RQ-1` additionally requires each dependency used by its qualification profile to be `Available` with the required live evidence.
