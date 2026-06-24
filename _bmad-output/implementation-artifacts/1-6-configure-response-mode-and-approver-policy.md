---
baseline_commit: c9fc081bc7ac53525ae08f2f0c98846341f7c0e3
---

# Story 1.6: Configure Response Mode And Approver Policy

Status: done

<!-- Note: Validation is optional. Run validate-create-story for quality check before dev-story. -->

## Story

As an Agent Administrator,
I want to configure response mode and approval authority for `hexa`,
so that the tenant can choose automatic posting or governed confirmation before generated content reaches a Conversation.

## Acceptance Criteria

**AC1 - Choosing a response mode records the mode + configuration version, and is future-only**
**Given** `hexa` exists with base configuration
**When** an authorized administrator chooses Automatic Response Mode
**Then** the Agent records the mode and configuration version
**And** the system makes clear that mode changes apply only to future Agent Calls.

**AC2 - Confirmation mode can configure all V1 approver sources, with the facilitator-based owner resolver**
**Given** an administrator chooses Confirmation Response Mode
**When** the administrator configures Approver Policy sources
**Then** the policy can include caller `PartyId`, predefined `PartyId`s, tenant roles from the local Tenants projection, and conversation authority resolved from Conversations detail
**And** Conversation owner authority uses the V1 facilitator-based resolver unless an explicit Conversations owner resolver exists.

**AC3 - Confirmation-mode activation fails closed when any approver source is not resolvable**
**Given** an Approver Policy source is missing, stale, ambiguous, disabled, or unavailable
**When** readiness is evaluated for confirmation mode
**Then** activation is blocked with a policy readiness reason
**And** the system fails closed rather than treating missing policy state as permissive.

**AC4 - Storing the policy records a policy version + disclosure category for consistent policy-basis reporting**
**Given** approval policy decisions will later be used in proposal workflows
**When** the policy is stored
**Then** the system records a policy version and disclosure category for policy-basis reporting
**And** API/client contracts and future UI surfaces can expose user-visible, operator-only, redacted, or omitted basis consistently.

[Source: _bmad-output/planning-artifacts/epics.md#Story-1.6-Configure-Response-Mode-And-Approver-Policy; prds/prd-agents-2026-06-23/prd.md#4-3-response-policy-and-approver-configuration; architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-2-Aggregate-Boundaries; #AD-4-Interaction-Snapshot; #AD-8-Approver-Policy-Resolution; #AD-12-Authorization-And-Dependency-Uncertainty]

## Tasks / Subtasks

- [x] **Task 1 - Add the response-mode + approver-policy public contracts to `Hexalith.Agents.Contracts/Agent/`** (AC: #1, #2, #4)
  - [x] Add the enum `AgentResponseMode` under `Hexalith.Agents.Contracts/Agent/` with values `Unknown = 0, Automatic, Confirmation`. Decorate it `[JsonConverter(typeof(JsonStringEnumConverter))]` and document `Unknown = 0` as the "not-yet-configured" sentinel that **fails the activation gate** (so an Agent created in 1.1 before a mode was chosen is not silently treated as Automatic). Mirror the XML-doc/sentinel style of `AgentLifecycleStatus.cs`.
  - [x] Add the enum `ApproverPolicySourceKind` with values `Unknown = 0, Caller, PredefinedParty, TenantRole, ConversationOwner` (`[JsonStringEnumConverter]`, `Unknown = 0` sentinel). These are the four V1 approver-source kinds (AD-8). `ConversationOwner` is resolved by the V1 facilitator-based resolver (AC2; AD-8) — the *kind* is a policy declaration; the concrete conversation is bound per Agent Call at runtime (Epic 3), not at config time.
  - [x] Add the enum `ApproverPolicyBasisDisclosure` with values `Unknown = 0, UserVisible, OperatorOnly, Redacted, Omitted` (`[JsonStringEnumConverter]`, `Unknown = 0` sentinel). This is the FR-7 disclosure category controlling how the policy basis is reported in later proposal/approval surfaces (Epic 3). It is a safe classification — no secret, no content (AD-14).
  - [x] Add the safe verdict enum `ApproverPolicyValidationStatus` with values `Unknown = 0, Valid, Incomplete, Missing, Disabled, Ambiguous, Unavailable, Unauthorized` (`[JsonStringEnumConverter]`, `Unknown = 0` fail-safe sentinel). **Mirror the exact shape/XML-doc of `PartyLinkValidationStatus.cs` / `ProviderSelectionValidationStatus.cs`.** This is the trusted confirmation-mode readiness verdict; `Incomplete` = no approver source configured; the rest map the AC3 dependency states (missing / stale→`Unavailable` / ambiguous / disabled / unavailable / cross-tenant→`Unauthorized`). It carries no secret, no Party PII, no provider/SDK type (AD-7, AD-9, AD-14).
  - [x] Add the value object `ApproverPolicySource` (bare record) under `Hexalith.Agents.Contracts/Agent/`: `ApproverPolicySource(ApproverPolicySourceKind Kind, string? PartyId, string? TenantRole)`. For `Caller`/`ConversationOwner`, both `PartyId` and `TenantRole` are `null`; for `PredefinedParty`, `PartyId` is set (a stable Parties-owned **reference, not PII** — AD-7); for `TenantRole`, `TenantRole` is the safe role name. Document that these are safe references only.
  - [x] Add the cohesive policy value object `AgentApproverPolicy(IReadOnlyList<ApproverPolicySource> Sources, ApproverPolicyBasisDisclosure DisclosureCategory)` — the unit the command/event/state/status reuse (do NOT spread the fields across multiple parameters). The monotonic policy *version* is assigned by the aggregate (not on this value object), exactly like `InstructionsVersion`.
  - [x] Add the aggregate command `Agent/Commands/ConfigureAgentResponseMode(AgentResponseMode Mode)`. Bare record; the Agent id comes from the envelope. Mirror `SelectAgentProviderModel.cs` shape. No wall-clock, no verdict on the payload.
  - [x] Add the aggregate command `Agent/Commands/ConfigureAgentApproverPolicy(AgentApproverPolicy Policy)`. Bare record; carries only the safe policy value (sources + disclosure). No policy version on the payload (the aggregate assigns it). Mirror `LinkAgentPartyIdentity.cs` doc style.
  - [x] Add the success event `Agent/Events/AgentResponseModeConfigured(string AgentId, AgentResponseMode Mode, int ConfigurationVersion) : IEventPayload` (no wall-clock). This durable event **is** the AC1 audit evidence of the mode choice. Mirror `AgentProviderModelSelected.cs`.
  - [x] Add the success event `Agent/Events/AgentApproverPolicyConfigured(string AgentId, AgentApproverPolicy Policy, int ApproverPolicyVersion, int ConfigurationVersion) : IEventPayload` (no wall-clock). This **is** the AC4 record: the configured policy, the new monotonic `ApproverPolicyVersion`, the disclosure category (inside `Policy`), and the bumped configuration version. Mirror `AgentPartyIdentityLinked.cs`.
  - [x] **Reuse** the existing `AgentNotFoundRejection`, `AgentAdministrationDeniedRejection`, and `InvalidAgentConfigurationRejection` (Story 1.3) for not-found / unauthorized / structurally-malformed-policy rejections — do NOT add new rejection events for those. The confirmation-mode readiness fail-closed surfaces through the existing `AgentActivationBlockedRejection` carrying the new blockers (Task 3), exactly as the provider gate does at activation (1.5).
  - [x] Extend `AgentActivationBlocker` by **appending** (preserve existing ordinals `Unknown=0 … MissingProviderSelection=5, ProviderUnavailable=6`): `MissingResponseMode = 7` (no response mode chosen), `MissingApproverPolicy = 8` (Confirmation mode but no approver source configured), `ApproverPolicyUnresolvable = 9` (Confirmation mode, a configured source is missing/disabled/ambiguous/unavailable/unauthorized). Do not reorder/renumber. Document that Automatic mode requires no approver policy.
  - [x] Extend `AgentStatusView` by **appending** (keep `IReadOnlyList<AgentActivationBlocker> ActivationBlockers` the final positional parameter): `AgentResponseMode ResponseMode`, `bool HasApproverPolicy`, `ApproverPolicyBasisDisclosure ApproverPolicyDisclosure`, `int ApproverPolicyVersion`. These feed the 1.8 overview / `agent-readiness-badge` (response mode + approver policy completeness gates). Do NOT put the full approver-source list on the status view — the source list lives on state and the configuration read path (`GetAgentConfigurationQuery`, deferred read-model); the status view stays a compact readiness projection.
  - [x] Keep `Hexalith.Agents.Contracts` inward-only (no provider SDK, no `Microsoft.Agents.AI`, no Dapr, no server infra, no sibling-module types). The Story 1.1 secret-non-disclosure guard (forbidden member tokens `Secret`/`ApiKey`/`Credential`/`Password`/`ConnectionString`) must stay green and unweakened — keep all new member names clear of those tokens (`ApproverPolicyVersion`, `DisclosureCategory`, `TenantRole`, etc. are safe).

- [x] **Task 2 - Extend the pure `Agent` aggregate replay state** (AC: #1, #2, #4)
  - [x] In `Hexalith.Agents/Agent/AgentState.cs` add: `public AgentResponseMode ResponseMode { get; set; } = AgentResponseMode.Unknown;` (default = not configured), `public IReadOnlyList<ApproverPolicySource>? ApproverPolicySources { get; set; }` (null = no policy), `public ApproverPolicyBasisDisclosure ApproverPolicyDisclosure { get; set; }`, and `public int ApproverPolicyVersion { get; set; }`. These are the ONLY new durable fields — safe references/enums + a version, no secrets, no content (AD-9, AD-14).
  - [x] Add `Apply(AgentResponseModeConfigured e)` → set `ResponseMode = e.Mode`, `ConfigurationVersion = e.ConfigurationVersion` (guarded by `if (!IsCreated) return;` exactly like the other update applies).
  - [x] Add `Apply(AgentApproverPolicyConfigured e)` → set `ApproverPolicySources = e.Policy.Sources`, `ApproverPolicyDisclosure = e.Policy.DisclosureCategory`, `ApproverPolicyVersion = e.ApproverPolicyVersion`, `ConfigurationVersion = e.ConfigurationVersion` (same `IsCreated` guard).
  - [x] No new no-op rejection `Apply` is needed — the only rejections this story emits (`AgentNotFoundRejection`, `AgentAdministrationDeniedRejection`, `InvalidAgentConfigurationRejection`, `AgentActivationBlockedRejection`) already have replay-safe no-op `Apply` overloads in `AgentState`. Confirm replay stays total.

- [x] **Task 3 - Implement the pure aggregate handlers + extend the activation gate** (AC: #1, #2, #3, #4)
  - [x] In `Hexalith.Agents/Agent/AgentAggregate.cs` add a private const `ApproverPolicyValidationExtensionKey = "approver:policyValidation"` (server-populated only; same SECURITY comment style as `ProviderSelectionValidationExtensionKey`) and a helper `ReadApproverPolicyValidation(CommandEnvelope envelope)` that parses the extension into `ApproverPolicyValidationStatus` using the **exact-name, case-sensitive** idiom (`Enum.TryParse(value, ignoreCase: false, out status) && string.Equals(Enum.GetName(status), value, StringComparison.Ordinal)`, else `Unknown`) — copy `ReadProviderSelectionValidation` verbatim in shape so numeric/aliased/cased input fails closed to `Unknown`.
  - [x] Add `Handle(ConfigureAgentResponseMode command, AgentState? state, CommandEnvelope envelope)` following the guard cascade: `ArgumentNullException.ThrowIfNull(command/envelope)` → `IsAgentAdmin(envelope)` else `Denied(agentId, envelope, nameof(ConfigureAgentResponseMode))` → `state is null || !state.IsCreated` else `AgentNotFoundRejection`. Then: reject `AgentResponseMode.Unknown` via `Invalid(agentId, "Response mode must be Automatic or Confirmation.")` (cannot configure the sentinel). Idempotent: `state.ResponseMode == command.Mode` → `DomainResult.NoOp()` (AD-13). Else emit `AgentResponseModeConfigured(agentId, command.Mode, state.ConfigurationVersion + 1)` — a configuration change bumps `ConfigurationVersion` (AD-4 snapshot); lifecycle unchanged (Story 1.3 invariant); future-only.
  - [x] Add `Handle(ConfigureAgentApproverPolicy command, AgentState? state, CommandEnvelope envelope)` with the same admin → not-found cascade. Then **structural validation** (reuse `Invalid(...)` → `InvalidAgentConfigurationRejection`, no echoing of values): each source's shape must match its kind (`PredefinedParty` requires a non-blank `PartyId` and no `TenantRole`; `TenantRole` requires a non-blank `TenantRole` and no `PartyId`; `Caller`/`ConversationOwner` require neither; reject `ApproverPolicySourceKind.Unknown`); reject `ApproverPolicyBasisDisclosure.Unknown`; reject duplicate sources. An **empty** source list is structurally storable (it just won't satisfy confirmation readiness — Task 4). Idempotent: a policy equal to the recorded one (same ordered sources + disclosure) → `DomainResult.NoOp()` (AD-13). Else emit `AgentApproverPolicyConfigured(agentId, normalizedPolicy, state.ApproverPolicyVersion + 1, state.ConfigurationVersion + 1)` — bumps both the policy version (AC4) and the configuration version (AD-4); lifecycle unchanged; future-only.
  - [x] **Configuring the policy never reads Parties/Tenants/Conversations** (AD-3): storing records the configured sources; *resolving* them against dependencies is the activation-readiness concern (Task 4 + Task 6). The aggregate stays pure — no I/O, no `UtcNow`, no `Guid.NewGuid`, no dependency reads; the readiness verdict is plain trusted data fed through the envelope extension.
  - [x] **Extend `Handle(ActivateAgent)`** to feed response-mode + approver-policy readiness into `ComputeActivationBlockers` (Task 4): pass `responseMode: state.ResponseMode`, `hasApproverPolicy: state.ApproverPolicySources is { Count: > 0 }`, and `approverPolicyResolved: ReadApproverPolicyValidation(envelope) == ApproverPolicyValidationStatus.Valid`. A direct `ActivateAgent` that did not re-resolve the policy (no trusted `approver:policyValidation` verdict) **fails closed** with `ApproverPolicyUnresolvable` whenever the recorded mode is Confirmation and a policy is present — the correct AC3 behavior. (Automatic mode needs no policy, so it is unaffected.)

- [x] **Task 4 - Wire response-mode + approver-policy gates into `AgentConfigurationPolicy` + `AgentInspection`** (AC: #1, #3)
  - [x] Change `AgentConfigurationPolicy.ComputeActivationBlockers` to also accept `AgentResponseMode responseMode, bool hasApproverPolicy, bool approverPolicyResolved`. After the existing provider gate, append in **deterministic order**: `if (responseMode == AgentResponseMode.Unknown) → MissingResponseMode;` then `else if (responseMode == AgentResponseMode.Confirmation) { if (!hasApproverPolicy) → MissingApproverPolicy; else if (!approverPolicyResolved) → ApproverPolicyUnresolvable; }`. Automatic mode appends no approver blocker. Keep the full order: display name → instructions → party identity → provider selection → provider unavailable → response mode → approver policy missing → approver policy unresolvable.
  - [x] Update both callers:
    - `AgentAggregate.Handle(ActivateAgent, ...)` passes the three new inputs as in Task 3 (live verdict via the trusted extension).
    - `AgentInspection.ToView(state)` passes `responseMode: state.ResponseMode`, `hasApproverPolicy: state.ApproverPolicySources is { Count: > 0 }`, and `approverPolicyResolved: true`, and sets the new `ResponseMode`/`HasApproverPolicy`/`ApproverPolicyDisclosure`/`ApproverPolicyVersion` view fields. **Rationale (mirror the 1.5 provider boundary comment):** `AgentInspection` is a *pure* read over Agent state only — it cannot freshly resolve the Tenants projection / Conversations detail, so it trusts the last-configured policy (`approverPolicyResolved: true`) and surfaces only the static `MissingResponseMode`/`MissingApproverPolicy` gates. Live `ApproverPolicyUnresolvable` surfacing for the readiness badge comes from the activation path (this story, via the verdict) and the 1.8 status/overview orchestration that resolves the sources. Do NOT make `AgentInspection` resolve dependencies — that would break purity and AD-3. Document this in a code comment.
  - [x] Confirm lifecycle and blockers stay distinct (Story 1.3 invariant; `agent-readiness-badge`): a resolvable policy clears the approver blockers but does not by itself make a `Draft`/`Disabled` agent `Active`.

- [x] **Task 5 - Approver-policy resolution port + the pure verdict evaluator in `Hexalith.Agents.Server`** (AC: #3)
  - [x] Define a server port `Hexalith.Agents.Server/Ports/IApproverPolicyResolver` with `Task<ApproverPolicyResolutionResult> ResolveAsync(string tenantId, AgentApproverPolicy policy, CancellationToken ct)`. The implementation resolves each configured source against its dependency — `PredefinedParty` through the existing `IAgentPartyDirectory.ValidateExistingPartyAsync` (reuse — do NOT add a second Parties port), `TenantRole` through the local Tenants projection, `ConversationOwner` through the V1 facilitator-resolver availability check (AD-8) — and returns ONLY safe per-source outcomes (no PII, no secrets crossing the boundary — mirror `AgentPartyValidationResult` / `ProviderCatalogEntryReadResult`). These port types are **server-internal**, not public contracts.
  - [x] Define the server-internal result `ApproverPolicyResolutionResult(IReadOnlyList<ApproverSourceResolution> Sources)` where `ApproverSourceResolution(ApproverPolicySourceKind Kind, ApproverSourceOutcome Outcome)` and `ApproverSourceOutcome { Unknown = 0, Resolved, Missing, Disabled, Ambiguous, Unavailable, Unauthorized }`. Keep these in the Server project.
  - [x] Add the pure, deterministic evaluator `Hexalith.Agents.Server/Application/Agents/ApproverPolicyVerdict` with `ApproverPolicyValidationStatus Evaluate(AgentApproverPolicy policy, ApproverPolicyResolutionResult result)` — **mirror `ProviderSelectionVerdict.Evaluate`** in spirit. Precedence (fail closed): empty `policy.Sources` → `Incomplete`; then aggregate the per-source outcomes worst-first — any `Unauthorized` → `Unauthorized` (AC3 cross-tenant never permissive); any `Unavailable`/`Unknown` → `Unavailable` (stale/degraded fails closed, AD-12); any `Ambiguous` → `Ambiguous`; any `Disabled` → `Disabled`; any `Missing` → `Missing`; only when every source is `Resolved` → `Valid`. It reads only safe outcomes and never resolves to `Valid` under any uncertainty.
  - [x] Provide a **deferred** adapter `DeferredApproverPolicyResolver : IApproverPolicyResolver` (mirror `DeferredProviderCatalogReader` / `DeferredAgentCommandDispatcher`): throws a clear `NotSupportedException` until the live Tenants-projection / Conversations-facilitator / Parties bindings are wired in the dedicated read-model/topology story. The verdict logic stays fully unit-testable here via a substituted `IApproverPolicyResolver`. Note in the message that the Parties leg can reuse the live `PartiesAgentPartyDirectory` once the read-model story wires the others.

- [x] **Task 6 - Response-mode + approver-policy config orchestrations, and extend activation re-validation in `Hexalith.Agents.Server`** (AC: #1, #2, #3, #4)
  - [x] Add `Hexalith.Agents.Server/Application/Agents/AgentResponseModeOrchestrator` (thin): authorize the actor as Agents admin (fail closed before dispatch), strip the reserved `actor:agentsAdmin` key from any client-supplied extensions and repopulate it server-side, build `ConfigureAgentResponseMode(mode)`, and dispatch through `IAgentCommandDispatcher`. Mirror `AgentProviderSelectionOrchestrator`'s structure (request/outcome records + `BuildTrustedExtensions`) but with **no dependency verdict** — response mode config has no external dependency.
  - [x] Add `Hexalith.Agents.Server/Application/Agents/AgentApproverPolicyOrchestrator` (thin): same authorize → strip-reserved → dispatch shape for `ConfigureAgentApproverPolicy(policy)`. Storing the policy does **not** resolve sources (structural validation is in the aggregate; resolution is the activation concern), so this orchestrator also carries only `actor:agentsAdmin`. Add the matching server-internal request/outcome records.
  - [x] **Extend the activation re-validation path (`AgentActivationProviderRevalidation`)** so activation populates the `approver:policyValidation` verdict alongside `provider:selectionValidation`. Activation is one command/one envelope carrying all trusted dependency verdicts. When the recorded response mode is Confirmation and a policy is present, call `IApproverPolicyResolver.ResolveAsync` + `ApproverPolicyVerdict.Evaluate` and put the result on the `approver:policyValidation` extension; otherwise leave it `Unknown` (Automatic mode / no policy → the aggregate's gate ignores it). Add `approver:policyValidation` to the reserved-keys set (server-populated, client-stripped). Extend `AgentActivationRevalidationRequest` with the recorded `ResponseMode` + `AgentApproverPolicy?` (supplied from the Agent status read, same way `SelectedProviderId`/`SelectedModelId` are). Keep the class name to avoid churning Story 1.5's tests; update its XML-doc to state it now re-validates all dependency gates (provider + approver policy). Inject `IApproverPolicyResolver` as a third constructor dependency.
  - [x] Register the new port adapter + the two orchestrations in `Hexalith.Agents.Server/Program.cs`: `IApproverPolicyResolver → DeferredApproverPolicyResolver` (singleton, like the deferred reader), `AgentResponseModeOrchestrator` + `AgentApproverPolicyOrchestrator` (scoped). The extended `AgentActivationProviderRevalidation` registration stays but now resolves the added `IApproverPolicyResolver` dependency. Live bindings (Tenants projection / Conversations facilitator / command dispatch / AppHost topology) remain **deferred** — register what compiles cleanly and note the deferred runtime binding in a completion note. No new sibling-module references and no provider SDK are needed (Parties is already referenced; the Tenants/Conversations live legs are deferred).

- [x] **Task 7 - Tests + narrow verification** (AC: #1, #2, #3, #4)
  - [x] **Domain (`tests/Hexalith.Agents.Tests`):** extend `AgentTestData` with: an approver-validation envelope helper (`ActivateEnvelope` or extend the existing select-style helper to also set `approver:policyValidation`), a `StateWithResponseMode(...)` and a `StateWithApproverPolicy(...)`/`StateConfirmationReady(...)` helper, constants for a sample policy, and the two new events in the `ApplyAll` dispatch switch. New `AgentResponseModeTests`: configure Automatic/Confirmation success (records mode, bumps `ConfigurationVersion`, lifecycle unchanged); reject `Unknown` mode (`InvalidAgentConfigurationRejection`); idempotent same-mode → `NoOp`; change mode → new event + bumped version; not-found → `AgentNotFoundRejection`; unauthorized → `AgentAdministrationDeniedRejection`; replay through `Apply`. New `AgentApproverPolicyTests`: configure success (records sources + disclosure, bumps `ApproverPolicyVersion` and `ConfigurationVersion`, lifecycle unchanged); structural rejects (`PredefinedParty` without `PartyId`, `TenantRole` without role, `Unknown` kind, `Unknown` disclosure, duplicate source) → `InvalidAgentConfigurationRejection`; empty policy is storable; idempotent identical policy → `NoOp`; changed policy → new event + bumped `ApproverPolicyVersion`, prior event preserved (AC4 future-only); not-found / unauthorized rejections; replay.
  - [x] **Activation gate (`tests/Hexalith.Agents.Tests`):** activation blocked by `MissingResponseMode` (no mode); Automatic mode + provider/party ready → activatable with **no** approver policy; Confirmation mode + no policy → blocked by `MissingApproverPolicy`; Confirmation mode + policy present but verdict non-`Valid`/absent → blocked by `ApproverPolicyUnresolvable`; Confirmation mode + policy present + `Valid` verdict → activatable; the verdict-parse fail-closed theory for `approver:policyValidation` (`"1"`, `"01"`, `"valid"`, `" Valid"` → `Unknown`) mirroring the 1.4/1.5 hardening.
  - [x] **Update prior activation tests (critical regression — exactly as 1.5 updated 1.3/1.4):** activation now also gates on response mode (and, for confirmation, approver policy). Update `AgentAggregateTests`, `AgentConfigurationValidationTests`, `AgentPartyIdentityTests`, `AgentProviderSelectionTests`, `AgentInspectionTests`, and the `AgentLifecycleE2ETests` journey so every activation path first configures a response mode (Automatic is simplest for the existing happy paths) and supplies the trusted verdicts (provider `Valid` + — only if a path uses Confirmation — approver `Valid`). A previously-passing "activates" test that now stops at `MissingResponseMode` is the correct consequence of the new gate, not a test bug.
  - [x] **Contracts (`tests/Hexalith.Agents.Contracts.Tests`):** new events/commands implement the expected EventStore marker interfaces; System.Text.Json round-trip for `AgentResponseModeConfigured`, `AgentApproverPolicyConfigured` (incl. the nested `AgentApproverPolicy`/`ApproverPolicySource` list), and the extended `AgentStatusView`; `AgentResponseMode`, `ApproverPolicySourceKind`, `ApproverPolicyBasisDisclosure`, `ApproverPolicyValidationStatus`, and the three new `AgentActivationBlocker` values serialize **by name** and unknown input deserializes to the `Unknown`/sentinel fail-safe; assert the new events/value objects expose only id-reference/enum/version members and no secret/credential/Party-PII member; the Story 1.1 `ContractsSecretNonDisclosureTests` and contracts-boundary guards stay green and unweakened.
  - [x] **Server (`tests/Hexalith.Agents.Server.Tests`):** `ApproverPolicyVerdict` precedence tests (empty→`Incomplete`, any `Unauthorized`→`Unauthorized`, `Unavailable`/`Unknown`→`Unavailable`, `Ambiguous`, `Disabled`, `Missing`, all-resolved→`Valid`) via a constructed `ApproverPolicyResolutionResult`; `AgentResponseModeOrchestratorTests` + `AgentApproverPolicyOrchestratorTests` (authorize fail-closed; strips client-supplied `actor:agentsAdmin`/`approver:policyValidation`; dispatches the right command + server-populated extensions) using substituted `IAgentCommandDispatcher`/`IApproverPolicyResolver` (NSubstitute `5.3.0` already centralized); extended activation-revalidation tests asserting Confirmation mode populates `approver:policyValidation` from the resolver while Automatic leaves it `Unknown`, and the reserved key is server-populated/client-stripped; `DeferredApproverPolicyResolver` throws like the other deferred placeholders. Structural/boundary guards (`ProjectReferenceDirectionTests`, `PublicContractPackageBoundaryTests`, `StructuralSeedConformanceTests`, `ModuleLayout`, `PackageVersionCentralizationTests`, `BuildContractConformanceTests`) stay green.
  - [x] xUnit v3 + Shouldly; PascalCase BDD-style names matching the surrounding tests; no raw `Assert.*`. Run from `Hexalith.Agents/`: `dotnet restore Hexalith.Agents.slnx`, `dotnet build Hexalith.Agents.slnx --configuration Release` (must be **0 warnings / 0 errors**, warnings-as-errors), then each touched test project individually with `dotnet test <project> --configuration Release`.

## Dev Notes

### Critical Guardrails

- This story is **response mode + approver policy configuration onto the existing `Agent` aggregate + its confirmation-mode readiness gate + the server-side approver-policy resolution seam, ONLY**. Do NOT implement: content safety / activation-gate hardening (1.7), the FrontComposer setup UI (1.8), proposal creation / edit / regenerate / approve / reject / abandon / expire or any approval-time enforcement (Epic 3), Conversation invocation / context / posting / generation (Epic 2), the DAPR read-model binding, or the live Tenants/Conversations/Parties resolution wiring. The aggregate makes **no dependency reads** in this story; source resolution is server orchestration only (AD-3). [Source: epics.md#Epic-1; ARCHITECTURE-SPINE.md#AD-8-Approver-Policy-Resolution]
- **Configuration-time vs approval-time split (the central scope line).** 1.6 *configures and stores* the response mode + approver policy (sources, policy version, disclosure category) and *validates resolvability for confirmation-mode activation*. Epic 3 *loads and enforces* the policy at proposal/approval time (who may edit/regenerate/approve, recording the policy basis per decision) and *applies* the disclosure category when rendering proposal basis. Store here; enforce there. [Source: prd.md#4-3-response-policy-and-approver-configuration; prd.md#4-6-proposed-agent-reply-workflow; epics.md#Story-1.6; #Story-3.5]
- **The `Agent` aggregate owns response policy + approver policy (AD-2).** Layer them onto the **existing** `Agent` aggregate (1.3/1.4/1.5) — extend it; do not create a new aggregate, and do not fold these onto `ProviderCatalog`/`AgentInteraction`. The interaction snapshot (Epic 2) will copy `response mode` + `approver policy version` at request time (AD-4), which is exactly why a config change bumps `ConfigurationVersion` and a policy change bumps `ApproverPolicyVersion`. [Source: ARCHITECTURE-SPINE.md#AD-2-Aggregate-Boundaries; #AD-4-Interaction-Snapshot]
- **Aggregates stay pure (AD-3).** Tenants-projection reads, Conversations-detail reads, and Parties validation run in the Server application orchestration/adapters and the **aggregated** result (the `ApproverPolicyValidationStatus` verdict) is fed back to the aggregate through the trusted `approver:policyValidation` envelope extension. The aggregate emits events only — it never reads a projection, calls a sibling module, reads time, or does I/O. [Source: ARCHITECTURE-SPINE.md#AD-3-Pure-Aggregates-Side-Effects-Outside; #Consistency-Conventions (Mutation, Time)]
- **Fail closed on confirmation-mode approver readiness (AD-12, AC3).** A missing, stale (→`Unavailable`), ambiguous, disabled, unavailable, or cross-tenant (→`Unauthorized`) approver source — or a confirmation-mode policy with no source at all (`Incomplete`) — blocks *activation* with `MissingApproverPolicy`/`ApproverPolicyUnresolvable`. Never treat unread/missing policy state as permissive. A direct `ActivateAgent` that did not re-resolve carries no trusted verdict, so a confirmation-mode agent fails closed. **Automatic mode requires no approver policy** and is unaffected by these gates. [Source: ARCHITECTURE-SPINE.md#AD-12-Authorization-And-Dependency-Uncertainty; prd.md#4-7-authorization-tenant-isolation-and-governance (FR-21); epics.md#Story-1.6 AC3]
- **Mode/policy changes affect future Agent Calls only; nothing is rewritten (AC1, AC4, AD-4).** A changed mode/policy emits a new append-only event and bumps the version(s); prior events are immutable. `AgentInteraction` (Epic 2) snapshots response mode + approver policy version at request time, so in-flight/historical interactions are unaffected. Re-applying the same mode/policy is a deterministic `NoOp` (AD-13). [Source: ARCHITECTURE-SPINE.md#AD-4; #AD-13-Idempotent-External-Effects; prd.md#4-3 (FR-6 "Response mode changes affect future Agent Calls only"); epics.md#Story-1.6 AC1]
- **V1 conversation-owner authority is facilitator-based (AD-8, AC2).** The `ConversationOwner` source kind is resolved through the V1 facilitator-based resolver: "Current Conversations contracts expose `ParticipantRole.Facilitator` but no owner field; V1 treats product 'conversation owner' authority as Conversation Facilitator unless Conversations adds an explicit owner resolver before implementation." Store the *kind* now; the concrete conversation is bound per Agent Call at runtime (Epic 3). Swapping in a first-class owner resolver later changes only the resolver, not the stored policy or proposal state. [Source: ARCHITECTURE-SPINE.md#AD-8-Approver-Policy-Resolution; #Deferred; epics.md#Story-1.6 AC2]
- **Authorization + tenant isolation fail closed before mutation/disclosure (AD-12, FR-19/20).** Reuse the transitional, server-populated `actor:agentsAdmin` extension gate exactly as 1.3/1.4/1.5 — client-supplied reserved extensions (`actor:agentsAdmin`, `approver:policyValidation`) are stripped at the entry point and never trusted. Unauthorized config → `AgentAdministrationDeniedRejection`. Tenant roles and predefined Parties are resolved within the Agent's tenant scope; a cross-tenant source surfaces as `Unauthorized` and never leaks another tenant's records. Config changes are auditable (the durable events are the audit evidence — FR-24). [Source: prd.md#4-7 (FR-19/FR-20/FR-24); ARCHITECTURE-SPINE.md#AD-12; #AD-17-Contract-And-Test-Gates; 1-5-select-provider-and-model-for-hexa.md#Critical-Guardrails]

### Design: Where Each Responsibility Lives (the AD-3 round-trip)

```
Admin/API → Server orchestration (Application/Agents) ── authorize actor (agentsAdmin)
   CONFIG PATH (store only — no dependency reads):
      AgentResponseModeOrchestrator   → dispatch ConfigureAgentResponseMode(Mode)
      AgentApproverPolicyOrchestrator → dispatch ConfigureAgentApproverPolicy(Policy)
            envelope.Extensions (server-populated, client-stripped): actor:agentsAdmin = true
                  │
   pure AgentAggregate.Handle ────────┘  structural validation only → emits
        AgentResponseModeConfigured(Mode, ConfigurationVersion)
        AgentApproverPolicyConfigured(Policy, ApproverPolicyVersion, ConfigurationVersion)

   ACTIVATION PATH (resolve dependencies, then gate):
      AgentActivationProviderRevalidation
            ├─ IProviderCatalogReader  → provider verdict          (1.5)
            ├─ IApproverPolicyResolver → per-source outcomes ──► ApproverPolicyVerdict.Evaluate → ApproverPolicyValidationStatus
            │       • PredefinedParty → IAgentPartyDirectory.ValidateExistingPartyAsync (reuse 1.4)
            │       • TenantRole      → local Tenants projection            (live binding deferred)
            │       • ConversationOwner → V1 facilitator resolver available (live binding deferred)
            └─ dispatch ActivateAgent
                  envelope.Extensions (server-populated, client-stripped):
                    actor:agentsAdmin            = true
                    provider:selectionValidation = <ProviderSelectionValidationStatus>
                    approver:policyValidation    = <ApproverPolicyValidationStatus>
                        │
   pure AgentAggregate.Handle(ActivateAgent) ─┘  ComputeActivationBlockers(... responseMode, hasApproverPolicy, approverPolicyResolved)
```

Rationale for the trusted envelope extension (identical to 1.4 `party:linkValidation` and 1.5 `provider:selectionValidation`): the EventStore gateway routes any aggregate command by domain/type, so a malicious client could POST `ActivateAgent` directly with a spoofed `approver:policyValidation=Valid`. The aggregate therefore requires trusted evidence that resolution happened; the key is server-populated and client-stripped. The aggregate independently fails closed on any non-`Valid`/absent verdict (Task 3) — the security guarantee — while the activation orchestration always resolving the policy gives the durable audit trail (the activation-blocked rejection records the coarse blocker). [Source: ARCHITECTURE-SPINE.md#AD-3; #AD-8; #AD-12; 1-5-select-provider-and-model-for-hexa.md#Design-Where-Each-Responsibility-Lives]

### Current Code State To Preserve

Read these completely before editing — they are the exact patterns to extend (do not reshape 1.3/1.4/1.5 events or contracts):

- `Hexalith.Agents/src/Hexalith.Agents/Agent/AgentAggregate.cs` — `[EventStoreDomain("agent")]`, static `Handle(command, AgentState?, CommandEnvelope) → DomainResult`; helpers `IsAgentAdmin`/`Denied`/`Invalid`/`ReadPartyLinkValidation`/`ReadProviderSelectionValidation`; consts `AgentAdminExtensionKey`, `PartyLinkValidationExtensionKey`, `ProviderSelectionValidationExtensionKey`. Add the two config handlers + `ReadApproverPolicyValidation` + `ApproverPolicyValidationExtensionKey`, and extend `Handle(ActivateAgent)`.
- `Hexalith.Agents/src/Hexalith.Agents/Agent/AgentState.cs` — replay state (`IsCreated`, `ConfigurationVersion`, `Lifecycle`, `PartyId`, `ProviderId`/`ModelId`/`ProviderCapabilityVersion`); `Apply(success)` mutates under `if (!IsCreated) return;`, `Apply(rejection)` is a no-op via `MarkReplayOnlyEventHandled()`. Add the four new fields + two new success `Apply` overloads.
- `Hexalith.Agents/src/Hexalith.Agents/Agent/AgentConfigurationPolicy.cs` — `ComputeActivationBlockers(displayName, instructions, hasPartyIdentity, hasProviderSelection, selectedProviderReady)`; centralizes the gate so the read path and the activation path stay in lock-step (one place updates both callers). Add the three response/approver params + blockers here.
- `Hexalith.Agents/src/Hexalith.Agents/Agent/AgentInspection.cs` — pure read → `AgentStatusView`; `GetStatus` fails closed (`NotAuthorized`/`AgentNotFound`). Add the new view fields + pass the new gate inputs (`approverPolicyResolved: true`); keep it pure (no dependency reads — copy the existing provider boundary comment).
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/AgentActivationBlocker.cs` (`Unknown=0 … ProviderUnavailable=6`) and `AgentStatusView.cs` (trailing `IReadOnlyList<AgentActivationBlocker> ActivationBlockers`) — the exact enum/record shapes to extend additively (preserve ordinals; append fields, keep `ActivationBlockers` last).
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/{PartyLinkValidationStatus,ProviderSelectionValidationStatus}.cs` — the verdict-enum template (`[JsonStringEnumConverter]`, `Unknown = 0` sentinel) to mirror for `ApproverPolicyValidationStatus`. `AgentLifecycleStatus.cs` is the template for `AgentResponseMode`.
- `Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/Commands/{LinkAgentPartyIdentity,SelectAgentProviderModel}.cs`, `Events/{AgentPartyIdentityLinked,AgentProviderModelSelected}.cs` — bare-record + `IEventPayload` marker style, no wall-clock fields. Mirror these for the new commands/events.
- `Hexalith.Agents/src/Hexalith.Agents.Server/{Program.cs, Ports/{IProviderCatalogReader,DeferredProviderCatalogReader,IAgentPartyDirectory,IAgentCommandDispatcher,DeferredAgentCommandDispatcher}.cs, Application/Agents/{AgentProviderSelectionOrchestrator,ProviderSelectionVerdict,AgentActivationProviderRevalidation,AgentActivationRevalidationRequest,AgentProviderSelectionRequest,AgentProviderSelectionOutcome}.cs}` — the host scans `typeof(AgentsAssemblyMarker).Assembly` + `typeof(ServerAssemblyMarker).Assembly`, so new aggregate `Handle` methods auto-register (no wiring needed). `AgentProviderSelectionOrchestrator` + `ProviderSelectionVerdict` + `DeferredProviderCatalogReader` + the activation re-validation step are the exact templates to mirror for the approver-policy resolver/verdict/orchestrations.
- `Hexalith.Agents/tests/Hexalith.Agents.Tests/AgentTestData.cs` — `Envelope<T>(... isAgentsAdmin ...)`, `SelectEnvelope<T>(... provider verdict ...)`, `StateWith*` builders, `ApplyAll` dispatch switch, `ProcessAndApplyAsync`. Extend with response-mode/approver helpers + the new `ApplyAll` cases.

What must be preserved: `.slnx` only, `net10.0`, C# 14, nullable, implicit usings, warnings-as-errors, Central Package Management (no inline versions), provider-SDK-free + secret-value-free + Party-PII-free public contracts, pure replay-safe aggregates, and the **no-`UtcNow`-in-aggregates** convention (occurrence time from EventStore metadata; actor from `envelope.UserId`). [Source: ARCHITECTURE-SPINE.md#Stack; #Consistency-Conventions; 1-5-select-provider-and-model-for-hexa.md#Current-Code-State-To-Preserve]

### Approver-Source → Outcome → Verdict Mapping (orchestration, deterministic precedence)

Per-source resolution (server `IApproverPolicyResolver`, live legs deferred):

| Source kind | Resolved through | `Resolved` when | Non-resolved outcomes |
|---|---|---|---|
| `Caller` | (structural — caller is bound at call time) | always structurally resolvable | — |
| `PredefinedParty` | `IAgentPartyDirectory.ValidateExistingPartyAsync` (reuse 1.4) | Party `Valid` | `Missing`/`Disabled`/`Ambiguous`/`Unavailable`/`Unauthorized` map from `PartyLinkValidationStatus` |
| `TenantRole` | local Tenants projection (deferred) | role exists + enabled in tenant | `Missing` (no such role) / `Unavailable` (projection stale/down) / `Unauthorized` (cross-tenant) |
| `ConversationOwner` | V1 facilitator-resolver availability (deferred) | resolver available | `Unavailable` (resolver absent) |

Policy verdict (`ApproverPolicyVerdict.Evaluate`, worst-first precedence — fail closed):

| Observation | `ApproverPolicyValidationStatus` |
|---|---|
| `policy.Sources` empty | `Incomplete` |
| any source `Unauthorized` | `Unauthorized` (AC3 — never leak/permit another tenant) |
| any source `Unavailable`/`Unknown` | `Unavailable` (stale/degraded — AD-12) |
| any source `Ambiguous` | `Ambiguous` |
| any source `Disabled` | `Disabled` |
| any source `Missing` | `Missing` |
| every source `Resolved` | `Valid` |
| (absent/unparseable verdict in the aggregate) | `Unknown` (aggregate fail-safe → blocks confirmation activation) |

[Source: epics.md#Story-1.6 AC2/AC3; ARCHITECTURE-SPINE.md#AD-8; #AD-12; 1-5-select-provider-and-model-for-hexa.md#Provider-Readiness-Verdict-Mapping]

### Readiness / UX Context (AC1, AC3) — contracts only; no UI in this story

- The `agent-readiness-badge` "combines Agent lifecycle, Party identity, provider/model readiness, instructions validity, **response mode, and approver policy completeness** into a readable readiness indicator … Use `status-success` only when all required readiness gates pass" and "must explain blockers, not hide them." This story contributes the `MissingResponseMode`/`MissingApproverPolicy`/`ApproverPolicyUnresolvable` blocker inputs and the `ResponseMode`/`HasApproverPolicy`/`ApproverPolicyDisclosure`/`ApproverPolicyVersion` status-view fields the 1.8 badge/overview will consume. [Source: DESIGN.md#agent-readiness-badge (UX-DR20); EXPERIENCE.md#agent-readiness]
- Canonical readiness vocabulary the 1.8 UI maps these to: `invalid configuration` ("Required Agent fields, instructions, response mode, or policy missing") for the missing-mode/missing-policy gates; blocked-readiness/unavailable-dependency uses `status-severe`; ambiguous authority / missing policy basis uses `status-important`. Keep this story's contracts as enum verdicts + safe ids/booleans (no prose) so 1.8 maps them to **whole localizable strings with named placeholders** — never runtime-assembled sentence fragments (UX-DR14). [Source: EXPERIENCE.md#agent-readiness; DESIGN.md#colors (status-severe, status-important); DESIGN.md#localization (UX-DR14)]
- `response-mode-toggle` is "a clear mutually exclusive choice … must not make automatic mode look 'better' by visual weight … mode choice is policy, not a product upsell" with explicit future-only copy (UX-DR23); `approver-policy-builder` rows carry "a readable basis and availability state. Missing or ambiguous policy sources render as blocked, not as empty success" (UX-DR5). UI is **Story 1.8** — this story only ships the contracts those components consume. [Source: DESIGN.md#response-mode-toggle (UX-DR23); DESIGN.md#approver-policy-builder (UX-DR5); EXPERIENCE.md#uj-1---nora-configures-hexa-for-a-tenant-launch (steps 6–7)]

### Sensitive-Data Handling (AD-9 / AD-14 / FR-7)

- `AgentResponseMode`, `ApproverPolicySourceKind`, `ApproverPolicyBasisDisclosure`, `ApproverPolicyValidationStatus`, the `ApproverPolicyVersion`, a predefined-source `PartyId` (a **reference, not PII** — AD-7), and a `TenantRole` name are all **safe** — store and carry them freely on commands/events/state/status. The disclosure category is itself safe metadata; it governs how *later* proposal-basis is shown, and is not a secret.
- No Party display name / contact / personal-data object, no tenant membership PII, no provider secret/SDK type, and no raw content may enter Agents events, state, status, logs, telemetry, or audit summaries (AD-7, AD-9, AD-14). The approver-policy resolver returns only safe per-source *outcomes* — Party/Tenant/Conversation PII never crosses the port boundary (mirror `AgentPartyValidationResult`). [Source: ARCHITECTURE-SPINE.md#AD-14-Sensitive-Content-And-Secret-Safety; #AD-7; prd.md#4-7]
- The Story 1.1 secret-non-disclosure guard (forbidden member tokens `Secret`/`ApiKey`/`Credential`/`Password`/`ConnectionString`) still applies — none are needed here; keep new member names clear of them and do not weaken the guard. [Source: 1-5-select-provider-and-model-for-hexa.md#Sensitive-Data-Handling]

### Idempotency (AD-13)

- Re-applying the same response mode via `ConfigureAgentResponseMode` is a deterministic `NoOp()` — no duplicate event, no version bump.
- Re-applying an equal approver policy (same ordered sources + disclosure) via `ConfigureAgentApproverPolicy` is a deterministic `NoOp()`.
- A genuine mode/policy change emits exactly one new event and bumps the relevant version(s) by one. [Source: ARCHITECTURE-SPINE.md#AD-13-Idempotent-External-Effects]

### Latest Technical Information

- **No new references or NuGet packages this story.** Parties is already referenced (the `PredefinedParty` leg reuses `IAgentPartyDirectory`); the Tenants-projection and Conversations-facilitator live legs are **deferred** via `DeferredApproverPolicyResolver`, mirroring how 1.2/1.4/1.5 deferred their read-model/dispatch bindings. NSubstitute `5.3.0` is already centralized for the orchestration unit tests. Do NOT add Agent Framework, provider SDK, Dapr-runtime, Tenants/Conversations clients, or UI packages. [Source: ARCHITECTURE-SPINE.md#Stack; Hexalith.Agents/Directory.Packages.props; Program.cs]
- Stack baseline unchanged: .NET `10.0.300`–`10.0.301`, `net10.0`, `.slnx`, Central Package Management, xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute `5.3.0`, warnings-as-errors. **Latency targets for Automatic vs Confirmation are explicitly out of scope** for 1.6 — they are launch-readiness gate inputs (PRD OQ-5 / FR-28), not configuration logic. [Source: ARCHITECTURE-SPINE.md#Stack; prd.md#12-open-questions-and-deferred-decisions (OQ-5); prd.md#4-10-content-safety-and-launch-readiness (FR-28)]

### Testing Requirements

- Aggregate (response mode): configure Automatic/Confirmation success (records mode, bumps `ConfigurationVersion`, lifecycle unchanged) / reject `Unknown` mode / idempotent same-mode `NoOp` / change mode → new event + bumped version / not-found `AgentNotFoundRejection` / unauthorized `AgentAdministrationDeniedRejection` / replay.
- Aggregate (approver policy): configure success (records sources + disclosure, bumps `ApproverPolicyVersion` + `ConfigurationVersion`) / structural rejects → `InvalidAgentConfigurationRejection` / empty policy storable / idempotent identical `NoOp` / changed policy → new event + bumped policy version + prior preserved / not-found / unauthorized / replay.
- Activation gate: blocked-by-`MissingResponseMode`; Automatic activatable without policy; Confirmation blocked-by-`MissingApproverPolicy`; Confirmation blocked-by-`ApproverPolicyUnresolvable` (non-`Valid`/absent verdict); Confirmation unblocked when policy present + `Valid` verdict; verdict-parse fail-closed theory (`"1"`/`"01"`/`"valid"`/`" Valid"` → `Unknown`). **Plus** the prior-activation-test updates (1.3/1.4/1.5 + E2E) to configure a mode (Automatic) before activating.
- Contracts: marker interfaces; JSON round-trip for the new events + nested `AgentApproverPolicy`/`ApproverPolicySource` + extended `AgentStatusView`; all new enums + the three new blockers serialize by name; unknown → sentinel; new members are id-reference/enum/version-shaped only (no secret/PII member); secret guard green.
- Server: `ApproverPolicyVerdict` precedence (incl. `Incomplete`, `Unauthorized`, `Unavailable`, `Ambiguous`, `Disabled`, `Missing`, all-`Resolved` `Valid`); the two config orchestrators authorize + strip-reserved + dispatch; extended activation re-validation populates `approver:policyValidation` for Confirmation and leaves `Unknown` for Automatic; deferred resolver placeholder throws; structural/boundary guards green.
- Build/test commands (run from `Hexalith.Agents/`):
  - `dotnet restore Hexalith.Agents.slnx`
  - `dotnet build Hexalith.Agents.slnx --configuration Release` (0 warnings / 0 errors, warnings-as-errors)
  - `dotnet test tests/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj --configuration Release`
  - `dotnet test tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release`
  - `dotnet test tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj --configuration Release`

### Project Structure Notes

- New/changed code:
  - `Hexalith.Agents.Contracts/Agent/`: `AgentResponseMode.cs`; `ApproverPolicySourceKind.cs`; `ApproverPolicyBasisDisclosure.cs`; `ApproverPolicyValidationStatus.cs`; `ApproverPolicySource.cs`; `AgentApproverPolicy.cs`; `Commands/ConfigureAgentResponseMode.cs`; `Commands/ConfigureAgentApproverPolicy.cs`; `Events/AgentResponseModeConfigured.cs`; `Events/AgentApproverPolicyConfigured.cs`; edits to `AgentActivationBlocker.cs` (+`MissingResponseMode`, +`MissingApproverPolicy`, +`ApproverPolicyUnresolvable`) and `AgentStatusView.cs` (+`ResponseMode`, +`HasApproverPolicy`, +`ApproverPolicyDisclosure`, +`ApproverPolicyVersion`).
  - `Hexalith.Agents/Agent/`: edits to `AgentState.cs` (+4 fields, +2 `Apply`), `AgentAggregate.cs` (+2 config handlers, +`ReadApproverPolicyValidation`, +`ApproverPolicyValidationExtensionKey`, +response/approver gate in `Handle(ActivateAgent)`), `AgentConfigurationPolicy.cs` (+3 params/blockers), `AgentInspection.cs` (+view fields + gate inputs).
  - `Hexalith.Agents.Server/`: `Ports/{IApproverPolicyResolver,ApproverPolicyResolutionResult,DeferredApproverPolicyResolver}.cs`; `Application/Agents/{ApproverPolicyVerdict,AgentResponseModeOrchestrator,AgentApproverPolicyOrchestrator + their request/outcome records}.cs`; edits to `AgentActivationProviderRevalidation.cs` + `AgentActivationRevalidationRequest.cs` (+approver resolution leg, +`IApproverPolicyResolver` dep, +recorded response-mode/policy on the request) and `Program.cs` (register resolver + two orchestrations).
  - Tests across `Hexalith.Agents.Tests` (response mode + approver policy + activation-gate + updated prior activation tests), `Hexalith.Agents.Contracts.Tests`, `Hexalith.Agents.Server.Tests`.
- Discovery loaded: root `epics.md` (Epic 1, Story 1.6 + cross-stories 1.5/1.7/1.8 + UX-DR5/UX-DR14/UX-DR20/UX-DR23), PRD (FR-6/FR-7 + FR-13/FR-19/FR-20/FR-21/FR-24 + non-goals + OQ-5/FR-28 latency-out-of-scope), architecture spine (AD-1/AD-2/AD-3/AD-4/AD-8/AD-12/AD-13/AD-14/AD-17 + Stack + Consistency Conventions), UX DESIGN/EXPERIENCE (agent-readiness-badge, response-mode-toggle, approver-policy-builder, canonical readiness states, localization, UJ-1), and the as-built Story 1.1–1.5 Agent module code (aggregate/state/policy/inspection/contracts + Server orchestrations/ports + test fixtures). No root `project-context.md` exists for `agents`; sibling-module `project-context.md` files supply carry-forward rules (aggregate purity, no-`UtcNow`-in-aggregates, FrontComposer/Fluent inherited UI, deferred read-path/dispatch bindings).

### References

- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.6-Configure-Response-Mode-And-Approver-Policy]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.7-Configure-Content-Safety-Policy-And-Activation-Gate]
- [Source: _bmad-output/planning-artifacts/epics.md#Story-1.8-Admin-Setup-UI-And-Readiness-Overview]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#4-3-response-policy-and-approver-configuration]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#4-6-proposed-agent-reply-workflow]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#4-7-authorization-tenant-isolation-and-governance]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#4-9-audit-evidence-and-operational-visibility]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#4-10-content-safety-and-launch-readiness]
- [Source: _bmad-output/planning-artifacts/prds/prd-agents-2026-06-23/prd.md#12-open-questions-and-deferred-decisions]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-2-Aggregate-Boundaries]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-3-Pure-Aggregates-Side-Effects-Outside]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-4-Interaction-Snapshot]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-8-Approver-Policy-Resolution]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-12-Authorization-And-Dependency-Uncertainty]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-13-Idempotent-External-Effects]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-14-Sensitive-Content-And-Secret-Safety]
- [Source: _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md#AD-17-Contract-And-Test-Gates]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md#agent-readiness-badge]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md#response-mode-toggle]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/DESIGN.md#approver-policy-builder]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#agent-readiness]
- [Source: _bmad-output/planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#uj-1---nora-configures-hexa-for-a-tenant-launch]
- [Source: _bmad-output/implementation-artifacts/1-5-select-provider-and-model-for-hexa.md#Design-Where-Each-Responsibility-Lives]
- [Source: _bmad-output/implementation-artifacts/1-4-link-hexa-to-a-party-identity.md#Current-Code-State-To-Preserve]
- [Source: Hexalith.Agents/src/Hexalith.Agents/Agent/AgentAggregate.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents/Agent/AgentState.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents/Agent/AgentConfigurationPolicy.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents/Agent/AgentInspection.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/AgentActivationBlocker.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/AgentStatusView.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Contracts/Agent/ProviderSelectionValidationStatus.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Application/Agents/AgentProviderSelectionOrchestrator.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Application/Agents/ProviderSelectionVerdict.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Application/Agents/AgentActivationProviderRevalidation.cs]
- [Source: Hexalith.Agents/src/Hexalith.Agents.Server/Ports/IAgentPartyDirectory.cs]

## Dev Agent Record

### Agent Model Used

Claude Opus 4.8 (1M context) — claude-opus-4-8[1m]

### Debug Log References

- `dotnet build Hexalith.Agents.slnx --configuration Release` → Build succeeded, 0 Warning(s) / 0 Error(s) (warnings-as-errors).
- `dotnet test tests/Hexalith.Agents.Tests` → Passed! 233/233.
- `dotnet test tests/Hexalith.Agents.Contracts.Tests` → Passed! 72/72.
- `dotnet test tests/Hexalith.Agents.Server.Tests` → Passed! 90/90.
- (Counts reflect the QA-gap-fill tests and the senior-review fail-closed hardening test added after the initial dev pass; total 395.)

### Completion Notes List

- **Contracts (Task 1):** Added the four safe enums (`AgentResponseMode`, `ApproverPolicySourceKind`, `ApproverPolicyBasisDisclosure`, `ApproverPolicyValidationStatus` — all `[JsonStringEnumConverter]`, `Unknown=0` fail-safe sentinels), the two value objects (`ApproverPolicySource`, `AgentApproverPolicy`), the two commands and two `IEventPayload` events, and additively extended `AgentActivationBlocker` (`MissingResponseMode=7`, `MissingApproverPolicy=8`, `ApproverPolicyUnresolvable=9`, ordinals preserved) and `AgentStatusView` (+`ResponseMode`/`HasApproverPolicy`/`ApproverPolicyDisclosure`/`ApproverPolicyVersion`, `ActivationBlockers` kept last). Contracts stay inward-only; the Story 1.1 secret-non-disclosure guard remains green and unweakened.
- **Aggregate (Tasks 2–4):** `AgentState` gained 4 durable fields + 2 replay-safe `Apply` overloads (guarded by `IsCreated`). `AgentAggregate` gained the `approver:policyValidation` trusted extension key + `ReadApproverPolicyValidation` (exact-name, case-sensitive, fail-closed to `Unknown`), the two pure config handlers (response-mode `Unknown`-reject + idempotent NoOp; approver-policy structural validation/normalization + duplicate rejection + idempotent NoOp), and the extended `Handle(ActivateAgent)` feeding response-mode + approver-policy readiness into `ComputeActivationBlockers`. `AgentConfigurationPolicy` appends the three gates in deterministic order (Automatic needs no approver policy); `AgentInspection` surfaces the new view fields and trusts the last-configured policy (`approverPolicyResolved: true`) to stay pure (AD-3).
- **Server (Tasks 5–6):** `IApproverPolicyResolver` + server-internal `ApproverPolicyResolutionResult`/`ApproverSourceResolution`/`ApproverSourceOutcome`; the pure fail-closed `ApproverPolicyVerdict.Evaluate` (worst-first precedence); `DeferredApproverPolicyResolver` placeholder; the two thin config orchestrations (authorize → strip reserved keys → dispatch with server-populated `actor:agentsAdmin` only); and the extended `AgentActivationProviderRevalidation` (now injects `IApproverPolicyResolver` as a 3rd dependency, re-resolves the recorded policy only in Confirmation mode with ≥1 source, and populates `approver:policyValidation`). Registered in `Program.cs`.
- **Deferred (as designed):** the live Tenants-projection / Conversations-facilitator / Parties bindings for `IApproverPolicyResolver` remain deferred via `DeferredApproverPolicyResolver` (mirroring 1.2/1.4/1.5); the `PredefinedParty` leg can reuse the live `PartiesAgentPartyDirectory` once the read-model/topology story wires the others. No new NuGet/SDK/sibling-module references were added.
- **Prior-test updates (Task 7):** activation now also gates on response mode (and, for Confirmation, approver policy). `StateWithSelectedProvider` was extended to record Automatic mode (it is documented as the "fully readiness-cleared" state), and the E2E/inspection/party/provider activation tests were updated to configure a mode before activating and to expect `MissingResponseMode` in the documented blocker order. Story 1.5's `AgentActivationProviderRevalidation` constructor call sites were updated for the added resolver dependency.

### File List

**Added — `Hexalith.Agents.Contracts`:**
- src/Hexalith.Agents.Contracts/Agent/AgentResponseMode.cs
- src/Hexalith.Agents.Contracts/Agent/ApproverPolicySourceKind.cs
- src/Hexalith.Agents.Contracts/Agent/ApproverPolicyBasisDisclosure.cs
- src/Hexalith.Agents.Contracts/Agent/ApproverPolicyValidationStatus.cs
- src/Hexalith.Agents.Contracts/Agent/ApproverPolicySource.cs
- src/Hexalith.Agents.Contracts/Agent/AgentApproverPolicy.cs
- src/Hexalith.Agents.Contracts/Agent/Commands/ConfigureAgentResponseMode.cs
- src/Hexalith.Agents.Contracts/Agent/Commands/ConfigureAgentApproverPolicy.cs
- src/Hexalith.Agents.Contracts/Agent/Events/AgentResponseModeConfigured.cs
- src/Hexalith.Agents.Contracts/Agent/Events/AgentApproverPolicyConfigured.cs

**Modified — `Hexalith.Agents.Contracts`:**
- src/Hexalith.Agents.Contracts/Agent/AgentActivationBlocker.cs (+3 appended values)
- src/Hexalith.Agents.Contracts/Agent/AgentStatusView.cs (+4 appended fields, ActivationBlockers kept last)

**Modified — `Hexalith.Agents` (domain):**
- src/Hexalith.Agents/Agent/AgentState.cs (+4 fields, +2 Apply overloads)
- src/Hexalith.Agents/Agent/AgentAggregate.cs (+2 handlers, +extension key/reader, +validation helper, +activation gate inputs)
- src/Hexalith.Agents/Agent/AgentConfigurationPolicy.cs (+3 gate params/blockers)
- src/Hexalith.Agents/Agent/AgentInspection.cs (+view fields + gate inputs)

**Added — `Hexalith.Agents.Server`:**
- src/Hexalith.Agents.Server/Ports/IApproverPolicyResolver.cs
- src/Hexalith.Agents.Server/Ports/ApproverPolicyResolutionResult.cs
- src/Hexalith.Agents.Server/Ports/DeferredApproverPolicyResolver.cs
- src/Hexalith.Agents.Server/Application/Agents/ApproverPolicyVerdict.cs
- src/Hexalith.Agents.Server/Application/Agents/AgentResponseModeOrchestrator.cs
- src/Hexalith.Agents.Server/Application/Agents/AgentResponseModeRequest.cs
- src/Hexalith.Agents.Server/Application/Agents/AgentApproverPolicyOrchestrator.cs
- src/Hexalith.Agents.Server/Application/Agents/AgentApproverPolicyRequest.cs

**Modified — `Hexalith.Agents.Server`:**
- src/Hexalith.Agents.Server/Application/Agents/AgentActivationProviderRevalidation.cs (+resolver dependency, +approver leg, +approver extension key)
- src/Hexalith.Agents.Server/Application/Agents/AgentActivationRevalidationRequest.cs (+ResponseMode/RecordedApproverPolicy, +approver verdict on outcome)
- src/Hexalith.Agents.Server/Program.cs (register resolver + 2 orchestrations)

**Added — tests:**
- tests/Hexalith.Agents.Tests/AgentResponseModeTests.cs
- tests/Hexalith.Agents.Tests/AgentApproverPolicyTests.cs
- tests/Hexalith.Agents.Tests/AgentResponseApproverActivationTests.cs
- tests/Hexalith.Agents.Contracts.Tests/AgentResponseModeApproverPolicyContractsTests.cs
- tests/Hexalith.Agents.Server.Tests/ApproverPolicyVerdictTests.cs
- tests/Hexalith.Agents.Server.Tests/AgentResponseModeOrchestratorTests.cs
- tests/Hexalith.Agents.Server.Tests/AgentApproverPolicyOrchestratorTests.cs
- tests/Hexalith.Agents.Server.Tests/AgentActivationApproverRevalidationTests.cs

**Modified — tests:**
- tests/Hexalith.Agents.Tests/AgentTestData.cs (+constants/sample policy/ActivateEnvelope/StateWith* helpers; +2 ApplyAll cases; StateWithSelectedProvider records Automatic mode)
- tests/Hexalith.Agents.Tests/AgentAggregateTests.cs (activation E2E configures a mode)
- tests/Hexalith.Agents.Tests/AgentPartyIdentityTests.cs (activation paths configure a mode; blocker order updated)
- tests/Hexalith.Agents.Tests/AgentProviderSelectionTests.cs (activation path configures Automatic mode before activating)
- tests/Hexalith.Agents.Tests/AgentInspectionTests.cs (+response-mode/approver view assertions)
- tests/Hexalith.Agents.Tests/AgentLifecycleE2ETests.cs (configure a mode before activation; blocker/version expectations updated)
- tests/Hexalith.Agents.Contracts.Tests/AgentContractsRoundTripTests.cs (AgentStatusView constructor + new field assertions)
- tests/Hexalith.Agents.Server.Tests/AgentProviderSelectionOrchestratorTests.cs (revalidation constructor + named ClientSuppliedExtensions)

## Change Log

| Date | Version | Description |
|------|---------|-------------|
| 2026-06-24 | 1.0 | Story 1.6 implemented: response-mode + approver-policy configuration on the `Agent` aggregate, the Confirmation-mode activation readiness gates (fail-closed via the trusted `approver:policyValidation` verdict), and the server-side approver-policy resolver/verdict/orchestrations (live dependency bindings deferred). All tasks complete; 385 tests pass (225 + 72 + 88); full solution builds with 0 warnings / 0 errors. Status → review. |
| 2026-06-24 | 1.1 | Senior Developer Review (AI, auto-fix). All 4 ACs verified implemented; all tasks confirmed done; tests are real assertions. Fixes: (1) `ApproverPolicyVerdict.Evaluate` now fails closed to `Unavailable` when the resolution does not cover every configured source (a partial/degraded read can no longer fail open to `Valid` — AC3/AD-12), with a new precedence test; the under-specified mock in `AgentActivationApproverRevalidationTests.Client_forged_approver_verdict...` was made realistic (one outcome per source, still → `Missing`). (2) File List corrected to include `AgentProviderSelectionTests.cs`. (3) Debug Log counts refreshed to actual (233 + 72 + 90 = 395). Build 0/0; 395 tests pass. Status remains `done` (no CRITICAL issues). |

## Senior Developer Review (AI)

**Reviewer:** Jérôme Piquot (automated adversarial review) — 2026-06-24
**Outcome:** Approved (auto-fix applied). Story status → `done`.

### Scope verified
- Read every file in the File List plus the git diff vs baseline `c9fc081`. All claimed-changed files were genuinely changed; no story-claimed file lacked git evidence.
- All 4 Acceptance Criteria validated against the implementation: **AC1** (response-mode records mode + configuration version, future-only) — `ConfigureAgentResponseMode` handler/event/state/gate; **AC2** (Confirmation configures all V1 approver sources incl. facilitator-based `ConversationOwner`) — `ApproverPolicySource` kinds + structural validation; **AC3** (fail-closed Confirmation activation when a source is unresolvable) — trusted `approver:policyValidation` verdict, exact-name fail-closed parse, deterministic gate order; **AC4** (policy version + disclosure category for policy-basis reporting) — `ApproverPolicyVersion` + `DisclosureCategory` on event/state/view. All IMPLEMENTED.
- Every `[x]` task audited against code: all genuinely done. Tests are real assertions (precedence, security boundary, replay, idempotency, fail-closed parse), not placeholders. Build 0 warnings / 0 errors; 395 tests pass (233 + 72 + 90). Story-1.1 secret-non-disclosure guard remains green.

### Findings & fixes (auto-fixed — no CRITICAL/HIGH)
- **MEDIUM (fail-closed hardening):** `ApproverPolicyVerdict.Evaluate` only special-cased an *empty* policy; a future live resolver returning fewer per-source outcomes than configured sources could fail **open** to `Valid`, violating the story's explicit "never resolves to Valid under any uncertainty" mandate (AC3/AD-12). Added a `result.Sources.Count != policy.Sources.Count → Unavailable` guard + a precedence test; corrected the one under-specified mock that returned a partial resolution.
- **MEDIUM (documentation):** `AgentProviderSelectionTests.cs` was modified (required by Task 7) but missing from the File List — added.
- **LOW (documentation):** Debug Log / Change Log test counts were stale (385) versus actual (395, after QA-gap-fill + the review hardening test) — refreshed.

## Story Completion Status

Implementation complete and verified — all tasks/subtasks checked, all acceptance criteria satisfied, full regression suite green (395 tests), senior review fixes applied. Story status is `done`.
