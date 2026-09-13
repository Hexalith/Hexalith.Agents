---
name: Hexalith Agents verified-current and PRD-conformance review v12
type: architecture-spine-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
intent: review-read-only
lens: verified-current / PRD-conformance / cross-artifact authority
verdict: fail
critical: 0
high: 3
medium: 3
low: 1
lint_ok: true
---

# Verified-Current / PRD-Conformance Reviewer Gate v12

## Gate Verdict

**FAIL — 0 Critical, 3 High, 3 Medium, 1 Low.** The v10/v11 deletion, source-signal, export, and effect-linearization corrections are substantially closed, but the current handoff still contradicts the Product-fixed FR-8 sequence, has no authorized completion path for the plaintext legacy state that exists in the repository, and omits three direct Story 6.1 dependencies from the authoritative story/register chain. PASS requires all three High findings to close; this review does not select the unresolved legacy-data outcome.

## Frozen Scope And Method

I read the complete frozen `ARCHITECTURE-SPINE.md`, `IMPLEMENTATION-CONVENTIONS.md`, architecture memlog, authoritative `VALIDATION-REPORT-2026-09-12.md`, bound PRD, replacement `epics.md`, both registers, repository instructions, root manifests, root-authoritative gitlinks, and relevant current source. I traced the literal permit/start/context/provider/mutation/post and Conversation-deletion paths, checked Product authority before proposing mechanics, and treated implementation absence separately from a target-contract defect.

The following SHA-256 values matched the supplied freeze at intake and again after writing this report:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `f77a939e0802e0c75375281a9f43a333df318de29f8b0fc4f0e0da8ba797bd25` |
| `IMPLEMENTATION-CONVENTIONS.md` | `d40aa9745f80b9d0b6ac65a0e5b37f7fa7b5ed1b6f8ff19c11725192d23d325b` |
| architecture `.memlog.md` | `63bf950c91f781760986d9bb0789af931396313305c1f64739435cfc7e3fbbec` |
| bound `prd.md` | `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb` |
| `epics.md` | `28b517b0d1d76463672e9852748a4f6d1e54362f827e38391ab0b57d0c1e13a2` |
| `external-dependency-register.md` | `a63a8f69384ae3b67d4f324ccca8888195eb4caba1b415955af93f303acb66a5` |
| `launch-readiness-register.md` | `164278a340b71b84831e67addd9d7c809032c157853a59167845bb4b2248bb32` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

The deterministic spine linter passed with `ok: true`, `total_findings: 0`. The final hash/lint recheck is recorded below.

The deliberate pending v12/v4 reviewer citations were excluded from source-existence findings as instructed. Every other local source declared in the spine frontmatter resolved.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 3 |
| Medium | 3 |
| Low | 1 |

## Critical

None. The remaining deletion defects fail closed or create a non-content external mutation; no currently specified path can falsely complete irreversible Agents-content erasure after the new Closing/Effective protocol is followed.

## High

### VC12-H1 — Pre-permit membership contradicts Product's fixed FR-8 order and is the one intake effect outside the deletion lease cut

**Classification:** target architecture / PRD-conformance and deletion-cutover defect; not implementation debt and not an unresolved Product choice.

**Binding authority and contradiction.** The PRD says an Agent Call is accepted only after ten checks in one explicit order: joint rate/open admission is step 5, Approver resolution step 6, context step 7, cost reservation step 8, safety step 9, and roster-dependent membership/join step 10; only steps 9 and 10 fall outside the fast-gate series (`prd.md:286-298`). The spine instead makes membership “the last pre-permit eligibility step,” then registers the directory permit and creates the interaction before rate/open, context, budget, and safety (`ARCHITECTURE-SPINE.md:235,239,307`; sequence `:542-603`). Story 6.1 and matrix v4 repeat that incompatible order (`epics.md:1889-1893`; `launch-readiness-register.md:235`). This is not a blank Product area: the PRD already chose the order.

**Deletion interleaving.** `ConversationEffectLease` covers `WorkflowStart`, `ContextRead`, `ProviderInvocation`, `ProposalMutation`, and `ConversationPosting`, but not the pre-permit membership append (`ARCHITECTURE-SPINE.md:189,291`; matrix `:236`). An intake can read old eligibility, deletion can install and even activate the barrier, and the stale intake can then attempt the idempotent Conversations membership effect before `RegisterInteractionPermit` observes and rejects the barrier. No prompt/interaction is persisted after the permit rejection, so this is High rather than Critical, but the architecture's claimed “no external effect can begin after the effective cut” is false and Conversations may observe a post-approval participant mutation. Independently, calls later rejected at rate, context, budget, or safety can leave the visible membership side effect Product placed last.

**Required correction — AUTOFIX mechanics, preserve Product.** Restore the PRD's exact step 1-through-10 order across AD-7, AD-12, AD-13, conventions, the sequence, matrix, and Story 6.1. Registering durable intake may occur before acceptance, but membership must remain step 10 after safety and before `AgentCallAccepted`. Give the membership/join operation its own directory-linearized effect lease (or an equivalently exact effect-permit kind) acquired after the interaction permit and immediately before the Conversations membership authorization/effect; Closing either rejects it or manifests the active lease until its typed result and acknowledgement settle. Add rate/context/budget/safety failure fixtures proving no membership occurred, plus Closing/Effective races immediately before and after membership authorization. Do not move the Product step unless the PRD itself is changed by Product.

### VC12-H2 — Nonempty legacy plaintext has no authorized migration/eradication outcome, while Story 6.1 can appear complete without testing the blocker

**Classification:** unresolved Product/Governance/EventStore migration decision plus cross-artifact delivery defect; not ordinary implementation debt. The safe runtime state is correctly fail-closed, but the V1 upgrade/deletion path is not executable for the repository reality.

**Repository and invariant evidence.** Current `InteractionRequested` carries `Prompt` as a plain `string`, explicitly calls the event/state its durable home, and folds the value directly into `AgentInteractionState.Prompt` (`src/Hexalith.Agents.Contracts/AgentInteraction/Events/InteractionRequested.cs:3-31`; `src/Hexalith.Agents/AgentInteraction/AgentInteractionState.cs:15-43,123-136`; `src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.cs:39-88`). No current `ConversationAgentState`, target-key alias, or interaction-directory contract exists. The spine also says old streams are frozen and never rewritten (`ARCHITECTURE-SPINE.md:672`). Its new migration can therefore reach `DirectoryReady` only when every legacy sensitive field was already protected or the signed inventory is exactly empty; plaintext stays `PayloadProtectionUnavailable` “pending a separately authorized migration or eradication procedure” (`ARCHITECTURE-SPINE.md:189,239`; matrix `launch-readiness-register.md:231-234`). Copying plaintext into a newly protected stream would not erase the immutable old event, snapshot, backup, or restored copy and therefore cannot satisfy PRD FR-30's cryptographic erasure/all-copies rule (`prd.md:955-962`).

**Missing authority and story mismatch.** No Blocking Open Decision, dependency record, owner, accepted procedure, or story acceptance/evidence row defines who may authorize the nonempty-plaintext outcome, what immutable-history exception or physical eradication is legal, or which all-copy receipts close it. Story 6.1's migration AC requires inventory/backfill/freeze/two scans but omits the spine/matrix predicate that every legacy sensitive field is protected-or-empty and names no negative plaintext fixture (`epics.md:1896-1899,1923-1932`). A story-only reviewer could declare the migration done over the exact current raw-prompt shape even though the runtime matrix must reject it; a matrix-literal implementation instead blocks calls and Conversation deletion indefinitely whenever a real legacy stream exists.

**Required correction — DISCUSS outcome, then bind it; do not invent it.** Product, Governance/Security, and the EventStore maintainer must explicitly choose and authorize the nonempty legacy posture. Safe candidates to evaluate include proving the inventory empty, defining an EventStore/backup-level physical eradication procedure with all-copy/restore receipts and an explicit immutable-history exception, or declaring a governed unsupported/decommission path for deployments with legacy content. Architecture must record that outcome as a stable open decision until resolved and bind its owner, scope, safe state, evidence, retries, backups/restores, and completion event. Independently and without choosing the outcome, add the protected-or-exact-empty condition and a plaintext rejection fixture to Story 6.1's AC/evidence so `DirectoryReady` cannot be claimed from backfill alone.

### VC12-H3 — Story 6.1 directly consumes host, secrets, and Parties contracts that its story and authoritative dependency consumers omit

**Classification:** cross-artifact dependency/evidence authority defect; target story metadata, not a claim that any dependency is implemented.

**Evidence.** Story 6.1 owns the Dapr Workflow instance, target-interaction DEK alias creation outbox, caller/Party and membership checks, and production-like restart evidence (`epics.md:1873-1931`). AD-16 says Dapr Workflow and the protection engine are composed only by `EXT-HOST-1` (`ARCHITECTURE-SPINE.md:335-337`); `EXT-PROTECTION-1` wraps each DEK through `EXT-SECRETS-1` and its alias compatibility test destroys/replays the same key in both owners (`external-dependency-register.md:233-239`); AD-7/AD-30 require the selected `EXT-PARTIES-1` identity branch and Party-state/subject binding on call intake (`ARCHITECTURE-SPINE.md:235,455`).

Story 6.1 nevertheless names only `EXT-PROTECTION-1`, `EXT-CONV-AI-1`, and `EXT-TOPOLOGY-1` as external prerequisites, and its evidence result omits even the first two current blockers (`epics.md:1881-1885,1923-1932`). The register's supposedly exact consumers omit Story 6.1 from `EXT-HOST-1` (`external-dependency-register.md:123-135`), `EXT-SECRETS-1` (`:181-193`), and `EXT-PARTIES-1` (`:107-119`). Because the spine says consuming stories and status are read from that register (`ARCHITECTURE-SPINE.md:1110-1129`), these omissions can authorize ready-for-dev/evidence closure against an unselected host, unavailable key custody, or uncommitted Party contract even though the literal flow cannot run.

**Required correction — AUTOFIX source chain.** Add Story 6.1 to the exact consumer lists for `EXT-HOST-1`, `EXT-SECRETS-1`, and `EXT-PARTIES-1`; add those dependencies to Story 6.1's External/Dependencies/Requirements rows with the exact direct-versus-inherited evidence required. Reconcile its Result with every currently `Uncommitted` direct dependency. Keep deterministic no-content component tests explicitly separable, but do not let them satisfy live Workflow, protected-payload, membership, or production-like evidence.

## Medium

### VC12-M1 — Architecture-owned `RQ-1` assumptions still lack the PRD-required literal retirement dates

PRD FR-28 requires a co-owner-approved literal calendar retirement date for Architecture-owned `RQ-1` assumptions; a missing date remains a blocker. `ARCH-A-1`, `-2`, `-3`, the test-stack portion of `-4`, `-6`, `-7`, `-8`, `-11`, `-12`, and `-14` remain `Unscheduled` or milestone-only (`ARCHITECTURE-SPINE.md:1135-1153`). The spine correctly fails closed, so this is governance incompleteness rather than a High runtime defect. Obtain recorded dates or an authorized bounded deferral; do not invent them in Architecture.

### VC12-M2 — `PostingPending` still has no single duration/configuration authority

AD-5 accepts any stored deadline no shorter than the Conversations posting timeout, while `ARCH-A-14` confirms that the PRD and spine fix no duration, owner, or admissible range (`ARCHITECTURE-SPINE.md:209,1152`). Story 7.4 is correctly blocked on retiring it (`epics.md:2544,2595`), preventing a silent code-local default. Resolve the source/value before that story becomes ready; no Product value is inferred here.

### VC12-M3 — Workflow-start lease acknowledgement is not explicitly mapped to the lease's closed state

The directory vocabulary is only `Active`, `Settled`, or `RevokedBeforeEffect` (`ARCHITECTURE-SPINE.md:189`). AD-7, the sequence, conventions, and the matrix say the winning `WorkflowStart` lease is “acknowledged” at the first durable checkpoint but never state whether that same checkpoint appends `Settled` or leaves the lease active (`ARCHITECTURE-SPINE.md:239,556-563`; `IMPLEMENTATION-CONVENTIONS.md:17`; `launch-readiness-register.md:237,242`). Both remain fail-closed, but one implementation can make every later deletion manifest a long-lived start lease while another closes it immediately. Bind the first checkpoint's exact `Settled` event/directory revision (or explicitly define a different active lifetime) and lost-ack recovery.

## Low

### VC12-L1 — bUnit remains behind the current stable catalog version, correctly classified as delivery debt

The root still pins `bunit` `2.9.0`, while the current Builds checkout and official NuGet feed expose `2.10.3`; the spine reports `2.9.0` and assigns alignment to Story 5.6 (`ARCHITECTURE-SPINE.md:724,1108,1142`; root `Directory.Packages.props`). This is a non-blocking test-stack maintenance item, not an architecture contradiction. Source: [official bUnit guidance](https://bunit.dev/docs/getting-started/create-test-project.html) and [NuGet V3 package index](https://api.nuget.org/v3-flatcontainer/bunit/index.json).

## Required High-Risk Boundary Results

| Boundary | Result |
| --- | --- |
| Conversation intake versus deletion | **FAIL at VC12-H1 only.** Permit creation itself serializes with Closing, but membership remains a pre-permit external effect outside the lease cut and violates FR-8 order. |
| Workflow start | **Pass for deletion safety; Medium closure ambiguity.** A losing lease suppresses start and terminalizes; a winning lease is manifested until an exact outcome. VC12-M3 asks that “acknowledged” map to a named lease state. |
| Context/safety, Provider authorization/invocation/result | **Pass.** Expected-revision lease acquisition precedes authorization; Closing manifests earlier leases; provider return under Closing records only usage/no-use/`Indeterminate`, budget/capacity settlement, and no content; unknown outcome remains active. |
| Proposal mutation and Conversation posting | **Pass.** Mutation/post authorization binds an active lease; `PostingPending` stays active through exact `MessageId` lookup; Effective waits; no deletion workflow invents a post result. |
| Source signal delivery, identity, origin, acknowledgement | **Pass.** Source-atomic durable feed/outbox, stable logical signal, ordered backfill, retry/poison behavior, authenticated target, durable Agents acknowledgement, and the closed no-human `ConversationApprovedDeletion` origin align. |
| AD-7 / AD-13 acceptance sequence | **FAIL at VC12-H1.** Both now agree with each other and the diagram, but agree on an order that contradicts the bound PRD's step 10. |
| Target protection-key alias | **Pass.** Spine, Story 6.1, and `EXT-PROTECTION-1` bind one tenant-scoped target alias to the same interaction DEK; tests require plaintext absence in both owners, cross-tenant substitution rejection, one-key destruction, and `Erased` replay in both streams. |
| Export prepare/commit/key release | **Pass.** Prepare pins lifecycle/store/frozen inventory; the fence is sole commit owner; secondary acknowledgement precedes durable key-delivery authorization; deterministic direct custodian release and exact outcome lookup handle expiry, revocation, retry, and lost acknowledgement without key bytes entering Agents. |
| Export-bearing and export-free deletion | **Pass.** The first export-bearing prepare and all destructive recovery are phase-pinned to lifecycle/store authority; the no-copy branch proves recorded absence and reads/fabricates no export decision; all-copy receipts gate completion. |
| Recorder bootstrap and open decisions | **Pass.** The bootstrap can record only the independently signed exact recorder-scope outcome and cannot choose it, activate a catalog, or approve itself. Other Product/Governance outcomes remain fail-closed. |
| Legacy direct streams/plaintext | **FAIL at VC12-H2.** Finite inventory/cutover is defined, but no authorized nonempty-plaintext disposition exists and Story 6.1 omits its rejection predicate. |
| Matrix/principal coverage | **Pass for runtime operations subject to VC12-H1; dependency chain fails at VC12-H3.** Human actor separation, Workflow allowlists, target scopes, bootstrap variants, and phase-pinned governance recovery are closed. |

## Authoritative Validation-Finding Closure Audit

| Authoritative finding | v12 disposition |
| --- | --- |
| C-1 weaker safety evaluation | **Closed.** AD-20 requires snapshot/current conjunctive evaluation, with only proven semantic dominance allowed to collapse work. |
| C-2 hold/deletion exclusion | **Closed.** One tenant `ProtectionFence`, frozen sets, prepare/arm/destruction separation, conditional export-copy handling, and exact receipts linearize irreversible work. |
| C-3 bootstrap/matrix deadlock | **Closed.** Matrix v4 uses scoped bootstrap/repair variants, direct owner evidence, EventStore self-bootstrap, containment-safe pull, and recorder-scope bootstrap. |
| H-1 scheduled Approver recheck | **Closed.** Durable single-flight scheduling, two-pass empty evidence, state-specific outcomes, and unavailable retry are bound. |
| H-2 safety rescan ownership | **Closed.** Epoch/index owners, finite manifests, fenced coordination, initialization, and `RescanPending` behavior are explicit. |
| H-3 human-only Approvers | **Closed.** Current/historical Parties-owned human/liveness and actor-binding evidence fail closed. |
| H-4 conflated ledgers | **Closed architecturally.** Rate, original-caller open interaction, and monthly budget have distinct owners, decisions, and lifetimes. |
| H-5 human separation identity | **Closed.** Every human principal carries stable `AuthenticatedHumanActorId`; same-actor and subject-set checks do not infer Party equivalence. |
| H-6 proposal-index crash consistency | **Closed.** Interaction truth, same-append outbox, source high-water, fixed-point reconciliation, and recovery are bound. |
| H-7 indivisible Conversations dependency | **Closed.** Six core seams and optional retraction have distinct records/status/consumers. |
| H-8 trusted-envelope security | **Closed.** Canonical authenticated bytes, logical/delivery identities, issuer-wide replay ownership, retention, rotation/revocation, ACLs, and durable denial spooling are bound. |
| H-9 export ownership/lifecycle/signature | **Closed architecturally.** Store/index ownership, AEAD, canonical ES256 manifest, fence/hold/restore policy blockers, purge receipts, and key-delivery recovery are explicit. |
| H-10 current Dapr exposure | **Closed as verified-current truth/debt.** The root-authoritative Builds gitlink pins `1.18.5`; dirty-checkout `1.18.7` is not treated as root authority; Client/ASP.NET are current transitive exposure and Workflow remains future adoption. |
| H-11 overstated shipped public parity | **Closed.** Missing contracts are required completion/delivery debt, not claimed current implementation. |
| H-12 Story 5.1/5.2 tracking conflict | **Closed as surfaced delivery governance debt.** `OD-SPRINT-5.1-5.2-1` does not rewrite history or weaken dependency authority. |

## v10 / v11 / Specialist Critical-High Correction Audit

| Earlier issue | v12 disposition |
| --- | --- |
| v10 incomplete Conversation admission barrier and `PostingPending` convergence | **Closed for permitted work.** Permanent Closing/Effective, finite permit/outbox/lease manifest, state-specific convergence, lookup, acknowledgement, and freeze are explicit. VC12-H1 is the remaining pre-permit membership effect. |
| v10 deletion source delivery/acknowledgement | **Closed.** The source retains and retries its atomic feed entry until exact authenticated Agents acknowledgement. |
| v10 human-shaped source tombstone | **Closed.** `DeletionOrigin` is a structural union and source origin cannot populate human fields. |
| v11 cross-owner effect linearization | **Closed.** Conversation-owned expected-revision effect leases precede start/context/provider/mutation/post authorization, and Effective waits for exact closure. |
| v11 export key authorize/effect/result | **Closed.** Durable authorization, stable delivery id, direct principal-bound custodian effect, exact outcome lookup, and terminal/pending result rules are present. |
| v11 export-bearing deletion gated only at completion | **Closed.** Conditional lifecycle/store authority is pinned at prepare and re-used through destruction/purge/completion; export-free deletion remains independent. |
| v11 legacy directory rollout | **Closed for finite inventory/direct-route cutover, not for actual plaintext disposal.** VC12-H2 is the deeper unresolved nonempty-plaintext outcome and Story 6.1 acceptance mismatch. |

## Verified Repository And Technology Reality

- The parent gitlinks remain authoritative: Builds `a32cb422`, EventStore `ce9e779a`, Conversations `73bcee6f`, Parties `fa423985`, Tenants `2fac1839`, and FrontComposer `053b2008`. The initialized Builds/EventStore/Conversations/FrontComposer working trees are internally clean but at different commits; the spine correctly treats those checkout HEADs as non-authoritative. No submodule was initialized, changed, or cleaned by this review.
- `global.json` pins SDK `10.0.401` with `latestPatch`; SDKs `10.0.302`, `10.0.400`, and `10.0.401` are installed. Root `Directory.Build.props` says `net10.0` and C# `14`; the solution is `.slnx`; root central package overrides match the spine.
- At the parent-authoritative Builds commit, `Dapr.Client`/`Dapr.AspNetCore` are `1.18.5`; EventStore Client/DomainService at the parent gitlink reference those packages, while no Agents project references Dapr Workflow. Official NuGet lists `1.18.7`, so the spine's current-exposure/future-Workflow distinction and `OD-DAPR-SECURITY-1` blocker are accurate. Source: [official NuGet package index](https://api.nuget.org/v3-flatcontainer/dapr.client/index.json).
- Official .NET release metadata lists .NET `10.0.12` and SDK `10.0.401` for 2026-09-08 and includes `CVE-2026-69522`; the SDK floor statement is current and scoped. Sources: [.NET 10.0.12 release notes](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md) and [official releases index](https://github.com/dotnet/core/blob/main/release-notes/releases-index.json).
- The imported/root versions named in Stack exist in their authoritative manifests and official package feeds: MediatR `14.2.0`, FluentValidation `12.1.1`, OpenTelemetry `1.18.0`, Fluent UI Blazor `5.0.0-rc.5-26219.1`, xUnit v3 `3.2.2`/catalog `4.0.0`, NSubstitute `5.3.0`/catalog `6.2.0`, Shouldly `4.3.0`, and bUnit `2.9.0`/current catalog `2.10.3`. No new version contradiction was found.
- Current code contains no interaction directory, effect leases, target key alias, deletion barrier, stable human actor id, or Dapr Workflow reference; the spine consistently labels those target architecture/delivery debt. The raw legacy prompt contract is the exception addressed by VC12-H2: its existence is reported, but the target has no authorized nonempty completion outcome.

## Architecture Defects Versus Implementation Debt

| Item | Classification |
| --- | --- |
| VC12-H1 FR-8 order / unleased membership intake effect | Architecture and cross-artifact conformance defect; correction is technical because Product already decided the order. |
| VC12-H2 legacy plaintext disposition | Unresolved Product/Governance/EventStore decision plus story evidence defect; must be surfaced, not invented. Current raw events are repository reality, not proof that an unsafe migration is allowed. |
| VC12-H3 missing Story 6.1 consumers | Planning/dependency authority defect; update story/register metadata, not implementation. |
| Missing directory, leases, protection, source feed, export store, decision catalog, and governance workflows | Correctly identified delivery debt owned by Stories 5.4-8.4 and external dependencies; not additional findings. |
| Dirty submodule checkout HEADs versus root gitlinks | Correctly distinguished repository reality; not a demand to update submodules. |
| bUnit/test-stack drift | Low delivery maintenance debt. |

## Final Hash And Lint Verification

**PASS for freeze integrity and deterministic lint.** All eight reviewed input hashes exactly matched the supplied frozen values after the report write. A fresh `uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2` returned `ok: true`, `total_findings: 0`, and no severity entries. This mechanical pass does not override the semantic FAIL verdict or the three High findings.
