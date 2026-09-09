# PRD Update Report — Hexalith Agents — 2026-09-09

- **PRD:** `prd.md` (status `final`, updated 2026-09-09)
- **Change signal:** validation report of 2026-09-08 (grade Poor; 6 critical, 15 high), applied at critical + high depth
- **Product decision recorded:** this run is the separately approved change that reopened the success-metric freeze (SM-1..SM-6, OQ-11, OQ-19)

## What changed

All 21 critical and high findings were applied, plus 17 co-located medium and low findings. Reconciliation against the extract: 31 fully addressed, 4 deliberately diverged from the reviewer's wording (H2 status label, H13 transitional membership clause, H14 blocked-mode naming, H15 outcome name), 3 partials then closed. Details in `reconcile-validation-2026-09-09.md`.

Structural additions:

- **FR-33** role matrix (Platform Operator, Tenant Agent Administrator, Approver, Conversation Participant, Compliance Inspector, Release Operator), now opening §4.7.
- **§8.1 Assumptions Index**, A-1 through A-17, with every inline `[ASSUMPTION A-n]` key resolving to a row. Unretired Product, Architecture, or Governance assumptions block `RQ-1`.
- **§12 split** into pre-enablement gate metrics (SM-1, SM-4, SM-5, SM-6) and launch-health metrics (SM-3 primary as human-decision latency, new SM-7 governed-review band, SM-2 secondary), plus counter-metric SM-C5.
- **OQ-20..OQ-23**: per-tenant `hexa` with tenant-wide response mode; two-level audit inspection with scoped compliance inspection; `RQ-1` composition; deferred Automatic-mode retraction metric.
- **FR-8** nine-step ordered acceptance pipeline; **FR-7** Eligible Approver predicate at four moments; **FR-18** ten shipped proposal states with frozen expiry after approval; **FR-2** three-part membership step and Agents-owned removal block; **FR-27** verdict cache keyed by policy version and content hash; **FR-28** kill switch with owner, effect, and trigger thresholds.

Prior decisions revised this run (all logged in `.memlog.md`): OQ-3, OQ-6, OQ-9, OQ-11, OQ-14 (caller Approver source retired via the FR-23 register), OQ-16, OQ-18 (decision due 2026-10-15), OQ-19 (resolved).

Companion artifacts: `external-dependency-register.md` now names six `EXT-CONV-AI-1` seams and provenance rendering under `EXT-CONV-UI-1`; `addendum.md` records the eight rejected alternatives.

## Reviewer gate on the updated PRD

| Reviewer | Verdict | Critical | High | Disposition |
| --- | --- | --- | --- | --- |
| Rubric walker | Good; no broken dimension | 0 | 1 | Applied (external removal reversed by idempotent join) |
| Adversarial | Would not sign off as first written | 4 | 5 | All 9 applied; 13 mediums deferred |
| Implementation drift | PRD names shipped values correctly | 1 | 2 | Code defects, not PRD defects; recorded as implementation debt |

Files: `review-rubric.md`, `review-adversarial-general.md`, `review-implementation-drift.md`, `review-consistency-2026-09-09.md`. The 2026-09-08 reviewer files are archived with the `-2026-09-08-validate` suffix.

## Implementation debt surfaced by the drift review

Owner: Agents Runtime Maintainer. Revisit at the next drift review.

1. Readiness recording must reject `ReportingOnlyMonitoring` and `AcceptedLaunchRisk` and emit `ProhibitedCostControlPosture` (FR-28).
2. Agent configuration must reject `ContentSafetyFailureHandling.BlockWithAuditableOverride` and `ApproverPolicySourceKind.Caller` (FR-23 register).
3. `ProposalDetail.razor` and its test treat `PostingFailed` and `Posted` as terminal and offer no retry; the domain and the PRD do not (FR-18).
4. Additive members required: `AgentInteractionContextMode.Blocked`, `AgentGenerationOutcome.Indeterminate`, `NoEligibleApprover`, `RemovedInConversations`, `SourceConversationUnavailable`, `NotInvoked`, `UnretiredAssumption`, a `Membership` gate check, and the Safe Context Budget terms.

## Open items after this run

- 17 assumptions in §8.1, each with owner and retirement condition; A-3, A-9..A-13 need Product confirmation before the first tenant is enabled.
- OQ-15, OQ-18 (due 2026-10-15), OQ-23 remain deferred.
- Rubric mediums (FR-26 "more restrictive" decidability, FR-31 commitment category, Agent Call state enumeration, p99 at n=30) and adversarial mediums M-1..M-13 are deferred to the next validate run, owner Product.
- No phase-blockers for UX, architecture, or epics were identified.

## Next steps

- `bmad-ux` and `bmad-architecture` should re-read FR-2, FR-7, FR-8, FR-18, FR-24, FR-33, and §8 before their next update; the UX spines cite proposal states and Approver rules that changed.
- `bmad-create-epics-and-stories` or `bmad-correct-course` to fold the implementation debt above into stories.
- Run `bmad-prd validate` once Product has confirmed the A-9..A-13 rows.
