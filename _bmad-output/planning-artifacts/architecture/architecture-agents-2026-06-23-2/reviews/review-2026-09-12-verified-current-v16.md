---
name: Hexalith Agents verified-current and PRD-conformance review v16
type: architecture-spine-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
intent: review-read-only
lens: verified-current / bound-PRD-conformance / cross-artifact authority
verdict: fail
critical: 0
high: 2
medium: 3
low: 1
lint_ok: true
---

# Verified-Current / PRD-Conformance Reviewer Gate v16

## Gate Verdict

**FAIL — 0 Critical, 2 High, 3 Medium, 1 Low.** The v16 candidate closes every v15 Critical and High finding: both deletion origins now share a canonical admission/effect cut, migration repair has an atomic boundary and complete cohort, pre-Provider capacity is recoverable, operator cancellation is decision-gated with exact cleanup/removal/release ordering, and initial-output-safety recovery is phase-pinned. Two fresh High contradictions remain. AD-12 literally requires call-permit registration to reserve and commit a lease even though the permit is the prerequisite and binding authority for every lease. Separately, the same exact-Conversation governance scope has inconsistent implicit temporal meanings: a hold freezes only the current set, while a completed operator deletion leaves permanent fences against every future matching call/content append, although the PRD does not decide either prospective-versus-point-in-time Product outcome.

## Frozen Scope And Method

I reviewed the complete frozen `ARCHITECTURE-SPINE.md`, `IMPLEMENTATION-CONVENTIONS.md`, authoritative `VALIDATION-REPORT-2026-09-12.md`, bound PRD, `epics.md`, both registers, repository instructions, root manifests, root-authoritative gitlinks, relevant parent-authoritative and checked-out source, and declared source-chain existence. Per the task constraint, the architecture memlog was **hash-verified only** and was not read or modified. I re-walked all original three Critical and twelve High findings, every v15 Critical/High correction, Product-fixed FR-8 ordering, permit/lease/effect sequences, both deletion origins, governance scope, migration/repair, external consumers, story metadata, open decisions, assumptions, and target-versus-shipped truth. I did not select any unresolved Product, Governance, or Security outcome.

The supplied SHA-256 values matched at intake and at final verification:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `33abd2ca65575b3c73b95827a0b2994cf7f1e909bcdd48a9a93f22403b4476f2` |
| `IMPLEMENTATION-CONVENTIONS.md` | `f30858efcfdb0439bcd3c2a92073061b8cecec35469c85f5503dbf32aeea160a` |
| architecture `.memlog.md` | `f47127fbac2f66bea528939c3fc55433487b9a0f539ab602956833324fe9240d` |
| bound `prd.md` | `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb` |
| `epics.md` | `e137a160dc9344d4dd2a5e5f1e8bd176ec81f5b721ee7fdcf0ab55c693040c8d` |
| `external-dependency-register.md` | `0a780c4c9cf77e8e9d1cf26f7003997e5e7f8a8fa7da61698299470ff7c8eb46` |
| `launch-readiness-register.md` | `7f5316db6d320ebd3f4b1e3eace38e7892c83533bb7b727fd3f4f2cbdf9f1cae` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

The deterministic spine linter returned `ok: true`, `total_findings: 0`, with no severity entries. AD IDs are unique and contiguous from AD-1 through AD-31.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 2 |
| Medium | 3 |
| Low | 1 |

## High

### VC16-H1 — AD-12 makes interaction-permit registration depend circularly on a permit-bound effect lease

**Classification:** target executable-ordering contradiction; architecture wording defect, not implementation debt and not a Product choice.

The detailed intake contract correctly makes `RegisterInteractionPermit` the first durable directory append after pre-creation checks and before any workflow, rate/open/Budget/capacity preparation, protected read, target mutation, or external effect (`ARCHITECTURE-SPINE.md:280`). AD-2 says every `ConversationEffectLease` is bound to one permit (`:205`), AD-13 says registration is directory-first and the first lease is the later `WorkflowStart` lease (`:353`), conventions say “permit first” (`IMPLEMENTATION-CONVENTIONS.md:17`), and matrix v4 gives `RegisterInteractionPermit` its own unleased conditional append while `ConversationEffectLease:Acquire` requires the exact permit (`launch-readiness-register.md:251-252`).

AD-12 nevertheless says “Call permit registration and every workflow-start ... phase first reserves and then conditionally commits its deterministic `ConversationEffectLease`” (`ARCHITECTURE-SPINE.md:336`). Read literally, the permit cannot be registered until a lease is committed, while no lease can be acquired or bound until that permit exists. One conforming unit deadlocks every call; another ignores the normative AD-12 sentence and follows AD-2/AD-13/matrix. The contradiction also makes deletion admission ordering untestable because the scope admission fence explicitly serializes permit registration separately from lease acquire/commit.

**Required correction — architecture wording only.** Remove call-permit registration from AD-12's reserve/commit list. State that the permit is the directory-first, EventStore-serialized intake append governed by the migration capability and deletion admission fence; only phases **after** the permit reserve/commit their deterministic permit-bound leases. Keep AD-2, AD-13, conventions, matrix `RegisterInteractionPermit`, and Story 6.1 on that same order. No Product decision is required.

### VC16-H2 — Exact-Conversation governance scope has an unresolved temporal Product meaning but is declared executable

**Classification:** unresolved Product + Governance + Security semantics currently hidden inside architecture mechanics; not current implementation debt.

`GovernanceScopeV1:ExactConversation(TenantId, ConversationId)` is declared executable for every hold, export, and deletion, while only `ClassUtcRange` is blocked on a scope-semantics decision (`ARCHITECTURE-SPINE.md:201-203`; `IMPLEMENTATION-CONVENTIONS.md:21`; `launch-readiness-register.md:107,267,273,283`). The implementation consequences give this one scope two materially different implicit temporal meanings:

- `ApplyHold` resolves and freezes the explicit interaction/export set at a checkpoint (`ARCHITECTURE-SPINE.md:444`). A later interaction created in the same still-live Conversation is not part of that frozen set and is not prospectively pinned.
- Either deletion origin installs an admission fence that rejects every later matching permit/action-intent/lease acquire/commit and a content fence that rejects every later matching interaction/failure create or append (`ARCHITECTURE-SPINE.md:209,448`; `launch-readiness-register.md:283-284,302-304`). Successful deletion completion requires both fences still installed (`launch-readiness-register.md:319`); only a separately approved pre-destruction operator Abort can remove them (`:312-316`). Therefore a successful operator-origin exact-Conversation deletion is also a permanent future-processing ban for that Conversation.

The PRD authorizes legal hold and approved deletion and fixes Conversation-origin deletion of derived content (`prd.md:656-665,955-962`), but it does not say that a human operator's exact-Conversation deletion permanently disables future Agent use, nor whether an exact-Conversation legal hold covers only the checkpoint set or also future derived content. The human-approved predicate records *which Conversation* but no point-in-time/prospective mode, cut instant, reopening rule, or public consequence. Story 8.3 repeats the persistent-fence behavior (`epics.md:2976-2985`) and therefore cannot serve as independent Product authority for it.

Two implementations can both follow the name `ExactConversation` while producing incompatible and legally significant behavior: point-in-time hold/deletion over the current set, or prospective hold/permanent processing prohibition. Treating a later interaction as outside a hold may lose evidence; treating an operator deletion as a permanent ban may disable an active Conversation without Product authority. Architecture must not choose between them.

**Required correction — surface the decision.** Add a versioned Product + Governance + Security decision for the temporal meaning of human-origin exact-Conversation hold, export, and deletion scopes: point-in-time versus prospective membership, the authoritative cut instant, later-content treatment, fence lifetime/removal or explicit permanent-ban outcome, and public/audit wording. While Open, block only the ambiguous human-origin exact-Conversation variants; exact-interaction scopes remain evaluable, and PRD-fixed `ConversationApprovedDeletion` can retain its source-deletion-specific permanent cut. If Product instead approves one common semantic, bind that decision version into the request, predicate, hold/export frozen set or deletion fences, recovery, and status. Do not infer the outcome from EventStore mechanics.

## Medium

### VC16-M1 — Architecture-owned `RQ-1` assumptions still lack literal target retirement dates

PRD FR-28 requires every Architecture-owned `RQ-1` row to carry a literal target retirement date; a milestone or `TBD` blocks on that ground (`prd.md:730,898`). The spine instead marks numerous Architecture-owned/co-owned rows “Unscheduled,” including ARCH-A-1, A-2, A-3, A-4's open test-stack sub-item, A-6, A-7, A-8, A-11, A-12, and A-14 (`ARCHITECTURE-SPINE.md:1281-1294`). The spine correctly treats them as blockers (`:1277`), so no unsafe readiness is authorized, but the authoritative register is still structurally nonconformant. Record literal dates through the named owners; do not invent them in Architecture.

### VC16-M2 — `PostingPending` recovery still has no single concrete timeout authority

AD-5 permits any stored timeout no shorter than the Conversations seam-2 posting timeout and explicitly leaves the concrete duration unfixed (`ARCHITECTURE-SPINE.md:239`). ARCH-A-14 acknowledges the gap and is unscheduled (`:1294`); Story 7.5 remains blocked on retiring it (`epics.md:2558,2614`). This safely prevents ready-for-dev but still permits incompatible workflow/recovery timing if implemented early. Architecture should select one configuration authority/snapshot rule or defer exactly to the committed seam value, then obtain Product confirmation before that story proceeds.

### VC16-M3 — `epics.md` is normative for the spine but absent from frontmatter sources

The spine says its story numbering follows the replacement Epics 5–8 in `epics.md` and repeatedly relies on story assignments to distinguish target architecture from delivery debt (`ARCHITECTURE-SPINE.md:1271,1232-1248`). The frontmatter `sources:` list does not include `../../epics.md`. This leaves a real authoritative input outside the declared source chain even though v16 correctly reconciles Story 5.5, 5.8, 6.1, and 8.3. Add that relative source path; this is document traceability only and does not change any story status or Product decision.

## Low

### VC16-L1 — bUnit `2.9.0` is valid but behind stable `2.10.3`

Root/package/project evidence and the Stack agree on bUnit `2.9.0`; the official NuGet version index lists stable `2.10.3`. No security or compatibility defect was established. Align during the focused Story 5.6 test-stack work or record the chosen retention. Source: [official bUnit package version index](https://api.nuget.org/v3-flatcontainer/bunit/index.json).

## v15 Critical/High Closure Audit

| v15 finding | v16 disposition |
| --- | --- |
| VC15-C1 operator deletion omitted directory/effect cut | **Closed.** Both origins install the same scope admission fence, freeze finite per-owner Closing manifests, converge every committed effect/ledger/capacity/posting outcome, and require a global Effective receipt before candidate acceptance (`ARCHITECTURE-SPINE.md:209-211`; matrix `:283-300`). |
| VC15-C2 canonical scope predicate absent | **Closed for identity/membership mechanics.** Versioned exact-interaction/exact-Conversation values, digest, immutable owner proof, and fail-closed EventStore evaluation are now explicit (`ARCHITECTURE-SPINE.md:201-203`; `external-dependency-register.md:129`). VC16-H2 is a different Product question about temporal meaning, not predicate computability. |
| VC15-H1 operator Abort unauthorized/stranded fences | **Closed.** `OD-OPERATOR-DELETION-CANCELLATION-1` blocks Abort; any approved permit path requires cleanup, exact two-fence removal, per-owner cut release, then terminal Abort (`ARCHITECTURE-SPINE.md:460,524,1215`; matrix `:309-316`). |
| VC15-H2 migration repair omitted User-action intents | **Closed.** Repair fence, frozen cohort, bridge, drain, and successor explicitly include protected User-action intents (`ARCHITECTURE-SPINE.md:215-217`). |
| VC15-H3 Closing could not release pre-Provider capacity | **Closed.** Owner manifests and recovery distinguish pre-Provider capacity cancellation from committed-Provider capacity settlement (`launch-readiness-register.md:289,294-297`; `epics.md:3006-3007`). |
| VC15-H4 repair lacked an atomic directory boundary | **Closed.** `InstallDirectoryRepairFence` atomically revokes ordinary directory capability and returns the checkpoint before cohort freeze; stale writers append nothing (`ARCHITECTURE-SPINE.md:215-217`; `external-dependency-register.md:129`). |
| VC15-H5 Provider status decision not phase-pinned | **Closed.** Provider authorization binds the effective `OD-INITIAL-OUTPUT-SAFETY-STATUS-1` version/outcome, and result/recovery must use that immutable authorization. Current `SafetyFailed` code remains explicitly classified as non-authoritative debt (`ARCHITECTURE-SPINE.md:1246`). |

## Authoritative Validation-Finding Closure Audit

| Authoritative finding | v16 disposition |
| --- | --- |
| C-1 weaker safety evaluation | **Closed.** Snapshot/current policies are conjunctive; a one-pass collapse requires machine-checkable semantic dominance. |
| C-2 hold/deletion exclusion | **Closed for all-copy fence/linearization mechanics.** Shared tenant fence, accepted immutable set, both-origin directory/effect cut, content fence, containment, and receipts prevent partial erasure. VC16-H2 separately blocks an unapproved temporal scope meaning. |
| C-3 bootstrap/matrix deadlock | **Closed.** Target-aware bootstrap, containment, security-recorder, migration/repair, and recovery rows avoid circular readiness. VC16-H1 is a different newly introduced prose contradiction inside call intake. |
| H-1 scheduled Approver recheck | **Closed.** Durable cadence/single-flight owner, two-pass empty evidence, phase lease, state-specific result, and unavailable retry are bound. |
| H-2 safety rescan ownership | **Closed.** Durable epoch/index owners, finite manifest, fenced coordinator, bootstrap, worker bounds, and `RescanPending` are explicit. |
| H-3 human-only Approvers | **Closed.** Parties-owned human type/liveness and stable actor binding fail closed. |
| H-4 conflated ledgers | **Closed.** Rolling-rate, caller-open, monthly Budget, and shared-capacity lifetimes have separate owners and settlement rules. |
| H-5 human separation identity | **Closed.** Stable `AuthenticatedHumanActorId` crosses Party-bearing and Party-free human principals. |
| H-6 proposal-index crash consistency | **Closed.** Interaction truth, same-append outbox, directory high-water, repair, and exact acknowledgement are defined. |
| H-7 indivisible Conversations dependency | **Closed.** Six mandatory `EXT-CONV-AI-1` seams and optional `EXT-CONV-RETRACTION-1` have distinct commitment/consumer authority. |
| H-8 trusted-envelope security | **Closed.** Canonical MAC bytes, audience/time/key lifecycle, platform-wide issuer nonce replay owner, constant-time validation, and safe denial spool are bound. |
| H-9 export ownership/lifecycle/signature | **Closed architecturally.** Immutable encrypted artifact/index ownership, fence commit, ES256 canonical manifest, phase-pinned lifecycle/store, principal-bound key delivery, and all-copy cleanup are explicit; policy outcomes remain correctly Open. |
| H-10 current Dapr exposure | **Closed as verified-current truth/debt.** Parent-authoritative Client/ASP.NET `1.18.5`, non-authoritative checkout `1.18.7`, and future Workflow adoption are distinguished. |
| H-11 overstated shipped parity | **Closed.** Required target contracts and absent current implementation are separated in the delivery-debt table, including the current unapproved `SafetyFailed` choice. |
| H-12 tracking/dependency conflict | **Closed as surfaced governance debt.** External/register/evidence authority overrides historical sprint labels; no uncommitted seam is presented as ready. |

## Requested Reconciliation Audit

| Focus | Result |
| --- | --- |
| Bound PRD and Product-fixed FR-8 order | **Fail only at VC16-H1.** The intended detailed order is correct: directory intake, joint rate/open step 5, Confirmation-only Approver resolution step 6, Context step 7, descriptor/Budget step 8, safety step 9, membership step 10, then acceptance. AD-12's permit-lease sentence contradicts the otherwise consistent executable path. |
| Both deletion origins / all-copy completion | **Pass mechanically.** Shared admission fence, finite owner cut, accepted inventory, content fence, violation containment, export/copy pinning, destruction verification, and completion receipts cover concurrent intake/provider/posting. **Product-scope semantics fail at VC16-H2.** |
| Source delivery, identity, and origin | **Pass.** The Conversations signal has durable ordered delivery/acknowledgement, stable logical identity, authenticated source revision/approval, target-limited workflow authority, one deterministic deletion id, and origin-union separation (`epics.md:2997-3001`; `external-dependency-register.md:63-65`). |
| Migration and legacy writes | **Pass.** Initial migration and repair revoke legacy/directory capabilities at EventStore, preserve active deletion fences, drain frozen authorized work, and block nonempty plaintext on `OD-LEGACY-PLAINTEXT-DISPOSITION-1`. |
| Export and deletion lifecycle | **Pass.** Fence commit precedes export acknowledgement/key release; recovery is phase-pinned; export-free deletion uses exact no-copy proof; export-bearing deletion binds lifecycle/store through completion. |
| Story/dependency/evidence ownership | **Pass semantically; source-chain Medium at VC16-M3.** Story 5.5/5.8/6.1/8.3 metadata and external consumers match target ownership and current `Uncommitted`/blocked states. |
| Unresolved Product decisions | **Fail at VC16-H2; otherwise pass.** Class/range scope, operator cancellation, initial-output status, legacy plaintext, instruction protection, export lifecycle, hold/deletion precedence, hold cancellation, rate/concurrency, Dapr security, and other named choices are surfaced and fail closed without invented outcomes. |
| Architecture versus implementation debt | **Pass.** Missing directory/leases/migration/deletion/export/workflows/contracts are delivery debt against explicit target ADs. Current direct/plaintext/no-op/post-before-dispatch/`SafetyFailed` behavior is current repository reality, not Product or architecture authority. |

## Verified Repository And Technology Reality

- Root-authoritative gitlinks remain Builds `a32cb422`, Conversations `73bcee6f`, EventStore `ce9e779a`, FrontComposer `053b2008`, Memories `3644ef63`, Parties `fa423985`, and Tenants `2fac1839`. Initialized Builds `cf52f74`, Conversations `64b05083`, EventStore `a568af4`, FrontComposer `1b3608c`, and Memories `42dfa26` checkout HEADs differ from parent authority; Parties/Tenants match. Inspected submodule worktrees were internally clean. This review did not initialize, edit, clean, or advance a submodule.
- Root `global.json` pins SDK `10.0.401` with `latestPatch`; the installed SDK reports `10.0.401`. Root props target `net10.0` and C# 14, use `.slnx`, and enable Central Package Management. Official .NET 10 metadata lists SDK `10.0.401` and runtime `10.0.12`: [official .NET 10 release metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json).
- The root-authoritative Builds catalog pins Dapr Client/ASP.NET/Workflow `1.18.5`; parent-authoritative EventStore Client/DomainService consume Client/ASP.NET. The non-authoritative Builds checkout pins `1.18.7`; official NuGet lists stable `1.18.7` for [Dapr.Client](https://api.nuget.org/v3-flatcontainer/dapr.client/index.json), [Dapr.AspNetCore](https://api.nuget.org/v3-flatcontainer/dapr.aspnetcore/index.json), and [Dapr.Workflow](https://api.nuget.org/v3-flatcontainer/dapr.workflow/index.json). Agents consumes EventStore Client/DomainService transitively and has no direct Workflow reference. The Stack and ARCH-A-15 state this accurately.
- Current code still persists a raw `Prompt`, relies on the EventStore no-op/unprotected default, posts in approval orchestration before durable command dispatch, and has no `ConversationAgentState`, effect leases, migration fences, canonical governance scope, target governance aggregates, or Dapr Workflow owner. Current output-safety maps to `SafetyFailed`. The v16 delivery-debt table explicitly labels each relevant shipped behavior/deferred target, so none is misreported as a completed architecture feature.
- The spine frontmatter declared 112 sources at final review time, 98 local. Rubric-v16 and adversarial-v16 existed; this report resolves its own anticipated path. The concurrently anticipated security-v5 and brownfield-v5 paths were not frozen source inputs and were the only other declared local paths absent at report-write time. All non-concurrent declared local sources resolved. `epics.md` exists and was reviewed, but VC16-M3 records that it is not declared in frontmatter.

No build/test success is claimed: this reviewer remained read-only apart from this report, and a build could create generated workspace output. Deterministic architecture lint was run instead as required.

## Architecture Defects Versus Implementation Debt

| Item | Classification |
| --- | --- |
| VC16-H1 | Target normative ordering contradiction; fix the spine wording, not runtime code. |
| VC16-H2 | Unresolved Product/Governance/Security decision; surface and block its ambiguous variants rather than invent a temporal scope. |
| VC16-M1/M2 | Explicit fail-closed planning inputs requiring owner-approved dates/timeout, not authorization to implement defaults. |
| VC16-M3 | Architecture-document source traceability defect. |
| Missing directory/effect leases, migration/repair, protected payload, governance fences/export, workflows, and expanded contracts | Correctly recorded implementation/external delivery debt assigned to stories and register commitments. |
| Current plaintext, no-op protection, direct streams, post-before-dispatch approval, and `SafetyFailed` selection | Correctly recorded brownfield behavior/debt, never target authority. |
| Modified checkout HEADs | Correctly separated from exact root-authoritative gitlinks; no parent or submodule update is implied. |
| bUnit lag | Low build/test maintenance debt. |

## Final Hash, Source, And Lint Verification

**PASS for freeze integrity and deterministic lint; FAIL for the semantic reviewer gate.** All eight supplied input hashes matched after this report was written. `uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2` returned `ok: true`, `total_findings: 0`, and no severity entries. AD-1 through AD-31 are unique and contiguous. Source-chain existence was checked as described above. Mechanical correctness does not override VC16-H1 or VC16-H2.
