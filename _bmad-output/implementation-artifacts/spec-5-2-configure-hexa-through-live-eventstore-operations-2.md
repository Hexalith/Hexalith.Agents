---
title: '5.2 Correlate Setup Writes With Their Exact Projected Outcome'
type: 'feature'
created: '2026-09-14'
status: 'done'
route: 'dispatch'
baseline_commit: '599208dd40efadef728363c227a0f75ebd888337'
review_loop_iteration: 1
context:
  - '_bmad-output/implementation-artifacts/epic-5-context.md'
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-state-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 5.2 setup writes are confirmed by a locally guessed `ConfigurationVersion + 1`. A no-op can therefore poll forever, a lost acknowledgement cannot be retried safely by the UI, and an unrelated concurrent write can satisfy the guessed threshold.

**Approach:** Return the actual applied/no-op result and resulting configuration version through EventStore's existing `DomainResult.ResultPayload`, preserve the canonical gateway receipt through Agents, and make the UI retain one ULID idempotency key and exact payload until that command-derived target is projected or fails closed.

**Decision (2026-09-14):** Deliver exact write/outcome/projection correlation as the current slice. Defer `ProvisionHexa` and legacy Party link/replace retirement together until Platform authority and `EXT-PARTIES-1` are concrete; do not retire the only current identity path before its replacement exists.

## Boundaries & Constraints

**Always:** Authorize before dispatch/read access; use the canonical EventStore receipt and ULID identities; keep V1 additions backward-compatible with `Unknown = 0`; keep aggregates pure and reads behind `IReadModelStore`; preserve API/client/UI parity, the 250 ms / 8 s catch-up contract, English/French whole strings, and lifecycle-versus-callability separation. A missing legacy result payload is unverifiable.

**Never:** Confirm from a locally predicted version; mint a new key or change payload while an outcome is unresolved; expose infrastructure metadata; add command-status/sequence machinery when existing result-payload/idempotency facilities suffice; change Party identity behavior; initialize nested submodules.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Eventful write | Canonical receipt carries `Applied` and target version | Poll until the projection reaches that version | Missing/malformed payload is `UnableToVerify` |
| No-op | Receipt carries `AlreadyApplied` and unchanged version | Stop and render localized no-change outcome | Never poll for an unreachable version |
| Domain rejection | EventStore returns a typed rejection | Preserve prior setup and show the safe mapped failure | Never render Submitted or Success |
| Lost acknowledgement | UI retains one key and payload | Retry only that pair for idempotent replay | Block different submission until resolved/abandoned |
| Concurrent write | Another command advances projection | Confirm only against the original command-derived target | Local N+1 is forbidden |
| Catch-up exhaustion | Target absent after 8 seconds | Keep attempt; show `awaiting projection` and refresh/retry | Never imply Success |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Agents/Agent/AgentAggregate.cs` plus a focused result type -- enrich live setup success/no-op `DomainResult`s with effect and authoritative version; keep rejections unchanged.
- `src/Hexalith.Agents.Server/Ports/{IAgentCommandDispatcher,EventStoreAgentCommandDispatcher,IAgentCommandIdentityFactory}.cs` -- return/validate the canonical `SubmitCommandResponse`; replace GUID identities with ULIDs.
- `src/Hexalith.Agents.Server/Application/Agents/` outcome/orchestrator types -- thread one receipt; parse it only at the operations boundary.
- `src/Hexalith.Agents.Contracts/{Operations/AgentCommandAcceptance.cs,Agent/AgentSetupWriteResult.cs}` -- add compatible target-version and effect evidence with `Unknown = 0`.
- `src/Hexalith.Agents.Server/Application/Agents/EventStoreAgentAdministrationOperations.cs` and `Api/AgentsOperationEndpoints.cs` -- validate receipt identity, bind idempotency/correlation metadata, and map unverifiable payloads safely.
- `src/Hexalith.Agents.UI/Services/Gateways/`, `Components/Pages/AgentConfiguration.razor`, and `Resources/AgentsResources*.resx` -- retain exact attempts, terminate no-op, poll for 8 seconds, block conflicts, and provide accessible recovery.
- `test/Hexalith.Agents.{Tests,Contracts.Tests,Server.Tests,UI.Tests}` and `eng/verify-story-5.2.ps1` -- cover payload, receipt, no-op, rejection, concurrency, retry, timeout, disclosure, and persisted end state.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Agents/Agent/` -- emit bounded applied/no-op result payloads for every live setup mutation.
- [x] `src/Hexalith.Agents.Contracts/`, `Server/`, and `Client/` -- preserve canonical receipts and expose only identity, target version, and safe effect.
- [x] `src/Hexalith.Agents.UI/` -- implement retained-key exact-intent handling, bounded catch-up, no-op termination, and accessible recovery.
- [x] `test/` and `eng/verify-story-5.2.ps1` -- prove the matrix, no-disclosure, compatible serialization, replay, and persisted end state.

**Acceptance Criteria:**
- Given an applied setup command, when EventStore completes it, then public acceptance carries its canonical receipt and authoritative resulting version.
- Given no-op, rejection, malformed legacy payload, lost acknowledgement, or concurrent write, when handled, then the attempt terminates or remains fail-closed without false confirmation or duplication.
- Given projection lag, when bounded polling expires, then localized `awaiting projection` retains the attempt and exposes accessible recovery.

### Review Findings

Code review of Group 1 (Domain + Contracts), 2026-09-14. Four layers, none failed.

**Decision needed**

- [x] [Review][Decision] No-op `ResultPayload` is dropped by `AggregateActor`, so `AlreadyApplied` is unreachable end to end — `AgentSetupDomainResult.AlreadyApplied` always passes `[]`, so `DomainResult.IsNoOp` (`DomainResult.cs:54`) is true and `AggregateActor.cs:1082` calls `CompleteTerminalAsync` without the optional `resultPayload:` argument that the eventful branch at `:1358` does pass. `TryParseSetupResult` then fails and every no-op returns `UnableToVerify`. Worse, the idempotency record stores the null payload, so the exact-key retry the UI is built around replays that null forever and can never resolve. All four review layers found this independently. Defeats the frozen matrix no-op row and AC2. The fix belongs in `references/Hexalith.EventStore`, a separate repository boundary, so it cannot land in this story's commit.
- [x] [Review][Decision] A domain rejection cannot be distinguished from a missing payload — `SubmitCommandResponse` carries only `CorrelationId`, `ResultPayload` and `MessageId`; rejection detail lives in `CommandStatusRecord.RejectionEventType`, which Agents never reads. `EventStoreAgentAdministrationOperations.cs:320` has no rejection branch, so a rejected `ActivateAgent` or duplicate `CreateAgent` surfaces as retryable `UnableToVerify` instead of the terminal mapped failure the frozen matrix requires. The only conflict test (`EventStoreAgentAdministrationOperationsTests.cs:272`) simulates an HTTP-level `EventStoreGatewayException(409)`, not a domain rejection, so nothing covers this seam. Resolution depends on the intended EventStore rejection contract.
- [x] [Review][Decision] Every accepted write now reports `AuthoritativePending`, including no-ops — baseline `599208d` returned `AgentSetupTruthState.Submitted` at this call site; HEAD returns `AuthoritativePending` for all accepted writes (`EventStoreAgentAdministrationOperations.cs:337`). The enum's own doc defines that as "the projected read model has not caught up", which is false for a no-op where no event was appended. This changes the value of an existing V1 field rather than only adding fields, and no contract test pins it. The correct value for the no-op case is a product call.
- [x] [Review][Decision] `AgentCommandAcceptance` ships two enum encodings in one JSON object — `AgentSetupWriteEffect` carries `[JsonConverter(typeof(JsonStringEnumConverter))]`; `AgentSetupTruthState` does not. The diff's own tests prove it: `AgentOperationContractsTests.cs:83` serializes with no options, and the legacy fixture at `:92` pins `"TruthState":1` while `Effect` serializes as `"Applied"`. Both fields cross the HTTP boundary in the same packable record. Either fix changes the wire format for deployed clients.
- [x] [Review][Decision] `AgentSetupWriteResult.Submitted(acceptance)` accepts unverified evidence, and a supported path reaches it — the diff's own `LegacyAgentCommandAcceptanceDeserializesWithUnknownEffectAndNoTargetVersion` test establishes `Effect = Unknown, TargetConfigurationVersion = null` as a supported deserialization outcome, and `AgentsClientSetupGateway.WriteAsync` calls `Submitted(acceptance)` on any `IsSuccess` without checking either field. It is rescued only downstream in one consumer (`AgentConfiguration.razor:392`), so the contract-level invariant lives in exactly one caller and a second will diverge. This supersedes the two prior triage rows that rejected it as reachable only by "a custom gateway bypassing the trusted boundary" — that refutation is contradicted by the new test.
- [x] [Review][Decision] New members on string-serialized enums break older deployed clients — `AgentOperationErrorCode.UnableToVerify` and the new `AgentOperationStatus` member are added to enums using a plain `JsonStringEnumConverter`, which throws `JsonException` on an unrecognized name instead of degrading to the documented `Unknown = 0` sentinel. This is the compatibility guarantee the spec names under **Always**. A tolerant converter is a public contract change.

**Patch**

- [x] [Review][Patch] No test drives the result payload through a real command pipeline [test/Hexalith.Agents.Tests/AgentSetupDomainResultTests.cs:38]
- [x] [Review][Patch] Result-payload wire format is duplicated across assemblies with no shared constant or round-trip test [src/Hexalith.Agents/Agent/AgentSetupDomainResult.cs:23]
- [x] [Review][Patch] Stale `<param name="status">The non-submitted outcome.</param>` doc after three non-submitted statuses became legal [src/Hexalith.Agents.Contracts/Agent/AgentSetupWriteResult.cs:52]
- [x] [Review][Patch] Table tests assert inside `foreach`, so the first failing command hides the rest; use `[Theory]` with `MemberData` [test/Hexalith.Agents.Tests/AgentSetupDomainResultTests.cs:38]
- [x] [Review][Patch] `using System.Text.Json.Serialization;` ordered after a project using, violating `.editorconfig:49`; plus a redundant `using System.Collections.Generic;` under `ImplicitUsings` [src/Hexalith.Agents.Contracts/Operations/AgentCommandAcceptance.cs:1]

**Deferred**

- [x] [Review][Defer] Eventful payload is dropped whenever the advisory status read is not `Completed` [references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Pipeline/SubmitCommandHandler.cs:538] — deferred: pre-existing, deliberate and separately tested EventStore gating, in a different repository boundary. This story's fail-closed correlation is what makes it operator-visible: a write that genuinely appended its event is reported as `UnableToVerify` when the advisory status write has not landed by the time it is read.

**Rejected**

- `AgentOperationOptions` documents a ULID requirement no code enforces — `false`. The guard exists immediately after the cited line: `EventStoreAgentAdministrationOperations.cs:281` runs `IsCanonicalUlid(messageId)` and returns `ValidationFailed`, with `:272` doing the same for the correlation id. Tests at `:359-391` cover both, including parseable-but-non-canonical values.
- `AgentsClientProviderCatalogGateway.ToWriteStatus` mislabels `UnableToVerify` as `Unavailable` — `false`. `ProviderCatalogAdministrationOrchestrator` never produces `UnableToVerify`, so that status cannot reach the catalog mapping. Raised by two layers and refuted by a third; the latent divergence only matters if catalog writes later adopt the same correlation.
- `AgentSetupWriteResult.Failed` accepts `Submitted`/`AlreadyApplied`/`AwaitingProjection` — `low`. No production caller passes a success-like status to `Failed`, and the fix adds an invariant branch for a path the implementation never exercises. Consistent with the two prior triage rows.
- `AgentSetupDomainResult.Applied` drops the empty-events guard and does not validate the version — `low`. Every current caller supplies a non-empty event list and a positive version, and the fix adds guards to an internal factory for a case not reachable today.
- `AlreadyApplied` returned while the projection still lags renders stale setup as confirmed — `false` in the sense that matters: the frozen I/O matrix explicitly requires a no-op to complete without polling ("Never poll for an unreachable version"). Changing it means editing the approved intent, which triage does not do.

---

Code review of Group 2 (Contracts + Domain, `599208d..eeb4f8f`), 2026-09-14, round 2. Four layers, none failed. Readiness gate passed (5 projects, 2665/2665, File List 71/71).

**Patch**

- [x] [Review][Patch] `UnknownFallbackEnumConverter` is bypassed wherever the server builds its own `JsonSerializerOptions` — STJ resolves `options.Converters` *before* a type-level `[JsonConverter]`, verified empirically on .NET 10 (`"Nope"` degrades to `Unknown` with the attribute alone, throws `JsonException` when `JsonStringEnumConverter` is in `Converters`). `AgentSetupView.TruthState` — the read model this story adds — round-trips through `AgentSetupQueryHandlerBase._jsonOptions` and `AgentSetupProjectionFold._jsonOptions`, both of which register `new JsonStringEnumConverter()`. The `Unknown = 0` degradation adopted to resolve the round-1 decision row therefore does not apply where the setup read model is actually deserialized. Same for `ProviderCatalogQueryHandlerBase.cs:32`, `ProviderCatalogProjectionFold.cs:30`, `AgentInteractionAuditQueryHandlerBase.cs:21`. [src/Hexalith.Agents.Server/Application/Queries/AgentSetupQueryHandlerBase.cs:39]
- [x] [Review][Patch] Five sibling public operation enums keep the throwing converter the change replaced — `AgentReadinessStatus`, `ProviderModelReadinessStatus`, `AgentCallOperationStatus`, `ProposalOperationStatus`, `AuditAvailabilityStatus` all document `Unknown = 0`, are each the `T` of an `AgentOperationResult<T>` on the same endpoints, and still throw on an unrecognized name. `UnrecognizedOperationEnumValuesDegradeToUnknownInsteadOfThrowing` asserts degradation for four types only and does not iterate the `enumTypes` array the sibling test uses. Exclude `AgentInspectionStatus` — its zero is `Success = 0`, so the fallback would fail open. [src/Hexalith.Agents.Contracts/Operations/AgentReadinessStatus.cs:6]
- [x] [Review][Patch] The new converter is more permissive than the reader written beside it in the same change — `Enum.TryParse` accepts comma-delimited name lists on a non-Flags enum, so `"Applied, AlreadyApplied"` yields undefined ordinal 3 (verified). `TryParseSetupResult` already guards exactly this with an `Enum.GetName(effect) == effectName` round-trip; the converter does not. Also: object/array/bool tokens still throw `JsonException` despite the type's summary promising degradation (verified), and `"1"` degrades while a bare `1` is accepted. [src/Hexalith.Agents.Contracts/Serialization/UnknownFallbackEnumConverter.cs:32]
- [x] [Review][Patch] Case-sensitivity narrowing on two already-shipped enums — `AgentOperationStatus` and `AgentOperationErrorCode` moved from the case-insensitive `JsonStringEnumConverter` to `ignoreCase: false`, so a differently-cased name now degrades to `Unknown` instead of parsing (verified both behaviors). It fails closed, but it is an unannounced narrowing shipped under a compatibility change and no test covers either direction. [src/Hexalith.Agents.Contracts/Serialization/UnknownFallbackEnumConverter.cs:39]
- [x] [Review][Patch] `AgentSetupTruthState`'s own XML doc contradicts the approved no-op mapping — the enum states `ProjectionConfirmed` "means the projected read model has caught up" and that it is the only evidence "that the durable read model reflects the change", while the approved behaviour maps an `AlreadyApplied` receipt to `ProjectionConfirmed` without reading the projection. The behaviour is settled by the frozen matrix; the doc in the packable assembly is not. [src/Hexalith.Agents.Contracts/Agent/AgentSetupTruthState.cs:15]
- [x] [Review][Patch] `AgentSetupWriteResult.Submitted`'s doc is stale after the factory gained a second outcome — it can now return `AgentSetupWriteStatus.AlreadyApplied` while `<returns>A submitted result.</returns>` is unchanged, and the record summary claims these outcomes "retain verified acceptance evidence" although the factory verifies nothing (the guard is in `AgentsClientSetupGateway.WriteAsync`). [src/Hexalith.Agents.Contracts/Agent/AgentSetupWriteResult.cs:30]
- [x] [Review][Patch] Redundant usings reintroduced under `ImplicitUsings` — the same round's patch list removed exactly this defect from `AgentCommandAcceptance.cs`. [src/Hexalith.Agents.Contracts/Serialization/UnknownFallbackEnumConverter.cs:1], [test/Hexalith.Agents.Tests/AgentSetupDomainResultTests.cs:1]
- [x] [Review][Patch] The new `UnableToVerify` error message is executed but never asserted — deleting the arm makes every unverifiable write fall to the generic "The operation failed." with no test failing; the server tests read `.Code`, never `.Message`, and `Operation_error_factory_uses_safe_messages` covers `Unavailable` alone. [src/Hexalith.Agents.Contracts/Operations/AgentOperationError.cs:34]
- [x] [Review][Patch] New contract tests pin a serialization shape that is not the transport shape — they use default options (PascalCase) while the route uses `JsonSerializerDefaults.Web` (camelCase), so "the legacy payload still round-trips" is proven against a fixture no deployed peer emits. [test/Hexalith.Agents.Contracts.Tests/AgentOperationContractsTests.cs:83]
- [x] [Review][Patch] The round-trip half of round-1's shared-constant patch is still open — `AgentSetupResultPayload` landed, but `AssertPayload` asserts the literals `"effect"`/`"configurationVersion"` and the server tests hand-build `new { effect, configurationVersion }`; no test feeds a real `AgentSetupDomainResult.ResultPayload` into `TryParseSetupResult`. [test/Hexalith.Agents.Tests/AgentSetupDomainResultTests.cs:302]

**Deferred**

- [x] [Review][Defer] The idempotency key is not bound to the payload it was first used with [src/Hexalith.Agents.Contracts/Operations/AgentOperationOptions.cs:17] — deferred: maybe-false, would be medium if true. `AgentOperationOptions` documents "An exact retry must reuse the same correlation ID, idempotency key, and payload", but nothing in this chunk enforces the payload half; a retry with the same key and a changed payload could be shown the first command's effect and version. Settled by inspecting EventStore's idempotency-record handling in `references/Hexalith.EventStore` for whether a replayed key with a different payload is rejected.
- [x] [Review][Defer] Caller-supplied `CorrelationId` is echoed on read paths without canonical-ULID validation [src/Hexalith.Agents.Server/Application/Agents/EventStoreAgentAdministrationOperations.cs:210] — deferred: maybe-false, would be medium if true. The write paths validate via `IsCanonicalUlid`; the cited read paths are outside this chunk. Settled by the Group 3 (Server) chunk review.
- [x] [Review][Defer] `AgentSetupWriteStatus` reserves zero for `Submitted` rather than `Unknown` [src/Hexalith.Agents.Contracts/Agent/AgentSetupWriteStatus.cs:8] — deferred: pre-existing. `Submitted = 0` was already declared inside `AgentSetupWriteResult.cs` at `baseline_commit`; this round only split the enum into its own file (per the one-type-per-file rule) and appended three members. It is UI-internal — the client gateway maps into it and it never crosses HTTP — so the fail-open default is not currently reachable from a wire payload.

**Rejected**

- `AgentSetupTruthState`'s ordinal-to-name wire change is an unflagged V1 break — previously decided. The break is real (verified: no global enum converter is registered for HTTP, so it previously emitted `2`), but round 1's decision row "AgentCommandAcceptance ships two enum encodings in one JSON object" was resolved by adopting one name-based encoding. Reverting it would undo an approved decision.
- `AlreadyApplied` renders a possibly-stale projection as confirmed — `false` in the sense that matters. The frozen I/O matrix's no-op row requires "Stop and render localized no-change outcome / Never poll for an unreachable version". Fixing it means editing the approved intent. Raised and rejected on these grounds in both prior rounds; the documentation half is kept as a patch above.
- `AgentsClientProviderCatalogGateway.ToWriteStatus` mislabels `UnableToVerify` as `Unavailable` — `false`. Verified: `EventStoreProviderCatalogOperations` returns only `NotAuthorized`, `Unavailable`, `ValidationFailed` and `Conflict`, so the arm is unreachable. The three `Agents.ProviderCatalog.Write.Status.*` strings added in en and fr exist only to satisfy a pre-existing test loop over every non-`Submitted` member. Carries round 1's identical refutation.
- `AgentSetupDomainResult.Applied` accepts an empty event list or a non-positive version — `low`. All 12 internal callers supply a non-empty list and a positive version, and `EveryEventfulSetupHandlerCarriesAppliedEffectAndItsEmittedVersion` pins both for every one of them. Rejected on the same grounds in the two prior rounds.
- `AgentSetupWriteResult.Submitted`/`Failed` accept states their docs forbid — `low`. `AgentsClientSetupGateway.WriteAsync` converts `Effect = Unknown` or a null target to `UnableToVerify` before constructing the result, verified at the only caller. The fix adds invariant branches for public-factory misuse no production path performs. Rejected in both prior rounds; the stale-doc half is kept as a patch above.
- Repeating `state.ConfigurationVersion + 1` at 12 call sites duplicates the version source of truth — `low`. The drift risk is real, but `EveryEventfulSetupHandlerCarriesAppliedEffectAndItsEmittedVersion` asserts payload version equals emitted event version for all 12 handlers, so drift in any existing handler fails the suite. Removing the duplication needs an event-level version interface — more than a direct correction.
- `AgentCommandAcceptance`'s non-disclosure summary is contradicted by `TargetConfigurationVersion` — `false`. `AgentSetupView.ConfigurationVersion` already exposes the same domain value publicly, from the same assembly, on the same endpoints; the summary's "revision" refers to the EventStore stream revision, which a domain configuration version is not.
- The converter loses round-trip fidelity for relayed unknown ordinals — `false`. Agents produces these values; no path re-emits a deserialized third-party status.
- `UnknownFallbackEnumConverter` lacks `ReadAsPropertyName`/`WriteAsPropertyName`, ignores `options`, and can emit JSON `null` — `low`. Verified that no enum-keyed dictionary exists for these types and that every one of them has a named zero member, so the null-write path is unreachable. The fix adds overrides for unused paths.
- Deferred gateways returning `Unavailable` now under-claim, since its doc no longer promises "nothing was mutated" — `low`. The weakened wording errs fail-closed; a distinct `NotBound` status adds public surface for no user-visible gain.
- `ProviderCatalogWriteResult`'s docs assert an invariant the shared enum no longer carries — `low`. The doc remains accurate for `ProviderCatalogWriteResult` itself, whose `Submitted` factory only ever yields `Submitted`; only the shared enum widened, and the fix edits a Story 5.3 type outside this chunk.
- `AgentSetupResultPayload` adds public API outside the spec's Code Map — `low`, and it is the accepted resolution of round 1's shared-constant patch; the fix would edit the spec under review.

## Implementation Notes

- Added the canonical `## Dev Agent Record` and nested `### File List` this story was missing, so the
  fail-closed readiness gate can run at all.
- The gate previously compared the File List against `git status` alone, which is empty for a story whose
  work is already committed. `tools/check-story-review-readiness.py` now reads the story's own
  `baseline_commit` frontmatter and unions the diff since that commit into the change set. Bidirectional
  exact equality is unchanged -- no exclusions, no bypass, and the File List is still never auto-edited.
- The last two File List entries are that gate repair itself; every other entry is Story 5.2 work committed
  between `599208d` and `e5857a7`.

**Resolutions applied 2026-09-14**

All six decision-needed items were resolved by the product owner and applied, together with all five patches.

| Finding | Resolution |
| --- | --- |
| No-op payload dropped | Patched `AggregateActor` to forward `resultPayload` on the no-op branch, plus an actor-level regression test. Verified the test fails without the fix and passes with it. |
| Domain rejection indistinguishable | Added `GetCommandStatusAsync` to `IEventStoreGatewayClient` and a new `IAgentCommandStatusReader` seam. A payload-less but identity-verified receipt now consults the recorded status and maps a rejection to `AgentOperationErrorCode.Rejected`; anything else stays `UnableToVerify`. The rejection event type is never surfaced. |
| `TruthState` for no-ops | `AlreadyApplied` now reports `ProjectionConfirmed`; applied writes keep `AuthoritativePending`. Both pinned by tests. |
| Mixed enum encodings + version skew | Added `UnknownFallbackEnumConverter<T>`, applied to `AgentSetupTruthState`, `AgentSetupWriteEffect`, `AgentOperationStatus` and `AgentOperationErrorCode`. Writes are by name; reads accept both name and ordinal, so the numeric legacy form still round-trips. |
| `Submitted(acceptance)` unverified evidence | The invariant moved up into `AgentsClientSetupGateway.WriteAsync`, which now returns `UnableToVerify` when effect or target version is missing rather than claiming progress. |
| Payload key duplication, docs, table tests, usings | Keys hoisted to `AgentSetupResultPayload`; stale `<param>` corrected; both table tests converted to `[Theory]`/`MemberData`; using order and a redundant using fixed. |

Note: the rejection fix required a second EventStore change. The originally chosen option was presented as needing none, which was wrong -- `IEventStoreGatewayClient` had no status-read method -- and the corrected choice was confirmed before it was applied.

**Round 2 patches applied 2026-09-14**

All ten round-2 patch findings were applied. No decision-needed items were raised in round 2.

| Finding | Resolution |
| --- | --- |
| Fallback converter bypassed by the server's own options | Added `UnknownFallbackEnumConverterFactory`, which resolves the attribute-declared converter from inside `JsonSerializerOptions.Converters`, and registered it ahead of `JsonStringEnumConverter` in all five server options bags. `ServerSerializationConformanceTests` reflects over every static `JsonSerializerOptions` in the server assembly and fails if any bag registers the string converter without the factory ahead of it, so options bags added later are covered too. Verified the guard fails when the factory is removed from one bag. |
| Five sibling operation enums kept the throwing converter | `AgentReadinessStatus`, `ProviderModelReadinessStatus`, `AgentCallOperationStatus`, `ProposalOperationStatus` and `AuditAvailabilityStatus` now declare `UnknownFallbackEnumConverter<T>`. `AgentInspectionStatus` is deliberately excluded -- its zero is `Success = 0`, so the fallback would fail open. The contract enum list is hoisted to one array both properties iterate. |
| Converter more permissive than the reader beside it | `FromName` now requires the parsed value's own name to match the input, which rejects the comma-delimited name lists `Enum.TryParse` accepts on a non-flags enum. Object, array, boolean and null tokens degrade to the sentinel instead of throwing (structured tokens are consumed with `Skip` first, so the reader cannot desynchronize), matching the type's documented promise. A quoted ordinal now reads exactly like a bare one. |
| Case-sensitivity narrowing | Reading is case-insensitive again, matching the `JsonStringEnumConverter` it replaced; the name round-trip check is case-insensitive too, so it still rejects comma lists. Both directions are now pinned by tests. |
| `AgentSetupTruthState` doc contradicted the approved no-op mapping | The enum now documents both routes to `ProjectionConfirmed` and states explicitly that it is not, on its own, proof that a read was performed. The behaviour is unchanged -- it is fixed by the frozen matrix. |
| `AgentSetupWriteResult` stale docs | The `Submitted` factory documents that it can return `AlreadyApplied`, and the record summary no longer claims the factories verify anything; it names the gateway as the place the invariant lives. |
| Redundant usings | Removed from the converter and from `AgentSetupDomainResultTests`. |
| `UnableToVerify` message never asserted | `EveryDeclaredErrorCodeCarriesItsOwnMessageAndOnlyUnknownFallsBack` pins the exact `UnableToVerify` message and proves every non-`Unknown` code has its own distinct message rather than the generic fallback. Verified it fails when the switch arm is deleted. |
| Contract tests pinned a non-transport shape | The acceptance compatibility properties are theories over both the default PascalCase options and `JsonSerializerDefaults.Web`, which is what the minimal-API routes actually use. |
| Result-payload round trip still open | `TheRealDomainResultPayloadIsUnderstoodAtTheOperationsBoundary` drives the genuine `AgentSetupDomainResult.ResultPayload` string, produced by a real `AgentAggregate.Handle` call, through the real receipt path for both the applied and the no-op case. The server fixture builder now keys off `AgentSetupResultPayload`. Verified the test fails on one-sided key drift and passes when both sides move together. |

Two changes live in the `references/Hexalith.EventStore` submodule (the no-op `resultPayload` forwarding in `AggregateActor` and `GetCommandStatusAsync` on `IEventStoreGatewayClient`). Both are now committed and pushed in that repository, and the submodule pointer here is bumped to `555c9047`.

**Round 3 fixes applied 2026-09-14**

| Finding | Resolution |
| --- | --- |
| `TryGetInt32` throws on a non-number element | `TryParseSetupResult` now checks `ValueKind` first, so a quoted, null, boolean or object version fails closed as `UnableToVerify` instead of escaping the dispatch `try/catch` as an unhandled exception. Four scenarios added to `An_unverifiable_gateway_receipt_fails_closed`; verified they fail without the guard. |
| Setup-payload enums still threw | `AgentSetupFreshness` (which declared no converter at all), `AgentLifecycleStatus`, `AgentResponseMode`, `ApproverPolicyBasisDisclosure`, `AgentActivationBlocker`, `AgentLaunchReadinessBlocker` and `OperationalStatusInspectionStatus` now declare `UnknownFallbackEnumConverter<T>`. The hand-maintained enum array in the contract tests is replaced by a walk of the graph reachable from `AgentSetupResult` plus the declared public operation status terms, filtered to `Unknown = 0`; `AgentInspectionStatus` is excluded by that filter (its zero is `Success`) and the AgentInteraction/ProviderCatalog enums by namespace, as deferred. A second, independent fact asserts the walk still covers the whole setup payload. |
| A non-matching `-class` selector passed as a no-op | `Invoke-TestClasses` reads the executed count back from the runner summary and fails on zero or on a missing summary. Probed both directions: a non-existent class now throws, a real suite still passes. |
| A transient read failure discarded the acceptance | Both `catch (Exception)` arms keep the acceptance when one exists, so an accepted write stays `AwaitingProjection` with its Refresh/Retry/Abandon actions instead of being relabelled a service outage and losing the version-gated Refresh. A never-accepted write still reports `Unavailable`. Two UI tests cover it; verified both fail without the fix. |
| Conformance guard missed `JsonStringEnumConverter<TEnum>` | The generic converter is a sibling type, not a subclass, so the guard now matches both shapes. |
| Readiness gate used three-dot diff semantics | `{baseline}..HEAD` instead of `{baseline}...HEAD`, so a diverged HEAD cannot silently shrink the change set; the tooling test pins the two-dot form. |
| Sprint note contradicted the commit | Corrected to record both EventStore changes as committed with the pointer bumped, keeping the still-open domain-rejection binding and provisioning/link-retirement facts. |
| Lost identity guarantee doc | `AgentCommandIdentityFactory` restates it accurately for ULIDs: canonical form is required for receipt identity comparison, the embedded timestamp is the caller's own submission time, and no tenant or user information is derivable. |

## Spec Change Log

- 2026-09-14: Added the mandatory Dev Agent Record and File List, and taught the readiness gate to include
  changes committed since `baseline_commit`. Intent and acceptance criteria are unchanged.
- 2026-09-14: Corrected the verification lanes to use project references only for Debug development builds and package references for Release builds, as required by the loaded repository instructions. Intent and acceptance criteria are unchanged.
- 2026-09-14: Applied the ten round-2 review patches (enum-compatibility wiring, converter tolerance, documentation, and the missing round-trip/transport-shape/error-message coverage) and added the new files to the File List. Intent and acceptance criteria are unchanged.
- 2026-09-14: Applied the round-3 review fixes (result-payload ValueKind guard, setup-payload enum compatibility with a derived rather than hand-listed guard, a verifier that cannot pass on zero executed tests, retained acceptance on a transient read failure, two-dot readiness diff, and the corrected sprint/identity notes). Intent and acceptance criteria are unchanged.

## Review Triage Log

| Reviewer | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| edge-case | A previous terminal/no-op write remains visible while a new attempt is awaiting its receipt. | medium | patch | `SubmitAsync` creates `_activeAttempt` without clearing `_pendingWrite`; after a terminal/no-op result clears only the attempt, a later slow submission can display the earlier outcome as current. |
| edge-case | `AgentSetupWriteResult.Failed` accepts `AlreadyApplied` without acceptance evidence. | low | reject | No production caller passes a success-like status to `Failed`; guarding this hypothetical public-factory misuse would add an invariant branch for a path not exercised by the implementation. |
| edge-case | `ConfigurationVersion + 1` can overflow at `int.MaxValue`. | low | reject | The theoretical overflow predates this story across the aggregate's versioned events and requires more than two billion setup mutations; a new exhaustion policy is disproportionate to this slice. |
| verification-gap | Live setup handlers are not individually verified to emit matching effect/version result payloads. | medium | patch | Only `UpdateAgentConfiguration` currently asserts `ResultPayload`; another handler could retain the correct event version while returning a stale command target and falsely confirm an older projection. |
| verification-gap | The invalid-idempotency test exits at invalid correlation validation first. | medium | patch | No authorized test reaches the message-id branch with a valid correlation ULID and malformed idempotency key, so that guard could regress unnoticed. |
| verification-gap | Manual awaiting-projection refresh is rendered but never exercised. | medium | patch | Tests assert only that the refresh button exists; they do not prove it reads the retained target, avoids resubmission, and clears recovery after confirmation. |
| verification-gap | Response-mode retry does not verify retention of the original mode. | medium | patch | Exact payload/ULID retry is tested only for configuration updates; closing over mutable `_responseMode` would reuse one key for two intents without failing current tests. |
| verification-gap | Real localization tests omit the new configuration recovery keys. | low | patch | Component tests use a permissive stub, while real-resource enumeration does not resolve `Agents.Config.Write.*` or the three recovery actions in both languages. |
| verification-gap-other | Manual refresh overwrites unsaved drafts after polling exhaustion. | medium | patch | `RefreshPendingAsync` reaches `RefreshAfterWriteAsync`, whose first read applies authoritative drafts; editable draft fields can therefore be silently reset by the new recovery action. |
| blind-hunter | An `AlreadyApplied` response can leave an older projection displayed because the UI performs no target-version read. | medium | reject | Carried no-op behavior is fixed by the frozen matrix and Design Notes: no-op completes without polling for a version that may never be newly emitted; changing that contract requires editing the approved intent. |
| blind-hunter | The immediate catch-up read can apply stale projected values to form drafts and later confirmed reads deliberately preserve those stale drafts. | medium | patch | `RefreshAfterWriteAsync` applies authoritative drafts on its first expected-version read even when that read is `AuthoritativePending`; preserving drafts for the entire catch-up avoids reverting a successful write. |
| blind-hunter | A value-shaped setup-read failure stops polling with the attempt labelled `Submitted`. | false | reject | The attempt remains retained and exposes exact Retry and Abandon actions; the page makes no success claim, so a failed read terminates safely rather than stranding the user without recovery. |
| blind-hunter | Navigating away discards the in-memory retry attempt and permits a new key on return. | maybe-false | defer | The component-lifetime retention boundary is not explicit in the approved intent; confirming whether retry identity must survive route navigation or browser reload requires a product/UX state-lifetime decision. |
| blind-hunter | `AgentSetupWriteResult.Submitted` accepts malformed acceptance evidence. | low | reject | The production server validates receipt identities, effect, and target before the client gateway constructs this result, and a missing target is converted to `UnableToVerify`; only a custom gateway bypassing the trusted boundary can reach the hypothetical misuse. |
| blind-hunter | Extending positional `AgentCommandAcceptance` removes the prior four-argument CLR constructor and deconstructor. | medium | patch | The record is in the packable Contracts assembly; restoring the prior constructor and deconstructor overloads preserves already-compiled V1 consumers while retaining additive wire fields. |
| blind-hunter | Changing `IAgentCommandDispatcher.DispatchAsync` from `Task` to `Task<SubmitCommandResponse>` breaks external implementations. | low | reject | `Hexalith.Agents.Server` is explicitly non-packable and deployed/recompiled as one server unit, so this internal hosting port is not a shipped extension contract. |
| blind-hunter | New mandatory `IAgentSetupGateway` overloads break existing out-of-tree implementations. | medium | patch | The UI assembly is packable and the gateway is its host binding seam; default fail-closed implementations preserve existing implementers without allowing them to claim exact retry support. |
| blind-hunter | ULID validation accepts non-canonical representations. | medium | patch | `NUlid.Ulid.TryParse` accepts representations that may normalize differently; require the parsed canonical string to match before dispatch so receipt identity comparison is deterministic. |
| blind-hunter | `AgentOperationOptions` documentation does not state the new canonical-ULID requirement. | low | patch | The server now rejects non-empty non-ULID values, so the public options documentation must describe that validation and exact-retry contract. |
| blind-hunter | Raw `HttpContext` header reads omit correlation/idempotency headers from generated OpenAPI metadata. | medium | patch | All five public write routes consume these headers but do not declare them as endpoint parameters, preventing generated clients from discovering retry metadata. |
| blind-hunter | The Story 5.2 verifier uses project-level `dotnet test --filter` for xUnit v3. | medium | patch | Repository instructions require invoking the built xUnit v3 assemblies directly with single-dash filters so Microsoft.Testing.Platform cannot ignore or reinterpret the focused selection. |
| blind-hunter | No test proves the result payload through a real EventStore persistence/replay/projection topology. | maybe-false | defer | The platform contract is unit-covered here but the production-like host/live integration fixture is assigned to Story 5.6; that fixture is needed to establish or refute a transport/persistence gap. |
| blind-hunter | The verifier omits the Release package-reference build. | false | reject | The spec lists the Story verifier and the Release build as separate verification commands; both were run and passed, so the script does not claim to subsume the second command. |
| blind-hunter | Three long recovery buttons use a non-wrapping horizontal layout at restrictive widths. | low | patch | The fixed horizontal FluentStack can make English/French actions unusable at 320 CSS pixels; a vertical FluentStack is a direct responsive correction. |
| blind-hunter | Sprint tracking still says exact correlation remains open. | low | patch | The story is now in review for the completed exact-correlation slice; remove only that stale phrase while retaining the still-open provisioning/link-retirement note. |
| edge-case-hunter | `AgentSetupWriteResult.Submitted` accepts unknown effect or a missing/non-positive target. | low | reject | This is the same trusted-boundary factory concern as the blind-hunter row: production validates before construction and missing target falls closed in the page; an extra public guard targets hypothetical custom-gateway misuse. |
| edge-case-hunter | `AgentSetupWriteResult.Failed` accepts `AlreadyApplied` without acceptance evidence. | low | reject | carried: no production caller passes a success-like status to `Failed`; guarding this hypothetical public-factory misuse would add an invariant branch for an unexercised path. |
| edge-case-hunter | Configuration-version increment can overflow at `int.MaxValue`. | low | reject | carried: the theoretical overflow predates this story and requires more than two billion setup mutations; defining a new exhaustion policy is disproportionate to this slice. |
| edge-case-hunter | `AgentSetupDomainResult.Applied` accepts an empty event list. | low | reject | Every internal caller supplies a non-empty event list and the full handler matrix proves it; adding a guard only protects hypothetical future misuse of an internal factory. |
| verification-gap | Four setup-write HTTP routes lack executable header-forwarding coverage. | medium | patch | Only configuration PUT currently proves `X-Correlation-ID` and `Idempotency-Key`; create, response-mode, activate, and disable could silently mint replacement identities. |
| verification-gap | The receipt-effect whitelist lacks invalid-effect coverage. | medium | patch | Missing/malformed tests fail on other fields and do not prove that `Unknown`, undefined, or wrong-case effects with a positive version return `UnableToVerify`. |
| verification-gap | `UnableToVerify` recovery is covered only at the gateway boundary. | medium | patch | No component test proves this distinct status retains Retry/Abandon, blocks competing writes, and reuses the exact attempt. |

| blind-hunter | `TryParseSetupResult` calls `JsonElement.TryGetInt32` with no `ValueKind` guard. | medium | patch | Verified: `TryGetInt32` throws `InvalidOperationException` when the element is not a Number, and the call sits after the dispatch `try/catch`, so a receipt carrying `"configurationVersion":"4"` escapes as an unhandled exception instead of the `UnableToVerify` the frozen matrix requires. The `malformed-payload` case only uses the number `0`. |
| edge-case-hunter | Receipt payload writes `configurationVersion` as a string, null, bool or object. | medium | patch | Same defect and location as the blind-hunter row above; grouped. |
| edge-case-hunter | Every `Unknown = 0` enum inside this story's own setup payload still throws. | medium | patch | Verified: `AgentSetupView`/`AgentStatusView` carry `AgentSetupFreshness`, `AgentLifecycleStatus`, `AgentResponseMode`, `ApproverPolicyBasisDisclosure`, `AgentActivationBlocker` and `AgentLaunchReadinessBlocker`; none declares `UnknownFallbackEnumConverter<T>` (`AgentSetupFreshness` declares no converter at all), so an additive member still fails the whole setup response for an older client — the exact guarantee the spec names under **Always**. |
| blind-hunter | The Contracts enum list is hand-maintained, which is the mechanism that produced the round-2 defect. | medium | patch | Grouped with the row above: the fix is a reflection guard over the setup payload's enum graph rather than another hand-listed array. Extending the converter to the ~45 remaining `Unknown = 0` contract enums on Epic 2/3/4 payloads is out of this story's reach and is deferred. |
| blind-hunter | The focused verifier can pass while running zero tests. | medium | patch | Verified empirically: `Hexalith.Agents.Tests -class Hexalith.Agents.Tests.NoSuchClassAtAll` prints `Total: 0` and exits `0`. A renamed or deleted suite silently turns a Story 5.2 gate into a no-op. The runner has no `-minimumExpectedTests`, so the gate must assert the executed count itself. |
| blind-hunter | A transient read failure permanently removes the read-only recovery path. | medium | patch | Verified: `RefreshPendingAsync`'s catch sets `Failed(Unavailable)`, discarding `Acceptance`; the Refresh button is gated on `TargetConfigurationVersion is not null`, so it disappears for a write that is still retained and whose target is still known, and an accepted write is relabelled "the configuration service is unavailable". The same discard exists in `ExecuteAttemptAsync`'s catch when the write succeeded and the poll threw. |
| edge-case-hunter | Setup read throws while polling or manually refreshing an already-accepted write. | medium | patch | Same defect and location as the blind-hunter row above; grouped. |
| verification-gap | The manual `Refresh projected setup` action has no test for a failing read. | medium | patch | Filed pre-verified: the only test that clicks refresh sets the read to succeed, so adding `_activeAttempt = null;` to the catch arm — silently dropping an unresolved attempt and re-enabling competing writes — passes the whole suite. Grouped with the two rows above: one root cause, one fix plus its test. |
| blind-hunter | `ServerSerializationConformanceTests` guards less than its own remark claims. | low | patch | Verified: the predicate is `converter is JsonStringEnumConverter`, which does not match `JsonStringEnumConverter<TEnum>` (a sibling factory type, not a subclass), so such a bag is skipped by `continue` rather than flagged, while the remark promises cover for "options bags added after this story". The instance-field and `ConfigureHttpJsonOptions` halves are not defects today — the verification-gap layer confirmed all five bags are static fields and the host configures no HTTP JSON options. |
| blind-hunter | The readiness gate uses three-dot diff semantics for a two-dot claim. | low | patch | Verified: `git diff --name-status -z --find-renames {baseline}...HEAD` diffs from `merge-base(baseline, HEAD)` while the gate's message says "the diff since baseline_commit". Identical on linear history, but when HEAD and the declared baseline diverge the change set silently shrinks and a File-List omission passes a fail-closed gate. |
| blind-hunter | `sprint-status.yaml` states something the same commit contradicts. | low | patch | Verified: the note says the EventStore fix is "uncommitted, pointer bump pending" while this diff bumps `references/Hexalith.EventStore` to `555c904` and the Implementation Notes record both changes as committed and pushed. |
| edge-case-hunter | The ledger note understates what this commit ships. | low | patch | Same stale note as the blind-hunter row above; grouped. |
| blind-hunter | The identity factory silently dropped its no-disclosure guarantee. | low | patch | Verified: the removed XML doc promised ids carrying "no tenant, user, or timing information", and a ULID embeds a 48-bit millisecond timestamp. Canonical ULIDs are mandated by the frozen Boundaries, so the identifier does not change; only the unstated trade is the defect, and the fix is the one doc sentence that records it. The timestamp is the caller's own submission time, and no tenant or user information is derivable. |
| edge-case-hunter | Returned correlation and message ids now leak millisecond submission timestamps. | low | patch | Same trade as the blind-hunter row above; grouped, and limited to restating the guarantee. |
| verification-gap | `Rejected`, the new terminal write outcome, is not adopted by `AgentsClientSetupGateway.ToWriteStatus`. | medium | defer | Filed pre-verified: `Rejected` falls through to `_ => Unavailable`, which `IsTerminalWriteFailure` excludes, so a rejected activation would render as a transient outage with a Retry that replays the same key against the same rejection. Not reachable in any composition that exists — only `DeferredAgentCommandStatusReader` is registered and it always answers `null` — so it is deferred onto the existing ledger entry for the live status-reader binding it depends on, which must not land without this mapping. |
| edge-case-hunter | The `Rejected` branch is dead in every composition. | false | reject | Accurate, and already disclosed: the spec's Implementation Notes and the deferred-work ledger both record that the live reader is absent pending an EventStore package release. A deliberately deferred binding that the change's own account states plainly is not an overstated claim. Its consequence is carried by the deferred row above. |
| edge-case-hunter | A live `IAgentCommandStatusReader` that throws escapes as an unhandled exception. | low | reject | The seam's own contract already requires `null` "when no status is recorded yet or the status could not be read", so translating read failures is the implementation's documented job; the only registered reader returns `null` unconditionally. Guarding the caller would add a catch-all that hides genuine faults in a reader that does not exist yet. |
| blind-hunter | The two readers of `AgentSetupWriteEffect` disagree on casing and the comment claims they do not. | false | reject | They read different sources on purpose: `TryParseSetupResult` reads the payload our own aggregate writes, where strict casing is the fail-closed choice, and the converter reads peer wire JSON, where round 2 explicitly restored case-insensitivity as a patch. The cited comment is about the comma-list name round-trip guard, which both readers do perform. |
| blind-hunter | `UnknownFallbackEnumConverterFactory` never checks the declared generic argument. | low | reject | A mis-declared `[JsonConverter(typeof(UnknownFallbackEnumConverter<WrongEnum>))]` fails loudly at first use with or without the factory — System.Text.Json rejects a converter whose type does not match either way — so the factory does not make the copy-paste worse, and the fix adds a guard for a state never demonstrated. |
| blind-hunter | An unbound gateway locks the whole page until Abandon is pressed. | low | reject | carried: round 2 rejected the same root cause — `Unavailable` conflating "not bound" with "unreachable mid-flight" — on the grounds that a distinct `NotBound` status adds public surface for no user-visible gain, and the deferred gateway resolves only when no Agent target is named. |
| blind-hunter | The recovery live region wraps its own action buttons. | low | reject | The region deliberately announces the write state together with the recovery actions, which is what AC3's accessible recovery asks for; splitting them would announce the problem without announcing the way out. No demonstrated user harm, and the alternative trades one accessibility judgment for another. |
| blind-hunter | `ProjectionPollingTimeout` went 5 s to 8 s with no stated reason. | false | reject | The frozen Boundaries require preserving "the 250 ms / 8 s catch-up contract" and the frozen matrix's exhaustion row says "Target absent after 8 seconds". The value is mandated by the approved intent, not an unexplained change. |
| blind-hunter | `-p:NuGetAudit=false` and the bunit bump ride along undocumented. | low | reject | Both flags live in this build's spec `## Verification` section, so the fix is a spec edit; the bunit bump is the dependency alignment the file's own comment requires for the Agents.UI tests. Surfaced to the human instead of patched. |
| blind-hunter | Nothing enforces the `AlreadyApplied` / `ProjectionConfirmed` pairing. | false | reject | The stated consequence does not hold: `A_noop_acceptance_is_projection_confirmed_because_it_appended_nothing_to_reach` and `An_applied_acceptance_is_authoritative_but_not_yet_projected` pin both directions at the producer, so a server regression that decoupled them fails those tests. The impossible fixtures in the UI tests are test hygiene with no reachable consequence. |
| edge-case-hunter | The catch-up read returning `Unknown` or `Submitted` freezes the attempt on `Submitted`. | false | reject | carried: round 2 rejected the identical claim — the attempt stays retained and exposes exact Retry and Abandon, and the page makes no success claim, so a value-shaped read failure terminates safely rather than stranding the administrator. |
| edge-case-hunter | `AgentSetupDomainResult.Applied` accepts an empty event list or a non-positive version. | low | reject | carried: rejected on the same grounds in rounds 1 and 2 — all internal callers supply a non-empty list and a positive version, and `EveryEventfulSetupHandlerCarriesAppliedEffectAndItsEmittedVersion` pins both for every handler. |

## Design Notes

`DomainResult.ResultPayload` already crosses EventStore only for completed success/no-op results and participates in idempotency replay. Carry only effect and resulting version; the gateway response owns message/correlation identity.

A later projected version confirms a command-derived target because ordered projection cannot skip that earlier event. No-op completes without polling; missing payload remains `UnableToVerify`.

## Verification

**Commands:**
- `pwsh ./eng/verify-story-5.2.ps1` -- expected: all named Story 5.2 domain, contract, server, UI, replay, and persisted read-model gates pass.
- `dotnet build Hexalith.Agents.slnx --configuration Debug -warnaserror -p:UseHexalithProjectReferences=true -p:NuGetAudit=false` -- expected: warning-free source-mode development build with additive contracts.
- `dotnet build Hexalith.Agents.slnx --configuration Release -warnaserror -p:UseHexalithProjectReferences=false -p:NuGetAudit=false` -- expected: warning-free package-mode release build.
- `git diff --check` in each owning repository -- expected: no whitespace errors.

## Dev Agent Record

<!-- dev-agent-test-evidence:start -->
### Latest Release Test Evidence

Run (UTC): 2026-09-14T20:02:24Z

| Test project | Total | Passed | Failed | Skipped | Pending | Other |
|---|---:|---:|---:|---:|---:|---:|
| Hexalith.Agents.Client.Tests | 6 | 6 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.Contracts.Tests | 515 | 515 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.Server.Tests | 489 | 489 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.Tests | 777 | 777 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.UI.Tests | 1067 | 1067 | 0 | 0 | 0 | 0 |
| **Total** | 2854 | 2854 | 0 | 0 | 0 | 0 |

Result: PASS
<!-- dev-agent-test-evidence:end -->
### File List

- `Directory.Packages.props`
- `_bmad-output/implementation-artifacts/deferred-work.md`
- `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `eng/verify-story-5.2.ps1`
- `references/Hexalith.Conversations`
- `references/Hexalith.EventStore`
- `references/Hexalith.FrontComposer`
- `src/Hexalith.Agents.Contracts/Agent/AgentActivationBlocker.cs`
- `src/Hexalith.Agents.Contracts/Agent/AgentLaunchReadinessBlocker.cs`
- `src/Hexalith.Agents.Contracts/Agent/AgentLifecycleStatus.cs`
- `src/Hexalith.Agents.Contracts/Agent/AgentResponseMode.cs`
- `src/Hexalith.Agents.Contracts/Agent/AgentSetupFreshness.cs`
- `src/Hexalith.Agents.Contracts/Agent/AgentSetupResultPayload.cs`
- `src/Hexalith.Agents.Contracts/Agent/AgentSetupTruthState.cs`
- `src/Hexalith.Agents.Contracts/Agent/AgentSetupWriteEffect.cs`
- `src/Hexalith.Agents.Contracts/Agent/AgentSetupWriteResult.cs`
- `src/Hexalith.Agents.Contracts/Agent/AgentSetupWriteStatus.cs`
- `src/Hexalith.Agents.Contracts/Agent/ApproverPolicyBasisDisclosure.cs`
- `src/Hexalith.Agents.Contracts/Operations/AgentCallOperationStatus.cs`
- `src/Hexalith.Agents.Contracts/Operations/AgentCommandAcceptance.cs`
- `src/Hexalith.Agents.Contracts/Operations/AgentOperationError.cs`
- `src/Hexalith.Agents.Contracts/Operations/AgentOperationErrorCode.cs`
- `src/Hexalith.Agents.Contracts/Operations/AgentOperationOptions.cs`
- `src/Hexalith.Agents.Contracts/Operations/AgentOperationResult.cs`
- `src/Hexalith.Agents.Contracts/Operations/AgentOperationStatus.cs`
- `src/Hexalith.Agents.Contracts/Operations/AgentReadinessStatus.cs`
- `src/Hexalith.Agents.Contracts/Operations/AuditAvailabilityStatus.cs`
- `src/Hexalith.Agents.Contracts/Operations/OperationalStatusInspectionStatus.cs`
- `src/Hexalith.Agents.Contracts/Operations/ProposalOperationStatus.cs`
- `src/Hexalith.Agents.Contracts/Operations/ProviderModelReadinessStatus.cs`
- `src/Hexalith.Agents.Contracts/Serialization/UnknownFallbackEnumConverter.cs`
- `src/Hexalith.Agents.Contracts/Serialization/UnknownFallbackEnumConverterFactory.cs`
- `src/Hexalith.Agents.Server/Api/AgentsOperationEndpoints.cs`
- `src/Hexalith.Agents.Server/Application/Agents/AgentActivationProviderRevalidation.cs`
- `src/Hexalith.Agents.Server/Application/Agents/AgentActivationRevalidationOutcome.cs`
- `src/Hexalith.Agents.Server/Application/Agents/AgentActivationRevalidationRequest.cs`
- `src/Hexalith.Agents.Server/Application/Agents/AgentAdministrationOrchestrator.cs`
- `src/Hexalith.Agents.Server/Application/Agents/AgentAdministrationOutcome.cs`
- `src/Hexalith.Agents.Server/Application/Agents/AgentAdministrationRequest.cs`
- `src/Hexalith.Agents.Server/Application/Agents/AgentResponseModeOrchestrator.cs`
- `src/Hexalith.Agents.Server/Application/Agents/AgentResponseModeOutcome.cs`
- `src/Hexalith.Agents.Server/Application/Agents/AgentResponseModeRequest.cs`
- `src/Hexalith.Agents.Server/Application/Agents/EventStoreAgentAdministrationOperations.cs`
- `src/Hexalith.Agents.Server/Application/Agents/ProviderCatalogAdministrationOrchestrator.cs`
- `src/Hexalith.Agents.Server/Application/Queries/AgentInteractionAuditQueryHandlerBase.cs`
- `src/Hexalith.Agents.Server/Application/Queries/AgentSetupQueryHandlerBase.cs`
- `src/Hexalith.Agents.Server/Application/Queries/ProviderCatalogQueryHandlerBase.cs`
- `src/Hexalith.Agents.Server/Composition/AgentSetupServiceCollectionExtensions.cs`
- `src/Hexalith.Agents.Server/Hexalith.Agents.Server.csproj`
- `src/Hexalith.Agents.Server/Ports/AgentCommandIdentityFactory.cs`
- `src/Hexalith.Agents.Server/Ports/DeferredAgentCommandDispatcher.cs`
- `src/Hexalith.Agents.Server/Ports/DeferredAgentCommandStatusReader.cs`
- `src/Hexalith.Agents.Server/Ports/EventStoreAgentCommandDispatcher.cs`
- `src/Hexalith.Agents.Server/Ports/IAgentCommandDispatcher.cs`
- `src/Hexalith.Agents.Server/Ports/IAgentCommandIdentityFactory.cs`
- `src/Hexalith.Agents.Server/Ports/IAgentCommandStatusReader.cs`
- `src/Hexalith.Agents.Server/Program.cs`
- `src/Hexalith.Agents.Server/Projections/AgentSetupProjectionFold.cs`
- `src/Hexalith.Agents.Server/Projections/ProviderCatalogProjectionFold.cs`
- `src/Hexalith.Agents.UI/Components/Pages/AgentConfiguration.razor`
- `src/Hexalith.Agents.UI/Components/_Imports.razor`
- `src/Hexalith.Agents.UI/Resources/AgentsResources.fr.resx`
- `src/Hexalith.Agents.UI/Resources/AgentsResources.resx`
- `src/Hexalith.Agents.UI/Services/Gateways/AgentsClientSetupGateway.cs`
- `src/Hexalith.Agents.UI/Services/Gateways/DeferredAgentSetupGateway.cs`
- `src/Hexalith.Agents.UI/Services/Gateways/IAgentSetupGateway.cs`
- `src/Hexalith.Agents.UI/State/AgentSetupAttempt.cs`
- `src/Hexalith.Agents/Agent/AgentAggregate.cs`
- `src/Hexalith.Agents/Agent/AgentSetupDomainResult.cs`
- `test/Hexalith.Agents.Contracts.Tests/AgentOperationContractsTests.cs`
- `test/Hexalith.Agents.Server.Tests/AgentActivationApproverRevalidationTests.cs`
- `test/Hexalith.Agents.Server.Tests/AgentAdministrationOrchestratorTests.cs`
- `test/Hexalith.Agents.Server.Tests/AgentProviderSelectionOrchestratorTests.cs`
- `test/Hexalith.Agents.Server.Tests/AgentResponseModeOrchestratorTests.cs`
- `test/Hexalith.Agents.Server.Tests/AgentSetupQueryHandlerTests.cs`
- `test/Hexalith.Agents.Server.Tests/AgentsOperationEndpointsTests.cs`
- `test/Hexalith.Agents.Server.Tests/EventStoreAgentAdministrationOperationsTests.cs`
- `test/Hexalith.Agents.Server.Tests/EventStoreAgentCommandDispatcherTests.cs`
- `test/Hexalith.Agents.Server.Tests/EventStoreProviderCatalogOperationsTests.cs`
- `test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj`
- `test/Hexalith.Agents.Server.Tests/ServerSerializationConformanceTests.cs`
- `test/Hexalith.Agents.Tests/AgentSetupDomainResultTests.cs`
- `test/Hexalith.Agents.UI.Tests/AgentConfigurationTests.cs`
- `test/Hexalith.Agents.UI.Tests/AgentsClientSetupGatewayTests.cs`
- `test/Hexalith.Agents.UI.Tests/AgentsTestContext.cs`
- `test/Hexalith.Agents.UI.Tests/LegacyAgentSetupGateway.cs`
- `test/Hexalith.Agents.UI.Tests/LocalizationResourceTests.cs`
- `tests/tooling/story_review_readiness/story_review_readiness_test.py`
- `tools/check-story-review-readiness.py`
