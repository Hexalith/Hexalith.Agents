---
review: bmad-architecture-verified-current
candidate: v19
date: 2026-09-13
verdict: PASS
critical: 0
high: 0
medium: 5
low: 1
---

# Verified-Current / Bound-PRD Reviewer Gate — v19

## Verdict

**PASS — 0 Critical, 0 High, 5 Medium, 1 Low.** The frozen v19 package closes the v18 Critical and three High findings without choosing an unresolved Product outcome. The shared tenant scope guard now gives legal-hold registration and deletion sealing one order; accepted admission violations have an explicit pre-first-cut recovery branch; a predicate-scoped content guard stays continuously installed while successor ordinal/token bindings are appended; and `EXT-PROTECTION-1` now owns a deterministic single-use, manifest-wide all-or-none destruction operation with exact ordered outcomes. The authoritative 2026-09-12 Critical/High findings remain closed. The remaining findings are fail-closed assumption/parameter debt, provenance maintenance, and current build maintenance; none authorizes implementation of a blocked branch.

## Frozen Inputs And Integrity

The following SHA-256 values matched at intake, immediately before report creation, and after report creation:

| Input | SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `79ebd22bba3ec5c42eae31377a461f00a94035c1cef8422927562295d9a7e950` |
| `IMPLEMENTATION-CONVENTIONS.md` | `4686b409fce780443ec4d141e5306b7bf85183d23e31e6f6260ddc18a0ca2744` |
| architecture `.memlog.md` | `4df0405f18c54441f6d8ed20cfa5ac39798eba21620d2f731fefc9860f2caa01` |
| bound `prd.md` | `d715f54d76e58a9ba6183a0e04979779703476b855842513816c35b8655ab9b0` |
| `epics.md` | `ed9ccb70a25e80fa16833f09b2af6f219d8291e4d5bd5cb0aa17d6e9de59ad56` |
| `external-dependency-register.md` | `60ff899220f10f2227f420e9321f01328d5048481cf4cad55553b0f8e78b206c` |
| `launch-readiness-register.md` | `eca4fd6c787a7f3e65fea4570262c294895933a7114b3f262e4f1f340b6b6383` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

No reviewed source artifact, memlog, code file, manifest, or submodule was modified by this review. This report is the only write.

## Method And Deterministic Gate

I read the repository instructions, the complete BMad reviewer-gate instructions, authoritative validation report, current spine and conventions, bound PRD, active Epics 5–8 delivery map, both authoritative registers, current memlog tail, root manifests/gitlinks, and relevant current source. I traced the four v18 correction paths through their literal matrix variants and dependency consumers, then re-walked the original C-1..C-3 and H-1..H-12 closures rather than treating prior reports as proof.

The required deterministic check passed:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py \
  --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2

ok: true
total_findings: 0
```

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 0 |
| Medium | 5 |
| Low | 1 |

## Critical

None.

## High

None.

## v18 Critical/High Correction Audit

| Prior finding | v19 disposition and exact evidence |
| --- | --- |
| `C-v18-1` / `C-R18-1` — hold arrives after barrier authorization but before actual irreversible start | **Closed.** One `GovernanceScopeGuard(TenantId)` owns both `HoldContenderRegistered` and `DestructionSealed`. Every already-authorized hold registers before `ProtectionFence` preparation; registration and `CommitDeletionDestructionStartBarrierEffect` serialize at that owner; a contender that wins after authorization makes the no-hold authorization stale; and an Open/missing/mismatched precedence decision advances neither branch. Only an exact effective deletion-allowed disposition may be consumed in the same seal commit. Exact result lookup and guard-release receipts close loss/recovery (`ARCHITECTURE-SPINE.md:209,229-231,466,472,480,553`; `IMPLEMENTATION-CONVENTIONS.md:25`; matrix rows 274-285 and 334-341; PRD FR-30/§9 at `prd.md:955-957`; Story 8.1 at `epics.md:2834`; Story 8.3 at `epics.md:3027`). The mechanism fixes ordering and preserves `OD-HOLD-DELETION-PRECEDENCE-1` as Open. |
| `H-v18-1` — an accepted admission violation before the first global cut had no successor path | **Closed.** `ContainDeletionScopeAdmissionFenceViolation` now has a literal `NoCurrentGlobalCutCandidateOrAcceptedToken` branch; it records the exact ordinal-partitioned violation, proves downstream artifacts absent, invalidates nothing nonexistent, and idempotently assigns one successor ordinal. The successor authorization accepts either this receipt or exact post-cut invalidation; concurrent detection and lost acknowledgement resolve to the same ordinal (`ARCHITECTURE-SPINE.md:227,553`; `IMPLEMENTATION-CONVENTIONS.md:23`; matrix rows 296-310 and 321; `epics.md:3011,3054,3056`; `external-dependency-register.md:129,132`). |
| `H-v18-2` — a recut after content-fence installation could neither reuse nor rebind the old-token fence | **Closed.** The content guard has immutable identity `(DeletionRequestId, ScopePredicateDigest)` independent of ordinal/token. Initial installation records the first binding; recut uses separate authorize/effect/result variants to append the new binding only after continuous-install, unchanged-predicate, predecessor-invalidation, new-cut, current-zero, and no-removal/no-seal proofs. Preparation and barrier consume the current binding; migration preserves all bindings (`ARCHITECTURE-SPINE.md:209,470,553`; `IMPLEMENTATION-CONVENTIONS.md:23,27`; matrix rows 315-320, 324, 334-341; `epics.md:2985,3011`; `external-dependency-register.md:129,132`). |
| `H-v18-3` — a singular barrier capability was undefined for multiple per-interaction DEKs | **Closed.** The accepted set is a sorted `(TenantId, AgentInteractionId, TargetProtectionKeyAlias)` manifest with deterministic digest/batch identity. The seal returns one authenticated single-use capability for that whole manifest, not a reusable per-key bearer. `EXT-PROTECTION-1` owns atomic all-or-none consumption, exact byte-identical retry/lookup, changed-manifest conflict, ordered irreversible per-target outcomes, revocation-versus-consumption, and distinct containment-ordinal batches (`ARCHITECTURE-SPINE.md:231,466,472,553`; `IMPLEMENTATION-CONVENTIONS.md:25`; matrix rows 334-341; `external-dependency-register.md:233-236`; Story 8.3 at `epics.md:3027,3034,3054,3056`; PRD §9 at `prd.md:957`). No current engine is claimed; `EXT-PROTECTION-1` remains `Uncommitted`, so consumers remain blocked. |

The four fixes are mutually compatible. In particular, the seal is the sole `DestructionStarted` instant; the earlier `ProtectionFence` authorization is reversible, the old content guard never opens during recut, and a multi-key operation is not simulated by repeated reuse of the seal receipt.

## Authoritative 2026-09-12 Critical/High Closure Audit

| Authoritative finding | Current disposition |
| --- | --- |
| `C-1` weaker safety evaluation | **Closed.** AD-20 requires snapshot-plus-current conjunctive evaluation for retry, regeneration, approval, and pre-post, with only equivalent dominance optimization; policy-epoch enumeration and exact evidence are separately bound. |
| `C-2` hold/deletion all-or-nothing exclusion | **Closed.** The protection fence, shared hold/seal guard, immutable accepted inventory, content guard, all-reservation preparation, and all-or-none manifest batch prevent partial ordinary destruction and make unknown evidence restrictive. |
| `C-3` operation-matrix bootstrap/scope deadlock | **Closed.** Matrix v4 has explicit gate-free, directly evidenced bootstrap/repair variants and Platform/Tenant scope; unrelated gates remain mandatory. |
| `H-1` scheduled Approver lifecycle | **Closed.** AD-8 and the matrix bind the scheduled owner, lease-before-read protocol, finite evidence, two-pass empty handling, state-specific results, and unavailable recovery. |
| `H-2` safety-rescan ownership | **Closed.** `SafetyVerdictEpoch`/`SafetyVerdictIndex` own the frozen manifest, bounded workers, call-time `RescanPending`, policy-change invalidation, recovery, and evidence. |
| `H-3` human-only Approvers | **Closed.** `EXT-PARTIES-1`, AD-8, and AD-30 require fresh human classification/liveness and stable actor binding at configuration and resolution. |
| `H-4` conflated ledger lifetimes | **Closed.** AD-2/AD-21 separate immutable rolling `RateLimitLedger`, nonterminal `OpenInteractionLedger`, and UTC-month `BudgetLedger`, with distinct reset/release/settlement authorities. The unresolved concurrency-rejection consumption choice is safely isolated as `OD-RATE-CONCURRENCY-CONSUMPTION-1`. |
| `H-5` missing human identity | **Closed.** AD-30 principal identities carry stable `AuthenticatedHumanActorId` on every human principal, with principal-kind-specific evidence and exact second-party comparison. |
| `H-6` crash-inconsistent Conversation proposal/index state | **Closed.** Directory-owned permits, protected outboxes, same-owner effect leases, target acknowledgements, reconciliation, migration, and deletion manifests close the cross-stream crash windows. |
| `H-7` optional retraction blocked core Conversations | **Closed.** `EXT-CONV-AI-1` contains exactly six core seams; optional retraction is the separate conditional `EXT-CONV-RETRACTION-1`. |
| `H-8` incomplete trusted-envelope cryptography/replay | **Closed.** AD-29/AD-30 and `EXT-SECRETS-1` bind canonical bytes, purpose/audience, issuer, time/skew, stable logical id, per-delivery nonce, first-seen ledger, digest/key versions, overlap, emergency revocation, constant-time verification, and confined denial recording. |
| `H-9` export-byte ownership/lifecycle/signature | **Closed.** `EXT-EXPORT-STORE-1` owns immutable encrypted bytes/index/purge/restore; AD-22 fixes RFC 8785 canonical bytes, detached ES256 JWS, trust-anchor identity, fence commit, key-delivery recovery, and all-copy completion. Product lifecycle choices remain `OD-EXPORT-LIFECYCLE-1`. |
| `H-10` existing Dapr exposure misstated as future-only | **Closed.** Stack and ARCH-A-15 distinguish current parent-authoritative transitive Client/ASP.NET `1.18.5` from future Workflow adoption and the non-authoritative Builds checkout's `1.18.7`. |
| `H-11` backlog contracts called currently shipped | **Closed.** AD-15 uses required-completion vocabulary; the delivery-debt table explicitly identifies missing current contracts. |
| `H-12` sprint status contradicts dependency/evidence authority | **Closed in architecture authority and safely surfaced as delivery-governance debt.** `OD-SPRINT-5.1-5.2-1` blocks dependent story authorization while the tracker/evidence discrepancy awaits its owner; the spine does not rewrite delivery history. |

## Medium

### VC19-M1 — Architecture-owned `RQ-1` assumptions still lack literal calendar retirement dates

**Classification:** planning-governance debt, fail-closed; no Product outcome may be invented.

PRD FR-28 item 9 and §8.1 require every Architecture-owned `RQ-1` row to carry a literal co-owner-approved calendar date. `ARCH-A-1`, `ARCH-A-2`, `ARCH-A-3`, the remaining test-stack portion of `ARCH-A-4`, `ARCH-A-6`, `ARCH-A-7`, `ARCH-A-8`, `ARCH-A-11`, `ARCH-A-12`, and `ARCH-A-14` remain `Unscheduled` or milestone-only (`ARCHITECTURE-SPINE.md:1327-1343`). The spine correctly emits `UnretiredAssumption` and cannot report READY, so this is contained. Obtain approved dates; do not synthesize them in Architecture.

### VC19-M2 — `PostingPending` has no one concrete timeout authority

**Classification:** unresolved Architecture/Product parameter, safely surfaced.

PRD FR-18 requires a stored attempt deadline no shorter than the posting seam timeout. AD-5 and `ARCH-A-14` preserve that lower bound but neither the PRD nor spine selects a literal duration or one versioned seam/profile field as the authority (`prd.md:438`; `ARCHITECTURE-SPINE.md:255-265,1342`). Two otherwise conforming posting/recovery workers could choose different deadlines. Bind one duration or one exact versioned seam/configuration source and obtain Product confirmation before the posting/recovery story is ready.

### VC19-M3 — Cross-artifact `updated` metadata no longer describes the v19 authority snapshot

**Classification:** document provenance and operator-readability debt; it does not weaken the rules.

The spine is `updated: 2026-09-13`, but the bound PRD remains `updated: 2026-09-12` and its §8.1 prose explicitly calls the current spine `updated: 2026-09-12`. Both registers also remain `updated: 2026-09-12` although their frozen v19 content includes the new shared-guard, pre-cut, continuous-binding, and manifest-batch contracts. Update only provenance metadata and the PRD's literal cross-reference in a coordinated document pass; no requirement or Product outcome needs to change.

### VC19-M4 — Four historical review files named as spine sources do not exist

**Classification:** source-chain completeness debt.

The source list names `review-2026-09-12-security-data-integrity-v5.md`, `review-2026-09-12-brownfield-drift-v5.md`, `review-2026-09-12-security-data-integrity-v18.md`, and `review-2026-09-12-brownfield-drift-v18.md`, but none exists in `reviews/`. The five v19 paths are recognized as concurrent anticipated deliverables and are not counted here. Remove the four stale historical citations or materialize the exact immutable reports they claim to source.

### VC19-M5 — The SDK row overstates the advisory's version mapping and omits the direct advisory source

**Classification:** verified-current security-source precision; current repository evidence is not vulnerable on the inspected dependency graph.

The Stack calls SDK `10.0.401` “the current Windows fixed floor” for `CVE-2026-69522`. Microsoft's advisory defines the vulnerable component as `Microsoft.DiaSymReader.Native`, the patched package floor as `18.9.0-beta1.26405.2`, and separately advises installing the latest .NET/Visual Studio. The official .NET 10 release index confirms `10.0.12` is the current September patch, and the installed/root-selected SDK is `10.0.401`; repository assets and project/package declarations contain no `Microsoft.DiaSymReader.Native` reference. Therefore the present scoped conclusion is safe, but `10.0.401` is not the component floor stated by the advisory. Reword the row as “current September SDK satisfying the advisory on the inspected Windows graph” and cite the [official .NET advisory](https://github.com/dotnet/announcements/issues/439) plus the [official .NET 10 release index](https://github.com/dotnet/core/blob/main/release-notes/10.0/README.md). Keep the already-correct rule that a future direct/transitive component reference must meet the component's patched floor independently.

## Low

### VC19-L1 — Root bUnit remains behind the current Builds checkout and current stable release

Root `Directory.Packages.props` pins bUnit `2.9.0`; the clean Builds working-tree checkout pins `2.10.3`, and current bUnit documentation also installs `2.10.3`. The parent-authoritative Builds gitlink itself still pins `2.9.0`, so the spine's root version is truthful and Story 5.6 owns alignment. This is build maintenance, not an architecture contradiction. Evidence: `Directory.Packages.props:26`; `references/Hexalith.Builds/Props/Directory.Packages.props:317`; [bUnit test-project documentation](https://bunit.dev/docs/getting-started/create-test-project.html).

## Verified Technology And Repository Reality

- Root commit: `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`.
- Parent-authoritative gitlinks exactly match the spine: Builds `a32cb422`, Conversations `73bcee6f`, EventStore `ce9e779a`, FrontComposer `053b2008`, Parties `fa423985`, and Tenants `2fac1839`; Memories is `3644ef63` and is not misstated as an architecture target.
- The initialized submodule working trees are internally clean but differ from root authority for Builds `cf52f74`, Conversations `64b05083`, EventStore `a568af4e`, FrontComposer `1b3608c9`, and Memories `42dfa26`; Parties and Tenants match. The spine correctly calls only the parent gitlink architecture authority and specifically identifies the non-authoritative Builds checkout.
- Root `global.json` pins `10.0.401` with `latestPatch`; `dotnet --version` resolves `10.0.401`. Root `Directory.Build.props` fixes `net10.0` and C# `14`; package management is central and imports the Builds catalog.
- The parent-authoritative Builds catalog at `a32cb422` pins Dapr Client/ASP.NET/Workflow `1.18.5`; the current clean Builds checkout pins all three at `1.18.7`. NuGet's primary package index contains `1.18.7` for [Dapr.Client](https://api.nuget.org/v3-flatcontainer/dapr.client/index.json), [Dapr.AspNetCore](https://api.nuget.org/v3-flatcontainer/dapr.aspnetcore/index.json), and [Dapr.Workflow](https://api.nuget.org/v3-flatcontainer/dapr.workflow/index.json).
- Agents source/package mode references EventStore Client and DomainService; the parent-authoritative EventStore projects reference `Dapr.Client` and `Dapr.AspNetCore`, and `Program.cs` calls `AddDaprClient`. No Agents project references `Dapr.Workflow` or `Microsoft.Agents.AI`, exactly as the spine reports.
- Dapr 1.18's workflow identity behavior is current: a nonterminal instance id cannot be reused; the v1.18 guidance requires purge before reuse. Current docs later permit terminal-tree reuse for compatibility but still reject nonterminal reuse, so the AD-18 nonterminal claim remains correct. Evidence: [Dapr v1.18 management guide](https://v1-18.docs.dapr.io/developing-applications/building-blocks/workflow/howto-manage-workflow/) and [current workflow identity guidance](https://docs.dapr.io/developing-applications/building-blocks/workflow/workflow-features-concepts/).
- Microsoft Agent Framework 1.0 and Dapr Agents 1.0 are GA, supporting AD-9's “unselected by dependency authority, not ecosystem immaturity” wording. Evidence: [Microsoft's v1.0 announcement](https://devblogs.microsoft.com/foundry/from-local-to-production-the-complete-developer-journey-for-building-composing-and-deploying-ai-agents/) and [Dapr Agents official docs](https://docs.dapr.io/developing-ai/dapr-agents/).
- Relevant current code remains materially behind the target: no directory/ordinal guards, owner-cut cycles, shared governance scope guard, content-guard successor binding, decision catalog, rate/open/Budget ledgers, atomic deletion barrier, or manifest-batch destruction exists; Conversations at its root-authoritative gitlink has no durable deletion signal seam. Current output-safety handling still records `SafetyFailed`, and posting code/tests still embody pre-target behavior. The delivery-debt section and blocked story/dependency records describe this as implementation debt rather than shipped conformance.

## PRD, Epic, Register, And Open-Decision Reconciliation

The Product-fixed FR-8 order remains intact: directory intake may precede processing, but the workflow performs rate/open step 5, Confirmation-only Approver step 6, Context step 7, Budget step 8, prompt/context safety step 9, and leased Conversations membership step 10 before `AgentCallAccepted`. The deletion additions do not reorder that sequence.

Story 8.1 owns scope-guard hold registration/release and its hold/deletion race fixtures. Story 8.3 owns both deletion origins, ordinal admission/owner recut, pre-cut containment, continuous content binding, the seal, atomic multi-key destruction, containment batches, migration preservation, and all-copy completion. Story 6.1 owns the prerequisite directory/migration/write-authority cutover. Their dependency and evidence metadata name `EXT-HOST-1`, `EXT-PROTECTION-1`, `EXT-SECRETS-1`, `EXT-PARTIES-1`, and `EXT-CONV-AI-1` where consumed; the external register includes the same direct consumers. All critical external records remain `Uncommitted` with `TBD` target/date/command, so no story can claim `ready-for-dev` or live qualification.

The spine preserves exactly one `AD-1` through `AD-31`, with no duplicate or renumbered ID. Existing `OD-*` identifiers remain surfaced with owners and affected scopes. In particular, v19 does not choose armed hold/deletion precedence, operator cancellation or nonterminal treatment, human exact-Conversation semantics, class/range semantics, export lifecycle, legacy plaintext disposition, instruction protection, rate/concurrency consumption, initial output-safety status, recorder scope, or hold-prepare cancellation. Exact-interaction governance and PRD-fixed Conversation-origin deletion remain independently evaluable only on their committed-dependency and approved-decision terms.

## Architecture Contract Versus Implementation Debt

The shared guard, ordinal recovery, continuous binding, and atomic manifest batch are target architecture contracts because they determine how independently built EventStore, host, protection, and Workflow units converge. Their absence from the repository is delivery debt assigned to Stories 6.1, 8.1, and 8.3 plus the relevant `EXT-*` owners; absence is not permission to weaken the invariants. Conversely, `OD-*` rows are Product/governance decisions and are not implementation debt. The spine maintains that separation accurately. `VC19-M1` through `VC19-M5` are planning/document provenance debt, and `VC19-L1` is build maintenance; none is evidence that the target mechanics are implemented.

## Gate Conclusion

The complete frozen v19 verified-current/bound-PRD gate is **PASS**. Final counts: **Critical 0, High 0, Medium 5, Low 1**. Deterministic lint passed with zero findings, and the frozen inputs retained their declared SHA-256 values.
