---
title: Adversarial Divergence Review v9
target: ARCHITECTURE-SPINE.md (working tree, post round-5 Update pass, architecture_assumption_index_version 4)
role: Independent adversarial architecture reviewer
method: Construct two AD-compliant implementations that build incompatibly
date: 2026-09-10
---

# Adversarial Divergence Review v9

## Method note

The round-5 Update pass (uncommitted working-tree diff against the last commit, `c4121b0`)
rewrote AD-5, AD-6, AD-7, AD-8, AD-11, AD-12, AD-13, AD-14, AD-17, AD-21, AD-22, AD-29, AD-30, the
frontmatter, and the Stack table, closing findings from `VALIDATION-REPORT-2026-09-09-4.md`
(round 5, v8 lenses). Every "per AD-X" citation relied on below was re-checked against AD-X's own
live text rather than trusted from any prior round's "closed" claim, per the brief. Two v8 Critical
findings (`OnBehalfOfPartyId` fixed to the snapshot caller; `ExternallyRemoved -> Joined` on bare
presence) and one v8 High finding (no exclusion for a regeneration-requesting Approver) are
independently confirmed genuinely closed by this round's AD-30, AD-7, and AD-8 rewrites
respectively — verified directly against the live text, not re-reported here. This round instead
surfaces two new Critical divergences the round-5 rewrite itself introduced (one text-vs-diagram,
one cross-AD), plus a residual graph gap the round-5 AD-7 rewrite did not reach, plus lower-tier
gaps.

## Verdict

**FAIL.** Two Critical divergences are new to this round: AD-13's rewritten acceptance-order
prose now contradicts the spine's own (unedited) sequence diagram on whether an Automatic-mode
call ever touches Eligible-Approver resolution at all, and AD-22's rewritten `DigestKeyVersion`
paragraph misclassifies the exact digest that AD-13 and AD-29 (both also rewritten this round)
treat as the canonical example of a version-pinned, rotation-safe comparison. One High finding
is a residual gap in AD-7's reconciliation text that the round-5 AD-7 rewrite fixed on one axis
(`ExternallyRemoved -> Joined`) but left open on an adjacent one (`NeverJoined -> Blocked`).

---

## Finding 1 — CRITICAL — AD-13's rewritten acceptance order contradicts the spine's own sequence diagram on Eligible-Approver resolution in Automatic mode

**Binds:** AD-13, AD-8, the "Call hexa" sequence diagram (Design Paradigm section, unedited by
round 5).

Round 5 rewrote AD-13's "Acceptance follows the FR-8 order" clause from a short parenthetical list
into a fully spelled-out ordered list, and in the same edit added an entirely new behavioral claim
about response modes. The live AD-13 text now reads:

> "Acceptance follows the FR-8 order — caller authorization, Party state, and Source Conversation
> access; Agent lifecycle and tenant suspension...; the local `ConversationAgentState` block
> check; Provider/model eligibility and production-like enablement; rate limits and the per-Party
> concurrent bound; **Eligible Approver resolution, Confirmation Response Mode only**;
> Conversation Context Policy measurement; cost reservation of the estimated attempt cost; Content
> Safety Policy presence and the pre-Provider scan; and Agent membership... — and ends with one
> `AgentCallAccepted` event. **No Eligible Approver is resolved or referenced in Automatic
> Response Mode (FR-11)**: the posting record substitutes any current Conversation Facilitator of
> the Source Conversation wherever an FR-18 rule names an Eligible Approver, and a zero-Approver
> configuration never blocks an Automatic-mode call."

This is a genuine behavior change from the pre-round-5 text (which had Eligible Approver
resolution run "in every response mode... an Automatic-mode Agent with zero configured Approvers
is rejected up front with `NoEligibleApprover`"), and it is stated twice, unambiguously, in the
new paragraph: Automatic mode never resolves and is never blocked by Eligible-Approver state.

But the spine's own sequence diagram — present before round 5 and **not touched by this round's
diff** — still shows Eligible-Approver resolution as an unconditional, mode-agnostic step inside
the shared acceptance sequence, before the automatic/confirmation branch even splits:

> ```
> Workflow->>Safety: validate prompt + context (inside activity)
> Safety-->>Workflow: versioned decision by reference
> Workflow->>Interaction: EligibleApprover resolution (both modes)
> Workflow->>Conv: membership three-part step
> alt any acceptance step fails
> ```

`(both modes)` is an explicit, literal annotation directly contradicting AD-13's new "Confirmation
Response Mode only" / "No Eligible Approver is resolved... in Automatic Response Mode" prose. The
two artifacts also disagree on *where* the step sits: AD-13's prose places Eligible Approver
resolution before Conversation Context Policy measurement and cost reservation and the
pre-Provider safety scan (5th of 10 named steps); the diagram places it after context measurement,
cost reservation, and the pre-Provider safety scan, immediately before the membership step (2nd
from last).

**Two-unit incompatible-build scenario:**

- **Team A** implements AD-13's rewritten prose literally: the Automatic-mode acceptance path
  never calls Eligible-Approver resolution, never checks Approver count, and always substitutes
  the current Facilitator at posting time — an Automatic-mode Agent with zero configured Approvers
  accepts every call. Team A's acceptance pipeline order is: auth → lifecycle/suspension → block
  check → provider eligibility → rate limits → **approver resolution (Confirmation only)** →
  context measurement → cost reservation → safety scan → membership.
- **Team B** implements the sequence diagram literally, as the canonical depiction of "the FR-8
  order" the prose claims to describe: Eligible-Approver resolution runs unconditionally for every
  accepted call regardless of mode, positioned after safety and immediately before membership. An
  Automatic-mode Agent with zero configured Approvers is rejected with `NoEligibleApprover` before
  membership is even attempted — exactly the pre-round-5 behavior the prose rewrite was supposed
  to replace.

These are observably different systems: Team A's Automatic-mode Agents work with zero Approvers
configured and never emit `NoEligibleApprover` in that mode; Team B's do not. This is precisely
the kind of functional, testable divergence FR-11/FR-18 acceptance tests would catch as two
different products, both built to the letter of AD-13.

**Suggested amendment:** Redraw the sequence diagram's `EligibleApprover resolution (both modes)`
step as conditional (an `alt`/`opt Confirmation Response Mode` guard) matching AD-13's new
per-mode rule, move it to the position AD-13's prose now specifies (before Conversation Context
Policy measurement), and add the Facilitator-substitution note to the diagram's automatic branch
so a reader of the diagram alone reaches the same acceptance pipeline as a reader of AD-13's prose
alone.

---

## Finding 2 — CRITICAL — AD-22 classifies the `ProviderAttempt` fingerprint as a "short-lived" digest with "no rotation risk," contradicting AD-13's and AD-29's own version-pinned recompute rule for that exact digest

**Binds:** AD-22, AD-13, AD-29.

All three of AD-13, AD-22, and AD-29 were rewritten this round to describe the same `DigestKey`
rotation/versioning mechanism, and the brief specifically asks whether the three descriptions
stayed the same mechanism. They did not.

AD-13's rewritten fingerprint paragraph states, of the `ProviderAttempt` fingerprint specifically:

> "...is persisted on the `ProviderAttempt` record together with its `DigestKeyVersion` **so a
> retry's fingerprint recheck always recomputes against the version the retained fingerprint was
> computed under**."

AD-29's rewritten sensitive-content-digest rule lists the same fingerprint among the digests that
must survive exactly this kind of recheck:

> "Every digest of sensitive content stored outside a `ProtectedContent` envelope (per-message
> digests, verdict-cache keys, evidence fingerprints, command-payload fingerprints, and **the
> AD-13 `ProviderAttempt` fingerprint**) is an HMAC-SHA-256 under a per-tenant `DigestKey`...and is
> persisted together with the `DigestKeyVersion` it was computed under...; re-verification or an
> idempotency-conflict comparison **always recomputes against the recorded `DigestKeyVersion`,
> never the tenant's current one**, so a comparison is never ambiguous after a rotation."

Both of these describe the `ProviderAttempt` fingerprint as a digest that is explicitly designed
to be recomputed and re-compared *after* a possible key rotation — that is the entire point of
persisting `DigestKeyVersion` alongside it. But AD-22's rewritten rotation paragraph, describing
the identical mechanism, puts the same fingerprint in the opposite bucket:

> "A **short-lived digest (a verdict-cache key, a `ProviderAttempt` fingerprint) is never compared
> across a rotation boundary and so carries no rotation risk**; a long-lived digest embedded in an
> immutable audit or idempotency record carries its `DigestKeyVersion` (AD-29) alongside the
> digest value and keeps its originating version's comparability forever..."

AD-22 explicitly names "a `ProviderAttempt` fingerprint" as the worked example of a digest that is
*never* compared across a rotation boundary — directly contradicting AD-13's and AD-29's own text
about that same field, both of which exist specifically to make a cross-rotation comparison safe.
This is not a subtle inference: AD-22 uses the identical term ("`ProviderAttempt` fingerprint")
that AD-13 and AD-29 use, and assigns it to the opposite classification. It also sits awkwardly
against AD-22's own retention rule two sentences later in the same AD: `ProviderAttempt` records
live inside `AgentInteraction`, which is retained 365 days under AD-22's own sensitive-content
retention clause — not a "short-lived" record by the spine's own definition of the term elsewhere
(a verdict-cache entry, by contrast, is invalidated on every policy publication per AD-20).

**Two-unit incompatible-build scenario:**

- **Team A** builds retry-fingerprint verification per AD-13's and AD-29's explicit rule:
  `ProviderAttempt.DigestKeyVersion` is persisted indefinitely (for the life of the attempt
  record, i.e. up to 365 days), and every retry recheck recomputes the fingerprint under the
  *recorded* version, never the tenant's current key, exactly as AD-29 specifies for "the AD-13
  `ProviderAttempt` fingerprint" by name.
- **Team B** builds off AD-22's explicit classification: treats the `ProviderAttempt` fingerprint
  as short-lived and rotation-risk-free, so does not implement (or actively strips) the
  version-pinned recompute path — comparing a retry's freshly computed fingerprint against the
  stored one always under the tenant's *current* `DigestKey`.

Team B's system silently breaks the very retry-authorization gate AD-13 depends on
("A retry before a terminal outcome is authorized only when outcome lookup... confirms no usage
and the entry's per-model retry budget is not exhausted") the moment a `DigestKey` rotation occurs
between the original attempt and a later retry (e.g., across an `Indeterminate` hold's 1–72 hour
window, or a `PostingFailed` retry within the 15-minute bound after a rotation lands mid-window):
every legitimate retry's recomputed fingerprint will mismatch the stored one under the *current*
key, and AD-13 says "any change to the fingerprint... fails closed under that attempt id" — Team
B's build fails closed on every retry across a rotation, an availability regression neither AD-13
nor AD-29 intends and that AD-22's own text (read on its own) explicitly claims is impossible
("no rotation risk").

**Suggested amendment:** Remove "a `ProviderAttempt` fingerprint" from AD-22's short-lived example
list; it belongs in the long-lived, `DigestKeyVersion`-pinned category alongside "an immutable
audit or idempotency record," matching AD-13's and AD-29's own text verbatim. Reserve the
short-lived/no-rotation-risk classification for digests that are genuinely never persisted past
their immediate use (the verdict-cache key is the correct example; a command-payload idempotency
fingerprint is not, either — AD-29 lists it as `DigestKeyVersion`-pinned too, which AD-22's
sentence does not currently classify at all).

---

## Finding 3 — HIGH — AD-7's "a block set from any state" text still has no edge in AD-2's literal state-machine graph string

**Binds:** AD-2 (Rule + second-update), AD-7 (Rule + third-update, both touched this round).

The brief specifically asks whether AD-7's round-5 rewrite now fully agrees with AD-2's literal
graph string. On the axis the round-5 rewrite targeted — `ExternallyRemoved -> Joined` — it does:
the new AD-7 third-update paragraph explicitly states re-admission is "through `ReadmitPending`,
never a direct `ExternallyRemoved` -> `Joined` transition on presence alone, matching AD-2's
state-machine graph exactly," closing v8's Finding 3. But a second, adjacent gap on the same graph
was not reached by this round's edit and survives unchanged from the prior round.

AD-2's Rule states the graph as a closed, literal transition list:

> "its membership state machine is exactly `NeverJoined -> Joined -> (ExternallyRemoved |
> Blocked) -> ReadmitPending -> Joined`"

Read as a literal FSM, `Blocked` is reachable only from `Joined`. But AD-7's Rule paragraph (the
main rule, not the third-update table) states block-setting is available from the start, and the
third-update reconciliation paragraph — rewritten this round — explicitly carries forward a
sentence handling the `NeverJoined` case by name:

> "The block is set by the Tenant Agent Administrator, the Conversation Facilitator..., or the
> membership step on detecting an external removal..." [AD-7 Rule]
>
> "A block set from any state increments `BlockVersion`, writes an at-least-once removal outbox
> entry, and sets `MirrorPending`; an already-absent participant confirms the entry, while
> `NeverJoined` causes no Conversations call." [AD-7 third-update]

"A block set from any state," with an explicit `NeverJoined` carve-out for the *outbox-call*
behavior (not for whether the transition itself is legal), describes a `NeverJoined -> Blocked`
transition — e.g. a Tenant Agent Administrator pre-emptively blocking `hexa` from a Conversation
it has never joined. AD-2's "exactly" graph has no such edge; under a literal reading, `Blocked`
is unreachable except from `Joined`.

**Two-unit incompatible-build scenario:**

- **Team A** implements AD-2's graph as a literal state-machine validator: a `SetBlock` command
  against a `ConversationAgentState` currently in `NeverJoined` is rejected as an illegal
  transition (or silently coerced to a no-op), because no `NeverJoined -> Blocked` edge exists in
  the "exactly" graph. A Tenant Agent Administrator cannot pre-block an Agent from a Conversation
  it has not yet joined.
- **Team B** implements AD-7's "any state" text literally: `SetBlock` succeeds from `NeverJoined`,
  incrementing `BlockVersion`, skipping the Conversations outbox call (per the explicit carve-out),
  and leaving the aggregate in `Blocked`.

This is a real governance-feature gap, not a cosmetic one: whichever team is "wrong" either loses
a legitimate pre-emptive block capability that AD-7's own text clearly intends to support, or
implements a transition AD-2's closed graph explicitly forbids.

**Suggested amendment:** Extend AD-2's graph string to include the `NeverJoined -> Blocked` edge
(and, for symmetry, confirm or deny whether `ReadmitPending -> Blocked` is also legal, since
AD-7's "any state" language covers that case too), e.g. `NeverJoined -> (Joined | Blocked) ->
... `, or narrow AD-7's "any state" to "any state reachable after `Joined`" if the pre-emptive
`NeverJoined` block is not actually intended.

---

## Finding 4 — MEDIUM — AD-5's new `PostingPending` attempt deadline has no stated duration, range, or `[ASSUMPTION]` key

**Binds:** AD-5 (rewritten this round), AD-28.

Round 5 added an entirely new timing concept to `PostingPending`'s uninterruptibility clause:

> "The attempt carries a stored deadline, evaluated on every read, command, and recovery after a
> process loss regardless of timer delivery, and never extended: an elapsed deadline concludes the
> attempt as timed out, the `MessageId` lookup below then runs, and the proposal moves to `Posted`
> with `LateConfirmed` if the message is found or to `PostingFailed` if the lookup finds no posted
> message..."

Every other deadline in the spine carries an explicit value, range, and governing `[ASSUMPTION]`
key: the proposal expiry window (24h default, 1h–30d range, Consistency Conventions table), the
`PostingFailed` retry bound (15 minutes, `[ASSUMPTION A-7]`), the `Indeterminate` reservation hold
(24h default, 1–72h range, `[ASSUMPTION A-8]`), the timer skew tolerance (2 seconds,
`[ASSUMPTION ARCH-A-7]`). This new `PostingPending` attempt deadline has none of these — no
number, no range, no assumption key — and AD-28 (Time Authorities), the spine's own named
authority for "every stored instant, deadline, freshness evaluation, timer" and the place that
enumerates the aggregate-stored deadlines (`ExpiresAt`, reservation deadline, retry-window start),
does not list it either.

**Two-unit incompatible-build scenario:** Team A ships the posting-attempt deadline at whatever
value happens to match the Conversations posting call's own HTTP/transport timeout, treating a
slow-but-eventually-successful post as never timing out within `PostingPending` (deadline
effectively unbounded, since none is specified). Team B invents a fixed operational constant (e.g.
60 seconds) independent of any transport timeout, so a legitimately slow-but-successful post
against a loaded Conversations instance gets marked `PostingFailed` while Team A's identical call
would have stayed `PostingPending` to a successful conclusion. Both builds satisfy AD-5's letter
("the attempt carries a stored deadline"); they disagree, unobservably from the spine text alone,
on when a proposal crosses into `PostingFailed` versus `Posted` with `LateConfirmed`.

**Suggested amendment:** Give the `PostingPending` attempt deadline an explicit default, range, and
`[ASSUMPTION]` key in AD-5 (or point it at an existing one, e.g. AD-10's per-model timeout policy,
if that is the intended source), and add it to AD-28's list of aggregate-stored deadlines.

---

## Lower-severity / carried-over notes

- **LOW — frontmatter provenance gap.** The `sources` list (frontmatter) was extended this round
  to add `reviews/review-2026-09-09-{rubric,verified-current,adversarial-divergence,
  security-data-integrity}-v7.md`, but round 5's own stated trigger — `VALIDATION-REPORT-2026-09-09-4.md`
  and its four v8-lens review documents — is cited nowhere in `sources`. A reviewer or downstream
  team tracing "what inputs shaped this spine" from the frontmatter alone will not find the
  reports that demonstrably drove this round's AD-5/6/7/8/11/12/13/14/17/21/22/29/30 rewrites.
  Fix: add `VALIDATION-REPORT-2026-09-09-4.md` and the four `-v8.md` review files to `sources`.

- **Carried over, still open — "the Agents service principal."** v8's Finding 5 is not touched by
  this round's AD-30 edits (which addressed only the `Workflow`-envelope `OnBehalfOfPartyId`
  clause and the `Platform` freshness-source wording). AD-2, AD-6, and AD-7 still name "the Agents
  service principal" for outbound Parties/Conversations calls (Party provisioning, membership
  adds, tenant-scoped reads) as a distinct actor from AD-30's closed four-member envelope taxonomy
  (`User`/`Administrator`/`Platform`/`Workflow`), with no stated mapping between the two. Confirmed
  independently: still live text, still unresolved.

---

## Summary table

| # | Finding | Severity | New this round? |
| - | --- | --- | --- |
| 1 | AD-13's rewritten acceptance order says Eligible-Approver resolution is Confirmation-mode-only and never runs in Automatic mode; the unedited sequence diagram labels the identical step `(both modes)` and places it at a different point in the pipeline | Critical | Yes — round-5 AD-13 rewrite |
| 2 | AD-22's rewritten rotation paragraph classifies the `ProviderAttempt` fingerprint as "short-lived... never compared across a rotation boundary," contradicting AD-13's and AD-29's own (also rewritten) text, which persists that fingerprint's `DigestKeyVersion` specifically to make a cross-rotation retry recompute safe | Critical | Yes — round-5 AD-13/AD-22/AD-29 rewrites |
| 3 | AD-7's "a block set from any state" (with explicit `NeverJoined` handling) has no `NeverJoined -> Blocked` edge in AD-2's literal "exactly" state-machine graph string | High | Partially — round-5 AD-7 rewrite fixed the adjacent `ExternallyRemoved -> Joined` gap (v8 Finding 3) but left this one |
| 4 | AD-5's new `PostingPending` attempt deadline has no stated duration, range, or `[ASSUMPTION]` key, and AD-28 doesn't enumerate it | Medium | Yes — round-5 AD-5 rewrite |
| 5 | Frontmatter `sources` omits `VALIDATION-REPORT-2026-09-09-4.md` and the v8 review family despite being this round's stated trigger | Low | Yes — round-5 frontmatter edit |
| 6 | "The Agents service principal" (AD-2/AD-6/AD-7) still not placed in AD-30's closed principal taxonomy | Medium (carried over from v8 Finding 5) | No — untouched by round 5 |
