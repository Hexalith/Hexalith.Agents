# Adversarial Divergence Review v4 — ARCHITECTURE-SPINE.md (Hexalith Agents)

**Reviewer pass date:** 2026-09-09 (independent re-derivation; prior v3 "all closed" claim not trusted)
**Verdict: FAIL** — the spine contains at least two Critical-severity internal contradictions in its own AD text (not merely intent-level ambiguity) that let two literally-compliant builders produce incompatible units, plus High/Medium findings on public-contract shape and cross-reference staleness.

---

## Method

For each candidate, I constructed two concrete "one level down" units (e.g. two Server command-handler implementations, or a Server team and a UI/BFF team) that each read the cited AD text literally, follow it to the letter, and pass a naive review against every other AD — yet ship incompatible behavior or incompatible wire/contract shapes. Only findings with an exact textual root cause (quoted) are listed. I read the entire 759-line file, including both Mermaid diagrams, the Consistency Conventions table, the Stack table, and the Architecture Assumptions table.

---

## Critical

### C-1. "Eligible Approver resolution" — is it a universal acceptance step or Confirmation-mode-only?

**AD-13** (Idempotent External Effects) states the canonical FR-8 acceptance order with no mode qualifier:

> "Acceptance follows the FR-8 order (authorization, lifecycle, provider eligibility, rate limits, context measurement, reservation, pre-Provider safety, **Eligible Approver resolution**, membership) and ends with one `AgentCallAccepted` event..."

**AD-8** (Approver Policy Resolution) independently states the predicate is applied unconditionally at every stage including call time:

> "The Eligible Approver predicate ... is applied at configuration time ..., **call time** (typed `NoEligibleApprover` before Provider work), edit time ..., and approval time..."

**AD-5** further implies automatic-mode proposals also need a resolvable Eligible Approver later, since `PostingFailed` recovery depends on one existing:

> "the proposal persists until an **Eligible Approver** abandons it, the Tenant Agent Administrator administratively retries it or ... abandons it ..., or the system abandons it ... Automatic-mode posting shares the same bound..."

But the sequence diagram (the spine's own worked example of the exact same pipeline) explicitly scopes the step to Confirmation mode only:

> line 374: `Workflow->>Interaction: EligibleApprover resolution (confirmation mode)`

None of AD-8 or AD-13's prose carries a mode qualifier (unlike AD-20, which is careful to say "restricted content requires ... **Confirmation Response Mode**" when a rule really is mode-conditional). This is a genuine fork, not a diagram slip a careful reader can wave away — it changes observable business behavior.

**Two-unit scenario:**
- **Unit A (Server team, "strict FR-8" reading):** builds `AgentCallAcceptance` so that *every* call — Automatic or Confirmation mode — runs Eligible-Approver resolution as acceptance step 8, per AD-13's literal, unqualified order and AD-8's literal, unqualified "call time" binding. A tenant whose Agent is in Automatic mode with zero resolvable Approvers now gets **every automatic-mode call rejected** with `NoEligibleApprover` before a Provider is ever invoked.
- **Unit B (Server team, "diagram" reading):** builds the workflow so Eligible-Approver resolution is skipped entirely for Automatic-mode interactions (matching the diagram literally), relying on the Tenant Agent Administrator alone to clear any resulting `PostingFailed`. A tenant with zero configured Approvers can run Automatic mode calls freely; failures pile up in `PostingFailed` with no Approver ever having been resolved.
- Both units satisfy their respective cited AD text to the letter. They are not interchangeable: Unit A tenants who configure Automatic-only Agents with no Approver Party will suddenly find their Agent unusable if migrated to Unit B's environment or vice versa, and integration tests written against one behavior will fail against the other.

**Fix:** Add an explicit mode qualifier to AD-13's FR-8 order enumeration, e.g.: *"...pre-Provider safety, Eligible Approver resolution (Confirmation Response Mode only — Automatic mode skips this step, since automatic posting has no approval gate; the `PostingFailed` recovery path in AD-5 tolerates a proposal that never had a resolved Approver and additionally allows the Tenant Agent Administrator to resolve it), membership..."* — or, if the intent is the opposite (universal resolution so `PostingFailed` recovery always has a fallback Approver), strike "(confirmation mode)" from the sequence diagram and add "regardless of response mode" to AD-8's "call time" clause. Either resolution is acceptable; the spine currently asserts both.

---

### C-2. AD-21's reservation-ordering clause reuses "authorization" for a term AD-12/AD-13 already pinned to a different, earlier step

**AD-13** establishes "authorization" as **step 1** of the FR-8 acceptance order — it must run before rate limits (step 4), context measurement (step 5), and reservation (step 6):

> "Acceptance follows the FR-8 order (**authorization**, lifecycle, provider eligibility, rate limits, context measurement, **reservation**, pre-Provider safety, Eligible Approver resolution, membership)..."

**AD-12** reinforces this as an inviolable, universal precondition on every side effect:

> "**Authorization gates run before every side effect** and fail closed on missing, stale, ambiguous, disabled, or unavailable dependency state."

**AD-21**, however, places reservation as occurring *before* something it calls "authorization":

> "After context measurement and **before safety, admission, and authorization**, one atomic `BudgetLedger` command reserves the estimated attempt cost..."

Reservation is unambiguously a side effect (it mutates `BudgetLedger`, a durable aggregate). Read against AD-12's "authorization gates run before every side effect," and against AD-13's own numbered FR-8 list where authorization is step 1 and reservation is step 6, this sentence is self-contradictory on its face — reservation cannot occur "before authorization" while also occurring after step-1 authorization by construction. The only way to save the sentence is to silently reinterpret "authorization" here as shorthand for the *different*, later concept `ProviderInvocationAuthorized` (AD-13's post-acceptance step (4): "`append ProviderInvocationAuthorized` at the expected revision") — but AD-21 never says that, and nothing in the shared vocabulary flags "authorization" as overloaded. AD-29 explicitly polices exactly this kind of collision elsewhere (distinguishing the AD-13 attempt fingerprint from the AD-29 command-payload fingerprint "by name"), but no such disambiguation exists here.

**Two-unit scenario:**
- **Unit A (BudgetLedger command-handler team):** reads AD-21 literally in isolation (it is the AD that binds `NFR-10`/cost and is the natural place to look for reservation-ordering rules) and implements the atomic reservation command to run *before* the AD-12 authorization gate — i.e., before confirming the caller has tenant/Party/role rights. This unit reserves budget for a request that may still be rejected as unauthorized, and — because `BudgetLedger`'s per-Party accounting and `MaxConcurrentNonterminalInteractionsPerParty` tracking begin at reservation — an unauthorized or cross-tenant caller can consume a tenant's concurrency/rate slot and budget capacity before ever being authenticated against tenant/Party state.
- **Unit B (a second, independently built BudgetLedger command-handler, e.g. after a rewrite):** reads AD-13's canonical FR-8 order as authoritative (it is the AD whose whole `Binds` list is "external side effects... idempotent... retry, replay, recovery" and it gives the full numbered sequence) and places authorization first, reservation at step 6, exactly as AD-13 states.
- Both units cite spine text verbatim for their ordering. They diverge on a security-relevant question: can an unauthorized caller trigger a durable budget-reservation side effect? Unit A says yes (bounded, reversible, but still a fail-open ordering versus AD-12's "authorization gates run before every side effect"); Unit B says no.

**Fix:** In AD-21, replace "before safety, admission, and authorization" with an unambiguous reference to the specific event, e.g.: *"After context measurement and before safety, admission, and `ProviderInvocationAuthorized`, one atomic `BudgetLedger` command reserves..."* — using the exact event name (as the rest of the spine does for `AgentCallAccepted`, `ProviderInvocationAuthorized`, etc.) rather than the bare word "authorization," which AD-12/AD-13 have already claimed for the step-1 gate.

---

## High

### H-1. `AgentReadinessStatus` has no fixed shape — is it an enum, or a composite object wrapping `ProviderReadinessResult`?

**AD-15** describes `AgentReadinessStatus` purely as an additive **enum-style status vocabulary**, in the same sentence pattern used for the unambiguous enum `AgentCallOperationStatus`:

> "`AgentCallOperationStatus` growth (`SafetyBlocked`, `BudgetBlocked`, ... `PostingFailed`), `AgentReadinessStatus` growth (`ActiveNotProvenCallable`, `Stale`, `AuthorityUnresolved`) ..."

But **AD-10** and **AD-17** both describe `AgentReadinessStatus` as a *container* that `ProviderReadinessResult` and `EntryMissing` live "inside":

> AD-10: "`EntryMissing` is reported only for the Agent's own currently-selected entry, or an in-flight interaction's previously-snapshotted entry, **inside `AgentReadinessStatus`/`ProviderReadinessResult`**"
> AD-17: "...a reason-coded `Blocked` `ProviderReadinessResult` **inside `AgentReadinessStatus`**, per AD-10..."

`AgentReadinessStatus` never appears in the classDiagram (unlike `ProviderReadinessResult`, which has a full field list: `OperationalState`, `Callability`, `ReasonCode`, `CapabilityVersion`, `ObservedAt`, `ValidUntil`, `Freshness`). No AD gives `AgentReadinessStatus` a field list, and the two families of description (bare enum vs. composite wrapper carrying a nested `ProviderReadinessResult`) are not compatible C#/contract shapes — an enum type cannot also be a struct/record that contains a `ProviderReadinessResult` field.

**Two-unit scenario:**
- **Unit A (Contracts team, "enum" reading of AD-15):** ships `AgentReadinessStatus` as a flat enum (`Active`, `ActiveNotProvenCallable`, `Stale`, `AuthorityUnresolved`, ...), and returns `ProviderReadinessResult` as a sibling field on the same DTO, never nested.
- **Unit B (Contracts team, "composite" reading of AD-10/AD-17):** ships `AgentReadinessStatus` as a class/record with an `EntryMissing` bool/flag and a nested `ProviderReadinessResult ProviderReadiness` property, plus a separate `Status` enum field for the growth values.
- A UI/BFF client built against Unit A's contract cannot deserialize Unit B's payload (missing nested object) and vice versa; both units are individually consistent with the AD text they read most closely.

**Fix:** Add one sentence to AD-15 (or AD-10) that fixes the shape explicitly, e.g.: *"`AgentReadinessStatus` is a record carrying a `Status` enum (`Active`, `ActiveNotProvenCallable`, `Stale`, `AuthorityUnresolved`, ...) plus an optional nested `ProviderReadinessResult` populated only under the AD-2/AD-10 `EntryMissing` carve-out; it is never itself the enum."* Then update AD-10/AD-17's "inside `AgentReadinessStatus`/`ProviderReadinessResult`" phrasing to name the specific field.

---

### H-2. `CurrencyMismatch` is asserted to live "in the AD-10 tenant join," but AD-10's own text never defines it — and the two ADs disagree on which FR-8 step catches it

**AD-21** asserts the currency-mismatch blocker is a `ProviderReadinessResult`-level concept owned by AD-10:

> "a platform entry whose pricing currency differs from the tenant budget currency is `Blocked` **in the AD-10 tenant join** with the tenant-scoped code `CurrencyMismatch` and refuses reservation"

But **AD-10**'s own enumeration of `Blocked` conditions — the one place a reader would expect to find every blocking reason for the "tenant join" — never mentions currency, pricing currency, ISO 4217, or `CurrencyMismatch` at all:

> "`Degraded` is callable only when every hard gate passes; missing, stale, unconfigured, unpriced, invalid-limit, failed, disabled, not-enabled, regressed, unknown, or indeterminate state is `Blocked`..."

and defers the actual reason-code vocabulary entirely to an external document:

> "...with the enum vocabulary and valid triples bound by `launch-readiness-register.md` section Provider Readiness Contract."

So AD-21 cross-references a mechanism inside AD-10 that AD-10's text does not contain (and that isn't obviously implied by "invalid-limit" or any other listed blocker — currency well-formedness and cross-currency mismatch are pricing-shape concerns AD-10 explicitly separates into "input/output pricing with currency" as a distinct capability-floor field, not folded into "unpriced" or "invalid-limit"). This is a stale/inconsistent cross-reference: either AD-10 was supposed to be amended when `CurrencyMismatch` was introduced in AD-21 and wasn't, or `CurrencyMismatch` was never actually meant to be a `ProviderReadinessResult` blocker and AD-21's "in the AD-10 tenant join" phrase is simply wrong.

**Two-unit scenario:**
- **Unit A (readiness/catalog team, building strictly from AD-10):** implements `ProviderReadinessResult`/`AgentReadinessStatus` exactly per AD-10's enumerated blockers and the (separately maintained) register enum — and never adds `CurrencyMismatch`, since AD-10's text doesn't name it. A tenant with a currency-mismatched entry sees the entry as fully `Callable` in the readiness UI/API (FR-8 step 3, "provider eligibility," passes clean).
- **Unit B (BudgetLedger/cost team, building strictly from AD-21):** implements `CurrencyMismatch` purely as a reservation-time (`BudgetLedger` command) rejection at FR-8 step 6, never touching `ProviderReadinessResult` at all, since that's the only place AD-21 actually specifies behavior ("refuses reservation").
- Neither unit produces a `ProviderReadinessResult.ReasonCode == CurrencyMismatch` anywhere, because AD-10 (the type owner) never defined it — yet AD-21 promises exactly that shape exists ("Blocked ... with the tenant-scoped code"). A third, more literal team could instead read AD-21's clause as an instruction to add the code to AD-10's enum and block **readiness itself** (visible pre-call, at step 3, before rate limits/context measurement/reservation ever run) — a materially different, earlier failure point than Unit B's reservation-time rejection.

**Fix:** Add `CurrencyMismatch` to AD-10's explicit blocker list (e.g., "...unpriced, **currency-mismatched**, invalid-limit...") and state which FR-8 step observes it first ("provider eligibility, step 3 — a tenant never reaches rate-limit or context-measurement work against a currency-mismatched entry"), so AD-21's reservation-time refusal is described as the *defense-in-depth backstop* it presumably is, not the primary detection point.

---

## Medium

### M-1. The Conversation membership "block" set/clear command has no declared lock-bearing-family membership

**AD-12** enumerates, closed-form, the commands that are lock-bearing and the commands that are explicitly not:

> "the lock-bearing subset is `ProposalResolution`, `PolicyPublication`, `TenantBudgetUpdate`, `LegalHold` (also `LegalHoldRelease`), `ExportRequest` (also `ExportDownload`), `DeletionRequest`, `ProviderCatalogMutation` (also `TenantProviderEnablement`), `AgentSetupMutation` (also `TenantKillSwitch`), and `AgentActivation`; `AgentCallAcceptance`, `ProposalEdit`, and `ProposalRegeneration` are not lock-bearing..."

**AD-22** reconfirms the closed count: "the reject-on-missing-or-whitespace-justification rule applies to all **nine** AD-12 lock-bearing families ... — with no exempt family," repeating the identical 9-item list.

But **AD-7** describes a governance-grade, audited command — setting/clearing the per-Conversation Agent block — that is neither in the 9-item lock-bearing list nor in the 3-item non-lock-bearing list:

> "The block is set by the Tenant Agent Administrator, the Conversation Facilitator [ASSUMPTION A-9], or the membership step on detecting an external removal, is cleared only by the first two, **is audited**, and is mirrored to the Conversations participant list through the removal seam."

This command is human-initiated (Administrator/Facilitator), audited, and high-impact (it can silently strand every non-terminal proposal in a Conversation — AD-7: "abandon that Conversation's non-terminal proposals"), which is exactly the profile of every other lock-bearing family. Yet it is absent from both AD-12 lists, and it has no declared command-family name at all (unlike `ProposalResolution`, `AgentSetupMutation`, etc.), so a builder cannot even map it onto the register's `OperationGateMatrix` family vocabulary AD-17 requires ("every public command ... declares exactly one family in its contract").

**Two-unit scenario:**
- **Unit A (Server/BFF team):** treats the block-set/clear command as lock-bearing by analogy (it's audited, human-triggered, governance-adjacent like `AgentActivation`), enforcing "at most one pending command per resource" and surfacing the AD-12 `accepted-by session reference` on it.
- **Unit B (a second Server/BFF team building the same feature independently):** treats it as non-lock-bearing, since AD-12's closed list simply doesn't name it and the three explicitly-named non-lock-bearing commands are the only carve-out given — relying purely on the AD-29 idempotency key and EventStore optimistic concurrency, with no advisory lock and no `accepted-by` UI affordance.
- Both units are individually consistent with AD-12's text (neither contradicts the *named* families); they diverge only because AD-12's rule has a scope gap for an unnamed command it should plausibly cover. The observable difference: whether two Facilitators/Administrators racing to toggle the block on the same Conversation see a "someone else has a pending action" UI state (Unit A) or a silent optimistic-concurrency conflict after both submit (Unit B).

**Fix:** Either add the block-set/clear command (name it, e.g., `MembershipBlockMutation`) to AD-12's lock-bearing list and to AD-22's "all nine" (making it ten), or explicitly add it to the non-lock-bearing carve-out alongside `AgentCallAcceptance`/`ProposalEdit`/`ProposalRegeneration` with a stated reason.

---

## Low

### L-1. `AgentCall` and `AgentResponse` are declared domain terms with zero binding rule content

The Naming convention row is the **only** place either bare term appears in the entire 759-line spine:

> "Domain terms are `Agent`, ... `ProposedAgentReply`, `ProposalVersion`, `ApproverPolicy`, `AgentCall`, `AgentResponse`, `BudgetLedger`, ..."

Every other domain term in that list is given a Rule elsewhere (e.g. `ProposedAgentReply` in AD-5, `ApproverPolicy` in AD-8). `AgentCall` and `AgentResponse` are not — every other occurrence in the document is a compound name (`AgentCallAccepted`, `AgentCallAcceptance`, `AgentCallOperationStatus`, a prose phrase "a new Agent Call," "Call hexa") or absent entirely. Nothing pins down what `AgentResponse` models (a DTO for the API call-return payload? a synonym for `ProposalVersion`/posted content in Automatic mode? a read-model row?).

**Two-unit scenario:** a Client-package team and a UI team, both told to "use the domain terms in the Naming table," independently invent incompatible types named `AgentResponse` — one as the typed return value of the call API (wrapping the `*CommandAcceptance` pending identity), another as a read-model DTO describing a posted/approved reply's final content — because no AD rule constrains either choice.

**Fix:** Either give `AgentCall`/`AgentResponse` a one-line binding definition (e.g., in AD-13 or AD-15, "the public call command DTO is named `AgentCall`; its accepted-write response, the `AgentResponse` `*CommandAcceptance` contract, carries only the pending identity per AD-15 — never proposal content"), or remove them from the Naming table if they are meant only as informal prose shorthand.

### L-2. `GenerationFailureRecord` has no AD-29 deterministic-identity derivation

AD-5 introduces `GenerationFailureRecord` as a first-class, AD-14-protected artifact distinct from `ProposalVersion`:

> "Failed or incomplete generated content, when retained, is a separate `GenerationFailureRecord` under AD-14 protection and is never a `ProposalVersion`."

AD-27 confirms it is dispatched via its own command, sealed on receipt, alongside the success path:

> "Generation, output safety, and the `RecordGeneratedVersion` or `GenerationFailed` dispatch are one activity..."

AD-29's exhaustive identity list — which explicitly derives `AttemptId`, `ProposalVersionId`, `SafetyDecisionId`, `MessageId`, `ObservationId`, `SampleId`, etc. — never derives a `GenerationFailureRecord` identity or states that it simply reuses `AttemptId` verbatim (the way `ProviderIdempotencyKey = AttemptId` is stated explicitly elsewhere in the same AD). Two builders could reasonably key the record by the owning `AttemptId` (natural, since one attempt yields at most one failure record) or mint a fresh AD-29-style hash the way `ProposalVersionId` is minted for successes — an inconsistency in whether the failure path has its own deterministic identity namespace at all.

**Fix:** Add one clause to AD-29: *"`GenerationFailureRecord` carries no identity of its own; it is addressed by its owning `AttemptId` and event position, never a separately derived id."* (or the converse, with an explicit `H(...)` formula, if a separate id is actually needed for the `ProtectedContentReference`).

---

## Summary Table

| # | Severity | Title | ADs in tension |
| --- | --- | --- | --- |
| C-1 | Critical | Eligible Approver resolution: universal vs. Confirmation-mode-only | AD-8, AD-13, AD-5 vs. sequence diagram |
| C-2 | Critical | "Before ... authorization" reuses a term AD-12/AD-13 already fixed to step 1 | AD-21 vs. AD-12, AD-13 |
| H-1 | High | `AgentReadinessStatus`: enum vs. composite-wrapper shape | AD-15 vs. AD-10, AD-17 |
| H-2 | High | `CurrencyMismatch` claimed to live in AD-10 but absent from AD-10's text | AD-21 vs. AD-10 |
| M-1 | Medium | Conversation-block set/clear command has no lock-bearing-family assignment | AD-7 vs. AD-12, AD-22 |
| L-1 | Low | `AgentCall`/`AgentResponse` are named but never bound by any Rule | Naming table vs. (absence) |
| L-2 | Low | `GenerationFailureRecord` has no AD-29 identity derivation | AD-5, AD-27 vs. AD-29 |

No finding in this pass rests on intent-level disagreement or stylistic preference; each cites exact, quoted, currently-live text from two or more locations in the spine that a literal, good-faith implementer would follow into materially different, non-interoperable builds.
