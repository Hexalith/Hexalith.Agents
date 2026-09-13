---
name: Hexalith Agents primary adversarial-divergence review v24
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
root_commit: 46936c9e63cabb7b329ee9f5ebd847666cbbadd2
---

# Primary Adversarial-Divergence Review — v24

## Verdict

**PASS — 0 Critical, 0 High, 0 Medium, and 0 Low findings.** Fresh independent-unit reconstruction found no executable divergence in the frozen v24 package. Stable deletion identity, actual-revision separation, replacement-key compromise arbitration, post-start hold gating, the three pre-command primitives, and migration/restore preservation remain mutually consistent. The matrix-v7 column header is now correct, and the v24 source declarations add no runtime or Product authority.

## Frozen Snapshot And Method

The inputs matched the supplied SHA-256 values before review and again after this report was written:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `3789b957d76c27c01ec363fc396335773014a7e9580fc5193c4ee3b98f388b82` |
| `IMPLEMENTATION-CONVENTIONS.md` | `ea1d311da8bef578c7fca428d6b66ef7ae91dc593eb5b3efc9cac382e2a938d9` |
| architecture `.memlog.md` — hash only, never opened | `890d18b355b6b7d475241426641701ebe724dd6906bed15fa180a5e14a0e17e0` |
| bound `prd.md` | `4fbc21e13301bb47c87e077ec7aa884ff698121837b0e19cbf6e0ea6e4334564` |
| `epics.md` | `15f208cd8e2270dfb82352c5d7303b083ffd8f64c91ae40013a946fae6f69982` |
| `external-dependency-register.md` | `3f29c4651eb79e475bd087fff402903652984218b1c66f1d26cd86dcfbb5a4f3` |
| `launch-readiness-register.md` | `9c747bc0329da6a38c4dc56eae3e75e7408d85cb9bb91a2365547ea0ee40f3c6` |
| authoritative `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

The review followed the complete repository and bmad-architecture reviewer-gate instructions. It independently reconstructed API/BFF, Workflow, domain aggregates, directory and ledger owners, `GovernanceScopeGuard`, `ProtectionFence`, EventStore admission/content guards, protection engine, Secrets signer and revocation publisher, host registrars, decision catalog/recorder, migration/repair, and restore. It exercised two or more literal implementations across concurrency, cross-tenant identity substitution, conditional-append loss, duplicate delivery, signer/guard/protection loss, decision activation, deletion acceptance, hold timing, batch dispatch, compromise/re-attestation, irreversible consumption, migration cutover, and restore.

The five v24 review paths in the spine source list were treated as concurrent anticipated gate outputs. Their temporary absence at review start was not treated as missing historical input; this report is one of those declared outputs. Every other declared local source resolved. No declared review output can supply runtime authority, close an Open decision, or weaken an AD.

Repository inspection was read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`. No submodule was initialized, updated, or modified by this review.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 0 |
| Medium | 0 |
| Low | 0 |

## Critical

None.

## High

None.

## Medium

None.

## Low

None.

## Fresh Adversarial Executions

### Repeated stale signing attempts and actual revision separation

**Converges.** The accepted inventory assigns and persists one `DestructionSealId` before the first barrier authorization from the accepted token, predicate digest, and accepted-manifest digest. `DeletionDestructionBatchId` composes that stable id with batch kind, ordinal, and manifest; neither identity contains a predicted guard revision (`ARCHITECTURE-SPINE.md:242,570`).

If attempts A1 and A2 sign at distinct intended issue revisions but each loses to a guard mutation, exact no-issue lookup must terminalize each as `SignedAttestationObsoleteUnissued` before A2 and then A3 may exist. Every successor retains the same seal, batch, manifest, and attestation ordinal while incrementing `SigningAttemptOrdinal` and binding its new intended guard revision/current healthy key. The eventual `CommittedIssuedGuardRevision` or accepted-batch `CommittedDestructionSealGuardRevision` is authenticated result evidence only and is excluded from seal id, batch id, signing-request id, and JWS bytes (`ARCHITECTURE-SPINE.md:263,271,570,574`; `IMPLEMENTATION-CONVENTIONS.md:29-31`; `external-dependency-register.md:199-201`; `launch-readiness-register.md:328-332,350,373`). Teams therefore cannot legally recompute the logical batch, reuse an obsolete signature, or confuse an intended revision with the assigned event revision.

### Replacement-key compromise and activation loss

**Converges.** Re-attestation starts only from the exact protection-owned `ConsumptionBlocked(CapabilityKeyCompromise)` state, leaves the batch blocked through guard credential replacement and successor dispatch, and reopens only at the same protection owner. The activation identity binds replacement key, next attestation, successor dispatch, expected blocked-batch revision, and expected tenant-key block-set revision (`ARCHITECTURE-SPINE.md:269,574`; `launch-readiness-register.md:359`).

If replacement key K2 is blocked before activation or its registrar operation wins concurrently, the one transaction returns `ActivationBlockedByReplacementKeyCompromise(K2, NewRevocationRevision, NewBlockReceiptId)` and retains or moves the same batch to K2's exact blocked state. Activation never clears the global tenant-key block. A lost acknowledgement retries the same activation id and recovers the identical activated-or-new-key-blocked result. A compromise after activation but before reservation still races `ReserveAndConsumeDeletionBatch` at the one protection owner; the block winner prevents destruction and the reservation winner returns the immutable reservation/vector (`ARCHITECTURE-SPINE.md:267-269,604`; `IMPLEMENTATION-CONVENTIONS.md:33`; `external-dependency-register.md:139,251`; `launch-readiness-register.md:356,359,373`).

### Admission violation, block delivery, and irreversible consumption

**Converges.** EventStore assigns accepted admission violations to the partition current at acceptance and records the receipt at that same linearization point; a caller-presented stale ordinal is diagnostic only. After seal, admission-integrity cancellation and key compromise use `BlockDeletionBatchConsumption`, which contends with reservation on the exact same local state transition. `ConsumptionReserved` is the irreversible instant for those cancellation reasons; unknown/lost block or reservation outcomes remain pending and are recovered by exact owner lookup (`ARCHITECTURE-SPINE.md:255,267-269`; `launch-readiness-register.md:324,356,358-359`). No independently built guard, host, or protection engine may implement a read-then-unconditional-write substitute.

### Legal holds before seal, after seal, and around dispatch

**Converges without selecting Product precedence.** Pre-seal hold registration/release and seal commit serialize on the same tenant `GovernanceScopeGuard` revision. Any intervening mutation makes the authorization stale and commits no seal. A post-seal attempt appends `PostStartHoldContenderObserved`; every still-preventable accepted or containment batch, purge, and completion remains blocked until an exact effective decision is applied to that step. A dispatch that won first is uninterruptible only for that batch; the later hold still restricts every other batch and completion (`ARCHITECTURE-SPINE.md:257-259,265,508`; `launch-readiness-register.md:95,118,279,327,353-356,360-362`).

`OD-HOLD-DELETION-PRECEDENCE-1` remains Open and its closed affected set covers pre-seal contention and every post-start branch. Ordinary no-contender deletion remains evaluable. No team can infer hold-allowed or deletion-allowed from this architecture (`launch-readiness-register.md:95,112,118`; `ARCHITECTURE-SPINE.md:1313`; `epics.md:3072`).

### Trusted envelopes, exact replay, and the three primitive boundary

**Converges.** The only pre-command paths remain: replay first-seen registration, content-free security observation recording, and deletion-capability compromise registration. Replay registration precedes target AD-29 idempotency even for a known logical command; exact byte-identical delivery proceeds only to idempotency, changed nonce evidence rejects, and unknown registration blocks. Security routing never trusts the attacked tenant. The compromise registrar is non-public, tenant/key-version limited, protection-first, and unable to sign, issue, re-attest, dispatch, consume, or select a hold outcome (`ARCHITECTURE-SPINE.md:279,577-582`; `IMPLEMENTATION-CONVENTIONS.md:43`; `launch-readiness-register.md:212,232-234,336,369`).

`BlockDeletionBatchConsumption` and `ActivateReattestedDeletionBatch` are downstream protection-owner operations under exact recorded facts and target-limited Workflow capabilities, not a fourth or fifth raw EventStore/pre-command mutation path (`ARCHITECTURE-SPINE.md:604`; `launch-readiness-register.md:358-359`).

### Matrix scope, bootstrap, recovery, and corrected version label

**Converges.** `OperationGateMatrixVersion = 7` is the sole current version. The matrix heading and table column now both name v7, while the prose explicitly describes v6 rules carried forward into v7 (`launch-readiness-register.md:180,208-215,369-373`; `ARCHITECTURE-SPINE.md:448,466`). Bootstrap and repair variants omit the entire circular GateId only when their row supplies the closed direct owner preconditions; unrelated gates remain mandatory. Platform operations never inherit tenant access, tenant operations require one concrete tenant, and missing/old/unknown variants fail closed. The prior v23 Low header residue is closed.

### Migration, repair, restore, and cross-tenant substitution

**Converges.** Migration/repair freezes exact outboxes, leases, phase authorizations, every admission-fence ordinal and partition high-water, content bindings/gap chain, hold/seal facts, stable batch/attestation/signing attempts, compromise receipts, activation identities/results, and protection states. The old-epoch bridge accepts only the frozen pending cohort; successor installation revokes old capabilities and preserves all live deletion restrictions (`ARCHITECTURE-SPINE.md:247-251,604`).

Capability JWS fields bind tenant, request, batch, manifest, guard, intended revision, attestation, signing attempt, key version, issuer, and protection audience. Registrar deliveries bind tenant/key family/version and authenticated revocation identity. Changed tenant, audience, batch, key, dispatch, revision, or target alias cannot satisfy exact lookup or idempotency (`ARCHITECTURE-SPINE.md:263,269,570,574,578`; `external-dependency-register.md:199-201,251`).

### Decision catalog, branch pins, and Product boundaries

**Converges.** Decision activation is one immutable expected-revision operation over exact record revisions/digests and independently signed governance/approval evidence. Pending successors cannot weaken existing blockers or strand an already recorded branch; dedicated recovery uses its pinned effective versions and direct evidence. Open decisions block only their recorded affected evaluations, and the source list is never a substitute for the runtime catalog (`launch-readiness-register.md:89-124`).

The package still surfaces rather than selects hold/deletion precedence, hold-prepare cancellation, operator deletion cancellation/nonterminal disposition, export lifecycle, rate/concurrency consumption, Dapr security posture, sprint reconciliation, historical safety, Automatic-mode retraction, instruction protection, legacy plaintext, initial output-safety status, class/range scope, human exact-Conversation semantics, and Release-recorder authority (`ARCHITECTURE-SPINE.md:1311-1328`).

## Prior Critical/High Closure

### v22/v23 closure

| Finding | v24 disposition |
| --- | --- |
| v22 accepted-batch identity changed after stale signing | **Closed.** Stable pre-seal seal/batch identity and separately stored actual committed revision survive any number of obsolete attempts. |
| v22 replacement-key compromise could race activation | **Closed.** Activation atomically consumes expected batch/key-block revisions and returns an exact new-key-blocked result when the replacement compromise wins. |
| v22 third compromise registrar contradicted older two-primitive wording | **Closed.** Spine, conventions, matrix, and Epics allow exactly the same three closed paths and reject all others. |
| v22 post-start hold scope was incomplete in Story 8.3 | **Closed.** PRD, decision affected set, Story 8.3, matrix, and spine cover every post-start branch through completion without selecting the outcome. |
| v23 package Critical/High | **None were reported, and fresh v24 reconstruction found none.** Stable identity, revision separation, activation arbitration, three-primitive scope, and branch-pinned Product boundaries remain intact. |

### Authoritative `VALIDATION-REPORT-2026-09-12.md`

| Finding | v24 disposition |
| --- | --- |
| C-1 safety could weaken the Product rule | **Closed.** AD-20 requires conjunctive exact-content evaluation against snapshot and every then-current applicable policy; one-pass collapse requires a tested dominance proof. |
| C-2 legal hold/deletion lacked full rejection | **Closed.** Canonical scope, acceptance-time ordinal guards, immutable owner cycles, shared hold/seal owner, stable signed batches, protection-owner cancellation, target coverage, purge evidence, and completion seal provide a fail-closed protocol for both origins. |
| C-3 bootstrap/scope deadlock | **Closed.** Matrix v7 provides target-scoped, typed direct-precondition bootstrap/repair variants and retains unrelated blockers. |
| H-1 scheduled Approver recheck | **Closed.** Resolution lease, cadence/freshness, two-pass empty evidence, typed outcomes, and restart recovery are explicit. |
| H-2 distributed safety rescan | **Closed.** Epoch/index owners freeze a finite manifest, leases/checkpoints, activation, call blocking, and exact recovery. |
| H-3 Approver human status | **Closed.** Parties-authoritative human liveness and stable historical actor binding are required. |
| H-4 ledger lifetimes | **Closed.** Rate, open-interaction, Budget, and capacity remain separate owners with separate identities and settlement. |
| H-5 human identity | **Closed.** Stable authenticated-human identity is distinct from Party and role evidence; Workflow cannot satisfy human separation. |
| H-6 proposal index recovery | **Closed.** Interaction truth, protected source-revision outboxes, directory high-waters, leases, acknowledgements, and repair are bound. |
| H-7 Conversations seam split | **Closed.** Core AI/context/membership/posting/source-deletion seams and optional retraction are independently committed and fail closed. |
| H-8 trusted envelope | **Closed.** Canonical bytes, key/time lifecycle, logical/delivery identity, replay owner, ACL, security spool, and loss recovery are fixed. |
| H-9 export bytes and lifecycle | **Closed at architecture level.** Protected store/index, signed manifest, direct key delivery, hold/deletion cleanup, restore, and exact receipts are explicit while the Product lifecycle outcome remains Open. |
| H-10 Dapr current exposure | **Closed.** Current transitive Client/ASP.NET `1.18.5`, non-authoritative checked-out `1.18.7`, and future Workflow adoption are separately stated (`ARCHITECTURE-SPINE.md:908-910,1352,1399`). |
| H-11 backlog vocabulary claimed shipped | **Closed.** Required target parity and current implementation debt remain separate (`ARCHITECTURE-SPINE.md:434,1332-1354`). |
| H-12 tracker/dependency conflict | **Closed as surfaced delivery governance debt.** It cannot alter external commitment or evidence authority. |

## Architecture Contract Versus Current Implementation Debt

The root-declared gitlinks remain Builds `a32cb422749352cce8dec948aa3e78c8f00eb4cf`, Conversations `73bcee6f04479d4743d5a65ce929728e22687d7d`, EventStore `ce9e779a3ec24d9b9a054afd0ceaa3c05b8adaaa`, FrontComposer `053b2008307d4e476c0d4329e6c47763c301d43e`, Memories `3644ef63da87b8c7ec9e17726fae7f534e642a1c`, Parties `fa42398552fba1c80eb2760791517659d6d1313a`, and Tenants `2fac18396ff11a4459de053b3ebb7ddfe7c13e30`. Checked-out Builds `cf52f74`, Conversations `64b0508`, EventStore `a568af4`, FrontComposer `1b3608c`, and Memories `42dfa26` differ from parent gitlinks; Parties and Tenants match. That uncommitted checkout drift is current repository evidence, not target authority.

Focused source/package inspection still finds none of the v24 stable-seal, obsolete-signature, activation-blocked, protection-owner batch-state, compromise-registrar, matrix-v7, decision-catalog, or full deletion-guard mechanisms. External targets remain Uncommitted/TBD. Root Agents currently consumes Dapr Client/ASP.NET transitively through EventStore but not Dapr Workflow. The spine calls these absences delivery/integration debt and blockers rather than claiming they are shipped (`ARCHITECTURE-SPINE.md:908-910,1332-1354`; `external-dependency-register.md`). No current-code absence weakens the target, and no dirty checkout is silently ratified.

Known assumption retirement dates, the concrete `PostingPending` timeout, and bUnit alignment remain visible planning/build debt elsewhere in the package. They do not create an executable adversarial-divergence finding here because their affected delivery/readiness paths stay blocked and no local default is authorized.

## Gate Result

The frozen v24 primary adversarial-divergence gate is **PASS** with **Critical 0 / High 0 / Medium 0 / Low 0**.

- Deterministic lint: **PASS**, `ok: true`, zero findings.
- AD identity check: **PASS**, exactly one each of AD-1 through AD-31, contiguous and unrenumbered.
- Open-decision check: **PASS**, 16 stable `OD-*` ids in both spine and launch register with no difference.
- Matrix label check: **PASS**, heading and current table column both name v7.
- Source declaration check: **PASS**, all non-concurrent local sources resolve; v24 gate reports are anticipated concurrent outputs and supply no runtime authority.
- Frozen-input after-write hash verification: **PASS**; all eight hashes remain exactly the values above. The architecture memlog was not read.
