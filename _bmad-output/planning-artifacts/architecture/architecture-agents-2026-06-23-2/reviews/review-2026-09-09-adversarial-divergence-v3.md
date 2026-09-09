# Adversarial Divergence Review v3 - ARCHITECTURE-SPINE.md (Hexalith Agents)

**Reviewer role:** independent adversarial reviewer, Reviewer Gate pass, 2026-09-09 (third pass; a delta/verification pass against the amendments made in response to `review-2026-09-09-adversarial-divergence-v2.md`, read fresh against the current ~758-line spine, not assumed from the prior review's or the caller's summary).

**Method:** for each of the six v2 findings, attempt to reconstruct the same or a closely related pair of independently-built units against the *current* text and determine whether the collision is actually closed. Then adversarially probe the exact new/amended sentences (AD-5↔AD-12, AD-7↔AD-2/AD-8/AD-30, AD-14's instrumentation-exclusion sentence, AD-17's tenant-check-before-freshness sentence, AD-22's justification-family list, AD-30's User-principal freshness sentence, and the AD-2/AD-10 carve-out pairing) for accuracy and for newly introduced divergence. Every verdict below is backed by a concrete pair or an exact-text cross-check, not a general worry.

---

## Finding 1 - General command idempotency payload-fingerprint canonicalization

**Verdict: PARTIALLY CLOSED**

AD-29 now states: "every public command's fingerprint hashes its `Hexalith.Agents.Contracts` DTO through that same shared canonicalizer as its own, self-contained component list — each public property, taken in contract-declared order, is one length-prefixed component preceded by a one-byte presence marker (`0x00` absent, `0x01` present) so an absent optional field is never conflated with an explicitly supplied default or empty value, and a nested object or collection flattens depth-first in declared/index order before hashing. This is the one command-payload fingerprint function every command-handler story uses; it is distinct from the AD-13 Provider-attempt fingerprint ... and from this AD's own id-derivation components."

This closes the original collision's dominant axis: Unit A (ad-hoc canonical-JSON serialization, camelCase) and Unit B (alphabetical-order identity-style flattening) are both now excluded — the AD names one mechanism (identity-canonicalizer-style, length-prefixed components, `contract-declared` order, not alphabetical, not JSON), explicitly disclaims the two sibling fingerprint functions it might be confused with, and gives a concrete nested/collection flattening rule. A builder can no longer defensibly choose JSON serialization or alphabetical ordering.

**Remaining gap.** The presence marker ("`0x00` absent, `0x01` present") presumes the `Hexalith.Agents.Contracts` DTO's public property already carries an unambiguous presence signal distinguishing "field omitted from the request" from "field explicitly supplied as null/default." Nothing in AD-29, the Naming convention, or the Package-layout convention mandates *how* a Contracts optional property preserves that distinction through deserialization. Two teams can each honor the presence-marker rule to the letter and still diverge:

- **Unit A** ("TenantBudgetUpdate" handler) models `CapOverrideExpiry` as a plain nullable CLR property (`DateTimeOffset? CapOverrideExpiry`). With ordinary System.Text.Json behavior, an explicit `"capOverrideExpiry": null` and an entirely omitted property both deserialize to CLR `null`; Unit A's canonicalizer reads "presence" off `is not null`, so both wire forms produce identical `0x00` components — Unit A believes it has fully implemented the AD-29 presence marker (it always emits one), but it has structurally collapsed exactly the case the AD's own text says must never be conflated ("an absent optional field is never conflated with an explicitly supplied default").
- **Unit B** ("ProposalEdit" generic command-handler base) models every optional property as an `Optional<T>` wrapper struct with a custom converter that sets `HasValue = true` on an explicit JSON `null` and `HasValue = false` only when the property is absent from the payload, and feeds `HasValue` as the presence bit.

Both satisfy AD-29's literal words ("one-byte presence marker... absent optional field is never conflated with an explicitly supplied default"), but only Unit B's DTO shape can actually keep that promise; Unit A's cannot, and nothing in the spine tells a command-handler story which DTO shape it must use to make the promise true. This is the same defect as before, moved one layer down — from "which canonicalizer style" (now closed) to "what CLR-level presence representation the Contracts DTOs must use" (still open). Severity is materially lower than the original CRITICAL (the ordering/style ambiguity that caused outright 409-vs-silent-collision behavior is gone), but it is a real, constructible divergence, not a hypothetical one — recommend the spine (or `IMPLEMENTATION-CONVENTIONS.md`, if the spine intends to delegate it there, in which case AD-29 should say so explicitly) name the required presence-preserving DTO shape for every optional public property.

---

## Finding 2 - AD-2 absent-key rule vs AD-10's `EntryMissing` carve-out

**Verdict: CLOSED** (core contradiction), but see new Finding A below for a scope mismatch the fix itself introduces.

AD-2 now reads: "...a query for a key outside the caller's tenant, or for a platform entry not enabled for it, returns exactly the absent-key response, **except that a query scoped to an Agent's own previously-snapshotted or currently-selected `ProviderId`/`ModelId` may instead report `EntryMissing` or a reason-coded `Blocked` `ProviderReadinessResult` inside `AgentReadinessStatus`, per AD-10 — the one named carve-out from the absent-key rule.**"

This directly resolves the v2 collision: AD-2 itself now grants the exception AD-10 already promised, in matching vocabulary (`EntryMissing`, reason-coded `Blocked`, `AgentReadinessStatus`). Unit A (generic catalog by-key query) and Unit B (`AgentReadinessStatus` projection) no longer have textually incompatible instructions — AD-2 tells Unit A's story that the readiness path is explicitly exempt, so the "structurally impossible to build" problem from v2 is gone: a builder now knows two code paths are required (generic absent-key query vs. self-scoped carve-out query) rather than having to reconcile one rule that admits no exception with another that requires one.

---

## Finding 3 - `ConfigurationVersion`/`InstructionsVersion`/`ApproverPolicyVersion` independence

**Verdict: CLOSED**

AD-4 now states explicitly: "`ConfigurationVersion`, `InstructionsVersion`, and `ApproverPolicyVersion` are three independent monotonic counters on the `Agent` aggregate, not one counter under three names: `ConfigurationVersion` increments on every accepted `Agent` configuration or lifecycle event, including an edit that also bumps one of the other two; `InstructionsVersion` increments only on an accepted edit to Agent Instructions text; `ApproverPolicyVersion` increments only on an accepted edit to `ApproverPolicy`'s source list under AD-8. A regeneration-ceiling, proposal-expiry, or response-mode edit through AD-21's `AgentSetupMutation` bumps `ConfigurationVersion` alone."

This directly answers the v2 collision: Unit A's assumption (one shared counter, everything bumps `ConfigurationVersion`) and Unit B's assumption (`ApproverPolicyVersion` is a separate, rare-moving epoch) are both partially right and now reconciled by an explicit rule — `ConfigurationVersion` is the broad "any config/lifecycle event" counter (confirming Unit A's breadth for that one counter), while `ApproverPolicyVersion` moves only on approver-policy source-list edits (confirming Unit B's narrowness for that one counter), and the AD states the two move together only when the *same* triggering event affects both ("including an edit that also bumps one of the other two"). AD-8 ("a new `ApproverPolicyVersion`" on the Conversations-owner-field amendment) and AD-21 ("increment `ConfigurationVersion`" for ceiling/expiry/response-mode edits) are both consistent with this rule as now stated. No remaining ambiguity found on this axis.

---

## Finding 4 - Per-Party rate-limit/concurrency attribution at regeneration

**Verdict: PARTIALLY CLOSED**

AD-21 now states: "`AdmitCall`'s per-Party accounting is keyed by `OnBehalfOfPartyId` per attempt (AD-30) — the caller for the first generation, the requesting Approver for each regeneration — never by `CallerPartyId` for the interaction's whole lifetime, and each attempt's charge releases independently, as soon as that attempt's `ProviderAttempt` reaches a terminal outcome under AD-13, regardless of whether the interaction itself has terminated."

The **keying** half of the v2 collision is fully closed: the AD now explicitly rules out Unit A's "one economic unit keyed by `CallerPartyId` for the interaction lifetime" reading — a builder can no longer defensibly key `AdmitCall`'s per-Party window by the interaction owner. Both prior units now converge on Unit B's `OnBehalfOfPartyId`-per-attempt model, and AD-30's `OnBehalfOfPartyId` definition ("is the Party charged by per-Party limits") is consistent with it.

**Remaining gap — the release trigger.** The prompt's own phrase, "released independently... when that attempt's `ProviderAttempt` reaches a terminal outcome under AD-13," does not name an unambiguous mechanism, specifically for the `Indeterminate` case:

- AD-13 itself only uses "terminal outcome" once, in "A retry before a terminal outcome is authorized only when outcome lookup ... confirms no usage," immediately followed by "a transport failure or timeout without that confirmation resolves the attempt to `Indeterminate` and is never retried" — which reads naturally as *Indeterminate is itself a terminal outcome* for that attempt id (no more retries are ever authorized against it).
- AD-21's own adjacent Indeterminate handling reads the opposite way: "an `Indeterminate` outcome holds the reservation for a configured period (default 24 hours, range 1 to 72) ... and then settles `ChargedAtMaximum`," i.e. an `Indeterminate` attempt is explicitly *not done* — it remains open, pending, and unsettled for up to 72 hours.

**Unit A** ("Cost Reservation & Rate Limits — `AdmitCall` release" story) reads AD-13's "resolves the attempt to `Indeterminate` ... never retried" as the terminal event and releases the `OnBehalfOfPartyId`'s concurrency/rate-limit charge immediately on `Indeterminate`, independent of budget settlement.

**Unit B** ("settlement reconciliation" story, built against AD-21's own vocabulary for the same word) treats "terminal outcome" for an attempt as not reached until the attempt's `BudgetLedger` fate is final (actuals settlement or `ChargedAtMaximum`), since that is the only place AD-21 (the very AD stating the release rule) itself defines finality for an `Indeterminate` attempt, and keeps the Approver's concurrency slot occupied for the full 24–72 hour hold window.

**Collision:** Under Unit A, an Approver whose regenerations frequently return `Indeterminate` (e.g. a flaky Provider) is never meaningfully rate-limited by `MaxConcurrentNonterminalInteractionsPerParty`, defeating the control exactly when Provider uncertainty is highest. Under Unit B, that same Approver is locked out of further regeneration for up to three days per stuck attempt — an operationally severe, self-inflicted denial-of-service on the Approver role. AD-13 never enumerates a closed set of "terminal `ProviderAttempt` outcomes" the way AD-5 enumerates terminal `ProposedAgentReplyState`s, so neither unit is textually wrong. Recommend AD-21 (or AD-13) state explicitly whether `Indeterminate` counts as a terminal outcome for `AdmitCall`-release purposes, independent of the separate `BudgetLedger` reservation settlement timeline.

---

## Finding 5 - Kill-switch retry-window pause/resume accounting

**Verdict: CLOSED**

AD-12 now defines the stored quantity precisely: "suspends `PostingFailed` and administrative retries with the retry-window clock paused — the affected proposal accumulates a `PausedDuration` recorded at pull and updated at release, and AD-5's 15-minute retry-window deadline is `origin + window + PausedDuration` evaluated under AD-28, so paused time is never counted against the audited retry bound." AD-5 mirrors this exactly: "a deadline computed as origin plus window plus the AD-12 kill-switch `PausedDuration`, since AD-12 pauses this clock while the tenant kill switch is pulled."

This is precisely the suggested fix from v2: a named, accumulating field (`PausedDuration`), a stated update trigger (recorded at pull, updated at release — supporting repeated pause/resume cycles by accumulation), an explicit formula, and an explicit cross-reference from AD-5 to AD-12 so a builder working from AD-5 alone cannot miss the pause behavior. No residual conflict with AD-28's "a deadline ... is derived once ... and written into the payload" rule was found: AD-28 names the retry-window **start** (the fixed origin instant) as the once-derived value, not the deadline itself; `PausedDuration` is a separate, explicitly mutable duration field, and the deadline is a value computed at evaluation time from the fixed origin, the fixed window, and the current `PausedDuration` — consistent with, not contradicting, AD-28's `EvaluationInstant` machinery. Retries are suspended entirely while the kill switch is pulled, so no deadline evaluation ever happens mid-pause with a stale `PausedDuration`.

---

## Finding 6 - `ConversationAgentState` index synchronization owner and automatic-mode `PostingFailed` terminality

**Verdict: PARTIALLY CLOSED**

The terminality contradiction is fully fixed. AD-5 no longer calls automatic-mode `PostingFailed` terminal: "then records `PostingFailed` on `AgentCallOperationStatus` — **non-terminal per the state list above**, and remaining in the AD-7 `ConversationAgentState` non-terminal-proposal index until an Approver, the Tenant Agent Administrator, or the system resolves it — and automatic mode itself attempts no further automatic retry." Unit A and Unit B from v2 (confirmation-path builder vs. automatic-posting builder) now read the identical, unambiguous instruction: automatic-mode `PostingFailed` is non-terminal and belongs in the index.

AD-7 also now names an owner and a key: "The confirmation/proposal workflow orchestration maintains `ConversationAgentState`'s non-terminal-proposal index idempotently alongside every `AgentInteraction` proposal-state transition, keyed by `AgentInteractionId`, explicitly including automatic-mode `PostingFailed`; the index never mirrors `Posted`, `Rejected`, `Abandoned`, or `Expired`."

**Remaining gap — the named owner is the wrong capability row.** The Capability-To-Architecture Map treats "Automatic response posting" and "Confirmation/proposal workflow" as two separate rows with two separate `Governed By` AD lists:

- `Automatic response posting | ... | AD-5, AD-6, AD-7, AD-13, AD-18`
- `Confirmation/proposal workflow | ... | AD-4, AD-5, AD-8, AD-13, AD-18, AD-28`

AD-7 is listed under **Automatic response posting**, not under **Confirmation/proposal workflow** — yet AD-7's own new sentence assigns index-maintenance duty to "the confirmation/proposal workflow orchestration." A story built strictly from the map (the document whose stated purpose is telling each capability/story which ADs govern it) would put automatic-mode index synchronization in the **Automatic response posting** story, because that is the only row that cites AD-7 at all; the **Confirmation/proposal workflow** story's own governing-AD list never cites AD-7, so nothing in that story's binds tells its builder to implement the index-maintenance rule AD-7 assigns to it by name.

**Unit A** ("Automatic response posting" story, built from its own map row AD-5/AD-6/AD-7/AD-13/AD-18): implements AD-7's index-maintenance rule itself for automatic-mode `PostingFailed`, since AD-7 is the only AD in its own binds list that mentions the index at all, and the story has no visibility into a "confirmation/proposal workflow orchestration" it isn't built to depend on.

**Unit B** ("Confirmation/proposal workflow" story, built from its own map row AD-4/AD-5/AD-8/AD-13/AD-18/AD-28, which never cites AD-7): never implements AD-7's index-maintenance rule at all — it has no textual instruction to, since AD-7 isn't in its binds list — but AD-7's literal wording ("the confirmation/proposal workflow orchestration maintains...") describes exactly this story as the owner.

**Collision:** If both stories are built independently against their own map-scoped AD lists, index maintenance for automatic-mode `PostingFailed` is implemented twice (Unit A, defensively, plus whatever Unit A assumes "the confirmation/proposal workflow orchestration" means) or never (Unit B never picks it up, and Unit A assumes a shared orchestration step it doesn't own actually exists) — reproducing the v2 "no defined synchronization owner" gap one level down, via a mismatch between AD-7's prose and the spine's own capability-ownership index rather than between AD-2/AD-3/AD-5/AD-7 directly. Recommend either updating the Capability-To-Architecture Map to add AD-7 to the "Confirmation/proposal workflow" row, or rewording AD-7 to name a shared orchestration step used by both flows rather than "the confirmation/proposal workflow orchestration."

---

## New Finding A - AD-2's carve-out scope is broader than the AD-10 text it cites (MEDIUM)

AD-2's new exception: "...except that a query scoped to an Agent's own **previously-snapshotted or currently-selected** `ProviderId`/`ModelId` may instead report `EntryMissing` or a reason-coded `Blocked` `ProviderReadinessResult` inside `AgentReadinessStatus`, per AD-10..."

AD-10's own text, which AD-2 cites as the source of this carve-out, only ever says: "`EntryMissing` is reported only for **the Agent's own selected entry** inside `AgentReadinessStatus`." AD-10 never uses or defines "previously-snapshotted." AD-4 corroborates that a real need exists for the broader scope — "Current Provider readiness and safe limits are re-evaluated under AD-10 and may tighten or block an in-flight interaction but never retarget it" — meaning an in-flight interaction's already-snapshotted `ProviderId`/`ModelId` (which, per AD-4, is never retargeted even if the Agent's live selection later changes) must remain queryable for readiness purposes independent of the Agent's *current* selection.

**Unit A** ("`AgentReadinessStatus` / `agent-setup`" story, built from AD-10's literal text): implements `EntryMissing`/`Blocked` reporting only for `Agent.ProviderSelection`'s current key; any query for an interaction's already-superseded snapshotted key falls back to the plain AD-2 absent-key response, since AD-10 — the AD that actually defines the `AgentReadinessStatus` contract and the only carve-out language it grants — never mentions "previously-snapshotted."

**Unit B** ("in-flight interaction re-validation" story, built from AD-2's literal carve-out and AD-4's re-evaluation requirement): expects a reason-coded `Blocked` response (not the bare absent-key sentinel) when an in-flight interaction's snapshotted Provider/Model becomes unavailable mid-flight, per AD-2's explicit "previously-snapshotted... may instead report `EntryMissing`."

**Collision:** Unit B's expected behavior (AD-4's promised tightening/blocking of an in-flight interaction whose provider entry disappears) is unreachable through Unit A's `AgentReadinessStatus` surface, which was built to the narrower scope AD-10 actually describes; the interaction re-check silently degrades to a generic "not found" instead of a reason-coded `Blocked`/`EntryMissing`. Recommend AD-10 be amended to match AD-2's wording ("the Agent's own previously-snapshotted or currently-selected entry"), or AD-2 be narrowed back to match AD-10 if the broader scope was not intended.

---

## New Finding B - AD-17's tenant-check-before-freshness sentence contradicts AD-10's Freshness-on-carve-out promise (MEDIUM-HIGH)

AD-17's new sentence: "The AD-2 tenant-ownership check on a keyed read always strictly precedes any `Freshness`/`RevisionLag` computation for that key, so **a foreign-tenant or absent key never reaches freshness evaluation** and cannot be distinguished from 'not found' by timing or staleness."

This was added in the same amendment pass as AD-2's carve-out, but was not itself updated to reference it. AD-10, unchanged in the relevant clause, requires the opposite for exactly the carve-out case AD-2 now names: "its public `ProviderReadinessResult` fixes `OperationalState`, `Callability`, safe `ReasonCode`, `CapabilityVersion`, `ObservedAt`, `ValidUntil`, **and AD-17 `Freshness`**" — stated unconditionally for every `ProviderReadinessResult`, including the `Blocked`/`EntryMissing` carve-out result for an Agent's own not-enabled or absent entry. An AD-10 carve-out key is, by construction, an "absent" (or not-enabled) key under AD-2's definition — the exact category AD-17's new sentence says must "never reach freshness evaluation."

**Unit A** ("provider-catalog / generic freshness gate" story, built from AD-17's literal, unconditional sentence): implements a single shared tenant-ownership-then-freshness gate reused by every Agents keyed read, short-circuiting Freshness computation for any absent-or-not-enabled key with no exception, because AD-17's sentence states the rule as an unconditional platform-wide security/timing invariant with no carve-out.

**Unit B** ("`AgentReadinessStatus`" story, built from AD-10's literal, unconditional Freshness requirement): always attaches a computed `Freshness` value to every `ProviderReadinessResult`, including the AD-2/AD-10 carve-out's `Blocked`/`EntryMissing` result for the Agent's own not-enabled entry, because AD-10 states this with no exception either.

**Collision:** If Unit A's shared gate is reused (a natural design, since AD-17 phrases the rule generically for "a keyed read," not scoped to non-readiness queries), Unit B's carve-out result can never legally acquire the `Freshness` value AD-10 promises it — the two ADs, both freshly touched by the same amendment pass that fixed Finding 2, now directly disagree about whether the AD-10 carve-out path is allowed to reach freshness evaluation at all. Recommend AD-17's sentence gain the same exception AD-2 now carries, e.g.: "...except a read scoped to the AD-2/AD-10 carve-out, whose `Blocked`/`EntryMissing` result computes `Freshness` as AD-10 requires."

---

## Verified consistent (no new finding)

- **AD-7 ↔ AD-2/AD-8/AD-30** (PartyId-reference-only cross-references): AD-2's "Party identity link," AD-8's "predefined `PartyId`s," and AD-30's `PartyId`/`OnBehalfOfPartyId`/`CallerPartyId` fields all match the vocabulary AD-7 cites. No divergence found.
- **AD-14's instrumentation-exclusion sentence** ("this exclusion is part of the AD-17 execution-state content sweep"): AD-17 and AD-27 both independently use the phrase "execution-state content sweep(s)" for the same test surface; the cross-reference is consistent, if broad.
- **AD-22's widened justification-family list**: AD-22's nine named families (`ProposalResolution`, `PolicyPublication`, `TenantBudgetUpdate`, `LegalHold`/`LegalHoldRelease`, `ExportRequest`/`ExportDownload`, `DeletionRequest`, `ProviderCatalogMutation`/`TenantProviderEnablement`, `AgentSetupMutation`/`TenantKillSwitch`, `AgentActivation`) are a verbatim, complete match against AD-12's lock-bearing-family list, in the same order, same count (nine). No divergence found.
- **AD-30's User-principal freshness sentence** ("mirroring the `Administrator` freshness rule below"): the `Administrator` clause does appear later in the same rule paragraph and states an equivalent fresh-per-command Tenants-projection check ("never from JWT roles alone" / "never trusted from a cached or token-embedded role claim alone"). Consistent.

---

## Summary Table

| # | v2 Finding | Verdict | Note |
| - | ---------- | ------- | ---- |
| 1 | No canonical payload-fingerprint mapping (AD-29) | Partially closed | Ordering/style ambiguity closed; DTO-level presence representation (explicit-null vs. absent) for optional Contracts properties still unspecified |
| 2 | AD-2 vs AD-10 absent-key contradiction | Closed | AD-2 now grants the exact carve-out AD-10 requires |
| 3 | Version-counter independence unstated | Closed | AD-4 states three independent counters and the exact bump rule for each |
| 4 | Per-Party attribution ambiguous at regeneration | Partially closed | Keying (`OnBehalfOfPartyId` per attempt) fully resolved; "terminal outcome" release trigger still ambiguous for the `Indeterminate` case |
| 5 | Kill-switch retry-window pause accounting undefined | Closed | AD-12's `PausedDuration` field, update rule, and AD-5 deadline formula are explicit and mutually consistent |
| 6 | `ConversationAgentState` index owner / automatic-mode terminality | Partially closed | Terminality contradiction fully fixed; AD-7 names an owner ("confirmation/proposal workflow orchestration") the spine's own Capability-To-Architecture Map excludes from AD-7's governed capabilities |

| New # | Finding | Severity | ADs |
| - | ------- | -------- | --- |
| A | AD-2's carve-out scope ("previously-snapshotted or currently-selected") is broader than AD-10's text ("the Agent's own selected entry"), leaving the in-flight-interaction re-check case unsupported by AD-10's actual contract | Medium | AD-2, AD-4, AD-10 |
| B | AD-17's new tenant-check-before-freshness sentence has no carve-out for the AD-2/AD-10 exception, directly contradicting AD-10's unconditional "every `ProviderReadinessResult` ... fixes ... `Freshness`" for the carve-out's `Blocked`/`EntryMissing` result | Medium-High | AD-17, AD-2, AD-10 |
