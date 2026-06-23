# Test Automation Summary — Story 1.2: Govern Provider Catalog Entries

**Workflow:** bmad-qa-generate-e2e-tests
**Date:** 2026-06-24
**Feature under test:** `ProviderCatalog` aggregate, contracts, and inspection read path (Story 1.2)
**Framework:** xUnit v3 + Shouldly (project convention — no UI exists, so "E2E" = full
`command → ProcessAsync reflection/JSON dispatch → events → Apply → state → inspection` lifecycle flows)

## Context

This story is a pure EventStore domain feature with **no UI surface**, so there are no API endpoints or browser
flows. The end-to-end boundary for this feature is the aggregate pipeline: a JSON command envelope dispatched by
reflection through `ProviderCatalogAggregate.ProcessAsync`, producing events that are applied to replay state and
then read back through the authorized `ProviderCatalogInspection` path. The dev-story already shipped 47 focused
unit tests; this QA pass adds true multi-command lifecycle ("E2E") flows and fills discovered coverage gaps.

## Generated Tests

### E2E / Lifecycle Tests (drive the real pipeline + thread evolving state)

`tests/Hexalith.Agents.Tests/ProviderCatalogLifecycleE2ETests.cs` (new — 6 flows)

- [x] AC1 — authorized create → inspect surfaces the full AD-10 capability floor with a safe configuration
      reference/state only (no secret value).
- [x] AC2 — create → disable → re-enable: disabled entry is hidden from the default list, still inspectable as
      history, flagged not-selectable, and selectable again after re-enable.
- [x] AC3 — unauthorized create fails closed *before mutation*; a later authorized read sees an empty catalog.
- [x] AC3 — unauthorized inspection fails closed (no entry data) even after an authorized create exists.
- [x] AC4 — duplicate create delivery is an idempotent no-op; a conflicting payload is rejected and never
      silently mutates state (verified via inspection).
- [x] AC4 — replaying the emitted event stream into a fresh state rehydrates **byte-for-byte identical**
      inspection views (deterministic rehydration), disabled history included.

### Aggregate Validation Gap Tests (previously-untested fail-before-mutation branches)

`tests/Hexalith.Agents.Tests/ProviderCatalogMetadataValidationTests.cs` (new — 12 cases)

- [x] Create with blank provider/model id → `InvalidProviderModelMetadataRejection` (4 cases).
- [x] Create with over-length provider/model id → invalid metadata (2 cases).
- [x] Create with blank / over-length display label → invalid metadata (3 cases).
- [x] Create with display label exactly at the max length → accepted (boundary).
- [x] Update with token limits violating the context-window invariant → invalid metadata, original state intact.
- [x] Update with an unsafe configuration reference → unsafe rejection that never echoes the offending value.

### Contract Serialization Gap Tests

`tests/Hexalith.Agents.Contracts.Tests/ProviderCatalogContractsRoundTripTests.cs` (new — 6 cases)

- [x] `ProviderModelEntryMetadataUpdated` / `ProviderModelEntryEnabled` / `ProviderModelEntryDisabled` round-trip
      through System.Text.Json (dev-story only round-tripped `Created`).
- [x] `ProviderModelEntryLifecycleStateAlreadySetRejection` round-trips with its enum status fields intact.
- [x] `ProviderConfigurationState` serializes by name; default/zero value is `Unknown` so an absent value never
      deserializes to `Configured` (AD-14 fail-safe).

### Shared Fixture Additions

`tests/Hexalith.Agents.Tests/ProviderCatalogTestData.cs` (modified — additive helpers only)

- `ApplyAll(state, result)` — applies a `DomainResult`'s events through the production typed `Apply` handlers.
- `ProcessAndApplyAsync(...)` — drives one command end-to-end through `ProcessAsync` and threads evolved state.

## Coverage

| Acceptance Criterion | E2E lifecycle | Unit (dev-story + gaps) |
| --- | --- | --- |
| AC1 — governed state change + secret safety | ✅ | ✅ |
| AC2 — disable blocks selection, preserves history | ✅ | ✅ |
| AC3 — authorization fails closed | ✅ | ✅ |
| AC4 — replay / idempotency / structured failures | ✅ | ✅ |

- Aggregate command handlers: create / update / enable / disable — all happy paths + every rejection branch
  (denied, already-exists, not-found, lifecycle-already-set, invalid-metadata, unsafe-config) covered.
- Validation branches: identifier, display-label, token-limit, timeout, and configuration-reference rules —
  now fully covered on both the create and update paths.
- Read path: authorized/unauthorized single + list inspection, disabled-history visibility, empty catalog.
- Contracts: all four success events + an enum-bearing rejection round-trip; enums serialize safely by name.

## Results

| Project | Tests | Result |
| --- | --- | --- |
| `Hexalith.Agents.Tests` (domain) | 59 (+18) | ✅ Passed |
| `Hexalith.Agents.Contracts.Tests` | 15 (+6) | ✅ Passed |
| `Hexalith.Agents.Server.Tests` | 12 | ✅ Passed |
| **Total** | **86 (+24)** | **✅ 0 failures** |

Commands run (Release, per-project per project convention):

```
dotnet build Hexalith.Agents.slnx --configuration Release                  # 0 warnings, 0 errors
dotnet test tests/Hexalith.Agents.Tests --configuration Release            # 59 passed
dotnet test tests/Hexalith.Agents.Contracts.Tests --configuration Release  # 15 passed
dotnet test tests/Hexalith.Agents.Server.Tests --configuration Release     # 12 passed
```

## Next Steps

- Run these in CI alongside the existing suites (no Docker/Aspire dependency — all pure unit/in-process).
- When the dedicated read-model story binds the inspection logic to the DAPR `IDomainQueryHandler` /
  `IReadModelStore` path, add Tier-2/3 integration tests that assert persisted read-model end-state (per the
  EventStore integration-test rule) — the pure inspection flows here become the oracle for that binding.
- Re-run when provider runtime-health states (`Degraded` / `Failed`) are introduced in later stories.
