# Adversarial Divergence Review v6 — ARCHITECTURE-SPINE.md (Hexalith Agents)

**Reviewer pass date:** 2026-09-09 (independent re-derivation of the v5 fix batch — memlog entry 59 — plus an unscoped adversarial sweep of AD-5/AD-7/AD-8/AD-10/AD-12/AD-13/AD-21/AD-22/AD-29/AD-30)

**Verdict: FAIL** — v5's Critical (AD-5's `PostingFailed`-only unavailable-lookup clause) and v5 H-1 (AD-22's vacuous TAA branch) are both genuinely closed on independent re-reading of the live text. v5's H-2 (hexa's own Party deactivation/replacement post-provisioning) is **not** genuinely closed: the fix text added to AD-7 asserts a per-side-effecting-step Party-state re-check that AD-12's own operative sentence — the one sentence in the spine that actually enumerates what every side-effecting step re-reads — never lists. One compliant, literal reading of AD-12 reproduces the exact silent-outage/silent-attribution-integrity gap v5 H-2 described, this time as a direct cross-AD misquote rather than a bare silence. This is reclassified and carried forward as **H-2 (reopened)** below. Two further new Highs/Mediums were found in freshly-touched AD-10/AD-12 text.

---

## Method

For each v5 finding I re-read the **current, live** AD text verbatim (not the memlog's description of the fix) and re-derived the logic myself. I then ran a fresh adversarial sweep over AD-5, AD-7, AD-8, AD-10, AD-12, AD-13, AD-21, AD-22, AD-29, and AD-30 — the pairs the task named as highest-risk — constructing, for each candidate, two independently-built "one level down" units that each follow their cited AD text to the letter yet diverge on an observable, non-interoperable outcome. `grep` was used against the live file to confirm no other clause elsewhere resolves each candidate ambiguity before writing it up.

---

## Verified Closures And Reopenings (re-derived from current text, not the memlog)

| v5 finding | Current AD text | Verdict |
| --- | --- | --- |
| C-1: AD-5's unavailable-lookup clause named `PostingFailed` as the only fallback, forcing an illegal `Approved -> PostingFailed` transition | AD-5: "An unavailable lookup refuses the exit and the proposal stays in its pre-check state — `PostingFailed` for a `PostingFailed`-origin exit, `Approved` for the `Approved`-origin system-abandon check — so a removal detected against an `Approved` proposal can never force the illegal `Approved` -> `PostingFailed` transition; it is re-evaluated on the next post attempt or membership re-check instead." | **Closed.** The outcome is now explicitly state-agnostic and the illegal transition is named and foreclosed in the same sentence. No residual ambiguity. |
| H-1: AD-22's "Tenant Agent Administrator when outside that set" branch was permanently unreachable on the same-referent reading | AD-22: "the subject set is every Party recorded as caller, editor, Approver, decision actor, or Conversation Facilitator on any proposal or Conversation in scope, plus the specific Tenant Agent Administrator principal(s) whose `ConfigurationVersion` was in force for any in-scope call — a historical fact about who held the role during the calls, not about who holds it now. The second party is the Tenant Agent Administrator currently holding the role at inspection time when that current principal is not one of the historical TAA principals recorded in the subject set (the common case after TAA turnover)..." | **Closed.** Set-membership is now explicitly historical-PartyId while second-party selection is explicitly a live role lookup — exactly the fix v5 prescribed. The branch is reachable on TAA turnover, and vacuously reachable (every call) when no historical TAA is recorded at all (e.g. a `ProvisionHexa`-only tenant never edited by a TAA), since "not one of []" is trivially true. |
| L-1: `AuditInspection`'s "case" scope value used but never defined | AD-22: "...scoped to a named Conversation or a case — an Inspector-assigned free-text identifier grouping multiple named Conversations under one inspection, always wider than one Conversation and so always requiring Platform Operator approval below..." | **Text-closed, model still open.** The prose now defines "case." The `AuditInspection` classDiagram entry (`{ TenantId, InspectionId, Mode }`) and the Naming table still carry no `Scope`/`Case` field, so the definition exists in prose only. Carried forward as Low (see L-1 below) rather than reopened at higher severity, since it is not adversarially exploitable (every wider-than-one-Conversation inspection is swept into the mandatory Platform-Operator path regardless). |
| H-2: hexa's own Party deactivation/replacement post-provisioning was unaddressed by AD-2/AD-7/AD-30, and AD-12's "Party state" clause didn't say whether it covers the Agent's own identity | AD-7 now states: "...a deactivation or record change at the Parties adapter surfaces only through the AD-12 Party-state gate, which explicitly covers the Agent's own identity link on the same terms as any caller's — a resolution failure, missing record, or deactivated state on `hexa`'s linked `PartyId` fails every side-effecting step closed tenant-wide..." — but AD-12's own operative sentence reads: "Every side-effecting step (Provider invocation, proposal creation, edit, regeneration, approval, posting) re-reads Agent lifecycle, tenant enablement, the per-Conversation block, and the per-tenant kill switch." **Party state never appears in this list**, for either the caller or `hexa`. | **Not genuinely closed — reopened, sharper, as H-2 below.** AD-7's fix asserts something about AD-12 that AD-12's own text does not say. |

---

## High

### H-2 (reopened from v5, sharper). AD-7's Party-liveness-gate fix asserts a per-side-effecting-step re-check that AD-12's own enumerated re-read list omits — a compliant reading still leaves a deactivated `hexa` identity undetected forever

AD-12 has two adjacent sentences that a builder must reconcile:

> "Authorization gates run before every side effect and fail closed on missing, stale, ambiguous, disabled, or unavailable dependency state. Tenant access comes from Agents' local Tenants projection; conversation access and context from Conversations authorized queries; **Party state from Parties adapters/projections**; provider readiness from the AD-10 view; approver rights from the AD-8 predicate on the snapshot policy; role rights from the FR-33 matrix carried by the AD-30 principal. **Every side-effecting step (Provider invocation, proposal creation, edit, regeneration, approval, posting) re-reads Agent lifecycle, tenant enablement, the per-Conversation block, and the per-tenant kill switch.**"

The first sentence names Party state as one of several data sources feeding "authorization gates [that] run before every side effect." The second sentence is the spine's only concrete statement of *what actually gets re-read on every side-effecting step*, and it is a closed, four-item list — Agent lifecycle, tenant enablement, per-Conversation block, kill switch. Party state (caller's or `hexa`'s own) is not one of the four.

AD-7's fix, added this round to close v5 H-2, reads:

> "`hexa`'s own Party identity, once provisioned, is never re-read from Parties as a liveness check; a deactivation or record change at the Parties adapter surfaces only through the AD-12 Party-state gate, **which explicitly covers the Agent's own identity link on the same terms as any caller's** — a resolution failure, missing record, or deactivated state on `hexa`'s linked `PartyId` fails every side-effecting step closed tenant-wide..."

This sentence's entire claim rests on AD-12 "explicitly" running a Party-state re-check "on every side-effecting step." AD-12's own operative sentence — the one that actually says what runs on every side-effecting step — never names Party state at all, for anyone. AD-7's fix cites a guarantee AD-12 does not, in its own text, make.

**Two-unit scenario:**
- **Unit A (whole-rule reading):** treats AD-12's first sentence ("Authorization gates run before every side effect... Party state from Parties adapters/projections...") as the operative contract and the second sentence's four-item list as merely the most operationally salient subset (the four values that also drive retry-pause and lock-family semantics elsewhere in the spine). Unit A additionally wires a live Party-state read (both caller and `hexa`'s own `PartyId`) into the authorization-gate battery invoked before every side-effecting step, exactly as AD-7's new sentence claims. A later Parties-side deactivation of `hexa`'s backing Party fails every subsequent side-effecting step closed tenant-wide, surfaced as `MissingPartyIdentity`.
- **Unit B (enumerated-list reading, the far more natural reading of a sentence that opens with "Every side-effecting step... re-reads" and then closes with a four-item list and a full stop):** implements AD-12's per-step re-check battery as literally and exactly those four booleans — Agent lifecycle, tenant enablement, per-Conversation block, kill switch — because that is the only sentence in the entire spine that states, operationally, what a side-effecting step's gate battery consists of. Party state is checked only where a *different* AD explicitly names a checkpoint for it: AD-7's activation-time `MissingPartyIdentity` blocker (an Agent-activation-time check, not a per-side-effecting-step one) and ARCH-A-2's ingress-time caller-`PartyId` resolution. Under Unit B, once `hexa` is activated, no later side-effecting step ever re-reads `hexa`'s own Party liveness, because the concrete per-step checklist Unit B implements — matching AD-12's own words — has no slot for it. A Parties-level deactivation of `hexa`'s Party after activation is never detected by anything in Unit B; `hexa` keeps generating and posting Conversation Messages attributed to a deactivated identity indefinitely.
- Both units are fully compliant with AD-7's and AD-12's literal text. They produce opposite outcomes for the exact governance scenario v5 H-2 raised and this round's fix claimed to close: Unit A's tenants get an unexplained, unaudited outage the moment `hexa`'s Party is deactivated; Unit B's tenants never notice, and keep attributing posted messages to an identity Parties itself no longer considers active — the precise data-governance/attribution-integrity gap the PRD's Party-ownership split (Parties owns identity/PII, Agents only references it) is designed to prevent.

**Fix:** Add Party state (both the caller's and `hexa`'s own linked `PartyId`) to AD-12's own four-item enumerated list — "Every side-effecting step... re-reads Agent lifecycle, tenant enablement, the per-Conversation block, the per-tenant kill switch, **and Party state (caller and, for the Agent's own identity, `hexa`'s linked `PartyId`)**" — so AD-7's claim is something AD-12 actually says, rather than something AD-7 says about AD-12.

### H-3 (new). AD-12's disabled-Agent clause never mentions `PostingFailed`, unlike its own parallel kill-switch clause — leaving retry-blocking and retry-window pause semantics for a disabled Agent unstated, and AD-5's `PausedDuration` formula names only the kill switch

AD-12 states the disabled-Agent consequence set explicitly and exhaustively by state:

> "A disabled Agent blocks new side effects for its proposals: awaiting proposals may be rejected or abandoned but never approved, edited, or regenerated; an `Approved` proposal cannot begin posting; and only a `PostingPending` attempt already in flight completes or fails on its own terms."

Three proposal states are named — awaiting-decision, `Approved`, `PostingPending` — and each gets a specific, different consequence. `PostingFailed` is conspicuously absent from this list. Compare the kill-switch clause a few sentences later, which explicitly and separately states the `PostingFailed` consequence, including a compensating clock adjustment:

> "`PostingFailed` automatic and administrative retries are suspended and their retry-window clock is paused — the affected proposal accumulates `PausedDuration` recorded at pull and updated at release, and AD-5's deadline is `origin + window + PausedDuration` under AD-28."

AD-5's own deadline formula names only this one source of pause: "a deadline computed as origin plus window plus the AD-12 kill-switch `PausedDuration`, since AD-12 pauses this clock while the tenant kill switch is pulled." There is no equivalent term for Agent-disabled time anywhere in AD-5, AD-12, or AD-21.

**Two-unit scenario:**
- **Unit A (broad fail-closed reading, following AD-4's general re-read rule — "every side-effecting step re-reads [Agent lifecycle]... and fails closed" — and AD-12's opening sentence that authorization gates "fail closed on... disabled... dependency state" for "every side effect," which explicitly includes "Provider invocation" and "posting"):** blocks automatic and administrative `PostingFailed` retries while the Agent is disabled, on the theory that a retry is itself a new Provider invocation / posting side effect. Because neither AD-5 nor AD-12 defines a `PausedDuration`-style clock adjustment for Agent-disabled time, Unit A does **not** pause the 15-minute/3-attempt retry-window clock during the disablement window. A Tenant Agent Administrator who disables `hexa` for 20 minutes (e.g. during a maintenance window) to prevent new calls, then re-enables it, returns to find in-flight `PostingFailed` proposals have silently blown past their audited retry deadline while blocked — they are no longer retriable and must instead be abandoned, an operational side effect of a routine disable/enable cycle that nothing in the spine warns about or names.
- **Unit B (enumerated-list reading, the same literal-text discipline applied to H-3's own controlling sentence):** treats AD-12's disabled-Agent clause as exhaustive of its consequences precisely because it is written as a closed, state-by-state list with no catch-all — `PostingFailed` retries are therefore **not** blocked by Agent disablement at all; automatic and administrative retries keep firing (and can keep succeeding, posting content) for a Conversation whose owning Agent is administratively disabled, because disablement was never named as one of PostingFailed's blocking conditions the way the kill switch explicitly was.
- Both units are literal, good-faith implementations of the same clause. They diverge on whether disabling an Agent silently starves a `PostingFailed` proposal's retry budget (Unit A) or lets it keep posting under a disabled Agent (Unit B) — materially different operational and safety behavior for the same administrative action, and neither is contradicted by any other spine text.

**Fix:** State the `PostingFailed` consequence of Agent disablement explicitly, on the same terms as the kill-switch clause — either "disabling an Agent has no PostingFailed-retry effect; only the kill switch pauses/suspends retries" (if that is the intent), or extend the same suspend-and-pause mechanics (and AD-5's `PausedDuration` formula) to cover Agent-disabled time as well as kill-switch-pulled time.

---

## Medium

### M-1 (new). AD-10's widened `EntryMissing` carve-out claims exact equivalence with AD-2's carve-out while adding an "in-flight" restriction AD-2's own text does not contain

AD-2 states the carve-out from the absent-key rule as:

> "...except that a query scoped to an Agent's own **previously-snapshotted or currently-selected** `ProviderId`/`ModelId` may instead report `EntryMissing` or a reason-coded `Blocked` `ProviderReadinessResult` inside `AgentReadinessStatus`, per AD-10 — the one named carve-out from the absent-key rule."

AD-10 restates the same carve-out, added this round specifically to close a prior finding that the two ADs disagreed in scope:

> "`EntryMissing` is reported only for the Agent's own currently-selected entry, or **an in-flight interaction's** previously-snapshotted entry, inside `AgentReadinessStatus`/`ProviderReadinessResult` — matching AD-2's carve-out exactly, no wider."

AD-2's "previously-snapshotted" has no non-terminality qualifier at all — on its own words it covers any interaction's historical snapshot of that Agent's provider entry, terminal or not (e.g. an audit or history view re-querying a long-completed call's now-removed catalog entry). AD-10's rewrite inserts "an in-flight interaction's," restricting the carve-out to non-terminal interactions only, while asserting this is "AD-2's carve-out exactly, no wider." It is not exactly AD-2's carve-out; it is narrower, by the interaction's terminality.

**Two-unit scenario:**
- **Unit A (AD-10-literal):** gates the `EntryMissing`/`Blocked` diagnostic on interaction non-terminality. A historical/audit query about a *terminal* interaction's snapshotted (now removed or disabled) `ProviderId`/`ModelId` gets the plain absent-key response — indistinguishable from a cross-tenant probe, with no diagnostic signal that the entry once existed.
- **Unit B (AD-2-literal):** implements the carve-out for any of the Agent's own previously-snapshotted entries regardless of the owning interaction's terminal state, since that is what AD-2 — the AD that actually states the general absent-key rule and its one carve-out — says. A historical query about the same terminal interaction gets the richer `EntryMissing`/reason-coded `Blocked` diagnostic.
- The two units produce different, observable API responses for the same audit/history query (relevant to AD-15/AD-17's audit-evidence and readiness surfaces), and each team can point to a currently-live AD sentence as sole authority for its behavior.

**Fix:** Either drop "in-flight" from AD-10's clause (matching AD-2's unqualified wording) or add the same qualifier to AD-2's clause (if the intent really is non-terminal-only) — the two ADs must use identical scope language, not merely both claim to.

---

## Low

### L-1 (carried from v5, narrowed). `AuditInspection`'s "case" now has a prose definition but no model representation

AD-22 now defines "case" inline ("an Inspector-assigned free-text identifier grouping multiple named Conversations under one inspection, always wider than one Conversation and so always requiring Platform Operator approval"), closing v5 L-1's complaint that the term was undefined. The `AuditInspection` classDiagram entry (`TenantId`, `InspectionId`, `Mode`) and the Naming table still carry no `Scope`/`Case` field, so a builder reading only the model section has no hook for storing it. Not adversarially exploitable (every case-scoped inspection is already swept into the mandatory Platform-Operator path regardless of how "case" resolves), so this stays Low.

**Fix:** Add a `Scope` field to the `AuditInspection` classDiagram entry (e.g. `Scope: ConversationId | CaseId`).

---

## Summary Table

| # | Severity | Title | ADs in tension |
| --- | --- | --- | --- |
| H-2 (reopened) | High | AD-7's Party-liveness-gate claim cites an AD-12 per-step re-check AD-12's own enumerated list never states, on the enumerated-list reading `hexa`'s deactivated Party identity is never re-detected | AD-7 vs. AD-12 |
| H-3 | High | AD-12's disabled-Agent clause is silent on `PostingFailed`; retry-blocking and retry-window-pause semantics for a disabled Agent are unstated, and AD-5's `PausedDuration` names only the kill switch | AD-12 vs. AD-5 |
| M-1 | Medium | AD-10's "in-flight interaction's previously-snapshotted entry" carve-out claims exact equivalence with AD-2's unqualified "previously-snapshotted" wording but is textually narrower | AD-2 vs. AD-10 |
| L-1 | Low | `AuditInspection`'s "case" is now defined in prose but has no classDiagram/Naming-table field | AD-22 vs. (model) |

### Reconciled against v5

| Ref | Verdict this round |
| --- | --- |
| v5 C-1 (AD-5 illegal `Approved -> PostingFailed`) | **Closed**, re-derived from live text. |
| v5 H-1 (AD-22 TAA branch unreachable) | **Closed**, re-derived from live text. |
| v5 H-2 (hexa Party deactivation post-provisioning) | **Not closed — reopened as H-2 above**, sharper: the fix text now makes an affirmative, textually unsupported claim about AD-12 rather than leaving a bare silence. |
| v5 L-1 (`AuditInspection` "case" undefined) | Prose definition **closed**; classDiagram/Naming-table representation still open, carried forward at Low. |

No finding in this pass rests on intent-level disagreement or stylistic preference; each cites exact, quoted, currently-live spine text — confirmed by full-text search to have no reconciling clause elsewhere — that a literal, good-faith implementer would follow into a materially different, non-interoperable build from another equally literal implementer.
