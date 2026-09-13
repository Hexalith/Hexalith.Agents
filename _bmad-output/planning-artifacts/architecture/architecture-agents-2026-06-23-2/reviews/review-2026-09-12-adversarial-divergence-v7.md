---
name: Hexalith Agents adversarial-divergence review v7
type: architecture-review
lens: adversarial-divergence
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
verdict: fail
critical: 1
high: 6
medium: 2
low: 1
lint_ok: true
---

# Adversarial Divergence Review — v7

## Verdict

**FAIL — the v6 corrections close their named recovery, replay, security-recorder, safety/budget, status, export-cleanup, and decision-record defects, but one legal-hold/export preservation hole and six other independently implementable High divergences remain in the frozen cross-artifact contract.**

This is a fresh whole-artifact pass, not a v6 closure checklist. It treats the binding PRD, epics, both normative registers, implementation convention, repository instructions, validation input, current memlog, and parent-gitlink repository reality as one handoff. The deterministic spine linter reports `ok: true` with zero findings.

The reviewed snapshot was hash-checked before analysis and again immediately before this review was written:

- `ARCHITECTURE-SPINE.md`: `98513d95e17af7df25d35f4ae45b35243f60b4f1aaf4ed69df760d671cc8f4ff`
- `VALIDATION-REPORT-2026-09-12.md`: `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529`
- bound `prd.md`: `94e2f751e01abd71278c6dcd1b7384206b55263a325905b4d3bc6016066c28fc`
- `epics.md`: `365d446eea6512aaf71b472fb8342db0b5f2ab9037af67b001220d4ddd282284`
- `external-dependency-register.md`: `3a88a794862ba180c9d920de0aa087ab28400a24f0e0ecc7a749add748c50e0c`
- `launch-readiness-register.md`: `9180a7d6c84337073416309fd1527b2a5f43aa1b990789baf2e087e472ba03b6`
- `IMPLEMENTATION-CONVENTIONS.md`: `2ebba31863470f233aeeeccca45283542be2b6e84e3253323b97e7437bf1e7fd`
- architecture `.memlog.md`: `d5f0ffe6363f58aa75e7200ffc840b719711a537fed100b4b13eaf6ef07359f2`
- repository instructions: `bfbe399b567d852a74c7ee8c9217217a3ea5eb80ca591b5b8ac01acfbe838966`
- Reviewer Gate instructions: `d32e32a3c1d59b5612b947004f3f6fef1117a13ce9f1ffa428d616e0b5d4db69`

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 6 |
| Medium | 2 |
| Low | 1 |

## Critical

### C-1 — A later legal hold can become `Active` without preserving an already committed export artifact

**Evidence.** AD-22 requires `ApplyHold` to freeze a set, pin every DEK **and export artifact**, and become Active only after every acknowledgement; it also says later-hold treatment is owned by `OD-EXPORT-LIFECYCLE-1` (`ARCHITECTURE-SPINE.md:345`). The decision's affected evaluations omit `GovernanceProtection:HoldPrepare` and both hold-recovery variants (`launch-readiness-register.md:96`). The matrix's hold rows require only protection/secrets and `ExistingPinOutcomesLookupExact`, unlike export/deletion rows that explicitly require the store and lifecycle version (`launch-readiness-register.md:217-228`). Story 8.1 neither depends on `EXT-EXPORT-STORE-1` nor binds the lifecycle decision, and the store register lists only Stories 8.2/8.3 as consumers (`epics.md:2730-2787`; `external-dependency-register.md:193-205`).

**Two literal units.** A LegalHold team implements its Story 8.1 and matrix row, pins interaction DEKs, and reports Active without calling the unavailable/unlisted store. An export-store team implements expiry according to its approved lifecycle decision and has no authenticated hold token for that artifact, so it physically purges or key-destroys the committed export while the hold is Active. A second LegalHold team treats “every export artifact” as an implicit store dependency and blocks or pins it. Both readings satisfy a different normative authority, but the first irreversibly loses held evidence.

**Impact.** Legal-hold status can assert protection while an independently compliant store destroys the only exported copy. This is an irreversible governance/data-integrity breach, not implementation debt.

**Disposition — AUTOFIX without deciding the open product policy.** Add hold prepare/release and their recovery variants to the closed affected-evaluation set of `OD-EXPORT-LIFECYCLE-1` wherever committed artifacts overlap; bind the exact lifecycle `DecisionVersion`, fence export-index high-water/frozen artifact set, store availability, and artifact pin/unpin outcome lookup in the matrix. Add `EXT-EXPORT-STORE-1` to Story 8.1 and its register consumers for the overlapping-artifact branch, with failure-injection coverage. The choice of whether a later hold extends or supersedes expiry remains Product/Governance/Security's open decision.

## High

### H-1 — Platform second-party approvals are both Party-free and required to prove a current human Party

**Evidence.** AD-30 intentionally defines `Platform` principal identity as (`AuthenticatedHumanActorId`, `ActorTenantId=system`) with no `PartyId`, sourced from Tenants' global-administrators authority, and expressly allows that principal as the human second-party for `LegalHoldRelease` (`ARCHITECTURE-SPINE.md:401-403`). Stories 8.1, 8.2, and 8.8 instead require `EXT-PARTIES-1` to prove **both** requester/approver actors are current human Parties with historical Party-to-actor bindings, while PRD FR-33 permits a Platform Operator on each applicable approval branch (`epics.md:2754-2761,2808-2814,3164-3173`; `prd.md:481-511`).

**Divergence.** One unit accepts a Platform Operator from the global-administrator projection and compares its stable actor id, exactly as AD-30 says. Another rejects it because no target-tenant Party/binding exists, exactly as the story AC says. A third invents a synthetic Party for a platform-global human. Hold release, wide inspection, and export approval therefore disagree on the same actor.

**Disposition — AUTOFIX.** State one actor-evidence union everywhere: Party-bearing tenant actors require current/historical `EXT-PARTIES-1` evidence; a `Platform` actor requires current and historically recorded Tenants global-administrator evidence, with both branches compared by stable `AuthenticatedHumanActorId`. Do not invent a tenant Party for `Platform` unless Product and identity owners explicitly choose that model. Amend the three story criteria and evidence manifests accordingly.

### H-2 — `EXT-PARTIES-1` does not gate all stories that perform live human approver or destructive-governance decisions

**Evidence.** AD-8 re-resolves human type, liveness, actor binding, and current access at discovery, edit, regeneration, approval, rejection, abandonment, recheck, and audit inspection; AD-30 requires a Party-bearing human command to match that binding (`ARCHITECTURE-SPINE.md:227,401`). Yet the dependency register and Epic-level summary declare only six human/identity consumers—5.2, 5.4, 6.6, 8.1, 8.2, and 8.8—omitting Stories 7.1–7.5 and the distinct Compliance Inspector approval before Story 8.3 destruction (`external-dependency-register.md:105-119`; `epics.md:167,2333-2657,2858-2910`). Story 8.3's external requirements omit Parties entirely even though its deletion becomes executable only after human Compliance approval (`epics.md:2865-2879`).

**Divergence.** The dependency gate can mark proposal-action or deletion stories ready while Parties remains `Uncommitted`/unavailable; a story team can use the trusted actor captured earlier, whereas an AD-8/AD-30 team performs the mandatory fresh binding and blocks. Identical human actions then differ in authorization, and irreversible deletion can proceed without the evidence the architecture requires.

**Disposition — AUTOFIX.** Enumerate every direct live `EXT-PARTIES-1` consumer in the register and story dependencies, at minimum 7.1–7.5 and 8.3, or define and bind an explicit previously-built authorization service whose own availability transitively and mechanically gates those stories. Story 8.3 must test current/historical actor binding, same-actor denial, and unavailability before `DestructionStarted`. Preserve the separate AD-30 global-Platform evidence branch from H-1.

### H-3 — Deferred PRD decisions have no runtime record or affected-evaluation contract

**Evidence.** PRD FR-28 requires `OpenDecision` for every Deferred §13 row due before enablement; OQ-18, OQ-23, and OQ-31 have distinct applicability, including tenant-mode-specific enablement for OQ-23 (`prd.md:729,1070,1075,1083`). AD-17 and the launch register make `ArchitectureDecisionRecord` the durable owner only for Spine `OD-*` rows, and the register materializes only those five rows (`ARCHITECTURE-SPINE.md:313`; `launch-readiness-register.md:78,89-101`). Runtime is explicitly forbidden to parse either planning document. No stable PRD decision id/version, initial Open record, approval contract, or `AffectedEvaluations` mapping exists for those OQs.

**Divergence.** One RQ-1 implementation compiles the prose OQ table into code, one reuses `ArchitectureDecisionRecord` with invented ids, and one evaluates only the materialized Spine OD rows. The last can omit OQ-18/OQ-31 from RQ-1 and OQ-23 from Automatic-mode enablement; the other two disagree on supersession and scope.

**Disposition — AUTOFIX without resolving the product questions.** Extend the existing durable decision model (or name a separate one) with stable records for every Deferred PRD decision that can block, initially `Open`, exact owners/required evidence, versions, and closed affected evaluations. Bind OQ-23 only to first Automatic-mode enablement, not global RQ-1, as the PRD requires. A planning-document edit must not be the runtime state transition.

### H-4 — A valid rate-preparation profile may let an admission commit after its consumption already reset

**Evidence.** AD-21 derives `PartyResetAt` and `ConversationResetAt` from `AdmissionEvaluatedAt`, derives `PreparationDeadline` independently from the positive `AdmissionPreparationTimeout`, aborts only a failed or deadline-expired preparation, and keeps committed consumption only until its exact reset (`ARCHITECTURE-SPINE.md:339`). The NFR-12 profile requires only that `AdmissionPreparationTimeout` be positive; it has no upper bound relative to either frozen rolling window (`launch-readiness-register.md:287-303`). Story 6.4 repeats both rules without a relation between them (`epics.md:2028-2035`).

**Divergence.** With a 30-second rate window and a valid 60-second preparation timeout, both ledgers can acknowledge at second 40. One unit commits because the preparation deadline has not passed, then treats the consumption as already expired at `ResetAt`; another aborts at the earliest reset or invents a reset extension. The first admits Provider work without durable rolling consumption.

**Disposition — AUTOFIX.** Require `PreparationDeadline` to be strictly earlier than both scope `ResetAt` values, or make an elapsed scope reset a named interaction-owned abort condition even when preparation has not otherwise timed out. Freeze and validate that relationship before either ledger is contacted, and add boundary/lost-ack fixtures.

### H-5 — Safety rescan has no authoritative Conversation-enumeration source or atomic tenant-creation handshake

**Evidence.** AD-20 says platform policy publication freezes an “active-tenant checkpoint,” each tenant epoch freezes an “authoritative Conversation-enumeration checkpoint,” and a tenant created after the first checkpoint initializes at the pending/latest epoch before callability (`ARCHITECTURE-SPINE.md:333`). Story 6.3 calls this the “eligible Conversation cohort” and requires every frozen member plus every post-checkpoint initialization to acknowledge (`epics.md:1968-1985`). Neither artifact names the owner, cursor grammar, completeness/high-water proof, or the expected-revision handshake between tenant creation and global policy activation. Story 6.3 does not consume `EXT-CONV-AI-1`, and that register record supplies content/roster reads and an SM-2 count/event feed, not a committed point-in-time tenant-wide enumeration seam (`external-dependency-register.md:51-69`).

**Divergence.** One coordinator enumerates only existing `SafetyVerdictIndex` streams, one invents a Conversations-wide enumeration, and one uses the SM-2 feed. Concurrent tenant creation between checkpoint capture and `PendingActivation` can miss the frozen cohort; later callability is safe-failed, but no named owner is required to initialize and unblock it. Policy activation timing, scan load, and tenant availability are incompatible.

**Disposition — AUTOFIX.** Name the active-tenant and Conversation-enumeration authorities, stable checkpoints/cursors, inclusion grammar, and a compare-and-append handshake that makes tenant creation either part of the frozen cohort or durably responsible for initializing the pending/latest epoch before activation/callability. Add the actual external seam and consuming story if Conversations owns enumeration; otherwise state explicitly that Agents' own indexed-conversation set is the cohort and define on-demand initialization for an unindexed Conversation.

### H-6 — The hold/deletion decision's late-hold outcome is not applied to the hold command that creates the race

**Evidence.** `OD-HOLD-DELETION-PRECEDENCE-1` explicitly leaves “behavior of a hold arriving against an armed deletion” to Product/Governance/Security (`ARCHITECTURE-SPINE.md:959`; `launch-readiness-register.md:95`). Its affected evaluations and matrix precondition cover `DeletionDestructionStarted`/purge, not `GovernanceProtection:HoldPrepare` or its recovery. At the same time AD-22 states unconditionally that a hold winning before `DestructionStarted` cancels deletion preparations (`ARCHITECTURE-SPINE.md:345`).

**Divergence.** A Hold unit always accepts and cancels an armed-but-not-started deletion, following AD-22. A decision-aware fence unit may implement an eventual approved branch in which `DeletionArmed` causes a late hold to defer/reject, but the hold command carries no decision version or precondition by which to do so. The open Product choice is therefore partly pre-decided and partly impossible to apply.

**Disposition — DISCUSS, then bind without inventing the answer.** Product/Governance/Security must retain the choice. Either narrow the OD so the current “hold before DestructionStarted wins” rule is the decided invariant and only post-start recovery remains open, or add hold prepare/recovery to its affected evaluations and carry the approved precedence version and armed-state outcome through the fence.

## Medium

### M-1 — Gate-free export preparation recovery may choose cleanup without a recorded abort decision

AD-17/AD-23 say a dedicated recovery row cannot select a new outcome and gate-free cleanup begins from an immutable cleanup decision (`ARCHITECTURE-SPINE.md:313,361`). The matrix nevertheless says `ExportPrepareRecovery` may “resume only the recorded export or choose cleanup,” while the separate cleanup row requires `RecordedExportAbortIntentValid` (`launch-readiness-register.md:222-223`). One team can choose cleanup directly from local retry policy; another requires an EventStore abort decision through a still-reversible ordinary row. Require the latter decision/revision before entering the gate-free cleanup row, or define the exact deterministic failure fact that already constitutes that decision.

### M-2 — Decision-manifest signature validity does not identify the authority/role-assignment trust source

The decision record correctly prevents the Platform recorder from counting as an approver and requires a signed exact-version manifest, but the manifest-signing key is placed in the Platform-owned `EXT-SECRETS-1` contract without naming the issuer, role roster/assignment authority, revocation/freshness rule, or who may request a signature (`ARCHITECTURE-SPINE.md:313`; `external-dependency-register.md:179-191`; `launch-readiness-register.md:101,214-215`). Teams can validate the same signature while accepting different Product/Governance/Security/Architecture actors. Bind a versioned decision-authority issuer and role-evidence source, key-use ACL, freshness/revocation semantics, and tests proving the recorder/key custodian cannot mint approver evidence.

## Low

### L-1 — ARCH-A-9 retains the superseded `TenantId` name for the routed security partition

AD-2/AD-29/AD-30 consistently use `RoutingTenantId`, but ARCH-A-9 still calls the key `(TenantId, UtcDay)` (`ARCHITECTURE-SPINE.md:165,395,401,1020`). This is unlikely to defeat the stronger rules, but a fixture or schema generator can preserve the old name. Rename the assumption field only; do not change its open Security-owned volume-policy status.

## Reassessed Areas That Converge

- **Conjunctive safety and retry-budget disposition:** snapshot plus current policy outcomes are conjunctive; retry-time failure preserves `InvocationSettlement` and uses confirmed-no-use evidence. No v6 H-4 recurrence remains.
- **Governance recorded-branch recovery:** dedicated gate-free variants now keep mutable readiness from stranding a fixed cleanup/destruction branch and use direct availability/outcome lookup. C-1/H-6/M-1 concern separate hold applicability and branch-entry gaps, not the repaired post-decision recovery rule.
- **Trusted-envelope replay and security recorder:** registration precedes target idempotency, exact replay reuses first-seen time, capabilities/ACLs are non-recursive and least privilege, caller-independent routing uses `RoutingTenantId`, and durable spool recovery is explicit.
- **Bootstrap, containment, and exact status:** EventStore self-observation, platform/tenant scope, gate-free confirmed containment pull, exact replay, and PRD-preserving interaction/status vocabularies converge.
- **Three ledger lifetimes and open Product choice:** rate, open-concurrency, and monetary ownership are separated; `OD-RATE-CONCURRENCY-CONSUMPTION-1` correctly blocks before either ledger and is story/operation-only. H-4 is the remaining numeric deadline/reset relation, not a reinvention of the unresolved consumption choice.
- **Export abort cleanup:** partial-output inventory, key non-delivery, exact lookup, durable receipts, and fence retention now converge even while the two governance ODs remain open.
- **Proposal-index recovery:** interaction-source revision, idempotent outbox application, removal checkpoint, authoritative reconciliation, and repeat-to-none rule now define one crash-consistent path.

## Architecture Defects Versus Current Implementation Debt

The C/H/M/L items above are planning-contract defects or ambiguities. They must not be closed by pointing to today's incomplete implementation.

Current repository reality remains consistent with the spine's debt table: the root gitlink pins `Hexalith.Builds@a32cb422`, whose catalog has Dapr Client/ASP.NET/Workflow `1.18.5`; the dirty Builds checkout is `cf52f74` and contains `1.18.7`, which is not parent authority. Conversations, EventStore, FrontComposer, and Memories also have dirty checked-out HEADs while Parties and Tenants match their parent gitlinks. EventStore's parent-authoritative projects already expose Dapr Client/ASP.NET transitively; Agents still has no Dapr Workflow reference. The current source still carries legacy interaction/proposal vocabulary and lacks the three new ledgers, replay/decision/security aggregates, safety epoch/index, proposal outbox, and governance fence/export/deletion implementation. Those are existing story/tracking debt and add **zero** findings to this review.

## Required Closure Order

1. Close **C-1** by wiring legal holds to the approved export lifecycle/store without choosing the open later-hold policy.
2. Reconcile human identity and exact Parties story gates in **H-1/H-2**, especially before deletion evidence.
3. Materialize PRD deferred decisions and fix hold-decision applicability in **H-3/H-6** without inventing Product outcomes.
4. Bind the reset/deadline inequality and rescan enumeration/tenant handshake in **H-4/H-5**.
5. Tighten recovery branch entry and decision-authority trust in **M-1/M-2**, correct **L-1**, re-distill, lint, and rerun the complete reviewer gate.
