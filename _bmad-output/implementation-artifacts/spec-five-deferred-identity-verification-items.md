---
title: 'Complete five deferred identity verification items'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five identity verification entries remain open in the root deferred ledger despite corrections and focused regressions in the pinned Parties dependency. The root lacks a maintained command to build and execute those exact owner regressions without nested submodules or dated sibling paths.

**Approach:** Complete the requested five-item batch by providing a portable root verification command for the existing current/client completion and authority regressions, strengthening exact-target receipt subprocess coverage, and reconciling the five entries against fresh evidence. Independently review, commit, and push the changes to main as requested. This establishes local behavior and regression coverage; existing dependency acceptance and production qualification remain governed by their owner records.

</frozen-after-approval>

## Implementation Notes

- Route facts: small root-only verification tooling, documentation and ledger changes; no new product behavior, unresolved intent choices, migrations or deployments. The user authorizes five deferred items and pushing main. No sprint story key applies. No owned AppHost is involved.
- Clean main baseline: `21cd8b1864a038a8cee8ce19f8c0d1ec6f02f9c4`. Recorded Parties gitlink: `4fc1124eb995a40553cb23b042d4a3c1add3dfb2`. Both obtained directly from Git. Preserve dependency contents and pointers.
- Exactly five selections: B03 client completion expiry; E02 current read lower time bound; V01 current authority regression coverage; V02 current completion-expiry coverage; V03 readiness exact-target receipt coverage. Their source is `spec-5-4-apply-365-day-lifecycle.md`.
- Existing owner implementation: `references/Hexalith.Parties/src/Hexalith.Parties.Client/HttpPartiesIdentityClient.cs` checks completion-clock validity; `src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs` checks final source/binding lower bounds, expiry and authority. Reuse existing owner tests, without duplicating or altering production code. `HttpPartiesIdentityClientTests.DelayedReply_ValidatesCompletionTime` has nine cases; `PartyIdentityQueryHandlerTests.CurrentCompletionTimeChangesDuringCustody_DenyEvidence` has four; `CurrentAuthorityChangesDuringAwait_DenyEvidence` has two. Include the one-case current-human successful control.
- Add `scripts/verify-parties-identity.py`: resolve root from script and caller-relative artifacts before any command; build both individual owner test projects in Debug using absolute root-declared source paths, the currently evaluated EventStore version and the established optional Memories package fallback. Run exact single-dash xUnit method selections, validate fresh XML for the expected assembly/type/method, minima, all passing and no skips/errors/not-run; preserve command logs and a success-only local evidence receipt. Remove stale receipts/XML before execution. Run the maintained exact-target receipt tests through real PowerShell scripts as the final stage. Never initialize nested submodules or modify dependency records.
- Add controlled executable fixtures in `tests/tooling/parties_identity_verification_test.py` that exercise the actual command, proving successful evidence, source path arguments, missing/foreign/empty/incomplete/skipped/error results, failed builds/tests/readiness and stale evidence rejection. These fixtures do not claim real owner builds.
- Extend `tests/tooling/parties_readiness_verifier_test.py` to test distinct exact-target receipt mismatches through the production history script and wrapper, with matching success controls, expected diagnostics, zero HTTP and no success receipt. Reuse the existing temporary loopback fixtures.
- Document the focused command in `CONTRIBUTING.md`. Execute it against the actual pinned owner source; run the full root tooling suite and Python 3.10 compatibility checks; check whitespace. Use the workflow's context-free blind reviewer, triage every finding, fix concrete gaps, and reconcile only the five selected entries after passing checks. Validate the exact Conventional Commit message with pinned commitlint, fetch and inspect divergence, commit and push main.

- The real maintained command passes both warning-free Debug/source owner builds, all 16 selected owner cases and all 14 production-script receipt cases. Logs and XML are retained in `/tmp/agents-five-identity-20261023`; the success receipt explicitly denies live readiness. Initial command fixtures pass all 27 cases, covering fresh XML, exact selection/assembly identity, counts, skip/error rejection, build failures and receipt failures/skips/missing cases. Exact receipt case presence is required independently of the unittest total.
- Reconciled exactly B03/E02/V01/V02/V03 using these results. No owner source or submodule pointer changed. Complete tooling validation, Python 3.10 checks and the independent blind review are in progress.

- Review uncovered a masking defect in the existing null-authority tests: deleting SameAuthority still denies through a later null dereference. Added the root-owned `tests/identity/CurrentIdentityAuthorityTests.cs` (one test type) and a narrowly conditioned MSBuild inclusion target. The maintained command now executes eight non-null authority-change/control cases in the real owner assembly, for 24 total owner cases. A private compile-time source override removing only current SameAuthority produces exactly six failures and two passing controls; no owner file was edited. This completes V01 with independent evidence instead of relying on the original masking cases.
- Final command fixtures pass all 30 cases on both Python 3.11 and 3.10. A 3.10-specific unittest verbose format difference was detected and corrected: require the exact class and optional identical method suffix. Both formats still require all 14 exact receipt cases and an unskipped successful summary.

## Review Triage Log

- R1 — medium / patch: null-authority tests masked removal of SameAuthority. Added non-null source/actor revision/authority revision changes during both awaits and two unchanged controls. Mutation result: six failures, two passes; real unchanged source: eight passes.
- R2 — medium / patch: case counts alone allowed unrelated theory rows to replace required boundaries. Require every exact normalized parameter combination as well as assembly/type/method identity, minimum counts and passing status; the unrelated-row subprocess fixture must fail.
- R3 — medium / patch: the receipt lacked source provenance and run identity. Add canonical root/Parties Git revisions, dirty statuses, UUID and UTC start/completion timestamps; command fixtures check these fields.
- R4 — low / patch: English build-summary matching inherited arbitrary CLI language. Explicitly set DOTNET_CLI_UI_LANGUAGE=en-US for commands; the French-caller fixture now passes while nonzero warning output remains rejected.
- R5 — medium / patch: an early failure retained later-method XML from a previous run. Remove all known prior XML/logs and the success receipt before starting commands; early-build/version failure fixtures verify later results are absent.
- R6 — medium / patch: concurrent calls could mix artifacts and leave misleading success evidence. Acquire an atomic artifact-directory lock before touching outputs, release it in finally, and reject an existing lock without commands or output changes. Document distinct directories for concurrent runs and verify lock exclusion/release.

- Final validation: complete `python3 -m unittest discover -s tests/tooling -p '*_test.py'` passed all 192 cases in 801.674 seconds with zero skips. That discovery began before the last three harness cases were added. The final 30-case harness suite separately passes on Python 3.11 (12.297 seconds) and Python 3.10 (11.432 seconds), covering those additions; together these establish all 195 current tooling cases, without claiming that the earlier broad run contained all 195.
- Final real `python3 scripts/verify-parties-identity.py --artifacts /tmp/agents-five-identity-reviewed-20261023` passes both zero-warning/zero-error Debug/source builds, all 24 exact owner cases and all 14 exact receipt regressions. Fresh XML meets the final required-scenario validator. The receipt records canonical root/Parties revisions, dirty states, run UUID, UTC timestamps and local-only evidence. The private SameAuthority mutation still records six failures/two passing controls.
- Reviewer recheck confirms all six code corrections and the mutation evidence; the requested final documentation corrections are recorded above and in the V01 resolution. No review finding is deferred. Exact Conventional Commit validation and staged CRLF-aware whitespace checks pass. Fetch shows zero divergence. Only eight root-owned files change; all submodule contents and pointers remain unchanged. Commit/push to main is explicitly authorized by the user.
