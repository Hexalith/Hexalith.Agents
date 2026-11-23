---
title: 'Five deferred changes to attempt context evidence'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'b9d39e3ad4fab7fea773db82c8664d7311f71fe5'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete the two open context-attempt evidence entries through five changes to the existing generation and regeneration paths, following FR-9, FR-24 and AD-11.

1. Preserve the safe fresh-read classification when generation or regeneration refuses context. Keep the existing attempt outcome and terminal status.
2. Record `Full` for a measured fit and `Blocked` with a typed reason for a read refusal, unavailable measurement or oversized context. Preserve provider-catalog refusal semantics when the budget was never evaluated.
3. Record the fresh measured count, model capability and window, output reserve, safety margin and remaining budget on a measured attempt. Include oversized attempts and preserve measured zero. Omit budget terms when no valid measurement was obtained.
4. Preserve the evidence on replay of both successful and failed generation and regeneration events. Older results and events may omit it.
5. Expose the same safe evidence through the existing authorized attempt inspections. It contains no Conversation content or identifiers, prompt, provider payload or diagnostic exception.

The existing attempt events capture the outcome after orchestration. The pre-invocation checkpoint, full prompt and framing terms, and owner qualification remain in the Epic 4 reconciliation.

</frozen-after-approval>

## Implementation Notes

Generation and regeneration share the readiness policy's read classifications and budget arithmetic. The attempt evidence distinguishes a refused read, a failed exact measurement, a measured overflow and a measured fit. It is recorded on successful and failed outcome events and retains measured terms after a subsequent provider or safety refusal.

Initial generation context is available through the existing scoped context inspection. Regeneration context is available through the scoped regeneration inspection. A stale audit read omits the new fields. Successful regeneration clears the prior attempt's failure reason so the latest classification matches its context evidence.

## Review Triage Log

- Medium, fixed. Initial context metadata follows the existing FR-24 context grant. The tenant-operations generation query contains the operational attempt fields.
- Medium, fixed. Both scoped query handlers withhold new attempt context details on stale reads.
- Medium, fixed. Both orchestration suites replay serialized denied, stale, unusable and oversized failures and inspect the resulting evidence and status.
- Low, fixed. Catalog refusal controls verify omission of attempt context and no tokenizer call.
- Medium, fixed. Literal historical success and failure events for both paths replay with their outcomes and absent attempt context.
- Low, fixed. The context contract disclosure test covers the two new records.

## Verification

The Debug source solution built with zero warnings and errors against the committed dependency revisions. All 279 focused Server, domain and contract tests passed with zero skips.
