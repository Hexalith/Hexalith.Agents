---
title: Sprint Change Proposal - Automate Dev Agent Record Accuracy
status: implemented
created: 2026-07-31
updated: 2026-07-31
mode: Incremental
change_scope: minor
recommended_path: direct-adjustment
owner: Amelia (Developer)
approved_by: Administrator
approved_on: 2026-07-31
implemented_on: 2026-07-31
approval_required: false
---

# Sprint Change Proposal: Automate Dev Agent Record Accuracy

## 1. Issue Summary

Dev Agent Records repeatedly reached code review with test totals copied from an earlier run or with changed files missing from the story File List. Review corrected the records, but the same defect recurred across every Epic 2 story and continued through later epics after a checklist reminder was introduced.

Trigger: automate Dev Agent Record accuracy by regenerating test counts from the latest run and comparing the File List with Git status before review.

Issue type: failed process approach requiring a mechanical replacement. The implementation and test quality were generally sound; the failure was manual evidence transcription at the development-to-review boundary.

Evidence:

- Story 2.1 recorded `280 / 96 / 101` where the latest run showed `293 / 99 / 108`; its QA summary was also omitted.
- Story 2.2 repeated stale counts and omitted its QA gap summary.
- Story 2.3 recorded `451 / 169` where the latest run showed `456 / 178`, and omitted `ContextPolicyResolutionTests.cs`.
- Story 2.4 recorded `489 / 158 / 197` where the latest run showed `492 / 159 / 202`.
- Story 2.5 recorded `526 / 218 / 1075` where the latest split was `527 / 175 / 232 / 156 = 1090`, and omitted `ConversationClientResponsePosterTests.cs`.
- Story 2.6 recorded a UI total of `336`; review ran `352` after QA automation.
- The Epic 2 retrospective explicitly identifies record drift across all six stories and assigns automation to Amelia.
- The Epic 4 retrospective reports the same pattern in all five Epic 4 stories and concludes that the checklist did not prevent recurrence. This expands the evidence to a three-epic, at-least-eleven-story process failure.
- `sprint-status.yaml` currently carries three overlapping open actions under Epics 2, 3, and 4 for the same correction.
- The current manual review workflow compares Git and the File List only after review has begun. The story automator runs `dev -> automate -> review`, so QA can add tests or files after the Dev Story completion check and before review.
- The repository's built xUnit v3 Release executables can emit CTRF JSON with machine-readable `tests`, `passed`, `failed`, `pending`, `skipped`, and `other` summary fields.

## 2. Impact Analysis

### Epic Impact

Epics 1-4 remain valid and complete. No epic scope, order, priority, or acceptance criterion changes are required. This correction closes an existing cross-epic process action; it does not create a new product epic.

### Story Impact

The six Epic 2 story files remain historical records and will not be rewritten retroactively. Future stories must use one machine-owned test-evidence block and pass an exact File List check before review begins.

The immediate implementation is a standalone developer-tooling change rather than a new product story. The existing sprint action remains the tracking source until implementation and effectiveness verification are complete.

### PRD Impact

None. Product goals, functional requirements, non-functional requirements, and MVP scope are unchanged.

### Architecture Impact

None. The gate operates on development evidence and repository state; it does not change runtime components, contracts, data models, APIs, integrations, or deployment topology.

### UX Impact

None. No user interface, journey, interaction, or accessibility specification changes.

### Technical and Process Impact

The correction adds:

- One standard-library Python gate that builds and runs the current Release test suites, parses fresh CTRF results, updates a generated Dev Agent Record block, and compares the File List bidirectionally with root Git status.
- A deterministic tooling regression suite.
- A team-owned manual code-review preflight customization.
- A story-automator pre-review hook placed after QA automation and before the first review session.
- Consolidation of three duplicate sprint actions into one authoritative action.

No CI/CD or deployment change is required in this correction. The tool can later be promoted to CI if local workflow evidence shows value.

## 3. Recommended Approach

Recommended path: Direct Adjustment.

Rationale:

- The defect is confined to development workflow evidence, so product rollback or scope reduction would not address it.
- A shared command avoids separate manual and automated interpretations of test-count or File List accuracy.
- Running the gate after QA automation closes the timing gap that allowed later test and file additions to escape the Dev Story completion check.
- Fail-closed workflow hooks prevent an inaccurate story from consuming review effort.
- Team customization is used for manual code review so the policy survives generated BMad skill refreshes.

Effort estimate: Medium, approximately one focused developer day including boundary-case tests and both workflow integrations.

Risk level: Low to Medium. Product behavior is unaffected. The main risks are incorrect Git porcelain parsing, runner-discovery assumptions, destructive story rewriting, and an automator retry loop. The proposed unit tests, marker-delimited replacement, temporary result storage, and immediate escalation behavior mitigate those risks.

Timeline impact: no sprint replan and no product milestone change. The next story should not enter review until the gate is available.

Alternatives considered:

- Potential rollback: not viable; completed product work is not the cause.
- PRD/MVP review: not viable; scope and goals remain achievable.
- Another checklist reminder: rejected by evidence; it already failed across five subsequent Epic 4 stories.
- Review-time correction only: rejected because it spends review capacity discovering a known, mechanically detectable defect.

## 4. Detailed Change Proposals

### 4.1 Consolidate the Sprint Action

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

Section: `action_items`

OLD:

```yaml
- epic: 2
  action: "Automate Dev Agent Record accuracy: regenerate test counts from the latest run and diff the File List against git status before review (recurring stale-count/omission finding across all 6 stories)."
  owner: "Amelia (Developer)"
  status: open

- epic: 4
  action: "Enforce Dev Agent Record accuracy mechanically (Epic 2 AI#1's checklist did not stop recurrence across 5 more stories): a pre-review script/hook or test that regenerates test counts from the latest Release run and diffs the File List against git status --short, failing the story if they drift."
  owner: "Amelia (Developer)"
  status: open

- epic: 3
  action: "Replace the Dev Agent Record checklist with a mechanical pre-review gate that fails when recorded test counts or File List entries drift from actual test output and git status --short."
  owner: "Amelia (Developer)"
  status: open
```

NEW:

```yaml
- epic: 4
  action: "Implement one mechanical Dev Agent Record pre-review gate for manual and story-automator paths. The gate must regenerate the canonical test-count record from fresh Release test results, compare the story File List bidirectionally with git status, and fail before review on test failure, result-parsing failure, or any missing, extra, or stale record entry. This consolidates the duplicate Epic 2, 3, and 4 actions raised after recurring drift across all six Epic 2 stories and subsequent stories."
  owner: "Amelia (Developer)"
  status: open
```

Rationale: one authoritative action prevents three overlapping backlog entries from diverging and gives sprint status a measurable fail-closed outcome.

### 4.2 Add the Shared Record-Readiness Gate

Artifact: `tools/check-story-review-readiness.py`

OLD:

```text
No root-level mechanical Dev Agent Record gate exists.
```

NEW functional contract:

```text
Command:
python3 tools/check-story-review-readiness.py <story-file>

1. Build the solution in Release mode.
2. Discover and execute every root test project through its built xUnit v3
   executable, emitting fresh CTRF JSON into a temporary directory.
3. Parse all CTRF summaries and replace one marker-delimited, machine-owned
   test-evidence block in Dev Agent Record.
4. Reject numeric test-count claims elsewhere in Dev Agent Record; prose must
   refer to the generated block.
5. Read the story File List and `git status --porcelain=v1 -z` at the root.
6. Require bidirectional equality between File List paths and changed or
   untracked Git paths after regenerating the story record.
7. Parse spaces, non-ASCII paths, renames, deletions, and submodule entries
   without line-oriented assumptions.
8. Exit nonzero on build/test failure, missing or malformed CTRF, unmanaged
   count claims, or any missing/extra File List path.
9. Print a concise remediation diff but never edit the File List.
10. Keep temporary test results outside the repository and clean them on all
    exits.
```

Generated block:

```markdown
<!-- dev-agent-test-evidence:start -->
### Latest Release Test Evidence

Run (UTC): <timestamp>

| Test project | Total | Passed | Failed | Skipped | Pending | Other |
|---|---:|---:|---:|---:|---:|---:|
| ... | ... | ... | ... | ... | ... | ... |
| **Total** | ... | ... | ... | ... | ... | ... |

Result: PASS
<!-- dev-agent-test-evidence:end -->
```

Rationale: results are created and consumed within one invocation, eliminating accidental reuse of an older "latest" report. The exact set comparison makes omissions and false File List claims equally visible.

### 4.3 Add Tooling Regression Tests

Artifact: `tests/tooling/story_review_readiness/story_review_readiness_test.py`

OLD:

```text
No automated coverage exists for Dev Agent Record synchronization.
```

NEW coverage:

```text
Test-count generation
- Aggregate all CTRF project summaries correctly.
- Insert or replace exactly one generated block.
- Reject malformed, missing, or stale CTRF output.
- Reject failed builds and tests.
- Detect unmanaged numeric test-count claims.
- Use an injectable clock for deterministic output.

File List comparison
- Accept exact bidirectional agreement.
- Report paths missing from either side.
- Handle modified, added, deleted, renamed, untracked, spaced, non-ASCII,
  and root submodule paths from NUL-delimited status.
- Never edit the File List automatically.

Operational behavior
- Return zero only when every check passes.
- Return concise remediation details for each failure class.
- Clean temporary directories on success and failure.
- Leave story content unchanged when test execution or parsing fails.
```

Test command:

```bash
python3 -m unittest discover \
  -s tests/tooling/story_review_readiness \
  -p '*_test.py'
```

Rationale: the parser and comparison are the enforcement mechanism; boundary-case coverage prevents the gate from becoming another unreliable checklist.

### 4.4 Enforce the Gate for Manual Review

Artifact: `_bmad/custom/bmad-code-review.toml`

OLD:

```text
No team customization exists. Manual code review can begin with stale test
counts or an incomplete File List.
```

NEW:

```toml
# Team-owned Dev Agent Record readiness guard. This override survives
# generated BMad skill refreshes.

[workflow]

activation_steps_append = [
  "DEV_AGENT_RECORD_READINESS_GATE: Resolve the requested story file before loading review context or launching review layers. Run `python3 tools/check-story-review-readiness.py <story-file>`. If the target is ambiguous, resolve it with the user before running the gate. Treat every non-zero exit as a fail-closed pre-review blocker: report the tool's remediation diff, leave the File List and story/sprint statuses unchanged, route repair to the Developer, and do not begin adversarial review. A validated test run may refresh only the managed evidence block before a later File List mismatch is reported. Do not waive or reinterpret missing, extra, or stale entries. On success, retain the command and final summary as review evidence; the green gate is a readiness floor, not proof that acceptance criteria or implementation quality are correct."
]
```

Rationale: direct `bmad-code-review` runs receive a mandatory preflight through a team override that survives skill updates.

### 4.5 Gate Story Automator After QA

Artifact: `.agents/skills/bmad-story-automator/steps-c/step-03a-execute-review.md`

Section: transition from Automate to Code Review.

OLD:

```markdown
- Automate succeeds or is skipped.
- Proceed directly to D. Code Review Loop.
- Spawn the configured review agent.
```

NEW:

```markdown
### D. Dev Agent Record Readiness Gate

After Automate completes or is skipped, resolve exactly one story file using
the same story-prefix logic as the review prompt.

Run:

`python3 tools/check-story-review-readiness.py <story-file>`

- Capture the exit code and concise output in the orchestration action log.
- On success, proceed to the Code Review Loop.
- On failure:
  - do not spawn a review session;
  - do not consume a review-cycle retry;
  - leave story and sprint status unchanged;
  - record `RECORD_READINESS_BLOCKED` with the remediation diff;
  - enter the existing escalation path for developer correction.
- Run the gate even when QA automation was skipped or failed, because test
  execution inside the gate remains mandatory.
- Never replace failure with a checklist assertion or reviewer judgment.

### E. Code Review Loop

[Existing review-loop behavior, with downstream section references renumbered.]
```

Rationale: QA automation runs after Dev Story and can add tests or files. This hook closes that timing gap before any reviewer inherits the record.

## 5. Implementation Handoff

Scope classification: Minor.

Route to: Amelia (Developer) for direct implementation.

Implementation sequence:

1. Add the gate as small testable functions with command execution and clock boundaries that the unit suite can replace.
2. Add and pass the tooling regression suite.
3. Exercise the command against a temporary story/repository fixture, including a deliberate File List omission and stale count claim.
4. Add the manual code-review customization.
5. Add the automator pre-review step and immediate escalation behavior.
6. Consolidate the three sprint action entries without changing unrelated action items.
7. Run the repository's standard Release build/tests and the new Python suite.
8. Review the implementation diff and confirm the workflow hooks invoke the same command.

Developer responsibilities:

- Preserve unrelated worktree and action-item changes.
- Keep the gate standard-library-only unless a dependency is separately approved.
- Make story writes atomic and limited to the generated marker block.
- Do not initialize nested submodules.
- Document exact verification commands and results in the implementation handoff.

Acceptance criteria:

- A successful invocation uses CTRF files created during that invocation and writes totals matching their summaries.
- A failed build, failed test, malformed result, unmanaged count claim, or File List mismatch returns nonzero.
- File List comparison is exact and bidirectional against NUL-delimited root Git status.
- The command never edits File List content and never leaves a partial story write.
- Tooling unit tests pass for all listed path and result edge cases.
- Manual code review does not begin after a failing gate.
- Story automator runs the gate after QA automation and does not spawn or charge a review cycle after failure.
- The next two stories using the gate reach review without a test-count or File List correction; only then is the process action considered proven effective.

Handoff dependencies: none from Product, Architecture, UX, infrastructure, or deployment. A Developer can implement directly after final approval.

## 6. Checklist Summary

| Item | Status | Finding |
| --- | --- | --- |
| 1.1 | Done | Triggering scope is Epic 2 Stories 2.1-2.6, with later recurrence in Epics 3 and 4. |
| 1.2 | Done | Failed manual/checklist process; test-count transcription and File List reconciliation are not mechanically enforced. |
| 1.3 | Done | Story review records, retrospectives, sprint actions, workflow order, and live CTRF capability provide concrete evidence. |
| 2.1 | Done | The completed trigger epic remains valid. |
| 2.2 | Done | No epic scope or acceptance-criteria change. |
| 2.3 | Done | Future stories are affected only at the pre-review process boundary. |
| 2.4 | Done | No epic becomes obsolete and no new product epic is needed. |
| 2.5 | Done | No epic resequencing or priority change. |
| 3.1 | N/A | PRD and MVP remain unchanged. |
| 3.2 | N/A | Runtime architecture remains unchanged. |
| 3.3 | N/A | UI/UX remains unchanged. |
| 3.4 | Action-needed | Tooling, tests, review customization, automator workflow, and sprint tracking require implementation. |
| 4.1 | Viable | Direct Adjustment; medium effort and low-to-medium technical risk. |
| 4.2 | Not viable | Rollback cannot correct a workflow-evidence defect. |
| 4.3 | Not viable | MVP review is unrelated to the defect. |
| 4.4 | Done | Direct Adjustment selected for sustainability and minimal product impact. |
| 5.1 | Done | Issue and evidence documented. |
| 5.2 | Done | Epic, story, artifact, and technical impacts documented. |
| 5.3 | Done | Recommendation and rejected alternatives documented. |
| 5.4 | Done | MVP unaffected; sequenced implementation plan included. |
| 5.5 | Done | Minor-scope Developer handoff and success criteria defined. |
| 6.1 | Done | All applicable checklist items are addressed in this proposal. |
| 6.2 | Done | Proposal is consistent with approved incremental edits and repository evidence. |
| 6.3 | Done | Administrator explicitly approved the complete proposal on 2026-07-31. |
| 6.4 | N/A | No epic or story status topology changes are required; sprint action consolidation remains an implementation deliverable. |
| 6.5 | Done | Minor-scope implementation is routed to Amelia (Developer) with the sequence and acceptance criteria in Section 5. |

## 7. Approval Record

The five detailed edits were individually approved by Administrator in Incremental mode on 2026-07-31.

Final proposal approval: approved by Administrator on 2026-07-31.

Implementation status: implemented. Effectiveness remains open until two subsequent stories use the gate without a record correction.

## 8. Workflow Execution Log

- 2026-07-31 — Change trigger confirmed and Incremental mode selected.
- 2026-07-31 — PRD, epics, architecture, UX, sprint tracking, retrospectives, story evidence, and workflow integration points assessed.
- 2026-07-31 — Five explicit edit proposals approved individually by Administrator.
- 2026-07-31 — Complete Sprint Change Proposal reviewed and explicitly approved by Administrator.
- 2026-07-31 — Minor-scope handoff issued to Amelia (Developer): implement Sections 4.1-4.5 in the sequence and against the acceptance criteria in Section 5.
- 2026-07-31 — Amelia implemented the gate, 39-case regression suite, manual review override, post-QA automator preflight, downstream numbering updates, and sprint-action consolidation.
- 2026-07-31 — Adversarial review hardening added exact Git filename preservation, solution-membership proof, strict all-pass CTRF validation, safe marker placement, concurrent-edit protection, subprocess timeouts, and durable automator pause handling.
- 2026-07-31 — Verification: Python compilation and all 39 tooling tests passed; customization resolution includes the preflight; all five existing Release test assemblies emitted fresh CTRF with 2,396 passing and zero failed/skipped/pending/other tests.
- 2026-07-31 — Baseline blocker recorded: a fresh solution build currently fails before compilation on pre-existing dependency configuration (`AngleSharp` NU1902 advisory-as-error and FluentUI NU1109 central-version downgrade). The gate remains fail closed and will block review until that repository baseline is repaired; dependency changes were not absorbed into this process correction.

## 9. Implementation Result

Implemented artifacts:

- `tools/check-story-review-readiness.py`
- `tests/tooling/story_review_readiness/story_review_readiness_test.py`
- `_bmad/custom/bmad-code-review.toml`
- `.agents/skills/bmad-story-automator/steps-c/step-03a-execute-review.md`
- `.agents/skills/bmad-story-automator/steps-c/step-03b-execute-finish.md`
- `_bmad-output/implementation-artifacts/sprint-status.yaml`

Verification results:

- `python3 -m py_compile ...` — passed.
- `python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py' -v` — 39 passed.
- BMad code-review customization resolution — readiness activation step present.
- CLI fail-closed smoke against a non-story artifact — exit 1 with byte-for-byte preservation.
- Fresh CTRF from existing Release assemblies — Client 6, Contracts 327, Server 371, Domain 724, UI 968; total 2,396 passed, zero failed/skipped/pending/other.
- `git diff --check` — passed before review.
- Fresh Release solution build — blocked before compilation by the pre-existing NU1902/NU1109 dependency baseline described above.

Tracking posture: the consolidated action remains `open` until the dependency baseline permits a green gate and the next two stories reach review without test-count or File List correction.
