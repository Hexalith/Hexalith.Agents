---
title: 'Five deferred context budget and read items'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '54bc11514d7dbf1175a062c224e49272b7a1e5fd'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Advance FR-9 and the Epic 4 runtime reconciliation with five local changes to context readiness and provider admission.

1. Apply the approved default 10% model-window margin to exact context checks in readiness, initial generation, and regeneration. Round upward to whole tokens and preserve the inclusive fit boundary.
2. Record the percentage, margin tokens, and resulting budget with the existing measured context evidence. Historical and unmeasured evidence can omit the new fields.
3. Record `Blocked` for new failed context decisions. Older recorded enum values retain their meaning.
4. Distinguish a definite unavailable read from an access denial or unknown load outcome in the safe block reason.
5. Pass the authorized regeneration requester's Party and actor together into the Conversation context reader. Refuse an absent requester Party before resolving or reading protected context.

The current provider port carries the measured Conversation payload. Separate Instructions, prompt and framing terms, margin configuration, and live owner qualification remain tracked separately.

</frozen-after-approval>

## Implementation Notes

Readiness and both invocation paths use the same arithmetic. The context event records the numeric margin terms after successful measurement, including for an oversized refusal. This is enforcement of the existing full-Conversation policy's approved default. Recorded terms distinguish the new decision from older events, which replay as written.

The Conversations adapter preserves authorization refusals from the typed detail payload and from HTTP 401 and 403 responses. The UI has English and French text for the new read-unavailable reason.

## Review Triage Log

- Medium, fixed. UI resource coverage now includes the new reason in both languages.
- Medium, fixed. HTTP 401 and 403 retain the unauthorized classification. The existing 503 control still expects unavailable.
- Medium, deferred. Final invocation attempts still collapse fresh-read refusals to `InvalidContext`. Their evidence contract needs a safe distinction between the underlying load outcomes.
- Medium, deferred. Exact budget terms on the final invocation attempt require an extension to durable attempt evidence. The readiness event describes its own evaluated budget.
- Low, addressed. The existing policy reference identifies full-Conversation behavior. The recorded margin fields identify new runtime enforcement while configurable policy versioning remains open.
- Medium, fixed. Readiness tests cover the inclusive boundary and the first excess token. Dispatch, aggregate replay and inspection distinguish read failure from authorization denial.
- Low, fixed. A literal historical payload verifies the prior mode and reason and the absence of margin fields.
- Low, fixed. Reader documentation and test fixtures reflect the new reason and allowance.

## Verification

The Debug source solution built against the recorded dependency revisions with zero warnings and errors. All five test projects reported 6,859 passing cases and zero skips. The four focused Server suites independently passed all 205 cases.
