# Adversarial-Divergence Review — 2026-09-12 v2

## Verdict

**CHANGES REQUIRED.** The spine is unusually explicit at individual aggregate and workflow seams, but it still permits two critical failures: the operation-gate matrix can make bootstrap and repair commands impossible, and the legal-hold/deletion rules promise an all-or-nothing irreversible outcome without defining a cross-stream exclusion protocol that can deliver it. Four further high-severity seams allow independently built units to disagree about limit lifetime, actor identity, proposal-index truth, and whether the core Conversations dependency is usable.

## Scope And Method

This lens attacked `ARCHITECTURE-SPINE.md` by pairing independently implemented units one level below the initiative architecture. A finding exists only where both units can cite the current architecture or one of its explicitly normative registers and still produce incompatible behavior. Local implementation was sampled only where it sharpened a contract boundary; this is not a general brownfield review.

The pass covered shared-data shape, state ownership and mutation, workflow/idempotency, extension contracts, operations, and governance. It did not modify the spine or its sources.

## Severity Summary

| Severity | Count |
| --- | ---: |
| Critical | 2 |
| High | 4 |
| Medium | 2 |
| Low | 0 |

## Critical

### C1 — The “non-circular” operation matrix still deadlocks bootstrap and repair commands

**Precise references**

- Spine AD-17 makes matrix version 3 the mandatory single contract for every public command and workflow activity, and makes an absent required gate block (`ARCHITECTURE-SPINE.md:276`).
- Spine AD-30 puts create-only `ProvisionHexa` inside `AgentSetupMutation`, platform catalog writes inside `ProviderCatalogMutation`, tenant cost configuration inside `TenantBudgetUpdate`, and tenant Provider enablement inside `TenantProviderEnablement` (`ARCHITECTURE-SPINE.md:362`).
- The register requires `LR-PARTY-IDENTITY` for `AgentSetupMutation`, `LR-SECRETS` and `LR-COST` for `ProviderCatalogMutation`, `LR-COST` for `TenantBudgetUpdate`, and `LR-PROVIDER` for `TenantProviderEnablement` (`launch-readiness-register.md:148-171`).
- `LR-PARTY-IDENTITY` requires an already unique active Agent Party identity; `LR-COST` requires already-current pricing and configured caps; and `LR-PROVIDER` requires tenant enablement and current data-handling acceptance (`launch-readiness-register.md:99-112`, `208-212`).
- AD-17 also says `tenant:system` is invalid, while AD-30 places platform catalog and platform policy state in tenant `system`; nevertheless their matrix rows require the Tenant-scoped `LR-TENANT-ACCESS` gate (`ARCHITECTURE-SPINE.md:276`, `launch-readiness-register.md:116-139`, `148-159`).

**Two-unit counterexamples**

1. The matrix evaluator team applies the row literally. A new tenant has no Agent Party identity, so `LR-PARTY-IDENTITY` blocks `ProvisionHexa`; the identity that would satisfy the gate can never be created. The provisioning team treats `ProvisionHexa` as a recovery exception and creates the Party and Agent. Both are defensible from AD-17/AD-30, but one system deadlocks while the other bypasses the claimed single matrix.
2. The cost-gate team refuses `TenantBudgetUpdate` while `LR-COST` is blocked for missing caps. The budget administration team permits the update because it is the operation that establishes those caps. The same loop exists for the first pricing/secret-bearing `ProviderCatalogMutation` and for `TenantProviderEnablement` while `LR-PROVIDER` says the entry is not enabled.
3. The global catalog team evaluates `ProviderCatalogMutation` in `system`; the gate evaluator cannot resolve the required Tenant-scoped `LR-TENANT-ACCESS` record because `tenant:system` is forbidden. Another evaluator silently skips the tenant gate or accepts a caller-supplied target tenant, producing either an unusable platform command or a tenant-dependent global catalog decision.

This is a systemic contract failure, not four unrelated missing exceptions. The matrix names end-state readiness checks where command-specific preconditions are needed.

**Action classification: autofix before implementation.** Add explicit bootstrap/recovery semantics to the normative matrix. For each mutation family, define the target scope and the subset of a gate it must satisfy while changing the fact that gate normally evaluates. `ProvisionHexa` must be allowed to clear `MissingPartyIdentity` while still enforcing exact target, Parties seam, tenant access, and payload/audit controls. Catalog, budget, and enablement writes need equivalent target-aware rules. Platform-scoped mutations must use only Platform-scoped gates or name an explicit target-tenant evaluation; they cannot require an unresolvable Tenant-scoped record. Add contract fixtures for empty-state creation and blocked-state recovery for every administrative family.

### C2 — Legal hold and class deletion cannot meet the promised all-or-nothing rule without a cross-stream exclusion protocol

**Precise references**

- AD-2 places each `LegalHold`, `ProtectedDeletion`, and `AgentInteraction` in separate EventStore streams and says unrecoverable decisions read owning aggregate state at an expected revision (`ARCHITECTURE-SPINE.md:144`).
- AD-22 says a class-scoped deletion enumerates interactions and is “rejected in full if any is pinned”; `ApplyHold` separately resolves a scope, pins each DEK, and becomes `Active` only after every pin is confirmed. Erasure reads each interaction's own pinned-hold set, while `DestroyDek` is irreversible (`ARCHITECTURE-SPINE.md:308`).
- AD-3 and the command-step convention limit a durable step to at most one server-trusted command; there is no multi-stream transaction, reservation, fence, or ordering rule joining all affected interactions (`ARCHITECTURE-SPINE.md:156`, `IMPLEMENTATION-CONVENTIONS.md:7-20`).

**Two-unit counterexample**

- The Legal Hold team implements the specified two-phase saga: record the hold/checkpoint, then pin interaction DEKs one at a time, finally mark the hold `Active`.
- The Protected Deletion team enumerates the same class, sees no pinned interaction at its checkpoint, then destroys DEKs one at a time, checking each interaction at its own expected revision immediately before destruction.

Both obey the literal rules. A concurrent run can destroy interaction A after deletion's pre-check, pin interaction B, then cause deletion to discover the pin. The deletion has already irreversibly erased A but must now be “rejected in full”; the hold can no longer pin A and cannot become `Active`. Reversing the order merely creates the symmetric race. EventStore optimistic concurrency on one interaction does not make the whole class atomic.

**Action classification: discuss the policy, then add a binding AD.** Decide whether class deletion is truly all-or-nothing. If yes, define a durable exclusion protocol before any key destruction: freeze the resolved interaction set, acquire a monotonic deletion/hold fence on every member, reject on any failed acquisition, and only then authorize irreversible destruction; a hold request must contend on the same fence. Recovery must prove that no DEK is destroyed before the full prepare phase commits. If best-effort partial completion is acceptable, remove “rejected in full,” define visible partial outcomes and retry semantics, and reconcile the PRD/governance evidence accordingly.

## High

### H1 — `BudgetLedger` has three incompatible lifetimes for one rate/concurrency record

**Precise references**

- AD-2 partitions `BudgetLedger` by `(TenantId, BudgetPeriod)`, where the period is a UTC calendar month (`ARCHITECTURE-SPINE.md:144`).
- AD-21 says `AdmitCall` in that ledger enforces rolling per-Party/per-Conversation rate windows and `MaxConcurrentNonterminalInteractionsPerParty`, but also says each attempt's charge releases as soon as its `ProviderAttempt` is terminal, regardless of whether the interaction is terminal (`ARCHITECTURE-SPINE.md:302`).
- AD-5 says an automatic call remains nonterminal while its posting record is nonterminal and releases the caller's per-Party concurrency slot only when that posting record reaches its terminal mapping (`ARCHITECTURE-SPINE.md:168`).

**Two-unit counterexamples**

1. The ledger team releases the caller slot when generation's Provider attempt settles, as AD-21 requires. The interaction/status team holds the slot through `Approved`/`PostingPending`/`PostingFailed` until posting terminalizes, as AD-5 requires. A second call is admitted by one unit while the other reports the caller at the concurrent-interaction ceiling.
2. One ledger implementation treats a rolling-window hit as durable until its window expires; another treats it as the “attempt charge” AD-21 says to release at attempt terminal. The same call count produces different rate-limit decisions.
3. At 00:00 UTC on the first of a month, one implementation checks only the new `(TenantId, BudgetPeriod)` stream; another joins the previous stream to preserve a rolling window and open concurrency. Both follow the monthly aggregate boundary, but only one sees calls made minutes earlier or interactions still open from the prior month.

**Action classification: discuss, then tighten AD-2/AD-21.** Separate three ownership concepts: immutable rolling-window consumption, tenant-wide open-interaction/concurrency leases, and monthly monetary reservations/settlement. Either give the first two tenant-wide aggregates or define an exact cross-period query/state-carry protocol. State the release event for each concept; do not use one “charge” for all three. Reconcile AD-5's interaction-terminal release with AD-21's attempt-terminal release.

### H2 — The principal envelope cannot enforce AD-22's person-level separation of duties

**Precise references**

- AD-7 explicitly says `Administrator` and `Platform` principals carry no `PartyId` (`ARCHITECTURE-SPINE.md:184`).
- AD-22 computes a subject set of specific human Parties, forbids the Inspector themself or a subject-set member from acting as second party, and permits a Platform Operator to approve inspection/export or release a hold (`ARCHITECTURE-SPINE.md:308`).
- AD-30 represents `Platform` only as a reserved platform extension with `ActorTenantId = system`, and then grants that principal `LegalHoldRelease` when it is the recorded second-party approver (`ARCHITECTURE-SPINE.md:362-364`).
- The audit convention persists the AD-30 principal as the event actor; no additional stable authenticated-human identifier is required (`ARCHITECTURE-SPINE.md:542`).

**Two-unit counterexample**

- The governance aggregate team sees an Inspector `User(PartyId=P)` request followed by a `Platform` approval with no Party identity. It treats the different principal kind as a distinct second party and accepts.
- The ingress team knows the same authenticated human P holds both Compliance Inspector and Platform Operator roles, but AD-30 requires the downstream Platform principal to omit `PartyId`; it cannot transmit the evidence needed for the aggregate to reject self-approval. Another implementation may reject every Platform approval because distinctness is unprovable.

Both are literal, fail-closed interpretations, yet one permits self-approval and the other makes the Platform approval branch unusable. The same loss prevents durable attribution of which administrator performed a configuration change when the reserved `Administrator` principal is used.

**Action classification: autofix.** Every human-originated reserved principal must carry a stable, tenant-safe authenticated actor identity distinct from its authorization role and target tenant. Bind the HMAC to that identity and require AD-22 to compare it with requester, Inspector, subject set, and prior approvers. If platform automation also needs a non-human principal, give it a separate kind and disallow it from satisfying a human second-party rule unless Product/Governance explicitly chooses that policy.

### H3 — The non-terminal proposal index has no crash-consistent owner across two aggregates

**Precise references**

- AD-2 makes `AgentInteraction` the owner of proposal lifecycle and `ConversationAgentState` the owner of the Conversation-wide non-terminal index (`ARCHITECTURE-SPINE.md:144`).
- AD-7 says orchestration maintains that index “idempotently alongside every `AgentInteraction` proposal-state transition” so external removal can abandon every affected proposal (`ARCHITECTURE-SPINE.md:184`).
- AD-3 and the companion convention allow at most one command per durable step, while AD-13 assigns business truth to `AgentInteraction`; no transactional outbox, acknowledged projection checkpoint, repair scan, or required write order binds the second stream (`ARCHITECTURE-SPINE.md:156`, `244`; `IMPLEMENTATION-CONVENTIONS.md:7-20`).

**Two-unit counterexample**

- Team A commits the `AgentInteraction` transition first, then updates `ConversationAgentState`. A crash between writes leaves a live proposal absent from the index; an external-removal sweep misses it, and it can later remain actionable.
- Team B writes the index first, then commits the interaction transition. A rejected/conflicting interaction command or crash leaves a phantom index member; later removal tries to abandon an interaction that never entered that state, and terminal/count projections disagree.

Both orders satisfy “idempotently alongside.” Expected revision protects each stream separately and cannot establish the cross-stream invariant.

**Action classification: autofix.** Name one source of truth and one delivery protocol. Preferred: append the proposal transition only to `AgentInteraction`, emit an idempotent outbox/projection item carrying interaction revision and target index action, let `ConversationAgentState` record the applied source revision, and require removal to reconcile any lag against an authoritative by-Conversation nonterminal query/checkpoint before it completes. Alternatively define a saga with explicit prepared/confirmed states and recovery. Add failure-injection tests at both write boundaries.

### H4 — One indivisible dependency record bundles six V1-critical seams with a deferred-beyond-V1 seventh seam

**Precise references**

- AD-6 says the first six `EXT-CONV-AI-1` seams are the only V1 membership/posting/resolution/deletion paths and that an Uncommitted record blocks their consuming stories and runtime callability. It then says seam 7 is uncommitted, unusable, and Deferred Beyond V1 (`ARCHITECTURE-SPINE.md:178`, `825`, `870`).
- The dependency register defines status at the whole-record level: any unknown field makes every consumer blocked (`external-dependency-register.md:35-47`).
- The same record now requires all seven seams, remains `Uncommitted`, and explicitly says adding deferred seam 7 keeps the whole record Uncommitted until all seven are accepted (`external-dependency-register.md:51-67`).

**Two-unit counterexample**

- The dependency/gate team treats `EXT-CONV-AI-1` atomically and blocks all V1 consumers until the retraction signal also commits.
- The Conversations integration team follows the spine's V1 distinction, delivers and verifies seams 1-6, and treats seam 7 as unusable deferred scope. It expects membership, context, posting, and deletion propagation to become callable.

The first implementation makes an explicitly beyond-V1 metric seam a prerequisite for all V1 Conversations behavior; the second invents partial availability that the register schema does not permit.

**Action classification: autofix.** Split seam 7 into its own `EXT-CONV-RETRACTION-1` record, or add independently versioned/statused sub-records and change all consumers to name the required subset. `RQ-1` and core V1 stories should consume seams 1-6; Automatic-mode enablement may consume the retraction record only if the OQ-23 decision selects that branch.

## Medium

### M1 — `ReadmitPending` does not bind who starts re-admission or what confirms `CurrentMirror`

**Precise references**

- AD-2 says clear creates a `Readmit` mirror, leaves `ReadmitPending`, and only the next accepted membership step moves to `Joined` (`ARCHITECTURE-SPINE.md:146`).
- AD-7 says `ReadmitPending` adds the participant, that clear never records `Joined`, and that `CurrentMirror` has mutually exclusive `Pending`, `Confirmed`, or `Refused` outcomes (`ARCHITECTURE-SPINE.md:186-190`).

**Two-unit counterexample**

- The mirror worker treats clear as immediately starting the idempotent Conversations add; its accepted add both confirms `CurrentMirror` and records `Joined`.
- The call workflow treats clear as recording intent only. It waits for the next Agent Call's membership step to add the participant, leaving the current mirror `Pending` indefinitely if no call arrives; it may require a separate acknowledgement command before marking `Confirmed`.

Both fit “next accepted membership step,” but public status, focus/UI remediation, and SM-C4 recovery timing differ.

**Action classification: autofix.** Bind the owner and trigger of a `Readmit` attempt, state whether it runs immediately or on demand, and define one atomic outcome command that maps an accepted add to both `CurrentMirror=Confirmed` and the intended membership state. Define retry/recovery when the acknowledgement is lost.

### M2 — The posting timeout remains an admitted divergence point immediately above implementation

**Precise references**

- AD-5 permits any stored `PostingPending` deadline no shorter than the Conversations posting timeout and explicitly fixes no duration (`ARCHITECTURE-SPINE.md:168`).
- `ARCH-A-14` acknowledges that neither the PRD nor spine selects the duration and gives no calendar retirement date, only “before the timeout/recovery story” (`ARCHITECTURE-SPINE.md:859`).
- AD-28 makes that deadline drive durable timeout, lookup, retry, and recovery outcomes (`ARCHITECTURE-SPINE.md:348`).

**Two-unit counterexample**

- The workflow team chooses exactly the seam timeout.
- The recovery team chooses twice that timeout to tolerate a lost acknowledgement.

Both satisfy “no shorter than,” but during the difference window one unit keeps `PostingPending` while the other performs existence lookup and transitions to `Posted`, `PostingFailed`, or continued pending. Timer fixtures and operational recovery cannot share expected outcomes.

**Action classification: discuss now or keep explicitly blocked.** Select one duration/configuration authority, an immutable snapshot rule, and a compatibility constraint with the seam timeout before any posting/recovery story starts. If intentionally deferred, add a dated pre-story gate rather than allowing independent implementation.

## Consolidated Failure Scenarios

### Scenario A — A fresh tenant can never become configurable

1. `ProvisionHexa` declares `AgentSetupMutation`.
2. Matrix v3 requires `LR-PARTY-IDENTITY`.
3. That gate is blocked because `hexa` has not yet been provisioned.
4. A strict evaluator returns a blocker forever; a permissive evaluator bypasses the normative matrix.

The same shape repeats for initial caps, Provider catalog setup, and tenant Provider enablement.

### Scenario B — A hold and a deletion both succeed partially

1. Hold H resolves interactions A and B; neither is pinned yet.
2. Deletion D resolves the same class and sees no pin.
3. D irreversibly destroys A's DEK.
4. H pins B; D then detects the pin and must reject “in full.”
5. H cannot pin erased A, D cannot undo A, and neither aggregate can reach its promised result.

### Scenario C — One human supplies both sides of a governed approval

1. Party P requests an inspection or hold release as Compliance Inspector.
2. The same authenticated human invokes the Platform Operator route.
3. Ingress emits the required `Platform` principal without PartyId.
4. The aggregate sees a different principal kind and either accepts self-approval or rejects every Platform approval because distinctness cannot be proven.

### Scenario D — Conversation removal misses a live proposal

1. `AgentInteraction` commits `ProposalCreated`.
2. The process crashes before `ConversationAgentState` indexes it.
3. A Facilitator removes `hexa`; the index-driven sweep sees no proposal.
4. The proposal remains pending/actionable even though the architecture says removal reaches every nonterminal proposal.

## Recommended Closure Order

1. Repair matrix scoping/bootstrap semantics (C1) before any administrative API or readiness evaluator is implemented.
2. Decide and encode the hold/deletion exclusion protocol (C2) before any key-destruction path exists.
3. Split rate, concurrency, and monthly-cost ownership (H1), then bind cross-aggregate proposal-index delivery (H3).
4. Add authenticated human identity to reserved principals before audit/governance stories (H2).
5. Split `EXT-CONV-AI-1` seam 7 from the core V1 record (H4), then close the readmission and posting-timeout details before their consuming stories.
