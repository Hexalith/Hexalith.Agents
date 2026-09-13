---
name: Hexalith Agents adversarial-divergence review v19
type: architecture-review
lens: adversarial-divergence
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-13
verdict: fail
critical: 1
high: 3
medium: 0
low: 0
lint_ok: true
---

# Adversarial Divergence Review — v19

## Verdict

**FAIL — 1 Critical, 3 High, 0 Medium, and 0 Low findings.** The v19 scope-guard, ordinal-recut, continuous-content-guard, and atomic manifest-batch changes close the literal v18 defects in their ordinary paths. A complete independent-implementation pass nevertheless found that the late-hold guarantee still lacks the compare token that makes it executable, and found three crash/concurrency gaps in repeated recuts and containment-batch/capability recovery. Deterministic architecture lint passed with `ok: true` and zero findings.

## Frozen Snapshot And Method

The following inputs were SHA-256 checked before review and after this report was written:

- `ARCHITECTURE-SPINE.md`: `79ebd22bba3ec5c42eae31377a461f00a94035c1cef8422927562295d9a7e950`
- `IMPLEMENTATION-CONVENTIONS.md`: `4686b409fce780443ec4d141e5306b7bf85183d23e31e6f6260ddc18a0ca2744`
- architecture `.memlog.md`: `4df0405f18c54441f6d8ed20cfa5ac39798eba21620d2f731fefc9860f2caa01`
- bound `prd.md`: `d715f54d76e58a9ba6183a0e04979779703476b855842513816c35b8655ab9b0`
- `epics.md`: `ed9ccb70a25e80fa16833f09b2af6f219d8291e4d5bd5cb0aa17d6e9de59ad56`
- `external-dependency-register.md`: `60ff899220f10f2227f420e9321f01328d5048481cf4cad55553b0f8e78b206c`
- `launch-readiness-register.md`: `eca4fd6c787a7f3e65fea4570262c294895933a7114b3f262e4f1f340b6b6383`
- authoritative `VALIDATION-REPORT-2026-09-12.md`: `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`

The review reconstructed independent API, Interaction Workflow, directory/effect-authorization, ledger, migration/repair, EventStore admission/content-guard, `ProtectionFence`, `GovernanceScopeGuard`, hold, both deletion-origin, protection-engine, and crash/lost-ack recovery units. Each unit had to obey the literal spine, conventions, PRD, epics, dependency contracts, and matrix-v4 rows without interpolating an unstated owner, field, clock, or Product outcome. The units were raced around hold registration/release and sealing; every admission append and current-zero proof; pre-cut and post-cut violation containment; repeated ordinal recuts before successor binding; accepted and containment manifest freezing, consumption, revocation, and lookup; migration/restore; and cross-tenant evidence. Every authoritative Critical/High and every v18 Critical/High was rechecked before seeking new divergence.

Repository inspection was read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`; no submodule was initialized, updated, or mutated. Root gitlinks are Builds `a32cb422749352cce8dec948aa3e78c8f00eb4cf`, Conversations `73bcee6f04479d4743d5a65ce929728e22687d7d`, EventStore `ce9e779a3ec24d9b9a054afd0ceaa3c05b8adaaa`, FrontComposer `053b2008307d4e476c0d4329e6c47763c301d43e`, Memories `3644ef63da87b8c7ec9e17726fae7f534e642a1c`, Parties `fa42398552fba1c80eb2760791517659d6d1313a`, and Tenants `2fac18396ff11a4459de053b3ebb7ddfe7c13e30`. The first five checked-out revisions differ from their root gitlinks; Parties and Tenants match. Those dirty checkouts are implementation evidence only and do not redefine the target architecture.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 3 |
| Medium | 0 |
| Low | 0 |

## Critical

### C-v19-1 — The scope-guard seal asserts stale late-hold rejection but the barrier authorization carries no scope-guard compare revision

**Classification:** target-architecture irreversible-deletion and legal-hold linearization defect; not a choice of the unresolved hold outcome.

**Evidence.** The PRD and spine correctly require a hold contender that wins after barrier authorization but before `DestructionSealed` to make that no-hold authorization stale; hold registration and sealing serialize on `GovernanceScopeGuard(TenantId)` (`prd.md:955,957`; `ARCHITECTURE-SPINE.md:229,472,480`; `IMPLEMENTATION-CONVENTIONS.md:25`). Hold registration has an explicit `TenantGovernanceScopeGuard...ExpectedRevisionExact` compare input (`launch-readiness-register.md:274-276`). By contrast, `AuthorizeDeletionDestructionStartBarrier` records a `ProtectionFence` expected revision, current ledger/content evidence, and a hold absence or disposition, but no `GovernanceScopeGuardExpectedRevision`, reservation id, or comparable guard token (`launch-readiness-register.md:334`). The later guard effect requires `NoOverlappingRegisteredHoldContenderAtGuardRevision` and asserts that an intervening contender makes the authorization lose, but it has no literal field relating that guard revision to the earlier authorization (`launch-readiness-register.md:335`). Generic “bindings” or EventStore timestamps cannot supply an unstated cross-stream compare token under matrix-v4's no-interpolation rule.

**Two literal units.** Team A snapshots and persists the scope-guard revision even though row 334 does not authorize that field, then conditionally seals only at that revision. Team B implements only the listed fields and checks the active contender set at guard execution. If a contender registered after authorization and was subsequently released, or a recovery worker observes a later guard revision with no active contender, Team B accepts the old no-hold authorization; Team A rejects it as stale. Team C rejects every guard-revision change, but cannot prove which revision the authorization observed. All can quote the prose assertion, but only Team A invents the missing executable evidence.

**Impact.** The architecture cannot mechanically prove the promised before/at/after classification at the irreversible boundary. A stale authorization can erase content after a legally material contender won, while `OD-HOLD-DELETION-PRECEDENCE-1` is Open or without consuming its exact effective disposition. Because the possible outcome is irreversible destruction under an unresolved legal-hold branch, this is Critical.

**Required correction — mechanics only.** Preserve `OD-HOLD-DELETION-PRECEDENCE-1` as Open. Add the exact `GovernanceScopeGuard` observed revision/compare token to barrier authorization and its durable event/result schema. Make `CommitDeletionDestructionStartBarrierEffect` conditionally append at that exact revision, or consume an equivalent single-owner reservation that every contender registration/release must advance. A guard change after authorization must yield an authenticated stale result; reauthorization may use the new revision only after the exact approved disposition/release evidence is durable. Bind lookup, restore, migration preservation, and contender-register/release immediately-before/after fixtures to the same token.

## High

### H-v19-1 — Two admission recuts can outrun content-guard successor binding and leave no legal predecessor binding

**Classification:** target-architecture recovery-state and continuous-fence evidence defect.

**Evidence.** The stable content guard gains each new ordinal/token through `AuthorizeDeletionScopeWriteFenceSuccessorBinding`, `Bind...`, and `Record...`; the authorization requires exact predecessor token/cut invalidation and the effect requires an exact predecessor *binding* plus invalidation (`ARCHITECTURE-SPINE.md:470`; `launch-readiness-register.md:318-320`). Admission-violation containment can immediately assign another successor ordinal and requires a new full recut (`ARCHITECTURE-SPINE.md:227,472`; `launch-readiness-register.md:321`). Nothing requires successor binding for ordinal N to finish before another admitted violation invalidates N and assigns N+1, and nothing permits N+1 to bind from the latest installed binding through an immutable invalidation chain when N never acquired a binding.

**Two literal units.** Starting with content binding 1, both teams recut to ordinal 2. Before binding 2, another accepted violation creates ordinal 3. Team A skips the unbound ordinal and binds 3 from binding 1 plus both invalidation receipts. Team B enforces `PredecessorBinding...Exact`, cannot produce binding 2 for an already-invalid token, and remains restrictive forever. Team C retroactively binds invalid ordinal 2 before binding 3, creating evidence that another literal unit rejects as non-current. No current identity/idempotency rule selects among them.

**Impact.** Repeated failures in an explicitly tested recovery path either wedge deletion or weaken the continuous-install/current-token proof used by preparation and sealing. Restriction remains fail-closed, so this is High rather than Critical.

**Required correction.** Define one owner-linearized successor-binding queue or chain. Either a recut cannot assign/accept N+1 until binding N has an authenticated terminal result, or binding N+1 must explicitly accept the most recent installed binding plus a complete gap-free chain of unbound-ordinal invalidation receipts. Specify obsolete authorization results, concurrent assignment, exact lookup, migration/restore preservation, and fixtures with two or more violations before any successor-binding acknowledgement.

### H-v19-2 — Post-seal containment batch creation has no guard-owned freeze/ordinal/effect protocol

**Classification:** target-architecture destructive-capability ownership, concurrency, and recovery defect.

**Evidence.** AD-2 makes `GovernanceScopeGuard(TenantId)` the durable owner of accepted and containment manifest-batch identities (`ARCHITECTURE-SPINE.md:209,1161-1168`). The accepted-set batch is correctly minted by the guard's atomic seal operation (`ARCHITECTURE-SPINE.md:229,472`; `launch-readiness-register.md:335-338`). The later content-containment row instead checks `ProtectionFenceExpectedRevision`, “appends the resource to the containment manifest,” and derives a batch in the same row, without a target-limited `GovernanceScopeGuard` authorization/effect/result, guard expected revision, manifest-freeze event, or exact outcome lookup (`launch-readiness-register.md:323`). The prose requires distinct containment batches but does not define whether concurrent resources share an ordinal, how the target list freezes, or how a lost result recovers the guard-owned capability (`ARCHITECTURE-SPINE.md:472,553`; `epics.md:3034`).

**Two literal units.** Team A lets `ProtectionFence` mint a batch per observed resource. Team B groups concurrent resources into one manifest per containment ordinal and attempts an undocumented guard append. Team C derives a batch at the first resource, then cannot add a second resource without changing the immutable digest or inventing another ordinal. A crash after the protection-fence append but before capability delivery lets the teams either recompute, mint another batch, or wait for an exact guard lookup the contract does not expose.

**Impact.** Independently built units cannot agree on the authority, immutable target set, idempotency identity, or lost-ack recovery for later irreversible destruction. The safest implementations wedge and permissive implementations may issue overlapping capabilities, so this is High.

**Required correction.** Add an explicit guard-owned authorize/effect/result protocol for each containment ordinal. Define ordinal allocation, an immutable sorted-distinct key-target manifest/digest, when it is frozen, the guard expected revision, deterministic batch/capability identity, concurrent-resource handling, exact lookup, and protection-fence acknowledgement. State whether one new resource creates one ordinal or a finite frozen cohort does, and prove that no later append mutates a capability already issued.

### H-v19-3 — A sealed single-use batch can expire, but no renewal or terminal recovery rule exists

**Classification:** target-architecture irreversible-effect liveness and crash-recovery defect.

**Evidence.** `CommitDeletionDestructionStartBarrierEffect` mints one deterministic batch and expressly cannot mint another; changed manifests conflict (`ARCHITECTURE-SPINE.md:472`; `launch-readiness-register.md:335`; `epics.md:3027`). Dispatch nevertheless requires the capability to be `AuthenticUnexpiredUnconsumedAndUnrevoked` (`launch-readiness-register.md:337`). Neither the spine, conventions, dependency contract, matrix, nor story defines the capability's `ExpiresAt`, time authority, exclusive comparison, safe renewal, or terminal expired state. The protection dependency calls it deterministic and signed but likewise gives no expiry/reissue contract (`external-dependency-register.md:233-236`). Migration must preserve the batch identity, which does not answer how a restored sealed request proceeds after capability expiry (`ARCHITECTURE-SPINE.md:235,543`).

**Two literal units.** Team A treats the guard capability as non-expiring despite the explicit precondition. Team B gives it a short security TTL; a crash, outage, restore, or delayed mirror after expiry can neither consume it nor mint a replacement. Team C re-signs a renewed bearer with the same batch id, but invents renewal authority and races old/new signed capabilities differently from Team B. All preserve the manifest identity while producing incompatible recovery and revocation behavior.

**Impact.** A sealed deletion can become permanently unrecoverable after the architecture's irreversible start, or an adapter can introduce an undocumented reissuable destructive credential. This is High.

**Required correction.** Choose a mechanical capability lifetime, not a Product outcome: declare the target-bound capability non-expiring but revocable, or persist exact `ExpiresAt`/time authority and define guard-owned renewal/reissue that retains the immutable `BatchId`/manifest, cannot coexist as two independently consumable grants, loses to revocation/consumption, and is exactly recoverable after restore. Add before/at/after expiry, response-loss, migration, and consume-versus-renew/revoke fixtures.

## v18 Finding Recheck

| v18 finding | v19 disposition |
| --- | --- |
| C-v18-1 late hold could cross the separate seal | **Mechanism introduced but not fully closed.** Hold registration and sealing now share `GovernanceScopeGuard`, and the guard declares that an intervening contender makes authorization stale. C-v19-1 is the remaining executable-schema defect: barrier authorization does not carry the guard revision/token needed to enforce that declaration. |
| H-v18-1 pre-first-global-cut violation had no successor transition | **Closed.** The admission-containment row now has an explicit no-cut/candidate/token branch, invalidates only artifacts that exist, assigns the next ordinal once, and permits successor authorization from either pre-cut or post-cut containment (`ARCHITECTURE-SPINE.md:227,472`; `launch-readiness-register.md:296,321`). |
| H-v18-2 installed content fence had no recut continuation | **Closed for one completed successor binding.** The content guard is stable by request/predicate and the new authorize/effect/result protocol appends ordinal/token bindings without removal (`ARCHITECTURE-SPINE.md:470`; `launch-readiness-register.md:318-320`). H-v19-1 is the still-undefined repeated-recut race before that binding completes. |
| H-v18-3 one-shot multi-DEK shape undefined | **Closed for the accepted set.** `DestroyDekManifest` is now atomic all-or-none, bound to one sorted target manifest, returns an ordered per-target vector, and supports exact retry/lookup and consume/revoke exclusion (`ARCHITECTURE-SPINE.md:472`; `external-dependency-register.md:233-236`; `launch-readiness-register.md:335-339`). H-v19-2 and H-v19-3 concern later containment issuance and capability lifetime, not atomic multi-target semantics. |

## Authoritative Validation And Package Reconciliation

The authoritative report's original C-1 through C-3 and H-1 through H-12 remain closed in the intended target: conjunctive safety, shared protection ownership, matrix-v4 bootstrap/scope, scheduled Approver leasing, finite rescan enumeration, tagged human identity, distinct rate/open/Budget lifetimes, proposal/user/source crash consistency, dependency split, trusted-envelope replay/rotation, Dapr exposure accounting, and story/dependency readiness remain explicit. The new target also materially closes the v18 ordinary pre-cut, continuous-binding, atomic multi-key, and batch-revocation paths.

The bound PRD, epics, and registers consistently keep armed late-hold precedence, post-start hold behavior, operator cancellation/nonterminal behavior, human exact-Conversation semantics, class/range semantics, export lifecycle, instruction protection, legacy plaintext, output-safety public status, rate/concurrency consumption, and release-recorder scope as named Open decisions with restrictive safe states. C-v19-1 and H-v19-1 through H-v19-3 request only owner, evidence, ordering, or recovery mechanics. They do not select any Product outcome.

## Areas That Converge

- Admission writes and sealing now share an EventStore guard boundary; current-ordinal zero, content checkpoint, migration/fence receipts, and compromise state are rechecked atomically before `DestructionSealed`.
- A violation before any global cut has an explicit no-artifact containment branch, while a later violation invalidates only exact existing artifacts; ordinal evidence remains append-only.
- One completed recut can extend the continuously installed request/predicate content guard through an authenticated successor binding without a remove/reinstall gap.
- The accepted-set protection operation is a deterministic single-use atomic manifest batch, all targets or none, with ordered per-target receipts, exact lookup, changed-manifest conflict, and consume-versus-revoke exclusion.
- Operator fence removal remains cleanup-gated, covers every ordinal/content binding, competes with barrier authorization, and is unavailable while its Product decision is Open.
- Migration/repair preserves admission ordinals/partitions, owner cycles, content bindings, hold contenders/releases, destruction seals, and accepted/containment batch identities rather than silently resetting them.
- Cross-tenant and malformed governance membership remains derived from authoritative tenant/source/permit facts and fails closed; no Product decision was inferred.

## Architecture Defects Versus Implementation Debt

C-v19-1 and H-v19-1 through H-v19-3 are target-architecture handoff defects. They concern the chosen guard, recut, containment-capability, and recovery mechanics; they must be closed so independently built components implement the same safety and liveness behavior. C-v19-1 specifically preserves the Open hold outcome and asks only for the compare evidence required by the already-stated fail-closed rule.

The current repository remains materially behind that target. It contains no shipped directory/effect-authorization namespace, ordinal admission ledger/guard, owner-cycle recut, continuous content guard, tenant scope guard, atomic deletion seal, manifest-batch protection operation, decision catalog, three-ledger protocol, safety epoch, export store, or Conversations deletion feed. `EXT-HOST-1`, `EXT-CONV-AI-1`, `EXT-PARTIES-1`, protection, secrets, Provider, and export targets remain `Uncommitted`/TBD as the registers state. Current transitive Dapr client/ASP.NET exposure and the first five checked-out submodule revisions differing from root gitlinks are delivery reality, not present implementation of these target contracts. None of the four findings is merely implementation debt, and current absence is not permission to weaken an AD.

## Gate Result

The v19 adversarial-divergence gate is **FAIL** because Critical and High counts are nonzero. Deterministic lint: **PASS**, `ok: true`, zero findings. Frozen-input after-write hash verification: **PASS**; all eight hashes remain exactly the values listed above.
