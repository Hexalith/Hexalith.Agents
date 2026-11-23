---
title: 'Five deferred catalog checks before provider invocation'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'fa214c48b5875592ea4688fcbae9ece7fa4c3439'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Apply the open Epic 4 runtime reconciliation action to the catalog read immediately before initial generation and proposal regeneration.

1. Match the returned provider and model identifiers exactly to the requested pair before classifying its state.
2. Require a positive snapshot capability version and a current version at least that high. Carry the accepted version into the adapter request, generated versions and attempt evidence, including post-invocation failure evidence.
3. Require current configuration, enabled status and text generation support.
4. Require current tenant selection eligibility and valid versioned pricing.
5. Require usable model and transport limits, including positive input space after reserving the output allowance.

Verify the five checks through both orchestration entry points. Preserve their existing typed refusal outcomes and check provider call counts. The sprint continues to track generation-time context measurement, persistent capability history and durable attempt checkpoints.

</frozen-after-approval>

## Implementation Notes

Both orchestrators read through the same catalog admission method. It uses the existing selection policy for configuration, pricing and tenant eligibility and the catalog aggregate's transport maxima. A matching disabled or non-text entry keeps its typed refusal. A foreign or regressed entry yields provider unavailable.

## Review Triage Log

- Medium, fixed. Replaying an initial generation failure discarded its safe attempt evidence. The state fold now retains the event's evidence and the authorized generation audit read returns it. A serialized failure event exercises this path.
- Medium, fixed. A negative request floor formerly leaked into failure metadata. Both paths record the existing zero sentinel for an invalid floor.
- Low, fixed. A safety-blocked provider response now verifies the invoked version alongside distinct prompt and output counts. The generation regression checks the resulting event through replay.
- Low, fixed. Both paths invoke the adapter at the inclusive timeout and retry ceilings with one input token of room. The regressions assert the exact transport settings and accepted version.
- Low, fixed. Separate zero input-price and zero output-price regressions exercise both entry points with tenant selection enabled.
- Low, fixed. The generation and regeneration request documentation now distinguishes the snapshot floor from the invoked version.

## Verification

The Debug source solution built with zero warnings and zero errors. All 99 cases across the two orchestration test classes passed with zero skips.
