---
title: '5.2 Configure hexa Through Live EventStore Operations'
type: 'feature'
created: '2026-08-09'
status: 'done'
baseline_commit: '860d7e8e47213c47bcd02721b651df94ed1672b4'
review_loop_iteration: 0
followup_review_recommended: false
context:
  - '_bmad-output/implementation-artifacts/epic-5-context.md'
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-state-instructions.md'
warnings: []
deferred: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Agent configure, response-mode, and lifecycle mutations still hit deferred/unavailable seams, so administrators cannot change durable Agent state through authorized live EventStore commands and see the same current projected truth on API/client and FrontComposer.

**Approach:** Bind the existing public administration surface to a live DomainService/EventStore command-query-projection path for configure, response-mode, and lifecycle; publish one authoritative setup read model with version/freshness; and prove replay, conflict, authorization, and no-disclosure with persisted end-state evidence. Do not claim callability or activation readiness.

## Boundaries & Constraints

**Always:** Treat `_bmad-output/planning-artifacts/epics.md` Story 5.2 as executable authority. Authorize tenant Agent-administration before mutation or lookup-dependent disclosure. Keep aggregate handlers pure; impure work stays in application adapters. Preserve submitted → authoritative-pending → projection-confirmed truth. Keep lifecycle `active` distinct from callability. Reuse Parties/Tenants EventStore Client/projection patterns and existing Agents contracts/orchestrator trusted-extension style. Return structured accepted identities only — no stream, aggregate, workflow, or Provider SDK leakage.

**Ask First:** If Story 5.1 awaiting-operator work is required to keep the Agents DomainService host packageable for this story's verify path, stop and ask before inventing a parallel host. If live conflict semantics require a public ExpectedRevision contract change beyond EventStore envelope revision handling, stop and ask.

**Never:** Do not implement archived launch-readiness Story 5.2 outcomes, nor Stories 5.3–5.7 Provider/Party/readiness/host/activation work. Do not treat lifecycle active, optimistic UI, or partial configuration as callable. Do not disclose another tenant's existence, instructions, counts, diagnostics, or audit summaries. Do not leave deferred dispatcher/gateway as the production administration path for in-scope commands/queries.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Authorized command | Valid configure, response-mode, or lifecycle command at expected revision | Persist Agent events via DomainService/EventStore; return structured accepted identity; replay reconstructs identical state | Typed validation/conflict; no partial mutation or infra leak |
| Setup query and UI | Accepted events projected | Same authoritative identity/config/lifecycle/response-mode/policy refs + configuration version, projection version, freshness; distinguish submitted / authoritative-pending / projection-confirmed | Never infer callability from lifecycle or optimistic state |
| Duplicate/stale/invalid/replay | Duplicate command, stale revision, invalid fields, or replayed projection delivery | Exact duplicates idempotent; conflicts/validation typed; persisted read-model end state deterministic | Do not overwrite prior state or accept partial/optimistic success as evidence |
| Unauthorized/cross-tenant | Missing Agent-admin authority or other tenant | Deny before mutation, disclosure, or Provider/Party/Conversation/secret side effect | Reveal no target existence, instructions, counts, diagnostics, accessible names, or audit summary |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/epics.md:1213-1260` -- authoritative Story 5.2 scope, ACs, evidence manifest, dependency on 5.1.
- `src/Hexalith.Agents.Contracts/Agent/Commands/{UpdateAgentConfiguration,ConfigureAgentResponseMode,ActivateAgent,DisableAgent,CreateAgent}.cs` -- public write payloads already defined.
- `src/Hexalith.Agents.Contracts/Agent/Queries/{GetAgentConfigurationQuery,GetAgentStatusQuery}.cs` -- setup queries marked deferred; wire live handlers.
- `src/Hexalith.Agents.Contracts/Agent/AgentStatusView.cs:51-75` -- current status DTO; extend or companion for projection version + freshness without leaking instructions text.
- `src/Hexalith.Agents/Agent/{AgentAggregate,AgentState,AgentConfigurationPolicy,AgentInspection}.cs` -- pure handlers/replay/policy/status projection; Level-2 reuse, do not rewrite semantics.
- `src/Hexalith.Agents.Server/Ports/{IAgentCommandDispatcher,DeferredAgentCommandDispatcher}.cs` -- replace deferred throw with live EventStore gateway submit.
- `src/Hexalith.Agents.Server/Application/Agents/AgentResponseModeOrchestrator.cs:54-79` -- authorize → strip/repopulate `actor:agentsAdmin` → dispatch pattern to reuse for update/activate/disable/create.
- `src/Hexalith.Agents.Server/Projections/.gitkeep` -- empty; add Agent setup projection using Parties `PartyDetailSdkProjectionHandler` + `IReadModelStore`/`IReadModelFreshness` pattern.
- `src/Hexalith.Agents.Server/Application/Queries/AgentInteractionAuditQueryHandlerBase.cs` -- auth-before-disclosure query template for setup `IDomainQueryHandler`s.
- `src/Hexalith.Agents.Server/Program.cs:29-31,50,87,202,206-207` -- DomainService already registered; swap deferred DI binds and keep Unavailable client out of live path.
- `src/Hexalith.Agents.Client/IAgentAdministrationOperations.cs` + `UnavailableOperations.cs` -- public ops shape; implement live configure/query/lifecycle without EventStore types on the public surface.
- `src/Hexalith.Agents.Server/Endpoints/AgentsOperationEndpoints.cs:52-83` -- BFF routes already map administration; keep contract, bind live client.
- `src/Hexalith.Agents.UI/Components/Pages/AgentConfiguration.razor` + `IAgentSetupGateway`/`DeferredAgentSetupGateway` -- bind live reads/writes; add truth-flow states; no callability inference.
- Reuse (read-only examples): `references/Hexalith.Parties/.../PartyDetailSdkProjectionHandler.cs`, `HttpPartiesCommandClient`, `references/Hexalith.Tenants/.../TenantQueryHandlerBase.cs`, `references/Hexalith.EventStore/.../EventStoreDomainServiceExtensions.cs`.
- Tests to extend/add: `test/Hexalith.Agents.Tests/AgentStateReplayTests.cs`; new EventStore integration + setup-query + auth/disclosure + UI truth-flow tests; `eng/verify-story.ps1` currently 5.1-only — add `eng/verify-story-5.2.ps1` (or extend parametric verifier) per evidence manifest.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Agents.Server/Ports/` + `Program.cs` -- bind live `IAgentCommandDispatcher` to EventStore DomainService/gateway submit; keep fail-closed only for out-of-scope seams -- replace deferred throw for in-scope administration.
- [x] `src/Hexalith.Agents.Server/Application/Agents/` -- add/complete authorize-then-dispatch orchestrators for UpdateConfiguration, Activate, Disable, and Create mirroring `AgentResponseModeOrchestrator` trusted-extension rules.
- [x] `src/Hexalith.Agents.Server/Projections/` + Contracts setup view -- implement Agent setup projection/read model with configuration version, projection version, and freshness; wire `IDomainQueryHandler` for `GetAgentConfigurationQuery`/`GetAgentStatusQuery`.
- [x] `src/Hexalith.Agents.Client/` + `AgentsOperationEndpoints` -- implement live `IAgentAdministrationOperations` returning structured accepted identities and the same projected setup truth; no EventStore internals on public types.
- [x] `src/Hexalith.Agents.UI/` (`AgentConfiguration.razor`, `IAgentSetupGateway`) -- bind live gateway; support configure/response-mode/lifecycle writes; render submitted / authoritative-pending / projection-confirmed; never treat lifecycle active as callable.
- [x] `test/` + `eng/verify-story-5.2.ps1` -- add aggregate/replay, live EventStore command-query-projection, duplicate/conflict/replay, cross-tenant auth/no-disclosure, and UI truth-flow tests with persisted read-model end-state assertions; wire verifier to those tests.

**Acceptance Criteria:**
- Given an authorized tenant administrator and a valid configure, response-mode, or lifecycle command, when the public client/API dispatches through the DomainService boundary, then EventStore persists the Agent events, replay reconstructs identical state and safe change evidence, and the response exposes a structured accepted identity rather than infrastructure internals.
- Given accepted Agent events, when the setup query and FrontComposer surface load, then both expose the same authoritative identity/configuration/lifecycle/response-mode/policy/version/freshness state and distinguish submitted, authoritative-pending, and projection-confirmed terminal truth without treating lifecycle active as callable.
- Given duplicate commands, stale expected revisions, invalid fields, or replayed projection deliveries, when focused tests execute, then duplicates are idempotent, failures are typed, prior state is preserved, and the persisted read-model end state remains deterministic with no partial or optimistic success accepted as evidence.
- Given an unauthorized caller or a caller from another tenant, when configuration commands or queries execute, then authorization denies before mutation or lookup-dependent disclosure, produces no Provider/Party/Conversation/secret side effect, and reveals no target-tenant existence or sensitive instructions.

## Spec Change Log

- 2026-08-05: Human selected current canonical Story 5.2. Replaced the archived launch-readiness collision with the bounded configure-through-EventStore intent.
- 2026-08-09: Re-planned after clean-tree unblock; drained live-binding investigation into Code Map/Tasks (deferred dispatcher, empty projections, unavailable client/UI gateway, missing verify-story-5.2).

## Design Notes

Filename slug still reflects the archived title; numeric Story 5.2 in `epics.md` is authority. Story 5.1 is `awaiting-operator` for EXT-HOST-1 register updates — treat package/host baseline as already landed unless verify proves otherwise. Mirror Parties projection freshness (`IReadModelFreshness`) rather than inventing Agents-only metadata. Keep `IAgentConfigurationSnapshotReader` consistent with the new setup read model only as needed for non-contradiction; call-path callability remains later stories.

## Verification

**Commands:**
- `pwsh ./eng/verify-story-5.2.ps1` -- expected: exit 0; focused Story 5.2 tests pass including negative auth/disclosure and persisted read-model assertions.
- `dotnet test test/Hexalith.Agents.Tests test/Hexalith.Agents.Server.Tests test/Hexalith.Agents.UI.Tests --filter "FullyQualifiedName~AgentConfiguration|FullyQualifiedName~AgentSetup|FullyQualifiedName~AgentStateReplay"` -- expected: pass for in-scope suites (adjust filter to final test names).

**Manual checks (if no CLI):**
- After an authorized configure command, API/client and `/agents/configuration` show identical projection-confirmed state; lifecycle Success is not shown as callable.

## Suggested Review Order

**Live command path**

- Host registers live setup services when EventStore BaseUrl is configured.
  [`Program.cs:210`](../../src/Hexalith.Agents.Server/Program.cs#L210)

- Conditional DI swaps deferred dispatcher/client for EventStore-backed implementations.
  [`AgentSetupServiceCollectionExtensions.cs:32`](../../src/Hexalith.Agents.Server/Composition/AgentSetupServiceCollectionExtensions.cs#L32)

- Gateway submit uses message id as idempotency key for exact duplicates.
  [`EventStoreAgentCommandDispatcher.cs:57`](../../src/Hexalith.Agents.Server/Ports/EventStoreAgentCommandDispatcher.cs#L57)

- Trusted tenant overwrites CreateAgent body before authorize-then-dispatch.
  [`AgentAdministrationOrchestrator.cs:72`](../../src/Hexalith.Agents.Server/Application/Agents/AgentAdministrationOrchestrator.cs#L72)

- Activation revalidates projected provider/approver before aggregate gates.
  [`EventStoreAgentAdministrationOperations.cs:152`](../../src/Hexalith.Agents.Server/Application/Agents/EventStoreAgentAdministrationOperations.cs#L152)

**Setup projection and queries**

- Checkpointed Agent setup projection owns the durable read-model slot.
  [`AgentSetupProjectionHandler.cs:24`](../../src/Hexalith.Agents.Server/Projections/AgentSetupProjectionHandler.cs#L24)

- Pure fold builds redacted setup state without instruction text.
  [`AgentSetupProjectionFold.cs:86`](../../src/Hexalith.Agents.Server/Projections/AgentSetupProjectionFold.cs#L86)

- Auth-before-disclosure query handlers serve identical setup truth.
  [`AgentSetupQueryHandlerBase.cs:32`](../../src/Hexalith.Agents.Server/Application/Queries/AgentSetupQueryHandlerBase.cs#L32)

- Expected-version lag maps to AuthoritativePending vs ProjectionConfirmed.
  [`AgentSetupViewFactory.cs:54`](../../src/Hexalith.Agents.Server/Projections/AgentSetupViewFactory.cs#L54)

**Public contracts and BFF**

- Setup view carries versions, freshness, and truth stage without secrets.
  [`AgentSetupView.cs:22`](../../src/Hexalith.Agents.Contracts/Agent/AgentSetupView.cs#L22)

- Acceptance returns structured identity at Submitted only.
  [`AgentCommandAcceptance.cs:15`](../../src/Hexalith.Agents.Contracts/Operations/AgentCommandAcceptance.cs#L15)

- Path-scoped administration routes pass agentId and expected version.
  [`AgentsOperationEndpoints.cs:25`](../../src/Hexalith.Agents.Server/Api/AgentsOperationEndpoints.cs#L25)

**FrontComposer truth flow**

- Configure/response-mode bump expected version; activate/disable do not.
  [`AgentConfiguration.razor:284`](../../src/Hexalith.Agents.UI/Components/Pages/AgentConfiguration.razor#L284)

- Live UI gateway maps typed read/write outcomes without fabricating state.
  [`AgentsClientSetupGateway.cs:34`](../../src/Hexalith.Agents.UI/Services/Gateways/AgentsClientSetupGateway.cs#L34)

- Hosts opt into live setup gateway via AddAgentsUiSetup when AgentId is set.
  [`AgentsUiServiceCollectionExtensions.cs:53`](../../src/Hexalith.Agents.UI/Services/Gateways/AgentsUiServiceCollectionExtensions.cs#L53)

**Verification**

- Story verifier runs focused gates plus DI composition assertions.
  [`verify-story-5.2.ps1:62`](../../eng/verify-story-5.2.ps1#L62)
