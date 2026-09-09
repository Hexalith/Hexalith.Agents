---
name: Hexalith Agents Architecture Spine Adversarial Divergence Review
type: architecture-spine-review
target: architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md
date: 2026-09-09
lens: adversarial divergence v7
---

# Adversarial Divergence Review v7

**Verdict:** The spine still lets two builders, each following every `AD-n`'s Rule to the letter, ship incompatible systems — the round-4 edits fixed the *wording* of several prior findings without fixing the underlying ambiguity, and one round-4 addition (AD-30's Platform-freshness source) appears to cite a Hexalith.Tenants data model that does not match the sibling module's own documented domain split.

## Findings by Severity

Counts: **Critical: 2, High: 3, Medium: 1, Low: 0**

---

### CRITICAL-1 — AD-30's Platform-principal freshness source names a Tenants-projection shape that Hexalith.Tenants' own docs contradict

**Quoted text (AD-30):**
> `Platform` (the `system`-tenant Platform Operator, carried as the reserved `actor:agentsProviderAdmin` extension, populated only by the Agents API ingress after a fresh role check against the `system` tenant's own Tenants-projection — the authoritative source for this one non-tenant-scoped FR-19 role, never the envelope/target tenant's projection used for `User`/`Administrator` — on the same never-cached, never-token-embedded terms as `User` and `Administrator` above)

This is a round-4 edit that closes a prior "which projection does Platform read?" gap by naming a specific source: "the `system` tenant's own Tenants-projection." It explicitly calls Platform Operator "this one non-tenant-scoped FR-19 role" — matching the PRD ("Every role except Platform Operator is tenant-scoped; a role assignment in one tenant grants nothing in another (FR-19)").

**Why the citation doesn't deliver what it claims:** `Hexalith.Tenants/_bmad-output/project-context.md` (a source this spine binds) states:

> "The two aggregate domains are `tenants` and `global-administrators`." ... "Constants: `DefaultTenantId = "system"`, `Domain = "tenants"`" ... "global administrator events publish on the shared `tenants.events` topic via AppHost gateway topic override" ... branch-coverage gate names `GlobalAdministratorsAggregate.cs` explicitly as an isolation/auth file distinct from `TenantAggregate.cs`.

Hexalith.Tenants models a **platform-wide, non-tenant-scoped** administrator concept as a *separate aggregate domain* (`global-administrators`), deliberately distinct from the per-tenant `tenants` domain (which merely happens to use the string `"system"` as `DefaultTenantId` for its own bookkeeping). The spine's claim that "the `system` tenant's own Tenants-projection" is "the authoritative source for this one non-tenant-scoped FR-19 role" conflates the tenant-scoped `tenants` domain (filtered to id `"system"`) with the genuinely non-tenant-scoped `global-administrators` domain — exactly the distinction Hexalith.Tenants itself draws for this scenario. The round-4 fix "fixed the citation, not the defect": it now names *a* source confidently, but the source it names is the wrong one per the cited sibling module's own contract.

**Two-unit divergence:**
- **Team A** (implements literally against AD-30's sentence): builds the Agents API ingress to query the Tenants `tenants`-domain projection scoped to `TenantId = "system"` for Platform Operator role assignment freshness — because that is exactly what "the `system` tenant's own Tenants-projection" says.
- **Team B** (implements against the actual Hexalith.Tenants integration surface, having read its project-context and REST contract, `GET /api/global-administrators`): builds the ingress to query the `global-administrators` domain/projection for Platform Operator role freshness, since that is where Hexalith.Tenants actually keeps non-tenant-scoped administrator role state.

Both teams believe they satisfy AD-30's Rule. Team A's integration may find no rows at all (Platform Operator assignments may never be written into the tenant-scoped `tenants` domain under id `system`), silently failing closed for every Platform-principal command tenant-wide — or worse, silently succeeding against stale/empty data that happens to satisfy a fail-open code path. Team B's integration is functionally correct but does not match what AD-30's text says to build against, so an integration test written straight from the spine (Team A's reading) will not exercise the real seam.

**Fix:** AD-30 must name the exact Hexalith.Tenants surface — domain (`global-administrators` vs `tenants`), and the concrete query/endpoint — that is authoritative for Platform Operator role freshness, and reconcile the "system tenant" framing with Hexalith.Tenants' documented two-domain model (`tenants` + `global-administrators`). If Product/Architecture in fact intend Platform Operator role assignment to live in the tenant-scoped `tenants` domain under a reserved `system` tenant id (overriding Hexalith.Tenants' own `global-administrators` concept), that decision needs to be stated and justified, not implied by reusing the phrase "Tenants-projection" ambiguously.

---

### CRITICAL-2 — AD-7's block-clearing-authority clause is ambiguous between Party-identity and role-based clearing for a Facilitator-set block

**Quoted text (AD-7, main Rule):**
> "a block is cleared only by the specific authority that set it, re-evaluated as currently held at clear time (a Party who has since lost the Facilitator role cannot clear it) — a Conversation Facilitator can never clear a block the Tenant Agent Administrator set — or by the Tenant Agent Administrator, while a system-set `ExternallyRemoved` block, having no setting Party, stays clearable by the Tenant Agent Administrator or any current Facilitator."

**Quoted text (AD-7, Second-update membership reconciliation):**
> "An authorized clear must name the current version; the Facilitator cannot clear an administrative block, and `ExternallyRemoved` may be cleared only by the Tenant Agent Administrator or current Facilitator."

This is the round-4-touched clearing clause. It resolves the TAA-set case ("a Conversation Facilitator can never clear a block the Tenant Agent Administrator set") and the system-set case ("any current Facilitator") at the *role* level — no identity check needed. But for a **Facilitator-set** block, the main clause says clearing requires "the specific authority that set it, re-evaluated as currently held" with the parenthetical keyed to a specific *Party* ("a Party who has since lost the Facilitator role cannot clear it"). Nothing in either paragraph states whether a *different* Party who currently holds the Facilitator role (but did not set that particular block) may clear it. The second-update paragraph is silent on this exact case — it only rules out the Facilitator clearing an *administrative* (TAA-set) block, never addressing Facilitator-clearing-a-Facilitator-set-block by a different Facilitator.

**Two-unit divergence:**
- **Team A** (identity-based reading, literal to "the specific authority that set it"): persists the setting `PartyId` on the block record; clear requires `callerPartyId == settingPartyId` *and* that caller currently holds Facilitator role. A different current Facilitator's clear attempt is rejected.
- **Team B** (role-based reading, generalizing from the explicit TAA/Facilitator role-level carve-outs elsewhere in the same clause): treats "the specific authority" as meaning "the role class that set it" (Facilitator vs TAA), mirroring how the TAA-set and system-set cases are resolved purely by role, not by individual identity; implements "any current Facilitator may clear any Facilitator-set block," storing no setting-Party identity at all.

These builds are incompatible: the same clear command from a *different* current Facilitator succeeds under Team B and is rejected under Team A. Both teams can point to text in the same clause supporting their reading.

**Fix:** State explicitly, for the Facilitator-set case, whether clearing requires the *same Party* that set it (still holding Facilitator) or *any* Party currently holding Facilitator role for that Conversation — the way the TAA-set and system-set branches already state their rule unambiguously at the role level.

---

### HIGH-1 — AD-22's DigestKey rotation sentence calls rotation "lock-bearing" one paragraph after AD-22 closes the lock-bearing family list to exactly nine, "with no exempt family"

**Quoted text (AD-22, lock-bearing enumeration, immediately preceding the DigestKey sentence):**
> "the reject-on-missing-or-whitespace-justification rule applies to all nine AD-12 lock-bearing families — `ProposalResolution`, `PolicyPublication`, `TenantBudgetUpdate`, `LegalHold` (also `LegalHoldRelease`), `ExportRequest` (also `ExportDownload`), `DeletionRequest`, `ProviderCatalogMutation` (also `TenantProviderEnablement`), `AgentSetupMutation` (also `TenantKillSwitch`), and `AgentActivation` — with no exempt family."

**Quoted text (AD-22, round-4 DigestKey addition, same AD, later paragraph):**
> "The per-tenant `DigestKey` ... is never rotated for a tenant holding any unerased protected content ... rotation is permitted only after full-tenant erasure or an accepted-risk waiver, and is itself a justified, audited lock-bearing operation under `EXT-SECRETS-1` [ASSUMPTION ARCH-A-12]."

DigestKey rotation is not one of the nine named families, and AD-12's own text says the lock-bearing "family vocabulary is owned by the register matrix version" — a closed, versioned enumeration, not an open-ended adjective. Calling rotation "lock-bearing" here either silently creates a tenth family (undermining "with no exempt family" as an exhaustive closed list one paragraph earlier) or uses "lock-bearing" loosely to mean nothing more than "justified and audited" — indistinguishable from prose without a normative signal either way.

**Two-unit divergence:**
- **Team A** (literal on "lock-bearing operation"): adds DigestKey rotation to the AD-12 family vocabulary/matrix as a tenth entry, enforcing the one-pending-command-per-session advisory lock and the reject-on-missing-justification rule on it, and wires it into the FR-33/lock UI surfaces the same way as the other nine families.
- **Team B** (literal on "all nine... no exempt family" as the closed, authoritative list): treats the DigestKey sentence's "lock-bearing" as descriptive prose only, implements rotation purely as an `EXT-SECRETS-1`-custodied administrative operation with justification and audit logging but *no* AD-12 session-lock participation, and never touches the family matrix.

Under Team B's build, two concurrent rotation requests for the same tenant race with no advisory lock preventing double submission — something Team A's build structurally prevents. The two units also disagree on whether rotation shows up anywhere in the FR-33 lock-bearing-family UI/audit surfaces.

**Fix:** Either add DigestKey rotation as a named tenth family in the AD-12/register matrix (and update "all nine... with no exempt family" to "all ten"), or explicitly state that rotation is deliberately outside the AD-12 lock-bearing family system despite being justified/audited, and say which mechanism (if any) prevents concurrent rotation races.

---

### HIGH-2 — The Consistency Conventions "Provider attempt fingerprint" row asserts a keyed-HMAC algorithm AD-13 itself never states, and that AD-29 explicitly treats as a *different* function

**Quoted text (Consistency Conventions table, "Provider attempt fingerprint" row):**
> "One shared canonicalizer hashes the length-prefixed, stable-order prepared request with HMAC-SHA-256 under the per-tenant `DigestKey` (AD-29's sensitive-content-digest rule) over the AD-13 field inventory; descriptors store the digest and safe scalar inputs, never raw context."

**Quoted text (AD-13, the fingerprint's own fuller description):**
> "(1) appends one prepared descriptor, consuming the next `AttemptOrdinal` and binding `AttemptId` to Provider/model, `EffectiveProviderCapabilityVersion`, validated limits/timeout, and the canonical request fingerprint..." ... "The fingerprint covers `TenantId`, `ProviderId`, `ModelId`, `EffectiveProviderCapabilityVersion`, `InstructionsVersion`, the structured role/provenance message sequence with per-message content digests, generation parameters, reserved output tokens, and the evaluated safety policy versions; nothing else invalidates a retry."

AD-13 never states an algorithm for "the canonical request fingerprint" — it calls it canonical and lists covered fields, nothing about keying. Worse, AD-29 (round-4-adjacent text, unchanged in this pass but directly relevant) explicitly separates the two:

**Quoted text (AD-29):**
> "This is the one command-payload fingerprint function every command-handler story uses; it is distinct from the AD-13 Provider-attempt fingerprint (which hashes the prepared external request, not the command DTO) and from this AD's own id-derivation components (which never include full payload state and stay unkeyed)."

AD-29 names the command-payload fingerprint as the thing that is HMAC-SHA-256-keyed, and calls out the AD-13 Provider-attempt fingerprint as *distinct* from it — distinguishing by input scope, but never confirming the AD-13 fingerprint is keyed at all. AD-13's own "canonical" language and its use of "the shared canonicalizer" (AD-29's canonicalizer, whose baseline behavior elsewhere in AD-29 is unkeyed SHA-256 for deterministic ids) supports reading the *outer* fingerprint hash as unkeyed, with only the embedded "per-message content digests" independently keyed (per AD-29's closing sentence naming "per-message digests" as their own HMAC'd category).

**Two-unit divergence:**
- **Team A** (builds from AD-13 + AD-29's unkeyed-canonicalizer default): computes the outer Provider-attempt fingerprint with the unkeyed `AgentsIdentity` canonicalizer, embedding already-HMAC'd per-message digests as some of its inputs.
- **Team B** (builds from the Consistency Conventions row): computes the entire fingerprint as one HMAC-SHA-256 value keyed under the tenant `DigestKey`.

These produce different fingerprint *values* for identical inputs. Any two components built by different teams (a workflow activity vs. a recovery/replay path recomputing the same fingerprint per AD-13's "any change to the fingerprint... fails closed under that attempt id") will disagree and spuriously reject a legitimate retry as changed. It also silently changes the blast radius of AD-22's DigestKey rotation restriction: under Team B's reading, rotating a tenant's `DigestKey` invalidates *every* in-flight `ProviderAttempt`'s retry-fingerprint comparability tenant-wide (not just per-message digest comparability), a materially larger operational consequence than AD-22's rotation-restriction text describes or scopes for.

**Fix:** State the algorithm for the Provider-attempt fingerprint inside AD-13 itself (not only in the summary table), and reconcile the "distinct from" language in AD-29 so it's clear whether "distinct" means a different keyed function or a different (unkeyed) function entirely.

---

### HIGH-3 — AD-5's "AD-13 seam-2 existence read" citation names the wrong AD, and collides with AD-13's own internal step numbering

**Quoted text (AD-5):**
> "...the system looks the approved version's `MessageId` up through the AD-13 seam-2 existence read (skipped only for a `PostingFailed` proposal whose failure was a pre-post re-validation that never attempted a post)."

The numbered Conversations seam list ("(1) Membership... (2) Posting: ... plus an existence read by `MessageId`... so Agents can check before retry or abandonment... record `LateConfirmed`... (3) Facilitator resolution... (4)... (5)... (6)...") is owned by `external-dependency-register.md`'s `RequiredArtifact` row for `EXT-CONV-AI-1`, and is described in prose (unnumbered) by **AD-6**, not AD-13. Seam "(2)" is indeed the Posting seam whose existence-read sub-capability matches what AD-5 needs — so the *target* is right, but the *citation* names AD-13, which owns no seam list at all.

This is not a harmless typo: **AD-13 has its own internal 1-6 numbered step list** for the acceptance/invocation protocol ("(1) appends one prepared descriptor... (2) reserves the estimated cost under `ReservationId`... (3) acquire admission... (4) append `ProviderInvocationAuthorized`... (5) invoke only after step 4... (6) append the outcome"). A reader who trusts the "AD-13 seam-2" citation and looks inside AD-13 for "seam 2" will find AD-13's own step (2) — the `BudgetLedger` reservation step — which has nothing to do with an existence read, `MessageId`, or `LateConfirmed`.

**Two-unit divergence:**
- **Team A** (knows the register's `EXT-CONV-AI-1` seam numbering from having implemented AD-6/the register directly): correctly wires the `Approved`/`PostingFailed` pre-exit check to the Posting seam's existence-read call.
- **Team B** (trusts the "AD-13 seam-2" citation literally, and reads AD-13's own numbered steps as the referent since that's the only numbered list actually inside AD-13): either wires the pre-exit check to something reservation-related (a misreading that produces no existence check at all, silently skipping the `LateConfirmed` safeguard), or spends implementation time unable to find any existence-read capability in AD-13 and escalates/guesses a different seam.

The practical risk is Team B shipping AD-5's `Approved`/`PostingFailed -> Abandoned` transition *without* the existence-read guard AD-5 requires, because the cited AD does not contain what the citing text implies it contains.

**Fix:** Correct the citation to name the register/`EXT-CONV-AI-1` seam list directly (e.g., "the `EXT-CONV-AI-1` seam (2) existence read, per AD-6" or "per `external-dependency-register.md`"), not "AD-13," which has its own unrelated numbered steps.

---

### MEDIUM-1 — AD-12's shared `PausedDuration` field does not say whether overlapping kill-switch and disabled-Agent pause windows are unioned or summed

**Quoted text (AD-12):**
> "`PostingFailed` automatic and administrative retries are suspended and their retry-window clock is paused — the affected proposal accumulates `PausedDuration` recorded at pull and updated at release — on the same terms as the disabled-Agent suspension above; the two causes accrue against the same `PausedDuration` field, and AD-5's deadline is `origin + window + PausedDuration` under AD-28."

"The two causes accrue against the same `PausedDuration` field" is consistent with AD-5's generalized formula ("a deadline computed as origin plus window plus AD-12's `PausedDuration`, since AD-12 pauses this clock while the tenant kill switch is pulled or the Agent is disabled") — the round-4 generalization itself is internally consistent. But neither AD-12 nor AD-5 states how the *same field* accumulates when both causes are simultaneously active for the same proposal (kill switch pulled while that proposal's Agent is also disabled): is `PausedDuration` the union of wall-clock time during which *at least one* cause was active, or the sum of each cause's own tracked duration (double-counting the overlap)?

**Two-unit divergence:**
- **Team A**: implements one shared "is currently paused" boolean/interval per proposal, toggled on whenever either cause first becomes active and released only when *both* causes have cleared; `PausedDuration` accumulates the union of paused wall-clock time.
- **Team B**: implements two independent duration accumulators (one for kill-switch pauses, one for disabled-Agent pauses) and sums them into the single `PausedDuration` field — literally satisfying "the two causes accrue against the same field," but double-counting any period where both were active.

For a tenant where both mechanisms are ever concurrently active on the same proposal, the two builds compute different retry deadlines for the same proposal history.

**Fix:** State explicitly that `PausedDuration` is the union of wall-clock time during which at least one suspending cause was active (not a sum of per-cause durations), or explicitly bless summation if double-counting during overlap is the intended, more-conservative behavior.

---

## Checked and found consistent (no divergence)

Per the specific composition check requested: **AD-12's "hexa's own linked Party state per AD-7" re-read enumeration does *not* contradict or double-specify AD-7's own three-part membership-check timing.** AD-7 explicitly separates two different mechanisms: (a) the three-part participant-state/membership check, scoped only to "the last acceptance step of a call" and "before every post"; and (b) "the AD-12 Party-state gate, which explicitly covers the Agent's own identity link on the same terms as any caller's" — a *different*, lighter liveness check that AD-7 itself says runs wherever AD-12 requires it (i.e., at every side-effecting step, including proposal creation, edit, regeneration, and approval). AD-12's enumeration cites AD-7 correctly for concept (b), not for re-invoking (a) outside its stated scope. AD-4's parallel language ("the per-Conversation Agent block ... every side-effecting step re-reads them under AD-12 and fails closed") likewise reads as a simple state check, not a re-run of AD-7's full three-part procedure. No fix needed here, but this composition is fragile enough (two similarly-worded but functionally distinct "Party state" checks sharing a name) that a future edit to either AD without cross-checking the other could reintroduce ambiguity.
