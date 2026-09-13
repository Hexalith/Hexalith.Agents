# Verified-Current / Cross-Artifact Authority Reviewer Gate — 2026-09-12 v9

**Lens:** complete independent BMad Architecture verified-current, source-chain, bound-artifact, delivery-authority, and repository-reality reviewer gate  
**Target:** frozen current `ARCHITECTURE-SPINE.md` and its bound artifacts after the v8 correction pass  
**Review date:** 2026-09-12  
**Verdict:** **CHANGES REQUIRED** — prior v8 Critical/High findings are closed and the authoritative validation report's three Critical and twelve High architecture corrections remain present. Four newly exposed High cross-artifact contradictions prevent this gate from passing: the rate/concurrency diagram selects an unresolved consumption outcome; Story 7.4 gives the approval event ownership of `PostingPending` and later requires a separate `BeginPosting` commit; FR-30's Conversations-deletion propagation has no owning delivery story; and matrix v4 requires the hold/deletion decision even for the explicitly executable no-hold destruction branch.

**Finding count:** **0 Critical · 4 High · 4 Medium · 0 Low**

## Frozen Snapshot And Review Method

The complete review used the following frozen inputs. A final hash check after this report was written reproduced every digest.

| Input | SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `f232325fb8fd05d040a0872a4b633fd4e2d4d717a3cc698e67fbbc4991d6064f` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |
| `IMPLEMENTATION-CONVENTIONS.md` | `a006a169b1b379df0ed41b3bea75eaf47859bf494fa3c0d6541ed8259d72a38c` |
| `.memlog.md` | `f1716965f57a0923bc2cb56e63dcee74fb272621a4bd8cfb2d5031ee076a4ed1` |
| bound `prd.md` | `cd36ccef836ddc38db09d67c22013c266c86ccb360d34d3fb587ee0bfc8baf52` |
| `epics.md` | `755f908ab43a2c1c533d41c75e294d0c1937c22747d635b188362c567f75ecf6` |
| `external-dependency-register.md` | `97b420ea0e2c915b8757d7be3df853e7fdf55b7234bd6eba0936b01ce6278085` |
| `launch-readiness-register.md` | `5bee06e4303af18e280abe718fcc1b77e3df12d5e44a1360be413ed9e893f2c4` |
| repository instruction source | `bfbe399b567d852a74c7ee8c9217217a3ea5eb80ca591b5b8ac01acfbe838966` |
| root `.gitmodules` | `d0ab19e5734dbe7215a83bf30433f9648088df25745e0fe81306443d79f87a46` |
| root `global.json` | `fc4602f9d88c9440f70f72732343a88c5e4190223fb9b77f9b8ac8eb1c4c8c2d` |
| root `Directory.Build.props` | `9f97e796f7c071fb0511612bd41e56a06e5c622379fe5a742c9472a29fb22ee5` |
| root `Directory.Packages.props` | `4798aa2eec87ac1c5225976967b3530496d436400c8b9c4223392ea056deae27` |
| root `Hexalith.Agents.slnx` | `a13fe1705a583738712eb8d75916cf1d193094bff06d7adbd3090e4914768de2` |

Root authority was commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`. The review read the complete current spine, authoritative validation report, implementation conventions, bound PRD, active delivery map in `epics.md`, both registers, memlog, and repository instructions. It inspected root manifests, solution and relevant source; exact parent gitlink objects rather than dirty checkout HEADs; imported and overridden package versions; cited local-source existence; every dependency record against its story consumers; architecture decision ids/scopes; and target architecture versus current implementation debt. Current technology claims were checked against repository evidence and official .NET, NuGet, Dapr, Microsoft Agent Framework, IETF, and W3C primary sources as applicable. No build, restore, test, external seam, submodule mutation, or reviewed-artifact edit was performed.

## Critical Findings

None.

## High Findings

### VC9-H1 — The happy-path diagram chooses the still-unresolved rate/concurrency rejection-consumption outcome

**Evidence.** PRD OQ-32 deliberately leaves open whether an original call rejected by `MaxConcurrentNonterminalInteractionsPerParty` consumes either rolling-rate window, as well as the joint owner order and FR-25 attribution; it forbids touching either ledger until `OD-RATE-CONCURRENCY-CONSUMPTION-1` is approved (`prd.md:291,770,1086`). AD-13/AD-21, the spine open-decision row, the launch decision record, and Story 6.4 retain that scope (`ARCHITECTURE-SPINE.md:271-276,390-397,997`; `launch-readiness-register.md:98,226`; `epics.md:2059,2100-2107`). The spine sequence diagram nevertheless says that when “any owner rejects,” the workflow aborts **every** prepared rate scope (`ARCHITECTURE-SPINE.md:520-527`). Because `OpenInteractionLedger` is an owner, that branch includes the unresolved concurrency-rejection case and fixes the Product outcome as “consumes no rolling-rate window.”

**Impact.** The architecture has two executable answers. An implementation following the primary diagram can silently ship one Product-owned outcome and metric attribution even though the decision remains Open elsewhere.

**Required correction.** Do not select either consumption outcome. Change the diagram's rejected-owner branch to apply the exact approved DecisionVersion's joint disposition: each rate scope commits or aborts only as that contract records, the open lease aborts as recorded, and the interaction records the matching status/FR-25 attribution. Retain the pre-ledger fail-closed branch while the decision is Open.

### VC9-H2 — Story 7.4 assigns `PostingPending` to the approval event and also to a later separate command

**Evidence.** AD-5 and the sequence contract require `Approved` to commit first, a later deterministic `BeginPosting` to commit `PostingPending`, then and only then the Conversations append; the spine correctly records the current post-before-dispatch/batched-result behavior as implementation debt (`ARCHITECTURE-SPINE.md:204-210,585-596,1019`). Story 7.4 first requires “exactly one approval event” to bind the selected version, approver evidence, timestamp, **and posting-pending state** (`epics.md:2527-2530`). The next acceptance criterion requires a separate `BeginPosting` command to commit `PostingPending` and explicitly forbids approval, posting-pending, and result in one batch (`:2537-2540`). Its negative evidence also expects those boundaries (`:2567-2572`).

**Impact.** The delivery authority contradicts the architecture's authorization-before-effect boundary. A developer cannot satisfy both acceptance criteria and could preserve the unsafe current aggregate batch while claiming the story complete.

**Required correction.** Make the first criterion say the approval event binds the selected version, approver Party/human binding, policy basis, timestamp, and `Approved` state only. Keep `PostingPending` exclusively owned by the subsequent expected-revision `BeginPosting` commit. This is a story reconciliation, not a new Product decision.

### VC9-H3 — Mandatory Conversations-deletion propagation has no owning delivery story or dependency mapping

**Evidence.** PRD FR-30 and §9 require a Conversations deletion signal to trigger approved deletion of derived Agent content and retain only the non-content tombstone (`prd.md:656-686,882,932,957,962`). Spine AD-22 repeats that binding and routes nonterminal interactions through terminalization before deletion (`ARCHITECTURE-SPINE.md:353-357`). `EXT-CONV-AI-1` seam 6 and its compatibility contract include the deletion signal (`external-dependency-register.md:57,60`). Yet its authoritative `ConsumingStories` list omits Story 8.3 (`:63`), and active Story 8.3 has no `EXT-CONV-AI-1` dependency, no FR-30/A-16 deletion-signal acceptance criterion, no owned clause, and no propagation or idempotent-recovery evidence (`epics.md:2903-2970`). Story 8.1 cites FR-30 but only emits retention-eligible work into Story 8.3; Story 8.8 uses Conversations for current inspection access, not deletion propagation.

**Impact.** All active stories can meet their recorded evidence manifests without implementing a mandatory cross-system erasure trigger. That makes the PRD's all-copies/deletion behavior undeliverable and can leave derived Agents content after an approved Conversations deletion.

**Required correction.** Assign the signal to Story 8.3 and add Story 8.3 to `EXT-CONV-AI-1` consumers. Its acceptance/evidence must cover tenant isolation, authenticated/idempotent signal intake, nonterminal terminalization, legal-hold deferral, exact frozen-set/fence progression, crash/lost-ack replay, and restrictive completion. This applies the already settled FR-30/A-16 behavior and invents no Product outcome.

### VC9-H4 — Matrix v4 blocks ordinary no-hold destruction on a decision scoped only to armed hold contention

**Evidence.** AD-22 says `OD-HOLD-DELETION-PRECEDENCE-1` applies only when a hold first contends after `DeletionArmed`; a deletion with no overlapping hold may append `DestructionStarted` without that decision (`ARCHITECTURE-SPINE.md:357,365,994`). The launch decision row likewise limits affected deletion operations to the case “when an overlapping deletion is armed,” and Story 8.3 preserves the conditional branch (`launch-readiness-register.md:95`; `epics.md:2914,2924-2927,2939-2942,2963-2969`). Matrix v4 instead makes `ApprovedHoldDeletionDecisionVersionMatches` an unconditional direct precondition for every `GovernanceProtection:DeletionDestructionStarted` (`launch-readiness-register.md:242`), and the register's post-table prose says the fence cannot append `DestructionStarted` unless it names the approved decision (`:106`).

**Impact.** The executable readiness authority contradicts the settled scope and unnecessarily blocks all no-hold deletion while the unrelated late-hold policy remains Open. Implementations cannot follow both the matrix and AD-22/Story 8.3.

**Required correction.** Make the matrix precondition conditional, for example `NoOverlappingHoldOrApprovedHoldDeletionDecisionVersionMatches`, and qualify the post-table sentence the same way. Preserve the Open decision and do not choose the late-hold outcome.

## Medium Findings

### VC9-M1 — Spine bind metadata and the blocking-decision distillation omit newly binding OQ-33/OQ-34 authority

The spine frontmatter says it binds only `PRD OQ-1..OQ-32` (`ARCHITECTURE-SPINE.md:12-15`), although the current PRD includes binding OQ-33 and OQ-34 and the spine/launch/story contracts rely on both. The `Blocking Open Decisions` table lists nine records but omits `OD-RELEASE-RECORDER-SCOPE-1` (`ARCHITECTURE-SPINE.md:990-1003`), even though AD-17, PRD OQ-33, the launch decision table, and Story 5.5 make it a runtime/delivery/`RQ-1` blocker (`ARCHITECTURE-SPINE.md:323`; `prd.md:1087`; `launch-readiness-register.md:104`; `epics.md:1499-1570`). OQ-34's hold-cancellation decision is present in the table but absent from the frontmatter range. Update the bind range through OQ-34 and add the recorder-scope decision to the summary without selecting a recorder principal.

### VC9-M2 — The closed blocker vocabulary omits the mandated `DecisionCatalogMissing` code

The launch register declares its blocker-code table a closed V1 cross-gate vocabulary and says consumers may not invent a peer code (`launch-readiness-register.md:38-58`). `DecisionCatalogMissing` is not in that table or its emitter table, but the same register and spine require that exact code when the decision catalog/effective authorization is absent (`launch-readiness-register.md:91`; `ARCHITECTURE-SPINE.md:323`), and Story 5.5 tests it (`epics.md:1526`). Add the code and emitter semantics to the closed vocabulary, or consistently map the condition to an existing code. The current unknown-code rule fails closed, so this is a public-contract completeness issue rather than an unsafe bypass.

### VC9-M3 — Two declared spine sources remain absent

The spine cites `reviews/review-2026-09-12-security-data-integrity-v3.md` and `reviews/review-2026-09-12-brownfield-drift-v3.md` (`ARCHITECTURE-SPINE.md:82-83`), but neither file exists in the frozen tree. Every other declared local source and all conventions links resolve. Generate those exact reviews or remove/replace the citations. This carries forward VC8-M1.

### VC9-M4 — Story 8.1's prose makes the export store unconditionally mandatory although its manifest correctly scopes it to committed artifacts

Epic 8 topology, AD-22, the launch matrix, and Story 8.1's evidence manifest require `EXT-EXPORT-STORE-1` only when the frozen hold set contains committed export artifacts (`epics.md:2763,2825-2831`; `ARCHITECTURE-SPINE.md:361`; `launch-readiness-register.md:227,229-232`). Story 8.1's external dependency prose instead says the protection, secrets, Parties, **and export-store** records must all be `Available`, then conditions only the decision version (`epics.md:2773-2777`). Qualify both the store and decision on committed-artifact overlap. The overconstraint fails closed and does not invent unsafe behavior, so it is Medium.

## Low Findings

None.

## v8 Correction Audit

| v8 item | Frozen-current evidence | Disposition |
| --- | --- | --- |
| VC8-H1 OQ-23 accidentally blocking global `RQ-1` | FR-28 now derives the dependency profile from exercised requirements/variants/Product branches and makes `EXT-CONV-RETRACTION-1` conditional; item 9 explicitly excludes A-28 from `RQ-1`; item 10 scopes the OQ-23 decision to `AutomaticModeEnablementEligibility` (`prd.md:727,730-731`). A-26 separates its non-OQ-23 `RQ-1` retirement from the Automatic-only branch, A-28 is Automatic-only, and OQ-22/OQ-24 repeat the same scopes (`:942,944,1076-1078`). Spine, register, and epics agree. | **Closed.** Product's three options remain unresolved; no outcome was invented. |
| VC8-M1 absent sources | The two named review files still do not exist. | **Open as VC9-M3.** |

## Authoritative Validation-Finding Closure Audit

The authoritative report's original three Critical and twelve High findings remain corrected in the target architecture. The four v9 High findings are later cross-artifact handoff defects; none requires reverting those corrections or deciding an unresolved Product question.

| Finding | Current disposition and evidence direction |
| --- | --- |
| C-1 | **Closed.** AD-20 requires both snapshot and current safety/key evidence; no comparable ordering is invented. |
| C-2 | **Closed.** AD-22 owns the tenant `ProtectionFence`, complete frozen sets, prepare/arm/destruction boundaries, legal-hold deferral, and restrictive recovery. VC9-H4 corrects a matrix scope contradiction, not the fence model. |
| C-3 | **Closed.** AD-17/matrix v4 use typed scope and closed direct bootstrap preconditions; unrelated blockers remain mandatory. |
| H-1 | **Closed.** AD-8 owns durable scheduled expiry and single-flight recheck. |
| H-2 | **Closed.** AD-20 owns durable safety epoch/index, finite manifests, bounded fenced rescan, recovery, and `RescanPending`; delivery remains Stories 6.3/8.4. |
| H-3 | **Closed.** AD-8 plus `EXT-PARTIES-1` require current human/liveness and historical Party-to-actor binding. |
| H-4 | **Closed architecturally.** AD-2/AD-21 split the three ledgers and Story 6.4 owns implementation. VC9-H1 removes a Product-outcome leak from the diagram. |
| H-5 | **Closed.** Stable `AuthenticatedHumanActorId` drives human separation across principal kinds. |
| H-6 | **Closed.** AD-7 owns source-revision proposal materialization, acknowledgement/high-water, and removal reconciliation. |
| H-7 | **Closed.** Core `EXT-CONV-AI-1` and optional `EXT-CONV-RETRACTION-1` remain split with Automatic-only consumption. VC9-H3 concerns the mandatory core deletion signal's omitted Story 8.3 consumer, not optional retraction recoupling. |
| H-8 | **Closed.** AD-29/AD-30 define canonical HMAC, stable logical identity, per-delivery nonce, replay-first registration, expiry/retention, rotation, and revocation. |
| H-9 | **Closed architecturally.** AD-22 binds the export store/index/manifest/fence/cleanup/all-copies invariant while leaving provider and lifecycle choice Open; VC9-H3 repairs its delivery ownership. |
| H-10 | **Closed.** Current transitive Dapr Client/ASP.NET `1.18.5`, dirty-checkout `1.18.7`, and future Workflow adoption are distinguished; release is blocked on `OD-DAPR-SECURITY-1`. |
| H-11 | **Closed.** Required-completion parity and the debt table distinguish target rules from current implementation. |
| H-12 | **Closed.** `OD-SPRINT-5.1-5.2-1` keeps the tracker/evidence contradiction as delivery-history debt, not architecture truth. |

## Cross-Artifact Authority, Open Decisions, And External Consumers

The PRD remains Product authority; the external register remains commitment/consumer authority; the launch register remains executable gate authority; and the active Epics 5-8 map remains delivery authority. All twelve dependency records are `Uncommitted`, so every listed consumer is truthfully backlog/blocked and `RQ-1` is NOT READY. No active story treats a committed or available seam as shipped.

The current decision catalog contains all ten stable ids found across the spine and register: hold/deletion precedence, hold-prepare cancellation, export lifecycle, rate/concurrency consumption, Dapr security disposition, sprint-history disposition, PRD OQ-18 historical safety, PRD OQ-23 Automatic retraction, PRD OQ-31 instruction protection, and release-recorder scope. Their Product/governance outcomes remain Open and their runtime affected scopes are generally consistent. VC9-H1 and VC9-H4 are the two operation-scope exceptions; VC9-M1 is the summary/bind omission. Exact AD identifiers remain preserved and sequential: one occurrence each of AD-1 through AD-31, with no missing, duplicate, or renumbered id.

Register-to-story consumer ownership was confirmed for the current list except VC9-H3. In particular:

| Dependency | Current consumer disposition |
| --- | --- |
| `EXT-CONV-AI-1` | Existing 5.4, 6.1-6.3, 6.6, 6.8, 7.1-7.5, 7.7, 8.5, and 8.8 mappings are present; mandatory deletion-signal consumer 8.3 is missing (VC9-H3). |
| `EXT-CONV-RETRACTION-1` | Correctly has no consumer until Product chooses an Automatic-mode OQ-23 branch. |
| `EXT-CONV-UI-1` | Story 6.7 owns all four artifact kinds and focus/status behavior. |
| `EXT-PARTIES-1` | Direct and conditional human/identity consumers, including Story 5.5 recorder and Story 8.4 Release-Operator branches, are explicitly scoped and blocked. |
| `EXT-HOST-1`, `EXT-PROVIDER-1`, `EXT-SAFETY-1`, `EXT-TOKEN-1`, `EXT-SECRETS-1`, `EXT-PROTECTION-1`, `EXT-TOPOLOGY-1` | Register consumers, active-story dependencies/evidence, and blocked results reconcile. |
| `EXT-EXPORT-STORE-1` | Story 8.2 is unconditional; Stories 8.1/8.3 are conditional on export-bearing frozen sets. VC9-M4 is one overbroad Story 8.1 prose sentence; its topology/manifest/result use the intended conditional scope. |

## Technology, Gitlink, Package, And Repository-Reality Verification

| Claim | Parent-authoritative / official evidence | Result |
| --- | --- | --- |
| Root SDK/language/build substrate | Root manifests and installed CLI agree on SDK `10.0.401`, `latestPatch`, `net10.0`, C# 14, `.slnx`, and Central Package Management. The official [.NET 10.0.12 release](https://github.com/dotnet/core/blob/main/release-notes/10.0/10.0.12/10.0.12.md) lists SDK `10.0.401`/`10.0.112` and CVE-2026-69522. | **Confirmed.** |
| Root-authoritative submodules | Exact gitlinks are AI.Tools `5f93d2ec`, Builds `a32cb422`, Commons `6da79aed`, Conversations `73bcee6f`, EventStore `ce9e779a`, FrontComposer `053b2008`, Memories `3644ef63`, Parties `fa423985`, PolymorphicSerializations `8aeed1d2`, and Tenants `2fac1839`. | **Confirmed.** |
| Dirty checkout is not authority | Checkout HEADs differ for Builds `cf52f74c`, Conversations `64b05083`, EventStore `a568af4e`, FrontComposer `1b3608c9`, and Memories `42dfa26b`; the other initialized root-declared submodules match. The spine uses exact parent gitlinks and labels dirty targets separately. | **Confirmed.** |
| Dapr exposure/version | Exact EventStore Client/DomainService projects reference Dapr Client/ASP.NET; Agents references those surfaces and calls `AddDaprClient()`. Exact Builds pins Dapr `1.18.5`; dirty Builds pins `1.18.7`; no Agents project references Workflow. Official NuGet indexes contain stable `1.18.7` for Client, ASP.NET, and Workflow. | **Confirmed.** Current exposure, dirty candidate, and future Workflow are separated. |
| Agent/hosting selection | No Agents manifest or source consumes Microsoft Agent Framework, Dapr Agents, Aspire AppHost, ServiceDefaults, or a selected Provider SDK. The spine records current ecosystem maturity but leaves selection under `EXT-PROVIDER-1`/`EXT-HOST-1`. | **Confirmed unselected.** |
| Catalog/root package truth | Exact Builds and root overrides match every named Dapr, MediatR, FluentValidation, OpenTelemetry, Fluent UI, test SDK, xUnit/runner, NSubstitute, Shouldly, and bunit version in the stack table. Dirty Builds advances Dapr/test SDK/bunit but is not root authority. | **Confirmed.** |
| Current source/delivery state | The solution has six source and five test projects; `eng/verify-story-5.1.ps1` is absent. No current `RateLimitLedger`, `OpenInteractionLedger`, `BudgetLedger`, decision catalog/record, safety epoch/index, protection fence/export store, trusted-envelope replay store, or security-audit spool exists. Current approval code performs the Conversations append before EventStore dispatch and batches approval/posting states as the debt table says. | **Confirmed as implementation debt.** |

No current version, package exposure, exact gitlink, project-shape, or current-source claim produced a Critical or High finding.

## Architecture Contract Versus Implementation Debt

The spine consistently labels AD-1 through AD-31, matrix v4, and approved-decision-dependent branch rules as target architecture. Its debt table accurately names current absences and assigns them to active stories/dependencies without claiming those targets are shipped. Current Dapr Client/ASP.NET exposure is correctly current, Workflow adoption correctly future, and the root-vs-dirty catalog distinction is exact. The Story 5.1/5.2 tracker/verifier inconsistency remains delivery history under its own Open decision. VC9-H1/H4 are target-contract contradictions, VC9-H2/H3 are delivery-authority omissions against settled target/PRD rules, VC9-M1/M2/M3 are distillation/schema/source-chain defects, and VC9-M4 is a fail-closed delivery overconstraint; none should be disguised as ordinary implementation debt.

## Linter And Source Resolution

The mandatory architecture linter completed against the frozen workspace before semantic disposition and again after this report was written:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
ok: true
total_findings: 0
```

All declared local sources and implementation-convention links resolve except the two v3 review files in VC9-M3. External standards/product links used by the spine resolve to the named primary authorities.

## Gate Conclusion

The frozen artifacts fail the complete v9 reviewer gate with **0 Critical, 4 High, 4 Medium, and 0 Low findings**. Correct VC9-H1 through VC9-H4 without choosing any unresolved Product/governance outcome, reconcile the four Medium defects, re-distill the spine where its bind/source/diagram summaries change, rerun lint, and rerun the complete frozen reviewer gate. A PASS requires zero Critical and zero High findings.
