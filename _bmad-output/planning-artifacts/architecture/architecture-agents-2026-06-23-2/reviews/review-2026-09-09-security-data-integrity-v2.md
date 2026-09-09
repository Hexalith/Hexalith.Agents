# Security / Data-Integrity Review (v2) — Hexalith Agents Architecture Spine

**Reviewer lens:** security / data-integrity (PII, secrets, content protection, tenant isolation, authorization/trust boundaries, audit/compliance)
**Target:** `ARCHITECTURE-SPINE.md`, updated 2026-09-09
**Date:** 2026-09-09
**Method:** Full read of all 31 ADs, the Consistency Conventions table, Stack, Structural Seed, class diagram, Capability map, External Prerequisites, Architecture Assumptions, and Deferred table. Read-only — no changes made.

---

## Overall verdict

The spine is unusually rigorous for this lens — tenant scoping, secret handling, and content protection are backed by real mechanisms (opaque handles, DEK/KEK envelopes, HMAC-tagged reserved principal extensions, closed `TenantScope` grammars) rather than aspirational language, and most rules are explicitly fail-closed. However, five concrete gaps survive a literal reading: an asymmetry between Agents' legal-hold/erasure guarantees and Conversations' independent retention of posted content, an audit-justification enforcement list that silently drops several lock-bearing families (including the kill switch and provider-catalog mutation), an unpinned freshness requirement for ordinary-user role claims versus the hardened `Administrator`/`Platform` principals, a PII no-duplication rule stated authoritatively in exactly one AD, and no guard against secret/content leakage through generic HTTP/SDK observability instrumentation around the Provider call. None of these are fatal, but each is the kind of gap a literal-minded builder could satisfy on paper while reintroducing the exact risk the AD was written to prevent.

---

## Finding 1 — HIGH — Legal hold / erasure does not bind the posted copy in Conversations

**AD(s):** AD-22 (Sensitive Audit Governance), cross-referenced by AD-6, AD-13.

**Quotes:**
- AD-22: *"Cryptographic erasure destroys the DEK at interaction granularity... A hold protects keys, not queries... EventStore history is never rewritten; **posted Conversation Messages remain governed by Conversations retention**."*
- AD-6: *"Agents reads context and posts final messages only through supported `Hexalith.Conversations.Client`/API boundaries... Agents never writes Conversation streams or events directly."*
- AD-22 (deletion propagation direction only): *"a Conversation deletion signal triggers the same deletion for derived content."*

**The gap:** Once a proposal is approved and posted, the approved plaintext is duplicated into a Conversations-owned message. AD-22 builds a complete, carefully engineered protection/erasure/legal-hold regime for the *Agents-side* copy (DEK per interaction, KEK per tenant, two-phase `ApplyHold` pinning DEKs, cryptographic erasure, class-scoped deletion rejected if any interaction is pinned). But by its own final sentence, that regime explicitly stops at the Conversations boundary: the posted copy is "governed by Conversations retention" — a policy this spine does not bind, does not reference by version, and does not require Conversations to honor an Agents-originated hold. The only cross-module propagation named runs in the opposite direction (Conversation deletion → Agents deletion), not (Agents legal hold → Conversation preservation) or (Agents legal hold → Conversation deletion blocked).

**Concrete failure scenario:** A Compliance Inspector places a `LegalHold` on an `AgentInteraction` under litigation. `ApplyHold` resolves scope, pins the DEK, and the aggregate reports `Active`; the `audit-evidence` projection shows the hold as effective. Meanwhile the *already-posted* Conversation Message containing the identical approved content is untouched by that hold — it lives entirely inside Conversations' own retention/purge cycle. If Conversations' retention window elapses (or a user/Facilitator deletes the conversation, which under AD-22's deletion-propagation rule would trigger deletion of the *Agents* copy, compounding the problem) before the litigation hold is released, the actually-disclosed record the parties read is gone, while Agents' own audit trail still asserts the content was "held." A builder can implement AD-22 to the letter — DEK pinning, two-phase activation, rejection-if-pinned on deletion — and still ship a legal-hold feature that does not reliably preserve the one artifact (the posted message) a litigation hold usually exists to preserve.

**Why it matters for this lens:** this is exactly the "audit trail that is presumably compliance-relevant" risk called out in the brief — a hold that is cryptographically sound on one side of a module boundary and silently absent on the other is worse than no hold, because the evidence-of-effectiveness (the `Active` state, the `audit-evidence` projection) actively misleads a compliance reviewer.

**Recommendation direction (not prescriptive):** AD-22 (or AD-6) should state explicitly whether `LegalHold` is required to call a Conversations-owned hold/preservation seam for any posted message derived from a held interaction, or explicitly document the residual risk and require it to be surfaced as a named blocker in `launch-readiness-register.md` rather than left as an implicit consequence of one sentence.

---

## Finding 2 — MEDIUM-HIGH — Audit-justification enforcement doesn't cover all lock-bearing families

**AD(s):** AD-22, cross-referenced against AD-12's lock-bearing family list.

**Quotes:**
- AD-12 defines the lock-bearing family set: *"`ProposalResolution`, `PolicyPublication`, `TenantBudgetUpdate`, `LegalHold` (also `LegalHoldRelease`), `ExportRequest` (also `ExportDownload`), `DeletionRequest`, `ProviderCatalogMutation` (also `TenantProviderEnablement`), `AgentSetupMutation` (also `TenantKillSwitch`), and `AgentActivation`"* — nine families.
- AD-22, general clause: *"Every lock-bearing-family command appends a change-evidence event carrying actor and role basis, family, resource identity, old-to-new values at the FR-20 disclosure level, published or pricing version, expected revision, and justification, and the set rendered in the confirmation is exactly the set recorded."*
- AD-22, enforcement clause (narrower): *"`LegalHold`, `LegalHoldRelease`, `ExportRequest`, `DeletionRequest`, `PolicyPublication`, and `TenantBudgetUpdate` reject a missing or whitespace justification before append."*

**The gap:** The general clause promises every lock-bearing command's change-evidence event carries "justification." The enforcement clause that actually makes justification non-optional (reject on missing/whitespace) names only 6 of the 9 lock-bearing families. `ProposalResolution` (administrative retry / abandon), `ProviderCatalogMutation`/`TenantProviderEnablement`, `AgentSetupMutation`/`TenantKillSwitch`, and `AgentActivation` are left without a stated rejection rule. AD-5 separately says the administrative retry/abandon "is one audited attempt per command" (implying *some* audit capture) but never says a blank justification is rejected there either.

**Concrete failure scenario:** A builder ships `TenantKillSwitch` (part of `AgentSetupMutation`) and `ProviderCatalogMutation` faithfully per AD-22's general clause — the change-evidence event has a `Justification` field — but, because the enforcement clause never lists these two families, does not add validation to reject an empty string. The Platform Operator pulls the kill switch tenant-wide (a `TenantKillSwitch` event, one of the two most consequential single actions in the whole system alongside `DeletionRequest`) with justification `""`. This satisfies the AD's literal wording (the field exists, the event was appended, "the set rendered in the confirmation is exactly the set recorded") while producing an audit trail that cannot answer "why was this tenant stopped" during an incident postmortem or a regulator's inquiry — precisely the compliance-relevant gap this AD exists to close, for precisely the operations (kill switch, catalog mutation) most likely to be scrutinized after an incident.

**Recommendation direction:** Either state that the reject-on-missing-justification rule applies to *all nine* lock-bearing families (and the six-item list is illustrative, not exhaustive), or explain why `ProposalResolution`, `ProviderCatalogMutation`/`TenantProviderEnablement`, `AgentSetupMutation`/`TenantKillSwitch`, and `AgentActivation` are deliberately exempt.

---

## Finding 3 — MEDIUM — Role-claim freshness is hardened for `Administrator`/`Platform` but left unpinned for ordinary `User` principals

**AD(s):** AD-30 (Principals And Trusted Envelope), AD-12 (Authorization, Dependency Uncertainty, And Emergency Stop).

**Quotes:**
- AD-30: *"`Administrator` (a reserved `actor:*` extension populated only by the Agents API ingress after a fresh Tenants-projection role check, **never from JWT roles alone**)"*.
- AD-30: *"`User` (`TenantId`, `PartyId`, resolved FR-33 roles)"* — no freshness or source qualifier given, unlike the `Administrator` definition two clauses later in the same sentence.
- AD-12: *"Every side-effecting step (Provider invocation, proposal creation, edit, regeneration, approval, posting) re-reads Agent lifecycle, tenant enablement, the per-Conversation block, and the per-tenant kill switch"* — role rights are conspicuously absent from this explicit "every side-effecting step" re-read list; AD-12 elsewhere says only *"role rights from the FR-33 matrix carried by the AD-30 principal"* (i.e., asserted once, in the envelope, not re-read).

**The gap:** AD-30 goes out of its way to forbid JWT-role-only trust for the `Administrator` and `Platform` principal kinds, with an explicit "never from JWT roles alone" and "fresh... check." The ordinary `User` kind — which is what most callers, including anyone whose FR-33 role gates a UI action, actually carry — gets no equivalent guarantee in the text. "Resolved FR-33 roles" does not say resolved *when*, *from what source*, or *how fresh*. Given the prevention clause for AD-12 is explicitly "JWT-only or UI-only authorization," and the AD text draws a real distinction in rigor between principal kinds, this reads as an intentional hardening of two kinds and a gap for the third — dynamic per-command re-verification is explicitly promised for tenant/kill-switch/block/lifecycle state but not for role membership itself, except where a specific AD (e.g., AD-8's Eligible Approver predicate) separately re-derives authority dynamically from Conversations.

**Concrete failure scenario:** A builder mints the `User` principal's `resolved FR-33 roles` once at token issuance (or once per browser session) directly from a role claim embedded in the identity token, and does not re-check the Tenants-projection role assignment on each subsequent command. This satisfies the literal AD-30 text (the roles are indeed "resolved," just not necessarily *freshly per command*) and does not contradict any explicit AD-12 re-read obligation (roles aren't in that list). A Tenant Agent Administrator is off-boarded and their role revoked in the Tenants projection mid-session; their already-issued session/token still carries the old role claim and can continue to submit role-gated commands (e.g., configuration changes gated by `Agents.Administrator`) until the token naturally expires — directly contradicting the "fail closed on... stale... dependency state" intent of AD-12, but not contradicting any single sentence of AD-30 as written.

**Recommendation direction:** Either state explicitly that `User.resolved FR-33 roles` must be (re-)resolved from the Tenants projection at ingress on every command — mirroring the `Administrator` language verbatim — or add "role assignment" to AD-12's explicit "every side-effecting step re-reads..." list.

---

## Finding 4 — MEDIUM — The "PartyId reference only, never PII" rule is authoritative in exactly one AD

**AD(s):** AD-7 (states the rule), versus AD-2, AD-8, AD-30 (carry Party-adjacent state without restating it).

**Quotes:**
- AD-7 (the only place the rule is stated): *"Agents stores stable `PartyId` references only; the shipped link and replace commands validate or provision identity through Parties adapters"* — listed under "Prevents: ...duplicated Party PII..."
- AD-2 (Agent aggregate fields): *"...a tenant-wide response mode, proposal expiry duration, regeneration ceiling, Conversation Context Policy, and **Party identity link**"* — no restatement that this link must never carry a display name, email, or other Parties-owned attribute.
- AD-8 (Approver Policy Resolution): *"ApproverPolicy is Agents-owned configuration with V1 sources Conversation Facilitator, **predefined `PartyId`s**, and tenant roles"* — again just an id reference, but nothing in AD-8 forbids an implementer from attaching a cached display name to make the "predefined approvers" admin screen usable without a live Parties lookup.
- AD-30: the `User` and `Workflow` principal shapes, and `OnBehalfOfPartyId`/`CallerPartyId`, are all bare `PartyId`s with no attached restriction language either.

**The gap:** the review brief specifically asks whether PII ownership is kept out of the Agents domain "across every AD that touches identity, not just the one AD that states the rule." It is not: AD-7 is the sole carrier of the constraint. Every other identity-bearing AD (AD-2's Party identity link, AD-8's predefined approvers, AD-30's principal shapes) is written in a way that is fully satisfied whether or not a denormalized Party attribute (name, email) is cached alongside the `PartyId` for UI convenience — none of them positively forbid it, they simply don't mention Party attributes either way.

**Concrete failure scenario:** The Admin UI needs to show a human-readable list of "predefined Approvers" for `ApproverPolicy` configuration (AD-8) and a "linked Party" for `Agent` identity (AD-2). A builder, reasonably wanting to avoid an extra Parties round-trip on every read of these frequently-displayed screens, adds a `DisplayName` (and maybe email, for an "invite" flow) column next to `PartyId` on the `Agent` and/or `ApproverPolicy` projection, refreshed opportunistically. This satisfies AD-2 and AD-8 exactly as written — neither says "PartyId and nothing else" — while violating the actual intent stated only in AD-7, and now Agents has duplicated Party PII into its own projections (and, per AD-14/AD-22, that PII is *not* content-class data, so it would not automatically fall under the `ProtectedContent` envelope, safety scanning, or the 365-day sensitive-content retention/erasure regime — it would live as an ordinary plaintext projection field with none of AD-22's protections).

**Recommendation direction:** Either restate the "PartyId reference only" constraint at every AD that carries Party-adjacent state (AD-2, AD-8, AD-30), or add one binding sentence to AD-7 (or the Consistency Conventions table's "Identity" row) making it a global, cross-referenced invariant that any Party-adjacent field anywhere in the Agents domain assembly or its projections is `PartyId`-typed only.

---

## Finding 5 — MEDIUM — No guard against secret/content leakage through generic Provider-call observability instrumentation

**AD(s):** AD-9 (Provider Adapter And Catalog Boundary), AD-14 (Sensitive Content And Secret Safety), Consistency Conventions "Observability" row.

**Quotes:**
- AD-9: *"secrets are resolved only inside the generation activity immediately before transport, held in memory for the request, and never enter events, workflow state, exports, or logs."*
- AD-14: *"Logs, telemetry, browser measurements, status, queue summaries, and audit summaries never include raw content, raw provider payloads, stack traces, Party PII, or secrets."*
- Observability convention: *"Logs, spans, and metric exemplars carry only `TenantId`, `AgentInteractionId`, `AttemptId`, and the trace id as identifying dimensions."*
- Stack table pins `OpenTelemetry 1.18.0` platform-wide; AD-16 places telemetry ownership with `EXT-HOST-1`.

**The gap:** every one of these rules is written about *domain-level* logs, telemetry, and events that the Agents module itself constructs deliberately. None of them speaks to the behavior of generic HTTP-client / SDK auto-instrumentation that will, by default in many OpenTelemetry HTTP instrumentation libraries and in some provider SDKs' own diagnostics, capture the outbound request's headers and/or body as span attributes or debug-log lines — exactly at the moment and in exactly the activity (the generation activity, immediately before transport, per AD-9 and AD-27) where the resolved secret is in memory and the raw prompt/context content is being sent. A platform-wide OpenTelemetry HTTP instrumentation package (pinned in the Stack table, owned by `EXT-HOST-1` per AD-16) applied uniformly across the platform would, unless specifically configured to suppress it for Provider calls, satisfy every quoted sentence above to the letter (none of those sentences govern instrumentation-library spans) while emitting the bearer token and/or the full prompt into the trace backend.

**Concrete failure scenario:** `EXT-HOST-1`'s platform-owned telemetry wiring enables default ASP.NET/`HttpClient` OpenTelemetry instrumentation for observability across all modules, including Agents' generation adapter. The generation activity resolves the provider secret (AD-9), builds the request with the secret in an `Authorization` header and the full authorized Conversation context in the body, and calls the Provider over `HttpClient`. The instrumentation library's default span enrichment (or a naive custom `DelegatingHandler` added for request/response debugging) records the header and/or body as span attributes. This is now sitting in the trace backend — outside every mechanism this spine defines (`ProtectedContent` envelope, AD-27 reference-only execution state, AD-9's "never enter... logs") because none of those mechanisms have jurisdiction over instrumentation-library spans, only over Agents' own domain artifacts.

**Recommendation direction:** Add an explicit sentence to AD-9 or AD-14 (or a new row in the Consistency Conventions table) requiring that any HTTP-client/SDK instrumentation wrapping the generation activity's outbound Provider call be configured to exclude headers and bodies from span/log capture, and that this exclusion is itself part of the AD-17 execution-state / observability test gate (the "execution-state content sweep" currently only names workflow/activity payloads, not instrumentation spans).

---

## Secondary observations (not top-tier findings, worth tracking)

- **Tenant-check-before-freshness ordering is implied, not stated.** AD-2's absolute rule ("a query for a key outside the caller's tenant... returns exactly the absent-key response") and AD-17's `Freshness`/`RevisionLag` computation (which accepts a caller-supplied `ExpectedRevision`) are never explicitly sequenced relative to each other. If an implementation computed `RevisionLag`/`Freshness` before the tenant-ownership check, a caller supplying a foreign tenant's stream key plus a guessed `ExpectedRevision` could distinguish "stale" from "not found" from "fresh" — a low-bandwidth existence oracle across tenants. Likely unintentional and easily closed by stating the tenant check strictly precedes any freshness computation, but the spine doesn't say so today.
- **EventStore tamper-evidence is asserted, not engineered, at this spine's altitude.** "EventStore history is never rewritten" (AD-22) and revision-serialized immutability are policy statements; the only cryptographic integrity artifact named anywhere in the spine is the export manifest's SHA-256/signature reference (AD-22). Whether the underlying event log itself is cryptographically tamper-evident (hash-chained, signed) versus merely access-controlled is presumably a property of the `Hexalith.EventStore` submodule and out of this spine's scope — but if that submodule's own architecture doesn't provide it either, "append-only" is an access-control promise, not a tamper-evidence one, for the entire audit trail this AD is built on.
- **`ProposalResolution`'s administrative retry/abandon is audited but not itself justification-gated even in the general clause's spirit** — see Finding 2; called out separately here because `ProposalResolution` uniquely lets a Tenant Agent Administrator bypass an Eligible Approver's decision entirely (abandoning a proposal without Conversation read access), which is a meaningful authority action even though it's less catastrophic than a kill-switch pull.

---

## What is done well (for calibration)

- AD-9's `SecretReference`/`ConfigurationReferenceId` design (opaque handle, never a vault URI, resolved only inside the generation activity, `SecretConfigured` as a platform-only-visible durable fact) is a genuine, enforceable mechanism, not a naming convention.
- AD-22's DEK-per-interaction/KEK-per-tenant envelope, `Erased` typed value for replay-safe folding after cryptographic erasure, and two-phase `ApplyHold` (pin-then-confirm) are real, well-thought-through mechanisms for the Agents-side content lifecycle.
- AD-2's blanket tenant-scoping rule plus AD-17's closed `TenantScope` grammar (explicitly rejecting `tenant:system`) show the authors already anticipated the "abuse the system tenant as a scope value" attack.
- AD-30's HMAC-tagged reserved `actor:*` extensions, ingress-side stripping of client-supplied reserved keys, and "never from JWT roles alone" language for `Administrator`/`Platform` are a strong, explicit anti-spoofing design — the gap in Finding 3 is that this rigor isn't extended to the `User` principal kind, not that it's absent from the spine.
