---
name: Hexalith Agents
type: architecture-spine-review
target: architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md (cross-referenced against launch-readiness-register.md)
date: 2026-09-09
lens: security/data-integrity v7
---

# Security / Data-Integrity Review (v7) — Hexalith Agents Architecture Spine

**Verdict:** Two of the three v6 fixes hold up under independent re-reading (the register now genuinely states `LegalHold`/`DeletionRequest` symmetry, and the Provider-attempt fingerprint is now consistently HMAC-SHA-256 everywhere it is named), but the third fix — the new `DigestKey` rotation rule — resolves "no stated bound" only by stating a bound that is practically unsatisfiable for any operationally active tenant, which is a new and more serious problem than the one it replaced.

## Re-verification of the three claimed fixes

### 1. AD-22/ARCH-A-10 register symmetry — VERIFIED FIXED

v6 Critical Finding 1 found that the spine claimed the register tracked the erasure residual risk symmetrically with the `LegalHold` residual risk, but the register file itself had not actually been edited to say so. Re-reading `launch-readiness-register.md` line 108 now:

> `LR-AUDIT-PROTECTION-DELETION` | Payload protection, authorized audit, retention/legal hold/export, cryptographic erasure/redaction, named projection purge, and safe tombstone evidence. Includes evidence that a `LegalHold` **or a `DeletionRequest`** either propagates to (or has a confirmed accepted-risk waiver for) the posted copy of held **or erased** content in Conversations, since AD-22 concedes posted Conversation Messages are otherwise governed only by Conversations' own independent retention (spine assumption ARCH-A-10).

This is genuinely symmetric: `LegalHold` pairs with "held" content, `DeletionRequest` pairs with "erased" content, both requiring the same evidence-or-waiver treatment. `ARCH-A-10`'s row text (`ARCHITECTURE-SPINE.md` line 780) was also updated to name both commands. AD-22's own claim ("now tracked symmetrically for `DeletionRequest`") is accurate against the current register text. **No finding here.**

### 2. AD-29/AD-13 Provider-attempt fingerprint keying — VERIFIED FIXED

v6 Medium Finding 3 found the Consistency Conventions table stated plain SHA-256 for the "Provider attempt fingerprint" row while AD-29's blanket rule required HMAC-SHA-256 for sensitive-content digests. The table row (`ARCHITECTURE-SPINE.md` line 472) now reads:

> Provider attempt fingerprint | One shared canonicalizer hashes the length-prefixed, stable-order prepared request with HMAC-SHA-256 under the per-tenant `DigestKey` (AD-29's sensitive-content-digest rule) over the AD-13 field inventory; descriptors store the digest and safe scalar inputs, never raw context.

This now matches AD-29's closing sentence ("Every digest of sensitive content stored outside a `ProtectedContent` envelope ... is an HMAC-SHA-256 under a per-tenant `DigestKey`"), and does not contradict AD-13's own text, which never names an algorithm for "the canonical request fingerprint" — it only enumerates the fields the fingerprint covers. AD-29's remark that the Provider-attempt fingerprint is "distinct from" the command-payload fingerprint is a statement about *input* (prepared external request vs. command DTO), not about keying, so there is no residual contradiction. **No finding here.**

### 3. AD-22 `DigestKey` rotation rule — FIX INTRODUCES A NEW PROBLEM (Critical)

See Critical Finding 1 below.

## Additional areas probed (per this round's brief)

- **AD-30 Platform-principal freshness-source rewording:** this also resolves v6 High Finding 2 (which complained that "the Tenants-projection" was ambiguous for a role the PRD defines as the one non-tenant-scoped role). The current text — *"a fresh role check against the `system` tenant's own Tenants-projection — the authoritative source for this one non-tenant-scoped FR-19 role, never the envelope/target tenant's projection used for `User`/`Administrator`"* — now explicitly names which projection and disambiguates it from the `User`/`Administrator` path. No new gap found.
- **AD-12 disabled-Agent `PostingFailed`-suspension addition:** does not create an authorization bypass (every actual retry attempt still re-reads Agent lifecycle and kill-switch state fresh at attempt time per AD-12's general rule, so a stale pause/resume cannot let a real retry through while blocked). It does create a data-integrity/determinism gap — see Medium Finding 2 below.
- **AD-7 block-clearing-authority rewording:** checked specifically for whether a Tenant Agent Administrator's own block could become unclearable after a TAA role handover. It cannot: the clearing rule is a disjunction — "cleared only by the specific authority that set it, re-evaluated as currently held at clear time ... **or by the Tenant Agent Administrator**" — and, consistent with AD-30's blanket rule that every role check is re-read fresh at command time (never bound to the specific historical Party), "the Tenant Agent Administrator" resolves to whoever currently holds that role. So even if the TAA who set a block loses the role, the incoming TAA can still clear it via the second disjunct. No new gap found.

## Critical

### Finding 1 — The `DigestKey` rotation rule now stated in AD-22/ARCH-A-12 is coherent as a sentence but is practically unsatisfiable for any operationally active tenant, defeating the purpose of having a rotation control

**Quoted text (AD-22, `ARCHITECTURE-SPINE.md` line 257):**

> The per-tenant `DigestKey` that keys every sensitive-content digest under AD-29 is never rotated for a tenant holding any unerased protected content, since rotation would silently break every stored digest's comparability against content it must keep matching; rotation is permitted only after full-tenant erasure or an accepted-risk waiver, and is itself a justified, audited lock-bearing operation under `EXT-SECRETS-1` [ASSUMPTION ARCH-A-12].

**Quoted text (`ARCH-A-12`, line 782):**

> The per-tenant `DigestKey` is never rotated while the tenant holds unerased protected content; rotation is permitted only after full-tenant erasure or an accepted-risk waiver.

**Why this fails the stated goal:** AD-22's own retention rule is *"Sensitive Agent content is retained 365 days after the AD-28 terminal instant unless legal hold suspends expiry."* For any tenant that is not permanently idle, every new `AgentInteraction` creates fresh `ProtectedContent` that will remain "unerased" for up to 365 days (longer under hold). The rotation gate is keyed on "holding **any** unerased protected content" — not "content older than N days" or "content in a specific class" — so for an operationally active tenant this condition is true essentially continuously: as soon as one interaction's content ages out and is erased, the tenant has almost certainly already produced newer content that is still within its retention window. "Full-tenant erasure" — the one non-waiver path to rotation — therefore requires the tenant to have **zero** unerased protected content simultaneously, which for a live, in-use tenant means either (a) the tenant has stopped calling `hexa` entirely and waited out the full 365-day retention tail with no new calls, or (b) the tenant deletes its entire Agent interaction history (a `DeletionRequest` covering every in-range interaction) purely to enable rotation. Neither is a real operational path for a tenant that wants to keep using the product.

**Concrete failure scenario:** A tenant's `DigestKey` is suspected compromised (e.g., the `EXT-SECRETS-1` custodian reports anomalous access, or a security incident implicates the key material). Standard incident response calls for immediate key rotation. Under this rule, rotation is available only via: (1) destroy every one of the tenant's Agent interactions (including live, non-expired audit evidence, safety-decision history, and anything under a `LegalHold` — which AD-22 elsewhere says a hold-protected interaction cannot be deleted at all, since "a class-scoped deletion ... is rejected in full if any is pinned," so full-tenant erasure is not even always achievable), or (2) formally accept the compromise risk via the waiver path. The architecture thus offers no way to rotate a suspected-compromised key while preserving the tenant's live, retained, unheld audit trail — the exact scenario a rotation control exists to handle. The "fix" turns "no stated key-lifecycle bound" (v6's complaint) into "a stated bound that functions as a permanent no-rotation policy in the only case (an active, compliant tenant) where rotation would ever actually be needed," which is a materially worse and more concrete gap than the one it replaced, not a resolution of it.

**Compounding issue — the rule is also over-broad relative to what it protects.** AD-22's justification is that "rotation would silently break every stored digest's comparability against content it must keep matching." But not every digest under the `DigestKey` needs multi-year comparability: AD-20's verdict-cache key is explicitly "invalidated on policy publication" (already short-lived by design), and AD-13's Provider-attempt fingerprint only needs to match for the duration of one attempt's retry window (minutes, per AD-5/AD-13), not 365 days. Only the durable audit-evidence fingerprints and per-message digests that must remain comparable against retained content for the full retention/hold period actually require the "never rotate while unerased content exists" guarantee. The current rule applies the same blanket ban to all four classes named in AD-29 ("per-message digests, verdict-cache keys, evidence fingerprints, command-payload fingerprints"), which is stricter than necessary and makes the practical-impossibility problem above worse than it needs to be.

**Fix:** Introduce a versioned key ring rather than a single per-tenant scalar `DigestKey`: tag each stored digest with the `DigestKeyVersion` it was computed under, keep retired key versions available read-only (verification-only, never for new digests) through `EXT-SECRETS-1` for as long as any digest computed under them remains within its retention/hold window, and let "current `DigestKey`" rotate freely and immediately (including under incident response) without waiting for full-tenant erasure. Restrict any blanket "never rotate" language to the specific digest classes that actually require long-lived comparability (durable evidence/per-message digests), not to short-lived ones (verdict-cache keys, Provider-attempt fingerprints, command-payload fingerprints) that are already invalidated or expire well within days. This also removes the current tension with `LegalHold`, where full-tenant erasure — the rule's only non-waiver rotation path — is definitionally blocked by any pinned interaction.

## Medium

### Finding 2 — AD-12's dual-cause `PausedDuration` (kill switch + disabled Agent) has no stated overlap/merge rule, leaving the audited `PostingFailed` retry deadline non-deterministic when both conditions co-occur

**Quoted text (AD-12, line 189):**

> `PostingFailed` automatic and administrative retries are suspended and their retry-window clock is paused — the affected proposal accumulates `PausedDuration` recorded at pull and updated at release — on the same terms as the disabled-Agent suspension above; **the two causes accrue against the same `PausedDuration` field**, and AD-5's deadline is `origin + window + PausedDuration` under AD-28.

And, for the disabled-Agent cause specifically: *"a `PostingFailed` automatic or administrative retry is suspended with its retry-window clock paused while the Agent is disabled, **resuming on re-enablement**"* (same AD).

**The gap:** the spine says both causes write into the *same* `PausedDuration` field but never states how to combine two independently-tracked pause intervals that can overlap (kill switch pulled while the Agent is also disabled, a realistic combination since both are independent per-tenant/per-Agent facts). Two different literal implementations of "resuming on re-enablement" and "updated at release" are both consistent with this text and produce different, audit-relevant numbers:
- If overlapping intervals are summed independently (naively adding the disabled-Agent pause duration and the kill-switch pause duration without deduplicating the overlap), the tenant is credited *more* paused time than the wall-clock period during which retries were actually blocked, silently extending the audited 15-minute/3-attempt bound AD-5 calls "bounded."
- If "resuming on re-enablement" is implemented literally (clock un-pauses the instant the Agent is re-enabled, without checking whether the kill switch is still pulled), the retry-window clock resumes ticking while the proposal is *still* blocked from retrying by the still-pulled kill switch (which independently suspends the same retries). The tenant's genuine remaining retry opportunity is silently consumed by a period in which no retry could have succeeded, so by the time the kill switch is finally released the 15-minute deadline may already have elapsed with zero of the three attempts used.

Neither outcome is caught by a fresh-read authorization gate (the actual retry attempt, when it runs, still correctly re-checks kill-switch and Agent-disabled state and would be correctly blocked or allowed) — so this is not an authorization bypass — but it does mean the *audited deadline itself*, which is the thing AD-5 calls a bound and which the register's `AuditInspection`/`audit-evidence` machinery would need to reconstruct after the fact, is implementation-dependent and not derivable from the spine text alone. That is a data-integrity/audit-completeness gap: two compliant implementations can legitimately disagree about whether a given `PostingFailed` proposal's retry deadline has passed.

**Fix:** State the merge rule explicitly — e.g., "`PausedDuration` is the wall-clock measure of the union of the two intervals (Agent-disabled, kill-switch-pulled), not their sum; a retry-window clock resumes only when *both* conditions have cleared, not when either individually clears."

## Low

None beyond what is already tracked as open in prior rounds (the v6 Low Finding on `[ASSUMPTION A-19]` tagging was not in this round's scope and was not re-checked here).

## Summary table

| # | Severity | Finding | ADs / files |
| --- | --- | --- | --- |
| 1 | Critical | `DigestKey` rotation rule (AD-22/`ARCH-A-12`) is now stated but is practically unsatisfiable for any operationally active tenant — the only non-waiver rotation path (full-tenant erasure) is unavailable while any content is retained or held, which for a live tenant is effectively always. Defeats incident-response key rotation; also over-broad across digest classes with different comparability lifetimes. | AD-22, AD-29, ARCH-A-12 |
| 2 | Medium | AD-12's `PausedDuration` field is fed by two independent pause causes (kill switch, disabled Agent) with no stated overlap/merge rule, making the audited `PostingFailed` retry deadline implementation-dependent | AD-5, AD-12, ARCH-A-11 |
| — | Verified fixed | AD-22/`ARCH-A-10` register symmetry (`LegalHold` + `DeletionRequest`) — register text confirmed to actually say this | AD-22, register line 108 |
| — | Verified fixed | Provider-attempt fingerprint HMAC-SHA-256 keying, consistent across AD-13/AD-29/Consistency Conventions table | AD-13, AD-29, line 472 |
| — | Verified no new gap | AD-30 Platform freshness-source rewording (resolves v6 High Finding 2) | AD-30 |
| — | Verified no new gap | AD-12 disabled-Agent suspension does not bypass fresh-read authorization at actual retry time | AD-12 |
| — | Verified no new gap | AD-7 block-clearing authority: TAA's own block cannot become unclearable after a TAA role handover (current-TAA disjunct always available) | AD-7 |
