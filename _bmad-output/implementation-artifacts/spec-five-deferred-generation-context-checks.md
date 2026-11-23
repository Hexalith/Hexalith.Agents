---
title: 'Five deferred checks on context before provider invocation'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'ccca05bf5f0fd57e9efd4f45fad67e598ac2d6a5'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Continue the Epic 4 runtime reconciliation by checking the Conversation payload immediately before initial generation and proposal regeneration.

1. Require a successful, fresh authorized Conversation read before using its content.
2. Reject a missing or malformed timeline, including a count mismatch or blank message.
3. Measure the exact payload against the admitted provider, model, and capability version. Missing, failed, or invalid measurement blocks the call.
4. Require the measured payload to fit the current context window after reserving the current output allowance. A payload at the remaining limit fits in full.
5. Carry the measured payload into the provider request and honor caller cancellation before invocation, including when a dependency returns after cancellation.

Prove each check through both entry points. Use the existing typed context refusal and keep refused content out of the command. The capacity check applies to the current provider payload. The governed Safe Context Budget also requires Instructions, system framing, and the policy margin. Live tokenizer qualification, capability history, and durable attempt checkpoints remain tracked by the open sprint action.

</frozen-after-approval>

## Implementation Notes

The existing Conversations adapter validates returned scope and freshness before projecting a timeline. The two orchestrators share a preparation routine for the authorized re-read, stable serialization, versioned exact-payload measurement, and capacity check. The existing tokenizer port keeps its timeline-count method and supplies a fail-closed default for exact payloads.

## Review Triage Log

- Medium, fixed. A Conversations transport timeout is an `InvalidContext` refusal. Caller and explicitly signaled operation cancellation still propagate.
- Medium, fixed. Cancellation is checked after catalog completion even when the catalog refuses admission.
- Low, fixed. Regeneration checks cancellation before interpreting a late approver denial.
- Medium, fixed. Default message timestamps are rejected under the same rule as the Conversations timeline contract.
- Medium, fixed. A measurement refusal identifies the admitted capability version and records zero provider usage.
- Low, fixed. A fresh empty timeline succeeds with an exact count of zero.
- Low, fixed. The fit boundary is tested with a new capability version and different context and output limits.
- Low, fixed. Existing timeline-only tokenizers cannot authorize generation through the default exact-payload method.

## Verification

The Debug solution built against the committed dependency revisions with zero warnings and errors. The initial generation, proposal regeneration, and composition test classes passed all 137 cases with zero skips.
