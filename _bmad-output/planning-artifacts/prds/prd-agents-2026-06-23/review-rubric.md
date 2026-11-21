# PRD quality review — Hexalith Agents

## Overall verdict

**Fair.** The PRD gives downstream teams a coherent product boundary and a testable distinction between an accepted append and a durably posted reply. Four contradictions affect approval of a release, the privacy of a rejected call, the money reserved for a retry, and when retained proposal content can be erased. The current release decision remains **NOT READY** in its own register.

## Decision-readiness — thin

§0 identifies the release decision and sends the reader to the register that owns it. The October amendment authorizes a development milestone while retaining the live custody requirements. A release operator still has two different instructions about a failed accessibility gate.

- **High · R1 — Accessibility is missing from the exhaustive release criteria.** FR-28, lines 727–743, includes “NFR-11 through NFR-14” in its introduction, then declares “nothing outside” its ten listed inputs can affect the decision. The list explicitly includes NFR-11, NFR-12 and NFR-14 and omits NFR-13. Its catch-all for §11 areas does not name UI conformance either. The launch register explicitly requires a passing `LR-UI-CONFORMANCE` covering WCAG, localization and the restricted viewport. A build with passing latency and failing keyboard navigation receives different instructions from the two authorities. **Suggested fix.** Name NFR-13 and its existing register gate in the exhaustive list, and include a qualification case in which that gate alone prevents READY. [Release qualification and UI conformance](../../launch-readiness-register.md#nfr-13-ui-conformance-contract)

## Substance over theater — adequate

FR-9 quantifies the safe context budget; FR-11 and FR-18 name the evidence required before participants see a posted reply. The four named journeys correspond to distinct authorization and disclosure rules. Three counter-metrics provide advice without defining a result that the required monthly review could report.

- **Medium · R2 — Three counter-metrics cannot be calculated consistently.** §12, SM-C1 through SM-C3, lines 1052–1056, says “Do not maximize automatic posting,” “Do not optimize approval completion time” and “Do not optimize the number of Providers/models.” None specifies the observation to report. The launch register requires counter-metric reporting. **Suggested fix.** Define the cohort, observable quantity and interpretation for each one, or designate these as qualitative review questions and state which evidence the reviewer records. Preserve their exclusion from pre-enablement gates. [Product metrics gate inventory](../../launch-readiness-register.md#minimum-gate-inventory)

## Strategic coherence — adequate

The product brief's thesis survives the later decisions. Calling `hexa` is explicit, uses only the Source Conversation and produces an attributable reply or a draft subject to human approval. The PRD explains the replacement of caller self-approval and the create-only Party identity. The main metric intended to test whether governance adds value still admits two different results.

- **Medium · R3 — A straightforward rejection has no unambiguous place in SM-7.** §12, lines 1048–1049, defines rejection as a human decision in SM-3, then counts replies “edited, regenerated, or rejected before that decision” in SM-7. A first-action rejection is the decision itself. Counting it or excluding it changes the 10–60% band that can trigger Product review under FR-28. **Suggested fix.** Specify that the final rejecting decision qualifies if that is the intended OQ-11 rule. Count distinct decided proposals once and give examples for a direct rejection and an edited-then-rejected proposal.

## Done-ness clarity — thin

The revised audit disclosure, tenant-scope and durable-posting requirements distinguish observable outcomes. Other paths still require an engineer to choose between incompatible acceptance conditions.

- **High · R4 — A refused call is required to produce a forbidden or unavailable measurement.** FR-8, lines 293–301, puts authorization first and context measurement seventh. FR-9, lines 317 and 323, stops on a mismatched read before measurement, but demands the measured size and budget components “for every call, including blocked calls.” FR-24, line 646, repeats the unconditional requirement. An unauthorized caller or a mismatched Conversation is a concrete counterexample. **Suggested fix.** Require actual measurements only once the scope and freshness checks pass. Define the permitted content-free audit result when execution stops earlier, and cover both a step-1 denial and a mismatched read.
- **High · R5 — A confirmed no-usage result both releases and reuses the reservation.** FR-4, line 208, permits retry “under the same cost reservation.” FR-28, line 747, releases that reservation on confirmed no usage and also makes this the only outcome eligible for retry under it. OQ-6 repeats both rules. If another call spends the freed budget before the retry, honoring both instructions can exceed the hard cap. **Suggested fix.** Specify whether the reservation stays held across a permitted retry or requires a fresh atomic admission. Identify the release instant and verify a competing call and a retry after restart.
- **High · R6 — The 365-day retention start is ambiguous for confirmation-mode content.** FR-8, line 307, makes `ProposalCreated` a terminal Agent Call status. §9, line 968, and OQ-8 start the clock when the interaction becomes terminal. FR-18, line 476, ties reaching the clock to exit from the proposal's posting states. A draft created on one day and finally posted two weeks later yields two possible erasure dates. FR-11 separately resolves automatic mode. **Suggested fix.** Name the authoritative event for each confirmation outcome and content class. Carry that definition into OQ-8 and test a delayed human decision, terminal posting and legal hold.
- **Medium · R7 — The guarantee of eventual posting resolution has an unstated dependency.** FR-7, line 275, says “no proposal is ever stranded.” FR-18, lines 446 and 476–479, says a failed proposal “always reaches the §9 retention clock,” yet an unavailable or conflicting message lookup can indefinitely prohibit every attempted exit; suspension or disablement also pauses the retry clock. FR-11 keeps an automatic call non-terminal, with its concurrency slot held, throughout that wait. **Suggested fix.** Qualify eventual resolution on authoritative lookup recovery and specify the observable unresolved state and responsible owner during a sustained outage. If a different retention or disposition is required, submit that choice to Governance. A timeout alone cannot certify absence or authorize a duplicate post.

## Scope honesty — strong

The October custody amendment, §5, §6.2 and §8 distinguish what the V1 product owes from what the current milestone can execute. The four committed dependencies still require owner qualification. OQ-18, OQ-23 and OQ-31 identify different affected evaluations. OQ-32 through OQ-34 name owners and restrictive behavior while decisions remain open. Their open status is appropriate to this requirements baseline.

## Downstream usability — adequate

The journeys, glossary and FRs give UX and engineering common vocabulary. Two cross-references could yield different behavior if a team implements from the decision table or journey alone.

- **Medium · R8 — The binding decision summary broadens the empty-roster abandonment rule.** FR-7, line 272, says a `PostingFailed` proposal receives a marker only and is “never abandoned for an empty roster.” OQ-26, line 1107, describes rechecks “over awaiting and `PostingFailed` proposals” and lists two empty rosters as an abandonment condition without limiting that branch to an awaiting proposal. **Suggested fix.** Split OQ-26 by source state. For two empty rosters, assert abandonment for an awaiting proposal and marker-only behavior for `PostingFailed`.
- **Low · R9 — The setup journey starts with an operation that must fail.** UJ-1, line 57, says Nora “activates” the agent before she selects its model and supplies its instructions. FR-1 and FR-5 require these values for activation, and the very next sentence in UJ-1 enforces that rule. **Suggested fix.** Have Nora configure the draft, confirm the required values and then submit activation. Preserve the existing edge case for missing prerequisites.

## Shape fit — adequate

The multi-role product benefits from both user journeys and a capability specification. The approval transition table and authorization matrix serve distinct audiences. §9's implementation-level detail is harder to keep aligned with the downstream design that owns the same mechanisms.

- **Medium · R10 — Detailed destruction mechanics obscure the product outcome.** §9, lines 968–974, interleaves the user-visible retention, legal-hold and erasure rules with signing-attempt ordinals, guard revisions and the `IDeletionCapabilityCompromiseRegistrar` interface. The full protocol already has a home in the Architecture Spine. **Suggested fix.** Keep the retention clock, legal precedence, verified erasure, recovery guarantees and outstanding decisions in the PRD. Link the exact design rules for signing, capabilities and batch recovery so a mechanism change has one authoritative home. [Architecture Spine](../../architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md)

## Mechanical notes

The document defines FR-1 through FR-34, NFR-1 through NFR-14, OQ-1 through OQ-34, four named journeys, seven success metrics and five counter-metrics. Each series has unique, consecutive identifiers. All 28 numeric inline assumption keys have matching index entries. The seven deferred OQ rows remain distinguishable from resolved decisions. The addendum attributes the November test counts to the tested parent revision and keeps later upstream checkouts outside that evidence claim.

**Open findings:** 0 critical, 4 high, 5 medium, 1 low.
