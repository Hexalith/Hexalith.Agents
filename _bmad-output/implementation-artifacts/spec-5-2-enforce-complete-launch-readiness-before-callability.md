---
title: '5.2 Configure hexa Through Live EventStore Operations'
type: 'feature'
created: '2026-08-04'
status: blocked
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '_bmad-output/implementation-artifacts/epic-5-context.md'
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
warnings: []
deferred: []
---

<intent-contract>

## Intent

**Problem:** Agent configuration must move through authorized live EventStore commands and queries so the durable state exposed by public API/client and FrontComposer surfaces is replayable, current, tenant-isolated, and safe to automate.

**Approach:** Implement the current canonical Story 5.2, `Configure hexa Through Live EventStore Operations`, exactly as defined in `_bmad-output/planning-artifacts/epics.md`. Route valid configure, response-mode, and lifecycle commands through the DomainService/EventStore boundary; project the accepted events into one authoritative setup read model; expose the same truth through public client/API and FrontComposer surfaces; and verify replay, conflict, authorization, and disclosure behavior with persisted end-state evidence.

## Boundaries & Constraints

**Always:** Treat `_bmad-output/planning-artifacts/epics.md` as executable authority; preserve the EventStore domain-module boundary; authorize before mutation or disclosure; keep submitted, authoritative-pending, and projection-confirmed states distinct; and keep lifecycle active separate from callability.

**Block If:** The implementation would bypass the DomainService/EventStore command-query-projection path, cannot enforce tenant administration before mutation or disclosure, or requires work owned by a later readiness, activation, Provider, Party, Conversation, secret, workflow, or release-qualification story.

**Never:** Do not implement the archived all-in-one launch-readiness Story 5.2 or the replacement outcomes owned by current Stories 5.5, 5.7, 8.7, or release gate `RQ-1`; do not treat lifecycle active, optimistic UI state, or partial configuration as callable or authoritative; and do not disclose another tenant's existence, instructions, configuration, counts, or diagnostics.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Authorized command | Valid configure, response-mode, or lifecycle command at the expected revision | Persist Agent events through the DomainService/EventStore boundary, return a structured accepted identity, and reconstruct identical state on replay | Typed validation/conflict outcomes; no partial mutation or infrastructure leakage |
| Setup query and UI | Accepted Agent events are projected | Expose current identity reference, display metadata, instructions version, lifecycle, response mode, approver-policy reference, configuration version, projection version, and freshness; distinguish submitted, authoritative-pending, and projection-confirmed states | Never infer callability from lifecycle or optimistic state |
| Duplicate, stale, invalid, or replayed input | Duplicate command, stale expected revision, invalid fields, or replayed projection delivery | Exact duplicates are idempotent; conflicts and validation failures are typed; persisted read-model end state remains deterministic | Do not overwrite prior state or accept partial/optimistic success as evidence |
| Unauthorized or cross-tenant access | Caller lacks Agent-administration authority or targets another tenant | Deny before mutation, lookup-dependent disclosure, or Provider/Party/Conversation/secret side effect | Reveal no target existence, instructions, counts, diagnostics, accessible names, or audit summary |

</intent-contract>

## Code Map

- `_bmad-output/planning-artifacts/epics.md:1213` -- authoritative Story 5.2 scope, acceptance criteria, evidence manifest, and dependency on Story 5.1.
- `src/Hexalith.Agents.Contracts/Agent/Commands/UpdateAgentConfiguration.cs` and `src/Hexalith.Agents.Contracts/Agent/Commands/ConfigureAgentResponseMode.cs` -- current public Agent configuration command contracts.
- `src/Hexalith.Agents.Contracts/Agent/Queries/GetAgentConfigurationQuery.cs` and `src/Hexalith.Agents.Contracts/Agent/AgentStatusView.cs` -- current setup-query and status contracts to align with the authoritative projected state.
- `src/Hexalith.Agents/Agent/AgentState.cs` and `src/Hexalith.Agents/Agent/AgentConfigurationPolicy.cs` -- current aggregate replay state and configuration policy seams.
- `src/Hexalith.Agents.Client/IAgentAdministrationOperations.cs` and `src/Hexalith.Agents.Server/Ports/DeferredAgentCommandDispatcher.cs` -- public client operation and fail-closed server dispatch seams.
- `src/Hexalith.Agents.Server/Ports/IAgentConfigurationSnapshotReader.cs` and `src/Hexalith.Agents.Server/Ports/DeferredAgentConfigurationSnapshotReader.cs` -- current configuration-read seam that must become live without bypassing EventStore projections.
- `src/Hexalith.Agents.UI/Components/Pages/AgentConfiguration.razor` -- FrontComposer configuration surface that must render authoritative command/query truth without inferring callability.
- `test/Hexalith.Agents.Tests/AgentStateReplayTests.cs`, `test/Hexalith.Agents.Server.Tests/DeferredAgentCommandDispatcherTests.cs`, and `test/Hexalith.Agents.UI.Tests/AgentConfigurationTests.cs` -- focused existing test seams to extend with persisted end-state, authorization, and UI truth-flow evidence.

## Tasks & Acceptance

**Execution:**
- Implement authorized configure, response-mode, and lifecycle commands through the public client/API and DomainService/EventStore boundary, preserving pure aggregate/event behavior and deterministic replay.
- Implement the authoritative Agent setup projection/query contract with version and freshness metadata, then bind the public client/API and FrontComposer configuration surface to that same projected truth.
- Preserve submitted, authoritative-pending, and projection-confirmed terminal states; never present lifecycle active or optimistic UI completion as callability evidence.
- Add focused aggregate/replay, live EventStore command-query-projection, duplicate/conflict, cross-tenant denial, no-disclosure, and UI truth-flow tests, including persisted read-model end-state assertions.

**Acceptance Criteria:**
- Given an authorized tenant administrator and a valid configure, response-mode, or lifecycle command, when the public client/API dispatches through the DomainService boundary, then EventStore persists the resulting Agent events, replay reconstructs identical Agent state and configuration evidence, and the response exposes a structured accepted identity rather than infrastructure internals.
- Given accepted Agent events, when the setup query and FrontComposer surface load, then both expose the same authoritative identity/configuration/lifecycle/response-mode/policy/version/freshness state and distinguish submitted, authoritative-pending, and projection-confirmed terminal truth without treating lifecycle active as callable.
- Given duplicate commands, stale expected revisions, invalid fields, or replayed projection deliveries, when focused tests execute, then duplicates are idempotent, failures are typed, prior state is preserved, and the persisted read-model end state remains deterministic with no partial or optimistic success accepted as evidence.
- Given an unauthorized caller or a caller from another tenant, when configuration commands or queries execute, then authorization denies before mutation or lookup-dependent disclosure, produces no Provider/Party/Conversation/secret side effect, and reveals no target-tenant existence or sensitive instructions.

## Spec Change Log

- 2026-08-05: Human selected current canonical Story 5.2. Replaced the archived launch-readiness collision with the bounded `Configure hexa Through Live EventStore Operations` intent, tasks, edge cases, and acceptance criteria; later readiness, activation, inspection, and release outcomes remain explicitly out of scope.

## Review Triage Log

## Design Notes

The historical dispatch slug still contains the archived title, but the human decision makes the numeric current authority decisive: this re-drive implements only current Story 5.2, `Configure hexa Through Live EventStore Operations`. The archived launch-readiness outcome remains decomposed across Stories 5.5, 5.7, 8.7, and `RQ-1`; none is pulled into this story.

## Auto Run Result

Status: blocked
Blocking condition: dirty working tree before Step 1; `_bmad-output/implementation-artifacts/spec-5-2-enforce-complete-launch-readiness-before-callability.md` was already untracked at preflight (the branch was also one commit ahead of `origin/main`)
