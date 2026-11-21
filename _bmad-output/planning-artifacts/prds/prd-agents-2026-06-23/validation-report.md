# Hexalith Agents PRD validation

**Fair** · 0 critical · 5 high · 6 medium · 1 low

Validated 2026-11-20T17:45:48+01:00. Working-tree [PRD](prd.md) dated 2026-11-20, fingerprint `44432642dedf` (SHA-256).

## Overall verdict

The PRD gives teams a coherent product boundary and a testable distinction between submitting a reply and proving it was durably posted. Five high-severity findings remain in the full requirements baseline. They affect the evidence needed to release, the scope of assumption blockers, the audit record for a rejected call, money reserved for a retry, and the start of the retention period.

The November update review covered the changed passages. This validation covers the complete working-tree PRD, its addendum and original brief. It checks the decision and launch registers and uses a static comparison with selected source and test definitions at Agents `05ddee8`. Repeated findings from different reviewers are counted once.

Release gate RQ-1 remains NOT READY. Four external dependencies are Committed and none is Available. The configured owner-host checks, live posting receipts and Story 5.11 hosted CI and release rehearsal remain outstanding.

## Dimension verdicts

| Dimension | Verdict | Basis |
| --- | --- | --- |
| Decision-readiness | Thin | The release decision and owner are clear. The exhaustive checklist and the launch register disagree about the evidence needed for UI conformance. |
| Substance over theater | Adequate | Context budgets, posting evidence and user journeys are concrete. Three counter-metrics still lack a reportable observation. |
| Strategic coherence | Adequate | Explicit invocation and attributable replies preserve the product brief’s intent. A direct rejection has two possible interpretations in the metric for meaningful human review. |
| Done-ness clarity | Thin | The expected proof for a successful post is clear. Early denials, provider retries and the retention clock still produce incompatible acceptance tests. |
| Scope honesty | Strong | The October custody amendment, V1 exclusions and four owner commitments draw a clear boundary around the development milestone. The PRD identifies which release and mode decisions remain open. |
| Downstream usability | Adequate | Stable IDs and glossary terms make requirements traceable. The roster decision summary and the setup journey can nevertheless lead a reader to implement the wrong sequence. |
| Shape fit | Adequate | The journeys, authorization matrix and state transitions suit a multi-role product. Detailed deletion mechanics duplicate the architecture and make the product obligations harder to find. |

## Findings by severity

### Critical (0)

None.

### High (5)

**R1 · UI conformance is missing from the exhaustive release checklist**

FR-28, lines 727–743 · NFR-13, line 864 · §11 · launch register, LR-UI-CONFORMANCE · also D1

The checklist includes timing and API parity. It does not name the required accessibility, English/French parity and viewport evidence. A candidate could pass the numbered checks while still failing keyboard navigation.

**Suggested correction.** Name NFR-13 and its existing LR-UI-CONFORMANCE record in the numbered list. Verify that a missing locale string or failed accessibility check alone blocks a release.

**R4 · Early denials require measurements the service cannot safely make**

FR-8, lines 293–301 · FR-9, lines 317–325 · FR-24, line 646 · also B1

Authorization can stop a call before Agents reads the Conversation. A response with the wrong tenant or Conversation must also be discarded before measurement. The audit requirement still demands context size and budget details for every blocked call.

**Suggested correction.** Specify a content-free audit result for an early stop, including the safe gate and reason. Mark measurements as unevaluated and require actual values only after authorized measurement.

**R5 · A provider retry is told to reuse a released reservation**

FR-4, line 208 · FR-28, line 747 · OQ-6

Confirmed zero usage releases the reservation and also permits a retry under that same reservation. A second call can take the released funds before the retry runs.

**Suggested correction.** Specify whether the original reservation stays held for the retry or whether the retry needs fresh atomic budget approval. Exercise a competing call and a restart at the release boundary.

**R6 · Confirmation-mode content has two plausible retention start dates**

FR-8, line 307 · FR-18, line 476 · §9, line 968 · OQ-8 · also B2

Creating a proposal terminates the Agent Call. The proposal can remain open and post weeks later. Both events can be read as starting the 365-day retention period, producing different erasure dates for the same content.

**Suggested correction.** Name the authoritative committed event for each confirmation outcome and content class. Align OQ-8 with §9 and cover delayed approval, final posting and legal hold.

**D2 · The assumption index does not identify every evaluation a row blocks**

FR-28 item 9, line 741 · §8.1, lines 926–957 · Architecture Spine, architecture assumptions

The gate requires each unretired assumption to name the evaluation it affects. Rows such as A-2 and A-27 only point to document sections. The architecture default applies to its own rows. Two release operators can derive different blocker lists from the same PRD.

**Suggested correction.** Record the affected evaluations for each PRD row, or an owner-approved default with explicit exceptions. Preserve A-26’s split scope, A-28’s Automatic-only scope and the retirement-observation requirement.

### Medium (6)

**R2 · Three counter-metrics lack a reportable observation**

§12, SM-C1–SM-C3, lines 1052–1056

The text cautions against maximizing automatic posting, approval speed and provider count. It leaves the required monthly report undefined.

**Suggested correction.** Specify the observation, cohort and interpretation for each measure, or designate it a qualitative review question and identify the evidence to record.

**R3 · A direct rejection can count two ways in SM-7**

FR-18 · §12, SM-3 and SM-7, lines 1048–1049 · also B3

Rejection is a terminal human decision. The numerator counts proposals rejected “before” the decision. Implementations can disagree about an unedited proposal rejected on its first review.

**Suggested correction.** State whether the terminal rejection counts. Give expected results for a direct rejection and for a proposal edited and then rejected, counting each eligible proposal once.

**R7 · The guarantee of eventual posting resolution assumes the owner recovers**

FR-7, line 275 · FR-11 · FR-18, lines 446 and 470–479 · also B2

A confirmed absence or matching durable message can settle an uncertain append. A permanently unavailable lookup can leave posting unresolved indefinitely, even though FR-18 says the proposal always reaches the retention clock.

**Suggested correction.** Make the guarantee conditional on authoritative evidence becoming available. Name the visible state and accountable owner during a prolonged outage. Keep timeout alone from certifying absence.

**R8 · The decision register broadens the empty-roster abandonment rule**

FR-7, line 272 · OQ-26, line 1107

FR-7 gives a failed post a marker after an empty roster and prohibits abandonment on that basis. OQ-26 can be read as abandoning both awaiting and failed proposals after two empty rosters.

**Suggested correction.** Split OQ-26 by source state and give the expected result for each state after two authoritative empty rosters.

**R10 · Deletion protocol details obscure the product guarantee**

§9, lines 968–974 · Architecture Spine, AD-22

The same passage defines retention and legal-hold behavior alongside signing ordinals, guard revisions and a named registrar interface. A protocol change can leave two documents describing different behavior.

**Suggested correction.** Keep the user-visible retention, precedence, erasure and recovery requirements in §9. Point to the architecture for the detailed protocol.

**D3 · A missing policy can be reported as a prohibited cost setting**

FR-28 item 7, line 739 · launch register, blocker vocabulary, line 61

The word “otherwise” applies one blocker to three conditions. With valid quotas but a missing safety policy, that wording reports `ProhibitedCostControlPosture`. The register reserves that result for `ReportingOnlyMonitoring` and `AcceptedLaunchRisk`.

**Suggested correction.** Give policy evidence, absent cost posture and the two explicitly prohibited settings distinct conditions. Use the existing blocker vocabulary for each case.

### Low (1)

**R9 · The setup journey asks for activation before its prerequisites**

UJ-1, line 57 · FR-1 · FR-5

Nora activates the agent before selecting the model and supplying instructions. The activation rules would reject that step.

**Suggested correction.** Place configuration and confirmation of required values before Nora submits activation.

## Implementation findings

These two high-severity source findings concern delivery of stated requirements. They are tracked separately from the twelve document findings and do not affect the PRD grade. The source Conversations adapter is excluded from Release/package mode. No live qualification or test suite was run for this validation.

**B4 · High · A durable message can be accepted without the required provenance**

FR-11 and FR-17 · ConversationClientResponsePoster.cs, lines 165–173 · ConversationClientResponsePosterTests.cs, lines 288–315 and 357–360

The source adapter accepts a message with null provenance. Its recovery fixture expects Posted in that case. A successful lookup can therefore establish message identity without proving its AI marker, call reference or required human-editor attribution.

**Verification.** Require the expected provenance for the selected version in the owner contract and fixture. Include missing provenance and incorrect editor identity.

**B5 · High · Appending the reply can precede the durable approval record**

FR-17 and FR-18 · Story 7.4, epics.md lines 2617–2623 · AgentInteractionProposalApprovalOrchestrator.cs, lines 95–105 and 164–176

The orchestrator invokes the external append before dispatching the approval result. An interruption between those operations can leave the Conversation with a message whose approval was never recorded in Agents.

**Verification.** Demonstrate that committed approval and posting authorization precede append, and that a later command records the owner outcome. Exercise restart on either side of each boundary.

## Mechanical checks

- 34 functional requirements, 14 nonfunctional requirements, 34 decision rows, four named journeys, seven success metrics and five counter-metrics have unique, consecutive IDs in their respective series.
- All 28 numbered inline assumption keys map to index rows and back. Index completeness does not establish an evaluation scope for every row.
- All eight local Markdown links in the PRD and all four in the addendum resolve, including named section anchors where present.
- The addendum attributes its recorded test counts to the parent-tested revisions. This review did not requalify the newer submodule checkouts.

## Reviewer files

- [review-rubric.md](review-rubric.md)
- [review-authority-2026-11-20-validate.md](review-authority-2026-11-20-validate.md)
- [review-behavior-2026-11-20-validate.md](review-behavior-2026-11-20-validate.md)

[Validation rubric](../../../../.agents/skills/bmad-prd/assets/prd-validation-checklist.md)
