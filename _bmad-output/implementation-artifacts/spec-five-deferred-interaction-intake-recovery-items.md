---
title: 'Five Deferred Interaction Intake Recovery Items'
type: 'bugfix'
created: '2026-11-16'
status: 'done'
baseline_commit: '895039d8513363424882b3b7d64bca574fddc43b'
route: 'dispatch'
context:
  - '{project-root}/_bmad-output/implementation-artifacts/spec-6-1-start-and-recover-an-automatic-interaction.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five unfinished, approved Story 6.1 review patches prevent automatic-interaction intake from safely recovering contention and rejection-only streams. Callers also receive untyped failures or persist identities the workflow cannot accept.

**Approach:** Complete those five patches as one intake-recovery batch: typed outcomes; workflow-compatible identifiers before writes; exact permit retry after revision contention; rejection-only directory recovery; rejection-only interaction creation. Independently review and verify the changes, then push to main, including the existing Platform work the user authorized reconciling.

## Boundaries & Constraints

**Always:** Preserve EventStore truth, exact protected intent, tenant isolation, business identities, enum ordinals, cancellation, bounded observation and Closing barriers. Reuse validation and Handle/Apply fixtures. Reject real legacy requested interactions. Use run-with-temp for disposable jobs; retain useful evidence outside scratch.

**Never:** Close the seven separate Story 6.1 ledger deferrals, invent host contracts, enable live adapters, change dependency acceptance, migrate legacy streams, deploy or publish packages. Suppress unrelated exceptions or dependency audits.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|---|---|---|---|
| Exact retry | Existing matching permit and interaction | Recover the same workflow and business identities | No duplicate effects |
| Divergence | Existing protected intent or creation differs | Conflict | Preserve recorded state |
| Recovery unavailable | Known workflow recovery refusal or exhausted observation | RecoveryRequired | Preserve cancellation and unrelated exceptions |
| Invalid identifiers | Tenant or Conversation outside the existing opaque-ID contract | NotFound before permits, creation or scheduling | No durable effect |
| Contended registration | Another write wins the expected directory revision | Re-submit the exact permit at the observed revision | Closing returns ConversationClosing; repeated lag remains bounded |
| Rejection-only directory | Fold has revision zero and empty scope | Continue exact permit recovery | Nonzero foreign scope remains NotFound |
| Rejection-only interaction | No successful request or permitted creation | Permit-created event can append and replay | Real legacy requests still reject |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Agents.Server/Application/AgentInteractions/AutomaticInteractionIntakeOrchestrator.cs`: gates and initial registration; lacks workflow ID validation.
- `src/Hexalith.Agents.Server/Application/AgentInteractions/InteractionOutboxRecoveryOrchestrator.cs`: bounded observation; throws divergence/exhaustion, rejects empty scope, never retries absent permits.
- `src/Hexalith.Agents.Server/Application/Workflows/WorkflowExecutionStateValidation.cs`: reuse opaque-ID contract `[A-Za-z0-9_-]{1,128}`.
- `src/Hexalith.Agents.Server/Ports/AutomaticInteractionIntakeStatus.cs`: preserve values 0–7. Adjacent `DaprInteractionWorkflowScheduler.cs` emits the known recovery refusal.
- `src/Hexalith.Agents/AgentInteraction/AgentInteractionAggregate.Automatic.cs`: legacy guard rejects every non-null state.
- `test/Hexalith.Agents.Server.Tests/AutomaticInteractionReplayFixture.cs`: envelope Handle/Apply model, retained histories and fault injection. Adjacent `AutomaticInteractionIntakeIntentTests.cs` and `AutomaticInteractionSubmissionRecoveryTests.cs` cover conflict/contention.
- `test/Hexalith.Agents.Tests/InteractionPermitCreationOutboxTests.cs`: creation and legacy controls.

## Tasks & Acceptance

**Execution:**
- [x] Intake status/recovery files above — append Conflict/RecoveryRequired for approved known failures — expose safe outcomes.
- [x] Intake/validation files above — share identifier validation before effects — prevent unrecoverable writes.
- [x] Recovery file above — retry the exact expected permit after revision contention; honor Closing — recover concurrent intake.
- [x] Recovery file above — admit revision-zero rejection-only directory folds — preserve established scope checks.
- [x] Aggregate file above — admit rejection-only history, retain actual legacy-request rejection — unblock creation.
- [x] Test files above — cover the matrix, replayed end states, asynchronous rejections and cancellation — verify persisted effects.
- [x] Original Story 6.1 review checklist and this spec — record exactly five verified patches and concise evidence — preserve unrelated deferrals.

**Acceptance Criteria:**
- Given recoverable history or contention, when recovery restarts, then original business identities survive without duplicate effects.
- Given invalid or unauthorized state, when intake/recovery runs, then the matrix outcome occurs without unauthorized durable effects.
- Given completed patches, when regression suites run, then existing gates pass without failures or skips.
- Given authorized delivery, when the parent records Platform, then its validated commit is already remote and remote main matches the delivered parent.

## Implementation Notes

Intent gaps: none. External effects: authorized Git pushes. Footprint: five production files, owning tests and scoped documentation.

Platform's existing commit rebased cleanly from `38d6c81fc7d8058bb93414a96993986e96da1058` to `fecdcbc309e19c95808673ff7c5549d993b19b76` atop fetched `c2236a9f95c37d4d2ef8dee3fe2cbfe5e9ab41d2`. Root starting revision is `895039d8513363424882b3b7d64bca574fddc43b`.

## Verification

Build/run affected Domain and Server projects individually in Debug/source disposable copies. Invoke built xUnit v3 assemblies for focused and full suites; reject zero discovery, failures and skips. Follow the fallback ladder for source-mapping blockers; retain them separately from Release/package evidence. Validate rebased Platform spool/catalog/source propagation and existing required gates. Neither module has `.github/hexalith-ci.json`; apply current gates. Independently review, validate Conventional Commits, inspect root submodules/divergence, push child content first, separately commit the parent and verify remote main.

**Executed implementation evidence (2026-11-16):**

- Exactly five approved Story 6.1 review patches are checked off. Typed statuses preserve ordinals 0–7; stale registration retries preserve the entire protected permit and use the observed revision. Rejection-only folds can converge without admitting real legacy requests or established foreign scope.
- Disposable `run-with-temp` copies: Release/package builds of Domain and Server test projects succeeded with zero warnings/errors. Built xUnit v3 assemblies passed 5 focused and 1,042 full Domain cases, plus 30 focused and 1,914 full Server cases; zero failures, skips, errors or not-run cases. The full suites include every frozen matrix row, asynchronous rejections, restart/replay state, exact identities, Closing and cancellation.
- Focused Debug/source attempts remain blocked independently of this batch: EventStore Contracts reports CA1822 for `ProductionProfileValidationResult.Authorization` and `.Qualification`; Server restore requires Conversations’ Platform tool-selected source mapping. These failures are retained separately, with no audit, analyzer or test suppression.
- Retained logs, xUnit XML and matrix audit: `/home/administrator/verification/agents-intake-recovery-20261116/`. Independent review and authorized Platform/parent Git delivery remain with the coordinating workflow.

## Review Triage Log

Three independent reviews completed; the edge-case reviewer returned no findings. Each finding is assessed separately below.

| Finding | Verdict and evidence | Route |
|---|---|---|
| BH-1 | medium, pre-existing: intake checks Closing before establishing the returned directory scope; the unchanged baseline has the same ordering. Recovery retains its established-scope refusal. | defer |
| BH-2 | low: the exact InvalidOperationException message is the existing scheduler refusal contract. A separate public exception/result surface would add complexity without a demonstrated accidental collision; other messages and cancellation exceptions propagate. | reject |
| BH-3 | medium: a real cancellation requested during scheduling can be hidden by the new known-refusal translation. Check the token at that boundary and exercise the race. | patch |
| BH-4 | medium: advancing directory revisions can bypass every observation delay and exhaust the bounded loop with a burst of submissions. Pace contended retries and verify repeated contention. | patch |
| BH-5 | medium verification gap: the first contention case does not cover a second stale retry, Closing after retry, or advancing revisions exhausting the bound. | patch |
| BH-6 | medium verification gap: new registration tests persist rejections but omit the terminal receipt cache that makes new submission identities necessary. Exercise cached original replay. | patch |
| BH-7 | low: successful normal identifiers already reach scheduling, and the accepted non-null regex is unchanged. Testing every accepted boundary through a new generalized scheduler fixture would add complexity for an unlikely regression; null refusal is directly evident from the shared predicate. | reject |
| BH-8 | medium verification gap: creation divergence tests cover caller and permit revision but omit protected bytes and other frozen permit metadata now compared in full. Expand negative controls. | patch |
| VG-1 | medium verification gap: accepted retry visibility at an unchanged nonzero revision is not covered; a >= comparison mutation would resubmit and exhaust instead of waiting. Add delayed retry visibility with exact persisted end-state checks. | patch |

## Final Coordinator Verification

All accepted review patches are verified: the corrected focused Server run passes 40 cases, and the final full native run passes 1,924. The maintained package gate passes all 6,330 Agents cases, six packages, seven isolated consumers and dependency-mode refusal checks. Story 5.2 focused/regression verification also passes. Broad tooling passes 336 cases; its two discovery skips have separate passing original-test replays against the genuinely built Release/package Domain assembly.

Platform's exact standalone tuple passes all 806 Custody cases, a warning-free CatalogBuilder build and 29 source-propagation controls. Its source build retains the independent EventStore CA1822 blocker. All fourteen tested production/test/build inputs match the final working tree. No audit or test was weakened. BH-3 through BH-6, BH-8 and VG-1 are patched; BH-1 remains a separately recorded pre-existing follow-up.

[Final verification and reproduction](tests/interaction-intake-recovery-2026-11-16/README.md) and [source/evidence hashes](tests/interaction-intake-recovery-2026-11-16/verification.json) preserve the bounded local claims and baseline hosted-CI pin mismatch. Platform's full commit was verified on origin/main before staging its gitlink. All disposable child jobs completed and their workspaces were removed. The user's explicit main-push instruction authorizes delivery after this workflow's local commit.
