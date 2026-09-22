---
title: '5.2 Normalize Live Setup Idempotency and Enum Contracts'
type: 'bugfix'
created: '2026-09-22'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '13857fd86e9fda7e2ba0f4017aadb042750e4284'
context:
  - '_bmad-output/implementation-artifacts/epic-5-context.md'
  - '_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md'
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-state-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** A retried live setup command can conflict when its JSON spelling changes but its declared meaning does not, and shipped enum contracts still depend on implicit ordinals or a converter that cannot read every valid numeric token.

**Approach:** Hash setup idempotency from the declared command semantics. Fail closed when an in-flight key's stored canonical bytes differ. Pin the current ordinal of every shipped setup enum, and make the tolerant converter return `Unknown` for numeric tokens it cannot represent.

**Decisions:** Do not renumber any shipped enum value. Activation identity stays target, semantic payload, administrator authorization, and expected configuration version; provider and approver verdicts stay out. A whitespace-padded app id and a second `AddAgentsEventStore` registration stay out of scope.

## Boundaries & Constraints

**Always:** Keep admission in the EventStore gateway. Authorize before mutation. Preserve `Unknown = 0`, name-based writes, and numeric legacy reads. A changed canonical form for an already-admitted key conflicts and must not append a second event.

**Never:** Do not add an Agents retry store, AppHost, or dynamic plugin load. Do not change Party identity, ProvisionHexa, or Story 5.6 host composition. Do not change CI, release workflows, Dependabot, or the Builds gitlink in this slice. Do not trim app ids or reject a second gateway registration.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Equivalent setup JSON | Same key, target, authorization, and version; payload differs only by JSON spelling or key order | One admission and one replay | A semantic payload change conflicts |
| In-flight key | Stored canonical bytes came from the previous encoder | Retry conflicts | Never double-apply |
| Non-agent domain | Setup adapter receives another domain | No intent is created | Typed argument failure |
| Activation-only key | Create, disable, or response-mode command carries a provider verdict or expected version | Policy rejects the key | No admission |
| Mixed Dapr identity | Caller claim contains the allow-listed app id and another app id | Policy rejects the caller | Fail closed |
| Wide enum token | Valid `long` or `ulong` ordinal outside `Int32` | Read degrades to `Unknown` | No throw and no truncated value |
| Domain host | Host composition is built without gateway registration | Setup adapters are absent | A source-text scan is not the proof |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Agents.EventStore/AgentSetupIdempotencyIntentAdapter.cs` — `CreateIntent` still hashes `command.Payload`. `RetentionTier` is `Mutation`. Reuse `CanonicalIdempotencyIntentEncoder` for syntax canonicalization only.
- `src/Hexalith.Agents.Contracts/Agent/AgentSetupWriteEffect.cs`, `Operations/AgentOperationStatus.cs`, `Operations/AgentOperationErrorCode.cs` — `Unknown = 0`; later members are implicit. `AgentSetupTruthState` already pins its ordinals.
- `src/Hexalith.Agents.Contracts/Serialization/UnknownFallbackEnumConverter.cs` — numeric reads use `TryGetInt32` only. Tests live in `test/Hexalith.Agents.Contracts.Tests/AgentOperationContractsTests.cs`.
- `src/Hexalith.Agents.EventStore/AgentsTrustedCommandExtensionPolicy.cs` and `test/Hexalith.Agents.Server.Tests/AgentsEventStoreGatewayIntegrationTests.cs` — theories miss `"false"`, `"0"`, create/disable/response-mode activation keys, a mixed app-id claim, and a non-agent domain on the adapter.
- `test/Hexalith.Agents.Server.Tests/AppHostSecurityTopologyTests.cs` — `ServerHostShouldDelegateTheStatusReaderFallbackToSetupComposition` only scans `Program.cs`.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Agents.EventStore/AgentSetupIdempotencyIntentAdapter.cs` -- hash the declared command semantics, and test non-agent domain, retention tier, and canonical bytes through the existing gateway HTTP path -- equivalent spelling replays and a stored-byte mismatch conflicts.
- [x] `src/Hexalith.Agents.Contracts/Agent/AgentSetupWriteEffect.cs`, `src/Hexalith.Agents.Contracts/Operations/AgentOperationStatus.cs`, `src/Hexalith.Agents.Contracts/Operations/AgentOperationErrorCode.cs`, `src/Hexalith.Agents.Contracts/Serialization/UnknownFallbackEnumConverter.cs` -- pin current ordinals and degrade unrepresentable numeric tokens to `Unknown` -- shipped values stay stable.
- [x] `test/Hexalith.Agents.Server.Tests/AgentsEventStoreGatewayIntegrationTests.cs` -- extend the accept, claims, and identity theories -- activation-only keys, `"false"`, `"0"`, a mixed app-id claim, and a non-agent domain are pinned.
- [x] `test/Hexalith.Agents.Server.Tests/AppHostSecurityTopologyTests.cs` -- prove by composition that the domain host does not register gateway adapters -- replace the source-text scan as the proof.

**Acceptance Criteria:**
- Given one setup key and equivalent command JSON, when the second submission is admitted, then EventStore replays the original outcome and a semantically different payload conflicts without a second event.
- Given current enum ordinals and a numeric token outside `Int32`, when the tolerant converter reads it, then every shipped ordinal is unchanged and the value is `Unknown`.
- Given a create, disable, or response-mode command, when it carries an activation-only extension or a mixed Dapr app-id claim, then the trusted policy rejects it before admission.

## Implementation Notes

- Setup adapters now pass the declared command type into `AgentSetupIdempotencyIntentAdapter`, which deserializes the payload with `EventStorePayloadSerialization.Options` and re-serializes that contract before the existing canonical encoder. Equivalent spelling and key order share one intent. An in-flight digest built from the previous raw payload conflicts on the gateway HTTP path and does not execute.
- `AgentSetupWriteEffect`, `AgentOperationStatus`, and `AgentOperationErrorCode` pin their existing ordinals. `UnknownFallbackEnumConverter` reads `long` and `ulong` tokens that do not fit `Int32` and returns `Unknown` without casting them down.
- Domain-host registration moved unchanged into `AgentDomainHostComposition`. `ServerHostShouldDelegateTheStatusReaderFallbackToSetupComposition` builds that graph and asserts no idempotency adapters or trusted-extension policies are registered.
- Verification on 2026-09-22, after rebuilding: `Hexalith.Agents.Contracts.Tests` class `AgentOperationContractsTests` passed 322/322; `AgentsEventStoreGatewayIntegrationTests` passed 62/62; `AppHostSecurityTopologyTests` passed 5/5. None skipped.

## Spec Change Log

## Review Triage Log

| ID | Verdict | Evidence | Route |
| --- | --- | --- | --- |
| BH-01 | false | `ActivateAgent` and `DisableAgent` declare no properties, so undeclared JSON is one declared command. Shared intent is the specified semantic hash; the removed `{"value":2}` row asserted raw JSON identity. | reject |
| BH-02 | false | `EventStorePayloadSerialization.Options` uses web defaults and leaves `RespectRequiredConstructorParameters` false, so missing arguments bind as defaults. That object is the declared command this spec hashes, and a later different payload conflicts. | reject |
| BH-03 | false | Dropping unmapped members is declared-contract normalization. Web defaults set `AllowDuplicateProperties` true, so the last duplicate is the value the shared reader and this hash both bind. | reject |
| BH-04 | false | `AgentResponseMode` uses `UnknownFallbackEnumConverter`, so `"nope"`, `9999`, and `"Unknown"` are one declared value. A later `Automatic` payload is a different command and conflicts, which the spec requires. | reject |
| BH-05 | medium | `Automatic` and `Confirmation` are still implicit while the new gateway test treats JSON `1` as `Automatic`. Inserting a member would change that idempotency identity without failing the pin test. | patch |
| BH-06 | medium | `AgentLifecycleStatus`, `AgentSetupWriteStatus`, `AgentActivationBlocker`, and `AgentLaunchReadinessBlocker` still leave members after the first explicit value implicit. The approved approach pins every shipped setup enum. | patch |
| BH-07 | false | The deferred-work entry is the approved split of CI and release honesty out of this spec. The Never section is why that slice is not in this diff. | reject |
| BH-08 | false | The proposed fix rewrites this build's spec. The Code Map is historical planning context, and the tasks already record the completed work. | reject |
| BH-09 | low | `AddAgentSetupServices` never registers gateway adapters, with or without `BaseUrl`. `Program.cs` only calls `Configure`, which the composition test builds. Restoring a source scan contradicts the spec. | reject |
| BH-10 | false | `A_non_agent_domain_fails_on_the_gateway_before_execution` asserts a non-success status, zero domain executions, and zero ledger executions. `Registry().Resolve` asserts `ArgumentException`. The spec does not require HTTP 503 or a problem body. | reject |
| BH-11 | defer | `AgentAggregate` already stores `CreateAgent.TenantId` from the payload rather than the envelope. This adapter does not choose that tenant, and the spec forbids changing Party identity. The missing numeric `MessageId` and omitted-description assertions do not show a wrong outcome. | defer |
| BH-12 | false | A signed or plus-prefixed value that fits in `long` returns `Unknown` before `Enum.TryParse`. A longer digit string makes `Enum.TryParse` return false, and the name check would still reject a numeric spelling that is not the member name. | reject |
| ECH-01 | false | Same duplicate-property claim as BH-03. A probe of `JsonSerializerDefaults.Web` on this SDK kept the last value (`AllowDuplicateProperties=True`), which is the declared bind. | reject |
| ECH-02 | medium | Same unpinned `AgentResponseMode` and `AgentLifecycleStatus` ordinals as BH-05 and BH-06. Numeric `1` is now part of setup idempotency identity. | patch |
| VG-01 | medium | Pre-verified. Nothing asserts that `{"value":1}` and `{"value":2}` share one activation or disable intent, or that the second activation HTTP submit replays with one execution. Restoring a raw payload hash would leave the remaining activation checks green. | patch |

## Design Notes

Semantic identity is the command after declared-contract normalization, then the existing syntax canonicalizer. Key order is already canonical. Property spelling is not. Observe retention tier and canonical bytes on the existing gateway HTTP path; do not add a store.

## Verification

**Commands:**
- `dotnet test test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj --filter-class Hexalith.Agents.Server.Tests.AgentsEventStoreGatewayIntegrationTests --filter-class Hexalith.Agents.Server.Tests.AppHostSecurityTopologyTests` -- expected: replay, conflict, extension-policy, and host-composition tests pass.
- `dotnet test test/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --filter-class Hexalith.Agents.Contracts.Tests.AgentOperationContractsTests` -- expected: explicit ordinals and wide-token degradation pass.
