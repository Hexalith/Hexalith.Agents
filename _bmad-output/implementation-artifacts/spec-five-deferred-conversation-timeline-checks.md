---
title: 'Five deferred checks for Conversation timeline evidence'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '77a1f91107619633cb1ae1db180895cd62424532'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Close the local owner-wire portion of the deferred timeline-evidence entry. Agents readiness, generation and regeneration use the Conversations full-detail client, which currently converts an omitted timeline into a valid empty one.

1. Preserve whether full Conversation detail received a timeline before normalizing collection access.
2. Refuse an omitted or null timeline on the owner client and preserve its safe validation result.
3. Keep explicit empty timelines eligible for context and retain full nonempty timeline contents.
4. Preserve native membership, advisory detail and typed responses that carry no detail.
5. Verify the owner wire behavior and the Agents mapping from a refused HTTP response to unavailable context.

</frozen-after-approval>

## Implementation Notes

The Conversations client checks the wire payload before returning typed full detail. The public detail record retains its collection and equality semantics. Its ordinary full-detail route requires a single `messages` array and refuses responses tagged for a bounded purpose. The existing bounded readers keep their own validators.

The Agents source test drives the actual typed HTTP client against the corrected Conversations checkout. Both source and package tests verify the adapter's handling of a typed validation failure carried in HTTP 200. Package-mode Agents runtime uses the deferred reader. A live package composition needs both the corrected client and a bound reader.

## Review Triage Log

- Medium, fixed. A full-detail request rejects either bounded read purpose even when the owner supplies an empty array.
- Medium, fixed. Wire validation reads the JSON before collection normalization, so no record member participates in equality or hashing.
- Medium, fixed. The Agents adapter regression drives omitted, null and explicit empty fixtures through the actual client.
- Low, fixed. The nonempty control asserts the complete ordered list of two distinct messages, including identities, authors, timestamps and text.
- Medium, fixed. Package publication and a live binding in package-mode Agents are separate prerequisites.
- Medium, fixed. The ledger records wire-level coverage. Projection completeness still depends on the owner upstream of HTTP serialization.

## Verification

The Conversations reference is `ff669cfcb9349aabf15645dc1bc2a2db8741ea66`. All 76 client tests passed without skips at `d81e2ab1e788aa95c79b3f85ff0bbd05d609c052` using the pinned CLI's Release package preparation. The final rebase preserved the Conversations production, client test and build input bytes. The ownership check and solution build passed. The isolated compatibility restore stopped when NuGet attempted to write to a read-only home directory.

The Agents Debug source build completed with zero warnings and errors, and 211 focused context reader, readiness, generation and regeneration cases passed without skips. The Agents Release package build completed with zero warnings and errors, and all 17 adapter cases passed without skips.
