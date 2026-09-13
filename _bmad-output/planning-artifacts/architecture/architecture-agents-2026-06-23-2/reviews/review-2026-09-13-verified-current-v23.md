---
review: verified-current-v23
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-13
intent: read-only-review
lens: primary verified-current / bound-PRD reconciliation
verdict: PASS
critical: 0
high: 0
medium: 2
low: 2
lint_ok: true
---

# Primary Verified-Current / Bound-PRD Reviewer Gate — v23

## Verdict

**PASS — 0 Critical, 0 High, 2 Medium, and 2 Low.** The frozen v23 package closes all four v22 High findings. The three pre-command security primitives now have one closed inventory; accepted-batch signing uses a stable pre-seal destruction identity while committed guard revisions remain result evidence; protection-owner activation atomically arbitrates compromise of the replacement key; and Story 8.3 carries the unresolved hold decision through every post-start branch. The original authoritative Critical and High findings remain closed.

No finding asks Architecture to choose an unresolved Product outcome. The remaining Medium findings are already fail-closed governance/implementation inputs, and the Low findings are package maintenance and a current-matrix label defect. Missing target mechanisms remain explicitly classified as delivery debt rather than current implementation or permission to weaken the architecture.

## Frozen Snapshot, Instructions, And Method

I read the complete repository instruction source and the complete BMad architecture reviewer-gate instructions before review. I then reviewed the frozen spine and implementation conventions, authoritative validation report, bound PRD, Epics delivery map, external-dependency register, launch-readiness register, root manifests, parent-authoritative gitlinks, relevant current source, current package catalogs, and official primary release/package indexes. Per the task constraint, the architecture `.memlog.md` was **not read**; only its SHA-256 was recomputed.

The reviewed inputs matched the supplied hashes before this report was written:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `b382475bf559f9d6262273ca498df16197919fead492fd72f4aa80ac5b16c1d4` |
| `IMPLEMENTATION-CONVENTIONS.md` | `ea1d311da8bef578c7fca428d6b66ef7ae91dc593eb5b3efc9cac382e2a938d9` |
| architecture `.memlog.md` — hash only, content unopened | `890d18b355b6b7d475241426641701ebe724dd6906bed15fa180a5e14a0e17e0` |
| bound `prd.md` | `4fbc21e13301bb47c87e077ec7aa884ff698121837b0e19cbf6e0ea6e4334564` |
| `epics.md` | `15f208cd8e2270dfb82352c5d7303b083ffd8f64c91ae40013a946fae6f69982` |
| `external-dependency-register.md` | `3f29c4651eb79e475bd087fff402903652984218b1c66f1d26cd86dcfbb5a4f3` |
| `launch-readiness-register.md` | `bd96ff74f3a6b3343b1a2f5922a9f7a65cefb7a1dd29fcff728714717ec1ca20` |
| authoritative `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

Repository inspection was read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`. This review modified only this report. The same eight hashes were recomputed after the report write and remained byte-for-byte identical. The memlog check remained a hash operation only.

## Deterministic Gate

Command:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: **PASS**, `ok: true`, zero findings. A separate heading scan found exactly one each of AD-1 through AD-31, in ascending order, with no gap, duplicate, deletion, reuse, or renumbering.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 0 |
| Medium | 2 |
| Low | 2 |

## Critical

None.

## High

None.

## Medium

### VC23-M1 — Architecture-owned `RQ-1` assumptions still lack literal retirement dates

PRD FR-28 item 9 and §8.1 require every Architecture-owned, `RQ-1`-scoped assumption to carry a co-owner-approved literal calendar date; a milestone or missing date blocks on that ground alone (`prd.md:730,894-898`). The spine correctly exposes multiple current `ARCH-A` rows as `Unscheduled`, including ARCH-A-1 through ARCH-A-4 where unretired, ARCH-A-6 through ARCH-A-8, ARCH-A-11, ARCH-A-12, and ARCH-A-14 (`ARCHITECTURE-SPINE.md:1378-1395`). The bound PRD also expressly records its Architecture-owned A-7, A-8, A-20, and A-24 as having no approved date (`prd.md:923-940`). This is safely visible and keeps `RQ-1` blocked, so it is not High. Obtain the named co-owner approvals and record literal dates; Architecture must not invent them.

### VC23-M2 — `PostingPending` still lacks one concrete timeout authority

FR-18 requires the stored attempt deadline to be no shorter than the `EXT-CONV-AI-1` posting timeout, but neither the PRD nor AD-5 selects a literal duration or one exact versioned seam/profile field (`prd.md:438`; `ARCHITECTURE-SPINE.md:287,1395`). Story 7.4 correctly blocks on retiring ARCH-A-14 (`epics.md:2558`), so no unsafe runtime path is authorized, but two future implementations could otherwise persist different deadlines. Bind one literal or exact versioned configuration authority and obtain the required Product confirmation before posting/recovery becomes ready-for-dev.

## Low

### VC23-L1 — Root bUnit remains behind the current stable package

Root authority and the parent-authoritative Builds catalog pin bUnit `2.9.0`; the separately checked-out Builds catalog and the official NuGet flat-container index expose stable `2.10.3`. The spine accurately reports the root pin and places alignment in Story 5.6 (`ARCHITECTURE-SPINE.md:917,1351`), so this is build maintenance rather than target-architecture drift.

### VC23-L2 — The current matrix-v7 table header still says “Required GateIds in v6”

The register makes `OperationGateMatrixVersion = 7` the sole current matrix and the section heading is version 7 (`launch-readiness-register.md:180,208`), but the table's third column remains labelled `Required GateIds in v6` (`launch-readiness-register.md:214`). The surrounding authority, carried-forward-v6 fixture description, matrix-v7 additions, spine, conventions, and Epics all select v7, so no old version is executable. Relabel the column `Required GateIds in v7` to remove the handoff residue.

## v22 Critical/High Correction Audit

| Prior finding | v23 disposition |
| --- | --- |
| `VC22-H1` — two conformance clauses rejected the third compromise registrar | **Closed.** AD-3, AD-30, Mutation, the conventions paragraph/checklist, the matrix, and Story 5.4 now agree on exactly three closed pre-command primitives. Story 5.4 owns only replay/security-recorder conformance while Story 8.3 owns the compromise registrar (`ARCHITECTURE-SPINE.md:276,575,864`; `IMPLEMENTATION-CONVENTIONS.md:43,79`; `epics.md:1480`; `launch-readiness-register.md:234,336`). |
| `VC22-H2` — accepted-batch identity depended on the not-yet-committed seal revision | **Closed.** `DestructionSealId` is assigned from tenant/request, accepted token, predicate, and manifest before the first barrier; batch/signing identities bind that stable id. The actual `CommittedDestructionSealGuardRevision`/`CommittedIssuedGuardRevision` exists only as authenticated result evidence and is excluded from identity and signed bytes. Exact no-issue proof terminalizes each stale signed attempt before an incremented signing attempt reuses the stable seal/batch/manifest/attestation identity, including two or more consecutive stale attempts (`ARCHITECTURE-SPINE.md:218,259-268,567-571`; `IMPLEMENTATION-CONVENTIONS.md:29-31`; `launch-readiness-register.md:328-332,349-351,373`; `epics.md:3036-3037,3071-3072`). A pre-prepare accepted-set supersession remains a distinct immutable token/set; it is not a stale signing retry. |
| `VC22-H3` — replacement-key compromise raced re-attestation activation | **Closed.** Activation identity now binds the replacement key, attestation, successor dispatch, expected blocked-batch revision, and expected tenant-key block-set revision. One protection-owner transaction reopens only when the replacement key has no block at that revision; a block that exists or wins returns `ActivationBlockedByReplacementKeyCompromise` and leaves/places the same batch in that new key's exact `ConsumptionBlocked` state. Exact lookup, migration, and restore preserve the winning result (`ARCHITECTURE-SPINE.md:266,571,601`; `IMPLEMENTATION-CONVENTIONS.md:33`; `launch-readiness-register.md:359,373`; `external-dependency-register.md:139,201,251`; `epics.md:3038,3071`). |
| `VC22-H4` — Story 8.3's primary dependency excluded post-start holds | **Closed.** The primary dependency, requirements, dependency manifest, and v21-v23 scope rows all apply `OD-HOLD-DELETION-PRECEDENCE-1` to post-arm/pre-seal contention and every post-start hold branch through completion, while ordinary no-contender deletion remains evaluable and no outcome is selected (`epics.md:2979,3062-3072`). The runtime decision catalog and bound PRD carry the same affected set (`launch-readiness-register.md:95,118`; `prd.md:955-961`). |

The v22 Medium source defect is also closed: the spine no longer cites the two absent historical v21 specialist reports. Every declared non-concurrent local source resolves. The five v23 review paths are the explicitly anticipated concurrent outputs for this gate and are not treated as missing authoritative inputs. The unused `ReattestedAwaitingDispatch` state was removed, and the carried-forward deletion fixture paragraph is now correctly described as matrix-v6 coverage retained by v7.

## Authoritative 2026-09-12 Critical/High Closure Audit

| Authoritative finding | Current disposition |
| --- | --- |
| `C-1` weaker safety evaluation | **Closed.** Snapshot and every then-current applicable platform/tenant policy are conjunctive; one-pass collapse requires a machine-checkable, contract-tested dominance proof. |
| `C-2` hold/deletion exclusion | **Closed.** Both origins share canonical scope, ordinal admission fences, immutable owner cycles, current-ordinal zero proof, continuous content guard, accepted inventory, shared hold/seal ordering, signed all-or-none destruction batches, post-start hold restrictions, target coverage, protection-owner cancellation/consume arbitration, and guard-linearized completion. |
| `C-3` bootstrap/matrix deadlock | **Closed.** Matrix v7 carries explicit scopes and closed target-aware bootstrap/repair/direct-precondition variants; consumers cannot infer local bypasses. |
| `H-1` scheduled Approver lifecycle | **Closed.** Single-flight ownership, cadence/freshness, two-pass evidence, leased resolution, typed outcomes, and recovery are bound. |
| `H-2` distributed safety rescan | **Closed.** Epoch/index owners, finite manifests, fenced bounded coordination, on-demand initialization, `RescanPending`, activation, and restart recovery are explicit. |
| `H-3` human-only Approvers | **Closed.** Parties-authoritative human classification/liveness and historical actor binding are mandatory. |
| `H-4` ledger lifetime conflation | **Closed.** Rate, open-interaction, monthly Budget, and capacity have separate owners, identities, lifetimes, and recovery; the remaining Product choice blocks before either joint ledger. |
| `H-5` human identity | **Closed.** Stable `AuthenticatedHumanActorId` plus historical Party binding supports separation across principal/role changes; Workflow is explicitly non-human. |
| `H-6` proposal/index crash consistency | **Closed.** Interaction truth, directory permits, protected action outboxes, effect leases, expected revisions, acknowledgements, and reconciliation are explicit. |
| `H-7` indivisible Conversations dependency | **Closed.** Core `EXT-CONV-AI-1` and conditional `EXT-CONV-RETRACTION-1` remain separately owned and consumed. |
| `H-8` trusted-envelope crypto/replay | **Closed.** Canonical MAC input, logical/delivery identities, replay registrar, time/key lifecycle, ACLs, durable security spool, and exact lost-ack recovery are bound. The later deletion-capability registrar is now consistently the third closed non-principal primitive. |
| `H-9` export bytes/lifecycle/signature | **Closed subject to the surfaced Product lifecycle decision.** Immutable protected store/index, AEAD bytes, JCS/ES256 manifest, direct key delivery, store/fence lifecycle, cleanup, restore, and all-copy receipts are explicit; export-free deletion does not fabricate this authority. |
| `H-10` Dapr current exposure | **Closed as current-reality classification.** Root-authoritative Client/ASP.NET exposure at `1.18.5`, non-authoritative checked-out `1.18.7`, and future Workflow adoption are distinct. |
| `H-11` public contracts falsely current | **Closed.** Required completion vocabulary remains separate from the explicit current delivery-debt ledger. |
| `H-12` sprint/dependency contradiction | **Closed as surfaced delivery-governance debt.** `OD-SPRINT-5.1-5.2-1` preserves the discrepancy without rewriting history or creating architecture authority. |

## Product Authority And Architecture Versus Delivery Debt

The bound FR-8 pre-Provider order remains exact: authorization; lifecycle/dependency; local block; provider/model; joint rate/open decision; Confirmation-mode Approver resolution; context; Budget; safety; membership (`prd.md:286-298`). The spine, conventions, matrix, and Epics preserve the sequence for initial generation and regeneration.

Initial-output safety status, rate/concurrency consumption, hold/deletion precedence, hold-prepare cancellation, operator deletion cancellation and nonterminal disposition, export lifecycle, historical safety, Automatic-mode retraction, instruction protection, legacy plaintext disposition, class/range scope, human-origin exact-Conversation scope, release-recorder authority, Dapr security posture, and sprint reconciliation remain explicit Open decisions with named owners, precise affected evaluations, and restrictive states. The runtime catalog's union/supersession contract prevents scope narrowing by omission. This review chooses none.

The current-versus-target boundary is accurate. Current Agents source imports EventStore Client/DomainService, a partial Conversations client, and registers `AddDaprClient`, but no Agents project references Dapr Workflow. Current source has no architecture-decision catalog, replay registrar/security spool, separated rate/open/Budget aggregates, safety epoch/index, interaction directory/effect-lease system, deletion scope guards/cycles, signed destruction capability/compromise registrar/protection-owner state machine, export store, or guard-owned deletion completion. EventStore exposes payload-protection contracts/hooks and a no-op-compatible default rather than the accepted production engine. The spine assigns each absence to stories and external prerequisites (`ARCHITECTURE-SPINE.md:1328-1351`). Those are implementation debt and readiness blockers, not evidence that the target exists and not causes to weaken it.

## Verified Repository, Gitlink, Package, And Source Reality

- Root authority is commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`. Exact root gitlinks are Builds `a32cb422`, Conversations `73bcee6f`, EventStore `ce9e779a`, FrontComposer `053b2008`, Memories `3644ef63`, Parties `fa423985`, and Tenants `2fac1839`.
- Internally clean working-tree checkouts for Builds `cf52f74`, Conversations `64b05083`, EventStore `a568af4e`, FrontComposer `1b3608c9`, and Memories `42dfa26b` differ from their parent gitlinks; Parties and Tenants match. The spine correctly treats root gitlinks as repository authority and these separate checkout heads as local/non-authoritative reality.
- Root `global.json` pins SDK `10.0.401` with `latestPatch`; root build properties set `net10.0` and C# 14. Microsoft's official .NET 10 release metadata identifies release `10.0.12` and SDK `10.0.401` for 2026-09-08. The official .NET advisory confirms CVE-2026-69522 is Windows-specific, affects `Microsoft.DiaSymReader.Native`, and fixes that component at `18.9.0-beta1.26405.2`; no direct reference exists in the inspected graph. The spine's technology/security statement is current.
- The parent-authoritative Builds catalog pins Dapr Client/ASP.NET/Workflow `1.18.5`; the checked-out Builds catalog pins `1.18.7`. The official NuGet flat-container indexes expose `1.18.7` as the newest stable Dapr Client/ASP.NET/Workflow family, with later entries prerelease only. Agents source consumes EventStore Client/DomainService and calls `AddDaprClient`; no Agents project references Dapr Workflow. The spine's exposure-versus-future distinction is exact.
- The imported catalog values for MediatR `14.2.0`, FluentValidation `12.1.1`, OpenTelemetry `1.18.0`, Fluent UI Blazor `5.0.0-rc.5-26219.1`, Shouldly `4.3.0`, and the recorded root test overrides match repository evidence. Official NuGet exposes OpenTelemetry `1.18.0` and the named Fluent UI v5 RC; no provider or Agent Framework SDK is selected by the architecture.
- Root and parent-authoritative Builds pin bUnit `2.9.0`; only the separate checked-out Builds catalog and official NuGet expose `2.10.3`, producing VC23-L1 rather than a false architecture claim.
- All twelve external dependencies remain `Uncommitted` with unresolved target/date/verification fields where owners have not committed them. Consumers remain blocked, no story is falsely ready-for-dev, and `RQ-1` remains NOT READY.

Primary verification endpoints: `https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json`; `https://github.com/dotnet/announcements/issues/439`; and the official NuGet flat-container indexes for `dapr.client`, `dapr.aspnetcore`, `dapr.workflow`, `bunit`, `opentelemetry`, and `microsoft.fluentui.aspnetcore.components`.

## Good-Spine Checklist

| Dimension | Result |
| --- | --- |
| Real divergence points and enforceable rules | **Pass.** Owners, conditional writes, typed stale/blocked results, exact lookup, and idempotent recovery close the material implementation choices. |
| Ownership and concurrency | **Pass.** Guard, interaction/directory, ledger, protection, export, and deletion owners have explicit linearization points; stable signing and replacement-key races now converge. |
| Security, tenancy, and irreversible data | **Pass.** Tenant/actor/target/manifest/key boundaries fail closed; hold/admission/compromise can no longer be bypassed by a stale lookup or ambiguous handoff. |
| Recovery, replay, migration, and restore | **Pass.** Exact identities, no-issue proof, obsolete attempts, protection states, activation results, manifests, and successor preservation are bound. |
| PRD, Epic, register, and convention traceability | **Pass with Low label residue.** Product authority and story dependencies agree; VC23-L2 is a header-only version label. |
| Product-decision discipline | **Pass.** Open choices are named, scoped, owned, and restrictive; no architecture text invents an outcome. |
| Architecture versus implementation debt | **Pass.** Required target mechanics and absent shipped behavior are explicitly separated. |
| Brownfield and named technology truth | **Pass with Low maintenance tail.** Gitlinks, checkout reality, Dapr exposure, SDK/security facts, and package pins are accurate; bUnit remains maintenance debt. |
| Sources and mechanics | **Pass.** All non-concurrent local sources resolve; lint and AD identity checks pass. |
| Altitude completeness | **Pass.** Deployment, dependency, security, data, operational, recovery, evidence, and governance dimensions are covered without implementation-level code design. |

## Gate Conclusion

The complete frozen v23 primary verified-current/bound-PRD gate is **PASS**. Final counts are **Critical 0, High 0, Medium 2, Low 2**. Deterministic lint passes and AD-1 through AD-31 remain stable. The v22 High findings are closed without choosing a Product outcome; the remaining Medium/Low items do not authorize unsafe behavior or invalidate the architecture spine.
