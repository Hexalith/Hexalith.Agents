---
review: rubric-v2
target: ARCHITECTURE-SPINE.md
target_updated: 2026-09-09
reviewer: independent rubric walker (second pass, post 2026-09-09 update run)
verdict: PASS WITH FINDINGS (0 critical / 1 high / 2 medium / 3 low)
---

# Rubric Review v2 — Hexalith Agents Architecture Spine (2026-09-09)

Scope note: this is an independent second rubric pass over the spine as it stands after the
2026-09-09 distill/reconcile/reviewer-gate run recorded in `.memlog.md`. It does not re-litigate
the prior rubric review (`review-2026-09-09-rubric.md`) or the adversarial/brownfield/security
lenses; it walks the same eight-point checklist fresh against the current document text.

## 1. Does the spine fix the real divergence points one level down (stories/epics), missing none?

Overall: strong coverage. The 31 ADs, the Consistency Conventions table, and the identity/time/
principal/freshness ADs (AD-28, AD-29, AD-30, AD-17) collectively close almost every seam where two
independently-built stories could disagree — clock source, id derivation, principal shape, retry
safety, readiness vocabulary, and lock-bearing concurrency are each pinned to exactly one owner and
one algorithm. FR/NFR binds coverage is essentially complete: every `FR-1..FR-33` and `NFR-1..NFR-14`
is either individually cited in an AD's **Binds** line or covered by a cited range (`FR-8..FR-12`,
`FR-13..FR-18`, `FR-24..FR-26`, `NFR-11..NFR-14`) plus AD-1's blanket "all V1 capabilities." `OQ-20`
through `OQ-23` (the freshest PRD rows) are each visibly absorbed (AD-2 for OQ-20, AD-17 for OQ-22,
the Deferred table for OQ-23, AD-22/AD-30 for OQ-21).

**Finding (HIGH) — the vocabulary the spine fixes for AD-30 has already drifted one level down, in the very artifact meant to carry it.**
AD-30 (`Principals And Trusted Envelope`) is unambiguous: "Every Agents command envelope carries
exactly one principal: `User` ..., `Administrator` ..., `Platform` ..., or `Workflow` ...". There is
no `Service` or `System` principal kind anywhere in the spine. But `epics.md` (Epic 5, Story 5.4
acceptance criteria, line 1360) states: "it selects exactly one AD-30 principal kind from `User`,
`Platform`, `Service`, or `System` according to the operation family..." This is not a paraphrase
mismatch — `Service`/`System` and `Administrator`/`Workflow` name different concepts (e.g. `Workflow`
carries `AgentInteractionId` as instance id and `OnBehalfOfPartyId`; nothing called `Service` or
`System` is defined). Since AD-30's whole job is to prevent "a workflow activity borrowing
administrator authority" and "two role vocabularies for one matrix," a downstream story whose own
acceptance criteria quote a *different* four-way enum than the spine defines is exactly the
divergence risk AD-30 exists to close — and it has reopened it in the epics document that stories
are built from. This should be corrected in `epics.md` (or, if `Service`/`System` reflect an intended
but unshipped widening of AD-30, the spine should say so) before Story 5.4 is implemented.

**Minor citation looseness (not a gap in substance):** FR-1 ("Configure `hexa`") and FR-3 ("Manage
Agent Lifecycle") are not individually named in any AD's **Binds** line — only captured by AD-1's
blanket "all V1 capabilities." Substantively both are fully addressed (AD-2's `Agent` aggregate,
AD-4's `ConfigurationVersion`), so this is a citation-precision nit, not a coverage gap.

## 2. Binds/Prevents/Rule triads — real teeth, not aspiration

Spot-checked a representative cross-section (AD-4, AD-6, AD-9, AD-10, AD-16, AD-19, AD-20, AD-24,
AD-29, AD-31): in every case the **Rule** text supplies a concrete, testable mechanism for the
**Prevents** claim, not just restated intent — e.g. AD-16's "guard tests enforce the boundary" for no
module-owned AppHost; AD-24's linearizable `BeginInvocation(AttemptId, AdmissionId, AdmissionFence)`
with fence invalidation for "rejection after Provider cost is incurred"; AD-31's structural claim
("Conversations never references Agents packages") plus a named removal deadline ("removed before
Story 6.7 closes") for "a deep-link entry point." AD-17 additionally centralizes enforcement by
requiring every command/activity to declare exactly one operation-gate-matrix family and by binding
the live-seam-flip-requires-passing-integration-test rule, which gives teeth to ADs that would
otherwise read as descriptive (AD-9, AD-13, AD-21, AD-22 all lean on this).

**Finding (MEDIUM) — one aggregate identity carries an unenforced qualifier.**
AD-2 defines `SecurityEventLog` as "(`TenantId`, `UtcDay`; content-free, **rate-bounded**
authorization denials and security events)." Every other quantitative constraint in this document is
either given a concrete number/range (margin 10%, range 5–25; retry bound 3 attempts/15 minutes;
`Indeterminate` hold 24h, range 1–72h; 2-second timer skew) or explicitly delegated to
`launch-readiness-register.md`. "Rate-bounded" has neither: it is not defined elsewhere in the spine,
and a grep of `launch-readiness-register.md` and `external-dependency-register.md` finds no
`SecurityEventLog` rate entry. The `(TenantId, UtcDay)` key does provide a *daily* partition, which
naturally caps how long one stream can grow before rolling over, but that is a side effect of the key
shape, not a stated rate bound — nothing here stops a burst of denials within a single day from
growing the stream unboundedly, and no story is told what number to implement or test against. Either
cite the register section that owns this number, or drop "rate-bounded" and rely on daily
partitioning alone if that is genuinely the intended control.

## 3. Deferred Beyond V1 — could any deferred item let two units diverge in a way that matters?

Reviewed all seven rows individually against the PRD's OQ register and MVP out-of-scope list (§6.2).
Each row states *why* it can wait and *what governs the interim behavior* (e.g. the Conversation-owner
row points to the AD-8 Facilitator-equivalence rule and names the amendment path; the residency row
names the interim `ProcessingRegion` capability flag and the platform-host inheritance). None leaves a
V1-relevant seam open with no interim rule. No finding here.

## 4. Named tech/frameworks — internal coherence

No internal contradictions found. The Stack table's "Unselected until `EXT-PROVIDER-1`" for Provider
SDK and Agent Framework SDK is consistent with AD-9 ("Provider and Agent Framework SDKs remain
unselected...") and with AD-18's careful "Microsoft Agent Framework **may** be used" (optional, not
assumed) — the class/flow diagrams correctly draw that edge as a dotted "optional internal SDK" link.
The declared deviations (.NET SDK 10.0.301 vs sibling 10.0.400; xUnit v3/NSubstitute root overrides)
are each named as an explicit, owned, dated deviation with a retirement story (ARCH-A-4), not silently
inconsistent. (A separate lens is checking live currency of these versions; this pass only checks that
the document doesn't contradict itself, and it doesn't.)

## 5. Ratifies rather than contradicts brownfield reality

On its face, the spine handles brownfield tension well: rather than silently asserting a target state
that ignores shipped code, the memlog (and by extension the spine's Architecture Assumptions table and
inline story pointers) explicitly names known non-conformant shipped code — e.g. the tenant-scoped
`ProviderCatalogAggregate` vs. AD-2's platform-scoped design, `HttpAgentAdministrationContextProvider`
authorizing from JWT roles alone vs. AD-30, non-conformant `AttemptId` derivation vs. AD-29 — and
assigns each to a specific remediation story rather than pretending the gap doesn't exist. That is the
correct pattern for a "final" spine coexisting with an in-flight brownfield codebase. (Deep code-level
verification is explicitly another reviewer's lens per the task brief; this pass only checks the
spine doesn't contradict itself about it, and it doesn't.)

## 6. PRD/epics scope coverage

MVP in-scope list (PRD §6.1) maps cleanly onto the AD set; MVP out-of-scope list (§6.2) maps cleanly
onto AD-19 (tools/remote-agent surface) and the Deferred table (memory, project/folder activation,
external channels, multiple named Agents). OQ-19 through OQ-23 (the freshest rows, resolved
2026-09-09) are each visibly absorbed into specific ADs as detailed in §1 above. No PRD capability
found that the spine is silent on.

## 7. Inherited Invariants (parent-spine check)

Not applicable, and the spine is correct not to carry a section by this name. Per this project's own
`bmad-architecture` skill convention, "Inherited Invariants" is the mechanism an **epic-level** spine
uses to bind itself to a pre-existing **feature/initiative** spine one altitude up. This document *is*
the initiative-altitude spine for the whole agents workspace — there is no parent
`ARCHITECTURE-SPINE.md` above it in this tree (sibling modules Tenants/EventStore/Conversations/
Parties/FrontComposer each have their own independent module-altitude spines that Agents *consumes as
dependencies* via the External Dependency Register, not *inherits invariants from*). Flagging this
explicitly rather than silently passing, as instructed.

## 8. Structural dimensions the altitude owns — decided, deferred, or open?

Checked deployment & environments, infra/provider strategy, operations/recovery, capacity, secrets
custody, data governance, identity, and UI/accessibility as the candidate "whole dimension" risks:

- **Deployment & environments / infra strategy:** decided, not silent — AD-16 explicitly assigns this
  to the platform host (`EXT-HOST-1`) and forbids Agents from owning AppHost/Aspire/ServiceDefaults,
  with a guard-test mechanism. Worth flagging as context (not a spine defect): per
  `external-dependency-register.md`, `EXT-HOST-1` is only `Committed` — "at `a66cdf34` the artifact is
  an empty file-based AppHost... without Dapr, so composition remains future work under Story 5.6" —
  so the *decision* (platform owns it) is made, but the *dependency* is not yet `Available`. That is
  correctly tracked by the register mechanism the spine itself points to, so it is not a spine gap,
  just worth the reader not mistaking "AD-16 exists" for "the host is built."
- **Operations (recovery, capacity, secrets, audit):** each has a dedicated AD (AD-23, AD-24, AD-9/14,
  AD-22) with register-bound numeric contracts. Decided.
- **Identity/auth integration:** decided in shape (AD-30) but the caller-`PartyId`-at-ingress mechanism
  is itself tagged `[ASSUMPTION ARCH-A-2]`, pending "Platform identity contract confirms the
  subject-to-Party mapping." This is tracked, owned, and retirement-conditioned, so it counts as
  "decided with a named open assumption," not "silent."

No wholly-silent structural dimension found.

## 9. Placeholders, template scaffolding, unresolved `[ASSUMPTION]` tags

No literal leftover scaffolding: a search for `TBD`, `TODO`, `FIXME`, `<insert...>`, `lorem ipsum`
found zero template artifacts (the one "placeholders" hit is a substantive usage inside AD-17's prose
about evidence quality, not a leftover marker).

**Finding (LOW/INFO) — volume and weight of live `[ASSUMPTION]` tags in a `status: final` document.**
The spine carries 16 distinct inline `[ASSUMPTION ...]` citations (PRD-owned A-3, A-4, A-5, A-6, A-7,
A-8, A-9, A-10, A-11, A-12, A-14, A-17, plus the spine's own ARCH-A-1, ARCH-A-2, ARCH-A-4, ARCH-A-7),
each backed by a concrete interim rule, an owner, and a retirement condition, and each is a declared
`RQ-1` `UnretiredAssumption` blocker (AD-17) rather than a silent gap — so the *mechanism* is sound and
this is not scored as a defect. It is worth a reviewer noting, though, that not all 16 carry equal
weight: `ARCH-A-2` (caller `PartyId` resolution through Parties/Tenants at ingress) is load-bearing
across *every* command via AD-30, unlike, say, `A-8`'s 24-hour `Indeterminate` hold default, which only
affects one budget-reconciliation edge case. A document titled "final" that still has its single most
structurally load-bearing authorization step resting on an unconfirmed platform identity contract is
fine as *tracked* risk, but it is not the same as "final" in the sense of "nothing here would change
if the assumption resolved differently" — worth Product/Platform prioritizing ARCH-A-2's retirement
ahead of the others given its blast radius.

**Finding (LOW) — no other issues found in structural-seed / class-diagram / naming-table
cross-consistency.** The 14-aggregate inventory in AD-2 matches the `Hexalith.Agents/` folder list in
the Structural Seed and the class diagram exactly (same 14 entities, consistent sub-entities
`ProposedAgentReply`/`ProposalVersion`/`ProviderAttempt` correctly *not* listed as separate aggregates).
Listed for completeness since this is exactly the kind of drift a stale "final" document accumulates;
none found.

## Summary table

| # | Checklist item | Result |
|---|---|---|
| 1 | Fixes real divergence points one level down | Mostly yes; **HIGH**: AD-30 principal vocabulary contradicted in epics.md Story 5.4 |
| 2 | Binds/Prevents/Rule teeth | Mostly yes; **MEDIUM**: `SecurityEventLog` "rate-bounded" has no defined mechanism |
| 3 | Deferred section materiality | Clean |
| 4 | Tech/framework internal coherence | Clean |
| 5 | Brownfield ratification (surface-level) | Clean — gaps are named, not hidden |
| 6 | PRD/epics scope coverage (skim-level) | Clean |
| 7 | Inherited Invariants | N/A — this is the top-of-tree initiative spine |
| 8 | Structural dimensions decided/deferred/open | Clean — deployment envelope explicitly decided via AD-16 |
| 9 | Placeholders / stale `[ASSUMPTION]` tags | Clean mechanism; **LOW/INFO**: ARCH-A-2 is the highest-priority assumption to retire given its blast radius across AD-30 |

**Overall verdict: PASS WITH FINDINGS — 0 critical, 1 high, 2 medium (one folded into finding 2's
writeup as a single medium plus the citation nit as a separate low), 3 low/info.** The high finding
(AD-30 vocabulary drift into epics.md) should be corrected before Story 5.4 is implemented, since it
is precisely the two-teams-diverge scenario this rubric exists to catch.
