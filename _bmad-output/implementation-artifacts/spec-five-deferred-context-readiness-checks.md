---
title: 'Five deferred conversation context readiness checks'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '491969d6ceb2be09cbe989f3bb08f74964697a1f'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Address five checks from the Epic 4 runtime reconciliation action and Story 6.2 in the existing conversation context builder.

1. Require a fresh, complete visible timeline before measuring its messages.
2. Refuse invalid token counts and output allowances when deciding whether the context fits.
3. Bind the catalog evidence to the exact requested provider and model.
4. Enforce the recorded snapshot capability floor and preserve the accepted live version in the durable context evidence.
5. Require the current catalog entry to pass the existing shared selection policy, including tenant eligibility, configuration and pricing.

Cover each refusal through its owning entry point and verify a valid higher-version result through event replay. The broader runtime action continues to track generation-time revalidation and durable attempt checkpoints.

</frozen-after-approval>

## Implementation Notes

The context builder checks the visible timeline before tokenization and reuses the provider selection verdict for catalog eligibility. The aggregate checks the measurement against its own persisted capability floor. Both the command and its resulting event retain only safe scalar evidence.

## Review

- The full-detail adapter rejects purpose-limited responses and disagreement between the outer and inner freshness reason. Both refusals have adapter tests.
- Missing message records and null or blank text stop measurement. The budget policy accepts zero measured tokens on an empty timeline and blocks that count on a populated timeline.
- The submission result describes the local evaluation. A regression with differing request and persisted floors verifies that the recorded decision follows the persisted snapshot. A valid higher-version case reconstructs state from the serialized ready event.
- A rejected model budget clears the capability version and limits from durable evidence. The aggregate tests exercise this directly.
- The sprint action retains its owning story references and tracks the generation-time work separately.

## Verification

On the original implementation, 17 new aggregate and orchestrator regression cases failed. The corrected Debug source solution built with zero warnings and errors. All 87 final cases across the context aggregate, server orchestrator and Conversations adapter test classes passed with zero skips.
