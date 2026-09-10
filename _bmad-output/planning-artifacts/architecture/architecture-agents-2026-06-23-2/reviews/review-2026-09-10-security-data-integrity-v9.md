---
name: Hexalith Agents
type: architecture-spine-review
target: architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md (cross-referenced against prd-agents-2026-06-23/prd.md and VALIDATION-REPORT-2026-09-09-4.md, round 5)
date: 2026-09-10
lens: security/data-integrity v9 (ad hoc)
---

# Security / Data-Integrity Review (v9) — Hexalith Agents Architecture Spine

**Verdict: PASS WITH FINDINGS.** The round's headline claim — that the versioned `DigestKey` rotation model now binds every persisted digest to the `DigestKeyVersion` it was computed under, closing both the false-409 idempotency-conflict risk and the unbounded audit re-verification risk the round-5 report (Critical, security lens v8) identified — **holds at the level of AD-29, the identity/idempotency authority.** AD-29's rewritten idempotency-record rule and its closing blanket digest rule both explicitly recompute against the *recorded* `DigestKeyVersion`, never the tenant's current one, and both concrete failure scenarios from the v8 report (idempotency false-conflict on a rotation-spanning retry; unbounded audit re-verification after erasure) are textually closed by that rule.

However, the edit was not carried through consistently to every place that describes the same facts. AD-22 still contains an un-updated sentence that flatly contradicts AD-13 and AD-29 about whether the `ProviderAttempt` fingerprint needs version tracking at all — a direct, load-bearing self-contradiction in the very paragraph the round edited to fix this problem. Two smaller version-binding omissions accompany it in the diagrams/tables that are supposed to mirror the prose. None of these break the *mechanism* AD-13/AD-29 describe, but they are exactly the kind of "one location left un-updated" defect this round was asked to hunt for, and in a spine that has already shown a pattern of prose-vs-prose contradictions (AD-30's `OnBehalfOfPartyId`, AD-5 vs FR-18, in prior rounds), an unresolved contradiction about which digests are rotation-safe is a genuine risk to get built wrong.

The FR-34 attestation rewrite and the AD-22 aggregation bound are both faithful to the PRD and internally consistent — no findings there.

## 1. DigestKeyVersion re-derivation

### 1a. Scenario A (false-409 on rotation-spanning idempotent retry) — CLOSED

AD-29 (line 303): "The idempotency record lives on the target aggregate's stream and carries its fingerprint's `DigestKeyVersion` alongside it; an exact replay (same tuple, and a fingerprint that matches when recomputed under the record's own `DigestKeyVersion`) returns the original authoritative identity and projection version, the same tuple whose fingerprint differs under that recorded version is a conflict... a rotation between submissions never manufactures a false conflict, because the comparison never recomputes under the tenant's current key."

This is unambiguous and directly answers the v8 scenario: a byte-identical retry after a rotation recomputes against the *recorded* version, not the current one, so it cannot spuriously diverge. **Verified fixed.**

### 1b. Scenario B (unbounded audit re-verification after erasure) — CLOSED

AD-29's closing blanket rule: "Every digest of sensitive content stored outside a `ProtectedContent` envelope (per-message digests, verdict-cache keys, evidence fingerprints, command-payload fingerprints, and the AD-13 `ProviderAttempt` fingerprint)... is persisted together with the `DigestKeyVersion` it was computed under, alongside the digest value, wherever it is stored — the idempotency record below, the `ProviderAttempt` fingerprint, and every per-message digest inside `audit-evidence`; re-verification or an idempotency-conflict comparison always recomputes against the recorded `DigestKeyVersion`, never the tenant's current one, so a comparison is never ambiguous after a rotation."

Per-message digests inside `audit-evidence` are explicitly named as carrying `DigestKeyVersion` and being recomputed-against-recorded-version on re-verification. A Compliance Inspector or legal-hold reviewer reconstructing an inspection years and several rotations later now has a deterministic, bounded (not brute-force-every-version) recheck procedure. **Verified fixed.**

### 1c. Cross-location consistency — NOT fully consistent (Critical)

See Critical Finding 1 below: AD-22 still asserts the opposite of AD-13/AD-29 for the `ProviderAttempt` fingerprint specifically.

### 1d. Full sweep of "digest"/"fingerprint"/"HMAC" mentions

Every occurrence in the file was checked (AD-13 line 201, AD-20 verdict-cache line 249, AD-22 line 261, AD-27 line 291, AD-29 line 303, AD-30 line 309 [`SecurityEventLog` denial event, content-free, not a content digest], the mermaid sequence diagram line 403 [`reference + digest + freshness`, a Conversation-context digest returned by an activity — not itself described as persisted long-term outside the fingerprint it feeds], the Consistency Conventions table line 476, the mermaid class diagram line 645, and `ARCH-A-12` line 786). No *additional* persisted-digest location beyond the ones AD-29's blanket rule already lists was found to be missing version-binding language in prose. The gaps found are internal-consistency defects among the locations the round did touch (below), not an undiscovered sixth location.

## Critical

### Finding 1 — AD-22 still classifies the `ProviderAttempt` fingerprint as a "short-lived digest... with no rotation risk," directly contradicting AD-13's and AD-29's now-explicit `DigestKeyVersion` persistence for that same fingerprint

**Quoted text (AD-22, line 261, unchanged by this round's edit):**

> A short-lived digest (a verdict-cache key, a `ProviderAttempt` fingerprint) is never compared across a rotation boundary and so carries no rotation risk; a long-lived digest embedded in an immutable audit or idempotency record carries its `DigestKeyVersion` (AD-29) alongside the digest value and keeps its originating version's comparability forever...

**Contradicted by AD-13 (line 201, edited this round):**

> The whole fingerprint, like every per-message digest it embeds, is computed as HMAC-SHA-256 under the per-tenant `DigestKey`... and is persisted on the `ProviderAttempt` record together with its `DigestKeyVersion` so a retry's fingerprint recheck always recomputes against the version the retained fingerprint was computed under.

**And by AD-29 (line 303, edited this round):**

> ...is persisted together with the `DigestKeyVersion` it was computed under, alongside the digest value, wherever it is stored — the idempotency record below, **the `ProviderAttempt` fingerprint**, and every per-message digest inside `audit-evidence`...

**Why this is a real contradiction, not a scoping nuance:** AD-22's sentence says the `ProviderAttempt` fingerprint is (a) short-lived, (b) never compared across a rotation boundary, and (c) therefore carries no `DigestKeyVersion`. AD-13 and AD-29 say the opposite of all three: the fingerprint is retained on the persisted `ProviderAttempt` record (part of the `AgentInteraction` stream, subject to the same 365-day/legal-hold retention as everything else — not transient), it *is* rechecked on a retry ("a retry's fingerprint recheck always recomputes against the version..."), and it *does* carry `DigestKeyVersion` specifically so that recheck survives a rotation between the original attempt and the retry. AD-13's own rationale for adding the field only makes sense if the fingerprint *can* be compared across a rotation boundary — which is exactly what AD-22 denies.

**Concrete failure scenario:** an implementer builds AD-22 first (or a Security reviewer audits the digest scheme against AD-22 as the governance-level summary) and concludes, correctly per AD-22's own words, that `ProviderAttempt.RequestFingerprint` needs no `DigestKeyVersion` field because it "carries no rotation risk." AD-13's field is then omitted. The next transport retry that spans a `DigestKey` rotation (rotation is now, per this round's own fix, "unrestricted and content-free" — `ARCH-A-12` — so it can happen at any time) recomputes the fingerprint under the tenant's *current* key with no recorded version to fall back on, sees a mismatch against the value captured at attempt time, and — per AD-13's own retry rule, "any change to the fingerprint... fails closed under that attempt id" — the retry is wrongly rejected. This is precisely the class of rotation-spanning-retry failure the round-5 Critical described, reintroduced by following the AD that appears to be the authoritative summary of the digest-safety rules.

**Fix:** delete "a `ProviderAttempt` fingerprint" from AD-22's short-lived/no-rotation-risk example list (leaving only the verdict-cache key, which is genuinely self-invalidating — see Finding 2), and either fold the `ProviderAttempt` case into the same sentence as the long-lived-digest example or cross-reference AD-13's paragraph directly, so AD-22 states the same rule as AD-13 and AD-29 rather than its predecessor.

## Medium

### Finding 2 — AD-29's blanket digest rule sweeps "verdict-cache keys" into the set of digests "persisted together with the `DigestKeyVersion`... wherever it is stored," but AD-20's actual verdict-cache definition carries no such field, and AD-22 separately (and correctly) calls the verdict-cache key short-lived with no rotation risk

**Quoted text (AD-29, line 303):** "Every digest of sensitive content stored outside a `ProtectedContent` envelope (per-message digests, **verdict-cache keys**, evidence fingerprints, command-payload fingerprints, and the AD-13 `ProviderAttempt` fingerprint)... is persisted together with the `DigestKeyVersion` it was computed under... wherever it is stored."

**Compare AD-20 (line 249), the only place the verdict cache's actual key/entry shape is defined:** "The verdict cache is keyed by (`TenantId`, `ConversationId`, keyed content digest, platform and tenant policy versions, adapter version) and invalidated on policy publication..." — no `DigestKeyVersion` component anywhere in that tuple.

**Compare AD-22 (line 261):** the verdict-cache key is named, correctly, as the one member of the "short-lived... never compared across a rotation boundary... no rotation risk" class that AD-22 gets right (see Finding 1 — the other member, the `ProviderAttempt` fingerprint, is wrongly classified there). A verdict-cache lookup after rotation naturally recomputes the "keyed content digest" component under the tenant's current key; if that digest doesn't match any cached entry's key, it's a cache miss (fail-safe: content is re-scanned), never a false match or an ambiguous comparison. It genuinely does not need a `DigestKeyVersion` field to be correct.

**The gap:** AD-29's blanket sentence literally includes verdict-cache keys in the "is persisted together with the `DigestKeyVersion`... wherever stored" set, but nothing else in the document — not AD-20's cache-key definition, not AD-22's own classification — agrees that the cache carries or needs that field. This is a second instance, in the same edited paragraph, of imprecise drafting about which digests actually need the new field: AD-22 wrongly excludes one thing that needs it (Finding 1), and AD-29 wrongly (or at best loosely) includes one thing that doesn't (this finding). Low practical risk on its own (adding an unnecessary field to a cache entry is not a correctness bug), but it signals the round's edit was not cross-checked sentence-by-sentence against the other two ADs it was meant to harmonize with.

**Fix:** narrow AD-29's parenthetical to the digests that are actually long-lived/persisted-for-comparison (per-message digests, evidence fingerprints, command-payload fingerprints, the `ProviderAttempt` fingerprint), or explicitly say the verdict-cache key is the one exception that needs no version tag because a rotation naturally produces a cache miss rather than an ambiguous comparison — matching AD-22's own (correct) treatment of that case.

### Finding 3 — The `ProviderAttempt` mermaid class diagram and the Consistency Conventions table row for "Provider attempt fingerprint" were not updated to show the new `DigestKeyVersion` field

**Quoted text (mermaid class diagram, lines 638–645):**

```
class ProviderAttempt {
  AttemptId
  AttemptOrdinal
  ReservationId
  AdmissionId
  AdmissionFence
  EffectiveProviderCapabilityVersion
  RequestFingerprint
}
```

No `DigestKeyVersion` field, despite AD-13's prose stating the fingerprint "is persisted on the `ProviderAttempt` record together with its `DigestKeyVersion`."

**Quoted text (Consistency Conventions table, line 476):** "Provider attempt fingerprint | One shared canonicalizer hashes the length-prefixed, stable-order prepared request with HMAC-SHA-256 under the per-tenant `DigestKey`... over the AD-13 field inventory; descriptors store the digest and safe scalar inputs, never raw context." — no mention of `DigestKeyVersion` either, unlike AD-13's own paragraph.

**Why this matters more than ordinary prose/diagram drift:** the class diagram is the artifact most likely to be transcribed directly into a C# record/event schema by whoever implements `ProviderAttempt`. Of the three places that describe this record's shape (AD-13 prose, the class diagram, the Consistency Conventions table), only one — AD-13's own paragraph — actually shows the new field. An implementer working from the diagram or the table (both plausible entry points, and both narrower/easier to scan than AD-13's dense prose) would ship the record without `DigestKeyVersion`, reproducing the same rotation-spanning-retry failure as Finding 1, by a different route.

**Fix:** add `DigestKeyVersion` to the `ProviderAttempt` class diagram entry and to the Consistency Conventions "Provider attempt fingerprint" row, matching AD-13's prose.

## AD-14 / FR-34 attestation rewrite — VERIFIED, no gap found

Re-derived independently against the PRD's FR-34 text (`prd.md`, §FR-34) rather than trusted. AD-14's rewritten paragraph (line 209) separates the two parts on the same terms as the PRD:

- **Identity (proof of protection):** "Security qualifies the production engine build, its signed identity and version are recorded in the launch readiness register and reach the runtime as an `EXT-SECRETS-1`-custodied value the Release Operator provisions, never a string the engine reports about itself; the runtime verifies the loaded engine's signed identity against that value on every check, pins it, and every seal and unseal call verifies the engine instance against the pin, failing closed... on a missing value, an unsigned engine, or a mismatch." This matches PRD FR-34's identity bullet almost clause-for-clause, including the "never a string the engine reports about itself" anti-self-report language and the per-seal/unseal-call pin verification.
- **Liveness (operating, not protecting):** "a canary protect/unprotect result confirming persisted bytes contain no plaintext, the DEK destroyed, and the field replaying as `Erased`; the canary alone is never proof of protection, only of liveness." Matches PRD FR-34's liveness bullet and its explicit "the canary is never a proof of protection" line.
- **Both required, pass-through-wrapper case named explicitly:** "The attestation passes only when both parts succeed, and refuses content-bearing activation or workflow progress on missing, stale, incompatible, no-op, or pass-through-wrapper evidence — this is exactly the pass-through-wrapper case the signed-identity pin exists to prevent, which a canary-only check cannot." This matches the PRD's own framing that a wrapper mimicking seal/unseal/`Erased` is caught "by signature, never by canary behavior" and that whether such a wrapper can pass at the *build* level is "a Security qualification test on the build, not a runtime test" (PRD FR-34, consequences bullet 4) — the spine does not claim to close that build-time question at runtime, and correctly doesn't try to.

**Is there a residual path where a pass-through wrapper around the no-op default could still pass?** Not one visible at this document's level of abstraction. The runtime check is anchored to a signed identity value that is (a) sourced from Security's qualification record via `EXT-SECRETS-1`, not from the running engine, and (b) verified against the *loaded engine instance* on every seal/unseal call, not just once at startup — so a wrapper would need to either forge the `EXT-SECRETS-1`-custodied value (out of scope for this spine's threat model; that's a secrets-custodian compromise, not an attestation-logic gap) or somehow present the genuine qualified engine's signed identity while substituting no-op behavior underneath the pin check (which both AD-14 and PRD FR-34 explicitly defer to the Security build-qualification process, not to a runtime signature check, and say so openly). No gap beyond what the PRD itself already scopes as out of the runtime check's reach. **No finding.**

## AD-22 compliance-inspection aggregation bound (5 Conversations / 30 days) — VERIFIED, exact fidelity to PRD

Spine (AD-22, line 261): "Post-hoc inspections by one Inspector that touch more than 5 distinct Conversations in a rolling 30-day window are one wide inspection: the sixth distinct Conversation requires prior Platform Operator approval [ASSUMPTION A-23], and the same inspection-rate threshold makes the Platform Operator's review mandatory at the next launch-health review; an Inspector with an unreviewed inspection cannot open another post-hoc inspection until it is reviewed or the Platform Operator records a disposition."

PRD (§FR-24, lines 628–629, and A-23 row line 936): "Post-hoc inspections by one Inspector that touch more than 5 distinct Conversations in a rolling 30-day window are one wide inspection: the sixth requires prior Platform Operator approval [ASSUMPTION A-23]. An Inspector with an unreviewed inspection cannot open another post-hoc inspection until it is reviewed or the Platform Operator records a disposition." / "The inspection rate carries a threshold [ASSUMPTION A-23] above which the Platform Operator's review is mandatory at the next launch-health review."

Every clause matches: the 5-Conversation/30-day window, "the sixth" trigger (spine adds "distinct Conversation" for clarity — a strict improvement, not a divergence), the launch-health-review escalation, and the unreviewed-inspection lockout. Both cite `[ASSUMPTION A-23]` for the same numeric threshold. **No finding.**

## Fresh sweep — other areas (no new findings; re-derived, not just re-read)

- **Tenant isolation:** AD-2's mandatory `TenantId` component, uniform absent-key response, and the single named carve-out (AD-10's `EntryMissing`/reason-coded `Blocked` for an Agent's own previously-snapshotted or selected Provider entry) are unchanged from the round this round didn't touch, and AD-17's tenant-check-before-freshness ordering guarantee still holds. No edge case found where a cross-tenant read could return anything other than the absent-key response or the one named carve-out.
- **Authorization fail-open paths:** AD-30's `User`/`Administrator`/`Platform` freshness rule ("never trusted from a cached or token-embedded role claim alone"), the reserved-extension HMAC-tag/ingress-strip discipline, and AD-12's "any decision whose wrong answer is unrecoverable... reads hold and policy state from the owning aggregate at an expected revision, never from a projection" rule were re-checked; every explicit failure mode named (missing, stale, unavailable, untagged, forged) resolves to a rejection, never a default-allow. No fail-open path found.
- **Logs/telemetry/audit-summary leakage:** AD-14's "Logs, telemetry, browser measurements, status, queue summaries, and audit summaries never include raw content, raw provider payloads, stack traces, Party PII, or secrets," the explicit HTTP-client/SDK header-and-body exclusion for the outbound Provider call, and `SecurityEventLog`'s "content-free" constraint (AD-30) were re-checked together; no path found where a digest, fingerprint, or protected field could substitute for raw content in a log/telemetry/summary surface (digests themselves are non-reversible HMAC outputs, not a leak vector for the underlying content).
- **DEK/KEK key hierarchy (AD-22):** per-`AgentInteraction` DEK wrapped by a per-tenant KEK, custodied through `EXT-SECRETS-1` with explicit `WrapDek`/`UnwrapDek`/`PinDek`/`UnpinDek`/`DestroyDek` operations and an irreversible destruction receipt; snapshots/replay caches "inherit the DEK so a restore cannot revive erased content." This is specified at a level consistent with the rest of the document's abstraction (operation names, not algorithms/parameters — same treatment as `EXT-PROTECTION-1` generally) and unchanged by this round. No new underspecification found beyond what prior rounds already left to `EXT-PROTECTION-1`'s own contract.
- **`SecurityEventLog` / `ARCH-A-9` unbounded-burst-volume assumption:** unchanged this round. Still an explicitly disclosed, tracked assumption ("`SecurityEventLog`'s daily `(TenantId, UtcDay)` partition is the only V1 mitigation against unbounded burst volume; no numeric rate bound is set," owned by Security, "Not Architecture-owned — no target here"). This is an honestly-disclosed open risk, not a silently-dropped one, and this round did not touch it — consistent with the round's own stated scope (the three digest paragraphs, the AD-14 attestation, and the AD-22 aggregation bound). Not re-raised as a new finding, but flagged here because it remains the one place in the document where "how much can an attacker or a misbehaving client actually write" has no numeric answer yet.

## Summary table

| # | Severity | Finding | Location(s) |
| --- | --- | --- | --- |
| 1 | Critical | AD-22 still says the `ProviderAttempt` fingerprint is short-lived and "carries no rotation risk," directly contradicting AD-13's and AD-29's explicit `DigestKeyVersion` persistence for that same fingerprint — following AD-22 as written would omit the field and reintroduce the rotation-spanning-retry failure this round set out to fix | AD-22 (line 261) vs AD-13 (line 201), AD-29 (line 303) |
| 2 | Medium | AD-29's blanket digest rule includes "verdict-cache keys" among digests that must carry `DigestKeyVersion` "wherever it is stored," but AD-20's actual verdict-cache key definition has no such field and AD-22 separately (correctly) treats the verdict-cache key as needing none | AD-29 (line 303) vs AD-20 (line 249), AD-22 (line 261) |
| 3 | Medium | The `ProviderAttempt` class diagram and the Consistency Conventions "Provider attempt fingerprint" row were not updated to show the new `DigestKeyVersion` field that AD-13's prose requires | Class diagram (lines 638–645), Consistency Conventions table (line 476), vs AD-13 (line 201) |
| — | Verified fixed | Scenario A (false-409 idempotency conflict on a rotation-spanning retry) | AD-29 (line 303) |
| — | Verified fixed | Scenario B (unbounded/unspecified audit re-verification after erasure) | AD-29 (line 303) |
| — | Verified fixed | AD-14/FR-34 attestation identity-vs-liveness separation, exact fidelity to PRD FR-34, no visible pass-through-wrapper runtime gap | AD-14 (line 209), PRD FR-34 |
| — | Verified fixed | AD-22 compliance-inspection aggregation bound (5 distinct Conversations / 30 days), exact fidelity to PRD FR-24 / A-23 | AD-22 (line 261), PRD lines 628–629, 936 |
| — | Verified no new gap | Tenant isolation, authorization fail-open paths, logs/telemetry/audit-summary leakage, DEK/KEK key hierarchy specification depth | AD-2, AD-10, AD-12, AD-14, AD-17, AD-22, AD-30 |
| — | Verified no new gap (pre-existing, disclosed) | `SecurityEventLog`/`ARCH-A-9` unbounded-burst-volume assumption — unchanged this round, still openly tracked, not silently dropped | `ARCH-A-9` |
