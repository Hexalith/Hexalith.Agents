# Reconcile — Story 5.3 (Provider Model And Pricing Governance) vs PRD

Extracted 2026-09-08 from commit `a191d24` ("govern provider models and pricing through live operations"), the code as it stands on disk, `spec-5-3-govern-provider-models-and-pricing-through-live-operations.md` (status `done`), `spec-5-3-fix-review-readiness-gate.md`, and `review-implementation-drift.md` (claims re-verified against code, corrections noted).

## Numbering confirmed

- Highest existing functional requirement: **FR-28** (`prd.md:431`, "Define Launch Readiness Controls"). No FR-29+ anywhere in `prd.md` or `addendum.md`; `addendum.md` defines no FR/NFR ids at all.
- Highest existing non-functional requirement: **NFR-14** (`prd.md:505`, "UI Interaction Performance").
- Next free ids: **FR-29**, **NFR-15**.

## Shipped vs planned

Shipped in `a191d24` and present on disk: versioned administrator-supplied pricing on catalog commands/events/state/views; tightened selection eligibility; CapabilityVersion concurrency and rejection types; live EventStore command → projection → query path; projection version/freshness/truth-state on catalog reads; live client operations; a write-capable admin UI with EN/FR parity.

Planned but **not** shipped (do not reflect into the PRD as delivered): any Provider SDK/price feed, FX conversion, cost reservation or budget consumption of the pricing units, `EXT-PROVIDER-1` commitment, live Agent provider-selection *writes*, launch-readiness gate aggregate.

## Drift table

| # | Capability | What the code does now (evidence) | What the PRD says now (quoted) | Delta | Class |
|---|---|---|---|---|---|
| 1 | Versioned pricing is mandatory catalog truth | `ProviderModelPricing(Currency, InputTokenUnitPrice, OutputTokenUnitPrice, PricingVersion)` is a non-optional parameter of both write commands and is carried on state, events, and the public view — `src/Hexalith.Agents.Contracts/ProviderCatalog/ProviderModelPricing.cs:14-18`, `.../Commands/CreateProviderModelEntry.cs:19-30`, `.../Commands/UpdateProviderModelEntry.cs:22-33`, `.../ProviderCatalogEntryView.cs:29-43` | FR-4 (`prd.md:146`): "configure the Global Providers Aggregate with provider records, model options, enabled/disabled state, and provider capability metadata needed for Agent selection." Its three consequences cover disabled selection, disabled-Agent activation, and secret non-exposure only. Glossary (`prd.md:91`) mentions "versioned pricing metadata"; OQ-7 (`prd.md:608`) records it as an architecture decision. | Pricing is a required, first-class part of catalog administration in code; FR-4 never requires it and no consequence is testable against it. | **PRD-SILENT** |
| 2 | Pricing validity rules and version assignment | Rejects blank/missing pricing, non-ISO-4217-shaped currency (3 ASCII letters), negative unit prices, negative version, version ≠ 1 on create, and any decrease/reuse — `src/Hexalith.Agents/ProviderCatalog/ProviderCatalogAggregate.cs:470-501`; the aggregate normalises currency to upper case and assigns version 1 on create, +1 only when units or currency change — `:505-518`; failures surface as `InvalidProviderModelPricingRejection` (`src/Hexalith.Agents.Contracts/ProviderCatalog/Events/Rejections/InvalidProviderModelPricingRejection.cs`) | Nothing. No FR, NFR, or Decision Register row states pricing must be well-formed, non-negative, currency-denominated, or monotonically versioned. | Fail-closed pricing validation with a typed rejection shipped with no requirement home. | **PRD-SILENT** |
| 3 | Eligibility for a new active selection is a named six-part set | `IsSelectableForNewActiveUse` = enabled ∧ text-generation ∧ Configured ∧ valid limits (context > 0, output > 0, output ≤ context, timeout > 0, retries ≥ 0) ∧ valid pricing ∧ CapabilityVersion ≥ 1 ∧ PricingVersion ≥ 1 — `src/Hexalith.Agents/ProviderCatalog/ProviderCatalogInspection.cs:85-95, 120-124`; mirrored with fail-closed precedence in `src/Hexalith.Agents.Server/Application/Agents/ProviderSelectionVerdict.cs:23-81`; the view publishes the boolean (`ProviderCatalogEntryView.cs:41`) | FR-4 consequence (`prd.md:149`): "Disabled providers or models cannot be selected for new Agent configuration." FR-5 consequence (`prd.md:158`): "The system validates that the selected Provider and model are enabled and usable before Agent activation." | The PRD's selection bar is "enabled"/"usable" (undefined); the code's bar is a fixed, testable six-part set and blocks activation for unpriced, unconfigured, invalid-limit, non-text-gen and regressed entries. Not contradicted — materially understated, and "usable" is not testable. | **PRD-SILENT** |
| 4 | Monotonic CapabilityVersion + optimistic concurrency | Create sets `CapabilityVersion: 1` (`ProviderCatalogAggregate.cs:132`); update sets `existing.CapabilityVersion + 1` (`:195, :210`); enable/disable emit `ProviderModelEntryEnabled`/`Disabled` and never touch the version (`:219-280`). An `ExpectedCapabilityVersion` lower than current → `ProviderModelCapabilityVersionRegressedRejection`; higher → `ProviderModelEntryStaleRevisionRejection` (`:385-421`). Selection treats version < 1 as `Regressed` (`ProviderSelectionVerdict.cs:64-66`); new statuses `Unpriced`, `Regressed` (`src/Hexalith.Agents.Contracts/Agent/ProviderSelectionValidationStatus.cs:43-47`) | Glossary (`prd.md:91`) and OQ-7 (`prd.md:608`) name `CapabilityVersion` as a catalog field. No FR states it is monotonic, non-reusable, unchanged by lifecycle transitions, or usable as a concurrency token. | A concurrency/versioning contract on a public command shipped with no capability-level requirement. Correction to `review-implementation-drift.md`: it says "any decrease is a rejection" — precisely, the *stored* version never decreases; the rejection is raised when the caller's `ExpectedCapabilityVersion` is below (Regressed) or above (Stale) the stored value, and `ExpectedCapabilityVersion` is optional (`null` skips the check, `UpdateProviderModelEntry.cs:33`). | **PRD-SILENT** |
| 5 | Catalog pricing metadata is in scope | Shipped as above; the spec states "Pricing is administrator-supplied catalog truth, not a Provider SDK quote". | §6.2 Out of scope (`prd.md:488`): "Fine-grained launch pricing, billing, or monetization." | Read literally, the exclusion covers what Story 5.3 built. A downstream reader cannot tell that catalog *unit-price metadata* is in scope while billing/monetization is not. | **PRD-STALE** |
| 6 | Public catalog surface carries pricing, versions, freshness, and structured acceptance | `IProviderCatalogOperations` exposes list/get with `expectedProjectionVersion` / `expectedCapabilityVersion` and create/update/enable/disable returning `ProviderCatalogCommandAcceptance` — `src/Hexalith.Agents.Client/IProviderCatalogOperations.cs:16-60`; results carry `ProjectionVersion`, `ProjectedAt`, `Freshness`, `TruthState` — `src/Hexalith.Agents.Contracts/ProviderCatalog/ProviderCatalogInspectionResult.cs:22-28`; write results are `Submitted` or a typed failure — `ProviderCatalogWriteResult.cs:11-29` | §10 (`prd.md:549`): "Provider administration: create, update, list, enable, or disable Provider and model options where authorized." | The bullet omits pricing administration and the read-consistency/acceptance fields that are now part of the stable public surface protected by FR-23's no-removal rule. | **PRD-SILENT** |
| 7 | Submitted → AuthoritativePending → ProjectionConfirmed write truth | View factory grades every read as `Current`/`Stale` and `AuthoritativePending`/`ProjectionConfirmed` against an expected version — `src/Hexalith.Agents.Server/Projections/ProviderCatalogViewFactory.cs:76-131`; the UI polls after each write through those three stages and never renders success as callability — `src/Hexalith.Agents.UI/Components/Pages/ProviderCatalog.razor:461-575`; strings exist EN/FR (`AgentsResources.resx`, `AgentsResources.fr.resx`, `Agents.ProviderCatalog.Truth.Stage.*`, `…Freshness.*`) | NFR-14 (`prd.md:505`) sets latency for "an authoritative pending acknowledgement" and "a projection-visible terminal change", and FR-22 (`prd.md:361`) requires the UI to "clearly distinguish active, disabled, invalid, pending proposal, failed call, and expired proposal states". | NFR-14 assumes the staged truth model exists but no FR requires it, and FR-22's state list has no entry for "accepted but not yet projected". The shipped rule — an accepted write is never presented as confirmed configuration until the projection catches up — is a testable capability with no requirement home. | **PRD-SILENT** |
| 8 | Fail-closed catalog reads and cross-tenant denial | Unauthorized → `Unauthorized`, missing → `Missing`, any non-Success/unknown status → `Unavailable`, and none resolve to `Valid` — `ProviderSelectionVerdict.cs:23-35`; unauthorized inspection returns no rows — `ProviderCatalogInspection.cs:33-36, 59-62`; the projected reader never addresses another tenant's store — `src/Hexalith.Agents.Server/Ports/ProjectedProviderCatalogReader.cs:45` | FR-21 (`prd.md:337-342`) "Fail Closed On Dependency Uncertainty"; FR-20 role/policy authorization. | Code is a faithful, narrower instance of the stated rule. | **ALIGNED** |
| 9 | Secrets never disclosed | Only `ConfigurationReferenceId` + a `Configured`/`NotConfigured` state are public (`ProviderCatalogAggregate.cs:284-292`, `ProviderCatalogEntryView.cs:39-40`); the reference is write-only in the UI and absent from the grid (`ProviderCatalog.razor:108`); `test/Hexalith.Agents.Server.Tests/ProviderSecretLeakTests.cs` sweeps for poison values. | FR-4 consequence (`prd.md:151`): "Provider configuration changes are auditable without exposing secrets in logs, API responses, UI, or Audit Evidence." FR-22 (`prd.md:360`). | No delta. | **ALIGNED** |
| 10 | Catalog changes are auditable and replayable | All mutations persist through EventStore with replay-identical state — `src/Hexalith.Agents.Server/Application/Agents/EventStoreProviderCatalogOperations.cs`, `test/Hexalith.Agents.Tests/ProviderCatalog/ProviderCatalogStateReplayTests.cs`, `test/Hexalith.Agents.Server.Tests/ProviderCatalogEventStoreIntegrationTests.cs`. | FR-4 consequence (`prd.md:151`) and §9 governance. | No delta (the Evidence-Level claim for these tests is a separate, already-filed finding and is out of scope here). | **ALIGNED** |
| 11 | Admin UI writes with authorization and localization parity | Create/update/enable/disable with FrontComposer + Fluent UI, EN/FR key parity, accessible names free of secrets — `ProviderCatalog.razor`, `test/Hexalith.Agents.UI.Tests/ProviderCatalogUiTests.cs`, `LocalizationResourceTests.cs`. | FR-22 (`prd.md:356-362`), NFR-13 (`prd.md:504`). | No delta beyond row 7. | **ALIGNED** |
| 12 | Pricing is not yet consumed by cost control | No reservation, budget ledger, or estimator reads `ProviderModelPricing`; the only cost-control code remains the Epic 4 `CostControlPosture` on the Agent aggregate. | NFR-10 (`prd.md:501`) and FR-28/OQ-6 (`prd.md:438, 607`) require hard caps with atomic reservation and state "Missing pricing or budget state blocks invocation". | Unbuilt, not contradicted: the PRD requires this at launch readiness, and Story 5.3 deliberately stopped at catalog truth. No PRD edit. Do **not** describe pricing as feeding cost caps. | **ALIGNED** |

Counts: **PRD-SILENT 6** (rows 1, 2, 3, 4, 6, 7) · **PRD-STALE 1** (row 5) · **ALIGNED 5** (rows 8–12).

## Proposed PRD edits

House style observed: `#### FR-N: Verb Phrase` heading, one-sentence capability statement naming the actor, then a `**Consequences (testable):**` bullet list. No technology, type, or file names. Terms use the glossary spelling ("Global Providers Aggregate", "Provider", "Agent Administrators").

### Edit A — amend FR-4 (rows 1, 2, 4)

Replace the FR-4 statement and extend its consequences.

> #### FR-4: Manage Global Providers Aggregate
>
> Authorized administrators can configure the Global Providers Aggregate with provider records, model options, enabled/disabled state, provider capability metadata, and administrator-supplied versioned pricing needed for Agent selection.
>
> **Consequences (testable):**
> - Disabled providers or models cannot be selected for new Agent configuration.
> - Existing Agents using a disabled provider/model cannot be activated or called until reconfigured. A documented migration state may allow temporary read-only inspection.
> - Provider configuration changes are auditable without exposing secrets in logs, API responses, UI, or Audit Evidence.
> - Every catalog entry carries pricing expressed as a currency and non-negative input and output token unit prices; a create or update that omits pricing, uses a currency outside the recognized three-letter code shape, or supplies a negative unit price is rejected without partial mutation.
> - Capability metadata is rejected unless the context limit, output limit, and request timeout are positive and the output limit does not exceed the context limit.
> - Each entry carries a capability version that starts at its first accepted record, increases on every accepted capability or pricing change, is unchanged by enable and disable, and is never reused or lowered. A write that names a capability version other than the entry's current one is rejected as a stale or regressed revision rather than applied.
> - The pricing version increases only when the priced units or currency change, so an unchanged re-submission does not create a new priced revision.

### Edit B — amend FR-5 (row 3)

Replace FR-5's first consequence.

> - The system validates before Agent activation that the selected Provider and model are enabled, have a recorded configuration reference, support text generation, carry valid capability limits, carry valid pricing, and are at a non-regressed capability version. An entry failing any of these is not selectable for new active use and cannot activate an Agent, and the reason is reported as a distinct, non-generic outcome.

### Edit C — new FR-29 (rows 6, 7)

Add to §4.8 "Admin Surface And Public Contracts", after FR-23.

> #### FR-29: Distinguish Accepted Configuration From Confirmed Configuration
>
> Administration surfaces and public contracts report where an accepted configuration change stands: submitted, accepted but not yet visible in the authoritative read view, or confirmed by it.
>
> **Consequences (testable):**
> - An accepted write returns a structured acknowledgement identifying the changed record and its truth stage; acknowledgement alone is never presented as confirmed configuration or as Provider callability.
> - Authorized reads report whether the returned state is current or stale, and expose a stable revision marker a caller can wait on to observe its own accepted change.
> - A caller may ask for a specific expected revision; until that revision is visible, the result is reported as accepted-and-pending rather than confirmed.
> - Admin UI distinguishes submitted, accepted-and-pending, confirmed, and failed outcomes for every high-impact configuration action.

Also extend the §10 Provider administration bullet:

> - Provider administration: create, update, list, enable, or disable Provider and model options, including capability metadata and versioned pricing, where authorized; read results carry the revision and freshness markers required by FR-29.

### Edit D — amend §6.2 out-of-scope (row 5)

Replace the bullet at `prd.md:488`.

> - Fine-grained launch billing, invoicing, or monetization. Administrator-supplied catalog pricing metadata used to govern selection and cost control is in scope (FR-4).

### No edit proposed

Rows 8–12. In particular, row 12: do not add wording implying that catalog pricing already drives reservation or cap enforcement — it does not.

## Notes on the reviewer's prior claims

- Verified: pricing shape, eligibility tightening, capability-version bump rules, `IsSelectableForNewActiveUse` beyond enabled-only, `§6.2` exclusion risk, `§10` omission.
- Corrected: "any decrease is a rejection" (see row 4 — the check is against a caller-supplied expected version, which is optional).
- Additional finding not in the drift review: the projection-freshness / truth-stage model (row 7) shipped as a public contract shape and has no FR either; NFR-14 already depends on it.
- Out of scope for this extract (already filed elsewhere): `EXT-PROVIDER-1` remaining `Uncommitted` while Story 5.3 closed, and the Evidence Level 4 claim over in-process fakes.
