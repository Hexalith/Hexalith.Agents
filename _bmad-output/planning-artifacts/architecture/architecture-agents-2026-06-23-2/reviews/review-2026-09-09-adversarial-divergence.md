# Adversarial Divergence Review — 2026-09-09

## Verdict

**FAIL — 1 critical and 8 high findings require new or tightened ADs before Epics 6–8 are built on this spine. Epic 5 stories can proceed once B-8 (Platform principal targeting) and B-12 (lock-bearing vocabulary) are closed; B-1 must be closed before Story 6.3 or 8.4 enters ready-for-dev.**

## Lens

Construct two units one level down (Epic 5–8 stories, the register, the `EXT-*` seams) that each obey every AD to the letter yet build incompatibly: clashing shared-data shapes, two owners of one entity, or conflicting state-mutation paths. Each pair is a hole to close with a new or tightened AD.

Reviewed authority: `ARCHITECTURE-SPINE.md` (re-distilled 2026-09-09, AD-1..AD-31), `IMPLEMENTATION-CONVENTIONS.md`, `launch-readiness-register.md` and `external-dependency-register.md` (both amended 2026-09-09), the PRD (FR-1..FR-33, §8.1, §13), `epics.md` Epics 5–8, and the prior review `review-2026-09-08-adversarial-divergence.md` (A-1..A-20). Two facts were verified against the checkout rather than assumed: Hexalith.Tenants reserves `DefaultTenantId = "system"` with a `ForGlobalAdministrators()` identity factory, and the shipped `AgentInteractionAggregate` states that request time is "the EventStore event-metadata timestamp, server-stamped at persist".

## Relationship To The 2026-09-08 Review

Closures that hold one seam further in and are not repeated: A-1 (AD-27 references only), A-2 (resolved by a platform-scoped `system` catalog plus `TenantProviderEnablement` rather than the tenant-scoped catalog A-2 proposed; coherent), A-5 (`ObservationId` derivation and legal re-observation), A-6 (`TenantScope` grammar, `ScopeKind`, `AuthorizedProducer`), A-7 (matrix v2 families), A-8 (`AttemptId`/`AttemptOrdinal`/`ReservationId`/`AdmissionId` derivations for Provider attempts), A-10 (governance aggregates named), A-11 (`BudgetPeriod` key), A-19, A-20.

Closures shown incomplete or wrong by this review: **A-9 is closed with a rule that admits the weaker policy it was meant to exclude (B-1)**; A-3's `ExpiresAt` base cannot be produced as written (B-2); A-12's release-on-terminalization never made it into the spine (B-9); A-14's replay semantics and non-attempt identities are still unbound (B-14); A-16 was resolved in the opposite direction, which reopens the AD-13/AD-20 contradiction (B-10); A-17's register clause was not added (B-15); A-18's single family vocabulary is contradicted by AD-12's own list (B-12). Prior M-1/M-2 style items (A-13 outcome envelope, A-15 contract evolution) were adopted only as Consistency rows and are not re-scored.

---

## Critical

### B-1 — `RestrictivenessRank` admits a `Loosens` version as "at least as restrictive", so the AD-20 comparison mandates the weaker retry it prevents

- **Severity:** Critical (silent, safety-weakening, and every literal implementation converges on it)
- **Units:** Story 8.4 policy publication (platform `ContentSafetyPolicy` and tenant restrictions) vs Story 6.3 / 7.3 / 7.4 re-evaluation (transport retry, regeneration, approval-time, pre-post).
- **ADs obeyed by both:** AD-20 ("aggregate-assigned non-decreasing `RestrictivenessRank` ... `Tightens` increments, `Loosens` and `Neutral` keep ... a retry evaluates under the current version only when its rank is at least the snapshot rank"), AD-2 (safety version is the pair), AD-4 (pair frozen in the snapshot), Consistency `Safety`.
- **Incompatible choices:**
  - Unit 1 (8.4) publishes v5 with `ChangeKind = Loosens` after v4 (rank 3). Per AD-20 the aggregate keeps rank 3.
  - Unit 2a (6.3, literal) compares `current.rank (3) >= snapshot.rank (3)` → true → evaluates the retry, regeneration, approval-time, and pre-post checks under v5, the looser policy.
  - Unit 2b (6.3, reading FR-26's "the more restrictive of the initial attempt's policy and the then-current active policy") compares category sets and evaluates under v4.
  - Neither unit has any rule for the tenant half of the pair: `TenantGovernancePolicy` safety restrictions carry no rank, so a tenant that removes a restriction it added (nothing in FR-26 or AD-20 forbids removal) produces a current tenant version that is always "current" and always used. The verdict cache is "invalidated on policy publication" — of which half is unspecified.
- **Failure:** Content v4 would have blocked passes under v5 and posts, exactly the "retry evaluating under weaker policy" AD-20 prevents, with the audit trail showing a compliant rank comparison. Unit 2a and 2b reach opposite decisions for the same proposal, so `LR-SAFETY` evidence is implementer-dependent. The rank as defined counts tightenings; it is not an order on restrictiveness.
- **Closing AD text (rewrite the AD-20 rank clause):** "Every published `ContentSafetyPolicy` version records `LastLoosensVersion`, the greatest version number (itself included) whose `ChangeKind` is `Loosens`; version B is at least as restrictive as version A iff `B.Version >= A.Version` and `B.LastLoosensVersion <= A.Version`. The aggregate rejects a `Tightens` or `Neutral` declaration whose always-blocked and restricted category sets are not supersets of the previous version's. Tenant safety restrictions in `TenantGovernancePolicy` are versioned with the same `ChangeKind`/`LastLoosensVersion` fields, and the rule is applied component-wise to the snapshot pair. Every re-evaluation point (regeneration, approval-time, pre-post) evaluates under the current pair when both components are at least as restrictive, otherwise under the snapshot pair exactly, and records the evaluated pair on the decision event. Publication of the platform version invalidates every tenant's verdict cache; publication of a tenant version invalidates that tenant's."
- **Disposition:** Autofix before Story 6.3 or 8.4 is ready-for-dev; add a `NoWeakerRetryTests` case for a `Loosens` publication between snapshot and retry.

---

## High

### B-2 — `ExpiresAt` cannot be stored "once in the creating event's payload" from that event's commit timestamp, so three deadline bases survive AD-28 (prior A-3 incomplete)

- **Severity:** High
- **Units:** Story 7.1 proposal creation (writes `ExpiresAt`) vs Story 7.6 expiry timer and Story 8.1 retention.
- **ADs obeyed by both:** AD-28 (`DomainInstant` = commit timestamp; "a derived deadline is stored once in that event's payload"; aggregates never read a clock), AD-5, AD-3.
- **Incompatible choices:** The commit timestamp is server-stamped at persist (verified in `AgentInteractionAggregate.cs`), so the payload of the creating event cannot contain `commit + Lifetime`. Unit 1 stores `ExpiresAt = EvaluatedAt + Lifetime` from the orchestrator `TimeProvider`. Unit 2 stores only `Lifetime` and lets the projection compute `commit + Lifetime`. Unit 3 appends a second `ExpiryScheduled` event after reading the first event's metadata. Each is "the" DomainInstant under a defensible reading.
- **Failure:** Three deadlines differing by command/commit latency; Unit 2's aggregate cannot enforce "treated as `Expired` on every command" (AD-5) because it never holds the deadline; Unit 3 doubles the event count and opens a crash window with a proposal that has no deadline at all. Retention base has the same ambiguity for terminal events.
- **Closing AD text (tighten AD-28):** "A deadline base is the `EvaluatedAt` the orchestrator stamps into the creating command (an EvaluationInstant), and the aggregate stores `ExpiresAt = EvaluatedAt + Lifetime` in the creating event's payload; the aggregate rejects an `EvaluatedAt` earlier than the greatest `EvaluatedAt` already recorded on its stream. The event-metadata commit timestamp is the DomainInstant for measurement only (NFR-9/SM metrics, the retention base, the terminal instant) and is never a deadline base, because it does not exist when the payload is built. The AD-28 timer re-arm rule absorbs the residual skew."
- **Disposition:** Autofix.

### B-3 — The `Workflow` principal allowlist forbids the commands AD-7, AD-21, AD-22, and AD-28 require workflow activities to dispatch

- **Severity:** High
- **Units:** Story 6.6 membership activity (must "set the Agents-owned per-Conversation block" on external removal), Story 8.1 retention-due activity (must "emit exactly one eligible-expiry request for Story 8.3"), Story 6.4/8.4 period close, Story 7.6 `TimerDrift` — vs Story 6.1's principal enforcement in every aggregate.
- **ADs obeyed by both:** AD-30 ("`Workflow` may dispatch only `AgentInteraction` lifecycle-result commands, `BudgetLedger` reserve and settle, and readiness evidence for its own interaction, never `Agent`, catalog, enablement, policy, hold, export, or deletion commands"; aggregates verify allowlist and scope), AD-7 (membership step sets the block; but "the block is set and cleared only by the Tenant Agent Administrator or the Conversation Facilitator"), AD-2 (blocks live in `TenantGovernancePolicy`, a policy aggregate), AD-21 (`Unreconciled` settles at period close), AD-22 (retention expiry triggers deletion).
- **Incompatible choices:** Unit 1 obeys AD-30: the membership activity rejects the call with `RemovedInConversations` but cannot set the block (a policy command), the retention activity cannot request deletion, period close needs an operator. Unit 2 obeys AD-7/AD-22: it synthesizes an `Administrator` extension inside the activity — which AD-30 defines as a forged reserved extension and an audited security event.
- **Failure:** Under Unit 1 the next call in that Conversation finds no block, sees "no block set", and re-adds `hexa` — the re-admission FR-2 and AD-7 exist to prevent. Retention never triggers deletion; months never close; `TimerDrift` has no writer. Under Unit 2 the security event fires on every external removal. AD-7 also contradicts itself: the step "sets" the block, yet it is "set ... only by" two human authorities.
- **Closing AD text (tighten AD-30, amend AD-7):** "The `Workflow` principal is additionally allowlisted, each bound to its own `AgentInteractionId` or the interaction's `ConversationId`, for: `ConversationAgentState.RecordExternalRemoval` (sets the block with reason `ExternalRemovalDetected`, see B-4); `AgentInteraction` system abandonment and `RecordTimerDrift`; `BudgetLedger` release and `ClosePeriod` under the `SystemTimer` family; `ProtectedDeletion.RequestRetentionExpiry(AgentInteractionId)` under `SystemTimer`, which opens a retention-originated deletion request that still runs the full AD-22 hold interlock. It never dispatches readiness observations (see B-13). AD-7 reads: the block is set by the Tenant Agent Administrator, the Conversation Facilitator, or the membership step on detected external removal, and is cleared only by the two human authorities."
- **Disposition:** Autofix.

### B-4 — "Agents previously established membership there" and the per-Conversation block have no bounded owner

- **Severity:** High
- **Units:** Story 6.6 membership step (needs a durable "membership established" fact per Conversation and the block) vs Story 5.2 `Agent` aggregate and Story 8.4 `TenantGovernancePolicy`.
- **ADs obeyed by both:** AD-2 (aggregates are "exactly" the eleven listed; blocks in `TenantGovernancePolicy`; "Prevents ... an unbounded tenant-wide hot aggregate"), AD-7 (three-part step: read state; if Agents previously established membership and `hexa` is absent → block), AD-12 (fail closed on stale projections).
- **Incompatible choices:** Unit 1 records `MembershipEstablished(ConversationId)` on the `Agent` aggregate (one `hexa` per tenant) — one event per Conversation ever called, on one stream. Unit 2 derives the fact from a projection over `AgentInteraction` events (any prior interaction in that Conversation with a membership event). Unit 3 records it in `TenantGovernancePolicy` next to the blocks, with the same growth. "Abandon that Conversation's non-terminal proposals" likewise needs an index by Conversation that no aggregate owns.
- **Failure:** Unit 1/3 build the unbounded tenant-wide hot aggregate AD-2 forbids (every first call in every Conversation appends to it; every side-effecting step re-reads it). Unit 2's projection lag makes the step conclude "never established" after a removal, so it idempotently re-adds a removed Agent — silently, because both branches are "compliant".
- **Closing AD text (amend AD-2, tighten AD-7):** "A twelfth aggregate `ConversationAgentState` (`TenantId`, `ConversationId`) owns membership-established state, the per-Conversation block with reason and audit, and the index of non-terminal `AgentInteractionId`s for that Conversation. `TenantGovernancePolicy` holds no per-Conversation state. The three-part membership step and every pre-post re-validation read `ConversationAgentState` at an expected revision; removal-driven abandonment enumerates its index, never a projection. Per AD-13 identity: `ConversationAgentStateId = H(conversation-state, TenantId, AgentId, ConversationId)`."
- **Disposition:** Discuss the aggregate name (one exchange), then autofix.

### B-5 — Per-`HoldId` `LegalHold` streams make "read hold state from the owning aggregate" unimplementable for "any active hold" (prior A-10 incomplete)

- **Severity:** High (unrecoverable: erasure under hold)
- **Units:** Story 8.3 deletion and Story 8.1 retention expiry (must answer "does any active hold cover this interaction?") vs Story 8.1 hold application.
- **ADs obeyed by both:** AD-2 (`LegalHold` keyed `TenantId`, `HoldId`; "any decision whose wrong answer is unrecoverable ... reads hold and policy state from the owning aggregate at an expected revision, never from a projection"), AD-22 (DEK destruction under an active hold is rejected).
- **Incompatible choices:** Unit 1 reads the `legal-hold` projection because the set of holds in a tenant is otherwise unknowable without enumerating streams. Unit 2 invents a per-tenant `LegalHoldRegistry` stream to read at an expected revision — a twelfth aggregate AD-2's "exactly" forbids. Neither can bind "the expected revision" of N independent hold streams into one deletion decision, and the `class:` + UTC range scope is keyed on an undefined instant (request time in one unit, terminal time in the other).
- **Failure:** A hold accepted after the projection checkpoint is invisible to Unit 1; the DEK is destroyed. Unit 2 violates AD-2 and still races a hold appended between its read and the destroy. The range ambiguity makes the same hold cover different interactions in the writer and the evaluator.
- **Closing AD text (tighten AD-2 and AD-22):** "Applying a hold resolves its scope at application time and dispatches `LegalHoldPinned(HoldId)` to every in-scope `AgentInteraction`; release dispatches `LegalHoldReleased(HoldId)`. The `LegalHold` aggregate records the pinned interaction list and becomes `Active` only when every pin is acknowledged. A `class:` scope with a UTC range selects interactions whose `InteractionRequested` DomainInstant lies in the range and whose recorded content classes include the class. DEK destruction, redaction, and retention expiry read only the target interaction's own pin set at its expected revision; any active pin rejects. The erasure activity re-reads at that revision immediately before destroying the DEK and aborts with `HeldAfterAuthorization` if a pin arrived. The `legal-hold` projection serves inspection only."
- **Disposition:** Autofix.

### B-6 — `SafetyDecisionId = H(safety, AttemptId, Stage)` collides for edited versions and for repeated same-stage decisions, and AD-20 omits the approval-time stage

- **Severity:** High
- **Units:** Story 7.4 approval/pre-post scans and Story 7.2 edited versions vs Story 6.1's aggregate idempotency guards (IMPLEMENTATION-CONVENTIONS step 3).
- **ADs obeyed by both:** AD-29 (`SafetyDecisionId = H(safety, AttemptId, Stage)`), AD-20 ("fresh ... decision" before proposal creation and "the approved version again before posting"; every decision is an event carrying `SafetyDecisionId`), AD-5 (`PostingFailed` retries), OQ-9 (four application points, including approval-time).
- **Incompatible choices:** An edited version has no Provider attempt; Unit 1 uses the source generated version's `AttemptId`, Unit 2 substitutes `ProposalVersionId`. A second pre-post decision on a posting retry, or approval-time followed by pre-post, has the same (`AttemptId`, `Stage`); Unit 1 treats the second as an exact-duplicate no-op (so no fresh decision is recorded and the stale verdict authorizes the post), Unit 2 treats it as a conflicting duplicate and blocks posting forever. Unit 1's `Stage` enum has three values (AD-20's list), Unit 2's has four (OQ-9).
- **Failure:** Either AD-20's "fresh decision before posting" is silently not recorded, or every posting retry is a permanent conflict. Different `Stage` vocabularies cannot be reconciled in `audit-evidence`.
- **Closing AD text (tighten AD-29 and AD-20):** "`SafetyDecisionId = H(safety, AgentInteractionId, Stage, SubjectId, DecisionOrdinal)` with `Stage ∈ {PreProvider, Output, ApprovalTime, PrePost}`, `SubjectId` = `AttemptId` for `PreProvider`/`Output` and `ProposalVersionId` for `ApprovalTime`/`PrePost`, and `DecisionOrdinal` the aggregate-assigned 1-based count of decisions for that (`Stage`, `SubjectId`). AD-20 names all four stages; `ApprovalTime` and `PrePost` coincide only when approval and posting are one command result."
- **Disposition:** Autofix.

### B-7 — Reservation precedes the descriptor in FR-8/AD-21/the sequence diagram but `ReservationId` is derived from an `AttemptId` that does not yet exist; AD-13's protocol lists reservation a second time with a different amount

- **Severity:** High
- **Units:** Story 6.4 (descriptor and reservation) vs Story 6.1 (acceptance pipeline in FR-8 order) and Story 7.3 (regeneration ordinal and ceiling).
- **ADs obeyed by both:** AD-13 ("Acceptance follows the FR-8 order (... context measurement, reservation, pre-Provider safety, Eligible Approver resolution, membership), and Provider invocation follows the ordered protocol: (1) append one prepared descriptor ... (2) reserve maximum cost under `ReservationId`"), AD-21 (reserve "the estimated attempt cost ... after context measurement and before safety, admission, and authorization"), AD-29 (`ReservationId = H(reservation, AttemptId)`; `AttemptOrdinal` = count of prepared descriptors), AD-2 ("one reservation event per attempt"), the sequence diagram (reserve → safety → approver → membership → `PrepareAttempt`).
- **Incompatible choices:** Unit 1 follows the diagram: reserves at FR-8 step 6 — but needs an `AttemptId`, so it appends the descriptor before safety, binding "evaluated safety policy versions" that have not been evaluated. Unit 2 follows the numbered protocol: descriptor then reservation after membership, so a membership failure has nothing to release and FR-8's "release `NotInvoked` on a later failure" is dead. Unit 3 reads both sentences literally and reserves twice (estimated at acceptance, maximum at protocol step 2), breaking AD-2's one-reservation bound. Units also disagree whether a descriptor that never reaches authorization consumes `AttemptOrdinal` and counts against the regeneration ceiling.
- **Failure:** The same regeneration gets different `AttemptId`s across implementations (ordinal 2 vs 3 after one blocked regeneration), so `EXT-PROVIDER-1` outcome lookup, recovery inventories, and `LR-RECOVERY` manifests disagree; double reservations exhaust the monthly cap at half the real spend.
- **Closing AD text (tighten AD-13, AD-21, AD-29):** "`PrepareAttempt` is FR-8 step 5b: immediately after context measurement and before reservation the interaction appends the descriptor, consuming `AttemptOrdinal`, binding the safety policy pair current at that instant; the pre-Provider decision must evaluate under that pair (or its at-least-as-restrictive successor per B-1) or the attempt fails closed. One reservation per descriptor, of the estimated attempt cost, follows immediately. A descriptor failing any of FR-8 steps 7–9 or admission records `AttemptCancelled(NotInvoked)` with the reservation released; its ordinal stays consumed. The invocation protocol is renumbered: (1) descriptor, (2) reservation, (3) safety, approver, membership, (4) admission, (5) `ProviderInvocationAuthorized`, (6) `BeginInvocation`, (7) invoke, (8) outcome, settle, release. The regeneration ceiling counts descriptors with ordinal ≥ 2."
- **Disposition:** Autofix; redraw the sequence diagram (see also B-19).

### B-8 — The `Platform` principal cannot target a tenant-scoped aggregate under AD-2's isolation rule, so tenant enablement, the kill switch, and cap overrides are unbuildable or unaudited

- **Severity:** High
- **Units:** Story 5.3 / 8.4 `TenantProviderEnablement` and `TenantKillSwitch` commands (Platform Operator acting on tenant A) vs Story 5.4's tenant-isolation enforcement and Story 8.5's audit envelope.
- **ADs obeyed by both:** AD-30 (`Platform` = "the `system`-tenant Platform Operator"; envelope carries exactly one principal), AD-2 (`TenantId` mandatory on every identity; "a query for a key outside the caller's tenant ... returns exactly the absent-key response"), Consistency `Audit envelope` (every event carries `TenantId`), AD-17 (matrix family `TenantProviderEnablement` requires `LR-TENANT-ACCESS`, a Tenant-kind gate).
- **Incompatible choices:** Unit 1 enforces `envelope.TenantId == aggregate.TenantId`: the Platform principal (tenant `system`) targeting `TenantProviderEnablement(A)` is "outside the caller's tenant" → absent-key → no tenant can ever be enabled; `LR-TENANT-ACCESS` is evaluated for `system`, which has no record → blocked. Unit 2 lets `Platform` target any tenant, evaluates the gate at tenant A, and stamps the event `TenantId` with the target — but nothing says whether `TenantId` in the audit envelope is the actor's or the target's, so Unit 2's projections for tenant A and Unit 1's for `system` index the same event differently.
- **Failure:** Either the platform-only FR-33 rows are impossible, or cross-tenant administrative events land in the wrong tenant's `audit-evidence`/`agent-setup` projections and the kill-switch state read by AD-4/AD-12 never appears for the stopped tenant.
- **Closing AD text (tighten AD-30 and AD-2):** "Every envelope's `TenantId` is the target aggregate's tenant. The `Platform` principal carries `ActorTenantId = system`, is resolved by the Tenants `ForGlobalAdministrators()` role check, and may target another tenant only for the cross-tenant families `TenantProviderEnablement`, `TenantKillSwitch`, `TenantBudgetUpdate` (set and override), and `DeletionRequest`; every other principal's `TenantId` must equal its membership tenant. Tenant-kind gates are evaluated at the target tenant. Audit events record `TenantId` and `ActorTenantId`; projections key on `TenantId`. The AD-2 absent-key rule applies to the target tenant for non-`Platform` principals."
- **Disposition:** Autofix.

### B-9 — A Provider timeout is a retryable transient failure under AD-13 and a never-retried `Indeterminate` outcome under AD-21/FR-12, and recovery charges never-invoked reservations at maximum (prior A-12 incomplete)

- **Severity:** High
- **Units:** Story 6.4 outcome classification and Story 6.1/AD-23 recovery vs Story 7.5/7.6 terminalization while an attempt is queued.
- **ADs obeyed by both:** AD-13 ("Only transient transport/timeout failures retry before a terminal outcome, reusing the exact descriptor, reservation, admission, and key"), AD-21 ("an `Indeterminate` outcome holds the reservation ... and is never retried"; "orphan reservations found by recovery settle the same way [`ChargedAtMaximum`]"), FR-12 (`Indeterminate` is never silently retried), AD-23 ("open reservations follow AD-21").
- **Incompatible choices:** Unit 1 classifies a timeout as transient and re-invokes under the same `ProviderIdempotencyKey`. Unit 2 classifies it as `Indeterminate`, holds 24 hours, settles at maximum. For a crash between reservation and `ProviderInvocationAuthorized` (no call was possible), Unit 1's recovery releases `NotInvoked` (AD-13: no call before step 4), Unit 2's settles `ChargedAtMaximum` (AD-21: orphan). When a proposal expires or is rejected while a regeneration attempt is queued, no AD assigns anyone the release, so the reservation ages into `Unreconciled` and is charged at maximum.
- **Failure:** Duplicate Provider work where the adapter is not idempotent on the key, or reservations for calls that provably never happened consuming the monthly cap after a restart storm — a false 100% fail-closed for the tenant. `LR-COST` and `LR-RECOVERY` evidence differ by implementer.
- **Closing AD text (tighten AD-13, AD-21, AD-23):** "After transport begins, a transport failure or timeout is retryable under the same attempt only when `EXT-PROVIDER-1` outcome lookup by `ProviderIdempotencyKey` authoritatively returns `NotStarted` or `CompletedNoUsage`; `Unknown`, unavailable, or `InProgress` past the timeout yields `Indeterminate`. A command that terminalizes an interaction with attempts lacking an outcome returns `AttemptCancelled(AttemptId, Reason)` in the same result; the workflow then cancels admission and releases the reservation `NotInvoked`. Recovery classifies open reservations by step: no `ProviderInvocationAuthorized` or no `InvocationActive` → release `NotInvoked`; authorized and lookup proves not started → release `NotInvoked`; otherwise the `Indeterminate` path; only reservations past the hold deadline become `Unreconciled`."
- **Disposition:** Autofix.

---

## Medium

### B-10 — The fingerprint includes "evaluated safety policy versions" while AD-20 lets a retry evaluate under a tighter current version, so the permitted retry fails closed (prior A-16 resolved in the opposite direction)

- **Units:** Story 6.4 fingerprint/attempt guard vs Story 6.3 no-weaker retry.
- **ADs obeyed:** AD-13 (fingerprint covers "the evaluated safety policy versions"; any fingerprint change fails closed under that attempt; transport retries recheck readiness only), AD-20 (retry may evaluate under the current, tighter version).
- **Incompatibility:** Unit 1 re-evaluates safety on retry under the tighter version → evaluated versions change → Unit 2's aggregate guard rejects the retry as a changed fingerprint. If instead retries do not re-evaluate (AD-13's letter), AD-20's retry clause is dead text and a tightening publication never reaches an in-flight retry.
- **Closing AD text:** "A transport retry never re-evaluates safety; it runs under the descriptor's bound pair, so the fingerprint never changes for a policy reason. Re-evaluation points are regeneration (a new descriptor), approval-time, and pre-post, where B-1's rule applies."
- **Disposition:** Autofix.

### B-11 — Kill-switch semantics are defined for four states and undefined for `PostingFailed`, in-flight generation, and automatic posting, and its read source is unnamed

- **Units:** Story 7.4/7.6 posting-retry workflow vs Story 8.4 `TenantKillSwitch` command and Story 5.7 `agent-setup` stop state.
- **ADs obeyed:** AD-12 (switch "lets `Approved` and `PostingPending` complete or fail on their own terms, cancels queued admissions"), AD-4 ("every side-effecting step re-reads ... the tenant kill switch ... and fails closed"), AD-5 (system abandonment reason "kill switch"; bounded posting retry).
- **Incompatibility:** Unit 1 continues `PostingFailed` retries and admin retries under the switch ("on their own terms"); Unit 2 blocks them (every side effect re-reads and fails closed) and leaves the proposal `PostingFailed` with the 15-minute clock running out. An automatic interaction whose generation completes under the switch records `PostingFailed(TenantStopped)` in one unit and terminal `Abandoned(KillSwitch)` in the other; posting-failure-rate (a kill-switch trigger) differs by implementer. Unit 1 reads the switch from `agent-setup` (stale seconds), Unit 2 from `TenantGovernancePolicy` at expected revision.
- **Closing AD text (tighten AD-12):** "Under a set kill switch: awaiting-decision proposals may be rejected or abandoned by humans and are not system-abandoned; `Approved`/`PostingPending` proceed; `PostingFailed` retries and administrative retries are suspended and the retry window clock is paused; an `InvocationActive` attempt completes and records its outcome; an automatic interaction whose output is allowed records `PostingFailed(TenantStopped)`, retryable once the switch clears; queued admissions are cancelled with `RecordCapacityBlocked(TenantStopped)` and `NotInvoked` release. The switch is read from `TenantGovernancePolicy` at an expected revision before posting and Provider invocation, and from `agent-setup` for every other surface."
- **Disposition:** Autofix.

### B-12 — AD-12's lock-bearing list is a second family vocabulary that omits five v2 families, and AD-25's viewport blocking inherits the gap (prior A-18 incomplete)

- **Units:** Story 8.1 UI (`LegalHoldRelease`) and Story 7.2/7.3 UI (`ProposalEdit`, `ProposalRegeneration`) vs Story 8.6 conformance suite.
- **ADs obeyed:** AD-12 (lock-bearing subset named: nine families; "the family vocabulary is owned by the register matrix version"), AD-25 ("every lock-bearing family remains blocked" at the restrictive viewport), register matrix v2 (`LegalHoldRelease`, `TenantKillSwitch`, `TenantProviderEnablement`, `ProposalEdit`, `ProposalRegeneration` added; no lock-bearing column).
- **Incompatibility:** Unit 1 treats `LegalHoldRelease` as not lock-bearing (absent from AD-12's list): no advisory lock, no restrictive-viewport block — a hold is releasable from a phone with no context, and release leads to erasure. Unit 2 folds it into `LegalHold`. The resource identity for `PolicyPublication` (platform policy vs tenant restriction) and `TenantKillSwitch` is undefined, so "one pending command per resource" scopes differently.
- **Closing AD text:** "The register matrix gains `LockBearing` and `LockResource` columns; AD-12 and AD-25 reference them and name no family. `LockBearing = true` for the nine AD-12 families plus `LegalHoldRelease`, `TenantKillSwitch`, `TenantProviderEnablement`, `ProposalEdit`, `ProposalRegeneration`; `LockResource` is the target aggregate identity."
- **Disposition:** Autofix (register + spine).

### B-13 — `AuthorizedProducer` names roles that no AD-30 principal carries, and AD-30 lets the workflow submit readiness evidence a Platform-kind gate rejects

- **Units:** Story 5.6/6.5 producers (`Platform Maintainer`) and Story 6.1 ("LR-RECOVERY partial observation for workflow-owned identities") vs Story 5.5 `LaunchReadinessGate` producer check.
- **ADs obeyed:** AD-17 ("Submissions from a principal lacking the gate's `AuthorizedProducer` role are rejected before append"), register (`Platform Maintainer`, `Release Operator`), AD-30 (principals `User`/`Administrator`/`Platform`/`Workflow`; `Workflow` may dispatch "readiness evidence for its own interaction").
- **Incompatibility:** `Platform Maintainer` is neither an FR-33 role nor an AD-30 principal; Unit 1 maps it to `Platform`, Unit 2 to a host service identity that AD-30 does not admit. `LR-RECOVERY`'s producer is `Platform Maintainer`, so the `Workflow` submission Story 6.1 plans is rejected by the gate, or accepted only by an aggregate that ignores the producer column.
- **Closing AD text:** "`AuthorizedProducer` values are AD-30 principal kinds: Platform-kind gates accept only `Platform`; Tenant-kind gates accept `User` holding Release Operator or `Platform`. No `Workflow` producer exists; the AD-30 clause 'readiness evidence for its own interaction' is struck, and recovery cohort evidence is submitted by the `EXT-TOPOLOGY-1` fixture under `Platform`."
- **Disposition:** Autofix.

### B-14 — "Exact replay" is undefined, the idempotency record has no owner for ids the client cannot derive, and `HoldId`/`ExportId`/`DeletionRequestId` have no derivation (prior A-14 incomplete)

- **Units:** Story 7.2 edit and Story 8.2 export (client key K sent twice, second with corrected payload) vs Story 6.1 replay guards.
- **ADs obeyed:** AD-29 (tuple `(TenantId, principal, family, client key)`; "an exact replay returns the original authoritative identity"; derivations listed for attempts, versions, messages, observations, samples only), IMPLEMENTATION-CONVENTIONS (aggregate idempotency guards).
- **Incompatibility:** Unit 1 returns the original for any same-tuple replay (payload ignored) — a corrected edit is silently dropped; Unit 2 compares a payload fingerprint and returns `Conflict`. Unit 1 stores the key on the aggregate; Unit 2 in a Server-side projection that is stale under replay. `ExportId = H(export, TenantId, PartyId, key)` in one unit and `H(export, scope, range, classes)` in another: two operators exporting the same scope collide, or one operator's re-key duplicates. "Principal" in the tuple includes resolved roles in one unit, so a role change between retries mints a new command.
- **Closing AD text (tighten AD-29):** "Exact replay = same tuple and same `PayloadFingerprint` (shared canonicalizer); same tuple with a different fingerprint → `Conflict`. The tuple's principal component is (`PrincipalKind`, `PartyId` or `system`), never roles. The idempotency record is the target aggregate's stream: it stores (`ClientKey`, `PayloadFingerprint`, result identity) for every accepted key for its lifetime. `HoldId = H(hold, TenantId, PartyId, ClientKey)`, `ExportId = H(export, TenantId, PartyId, ClientKey)`, `DeletionRequestId = H(deletion, TenantId, PartyId, ClientKey)`."
- **Disposition:** Autofix.

### B-15 — `EXT-CONV-AI-1` does not require Conversations to persist the Agents-supplied `MessageId`, and the AD-31 decoration slot is keyed by whichever id Conversations stores (prior A-17 register half missing)

- **Units:** Story 6.6/7.4 posting and provenance accessor vs the `EXT-CONV-AI-1` and `EXT-CONV-UI-1` owners.
- **ADs obeyed:** Consistency `Identity` (Conversations owns the `MessageId` namespace; Agents supplies the value), AD-13, AD-31 (slot keyed by `MessageId`), register seam (2) (Agents-supplied idempotency key and metadata only).
- **Incompatibility:** The seam owner assigns its own `MessageId`; Agents' accessor indexes provenance by the derived id; the slot passes the stored id → no provenance marker ever renders (FR-11's participant-visible marker silently absent), and audit records an id Conversations never stored.
- **Closing text:** "Register `EXT-CONV-AI-1` seam (2) `RequiredArtifact`: the append persists the Agents-supplied `MessageId` verbatim or rejects; a differing returned id is a typed incompatibility that blocks posting. `EXT-CONV-UI-1` slot keys are that id. Recovery looks up by idempotency key."
- **Disposition:** Discuss (owner acceptance), then autofix the register.

### B-16 — `Agents.Approver` is a FrontComposer policy mapped one-to-one to an FR-33 role, but proposal authority is the AD-8 predicate, so one unit requires both and the other the predicate alone

- **Units:** Story 7.4 API authorization vs Story 7.1 UI route gating and Story 5.4 role evidence.
- **ADs obeyed:** AD-30 (roles map one-to-one to policies including `Agents.Approver`; missing assignment fails closed), AD-8 (Conversation-scoped predicate; sources Facilitator, predefined Parties, tenant roles), FR-33 ("Approvers resolved under FR-7").
- **Incompatibility:** A Facilitator-source approver without the `Agents.Approver` tenant assignment is denied by Unit 1 (role AND predicate) and allowed by Unit 2 (predicate). SM-4 counts one as an unauthorized attempt.
- **Closing AD text (tighten AD-30):** "`Agents.Approver` is a navigation policy only; authorization of every proposal action is the AD-8 predicate alone, and a tenant-role source names Tenants roles independent of the FrontComposer policy."
- **Disposition:** Autofix.

### B-17 — `BudgetPeriod` is the ledger stream key but is defined by a commit timestamp that does not exist before dispatch

- **Units:** Story 6.4 reservation routing vs Story 8.4 budget period close and `budget-reservation-usage`.
- **ADs obeyed:** AD-2 (`BudgetLedger` keyed `TenantId`, `BudgetPeriod`), AD-21 ("attributes it to the current `BudgetPeriod`"; "reservations never migrate between periods"), AD-28 (DomainInstant = commit timestamp; aggregates never read a clock).
- **Incompatibility:** Unit 1 routes by `EvaluatedAt`; Unit 2 records the commit month and, at a month boundary, holds a reservation whose DomainInstant is October in the September stream — which it can neither validate nor migrate.
- **Closing AD text:** "`BudgetPeriod` is the UTC calendar month of the reservation command's `EvaluatedAt`, recorded in the reservation event; the period stream rejects a reservation whose `EvaluatedAt` falls outside it. Period close is a `SystemTimer` activity using `EvaluatedAt`."
- **Disposition:** Autofix.

### B-18 — A regeneration runs under a `Workflow` principal whose `OnBehalfOfPartyId` is the snapshot caller, so rate limits and audit actor are charged to a Party that did not act

- **Units:** Story 7.3 regeneration vs Story 6.1 rate limits and Story 8.5 audit.
- **ADs obeyed:** AD-30 (`Workflow` carries `OnBehalfOfPartyId` equal to the snapshot caller), AD-21 (per-Party rate limits), FR-16 (regeneration is a chargeable call).
- **Incompatibility:** Unit 1 charges the regeneration to the Approver who requested it; Unit 2 to the caller (the only Party on the envelope). The caller can be rate-limited by other people's regenerations.
- **Closing AD text:** "`OnBehalfOfPartyId` is the Party whose command initiated the current step (the caller for the initial generation, the requesting Approver for a regeneration); `CallerPartyId` is carried separately. Per-Party limits are charged to `OnBehalfOfPartyId`."
- **Disposition:** Autofix.

### B-19 — The sequence diagram reads the complete Conversation before rate limits, lifecycle, enablement, block, and kill switch, contradicting AD-13's FR-8 order; a new per-caller concurrency bound has no configuration owner

- **Units:** Story 6.1 workflow (built from the diagram) vs Story 6.2 context activity and Story 8.5 blocked-reason counts.
- **ADs obeyed:** AD-13 (FR-8 order: rate limits before context measurement), the diagram (Conv read → rate limits → reserve), AD-21 ("a maximum of concurrent nonterminal interactions per caller ... no implicit defaults").
- **Incompatibility:** A rate-limited caller triggers a full authorized Conversations read and tokenizer measurement in Unit 1 (NFR-9's 2-second pre-Provider rejection gate at risk; a blocked Conversation read is recorded as the reason instead of `RateLimited`, moving FR-25/SM-C4 counts). Unit 2 rejects first. The concurrency bound is a required configuration in Unit 1 (unconfigured → blocked) and hard-coded in Unit 2.
- **Closing AD text:** "The sequence diagram follows FR-8 order; the first failing check in that order is the recorded blocked reason. `MaxConcurrentNonterminalInteractionsPerParty` is a `TenantGovernancePolicy` rate-limit field with no implicit default."
- **Disposition:** Autofix.

### B-20 — Per-interaction DEKs make class-scoped deletion either an over-erasure or a projection-only redaction that AD-22 calls incomplete

- **Units:** Story 8.3 class-scoped deletion vs Story 8.1 class-scoped hold.
- **ADs obeyed:** AD-22 (per-interaction DEK; "redaction rewrites projection copies only"; "restrictive completion means every protected event unprotects as unreadable"), AD-2 (`class:<ContentClass>` scope grammar).
- **Incompatibility:** Unit 1 destroys the DEK for every interaction containing the class (erasing other classes too); Unit 2 redacts projections and reports `Complete` with events still readable.
- **Closing AD text:** "Erasure granularity is the interaction. A class-scoped deletion request enumerates the interactions holding that class in range, is rejected in full if any is pinned, and completes by DEK destruction per interaction; redaction is never a completion path for EventStore payloads."
- **Disposition:** Autofix.

### B-21 — `User` with resolved roles and `Administrator` both describe a human administrator, and aggregates "verify" a principal kind no rule selects

- **Units:** Story 5.2 `AgentSetupMutation` ingress vs Story 5.2 aggregate allowlist check.
- **ADs obeyed:** AD-30 (`User` carries resolved FR-33 roles; `Administrator` is populated by ingress after a role check; aggregates verify allowlisted command set per extension).
- **Incompatibility:** Unit 1 sends every human command as `User` with roles; Unit 2's aggregate requires `Administrator` for setup mutations and rejects. Compliance Inspector and Release Operator have no principal kind at all.
- **Closing AD text:** "Principal kind is selected by the command's declared family: `Administrator` for `AgentSetupMutation`, `AgentActivation`, tenant `PolicyPublication`, tenant `TenantBudgetUpdate`, administrative `ProposalResolution`; `Platform` for the B-8 families and platform `PolicyPublication`; `User` (with roles Approver, Compliance Inspector, Release Operator, Participant) for everything else."
- **Disposition:** Autofix.

### B-22 — The "versioned live-seam matrix" has no home and `BindingStatus` has no vocabulary

- **Units:** Story 5.6 (flips seams to `Live`, stores the matrix in `Hexalith.Agents.Testing`) vs Story 8.7 (expects it in the register for inspection).
- **ADs obeyed:** AD-17 (matrix maps seam → authority, `BindingStatus`; a live seam without its named test blocks completion), register (no such section).
- **Closing text:** "The register gains a `Live-Seam Matrix` section (Seam, Authority, `BindingStatus ∈ {Deferred, Live}`, `RequiredIntegrationTest`); a seam flips only by a register amendment in the same change as its test."
- **Disposition:** Autofix (register).

### B-23 — A non-enabled entry returns the absent-key response under AD-2 and a `Blocked/EntryMissing` readiness result under the register

- **Units:** Story 5.5 by-Provider/model readiness query vs Story 5.3 tenant isolation tests.
- **ADs obeyed:** AD-2 ("a platform entry not enabled for it returns exactly the absent-key response"), register Provider Readiness Contract ("not enabled ... returns `EntryMissing`").
- **Incompatibility:** HTTP 404 in one unit, 200 with a readiness triple in the other; the isolation test passes in one and fails in the other.
- **Closing text:** "The by-key readiness query for a non-enabled or absent entry returns the identical not-found response (AD-2 wins); `EntryMissing` is reported only for the Agent's own selected entry inside `AgentReadinessStatus`."
- **Disposition:** Autofix (register).

### B-24 — `RevisionLag` freshness has two sources for `ExpectedRevision`, and no projection declares its `Basis`

- **Units:** Story 5.2 setup query (expected revision from the caller's accepted-write reference) vs Story 5.5 readiness (expected = current stream revision read server-side).
- **ADs obeyed:** AD-17 (`Freshness` evaluated once with expected and observed revision), Consistency `Freshness`.
- **Incompatibility:** The same read is `Fresh` for Unit 1 and `Stale` for Unit 2 whenever the projection lags by one event.
- **Closing text:** "`ExpectedRevision` is the caller-supplied accepted-write reference; absent it, `RevisionLag` reports `ObservedRevision` only and is `Fresh`; servers never read the aggregate to compute lag. The register projection inventory gains a `FreshnessBasis` column."
- **Disposition:** Autofix.

### B-25 — After the third re-arm `TimerDrift` is "recorded" nowhere in particular and the proposal is `Expired` on every read but never terminal

- **Units:** Story 7.6 timer activity vs Story 8.1 retention (365 days after the terminal instant).
- **ADs obeyed:** AD-28 (re-arm at most three times then record `TimerDrift`), AD-5 (`ExpiresAt` enforced on every read regardless of timer delivery), AD-22 (retention base = terminal DomainInstant).
- **Incompatibility:** Unit 1 stops after the blocker; no `ProposalExpired` event exists, so retention never starts and `pending-proposal-queue` shows `Expired` while `agent-interaction-status` shows `Pending`. Unit 2 records the blocker as a readiness observation on a gate it is not the producer of.
- **Closing text:** "`TimerDrift` is an `AgentInteraction` event. On the third re-arm failure the activity dispatches `ExpireProposal` if `EvaluatedAt >= ExpiresAt`, else records `TimerDrift` and hands the proposal to the `SystemTimer` expiry sweep, which dispatches `ExpireProposal` for every proposal whose stored `ExpiresAt` has passed; the terminal DomainInstant is the commit of `ProposalExpired`; reads render the derived `Expired` state with `Freshness` marked stale until then."
- **Disposition:** Autofix.

---

## Low

### B-26 — `tenant:system` is grammatical for a Platform-kind gate; the register's initial records use an unversioned profile

- **Closing text:** "A Platform-kind gate rejects any `TenantScope` other than `platform`; a Tenant-kind gate rejects `platform` and `tenant:system`. The initial records read `production-like@0`."
- **Disposition:** Autofix (register).

### B-27 — Administrative abandon/retry are unnamed commands; the 15-minute retry window has no base instant; the administrative retry's budget is unbound

- **Closing text:** "`AdministrativelyAbandonProposal` and `AdministrativelyRetryPosting` are distinct commands under the `Administrator` principal, permitted only in `PostingFailed` with an exhausted budget, exempt from the AD-8 predicate; the retry window runs from the first `PostingFailed` `EvaluatedAt`; an administrative retry is exactly one attempt that re-runs every pre-post gate."
- **Disposition:** Autofix.

### B-28 — AD-22 inspection eligibility uses the live AD-8 predicate ("resolved now") that FR-24 defines historically ("was resolved")

- **Closing text:** "Inspection eligibility = the Party appears in any durable Eligible Approver set recorded on the proposal and holds current read access; the not-caller/not-editor exclusions apply to decisions only."
- **Disposition:** Autofix.

### B-29 — `GetCallabilityAsync(tenant, conversation)` may or may not resolve Eligible Approvers, and the panel "submits with the caller principal" that only ingress can resolve

- **Closing text:** "`GetCallabilityAsync` returns the `AgentCallAcceptance` matrix result at one checkpoint joined with Agent lifecycle, kill switch, the `ConversationAgentState` block, and the caller restriction; it never resolves Eligible Approvers or reads content, and is advisory. The panel submits the public call command; the caller `PartyId` is resolved at ingress."
- **Disposition:** Autofix.

### B-30 — Automatic-mode output has no `ProposalVersionId`, yet `MessageId = H(message-id, AgentInteractionId, VersionId)`; the formula also uses `VersionId` where AD-20 uses `ProposalVersionId`

- **Closing text:** "Every allowed generated output, automatic or confirmation, is a `ProposalVersion` with ordinal and kind; `MessageId` and the posting key derive from its `ProposalVersionId`; the name `VersionId` is not used."
- **Disposition:** Autofix.

### B-31 — The materialized Conversation context behind a `ProtectedContentReference` is an event on the interaction stream in one unit and a side store in another, so export and retention cover it in one and not the other

- **Closing text:** "Every `ProtectedContentReference` resolves to a protected payload appended to the owning `AgentInteraction` stream (`ContentCaptured` events), so export manifests, retention, and erasure cover it by construction."
- **Disposition:** Discuss (EventStore payload-protection mechanics were not verifiable from the checkout), then autofix.

---

## Contradictions Found Between ADs, Diagram, Companion, Or Register

| Pair | Contradiction | Resolved by |
| --- | --- | --- |
| AD-20 rank rule / AD-20 "Prevents" | `Loosens` keeps rank and passes the `>=` test | B-1 |
| AD-28 / EventStore stamping | Deadline from a commit timestamp stored in the same payload | B-2 |
| AD-30 / AD-7, AD-21, AD-22 | Workflow allowlist vs block-setting, period close, retention deletion | B-3 |
| AD-7 (internal) | Step sets the block vs "set only by" two human authorities | B-3 |
| AD-2 "exactly" / AD-7 | No bounded owner for membership-established and blocks | B-4 |
| AD-2 hold key / AD-2 owning-aggregate rule | Per-`HoldId` streams cannot answer "any active hold" | B-5 |
| AD-13 protocol / AD-21, FR-8, sequence diagram | Reservation before descriptor vs descriptor before reservation; estimated vs maximum | B-7 |
| AD-30 `Platform` / AD-2 isolation | `system` principal targeting tenant aggregates | B-8 |
| AD-13 / AD-21, FR-12 | Timeout retryable vs `Indeterminate` never retried | B-9 |
| AD-13 fingerprint / AD-20 retry | Evaluated policy versions in the fingerprint vs tighter-policy retry | B-10 |
| AD-12 list / register matrix v2 | Nine named lock-bearing families vs fourteen candidate families | B-12 |
| AD-17 `AuthorizedProducer` / AD-30 | `Platform Maintainer` is not a principal; workflow evidence producer | B-13 |
| AD-2 absent-key / register `EntryMissing` | Not-found vs readiness triple for non-enabled entries | B-23 |
| Sequence diagram / AD-13 FR-8 order | Context read before rate limits | B-19 |
| IMPLEMENTATION-CONVENTIONS | No contradiction; its "trusted result" pattern is what B-2 and B-17 rely on (`EvaluatedAt` in the command). | — |

## Covered Attacks — No Finding

- Two owners of `CapabilityVersion`: the `system` catalog is the sole sequence owner; `TenantProviderEnablement` never bumps it; the high-water mark is per interaction. Convergent.
- `ObservationId` re-observation and conflict detection (A-5 closure): the derivation includes `ObservedAt` and `EvidenceReference`; hourly renewal with unchanged `SourceVersion` produces new revisions; exact duplicates are no-ops. Holds.
- `TenantScope`/`ScopeKind`/`EnvironmentProfile` evaluation (A-6 closure): one reader rule for Platform and Tenant gates. Holds (residual B-26).
- `Freshness` as one server-evaluated value carried unchanged (A-4 closure): API, BFF, and UI cannot diverge on the same payload. Holds (residual B-24).
- AD-27 execution-state references and AD-22 purge scope `workflow-execution-state`: both units keep content out of history; purge is a named scope. Holds (residual B-31).
- AD-24 fence reuse, reclamation, and `BeginInvocation`: the allocator is the single owner; stale fences cannot invoke. Holds.
- AD-29 `AttemptId` for transport retry vs regeneration: the ordinal rule yields one derivation. Holds (residual on descriptor timing, B-7).
- AD-23 frozen cohort and immutable pre-fault terminal decisions: convergent.
- AD-26 discriminated `BrowserTimingSample` and `SampleId`: convergent with the register.
- AD-4 snapshot vs live re-reads: the split between frozen provenance and never-frozen gates is stated field by field. Holds.
- AD-9 `SecretReference` handle stability under rotation and `SecretConfigured` as a durable probe fact: no second writer. Holds.
- AD-16 platform-owned host and AD-31 dependency direction (Conversations never references Agents): no cycle can be built. Holds.
- AD-10 `PlatformNotReady` additive code: the register's platform-only list and the tenant-facing mapping agree; `Disabled` remains visible to tenants by design (platform disable is a public fact under FR-4). No hole.
- `hexa` one-per-tenant with tenant-wide response mode (AD-2/OQ-20): `AgentInteractionId` embeds `AgentId`, so a future second Agent cannot collide. Holds.

## Recommended Fix Order

1. B-1 — before Story 6.3 or 8.4 is ready-for-dev; the current text mandates the unsafe branch.
2. B-8, B-12, B-21 — before Epic 5 acceptance; they decide principal shapes and lock vocabulary every later story inherits.
3. B-2, B-7, B-9, B-17 — before Story 6.4/7.1/7.6; they decide clocks, attempt timing, and money.
4. B-3, B-4, B-5, B-6, B-13 — before Stories 6.6, 7.4, 8.1, 8.3; they decide owners and identities for membership, holds, and safety decisions.
5. B-10, B-11, B-14, B-15, B-16, B-18, B-19, B-20, B-22, B-23, B-24, B-25 — before their consuming stories; most are a paragraph each.
6. B-26 through B-31 — editorial pass.
