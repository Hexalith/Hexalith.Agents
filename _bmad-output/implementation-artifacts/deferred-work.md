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

- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Prove exact setup result payload replay and projection through the production-like EventStore topology.
  evidence: Unit and component evidence covers the platform contract, but the Story 5.6 production-like host fixture is required to establish whether the payload survives the real EventStore completion, idempotency replay, and persisted projection path end to end.

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

origin: code review, 2026-09-14
location: tools/check-story-review-readiness.py
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: A persistent MSBuild child can keep the captured output pipe open until the runner's timeout; setting MSBUILDDISABLENODEREUSE inside the runner would make the workaround automatic.
status: open

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

- GlobalJsonShouldPinTheSdk does not assert `test.runner` — pre-existing; `global.json` is unchanged in this diff and already contains `Microsoft.Testing.Platform`.

- source_spec: `_bmad-output/implementation-artifacts/spec-implement-agents-ci-cd.md`
  summary: Determine whether another credential holder can race the NuGet absence proof for the six Agents package IDs and whether an atomic reservation mechanism exists.
  evidence: The absence probe and first push are necessarily separate operations; the risk becomes concrete only if another principal can publish the same IDs/version in that interval, which requires an authority inventory or NuGet reservation evidence to settle.
