# Test Automation Summary — Story 3.1 (Create Proposed Agent Replies In Confirmation Mode)

**Workflow:** `bmad-qa-generate-e2e-tests`
**Date:** 2026-06-24
**Engineer role:** QA automation (Administrator)
**Story:** `_bmad-output/implementation-artifacts/3-1-create-proposed-agent-replies-in-confirmation-mode.md` (status: review)

## Test Framework Detected

xUnit v3 (`xunit.v3` 3.2.2) + Shouldly 4.3.0 + NSubstitute 5.3.0, .NET 10 (`net10.0`), warnings-as-errors,
`ConfigureAwait(false)` enforced (CA2007). Tests run **per project** (never solution-level), Release configuration.
This is a DDD / event-sourced domain library (no HTTP API surface and no UI in this story — proposal read UI is deferred to
Stories 3.2/3.7), so "E2E" here means the full command → event → replay-state chain through the real aggregate pipeline.

## Scope

Story 3.1 was already implemented (status `review`) with extensive per-layer tests. This QA pass mapped the existing
coverage against AC1–AC4 + the story's Testing Requirements, then **auto-applied the discovered coverage gaps**. No
production code was changed — tests only.

## Coverage Map (pre-existing, verified green)

| Layer | File | Covers |
|---|---|---|
| Domain (aggregate) | `AgentInteractionProposalAggregateTests.cs` | created path + every failure outcome → `ProposalCreationFailed`; not-creatable rejections (not-requested / not-confirmation / not-generated incl. `SafetyFailed`/`GenerationFailed` for AC3); terminal idempotent no-op on both terminal states (AC4); Evaluate/Decide no-drift; `ProcessAsync` reflection-dispatch + JSON round-trip |
| Domain (replay) | `AgentInteractionStateReplayTests.cs` | `Apply` for created / creation-failed / not-creatable no-op; `IsRequested` guard totality; deterministic rebuild for both terminal states (AC4) |
| Domain (fixtures) | `AgentInteractionTestData.cs` | proposal result/command builders + 3 new `ApplyAll` dispatch cases |
| Contracts | `AgentInteractionProposalContractsTests.cs` | marker interfaces; JSON round-trip (command, both events, evidence, result incl. null expiry); enums by-name + `Unknown` sentinel; ordinals 10–13 pinned; AD-14 content/secret no-leak + no content-bearing member |
| Server (orchestrator) | `AgentInteractionProposalOrchestratorTests.cs` | happy path → `ProposalCreated` w/ deterministic id; no content on envelope/result; version-unavailable / reader-throws → `ProposalCreationFailed`; all-deferred graph fails closed; deterministic id reuse; reserved-key stripping; non-Confirmation short-circuit; expiry flows; version-read cancellation propagates |
| Server (identity) | `AgentProposalIdentityTests.cs` | determinism, distinctness, distinct-from-posting-id, 64-char lowercase hex |
| Server (deferred port) | `DeferredProposalExpiryPolicyReaderTests.cs` | returns null expiry |

## Discovered Gaps → Auto-Applied

### Gap 1 — Full Confirmation-mode lifecycle E2E (request → gate → context → generate → propose)

Every existing domain test built the `Generated` precondition via direct `Apply(...)`. No test drove the **whole command
chain through the real reflection-dispatch pipeline** (`AgentInteractionAggregate.ProcessAsync` + JSON round-trip + the
production `Apply` handlers), so the actual precondition enforcement at each step was never exercised end-to-end for the
proposal path. This is the canonical E2E for an event-sourced aggregate (mirrors `ProviderCatalogLifecycleE2ETests`) and the
signature deliverable of this workflow.

**Added:** `tests/Hexalith.Agents.Tests/AgentInteractionProposalLifecycleE2ETests.cs` (2 tests)

- `A_confirmation_interaction_reaches_a_pending_proposal_through_the_full_command_chain` — drives all 5 commands through the
  real handlers, asserting each intermediate status (Requested → Authorized → ContextReady → Generated → ProposalCreated),
  `Pending` sub-state, safe evidence ids, and that the round-tripped outcome carries no generated content (AC1, AC2).
- `A_safety_blocked_confirmation_interaction_can_never_reach_a_proposal_end_to_end` — drives a Content-Safety-blocked
  generation (→ `SafetyFailed`, no version appended) then a create command, proving the create is structurally rejected
  (`OutputNotGenerated`) with no state change — the **end-to-end** enforcement of AC3.

### Gap 2 — Expiry-read cancellation propagation (orchestrator)

`AgentInteractionProposalOrchestrator.ReadExpiryAsync` re-throws `OperationCanceledException` (the fail-closed catch
deliberately excludes it). Only the **version**-read cancellation path was tested; the **expiry**-read cancellation branch
was uncovered.

**Added:** `tests/Hexalith.Agents.Server.Tests/AgentInteractionProposalOrchestratorTests.cs` (1 test)

- `A_cancellation_during_the_expiry_read_propagates_and_no_command_is_dispatched` — a genuine cancellation during the expiry
  read propagates (does not silently degrade to no-expiry) and dispatches nothing.

## Generated Tests

### E2E Tests
- [x] `tests/Hexalith.Agents.Tests/AgentInteractionProposalLifecycleE2ETests.cs` — full Confirmation-mode lifecycle (happy path + AC3 safety-blocked path)

### API / Service Tests
- [x] `tests/Hexalith.Agents.Server.Tests/AgentInteractionProposalOrchestratorTests.cs` — +1 expiry-read cancellation-propagation test

## Test Run (Release, run per project)

Build: `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` → **Build succeeded, 0 Warning(s) / 0 Error(s)**.

| Project | Before | After | Delta |
|---|---|---|---|
| `Hexalith.Agents.Tests` (domain) | 555 | **557** | +2 (lifecycle E2E) |
| `Hexalith.Agents.Server.Tests` | 252 | **253** | +1 (expiry cancellation) |
| `Hexalith.Agents.Contracts.Tests` | 204 | **204** | 0 (regression check) |
| **Total** | 1011 | **1014** | **+3** |

**Result: Passed! Failed: 0, Skipped: 0** across all three projects. ✅

## Coverage

- **AC1** (proposal created recording safe facts incl. optional expiry): covered — aggregate + replay + orchestrator + new full-chain E2E.
- **AC2** (never a Conversation Message; content-free surfaces): covered — content-free contract/aggregate/orchestrator guards + new E2E no-leak round-trip.
- **AC3** (unsafe content never yields an approvable proposal): covered — unit (`SafetyFailed`/`GenerationFailed` rejections) **and now end-to-end** via the new safety-blocked lifecycle test.
- **AC4** (no duplicate proposals/versions; deterministic replay): covered — terminal no-op + deterministic id + replay-determinism tests.

## Next Steps

- Run in CI alongside the existing Tier-1/Tier-2 suites (these are unit/replay tests; no Docker/Aspire required).
- The live read-model / expiry-policy / command-dispatch / Conversations bindings remain deferred (default DI graph fails
  closed); Tier-3 integration coverage of the real dispatch/read path arrives with the Epic 4 operational topology and
  Stories 3.2/3.6.

## Notes

- **Keep-it-simple:** the new E2E tests are linear command-chain drives through the existing `ProcessAndApplyAsync` /
  fixture helpers — no new mocks, no hardcoded waits, fully order-independent. The cancellation test mirrors the existing
  version-read cancellation test exactly.
- **Standards:** PascalCase BDD-style names (`{behavior}`), Shouldly assertions (no raw `Assert.*`), file-scoped namespaces,
  Allman braces, `ConfigureAwait`-clean, consistent with the surrounding Story 3.1 tests and `CLAUDE.md`.
