# Review 2026-09-09 - Reconcile Architecture Spine Against UX Spines

- **Subject:** `ARCHITECTURE-SPINE.md` (updated 2026-09-09) against `ux-designs/ux-agents-2026-06-23/EXPERIENCE.md`, `DESIGN.md` (both updated 2026-09-09), and `reconcile-validation-2026-09-09.md` section 4.
- **Method:** full read of the spine and EXPERIENCE.md; DESIGN.md read for components, colors, and Do/Don't; cross-checked against `launch-readiness-register.md` (operation-family matrix, projection ids), `external-dependency-register.md` (`EXT-CONV-UI-1`), PRD FR-18/FR-27/FR-29/FR-32/FR-33, and `src/Hexalith.Agents.UI/Composition/AgentsFrontComposerRegistration.cs`.
- **No document was modified.**

## Verdict

**Mostly aligned; not yet bindable as-is.** The 2026-09-09 re-distillation landed the items the UX reconciliation handed to Architecture: the `Agents.PlatformOperator` rename is consistent across AD-30 and the three EXPERIENCE.md occurrences; the Conversations seam ownership split, `GetCallabilityAsync`, the per-message decoration slot, the self-contained `ConversationAgentCallPanel`, the ten-state proposal machine with frozen expiry on approval, the nine lock-bearing families, the nearing-expiry rule (10 percent / 1 hour / 15-minute floor), and the AD-15 contract-growth list all match the UX text.

What remains is one semantic contradiction that changes runtime behavior (safety re-check points: the spine binds three, the UX and PRD bind four, and the `SafetyDecisionId` derivation cannot key the fourth), four ownership or authority contradictions (Disable semantics, Eligible Approver and the caller, expiry/ceiling owning aggregate, `Caller` policy source), three naming contradictions (`CapabilityVersion` as concurrency token, `ConfigurationReferenceId` vs `SecretReference`, `agent-setup-readiness` vs `agent-setup`), and fourteen UX-stated contracts or rules the spine should absorb, mostly one-sentence additions to AD-5, AD-15, AD-21, AD-22, and AD-28.

## Not landed

Each row: UX reference, what the spine lacks, where it should land, proposed text.

### NL-1 - Approval-time safety check (fourth application point)

- **UX:** `proposal-editor` row ("Approval re-runs the then-current Content Safety Policy on the exact selected version"); display-only outcome `safety blocked at approval`; `audit-evidence-panel` ("safety decisions at each of the four points"); `content-safety-policy-editor` copy. PRD FR-27 line 574 names approval-time as "the third of the four application points".
- **Missing:** AD-20 binds three stages (pre-Provider, pre-proposal, pre-post). There is no approval-time decision, no rejection outcome for it, and `SafetyDecisionId = H(safety, AttemptId, Stage)` (AD-29) cannot identify a decision on an edited version, which has no `AttemptId`.
- **Absorb in:** AD-20 rule, AD-29 identity list.
- **Proposed:** "Approval passes the exact selected version through a fresh decision before `Approved` is appended; a failing decision rejects the approval, leaves the proposal non-terminal, marks that version, and is recorded as a `SafetyDecision` keyed `H(safety-version, ProposalVersionId, Stage)` so edited versions are identifiable without an attempt."

### NL-2 - Truth-flow stage vocabulary and the acceptance contracts

- **UX:** § State Patterns (`Submitted` is local only, "no server acceptance claimed"; `AuthoritativePending` exists only after the UI renders the accepted identity plus projection/version reference); § Projection catch-up contract names `AgentCommandAcceptance` and `ProviderCatalogCommandAcceptance` as the truth-flow helper; PRD FR-29 defines the three stages.
- **Missing:** AD-15 and the Trusted-verdicts convention say "Accepted writes return `Submitted` identities", colliding with the UX meaning of `Submitted` (pre-acceptance, client-local). The spine never names FR-29 or the acceptance contracts.
- **Absorb in:** AD-15 rule, Trusted verdicts convention.
- **Proposed:** "Accepted writes return the FR-29 `AuthoritativePending` identity and projection version (HTTP 202) through the `*CommandAcceptance` contracts; `Submitted` is a client-local stage that claims no server acceptance, and callability is never returned by a write."

### NL-3 - Projection catch-up cadence and nudge transport

- **UX:** § Projection catch-up contract (poll every 250 ms for up to 8 s on every write surface; `IProjectionChangeDetailNotifier.ProjectionChangedDetail` filtered to authoritative projection ids triggers an immediate poll and never replaces polling; "pinning the transport in the Architecture Spine is a deferred item"). Reconcile § 4 assigns this to Architecture.
- **Missing:** AD-15 names the notifier as a Story 5.5/5.7 deliverable only. No cadence, no "nudge never replaces polling", no transport (FrontComposer's projection-change channel vs an Agents hub), no exhaustion rule.
- **Absorb in:** AD-15 rule or a new "Projection catch-up" convention row.
- **Proposed:** "The UI proves `ProjectionConfirmed` only by polling the authoritative projection (250 ms interval, 8 s budget) after an accepted write; `IProjectionChangeDetailNotifier` over the FrontComposer projection-change channel is the only nudge, is filtered to the register's projection ids, triggers an immediate poll, never replaces it, and Agents owns no hub; exhaustion keeps the pending state and is never Success or `Stale`."

### NL-4 - `RegistryRevision` as the readiness checkpoint identity

- **UX:** `agent-readiness-badge`, `AgentActivation` confirmation contents, `launch-readiness-panel`, Agents overview ("at one `RegistryRevision`"). The register's matrix section already says "the readiness decision exposes the matrix version, evaluated `RegistryRevision`, applicable GateIds, and safe blockers".
- **Missing:** The spine says "registry checkpoint" and never names `RegistryRevision`, so the UI's carried identity has no spine-level name.
- **Absorb in:** AD-17 rule.
- **Proposed:** "Every readiness evaluation is stamped with the `RegistryRevision` of the checkpoint it read, carried unchanged to API, BFF, UI, and the `AgentActivation` audit record, so two surfaces showing the same revision show the same outcome."

### NL-5 - Proposal read contracts for expiry and segregation of duties

- **UX:** § Known gaps table rows `ProposalDetailView` (`RegenerationCount`, `RegenerationCeiling`, `CanCurrentUserApproveSelectedVersion` with reason; owners 7.3, 7.4), `PendingProposalView`/`ProposalDetailView` (configured expiry window plus server read time; owners 7.1, 7.2), `ApproverPolicySource.DisclosureCategory` (owner 5.4); `proposal-queue-grid` `needs my action` requires per-viewer eligibility on `PendingProposalsResult`.
- **Missing:** AD-15's growth list stops at 5.5/5.7 items. AD-28 says the UI "renders the server's `EvaluatedAt`-relative result" but no proposal read is bound to carry `EvaluatedAt`, the window, or a per-viewer approve verdict, so the UX must otherwise compare `EditorPartyId` client-side (which it forbids).
- **Absorb in:** AD-15 rule; AD-28 rule (last sentence).
- **Proposed:** "Proposal reads carry `ExpiresAt`, the configured expiry window, `EvaluatedAt`, `RegenerationCount`, `RegenerationCeiling`, and a server-evaluated `CanCurrentUserApproveSelectedVersion` with a safe reason per version, so the UI computes nearing-expiry and disables Approve without a browser clock or a Party comparison; per-source `DisclosureCategory` is a contract field, not a policy-level one."

### NL-6 - Abandon availability in `Approved` and `PostingPending`; who exits an exhausted `PostingFailed`

- **UX:** § Proposal editor action rail (`Approved`/`PostingPending`: "abandon where permitted"; `PostingFailed` exhausted: "`Start a new Agent Call` only"). PRD FR-18 line 396 lists Approver abandon, Administrator abandon, administrative retry, or system abandonment.
- **Missing:** AD-5 does not say whether abandon is a legal transition from `Approved` or `PostingPending`, and AD-12 lets those states "complete or fail on their own terms" under the kill switch, which reads as not abandonable. The UX rail therefore has no transition table to bind to, and the UX has no surface for the Administrator's administrative retry/abandon that AD-5 names.
- **Absorb in:** AD-5 rule.
- **Proposed:** "Abandon is legal from every awaiting-decision state and from `PostingFailed`, never from `Approved` or `PostingPending`, which complete or fail on their own terms; after the retry bound the Tenant Agent Administrator's audited administrative retry and abandon are `ProposalResolution` commands requiring no Conversation read access, exposed on the operational-status surface."

### NL-7 - Automatic-path posting retry

- **UX:** § Agent call `posting failed` row ("the same bounded audited retry rule as the proposal path, reusing the deterministic `MessageId`; exhaustion leaves a new call as the sole recovery"); UJ-2 failure paths.
- **Missing:** AD-5's bound (3 attempts / 15 minutes, A-7) is stated for proposals only. The automatic path has no proposal and no Approver, so who drives the retry, whether it is system-driven inside the workflow, and what the exhausted terminal status is are unbound.
- **Absorb in:** AD-5 or AD-18 rule.
- **Proposed:** "Automatic-mode posting shares the A-7 bound: the workflow retries transient posting failures with the same `MessageId` and idempotency key, then records terminal `PostingFailed` on `AgentCallOperationStatus` with a typed reason; no human retry exists on the automatic path and the sole recovery is a new Agent Call."

### NL-8 - Currency mismatch as a tenant-scoped readiness blocker

- **UX:** `provider-catalog-grid` and `cost-control-editor` rows (tenant budget currency authoritative; per-tenant mismatch renders `{colors.status-important}` on the tenant readiness view and blocks reservations there); § Provider and model `Blocked` row lists "currency mismatch".
- **Missing:** AD-21 says the tenant currency is authoritative and the catalog validates ISO 4217 only, but neither AD-10's blocked list nor AD-21 says a mismatch blocks, or at which layer (readiness join vs reservation).
- **Absorb in:** AD-10 rule (blocked list) and AD-21.
- **Proposed:** "A platform entry whose pricing currency differs from the tenant budget currency is `Blocked` with the tenant-scoped code `CurrencyMismatch` in the AD-10 readiness join and refuses reservation under AD-21; the shared catalog row is never marked by one tenant's mismatch."

### NL-9 - Basis of the 80 / 100 percent budget states and the override bound

- **UX:** `cost-control-editor` ("evaluated on settled usage plus outstanding reservations, and both figures are labeled"; override "carries a numeric ceiling and an expiry"; changing the budget currency after settled spend "states that settled spend is not converted").
- **Missing:** AD-21 gives the thresholds but not the basis; "bounded" for the override is unquantified; currency change after settlement is unaddressed.
- **Absorb in:** AD-21 rule.
- **Proposed:** "Budget percentage is settled usage plus outstanding and `Unreconciled` reservations over the cap; a cap override carries a numeric ceiling and an expiry and lapses at the earlier of the two; a budget-currency change is a `TenantBudgetUpdate` that never converts settled spend."

### NL-10 - Configuration and governance change evidence class

- **UX:** `audit-evidence-panel` second evidence class (actor, operation family, resource identity, old-to-new values where safe, published version, concurrency token, projection id and version, justification, acceptance stages; override scope, ceiling, expiry); § Confirmation contents by family ("the rendered set is what the audit record carries"); § High-risk pending commands.
- **Missing:** AD-22 governs sensitive content, export, and deletion; the Audit envelope convention governs headers. No decision binds an FR-24/FR-32 evidence record for configuration and governance writes, nor that the confirmation-rendered set is what is recorded.
- **Absorb in:** AD-22 rule, or a new "Change evidence" convention row.
- **Proposed:** "Every lock-bearing-family command appends a change-evidence event carrying actor and role basis, operation family, resource identity, old-to-new values at the FR-20 disclosure level, published or pricing version, expected revision, and the typed justification where FR-30 requires it, and the set rendered in the confirmation is exactly the set recorded."

### NL-11 - Which families require a typed justification

- **UX:** `audit-governance-panel` ("each of these plus `PolicyPublication` and `TenantBudgetUpdate` requires a justification ... an empty or whitespace value is rejected client-side and server-side").
- **Missing:** AD-22 says "Governance writes carry a typed justification" without naming the families; the UI convention repeats it.
- **Absorb in:** AD-22 rule.
- **Proposed:** "`LegalHold`, `LegalHoldRelease`, `ExportRequest`, `DeletionRequest`, `PolicyPublication`, and `TenantBudgetUpdate` reject a missing or whitespace justification before append."

### NL-12 - `LegalHoldRelease` family and the nine UI families

- **UX:** § Confirmation contents by family treats release as `LegalHold` ("for a release the statement of what becomes deletable"); the register matrix v2 has a distinct `LegalHoldRelease` family; AD-12's lock-bearing subset lists `LegalHold` only.
- **Missing:** Whether `LegalHoldRelease` (and `ExportDownload`, `TenantProviderEnablement`, `TenantKillSwitch`) is lock-bearing, and whether the UX nine-family roster is the lock-bearing subset or a UI grouping over more register families.
- **Absorb in:** AD-12 rule.
- **Proposed:** "The UX confirmation families are the lock-bearing subset; register families `LegalHoldRelease`, `ExportDownload`, `TenantProviderEnablement`, and `TenantKillSwitch` are lock-bearing under the UI family that renders them (`LegalHold`, `ExportRequest`, `ProviderCatalogMutation`, and a stop control on `agent-setup`), and `AgentCallAcceptance`, `ProposalEdit`, and `ProposalRegeneration` are not lock-bearing; their duplicate suppression is the AD-29 client idempotency key."

### NL-13 - Session definition for the advisory lock and the `pending in another session` variant

- **UX:** § High-risk pending commands ("Session: authenticated user plus browser tab or circuit"; second tab renders `pending in another session` from the accepted-by reference).
- **Missing:** AD-12 says "UI/BFF user session" without defining it; AD-15 lists the accepted-by session reference but not what it identifies.
- **Absorb in:** AD-12 rule.
- **Proposed:** "A session is the authenticated principal plus one browser circuit; the accepted-by reference on pending status is that session's opaque id so another circuit renders `pending in another session` without local lock state."

### NL-14 - `hexa` provisioning and the Agent Party identity

- **UX:** `agent-config-form` ("`hexa` is pre-provisioned once per tenant, so there is no not-yet-created state"; "Agents issues no command to create or link [the Party identity], and an unresolved identity is a platform provisioning task"; `HasPartyIdentity`/`MissingPartyIdentity` blocker).
- **Missing:** AD-2 says `hexa` is one `Agent` per tenant but not who creates the aggregate or when; AD-7 says "creation/linking validates or provisions identity through Parties adapters", which the UX reads as an Agents-issued command it says does not exist.
- **Absorb in:** AD-7 rule (and AD-2 one clause).
- **Proposed:** "The `hexa` `Agent` stream and its Party identity are provisioned once per tenant by the platform host at tenant enablement under the `Platform` principal; Agents exposes no create-or-link command, validates the stored `PartyId` through the Parties adapter on every activation and post, and renders `MissingPartyIdentity` as a blocker owned by platform provisioning."

### NL-15 - Interim harness constraints and the Conversation status entry seam

- **UX:** § Information Architecture rules (harness "writes nothing in a production-like `EnvironmentProfile`", excluded from `LR-UI-CONFORMANCE` while it exists); Notifications convention and UJ-3 step 2 name a "Conversation status entry" as a discovery channel.
- **Missing:** AD-31 binds removal before Story 6.7 closes but not the interim write prohibition; the Conversation status entry has no seam (`EXT-CONV-UI-1` carries an action contribution and a decoration slot only).
- **Absorb in:** AD-31 rule; Notifications convention.
- **Proposed:** "Until removed, the harness is denied in any production-like `EnvironmentProfile` by the `AgentCallAcceptance` gate set; the Conversation status entry is rendered through the `EXT-CONV-UI-1` decoration slot from the same provenance accessor, and no third seam is introduced for it."

### NL-16 - Minor contract fields

- `RateLimited` should carry scope, window, and `ResetAt` (UX voice row and `rate limited` state) - add to AD-21 typed rejections.
- `Posted` on `AgentCallOperationStatus` should carry the posted `MessageId` (UX Known gaps row) - add to AD-15's growth sentence.
- `CapacityQueued` "queue position where safe" - AD-24 returns `QueueId` only; either bind a safe position or the UX drops it.
- The approver-policy surface's "count of pending proposals under each prior policy version, linking to the queue filtered by that version" needs a query by `ApproverPolicyVersion` - add to AD-15.
- Every public read carries projection id, version, and `Freshness` (UX § State Patterns) - the Projections convention states only readiness/evidence/deletion name ids.

## Contradictions

### C-1 - Disable semantics [behavioral]

- **UX:** `agent-config-form` Disable confirmation and `AgentActivation` confirmation contents: "in-flight calls complete and new calls are rejected before Provider invocation, and pending proposals stay resolvable".
- **Spine:** AD-4 "Prevents: ... pending interactions surviving a disable or stop"; "Agent lifecycle state ... never frozen: every side-effecting step re-reads them under AD-12 and fails closed". Approval and posting are side-effecting steps (AD-12 list), so a disabled Agent cannot have proposals approved or posted.
- **Resolution needed:** either the spine adopts kill-switch-like semantics for Disable (awaiting proposals may be rejected or abandoned, never approved; `Approved`/`PostingPending` complete on their own terms) and says so in AD-4/AD-12, or the UX confirmation text changes. The spine's own text is internally consistent; the UX copy is wrong against it.

### C-2 - Eligible Approver and the caller [behavioral]

- **UX:** `proposal-editor`: approval blocked when the Approver "last edited the selected version or is the sole Approver of their own call", which lets a caller approve their own call when other Approvers exist.
- **Spine:** AD-8 predicate: "resolved now, not the caller, not the last editor of the version" - the caller is excluded unconditionally.
- **Resolution needed:** UX to drop "sole Approver of their own call" and use "the caller of this Agent Call"; the spine is right per FR-13/FR-15.

### C-3 - Owning aggregate of proposal expiry duration and regeneration ceiling [ownership]

- **UX:** both are `hexa` configuration fields on `/agents/configuration`, written as `AgentSetupMutation`; the posting retry maximum is also "a `hexa` configuration field". PRD FR-16/FR-32/FR-33 say "configurable per Agent".
- **Spine:** AD-2 places "regeneration ceiling, expiry defaults" in `TenantGovernancePolicy`; the `Agent` class diagram has no such fields; AD-4's snapshot does not pin either value although both are "future proposals only".
- **Resolution needed:** move both to the `Agent` aggregate (bumping `ConfigurationVersion` and pinned in the AD-4 snapshot) or state in AD-2 that they are `TenantGovernancePolicy` values written through `AgentSetupMutation` and snapshotted; the UX and PRD agree with each other, not with the spine.

### C-4 - Posting retry maximum [ownership]

- **UX:** "the maximum number of attempts is a `hexa` configuration field"; confirmation shows "attempts used of the configured maximum".
- **Spine:** AD-5 fixes "at most 3 attempts over 15 minutes [ASSUMPTION A-7]"; PRD A-7 assigns the bound to Architecture.
- **Resolution needed:** UX to render the A-7 bound, not a configuration field; or the spine makes the bound configurable within a range and adds it to the snapshot. The spine's position matches the PRD.

### C-5 - `Caller` as an Approver Policy source [contract]

- **UX:** `approver-policy-builder` builds authority from `ConversationOwner`, `Caller`, `PredefinedParty`, `TenantRole`.
- **Spine:** AD-8 "the caller source is retired on the FR-23 deprecate-and-reject register"; a policy containing it is rejected.
- **Resolution needed:** UX to render `Caller` as a rejected legacy value (never selectable, validation error on load) so the enum stays wire-stable per FR-23.

### C-6 - Concurrency token for catalog mutations [contract]

- **UX:** `provider-catalog-grid`: "`CapabilityVersion` is the concurrency token; outdated or regressed submissions render `superseded by another decision`", for pricing edits, enable, and disable.
- **Spine:** AD-10: "Enable/disable never bumps `CapabilityVersion`"; pricing has its own effective version; EventStore optimistic concurrency at the expected revision is authoritative (AD-13).
- **Resolution needed:** the token must be the catalog entry's expected stream revision (or an `EntryRevision` on `ProviderCatalogEntryView`); `CapabilityVersion` cannot detect two concurrent enable/disable or pricing writes. Spine should name the token in AD-10 or the Errors convention.

### C-7 - Secret reference: name and who issues it [contract]

- **UX:** `ConfigurationReferenceId`, "a reference only (`EXT-SECRETS-1`), operator-only visible, masked with autocomplete off when editable" - an operator-typed value.
- **Spine:** `SecretReference`, "an opaque Agents-issued handle (ULID) bound to a secret only inside `EXT-SECRETS-1`; rotation keeps the handle".
- **Resolution needed:** one name (`SecretReference`) and one issuance model. If Agents issues the ULID, the UI has no editable reference field, only "bind secret" and "rotate" commands; if the operator supplies a vault reference, AD-9's "Agents-issued" is wrong.

### C-8 - Visibility of Provider configured state [authorization]

- **UX:** `provider-catalog-grid` shows "configured state" to `Agents.Administrator` readers; the six-part eligibility set shows "configured" as an activation blocker to Nora.
- **Spine:** AD-9 lists "configured state" among public exposures and then says "secret state is visible to the Platform Operator only"; PRD FR-33 A-10 says "secret references and configured state are visible only to the Platform Operator".
- **Resolution needed:** AD-9 should say the tenant sees only the safe readiness `ReasonCode` (`Unconfigured`) through the AD-10 join, never the `SecretConfigured` fact or the reference; the UX column then becomes a readiness reason, not a configured-state column.

### C-9 - Safety policy publication surface and role [authorization]

- **UX:** `/agents/content-safety` gated `Agents.Administrator` "validates and publishes a versioned safety policy" (`PolicyPublication`); UX-J5 has Priya (Release Operator) publishing a Content Safety Policy version.
- **Spine:** `ContentSafetyPolicy` is a `system`-tenant aggregate with `RestrictivenessRank` and `ChangeKind`, published by the Platform principal (AD-2, AD-20, AD-30); tenant restrictions live in `TenantGovernancePolicy`. PRD FR-33: Platform Operator with Security approval.
- **Resolution needed:** UX splits the surface: tenant restrictions (Administrator, `TenantGovernancePolicy`) and platform publication (`Agents.PlatformOperator`, with `ChangeKind` and rank rendered). The spine is consistent with the PRD.

### C-10 - Who sets cost caps and rate limits [authorization]

- **UX:** `/agents/cost-controls` gated `Agents.Administrator`; UJ-1 step 8 has Nora (Tenant Agent Administrator) configuring unconfigured caps and rate limits.
- **Spine:** AD-21 "the Tenant Agent Administrator may lower but never raise them"; PRD FR-33: Platform Operator or Release Operator sets them. Setting an unconfigured cap is not a lowering.
- **Resolution needed:** UX to gate initial configuration and raises by `Agents.Operator`/`Agents.PlatformOperator` and lowering by `Agents.Administrator`, or the spine relaxes AD-21 for initial configuration. Spine matches PRD.

### C-11 - Removal of `hexa` from a Conversation: trigger and resulting state [behavioral]

- **UX:** "Removal of `hexa` is a Conversation-owned action. Agents reacts by blocking further calls ... and moving every non-terminal proposal to `Abandoned` with reason `Agent removed from Conversation`."
- **Spine:** AD-7 detects absence lazily at the next accepted call's membership step (then sets the block, abandons, rejects `RemovedInConversations`); before every post, membership is re-validated and failure records `PostingFailed`, not `Abandoned`. `EXT-CONV-AI-1` carries no removal notification.
- **Resolution needed:** UX should state lazy detection and that a pending proposal whose Agent was removed reaches `PostingFailed` at post time or `Abandoned` at the next call, whichever comes first; or the spine adds a removal signal to `EXT-CONV-AI-1` (a register change). The UX abandonment reason list (Approver, policy, removed) also omits the spine's `SourceConversationUnavailable` and kill-switch reasons.

### C-12 - `Expired` rendering after approval [behavioral, minor]

- **UX:** `Expired` "rendered whenever `ExpiresAt` has passed on any read, regardless of timer delivery".
- **Spine:** AD-5 "`ExpiresAt` applies only while awaiting a decision" and approval "freezes expiry".
- **Resolution needed:** UX to qualify the rule to `Pending`/`Edited`/`Regenerated`; nearing-expiry likewise. Spine is right.

### C-13 - Tokenizer approximation [contract, minor]

- **UX:** `conversation-context-policy-panel` shows "tokenizer or named approximation".
- **Spine:** AD-11 requires the exact `EXT-TOKEN-1` tokenizer; a missing or unsupported tokenizer blocks. No approximation exists.
- **Resolution needed:** UX drops "named approximation".

### C-14 - Projection id naming [naming]

- **UX:** `agent-readiness-badge` reads the `agent-setup-readiness` projection.
- **Spine and register:** the shipped, ratified id is `agent-setup` (AD-17; register projection inventory).
- **Resolution needed:** UX to use `agent-setup`.

### C-15 - Story ownership of contract growth [ownership, minor]

- **UX Known gaps:** `AgentCallOperationStatus` growth owned by 6.3/6.4/6.5/6.7; `ProposalDetailView` by 7.3/7.4; proposal views by 7.1/7.2; `ApproverPolicySource` by 5.4.
- **Spine AD-15:** the whole list "owned by Stories 5.5 and 5.7".
- **Resolution needed:** AD-15 to defer story ownership to the UX Known-gaps table or list the owners per contract.

### C-16 - Call hexa duplicate suppression [minor]

- **UX:** `conversation-agent-call`: "Duplicate submission is blocked per session, resource, and family", and the button is `DisabledFocusable` while pending; connection-lost rule groups Call hexa with the nine high-risk submits.
- **Spine:** `AgentCallAcceptance` is not in the AD-12 lock-bearing subset; duplicates are suppressed by `ClientIdempotencyKey` in `AgentInteractionId` (AD-29).
- **Resolution needed:** see NL-12; UX wording should say idempotency key, not family lock.

### Residual naming observations (no spine change)

- `Agents.PlatformOperator` is consistent between AD-30 and EXPERIENCE.md (lines 63, 80, 179); DESIGN.md says only "platform-scoped mutation policy". The superseded name `Agents.PlatformProviderAdministrator` survives in `reconcile-validation-2026-09-09.md` § 1 and § 2 and in `.memlog.md` line 48 as historical record; the spine lists that reconcile file as a source, so a reader following the source could pick up the old name. EXPERIENCE.md § FrontComposer Readiness still says "Four policy constants" (there are now five) and the shipped registration declares four (no `Agents.PlatformOperator`), which the UX Shipped-code corrections table does not list.
- `EXT-CONV-UI-1` is "two artifact kinds" plus a gateway method in EXPERIENCE.md and "three artifact kinds" in the register; AD-31 lists all three without counting. Harmless, but the UX and register should agree.
- UX says the `ProposedAgentReplyState` enum "carries ten" values; AD-5 has ten states plus the `Unknown = 0` sentinel. Same enum, different counting.
- Posting retry attempts are "rows in version history" in the UX; under AD-29 they are outcome events, not `ProposalVersion`s. The UX should call them attempt rows, not versions.

### UX surfaces missing for spine-bound operations (for the UX owner, not the spine)

- `TenantKillSwitch` (AD-12): no surface, no state on `agent-readiness-badge`, no "stopped" copy, although AD-12 says `agent-setup` and `launch-readiness` expose the stop state.
- `TenantProviderEnablement` (AD-2, register v2 family): no Platform Operator surface to enable a Provider/model for a tenant.
- Administrative retry/abandon of an exhausted `PostingFailed` proposal by the Tenant Agent Administrator (AD-5, PRD FR-33): no surface.
- `DeletionRequest` requester: UX places it on the `Agents.AuditOperator` route; PRD FR-33 A-12 says Platform Operator with Compliance Inspector approval; the spine does not bind the requester role (should be a clause in AD-22 or AD-30).

## Landed correctly

- **Policy vocabulary:** AD-30 maps the five FrontComposer policies one-to-one to FR-33 roles; `Agents.PlatformOperator` is used identically in EXPERIENCE.md's IA table, rules, and `provider-catalog-grid` row; the read/mutation split on the Provider catalog matches AD-9/AD-30.
- **Conversations seam (AD-31):** Conversations owns the trigger; Agents owns `ConversationAgentCallPanel` with its own live regions and post-submit focus; action contribution with typed registration failure, `MessageId`-keyed decoration slot served by an Agents provenance accessor, `GetCallabilityAsync(tenant, conversation)`; harness removed before Story 6.7 closes; `Uncommitted` blocks the story. Matches EXPERIENCE.md § Conversation Integration Seam exactly.
- **Membership (AD-7):** idempotent join as the last pre-Provider step under the service principal; fail-closed with no Provider work; block set only by Administrator or Facilitator; mirrored through the removal seam.
- **Proposal state machine (AD-5):** ten states, awaiting-decision triple, `Approved` pins `ApprovedVersionId` and removes edit/regenerate/approve, expiry frozen on approval, enforced on every read regardless of timer delivery, versions immutable and preserved, typed system-abandonment reasons.
- **Nine lock-bearing families (AD-12):** identical roster to the UX confirmation table; advisory lock from submission to authoritative rejection or terminal result; EventStore concurrency authoritative; timeout never implies success.
- **Nearing expiry and expiry defaults (Time convention, AD-25, AD-28):** 24 h default, 1 h to 30 d for future proposals, 10 percent or 1 hour whichever is smaller, 15-minute floor, server instants only; matches EXPERIENCE.md § Proposal lifecycle and the WCAG 2.2.1 argument.
- **Contract growth (AD-15):** `ProviderReadinessResult` with by-Provider/model query, the nine `AgentCallOperationStatus` additions, three `AgentReadinessStatus` additions, accepted-by session reference, `IProjectionChangeDetailNotifier` nudge; call-status tokens on the automatic path are distinct from proposal states, as UJ-2 step 7 requires.
- **Provider readiness (AD-10):** triple `OperationalState / Callability / ReasonCode`, `Degraded` callable only on the single non-blocking warning, blocked list, `PlatformNotReady`, `Freshness`, `ValidUntil`; matches § Provider and model and the interim `Unknown` rule.
- **Approver policy (AD-8):** Facilitator on the `ConversationOwner` wire identifier, disclosure category per source defaulting to operator-only, Conversation-scoped resolution, predicate at configuration/call/edit/approval time, future-only publication with snapshot retention.
- **Regeneration ceiling (AD-21):** default 3, range 1 to 10, typed rejection; matches the UX numbers and Product sign-off flag.
- **Not-found parity and error shape (Errors convention):** absent and cross-tenant keys return the identical response; 202 with authoritative identity and projection version; typed `AgentsProblem`.
- **Notifications and inspection (Notifications convention, AD-22):** in-product only, never grant rights; two inspection levels, `existence only` for a former Approver, compliance inspection scoped and justified.
- **UI inheritance (AD-25, UI convention):** WCAG 2.2 AA, 320 px, live-region ownership, EN/FR parity, `InsufficientEvidence` on missing evidence; typed justification on governance writes.
