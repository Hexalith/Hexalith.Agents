# PRD Internal-Consistency Review — 2026-09-08

**Scope:** `prd.md` only. Adversarial check for internal contradiction and self-inconsistency introduced by the single-pass 2026-09-08 revision. Not a product-quality review.

**Verdict: CONDITIONAL — the revision is structurally clean (no dangling ids, no duplicates, no heading damage, correct frontmatter) but left 3 substantive contradictions and 6 lesser inconsistencies.**

## 1. Bounded-context contradictions (FR-9 / NFR-8 / OQ-10 seam)

The seam text itself is consistent across FR-9, NFR-8, the `Approved Bounded Context Behavior` glossary entry, the `Conversation Context Policy` glossary entry, and OQ-10. Surviving absolutes:

- **FR-9, audit bullet (line 236)** — requires recording that "complete context was used or that the call was blocked", a two-valued record, while the bullet immediately above it (line 235) requires recording "the resulting context mode", which under the same FR has three values (complete / blocked / Approved Bounded Context Behavior). Self-contradiction inside one FR.
- **§3 glossary, `Conversation Context` (line 89)** — defines the term as "The complete authorized Source Conversation content". Every other passage uses `Conversation Context` as the thing that may be bounded (NFR-8 is literally titled Context Bounds and governs "Conversation Context"). Under a declared Approved Bounded Context Behavior the defined term would be false by definition.
- **OQ-12 (line 719)** — "V1 Agent Calls use only the complete authorized Source Conversation", absolute, with no seam caveat and no `amended 2026-09-08` marker, while its sibling OQ-10 was amended in the same pass to add the seam. True for V1 as declared, but the register now states the rule two different ways.
- **§6.1 In Scope (line 557)** and **§2.3 UJ-2 Path (line 54)** — both state the complete/fail-closed dichotomy with no seam. Accurate for V1 (no behavior declared) but inconsistent in level of qualification with FR-9/NFR-8/OQ-10.
- **§5 Non-Goals (line 545)** and §1 Vision are clean — the non-goal is scoped to *silent* truncation and the Vision makes no context claim.

## 2. Safety checkpoint count

**Mismatch.** OQ-9 states explicitly that "Policy is applied at **three points**", and folds "at approval time and before posting" into the third. But:

- **FR-26 (line 475)** refers to "the **approval-time and pre-post re-checks** required by FR-27" — plural, two distinct re-checks, i.e. four points total.
- **FR-18 (line 344)** specifies an independent pre-post re-validation ("Before posting, the system re-validates ... the approved version still passes the then-current Content Safety Policy"), separate in time and in outcome (`PostFailed`) from FR-17's approval-time check.
- **FR-27 (line 493)** hedges both ways: "at approval time and before posting", readable as one check or two.

Policy version at each point is otherwise consistent (all re-checks = then-current active policy; FR-26 carves the correct exception to its future-calls-only rule). One residual tension: **FR-26 (line 479)** requires each retry to use a policy "at least as restrictive as the initial attempt", while FR-17/FR-18/FR-27/OQ-9 mandate the *then-current* policy at approval and pre-post, which may be weaker than the policy in force at generation.

## 3. Cost / reservation coherence

- **FR-28 (line 512)** still promises release-only-after-confirming-no-usage: "releases any unused reservation **only after confirming that no usage occurred**", and says nothing about the unknown-outcome case. **OQ-6 (line 713)** was amended to hold an unknown-outcome reservation for a bounded period and then conservatively settle it at the estimated maximum. FR-28 as written mandates the indefinite hold OQ-6 explicitly rejects. This is the single clearest surviving contradiction of the revision's own fix.
- **FR-4 (line 167)** states pricing metadata "is not wired to **cost caps, reservation**, or billing" in V1. FR-28, NFR-10 and OQ-6 all require reserving "the maximum estimated attempt cost" before Provider invocation and make "missing pricing ... state" a hard block on invocation — which is precisely wiring catalog pricing to reservation and caps. §6.2 (line 578) is narrower and safe (billing only); FR-4's clause is not.
- **NFR-10 (line 591)** describes reservation + reconciliation with no unknown-outcome rule at all — silent where OQ-6 and FR-12 are now explicit.
- **Exactly-once:** nothing promises exactly-once. NFR-11 says at-most-once, FR-12 says `Unknown` is terminal and "never silently retried". But FR-28, OQ-6 and FR-26 all presuppose "eligible retries" of a Provider attempt, and no passage defines which outcomes are retry-eligible under at-most-once. Not a flat contradiction, but the terms are unreconciled.
- FR-25 (reserved vs settled) and FR-32 (no retroactive release/re-reserve, bounded audited override) are coherent with OQ-6.

## 4. Approver constraints

- **§2.3 UJ-3 (line 61)** does not work under FR-7 as revised: Anika is the only human in the journey, and she "edits the draft ... and approves the final draft". FR-7's last bullet forbids the approving Party from being the Party that last edited the version being approved. The journey needs either a second Approver or an explicit statement that the approved version is the post-edit regeneration; as written it depicts a flow FR-7 rejects.
- **OQ-14 (line 721)** — "**The** Approver Policy authority source **is** the Conversation Facilitator", singular and exclusive, contradicts FR-7 and the §3 `Approver Policy` entry, which both enumerate multiple sources (Facilitator, caller, predefined Parties, tenant roles). OQ-14 was not amended in this pass while FR-7 was expanded.
- The §3 `Approver Policy` entry now cross-references FR-7's Conversation-access and segregation-of-duties constraints and agrees with it. **Caller-as-sole-approver:** nothing permits it; FR-7 forbids it and the glossary defers to FR-7. Clean.

## 5. Dangling references

**Clean.** All 32 FR headings exist (FR-1..FR-32, none missing, none duplicated). Every FR-N, NFR-N (1..14), OQ-N (1..19), SM-N (SM-1..SM-6, SM-C1..SM-C4) and §N reference (§6.2, §8, §9, §11) resolves to an existing target. `RQ-1` is defined in §3. All eight `EXT-*` ids in §8's register scope are internally consistent; five of them (`EXT-PROVIDER-1`, `EXT-SAFETY-1`, `EXT-TOKEN-1`, `EXT-SECRETS-1`, `EXT-TOPOLOGY-1`) appear only in that enumeration and are never described anywhere in the PRD — defensible if the register is the authority, but they carry no in-document meaning.

## 6. Duplicate / orphaned ids and numbering damage

**No duplicates, no heading damage.** Section numbering §0..§13 and §4.1..§4.10 is contiguous.

- Inserted FRs sit out of numeric order within their sections: §4.8 runs FR-22, FR-23, **FR-29**; §4.9 runs FR-24, FR-25, **FR-30**; §4.10 runs FR-26, **FR-31**, FR-27, FR-28, FR-32. Intentional under globally-stable ids, but FR-31 splitting FR-26 from FR-27 is the worst placement — FR-26's forward reference to FR-27 now jumps over an unrelated FR.
- **Orphaned validation coverage:** FR-29, FR-30, FR-31 and FR-32 are named by no success metric. SM-1 covers FR-1..FR-12 and FR-22..FR-28; SM-4 covers FR-19..FR-21. The four FRs added by recent passes fall outside every SM's stated range.
- Minor: SM-3/OQ-11's 26-hour terminal-state threshold is derived from the 24-hour default expiry, but FR-18/OQ-3 permit configuring expiry up to 30 days, under which the metric is unattainable. The metric contract does not scope itself to default-expiry cohorts.

## 7. Frontmatter

**Clean.** `updated: 2026-09-08` (line 5). `status: final`, `created: 2026-06-23` unchanged.
