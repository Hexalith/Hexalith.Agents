# PRD quality review · Hexalith Agents · 20 November 2026

## Overall verdict

**Changes required at initial review.** The update gives Release a concrete gate inventory, gives operators a safe pending outcome for an unresolved post and distinguishes eligibility for erasure from authority to erase. Four remaining discrepancies affect the owner qualification fixture, safe denial evidence, the call lifecycle after an inconclusive Provider invocation and the decision to convene a containment review. This pass assessed PRD `93518e84ef2b` and addendum `9bee2f32a4ed` against the November PRD and architecture findings.

## Decision-readiness — adequate

FR-28 names all 18 required gates, distinguishes the four policy/posture blockers and preserves the narrower assumption scopes. OQ-35–38 identify the operations, owners and prerequisite decisions governing expiry, posting finality, Provider retries and export-key delivery. Live evidence and owner approvals remain outstanding. [PRD, lines 736–748, 914 and 1150–1153](prd.md)

- **High · The release fixture can accept a false posting failure.** Section 11 says a definitive owner refusal and authoritative absence yield `PostingFailed`. A refusal of a later delivery and absence at the time of lookup can coexist with an earlier dispatch that will commit. Require proof that all previously issued dispatches for the intent have closed and verify that an outstanding earlier dispatch preserves `PostingPending` past the deadline. Exercise both response modes. [PRD, lines 445–447, 896, 1052–1053 and 1151](prd.md)

## Substance over theater — strong

SM-C1–C3 now identify observable activity, source evidence, the reporting window, owners and how to report missing evidence. SM-7 counts each human-decided proposal once, including a direct rejection and a rejection after an edit. These criteria give a launch-health reviewer a usable record without introducing a new threshold. [PRD, lines 1075–1089](prd.md)

## Strategic coherence — adequate

A tenant's protection depends on the operator recognizing a threshold crossing in the correct population. FR-28 defines the relevant exposure for each rate.

- **High · OQ-27 applies the distinct-Party rule to all four review triggers.** FR-28 uses that denominator and its convening floor for the blocked and unavailable shares. The Provider-failure denominator is accepted calls that reached the Provider. The posting-failure denominator is the cohort entering Approved. A single prolific caller can account for enough failures to cross the Provider threshold in a qualifying cohort. Using distinct Parties or applying the three-Party floor to that branch would suppress the review. Point OQ-27 to FR-28's metric-specific rules and common minimum sample. [PRD, lines 762–768 and 1142](prd.md)

## Done-ness clarity — thin

The new reservation rule protects funds from competing calls while an authorized retry remains available. The three retention rows distinguish a terminal proposal, a terminal automatic posting record and a terminal call. Two remaining acceptance choices can still produce incompatible implementations.

- **High · An indeterminate first attempt has no disposition for the overall call.** FR-10 and FR-12 end the attempt and preserve its public `AgentGenerationOutcome.Indeterminate` across financial settlement. FR-8 defines a separate closed set of terminal call statuses. On a first attempt that produces no proposal or posting record, FR-32 needs the overall terminal fact to release the caller slot, and §9 needs it to start the retention clock. Specify that call fact, its public status/reason or the owner decision governing it, the release of the caller slot and the retention anchor independently of the budget hold. A failed regeneration must preserve the existing proposal. [PRD, lines 306–307, 333–335, 371, 785 and 980–984](prd.md)
- **Medium · One audit clause still requires context metadata on every call.** FR-9 still says “Agent Calls record the resolved context mode” with a policy version and context metadata. A caller denied before the context gate has neither an evaluated mode nor authority to acquire the missing fields. Restrict this clause to the checks reached and the fields safely established. A typed failure at an executed context check can record the outcome and permitted reason while omitting the unexpected resource's metadata. [PRD, lines 292, 304, 317, 323–325 and 649](prd.md)

## Scope honesty — strong

The October custody amendment preserves protected-content and launch requirements. The revised governance section makes due expiry, hold deferral, first key delivery, and confirmed deletion visible as separate states. The addendum attributes builds to their tested revision and identifies open hosted and owner work. [PRD, lines 10–23 and 976–1000](prd.md) · [Addendum, lines 12–28](addendum.md)

## Downstream usability — adequate

The setup journey orders configuration before activation. OQ-26 distinguishes eligible-roster behavior for pending proposals and failed posts. FR-18 gives operators a safe correlation for unresolved posting, specifies both retry deadlines, and preserves lookup obligations after an earlier attempted append. The discrepancies above affect fixtures sourced directly from §11, FR-9 or OQ-27. [PRD, lines 52–59, 477–480 and 1141](prd.md)

## Shape fit — strong

The FRs and named journeys serve the administrator, Participant, Approver and operator. Section 9 carries the observable hold, retention, erasure and export guarantees. The addendum links to the detailed ordering, signing and receipt protocols maintained by Architecture. [PRD, lines 37–85 and 966–1009](prd.md) · [Addendum, lines 20–28](addendum.md)

## Mechanical notes

All 34 FRs, 14 NFRs, 38 OQ rows, 28 indexed assumptions, seven success metrics and five counter-metrics have unique consecutive IDs within their respective series. Local Markdown file destinations in the PRD and addendum resolve. The source-adapter provenance, approval ordering, credential resolution and legacy-route behavior remain delivery issues recorded separately in the addendum. RQ-1 remains **NOT READY**. The review exercised document checks only.

## Post-correction disposition · 20 November 2026

**Pass on the four substantive findings.** Reviewed PRD `0ec65b99db88` and addendum `4b50332811fb`. The updated document supports the following decisions.

| Initial finding | Disposition |
| --- | --- |
| Release fixture accepts an early negative posting result | Closed. The approved-reply fixture refuses the replay, observes temporary absence, retains pending status through the deadline, then confirms the delayed original once. A separate run needs proof that every dispatch is closed to record a negative result. [PRD, lines 445–447 and 1053](prd.md) |
| Kill-switch review uses the wrong denominator | Closed. OQ-27 applies the distinct-calling-Party denominator and convening floor only to the blocked and unavailable shares. It identifies the invoked-call and approved-proposal denominators and retains the common minimum sample. [PRD, lines 762–768 and 1142](prd.md) |
| Initial indeterminate generation lacks a terminal call disposition | Closed. The caller sees a terminal `AgentInteractionStatus.GenerationFailed` and a separate, immutable `AgentGenerationOutcome.Indeterminate`. The call commit releases the caller's slot and anchors retention for retained data. The budget follows the independent indeterminate-usage treatment. AD-13 defines the content-free call result and the unknown Provider outcome. Story 6.4 owns the attempt outcome, which is included in the public vocabulary. [PRD, lines 333, 371, 752 and 984](prd.md) · [Spine, lines 443–445](../../architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md) · [Epics, lines 372 and 2390](../../epics.md) |
| Context audit fields required before the check runs | Closed. A denial before the context step leaves its mode absent. At the context check, policy identifiers and metadata require independent authorization and observation. [PRD, lines 304, 317, 323–325 and 649](prd.md) |

Two low-severity clarifications remain at this fingerprint. The automatic posting qualification row can explicitly reuse the delayed-dispatch negative fixture from the approved-reply row. FR-10 can clarify that a failed regeneration leaves the existing proposal and caller-slot disposition unchanged and starts no retention clock. The latter matters when the original slot has already been released at call termination. [PRD, lines 307, 333, 982 and 1052–1053](prd.md) · [Spine, AD-21 line 533](../../architecture/architecture-agents-2026-06-23-2/ARCHITECTURE-SPINE.md)

### Final gate verdict · 20 November 2026

**Pass.** Rechecked PRD `8c6ac3a71cd2` and addendum `4b50332811fb`. The automatic fixture now exercises the delayed-dispatch and terminal-failure cases. An indeterminate regeneration ends only its attempt, leaves the proposal open and changes neither the original caller’s slot nor the proposal’s retention clock. All four initial findings and both clarifications are closed. No document finding remains from this gate. Release qualification remains **NOT READY**. [PRD, lines 17, 333 and 1052–1053](prd.md)
