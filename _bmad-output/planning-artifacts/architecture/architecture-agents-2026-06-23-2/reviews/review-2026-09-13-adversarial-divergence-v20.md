---
name: Hexalith Agents adversarial-divergence review v20
type: architecture-review
lens: adversarial-divergence
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-13
verdict: fail
critical: 1
high: 3
medium: 1
low: 0
lint_ok: true
---

# Adversarial Divergence Review — v20

## Verdict

**FAIL — 1 Critical, 3 High, 1 Medium, and 0 Low findings.** The v20 compare-token, gap-chain, guard-owned containment-issuance, and non-expiring batch changes close all four v19 findings as written. The fresh whole-package pass found one unresolved Product boundary at post-start containment and three implementation-divergence gaps involving destructive-capability trust, ordinal attribution, and repeated destruction of one target alias. Deterministic architecture lint passed with `ok: true` and zero findings.

## Frozen Snapshot And Method

The following inputs were SHA-256 checked before review and after this report was written:

- `ARCHITECTURE-SPINE.md`: `ec53b5116c56af2bb87c52002b1906d60189d2e738cbc038d165a11aeeceb0fd`
- `IMPLEMENTATION-CONVENTIONS.md`: `6f2503b8e31a357ee0d91b0bd754e067e7f364590b2ff741fa9a639d2856ad80`
- architecture `.memlog.md`: `e9da3dce567da7458dc499ca3c6a8b55891e864dafda09d6ef9a4084852d4510`
- bound `prd.md`: `2653ad3e683ba2d4901f4e29c073c29603b6110be079cfc7c3c92b53a6c323a9`
- `epics.md`: `533b33d9eaebd5a2ef3cdb6901237dff41a41c2ac44ca1326a69645e25c4c0ef`
- `external-dependency-register.md`: `54fa2073f583bdaa444e3b193e188963547482a04c5e33fbe1fc9b3fb960a734`
- `launch-readiness-register.md`: `ecb205005322a91aae24718c643861231d730f79a5fdb1676f42bb77ccc1e6f9`
- authoritative `VALIDATION-REPORT-2026-09-12.md`: `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`

The review reconstructed independent API, Interaction Workflow, directory/effect-authorization, admission/content-guard, `ProtectionFence`, `GovernanceScopeGuard`, hold, both deletion-origin, migration/repair, secrets/capability-signing, protection-engine, and crash/lost-ack recovery units. Each unit had to obey the literal spine, conventions, bound PRD, epics, dependency contracts, and matrix-v4 rows without inventing a field, trust anchor, owner, result class, or Product decision. The units were raced around guard observation versus fence authorization; hold registration/release versus conditional seal; stale-result recording and reauthorization; initial and repeated admission ordinals; two or more violations before content binding; containment allocation/issue/stale/lost-ack; capability-key rotation, compromise, restore, consumption, exact retry, and revocation; duplicate aliases; and migration successor installation. All authoritative Critical/High findings and every v19 finding were rechecked before seeking new divergence.

Repository inspection was read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`; no submodule was initialized, updated, or mutated. Root gitlinks remain Builds `a32cb422749352cce8dec948aa3e78c8f00eb4cf`, Conversations `73bcee6f04479d4743d5a65ce929728e22687d7d`, EventStore `ce9e779a3ec24d9b9a054afd0ceaa3c05b8adaaa`, FrontComposer `053b2008307d4e476c0d4329e6c47763c301d43e`, Memories `3644ef63da87b8c7ec9e17726fae7f534e642a1c`, Parties `fa42398552fba1c80eb2760791517659d6d1313a`, and Tenants `2fac18396ff11a4459de053b3ebb7ddfe7c13e30`. The first five checked-out revisions differ from those gitlinks; Parties and Tenants match. That dirty checkout is implementation evidence only and does not redefine the target contract.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 3 |
| Medium | 1 |
| Low | 0 |

## Critical

### C-v20-1 — A post-start hold can be pending on an unresolved Product outcome while a newly minted containment capability destroys content not covered by the original seal

**Classification:** target-architecture legal-hold, irreversible-deletion, and unresolved-Product-decision leakage defect.

**Evidence.** The hold protocol says that when sealing wins first, hold registration returns `DestructionAlreadySealed` and the hold advances only under the approved post-start outcome (`ARCHITECTURE-SPINE.md:481`; `launch-readiness-register.md:275-277`; `epics.md:2840`). No such outcome is approved: `OD-HOLD-DELETION-PRECEDENCE-1` remains Open and its stated subject/safe state primarily covers a registered contender against an armed but unsealed deletion (`ARCHITECTURE-SPINE.md:1262`; `launch-readiness-register.md:95`). V20 separately permits a content target discovered after sealing to acquire a *new* containment ordinal and a newly issued destructive capability (`ARCHITECTURE-SPINE.md:232,554`; `launch-readiness-register.md:323-327`). Neither `AuthorizeDeletionContainmentBatch` nor `CommitDeletionContainmentBatchEffect` checks `DestructionAlreadySealed` hold results, a pending post-start hold outcome, an effective decision version/outcome, or absence of an overlapping post-start hold (`launch-readiness-register.md:324-325`). The accepted batch was phase-authorized at sealing, but the later containment batch did not exist then and is preventable new authority.

**Two literal units.** Seal wins at guard revision G10. A hold then arrives, receives `DestructionAlreadySealed`, and remains pending because Product has approved no post-start outcome. A later in-predicate content violation receives containment ordinal C1. Team A treats all containment as recovery of the already-started deletion and issues/consumes C1 despite the pending hold. Team B treats the new capability as a new irreversible decision and blocks it until the post-start hold outcome explicitly permits deletion. Team C lets the hold proceed locally as rejected because sealing won. The current matrix authorizes Team A, the hold rows forbid Team C, and the spine's “only approved post-start outcome” supports Team B; the artifacts do not converge.

**Impact.** Team A can irreversibly destroy newly discovered content while the architecture says the overlapping hold's post-start behavior has no approved outcome. This chooses “deletion wins for future containment targets” on Product's behalf and can violate legal-hold intent. Because the effect is irreversible and preventable, this is Critical.

**Required correction — do not choose the Product outcome.** Clarify `OD-HOLD-DELETION-PRECEDENCE-1`'s post-start applicability or create a separate named Product decision if the existing one deliberately excludes it. Until an effective outcome exists, block authorization/issuance/consumption of every *new* containment batch overlapping an unresolved `DestructionAlreadySealed` hold result. Preserve exact already-consumed results and define whether the already-issued accepted batch may continue as phase-pinned recovery; that distinction itself must come from the approved decision contract. Bind the selected outcome/version and hold result to containment authorization, guard issue, protection consumption/recovery, completion, migration, and before/after fixtures.

## High

### H-v20-1 — The non-expiring destructive capability has a key version but no signing authority, trust-anchor, rotation, or compromise protocol

**Classification:** target-architecture destructive-capability trust and key-lifecycle defect.

**Evidence.** `GovernanceScopeGuard` returns signed, non-expiring accepted and containment capabilities carrying a `CapabilityKeyVersion`; that version must remain verifiable for the full batch/outcome retention and there is no renewal branch (`ARCHITECTURE-SPINE.md:232,554,1163-1171`; `IMPLEMENTATION-CONVENTIONS.md:25`; `external-dependency-register.md:233`). However, neither AD-2 nor the dependency registers identify who owns the private signing key, which dependency publishes the trust anchor, how routine key rotation chooses the issuance version, or what an emergency compromise does to unconsumed capabilities. `EXT-SECRETS-1` enumerates Provider, KEK/DigestKey, migration/directory capability, manifest, trusted-envelope, and decision-verification key material, but not a GovernanceScopeGuard destruction-capability signing family (`external-dependency-register.md:187`). The only batch-revocation row requires a recorded post-seal *admission-integrity* compromise; it does not authorize revocation because a capability key was compromised (`launch-readiness-register.md:344`).

**Two literal units.** Team A reuses the tenant migration-capability key and its revocation profile. Team B gives EventStore a new private signing key and has the protection engine trust an out-of-band public key. Team C uses a protection-engine MAC. After routine rotation, all can retain the old verifier; after emergency compromise, Team A rejects all old versions, stranding exact retry, while Team B continues verifying indefinitely as required and permits newly forged capabilities unless it invents online guard lookup. Team C cannot interoperate with either signature format. All can claim a `CapabilityKeyVersion` and retained verification.

**Impact.** Independently built guard, host, secrets, and protection units cannot authenticate the same capability or agree on rotation/compromise behavior. The fail-closed form wedges sealed deletion; the permissive form leaves a non-expiring destructive grant forgeable after key compromise. This is High.

**Required correction.** Bind the key family and owner, algorithm/canonical signed bytes, issuer/audience, trust-anchor distribution, issuance-version selection, routine rotation overlap, retention, and emergency compromise behavior. If compromise revokes a version, define an owner-linearized enumeration/revocation of every unconsumed batch under it and a safe reauthorization/reissue rule that keeps immutable batch/manifest identity and cannot create two consumable grants; otherwise require online exact guard-issued-state validation at protection consumption. Extend `EXT-SECRETS-1`, `EXT-HOST-1`, `EXT-PROTECTION-1`, matrix rows, migration/restore, and consume-versus-revoke/rotate fixtures.

### H-v20-2 — A late accepted write can be attributed to its stale ordinal or the current installed ordinal, so current-zero evidence can hide it

**Classification:** target-architecture admission-integrity partition and successor-recut defect.

**Evidence.** The request ledger is partitioned by `AdmissionFenceOrdinal`; prior partitions remain visible, while destructive progress consults only the current ordinal's `ZeroAcceptedViolationsSinceInstall` (`ARCHITECTURE-SPINE.md:228`; `IMPLEMENTATION-CONVENTIONS.md:23`; `launch-readiness-register.md:298,310,338-339`). Containment takes an exact fence id/ordinal receipt and assigns that ordinal plus one (`launch-readiness-register.md:321`). The artifacts never specify whether a delayed or stale-writer violation is recorded under the authorization/fence ordinal carried by the violating write, the ordinal current at acceptance, or the ordinal current when detection/containment is recorded. This matters after ordinal N+1 is installed while an old-N writer, restored node, or delayed detection surfaces another accepted matching append.

**Two literal units.** Ordinal 2 is installed and has a clean proof. A restored writer carrying ordinal-1 evidence is nevertheless accepted. Team A records the violation in immutable partition 1 because that is the receipt the writer violated; “next ordinal” is therefore 2, already installed, and the current ordinal-2 zero remains clean. Team B attributes it to the fence active at the acceptance instant, invalidates ordinal 2, and assigns 3. Team C records it in both partitions. The successor/gap-chain and candidate/barrier evidence differ, and Team A may seal from a proof Team B rejects.

**Impact.** Current-zero can cease to mean “no matching admission after this installation,” or recovery can wedge trying to assign an already-existing successor ordinal. If the stale acceptance is discovered only after batch consumption, the wrong partition cannot restore destroyed content. This is High.

**Required correction.** Define one authoritative acceptance-time ordinal supplied by the EventStore guard, independently of the stale caller's evidence and later detection time. Every accepted matching append must durably contaminate the ordinal that was current at its acceptance linearization, or atomically invalidate the current-and-later proof chain under an equivalent rule. Bind `AcceptedAtGuardHighWater`, current ordinal, violating caller ordinal, containment successor calculation, delayed detection, restore, and concurrent install. Tests must cover an ordinal-N credential/write accepted before, at, and after N+1 installation and discovered before/after its zero proof.

### H-v20-3 — Distinct post-seal violations sharing one interaction DEK alias have no canonical containment identity or already-destroyed outcome

**Classification:** target-architecture containment identity and protection-retry defect.

**Evidence.** Protection uses one DEK per `AgentInteraction`, while an interaction can contain multiple protected resource writes (`ARCHITECTURE-SPINE.md:209,219`; `external-dependency-register.md:233`). V20 allocates one singleton target-alias manifest per authoritative post-seal content violation, says an “exact duplicate” reuses its ordinal, and gives distinct resources/targets distinct ordinals/batches (`ARCHITECTURE-SPINE.md:232`; `launch-readiness-register.md:323-327`; `epics.md:3027`). It does not define whether two different violation receipts/resources mapped to the same `(TenantId, AgentInteractionId, TargetProtectionKeyAlias)` are duplicates at the containment layer. Nor does `DestroyDekManifest` define the result of a new BatchId targeting a key already destroyed by the accepted batch or an earlier containment batch; its exact retry rule applies only to the same BatchId (`external-dependency-register.md:233-236`; `launch-readiness-register.md:342-343`).

**Two literal units.** Two late protected writes W1 and W2 use the same interaction key alias. Team A deduplicates by key target, reuses C1/batch B1 for W2, and records both resources as covered. Team B deduplicates only an identical violation observation, allocates C1/B1 and C2/B2, then treats B2's already-destroyed target as a successful `AlreadyDestroyed` receipt. Team C rejects B2 because “destroy every listed DEK” cannot newly destroy an absent key, leaving completion pending. All obey singleton manifests and same-Batch exact retry but expose incompatible evidence and terminality.

**Impact.** Containment can duplicate destructive commands, strand completion, or under-account violation resources even though all bytes share an already-erased key. This is High.

**Required correction.** Define the containment dedupe identity explicitly—violation resource, complete key target, or both—and the mapping from every violation receipt to an immutable batch/vector. If key-target dedupe is intended, let later resources attach only through an append-only non-authorizing coverage receipt to the already-issued/consumed batch. If one batch per violation is intended, define the protection-owner's atomic `AlreadyDestroyedByBatch(OriginalBatchId, Receipt)` result and prove it cannot mask a wrong-tenant/wrong-alias target. Add same-alias distinct-resource races before and after accepted/containment batch consumption and restore.

## Medium

### M-v20-1 — The migration capability paragraph omits evidence that the normative migration state machine now requires preserving

**Classification:** target-document internal handoff inconsistency.

AD-17's detailed repair path and the conventions preserve the ordered binding gap chain, barrier/containment compare authorizations and stale outcomes, capability-key versions, guard-issued states, and referenced protection-owner consumed/revoked outcomes (`ARCHITECTURE-SPINE.md:236-238`; `IMPLEMENTATION-CONVENTIONS.md:27`). The closed migration-principal paragraph still says its repair variants bind only admission ordinals/partitions, owner cycle, one content binding, hold facts, seal, and batch identity (`ARCHITECTURE-SPINE.md:544`). A narrow command-schema implementation can therefore omit the newer evidence even while a state-machine implementation expects it. Align that capability paragraph with the current AD-17 manifest/high-water contract or explicitly state its list is non-exhaustive and subordinate to AD-17.

## v19 Finding Recheck

| v19 finding | v20 disposition |
| --- | --- |
| C-v19-1 barrier authorization lacked a scope-guard compare revision | **Closed.** Authorization now persists the exact observed guard revision and active-contender manifest; seal conditionally appends at that revision; every registration/release/guard mutation returns authenticated stale; reauthorization requires the recorded stale result and current contender disposition/release (`ARCHITECTURE-SPINE.md:232`; `launch-readiness-register.md:338-341`). |
| H-v19-1 repeated recuts could outrun content successor binding | **Closed.** The next binding starts at the latest installed binding and carries a complete ordered invalidated/tokenless gap chain; overtaken authorizations terminate as `ObsoleteBindingAuthorization` without binding an invalid token (`ARCHITECTURE-SPINE.md:228`; `launch-readiness-register.md:318-321`). |
| H-v19-2 containment batch lacked guard-owned issuance | **Closed for one unique target.** ProtectionFence allocates/fixes a singleton ordinal/manifest; explicit authorization observes the guard; guard conditional issue returns issued-or-stale; only the recorded issued result enables protection (`ARCHITECTURE-SPINE.md:232`; `launch-readiness-register.md:323-327`). H-v20-3 is the uncovered same-alias multi-resource case. |
| H-v19-3 expiring sealed capability had no renewal/recovery rule | **Closed for ordinary rotation/restore.** Capabilities are now explicitly non-expiring, revocable, and non-renewable; their signing-key versions remain verifiable through the complete batch/outcome retention and restore reuses the same identity (`ARCHITECTURE-SPINE.md:232`; `external-dependency-register.md:233-236`). H-v20-1 is the separate missing signer/trust/emergency-compromise contract. |

## Authoritative Validation And Package Reconciliation

The authoritative report's C-1 through C-3 and H-1 through H-12 remain closed in the intended target: conjunctive safety, shared governance protection, executable matrix-v4 bootstrap/scope, scheduled Approver leasing, durable finite safety rescans, human actor tagging, distinct rate/open/Budget lifetimes, crash-consistent proposal/user/source delivery, dependency splitting, trusted-envelope replay and key lifecycle, export ownership, current Dapr exposure, public-contract debt language, and readiness/tracking authority remain explicit. The v20 corrections also make the ordinary guard read/authorization/seal race, stale reauthorization, initial ordinal-one path, repeated pre-binding recuts, one-target containment issue/lost-ack, non-expiring exact retry, and detailed migration preservation convergent.

The package retains all AD-1 through AD-31 and named Open Decisions. C-v20-1 does not choose hold-versus-deletion; it identifies that the current new-capability path has silently chosen one side. H-v20-1 through H-v20-3 and M-v20-1 require only trust ownership, evidence attribution, identity/recovery, and schema reconciliation.

## Areas That Converge

- Guard observation, ProtectionFence authorization, hold registration/release, and seal now have a monotonic compare token. Any intervening guard mutation prevents seal, yields an authenticated stale branch, and requires recorded reauthorization.
- Initial admission ordinal one has an explicit no-containment/no-prior-evidence branch; successor ordinals require their exact containment receipt and retain all earlier evidence.
- Multiple admission recuts before a content binding no longer require an invalid intermediate binding: the installed guard consumes a gap-free invalidation chain and overtaken authorization has one terminal obsolete result.
- One unique post-seal content target receives a ProtectionFence-serialized ordinal/manifest and a separate guard-owned issued-or-stale result before protection consumption.
- Accepted and containment capabilities are non-expiring, revocable, immutable-batch bound, and exactly recoverable across ordinary delay, migration, and restore; multi-key consumption remains all-or-none with exact per-target vectors.
- Operator fence removal and sealing remain mutually exclusive; migration's detailed state machine preserves the new guard/containment/batch facts; malformed/cross-tenant scope evidence remains fail-closed.

## Architecture Defects Versus Implementation Debt

C-v20-1 and H-v20-1 through H-v20-3 are target-architecture defects; M-v20-1 is an internal handoff inconsistency. None is merely absence from the current code. C-v20-1 must be surfaced to Product/Governance/Security rather than resolved by an architecture-local deletion-wins default.

The current repository remains materially behind the target and makes no present implementation claim for these protocols. It has no shipped directory/effect namespace, ordinal admission ledger/guard, owner-cycle recut, continuous content guard, tenant scope guard, atomic deletion seal, signed manifest-batch capability, decision catalog, safety epoch, export store, or Conversations deletion feed. `EXT-HOST-1`, `EXT-CONV-AI-1`, `EXT-PARTIES-1`, protection, secrets, Provider, and export targets remain `Uncommitted`/TBD. Current transitive Dapr Client/ASP.NET exposure and the first five checked-out submodules differing from root gitlinks are delivery reality. They do not weaken the target or turn these findings into implementation debt.

## Gate Result

The v20 adversarial-divergence gate is **FAIL** because Critical and High counts are nonzero. Deterministic lint: **PASS**, `ok: true`, zero findings. Frozen-input after-write hash verification: **PASS**; all eight hashes remain exactly the values listed above.
