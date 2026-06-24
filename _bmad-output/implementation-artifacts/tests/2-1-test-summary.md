# Story 2.1 — Test Summary: Request `hexa` From A Source Conversation

**Story:** 2-1-request-hexa-from-a-source-conversation
**Date:** 2026-06-24
**Build:** `dotnet build Hexalith.Agents.slnx --configuration Release` → **0 warnings / 0 errors** (`TreatWarningsAsErrors=true`)

## Result

| Test project | Tests | Passed | Failed | Skipped |
| --- | --- | --- | --- | --- |
| `Hexalith.Agents.Tests` (domain) | 293 | 293 | 0 | 0 |
| `Hexalith.Agents.Contracts.Tests` | 99 | 99 | 0 | 0 |
| `Hexalith.Agents.Server.Tests` | 108 | 108 | 0 | 0 |

> Counts reconciled by the senior review (2026-06-24): the QA-automation step added 23 tests
> (13 domain + 3 contracts + 7 server — cross-seam E2E + snapshot-scalar `[Theory]` cases) after this
> summary was first written. Build remained 0W/0E; `Hexalith.Agents.UI.Tests` regression: 156/156.

All pre-existing structural/guard tests stayed green (`StructuralSeedConformanceTests`, `ProjectReferenceDirectionTests`, `PublicContractPackageBoundaryTests`, `ContractsBoundaryTests`, `ContractsSecretNonDisclosureTests`, `ModuleLayout`, `PackageVersionCentralizationTests`, `BuildContractConformanceTests`). New tests are additive — no existing test was modified.

## New tests added

### Domain — `Hexalith.Agents.Tests`
- `AgentInteractionTestData.cs` — fixture: `Envelope<T>` (agent-interaction domain), `ValidRequest`, `RequestedEvent`, `StateWith`, `ApplyAll`, `ProcessAndApplyAsync`.
- `AgentInteractionAggregateTests.cs` — request → `InteractionRequested` with full AD-4 snapshot (AC1); prompt captured on durable state only (AD-14); missing caller/source/prompt/snapshot + empty-snapshot-scalar → `InvalidAgentInteractionRequestRejection` with the correct safe classification (AC1, AC4); validation precedence; rejection does not echo prompt/caller (AD-14); exact-duplicate → `NoOp` and conflicting prompt/snapshot on the same id → `AgentInteractionAlreadyRequestedRejection` (AC2/AD-13); full reflection-dispatch + JSON round-trip through `ProcessAsync`, including idempotent re-issue.
- `AgentInteractionStateReplayTests.cs` — `Apply(InteractionRequested)` records request + snapshot; replay determinism across independent rebuilds; persisted rejections are replay-safe no-ops; a rejection-only stream rehydrates to un-requested.

### Contracts — `Hexalith.Agents.Contracts.Tests`
- `AgentInteractionContractsTests.cs` — marker conformance (`IEventPayload`/`IRejectionEvent`); plain-record command (no domain attribute); System.Text.Json round-trips for command (with and without snapshot), event, both rejections, query, reference, and view; enums serialize by name + `Unknown` fallback; **`Prompt` absent** from the status view, the reference, and every rejection, and present only on the durable event/command (AD-14).

### Server — `Hexalith.Agents.Server.Tests`
- `AgentInteractionRequestOrchestratorTests.cs` — snapshot read from `IAgentConfigurationSnapshotReader` and written into the dispatched command with the V1 context-policy reference stamped (AC1); safe `AgentInteractionReference` returned (id + status only, AC2); deterministic id is stable, regex-valid, colon-free, and varies per identity component (AD-13); reserved client-forged extensions stripped while benign ones pass through (AC4); not-available snapshot still dispatches a null-snapshot request with an `Unknown` reference (auditable `MissingAgentSnapshot`); only the reader + dispatcher are touched — no provider/Conversation client (AC3); deferred reader fails closed to not-available.

## Commands run

```
dotnet restore Hexalith.Agents.slnx
dotnet build  Hexalith.Agents.slnx --configuration Release        # 0W / 0E
dotnet test tests/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release
dotnet test tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj --configuration Release
```

## Acceptance criteria coverage

- **AC1** (create request record with config snapshot) — aggregate creates `InteractionRequested` with the full AD-4 snapshot frozen; orchestrator assembles the snapshot from the trusted Agent read.
- **AC2** (safe status reference + dedupe) — orchestrator returns `AgentInteractionReference` (no stream/provider detail); deterministic id makes re-issues a `NoOp`.
- **AC3** (explicit invocation only; no side effects) — only the explicit command path creates an interaction; the orchestrator touches only the snapshot reader + dispatcher, no provider/Conversation/Parties client.
- **AC4** (protect prompt/context + tenant boundaries) — prompt lives only on the durable event/state; rejections carry only id + safe classification; tenant scope flows from the envelope.
