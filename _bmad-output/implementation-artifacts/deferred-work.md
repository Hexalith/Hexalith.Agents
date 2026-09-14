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

origin: migrated from legacy ledger (""), 2026-09-08
location: Post-write setup-status projection refresh
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-enforce-complete-launch-readiness-before-callability.md`
reason: The spec accepts a single post-write re-read; a lagging projection keeps the submission pending until the administrator reloads or acts again.
status: done 2026-09-08
resolution: resolved by sweep bundle dw-setup-projection-polling
resolution-undo: 1304c76d4ea1a612802deacd61c7dc7b6f8996c6c45f352f2ebd118674e18e36 2026-09-08 7374617475733a206f70656e

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

## Deferred from: code review of spec-5-2-configure-hexa-through-live-eventstore-operations-2 (2026-09-14)

- Eventful `ResultPayload` is dropped whenever the durable advisory status read is not `Completed`.
  `references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Pipeline/SubmitCommandHandler.cs:538` gates
  payload forwarding on `finalStatus?.Status == CommandStatus.Completed` and otherwise logs `ResultPayloadDropped`.
  The gating is deliberate and separately tested in EventStore, and it lives in a different repository boundary,
  so it is not actionable from this story. Story 5.2's new fail-closed correlation is what makes it visible:
  a write that genuinely appended its event is reported to the administrator as `UnableToVerify` whenever the
  advisory status write has not landed by the time it is read. Revisit alongside the no-op payload fix.

- Domain-rejection correlation is seamed but not bound. `IAgentCommandStatusReader` and the mapping of a rejected
  command to `AgentOperationErrorCode.Rejected` are implemented and tested, but the live reader is absent because it
  needs `IEventStoreGatewayClient.GetCommandStatusAsync`, which is added in the submodule and not yet released.
  Release builds resolve Hexalith libraries by package reference, so Agents cannot bind to it until that package
  ships. Until then a domain rejection still renders as retryable `UnableToVerify`. Going live is a single
  registration change in `AgentSetupServiceCollectionExtensions`.

- `tools/check-story-review-readiness.py` can hang for its full 900 s timeout on a test project. `default_runner`
  uses `subprocess.run(capture_output=True)`, which waits for the stdout pipe to reach EOF rather than for the child
  to exit, so a persistent MSBuild node that inherited that pipe keeps it open and the gate times out. Observed twice
  on `Hexalith.Agents.Client.Tests`, whose 6 tests finish in 0.14 s when the identical command is run directly.
  Workaround: `dotnet build-server shutdown` and run the gate with `MSBUILDDISABLENODEREUSE=1`. A durable fix is to
  set that variable inside `default_runner` for `dotnet` commands.
