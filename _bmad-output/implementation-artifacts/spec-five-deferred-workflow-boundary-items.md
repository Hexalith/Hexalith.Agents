---
title: 'Five deferred automatic interaction workflow boundary checks'
type: 'bugfix'
created: '2026-11-22'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
baseline_commit: '4907f010c75125f8a2f1de591b346d2cfeaa5f68'
context:
  - '{project-root}/AGENTS.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five Story 6.1 review items leave the automatic interaction workflow's runtime and no-effect boundaries insufficiently verified.

**Approach:** Exercise the real default host's inactive dispatch and scheduling ports, every intake refusal, restart after Closing before creation, the default and opt-in Dapr service graphs, and malformed or mismatched workflow input. Use the existing replay fixture and test classes already selected by the Story 6.1 verifier. Close the five matching review items after verifying them locally.

</frozen-after-approval>

## Implementation Notes

- The first focused Release run passed 32 of 33 cases. The opt-in graph could not construct its scheduler because Dapr Workflow 1.19.1 registers the concrete `DaprWorkflowClient` only. The qualified registration now supplies that SDK client to the scheduler through its existing interface constructor.
- The existing Story 6.1 verifier already selects the four modified test classes. The tests resolve workflow services without starting a host or sidecar. The recovery case reconstructs state from the fixture's accepted event history after each restart.

## Review Triage Log

- Medium, patched. The scheduler preserves an explicitly registered host client and uses the concrete SDK client otherwise. Both qualified bindings have executable coverage.
- Medium, patched. The default service graph includes the EventStore SDK registrations and Agents composition used at startup.
- Medium, patched. Intake and recovery share the instrumented readers. Assertions count owner reads and cover both pre-directory refusals and the single Closing read.
- Medium, patched. Malformed inputs exercise the production workflow, scheduler and activity orchestrator. The tested fields include the protected-reference digest.
- Medium, patched. The reference tests start from the workflow input passed to the scheduler and compare it with the replayed directory and creation records.
- Patched. The five matching [Story 6.1 review items](spec-6-1-start-and-recover-an-automatic-interaction.md#review-findings) are marked complete on the strength of the local verification below.


## Verification

- Debug source used the published 6.6.1 module CLI's prepared source mapping. All 6,720 tests passed across the five projects.
- The Story 6.1 Release package verifier passed all 6,634 tests across the five projects. Both lanes built with zero warnings and executed all 35 cases across the four reviewed classes without skips.
- The EventStore package-floor verifier accepted all 13 selected rows at version 5.4.0. Workflow composition and recovery evidence is local.
