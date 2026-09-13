---
name: Hexalith Agents adversarial-divergence review v13
type: architecture-review
lens: adversarial-divergence
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
verdict: fail
critical: 1
high: 3
medium: 0
low: 0
lint_ok: true
---

# Adversarial Divergence Review — v13

## Verdict

**FAIL — 1 Critical and 3 High target-architecture divergences remain.** The revised spine closes the previously reported same-owner reservation/commit race, initial migration bootstrap/principal cycle, Conversation-deletion candidate/fence race, manifest-bound Budget/capacity authority, first-catalog digest consumption, and accepted-late-write prevention as those findings were stated. A fresh independent-implementation pass found one safety/deletion-cut contradiction in the normative sequences and three High handoff gaps: the generated-failure branch has no coherent concrete variant, operator-origin deletion cannot reach its candidate/refreeze variants under the closed principal allowlist, and migration repair does not define the fate of already-issued epoch capabilities.

The deterministic architecture linter passed with `ok: true` and zero findings. Mechanical lint does not close the semantic findings.

## Frozen Snapshot And Method

The following inputs were SHA-256 checked before the review and again after this report was written:

- `ARCHITECTURE-SPINE.md`: `76adbcfe3dd7f07cc836f5c55b31d7b40945078171fcd53c9e30fb3929969efb`
- `IMPLEMENTATION-CONVENTIONS.md`: `b619ea870627e299dbec0e82ecefde1b3c182bc0a27bb5a5c04653ab091f3b34`
- architecture `.memlog.md`: `8128e951ca03c455af19965070932bffd37153c243d0afc387b64b76b43ca07d`
- bound `prd.md`: `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb`
- `epics.md`: `ddbf1fe179ee3dd49ece7cad6fa3f5f34814f21d6669c64d9a92458954beebe9`
- `external-dependency-register.md`: `b1d839b2a55289676e8bf09b737d135fb9c02f610ddbc11131bb10294d91ea27`
- `launch-readiness-register.md`: `ec6eb4cf624acb88c92a919bca594de3a8f7ffc7b8b77c76f65cde7fe3e17c7e`
- authoritative `VALIDATION-REPORT-2026-09-12.md`: `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`

The review independently constructed directory, interaction-workflow, proposal, Provider, Conversations, migration, EventStore-guard, governance-workflow, protection-fence, decision-recorder, and recovery units. Each unit was made to obey a literal local clause; their combined behavior was then stressed at expected-revision, crash, lost-acknowledgement, replay, restore, cross-tenant, policy-successor, Closing, and irreversible-effect boundaries. The cited v13/v5 reviewer outputs were treated as concurrent anticipated deliverables, not missing-source defects.

Repository reality was inspected read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`; no submodule was initialized, updated, or mutated. Root-authoritative gitlinks remain Builds `a32cb422749352cce8dec948aa3e78c8f00eb4cf`, Conversations `73bcee6f04479d4743d5a65ce929728e22687d7d`, EventStore `ce9e779a3ec24d9b9a054afd0ceaa3c05b8adaaa`, FrontComposer `053b2008307d4e476c0d4329e6c47763c301d43e`, Memories `3644ef63da87b8c7ec9e17726fae7f534e642a1c`, Parties `fa42398552fba1c80eb2760791517659d6d1313a`, and Tenants `2fac18396ff11a4459de053b3ebb7ddfe7c13e30`. The checked-out Builds, Conversations, EventStore, FrontComposer, and Memories revisions differ from those gitlinks; this dirty parent state is not architecture authority.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 3 |
| Medium | 0 |
| Low | 0 |

## Critical

### C-v13-1 — The normative posting and approval sequences perform protected dependency reads before acquiring the lease that is supposed to fence those reads

**Classification:** target architecture safety/deletion-cut defect.

**Evidence.** The same-owner rule says a `Reserved` lease authorizes nothing, only `CommittedToEffect` may authorize the bound dependency read/effect, and Closing can cancel a reservation while Effective waits only for manifested committed work (`ARCHITECTURE-SPINE.md:192,232`; `IMPLEMENTATION-CONVENTIONS.md:19`). AD-7 correspondingly says pre-post membership and related checks run inside the committed `ConversationPosting` lease (`ARCHITECTURE-SPINE.md:242`). The concrete sequence reverses that order in both posting branches: it performs current safety/lifecycle/kill-switch/Party checks and a Conversations existence/access/membership read, and only afterwards reserves and commits `ConversationPosting` (`ARCHITECTURE-SPINE.md:656-660,672-676`). The approval branch likewise performs the exact-version safety check before reserving/committing its `ProposalMutation` lease (`ARCHITECTURE-SPINE.md:666-669`). The matrix cannot resolve the contradiction: generic commit requires `EveryLastMutableOperationCheckExact`, while `ConversationPosting:BeginUnderEffectLease` also requires `FullPrePostValidationExact` after a committed lease exists (`launch-readiness-register.md:242,249`).

**Two literal units.** Team A follows the diagram and calls safety/Conversations before a lease exists. Closing can then append and reach Effective because that in-flight work is absent from its finite manifest; A's content-bearing safety evaluation or Conversation read may begin after the effective deletion cut even though its later lease acquisition correctly rejects. Team B follows AD-6/AD-7 and first commits the lease, then performs the same reads; Closing manifests and waits for its exact result and settlement. Team C treats `EveryLastMutableOperationCheckExact` as requiring those dependency results before commit and concludes the operation is circular, because Reserved authorizes no such read. All are supported by a literal clause, but only B preserves the intended cut.

**Impact.** Sensitive proposal/Conversation content can be read or disclosed to the safety dependency after the architecture claims that no content-producing read/effect can begin. This reopens the irreversible deletion-safety guarantee even though the reservation-versus-Closing state transition itself is now correct.

**Required correction — architecture mechanics, not a Product decision.** Define one closed lease ordering for each target operation. For protected/mutable target validation, distinguish safe pre-reservation metadata checks from the target dependency reads: reserve, conditionally commit at the directory, then perform the bound safety/roster/Conversation reads and either append the exact negative result or `BeginPosting`/proposal mutation, finally settle. Reorder both diagram branches and approval, reconcile `EveryLastMutableOperationCheckExact`, AD-6, AD-7, the matrix, conventions, and failure-injection fixtures. No protected dependency read may occur on an unmanifested worker after Closing/Effective.

## High

### H-v13-1 — The generated-failure alternative is impossible under its only concrete matrix row

**Classification:** target architecture operation-variant and failure-recovery defect.

**Evidence.** AD-13 and the diagram put `RecordGeneratedVersion` and `GenerationFailed` behind one generated-output `ProposalMutation` lease (`ARCHITECTURE-SPINE.md:320,646-651`). Its sole concrete row requires `OutputSafetyAllowedAtExactVersion` and `ProposalVersionIdSealedEnvelopeKindAndExpectedInteractionRevisionMatch`, then says it may append either success or `GenerationFailed` (`launch-readiness-register.md:247`). The PRD requires Provider, timeout, and safety failures to be durably visible while creating no Proposed Agent Reply; retained failed content belongs only to a separate non-approvable failure record (`prd.md:299-300,320-328`). A safety failure cannot satisfy `OutputSafetyAllowed`; an ordinary Provider failure has no generated output to scan; neither failure may invent a `ProposalVersionId`. No other v4 concrete variant records the ordinary Provider/safety failure outcome after invocation.

Current code illustrates the already-declared implementation debt rather than closing the target gap: `AgentOutputGenerationPolicy` maps success to a version but every failure to `AgentOutputGenerationFailed` with no version (`src/Hexalith.Agents/AgentInteraction/AgentOutputGenerationPolicy.cs:17-20,25-38,52-73`), and the event explicitly represents both `GenerationFailed` and `SafetyFailed` without persisted failed content (`src/Hexalith.Agents.Contracts/AgentInteraction/Events/AgentOutputGenerationFailed.cs:3-25`).

**Two literal units.** A strict matrix unit cannot append either failure and leaves the Provider lease/Budget/capacity/open-interaction recovery unfinished. A second unit fabricates a proposal-version identity to use the listed row. A third records the failure under `ProviderInvocation:AuthorizeUnderEffectLease`, interpreting its broad effect/result sentence as an implicit outcome variant despite AD-17's absent-variant failure rule. They disagree on state, identity, retention, and deletion inventory.

**Required correction.** Split success from failure or define a closed discriminated row: allowed output requires the sealed `ProposalVersionId`; Provider/timeout failure and output-safety failure require their exact negative evidence, a deterministic non-proposal failure-record identity/location, and structurally absent proposal/version fields. State whether the failure event is authorized by the committed Provider lease or a separately committed failure-record mutation lease, and bind settlement, lost acknowledgement, directory membership, protection key, and deletion inventory for that choice. This selects no unresolved Product outcome.

### H-v13-2 — Operator-origin deletion candidate acceptance/refreeze has no authorized principal

**Classification:** target architecture authorization and deletion-progress defect.

**Evidence.** Operator deletion requires a reversible candidate, fence-owned acceptance, acknowledgement, and candidate supersession/refreeze after a competing hold/export/copy revision (`ARCHITECTURE-SPINE.md:400-402`; `epics.md:2956-2964`; `launch-readiness-register.md:273,275-276`). AD-30 permits `Platform` only the initial `DeletionRequest` requester operation and rejects it for every unnamed family. Its generic `GovernanceProtection` Workflow list allows prepare/pin/unpin, result/destruction/purge, delivery, and fence-acknowledgement operations only after a frozen resource set has been verified; it also says that workflow cannot change scope or add a resource (`ARCHITECTURE-SPINE.md:470`). The later correction grants candidate freeze, accept, acknowledgement, and refreeze authority only to `ConversationDeletionPropagation`, not to the ordinary operator-origin `GovernanceProtection` workflow (`ARCHITECTURE-SPINE.md:474`). Yet the matrix names `GovernanceProtection:BuildDeletionInventoryCandidate`, `GovernanceProtection:AcceptDeletionInventory`, and `GovernanceProtection:AcknowledgeAcceptedDeletionInventory` and requires refreeze to include a newly discovered competing resource/revision.

**Two literal units.** A strict principal verifier rejects the operator workflow before candidate construction/acceptance because no frozen set exists and the exact variants are absent from its allowlist. Another team stretches “prepare” to include candidate construction and treats discovery of a newly committed export as not adding a resource. A third redispatches the variant as the original Platform requester, contradicting the closed Platform family list. The strict implementation permanently blocks every operator-origin deletion before preparation; the permissive variants implement different authority boundaries.

**Required correction.** Give the ordinary target-limited `GovernanceProtection` workflow an explicit operator-origin allowlist for build/supersede candidate, fence accept, accepted-set acknowledgement, and hold deferral, each bound to the already-recorded requester/Inspector approval, stable request id, authoritative scope grammar, target tenant, candidate ordinal, current fence revisions, and expected owner revision. Clarify that refreeze may add only newly discovered resources already inside the immutable human-approved scope, never expand that scope. Alternatively keep the initial human command as owner, but then name that principal and its exact concrete variants consistently. No new deletion policy is needed.

### H-v13-3 — Migration repair leaves old-epoch creation outboxes and committed leases with incompatible but still locally compliant fates

**Classification:** target architecture recovery/availability defect.

**Evidence.** Every post-cutover create or content-bearing append needs the *current* migration epoch plus its exact permit or committed-effect capability (`ARCHITECTURE-SPINE.md:194`; `launch-readiness-register.md:236,240-242`; `external-dependency-register.md:129`). An accepted late legacy append records `DirectoryInvalidated` and requires fence repair plus complete reconciliation (`launch-readiness-register.md:239`). AD-29 derives `MigrationWriteEpochId` with a `FenceOrdinal`, permitting an epoch successor, and AD-7 calls Begin a bootstrap/repair variant (`ARCHITECTURE-SPINE.md:248,462`). However, the repair path never freezes or resolves the active directory cohort before installing a successor guard: no row says whether pending creation/workflow-start outboxes and `CommittedToEffect` leases retain the old epoch until settlement, are rebound to the successor epoch, or must drain before rotation. The Begin row freezes only the legacy-stream checkpoint/count/hash/high-water and says writers are quiesced; it does not define the directory permit/outbox/lease inventory or its recovery (`launch-readiness-register.md:235-239`).

**Two literal units.** Team A rotates `FenceOrdinal` immediately and correctly rejects every old capability; already-durable creation outboxes and committed local target appends then cannot complete, while committed leases may never settle. Team B reinstalls/repairs the guard without changing the epoch, preserving those capabilities. Team C drains every directory member before rotating, interpreting “call writers quiesced” as a fixed-point drain although no owner/checkpoint is named. These implementations disagree after the very integrity incident whose recovery must be deterministic.

**Required correction.** Add an exact repair variant and state the epoch transition. Before a successor epoch becomes current, freeze the directory permit/outbox/lease high-waters and either (a) drain every old-epoch committed/pending member under a recorded bridge that cannot authorize new work, or (b) conditionally rebind each exact still-valid capability with old/new epoch and owner revisions. Reserved leases may not be promoted across repair without winning the ordinary current-owner commit; committed leases may not be cancelled. Ready requires the complete legacy reconciliation plus complete accounting of this repair cohort. Add crash/lost-ack/restore fixtures around old-epoch creation, workflow-start, membership, generated-output, and posting steps.

## v12 Critical/High Recheck

| v12 finding | v13 disposition |
| --- | --- |
| C-v12-1 Active lease revocation race | **Closed as stated.** `Reserved`, same-owner `CommittedToEffect`, Closing cancellation, non-revocable must-settle work, and exact settlement are explicit. C-v13-1 is the distinct ordering contradiction for dependency reads performed before reservation/commit. |
| C-v12-2 migration circularity/principal | **Closed for initial bootstrap.** Platform start, the target-limited Workflow kind, omitted circular gate, direct prerequisites, EventStore guard, and backfill/complete variants exist. H-v13-3 concerns repair of capabilities already issued after Ready. |
| H-v12-1 deletion candidate/fence race | **Closed for Conversation-origin mechanics.** Candidate is reversible, fence acceptance owns the immutable set, revision conflict refreezes under the same id, and acknowledgement is recoverable. H-v13-2 is the separate missing operator-origin principal grant. |
| H-v12-2 deletion-workflow Budget/capacity authority | **Closed.** Exact manifest/outcome-bound settle/release variants and least-privilege principal grants now exist. |
| H-v12-3 generated-output lease coverage | **Closed for successful output only.** A separate ProposalMutation commit precedes persistence and Closing discards the response if it wins. H-v13-1 is the newly exposed failure-arm contradiction. |
| H-v12-4 bootstrap catalog digest | **Closed.** First `PublishCatalog` must equal and consume the exact initial version/digest committed by bootstrap. |
| H-v12-5 late legacy writes | **Closed for prevention and invalidation.** The EventStore boundary now rejects stale/disconnected/restored writers and accepted writes invalidate readiness. H-v13-3 is the unspecified recovery of already-issued current-path capabilities after invalidation. |

## Authoritative Validation Recheck

The authoritative report's C-1 through C-3 and H-1 through H-12 were independently rechecked. Their original issues remain closed as stated: safety is conjunctive across snapshot/current versions; hold/export/deletion share `ProtectionFence`; matrix-v4 has target-aware bootstrap; periodic human Approver resolution and durable rescan ownership are explicit; stable human actor identity is carried by every human principal; rolling-rate, open-interaction, and monetary-budget owners/lifetimes are split; proposal indexing uses an outbox/source high-water and reconciliation; optional retraction is split from core Conversations; trusted envelopes bind canonical bytes, replay, key rotation/revocation, and a non-recursive durable security recorder; current Dapr Client/ASP.NET exposure is reported separately from future Workflow; public shapes are required-completion targets rather than shipped claims; and tracking/dependency debt is not treated as implementation evidence. C-v13-1 and H-v13-1..3 arise from later protocol layers and do not reinstate those findings verbatim.

## Areas That Converge

- FR-8 retains the Product-fixed step order: rate/open step 5, Confirmation-only Approver step 6, Context step 7, Budget step 8, safety step 9, leased roster/join step 10, then one acceptance decision. Membership commit versus Closing is same-owner and fail-closed.
- Conversation deletion intake, Closing manifests, manifest-bound Provider/Budget/capacity/posting settlement, Effective activation, reversible candidate, fence-owned acceptance, acknowledgement, source-origin authority, and recorded destruction recovery have explicit identities and no projection-scan fallback.
- The decision catalog has a narrow recorder-scope bootstrap, exact first-catalog commitment, pending/effective separation, activation manifest, and predecessor-authorized successors.
- Export preparation pins lifecycle/store authority; fence commit, secondary acknowledgement, principal-bound key delivery, cleanup, hold coupling, and deletion completion remain one-way and recoverable.
- Replay admission precedes domain idempotency; cross-tenant routing, registrar/recorder ACLs, emergency key revocation, and lost-ack recovery remain closed.
- The current open decisions remain explicit and fail closed in their affected scope. This review does not choose `OD-RATE-CONCURRENCY-CONSUMPTION-1`, `OD-HOLD-DELETION-PRECEDENCE-1`, `OD-HOLD-PREPARE-CANCELLATION-1`, `OD-EXPORT-LIFECYCLE-1`, `OD-RELEASE-RECORDER-SCOPE-1`, `OD-LEGACY-PLAINTEXT-DISPOSITION-1`, or any bound PRD Product outcome.

## Architecture Defects Versus Implementation And Tracking Debt

C-v13-1 and H-v13-1..3 are target-architecture/handoff defects; their corrections choose mechanics and authority already implied by approved requirements, not an unresolved Product outcome.

The repository is materially behind the target and the spine correctly labels that state as delivery debt. There is no shipped directory migration/write fence, permit/create/start outbox, same-owner effect lease, Conversation-deletion barrier/workflow, protection fence, three-ledger protocol, decision catalog/recorder, safety epoch, trusted replay/security recorder, or target public vocabulary. Current generation failure is an `AgentInteraction` event with no separate proposal version, and current generation/version identities are legacy shapes; these facts support H-v13-1's need for an explicit target migration but do not authorize weakening the target. The current Conversations and EventStore checkouts are non-authoritative relative to the root gitlinks; their extra seams cannot be claimed as committed dependencies.

The root-authoritative Builds catalog pins the Dapr package family at `1.18.5`; the checked-out but parent-modified Builds revision pins `1.18.7`. Agents consumes EventStore Client/DomainService and therefore already has transitive Dapr Client/ASP.NET exposure, while no Agents project references Dapr Workflow. The spine reports that accurately as `ARCH-A-15`/dependency debt, not as a new architecture finding. `EXT-HOST-1`, `EXT-CONV-AI-1`, `EXT-PARTIES-1`, `EXT-SECRETS-1`, `EXT-PROTECTION-1`, Provider/safety/export targets, and their live evidence remain Uncommitted/TBD blockers. No build, integration, deployment, or release success is claimed.

## Required Closure Order

1. Reconcile lease ordering and reorder every protected pre-post/approval validation so no unmanifested dependency read can cross Closing/Effective (C-v13-1).
2. Split or close the generated success/failure operation variants and bind the failure record's identity, owner, protection, directory membership, and settlement (H-v13-1).
3. Grant the operator-origin governance workflow exact candidate/refreeze/fence/acknowledgement authority without broadening human-approved scope (H-v13-2).
4. Define the migration repair epoch transition and accounting of all old-epoch permits, outboxes, and committed leases (H-v13-3).
5. Re-distill and rerun deterministic lint plus the complete reviewer gate.

## Gate Result

The v13 adversarial-divergence gate is **FAIL** because Critical and High findings are nonzero. Deterministic lint: **PASS**, `ok: true`, zero findings.
