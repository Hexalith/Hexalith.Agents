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

### Review Findings

- [x] [Review][Patch] Unpinned setup-surface enums can still be renumbered [src/Hexalith.Agents.Contracts/Agent/ApproverPolicyBasisDisclosure.cs:24]
- [x] [Review][Patch] Create canonical intent is not asserted [test/Hexalith.Agents.Server.Tests/AgentsEventStoreGatewayIntegrationTests.cs:205]
- [x] [Review][Patch] A payload that is not the declared command is untested [src/Hexalith.Agents.EventStore/AgentSetupIdempotencyIntentAdapter.cs:69]

**Rejected**

- Composition proof never builds `Program.cs` — `false`. `ServerHostShouldDelegateTheStatusReaderFallbackToSetupComposition` builds `AgentDomainHostComposition` and asserts both adapter collections are empty. Removing `Configure` from `Program.cs` leaves those collections empty too, and `GetServices` still sees a registration when `ValidateOnBuild` is false.
- Empty configuration hides a live adapter registration — `false`. The `Agents:EventStore:BaseUrl` branch registers a gateway client and dispatcher, not `IIdempotencyIntentAdapter` or `ITrustedCommandExtensionPolicy`.
- Verification commands pass `--filter-class` — rejected. The only correction is editing this spec.
- The Code Map and open triage rows disagree with `status: done` — rejected. The only correction is editing this spec.
- Duplicate `Mode` and `mode` keys collapse to the last value — `low`. `JsonSerializerDefaults.Web` keeps the last value (`AllowDuplicateProperties=True`), which is the declared bind. Rejecting both spellings needs a new guard, and everyday clients do not send both.
- A whitespace-padded enum name shares an intent — `false`. `FromName` trims by design, so the padded token is that declared member. The padded display-name assertion covers a free-text field.
- In-flight create and update payloads that omitted nulls are unseeded — `low`. Re-serialization writes those nulls (`DefaultIgnoreCondition=Never`), and the spec wants that byte mismatch to conflict. The shared HTTP conflict test already requires a mismatch to return 409.
- New deferred-work bullets hide the payload-tenant hash and have no status — `false` for the hash claim. The bullet names `CreateAgent.TenantId` and says this slice hashes the declared payload. Neighboring bullets in that section also omit `status`, and `source_spec` names this story.

### Review Findings

- [x] [Review][Patch] A permanent payload or domain defect is reported as a retryable outage [references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Pipeline/SubmitCommandHandler.cs:87]
- [x] [Review][Patch] Wide tokens that truncate to a declared ordinal are untested [src/Hexalith.Agents.Contracts/Serialization/UnknownFallbackEnumConverter.cs:50]
- [x] [Review][Patch] The unsigned-enum test never reads a positive in-range ordinal [test/Hexalith.Agents.Contracts.Tests/AgentOperationContractsTests.cs:526]
- [x] [Review][Patch] The numeric gateway replay does not require the original receipt [test/Hexalith.Agents.Server.Tests/AgentsEventStoreGatewayIntegrationTests.cs:402]
- [x] [Review][Patch] Declared-command collapse for unknown mode tokens and omitted nulls is unasserted [test/Hexalith.Agents.Server.Tests/AgentsEventStoreGatewayIntegrationTests.cs:228]
- [x] [Review][Defer] A create payload tenant can disagree with the envelope tenant [src/Hexalith.Agents/Agent/AgentAggregate.cs:118] — deferred: pre-existing. The aggregate stores `CreateAgent.TenantId` while idempotency scopes the target by the envelope tenant, and this spec forbids changing Party identity.

**Rejected**

- Plus-prefixed and over-long integer strings fall through to `Enum.TryParse` — `false`. Those strings do reach `TryParse`, and it returns false, so `"+9223372036854775808"`, a 30-digit string, and `"+18446744073709551615"` still deserialize to `Unknown`.
- `1.0` and `1e1` collapse to the zero sentinel — `false`. They are not integer ordinal tokens. `TryGetInt32`, `TryGetInt64`, and `TryGetUInt64` all fail, and the converter returns `Unknown`. Bare `1` still reads as `Automatic`.
- Composition proof never boots `Program.cs` and ignores the `BaseUrl` branch — `low`. `AddAgentSetupServices` registers a gateway client and dispatcher on that branch, not admission adapters, and the spec's proof is the composed graph the test already builds. An executable host harness is more than a direct correction.
- Spec verification commands, the empty change log, and `review_loop_iteration: 0` disagree with `status: done` — rejected. The only correction is editing this spec.
- The Code Map still describes the pre-change converter and theories — rejected. The only correction is editing this spec.
- The CI deferred bullet leaves the unpublished-release signal undecided — `low`. This slice is forbidden to change CI, and the choice is already recorded in `deferred-work.md`. Closing it is a workflow policy change, not a direct correction of this diff.

## Implementation Notes

- Setup adapters now pass the declared command type into `AgentSetupIdempotencyIntentAdapter`, which deserializes the payload with `EventStorePayloadSerialization.Options` and re-serializes that contract before the existing canonical encoder. Equivalent spelling and key order share one intent. An in-flight digest built from the previous raw payload conflicts on the gateway HTTP path and does not execute.
- `AgentSetupWriteEffect`, `AgentOperationStatus`, and `AgentOperationErrorCode` pin their existing ordinals. `UnknownFallbackEnumConverter` reads `long` and `ulong` tokens that do not fit `Int32` and returns `Unknown` without casting them down.
- The complete setup-view enum graph now has pinned ordinal assertions, including explicit `ApproverPolicyBasisDisclosure` values. Create-command normalization and incompatible payload rejection are covered directly at the adapter registry boundary.
- Domain-host registration moved unchanged into `AgentDomainHostComposition`. `ServerHostShouldDelegateTheStatusReaderFallbackToSetupComposition` builds that graph and asserts no idempotency adapters or trusted-extension policies are registered.
- Verification results are recorded in the generated test-evidence block under the Dev Agent Record.

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
| VG2-01 | low | Pre-verified: the composition test calls `AgentDomainHostComposition.Configure` directly, so deleting the production call would not fail this focused test. The approved proof target is the composed domain graph rather than executable-entrypoint bootstrapping, and adding a second host harness is disproportionate to this hypothetical regression. | reject |
| ECH2-01 | defer | carried: `CreateAgent.TenantId` can differ from the envelope tenant, as already recorded by BH-11 and the existing deferred-work entry; this slice deliberately does not choose event tenant identity. | defer |
| ECH2-02 | medium | `FromOrdinal(-1)` can create a declared `ulong.MaxValue` member and then throw from `Convert.ToInt64`, violating the converter's total fallback contract. | patch |
| ECH2-03 | low | The test builds the extracted composition directly and therefore would not detect deletion of the call in `Program.cs`. That is a hypothetical entrypoint-wiring regression outside the approved composed-graph isolation proof, and an executable host harness is not a proportionate patch here. | reject |
| BH2-01 | false | `DescriptorVersion` versions the descriptor field schema, which is unchanged; declared-command normalization changes the semantic payload value while intentionally preserving the same operation identity so prior admitted keys conflict. | reject |
| BH2-02 | false | The frozen intent explicitly hashes declared command semantics, not the aggregate's later storage normalization. Prior BH-02 also records that default-bound command values remain part of that declared object identity. | reject |
| BH2-03 | defer | carried: the payload/envelope tenant mismatch is the same claim already recorded by BH-11 and the existing deferred-work entry. | defer |
| BH2-04 | false | The cited `null` and array payloads are rejected as HTTP 400 before mediation because `SubmitCommandRequestValidator` requires an object payload; the direct adapter tests intentionally verify its typed fail-closed boundary. | reject |
| BH2-05 | false | The gateway assertion proves the coordinator received the digest derived from the resolved descriptor. Exact encoder bytes are not this operation's contract and intentionally changed in this story. | reject |
| BH2-06 | low | Rebuilding old raw-payload bytes with the shared syntax encoder does not freeze a historical byte fixture, but the required behavior is the raw-versus-declared digest mismatch and conflict; a literal encoder snapshot would add brittle duplication without changing that proof. | reject |
| BH2-07 | false | EventStore's `AdmitAsync_LiveDifferentIntent_ReturnsConflictWithoutMutation` already covers a terminal admission with a different digest, while this integration test proves the prior Agents encoder produces that different digest. The actor compares intent before replay state. | reject |
| BH2-08 | low | The focused composition test would stay green if `Program.cs` stopped calling the helper, but executable-entrypoint bootstrapping is not the approved isolation proof and no current wiring is missing. | reject |
| BH2-09 | low | The diff moves the full registration block unchanged and the focused test verifies the story-relevant absence/presence seams. No omitted registration was identified; full graph parity would be a broad hypothetical regression harness. | reject |
| BH2-10 | medium | `AgentInspectionStatus` is shipped directly by `AgentSetupResult` yet leaves its nonzero ordinals implicit, contrary to the approved requirement to pin every shipped setup enum. | patch |
| BH2-11 | medium | The hand-maintained ordinal assertions do not prove that every enum discovered through the setup-contract graph is included, so another setup enum can be added without entering the pin guard. | patch |
| BH2-12 | false | `Claims` has one command-independent non-activation branch: all activation-only keys are rejected for every non-`ActivateAgent` command before key-specific parsing. Existing rows exercise every command and the distinct branch, so the omitted Cartesian pairs add no behavior. | reject |
| BH2-13 | medium | The same unsigned-backing defect as ECH2-02 lets a negative ordinal reach a declared `ulong.MaxValue` and overflow instead of returning `Unknown`. | patch |
| BH2-14 | false | carried: as BH-08 records, the Code Map is historical pre-change planning context; the completed tasks and Implementation Notes are the artifact's implementation account. | reject |

## Design Notes

Semantic identity is the command after declared-contract normalization, then the existing syntax canonicalizer. Key order is already canonical. Property spelling is not. Observe retention tier and canonical bytes on the existing gateway HTTP path; do not add a store.

## Verification

**Commands:**
- `dotnet test test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj --filter-class Hexalith.Agents.Server.Tests.AgentsEventStoreGatewayIntegrationTests --filter-class Hexalith.Agents.Server.Tests.AppHostSecurityTopologyTests` -- expected: replay, conflict, extension-policy, and host-composition tests pass.
- `dotnet test test/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --filter-class Hexalith.Agents.Contracts.Tests.AgentOperationContractsTests` -- expected: explicit ordinals and wide-token degradation pass.

## Dev Agent Record

<!-- dev-agent-test-evidence:start -->
### Latest Release Test Evidence

Run (UTC): 2026-09-22T17:16:18Z

| Test project | Total | Passed | Failed | Skipped | Pending | Other |
|---|---:|---:|---:|---:|---:|---:|
| Hexalith.Agents.Client.Tests | 6 | 6 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.Contracts.Tests | 647 | 647 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.Server.Tests | 608 | 608 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.Tests | 805 | 805 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.UI.Tests | 1099 | 1099 | 0 | 0 | 0 | 0 |
| **Total** | 3165 | 3165 | 0 | 0 | 0 | 0 |

Result: PASS
<!-- dev-agent-test-evidence:end -->
### File List

- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-3.md`
- `src/Hexalith.Agents.Contracts/Agent/ApproverPolicyBasisDisclosure.cs`
- `src/Hexalith.Agents.Contracts/Agent/AgentInspectionStatus.cs`
- `src/Hexalith.Agents.Contracts/Agent/AgentActivationBlocker.cs`
- `src/Hexalith.Agents.Contracts/Agent/AgentLaunchReadinessBlocker.cs`
- `src/Hexalith.Agents.Contracts/Agent/AgentLifecycleStatus.cs`
- `src/Hexalith.Agents.Contracts/Agent/AgentResponseMode.cs`
- `src/Hexalith.Agents.Contracts/Agent/AgentSetupWriteEffect.cs`
- `src/Hexalith.Agents.Contracts/Agent/AgentSetupWriteStatus.cs`
- `src/Hexalith.Agents.Contracts/Operations/AgentOperationErrorCode.cs`
- `src/Hexalith.Agents.Contracts/Operations/AgentOperationStatus.cs`
- `src/Hexalith.Agents.Contracts/Serialization/UnknownFallbackEnumConverter.cs`
- `src/Hexalith.Agents.EventStore/ActivateAgentIdempotencyIntentAdapter.cs`
- `src/Hexalith.Agents.EventStore/AgentSetupIdempotencyIntentAdapter.cs`
- `src/Hexalith.Agents.EventStore/ConfigureAgentResponseModeIdempotencyIntentAdapter.cs`
- `src/Hexalith.Agents.EventStore/CreateAgentIdempotencyIntentAdapter.cs`
- `src/Hexalith.Agents.EventStore/DisableAgentIdempotencyIntentAdapter.cs`
- `src/Hexalith.Agents.EventStore/UpdateAgentConfigurationIdempotencyIntentAdapter.cs`
- `src/Hexalith.Agents.Server/Composition/AgentDomainHostComposition.cs`
- `src/Hexalith.Agents.Server/Program.cs`
- `test/Hexalith.Agents.Contracts.Tests/AgentOperationContractsTests.cs`
- `test/Hexalith.Agents.Server.Tests/AgentsEventStoreGatewayIntegrationTests.cs`
- `test/Hexalith.Agents.Server.Tests/AppHostSecurityTopologyTests.cs`
- `references/Hexalith.EventStore`
