---
review: verified-current-v22
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-13
intent: read-only-review
lens: verified-current / bound-PRD reconciliation
verdict: FAIL
critical: 0
high: 4
medium: 4
low: 2
lint_ok: true
---

# Verified-Current / Bound-PRD Reviewer Gate — v22

## Verdict

**FAIL — 0 Critical, 4 High, 4 Medium, and 2 Low.** The frozen v22 package closes the v21 protection-owner cancel/consume race, supplies signed-but-unissued recovery, moves emergency compromise outside the four-kind Agents principal model, and widens the runtime hold-decision catalog to all post-start consumers. It still cannot pass: current handoff clauses reject the third compromise registrar; the accepted-batch identity cannot remain stable across the stale signing retry it mandates; replacement-key compromise can race re-attestation activation without a defined winning state; and Story 8.3's primary dependency metadata excludes the post-start hold scope that its evidence manifest and Product authority require.

No finding asks Architecture to choose an unresolved Product outcome. The four High findings are architecture identity, state-transition, authority-handoff, or cross-artifact scope defects. Missing current implementations remain separately classified as delivery debt.

## Frozen Snapshot, Instructions, And Method

I read the complete repository instruction source and the complete BMad architecture reviewer-gate instructions before review. I then read the full frozen spine, conventions, authoritative validation report, bound PRD, Epics 5–8 delivery map, external-dependency register, and launch-readiness register; inspected exact root manifests, parent-authoritative submodule gitlinks, relevant current source and package catalogs; and checked official primary package/release indexes. Per the task constraint, the architecture `.memlog.md` was **not read**; only its supplied SHA-256 was recomputed.

The reviewed inputs matched the supplied hashes before this report was written:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `38db1ac6d9fe9cc1155ec042e8217ed2395d9c4399a2ac0e4d4e70c890ad1fa8` |
| `IMPLEMENTATION-CONVENTIONS.md` | `ec646e305acfe24534333e1db80aa4e9a6cd613561b84ab9752f50c846506f29` |
| architecture `.memlog.md` — hash only, content unopened | `907d88500cd5d66b4e0d7a818944d2f4ef34d7faacc3e416dde2ea7550d95dfb` |
| bound `prd.md` | `48ba04a4d89441fe9c6da95a114c307f2206e73eb42bda06635e4ba03073beee` |
| `epics.md` | `97dcb91fbd6e2418bb17f4c81303285d364d40a8071c1c42bc2be9ce4179d6de` |
| `external-dependency-register.md` | `78ecf69d009f828fb92da1f27c209a2f54676a9b4a2e39ff0fdf68eadd3206c4` |
| `launch-readiness-register.md` | `2f568fe50edf567cd7b39d24fa3ac43e077839ffe561a00874cb271e65198b8d` |
| authoritative `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

Repository inspection was read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`. This review modified only this report.

The same eight hashes were recomputed after the report write and remained byte-for-byte identical. The memlog check was a hash operation only; its content was never opened.

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
| High | 4 |
| Medium | 4 |
| Low | 2 |

## Critical

None.

## High

### VC22-H1 — Two current conformance clauses reject the third and final pre-command compromise registrar

**Classification:** target-architecture security/authority handoff contradiction; not implementation debt and not a Product decision.

**Evidence.** AD-3 defines exactly three pre-command primitives, adding `RegisterDeletionCapabilityKeyCompromise`; AD-30 calls `IDeletionCapabilityCompromiseRegistrar` the third and final primitive and confines its authenticated Secrets delivery, protection target, guard mirror, ACL, replay, and recovery behavior (`ARCHITECTURE-SPINE.md:275,574`). The normative conventions paragraph and matrix-v6 agree (`IMPLEMENTATION-CONVENTIONS.md:43`; `launch-readiness-register.md:212,232-234`). But the spine's current Mutation convention says the *only* exceptions are `RegisterTrustedEnvelopeFirstSeen` and `RecordSecurityObservation`, then rejects every other direct capability (`ARCHITECTURE-SPINE.md:863`). The conventions adoption checklist likewise mandates a test permitting only “the two AD-3” triples (`IMPLEMENTATION-CONVENTIONS.md:79`). Story 5.4's `OwnedClauses` also describes AD-30 as having two nonrecursive pre-command capabilities (`epics.md:1480`).

**Executable divergence.** A platform/boundary team can correctly implement the mandatory two-entry allowlist and prove the new registrar is rejected, while Story 8.3 and the protection team correctly require that registrar to install the tenant/key-version block before guard mirroring. During emergency revocation, one unit therefore prevents the only target protocol that can win the block-versus-consume race.

**Required correction.** Make the Mutation convention, adoption checklist, and Story 5.4 metadata agree with the closed three-entry inventory. If Story 5.4 owns only the first two, say that without restating AD-30's global inventory as two. Retain the exact Secrets-subscriber, tenant/key-family, protection-before-guard, non-public, non-principal, nonrecursive restrictions; do not add a fifth principal kind or a general direct-append path.

### VC22-H2 — A stale accepted-batch signing retry cannot keep the same batch identity and also record the actual successful seal revision

**Classification:** target-architecture deterministic identity and stale-recovery contradiction; not implementation debt.

**Evidence.** `DeletionDestructionBatchId` includes `DestructionSealRevision`, and the JWS signs that same field separately from `IntendedIssuedGuardRevision` (`ARCHITECTURE-SPINE.md:259,566`). The accepted batch is signed before the conditional guard seal/issue. If an intervening guard mutation makes that issue stale, v22 requires exact no-issue proof, `SignedAttestationObsoleteUnissued`, and a successor attempt that keeps the same batch, manifest, and `AttestationOrdinal` while changing the signing attempt and intended guard revision (`ARCHITECTURE-SPINE.md:267`; `launch-readiness-register.md:328-331,349-351`). The successful `DestructionSealed` event necessarily occupies a later guard revision, yet no contract defines `DestructionSealRevision` as a stable logical ordinal distinct from the actual event revision.

**Executable divergence.** Starting from guard revision G10, a signer may derive a batch whose anticipated seal is G11. If a hold or other mutation wins G11, the successor seal can only commit later. One team keeps the original batch and treats `DestructionSealRevision=G11` as logical even though no seal exists there; another recomputes the batch for the actual later seal; a third keeps the batch but changes its signed component. The exact JWS, lookup, protection idempotency key, and post-start evidence then disagree.

**Required correction.** Either derive the immutable batch from a once-assigned pre-seal authority ordinal/id and record the eventual guard event revision separately, or explicitly permit a proven-obsolete pre-issue batch identity to be superseded through one closed chain while guaranteeing only one guard-issued batch. Name the owner and meaning of both fields, propagate them through the matrix, conventions, external contracts, Story 8.3 metadata, recovery/migration/restore manifests, and test two or more consecutive stale signing attempts.

### VC22-H3 — Re-attestation activation can race compromise of the replacement key without a closed winning transition

**Classification:** target-architecture security state-transition and recovery defect; not a Product decision.

**Evidence.** Emergency blocks are installed per tenant/key version at the protection owner before the guard mirror (`ARCHITECTURE-SPINE.md:265,574`; `launch-readiness-register.md:234,336`). A batch blocked for K1 remains blocked while the guard activates K2 and issues a successor dispatch, after which `ActivateReattestedDeletionBatch` changes the exact K1-blocked batch to `Unconsumed` (`ARCHITECTURE-SPINE.md:265`; `launch-readiness-register.md:337-359`). The activation row checks the old compromise receipt, new guard attestation/dispatch, old blocked revision, and absence of admission/terminal state, but it does not atomically require absence of a protection-owner tenant/key-version block for replacement key K2.

**Executable divergence.** If K2's registrar installs its protection block after successor dispatch but before activation and its guard mirror is delayed, one implementation can satisfy the listed activation preconditions and reopen the batch under K2; another rejects because the K2 block exists; a third converts the batch to a K2-specific block. Later reserve correctly checks the global key block, but the required exact per-batch K2 blocked state for another re-attestation is no longer deterministic and restore/recovery cannot converge.

**Required correction.** Make activation conditionally check `NoTenantKeyVersionBlockForReplacementAttestation` at the same protection-owner revision and define the typed result when a newer block wins. The winner must preserve or install the exact per-batch `ConsumptionBlocked(CapabilityKeyCompromise, NewRevision)` state used by another same-batch re-attestation. Bind replacement key/block revision into activation identity and test compromise before re-attest, before dispatch, before/at activation, and after activation but before reserve, including lost guard mirror and restore.

### VC22-H4 — Story 8.3's primary dependency text excludes post-start holds from the Product decision

**Classification:** bound-PRD / story-authorization scope contradiction at an irreversible deletion boundary; not permission to choose the Product outcome.

**Evidence.** The Product authority states that `OD-HOLD-DELETION-PRECEDENCE-1` owns both pre-seal contention and post-start holds; while Open/missing/mismatched/not deletion-allowed, every still-preventable accepted-batch consumption, containment, purge, and completion remains blocked (`prd.md:955-961`). The runtime catalog's closed baseline now includes those post-start evaluations and StoryAuthorization 8.1/8.3 (`launch-readiness-register.md:95,118`), and the spine's final decision row matches (`ARCHITECTURE-SPINE.md:1309`). Story 8.1's criteria/evidence and Story 8.3's evidence manifest are also correct (`epics.md:2837-2845,2881-2887,3062-3070`). But Story 8.3's primary External dependency paragraph says hold precedence applies **only** to a contender registered after `DeletionArmed` and before `DestructionSealed` (`epics.md:2979`).

**Executable divergence.** A story authorization/dependency consumer may follow the primary dependency list and schedule or accept the post-start branch without the decision, while runtime/matrix units correctly require the decision. The runtime fails closed, but the delivery/evidence unit can claim a branch ready or complete under an explicitly narrower scope than Product fixed.

**Required correction.** Replace the exclusive sentence with the exact current split: conditional for post-arm/pre-seal contention **and every post-start hold branch through completion**, while ordinary no-contender deletion remains evaluable. Keep the decision Open and select neither hold-wins nor deletion-wins.

## Medium

### VC22-M1 — `ReattestedAwaitingDispatch` is declared but no legal transition enters or leaves it

The closed protection-owner state list includes `ReattestedAwaitingDispatch` (`ARCHITECTURE-SPINE.md:263`), but the normative protocol keeps the batch in the exact `ConsumptionBlocked` state through guard re-attestation and successor dispatch, then activation changes that exact blocked state directly to `Unconsumed` (`ARCHITECTURE-SPINE.md:265`; `launch-readiness-register.md:337-359`). Remove the unreachable state or define its owner transition, revision condition, recovery, migration/restore preservation, and activation input. The current exact rows are safely restrictive, so this is Medium rather than High.

### VC22-M2 — Architecture-owned `RQ-1` assumptions still lack literal retirement dates

PRD FR-28 item 9 and §8.1 require every Architecture-owned `RQ-1` assumption to carry a co-owner-approved literal calendar date (`prd.md:730,894-898`). Multiple current `ARCH-A` rows remain `Unscheduled` or milestone-only, including `ARCH-A-1` through `ARCH-A-4` where unretired, `ARCH-A-6` through `ARCH-A-8`, `ARCH-A-11`, `ARCH-A-12`, and `ARCH-A-14` (`ARCHITECTURE-SPINE.md:1381-1395`). The spine correctly keeps them blocking. Obtain owner-approved dates; Architecture must not invent them.

### VC22-M3 — `PostingPending` still has no single concrete timeout authority

FR-18 requires the durable attempt deadline to be no shorter than the seam posting timeout, but AD-5 and `ARCH-A-14` select neither a literal duration nor one exact versioned configuration field (`prd.md:438`; `ARCHITECTURE-SPINE.md:287,1394`). Two workflow implementations can persist different deadlines. Bind one literal or exact versioned seam/profile source and obtain the required Product confirmation before the posting/recovery work becomes ready.

### VC22-M4 — Two declared historical v21 sources do not exist

The spine frontmatter declares `reviews/review-2026-09-13-security-data-integrity-v21.md` and `reviews/review-2026-09-13-brownfield-drift-v21.md` (`ARCHITECTURE-SPINE.md:122-123`), but neither file exists. The five v22 report paths are concurrent anticipated gate outputs and are not classified as missing inputs; the two v21 paths are historical citations. Restore the reports or remove the unsupported declarations with an auditable explanation.

## Low

### VC22-L1 — Root bUnit remains behind the current stable package

Root authority and the parent-authoritative Builds gitlink pin bUnit `2.9.0`; the separately checked-out Builds catalog and the official NuGet index expose stable `2.10.3`. The spine accurately reports the root pin and assigns test-stack alignment to Story 5.6, so this is build maintenance, not a target-architecture contradiction.

### VC22-L2 — A carried-forward fixture paragraph is still labelled matrix-v5

The launch register makes matrix version 6 current and says v6 retains v5, then adds the v6 registrar/protection/signed-unissued fixtures (`launch-readiness-register.md:371-373`). The preceding inherited suite is still labelled “Matrix-v5 deletion fixtures” (`launch-readiness-register.md:369`). No operation is thereby authorized under v5, but relabelling it as inherited/current v6 coverage would make the re-distilled handoff unambiguous.

## V21 Critical/High Correction Audit

| Prior finding | v22 disposition |
| --- | --- |
| `C-v21-1` — guard blocker could lose after lookup to irreversible protection consume | **Closed.** Admission-integrity and capability-key blocks now race reserve/consume on one protection-owner state; `ConsumptionReserved` is the irreversible instant, exact lookup resolves loss, and unknown remains restrictive (`ARCHITECTURE-SPINE.md:263-265`; matrix rows 356,358-359). |
| `H-R21-1` — re-attestation and old-attestation consume lacked a common owner | **Closed for the first compromised key; VC22-H3 remains for compromise of the replacement key.** Re-attestation now begins only from protection-owned `ConsumptionBlocked`, remains blocked through replacement/dispatch, and activates at that same owner. |
| `H-v21-1` — signed-but-unissued attempt had no terminal/retry identity | **Closed at the attempt level; VC22-H2 remains at the enclosing batch identity.** Exact no-issue proof, terminal obsolete result, and incremented signing-attempt identity exist. |
| `H-v21-2` — Secrets revocation could not fit the closed principal model | **Core mechanism closed; VC22-H1 remains in conformance handoff.** The registrar is correctly non-public and outside the four principal kinds, but two current allowlist clauses still reject it. |
| `VC21-C1` — hold decision catalog excluded post-start destructive consumers | **Runtime catalog closed; VC22-H4 remains in Story 8.3 metadata.** The catalog baseline now enumerates post-start containment, dispatch, protection reservation, purge, completion, and Story 8.1/8.3 evaluations without selecting an outcome. |

## Authoritative 2026-09-12 Critical/High Closure Audit

| Authoritative finding | Current disposition |
| --- | --- |
| `C-1` weaker safety evaluation | **Closed.** Snapshot and every then-current applicable policy are conjunctive; dominance requires tested equivalence. |
| `C-2` hold/deletion exclusion | **Core mechanics closed, but final gate remains High at VC22-H2 through H4.** Admission/content fences, immutable owner cycles, all-or-none target batches, shared hold/seal ordering, post-start restrictive facts, and guard-linearized completion are explicit. The remaining findings concern identity/replacement ordering and a stale story scope, not a Product outcome. |
| `C-3` bootstrap/matrix deadlock | **Closed.** Matrix-v6 has explicit scopes, closed direct preconditions, and target-aware bootstrap/repair exceptions without locally inferred bypasses. |
| `H-1` scheduled Approver lifecycle | **Closed.** Single-flight ownership, cadence/freshness, two-pass evidence, leased resolution, typed results, and unavailable recovery are bound. |
| `H-2` distributed safety rescan | **Closed.** Epoch/index ownership, finite manifests, fenced bounded coordination, `RescanPending`, activation, and recovery are explicit. |
| `H-3` human-only Approvers | **Closed.** Parties-authoritative human classification, liveness, current authorization, and historical actor binding are required. |
| `H-4` ledger lifetime conflation | **Closed.** Rate, open-interaction, monthly Budget, and capacity owners/identities/lifetimes are separate; the unresolved concurrency-rejection choice is isolated and fail-closed. |
| `H-5` human identity | **Closed.** Stable `AuthenticatedHumanActorId` and historical Party binding support every human separation check; Workflow remains non-human. |
| `H-6` proposal/index crash consistency | **Closed.** Interaction truth, protected source-revision outboxes, directory high-waters, effect leases, acknowledgement, and reconciliation are explicit. |
| `H-7` indivisible Conversations dependency | **Closed.** Core `EXT-CONV-AI-1` and conditional `EXT-CONV-RETRACTION-1` have independent authority and consumers. |
| `H-8` trusted-envelope crypto/replay | **Closed for the original envelope protocol.** Canonical MAC inputs, logical/delivery identity, replay owner, time/key lifecycle, ACLs, security spool, and lost-ack recovery are bound. VC22-H1 concerns the later compromise registrar's stale conformance inventory. |
| `H-9` export bytes/lifecycle/signature | **Closed subject to the surfaced Product lifecycle decision.** Store/index ownership, immutable AEAD bytes, JCS/ES256 manifest, direct key delivery, hold/expiry fencing, cleanup, restore, and all-copy receipts are explicit. |
| `H-10` Dapr current exposure | **Closed as current-reality classification.** Parent-authoritative Client/ASP.NET `1.18.5`, non-authoritative checkout `1.18.7`, and future Workflow adoption remain distinct. |
| `H-11` public contracts falsely current | **Closed.** Required-completion vocabulary is separate from the explicit current delivery-debt ledger. |
| `H-12` sprint/dependency contradiction | **Closed as surfaced delivery-governance debt.** `OD-SPRINT-5.1-5.2-1` preserves the discrepancy without rewriting history or creating architecture authority. |

## Product Authority And Architecture Versus Delivery Debt

The bound FR-8 pre-Provider order remains exact: authorization; lifecycle/dependency; local block; provider/model; joint rate/open decision; Confirmation-mode Approver resolution; context; Budget; safety; membership (`prd.md:286-298`). The spine, conventions, matrix, and story map preserve that sequence, including regeneration. Initial-output safety status, rate/concurrency consumption, hold/deletion precedence, hold-prepare cancellation, operator cancellation/nonterminal handling, export lifecycle, historical safety, optional retraction, instruction protection, legacy plaintext disposition, class/range scope, human exact-Conversation scope, recorder scope, Dapr security, and sprint reconciliation remain explicit Open decisions with named owners and restrictive states. This review chooses none.

The target-versus-shipped boundary is accurate. Current Agents code imports EventStore client/domain-service and Conversations client contracts and registers `AddDaprClient`, but it does not implement Dapr Workflow, the architecture decision catalog, trusted-envelope replay/security spool, separated rate/open/Budget ledgers, safety epoch/index, interaction directory/effect leases, deletion scope guards/cycles, signed destruction capabilities, compromise registrar, export store, or guard-owned completion. The spine assigns those absences to stories and external prerequisites (`ARCHITECTURE-SPINE.md:1328-1350`). They are delivery debt and readiness blockers, not permission to weaken the target and not causes of VC22-H1 through VC22-H4.

## Verified Repository, Gitlink, Package, And Source Reality

- Root authority is commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`. Exact root gitlinks include Builds `a32cb422`, Conversations `73bcee6f`, EventStore `ce9e779a`, FrontComposer `053b2008`, Memories `3644ef63`, Parties `fa423985`, and Tenants `2fac1839`.
- Internally clean working-tree checkouts for Builds `cf52f74`, Conversations `64b05083`, EventStore `a568af4e`, FrontComposer `1b3608c9`, and Memories `42dfa26b` differ from their parent gitlinks; Parties and Tenants match. The spine correctly treats the parent gitlinks as repository authority and the checkout differences as local/non-authoritative reality.
- Root `global.json` pins SDK `10.0.401` with `latestPatch`; root build properties set `net10.0` and C# 14. Microsoft's official [.NET 10 release metadata](https://dotnetcli.blob.core.windows.net/dotnet/release-metadata/10.0/releases.json) identifies current release `10.0.12` and SDK `10.0.401` for 2026-09-08, so the Stack statement is current.
- The parent-authoritative Builds catalog pins Dapr Client/ASP.NET/Workflow `1.18.5`; its checked-out catalog pins `1.18.7`. The official [Dapr.Client NuGet index](https://api.nuget.org/v3-flatcontainer/dapr.client/index.json) exposes `1.18.7` as the latest stable with only `1.19.0-preview.*` afterward. The spine correctly classifies current Client/ASP.NET exposure and future Workflow adoption.
- Root and parent-authoritative Builds pin bUnit `2.9.0`; the separate checked-out Builds catalog and official [bUnit NuGet index](https://api.nuget.org/v3-flatcontainer/bunit/index.json) expose stable `2.10.3`, producing only VC22-L1.
- Every declared non-concurrent local source resolves except the two historical v21 paths in VC22-M4. The active v22 report paths are concurrent gate outputs, not authoritative inputs. All external dependency records remain `Uncommitted`, with unresolved targets/dates/commands where their owners have not committed them; no consumer is falsely ready-for-dev and `RQ-1` remains NOT READY.

## Good-Spine Checklist

| Dimension | Result |
| --- | --- |
| Real divergence points and enforceable rules | **Fail High.** VC22-H1 through VC22-H4 permit incompatible boundary, identity, state, or story-authority implementations. |
| Ownership and concurrency | **Fail High at VC22-H2/H3.** First-key block/reserve is closed; stale seal identity and replacement-key activation are not. |
| Security, tenancy, and irreversible data | **Fail High.** Tenant/target/manifest boundaries otherwise fail closed, so no Critical is assigned. |
| Recovery, replay, migration, and restore | **Fail High at VC22-H2/H3; Medium at VC22-M1.** Exact-result protocols otherwise converge. |
| PRD, epic, register, and convention traceability | **Fail High at VC22-H1/H4.** Runtime hold scope is correct, but Story 8.3 and the direct-mutation allowlist are not. |
| Product-decision discipline | **Pass.** Open choices are named, scoped, owned, and restrictive; none is invented here. |
| Architecture versus implementation debt | **Pass.** Target defects and absent shipped mechanisms are classified separately. |
| Brownfield and named technology truth | **Pass with Low maintenance tail.** Parent gitlinks, dirty checkout reality, current transitive Dapr exposure, SDK, and package versions are represented accurately. |
| Sources and mechanics | **Lint/AD pass; Medium source defect.** Two historical source paths are absent. |
| Altitude completeness | **Pass.** Deployment, dependency, security, data, operational, recovery, evidence, and governance dimensions are represented. |

## Gate Conclusion

The complete frozen v22 verified-current/bound-PRD reviewer gate is **FAIL**. Final counts are **Critical 0, High 4, Medium 4, Low 2**. Deterministic lint passes and AD-1 through AD-31 are stable. Correct VC22-H1 through VC22-H4 without inventing a Product outcome, reconcile the Medium/Low tail as appropriate, re-distill, and rerun the complete gate; PASS requires zero Critical and zero High.
