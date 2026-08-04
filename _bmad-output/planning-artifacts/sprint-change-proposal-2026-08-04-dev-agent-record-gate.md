---
title: Sprint Change Proposal - Recover and Harden the Dev Agent Record Pre-Review Gate
status: approved
created: 2026-08-04
updated: 2026-08-04
mode: Batch
change_scope: minor
recommended_path: direct-adjustment
owner: Amelia (Developer)
approval_required: false
approved_by: Administrator
approved_on: 2026-08-04
routed_to:
  - Amelia (Developer)
amends: sprint-change-proposal-2026-07-31.md
---

# Sprint Change Proposal: Recover and Harden the Dev Agent Record Pre-Review Gate

## 1. Issue Summary

The consolidated Epic 2/3/4 action is still valid, but its previously implemented story-automator integration has regressed.

The shared command, its 39-case regression suite, and the manual review customization remain present and green. The current story-automator workflow, however, proceeds directly from Automate to Code Review. Commit `f15981c` (the BMAD 6.10.1-next.49 refresh) deleted the complete pre-review gate inserted by commit `03900d9` and restored the old downstream numbering. The historical 2026-07-31 proposal therefore describes an implementation that is only partially live today.

This is a workflow-integration durability defect, not a defect in the canonical record checker.

### Trigger and evidence

- Epic 2 recorded stale test counts or File List omissions across all six stories.
- Epic 3 recorded recurrence after the checklist action.
- Epic 4 recorded recurrence across all five stories and concluded that the checklist was ineffective.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` correctly retains one consolidated open action, tracked by Story 5.18.
- `tools/check-story-review-readiness.py` remains the canonical gate.
- `python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py' -v` passes all 39 cases.
- Resolving the `bmad-code-review` customization includes `DEV_AGENT_RECORD_READINESS_GATE`, so the manual path remains guarded.
- `.agents/skills/bmad-story-automator/steps-c/step-03a-execute-review.md` currently transitions from Automate directly to `### D. Code Review Loop`.
- `git diff f15981c^ f15981c` shows that the refresh deleted the prior `### D. Dev Agent Record Readiness Gate` block before review.
- Story-automator has no `customize.toml` activation surface comparable to `bmad-code-review`; another unguarded edit to a generated step file would repeat the same failure mode.

## 2. Impact Analysis

### Epics and stories

No epic, story sequence, product acceptance criterion, or estimate changes are required. Epics 1-4 remain historical and will not be rewritten. The existing consolidated action remains open; this proposal repairs its enforcement before Story 5.18 or any earlier story enters review.

### PRD, architecture, and UX

No changes. This correction affects development tooling and orchestration only. It does not change product behavior, runtime contracts, deployment topology, data, user journeys, or accessibility requirements.

### Implementation artifacts

The approved intent in `spec-dev-agent-record-readiness-gate.md` remains correct. Its implementation status and Code Map are stale because the automated hook is no longer live. After approval, add a dated revalidation addendum without weakening the frozen intent.

### Process and technical impact

The correction will:

- Preserve one canonical Release/CTRF/File List gate for both review paths.
- Keep the existing team-owned manual code-review override.
- Enforce the same gate in executable story-automator code immediately before a review session can spawn.
- Put the automator hook configuration in the existing team-owned policy override path.
- Make a future generated-skill refresh fail closed if it removes support for that policy, instead of silently restoring QA-to-review bypass.
- Add behavioral integration tests for ordering, nonzero propagation, exact story resolution, provider copies, and refresh drift.

## 3. Checklist Assessment

| Area | Result | Evidence / disposition |
|---|---|---|
| Trigger understood | Pass | Recurring record drift is documented across Epics 2-4; the live regression is isolated to automator integration. |
| Epic impact | No change | No new epic or resequencing. |
| Story impact | Minor process dependency | Keep the existing action open and enforce the gate before future review. |
| PRD impact | None | Product requirements and MVP remain achievable. |
| Architecture impact | None | Repository tooling only. |
| UX impact | None | No user-facing surface changes. |
| Direct adjustment viability | Recommended | Existing gate and manual path are healthy; only durable automated enforcement and its proof are missing. |
| Rollback viability | Not applicable | Product work is not the cause. |
| MVP scope change | Not applicable | No product scope change. |
| Handoff | Developer | Tooling/runtime integration with deterministic tests. |

## 4. Recommended Approach

Use a Direct Adjustment with a fail-closed, policy-backed automator boundary.

The canonical gate stays in `tools/check-story-review-readiness.py`. Manual review continues to invoke it through `_bmad/custom/bmad-code-review.toml`. Story-automator invokes it from its executable `spawn review` boundary, before `spawn_session(...)` is allowed to run.

The automator configuration lives in `_bmad/bmm/story-automator.policy.json`, an existing project override path already loaded and snapshotted by story-automator. Runtime support explicitly recognizes and validates a top-level pre-review-gate contract. If a later BMAD refresh restores an upstream runtime that does not recognize the contract, policy loading fails on the unknown top-level key and the automator stops. This converts future refresh drift from a silent bypass into a visible fail-closed error.

Effort: Medium, approximately one focused developer day.

Risk: Low to Medium. Product behavior is unaffected. The principal risks are double-running tests, spawning review after a failed command, resolving the wrong story, or creating provider-specific behavior. Boundary-level tests and one shared policy contract address those risks.

## 5. Detailed Change Proposals

### 5.1 Preserve the consolidated sprint action

Artifact: `_bmad-output/implementation-artifacts/sprint-status.yaml`

OLD and NEW during implementation:

```yaml
- epic: 4
  action: "Implement one mechanical Dev Agent Record pre-review gate for manual and story-automator paths. The gate must regenerate the canonical test-count record from fresh Release test results, compare the story File List bidirectionally with git status, and fail before review on test failure, result-parsing failure, or any missing, extra, or stale record entry. This consolidates the duplicate Epic 2, 3, and 4 actions raised after recurring drift across all six Epic 2 stories and subsequent stories."
  owner: "Amelia (Developer)"
  status: open  # Tracked by Story 5.18.
```

Do not create another Epic 2, 3, or 4 action. Do not mark this action done merely because the hook is restored. Closure still requires the approved effectiveness evidence in section 7.

### 5.2 Extend canonical story selection without duplicating gate logic

Artifact: `tools/check-story-review-readiness.py`

OLD command surface:

```text
python3 tools/check-story-review-readiness.py <story-file>
```

NEW compatible command surface:

```text
python3 tools/check-story-review-readiness.py <story-file>
python3 tools/check-story-review-readiness.py --story-id <epic.story>
```

Rules for `--story-id`:

1. Resolve the configured implementation-artifacts directory using the same project configuration rules as story-automator.
2. Convert `epic.story` to the existing `epic-story-*` filename prefix.
3. Require exactly one regular Markdown story file.
4. On zero, multiple, unreadable, or out-of-root matches, exit nonzero before building or editing anything.
5. Pass the resolved path into the existing execution pipeline. Do not fork or reimplement Release execution, CTRF parsing, managed-block replacement, unmanaged-count detection, porcelain parsing, or bidirectional File List comparison.

Rationale: the manual path can retain its explicit path, while the automator can identify its current story mechanically without embedding artifact-discovery logic in generated orchestration code.

### 5.3 Add a team-owned, fail-closed automator policy sentinel

Artifact: `_bmad/bmm/story-automator.policy.json` (new)

OLD:

```text
No project story-automator policy override exists.
```

NEW contract:

```json
{
  "preReviewGate": {
    "command": [
      "python3",
      "tools/check-story-review-readiness.py",
      "--story-id",
      "{story_id}"
    ],
    "required": true
  }
}
```

The exact field name may be normalized during implementation, but it must remain a top-level, validated contract that an unextended upstream runtime rejects. Do not hide it inside a permissive nested map that an older runtime silently ignores.

The effective policy snapshot must retain the contract so resumed runs use the same pre-review rule as the run that started them.

### 5.4 Enforce the gate at the executable pre-spawn boundary

Artifacts:

- `.agents/skills/bmad-story-automator/src/story_automator/core/runtime_policy.py`
- `.agents/skills/bmad-story-automator/src/story_automator/commands/tmux.py`
- Matching `.claude/skills/bmad-story-automator/...` installed copies

OLD behavior in `_spawn(...)`:

```python
root = get_project_root()
agent = _resolve_agent_selection(agent, root)
if not command:
    ...
session = generate_session_name(step, epic, story_id, cycle)
out, code = spawn_session(session, command, agent, root, mode=runtime_mode())
```

NEW ordering contract:

```python
root = get_project_root()
if step == "review":
    run_required_pre_review_gate(root, story_id)
agent = _resolve_agent_selection(agent, root)
if not command:
    ...
session = generate_session_name(step, epic, story_id, cycle)
out, code = spawn_session(session, command, agent, root, mode=runtime_mode())
```

`run_required_pre_review_gate(...)` must:

1. Load the run's effective/snapshotted policy.
2. Require a valid `preReviewGate` contract.
3. Substitute only the documented `{story_id}` token and execute an argument vector without a shell.
4. Run from the repository root with bounded execution and inherited task-relevant environment.
5. Relay concise stdout/stderr evidence.
6. Return nonzero on command failure, timeout, missing executable, invalid policy, or result-decoding failure.
7. Never call `spawn_session`, generate a review session, increment a review cycle, or alter story/sprint status after a gate failure.
8. Run again before every later review cycle, because an auto-fix cycle may change tests or files.

Non-review steps must not invoke the gate.

### 5.5 Keep the story-automator workflow text aligned

Artifacts:

- `.agents/skills/bmad-story-automator/steps-c/step-03a-execute-review.md`
- Matching `.claude/skills/bmad-story-automator/steps-c/step-03a-execute-review.md`

OLD transition:

```markdown
→ proceed to D

### D. Code Review Loop
```

NEW transition description:

```markdown
→ proceed to D

### D. Required Dev Agent Record Readiness Boundary

The executable `spawn review` boundary runs the project policy's required
pre-review gate before any review session exists. A nonzero result is a
resumable Developer blocker: preserve story/sprint status, do not consume a
review cycle, report the remediation output, and halt.

### E. Code Review Loop
```

Renumber downstream section references coherently. The Markdown is operational guidance; the executable pre-spawn check is the enforcement authority.

### 5.6 Preserve and verify the manual path

Artifact: `_bmad/custom/bmad-code-review.toml`

No semantic change is expected. Keep `DEV_AGENT_RECORD_READINESS_GATE` before context loading and review layers. Add a regression assertion that the customization resolver still returns the gate after a generated BMad refresh.

The two paths may resolve the story differently, but both must invoke `tools/check-story-review-readiness.py` and accept only its zero exit as readiness.

### 5.7 Expand behavioral regression coverage

Artifacts:

- `tests/tooling/story_review_readiness/story_review_readiness_test.py`
- A focused automator integration test beside it if separation improves clarity

Add cases proving:

- `--story-id` resolves exactly one story and delegates to the existing gate.
- Zero and multiple story matches fail before build and preserve all story bytes.
- Manual customization resolution retains the canonical command and fail-closed instruction.
- A review spawn runs the gate before `spawn_session`.
- A nonzero gate, timeout, malformed policy, or missing policy never calls `spawn_session`.
- A successful gate allows exactly one requested review spawn.
- Non-review spawns do not run the gate.
- A second review cycle reruns the gate.
- Effective-policy snapshots retain the pre-review contract.
- Both installed provider copies expose equivalent behavior.
- Removing runtime recognition while retaining the team policy causes policy loading to fail, demonstrating refresh-safe failure rather than bypass.

Do not run the real solution once per unit case; continue using injected runners and temporary repositories.

### 5.8 Revalidate the implementation spec without weakening intent

Artifact: `_bmad-output/implementation-artifacts/spec-dev-agent-record-readiness-gate.md`

OLD implementation state:

```yaml
status: 'done'
```

NEW implementation state while repair is pending:

```yaml
status: 'in-progress'
```

After implementation and verification, restore `status: 'done'` and append a dated change-log entry describing the refresh regression, executable pre-spawn enforcement, team-policy sentinel, provider parity, and actual verification results. Update the Code Map and task checklist accordingly. Do not edit the frozen Intent or relax its Always/Ask First/Never boundaries.

### 5.9 Preserve the historical proposal

Artifact: `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-31.md`

No edit. It remains the record of the approved and implemented 2026-07-31 change. This proposal is its dated revalidation and repair record.

## 6. Scope and Compatibility Guardrails

In scope:

- Compatible `--story-id` resolution on the existing gate.
- Team-owned automator policy override.
- Executable review pre-spawn enforcement for installed Codex and Claude automator copies.
- Aligned workflow guidance and deterministic tests.
- Spec revalidation metadata and evidence.

Out of scope:

- Product code, APIs, runtime domain behavior, UX, deployment, or CI changes.
- Automatic File List editing.
- Rewriting historical story records.
- Test-result reuse or a bypass/waiver option.
- Recursive submodule initialization.
- Closing the sprint action without effectiveness evidence.

## 7. Acceptance and Effectiveness Evidence

Implementation acceptance requires all of the following:

1. The tooling unit/integration suite is green.
2. Python compilation checks are green for the gate and added tests.
3. Manual customization resolution contains the canonical gate.
4. Both installed story-automator providers reject review spawn on every gate failure class.
5. Removing or simulating loss of automator runtime support while retaining the project policy fails policy loading; it never bypasses to review.
6. A controlled story fixture proves fresh managed counts and exact bidirectional File List comparison.
7. `git diff --check` is clean.
8. The repository Release build and all discovered xUnit v3 projects are run once through the complete gate. Any existing baseline failure is reported as a real blocker, not waived.

The sprint action may close only after:

- one complete real-repository gate run passes, and
- two subsequent stories reach review without reviewer corrections to the managed test evidence or File List.

Until then, leave the consolidated action open and record the observed evidence.

## 8. Implementation Handoff

Scope classification: Minor.

Route to: Amelia (Developer), using the existing implementation spec plus this approved amendment.

Implementation sequence:

1. Mark the implementation spec in progress and add the compatible story-id contract.
2. Add and test the project policy sentinel.
3. Implement executable pre-spawn enforcement in both installed provider copies.
4. Align workflow text and downstream references.
5. Add behavioral integration and refresh-drift tests.
6. Run targeted tooling verification, customization resolution, provider parity checks, the complete real gate, and `git diff --check`.
7. Record actual results in the spec and sprint action without closing the action prematurely.

Success criterion: no manual or story-automator review can begin unless the same canonical command has just produced fresh, fully passing Release evidence and an exact bidirectional File List/Git-status match; a future generated-skill refresh can block automation but cannot silently remove that requirement.

## 9. Approval State

Approved by Administrator on 2026-08-04 for direct implementation by Amelia (Developer).

Approval authorizes the scoped tooling, policy, installed-provider workflow, test, and implementation-spec changes in this proposal. It does not authorize product changes, CI changes, bypasses, automatic File List edits, historical story rewrites, recursive submodule initialization, or premature closure of the consolidated sprint action.

## 10. Handoff and Workflow Execution Log

- 2026-08-04 — Correct Course activated for the consolidated Epic 2/3/4 Dev Agent Record action; Batch review mode selected.
- 2026-08-04 — Repository instructions, resolved workflow customization, persistent project contexts, PRD, epics, architecture, UX, retrospectives, sprint tracking, prior proposal, implementation spec, gate, tests, and both review paths were assessed.
- 2026-08-04 — The canonical gate's 39-case suite passed and the manual customization resolved with the required guard.
- 2026-08-04 — Live story-automator inspection found that BMAD refresh commit `f15981c` deleted the pre-review gate previously added by `03900d9`.
- 2026-08-04 — Direct Adjustment selected: executable pre-spawn enforcement plus a team-owned, fail-closed policy sentinel and refresh-drift regression proof.
- 2026-08-04 — Administrator continued the complete-proposal review and explicitly approved implementation.
- 2026-08-04 — Minor-scope handoff routed to Amelia (Developer).

### Handoff Completion

The handoff package contains the approved Sprint Change Proposal, artifact-specific old-to-new edits, compatibility and scope guardrails, behavioral acceptance cases, effectiveness criteria, and implementation sequence. The first implementation checkpoint is to mark the existing implementation spec in progress and add the compatible `--story-id` contract; the sprint action remains open until one complete real gate run and two correction-free subsequent stories provide the required effectiveness evidence.
