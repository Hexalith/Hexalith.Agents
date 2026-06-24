---
baseline_commit: c01d8c44746e7b50ac320d36308dac96316dbe3b
---

# Story 1.4: Link `hexa` To A Party Identity

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an Agent Administrator,
I want to link or provision `hexa` with exactly one Party identity,
so that future Agent responses are attributable to a known AI participant rather than a caller or generic system account.

## Acceptance Criteria

**AC1 - Store only a stable `PartyId` reference; never Party PII**
**Given** `hexa` exists for a tenant
**When** an authorized administrator links an existing Party identity or provisions a new Agent Party identity through the Parties adapter
**Then** `hexa` stores only the stable `PartyId` reference
**And** no Party display names, contact values, personal identifiers, or Parties personal-data objects are persisted in Agents durable events.

**AC2 - Reject linking on missing/disabled/ambiguous/unavailable/unauthorized Party state (fail closed)**
**Given** Party validation returns missing, disabled, ambiguous, unavailable, or unauthorized state
**When** the administrator attempts to link that Party identity
**Then** the link is rejected
**And** `hexa` remains not callable for posting-dependent workflows.

**AC3 - Exactly one active Party identity (replacement is explicit)**
**Given** `hexa` already has a linked Party identity
**When** an administrator attempts to link a second active Party identity
**Then** the system rejects the operation or requires an explicit replacement command
**And** the Agent can never have more than one active Party identity.

**AC4 - Party identity is a distinct readiness gate; status exposes presence without PII**
**Given** Party identity linking succeeds
**When** Agent readiness is evaluated
**Then** readiness includes Party identity state as a distinct gate
**And** audit/status output identifies the presence of a valid Party reference without exposing personal Party data.

[Source: _bmad-output/planning-artifacts/epics.md#Story-1.4-Link-hexa-To-A-Party-Identity; _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-2-Link-Agent-To-Party-Identity; _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-7-Agent-Party-Identity-And-Membership]

## Tasks / Subtasks

- [x] **Task 1 - Add the Party-identity public contracts to `Hexalith.Agents.Contracts/Agent/`** (AC: #1, #2, #3, #4)
  - [x] Add a safe validation-verdict enum `PartyLinkValidationStatus` under `Hexalith.Agents.Contracts/Agent/` with values `Unknown = 0, Valid, Missing, Disabled, Ambiguous, Unavailable, Unauthorized`. Decorate it `[JsonConverter(typeof(JsonStringEnumConverter))]` and document `Unknown` as the fail-safe sentinel — mirror the exact shape/XML-doc style of `AgentActivationBlocker.cs`. This enum classifies *which* dependency state blocked a link; it carries no PII and no Parties types.
  - [x] Add aggregate commands under `Agent/Commands/`: `LinkAgentPartyIdentity` (first link) and `ReplaceAgentPartyIdentity` (explicit swap). **The command payload carries only the stable `PartyId` string** (plus any existing base command members) — it does NOT carry the verdict (see Task 4 trust model) and never any Party display name/contact/personal data.
  - [x] Add success events under `Agent/Events/` (each `: IEventPayload`, no wall-clock fields): `AgentPartyIdentityLinked(string AgentId, string PartyId, int ConfigurationVersion)` and `AgentPartyIdentityReplaced(string AgentId, string? PreviousPartyId, string PartyId, int ConfigurationVersion)`. `PartyId`/`PreviousPartyId` are stable Parties-owned identifiers — **not PII** — so storing them is the AC1-sanctioned "stable reference only".
  - [x] Add typed rejections under `Agent/Events/Rejections/` (each `: IRejectionEvent`): `AgentPartyIdentityLinkRejected(string AgentId, PartyLinkValidationStatus Status)` (dependency-validation failure — AC2) and `AgentPartyIdentityAlreadyLinkedRejection(string AgentId, string AttemptedPartyId)` (second distinct link without replacement — AC3). Reuse the existing `AgentNotFoundRejection` and `AgentAdministrationDeniedRejection` from Story 1.3 — do NOT duplicate them.
  - [x] Extend `AgentActivationBlocker` by **appending** `MissingPartyIdentity` after `InvalidInstructions` (preserve existing ordinals; `Unknown = 0` stays). Do not reorder/renumber — the enum is additively extensible by design.
  - [x] Extend `AgentStatusView` with a `bool HasPartyIdentity` parameter (presence only — AC4). Do NOT add the raw `PartyId` to the badge view if it would be rendered in accessible names; presence is sufficient for `agent-readiness-badge`. (The `PartyId` itself is a safe id and may be surfaced through the configuration query if a later task needs it, but the readiness/status view needs only presence.)
  - [x] Keep `Hexalith.Agents.Contracts` inward-only: it must reference nothing outward except the EventStore **contracts** marker already wired (no `Hexalith.Parties.*`, no provider SDK, no Dapr, no server infra). `PartyId` is a plain `string` here — there is no `PartyId` value type in Parties to import (see Dev Notes › Parties Adapter Facts).

- [x] **Task 2 - Extend the pure `Agent` aggregate replay state** (AC: #1, #3, #4)
  - [x] In `Hexalith.Agents/Agent/AgentState.cs` add `public string? PartyId { get; set; }` (null = no linked identity). This is the ONLY new durable field — store the id reference, nothing else (AC1).
  - [x] Add `Apply(AgentPartyIdentityLinked e)` → set `PartyId = e.PartyId`, `ConfigurationVersion = e.ConfigurationVersion` (guarded by `if (!IsCreated) return;` like the other update applies).
  - [x] Add `Apply(AgentPartyIdentityReplaced e)` → set `PartyId = e.PartyId`, `ConfigurationVersion = e.ConfigurationVersion` (same `IsCreated` guard).
  - [x] Add **no-op** `Apply(...)` overloads for both new rejection events (`AgentPartyIdentityLinkRejected`, `AgentPartyIdentityAlreadyLinkedRejection`) calling `MarkReplayOnlyEventHandled()` — replay stays total exactly like the Story 1.3 rejections.

- [x] **Task 3 - Implement the pure aggregate link/replace handlers** (AC: #1, #2, #3)
  - [x] In `Hexalith.Agents/Agent/AgentAggregate.cs` add `Handle(LinkAgentPartyIdentity, AgentState?, CommandEnvelope)` and `Handle(ReplaceAgentPartyIdentity, AgentState?, CommandEnvelope)` following the exact guard cascade of the existing handlers: `ArgumentNullException.ThrowIfNull`, `IsAgentAdmin(envelope)` → `Denied(...)`, then `state is null || !state.IsCreated` → `AgentNotFoundRejection`.
  - [x] **Read the trusted validation verdict from a server-populated envelope extension** (see Task 4 trust model). Add a private const `PartyLinkValidationExtensionKey = "party:linkValidation"` and a helper that parses the extension value into `PartyLinkValidationStatus` (defaulting to `Unknown` when absent/unparseable — fail closed). Use the same `envelope.Extensions?.TryGetValue(...)` access pattern as `IsAgentAdmin`.
  - [x] **AC2 (fail closed):** if the parsed verdict is anything other than `Valid`, emit `AgentPartyIdentityLinkRejected(agentId, status)` and change no state. This single check covers Missing/Disabled/Ambiguous/Unavailable/Unauthorized/Unknown — a direct client command that never went through the orchestration has no trusted verdict and is rejected here.
  - [x] **`LinkAgentPartyIdentity` (valid verdict):** if `state.PartyId == command.PartyId` → `DomainResult.NoOp()` (idempotent re-link); else if `state.PartyId is not null` (a *different* identity already linked) → `AgentPartyIdentityAlreadyLinkedRejection(agentId, command.PartyId)` (AC3 — caller must use replace); else emit `AgentPartyIdentityLinked(agentId, command.PartyId, state.ConfigurationVersion + 1)`.
  - [x] **`ReplaceAgentPartyIdentity` (valid verdict):** if `state.PartyId == command.PartyId` → `DomainResult.NoOp()`; else emit `AgentPartyIdentityReplaced(agentId, state.PartyId /* nullable previous */, command.PartyId, state.ConfigurationVersion + 1)`. Replace deterministically sets the single active identity (AC3 holds: there is always at most one `PartyId`).
  - [x] Linking/replacing is a configuration change → bump `ConfigurationVersion` on the success event (needed for the AD-4 interaction snapshot later). Do NOT change `Lifecycle` here — preserve the Story 1.3 "lifecycle ≠ callability" invariant; readiness is surfaced through the blocker, not by auto-activating/demoting.
  - [x] Keep the aggregate pure (AD-3): no Parties client, no I/O, no `DateTimeOffset.UtcNow`, no `Guid.NewGuid`, no state mutation outside `Apply`. The verdict is plain data fed in through the envelope; the aggregate never calls Parties.

- [x] **Task 4 - Party validation/provisioning adapter + application orchestration in `Hexalith.Agents.Server`** (AC: #1, #2, #3)
  - [x] Add the Parties sibling-source references to **`Hexalith.Agents.Server` only** (never Contracts/Client/domain): `$(HexalithPartiesRoot)\src\Hexalith.Parties.Client\Hexalith.Parties.Client.csproj` and `...\Hexalith.Parties.Contracts\Hexalith.Parties.Contracts.csproj`. The `$(HexalithPartiesRoot)` discovery block already exists in `Directory.Build.props` — use it; never a NuGet `PackageReference`.
  - [x] Define a port in `Hexalith.Agents.Server/Ports/` — e.g. `IAgentPartyDirectory` — with `Task<AgentPartyValidationResult> ValidateExistingPartyAsync(string tenantId, string partyId, CancellationToken ct)` and `Task<AgentPartyValidationResult> ProvisionAgentPartyAsync(string tenantId, AgentPartyProvisioningRequest request, CancellationToken ct)`. `AgentPartyValidationResult` is a server-internal type carrying `PartyLinkValidationStatus Status` + the validated/created `string? PartyId` (no PII). These port types are NOT public contracts — keep them in the Server project.
  - [x] Implement the adapter (e.g. `PartiesAgentPartyDirectory : IAgentPartyDirectory`) in `Hexalith.Agents.Server/Ports/` over `IPartiesQueryClient` / `IPartiesCommandClient`. **Validate existing:** call `GetPartyAsync(partyId, ct)` and map `PartyDetail` → verdict (table in Dev Notes › Parties State → Verdict Mapping): active+current → `Valid`; `IsActive == false` or `IsErased == true` or `IsRestricted == true` → `Disabled`; not-found → `Missing`; `Freshness.Status != Current` → `Unavailable` (fail closed, do NOT treat stale as fresh); wrong-tenant / unauthorized → `Unauthorized`; unexpected/transport failure → `Unavailable`. Never surface the `PartyDetail` PII fields (`DisplayName`, `SortName`, `PersonDetails`, contact channels) out of the adapter.
  - [x] **Provision new:** build `CreateParty { PartyId = <deterministic id derived from AgentId>, Type = PartyType.Organization, OrganizationDetails = <minimal non-personal label> }` and call `CreatePartyWithResultAsync(...)`. Use `PartyType.Organization` (there is **no `AiAgent`/`Bot`/`System` PartyType** — see Dev Notes) to avoid person PII. Derive the new `PartyId` deterministically from `AgentId` so a retried provision is idempotent (AD-13) and does not create duplicate Parties. Return only the new `PartyId` + `Valid` into the Agents side — the agent name/label lives in Parties, never in Agents durable events (AC1).
  - [x] Add the application orchestration in `Hexalith.Agents.Server/Application/Agents/` (e.g. a `LinkAgentPartyIdentityHandler`/service) that: (1) authorizes the actor as an Agents admin and strips any client-supplied reserved extensions; (2) calls the port (validate-existing or provision-new); (3) constructs the aggregate command (`LinkAgentPartyIdentity` / `ReplaceAgentPartyIdentity`) carrying the `PartyId`, with the **server-populated** trusted extensions `actor:agentsAdmin=true` and `party:linkValidation=<Status>`; (4) dispatches it through the EventStore command path. Always feed the computed verdict (even non-`Valid`) so the aggregate records an auditable `AgentPartyIdentityLinkRejected` — the aggregate's independent non-`Valid` rejection (Task 3) is the security guarantee against direct-gateway calls.
  - [x] **Trust model (CRITICAL):** `party:linkValidation` is server-populated ONLY, exactly like `actor:agentsAdmin`. The command entry point must strip both keys from any client-supplied payload/extensions and repopulate them from the orchestration's port result / trusted claims. A client must never be able to assert `party:linkValidation=Valid` to bypass Parties validation.
  - [x] Register `services.AddPartiesClient(configuration)` plus the port adapter and orchestration in `Hexalith.Agents.Server/Program.cs`. Binding the orchestration's command dispatch to the live DAPR/EventStore gateway and a runnable AppHost topology is **deferred** (mirroring Story 1.2/1.3 deferring the DAPR read-model binding) — keep the adapter/orchestration decision logic pure and fully unit-testable here. If full DI wiring would require a runnable host this story does not stand up, register what compiles cleanly and note the deferred runtime binding in a completion note.

- [x] **Task 5 - Wire the Party-identity readiness gate** (AC: #4)
  - [x] Change `AgentConfigurationPolicy.ComputeActivationBlockers` to accept party presence — `ComputeActivationBlockers(string displayName, string instructions, bool hasPartyIdentity)` — and append `AgentActivationBlocker.MissingPartyIdentity` when `!hasPartyIdentity`. Keep the deterministic order: display name → instructions → party identity.
  - [x] Update both callers: `AgentAggregate.Handle(ActivateAgent, ...)` passes `state.PartyId is not null`; `AgentInspection.ToView` passes `state.PartyId is not null` and sets the new `HasPartyIdentity` view field. Activation now fails closed with `MissingPartyIdentity` until a valid Party is linked (AC2/AC4) and succeeds once linked (other gates permitting).
  - [x] Confirm the readiness contract still keeps lifecycle and blockers distinct (Story 1.3 invariant; UX `agent-readiness-badge`): a linked Party clears `MissingPartyIdentity` but does not by itself make a `Draft`/`Disabled` agent `Active`.

- [x] **Task 6 - Tests + narrow verification** (AC: #1, #2, #3, #4)
  - [x] **Domain (`tests/Hexalith.Agents.Tests`):** extend `AgentTestData` with a link-validation envelope helper (sets `party:linkValidation`) and a `StateWithLinkedParty(...)` helper; add the new event/rejection cases to the `ApplyAll` dispatch switch. Aggregate tests: link success (stores `PartyId`, bumps `ConfigurationVersion`, lifecycle unchanged); link with each non-`Valid` verdict → `AgentPartyIdentityLinkRejected` carrying that status AND `PartyId` not stored AND `MissingPartyIdentity` still blocks activation (AC2); re-link same `PartyId` → `NoOp` (idempotent); link a *different* `PartyId` while already linked → `AgentPartyIdentityAlreadyLinkedRejection` (AC3); replace success → `AgentPartyIdentityReplaced` (PreviousPartyId + new id, still exactly one identity); replace same id → `NoOp`; link/replace on missing agent → `AgentNotFoundRejection`; unauthorized link/replace → `AgentAdministrationDeniedRejection` (AC); replay/rehydration through `Apply` including the new persisted rejections; activation blocked-by-`MissingPartyIdentity` then unblocked after a valid link (AC4). Add an end-to-end `ProcessAndApplyAsync` journey mirroring `AgentLifecycleE2ETests`.
  - [x] **Contracts (`tests/Hexalith.Agents.Contracts.Tests`):** new events/rejections implement the expected EventStore marker interfaces; System.Text.Json round-trip for each new event/rejection + the extended `AgentStatusView`; `PartyLinkValidationStatus` and the new `MissingPartyIdentity` blocker serialize by name and unknown input deserializes to the `Unknown` fail-safe; assert the party events/rejections expose only id-shaped fields (`PartyId`/`PreviousPartyId`/`AttemptedPartyId`) and no display-name/contact/personal-data member; the Story 1.1 secret-non-disclosure guard stays green and unweakened.
  - [x] **Server (`tests/Hexalith.Agents.Server.Tests`):** adapter mapping tests using a substituted `IPartiesQueryClient`/`IPartiesCommandClient` (NSubstitute `5.3.0` is already centralized) — assert each `PartyDetail` state maps to the correct verdict per the mapping table, including `Freshness.Status != Current → Unavailable` and not-found → `Missing`; provisioning builds a `CreateParty` with `PartyType.Organization`, a deterministic `PartyId` derived from `AgentId`, and surfaces no PII back into Agents; orchestration authorizes, sets the server-populated `party:linkValidation`/`actor:agentsAdmin` extensions, and dispatches the correct command/verdict; structural/boundary guards (`ProjectReferenceDirectionTests`, `PublicContractPackageBoundaryTests`, `StructuralSeedConformanceTests`, `ModuleLayout`) stay green — the new `Hexalith.Parties.*` refs are out-of-module so they do not register in the in-module direction guard. **Optional hardening:** add `"Hexalith.Parties"` to the forbidden-package/assembly prefix lists in `PublicContractPackageBoundaryTests` and `ContractsBoundaryTests` to lock that Parties never leaks into the public contracts.
  - [x] xUnit v3 + Shouldly; PascalCase BDD-style names matching the surrounding tests; no raw `Assert.*`. Run from `Hexalith.Agents/`: `dotnet restore Hexalith.Agents.slnx`, `dotnet build Hexalith.Agents.slnx --configuration Release` (must be 0 warnings / 0 errors), then each touched test project individually with `dotnet test <project> --configuration Release`.

## Dev Notes

### Critical Guardrails

- This story is **Party-identity linking/provisioning + its readiness gate only**. Do NOT implement provider/model selection (1.5), response mode / approver policy (1.6), content safety (1.7), the FrontComposer setup UI (1.8), Conversation membership/`AddParticipant`, posting, invocation, generation, or proposals (Epics 2–3). AD-7's "before posting, the Agent `PartyId` must be present in the Conversation as `ParticipantType.AiAgent`" is an Epic-2 posting concern — this story only establishes the durable Agent↔Party link, not Conversation membership. [Source: epics.md#Epic-1; ARCHITECTURE-SPINE.md#AD-7-Agent-Party-Identity-And-Membership]
- `Agent` is its own aggregate boundary (AD-2). Party identity layers onto the **existing** `Agent` aggregate built in Story 1.3 — extend it; do not create a new aggregate or fold Party state into `ProviderCatalog`/`AgentInteraction`. [Source: ARCHITECTURE-SPINE.md#AD-2; 1-3-configure-and-manage-hexa-lifecycle.md#Dev-Agent-Record]
- **Aggregates stay pure (AD-3).** Parties validation/provisioning runs in the Server application orchestration/adapter and its result is **fed back to the aggregate through a command** (the trusted `party:linkValidation` extension). The aggregate emits events only — it never calls the Parties client, reads time, or does I/O. [Source: ARCHITECTURE-SPINE.md#AD-3-Pure-Aggregates-Side-Effects-Outside; epics.md (Additional Requirements: "provider calls, … Parties validation/provisioning … run in application orchestration/adapters and return through commands")]
- **Store the stable `PartyId` reference only (AD-7, AC1).** No Party display names, contact values, personal identifiers, or Parties personal-data objects in Agents durable events, status, logs, telemetry, or audit summaries. A `PartyId` (and `PreviousPartyId`/`AttemptedPartyId`) is a stable Parties-owned **identifier, not PII** — storing/carrying it is exactly the sanctioned reference. The PII (name/contact) lives in Parties. [Source: ARCHITECTURE-SPINE.md#AD-7; #Consistency-Conventions (Identity); epics.md#Story-1.4 AC1]
- **Fail closed on dependency uncertainty (AD-12, AC2).** Missing, disabled, ambiguous, unavailable (incl. stale/degraded projection), or unauthorized Party state rejects the link. Never treat a non-`Current` Parties projection as fresh. While not linked, `hexa` carries `MissingPartyIdentity` and is not callable for posting-dependent workflows. [Source: ARCHITECTURE-SPINE.md#AD-12-Authorization-And-Dependency-Uncertainty; PRD FR-21; epics.md#Story-1.4 AC2]
- **Exactly one active Party identity (AC3).** A second *distinct* link is rejected (`AgentPartyIdentityAlreadyLinkedRejection`); changing identity requires the explicit `ReplaceAgentPartyIdentity`. Re-asserting the same `PartyId` is a deterministic no-op. The aggregate structurally holds a single `PartyId`, so >1 active identity is impossible. [Source: epics.md#Story-1.4 AC3; PRD FR-2 ("An active Agent has exactly one Party identity")]
- **Authorization fails closed before mutation (AD-12, AC).** Reuse the transitional, server-populated `actor:agentsAdmin` extension gate exactly as Story 1.3 — client-supplied reserved extensions are stripped at the entry point and never trusted. Unauthorized link/replace → `AgentAdministrationDeniedRejection` (AgentId + actor + command name only). [Source: 1-3-configure-and-manage-hexa-lifecycle.md#Authorization-Guidance; ARCHITECTURE-SPINE.md#AD-12]

### Design: Where Each Responsibility Lives (the AD-3 round-trip)

```
Admin/API → Server orchestration (Application/Agents) ── authorize actor (agentsAdmin)
                                          │
                                          ├─ IAgentPartyDirectory port (Ports/) ── Parties adapter
                                          │      • ValidateExistingPartyAsync → GetPartyAsync → verdict
                                          │      • ProvisionAgentPartyAsync   → CreatePartyWithResultAsync → new PartyId + Valid
                                          │
                                          └─ dispatch aggregate command  LinkAgentPartyIdentity / ReplaceAgentPartyIdentity
                                                 envelope.Extensions (server-populated, client-stripped):
                                                   actor:agentsAdmin = true
                                                   party:linkValidation = <PartyLinkValidationStatus>
                                                          │
                          pure AgentAggregate.Handle ─────┘  emits  Linked / Replaced / LinkRejected / AlreadyLinkedRejection
                                          │
                                  AgentState.Apply  stores ONLY PartyId, bumps ConfigurationVersion
```

Rationale for putting the verdict in a **trusted envelope extension** (not the command payload): the EventStore gateway routes any aggregate command type by domain/type, so a malicious client could POST `LinkAgentPartyIdentity{PartyId}` directly. The aggregate therefore requires trusted evidence that validation happened — `party:linkValidation` is server-populated and client-stripped, identical to the `actor:agentsAdmin` trust model. The aggregate independently rejects any non-`Valid`/absent verdict (Task 3), which is the security guarantee; the orchestration always feeding the verdict gives the durable audit trail. [Source: ARCHITECTURE-SPINE.md#AD-3; #AD-12; #AD-13; 1-3-configure-and-manage-hexa-lifecycle.md#Authorization-Guidance]

### Current Code State To Preserve

Read these completely before editing — they are the exact patterns to extend (do not reshape Story 1.3's events):

- `Hexalith.Agents/src/Hexalith.Agents/Agent/AgentAggregate.cs` — `[EventStoreDomain("agent")]`, static `Handle(command, AgentState?, CommandEnvelope) → DomainResult`, `IsAgentAdmin`/`Denied`/`Invalid` helpers, `AgentAdminExtensionKey = "actor:agentsAdmin"` (server-populated). Add the link/replace handlers here using the same cascade.
- `Hexalith.Agents/src/Hexalith.Agents/Agent/AgentState.cs` — replay state; `Apply(success)` mutates, `Apply(rejection)` is a no-op via `MarkReplayOnlyEventHandled()`; `IsCreated` guard. Add `PartyId` + the four new `Apply` overloads here.
- `Hexalith.Agents/src/Hexalith.Agents/Agent/AgentConfigurationPolicy.cs` — `ComputeActivationBlockers(displayName, instructions)`; centralizes the gate so the read path and the activation path stay in lock-step. Add the `hasPartyIdentity` parameter + `MissingPartyIdentity` blocker here (one place updates both callers).
- `Hexalith.Agents/src/Hexalith.Agents/Agent/AgentInspection.cs` — pure read path → `AgentStatusView`; `GetStatus` fails closed (`NotAuthorized`/`NotFound`). Add `HasPartyIdentity` to `ToView` and pass party presence to the policy.
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/AgentActivationBlocker.cs` and `AgentStatusView.cs` — the exact enum/record shapes to extend additively (preserve ordinals; append fields).
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/Commands/*`, `Events/*`, `Events/Rejections/*` — record/XML-doc style, `IEventPayload`/`IRejectionEvent` markers, no wall-clock fields. Mirror these for the new types.
- `Hexalith.Agents/src/Hexalith.Agents.Server/Program.cs` + `Hexalith.Agents.Server.csproj` — the host scans `typeof(AgentsAssemblyMarker).Assembly`, so the new aggregate handlers auto-register (no `Handle` wiring needed). The csproj is where the Parties `Client`+`Contracts` references and `AddPartiesClient` go.
- `Hexalith.Agents/tests/Hexalith.Agents.Tests/AgentTestData.cs` — `Envelope<T>(... isAgentsAdmin ...)` builder (extend with `party:linkValidation`), `ApplyAll` dispatch switch (add new events), `ProcessAndApplyAsync` E2E driver.
- `Hexalith.Agents/tests/Hexalith.Agents.Server.Tests/{ProjectReferenceDirectionTests,PublicContractPackageBoundaryTests,StructuralSeedConformanceTests,ModuleLayout}.cs` — boundary guards. The in-module direction guard only inspects `Hexalith.Agents*` references, so adding `Hexalith.Parties.*` to `Server` is allowed; do not add Parties refs to Contracts/Client/domain.

What must be preserved: `.slnx` only, `net10.0`, C# 14, nullable, implicit usings, warnings-as-errors, Central Package Management (no inline versions), provider-SDK-free + secret-value-free public contracts, pure replay-safe aggregates, and the Agents-module **no-`UtcNow`-in-aggregates** convention (occurrence time comes from EventStore metadata; actor from `envelope.UserId`). [Source: 1-3-configure-and-manage-hexa-lifecycle.md#Current-Code-State-To-Preserve; Hexalith.AI.Tools/CLAUDE.md]

### Parties Adapter Facts (verified against the as-built `Hexalith.Parties`)

The Agents Server consumes only the **public** `Hexalith.Parties.Client` + `Hexalith.Parties.Contracts` (server/projection internals are off-limits). Verified facts:

- **Client interfaces** (`namespace Hexalith.Parties.Client.Abstractions`):
  - `IPartiesQueryClient.GetPartyAsync(string partyId, CancellationToken ct, Func<HttpRequestMessage, CancellationToken, ValueTask>? requestCustomizer = null) → Task<PartyDetail>`
  - `IPartiesCommandClient.CreatePartyWithResultAsync(CreateParty command, CancellationToken ct) → Task<PartiesCommandResult<PartyDetail>>` (also `CreatePartyAsync(...) → Task<string>` returning the new id).
- **No `PartyId` type** — Party ids are plain `string` everywhere (`CreateParty.PartyId`, `PartyDetail.Id`). So Agents stores `PartyId` as `string`/`string?` and imports nothing from Parties into its contracts.
- **`PartyType`** (`namespace Hexalith.Parties.Contracts.ValueObjects`): `Unknown, Person, Organization`. **There is no `AiAgent`/`Bot`/`System`/`Service` party type.** Provision the Agent Party as `PartyType.Organization` to avoid person PII.
- **`PartyDetail`** (`namespace Hexalith.Parties.Contracts.Models`) carries the state signals you map to a verdict: `bool IsActive`, `bool IsErased`, `bool IsRestricted`, and `ProjectionFreshnessMetadata? Freshness` (with `ProjectionFreshnessStatus { Current, Stale, Rebuilding, Degraded, Unavailable, LocalOnly }`). It also carries `[PersonalData]` fields (`DisplayName`, `SortName`, `PersonDetails`, `ContactChannels`, …) — **these must never cross the port boundary into Agents.**
- **`CreateParty`** (`namespace Hexalith.Parties.Contracts.Commands`): `{ required string PartyId; required PartyType Type; PersonDetails?; OrganizationDetails? }`. For provisioning supply `Type = Organization` + a minimal non-personal `OrganizationDetails` label; the label/name is owned by Parties and is not persisted on the Agents side.
- **DI:** `services.AddPartiesClient(IConfiguration configuration)` (`namespace Hexalith.Parties.Client.Extensions`) registers both clients (HTTP/DAPR to the Parties gateway). Config section `Parties:{ BaseUrl, Tenant }`. Agents is the first non-Parties consumer of this client.
- **References:** `$(HexalithPartiesRoot)\src\Hexalith.Parties.Client\Hexalith.Parties.Client.csproj` and `...\Hexalith.Parties.Contracts\Hexalith.Parties.Contracts.csproj`. The `$(HexalithPartiesRoot)` block is already in `Directory.Build.props`; `Hexalith.Parties` is already a `RequiredRootSubmodule`.

[Source: Hexalith.Parties/src/Hexalith.Parties.Client/Abstractions/{IPartiesQueryClient,IPartiesCommandClient}.cs; Hexalith.Parties/src/Hexalith.Parties.Contracts/{ValueObjects/PartyType,Models/PartyDetail,Models/ProjectionFreshnessMetadata,Commands/CreateParty}.cs; Hexalith.Parties/_bmad-output/project-context.md]

### Parties State → Verdict Mapping (adapter)

| `PartyDetail` observation | `PartyLinkValidationStatus` |
|---|---|
| Exists, `IsActive == true`, not erased/restricted, `Freshness.Status == Current` (or null) | `Valid` |
| `GetPartyAsync` returns not-found | `Missing` |
| `IsActive == false` **or** `IsErased == true` **or** `IsRestricted == true` | `Disabled` |
| `Freshness.Status != Current` (Stale/Rebuilding/Degraded/Unavailable/LocalOnly) | `Unavailable` (fail closed — AD-12) |
| Party not in the Agent's tenant scope / caller unauthorized to read it | `Unauthorized` |
| Lookup-by-reference resolves to more than one Party (e.g. ambiguous provisioning match) | `Ambiguous` |
| Transport/unexpected adapter failure | `Unavailable` |

`Ambiguous` is reachable mainly on a provision/lookup-by-name path; for a direct `GetPartyAsync(partyId)` it is rarely produced but is part of the verdict vocabulary the AC and UX require (DESIGN.md maps "ambiguous Party identity" to the caution/`status-important` treatment). [Source: epics.md#Story-1.4 AC2; ux-designs/ux-agents-2026-06-23/DESIGN.md (status-important: "ambiguous Party identity"); Hexalith.Parties/_bmad-output/project-context.md (projection freshness / stale-fallback)]

### Readiness / UX Context (AC4)

- Add `missing party identity` as a **distinct** readiness gate (not collapsed into lifecycle). UX canonical states: `callable`, `checking`, `invalid configuration`, `missing party identity`, `provider unavailable`, `disabled`. This story contributes the `missing party identity` input (the `MissingPartyIdentity` activation blocker + `HasPartyIdentity` on the status view); `provider unavailable` arrives in 1.5. [Source: ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#Agent-Readiness; #Cross-Surface-Components (agent-readiness-badge); 1-3-configure-and-manage-hexa-lifecycle.md#UI-/-UX-Context-For-Later-Stories]
- `agent-readiness-badge` must "explain blockers, not hide them" and must not show success unless all gates pass. The status view exposes presence (`HasPartyIdentity`) and the blocker, never Party PII in accessible names. [Source: ux-designs/ux-agents-2026-06-23/DESIGN.md#agent-readiness-badge; #EXPERIENCE.md]
- No UI is built in this story — only the contracts the later 1.8 overview/badge will consume. [Source: epics.md#Story-1.8]

### Sensitive-Data Handling (AD-7 / AD-14 / AC1)

- A `PartyId` is a stable id reference, **not** sensitive content/PII — store and carry it freely on events/rejections/state.
- Party **PII** (names, contacts, `PersonDetails`, `OrganizationDetails`, any `[PersonalData]` field) must never enter Agents events, state, status, logs, telemetry, or audit summaries — it stays in Parties and never crosses the port boundary. The adapter returns only `{ Status, PartyId }`. [Source: ARCHITECTURE-SPINE.md#AD-7; #AD-14-Sensitive-Content-And-Secret-Safety; epics.md#Story-1.4 AC1]
- The Story 1.1 secret-non-disclosure guard (forbidden member tokens `Secret`/`ApiKey`/`Credential`/`Password`/`ConnectionString`) still applies — none are needed here; keep new field names clear of them and do not weaken the guard. [Source: 1-3-configure-and-manage-hexa-lifecycle.md#Current-Code-State-To-Preserve]

### Idempotency (AD-13)

- Re-asserting the same `PartyId` via `LinkAgentPartyIdentity`/`ReplaceAgentPartyIdentity` is a deterministic `NoOp()` — no duplicate event, no version bump.
- Provisioning derives the new `PartyId` deterministically from `AgentId` so a retried `ProvisionAgentPartyAsync` does not create duplicate Parties. [Source: ARCHITECTURE-SPINE.md#AD-13-Idempotent-External-Effects]

### Latest Technical Information

- **New references this story:** `Hexalith.Parties.Client` + `Hexalith.Parties.Contracts` (sibling **source** via `$(HexalithPartiesRoot)`), added to `Hexalith.Agents.Server` only. No new NuGet packages. NSubstitute `5.3.0` is already centralized for the adapter/orchestration unit tests. [Source: Directory.Packages.props; Directory.Build.props]
- Stack baseline unchanged: .NET `10.0.300`–`10.0.301`, `net10.0`, `.slnx`, Central Package Management, xUnit v3 `3.2.2`, Shouldly `4.3.0`. Do NOT add Agent Framework, provider SDK, Dapr-runtime, or UI packages. [Source: ARCHITECTURE-SPINE.md#Stack]
- Parties pinned compatibility note: `Microsoft.IdentityModel.Tokens` is pinned to `8.19.x` across the ecosystem to align with EventStore — if a transitive conflict surfaces from the new Parties.Client reference, align versions rather than bumping independently. [Source: Hexalith.Parties/_bmad-output/project-context.md]

### Testing Requirements

- Aggregate: link success (stores `PartyId`, bumps `ConfigurationVersion`, lifecycle unchanged) / each non-`Valid` verdict → `AgentPartyIdentityLinkRejected` (+ not stored + still blocks activation) / re-link same id no-op / second distinct link → `AgentPartyIdentityAlreadyLinkedRejection` / replace success (PreviousPartyId + new id) / replace same no-op / not-found / unauthorized for both commands / replay incl. persisted new rejections / activation blocked-then-unblocked by Party gate.
- Contracts: marker interfaces present; JSON round-trip for new events/rejections + extended `AgentStatusView`; `PartyLinkValidationStatus` + `MissingPartyIdentity` serialize by name; unknown → `Unknown`; party events/rejections expose only id-shaped fields (no PII member); secret guard green.
- Server: adapter `PartyDetail`→verdict mapping (incl. stale freshness → `Unavailable`, not-found → `Missing`, cross-tenant → `Unauthorized`) via substituted Parties clients; provisioning builds `CreateParty{Organization, deterministic id}` with no PII returned to Agents; orchestration authorizes + sets server-populated extensions + dispatches the right command/verdict; structural/boundary guards still green; optional Parties-in-forbidden-prefix hardening.
- Build/test commands (run from `Hexalith.Agents/`):
  - `dotnet restore Hexalith.Agents.slnx`
  - `dotnet build Hexalith.Agents.slnx --configuration Release` (0 warnings / 0 errors, warnings-as-errors)
  - `dotnet test tests/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj --configuration Release`
  - `dotnet test tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release`
  - `dotnet test tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj --configuration Release`

### Project Structure Notes

- New/changed code:
  - `Hexalith.Agents.Contracts/Agent/`: `PartyLinkValidationStatus.cs`; `Commands/{LinkAgentPartyIdentity,ReplaceAgentPartyIdentity}.cs`; `Events/{AgentPartyIdentityLinked,AgentPartyIdentityReplaced}.cs`; `Events/Rejections/{AgentPartyIdentityLinkRejected,AgentPartyIdentityAlreadyLinkedRejection}.cs`; edits to `AgentActivationBlocker.cs` (+`MissingPartyIdentity`) and `AgentStatusView.cs` (+`HasPartyIdentity`).
  - `Hexalith.Agents/Agent/`: edits to `AgentState.cs` (+`PartyId`, +4 `Apply`), `AgentAggregate.cs` (+2 `Handle`), `AgentConfigurationPolicy.cs` (+party param/blocker), `AgentInspection.cs` (+`HasPartyIdentity`).
  - `Hexalith.Agents.Server/`: `Ports/{IAgentPartyDirectory,PartiesAgentPartyDirectory, result types}.cs`; `Application/Agents/<orchestration>.cs`; edits to `Program.cs` + `Hexalith.Agents.Server.csproj`.
  - Tests across `Hexalith.Agents.Tests`, `Hexalith.Agents.Contracts.Tests`, `Hexalith.Agents.Server.Tests`.
- Discovery loaded: root `epics.md`, PRD (FR-1/FR-2/FR-19/FR-20/FR-21/FR-24), architecture spine (AD-2/AD-3/AD-7/AD-12/AD-13/AD-14/AD-15), UX DESIGN/EXPERIENCE, the as-built Story 1.1–1.3 module code, and the `Hexalith.Parties` public client surface + its `project-context.md`. No root `project-context.md` exists for `agents`; sibling-module `project-context.md` files supply carry-forward rules (sibling **source** references, aggregate purity, no-`UtcNow`-in-aggregates, FrontComposer/Fluent inherited UI).

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.4-Link-hexa-To-A-Party-Identity]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-2-Link-Agent-To-Party-Identity]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-19-Tenant-Isolation]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-20-Enforce-Role-And-Policy-Authorization]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#FR-21-Fail-Closed-On-Dependency-Uncertainty]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-2-Aggregate-Boundaries]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-3-Pure-Aggregates-Side-Effects-Outside]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-7-Agent-Party-Identity-And-Membership]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-12-Authorization-And-Dependency-Uncertainty]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-13-Idempotent-External-Effects]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-14-Sensitive-Content-And-Secret-Safety]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#Agent-Readiness]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md#agent-readiness-badge]
- [Source: _bmad-output/implementation-artifacts/1-3-configure-and-manage-hexa-lifecycle.md#Dev-Agent-Record]
- [Source: Hexalith.Agents/src/Hexalith.Agents/Agent/AgentAggregate.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents/Agent/AgentState.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents/Agent/AgentConfigurationPolicy.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents/Agent/AgentInspection.cs]
- [Source: Hexalith.Parties/src/Hexalith.Parties.Client/Abstractions/IPartiesQueryClient.cs]
- [Source: Hexalith.Parties/src/Hexalith.Parties.Client/Abstractions/IPartiesCommandClient.cs]
- [Source: Hexalith.Parties/src/Hexalith.Parties.Contracts/Models/PartyDetail.cs]
- [Source: Hexalith.Parties/src/Hexalith.Parties.Contracts/Commands/CreateParty.cs]
- [Source: Hexalith.Parties/_bmad-output/project-context.md]

## Story Completion Status

Ultimate context engine analysis completed - comprehensive developer guide created. Story status is `ready-for-dev`.

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (claude-opus-4-8[1m])

### Debug Log References

- `dotnet build Hexalith.Agents.slnx --configuration Release` → **0 Warning(s) / 0 Error(s)** (warnings-as-errors).
- `dotnet test Hexalith.Agents.slnx --configuration Release` → **228 passed, 0 failed** (Contracts.Tests 49, Tests 142, Server.Tests 37).
- Post-review re-run (after auto-fix) → **241 passed, 0 failed** (Contracts.Tests 49, Tests 148, Server.Tests 44).

### Completion Notes List

- **Task 1 (contracts):** Added `PartyLinkValidationStatus` (fail-safe `Unknown = 0`, `[JsonStringEnumConverter]`), the `LinkAgentPartyIdentity`/`ReplaceAgentPartyIdentity` commands (PartyId-only payloads), the `AgentPartyIdentityLinked`/`AgentPartyIdentityReplaced` success events, and the `AgentPartyIdentityLinkRejected`/`AgentPartyIdentityAlreadyLinkedRejection` rejections. Appended `MissingPartyIdentity` to `AgentActivationBlocker` (ordinal 4 — existing ordinals preserved) and `HasPartyIdentity` to `AgentStatusView` (presence only). Contracts stays inward-only — `PartyId` is a plain `string`; nothing imported from Parties. Reused the existing `AgentNotFoundRejection`/`AgentAdministrationDeniedRejection`.
- **Task 2 (replay state):** Added the single new durable field `AgentState.PartyId` (`string?`) plus mutating `Apply(AgentPartyIdentityLinked/Replaced)` (each `IsCreated`-guarded) and no-op `Apply(...)` for both new rejections (replay stays total).
- **Task 3 (aggregate handlers):** Added pure `Handle(LinkAgentPartyIdentity)`/`Handle(ReplaceAgentPartyIdentity)` following the existing guard cascade. The trusted verdict is read from the server-populated `party:linkValidation` envelope extension (defaulting to `Unknown` when absent/unparseable — fail closed). Non-`Valid` → `AgentPartyIdentityLinkRejected`; idempotent re-link → `NoOp`; distinct second link → `AgentPartyIdentityAlreadyLinkedRejection`; replace sets the single identity. Linking/replacing bumps `ConfigurationVersion` and never touches `Lifecycle`. Aggregate remains pure (no Parties client, no I/O, no wall-clock).
- **Task 4 (adapter + orchestration):** Added the `$(HexalithPartiesRoot)` `Client`+`Contracts` sibling-source references to **Server only**. `IAgentPartyDirectory` port + `PartiesAgentPartyDirectory` adapter map `PartyDetail` → verdict per the Dev-Notes table (active+current → `Valid`; not-found → `Missing`; inactive/erased/restricted → `Disabled`; non-`Current` freshness → `Unavailable` fail-closed; 401/403 → `Unauthorized`; transport/unexpected → `Unavailable`) and provision an `Organization` Party with a deterministic id (`agent-{agentId}`), surfacing only `{ Status, PartyId }` (no PII). `AgentPartyIdentityOrchestrator` authorizes, strips+repopulates the reserved `actor:agentsAdmin`/`party:linkValidation` extensions from trusted sources only, builds the link/replace command, and dispatches it (always feeding the verdict for an auditable rejection).
- **Deferred runtime binding (mirrors Story 1.2/1.3):** The live Parties client requires a `Parties:{ BaseUrl, Tenant }` config section (validated eagerly), so `AddPartiesClient` is registered in `Program.cs` only when that section is present; the adapter + orchestration are always registered. The `IAgentCommandDispatcher` live DAPR/EventStore binding is deferred — a `DeferredAgentCommandDispatcher` placeholder keeps the DI graph complete and compiling, and the decision logic is fully unit-tested via substitutes. No runnable AppHost topology is stood up by this story.
- **Task 5 (readiness gate):** `AgentConfigurationPolicy.ComputeActivationBlockers` now takes `bool hasPartyIdentity` and appends `MissingPartyIdentity` (deterministic order: display name → instructions → party identity). Both callers updated (`Handle(ActivateAgent)` and `AgentInspection.ToView` pass `state.PartyId is not null`). Activation now fails closed with `MissingPartyIdentity` until a valid Party is linked; lifecycle/readiness stay distinct (Story 1.3 invariant). Existing Story 1.3 activation tests were updated to link a Party first — the correct consequence of the new gate.
- **Task 6 (tests):** Added domain (`AgentPartyIdentityTests`, replay cases), contracts (`AgentPartyIdentityContractsTests`), and server (`PartiesAgentPartyDirectoryTests`, `AgentPartyIdentityOrchestratorTests`) suites covering every AC, plus the optional hardening adding `Hexalith.Parties` to the public-contract forbidden-prefix guards. All structural/boundary guards stay green.

### File List

**Added — Contracts (`Hexalith.Agents.Contracts/Agent/`):**
- `PartyLinkValidationStatus.cs`
- `Commands/LinkAgentPartyIdentity.cs`
- `Commands/ReplaceAgentPartyIdentity.cs`
- `Events/AgentPartyIdentityLinked.cs`
- `Events/AgentPartyIdentityReplaced.cs`
- `Events/Rejections/AgentPartyIdentityLinkRejected.cs`
- `Events/Rejections/AgentPartyIdentityAlreadyLinkedRejection.cs`

**Added — Server (`Hexalith.Agents.Server/`):**
- `Ports/AgentPartyValidationResult.cs`
- `Ports/AgentPartyProvisioningRequest.cs`
- `Ports/IAgentPartyDirectory.cs`
- `Ports/PartiesAgentPartyDirectory.cs`
- `Ports/IAgentCommandDispatcher.cs`
- `Ports/DeferredAgentCommandDispatcher.cs`
- `Application/Agents/AgentPartyLinkRequest.cs`
- `Application/Agents/AgentPartyLinkOutcome.cs`
- `Application/Agents/AgentPartyIdentityOrchestrator.cs`

**Added — Tests:**
- `tests/Hexalith.Agents.Tests/AgentPartyIdentityTests.cs`
- `tests/Hexalith.Agents.Contracts.Tests/AgentPartyIdentityContractsTests.cs`
- `tests/Hexalith.Agents.Server.Tests/PartiesAgentPartyDirectoryTests.cs`
- `tests/Hexalith.Agents.Server.Tests/AgentPartyIdentityOrchestratorTests.cs`
- `tests/Hexalith.Agents.Server.Tests/DeferredAgentCommandDispatcherTests.cs`

**Modified — Contracts:**
- `Hexalith.Agents.Contracts/Agent/AgentActivationBlocker.cs` (+`MissingPartyIdentity`)
- `Hexalith.Agents.Contracts/Agent/AgentStatusView.cs` (+`HasPartyIdentity`)

**Modified — Domain (`Hexalith.Agents/Agent/`):**
- `AgentState.cs` (+`PartyId`, +4 `Apply`)
- `AgentAggregate.cs` (+2 `Handle`, +verdict helper, +party-presence in activation)
- `AgentConfigurationPolicy.cs` (+`hasPartyIdentity` param/blocker)
- `AgentInspection.cs` (+`HasPartyIdentity` and party-presence to the policy)

**Modified — Server:**
- `Hexalith.Agents.Server/Hexalith.Agents.Server.csproj` (+Parties `Client`/`Contracts` sibling refs)
- `Hexalith.Agents.Server/Program.cs` (registration: `AddPartiesClient` guarded, adapter, orchestrator, deferred dispatcher)

**Modified — Tests (Story 1.3 updates for the new activation gate + party coverage + boundary hardening):**
- `tests/Hexalith.Agents.Tests/AgentTestData.cs`
- `tests/Hexalith.Agents.Tests/AgentAggregateTests.cs`
- `tests/Hexalith.Agents.Tests/AgentConfigurationValidationTests.cs`
- `tests/Hexalith.Agents.Tests/AgentInspectionTests.cs`
- `tests/Hexalith.Agents.Tests/AgentLifecycleE2ETests.cs`
- `tests/Hexalith.Agents.Tests/AgentStateReplayTests.cs`
- `tests/Hexalith.Agents.Contracts.Tests/AgentContractsRoundTripTests.cs`
- `tests/Hexalith.Agents.Contracts.Tests/ContractsBoundaryTests.cs`
- `tests/Hexalith.Agents.Server.Tests/PublicContractPackageBoundaryTests.cs`

## Senior Developer Review (AI)

**Reviewer:** Administrator · **Date:** 2026-06-24 · **Outcome:** Approved (auto-fixed)

Adversarial review of the full File List against the four ACs. All tasks marked `[x]` are genuinely implemented; AC1–AC4 are each implemented and covered by real (non-placeholder) assertions; the aggregate stays pure (no Parties client/I/O/wall-clock); contracts stay inward-only; structural/boundary guards stay green. Git reality matched the story File List apart from one undocumented test (fixed). **No CRITICAL findings.** Three findings, all auto-fixed:

- **[MED][Security/fail-closed] Verdict parse accepted numeric/aliased strings.** `AgentAggregate.ReadPartyLinkValidation` used `Enum.TryParse` + `Enum.IsDefined`, which resolves numeric input — e.g. `party:linkValidation="1"` parsed to `Valid`, silently bypassing the by-name fail-closed contract the aggregate documents as its independent security boundary (AC2). Not exploitable beyond the existing "upstream must strip reserved extensions" trust assumption (an attacker who can inject `1` can already inject `Valid`), but it contradicted the `[JsonStringEnumConverter]` by-name contract and was looser than the strict `actor:agentsAdmin == "true"` gate. **Fix:** require an exact, case-sensitive canonical name (`string.Equals(Enum.GetName(status), value, Ordinal)`) → numeric/aliased/cased/padded values now fail closed to `Unknown`. Added a `[Theory]` regression (`"1"`, `"01"`, `"valid"`, `" Valid"`) in `AgentPartyIdentityTests`. [`AgentAggregate.cs` `ReadPartyLinkValidation`]
- **[MED][Docs] File List omitted a delivered test.** `tests/Hexalith.Agents.Server.Tests/DeferredAgentCommandDispatcherTests.cs` was committed but missing from the story File List. **Fix:** added to the File List.
- **[LOW][Clarity] Unused `tenantId` in the Parties adapter.** `PartiesAgentPartyDirectory.ValidateExistingPartyAsync`/`ProvisionAgentPartyAsync` accept `tenantId` but don't use it — tenant isolation is enforced ambiently by the Parties client (config/auth context), and a cross-tenant read surfaces as `401/403 → Unauthorized` (never a silently-fresh `Valid`). Per-call scoping is correctly deferred with the live runtime binding. **Fix:** documented the intent with a code comment (no behavior change; FR-19/AD-12 rationale preserved).

**Verification:** `dotnet build Hexalith.Agents.slnx -c Release` → 0 warnings / 0 errors; `dotnet test` (3 projects) → **241 passed, 0 failed** (Tests 148, Contracts 49, Server 44).

## Change Log

| Date       | Version | Description                                                                 | Author |
| ---------- | ------- | --------------------------------------------------------------------------- | ------ |
| 2026-06-24 | 1.1     | Senior Developer review (auto-fix): hardened `ReadPartyLinkValidation` to an exact by-name match so numeric/aliased verdict strings fail closed to `Unknown` (+regression theory); documented the adapter's ambient-tenant scoping; added the missing `DeferredAgentCommandDispatcherTests.cs` to the File List. Build 0/0; 241 tests pass. No CRITICAL findings → Status → done. | Administrator (Review) |
| 2026-06-24 | 0.1     | Created Story 1.4 context (Link `hexa` To A Party Identity): Party-identity link/replace contracts + pure aggregate handlers, `IAgentPartyDirectory` port + Parties adapter, application orchestration with server-populated `party:linkValidation` trust model, and the `MissingPartyIdentity` readiness gate. Status → ready-for-dev. | Bob (Scrum Master) |
| 2026-06-24 | 1.0     | Implemented Story 1.4: Party-identity link/replace contracts + pure aggregate handlers (trusted `party:linkValidation` verdict, fail-closed), `AgentState.PartyId` replay, `IAgentPartyDirectory` port + `PartiesAgentPartyDirectory` adapter (verdict mapping + deterministic Organization provisioning, no PII), `AgentPartyIdentityOrchestrator` (client-stripped server-populated extensions), and the `MissingPartyIdentity` activation gate. Updated Story 1.3 activation tests for the new gate; added domain/contracts/server suites (228 tests, 0 warnings). Live Parties-client + command-dispatch binding deferred. Status → review. | Amelia (Dev) |
