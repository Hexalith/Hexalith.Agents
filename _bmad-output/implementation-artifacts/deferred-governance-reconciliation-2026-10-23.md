# Deferred governance reconciliation — 2026-10-23

This record closes five governance planning entries using existing governing decisions. It synchronizes two downstream authority clauses with PRD FR-24 and clarifies the inspection UX and Story 8.8 contract handoff. It establishes no runtime delivery, custody behavior, dependency acceptance or production qualification.

The source baseline is `da7b1075bedae3b3f821888e410a6e8274e8b2a8` on `main`. Line numbers identify that revision, while named sections and relative links identify maintained documents. The original ledger summaries and evidence are retained.

## Facilitator block-clearing authority

The [Architecture Spine, AD-7](../planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md) (baseline line 332) and [Experience Spine, Operational status rules](../planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#operational-status-rules) (lines 470–472) distinguish three authorities:

- An Administrator-set block is cleared by the current Tenant Agent Administrator; a Facilitator cannot clear it.
- A Facilitator-set block is cleared by the same setting Party only while that Party still holds the Conversation Facilitator role, or by the current Tenant Agent Administrator.
- A system-set `ExternallyRemoved` record has no setting Party and may be cleared by the current Administrator or any current Facilitator.

The second branch answers the ledger's same-Party-versus-any-Facilitator question. The current role is rechecked at clear time. The UX's declared Conversations seam dependency and A-9 qualification remain in force; this is no claim that Facilitator controls ship today.

## Inspection without an established eligible Administrator

The [PRD, FR-24](../planning-artifacts/prds/prd-agents-2026-06-23/prd.md) (baseline lines 626–633) and [Architecture Spine, AD-22 and AD-30](../planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md) (lines 506 and 602) already provide a fail-closed answer rather than an automatic fallback:

- A current Tenant Agent Administrator outside the computed historical subject set is the normal second party. Turnover can make this branch eligible.
- If the established current Administrator is in that subject set, the second party is instead the Platform Operator or a second Compliance Inspector, outside the subject set and distinct from the Inspector.
- Missing, stale, ambiguous, overlapping or unverifiable tagged authority evidence blocks inspection. Inability to establish an eligible current Administrator does not itself authorize substitution of another role. A proposed second party who is the Inspector is a typed refusal, even if they carry a different role or principal kind; separation uses stable `AuthenticatedHumanActorId`.

The review found two downstream discrepancies against that PRD rule: baseline AD-22 line 506 selected alternatives only for membership as a historical TAA, omitting membership in another subject role; baseline [Story 8.8](../planning-artifacts/epics.md#story-88-inspect-audit-evidence-under-durable-compliance-governance) line 3355 used the broader phrase ‘when eligible and otherwise’, which could mistake any authority failure for an alternate grant. Both now use the PRD's full computed-subject-set condition. Thus a current Administrator who was an in-scope caller can select the alternate branch, while Inspector-self identity alone or missing authority cannot. This synchronizes derived planning text without deciding a broader fallback.

Thus no unique eligible authority means the inspection cannot proceed through that unresolved branch. A known Administrator-in-subject branch and the independent prior-Platform-approval requirement for wide inspections remain governed as above; uncertainty does not create either grant. The [Experience Spine, Audit evidence rules](../planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#audit-evidence-rules) now makes the existing eligibility/refusal distinction explicit and renders the server-computed result. A future broader fallback would require a Product/Governance decision.

## Blocked and unavailable shares

The [PRD, FR-28](../planning-artifacts/prds/prd-agents-2026-06-23/prd.md) (baseline lines 749–752) and [Architecture Spine, AD-12 — Trigger calculation](../planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md) (line 394) use distinct calling Parties for both numerator and denominator. The numerator contains Parties with at least one relevant blocked or unavailable call; the denominator contains Parties that attempted a call in the same rolling seven-day window. Repeated calls, reasons and Conversations do not multiply one Party in an aggregate share. Reason breakdowns remain available and are not summed to compute that share.

For example, one Party with two blocked reasons among four calling Parties contributes 1/4, not 2/4. A Party may belong to both separately calculated blocked and unavailable shares. Existing exclusions, the distinct-Party convening floor, minimum sample, acknowledgement exceptions and `InsufficientEvidence` remain binding. This closes the formula-definition defect without qualifying a metrics implementation or changing trigger thresholds.

## Restrictive partial hold release

The [epics, Story 8.1 release and recovery criteria](../planning-artifacts/epics.md#story-81-retain-sensitive-content-and-apply-legal-holds) (baseline lines 2858–2867) and [Architecture Spine, AD-2 and AD-22](../planning-artifacts/architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md) (lines 245, 516, 518 and 520) settle the partial-unpin safety question with staged release and a continuously restrictive guard:

1. A durable human release decision and distinct authorized second-party approval precede unpinning the frozen DEK/artifact set.
2. Every exact unpin and fence-release receipt must arrive before the original hold contender may be released at the tenant scope guard. Overlapping deletion stays blocked until the exact guard-release receipt is mirrored.
3. A partial failure, crash or lost acknowledgement resumes the same decision revision, frozen set/token and, where artifacts overlap, the recorded lifecycle decision version and exact store target. Recovery uses exact outcome lookup and idempotent retries; unavailable members remain restrictive and pending. A later policy successor cannot retarget or strand that release.
4. Physical expiry, purge, key destruction, provider TTL, backup expiry and restore cleanup require current linearizable fence/store authorization. Unavailable control evidence retains bytes and keys, so partially unpinned members cannot bypass the still-restrictive hold transition.

The safety mechanism is serialized release and retry convergence, rather than an invented atomic multi-key unpin or re-pin compensation. Hold-prepare cancellation remains separately gated by `OD-HOLD-PREPARE-CANCELLATION-1`; a prepare failure is not an approved release. The artifact branch retains `OD-EXPORT-LIFECYCLE-1` and exact provider qualification requirements. These are planning contracts, not demonstrated custodian guarantees.

## Alternate inspection approvers in UX

The [Experience Spine, Audit evidence rules](../planning-artifacts/ux-designs/ux-agents-2026-06-23/EXPERIENCE.md#audit-evidence-rules) (baseline lines 534–539) already names the Platform Operator and second Compliance Inspector alternatives, the subject-set exclusion, 30-day anti-collusion rule, seven-day narrow-scope post-hoc window, and mandatory prior Platform approval for wide/case scopes. The targeted clarification now states the normal current-Administrator branch and the Administrator-in-subject alternatives together, instead of describing alternatives only inside the post-hoc paragraph. The existing `/agents/audit` change-and-inspection list and its detail now explicitly provide viewer-scoped discovery, **Approve inspection** and **Record inspection review** through the existing `AuditInspection` confirmation/pending flow. The Contracts that must grow table and Story 8.8 acceptance criteria assign the missing public list/result and approval/review handoff to Story 8.8, including review state/deadline, authority basis, server-evaluated per-viewer verdict, safe refusal/unavailable reasons and expected revision. Inspector reviewers receive only requests they may oversee and no protected content; missing contracts render deferred. An eligible late reviewer can still record review after seven days, clearing the current unreviewed signal while retaining the missed-window finding in immutable history and granting no new content read. The browser infers neither authority nor review timing from identifiers or its own clock.

This closes the UX-branch documentation gap. It preserves A-19/A-23, the persistent unreviewed-inspection signal and all prior-approval restrictions. It adds no new runtime role, policy identifier, automatic fallback or inspection grant.

## Verification scope

The [build spec](spec-five-deferred-governance-planning-items.md) records independent review and document checks. This batch changes exactly five ledger statuses, retains original deferral history and links each resolution here. No application tests or production qualification are claimed. Runtime code, sprint status, assumption decisions, dependency acceptance and submodule revisions are outside this reconciliation.
