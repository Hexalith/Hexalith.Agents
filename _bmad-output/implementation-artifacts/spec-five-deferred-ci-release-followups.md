---
title: 'Resolve five deferred CI and release follow-ups'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Deferred CI and release follow-ups leave security callers on mutable refs, allow CodeQL evidence to be cancelled, duplicate release configuration, omit live GitHub CLI coverage, and document an obsolete CI revision.

**Approach:** Complete these five ledger items as requested: pin all three security callers to the already reviewed Builds revision used by CI; allow only superseded pull-request CodeQL runs to cancel and give other scans distinct concurrency groups; reconcile the completed CI/CD specification with current immutable CI/release pins while preserving historical frozen intent; derive planner branches, tag format, and analysis-plugin options from `.releaserc.json` while retaining its isolated dry-run mirror and excluding publication hooks; exercise the production source verifier's live `gh api` branch with a controlled executable on PATH, including exact endpoint arguments, success, process failures and malformed JSON. Mark exactly these five items complete (including duplicate references to the same pinning item), run relevant tooling and workflow checks, review, commit, and push on main. Preserve the six-package inventory and existing publication policy.

</frozen-after-approval>

## Implementation Notes

- Investigation: baseline `0709c0670ba736155c63f3ee07c4c3f4a839d8d1`, clean main. Existing CI/release and Builds gitlink use `359fee24aeca4d9eabed4b425d4cfa2520b2adda`; security callers still use main. CodeQL concurrency currently groups every event by ref with unconditional cancellation. Planner hardcodes main, v-prefixed tags, and analysis plugins; its executable tests already create scratch repositories. Source verifier accepts fixture flags but its live gh branch is uncovered.
- Route facts: no unresolved user-visible choices; small reversible workflow, documentation, and tooling changes; no new public API or deployment. Main push explicitly authorized; no release dispatch or package publication required. Freeform five-item bundle, no sprint story key.
- Files: `.github/workflows/{codeql,commitlint,dependency-review}.yml`, `eng/semantic-release-plan.mjs`, existing `tests/tooling/{semantic_release_plan,release_source_proof}_test.py`, completed CI/CD spec and deferred ledger. Use existing workflow lint and tooling discovery; inspect that security pins resolve at the root-reviewed Builds revision. Add only regression coverage that exercises planner decisions or actual subprocess behavior.
- Implemented all five items. CodeQL uses unique run-ID concurrency groups for non-PR events, protecting pending scans as well as active scans. Planner reads the caller repository's JSON configuration and preserves analysis-plugin tuple options; only the two analysis plugins are admitted. Scratch repositories use failing publication hooks to detect accidental plugin inclusion. Live-gh tests isolate PATH to the controlled executable (or an empty directory for the missing-CLI case).
- `python3 -m unittest discover -s tests/tooling -p '*_test.py'` passed all 56 tests. `actionlint -no-color`, `node --check eng/semantic-release-plan.mjs`, and `git diff --check` passed. Verified the three reusable workflow files exist at the exact current Builds gitlink revision. Changes affect tooling and workflow behavior, so verification uses the tooling lane; no .NET runtime code or package metadata changed.
- Marked five distinct deferred items complete, including both ledger references to the same security-pinning item; did not count that carried duplicate as a sixth item. Historical frozen CI/CD intent is preserved with an explicit current-state supersession and corrected implementation notes.
- Independent blind review returned four findings; all received bounded corrections. Scratch planner subprocesses ignore the host's GitHub CI marker; mirror setup uses the actual checked-out branch (or detached dispatch branch); top-level lifecycle hooks and shareable configs are explicitly disabled; fixture plugins are identified by name. Added real planner regressions for multi-branch source-ref preservation, excluded top-level hooks/configs, and reordered plugin options.
- Final broad validation with GitHub Actions main-push variables passed all 59 tooling tests. After adding explicit top-level analysis-hook sentinels, the eight planner tests also passed with GitHub Actions pull-request variables. Final workflow lint, JavaScript syntax, and whitespace checks passed. All four review findings are patched; no additional deferred items were created.

## Review Triage Log

- R1 — medium / patch: Reproduced the custom-branch test returning no release under main-push GitHub Actions variables because env-ci selected the hosting branch. Remove GITHUB_ACTIONS only from scratch-test subprocesses; production retains dispatch metadata for detached CI checkouts.
- R2 — high / patch: The reviewer's scratch repository with main and release branches demonstrated semantic-release fetching the mirror's rewritten main into the source repository. Seed and select the mirror's actual current branch instead; a multi-branch test compares every source ref before and after planning.
- R3 — medium / patch: The reviewer demonstrated a top-level verifyRelease exec hook executing despite filtering the plugins array. Explicitly disable non-analysis lifecycle hooks and extends, and clear top-level analysis overrides so only the selected analysis plugins supply those steps. Regression fixtures include forbidden hooks and an unresolvable shareable config.
- R4 — low / patch: Fixed positional fixture-plugin edits by identifying configured plugin names. A real planner case reverses plugin order and retains custom analyzer options; future configuration reordering no longer corrupts the fixture.
