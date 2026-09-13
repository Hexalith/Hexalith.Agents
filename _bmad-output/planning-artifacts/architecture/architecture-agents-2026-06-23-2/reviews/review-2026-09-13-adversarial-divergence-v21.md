---
name: Hexalith Agents adversarial-divergence review v21
type: architecture-review
lens: adversarial-divergence
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-13
verdict: fail
critical: 1
high: 2
medium: 1
low: 0
lint_ok: true
---

# Adversarial Divergence Review — v21

## Verdict

**FAIL — 1 Critical, 2 High, 1 Medium, and 0 Low findings.** The v21 package closes every v20 finding at the stated contract level: post-start holds now durably block still-preventable work without choosing the Open Product outcome, the capability signer/trust lifecycle is named, admission attribution is guard-assigned at acceptance, same-alias resources converge on one target, and completion is guard-sealed. A fresh independent-unit reconstruction nevertheless found one unsafe cross-owner cancellation race, one missing signed-but-unissued recovery path, and one principal-authority contradiction in the new emergency-compromise protocol. The remaining Medium is stale per-resource wording that conflicts with the new per-target containment identity. Deterministic architecture lint passed with `ok: true` and zero findings.

## Frozen Snapshot And Method

The following inputs were SHA-256 checked before review and after this report was written:

- `ARCHITECTURE-SPINE.md`: `0c62966b3d91a7368d032a0f31f669d7fb0a54628e3e1675cc15e78080585448`
- `IMPLEMENTATION-CONVENTIONS.md`: `c53767c011ce2d4312cfad5e565dc3851babfb9bd609b965017dfb352ec203cb`
- architecture `.memlog.md`: `d379abad04e8f2c7d2a343c5438bb96fcb7d9c6ed7b7429230c6279aea9ae985`
- bound `prd.md`: `a6e2fd051206c8547533558978c91cf9466ce05cb29997e60c16b6921b3d51a3`
- `epics.md`: `41beb3fd70cd7f61f058fd62c5835782daee7ce89192a351d09d30456f0bb506`
- `external-dependency-register.md`: `2da84f01e03e2966b2d2d56b354627c9cfc232648c74e5981f5c94c8b4537377`
- `launch-readiness-register.md`: `a85a1070e8837ff3ffaaddff69c38c6702f792945a5054430cba413e0751c606`
- authoritative `VALIDATION-REPORT-2026-09-12.md`: `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`

The pass reconstructed separately implemented API/principal verification, Interaction Workflow, deletion Workflow for both origins, `ProtectionFence`, `GovernanceScopeGuard`, EventStore admission/content guards, hold, secrets signer/trust publisher, protection engine, completion, migration/repair, and crash/lost-ack recovery units. It then raced initial issue, guard stale outcomes, dispatch, post-seal admission violations, emergency key compromise, protection consumption, re-attestation, post-start holds, completion, restore, and migration. A unit was considered compliant only if it used fields, principals, operations, state owners, and Product decisions literally present in the frozen package. The authoritative three Critical and twelve High findings, every v20 Critical/High, and the bound PRD/register claims were rechecked before looking for fresh divergence.

Repository inspection was read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`; no submodule was initialized, updated, or mutated. Root gitlinks remain Builds `a32cb422749352cce8dec948aa3e78c8f00eb4cf`, Conversations `73bcee6f04479d4743d5a65ce929728e22687d7d`, EventStore `ce9e779a3ec24d9b9a054afd0ceaa3c05b8adaaa`, FrontComposer `053b2008307d4e476c0d4329e6c47763c301d43e`, Memories `3644ef63da87b8c7ec9e17726fae7f534e642a1c`, Parties `fa42398552fba1c80eb2760791517659d6d1313a`, and Tenants `2fac18396ff11a4459de053b3ebb7ddfe7c13e30`. The first five checked-out revisions differ from those gitlinks; Parties and Tenants match. No current source occurrence of `DeletionBatchCapability`, `GovernanceScopeGuard`, `AdmissionFenceOrdinal`, `PostStartHoldContender`, or `DeletionCompletionSealed` was found. That dirty checkout and missing implementation are delivery evidence only, not authority to weaken the target.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 2 |
| Medium | 1 |
| Low | 0 |

## Critical

### C-v21-1 — A guard blocker can win after the protection lookup but lose to irreversible consumption because the two owners have no shared linearization

**Classification:** target-architecture irreversible-effect authorization and cross-owner concurrency defect.

**Evidence.** A successful dispatch is deliberately uninterruptible only with respect to a *later hold*; by contrast, a later capability-key compromise must block before physical consumption and require re-attestation, and a post-seal accepted admission violation records integrity compromise and blocks further destruction (`ARCHITECTURE-SPINE.md:250,254,256,307`; `launch-readiness-register.md:321,332,350,352,354`). The protection extension likewise says emergency revocation blocks before consume (`external-dependency-register.md:245`). The only enforcement given to `EXT-PROTECTION-1`, however, is a linearizable exact `GovernanceScopeGuard` lookup followed by the protection owner's separate atomic batch consume (`ARCHITECTURE-SPINE.md:254`; `launch-readiness-register.md:352`; `external-dependency-register.md:237,245`). A linearizable read at owner G cannot be atomic with a later state transition at owner P without a reservation/commit, shared compare token, or P-owned revocation operation. The package defines no such bridge for capability compromise or admission-integrity compromise. `RevokeDeletionDestructionBatch` is P-owned and races consumption, but its authorization is restricted to recorded post-seal admission compromise and nothing connects its winning revocation to the lookup-before-consume window for every canceling guard mutation (`launch-readiness-register.md:354`; `external-dependency-register.md:237`).

**Two literal units.** Dispatch D is committed. Protection team P1 reads the guard at G40, sees D active and uncompromised, then pauses before its atomic consume. A compromise or accepted admission violation commits at G41. P1 resumes and consumes because its required lookup was linearizable and valid when performed. Protection team P2 performs a second read immediately before its local compare-and-consume and blocks. Team P3 treats successful dispatch as uninterruptible for all later facts, extending the explicit hold rule to compromise. All three can satisfy the stated lookup plus local atomicity, but P1/P3 perform irreversible work after the architecture says the newly committed blocker must prevent it.

**Impact.** An old-key batch or integrity-compromised deletion can destroy keys after the authoritative guard has made that effect ineligible. Exact retry cannot restore destroyed plaintext, and completion being blocked later does not repair the irreversible authorization breach. This is Critical.

**Required correction.** Define one cross-owner, crash-recoverable consume protocol for all guard facts that are intended to cancel a dispatched but unconsumed batch. For example, conditionally reserve the exact active dispatch/attestation on the guard, atomically race a corresponding consume-or-revoke state at `EXT-PROTECTION-1`, then settle the guard from the immutable P-owned outcome; or install the finite compromise/integrity revocation set at the protection owner and make local consume race that revocation. State exactly which instant makes a batch uninterruptible, distinguish the intentional later-hold ordering, make lost acknowledgements/restore converge, and prohibit both post-blocker consume and false re-attestation while an old consume may still be in flight. Add before/at/after races for admission integrity and key compromise against guard lookup, protection reserve/consume, revocation, re-attestation, and outcome lookup.

## High

### H-v21-1 — A signer result that becomes stale before guard issue has no legal terminalization or retry identity

**Classification:** target-architecture crash/stale recovery and destructive-capability issuance defect.

**Evidence.** Accepted and containment paths obtain the signed attestation *before* the conditional guard issue, and the signed bytes include `IssuedGuardRevision` (`ARCHITECTURE-SPINE.md:254`; `launch-readiness-register.md:325-328,345-347`). Any intervening hold, release, coverage update, compromise, or other guard mutation correctly returns an authenticated stale result and permits a new barrier or containment authorization at the returned revision (`launch-readiness-register.md:328,330,346,347`). But the signing result has no obsolete/abandoned terminal state. `DeletionBatchCapabilitySigningRequestId` omits `IssuedGuardRevision` and is fixed by batch, manifest, attestation ordinal, key version, and audience (`ARCHITECTURE-SPINE.md:557`). Reusing it with the new revision changes the canonical signed payload and therefore conflicts; reusing the old JWS fails the new exact intended-issue-revision check. Incrementing `AttestationOrdinal` is authorized only by emergency re-attestation of a *guard-issued* unconsumed batch, which this one is not (`ARCHITECTURE-SPINE.md:254,581`; `launch-readiness-register.md:331-334`).

**Two literal units.** A hold registration wins between `RecordDeletionBatchCapabilitySigned` and accepted-batch seal. Team S1 records the barrier stale result, keeps the same attestation ordinal, and asks `EXT-SECRETS-1` to sign the new `IssuedGuardRevision`; the unchanged signing request id now names changed canonical bytes and must conflict. Team S2 reuses the old JWS, which the new issue commit must reject. Team S3 invents attestation ordinal 2 even though no emergency compromise or issued batch exists. The same dead end occurs for containment issue stale. None has a literal successful recovery path.

**Impact.** An ordinary concurrent guard append can permanently wedge an otherwise valid accepted or containment deletion after external signing has succeeded. Implementers may escape only by weakening idempotency, accepting a stale signed revision, minting an unauthorized attestation, or creating a second batch. This is High.

**Required correction.** Add an authenticated signed-but-unissued terminal outcome and a deterministic successor-signing rule. Bind signing identity to the guard-issue attempt/revision, or explicitly allow a next pre-issue attestation ordinal after the prior signed result is proven unissued and recorded obsolete. The guard must accept exactly one active attestation, exact lookup must distinguish issued, stale-signed, and unknown states, and emergency re-attestation must remain a separate post-issue recovery. Cover stale after authorization, after signer effect, after signer-result record, signer lost acknowledgement, routine rotation, emergency compromise before issue, and restore for both accepted and containment batches.

### H-v21-2 — The emergency-compromise command requires an `EXT-SECRETS-1` service identity that the closed principal model cannot express

**Classification:** target-architecture principal authority and security-recovery defect.

**Evidence.** The matrix assigns `GovernanceProtection:RecordDeletionBatchCapabilityKeyCompromise` to `Platform; exact EXT-SECRETS-1 issuer only` and expects a replay-registered trusted notification (`launch-readiness-register.md:332`). AD-30 defines `Platform` as a freshly authorized human Platform Operator entering through the Agents API with a stable `AuthenticatedHumanActorId`, and says every family rejects Platform unless AD-30 itself names a narrower operation grant (`ARCHITECTURE-SPINE.md:563`). `EXT-SECRETS-1` is neither that human nor any closed `WorkflowKind`; no non-public issuer primitive comparable to the replay registrar or security recorder is defined. The deletion Workflow allowlist and host capability addendum name signing, re-attestation, dispatch, and completion operations, but omit the key-compromise notification (`ARCHITECTURE-SPINE.md:581`). Thus the matrix requires a caller the envelope/principal contract must reject, while letting a human Platform principal stand in for the service would violate “exact issuer only.”

**Two literal units.** Team A rejects the Secrets notification because it cannot construct a valid AD-30 principal. Team B maps the dependency service to `Platform`, invents an `AuthenticatedHumanActorId`, and bypasses the fresh human-role check. Team C exposes a bespoke internal credential/ACL patterned after the replay registrar, although no such capability is authorized. Team A leaves compromised guard-issued batches unresolved; B and C accept different trust and replay boundaries.

**Impact.** Emergency compromise is either unactionable, dependent on forged human provenance, or implemented behind an undocumented privileged path. That prevents the finite batch enumeration/re-attestation recovery and can leave fail-closed deletion permanently stalled or old attestations inconsistently accepted. This is High.

**Required correction.** Define a closed non-human principal or a non-public, composition-limited compromise registrar with exact stream/event ACLs and no general dispatcher exposure; alternatively require a human Platform action but change the contract honestly and retain authenticated Secrets revocation evidence as a precondition. Bind issuer/audience, tenant routing, key family/version, revocation revision, replay/idempotency, finite batch enumeration, cross-tenant denial, lost acknowledgement, and restore. Add the operation explicitly to AD-30's allowlist and fixtures rather than relying on the matrix to broaden a principal family AD-30 closes.

## Medium

### M-v21-1 — Older identity/dependency sentences still allocate containment per violation resource rather than per complete protection target

**Classification:** target-document handoff inconsistency.

The authoritative v21 rule deduplicates by complete `(TenantId, AgentInteractionId, TargetProtectionKeyAlias)` and gives later same-target resources coverage receipts without another ordinal or capability (`ARCHITECTURE-SPINE.md:246,252,557`; `launch-readiness-register.md:323`; `IMPLEMENTATION-CONVENTIONS.md:70`; `epics.md:3038`). Yet the general deterministic-id rule still says “each singleton content-containment resource receives” an ordinal (`ARCHITECTURE-SPINE.md:553`), and the base protection dependency says each post-start content violation uses its own assigned containment ordinal/batch (`external-dependency-register.md:237`). A guard implementer reading the v21 clarification converges by target; an external-owner implementer following its base required-artifact row can expect per-violation batches. Replace the stale per-resource wording with the complete-target identity and make `AlreadyDestroyedByBatch` explicit defense-in-depth rather than permission to mint a same-target successor batch.

## v20 Critical/High Recheck

| v20 finding | v21 disposition |
| --- | --- |
| C-v20-1 post-start hold could be bypassed by a new containment capability | **Closed as a Product-neutral rule.** `PostStartHoldContenderObserved` is durable; Open/missing/non-deletion-allowed evidence blocks every still-preventable dispatch, containment issue, purge, and completion; a dispatch that wins earlier is explicitly ordered and already-consumed vectors remain immutable (`ARCHITECTURE-SPINE.md:248-250`; `launch-readiness-register.md:275,324,349-357`; `prd.md:958`). |
| H-v20-1 signer, trust anchor, rotation, and emergency compromise were absent | **Closed at static contract level.** `EXT-SECRETS-1` owns ES256/RFC-8785 signing and versioned anchors; routine rotation, emergency revocation, online guard validation, and same-batch re-attestation are present (`ARCHITECTURE-SPINE.md:254`; `external-dependency-register.md:197,245`; `launch-readiness-register.md:325-334`). C-v21-1, H-v21-1, and H-v21-2 are new execution/authority defects inside that protocol, not persistence of the missing static design. |
| H-v20-2 accepted-write ordinal attribution was ambiguous | **Closed.** The EventStore guard assigns `AcceptedAtAdmissionFenceOrdinal` and high-water at the append linearization, contaminates that partition atomically with successor install, treats caller ordinal as diagnostic, and blocks on unattributable evidence (`ARCHITECTURE-SPINE.md:246`; `launch-readiness-register.md:321`; `epics.md:3015`). |
| H-v20-3 same-alias violations could receive divergent batches | **Closed by the current normative rule.** ProtectionFence coverage is keyed by complete target; same-target observations converge and only uncovered targets receive a batch (`ARCHITECTURE-SPINE.md:252,557`; `launch-readiness-register.md:323,353`; `epics.md:3038`). M-v21-1 is stale handoff prose, not a missing normative owner/index. |

## Authoritative Validation And Package Reconciliation

The authoritative report's C-1 through C-3 and H-1 through H-12 remain closed in the intended target: conjunctive snapshot/current safety, shared hold/deletion protection, scoped matrix-v5 bootstrap/repair, scheduled Approver resolution, finite safety rescans, human Parties/actor evidence, three ledger lifetimes, crash-consistent proposal/source/user-action recovery, dependency split, trusted-envelope replay/key lifecycle, export ownership, Dapr current-reality wording, public-contract debt language, and readiness/tracking authority are still explicit. The v21 additions do not rename any AD-1 through AD-31 or choose any named Open Product outcome. C-v21-1 and H-v21-1 are technical linearization/recovery omissions; H-v21-2 is a security-principal schema contradiction. None requires deciding hold-versus-deletion, operator Abort/nonterminal behavior, human exact-Conversation semantics, class/range scope, initial output-safety status, export lifecycle, legacy plaintext disposition, or any other Open Product record.

The bound PRD and epics correctly require post-start hold blocking, guard-assigned admission attribution, signed target-bound capabilities, per-target coverage, and guard-sealed completion. They do not supply the missing cross-owner consume race, signed-before-issue stale recovery, or callable Secrets compromise principal. Both registers preserve the intended Uncommitted/TBD status of `EXT-SECRETS-1`, `EXT-PROTECTION-1`, `EXT-HOST-1`, `EXT-CONV-AI-1`, and related seams, so no dependency is falsely claimed shipped.

## Areas That Converge

- Snapshot and every then-current safety check remain conjunctive, retry-stable, and phase-owned.
- OperationGateMatrix version 5 remains scoped and bootstrap-safe; circular-gate omission does not omit unrelated failures.
- Every accepted admission gets a server-owned current-ordinal receipt, and repeated recuts preserve immutable partitions, carried obligations, gap-chain content bindings, and current-zero evidence.
- Hold registration/release, seal, post-start hold observation, dispatch, containment issue, and completion are guard-revision conditional; ordinary stale/lost-ack outcomes are explicit.
- Same-alias content observations converge through one target index and append-only resource coverage; multi-target protection remains all-or-none with exact terminal vectors.
- The JWS canonical shape, issuer/audience, trust anchors, normal rotation, immutable batch identity, and post-issue re-attestation shape are interoperable when no newly found race occurs.
- Migration/repair preserves the acceptance receipts, violation chains, hold facts, guard revisions, capability attestations, dispatches, terminal protection vectors, and completion state without giving migration a destructive capability.
- Tenant isolation, trusted-envelope replay, decision-catalog activation, export cleanup/destruction pinning, posting, and the three rate/open/Budget lifetimes remain literal and fail closed.

## Architecture Defects Versus Implementation Debt

C-v21-1 and H-v21-1 through H-v21-2 are target-architecture defects; M-v21-1 is internal handoff inconsistency. None is merely the absence of code. The architecture should define these seams before delivery rather than delegating a security or idempotency choice to implementers.

Current code remains materially behind the target and does not contain the v21 guard, fence, signed-capability, directory/effect, decision, safety-epoch, export-store, or Conversations deletion-feed protocols. `EXT-HOST-1`, `EXT-CONV-AI-1`, `EXT-PARTIES-1`, protection, secrets, Provider, and export targets remain Uncommitted/TBD. Current transitive Dapr Client/ASP.NET exposure and the first five checked-out submodules differing from root gitlinks are implementation/integration debt. The spine's delivery-debt ledger states that these v21 mechanisms are absent and does not falsely treat the dirty checkout as parent authority.

## Gate Result

The v21 adversarial-divergence gate is **FAIL** because Critical and High counts are nonzero. Deterministic lint: **PASS**, `ok: true`, zero findings. Frozen-input after-write hash verification: **PASS**; all eight hashes remain exactly the values listed above.
