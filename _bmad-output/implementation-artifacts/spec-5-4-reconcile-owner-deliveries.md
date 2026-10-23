---
title: '5.4 Reconcile owner deliveries before bounded qualification'
type: 'chore'
created: '2026-10-22'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
qualification_blocked: true
context:
  - '_bmad-output/planning-artifacts/external-dependency-register.md'
  - '_bmad-output/implementation-artifacts/story-5-4-dependency-reconciliation-2026-10-18.md'
  - '_bmad-output/implementation-artifacts/story-5-4-dependency-reconciliation-2026-10-22.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 5.4 remains incomplete because its four accepted owner records are unavailable. New source deliveries and acceptance packets must be reconciled against the complete contracts before they can replace the accepted targets.

**Approach:** Inventory every new owner packet and canonical source target. Prepare complete replacement records with executable exact-target commands, actual installed profile/configuration/plan/trust hashes and explicit integration dates. Present only complete commitments for owner acceptance. After acceptance and prerequisite installation, run the bounded qualification sequence and resume Story 5.4 only when its relevant records are Available.

## Boundaries & Constraints

**Always:** Preserve all nine commitment fields and full contract extensions. Preserve Branch B, 365-day history, host 192.168.1.30, envelope bounds 300/30/600/86400/604800, independent decision authority and the adopted Dapr-only custody architecture. Follow OQ-5.4-1: one attempt per accepted target set; shared Platform target; immutable profile/configuration/plan/anchor bindings; retained attempt directories, recovery journals and original outcomes. Establish Available only from passing required exact-target Level 4 evidence; Level 5 remains assigned to consuming qualification/RQ-1. Treat missing installations and owner implementations as delivery blockers, not intent questions.

**Never:** Execute a consuming seam before Available, run an unaccepted replacement command, fabricate public trust or installed references, accept the hash of a null-anchor map as trust, substitute component tests for persisted qualification, overwrite journals, or reset an attempt. The Conversations qualification receiver must be independently authenticated and contain no Agents package/runtime. Architecture adoption does not authorize new physical experiments or resource spending.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Complete replacement | Canonical clean target, full contract, installed hashes, executable command, explicit date | Present the exact replacement for acceptance; retain Committed until qualification passes | Any changed accepted byte invalidates acceptance |
| Incomplete owner packet | Missing target, authority, installed hash, date or executable qualification | Record exact blockers; preserve the accepted register | Do not submit incomplete replacements or consume a cohort attempt |
| Qualification | Accepted prerequisites and exact installed target set | Run phase 0 protection, phase 1 secrets, phase 2 host, phase 3 mutual cases | Any failure leaves both Platform records non-Available; retain originals |
| Consumer continuation | All relevant owner records Available at installed targets | Resume bmad-build for Story 5.4 | Otherwise retain DependencyNotAvailable |

</frozen-after-approval>

## Code Map

- `external-dependency-register.md` in planning artifacts — authoritative contracts and OQ-5.4-1; proposal documents do not amend it.
- `story-5-4-dependency-reconciliation-2026-10-22.md` — candidate disposition, dates, exact command references and ordered completion requirements.
- `tests/story-5-4-reconciliation-2026-10-22/` — durable owner reports, source/hash inventory and static verification; no runtime evidence claim.
- Sibling Platform `eng/ext-{secrets,host}-1/prepare_acceptance.py`, `qualify.py`, `verify_full.py` — reuse accepted packet semantics; the joint Live runner remains absent.
- Sibling Conversations owner packet and Parties `eng/verify-ext-parties-1.ps1` — incomplete installed qualification; preserve receiver separation and complete P-01–P-10 scope.
- Sibling EventStore `docs/implementation/ext-protection-1-owner-acceptance.md` — G1–G7 and proposed date; preserve retained negative restore evidence.
- `spec-5-4-prove-trusted-principal-tenant-party-and-approver-readiness.md` and `eng/verify-story-5.4.ps1` — existing approved implementation; do not mark complete or start consumer qualification during reconciliation.

## Tasks & Acceptance

**Execution:**
- [ ] `story-5-4-dependency-reconciliation-2026-10-22.md` and supporting reports — reconcile all owner packets and make missing prerequisites reviewable.
- [ ] `_bmad-output/planning-artifacts/external-dependency-register.md` — apply only complete explicitly accepted replacements; retain historical acceptance evidence.
- [ ] Accepted owner checkout/configuration locations — install exact targets and independently enrolled inputs after acceptance; preserve unrelated owner work.
- [ ] Accepted evidence/attempt locations — execute the permitted sequence once and retain commands, persisted outcomes and recovery journals; advance availability only on complete passes.
- [ ] Existing Story 5.4 spec — resume its build workflow after exact-target availability, then complete its own acceptance and review.

**Acceptance Criteria:**
- Given new packets, when reconciled, then every proposed commitment has verifiable canonical targets, executable commands, actual installed hashes and explicit dates, or a precise blocked disposition with no acceptance claim.
- Given incomplete prerequisites, when this run ends, then register statuses, prior attempts, recovery journals and consuming runtime remain unchanged.
- Given a passing complete accepted owner sequence, when records advance, then installed targets and required persisted evidence match every accepted binding; Story 5.4 completion still requires its own verification.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Verification

Validate packet hashes, canonical ancestry, raw versus canonical hash semantics, exact command text, unchanged register and historical attempt bytes. Inspect the full required-field blocker lists. These are static reconciliation checks, not owner qualification. Do not run application tests for documentation-only preparation.
