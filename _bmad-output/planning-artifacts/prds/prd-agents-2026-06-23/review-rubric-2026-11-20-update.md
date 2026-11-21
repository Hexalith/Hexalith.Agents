# PRD quality review — Hexalith Agents, 2026-11-20 update

## Overall verdict

The revised PRD gives the release owner a clear decision and gives testers concrete evidence to collect for posting and disclosure. The final recheck closed the sole medium finding. All seven dimensions are strong for the November update, and the live qualification work in §11 remains open.

## Decision-readiness — strong

§0 records `RQ-1` as **NOT READY**. §8 names the four committed dependencies, identifies who may qualify them and points to the exact owner records. §8.1 distinguishes the confirmed Branch B identity contract from the outstanding FR-28 retirement observation and the separate live qualification. A maintainer can proceed with the development milestone while the release owner can identify the missing evidence.

## Substance over theater — strong

FR-9 gives QA two independent negative cases, a wrong tenant and a wrong Conversation. FR-24 identifies a distinct disclosure failure when a grant expires during an asynchronous read. The addendum attributes the test counts and synthetic recovery to the parent revision on which they were measured.

## Strategic coherence — strong

UJ-2 depends on the reader being able to trust an attributed reply. UJ-3 depends on only the selected version reaching the Conversation. The common durable message check in FR-18 supports both journeys. SM-4 and SM-5 retain the original measures of authorization safety and audit completeness.

## Done-ness clarity — strong

FR-18 specifies the required message identity, author, tenant, Conversation and content. FR-23 requires a full route inventory and a specific result for the legacy lookup. FR-24 specifies the authorization checks before a read and immediately before disclosure.

### Findings

- **Medium, resolved — Participant outcome after an owner refusal (FR-11, FR-18).** The initial wording kept the participant’s reply pending “until Conversations confirms” and left the rejection case ambiguous. FR-11 now limits that display to uncertainty. In both modes, FR-18 requires a definitive owner refusal and authoritative message absence before reporting `PostingFailed` with its typed reason. §11 tests the rejection separately from delayed confirmation and specifies a restart after Conversations persists an automatic reply but before Agents receives its acknowledgement. [Posting requirements and qualification cases](prd.md) · [Workflow qualification](../../launch-readiness-register.md#module-capabilities-and-owner-qualification)

## Scope honesty — strong

The October custody amendment still identifies the work that can proceed and the capabilities that require qualified custody. §11 names the Platform process, configured read, protected disclosure, durable posting and restart checks. The addendum identifies the outstanding hosted run and release rehearsal without treating the reported local runs as owner receipts.

## Downstream usability — strong

§8 links the owner records. §11 links each workflow to the responsible parties and evidence. The source and package test counts are in the addendum, where a reader can find their revision and validation scope without mistaking either for a functional requirement.

## Shape fit — strong

The existing capability groups serve product, UX and engineering readers. The update places the common posting contract in FR-18, the two authorization instants in FR-20 and FR-24, and qualification in §11. It preserves participant and approver journeys with named protagonists and introduces no new feature group.

## Mechanical notes

The FR, NFR, OQ, assumption, journey, metric and counter-metric ID series each have unique, consecutive definitions. The revised PRD and addendum’s relative links resolve to repository files. The November update preserves the distinction between `Approved`, `PostingPending`, `PostingFailed` and `Posted` in the state table.
