---
name: Hexalith Agents good-spine rubric review v22
type: architecture-spine-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-13
intent: review-read-only
lens: bmad-architecture good-spine rubric walker
verdict: fail
critical: 0
high: 1
medium: 3
low: 2
lint_ok: true
---

# Good-Spine Rubric Reviewer Gate — v22

## Verdict

**FAIL — 0 Critical, 1 High, 3 Medium, 2 Low.** The v22 target closes the v21 protection-owner consume/cancel race, signed-but-unissued recovery gap, registrar authority gap, and runtime hold-decision scope gap. One current handoff contradiction remains High: AD-3/AD-30 and the matrix define three pre-command security primitives, but the spine's mutation convention and the implementation-conventions adoption test still permit only the original two and reject every other bypass. Independently built conformance/security units can therefore reject the required deletion-capability compromise registrar and strand emergency revocation.

## Frozen Inputs And Scope

I read the repository instructions and the complete BMad reviewer-gate instructions, then re-walked the full good-spine rubric against the current spine, implementation conventions, authoritative validation, bound PRD, epics, both registers, declared sources, and focused current repository contracts. The review covered enforceability and divergence prevention; ownership and cross-owner concurrency; authorization/effect/result order; crash, replay, lost-ack, migration and restore behavior; tenancy, separation of duties, security and irreversible data loss; Product-decision discipline; PRD/epic/register consistency; brownfield ratification; architecture-versus-delivery debt; source integrity; and mechanical correctness.

Per the explicit review instruction, I did **not** read or modify the architecture `.memlog.md`; only its supplied SHA-256 was recomputed. Consequently, this report does not claim a content-level append-only memlog audit. No reviewed artifact, source file, submodule, PRD, register, conventions file, or memlog was modified. This report is the sole write.

The supplied hashes matched before review and were recomputed unchanged after report creation:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `38db1ac6d9fe9cc1155ec042e8217ed2395d9c4399a2ac0e4d4e70c890ad1fa8` |
| `IMPLEMENTATION-CONVENTIONS.md` | `ec646e305acfe24534333e1db80aa4e9a6cd613561b84ab9752f50c846506f29` |
| architecture `.memlog.md` — hash only | `907d88500cd5d66b4e0d7a818944d2f4ef34d7faacc3e416dde2ea7550d95dfb` |
| bound `prd.md` | `48ba04a4d89441fe9c6da95a114c307f2206e73eb42bda06635e4ba03073beee` |
| `epics.md` | `97dcb91fbd6e2418bb17f4c81303285d364d40a8071c1c42bc2be9ce4179d6de` |
| `external-dependency-register.md` | `78ecf69d009f828fb92da1f27c209a2f54676a9b4a2e39ff0fdf68eadd3206c4` |
| `launch-readiness-register.md` | `2f568fe50edf567cd7b39d24fa3ac43e077839ffe561a00874cb271e65198b8d` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

## Deterministic Lint And AD Preservation

Command:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: **PASS**, `ok: true`, `total_findings: 0`.

An independent heading/required-field scan found exactly one each of AD-1 through AD-31, maximum AD-31, no gaps or duplicate/reused headings, and `Binds`, `Prevents`, and `Rule` on every AD. The current architecture preserves all existing AD identities and adds no renumbered replacement. Because memlog contents were intentionally excluded, preservation is established from the current spine and prior frozen review evidence, not from a new memlog-content comparison.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 1 |
| Medium | 3 |
| Low | 2 |

## Critical

None.

## High

### H-R22-1 — Current conformance rules still reject the third pre-command compromise registrar

**Classification:** target-architecture security/authority handoff contradiction; not implementation debt and not an unresolved Product choice.

**Disposition:** autofix the current rule/test inventories, then rerun the gate.

**Evidence.** V22 correctly defines three and only three pre-command security primitives:

- AD-3 names `RegisterTrustedEnvelopeFirstSeen`, `RecordSecurityObservation`, and `RegisterDeletionCapabilityKeyCompromise` (`ARCHITECTURE-SPINE.md:275`).
- AD-30 calls `IDeletionCapabilityCompromiseRegistrar` the “third and final” primitive and closes its authenticated input, target, ACL, replay/lost-ack behavior, and prohibited authorities (`ARCHITECTURE-SPINE.md:574`).
- The implementation convention's normative exception paragraph likewise requires exactly three (`IMPLEMENTATION-CONVENTIONS.md:43`), and matrix-v6 exposes three gate-free pre-command rows (`launch-readiness-register.md:232-234`).

Two current handoff rules contradict that closure:

- The spine's **Mutation** consistency convention says the *only* pre-command exceptions are the replay registrar and security recorder, and says every other direct append/capability is rejected (`ARCHITECTURE-SPINE.md:863`).
- The implementation-conventions adoption checklist requires the direct-mutation guard test to permit only “the two AD-3 pre-command capability/event/stream triples” (`IMPLEMENTATION-CONVENTIONS.md:79`). A literal implementation of that mandatory test rejects `RegisterDeletionCapabilityKeyCompromise` as a bypass even though lines 43 and AD-3 require it.

Story 5.4's evidence metadata also still labels AD-30 as having “two-nonrecursive-pre-command-capabilities” (`epics.md:1480`). Story 8.3 owns registrar implementation and tests (`epics.md:3037-3038,3063-3073`), so this metadata can be scoped to the two Story-5.4 capabilities, but it must not restate the whole AD-30 inventory as two.

**Executable divergence.** A boundary-test team follows `IMPLEMENTATION-CONVENTIONS.md:79` and builds an allowlist of two, proving every other pre-command route is rejected. A deletion/security team follows AD-3, AD-30, Story 8.3, and matrix-v6 and requires the third registrar to install a per-tenant/key-version protection-owner block before guard mirroring. Both comply with a current mandatory clause, yet the first makes the second path impossible. During key compromise, old issued credentials remain blocked only if the registrar can execute; rejecting it can strand or inconsistently handle emergency revocation and re-attestation.

**Required correction.** Change `ARCHITECTURE-SPINE.md:863` and `IMPLEMENTATION-CONVENTIONS.md:79` to enumerate/permit exactly the same three target-limited primitive/capability/event/owner paths already closed by AD-3/AD-30 and line 43. Clarify `epics.md:1480` as Story 5.4 ownership of the first two rather than an assertion that AD-30 globally has two. The generic negative test must allow the third only for its exact authenticated Secrets-revocation subscriber, protection block, and guard mirror and continue rejecting every API, human, Workflow, general-dispatcher, wrong-tenant, wrong-key-family, wrong-stream/event, or broadened operation.

## Medium

### M-R22-1 — Architecture-owned `RQ-1` assumptions still lack literal retirement dates

**Classification:** planning-governance debt; safely blocking, not a runtime architecture defect.

PRD §8.1 requires an Architecture-owned `RQ-1` assumption to carry a co-owner-approved literal calendar retirement date. `ARCH-A-1`, `ARCH-A-2`, `ARCH-A-3`, the remaining test-stack portion of `ARCH-A-4`, `ARCH-A-6`, `ARCH-A-7`, `ARCH-A-8`, `ARCH-A-11`, `ARCH-A-12`, and `ARCH-A-14` remain `Unscheduled` or milestone-only (`ARCHITECTURE-SPINE.md:1381-1395`; `prd.md:898`). The spine correctly keeps them as `UnretiredAssumption` blockers, so this does not enable an unsafe readiness pass. Obtain owner-approved dates; Architecture must not invent them.

### M-R22-2 — `PostingPending` still has no single concrete timeout authority

**Classification:** safely surfaced Architecture/Product parameter.

AD-5 fixes only “no shorter than” the Conversations seam-2 timeout and explicitly says neither the PRD nor spine fixes a more specific duration (`ARCHITECTURE-SPINE.md:287,1394`; `prd.md:438`). Two otherwise conforming posting/recovery units can persist different deadlines. Bind one literal duration or one exact versioned seam/profile field and obtain Product confirmation before the posting/recovery story; this review selects none.

### M-R22-3 — Two declared historical v21 review sources are absent

**Classification:** source/provenance defect; not a runtime contract defect.

The frontmatter declares `reviews/review-2026-09-13-security-data-integrity-v21.md` and `reviews/review-2026-09-13-brownfield-drift-v21.md` (`ARCHITECTURE-SPINE.md:122-123`), but neither path exists. The five v22 paths were treated as concurrent anticipated outputs of the active gate; the two v21 paths are historical declarations and have no such current-run exemption. Restore the actual reports or remove the unsupported source declarations with an auditable explanation. The authoritative validation, v21 rubric/verified/adversarial reports, and all other declared local sources resolved.

## Low

### L-R22-1 — Root bUnit remains behind the current checked-out catalog

**Classification:** implementation/build maintenance, not architecture.

Root authority still pins bUnit `2.9.0` (`Directory.Packages.props:26`), while the currently checked-out Builds catalog observed by the package names `2.10.3`. The spine accurately treats catalog alignment as Story 5.6 delivery debt, so no target-rule divergence follows.

### L-R22-2 — One carried-forward fixture paragraph is still labelled matrix-v5

**Classification:** re-distillation/editorial debt.

The launch register makes matrix-v6 the sole current contract and adds explicit matrix-v6 tests (`launch-readiness-register.md:180,208,373`), but the immediately preceding common fixture paragraph still calls its deletion corpus “Matrix-v5 deletion fixtures” (`launch-readiness-register.md:369`). Version 6 carries those semantics forward, so the tests remain meaningful and no current execution is authorized under v5. Relabel the corpus as carried-forward matrix-v6 coverage to keep the final handoff present-tense.

## V21 Critical/High Closure Audit

| V21 finding | V22 disposition |
| --- | --- |
| Adversarial `C-v21-1` — guard blocker could win after lookup but lose to protection consume | **Closed.** Admission-integrity and key-compromise blocks now race `ReserveAndConsumeDeletionBatch` on one protection-owner state; `ConsumptionReserved` is the irreversible instant, exact lookup resolves loss, unknown stays restrictive, and migration/restore preserve the result (`ARCHITECTURE-SPINE.md:263-265`; `IMPLEMENTATION-CONVENTIONS.md:33`; `launch-readiness-register.md:356,358-359`; external register v22 extensions at lines 139 and 251). |
| Rubric `H-R21-1` — re-attestation and old-attestation consume lacked common linearization | **Closed.** Replacement begins only from protection-owned `ConsumptionBlocked(CapabilityKeyCompromise)`, remains blocked through guard replacement and successor dispatch, and reopens only through protection-owner `ActivateReattestedDeletionBatch`; a reservation/consume winner makes replacement illegal (`ARCHITECTURE-SPINE.md:263-265`; matrix rows 335-338,359). |
| Adversarial `H-v21-1` — signed-but-unissued result had no terminalization/retry identity | **Closed.** Signed bytes and request identity bind `SigningAttemptOrdinal` plus intended issue revision; exact no-issue lookup precedes `SignedAttestationObsoleteUnissued`; the successor keeps batch/manifest/attestation and increments only signing attempt (`ARCHITECTURE-SPINE.md:267`; matrix rows 328-331). |
| Adversarial `H-v21-2` — Secrets emergency issuer could not fit the principal model | **Mechanism closed, handoff not fully closed.** AD-3/AD-30 now define the non-public registrar outside command principals, with exact protection-before-guard ordering, ACL, replay and recovery. H-R22-1 is the remaining contradictory two-exception conformance inventory that can reject this otherwise executable primitive. |
| Verified `VC21-C1` — runtime hold-decision seed excluded post-start boundaries | **Closed.** The decision row and initial-catalog baseline include pre-seal contention plus containment, dispatch, protection reservation, purge, completion, and Story 8.1/8.3 branches, while selecting no outcome (`launch-readiness-register.md:95,118`; `epics.md:3070`; `ARCHITECTURE-SPINE.md:1309`). |

The prior v21 per-violation-versus-per-target Medium is also closed: current identity, spine, matrix, Story 8.3, and protection dependency all deduplicate on complete `(TenantId, AgentInteractionId, TargetProtectionKeyAlias)` and use coverage links without a second destructive grant.

## Authoritative 2026-09-12 Critical/High Closure Audit

| Authoritative finding | Current disposition |
| --- | --- |
| `C-1` weaker safety evaluation | **Closed.** Snapshot plus every then-current applicable policy are conjunctive; dominance is allowed only as tested equivalence. |
| `C-2` hold/deletion exclusion | **Closed.** Admission/content fences, immutable owner/ordinal cut cycles, fence-owned accepted inventory, shared hold/seal guard, all-or-none target batches, coverage, post-start disposition gating, and guard-linearized completion prevent partial false success. |
| `C-3` matrix bootstrap/scope deadlock | **Closed.** Matrix-v6 has explicit target scope, gate-free pre-command/service rows, and target-aware bootstrap/repair variants that omit only their circular gate while retaining unrelated fail-closed checks. |
| `H-1` scheduled Approver lifecycle | **Closed.** Single-flight ownership, cadence/freshness, two-pass empty evidence, leased resolution, state-specific results, and unavailable recovery are bound. |
| `H-2` distributed safety rescan | **Closed.** `SafetyVerdictEpoch`/`SafetyVerdictIndex` own a finite manifest, fenced bounded worker coordination, activation, recovery, and exact `RescanPending`. |
| `H-3` human-only Approvers | **Closed.** Parties-authoritative human type, liveness, historical actor binding, and current access are enforced at configuration and every resolution point. |
| `H-4` ledger lifetime conflation | **Closed.** Rolling rate, caller-open interaction, monthly monetary Budget, and capacity identities/lifetimes/settlement are separate. The still-Product-owned concurrency-rejection consumption choice is isolated and fail-closed. |
| `H-5` human identity/separation | **Closed.** Stable `AuthenticatedHumanActorId` spans User, Administrator, and Platform evidence; historical Party binding is explicit; Workflow cannot satisfy human separation. |
| `H-6` proposal/index crash consistency | **Closed.** Interaction truth, source-revision outboxes, directory high-waters, acknowledgements, leases, and checkpoint reconciliation are explicit. |
| `H-7` indivisible Conversations dependency | **Closed.** Six core seams remain in `EXT-CONV-AI-1`; optional retraction is independently governed by `EXT-CONV-RETRACTION-1`. |
| `H-8` trusted-envelope security | **Closed for the original envelope issue.** Canonical MAC input, logical/delivery identity, replay owner, time/key lifecycle, ACL confinement, durable security spool, and lost-ack recovery are bound. H-R22-1 concerns the later third compromise primitive's stale allowlist, not the original envelope contract. |
| `H-9` export bytes/lifecycle/signature | **Closed subject to the named Product lifecycle decision.** Store/index ownership, immutable AEAD artifact, JCS/ES256 manifest, direct key delivery, hold/expiry fencing, cleanup, restore, and all-copy receipts are explicit. |
| `H-10` Dapr current truth | **Closed as current-reality classification.** Current transitive Client/ASP.NET `1.18.5` exposure, the non-authoritative checked-out Builds update, and future Workflow adoption remain distinct. |
| `H-11` public contracts falsely described as current | **Closed.** Required-completion vocabulary and current delivery debt are separated. |
| `H-12` tracker/dependency contradiction | **Closed as surfaced delivery-governance debt.** `OD-SPRINT-5.1-5.2-1` records the owner decision without rewriting delivery history or granting architecture authority. |

## Product Decisions And Architecture Versus Delivery Debt

The package continues to surface rather than select the unresolved hold/deletion precedence, hold-prepare cancellation, operator-deletion cancellation/nonterminal treatment, export lifecycle, rate/concurrency consumption, Dapr security, sprint evidence, historical safety, Automatic retraction, instruction protection, legacy plaintext, initial output-safety status, class/range scope, human exact-Conversation scope, and Release-recorder scope decisions (`ARCHITECTURE-SPINE.md:1305-1326`; `launch-readiness-register.md:89-118`). Their affected evaluations and restrictive states are explicit. No Product outcome is inferred by this review.

Current code contains none of the v22 registrar, signed-attempt, protection-owner block/reserve/activation, deletion guard, directory/effect-lease, decision-catalog, safety-epoch, export-store, or separated-ledger target mechanics. The spine assigns these absences to stories and dependency contracts (`ARCHITECTURE-SPINE.md:1328-1350`), and all twelve `EXT-*` records remain `Uncommitted`. Those absences are delivery debt and readiness blockers; they are not findings against the target architecture. H-R22-1 is different: it is a contradiction inside the target handoff itself.

## Whole Good-Spine Checklist

| Dimension | Result |
| --- | --- |
| Real divergence points / enforceable ADs | **Fail High at H-R22-1.** The substantive AD rules otherwise name owners, revisions, identities, outcomes, and recovery paths. |
| Prevents stated divergence | **Fail High only for registrar conformance.** A mandatory two-item allowlist can reject the third required primitive. |
| Ownership and concurrency | **Pass.** V22 gives compromise/admission cancellation and destructive reservation one protection owner; hold/dispatch/completion retain deliberate guard ordering. |
| Recovery, replay, migration and restore | **Pass.** Signed-unissued, block-pending, protected-owner terminal states, gap chains, stale authorizations, exact lookup, and preservation are closed. |
| Security, tenancy and irreversible data | **Fail High at H-R22-1; otherwise pass.** Target/audience/tenant/key/manifest binding and fail-closed unknown states are explicit. |
| PRD/epic/register/convention traceability | **Fail High at H-R22-1, with two Medium planning parameters.** The substantive v22 mechanics are otherwise reconciled. |
| Product-decision discipline | **Pass.** Open outcomes are named, owned, scoped, and restrictive; none is selected. |
| Architecture versus implementation debt | **Pass.** Current absence is assigned delivery debt; target contradictions are not mislabeled as backlog. |
| Brownfield/current reality | **Pass with Low maintenance tail.** Current target-mechanism absence and bUnit/root-catalog lag are accurately described. |
| Sources | **Medium.** Two historical v21 source paths are unresolved; active v22 outputs are concurrent anticipated gate deliverables. |
| Deferred / altitude completeness | **Pass.** Deferred V1 exclusions cannot silently enter V1; deployment, topology, dependency, readiness, operations, recovery, and governance dimensions are represented. |
| Mechanics | **Pass.** Deterministic lint and AD identity/required-field scans are clean. |

## Gate Result

The frozen v22 good-spine rubric gate is **FAIL** because the High count is nonzero. Correct H-R22-1 without broadening the registrar, weakening its protection-before-guard order, or inventing a Product outcome; reconcile the Medium/Low tail as appropriate; re-distill; then rerun the complete reviewer gate.
