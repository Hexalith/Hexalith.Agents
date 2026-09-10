# PRD Update Report — Hexalith Agents (third update, 2026-09-09)

- **PRD:** `prd.md` (1000 → 1081 lines after polish; `status: final`, `updated: 2026-09-09`)
- **Change signal:** `validation-report.md` (validate run 2026-09-09T11:01Z, grade Poor: 8 critical / 24 high / 46 medium / 40 low; archived as `validation-report-2026-09-09-3.md`)
- **Run mode:** autonomous (no user present). Scope was inferred from the two earlier 2026-09-09 updates and every product decision taken without the user is logged in `.memlog.md` as an `override` or `decision` entry dated this run; all are reversible on review.
- **Reconciliation of the signal:** `reconcile-validation-2026-09-09-3.md` — 124 findings: 80 applied, 5 partial, 3 diverged, 14 out of scope (Spine, registers, epics, proposal), 22 code-side. Cross-document facts re-verified on disk in `reconcile-cross-artifact-2026-09-09-3.md`.
- **Reviewer gate (post-update):** `review-rubric.md` (Good on its own page: 0C/1H/6M/6L), `review-adversarial-general.md` (1C/8H/14M/10L; 28 of 38 prior findings resolved), `review-consistency.md` (internal 0C/0H/4M/6L; cross-artifact 0C/4H/8M/6L, 13 prior still open on the Spine/register/epics side), `review-implementation-drift.md` (PRD-side 0C/3H/2M/2L; code-side 0C/0H/4M/2L). Every gate critical and high was applied in a second batch; the signal's reviewer files are archived with the `-2026-09-09-validate-3` suffix.

## What changed

**Cross-document claims made pointer-only.** §0 and §8 no longer restate register statuses (`EXT-HOST-1` is no longer said to be `Committed`; the register's Current Blocking Summary is the authority). §8.1 cites the Architecture Spine by path, `updated` date, and `architecture_assumption_index_version` with no row count, lists the known Spine divergences by AD number with a date, and names `reconcile-validation-2026-09-09-3.md` as the authority for that list. The seam-2 existence read and seam-4 event feed are no longer "pending register extensions". The open Product approval for Story 5.3 / `EXT-PROVIDER-1` is stated as a `ready-for-dev` blocker.

**Launch gate.** A late `ARCH-A` row now blocks `RQ-1` unless the Release PM records a `DeferredAssumption` deferral with Product acceptance and a revisit date within 30 days; Security-owned rows are never deferrable; a passed Architecture target date escalates to Product. "Release PM" has a glossary entry and FR-33 rows. `TriggerReviewOverdue` blocks production-like enablement and new Agent activation. NFR-9 gates on p95 at n ≥ 30 (p99 only at n ≥ 300).

**Conversation Agent State (FR-2).** Presence in Conversations re-admits from `ExternallyRemoved`, gated by A-22 (Facilitator-only adds). A clear issues its own mirror add; once confirmed, a later absence is an external removal. Removal is detected only on a roster read at least as fresh as the last confirmed add. `MirrorRefused` is a terminal mirror outcome surfaced for the Tenant Agent Administrator.

**Proposal lifecycle (FR-18).** `PostingWindowElapsed` staleness bound for `Approved` and `PostingFailed`; human abandon from `Approved`; administrative retry age bound; durable `PostingPending` deadline evaluated on recovery; `Disabled`-Agent retry-clock pause; no `MessageId` lookup from `Approved`. Automatic mode posts through a system-approved posting record under the same rows, with a Facilitator or Administrator exit.

**Safety and governance.** Human edits are scanned at edit time (`RestrictedContent` version field). Approver sources must resolve to person Parties. "More restrictive of two policies" replaced by "passes every applicable policy version". FR-31 narrowed to first-person impersonation and unrecorded decisions. Compliance inspections aggregate per tenant with a review threshold (A-23). A data-handling change is "tightening" only when the Platform Operator declares it with the field diff; otherwise Provider use blocks until the Tenant Agent Administrator accepts.

**FR-34 attestation.** Security qualifies the engine build and records its signed identity in `EXT-SECRETS-1`; the runtime verifies the loaded engine against it hourly (A-24); the seal/unseal/erase canary is a liveness check.

**Brownfield corrections.** Hexalith.Parties has no AI Party type: new `EXT-PARTIES-1` and A-27, with the provisioned Organization Party as `hexa`'s immutable, id-verified identity in the interim. The shipped `ContextUnavailable` keeps its meaning; `ContextReadUnavailable` is the unavailable-call class. FR-23's register names the nine state-duplicating `Proposal*` statuses and the four zero-valued success members.

**FR-8** reordered so free local rejections precede the classifier and the cost reservation (ten steps; every citation renumbered). FR-21's register gate rules moved to §8.

New IDs: OQ-31; A-22..A-28; `EXT-PARTIES-1`; `EXT-CONV-AI-1` seam 7 (retraction). Provisional Product due date 2026-09-30 (A-26) for the FR-33 confirmations and OQ-23.

## Deliberate divergences from the reviewers' suggested fixes

- `ExternallyRemoved` is deferred to FR-8 step 10 rather than rejected at step 3, so presence re-admission can fire.
- `MirrorRefused` is a per-Conversation SM-C4 count, not an unavailable-call signal.
- Release PM is a distinct party with no runtime permission, not an alias of the Release Operator.
- `ContextUnavailable` stays a blocked-call reason; the unavailable class is the additive `ContextReadUnavailable`.
- Automatic calls end `Posted` or `PostingFailed` with no new `AgentInteractionStatus` member.
- FR-34's wrapper test is a Security qualification test, not a runtime mechanism.

Rationale for each is in `addendum.md`, "Options Considered — Third 2026-09-09 Update".

## Deferred (owner Product, revisit at the next validate run)

Adversarial M-2, M-3, M-4, M-8, M-10..M-14, L-3, L-4, L-6..L-10; rubric R-M-4..R-M-6, R-L-1, R-L-5; consistency CX-18 optional dating. Full list with locations in `reconcile-validation-2026-09-09-3.md`, "Reviewer gate (third update)".

## For correct-course (fix targets another artifact)

See `reconcile-validation-2026-09-09-3.md`, sections "For correct-course" and "Correct-course additions from this gate". Headlines:

- **`ARCHITECTURE-SPINE.md`** — AD-13 Confirmation-only Eligible Approver resolution and the ten-step FR-8 order; AD-5 human abandon, `PostingWindowElapsed`, `PostingPending` deadline, automatic posting record; AD-7 A-22 gate, mirror-on-clear, roster freshness; AD-12 distinct-Party share and 3-Party floor; AD-14/`EXT-SECRETS-1` signed engine identity; AD-17 vocabulary; AD-20 "every applicable version"; AD-22 inspection aggregation; `ARCH-A-11` retirement on FR-3; literal target dates in the `ARCH-A` table.
- **`external-dependency-register.md`** — new `EXT-PARTIES-1`; `EXT-CONV-AI-1` seam 7 (retraction), seam 1 roster version and typed already-absent answer, A-22 add restriction; `EXT-SECRETS-1` engine identity value; `EXT-CONV-UI-1` fourth artifact kind (status entry); Provider Readiness Contract data-handling reason codes; Story 5.3 Branch A/B decision.
- **`launch-readiness-register.md`** — `DeferredAssumption` and `SuspensionReviewOverdue` vocabulary; record kinds for the `RQ-1` scheduling event, deferral acceptance, SM-4 containment review, engine qualification; Release PM rows; `PayloadProtectionUnavailable` in attestation terms; `LR-PARTY-IDENTITY` wording.
- **`epics.md`** — inventory rows FR4/FR5/FR12/FR22/FR25/FR27; `Draft`, `MembershipUnavailable`, `MembershipRejected`; data-handling stories; the new vocabulary; a provisioning story re-homing the link command's provisioning leg; 33-story count vs the proposal's 29.
- **Sprint change proposal** — `prd.md` must be listed under `amends_if_approved` for these edits to be scheduled.
- **Code (owner Agents Runtime Maintainer)** — the 22 prior drift findings (5 critical) remain open; plus the posting-record shape, `PostingPending` deadline, successor enums, signed-identity verification, person-only Approver rule, edit-time scan, `ContextReadUnavailable`, and the FR-8 gate checks.

## Artifacts

- `prd.md`, `addendum.md` (updated)
- `.memlog.md` (overrides, decisions, changes, events appended)
- `change-extract-validation-2026-09-09-3.md`, `reconcile-cross-artifact-2026-09-09-3.md`, `reconcile-validation-2026-09-09-3.md`
- `review-rubric.md`, `review-adversarial-general.md`, `review-consistency.md`, `review-implementation-drift.md` (this run's gate); `review-*-2026-09-09-validate-3.md` and `validation-report-2026-09-09-3.{md,html}` (the signal, archived)
- `polish-structure-prd-2026-09-09-3.md`, `polish-prose-prd-2026-09-09-3.md`, `polish-structure-addendum-2026-09-09-3.md`, `polish-prose-addendum-2026-09-09-3.md`
