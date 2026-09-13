---
name: Hexalith Agents adversarial-divergence review v22
type: architecture-review
lens: adversarial-divergence
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-13
verdict: fail
critical: 0
high: 2
medium: 1
low: 0
lint_ok: true
---

# Adversarial Divergence Review — v22

## Verdict

**FAIL — 0 Critical, 2 High, 1 Medium, and 0 Low findings.** V22 closes the v21 irreversible consume race and compromise-ingress authority defect: canceling facts now race reservation at one protection owner, and the Secrets notification uses a closed non-public registrar. It also adds the missing signed-but-unissued terminal path. Two independently compliant implementations can still diverge on the accepted-batch identity after a stale pre-seal signing attempt and on activation when a replacement key is compromised before activation. The remaining Medium is an unreachable/contradictory named protection state. Deterministic architecture lint passed with `ok: true` and zero findings.

## Frozen Snapshot And Method

The following inputs were SHA-256 checked before review and after this report was written:

- `ARCHITECTURE-SPINE.md`: `38db1ac6d9fe9cc1155ec042e8217ed2395d9c4399a2ac0e4d4e70c890ad1fa8`
- `IMPLEMENTATION-CONVENTIONS.md`: `ec646e305acfe24534333e1db80aa4e9a6cd613561b84ab9752f50c846506f29`
- architecture `.memlog.md`: `907d88500cd5d66b4e0d7a818944d2f4ef34d7faacc3e416dde2ea7550d95dfb` (**hash only; content was not read**)
- bound `prd.md`: `48ba04a4d89441fe9c6da95a114c307f2206e73eb42bda06635e4ba03073beee`
- `epics.md`: `97dcb91fbd6e2418bb17f4c81303285d364d40a8071c1c42bc2be9ce4179d6de`
- `external-dependency-register.md`: `78ecf69d009f828fb92da1f27c209a2f54676a9b4a2e39ff0fdf68eadd3206c4`
- `launch-readiness-register.md`: `2f568fe50edf567cd7b39d24fa3ac43e077839ffe561a00874cb271e65198b8d`
- authoritative `VALIDATION-REPORT-2026-09-12.md`: `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`

The review reconstructed independent guard, `ProtectionFence`, deletion Workflow for both origins, protection engine, Secrets signer/revocation publisher, host composition/registrar, migration/restore, decision catalog/projection, and recovery units. It executed literal interleavings for block delivery versus reserve/consume; guard mirror loss; issue/sign/result versus a stale guard revision; obsolete-signature proof and successor identity; routine and emergency key changes; re-attestation, successor dispatch, activation, and a second compromise; holds before/after dispatch/reservation; catalog activation; crash/lost acknowledgement; cross-tenant substitution; and migration/restore. It rechecked every authoritative Critical/High and every v21 Critical/High before seeking new divergence. No Product outcome was assumed.

Repository inspection was read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`; no submodule was initialized, updated, or mutated. Root gitlinks remain Builds `a32cb422749352cce8dec948aa3e78c8f00eb4cf`, Conversations `73bcee6f04479d4743d5a65ce929728e22687d7d`, EventStore `ce9e779a3ec24d9b9a054afd0ceaa3c05b8adaaa`, FrontComposer `053b2008307d4e476c0d4329e6c47763c301d43e`, Memories `3644ef63da87b8c7ec9e17726fae7f534e642a1c`, Parties `fa42398552fba1c80eb2760791517659d6d1313a`, and Tenants `2fac18396ff11a4459de053b3ebb7ddfe7c13e30`. The first five checked-out revisions differ from those gitlinks; Parties and Tenants match. Focused source search found none of the v22 guard/capability state names. This is delivery evidence only and does not redefine the target.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 2 |
| Medium | 1 |
| Low | 0 |

## Critical

No Critical findings.

## High

### H-v22-1 — The accepted batch must remain identical after a stale signing attempt, but its identity includes the seal revision that the retry changes

**Classification:** target-architecture deterministic identity, stale recovery, and cross-team interoperability defect.

**Evidence.** V22 correctly gives each pre-issue signature `SigningAttemptOrdinal` plus `IntendedIssuedGuardRevision`, records a losing attempt as `SignedAttestationObsoleteUnissued`, and says the successor keeps the same batch/manifest/attestation ordinal while changing only signing attempt, intended guard revision, and possibly the current healthy key (`ARCHITECTURE-SPINE.md:267,570`; `launch-readiness-register.md:328-331`). But `DeletionDestructionBatchId` is still derived from `DestructionSealRevision`, and the JWS signs that field separately from `IntendedIssuedGuardRevision` (`ARCHITECTURE-SPINE.md:259,566`). No contract defines `DestructionSealRevision` as a stable pre-seal logical ordinal independent of the actual `DestructionSealed` guard revision. The accepted issue operation is the seal itself; when a hold or other guard append makes attempt 1 stale, the successful seal necessarily occupies a later guard revision (`launch-readiness-register.md:349-351`).

**Literal interleaving.** At guard G10, Team A computes accepted BatchId B with `DestructionSealRevision=G11` and signs attempt 1 for intended issue G11. A hold wins G11; barrier issue returns stale. The successor can seal only at G12. Team A obeys “same batch” and keeps B/`DestructionSealRevision=G11`, treating the field as a logical revision even though no seal exists there. Team B treats it as the actual seal event revision, recomputes B2 for G12, and violates the explicit same-batch successor rule. Team C keeps B but signs `DestructionSealRevision=G12`, which changes a component of B without changing B. All can point to literal requirements; their JWS, exact lookup, post-start hold evidence, and protection idempotency keys are incompatible.

**Impact.** Any ordinary guard mutation after signing can wedge accepted deletion or make teams mint incompatible batch identities. A permissive escape weakens deterministic idempotency; a strict implementation can never complete the successor seal. This is High.

**Required correction.** Define a stable, once-assigned pre-seal identity component (for example `DestructionSealOrdinal` or `AcceptedBatchAuthorityId`) and use it consistently in BatchId/JWS, while storing the actual successful `GovernanceScopeGuard` event revision separately. Alternatively, explicitly let a proven-obsolete pre-issue accepted BatchId change and bind the supersession chain, while still allowing only one guard-issued batch. Specify the meaning/owner of every revision field and add a fixture in which multiple guard mutations force two or more signed-obsolete attempts before one accepted seal.

### H-v22-2 — Re-attestation activation can race a compromise of the replacement key without a required new-key block check

**Classification:** target-architecture security recovery and protection-owner state-transition defect.

**Evidence.** A compromise block is per tenant/key version and is installed at `EXT-PROTECTION-1` before its guard mirror (`ARCHITECTURE-SPINE.md:265,574`; `launch-readiness-register.md:234,336`). Re-attestation keeps the batch blocked while the guard activates a healthy replacement attestation and writes a successor dispatch; `ActivateReattestedDeletionBatch` then reopens the local batch (`ARCHITECTURE-SPINE.md:265`; `launch-readiness-register.md:337-359`). The activation row requires the old exact compromise block, guard replacement/dispatch, expected blocked revision, and no admission/terminal state, but it does **not** require absence of a protection-owner tenant/key-version block for the *replacement* attestation (`launch-readiness-register.md:359`). This omission matters because the second registrar can install that replacement-key block before its guard mirror, so the guard can still appear healthy. `ReserveAndConsumeDeletionBatchEffect` later checks the tenant key-version block, preventing immediate destruction, but the architecture no longer says whether activation must fail, remain old-key blocked, or transition to a new-key block.

**Literal interleaving.** K1 is compromised and batch B is `ConsumptionBlocked(K1,R1)`. The guard re-attests B under K2 and commits successor dispatch D2. Before activation, K2 compromise delivery installs protection block BK2; its guard mirror is delayed. Team A evaluates the listed activation preconditions, sees the exact K1 batch revision and a guard-healthy K2/D2, and changes B to `Unconsumed(K2,D2)`. Team B consults BK2 and rejects activation. Team C changes B directly to `ConsumptionBlocked(K2,R2)`. A later reserve blocks in all careful implementations, but Team A no longer has the exact per-batch K2 block required to re-attest again, while Team B/C expose different authoritative states and recovery evidence.

**Impact.** A second emergency compromise can permanently strand an immutable sealed batch or produce conflicting blocked/active state across protection, guard, workflow, migration, and restore. An implementation that treats activation as consuming/clearing the global block can also weaken the security boundary. This is High.

**Required correction.** Make activation atomically require `NoTenantKeyVersionBlockForReplacementAttestationAtProtectionOwner` at the same local revision, and define a typed losing outcome when a newer key block wins. The winning newer block must yield the exact per-batch `ConsumptionBlocked(CapabilityKeyCompromise, NewRevision)` state needed for another same-batch re-attestation; it must never be overwritten by activation. Bind the replacement key version/block revision into activation identity and add second-compromise races before guard re-attest, before successor dispatch, before/at activation, and after activation but before reserve, including lost mirror and restore.

## Medium

### M-v22-1 — `ReattestedAwaitingDispatch` is declared as a protection-owner state but no legal transition enters or leaves it

**Classification:** target-document state-machine inconsistency.

The closed state list includes `ReattestedAwaitingDispatch` (`ARCHITECTURE-SPINE.md:263`), while the normative replacement sequence says the batch remains `ConsumptionBlocked` through guard re-attestation and successor dispatch, then activation changes it directly from that exact block to `Unconsumed` (`ARCHITECTURE-SPINE.md:265`; `launch-readiness-register.md:337-359`; `IMPLEMENTATION-CONVENTIONS.md:33`). No matrix operation creates `ReattestedAwaitingDispatch`, and activation rejects anything other than the exact compromise-blocked state. Remove the unused state or define its owner transition, expected revision, recovery, and activation precondition. Otherwise one protection implementation will never emit it while another will enter it after guard replacement and then be unable to satisfy the literal activation row.

## v21 Critical/High Recheck

| v21 finding | v22 disposition |
| --- | --- |
| C-v21-1 guard blocker could win after lookup but lose to irreversible consumption | **Closed.** `BlockDeletionBatchConsumption`/registrar key block and `ReserveAndConsumeDeletionBatch` now contend on one protection-owner local transition; `ConsumptionReserved` fixes the cancel-versus-consume order, unknown remains restrictive, and recovery finishes only the same reservation (`ARCHITECTURE-SPINE.md:263-265`; `external-dependency-register.md:251`; `launch-readiness-register.md:356-359`). |
| H-v21-1 signed-but-unissued stale result had no retry | **Closed at protocol level.** Exact no-issue proof terminalizes the signed attempt, and a successor signing identity binds a new signing-attempt ordinal and intended revision (`ARCHITECTURE-SPINE.md:267,570`; `launch-readiness-register.md:328-334`). H-v22-1 is the newly exposed contradiction in the accepted batch's enclosing seal-revision component. |
| H-v21-2 Secrets compromise notification had no valid principal/capability | **Closed.** `IDeletionCapabilityCompromiseRegistrar` is a non-public AD-3 primitive with closed authenticated fields, tenant/key target, protection-before-guard ordering, exact replay/lost-ack lookup, and ACL denial to public/human/Workflow/general-dispatcher paths (`ARCHITECTURE-SPINE.md:275,574`; `IMPLEMENTATION-CONVENTIONS.md:43`; `launch-readiness-register.md:234,336`). |

## Required Stress-Path Results

- **Block delivery versus reserve/consume:** converges. A delivered protection block and reserve race one state transition. A lost/late block can legitimately return reserved/consumed; an installed block prevents old-attestation reservation; unknown remains pending.
- **Block/re-attest/dispatch/activation:** converges for one compromise, but a second compromise of the replacement key exposes H-v22-2.
- **Signed obsolete/successor:** attempt/result/obsolete lookup converges for containment and for an accepted batch only if `DestructionSealRevision` is given one stable interpretation; H-v22-1 is the unresolved accepted identity.
- **Registrar replay, ACL, and tenant routing:** converges. Authenticated fields, per-tenant key family, protection-first effect, exact event/block identity, cross-tenant rejection, and capability confinement are explicit.
- **Post-start hold catalog materialization:** converges without choosing an outcome. The catalog baseline enumerates containment, dispatch, protection reservation, purge, completion, and StoryAuthorization consumers; ordinary no-contender deletion remains evaluable. A post-start contender is a new recorded branch that may bind its own then-effective exact outcome, after which that branch—not an earlier Open non-applicable record—is phase-pinned (`launch-readiness-register.md:91-124`).
- **Crash/lost acknowledgement/migration/restore:** converges except for the identities/states in H-v22-1/H-v22-2/M-v22-1. The v22 addendum preserves signing attempts, obsolete receipts, registrar delivery/block receipts, batch-block pending facts, and protection states; old dispatch/credentials cannot revive.

## Authoritative Validation, Product Decisions, And Debt

The authoritative report's C-1 through C-3 and H-1 through H-12 remain closed in the target: conjunctive current/snapshot safety, shared hold/deletion fencing, scoped matrix-v6 bootstrap, scheduled Approver resolution, finite safety rescans, human identity, distinct rate/open/Budget lifetimes, crash-consistent proposal/source/user-action delivery, dependency split, trusted-envelope replay, export ownership, current Dapr reality, public-contract debt language, and readiness/tracking authority remain explicit. All AD-1 through AD-31 are present exactly once in ascending order. The 16 stable `OD-*` ids in the spine and launch register match exactly.

No finding requires choosing an unresolved Product result. Hold/deletion precedence, operator Abort/nonterminal handling, human exact-Conversation semantics, class/range scope, initial output-safety status, export lifecycle, legacy plaintext disposition, and other Open records remain restrictive. H-v22-1 and H-v22-2 are technical identity/state fixes; M-v22-1 is an internal handoff correction.

Current code does not implement the v22 guard, signed-capability, protection-owner, registrar, deletion-directory, decision-catalog, or completion protocols. `EXT-HOST-1`, `EXT-SECRETS-1`, `EXT-PROTECTION-1`, `EXT-CONV-AI-1`, `EXT-PARTIES-1`, Provider, and export seams remain Uncommitted/TBD. The spine describes those absences as target implementation/integration debt and does not claim they are shipped. Current transitive Dapr exposure and the first five checked-out submodules differing from parent gitlinks remain repository reality, not target authority.

## Gate Result

The v22 adversarial-divergence gate is **FAIL** because High findings remain. Deterministic lint: **PASS**, `ok: true`, zero findings. AD check: **PASS**, 31 unique contiguous IDs, AD-1 through AD-31. Open-decision check: **PASS**, 16 stable ids with no spine/register difference. Frozen-input after-write hash verification: **PASS**; all eight hashes remain exactly the values listed above, and the memlog was not read.
