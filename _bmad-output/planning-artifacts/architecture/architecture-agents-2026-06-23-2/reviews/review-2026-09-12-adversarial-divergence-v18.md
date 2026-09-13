---
name: Hexalith Agents adversarial-divergence review v18
type: architecture-review
lens: adversarial-divergence
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
verdict: fail
critical: 1
high: 3
medium: 0
low: 0
lint_ok: true
---

# Adversarial Divergence Review — v18

## Verdict

**FAIL — 1 Critical, 3 High, 0 Medium, and 0 Low findings.** The v18 ordinal owner-cycle and guard-owned start-barrier changes close the literal v17 defects: an old Effective cut is never reopened, current-ordinal zero is distinct from request-lifetime history, and `DestructionStarted` is now atomically serialized with admission writes. The complete independent-implementation pass nevertheless found one irreversible late-hold race and three recovery/capability contracts that still permit competent teams to diverge. Deterministic architecture lint passed with `ok: true` and zero findings.

## Frozen Snapshot And Method

The following inputs were SHA-256 checked before review and after this report was written:

- `ARCHITECTURE-SPINE.md`: `ab7b4962d281a103dbe52bb3f2bbba2d34848d27718b7b8f83f999e1ff484d25`
- `IMPLEMENTATION-CONVENTIONS.md`: `247ea972ca9c16c0366412dc338806ab7ddc69741a03307e37dc354ef7a4300c`
- architecture `.memlog.md`: `828ed872d585f3ddf640d331d3172426f65cc9c48fcabc550689481206cb70d3`
- bound `prd.md`: `40fe0512b89bffcd3df939a8fb40fff99a720935d997f1a83b1c2491ad4052cb`
- `epics.md`: `e07a65002449503320f25495b6ffe1953fd786492435e0ddfccde14d0c620296`
- `external-dependency-register.md`: `1353ef89e76f74c8257ce3b4cdefd0fd2a98ec33f530de0cfd1376bea73ed25a`
- `launch-readiness-register.md`: `1010c0cbf5d03e5d65664c9bbb3aef4a8a1e734620a91b3a123d8bf98a43b4bd`
- authoritative `VALIDATION-REPORT-2026-09-12.md`: `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`

The review reconstructed independent public-command, Interaction Workflow, directory/effect-authorization, rate/open/Budget/capacity, Provider, migration/repair, EventStore-guard, protection-fence, both deletion-origin, hold/export, and crash/lost-ack recovery units. Each unit was required to obey the literal spine, conventions, bound PRD, epics, dependency register, and matrix-v4 rows. The units were raced around every authorization/effect/result boundary, especially initial and successor admission ordinals, pre-cut and post-cut violations, content-fence carry-forward, hold and operator-removal contention, migration successor installation, guard sealing, capability consumption, restore, and cross-tenant evidence. All authoritative Critical/High findings and both v17 findings were rechecked before seeking new divergence.

Repository inspection was read-only at root commit `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`; no submodule was initialized, updated, or mutated. Root gitlinks remain Builds `a32cb422749352cce8dec948aa3e78c8f00eb4cf`, Conversations `73bcee6f04479d4743d5a65ce929728e22687d7d`, EventStore `ce9e779a3ec24d9b9a054afd0ceaa3c05b8adaaa`, FrontComposer `053b2008307d4e476c0d4329e6c47763c301d43e`, Memories `3644ef63da87b8c7ec9e17726fae7f534e642a1c`, Parties `fa42398552fba1c80eb2760791517659d6d1313a`, and Tenants `2fac18396ff11a4459de053b3ebb7ddfe7c13e30`. The first five checked-out revisions differ from those root gitlinks; Parties and Tenants match. Those dirty checked-out revisions are implementation evidence only and do not change the target contract.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 3 |
| Medium | 0 |
| Low | 0 |

## Critical

### C-v18-1 — A hold can first contend after start authorization but before guard sealing without invalidating the already-issued barrier authority

**Classification:** target-architecture irreversible-deletion, legal-hold, and cross-owner linearization defect; not a Product outcome selection.

**Evidence.** `AuthorizeDeletionDestructionStartBarrier` appends on `ProtectionFence` and explicitly does not start destruction. A later, separate EventStore-guard operation commits `DestructionSealed`, and only that guard instant is `DestructionStarted` (`ARCHITECTURE-SPINE.md:224,465`; `launch-readiness-register.md:325-327`; `epics.md:3027`). The PRD and spine say a hold that first contends after `DeletionArmed` and before `DestructionStarted` is governed only by `OD-HOLD-DELETION-PRECEDENCE-1`; while it is Open, neither branch may infer an outcome or advance (`prd.md:955,957`; `ARCHITECTURE-SPINE.md:459,473,1239`; `launch-readiness-register.md:95,112`). The authorization row checks absence/approved outcome only at the armed and authorization revisions. The guard-effect row later checks the recorded authorization, ledgers, migration/fences, removal, and integrity state, but neither consumes a still-current `ProtectionFence` revision nor serializes with a hold-intent append; it is serialized only with matching admission writes (`launch-readiness-register.md:325-326`). Operator-removal contention is closed because its authorization is declared mutually exclusive at the fence and the guard checks that fact, but no equivalent late-hold invalidation is stated.

**Two literal units.** Team A treats the earlier no-hold barrier authorization as a reservation that excludes a later hold, even though the contract says it is not `DestructionStarted`; it lets the guard seal and destroys the DEKs. Team B applies the PRD literally: a hold that wins the fence after authorization but before sealing is an armed-contention case, so it blocks the guard until the Open Decision has an effective outcome. A third fail-closed unit refuses to record the hold because the authorization already exists, which silently invents the reject/defer outcome the PRD forbids. None of the current guard predicates distinguishes these executions.

**Impact.** An ordinary no-hold deletion can cross the irreversible boundary after a legally material hold arrived inside the explicitly unresolved interval. That can erase held content while the binding Product contract says neither branch may advance, so this is Critical.

**Required correction — architecture mechanics, not the Product branch.** Preserve `OD-HOLD-DELETION-PRECEDENCE-1` as Open. Make every hold intent contend durably at `ProtectionFence` even when its outcome cannot yet be chosen, and make `CommitDeletionDestructionStartBarrierEffect` consume an authenticated still-current fence reservation/revision or equivalent guard fact that is linearized with those hold-intent writes. Any hold that wins after authorization and before sealing must invalidate or stall that authorization unless the same fence has already recorded the exact approved decision version/outcome for this contention. Define lost-ack/restore lookup and before/at/after fixtures. Do not redefine the earlier authorization as `DestructionStarted` or choose accept-versus-reject locally.

## High

### H-v18-1 — An accepted admission violation before the first global cut has no legal containment-to-successor transition

**Classification:** target-architecture recovery state-machine defect.

**Evidence.** A guard-integrity violation may be discovered at any time after the admission-fence install checkpoint, and the failure-injection contract explicitly includes violations before and after candidate, acceptance, preparation, and barrier authorization (`ARCHITECTURE-SPINE.md:222-224,283`; `epics.md:3009-3012`). But `ContainDeletionScopeAdmissionFenceViolation` requires invalidating the *cited current global cut* and derived candidate/token, while successor-fence authorization requires a recorded violation that invalidated that cited cut (`launch-readiness-register.md:290,312`). Immediately after fence installation or while owner cycles are still Closing, there is no global cut, candidate, or token to cite. The current ordinal can no longer attest zero, so it can never create that missing global cut (`launch-readiness-register.md:303-304`).

**Two literal units.** Team A treats invalidation of nonexistent downstream artifacts as an explicit no-op, records the violation, and assigns the successor ordinal. Team B enforces the named precondition literally and rejects containment because no global-cut revision exists. Team C waits for a global cut that the nonzero current partition makes impossible. Only Team A progresses, but it invents a state transition and idempotency rule absent from the matrix.

**Impact.** The safe recovery path wedges permanently when the same integrity failure occurs before the first cut rather than after it. Fences remain restrictive, so the failure is High rather than Critical, but independently built workers cannot converge.

**Required correction.** Give containment an explicit pre-global-cut branch: record the exact violation/partition and assign the next ordinal once while proving that no current global cut/candidate/token exists; invalidate each downstream artifact only when its exact revision exists. The successor authorization must accept either this no-artifact receipt or the existing post-cut invalidation receipt. Exact replay, concurrent detection, and lost acknowledgement must resolve to one successor ordinal.

### H-v18-2 — A higher-ordinal recut after content-fence installation cannot legally reuse, rebind, or replace the old-token content fence

**Classification:** target-architecture recovery identity and lifecycle defect.

**Evidence.** Admission-violation recovery is required after candidate, acceptance, content-fence installation, preparation, and barrier authorization; it invalidates the old cut/candidate/accepted token while preserving every restrictive fence (`ARCHITECTURE-SPINE.md:222-224,283,461-465`; `epics.md:3009-3012`). The successor ordinal eventually creates a new global cut and accepted token. Yet `AuthorizeDeletionScopeWriteFence` and its effect require that new exact token/current ordinal, while the installed content-fence receipt is bound to the old token/global cut/ordinal; the architecture names one persistent content fence and forbids removal except the approved operator Abort path (`launch-readiness-register.md:309-311,317,320-324`; `ARCHITECTURE-SPINE.md:463,475,546`). No row says that an unchanged-scope existing fence may gain a successor binding, nor defines a separately keyed per-ordinal content-fence instance and corresponding completion/removal manifest.

**Two literal units.** Team A reuses the old content-fence receipt because its canonical predicate is unchanged, thereby ignoring the required new-token binding. Team B attempts the ordinary install with the new token and receives an immutable-write/idempotency conflict against the existing fence id, so preparation never resumes. Team C installs a second fence with an undocumented id and later removes or verifies only one of them. All preserve restriction, but they produce incompatible receipts and recovery behavior.

**Impact.** A violation in a specifically required pre-seal failure window either strands deletion permanently or weakens the exact-token evidence chain. This is High.

**Required correction.** Decide the architecture identity without choosing Product policy: either (a) make the predicate-scoped content guard independently immutable and add an authenticated successor-binding operation/result that proves the same fence remained continuously installed while binding the new accepted token/ordinal, or (b) key content fences per ordinal and update install, verification, migration preservation, completion, integrity containment, and approved removal to enumerate every instance. Add failure injection after content-fence install and after prepare.

### H-v18-3 — One barrier-wide “one-shot” capability is undefined for a deletion set containing multiple per-interaction DEKs

**Classification:** target-architecture destructive-effect capability and recovery defect.

**Evidence.** Payload protection uses one DEK per `AgentInteraction`, and a deletion inventory may contain many interactions (`ARCHITECTURE-SPINE.md:204,459`; `external-dependency-register.md:233`). The guard returns one authenticated “one-shot sole capability for DEK destruction,” `RecordDeletionDestructionStarted` requires that singular capability to be unconsumed, and the protection engine accepts only that receipt (`ARCHITECTURE-SPINE.md:224,465,546`; `IMPLEMENTATION-CONVENTIONS.md:25`; `launch-readiness-register.md:326-328`; `epics.md:3027`). The committed protection dependency exposes per-DEK `DestroyDek` with per-interaction irreversible receipts, not an atomic manifest-wide destruction operation (`external-dependency-register.md:233-236`). No contract defines whether the one shot is consumed once for the whole accepted-set batch, once per DEK, or once merely to enter the destructive phase; nor does it derive resource-bound child capabilities or an atomic consumption ledger.

**Two literal units.** Team A submits the barrier receipt to the first `DestroyDek`; the one-shot rule then forbids all remaining keys. Team B reuses the same receipt for every inventory member, contradicting one-shot consumption and making replay/late-compromise invalidation implementation-dependent. Team C invents an atomic batch call that the dependency does not publish. Crash after the first key and response loss produce different permitted retries in all three units.

**Impact.** Multi-interaction deletion either cannot complete or relies on an undocumented reusable bearer capability at the irreversible boundary. The fail-closed form is a durable wedge, while the permissive form weakens replay and target binding, so this is High.

**Required correction.** Define one closed shape end to end: an atomic manifest-bound destroy operation returning an exact per-key outcome vector and exact lookup, or a barrier-owned append-only capability ledger deriving deterministic single-use child capabilities for every accepted/contained `(TenantId, AgentInteractionId, TargetProtectionKeyAlias)` and consuming each atomically with its `DestroyDek` outcome. Specify capability identity, target binding, first/remaining effect order, post-seal compromise behavior, crash/lost-ack recovery, and completion proof.

## v17 Finding Recheck

| v17 finding | v18 disposition |
| --- | --- |
| C-v17-1 clean-ledger read followed by unrelated `ProtectionFence` append | **Closed for admission writes.** The guard now atomically verifies both ledgers, commits immutable `DestructionSealed`, and serializes that commit with every matching permit/intent/lease/phase-authorization append. The commit, not the preceding read or fence authorization, is `DestructionStarted` (`ARCHITECTURE-SPINE.md:224,465`; `launch-readiness-register.md:325-327`; `external-dependency-register.md:129,132`). C-v18-1 is a different cross-owner race with hold intent, which the new admission serialization does not cover. |
| H-v17-1 Effective owner could not recut and zero was request-lifetime ambiguous | **Closed as stated.** Owner cycles are separately keyed by request/scope/ordinal, Effective is immutable, successor manifests carry still-restrictive obligations and new winners, the ledger has immutable ordinal partitions, and only current-ordinal `ZeroAcceptedViolationsSinceInstall` binds a new global cut (`ARCHITECTURE-SPINE.md:214,220-222,544`; `launch-readiness-register.md:290-304`; `IMPLEMENTATION-CONVENTIONS.md:23`). H-v18-1 and H-v18-2 are uncovered timing and content-fence continuation branches, not the old identity defect. |

## Authoritative Validation And Package Reconciliation

The authoritative report's C-1 through C-3 and H-1 through H-12 remain closed in the target architecture: safety authorization is conjunctive; legal hold/export/deletion share the fence; matrix-v4 bootstrap and platform scope are explicit; scheduled Approver resolution is leased; safety rescan has finite authoritative enumeration; human identity is principal-kind tagged; rate, open-interaction, and Budget lifetimes have distinct owners; proposal and User-action/source ingress are crash-consistent; Conversations core and optional retraction dependencies are split; trusted-envelope replay/rotation and non-recursive denial recording are closed; Dapr's current transitive presence is distinguished from target workflow adoption; public-contract parity is not falsely claimed; and story/dependency readiness is governed by the registers. The bound PRD retains late armed-hold precedence, operator cancellation, operator nonterminal disposition, human exact-Conversation scope, class/range scope, instruction protection, and legacy plaintext as explicit decisions rather than silently chosen defaults. C-v18-1 requires only fail-closed serialization while the hold decision is Open; H-v18-1 through H-v18-3 select no Product outcome.

## Areas That Converge

- The admission fence, owner cohort, separately keyed owner/ordinal Closing-to-Effective cycles, carried-forward obligation union, same-ordinal fixed point, and current-ordinal zero-since-install proof now give compatible teams the same ordinary and post-cut recut state.
- The EventStore guard is the named owner of the admission ledger and atomic `DestructionSealed` boundary. All later ordinary matching admissions are unconditionally rejected; integrity-failure acceptance is recorded separately and can never be hidden as content-only containment.
- Operator fence-removal authorization and barrier authorization contend at the same `ProtectionFence` revision. Removal covers every admission ordinal plus the content fence, precedes every owner-cycle release, and makes the guard refuse a later barrier; no operator or Workflow may enable this branch while its Product decision is Open.
- Migration repair freezes only after its guard fence, drains a finite pre-boundary cohort, preserves every active deletion ordinal/cycle/content fence/barrier, and atomically revokes the old epoch, repair fence, and bridge on successor installation.
- Cross-tenant scope membership remains derived from authoritative tenant/permit/source facts, and malformed, cross-tenant, unknown-version, or unresolvable evidence rejects writes and blocks clean verification.
- AD-1 through AD-31 and all existing Open Decision ids remain preserved. The review invents no human exact-Conversation, operator-cancellation, operator-nonterminal, class/range, hold-precedence, export-lifecycle, instruction-protection, legacy-plaintext, rate/concurrency, decision-recorder, or output-status outcome.

## Architecture Defects Versus Implementation Debt

C-v18-1 and H-v18-1 through H-v18-3 are target-architecture handoff defects. They concern the chosen barrier, recut, content-fence, and destructive-capability mechanics and must be closed before independently implemented units can converge. C-v18-1 does not decide the late-hold Product branch; it enforces the PRD's existing fail-closed safe state until that branch is approved.

The current repository remains materially behind the target. It contains no shipped directory/effect-authorization namespace, ordinal admission guard/ledger, owner-cycle recut, content fence, atomic deletion barrier, capability-consumption ledger, migration repair bridge, decision catalog, three-ledger protocol, safety epoch, export store, or Conversations deletion feed. `EXT-HOST-1`, `EXT-CONV-AI-1`, `EXT-PARTIES-1`, protection, secrets, Provider, and export targets remain `Uncommitted`/TBD as the registers state. The server currently receives Dapr transitively and calls `AddDaprClient`, but no target Dapr Workflow owner is implemented; package and submodule checkout differences are delivery reality. None of that implementation debt is evidence for weakening the target invariants, and none of the four findings is merely a missing current-code feature.

## Gate Result

The v18 adversarial-divergence gate is **FAIL** because Critical and High counts are nonzero. Deterministic lint: **PASS**, `ok: true`, zero findings. Frozen-input after-write hash verification: **PASS**; all eight hashes remain exactly the values listed above.
