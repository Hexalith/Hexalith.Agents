---
title: '5.3 Migrate Provider Governance to Platform Catalog and Tenant Enablement'
type: 'feature'
created: '2026-09-23'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '1d69f3f349b76716fca20ef126ae35ed4a6c4d40'
context:
  - '_bmad-output/implementation-artifacts/epic-5-context.md'
  - '_bmad-output/planning-artifacts/epics.md'
  - 'references/Hexalith.AI.Tools/hexalith-state-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** The shipped tenant-scoped catalog violates AD-2 and leaves `NC-5.3-PLATFORM-CATALOG-SCOPE` open. Tenant enablement and data-handling decisions are absent.

**Approach:** Migrate to one reserved-`system` ProviderCatalog stream per provider/model and tenant-scoped TenantProviderEnablement. Publish joined reads and versioned decisions through EventStore, API/client, and UI.

## Boundaries & Constraints

**Always:** Persist pure aggregate decisions through EventStore; freeze legacy writes without rewriting history. Platform Operators alone mutate catalog/enablement; tenant administrators make lock-bearing decisions on their own current terms. Check revisions, positive limits, explicit nonnegative prices, ISO currency, and non-reusable capability/pricing/data-handling versions. DataHandlingVersion changes only with retention, training use, processing regions, or retained terms reference; pricing version only with price/currency. Missing terms, zero price, missing acceptance, stale or unknown evidence block. Trusted server time starts one exclusive 30-day grace at the first unaccepted cumulatively tightening version; tightening means non-lengthening retention, movement toward opt-out, narrowed regions, and unchanged terms reference. Decline, loosening, incomparable terms, or expiry block. Authorize before disclosure; tenant reads show only enabled entries, without secret reference/configured state; platform reads may show these safe fields. Keep submitted/pending/confirmed truth and English/French parity.

**Never:** Do not choose among divergent legacy entries, delete history, infer callability, expose secrets, invoke a Provider, add its SDK adapter, or claim launch readiness. Story 5.4 owns signed-envelope ingress; 5.5–5.7 own readiness, host, and activation.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Migration | Legacy tenant entries | Deterministic `MigratedFrom` targets; repeat no-op; legacy frozen | Divergent same-key metadata conflicts |
| Governance | Platform mutation or tenant enablement | Expected-revision events/projections; terms required to enable | Wrong principal, stale or invalid input rejects |
| Terms | Named version, justification | Durable accept/decline, actor/role, exact confirmation, current/in-force versions | Superseded, blank, divergent duplicate, cross-tenant rejects |
| Tightening | Successive versions | Cumulative comparison; one exclusive deadline | Decline, loosening, missing diff, expiry blocks |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Agents/ProviderCatalog/ProviderCatalogAggregate.cs` — reuse pure validation; replace tenant-wide state with `system` entry ownership.
- `src/Hexalith.Agents.Server/Application/Agents/ProviderCatalogAdministrationOrchestrator.cs` — replace tenant target/`IsProviderAdmin` with Platform scope and entry ID; reuse dispatcher.
- `src/Hexalith.Agents.Server/Projections/ProviderCatalogReadModelAddresses.cs`, `src/Hexalith.Agents.Server/Application/Queries/ProviderCatalogQueryHandlerBase.cs`, `src/Hexalith.Agents.Server/Ports/ProjectedProviderCatalogReader.cs` — replace tenant catalog slots with authorized platform/tenant join.

## Tasks & Acceptance

**Execution:**
- [x] `src/Hexalith.Agents.Contracts/ProviderCatalog/ProviderDataHandlingRecord.cs`, `TenantProviderCatalogEntryView.cs`, and `Commands/CreateProviderModelEntry.cs` — add terms, tenant-safe view, versions, revisions, provenance, and rejections.
- [x] `src/Hexalith.Agents/ProviderCatalog/ProviderCatalogAggregate.cs` and `src/Hexalith.Agents/TenantProviderEnablement/TenantProviderEnablementAggregate.cs` — implement platform entry and tenant enablement/decision folds, validation, replay, and nonextending grace.
- [x] `src/Hexalith.Agents.Server/Application/Agents/ProviderCatalogMigrationService.cs` and `ProviderCatalogAdministrationOrchestrator.cs` — migrate deterministic targets, freeze legacy writes, enforce authority before dispatch.
- [x] `src/Hexalith.Agents.Server/Projections/TenantProviderEnablementProjectionHandler.cs`, `src/Hexalith.Agents.Server/Application/Queries/ProviderCatalogQueryHandlerBase.cs`, and `src/Hexalith.Agents.Server/Ports/ProjectedProviderCatalogReader.cs` — persist/join read models; hide absent and nonenabled entries alike.
- [x] `src/Hexalith.Agents.Server/Application/Agents/EventStoreProviderCatalogOperations.cs`, `src/Hexalith.Agents.Client/IProviderCatalogOperations.cs`, `src/Hexalith.Agents.UI/Components/Pages/ProviderCatalog.razor`, and `src/Hexalith.Agents.UI/Resources/AgentsResources.fr.resx` — expose scoped writes/reads, acceptance, grace, and truth stages with English/French resources.
- [x] `test/Hexalith.Agents.Tests/ProviderCatalogAggregateTests.cs`, `test/Hexalith.Agents.Server.Tests/ProviderCatalogEventStoreIntegrationTests.cs`, `test/Hexalith.Agents.UI.Tests/ProviderCatalogUiTests.cs`, and `eng/verify-story-5.3.ps1` — prove migration/replay, persisted end state, isolation, grace, UI, and secret sweep; close `NC-5.3-PLATFORM-CATALOG-SCOPE` in `_bmad-output/planning-artifacts/external-dependency-register.md` only after evidence is accepted.

**Acceptance Criteria:**
- Given a Platform Operator, when a valid catalog command commits, then only the `system` entry changes and replay preserves terms, pricing, versions, and enablement.
- Given a Platform Operator, when tenant enablement changes, then only that tenant's EventStore state changes; incomplete terms and tenant-admin mutation reject.
- Given legacy entries, when migration repeats, then targets match, the repeat is a no-op, and divergent entries conflict without rewriting history.
- Given an authorized tenant reader, when projections join, then only enabled entries appear, hidden keys resemble absent keys, and no configuration state leaks.
- Given current terms, when a tenant administrator accepts or declines, then durable evidence records exact rendered fields/justification; stale or cross-tenant decisions preserve state.
- Given successive terms, when eligibility is evaluated at the deadline, then only declared cumulative tightening grants nonextending grace until that instant.

## Implementation Notes

The staged implementation adds the reserved `system` catalog, tenant enablement and terms decisions, a Platform Operator migration endpoint, safe tenant joins, recorded tightening declarations, and command-message-specific UI confirmation. Migration reads the operator-supplied legacy tenant inventory from persisted projections and rejects divergent or mismatched records before dispatch. The supplied inventory cannot itself prove that every legacy tenant was named.

`TMPDIR=/home/administrator/tmp-story-5-3 pwsh ./eng/verify-story-5.3.ps1` passed after review fixes. Both builds had zero warnings. Focused EventStore/domain/server/UI suites passed (5/68/60/16 tests), as did all five full owning test projects (689/6/828/688/1104 tests). The review findings were triaged below and every accepted patch was verified. Both repositories passed `git diff --check`. The broader EventStore Server.Tests run had three failures in unchanged security/query paths; a clean baseline was not run, so their provenance is unconfirmed.

On 2026-09-24 the user chose to extend Hexalith.EventStore for the Terms matrix row. A provider/model keyed coordinator now serializes platform catalog mutations with tenant enablement and terms decisions, reads the authoritative platform events, and rejects superseded terms before tenant mutation. Focused tests cover both command orderings, persisted tenant revision, exact retry, cross-tenant message-ID isolation, and altered rendered terms. The ordering test uses the real router/coordinator/policy with simulated actors; a live Dapr sidecar concurrency test has not run. Keep `NC-5.3-PLATFORM-CATALOG-SCOPE` open until its evidence is accepted, the operator supplies a complete legacy tenant inventory, and the external EventStore gateway host registers `AddAgentsEventStore` (DW-21).

## Spec Change Log

## Review Triage Log

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| B1 | medium — `EntryKey` accepts its own separator in IDs, and the new tenant fold also uses it; distinct pairs can alias. | patch |
| B2 | medium — migration fingerprints `Status`, so tenant-specific disabled state falsely conflicts with an enabled peer. | patch |
| B3 | false — `MigrateAsync` explicitly takes a complete operator inventory, reports only work for those IDs, and NC closure remains pending independent inventory acceptance; it does not attest global completeness. | reject |
| B4 | medium — platform `MigratedFrom` depends on the supplied tenant set, preventing a later omitted tenant from joining an existing exact platform target. | patch |
| B5 | false — the pre-migration legacy contract had no data-handling record or history; no historical terms exist to copy from those streams. | reject |
| B6 | medium — public tenant disable checks for terms before dispatch even though disable needs no current terms. | patch |
| B7 | medium — enablement's value-based no-op precedes `ExpectedRevision`, allowing a distinct stale command to succeed. | patch |
| B8 | medium — public decision precheck ignores effective time and declaration, then records the server snapshot as caller confirmation. | patch |
| B9 | medium — a committed decision retry after a terms update fails the projected-current check before EventStore can reconcile its message ID. | patch |
| B10 | medium — governance HTTP writes omit the available idempotency and correlation headers, preventing callers from controlling retry identity. | patch |
| B11 | medium — an absent projection returns pending truth but the initial tenant page renders Empty because it checks entry count only. | patch |
| B12 | false — the review artifact includes a separate prefixed EventStore submodule diff, so its changes can be reviewed or applied; repository instructions prohibit creating a submodule commit solely for this build. | reject |
| V1 | medium — authorized tenant list/get query handlers have no executed disclosure test, leaving a meaningful regression gap. | patch |
| V2 | medium — the migration test's fixture-based persistence bypasses label, limits, capabilities, configuration, pricing, and version. | patch |
| V3 | medium — the public authorized tenant decision dispatch is not exercised by current tests. | patch |
| E1 | medium — `HasValidPricing` applies a version-one creation rule to a valid migrated or updated version above one, blocking selection. | patch |
| E2 | medium — a non-null confirmed terms record with null regions reaches `SameFields` and throws rather than returning a safe rejection. | patch |
| E3 | medium — same full-snapshot mismatch as B8; the public precheck is weaker than the coordinator. | patch (B8) |
| E4 | medium — same stale enablement no-op as B7. | patch (B7) |
| E5 | medium — a distinct stale decision with the same value returns no-op before revision validation. | patch |
| E6 | low — imported data-handling version `int.MaxValue` can overflow on the next terms change. | patch |
| E7 | low — imported capability version `int.MaxValue` can overflow on update. | patch |
| E8 | false — `ProjectedProviderCatalogReader` is an internal server selection/activation port; its callers use only readiness and capability version and never serialize the returned platform view to a tenant. Public tenant queries use `TenantProviderCatalogEntryView`. | reject |
| A1 | low — final audit found the same `int.MaxValue` overflow in migrated pricing on a changed price; the guard and focused aggregate test now reject it before arithmetic. | patch |

## Design Notes

PRD FR-33 and UX restrict configured state/reference to Platform Operators despite broad Story 5.3 query wording. Use distinct views. PRD assumption A-10 remains a launch gate.

## Verification

**Commands:**
- `pwsh ./eng/verify-story-5.3.ps1` — warning-free owning build and focused/full test projects, including persisted end state and negative cases.
