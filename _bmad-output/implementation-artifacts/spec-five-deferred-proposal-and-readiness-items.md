---
title: 'Five deferred proposal, directory and readiness items'
type: 'bugfix'
created: '2026-11-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '6017498b33d98f933c25cb82efc37dea8905390f'
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

Fix the proposal edit retry identified by the Epic 3 retrospective and exercise the shared-directory contention case from the Story 6.1 review. Publish the three outstanding conventions for proposal lifecycle, record-and-gate commands and static versus live setup readiness. Update the individual tracking entries against the implementation and executable evidence. Directory retention belongs to Story 8.3.

</frozen-after-approval>

## Implementation Notes

- Edit eligibility now uses the replayed proposal sub-state. A repeat of the last recorded negative edit decision emits no additional event. A fresh authorized edit appends one version and preserves the earlier versions.
- The directory regression sends typed JSON commands through the aggregate dispatcher. Two workflow instances observe one revision. It covers the losing reservation, refreshed acquisition, exact replay and independent commit. The deferred ledger keeps retention open.
- The architecture conventions and Epic 7 now reference the recorded readiness and proposal lifecycle rules. Four matching retrospective actions are closed in the sprint tracker.

## Review triage

- Medium, fixed. Replaying the same failed edit could append a duplicate failure. The test replays the recorded failure and supplies a distinct version identity for the new successful attempt.
- Medium, fixed. The proposal convention identifies current PostingFailed presentation and links to the governed recovery in Story 7.7.
- Low, fixed. The activation example distinguishes an already-active Agent with the expected version from a new transition and a stale-version refusal.
- Medium, fixed. The contention test now gives distinct submissions distinct message IDs and payloads and routes the exact retry through the dispatcher with the original envelope.
- Low, fixed. The setup guidance now explains the Active lifecycle flag and links to the configuration page.

## Verification

Both new proposal regression cases failed against the original eligibility guard. The corrected Release solution built with zero warnings and errors. All 31 focused proposal-edit and directory cases passed. The EventStore floor accepted all 13 selected package references at 5.4.0.
