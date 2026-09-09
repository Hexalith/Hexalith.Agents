# Security / Compliance / Data-Integrity Review — 2026-09-09

## Lens

Independent security, compliance, and data-integrity reviewer walking ten concerns against the re-distilled spine (AD-1..AD-31, updated 2026-09-09). The test for every finding is unchanged from the 2026-09-08 review: not "does the spine mention it" but "can two separately built, literally compliant units still produce a cross-tenant leak, an unaudited or privilege-escalated effect, an unerasable copy of sensitive content, a weakened safety decision, or a wedged budget". A finding requires a concrete compliant-but-wrong build. Findings that merely restate a prior finding are not repeated; where a prior finding was closed in text but its closure is not implementable as written, the residual is reported as a new finding and cross-referenced.

Reviewed authority (read in full):

- `ARCHITECTURE-SPINE.md` (updated 2026-09-09) and `IMPLEMENTATION-CONVENTIONS.md`.
- `launch-readiness-register.md` and `external-dependency-register.md` (both amended 2026-09-09): gate inventory, `OperationGateMatrixVersion = 2`, Provider Readiness Contract (`EntryMissing`, `PlatformNotReady`), projection inventory and deletion scope, `EXT-*` records (seven of eight `Uncommitted`).
- PRD `prd-agents-2026-06-23/prd.md` FR-19..FR-33, §8 (including the 8.1 assumptions index), §9 Data Governance And Audit.
- Prior review `review-2026-09-08-security-data-integrity.md` (S-1..S-15) and `VALIDATION-REPORT-2026-09-08.md` for the closure state.
- EventStore reality in `references/Hexalith.EventStore/src`: `IEventPayloadProtectionService` (whole-payload protect/unprotect keyed by `AggregateIdentity`, typed `PayloadUnprotectionOutcome`), `PayloadProtectionState`, `EventStorePayloadProtectionMetadata` (`Scheme`, `KeyAlias` ≤ 256 chars, bounded `CompatibilityFlags`), `UnreadableProtectedDataReason` (Stories 22.7c/22.7d "key lifecycle workflow, restored-backup governance" still owned by EventStore, not shipped), `NoOpEventPayloadProtectionService` (the only shipped provider), `EventStoreDataProtectionOptions` / `AddEventStoreDataProtection` (an ASP.NET Data Protection key ring that backs the query cursor codec only — one ring per application name), `EventPersister` (protects every event of a domain through the one registered service), `EventPublisher` (unprotects before `DaprClient.PublishEventAsync` to the pub/sub topic), `ProjectionEventWireBuilder` (unprotects for projection delivery), `SnapshotManager` (protects snapshots through the same service; unreadable snapshot forces replay), `AggregateActor` (an unreadable protected event during rehydration throws `ProtectedDataUnreadableException` and dead-letters the command), `DaprBackupCommandService` (stream export refuses `Protected`/`ProviderOpaque` payloads with `ProtectedPayloadUnavailable`; restore is explicitly deferred), and `CommandsController` / `SubmitCommandExtensions` (the public `[Authorize]` command ingress strips exactly one reserved key, `actor:globalAdmin`, and forwards every other extension verbatim).
- Agents code reality only where it shows what a builder does when the spine is silent: every shipped interaction orchestrator (request, context, gate, edit, regeneration, approval, rejection, abandonment, expiry, posting) dispatches under `actor:agentsAdmin`, the same key that gates `Agent` configuration; `EventStoreAgentCommandDispatcher` "performs no authorization and populates no trusted extension"; `Application/Workflows` and `Application/Activities` remain empty; no `system` tenant constant exists yet in Agents (Tenants defines `DefaultTenantId = "system"` for its global-administrators stream).

## Verdict

**FAIL — pending 2 critical and 6 high tightenings; all are expressible as Rule text (autofix) except two product/governance decisions (H-6 Platform principal scope, M-2 two-person deletion) that the PRD has already taken and the spine must simply bind.**

The 2026-09-09 re-distillation is a large step: every one of the fifteen 2026-09-08 findings is addressed in text (see "Prior Findings Now Closed"), the `system`-tenant catalog, `TenantId`-bearing identities, the AD-30 principal model, the AD-27 execution-state boundary, the DEK/KEK hierarchy, bounded reservations, the audit envelope, and the kill switch are all now Rules. The verdict remains FAIL for two reasons that a builder cannot fix by reading the spine harder:

1. **AD-14/AD-22 name a mechanism — "EventStore payload protection under the AD-22 key hierarchy" — that, as EventStore exists today, protects whole payloads per aggregate stream, unprotects them onto the pub/sub broker, and makes a stream un-rehydratable once its key is gone.** A compliant build that destroys the per-interaction DEK therefore also erases the interaction's non-content facts, can never append the deletion tombstone or a later hold decision to that stream, and leaves plaintext copies on the broker and in read-model stores that no purge scope names (C-1, H-1, H-2).
2. **AD-30's trust boundary has no enforcement point.** The Agents API ingress strips reserved extensions, but Agents dispatches to the EventStore command route, which is a second authenticated ingress that forwards `actor:agentsAdmin` and every `*:validation` verdict verbatim from any caller. The rule "a reserved extension arriving from a non-ingress path is a rejection" cannot be evaluated by a pure aggregate that only sees the envelope (C-2).

Counts: Critical 2 · High 6 · Medium 9 · Low 5 (22 findings). Prior findings closed: 15 of 15 (7 outright, 8 with a residual reported below).

## Findings

### Critical

#### C-1 — Whole-payload protection makes DEK destruction erase the stream's non-content facts and freeze the aggregate; "restrictive completion" and the tombstone are then unappendable

- **Severity:** Critical
- **Concern:** 5 (payload protection / crypto-erasure), 6, 10
- **ADs:** AD-14, AD-22, AD-27, AD-2 ("reads hold and policy state from the owning aggregate"), AD-17 (`LR-AUDIT-PROTECTION-DELETION`); register deletion scope
- **Evidence:** `IEventPayloadProtectionService.ProtectEventPayloadAsync(AggregateIdentity, IEventPayload, eventTypeName, bytes, …)` is invoked by `EventPersister` for every event of the domain and transforms the whole serialized payload. The natural per-interaction DEK implementation keys on `identity.AggregateId` and protects every `AgentInteraction` event. `AggregateActor` rehydration calls the readability boundary for every event; an `Unreadable` outcome (`KeyInvalidatedOrDeleted`) throws `ProtectedDataUnreadableException` and dead-letters the command. `SnapshotManager` deliberately does not delete an unreadable protected snapshot and forces replay, which then also fails. AD-22 requires that after erasure "every protected event unprotects as unreadable", that the interaction retains a "support-safe non-content tombstone", that inspection evidence "survives loss of the Source Conversation", and that a hold decision reads the owning aggregate at an expected revision.
- **Compliant-but-wrong scenario:** Epic 8 implements the DEK provider exactly as AD-22 reads: one DEK per `AgentInteractionId`, wrapping every event and snapshot of that stream. Retention expiry destroys the DEK through `EXT-SECRETS-1`. Every event of the stream is now unreadable, so (a) the `ProtectedDeletion` activity's follow-up command to `AgentInteraction` (`DeletionCompleted` / tombstone) is dead-lettered because the actor cannot rehydrate; (b) `SafetyDecision`, `ProviderInvocationAuthorized`, posting outcome, approval Party, role basis, and `CorrelationId` — the audit facts FR-24 and SM-5 require to survive — are gone with the content; (c) the `audit-evidence` projection cannot be rebuilt for that interaction after any projection reset; (d) a later legal hold naming that interaction cannot be evaluated "from the owning aggregate at an expected revision" because the aggregate has none. `LR-AUDIT-PROTECTION-DELETION` still passes: every protected event does unprotect as unreadable, and every named projection does report purged. Conversely a second team, seeing the rehydration failure, protects only "content-bearing" events by type name — but no event contract separates content from metadata, so `ProposalVersionRecorded` (content plus author plus version id plus kind) is either wholly erased or wholly kept.
- **Missing invariant:** erasure granularity below the stream; a content/metadata segregation rule for event contracts; a rehydration rule for post-erasure streams.
- **Proposed Rule (AD-22 amendment, with an AD-14 sentence):** "Content and facts are segregated at the event-contract level: every content-bearing Agents event carries its sensitive content only inside a `ProtectedContent` envelope field (ciphertext under the interaction DEK, plus `ContentDigest`, DEK `KeyAlias`, and scheme), and every other field of that event — identities, versions, ordinals, Party references, role basis, decision codes, outcome, timestamps, correlation — is plaintext to EventStore and is never under the DEK. `IEventPayloadProtectionService` is registered for the Agents domain as a pass-through for non-content events and MUST NOT wrap whole payloads under the interaction DEK; the Agents orchestrator seals `ProtectedContent` before dispatch so that aggregates, snapshots, replay, projections, and the broker only ever see ciphertext for content. Cryptographic erasure destroys the DEK and leaves every event replayable; an aggregate `Apply` treats a `ProtectedContent` field whose key is destroyed as `Erased` (a typed state, never an exception) so the stream accepts the tombstone, hold, inspection, and deletion-completion commands after erasure. Restrictive completion means every `ProtectedContent` field of the interaction decrypts as `Erased`, every named projection reports purged, and `workflow-execution-state` reports purged; it is asserted by a read of the stream after erasure, not by the absence of readable events." Add to AD-17 test obligations: "a post-erasure rehydration test appends a command to an erased `AgentInteraction` stream and proves it is accepted."
- **Disposition:** autofix (Workflows, Activities, and the protection provider are unbuilt; the shipped event contracts need one additive envelope field).

#### C-2 — Reserved `actor:*` and `*:validation` extensions are trusted by aggregates but the EventStore command ingress forwards them from any authenticated caller; AD-30's "non-ingress path is a rejection" has no enforcement point

- **Severity:** Critical
- **Concern:** 2 (authN/authZ), 1
- **ADs:** AD-30, AD-12, AD-16, AD-3, Consistency Convention "Trusted verdicts"; register `LR-TENANT-ACCESS`
- **Evidence:** AD-30: "The ingress removes every client-supplied reserved extension before any handler runs; a reserved extension arriving from a non-ingress path is a rejection and an audited security event." The Agents ingress is the Agents API. Agents dispatches through `EventStoreAgentCommandDispatcher` to EventStore's `POST /api/v1/commands`, an `[Authorize]`d route whose `BuildTrustedExtensions` drops only `actor:globalAdmin` and copies every other key verbatim into the envelope; `SubmitCommandExtensions.ToCommandEnvelope` does the same. The aggregate then trusts `actor:agentsAdmin` / `actor:agentsProviderAdmin` / `provider:selectionValidation` / `party:linkValidation` because "server-populated only" is a comment, not a check. Nothing in AD-16 or `EXT-HOST-1` says the EventStore command route is unreachable by end-user principals, and `LR-TENANT-ACCESS` tests "cross-tenant denial for every affected public/runtime path" without naming this path.
- **Compliant-but-wrong scenario:** The platform host exposes the EventStore command route behind the same identity provider as the Agents API (the sibling modules' shipped topology). A Conversation Participant of tenant A obtains a valid JWT, posts `SubmitCommand{Tenant: A, Domain: agents, CommandType: UpdateAgentInstructions, Extensions: {"actor:agentsAdmin": "true"}}` directly to EventStore, and rewrites `hexa`'s Agent Instructions, Approver Policy, or lifecycle in tenant A — a within-tenant privilege escalation that FR-33 forbids and SM-4 counts. With `Tenant: system` and `actor:agentsProviderAdmin` the same request administers the platform catalog if EventStore's own tenant check accepts `system` for that principal. Every Agents unit is literally compliant: the API stripped what reached it, the orchestrator repopulated only what it vouched for, the aggregate verified the key. AD-30's "rejection and audited security event" never fires because the aggregate cannot distinguish an ingress-populated key from a forged one.
- **Missing invariant:** an enforcement point that binds reserved extensions to the Agents server's own identity, plus a named key vocabulary per principal kind.
- **Proposed Rule (AD-30 amendment):** "Reserved extensions are honored only when the command reaches EventStore from the Agents Server principal. The Agents DomainService declares its reserved key set (`actor:agentsPlatform`, `actor:agentsAdmin`, `actor:agentsUser`, `actor:agentsWorkflow`, and every `*:validation` verdict key) to the EventStore domain-service registration; the EventStore command ingress strips those keys from any caller that does not present the Agents Server service-principal credential committed through `EXT-HOST-1`, and the Agents server-side `SubmitCommand` pipeline additionally verifies a per-envelope integrity tag (`agents:envelopeTag` = HMAC under an `EXT-SECRETS-1` key over `TenantId`, `AggregateId`, `MessageId`, and the ordered reserved key/value pairs) before the aggregate runs; a missing or invalid tag is the AD-30 rejection and security event. Each AD-30 principal kind maps one-to-one to exactly one reserved key; an `AgentInteraction` lifecycle command dispatched from a workflow activity carries `actor:agentsWorkflow`, a User-originated proposal command carries `actor:agentsUser` with `PartyId` and resolved roles, and an aggregate rejects a command whose reserved key is not on that command's allowlist even when the key is otherwise valid. `LR-TENANT-ACCESS` includes a forged-extension test that posts every reserved key from a non-Agents principal directly to the EventStore command route and proves rejection." Note the shipped orchestrators currently dispatch every interaction command as `actor:agentsAdmin`; the mapping rule makes that a contract-test failure rather than an accepted pattern.
- **Disposition:** autofix (rule) + coordinate with `EXT-HOST-1` (route exposure, service credential) and EventStore (per-domain reserved-key registration; today it is hard-coded to one key).

### High

#### H-1 — Plaintext copies of protected content exist outside every named protection and purge scope: the pub/sub broker, projection read-model stores, and `EXT-PROVIDER-1` outcome lookup

- **Severity:** High
- **Concern:** 5, 10
- **ADs:** AD-14, AD-22, AD-27, AD-13 ("outcome lookup by `ProviderIdempotencyKey`"), AD-9; register deletion scope
- **Evidence:** `EventPublisher` unprotects each event and calls `DaprClient.PublishEventAsync(pubSub, topic, …)`; `ProjectionEventWireBuilder` unprotects for projection delivery. Projections receive plaintext and store whatever they store in the read-model store (`IReadModelStore`, a Dapr state store). AD-14 says "content-bearing Agents events and projections use EventStore payload protection" — EventStore offers no projection-side protection; "redaction rewrites projection copies only" assumes the copies are reachable. AD-13 requires `EXT-PROVIDER-1` to provide "idempotent invocation and outcome lookup by `ProviderIdempotencyKey`" so recovery can "reclaim `InvocationActive` leases through outcome lookup" — an adapter can only answer a lookup for a completed generation by having stored the generated content somewhere.
- **Compliant-but-wrong scenario:** (a) The broker is Redis Streams or a cloud topic with 7-day retention and a dead-letter topic; every `ProposalVersionRecorded` plaintext sits there after deletion reports complete. (b) `proposal-version-history` stores plaintext versions in the state store; the projection purge deletes the keys, but state-store backups taken between creation and purge, and any projection rebuild from a broker replay, revive them. (c) The Provider adapter implements outcome lookup with its own state-store cache keyed by `AttemptId` holding the full response body so that a crash between transport and `RecordGeneratedVersion` can resume idempotently — a copy of generated content under no DEK, no hold, no export manifest, no purge item.
- **Missing invariant:** the content boundary for transport, read models, and adapters.
- **Proposed Rule (AD-14 amendment):** "With C-1's `ProtectedContent` envelope, content leaves the Agents Server only as ciphertext: the broker, projection wire, read-model stores, backups of those stores, and exports carry `ProtectedContent` as sealed under the interaction DEK, never plaintext. A projection that must present content (proposal detail, version history, evidence inspection) stores the sealed envelope and decrypts at query time through the DEK under the AD-22 inspection authorization; a projection that does not present content stores only `ContentDigest`. Purge of a projection therefore removes ciphertext copies only, and a broker or state-store backup revives nothing readable. `EXT-PROVIDER-1` outcome lookup returns usage, status, and safe error class, plus content only through an Agents-supplied `ProtectedContentSink` that seals into the interaction's `ProtectedContent` before the adapter returns; the adapter keeps no content-bearing cache, and `LR-SECRETS` / `LR-AUDIT-PROTECTION-DELETION` include an adapter and broker content sweep." Add `provider-adapter-state` and the broker topic retention to the register's deletion scope as items that must prove they hold no plaintext.
- **Disposition:** autofix (AD-14) + register scope amendment.

#### H-2 — The key hierarchy has no custodian implementation, no wrapped-DEK location, no rotation path that survives immutable events, and no irreversibility across a secret-store restore

- **Severity:** High
- **Concern:** 5, 9, 10
- **ADs:** AD-22, AD-23, AD-16, AD-9; `EXT-SECRETS-1` (Uncommitted; artifact says "secret resolution, rotation, denial, and leak-evidence contract for Provider and export/deletion operations" — no key-wrapping, pinning, or destruction operations)
- **Evidence:** EventStore ships `NoOpEventPayloadProtectionService` only; `EventStoreDataProtectionOptions` is the ASP.NET Data Protection ring for the query cursor codec, one ring per application name, persisted to `statestore`. `EventStorePayloadProtectionMetadata.KeyAlias` is ≤ 256 printable ASCII and "treated sensitive-by-default", enough to carry a wrapped 256-bit DEK. `UnreadableProtectedDataReason.KeyInvalidatedOrDeleted` and `ConsistencyMismatch` comments assign "key lifecycle workflow, restored-backup governance" to EventStore Stories 22.7c/22.7d, which are not shipped. AD-23 says a restore "cannot revive erased content" because snapshots inherit the DEK, and AD-22 says the KEK is "custodied through `EXT-SECRETS-1`".
- **Compliant-but-wrong scenario:** (a) The builder wires an `IDataProtector` from `AddEventStoreDataProtection` as the payload protection service: "EventStore payload protection" literally, one deployment-wide ring, per-interaction erasure impossible — exactly S-5's fiction under a rule that now forbids it but names the very API that produces it. (b) The builder stores the KEK-wrapped DEK in `KeyAlias` of each event's metadata; KEK rotation now requires rewriting immutable events or keeping every retired KEK live forever, so "rotation" is a no-op. (c) DEK destruction deletes a secret in the platform vault; the vault's own backup/soft-delete restores it during an unrelated incident and every "erased" interaction becomes readable again with no Agents event recording it.
- **Missing invariant:** where wrapped DEKs live, who performs wrap/unwrap/pin/destroy, and what destruction means across the custodian's own backups.
- **Proposed Rule (AD-22 amendment + `EXT-SECRETS-1` scope):** "Wrapped DEKs live in an Agents-owned `InteractionKeyRegistry` keyed by (`TenantId`, `AgentInteractionId`) whose entries are produced and consumed only through `EXT-SECRETS-1` operations `WrapDek(TenantId)`, `UnwrapDek(TenantId, KeyAlias)`, `PinDek(KeyAlias, HoldId)`, `UnpinDek(KeyAlias, HoldId)`, and `DestroyDek(KeyAlias)`; events and snapshots carry `KeyAlias` only. The KEK never leaves the custodian; KEK rotation re-wraps registry entries in place and never touches events. `DestroyDek` is irreversible by contract: the custodian records a destruction tombstone that survives its own backup and restore and refuses to serve a destroyed alias, and the Agents `ProtectedDeletion` aggregate records `DekDestroyed(KeyAlias, CustodianReceipt)` as a durable fact. After any custodian restore, `EXT-HOST-1` re-applies every `DekDestroyed` receipt before Agents resumes. The ASP.NET Data Protection ring registered by `AddEventStoreDataProtection` protects query cursors only and is never used for content." Update `EXT-SECRETS-1.RequiredArtifact` to name these five operations and the destruction-tombstone guarantee.
- **Disposition:** autofix (rule) + register amendment; the custodian contract is on the `EXT-SECRETS-1` critical path.

#### H-3 — `RestrictivenessRank` is non-decreasing on `Loosens`, so the "no weaker retry" comparison is vacuous and a retry, approval-time, or pre-post check can run under a looser policy

- **Severity:** High
- **Concern:** 4 (content safety)
- **ADs:** AD-20, AD-4 (safety version pair), AD-2 (`ContentSafetyPolicy` with `RestrictivenessRank`); PRD FR-26 ("uses the more restrictive of the initial attempt's policy and the then-current active policy")
- **Evidence:** AD-20: "each published platform version carries an aggregate-assigned non-decreasing `RestrictivenessRank` and a publisher-declared `ChangeKind` (`Tightens` increments, `Loosens` and `Neutral` keep), and a retry evaluates under the current version only when its rank is at least the snapshot rank, otherwise under the snapshot version exactly." Because rank never decreases, `rank(current) >= rank(snapshot)` is always true.
- **Compliant-but-wrong scenario:** Policy v7 (rank 4) is the snapshot. Security publishes v8 as `Loosens` (rank stays 4) to permit a previously restricted category for a tenant use case. A transient transport retry, and every approval-time and pre-post re-check, evaluates "under the current version" v8 alone, exactly as AD-20 reads, and passes content that v7 blocked. The tenant restriction half of the pair has no rank at all, so a tenant administrator lowering their restriction between attempt and retry has the same effect. FR-26's "more restrictive of" is violated by a compliant build.
- **Missing invariant:** a sound realization of "more restrictive of" that does not depend on a total order.
- **Proposed Rule (replace the AD-20 sentence):** "Every re-evaluation of an attempt or version (transport retry, regeneration under the same interaction, approval-time, pre-post) evaluates under both the interaction's snapshot policy pair and the then-current pair and passes only if both pass; a decision event records both version pairs. `RestrictivenessRank` is an optimization only: when every platform version published after the snapshot carries `ChangeKind = Tightens` or `Neutral` and the tenant restriction version is unchanged, evaluating the current pair alone is permitted. `Loosens` is recorded and never lowers the rank, but its presence since the snapshot forces dual evaluation. An adapter that cannot evaluate a retired snapshot version fails closed."
- **Disposition:** autofix.

#### H-4 — Legal hold pinning is not race-free and class-scoped holds can only be resolved through a projection; DEK destruction under a hold remains possible in a compliant build

- **Severity:** High
- **Concern:** 5, 10, 7
- **ADs:** AD-22, AD-2 (hold scope grammar `interaction:<id>` list or `class:<ContentClass>` plus UTC range; "reads hold … from the owning aggregate at an expected revision, never from a projection"), AD-17 (`legal-hold`, `retention` projections)
- **Evidence:** `LegalHold` (`TenantId`, `HoldId`) and `ProtectedDeletion` (`TenantId`, `DeletionRequestId`) are separate aggregates; EventStore offers no cross-aggregate transaction. A class-plus-range hold names no interaction ids, so "which DEKs does it pin" is a set computed over interactions — necessarily from `retention`/`legal-hold` projections or a stream scan.
- **Compliant-but-wrong scenario:** (a) The deletion activity reads `LegalHold` streams for `interaction:<id>` entries at revision r, finds none, and calls `DestroyDek`; a Compliance Inspector's `ApplyHold` naming that interaction commits at r+1 between the read and the destroy. Both aggregates are consistent; the content is gone under an active hold. (b) A `class:ProposalVersions` hold with a range covering last quarter is applied; the deletion activity evaluates class holds against the `legal-hold` projection, which lags by one delivery retry; erasure proceeds. Each unit obeys AD-2 for the decision it owns.
- **Missing invariant:** the custodian, not Agents, as the last-line interlock, and explicit pinned-key enumeration for class holds.
- **Proposed Rule (AD-22 amendment):** "A hold protects keys, not queries. `ApplyHold` is two-phase: `HoldRequested` (scope, justification) is followed by a hold activity that resolves the scope to an explicit `KeyAlias` list — for `interaction:` scopes directly, for `class:` scopes from the `retention` projection at a recorded checkpoint whose range end is at or before the request's `DomainInstant` — calls `PinDek` for each through `EXT-SECRETS-1`, and only then appends `HoldApplied` listing every pinned alias and the checkpoint. `DestroyDek` is refused by the custodian while any pin exists, regardless of what Agents read; the deletion activity treats `PinnedByHold` as a typed blocked result and never retries destruction on its own. An interaction reaching terminal after the checkpoint is outside the hold by construction. Release unpins only aliases the hold pinned. `LR-AUDIT-PROTECTION-DELETION` includes a concurrent hold-versus-erasure test that proves the custodian refusal."
- **Disposition:** autofix (rule) + `EXT-SECRETS-1` operations from H-2.

#### H-5 — Rate limits, the regeneration ceiling, and the concurrent-nonterminal cap have no atomic owner, so concurrent calls pass them together

- **Severity:** High
- **Concern:** 8 (availability / abuse), 7
- **ADs:** AD-21 ("Before reservation, rate limits, the regeneration ceiling, and a maximum of concurrent nonterminal interactions per caller yield typed rejections with no reservation"), AD-13 (single owners: `BudgetLedger` owns reservation, allocator owns admission "only"), AD-24 (process-local counters forbidden for capacity, silent for rate limits), AD-2 (`TenantGovernancePolicy` holds the configured limits)
- **Evidence:** The limits live in `TenantGovernancePolicy`; the counters live nowhere named. The sequence diagram evaluates "rate limits, lifecycle, enablement, block, kill switch re-read" as one workflow step before the ledger reservation. Nothing says the check is linearizable.
- **Compliant-but-wrong scenario:** A Participant scripts 200 concurrent "Call hexa" requests with distinct client keys. Each workflow instance reads the per-Party call count from `agent-interaction-status` (a projection) or from a process-local window, sees 0, and proceeds to reserve; the ledger reserves 200 estimated attempts atomically and correctly; the per-Party limit of 10 per hour is never exceeded by any single reader's observation. The per-tenant monthly cap is consumed in one minute. Every unit obeys AD-21 literally; S-8's closure is hollow.
- **Missing invariant:** the linearizable owner of consumption counters.
- **Proposed Rule (AD-21 amendment):** "Per-Party and per-Conversation rolling-window counts, the per-proposal regeneration count, and the per-caller concurrent-nonterminal count are enforced in the same atomic append that reserves cost: the `BudgetLedger` reserve command carries `CallerPartyId`, `ConversationId`, `AgentInteractionId`, and `AttemptOrdinal`, the ledger maintains the windows and the open-interaction set for its `BudgetPeriod`, and it rejects with the typed limit reason before reserving so that a rejection leaves no reservation. Terminal handling releases the caller's open-interaction slot through the settle command. Projections and process-local memory are never a limit's source of truth. Window boundaries follow AD-28 `EvaluationInstant`."
- **Disposition:** autofix.

#### H-6 — The `Platform` principal's cross-tenant command allowlist and target scope are unstated, and the `system` tenant is not fenced off from tenant-scoped families

- **Severity:** High
- **Concern:** 2, 1
- **ADs:** AD-30 (`Platform` = "the `system`-tenant Platform Operator"), AD-2, AD-12 (kill switch pulled by the Platform Operator), AD-21 (cap override Platform-Operator-only), AD-22 (deletion), FR-33 matrix (Platform Operator has no content-inspection row; A-10, A-12)
- **Evidence:** AD-30 binds allowlists explicitly only for `Workflow`. The FR-33 matrix gives the Platform Operator platform-scoped catalog and safety-policy rows plus tenant-scoped enablement, cap setting, cap override, kill switch, and deletion-request rows, and no proposal, approval, or content-inspection row. Tenants defines `DefaultTenantId = "system"` for global administrators, so `system` is a real tenant id a client can put on a route.
- **Compliant-but-wrong scenario:** (a) The builder implements `Platform` as "global admin": any tenant, any command — the Platform Operator approves proposals, reads unposted versions, and abandons holds in tenant A without a Compliance Inspector, which FR-33 forbids and SM-4 counts. (b) Another builder implements `Platform` as `system`-tenant-only and cannot pull tenant A's kill switch or enable a Provider for it. (c) A client submits `RequestInteraction` with `TenantId = system`; `TenantId` is present as AD-2 requires, the Tenants projection confirms the caller is a `system` member, and an `AgentInteraction` stream is created in the reserved catalog tenant.
- **Missing invariant:** the `Platform` allowlist and scope, and the `system`-tenant fence.
- **Proposed Rule (AD-30 amendment):** "`Platform` may dispatch exactly the FR-33 platform rows (`ProviderCatalogMutation`, `PolicyPublication` for the platform policy) in tenant `system` and exactly the FR-33 tenant rows it names (`TenantProviderEnablement`, `TenantBudgetUpdate` including cap override, `TenantKillSwitch`, and `DeletionRequest` as requester) in a named target tenant carried in the envelope; every other family rejects `Platform`, and `Platform` never satisfies an inspection, approval, edit, hold, or export authorization. Tenant `system` is reserved: tenant-scoped families reject `TenantId = system`, no `Agent`, `AgentInteraction`, `TenantGovernancePolicy`, `TenantProviderEnablement`, `BudgetLedger`, `LegalHold`, `AuditExport`, or `ProtectedDeletion` stream may exist under it, and its membership is administered only through Tenants' global-administrator path. `LR-TENANT-ACCESS` proves each rejection."
- **Disposition:** autofix (the PRD matrix already decides the rows; the spine binds them).

### Medium

#### M-1 — Compliance inspection: the second party, the review window, and the record owner are unnamed, and the record must survive the interaction's erasure

- **Severity:** Medium
- **Concern:** 2, 6, 10
- **ADs:** AD-22, AD-2 (no aggregate owns inspections), AD-30 (`Agents.AuditOperator`); PRD FR-24 ("second-party approval or post-hoc review (both by the Tenant Agent Administrator)")
- **Compliant-but-wrong scenario:** The inspector self-selects "post-hoc review", reads the content immediately, and the review never occurs; or the second party is another Compliance Inspector; or the inspection event is appended to the `AgentInteraction` stream and disappears with its DEK, so the inspection of an erased interaction — the case AD-22 says must survive — is itself unrecorded. Each reading is compliant.
- **Proposed Rule (AD-22 amendment; add an aggregate to AD-2):** "Compliance inspections are owned by `AuditInspection` (`TenantId`, `InspectionId`), never by `AgentInteraction`, and are never under an interaction DEK. An inspection names one Conversation or case, a typed justification, and a mode: `PreApproved` (a distinct Tenant Agent Administrator principal approves before any content read) or `PostHoc` (content is readable immediately and a distinct Tenant Agent Administrator must record the review within 7 days [ASSUMPTION]; an overdue review is an `audit-evidence` blocker visible to the Tenant Agent Administrator and suspends the inspector's further `PostHoc` inspections). The approver or reviewer is never the inspector. Every content read under an inspection appends `InspectionContentRead(InspectionId, AgentInteractionId, VersionId)`. Inspection rate is exposed per tenant."
- **Disposition:** autofix (window value to discuss).

#### M-2 — The Deferred table says "single-actor deletion" while FR-33 (A-12) requires Platform Operator request with Compliance Inspector approval

- **Severity:** Medium
- **Concern:** 10, 2
- **ADs:** Deferred Beyond V1 ("V1 requires single-actor deletion with typed justification, exact scope, and the hold interlock"), AD-30, AD-22; PRD FR-33 row "Request approved deletion: Platform Operator with Compliance Inspector approval [A-12]"
- **Compliant-but-wrong scenario:** Epic 8 builds `RequestDeletion` as one command from the `Platform` principal that authorizes DEK destruction directly — compliant with the spine's deferred row, non-compliant with the PRD matrix the same spine says AD-30 carries. A second team builds the two-step. `LR-AUDIT-PROTECTION-DELETION` has no row to prefer.
- **Proposed Rule (replace the Deferred row; AD-22 sentence):** "Approved deletion is two-principal in V1 as FR-33 A-12 states: `DeletionRequested` by `Platform` and `DeletionApproved` by a Compliance Inspector of the target tenant who is a distinct principal; `DestroyDek` is authorized only by an approved request at an expected `ProtectedDeletion` revision. What remains deferred is the two-person rule on legal-hold release and on evidence export, which stay single-actor with typed justification." If Product retires A-12 differently, the spine follows the retired text.
- **Disposition:** autofix (bind A-12) — flag for Product as the assumption owner.

#### M-3 — Kill-switch semantics contradict between AD-12's two sentences, and release authority and system-versus-human abandonment are unstated

- **Severity:** Medium
- **Concern:** 8, 9
- **ADs:** AD-12, AD-5 (kill switch as a system-abandonment reason), AD-24
- **Compliant-but-wrong scenario:** AD-12 sentence one: "Every side-effecting step (… posting) re-reads … the per-tenant kill switch" and fails closed; sentence two: the switch "lets `Approved` and `PostingPending` complete or fail on their own terms". One team fails posting closed under the switch — proposals pile up in `PostingFailed` with a bounded retry that keeps failing; another posts. AD-5 lists "kill switch" as a system-abandonment reason while AD-12 says awaiting proposals may be "rejected or abandoned" (by humans); one team auto-abandons every awaiting proposal on pull (irreversible), another leaves them to expire. Nothing says who may release the switch or whether it may be released automatically.
- **Proposed Rule (AD-12 amendment):** "The kill switch blocks call acceptance, Provider invocation, proposal creation, edit, regeneration, and approval; it does not block posting of an already-approved version, which proceeds under its own gates. Pulling the switch does not abandon proposals; awaiting proposals remain rejectable or abandonable by Eligible Approvers and the Tenant Agent Administrator and otherwise expire; `KillSwitch` as an AD-5 system-abandonment reason applies only when the switch is still pulled at a proposal's `ExpiresAt`, in which case the terminal state is `Expired` with the reason recorded. Release is an audited `TenantKillSwitch` command by the Platform Operator only, with typed justification; there is no automatic release."
- **Disposition:** autofix.

#### M-4 — Unkeyed SHA-256 content digests in unprotected descriptors and the verdict cache are a content oracle for short messages

- **Severity:** Medium
- **Concern:** 5, 4, 1
- **ADs:** AD-13 (fingerprint "with per-message content digests"), AD-20 (cache keyed by "Agents content hash"; decision events carry "the content fingerprint, never content"), AD-29 (one canonicalizer, SHA-256), Consistency Convention "Provider attempt fingerprint" ("descriptors store the digest … never raw context")
- **Compliant-but-wrong scenario:** Descriptors and safety-decision events are plaintext facts (and after C-1 must be). A tenant operator with `audit-evidence` read, or anyone who obtains a backup of the verdict cache, dictionary-tests the SHA-256 of "yes", "approved", a phone number, or a salary figure against the per-message digests and recovers short message content and its presence in a Conversation — without ever touching a protected payload.
- **Proposed Rule (AD-29 amendment):** "Every digest of sensitive content that is stored outside a `ProtectedContent` envelope (per-message digests in fingerprints, verdict-cache keys, `ContentDigest`, evidence fingerprints) is a keyed digest: HMAC-SHA-256 under a per-tenant `DigestKey` custodied through `EXT-SECRETS-1` and rotated only with re-derivation of live caches; unkeyed SHA-256 is used only for identities derived from identifiers. The canonicalizer exposes `KeyedDigest(TenantId, bytes)` for this purpose."
- **Disposition:** autofix.

#### M-5 — Pre-aggregate authorization denials and the AD-30 "audited security event" have no durable owner

- **Severity:** Medium
- **Concern:** 6, 2, 8
- **ADs:** AD-12, AD-30, AD-17 (`audit-evidence` is built from events), sequence diagram ("safe blocked response; no interaction or Provider call"); PRD FR-20 ("Authorization decisions are auditable"), FR-28 kill-switch trigger "any confirmed cross-tenant or unauthorized action (SM-4)"
- **Compliant-but-wrong scenario:** Every cross-tenant attempt, role denial, forged extension, and readiness block is refused at the API before any aggregate exists, so no event, no projection entry, and no `audit-evidence` row records it; SM-4 detection and the "immediately pull the switch" trigger depend on unstructured logs that AD-14 restricts and nobody retains as evidence. FR-25's per-reason blocked-call counts miss every block that precedes `InteractionRequested`.
- **Proposed Rule (AD-30/AD-12 amendment; AD-2 addition):** "Denials and security events are durable: a tenant-scoped `SecurityEventLog` (`TenantId`, `UtcDay`) aggregate records, content-free and rate-bounded, every authorization denial, forged reserved extension, cross-tenant key access, and pre-interaction readiness block with the principal reference, family, `ReasonCode`, and trace id; ingress appends it under the `Administrator`-free `actor:agentsSecurity` key; the `audit-evidence` projection exposes counts and the Platform Operator sees cross-tenant events across tenants. A denial for tenant B attempted from tenant A is recorded under A."
- **Disposition:** autofix.

#### M-6 — Export: the envelope key's delivery path is unstated, the manifest hash lives only in a rebuildable projection, and a symmetric HMAC cannot be verified by a third party without gaining the power to forge

- **Severity:** Medium
- **Concern:** 6, 3, 10
- **ADs:** AD-22, AD-2 (`AuditExport` with `ManifestHash`), AD-17 (`export` projection); register `ExportDownload` family
- **Compliant-but-wrong scenario:** The export API response returns the download URL and the envelope key in the same JSON; the "time limit" then bounds the URL but not the key, and the response body sits in browser history and BFF logs. The manifest hash is written to the `export` projection by the handler; a projection rebuild recomputes nothing and the hash is lost. An external auditor asks to verify the manifest; verification requires the HMAC key, which is exactly the forgery key.
- **Proposed Rule (AD-22 amendment):** "The export envelope key is issued by `EXT-SECRETS-1` to the requesting Compliance Inspector principal through the custodian's own delivery (never in an Agents response, manifest, log, or projection) and expires with the export. `ManifestSealed(ManifestHash, SignatureReference, KeyVersions)` is an `AuditExport` event; the `export` projection is derived from it. The manifest is signed with an `EXT-SECRETS-1` asymmetric key whose public half is published for verification; an HMAC is acceptable only for internal integrity checks."
- **Disposition:** autofix.

#### M-7 — Generated output must cross an activity boundary between generation, output safety, and `RecordGeneratedVersion` unless the spine fixes the activity cut

- **Severity:** Medium
- **Concern:** 5, 7
- **ADs:** AD-27, AD-20, AD-13 step 6, sequence diagram (Provider → "generated content by reference"; Safety → "versioned decision by reference"; then `RecordGeneratedVersion`)
- **Compliant-but-wrong scenario:** The generation activity returns the Provider response; a separate safety activity takes it as input — plaintext in workflow history, failing AD-27 and the AD-17 sweep. Avoiding that, a builder persists the raw output as `GeneratedOutputReceived` before the safety decision so that the safety activity can resolve it by reference; that event is a "proposal version" in every practical sense, created before "a fresh decision before proposal creation", and unsafe content now has an event, a projection row, and an inspection surface.
- **Proposed Rule (AD-27 amendment):** "Generation, output safety, and the `RecordGeneratedVersion` / `GenerationFailed` dispatch are one activity. Inside it the Provider response is sealed into a `ProtectedContent` envelope immediately on receipt, the output safety decision is taken on the plaintext still in memory, and exactly one command is dispatched: `RecordGeneratedVersion` carrying the sealed envelope and the decision, or `GenerationFailed(SafetyBlocked)` carrying the decision and the envelope's `ContentDigest` only. Blocked output is never persisted as content. A crash after the Provider returned and before dispatch is resolved through `EXT-PROVIDER-1` outcome lookup under H-1's sink, never by re-invocation."
- **Disposition:** autofix.

#### M-8 — `TimerDrift` fires for every proposal under a constant scheduler/commit-clock skew, and a `TimerDrift` interaction is never terminalized

- **Severity:** Medium
- **Concern:** 9, 7
- **ADs:** AD-28 (re-arm "at most three times before recording a safe `TimerDrift` blocker"), AD-5 (`ExpiresAt` enforced on every read regardless of timer delivery), AD-22 (retention runs from the terminal instant), AD-27 (purge at terminal handling)
- **Compliant-but-wrong scenario:** The Dapr scheduler clock leads the EventStore commit clock by 800 ms. Every expiry timer fires early, re-arms for a remainder of under a second, fires early again, and after three re-arms records `TimerDrift`. The proposal is unusable (reads enforce expiry) but no `Expired` event exists, so the terminal instant never occurs, the 365-day retention never starts, the workflow instance is never purged, SM-3's cohort never closes, and the caller's concurrent-nonterminal slot (H-5) is held forever.
- **Proposed Rule (AD-28 amendment):** "A timer activity treats `EvaluatedAt >= StoredDeadline - SkewTolerance` (default 2 seconds [ASSUMPTION]) as elapsed; re-arms are for `max(remainder, 1 second)`. `TimerDrift` is a blocker on the timer path only: the same activity still appends the terminal event (`Expired`, reservation deadline, queue expiry) with `EvaluatedAt` as the evaluation instant and the commit timestamp as the `DomainInstant`, so every interaction reaches a terminal state; `TimerDrift` is surfaced in `launch-readiness` as a platform blocker for `LR-RECOVERY` freshness."
- **Disposition:** autofix.

#### M-9 — Spine-local `[ASSUMPTION]`s (AD-27 purge window, AD-30 identity resolution, AD-17 gate scope kinds, stack deviations) are outside the PRD §8.1 index and therefore outside the `UnretiredAssumption` gate

- **Severity:** Medium
- **Concern:** 10, 2
- **ADs:** AD-17 ("records an `UnretiredAssumption` blocker for every unretired … assumption in PRD section 8.1"), AD-27, AD-30; register note on `LR-UI-*` scope kinds
- **Compliant-but-wrong scenario:** `RQ-1` evaluates READY while the caller-`PartyId` resolution rule that every authorization decision depends on is still "[ASSUMPTION pending platform identity confirmation]", because the gate reads only PRD §8.1. The identity assumption is the one whose wrong answer is a cross-tenant call under another Party's name.
- **Proposed Rule (AD-17 amendment):** "Every `[ASSUMPTION]` in this spine is registered in the PRD §8.1 index with an Architecture owner in the same change that introduces it, so the `UnretiredAssumption` blocker covers it; an unregistered spine assumption fails the spine's own validation."
- **Disposition:** autofix (mechanical), plus the PRD edit.

### Low

#### L-1 — The verdict cache key omits the safety adapter version and the tenant-restriction version

- **Severity:** Low · **Concern:** 4 · **ADs:** AD-20 (key = `TenantId`, `ConversationId`, content hash, policy versions; decision events carry adapter version)
- **Compliant-but-wrong scenario:** A classifier upgrade with the same policy version reuses stale verdicts indefinitely; a tenant tightening its restriction does not invalidate cached history if "policy versions" is read as the platform version only.
- **Proposed Rule:** "The verdict cache key is (`TenantId`, `ConversationId`, keyed content digest, platform policy version, tenant restriction version, adapter version); publication of any of the three invalidates the tenant's cache."
- **Disposition:** autofix.

#### L-2 — `SampleId` has no `TenantId` component contrary to AD-2, and the EventStore `TenantId` of `platform`-scoped `LaunchReadinessGate` streams is undefined

- **Severity:** Low · **Concern:** 1 · **ADs:** AD-2, AD-29, AD-17
- **Compliant-but-wrong scenario:** Platform gate observations are stored under the evaluating tenant by one producer and under `system` by another; a tenant evaluation then sees `GateRecordMissing` for the platform gates it must read. `SampleId` collisions across tenants are prevented only by `QualificationSessionId` uniqueness, not by the rule.
- **Proposed Rule:** "`LaunchReadinessGate` streams for `TenantScope = platform` live under EventStore tenant `system` and are readable by tenant evaluation through the server only; `SampleId = H(sample, TenantScope, QualificationSessionId, ExecutionId, SampleKind)`."
- **Disposition:** autofix.

#### L-3 — The legal basis and lawful-processing role for the 365-day retention are still unrecorded (S-14 residual)

- **Severity:** Low · **Concern:** 10 · **ADs:** AD-22, Deferred Beyond V1
- **Proposed text:** Add a Deferred row: "Legal basis and controller/processor allocation for the 365-day sensitive-content retention are inherited from the platform's data-processing terms; the value is a Governance assumption registered in PRD §8.1 until Governance confirms it." Residency is now correctly deferred; this closes the other half of S-14.
- **Disposition:** discuss → defer with a row.

#### L-4 — `Agents.Approver` is a FrontComposer policy while approval rights come from the AD-8 predicate; the two can be conflated

- **Severity:** Low · **Concern:** 2 · **ADs:** AD-30, AD-8, AD-12
- **Compliant-but-wrong scenario:** The API gates `ApproveProposedAgentReply` on the `Agents.Approver` policy instead of the predicate, or the "tenant roles" Approver Policy source treats the policy as sufficient without current Conversation read access.
- **Proposed Rule:** "`Agents.Approver` gates navigation to the proposal queue only and is one admissible input to the tenant-roles source; every proposal command authorizes on the AD-8 predicate evaluated now, and a policy holder outside the predicate is denied."
- **Disposition:** autofix.

#### L-5 — Deletion or Conversation-deletion propagation for a nonterminal interaction must terminalize before purging execution state

- **Severity:** Low · **Concern:** 9, 7 · **ADs:** AD-22, AD-27, AD-18 (a non-terminal instance id is never re-created), AD-5
- **Compliant-but-wrong scenario:** A Conversation deletion signal arrives while a proposal is `Pending`; the deletion purges `workflow-execution-state` for a running instance (Dapr refuses or the purge silently no-ops), reports purged, and the instance later resumes and posts nothing but holds its admission slot.
- **Proposed Rule:** "Deletion of a nonterminal interaction first system-abandons it (`SourceConversationUnavailable` for the Conversation signal, `DeletionRequested` otherwise), waits for the AD-18 terminal handling, then destroys the DEK and purges; purge confirmation for a running instance is a typed failure, never a no-op."
- **Disposition:** autofix.

## Concern Walk

### 1. Tenant isolation

- **Covered by:** AD-2 (`system` catalog, `TenantProviderEnablement`, `TenantId` in every identity, key, cursor, allocator and ledger call, absent-key equivalence), AD-10 + register (`EntryMissing`, `PlatformNotReady`), AD-29 (tenant inside the hash input; idempotency tuple), Errors convention (identical not-found), `LR-TENANT-ACCESS`.
- **Compliant-but-wrong:** C-2 (forged reserved extension through the EventStore route), H-6 (`Platform` scope and the `system` fence), M-5 (denials unrecorded), L-2.
- **Missing/weak invariant:** enforcement point for the trust boundary; `Platform` allowlist; `system` reserved.
- **Proposed tightening:** C-2, H-6, M-5, L-2.

### 2. AuthN/AuthZ

- **Covered by:** AD-30 (four principal kinds, ingress stripping, `Workflow` allowlist, FR-33 policy mapping, caller `PartyId` resolution, role basis), AD-12 (gates before every side effect; sources per gate), AD-8 (Eligible Approver predicate at four points), AD-22 (two inspection levels), AD-15 (UI/API parity).
- **Compliant-but-wrong:** C-2, H-6, M-1 (second party, review window, record owner), M-2 (deletion two-person contradiction), M-9 (identity assumption outside the gate), L-4.
- **Missing/weak invariant:** enforcement mechanism; `Platform` bounds; inspection contract; assumption registration.
- **Proposed tightening:** C-2, H-6, M-1, M-2, M-9, L-4.

### 3. Secrets

- **Covered by:** AD-9 (opaque ULID handle, probe-derived `SecretConfigured`, resolution only inside the generation activity, memory-only), AD-14, AD-10 (`SecretUnavailable`), `LR-SECRETS`.
- **Compliant-but-wrong:** H-2 (custodian operations for keys are not in `EXT-SECRETS-1`'s artifact; cursor ring confusion), M-6 (export envelope key delivery). Provider credentials themselves are adequately bounded.
- **Missing/weak invariant:** `EXT-SECRETS-1` key-custody operations.
- **Proposed tightening:** H-2, M-6.

### 4. Content safety

- **Covered by:** AD-20 (two stages plus approval-time and pre-post, platform+tenant pair, `SafetyDecisionId` event with fingerprint, structured request, `ControlBypass` includes instruction override, cache keying and invalidation, no Approver override, fail-closed), AD-11 (untrusted context), FR-26/FR-31 binding.
- **Compliant-but-wrong:** H-3 (rank vacuous on `Loosens`), M-4 (digest oracle), M-7 (output crossing activities or persisted before the decision), L-1 (cache key).
- **Missing/weak invariant:** dual evaluation for re-checks; keyed digests; single generation activity.
- **Proposed tightening:** H-3, M-4, M-7, L-1.

### 5. Payload protection and crypto-erasure

- **Covered by:** AD-22 (per-interaction DEK under tenant KEK through `EXT-SECRETS-1`, no deployment-wide key, hold pins DEK, snapshots and replay caches inherit, restrictive completion, Conversation deletion propagation), AD-14, AD-27 (reference-only execution state, `workflow-execution-state` purge item), register deletion scope.
- **Is it implementable with the EventStore service as it exists?** Partly. `IEventPayloadProtectionService` is keyed by `AggregateIdentity`, so a per-stream DEK is expressible and snapshots do inherit it. But it protects whole payloads (C-1), unprotects onto the broker and the projection wire (H-1), ships no key-hierarchy provider — the only shipped key management is the cursor ring (H-2) — and EventStore itself assigns key lifecycle and restored-backup governance to unshipped Stories 22.7c/22.7d. Whole-stream erasure freezes the aggregate (C-1). The rule is therefore implementable only if the DEK is applied at the Agents envelope level (field-level `ProtectedContent`) rather than through the EventStore hook.
- **Custody, rotation, hold pinning, replay caches:** custody operations unnamed (H-2); rotation impossible if wrapped DEKs ride in event metadata (H-2); pinning racy and projection-dependent for class holds (H-4); replay caches inherit correctly, but broker retention, read-model stores, and the Provider adapter's outcome cache are uncovered copies (H-1); a custodian restore revives destroyed DEKs (H-2).
- **Proposed tightening:** C-1, H-1, H-2, H-4, L-5.

### 6. Audit integrity

- **Covered by:** Audit envelope convention (principal, `OnBehalfOfPartyId`, `CorrelationId` = `AgentInteractionId`, `CausationId`, `TenantId`, trace id), AD-28 (`DomainInstant` = commit timestamp), AD-22 (manifest fields, HMAC, hash in `export`), AD-17 (greatest committed revision), AD-5 (immutable versions), Observability convention.
- **Compliant-but-wrong:** C-1 (audit facts erased with content), M-5 (denials and security events have no durable owner), M-6 (manifest hash only in a projection; HMAC verification equals forgery).
- **Missing/weak invariant:** content/fact segregation; `SecurityEventLog`; `ManifestSealed` event and asymmetric signature.
- **Proposed tightening:** C-1, M-5, M-6.

### 7. Data integrity across durable owners

- **Covered by:** AD-13 (six-step protocol, single owners, recovery, fingerprint), AD-21 (`NotInvoked`, `Indeterminate` hold, `Unreconciled`, period attribution, orphan settlement), AD-23 (RPO 0, rebuildable stores never restored past truth, fence invalidation, lease reclamation), AD-24 (fence, `BeginInvocation`), AD-28 (timer re-arm), AD-29 (aggregate-assigned ordinals under EventStore concurrency).
- **Compliant-but-wrong:** H-5 (limits with no atomic owner), H-4 (hold/erasure race), H-2 (custodian restore ordering), M-7 (generation cut), M-8 (`TimerDrift` never terminalizes), L-5. `AttemptOrdinal` and `VersionOrdinal` under concurrent regenerations are sound: the loser of the optimistic-concurrency race retries with a new ordinal and the ceiling bounds it. AD-21 settlement states are complete.
- **Missing/weak invariant:** as listed.
- **Proposed tightening:** H-5, H-4, H-2, M-7, M-8, L-5.

### 8. Availability and abuse

- **Covered by:** AD-21 (rate limits, regeneration ceiling, concurrent-nonterminal cap, caps, `Unreconciled` exit), AD-24 (shared allocator, fairness, queue-or-reject), AD-12 (kill switch), AD-20 (always-blocked classes, injection).
- **Compliant-but-wrong:** H-5 (limits not linearizable → denial-of-wallet remains), M-3 (kill-switch contradictions), M-5 (attack attempts unrecorded). `Unreconciled` settlement at period close is sound; phantom `ChargedAtMaximum` consumption during a Provider incident is the PRD's accepted trade-off with the audited override.
- **Proposed tightening:** H-5, M-3, M-5.

### 9. Operational envelope

- **Covered by:** AD-16 (`EXT-HOST-1` owns backup/restore, secrets, identity wiring), AD-23 (restore ordering and exercise), AD-27 (7-day purge window), AD-28 (`TimerDrift`), Observability convention.
- **Compliant-but-wrong:** H-2 (custodian restore revives erased keys; EventStore restore is itself deferred and its stream export refuses protected payloads, so `LR-RECOVERY`'s restore exercise must be a state-store-level restore plus registry replay), M-8 (`TimerDrift` under constant skew), M-9 (7-day window outside the gate), L-5.
- **Proposed tightening:** H-2, M-8, M-9, L-5.

### 10. Compliance posture

- **Covered by:** AD-22 (365 days from the AD-28 terminal instant, hold, two inspection levels, justified governance writes, export manifest), Deferred rows (residency with `ProcessingRegion`; two-person rule), PRD §9.
- **Compliant-but-wrong:** M-2 (deferred row contradicts FR-33 A-12), M-1 (inspection second party and review), C-1/H-1 (a deletion attested complete while readable copies survive is a compliance misstatement), L-3 (legal basis).
- **Proposed tightening:** M-2, M-1, C-1, H-1, L-3.

## Prior Findings Now Closed

| Prior | Closed by (2026-09-09 spine) | Status |
| --- | --- | --- |
| S-1 `TenantId` absent; catalog scope contradictory | AD-2 (`system` catalog, `TenantProviderEnablement`, `TenantId` in every identity/key/cursor/call, absent-key equivalence), AD-10, AD-29, Identity convention, register `EntryMissing`/`PlatformNotReady` | Closed |
| S-2 Content in workflow history / Agent Framework state | AD-27 (reference-only execution state, session persistence disabled, `workflow-execution-state` purge item), AD-18, Data-planes convention | Closed |
| S-3 No principal model | AD-30 (four kinds, ingress stripping, `Workflow` allowlist, policy mapping, `PartyId` resolution) | Closed as taxonomy; residual C-2 (no enforcement point; key vocabulary) |
| S-4 Unbounded reservations | AD-21 (`NotInvoked`, `Indeterminate` hold, `Unreconciled`, period attribution, orphans, operator settlement) | Closed |
| S-5 Key ownership / erasure granularity | AD-22 (DEK per interaction under tenant KEK, hold pins, snapshots inherit, restrictive completion definition) | Closed in text; residual C-1, H-1, H-2, H-4 (implementability against EventStore reality) |
| S-6 Audit envelope, clock authority, manifest | Audit envelope convention, AD-28, AD-22 manifest fields and HMAC | Closed; residual M-6 (hash location, verification key) |
| S-7 Disable/emergency stop does not fail in-flight work closed | AD-4 (lifecycle, enablement, block, kill switch never frozen), AD-12 (`TenantKillSwitch` family, semantics), AD-24 (queued admissions cancelled) | Closed; residual M-3 (contradictory posting semantics, release authority) |
| S-8 Intra-tenant denial-of-wallet | AD-21 (per-Party/per-Conversation limits, regeneration ceiling, concurrent-nonterminal cap before reservation) | Closed in text; residual H-5 (no atomic owner) |
| S-9 Prompt injection / structured request | AD-20 (role/message structure, `ControlBypass` includes instruction override), AD-11 (untrusted context) | Closed |
| S-10 Secret reference shape and rotation | AD-9 (opaque ULID handle, probe-derived `SecretConfigured`, in-activity resolution) | Closed |
| S-11 Safety decision shape and restrictiveness order | AD-20 (`SafetyDecisionId` event contract, `RestrictivenessRank`, `ChangeKind`) | Closed in text; residual H-3 (rank vacuous on `Loosens`) |
| S-12 Idempotency keys not tenant/principal bound | AD-29 (tuple `TenantId`, principal, family, client key) | Closed |
| S-13 Backup/restore ordering | AD-23 (EventStore only backed-up truth, rebuildable stores never restored later, fences, leases, reservations, restore exercise) | Closed; residual H-2 (secret-custodian restore revives destroyed DEKs) |
| S-14 Residency and retention basis | Deferred row (residency, `ProcessingRegion` in AD-10) | Closed for residency; residual L-3 (legal basis) |
| S-15 Observability correlation | Observability convention (trace id, `TenantId`, `AgentInteractionId`, `AttemptId` only) | Closed |

## Non-Findings Worth Recording

- AD-13 plus AD-24 remain sufficient against duplicate Provider calls and duplicate posts; the deterministic `MessageId` and Conversations idempotency close posting compensation.
- AD-29's hash-based identities with `TenantId` inside the input leak nothing through identifiers; the residual leak is through content digests (M-4), not ids.
- AD-21's settlement state machine (`NotInvoked`, released, settled to actuals, `ChargedAtMaximum`, `Unreconciled`, period close, orphan settlement, no period migration) is complete; no tightening proposed.
- The register's `PlatformNotReady` substitution list is correct: `Disabled`, `Stale`, and `CapabilityVersionRegressed` are legitimately tenant-visible because they disclose no platform configuration detail.
- AD-17's `SystemTimer` family (`LR-EVENTSTORE` only) is the right authority for expiry and reservation deadlines; M-8 concerns what the timer does, not who authorizes it.

## Disposition Summary

| Id | Severity | Concern | ADs | Disposition |
| --- | --- | --- | --- | --- |
| C-1 | Critical | 5, 6, 10 | AD-14, AD-22, AD-27, AD-2, AD-17 | autofix (content/fact segregation, `Erased` state, post-erasure rehydration test) |
| C-2 | Critical | 2, 1 | AD-30, AD-12, AD-16 | autofix (reserved-key registration, envelope tag, key-per-principal) + `EXT-HOST-1`/EventStore coordination |
| H-1 | High | 5, 10 | AD-14, AD-22, AD-13, AD-9 | autofix + register scope (`provider-adapter-state`, broker) |
| H-2 | High | 5, 9, 10 | AD-22, AD-23, AD-16 | autofix + `EXT-SECRETS-1` artifact amendment |
| H-3 | High | 4 | AD-20, AD-4 | autofix (dual evaluation; rank as optimization only) |
| H-4 | High | 5, 10, 7 | AD-22, AD-2 | autofix (two-phase hold, custodian pin interlock) |
| H-5 | High | 8, 7 | AD-21, AD-13, AD-24 | autofix (ledger enforces windows atomically) |
| H-6 | High | 2, 1 | AD-30, AD-2, AD-12, AD-21 | autofix (bind FR-33 platform rows; reserve `system`) |
| M-1 | Medium | 2, 6, 10 | AD-22, AD-2, AD-30 | autofix (`AuditInspection` aggregate; review window to discuss) |
| M-2 | Medium | 10, 2 | Deferred table, AD-22, AD-30 | autofix (bind A-12) — Product as owner |
| M-3 | Medium | 8, 9 | AD-12, AD-5 | autofix |
| M-4 | Medium | 5, 4, 1 | AD-29, AD-13, AD-20 | autofix (keyed digests) |
| M-5 | Medium | 6, 2, 8 | AD-30, AD-12, AD-2 | autofix (`SecurityEventLog`) |
| M-6 | Medium | 6, 3, 10 | AD-22, AD-2 | autofix |
| M-7 | Medium | 5, 7 | AD-27, AD-20, AD-13 | autofix (single generation activity) |
| M-8 | Medium | 9, 7 | AD-28, AD-5, AD-22 | autofix (skew tolerance; terminalize under `TimerDrift`) |
| M-9 | Medium | 10, 2 | AD-17, AD-27, AD-30 | autofix + PRD §8.1 rows |
| L-1 | Low | 4 | AD-20 | autofix |
| L-2 | Low | 1 | AD-2, AD-29, AD-17 | autofix |
| L-3 | Low | 10 | AD-22, Deferred table | discuss → defer with a row |
| L-4 | Low | 2 | AD-30, AD-8 | autofix |
| L-5 | Low | 9, 7 | AD-22, AD-27, AD-18 | autofix |
