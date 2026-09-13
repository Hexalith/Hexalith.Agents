# UX Reconciliation — Architecture Update — 2026-09-12

- **Subject:** `ARCHITECTURE-SPINE.md` (`updated: 2026-09-12`, `ARCH-A-INDEX-6`)
- **Inputs:** final `EXPERIENCE.md`, final `DESIGN.md`, `reconcile-validation-2026-09-10.md`, and the current external-dependency and launch-readiness registers.
- **Method:** load-bearing contract reconciliation only. Presentation detail that the UX spine already owns was not treated as missing merely because the architecture spine does not repeat it.
- **Source artifacts modified:** none.

## Verdict

**The seven Architecture / Architecture + Conversations items deferred in reconciliation section 8 are closed (7/7). The architecture is not yet fully reconciled to the rest of the UX update.** Two domain/public-contract gaps remain material: the Provider data-handling/tenant-acceptance model is almost wholly absent, and `MirrorRefused` is named without its durable/public semantics. AD-31 also leaves live-region ownership ambiguous against the UX's explicit no-dialog-node correction.

Several UX passages are now stale because this architecture update deliberately chose one side of their former open question. Those source contradictions require a later UX update; they are not reasons to reverse the architecture decisions.

## Section 8 closure matrix

| # | Deferred item | Verdict | Evidence in the updated architecture/registers | Required source follow-up |
|---|---|---|---|---|
| 1 | Whether `ConversationPosting` is lock-bearing | **Closed** | AD-12's fourth-update classification makes `ConversationPosting` workflow-only and outside browser-session advisory locking; it relies on expected revisions plus AD-13/AD-29 deterministic idempotency. | Remove the UX statement that the family remains undecided and is conservatively treated as lock-bearing. |
| 2 | Administrative retry had no valid family | **Closed** | AD-5 classifies the public administrative-retry request as lock-bearing `ProposalResolution`, requiring no Conversation read access. Its accepted workflow lookup, re-validation, and post steps declare `ConversationPosting`. | Remove the UX open-question qualifier from the administrative-retry confirmation row. |
| 3 | `CapacityQueued` queue position had no contract | **Closed by explicit non-disclosure** | AD-24 exposes durable `QueueId` and queued state, but no ordinal position because it is unstable under weighted cross-tenant scheduling and could disclose other-tenant activity. | Remove “queue position where safe” from the Agent-call state and UX-J6. Keep status plus `QueueId` where disclosure allows. |
| 4 | `CurrencyMismatch` absent from the binding vocabulary | **Closed** | AD-10 requires the additive code, AD-21 binds its tenant-scoped blocking semantics, and the launch-readiness register's `ProviderReadinessReasonCode` list now contains `CurrencyMismatch`. | Remove the corresponding Known-gap row. |
| 5 | `EntryMissing` carve-out differed for historical interactions | **Closed** | AD-2 and AD-10 now permit it only for the Agent's current selection or an in-flight interaction's prior snapshot. Terminal history uses immutable snapshot plus authorized Audit Evidence. The register says the same. | Add the terminal-history rendering rule to UX and remove the section-8 deferral. |
| 6 | `ARCH-A-13` regeneration-requester exclusion lacked Product authority | **Closed / retired** | AD-8 includes the regeneration requester in the Eligible Approver exclusions; current PRD FR-7 now says the same; `ARCH-A-13` is retired on 2026-09-12. | Update the UX's five-conjunct predicate to six, add its safe reason/copy, and remove the provisional Known-gap wording. |
| 7 | `EXT-CONV-UI-1` lacked the persistent region and focus-return contract | **Closed** | AD-31 and `external-dependency-register.md` now agree on four artifact kinds—action contribution, `MessageId` decoration, `GetCallabilityAsync`, persistent status region—plus deterministic focus return. The Agents provenance accessor is the sole decoration source. | Replace the UX's “three artifact kinds” wording and its claim that the register amendment remains open. |

## Material requirements that did not land

### M-1 — Provider data handling and tenant acceptance are unbound — Critical

The UX binds a complete FR-4/OQ-29 model, but the architecture has no `DataHandlingVersion` occurrence and no decision that assigns its durable state or runtime behavior. The missing contract includes:

- the four Provider/model fields: retention term, training-use status, processing region, and retained contractual reference;
- `DataHandlingVersion`, incremented only when those fields change;
- refusal to enable an incomplete record;
- the tenant's accepted version and the version currently in force;
- immediate `DataHandlingAcceptanceLapsed` activation/call blocking by default;
- the Platform-Operator-declared tightening exception, its field-level diff, and its 30-day grace;
- Accept and Decline behavior, with Decline blocking immediately;
- Provider-attempt and Audit Evidence recording of both the current record version and version in force.

The current spine mentions `DataHandlingAcceptanceLapsed` only as an exclusion in the trigger calculation. That does not establish the state, the gate, or the ownership needed to produce it.

This also leaves a register-level incompatibility: UX issues `DataHandlingAcceptance`, but `OperationGateMatrixVersion = 2` has no such operation family, and AD-12 neither maps it to an existing family nor classifies its advisory locking/idempotency behavior. Before implementation, Architecture must decide whether this is a new family or a named operation under `TenantProviderEnablement`/another existing family, then amend the launch-readiness register consistently.

### M-2 — `MirrorRefused` lacks a complete durable/public contract — High

AD-7 mentions re-admission under the same `MirrorPending`/`MirrorRefused` terms, but nowhere defines those `MirrorRefused` terms. AD-2's aggregate inventory carries `MirrorPending` only, and AD-15's public-parity list also omits `MirrorRefused`.

The UX requires a typed permanent Conversations refusal to end retry, set a terminal mirror flag while leaving the block in force, expose the flag and its count, and offer exactly two Tenant Agent Administrator recoveries: re-set the block or clear it. Without those rules, separately built aggregate, projection, API, and UI units can choose incompatible terminal and remediation behavior.

### M-3 — AD-31 is ambiguous about Conversation live-region ownership — High

The final UX correction is explicit: `ConversationAgentCallPanel` owns **no** live-region nodes; in-dialog validation, `submitted`, and every post-submit outcome are sent to the single persistent contributed region whose nodes exist empty from first paint. This prevents duplicate/missed announcements and protects NFR-14 evidence.

AD-31 instead says the persistent region is for outcomes after the dialog closes, then says the panel “announces only outcomes produced while it remains open.” That can reasonably be implemented as a second dialog-owned speaker, recreating the defect the UX reconciliation closed. AD-31 should state that the panel emits all status to the persistent region and owns no speaker node.

### M-4 — Operational-status public parity remains partial — High

AD-15's FR-25 counter list omits UX-required classes that affect recovery and launch-governance surfaces: capacity queue/reject, `DataHandlingAcceptanceLapsed`, `TenantSuspended`, `MembershipRejected`, dependency/protection unavailability, authorization-denial counts, and `MirrorRefused` counts. Some tokens exist elsewhere in the spine, but the authoritative operational projection/public-contract obligation does not. The result can be a domain that records the outcome while the status projection required by UX cannot expose it.

## Material source contradictions exposed by the update

These are UX-source corrections, not missing architecture decisions:

1. **Eligible Approver predicate:** UX still states five conjuncts and omits the regeneration requester; AD-8 and current PRD FR-7 now bind six.
2. **Queue position:** UX still promises “position where safe”; AD-24 now deliberately forbids an ordinal position in V1.
3. **Retry actor:** UX offers an Approver-driven public `ConversationPosting` retry. The architecture/current PRD bind automatic system retry within budget and an audited Tenant Agent Administrator retry after that budget; they do not grant the Approver a retry command. This is a pre-existing behavioral contradiction that the family-closure wording makes more visible and should be corrected in UX.
4. **Reconciliation state:** `reconcile-validation-2026-09-10.md` section 8 still labels all seven items open, and the UX Known-gaps/Conversation-seam text still says the CurrencyMismatch and `EXT-CONV-UI-1` register amendments are pending.

## Reconciliation disposition

- **Closed from section 8:** 7 of 7.
- **Architecture changes still required before final handoff:** M-1 through M-4.
- **UX source synchronization required afterward:** the four contradictions above, while preserving the architecture decisions and stable AD ids.

## Post-fix verification — 2026-09-12

**Verdict:** All seven section-8 Architecture / Architecture + Conversations gap-register items remain closed. Of the four material residuals above, M-1, M-2, and M-4 are now closed; M-3 remains partially open as one exact live-region ownership contradiction.

### Seven-item closure recheck

| # | Post-fix verdict | Current evidence |
|---|---|---|
| 1 | **Closed** | AD-12 classifies workflow-only `ConversationPosting` outside browser-session advisory locking. |
| 2 | **Closed** | AD-5 keeps the administrative request in lock-bearing `ProposalResolution`; its accepted lookup, re-validation, and posting activities declare `ConversationPosting`. |
| 3 | **Closed by explicit non-disclosure** | AD-24 exposes durable `QueueId` and safe queued state while forbidding an ordinal queue position in V1. |
| 4 | **Closed** | `CurrencyMismatch` remains in AD-10 and the launch register's binding `ProviderReadinessReasonCode` vocabulary. |
| 5 | **Closed** | The spine and launch register restrict `EntryMissing` to current selection or an in-flight snapshot; terminal history uses immutable snapshot plus authorized Audit Evidence. |
| 6 | **Closed / retired** | AD-8 excludes the regeneration requester from approving that version, and `ARCH-A-13` remains retired by the 2026-09-12 Product decision. |
| 7 | **Closed** | AD-31 and `EXT-CONV-UI-1` retain four artifact kinds, the sole Agents provenance source, the persistent region, and deterministic focus return. |

### Residual-finding recheck

| Finding | Post-fix verdict | Current evidence or exact residual |
|---|---|---|
| M-1 | **Closed** | AD-2/AD-10/AD-12/AD-13 now bind the four-field record, monotonic `DataHandlingVersion`, tenant accept/decline ownership, refusal of incomplete enablement, immediate lapse blocking, declared-tightening field diff and 30-day grace, decline behavior, and both versions in Provider-attempt/Audit Evidence. The launch register adds lock-bearing `DataHandlingAcceptance` in `OperationGateMatrixVersion = 3`. |
| M-2 | **Closed** | AD-7 now makes typed permanent refusal terminal for that mirror operation, durably sets `MirrorRefused` while the block remains authoritative, exposes it through aggregate/public status/metrics, counts it for SM-C4, and defines exactly the re-set-block and clear-block remediations. |
| M-3 | **Residual — High** | AD-31 says the panel owns no **post-submit** live-region node and still permits it to announce an outcome while open. The load-bearing UX contract says `ConversationAgentCallPanel` owns **no live-region nodes at all** and sends in-dialog validation, `submitted`, and every later state to the persistent contributed region. `EXT-CONV-UI-1` correctly assigns `submitted` and later states to that region, but does not remove AD-31's permission for a second panel-owned announcer. Close by stating that the panel emits every validation/status announcement to the persistent region and owns no speaker/live-region node. |
| M-4 | **Closed** | AD-15's operational-status parity paragraph now requires one shared projection vocabulary across API, BFF, UI, and metrics, including acceptance lapse, authorization denials, lifecycle and suspension, membership rejection, capacity, dependency/protection unavailability, abandonment/resolution markers, and `MirrorPending`/`MirrorRefused`. |

**Post-fix disposition:** 7/7 original gap-register items closed; 3/4 former residuals closed; M-3 is the sole material residual.

## M-3 corrective verification — 2026-09-12

**Closed.** AD-31 now states that `ConversationAgentCallPanel` owns no live-region node at all and routes validation, submission, and every later announcement through the persistent status region. `EXT-CONV-UI-1` matches: its required artifact assigns validation, submitted, authoritative-pending, terminal caller outcomes, and authorized-Approver pending state to that persistent region; its compatibility contract names the persistent region as the sole live-region owner and requires validation/submission/caller/Approver disclosure tests.

**Final post-fix disposition:** 7/7 original gap-register items closed; 4/4 former material residuals closed; no material UX-to-architecture residual remains in this reconciliation scope.
