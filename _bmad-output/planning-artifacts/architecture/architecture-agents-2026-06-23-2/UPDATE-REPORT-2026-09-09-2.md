# Architecture Spine Update Report - Hexalith Agents - 2026-09-09 (round 4)

- **Spine:** `ARCHITECTURE-SPINE.md` (status `final`, updated 2026-09-09, AD-1..AD-31; `architecture_assumption_index_version` 2 -> 3)
- **Trigger:** `VALIDATION-REPORT-2026-09-09-3.md` (round-3 gate, v6 lenses) recommended rolling its findings into an Update pass.
- **Path:** autonomous application, same convention as prior rounds; product-level calls carry `[ASSUMPTION]` keys (new: `ARCH-A-11`, `ARCH-A-12`).

## What changed

### Pass 1 - closed round-3's own findings

| Fix | Closes |
| --- | --- |
| C-1 verified already closed (no action) | round-3 C-1: stale `Deferred` table row |
| AD-7 block-clear authority rewritten (first pass) | round-3 C-2 |
| `launch-readiness-register.md` `LR-AUDIT-PROTECTION-DELETION` made symmetric for `DeletionRequest` | round-3 C-3 |
| AD-12 re-read enumeration now names Party state | round-3 H-1 |
| AD-12 disabled-Agent clause now states `PostingFailed` suspension (`ARCH-A-11`); AD-5 `PausedDuration` formula generalized | round-3 H-2 |
| AD-30 Platform freshness source reworded (first pass) | round-3 H-3 |
| Stack `Hexalith.EventStore` gitlink `1b6f08d4` -> `e302432c`; Consistency Conventions fingerprint row -> HMAC-SHA-256; AD-22 given a DigestKey rotation bound (first pass, `ARCH-A-12`); `AuditInspection` classDiagram `Mode` -> `Scope` | round-3 Medium tier |

### Pass 2 - reviewer gate (v7) on the pass-1 spine, then fixed what it found

Ran rubric, verified-current, adversarial-divergence, and security/data-integrity as four parallel subagents against the pass-1 spine. All independently re-confirmed every pass-1 fix as genuinely closed, then surfaced new problems - two of them in text pass 1 itself had just written:

| Fix | Closes |
| --- | --- |
| AD-30's Platform source corrected to Hexalith.Tenants' actual `global-administrators` domain/projection (pass 1's own "system tenant's Tenants-projection" wording was itself a domain-model conflation, per `references/Hexalith.Tenants/_bmad-output/project-context.md`) | adversarial CRITICAL |
| AD-7's clearing clause rewritten to state explicitly which side is role-bound (TAA) and which is identity-bound (Facilitator) | adversarial CRITICAL |
| AD-22's DigestKey rule redesigned from "never rotate while unerased content exists" (practically unsatisfiable for a live tenant) to a versioned-key model matching `ContentSafetyPolicy`/pricing elsewhere in the spine, and no longer claimed as one of AD-12's nine lock-bearing families; `ARCH-A-12` reworded to match | security CRITICAL; rubric HIGH; adversarial HIGH (same root cause, three lenses) |
| AD-13 given its own explicit fingerprint algorithm (HMAC-SHA-256 under `DigestKey`), closing the gap where only the Consistency Conventions summary stated it | adversarial HIGH |
| AD-12's dual-cause `PausedDuration` given an explicit union/no-double-count merge rule | security + rubric MEDIUM (same finding) |
| Stack `.NET SDK` row and `ARCH-A-4` reworded: the row's own escalation condition ("on any security advisory against `10.0.3xx`") has already fired - `10.0.12` (2026-09-08) fixed `CVE-2026-69522` (CVSS 8.8 RCE) and others, unreachable by this repo's capped `10.0.303` pin; target moved from Story 5.6 to immediate for the SDK pin specifically | verified-current HIGH |
| Stack `MediatR` row corrected: "not referenced by Agents projects" is true only at the direct-reference level - AD-16's platform EventStore SDK host Agents runs on is itself built on MediatR, so the commercial-license exposure is already live | verified-current MEDIUM |
| AD-22 and AD-7 `Prevents` lines extended; Consistency Conventions `Identity` row gained `InspectionId` | rubric LOW x3 |

**Checked and not reproduced:** adversarial v7 also flagged AD-5 as citing "the AD-13 seam-2 existence read"; the live text reads "the AD-6 `EXT-CONV-AI-1` seam-2 existence read" (one match in the whole file) - false positive, not applied.

**Deliberately deferred (not applied this round):** a Medium carried from round 3 and re-flagged by rubric v7 - AD-10's `EntryMissing` carve-out adds an "in-flight interaction's" qualifier AD-2's unqualified "previously-snapshotted" wording doesn't have. Logged as an open question; revisit before Story 5.3/5.5 finalize.

## Reviewer gate (v7)

| Lens | Verdict before fixes | Critical / High / Medium / Low | Applied |
| --- | --- | --- | --- |
| Rubric walker | PASS WITH FINDINGS | 0 / 1 / 1 / 3 | all |
| Verified-current | PASS WITH FINDINGS | 0 / 1 / 1 / 1 (low unchanged, no action) | high + medium |
| Adversarial divergence | FAIL | 2 / 3 / 1 / 0 | 2 critical + 2 of 3 high (1 false positive) + 1 medium (shared with security) |
| Security / data integrity | FAIL | 1 / 0 / 1 / 0 | all |

Deterministic `lint_spine.py`: 10 findings, all literal `TBD` markers in the Architecture Assumptions table's `TargetRetirementDate` column. **This is a pre-existing discrepancy, not a regression**: re-running the linter against the last-committed (pre-round-4) spine also returns 8 TBD findings, even though every prior validation report claimed "lint: 0 findings." Not resolved here - these `TBD` cells are legitimate open items with named owners and revisit conditions per the Assumption Index's own governing text (`ARCH-A-INDEX-3`: "Architecture-owned open rows remain `TBD` until their co-owners approve a date"), and fabricating dates to silence the linter would violate the no-invented-content rule. Flagged for the linter's own maintainers or a future round to reconcile (e.g. exempt the `TargetRetirementDate` column, or the whole Architecture Assumptions table, from the placeholder scan).

## Implementation follow-ups this update creates

1. Bump the root `.NET SDK` pin past the capped `10.0.3xx` band immediately (`ARCH-A-4` escalation has fired: `CVE-2026-69522`, CVSS 8.8 RCE, unpatched at the current pin) - independent of Story 5.6's broader test-stack catalog alignment.
2. Reconcile AD-10's `EntryMissing` carve-out wording against AD-2's unqualified "previously-snapshotted" language before Story 5.3/5.5 finalize (open question, not yet a spine defect blocking anything today).
3. `lint_spine.py`'s placeholder scan and this project's validation-report convention disagree on whether the Architecture Assumptions table's `TBD` cells count as findings; reconcile the tooling or the reporting convention in a future round.
