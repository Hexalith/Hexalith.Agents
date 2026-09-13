---
name: Hexalith Agents verified-current and PRD-conformance review v15
type: architecture-spine-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
intent: review-read-only
lens: verified-current / bound-PRD-conformance / cross-artifact authority
verdict: fail
critical: 2
high: 5
medium: 4
low: 1
lint_ok: true
---

# Verified-Current / PRD-Conformance Reviewer Gate v15

## Gate Verdict

**FAIL — 2 Critical, 5 High, 4 Medium, 1 Low.** The v14 Approver-resolution lease, initial-output-safety decision, and failure-record corrections are present and mutually consistent. However, the deletion path reopens the authoritative legal-hold/deletion Critical twice: operator deletion does not close `ConversationAgentState` permits, protected outboxes, or effect acquire/commit; and the new EventStore fence has no canonical, fail-closed membership predicate for Conversation or class/range scopes. Five High defects also prevent PASS: operator `Abort` lacks Product/Governance authority and fence removal; migration repair both omits User-action intents and lacks an atomic boundary against a stale directory append; Closing cannot release pre-lease capacity; and Provider authorization does not phase-pin the Product-selected initial-output-safety status contract for result recovery.

## Frozen Scope And Method

I read the complete frozen spine, implementation conventions, architecture memlog, authoritative `VALIDATION-REPORT-2026-09-12.md`, bound PRD, `epics.md`, both registers, repository instructions, root manifests, exact root-declared gitlinks, relevant current source, and all available declared local sources. I traced the Product-fixed FR-8 order, effect-lease authorization and recovery, generated failure semantics, operator and Conversation deletion origins, accepted-inventory/scope-fence lifecycle, migration repair, export phase pinning, story/dependency evidence, open decisions and assumptions, and current-versus-target claims. I did not select any unresolved Product, Governance, or Security outcome.

The supplied SHA-256 values matched at intake and again after this report was written:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `2b64657e34ac93becc0bc7dbf5e2829c290ad581ee578e606053dabd1a68c5ad` |
| `IMPLEMENTATION-CONVENTIONS.md` | `2a3c681658ab045de0302a9f20a527717172983619fd6e93e261bc7a07cd4a06` |
| architecture `.memlog.md` | `1b65ce14c02a64b2abf5da7e9e225b10a2efc45b35f72da6112ce865158721d7` |
| bound `prd.md` | `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb` |
| `epics.md` | `19db22ebf78bf925b6b0906d6e31a086973459b5962c611fdd58972d3070f5b8` |
| `external-dependency-register.md` | `0b85c8bdf893d9a0c38ea93b7f4beb8bf8871d8323f849d4a7fd03082488997d` |
| `launch-readiness-register.md` | `d251642ea2ef765267a7a865a1109f19ded7bf8779ce09568f4b4fd069948090` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

The deterministic spine linter returned `ok: true`, `total_findings: 0`. The spine declares 108 sources, 94 of them local. The five v15/v5 review paths declared in frontmatter were treated as anticipated concurrent outputs rather than frozen inputs; every other local source resolved. Source existence was rechecked at finalization. AD IDs are unique and contiguous from AD-1 through AD-31.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 2 |
| High | 5 |
| Medium | 4 |
| Low | 1 |

## Critical

### VC15-C1 — Operator deletion's scope fence does not close directory admission or committed effects

**Classification:** target deletion-integrity defect that can violate PRD all-copy erasure; not current implementation debt and not an unresolved Product choice.

The accepted operator-deletion candidate includes interactions, exports, indexes, artifacts/copies, projections, workflow state, and fence revisions. Before preparation, the new EventStore scope fence rejects later in-scope `AgentInteraction`/`GenerationFailureRecord` creates and content-bearing appends (`ARCHITECTURE-SPINE.md:248-250,424-428`; `launch-readiness-register.md:287-296`). It does not fence `ConversationAgentState`. That owner may still append a new interaction permit carrying a protected creation-outbox prompt under the target DEK, a protected User-action-intent edit payload, or a lease acquisition/commit authorizing protected reads and external effects (`ARCHITECTURE-SPINE.md:198-200`; `launch-readiness-register.md:246-247`). The Closing/Effective barrier that serializes these operations exists only for `ConversationApprovedDeletion`; the operator-origin `GovernanceProtection` allowlist has no directory Closing, finite permit/outbox/lease manifest, or must-settle recovery grant (`ARCHITECTURE-SPINE.md:242-252,502`; `launch-readiness-register.md:277-286`).

After operator inventory acceptance and scope-fence installation, a worker can append a new permit/protected outbox or commit an in-scope Provider/posting/mutation lease because neither append targets the guarded interaction namespace. If target delivery later attempts an interaction append, EventStore rejects it, so there may be no accepted target-stream violation to add to the containment ledger; the protected directory payload and its DEK were never in the accepted set. A committed external effect may likewise run without the operator branch having the Conversation-origin manifest authority required to resolve and settle it. `DeletionDestructionStarted` can still observe the interaction namespace fence as installed and violation-free, and completion can omit the directory copy or late effect. This contradicts PRD FR-30's restrictive deletion/all-copy outcome and reopens authoritative C-2 for operator-origin scope.

**Required correction — architecture mechanics.** Give both deletion origins an executable admission/effect cut before inventory acceptance or preparation eligibility. Extend the persistent guard to the exact in-scope `ConversationAgentState` permit, protected creation/User-action outbox, lease-acquire, and lease-commit operations, or install scope-aware same-owner Closing barriers over a finite checkpointed directory cohort. Add operator Workflow grants only for cancelling reservations, consuming outboxes, resolving existing committed target/external outcomes, settling ledgers/capacity/posting, acknowledging every member, and proving an Effective/fixed-point cut; forbid new effects and scope expansion. Race permit/action-intent creation, every lease acquire/commit, Provider/capacity/posting boundaries, migration successors, scope-fence installation, and destruction verification for both origins. No Product decision is needed to require complete erasure and no post-cut content/effect admission.

### VC15-C2 — The deletion-scope fence has no canonical, EventStore-computable membership predicate

**Classification:** target scope-authorization/all-copy containment defect; Product owns only any unresolved class taxonomy or range meaning, not the need for a fail-closed wire predicate.

AD-2 gives legal hold the literal forms `interaction:<AgentInteractionId>` and `class:<ContentClass>` plus a UTC range, but defines neither a closed `ContentClass` vocabulary nor the authoritative timestamp and inclusive/exclusive range semantics (`ARCHITECTURE-SPINE.md:192`; `epics.md:2830-2833`). Conversation-origin deletion instead targets all derived Agents content of one exact source Conversation (`ARCHITECTURE-SPINE.md:242-254`; `prd.md:876-883`). The EventStore guard is bound only to an “approved immutable scope grammar” and must decide whether a later interaction/failure create or append is inside/matching it (`ARCHITECTURE-SPINE.md:248-250,424-428,507`; `external-dependency-register.md:129`; `launch-readiness-register.md:291-303`). No source defines a `conversation:<ConversationId>` encoding, the authenticated owner/permit field from which EventStore proves Conversation membership, class assignment ownership, the range timestamp, or fail-closed behavior for missing/ambiguous classification.

One implementation can guard only current accepted stream ids; another can evaluate future streams by source Conversation; class/range implementations can use interaction creation, source-event, proposal, terminal, or append time and incompatible class values. A stale/restored writer can then create or append a resource that is in Product scope under one interpretation but outside another. After `DestructionStarted`, recovery intentionally relies on the phase-pinned fence and violation ledger rather than mutable migration/inventory; an unclassified or differently classified accepted write is absent from that ledger and can survive successful completion.

Define one versioned canonical scope schema shared by Agents and `EXT-HOST-1`: exact Conversation, exact interaction-id set, and any Product-authorized class/range form; closed class vocabulary; time source and boundary semantics; and an EventStore-computable membership proof for create and append derived from authenticated permit/owner facts, never caller assertions. Bind schema version and predicate digest into request approval, accepted inventory, fence authorization/receipt, violation evidence, and completion. Missing, malformed, unknown-class, cross-tenant, or unverifiable membership must reject the write and block verification. If class taxonomy or time meaning is not settled Product/Governance policy, surface and scope an Open Decision and block only that variant; do not invent it. Exact Conversation and interaction variants still require architecture-defined wire predicates now.

## High

### VC15-H1 — Operator deletion `Abort` is Product-unauthorized and strands its persistent scope fence

**Classification:** target Product/governance-authority gap plus target deletion-lifecycle divergence; not current implementation debt.

**Binding authority.** The PRD authorizes a Platform Operator to request deletion and requires recorded Compliance Inspector approval before execution, but defines no cancellation of an approved operator deletion and no permanent-failure class that may silently replace deletion with `Abort` (`prd.md:109,662`). Where a similarly material reversible hold-cancellation choice is unresolved, the PRD and spine correctly expose `OQ-34` / `OD-HOLD-PREPARE-CANCELLATION-1` and fail closed (`prd.md:1046,1088`; `ARCHITECTURE-SPINE.md:1184`). Conversation-origin deletion likewise has no local `Abort` without a future Product/Governance-approved source supersession (`ARCHITECTURE-SPINE.md:240-254`; `epics.md:3003-3005`).

**Invented operator branch.** In contrast, AD-17 permits operator-origin deletion `Abort` from an “explicitly authorized cancellation” or a closed failure class mapped by an already-recorded recovery profile, without naming the authority owner, decision id/version, allowed requester/approver relationship, terminal product outcome, or even a required open decision (`ARCHITECTURE-SPINE.md:388,438`). Matrix v4 makes that branch executable with `RecordedAuthorizedOperatorDeletionCancellationOrProfileMappedClosedFailureFactValid`, and Story 8.3 requires it (`launch-readiness-register.md:298-300`; `epics.md:3007-3010`). No corresponding PRD open question, spine Open Decision, or register decision record exists. Two conforming units may therefore deny all cancellation, let the original requester cancel after Inspector approval, require another Inspector, or turn a permanent dependency failure into an abandoned regulatory deletion.

**Stranded-fence failure.** Independently of who may authorize it, every deletion preparation requires a previously installed persistent EventStore deletion-scope write fence. The guard rejects every later matching interaction/failure create or content append and must survive every migration successor (`ARCHITECTURE-SPINE.md:248-250,426-428,507`; `external-dependency-register.md:129`; `launch-readiness-register.md:291-296`). The operator `Abort` recovery row may unwind reversible prepare effects but names no fence-removal authorization, target effect, authenticated result, exact lookup, migration acknowledgement, or terminal ordering (`launch-readiness-register.md:300`). No such remove/release/uninstall command exists anywhere in the spine, conventions, epics, or registers. Thus a successful unwind leaves the tenant/scope permanently write-fenced and every future migration is expressly required to preserve the orphan. If an implementation removes it locally, it acts outside the closed Workflow/EventStore capabilities and cannot safely resolve crash, lost acknowledgement, retry, or a race with `DestructionStarted`.

**Required correction without choosing Product.** Surface a versioned Product + Governance + Security decision for whether an already-approved operator-origin deletion may cancel/Abort at all, which actors/evidence authorize it, whether any closed failure may end rather than remain restrictive pending, and its user-visible/audit outcome. While Open, operator deletion must Resume/retry and the Abort/unwind branch must be blocked, just like the source-deletion branch. If the approved outcome permits Abort, add a closed authorize/effect/result protocol that removes exactly the phase-pinned fence id/scope only after every reversible preparation has authenticated never-created or cleanup-complete receipts and before terminal `Aborted`; bind exact outcome lookup, lost-ack recovery, expected revisions, migration preservation until removal acknowledgement, no `DestructionStarted`, and remove-versus-start race fixtures. If Product chooses no Abort, remove the branch and preserve the fence/deletion until completion. Architecture must not choose between those outcomes.

### VC15-H2 — Migration repair omits User-action-intent outboxes from the spine-authoritative cohort

**Classification:** cross-artifact recovery-contract divergence; not implementation debt.

AD-2's repair rule freezes every directory permit, creation/workflow-start outbox, and effect-lease count/hash/high-water, and its old-epoch bridge admits only those named pending outboxes and committed leases (`ARCHITECTURE-SPINE.md:204`). It does not name the protected User-action-intent outbox introduced four lines earlier. Matrix v4 and Story 6.1 explicitly include User-action intents in the repair manifest, bridge, drain/cancel, and successor proof (`launch-readiness-register.md:241-245`; `epics.md:1903`), while the conventions say only generic “all current permit/outbox/lease high-waters” (`IMPLEMENTATION-CONVENTIONS.md:23`). Because the conventions and registers defer to the spine, separate implementations can omit or include these intents. Omitting one may revoke the old epoch with a committed, non-revocable action still awaiting its exact target result/acknowledgement or leave a reserved protected intent unconsumed.

Amend AD-2 and the AD-30 migration capability to enumerate creation, workflow-start, and User-action-intent outboxes consistently. A reserved intent must be cancelled/consumed; a committed intent must reach the exact target result, outbox acknowledgement, and lease settlement. Successor activation must require every frozen member's exact acknowledgement.

### VC15-H3 — Closing cannot release capacity acquired before the Provider lease commits

**Classification:** target recovery/availability defect; not implementation debt.

The normative sequence acquires or queues capacity by `AttemptId` before it reserves and commits `ProviderInvocation` (`ARCHITECTURE-SPINE.md:667-675`). Closing can therefore win after the allocator returns admitted/queued but before the Provider lease exists or commits. Effective correctly requires applicable capacity settlement, yet the only deletion recovery row, `ReleaseManifestedCapacity`, requires a committed Provider lease plus authenticated Provider outcome and Budget settlement (`ARCHITECTURE-SPINE.md:252,320,505`; `launch-readiness-register.md:284-286`). Those facts do not exist in this race. The ordinary Interaction Workflow is named as owner of rate/open/pre-Provider Budget recovery, but no text grants it or the deletion Workflow an exact pre-Provider capacity cancellation path after Closing. A crash at that boundary can therefore retain an allocator admission forever and prevent the finite manifest from reaching Effective.

Add a distinct manifest-bound pre-Provider capacity cancel/release variant authorized only by the existing interaction/attempt admission identity and fence plus authoritative proof that no `ProviderInvocationAuthorized` or `BeginInvocation` exists. It must cover queued and admitted states, forbid acquire/invoke/outcome invention, record the interaction decision and allocator acknowledgement, and support exact lookup/lost acknowledgement. Race Closing immediately after enqueue/admission and before lease reserve/commit.

### VC15-H4 — Migration repair has no atomic boundary against a stale directory append

**Classification:** target cross-owner repair/fencing defect; not implementation debt.

Migration invalidation/repair is owned by tenant `InteractionDirectoryMigration`, while permits, protected outboxes, and lease acquire/commit append to independent per-Conversation `ConversationAgentState` owners. Begin repair quiesces topology and freezes directory high-waters; the old-epoch bridge then admits only the frozen cohort (`ARCHITECTURE-SPINE.md:202-204`; `launch-readiness-register.md:241-245`). Directory commands carry a previously observed migration-ready epoch plus their directory expected revision, but the EventStore epoch guard protects only interaction/failure target-stream creates/content appends—not the directory append itself (`ARCHITECTURE-SPINE.md:202`; `external-dependency-register.md:129`; `launch-readiness-register.md:246-249`).

A disconnected worker can read `DirectoryReady` and a still-current directory revision, then append a permit, User-action intent, acquisition, or commit after repair freezes the separate migration owner. Its later target write may be rejected, but the new directory member is outside the bridge manifest. One repair waits forever after detecting it; another completes from the frozen cohort and namespace scans, leaving protected pending state outside readiness proof. Install a directory repair Closing/fence at every affected owner, or make every directory append consume a current migration-epoch capability whose revocation is atomic with the repair freeze. Freeze only after that boundary; every pre-boundary winner enters the manifest and every loser appends nothing. Successor activation must compare the exact directory fence receipts/high-waters, with lost-ack recovery.

### VC15-H5 — Provider authorization does not phase-pin the chosen initial-output-safety status contract

**Classification:** target decision-governance and committed-effect recovery defect; no Product outcome is selected by the correction.

While `OD-INITIAL-OUTPUT-SAFETY-STATUS-1` is Open, live Provider work correctly blocks. Once approved, however, `ProviderInvocationAuthorized` binds descriptor, Budget/capacity, effect-lease commit, and evaluated readiness/matrix versions but not the exact effective decision record/revision, selected status/reason/metric/open-lease mapping, or catalog activation (`ARCHITECTURE-SPINE.md:338-340`; `launch-readiness-register.md:254-256`). The gate-free safety-negative result merely requires an approved matching decision version without saying whether it means authorization-time or current authority. If a successor becomes pending/Open or changes the mapping after invocation begins, one implementation uses the original contract, another blocks the non-revocable committed lease indefinitely, and another uses the new public status/metric/ledger mapping for old authorization.

Bind the exact effective output-status decision record, contract digest, selected mapping, and catalog activation revision into `ProviderInvocationAuthorized` and the committed lease. Negative-result recording and crash recovery must use only that phase-pinned mapping; a successor governs only Provider authorization not yet committed. This is recovery mechanics and chooses neither `GenerationFailed` nor `SafetyFailed`.

## Medium

### VC15-M1 — Architecture-owned `RQ-1` assumptions still lack literal retirement dates

PRD FR-28 requires co-owner-approved literal calendar dates for Architecture-owned readiness assumptions; absence remains a blocker. `ARCH-A-1`, `-2`, `-3`, the test-stack part of `-4`, `-6`, `-7`, `-8`, `-11`, `-12`, and `-14` remain `Unscheduled` or milestone-only (`ARCHITECTURE-SPINE.md:1244-1262`). The spine correctly fails readiness closed, so this is governance incompleteness rather than an unsafe runtime default. Obtain recorded dates or an authorized bounded deferral; do not invent them.

### VC15-M2 — `PostingPending` still has no one concrete timeout authority

AD-5 fixes only a lower bound—no shorter than the committed Conversations seam-2 posting timeout—while `ARCH-A-14` confirms that neither the PRD nor spine fixes the concrete duration or definitive versioned configuration source (`ARCHITECTURE-SPINE.md:221,1261`). Story 7.4 remains blocked, so no unsafe implementation default is authorized. Architecture should propose the source, snapshot rule, and compatibility bound for Product confirmation before posting/recovery is ready.

### VC15-M3 — Pre-commit control-plane observations lack one normative vocabulary boundary

AD-2 and the conventions permit only content-free owner-local checks before commit and broadly put every “other dependency read” after commit (`ARCHITECTURE-SPINE.md:198`; `IMPLEMENTATION-CONVENTIONS.md:19`). AD-12 instead says commit occurs after the last mutable authorization/readiness checks, while the acquire matrix accepts phase-pinned control-plane evidence (`ARCHITECTURE-SPINE.md:320`; `launch-readiness-register.md:247`). The target consistently commits before protected-version, safety, roster/Conversation, target, and effect I/O, so this is not an identified unsafe branch. Define `OwnerLocalCheck`, `ControlPlaneObservation`, and `TargetDependencyRead` once so separately built orchestrators place content-free readiness observations on the same side of the commit.

### VC15-M4 — Current code already selects the open initial-output-safety status, but the debt ledger does not say so

`OD-INITIAL-OUTPUT-SAFETY-STATUS-1` correctly leaves `GenerationFailed` versus `SafetyFailed` to Product + Security and blocks live Provider work (`ARCHITECTURE-SPINE.md:1193`; `launch-readiness-register.md:256`; `epics.md:1884,1994-2012`). Current code nevertheless maps `ContentSafetyBlocked` to public/durable `SafetyFailed`, and current tests assert that mapping (`src/Hexalith.Agents/AgentInteraction/AgentOutputGenerationPolicy.cs:25-28,43-55,69-71`; `test/Hexalith.Agents.Tests/AgentInteractionGenerationAggregateTests.cs:57-81`; `test/Hexalith.Agents.Server.Tests/AgentInteractionGenerationOrchestratorTests.cs:93-127`). The implementation-debt table lists many current/target gaps but omits this one (`ARCHITECTURE-SPINE.md:1200-1217`). Because the target blocks live use, this does not override the Open decision or rise to High; however, builders may mistake existing code/tests for Product evidence. Add one explicit debt row saying the mapping is unapproved current behavior, cannot satisfy the decision, and may require contract/event/UI/test migration after Product chooses.

## Low

### VC15-L1 — bUnit is valid but behind the current stable package

The root pins `bunit` `2.9.0`, which the Stack reports accurately, while the non-authoritative Builds checkout and official NuGet V3 index contain stable `2.10.3`. This is non-blocking test-stack delivery maintenance assigned to Story 5.6, not an architecture defect. Source: [official NuGet package index](https://api.nuget.org/v3-flatcontainer/bunit/index.json).

## v14 Finding Closure Audit

| v14 item | v15 disposition |
| --- | --- |
| VC14-H1 Approver resolution lacked a deletion-cutover lease | **Closed.** `ApproverResolution` is a closed lease kind; call-time and scheduled resolution reserve/commit before Conversations/Parties reads, bind typed results, settle before Context, and race Closing on the same owner (`ARCHITECTURE-SPINE.md:198,245,269-273,320,337`; `IMPLEMENTATION-CONVENTIONS.md:19`; `launch-readiness-register.md:248-249`; `epics.md:1893`). |
| VC14-H2 initial-output-safety status conflict | **Closed in target authority.** `OD-INITIAL-OUTPUT-SAFETY-STATUS-1` is Open under Product + Security; every target consumer blocks rather than chooses, while Provider error/timeout remains `GenerationFailed`. VC15-M4 separately records that current code already made an unapproved choice. |
| VC14-H3 unconditional live failure record | **Closed.** Spine, conventions, matrix, and Stories 6.1/6.3/6.7/7.1 agree that V1 retains no failed bytes and creates no live `GenerationFailureRecord`; any future retention requires Product approval and a complete owner/protection/deletion protocol. |
| VC14-M1 assumption dates | **Open as VC15-M1.** |
| VC14-M2 posting timeout | **Open as VC15-M2.** |
| VC14-L1 bUnit lag | **Open as VC15-L1.** |

## Authoritative Validation-Finding Closure Audit

| Authoritative finding | v15 disposition |
| --- | --- |
| C-1 weaker safety evaluation | **Closed.** Snapshot/current policies are conjunctive; collapse needs machine-checkable semantic dominance. |
| C-2 hold/deletion exclusion | **Reopened at VC15-C1/C2.** The shared `ProtectionFence` and target-stream guard serialize their stated set, but the operator branch does not stop/drain directory state and effects, and the guard's scope-membership predicate is undefined. Conversation-origin Closing is otherwise strong but still depends on that predicate and has VC15-H3's capacity race. |
| C-3 bootstrap/matrix deadlock | **Closed.** Target-aware Platform/Tenant bootstrap, containment, recorder, migration, repair, and recovery variants avoid circular readiness. |
| H-1 scheduled Approver recheck | **Closed.** Durable cadence/single-flight ownership, two-pass empty evidence, state-specific outcomes, committed `ApproverResolution` leasing, and unavailable retry are explicit. |
| H-2 safety rescan ownership | **Closed.** Durable epoch/index owners, finite manifests, fencing, initialization, and `RescanPending` are bound. |
| H-3 human-only Approvers | **Closed.** Parties-owned human type/liveness and stable actor binding fail closed. |
| H-4 conflated ledgers | **Closed for owner/lifetime separation; VC15-H3 is a separate deletion-recovery omission for an allocator admission before Provider-lease commit.** |
| H-5 human separation identity | **Closed.** Stable `AuthenticatedHumanActorId` spans Party-bearing and Party-free principals. |
| H-6 proposal-index crash consistency | **Closed.** Interaction truth, same-append outbox, source high-water, fixed-point reconciliation, and acknowledgement recovery are explicit. |
| H-7 indivisible Conversations dependency | **Closed.** Six core seams and optional retraction have separate records, readiness, and consumers. |
| H-8 trusted-envelope security | **Closed.** Canonical authenticated bytes, replay owner, issuer-wide nonce, expiry, rotation/revocation, and safe denial spool are bound. |
| H-9 export ownership/lifecycle/signature | **Closed architecturally.** Immutable encrypted store/index, fence commit, canonical ES256 manifest, principal-bound key delivery, phase pinning, purge, and all-copy receipts are explicit while Product policy stays Open. |
| H-10 current Dapr exposure | **Closed as verified-current truth/debt.** Parent-authoritative Client/ASP.NET `1.18.5`, official/non-authoritative `1.18.7`, and future Workflow adoption are distinguished. |
| H-11 overstated shipped parity | **Closed.** Missing target contracts are classified as delivery debt, never shipped behavior, subject to VC15-M4's one omitted current mapping. |
| H-12 tracking/dependency conflict | **Closed as surfaced governance debt.** Sprint history cannot override external/dependency/evidence authority. |

## Requested Reconciliation Audit

| Focus | Result |
| --- | --- |
| Bound PRD and Product-fixed FR-8 order | **Pass.** Directory registration remains pre-acceptance intake; joint rate/open is step 5, leased Confirmation-only Approver resolution step 6, Context step 7, descriptor/Budget step 8, safety step 9, and separately leased membership step 10 immediately before acceptance. |
| Effect authorization and deletion cutover | **Fail at VC15-C1/H3/H4.** Every ordinary interaction phase commits its exact lease before protected access/dependency/effect, but only Conversation-origin deletion installs Closing; its capacity recovery misses the acquire-before-lease race. The operator branch has no corresponding directory/effect cut, and repair cannot atomically fence stale directory appends. |
| Generated failure semantics | **Pass for byte/record ownership; fail at VC15-H5 for decision recovery; debt clarity at VC15-M4.** Provider error/timeout remains content-free `GenerationFailed`; output safety creates no live failure record. Current code's pre-existing selection is not target authority. |
| Operator and Conversation deletion authority | **Fail at VC15-C1/C2/H1/H3.** Initial operator request/Inspector approval and approved scope are exact, but operator admission/effect cut and Abort authority/removal are absent, and neither origin has a canonical scope predicate. Conversation origin remains source-limited with no local Abort. |
| Migration epoch repair | **Fail at VC15-H2/H4.** The declared bridge/successor mechanics are otherwise strong, but the cohort both omits User-action intents in the authoritative prose and can be raced by a stale directory append. |
| Story/dependency/evidence ownership | **Fail at VC15-C1/C2/H1-H5.** Direct and conditional dependency consumers otherwise match. Story 8.3 makes the incomplete operator branch executable; Story 6.1's repair criteria exceed the spine rule; Provider-result authority is not phase-pinned at authorization. |
| Unresolved Product/Governance decisions | **FAIL at VC15-H1; otherwise pass.** Rate/concurrency, output-safety status, recorder scope, legacy plaintext, OQ-31, export lifecycle, hold/deletion precedence, hold cancellation, Dapr security, and historical-safety/retraction questions are surfaced without chosen outcomes. |
| Architecture versus implementation debt | **Pass except VC15-M4.** Directory/effect leases, protection, migration, deletion, export, decision catalog, Dapr Workflow, and public vocabulary are target/deferred; current raw prompt, no-op protection, direct streams, and post-before-dispatch approval are debt. Existing initial-output-safety status selection needs its own explicit debt row. |

## Verified Repository And Technology Reality

- Root-authoritative gitlinks remain Builds `a32cb422`, EventStore `ce9e779a`, Conversations `73bcee6f`, Parties `fa423985`, Tenants `2fac1839`, FrontComposer `053b2008`, and Memories `3644ef63`. Initialized Builds `cf52f74`, EventStore `a568af4`, Conversations `64b05083`, FrontComposer `1b3608c`, and Memories `42dfa26` checkout HEADs differ from their parent gitlinks; Parties and Tenants match. Every checkout was internally clean at review time. The spine does not promote checkout HEADs to root authority. No submodule was initialized, edited, cleaned, or advanced by this review.
- Root `global.json` pins SDK `10.0.401` with `latestPatch`; `Directory.Build.props` targets `net10.0` and C# 14; the solution is `.slnx`; Central Package Management and root overrides match the Stack. Official .NET metadata lists current SDK `10.0.401` and runtime `10.0.12`. Source: [official .NET 10 release metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json).
- Parent-authoritative Builds pins Dapr Client/ASP.NET/Workflow `1.18.5`; parent EventStore Client/DomainService consume Client/ASP.NET. The different Builds checkout pins `1.18.7`, and official NuGet contains stable `1.18.7` for all three packages. Current Agents projects consume EventStore transitively and do not reference Dapr Workflow. Sources: [Dapr.Client](https://api.nuget.org/v3-flatcontainer/dapr.client/index.json), [Dapr.AspNetCore](https://api.nuget.org/v3-flatcontainer/dapr.aspnetcore/index.json), [Dapr.Workflow](https://api.nuget.org/v3-flatcontainer/dapr.workflow/index.json).
- Current `InteractionRequested`/interaction state retain raw `Prompt`; the parent-authoritative EventStore default protection service is no-op/unprotected. Current code has no `ConversationAgentState`, effect lease, target protection-key alias, migration fence, deletion barrier/scope guard, durable Dapr Workflow owner, or target governance aggregates. The spine correctly blocks live content/deletion evidence and assigns implementation work rather than claiming these structures exist.
- Current approval orchestration still posts to Conversations before durable aggregate dispatch. That is accurately listed as Story 7.4 implementation debt. Current output-safety handling selects `SafetyFailed`; VC15-M4 requires the debt ledger to state that this is not the target Product decision.

## Architecture Defects Versus Implementation Debt

| Item | Classification |
| --- | --- |
| VC15-C1/C2 | Target all-copy deletion/admission/effect-cut and canonical-scope defects; implementation cannot infer a safe destructive boundary. |
| VC15-H1 | Target Product/governance authority and target fence-lifecycle defect. Surface the decision; only after approval may Architecture bind the selected complete branch. |
| VC15-H2-H5 | Target recovery/fencing/phase-pinning omissions; not missing implementation of a complete design. |
| VC15-M1/M2 | Explicit governance/architecture inputs that already fail closed; do not invent dates or timeout policy. |
| VC15-M3 | Target vocabulary/placement clarity debt. |
| VC15-M4 | Existing brownfield behavior omitted from the target/debt reconciliation; code and tests are not Product authority. |
| Missing directory, leases, protection, migration guard, durable deletion feed, export, decision catalog, and workflows | Correctly stated implementation/external delivery debt assigned to stories/register records. |
| Current plaintext/direct-stream/post-before-dispatch behavior | Correctly stated brownfield debt, not target architecture truth. |
| Different submodule checkout HEADs | Correctly separated from exact root gitlink authority; no repository update is implied. |
| bUnit lag | Low build/test maintenance debt. |

## Final Hash, Source, And Lint Verification

**PASS for freeze integrity and deterministic lint; FAIL for the semantic reviewer gate.** All eight reviewed input hashes exactly matched the supplied values after the report write. A final `uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2` returned `ok: true`, `total_findings: 0`, with no severity entries. This report plus the concurrently produced rubric-v15 and adversarial-v15 reports existed at finalization; security-v5 and brownfield-v5 remained anticipated missing paths. Mechanical/source checks do not override VC15-C1/C2 or VC15-H1 through VC15-H5.
