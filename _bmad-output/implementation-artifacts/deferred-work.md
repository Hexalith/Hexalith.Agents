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

## Deferred from: code review of spec-5-2-configure-hexa-through-live-eventstore-operations-2 (2026-09-14, round 2)

- The idempotency key is not bound to the payload it was first used with. `AgentOperationOptions` documents that
  "An exact retry must reuse the same correlation ID, idempotency key, and payload", but nothing in the Contracts
  or domain layer enforces the payload half. A retry that reuses the key with a changed payload could be shown the
  first command's effect and target version. Unverified — settled by inspecting EventStore's idempotency-record
  handling in `references/Hexalith.EventStore` for whether a replayed key with a different payload is rejected.
  Would be medium if confirmed.

- Caller-supplied `CorrelationId` may be echoed on read paths without canonical-ULID validation.
  `EventStoreAgentAdministrationOperations.cs:210,222` were cited; the write paths do validate via
  `IsCanonicalUlid`, and the read paths fall outside the Contracts/domain chunk reviewed here. Unverified —
  settled by the Group 3 (Server) chunk review. Would be medium if confirmed.

- `AgentSetupWriteStatus` reserves zero for `Submitted` rather than `Unknown`, so a default or absent value reads
  as "accepted for processing" — fail-open, against the `Unknown = 0` convention every neighbouring operations
  enum follows. Pre-existing: `Submitted = 0` was already declared inside `AgentSetupWriteResult.cs` at
  `baseline_commit`; the current round only split the enum into its own file and appended three members. It is
  UI-internal today (the client gateway maps into it; it never crosses HTTP), so the fail-open default is not
  currently reachable from a wire payload. Revisit if the status is ever serialized.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Map `AgentOperationStatus.Rejected` to a terminal Agent setup write status before the live `IAgentCommandStatusReader` is registered.
  evidence: `AgentsClientSetupGateway.ToWriteStatus` has no `Rejected` arm, so a rejection falls through to `Unavailable`, which `IsTerminalWriteFailure` excludes; the page would keep the attempt and offer a Retry that replays the same idempotency key against the same rejection forever. Unreachable today because only `DeferredAgentCommandStatusReader` is registered and it always answers `null`, so this must land with — and no later than — the single registration change that binds the live reader, together with the localized wording a rejection should show.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Extend the `Unknown = 0` fallback converter to the remaining public contract enums on Epic 2/3/4 payloads.
  evidence: About 45 `Unknown = 0` enums in `Hexalith.Agents.Contracts` (the `AgentInteraction` and `ProviderCatalog` families, plus `OperationalStatusInspectionStatus`) still declare the throwing `JsonStringEnumConverter`, so an additive member still fails a whole response for an older client on those routes. Story 5.2 closes this for its own setup payload only; the rest is the same latent defect on payloads outside this story's reach and should be swept once, with `AgentInspectionStatus` deliberately excluded because its zero is `Success = 0` and the fallback would fail open.

## Deferred from: code review of spec-5-2-configure-hexa-through-live-eventstore-operations-2 (2026-09-15, round 4)

- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Complete the provider-catalog write path to the Story 5.2 acceptance contract.
  evidence: `ProviderCatalogAdministrationOrchestrator` now returns `AgentAdministrationOutcome.FromDispatch(receipt)`, but `EventStoreProviderCatalogOperations.WriteAsync` discards `outcome.Receipt`, reports `AgentSetupTruthState.Submitted` with no effect and no target version, never verifies receipt identity, and accepts any caller string as `IdempotencyKey`/`CorrelationId` while the sibling Agent path requires a canonical ULID. One public surface answers two contradictory acceptance contracts. Completing it belongs to Story 5.3.
  status: open

- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Propagate the zero-test guard to `eng/verify-story-5.3.ps1`.
  evidence: That script still selects its focused and composition gates with `--filter`, which prints "No test matches the given testcase filter" and exits 0, so a renamed or deleted suite silently turns a gate into a no-op — the defect `Invoke-TestClasses` was written to close in the 5.2 verifier. Pre-existing and outside this story's diff.
  status: open

- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Map every write outcome code to an HTTP status instead of returning 200 with a failure body.
  evidence: The write handlers return `AgentOperationResult<AgentCommandAcceptance>` directly, so `ValidationFailed`, `NotAuthorized`, `Rejected` and `UnableToVerify` all arrive as 200; `SendWriteAsync` only calls `EnsureSuccessStatusCode()`, so no test would notice. Pre-existing module-wide pattern, not introduced by this story.
  status: open

- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Decide whether the page must signal that a local draft diverges from confirmed authoritative setup.
  evidence: Both post-write read call sites now pass `applyAuthoritativeDrafts: false`, so a concurrent administrator's `DisplayName`/`Description` change never surfaces while `agents-config-truth-stage` renders `ProjectionConfirmed`. Unverified at `medium`: whether this is a defect or the intended precedence is a UX decision. What would settle it: a product call on draft-versus-authority precedence.
  status: open

- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Group the ten sibling titled sections of `AgentConfiguration.razor` into a single `FluentAccordion`.
  evidence: `hexalith-ux-instructions.md` requires two or more sibling titled content sections to be grouped in one `FluentAccordion`; the page renders ten with raw `<dl>`, `<ul>`, `<p>` and no accordion. Pre-existing — this story adds no titled section.
  status: open

- source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Establish whether the command-status seam is reachable through the live EventStore gateway at all.
  evidence: `SubmitCommandHandler` calls `ThrowDeterministicFailure` for every `!Accepted` processing result, so a domain rejection throws `DomainCommandRejectedException` (409/422) and is already mapped by the existing dispatch `try/catch` to `Conflict`/`ValidationFailed`; an accepted-but-payload-less receipt may therefore never correspond to a rejection, and the test that proves the `Rejected` branch fabricates that shape rather than producing it. The same shape is produced for a genuine success whenever the advisory status read is not `Completed`, which is what makes the question open rather than settled. If the shape is unreachable, `IAgentCommandStatusReader`, `IEventStoreGatewayClient.GetCommandStatusAsync` and the `Rejected` branch are public surface added to a shared technical module for a path the pipeline never produces, against the frozen Never clause. Unverified at medium by product-owner decision (2026-09-15). What would settle it: a production-like EventStore topology run, i.e. Story 5.6's host fixture.
  status: open
