# Story 2.3 — Test Summary: Build Conversation Context With Safe Bounds

**Story:** 2-3-build-conversation-context-with-safe-bounds
**Date:** 2026-06-24
**Build:** `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` → **0 warnings / 0 errors** (`TreatWarningsAsErrors=true`)

## Result

| Test project | Tests | Passed | Failed | Skipped |
| --- | --- | --- | --- | --- |
| `Hexalith.Agents.Tests` (domain) | 456 | 456 | 0 | 0 |
| `Hexalith.Agents.Contracts.Tests` | 140 | 140 | 0 | 0 |
| `Hexalith.Agents.Server.Tests` | 178 | 178 | 0 | 0 |

Regression: `Hexalith.Agents.UI.Tests` (consumes Contracts after the additive `AgentInteractionStatus` extension — ordinals 5/6 added) → **156/156**. All pre-existing structural/guard tests stayed green (`StructuralSeedConformanceTests`, `ProjectReferenceDirectionTests` — only inspects `Hexalith.Agents*` refs, so the new `Hexalith.Conversations.Client` Server reference does not trip it, `PublicContractPackageBoundaryTests`, `ContractsBoundaryTests`, `ContractsSecretNonDisclosureTests`, `ModuleLayout`, `PackageVersionCentralizationTests`, `BuildContractConformanceTests`). New tests are additive — no existing test was modified (only `AgentInteractionTestData.cs` was extended and `AgentInteractionStateReplayTests.cs` appended to).

## Acceptance-criteria coverage

- **AC1** (load only authorized Conversations detail + visible timeline; V1 excludes non-conversation sources) — `ConversationClientContextReaderTests` (visible+fresh → `Loaded` with the ordered visible timeline only); the context reader/measurement/evidence carry no project/folder/file/tool/memory/external-channel source; coarse `Unauthorized`/`Unavailable` load outcomes never disclose cross-tenant existence.
- **AC2** (full-context fit records full usage + policy version + budget metadata + safe evidence, raw context redacted) — aggregate full-fits → `AgentInteractionContextReady(Mode=Full, UsedContextTokenCount==FullContextTokenCount)` with budget + policy reference + capability version in evidence; orchestrator `The_dispatched_command_and_returned_result_carry_no_raw_message_text` proves the raw conversation text never reaches the command/envelope/outcome (AD-14).
- **AC3** (oversized / not-fresh with no approved bounded behavior fails closed, no side effects) — `Oversized_with_no_approved_bounded_behavior_blocks_and_never_silently_truncates` (asserts **no** `ContextReady` is emitted), stale/unauthorized/unavailable/invalid-budget blocks, and `Only_reads_and_one_context_dispatch_no_provider_invocation_or_conversation_post` (no provider adapter, no Conversations post, no proposal).
- **AC4** (approved bounded behavior records behavior + policy version + bounds + audit-safe metadata; never silent truncation) — `Oversized_with_an_approved_bounded_behavior_records_context_ready_bounded` (`Mode=Bounded`, `BoundedBehaviorReference` recorded, `UsedContextTokenCount == Min(full, bounded limit)`); a bounded limit that itself exceeds the budget still blocks (never silent).

## New tests added

### Domain — `Hexalith.Agents.Tests`
- `AgentInteractionTestData.cs` — extended with Story-2.3 context fixtures: `StateAuthorized()` (request → authorize, the context precondition), `Measurement(...)` + `FullFitsMeasurement()`/`OversizedMeasurement()`/`BoundedApprovedMeasurement(limit)`, `ContextCommand(measurement)`; `ApplyAll` now also dispatches `AgentInteractionContextReady`/`AgentInteractionContextBlocked`/`AgentInteractionContextNotBuildableRejection`.
- `AgentInteractionContextAggregateTests.cs` — full-fits → `ContextReady(Full)` + state `ContextReady` (`UsedContextTokenCount == FullContextTokenCount`); approved-bounded + oversized → `ContextReady(Bounded)` with bounds recorded; **oversized + no approved bounded behavior → `ContextBlocked(ExceedsModelBudget)` asserting no `ContextReady` is emitted** (the AD-17 "context-too-large blocking" gate); a bounded limit that exceeds the budget still blocks; stale load → `ContextBlocked(ContextNotFresh)`; unauthorized/unavailable/unknown load → `ContextBlocked(ContextUnavailable)`; invalid/zero budget (non-positive window, negative reserved, reserved≥window, non-positive capability version) → `ContextBlocked(ModelBudgetUnavailable)`; not-requested (null + rejection-only stream) → `…NotBuildableRejection(InteractionNotRequested)`; requested-but-not-authorized / gate-denied / gate-blocked → `…(InteractionNotAuthorized)`; idempotent re-build on already `ContextReady`/`ContextBlocked` → `NoOp` (decision never flips); `Decide`/`Evaluate` no-drift across every outcome; full reflection-dispatch + JSON round-trip through `ProcessAsync` (ready + blocking, reason survives by name).
- `AgentInteractionStateReplayTests.cs` — `Apply(AgentInteractionContextReady)` → status `ContextReady` + safe evidence (prompt untouched, AD-14); `Apply(AgentInteractionContextBlocked)` → records decision + block reason + evidence; `Apply(AgentInteractionContextNotBuildableRejection)` is a replay-safe no-op; context-outcome `Apply` only over a requested stream (the `IsRequested` guard); replay determinism over request+authorize+context streams.

### Contracts — `Hexalith.Agents.Contracts.Tests`
- `AgentInteractionContextContractsTests.cs` — marker conformance (`AgentInteractionContextReady`/`AgentInteractionContextBlocked` are `IEventPayload` and **not** `IRejectionEvent`; `AgentInteractionContextNotBuildableRejection` is `IRejectionEvent`); plain-record command (no `Hexalith`-namespaced attribute — scoped to avoid the compiler's `Nullable*` attributes per the Story 2.1 learning); System.Text.Json round-trips for the command, measurement, bounded behavior, evidence (full + bounded), both outcome events, the rejection, the query/view/result; the new enums (`ContextMode`/`ContextLoadOutcome`/`ContextBlockReason`/`ContextNotBuildableReason`) serialize by name + `Unknown` fallback; the additive `AgentInteractionStatus` extension serializes `ContextReady`/`ContextBlocked` by name **and preserves ordinals 0–6** (Story 2.1/2.2 `Unknown`/`Requested`/`Authorized`/`Denied`/`Blocked` unchanged at 0–4); measurement/evidence/view/events/bounded-behavior carry **only** safe numerics/enums/references — exact-name guard avoids tripping the safe count members (`MessageCount`, `FullContextTokenCount`, `ContextWindowTokenLimit`) and asserts no Conversations/provider type leaks; `AgentInteractionContextEvidenceResult.NotAuthorized()/NotFound()` carry a `null` view (AC1).

### Server — `Hexalith.Agents.Server.Tests`
- `AgentInteractionContextOrchestratorTests.cs` — loaded+fits → dispatched `BuildAgentInteractionContext` with `Measurement.LoadOutcome == Loaded` returning `ContextReady`; oversized → dispatched measurement + `ContextBlocked`; stale read → `ContextBlocked`; `Hidden`/`Unavailable` read → `ContextBlocked`; missing/disabled/non-text-capable catalog entry and unavailable token measurement → zeroed budget → `ContextBlocked` (`ModelBudgetUnavailable`); a context-reader / token-measurer / catalog reader that **throws** → fail-closed `ContextBlocked` with **no raw error text** in the command/outcome (AD-14); a genuine `OperationCanceledException` propagates and dispatches nothing; reserved client extensions stripped (benign preserved); trusted envelope scope from the request; only read ports + one context dispatch — no provider invocation / Conversations post / proposal (AC3); **the dispatched command + returned result carry no raw message text** (the loaded `SecretConversationText` appears nowhere); all-deferred defaults fail closed to `ContextBlocked` (`ContextUnavailable`); cross-seam end-to-end (the dispatched command drives the real pure aggregate to the **same** decision — shared `AgentInteractionContextPolicy`, no drift).
- `ConversationClientContextReaderTests.cs` — the `IConversationClient.GetConversationAsync` mapping: visible + trust-bearing → `Loaded` with the messages **ordered by `CreatedAt`**; `Hidden` → `Unauthorized`; a no-details `Redacted` result → `Unauthorized`; `Unavailable` → `Unavailable`; visible-but-not-trust-bearing freshness → `Stale`; a `!IsSuccess` failure result → `Unavailable`; a thrown client exception → fail-closed `Unavailable` (no messages, no raw error); a genuine cancellation propagates.

## Commands run (from `Hexalith.Agents/`)

```
dotnet build Hexalith.Agents.slnx --configuration Release -m:1                                   # 0W / 0E
dotnet test  tests/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj --configuration Release    # 456/456
dotnet test  tests/Hexalith.Agents.Contracts.Tests/...csproj --configuration Release             # 140/140
dotnet test  tests/Hexalith.Agents.Server.Tests/...csproj --configuration Release                # 178/178
dotnet test  tests/Hexalith.Agents.UI.Tests/...csproj --configuration Release                    # 156/156 (regression)
```

## Notes / deferred (per story scope)

- **Live bindings deferred (fail closed):** token measurement (`DeferredConversationContextTokenMeasurer` — no tokenizer library bound; provider SDK stays "adapter-local when selected") and command dispatch (`DeferredAgentCommandDispatcher`). The **live** `ConversationClientContextReader` over `IConversationClient.GetConversationAsync` IS authored and unit-tested in this story but registered only behind a `Conversations` config section; the default DI graph uses `DeferredConversationContextReader` and fails closed to `ContextBlocked(ContextUnavailable)`.
- **OQ-10 (bounded-context behavior):** the bounded path's *shape* (`Mode=Bounded`, evidence, policy-gated branch) is implemented and tested, but V1 wires **no** approved bounded behavior — `ContextPolicyResolution.Resolve("full-conversation-v1")` (and any unknown reference) returns `null`, so production overflow always blocks. No truncation/summarization behavior was invented.
- **No `ContextLoading` status** added (per Dev Notes rationale) — only the two terminal outcomes `ContextReady`/`ContextBlocked` are recorded; "context loading" is the transient UI representation surfaced by Story 2.6.
- The VSTest local-TCP-listener `SocketException` did **not** occur in this run; `dotnet test` ran cleanly for all four projects.
