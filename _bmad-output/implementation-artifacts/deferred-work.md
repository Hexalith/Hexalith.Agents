# Deferred Work

### DW-1: Platform/FrontComposer host must call AddAgentsUiSetup so the live AgentsClientSetupGateway replaces the deferred UI gateway in a runnable composition.

origin: migrated from legacy ledger (""), 2026-09-08
location: Platform/FrontComposer host composition
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-enforce-complete-launch-readiness-before-callability.md`
reason: Story 5.2 ships AddAgentsUiSetup and composition tests, but no in-repo host invokes it; platform-owned hosting is Story 5.6 / EXT-HOST-1.
status: open

### DW-2: Align setup-query authorization with live tenant-access evidence instead of the deferred ITenantAccessReader alongside HTTP Agents.Administrator role checks.

origin: migrated from legacy ledger (""), 2026-09-08
location: IDomainQueryHandler setup-query authorization
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-enforce-complete-launch-readiness-before-callability.md`
reason: The write/read HTTP path uses HttpAgentAdministrationContextProvider while IDomainQueryHandler setup queries still authorize via ITenantAccessReader; live tenant/Party binding is Story 5.4.
status: open

### DW-3: Optional post-write projection polling so AuthoritativePending can advance to ProjectionConfirmed without a manual reload.

status: done 2026-09-08
resolution: resolved by sweep bundle dw-setup-projection-polling
resolution-undo: 1304c76d4ea1a612802deacd61c7dc7b6f8996c6c45f352f2ebd118674e18e36 2026-09-08 7374617475733a206f70656e
origin: migrated from legacy ledger (""), 2026-09-08
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-enforce-complete-launch-readiness-before-callability.md`
archived: 2026-09-18

### DW-4: Expected configuration versions cannot correlate lifecycle writes or distinguish a specific accepted configuration write.
origin: spec-deferred 5378b7fc0051
location: src/Hexalith.Agents.UI/Components/Pages/AgentConfiguration.razor:310
source_spec: `spec-setup-projection-polling.md`
severity: medium
reason: This limitation predates the polling bundle: activation and disable do not increment ConfigurationVersion, while configuration writes expose only an expected version rather than the accepted command identity. A same-version lifecycle read can therefore confirm the pre-write lifecycle, and a concurrent configuration write can satisfy current+1. Correcting either case requires changing the API/projection correlation contract that this bundle explicitly leaves untouched.
status: open

### DW-5: The intent may require retaining AuthoritativePending alongside every typed terminal read failure rather than rendering the existing fail-closed terminal surface.
origin: spec-deferred 333d98453824
location: src/Hexalith.Agents.UI/Components/Pages/AgentConfiguration.razor:375
source_spec: `spec-setup-projection-polling.md`
reason: The phrase "preserving the truthful pending state" admits a strong reading, but the existing single-result UI contract replaces setup content with denied, missing, malformed, or unavailable surfaces. A product decision about whether prior pending truth must remain visible beside those terminal outcomes would settle the ambiguity.
status: open

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Reconcile AD-30 with the Hexalith.Tenants authority for non-tenant-scoped Platform Operator role freshness.
  evidence: Tenants documents `global-administrators` as separate from tenant-scoped `tenants`, while the Spine names the `system` tenant projection; the wrong source can reject or mis-authorize Platform commands (BH-8, EC-1).

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Define a viable, versioned `DigestKey` rotation and concurrency policy for retained and legally held content.
  evidence: AD-22/ARCH-A-12 makes ordinary incident-response rotation unavailable while protected content exists and calls rotation lock-bearing although the closed AD-12/readiness family inventory omits it (BH-9, BH-10, EC-3, EC-6).

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Define how overlapping Agent-disabled and tenant-kill-switch pauses contribute to `PausedDuration`.
  evidence: Both causes can overlap, but AD-5/AD-12 do not say whether elapsed overlap is unioned or summed, so implementations can compute different retry deadlines (BH-11, EC-5).

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Decide whether a Facilitator-set Conversation block is clearable only by the same Party or by any current Facilitator.
  evidence: PRD and AD-7 require the setting authority evaluated at clear time without identifying whether authority is principal identity or current role membership (BH-13, EC-2).

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Define the compliance-inspection second-party fallback when no unique eligible current Tenant Agent Administrator exists or that principal is the Inspector.
  evidence: The current documents do not settle this edge; Product/Architecture clarification is required to determine whether fail-closed stalling is intended (EC-7, unverified medium).

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Adjudicate the concurrent v7 Architecture findings and the already-triggered .NET `10.0.3xx` advisory escalation.
  evidence: Critical/high review findings remain outside the final Spine's source/triage record, including evidence that ARCH-A-4's advisory condition has fired without an accepted upgrade date or risk decision (BH-15, EC-16).

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Resolve the blocked/unavailable-call metric's reason-dimensional numerator against its pair-only denominator.
  evidence: The governing PRD formula can exceed 100% when one Party/Conversation pair has multiple reasons, potentially causing an unnecessary trigger review; changing it requires Product authority (EC-10).

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Specify atomicity or compensation for partial legal-hold DEK unpin failure.
  evidence: Story 8.1 requires restrictive failure but does not say how already-unpinned DEKs are re-pinned, leaving held content potentially erasable after a partial failure (EC-11).

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Reconcile the UX retry-posting failure transition with the PRD removal/block transition.
  evidence: EXPERIENCE says every pre-post validation failure returns to `PostingFailed`, while the PRD requires `Abandoned(RemovedInConversations)` after the message-absence guard confirms a removal or block (EC-13).

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Add the governed Platform Operator and second-Compliance-Inspector branches to the UX compliance-inspection approval path.
  evidence: EXPERIENCE exposes Tenant Agent Administrator approval/post-hoc review without the alternative approver eligibility required when the Administrator is in the subject set (EC-14).

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Reconcile UX retry-posting locking with the closed AD-12 operation-family contract.
  evidence: EXPERIENCE treats `ConversationPosting` as one of ten high-risk advisory-lock families, while the Spine and epics exclude it from the closed lock-bearing family set (EC-15).

- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations.md`
  summary: Implement immutable create-only `ProvisionHexa` and retire every Agent Party link/replace mutation path.
  evidence: Current Story 5.2 requires provision-only identity, but safe live completion depends on unresolved `EXT-PARTIES-1` behavior and Story 5.4 Platform-principal enforcement; implementing it in the lifecycle slice would create speculative security and cross-service orchestration.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations.md`
  summary: Correlate setup writes to their specific terminal command outcome and projection identity.
  evidence: Lifecycle N+1 polling prevents same-version false confirmation, but rejected/no-op submissions never reach that version, a timed-out submission permits another write from a stale baseline, and a concurrent different write can satisfy the threshold. Exact confirmation and safe re-submission need an accepted-command/outcome/projection correlation contract beyond this bounded remediation.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Implement immutable create-only `ProvisionHexa` and retire every legacy Agent Party link/replace mutation path after its replacement is available.
  evidence: The 2026-09-14 scope decision split exact setup-write correlation into the current slice; provisioning still depends on concrete Platform authority and `EXT-PARTIES-1`, and retiring the existing identity path before a verified provision-only replacement would leave deployed Agents without a safe recovery path.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Decide whether unresolved setup-write identity and payload must survive route navigation or browser reload.
  evidence: The component retains an exact attempt only for its lifetime; whether leaving and returning may create a new attempt while the first outcome is unresolved requires an explicit Product/UX state-lifetime decision.
  status: open

- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Prove exact setup result payload replay and projection through the production-like EventStore topology.
  evidence: Unit and component evidence covers the platform contract, but the Story 5.6 production-like host fixture is required to establish whether the payload survives the real EventStore completion, idempotency replay, and persisted projection path end to end.
  status: open

### DW-6: Eventful result payload forwarding depends on advisory command status reaching Completed.

origin: code review, 2026-09-14
location: references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Pipeline/SubmitCommandHandler.cs:538
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: EventStore deliberately drops the payload when the advisory status read has not reached Completed; a genuinely appended write therefore remains UnableToVerify until the platform policy is revisited.
status: open

### DW-7: Bind a live Agent command-status reader after the EventStore status API ships.

origin: code review, 2026-09-14
location: src/Hexalith.Agents.Server/Composition/AgentSetupServiceCollectionExtensions.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: The seam and rejection mapping are implemented, but package-mode builds cannot bind the live EventStore reader until the package containing GetCommandStatusAsync ships. The host-overridable deferred registration preserves fail-closed behavior meanwhile.
status: open

### DW-8: Prevent persistent MSBuild nodes from holding the readiness runner's captured output pipe.

status: done 2026-09-20
origin: code review, 2026-09-14
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
archived: 2026-09-21

### DW-9: Verify that an idempotency key is bound to its first submitted payload.

origin: code review round 2, 2026-09-14
location: references/Hexalith.EventStore
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
severity: medium
reason: Exact retry requires the same identity and payload; inspect EventStore's idempotency record handling to prove a changed payload cannot replay the first result.
status: open

### DW-10: Canonicalize correlation identifiers echoed by setup read paths.

status: done 2026-09-15
origin: code review round 2, 2026-09-14
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
archived: 2026-09-18

### DW-11: Reserve Unknown at zero if AgentSetupWriteStatus ever becomes a wire contract.

origin: code review round 2, 2026-09-14
location: src/Hexalith.Agents.Contracts/Agent/AgentSetupWriteStatus.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: The UI-internal enum predates this story with Submitted at zero; migration is required before it can safely be serialized.
status: open

### DW-12: Map Rejected to a terminal UI outcome when the live command-status reader is bound.

origin: code review round 2, 2026-09-14
location: src/Hexalith.Agents.UI/Services/Gateways/AgentsClientSetupGateway.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: Until the live reader is bound Rejected is unreachable; binding must add a terminal mapping and localized wording so retry does not replay a known rejection forever.
status: open

### DW-13: Extend Unknown-zero fallback to the remaining AgentInteraction and ProviderCatalog wire enums.

origin: code review round 2, 2026-09-14
location: src/Hexalith.Agents.Contracts
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: Story 5.2 covers its setup payload and operation terms; the remaining AgentInteraction and ProviderCatalog payload families still require a coordinated prospective-compatibility migration. AgentInspectionStatus remains deliberately excluded because zero means Success.
status: open

### DW-14: Complete the provider-catalog write path to the Story 5.2 acceptance contract.

origin: code review round 4, 2026-09-15
location: src/Hexalith.Agents.Server/Application/Agents/EventStoreProviderCatalogOperations.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: The provider path still discards the receipt, lacks effect/version evidence and canonical identity validation, and belongs to Story 5.3.
status: open

### DW-15: Propagate the per-class zero-test guard to the Story 5.3 verifier.

origin: code review round 4, 2026-09-15
location: eng/verify-story-5.3.ps1
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: That script's pre-existing filter can match nothing and still exit successfully.
status: open

### DW-16: Map operation failure bodies to appropriate HTTP statuses.

origin: code review round 4, 2026-09-15
location: src/Hexalith.Agents.Server/Api/AgentsOperationEndpoints.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: The pre-existing module-wide pattern returns HTTP 200 with typed failure bodies; changing that contract is outside this bounded story.
status: open

### DW-17: Decide how confirmed authoritative setup should interact with a divergent local draft.

origin: code review round 4, 2026-09-15
location: src/Hexalith.Agents.UI/Components/Pages/AgentConfiguration.razor
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
severity: medium
reason: Post-write reads deliberately preserve local display-name and description drafts; Product/UX must decide whether divergence needs a signal.
status: open

### DW-18: Group the configuration page's sibling titled sections into a FluentAccordion.

origin: code review round 4, 2026-09-15
location: src/Hexalith.Agents.UI/Components/Pages/AgentConfiguration.razor
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: The pre-existing ten-section page does not meet the loaded UX accordion convention; this story adds no titled section.
status: open

### DW-19: Establish whether the command-status seam is reachable through a production EventStore gateway.

origin: product-owner decision from code review round 4, 2026-09-15
location: references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Pipeline/SubmitCommandHandler.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
severity: medium
reason: Deterministic rejection may throw before returning an accepted payload-less receipt, while advisory status lag produces the same shape for success. Story 5.6's production-like topology must settle reachability before the live reader is bound.
status: open

### DW-20: Publish and consume the EventStore release that forwards no-op result payloads.

status: done 2026-09-16
origin: product-owner decision from code review round 4, 2026-09-15
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
archived: 2026-09-18

### DW-21: Register the Agents integration in the platform-owned EventStore gateway host.

origin: approved Story 5.2 replan, 2026-09-15
location: external Platform EventStore gateway composition
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
severity: blocker
reason: This repository publishes `Hexalith.Agents.EventStore` and proves its explicit registration in an isolated gateway provider, but it does not own the deployed Platform gateway host. Promotion requires that host to consume the package and call `AddAgentsEventStore` with the exact allow-listed Agents Dapr app id; without that call, reserved metadata and keyed setup commands fail closed before domain execution.
status: open

### DW-22: Verify that packed dependency asset-exclusion metadata prevents transitive build or analyzer leakage.

origin: code review of spec-5-1-adopt-hexalith-builds-as-the-sole-package-version-authority.md, 2026-09-17
location: scripts/validate-nuget-packages.py:197
source_spec: `_bmad-output/implementation-artifacts/spec-5-1-adopt-hexalith-builds-as-the-sole-package-version-authority.md`
severity: medium
reason: This may be a false positive because the review did not establish that current EventStore dependencies expose affected assets. The package validator currently discards nuspec dependency asset metadata; a mutated-package restore and `project.assets.json` comparison would settle whether omitting generated exclusions creates a consumer leak.
status: open

## Deferred from: code review of spec-5-1-adopt-hexalith-builds-as-the-sole-package-version-authority.md (2026-09-18)

- source_spec: `_bmad-output/implementation-artifacts/spec-5-1-adopt-hexalith-builds-as-the-sole-package-version-authority.md`
  summary: Make GlobalJsonShouldPinTheSdk assert the `test.runner` selection.
  evidence: The omission is pre-existing; `global.json` was unchanged by that story and already selects `Microsoft.Testing.Platform`, but the contract test does not pin it.
  status: open

- source_spec: `_bmad-output/implementation-artifacts/spec-implement-agents-ci-cd.md`
  summary: Determine whether another credential holder can race the NuGet absence proof for the six Agents package IDs and whether an atomic reservation mechanism exists.
  evidence: The absence probe and first push are necessarily separate operations; the risk becomes concrete only if another principal can publish the same IDs/version in that interval, which requires an authority inventory or NuGet reservation evidence to settle.
  status: open

## Deferred from: code review of spec-5-2-configure-hexa-through-live-eventstore-operations-2.md (2026-09-19)

### DW-23: Finish the `Unknown = 0` tolerance migration and make its guard traverse the Operations graph.

origin: code review, 2026-09-19
location: test/Hexalith.Agents.Contracts.Tests/AgentOperationContractsTests.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: Eight sibling `Hexalith.Agents.Contracts.Agent` enums (`ContentSafetyFailureHandling`, `ContentSafetyAuditTreatment`, `ApproverPolicySourceKind`, `ApproverPolicyValidationStatus`, `CostControlPosture`, `LaunchMetricClassification`, `PartyLinkValidationStatus`, `ProviderSelectionValidationStatus`) still declare the throwing `JsonStringEnumConverter` and escape `DiscoverPublicUnknownSentinelEnums` only because nothing on `AgentSetupView`/`AgentStatusView` references them. The Operations side is enumerated by namespace rather than traversed, so an `Unknown = 0` enum reaching the wire through an Operations record — `ContentSafetyAuditTreatment` on `AgentAuditGovernanceReadiness` today — is invisible to the guard despite its comment claiming either graph is "covered without editing this file". DW-13 is worded by namespace and does not name these. Widen the ledger wording, and replace `_deferredEnumNamespaces` with an explicit deferred-type list so a new Agent-namespace enum cannot fall through both nets.
status: open

### DW-24: Settle the tenant-scoped authorization contract for the gateway command-status read.

origin: code review, 2026-09-19
location: references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/CommandStatusController.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: `GetStatus` returns 403 for any principal without an `eventstore:tenant` claim and has no global-admin bypass, while `DaprInternalAuthenticationHandler` issues only `sub`, `NameIdentifier`, `global_admin` and `dapr_caller_app_id`. The submit path works for that identity only because `ClaimsTenantValidator` short-circuits for global admins. Binding the live `IAgentCommandStatusReader` through `AddEventStoreDaprServiceInvocation` would therefore yield 403 → `EventStoreGatewayException` → `UnableToVerify`, never the `Rejected` the round-1 resolution specifies. This is the concrete mechanism behind the round-4 item deferred as only possibly unreachable, and it blocks DW-12 and DW-19.
status: open

### DW-25 (carried): `Rejected`/`Blocked` map to retryable `Unavailable` in the UI.

origin: code review, 2026-09-19 (carried — owned by DW-12)
location: src/Hexalith.Agents.UI/Services/Gateways/AgentsClientSetupGateway.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: Re-confirmed this round that the `_ => AgentSetupWriteStatus.Unavailable` fall-through is unchanged and `IsTerminalWriteFailure` excludes `Unavailable`, so a rejected write renders as a transient outage with a Retry that replays the same rejected key. Unreachable in every composition that exists because only `DeferredAgentCommandStatusReader` is registered. Recorded as a pointer only — DW-12 owns both the live binding and this terminal mapping, and the two must land together.
status: open

### DW-26 (carried): `Unavailable` conflates "write path not bound" with "unreachable mid-flight".

origin: code review, 2026-09-19 (carried — round 2 rejected the same root cause)
location: src/Hexalith.Agents.UI/Services/Gateways/DeferredAgentSetupGateway.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: `DeferredAgentSetupGateway` returns `Unavailable` unconditionally because the write path is not bound, but `ExecuteAttemptAsync` treats `Unavailable` as non-terminal, so a single click retains the attempt and disables every mutable control until Abandon is pressed. A distinct `NotBound` status adds public surface for no user-visible gain while the deferred gateway resolves only when no Agent target is named; revisit when the live gateway binding lands with DW-12.
status: open

### DW-27: Revisit the repo-wide `-p:NuGetAudit=false` pin now its advisory is remediated.

origin: code review, 2026-09-19
location: eng/verify-story.ps1
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: The pin sits in `eng/verify-story.ps1` (4 sites), `eng/verify-story-5.2.ps1` (2) and `eng/verify-story-5.3.ps1` (1), so it is a repo-wide convention that predates and outlives Story 5.2; round 4 already adjudicated the 5.2 half as "keep and record the advisory". The advisory it was scoped to, `GHSA-pgww-w46g-26qg`, is now remediated in the shared Builds catalog (`AngleSharp 1.8.2` with that fix pinned by comment), so the suppression no longer hides a known blocker — it only hides future ones, against `hexalith-llm-instructions.md`'s rule that the pin is a triage-ladder fallback. Decide repo-wide whether to drop it from all three verifiers.
status: open

### DW-28: Settle the durable replay authorization contract for reserved command extensions.

origin: code review, 2026-09-19 (durable design behind the DW-flagged stopgap)
location: references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/ReplayController.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: The accepted fix strips colon-namespaced keys in `ArchivedCommandExtensions.ToSubmitCommand`, which is fail-closed but blunt — it applies to every integration's reserved keys and leaves Agents commands technically replayable yet always domain-rejected for want of `actor:agentsAdmin`. `ReplayController` authorizes on an `eventstore:tenant` claim alone while the replayed command runs as `UserId: "system"`, so replay authority and command authority are decoupled by design. The durable options are to require global-admin (or an explicit replay claim) when an archived command carries reserved keys, or to re-evaluate the trust policy against a principal the replay path can actually present. This is a platform authorization decision, not a domain-module one.
status: open

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Fail closed when a setup query reads a projection whose embedded tenant or Agent identity does not match its address.
  evidence: `AgentSetupQueryHandlerBase` passes the stored model directly to `AgentSetupViewFactory`, whose state reconstruction uses the embedded identifiers and metadata; unlike the operations path, no identity guard prevents a mis-keyed projection from disclosing another scope.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Render an unavailable setup state when the initial configuration-page read throws.
  evidence: `AgentConfiguration.OnInitializedAsync` catches disposal cancellation only, so a transport or deserialization exception from `LoadAsync` breaks initialization instead of using the page's existing unavailable state.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Define a fail-closed projection policy for malformed payloads of known Agent event types.
  evidence: `AgentSetupProjectionFold.Deserialize` returns null for corrupt JSON while `Fold` still advances the persisted sequence checkpoint, permanently presenting incomplete state as current; this behavior predates the current exact-correlation patch.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Pin inherited reusable security workflows to reviewed immutable revisions.
  evidence: CodeQL, commitlint, and dependency-review workflows consume mutable `@main` refs, so upstream branch movement can change repository gates without a local review.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Make multi-package NuGet publication recoverable after a partial push.
  evidence: The inherited release tooling requires the entire immutable version set to be absent before a non-transactional push; a partial service-side success leaves a package version set that the next run refuses to reconcile.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Restrict CodeQL concurrency cancellation so merged and scheduled revisions retain completed scans.
  evidence: The inherited CodeQL workflow cancels any run sharing the branch ref, including main pushes and scheduled scans, so a later run can erase the only completed evidence for an earlier merged SHA.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Reject GitHub releases marked prerelease when verifying a stable semantic-version tag.
  evidence: The inherited release verifier checks tag, draft status, source SHA, and assets but never checks the GitHub `prerelease` flag, so a release marked prerelease can satisfy the stable-publication gate.

## Deferred from: code review of spec-5-2-configure-hexa-through-live-eventstore-operations-2.md (2026-09-20)

Chunk 1 (production source) review. These items are carried onto existing ledger rows; no new DW keys.

Group A contracts re-review (second pass). Incomplete `Unknown = 0` tolerance migration remains DW-23; no new DW key.

Group A contracts re-review (third pass, story-file diffs including Contracts tests). Incomplete `Unknown = 0` tolerance migration remains DW-23; no new DW key.

Group A contracts re-review (fourth pass). Incomplete `Unknown = 0` tolerance migration remains DW-23; no new DW key.

- Domain rejection stays unverifiable: no live status reader, and `Rejected`/`Blocked` map to retryable `Unavailable`. Carried; owned by DW-7, DW-12, DW-19, DW-24, and DW-25.
- A failed catch-up read aborts polling and replaces the configuration form with Unavailable/Empty while the attempt is retained. Carried; owned by DW-5.
- `Unknown = 0` tolerance remains incomplete and `AgentSetupWriteStatus.Submitted = 0` is fail-open if it becomes a wire contract. Carried; owned by DW-11 and DW-23.
- Provider-catalog writes lack the configuration page's retained-attempt and exact-retry path. Carried; owned by DW-14 / Story 5.3.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Convert setup-query read-model-store failures into the typed unavailable result.
  evidence: `AgentSetupQueryHandlerBase.ExecuteAsync` lets a non-cancellation `IReadModelStore.GetAsync` failure escape, while the sibling provider query handler already catches the same infrastructure failure and returns its structured unavailable result; this behavior predates the current story patch.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Reject non-HTTP EventStore base-address schemes during Agents host composition.
  evidence: `AgentSetupServiceCollectionExtensions` accepts any absolute URI even though the configured HTTP client cannot send non-HTTP schemes, so an invalid pre-existing configuration can resolve the live setup services and then fail every request at runtime.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Verify the integrity and package identity of GitHub Release assets instead of accepting filenames alone.
  evidence: The inherited release verifier accepts empty, truncated, or substituted assets carrying the six expected names; NuGet publication checks protect the feed but do not validate the separate GitHub download artifacts.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Reject non-positive and non-finite NuGet publication verifier timeouts with a bounded diagnostic.
  evidence: The inherited CLI passes an explicit invalid timeout to URL opening without validation, so negative, NaN, or infinite values can crash or wait unpredictably instead of failing through `VerificationError`.

## Deferred from: code review of spec-5-2-configure-hexa-through-live-eventstore-operations-2.md (2026-09-21)

Group D1 (`test/Hexalith.Agents.Server.Tests/**`) review. These items are carried onto existing ledger rows; no new DW keys.

- Typed `Rejected` is proven only against a substitute; the composed host still registers `DeferredAgentCommandStatusReader`. Carried; owned by DW-7, DW-19, DW-24, and DW-25.
- Never-admitted stale/newer activation is unproven on the HTTP split-provider fabric because `BuildDomainApp` always returns `Applied`. Aggregate fence remains Group D3 (`AgentLifecycleConfigurationVersionTests`); a live HTTP proof needs the platform-owned domain processor, owned by Story 5.6 / DW-21.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Bind Agent administration scope, actor, and role claims to one unambiguous authenticated identity.
  evidence: `HttpAgentAdministrationContextProvider` resolves claims and roles across the whole principal, so a primary authenticated identity can borrow authority or a conflicting tenant from another identity; this provider and behavior predate the current Story 5.2 patch.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Define how confirmed setup writes reconcile editable drafts with a newer concurrent projection.
  evidence: Post-write polling deliberately preserves local drafts even when the projection advances beyond the accepted target; deciding whether to replace, merge, or flag those drafts requires a product/UX draft-versus-authority rule.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Add a negative regression test proving release source verification rejects non-main dispatch refs.
  evidence: The inherited source-proof suite passes `refs/heads/main` in every case, so deleting the explicit main-ref guard leaves all current tests green and can admit a branch ref that points at the current main commit.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Exercise the release source-proof CLI entry point through fixture-backed subprocess tests.
  evidence: Current tests invoke `verify_source_proof` directly; deleting the production CLI call leaves the suite green while the command exits successfully without checking main identity or CI evidence.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Align the completed Agents CI/CD specification with the immutable shared-workflow revision now used by ordinary CI.
  evidence: The completed CI/CD artifact still says ordinary CI tracks `domain-ci.yml@main`, while `.github/workflows/ci.yml` now pins the reusable workflow to a reviewed full commit SHA.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Exercise the GitHub Release and NuGet publication verifier CLI entry points through fixture-backed subprocess tests.
  evidence: The inherited tooling tests call helper functions directly, so deleting either production `main` verifier call leaves its suite green while the CLI can exit successfully without performing the release check.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Make high and critical NuGet vulnerability findings fail an ordinary CI build.
  evidence: The inherited build policy exempts `NU1901` through `NU1904` from warnings-as-errors, so a newly disclosed high or critical vulnerability in an existing dependency does not fail the warning-as-error lane.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Reject traversing NuGet archive paths before stripping an optional current-directory prefix.
  evidence: The inherited package validator applies `lstrip("./")` before its traversal check, allowing a normalized `../../lib/...` path to become `lib/...` and potentially satisfy the exact DLL inventory.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Derive semantic-release planning and publication from one release configuration.
  evidence: The inherited planner hardcodes branches, tag format, and plugins separately from `.releaserc.json`, so a later release-rule change can produce a different reserved version and block publication.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Enforce the centrally declared package license, provenance URLs, and readme in package validation.
  evidence: The inherited package validator requires only a nonempty license and does not compare the expected MIT expression, repository/project URLs, or readme, so a project override can pass the release gate with inconsistent metadata.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Re-prove that the dispatched source is still current immediately before the first irreversible NuGet push.
  evidence: The release workflow proves current green main before planning and packing, but has no local publication-boundary recheck, so main can advance before the first package write despite the completed CI/CD requirement to stop stale source before publication.

## Deferred from: code review of spec-5-2-configure-hexa-through-live-eventstore-operations-2.md (2026-09-22)

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Pin the CodeQL, commitlint, and dependency-review callers to an exact reviewed revision.
  evidence: The three security workflows consume `Hexalith.Builds` reusable workflows from mutable `@main` refs while the same change pins the CI and release callers to an exact revision, and the CodeQL caller grants security-events write to an unpinned workflow, so upstream movement can alter this repository's gates with no local review. Carried from earlier review rounds.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Decide whether high and critical NuGet audit diagnostics should fail the build.
  evidence: `Directory.Build.props` enables NuGet auditing across all dependencies and in the same property group appends NU1901 through NU1904 to `WarningsNotAsErrors`, so warnings-as-errors lanes report advisories and pass; a build-contract test pins that combination, and the story text scopes the audit exception to one inherited advisory, so closing it also requires renegotiating that text. Carried from an earlier review round.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Make the release planner read `.releaserc.json` instead of restating branches and tag format.
  evidence: The planner hardcodes its own branch list and tag format; a probe run in a scratch repository carrying a different tag format confirmed the rc file is fully overridden, so editing either file alone makes the planner reserve a version the real run never produces and the release aborts at the reserved-version check after packing. The actionable residue is a tooling test asserting the two sources agree. Carried from an earlier review round.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Document the Conventional Commits, semantic-release, Node tooling, and test-runner changes for contributors.
  evidence: The change enforces Conventional Commits on pull-request titles and pushes to main, adds a Node toolchain and semantic-release, gates publication behind a repository variable, and switches the test runner, while the readme and agent instructions mention none of it and no contributing guide exists. Part of the fix edits agent-context files.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Reconcile the `references/Hexalith.EventStore` gitlink with the released package version the floor gate requires.
  evidence: `git describe --tags` on the gitlink returns a revision twenty commits past the `v3.106.0` tag, so Debug source mode compiles unreleased EventStore code while Release consumes the published package and a green source-mode lane does not prove the package-mode lane. Deferred because moving the gitlink changes the File List and invalidates the chunk evidence already recorded for this story, so it needs its own run.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Show that the Builds, Commons, Conversations, FrontComposer, and PolymorphicSerializations gitlinks are required by the Agents EventStore integration.
  evidence: The EventStore integration diff moves those five pins with the EventStore pin. Nothing in that diff shows the new adapters need them. Unverified medium: an unnecessary pin can pull unrelated submodule behavior into this story. Settle by rebuilding `Hexalith.Agents.EventStore` against the previous five pins.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-3.md`
  summary: Make CI and release evidence honest for Story 5.2 floors, pins, skips, package layout, Dependabot, and unpublished dispatches.
  evidence: Split from the live-setup correctness spec because token count was 2491. The deferred slice is test floors and skip rejection in agents-policy, aligning the domain-ci and domain-release pin with the Builds gitlink, verifier skip and source-mode gaps, checkout credentials, npm signature audit, package-count and test-project drift, Dependabot gitlinks, and the unpublished-release signal. That last signal is still undecided: fail the workflow when publication is disabled, or stay green and write a job summary that publication did not run.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-3.md`
  summary: Stop a create command from storing a payload tenant that disagrees with the envelope tenant.
  evidence: `CreateAgent.TenantId` is stored by `AgentAggregate` from the payload, while idempotency scopes the canonical target by the envelope tenant. This slice hashes the declared payload and does not choose the event tenant; the approved spec forbids changing Party identity. The split predates this change.

## Deferred from: code review of spec-5-2-configure-hexa-through-live-eventstore-operations-3.md (2026-09-22)

- A create payload tenant can disagree with the envelope tenant. `AgentAggregate.Handle(CreateAgent)` stores `command.TenantId`, while setup idempotency scopes the canonical target by the envelope tenant. Pre-existing; this spec forbids changing Party identity. Same split as the bullet above.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Require Agents administrator authority before disclosing live setup queries.
  evidence: `AgentSetupQueryHandlerBase.IsAgentsAdminAsync` treats fresh tenant access as administrator authority, so an ordinary tenant member can receive administrator setup details through a direct EventStore query; this authorization path predates the current review pass.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Fail provider catalog projection deliveries closed when a known event payload is corrupt.
  evidence: `ProviderCatalogProjectionFold.Fold` stops on a malformed payload but `ProviderCatalogProjectionHandler` can write the partial model and acknowledge the delivery, leaving later events unapplied; this projection policy predates the current setup-correlation patch.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Publish only archives declared in the release package manifest.
  evidence: The inherited pack script preserves unrelated archives in `nupkgs`, while `publish-release-packages.sh` pushes the whole `nupkgs/*.nupkg` glob; a stray archive can bypass the manifest preflight.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Reject a direct trusted CreateAgent command whose payload tenant differs from the EventStore envelope tenant.
  evidence: `AgentAggregate.Handle(CreateAgent)` stores `command.TenantId` without comparing it to `envelope.TenantId`; the supplied orchestrator rewrites the tenant but a direct or misconfigured trusted caller can bypass that caller-side guard. This repeats the previously recorded domain-invariant risk for this review finding.

## Deferred from: code review of spec-5-2-configure-hexa-through-live-eventstore-operations-2.md (2026-09-23)

Server orchestration chunk review. Carried onto existing ledger rows; no new DW keys.

- The `WriteAsync` `Rejected` branch is unreachable because only `DeferredAgentCommandStatusReader` is registered. A domain-rejected setup command, such as a stale or blocked activation, returns a retryable `UnableToVerify`. Carried; owned by DW-7, DW-12, DW-19, DW-24, and DW-25.

## Deferred from: code review of spec-5-2-configure-hexa-through-live-eventstore-operations-2.md (2026-09-23, aggregate and contracts chunk)

Aggregate and contracts chunk review. One new item; the rest are carried onto existing ledger rows.

- New: six operation enums (`AgentReadinessStatus`, `AgentCallOperationStatus`, `AuditAvailabilityStatus`, `ProposalOperationStatus`, `ProviderModelReadinessStatus`, `OperationalStatusInspectionStatus`) read numeric tokens through `UnknownFallbackEnumConverter`, but their members after `Unknown = 0` still have implicit ordinals and are not covered by `ShippedSetupEnumsKeepTheirPinnedOrdinals`. Inserting a member would silently change what a numeric peer's value means. The fix is to pin the current ordinals explicitly, without renumbering, and add them to the pin test. Pre-existing; spec -3 pinned setup enums only.
- `AgentSetupWriteStatus.Submitted = 0` has no fallback converter. Carried; owned by DW-11.
- `AgentInspectionStatus.Success = 0` is deliberately excluded from the fallback. Carried; owned by DW-13.
- A stale activation's `AgentActivationConfigurationVersionMismatchRejection` surfaces as retryable `UnableToVerify`, not `Stale` or `Conflict`. Carried; owned by DW-7, DW-12, and DW-25.
- The Agent setup-event enums still use the throwing `JsonStringEnumConverter`. Carried; owned by DW-23.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Compare published NuGet package bytes with the release archives during publication verification.
  evidence: The inherited `verify-nuget-publication.py` accepts a matching registration-leaf ID and version without downloading or hashing the package, so the publication gate cannot establish that NuGet serves the same archive as the release artifacts.

## Deferred from: code review of spec-5-2-configure-hexa-through-live-eventstore-operations-3.md (2026-09-23)

- CI runs only `verify-story-5.2.ps1 -PackageFloorOnly` and the Story 5.1 floor, so a missing Story 5.2 focused class can leave CI green while broad test counts remain above their minima. Pre-existing; carried under the existing CI and release evidence row.
- The Story 5.2 verifier's project-level regression calls have neither `--fail-skips on` nor minimum test counts, unlike its focused class calls. Skipped or undiscovered regression tests can leave the local verifier green. Pre-existing; carried under the existing CI and release evidence row.
- `AgentAggregate.Handle(CreateAgent)` stores `CreateAgent.TenantId` while the setup adapter scopes canonical intent by the envelope tenant. A trusted direct caller can submit a mismatched pair. Pre-existing; the approved slice does not change Party identity, and this is already recorded above.
- The domain host still falls back to `DeferredAgentCommandStatusReader`, so a domain-rejected setup write lacks a live terminal status and remains unverifiable. Pre-existing; carried under DW-7, DW-12, DW-19, DW-24, and DW-25.
- `AgentInspectionStatus.Success = 0` makes a missing status field deserialize as success status with a null setup payload. Pre-existing; this type is deliberately excluded from the Unknown-zero migration and is tracked under the existing enum-compatibility work.
- Six public operation enums still have implicit nonzero ordinals despite numeric legacy reads. A future insertion can reinterpret a shipped numeric value. Pre-existing; the setup-only pin test does not cover them, and the same finding is recorded in the aggregate-and-contracts chunk above.

- source_spec: `spec-5-2-configure-hexa-through-live-eventstore-operations-3.md`
  summary: Exercise the Story 5.2 focused class-presence gates in CI and reject skipped or undiscovered tests in its regression lane.
  evidence: The resumed review confirms CI invokes only PackageFloorOnly plus Story 5.1 broad floors, while verify-story-5.2.ps1 regression calls still omit skip rejection and minimum counts; these inherited gaps precede the normalization follow-up.

- source_spec: `spec-5-2-configure-hexa-through-live-eventstore-operations-3.md`
  summary: Define the coupled update path for Dependabot proposals that advance the Builds gitlink.
  evidence: A gitlink-only proposal fails the literal Builds SHA assertions in CI and PackageInventoryTests until workflow and test pins move together; preserve the consistency gate and supply automation or explicit maintainer instructions in the separate dependency-maintenance work.

- source_spec: `spec-5-2-configure-hexa-through-live-eventstore-operations-3.md`
  summary: Add executable fixture coverage for the focused Story 5.2 verifier's success, skip, unrun, and zero-discovery outcomes.
  evidence: The verification-gap reviewer found only PackageFloorOnly execution and source-text checks; reverting Invoke-TestClasses to its earlier total-only behavior would leave those checks green. This inherited CI/tooling change predates the normalization follow-up.

## Deferred from: code review of spec-5-2-configure-hexa-through-live-eventstore-operations-3.md (2026-09-23, re-review)

- `AgentDomainHostComposition.Configure` carries about 180 lines of story-by-story comments moved verbatim from `Program.cs`, and several of them say live command dispatch "stays deferred behind DeferredAgentCommandDispatcher" (for example line 72). That contradicts the Story 5.2 `Agents:EventStore:BaseUrl` block in the same method. Pre-existing; trim or correct them when that composition is next edited.
