---
name: Hexalith Agents good-spine rubric review v23
type: architecture-spine-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-13
intent: review-read-only
lens: bmad-architecture primary good-spine rubric walker
verdict: pass
critical: 0
high: 0
medium: 2
low: 1
lint_ok: true
root_commit: 46936c9e63cabb7b329ee9f5ebd847666cbbadd2
---

# Good-Spine Rubric Reviewer Gate — v23

## Verdict

**PASS — 0 Critical, 0 High, 2 Medium, 1 Low.** The v22 High is closed: all normative inventories now agree that exactly three tightly scoped pre-command security primitives exist, with Story 5.4 owning replay/security recording and Story 8.3 owning deletion-key compromise registration. A fresh whole-package walk found no new Critical or High contradiction. The v23 stable pre-seal destruction identity and replacement-key activation arbitration form one implementable, fail-closed protocol across the spine, conventions, PRD, epics, dependency contracts, and matrix-v7. The remaining Medium items are already-restrictive planning parameters, and the Low item is build-maintenance debt.

## Frozen Inputs And Review Scope

I read the repository instructions and complete BMad reviewer-gate instructions, then reviewed the full current spine, implementation conventions, authoritative validation report, bound PRD, epics, external-dependency register, launch-readiness register, declared local sources, and focused repository reality. The walk covered every good-spine dimension rather than only prior findings: enforceability and actual divergence prevention; spec traceability; ownership, concurrency, authorization/effect/result order, replay, lost acknowledgement, migration, restore, security, tenant isolation, irreversible data loss, Product-decision discipline, delivery-debt separation, sources, and mechanics.

Per the explicit instruction, I did **not** read or modify the architecture `.memlog.md`; I used `sha256sum` only. This report therefore makes no content-level append-only memlog claim. The five v23 review paths declared in spine frontmatter were treated as concurrent anticipated gate outputs, not missing historical sources. No reviewed artifact, code, submodule, PRD, register, conventions file, or memlog was modified; this report is the sole write.

The supplied hashes matched at review start:

| Input | Frozen SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `b382475bf559f9d6262273ca498df16197919fead492fd72f4aa80ac5b16c1d4` |
| `IMPLEMENTATION-CONVENTIONS.md` | `ea1d311da8bef578c7fca428d6b66ef7ae91dc593eb5b3efc9cac382e2a938d9` |
| architecture `.memlog.md` — hash only, never opened | `890d18b355b6b7d475241426641701ebe724dd6906bed15fa180a5e14a0e17e0` |
| bound `prd.md` | `4fbc21e13301bb47c87e077ec7aa884ff698121837b0e19cbf6e0ea6e4334564` |
| `epics.md` | `15f208cd8e2270dfb82352c5d7303b083ffd8f64c91ae40013a946fae6f69982` |
| `external-dependency-register.md` | `3f29c4651eb79e475bd087fff402903652984218b1c66f1d26cd86dcfbb5a4f3` |
| `launch-readiness-register.md` | `bd96ff74f3a6b3343b1a2f5922a9f7a65cefb7a1dd29fcff728714717ec1ca20` |
| `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |

## Deterministic Lint And AD Preservation

Command:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: **PASS**, `ok: true`, `total_findings: 0`.

An independent heading/required-field scan found exactly one each of AD-1 through AD-31, maximum AD-31, no gaps or duplicate/reused headings, and `Binds`, `Prevents`, and `Rule` on every AD. The current spine preserves the established AD identities and adds no renumbered replacement. Because memlog contents were intentionally excluded, this is a current-spine identity scan plus prior review continuity, not a new memlog-content audit.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 0 |
| High | 0 |
| Medium | 2 |
| Low | 1 |

## Critical

None.

## High

None.

## Medium

### M-R23-1 — Architecture-owned `RQ-1` assumptions still lack literal retirement dates

**Classification:** planning-governance debt; safely blocking, not a runtime architecture defect.

The PRD requires each Architecture-owned assumption to carry a co-owner-approved literal calendar retirement date and says a milestone or `TBD` remains an `RQ-1` blocker (`prd.md:894-898`). `ARCH-A-1`, `ARCH-A-2`, `ARCH-A-3`, the remaining test-stack part of `ARCH-A-4`, `ARCH-A-6`, `ARCH-A-7`, `ARCH-A-8`, `ARCH-A-11`, `ARCH-A-12`, and `ARCH-A-14` remain `Unscheduled` or milestone-only (`ARCHITECTURE-SPINE.md:1380-1395`). The spine correctly keeps every affected evaluation blocked, so two units cannot use the omission to pass readiness differently. Obtain the required co-owner dates; Architecture must not invent them.

### M-R23-2 — `PostingPending` still has no single concrete timeout authority

**Classification:** safely surfaced Architecture/Product parameter, not hidden implementation debt.

AD-5 and PRD FR-18 require a stored, non-extendable attempt deadline no shorter than the Conversations seam-2 timeout but select neither a literal duration nor one exact versioned configuration authority (`ARCHITECTURE-SPINE.md:288,1395`; `prd.md:438`). Two otherwise conforming posting/recovery units could therefore persist different deadlines. `ARCH-A-14` explicitly blocks the owning story/evaluation rather than permitting a local default. Bind a literal duration or a named versioned seam/profile field and obtain Product confirmation before implementation; this review selects no outcome.

## Low

### L-R23-1 — Root bUnit remains behind the checked-out catalog

**Classification:** implementation/build maintenance debt, not a target-architecture defect.

Root authority pins bUnit `2.9.0` (`Directory.Packages.props:26`), while the currently checked-out Builds catalog pins `2.10.3` (`references/Hexalith.Builds/Props/Directory.Packages.props:317`). The spine accurately assigns legacy conformance/test-stack alignment to Story 5.6 (`ARCHITECTURE-SPINE.md:1351`); no architecture rule or release success claim depends on the newer checkout.

## V22 Critical/High Closure Audit

### H-R22-1 — Third pre-command compromise registrar rejected by stale two-item inventories

**Closed.** Every current normative inventory now permits exactly the same three target-limited primitives and rejects every other bypass:

- AD-3 enumerates replay registration, security observation recording, and deletion-capability key-compromise registration as the only three (`ARCHITECTURE-SPINE.md:276`).
- AD-30 calls replay registration one of three and the security recorder the second, then defines the compromise registrar under the same closed authority model; the mutation convention enumerates all three (`ARCHITECTURE-SPINE.md:579,864`).
- The implementation convention and its adoption test both say exactly three and assign the first two to Story 5.4 and the registrar to Story 8.3 (`IMPLEMENTATION-CONVENTIONS.md:43,79`).
- Matrix-v7 has three distinct no-readiness-record rows with exact target, ACL, and effect constraints (`launch-readiness-register.md:232-234`).
- Story 5.4 now scopes its evidence to its own replay/security-recorder pair rather than claiming the entire AD-30 inventory has only two, while Story 8.3 owns registrar/revocation/re-attestation evidence (`epics.md:1480,3037-3038,3063-3072`).

A conformance implementation can therefore allow the registrar only for the authenticated Secrets-revocation subscriber, exact tenant/key protection block, and exact guard mirror without opening a public, human, Workflow, target-handler, general-dispatcher, wrong-tenant, wrong-key, wrong-stream, or broader mutation path.

## V23 Adversarial Mechanics Check

### Stable pre-seal destruction identity and signed-but-unissued recovery

**Pass.** `ProtectionFence:AcceptDeletionInventory` owns one immutable accepted set/token and assigns `DestructionSealId = H(deletion-destruction-seal, TenantId, DeletionRequestId, AcceptedDeletionToken, ScopePredicateDigest, AcceptedManifestDigest)` before barrier authorization; no guard revision enters it (`ARCHITECTURE-SPINE.md:218,240,501,567`). `DeletionDestructionBatchId` composes that stable seal id with immutable batch kind/ordinal/manifest, and the signing request separately binds attestation, signing-attempt ordinal, intended issue revision, key version, and audience (`ARCHITECTURE-SPINE.md:567,571`). The detached JWS contains the intended conditional-append revision, while the actual successful `CommittedIssuedGuardRevision`/`CommittedDestructionSealGuardRevision` is authenticated result evidence only (`ARCHITECTURE-SPINE.md:260,268`; `external-dependency-register.md:199-201`; matrix rows 328-332 and 349-350). Thus a guard mutation after signing yields a provably never-issued, terminal `SignedAttestationObsoleteUnissued`; a successor keeps seal/batch/manifest/attestation identity and increments only its signing attempt. Unknown signer/issue evidence stays pending, so retry cannot mint or activate ambiguous authority.

The derivation is non-circular: the accepted token, predicate digest, and accepted-manifest digest exist at fence acceptance before signing or guard issue. A superseded pre-seal acceptance preserves its historical identity but cannot pass current token/cut/binding checks; a valid retry of the same accepted inventory remains byte-stable. Matrix-v7 tests consecutive stale attempts, lost acknowledgement, rotation, issue revision separation, migration, and restore (`launch-readiness-register.md:328-332,349-350,371-373`; `epics.md:3036-3038,3071-3072`).

### Replacement-key compromise arbitration

**Pass.** The protocol gives the protection engine the single block/reserve/consume owner. Re-attestation can start only from its exact `ConsumptionBlocked(CapabilityKeyCompromise)` state; the batch remains blocked through guard replacement and successor dispatch. `ActivateReattestedDeletionBatch` then binds the replacement key, next attestation, successor dispatch, expected blocked-batch revision, and expected tenant-key block-set revision and conditionally reopens only if the replacement key is unblocked in that same owner transaction (`ARCHITECTURE-SPINE.md:264-266,571,601`; matrix row 359; `external-dependency-register.md:139,251`). If a second compromise exists or wins the compare, the exact typed `ActivationBlockedByReplacementKeyCompromise` result retains or moves the batch to that new key's blocked state; activation never consumes or clears the tenant-wide key block. A compromise after successful activation still races `ReserveAndConsumeDeletionBatch` on the same owner. Exact activation identity/lookup and preserved blocked/reserved/terminal state close duplicate, lost-ack, migration, and restore ambiguity. The PRD and Story 8.3 reflect the same technical rule without selecting the Open legal-hold outcome (`prd.md:960-961`; `epics.md:3037-3038,3064,3071-3072`).

### Cross-contract conclusion

The two v23 changes compose without weakening prior hold/admission rules: a stable identity grants nothing until the exact guard issue and batch dispatch exist; a dispatch remains uninterruptible only against a later hold for that same batch; admission-integrity and key compromise still contend at the protection owner; unresolved hold, issue, activation, lookup, or owner state blocks purge/completion. Migration/repair/restore manifests preserve stable and actual revisions as distinct fields, every signing attempt, registrar receipt, protection state, activation identity/result, active/revoked attestation, dispatch, and terminal vector (`ARCHITECTURE-SPINE.md:264-270,599-601`; `IMPLEMENTATION-CONVENTIONS.md:29-43`). No new Critical or High divergence was found.

## Authoritative 2026-09-12 Critical/High Closure Audit

| Authoritative finding | Current disposition |
| --- | --- |
| `C-1` weaker safety evaluation | **Closed.** AD-20 requires conjunctive snapshot-plus-current evaluation; dominance is only a tested equivalent optimization. |
| `C-2` hold/deletion exclusion | **Closed.** Admission/content fences, immutable ordinal owner cycles, fence-owned acceptance, shared hold/seal guard, atomic manifest destruction, post-start disposition gating, target coverage, and guard-linearized completion prevent false full success. |
| `C-3` matrix bootstrap/scope deadlock | **Closed.** Matrix-v7 carries the target-scoped bootstrap/repair rows that omit only a circular gate and keep unrelated checks fail-closed. |
| `H-1` scheduled Approver lifecycle | **Closed.** Single-flight ownership, cadence/freshness, two-pass empty evidence, leased resolution, typed outcomes, and unavailable recovery are fixed. |
| `H-2` distributed safety rescan | **Closed.** `SafetyVerdictEpoch`/`SafetyVerdictIndex` own the finite manifest, fenced bounded coordination, activation, recovery, and exact `RescanPending`. |
| `H-3` human-only Approvers | **Closed.** Parties-authoritative human type/liveness and current plus historical actor binding are required at configuration and resolution. |
| `H-4` ledger lifetime conflation | **Closed.** Rolling rate, caller-open interaction, monthly Budget, and capacity have separate owners, identities, and settlement rules; the remaining Product choice is isolated and restrictive. |
| `H-5` human identity/separation | **Closed.** Stable `AuthenticatedHumanActorId` spans User, Administrator, and Platform evidence; Workflow cannot satisfy human separation. |
| `H-6` proposal/index crash consistency | **Closed.** Interaction truth, protected source-revision outboxes, directory high-waters, acknowledgements, committed leases, and repair are explicit. |
| `H-7` indivisible Conversations dependency | **Closed.** The six V1 seams are `EXT-CONV-AI-1`; optional retraction is independent `EXT-CONV-RETRACTION-1`. |
| `H-8` trusted-envelope security | **Closed.** Canonical authenticated fields, logical/delivery identity, replay owner and retention, key lifecycle, ACL confinement, security spool, and lost-ack recovery are bound; v23 retains exactly three closed pre-command primitives. |
| `H-9` export bytes/lifecycle/signature | **Closed subject to the named Product decision.** Store/index ownership, immutable AEAD artifact, JCS/ES256 manifest, direct key delivery, hold/expiry fencing, cleanup, restore, and all-copy receipts are explicit and fail closed while unresolved. |
| `H-10` Dapr current truth | **Closed as current-reality classification.** Current root-authoritative transitive Client/ASP.NET `1.18.5`, non-authoritative checked-out `1.18.7`, and future Workflow adoption are distinct. |
| `H-11` public contracts falsely described as current | **Closed.** Required-completion vocabulary and current implementation debt are separated. |
| `H-12` tracker/dependency contradiction | **Closed as surfaced delivery-governance debt.** `OD-SPRINT-5.1-5.2-1` records the required owner decision without granting architecture authority or rewriting evidence. |

## Product Decisions And Architecture Versus Delivery Debt

The package surfaces rather than selects the unresolved hold/deletion precedence, hold-prepare cancellation, operator-deletion cancellation/nonterminal treatment, export lifecycle, rate/concurrency consumption, Dapr security disposition, sprint evidence, historical safety, Automatic retraction, instruction protection, legacy plaintext, initial output-safety status, class/range governance scope, human exact-Conversation semantics, and Release-recorder authority (`ARCHITECTURE-SPINE.md:1305-1326`; `launch-readiness-register.md:89-118`). Each names owners, affected evaluations, and a restrictive Open/missing behavior. V23's identity and replacement-key arbitration are technical safety mechanics, not Product outcome selection.

The repository still lacks most target mechanisms: decision catalog/recorder, safety epochs, separated ledgers, directory/effect leases and repair, governance guards, protected export storage, stable seal/signing/registrar/protection-owner state, and the durable-before-post sequence. The spine assigns these to stories and external contracts and makes every one of the twelve `EXT-*` records `Uncommitted` until qualifying evidence exists (`ARCHITECTURE-SPINE.md:1329-1351`; `external-dependency-register.md`). Their absence is implementation/delivery debt and readiness blockage, not permission to weaken the target and not a target-architecture finding. The two Medium items above remain architecture/planning inputs because separate builders could otherwise choose different values; they are safe only because the current gate blocks them.

## Sources And Whole Good-Spine Checklist

All declared local sources resolved except the five v23 review reports explicitly designated as concurrent anticipated outputs; they were not counted as missing evidence. Remote authoritative references remain declared for the verified-current lens. The root commit matched `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`; modified submodule working trees and root planning edits were treated as the frozen workspace reality, never as committed root authority.

| Dimension | Result |
| --- | --- |
| Real divergence points / enforceable ADs | **Pass.** Owners, identities, revisions, transitions, failure behavior, and recovery are named at implementation altitude. |
| Each Rule prevents its stated divergence | **Pass.** The former two-versus-three mutation contradiction is removed; v23 identity and activation rules are executable and fail closed. |
| Ownership, concurrency, data loss | **Pass.** Shared guard/fence/protection owners linearize the irreversible boundaries, including replacement-key races. |
| Recovery, replay, migration, restore | **Pass.** Signed-unissued, activation, stale authorization, exact lookup, and preservation states are complete. |
| Security and tenant isolation | **Pass.** Principal, audience, tenant, key, manifest, registrar ACL, protected-content, and unknown-state constraints remain restrictive. |
| PRD, epic, register, convention traceability | **Pass with two Medium planning parameters.** V23 mechanics and operation-matrix version are synchronized. |
| Product-decision discipline | **Pass.** Open outcomes are named, owned, affected-scope bound, and denied while unresolved. |
| Architecture versus implementation debt | **Pass.** Current code gaps are assigned delivery work; target contradictions are not relabeled as backlog. |
| Brownfield/current reality | **Pass with one Low maintenance item.** Root gitlink/package authority and non-shipped target mechanics are accurately distinguished. |
| Sources | **Pass.** Non-concurrent declared local sources resolve; v23 outputs are intentionally pending. |
| Deferred / altitude completeness | **Pass.** Deferred items cannot silently enter V1; deployment, topology, dependencies, operations, recovery, governance, and environment/readiness envelopes are represented. |
| Mechanics | **Pass.** Lint clean; AD-1..AD-31 unique, contiguous, and structurally complete. |

## Gate Result

The frozen v23 primary rubric gate is **PASS** because Critical = 0 and High = 0. The Medium assumptions/timeout parameters should remain visible blockers until their owners decide them; the Low bUnit lag remains Story 5.6 maintenance debt.
