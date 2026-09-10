---
title: '5.3 Govern Provider Models And Pricing Through Live Operations'
type: 'feature'
created: '2026-09-08'
status: 'done'
baseline_commit: 'a4003a9bbe9c2939ebafb35b19a3df3b0ed9403a'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-5-context.md'
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-state-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Provider catalog create, update, enable, disable, and inspection still hit deferred or unavailable seams, and catalog records have no versioned pricing. Administrators cannot persist safe capability and pricing truth through EventStore or see the same current projection on the API, client, and Provider catalog UI.

**Approach:** Bind the existing `ProviderCatalog` aggregate and public catalog surface to a live DomainService/EventStore command-query-projection path; add administrator-supplied versioned pricing units and currency; publish one authoritative catalog read model with CapabilityVersion, projection version, and freshness; and prove replay, eligibility, authorization, and secret non-disclosure with persisted end-state evidence. Do not invoke a Provider or claim adapter availability.

## Boundaries & Constraints

**Always:** Treat `_bmad-output/planning-artifacts/epics.md` Story 5.3 as executable authority. Authorize provider-catalog administration (`actor:agentsProviderAdmin`, trusted-extension style from Story 5.2) before mutation or lookup-dependent disclosure. Keep aggregate handlers pure; impure work stays in application adapters. Persist only through Hexalith.EventStore (`IReadModelStore` + freshness). Reuse `EventStoreAgentCommandDispatcher`, the Agent-setup projection/query/truth-flow, and existing `IProviderCatalogOperations` / `MapProviderCatalog` routes. CapabilityVersion stays monotonic and non-reusable (1 on create, +1 on metadata or pricing updates, unchanged by enable/disable). Disabled, unconfigured, unpriced, invalid-limit, regressed, or unknown entries are not eligible for a new active selection. Public types expose secret reference and configured/not-configured state only. UI uses FrontComposer and Fluent UI V5; writes follow submitted → authoritative-pending → projection-confirmed; Success is never inferred callability.

**Ask First:** If a newly committed EXT-PROVIDER-1 artifact requires reshaping catalog contracts or inventing a generation or prepared-attempt adapter, stop and ask. If pricing must be a live Provider price feed, FX conversion, or SDK price object rather than administrator-supplied versioned units and currency, stop and ask. If catalog-admin HTTP identity must be a new role distinct from the existing `Agents.Administrator` policy already on `/agents/providers`, stop and ask.

**Never:** Do not implement `IAgentGenerationProvider`, prepared-attempt execution, or Provider SDK types. Do not mark EXT-PROVIDER-1 Committed or Available, and do not claim production enablement. Do not bind live Agent provider-selection writes, Party or approver readiness, launch-readiness, host topology, or activation (Stories 5.4–5.7). Do not treat host configuration, lifecycle active, or optimistic UI as catalog truth or callability. Do not disclose another tenant's existence, catalog rows, counts, diagnostics, secret values, or configuration references in browser or accessible output. Do not leave Deferred or Unavailable as the production catalog path when EventStore is configured.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Authorized catalog command | Valid create, update, enable, or disable with capability, versioned pricing, and secret reference | Persist ProviderCatalog events; replay reconstructs identical state; return structured accepted identity (provider/model, message/correlation, Submitted) | Typed validation or conflict; no partial mutation or infrastructure leak |
| Catalog query and UI | Accepted events projected | Same safe capability, pricing, enablement, configured-state, CapabilityVersion, projection version, and freshness; writes show submitted / authoritative-pending / projection-confirmed; ineligible rows are not selectable | Never infer callability or Provider readiness triples |
| Duplicate, stale, invalid, or regressed | Duplicate command, stale revision, invalid or missing pricing or limits, reused or decreased CapabilityVersion, or replayed projection | Exact duplicates are idempotent; others fail closed with a typed outcome; persisted read-model end state stays deterministic | Do not overwrite prior state or accept optimistic success |
| Unauthorized, cross-tenant, or secret | Missing catalog-admin authority, other tenant, or poison secret value | Deny before mutation, disclosure, or secret resolution; selection via `IProviderCatalogReader` is Unauthorized or ineligible | No target existence, rows, counts, diagnostics, secret values, or SDK types in any captured artifact |

</frozen-after-approval>

## Code Map

- `_bmad-output/planning-artifacts/epics.md:1262-1309` -- authoritative Story 5.3 ACs and evidence manifest. EXT-PROVIDER-1 Uncommitted does not authorize inventing an adapter.
- `src/Hexalith.Agents/ProviderCatalog/{ProviderCatalogAggregate,ProviderCatalogState,ProviderModelEntryState,ProviderCatalogInspection}.cs` -- pure handlers and replay; `[EventStoreDomain("provider-catalog")]`; `actor:agentsProviderAdmin`; CapabilityVersion already 1 then +1; add pricing; tighten `IsSelectableForNewActiveUse` beyond enabled-only (`ProviderCatalogInspection.cs:97`).
- `src/Hexalith.Agents.Contracts/ProviderCatalog/Commands/{Create,Update,Enable,Disable}ProviderModelEntry.cs` -- public writes exist; Create/Update lack pricing (`CreateProviderModelEntry.cs:18-28`).
- `src/Hexalith.Agents.Contracts/ProviderCatalog/Events/` + `ProviderCatalogEntryView.cs:29-42` -- events and view lack pricing, projection version, and freshness; keep `ConfigurationReferenceId` as the only secret-adjacent field.
- `src/Hexalith.Agents.Contracts/ProviderCatalog/Queries/{ListProviderCatalogEntriesQuery,GetProviderCatalogEntryQuery}.cs` -- contracts exist; no handlers.
- `src/Hexalith.Agents.Server/Application/Agents/ProviderSelectionVerdict.cs:36-55` -- add unpriced, regressed, and unknown eligibility; keep fail-closed precedence.
- `src/Hexalith.Agents.Server/Ports/{IProviderCatalogReader,DeferredProviderCatalogReader}.cs` + `Program.cs:59` -- replace the deferred throw with a live projected reader when EventStore is configured.
- `src/Hexalith.Agents.Server/Application/Agents/AgentProviderSelectionOrchestrator.cs` -- consume the live reader and prove cross-tenant selection is denied; do not add a public `SelectProviderModel` write.
- Reuse: `EventStoreAgentCommandDispatcher.cs`; `AgentAdministrationOrchestrator.cs` (trusted-extension template); `AgentSetupProjectionHandler.cs` / `Fold` / `ViewFactory` / `QueryHandlerBase`; `EventStoreAgentAdministrationOperations.cs`; `AgentSetupServiceCollectionExtensions.cs:46-66`; `AgentsOperationEndpoints.MapProviderCatalog:34-49`; `AgentCommandAcceptance.cs` as the catalog-acceptance analogue.
- `src/Hexalith.Agents.Client/{IProviderCatalogOperations,UnavailableOperations,AgentsClient}.cs` -- `WithAdministration` still leaves catalog Unavailable (`AgentsClient.cs:69-76`); add live operations and `WithProviderCatalog`.
- `src/Hexalith.Agents.UI/Components/Pages/ProviderCatalog.razor` + `IProviderCatalogGateway` -- read-only grid today; add writes and truth flow; never render `ConfigurationReferenceId` or secrets; FrontComposer and Fluent UI V5 only.
- Read-only reuse: `references/Hexalith.Parties/.../PartyDetailSdkProjectionHandler.cs`; `references/Hexalith.EventStore/.../IReadModelStore.cs`; `IReadModelFreshness.cs`.
- Tests to extend or add: existing `ProviderCatalog*Tests`; clone `EventStoreAgentAdministrationOperationsTests`, `AgentSetupProjectionTests`, `AgentSetupQueryHandlerTests`, `AgentSetupCompositionTests`; add EventStore integration, query, authorization, version-regression, and secret-leak suites; `eng/verify-story-5.3.ps1` cloned from `eng/verify-story-5.2.ps1`.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Agents/` + `src/Hexalith.Agents.Contracts/ProviderCatalog/` -- add versioned pricing units and currency to commands, events, state, and views; reject invalid or missing pricing and CapabilityVersion regression; eligibility includes configured, text-generation, limits, and pricing; add projection version and freshness on catalog reads.
- [x] `src/Hexalith.Agents.Server/` -- add catalog orchestrator, projection handler/fold/view factory/read model, query handlers, live `IProviderCatalogReader`, live operations, and composition gated on `Agents:EventStore:BaseUrl`; reuse the existing dispatcher; keep generation and selection-write deferred.
- [x] `src/Hexalith.Agents.Client/` + `AgentsOperationEndpoints` -- bind live `IProviderCatalogOperations`; return structured accepted identities; keep EventStore and SDK types off the public surface.
- [x] `src/Hexalith.Agents.UI/` -- bind a live gateway; support create, update, enable, and disable with submitted / pending / confirmed; render a safe grid (capability, pricing, enablement, configured-state, CapabilityVersion, freshness); keep EN/FR parity; keep secrets and configuration references out of markup and accessible names.
- [x] `test/` + `eng/verify-story-5.3.ps1` -- add aggregate/replay, live EventStore command-query-projection, duplicate/conflict/regression, cross-tenant auth and no-disclosure including selection-denied, UI truth-flow, and poison-secret sweep tests; assert persisted read-model end state; prove live catalog seams in DI composition.

**Acceptance Criteria:**
- Given an authorized catalog administrator and valid capability and pricing metadata, when create, update, enable, or disable is accepted, then EventStore persists the catalog events with CapabilityVersion and a secret reference only, replay matches, and the response is a structured accepted identity.
- Given accepted catalog events, when the API, client, and `/agents/providers` load, then they expose the same safe projection (capability, pricing, enablement, configured-state, versions, freshness) and ineligible entries cannot be newly selected.
- Given duplicate, stale, invalid-pricing, invalid-limit, or regressed-version input, when focused tests run, then duplicates are idempotent, failures are typed, and the persisted read model is unchanged except for exact duplicates.
- Given unauthorized, cross-tenant, or poison-secret input, when commands, queries, selection reads, logs, or UI markup are inspected, then the operation fails before mutation or secret resolution and no secret value or other-tenant existence is present.

## Spec Change Log

- 2026-09-10: Product ruled that Story 5.3 executed no `EXT-PROVIDER-1` seam. Substituted or deferred generation-provider ports did not constitute execution of a live external Provider adapter; Branch B is recorded in the external dependency register without changing the dependency's `Uncommitted` status.

## Design Notes

Sprint-status slug `5-3-bind-eventstore-operations-and-setup-read-models` is archived Agent-setup numbering; numeric Story 5.3 in `epics.md` is authority. EXT-PROVIDER-1 stays Uncommitted: this story publishes catalog truth only and must not invent or invoke the generation adapter. Pricing is administrator-supplied catalog truth, not a Provider SDK quote. Mirror Agent-setup freshness (`IReadModelFreshness`) rather than inventing catalog-only metadata.

## Verification

**Commands:**
- `pwsh ./eng/verify-story-5.3.ps1` -- expected: exit 0; focused catalog suites plus composition DI prove the live seams.

**Manual checks (if no CLI):**
- After an authorized catalog create, API, client, and `/agents/providers` show identical projection-confirmed safe state; the secret reference is absent from the grid; Success is not shown as callable.

## Suggested Review Order

**Live command path**

- EventStore BaseUrl swaps deferred catalog ops and the projected reader.
  [`AgentSetupServiceCollectionExtensions.cs:66`](../../src/Hexalith.Agents.Server/Composition/AgentSetupServiceCollectionExtensions.cs#L66)

- Trusted `actor:agentsProviderAdmin` is stripped then repopulated before dispatch.
  [`ProviderCatalogAdministrationOrchestrator.cs:24`](../../src/Hexalith.Agents.Server/Application/Agents/ProviderCatalogAdministrationOrchestrator.cs#L24)

- Public writes return provider/model identity at Submitted only.
  [`EventStoreProviderCatalogOperations.cs:180`](../../src/Hexalith.Agents.Server/Application/Agents/EventStoreProviderCatalogOperations.cs#L180)

**Pricing and eligibility**

- Administrator-supplied versioned pricing is the catalog truth type.
  [`ProviderModelPricing.cs:14`](../../src/Hexalith.Agents.Contracts/ProviderCatalog/ProviderModelPricing.cs#L14)

- Metadata and pricing updates bump CapabilityVersion; enable/disable do not.
  [`ProviderCatalogAggregate.cs:187`](../../src/Hexalith.Agents/ProviderCatalog/ProviderCatalogAggregate.cs#L187)

- Selectable means enabled, configured, text-gen, limits, and valid pricing.
  [`ProviderCatalogInspection.cs:85`](../../src/Hexalith.Agents/ProviderCatalog/ProviderCatalogInspection.cs#L85)

- Selection verdicts add Unpriced/Regressed with the same ISO-3 currency rule.
  [`ProviderSelectionVerdict.cs:59`](../../src/Hexalith.Agents.Server/Application/Agents/ProviderSelectionVerdict.cs#L59)

**Projection and queries**

- Checkpointed catalog projection owns the durable read-model slot.
  [`ProviderCatalogProjectionHandler.cs:18`](../../src/Hexalith.Agents.Server/Projections/ProviderCatalogProjectionHandler.cs#L18)

- Empty or corrupt payloads do not advance the fold checkpoint.
  [`ProviderCatalogProjectionFold.cs:90`](../../src/Hexalith.Agents.Server/Projections/ProviderCatalogProjectionFold.cs#L90)

- Create catch-up treats a missing entry plus expected version as pending.
  [`ProviderCatalogViewFactory.cs:88`](../../src/Hexalith.Agents.Server/Projections/ProviderCatalogViewFactory.cs#L88)

- Query auth runs before the store; store faults return structured Unavailable.
  [`ProviderCatalogQueryHandlerBase.cs:58`](../../src/Hexalith.Agents.Server/Application/Queries/ProviderCatalogQueryHandlerBase.cs#L58)

**Authorization**

- Unauthorized or cross-tenant selection never addresses the other tenant store.
  [`ProjectedProviderCatalogReader.cs:45`](../../src/Hexalith.Agents.Server/Ports/ProjectedProviderCatalogReader.cs#L45)

**Public client and UI**

- Client composition can bind live catalog operations independently of setup.
  [`AgentsClient.cs:85`](../../src/Hexalith.Agents.Client/AgentsClient.cs#L85)

- Catalog writes poll submitted → pending → confirmed; lifecycle waits on Status.
  [`ProviderCatalog.razor:473`](../../src/Hexalith.Agents.UI/Components/Pages/ProviderCatalog.razor#L473)

- Configuration reference stays write-only and is never rendered on the grid.
  [`ProviderCatalog.razor:108`](../../src/Hexalith.Agents.UI/Components/Pages/ProviderCatalog.razor#L108)

**Verification**

- Story verifier gates focused suites, owning projects, and live-seam anchors.
  [`verify-story-5.3.ps1:33`](../../eng/verify-story-5.3.ps1#L33)

## Dev Agent Record

<!-- dev-agent-test-evidence:start -->
### Latest Release Test Evidence

Run (UTC): 2026-09-08T15:32:15Z

| Test project | Total | Passed | Failed | Skipped | Pending | Other |
|---|---:|---:|---:|---:|---:|---:|
| Hexalith.Agents.Client.Tests | 6 | 6 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.Contracts.Tests | 327 | 327 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.Server.Tests | 458 | 458 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.Tests | 739 | 739 | 0 | 0 | 0 | 0 |
| Hexalith.Agents.UI.Tests | 1039 | 1039 | 0 | 0 | 0 | 0 |
| **Total** | 2569 | 2569 | 0 | 0 | 0 | 0 |

Result: PASS
<!-- dev-agent-test-evidence:end -->

### File List

- `_bmad-output/implementation-artifacts/spec-5-3-fix-review-readiness-gate.md`
- `_bmad-output/implementation-artifacts/spec-5-3-govern-provider-models-and-pricing-through-live-operations.md`
