---
title: 'Five deferred provider refusal changes'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'd4b808743f5c5630b2ec49b9145981f6fb295b29'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Complete the local Story 6.1 provider-refusal path under AD-13 through five changes.

1. Carry the typed Open, Missing, Malformed and Unavailable authority classifications from the read to the immutable refusal event.
2. Refuse malformed or inconsistent authority results before provider invocation. Record an observed deletion Closing independently of an authority read.
3. Enforce the refusal shape on new aggregate commands and preserve historical events and exact replays.
4. Record the refusal and the Budget no-use decision, then require the allocator's exact no-invocation lookup before recording capacity cancellation or settling the Provider lease.
5. Verify that recovery resumes the pending lookup after restart, preserves the recorded authority decision and never starts the Provider from a refusal.

The existing owner ports carry the local protocol. Installed decision-catalog and allocator qualification depend on Stories 6.8 and 6.5.

</frozen-after-approval>

## Implementation Notes

The Provider refusal and Budget disposition share one append. Capacity cancellation is a later checkpoint with the exact admission ID and the allocator record reference. A missing or foreign record leaves the workflow pending. Legacy events replay with absent fields. Settlement of a legacy cancellation receives fresh allocator evidence through the owner port.

## Review Triage Log

- High, fixed. All capacity cancellation branches require an owner record for the exact admission. Historical cancellation settlement receives fresh evidence when the old fact lacks it.
- High, fixed. A contradictory authority result and a status outside the two governed choices produce a malformed refusal before the Provider runs.
- Medium, fixed. An absent refusal code denotes historical or Closing evidence. An explicit unknown value is rejected on a new command.
- Medium, fixed. The Open fixture supplies a distinct owner observation reference. The aggregate requires it to accept Open.
- Medium, fixed. Command tests reject invalid provenance and distinguish exact replay from a divergent duplicate. Recovery tests cover unavailable or foreign allocator results and crash after a cancellation append.
- Low, fixed. All three allocator lookup paths report the same retryable activity error when exact evidence is unavailable.

## Verification

The Debug solution built against the committed dependency revisions with zero warnings and errors. All 41 focused server and domain tests passed with zero skips.
