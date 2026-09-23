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

### Review Findings (2026-09-23)

- [x] [Review][Patch] The production `Program.cs` call to `AgentDomainHostComposition.Configure` is unguarded after the test refactor [test/Hexalith.Agents.Server.Tests/AppHostSecurityTopologyTests.cs:42] — patched: the host topology test now checks the entrypoint call alongside the composed service graph.
- [x] [Review][Defer] CI does not run the Story 5.2 focused verifier [.github/workflows/ci.yml:64] — deferred: pre-existing. The policy job invokes only `-PackageFloorOnly` and the Story 5.1 floor; a missing 5.2 class can remain above those broad counts. The existing CI evidence row tracks this gap.
- [x] [Review][Defer] The Story 5.2 regression lane accepts skipped or undiscovered tests [eng/verify-story-5.2.ps1:357] — deferred: pre-existing. Unlike the focused lane, its project-level `dotnet test` calls have neither `--fail-skips on` nor a minimum count; the existing CI evidence row tracks the verifier gap.
- [x] [Review][Defer] A trusted create payload can store a tenant different from its envelope tenant [src/Hexalith.Agents.EventStore/AgentSetupIdempotencyIntentAdapter.cs:52] — deferred: pre-existing. `AgentAggregate.Handle(CreateAgent)` stores the payload tenant while admission scopes the target by the envelope tenant; this is already recorded in deferred work.
- [x] [Review][Defer] Domain rejection remains unverifiable while the command-status reader is deferred [src/Hexalith.Agents.Server/Composition/AgentDomainHostComposition.cs:204] — deferred: pre-existing. The moved registration still installs `DeferredAgentCommandStatusReader`; DW-7, DW-12, DW-19, DW-24, and DW-25 cover the live binding and terminal mapping.
- [x] [Review][Defer] A missing inspection status can deserialize as `Success` [src/Hexalith.Agents.Contracts/Agent/AgentInspectionStatus.cs:11] — deferred: pre-existing. `Success = 0` remains the positional-record default, so a malformed response can report success status with no setup payload; the existing enum-compatibility ledger records the deliberate exclusion.
- [x] [Review][Defer] Six public operation enums retain implicit numeric ordinals [test/Hexalith.Agents.Contracts.Tests/AgentOperationContractsTests.cs:384] — deferred: pre-existing. Their numeric readers can reinterpret a shipped value after a future member insertion; the 5.2 pin test covers setup enums only, and this gap is already recorded in deferred work.

**Rejected**

- `AgentSetupWriteStatus.Submitted = 0` can arise from a missing wire field — `false`. Current setup and provider-catalog gateways construct this UI result in process; no current reader deserializes a missing `Status` into it. DW-11 already tracks migration if it becomes a wire contract.
- A future optional command property makes today's normalization wrong — `false`. No such property is added in this diff; the approved contract intentionally conflicts with prior canonical bytes, and any later schema change needs its own identity decision.
- The gateway replay test must persist and reload admission state — `low`, rejected. This focused test exercises HTTP admission semantics with an in-memory ledger; live persistence and restart evidence belong to the separately deferred integration tier, so adding that harness is disproportionate to this normalization change.
- Gateway registration violates this follow-up's scope — `false`. The spec's File List explicitly attributes the padded-ID and different-app-ID guards to separate work in the baseline range; repeated registration with the same ID is still accepted.
- CI, release, Dependabot, and Builds changes violate this follow-up's scope — `false`. The spec's File List explicitly identifies those paths as separate work included by the shared baseline range.

## Implementation Notes

- Setup adapters now pass the declared command type into `AgentSetupIdempotencyIntentAdapter`, which deserializes the payload with `EventStorePayloadSerialization.Options` and re-serializes that contract before the existing canonical encoder. Equivalent spelling and key order share one intent. An in-flight digest built from the previous raw payload conflicts on the gateway HTTP path and does not execute.
- `AgentSetupWriteEffect`, `AgentOperationStatus`, and `AgentOperationErrorCode` pin their existing ordinals. `UnknownFallbackEnumConverter` reads `long` and `ulong` tokens that do not fit `Int32` and returns `Unknown` without casting them down.
- The complete setup-view enum graph now has pinned ordinal assertions, including explicit `ApproverPolicyBasisDisclosure` values. Create-command normalization and incompatible payload rejection are covered directly at the adapter registry boundary.
- Domain-host registration moved unchanged into `AgentDomainHostComposition`. `ServerHostShouldDelegateTheStatusReaderFallbackToSetupComposition` builds that graph and asserts no idempotency adapters or trusted-extension policies are registered.
- Verification results are recorded in the generated test-evidence block under the Dev Agent Record.

## Spec Change Log

- 2026-09-23 resumed review: retained all semantic-normalization and enum behavior. Corrected the package-count assertion for CRLF checkouts and moved the unsigned test enum into its own documented file. The existing host-entrypoint assertion was preserved. Full Debug/source verification passed; frozen intent is unchanged.

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

| BH3-01 | medium | The new package-count regex anchors digits directly to `$`, so a valid CRLF workflow fails on Windows. Accepting an optional carriage return is a direct test correction. | patch |
| BH3-02 | medium | CI invokes the package-floor-only mode and broad Story 5.1 counts, not the Story 5.2 focused class checks. This predates this resumed normalization follow-up and belongs to the separately split CI work. | defer |
| BH3-03 | medium | The verifier regression loop omits skip rejection and minimum counts; only focused classes enforce them. This inherited verification gap predates the normalization follow-up. | defer |
| BH3-04 | false | Commit `2e3fd00ccc07f88e00824e5dfbca059e452af6a0` deliberately removes the readiness gate and its override as separate work. No active gate invocation remains in the loaded workflow; historical evidence text does not restore the removed requirement. | reject |
| BH3-05 | medium | A Builds gitlink-only Dependabot proposal fails the literal workflow/test SHA checks until the coupled pins are updated. The gate correctly refuses inconsistent pins, but dependency maintenance needs a documented/manual or automated coupled-update path; this is inherited CI work. | defer |
| BH3-06 | low | The HTTP fixture proves one gateway dispatch and receipt replay, but uses a simulated ledger rather than persisted events. A durable runtime harness is the separately deferred live-integration tier; introducing that harness is more than a direct correction to this normalization follow-up. | reject |
| BH3-07 | false | `SubmitCommandHandlerIdempotencyAdmissionTests.Handle_PermanentAdmissionRejectionIsNotRetryable` at lines 512–531 explicitly makes descriptor resolution throw `ArgumentException` and asserts HTTP 400, `Retryable=false`, and `correct_request`. The Agents adapter tests pin that exception boundary; the claimed absence of mapping regression coverage is disproved by the platform test. | reject |
| BH3-08 | false | carried: the configured `AddAgentSetupServices` branch registers the live dispatcher and operations, not admission adapters, and `AgentSetupCompositionTests.A_configured_gateway_resolves_the_live_dispatcher_and_administration_operations` resolves both. The moved host composition adds no configured admission branch. | reject |
| BH3-09 | low | The newly added unsigned test enum is nested in a second type's file despite the explicit single-type-per-file rule. Moving it to a named file is a direct correction with no behavior change. | patch |
| BH3-10 | false | The deferred ledger is append-only in this workflow, and its older undecided-publication wording records the state when that work was split. Later CI changes are explicitly recorded as separate baseline work; rewriting or consolidating those historical entries would violate this workflow. | reject |
| ECH3-01 | medium | Same demonstrated CRLF package-count failure as BH3-01; group with that direct correction. | patch |
| VG3-01 | medium | Pre-verified: no executable regression test exercises the focused runner's skip/non-discovery failure behavior. This runner hardening is separately split CI/tooling work that predates the normalization follow-up; add fixture-based verification in that workstream. | defer |

## Design Notes

Semantic identity is the command after declared-contract normalization, then the existing syntax canonicalizer. Key order is already canonical. Property spelling is not. Observe retention tier and canonical bytes on the existing gateway HTTP path; do not add a store.

## Verification

**Executed verification (2026-09-23):**
- `dotnet build Hexalith.Agents.slnx -c Debug -m:1 -p:UseHexalithProjectReferences=true --nologo` — passed with zero warnings/errors.
- Each root test project was run individually through `dotnet test/<project>/bin/Debug/net10.0/<project>.dll -noLogo -noColor -failSkips -ctrf <report>` after that build. CTRF summaries and individual test statuses were checked: no failures, skips, pending, or unrun tests.
- Focused xUnit classes were also run directly with `-class` for `AgentsEventStoreGatewayIntegrationTests`, `AppHostSecurityTopologyTests`, and `AgentOperationContractsTests`. The CRLF correction passed both LF/CRLF theory cases; unsigned-enum tests passed after relocation.
- Initial source build with `--no-restore` encountered stale Conversations contract references. A normal restore/build resolved it; final solution verification has no build blocker.
- Local validation uses Debug and source references. The earlier Release/package evidence is historical; no new Release/package run or live-platform test is claimed.

**Matrix audit:** every covering test below appears as passed in the current CTRF output.

| Matrix row | Covering test(s) |
| --- | --- |
| Equivalent setup JSON | `Equivalent_setup_spelling_replays_on_the_gateway_and_a_semantic_change_does_not_append_again`; `Declared_command_spelling_shares_one_canonical_intent_and_a_semantic_change_does_not` |
| In-flight key | `An_in_flight_key_from_the_previous_encoder_conflicts_without_a_second_execution` |
| Non-agent domain | `A_non_agent_domain_is_rejected_by_the_setup_adapter`; `A_non_agent_domain_fails_on_the_gateway_before_execution` |
| Activation-only key | `Reserved_extension_policy_accepts_only_the_exact_Dapr_identity_command_key_and_value` create/disable/response-mode rows |
| Mixed Dapr identity | `Reserved_extension_policy_rejects_missing_duplicate_or_foreign_identity_claims` |
| Wide enum token | `UnrecognizedOperationEnumValuesDegradeToUnknownInsteadOfThrowing`; `ShippedSetupEnumsKeepTheirPinnedOrdinals`; signed/unsigned wide-ordinal theories |
| Domain host | `ServerHostShouldDelegateTheStatusReaderFallbackToSetupComposition` |

Review: three independent layers completed. BH3-01/ECH3-01 and BH3-09 are patched; CI/verifier and coupled dependency-update follow-ups are recorded in deferred work. External Platform registration, tenant-scoped command-status binding, and immutable provisioning remain broader Story 5.2 promotion prerequisites.

## Dev Agent Record

<!-- dev-agent-test-evidence:start -->
### Latest Debug Source Test Evidence

Run (UTC): 2026-09-23T12:39:56.614394+00:00

| Test project | Total | Passed | Failed | Skipped | Pending | Other |
|---|---:|---:|---:|---:|---:|---:|
| Hexalith.Agents.Client.Tests | 6 | 6 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.Contracts.Tests | 687 | 687 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.Server.Tests | 639 | 639 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.Tests | 805 | 805 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.UI.Tests | 1099 | 1099 | 0 | 0 | 0 | 0 |
| **Total** | 3236 | 3236 | 0 | 0 | 0 | 0 |

Result: PASS
<!-- dev-agent-test-evidence:end -->
### File List

#### Follow-up -3 paths

These paths contain follow-up -3 work. `deferred-work.md` and the EventStore gitlink also have changes from other work in the baseline range; the gate compares whole paths.

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
- `test/Hexalith.Agents.Contracts.Tests/WideSignedToleranceStatus.cs`
- `test/Hexalith.Agents.Contracts.Tests/UnsignedBackedToleranceStatus.cs`
- `test/Hexalith.Agents.Server.Tests/AgentsEventStoreGatewayIntegrationTests.cs`
- `test/Hexalith.Agents.Server.Tests/AppHostSecurityTopologyTests.cs`
- `references/Hexalith.EventStore`

#### Other paths required by the baseline, outside follow-up -3

These paths were changed by CI/release, submodule-pointer, follow-up -2, or later registration-policy work. They are included only for the gate's path reconciliation and are not claimed as follow-up -3 work. The registration-policy changes add whitespace rejection and duplicate-registration checks, which the frozen intent excludes.

- `.github/dependabot.yml`
- `.github/workflows/ci.yml`
- `.github/workflows/release.yml`
- `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
- `eng/verify-story-5.2.ps1`
- `references/Hexalith.Builds`
- `references/Hexalith.Conversations`
- `references/Hexalith.FrontComposer`
- `references/Hexalith.Memories`
- `references/Hexalith.Tenants`
- `src/Hexalith.Agents.EventStore/AgentsEventStoreServiceCollectionExtensions.cs`
- `src/Hexalith.Agents.EventStore/AgentsTrustedCommandExtensionPolicy.cs`
- `test/Hexalith.Agents.Server.Tests/PackageInventoryTests.cs`

#### Additional baseline paths from separate completed work

These existing baseline changes are preserved and are not introduced by this resumed normalization follow-up. Deleted readiness tooling belongs to the separately committed removal of that gate.

- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad/custom/bmad-code-review.toml` (deleted)
- `src/Hexalith.Agents.Contracts/Agent/Events/Rejections/AgentLifecycleStateAlreadySetRejection.cs`
- `src/Hexalith.Agents.Server/Application/Agents/AgentActivationProviderRevalidation.cs`
- `test/Hexalith.Agents.Server.Tests/EventStoreAgentAdministrationOperationsTests.cs`
- `tests/tooling/story_review_readiness/__init__.py` (deleted)
- `tests/tooling/story_review_readiness/story_review_readiness_test.py` (deleted)
- `tools/check-story-review-readiness.py` (deleted)
