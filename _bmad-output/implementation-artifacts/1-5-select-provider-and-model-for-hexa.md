---
baseline_commit: 7bb74654329ec67abdb6b47ceb0e1ded749e5d46
---

# Story 1.5: Select Provider And Model For `hexa`

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an Agent Administrator,
I want to select an enabled Provider/model from the governed catalog for `hexa`,
so that future Agent calls use an approved model choice with explainable readiness and audit evidence.

## Acceptance Criteria

**AC1 - Recording a selection captures safe Provider/model identity + capability version + configuration version (audit-ready)**
**Given** enabled Provider/model catalog entries exist
**When** an authorized administrator selects a Provider/model for `hexa`
**Then** the Agent configuration records `ProviderId`, `ModelId`, provider capability version, and configuration version
**And** enough safe Provider/model identity is available for future Audit Evidence.

**AC2 - Reject selection/activation on a not-ready Provider/model, with no provider SDK call or credential access (fail closed)**
**Given** the selected Provider/model is disabled, missing, not configured, not text-generation capable, or lacks required context/output/timeout metadata
**When** the administrator attempts to select it or activate `hexa`
**Then** the system rejects the selection or activation with a Provider readiness blocker
**And** no provider SDK call or credential access occurs.

**AC3 - A selection change applies only to future Agent Calls; prior versions/evidence are not rewritten**
**Given** an existing Agent selection is changed
**When** future Agent configuration is inspected
**Then** the new Provider/model applies only to future Agent Calls
**And** prior configuration versions and historical evidence are not rewritten.

**AC4 - Provider/model status fails closed cross-tenant and never reveals another tenant's records**
**Given** provider/model status is displayed through setup status contracts
**When** a caller lacks authorization or tenant access
**Then** the response fails closed
**And** does not reveal Provider/model records from another tenant.

[Source: _bmad-output/planning-artifacts/epics.md#Story-1.5-Select-Provider-And-Model-For-hexa; prd.md#functional-requirements-fr-5-select-provider-and-model-per-agent; ARCHITECTURE-SPINE.md#AD-4-Interaction-Snapshot; ARCHITECTURE-SPINE.md#AD-9-Provider-Adapter-And-Catalog-Boundary; ARCHITECTURE-SPINE.md#AD-10-Provider-Capability-Floor]

## Tasks / Subtasks

- [x] **Task 1 - Add the provider-selection public contracts to `Hexalith.Agents.Contracts/Agent/`** (AC: #1, #2, #4)
  - [x] Add a safe validation-verdict enum `ProviderSelectionValidationStatus` under `Hexalith.Agents.Contracts/Agent/` with values `Unknown = 0, Valid, Disabled, Missing, NotConfigured, NotTextGenerationCapable, MissingCapabilityMetadata, Unauthorized, Unavailable`. Decorate it `[JsonConverter(typeof(JsonStringEnumConverter))]` and document `Unknown` as the fail-safe sentinel — **mirror the exact shape/XML-doc style of `PartyLinkValidationStatus.cs`** (Story 1.4). This enum classifies *which* provider-readiness state blocked a selection/activation; it carries no secrets, no provider SDK types, no capability payload — just the safe verdict (AD-9, AD-14).
  - [x] Add the aggregate command `Agent/Commands/SelectAgentProviderModel(string ProviderId, string ModelId, int ProviderCapabilityVersion)`. The payload carries only the safe `ProviderId`/`ModelId` identifiers (Agents/Provider-owned, **not secrets** — Identity convention) plus the captured `ProviderCapabilityVersion`. It does NOT carry the verdict (see Task 6 trust model) and never carries a secret value, configuration reference, capability metadata blob, or provider SDK type. Mirror the bare-record shape of `LinkAgentPartyIdentity.cs`.
  - [x] Add the success event `Agent/Events/AgentProviderModelSelected(string AgentId, string ProviderId, string ModelId, int ProviderCapabilityVersion, int ConfigurationVersion) : IEventPayload` (no wall-clock fields). This durable event **is** the AC1 Audit Evidence: it records the safe provider/model identity, the capability version selected, and the resulting configuration version. Mirror `AgentPartyIdentityLinked.cs`.
  - [x] Add the typed rejection `Agent/Events/Rejections/AgentProviderModelSelectionRejected(string AgentId, ProviderSelectionValidationStatus Status) : IRejectionEvent` (provider-readiness failure — AC2). Mirror `AgentPartyIdentityLinkRejected.cs`. **Reuse** the existing `AgentNotFoundRejection` and `AgentAdministrationDeniedRejection` from Story 1.3 — do NOT duplicate them.
  - [x] Extend `AgentActivationBlocker` by **appending** `MissingProviderSelection` (no Provider/model selected yet) then `ProviderUnavailable` (a Provider/model is selected but it is currently not selectable/ready) **after** `MissingPartyIdentity` (preserve existing ordinals `Unknown=0, MissingDisplayName=1, MissingInstructions=2, InvalidInstructions=3, MissingPartyIdentity=4`; the new values become `MissingProviderSelection=5`, `ProviderUnavailable=6`). Do not reorder/renumber — the enum is additively extensible by design. `ProviderUnavailable` maps to the canonical UX `provider unavailable` readiness state.
  - [x] Extend `AgentStatusView` by **appending** `bool HasProviderSelection`, `string? SelectedProviderId`, `string? SelectedModelId` (after the existing trailing `IReadOnlyList<AgentActivationBlocker> ActivationBlockers` is moved to remain last, OR insert before it — keep `ActivationBlockers` the final positional parameter to match the existing read-path call shape). `ProviderId`/`ModelId` are safe identifiers (AD-9 — not secrets), intended for the overview/config surface (UX-DR2). Do NOT add the capability version, the configuration reference, or any secret/credential field to the status view.
  - [x] Keep `Hexalith.Agents.Contracts` inward-only: reference nothing outward except the EventStore **contracts** marker already wired (no provider SDK, no `Microsoft.Agents.AI`, no Dapr, no server infra). Provider/model ids are plain `string`. The Story 1.1 secret-non-disclosure guard (forbidden member tokens `Secret`/`ApiKey`/`Credential`/`Password`/`ConnectionString`) must stay green and unweakened — keep all new member names clear of those tokens.

- [x] **Task 2 - Extend the pure `Agent` aggregate replay state** (AC: #1, #3)
  - [x] In `Hexalith.Agents/Agent/AgentState.cs` add `public string? ProviderId { get; set; }`, `public string? ModelId { get; set; }`, `public int? ProviderCapabilityVersion { get; set; }` (all null = no selection). These are the ONLY new durable fields — safe identifiers + version, no secrets, no metadata blob (AC1, AD-9, AD-14).
  - [x] Add `Apply(AgentProviderModelSelected e)` → set `ProviderId = e.ProviderId`, `ModelId = e.ModelId`, `ProviderCapabilityVersion = e.ProviderCapabilityVersion`, `ConfigurationVersion = e.ConfigurationVersion` (guarded by `if (!IsCreated) return;` exactly like the existing update applies).
  - [x] Add a **no-op** `Apply(AgentProviderModelSelectionRejected e)` calling `MarkReplayOnlyEventHandled()` — replay stays total exactly like the Story 1.3/1.4 persisted rejections.

- [x] **Task 3 - Implement the pure aggregate selection handler + activation provider gate** (AC: #1, #2, #3)
  - [x] In `Hexalith.Agents/Agent/AgentAggregate.cs` add a private const `ProviderSelectionValidationExtensionKey = "provider:selectionValidation"` and a helper `ReadProviderSelectionValidation(CommandEnvelope envelope)` that parses the extension into `ProviderSelectionValidationStatus` using the **exact-name, case-sensitive match** idiom (`Enum.TryParse(value, ignoreCase: false, out status) && string.Equals(Enum.GetName(status), value, StringComparison.Ordinal)`, else `Unknown`) — copy `ReadPartyLinkValidation` verbatim in shape so numeric/aliased/cased input fails closed to `Unknown` (the Story 1.4 review hardening).
  - [x] Add `Handle(SelectAgentProviderModel command, AgentState? state, CommandEnvelope envelope)` following the exact guard cascade of `Handle(LinkAgentPartyIdentity)`: `ArgumentNullException.ThrowIfNull(command/envelope)` → `IsAgentAdmin(envelope)` else `Denied(agentId, envelope, nameof(SelectAgentProviderModel))` → `state is null || !state.IsCreated` else `AgentNotFoundRejection`.
  - [x] **AC2 (fail closed):** read the verdict; if it is anything other than `Valid`, emit `AgentProviderModelSelectionRejected(agentId, status)` and change no state. This single check covers Disabled/Missing/NotConfigured/NotTextGenerationCapable/MissingCapabilityMetadata/Unauthorized/Unavailable/Unknown. A direct-gateway command that never went through the orchestration has no trusted verdict and is rejected here — **this is the security guarantee that no bad selection is ever recorded and no provider SDK/credential path is reachable from the aggregate** (the aggregate makes no provider calls at all — AD-3).
  - [x] **Valid verdict, idempotent re-select:** if `state.ProviderId == command.ProviderId` AND `state.ModelId == command.ModelId` AND `state.ProviderCapabilityVersion == command.ProviderCapabilityVersion` → `DomainResult.NoOp()` (deterministic; no duplicate event, no version bump — AD-13).
  - [x] **Valid verdict, new/changed selection:** emit `AgentProviderModelSelected(agentId, command.ProviderId, command.ModelId, command.ProviderCapabilityVersion, state.ConfigurationVersion + 1)`. Selecting/changing the provider is a configuration change → bump `ConfigurationVersion` (needed for the AD-4 interaction snapshot in Epic 2). A changed selection deterministically overwrites the single recorded selection; **prior events are append-only and never rewritten** (AC3) and a later `AgentInteraction` snapshots the config version + provider/model + capability version at request time, so changing selection affects only future calls.
  - [x] Do NOT change `Lifecycle` here — preserve the Story 1.3 "lifecycle ≠ callability" invariant; provider readiness is surfaced through the blocker, not by auto-activating/demoting.
  - [x] **Extend `Handle(ActivateAgent)` for the AC2 "or activate `hexa`" path:** activation must re-validate the *currently recorded* selection. Read `ReadProviderSelectionValidation(envelope)` (the activation orchestration re-reads the catalog for the recorded `(ProviderId, ModelId)` and populates this trusted extension — Task 6) and pass provider readiness to `ComputeActivationBlockers` (Task 4). A direct `ActivateAgent` that did not re-validate (no trusted provider verdict) **fails closed** with `ProviderUnavailable` whenever a selection is present — the correct, AC2-aligned behavior.
  - [x] Keep the aggregate pure (AD-3): no `ProviderCatalog` query, no I/O, no `DateTimeOffset.UtcNow`, no `Guid.NewGuid`, no state mutation outside `Apply`. The verdict and the capability version are plain data fed in through the command/envelope; the aggregate never reads the catalog or calls a provider.

- [x] **Task 4 - Wire the provider-readiness gate into `AgentConfigurationPolicy` + `AgentInspection`** (AC: #2, #4)
  - [x] Change `AgentConfigurationPolicy.ComputeActivationBlockers` to accept provider readiness — `ComputeActivationBlockers(string displayName, string instructions, bool hasPartyIdentity, bool hasProviderSelection, bool selectedProviderReady)` — and after the party gate append, in deterministic order: `if (!hasProviderSelection) → MissingProviderSelection; else if (!selectedProviderReady) → ProviderUnavailable`. Keep the full deterministic order: display name → instructions → party identity → provider selection → provider unavailable.
  - [x] Update both callers:
    - `AgentAggregate.Handle(ActivateAgent, ...)` passes `hasProviderSelection: state.ProviderId is not null` and `selectedProviderReady: ReadProviderSelectionValidation(envelope) == ProviderSelectionValidationStatus.Valid`. Activation now fails closed with `MissingProviderSelection` (no selection) or `ProviderUnavailable` (selected-but-not-ready) until both clear (AC2).
    - `AgentInspection.ToView(state)` passes `hasProviderSelection: state.ProviderId is not null` and `selectedProviderReady: true`, and sets the new `HasProviderSelection`/`SelectedProviderId`/`SelectedModelId` view fields. **Rationale:** `AgentInspection` is a *pure* read over Agent state only — it cannot freshly re-read the catalog, so it trusts the last-validated recorded selection and surfaces the static `MissingProviderSelection` gate. Live `ProviderUnavailable` surfacing for the readiness badge is supplied by the activation path (this story, via the verdict) and by the 1.8 status/overview orchestration that reads the catalog. Document this boundary in a code comment — do NOT make `AgentInspection` read the catalog (that would break purity and AD-3).
  - [x] Confirm the readiness contract still keeps lifecycle and blockers distinct (Story 1.3 invariant; UX `agent-readiness-badge`): a ready provider clears the provider blockers but does not by itself make a `Draft`/`Disabled` agent `Active`.

- [x] **Task 5 - Add a replay-derived provider capability version to the `ProviderCatalog` entry** (AC: #1)
  - [x] **Why this exists:** AC1 (and AD-4) require recording a *provider capability version*. The Story 1.2 `ProviderCatalog` has **no capability-version field today** — metadata versioning is only implicit in the event stream. Story 1.5 is the first consumer that needs an explicit, snapshot-able version, so add a minimal, **purely additive** replay-derived counter. [Source: 1-2-govern-provider-catalog-entries.md#Dev-Agent-Record; ARCHITECTURE-SPINE.md#AD-4; #AD-10]
  - [x] In `Hexalith.Agents/ProviderCatalog/ProviderModelEntryState.cs` add `public int CapabilityVersion { get; set; }`. Increment it in replay only: `Apply(ProviderModelEntryCreated)` sets `CapabilityVersion = 1`; `Apply(ProviderModelEntryMetadataUpdated)` does `CapabilityVersion += 1`. Enable/disable events (`ProviderModelEntryEnabled`/`Disabled`) do NOT change capability metadata, so they MUST NOT bump the version. (Because Story 1.2 emits `ProviderModelEntryMetadataUpdated` only on a real change — exact-duplicate updates are `NoOp` — the counter increments only on genuine capability changes.)
  - [x] **Do NOT change any existing event payload** (`ProviderModelEntryCreated`/`ProviderModelEntryMetadataUpdated` records stay as-is) — the version is derived during `Apply`, not carried on the wire. This keeps the change additive and replay-safe and avoids reshaping Story 1.2 contracts.
  - [x] Expose the version on the read projection: add `int CapabilityVersion` as the **trailing positional parameter** of `ProviderCatalogEntryView` (`Hexalith.Agents.Contracts/ProviderCatalog/ProviderCatalogEntryView.cs`) and populate it in `ProviderCatalogInspection.ToView(entry)` from `entry.CapabilityVersion`. This is the value the selection orchestration captures into `ProviderCapabilityVersion` (Task 6). Update the existing Story 1.2 `ProviderCatalogEntryView` constructions (inspection + tests) for the new trailing parameter.
  - [x] Keep the ProviderCatalog aggregate pure and the secret guard green — `CapabilityVersion` is a plain int, exposes nothing secret.

- [x] **Task 6 - Provider-catalog reader port + selection orchestration in `Hexalith.Agents.Server`** (AC: #1, #2, #4)
  - [x] Define a server port `Hexalith.Agents.Server/Ports/IProviderCatalogReader` with `Task<ProviderCatalogEntryReadResult> GetEntryAsync(string tenantId, string providerId, string modelId, CancellationToken ct)`. `ProviderCatalogEntryReadResult` is a server-internal type carrying `ProviderCatalogInspectionStatus Status` + the safe `ProviderCatalogEntryView? Entry`. These port types are NOT public contracts — keep them in the Server project (mirror `AgentPartyValidationResult` from Story 1.4).
  - [x] Provide a **deferred** adapter `DeferredProviderCatalogReader : IProviderCatalogReader` (mirror `DeferredAgentCommandDispatcher` from Story 1.4): keep the DI graph complete and compiling; the live binding to the catalog read-model / rehydrated `ProviderCatalogState` (via `ProviderCatalogInspection.GetEntry`) is **deferred** to the dedicated read-model story, exactly as Story 1.2 deferred its DAPR read-path binding and Story 1.4 deferred the command-dispatch binding. The orchestration decision logic must be fully unit-testable here via a substituted `IProviderCatalogReader`.
  - [x] Add the orchestration `Hexalith.Agents.Server/Application/Agents/AgentProviderSelectionOrchestrator` that: (1) authorizes the actor as an Agents admin and strips any client-supplied reserved extensions; (2) calls `IProviderCatalogReader.GetEntryAsync`; (3) computes the `ProviderSelectionValidationStatus` verdict from the result with a **deterministic precedence** (read `EntryNotFound → Missing`; read `NotAuthorized → Unauthorized`; read failure/unavailable `→ Unavailable` (fail closed, AD-12); then on a `Success` entry: `Status != ProviderModelStatus.Enabled → Disabled`; `!SupportsTextGeneration → NotTextGenerationCapable`; `ConfigurationState != Configured → NotConfigured`; `ContextWindowTokenLimit <= 0 || MaxOutputTokenLimit <= 0 || MaxOutputTokenLimit > ContextWindowTokenLimit || invalid TimeoutPolicy → MissingCapabilityMetadata`; else `Valid`); (4) captures `entry.CapabilityVersion` as the `ProviderCapabilityVersion`; (5) builds `SelectAgentProviderModel(providerId, modelId, capabilityVersion)` with the **server-populated** trusted extensions `actor:agentsAdmin=true` and `provider:selectionValidation=<Status>`; (6) dispatches it through `IAgentCommandDispatcher`. Always feed the computed verdict (even non-`Valid`) so the aggregate records an auditable `AgentProviderModelSelectionRejected`.
  - [x] **Trust model (CRITICAL):** `provider:selectionValidation` is server-populated ONLY, exactly like `actor:agentsAdmin` and `party:linkValidation`. The command entry point must strip both reserved keys from any client-supplied payload/extensions and repopulate them from the orchestration's catalog read / trusted claims. A client must never assert `provider:selectionValidation=Valid` to bypass catalog validation. The aggregate's independent non-`Valid` rejection (Task 3) is the security boundary; the orchestration always feeding the verdict gives the durable audit trail.
  - [x] **Activation re-validation (AC2 "or activate"):** ensure the activation command path populates `provider:selectionValidation` by re-reading the catalog for the *recorded* `(state.ProviderId, state.ModelId)` before dispatching `ActivateAgent`. If an activation orchestration does not yet exist (Story 1.3 dispatched `ActivateAgent` directly), add a minimal `AgentActivationProviderRevalidation` step (or extend the existing path) that performs the catalog read and populates the verdict. A direct activation without this step fails closed with `ProviderUnavailable` when a selection is present (Task 3) — acceptable, but the orchestration is what lets a genuinely-ready agent activate.
  - [x] Register the port adapter + orchestration in `Hexalith.Agents.Server/Program.cs`. Binding the reader/dispatcher to the live DAPR/EventStore gateway and a runnable AppHost topology is **deferred** (mirroring Stories 1.2/1.3/1.4) — register what compiles cleanly and note the deferred runtime binding in a completion note. No new sibling-module references and no provider SDK are needed: the `ProviderCatalog` is in-module.

- [x] **Task 7 - Tests + narrow verification** (AC: #1, #2, #3, #4)
  - [x] **Domain (`tests/Hexalith.Agents.Tests`):** extend `AgentTestData` with a provider-validation envelope helper (sets `provider:selectionValidation`) and a `StateWithSelectedProvider(...)` helper; add the new event/rejection cases to the `ApplyAll` dispatch switch. Aggregate tests (new `AgentProviderSelectionTests`): select success (stores `ProviderId`/`ModelId`/`ProviderCapabilityVersion`, bumps `ConfigurationVersion`, lifecycle unchanged); select with each non-`Valid` verdict → `AgentProviderModelSelectionRejected` carrying that status AND nothing stored AND `MissingProviderSelection`/`ProviderUnavailable` still blocks activation (AC2); re-select identical provider/model/version → `NoOp` (idempotent, AD-13); change selection → new `AgentProviderModelSelected` with bumped `ConfigurationVersion`, single recorded selection, prior event not rewritten (AC3); select on missing agent → `AgentNotFoundRejection`; unauthorized select → `AgentAdministrationDeniedRejection`; the verdict-parse regression theory (`"1"`, `"01"`, `"valid"`, `" Valid"` → fail closed to `Unknown`) mirroring the Story 1.4 hardening; replay/rehydration through `Apply` including the persisted rejection. Add activation cases: activation blocked by `MissingProviderSelection` (no selection), blocked by `ProviderUnavailable` (selection present, verdict non-`Valid`/absent), and unblocked when a selection is present AND the verdict is `Valid`. Extend the `AgentLifecycleE2ETests` journey (and **update Story 1.3/1.4 activation tests** to also select a provider + supply the `Valid` provider verdict before activating — the correct consequence of the new gate, exactly as Story 1.4 updated Story 1.3's tests for the party gate).
  - [x] **ProviderCatalog domain (`tests/Hexalith.Agents.Tests`):** extend `ProviderCatalogStateReplayTests`/`ProviderCatalogInspectionTests` to assert `CapabilityVersion` = 1 after create, increments on each `ProviderModelEntryMetadataUpdated`, and is unchanged by enable/disable; assert `ProviderCatalogEntryView.CapabilityVersion` is mapped from state. Update `ProviderCatalogTestData` / existing `ProviderCatalogEntryView` constructions for the new trailing parameter.
  - [x] **Contracts (`tests/Hexalith.Agents.Contracts.Tests`):** new events/rejections implement the expected EventStore marker interfaces; System.Text.Json round-trip for `AgentProviderModelSelected`, `AgentProviderModelSelectionRejected`, the extended `AgentStatusView`, and the extended `ProviderCatalogEntryView`; `ProviderSelectionValidationStatus` and the new `MissingProviderSelection`/`ProviderUnavailable` blockers serialize by name and unknown input deserializes to the `Unknown`/sentinel fail-safe; assert the provider events/rejections + status view expose only id-shaped/version fields and no secret/credential/configuration-reference member; the Story 1.1 secret-non-disclosure guard (`ContractsSecretNonDisclosureTests`) stays green and unweakened.
  - [x] **Server (`tests/Hexalith.Agents.Server.Tests`):** orchestration tests (new `AgentProviderSelectionOrchestratorTests`) using a substituted `IProviderCatalogReader` (NSubstitute `5.3.0` is already centralized) — assert each catalog read outcome maps to the correct verdict per the deterministic precedence (incl. `EntryNotFound→Missing`, `NotAuthorized→Unauthorized`, read-failure→`Unavailable`, `Disabled`, `NotTextGenerationCapable`, `NotConfigured`, `MissingCapabilityMetadata`, and the all-pass `Valid`); the captured `ProviderCapabilityVersion` equals the entry's `CapabilityVersion`; the orchestration authorizes, sets the server-populated `provider:selectionValidation`/`actor:agentsAdmin` extensions (stripping client-supplied ones), and dispatches the correct command/verdict; `DeferredProviderCatalogReader` behaves like the Story 1.4 deferred placeholder. Structural/boundary guards (`ProjectReferenceDirectionTests`, `PublicContractPackageBoundaryTests`, `StructuralSeedConformanceTests`, `ModuleLayout`, `PackageVersionCentralizationTests`) stay green.
  - [x] xUnit v3 + Shouldly; PascalCase BDD-style names matching the surrounding tests; no raw `Assert.*`. Run from `Hexalith.Agents/`: `dotnet restore Hexalith.Agents.slnx`, `dotnet build Hexalith.Agents.slnx --configuration Release` (must be 0 warnings / 0 errors, warnings-as-errors), then each touched test project individually with `dotnet test <project> --configuration Release`.

## Dev Notes

### Critical Guardrails

- This story is **Provider/model selection onto the existing `Agent` aggregate + its readiness gate + the capability-version source on the catalog entry, ONLY**. Do NOT implement response mode / approver policy (1.6), content safety / activation gate hardening (1.7), the FrontComposer setup UI (1.8), the DAPR read-model binding, Conversation membership/posting/invocation, generation, `AgentInteraction`, or proposals (Epics 2–3). The aggregate makes **no provider SDK calls** in this story (and never will — AD-3/AD-9); selection is configuration only. [Source: epics.md#Epic-1; ARCHITECTURE-SPINE.md#AD-9-Provider-Adapter-And-Catalog-Boundary]
- `Agent` owns provider/model **selection**; `ProviderCatalog` owns provider/model **records** (AD-2). Selection layers onto the **existing** `Agent` aggregate (Story 1.3) — extend it; do not fold selection into `ProviderCatalog`/`AgentInteraction`, and do not move catalog records onto the `Agent`. The `Agent` stores only the safe `(ProviderId, ModelId, ProviderCapabilityVersion)` reference; the capability metadata stays in `ProviderCatalog`. [Source: ARCHITECTURE-SPINE.md#AD-2-Aggregate-Boundaries; #Consistency-Conventions (Identity)]
- **Aggregates stay pure (AD-3).** Provider/catalog reads run in the Server application orchestration/adapter and the result (verdict + capability version) is **fed back to the aggregate through a command** (the trusted `provider:selectionValidation` extension + the safe payload). The aggregate emits events only — it never reads the catalog, calls a provider, reads time, or does I/O. [Source: ARCHITECTURE-SPINE.md#AD-3-Pure-Aggregates-Side-Effects-Outside]
- **Safe identifiers only, never secrets or capability blobs (AD-9, AD-14, AC1).** `ProviderId`/`ModelId` are stable safe identifiers (not secrets); `ProviderCapabilityVersion` is a plain int. No provider secret value, configuration reference, credential, capability-metadata payload, or provider SDK type enters Agents events, state, status, logs, telemetry, or audit summaries. Public contracts and durable events expose only `ProviderId`, `ModelId`, safe capability *version*, and safe verdict classes. [Source: ARCHITECTURE-SPINE.md#AD-9; #AD-14-Sensitive-Content-And-Secret-Safety; prd.md#provider-safety; prd.md#non-goals]
- **Fail closed on provider readiness/dependency uncertainty (AD-12, AC2).** Disabled, missing, not-configured, not-text-generation-capable, missing-capability-metadata, unauthorized, or unavailable (incl. stale/degraded/transport failure) provider state rejects the *selection* AND blocks *activation*. Never treat an unreadable catalog as ready. While not ready, `hexa` carries `MissingProviderSelection`/`ProviderUnavailable` and is not callable. **No provider SDK call or credential access occurs on the rejection path** — the verdict is computed from the safe catalog projection, and the aggregate never reaches a provider. [Source: ARCHITECTURE-SPINE.md#AD-12-Authorization-And-Dependency-Uncertainty; prd.md#functional-requirements-fr-21-fail-closed-on-dependency-uncertainty; prd.md#functional-requirements-fr-10-handle-generation-failure; epics.md#Story-1.5 AC2]
- **Selection change affects future calls only; nothing is rewritten (AC3, AD-4).** A changed selection emits a new append-only `AgentProviderModelSelected` and bumps `ConfigurationVersion`; prior events are immutable. `AgentInteraction` (Epic 2) snapshots `ProviderId`/`ModelId`/provider capability version/configuration version at request time per AD-4, so in-flight/historical interactions are unaffected. Re-selecting the same provider/model/version is a deterministic `NoOp` (AD-13). [Source: ARCHITECTURE-SPINE.md#AD-4-Interaction-Snapshot; #AD-13-Idempotent-External-Effects; prd.md#functional-requirements-fr-5-select-provider-and-model-per-agent; epics.md#Story-1.5 AC3]
- **Authorization + tenant isolation fail closed before mutation/disclosure (AD-12, AC4).** Reuse the transitional, server-populated `actor:agentsAdmin` extension gate exactly as Stories 1.3/1.4 — client-supplied reserved extensions are stripped at the entry point and never trusted. Unauthorized select → `AgentAdministrationDeniedRejection`. The status read (`AgentInspection.GetStatus`) returns `NotAuthorized` for non-admins. Cross-tenant isolation is structural: the `Agent` aggregate and the `ProviderCatalog` read are tenant-scoped, so a cross-tenant catalog read surfaces as `NotAuthorized → Unauthorized` and never leaks another tenant's Provider/model records. [Source: 1-3-configure-and-manage-hexa-lifecycle.md#Authorization-Guidance; ARCHITECTURE-SPINE.md#AD-12; prd.md#functional-requirements-fr-19-enforce-tenant-isolation; prd.md#functional-requirements-fr-20-enforce-role-and-policy-authorization; epics.md#Story-1.5 AC4]

### Design: Where Each Responsibility Lives (the AD-3 round-trip)

```
Admin/API → Server orchestration (Application/Agents) ── authorize actor (agentsAdmin)
                                          │
                                          ├─ IProviderCatalogReader port (Ports/) ── ProviderCatalog read
                                          │      • GetEntryAsync → ProviderCatalogInspection.GetEntry → safe ProviderCatalogEntryView
                                          │      • compute ProviderSelectionValidationStatus (deterministic precedence)
                                          │      • capture entry.CapabilityVersion
                                          │
                                          └─ dispatch aggregate command  SelectAgentProviderModel(ProviderId, ModelId, CapabilityVersion)
                                                 envelope.Extensions (server-populated, client-stripped):
                                                   actor:agentsAdmin            = true
                                                   provider:selectionValidation = <ProviderSelectionValidationStatus>
                                                          │
                          pure AgentAggregate.Handle ─────┘  emits  ProviderModelSelected / ProviderModelSelectionRejected
                                          │
                                  AgentState.Apply  stores ONLY (ProviderId, ModelId, ProviderCapabilityVersion), bumps ConfigurationVersion
```

Rationale for putting the verdict in a **trusted envelope extension** (not the command payload): the EventStore gateway routes any aggregate command by domain/type, so a malicious client could POST `SelectAgentProviderModel{ProviderId, ModelId, CapabilityVersion}` directly with a spoofed capability version. The aggregate therefore requires trusted evidence that catalog validation happened — `provider:selectionValidation` is server-populated and client-stripped, identical to the `party:linkValidation` (1.4) and `actor:agentsAdmin` (1.3) trust models. The aggregate independently rejects any non-`Valid`/absent verdict (Task 3), which is the security guarantee (no bad selection recorded, no provider/credential path reachable); the orchestration always feeding the verdict gives the durable audit trail. The safe `ProviderId`/`ModelId`/`CapabilityVersion` in the payload are only ever recorded *after* a `Valid` verdict gates them. [Source: ARCHITECTURE-SPINE.md#AD-3; #AD-9; #AD-12; #AD-13; 1-4-link-hexa-to-a-party-identity.md#Design-Where-Each-Responsibility-Lives]

### Current Code State To Preserve

Read these completely before editing — they are the exact patterns to extend (do not reshape Story 1.3/1.4 events or Story 1.2 catalog contracts):

- `Hexalith.Agents/src/Hexalith.Agents/Agent/AgentAggregate.cs` — `[EventStoreDomain("agent")]`, static `Handle(command, AgentState?, CommandEnvelope) → DomainResult`, helpers `IsAgentAdmin`/`Denied`/`Invalid`/`ReadPartyLinkValidation`, consts `AgentAdminExtensionKey = "actor:agentsAdmin"` and `PartyLinkValidationExtensionKey = "party:linkValidation"`. Add the `SelectAgentProviderModel` handler + `ReadProviderSelectionValidation` here using the same cascade, and extend `Handle(ActivateAgent)`.
- `Hexalith.Agents/src/Hexalith.Agents/Agent/AgentState.cs` — replay state (fields incl. `IsCreated`, `ConfigurationVersion`, `Lifecycle`, `PartyId`); `Apply(success)` mutates under `if (!IsCreated) return;`, `Apply(rejection)` is a no-op via `MarkReplayOnlyEventHandled()`. Add `ProviderId`/`ModelId`/`ProviderCapabilityVersion` + the two new `Apply` overloads here.
- `Hexalith.Agents/src/Hexalith.Agents/Agent/AgentConfigurationPolicy.cs` — `ComputeActivationBlockers(displayName, instructions, hasPartyIdentity)`; centralizes the gate so the read path and the activation path stay in lock-step. Add the two provider params + blockers here (one place updates both callers).
- `Hexalith.Agents/src/Hexalith.Agents/Agent/AgentInspection.cs` — pure read path → `AgentStatusView`; `GetStatus` fails closed (`NotAuthorized`/`AgentNotFound`). Add the new view fields and pass provider presence (`true` ready) to the policy; keep it pure (no catalog read).
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/AgentActivationBlocker.cs` (`Unknown=0…MissingPartyIdentity=4`) and `AgentStatusView.cs` (`…, bool HasPartyIdentity, IReadOnlyList<AgentActivationBlocker> ActivationBlockers`) — the exact enum/record shapes to extend additively (preserve ordinals; append fields, keep `ActivationBlockers` last).
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/PartyLinkValidationStatus.cs` — the verdict-enum template (`[JsonConverter(typeof(JsonStringEnumConverter))]`, `Unknown = 0` sentinel) to mirror for `ProviderSelectionValidationStatus`.
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/Commands/LinkAgentPartyIdentity.cs`, `Events/AgentPartyIdentityLinked.cs`, `Events/Rejections/AgentPartyIdentityLinkRejected.cs` — bare-record + `IEventPayload`/`IRejectionEvent` marker style, no wall-clock fields. Mirror these for the provider command/event/rejection.
- `Hexalith.Agents/src/Hexalith.Agents/ProviderCatalog/{ProviderModelEntryState,ProviderCatalogInspection}.cs` and `Hexalith.Agents.Contracts/ProviderCatalog/{ProviderCatalogEntryView,ProviderModelStatus,ProviderConfigurationState}.cs` — the catalog state/projection to read and the entry where the `CapabilityVersion` counter is added. `ProviderModelStatus`: `Unknown=0, Enabled, Disabled, Degraded, Failed`; `ProviderConfigurationState`: `Unknown=0, NotConfigured, Configured`.
- `Hexalith.Agents/src/Hexalith.Agents.Server/{Program.cs, Ports/DeferredAgentCommandDispatcher.cs, Ports/IAgentCommandDispatcher.cs, Application/Agents/AgentPartyIdentityOrchestrator.cs}` — the host scans `typeof(AgentsAssemblyMarker).Assembly`, so new aggregate handlers auto-register (no `Handle` wiring needed). `AgentPartyIdentityOrchestrator` + `DeferredAgentCommandDispatcher` are the exact orchestration/deferred-binding templates to mirror for the provider reader/orchestration. `IAgentCommandDispatcher` is the dispatch seam to reuse.
- `Hexalith.Agents/tests/Hexalith.Agents.Tests/{AgentTestData,ProviderCatalogTestData}.cs` — `Envelope<T>(... isAgentsAdmin ...)` builder (extend with `provider:selectionValidation`), `ApplyAll` dispatch switch (add new events), `ProcessAndApplyAsync` E2E driver, and the catalog test fixtures.

What must be preserved: `.slnx` only, `net10.0`, C# 14, nullable, implicit usings, warnings-as-errors, Central Package Management (no inline versions), provider-SDK-free + secret-value-free public contracts, pure replay-safe aggregates, and the Agents-module **no-`UtcNow`-in-aggregates** convention (occurrence time comes from EventStore metadata; actor from `envelope.UserId`). [Source: 1-4-link-hexa-to-a-party-identity.md#Current-Code-State-To-Preserve; ARCHITECTURE-SPINE.md#Stack; #Consistency-Conventions]

### ProviderCatalog Read Facts (verified against the as-built Story 1.2 module)

The selection orchestration consumes the **in-module** `ProviderCatalog` read path — no sibling-module client, no provider SDK. Verified facts:

- **Read entry point** (`Hexalith.Agents/ProviderCatalog/ProviderCatalogInspection.cs`): `GetEntry(ProviderCatalogState? state, bool isProviderAdmin, string providerId, string modelId) → ProviderCatalogInspectionResult` and `ListEntries(state, isProviderAdmin, bool includeDisabled)`. Pure logic over rehydrated catalog state; fail-closed (`NotAuthorized`/`EntryNotFound`); cross-tenant isolation is structural (single-tenant catalog state). The live read-model binding is **deferred** (Story 1.2 scope note) — hence Task 6's `DeferredProviderCatalogReader`.
- **`ProviderCatalogInspectionResult`** (`…Contracts/ProviderCatalog/ProviderCatalogInspectionResult.cs`): `{ ProviderCatalogInspectionStatus Status; IReadOnlyList<ProviderCatalogEntryView> Entries }`. **`ProviderCatalogInspectionStatus`**: `Success = 0, NotAuthorized, EntryNotFound`.
- **`ProviderCatalogEntryView`** (the safe projection — no secrets) carries everything the verdict needs: `ProviderId`, `ModelId`, `DisplayLabel`, `ProviderModelStatus Status`, `bool SupportsTextGeneration`, `int ContextWindowTokenLimit`, `int MaxOutputTokenLimit`, `ProviderModelTimeoutPolicy TimeoutPolicy` (`{ int RequestTimeoutMilliseconds; int MaxRetries }`), `ProviderModelCapabilityFlags SafeCapabilityFlags`, `ProviderConfigurationState ConfigurationState`, `string? ConfigurationReferenceId`, `bool IsSelectableForNewActiveUse`, **+ the new `int CapabilityVersion` (Task 5)**. No raw credentials/secret values/SDK types ever appear.
- **Enabled + configured semantics:** an entry is selectable iff `Status == ProviderModelStatus.Enabled` AND `ConfigurationState == ProviderConfigurationState.Configured` (a non-blank, regex-safe `ConfigurationReferenceId` is what makes state `Configured`; the secret *value* is never stored or exposed). `IsSelectableForNewActiveUse` already encodes `IsEnabled && Enabled`; the verdict additionally enforces text-gen + configured + metadata.
- **Entry identity:** keyed by composite `EntryKey(providerId, modelId) = $"{providerId}{modelId}"` (ASCII unit separator avoids `("ab","c")` vs `("a","bc")` collisions). `GetEntry`/the orchestration look up by the `(ProviderId, ModelId)` pair, not a single id string.
- **Capability version (NEW — Task 5):** Story 1.2 has **no** explicit version field; this story adds a replay-derived `CapabilityVersion` counter (create→1, each `ProviderModelEntryMetadataUpdated`→+1, enable/disable→no change) on `ProviderModelEntryState`, surfaced on `ProviderCatalogEntryView`. Additive only; no existing event payload changes.
- **Authorization:** Story 1.2 uses the transitional server-populated `actor:agentsProviderAdmin` extension for catalog *admin* mutations. Story 1.5's *selection* is an Agents-admin action; the orchestration authorizes the actor as an Agents admin (`actor:agentsAdmin`) and reads the catalog (a read, not a catalog mutation). Keep the two admin scopes distinct — do not require provider-admin to *select* an already-governed entry.

[Source: 1-2-govern-provider-catalog-entries.md#Dev-Agent-Record; #Authorization-Guidance; Hexalith.Agents/src/Hexalith.Agents/ProviderCatalog/ProviderCatalogInspection.cs; Hexalith.Agents/src/Hexalith.Agents.Contracts/ProviderCatalog/{ProviderCatalogEntryView,ProviderModelStatus,ProviderConfigurationState,ProviderModelTimeoutPolicy,ProviderModelCapabilityFlags,ProviderCatalogInspectionResult,ProviderCatalogInspectionStatus}.cs]

### Provider Readiness → Verdict Mapping (orchestration, deterministic precedence)

| Catalog read / entry observation | `ProviderSelectionValidationStatus` |
|---|---|
| `GetEntry` returns `EntryNotFound` | `Missing` |
| `GetEntry` returns `NotAuthorized` (cross-tenant / not authorized) | `Unauthorized` (AC4 — never leak another tenant's records) |
| Read failed / unavailable / stale / transport error | `Unavailable` (fail closed — AD-12) |
| Entry found, `Status != ProviderModelStatus.Enabled` (Disabled/Degraded/Failed/Unknown) | `Disabled` |
| Entry found+enabled, `SupportsTextGeneration == false` | `NotTextGenerationCapable` |
| Entry found+enabled+text-gen, `ConfigurationState != Configured` | `NotConfigured` |
| Entry found+enabled+text-gen+configured, missing/invalid metadata (`ContextWindowTokenLimit <= 0`, `MaxOutputTokenLimit <= 0`, `MaxOutputTokenLimit > ContextWindowTokenLimit`, or invalid `TimeoutPolicy`) | `MissingCapabilityMetadata` |
| All of the above pass | `Valid` |
| (absent/unparseable verdict in the aggregate) | `Unknown` (aggregate fail-safe) |

Precedence is top-to-bottom (existence → authorization → availability → enabled → text-gen → configured → metadata → Valid). Story 1.2 validates metadata at write time, so `MissingCapabilityMetadata` is largely defensive, but the AC enumerates it ("lacks required context/output/timeout metadata") so the verdict vocabulary must include it. [Source: epics.md#Story-1.5 AC2; ARCHITECTURE-SPINE.md#AD-10-Provider-Capability-Floor; #AD-12]

### Readiness / UX Context (AC2, AC4)

- The canonical readiness state for a blocked provider is **`provider unavailable`** — "Provider/model missing, disabled, or failed" — treated with `{colors.status-severe}` (blocked, not a runtime failure) and surfaced by the `provider-status-badge` (enabled = `status-success`; disabled/unavailable = `status-severe`; failure = `status-danger`; degraded = `status-warning`; valid historical = `status-subtle`). This story contributes the `MissingProviderSelection` + `ProviderUnavailable` activation-blocker inputs and the `HasProviderSelection`/`SelectedProviderId`/`SelectedModelId` status-view fields the 1.8 overview/badge will consume. [Source: EXPERIENCE.md#agent-readiness; DESIGN.md#provider-status-badge; DESIGN.md#colors; DESIGN.md#agent-readiness-badge]
- `agent-readiness-badge` must combine lifecycle + Party identity + **provider/model readiness** + instruction validity + response mode + approver policy and "explain blockers, not hide them"; it uses `status-success` only when all gates pass. The `provider unavailable` umbrella state is explained by the precise `MissingProviderSelection`/`ProviderUnavailable` blockers. [Source: DESIGN.md#agent-readiness-badge; EXPERIENCE.md#agent-readiness]
- The selection happens inside the constrained `hexa` configuration form against the governed `provider-catalog-grid` (enabled state + capability metadata visible, secrets never exposed). UJ-1 step 4 ("She selects an enabled provider/model from the governed provider catalog") and its failure path ("no provider/model is enabled → activation blocked with a provider readiness reason") are exactly AC1/AC2. **No UI is built in this story** — only the contracts the later 1.8 overview/badge/form will consume. [Source: EXPERIENCE.md#uj-1---nora-configures-hexa-for-a-tenant-launch; DESIGN.md#provider-catalog-grid; EXPERIENCE.md#component-patterns; epics.md#Story-1.8]
- **Localization:** all provider/model names, readiness states, and denial reasons must be localizable **whole strings with named placeholders**, never runtime-assembled sentence fragments. The contracts this story adds are enum verdicts + safe ids/booleans (no prose) — keep it that way so 1.8 maps them to whole localized strings. [Source: DESIGN.md#typography; EXPERIENCE.md#voice-and-tone; ARCHITECTURE-SPINE.md#Consistency-Conventions (UI)]

### Sensitive-Data Handling (AD-9 / AD-14 / AC1)

- `ProviderId`/`ModelId` are stable safe identifiers and `ProviderCapabilityVersion` is a plain int — store and carry them freely on events/rejections/state/status (these are the AC1-sanctioned "safe Provider/model identity available for future Audit Evidence").
- Provider **secrets** (the secret *value*), `ConfigurationReferenceId`, raw provider payloads, provider SDK option types, capability-metadata blobs, and any `[PersonalData]`/credential field must never enter Agents events, state, status, logs, telemetry, or audit summaries. The orchestration reads the safe `ProviderCatalogEntryView` and surfaces only `{ verdict, ProviderId, ModelId, CapabilityVersion }` into the Agents side. [Source: ARCHITECTURE-SPINE.md#AD-9; #AD-14; prd.md#provider-safety; prd.md#functional-requirements-fr-24-capture-agent-audit-evidence; prd.md#non-goals]
- The Story 1.1 secret-non-disclosure guard (forbidden member tokens `Secret`/`ApiKey`/`Credential`/`Password`/`ConnectionString`) still applies — none are needed here; keep new field names clear of them (`ProviderCapabilityVersion`, `SelectedProviderId`, etc. are safe) and do not weaken the guard. [Source: 1-2-govern-provider-catalog-entries.md (secret guard); 1-4-link-hexa-to-a-party-identity.md#Sensitive-Data-Handling]

### Idempotency (AD-13)

- Re-selecting the same `(ProviderId, ModelId, ProviderCapabilityVersion)` via `SelectAgentProviderModel` is a deterministic `NoOp()` — no duplicate event, no version bump.
- A changed selection (different provider, model, or captured capability version) emits exactly one new `AgentProviderModelSelected` and bumps `ConfigurationVersion` by one. [Source: ARCHITECTURE-SPINE.md#AD-13-Idempotent-External-Effects]

### Latest Technical Information

- **No new references or NuGet packages this story.** The `ProviderCatalog` is in-module; the orchestration reads it through the in-module `ProviderCatalogInspection` (live binding deferred via `DeferredProviderCatalogReader`). NSubstitute `5.3.0` is already centralized for the orchestration unit tests. Do NOT add Agent Framework, provider SDK, Dapr-runtime, Parties, or UI packages. [Source: ARCHITECTURE-SPINE.md#Stack; Directory.Packages.props]
- Stack baseline unchanged: .NET `10.0.300`–`10.0.301`, `net10.0`, `.slnx`, Central Package Management, xUnit v3 `3.2.2`, Shouldly `4.3.0`, warnings-as-errors. [Source: ARCHITECTURE-SPINE.md#Stack]

### Testing Requirements

- Aggregate: select success (stores `ProviderId`/`ModelId`/`ProviderCapabilityVersion`, bumps `ConfigurationVersion`, lifecycle unchanged) / each non-`Valid` verdict → `AgentProviderModelSelectionRejected` (+ nothing stored + still blocks activation) / idempotent identical re-select → `NoOp` / changed selection → new event + bumped version + prior event preserved (AC3) / select on missing agent → `AgentNotFoundRejection` / unauthorized → `AgentAdministrationDeniedRejection` / verdict-parse fail-closed theory (`"1"`/`"01"`/`"valid"`/`" Valid"` → `Unknown`) / replay incl. persisted rejection / activation blocked-by-`MissingProviderSelection`, blocked-by-`ProviderUnavailable`, and unblocked when selection present + `Valid` verdict.
- ProviderCatalog: `CapabilityVersion` = 1 after create, +1 per metadata update, unchanged by enable/disable; mapped onto `ProviderCatalogEntryView`.
- Contracts: marker interfaces present; JSON round-trip for the new events/rejection + extended `AgentStatusView` + extended `ProviderCatalogEntryView`; `ProviderSelectionValidationStatus` + `MissingProviderSelection`/`ProviderUnavailable` serialize by name; unknown → sentinel; new members are id/version-shaped only (no secret/credential/config-reference member); secret guard green.
- Server: orchestration verdict mapping (incl. `EntryNotFound→Missing`, `NotAuthorized→Unauthorized`, read-failure→`Unavailable`, `Disabled`, `NotTextGenerationCapable`, `NotConfigured`, `MissingCapabilityMetadata`, all-pass `Valid`) via substituted `IProviderCatalogReader`; captured capability version equals entry's; orchestration authorizes + sets server-populated `provider:selectionValidation`/`actor:agentsAdmin` (stripping client-supplied) + dispatches the right command/verdict; deferred reader placeholder behaves; structural/boundary guards still green.
- Build/test commands (run from `Hexalith.Agents/`):
  - `dotnet restore Hexalith.Agents.slnx`
  - `dotnet build Hexalith.Agents.slnx --configuration Release` (0 warnings / 0 errors, warnings-as-errors)
  - `dotnet test tests/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj --configuration Release`
  - `dotnet test tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release`
  - `dotnet test tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj --configuration Release`

### Project Structure Notes

- New/changed code:
  - `Hexalith.Agents.Contracts/Agent/`: `ProviderSelectionValidationStatus.cs`; `Commands/SelectAgentProviderModel.cs`; `Events/AgentProviderModelSelected.cs`; `Events/Rejections/AgentProviderModelSelectionRejected.cs`; edits to `AgentActivationBlocker.cs` (+`MissingProviderSelection`, +`ProviderUnavailable`) and `AgentStatusView.cs` (+`HasProviderSelection`, +`SelectedProviderId`, +`SelectedModelId`).
  - `Hexalith.Agents.Contracts/ProviderCatalog/`: edit `ProviderCatalogEntryView.cs` (+`CapabilityVersion` trailing param).
  - `Hexalith.Agents/Agent/`: edits to `AgentState.cs` (+`ProviderId`/`ModelId`/`ProviderCapabilityVersion`, +2 `Apply`), `AgentAggregate.cs` (+`SelectAgentProviderModel` handler, +`ReadProviderSelectionValidation`, +provider gate in `Handle(ActivateAgent)`), `AgentConfigurationPolicy.cs` (+provider params/blockers), `AgentInspection.cs` (+view fields).
  - `Hexalith.Agents/ProviderCatalog/`: edits to `ProviderModelEntryState.cs` (+`CapabilityVersion` counter in `Apply`), `ProviderCatalogInspection.cs` (+map `CapabilityVersion` onto the view).
  - `Hexalith.Agents.Server/`: `Ports/{IProviderCatalogReader,ProviderCatalogEntryReadResult,DeferredProviderCatalogReader}.cs`; `Application/Agents/AgentProviderSelectionOrchestrator.cs` (+activation re-validation step/seam); edits to `Program.cs`.
  - Tests across `Hexalith.Agents.Tests` (Agent + ProviderCatalog), `Hexalith.Agents.Contracts.Tests`, `Hexalith.Agents.Server.Tests`, including the Story 1.3/1.4 activation-test updates for the new provider gate.
- Discovery loaded: root `epics.md` (Epic 1, Story 1.5 + cross-stories), PRD (FR-5/FR-4/FR-10/FR-19/FR-20/FR-21/FR-24 + Provider-Safety/Auditability NFRs + non-goals), architecture spine (AD-2/AD-3/AD-4/AD-9/AD-10/AD-12/AD-13/AD-14/AD-15/AD-17), UX DESIGN/EXPERIENCE (readiness states, provider badges, catalog grid, localization, UJ-1), the as-built Story 1.1–1.4 Agent module + Story 1.2 ProviderCatalog code. No root `project-context.md` exists for `agents`; sibling-module `project-context.md` files supply carry-forward rules (aggregate purity, no-`UtcNow`-in-aggregates, FrontComposer/Fluent inherited UI, deferred DAPR read-path bindings).

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.5-Select-Provider-And-Model-For-hexa]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#functional-requirements-fr-5-select-provider-and-model-per-agent]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#functional-requirements-fr-4-manage-global-providers-aggregate]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#functional-requirements-fr-10-handle-generation-failure]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#functional-requirements-fr-19-enforce-tenant-isolation]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#functional-requirements-fr-20-enforce-role-and-policy-authorization]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#functional-requirements-fr-21-fail-closed-on-dependency-uncertainty]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#functional-requirements-fr-24-capture-agent-audit-evidence]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#provider-safety]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#auditability]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#non-goals]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-2-Aggregate-Boundaries]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-3-Pure-Aggregates-Side-Effects-Outside]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-4-Interaction-Snapshot]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-9-Provider-Adapter-And-Catalog-Boundary]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-10-Provider-Capability-Floor]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-12-Authorization-And-Dependency-Uncertainty]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-13-Idempotent-External-Effects]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-14-Sensitive-Content-And-Secret-Safety]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#agent-readiness]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#provider-and-model]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#uj-1---nora-configures-hexa-for-a-tenant-launch]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md#agent-readiness-badge]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md#provider-status-badge]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md#provider-catalog-grid]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md#typography]
- [Source: _bmad-output/implementation-artifacts/1-2-govern-provider-catalog-entries.md#Dev-Agent-Record]
- [Source: _bmad-output/implementation-artifacts/1-4-link-hexa-to-a-party-identity.md#Design-Where-Each-Responsibility-Lives]
- [Source: Hexalith.Agents/src/Hexalith.Agents/Agent/AgentAggregate.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents/Agent/AgentState.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents/Agent/AgentConfigurationPolicy.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents/Agent/AgentInspection.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/PartyLinkValidationStatus.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/AgentActivationBlocker.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/AgentStatusView.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents/ProviderCatalog/ProviderCatalogInspection.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents/ProviderCatalog/ProviderModelEntryState.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/ProviderCatalog/ProviderCatalogEntryView.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Application/Agents/AgentPartyIdentityOrchestrator.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Ports/DeferredAgentCommandDispatcher.cs]

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created. Story status is `ready-for-dev`.

## Dev Agent Record

### Agent Model Used

claude-opus-4-8[1m] (Opus 4.8, 1M context) — BMAD dev-story workflow.

### Debug Log References

Build/test (run from `Hexalith.Agents/`):

- `dotnet restore Hexalith.Agents.slnx` — up to date.
- `dotnet build Hexalith.Agents.slnx --configuration Release` — Build succeeded, **0 Warning(s) / 0 Error(s)** (warnings-as-errors enforced).
- `dotnet test tests/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj --configuration Release` — Passed: **180**, Failed: 0.
- `dotnet test tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release` — Passed: **60**, Failed: 0.
- `dotnet test tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj --configuration Release` — Passed: **67**, Failed: 0.

### Completion Notes List

- **Task 1 (Contracts):** Added `ProviderSelectionValidationStatus` (`Unknown=0` fail-safe sentinel + the 8 verdicts), the `SelectAgentProviderModel` command (safe ids + capability version only — verdict lives in the trusted extension, not the payload), the durable `AgentProviderModelSelected` event (the AC1 Audit Evidence), and the typed `AgentProviderModelSelectionRejected` rejection. Reused the existing `AgentNotFoundRejection`/`AgentAdministrationDeniedRejection` (no duplicates). Appended `MissingProviderSelection=5`/`ProviderUnavailable=6` to `AgentActivationBlocker` (existing ordinals preserved) and appended `HasProviderSelection`/`SelectedProviderId`/`SelectedModelId` to `AgentStatusView` with `ActivationBlockers` kept last. Contracts stay inward-only; secret-token guard untouched.
- **Task 2 (AgentState):** Added nullable `ProviderId`/`ModelId`/`ProviderCapabilityVersion` durable fields, an `Apply(AgentProviderModelSelected)` (records ids + version, bumps `ConfigurationVersion`, `IsCreated`-guarded) and a no-op `Apply(AgentProviderModelSelectionRejected)` so replay stays total.
- **Task 3 (Aggregate handler + activation gate):** Added the `provider:selectionValidation` server-populated extension key, the exact-name case-sensitive `ReadProviderSelectionValidation` helper (numeric/aliased/cased input fails closed to `Unknown`), and `Handle(SelectAgentProviderModel)` following the `LinkAgentPartyIdentity` guard cascade (admin → not-found → non-`Valid` verdict rejects → idempotent re-select `NoOp` → new/changed selection bumps version). Extended `Handle(ActivateAgent)` to feed provider readiness into the gate (a direct activation with a present selection but no trusted verdict fails closed with `ProviderUnavailable`). Aggregate stays pure — no catalog/provider/clock/IO access.
- **Task 4 (Policy + Inspection):** `ComputeActivationBlockers` now takes `hasProviderSelection`/`selectedProviderReady` and appends `MissingProviderSelection`/`ProviderUnavailable` in deterministic order (display name → instructions → party → provider selection → provider unavailable). `AgentInspection.ToView` surfaces the new view fields and passes `selectedProviderReady: true` (pure read — trusts the last-validated recorded selection; live `ProviderUnavailable` comes from the activation path / 1.8). Documented the no-catalog-read boundary in a code comment.
- **Task 5 (ProviderCatalog capability version):** Added a replay-derived `CapabilityVersion` counter on `ProviderModelEntryState` (1 at create, +1 per `ProviderModelEntryMetadataUpdated`, unchanged by enable/disable) — derived in `Apply`, no event payload reshaped. Exposed it as the trailing `ProviderCatalogEntryView.CapabilityVersion` parameter and mapped it in `ProviderCatalogInspection.ToView`.
- **Task 6 (Server port + orchestration):** Added the `IProviderCatalogReader` port + `ProviderCatalogEntryReadResult` (server-only types), the `DeferredProviderCatalogReader` placeholder (throws until the read-model story wires it), the shared `ProviderSelectionVerdict` evaluator (deterministic precedence: existence → authorization → availability → enabled → text-gen → configured → metadata → `Valid`), the `AgentProviderSelectionOrchestrator` (authorizes, reads the catalog, computes the verdict, captures the capability version, strips client-supplied reserved keys, dispatches with server-populated `actor:agentsAdmin`/`provider:selectionValidation`), and the minimal `AgentActivationProviderRevalidation` step for the AC2 "or activate" path. Registered all three in `Program.cs`. **Deferred (note):** live `IProviderCatalogReader`/`IAgentCommandDispatcher` bindings to the DAPR/EventStore read-model + command gateway and a runnable AppHost topology — mirroring Stories 1.2/1.3/1.4. No provider SDK / sibling-module reference added.
- **Task 7 (Tests):** New `AgentProviderSelectionTests` (select success/idempotent/changed/non-`Valid` fail-closed + verdict-parse regression theory + replay + activation gate blocked/unblocked), `AgentProviderSelectionContractsTests`, `AgentProviderSelectionOrchestratorTests` (verdict-mapping facts via substituted `IProviderCatalogReader`, trust-model stripping, deferred reader, activation re-validation). Extended `ProviderCatalogStateReplayTests`/`ProviderCatalogInspectionTests` for `CapabilityVersion`, the contracts round-trip suites for the extended `AgentStatusView`/`ProviderCatalogEntryView`. Updated Story 1.3/1.4 activation tests (`AgentAggregateTests`, `AgentConfigurationValidationTests`, `AgentPartyIdentityTests`, `AgentLifecycleE2ETests`, `AgentInspectionTests`) and the shared `AgentTestData` (added `SelectEnvelope`, `StateWithSelectedProvider`, the new `ApplyAll` cases) to select a provider + supply the `Valid` verdict before activating — the correct consequence of the new gate. All structural/boundary/secret guards stay green.

### File List

**New — production (`Hexalith.Agents.Contracts`):**

- `src/Hexalith.Agents.Contracts/Agent/ProviderSelectionValidationStatus.cs`
- `src/Hexalith.Agents.Contracts/Agent/Commands/SelectAgentProviderModel.cs`
- `src/Hexalith.Agents.Contracts/Agent/Events/AgentProviderModelSelected.cs`
- `src/Hexalith.Agents.Contracts/Agent/Events/Rejections/AgentProviderModelSelectionRejected.cs`

**New — production (`Hexalith.Agents.Server`):**

- `src/Hexalith.Agents.Server/Ports/IProviderCatalogReader.cs`
- `src/Hexalith.Agents.Server/Ports/ProviderCatalogEntryReadResult.cs`
- `src/Hexalith.Agents.Server/Ports/DeferredProviderCatalogReader.cs`
- `src/Hexalith.Agents.Server/Application/Agents/ProviderSelectionVerdict.cs`
- `src/Hexalith.Agents.Server/Application/Agents/AgentProviderSelectionRequest.cs`
- `src/Hexalith.Agents.Server/Application/Agents/AgentProviderSelectionOutcome.cs`
- `src/Hexalith.Agents.Server/Application/Agents/AgentProviderSelectionOrchestrator.cs`
- `src/Hexalith.Agents.Server/Application/Agents/AgentActivationRevalidationRequest.cs` (request + outcome)
- `src/Hexalith.Agents.Server/Application/Agents/AgentActivationProviderRevalidation.cs`

**Modified — production:**

- `src/Hexalith.Agents.Contracts/Agent/AgentActivationBlocker.cs` (+`MissingProviderSelection`, +`ProviderUnavailable`)
- `src/Hexalith.Agents.Contracts/Agent/AgentStatusView.cs` (+`HasProviderSelection`, +`SelectedProviderId`, +`SelectedModelId`)
- `src/Hexalith.Agents.Contracts/ProviderCatalog/ProviderCatalogEntryView.cs` (+`CapabilityVersion`)
- `src/Hexalith.Agents/Agent/AgentAggregate.cs` (+`SelectAgentProviderModel` handler, +`ReadProviderSelectionValidation`, +provider gate in `Handle(ActivateAgent)`)
- `src/Hexalith.Agents/Agent/AgentState.cs` (+provider fields, +2 `Apply`)
- `src/Hexalith.Agents/Agent/AgentConfigurationPolicy.cs` (+provider params/blockers)
- `src/Hexalith.Agents/Agent/AgentInspection.cs` (+view fields, +provider gate inputs)
- `src/Hexalith.Agents/ProviderCatalog/ProviderModelEntryState.cs` (+`CapabilityVersion`)
- `src/Hexalith.Agents/ProviderCatalog/ProviderCatalogState.cs` (+`CapabilityVersion` counter in `Apply`)
- `src/Hexalith.Agents/ProviderCatalog/ProviderCatalogInspection.cs` (+map `CapabilityVersion` onto the view)
- `src/Hexalith.Agents.Server/Program.cs` (register reader + selection + activation-revalidation)

**New — tests:**

- `tests/Hexalith.Agents.Tests/AgentProviderSelectionTests.cs`
- `tests/Hexalith.Agents.Contracts.Tests/AgentProviderSelectionContractsTests.cs`
- `tests/Hexalith.Agents.Server.Tests/AgentProviderSelectionOrchestratorTests.cs`

**Modified — tests:**

- `tests/Hexalith.Agents.Tests/AgentTestData.cs` (+`SelectEnvelope`, +`StateWithSelectedProvider`, +`ApplyAll` cases, +constants)
- `tests/Hexalith.Agents.Tests/AgentAggregateTests.cs`
- `tests/Hexalith.Agents.Tests/AgentConfigurationValidationTests.cs`
- `tests/Hexalith.Agents.Tests/AgentPartyIdentityTests.cs`
- `tests/Hexalith.Agents.Tests/AgentLifecycleE2ETests.cs`
- `tests/Hexalith.Agents.Tests/AgentInspectionTests.cs`
- `tests/Hexalith.Agents.Tests/ProviderCatalogStateReplayTests.cs`
- `tests/Hexalith.Agents.Tests/ProviderCatalogInspectionTests.cs`
- `tests/Hexalith.Agents.Contracts.Tests/AgentContractsRoundTripTests.cs`
- `tests/Hexalith.Agents.Contracts.Tests/ProviderCatalogContractsRoundTripTests.cs`

## Senior Developer Review (AI)

**Reviewer:** Administrator — adversarial code review (story-automator) on 2026-06-24.

**Outcome:** ✅ Approved → status `done`. **0 Critical / 0 High / 0 Medium / 1 Low** found. The Low was auto-fixed; no production code changes were required.

**Scope reviewed:** all 4 new contracts, the 3 modified contracts, the pure `Agent` aggregate handler + activation provider gate, `AgentState` replay, `AgentConfigurationPolicy`/`AgentInspection`, the replay-derived `ProviderCatalog` `CapabilityVersion`, all 9 new Server files (port + verdict evaluator + selection/activation orchestrations + deferred reader) and `Program.cs`, plus all new and modified test suites. Verified against git reality (`git status`/`diff`) — the story File List matches the actual changed source files exactly.

**Verification performed (claims validated, not trusted):**
- `dotnet build Hexalith.Agents.slnx --configuration Release` → **0 warnings / 0 errors** (warnings-as-errors). ✔
- Domain **180**, Contracts **60**, Server **67** = **307 passing / 0 failing**. ✔
- **AC1 (audit-ready selection):** `AgentProviderModelSelected` records `ProviderId`/`ModelId`/`ProviderCapabilityVersion`/`ConfigurationVersion`; capability version is sourced from the new replay-derived `ProviderCatalogEntryView.CapabilityVersion`. Verified end-to-end. ✔
- **AC2 (fail closed, no SDK/credential path):** the aggregate rejects on any non-`Valid`/absent verdict and makes no catalog/provider call; verdict-parse hardening rejects `"1"`/`"01"`/`"valid"`/`" Valid"` to `Unknown`; activation re-validation gate blocks with `MissingProviderSelection`/`ProviderUnavailable`. Verified. ✔
- **AC3 (future-only, append-only):** changed selection emits a new event + bumps `ConfigurationVersion`; prior events unchanged; identical re-select is a deterministic `NoOp` (full provider/model/version triple). Verified. ✔
- **AC4 (fail closed cross-tenant):** read `NotAuthorized → Unauthorized`; status read returns `NotAuthorized` for non-admins; isolation is structural (tenant-scoped aggregate + catalog read). Verified. ✔
- **Trust model:** reserved `actor:agentsAdmin` / `provider:selectionValidation` extensions are server-populated and client-stripped in both orchestrations; forged `Valid` verdicts are overwritten by the trusted verdict (tested on both the select and activate paths). ✔
- **Secret safety:** the provider surface exposes only id/version/verdict-shaped members; `ContractsSecretNonDisclosureTests` and the new provider-surface guard stay green. ✔

**Findings:**
- 🟢 **[Low — Documentation] Stale test counts (auto-fixed).** The Debug Log References and Change Log v1.0 entry reported 298 tests (178 + 60 + 60), but the QA gap-fill tests added afterward bring the actual total to **307** (180 + 60 + 67). Corrected the Debug Log counts; the discrepancy is favorable (more coverage) and non-blocking.

No action items remain; the story is complete and verified.

## Change Log

| Date       | Version | Description                                                                 | Author |
| ---------- | ------- | --------------------------------------------------------------------------- | ------ |
| 2026-06-24 | 0.1     | Created Story 1.5 context (Select Provider And Model For `hexa`): provider-selection contracts + pure `Agent` aggregate handler with trusted `provider:selectionValidation` verdict (fail-closed), `AgentState` provider/model/capability-version replay, `MissingProviderSelection`/`ProviderUnavailable` readiness gates, an additive replay-derived `CapabilityVersion` on the `ProviderCatalog` entry, and the `IProviderCatalogReader` port + `AgentProviderSelectionOrchestrator` (server-populated, client-stripped extensions; live catalog/dispatch binding deferred). Status → ready-for-dev. | Bob (Scrum Master) |
| 2026-06-24 | 1.1     | Adversarial code review (story-automator): verified all 4 ACs and all 7 tasks against the implementation and git reality; re-ran build (0 warnings/0 errors) and the full suite (307 passing: 180 domain + 60 contracts + 67 server). 0 Critical/High/Medium findings; 1 Low (stale test counts in the Debug Log) auto-fixed. No production code changes required. Status → done. | Administrator (Review) |
| 2026-06-24 | 1.0     | Implemented Story 1.5 (all 7 tasks): provider-selection contracts (`ProviderSelectionValidationStatus`, `SelectAgentProviderModel`, `AgentProviderModelSelected`, `AgentProviderModelSelectionRejected`, +2 activation blockers, +3 status-view fields); pure aggregate `Handle(SelectAgentProviderModel)` with trusted fail-closed `provider:selectionValidation` verdict + provider gate in `Handle(ActivateAgent)`; `AgentState` provider/model/capability-version replay; `ComputeActivationBlockers`/`AgentInspection` provider readiness wiring; replay-derived `ProviderCatalog` `CapabilityVersion`; Server `IProviderCatalogReader` port + `DeferredProviderCatalogReader` + `AgentProviderSelectionOrchestrator` + `AgentActivationProviderRevalidation` (live read-model/dispatch binding deferred). Build: 0 warnings / 0 errors. Tests: 298 passing (178 domain + 60 contracts + 60 server). Status → review. | Amelia (Dev) |
