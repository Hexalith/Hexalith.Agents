---
title: 'Automate Dev Agent Record Review Readiness'
type: 'chore'
created: '2026-07-31'
status: 'done'
baseline_commit: '3478478'
context:
  - '{project-root}/_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-31.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Dev Agent Records repeatedly enter review with counts copied from an earlier test run or with paths omitted from the File List. The checklist-based correction failed across at least eleven stories because QA automation can change tests and files after Dev Story completion.

**Approach:** Add one standard-library, fail-closed command that creates fresh Release/CTRF evidence, owns the canonical count block, and compares the File List exactly with Git status. Invoke it before both manual and story-automator reviews.

## Boundaries & Constraints

**Always:** Build `Hexalith.Agents.slnx` once in Release with serialized MSBuild; discover every `test/**/*Tests.csproj`; resolve its Release `TargetPath`; execute the xUnit v3 DLL with `-automated sync` and fresh CTRF output in a temporary directory; atomically insert or replace one marker-delimited test-evidence block; reject count claims elsewhere in Dev Agent Record; parse `git status --porcelain=v1 -z --untracked-files=all --ignore-submodules=none`; compare normalized root-relative paths bidirectionally, including both rename endpoints; return nonzero with concise remediation on every unverifiable state. Manual and automated review hooks must stop before review on failure.

**Ask First:** Adding a third-party Python dependency; changing CI; introducing path exclusions or bypasses; relaxing exact File List equality; rewriting historical stories; changing product/runtime artifacts.

**Never:** Reuse pre-existing result files; parse porcelain by lines; edit the File List automatically; leave a partial story write; initialize nested submodules; start or charge a review cycle after gate failure; treat a green readiness result as implementation approval.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Ready story | All fresh tests pass; counts and paths agree | Generated block updated; exit 0 | N/A |
| Test failure | Build, runner, or one CTRF summary fails | Story unchanged; no Git comparison; exit 1 | Name failing stage/project |
| Invalid evidence | Missing/malformed CTRF or inconsistent summary | Story unchanged; exit 1 | Name file and validation error |
| Stale prose | Numeric test claim outside managed block | Block may refresh; exit 1 | Report source line(s) |
| Missing path | Git path absent from File List | Exit 1; File List unchanged | Print `missing from File List` set |
| Extra path | File List path absent from Git status | Exit 1; File List unchanged | Print `not present in git status` set |
| Complex status | Rename, deletion, spaces, Unicode, submodule | Compare exact decoded path set | Fail on malformed porcelain |
| Workflow failure | Gate exits nonzero before review | No reviewer spawned; status unchanged | Log and escalate to Developer |

</frozen-after-approval>

## Code Map

- `tools/check-story-review-readiness.py` -- command, CTRF aggregation, atomic record update, File List and porcelain parsing.
- `tests/tooling/story_review_readiness/story_review_readiness_test.py` -- temporary-repository and injected-runner regression suite.
- `_bmad/custom/bmad-code-review.toml` -- durable team override for manual review preflight.
- `.agents/skills/bmad-story-automator/steps-c/step-03a-execute-review.md` -- post-QA, pre-review automator gate and escalation.
- `.agents/skills/bmad-story-automator/steps-c/step-03b-execute-finish.md` -- downstream section references renumbered after inserting the gate.
- `_bmad-output/implementation-artifacts/sprint-status.yaml` -- consolidated authoritative process action.
- `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-31.md` -- approved scope, completion status, and verification record.

## Tasks & Acceptance

**Execution:**
- [x] `tools/check-story-review-readiness.py` -- implement test discovery/execution, validated CTRF aggregation, marker-only atomic update, unmanaged-claim lint, exact porcelain parser, and actionable exit output.
- [x] `tests/tooling/story_review_readiness/story_review_readiness_test.py` -- cover success, all failure classes, idempotent block replacement, no-write-on-evidence-failure, and path/status boundaries without running the real solution per unit case.
- [x] `_bmad/custom/bmad-code-review.toml` -- require the command before context gathering/review layers and route failure back to Developer.
- [x] `.agents/skills/bmad-story-automator/steps-c/step-03a-execute-review.md` and `step-03b-execute-finish.md` -- resolve exactly one story through the existing creation verifier; insert the command after Automate and before Code Review; pause/log failure without consuming a review cycle; keep downstream section references coherent.
- [x] `_bmad-output/implementation-artifacts/sprint-status.yaml` -- replace three duplicate actions with the approved canonical action; preserve unrelated entries.
- [x] `_bmad-output/planning-artifacts/sprint-change-proposal-2026-07-31.md` -- record implementation state and actual verification results.

**Acceptance Criteria:**
- Given fresh passing Release suites, when the gate runs, then its generated per-project and total counts equal the CTRF summaries from that invocation.
- Given any execution or evidence error, when the gate exits, then it is nonzero and the story bytes are unchanged.
- Given any File List/Git set difference, when comparison runs, then both difference directions are reported and File List text is unchanged.
- Given manual or automated review, when readiness fails, then no review layer/session begins and existing story/sprint status is preserved.
- Given the tooling suite and repository verification commands, when they complete, then all pass and no temporary results remain in the worktree.

## Design Notes

Keep pure parsers/renderers separate from command execution. Inject the clock and command runner in tests. Resolve test DLLs through MSBuild rather than assuming `net10.0` or an OS-specific apphost. Preserve the story's newline convention and mode during same-directory atomic replacement. Only write after every test result validates; a later File List mismatch may leave the newly accurate managed block in place.

## Verification

**Commands:**
- `python3 -m py_compile tools/check-story-review-readiness.py tests/tooling/story_review_readiness/story_review_readiness_test.py` -- expected: both files compile.
- `python3 -m unittest discover -s tests/tooling/story_review_readiness -p '*_test.py'` -- expected: all tooling cases pass.
- `dotnet build Hexalith.Agents.slnx -c Release -m:1 --nologo` -- expected: 0 warnings and 0 errors.
- Run all five built xUnit v3 DLLs with fresh CTRF output -- expected: every summary has zero failed tests and totals reconcile.
- `python3 _bmad/scripts/resolve_customization.py --skill .agents/skills/bmad-code-review --key workflow` -- expected: the team readiness preflight is present.
- `git diff --check` -- expected: no whitespace errors.

## Spec Change Log

- 2026-07-31 — Implemented the shared gate, 39-case tooling suite, manual and automated review preflights, downstream automator numbering, and sprint-action consolidation. Adversarial review fixes added exact Git filename preservation, solution-membership proof, strict all-pass CTRF validation, safe marker placement, concurrent-edit protection, subprocess timeouts, and durable automator pause handling. A fresh solution build is correctly blocked by pre-existing dependency-baseline errors; all five existing Release assemblies emitted fresh CTRF with 2,396 passing tests.

## Suggested Review Order

**Readiness gate**

- Start with the fail-closed orchestration and marker-only write sequence.
  [check-story-review-readiness.py:689](../../tools/check-story-review-readiness.py#L689)

- Validate story structure, marker safety, and exact File List parsing.
  [check-story-review-readiness.py:265](../../tools/check-story-review-readiness.py#L265)

- Confirm every discovered test project belongs to the freshly built solution.
  [check-story-review-readiness.py:381](../../tools/check-story-review-readiness.py#L381)

- Inspect strict CTRF all-pass validation and fresh assembly execution.
  [check-story-review-readiness.py:423](../../tools/check-story-review-readiness.py#L423)

- Review NUL-delimited Git status parsing and exact filename preservation.
  [check-story-review-readiness.py:619](../../tools/check-story-review-readiness.py#L619)

**Review workflow integration**

- See the post-QA gate, resumable pause, and pre-review stop behavior.
  [step-03a-execute-review.md:66](../../.agents/skills/bmad-story-automator/steps-c/step-03a-execute-review.md#L66)

- Confirm downstream automator transitions remain coherent after gate insertion.
  [step-03b-execute-finish.md:18](../../.agents/skills/bmad-story-automator/steps-c/step-03b-execute-finish.md#L18)

- Verify manual code review resolves the same mandatory readiness command.
  [bmad-code-review.toml:7](../../_bmad/custom/bmad-code-review.toml#L7)

**Evidence and tracking**

- Exercise parser, orchestration, concurrency, and failure-boundary regression coverage.
  [story_review_readiness_test.py:143](../../tests/tooling/story_review_readiness/story_review_readiness_test.py#L143)

- Review the consolidated open action replacing three duplicate sprint entries.
  [sprint-status.yaml:105](sprint-status.yaml#L105)

- Trace approval, implementation evidence, and the pre-existing Release blocker.
  [sprint-change-proposal-2026-07-31.md:398](../planning-artifacts/sprint-change-proposal-2026-07-31.md#L398)
