---
title: Brownfield Ratification / Drift Review - Architecture Spine
lens: brownfield-ratification-drift
target: ../ARCHITECTURE-SPINE.md
target_updated: 2026-09-09
reviewed: 2026-09-09
reviewer: independent architecture reviewer (validation gate)
repository_head: 1c40632
prior_review: review-2026-09-08-brownfield-drift.md (B-1..B-18)
---

# Brownfield Drift Review - ARCHITECTURE-SPINE.md (2026-09-09)

**Gate verdict: FAILS TO RATIFY.** The 2026-09-09 re-distillation fixed the repository picture the 2026-09-08 review faulted (host projects, register status, projection ids, Stack table, seed shape, EXT-CONV-UI-1, trusted-verdict conventions: ten of eighteen B-findings are resolved), and its memlog honestly records six accepted implementation gaps. But four further places where checked-in code contradicts a decision are neither ratified nor recorded as accepted gaps, and two of them sit on the security and identity seams (AD-30 ingress authority, AD-29 identity formula). The memlog also claims to have "ratified" a shipped derivation (`AgentInteractionIdentity`) while specifying a different one.

Counts: critical 0 / high 4 / medium 7 / low 7 (18 findings). Prior findings: 10 resolved, 4 partial, 4 unresolved.

Method: every claim in the spine about existing reality (Structural Seed, Stack, aggregate inventory, projection ids, identity derivations, principals, time handling, enums, routes, policies, seams, consumer lists) was checked against `src/`, `test/`, `Hexalith.Agents.slnx`, root build files, `git submodule status`, `references/Hexalith.Builds/Props/Directory.Packages.props`, `epics.md`, `external-dependency-register.md`, `launch-readiness-register.md`, `sprint-status.yaml`, and `deferred-work.md` at `1c40632` (last code commit `a191d24`, Story 5.3).

Classification vocabulary: **stale claim** (spine/memlog asserts something the repository does not show), **code-diverged-from-AD** (shipped code contradicts a rule; noted whether the memlog already records it as an accepted gap), **unratified convention** (code follows a pattern the spine should name but does not), **expected-not-yet-built** (rule describes Epic 5-8 work; no drift).

---

## Findings

### HIGH

#### N-1 - AD-2 says the `Agent` aggregate stores no safety policy; the shipped `Agent` aggregate owns the content-safety policy and its version
- **AD / section:** AD-2 ("The `Agent` aggregate stores no safety policy; the interaction snapshot's safety version is the pair (`ContentSafetyPolicy` version, `TenantGovernancePolicy` version)"); AD-4; AD-20; class diagram (`Agent` has no safety member, `ContentSafetyPolicy` is a `system` aggregate).
- **Observed reality:** `src/Hexalith.Agents/Agent/AgentAggregate.cs:538-580` handles `ConfigureAgentContentSafetyPolicy` and emits `AgentContentSafetyPolicyConfigured` with `ContentSafetyPolicyVersion + 1`; `src/Hexalith.Agents/Agent/AgentState.cs:99-102` holds `ContentSafety` and `ContentSafetyPolicyVersion`; activation requires `state.ContentSafety is not null` (`AgentAggregate.cs:251-264`). The interaction snapshot carries one integer `ContentSafetyPolicyVersion` (`src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionSnapshot.cs:46`), not a pair. No `ContentSafetyPolicy` or `TenantGovernancePolicy` aggregate, command, or event exists (`find src -name '*ContentSafetyPolicy*'` returns only the Agent-scoped contracts). Endpoint `POST /agents/{agentId}/content-safety-policy` (`AgentsOperationEndpoints.cs:80`) and orchestrator `AgentContentSafetyPolicyOrchestrator` ship the Agent-scoped model.
- **Memlog:** H-1 (2026-09-09) records the decision text verbatim but no `(constraint) Brownfield gap` entry for the Agent-held policy; no owner story. `epics.md` Story 8.4 (`L2381-2434`) names `ContentSafetyPolicyAggregateTests` but nothing about migrating the Agent-held configuration or the snapshot pair.
- **Classification:** code-diverged-from-AD, **not recorded** as an accepted gap.
- **Disposition:** discuss. Either add a memlog constraint mirroring the catalog one ("shipped `Agent` owns `AgentContentSafetyConfiguration` and a single `ContentSafetyPolicyVersion`; non-conformant with AD-2/AD-4; migrated to the `system` `ContentSafetyPolicy` aggregate plus tenant restrictions in `TenantGovernancePolicy` by Story 8.4, snapshot pair by Story 6.3") and name the owner in `epics.md`, or amend AD-2 to allow a transitional Agent-scoped safety configuration. Leaving it silent means Story 6.3 (first live safety decision) has no instruction on which version(s) to snapshot.

#### N-2 - AD-17 makes `LaunchReadinessGate` the only readiness writer; the shipped writer is the `Agent` aggregate, and the B-6 gap note was removed rather than carried into the memlog
- **AD / section:** AD-17 ("The `LaunchReadinessGate` aggregate is the only writer for each logical key"); AD-2 inventory; Capability map row "Launch readiness and release qualification".
- **Observed reality:** `src/Hexalith.Agents/Agent/AgentAggregate.cs:587-636` handles `RecordAgentLaunchReadiness` and stores `AgentLaunchReadiness` with `LaunchReadinessVersion` on `AgentState.cs:109-112`; `EnableProductionLikeGeneration` (`AgentAggregate.cs:637-673`) gates on it; the public route `POST /agents/{agentId}/launch-readiness` (`AgentsOperationEndpoints.cs:82`) and `GET /status/agents/{agentId}/launch-readiness` (`:137`), `LaunchReadiness.razor`, `ILaunchReadinessGateway`, `AgentLaunchReadinessPolicy/Inspection` all ship the Story 4.4 shape. No `LaunchReadinessGate`, `ReadinessObservation`, `OperationGateMatrix`, `OperationFamily`, or `LR-*` string exists in `src/` (0 hits). `EnableProductionLikeGeneration` is a command on an aggregate whose AD-2 inventory does not list it.
- **Memlog:** H-16 records that "every dated status note is removed from AD rules"; the 2026-09-09 entries contain no constraint for the Agent-held readiness record (grep `RecordAgentLaunchReadiness|Story 4.4` in `.memlog.md`: 0 hits). B-6 asked for exactly that note; the fix chosen (strip notes, keep history in memlog) was applied to the spine but the memlog never received the entry.
- **Classification:** code-diverged-from-AD (historical Epic 4 shape), **not recorded**; regression of B-6.
- **Disposition:** autofix. Append a memlog constraint: "Brownfield gap 2026-09-09: `RecordAgentLaunchReadiness`, `AgentLaunchReadiness`, `EnableProductionLikeGeneration`, and the `/launch-readiness` routes are the Story 4.4 predecessor; non-conformant with AD-17's sole-writer rule; replaced by the `LaunchReadinessGate` aggregate in Story 5.5 and consumed for activation in Story 5.7 (`epics.md:1377,1404`)."

#### N-3 - AD-29 claims to ratify the shipped identity canonicalizer but specifies a different hash input for `AgentInteractionId`, and three shipped derivations outside the memlog's two recorded gaps diverge or are unnamed
- **AD / section:** AD-29 ("One shared `AgentsIdentity` canonicalizer ... purpose tag as the first component ... `AgentInteractionId = H(interaction, TenantId, AgentId, SourceConversationId, CallerPartyId, ClientIdempotencyKey)` ... `ProposalVersionId = H(version, AgentInteractionId, VersionOrdinal, Kind)`"); memlog H-5 ("ratified from the shipped AgentInteractionIdentity").
- **Observed reality:**
  | Identity | Shipped derivation | AD-29 | Memlog |
  |---|---|---|---|
  | `AgentInteractionId` | `SHA256(len:tenant␟len:agent␟len:conv␟len:caller␟len:key␟)` with **no purpose tag** (`src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionIdentity.cs:39-47`) | `H(interaction, ...)` purpose tag first | claims ratified - **stale claim**; adopting AD-29 changes every interaction id |
  | `AttemptId` (generation) | `"attempt-" + interactionId` (`AgentInteractionGenerationOrchestrator.cs:339`) | `H(attempt, TenantId, AgentInteractionId, AttemptOrdinal)` | recorded (constraint L97) |
  | regeneration `AttemptId` / `VersionId` | `H(proposal-regeneration-attempt-id|version-id, interactionId, sourceConversationId, regenerationAttemptId)` (`AgentProposalRegenerationIdentity.cs:36-45`) | ordinal-based | recorded (constraint L97) |
  | generated `VersionId` | `"version-" + attemptId` computed **inside the domain twin-policy** (`src/Hexalith.Agents/AgentInteraction/AgentOutputGenerationPolicy.cs:104`) | `H(version, AgentInteractionId, VersionOrdinal, Kind)` by the shared Server canonicalizer | **not recorded** |
  | edited `VersionId` | `H(proposal-edit-version-id, interactionId, sourceVersionId, editAttemptId)` (`AgentProposalEditIdentity.cs:32-42`) | `H(version, AgentInteractionId, VersionOrdinal, Kind)` | **not recorded** |
  | `ProposalId` | `H(proposal-id, interactionId, versionId)` (`AgentProposalIdentity.cs:31`); carried on `ProposedAgentReplyCreated` and every proposal command | **absent from AD-29 and the Identity convention row** | **not recorded** |
  | `MessageId` / posting key | `H(message-id|idempotency-key, interactionId, versionId)` (`AgentResponsePostingIdentity.cs:30-38`) | identical | conforms |
  The component framing itself (`len:value␟`, UTF-8, SHA-256, lowercase hex) matches AD-29 in all five classes; only the tag/tuple choices differ.
- **Classification:** stale claim (memlog H-5) + code-diverged-from-AD, three derivations unrecorded (generated version, edited version, `ProposalId`).
- **Disposition:** discuss. (a) Correct memlog H-5: the shipped `AgentInteractionIdentity` is the *framing* ratified, its tuple gains the `interaction` tag under AD-29 and Story 6.1 (first live interaction) owns the cut-over, or drop the tag for that one id to keep ids stable. (b) Add `ProposalId = H(proposal, AgentInteractionId, VersionId)` to AD-29 or state it is subsumed by `ProposalVersionId`. (c) Record the generated/edited version-id gaps with owners (7.2 edit, 6.4 generation) and note that `AgentOutputGenerationPolicy.DeriveVersionId` violates the "one shared canonicalizer" rule by living in the domain assembly.

#### N-4 - AD-30 Administrator principal is described as populated "after a fresh Tenants-projection role check"; the shipped ingress is a JWT role check, and it also grants Platform-Operator-only catalog authority to the tenant administrator
- **AD / section:** AD-30 ("`Administrator` (a reserved `actor:*` extension populated only by the Agents API ingress after a fresh Tenants-projection role check)"; "`Platform` (the `system`-tenant Platform Operator)"; catalog mutation is Platform-only per AD-2/AD-9/memlog C-2); AD-12 ("Prevents: JWT-only or UI-only authorization").
- **Observed reality:** `src/Hexalith.Agents.Server/Ports/HttpAgentAdministrationContextProvider.cs:24,40-51` resolves tenant from the `tenantId|tenant_id|tid|tenant` claim and authority from `user.IsInRole("Agents.Administrator")` - claims only, no Tenants projection read. That single `AgentAdministrationContext` is what `EventStoreProviderCatalogOperations.cs:149,187` consults before `ProviderCatalogAdministrationOrchestrator.cs:135` stamps `actor:agentsProviderAdmin = "true"`, so a tenant `Agents.Administrator` mutates the catalog. `ProviderCatalog.razor:2` is `[Authorize(Policy = AgentsFrontComposerRegistration.AgentsAdministratorPolicy)]`. The `Agents.PlatformOperator` policy named by AD-30 does not exist (`AgentsFrontComposerRegistration.cs:23-61` defines `Agents.Administrator`, `Agents.Approver`, `Agents.Operator`, `Agents.AuditOperator` only). `deferred-work.md` DW-2 (open) records the query-side half of this (setup queries authorize through the deferred `ITenantAccessReader`).
- **Memlog:** the 2026-09-09 catalog constraint (L91) records the *tenant-scope* gap (aggregate id = tenant id) but not the *authority* gap (who may mutate); H-2 "ratifies the shipped actor:agentsAdmin / actor:agentsProviderAdmin pattern as Administrator extensions" without noting that today's population is JWT-only. DW-2 is not cited anywhere in the spine or memlog (grep 0).
- **Classification:** code-diverged-from-AD, partially recorded (scope yes, authority and ingress mechanism no).
- **Disposition:** discuss. Add a memlog constraint: "Shipped ingress (`HttpAgentAdministrationContextProvider`) derives Administrator from the JWT role only and grants catalog mutation to `Agents.Administrator`; non-conformant with AD-30/AD-12/AD-2; Story 5.4 binds the Tenants-projection role check (DW-2), Story 5.5 introduces `Agents.PlatformOperator` and re-scopes catalog mutation." The spine's AD-30 rule stands; what is missing is the acknowledgement that the current gate is the one AD-12 forbids.

### MEDIUM

#### N-5 - Structural Seed drops `Server/Aggregates` and `Application/Tools`, but both exist on disk and the shipped guard test requires them; AD-19 says the tree reserves no tool folder
- **AD / section:** Structural Seed (L507-517 lists `Api/ Application/{Agents,AgentInteractions,Queries,Workflows,Activities} Composition/ Ports/ Projections/`); AD-19 ("the source tree reserves no folder for them"); memlog R-5 ("Server/Aggregates and Application/Tools are dropped ... StructuralSeedConformanceTests must stop requiring it").
- **Observed reality:** `src/Hexalith.Agents.Server/Aggregates/README.md` ("intentionally empty and unused") and `src/Hexalith.Agents.Server/Application/Tools/.gitkeep` are checked in; `test/Hexalith.Agents.Server.Tests/StructuralSeedConformanceTests.cs:40-49,112-123` asserts `Aggregates` and `Application/Tools` exist under Server. Deleting the folders to match the seed and AD-19 fails the suite. No story owns the test change (memlog names none; `epics.md` 0 hits for `StructuralSeedConformanceTests`). Everything else in the seed matches the tree: six `src/` and five `test/` projects in `Hexalith.Agents.slnx`, domain folders `Agent/ AgentInteraction/ ProviderCatalog/` (the other eight are Epic 5-8 work and are correctly unbuilt), Server `Api/ Composition/ Application/{Activities,AgentInteractions,Agents,Queries,Workflows}` (Workflows/Activities `.gitkeep` only, Story 6.1), UI `Components/ Composition/ Resources/ Services/`, `IntegrationTests` absent and annotated "created by Story 5.6".
- **Classification:** code-diverged-from-AD (AD-19) with the memlog recording intent but no owner; B-10 partial.
- **Disposition:** autofix. Name the owner (Story 5.6, which next touches the guard suite when it adds `IntegrationTests`) in the memlog line, and add `Server/Aggregates` and `Application/Tools` as "to be deleted by 5.6" so the seed is not contradicted by a green test.

#### N-6 - Story obligations the spine assigns to 5.5, 5.6, 5.7, 6.4, 6.7, 7.3 are not present in `epics.md`; `sprint-status.yaml` still tracks the superseded 18-story Epic 5
- **AD / section:** AD-15 ("owned by Stories 5.5 and 5.7"); AD-31 ("removed before Story 6.7 closes"); Stack ("until Story 5.6 aligns"); Seed ("created by Story 5.6"); memlog constraints L91 (5.5 migration), L97 (6.4/7.3 AttemptId), L115 (5.5 route prefix); External V1 Prerequisites consumer column.
- **Observed reality:** Consumer lists match `external-dependency-register.md` exactly (all eight records, verified row by row). But `epics.md`: Story 5.5 (`L1360-1408`) mentions no `TenantProviderEnablement`, catalog migration, `/api/v1` route, `Agents.PlatformOperator`, `AgentCallOperationStatus`/`AgentReadinessStatus` growth, or `IProjectionChangeDetailNotifier`; Story 5.6 (`L1409-1457`) mentions neither `Hexalith.Agents.IntegrationTests` nor the test-stack alignment; Story 5.7 (`L1458-1511`) mentions no enum growth; Stories 6.4/7.3 mention `AttemptId` but not `AttemptOrdinal`; Story 6.7 (`L1831-1889`) has zero mentions of `EXT-CONV-UI-1` (file-wide count 0) or the harness removal; `L151` still lists Aspire `13.4.6`/CommunityToolkit `13.4.1-beta.687` as the stack baseline; `L152` says all seven records are "currently `Uncommitted`" and omits `EXT-CONV-UI-1`; `L1273,1304` still bind Story 5.3 to `EXT-PROVIDER-1`. `sprint-status.yaml:87-104` still carries `5-2-enforce-complete-launch-readiness-before-callability` ... `5-18-prove-live-conformance-and-reassess-readiness` (B-12 unchanged; no provenance note was added under the spine table).
- **Classification:** stale claim (spine assigns work to stories whose authoritative text does not carry it) / authority drift.
- **Disposition:** discuss. Either run the epics update so each assignment lands in the named story, or add the one-line provenance note B-12 asked for and list the six assignments as "pending epics reconciliation" in the memlog. Until then a developer picking up 5.5 from `epics.md` will not see the migration, route, or policy work the spine expects.

#### N-7 - Errors convention names `AgentsProblem` with 422/409/202 mapping; the shipped contract is `AgentOperationError`/`AgentOperationErrorCode`, and the endpoints map no HTTP status
- **AD / section:** Consistency Conventions "Errors" row (L440).
- **Observed reality:** `src/Hexalith.Agents.Contracts/Operations/AgentOperationErrorCode.cs:13-40` (`NotAuthorized, ValidationFailed, NotFound, Conflict, Stale, Unavailable, Rejected, Blocked`) and `AgentOperationError.cs` are the shipped safe error shape, wrapped in `AgentOperationResult<T>` on every `IAgentsClient` member (`IProviderCatalogOperations.cs:16-51`). `AgentsOperationEndpoints.cs:38-165` returns the client result object directly from every lambda - no `Results.*`/`TypedResults.*`/`StatusCodes.*` call exists in the file (grep 0), so every response is 200 with the envelope. `AgentsProblem` appears nowhere in `src/`.
- **Classification:** unratified convention (shipped error shape unnamed) + code-diverged-from-AD (no status mapping), unrecorded.
- **Disposition:** autofix. Either rename the convention to the shipped `AgentOperationError` (with `AgentOperationErrorCode` as the safe `ReasonCode`) or record that `AgentsProblem` replaces it and which story (5.5) performs the HTTP status mapping.

#### N-8 - AD-17 `Freshness` is a discriminated value; the shipped `Freshness` is a three-value enum reused by both setup and catalog reads
- **AD / section:** AD-17 ("`Freshness` is one versioned discriminated value (`Basis` `ValidityWindow` or `RevisionLag`, `EvaluatedAt`, expected and observed revision, `ValidUntil`)"); Conventions "Freshness" row; memlog H-4 ("Ratifies the shipped ProviderCatalogViewFactory revision-lag basis").
- **Observed reality:** `src/Hexalith.Agents.Contracts/Agent/AgentSetupFreshness.cs:8-18` is `enum { Unknown, Current, Stale }`; `AgentSetupView.cs:27` and `ProviderCatalogInspectionResult.cs:27` both carry it, with `ProjectionVersion`/`ProjectedAt` as separate loose fields; `ProviderCatalogViewFactory.cs:76-88` computes it from an expected projection or capability version. No `Basis`, `EvaluatedAt`, or `ValidUntil` exists. The revision-lag *basis* is indeed what ships; the *shape* AD-17 mandates does not, and no memlog constraint records the gap or the owner.
- **Classification:** code-diverged-from-AD, unrecorded (memlog ratifies the basis, not the shape).
- **Disposition:** autofix. Add a memlog constraint: "shipped `AgentSetupFreshness` enum + loose `ProjectionVersion`/`ProjectedAt` are the RevisionLag predecessor of the AD-17 discriminated `Freshness`; Story 5.5 publishes the discriminated shape."

#### N-9 - AD-28 time authorities vs shipped expiry and UI clocks
- **AD / section:** AD-28 (`EvaluationInstant` "one injected `TimeProvider` supplied to the orchestrator and stamped ... as `EvaluatedAt`"; `DomainInstant` "`ExpiresAt` base ... is the EventStore event-metadata commit timestamp of the creating event, and a derived deadline is stored once in that event's payload"; "The UI never re-evaluates deadlines ... against a browser clock").
- **Observed reality:** The Server registers no `TimeProvider` (grep `TimeProvider` in `src/Hexalith.Agents.Server`: 0); `AgentInteractionProposalExpiryRequest.cs:44` takes `EvaluationTimestamp` from whoever builds the request; the aggregate compares supplied instants only (`AgentProposalExpiryPolicy.cs:12,47`) - that half conforms. `ExpiresAt` is sourced as an absolute ISO string from `IProposalExpiryPolicyReader` (`AgentInteractionProposalOrchestrator.cs:131-133`; `DeferredProposalExpiryPolicyReader` returns none), not derived from a commit timestamp. UI: `AgentsUiServiceCollectionExtensions.cs:30` registers `TimeProvider.System`, and `ProposalQueue.razor:147,207` computes age buckets and row filters from `TimeProvider.GetUtcNow()`; `ProposalDetail.razor:95` renders `ExpiresAt` raw. No `EvaluatedAt`, `TimerDrift`, or nearing-expiry computation exists anywhere.
- **Classification:** mostly expected-not-yet-built (Story 7.6 expiry, Story 6.1 workflow timers), but the shipped mechanisms (caller-supplied instant, policy-reader absolute `ExpiresAt`, UI wall-clock buckets) are different designs and are unrecorded.
- **Disposition:** autofix. Record in the memlog that the shipped expiry path is a Story 3.x predecessor replaced under AD-28 by 7.6, and that the UI age bucket is a display heuristic outside the deadline rule (or move it server-side with the `EvaluatedAt` result).

#### N-10 - AD-9 names `SecretReference` (ULID) and probe-derived `SecretConfigured`; the shipped catalog uses an administrator-asserted `ConfigurationReferenceId` and `ProviderConfigurationState`
- **AD / section:** AD-9; AD-10 ("`SecretReference` and configured state"; "pricing with currency and effective version").
- **Observed reality:** `ProviderCatalogAggregate.cs:56-57` validates `ConfigurationReferenceId` against `^[A-Za-z0-9._:-]+$` up to 128 chars (any administrator-supplied token, not a ULID); `ProviderConfigurationState { Unknown, NotConfigured, Configured }` is set by the create/update command, not by a "server-side resolvability probe (`Resolvable`, `Unresolvable`, `Denied`, `ObservedAt`)". `ProviderModelPricing.PricingVersion` is the shipped name for what AD-10 still calls "effective version" (B-5 residual). The non-disclosure property itself conforms (`ProviderSecretLeakTests`, `ContractsSecretNonDisclosureTests`).
- **Classification:** code-diverged-from-AD (naming and semantics), unrecorded; B-5 partial.
- **Disposition:** autofix. Either ratify `ConfigurationReferenceId`/`ProviderConfigurationState` as the wire names and say the probe is added by Story 6.4/`EXT-SECRETS-1`, or record the rename as a 5.5 obligation. Align "effective version" to `PricingVersion`.

#### N-11 - Audit-envelope `CorrelationId` rule vs the shipped identity factory
- **AD / section:** Conventions "Audit envelope" row ("`CorrelationId` (`AgentInteractionId` for every interaction-lifecycle command, else the ingress request id)").
- **Observed reality:** `src/Hexalith.Agents.Server/Ports/IAgentCommandIdentityFactory.cs:28-31` mints `NewMessageId()`/`NewCorrelationId()` with `Guid.NewGuid()`; `EventStoreAgentAdministrationOperations.cs:261` and `EventStoreProviderCatalogOperations.cs:190` use it whenever the client supplies no correlation id. No ingress request id (trace id) is consulted. Interaction-lifecycle orchestrators do not yet dispatch live (all bound to `DeferredAgentCommandDispatcher` outside the setup path), so the `AgentInteractionId` half is expected-not-yet-built. Also noted: the twelve interaction orchestrators correctly strip `actor:agentsAdmin` and repopulate nothing (`AgentInteractionRequestOrchestrator.cs:38-47`), so no activity borrows admin authority today; the `Workflow` principal is Story 6.1 work.
- **Classification:** code-diverged-from-convention (admin path), unrecorded; expected-not-yet-built (interaction path).
- **Disposition:** autofix. Record that the Story 5.2/5.3 admin path mints random correlation ids pending the ingress request-id binding (owner 5.5 or 5.6).

### LOW

#### N-12 - AD-4 wording still lists caller `PartyId` and source `ConversationId` inside the snapshot (B-14, unchanged)
- `AgentInteractionSnapshot.cs:38-47` has nine fields; caller and conversation live on the interaction. Disposition was "ignore" last time; still accurate that the text is loose. Classification: stale claim (minor).

#### N-13 - Stack table: every version and gitlink verified; annotations missing
- Verified correct: `global.json` `10.0.301`/`latestPatch`; siblings all `10.0.400`; `LangVersion 14`; `.slnx`; CPM importing `references/Hexalith.Builds/Props/Directory.Packages.props`; gitlinks EventStore `1b6f08d4`, Conversations `73bcee6f`, Parties `fa423985`, Tenants `54fc4040`, FrontComposer `053b2008` (`git submodule status`, clean); catalog Dapr/Workflow `1.18.5`, MediatR `14.2.0`, FluentValidation `12.1.1`, OpenTelemetry `1.18.0`, xunit.v3 `4.0.0`, NSubstitute `6.2.0`, Shouldly `4.3.0`, bunit `2.9.0`, Fluent UI `5.0.0-rc.5-26219.1`; root overrides xunit.v3 `3.2.2`, NSubstitute `5.3.0`, bunit `2.9.0`; `EXT-HOST-1` `a66cdf34`.
- Gaps: Dapr, MediatR, FluentValidation, OpenTelemetry rows have zero `PackageReference`s in the module (only FluentUI, FrontComposer, EventStore, Parties, bunit, TimeProvider.Testing and the test-stack packages are referenced) and are not annotated "catalog-pinned, unreferenced" as B-9 asked; the `Hexalith.Builds` gitlink (`a32cb422`, `v4.27.2-10`) that *is* the catalog source is unlisted; root overrides `Microsoft.NET.Test.Sdk 18.6.0`, `xunit.runner.visualstudio 3.1.5`, `coverlet.collector 10.0.1` are omitted from the accepted-deviation list. Classification: stale claim (minor). Disposition: autofix.

#### N-14 - Trusted-verdicts row names `*:validation`; the shipped keys are `provider:selectionValidation`, `approver:policyValidation`, `party:linkValidation`
- `AgentInteractionRequestOrchestrator.cs:43-47`, `AgentProviderSelectionOrchestrator.cs:41`, `AgentActivationProviderRevalidation.cs:36`. The pattern is `<subject>:<purpose>Validation`. Classification: unratified convention (naming). Disposition: autofix the row.

#### N-15 - AD-8 retires the `Caller` approver source; the shipped aggregate still accepts it as a valid configuration
- `ApproverPolicySourceKind.cs:24` keeps `Caller` (correct under FR-23 no-removal), but `AgentAggregate.cs:751-757` validates `Caller` as an accepted source with no rejection, and no `[Obsolete]`/deprecate-and-reject entry exists. No story owner is named in the spine. Classification: expected-not-yet-built (5.4 approver readiness) but unrecorded. Disposition: name the owner in the memlog.

#### N-16 - `ReadModelWritePolicy` / `IQueryCursorCodec` still unmentioned (B-15, unresolved)
- Projection handlers still write through `IReadModelStore` with their own checkpoint fold (`ProviderCatalogProjectionFold.cs`, `AgentSetupProjectionFold.cs`); `ReadModelWritePolicy` 0 hits in `src/`; spine 0 hits. Classification: unratified deviation from the platform state convention. Disposition: discuss with the EventStore owner, as before.

#### N-17 - Memlog supersession markers never added (B-18, unresolved)
- `grep -i supersed .memlog.md` returns 0; L44 ("module-local AppHost"), L46, L60 ("hybrid runtime ... MCP/A2A"), L61, L80 (AppHost gap) remain unmarked while contradicted by AD-16/AD-18/AD-19 and the 2026-09-09 entries. Classification: stale claim (memlog). Disposition: autofix.

#### N-18 - Predecessor readiness enum and route are unacknowledged
- `ProviderModelReadinessStatus { Enabled, Disabled, Degraded, Failed, NotConfigured }` (`Contracts/Operations/ProviderModelReadinessStatus.cs`) and `GET /status/providers/{providerId}/{modelId}/readiness` (`AgentsOperationEndpoints.cs:139`) are the Story 4.1 predecessor of AD-10's `ProviderReadinessResult` (`OperationalState`/`Callability`/`ReasonCode`). AD-15 assigns the successor to 5.5 but never says the predecessor exists and must stay additive under FR-23. Classification: expected-not-yet-built, unrecorded predecessor. Disposition: one memlog line.

---

## Verified conforming (no finding)

| Claim | Evidence |
|---|---|
| AD-16 no module-owned AppHost/Aspire/ServiceDefaults; guard tests enforce | `.slnx` six `src/` + five `test/` projects; `AppHostSecurityTopologyTests`, `ForbiddenHostingOwnershipTests`, `StructuralSeedConformanceTests.cs:83-97` |
| AD-3 aggregates, states, twin-policies only in `Hexalith.Agents` | `src/Hexalith.Agents/{Agent,AgentInteraction,ProviderCatalog}`; `Server/Aggregates` empty; `GovernanceConformanceTests` purity gate |
| AD-5 `ProposedAgentReplyState` = `Unknown` + ten states, terminal set exact | `ProposedAgentReplyState.cs:23-57` |
| AD-15 enum growth is 5.5/5.7 work | `AgentCallOperationStatus.cs` (9 values, none of the growth list), `AgentReadinessStatus.cs` (7 values) - expected-not-yet-built |
| AD-17 shipped projection ids `agent-setup`, `provider-catalog` | `AgentSetupReadModelAddresses.cs:15`, `ProviderCatalogReadModelAddresses.cs:15`; register `L203-205` renamed and adds `tenant-provider-enablement`, `workflow-execution-state` (`L220`) |
| Register amendments the memlog promised | `OperationGateMatrixVersion = 2` (`launch-readiness-register.md:110`), `ScopeKind`/`AuthorizedProducer` columns (`L83`), `ProposalEdit`/`TenantKillSwitch` v2 rows (`L127,133`), `ProviderReadinessResult` field set (`L142`) |
| AD-2 catalog tenant-scoped gap | recorded (memlog L91); `ProviderCatalogAggregate.cs:17` "tenant-scoped", `ProviderCatalogReadModelAddresses.cs:26` key uses tenant twice |
| AD-4 `ConfigurationVersion` not bumped by Activate/Disable | recorded (memlog A-19 / DW-4); `AgentAggregate.cs:214-305` |
| Route prefix `/api/agents/operations` | recorded (memlog L115); `AgentsOperationEndpoints.cs:22` |
| Test-stack overrides | recorded (memlog V-3..V-10); `Directory.Packages.props:16-25` |
| `IAgentCommandDispatcher` / `Deferred*` seam gated on `Agents:EventStore:BaseUrl` | `AgentSetupServiceCollectionExtensions.cs:48-66`; `DeferredAgentCommandDispatcher`, 17 `Deferred*` ports |
| AD-29 `MessageId`/posting key | `AgentResponsePostingIdentity.cs` identical tuple and tags |
| AD-7 `ParticipantType.AiAgent` / `ParticipantRole.Facilitator` exist in Conversations | `references/Hexalith.Conversations/.../ParticipantType.cs:32` (wire `AIAgent`), `ParticipantRole.cs:26`; `AddParticipantAsync` absent (EXT-CONV-AI-1 `Uncommitted`, expected) |
| AD-8 `ApproverPolicySourceKind.ConversationOwner` wire id | `ApproverPolicySourceKind.cs:33` |
| AD-31 harness exists pending 6.7 | `ConversationCall.razor:1` `@page "/agents/conversation-call"` |
| AD-18/AD-19 no Dapr/Agent Framework/tool SDK in contracts, domain, UI | `RuntimeOwnershipConformanceTests.cs:40-53`; `Directory.Packages.props` lists none |
| `system` reserved tenant exists in EventStore | `references/Hexalith.EventStore/.../ClaimsTenantValidator.cs:22`, `TopicNameValidator.cs:29` |
| External prerequisites consumers | identical to `external-dependency-register.md` for all eight records |
| Package layout / Trusted verdicts / Notifications / API versioning rows | present (B-13, B-16 resolved) |

---

## Disposition of 2026-09-08 findings (B-1..B-18)

| Id | 2026-09-08 finding | Status at 2026-09-09 | Evidence |
|---|---|---|---|
| B-1 | AppHost gap notes stale | **resolved** | AD-16 has no dated note; "guard tests enforce the boundary" |
| B-2 | "all seven `Uncommitted`" false | **resolved** | Prerequisites paragraph defers status to the register |
| B-3 | projection ids not authoritative | **resolved** | AD-17 names `agent-setup`/`provider-catalog`; register renamed |
| B-4 | 5.3 bound to `EXT-PROVIDER-1` | **resolved** | consumers `5.5, 6.4, 7.3`; AD-9 "catalog-truth publication is adapter-independent"; register narrowed (`epics.md` not, see N-6) |
| B-5 | AD-10 gap note partially stale | **partial** | note removed; "effective version" wording remains vs `PricingVersion` (N-10) |
| B-6 | `LaunchReadinessGate` unbuilt, Epic-4 shape unnoted | **unresolved / regressed** | note deliberately removed, memlog entry never added (N-2) |
| B-7 | SCP 08-04 AD-17 amendment unapplied | **resolved** | AD-17 live-seam matrix and bind-and-test sentence; seed marks IntegrationTests "created by Story 5.6" |
| B-8 | six vs thirteen families | **resolved** | AD-12 names the nine lock-bearing families and defers the vocabulary to the matrix version |
| B-9 | Stack drift | **resolved** | every row re-verified (N-13 annotations only) |
| B-10 | Seed vs tree | **partial** | seed redrawn; on-disk `Aggregates`/`Tools` and guard test still contradict it (N-5) |
| B-11 | `EXT-CONV-UI-1` missing | **resolved** | AD-31; register record; consumer 6.7 |
| B-12 | three story-numbering views | **unresolved** | `sprint-status.yaml:87-104` unchanged; no provenance note (N-6) |
| B-13 | unratified 5.2/5.3 conventions | **resolved** | Trusted verdicts row (naming nit N-14) |
| B-14 | AD-4 snapshot wording | **unchanged (ignored by disposition)** | N-12 |
| B-15 | `ReadModelWritePolicy` unused | **unresolved** | N-16 |
| B-16 | flat layout ratified only in memlog | **resolved** | Package layout row |
| B-17 | DW-2/DW-4 not cited | **partial** | DW-4 recorded (A-19); DW-2 absent (N-4) |
| B-18 | memlog supersession markers | **unresolved** | N-17 |

---

## Evidence tables

### A. Aggregate inventory (AD-2) vs `src/Hexalith.Agents`

| AD-2 aggregate | Exists | Location / note |
|---|---|---|
| `Agent` | yes | `Agent/AgentAggregate.cs:46` `[EventStoreDomain("agent")]`; also holds content safety (N-1), launch readiness and `EnableProductionLikeGeneration` (N-2) that AD-2 assigns elsewhere |
| `ProviderCatalog` | yes, tenant-scoped | `ProviderCatalog/ProviderCatalogAggregate.cs:30`; recorded gap |
| `TenantProviderEnablement` | no | Story 5.5 (per memlog assumption; not in `epics.md`, N-6) |
| `AgentInteraction` | yes | `AgentInteraction/AgentInteractionAggregate.cs:39`; snapshot single safety version (N-1) |
| `BudgetLedger` | no | Story 6.4 |
| `TenantGovernancePolicy` | no | Story 8.4 (`TenantBudgetPolicyAggregateTests` named there) |
| `ContentSafetyPolicy` | no | Story 8.4; predecessor lives on `Agent` (N-1) |
| `LaunchReadinessGate` | no | Story 5.5; predecessor lives on `Agent` (N-2) |
| `LegalHold` | no | Story 8.1 |
| `AuditExport` | no | Story 8.2 |
| `ProtectedDeletion` | no | Story 8.3 |

### B. Identity derivations (AD-29) - see N-3 table.

### C. Principals / extensions (AD-30)

| Spine | Code |
|---|---|
| `Administrator` reserved `actor:*` after Tenants-projection role check | `actor:agentsAdmin` (`AgentAggregate.cs:52`), `actor:agentsProviderAdmin` (`ProviderCatalogAggregate.cs:53`); populated from JWT `IsInRole("Agents.Administrator")` (N-4) |
| `Platform` (`system` tenant) | absent; `Agents.PlatformOperator` policy absent |
| `Workflow` | absent (Story 6.1); interaction orchestrators strip and repopulate nothing |
| `User` | not modelled as a principal type; commands carry caller ids as fields |
| policies `Agents.Administrator/Approver/AuditOperator/Operator` | `AgentsFrontComposerRegistration.cs:23-61` conform |

### D. Time (AD-28) - see N-9.

### E. Public enums

| Enum | Spine | Code | Status |
|---|---|---|---|
| `ProposedAgentReplyState` | AD-5 ten states + `Unknown` | identical | conforms |
| `AgentCallOperationStatus` | AD-15 growth by 5.5/5.7 | `Requested..Generated` (9) | expected-not-yet-built |
| `AgentReadinessStatus` | AD-15 growth by 5.5/5.7 | `Callable..Disabled` (7) | expected-not-yet-built |
| `ProviderModelReadinessStatus` | unnamed | predecessor of `ProviderReadinessResult` | N-18 |
| `AgentSetupFreshness` | AD-17 discriminated `Freshness` | flat enum | N-8 |
| `AgentOperationErrorCode` | `AgentsProblem` | eight safe codes | N-7 |

### F. Planning authorities

| Authority | Spine assumption | Current | Drift |
|---|---|---|---|
| `external-dependency-register.md` (2026-09-09) | consumers per table | identical; `EXT-HOST-1` `Committed`, seven `Uncommitted` | none |
| `launch-readiness-register.md` (2026-09-09) | renamed projection ids, matrix v2, ScopeKind, AuthorizedProducer, Freshness | all present | none |
| `epics.md` | story numbers 5.1-8.7 carry spine assignments | numbers and meanings match; assignments absent; stale stack/EXT lines | N-6 |
| `sprint-status.yaml` | - | superseded 18-story slugs | N-6 / B-12 |
| `deferred-work.md` | DW-4 recorded | DW-1, DW-2, DW-4, DW-5 open; DW-2 uncited | N-4 |
| memlog | six 2026-09-09 constraints | no entries for N-1, N-2, N-3 (three ids), N-4 authority, N-7, N-8, N-9, N-10, N-11 | this review |

---

## Disposition summary

| Id | Sev | Classification | Memlog records it | Disposition |
|---|---|---|---|---|
| N-1 | high | code-diverged-from-AD (AD-2/AD-4 safety policy on `Agent`) | no | discuss |
| N-2 | high | code-diverged-from-AD (AD-17 readiness on `Agent`); B-6 regressed | no | autofix (memlog constraint) |
| N-3 | high | stale claim (H-5) + code-diverged-from-AD (AD-29) | partly (AttemptId, regeneration only) | discuss |
| N-4 | high | code-diverged-from-AD (AD-30/AD-12 ingress + catalog authority) | partly (scope only) | discuss |
| N-5 | medium | code-diverged-from-AD (AD-19) / seed vs guard test | intent yes, owner no | autofix |
| N-6 | medium | stale claim / authority drift (epics, sprint-status) | no | discuss |
| N-7 | medium | unratified convention + code-diverged (errors, HTTP mapping) | no | autofix |
| N-8 | medium | code-diverged-from-AD (AD-17 Freshness shape) | basis only | autofix |
| N-9 | medium | expected-not-yet-built with unrecorded predecessor mechanisms (AD-28) | no | autofix |
| N-10 | medium | code-diverged-from-AD (AD-9 names/semantics); B-5 residual | no | autofix |
| N-11 | medium | code-diverged-from-convention (CorrelationId) | no | autofix |
| N-12 | low | stale claim (AD-4 wording); B-14 | - | ignore |
| N-13 | low | stale claim (Stack annotations) | - | autofix |
| N-14 | low | unratified convention (verdict key naming) | - | autofix |
| N-15 | low | expected-not-yet-built (Caller retirement), no owner | no | autofix |
| N-16 | low | unratified deviation (`ReadModelWritePolicy`); B-15 | no | discuss |
| N-17 | low | stale claim (memlog markers); B-18 | - | autofix |
| N-18 | low | expected-not-yet-built predecessor unacknowledged | no | autofix |
