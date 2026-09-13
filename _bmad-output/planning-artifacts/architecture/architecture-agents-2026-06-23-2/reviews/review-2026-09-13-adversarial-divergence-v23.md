---
name: Hexalith Agents primary adversarial-divergence review v23
type: architecture-review
lens: adversarial-divergence
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-13
verdict: pass
critical: 0
high: 0
medium: 0
low: 0
lint_ok: true
---

# Primary Adversarial-Divergence Review — v23

## Verdict

**PASS — 0 Critical, 0 High, 0 Medium, and 0 Low findings.** Two independent implementations can now retain one accepted deletion-batch identity through any number of proven-obsolete pre-issue signatures, keep actual guard revisions as result evidence, and converge when a replacement signing key is itself compromised before or during protection-owner activation. The three pre-command primitives remain closed, and post-start legal-hold behavior remains safely blocked behind its Open Product decision rather than being selected by architecture.

## Frozen Snapshot And Method

The following inputs were SHA-256 checked before review and after this report was written:

- `ARCHITECTURE-SPINE.md`: `b382475bf559f9d6262273ca498df16197919fead492fd72f4aa80ac5b16c1d4`
- `IMPLEMENTATION-CONVENTIONS.md`: `ea1d311da8bef578c7fca428d6b66ef7ae91dc593eb5b3efc9cac382e2a938d9`
- architecture `.memlog.md`: `890d18b355b6b7d475241426641701ebe724dd6906bed15fa180a5e14a0e17e0` (**hash only; content was not read**)
- bound `prd.md`: `4fbc21e13301bb47c87e077ec7aa884ff698121837b0e19cbf6e0ea6e4334564`
- `epics.md`: `15f208cd8e2270dfb82352c5d7303b083ffd8f64c91ae40013a946fae6f69982`
- `external-dependency-register.md`: `3f29c4651eb79e475bd087fff402903652984218b1c66f1d26cd86dcfbb5a4f3`
- `launch-readiness-register.md`: `bd96ff74f3a6b3343b1a2f5922a9f7a65cefb7a1dd29fcff728714717ec1ca20`
- authoritative `VALIDATION-REPORT-2026-09-12.md`: `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`

The review reconstructed independent API/BFF, Workflow, domain, guard, `ProtectionFence`, protection engine, Secrets signer/revocation subscriber, host registrar, decision-catalog, migration, and restore units. It then raced literal implementations through authorization/effect/result boundaries, conditional-append loss, repeated stale issue attempts, signer loss, guard loss, compromise delivery and mirror loss, block/reserve/consume, replacement-key activation, post-start hold timing, cross-tenant substitution, migration cutover, and restore. It rechecked every v22 Critical/High and all authoritative Critical/High findings before seeking fresh divergence. No Product outcome was assumed.

Repository inspection was read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`; no submodule was initialized, updated, or modified by this review.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 0 |
| Medium | 0 |
| Low | 0 |

## Critical

No Critical findings.

## High

No High findings.

## Medium

No Medium findings.

## Low

No Low findings.

## Independent-Implementation Stress Results

### Stable accepted-batch identity across two or more stale pre-issue attempts

Converges. `ProtectionFence:AcceptDeletionInventory` assigns `DestructionSealId` once from the accepted deletion token, predicate digest, and accepted-manifest digest before any barrier authorization (`ARCHITECTURE-SPINE.md:239,567`). `DeletionDestructionBatchId` then derives from that stable seal id, batch kind/ordinal, and manifest digest rather than an anticipated guard revision (`ARCHITECTURE-SPINE.md:567`).

For a concrete three-attempt execution, team A and team B must both produce the following outcome:

1. Attempt 1 signs the stable seal/batch under signing attempt 1 and intended issue revision G11; another guard append wins, exact lookup proves no issue, and the attempt terminalizes as `SignedAttestationObsoleteUnissued`.
2. Attempt 2 keeps byte-identical seal/batch/manifest/attestation identity, increments only the signing-attempt ordinal, and binds a new intended revision; a second guard mutation wins and produces a second terminal obsolete attempt.
3. Attempt 3 again keeps the seal/batch identity, uses its own signing request, and wins the conditional issue append. Only its actual `CommittedDestructionSealGuardRevision` is stored as result evidence.

The signed closed field set includes stable `DestructionSealId`, `SigningAttemptOrdinal`, and `IntendedIssuedGuardRevision`; actual `CommittedIssuedGuardRevision`/`CommittedDestructionSealGuardRevision` is explicitly excluded from the seal id, batch id, signing request id, and signed bytes (`ARCHITECTURE-SPINE.md:260,268,571`; `IMPLEMENTATION-CONVENTIONS.md:29-31`; `external-dependency-register.md:199-201`; `launch-readiness-register.md:328-332,350,373`; `epics.md:3036-3037,3071`). Thus neither team can recompute the logical batch from the later physical event revision, revive an obsolete JWS, or mint a second accepted batch.

### Replacement-key compromise before or at activation

Converges at the protection owner. `DeletionBatchReattestationActivationId` binds batch, replacement attestation/key, successor dispatch, expected blocked-batch revision, and expected tenant-key block-set revision (`ARCHITECTURE-SPINE.md:571`). `ActivateReattestedDeletionBatch` atomically compares the batch and key-block set at that identity. If replacement key K2 has already been blocked or its block wins the compare, the exact result is `ActivationBlockedByReplacementKeyCompromise(K2, NewRevocationRevision, NewBlockReceiptId)` and the same logical batch remains or becomes the precise K2 `ConsumptionBlocked` state; activation cannot consume or clear the tenant key block (`ARCHITECTURE-SPINE.md:266,601`; `IMPLEMENTATION-CONVENTIONS.md:33`; `external-dependency-register.md:139,251`; `launch-readiness-register.md:359`).

A lost activation acknowledgement cannot split behavior: both teams retry the same activation id and receive the same activated-or-new-key-blocked result. A later re-attestation uses a healthy successor key and a new attestation ordinal while retaining the same seal/batch/manifest. If K2 compromise arrives after successful activation but before reservation, the registrar's K2 block and reservation still serialize at the same owner; the block winner prevents consumption, while a reservation winner returns its immutable outcome. Matrix-v7 fixtures explicitly cover every before/at/after boundary plus crash, loss, migration, and restore (`launch-readiness-register.md:373`; `IMPLEMENTATION-CONVENTIONS.md:57`; `epics.md:3071`).

### Post-dispatch cancellation, reservation, and crash recovery

Converges. Admission-integrity block, registrar key block, and `ReserveAndConsumeDeletionBatch` use the protection owner's closed local state machine. Only `Unconsumed` can transition to `ConsumptionReserved`, and that reservation is the irreversible point for these cancellation reasons. A block winner prevents destruction; a reserve winner recovers or returns the same all-or-none batch vector; unknown owner outcome remains restrictive (`ARCHITECTURE-SPINE.md:264-266`; `launch-readiness-register.md:356,358-359`; `external-dependency-register.md:251`). No guard read followed by an unconditional protection write can satisfy the contract.

### Legal hold before seal, after seal, and around dispatch

Converges without selecting Product precedence. Pre-seal registration/release and the destruction seal conditionally append on the same `GovernanceScopeGuard` revision, so a late pre-seal hold makes prior authorization stale (`ARCHITECTURE-SPINE.md:240,505`; `launch-readiness-register.md:278-279,349-351`). After sealing, `PostStartHoldContenderObserved` is durably ordered; every still-preventable containment issue, dispatch, consumption, purge, and completion blocks unless the exact effective deletion-allowed outcome is applied to that specific step. A dispatch that won first is uninterruptible only for its same batch, while the later hold remains restrictive elsewhere (`ARCHITECTURE-SPINE.md:254-256,262`; `launch-readiness-register.md:95,118,327,353-356,360-362`).

`OD-HOLD-DELETION-PRECEDENCE-1` remains Open. Its affected set includes both pre-seal contention and every post-start evaluation, and ordinary no-contender deletion remains evaluable. The architecture fixes ordering, applicability, evidence, and fail-closed behavior but supplies no Product outcome (`launch-readiness-register.md:95,112,118`; `epics.md:3072`).

### Exactly three pre-command primitives

Converges. AD-3 and AD-30 admit exactly:

1. replay nonce registration through `TrustedEnvelopeReplay`;
2. content-free security observation spooling/recording through `SecurityEventLog`; and
3. authenticated deletion-capability compromise registration through the non-public `IDeletionCapabilityCompromiseRegistrar`.

The registrar is target-limited to one tenant/key-version protection block before guard mirroring and has no public, human, Workflow, target-handler, or general-dispatcher route (`ARCHITECTURE-SPINE.md:276,574,601`; `external-dependency-register.md:139,201`; `launch-readiness-register.md:234,336,373`). `BlockDeletionBatchConsumption` and `ActivateReattestedDeletionBatch` are downstream protection-owner operations under already-recorded facts and target-limited Workflow capabilities, not extra raw EventStore/pre-command mutation paths (`ARCHITECTURE-SPINE.md:601`; `launch-readiness-register.md:358-359`). Their results are exact-lookup recoverable; ordinary aggregate mirrors remain subject to AD-3.

### Identity, tenant isolation, migration, and restore

Converges. All capability and activation identities include or inherit tenant-bound request/batch identity, the JWS binds `TenantId` and committed protection audience, and the registrar delivery binds issuer, audience, tenant, key family/version, revocation revision, trust-profile revision, event identity, and signature digest. Cross-tenant, changed-field, key-version, attestation, dispatch, and revision substitution reject (`ARCHITECTURE-SPINE.md:260,266,567,571,574`; `external-dependency-register.md:199-201,251`).

Migration/repair/restore preserves the stable seal and batch ids, intended and actual issue revisions as separate fields, every signing attempt and obsolete receipt, compromise delivery/block receipt, activation id and exact result, and every blocked/reserved/terminal protection state with its referenced guard attestation/dispatch. A successor cannot relabel identity, revive an obsolete signature or old dispatch, clear a replacement-key block, or turn unknown state into authority (`ARCHITECTURE-SPINE.md:601`; `external-dependency-register.md:139,201,251`; `epics.md:3071`).

### Decision catalog and branch-pinned recovery

Converges. Catalog activation remains an immutable expected-revision operation over exact required record revisions/digests; pending successors cannot replace effective blockers, reduce quorum/affected scope without predecessor authority, or strand a branch that already pinned its applicable decision versions (`launch-readiness-register.md:112,118`). Open records block only their exact affected evaluations. V23 changes technical identity and owner ordering and does not materialize any Product outcome (`epics.md:3072`).

## v22 Critical/High Recheck

| v22 finding | v23 disposition |
| --- | --- |
| H-v22-1 accepted batch identity included the changing actual seal revision | **Closed.** The accepted inventory now assigns stable `DestructionSealId` before signing; BatchId and JWS use it, while the eventual actual guard issue revision is separately named result evidence and excluded from identity/signed bytes. Multiple obsolete attempts have distinct signing-request identities without changing the batch (`ARCHITECTURE-SPINE.md:239,260,268,567,571`; `launch-readiness-register.md:328-332,350,373`). |
| H-v22-2 replacement-key compromise could race activation without a new-key block check | **Closed.** Activation binds and atomically compares the tenant-key block-set revision. A K2 block returns the exact typed K2-blocked result and preserves the same batch for another re-attestation; activation cannot clear the block (`ARCHITECTURE-SPINE.md:266,571`; `launch-readiness-register.md:359,373`; `external-dependency-register.md:251`). |

The former v22 Medium orphan state is also closed: `ReattestedAwaitingDispatch` no longer appears in the reviewed contracts. The protection state remains `ConsumptionBlocked` throughout replacement signing and successor dispatch and changes only at protection-owner activation (`ARCHITECTURE-SPINE.md:264-266`; `IMPLEMENTATION-CONVENTIONS.md:33`).

## Authoritative Validation Critical/High Recheck

| Authoritative finding | Current disposition |
| --- | --- |
| C-1 weaker safety evaluation | **Closed.** AD-20 requires the same exact content to pass both snapshot policies and every then-current applicable policy; collapse requires a machine-checkable dominance proof. Retry identity and terminal/no-use settlement remain explicit (`ARCHITECTURE-SPINE.md:409,481`). |
| C-2 hold/deletion full rejection | **Closed.** Scope admission/content guards, `GovernanceScopeGuard` hold/seal serialization, protection-owner block/reserve ordering, target coverage, and completion seal form an executable fail-closed protocol for both deletion origins (`ARCHITECTURE-SPINE.md:239-270,505`). |
| C-3 matrix bootstrap/scope deadlock | **Closed.** Matrix v7 preserves typed gate-free bootstrap/repair direct preconditions, platform-vs-tenant evaluation, no local fallback, and broken-empty-state fixtures (`ARCHITECTURE-SPINE.md:445,463`; `launch-readiness-register.md:180,208,369`). |
| H-1 scheduled Approver recheck | **Closed.** The scheduled and event-driven recheck has a recorded resolution lifecycle and terminal behavior. |
| H-2 distributed safety rescan | **Closed.** The spine binds finite authoritative enumeration, leases/checkpoints, call blocking, retry/recovery, and evidence. |
| H-3 non-human Approvers | **Closed.** Eligible Approvers carry human Party and stable authenticated-human identity; automatic mode does not invent an Approver (`ARCHITECTURE-SPINE.md:288,351`). |
| H-4 conflated ledger lifetimes | **Closed.** `RateLimitLedger`, `OpenInteractionLedger`, and `BudgetLedger` own distinct rolling, concurrency, and monetary lifetimes (`ARCHITECTURE-SPINE.md:402`). |
| H-5 missing human identity | **Closed.** Human origin, authenticated actor, Party, binding version, and separation-of-duty evidence remain distinct through trusted commands and durable records (`ARCHITECTURE-SPINE.md:351,567`). |
| H-6 crash-inconsistent proposal index | **Closed.** The Conversation interaction directory, protected outboxes, target-authorized proposal mutations, leases, acknowledgements, and Closing manifest provide recovery authority. |
| H-7 indivisible Conversations dependency | **Closed.** Context, membership, posting/existence, AI-participant mutation, and source-deletion delivery are separate committed/deferred seams with fail-closed package/source behavior (`src/Hexalith.Agents.Server/Hexalith.Agents.Server.csproj:38-56`; `external-dependency-register.md`, `EXT-CONV-AI-1`). |
| H-8 underspecified trusted envelope | **Closed.** AD-30 fixes canonical HMAC input, issuer/audience/tenant/target/operation binding, nonce registration before dispatch, replay result, key rotation/retention/revocation, and ACL. |
| H-9 export package protection | **Closed.** The target contracts bind protected artifact/key owners, signed evidence, requester/audience capability, hold/deletion lifecycle, exact release, and fail-closed lookup. |
| H-10 Dapr described as future only | **Closed.** The spine records current transitive Dapr Client/ASP.NET exposure at root-authoritative Builds `1.18.5`, separately marks Workflow unconsumed, and tracks the upgrade/exception debt (`ARCHITECTURE-SPINE.md:905-907,1349,1396`; `src/Hexalith.Agents.Server/Hexalith.Agents.Server.csproj:17-22`; `src/Hexalith.Agents.Server/obj/project.assets.json`). |
| H-11 backlog vocabulary claimed shipped | **Closed.** Missing protocol/public vocabulary is explicitly implementation debt owned by named stories, not present parity (`ARCHITECTURE-SPINE.md:431,1338-1339`). |
| H-12 tracker contradicted dependency/evidence authority | **Closed.** Readiness, external-register status, story evidence, and seam flips have one explicit authority/order; uncommitted/TBD evidence stays blocking. |

## Product Decisions And Implementation Debt

No reviewed correction chooses an unresolved Product outcome. The stable 16 `OD-*` ids match between spine and launch register. In particular, hold/deletion precedence, operator deletion cancellation/nonterminal disposition, human exact-Conversation scope, class/range scope, initial output-safety public status, export lifecycle, and legacy plaintext disposition remain Open and restrictive only where applicable.

The target architecture is substantially ahead of the code. Focused source/package search found none of the v23 stable-seal, obsolete-signature, activation-blocked, protection-owner batch-state, compromise-registrar, or matrix-v7 constructs. `EXT-HOST-1`, `EXT-SECRETS-1`, `EXT-PROTECTION-1`, `EXT-CONV-AI-1`, Provider, and export seams remain Uncommitted/TBD. These are delivery/integration debt and launch blockers, not evidence that the target contract is optional or a current architecture defect.

The root-declared gitlinks are Builds `a32cb422749352cce8dec948aa3e78c8f00eb4cf`, Conversations `73bcee6f04479d4743d5a65ce929728e22687d7d`, EventStore `ce9e779a3ec24d9b9a054afd0ceaa3c05b8adaaa`, FrontComposer `053b2008307d4e476c0d4329e6c47763c301d43e`, Memories `3644ef63da87b8c7ec9e17726fae7f534e642a1c`, Parties `fa42398552fba1c80eb2760791517659d6d1313a`, and Tenants `2fac18396ff11a4459de053b3ebb7ddfe7c13e30`. The checked-out Builds `cf52f74`, Conversations `64b0508`, EventStore `a568af4`, FrontComposer `1b3608c`, and Memories `42dfa26` differ from their root gitlinks; Parties and Tenants match. The spine correctly treats the parent gitlinks as authority and the alternate checked-out states as uncommitted repository reality. This review did not bless or mutate that drift.

## Gate Result

The v23 primary adversarial-divergence gate is **PASS** with **Critical 0 / High 0 / Medium 0 / Low 0**. Deterministic lint: **PASS**, `ok: true`, zero findings. AD check: **PASS**, 31 unique contiguous IDs, AD-1 through AD-31. Open-decision check: **PASS**, 16 stable ids with no spine/register difference. Frozen-input after-write hash verification: **PASS**; all eight hashes remain exactly the values listed above, and the memlog was not read.
