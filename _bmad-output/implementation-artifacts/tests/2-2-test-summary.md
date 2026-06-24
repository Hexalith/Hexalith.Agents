# Story 2.2 — Test Summary: Enforce Invocation Authorization And Dependency Readiness

**Story:** 2-2-enforce-invocation-authorization-and-dependency-readiness
**Date:** 2026-06-24
**Build:** `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` → **0 warnings / 0 errors** (`TreatWarningsAsErrors=true`)

## Result

| Test project | Tests | Passed | Failed | Skipped |
| --- | --- | --- | --- | --- |
| `Hexalith.Agents.Tests` (domain) | 365 | 365 | 0 | 0 |
| `Hexalith.Agents.Contracts.Tests` | 118 | 118 | 0 | 0 |
| `Hexalith.Agents.Server.Tests` | 131 | 131 | 0 | 0 |

Regression: `Hexalith.Agents.UI.Tests` (consumes Contracts after the additive `AgentInteractionStatus` extension) → **156/156**. All pre-existing structural/guard tests stayed green (`StructuralSeedConformanceTests`, `ProjectReferenceDirectionTests`, `PublicContractPackageBoundaryTests`, `ContractsBoundaryTests`, `ContractsSecretNonDisclosureTests`, `ModuleLayout`, `PackageVersionCentralizationTests`, `BuildContractConformanceTests`). New tests are additive — no existing test was modified (only `AgentInteractionTestData.cs` was extended and `AgentInteractionStateReplayTests.cs` appended to).

## New tests added

### Domain — `Hexalith.Agents.Tests`
- `AgentInteractionTestData.cs` — extended with gate fixtures: `StateRequested()`, `Verdict(check, outcome)`, `AllSatisfied()` (nine satisfied verdicts in evaluation order), `SatisfiedExcept(check, outcome)`, `GateCommand(...)`; `ApplyAll` now dispatches `AgentInteractionAuthorized`/`AgentInteractionGateFailed`/`AgentInteractionGateNotEvaluableRejection`.
- `AgentInteractionGateAggregateTests.cs` — all-satisfied → `AgentInteractionAuthorized` + state `Authorized`; each **authorization-class** blocker (`TenantAccess`/`CallerPartyState`/`SourceConversationAccess`) × each AD-12 outcome (`Missing`/`Stale`/`Ambiguous`/`Disabled`/`Unavailable`/`Unauthorized`) → `AgentInteractionGateFailed(Denied, blockers)`; each **readiness-class** blocker × each outcome → `AgentInteractionGateFailed(Blocked, blockers)`; mixed authorization+readiness → `Denied` precedence; all-blocking-readiness records every blocker as evidence; not-requested (null state and rejection-only stream) → `AgentInteractionGateNotEvaluableRejection(InteractionNotRequested)`; empty and null verdicts → `…(NoVerdictsProvided)`; not-requested precedes empty-verdicts; idempotent re-evaluation on already-Authorized/Denied/Blocked → `NoOp` (decision never flips); full reflection-dispatch + JSON round-trip through `ProcessAsync` (authorized + blocking, outcome enum survives by name).
- `AgentInteractionStateReplayTests.cs` — `Apply(AgentInteractionAuthorized)` → status `Authorized`; `Apply(AgentInteractionGateFailed)` → records decision + safe blocker evidence; `Apply(AgentInteractionGateNotEvaluableRejection)` is a replay-safe no-op; gate-outcome `Apply` only over a requested stream (the `IsRequested` guard); replay determinism over request+gate streams.

### Contracts — `Hexalith.Agents.Contracts.Tests`
- `AgentInteractionGateContractsTests.cs` — marker conformance (`AgentInteractionAuthorized`/`AgentInteractionGateFailed` are `IEventPayload` and **not** `IRejectionEvent`; `AgentInteractionGateNotEvaluableRejection` is `IRejectionEvent`); plain-record command (no domain attribute); System.Text.Json round-trips for command, both outcome events, the rejection, the verdict, the evidence query/view/result, and the success result; the new enums serialize by name + `Unknown` fallback; the additive `AgentInteractionStatus` extension serializes `Authorized`/`Denied`/`Blocked` by name **and preserves the Story-2.1 `Unknown=0`/`Requested=1` ordinals**; the verdict exposes **only** the two safe enums; gate surfaces carry no sensitive members (prompt/claims/tokens/PartyId/payload/message — AD-14); `AgentInteractionGateEvidenceResult.NotAuthorized()/NotFound()` carry a `null` evidence view (AC3).

### Server — `Hexalith.Agents.Server.Tests`
- `AgentInteractionGateOrchestratorTests.cs` — all-ready reads → nine `Satisfied` verdicts (in evaluation order) → dispatched `EvaluateAgentInteractionGate` returning `Authorized`; Confirmation-mode with a resolvable approver policy → `Satisfied`; each reader's denial/disabled/missing/stale/unavailable maps to the correct `(Check, Outcome)` verdict and the correct `Denied`/`Blocked` decision (tenant unauthorized, caller-party missing, conversation stale, agent lifecycle disabled/draft, agent-party missing/invalid, provider non-selectable/not-found, response-mode unknown, approver-unresolvable, content-safety missing, dependency-freshness stale); a reader that **throws** → fail-closed `Unavailable` with **no raw error text** in the dispatched command or outcome (AD-14); not-available readiness fails closed every readiness check; reserved client extensions stripped (benign preserved); only server-read verdicts are dispatched (the request carries none); only read ports + one gate dispatch — no provider invocation / Conversations post / proposal (AC2); trusted envelope scope from the request; **all-deferred defaults fail closed to `Denied`**; cross-seam end-to-end (the orchestrator's dispatched command drives the real pure aggregate to the **same** decision — shared `AgentInvocationGatePolicy`, no drift).

## Commands run

```
dotnet restore Hexalith.Agents.slnx                               # attempted; sandbox/parallel graph returned code 1 with no diagnostics
dotnet build  Hexalith.Agents.slnx --configuration Release -m:1   # 0W / 0E
tests/Hexalith.Agents.Tests/bin/Release/net10.0/Hexalith.Agents.Tests -noLogo -noColor
tests/Hexalith.Agents.Contracts.Tests/bin/Release/net10.0/Hexalith.Agents.Contracts.Tests -noLogo -noColor
tests/Hexalith.Agents.Server.Tests/bin/Release/net10.0/Hexalith.Agents.Server.Tests -noLogo -noColor
tests/Hexalith.Agents.UI.Tests/bin/Release/net10.0/Hexalith.Agents.UI.Tests -noLogo -noColor
```

Note: in the current sandbox, default parallel `dotnet restore`/`dotnet build` returned exit code 1 without MSBuild diagnostics while traversing project references. Serialized build with `-m:1` succeeded. `dotnet test` was blocked by VSTest local TCP listener creation (`SocketException (13): Permission denied`), so the generated xUnit v3 executables were used for the actual test run.

## Acceptance criteria coverage

- **AC1** (gate every dependency before invocation; fail closed on uncertainty) — the aggregate evaluates all nine `AgentInteractionGateCheck` verdicts and blocks on any non-`Satisfied`; the orchestrator reads every dependency in evaluation order, each fail-closed to `Unavailable` on throw/not-available; with all live bindings deferred the gate decides `Denied`.
- **AC2** (missing/stale caller access → denied/blocked, no side effects) — authorization-class blockers record `AgentInteractionGateFailed(Denied)`; the orchestration only reads dependency state and dispatches the gate command — no provider adapter, no `AppendMessageAsync`, no proposal.
- **AC3** (unprovable tenant/Party authorization fails closed without revealing cross-tenant existence) — fail-closed `Unavailable`/`Unauthorized` verdicts; coarse safe enums; the evidence inspection result returns a `null` view on `NotAuthorized`/`NotFound`; tenant scope flows from the envelope.
- **AC4** (safe audit evidence distinguishes failure classes) — `AgentInteractionGateFailed` durably records the decision + the non-satisfied verdicts; the verdict/view/events carry only the two safe gate enums (no claims, tokens, Party PII, provider payloads, or stack traces).
