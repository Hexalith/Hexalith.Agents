# Hexalith Agents architecture validation

20 November 2026 · Assumption index 15 · Repository baseline `05ddee8`

> **Changes required.** The spine has six high, four medium and one low architecture finding. The highest-impact gaps concern routine retention expiry, a delayed Conversations append, and emergency containment during an outage. The review also identified three repository conformance findings. **RQ-1 remains NOT READY.**

| Review scope | Critical | High | Medium | Low |
| --- | ---: | ---: | ---: | ---: |
| Architecture rules, owner contracts and diagrams | 0 | 6 | 4 | 1 |
| Implementation and evidence | 0 | 1 | 1 | 1 |
| **Total after merging overlapping findings** | **0** | **7** | **5** | **2** |

**Decisions needed first.** Product, Governance and Architecture need to authorize the handoff from retention expiry to erasure. Agents and Conversations need to establish when an outstanding append can no longer commit. The capacity owner and the export custodian also own concurrency decisions described below. The other architecture findings have corrections supported by existing requirements.

This review covers the current working copy of the initiative spine and the documents and source code on which its decisions depend. The four independent reviewers examined the architecture rubric, repository evidence, incompatible implementations, and data integrity. A finding appearing in multiple reviews is counted once. Earlier implementation gaps already identified in the spine appear under release evidence below. [Architecture spine](ARCHITECTURE-SPINE.md) · [PRD](../../prds/prd-agents-2026-06-23/prd.md)

## High findings

### H1 · Retention expiry has no authorized path to erasure

At the end of retention, or when a hold releases after that date, Story 8.1 emits one eligible-expiry request for Story 8.3. The timer has authority to record that the content is due. The deletion aggregate and its admission rule recognize only an operator request approved by Compliance or an approved Conversations deletion. A routine expiration has neither. Content can stay past its deadline, and a direct timer purge would bypass the hold and destruction controls. [AD-2, line 272 and AD-22, line 563](ARCHITECTURE-SPINE.md) · [PRD section 9, lines 968–970](../../prds/prd-agents-2026-06-23/prd.md) · [Story 8.1, lines 2904–2912](../../epics.md) · [DeletionPrepare, line 347](../../launch-readiness-register.md)

**Owner decision.** Product, Governance and Architecture need to define what authorizes routine expiration and how it enters the existing protected deletion process. Prove one expiry both without a hold and after hold release. Include a racing new hold, duplicate delivery and a restart. Require the existing protected-copy and projection receipts before reporting completion.

### H2 · A delayed append can succeed after Agents abandons a reply

The posting rule accepts the stored deadline plus an authoritative read reporting an absent message. Conversations promises an idempotent append and a lookup by `MessageId`. The contract gives no assurance that the original append has finished or can no longer commit. A lookup can truthfully return absence, Agents can record failure and abandonment, and the outstanding append can then make the message visible. Reusing the same key prevents a second message but cannot withdraw the first request. [AD-5 and AD-6, lines 312–328](ARCHITECTURE-SPINE.md) · [Conversations owner contract, lines 79–84](../../external-dependency-register.md)

**Owner decision.** Agents and Conversations need a durable owner outcome that closes the outstanding attempt before failure or abandonment, or must keep its outcome unresolved. Exercise an append delayed through both the deadline lookup and the abandonment lookup in each response mode.

### H3 · The general authorization rule can prevent emergency containment

The conventions table tells every path to check Party, Conversation, Provider and ApproverPolicy before side effects. The emergency containment rules deliberately omit readiness gates so an authorized operator can stop activity during a dependency outage. Middleware that follows the table literally could refuse an incident confirmation or its emergency kill-switch command during that outage. [Conventions, line 1010 and AD-12, line 423](ARCHITECTURE-SPINE.md) · [Containment matrix, lines 235–237](../../launch-readiness-register.md)

**Architecture correction.** Make the specific operation and its versioned matrix row determine the dependency checks. Preserve direct authority, tenant isolation and expected-revision checks on the emergency command. Test an authorized containment request with the unrelated services unavailable and an unauthorized request under the same conditions.

### H4 · The execution diagram exposes a generated-output reference too early

The diagram returns generated output by reference to Dapr Workflow before the output safety check and before `RecordGeneratedVersion`. AD-13 and AD-27 keep the bytes inside the activity until safety passes and a protected version commits. Denied output, or output discarded because deletion won the race, has no valid reference that workflow history can retain. [AD-13, line 445, AD-27, line 605 and sequence, lines 873–906](ARCHITECTURE-SPINE.md)

**Architecture correction.** Keep provider output, safety evaluation and conditional persistence inside the activity. Return a sealed reference only for a committed version. Test denied output and deletion winning the race and inspect recorded workflow history in each case.

### H5 · The stale-capacity branch leaves a committed directory lease unsettled

When `BeginInvocation` reports a stale or reclaimed fence, the diagram records capacity failure and settles the budget. The instruction to settle the `ProviderInvocation` directory lease appears only on the other branch. AD-13 requires a durable result and acknowledgement. On restart, an implementation that follows the drawing can leave the directory showing work in flight and delay deletion. [AD-13, lines 436–443 and sequence, lines 858–873 and 910–912](ARCHITECTURE-SPINE.md)

**Architecture correction.** Show the stale-fence branch obtaining the authoritative allocator result, recording its no-use and capacity disposition, then settling the same directory lease. Interrupt between those steps and verify recovery reaches a single acknowledged outcome.

### H6 · An inconclusive Provider timeout has two possible public outcomes

FR-12 makes `Indeterminate` a terminal generation outcome when the owner cannot establish whether the attempt succeeded. AD-13 also instructs the workflow to record `GenerationFailed` on a Provider timeout. The diagram records uncertainty and exits the branch without naming the public outcome. Accounting can legitimately remain open after the attempt has a terminal outcome. [FR-12, line 371](../../prds/prd-agents-2026-06-23/prd.md) · [AD-13, lines 443–445 and sequence, lines 875–894](ARCHITECTURE-SPINE.md)

**Architecture correction.** Reserve `GenerationFailed` for confirmed failure. Record `AgentGenerationOutcome.Indeterminate` once when reconciliation cannot settle the Provider outcome. Keep the reservation under AD-21 and suppress retries while usage is unknown. Verify the public result both before and after financial settlement.

## Medium findings

### M1 · A pre-acceptance safety refusal has two public statuses

FR-8 assigns a step-9 policy refusal the public status `Blocked` with a typed reason. AD-13 and Story 6.4 allow `SafetyFailed` before `AgentCallAccepted`. The same request can therefore produce different API responses, audit records and metric classifications. The open decision for safety of generated output concerns a later phase. [FR-8, lines 302–308](../../prds/prd-agents-2026-06-23/prd.md) · [AD-13, line 447](ARCHITECTURE-SPINE.md) · [Story 6.4, line 2121](../../epics.md)

**Architecture and story correction.** Specify separate public status, internal safety result, typed reason and ledger effect for a refusal before acceptance. Test the API, projection and budget/caller-lease outcome against one expected row.

### M2 · The allocator has no transition for a same-attempt retry

The initial transition changes the capacity lease from `Admitted` to `InvocationActive`. After confirmed no-use, AD-21 permits bounded retry against the same lease and fence. AD-24 requires the allocator transition before transport, yet the diagram checks the fence at Workflow and dispatches. Implementations can disagree on whether to reject the retry or accept it against a lease that was already reclaimed. [AD-21, line 535, AD-24, line 583 and sequence, lines 882–889](ARCHITECTURE-SPINE.md) · [Capacity contract, line 465](../../launch-readiness-register.md)

**Owner decision.** Architecture and the capacity owner should specify how a retry is authorized atomically against the original active fence. Exercise retry racing lease reclamation, two claimants, and restart. Preserve the original request identity and reservation.

### M3 · The administrative retry verdict omits the overall posting window

`CanAdministrativeRetry` checks remaining attempts, the adjusted 15-minute retry limit, eligibility and lookup evidence. AD-5 and FR-18 also forbid retry once the cumulative posting window has elapsed. A reply can spend most of its allowed time in `Approved`, then reach `PostingFailed` shortly before the overall bound. The UI can still offer a retry that the command must refuse. [AD-5, lines 312 and 318](ARCHITECTURE-SPINE.md) · [FR-18, lines 465 and 477](../../prds/prd-agents-2026-06-23/prd.md)

**Architecture correction.** Include the adjusted overall posting-window check in the shared server verdict. Test the instant on each side of that bound with attempts still available and the first failure less than 15 minutes old.

### M4 · First export-key delivery can race deletion of the artifact

A custodian can hold a valid, undelivered download authorization when deletion seals the corresponding export. The contracts govern grant expiry, actor revocation and artifact purge. They leave open whether that first key delivery is still allowed between the seal and physical destruction. An inspector may already possess the encrypted bytes. [AD-12, line 417 and AD-22, lines 545 and 565](ARCHITECTURE-SPINE.md) · [Secrets and export-store contracts, lines 228 and 246–249](../../external-dependency-register.md) · [Download variants, lines 303–305](../../launch-readiness-register.md)

**Owner decision.** Governance, Security and the custodian should name the exact point at which deletion changes an outstanding delivery grant and how the custodian orders the two actions. Cover both orderings and a lost delivery acknowledgement.

## Low finding

### L1 · The class diagram leaves export ownership ambiguous

`AuditExport.ArtifactKey` can be read as a storage locator or key material. The diagram also omits the relationship to the authoritative `ProtectionFence` export commit. AD-22 gives the fence ownership of the commit and index, with acknowledgement required before download. [Export class, lines 1309–1317 and relationships, lines 1399–1406](ARCHITECTURE-SPINE.md) · [AD-22, lines 545 and 565](ARCHITECTURE-SPINE.md)

**Diagram correction.** Name the safe storage locator and show the reference to the fence commit. A crash between commit and acknowledgement should leave the copy discoverable to deletion and its key unavailable for download.

## Implementation and evidence

**High — Provider eligibility can rely on an unresolved secret handle.** AD-9 requires a durable, server-observed resolvability result. The aggregate maps any nonblank reference to `Configured`. The catalog and tenant view can then expose an enabled entry as selectable if its other checks pass. The September review had already identified the missing probe. The current trace confirms the effect on tenant eligibility. Story 5.3 should track the source of this status and exercise missing, unresolved, denied and resolvable handles. [AD-9, line 389](ARCHITECTURE-SPINE.md) · [Aggregate, lines 364–373](../../../../src/Hexalith.Agents/ProviderCatalog/ProviderCatalogAggregate.cs) · [Selectability, lines 85–95](../../../../src/Hexalith.Agents/ProviderCatalog/ProviderCatalogInspection.cs) · [Tenant view, lines 100–116](../../../../src/Hexalith.Agents.Server/Projections/TenantProviderCatalogViewFactory.cs)

**Medium — The retired provider-status route can return HTTP 503 before its documented 422 refusal.** Both prefixes inherit the dependency filter. In ordinary authenticated composition, the default availability implementation returns a blocker before the static legacy handler runs. The 422 fixture substitutes availability. The inventory test also compares routes produced by the same mapping method, so removing the same endpoint from both prefixes can pass. Story 5.11 should exercise the default composition, compare versioned routes against an independently maintained supported inventory, and clarify the HTTP alias retirement separately from event-stream migration. [AD-15, line 471](ARCHITECTURE-SPINE.md) · [Router, lines 30–47](../../../../src/Hexalith.Agents.Server/Api/AgentsOperationEndpoints.cs) · [Dependency filter, lines 15–26](../../../../src/Hexalith.Agents.Server/Api/AgentDependencyAvailabilityFilter.cs) · [Default availability, line 9](../../../../src/Hexalith.Agents.Server/Ports/CommittedAgentDependencyAvailability.cs) · [Route fixtures, lines 54–65 and 249–261](../../../../test/Hexalith.Agents.Server.Tests/AgentsOperationEndpointsTests.cs)

**Low — A tracked folder marker points future MCP adapters toward the Agents module.** The McpCli amendment gives that surface to McpCli. The marker is the discrepancy. The review found no executable adapter in that folder. Track the obsolete marker as cleanup under AD-19. [Ownership amendment and AD-19, lines 178 and 519](ARCHITECTURE-SPINE.md) · [Tracked marker](../../../../src/Hexalith.Agents.Server/Application/Tools/.gitkeep)

## Release evidence

**Four committed dependencies, zero available.** Parties, Conversations, Secrets and Host still require successful owner qualification at their recorded targets. The recorded build evidence covers 6,667 Debug/source tests, 6,581 Release/package tests and seven package-consumer selections. Hosted CI, a same-revision nonpublishing release rehearsal, configured Platform reads and live owner receipts remain outstanding. This validation ran document checks and static source checks. The build counts come from the November verification record. [External dependency register, lines 40–71](../../external-dependency-register.md) · [November verification, lines 38–44](../../sprint-change-proposal-2026-11-20.md) · [Launch-readiness register](../../launch-readiness-register.md)

Two posting defects are already assigned in the spine. The approval orchestrator can append externally before the approval and pending intent commit. The source adapter can accept a matching message with missing provenance. Both defects remain open in the reviewed source. The delayed-append finding above applies even after those two fixes. [Delivery debt, lines 1469–1470](ARCHITECTURE-SPINE.md) · [Approval orchestrator, lines 95–105 and 173](../../../../src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionProposalApprovalOrchestrator.cs) · [Posting adapter, lines 165–173](../../../../src/Hexalith.Agents.Server/Ports/ConversationClientResponsePoster.cs)

The prior update’s corrections hold for the cases it checked. Confirmed-no-use retries keep their existing budget reservation. At hold expiry an unresolved amount becomes `Unreconciled`. Early timer wakeups cannot advance the stored business deadline. Both posting modes require a committed generated version, and an empty approver set stops admission before the context read. The PRD and readiness owners still have their recorded assumption-retirement and metric-definition work. [AD-5, AD-8, AD-17, AD-21 and AD-28](ARCHITECTURE-SPINE.md) · [PRD validation](../../prds/prd-agents-2026-06-23/validation-report.md)

## Verification record

- The BMAD spine linter reported **zero findings**. All 31 decision IDs and titles match the parent revision. All local files named in the spine’s source list exist.
- Mermaid parsed both flowcharts, the sequence diagram and the class diagram. The behavioral findings above concern what the diagrams instruct engineers to do.
- The repository parent is `05ddee81529d89950182bf35aaf6e12035ccaaed`. The reviewed spine’s SHA-256 is `7189848862828724fcf4f0f60cdc80fb28d9c6e333e36e16b847743f1a25a7ed`. Package selection was checked against the parent-pinned Builds catalog.
- The spine and its decision log were unchanged by validation. Source line references refer to this working copy.

[Rubric review](reviews/review-2026-11-20-validate-rubric.md) · [Verified-current review](reviews/review-2026-11-20-validate-verified-current.md) · [Adversarial review](reviews/review-2026-11-20-validate-adversarial.md) · [Data-integrity review](reviews/review-2026-11-20-validate-data-integrity.md)
