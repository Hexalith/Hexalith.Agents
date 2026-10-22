---
title: 'Resolve five deferred release-verification items'
type: 'bugfix'
created: '2026-10-22'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five deferred release-verification items leave stable-release metadata and NuGet timeout handling insufficiently guarded, and leave production CLI verification calls without executable regression coverage.

**Approach:** Resolve the existing ledger entries for rejecting GitHub prereleases under stable tags, rejecting non-positive or non-finite NuGet timeouts, rejecting non-main dispatch refs even at the correct main SHA, exercising the release-source CLI with file fixtures, and exercising the GitHub Release and NuGet publication CLIs with file and loopback HTTP fixtures. Require bounded subprocess execution, useful failure diagnostics, and both passing and failing verification results. Preserve valid prerelease-version behavior, manifest-driven inventory checks, and the existing release workflow. Mark exactly these five entries resolved with verification evidence. Leave the three pre-existing untracked Story 5.4 artifacts untouched. Commit and push the completed changes on main as requested.

</frozen-after-approval>

## Implementation Notes

- Investigation: the five entries remain unresolved in `deferred-work.md`. Relevant code is in `scripts/verify-{github-release,nuget-publication,release-source}.py` and the corresponding three `tests/tooling/*_test.py` suites. Source and GitHub CLIs accept JSON file fixtures; NuGet accepts `--base-url`, enabling a loopback server without new production test hooks. Existing CI discovers all tooling suites.
- Route facts: no unresolved intent gaps; small, reversible changes to two scripts, three test suites, this spec, and the ledger; no new public API, deployment, package publication, or submodule changes. The requested main push is authorized. Baseline root revision: `dd1639439c6822337a747e254fcd626151b27000`.
- Applicable release constraints from `spec-implement-agents-ci-cd.md`: require exact current main with successful push CI, exact manifest package assets, and collision-failing NuGet checks. No release workflow dispatch is needed for local verifier tests.
- Implemented literal prerelease=false enforcement for stable GitHub versions, excluding prerelease identifiers before build metadata; preserved valid prerelease-version behavior. NuGet publication rejects non-positive and non-finite timeouts before transport or retry waits.
- Added bounded real-CLI subprocess tests to all three existing suites. NuGet uses a loopback HTTP server, the six-package manifest, and explicit proxy bypass; invalid timeouts assert no requests. Source fixtures cover exact main, non-main refs, stale source, failed CI and empty CI. GitHub fixtures cover exact release, invalid assets/source and stable prereleases.
- Initial full tooling discovery needed `npm ci --ignore-scripts` because locked Node dependencies were absent. After installation, `python3 -m unittest discover -s tests/tooling -p '*_test.py'` passed all 42 tests. `npm audit signatures` verified all 504 registry signatures and 127 attestations. No lockfile or dependency version changed.
- Six isolated mutation checks passed: removing each of the three production CLI verifier calls, the main-ref guard, the stable-prerelease guard, or the NuGet timeout guard makes its regression suite fail. The final tooling run after explicit proxy isolation also passed all 42 tests; `git diff --check` passed. No .NET code or package contents changed, so validation used the configured tooling lane.
- Blind review exposed a remaining extreme finite-timeout case: `--timeout=1e300` raised an unhandled transport OverflowError before requests. `_open_url` now translates that into the bounded VerificationError diagnostic for both registration and catalog transport. Added real-CLI retries of pending inventory and unsupported finite-timeout regression cases; the NuGet suite passes 21 tests.
- Final validation after review patches: `python3 -m unittest discover -s tests/tooling -p '*_test.py'` passed all 48 tests; `git diff --check` passed. GitHub/source CLI tests now also cover version variants and malformed/missing fixture files. Four review improvements were applied; one pre-existing live-gh transport-coverage idea was deferred. The conventional commit message passed commitlint.

## Review Triage Log

- R1 — medium / patch: Reproduced an unhandled OverflowError with timeout=1e300 against loopback (exit 1, zero requests, traceback). Translate transport overflow to VerificationError; subprocess regressions for both publication modes verify a concise diagnostic without a traceback.
- R2 — low / patch: The new NuGet CLI helper originally hardcoded one attempt, leaving retry forwarding uncovered. Added a two-attempt loopback case which returns an initial 404 for one package and asserts that only that pending package is retried.
- R3 — low / patch: GitHub CLI fixtures originally fixed the version to 1.2.3 while new behavior distinguishes stable, metadata and prerelease forms. Parameterize the test helper and exercise matching metadata/prerelease tags and package assets through the CLI.
- R4 — low / defer: The unchanged live gh branch of the source verifier lacks subprocess coverage. Both callers use it, but no broken endpoint or error behavior was shown. Recorded a separate transport-coverage follow-up in the ledger; existing JSON fixtures prove the selected CLI verification boundary.
- R5 — low / patch: New source/GitHub CLI helpers originally wrote only valid JSON. Add missing-file and malformed-JSON fixtures to assert script-prefixed failures without tracebacks or false success output.
