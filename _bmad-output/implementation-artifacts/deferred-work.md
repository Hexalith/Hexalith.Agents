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
status: done 2026-10-23
resolution: `spec-five-deferred-contract-and-setup-items.md` verifies the existing canonical command receipt, domain-derived effect/target version and incrementing lifecycle versions. The new lifecycle regression carries an actual disable DomainResult through receipt validation and AgentSetupProjectionHandler, observes the old projection as pending, then asserts persisted Disabled/version state and a confirmed public view. Full local source verification passes; production topology qualification remains separately governed.

### DW-5: The intent may require retaining AuthoritativePending alongside every typed terminal read failure rather than rendering the existing fail-closed terminal surface.
origin: spec-deferred 333d98453824
location: src/Hexalith.Agents.UI/Components/Pages/AgentConfiguration.razor:375
source_spec: `spec-setup-projection-polling.md`
reason: The phrase "preserving the truthful pending state" admits a strong reading, but the existing single-result UI contract replaces setup content with denied, missing, malformed, or unavailable surfaces. A product decision about whether prior pending truth must remain visible beside those terminal outcomes would settle the ambiguity.
status: open

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Reconcile AD-30 with the Hexalith.Tenants authority for non-tenant-scoped Platform Operator role freshness.
  evidence: Tenants documents `global-administrators` as separate from tenant-scoped `tenants`, while the Spine names the `system` tenant projection; the wrong source can reject or mis-authorize Platform commands (BH-8, EC-1).
  status: done 2026-10-23
  resolution: `spec-five-deferred-planning-reconciliation-items.md` — AD-30 explicitly requires fresh non-tenant-scoped global-administrators evidence for Platform and distinguishes the tenant-scoped tenants source. ActorTenantId=system is not a role-source selector; live authority qualification is unchanged.
  evidence_record: [Planning reconciliation](deferred-planning-reconciliation-2026-10-23.md#platform-operator-authority)

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Define a viable, versioned `DigestKey` rotation and concurrency policy for retained and legally held content.
  evidence: AD-22/ARCH-A-12 makes ordinary incident-response rotation unavailable while protected content exists and calls rotation lock-bearing although the closed AD-12/readiness family inventory omits it (BH-9, BH-10, EC-3, EC-6).
  status: done 2026-10-23
  resolution: `spec-five-deferred-planning-reconciliation-items.md` — AD-22/AD-29 specify future-version rotation, retained originating key versions, recorded DigestKeyVersion comparisons across retries and exclusion from browser locks. This closes the policy-definition gap only; ARCH-A-12 still requires Security confirmation or a superseding procedure before RQ-1.
  evidence_record: [Planning reconciliation](deferred-planning-reconciliation-2026-10-23.md#versioned-digestkey-rotation-and-concurrency)

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Define how overlapping Agent-disabled and tenant-kill-switch pauses contribute to `PausedDuration`.
  evidence: Both causes can overlap, but AD-5/AD-12 do not say whether elapsed overlap is unioned or summed, so implementations can compute different retry deadlines (BH-11, EC-5).
  status: done 2026-10-23
  resolution: `spec-five-deferred-planning-reconciliation-items.md` — PRD FR-18, AD-12 and EXPERIENCE agree on the union of pause intervals, counting overlap once and resuming only after both causes clear. Awaiting-proposal expiry continues independently; no runtime timer qualification is claimed.
  evidence_record: [Planning reconciliation](deferred-planning-reconciliation-2026-10-23.md#overlapping-agent-and-tenant-pauses)

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Decide whether a Facilitator-set Conversation block is clearable only by the same Party or by any current Facilitator.
  evidence: PRD and AD-7 require the setting authority evaluated at clear time without identifying whether authority is principal identity or current role membership (BH-13, EC-2).
  status: done 2026-10-23
  resolution: `spec-five-deferred-governance-planning-items.md` — AD-7 and EXPERIENCE require the setting Party still to hold the Facilitator role, or the current Tenant Agent Administrator. Any-current-Facilitator authority applies only to the system-set ExternallyRemoved branch. This closes the planning question; the Conversations seam and A-9 remain qualification dependencies.
  evidence_record: [Governance reconciliation](deferred-governance-reconciliation-2026-10-23.md#facilitator-block-clearing-authority)

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Define the compliance-inspection second-party fallback when no unique eligible current Tenant Agent Administrator exists or that principal is the Inspector.
  evidence: The current documents do not settle this edge; Product/Architecture clarification is required to determine whether fail-closed stalling is intended (EC-7, unverified medium).
  status: done 2026-10-23
  resolution: `spec-five-deferred-governance-planning-items.md` — FR-24 and AD-22/AD-30 already refuse missing/ambiguous authority and Inspector-self approval. EXPERIENCE now distinguishes those refusals from the established Administrator-in-subject Platform/second-Inspector branch, and AD-22/Story 8.8 use the governing PRD subject-set condition consistently. Uncertainty grants no automatic alternate; a broader fallback would require a new Product/Governance decision. No runtime inspection grant or qualification is added.
  evidence_record: [Governance reconciliation](deferred-governance-reconciliation-2026-10-23.md#inspection-without-an-established-eligible-administrator)

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Adjudicate the concurrent v7 Architecture findings and the already-triggered .NET `10.0.3xx` advisory escalation.
  evidence: Critical/high review findings remain outside the final Spine's source/triage record, including evidence that ARCH-A-4's advisory condition has fired without an accepted upgrade date or risk decision (BH-15, EC-16).

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Resolve the blocked/unavailable-call metric's reason-dimensional numerator against its pair-only denominator.
  evidence: The governing PRD formula can exceed 100% when one Party/Conversation pair has multiple reasons, potentially causing an unnecessary trigger review; changing it requires Product authority (EC-10).
  status: done 2026-10-23
  resolution: `spec-five-deferred-governance-planning-items.md` — Current FR-28 and AD-12 use distinct calling Parties in both numerator and denominator, deduplicating repeated calls/reasons/Conversations within each rolling seven-day aggregate share. Reason breakdowns do not sum into that share; existing exclusions, convening floor, samples and thresholds remain binding. This closes the formula definition, not metrics implementation.
  evidence_record: [Governance reconciliation](deferred-governance-reconciliation-2026-10-23.md#blocked-and-unavailable-shares)

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Specify atomicity or compensation for partial legal-hold DEK unpin failure.
  evidence: Story 8.1 requires restrictive failure but does not say how already-unpinned DEKs are re-pinned, leaving held content potentially erasable after a partial failure (EC-11).
  status: done 2026-10-23
  resolution: `spec-five-deferred-governance-planning-items.md` — Current Story 8.1 and AD-2/AD-22 define staged release: the guard remains restrictive through every exact unpin/fence receipt, overlapping deletion waits for mirrored guard release, and phase-pinned exact lookup/retry preserves the release decision after partial failure. Physical expiry/destruction also require linearizable authorization. This supplies the existing serialization alternative to atomic unpin or re-pin compensation; actual custodian/provider qualification and open lifecycle/cancellation decisions remain required.
  evidence_record: [Governance reconciliation](deferred-governance-reconciliation-2026-10-23.md#restrictive-partial-hold-release)

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Reconcile the UX retry-posting failure transition with the PRD removal/block transition.
  evidence: EXPERIENCE says every pre-post validation failure returns to `PostingFailed`, while the PRD requires `Abandoned(RemovedInConversations)` after the message-absence guard confirms a removal or block (EC-13).
  status: done 2026-10-23
  resolution: `spec-five-deferred-planning-reconciliation-items.md` — PRD FR-18 and AD-5/AD-7 already distinguish Abandoned(RemovedInConversations) from other pre-post PostingFailed outcomes. EXPERIENCE now also preserves their no-post lookup exception and typed deleted/removed abandonment-only semantics; present and unavailable lookup safeguards remain intact.
  evidence_record: [Planning reconciliation](deferred-planning-reconciliation-2026-10-23.md#retry-removalblock-transitions-and-existence-lookup)

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Add the governed Platform Operator and second-Compliance-Inspector branches to the UX compliance-inspection approval path.
  evidence: EXPERIENCE exposes Tenant Agent Administrator approval/post-hoc review without the alternative approver eligibility required when the Administrator is in the subject set (EC-14).
  status: done 2026-10-23
  resolution: `spec-five-deferred-governance-planning-items.md` — EXPERIENCE groups both alternatives with the current-Administrator branch, authority/self-approval refusal and viewer-scoped discovery/approval/review on the existing audit list/detail. Its contract handoff and Story 8.8 assign the required public verdict/review fields and deferred-until-available behavior. Narrow-scope post-hoc limits, prior Platform approval for wide cases, anti-collusion and A-19/A-23 remain in force; no new role or grant is introduced.
  evidence_record: [Governance reconciliation](deferred-governance-reconciliation-2026-10-23.md#alternate-inspection-approvers-in-ux)

- source_spec: `_bmad-output/implementation-artifacts/spec-synchronize-second-2026-09-09-prd-downstream-planning.md`
  summary: Reconcile UX retry-posting locking with the closed AD-12 operation-family contract.
  evidence: EXPERIENCE treats `ConversationPosting` as one of ten high-risk advisory-lock families, while the Spine and epics exclude it from the closed lock-bearing family set (EC-15).
  status: done 2026-10-23
  resolution: `spec-five-deferred-planning-reconciliation-items.md` — AD-5/AD-12 and EXPERIENCE classify public administrative retry as lock-bearing ProposalResolution followed by workflow-only ConversationPosting. The latter owns no browser advisory lock; the closed set remains ten families.
  evidence_record: [Planning reconciliation](deferred-planning-reconciliation-2026-10-23.md#administrative-retry-and-browser-locking)

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
status: done 2026-10-23
resolution: `spec-five-deferred-package-integrity-items.md` verifies first-intent binding at pinned EventStore `5ce5f1d84b88c808cc8a526b9decf149de977e55`: four exact actor cases prove first reservation retains IntentDigest, equivalent retry reuses the original execution identity/fence, and a different intent conflicts before replay or state mutation. All 141 root Debug/source gateway cases pass, including equivalent payload spellings replaying the original result while a changed mode conflicts with one domain execution. The checked-in gateway-verification.json records commands, source revisions and XML hashes. This is local unit/in-process evidence; external Dapr persistence remains outside this proof.

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
status: done 2026-10-23
resolution: `spec-five-deferred-contract-and-setup-items.md` migrates 44 interaction and two provider Unknown-zero enums without changing names or ordinals. Future scalar names/numbers degrade to Unknown, configured enum naming policies and known dictionary keys round-trip, and unknown dictionary keys fail closed rather than merge facts. The one previously numeric status retains its default numeric writer and explicit string-options encoding. Flags and Success-zero enums keep their prior behavior. The full 1,970-case Contracts suite passes.

### DW-14: Complete the provider-catalog write path to the Story 5.2 acceptance contract.

origin: code review round 4, 2026-09-15
location: src/Hexalith.Agents.Server/Application/Agents/EventStoreProviderCatalogOperations.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: The provider path still discards the receipt, lacks effect/version evidence and canonical identity validation, and belongs to Story 5.3.
status: done 2026-10-23
resolution: `spec-five-deferred-contract-and-setup-items.md` completes canonical caller/generated identity validation across all six provider write families, rejecting empty/lowercase/overflowing/GUID/whitespace IDs before dispatch. Tenant writes validate supplied IDs before dependency reads; authorization remains first. Exact retries retain their pair and payload, mismatched receipts cannot confirm or query another command, and existing receipt/effect/outcome/projection and retained-UI regressions all pass in the 1,491-case Server and 1,171-case UI suites. This is local source evidence, not external host qualification.

### DW-15: Propagate the per-class zero-test guard to the Story 5.3 verifier.

origin: code review round 4, 2026-09-15
location: eng/verify-story-5.3.ps1
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: That script's pre-existing filter can match nothing and still exit successfully.
status: done 2026-10-23
resolution: `spec-five-deferred-projection-verification-items.md` replaces combined VSTest filters with independent native xUnit class/method selections. Every focused, composition and full-project run requires a nonzero test summary and rejects failures, skips and not-run tests; controlled subprocess fixtures exercise missing selections and runner failures.

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
status: done 2026-10-23
resolution: `spec-five-deferred-contract-and-setup-items.md` groups all ten titled configuration sections in one FluentAccordion using actual Fluent UI v5 Header parameters and level-two headings. Sections start expanded and retain user collapse across parent renders; editing controls remain available, with activation actions and the write live region outside. Structure, collapse/edit and all existing configuration regressions pass within the 1,171-case full UI suite.

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
status: done 2026-10-23
resolution: `spec-five-deferred-package-integrity-items.md` retains nuspec include/exclude metadata and rejects dependencies that effectively include ordinary Build assets. A maintained isolated local-feed restore regression compares project.assets.json and evaluated build properties: generated Build,Analyzers exclusions block the build-only leaf, while omitted or Analyzers-only exclusions import it; Compile,Runtime inclusion blocks it. Synthetic archive regressions cover omitted, weakened, case-varied and restricted metadata. Analyzer items and buildTransitive assets have distinct NuGet semantics; this closes the demonstrated ordinary-build omission gap without claiming those categories are suppressed.

## Deferred from: code review of spec-5-1-adopt-hexalith-builds-as-the-sole-package-version-authority.md (2026-09-18)

- source_spec: `_bmad-output/implementation-artifacts/spec-5-1-adopt-hexalith-builds-as-the-sole-package-version-authority.md`
  summary: Make GlobalJsonShouldPinTheSdk assert the `test.runner` selection.
  evidence: The omission is pre-existing; `global.json` was unchanged by that story and already selects `Microsoft.Testing.Platform`, but the contract test does not pin it.
  status: done 2026-10-23
  resolution: `spec-five-deferred-build-verification-items.md` The existing global.json contract now requires Microsoft.Testing.Platform alongside SDK fields; all 1,345 Server Debug/source tests pass.

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
status: done 2026-10-23
resolution: `spec-five-deferred-contract-and-setup-items.md` migrates the six remaining Agent enums and covers all 70 public Unknown-zero enums, including nested Operations payloads and externally visible nested types. Namespace exclusions are replaced by three exact closed-directory protocol deferrals; five non-Unknown-zero exceptions are explicit. An embedded pre-migration member/ordinal snapshot prevents renumbering. Direct nested audit payload, configured serializer factory, numeric/name compatibility and future-value regressions pass in the full 1,970-case Contracts suite.

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
status: done 2026-10-23
resolution: `spec-five-deferred-admission-and-verification-items.md` removes routine audit suppression from all five verification scripts. Default source/package builds audit every dependency; production-script fixtures propagate NU1903 failures without running later gates. Explicit diagnostics-only negative probes in test fixtures remain separately scoped.

### DW-28: Settle the durable replay authorization contract for reserved command extensions.

origin: code review, 2026-09-19 (durable design behind the DW-flagged stopgap)
location: references/Hexalith.EventStore/src/Hexalith.EventStore/Controllers/ReplayController.cs
source_spec: `_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
reason: The accepted fix strips colon-namespaced keys in `ArchivedCommandExtensions.ToSubmitCommand`, which is fail-closed but blunt — it applies to every integration's reserved keys and leaves Agents commands technically replayable yet always domain-rejected for want of `actor:agentsAdmin`. `ReplayController` authorizes on an `eventstore:tenant` claim alone while the replayed command runs as `UserId: "system"`, so replay authority and command authority are decoupled by design. The durable options are to require global-admin (or an explicit replay claim) when an archived command carries reserved keys, or to re-evaluate the trust policy against a principal the replay path can actually present. This is a platform authorization decision, not a domain-module one.
status: open

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Fail closed when a setup query reads a projection whose embedded tenant or Agent identity does not match its address.
  evidence: `AgentSetupQueryHandlerBase` passes the stored model directly to `AgentSetupViewFactory`, whose state reconstruction uses the embedded identifiers and metadata; unlike the operations path, no identity guard prevents a mis-keyed projection from disclosing another scope.
  status: done 2026-10-23
  resolution: `spec-five-deferred-setup-boundary-items.md` verifies the existing `AgentSetupViewFactory` identity guard through both query entry points, including foreign, blank and case-mismatched tenant/Agent values; unavailable results carry no setup data and never invoke the approver resolver. Existing matching/missing controls and all 1,306 Server Debug/source tests pass.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Render an unavailable setup state when the initial configuration-page read throws.
  evidence: `AgentConfiguration.OnInitializedAsync` catches disposal cancellation only, so a transport or deserialization exception from `LoadAsync` breaks initialization instead of using the page's existing unavailable state.
  status: done 2026-10-23
  resolution: `spec-five-deferred-setup-boundary-items.md` renders the existing typed unavailable surface for synchronous/asynchronous initial transport, JSON and unrelated cancellation failures, without exception details or mutation controls. Disposal cancellation and late results preserve page state. All 1,169 UI Debug/source tests pass.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Define a fail-closed projection policy for malformed payloads of known Agent event types.
  evidence: `AgentSetupProjectionFold.Deserialize` returns null for corrupt JSON while `Fold` still advances the persisted sequence checkpoint, permanently presenting incomplete state as current; this behavior predates the current exact-correlation patch.
  status: done 2026-10-23
  resolution: `spec-five-deferred-projection-verification-items.md` validates deserialization of every pending known event before the handler writes any batch, returning the existing retryable reason for malformed syntax, empty bytes, JSON null or incompatible JSON. Direct folding stops before the invalid event; persisted-state regressions cover whole-batch rejection, rebuild rejection, corrected redelivery and checkpoint-covered duplicates.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Pin inherited reusable security workflows to reviewed immutable revisions.
  evidence: CodeQL, commitlint, and dependency-review workflows consume mutable `@main` refs, so upstream branch movement can change repository gates without a local review.
  status: done 2026-10-23
  resolution: `spec-five-deferred-ci-release-followups.md` pins all three callers to the already reviewed CI/release and Builds gitlink revision `359fee24aeca4d9eabed4b425d4cfa2520b2adda`; workflow lint and source inspection verify the callers. The 2026-09-22 pinning row below records the same item.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Make multi-package NuGet publication recoverable after a partial push.
  evidence: The inherited release tooling requires the entire immutable version set to be absent before a non-transactional push; a partial service-side success leaves a package version set that the next run refuses to reconcile.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Restrict CodeQL concurrency cancellation so merged and scheduled revisions retain completed scans.
  evidence: The inherited CodeQL workflow cancels any run sharing the branch ref, including main pushes and scheduled scans, so a later run can erase the only completed evidence for an earlier merged SHA.
  status: done 2026-10-23
  resolution: `spec-five-deferred-ci-release-followups.md` limits cancellation and shared-ref grouping to pull requests; main and scheduled runs use their unique run ID so neither active nor pending scans displace another revision.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Reject GitHub releases marked prerelease when verifying a stable semantic-version tag.
  evidence: The inherited release verifier checks tag, draft status, source SHA, and assets but never checks the GitHub `prerelease` flag, so a release marked prerelease can satisfy the stable-publication gate.
  status: done 2026-10-22
  resolution: `spec-five-deferred-release-verification-items.md` requires explicit prerelease=false for stable versions, including versions with build metadata; direct and subprocess regression fixtures cover invalid flags and preserve prerelease-version support.

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
  status: done 2026-10-23
  resolution: `spec-five-deferred-setup-boundary-items.md` translates non-cancellation store faults after authorization into `AgentSetupResult.Unavailable`, leaving approver enrichment outside the catch and rechecking caller cancellation before returning. Both query types have synchronous/asynchronous fault, denial-before-store and cancellation-race regressions; all 1,306 Server Debug/source tests pass.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Reject non-HTTP EventStore base-address schemes during Agents host composition.
  evidence: `AgentSetupServiceCollectionExtensions` accepts any absolute URI even though the configured HTTP client cannot send non-HTTP schemes, so an invalid pre-existing configuration can resolve the live setup services and then fail every request at runtime.
  status: done 2026-10-23
  resolution: `spec-five-deferred-setup-boundary-items.md` requires HTTP or HTTPS before registering live gateway services. Five unsupported absolute schemes with a valid Dapr AppId preserve the deferred dispatcher/client/readers and omit live operations/gateway registration; HTTP and HTTPS positive controls pass with all 1,306 Server Debug/source tests.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Verify the integrity and package identity of GitHub Release assets instead of accepting filenames alone.
  evidence: The inherited release verifier accepts empty, truncated, or substituted assets carrying the six expected names; NuGet publication checks protect the feed but do not validate the separate GitHub download artifacts.
  status: done 2026-10-23
  resolution: `spec-five-deferred-package-integrity-items.md` requires all downloaded manifest archives, validates package identity and boundaries with bounded ZIP reads, then matches API size and SHA-256 before success. The release workflow downloads the exact tag assets and invokes this gate. Actual CLI fixtures reject corrupt/substituted/wrong-identity assets, missing or extra files, bad size and missing/malformed/mismatched digests.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Reject non-positive and non-finite NuGet publication verifier timeouts with a bounded diagnostic.
  evidence: The inherited CLI passes an explicit invalid timeout to URL opening without validation, so negative, NaN, or infinite values can crash or wait unpredictably instead of failing through `VerificationError`.
  status: done 2026-10-22
  resolution: `spec-five-deferred-release-verification-items.md` validates positive finite timeouts before probing; direct and bounded subprocess tests cover zero, negative, NaN and infinite inputs for both modes and assert zero HTTP requests.

## Deferred from: code review of spec-5-2-configure-hexa-through-live-eventstore-operations-2.md (2026-09-21)

Group D1 (`test/Hexalith.Agents.Server.Tests/**`) review. These items are carried onto existing ledger rows; no new DW keys.

- Typed `Rejected` is proven only against a substitute; the composed host still registers `DeferredAgentCommandStatusReader`. Carried; owned by DW-7, DW-19, DW-24, and DW-25.
- Never-admitted stale/newer activation is unproven on the HTTP split-provider fabric because `BuildDomainApp` always returns `Applied`. Aggregate fence remains Group D3 (`AgentLifecycleConfigurationVersionTests`); a live HTTP proof needs the platform-owned domain processor, owned by Story 5.6 / DW-21.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Bind Agent administration scope, actor, and role claims to one unambiguous authenticated identity.
  evidence: `HttpAgentAdministrationContextProvider` resolves claims and roles across the whole principal, so a primary authenticated identity can borrow authority or a conflicting tenant from another identity; this provider and behavior predate the current Story 5.2 patch.
  status: done 2026-10-23
  resolution: `spec-five-deferred-setup-boundary-items.md` verifies the existing single-authenticated-identity provider and current-evidence authority path. Regressions reject multiple authenticated identities and conflicting aliases before authority reads, ignore unauthenticated secondary claims, reject borrowed missing fields and preserve agreeing aliases without granting claim-based roles; all 1,306 Server Debug/source tests pass.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Define how confirmed setup writes reconcile editable drafts with a newer concurrent projection.
  evidence: Post-write polling deliberately preserves local drafts even when the projection advances beyond the accepted target; deciding whether to replace, merge, or flag those drafts requires a product/UX draft-versus-authority rule.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Add a negative regression test proving release source verification rejects non-main dispatch refs.
  evidence: The inherited source-proof suite passes `refs/heads/main` in every case, so deleting the explicit main-ref guard leaves all current tests green and can admit a branch ref that points at the current main commit.
  status: done 2026-10-22
  resolution: `spec-five-deferred-release-verification-items.md` adds branch, tag and pull-ref regressions with the exact main SHA and green main CI; a real CLI fixture also rejects a non-main dispatch.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Exercise the release source-proof CLI entry point through fixture-backed subprocess tests.
  evidence: Current tests invoke `verify_source_proof` directly; deleting the production CLI call leaves the suite green while the command exits successfully without checking main identity or CI evidence.
  status: done 2026-10-22
  resolution: `spec-five-deferred-release-verification-items.md` runs the production CLI against temporary JSON fixtures for exact green main, non-main dispatch, stale source, failed CI and empty CI, asserting exit codes and diagnostics.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Align the completed Agents CI/CD specification with the immutable shared-workflow revision now used by ordinary CI.
  evidence: The completed CI/CD artifact still says ordinary CI tracks `domain-ci.yml@main`, while `.github/workflows/ci.yml` now pins the reusable workflow to a reviewed full commit SHA.
  status: done 2026-10-23
  resolution: `spec-five-deferred-ci-release-followups.md` adds an explicit current-state supersession of the historical frozen ordinary-CI instruction and corrects the CI/release implementation notes to the full revision actually used by the workflows.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Exercise the GitHub Release and NuGet publication verifier CLI entry points through fixture-backed subprocess tests.
  evidence: The inherited tooling tests call helper functions directly, so deleting either production `main` verifier call leaves its suite green while the CLI can exit successfully without performing the release check.
  status: done 2026-10-22
  resolution: `spec-five-deferred-release-verification-items.md` exercises real bounded subprocesses using GitHub JSON and loopback NuGet fixtures, covering success, wrong source/assets, prereleases, complete inventory checks, collisions, partial publication and malformed responses.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Make high and critical NuGet vulnerability findings fail an ordinary CI build.
  evidence: The inherited build policy exempts `NU1901` through `NU1904` from warnings-as-errors, so a newly disclosed high or critical vulnerability in an existing dependency does not fail the warning-as-error lane.
  status: done 2026-10-23
  resolution: `spec-five-deferred-build-verification-items.md` NU1903/NU1904 are no longer exempt from warnings-as-errors; low/moderate exemptions remain. The existing build guard enforces this policy, the CI/CD artifact records the supersession, and ordinary audit-enabled Release/package restore/build pass without warnings or errors.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Reject traversing NuGet archive paths before stripping an optional current-directory prefix.
  evidence: The inherited package validator applies `lstrip("./")` before its traversal check, allowing a normalized `../../lib/...` path to become `lib/...` and potentially satisfy the exact DLL inventory.
  status: done 2026-10-23
  resolution: `spec-five-deferred-package-tooling-items.md` rejects traversal segments, absolute paths and Windows drive paths before normalization, preserves leading dots, and exercises malicious DLL and other asset paths through synthetic archive fixtures.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Derive semantic-release planning and publication from one release configuration.
  evidence: The inherited planner hardcodes branches, tag format, and plugins separately from `.releaserc.json`, so a later release-rule change can produce a different reserved version and block publication.
  status: done 2026-10-24
  resolution: `spec-five-deferred-evidence-reconciliation-items.md` — Reconciles the older open reference to the already implemented shared-config planner. The planner reads branches, tag format and analysis options from .releaserc.json and now refuses unsupported analysis overrides, unreviewed plugins and exec analysis hooks before generating a divergent plan; ten executable scratch-repository tests verify decisions, refusals and hook isolation. This closes the duplicate reference without claiming another planner implementation.
  evidence_record: [Evidence reconciliation](deferred-evidence-reconciliation-2026-10-24.md#shared-release-configuration)

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Enforce the centrally declared package license, provenance URLs, and readme in package validation.
  evidence: The inherited package validator requires only a nonempty license and does not compare the expected MIT expression, repository/project URLs, or readme, so a project override can pass the release gate with inconsistent metadata.
  status: done 2026-10-23
  resolution: `spec-five-deferred-package-tooling-items.md` reads the central build metadata policy and requires its expression license, project/repository URLs, repository type and one nonempty packaged README; fixture regressions reject missing, duplicated or conflicting metadata.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Re-prove that the dispatched source is still current immediately before the first irreversible NuGet push.
  evidence: The release workflow proves current green main before planning and packing, but has no local publication-boundary recheck, so main can advance before the first package write despite the completed CI/CD requirement to stop stale source before publication.
  status: done 2026-10-23
  resolution: `spec-five-deferred-admission-and-verification-items.md` verifies the existing production publisher recheck with the actual source-proof CLI and controlled gh endpoints after complete local preflight and NuGet absence. Exact green main proceeds; main advancement, failed CI and either API failure prevent every package push.

## Deferred from: code review of spec-5-2-configure-hexa-through-live-eventstore-operations-2.md (2026-09-22)

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Pin the CodeQL, commitlint, and dependency-review callers to an exact reviewed revision.
  evidence: The three security workflows consume `Hexalith.Builds` reusable workflows from mutable `@main` refs while the same change pins the CI and release callers to an exact revision, and the CodeQL caller grants security-events write to an unpinned workflow, so upstream movement can alter this repository's gates with no local review. Carried from earlier review rounds.
  status: done 2026-10-23
  resolution: `spec-five-deferred-ci-release-followups.md` resolves this carried duplicate with the same three immutable security-workflow pins as the earlier row; it is one deferred work item.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Decide whether high and critical NuGet audit diagnostics should fail the build.
  evidence: `Directory.Build.props` enables NuGet auditing across all dependencies and in the same property group appends NU1901 through NU1904 to `WarningsNotAsErrors`, so warnings-as-errors lanes report advisories and pass; a build-contract test pins that combination, and the story text scopes the audit exception to one inherited advisory, so closing it also requires renegotiating that text. Carried from an earlier review round.
  status: done 2026-10-24
  resolution: `spec-five-deferred-evidence-reconciliation-items.md` — The existing adopted policy exempts only NU1901/NU1902, audits every dependency and fails NU1903/NU1904. Fresh effective Release-property tests and controlled verifier failures confirm it. The completed Story 5.2 specification now carries an explicit current-policy supersession outside frozen historical intent; no new exception or risk acceptance is granted.
  evidence_record: [Evidence reconciliation](deferred-evidence-reconciliation-2026-10-24.md#high-and-critical-nuget-audit-diagnostics)

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Make the release planner read `.releaserc.json` instead of restating branches and tag format.
  evidence: The planner hardcodes its own branch list and tag format; a probe run in a scratch repository carrying a different tag format confirmed the rc file is fully overridden, so editing either file alone makes the planner reserve a version the real run never produces and the release aborts at the reserved-version check after packing. The actionable residue is a tooling test asserting the two sources agree. Carried from an earlier review round.
  status: done 2026-10-23
  resolution: `spec-five-deferred-ci-release-followups.md` derives branches, tag format, and analysis-plugin options from `.releaserc.json`, excludes publication plugins, and exercises custom branches, tags and analyzer rules through the real isolated planner.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Document the Conventional Commits, semantic-release, Node tooling, and test-runner changes for contributors.
  evidence: The change enforces Conventional Commits on pull-request titles and pushes to main, adds a Node toolchain and semantic-release, gates publication behind a repository variable, and switches the test runner, while the readme and agent instructions mention none of it and no contributing guide exists. Part of the fix edits agent-context files.
  status: done 2026-10-23
  resolution: `spec-five-deferred-package-tooling-items.md` adds a README-linked contributor guide with locked Node setup, commitlint validation, semantic-release planning/manual publication/freeze policy, Debug/source and Release/package modes, and Microsoft.Testing.Platform project-level commands.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Reconcile the `references/Hexalith.EventStore` gitlink with the released package version the floor gate requires.
  evidence: `git describe --tags` on the gitlink returns a revision twenty commits past the `v3.106.0` tag, so Debug source mode compiles unreleased EventStore code while Release consumes the published package and a green source-mode lane does not prove the package-mode lane. Deferred because moving the gitlink changes the File List and invalidates the chunk evidence already recorded for this story, so it needs its own run.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Show that the Builds, Commons, Conversations, FrontComposer, and PolymorphicSerializations gitlinks are required by the Agents EventStore integration.
  evidence: The EventStore integration diff moves those five pins with the EventStore pin. Nothing in that diff shows the new adapters need them. Unverified medium: an unnecessary pin can pull unrelated submodule behavior into this story. Settle by rebuilding `Hexalith.Agents.EventStore` against the previous five pins.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-3.md`
  summary: Make CI and release evidence honest for Story 5.2 floors, pins, skips, package layout, Dependabot, and unpublished dispatches.
  evidence: Split from the live-setup correctness spec because token count was 2491. The deferred slice is test floors and skip rejection in agents-policy, aligning the domain-ci and domain-release pin with the Builds gitlink, verifier skip and source-mode gaps, checkout credentials, npm signature audit, package-count and test-project drift, Dependabot gitlinks, and the unpublished-release signal. That last signal is still undecided: fail the workflow when publication is disabled, or stay green and write a job summary that publication did not run.
  status: done 2026-10-24
  resolution: `spec-five-deferred-evidence-reconciliation-items.md` — Reconciles every sub-item with the existing workflows, exact Builds gitlink/ref alignment, source/package floors, focused native-result guards, credential/signature rules, five test projects, six-package manifest, Dependabot and documented green frozen-publication summary. Fresh 26-case conformance and production-script tooling checks pass. Local automation evidence does not claim a hosted run or package publication.
  evidence_record: [Evidence reconciliation](deferred-evidence-reconciliation-2026-10-24.md#story-52-ci-and-release-evidence)

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-3.md`
  summary: Stop a create command from storing a payload tenant that disagrees with the envelope tenant.
  evidence: `CreateAgent.TenantId` is stored by `AgentAggregate` from the payload, while idempotency scopes the canonical target by the envelope tenant. This slice hashes the declared payload and does not choose the event tenant; the approved spec forbids changing Party identity. The split predates this change.
  status: done 2026-10-23
  resolution: `spec-five-deferred-admission-and-verification-items.md` rejects ordinal payload/envelope tenant mismatches before creation or duplicate no-op using the existing invalid-configuration rejection. Absent/existing-state, case/whitespace, immutable-state and authorization-precedence regressions pass; valid create/replay controls remain intact.

## Deferred from: code review of spec-5-2-configure-hexa-through-live-eventstore-operations-3.md (2026-09-22)

- A create payload tenant can disagree with the envelope tenant. `AgentAggregate.Handle(CreateAgent)` stores `command.TenantId`, while setup idempotency scopes the canonical target by the envelope tenant. Pre-existing; this spec forbids changing Party identity. Same split as the bullet above. Resolved by `spec-five-deferred-admission-and-verification-items.md` with the existing create-tenant item above; this carried pointer is not an additional work item.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Require Agents administrator authority before disclosing live setup queries.
  evidence: `AgentSetupQueryHandlerBase.IsAgentsAdminAsync` treats fresh tenant access as administrator authority, so an ordinary tenant member can receive administrator setup details through a direct EventStore query; this authorization path predates the current review pass.
  status: done 2026-10-23
  resolution: `spec-five-deferred-admission-and-verification-items.md` binds the query to the authenticated subject and exact tenant, requires current independent administrator evidence plus fresh tenant access, and denies before store/enrichment otherwise. Source and pinned EventStore 3.126.0 package forwarding use only the SDK-verified human principal, preserving the transport identity. Direct, DI, cancellation and real-dispatch regressions cover both query handlers.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Fail provider catalog projection deliveries closed when a known event payload is corrupt.
  evidence: `ProviderCatalogProjectionFold.Fold` stops on a malformed payload but `ProviderCatalogProjectionHandler` can write the partial model and acknowledge the delivery, leaving later events unapplied; this projection policy predates the current setup-correlation patch.
  status: done 2026-10-23
  resolution: `spec-five-deferred-projection-verification-items.md` verifies the already implemented provider preflight guard, which rejects the entire delivery before folding or writing and rejects corrupt rebuilds before store access. Added absent/existing-store mixed-batch, corrected-redelivery, covered-duplicate and rebuild regressions assert complete persisted snapshots, sequence and command-identity windows.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Publish only archives declared in the release package manifest.
  evidence: The inherited pack script preserves unrelated archives in `nupkgs`, while `publish-release-packages.sh` pushes the whole `nupkgs/*.nupkg` glob; a stray archive can bypass the manifest preflight.
  status: done 2026-10-23
  resolution: `spec-five-deferred-package-tooling-items.md` preflights the entire exact manifest/version set and pushes each validated path explicitly. Controlled publisher subprocesses prove stray and old-version archives are preserved and excluded, and missing/corrupt packages or failed proofs prevent every push.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Reject a direct trusted CreateAgent command whose payload tenant differs from the EventStore envelope tenant.
  evidence: `AgentAggregate.Handle(CreateAgent)` stores `command.TenantId` without comparing it to `envelope.TenantId`; the supplied orchestrator rewrites the tenant but a direct or misconfigured trusted caller can bypass that caller-side guard. This repeats the previously recorded domain-invariant risk for this review finding.
  status: done 2026-10-23
  resolution: `spec-five-deferred-admission-and-verification-items.md` resolves this carried duplicate with the same aggregate tenant invariant and regression coverage as the earlier create row; it is one deferred item.

## Deferred from: code review of spec-5-2-configure-hexa-through-live-eventstore-operations-2.md (2026-09-23)

Server orchestration chunk review. Carried onto existing ledger rows; no new DW keys.

- The `WriteAsync` `Rejected` branch is unreachable because only `DeferredAgentCommandStatusReader` is registered. A domain-rejected setup command, such as a stale or blocked activation, returns a retryable `UnableToVerify`. Carried; owned by DW-7, DW-12, DW-19, DW-24, and DW-25.

## Deferred from: code review of spec-5-2-configure-hexa-through-live-eventstore-operations-2.md (2026-09-23, aggregate and contracts chunk)

Aggregate and contracts chunk review. One new item; the rest are carried onto existing ledger rows.

- New: six operation enums (`AgentReadinessStatus`, `AgentCallOperationStatus`, `AuditAvailabilityStatus`, `ProposalOperationStatus`, `ProviderModelReadinessStatus`, `OperationalStatusInspectionStatus`) read numeric tokens through `UnknownFallbackEnumConverter`, but their members after `Unknown = 0` still have implicit ordinals and are not covered by `ShippedSetupEnumsKeepTheirPinnedOrdinals`. Inserting a member would silently change what a numeric peer's value means. The fix is to pin the current ordinals explicitly, without renumbering, and add them to the pin test. Pre-existing; spec -3 pinned setup enums only.
  status: done 2026-10-23
  resolution: `spec-five-deferred-build-verification-items.md` All six operation enums now explicitly assign every existing ordinal without renumbering. The shipped enum pin test verifies all members and exact numeric values; all 731 Contracts Debug/source tests pass with numeric fallback coverage intact.
- `AgentSetupWriteStatus.Submitted = 0` has no fallback converter. Carried; owned by DW-11.
- `AgentInspectionStatus.Success = 0` is deliberately excluded from the fallback. Carried; owned by DW-13.
- A stale activation's `AgentActivationConfigurationVersionMismatchRejection` surfaces as retryable `UnableToVerify`, not `Stale` or `Conflict`. Carried; owned by DW-7, DW-12, and DW-25.
- The Agent setup-event enums still use the throwing `JsonStringEnumConverter`. Carried; owned by DW-23.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-2-configure-hexa-through-live-eventstore-operations-2.md`
  summary: Compare published NuGet package bytes with the release archives during publication verification.
  evidence: The inherited `verify-nuget-publication.py` accepts a matching registration-leaf ID and version without downloading or hashing the package, so the publication gate cannot establish that NuGet serves the same archive as the release artifacts.
  status: done 2026-10-23
  resolution: `spec-five-deferred-package-integrity-items.md` makes presence CLI require locally validated expected archives, download each exact flat-container package within the existing bounded retry budget, and compare every ordinary ZIP member by SHA-256. Only the root repository-signature member may differ, as NuGet.org changes it during signing. Publisher and release verification pass their respective original/verified GitHub archives. CLI fixtures prove successful comparison, signing allowance, substitution/corruption rejection, missing-package retry exhaustion, transient recovery and zero-network preflight failure.

## Deferred from: code review of spec-5-2-configure-hexa-through-live-eventstore-operations-3.md (2026-09-23)

- CI runs only `verify-story-5.2.ps1 -PackageFloorOnly` and the Story 5.1 floor, so a missing Story 5.2 focused class can leave CI green while broad test counts remain above their minima. Pre-existing; carried under the existing CI and release evidence row.
- The Story 5.2 verifier's project-level regression calls have neither `--fail-skips on` nor minimum test counts, unlike its focused class calls. Skipped or undiscovered regression tests can leave the local verifier green. Pre-existing; carried under the existing CI and release evidence row.
- `AgentAggregate.Handle(CreateAgent)` stores `CreateAgent.TenantId` while the setup adapter scopes canonical intent by the envelope tenant. A trusted direct caller can submit a mismatched pair. Pre-existing; the approved slice does not change Party identity, and this is already recorded above. Resolved by `spec-five-deferred-admission-and-verification-items.md` with the existing create-tenant item above; this carried pointer is not an additional work item.
- The domain host still falls back to `DeferredAgentCommandStatusReader`, so a domain-rejected setup write lacks a live terminal status and remains unverifiable. Pre-existing; carried under DW-7, DW-12, DW-19, DW-24, and DW-25.
- `AgentInspectionStatus.Success = 0` makes a missing status field deserialize as success status with a null setup payload. Pre-existing; this type is deliberately excluded from the Unknown-zero migration and is tracked under the existing enum-compatibility work.
- Six public operation enums still have implicit nonzero ordinals despite numeric legacy reads. A future insertion can reinterpret a shipped numeric value. Pre-existing; the setup-only pin test does not cover them, and the same finding is recorded in the aggregate-and-contracts chunk above.

- source_spec: `spec-5-2-configure-hexa-through-live-eventstore-operations-3.md`
  summary: Exercise the Story 5.2 focused class-presence gates in CI and reject skipped or undiscovered tests in its regression lane.
  evidence: The resumed review confirms CI invokes only PackageFloorOnly plus Story 5.1 broad floors, while verify-story-5.2.ps1 regression calls still omit skip rejection and minimum counts; these inherited gaps precede the normalization follow-up.
  status: done 2026-10-23
  resolution: `spec-five-deferred-build-verification-items.md` CI runs the complete Story 5.2 verifier with SkipBuild after the source/package floors. Each native focused/composition class requires an unambiguous nonzero successful expected-assembly summary; full-project regressions also enforce the existing Debug test floors. All calls reject errors, failures, skips and not-run tests. The full standalone source gate passes 20 named selections and 4,261 owning-project tests.

- source_spec: `spec-5-2-configure-hexa-through-live-eventstore-operations-3.md`
  summary: Define the coupled update path for Dependabot proposals that advance the Builds gitlink.
  evidence: A gitlink-only proposal fails the literal Builds SHA assertions in CI and PackageInventoryTests until workflow and test pins move together; preserve the consistency gate and supply automation or explicit maintainer instructions in the separate dependency-maintenance work.
  status: done 2026-10-23
  resolution: `spec-five-deferred-package-tooling-items.md` documents a single parent commit/PR updating the reviewed Builds gitlink, five reusable workflow refs, CI checkout assertion, release execution SHA and PackageInventoryTests expectations, followed by existing authority, tooling and source/package gates.

- source_spec: `spec-5-2-configure-hexa-through-live-eventstore-operations-3.md`
  summary: Add executable fixture coverage for the focused Story 5.2 verifier's success, skip, unrun, and zero-discovery outcomes.
  evidence: The verification-gap reviewer found only PackageFloorOnly execution and source-text checks; reverting Invoke-TestClasses to its earlier total-only behavior would leave those checks green. This inherited CI/tooling change predates the normalization follow-up.
  status: done 2026-10-23
  resolution: `spec-five-deferred-build-verification-items.md` Production-script subprocess fixtures exercise every class/full-project invocation, successful and missing selections, every full-project below-floor path, full-regression zero/failure/skip/not-run paths, default source restore/build ordering, absent/foreign/duplicate/incomplete/malformed summaries, native diagnostics, missing assemblies, package-floor-only behavior and live progress; matched/unmatched real xUnit selectors additionally verify runner compatibility.

## Deferred from: code review of spec-5-2-configure-hexa-through-live-eventstore-operations-3.md (2026-09-23, re-review)

- `AgentDomainHostComposition.Configure` carries about 180 lines of story-by-story comments moved verbatim from `Program.cs`, and several of them say live command dispatch "stays deferred behind DeferredAgentCommandDispatcher" (for example line 72). That contradicts the Story 5.2 `Agents:EventStore:BaseUrl` block in the same method. Pre-existing; trim or correct them when that composition is next edited.
  status: done 2026-10-23
  resolution: `spec-five-deferred-admission-and-verification-items.md` replaces historical story narratives with current composition conditions: projected tenant/approver adapters with unavailable authority sources, HTTP(S) BaseUrl plus AppId for live dispatch, and gated automatic-interaction ports. Executable registrations and ordering match baseline exactly.

## Deferred from: code review of spec-5-3-govern-provider-models-and-pricing-through-live-operations-2.md (2026-09-24, contracts and domain chunk)

- Pricing-version and initial-capability-version validation in `ProviderCatalogAggregate.ValidatePricing`/`Handle(CreateProviderModelEntry)` has no rejection test (mismatched positive version on update/create, non-migrated `InitialCapabilityVersion != 1`). The UI always sends `PricingVersion: 0`, so no current caller reaches it; add theory rows when this area is next touched.
  status: done 2026-10-23
  resolution: `spec-five-deferred-projection-verification-items.md` adds explicit create/update pricing-version and non-migrated initial-capability rejection cases, full input-state immutability before/after rejection replay, and valid automatic/explicit-version controls while preserving historical migration coverage.
- `ProviderCatalogAggregate` and `TenantProviderEnablementAggregate` trust payload `EffectiveAt`/`DecidedAt`; only `EventStoreProviderCatalogOperations` stamps server time. Unverified (medium if true): settle by confirming whether the external EventStore gateway host (DW-21) exposes generic command submission for `provider-catalog`/`tenant-provider-enablement`, which would let an operator backdate or future-date grace or a tenant administrator falsify decision time.

## Deferred from: code review of spec-5-3-govern-provider-models-and-pricing-through-live-operations-2.md (2026-09-26, group 2 slice 1)

- `AgentsProviderCatalogCoordinationPolicy.ReadIdentity` reads `ProviderId`/`ModelId` from raw JSON properties (exact then camelCase), while `Validate` and the tenant aggregate deserialize case-insensitively. A payload repeating an identity under two casings could validate one platform entry and record a decision for another. Unverified (medium if true): the server builds typed payloads, so this needs a raw EventStore submission; settle together with the raw-gateway residual under DW-21.

## Deferred from: code review of spec-5-3-govern-provider-models-and-pricing-through-live-operations-2.md (2026-09-26, group 2 slice 2)

- Platform-scope provider-catalog queries through EventStore `/query` now require `HttpAgentAdministrationContextProvider` to prove `Agents.PlatformOperator` and a matching `ActorUserId` from the ambient `HttpContext` (`ProviderCatalogQueryHandlerBase.IsProviderAdminAsync`). No `AddAuthentication`/JWT setup exists in `src/`, so in this repository's host the context is `Anonymous`. Unverified (high if true): every platform catalog query would return `NotAuthorized` in production. Settle under DW-21 by confirming the deployed domain-service host authenticates the bearer token EventStore forwards on the Dapr-invoked `/query` call.
- `ProviderSelectionVerdict.HasValidPricing` switched to the ISO 4217 list without a selection-orchestrator test for a non-ISO currency (`ZZZ`, `XTS`) mapping to `Unpriced`. Aggregate and migration validate currency at write time, so only pre-validation data is exposed; add theory rows when the verdict is next touched.
  status: done 2026-10-23
  resolution: `spec-five-deferred-projection-verification-items.md` adds otherwise-valid positive-priced `ZZZ`/`XTS` orchestrator cases that return Unpriced, overwrite forged Valid client evidence, and dispatch the exact trusted rejection verdict and capability version. The existing valid ISO selection control remains intact.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md`
  summary: Packaged SDK transport is missing (B01) remains an earlier Story 5.4 prerequisite before owner qualification.
  evidence: The pinned Client 3.113.0 has no retained-history reader while source mode supplies it. Contracts already contains the older custody seam, so the claim is narrowed to missing retained-stream/reader APIs; packaged release verification remains an earlier prerequisite.
  status: done 2026-10-24
  resolution: `spec-five-deferred-evidence-reconciliation-items.md` — The actual catalog selects Client 3.126.0, which exports RetainedIdentityHistoryReader, IRetainedIdentityHistoryReader and the typed ReadAsync API. dotnet-inspect and a fresh isolated package-only compilation/runtime type probe confirm availability with no source references. This closes the missing-packaged-API defect; authenticated reads, full owner qualification and source/package revision equality remain separate requirements.
  evidence_record: [Evidence reconciliation](deferred-evidence-reconciliation-2026-10-24.md#packaged-retained-history-api-b01)

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md`
  summary: Successor continuity after predecessor expiry (B02) remains an earlier Story 5.4 prerequisite before owner qualification.
  evidence: RetainedHumanActorHistoryFold starts version at zero and rejects a Rebound with version zero. Removing an expired predecessor cannot reproduce a live successor. This previously recorded production blocker requires owner-authenticated continuity evidence; qualification alone is insufficient.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md`
  summary: Client completion expiry (B03) remains an earlier Story 5.4 prerequisite before owner qualification.
  evidence: HttpPartiesIdentityClient.Matches validates recorded interval/custody shape but has no current-clock comparison. A delayed historical response can pass after custody expiry. This is existing client behavior, separate from the new cleanup operation.
  status: done 2026-10-23
  resolution: `spec-five-deferred-identity-verification-items.md` verifies the pinned client's existing completion-clock checks through the maintained root command. All nine delayed-reply cases pass, including valid current/closed historical controls and historical custody expiry at completion; rejected replies return 503. This establishes local behavior, not production qualification.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md`
  summary: Version-one rollout compatibility (B04) remains an earlier Story 5.4 prerequisite before owner qualification.
  evidence: The new field defaults to zero and the client rejects zero, but these identity APIs are documented as proposed/unpublished. No deployed older v1 producer was demonstrated. Medium if such a producer exists; settle with the owner publication/deployment inventory and explicit rollout contract.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md`
  summary: Concrete actor revocation freshness (B05) remains an earlier Story 5.4 prerequisite before owner qualification.
  evidence: PartyIdentityAuthority.Admit re-verifies the same signed proof without consulting the actor registry. SameAuthority cannot see revocation during its lifetime. This is an earlier authority seam/production qualification gap, not introduced by applying the duration.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md`
  summary: Party query deadline (B06) remains an earlier Story 5.4 prerequisite before owner qualification.
  evidence: Current and retained-history custody waits use WaitAsync(cancellationToken) without an operation deadline. An ignoring provider with CancellationToken.None can stall indefinitely. This earlier query boundary needs its own bounded operation contract.
  status: done 2026-10-24
  resolution: `spec-five-deferred-evidence-reconciliation-items.md` — The pinned Parties service already supplies one bounded current/history query budget. The maintained root identity command now requires 54 exact deadline rows, alongside 24 earlier owner cases and 14 receipt cases. Thirty-six controlled negative executions reject missing, substituted or skipped deadline results before a success receipt. Fresh actual owner runs pass; authority responsiveness, worker reclamation, standalone transport and production qualification retain their separate gates.
  evidence_record: [Evidence reconciliation](deferred-evidence-reconciliation-2026-10-24.md#party-query-deadline-b06)

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md`
  summary: Readiness exact immutable expiry (B08, E05, V04) remains an earlier Story 5.4 prerequisite before owner qualification.
  evidence: The readiness script parses retention but checks only a future returned expiry, not ValidFrom + retention or its interval bound. This pre-existing ReadinessOnly verifier can accept a wrong duration; it cannot qualify production. Same missing immutable-expiry comparison as B08. The source opening custody expiry is not compared with returned expiry. The verification reviewer independently confirms B08/E05: a 366-day returned deadline can pass a declared 365-day policy because only future expiry and opening actor/version/scope are checked.
  status: done 2026-10-23
  resolution: `spec-five-deferred-readiness-verifier-items.md` verifies the existing pinned Parties correction through maintained production-script subprocess tests: exact 365-day positive controls, a 366-day opening, returned custody expiry changes including one 100 ns tick, revocation-effective retention and unsupported policy. Refusals assert their diagnostic, request stage and absent success receipt. A temporary mutation removing the original-expiry comparison is detected. This closes the verifier defect, not owner qualification.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md`
  summary: Readiness transition proof (B09) remains an earlier Story 5.4 prerequisite before owner qualification.
  evidence: The verifier compares the opening actor/version/scope but does not reconstruct later revoke/rebind boundaries. An incorrectly open returned interval can pass. This is an earlier readiness verifier defect, not a cleanup/provider qualification result.
  status: done 2026-10-23
  resolution: `spec-five-deferred-readiness-verifier-items.md` exercises the pinned independent transition fold with valid revoked/rebound controls, separate returned intervals ignoring revoke/rebind, a changed later revocation, an independently changed returned opening time, a wrong predecessor and a shortened opening without a closing transition. A temporary mutation removing the closing-interval comparison is detected. Tests establish synthetic verifier behavior only.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md`
  summary: Readiness streaming bound (B10) remains an earlier Story 5.4 prerequisite before owner qualification.
  evidence: Invoke-WebRequest buffers the response before the UTF-8 byte-count check. The 32 MiB receive bound does not prevent oversized allocation; streaming enforcement is an earlier verifier prerequisite.
  status: done 2026-10-23
  resolution: `spec-five-deferred-readiness-verifier-items.md` verifies the pinned bounded HttpClient receive path at exactly 32 MiB and one byte above it for declared/chunked responses, also observing early disconnect before a full 40 MiB stream is sent. Otherwise wire-bounded responses separately exceed the 16 MiB decoded limit through one large event or two cumulatively oversized valid events. Overflow cases fail at the expected diagnostic with no receipt; raising the wire limit in a temporary copy is detected. Redirect refusal is also covered.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md`
  summary: Relative evidence paths (B11, E03) remains an earlier Story 5.4 prerequisite before owner qualification.
  evidence: The wrapper creates a caller-relative EvidenceDirectory then changes directory before resolving build/test outputs. Legal relative arguments can redirect or fail output. Normalize evidence and optional Memories paths before lane execution in the owning verifier slice. Same legal relative-path failure as B11, verified at Push-Location and the build/test Join-Path calls.
  status: done 2026-10-23
  resolution: `spec-five-deferred-readiness-verifier-items.md` runs the pinned wrapper from a foreign process directory and after PowerShell Set-Location. Controlled dotnet handles the current preliminary version query, then stops the first build; assertions verify absolute caller-relative evidence, artifact, Memories and EventStore paths and no later lane or success receipt. A LiveReadiness positive control uses relative register/evidence paths. Removing evidence normalization in a temporary copy is detected; no real owner build is claimed.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md`
  summary: Current read lower time bound (E02) remains an earlier Story 5.4 prerequisite before owner qualification.
  evidence: After custody, the current method checks upper expiry but not completedAt >= binding.ValidFrom or source.ObservedAt <= completedAt. An earlier-issued still-valid grant plus clock rollback can release future current evidence. This pre-existing current-read boundary remains a launch blocker.
  status: done 2026-10-23
  resolution: `spec-five-deferred-identity-verification-items.md` verifies the pinned query service's existing final source/binding lower bounds through the actual current-read method. The maintained root command runs source-rollback and binding-rollback custody-suspension cases with an otherwise valid authority, requiring Unavailable and no evidence, alongside the successful current-human control.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md`
  summary: Readiness nested binding scope (E04) remains an earlier Story 5.4 prerequisite before owner qualification.
  evidence: The readiness response guard checks outer tenant/Party and actor/version but not the nested evidence tenant/Party. Comparing opening source scope to the expected scope does not compare the returned nested scope; another tenant can pass the verifier.
  status: done 2026-10-23
  resolution: `spec-five-deferred-readiness-verifier-items.md` verifies both source-opening and returned nested tenant/Party against the pinned verifier, with independent foreign and case-only scope mutations and otherwise matching actor/version/outer scope. Each fails at its expected request stage without a receipt; removing the returned scope comparison in a temporary copy is detected. Current dependency acceptance records remain unchanged.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md`
  summary: Current authority regression coverage (V01) remains an earlier Story 5.4 prerequisite before owner qualification.
  evidence: Accept the pre-verified gap: authority-change tests call ResolveAtAsync, while current policy/cancellation tests do not observe removal of SameAuthority in ResolveAsync. Add current source/custody authority-change cases in the owning query verification slice.
  status: done 2026-10-23
  resolution: `spec-five-deferred-identity-verification-items.md` makes current-source and current-custody authority-withdrawal cases mandatory and adds root-owned non-null source/actor-revision/authority-revision changes plus unchanged controls. A private mutation removing current SameAuthority fails all six changed-authority cases while both controls pass; owner source stays untouched. Missing required scenarios or incomplete/failed/skipped/unrun results cannot produce a success receipt.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md`
  summary: Current completion-expiry coverage (V02) remains an earlier Story 5.4 prerequisite before owner qualification.
  evidence: Accept the pre-verified gap: the clock-crossing test exercises historical resolution; client invalid replies do not execute the current service. Add current binding-closure and custody-expiry crossing cases with otherwise valid authority.
  status: done 2026-10-23
  resolution: `spec-five-deferred-identity-verification-items.md` runs the existing current-query binding-interval and custody-expiry crossing rows through the maintained root command, asserting Unavailable with absent evidence at the exclusive completion boundary while authority remains valid. All four current completion-time rows and the successful control pass against actual owner sources.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-apply-365-day-lifecycle.md`
  summary: Readiness exact-target receipt coverage (V03) remains an earlier Story 5.4 prerequisite before owner qualification.
  evidence: Accept the pre-verified gap: existing counting-server smoke cases fail before receipt validation. A synthetic Available register with a mismatched receipt needs a no-HTTP regression test before this earlier readiness gate can be relied upon.
  status: done 2026-10-23
  resolution: `spec-five-deferred-identity-verification-items.md` extends the maintained production-script subprocess suite to 14 exact-target receipt cases: matching positive controls and independent record/target/command/outcome/evidence-level/prerequisite mismatches through both history verifier and wrapper. Every mismatch reaches its receipt diagnostic with zero HTTP and no success receipt; the root identity command requires all 14 exact named cases. Synthetic Available fixtures do not alter dependency acceptance.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-identity-completion-and-readiness-fixes.md`
  summary: Owner retained-history privacy and schema-drift qualification must account for unexpected profile fields.
  evidence: Before and after this correction, the readiness parser ignores additional payload members; PartiesJsonOptions itself permits unmapped fields while the SDK closes payloads through typed deserialize/serialize. Complete owner qualification must establish that every authenticated installed source emits only the purpose-separated identity schema; local read probes supply neither that production proof nor erasure certification.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md`
  summary: Adopt the current EventStore SDK sidecar/workload security contract in the Parties host and authenticated process-endpoint fixture, then rerun all 13 endpoint cases without weakening the startup audit.
  evidence: Root final run against the reviewed assembly fails before requests on missing EventStoreSidecarChannel for /dapr/subscribe, /dapr/config and actor routes, plus anonymous /healthz; Parties `tests/owner-continuation-2026-10-07/review-final/endpoint-tests.xml` and endpoint-tests.log record the exact audit. This concurrent SDK/host compatibility blocker is independent of the passing 802-test Local matrix and serialized DomainServiceRequest fallback.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md`
  summary: B1 — Synchronous authority/fold responsiveness remains an owner qualification requirement.
  evidence: Synchronous authority admission and local folding were already synchronous; the deadline guards refuse post-budget evidence but cannot forcibly terminate that work. Existing authority/processing responsiveness remains an owner qualification requirement, separate from the selected source/custody wait correction.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md`
  summary: B4 — Noncooperative provider resource reclamation and bounded abandoned-work admission remain shared runtime/host qualification requirements.
  evidence: Noncooperative synchronous provider invocations can retain workers after timeout. The previous direct invocation also retained a request worker indefinitely. The delivered source provider ports do not establish qualified cancellation/resource reclamation; bounded abandoned-work admission belongs to shared runtime/host qualification, not an invented local capacity policy.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md`
  summary: Noncooperative resource qualification remains part of full owner prerequisites (2026-10-08 review B7).
  evidence: Private Task.Run bounds verdict wait but cannot reclaim indefinitely blocking invocations/callbacks. Prior authority/provider paths already required cooperation. No qualified concurrency/execution-isolation/failure-model contract is supplied; shared host resource admission/reclamation remains required and unqualified.

- source_spec: `_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md`
  summary: Standalone retained transport invocation remains part of full owner prerequisites (2026-10-08 review E2).
  evidence: Pre-existing RetainedIdentityHistoryReader invokes SendAsync/body providers synchronously before bounded waits and shares provider callbacks with wait cancellation. Parties query wraps this provider under its own deadline, but standalone callers remain unqualified. This is additional feasible full-scope source work, to address after current review corrections without inventing resource reclamation.

## Deferred from: code review of spec-5-4-owner-prerequisites.md (2026-10-09, revisions 8+9 cumulative owner slice)

- Spool archive history scan: `FindArchivedAsync`/`ArchivesCompleteAsync` read and fully revalidate every linked page (up to 16 × 32 MiB) on non-duplicate observe, lookup misses and readiness, all under one 30 s budget. Unverified (medium if true): ordinary operations may time out well before the 16-page ceiling. Settle by measuring 16 full pages on the selected Dapr state backend; revision 9 explicitly leaves throughput unqualified and forbids a new index now.
- R9-B8 carry: an already persisted acknowledged receipt whose `EventId` exceeds the new 2,048-byte strict UTF-8 bound would make `Capture` reject its head or archive page, so every spool operation would fail. Unverified (medium if true): settle with an actual retained pre-revision-9 receipt or an accepted migration contract.
- R8-B7 carry: a stored violation with the new `AcceptanceReceiptId` defaulted to empty makes `RecordViolation` deny every later violation for that deletion. Unverified (medium if true): settle with actual persisted legacy violation state or an accepted migration contract.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-update-eventstore-to-3-119-0.md`
  summary: Verify the EventStore gateway test in Debug source-reference mode after the Conversations.Client analyzer blocker is resolved.
  evidence: The Debug server test project build stops on nine pre-existing CA1062 errors in `references/Hexalith.Conversations/src/Hexalith.Conversations.Client`, before the gateway test can run. A passing source-reference build and test run would settle whether the local EventStore checkout remains compatible.
  status: done 2026-10-23
  resolution: `spec-five-deferred-package-integrity-items.md` performs a fresh isolated Debug build with all six dependency FromSource properties true and zero warnings/errors; all 141 exact AgentsEventStoreGatewayIntegrationTests cases pass with zero failures/skips/not-run/errors. The old Conversations analyzer blocker does not reproduce. CONTRIBUTING documents the focused fresh-artifact command; gateway-verification.json retains canonical source revisions, commands and XML hashes. This establishes current pinned source compatibility, not live topology qualification.

- source_spec: `/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md`
  summary: Rehashing a revision-10 spool head or archive page may fail its stored digest after the reservation fields were added.
  evidence: `ReservedArchiveDigest` and `ArchiveReservationCount` serialize under default options, so a snapshot written before those fields would get a new `StateDigest` and could fail anchor or archive-link checks. No retained pre-change head or page was shown, and divergent digests already fail closed. An actual retained revision-10 head or archive page would settle whether this is a medium compatibility break.

- source_spec: none
  summary: Story 6.1 goal B — legacy InteractionDirectoryMigration Workflow (EventStore migration-epoch write fence, directory-repair fence, old-epoch bridge, two-scan DirectoryReady, legacy-plaintext negative fixture).
  evidence: Split from Story 6.1 on 2026-10-19 by user choice; it is independently shippable and depends on EXT-HOST-1 EventStore namespace ports that do not exist yet, while the interaction core (goal A) stays fail-closed on DirectoryReady until it lands.

- source_spec: `_bmad-output/implementation-artifacts/spec-6-1-start-and-recover-an-automatic-interaction.md`
  summary: Story 6.1 User proposal action-intent outbox on `ConversationAgentState` (protected User approve/edit intents, ProposalMutation reservation, API-crash recovery, Closing races) with UserProposalActionIntentOutboxRecoveryTests and UserActionIntentImmediatelyBeforeAndAfterCommitClosingAndApiCrashTests.
  evidence: Split at the 2026-10-20 spec-size gate (spec ~3,400 tokens) by user choice; it serves Confirmation-mode human actions consumed by Epic 7 (Stories 7.1-7.4), not the automatic path.

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-release-verification-items.md`
  summary: Add subprocess coverage for the existing live gh API branch of release-source verification.
  evidence: The source CLI regression tests use the existing JSON fixture flags and exercise the production proof call; both release callers use the unchanged live gh branch. A controlled gh executable on PATH could additionally prove endpoint construction, subprocess-failure diagnostics and malformed API-response handling without real GitHub requests. This pre-existing transport coverage gap is separate from the five selected deferrals; no incorrect live behavior was demonstrated.
  status: done 2026-10-23
  resolution: `spec-five-deferred-ci-release-followups.md` exercises the production live branch with a controlled gh executable on PATH, asserting exact API arguments, successful proof, stale/failed CI, failures and malformed JSON at either endpoint, and a missing CLI without making GitHub requests.

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-projection-verification-items.md`
  summary: Define structural event-payload validation for successfully deserialized Agent projection events while preserving supported legacy defaults.
  evidence: Existing `AgentState.Apply(AgentResponseModeConfigured)` accepts default mode/configuration-version values from `{}`, while `Apply(AgentApproverPolicyConfigured)` dereferences a null Policy and lifecycle Apply rejects negative configuration versions. These paths predate the syntax/null/shape delivery correction; deserialization alone cannot enforce their semantic invariants. Agree the required-member/legacy compatibility policy and validate before folding; application failures already occur before any read-model batch is written.

- source_spec: `_bmad-output/implementation-artifacts/spec-five-deferred-package-integrity-items.md`
  summary: Qualify the actual consumer analyzer and buildTransitive inventory separately from ordinary dependency Build exclusions.
  evidence: The DW-22 offline restore proof demonstrates that nuspec Build,Analyzers exclusions block ordinary build imports but do not suppress transitive analyzer items; buildTransitive has separate semantics. No unexpected analyzer in an actual Agents consumer was demonstrated. Unverified medium: an unexpected executable asset would require a consumer graph/asset inventory and owner-approved policy, rather than treating nuspec exclusion flags as proof of suppression. The demonstrated ordinary-build omission gap is fixed and covered.
