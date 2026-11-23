---
title: 'Five deferred exact context measurement checks'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: 'f57902c959495e4e5a97392fa3eaf5f417da5eae'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Continue the open Epic 4 runtime reconciliation by measuring context readiness against the same Conversation payload and admitted model capability used for generation.

1. Reject incomplete, stale or malformed timelines before tokenization. Preserve the authorized caller and the typed load outcome.
2. Admit the current catalog entry before sending Conversation text to the tokenizer. Refuse mismatched identity, regressed capability and unusable limits.
3. Render the timeline with the same ordering and line separators as generation and regeneration. Give the tokenizer that exact payload and the admitted provider, model and capability version.
4. Refuse unavailable or invalid exact counts. Honor caller cancellation at every dependency boundary before dispatching the context decision.
5. Carry the measured count and admitted capability through the context command into replayable context evidence, with text kept in memory.

Use focused tests for rendering, admission, invalid measurement, cancellation and event replay. Live tokenizer qualification, the complete Safe Context Budget, durable capability history and attempt checkpoints remain part of the sprint action.

</frozen-after-approval>

## Implementation Notes

The readiness, generation, and regeneration orchestrators now share timeline validation, payload rendering, and exact measurement. Readiness passes the authorized caller’s Party and principal, admits the current catalog before measuring, and sends only safe scalar evidence to the aggregate. The existing line separator and chronological ordering match provider invocation.

## Review Triage Log

- Medium, fixed. A catalog `TaskCanceledException` with an active caller token yields a blocked context without sending content to either tokenizer method.
- Medium, deferred. The Conversations wire contract normalizes a missing timeline to empty before the Agents adapter can distinguish it. The deferred ledger records the owner-contract follow-up.
- Medium, fixed. Explicit tokenizer operation cancellation still propagates. A dependency timeout with an active caller token yields an unavailable measurement.
- Medium, deferred. Regeneration has historically passed an empty Party reference. The current adapter uses the caller principal. The deferred ledger records the qualification needed for a participation-aware adapter.
- Medium, fixed. Catalog refusal tests observe both tokenizer methods. Successful context readiness verifies the exact method alone.
- Medium, fixed. Null, negative and nonempty zero measurements leave the submitted count, budget and capability unset.
- Low, fixed. The command and tokenizer port documentation describe prior catalog admission and versioned payload measurement.
- Low, reviewed. The spec was in progress during blind review. The sprint entry links to the completed implementation and verification below.

## Verification

The Debug source solution built against the committed dependency revisions with zero warnings and errors. The three affected Server orchestration classes passed all 187 cases. The context aggregate class passed all 33 cases. All 220 focused cases ran without skips.
