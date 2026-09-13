---
name: Hexalith Agents architecture good-spine rubric review v8
type: architecture-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
lens: rubric-walker
intent: validate-read-only
verdict: fail
counts:
  critical: 1
  high: 5
  medium: 3
  low: 1
---

# Good-Spine Rubric Walker — v8

## Verdict

**FAIL — 1 Critical, 5 High, 3 Medium, 1 Low.** The frozen revision closes all three Critical and all twelve High findings in the authoritative `VALIDATION-REPORT-2026-09-12.md`, and the concrete v7 fixes for decision self-supersession, recorder-role selection, branch-event serialization, mutation exceptions, hold/export coupling, deferred PRD rows, rate timeout bounds, safety cohorts, and delivery-debt wording are present. A fresh whole-artifact walk nevertheless finds one Critical legal-hold recovery contradiction and five High downstream-divergence seams in the newly added decision, recorder, human-evidence, and export-store contracts.

The deterministic linter passes with zero findings. Mechanical validity does not close the authority, recovery, and cross-team contract findings below.

## Frozen Input Snapshot And Method

The reviewer re-read the complete current spine, authoritative validation report, bound PRD, epics, both registers, current memlog, implementation convention, repository instructions, and focused repository/package/gitlink reality. The Good-Spine checklist was walked independently, not as a prior-finding closure checklist. The review covered aggregate/state/mutation ownership, decision bootstrap and supersession, authorization and separation of duties, tenant routing, readiness and operation gates, rate/open/budget races, safety activation, trusted replay, legal-hold/export/deletion recovery, public contracts, external seams, operations/environment/provider dimensions, unresolved decisions, source traceability, and architecture-versus-delivery debt.

| Frozen input | SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `8ce81ba273d0cd9185f74886513eeccf5e5e6a732313eec3c2aeaa0c311569f4` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |
| `IMPLEMENTATION-CONVENTIONS.md` | `31190a6a351a0710878d8f00d2a1a4c79e15e37a644dbfb6526b2d0d462cba73` |
| bound `prd.md` | `3028350705ee22365f67669732b961ee8a7671a36851deb11a6026477d3b1279` |
| `epics.md` | `40dc6bee2bfeeb4fa9f594bd73a09f28dbd779fccd09b74682b10c0231bc6315` |
| `external-dependency-register.md` | `46009827b0010a40c5432c2b5f49dc9a5f78580e82b60b9760e2a4ef6547eba8` |
| `launch-readiness-register.md` | `5603d65672dfa03da67b1437800bbb912f962335756c576655d2105dfc2625c2` |
| `.memlog.md` | `69ee2dd3d6440f6eeca5fb26c4d7bd566a0effec26e3b11a766ed8e906a0f8f8` |
| repository instruction source | `bfbe399b567d852a74c7ee8c9217217a3ea5eb80ca591b5b8ac01acfbe838966` |
| reviewer-gate rubric | `d32e32a3c1d59b5612b947004f3f6fef1117a13ce9f1ffa428d616e0b5d4db69` |
| root `global.json` | `fc4602f9d88c9440f70f72732343a88c5e4190223fb9b77f9b8ac8eb1c4c8c2d` |

Command:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: `ok: true`, `total_findings: 0`, no severity entries.

The parent-authoritative root gitlinks remain Builds `a32cb422`, Conversations `73bcee6f`, EventStore `ce9e779a`, FrontComposer `053b2008`, Parties `fa423985`, and Tenants `2fac1839`; the initialized Builds/Conversations/EventStore/FrontComposer checkouts are dirty and do not supersede those gitlinks. `global.json` remains `10.0.401` with `latestPatch`. Focused source search finds none of `ArchitectureDecisionRecord`, `SafetyVerdictEpoch`, `TrustedEnvelopeReplay`, `ProtectionFence`, `RateLimitLedger`, `OpenInteractionLedger`, or `BudgetLedger`; current proposal-operation status shapes remain the legacy delivery debt the spine describes. No build-success claim was made or needed for this read-only architecture gate.

## Critical

### C-R8-1 — The generic prepare-recovery source can abort a legal hold from a permanent failure even though AD-22 says failure alone selects no branch

**Classification:** architecture recovery and preservation defect; not implementation debt. The policy for cancelling a legal hold is also a governance choice that the architecture must not infer from a dependency failure.

**Evidence**

- AD-22 says a recorded `Abort` disposition authorizes hold unwind and a `Resume` disposition authorizes acknowledgement, then states explicitly: **“failure alone selects neither branch”** (`ARCHITECTURE-SPINE.md:350`).
- AD-17 instead requires the disposition to be produced “from an already-recorded authorized cancellation **or closed typed permanent-failure fact**,” without identifying an authority or branch-specific mapping that chooses `Resume` versus `Abort` (`ARCHITECTURE-SPINE.md:318`).
- All three matrix branch-decision rows accept the same union `RecordedAuthorizedCancellationOrClosedPermanentFailureFactValid` and a caller-supplied mutually exclusive branch; none requires a branch-specific authorization. The hold `Abort` row then permits exact artifact/DEK unpin and unwind (`launch-readiness-register.md:222-224`; export/deletion repeat the same source at `:228` and `:233`).
- The Story 8.1 recovery criterion likewise starts from “an authorized cancellation or closed permanent-failure fact” and permits either disposition, while no story criterion defines who or what authorizes `Resume` after an ordinary crash or prevents `Abort` from a hold-pin failure (`epics.md:2786-2789`). Story 8.2 makes the same generic source authorize either continuation or cleanup (`epics.md:2865-2868`).

**Divergence**

One implementation follows AD-22 and keeps a failed hold restrictive pending until a legally authorized cancellation or a later successful resume. Another follows AD-17/the matrix and deterministically maps a closed pin/store failure to `Abort`, unpins members, and releases the fence without a hold-release decision. A third cannot ever record `Resume` after an ordinary crash because neither a cancellation nor permanent failure exists. All three are literal readings of current normative text.

**Impact**

The second implementation can remove the only protection on interaction keys and committed export artifacts while the legal-hold intent still exists; later expiry, purge, or deletion can irreversibly lose evidence. The first and third can strand recoverable preparation. Because the contradiction crosses the legal-hold preservation boundary and can authorize unpin without legal cancellation, it is Critical.

**Disposition — AUTOFIX ARCHITECTURE without choosing a Product/Governance outcome.** Define branch-specific source facts. `Resume` should bind the original still-valid prepare intent, frozen set/token/version, absence of authorized cancellation, and exact owner revision; ordinary crash/lost acknowledgement must not need a fabricated failure. Export/deletion `Abort` may use an explicitly closed policy-mapped permanent-failure class or authorized cancellation. Legal-hold `Abort` must require the recorded legal/governance cancellation authority the bound product contract selects; a pin/store failure alone leaves the hold restrictive pending and retry/escalation visible. Split the three `...BranchDecision` preconditions and story criteria accordingly, preserve expected-revision mutual exclusion, and add failure-to-abort, crash-to-resume, and hold-unpin denial fixtures.

## High

### H-R8-1 — Missing decision records cannot be detected from the declared runtime model, and `DecisionId` has no AD-29 identity rule

**Classification:** architecture/runtime decision-catalog and identity defect; not the open Product decisions themselves.

**Evidence**

- The register says a missing `ArchitectureDecisionRecord` emits `OpenDecision`, while runtime never parses the register or PRD (`launch-readiness-register.md:78,91,104`).
- The only durable runtime owner is one event stream **per existing `DecisionId`**; the projection selects effective/pending versions from those streams. No signed/versioned catalog aggregate or deployed required-decision inventory tells the projection which streams must exist, so an entirely absent record produces no event from which the projection can discover its absence (`ARCHITECTURE-SPINE.md:168,316`; `launch-readiness-register.md:104,110`).
- The static table materializes eight current IDs, including OQ-18/OQ-23/OQ-31, but is expressly non-runtime (`launch-readiness-register.md:93-104`). Story 5.5 tests that planning documents cannot supply runtime decisions but names no separate catalog/manifest input that supplies the expected key set (`epics.md:1519-1534,1554-1558`).
- The consistency convention says every Agents identity is derived under AD-29 and Agents owns `DecisionId` (`ARCHITECTURE-SPINE.md:594`). AD-29's exhaustive derivation list has no `DecisionId` rule (`ARCHITECTURE-SPINE.md:400-404`), leaving literal stable `OD-*` ids versus a hashed id as incompatible implementations.

**Divergence**

A build can compile the current table into an unstated constant list and use literal `OD-*` stream keys. Another can discover only streams/events and therefore never emit a blocker for a completely absent OQ-31 record. A third can hash the decision id to satisfy the AD-29 convention. Separately built publishers, projections, operation evaluators, and qualification runners can then address different streams or disagree on whether a missing decision exists.

**Impact**

The gap can make a binding deferred decision disappear from `RQ-1` or an affected operation simply because its first record was never published. It can also make valid approvals invisible across independently built units. Direct destructive rows name some decisions explicitly and remain fail-closed, keeping this below Critical, but the general release/enablement path is not mechanically closed.

**Disposition — AUTOFIX ARCHITECTURE.** Add an independently authorized, immutable, versioned required-decision catalog/manifest (or an equivalent aggregate) that runtime consumes without parsing planning documents. It must enumerate stable `DecisionId`, minimum/current contract version, and baseline affected evaluations, and make a missing stream emit `OpenDecision`. Define `DecisionId` in AD-29 explicitly—preferably the validated stable register id if that is the intended public key, or a single derivation plus a durable mapping—and bind publication/projection/catalog changes atomically. Preserve every existing decision id and do not choose any open outcome.

### H-R8-2 — A tenant-scoped Party-bearing Release Operator has no defined authority scope for a platform-global decision stream

**Classification:** bound PRD/architecture authorization conflict requiring Product and identity-owner discussion.

**Evidence**

- PRD FR-33 makes the planning-decision operation Platform-scoped but also says every role except Platform Operator is tenant-scoped (`prd.md:500,523-526`). It does not identify which tenant's Release Operator assignment authorizes a global decision affecting all tenants.
- AD-30 implements the operation as the single reserved-`system` exception for a `User` holding Release Operator. A `User` necessarily carries a concrete `TenantId`, `PartyId`, and tenant-projection role assignment, yet the command targets the global `system` decision stream; no qualification-tenant, release-cohort, designated governance tenant, or cross-tenant authorization rule is fixed (`ARCHITECTURE-SPINE.md:408-410`).
- Every human command with a `PartyId` requires live/historical `EXT-PARTIES-1` binding under AD-30, but Story 5.5 and the external register do not list `EXT-PARTIES-1` as a direct decision-publication dependency/consumer (`epics.md:1494-1498,1550-1559`; `external-dependency-register.md:105-117`).

**Divergence**

One ingress can allow a Release Operator assigned in any tenant to publish a platform-global contract. Another can allow only the tenant currently being qualified. A third can treat Release Operator as a platform operator-style principal despite the PRD's tenant-scoped rule. The permitted actor population and the dependency set differ, and one tenant's operator can affect every tenant under the first reading.

**Impact**

This is the sole recorder path for decisions controlling release qualification, Dapr adoption, Automatic mode, export-store activation, and destructive-governance policy. An undefined scope either grants excessive global write authority or makes legitimate publication impossible.

**Disposition — DISCUSS PRODUCT AUTHORITY, then reconcile atomically.** Product must identify the authoritative Release Operator assignment for this Platform-scoped operation or amend the role to a separately governed platform-scoped recorder. Then synchronize PRD FR-33, AD-30 principal/target rules, matrix preconditions, Story 5.5, and the Parties/Tenants dependency evidence. Do not select a tenant or broaden the role in architecture by inference.

### H-R8-3 — The decision matrix permits the recorder to count as an approver despite the PRD and story's unconditional prohibition

**Classification:** internal authority/separation-of-duty contradiction; not implementation debt.

**Evidence**

- PRD FR-33 says the Release Operator recorder “is never an approver” (`prd.md:500`). Story 5.5 repeats that the recorder may never approve or mint authority evidence (`epics.md:1521-1523`).
- AD-17 says the recorder cannot substitute for an owner or required approver (`ARCHITECTURE-SPINE.md:316`).
- Matrix row `ArchitectureDecision:RecordApproval` weakens that invariant: the Release Operator recorder “cannot count as an owner/approver **unless** the independently signed contract requires and authenticates that separate role” (`launch-readiness-register.md:218`). That permits the same human actor to record and satisfy an approval under a second authority role.

**Divergence**

A PRD/story-led implementation rejects approval evidence whose stable actor id equals the recorder. A register-led implementation accepts a dual-hatted recorder when the external contract authenticates their Product/Governance/Security/maintainer role. Both validate the signature and role correctly but enforce opposite separation rules.

**Impact**

The exception lets the only runtime recorder help satisfy or complete the quorum that controls its own system-wide publication. That weakens the independent-authority boundary introduced to close v7 self-supersession.

**Disposition — AUTOFIX unless Product explicitly amends FR-33.** Remove the matrix exception and require stable-actor inequality between the recorder and every owner/approval evidence item, independent of dual roles. If dual-hatting is actually intended, surface it as a Product/Governance/Security decision and amend the PRD/story/AD together; do not let the matrix choose it silently.

### H-R8-4 — AD-22 still requires Party-binding evidence for Party-free Tenant Administrators

**Classification:** cross-AD human-evidence union defect; not an unresolved Product outcome.

**Evidence**

- AD-30 and PRD FR-33 deliberately define a tagged union: Party-bearing `User` evidence comes from `EXT-PARTIES-1`; Party-free `Administrator` evidence is the current tenant-role projection plus durable actor/role-evidence version; Party-free `Platform` evidence is the global-administrator authority plus durable version/binding. All branches compare stable `AuthenticatedHumanActorId` (`ARCHITECTURE-SPINE.md:414`; `prd.md:526`).
- AD-22's computed subject set includes the Tenant Agent Administrator actor ids whose configuration versions were in force, then requires those historical identities to be captured with “their Party-to-actor binding versions” and blocks on a missing historical binding (`ARCHITECTURE-SPINE.md:350`). A Party-free `Administrator` has no `PartyId` or `HumanActorBindingVersion` by design.
- Epics 8.1-8.3 use the corrected union and accept Administrator role evidence without a Party (`epics.md:2782-2784,2841-2844,2904-2907`), so the owning AD and consuming stories do not enforce the same subject-set contract.

**Divergence**

An AD-22-led subject-set builder rejects a historical Tenant Agent Administrator action unless it has a Party binding. An AD-30/story-led builder accepts the tagged Administrator evidence and compares its stable actor id. A third may invent a Party to satisfy AD-22, which AD-30 forbids for Party-free branches.

**Impact**

Compliance inspection, export approval, hold release, and deletion approval can disagree on the computed subject set and second-party eligibility. That can either block legitimate governance indefinitely or omit a relevant historical actor from anti-collusion checks.

**Disposition — AUTOFIX ARCHITECTURE.** Rewrite AD-22's subject-set evidence as the same closed tagged union as AD-30, persisted at action/resolution time: Party user = Party/classification/liveness/binding version; Administrator = tenant role-source/revision plus stable actor evidence; Platform = global authority-source/revision plus stable actor evidence. Compare only the stable actor id across branches, reject historically incomparable evidence, and never require or invent a Party for a Party-free principal. Add mixed-principal and role-change fixtures.

### H-R8-5 — The export-store dependency does not contract the artifact pin/unpin and expiry-refusal operations that legal hold now requires

**Classification:** external seam contract defect; the actual later-hold lifetime outcome remains correctly unresolved.

**Evidence**

- AD-22 now requires an overlapping hold to pin every committed export artifact/key/index member, receive exact artifact pin acknowledgements before `Active`, receive exact unpin acknowledgements on release, and forbid expiry/purge/unpin reporting while the lifecycle decision or exact target is unavailable (`ARCHITECTURE-SPINE.md:354`).
- Matrix hold recovery requires existing artifact pin/unpin outcome lookup against the exact store (`launch-readiness-register.md:221-226`), and Story 8.1 explicitly consumes live artifact pin/unpin (`epics.md:2761,2768-2779`).
- `EXT-EXPORT-STORE-1` contracts immutable writes, download, expiry/purge, physical purge receipts, export/deletion fencing, and generic “later-hold ... behavior,” but it does not name idempotent artifact/key/index `Pin`/`Unpin`, token/version inputs, exact acknowledgements/outcome lookup, or an expiry/purge refusal while a hold is restrictive pending (`external-dependency-register.md:193-205`).

**Divergence**

A store team can satisfy the register with a lifecycle-policy implementation that knows later-hold behavior but exposes no explicit pin/unpin acknowledgement API. The hold team, following AD-22 and Story 8.1, requires those operations before it can report `Active` or release. Another store may implement object tags, another may extend expiry, and another may use key pins; the current compatibility command cannot prove the exact contract the hold workflow consumes.

**Impact**

The module/store boundary can report `Available` without the only operation that prevents an independently scheduled expiry from destroying a held committed artifact. This is a High preservation seam; the open lifecycle decision still blocks current execution and keeps it below Critical.

**Disposition — AUTOFIX THE DEPENDENCY CONTRACT without selecting the open lifecycle outcome.** Add the exact idempotent pin/unpin/outcome-lookup contract, artifact/key/index identity, hold/fence token, lifecycle decision version, expiry/purge refusal semantics while restrictive pending, lost-ack recovery, and compatibility tests to `EXT-EXPORT-STORE-1`. If Product chooses a different preservation primitive, the approved `OD-EXPORT-LIFECYCLE-1` contract must name that single primitive and AD-22/matrix/Story 8.1 must use it consistently.

## Medium

### M-R8-1 — Architecture-owned assumptions still lack literal retirement dates

**Classification:** unresolved governance input, safely fail-closed; not implementation debt.

**Evidence:** The PRD requires every Architecture-owned `ARCH-A` row to carry a co-owner-approved literal calendar date; milestones or `TBD` leave `RQ-1` blocked (`prd.md:894-898`). `ARCH-A-1` through `ARCH-A-4`, `ARCH-A-6` through `ARCH-A-8`, `ARCH-A-11`, `ARCH-A-12`, and `ARCH-A-14` remain unscheduled or milestone-only (`ARCHITECTURE-SPINE.md:1026-1042`).

**Impact:** Qualification remains safely blocked, so this is not fail-open. The governance process remains unbounded and violates its own scheduling contract.

**Disposition — DISCUSS.** Obtain co-owner-approved literal dates or retire/replace the assumptions. Do not invent dates and do not remove their blockers.

### M-R8-2 — `PostingPending` timeout remains implementation-selected

**Classification:** explicit unresolved Architecture/Product choice; safely blocked if the assumption gate is enforced.

**Evidence:** AD-5 requires a stored attempt deadline no shorter than the Conversations posting timeout but fixes no duration, range, configuration owner, or version/snapshot rule. `ARCH-A-14` records the missing choice and has no literal retirement date (`ARCHITECTURE-SPINE.md:195,1041`; bound PRD FR-18).

**Impact:** Each individual stored deadline is deterministic, keeping this below High, but separately built clients/workflows can choose different transition times for lookup, `LateConfirmed`, and `PostingFailed`.

**Disposition — DISCUSS OR HARD-BLOCK THE CONSUMING STORY.** Architecture should propose the duration/configuration authority, allowed range, snapshot rule, and `EXT-CONV-AI-1` compatibility condition; Product confirms the user-visible timing. No implementation-local default should retire `ARCH-A-14`.

### M-R8-3 — Two cited 2026-09-12 reviewer sources are absent from the frozen tree

**Classification:** reproducibility/source-traceability defect only.

**Evidence:** Spine frontmatter cites `reviews/review-2026-09-12-security-data-integrity-v3.md` and `reviews/review-2026-09-12-brownfield-drift-v3.md` (`ARCHITECTURE-SPINE.md:79-80`), but neither file exists in the current reviews directory. This persists from the prior verified-current review.

**Impact:** No runtime rule changes, but a later maintainer cannot reproduce the source chain the final spine claims.

**Disposition — AUTOFIX DOCUMENTATION.** Generate the exact cited reports or remove/replace the citations with the actual review artifacts. Do not infer their conclusions.

## Low

### L-R8-1 — The external-prerequisite map omits AD-22 from `EXT-PARTIES-1`

**Classification:** summary traceability only; the external register and stories remain fail-closed.

**Evidence:** The spine's dependency map lists `EXT-PARTIES-1` as governed by AD-2, AD-7, AD-8, and AD-30 (`ARCHITECTURE-SPINE.md:1016`). AD-22 directly consumes historical human identity in compliance subject sets, export, inspection, hold release, and deletion (`ARCHITECTURE-SPINE.md:350`). The authoritative external register correctly lists the consuming stories (`external-dependency-register.md:105-117`).

**Impact:** Availability and story gating remain correct; only curated impact analysis can miss AD-22.

**Disposition — AUTOFIX DOCUMENTATION.** Add AD-22 to the map entry without changing dependency status or consumers.

## Requested Cross-Contract Audit

| Boundary | Result | Evidence / disposition |
| --- | --- | --- |
| Decision self-supersession | **Pass** | Pending successors cannot replace effective predecessor governance; root/predecessor authorization, nonempty quorum, explicit narrowing approval, unioned affected evaluations, and activation ordering are now explicit in AD-17/register/matrix. |
| Release Operator recorder authority | **Fail** | The prior Platform-vs-Release mismatch is corrected, but H-R8-2 leaves the tenant assignment for a Platform operation undefined and H-R8-3 permits dual-hatted recorder approval contrary to the PRD/story. |
| Prepare recovery branch serialization | **Fail at branch source** | Dedicated Resume/Abort rows and expected-revision disposition facts close the v7 race. C-R8-1 shows the generic cancellation-or-failure source contradicts AD-22, cannot authorize normal crash Resume, and can unwind legal hold without the required authority. |
| Hold/export preservation | **Fail at branch entry and external seam** | Frozen committed-artifact sets, exact acknowledgements, and restrictive pending are present. C-R8-1 permits unowned hold unwind; H-R8-5 leaves the actual store pin/unpin/outcome contract unspecified. |
| Actor-evidence union | **Fail narrowly** | PRD, AD-30, and epics use a sound Party/User vs Administrator vs Platform union; AD-22 still requires a Party binding for Party-free Tenant Administrators (H-R8-4). |
| Deferred PRD decision materialization | **Fail at inventory/identity mechanism** | OQ-18/OQ-23/OQ-31 now have stable visible records and correct affected scopes without selected outcomes. H-R8-1 shows runtime has no authoritative expected-record catalog and no `DecisionId` rule by which missing records can be detected consistently. |
| Rate timeout bounds | **Pass** | Positive `AdmissionPreparationTimeout`, strict inequality to both frozen windows, exact shared deadline/profile version, and reset-boundary abort are aligned across AD-21, matrix, register NFR-12, and Story 6.4. |
| Safety cohort enumeration | **Pass** | Policy publication freezes the provision-event tenant cohort; directory catch-up gives deterministic indexed-Conversation count/hash/high-water; concurrent tenant provisioning and unindexed Conversations have explicit conditional initialization. |
| Readiness self-bootstrap | **Pass** | EventStore bootstrap/repair remains target-limited and gate-free; other observations require EventStore readiness but not the target gate. |
| Kill-switch pull/release | **Pass** | Containment pull is gate-free with direct incident/review authority and expected revision; release is separately reviewed and normally gated. |
| Public status vocabulary | **Pass as architecture** | AD-15 preserves PRD statuses and reason/progress separation. Existing legacy proposal-operation statuses are explicitly delivery debt. |
| Rate/open/budget owner races | **Pass with visible Product blocker** | The three lifetimes have separate owners; interaction-owned decisions/acks close preparation and settlement races; `OD-RATE-CONCURRENCY-CONSUMPTION-1` blocks before either ledger without choosing the consumption policy. |
| Trusted replay/security recorder | **Pass** | First-seen replay registration precedes target idempotency; only the two named ACL-confined pre-command capabilities exist; exact replay, rotation, outage, spool, and acknowledgement recovery are bound. |
| Export/deletion cleanup and destruction | **Pass after a branch is validly selected** | Partial-output inventory, `ExportCleanupPending`, authenticated lookup/receipts, `DestructionStarted`, and all-copies completion are one-way and fail closed. C-R8-1 concerns the reversible branch-selection authority before those rows. |
| Open Product/Governance/Security decisions | **Pass as surfaced blockers** | Hold/deletion precedence, export lifecycle, rate/concurrency consumption, Dapr security, sprint history, and OQ-18/OQ-23/OQ-31 remain open with owners and safe states; this review chooses none. H-R8-2 explicitly returns the recorder-scope choice to Product. |
| Architecture vs delivery debt | **Pass** | Missing decision, safety, replay, ledger, fence, export, and workflow implementations are named as delivery work. No absent aggregate is described as shipped/current. |

## v7 Critical/High Closure Audit

| v7 finding | v8 disposition |
| --- | --- |
| C-R7-1 decision self-supersession | **Closed.** Effective predecessor/root governance authorizes successor changes, pending never supersedes, quorum cannot be empty, narrowing requires explicit authorization, and blocker/affected-evaluation unions remain until activation. |
| H-R7-1 Platform recorder contradicted FR-33 | **Exact role mismatch closed, replacement authority incomplete.** Release Operator is now the recorder. H-R8-2 and H-R8-3 are new scope/separation contradictions created or exposed by the replacement model. |
| H-R7-2 recovery rows selected branches | **Serialization closed, entry authority not closed.** A durable expected-revision disposition and separate rows now prevent concurrent branch switching. C-R8-1 concerns which source is allowed to create that event and whether hold abort has legal authority. |
| H-R7-3 mutation convention contradicted security primitives | **Closed.** Spine summary and `IMPLEMENTATION-CONVENTIONS.md` permit exactly the replay registrar and security recorder and require a generic rejection test for every other bypass. |

## Authoritative 2026-09-12 Finding Closure Audit

| Authoritative finding | v8 disposition |
| --- | --- |
| C-1 weaker safety-policy retry | **Closed.** Snapshot and current policies are conjunctive; dominance is machine-checkable only. |
| C-2 hold/deletion exclusion | **Closed at the common fence/destruction boundary.** ProtectionFence serializes intents and open precedence blocks destruction. C-R8-1 is a new legal-hold prepare-abort authority contradiction. |
| C-3 bootstrap/repair deadlock | **Closed.** Typed self-bootstrap/repair and direct prerequisites prevent circular readiness. |
| H-1 Eligible-Approver recheck | **Closed.** Fresh scheduled single-flight resolution, durable empty/unavailable state, and current decision-time evidence are defined. |
| H-2 safety-rescan ownership | **Closed.** Durable epoch/index, bounded fenced coordinator, deterministic tenant/index cohorts, unindexed initialization, and recovery are defined. |
| H-3 human-only approvers | **Closed at proposal authority.** Party classification/liveness fails closed. H-R8-4 is the separate mixed-principal compliance subject-set seam. |
| H-4 conflated ledger lifetimes | **Closed.** Rate, original-caller concurrency, and monthly money have distinct owners and lifetimes. |
| H-5 human separation identity | **Closed in the principal model; one AD-22 wording conflict remains.** Stable actor identity and historical evidence are defined. H-R8-4 reconciles the tagged evidence union. |
| H-6 proposal-index crash consistency | **Closed.** Interaction source revisions, outbox application, high-water, freeze/reconcile, and repeat-to-none are bound. |
| H-7 indivisible Conversations dependency | **Closed.** Optional retraction remains separate from the six core seams. |
| H-8 envelope cryptography/replay | **Closed.** Canonical HMAC, logical/delivery identities, first-seen owner, replay retention, rotation/revocation, and exact redispatch are explicit. |
| H-9 export artifact lifecycle | **Closed at architecture invariants with one seam follow-through.** Immutable encrypted object/index/manifest/purge rules and the open lifecycle blocker remain. H-R8-5 requires the external store record to expose the new hold preservation primitive AD-22 already mandates. |
| H-10 Dapr current reality | **Closed.** Parent `1.18.5`, dirty-checkout `1.18.7`, and future Workflow adoption remain distinct. |
| H-11 public parity assertion | **Closed.** Target completion vocabulary and current repository debt are separated. |
| H-12 sprint/dependency history | **Closed as a visible delivery decision.** It does not rewrite architecture or commitment authority. |

## Good-Spine Checklist

| Criterion | Result | Reason |
| --- | --- | --- |
| Fixes the real divergence points for the level below and misses none | **Fail** | C-R8-1 leaves incompatible recovery authority; H-R8-1 through H-R8-5 leave runtime decision, authorization, evidence, and external-store units able to diverge. |
| Every AD Rule is enforceable and prevents its stated divergence | **Fail** | AD-17/AD-22 contradict on recovery source, AD-29 omits a claimed owned id, and AD-22's evidence wording conflicts with AD-30's principal union. |
| Deferred/open items are safe to defer | **Pass with Medium governance debt** | Product outcomes remain fail-closed and unchosen. Missing assumption dates and posting timeout remain explicit blockers. |
| Named technology is verified-current | **Pass** | Parent gitlinks, SDK/Dapr distinction, unselected Provider/Agent Framework, and dirty-checkout limitations match current repository authority. |
| Brownfield ratification | **Pass** | Missing target aggregates/seams are stated as delivery debt rather than shipped state. |
| Bound PRD capability and authority coverage | **Fail** | Capabilities are covered, but H-R8-2/H-R8-3 do not preserve the PRD's recorder scope/separation contract and H-R8-4 conflicts with its evidence union. |
| Inherited parent constraints | **N/A / no weakening found** | No parent architecture spine or inherited AD set is declared; root instructions/gitlinks are respected. |
| State/mutation/recovery ownership | **Fail** | Durable branch events now exist, but C-R8-1 leaves their branch-specific authority undefined and contradictory. |
| Security, tenant isolation, audit, data-loss boundaries | **Fail** | C-R8-1 can unpin held evidence; H-R8-2/H-R8-3 weaken recorder scope/separation; H-R8-4/H-R8-5 leave compliance identity and held-artifact seams inconsistent. |
| API/integration/operations/environment/provider dimensions | **Pass with H-R8-5** | The dimensions are covered and generally fail closed; the export-store artifact-preservation operation must be made an exact external contract. |
| Structural handoff and source traceability | **Fail narrowly** | M-R8-3 cites absent review files; L-R8-1 is a stale dependency map. Other diagrams, seed, maps, registers, and debt tables are present. |
| Mechanical validity | **Pass** | Deterministic lint returned zero findings. |

## Architecture Defects Versus Implementation Debt

The ten counted findings are architecture/governance/source-contract defects or explicit unresolved inputs. They do not count absent implementation as a second defect.

| Current repository gap | Classification / existing disposition |
| --- | --- |
| No `ArchitectureDecisionRecord`, required-decision projection/catalog, trusted replay aggregate, security recorder/spool, split rate/open/budget ledgers, safety epoch/index, common protection fence, protected export store, or Dapr Workflow owner | **Implementation debt.** Assigned across Stories 5.4-5.7, 6.1, 6.3-6.4, and 8.1-8.3 plus external records. H-R8-1 asks the architecture to define the missing-decision inventory/key contract before that implementation; it does not report absent code as the defect. |
| Current interaction contracts still expose legacy proposal-operation status members | **Implementation debt.** AD-15 and stories require additive/compatibility migration and do not claim current parity. |
| All twelve external dependency records remain `Uncommitted` | **External delivery state and launch blocker.** H-R8-5 tightens the required artifact for one already-blocking record; it does not relabel `Uncommitted` as an architecture failure. |
| Parent Dapr family remains `1.18.5`, dirty Builds checkout contains `1.18.7`, and Agents has no Workflow reference | **Recorded delivery/security decision.** `ARCH-A-15` and `OD-DAPR-SECURITY-1` remain open and fail closed. |
| Story 5.1/5.2 tracking and evidence disagree | **Delivery-history decision.** `OD-SPRINT-5.1-5.2-1` blocks dependent authorization and changes no architecture. |

## Gate Conclusion

The frozen v8 artifacts do **not** pass: **1 Critical, 5 High, 3 Medium, 1 Low**. First close C-R8-1 by giving Resume and Abort branch-specific, hold-safe authority. Then define the runtime decision catalog/keyspace, resolve the Release Operator platform-scope and unconditional recorder/approver separation, reconcile AD-22 to the tagged human-evidence union, and make artifact pin/unpin an exact export-store contract. Preserve every existing AD and decision id, and do not select any unresolved Product/Governance/Security outcome.
