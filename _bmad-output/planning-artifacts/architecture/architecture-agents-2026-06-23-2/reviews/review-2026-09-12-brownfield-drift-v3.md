---
name: Hexalith Agents brownfield-drift review v3
type: architecture-spine-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
intent: frozen-read-only-specialist-review
reviewer: brownfield-reality drift lens
verdict: fail
critical: 1
high: 4
medium: 3
low: 1
lint_ok: true
---

# Brownfield-Reality Drift Review — 2026-09-12 v3

## Verdict

**FAIL — 1 Critical, 4 High, 3 Medium, 1 Low.** The revised artifacts now distinguish most target architecture from delivery debt accurately, and all original Critical/High findings in the authoritative validation report are either closed or explicitly retained as non-architectural debt. The current handoff nevertheless cannot pass: the Conversation-deletion barrier is not linearized with effects from already-permitted interactions; the acceptance order contradicts the new directory-first protocol; the EventStore protection commitment does not prove that a directory-stream outbox shares the target interaction DEK; export-key delivery lacks a durable effect/recovery seam; and the architecture-assumption index identifies two different versions.

PASS requires zero Critical and zero High.

## Scope And Method

This review applied the BMad Architecture brownfield-drift lens to the complete frozen architecture spine, implementation conventions, authoritative validation report, bound PRD, active epics, external-dependency register, launch-readiness register, current architecture memlog, root repository, and root-declared submodule state. It traced the directory/permit/create/start/acknowledgement path, Conversations deletion delivery and acknowledgement, barrier and deletion-set convergence, workflow principals, operation-matrix variants, common protection fence, export lifecycle and key release, decision-recorder/bootstrap state, current public contracts, and current external adapters.

Repository inspection was read-only. No submodule was initialized, updated, or mutated. Root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2` remains the authority. The initialized Builds, Conversations, EventStore, FrontComposer, and Memories worktrees are internally clean but checked out at commits different from their parent gitlinks; Parties and Tenants match their parent gitlinks. The differing worktree commits were used only to identify local drift, never as architecture authority.

## Frozen Inputs

The requested report was absent at review start, as expected. The following SHA-256 values were captured before analysis and verified unchanged after this report was created:

| Frozen input | SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `8d122b1b8318edc92a40d291e4e9adf32bf22b125a16f73d594435818c64c026` |
| `IMPLEMENTATION-CONVENTIONS.md` | `c7464254b343cfd0caffcbbefe9224e1c518a3affe97e886f0fa1fcacb41dbc9` |
| bound `prd.md` | `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb` |
| `epics.md` | `3f5a9afc605a9352a5c43e505cce9c56fd347ea2c6f476ddf77be3c4729989ec` |
| `external-dependency-register.md` | `577a102a86b4b55af7b971789087594c1008d3231f45509c0ac690921474af63` |
| `launch-readiness-register.md` | `a864a07bbb31a627b24b2ae593cb3f84f404b5bde63b878f635d327d42107420` |
| authoritative `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

## Deterministic Linter

Command:

```text
python3 .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: **PASS** — `ok: true`, `total_findings: 0`, no severity entries. The linter does not currently compare the numeric frontmatter assumption-index version with its rendered body identifier, so it does not close H-BD3-1.

## Critical

### C-BD3-1 — The Conversation-deletion barrier does not linearize effects from already-permitted interactions

**Classification:** target architecture defect; not current implementation debt and not an unresolved Product choice.

**Evidence**

- The new directory protocol safely serializes only `RegisterInteractionPermit` against `InstallBarrier` on one `ConversationAgentState` revision. A permit that wins first has a durable creation outbox; a barrier that wins first rejects a later permit (`ARCHITECTURE-SPINE.md:221,233`; `launch-readiness-register.md:231,245`).
- Already-permitted interactions are different streams. AD-6 says invocation/result recording, regeneration, edit, approval, retry, and `BeginPosting` merely re-read the barrier before their later content append or external effect (`ARCHITECTURE-SPINE.md:221`). Their ordinary matrix variants do not acquire or consume a Conversation-owner revision/lease; only deletion's later `ConvergeInteraction` checks `BarrierStillEffective` (`launch-readiness-register.md:178-188,246`).
- The implementation convention promises that no post-barrier interaction, workflow, or Provider effect starts, but supplies no cross-owner compare/lease primitive capable of making the read and later effect atomic (`IMPLEMENTATION-CONVENTIONS.md:9,15,32`). Story 8.3 repeats the same re-read and no-late-effect claim (`epics.md:2961-2965`).
- Current EventStore protection and aggregate dispatch are per aggregate identity/revision. No current code or root-declared dependency supplies a cross-stream transaction that could silently satisfy the missing cutover.

**Failure scenario**

An existing interaction reads `ConversationAgentState` revision R and sees no barrier. Deletion then appends the barrier at R+1. The interaction can still append `ProviderInvocationAuthorized` at its independent `AgentInteraction` revision and invoke the Provider, or deliver an already-recorded workflow-start outbox, because neither operation conditionally consumes R. The deletion workflow may later discard returned content and erase Agents-owned copies, but it cannot undo Provider processing or disclosure that began after the claimed cutover.

**Required correction — AUTOFIX architecture protocol.** Define one linearizable cutover for every content-producing/external-effect transition of a permitted interaction. A Conversation-owned effect permit/lease that contends with barrier installation, or an equivalent per-interaction stop fence acknowledged into the barrier manifest, would be sufficient if its owner, acquisition/consumption order, expiry/recovery, queued create/start-outbox disposition, and first/last failure-injection cases are explicit. Add the exact owner revision/lease to the relevant operation-matrix variants. This selects no Product policy.

## High

### H-BD3-1 — The authoritative Architecture Assumption Index has two current versions

**Classification:** architecture evidence/governance defect.

The frontmatter declares `architecture_assumption_index_version: 9` (`ARCHITECTURE-SPINE.md:11`), while the normative body declares `ARCH-A-INDEX-8` and requires every `RQ-1` evaluation/blocker to name that older identifier (`ARCHITECTURE-SPINE.md:1095-1097`). The same paragraph says these values must increment together. The PRD delegates evaluation to the spine's authoritative version, so one recorder can persist version 9 while another literal evaluator records INDEX-8. That makes `UnretiredAssumption` evidence non-comparable and can invalidate decision activation or readiness replay.

**Required correction — AUTOFIX.** Reconcile the frontmatter, rendered identifier, and every body reference to one version in the same change. Do not change any assumption outcome or invent a retirement date.

### H-BD3-2 — AD-7 and AD-13 prescribe incompatible call-acceptance order around the new permit

**Classification:** target architecture contradiction.

AD-7 says the interaction permit is appended after pre-creation authorization/readiness/Conversation membership/protection, before an `AgentInteraction` exists, and that rate, open, budget, and capacity decisions occur later on the interaction (`ARCHITECTURE-SPINE.md:233`). The operation matrix and Story 6.1 agree (`launch-readiness-register.md:231`; `epics.md:1884-1888`). AD-13 still states the FR-8 acceptance order as Provider eligibility, rolling-rate/open admission, Approver resolution, context, cost reservation, safety, and only then membership, ending in `AgentCallAccepted` (`ARCHITECTURE-SPINE.md:301`).

Two literal teams therefore place membership and rate/budget reservation on opposite sides of the directory permit. In a deletion race, one team has a manifest member before rate/budget work; another can consume or prepare ledger capacity before attempting a permit that the barrier rejects. The latter has no single specified terminal pre-acceptance owner/disposition for the already-prepared work, and audit/status ordering differs.

**Required correction — AUTOFIX.** Redistill AD-13 to the directory-first state machine and one exact acceptance sequence. Name which checks are pre-permit, which are repeated after interaction creation, when ledger preparations may begin, and the authoritative abort/recovery outcome when a post-permit acceptance step fails. Reconcile the sequence diagram and Story 6.1 without selecting the still-open rate/concurrency consumption outcome.

### H-BD3-3 — `EXT-PROTECTION-1` can become acceptable without proving the cross-stream shared-DEK behavior the directory outbox requires

**Classification:** architecture/external-contract defect exposed by current repository reality.

- AD-2 and AD-7 require a `ProtectedContent` creation-outbox field on the `ConversationAgentState` stream to be sealed under the target `AgentInteraction` DEK, with later DEK destruction yielding typed `Erased` in both streams (`ARCHITECTURE-SPINE.md:183,233`). Story 6.1 owns that exact behavior (`epics.md:1884-1888,1914-1920`).
- `EXT-PROTECTION-1` requires one DEK per interaction and generic field-level protection, but its minimum compatibility contract proves only seal/unseal/erase, hold pin, engine identity, tenant isolation, and restore behavior. It never requires a foreign-owner event to select the target interaction DEK or proves dual-stream `Erased` replay (`external-dependency-register.md:227-239`).
- The parent-authoritative EventStore contract invokes `ProtectEventPayloadAsync` with the aggregate identity **owning** the payload (`references/Hexalith.EventStore/src/Hexalith.EventStore.Contracts/Security/IEventPayloadProtectionService.cs:10-27`). Persisted metadata may contain a `KeyAlias`, but the current interface does not itself bind how a directory-stream event selects an interaction-owned alias (`EventStorePayloadProtectionMetadata.cs:5-23`). The shipped default remains pass-through/no-op (`references/Hexalith.EventStore/src/Hexalith.EventStore.Server/Events/NoOpEventPayloadProtectionService.cs:7-30`).

An EventStore implementation that keys solely by the owning `ConversationAgentState` aggregate can satisfy the current minimum command yet violate the interaction-erasure invariant; another implementation may inspect the event payload and choose the embedded interaction alias. Both fit the current dependency wording.

**Required correction — AUTOFIX the dependency contract.** Add a provider-neutral target-DEK/key-alias contract for the directory creation outbox and require the `EXT-PROTECTION-1` verification command to prove plaintext absence plus identical typed `Erased` replay from both the source directory stream and target interaction stream after one interaction-DEK destruction. Include cross-tenant alias substitution, changed-payload, lost-acknowledgement, snapshot/cache, and restore-negative tests. The record correctly remains `Uncommitted`; no engine/provider choice is implied.

### H-BD3-4 — Export-key delivery is an external effect with no durable authorization/result or lost-acknowledgement seam

**Classification:** target architecture and external-contract defect.

AD-22 correctly makes the `ProtectionFence` the sole export-commit owner and makes the secondary `AuditExport` acknowledgement gate key delivery. It then says only that the custodian delivers the key (`ARCHITECTURE-SPINE.md:381`). Story 8.2 likewise jumps from an approved, unexpired request to direct `EXT-SECRETS-1` delivery (`epics.md:2881-2889`). The operation matrix exposes only a generic `ExportDownload` family and ends `ExportCommitRecovery` by enabling delivery; it has no authorize/effect/result/recovery variants (`launch-readiness-register.md:191,242-243`). `EXT-SECRETS-1` specifies generic secret custody and key operations but no principal-bound idempotent delivery identity or exact delivery-outcome lookup (`external-dependency-register.md:181-193`). This contradicts the implementation convention's durable authorize/effect/result rule for effects (`IMPLEMENTATION-CONVENTIONS.md:9,15`).

After the custodian releases a key and the response is lost, implementations may redeliver, report failure, or infer success. A retry crossing expiry, actor revocation, or policy change has no common outcome, and Agents cannot produce authoritative audit evidence that the concentrated export became decryptable.

**Required correction — AUTOFIX.** Add a durable `ExportKeyDeliveryAuthorized` decision (or equivalent) with deterministic delivery id, exact export-commit acknowledgement revision, requester `AuthenticatedHumanActorId`, audience/purpose, pinned lifecycle/store/contract versions, and exclusive expiry; require idempotent custodian delivery or authenticated exact-outcome lookup; then record a safe result and define lost-ack recovery. Add concrete `ExportDownload` matrix variants. Keep raw key material outside Agents. This does not choose the open provider or lifecycle policy.

## Medium

### M-BD3-1 — The implementation-conventions companion labels target-only protocols as “shipped”

**Classification:** false present-state wording; implementation work is already assigned.

The companion begins, “This companion defines the shipped command-step convention” (`IMPLEMENTATION-CONVENTIONS.md:3`), then normatively includes the directory-first creation outbox, workflow-start outbox, deletion barrier, and deletion propagation protocol (`IMPLEMENTATION-CONVENTIONS.md:17,32,48`). The spine explicitly says those objects do not exist in current code and that the amended rules do not claim current implementation (`ARCHITECTURE-SPINE.md:1052-1065`). Only the edit orchestrator/policy examples are accurately labeled shipped (`IMPLEMENTATION-CONVENTIONS.md:35-38`).

**Action — AUTOFIX wording only.** Call the document the target command-step convention, retain “Representative Shipped Examples” for the verified subset, and leave the directory/deletion implementation with Stories 6.1 and 8.3.

### M-BD3-2 — Story 8.2 exposes “completed” before the sole export commit point

**Classification:** bound-story status contradiction, not current code debt.

Story 8.2 says the `export` projection “reaches completed” after artifact/manifest verification (`epics.md:2876-2879`), but the next criterion says state becomes `ExportCommitted` only after the fence commit decision and `AuditExport` acknowledgement (`epics.md:2881-2884`). AD-22 and the matrix make the latter the sole commit/success boundary. A projection/UI team can expose terminal success while another exposes only a prepared/materialized state.

**Action — AUTOFIX wording.** Replace the earlier `completed` state with an explicitly nonterminal prepared/materialized status, and reserve terminal success for `ExportCommitted`.

### M-BD3-3 — Debug/source composition can bind partial Conversations adapters on configuration alone while the dependency remains Uncommitted

**Classification:** current implementation/integration debt; the target architecture is fail-closed.

Ordinary Debug builds select sibling source when present (`Directory.Build.props:22-55`), define `HEXALITH_CONVERSATIONS_SOURCE`, and compile the live adapters (`src/Hexalith.Agents.Server/Hexalith.Agents.Server.csproj:38-57`). `AddAgentsConversationServices` replaces deferred context/posting ports whenever a `Conversations` section exists, without checking the accepted dependency target, contract version, or `Available` status (`src/Hexalith.Agents.Server/Composition/ConversationServiceCollectionExtensions.cs:15-46`). The root-authoritative Conversations `IConversationClient` has create/append/read/list only and no AI membership or deletion-feed/acknowledgement seam (`references/Hexalith.Conversations/src/Hexalith.Conversations.Client/IConversationClient.cs:15-65`); the current poster itself documents the missing participant-add seam and can still call append (`src/Hexalith.Agents.Server/Ports/ConversationClientResponsePoster.cs:23-38,102-128`). Meanwhile, the live-seam matrix says Conversations membership/posting and deletion propagation are `Deferred`, and all twelve external records are `Uncommitted` (`launch-readiness-register.md:281-299`; `external-dependency-register.md:267-269`).

The current public proposal facade remains unavailable and other readers are deferred, so this review does not claim a presently reachable production post. The drift is a latent composition/evidence hazard: mere config presence is not dependency availability and must never be cited as `Live` evidence.

**Action — DEFER as implementation debt, but make the guard explicit.** Story 6.6/5.6 should bind these adapters only when the exact accepted Conversations target/version and compatibility status are present, with the matrix test flipping atomically. Until then, keep the source adapter test-only/deferred and do not use its presence as readiness evidence.

## Low

### L-BD3-1 — “Dirty Builds checkout” conflates parent-gitlink drift with nested uncommitted changes

The stack correctly treats root gitlink `Hexalith.Builds@a32cb422` and Dapr `1.18.5` as authoritative and the separate `cf52f74`/`1.18.7` checkout as non-authoritative (`ARCHITECTURE-SPINE.md:682,688-689`). The nested Builds worktree is internally clean; the root reports the submodule path modified only because its checked-out commit differs from the parent gitlink. Replace “dirty Builds checkout” with “parent-modified but internally clean Builds checkout at `cf52f74`” so evidence does not imply uncommitted nested files. The architectural version conclusion is unchanged.

## Authoritative Validation Critical/High Recheck

| Authoritative finding | Brownfield v3 disposition |
| --- | --- |
| C-1 conjunctive safety | **Closed in target architecture.** Snapshot-plus-current safety, retry outcome, evidence, and distributed rescan state are explicit. Current code remains assigned delivery debt. |
| C-2 legal-hold/deletion exclusion | **Closed for the common `ProtectionFence`.** Frozen sets, preparation, arm, destruction, receipts, and recovery prevent the original partial-erasure race. C-BD3-1 is a distinct earlier Conversation-barrier/effect cutover defect. |
| C-3 bootstrap/matrix | **Closed for matrix v4 bootstrap/scope.** Direct bootstrap and containment variants are explicit. H-BD3-2 is the stale cross-AD call-acceptance order, not the original matrix deadlock. |
| H-1 Approver recheck | **Closed.** Scheduler owner, bounded cadence, two-pass empty evidence, state outcomes, and retry are bound. |
| H-2 safety rescan | **Closed architecturally.** Durable epoch/index, finite enumeration, bounded lease/workers, cross-replica recovery, and exact `RescanPending` behavior are present. |
| H-3 human Approvers | **Closed.** Parties-owned human/liveness classification and failure outcomes are explicit. |
| H-4 three ledger lifetimes | **Closed architecturally.** Separate rate/open/budget owners and recovery are explicit; no current implementation exists, correctly recorded as debt. |
| H-5 human identity | **Closed.** Human-originated principals carry stable `AuthenticatedHumanActorId`; evidence is origin-tagged. |
| H-6 proposal index | **Closed architecturally.** Interaction events/outbox are truth; source-revision high-water and reconciliation are explicit. Current code remains debt. |
| H-7 dependency split | **Closed.** Core `EXT-CONV-AI-1` and optional retraction are separate records. |
| H-8 trusted envelope | **Closed architecturally.** Canonical bytes, logical/delivery ids, replay owner, retention, rotation/revocation, ACL confinement, and security recording are bound. |
| H-9 export | **Substantially closed.** Store/index/fence/manifest/expiry/hold/deletion lifecycle are bound without choosing the provider; H-BD3-4 remains at key delivery. |
| H-10 Dapr current exposure | **Closed.** Current transitive Client/ASP.NET `1.18.5` and future Workflow adoption are distinguished, with the non-authoritative `1.18.7` checkout reported. |
| H-11 public parity wording | **Closed in the spine.** AD-15 says required completion parity and names owners. M-BD3-1 is the remaining companion-document wording. |
| H-12 sprint/evidence contradiction | **Correctly surfaced as delivery-history debt.** `OD-SPRINT-5.1-5.2-1` preserves the choice and blocks dependent authorization; the spine does not pretend the tracker is reconciled. |

## Current Reality And Debt — Not Additional Findings

The revised `Architecture Contract Versus Delivery Debt` section is materially accurate (`ARCHITECTURE-SPINE.md:1052-1070`). Focused source search found no current `ConversationAgentState` permit directory, creation/workflow-start outboxes, deletion barrier/feed/acknowledgement, `ProtectionFence`, `AuditExport` store/index, three-ledger protocol, safety epoch/index, trusted-envelope replay registrar, architecture-decision catalog/record, or Dapr Workflow owner. Current approval code still emits `Approved`, `PostingPending`, and final posting state together and current orchestration performs the Conversations effect before EventStore dispatch (`src/Hexalith.Agents/AgentInteraction/AgentProposalApprovalPolicy.cs:14-37`; `ARCHITECTURE-SPINE.md:1066`). These are already assigned to Stories 5.4-5.8, 6.1, 6.3-6.6, 7.4, and 8.1-8.4 plus their external dependencies. They do not inflate this review's counts.

The root-authoritative gitlinks are Builds `a32cb422`, Conversations `73bcee6f`, EventStore `ce9e779a`, FrontComposer `053b2008`, Memories `3644ef63`, Parties `fa423985`, and Tenants `2fac1839`. Parent-authoritative Builds pins Dapr Client/ASP.NET/Workflow `1.18.5`; current Agents registers `AddDaprClient` through transitive EventStore exposure, while no Agents project references Dapr Workflow (`src/Hexalith.Agents.Server/Program.cs:27-36`). The separate clean Builds worktree pins `1.18.7`. The spine's architecture conclusion and `ARCH-A-15` blocker match this reality.

The current EventStore default is a no-op protection provider; the current Conversations public client lacks the required membership/deletion seams; all external records remain `Uncommitted`; every initial readiness gate remains insufficient, and `RQ-1` is explicitly `NOT READY` (`external-dependency-register.md:233-241,267-269`; `launch-readiness-register.md:394-421`). No build, test, dependency availability, or release success is claimed by this review.

## Unresolved Product And Governance Choices

No Product decision was selected or inferred. `OD-HOLD-DELETION-PRECEDENCE-1`, `OD-HOLD-PREPARE-CANCELLATION-1`, `OD-EXPORT-LIFECYCLE-1`, `OD-RATE-CONCURRENCY-CONSUMPTION-1`, `OD-DAPR-SECURITY-1`, `OD-SPRINT-5.1-5.2-1`, `OD-PRD-OQ18-HISTORICAL-SAFETY-1`, `OD-PRD-OQ23-AUTOMATIC-RETRACTION-1`, `OD-PRD-OQ31-INSTRUCTION-PROTECTION-1`, and `OD-RELEASE-RECORDER-SCOPE-1` retain their recorded Open scopes. C-BD3-1 and H-BD3-1 through H-BD3-4 require ownership, ordering, evidence, or recovery mechanics around already-stated behavior; none requires choosing one of those outcomes.

## Required Correction Order

1. Close C-BD3-1 with an enforceable effect/barrier cutover and reconcile every affected matrix variant and queued-outbox disposition.
2. Reconcile H-BD3-2's call-acceptance order so directory permit, membership, ledgers, content gates, and `AgentCallAccepted` have one literal sequence.
3. Strengthen `EXT-PROTECTION-1` for H-BD3-3's cross-stream target-DEK behavior and close H-BD3-4's export-key delivery protocol.
4. Make the assumption index singular (H-BD3-1), then correct the three wording/integration Medium items and the Low worktree descriptor.
5. Re-distill the spine/conventions/stories, append the decisions to the memlog, lint, freeze/hash, and rerun the complete reviewer gate.

## Gate Conclusion

The frozen brownfield-drift gate is **FAIL: 1 Critical, 4 High, 3 Medium, 1 Low**. Target-versus-current debt is now largely explicit and correctly assigned, but the barrier cutover, acceptance sequence, protection seam, export-key effect, and assumption-version authority must converge before the artifact set can be a safe implementation handoff.
