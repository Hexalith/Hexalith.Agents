# PRD Reconciliation Review — Architecture Spine (Hexalith Agents)

**Date:** 2026-09-09
**Target:** `ARCHITECTURE-SPINE.md` (frontmatter `updated: 2026-09-09`, re-distilled today)
**Authority:** `prds/prd-agents-2026-06-23/prd.md` (status `final`, updated 2026-09-09) and `update-report-2026-09-09.md`
**Lens:** every PRD requirement the spine binds (FR-1..FR-33, NFR-1..NFR-14, OQ-1..OQ-23, §8, §8.1, §9, §12) was read against the spine's AD rules, conventions, diagrams, and Deferred table. The PRD is final authority on requirements; the spine may bind stricter architecture but not contradict. Neither document was modified.

## Verdict

**RECONCILED WITH GAPS — not sign-off-ready as written.** The headline 2026-09-09 PRD changes landed: the FR-8 nine-step order (AD-13), the FR-7 Eligible Approver predicate at four moments (AD-8), the FR-18 ten-state contract with frozen expiry after approval (AD-5), the FR-2 three-part membership step and Agents-owned block (AD-7), the FR-27 verdict cache keys (AD-20), the kill-switch owner and effects (AD-12), the six `EXT-CONV-AI-1` seams (AD-6), the two-level inspection model (AD-22), per-tenant `hexa` with tenant-wide response mode (AD-2), and the `UnretiredAssumption` blocker (AD-17).

What did not land is mostly quiet: three places where the spine states something the PRD decided differently (C-1 deletion authority, C-2 Provider retry trigger, C-3 Tenant Agent Administrator abandon scope), two measurement-basis mismatches (C-4 latency clock start, C-5 fast-rejection gate including safety rejections), and twenty-two requirements the spine does not carry — the kill-switch trigger thresholds and A-17, the approval-time safety check as a distinct point, the mode-specific stricter tenant policy, the OQ-18 blocked-history rule, the FR-13 discovery disclosure rule and the durable resolved-Approver set it needs, the Conversation Context Policy owner and `Bounded` seam, and eleven of seventeen §8.1 assumption keys the spine should cite.

Counts: contradictions 5 (2 high, 2 medium, 1 low); not landed 22 (4 high, 10 medium, 8 low); landed correctly 24 areas.

## Not Landed

Each item: PRD reference; what is missing; which AD or convention should absorb it; proposed one-sentence text.

### High

#### N-1 — Kill-switch trigger thresholds and A-17
- **PRD:** FR-28 (kill switch consequence), §8.1 A-17, FR-33 row "Pull the per-tenant kill switch", OQ-22.
- **Missing:** AD-12 names the owner and the effects of `TenantKillSwitch` but not the three recorded trigger conditions (SM-4 confirmed cross-tenant or unauthorized action → Platform Operator immediately; SM-C4 blocked-call share above 50% or posting-failure rate above 10% sustained seven days → Release Operator; two consecutive SM-3/SM-7 launch-health misses → Product disable-or-continue decision), the 30/60-day-then-monthly review cadence, or the `[ASSUMPTION A-17]` key. The Release Operator's authority is conditional on those triggers, so without them AD-12 grants the Release Operator an unconditioned pull.
- **Absorb in:** AD-12; the `product-metrics` measurement contract reference in AD-28.
- **Proposed text:** "The Release Operator pulls the kill switch only on the recorded FR-28 triggers — SM-C4 above 50% or posting failures above 10% sustained for seven days — while the Platform Operator pulls it immediately on any confirmed SM-4 breach, and two consecutive SM-3/SM-7 misses at the 30-, 60-day, or monthly launch-health review yield a recorded Product decision; the thresholds are [ASSUMPTION A-17] and the `product-metrics` projection is their only source."

#### N-2 — Approval-time safety check as a distinct application point
- **PRD:** FR-27 (lead paragraph and "The exact version being approved passes the then-current active policy at approval time"), FR-17, FR-26 third consequence, OQ-9 (four points: pre-Provider, pre-proposal, at approval time, again immediately before posting when not simultaneous).
- **Missing:** AD-20 binds three decisions ("before Provider invocation", "before proposal creation", "the approved version again before posting"); the sequence diagram re-checks only after the durable wait, immediately before posting. The approval command itself must be refused when the exact version — generated or human-edited — fails the then-current policy, and the pre-post check is a fourth, separate decision when approval and posting are not simultaneous (`PostingFailed` retry, administrative retry). A human-edited version must never be approvable unscanned.
- **Absorb in:** AD-20; AD-5 (approval preconditions); sequence diagram.
- **Proposed text:** "The approval command evaluates the exact version being approved, generated or human-edited, under the then-current effective policy and is rejected on failure, and when posting is not simultaneous with approval the posting step runs a fourth fresh decision on the pinned `ApprovedVersionId`, so no edit path places unscanned content in a Conversation."

#### N-3 — Durable resolved Eligible Approver set and the FR-13 discovery disclosure rule
- **PRD:** FR-13 last consequence; FR-7 ("An Approver who has lost Conversation read access may see that a proposal exists and what state it is in, but no proposal content"); UJ-3 edge case.
- **Missing:** FR-13 makes disclosure depend on whether the requester "was previously resolved as an Approver" for that proposal: full content if currently readable, existence and state only if previously resolved but access lost, nothing at all (not listed, not counted) if never resolved. AD-8 resolves "now" only and records nothing durable; AD-5 does not record the resolved set. The rule is undecidable without an `AgentInteraction` event carrying the resolved Eligible Approver `PartyId`s at call time and at each re-resolution, and no AD states the three-tier discovery response.
- **Absorb in:** AD-8 (record), AD-5 or AD-15 (discovery contract), AD-22 (inspection consistency).
- **Proposed text:** "Every Eligible Approver resolution appends an `AgentInteraction` event listing the resolved `PartyId`s, and proposal discovery, queue, and pending count return full content only to a currently-readable resolved Approver, existence and state only to a previously-resolved Party that lost read access, and the absent-key response to any Party never resolved for that proposal."

#### N-4 — Assumption keys the spine should cite
- **PRD:** §8.1 A-1..A-17; FR-28 (`UnretiredAssumption` names the row); §0 ("Inferences about external contracts that no owner has yet confirmed carry an `[ASSUMPTION A-n]` key").
- **Missing:** The spine cites A-5 (AD-11), A-6 and A-8 (AD-21), A-7 (AD-5), and A-9 (AD-7). It binds the substance of A-1, A-2, A-15, A-16 (AD-6 seams), A-3 (AD-8 `ParticipantRole.Facilitator`), A-4 (SM-2 denominator, AD-28), A-10 (AD-2/AD-9 tenant enablement and Platform-Operator-only secret state), A-11 (AD-2 calling restriction), A-12 (AD-22 deletion), A-13 (SM-3/SM-7 thresholds), A-14 (OQ-18 date), and A-17 (N-1) without keying any of them, so a reader of the spine cannot tell which rules rest on unconfirmed inferences. Separately, the spine carries three unkeyed `[ASSUMPTION]` tags of its own (AD-27 seven-day purge window, AD-30 caller identity resolution, Stack xUnit deviation); because FR-28 makes every unretired Architecture-owned assumption an `RQ-1` blocker, these need an owner, a retirement condition, and a key the `UnretiredAssumption` blocker can name.
- **Absorb in:** AD-6, AD-8, AD-2, AD-9, AD-22, AD-28, AD-12 inline keys; a short "Architecture Assumptions" list (A-18.. or `ARCH-A-n`) under External V1 Prerequisites, or rows contributed to PRD §8.1 by a PRD Update.
- **Proposed text:** "Every spine rule that rests on a PRD §8.1 assumption carries its `[ASSUMPTION A-n]` key inline, and each spine-originated assumption is listed with owner and retirement condition so `RQ-1` can name it as an `UnretiredAssumption` blocker."

### Medium

#### N-5 — Mode-specific stricter tenant Content Safety Policy
- **PRD:** FR-26 lead ("a Tenant Agent Administrator may only add restrictions through a stricter mode-specific policy") and fourth consequence; FR-33 row "Publish or version the Content Safety Policy".
- **Missing:** AD-20 says "plus the tenant's stricter restrictions" and AD-2 gives `TenantGovernancePolicy` "tenant safety restrictions", but neither says the tenant restrictions may differ by response mode (Automatic vs Confirmation) or that they can only tighten.
- **Absorb in:** AD-20; AD-2 (`TenantGovernancePolicy` field).
- **Proposed text:** "Tenant safety restrictions are declared per response mode and may only add categories or tighten thresholds relative to the platform version; a restriction that would permit anything the platform version blocks is rejected at configuration."

#### N-6 — OQ-18 blocked-history rule and Deferred row
- **PRD:** FR-27 ("A Conversation whose scanned history fails the active policy stays blocked for generation until that history has been re-evaluated under a policy it passes; V1 provides no redaction or exclusion path"), OQ-18 (deferred, decision due 2026-10-15, A-14).
- **Missing:** AD-20 carries the cache but not the consequence that a single failing historical message blocks the Conversation with no exclusion path, and the Deferred Beyond V1 table has no OQ-18 row although it lists OQ-15 and OQ-23.
- **Absorb in:** AD-20; Deferred Beyond V1 table.
- **Proposed text:** "A Conversation whose scanned history fails the effective policy is blocked for generation with the typed reason `HistoryFailsSafetyPolicy` until re-evaluation under a policy it passes, and V1 offers no per-message redaction or exclusion (OQ-18, decision due 2026-10-15 [ASSUMPTION A-14])."

#### N-7 — Conversation Context Policy owner and the `Bounded` seam
- **PRD:** FR-9 (`Full`, `Bounded`, `Blocked` modes; "A bounded-context mode presented as an input without a matching declared Approved Bounded Context Behavior is a rejected input"), §3 Conversation Context Policy, §10 ("Agent administration: ... Conversation Context Policy"), OQ-10 ("the seam exists so a governed bounded behavior can be approved later without a contract change").
- **Missing:** AD-4 snapshots a `ContextPolicyReference` and AD-11 names the `Blocked` mode, but no aggregate in AD-2 owns the Conversation Context Policy, the `Bounded` mode and its rejected-input rule do not appear, and the Naming convention does not list the term.
- **Absorb in:** AD-2 (owner — `TenantGovernancePolicy` or `Agent`), AD-11, Naming convention.
- **Proposed text:** "The versioned Conversation Context Policy is `Agent` configuration snapshotted as `ContextPolicyReference`; V1 publishes exactly one version declaring no Approved Bounded Context Behavior, the public context mode enum is `Unknown`, `Full`, `Bounded`, `Blocked`, and a request carrying `Bounded` while the policy declares no behavior is a typed rejection, never a fallback."

#### N-8 — Status counters for structural unavailability (SM-C4 inputs)
- **PRD:** FR-25 fourth and fifth consequences; SM-C4; FR-7 last consequence; FR-18 (system-abandoned reported under FR-25).
- **Missing:** No AD or projection statement says status exposes, per tenant, counts of calls blocked by context policy, safety, cost caps, `NoEligibleApprover`, `RemovedInConversations` or the block, and system-abandoned proposals by reason, nor reserved-versus-settled spend with the 80%/100% states. AD-17 defers projection inventory to the register, but the spine's Capability map should name where these live.
- **Absorb in:** AD-15 (public contract) and Capability map row "Runtime latency and product metrics" or "Audit/status evidence".
- **Proposed text:** "The `product-metrics` and `agent-setup` projections expose per-tenant blocked-call counts by typed reason (context policy, safety, cost cap, `NoEligibleApprover`, `RemovedInConversations`), system-abandoned proposals by reason, and reserved-versus-settled spend with the 80% and 100% states, and they are the only SM-C4 and FR-32 status sources."

#### N-9 — Read-access re-check moments beyond the four predicate moments
- **PRD:** FR-7 ("re-checks that access at discovery, edit, regeneration, approval, rejection, abandonment, and audit-content inspection").
- **Missing:** AD-8 applies the Eligible Approver predicate at configuration, call, edit, and approval; it omits the read-access re-check at discovery, regeneration, rejection, abandonment, and audit-content inspection.
- **Absorb in:** AD-8.
- **Proposed text:** "Current Conversation read access is re-read from Conversations at discovery, edit, regeneration, approval, rejection, abandonment, and audit-content inspection and fails closed when absent, stale, or unavailable."

#### N-10 — Configuration-time Approver Policy rule
- **PRD:** FR-7 first bullet of the four moments.
- **Missing:** AD-8 says "reject a policy that can never yield one" without the decidable rule: a policy naming no Conversation-dependent source (Facilitator or tenant role) and fewer than two predefined Parties is rejected; Facilitator-only is valid.
- **Absorb in:** AD-8.
- **Proposed text:** "At configuration the aggregate rejects a policy with no Conversation-dependent source and fewer than two predefined Parties; a Facilitator-only policy is valid."

#### N-11 — Disclosure category vocabulary and restricted-role constraint
- **PRD:** FR-7 ("user-visible, operator-only, redacted, or omitted"; "a source whose disclosure would reveal membership of a restricted tenant role cannot be configured as user-visible").
- **Missing:** AD-8 names only the default (operator-only).
- **Absorb in:** AD-8; API and contract versioning convention (enum).
- **Proposed text:** "`ApproverPolicyDisclosure` is `Unknown`, `UserVisible`, `OperatorOnly`, `Redacted`, `Omitted`; a tenant-role source flagged restricted cannot be `UserVisible`."

#### N-12 — Provider retry budget in the capability floor and the retry precondition
- **PRD:** FR-4 ("A per-model retry budget, where configured, authorizes re-invocation only for an attempt whose Provider outcome is a confirmed no-usage outcome, under the same cost reservation"), NFR-11.
- **Missing:** AD-10's capability floor has no per-model retry budget, and AD-13 does not bind the retry to a confirmed no-usage outcome (see C-2).
- **Absorb in:** AD-10 (field), AD-13 (precondition).
- **Proposed text:** "The catalog entry optionally records a per-model retry budget, and a transport retry is authorized only after outcome lookup by `ProviderIdempotencyKey` confirms no usage and the budget is not exhausted."

#### N-13 — External removal detected at the pre-post step sets the block
- **PRD:** FR-2 last consequence ("observed at the next membership step — at acceptance or before posting — and that step sets the block itself and fails the call or post closed with `RemovedInConversations`").
- **Missing:** AD-7 has the acceptance-time detection setting the block, but the pre-post re-validation only "records `PostingFailed` with a typed reason".
- **Absorb in:** AD-7.
- **Proposed text:** "The pre-post re-validation runs the same three-part step: if `hexa` is absent after Agents established membership, it sets the block, abandons the Conversation's other non-terminal proposals, and records `PostingFailed` with `RemovedInConversations`."

#### N-14 — Security approval on Content Safety Policy publication; roles on governance rows
- **PRD:** FR-33 rows "Publish or version the Content Safety Policy" (Platform Operator with Security approval — "Security approval is a condition on one row, not a role"), "Apply or release legal hold; request authorized export" (Compliance Inspector), "Configure cost caps and rate limits" (Platform Operator or Release Operator set), "Inspect operational status and failed-call evidence" (TAA, Release Operator, Platform Operator; a caller sees own calls only).
- **Missing:** AD-20 has a publisher-declared `ChangeKind` but no recorded Security approval condition; AD-22 names no role for hold or export; AD-21 names who may lower caps but not who sets them; no AD states the caller-sees-own-calls-only status rule.
- **Absorb in:** AD-20, AD-22, AD-21, AD-15.
- **Proposed text:** "`PolicyPublication` requires a recorded Security approval reference in the command; `LegalHold` and `ExportRequest` are Compliance Inspector commands; caps and rate limits are set by the Platform Operator or Release Operator; and status queries return a caller only the outcomes of their own interactions."

### Low

#### N-15 — Compliance inspection approver and rate surface are the Tenant Agent Administrator
- **PRD:** FR-24 ("either second-party approval or post-hoc review (both by the Tenant Agent Administrator), has its rate visible on an audit surface the Tenant Agent Administrator can read").
- **Missing:** AD-22 says "second-party-approved or post-hoc-reviewed, rate-visible" without naming the TAA as approver, reviewer, and reader.
- **Absorb in:** AD-22.
- **Proposed text:** "The second-party approval or post-hoc review of a compliance inspection is a Tenant Agent Administrator command, and the inspection rate is exposed on an audit projection the Tenant Agent Administrator can read."

#### N-16 — Named posting-failure reasons for the pre-post re-validation only
- **PRD:** FR-2 ("`MembershipUnavailable` and `MembershipRejected` remain posting-failure reasons for that re-validation only").
- **Missing:** AD-7 says "a typed reason" without naming them or the "re-validation only" scoping, which prevents a membership failure at acceptance from being mislabeled as a posting failure.
- **Absorb in:** AD-7.
- **Proposed text:** "`MembershipUnavailable` and `MembershipRejected` are `PostingFailed` reasons produced only by the pre-post re-validation; acceptance-time membership failures use the FR-8 acceptance rejection reasons."

#### N-17 — Separate non-approvable failure record
- **PRD:** FR-10 ("Any failed or incomplete generated content retained for authorized audit is stored only in a separate, non-approvable failure record").
- **Missing:** AD-5 and the sequence diagram (`GenerationFailed`) do not state that retained failed output is a distinct record that never becomes a `ProposalVersion`.
- **Absorb in:** AD-5.
- **Proposed text:** "Failed or incomplete generated content, when retained, is a `GenerationFailureRecord` under the AD-14 protection and is never a `ProposalVersion`, so it cannot be edited, regenerated, or approved."

#### N-18 — Deprecate-and-reject register listed in full; `[Flags]` exemption; `AgentSetupWriteStatus` correction
- **PRD:** FR-23 (register of four values; `[Flags]` use `None = 0`; `AgentSetupWriteStatus.Submitted` at zero is non-conformant and corrected additively before the first tenant is enabled; breaking change needs package-consumer compatibility tests).
- **Missing:** AD-21 names the two postures and AD-8 the Caller source, but `ContentSafetyFailureHandling.BlockWithAuditableOverride` appears nowhere; the API and contract versioning convention does not carry the `[Flags]` exemption, the `AgentSetupWriteStatus` correction, or the compatibility-test obligation. The update report lists these as implementation debt, so the spine should own the rule.
- **Absorb in:** API and contract versioning convention; AD-20.
- **Proposed text:** "The V1 deprecate-and-reject register is `CostControlPosture.ReportingOnlyMonitoring`, `CostControlPosture.AcceptedLaunchRisk`, `ContentSafetyFailureHandling.BlockWithAuditableOverride`, and `ApproverPolicySourceKind.Caller`, each declared and deserializable but rejected with a typed error; `[Flags]` enums use `None = 0`; `AgentSetupWriteStatus` gains `Unknown = 0` additively before the first tenant is enabled; and a Contracts major bump ships package-consumer compatibility tests."

#### N-19 — Pricing-version and `CapabilityVersion` concurrency-token rules
- **PRD:** FR-4 ("pricing version that starts at 1 on create and strictly increases"; non-negative unit prices; "`CapabilityVersion` ... acts as the optimistic-concurrency token for catalog writes: a caller-supplied expected version below the stored value is rejected as regressed, and one above it is rejected as stale").
- **Missing:** AD-10 binds monotonic non-reuse and "any observed decrease is a blocker" but not the pricing-version start and strict increase, non-negative prices, or the expected-version-below/above rejection classes.
- **Absorb in:** AD-10.
- **Proposed text:** "Catalog writes carry an expected `CapabilityVersion`: below the stored value rejects as `Regressed`, above rejects as `Stale`; pricing versions start at 1, strictly increase, and carry non-negative unit prices, with any violation a typed rejection."

#### N-20 — Reused-verdict provenance in Audit Evidence
- **PRD:** FR-27 ("Audit Evidence records the policy version each reused verdict came from"), OQ-9.
- **Missing:** AD-20 keys and invalidates the cache but does not require the safety decision event to record which cached verdicts were reused and under which policy version.
- **Absorb in:** AD-20.
- **Proposed text:** "A pre-Provider safety decision event lists, per reused message verdict, the content hash and the policy version that produced it."

#### N-21 — Regeneration ceiling and expiry duration are per-Agent, TAA-owned configuration
- **PRD:** FR-32 lead ("the Tenant Agent Administrator ... separately configures the per-proposal regeneration ceiling"), FR-33 "Configure `hexa`" row (proposal expiry duration, regeneration ceiling), FR-16, FR-18, OQ-3 ("configurable per Agent").
- **Missing:** AD-2 places the regeneration ceiling and expiry defaults in `TenantGovernancePolicy` beside the caps that AD-21 says the TAA "may lower but never raise". Because `hexa` is one Agent per tenant this is not behaviourally wrong, but the spine should say these two values are TAA-owned within their ranges (1..10; 1 hour..30 days), not lower-only, and whether their change bumps `ConfigurationVersion`.
- **Absorb in:** AD-2 or AD-4; AD-21.
- **Proposed text:** "The regeneration ceiling and proposal expiry duration are Tenant Agent Administrator configuration set anywhere within their PRD ranges, apply to future proposals only, and increment `ConfigurationVersion`; the lower-only rule in AD-21 applies to caps and rate limits alone."

#### N-22 — Minor quiet requirements
- **PRD:** FR-1 (configuration audit records "prior value where safe to expose"); FR-32 (rate limits over "a stated rolling window"; cap changes "never retroactively release or re-reserve settled spend"); FR-4 (documented migration state for read-only inspection of an Agent on a disabled Provider/model); §9 and OQ-8 ("support-safe non-content tombstone"); §11 (a Level 4/5 claim must cite harness and environment in its evidence manifest); §8.1 A-4 (an Agents-owned SM-2 denominator substitute must count from a Conversations-side feed, never from Agent Calls); FR-28 ("Any per-Agent readiness record is transitional and must reconcile to that authority before `RQ-1`"; `InsufficientEvidence` on a gate metric records NOT READY naming the metric).
- **Missing:** none of these appears in the spine.
- **Absorb in:** Audit envelope convention (prior value); AD-21 (rolling window, no retroactive settlement); AD-10 (migration read-only state); AD-22 (tombstone); AD-17 (manifest citation, transitional per-Agent readiness, NOT READY naming); AD-28 (A-4 substitute rule).
- **Proposed text:** "Configuration audit events carry prior and new values where safe; rate limits name their rolling window; cap changes never touch settled spend; an Agent on a disabled Provider/model is read-only inspectable in a named migration state; erasure leaves a support-safe non-content tombstone; a Level 4 or 5 claim cites harness and environment or is unproven; `agent-setup` readiness is transitional to `launch-readiness`; `RQ-1` records NOT READY naming any gate metric at `InsufficientEvidence`; and any Agents-side SM-2 denominator counts from a Conversations event feed [ASSUMPTION A-4]."

## Contradictions

The PRD is final authority on requirements. Each item states the spine claim, the PRD decision, and the fix.

### C-1 — Deletion authority: spine says single-actor, PRD requires Compliance Inspector approval (high)
- **Spine:** Deferred Beyond V1 — "Two-person rule on evidence deletion and legal-hold release: Product and Security own it; V1 requires single-actor deletion with typed justification, exact scope, and the hold interlock." AD-22 names no role for `ProtectedDeletion`.
- **PRD:** FR-33 row "Request approved deletion (FR-30) — Platform Operator with Compliance Inspector approval `[ASSUMPTION A-12]`"; §8.1 A-12 (owner Product + Governance).
- **Impact:** The spine defers a rule the PRD has already decided (provisionally, under A-12) the other way; a story built from the spine would ship a single-actor deletion path that FR-33 rejects.
- **Fix:** In AD-22: "A `DeletionRequest` is submitted by the Platform Operator and becomes executable only after a recorded Compliance Inspector approval [ASSUMPTION A-12]; legal-hold release remains single-actor with typed justification." Rewrite the Deferred row to cover only the legal-hold-release two-person rule.

### C-2 — Provider retry trigger: "transient transport/timeout failures" vs "confirmed no-usage outcome" (high)
- **Spine:** AD-13 — "Only transient transport/timeout failures retry before a terminal outcome, reusing the exact descriptor, reservation, admission, and key while rechecking readiness."
- **PRD:** FR-4 ("authorizes re-invocation only for an attempt whose Provider outcome is a confirmed no-usage outcome ... It never authorizes re-invocation after an `Indeterminate` outcome"); FR-28 ("Only an attempt with a confirmed no-usage outcome is eligible for retry under the same reservation; an `Indeterminate` attempt is never retried"); NFR-11 (at-most-once; an attempt whose outcome cannot be determined is `Indeterminate`); FR-12.
- **Impact:** A timeout is, by the PRD's definition, an attempt whose outcome cannot be determined until outcome lookup says otherwise. As written, AD-13 permits a retry on a bare timeout, which is the double-charge and duplicate-generation case NFR-11 forbids. AD-13 already provides the outcome-lookup primitive; the precondition is what is missing.
- **Fix:** Replace the sentence with: "A retry before a terminal outcome is authorized only when outcome lookup by `ProviderIdempotencyKey` confirms no usage and the catalog's per-model retry budget is not exhausted; a transport failure or timeout without that confirmation resolves the attempt to `Indeterminate` and is never retried."

### C-3 — Tenant Agent Administrator abandon of `PostingFailed` is scoped narrower in the PRD (medium)
- **Spine:** AD-5 — "after which the proposal persists until an Eligible Approver abandons it, the Tenant Agent Administrator abandons or administratively retries it, or the system abandons it."
- **PRD:** FR-33 row "Abandon a `PostingFailed` proposal whose Source Conversation is gone or inaccessible (FR-18) — Tenant Agent Administrator; audited; no Conversation read access required"; FR-18 ("the Tenant Agent Administrator abandons it under the audited FR-33 row that needs no Conversation read access").
- **Impact:** The spine grants the TAA an unconditional abandon on any `PostingFailed` proposal; the PRD grants it only when the Source Conversation is gone or inaccessible (otherwise an Eligible Approver decides). The spine is looser than the PRD, not stricter.
- **Fix:** "the Tenant Agent Administrator abandons it, audited and without Conversation read access, only when the Source Conversation is gone or inaccessible to the Agents Service Principal, or administratively retries it".

### C-4 — NFR-9 clock start: spine measures from `InteractionRequested`, PRD from acceptance (medium)
- **Spine:** AD-28 — "NFR-9 runtime latency and SM product metrics derive only from commit timestamps of named Agents events (`InteractionRequested` to `PostingSucceeded` or `ProposalCreated`; ...)".
- **PRD:** §3 Accepted Agent Call ("The acceptance timestamp is recorded when the last check passes; it starts the NFR-9 latency clocks"); NFR-9 and OQ-5 ("accepted-call-to-post", "accepted-call-to-proposal"); SM-2 numerator counts Accepted Agent Calls.
- **Impact:** A longer interval is a stricter gate, which the spine may bind, but it is not the PRD's metric, and no named event marks acceptance, so neither the NFR-9 clock nor the SM-2 numerator has an authoritative source event. The nine-step pipeline includes a safety classifier call and a Conversations read whose latency the PRD deliberately excludes from these gates (FR-27 last consequence).
- **Fix:** AD-13 appends an `AgentCallAccepted` event when the ninth step passes; AD-28 reads "`AgentCallAccepted` to `PostingSucceeded` or `ProposalCreated`" and names `AgentCallAccepted` as the SM-2 numerator event.

### C-5 — Fast pre-Provider rejection gate must exclude classifier-dependent rejections (low)
- **Spine:** AD-28 — fast rejection is "`InteractionRequested` to the first blocking event".
- **PRD:** FR-27 ("The content safety scan is budgeted separately and is excluded from the pre-Provider rejection gate in NFR-9; that gate covers authorization, policy, budget, and context rejections that do not require a classifier call"); NFR-9.
- **Impact:** A `SafetyBlocked` first blocking event would enter the p95 ≤ 2 s sample and either fail the gate spuriously or hide classifier latency that must be measured separately.
- **Fix:** "the fast-rejection sample excludes any blocking event whose reason required a content-safety classifier call, and safety-scan latency is measured as its own `runtime-metrics` series."

### Noted, not a contradiction — sequence diagram step order
The AD-13 prose carries the FR-8 order correctly, but the sequence diagram performs the "authorized complete Conversation read" before "rate limits, lifecycle, enablement, block, kill switch re-read", i.e. step 5 before steps 2 and 4. Since a rate-limited caller must not trigger a Conversations read, reorder the diagram to match the prose.

## Landed Correctly

- FR-8 nine-step acceptance order — AD-13 prose, with `NotInvoked` release in AD-21.
- FR-7 Eligible Approver predicate at four moments, Conversation-scoped resolution, caller source retired, `ConversationOwner` wire identifier labelled Conversation Facilitator — AD-8, Deferred row OQ-15.
- FR-18 ten `ProposedAgentReplyState` values, `Unknown = 0` never recorded, frozen expiry after approval, expiry enforced on every read and command, 3-attempts/15-minute retry [A-7], typed system-abandon reasons — AD-5, Time convention.
- FR-2 three-part membership as last pre-Provider step, Agents-owned block set/cleared by TAA or Facilitator [A-9], mirrored through the removal seam, non-terminal proposals abandoned on removal — AD-7.
- FR-27 verdict cache keyed by Conversation, content hash, policy version and invalidated on publication; Approvers cannot override; retries cannot weaken policy (via `RestrictivenessRank`) — AD-20.
- FR-28 kill-switch owner and effects (calls disabled, awaiting proposals reject/abandon only, `Approved`/`PostingPending` complete on their own terms, nothing deleted) — AD-12; postures `Quotas`/`Budgets`/`ProviderModelLimits` only with `ProhibitedCostControlPosture` — AD-21.
- FR-28 / OQ-22 `RQ-1` reads only qualification-cohort evidence and records `UnretiredAssumption` for unretired Product/Architecture/Governance rows — AD-17.
- FR-6 / OQ-20 per-tenant `hexa` with tenant-wide response mode — AD-2, Deferred row.
- FR-24 / OQ-21 two-level inspection (posted provenance for Participants; unposted content for resolved Eligible Approvers or scoped, justified, second-party-approved or post-hoc-reviewed compliance inspection; survives Conversation loss) — AD-22.
- §8 six `EXT-CONV-AI-1` seams and `EXT-CONV-UI-1` action plus provenance decoration — AD-6, AD-31.
- FR-9 / NFR-8 / OQ-10 Safe Context Budget terms, 10% margin (5–25) [A-5], every term in audit, fail-closed oversized case, no silent truncation — AD-11 (stricter than the PRD by requiring the exact tokenizer through `EXT-TOKEN-1` with no documented approximation; permitted).
- FR-16 / FR-32 regeneration ceiling default 3, range 1–10 [A-6]; regeneration re-runs context and safety on the live Conversation — AD-21, AD-11.
- FR-28 / OQ-6 reservation after context measurement and before safety; `Indeterminate` hold default 24 h, 1–72 [A-8], settled `ChargedAtMaximum`; 80% warn, 100% fail closed; TAA may lower caps only; override Platform-Operator-only — AD-21.
- FR-4 / OQ-7 capability floor, monotonic `CapabilityVersion`, enable/disable never bumps version, tenant view via `TenantProviderEnablement`, `PlatformNotReady` — AD-10.
- FR-4 / NFR-6 secret handles, `SecretConfigured` from probe, secrets never in events/logs/exports, Platform-Operator-only secret state — AD-9, AD-14.
- FR-12 `Indeterminate` typed outcome distinct from `Unknown` — AD-21.
- FR-11 / FR-17 AI-generated and human-edited provenance carried in message metadata and rendered through the seam — AD-6, AD-31.
- FR-13 / OQ-4 in-product notifications only (queue, count, Conversation status entry) — Notifications convention.
- FR-29 `Submitted` → `AuthoritativePending` → `ProjectionConfirmed`, freshness never recomputed by the UI — AD-15, AD-17.
- FR-33 six roles mapped one-to-one to FrontComposer policies, Conversation Participant resolved from Conversations, missing assignment fails closed, role basis recorded — AD-30.
- FR-31 instructions separated from context in the role/message structure; `ControlBypass` covers instruction-override attempts — AD-20.
- §9 / OQ-8 365-day retention from terminal instant, legal hold pins DEK, cryptographic erasure, restrictive purge confirmation including `workflow-execution-state`, manifested HMAC-signed export, Conversation deletion signal propagation, posted messages under Conversations retention — AD-22, AD-27.
- NFR-9 thresholds and n ≥ 30, NFR-11..NFR-14 qualification contracts, Evidence Levels 4 and 5 — AD-17, AD-23..AD-26, AD-28.
- OQ-1 sole invocation entry; OQ-2 Dapr Workflow execution-only; OQ-12/OQ-13 sources and single named Agent; OQ-19 metric split; OQ-23 retraction metric deferred — AD-31, AD-18, AD-19, AD-28, Deferred table.
