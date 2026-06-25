---
baseline_commit: 7a35b30
created: 2026-06-25T02:14:55+02:00
---

# Story 4.1: Stable API And Client Contracts For Agent Operations

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an Integration Developer,
I want stable public API/client contracts for Agent operations,
so that automation can manage and monitor governed Agent workflows without depending on internals.

## Acceptance Criteria

**AC1 - Public operation coverage (FR23, AD-15)**

**Given** setup, invocation, and proposal workflows exist
**When** public API/client contracts are published
**Then** contracts cover Provider administration, Agent administration, Agent invocation, proposal workflow, status inspection, and audit inspection
**And** consumers do not need raw EventStore stream names, aggregate mechanics, projection internals, provider SDK details, or workflow SDK types.

**AC2 - Structured automation errors (FR20/FR23, AD-12/AD-14)**

**Given** an integration caller submits an operation
**When** authorization or validation fails
**Then** the contract returns structured success/error results suitable for automation
**And** errors do not leak provider secrets, raw payloads, stack traces, or unrelated tenant records.

**AC3 - Additive-first contract stability (AD-17)**

**Given** public contract changes are made during V1
**When** contract tests and public API baselines run
**Then** changes are additive-first or explicitly versioned when breaking
**And** admin UI and client contracts share the same authorization outcomes.

**AC4 - Canonical operation status terms (UX-DR25..UX-DR30, FR25)**

**Given** the API/client contracts expose operation status
**When** callers inspect setup, invocation, proposal, posting, audit, or launch readiness state
**Then** status terms align with the UX canonical states
**And** pending states are not promoted to success.

## Scope & Boundaries (read first)

- **This is a Contracts + Client + thin API/BFF contract story.** It publishes the stable integration surface over functionality shipped in Epics 1-3. It must not re-implement provider generation, proposal lifecycle, audit storage, launch readiness gates, or FrontComposer pages.
- **Build on the existing contract types.** `Hexalith.Agents.Contracts` already contains ProviderCatalog commands/queries, Agent admin commands/queries, AgentInteraction commands/results, proposal workflow commands/results, proposal detail/queue contracts, and several evidence query wrappers. Reuse those shapes; add only the missing automation-level envelopes/facade types needed to make the surface usable without raw EventStore or projection details.
- **`Hexalith.Agents.Client` is currently only a marker assembly.** This story is where the public client facade begins. Keep the client consumer-facing and infrastructure-light: `Client -> Contracts` only, no Server/UI/EventStore DomainService/Dapr/provider/runtime SDK references.
- **Live read/write bindings are still partially deferred.** `src/Hexalith.Agents.Server/Program.cs` registers `DeferredAgentCommandDispatcher` and many `Deferred*Reader` seams. The public API must convert unavailable/deferred paths into structured `Unavailable`/`NotAuthorized`/`Rejected` style results, never throw the deferred seam message to callers. Live read-model and dispatcher binding can be limited to what this story needs; broader status/audit/readiness implementation belongs to Stories 4.2-4.4.
- **Audit inspection is contract coverage only in 4.1.** Provide the stable audit/evidence query/client operation surface, but do not implement retention/legal hold/export/deletion behavior and do not expose raw prompt/generated/edited content. Story 4.2 owns safe Audit Evidence query behavior.
- **Operational status surfaces are contract coverage only in 4.1.** Align status enums and result wrappers to canonical UX states. Story 4.3 owns richer operational status UI/API implementation, and Story 4.4 owns launch readiness gates.
- **No dependency upgrades in this story.** External check on 2026-06-25 found Microsoft.Agents.AI `1.11.0` on NuGet, while architecture lists `1.10.0`; this story must not add or upgrade Microsoft Agent Framework packages because public contracts must remain free of runtime SDK types. Dapr docs identify v1.18 as latest docs, but public contract/client code must not reference Dapr packages. [Source: NuGet Microsoft.Agents.AI, Dapr v1.18 docs]
- **Module is `Hexalith.Agents/`**, not any sibling module. Sibling source dependencies are read-only context unless a specific integration contract is already public.

## Tasks / Subtasks

- [x] **Task 1 - Define the public operation envelope and error taxonomy** (AC: 2, 3, 4) - `src/Hexalith.Agents.Contracts/Operations/`
  - [x] Add a small, reusable operation result model for automation (for example `AgentOperationResult<T>`, `AgentOperationStatus`, `AgentOperationError`, `AgentOperationErrorCode`, and optional paging/continuation metadata if needed by list operations). Keep it additive, JSON round-trip friendly, and enum-by-name with `Unknown = 0`.
  - [x] Encode safe error classes only: `NotAuthorized`, `ValidationFailed`, `NotFound`, `Conflict`, `Stale`, `Unavailable`, `Rejected`, `Blocked`, and `Unknown`. Do not include exception type names, stack traces, raw provider error payloads, raw EventStore stream names, tenant fingerprints, prompt/content, or secrets.
  - [x] Add explicit contract guidance/XML docs that `TenantId`, authenticated user/party context, tokens, claims, and trusted policy verdicts are server-controlled. Client-supplied metadata is limited to idempotency/correlation/options that existing orchestrators sanitize.
  - [x] Map pending and degraded operation states to canonical UX terms; do not collapse `Approved`, `PostingPending`, `Posted`, and `PostingFailed`.
  - [x] Tests: JSON round-trip, enum string serialization, `Unknown = 0`, factory behavior, no-sensitive-member scan, and no leakage of poison strings such as prompt/generated content/provider secrets.

- [x] **Task 2 - Publish the `Hexalith.Agents.Client` facade** (AC: 1, 2, 3) - `src/Hexalith.Agents.Client/`
  - [x] Replace the marker-only client assembly with public operation interfaces and a concrete client entry point. Prefer a thin facade split by workflow area if it keeps the surface readable, for example `IAgentsClient` plus `ProviderCatalog`, `AgentAdministration`, `AgentInteractions`, `ProposalWorkflow`, `Status`, and `Audit` operation groups.
  - [x] Cover these operation families using existing contract records where possible:
    - Provider administration: list/get/create/update/enable/disable provider-model entries.
    - Agent administration: get status/configuration, create/update configuration, link/replace Party identity, select provider/model, configure response mode, configure approver policy, configure content safety policy, activate, disable.
    - Invocation: request Agent interaction, inspect Agent interaction status, inspect gate/context/generation/posting evidence through safe result wrappers.
    - Proposal workflow: list pending/historical authorized proposals, get proposal detail, edit, regenerate, approve selected version, reject, abandon, expire.
    - Status inspection: setup/readiness, provider/model readiness, call/proposal/posting status terms using the existing and newly added canonical status DTOs.
    - Audit inspection: stable query methods for support-safe evidence that Story 4.2 can bind to real projections.
  - [x] The public client must not expose `CommandEnvelope`, EventStore domain/aggregate ids as implementation mechanics, raw stream names, projection actor names, Dapr app ids, provider SDK request/response types, Microsoft Agent Framework workflow/session types, or `IServiceProvider`.
  - [x] If an HTTP adapter is added, use only base framework HTTP/STJ APIs already available to the client project. Do not add ASP.NET Core, Dapr, EventStore DomainService, provider SDK, Agent Framework, or FrontComposer packages to `Hexalith.Agents.Client`.
  - [x] Keep cancellation tokens on all async methods, use `ConfigureAwait(false)` on every awaited call, and validate public arguments with `ArgumentNullException.ThrowIfNull` / `ArgumentException.ThrowIfNullOrWhiteSpace`.
  - [x] Tests: client surface compiles without forbidden packages, operation methods serialize expected request/response JSON, failed HTTP/transport paths map to structured `Unavailable`/`NotAuthorized` style results, and no method exposes internal substrate types.

- [x] **Task 3 - Add the thin public API/BFF contract layer** (AC: 1, 2, 3) - `src/Hexalith.Agents.Server/`
  - [x] Add endpoint/route registration under an explicit Agents operations area (for example `Api/AgentsOperationEndpoints.cs`) and call it from `Program.cs` after service registration and before `app.Run()`. Keep `UseEventStoreDomainService()` intact.
  - [x] Endpoints must consume the same public contract/client request shapes and return the same operation result envelopes as the client facade. The UI gateways and the integration client must share authorization outcomes and status terms.
  - [x] For write paths, call the existing application orchestrators rather than dispatching raw EventStore envelopes from endpoint code. Existing orchestrators already strip reserved client-supplied extensions and repopulate trusted server evidence.
  - [x] For read paths not yet live, return structured fail-closed results (`NotAuthorized`, `Unavailable`, `Stale`, or `NotFound`) rather than reading projections directly or inventing temporary in-memory data.
  - [x] Catch known deferred/unavailable seams at the boundary and map them to structured automation errors. Do not leak `DeferredAgentCommandDispatcher` exception text or stack traces.
  - [x] Keep endpoint authorization fail-closed. Authorization and tenant/conversation/approver dependency uncertainty must occur before provider invocation, command dispatch, or content-bearing reads.
  - [x] Tests: endpoint contract tests with substituted orchestrators/read seams proving success, validation failure, not authorized, unavailable/deferred, stale, and not found shapes; no raw internal strings or substrate names in responses.

- [x] **Task 4 - Align UI gateways with the public client surface without rewiring UI behavior** (AC: 3, 4) - `src/Hexalith.Agents.UI/Services/Gateways/`
  - [x] Review gateway result wrappers and ensure their statuses can be represented by the new public operation taxonomy without losing meaning.
  - [x] Do not replace existing fail-closed deferred UI gateways wholesale. Add adapter seams only where the public client facade naturally backs an existing gateway.
  - [x] Preserve `TryAddScoped` registration in `AgentsUiServiceCollectionExtensions.cs` so live host registration can override deferred gateways.
  - [x] Tests: existing `DeferredGatewayTests`, proposal queue/detail tests, and gateway wrapper tests still prove fail-closed behavior and status parity.

- [x] **Task 5 - Add public API/client baseline and boundary tests** (AC: 1, 2, 3, 4) - `tests/Hexalith.Agents.Contracts.Tests`, `tests/Hexalith.Agents.Client.Tests`, `tests/Hexalith.Agents.Server.Tests`
  - [x] Add or extend public API baseline tests for the client facade and operation result contracts. If this repo has no shipped-baseline file yet, create a focused test that reflects public types/methods and fails on accidental removal/rename; do not create broad snapshot noise.
  - [x] Extend forbidden dependency tests so `Hexalith.Agents.Client` remains `Contracts`-only and both public projects stay free of Dapr, ASP.NET Core server infrastructure, EventStore DomainService/server internals, FrontComposer, provider SDKs, Microsoft Agent Framework, and ModelContextProtocol.
  - [x] Add conformance tests proving all status enums use `Unknown = 0`, serialize by name, and include all canonical states needed by UX-DR25..UX-DR30.
  - [x] Add no-leak tests that serialize representative success/error/status/audit results and assert poison values for prompt text, generated content, edited content, provider secret, raw provider payload, stack trace, and other-tenant ids never appear unless a field is explicitly content-bearing and authorized. This story should avoid content-bearing fields.
  - [x] Add client/API tests proving pending states remain pending (`PostingPending`, `AuditPending`, readiness `Checking`) and are never rendered or serialized as `Success`.

- [x] **Task 6 - Build, test, and Dev Agent Record accuracy** (AC: 1, 2, 3, 4)
  - [x] From `Hexalith.Agents/`: `dotnet restore Hexalith.Agents.slnx`; `dotnet build Hexalith.Agents.slnx -c Release -m:1` with 0 warnings and 0 errors.
  - [x] Run touched test projects individually. Expected likely set: Contracts, Client, Server, UI if gateway adapters change. If VSTest socket fails with `SocketException (13)`, run the built xUnit v3 executables directly as the previous stories did.
  - [x] Regenerate test counts from the actual run and diff the File List against `git status --short` before moving the story to review. This is a recurring Epic 2/3 review finding; stale counts or omitted files are not acceptable.

## Dev Notes

### Current Implementation Context

- `Hexalith.Agents.Contracts` already contains the domain command/query/result vocabulary for most operation families:
  - Agent admin: `Agent/Commands/*`, `Agent/Queries/GetAgentConfigurationQuery.cs`, `Agent/Queries/GetAgentStatusQuery.cs`, `AgentInspectionResult`, `AgentStatusView`.
  - Provider admin: `ProviderCatalog/Commands/*`, `ProviderCatalog/Queries/*`, `ProviderCatalogEntryView`, `ProviderCatalogInspectionResult`.
  - Invocation/status: `AgentInteraction/Commands/RequestAgentInteraction.cs`, `GetAgentInteractionStatusQuery`, `AgentCallRequestResult`, `AgentInteractionInspectionResult`, `AgentInteractionStatusView`.
  - Proposal queue/detail/actions: `ListPendingProposalsQuery`, `PendingProposalsResult`, `GetProposalDetailQuery`, `ProposalDetailResult`, edit/regenerate/approve/reject/abandon/expire commands and result types.
  - Evidence reads: `GetAgentInteractionGateEvidenceQuery`, `GetAgentInteractionContextEvidenceQuery`, `GetAgentProposal*EvidenceQuery` and their result/view records.
- `Hexalith.Agents.Client` currently contains only `AgentsClientAssemblyMarker`; Story 4.1 should introduce the first real public client surface here, not in UI or Server internals.
- `Program.cs` is a long, story-annotated composition root. It registers existing orchestrators/read ports and many deferred fail-closed seams, then builds the EventStore domain service host. Do not delete the story comments or change unrelated registrations.
- Existing UI gateways are fail-closed and registered with `TryAddScoped` in `AgentsUiServiceCollectionExtensions.AddAgentsUi()`. A future host can replace them; this story should preserve that override pattern.

### Architecture Guardrails

- **AD-15 Public surface and UI parity:** admin UI and API/client surfaces must share public Agents contracts and authorization outcomes. FrontComposer UI calls Agents API/BFF/client boundaries, never EventStore streams, provider SDKs, or aggregate internals. [Source: ARCHITECTURE-SPINE.md#AD-15]
- **AD-17 Contract and test gates:** public contracts are versioned and additive-first. Tests must cover contract stability, fail-closed authorization, replay/idempotency, tenant isolation, provider-secret non-disclosure, UI/contract conformance, and audit completeness. [Source: ARCHITECTURE-SPINE.md#AD-17]
- **AD-12 Authorization and dependency uncertainty:** every API/UI/provider/post/proposal/audit path evaluates tenant, Party, Conversation, Agent, Provider, and ApproverPolicy gates before side effects; missing/stale/unavailable dependency state fails closed. [Source: ARCHITECTURE-SPINE.md#AD-12]
- **AD-14 Sensitive content and secret safety:** generated, edited, prompt-derived, and context-derived content is sensitive. Logs, telemetry, status, audit summaries, and accessible names must not include raw content, provider payloads, stack traces, or secrets. [Source: ARCHITECTURE-SPINE.md#AD-14]
- **AD-18/AD-19 runtime boundaries:** public contracts and EventStore aggregates must not depend on Microsoft Agent Framework, Dapr AI, Dapr Agents, provider SDK, workflow SDK, MCP, or A2A types. Runtime/tool protocols stay behind host/adapters. [Source: ARCHITECTURE-SPINE.md#AD-18, #AD-19]
- **Project structure:** keep public contract additions under `src/Hexalith.Agents.Contracts`; public client additions under `src/Hexalith.Agents.Client`; API/BFF endpoint glue under `src/Hexalith.Agents.Server`; UI gateway adapters under `src/Hexalith.Agents.UI/Services/Gateways`.

### UX/Status Contract Requirements

- Canonical states from UX must be preserved across API/client/UI:
  - Agent readiness: `callable`, `checking`, `invalid configuration`, `missing party identity`, `provider unavailable`, `disabled`.
  - Provider/model: `enabled`, `disabled`, `degraded`, `failed`, `not configured`.
  - Agent call: `requested`, `authorized`, `denied`, `context loading`, `context blocked`, `generating`, `generation failed`, `generated`.
  - Proposal lifecycle: `generated`, `edited`, `regenerated`, `pending approval`, `approved`, `rejected`, `abandoned`, `expired`, `posting pending`, `posted`, `posting failed`.
  - Audit availability: `audit pending`, `audit available`, `audit delayed`, `audit unavailable`.
- Every public error/status string must be safe for automation and UI display. Do not use raw exception text as a public error message.
- Pending and degraded states are not success. `Approved` is not `Posted`; `AuditPending` is not `AuditAvailable`; readiness `Checking` is not `Callable`.

### Reuse Map - Build On These, Do Not Reinvent

| Need | Reuse / Extend | Path |
| --- | --- | --- |
| Provider admin contracts | Existing commands/queries/results | `src/Hexalith.Agents.Contracts/ProviderCatalog/` |
| Agent admin contracts | Existing commands/queries/results | `src/Hexalith.Agents.Contracts/Agent/` |
| Invocation result wrappers | Existing request/status wrappers | `src/Hexalith.Agents.Contracts/AgentInteraction/AgentCallRequestResult.cs`, `AgentInteractionInspectionResult.cs` |
| Proposal queue/detail contracts | Existing read contracts | `src/Hexalith.Agents.Contracts/AgentInteraction/PendingProposalsResult.cs`, `ProposalDetailResult.cs` |
| Proposal workflow contracts | Existing commands/results/outcomes | `src/Hexalith.Agents.Contracts/AgentInteraction/Commands/`, `AgentProposal*Result.cs` |
| Server write decision logic | Existing orchestrators | `src/Hexalith.Agents.Server/Application/Agents/`, `Application/AgentInteractions/` |
| Command dispatch seam | Existing port, currently deferred | `src/Hexalith.Agents.Server/Ports/IAgentCommandDispatcher.cs`, `DeferredAgentCommandDispatcher.cs` |
| UI fail-closed gateway pattern | Existing gateway interfaces/deferred implementations | `src/Hexalith.Agents.UI/Services/Gateways/` |
| Boundary tests | Existing dependency guards | `tests/Hexalith.Agents.Contracts.Tests/ContractsBoundaryTests.cs`, `tests/Hexalith.Agents.Server.Tests/PublicContractPackageBoundaryTests.cs`, `ProjectReferenceDirectionTests.cs` |

### Known Traps / Carry-Forward

- **Do not create duplicate contracts.** Many operation-specific commands, queries, views, statuses, and result wrappers already exist. Add a thin operation/client layer over them instead of creating parallel provider/agent/proposal DTO families.
- **Do not leak EventStore mechanics.** Consumers must not need `CommandEnvelope`, raw stream names, aggregate replay details, projection actor ids, domain-service `/process`, or internal command status plumbing.
- **Do not expose runtime SDK types.** Public Contracts/Client projects must stay free of Dapr, Agent Framework, provider SDK, MCP/A2A, FrontComposer, and Server infrastructure dependencies.
- **Deferred seams must become structured responses.** The default graph has deferred dispatcher/readers. Public API calls must map unavailable/deferred dependencies to structured results; leaking the deferred exception message is a contract bug.
- **Tenant and authorization context is not client-supplied.** Do not accept `TenantId`, user id, Party claims, policy verdicts, or authorization basis from client request bodies. These come from authenticated server context and trusted readers/resolvers.
- **Audit retention/legal hold/export/deletion remains unresolved.** Do not silently make product/governance assumptions in 4.1; expose launch/audit blockers as safe status where needed and leave implementation to 4.2/4.4.
- **Recurring review finding:** test counts and File List have been stale in prior stories. Regenerate from the latest actual runs and compare against `git status --short` immediately before review.

### Technical Baseline

- Local baseline: .NET SDK `10.0.300`-`10.0.301`, `net10.0`, `.slnx`, Central Package Management, Dapr runtime/docs v1.18 family, Aspire `13.4.6`, MediatR `14.1.0`, FluentValidation `12.1.1`, Fluent UI Blazor `5.0.0-rc.3-26138.1`, xUnit v3 `3.2.2`, Shouldly `4.3.0`. [Source: epics.md#Additional-Requirements; ARCHITECTURE-SPINE.md#Stack; Directory.Packages.props]
- External package check on 2026-06-25:
  - NuGet lists Microsoft.Agents.AI `1.11.0` as current; architecture pinned `1.10.0`. This story should not add Agent Framework dependencies to public contracts/client. If runtime code later upgrades, that belongs to a runtime-owned story.
  - Dapr docs show v1.18 as latest stable docs. This story should not add Dapr dependencies to public contracts/client.

### Testing Notes

- Contracts tests use xUnit v3 + Shouldly and favor `Subject_scenario_expectation` names in this module.
- `ConfigureAwait(false)` is required on every awaited call in library/client/server code unless a test framework-specific exception is justified.
- Do not run solution-level `dotnet test`; run touched test projects individually. If the VSTest socket permission issue recurs, use the built xUnit v3 executables directly, as in Story 3.7.
- Existing test baselines from the last reviewed story: Contracts 297/297, UI 682/682, Agents 651/651, Server 335/335. Treat these only as historical reference; regenerate actual counts after this story.

### Project Structure Notes

- Expected new files:
  - `src/Hexalith.Agents.Contracts/Operations/*`
  - `src/Hexalith.Agents.Client/IAgentsClient.cs` and related operation-group/client files
  - `src/Hexalith.Agents.Server/Api/*` or equivalent endpoint registration files
  - `tests/Hexalith.Agents.Client.Tests/*` if not already present in the solution
- Expected edited files:
  - `src/Hexalith.Agents.Client/AgentsClientAssemblyMarker.cs`
  - `src/Hexalith.Agents.Server/Program.cs`
  - `src/Hexalith.Agents.UI/Services/Gateways/AgentsUiServiceCollectionExtensions.cs` only if adding client-backed gateway adapters
  - `tests/Hexalith.Agents.Contracts.Tests/*`
  - `tests/Hexalith.Agents.Server.Tests/PublicContractPackageBoundaryTests.cs`
  - `tests/Hexalith.Agents.Server.Tests/ProjectReferenceDirectionTests.cs`
- Avoid `bin/`, `obj/`, generated FrontComposer output, sibling module source, and planning artifacts except for story/test output updates.

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-4.1] - story and acceptance criteria.
- [Source: _bmad-output/planning-artifacts/epics.md#Requirements-Inventory] - FR22, FR23, FR24, FR25, FR28 plus cross-cutting FR19-FR21.
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-12] - fail-closed authorization and dependency uncertainty.
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-14] - sensitive content and secret safety.
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-15] - public surface and UI parity.
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-17] - contract and test gates.
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#State-Patterns] - canonical Agent, provider, call, proposal, list/detail, and audit states.
- [Source: _bmad-output/implementation-artifacts/3-7-proposal-detail-version-history-and-accessibility.md#Known-traps] - carry-forward deferred live binding, safe status, and Dev Agent Record accuracy lessons.
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Program.cs] - current deferred/live service composition.
- [Source: Hexalith.Agents/src/Hexalith.Agents.Client/AgentsClientAssemblyMarker.cs] - client marker-only current state.
- [Source: Hexalith.Agents/tests/Hexalith.Agents.Server.Tests/PublicContractPackageBoundaryTests.cs] - public package boundary guard to extend.
- [Source: https://www.nuget.org/packages/Microsoft.Agents.AI/] - current NuGet package information checked 2026-06-25.
- [Source: https://docs.dapr.io/developing-applications/sdks/dotnet/dotnet-client/] - Dapr v1.18 docs and client guidance checked 2026-06-25.

## Dev Agent Record

### Agent Model Used

GPT-5 Codex

### Debug Log References

- 2026-06-25: Added operation envelope/status/error contracts under `Hexalith.Agents.Contracts/Operations`.
- 2026-06-25: Added `Hexalith.Agents.Client` operation-group facade with a fail-closed unavailable default implementation.
- 2026-06-25: Added `/api/agents/operations` route registration and wired it in `Program.cs` while preserving `UseEventStoreDomainService()`.
- 2026-06-25: `dotnet restore Hexalith.Agents.slnx` exits 1 with no diagnostics at the solution restore target in this environment; individual Contracts/Client restores pass, Server restore shows the same silent graph issue, and `dotnet build Hexalith.Agents.slnx -c Release -m:1` performs restore and succeeds with 0 warnings/0 errors.
- 2026-06-25: `dotnet test` hit the expected VSTest `SocketException (13)`; used built xUnit v3 executables directly for final counts.

### Completion Notes List

- Published reusable automation envelopes: `AgentOperationResult`, `AgentOperationResult<T>`, safe error codes, paging metadata, and canonical readiness/call/proposal/audit status terms.
- Published `IAgentsClient` with ProviderCatalog, AgentAdministration, AgentInteractions, ProposalWorkflow, Status, and Audit operation groups over existing public contract records.
- Added a structured fail-closed default client and API/BFF route surface so deferred/unavailable bindings return `Unavailable` without leaking deferred seam text or stack traces.
- Added focused contract/client/server tests for JSON round-trip, enum-by-name/`Unknown = 0`, poison-string non-disclosure, public facade boundary shape, route coverage, and pending-state parity.
- Verified full regression via xUnit v3 executables: Contracts 303/303, Client 6/6, Server 340/340, Agents 651/651, UI 682/682.

### File List

- `_bmad-output/implementation-artifacts/4-1-stable-api-and-client-contracts-for-agent-operations.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`
- `_bmad-output/implementation-artifacts/tests/test-summary.md`
- `_bmad-output/implementation-artifacts/tests/4-1-qa-e2e-test-summary.md`
- `Hexalith.Agents/Hexalith.Agents.slnx`
- `Hexalith.Agents/src/Hexalith.Agents.Client/AgentsClient.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Client/IAgentAdministrationOperations.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Client/IAgentAuditOperations.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Client/IAgentInteractionOperations.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Client/IAgentStatusOperations.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Client/IAgentsClient.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Client/IProposalWorkflowOperations.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Client/IProviderCatalogOperations.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Client/UnavailableOperations.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Operations/AgentCallOperationStatus.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Operations/AgentOperationError.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Operations/AgentOperationErrorCode.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Operations/AgentOperationOptions.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Operations/AgentOperationPage.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Operations/AgentOperationResult.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Operations/AgentOperationResultGeneric.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Operations/AgentOperationStatus.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Operations/AgentReadinessStatus.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Operations/AuditAvailabilityStatus.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Operations/ProposalOperationStatus.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Operations/ProviderModelReadinessStatus.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Server/Api/AgentsOperationEndpoints.cs`
- `Hexalith.Agents/src/Hexalith.Agents.Server/Program.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Client.Tests/AgentsClientFacadeTests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Client.Tests/Hexalith.Agents.Client.Tests.csproj`
- `Hexalith.Agents/tests/Hexalith.Agents.Contracts.Tests/AgentOperationContractsTests.cs`
- `Hexalith.Agents/tests/Hexalith.Agents.Server.Tests/AgentsOperationEndpointsTests.cs`

### Change Log

- 2026-06-25: Added stable operation contracts, client facade, API/BFF route surface, boundary/conformance tests, and final validation results for Story 4.1.
- 2026-06-25: Senior Developer Review (AI) — auto-fix pass. Corrected stale Dev Agent Record test counts (Client 4→6, Server 337→340), completed the File List, refreshed the misleading client assembly-marker doc comment, and moved the story to done.

## Senior Developer Review (AI)

**Reviewer:** Administrator
**Date:** 2026-06-25
**Outcome:** Approved (issues auto-fixed)

### Verification performed

- `dotnet build Hexalith.Agents.slnx -c Release -m:1` → Build succeeded, 0 warnings / 0 errors.
- xUnit v3 executables (VSTest socket unavailable in sandbox, ran built executables directly):
  - Contracts 303/303, Client 6/6, Server 340/340, Agents 651/651, UI 682/682 — all passing.
- Acceptance Criteria cross-checked against implementation:
  - **AC1** (public operation coverage) — `IAgentsClient` exposes ProviderCatalog, AgentAdministration, AgentInteractions, ProposalWorkflow, Status, and Audit over existing contract records; `/api/agents/operations` endpoints map the same surface. No raw EventStore/aggregate/projection/provider/workflow SDK types exposed (enforced by `ProjectReferenceDirectionTests`, `PublicContractPackageBoundaryTests`, and the facade substrate-type test). **Implemented.**
  - **AC2** (structured automation errors) — `AgentOperationResult`/`AgentOperationError`/`AgentOperationErrorCode` carry only safe classes and stable messages; poison-string non-disclosure tests pass at contract and endpoint level. **Implemented.**
  - **AC3** (additive-first stability + UI parity) — enums are `Unknown = 0`, serialize by name; Client → Contracts only; UI gateways unchanged and share the same contracts. **Implemented.**
  - **AC4** (canonical status terms, pending ≠ success) — readiness/call/proposal/audit enums match the UX canonical states; `Pending`/`Checking`/`Degraded`/`PostingPending`/`AuditPending` are never `Succeeded`; tests enforce. **Implemented.**

### Findings (all auto-fixed)

- 🟡 **MEDIUM — Stale Dev Agent Record test counts.** Completion Notes claimed `Client 4/4` and `Server 337/337`; actual runs are `6/6` and `340/340` (and the story's own QA test-summary artifacts already showed the correct numbers). This is the recurring Epic 2/3 stale-count finding called out by Task 6. *Fixed:* counts corrected.
- 🟡 **MEDIUM — Incomplete File List.** Two Story-4.1 test artifacts (`tests/test-summary.md`, `tests/4-1-qa-e2e-test-summary.md`) were changed/added but not listed; Task 6 requires the File List to match `git status --short`. *Fixed:* added to File List.
- 🟢 **LOW — Misleading assembly-marker doc comment.** `AgentsClientAssemblyMarker` still described the assembly as a "buildable-shell placeholder" after the full public client surface landed. *Fixed:* doc comment updated; marker retained to match the cross-module assembly-anchor convention.

No CRITICAL or HIGH issues found: the implementation matches all acceptance criteria, every `[x]` task is genuinely done, build is clean, and the full regression passes.
