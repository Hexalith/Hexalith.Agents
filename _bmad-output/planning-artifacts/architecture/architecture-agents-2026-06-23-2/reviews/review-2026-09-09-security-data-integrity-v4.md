# Security / Data-Integrity Review (v4) — Hexalith Agents Architecture Spine

**Verdict: PASS WITH FINDINGS**

**Reviewer lens:** tenant isolation, sensitive-content handling, authorization/principal freshness, legal-hold/retention/erasure cross-module consistency, justification/audit-trail coverage, PII/Party-reference discipline.
**Target:** `ARCHITECTURE-SPINE.md` (`updated: 2026-09-09`), companion `launch-readiness-register.md`.
**Method:** Full independent read of all 31 ADs, both mermaid diagrams, the Consistency Conventions table, Stack, Structural Seed, class diagram, Capability map, External Prerequisites, Architecture Assumptions, and Deferred table — current text only, no assumption that prior rounds' claimed closures still hold. Cross-checked against `reviews/review-2026-09-09-security-data-integrity-v3.md` (which claimed 4 of 5 v2 findings CLOSED and 1 PARTIALLY CLOSED, plus one new MEDIUM finding) and against `launch-readiness-register.md`'s `LR-AUDIT-PROTECTION-DELETION` / `LR-TENANT-ACCESS` rows. Two v3 items are reconfirmed below (one re-verified CLOSED, one re-verified as a genuine, still-open gap); two new findings not raised in v2/v3 are added.

---

## Re-verification of prior-round claims (not re-litigated as new findings)

- **v3's new MEDIUM finding (AD-29 command-payload fingerprint keying ambiguity) is now CLOSED.** Current AD-29 text explicitly states: *"Because a command payload may itself carry sensitive content (e.g. `ProposalEdit`'s edited text), this fingerprint is computed as HMAC-SHA-256 under the per-tenant `DigestKey` per the sensitive-content-digest rule below, never the identity canonicalizer's unkeyed SHA-256."* This directly forecloses the unkeyed-SHA-256-dictionary-attack scenario v3 described. Verified against the current file — no residual ambiguity.
- **v3 Finding 1 (legal hold vs. Conversations retention) is reconfirmed PARTIALLY CLOSED, not CLOSED** — see Finding 2 below, which both reconfirms and sharpens this into a broader, previously-untracked gap (erasure, not just hold).
- **v3 Findings 2, 4, 5 (justification coverage, PartyId discipline, HTTP-instrumentation leakage) are reconfirmed CLOSED** against the current text — AD-22's nine-family justification list, AD-7's cross-referencing PartyId clause, and AD-14's instrumentation-exclusion sentence all read exactly as v3 quoted them, with no regression found.

---

## Critical

### Finding 1 — Erasure ("Restrictive completion") is claimed complete in AD-22 without ever addressing the posted copy in Conversations — and, unlike the LegalHold case, this gap is not even named as a tracked residual risk

**AD:** AD-22 (Sensitive Audit Governance).

**Quoted text:** *"Cryptographic erasure destroys the DEK at interaction granularity; redaction rewrites projection copies only and never completes an EventStore erasure; a class-scoped deletion enumerates the in-range interactions and is rejected in full if any is pinned... Restrictive completion means every protected event unprotects as unreadable, every named projection and the `workflow-execution-state` scope report purged, and only a support-safe non-content tombstone remains; deletion is not complete before that, and a Conversation deletion signal triggers the same deletion for derived content."*

Immediately afterward, in the same AD, the spine separately concedes: *"EventStore history is never rewritten; posted Conversation Messages remain governed by Conversations retention, independent of an Agents-side `LegalHold` — a hold that outlives Conversations' own retention window for the posted copy is a named residual risk... tracked inside `LR-AUDIT-PROTECTION-DELETION`'s evidence contract... [ASSUMPTION ARCH-A-10] until a Conversations-owned preservation seam is committed or Legal confirms the residual risk is accepted."*

**The gap:** the "posted content is independent of Agents" concession is written *only* in terms of `LegalHold` (preservation outlasting Conversations' retention). It is never extended to `ProtectedDeletion`/erasure (the opposite direction: an Agents-side erasure/right-to-be-forgotten request should cause the same content to stop being readable, but nothing propagates it into Conversations either). The "Restrictive completion" paragraph — the actual definition of what "deletion is complete" means — never mentions Conversations, never requires checking whether the erased content was ever posted, and never conditions completion on any Conversations-side action. AD-6 (Conversations Boundary) confirms Agents "never writes Conversation streams or events directly," so Agents has no mechanism to reach into Conversations and delete/redact a posted message even if it wanted to — yet AD-22 asserts an unqualified completion bar ("deletion is not complete before that") that a builder can satisfy in full while the exact same sensitive content (the approved proposal version that was posted verbatim as a Conversation Message under AD-5/AD-6) remains permanently, plaintext-readable to every current Conversation Participant, forever, under Conversations' own retention.

Cross-checked against the companion register: `LR-AUDIT-PROTECTION-DELETION`'s evidence description in `launch-readiness-register.md` (line 73) reads: *"Includes evidence that a `LegalHold` either propagates to (or has a confirmed accepted-risk waiver for) the posted copy of held content in Conversations... Invalidated by protection, retention, export, deletion, propagation-seam, or projection-inventory changes."* This evidence requirement — the only place in the whole spine+register pair that gates on the Conversations-boundary problem — is scoped to `LegalHold` by name. It says nothing about `DeletionRequest`/erasure needing equivalent evidence. So even the one safety net that makes the *hold* version of this gap a real, blocking, `RQ-1`-visible risk (per v3's Finding 1 analysis) does not exist for the *erasure* version.

**Concrete exploit/compliance-failure scenario:** A tenant's data subject exercises an erasure right. The Tenant Agent Administrator or Platform Operator submits `DeletionRequest`, gets Compliance Inspector approval, and the system executes: DEK destroyed, protected events unprotect as `Erased`, named projections purged, tombstone recorded. Every readiness/audit surface (`audit-evidence`, the deletion projection, `LR-AUDIT-PROTECTION-DELETION` evidence once it exists) reports the erasure as complete per AD-22's own definition. Meanwhile, the exact proposal text that was approved and posted under AD-5/AD-6 sits, unredacted, as an ordinary Conversation Message, readable by every current Participant, for as long as Conversations' own retention (which the spine never bounds — it could be indefinite) chooses to keep it. This is a false-completeness claim on a compliance-critical guarantee, not merely an unresolved edge case: nothing in the text stops a builder or an auditor from believing "erasure complete" means the content is actually gone.

**Why this is worse than the already-tracked LegalHold gap:** the LegalHold direction at least has a named `ARCH-A-10` assumption, a register-gated evidence requirement wired into essentially every governance-relevant `OperationGateMatrix` row, and an explicit "this is a residual risk, not silently absorbed" sentence. The erasure direction has none of that — it is a silent gap inside a paragraph that otherwise reads as an unconditional completeness guarantee.

**Fix:** Either (a) extend AD-22's "Restrictive completion" definition to explicitly state that a class or interaction whose content was ever posted to a Conversation is not — and by architecture cannot currently be — completely erased, and require that qualification to be surfaced in the deletion outcome/evidence (so "complete" never silently means "complete except the posted copy"); or (b) extend `ARCH-A-10` and the `LR-AUDIT-PROTECTION-DELETION` evidence clause to cover `DeletionRequest`/erasure symmetrically with `LegalHold` (a propagation seam or a confirmed accepted-risk waiver, evaluated the same way); most defensibly, do both — fix the false-completeness sentence in AD-22 itself, and make the register track the erasure direction as its own evidence sub-clause, not just the hold direction.

---

## High

### Finding 2 — The `Platform` principal (Platform Operator) has no stated role-freshness re-check, unlike `Administrator` and (as of this pass) `User` — despite carrying the widest cross-tenant powers of any principal kind

**AD:** AD-30 (Principals And Trusted Envelope).

**Quoted text (current):** *"Every Agents command envelope carries exactly one principal: `User` (`TenantId`, `PartyId`, resolved FR-33 roles re-read from the Tenants-projection role assignment at every command ingress, mirroring the `Administrator` freshness rule below — never trusted from a cached or token-embedded role claim alone), `Administrator` (a reserved `actor:*` extension populated only by the Agents API ingress after a fresh Tenants-projection role check, never from JWT roles alone), `Platform` (the `system`-tenant Platform Operator, carried as the reserved `actor:agentsProviderAdmin` extension), or `Workflow` (...)."*

**The gap:** `User` now explicitly requires role re-read "at every command ingress ... never trusted from a cached or token-embedded role claim alone" (this pass's fix, confirmed against `git log -p` on the spine file: the pre-fix text for `User` was bare — *"resolved FR-33 roles"* — with no freshness language at all). `Administrator` has always carried its own explicit freshness clause ("after a fresh Tenants-projection role check, never from JWT roles alone"). `Platform`, by contrast, has *never* carried any freshness language in any committed version of this spine (confirmed via `git log -p --follow` on `ARCHITECTURE-SPINE.md`: the `Platform` clause is textually identical — "carried as the reserved `actor:agentsProviderAdmin` extension" — going back through every prior commit). The only protection `Platform` gets from the general AD-30 machinery is anti-*forgery*: *"Every reserved extension carries an HMAC tag issued to the Agents Server principal through `EXT-SECRETS-1` and verified by the Server command pipeline before the aggregate, which also rejects an untagged extension."* An HMAC tag proves the extension was legitimately issued by the ingress at some point; it says nothing about whether the underlying human still holds the Platform Operator role *now*. Nothing in AD-30, AD-12, or the register (`LR-TENANT-ACCESS`'s freshness contract is scoped to "tenant projection ... role mapping changes," and Platform Operator is explicitly a `system`-tenant, cross-tenant role, not a tenant-scoped one) states where the Platform Operator role assignment is authoritatively sourced from, or how often the ingress re-checks it before minting the `actor:agentsProviderAdmin` extension.

**Why this matters more than the User/Administrator cases already fixed:** `Platform` is by a wide margin the most powerful principal kind in the system. Per AD-30 itself, `Platform` may dispatch `ProviderCatalogMutation`, platform `PolicyPublication` (both in tenant `system`, affecting every tenant's catalog view), and in any *named target tenant*: `TenantProviderEnablement`, `TenantBudgetUpdate` including cap override, `TenantKillSwitch`, `DeletionRequest` as requester, and the create-only `AgentSetupMutation`. Per AD-12, the Platform Operator is also the role authorized to pull the tenant kill switch "immediately... on any confirmed cross-tenant or unauthorized action" for *any* tenant. A revoked or off-boarded Platform Operator whose session or reserved-extension issuance is not re-validated against a current, authoritative role source on every command retains, for as long as their session/token lives, the ability to mutate the platform provider catalog, override any tenant's budget caps, pull or release any tenant's kill switch, and request deletion in any tenant — i.e., exactly the class of "confirmed cross-tenant or unauthorized action" AD-12 names as the trigger for using this very role to shut things down.

**Fix:** Extend AD-30's `Platform` clause with the same freshness discipline given to `Administrator`/`User`: name the authoritative source for the Platform Operator role assignment (presumably a platform-level identity/role registry, since it is not tenant-scoped and therefore not literally "the Tenants-projection"), and require the ingress to re-check it fresh at every command, "never from a cached or token-embedded role claim alone," mirroring the other two principal kinds' language exactly.

---

## Medium

### Finding 3 — Legal hold vs. Conversations retention: re-verified as genuinely still open (PARTIALLY CLOSED), current text unchanged from the state v3 assessed

**AD:** AD-22, assumption `ARCH-A-10`.

This is the narrower (hold-only) half of Finding 1 above, re-verified independently against the current spine text rather than assumed closed. The text is byte-for-byte the same as what v3 quoted: *"EventStore history is never rewritten; posted Conversation Messages remain governed by Conversations retention, independent of an Agents-side `LegalHold` — a hold that outlives Conversations' own retention window for the posted copy is a named residual risk, not a silently absorbed one..."* This is a real, honestly-disclosed, register-gated residual risk (the `LR-AUDIT-PROTECTION-DELETION` row is `InsufficientEvidence` today, which blocks `RQ-1`), so it is rated Medium rather than Critical/High here — it is a *process* commitment with teeth (a blocking gate), not a silent gap. It is listed separately from Finding 1 only because Finding 1 (the erasure direction) has no equivalent tracking at all and is materially worse. No engineering mechanism (seam, activity, or command) exists anywhere in the spine that would let an Agents-side `LegalHold` actually reach into Conversations and preserve the posted message; the exit is either "build the seam" or "Legal accepts the risk," and neither has happened yet.

**Fix:** unchanged from v3's recommendation — commit to one of the two `ARCH-A-10` exit conditions before `RQ-1`, or accept that release proceeds with this named as an accepted residual risk rather than a closed one.

### Finding 4 — `AuditInspection` permits content access before authorization is confirmed, with an undefined review window and no stated consequence for a missed or failed post-hoc review

**AD:** AD-22.

**Quoted text:** *"unposted versions, rejected content, and context metadata are inspectable only by a Party durably recorded as an Eligible Approver for that proposal or under a compliance inspection owned by `AuditInspection`, held by the Compliance Inspector, scoped to a named Conversation or case, justified, either pre-approved by a distinct Tenant Agent Administrator principal before any content read or post-hoc reviewed by one within the tenant's review window, rate-visible on the `audit-evidence` projection..."*

**The gap:** this clause defines two legitimate modes — pre-approved, or post-hoc reviewed "within the tenant's review window." The second mode means a Compliance Inspector can read unposted proposal versions, rejected (safety-failed) content, and context metadata — all explicitly "sensitive conversation-derived content" under AD-14 — *before* any other principal has confirmed the access was justified. Two things are left unstated: (1) "the tenant's review window" is never defined anywhere else in the spine (it is not one of the `TenantGovernancePolicy` fields enumerated in AD-2/AD-21, nor bound by the register in the excerpts checked), so its length, and therefore how long content can sit read-but-unreviewed, is unbounded by this document; (2) there is no stated consequence if the post-hoc review never happens, happens late, or the reviewing Tenant Agent Administrator declines to ratify the access — AD-22 says the inspection is "rate-visible" and "recorded as evidence," but not what changes (revocation, escalation, a `SecurityEventLog` entry, a compliance flag) if post-hoc review fails. This is the one place in the spine's authorization model where "read first, authorize later" is the designed behavior rather than the fail-closed default AD-12 states everywhere else ("Authorization gates run before every side effect and fail closed on missing, stale, ambiguous, disabled, or unavailable dependency state").

**Concrete scenario:** A Compliance Inspector opens an inspection citing a plausible case reference, reads unposted/rejected content and context metadata for a Conversation, and the designated Tenant Agent Administrator never gets around to the post-hoc review (no window bound is defined, no escalation is defined). Per the spine as written, nothing marks this access as ever having failed authorization — the read already happened, is "recorded as evidence," and no rule says the access itself becomes retroactively invalid or flagged.

**Fix:** bind "the tenant's review window" to an explicit, bounded field (e.g., alongside the other `TenantGovernancePolicy` numeric fields), and state what happens on a missed or negative post-hoc review — at minimum, an audited `SecurityEventLog` entry and a way for the `audit-evidence`/readiness surfaces to distinguish "reviewed and ratified" from "pending review" from "review window elapsed with no ratification," so a builder cannot implement this literally as "read now, and nothing enforces the review ever completing."

---

## Low

None rising to a distinct, evidence-backed finding beyond the Medium items above; the spine's tenant-scoping (AD-2), PartyId-reference discipline (AD-7, reconfirmed closed), and justification coverage (AD-22, reconfirmed closed) held up well under an independent re-read and are not repeated here as findings.

---

## Summary table

| # | Severity | Finding | AD(s) |
| - | -------- | ------- | ----- |
| 1 | Critical | Erasure ("Restrictive completion") claims completeness without addressing or even tracking the posted copy in Conversations | AD-22 |
| 2 | High | `Platform` principal has no stated role-freshness re-check, unlike `Administrator`/`User` | AD-30 |
| 3 | Medium | Legal hold vs. Conversations retention — re-verified still open, honestly tracked as residual risk | AD-22, ARCH-A-10 |
| 4 | Medium | `AuditInspection` allows content access before authorization confirmation; undefined review window, no defined failure path | AD-22 |
