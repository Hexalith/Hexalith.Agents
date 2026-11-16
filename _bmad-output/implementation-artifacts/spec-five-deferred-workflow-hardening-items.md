---
title: 'Five deferred automatic interaction workflow fixes'
type: 'bugfix'
created: '2026-11-16'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: '902c3bcbb0f4ac6762b869aea7753dae9831e0d4'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-6-1-start-and-recover-an-automatic-interaction.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five unfinished Story 6.1 review patches repeat generation work, spin on invisible writes, retry outages too briefly, present terminal refusals as pending, and lose diagnostics.

**Approach:** Complete those five patches as one workflow reliability batch. Independently review and verify the changes, then commit and push to main as requested.

## Boundaries & Constraints

**Always:** Preserve EventStore truth, operation/attempt identities, cancellation, isolation, Closing and phase-pinned safety. Keep workflow contracts content-free. Generation may reserve, commit and record within one activity, as already approved; other checkpoints retain their one-command limit. Use protected results and exact retry lookup. Use run-with-temp; retain useful logs outside scratch.

**Never:** Claim exactly-once external invocation from local counts, carry transient output across activities, log messages/payloads/stacks, invent live acceptance, implement separate owner/migration obligations, deploy, publish packages or suppress audits/tests.

## I/O & Edge-Case Matrix

| Scenario | Input/state | Required behavior |
| --- | --- | --- |
| Generation | Authorized Provider; mutation lease absent/reserved/committed | One effect call per uninterrupted activity; observe exact reservation/commit and persist one protected result |
| Closing/restart | Closing before/after commit; crash/lost acknowledgement | Discard losing output with content-free Provider failure; preserve winning commit and immutable retry identities |
| Delayed checkpoint | Append lags, rejects, or never appears | Bounded paced observation of the intended owner fact; safe retry on exhaustion; preserve cancellation |
| Retry policy | Repeated checkpoint failures | 100 attempts, initial 1 second, coefficient 2, maximum interval 1 minute |
| Terminal refusal | ProviderInvocationNotAuthorized or PreProviderCapacityDispositionDecided | Public status becomes Blocked through settlement/replay; preserve safety mappings |
| Activity failure | Error/cancellation or invalid input | Log only exception type and validated interaction ID (placeholder otherwise); retain safe Dapr exception without inner exception |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Agents.Server/Application/AgentInteractions/AutomaticInteractionStepOrchestrator.cs`: generation repeats before lease transitions; RefreshAsync misses directory-only progress.
- `src/Hexalith.Agents.Server/Application/Workflows/AutomaticInteractionWorkflow.cs`: WorkflowTaskOptions uses linear retry.
- `src/Hexalith.Agents.Server/Application/Activities/ExecuteAutomaticInteractionStepActivity.cs`: add source-generated LoggerMessage without an Exception argument.
- `src/Hexalith.Agents/AgentInteraction/AgentInteractionState.Automatic.cs`: add both Blocked mappings in ApplyAutomaticFact.
- `test/Hexalith.Agents.Server.Tests/AutomaticInteractionReplayFixture.cs`: reuse Handle/Apply histories, faults and effect tracking.
- Server tests: `GeneratedProposalMutationLeaseTests.cs`, `GeneratedOutputDiscardedWhenProposalMutationCommitLosesTests.cs`, `AutomaticInteractionCheckpointAgreementTests.cs`, `AutomaticInteractionRestartIntegrationTests.cs`, `AutomaticInteractionWorkflowTests.cs`, `WorkflowActivityFailureSafetyTests.cs`.
- `test/Hexalith.Agents.Tests/AutomaticInteractionPublicStatusTests.cs`: public audit visibility and serialized replay controls.
- AD-27 and Story 6.1 Review/Patch decisions govern the generation exception. Reuse recovery pacing and safe retry exception.

## Tasks & Acceptance

**Execution:**
- [x] Orchestrator/generation tests above — generate, observe mutation acquire/commit and record within one activity; cover Closing and faults.
- [x] Orchestrator/checkpoint tests above — bound observation of exact owner transitions; cover lag, unrelated revisions, rejection, exhaustion and cancellation.
- [x] Workflow/tests above — set exponential backoff; inspect public settings on actual activity options, including the cap.
- [x] State/public-status tests above — add both Blocked mappings; verify inspection, replay and later dispositions.
- [x] Activity/failure-safety tests above — add safe diagnostics; cover poisoned exceptions and invalid IDs.
- [x] Original Story 6.1 checklist/this spec — mark exactly five verified patches; retain concise evidence and blockers.

**Acceptance Criteria:**
- Given the matrix, when verification finishes, then every branch has passing behavioral and replayed end-state evidence without skips.
- Given non-generation checkpoints, when executed, then their one-command limit and authoritative outcomes remain intact.
- Given verified/reviewed changes, when delivery runs, then commitlint, repository checks, submodule availability and divergence checks precede the authorized main push.

## Implementation Notes

User approved and continued on 2026-11-16. The implementation handoff performs local implementation and verification only; the coordinator handles independent review, staging, commits and remote delivery. Do not stage, commit, push or change submodules in the handoff.

Coordinator audit independently verified all 14 code/test hashes, all 6,449 full native results and all 57 focused results, with each matrix row covered. Review corrections below strengthen verification without changing frozen intent.

No intent gaps. External effects: authorized Git delivery. Footprint: four production files, owning tests, observation/logging helpers if needed, and evidence.

## Spec Change Log

- 2026-11-16: Review corrections strengthen verification only: exact competing results, delayed and canceled generation transitions, complete envelope preservation, bounded setup, exact command sequences, restored Confirmation commit/Closing ordering and a portable reproducer. Production code and frozen intent remain unchanged.

## Review Triage Log

| Finding | Verdict and evidence | Route |
| --- | --- | --- |
| BH-1 | medium, pre-existing — Neither the owner-reader ports nor the baseline initial/refresh reads supplied a deadline. The default readers refuse immediately; noncooperative accepted host readers still need a qualified operation budget. This change bounds completed polling attempts, not arbitrary host-call duration. | defer |
| BH-2 | medium — Interaction observation has no same-step divergent-fact, permanent invisibility or rejected-append negative control; the current directory rejection theory cannot verify that guard. | patch |
| BH-3 | medium — The changed cancel observer has no delayed-cancellation or competing-commit control, although both are reachable cleanup outcomes. | patch |
| BH-4 | medium — Generation tests expose its lease transitions synchronously; they do not prove one local result survives delayed acquire/commit/result observations. | patch |
| BH-5 | medium — Cancellation tests cover individual checkpoints but omit the new generation activity's three observation boundaries and their replayed partial state. | patch |
| BH-6 | medium — Non-null output and a fixed digest do not verify preservation of the complete protected envelope, dependency reference and digest-key version. | patch |
| BH-7 | medium — New setup loops lack a bound and could hang verification on the exact no-progress regression under test. Reuse a bounded fixture helper. | patch |
| BH-8 | low — Provider authorization alone grants the checkpoint test a broad three-command allowance. Assert the generation command sequence so unrelated work retains the one-command constraint. | patch |
| BH-9 | medium — The changed pause helper makes the existing Confirmation winning-commit case record before Closing. Pause after commit and prove the result is absent before Closing to restore that race. | patch |
| BH-10 | medium — A fresh checkout cannot run the documented administrator-local external script. Replace it with a self-contained repository reproduction command. | patch |
| VG-1 | medium — Pre-verified regression gap: removing exact same-step payload comparison is not detected by the current visibility tests. A competing generated result must remain authoritative while the divergent submission reaches safe retry. | patch with BH-2 |

The edge-case review returned no findings. All three independent layers completed before this triage.

Closure: BH-2 through BH-10 and VG-1 are patched and verified in the final full native gate, including all 38 affected cases. BH-1 is recorded as a pre-existing host-qualification deferral. No unresolved in-scope finding remains.

## Verification

Final coordinator verification passes all 6,465 package-mode tests across five warning-free builds, including 1,047 Domain and 2,044 Server cases. All 73 named controls and 38 affected review cases pass in the full native XML; 16 final code/test hashes match the disposable compiled copy and source verification attempt. The self-contained documented reproducer exits 0. [Workflow verification evidence](tests/workflow-hardening-2026-11-16/README.md) retains final and historical results. Debug/source remains separately blocked by EventStore CA1822 diagnostics and the missing Conversations Platform mapping. Existing hosted CI fails its unchanged Builds-pin policy check. No live acceptance is claimed. All three reviews and in-scope patches are complete; Git delivery follows the authorized repository checks.

Build affected projects individually in Debug/source disposable copies; run focused/full Domain and Server suites through native xUnit v3 selectors. Follow the fallback ladder, recording source blockers separately from serialized Release/package evidence. Run `eng/verify-story-6.1.ps1` Package with an explicit retained artifact path. Assert replayed end-state through faults. Review independently and triage. Inspect all root submodules, apply required CI, validate commitlint, fetch/check divergence, push child content before parent gitlinks, and verify remote main.
