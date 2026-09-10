---
name: Hexalith Agents spine validation (round 5)
type: architecture-spine-validation
target: ARCHITECTURE-SPINE.md (updated 2026-09-09, status final, AD-1..AD-31; round-4 Update pass applied same day, uncommitted in the working tree)
repository_state: main @ c4121b0 (2026-09-09), working tree carries uncommitted edits to ARCHITECTURE-SPINE.md and .memlog.md from the round-4 Update pass
date: 2026-09-09
lint: 10 findings, all pre-existing literal-TBD placeholders in the Architecture Assumptions table's TargetRetirementDate column (known tooling/reporting discrepancy, not a regression — see Lint note)
lenses:
  - rubric walker v8 (good-spine checklist)
  - verified-current v8 (configured)
  - adversarial divergence v8 (configured)
  - brownfield drift v8 (ad hoc)
  - security / data integrity v8 (ad hoc)
---

# Spine Validation Report — 2026-09-09 (round 5)

## Gate verdict

**FAIL.** This round independently re-derived — rather than trusted — every claim the round-4 Update pass (and its own v7 gate) made about what it had closed. The headline result: the Spine's own binding PRD (§8.1, "The known Spine divergences as of 2026-09-09") lists seven AD-level divergences between the PRD and the Spine text. All seven were re-verified against the live Spine text and are **still present** — none were closed by the "Second-update" correction paragraphs already baked into AD-2, AD-7, AD-12, AD-14, AD-15, AD-17, and AD-30, despite four prior rubric-review rounds (v4, v6, v7, and now v8) touching this document today. Three of these are Critical because they describe runtime behavior that directly contradicts an FR-18/FR-8/FR-28 transition table or formula:

1. **AD-5 contradicts FR-18 on three points** — forbids a human abandon from `Approved` that FR-33/FR-18 explicitly grant, requires a `MessageId` lookup FR-18 says must be skipped for that exact case (and could deadlock a proposal in `Approved` forever when the lookup is unavailable), and has no `PostingWindowElapsed` staleness bound or `PostingPending` attempt deadline at all — both are normative FR-18 rows.
2. **AD-12's kill-switch trigger-review formula uses exactly the flat-20/`(Party,Conversation,reason)`-tuple rule the PRD's addendum records as explicitly rejected**, in favor of a distinct-calling-Party ratio with a 3-Party convening floor (FR-28/A-17) — a safety-critical miscalculation of when the mandatory kill-switch review convenes.
3. **AD-13 misorders FR-8's ten-step acceptance sequence and requires Eligible Approver resolution in Automatic Response Mode**, contradicting FR-8 (which scopes that step to Confirmation mode only) and FR-11 (which forbids resolving an Approver in Automatic mode at all) — as written, it would incorrectly reject legitimate Automatic-mode Agent Calls with zero configured Approvers.

Independently of the PRD-alignment failures, the adversarial lens found **two fresh, self-contained Critical contradictions inside the Spine's own text** that no prior round touched: AD-30 asserts a fixed `OnBehalfOfPartyId` for the `Workflow` principal in one sentence and a per-attempt-varying value (matching AD-21's own citation of AD-30) three sentences later — two compliant teams would charge rate/cost-cap consumption for a regeneration to different Parties. Separately, `ContextPolicyReference` is described in AD-11 as pointing at an independently versioned, published record — parallel to `ContentSafetyPolicy` — but AD-2's closed ("exactly") aggregate enumeration has no such aggregate, leaving its ownership, EventStore topology, and the context-budget margin's authorized mutator all unresolved.

The security lens also surfaced a new Critical: the round-4 fix that made `DigestKey` rotation actually work (redesigned from an unsatisfiable "never rotate while unerased content exists" bar to a versioned-key model) never states that a persisted digest carries the key version it was computed under — so after a rotation, nothing can deterministically tell which retained key version to re-verify a digest against. That is a direct, foreseeable consequence of the fix, not a leftover of the bug it replaced, and it undermines the long-horizon verifiability of the compliance audit trail the digest scheme exists to protect.

Brownfield drift is unchanged and clean news on its own terms: no source code has changed since the last pass (`git diff a191d24..HEAD -- src/ test/` is empty), the one previously-fixed tracking-integrity defect (`sprint-status.yaml` Story 5.2) is still correctly fixed, and every other open item is pre-existing, already-tracked implementation debt with a named backlog story — not new drift. Verified-current is also largely clean: the live CVE-2026-69522 exposure claim is independently confirmed true from primary sources, but the Stack table's `Hexalith.EventStore` gitlink had already drifted ~2h11m stale by the time of this review, and the "immediate" SDK remediation `ARCH-A-4` calls for has not actually landed in `global.json`.

| Lens | Verdict | Critical / High / Medium / Low | Review file |
| --- | --- | --- | --- |
| Rubric walker | **FAIL** | 3 / 4 / 4 / 0 | `reviews/review-2026-09-09-rubric-v8.md` |
| Verified-current | PASS WITH FINDINGS | 0 / 0 / 2 / 2 | `reviews/review-2026-09-09-verified-current-v8.md` |
| Adversarial divergence | PASS WITH FINDINGS | 2 / 2 / 1 / 1 | `reviews/review-2026-09-09-adversarial-divergence-v8.md` |
| Brownfield drift | FAILS TO RATIFY, fixable | 0 / 7‡ / 2 / 2 | `reviews/review-2026-09-09-brownfield-drift-v8.md` |
| Security / data integrity | PASS WITH FINDINGS | 1 / 0 / 0 / 1 | `reviews/review-2026-09-09-security-data-integrity-v8.md` |
| **Total** | | **6 / 13 / 9 / 6** (34) | |

‡ Brownfield's 7 Highs (H1–H7) are all already-tracked implementation debt with named backlog stories (5.2, 5.3, 5.6, 5.7) — confirmed unchanged, not new spine defects; no spine amendment required. Its 2 Mediums are 1 new minor tracking-annotation gap (M1) and 1 confirmed-still-fixed register check (M2, not open).

**Lint note (`lint_spine.py`):** 10 findings, all `TBD` markers in the Architecture Assumptions table's `TargetRetirementDate` column — this is the same pre-existing tool/reporting discrepancy the round-4 Update report already flagged (fabricating dates to silence the linter would violate the no-invented-content rule; these are legitimately open, owned rows per `ARCH-A-INDEX-3`). Not treated as a gate finding here, consistent with that prior triage. No duplicate `AD` ids, no missing Binds/Prevents/Rule, no unpinned Stack versions.

---

## Critical — close before any new command handler is built against AD-5, AD-12, AD-13, AD-30, AD-11, or the DigestKey scheme

### C-1 — AD-5 contradicts FR-18's transition table on three points (Rubric)

**Location:** AD-5, lines 145–149.

1. AD-5: *"from `Approved` it is legal only as the system's own `RemovedInConversations` abandonment below, never a human abandon."* FR-18's transition table grants a human abandon from `Approved` with no such restriction (actor: *"Eligible Approver; Tenant Agent Administrator"*) — a direct contradiction.
2. AD-5 requires the `MessageId` lookup before the system's `Approved → Abandoned` transition and leaves the proposal stuck in `Approved` if the lookup is unavailable. FR-18 explicitly skips that lookup for this exact case (*"no post was attempted from `Approved`, so the lookup is skipped and a detected removal abandons directly"*) — the addendum records this rejection verbatim.
3. `PostingWindowElapsed` and a stored `PostingPending` attempt deadline are grep-confirmed absent from the entire Spine; both are normative FR-18 transition rows.

**Divergence risk:** the core proposal-lifecycle state machine — the single most implementation-critical aggregate in the module — would, built from the Spine alone, forbid an action FR-33/FR-18 grant, risk deadlocking a proposal in `Approved` forever, and never implement two load-bearing FR-18 transitions.

**Fix:** rewrite AD-5's abandon/lookup clause to match FR-18 verbatim (skip the lookup for any `Approved`-origin abandon; permit a human abandon from `Approved` with no Conversation-read-access condition); add `PostingWindowElapsed` and the `PostingPending` stored deadline as first-class rule text.

---

### C-2 — AD-12's "Second-update trigger calculation" computes a formula the PRD addendum records as rejected (Rubric)

**Location:** AD-12, lines 193 and 195.

AD-12's paragraph uses a flat-20-call floor over `(Party, Conversation, reason)` tuples. FR-28/A-17 (as amended by the third 2026-09-09 update) define a distinct-calling-Party ratio with a 3-Party convening floor and a 10-calls-per-Party (20–50 bounded) small-tenant sample; `addendum.md` records the flat-20 rule's rejection verbatim: *"Rejected for the discontinuity it produced at 5 Parties."* AD-12 also never mentions the 3-Party convening floor.

**Divergence risk:** this formula decides when the tenant-wide kill switch's mandatory trigger review convenes — a safety-critical control. Built from the Spine, it computes review-trigger conditions on the wrong population with a formula the PRD's own reviewer gate rejected as worse.

**Fix:** replace the Second-update paragraph's formula with FR-28/A-17's distinct-calling-Party ratio, the 3-Party convening floor, and the bounded small-tenant sample, verbatim.

---

### C-3 — AD-13 misorders FR-8's acceptance steps and requires Eligible Approver resolution in Automatic mode (Rubric)

**Location:** AD-13, line 201 (opening sentences).

Current FR-8's ten-step order places Eligible Approver resolution at step 6, Confirmation mode only; AD-13 places it after context measurement/reservation/safety and claims it "runs in every response mode," rejecting a zero-Approver Automatic-mode Agent with `NoEligibleApprover`. FR-11 states outright that no Eligible Approver is resolved or referenced in Automatic mode. AD-13 also drops FR-8 step 3 (the local `Blocked` check) entirely.

**Divergence risk:** built as written, acceptance would skip the Conversation-block check at the wrong point, run Approver resolution after cost/safety work has already spent budget, and reject valid Automatic-mode calls FR-11 says must succeed.

**Fix:** replace AD-13's opening sentence with the current ten-step FR-8 order (including the step-3 block check); scope Eligible Approver resolution to Confirmation mode only, matching FR-8/FR-11.

---

### C-4 — AD-30's `Workflow` principal envelope states two contradictory rules for `OnBehalfOfPartyId` in the same paragraph (Adversarial)

**Location:** AD-30, principal-envelope definition; also binds AD-21, AD-13.

AD-21 cites AD-30 for the rule that `OnBehalfOfPartyId` varies per attempt (caller for the first generation, requesting Approver for a regeneration) — and explicitly warns against a constant-`CallerPartyId` reading. But AD-30's own `Workflow`-principal envelope clause fixes `OnBehalfOfPartyId` to *"the snapshot caller"* — a lifecycle constant — then, three sentences later in the same paragraph, restates the per-step-varying rule.

**Two-unit scenario:** Team A implements the envelope clause literally — every Workflow-dispatched reserve command, including a regeneration's, charges the original caller. Team B implements AD-21's rule (and AD-30's own later sentence) — a regeneration's reserve command charges the requesting Approver. Both are literal readings of currently-in-force text; the two systems charge rate limits and cost-cap consumption to different Parties for the same event.

**Fix:** delete or qualify the `Workflow` envelope's fixed clause; state explicitly that the Workflow principal's `OnBehalfOfPartyId` is not a lifecycle constant, and point AD-21's citation at the corrected clause.

---

### C-5 — `ConversationContextPolicy` is referenced and described as independently versioned/published, but has no aggregate in AD-2's closed enumeration (Adversarial)

**Location:** AD-2 (aggregate enumeration), AD-11 (`ContextPolicyReference`).

AD-2's aggregate list is closed ("V1 aggregates are **exactly**...") and has no `ConversationContextPolicy` row. AD-11 nonetheless describes the Conversation Context Policy as something that "publishes... version[s]," snapshotted via a `*Reference` field — the same pattern AD-2 uses for the one entity that *does* get its own `system`-scoped aggregate, `ContentSafetyPolicy`.

**Two-unit scenario:** Team A reads AD-2's closed list as authoritative and embeds context-policy content inline in `Agent`, versioned by `ConfigurationVersion`. Team B reads AD-11's "publishes... version[s]" language by direct analogy with `ContentSafetyPolicy` and adds a fifteenth, `system`-scoped aggregate with its own stream, revision, and migration path. The two builds have structurally incompatible EventStore topologies, and neither AD-2 nor AD-11 names an owner or authorized mutator for the context-budget `margin` default (10%, range 5–25) either way.

**Fix:** either add `ConversationContextPolicy` as a fifteenth AD-2 aggregate (parallel to `ContentSafetyPolicy`, naming the margin field's owner/range authority), or strike the "publishes... version[s]"/`*Reference` framing from AD-11 and state plainly that `ContextPolicyReference` is a self-versioned field on `Agent`.

---

### C-6 — The versioned `DigestKey` model never binds a stored digest to the key version that produced it (Security)

**Location:** AD-22 (`DigestKey` rotation rule), also AD-29, AD-13, `ARCH-A-12`.

The round-4 fix made `DigestKey` rotation genuinely usable (versioned, `EXT-SECRETS-1` retains every issued version indefinitely, rotation is unrestricted and content-free). But no record that carries a sensitive-content digest — not the `ProviderAttempt` fingerprint, not the AD-29 idempotency record, not an `AuditEvidence` entry, not the per-message digests inside the AD-13 fingerprint — carries the `DigestKeyVersion` it was computed under. HMAC-SHA-256 is one-way: given only a stored digest and the plaintext, nothing can tell which of the (now multiple, all-retained) key versions to recompute against.

**Concrete failure scenarios:**
- **Idempotency false-conflict:** a client retries a byte-identical lock-bearing command after the tenant's `DigestKey` has rotated between submissions; the recomputed fingerprint (against the *current* key) differs from the original, and AD-29's own rule treats a different fingerprint on the same tuple as a conflict — a legitimate idempotent retry is misclassified as a 409.
- **Audit-evidence unverifiability:** a Compliance Inspector or legal-hold reviewer reconstructing an inspection years later, after content has been legitimately erased, has no way to know which retained key version to recompute against without brute-forcing every version — an unbounded, unspecified procedure that quietly breaks the exact tamper-evidence guarantee the digest scheme exists to provide.

**Why this is new, not a restatement:** the v7-era finding was that rotation was practically impossible; that is now genuinely fixed. This gap only exists *because* rotation now genuinely happens — a direct, foreseeable consequence of the fix that the Spine text nowhere resolves.

**Fix:** tag every persisted digest (idempotency fingerprint, retained `ProviderAttempt` fingerprint, per-message digest inside `audit-evidence`) with the `DigestKeyVersion` used to compute it, alongside the digest value, so re-verification and idempotency-conflict comparison recompute against the recorded version rather than "current."

---

## High

### From the rubric lens (new — PRD/Spine text mismatches)

- **H-1 — AD-6 says `EXT-CONV-AI-1` has six seams; the PRD's third update added a seventh** (message retraction, `[ASSUMPTION A-28]`). The "Deferred Beyond V1" table still says the seam "does not yet name" retraction — now stale, since the PRD has named it as pending. *Fix: update AD-6 to seven seams incl. A-28; reword the Deferred row.*
- **H-2 — Two undocumented, non-cross-referenced status contracts:** the Spine cites `AgentCallOperationStatus` (AD-5, AD-15) while FR-8 defines the PRD's actual public write-path contract as `AgentInteractionStatus` — zero matches for that name anywhere in the Spine. The two enums share only two members (`Posted`, `PostingFailed`) and are otherwise disjoint, with no stated relationship. *Fix: reconcile the names, or state explicitly why they're distinct.*
- **H-3 — AD-7's "Second-update membership reconciliation" still diverges from FR-2 on three points:** it re-admits `ExternallyRemoved` on bare presence with no A-22 gate (FR-2 blocks this while A-22 is unretired); it omits the outbound re-admission mirror call FR-2 requires on clear; it omits FR-2's roster-freshness condition for detecting external removal entirely. *Fix: add all three conditions to AD-7's Second-update paragraph.*
- **H-4 — AD-14's attestation paragraph omits FR-34's signed-identity pin,** describing only a canary + self-reported identity/version — exactly the "pass-through-wrapper" case FR-34's reviewer gate designed the signed-identity pin to prevent. *Fix: state the `EXT-SECRETS-1`-sourced signed identity, the pin-and-verify-on-every-call requirement, and the canary as liveness-only.*

### From the adversarial lens (new — internal self-contradictions)

- **H-5 — AD-2's literal membership state-machine graph contradicts AD-7's own reconciliation table.** AD-2: `ExternallyRemoved` can only reach `Joined` via `ReadmitPending` (an authorized clear). AD-7's Second-update table: `ExternallyRemoved` + present → `Joined` directly, no clear step. One build enforces the clearing-authority gate; the other silently bypasses it — a real, security-relevant divergence. *Fix: explicitly supersede the transition graph in AD-2, or fix AD-7's table to route through `ReadmitPending`.*
- **H-6 — AD-8's Eligible Approver predicate doesn't exclude the regeneration-requesting Approver,** opening a self-approval path for `Regenerated` (not `Edited`) versions: the predicate excludes only "the caller" and "the last editor," neither of which literally covers an Approver who triggered a regeneration. *Fix: add the regeneration-requesting Approver as a fourth explicit exclusion.*

### From the brownfield lens (already-tracked implementation debt — confirmed unchanged, no spine amendment needed)

- **H-7** — AD-2's platform/tenant `ProviderCatalog` split still absent in code (`NC-5.3-PLATFORM-CATALOG-SCOPE`; Story 5.3).
- **H-8** — AD-4's `ConfigurationVersion` still not bumped by `AgentActivated`/`AgentDisabled` (DW-4; Story 5.2, correctly `in-progress`).
- **H-9** — No shared `AgentsIdentity` canonicalizer (AD-29; Stories 6.4/7.1–7.3).
- **H-10** — Structural seed gaps: `Aggregates/`, `Application/Tools/` empty; `Hexalith.Agents.IntegrationTests` absent (Story 5.6).
- **H-11** — AD-30's HMAC-tagged extensions and `Agents.PlatformOperator` policy absent from the shipped (transitional) authorization gate.
- **H-12** — AD-7's "retired" Party link/replace commands still succeed live — explicitly tracked as Story 5.2's unmet AC in both the PRD and `epics.md`.
- **H-13** — Legacy `EnableProductionLikeGeneration` gate bypasses `LaunchReadinessGate`/register/`RQ-1` entirely — independently classified identically by the PRD's own validation report; Story 5.7 (`backlog`) is the named fix.

---

## Medium

- Frontmatter `binds:` still cites `PRD OQ-1..OQ-30`; the PRD now runs through `OQ-31`. *(Rubric)*
- AD-17's "Second-update blocker emission" paragraph omits `SuspensionReviewOverdue` and `DeferredAssumption`, both defined in PRD FR-28/FR-30. *(Rubric)*
- AD-22 defines no post-hoc compliance-inspection aggregation bound (the PRD's 5-Conversation/30-day "wide inspection" governance guardrail, A-23/FR-24, is absent). *(Rubric)*
- AD-21 cites a stale "FR-8 step 4" for the rate-limit/concurrency admission check; current FR-8 step 4 is Provider eligibility, step 5 is rate limits (renumbered by the third update, citation not updated). *(Rubric)*
- Stack table's `Hexalith.EventStore` gitlink (`e302432c`) is stale — the actual committed pin is now `0994c378`, moved ~2h11m before this review, same calendar day. The other four gitlinks are exact matches. *(Verified-current)*
- `ARCH-A-4`'s "immediate" SDK remediation has not landed: `global.json` still pins `10.0.301`/`rollForward: latestPatch`, still in the abandoned, CVE-2026-69522-exposed `10.0.3xx` band. *(Verified-current — the underlying exposure claim itself is independently confirmed accurate, see review file F3)*
- AD-10's widened `EntryMissing` carve-out isn't as exact a match to AD-2's carve-out as claimed — flagged again this round; see adversarial finding 6/low-tier detail in the v8 review. *(Adversarial)*
- `sprint-status.yaml`'s Story 5.3 line (`backlog`) carries no inline annotation explaining that a completed, fully-tested prior implementation (2569/2569 tests passing) exists underneath it — safe-direction (under-claiming) gap, easy one-line fix, not a false-done defect. *(Brownfield, new — M1)*
- *(Confirmed still fixed, not open)* — `launch-readiness-register.md`'s `LR-AUDIT-PROTECTION-DELETION` `DeletionRequest`/`LegalHold` symmetry, re-verified against the live register text. *(Brownfield — M2)*

---

## Low

- AD-22's stated rationale for exempting `DigestKey` rotation from AD-12 session locking ("targets no tenant-scoped resource") contradicts the same sentence's own premise that it is "the *per-tenant* `DigestKey`" — the exemption is likely still correct, but for a different, unstated reason (rotation is purely additive/non-conflicting, not because the resource is untenanted). *(Security)*
- "The Agents service principal" (used in AD-2/AD-6/AD-7) is never placed inside AD-30's otherwise-closed four-member principal taxonomy — unclear whether it's a `Platform` alias or a distinct out-of-envelope credential with its own audit-coverage answer. *(Adversarial)*
- AD-17's register-deferred `AuthorizedProducer` per `GateId` isn't constrained to match AD-30's blanket producer rule by `ScopeKind` — a lower-confidence finding contingent on a document outside this file. *(Adversarial)*
- Stack-table version-pair detail gaps and a `bunit` patch-behind note; no functional impact. *(Verified-current)*
- AppHost-free claim (AD-16) re-verified by an actual passing test run (`ForbiddenHostingOwnershipTests`, 5/5); Epics 1–4 "done" status confirmed explicitly historical-only per `epics.md`'s own reconciliation language, not a live-conformance claim. *(Brownfield, confirmed — not open)*

---

## What's already closed (re-verified, not re-litigated)

This round independently re-derived, rather than trusted, every claim the round-4 Update pass and its v7 gate made: the AD-7 block-clearing authority rewrite, the `launch-readiness-register.md` `LR-AUDIT-PROTECTION-DELETION` `DeletionRequest` symmetry, the AD-30 `Platform` freshness source correction, AD-12's `PostingFailed` disabled-Agent suspension and its `PausedDuration` union-not-sum merge rule, AD-13's HMAC-SHA-256 fingerprint algorithm specification (now consistent everywhere it's referenced), and the `sprint-status.yaml` Story 5.2 tracking-integrity correction — **all confirmed genuinely closed** against the live text. The `DigestKey` rotation redesign itself is also confirmed genuinely closed on its own narrow terms (rotation now actually works for a live tenant) — it is reopened above at C-6 only because the fix, while correct as far as it goes, left a new gap the redesign itself introduces (key-version binding), not because the original v7 defect resurfaced.

---

## Recommendation

Roll this report into an Update pass. Priority order:

1. **C-1 / C-2 / C-3 (rubric — PRD alignment)** — these are the highest-confidence, highest-impact findings: three core runtime rules (proposal abandon/lookup, kill-switch trigger formula, acceptance-step order and Approver scope) directly contradict the PRD they're bound to and have survived four prior rounds. Fix by adopting the PRD's own FR-18/FR-28/FR-8/FR-11 text verbatim — no new design work, only reconciliation.
2. **C-4 / C-5 (adversarial — internal self-contradiction)** — resolve the `OnBehalfOfPartyId` envelope contradiction (a one-sentence deletion/qualification) and decide `ConversationContextPolicy`'s ownership (aggregate vs. inline field) — both are genuine design decisions, not just wording.
3. **C-6 (security — DigestKeyVersion binding)** — a small, additive schema change (one more field alongside every persisted digest) that completes the versioned-key model the round-4 fix already adopted.
4. **H-1 .. H-6** — the remaining PRD/Spine and internal-consistency gaps; all are spine-text-only fixes with no fresh design work.
5. **H-7 .. H-13** — already-tracked implementation debt; no spine amendment needed, only code catching up per its named backlog story.
6. **Medium tier**, same pass, especially the OQ-31/binds-list refresh, the `SuspensionReviewOverdue`/`DeferredAssumption` blocker-vocabulary gap, and re-pulling the `Hexalith.EventStore` gitlink immediately before finalizing (this spine is churning same-day; a value verified hours ago is not reliable).
7. Separately from the spine text: bump `global.json`'s SDK pin past the exposed `10.0.3xx` band — `ARCH-A-4` already calls this urgent, but the fix has not actually landed in the repo.

Spine and memlog were **not** modified by this run (Validate intent).
