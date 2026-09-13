---
name: Hexalith Agents architecture good-spine rubric review v7
type: architecture-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
lens: rubric-walker
intent: validate-read-only
verdict: fail
counts:
  critical: 1
  high: 3
  medium: 4
  low: 1
---

# Good-Spine Rubric Walker — v7

## Verdict

**FAIL — 1 Critical, 3 High, 4 Medium, 1 Low.** The frozen revision closes all three Critical and all twelve High findings in the authoritative `VALIDATION-REPORT-2026-09-12.md`, and it closes the two v6 trusted-envelope findings. A fresh whole-artifact review nevertheless finds a Critical self-supersession weakness in the new runtime open-decision authority. It also finds three High cross-contract defects: the runtime recorder role contradicts the bound PRD, three recovery rows can choose between incompatible branches without a durable branch decision, and the summary mutation convention contradicts the two explicitly non-command security primitives.

The deterministic linter passes with zero findings. Mechanical validity does not close these semantic and authority-boundary findings.

## Frozen Input Snapshot And Method

The reviewer re-read the complete current spine, authoritative validation report, bound PRD, epics, both registers, memlog, implementation convention, repository instructions, and focused repository/package/gitlink reality. The Good-Spine checklist was walked independently rather than treating previous findings as the checklist. The review covered state and mutation ownership, authorization and separation of duties, tenant routing, decision bootstrap and supersession, readiness and operation gates, recovery decisions and acknowledgements, rate/open/budget ledgers, trusted replay and denial auditing, hold/export/deletion data-loss fences, public contracts, external seams, deployment/operations/provider dimensions, unresolved decisions, source traceability, and brownfield debt classification.

| Frozen input | SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `98513d95e17af7df25d35f4ae45b35243f60b4f1aaf4ed69df760d671cc8f4ff` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |
| bound `prd.md` | `94e2f751e01abd71278c6dcd1b7384206b55263a325905b4d3bc6016066c28fc` |
| `epics.md` | `365d446eea6512aaf71b472fb8342db0b5f2ab9037af67b001220d4ddd282284` |
| `external-dependency-register.md` | `3a88a794862ba180c9d920de0aa087ab28400a24f0e0ecc7a749add748c50e0c` |
| `launch-readiness-register.md` | `9180a7d6c84337073416309fd1527b2a5f43aa1b990789baf2e087e472ba03b6` |
| `.memlog.md` | `d5f0ffe6363f58aa75e7200ffc840b719711a537fed100b4b13eaf6ef07359f2` |
| `IMPLEMENTATION-CONVENTIONS.md` | `2ebba31863470f233aeeeccca45283542be2b6e84e3253323b97e7437bf1e7fd` |

Command:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: `ok: true`, `total_findings: 0`, no severity entries.

The source tree contains no shipped `ArchitectureDecisionRecord`, `TrustedEnvelopeReplay`, `ISecurityEventRecorder`, split rate/open ledgers, or protection-fence implementation. It still contains proposal-operation status shapes that the spine classifies as legacy delivery debt. The parent-authoritative root gitlinks remain Builds `a32cb422`, Conversations `73bcee6f`, EventStore `ce9e779a`, FrontComposer `053b2008`, Parties `fa423985`, and Tenants `2fac1839`; `global.json` remains `10.0.401` with `latestPatch`. The dirty Builds checkout does not supersede the parent gitlink. No build-success claim was made or needed for this read-only architecture gate.

## Critical

### C-R7-1 — An `ArchitectureDecisionRecord` successor can replace the governance that was supposed to authorize the successor

**Classification:** architecture contract defect; not implementation debt and not an unresolved Product outcome.

**Evidence**

- An open-decision record carries mutable `Owner`, `RequiredApprovers`, and `AffectedEvaluations`; approval is valid against the roles declared by that exact version (`launch-readiness-register.md:91-101`).
- Projection authority selects the greatest *published* `DecisionVersion` immediately and says it supersedes every lower version; there is no transitional rule retaining the previous version's blockers or authority while the successor is unapproved (`launch-readiness-register.md:103-107`).
- `ArchitectureDecision:PublishOrSupersede` checks recorder authority, a signed contract manifest, monotonic version, a closed affected-evaluation set, and expected revision, but does not require the current version's approvers, forbid a reduced or empty approver set, preserve the prior affected-evaluation set, or name a fixed meta-governance signer that is not supplied by the candidate successor (`launch-readiness-register.md:214`).
- `ArchitectureDecision:RecordApproval` then validates only “every required approver” of the selected contract. Empty or reduced requirements are not forbidden, and the authority that makes `SignedDecisionContractManifestValid` is not defined independently of the candidate contract (`launch-readiness-register.md:215`; `ARCHITECTURE-SPINE.md`, AD-17 “Open-decision authority”).
- Runtime and qualification intentionally do not parse the planning table, so they cannot recover the original affected evaluations or original owner/approver quorum from the spine when the new event omits them (`ARCHITECTURE-SPINE.md:313`; bound PRD FR-28 item 10).

**Divergence**

One compliant implementation can require a successor contract to be authorized under the previous version's governance and retain the previous blockers until then. Another can accept a correctly signed, monotonic v2 whose own contract reduces `RequiredApprovers` and removes `RQ-1` or a destructive operation from `AffectedEvaluations`; the projection immediately selects v2, stops applying v1 at the omitted evaluations, and can treat v2 as approved under its reduced or vacuous quorum. The current rules do not decide which interpretation is mandatory.

**Impact**

`OD-HOLD-DELETION-PRECEDENCE-1` and `OD-EXPORT-LIFECYCLE-1` protect key destruction, export preparation/commit, deletion completion, dependency activation, and `RQ-1`. Self-narrowing supersession can remove those fail-closed protections without authorization from the governance parties whose decision the record is meant to preserve. This can authorize irreversible destruction or release qualification under a weaker governance contract, so the defect is Critical even though no such implementation exists yet.

**Disposition — AUTOFIX ARCHITECTURE, while preserving the unresolved Product decisions.** Bind immutable genesis/current meta-governance separately from candidate-version data. A successor that changes owner, approvers, decision contract, or affected evaluations must be authorized under the previous effective version or a fixed externally governed policy. Until that authorization and new approval are durable, keep the previous version's blockers and at least the union of previous and proposed affected evaluations in force. Reject empty or reduced quorums and narrowed affected sets unless the governing predecessor explicitly approves that exact change. Add failure fixtures for an unapproved higher version, removed `RQ-1`/operation references, reduced or empty required roles, changed owner, lost acknowledgements, and concurrent publish/approve attempts. This fix defines governance mechanics only; it must not decide any `OD-*` outcome.

## High

### H-R7-1 — The runtime decision-recorder role contradicts bound PRD FR-33

**Classification:** cross-artifact architecture/Product-authority conflict; requires discussion, not an implementation workaround.

**Evidence**

- FR-33 says Product, Governance, and Security are planning parties rather than Agents roles, and that a decision assigned to them is recorded in the launch-readiness register by the **Release Operator**. It further says a Spine operation granted to a role absent from FR-33 is a pending PRD amendment, not an architecture detail (`prd.md:481-483`, `898`).
- AD-17 instead makes a **Platform Operator** the recorder for both publish/supersede and approval-recording commands (`ARCHITECTURE-SPINE.md:313`). AD-30 grants `ArchitectureDecision` to the `Platform` principal, and register rows 214-215 require `PlatformRecorderAuthorized`.
- Story 5.5 repeats the Platform-Operator publication authority, so epics follow the spine rather than resolving the conflict (`epics.md:1519-1522`).

**Divergence**

A PRD-led ingress permits a Release Operator and rejects a Platform Operator. A spine/register/epics-led ingress does the reverse. Both cannot conform to the bound authorization contract, and a builder cannot safely treat the difference as naming because AD-30 binds distinct principals, scopes, freshness sources, and policies.

**Impact**

The contradiction affects the only runtime path that publishes or records approval of open decisions controlling qualification, dependency activation, rate admission, export, and destructive governance. Either interpretation grants an unauthorized role or makes the intended decision path unusable.

**Disposition — DISCUSS PRODUCT AUTHORITY, then reconcile atomically.** Product must choose Release Operator, Platform Operator, or an explicitly separated publish/record protocol. Record that as a PRD amendment if it differs from current FR-33, then synchronize PRD, AD-17/AD-30, both matrix rows, the register prose, and Story 5.5. Preserve the rule that a recorder is not an approver. Do not infer the Product choice in architecture.

### H-R7-2 — Prepare-recovery rows can choose between incompatible branches without a durable branch decision

**Classification:** architecture recovery/data-integrity defect; no Product choice is needed to require a recorded decision, though policy owners still decide the policy outcome.

**Evidence**

- AD-17 says a dedicated recovery row “cannot select a new outcome” and that current-gate failure authorizes only a **decision-bound** unwind/release recovery (`ARCHITECTURE-SPINE.md:315`). AD-30 likewise limits `GovernanceProtection` Workflow to executing, unwinding, acknowledging, and finalizing an already-recorded human or governance decision at its exact revision.
- `GovernanceProtection:HoldPrepareRecovery` validates a recorded hold-prepare decision but then says the worker may acknowledge existing effects **or unwind** (`launch-readiness-register.md:218`). No resume-versus-unwind decision id/revision is required.
- `GovernanceProtection:ExportPrepareRecovery` says the worker may resume the recorded export **or choose cleanup**, while the separate abort-cleanup row requires `RecordedExportAbortIntentValid` (`launch-readiness-register.md:222-223`). The first row can therefore select cleanup without the abort decision the second row requires.
- `GovernanceProtection:DeletionPrepareRecovery` similarly permits acknowledging or unwinding existing preparations without a mutually exclusive recorded recovery disposition (`launch-readiness-register.md:226`).

**Divergence**

After a crash or gate change, one worker can resume/acknowledge the existing prepare while another can unwind or clean it up from the same durable pre-state. Each individual side effect may be idempotent, but there is no single expected-revision branch fact that makes the two choices mutually exclusive. Export is especially explicit: one row may “choose cleanup” before a `RecordedExportAbortIntentValid` event exists, while another requires that event.

**Impact**

Concurrent or restarted workers can race artifact writes against cleanup, pins against unpins, or deletion preparation against unwind. That can strand a protection fence, leave an unindexed export copy, remove a pin while the hold remains intended, or report a recovery state inconsistent with the owning aggregate. This is a High recovery and data-loss-boundary defect; irreversible deletion after `DestructionStarted` remains correctly one-way, keeping it below Critical.

**Disposition — AUTOFIX ARCHITECTURE.** Before any prepare-recovery side effect, append one mutually exclusive `Resume`/`Acknowledge` versus `Abort`/`Unwind` disposition to the owning aggregate or common fence at an expected revision. Split recovery rows by that recorded branch and require its exact event revision, frozen set, token, and policy/decision versions on every effect and acknowledgement. The export cleanup path must require the same durable abort intent as `ExportAbortCleanupRecovery`. Add concurrent resume/unwind, crash-after-decision, crash-after-first-effect, lost-ack, and stale-revision fixtures. Policy owners may still decide *when* unwind is selected; implementations may not select it from timing or current readiness.

### H-R7-3 — The summary mutation convention forbids the two pre-command security primitives that AD-3 and AD-30 require

**Classification:** internal architecture contract defect; not missing implementation.

**Evidence**

- AD-3 defines exactly two pre-command security primitives: `RegisterTrustedEnvelopeFirstSeen` and `RecordSecurityObservation`. Each acts before an ordinary Agents command exists and invokes a pure aggregate handler through a narrowly limited capability (`ARCHITECTURE-SPINE.md:173-177`).
- AD-30 explicitly says neither primitive carries an Agents command principal or recursive trusted envelope; the replay registrar conditionally appends `TrustedEnvelopeFirstSeen`, and the spool worker appends `SecurityEventRecorded` under a restricted EventStore credential.
- The Consistency Conventions table says without qualification: “Only EventStore commands mutate Agents state” (`ARCHITECTURE-SPINE.md:584`). `IMPLEMENTATION-CONVENTIONS.md` describes the ordinary orchestrator-to-single-command path but does not define these two exceptions.

**Divergence**

A shared conformance guard built from the summary convention can reject direct use of both capabilities or force each through an ordinary trusted command, recreating the recursion and denial-audit impossibility that v6 corrected. An AD-3-led implementation bypasses that guard for exactly two paths. Both follow normative-looking architecture text.

**Impact**

The contradiction sits at the first-seen replay and fail-closed security-audit boundary. Depending on which rule a separately built unit enforces, ingress either becomes unusable during envelope verification or bypasses a repository-wide mutation conformance test with an undocumented local exception.

**Disposition — AUTOFIX ARCHITECTURE.** Amend the Mutation convention to state that ordinary business/domain mutation uses trusted EventStore commands and that the only exceptions are the two named AD-3 pre-command primitives with their exact aggregate/event/stream ACLs. Mirror that exception in `IMPLEMENTATION-CONVENTIONS.md` if the convention is meant to drive generic tests. Add a guard fixture proving every other direct append/capability is rejected and both named primitives remain non-recursive.

## Medium

### M-R7-1 — Architecture-owned assumptions still lack literal retirement dates

**Classification:** unresolved governance input, safely fail-closed; not implementation debt.

**Evidence:** The PRD requires each Architecture-owned `ARCH-A` row to carry an owner-approved literal calendar date; a milestone or `TBD` remains an `RQ-1` blocker (`prd.md:890-896`). Open/co-owned `ARCH-A-1` through `ARCH-A-4`, `ARCH-A-6` through `ARCH-A-8`, `ARCH-A-11`, `ARCH-A-12`, and `ARCH-A-14` remain `Unscheduled` or milestone-only (`ARCHITECTURE-SPINE.md:1012-1025`).

**Impact:** `RQ-1` remains correctly blocked, so no unsafe default is created. The retirement process is nevertheless unbounded and does not meet its own governance contract.

**Disposition — DISCUSS.** Obtain co-owner-approved literal dates or retire/replace the assumptions. Do not invent dates and do not remove `UnretiredAssumption` blockers meanwhile.

### M-R7-2 — `PostingPending` timeout remains implementation-selected

**Classification:** explicit unresolved Architecture/Product choice; safely blocked if enforced.

**Evidence:** AD-5 requires a stored attempt deadline no shorter than the Conversations posting timeout but fixes no duration, allowed range, configuration owner, or version/snapshot rule. `ARCH-A-14` explicitly records the missing choice and has no literal retirement date (`ARCHITECTURE-SPINE.md`, AD-5 and `ARCH-A-14`; bound PRD FR-18).

**Impact:** A single stored attempt is deterministic, keeping this below High, but different builders can choose different deadlines and transition times for lookup, `LateConfirmed`, and `PostingFailed`.

**Disposition — DISCUSS OR HARD-BLOCK THE CONSUMING STORY.** Architecture proposes the duration/configuration authority, range, snapshot rule, and `EXT-CONV-AI-1` compatibility condition; Product confirms any user-visible timing. No implementation-local default should retire `ARCH-A-14`.

### M-R7-3 — `ArchitectureDecisionRecord` is absent from two build-substrate summaries and the debt ledger

**Classification:** architecture-document completeness defect plus accurately unimplemented delivery work.

**Evidence:** AD-2, Naming, and the class diagram define `ArchitectureDecisionRecord`, and Story 5.5 assigns aggregate/projection tests (`ARCHITECTURE-SPINE.md:165`, `582`, `889-926`; `epics.md:1519-1549`). The Structural Seed's closed aggregate-folder list omits `ArchitectureDecisionRecord/` (`ARCHITECTURE-SPINE.md:652-675`). The Capability-to-Architecture map's launch-readiness row names `LaunchReadinessGate` and the matrix but not the decision aggregate, and the Architecture Contract Versus Delivery Debt table does not name the absent decision aggregate/projection or security-audit spool (`ARCHITECTURE-SPINE.md:930-981`). Focused source search confirms those components are not shipped.

**Impact:** The core AD is clear enough to prevent a direct semantic High, and epics retain an owner. A seed-driven builder or brownfield reconciler can nevertheless omit/place the aggregate differently or misreport it as already present.

**Disposition — AUTOFIX DOCUMENTATION.** Add the aggregate folder to Structural Seed, name decision publication/projection in the capability map, and add the missing decision record/projection and audit-spool work to the delivery-debt table with their existing story/dependency owners. Do not recast absent code as an architecture defect.

### M-R7-4 — `SecurityObservation:SpoolAndRecord` does not use the matrix's closed `EvaluationScope` value

**Classification:** architecture/register schema defect; no implementation exists yet.

**Evidence:** AD-17 says every matrix row declares `EvaluationScope` exactly `Platform` or `Tenant`, and a missing or ambiguous target scope blocks (`ARCHITECTURE-SPINE.md:307`). Register row 213 instead declares `Platform service; route computed ...`, conflating the evaluation scope with a non-human execution-kind/routing description. AD-30 carefully distinguishes a service capability with no `Platform` human principal from the `Platform` evaluation/routing scope.

**Impact:** A strict schema parser rejects the security-audit row; a permissive parser normalizes it to Platform. The row is non-public and gate-free, so this remains Medium, but qualification and generated contracts can disagree.

**Disposition — AUTOFIX REGISTER.** Use the exact `Platform` scope token and place “service capability” and computed routing in a separate typed execution-kind/direct-precondition field or explanatory text. Apply the same token-versus-qualifier normalization consistently to other descriptively suffixed scope cells and add schema fixtures.

## Low

### L-R7-1 — External-prerequisite decision traceability omits new host/secrets consumers

**Classification:** documentation traceability only; the authoritative register remains fail-closed.

**Evidence:** The spine's External V1 Prerequisites table maps `EXT-HOST-1` only to AD-16/AD-23 and `EXT-SECRETS-1` only to AD-9/14/16/21/22 (`ARCHITECTURE-SPINE.md:987-1000`). The current contract also makes `EXT-HOST-1` supply the replicated security-audit spool under AD-3/AD-30, and makes `EXT-SECRETS-1` verify decision contract/authority manifests under AD-17 plus trusted-envelope/digest keys under AD-29/AD-30.

**Impact:** The dependency records and consuming stories remain authoritative, so no seam is made available incorrectly. The curated governing-decision map is simply stale and can slow later impact analysis.

**Disposition — AUTOFIX DOCUMENTATION.** Add AD-3/AD-30 to `EXT-HOST-1` and AD-17/AD-29/AD-30 to `EXT-SECRETS-1` on the next architecture edit.

## Requested Cross-Contract Audit

| Boundary | Result | Evidence / disposition |
| --- | --- | --- |
| Readiness self-bootstrap/repair | **Pass** | Matrix v4 gives `LR-EVENTSTORE` a target-limited gate-free first/repair observation. Other observations still require the current EventStore record, and broken target state does not block correction. |
| Kill-switch pull/release | **Pass** | Pull-containment remains gate-free and evidence/revision bound; release remains separately reviewed and normally gated. No fail-open release path was found. |
| Public statuses | **Pass as architecture; implementation debt remains** | AD-15/PRD vocabulary is coherent. Remaining proposal-operation shapes in source are explicitly debt rather than claimed current parity. |
| Open-interaction lease | **Pass** | Interaction-owned prepare/commit-or-abort/release decisions and acknowledgements include pre-acceptance terminal outcomes and make missing decision evidence fail closed. |
| Rate admission | **Pass with visible Product blocker** | `OD-RATE-CONCURRENCY-CONSUMPTION-1` blocks before either ledger, so the unresolved consumption choice is not invented. Once approved, the two-scope prepare/decision/ack protocol is atomic at the interaction owner. |
| Budget reservation/recovery | **Pass** | Invocation settlement and mutually exclusive budget disposition facts prevent release from absence; recovery follows the recorded revision. |
| Trusted replay owner/key | **Pass after v6 correction** | Nonce registration precedes target idempotency; replay uses reserved `system`, issuer/nonce identity, exact authenticated-field comparison, stored first-seen time, and lost-ack recovery. |
| Security denial audit | **Pass in AD-3/AD-30, fail at summary consistency** | The limited spool/recorder/worker protocol closes recursion and outage loss, but H-R7-3 must reconcile the Mutation convention and M-R7-4 the matrix scope token. |
| Open-decision runtime owner | **Fail** | `ArchitectureDecisionRecord` now exists in the architecture and register, but C-R7-1 permits self-weakening supersession and H-R7-1 conflicts with FR-33 recorder authority. |
| Hold/export/deletion fence | **Fail narrowly** | Initial intent/fence/partial-inventory/destruction boundaries remain strong, but H-R7-2 leaves reversible prepare recovery able to select resume versus unwind/cleanup without a recorded branch fact. |
| Unresolved Product/Governance/Security choices | **Pass as surfaced blockers** | Hold/deletion precedence, export lifecycle, rate/concurrency consumption, Dapr security, and sprint-history decisions remain open with named owners and affected evaluations. This review does not choose them. |
| External dependencies | **Pass with Low traceability debt** | All twelve records remain `Uncommitted`, their consumers fail closed, and no dirty submodule state is treated as commitment. L-R7-1 only corrects the summary map. |
| Operational/environment/provider dimensions | **Pass** | Host composition, Dapr ownership, readiness, capacity, telemetry, restore, protection, provider/tokenizer/safety/secrets seams, and current package/gitlink realities are addressed or explicitly blocked. |

## Authoritative 2026-09-12 Finding Closure Audit

| Authoritative finding | v7 disposition |
| --- | --- |
| C-1 weaker safety-policy retry | **Closed.** AD-20 evaluates current and snapshotted policy conjunctively and carries immutable version/evaluation evidence. |
| C-2 hold/deletion exclusion | **Closed at initial/irreversible architecture boundary.** `ProtectionFence` serializes hold/export/deletion intent and open policy blocks destruction. H-R7-2 is a new reversible-recovery branch defect, not recurrence of the missing common fence. |
| C-3 bootstrap/repair deadlock | **Closed.** Matrix v4 has typed target-aware bootstrap/repair variants, explicit scopes, direct prerequisites, and unrelated-gate fail-closed fixtures. |
| H-1 Eligible-Approver recheck | **Closed.** Single-flight cadence, evidence freshness, two-pass empty handling, marker behavior, and unavailable-evidence recovery are bound. |
| H-2 safety-rescan ownership | **Closed.** Durable epoch/index state, bounded coordinator lease/cursor, call behavior, and recovery are defined. |
| H-3 human-only approvers | **Closed.** Configuration and decision-time Parties human/liveness evidence fail closed and use stable authenticated-human identity. |
| H-4 conflated ledger lifetimes | **Closed.** Rate, open concurrency, and monthly budget have separate aggregates, partitions, decisions, and acknowledgements. |
| H-5 human separation identity | **Closed.** AD-30 carries stable `AuthenticatedHumanActorId` independently of Party/role and verifies the historical Party binding. |
| H-6 proposal-index crash consistency | **Closed.** Interaction events/source high-water drive idempotent indexing and repair before removal completion. |
| H-7 indivisible Conversations dependency | **Closed.** Optional retraction is split into `EXT-CONV-RETRACTION-1`; six core seams remain in `EXT-CONV-AI-1`. |
| H-8 envelope cryptography/replay | **Closed.** Canonical fields, HMAC/key lifecycle, lifetime/skew, rotation/revocation, logical-versus-delivery identity, global issuer/nonce replay owner, and first-seen ordering are explicit. |
| H-9 export artifact lifecycle | **Closed at architecture scope.** Immutable encrypted store/key/index/manifest/purge invariants exist; unresolved lifetime/later-hold/restore/provider policy remains a visible `OD-*` blocker. |
| H-10 Dapr current reality | **Closed.** Parent `1.18.5` transitive exposure, dirty-checkout `1.18.7`, future Workflow adoption, and security decision are distinguished. |
| H-11 public parity assertion | **Closed.** Required completion vocabulary is separated from present repository debt. |
| H-12 sprint/dependency history | **Closed as visible Delivery decision.** `OD-SPRINT-5.1-5.2-1` blocks dependent authorization without rewriting history or dependency status. |

## Post-v6 Correction Audit

| v6 finding | v7 disposition |
| --- | --- |
| H-R6-1 nonce registration after target idempotency | **Closed.** Matrix-v4 ordering now explicitly registers/verifies the delivery nonce before target AD-29 idempotency, including exact-target-idempotency replay fixtures. |
| H-R6-2 recursive/unavailable denial auditing | **Closed in the defining ADs.** AD-3/AD-30 define a non-public `ISecurityEventRecorder`, durable replicated spool, restricted worker, exact-id acknowledgement recovery, outage fail-closed behavior, and gate recovery. H-R7-3 is the remaining contradiction in the separate summary convention. |
| M-R6-3/M-R6-4 epic evidence naming | **Closed.** Story 5.5/5.7 evidence names matrix-v4 tests, and Story 6.1 locally includes the Dapr decision blocker. |
| L-R6-1 review-source traceability | **Closed.** The spine source list now includes the material v4/v5/v6 review rounds. |

## Good-Spine Checklist

| Criterion | Result | Reason |
| --- | --- | --- |
| Fixes the real divergence points for the level below and misses none | **Fail** | C-R7-1 permits two governance/supersession implementations, H-R7-2 two recovery branches, and H-R7-3 two mutation-boundary interpretations. |
| Every AD Rule is enforceable and prevents its stated divergence | **Fail** | AD-17's selected-version rule does not protect its own supersession authority; recovery wording contradicts “cannot select a new outcome”; the summary mutation rule conflicts with AD-3. |
| Deferred/open items are safe to defer | **Pass with Medium governance debt** | Each `OD-*` and unretired assumption fails closed at its named operation/authorization/qualification boundary. No Product outcome or date was inferred. |
| Named technology is verified-current | **Pass** | The SDK and Dapr/gitlink distinctions match the frozen repository authority. The spine makes no unsupported build-success or dirty-checkout claim. |
| Brownfield ratification | **Pass with documented delivery debt** | Missing runtime aggregates, spool, ledgers, fence, workflow, and contract parity are treated as assigned work. The architecture does not claim they are shipped. |
| Bound PRD capability and authority coverage | **Fail** | Capabilities are covered, but H-R7-1 directly contradicts FR-33's recorder role. |
| Inherited parent constraints | **N/A / no weakening found** | No parent architecture spine or inherited AD set is declared. Repository/project-context constraints and root gitlinks are reflected. |
| State/mutation/recovery ownership | **Fail** | Aggregate ownership is broad and explicit, but H-R7-2 lacks a mutually exclusive recovery branch decision and H-R7-3 contradicts the command boundary. |
| Security, tenant isolation, audit, data-loss boundaries | **Fail** | Trusted replay/audit details are strong, but C-R7-1 can weaken destructive-operation decision blockers and H-R7-2 can race recovery effects. |
| API/integration/operations/environment/provider dimensions | **Pass** | Public route/contract, adapter seams, host/deployment, readiness, telemetry, capacity, restore, provider, and security dependency dimensions are decided or blocked. |
| Structural handoff and source traceability | **Pass with M-R7-3/L-R7-1** | Diagrams/maps/seed/debt/registers are present; two summary inventories need synchronization with the newly introduced decision/spool contracts. |
| Mechanical validity | **Pass** | Deterministic lint returned zero findings. |

## Architecture Defects Versus Implementation Debt

The nine counted findings are document/governance defects or explicit unresolved inputs. They do not count absent implementation a second time. Specifically:

| Current repository gap | Classification / existing disposition |
| --- | --- |
| No runtime `ArchitectureDecisionRecord`, decision projection, trusted replay aggregate, security recorder/spool, split rate/open ledgers, safety epoch/index, common protection fence, protected export store, or Dapr Workflow owner | **Implementation debt.** Assigned across Stories 5.4-5.7, 6.1, 6.3-6.4, and 8.1-8.3 plus external records. M-R7-3 asks only that the document's seed/map/debt summaries name two newly added items. |
| Current proposal-operation/public status duplication | **Implementation debt.** Required completion parity and owning stories are explicit; no new finding. |
| All external dependency records `Uncommitted` | **External delivery blocker, not architecture ambiguity.** Runtime/test/qualification use fails closed with `DependencyNotAvailable`. |
| Parent Dapr family at `1.18.5`, dirty Builds checkout at `1.18.7`, Workflow absent | **Known dependency/security decision and delivery work.** `OD-DAPR-SECURITY-1` remains open and no approval is inferred. |
| Sprint 5.1/5.2 status/evidence mismatch | **Delivery-governance decision.** `OD-SPRINT-5.1-5.2-1` stays open; this review does not rewrite history. |
| C-R7-1 and H-R7-1 | **Architecture/Product governance defects.** Implementing the current record cannot safely repair its self-supersession or role authority locally. |
| H-R7-2 and H-R7-3 | **Architecture contract defects.** They require one recovery/mutation rule before separate units implement them. |
| M-R7-1 and M-R7-2 | **Unresolved architecture/product inputs.** They remain blockers and must not become implementation defaults. |

## Recommended Resolution Order

1. Close **C-R7-1** first by defining predecessor/meta-governed supersession and blocker carry-forward, without deciding any open Product outcome.
2. Resolve **H-R7-1** with Product and synchronize the recorder role across PRD, spine, register, and epics.
3. Close **H-R7-2** with durable mutually exclusive recovery dispositions and exact-revision recovery rows.
4. Close **H-R7-3** by naming the two AD-3 exceptions in the summary and implementation convention.
5. Reconcile the four Medium and one Low summary/governance items without inventing dates, timeouts, or policy outcomes.
6. Re-distill, rerun deterministic lint, freeze/hash the new inputs, and rerun the complete reviewer gate rather than a closure-only pass.

## Validation Decision

The spine is not yet safe as the authoritative implementation handoff. Its original authoritative Critical/High backlog is closed, but the new runtime decision mechanism can weaken its own governance and the three High cross-contract boundaries allow independently built units to diverge. Preserve all AD ids and open `OD-*` outcomes, update the architecture/PRD/register contracts in the order above, re-distill, and rerun the complete gate.
