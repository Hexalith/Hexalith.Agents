# Adversarial Divergence Review v5 — ARCHITECTURE-SPINE.md (Hexalith Agents)

**Reviewer pass date:** 2026-09-09 (independent re-derivation of the just-applied AD-5/AD-12/AD-22/AD-13/AD-21 fixes; v4's FAIL is not trusted as resolved without re-reading the live AD text)
**Verdict: FAIL** — the round-2 fix to AD-5's abandon/retry-exit machinery leaves a self-contradiction inside its own rewritten sentence (a literal reading forces an illegal `Approved -> PostingFailed` transition that AD-5's own state table forbids), and AD-22's rewritten computed-subject-set model contains a clause that is either meaningful or permanently vacuous depending on which of two equally-defensible readings of "the Tenant Agent Administrator" a builder picks. The five specifically re-attacked closures (AD-5's mode/`PostingFailed` fix at C-4, AD-13/AD-8's Eligible-Approver mode-scoping, AD-21's `authorization`-wording swap, AD-7/AD-2/AD-30's `ProvisionHexa` agreement) are otherwise verified genuinely closed on their own terms — the two new Criticals/Highs below are freshly introduced or freshly exposed by this round's edits, not restatements of C-1/C-2/C-4/C-5 from v4.

---

## Method

For each candidate I read the **current, live** AD text (not the memlog's description of the fix) and constructed two concrete "one level down" units — e.g. two independently built proposal-lifecycle command handlers, or two independently built compliance-inspection services — that each follow their cited AD text to the letter and pass a naive review against every other AD, yet diverge on an observable, non-interoperable outcome. I re-verified, by reading the current AD text directly, every fix the task asked me to re-attack (AD-5, AD-13/AD-8/sequence diagram, AD-21, AD-22, AD-7/AD-2/AD-30), then ran an unscoped pass over the remainder of the 770-line spine, focused on the areas the update pass's other edits (AD-2's `ConversationAgentState` state machine, AD-12's kill-switch rewrite, AD-30's principal rules) touch or border.

---

## Verified Closures (re-derived from current text, not the memlog)

| Prior finding | Current AD text | Verdict |
| --- | --- | --- |
| C-1 (v4): Eligible Approver resolution universal vs. Confirmation-mode-only | AD-13: "Eligible Approver resolution runs in every response mode, not Confirmation mode only — an Automatic-mode Agent with zero configured Approvers is rejected up front with `NoEligibleApprover`..."; sequence diagram line 382: `EligibleApprover resolution (both modes)`; AD-8's "call time (typed `NoEligibleApprover` before Provider work)" carries no mode qualifier, consistent with the now-explicit AD-13 text | **Closed.** All three locations (AD-13, AD-8, diagram) now state the same unconditional rule; no textual fork remains. |
| C-2 (v4): AD-21 reusing "authorization" for AD-13 step 1's term | AD-21: "...before safety, admission, and the AD-13 step-4 `ProviderInvocationAuthorized` event, one atomic `BudgetLedger` command reserves..." | **Closed.** The bare word "authorization" is gone; the clause now names the exact event (step 4), and AD-13's own numbered list confirms reservation (its step (2), immediately after context measurement) runs before admission acquisition (step (3)) and before `ProviderInvocationAuthorized` (step (4)), never before step 1's authorization gate. Sequence diagram ordering (`reserve estimated cost` before `Safety`, before `Capacity`, before `append ProviderInvocationAuthorized`) matches exactly. No residual "which authorization" ambiguity. |
| H-6/H-1 (v4): `AgentReadinessStatus` enum-vs-composite shape | AD-10: "`AgentReadinessStatus` (AD-15, AD-17) is a composite wrapper, never a bare enum: it carries the selected entry's `ProviderReadinessResult` as one field and the Agent-level growth states ... as sibling fields alongside it, not as alternate values of the same type." | **Closed.** AD-10, AD-15, and AD-17 now describe one shape. |
| H-7/H-2 (v4): `CurrencyMismatch` claimed to live in AD-10 but absent from its text | AD-10's `Blocked` enumeration now lists "currency-mismatched (`CurrencyMismatch`, AD-21)" explicitly | **Closed.** |
| M-1 (v4): Conversation block/clear command had no lock-bearing-family assignment | AD-12: "`AgentSetupMutation` (also `TenantKillSwitch`, and the block/clear commands that set or clear `hexa`'s Conversation block)" | **Closed.** |
| AD-7/AD-2/AD-30 Party-identity agreement (task item 5, first half) | AD-2: create-only `ProvisionHexa`, "no tenant role may create a second Agent, delete `hexa`, or replace its Party identity"; AD-7: "no principal, including the Tenant Agent Administrator, can link, replace, or clear `hexa`'s Party identity after provisioning in V1"; AD-30: "`ProvisionHexa` is idempotent ... cannot carry ... Party-replacement mutations" | **Closed for initial provisioning.** All three ADs now agree hexa's identity is create-only with no live re-link/replace path. (See new Finding C below for the residual seam the task also asked about — Party deactivation *after* provisioning is still unaddressed by all three.) |

---

## Critical

### C-1 (new). AD-5's rewritten `PostingFailed`-exit mechanics don't cover the `Approved`-exit case they were just extended to gate, forcing an illegal state transition on one code path

This round's headline AD-5 fix (memlog: "system abandonment is now legal from `Approved` and `PostingFailed` on a detected removal... Before any exit from `PostingFailed`... a `MessageId` lookup decides whether the post landed") widened the abandon rule to fire from `Approved`, but the procedural machinery describing *how* that lookup behaves was written — and stays scoped — to `PostingFailed` alone.

The rule states, first, that the widened transition is gated by the lookup:

> "...and, per AD-7's membership re-check, now also fires from `Approved` and `PostingFailed` on a detected removal or block, not from `PostingFailed` alone: the **`Approved`/`PostingFailed` -> `Abandoned`** (`RemovedInConversations`) transition runs only after **the `MessageId` lookup below** finds no posted message."

Then the referenced "lookup below" is introduced and scoped explicitly to one state:

> "Posting is at-least-once, so before every retry attempt and **before any exit from `PostingFailed`** — a human or administrative abandon, an automatic or administrative retry, or the system's own `RemovedInConversations`/`SourceConversationUnavailable` abandonment — the system looks the approved version's `MessageId` up..."

And its unavailable-outcome clause names one specific fallback state:

> "...an unavailable lookup refuses the exit **and the proposal stays `PostingFailed`**."

Read literally, the "lookup below" that the first sentence requires for the `Approved -> Abandoned` transition is the same lookup whose own text only ever talks about exits *from* `PostingFailed`, and whose unavailable-outcome branch has exactly one named result: "stays `PostingFailed`." A proposal in `Approved` state was never `PostingFailed` — AD-5's own state list requires each proposal to occupy "exactly one public `ProposedAgentReplyState`," and there is no state-table entry anywhere in AD-5 (or FR-18) for an `Approved` proposal transitioning to `PostingFailed` without ever having attempted a post (`PostingPending` is the only route into `PostingFailed`, and `PostingPending` "accepts no abandon at all and completes or fails on its own terms" — it is never entered by this membership-re-check path at all).

**Two-unit scenario:**
- **Unit A (literal-mechanics reading):** implements the unavailable-lookup branch exactly as written — a shared `HandleExitAttempt` code path that, on an unavailable `MessageId` existence read, unconditionally sets the proposal's state to `PostingFailed` and refuses the exit, applying this uniformly regardless of which state the exit was attempted from. When the system's `RemovedInConversations` abandon attempt fires against an `Approved` proposal (per AD-7's pre-post membership re-check) and the existence read is `Unavailable` (a real, producible EXT-CONV-AI-1 outcome, per AD-8's parallel "fails closed when absent, stale, or unavailable" pattern for the same kind of read), Unit A force-transitions the proposal from `Approved` directly to `PostingFailed` — a state AD-5's own list says is reachable only after an actual posting attempt, and one that now starts the AD-5 15-minutes/3-attempts retry-window clock for a proposal that never tried to post.
- **Unit B (state-preserving reading):** treats "the proposal stays `PostingFailed`" as shorthand for "the proposal stays in its pre-exit state" (the natural reading if the paragraph's own scope note — "before any exit from `PostingFailed`" — is taken as literally excluding the `Approved` case from this specific unavailable-outcome clause). On the same `Unavailable` result, Unit B simply aborts the abandon attempt and leaves the proposal at `Approved`, unchanged, to be re-attempted on the next pre-post re-validation.
- The two units produce different terminal states for the identical input (an `Approved` proposal, an external removal detected, an unavailable existence read) — one lands in `PostingFailed` with a running retry-window deadline and an `AgentCallOperationStatus` visible to operators as a posting failure that never happened; the other stays silently `Approved`. A monitoring dashboard, an SLA calculation, or a downstream `PostingFailed`-triggered notification built against one unit's behavior breaks against the other's.

**Fix:** Replace "the proposal stays `PostingFailed`" with a state-agnostic outcome, e.g.: *"an unavailable lookup refuses the exit and the proposal remains in its pre-exit state (`Approved` or `PostingFailed`, as applicable)"* — and add one clause confirming the `Approved`-triggered `RemovedInConversations` abandon attempt reuses this exact mechanics paragraph (currently only cross-referenced by the earlier sentence, never re-scoped by the paragraph itself) rather than defining a parallel, un-stated procedure for `Approved`.

---

## High

### H-1 (new). AD-22's computed-subject-set rule names "the Tenant Agent Administrator" twice for two different purposes, and the naive same-referent reading makes the fallback-to-TAA branch permanently unreachable

AD-22's rewritten second-party rule builds the subject set as:

> "the subject set is every Party recorded as caller, editor, Approver, decision actor, or Conversation Facilitator on any proposal or Conversation in scope, **plus the Tenant Agent Administrator whose `ConfigurationVersion` was in force for any in-scope call**"

and then selects the second party as:

> "the second party is **the Tenant Agent Administrator when outside that set**, and otherwise the Platform Operator or a second Compliance Inspector..."

Every in-scope call has *some* `ConfigurationVersion` in force, and by the subject-set clause's own wording that version's owning Tenant Agent Administrator is unconditionally added to the subject set for every in-scope call — there is no in-scope call for which this addition doesn't fire (V1 always has exactly one config-version-in-force per call, per AD-4). If "the Tenant Agent Administrator" in the second sentence refers to the same, singular entity just defined two clauses earlier, that entity is — by construction — *always* inside the set it is being tested against, making "when outside that set" a branch with no reachable input. Nothing in AD-22, AD-30, or AD-2 states that "the Tenant Agent Administrator" candidate for second-party selection is instead a *live, current* role lookup (independent of whichever specific Party's `ConfigurationVersion` happens to be recorded in the subject set) — which is the only reading that makes the "when outside that set" branch reachable (e.g., after tenant-admin turnover, where the *current* TAA differs from the historical config-setting TAA baked into the subject set).

**Two-unit scenario:**
- **Unit A (live-lookup reading, matching AD-30's "resolved... at every command ingress, never... a cached... claim" philosophy applied elsewhere to every other principal):** resolves "the Tenant Agent Administrator" for second-party purposes as a fresh role lookup at approval time, distinct from the specific PartyId recorded in the subject set by config authorship. Whenever the currently-serving TAA differs from whoever's `ConfigurationVersion` was in force during the in-scope calls (tenant admin turnover, or a tenant whose config was Platform-provisioned via `ProvisionHexa` and never edited by any TAA — meaning no TAA is added to the subject set by that clause at all), Unit A lets the current TAA serve as second party.
- **Unit B (single-referent reading):** treats "the Tenant Agent Administrator" in the second-party sentence as naming the exact same entity the subject-set clause just introduced — i.e., always inside the set by that clause's own construction — so the "when outside that set" branch never fires in Unit B's implementation. Every inspection routes to "the Platform Operator or a second Compliance Inspector," and the TAA is never selected as second party under any circumstance.
- The two units diverge on who is authorized to gatekeep a compliance inspection into their own tenant's Agent activity: Unit A sometimes routes it to a (different-person, turnover-safe) Tenant Agent Administrator; Unit B always escalates to the Platform Operator or a peer Inspector. This changes required staffing/availability (a small tenant with one Compliance Inspector and no Platform Operator on call cannot get any inspection approved under Unit B if that Inspector is themself in the subject set, whereas Unit A would legitimately let the tenant's own current TAA — if uninvolved — approve it), and it changes audit records: two independently-built compliance tools reviewing the same historical inspection would disagree on whether the recorded second-party role was policy-compliant.

**Fix:** Disambiguate the two uses explicitly, e.g.: *"...plus the Tenant Agent Administrator(s) whose `ConfigurationVersion` was in force for any in-scope call (by PartyId, however many held the role over the scope's time range); the second party is the **currently-serving** Tenant Agent Administrator when that current PartyId is outside the computed set, and otherwise the Platform Operator or a second Compliance Inspector."* This makes explicit that set-membership is evaluated by historical PartyId while second-party selection is a live role lookup — the only reading under which the TAA branch is ever reachable.

### H-2 (new). Party deactivation/replacement at the Parties-adapter level, after `hexa` is provisioned, is still unaddressed by AD-2, AD-7, and AD-30 — and AD-12's generic Party-state clause is ambiguous about whether it even applies to the Agent's own identity

The just-closed AD-7/AD-2/AD-30 agreement (verified above) covers only the *provisioning-time* path: it is unanimous that no principal can link, replace, or clear `hexa`'s Party identity through Agents commands after provisioning. None of the three addresses the case the PRD's brownfield reality already assumes is possible — Parties is a separately-owned module, and its own admin surface can deactivate, merge, or otherwise change the state of any Party record, including the AI-type Party Agents referenced at provisioning time, entirely outside any Agents command.

AD-12's authorization rule is the only clause in the spine that could plausibly cover this:

> "Authorization gates run before every side effect and fail closed on missing, stale, ambiguous, disabled, or unavailable dependency state. Tenant access comes from Agents' local Tenants projection; conversation access and context from Conversations authorized queries; **Party state from Parties adapters/projections**..."

This sentence never distinguishes "Party state" belonging to the *calling* human Party from "Party state" belonging to `hexa`'s *own* AI-type Party — both are `PartyId`-referenced the same way under AD-7's PartyId-reference-only rule, and AD-12 lists "Party state" as one undifferentiated authorization input alongside tenant/conversation/provider/approver state, all of which are per-request checks about the request's actors. AD-7's only stated check against a missing/dead Party is `MissingPartyIdentity`, and it is scoped explicitly as "the activation blocker" (i.e., checked when the Tenant Agent Administrator activates the Agent) — the text never says this check re-runs on every subsequent `AgentCallAcceptance` once the Agent is already active.

**Two-unit scenario:**
- **Unit A (Server team, "Party state" reads broadly):** implements AD-12's authorization-gate re-read to include a liveness check on `hexa`'s own `PartyId` on every side-effecting step (matching the sentence's plain, undifferentiated wording), alongside the caller's Party state. If Parties later deactivates the AI-type Party backing an already-active, already-provisioned `hexa` (e.g., a Parties-side data-cleanup or Party-merge operation unrelated to Agents), every subsequent `AgentCallAcceptance` for that tenant fails closed — Unit A's Agent becomes silently unusable with no Agents-side lifecycle event ever recorded (Agent lifecycle state stays `Enabled`; only calls start failing).
- **Unit B (a second Server team, "Party state" reads narrowly):** implements AD-7's `MissingPartyIdentity` as literally scoped — an activation-time-only check — and reads AD-12's "Party state from Parties adapters/projections" as referring solely to the calling human's Party (the natural counterpart to "tenant access," "conversation access," and "approver rights," which are all about the requester, not the Agent). Under Unit B, once `hexa`'s Party identity is set at provisioning, its liveness is never re-checked; an Agent whose backing Party was later deactivated at the Parties level keeps generating and posting Conversation Messages attributed to a deactivated identity indefinitely.
- Neither unit contradicts AD-2, AD-7, AD-30, or AD-12's literal text; they diverge on a scenario none of the four decisions actually names. The observable difference is stark: Unit A's tenants experience an unexplained, unaudited outage with no matching lifecycle event to diagnose it against; Unit B's tenants keep posting messages under a Party the Parties module itself no longer considers active — a data-governance and attribution-integrity gap the PRD's Party-ownership model (Parties owns identity/PII, Agents only references it) seems designed to prevent.

**Fix:** Add one sentence to AD-7 (the AD that already owns every Party-adjacent field) stating whether `hexa`'s own Party liveness is re-checked on every side-effecting step (matching AD-12's per-call re-read pattern for lifecycle/enablement/block/kill-switch) or only at activation, and, if re-checked, what the resulting typed outcome is (a new `AgentPartyUnavailable`-style blocker, or a reuse of `MissingPartyIdentity` outside its currently-stated activation-only scope) and whether it also durably disables the Agent (an AD-12-style lifecycle state) rather than failing calls silently forever.

---

## Low

### L-1 (carried context, sharpened). `AuditInspection`'s "case" scope value is used but never defined

AD-22 scopes a compliance inspection to "a named Conversation or case," and `any inspection wider than one Conversation requires Platform Operator approval regardless of the computed second party" — which does mean a multi-Conversation "case" scope is safely swept into the mandatory-Platform-Operator path regardless of how "case" is defined, so this is not adversarially exploitable the way H-1/H-2 are. But no AD, the classDiagram's `AuditInspection { TenantId, InspectionId, Mode }` (no `Scope` field at all), nor the Naming table defines what a "case" is, what identifies one, or how its Conversation set is bounded — a residual authoring gap worth closing before a team has to invent the concept unassisted.

**Fix:** Either give `case` a one-clause definition (e.g., an operator-defined named grouping of Conversation ids under one `InspectionId`) or drop the word and scope every inspection to an explicit Conversation-id set.

---

## Summary Table

| # | Severity | Title | ADs in tension |
| --- | --- | --- | --- |
| C-1 | Critical | AD-5's unavailable-lookup clause names `PostingFailed` as the only fallback state, forcing an illegal `Approved -> PostingFailed` transition on the literal reading | AD-5 (internal) |
| H-1 | High | AD-22's "Tenant Agent Administrator when outside that set" branch is vacuous under the same-referent reading of the subject-set clause | AD-22 (internal) |
| H-2 | High | `hexa`'s own Party deactivation/replacement at the Parties-adapter level post-provisioning is unaddressed; AD-12's "Party state" clause doesn't say whether it covers the Agent's own identity | AD-7, AD-2, AD-30 vs. AD-12 (gap) |
| L-1 | Low | `AuditInspection`'s "case" scope value is used but never defined | AD-22 vs. (absence) |

### Verified closed this round (re-derived from live text, not memlog claims)

| Ref | Title |
| --- | --- |
| v4 C-1 | Eligible Approver resolution: universal in both modes now, in AD-13, AD-8, and the sequence diagram alike |
| v4 C-2 | AD-21's reservation-ordering clause now names `ProviderInvocationAuthorized` (AD-13 step 4) explicitly, no longer the bare word "authorization" AD-12/AD-13 already claimed for step 1 |
| v4 H-1/adversarial H-6 | `AgentReadinessStatus` fixed as a composite wrapper in AD-10, AD-15, and AD-17 alike |
| v4 H-2/adversarial H-7 | `CurrencyMismatch` now named in AD-10's own `Blocked` enumeration |
| v4 M-1 | Conversation block/clear command now explicitly folded into the `AgentSetupMutation` lock-bearing family |
| task item 5 (initial provisioning) | AD-7, AD-2, and AD-30 agree hexa's identity is create-only with no live post-provisioning link/replace path |

No finding in this pass rests on intent-level disagreement or stylistic preference; each cites exact, quoted, currently-live spine text that a literal, good-faith implementer would follow into a materially different, non-interoperable build from another equally literal implementer.
