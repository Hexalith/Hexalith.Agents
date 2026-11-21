# Adversarial review of the November 21 architecture update

**Initial verdict.** Three changes are needed to align the revised sequence, authority rules and owner evidence. The caller-slot release, active-capacity restriction and export-key guard have the same outcome across the updated spine, matrix and story fixtures.

| Priority | Replay or concurrency case | Required correction |
| --- | --- | --- |
| High | Two users request regeneration at the same proposal revision. The sequence at lines 1014–1019 sends regeneration to Workflow before the common target result. AD-8 and Story 7.3 allow just the winning durable authorization to start any of those checks or Provider work. | Commit or recover the expected-revision `RegenerationAuthorized` event before scheduling the winning attempt. A losing request or lost commit acknowledgement must resolve the target receipt first. Preserve single-command steps and avoid a second authorization at the shared result arrow. |
| High | The worker dies after confirmed Provider success and before any generated-output child lease exists. AD-27 directs the Interaction workflow to create the no-child fence. AD-30’s closed allowlist permits acquiring, committing and settling a child. Settlement requires an existing committed child. The new close command otherwise lacks a principal grant. | Bind the close variant to the exact Interaction workflow, committed parent Provider lease, original attempt, intended version and source. Check the evidence for confirmed success and loss of available original output before closing an absent or reserved child. Recover the existing committed child when it won. Add these direct checks to the matrix. |
| High | The owner observes absence, a delayed append commits, and the owner then closes the old dispatch. At that point both the absence observation and proof of closure exist, but the reply is posted. The updated story’s requirement for absence covering closure rules this out. The spine, common matrix row and negative sequence arms list the two observations without ordering. | Require an authoritative absence observation covering the closure of every earlier dispatch, or an equivalent owner attestation that none committed. Bring the same ordering to the seam note and both dispatch modes. A durable match overrides a stale negative observation. The OQ-36 mechanism stays open. |

Sources include [AD-6, AD-8, AD-27, AD-30 and the sequence](../ARCHITECTURE-SPINE.md), [matrix v8](../../../launch-readiness-register.md#operationgatematrixversion-8--caller-provider-posting-and-custody-decisions), [Stories 6.6 and 7.3–7.4](../../../epics.md), and the [Conversations owner contract](../../../external-dependency-register.md).

## Checks that held

- H1. A Confirmation proposal awaiting human review releases the original caller slot once at the committed ProposalCreated revision. The proposal remains reviewable. Automatic posting and initial failure use their own terminal events. A later regeneration cannot claim or release that slot.
- H2. At a one-slot capacity limit, a terminal Indeterminate attempt retains the active tenant and system claim across allocator restart and budget settlement. A second dispatch requires owner-backed closure of the active attempt.
- H3. The content-custody activity can coordinate separately identified, single-command steps. The generated version and OutputUnavailable contend at the owning interaction. The no-child fence prevents a delayed reservation. A committed child settles using the winning target revision. The missing principal grant above affects whether the recovery step can run.
- H4. A human edit gets classified before the new version can be stored. Always-blocked or unavailable results preserve the prior version, and a permitted restricted edit carries the marker and cannot be approved. The remaining sequence finding concerns regeneration ordering.
- H7. First key delivery, including a retry after NotDelivered, requires the same custodian ordering check. A matching earlier Delivered receipt is recovered under its original identity after a seal, loss of authority or expiry.
- Live owner qualification, existing implementation debt and the actual policy outcomes for OQ-35 through OQ-38 remain separate. This review inspected documents and traced replay cases. It supplied no runtime qualification.

## Final disposition · 2026-11-21

**Passed. All three findings are closed.** The sequence now commits or recovers the expected-revision regeneration authorization before notifying Workflow. The action branch excludes regeneration from the later result write, and only the winner resumes. This agrees with AD-8 and Story 7.3.

AD-30 and matrix v8 now grant child closure only to the original Interaction workflow using its committed Provider lease. They bind the tenant, source, intended version, attempt and child identity, and require confirmed Provider success and proof of lost output. A missing or ambiguous receipt leaves recovery pending. Story 6.1 and the implementation companion use the same fenced child and target receipts.

AD-6, the matrix, both posting branches, the Conversations contract, the companion and the story cases require an authoritative observation that covers closure of all possible earlier dispatches. If the message commits after a first absence but before closure, the subsequent observation confirms the post. The earlier absence cannot authorize a negative exit. No further correction is required for these three findings.
