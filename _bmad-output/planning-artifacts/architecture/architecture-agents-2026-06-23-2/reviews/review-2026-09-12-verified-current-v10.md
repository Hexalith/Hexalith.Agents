---
name: Hexalith Agents verified-current cross-artifact authority review v10
type: architecture-review
target: ../ARCHITECTURE-SPINE.md
date: 2026-09-12
lens: verified-current-cross-artifact-authority
intent: validate-read-only
verdict: fail
counts:
  critical: 1
  high: 2
  medium: 3
  low: 0
---

# Verified-Current / Cross-Artifact Authority Reviewer Gate — 2026-09-12 v10

## Verdict

**FAIL — 1 Critical, 2 High, 3 Medium, 0 Low.** The current snapshot preserves the authoritative validation report's three Critical and twelve High corrections and closes the v9 Critical/High findings. The newly added Conversation-deletion intake does not yet converge: it freezes a finite interaction checkpoint without a durable admission barrier or a legal rule for an in-flight `PostingPending` attempt; the Conversations-owned seam has no durable delivery/acknowledgement owner; and the signal branch prohibits human impersonation while the shared tombstone schema unconditionally requires a requester and approver. These are target-architecture and cross-team handoff defects, not evidence that the target has been implemented. PASS requires zero Critical and zero High.

## Frozen Snapshot And Method

The inputs below were SHA-256 frozen before analysis and rechecked after this report and the linter run. The complete current spine, authoritative report, implementation convention, bound PRD, epics, both registers, architecture memlog, repository instructions, root manifests, root-authoritative gitlinks, relevant current source, all three 2026-09-12 v9 review reports, and declared source chain were inspected. The gate was rerun as a whole-artifact review rather than a v9 closure checklist. No reviewed artifact or submodule was edited.

| Frozen input | SHA-256 |
| --- | --- |
| `ARCHITECTURE-SPINE.md` | `50de644622287cb62bec31a56ee8dff3028e04c06221cd8d292e3b0cbdb40604` |
| authoritative `VALIDATION-REPORT-2026-09-12.md` | `950993a4aa262c9209ac5b42a70e4db3b67761e74b201b39e26529d64af71529` |
| `IMPLEMENTATION-CONVENTIONS.md` | `a006a169b1b379df0ed41b3bea75eaf47859bf494fa3c0d6541ed8259d72a38c` |
| architecture `.memlog.md` | `6f366ce5ca778ea66cde8cf4aba6a11161934a0998d5f4fe1f7e5fa2a9b75908` |
| bound `prd.md` | `af43b92cf23c983ab31d85766cebb74b8ed884fdc4f7ed8c84b0f6aa5f4645f5` |
| `epics.md` | `687c4c20723e16c1bddc9a23ab30c10133dfbaec436e4eec2f6d43dc4fa5b906` |
| `external-dependency-register.md` | `cd6bd54ceceb147fe6f7c25190450ab27c4a45a6ca922baac93ddb98fd55b836` |
| `launch-readiness-register.md` | `f329e279b6e9675a930ccfd5d5e54ff4bdc87655b590e564a7303f4e9f2fd24f` |
| repository instruction source | `bfbe399b567d852a74c7ee8c9217217a3ea5eb80ca591b5b8ac01acfbe838966` |
| root `.gitmodules` | `d0ab19e5734dbe7215a83bf30433f9648088df25745e0fe81306443d79f87a46` |
| root `global.json` | `fc4602f9d88c9440f70f72732343a88c5e4190223fb9b77f9b8ac8eb1c4c8c2d` |
| root `Directory.Build.props` | `9f97e796f7c071fb0511612bd41e56a06e5c622379fe5a742c9472a29fb22ee5` |
| root `Directory.Packages.props` | `4798aa2eec87ac1c5225976967b3530496d436400c8b9c4223392ea056deae27` |
| root `Hexalith.Agents.slnx` | `a13fe1705a583738712eb8d75916cf1d193094bff06d7adbd3090e4914768de2` |

Repository HEAD was `46936c9e63cabb7b329ee9f5ebd847666cbbadd2`. The root-authoritative gitlinks were AI.Tools `5f93d2ec`, Builds `a32cb422`, Commons `6da79aed`, Conversations `73bcee6f`, EventStore `ce9e779a`, FrontComposer `053b2008`, Memories `3644ef63`, Parties `fa423985`, PolymorphicSerializations `8aeed1d`, and Tenants `2fac1839`. The initialized Builds (`cf52f74c`), Conversations (`64b05083`), EventStore (`a568af4e`), FrontComposer (`1b3608c9`), and Memories (`42dfa26b`) checkouts differ from their parent gitlinks and do not supersede them.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 1 |
| High | 2 |
| Medium | 3 |
| Low | 0 |

## Critical Findings

### VC10-C1 — Conversation deletion can freeze and erase an incomplete set because no durable admission/posting barrier precedes its checkpoint

**Classification:** target architecture/data-integrity defect; not current implementation debt and not an unresolved Product outcome.

**Evidence.** AD-6 says the new workflow records its signal, freezes a by-Conversation checkpoint, terminalizes nonterminal interactions through it, “reconciles late indexed records,” and then freezes the deletion set (`ARCHITECTURE-SPINE.md:212`). Story 8.3 and the matrix repeat that fixed-checkpoint order (`epics.md:2949-2953`; `launch-readiness-register.md:240-241`). However, the authoritative `ConversationAgentState` has exactly five membership states and no deletion/admission tombstone; normal side-effect gates re-read its block, not `ProtectedDeletion` (`ARCHITECTURE-SPINE.md:174,218,270`). The ordinary `RequestInteraction` path can therefore win after the deletion checkpoint. AD-7's existing removal protocol explicitly blocks acceptance before draining and repeats until no authoritative nonterminal remains (`ARCHITECTURE-SPINE.md:218`), but the new deletion protocol does not adopt that barrier.

The phrase “terminalizes every nonterminal” is also incompatible with `PostingPending`: AD-5 makes that state uninterruptible and permits only its own posting outcome to leave it (`ARCHITECTURE-SPINE.md:198,200`), and the bound PRD repeats that rule (`prd.md:744,1080`). The deletion workflow has neither authority to force another terminal transition nor a specified wait/`MessageId`-lookup convergence rule before freezing the set.

**Literal divergence/failure.** Team A freezes checkpoint N, drains the index through N, and arms deletion. Concurrent Team B accepts interaction N+1 because `ConversationAgentState` has no deletion barrier. N+1 creates protected content after the deletion set is frozen; deletion can report completion while a derived copy survives. In a second race, a `PostingPending` attempt exists at N: one implementation illegally abandons it, another excludes it from deletion until its own timeout, and a third erases its workflow/projection state while an at-least-once Conversation append remains capable of succeeding. These outcomes violate FR-30's all-derived-content trigger and AD-22's complete-set/receipt invariant and can produce irreversible incomplete erasure or contradictory posted/tombstone state.

**Required correction — AUTOFIX ARCHITECTURE, no Product choice.** Before freezing an interaction checkpoint, append a permanent signal-derived deletion/admission barrier to the authoritative per-Conversation owner at an expected revision. `RequestInteraction`, regeneration, approval, retry, and `BeginPosting` must re-read/serialize against that barrier so no new content-bearing work can cross it. Freeze a post-barrier authoritative by-Conversation high-water, drain/reconcile the source-of-truth interaction/outbox set through it to a fixed point, and only then freeze the `ProtectionFence` deletion set. Define state-specific convergence: awaiting, `Approved`, and `PostingFailed` interactions may take their already-authorized deletion terminalization; `PostingPending` must reach only its AD-5 posting outcome using the exact `MessageId` lookup before its resulting content/copies enter the frozen set. Recovery must replay the same barrier/high-water and failure-injection must cover immediately before/after barrier, interaction append/outbox, `BeginPosting`, Conversation append/lost acknowledgement, drain, set freeze, and `DeletionArmed`.

## High Findings

### VC10-H1 — The mandatory Conversations deletion signal has no durable delivery and acknowledgement contract

**Classification:** cross-repository seam/handoff defect; not implementation debt and not a Product decision.

The bound PRD says an approved Conversations deletion **triggers** FR-30 deletion of derived Agents content (`prd.md:882,932,962`). `EXT-CONV-AI-1` specifies a signal emitted after durable source approval, exact target authentication, duplicate handling, and source-revision lookup for a lost acknowledgement (`external-dependency-register.md:57,60`). It does not require a Conversations-owned transactional outbox/event feed, ordered delivery checkpoint, retention/backfill, retry-until-Agents-acknowledgement, or a durable Agents acknowledgement. The Intake operation is correctly allowed to reject without state during dependency/readiness/unavailability failures (`ARCHITECTURE-SPINE.md:212`; `launch-readiness-register.md:240`), so source-side durability is not optional.

A fire-once producer and a durable-outbox producer both satisfy the written “emitted” seam but behave differently when Agents is unavailable at first delivery. Lost-ack lookup resolves ambiguity after an attempted append; it does not recover a signal that was never durably retained or accepted. The former implementation can permanently omit mandatory deletion propagation.

**Required correction — AUTOFIX ARCHITECTURE/REGISTER.** Bind seam 6 to a Conversations-owned durable outbox or ordered tenant/source event feed created atomically with the approved-deletion decision, retained and retried with the same immutable signal until an authenticated durable Agents acknowledgement/checkpoint covers its exact source revision. Define reconnect/backfill, ordering, target-version rollover, poison/conflict handling, and which side owns acknowledgement recovery. Agents should durably admit the authenticated signal before gating long-running processing, or the source must retain and retry every typed refusal. This selects no Product outcome; it makes the settled cross-system trigger reliable.

### VC10-H2 — The signal workflow forbids human impersonation while the shared deletion tombstone requires human-shaped requester and approver fields

**Classification:** cross-artifact audit-schema contradiction; not implementation debt and not an unresolved Product outcome.

AD-30 says `ConversationDeletionPropagation` carries no human identity and cannot impersonate a human requester or approver (`ARCHITECTURE-SPINE.md:426`). Story 8.3 likewise says the signal branch has no human or `OnBehalfOfPartyId` (`epics.md:2952`), yet says it uses the same safe tombstone contract as an operator request (`epics.md:2953`); that contract unconditionally records `requester` and `approver` (`epics.md:2962`). Neither AD-2's `ProtectedDeletion` shape nor the matrix defines an origin discriminator that makes these fields variant-specific (`ARCHITECTURE-SPINE.md:174`; `launch-readiness-register.md:240-241`).

One implementation can synthesize operator/Inspector values and violate AD-30, another can omit mandatory tombstone fields, and a third can place a service identity and opaque source approval reference into fields consumers understand as authenticated humans. The resulting immutable audit evidence is not interoperable and can misstate who authorized erasure.

**Required correction — AUTOFIX SCHEMA.** Define one closed discriminated origin, for example `OperatorApprovedDeletion(RequesterPrincipal, ComplianceApprovalEvidence)` versus `ConversationApprovedDeletion(AuthenticatedSourceIdentity, SignalId, SourceStreamRevision, SourceApprovalReference, SourceContractVersion)`. The tombstone records exactly the origin-specific evidence plus common policy/version/scope/time/result fields; the source variant never populates or claims a human requester/approver. Synchronize AD-2/AD-22, Story 8.3, matrix fixtures, public safe status, and audit/export schemas.

## Medium Findings

### VC10-M1 — Two declared source files remain absent

Spine frontmatter cites `reviews/review-2026-09-12-security-data-integrity-v3.md` and `reviews/review-2026-09-12-brownfield-drift-v3.md` (`ARCHITECTURE-SPINE.md:79-80`), but neither path exists. Every other declared local source inspected resolves. Generate the reports or remove/replace the citations; their absent conclusions cannot be inferred.

### VC10-M2 — Architecture-owned `RQ-1` assumptions still lack the literal dates required by the bound PRD

PRD FR-28 requires every Architecture-owned `RQ-1` assumption to carry a co-owner-approved literal calendar retirement date, with a non-date itself blocking (`prd.md:730`). The current assumption index still labels numerous Architecture-owned rows “Unscheduled” or names only a story/milestone (`ARCHITECTURE-SPINE.md:1061-1079`). The safe blocker remains visible, so this is not a fail-open defect, but the spine does not yet satisfy its bound governance rule. Obtain recorded dates or retire/supersede the rows; do not invent dates.

### VC10-M3 — The `PostingPending` deadline remains intentionally non-convergent for implementers

AD-5 requires a stored deadline no shorter than the Conversations posting timeout but fixes no duration, configuration owner, allowed range, or versioned lookup (`ARCHITECTURE-SPINE.md:198`). `ARCH-A-14` correctly exposes the gap, and Story 7.4 remains blocked pending retirement, so it is not hidden implementation debt. Resolve the actual value/source before Story 7.4 becomes ready-for-dev; do not accept an implementation-local default as architecture evidence.

## v9 Critical/High Closure Audit

Every v9 Critical/High item from the verified-current, rubric, and adversarial reviews was checked against current text:

| v9 issue family | Current disposition |
| --- | --- |
| Export commit lacked one irreversible owner/initial operation | **Closed.** `ProtectionFence` owns `ExportCommitDecided` and index high-water; `GovernanceProtection:ExportCommitDecision` is the single append and `ExportCommitRecovery` is secondary acknowledgement (`ARCHITECTURE-SPINE.md:174,362`; `launch-readiness-register.md:238-239`). |
| Posting approval/`BeginPosting` ordering and pre-post diagram divergence | **Closed.** Approval appends only `Approved`; both modes perform the named validation/read before `BeginPosting`, then append (`ARCHITECTURE-SPINE.md:200,579-596`; `epics.md:2527-2540`). VC10-C1 concerns deletion racing this now-consistent protocol. |
| Decision catalog had no activation operation | **Closed.** `ArchitectureDecision:ActivateCatalog` and its immutable manifest/lost-ack behavior are explicit (`ARCHITECTURE-SPINE.md:328`; `launch-readiness-register.md:224`). |
| Missing catalog used an undeclared blocker | **Closed.** It now emits existing `OpenDecision` with detail `CatalogAbsentOrInvalid` (`ARCHITECTURE-SPINE.md:328`; `launch-readiness-register.md:78,91,112`). |
| Generic recovery contradicted branch-specific Resume/Abort | **Closed.** NFR-11 uses original-valid-intent Resume and decision/profile-specific Abort (`launch-readiness-register.md:303`). |
| No-hold deletion was gated by the armed-contention decision | **Closed.** The matrix has the exact no-overlap-or-approved-contention disjunction (`launch-readiness-register.md:245`). |
| Hold-cancellation decision was promoted to global `RQ-1` | **Closed.** Its affected scope explicitly excludes `RQ-1` (`launch-readiness-register.md:96`; `ARCHITECTURE-SPINE.md:1007`). |
| OQ-31 Story 8.3 blocker was conditional | **Closed.** Story authorization and every deletion signal/request block unconditionally while Open (`epics.md:2929,2944-2947`; `launch-readiness-register.md:103`). |
| Hold-release recovery omitted the recorded lifecycle phase pin | **Closed.** The phase-pin prose and recovery row name the recorded human decision, exact lifecycle version, and store target (`launch-readiness-register.md:106,112,233`). |
| Rate/concurrency diagram selected an unresolved consumption outcome | **Closed.** It records the exact approved disposition and commits or aborts each rate scope according to that disposition (`ARCHITECTURE-SPINE.md:522-539`). |
| Conversations-deletion propagation lacked a delivery story/consumer | **Closed as ownership mapping, reopened as protocol defects VC10-C1/H1/H2.** Story 8.3, `EXT-CONV-AI-1`, `ConversationDeletionPropagation:Intake`, and AD-30 now name the consumer and capability (`epics.md:2949-2953`; `external-dependency-register.md:57-65`; `launch-readiness-register.md:240`; `ARCHITECTURE-SPINE.md:426`). |
| Frontmatter/decision-table omissions and Story 8.1 export-store scope | **Closed.** The spine binds OQ-1 through OQ-34 and lists recorder scope; Story 8.1 conditions the store on committed-artifact overlap (`ARCHITECTURE-SPINE.md:10-14,1013`; `epics.md:2773-2777`). |

## Authoritative Validation-Finding Closure Audit

The authoritative report's original findings remain architecturally corrected. The v10 findings are follow-through defects in the later Conversation-deletion protocol and do not reverse those closures.

| Finding | Current disposition |
| --- | --- |
| C-1 weaker retry safety | **Closed:** AD-20 requires snapshot-and-current conjunctive evaluation or a machine-checkable dominance proof (`ARCHITECTURE-SPINE.md:348`). |
| C-2 hold/deletion exclusion | **Closed at the common fence:** AD-22 and matrix v4 serialize prepare, arm, contention, destruction, cleanup, and receipts. VC10-C1 is the missing pre-fence Conversation admission boundary (`ARCHITECTURE-SPINE.md:362`; `launch-readiness-register.md:227-247`). |
| C-3 bootstrap/repair deadlock | **Closed:** direct target-aware bootstrap/repair and closed operation variants remain explicit in AD-17/AD-23 and matrix v4. |
| H-1 Eligible-Approver recheck | **Closed:** the durable scheduled lifecycle, finite membership, and authoritative empty/unavailable behavior remain explicit in AD-8. |
| H-2 distributed safety rescan | **Closed:** AD-20 owns epoch/index, finite manifests, fenced coordinator/workers, and call behavior (`ARCHITECTURE-SPINE.md:348`). |
| H-3 human-only Approvers | **Closed:** AD-8 and AD-30 bind Parties-owned human/liveness and stable actor identity. |
| H-4 conflated ledgers | **Closed:** AD-21 separates rolling-rate, original-caller open lease, and monthly budget owners/lifetimes (`ARCHITECTURE-SPINE.md:354`). |
| H-5 human separation identity | **Closed:** the tagged principal union and mandatory stable `AuthenticatedHumanActorId` remain explicit (`ARCHITECTURE-SPINE.md:424`). |
| H-6 proposal-index crash consistency | **Closed architecturally:** AD-7 owns same-append outbox, source high-water, fixed-point drain, and recovery (`ARCHITECTURE-SPINE.md:218`). VC10-C1 requires the new deletion intake to use an equivalent barrier. |
| H-7 indivisible deferred Conversations seam | **Closed:** core `EXT-CONV-AI-1` and optional `EXT-CONV-RETRACTION-1` remain separate with branch-scoped consumption. |
| H-8 trusted-envelope security | **Closed:** AD-29/AD-30 define canonical input, logical/delivery identity, durable replay registration, expiry, rotation/revocation, and ACL-limited security capabilities. |
| H-9 export package ownership/lifecycle/signature | **Closed architecturally:** AD-22 and the export-store register bind store/index/manifest/signature/fence/cleanup and retain unresolved lifecycle policy as an Open decision. |
| H-10 current Dapr exposure | **Closed as truth/debt:** the stack and debt table distinguish transitive Client/ASP.NET exposure from future Workflow adoption and distinguish root `1.18.5` authority from dirty `1.18.7` (`ARCHITECTURE-SPINE.md:658-659,1032`). |
| H-11 shipped public-contract parity claim | **Closed:** incomplete public vocabulary is classified as delivery debt rather than current parity (`ARCHITECTURE-SPINE.md:1031`). |
| H-12 Story 5.1/5.2 tracker conflict | **Closed as surfaced delivery governance debt:** the discrepancy remains isolated under `OD-SPRINT-5.1-5.2-1`, outside architecture and `RQ-1` (`ARCHITECTURE-SPINE.md:1033`; `launch-readiness-register.md:100`). |

## Cross-Artifact Authority, Dependencies, And Open Decisions

The current catalog preserves ten stable literal `OD-*` ids. Scope checks found no fresh Product-outcome leak: hold/deletion precedence affects only armed contention plus its recorded `RQ-1` scope; hold-prepare cancellation remains story/operation-only and not `RQ-1`; rate/concurrency and sprint reconciliation remain story-only; OQ-23 remains Automatic-only; OQ-18/OQ-31, export lifecycle, Dapr security, and recorder scope retain their recorded blockers. No outcome was inferred in this review.

The external dependency register remains commitment authority and every current `EXT-*` consumer mapping inspected has a story/evidence owner. In particular, Story 8.3 is now listed under core `EXT-CONV-AI-1`, and export-store use in Stories 8.1/8.3 is conditional while Story 8.2 is unconditional. VC10-H1 is not a missing consumer; it is the absence of reliable delivery semantics inside the now-owned seam. Target versions/dates/commands that remain `TBD` keep entries `Uncommitted` and block live consumers, as intended.

AD identifiers are preserved: exactly one heading each for AD-1 through AD-31, with no gap, duplicate, or renumbering.

## Verified Technology And Repository Reality

The root manifest pins SDK `10.0.401` with `latestPatch`, and the installed SDK reports `10.0.401`. The official .NET 10.0.12 release notes list SDK `10.0.401`/`10.0.112` and the cited Windows servicing fix, so the stack's present fixed-floor wording remains current. The parent-authoritative Builds catalog at `a32cb422` pins `Dapr.Client`, `Dapr.AspNetCore`, and `Dapr.Workflow` `1.18.5`; the non-authoritative checkout pins `1.18.7`, whose official NuGet package pages exist. Current Agents project references consume EventStore Client/DomainService and therefore Client/ASP.NET transitively, while no Agents project references Dapr Workflow, Aspire hosting, or Microsoft Agent Framework. Official Dapr Workflow/Agents documentation and Microsoft Agent Framework package/overview pages resolve; the spine leaves hosting and Agent Framework unselected rather than inventing versions.

Current source still lacks the target decision catalog/records, three ledgers, safety epoch/index, trusted-envelope replay/security spool, governance fence/export/deletion protocol, and complete public vocabulary. Current approval code still emits posting-pending state and calls Conversations before EventStore dispatch. Those facts match the delivery-debt table; they do not authorize weakening AD-1 through AD-31. The new Conversation-deletion protocol is likewise target architecture under Story 8.3/`EXT-CONV-AI-1`, not a shipped feature. The table's broader “no shared hold/deletion fence or all-copies receipt” row correctly covers its unimplemented deletion substrate, although VC10-C1/H1/H2 must be repaired in the target before delivery.

## Linter And Source Resolution

Command:

```text
uv run .agents/skills/bmad-architecture/scripts/lint_spine.py --workspace _bmad-output/planning-artifacts/architecture/architecture-agents-2026-06-23-2
```

Result: `ok: true`, `total_findings: 0`, with zero severity entries. Mechanical validity does not close the semantic findings above. All declared local spine sources inspected resolve except the two files in VC10-M1; official technology and standards sources resolve to the named primary authorities.

## Gate Conclusion

The frozen artifacts fail v10 with **1 Critical, 2 High, 3 Medium, and 0 Low findings**. Repair Conversation-deletion quiescence and `PostingPending` convergence first, then reliable inter-service delivery and the origin-discriminated tombstone, without selecting any unresolved Product/governance outcome. Re-distill, append the changes to the memlog, lint, freeze/hash, and rerun the complete reviewer gate. PASS requires zero Critical and zero High.
