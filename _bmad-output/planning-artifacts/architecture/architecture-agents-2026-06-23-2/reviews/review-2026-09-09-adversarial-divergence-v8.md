---
title: Adversarial Divergence Review v8
target: ARCHITECTURE-SPINE.md (updated 2026-09-09, architecture_assumption_index_version 3)
role: Independent adversarial architecture reviewer
method: Construct two AD-compliant implementations that build incompatibly
date: 2026-09-09
---

# Adversarial Divergence Review v8

## Method note

This round does not take any prior round's "closed" claim on faith. For every citation of the
form "per AD-X" relied on below, AD-X's own live text was re-read and checked for whether it
actually delivers the cited guarantee, per the brief. Six prior rounds (v4, v6, v7 families) have
already closed a large number of genuine ambiguities — this round only reports what still
survives a hostile two-implementer reading of the current text.

## Verdict

**PASS WITH FINDINGS.** Two Critical-severity divergences remain live: one is a direct
self-contradiction inside AD-30's own paragraph (a citation that does not deliver what the citing
AD claims), the other is a genuine ownership gap (an entity implied by a reference field and by
AD-11's language, but absent from AD-2's closed aggregate enumeration). Both produce concretely
incompatible builds from spine-compliant teams. Two High findings and two lower-severity findings
round out the set.

---

## Finding 1 — CRITICAL — `OnBehalfOfPartyId` for a Workflow-dispatched regeneration attempt: AD-30 says two different things in the same paragraph

**Binds:** AD-30, AD-21, AD-13.

**The citation that doesn't deliver:** AD-21 states, as a load-bearing rule protecting per-Party
rate/cost fairness:

> "`AdmitCall`'s per-Party accounting is keyed by `OnBehalfOfPartyId` per attempt (AD-30) — the
> caller for the first generation, the requesting Approver for each regeneration — never by
> `CallerPartyId` for the interaction's whole lifetime."

This explicitly cites AD-30 as the source of the "OnBehalfOfPartyId varies per attempt" rule, and
explicitly warns against the reading "constant CallerPartyId for the whole lifetime." But AD-30's
own principal-envelope definition says exactly the opposite for the `Workflow` principal kind —
the principal kind that dispatches the `BudgetLedger` reserve command inside AD-13 steps (1)–(2)
for every attempt, including a regeneration's new attempt:

> "`Workflow` (`AgentInteractionId` as instance id, activity name, `CorrelationId`,
> `OnBehalfOfPartyId` **equal to the snapshot caller**)."

"The snapshot caller" is the AD-4 interaction snapshot's single, request-time-fixed `PartyId`
field ("caller `PartyId`") — a lifecycle constant. This is the literal "CallerPartyId for the
whole lifetime" reading AD-21 explicitly forbids. Three sentences later in the very same AD-30
paragraph, the text contradicts its own envelope clause:

> "`OnBehalfOfPartyId` is the Party whose command initiated the current step (the caller for the
> first generation, the requesting Approver for a regeneration) and is the Party charged by
> per-Party limits, with `CallerPartyId` carried separately."

AD-30 asserts a fixed value for `Workflow` and a step-varying value in the same breath, and never
reconciles which governs a Workflow-dispatched reservation command for a regeneration attempt.

**Two-unit incompatible-build scenario:** Both teams implement AD-13's numbered attempt flow via
Dapr Workflow activities dispatching `BudgetLedger`/`AgentInteraction` commands under the
`Workflow` principal, exactly as AD-18/AD-30 require.

- **Team A** implements AD-30's `Workflow`-principal envelope clause literally: every
  Workflow-dispatched command — including the `BudgetLedger` reserve for a regeneration's new
  attempt — carries `OnBehalfOfPartyId = CallerPartyId` (the interaction's original caller),
  constant for the interaction's whole lifetime. This is what AD-30's parenthetical says, word for
  word.
- **Team B** implements AD-21's rule (and AD-30's own later sentence) literally: the workflow is
  handed the regeneration-requesting Approver's `PartyId` as a step input, and the
  Workflow-dispatched reserve command for that attempt carries `OnBehalfOfPartyId` = that
  Approver.

Both teams can point to spine text that they followed to the letter. The resulting systems charge
rate limits and cost-cap consumption to **different Parties** for the same regeneration event —
Team A always attributes it to the original caller regardless of who triggered the regeneration,
Team B attributes it to the requesting Approver. This is exactly the kind of consequential,
externally observable divergence (who gets rate-limited, whose budget is debited, what the audit
trail names as "the Party charged") the brief asks to hunt for, and it sits inside a single AD's
own paragraph, not just across two ADs.

**Suggested amendment:** Delete or qualify the `Workflow` envelope's fixed
`OnBehalfOfPartyId equal to the snapshot caller` clause. Replace with: "`Workflow`
(`AgentInteractionId` as instance id, activity name, `CorrelationId`, `OnBehalfOfPartyId` as
supplied by the dispatching step per the per-step rule below — the caller for the first
generation, the requesting Approver for a regeneration)." Then have AD-21's citation point at the
corrected clause, and add one sentence stating explicitly that the Workflow principal's
`OnBehalfOfPartyId` is *not* a lifecycle constant.

---

## Finding 2 — CRITICAL — `ConversationContextPolicy`: a reference field with no aggregate to reference

**Binds:** AD-1, AD-2, AD-11, class diagram, Naming conventions.

AD-2's aggregate enumeration is written as a closed list ("V1 aggregates are **exactly**...") and
lists fourteen aggregates: `Agent`, `ProviderCatalog`, `TenantProviderEnablement`,
`AgentInteraction`, `BudgetLedger`, `TenantGovernancePolicy`, `ConversationAgentState`,
`AuditInspection`, `SecurityEventLog`, `ContentSafetyPolicy`, `LaunchReadinessGate`, `LegalHold`,
`AuditExport`, `ProtectedDeletion`. There is no `ConversationContextPolicy` aggregate in this list.

Yet AD-11 describes the Conversation Context Policy in aggregate-like, versioned-publication
terms directly parallel to `ContentSafetyPolicy` (which *does* get its own `system`-scoped
aggregate row in AD-2):

> "The versioned Conversation Context Policy is `Agent` configuration snapshotted as
> `ContextPolicyReference`; V1 publishes exactly one version declaring no Approved Bounded Context
> Behavior..."

"Publishes... version[s]" and a dedicated `*Reference` snapshot field is precisely the pattern
AD-2 uses for `ContentSafetyPolicy` (an independently versioned, `system`-scoped published
record, snapshotted onto `AgentInteraction` as a version number, never embedded inline). By
contrast, `Agent`'s genuinely inline configuration fields — `ResponsePolicy`, `ExpiryDuration`,
`RegenerationCeiling` — are plain values in the class diagram, not `*Reference` fields. Only
`ContextPolicyReference` and (implicitly, via the safety-version pair) safety carry the
"Reference" naming, and only safety has a named aggregate behind it.

**Two-unit incompatible-build scenario:**

- **Team A** reads AD-2's "exactly" list as authoritative and closed: it has no
  `ConversationContextPolicy` aggregate, so Team A embeds the full context-policy content
  (mode enum, Approved Bounded Context Behavior list, margin) as inline fields directly inside the
  `Agent` aggregate's own stream, versioned by `ConfigurationVersion`, with `ContextPolicyReference`
  reduced to a self-referential constant. AD-1's "EventStore-backed domain state" promise is
  satisfied via `Agent`'s own stream.
- **Team B** reads AD-11's "publishes... version[s]" and the `*Reference` naming as decisive,
  by direct analogy with `ContentSafetyPolicy`: it adds a `ConversationContextPolicy` aggregate,
  `system`-scoped exactly like `ContentSafetyPolicy`, with its own EventStore stream, its own
  optimistic-concurrency revision, and its own migration/versioning path, referenced from `Agent`
  by `ContextPolicyReference`.

These are structurally incompatible EventStore topologies (one extra top-level aggregate/stream
exists in Team B's system and not in Team A's), with different concurrency, replay, and migration
consequences, and different answers to "who governs the context-budget `margin` default of 10%
(range 5–25)" cited in AD-11 — that value has no named owner or authorization path in either AD-2
or AD-11 (not listed among `Agent`'s configurable fields, not listed among
`TenantGovernancePolicy`'s fields), so each team also has to invent, independently, whether it is
a compile-time constant or a tenant-configurable, audited field.

**Suggested amendment:** Either (a) add `ConversationContextPolicy` (`system`, versioned) as a
fifteenth row in AD-2's aggregate enumeration, parallel to `ContentSafetyPolicy`, explicitly
including the margin field and its authorized mutator/range, or (b) if the intent is genuinely
inline `Agent` configuration, strike the word "publishes... version[s]" and the `*Reference`
naming from AD-11 and state plainly that `ContextPolicyReference` is a self-versioned field on
`Agent`, not a pointer to an independently-owned record.

---

## Finding 3 — HIGH — `ExternallyRemoved → Joined` direct transition contradicts AD-2's own state-machine string

**Binds:** AD-2 (Rule + second-update), AD-7 (Rule + second-update).

AD-2's Rule states the membership state machine as a literal graph:

> "its membership state machine is exactly `NeverJoined -> Joined -> (ExternallyRemoved |
> Blocked) -> ReadmitPending -> Joined`"

Under this graph, the *only* path out of `ExternallyRemoved` is through `ReadmitPending` (i.e.,
through an authorized clear). But AD-7's second-update membership reconciliation table states a
direct edge, condition-gated only on presence, with no clear/`ReadmitPending` step at all:

> "`ExternallyRemoved` plus absent rejects and **plus present records `Joined`**"

AD-2's own second-update paragraph says it is "the normative state contract where the earlier
AD-2 shorthand differs," but it only re-lists the five state names and the `BlockVersion`/
`MirrorPending` mechanics — it never re-asserts or withdraws the explicit transition graph, so the
graph string remains live spine text that a compliant implementer can rely on.

**Two-unit incompatible-build scenario:**

- **Team A** implements AD-2's literal graph: from `ExternallyRemoved`, the only legal transition
  is to `ReadmitPending` (via an authorized Tenant-Agent-Administrator/Facilitator clear), then to
  `Joined` on the next accepted membership step. If the membership three-part step ever finds
  `hexa` present again in Conversations while the aggregate still records `ExternallyRemoved`
  (e.g., someone re-added the participant directly in Conversations, bypassing any Agents-side
  clear), Team A's build treats this as an anomaly to reject or leave unresolved pending an
  authorized clear — it never auto-heals to `Joined`.
- **Team B** implements AD-7's second-update table literally: the very next membership check that
  observes `ExternallyRemoved` + present writes `Joined` directly, with no clear, no
  `BlockVersion` check, and no `ReadmitPending` step — an unauthorized external re-add silently
  and automatically restores full membership.

This is a real, security-relevant divergence: whether an Agent's Conversation membership can be
silently restored without ever going through the authorized-clearing-authority machinery AD-7's
own Rule paragraph otherwise treats as load-bearing (role-bound vs. identity-bound clearing
authority, `BlockVersion` checks, audit of the clear). One build enforces the authorization gate,
the other bypasses it, and both are literal readings of currently-in-force spine text.

**Suggested amendment:** In AD-2's second-update paragraph, explicitly supersede the transition
graph, not just the state list — state plainly whether `ExternallyRemoved -> Joined` on bare
presence-detection is intentional (and, if so, reconcile it with the clearing-authority rules in
AD-7's main Rule, which otherwise imply an authorized act is required to leave `ExternallyRemoved`)
or whether AD-7's second-update table's "plus present records `Joined`" line is itself the error
and should route through `ReadmitPending` like the `Blocked` path does.

---

## Finding 4 — HIGH — Eligible Approver predicate does not name the regeneration-requesting Approver, opening a self-approval path

**Binds:** AD-8, AD-5, AD-13, AD-30.

AD-8 states the Eligible Approver predicate as a closed list of three exclusions plus one
inclusion test:

> "The Eligible Approver predicate (resolved now, not the caller, not the last editor of the
> version, holding current Conversation read access)"

AD-8's own "Prevents" line frames the intent broadly ("a caller approving their own call"), but
the predicate's actual exclusions are narrower and textually literal: "the caller" (a defined term
— AD-30 distinguishes `CallerPartyId` from `OnBehalfOfPartyId` precisely so "the caller" has one
fixed referent, the interaction's original `CallerPartyId`) and "the last editor of the version"
(naturally read as the Party who produced an `Edited` version — AD-5 defines `Edited` and
`Regenerated` as distinct version kinds with different production mechanisms; a regeneration is
not an edit and has no "editor"). Nothing in the predicate excludes the Party who *requested a
regeneration* (AD-30: "the requesting Approver for a regeneration" — an Approver, by construction,
can request a regeneration) from then being resolved as an Eligible Approver for that same
regenerated version.

**Two-unit incompatible-build scenario:**

- **Team A** implements AD-8's predicate exactly as the three parenthetical clauses state:
  excludes only `CallerPartyId` and (for `Edited` versions only) the editing Party. An Approver
  who triggers a regeneration is not "the caller" and not "the last editor" of the resulting
  `Regenerated` version (there is no editor concept for a regeneration), so that same Approver is
  resolved as eligible and can approve their own regenerated content — a closed loop with no
  independent check.
- **Team B** implements the "Prevents" intent broadly: treats "not the last editor of the version"
  as covering "not the Party who most recently produced the current version by any mechanism,"
  which for a `Regenerated` version means the regeneration-requesting Approver (AD-30's
  `OnBehalfOfPartyId` for that attempt), and excludes them.

Both are defensible readings of AD-8's literal predicate text; they diverge on whether an Approver
can regenerate-then-self-approve, which is exactly a "who exactly" approval-eligibility question
the brief calls out, and it has direct governance consequence (whether every proposal that reaches
`Approved` has had at least one independent decision-maker look at the exact content approved).

**Suggested amendment:** Add "the requesting Approver of the version's own regeneration (if any)"
as a fourth explicit exclusion in AD-8's Eligible Approver predicate, or explicitly broaden "last
editor" to a defined term ("last content-producing actor, covering both edit and regeneration")
used consistently by AD-5, AD-8, and AD-30.

---

## Finding 5 — MEDIUM — "the Agents service principal" is used four times but never placed in AD-30's principal taxonomy

**Binds:** AD-2, AD-6, AD-7, AD-30.

AD-30 states its principal envelope as an exhaustive, closed set: "Every Agents command envelope
carries exactly one principal: `User`... `Administrator`... `Platform`... or `Workflow`..." Yet
AD-2, AD-6, and AD-7 each independently invoke a fifth actor by name that is not one of these four
— "the Agents service principal" — for outbound calls to Parties and Conversations (Party
provisioning, membership adds, tenant-scoped content/roster reads). It is never stated whether
this is (a) an alias for the `Platform` command-envelope principal reused for outbound adapter
calls, (b) a distinct service-account credential that lives entirely outside AD-30's
command-envelope taxonomy (pure infrastructure/mTLS identity, never appearing in an
`Agents` command envelope), or (c) something that itself needs `SecurityEventLog`/audit treatment
under AD-30's "every denial and security event is appended... to `SecurityEventLog`" rule.

**Two-unit incompatible-build scenario:** Team A implements outbound Parties/Conversations calls
under the same `Platform` principal already defined by AD-30 (reusing its HMAC-tagged extension).
Team B treats it as a separate, AD-30-invisible service credential issued directly to the
adapter layer, with no envelope principal at all and thus no `SecurityEventLog` denial-path
coverage for adapter-level authorization failures. The two builds have different audit coverage
and different answers to what a forged/misused adapter credential looks like under
`LR-TENANT-ACCESS`'s "forged-extension test."

**Suggested amendment:** Add one sentence to AD-30 naming "the Agents service principal" as either
a fifth, explicitly out-of-envelope infrastructure identity (with a stated audit-coverage answer)
or an explicit alias of `Platform` used for outbound adapter calls.

---

## Finding 6 — LOW — `AuthorizedProducer` per `GateId` is deferred to an external register that could contradict AD-30's blanket producer rule

**Binds:** AD-17, AD-30.

AD-30 states a blanket rule for who may submit `ReadinessObservation`s: "Platform-kind gates
accept only from `Platform` and Tenant-kind gates from `User` holding Release Operator or
`Platform`." AD-17 separately defers the actual `AuthorizedProducer` value per `GateId` to
`launch-readiness-register.md`, an external document outside this spine's own text. Nothing in the
spine constrains the register from assigning some Tenant-kind gate an `AuthorizedProducer` other
than "User holding Release Operator" (e.g., Tenant Agent Administrator), which would silently
override AD-30's blanket statement without an AD-30 amendment. This is a lower-confidence finding
because it is contingent on a document outside the reviewed file, but it is a real seam: two teams
implementing strictly from the spine's own text (ignoring whatever the register currently says)
could reasonably build different default `AuthorizedProducer` enforcement for a hypothetical new
Tenant-kind gate.

**Suggested amendment:** State in AD-17 or AD-30 that the register's `AuthorizedProducer` column
is constrained to be a subset of (or identical to) AD-30's blanket rule per `ScopeKind`, and that
any register entry naming a different producer for a Tenant/Platform-kind gate is itself invalid
without an AD-30 amendment.

---

## Summary table

| # | Finding | Severity |
| - | --- | --- |
| 1 | AD-30 `Workflow` envelope fixes `OnBehalfOfPartyId` to the snapshot caller, contradicting AD-21's (and AD-30's own later sentence's) per-attempt-varying rule for regeneration cost/rate attribution | Critical |
| 2 | `ConversationContextPolicy` is referenced (`ContextPolicyReference`) and described as independently versioned/published, but has no aggregate in AD-2's closed enumeration — margin/config ownership undefined | Critical |
| 3 | AD-2's literal `ExternallyRemoved -> ReadmitPending -> Joined` graph contradicts AD-7's second-update table's direct `ExternallyRemoved` + present `-> Joined` edge | High |
| 4 | AD-8's Eligible Approver predicate does not exclude the regeneration-requesting Approver, opening a self-approval path for `Regenerated` (not `Edited`) versions | High |
| 5 | "The Agents service principal" (AD-2/AD-6/AD-7) is never placed in AD-30's closed four-member principal taxonomy | Medium |
| 6 | AD-17's register-deferred `AuthorizedProducer` per `GateId` is not constrained to match AD-30's blanket producer rule | Low |
