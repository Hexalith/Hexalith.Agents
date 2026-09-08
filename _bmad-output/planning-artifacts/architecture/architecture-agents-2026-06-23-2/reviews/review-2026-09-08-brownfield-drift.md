---
title: Brownfield Ratification / Drift Review - Architecture Spine
lens: brownfield-ratification-drift
target: ../ARCHITECTURE-SPINE.md
target_updated: 2026-08-02
reviewed: 2026-09-08
reviewer: independent architecture reviewer (validation gate)
repository_head: d7e9cda
---

# Brownfield Drift Review - ARCHITECTURE-SPINE.md (2026-09-08)

**Gate verdict: FAIL-TO-RATIFY (spine is decision-sound but no longer ratifies repository reality; 2 high stale-gap/authority findings, 1 high AD-17 projection-id divergence, 1 high governance contradiction on Story 5.3; all fixable without changing any AD decision).**

Counts: critical 0 / high 4 / medium 8 / low 6.

The 26 architecture decisions themselves remain valid and nothing in the code contradicts a decision's *intent*. What has drifted is the spine's picture of the repository: its three dated gap annotations, its Stack table, its Structural Seed, its External-Prerequisites paragraph, and its AD-17 projection inventory no longer match what is checked in at `d7e9cda`. Story 5.1 (commit `516e7b9`) removed the module-owned hosting projects the spine still calls a gap; Story 5.2/5.3 landed two live projections under names the spine does not authorise; `EXT-HOST-1` moved to `Committed` on 2026-08-09; and Story 5.3 executed against a dependency the spine says blocks it.

---

## Findings

### HIGH

#### B-1 - AD-16 / Structural Seed / Capability Map: AppHost gap annotations are stale
- **Severity:** high
- **AD / section:** AD-16 gap note (spine L189); Structural Seed gap note (L464); Capability map row "Deployment/dev topology" (L625); memlog constraint (`.memlog.md` L80)
- **Spine claim:** "the current solution still contains `Hexalith.Agents.AppHost`, `Hexalith.Agents.Aspire`, and `Hexalith.Agents.ServiceDefaults`. Their presence is non-conformant ... Story 5.1 owns removal."
- **Observed reality:** Commit `516e7b9` (Story 5.1) deleted `src/Hexalith.Agents.AppHost/{Program.cs,*.csproj}`, `src/Hexalith.Agents.Aspire/`, `src/Hexalith.Agents.ServiceDefaults/` and their `.slnx` entries. `Hexalith.Agents.slnx` now lists six `src/` projects and five `test/` projects, none host-owned. Guard tests now enforce the boundary: `test/Hexalith.Agents.Server.Tests/AppHostSecurityTopologyTests.cs:13-45` (no AppHost Program; Server is the only `Microsoft.NET.Sdk.Web` project), `ForbiddenHostingOwnershipTests.cs:19-103` (rejects `.AppHost/.Aspire/.ServiceDefaults` suffixes, `Aspire.AppHost.Sdk`, `IsAspireSharedProject`, `Aspire.Hosting`). `src/Hexalith.Agents.Server/Program.cs:29-31,216` is the two-line DomainService host. `sprint-status.yaml:87` marks 5.1 `done`.
- **Classification:** stale-gap-note
- **Disposition:** autofix. Replace the AD-16 note with: *"Conformance (2026-09-08): Story 5.1 (`516e7b9`) removed the module-owned AppHost/Aspire/ServiceDefaults projects; `AppHostSecurityTopologyTests` and `ForbiddenHostingOwnershipTests` guard the boundary. `EXT-HOST-1` is `Committed` (Hexalith.Platform `a66cdf34`, target 2026-09-30); Story 5.6 still owns consumption and Level-4 proof."* Delete the Seed gap paragraph (L464) and reword the capability-map row to "platform-hosted composition pending Story 5.6 / EXT-HOST-1 `Available`".

#### B-2 - External V1 Prerequisites: "All seven records are currently `Uncommitted`" is false
- **Severity:** high
- **AD / section:** External V1 Prerequisites (L629); AD-16 (`EXT-HOST-1`)
- **Spine claim:** all seven EXT records `Uncommitted`; consumers blocked from `ready-for-dev`.
- **Observed reality:** `external-dependency-register.md` (`updated: 2026-08-09`, commit `90761e3`) records `EXT-HOST-1` as `AcceptedStatus = Committed` with `Repository = Hexalith.Platform`, `TargetVersionOrCommit = a66cdf346e...`, `TargetIntegrationDate = 2026-09-30`, and an executable command (`./eng/verify-agents-host.sh`) (register L63-75, L146). The other six remain `Uncommitted` (L88, L102, L116, L130, L144).
- **Classification:** authority-changed
- **Disposition:** autofix. Rewrite L629: *"Six of seven records are `Uncommitted`; `EXT-HOST-1` is `Committed` (development/contract work only, not execution). Consumers of `Uncommitted` records remain blocked from `ready-for-dev`; `RQ-1` requires `Available`."* Add a Status column to the table.

#### B-3 - AD-17 projection inventory: two live projections use ids outside the authoritative list
- **Severity:** high
- **AD / section:** AD-17 (L195 "Authoritative projection IDs are ..."); Consistency Conventions "Projections" row (L401); AD-22 (purge keyed to named IDs)
- **Spine claim:** the 17 listed ids are authoritative; "readiness, purge, and evidence criteria name IDs rather than saying all affected projections"; the two setup/catalog projections are named `agent-setup-readiness` and `provider-capability-pricing`.
- **Observed reality:** The two projections that actually exist declare `ProjectionName = "agent-setup"` (`src/Hexalith.Agents.Server/Projections/AgentSetupReadModelAddresses.cs:43`, Domain `"agent"`) and `ProjectionName = "provider-catalog"` (`ProviderCatalogReadModelAddresses.cs:15`, Domain `"provider-catalog"`), registered as `ProjectionType` at `AgentSetupProjectionHandler.cs:46` and `ProviderCatalogProjectionHandler.cs:40`. None of the 17 spine ids appears as a projection id anywhere in `src/` (grep: `agent-setup-readiness` 0, `provider-capability-pricing` 0, `launch-readiness` only in prose/UI labels, the remaining 14 zero or CSS/test-id hits only). `epics.md:145` repeats the spine list verbatim, so the story authority also does not sanction `agent-setup`/`provider-catalog`.
- **Classification:** code-diverged-from-AD
- **Disposition:** discuss. Two acceptable resolutions: (a) rename the constants to the AD-17 ids (the slot-key format `{domain}:{tenant}:{projection}:{slot}:{id}` makes this a one-line rename per address class plus the platform slot registration), or (b) ratify `agent-setup` and `provider-catalog` as the *setup-detail* projections and keep `agent-setup-readiness`/`provider-capability-pricing` for the Story 5.5 readiness-bearing views. Either way AD-17 must say which ids are live today, because AD-22 deletion completeness is defined over the named list.

#### B-4 - Story 5.3 executed while the spine binds it to an `Uncommitted` dependency
- **Severity:** high
- **AD / section:** External V1 Prerequisites table row `EXT-PROVIDER-1` (L635: consumers "5.3, 5.5, 6.4, 7.3"); AD-9 (L145)
- **Spine claim:** `EXT-PROVIDER-1` consumers remain blocked from `ready-for-dev` while the record is `Uncommitted`. `epics.md:1273` ("EXT-PROVIDER-1 must be at least Committed before ready-for-dev") and the register's entry gate (L14-16) say the same.
- **Observed reality:** `EXT-PROVIDER-1` is still `Uncommitted` with `TBD` repository/target/command (register L77-89). Story 5.3 is nevertheless `status: done` (spec-5-3 frontmatter, commit `a191d24` 2026-09-08). The spec deliberately scoped around the adapter ("EXT-PROVIDER-1 stays Uncommitted: this story publishes catalog truth only and must not invent or invoke the generation adapter" - spec Design Notes) and the code honours that: no `IAgentGenerationProvider` implementation, no SDK package (`Directory.Packages.props` lists none; `RuntimeOwnershipConformanceTests.cs:43` forbids `Dapr`/`Microsoft.Agents.AI`), `DeferredAgentGenerationProvider` still bound (`Program.cs:124`).
- **Classification:** authority-changed (the spine's consumer binding is broader than what 5.3 actually consumes; execution and spine disagree)
- **Disposition:** discuss. Recommended: narrow the `EXT-PROVIDER-1` consumer list to the stories that *execute* the adapter (5.5 readiness contract, 6.4, 7.3) and record in AD-9 that catalog-truth publication (5.3) is adapter-independent. Alternatively record 5.3 as an accepted out-of-gate exception in the register. Leaving the table as-is makes the spine assert a gate that the team has already decided not to honour.

### MEDIUM

#### B-5 - AD-10 gap note is partially stale after Story 5.3
- **Severity:** medium
- **AD / section:** AD-10 gap note (L152); AD-9 pricing wording (L145)
- **Spine claim:** context/generation/regeneration lack high-water/effective-version; catalog rules as listed.
- **Observed reality:** Catalog side now conforms: `CapabilityVersion` 1 on create (`ProviderCatalogAggregate.cs:132`), +1 on metadata/pricing update (L195), regression and reuse rejected (`TryGetCapabilityVersionRejection` L385-420, `ProviderModelCapabilityVersionRegressedRejection`), enable/disable emit lifecycle events without touching the version (L219-292; `ProviderCatalogVersionRegressionTests.cs:78`); administrator-supplied pricing with ISO-4217 currency, input/output unit prices and monotonic `PricingVersion` (`ProviderModelPricing.cs:14-18`); selectability now requires enabled+configured+text-gen+valid limits+valid pricing+version>=1 (`ProviderCatalogInspection.cs:85-95`); verdicts add `Unpriced`/`Regressed` (`ProviderSelectionVerdict.cs:59-71`); only `ConfigurationReferenceId` + `ConfigurationState` cross the boundary (`ProviderSecretLeakTests.cs`). Runtime side is unchanged and the note remains true there: `EffectiveProviderCapabilityVersion` 0 hits, `HighWater` 0 hits, `ProviderReadinessResult`/`ProviderReadinessReasonCode`/`OperationalState`/`Callability` 0 hits in `src/` (the only `Callability` hit is a UI label at `AgentsOverview.razor:69`). `AgentInteractionContextOrchestrator.cs` still reads current limits and carries the snapshot version as provenance.
- **Classification:** stale-gap-note (partial); expected-not-yet-built for the readiness contract (Story 5.5, `epics.md:1360-1408`) and runtime high-water (Stories 6.2/6.4/7.3, `epics.md:1592-1612,1687,2016-2039`)
- **Disposition:** autofix. Re-date the note: *"(2026-09-08) Catalog invariants (monotonic `CapabilityVersion`, non-bump on enable/disable, regression rejection, versioned pricing, eligibility floor) shipped in Story 5.3. `ProviderReadinessResult` and reason codes are Story 5.5. Durable high-water / `EffectiveProviderCapabilityVersion` / pre-invocation revalidation remain absent until Stories 6.2, 6.4, 7.3."* Also align AD-9/AD-10 wording "pricing ... with an effective version" to the shipped name `PricingVersion`.

#### B-6 - AD-17 `LaunchReadinessGate` is not built; a different, Epic-4 shape exists with no gap note
- **Severity:** medium
- **AD / section:** AD-17 (L195); Capability map "Launch readiness and release qualification" (L621); class diagram `LaunchReadinessRecord`/`ReadinessObservation`/`OperationGateMatrix` (L512-536)
- **Spine claim:** the EventStore `LaunchReadinessGate` aggregate is the only writer of `(GateId, TenantScope, EnvironmentProfile)` records; `OperationGateMatrix` is consumed by API/BFF/UI/workflow.
- **Observed reality:** No aggregate, command, event or type named `LaunchReadinessGate`, `LaunchReadinessRecord`, `ReadinessObservation`, `OperationGateMatrix`, `OperationFamily` exists (grep 0). No `LR-*` GateId string exists in `src/` (grep 0). What exists is the historical Story 4.4 shape: `RecordAgentLaunchReadiness` handled on the **Agent** aggregate (`src/Hexalith.Agents/Agent/AgentAggregate.cs:587-636`) storing an `AgentLaunchReadiness` value (metrics, latency targets, cost posture) on `AgentState` with `LaunchReadinessVersion`, plus `AgentLaunchReadinessPolicy`/`AgentLaunchReadinessInspection` and a UI gateway (`ILaunchReadinessGateway`). `epics.md:1377,1404` assigns the AD-17 aggregate to Story 5.5 (`LaunchReadinessGateAggregateTests`, `OperationGateMatrixParityTests`).
- **Classification:** code-diverged-from-AD (historical Epic 4 implementation) + expected-not-yet-built (Story 5.5)
- **Disposition:** autofix. Add an AD-17 note: *"Implementation gap (2026-09-08): the checked-in launch-readiness record is an Agent-aggregate value (`RecordAgentLaunchReadiness`, Story 4.4) with no GateId inventory or matrix. It is a historical predecessor, not the `LaunchReadinessGate` aggregate; Story 5.5 replaces it and Story 5.7 consumes it for activation."*

#### B-7 - Approved SCP 2026-08-04 (live-integration tier) amends AD-17 but the spine was not updated; the atomicity rule is already being bypassed
- **Severity:** medium
- **AD / section:** AD-17 (L195); Structural Seed `test/Hexalith.Agents.IntegrationTests/` (L461)
- **Spine claim:** AD-17 text as of 2026-08-02; seed lists an IntegrationTests project.
- **Observed reality:** `sprint-change-proposal-2026-08-04-live-integration-tier.md` (status `approved`, `proposed_artifact_changes` includes `ARCHITECTURE-SPINE.md`) drafts an AD-17 append: "The story that changes a seam from deferred to live must atomically add and execute the corresponding `Hexalith.Agents.IntegrationTests` coverage and assert persisted state-store/read-model end state" (SCP §4.7, L211-221). The spine contains none of that text. Meanwhile Stories 5.2 and 5.3 bound the first live projection/query/dispatch seams (`AgentSetupServiceCollectionExtensions.cs:46-66`, gated on `Agents:EventStore:BaseUrl`) and no `test/Hexalith.Agents.IntegrationTests/` project exists; the tests named `*EventStoreIntegrationTests` live in `Hexalith.Agents.Server.Tests` and run over `FakeReadModelStore` (`ProviderCatalogEventStoreIntegrationTests.cs:29`).
- **Classification:** authority-changed (approved amendment unapplied) + unratified deviation from the amendment's trigger
- **Disposition:** discuss. Either apply the SCP text to AD-17 and record that 5.2/5.3 predate the project (owed by 5.6 per SCP L235), or explicitly retire the IntegrationTests project in favour of the `*EventStoreIntegrationTests` fake-store suites. Do not leave the seed naming a project that the approved plan says will not exist until 5.6 while the amendment's rule is silently unmet.

#### B-8 - AD-12 names six operation families; the register's `OperationGateMatrix` v1 has thirteen
- **Severity:** medium
- **AD / section:** AD-12 (L164 "Normative families are ..."); AD-17 (L195 "The versioned `OperationGateMatrix` in the register is the single mapping"); AD-25 (L243)
- **Spine claim:** the normative families are `ProposalResolution`, `PolicyPublication`, `TenantBudgetUpdate`, `LegalHold`, `ExportRequest`, `DeletionRequest`.
- **Observed reality:** `launch-readiness-register.md:87-101` (`OperationGateMatrixVersion = 1`) lists `ProviderCatalogMutation`, `AgentSetupMutation`, `AgentActivation`, `AgentCallAcceptance`, `ProviderInvocation`, `ConversationPosting`, `ReadinessInspection` in addition to the six. AD-17 says a missing family blocks, yet the spine never names the seven additional families and never states whether AD-12's advisory session lock applies to them (e.g. is a `ProviderCatalogMutation` a high-risk one-pending-per-resource command?). No `OperationFamily` type exists in code (grep 0), so the question is open. GateId inventory itself is consistent: the register uses exactly the 18 `LR-*` ids in AD-17 (frequency table in Evidence §E).
- **Classification:** authority-changed
- **Disposition:** autofix (clarify). Append to AD-12: *"These six are the high-risk families subject to the session lock; the full readiness family inventory (including `ProviderCatalogMutation`, `AgentSetupMutation`, `AgentActivation`, `AgentCallAcceptance`, `ProviderInvocation`, `ConversationPosting`, `ReadinessInspection`) is owned by the register's `OperationGateMatrix`."*

#### B-9 - Stack table drift
- **Severity:** medium
- **AD / section:** Stack (L405-431)
- **Spine claim / Observed reality:**
  | Row | Spine | Repository |
  |---|---|---|
  | Hexalith.EventStore | `30810727` | submodule at `f54d6048` (`git submodule status`) |
  | Hexalith.Conversations | `331ec28e` | `73bcee6f` |
  | Hexalith.Parties | `3295560a` | `fa423985` |
  | Hexalith.Tenants | `085e5021` | `54fc4040` |
  | Hexalith.FrontComposer | `62841406` | `d42e8312` |
  | Aspire Hosting / AppHost SDK | `13.4.6` "from ... local AppHost SDK declarations" | no AppHost SDK declaration exists; Builds catalog `Aspire.Hosting` = `13.5.3` (`references/Hexalith.Builds/Props/Directory.Packages.props:113`); zero module references (`ForbiddenHostingOwnershipTests` forbids them) |
  | CommunityToolkit Aspire Hosting Dapr | `13.4.1-beta.687` | catalog `13.5.0-preview.1.260825-0345` (L136); unreferenced |
  | OpenTelemetry | `1.17.0` | catalog `1.18.0` (L266); unreferenced by module code |
  | MediatR / FluentValidation | `14.2.0` / `12.1.1` | catalog values unchanged but **zero** references in `src/`/`test/` |
  | Dapr / Dapr Workflow | `1.18.5` | catalog unchanged; Server declares no Dapr package (`Program.cs:33-36`, transitive via DomainService) |
  | Fluent UI Blazor | `5.0.0-rc.4-26180.1` | `Directory.Packages.props:14` = `5.0.0-rc.5-26219.1` |
  | Provider / Agent Framework SDK | `Unselected` | still unselected (correct) |
  Root `global.json` (`10.0.301`, `latestPatch`), `net10.0`, C# `14`, `.slnx`, CPM: all still correct.
- **Classification:** stale-gap-note (Stack rows referring to removed AppHost declarations) / authority-changed (catalog versions)
- **Disposition:** autofix. Refresh commits and versions; drop the "local AppHost SDK declarations" source; mark MediatR/FluentValidation/OpenTelemetry/Aspire rows as "catalog-pinned, not referenced by the module" or remove them.

#### B-10 - Structural Seed no longer matches the tree
- **Severity:** medium
- **AD / section:** Structural Seed (L435-462); class placement implied by AD-1/AD-3
- **Spine claim:** `Hexalith.Agents.Server/{Aggregates, Application/{Agents,Workflows,Activities}, Ports, Projections}`; tests include `Hexalith.Agents.IntegrationTests/`.
- **Observed reality:** (1) `test/Hexalith.Agents.Tests/` (44 files, domain aggregate tests, created 2026-06-26 `84f6156`, in `.slnx`) is absent from the seed; `Hexalith.Agents.IntegrationTests/` does not exist (see B-7). (2) `Server/Aggregates/` holds only a README stating it is an "unused placeholder"; the three aggregates live in `src/Hexalith.Agents/{Agent,AgentInteraction,ProviderCatalog}/` (`Aggregates/README.md`; `AgentAggregate.cs:47`, `ProviderCatalogAggregate.cs:30`, `AgentInteractionAggregate.cs:40`). (3) Server actually contains `Api/`, `Composition/`, `Application/{Activities,AgentInteractions,Agents,Queries,Tools,Workflows}` - `Api`, `Composition`, `AgentInteractions`, `Queries`, `Tools` are unlisted; `Workflows`, `Activities`, `Tools` are `.gitkeep`-only. (4) `StructuralSeedConformanceTests.cs:39-48` *requires* `Application/Tools`, a folder the spine seed does not name and whose purpose AD-19 defers out of V1. (5) memlog L35 ratifies the flat sibling-module shape (`Contracts/Client/Server/UI/Testing`) but describes `Hexalith.Agents` as "host composition", whereas it is the domain-aggregate library and `Server` is the host.
- **Classification:** stale-gap-note / unratified-new-convention (`Application/Tools`, `Api/`, `Composition/`)
- **Disposition:** autofix. Redraw the seed: move `Aggregates/` under `Hexalith.Agents/` as `{Agent,AgentInteraction,ProviderCatalog}/`; list Server `Api/ Composition/ Application/{Agents,AgentInteractions,Queries,Workflows,Activities}/ Ports/ Projections/`; add `test/Hexalith.Agents.Tests/`; either drop `Application/Tools` from the conformance test or annotate it "reserved, AD-19 out of V1"; keep `IntegrationTests` only if B-7 resolves in its favour.

#### B-11 - SCP 2026-08-03 obligations on the spine (EXT-CONV-UI-1, AD-6/AD-8/AD-15 references) were never carried out
- **Severity:** medium
- **AD / section:** AD-6, AD-8, AD-15; External V1 Prerequisites
- **Spine claim:** seven EXT records; Call-hexa action is "Conversation-owned" (L612) with no registration contract.
- **Observed reality:** `sprint-change-proposal-2026-08-03.md` (approved) lists `ARCHITECTURE-SPINE.md` under `amends_if_approved` and instructs the Solution Architect to "update AD-6/AD-8/AD-10/AD-15/AD-16 references, define `EXT-CONV-UI-1`" (L560). `EXT-CONV-UI-1` appears only in the three SCPs; it is in neither the register, `epics.md`, nor the spine (grep). The spine's `updated: 2026-08-02` predates the SCP.
- **Classification:** authority-changed (approved but unapplied)
- **Disposition:** discuss. Either add `EXT-CONV-UI-1` (Conversation-owned action-contribution seam for **Call hexa**) to the register and the spine table with consumer Story 6.7, or formally withdraw that SCP directive.

#### B-12 - Three story-numbering views coexist; the spine's table matches only one
- **Severity:** medium
- **AD / section:** External V1 Prerequisites consumer column (L633-639); AD-16 note ("Story 5.1 ... Story 5.6")
- **Spine claim:** consumers 5.1, 5.3, 5.5, 5.6, 6.1-6.6, 7.3, 7.4, 8.2-8.7.
- **Observed reality:** `epics.md` (27 active stories, Epics 5-8) still carries those numbers with the meanings the spine assumes (5.3 Govern Provider Models; 5.6 Compose in platform host; 6.4 cost reservations; 6.6 join/post; 7.3 regenerate; 7.4 approve/post; 8.2 export; 8.3 delete; 8.5 metrics; 8.6 UI; 8.7 launch evidence - `epics.md:1164-2543`). But `sprint-status.yaml:87-104` tracks a superseded 18-story Epic 5 (`5-2-enforce-complete-launch-readiness-before-callability`, `5-3-bind-eventstore-operations-and-setup-read-models`, `5-6-reconcile-provider-capability-runtime-contracts` ...) whose slugs do not match the specs they point at (spec-5-2 title is "Configure hexa Through Live EventStore Operations"), and the approved SCPs of 08-03/08-04 direct a 44-story Epics 5-10 re-materialisation that was never applied (`implementation-readiness-report-2026-08-04.md:9` still `NOT READY`). Spec 5.3 explicitly declares `epics.md` numbering authoritative.
- **Classification:** authority-changed (pending) - the spine is currently consistent with the declared authority
- **Disposition:** defer, but add a one-line provenance note under the table: *"Story numbers follow `epics.md` (27-story Epics 5-8); `sprint-status.yaml` slugs are archived numbering."* Re-check on any 44-story materialisation.

### LOW

#### B-13 - Unratified conventions introduced by Stories 5.2/5.3
- **Severity:** low
- **AD / section:** Consistency Conventions (L378-403); IMPLEMENTATION-CONVENTIONS.md
- **Observed reality:** (a) Server-populated trusted command-envelope extensions `actor:agentsProviderAdmin` / `actor:agentAdmin` etc., stripped from client input and repopulated before dispatch (`ProviderCatalogAggregate.cs:24-53`, `ProviderCatalogAdministrationOrchestrator.cs:17-24`, `AgentAggregate.cs`, 40 `TrustedExtension` hits) - patterned on Tenants' `actor:globalAdmin`; (b) `EventStoreAgentCommandDispatcher` as the single live dispatch seam, swapped for `DeferredAgentCommandDispatcher` only when `Agents:EventStore:BaseUrl` is configured (`AgentSetupServiceCollectionExtensions.cs:46-66`, `Program.cs:202-212`); (c) `AgentCommandAcceptance` / `ProviderCatalogCommandAcceptance` "Submitted" accepted-identity responses (`EventStoreProviderCatalogOperations.cs:180`); (d) UI post-write polling submitted -> authoritative-pending -> projection-confirmed (`ProviderCatalog.razor:473`, `spec-setup-projection-polling.md`, DW-3 done). The truth-flow itself is UX-ratified (`EXPERIENCE.md:119-159`) and the lock is AD-12; (a)-(c) are named nowhere in the spine or IMPLEMENTATION-CONVENTIONS.md (grep `actor:`/`trusted`/`Dispatcher` 0 hits in the conventions file).
- **Classification:** unratified-new-convention
- **Disposition:** autofix. Add a "Trusted verdicts" row: *"Authorization and dependency verdicts reach aggregates only as server-populated reserved `actor:*`/`*:validation` envelope extensions; orchestrators strip client-supplied reserved keys and repopulate them. Live EventStore dispatch is one `IAgentCommandDispatcher` seam, fail-closed `Deferred*` when the EventStore base URL is absent; accepted writes return `Submitted` identities, never callability."*

#### B-14 - AD-4 gap note is accurate (verified) - keep, re-date optional
- **Severity:** low
- **Observed reality:** `AgentInteractionSnapshot(ConfigurationVersion, InstructionsVersion, ResponseMode, ApproverPolicyVersion, ProviderId, ModelId, ProviderCapabilityVersion, ContentSafetyPolicyVersion, ContextPolicyReference)` with `DefaultContextPolicyReference = "full-conversation-v1"` (`src/Hexalith.Agents.Contracts/AgentInteraction/AgentInteractionSnapshot.cs:38-54`). Caller `PartyId` and source `ConversationId` live on the interaction, not the snapshot record (spine wording lists them in the snapshot).
- **Classification:** verified-current (minor wording)
- **Disposition:** ignore, or tighten the sentence to "snapshots ... and records caller `PartyId` and source `ConversationId` on the interaction".

#### B-15 - Platform SDK seams: `ReadModelWritePolicy` and `IQueryCursorCodec` unused
- **Severity:** low
- **AD / section:** Design Paradigm; hexalith-state-instructions.md ("Persisted read models - use `IReadModelStore` + `ReadModelWritePolicy`"; "Pagination cursors - use `IQueryCursorCodec`")
- **Observed reality:** `IDomainQueryHandler` (10 hits), `IDomainProjectionHandler` (2), `IReadModelStore` (25) are used; `ReadModelWritePolicy` 0, `IQueryCursorCodec`/`QueryCursorScope` 0 in `src/`. Both exist in the pinned EventStore (`references/Hexalith.EventStore/src/Hexalith.EventStore.Client/Projections/ReadModelWritePolicy.cs`, `Queries/IQueryCursorCodec.cs`). Projection handlers write through `IReadModelStore` directly with their own checkpoint fold (`ProviderCatalogProjectionFold.cs:90`). No paginated query exists yet, so the cursor seam is not yet applicable.
- **Classification:** unratified deviation from platform convention (write policy); expected-not-yet-built (cursor)
- **Disposition:** discuss (with the EventStore owner). If the fold-with-checkpoint pattern is the intended replacement for `ReadModelWritePolicy`, say so in the spine; otherwise Story 5.5/5.6 should adopt the policy type.

#### B-16 - Flat module layout vs platform vertical-slice layout is ratified only in the memlog
- **Severity:** low
- **AD / section:** Structural Seed; hexalith-llm-instructions.md "Domain-Driven Design Architecture" (per-layer packages under `src/libraries/{Domain,Application,Infrastructure,Presentation}`)
- **Observed reality:** memlog L35 records the decision "Project topology follows sibling Hexalith module shape (Contracts/Client/Server/.../UI/Testing)" and L36 the inward dependency rule; the spine's seed embodies it but never states that it deliberately departs from the platform's canonical per-layer layout, nor why (parity with Tenants/Parties/Conversations siblings). Package references confirm the module obeys the *substance* of the platform rule (only `Hexalith.EventStore.DomainService` from the Server, `EventStore.Client` from the domain, `EventStore.Contracts` from Contracts - csproj L11-21 each).
- **Classification:** ratified-in-memlog, unstated-in-spine
- **Disposition:** autofix. One sentence under the seed: *"Layout intentionally mirrors the sibling modules' `Contracts/Client/Server/UI/Testing` shape rather than the per-layer library tree in `hexalith-llm-instructions.md`; the dependency-direction rule of that document is preserved."*

#### B-17 - Deferred-work ledger: no deliberate AD deviations recorded; two entries touch AD semantics
- **Severity:** low
- **Observed reality:** `deferred-work.md` has DW-1..DW-5. None records a chosen deviation from an AD. DW-2 (open) notes setup queries still authorise via the deferred `ITenantAccessReader` while HTTP paths use `Agents.Administrator` role checks - an AD-12 "JWT-only authorization" hazard owned by Story 5.4. DW-4 (open) notes lifecycle writes do not bump `ConfigurationVersion`, which bears on AD-4's "Agent configuration version" as a snapshot key.
- **Classification:** expected-not-yet-built
- **Disposition:** defer; cite DW-2 in an AD-12 note if the spine adopts gap notes for Epic 5.

#### B-18 - memlog retains superseded decisions without supersession markers
- **Severity:** low
- **Observed reality:** memlog L44 ("Operational topology is module-local: Hexalith.Agents owns its own AppHost"), L46, L60 ("hybrid runtime ... Microsoft Agent Framework ... MCP/A2A"), L61 are contradicted by AD-16/AD-18/AD-19 as corrected 2026-08-01; L80 (AppHost gap constraint) is now stale per B-1. The memlog's last entry is 2026-08-02.
- **Classification:** stale-gap-note (memlog)
- **Disposition:** autofix - append 2026-09-08 entries recording B-1, B-2, B-3 outcomes and marking L44/L46/L60/L61/L80 superseded.

---

## Evidence Tables

### A. Structural Seed vs repository (`Hexalith.Agents.slnx`, `src/`, `test/`)

| Seed entry | Exists | Notes |
|---|---|---|
| `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `NuGet.config`, `Hexalith.Agents.slnx` | yes | `.slnx` Solution Items folder lists all |
| `src/Hexalith.Agents.Contracts/` | yes | refs `Hexalith.EventStore.Contracts` only |
| `src/Hexalith.Agents.Client/` | yes | refs Contracts only |
| `src/Hexalith.Agents.Server/Aggregates/` | placeholder | README: "empty and unused"; aggregates in `src/Hexalith.Agents/` |
| `src/Hexalith.Agents.Server/Application/Agents/` | yes | orchestrators, verdicts |
| `src/Hexalith.Agents.Server/Application/Workflows/` | `.gitkeep` only | Dapr Workflow not started (Story 6.1) |
| `src/Hexalith.Agents.Server/Application/Activities/` | `.gitkeep` only | same |
| `src/Hexalith.Agents.Server/Ports/` | yes | 70 files, `Deferred*` fail-closed adapters + live `EventStoreAgentCommandDispatcher`, `ProjectedProviderCatalogReader`, Conversations/Parties adapters |
| `src/Hexalith.Agents.Server/Projections/` | yes | `AgentSetup*`, `ProviderCatalog*` (fold, handler, view factory, addresses) |
| unlisted: `Server/Api/`, `Server/Composition/`, `Server/Application/{AgentInteractions,Queries,Tools}` | present | `Tools` is `.gitkeep`, required by `StructuralSeedConformanceTests.cs:46` |
| `src/Hexalith.Agents/` | yes | domain library: `Agent/`, `AgentInteraction/`, `ProviderCatalog/` |
| `src/Hexalith.Agents.UI/`, `src/Hexalith.Agents.Testing/` | yes | |
| `test/Hexalith.Agents.Contracts.Tests/`, `Server.Tests/`, `Client.Tests/`, `UI.Tests/` | yes | |
| `test/Hexalith.Agents.IntegrationTests/` | **no** | planned Story 5.6 (SCP 08-04) |
| unlisted: `test/Hexalith.Agents.Tests/` | present | 44 files, domain aggregate tests, since `84f6156` 2026-06-26 |
| `Hexalith.Agents.AppHost/.Aspire/.ServiceDefaults` | **removed** | `516e7b9` (Story 5.1); only `bin/obj` artefacts remain |

### B. Spine-named types / fields in `src/` (+ `test/`)

| Symbol | Hits | Where | Status |
|---|---|---|---|
| `AgentInteractionSnapshot` | 63 | Contracts `AgentInteraction/AgentInteractionSnapshot.cs:38` | exists, fields as AD-4 note |
| `ContentSafetyPolicyVersion` | 144 | snapshot, aggregates, orchestrators | exists |
| `ContextPolicyReference` | 68 | snapshot (`full-conversation-v1`), context policy | exists |
| `CapabilityVersion` | 305 | ProviderCatalog aggregate/state/view, Agent selection, setup read model | exists; monotonic rules enforced |
| `ProviderCapabilityVersion` (snapshot) | in 63 | snapshot | exists (provenance) |
| `EffectiveProviderCapabilityVersion` | 0 | - | absent (6.2/6.4/7.3) |
| `HighWater` | 0 | - | absent |
| `ProviderReadinessResult` | 0 | - | absent (5.5) |
| `ProviderReadinessReasonCode` | 0 | - | absent (5.5) |
| `OperationalState` | 0 | - | absent (5.5) |
| `Callability` | 1 | `AgentsOverview.razor:69` (label only) | no contract type |
| `ModelBudgetUnavailable` | 12 | context orchestrator, block reason enum | exists (AD-10 context clause) |
| `LaunchReadiness*` | 389 | Agent aggregate/policy/inspection, UI gateway | Epic-4 shape, not AD-17 aggregate |
| `LaunchReadinessGate`, `OperationGateMatrix`, `LR-*` | 0 | - | absent (5.5) |
| `BudgetLedger`, `Reservation`, `Admission`, `AdmissionFence`, `BeginInvocation`, `ProviderInvocationAuthorized` | 0 | - | absent (6.4/6.5) |
| `AttemptId` | 123 | proposal edit/regeneration identities, generation orchestrator | deterministic attempt ids exist (AD-13 partial) |
| `Dapr.Workflow`, `WorkflowActivity`, `Workflow<` | 0 in src (3 test guards) | `RuntimeOwnershipConformanceTests.cs:43` forbids | absent (6.1) |
| `Microsoft.Agents.AI` | 0 in src (3 test guards) | `PublicContractPackageBoundaryTests`, `ContractsBoundaryTests` | absent (correct - unselected) |

### C. Aggregates (AD-2 / AD-17 / AD-21)

| Aggregate | Exists | Location | Handlers |
|---|---|---|---|
| `Agent` | yes | `src/Hexalith.Agents/Agent/AgentAggregate.cs:47` | Create, UpdateConfiguration, Activate, Disable, LinkPartyIdentity, ReplacePartyIdentity, SelectProviderModel, ConfigureResponseMode, ConfigureApproverPolicy, ConfigureContentSafetyPolicy, **RecordAgentLaunchReadiness**, EnableProductionLikeGeneration |
| `ProviderCatalog` | yes | `ProviderCatalog/ProviderCatalogAggregate.cs:30` (`[EventStoreDomain("provider-catalog")]`) | Create/Update/Enable/Disable entry |
| `AgentInteraction` | yes | `AgentInteraction/AgentInteractionAggregate.cs:40` | Request, EvaluateGate, BuildContext, GenerateOutput, PostResponse, Create/Edit/Regenerate/Approve/Reject/Abandon/Expire proposal |
| `LaunchReadinessGate` | **no** | - | Story 5.5 |
| budget ledger | **no** | - | Story 6.4 |

### D. AD-17 projection ids

| Spine id | In `src/` as projection id | Notes |
|---|---|---|
| `agent-setup-readiness` | no | live projection is `agent-setup` (`AgentSetupReadModelAddresses.cs:43`) |
| `provider-capability-pricing` | no | live projection is `provider-catalog` (`ProviderCatalogReadModelAddresses.cs:15`) |
| `agent-interaction-status` | no | only query type `get-agent-interaction-status` |
| `proposal-detail`, `proposal-version-history`, `audit-evidence` | no | CSS/test-id/page names only |
| `pending-proposal-queue`, `pending-proposal-count`, `budget-reservation-usage`, `retention`, `legal-hold`, `export`, `deletion`, `launch-readiness`, `runtime-metrics`, `browser-ui-metrics`, `product-metrics` | no | prose/labels only |
| **not in list:** `agent-setup`, `provider-catalog` | **yes, live** | B-3 |

### E. Planning authorities

| Authority | Spine assumption | Current | Drift |
|---|---|---|---|
| `external-dependency-register.md` (updated 2026-08-09) | 7/7 `Uncommitted` | `EXT-HOST-1` `Committed` (Hexalith.Platform `a66cdf34`, 2026-09-30); 6 `Uncommitted` | B-2 |
| `launch-readiness-register.md` (2026-08-02) GateIds | 18 `LR-*` | 18 identical ids (TOPOLOGY 16, EVENTSTORE 16, TENANT-ACCESS 16, AUDIT-PROTECTION-DELETION 15, SECRETS 9, COST 8, PARTY-IDENTITY 8, SAFETY 7, CAPACITY-FAIRNESS 7, PROVIDER 6, TOKENIZER 6, CONVERSATION-CONTEXT 5, CONVERSATIONS-MEMBERSHIP-POSTING 4, RECOVERY 4, UI-CONFORMANCE 4, PRODUCT-METRICS 3, RUNTIME-PERFORMANCE 3, UI-PERFORMANCE 3 mentions) | none |
| register `OperationGateMatrix` v1 | 6 families named in AD-12 | 13 families (L89-101) | B-8 |
| register "Provider Readiness Contract" | AD-10 | identical field/enum list | none |
| `epics.md` story numbers | 5.1, 5.3, 5.5, 5.6, 6.1-6.6, 7.3, 7.4, 8.2-8.7 | same numbers, same meanings (L1164-2543) | none (B-12 caveat) |
| `sprint-status.yaml` | - | superseded 18-story Epic 5 slugs; 5.1 done, 5.2/5.3 review | B-12 |
| SCPs after 2026-08-02 | none consumed | 08-03 (major; EXT-CONV-UI-1, AD refs), 08-03 follow-up, 08-04 (44-story), 08-04 dev-agent-record (tooling only), 08-04 live-integration-tier (AD-17 amendment) | B-7, B-11, B-12 |
| `deferred-work.md` | - | DW-1..5, no deliberate AD deviation | B-17 |
| memlog | last 2026-08-02 | L80 AppHost constraint stale; L44/L60/L61 superseded | B-18 |

### F. Story 5.3 conformance to AD-9 / AD-10

| Rule | Evidence | Verdict |
|---|---|---|
| pricing units + currency + version | `ProviderModelPricing(Currency ISO-4217, InputTokenUnitPrice, OutputTokenUnitPrice, PricingVersion)` `.cs:14-18`; validated `ProviderCatalogAggregate.cs:470-499` | conforms (name `PricingVersion` vs spine "effective version") |
| `CapabilityVersion` 1 on create, +1 on update | `ProviderCatalogAggregate.cs:132`, `:195` | conforms |
| enable/disable never bump | `:219-292` emit lifecycle events only; `ProviderCatalogVersionRegressionTests.cs:78` | conforms |
| regression / reuse is a blocker | `TryGetCapabilityVersionRejection` `:385-420`; `ProviderModelCapabilityVersionRegressedRejection` | conforms |
| eligibility floor beyond enabled | `ProviderCatalogInspection.cs:85-95`; `ProviderSelectionVerdict.cs:39-71` (`Unpriced`, `Regressed`, `NotConfigured`, `MissingCapabilityMetadata`) | conforms |
| secret reference / configured state only | `ConfigurationReferenceId` + `ProviderConfigurationState`; `ProviderSecretLeakTests.cs`; grid never renders reference (`ProviderCatalog.razor:108`) | conforms |
| no Provider SDK / adapter | no package; `DeferredAgentGenerationProvider` bound (`Program.cs:124`) | conforms (but see B-4 gate) |
| readiness triple not inferred | writes return `Submitted` identity only (`EventStoreProviderCatalogOperations.cs:180`); UI Success never callability | conforms |
| new conventions | trusted `actor:agentsProviderAdmin`, `EventStoreAgentCommandDispatcher`, polling truth-flow | unratified (B-13) |

### G. Platform SDK seams (hexalith-state-instructions.md)

| Seam | Used | Evidence |
|---|---|---|
| two-line host `AddEventStoreDomainService` / `UseEventStoreDomainService` | yes | `Program.cs:29-31,216` |
| `IDomainQueryHandler` | yes | `Application/Queries/*QueryHandlerBase.cs`, `ServerAssemblyMarker.cs` |
| `IDomainProjectionHandler` | yes | `AgentSetupProjectionHandler.cs`, `ProviderCatalogProjectionHandler.cs` |
| `IReadModelStore` | yes | handlers, `ProjectedProviderCatalogReader.cs`, query handlers |
| `ReadModelWritePolicy` | **no** | 0 hits; available at pinned EventStore |
| `IQueryCursorCodec` / `QueryCursorScope` | no (n/a yet) | no paginated query |
| no module AppHost/Aspire/ServiceDefaults | yes | B-1 |
| state-store end-state assertions | partial | `*EventStoreIntegrationTests` assert `FakeReadModelStore` contents, not a live store |

---

## Disposition summary

| Id | Sev | Classification | Disposition |
|---|---|---|---|
| B-1 | high | stale-gap-note | autofix |
| B-2 | high | authority-changed | autofix |
| B-3 | high | code-diverged-from-AD | discuss |
| B-4 | high | authority-changed | discuss |
| B-5 | medium | stale-gap-note (partial) / expected-not-yet-built | autofix |
| B-6 | medium | code-diverged-from-AD (historical) / expected-not-yet-built | autofix (add note) |
| B-7 | medium | authority-changed | discuss |
| B-8 | medium | authority-changed | autofix (clarify) |
| B-9 | medium | stale / authority-changed | autofix |
| B-10 | medium | stale / unratified-new-convention | autofix |
| B-11 | medium | authority-changed | discuss |
| B-12 | medium | authority-changed (pending) | defer + note |
| B-13 | low | unratified-new-convention | autofix |
| B-14 | low | verified-current | ignore |
| B-15 | low | unratified deviation / expected-not-yet-built | discuss |
| B-16 | low | ratified-in-memlog | autofix |
| B-17 | low | expected-not-yet-built | defer |
| B-18 | low | stale (memlog) | autofix |
