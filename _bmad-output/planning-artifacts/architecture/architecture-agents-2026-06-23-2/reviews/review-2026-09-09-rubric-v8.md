# Rubric Walker Review — Architecture Spine (Hexalith Agents) — v8

**Reviewer lens:** independent rubric walker. No prior review's "resolved" or "second-update" claim was trusted; every finding below was re-derived by reading the live spine text (798 lines), the live PRD (`prd.md`, 1081 lines, including §8, §8.1, FR-33, and the V1 Decision Register), `addendum.md` (all three "Options Considered" sections plus the reviewer-gate rejections), and `update-report-2026-09-09-2.md`, and then cross-checking specific claims against the spine with targeted greps to confirm presence/absence rather than trusting summaries.

**Overall verdict: FAIL**

The spine's own binding PRD (§8.1, "The known Spine divergences as of 2026-09-09 (third update and its reviewer gate)") lists seven AD-level divergences between itself and the current Spine text. All seven were independently re-verified against the live Spine text below and are **still present** — none were closed by the "Second-update" correction paragraphs already baked into AD-2, AD-7, AD-12, AD-14, AD-15, AD-17, and AD-30. In addition, two further defects were found that the PRD's own list does not mention (a seam-count mismatch on `EXT-CONV-AI-1`, and an undocumented dual status-contract naming split). Three of the confirmed findings are Critical because they describe runtime behavior that directly contradicts an FR-18/FR-8/FR-28 transition table or formula an implementer would otherwise build correctly from the PRD alone — i.e., a story built strictly from the Spine, per the Spine's own stated purpose as build-substrate, will diverge from the PRD it claims to bind.

---

## Findings

### CRITICAL — AD-5 (Proposal Lifecycle) contradicts FR-18's transition table on three points

**Location:** `ARCHITECTURE-SPINE.md`, AD-5, lines 145–149 (the whole rule paragraph).

**What's wrong:**
1. AD-5 states: *"from `Approved` it is legal only as the system's own `RemovedInConversations` abandonment below, never a human abandon."* PRD FR-18's transition table explicitly grants a human abandon from `Approved`: the row `Approved, PostingFailed` → `Abandoned` lists actor *"Eligible Approver; Tenant Agent Administrator (FR-33, audited; for `PostingFailed` only when the Source Conversation is gone or inaccessible)"* — i.e., an Eligible Approver **can** abandon an `Approved` proposal with no such restriction. This is a direct, unambiguous contradiction, not a phrasing nuance.
2. AD-5 requires the `MessageId` lookup to run *"before the system's `Approved` -> `Abandoned` (`RemovedInConversations`) transition"* and states an unavailable lookup leaves the proposal stuck in `Approved`. FR-18 explicitly does the opposite: *"The lookup is skipped for any proposal for which no post was ever attempted — a `PostingFailed` proposal whose failure was a pre-post re-validation, and every `Approved` proposal, since posting has not started — so a removal detected against an `Approved` proposal abandons it directly."* `addendum.md`'s "Third 2026-09-09 Update" section records this exact rejection verbatim: *"The `MessageId` lookup from `Approved`... was rejected: no post was attempted from `Approved`, so the lookup is skipped and a detected removal abandons directly (FR-18)."*
3. AD-5 has no `PostingWindowElapsed` staleness bound and no stored attempt-deadline for `PostingPending` — both grep-confirmed absent from the entire document (`grep -n "PostingWindowElapsed" ARCHITECTURE-SPINE.md` → no match). FR-18 defines `PostingWindowElapsed` as a real terminal transition (`Approved`/`PostingFailed` → `Abandoned (PostingWindowElapsed)`) bounding how long a proposal may sit `Approved`/`PostingFailed`, and defines the `PostingPending` attempt timeout as *"a stored deadline carried by the `PostingPending` record and evaluated on every read, every command, and on recovery."* Neither concept exists anywhere in the Spine.

**Divergence risk:** This is the core proposal-lifecycle state machine — the single most implementation-critical aggregate in the module. A story built from AD-5 alone would (a) forbid a human action FR-33/FR-18 explicitly grants, (b) run an extra Conversations existence-read before an abandon that the PRD says must not run there, and could deadlock a proposal in `Approved` forever when that read is unavailable, and (c) never implement the `PostingWindowElapsed` staleness exit or the `PostingPending` attempt deadline that FR-18's own transition table treats as normative, load-bearing rows. This is precisely the divergence the PRD's own §8.1 already names, unresolved across at least four prior rubric-review rounds (v4, v6, v7 sources are cited in this spine's own frontmatter, dated the same day).

**Suggested fix:** Rewrite AD-5's abandon/lookup clause to match FR-18 verbatim: skip the `MessageId` lookup for any `Approved`-origin abandon (system or human), permit a human (Eligible Approver or Tenant Agent Administrator) abandon from `Approved` with no Conversation-read-access condition, and add the `PostingWindowElapsed` bound and the `PostingPending` stored attempt-deadline as first-class rule text, then increment `architecture_assumption_index_version` per the frontmatter's own change-tracking convention if any assumption is touched.

---

### CRITICAL — AD-12's "Second-update trigger calculation" paragraph computes a different formula than the PRD's own superseding rule

**Location:** `ARCHITECTURE-SPINE.md`, AD-12, main rule (line 193) and the "Second-update trigger calculation" paragraph immediately below it (line 195).

**What's wrong:** The Second-update paragraph states: *"The blocked-call and unavailable-call shares count distinct `(Party, Conversation, reason)` tuples over distinct `(Party, Conversation)` attempts... a tenant with fewer than 5 calling Parties uses every Party that called and 20 calls."* PRD FR-28 and assumption A-17 (as amended by the third 2026-09-09 update) define a materially different formula: *"the two shares count distinct calling Parties and convene a review only with at least 3 distinct Parties in the numerator... minimum sample 5 distinct calling Parties and 50 Agent Calls (every calling Party and 10 calls per Party, 20–50, below 5 Parties)."* `addendum.md`'s "Third 2026-09-09 Update" section records the flat-20 rule's rejection explicitly: *"A flat 20-call minimum sample below 5 calling Parties. Rejected for the discontinuity it produced at 5 Parties; the sample is 10 calls per calling Party, bounded to 20–50 (FR-28, A-17)."* AD-12's own text uses exactly the rejected flat-20 rule, uses a `(Party, Conversation)`-pair denominator instead of a distinct-calling-Party denominator, and never mentions the "3 distinct Parties in the numerator" convening floor at all.

**Divergence risk:** This is the formula that decides when the tenant-wide kill switch's mandatory trigger review convenes — a safety-critical control (SM-4/FR-28). A story built from AD-12 would compute review-trigger conditions on a completely different population (Party×Conversation pairs vs. distinct Parties) and a different small-tenant floor, producing different — and, per the PRD's own stated rationale, worse (discontinuous) — trigger behavior than the governing requirement.

**Suggested fix:** Replace the Second-update paragraph's formula with FR-28/A-17's distinct-calling-Party ratio, the 3-Party convening floor, and the 10-calls-per-Party (20–50 bounded) small-tenant sample, verbatim.

---

### CRITICAL — AD-13 misorders FR-8's acceptance steps and requires Eligible Approver resolution in Automatic mode, contradicting FR-8/FR-11

**Location:** `ARCHITECTURE-SPINE.md`, AD-13, line 201 (opening two sentences of the rule).

**What's wrong:** AD-13 states: *"Acceptance follows the FR-8 order (authorization, lifecycle, provider eligibility, rate limits, context measurement, reservation, pre-Provider safety, Eligible Approver resolution, membership) and ends with one `AgentCallAccepted` event; Eligible Approver resolution runs in every response mode, not Confirmation mode only — an Automatic-mode Agent with zero configured Approvers is rejected up front with `NoEligibleApprover` before any Provider call, on the same terms as Confirmation mode..."*

This is wrong on two independent axes against the live PRD:
1. **Order:** current FR-8 (as reordered by the third 2026-09-09 update, which explicitly renumbered every citing FR) is: 1 caller/Party/Conversation authorization, 2 lifecycle+tenant-suspension, 3 Conversation Agent State (`Blocked` check), 4 Provider/model eligibility, 5 rate limits + per-Party concurrent bound, 6 Eligible Approver resolution (Confirmation mode only), 7 context measurement, 8 cost reservation, 9 safety scan, 10 membership. AD-13's sequence omits step 3 (the local `Blocked` check) entirely and places Eligible Approver resolution *after* context measurement/reservation/safety instead of at step 6, before them.
2. **Scope:** FR-8 step 6 is explicitly gated *"where Confirmation Response Mode applies,"* and FR-11 states outright: *"No Eligible Approver is resolved or referenced in Automatic Response Mode: where an FR-18 row names an Eligible Approver, the posting record substitutes any current Conversation Facilitator..."* AD-13's claim that an Automatic-mode Agent with zero Approvers is rejected with `NoEligibleApprover` is not merely a documentation gap — it describes different runtime behavior that would incorrectly block valid Automatic-mode Agent Calls (Automatic-mode Agents commonly configure no Approver Policy sources at all, since they don't need one).

**Divergence risk:** Building the acceptance pipeline from AD-13 as written would (a) skip the Conversation-block check at the wrong point in the sequence, (b) evaluate Eligible Approver resolution after context/cost/safety work has already run (wasting spend and violating the "before Provider work" ordering FR-7/FR-13 rely on for its "no proposal created without an Eligible Approver" guarantee), and (c) reject legitimate Automatic Response Mode calls that FR-11 says must succeed.

**Suggested fix:** Replace AD-13's opening sentence with the current ten-step FR-8 order (including the step-3 block check), and correct the Eligible Approver scope to Confirmation Response Mode only, matching FR-8 step 6 and FR-11.

---

### HIGH — AD-6 says `EXT-CONV-AI-1` has six seams; the PRD's third update added a seventh

**Location:** `ARCHITECTURE-SPINE.md`, AD-6, line 155 (also affects the "External V1 Prerequisites" table's `EXT-CONV-AI-1` row and the "Deferred Beyond V1" table row at line 796).

**What's wrong:** AD-6 states: *"The six Conversations-owned seams committed as `EXT-CONV-AI-1` [ASSUMPTION A-1, A-2, A-3, A-4, A-15, A-16] (idempotent AI membership..., idempotent posting..., Facilitator resolution, active-Conversation count, tenant-scoped content/roster/existence reads..., and a Conversation deletion signal)"* — six items, six assumption keys. PRD §8 now lists **seven** seams; item 7 is *"Message retraction: a signal that a Conversation Message posted as the `AiAgent` participant was retracted, deleted, or flagged... [ASSUMPTION A-28]"*, requested by the PRD's third 2026-09-09 update. AD-6's list omits seam 7 and its assumption key (A-28) entirely.

This omission compounds with the "Deferred Beyond V1" table, which still reads: *"Automatic-mode retraction metric | Needs a Conversations retraction seam that `EXT-CONV-AI-1` does not yet name (PRD OQ-23)."* That statement is now stale — the PRD has named the seam (as a pending, not-yet-committed request), which is a materially different state than "does not yet name."

**Divergence risk:** A story integrating `EXT-CONV-AI-1`, or a governance/compliance story that needs the eventual retraction signal, would not learn from the Spine that a seventh seam exists at all or that A-28 tracks it — the Spine's own count and its Deferred-table rationale actively assert it doesn't exist yet, when the PRD register already carries the request.

**Suggested fix:** Update AD-6 to "seven seams," add seam 7 and `[ASSUMPTION A-28]` to its list, and reword the Deferred Beyond V1 row to say the seam is requested/pending rather than unnamed.

---

### HIGH — Two undocumented, non-cross-referenced status contracts: `AgentCallOperationStatus` (Spine) vs. `AgentInteractionStatus` (PRD)

**Location:** `ARCHITECTURE-SPINE.md`, AD-5 line 149 and AD-15 line 215 (both cite `AgentCallOperationStatus`).

**What's wrong:** The Spine cites `AgentCallOperationStatus` twice as a public/durable status contract, with described members `SafetyBlocked`, `BudgetBlocked`, `RateLimited`, `CapacityQueued`, `CapacityRejected`, `UnknownOutcome`, `PostingPending`, `Posted`, `PostingFailed`. The string `AgentInteractionStatus` — the actual and only publicly named Agent-Call status contract the PRD defines (FR-8: *"The Agent Call's public state contract is `AgentInteractionStatus`, whose members... are `Requested`, `Authorized`, `Denied`, `Blocked`, `ContextReady`, `ContextBlocked`, `Generated`, `GenerationFailed`, `SafetyFailed`, `Posted`, `PostingFailed`, and `ProposalCreated`..."*) — never appears anywhere in the Spine (grep-confirmed: zero matches across the whole 798-line document). The two enums overlap in concept and share two literal member names (`Posted`, `PostingFailed`) but are otherwise disjoint, and nothing in the Spine explains whether `AgentCallOperationStatus` is a distinct query/UI-side status summary, a stale predecessor name for `AgentInteractionStatus`, or an editorial slip.

**Divergence risk:** AD-15 explicitly ties this name to the FR-15/UI-parity guarantee ("Every UX-required state has a public contract before its route ships"). An implementer following the Spine's contract-naming for UI/status work and an implementer following FR-8's PRD-defined `AgentInteractionStatus` for the write path could build two different, uncoordinated enums for what reads as the same concept — exactly the kind of type-name drift this rubric is meant to catch.

**Suggested fix:** Either rename `AgentCallOperationStatus` to `AgentInteractionStatus` throughout the Spine (if they are meant to be the same contract) or, if genuinely distinct (e.g., a derived read-model status vs. the write-path state), add one sentence at first use stating the relationship and why the member sets differ.

---

### HIGH — AD-7's "Second-update membership reconciliation" still doesn't match FR-2 on three points

**Location:** `ARCHITECTURE-SPINE.md`, AD-7, main rule (line 161) and "Second-update membership reconciliation" paragraph (line 163).

**What's wrong:** Despite carrying a dedicated correction paragraph, AD-7 still diverges from FR-2 (per the PRD's own §8.1 list, independently re-verified):
1. **A-22 gate missing on re-admission-by-presence.** AD-7's Second-update text says: *"`ExternallyRemoved` plus absent rejects and plus present records `Joined`"* — unconditionally. FR-2 gates this explicitly: *"Until A-22 is retired, presence again does not re-admit: the state stays `ExternallyRemoved`, the call is rejected on the same terms as `Blocked`, and only the FR-33 clear row re-admits."* Since A-22 is currently unretired (its row in the PRD's Assumptions Index is still open), AD-7's rule describes behavior the PRD explicitly forbids today.
2. **No re-admission mirror on clear.** AD-7's text says a clear *"records `ReadmitPending`... and does not rejoin"* but never states that the clear itself issues an outbound Conversations mirror call. FR-2 requires it: *"Clearing records `ReadmitPending` and issues the re-admission add as its own seam-1 mirror entry, at-least-once under the same `MirrorPending` and `MirrorRefused` terms as a block mirror, so that a later absence can be told from the mirror's late effect."*
3. **No roster-freshness condition for detecting external removal.** AD-7's text conditions `ExternallyRemoved` only on *"the last removal mirror is confirmed and no newer clear exists."* FR-2 adds a second, independent condition the Spine omits entirely: the roster read used to detect absence must be *"at least as fresh as the last confirmed add for that Conversation"*; an older read is `MembershipUnavailable`, not evidence of removal. Nothing in AD-7 mentions roster-read freshness at all.

**Divergence risk:** All three affect the correctness of `hexa`'s Conversation-membership state machine, which gates every Agent Call. Divergence 1 in particular means a story built from AD-7 would incorrectly re-admit `hexa` on mere presence while A-22 remains open — re-establishing an Agent a Facilitator explicitly removed, which is exactly the failure mode FR-2's negative-test list calls out by name.

**Suggested fix:** Add the A-22 gate condition, the clear-issues-its-own-mirror-entry rule, and the roster-freshness condition to AD-7's Second-update paragraph (or fold them into the main rule), matching FR-2 verbatim.

---

### HIGH — AD-14's attestation description omits the signed-identity pin FR-34 requires, reading as satisfiable by canary-only evidence

**Location:** `ARCHITECTURE-SPINE.md`, AD-14, "Second-update protection attestation" paragraph (line 209).

**What's wrong:** The paragraph reads in full: *"`EXT-HOST-1` binds the `EXT-PROTECTION-1` engine at startup and on every readiness evaluation, records a canary protect/unprotect result plus engine identity and version, and refuses content-bearing activation or workflow progress on missing, stale, incompatible, no-op, or pass-through-wrapper evidence."* FR-34 defines a materially more specific, two-part attestation: (1) an **identity** check — the runtime verifies the loaded engine's *signed* build identity against a Security-qualified value custodied through `EXT-SECRETS-1` (*"never a string the engine reports about itself"*) and **pins** it, so every subsequent seal/unseal call is checked against that pin; and (2) a **liveness** canary that is explicitly demoted to *"never a proof of protection"* on its own. FR-34's own test clause is explicit that a self-reporting wrapper must be caught *by signature, never by canary behavior* — this is exactly the attack the PRD's reviewer gate rejected under finding H-6 (recorded in `addendum.md`). AD-14's paragraph describes only "engine identity and version" being "recorded," with no mention of the signed-identity comparison, the pin, the `EXT-SECRETS-1`-sourced value, or the re-verification on every seal/unseal call.

**Divergence risk:** As written, AD-14 could be satisfied by an engine that merely reports its own identity/version and passes a canary — precisely the "pass-through-wrapper" case FR-34 and the PRD's own reviewer gate designed the signed-identity pin to prevent. A story built from AD-14 alone risks re-introducing the vulnerability the PRD explicitly closed.

**Suggested fix:** Expand AD-14's Second-update paragraph to state both attestation parts explicitly: the Security-qualified signed identity/version sourced from `EXT-SECRETS-1`, the runtime-side pin-and-verify-on-every-call requirement, and the canary as a liveness-only, non-identity check.

---

### MEDIUM — Frontmatter `binds:` still cites `PRD OQ-1..OQ-30`; the PRD now runs through OQ-31

**Location:** `ARCHITECTURE-SPINE.md`, frontmatter, line 15.

**What's wrong:** The PRD's V1 Decision Register (§13) now contains `OQ-31` (*"Agent Instructions and configuration audit are not protected content (FR-34)... Deferred; revisit before enablement — whether instructions move under an Agent-level key"*), added by the third 2026-09-09 update. The Spine's frontmatter `binds:` list was not extended to match.

**Divergence risk:** Low direct behavioral impact (OQ-31 is itself Deferred, owned by Governance/Product, not yet a build-affecting decision), but it means the Spine's own self-declared coverage claim is measurably out of date, and `RQ-1`'s "no open decision" gate (FR-28 item 10) depends on every *Deferred* row landing before enablement — the Spine should at minimum track that this row exists.

**Suggested fix:** Update the `binds:` line to `PRD OQ-1..OQ-31` (or state explicitly why OQ-31 is out of scope for the Spine, if that is the intent).

---

### MEDIUM — AD-17's "Second-update blocker emission" paragraph omits two PRD-defined blocker/status values

**Location:** `ARCHITECTURE-SPINE.md`, AD-17, "Second-update blocker emission" paragraph (line 231).

**What's wrong:** The paragraph enumerates the blocker vocabulary authoritative producers emit: *"`UnretiredAssumption`, `OpenDecision`, `DependencyNotAvailable`, `GateOutOfScope`, `TriggerReviewOverdue`, and `InsufficientEvidence`; `RQ-1` additionally names `ProhibitedCostControlPosture` and `PayloadProtectionUnavailable`."* Grep-confirmed absent from the entire Spine document: `SuspensionReviewOverdue` and `DeferredAssumption` — both are explicit, currently-defined status/blocker values in PRD FR-28/FR-30 (*"`SuspensionReviewOverdue` is recorded on the FR-30 surface..."*; *"...unless the Release PM records a `DeferredAssumption` deferral..."*), and FR-30 explicitly states both are surfaced conditions the readiness system must expose.

**Divergence risk:** A story implementing the readiness/blocker vocabulary from AD-17 alone would omit two status values the PRD's own FR-30 public-surface requirement names explicitly, producing an incomplete readiness surface.

**Suggested fix:** Add `SuspensionReviewOverdue` and `DeferredAssumption` to AD-17's emitted-vocabulary list.

---

### MEDIUM — AD-22 defines no post-hoc compliance-inspection aggregation bound

**Location:** `ARCHITECTURE-SPINE.md`, AD-22, main rule (line 261).

**What's wrong:** AD-22's long paragraph on compliance inspection covers the second-party/subject-set computation and the unreviewed-inspection flag, but never states the PRD's aggregation guardrail (A-23/FR-24): *"Post-hoc inspections by one Inspector that touch more than 5 distinct Conversations in a rolling 30-day window are one wide inspection: the sixth requires prior Platform Operator approval... An Inspector with an unreviewed inspection cannot open another post-hoc inspection until it is reviewed."* Grep-confirmed: no mention of a 5-Conversation bound, "wide inspection," or an unreviewed-inspection block on further post-hoc access anywhere in the Spine.

**Divergence risk:** A story built from AD-22 alone would implement per-inspection controls correctly but omit the volume-based governance guardrail against an Inspector accumulating many small post-hoc inspections to avoid the Platform-Operator-approval threshold that a single wide inspection would trigger.

**Suggested fix:** Add the 5-Conversation/30-day aggregation rule and the unreviewed-inspection lockout to AD-22.

---

### MEDIUM — AD-21 cites a stale FR-8 step number for the rate-limit/concurrency admission check

**Location:** `ARCHITECTURE-SPINE.md`, AD-21, line 255.

**What's wrong:** AD-21 states: *"Before reservation, one atomic `BudgetLedger` `AdmitCall` command at FR-8 step 4 enforces the per-Party and per-Conversation windows..."* Current FR-8 step 4 is *"Provider/model eligibility (FR-5) and production-like enablement (FR-28)"*; rate limits and the per-Party concurrent bound are step 5. The PRD's third 2026-09-09 update explicitly reordered FR-8's steps and renumbered every citing cross-reference in the PRD itself — this Spine citation was not updated to match, and the PRD's own §8.1 divergence list names this exact citation as stale.

**Divergence risk:** Lower than the Critical findings above, since AD-21's described behavior (reserve after context measurement, before safety/admission) is still broadly consistent with the current FR-8 order — but a story author who trusts the literal "step 4" citation to locate the check in FR-8 will look in the wrong place.

**Suggested fix:** Change "FR-8 step 4" to "FR-8 step 5" in AD-21.

---

## Checklist coverage notes (no new findings beyond those above)

- **Coverage spot-check (item 6):** FR-1, FR-2, FR-4, FR-5, FR-7, FR-8, FR-9, FR-18, FR-28, FR-29, FR-33, FR-34, NFR-9, NFR-11..NFR-14 were each traced to at least one AD; no capability gap found beyond the specific contradictions above.
- **Operational envelope (item 7):** AD-16 addresses deployment/environments/infra appropriately for initiative altitude (deferring specifics to `EXT-HOST-1`), not silently skipped.
- **Cross-references verified correct (item 8, positive findings):** the AD-2/AD-10/AD-17 `EntryMissing` carve-out cross-reference is consistent across all three ADs; the nine lock-bearing-family list in AD-12 and AD-22 matches exactly.
- **Verified-current lens overlap (item 4):** Stack table's ARCH-A-4 (SDK CVE-2026-69522 exposure) and ARCH-A-8 (Fluent UI v5 RC pin) are already self-flagged by the Spine with dated escalation — not a new finding, noted only for completeness since a separate reviewer owns this lens.
- **Brownfield lens overlap (item 5):** nothing additional noticed; deferred to that lens per instructions.

---

## Severity Summary

| Severity | Count |
| --- | --- |
| Critical | 3 |
| High | 4 |
| Medium | 4 |
| Low | 0 |
| **Total** | **11** |
