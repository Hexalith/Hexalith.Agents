---
title: '5.4 Reconcile owner deliveries before bounded qualification'
type: 'chore'
created: '2026-10-22'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
qualification_blocked: true
last_reconciled: '2026-10-25'
context:
  - '_bmad-output/planning-artifacts/external-dependency-register.md'
  - '_bmad-output/implementation-artifacts/story-5-4-dependency-reconciliation-2026-10-25.md'
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

## EXT-PARTIES-1 execution direction — 2026-10-25

The user applied the [custody feasibility recommendation](../../../platform/docs/implementation/ext-parties-1-custody-direction-2026-10-25.md): the custom Luna/FM path and further production actor-history backend implementation are stopped under the unchanged contract. No complete non-AWS backend is verified. OpenBao remains conditional on an explicit guarantee amendment; it supplies no accepted replacement or Available evidence. Preserve the complete EXT-PARTIES-1 record, October 25 reconciliation and original attempts. Its accepted October 8 date stays historical/missed; no replacement date is accepted and the proposed December 14 target is not an active commitment. No consuming Story 5.4 execution or qualification follows from this direction. Other owner work is unaffected.

## Code Map

- `external-dependency-register.md` in planning artifacts — complete contracts and OQ-5.4-1; preserve accepted bytes.
- `story-5-4-dependency-reconciliation-2026-10-25.md` and `tests/story-5-4-reconciliation-2026-10-25/` — all fields, commands, source/installed hashes, blockers and preservation evidence. Earlier reports remain historical.
- Sibling Platform `eng/ext-{secrets,host}-1/` — packet/hash semantics; joint Live runner and installations remain missing despite October 25 component delivery.
- Sibling Parties `eng/verify-ext-parties-1.ps1` — uncommitted coordination, null date, refusal-only Live. Conversations October 22 packet remains NotSubmitted with absent installed manifest/trust/receiver.
- Sibling EventStore `docs/implementation/ext-protection-1-delivery-2026-10-25.json` — BlockedNotSubmitted, G1–G7 open, proposed 2026-11-02 date.
- `spec-5-4-prove-trusted-principal-tenant-party-and-approver-readiness.md` and `eng/verify-story-5.4.ps1` — approved consumer work; resume after exact-target availability.

## Tasks & Acceptance

**Execution:**
- [x] `story-5-4-dependency-reconciliation-2026-10-25.md` and reports — reconcile current packets; zero complete replacements.
- [ ] `_bmad-output/planning-artifacts/external-dependency-register.md` — apply only complete explicitly accepted replacements; preserve historical acceptance.
- [ ] Owner checkout/configuration paths in the reports — install accepted exact targets and independently enrolled inputs.
- [ ] Accepted attempt paths in the reports — run the ordered sequence once; retain commands, persisted outcomes and journals; advance only on complete passes.
- [ ] Existing Story 5.4 spec — resume its build after availability; finish production bindings, signed replay, durable denial recovery, persisted acceptance and full review.

**Acceptance Criteria:**
- Given new packets, when reconciled, then every commitment has canonical targets, executable commands, actual installed hashes and explicit dates, or precise blockers without acceptance.
- Given incomplete prerequisites, when this run ends, then accepted records, original attempts/journals and consuming runtime remain unchanged.
- Given complete accepted passing qualification, when availability advances, then installed targets and persisted evidence match every binding; Story 5.4 still needs its own verification.

## Implementation Notes

2026-10-25: Static reconciliation completed. No intent gap; missing owner deliveries block readiness. Footprint: this spec and new reports. Irreversible qualification stays conditional on acceptance and installation. No runtime, installation, owner command or attempt ran. Retain draft and blocked status.

## Spec Change Log

## Review Triage Log

## Verification

Compare packet/raw/canonical hashes, exact commands, local canonical ancestry, exhaustive missing-field lists and original evidence bytes. Preserve register, sprint/story gates and frozen intent. Reports and verification receipt distinguish static inspection from qualification; no application tests for documentation-only work.
