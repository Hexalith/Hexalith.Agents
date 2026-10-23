---
title: 'Complete five deferred build verification items'
type: 'bugfix'
created: '2026-10-23'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Five deferred verification gaps leave the selected test runner unguarded, six shipped operation enums vulnerable to accidental ordinal changes, high/critical NuGet findings exempt from ordinary CI failure, and Story 5.2 focused/regression gates absent from CI or insufficiently verified.

**Approach:** Complete these five requested ledger items as one build-verification batch: pin existing runner and numeric contract behavior, enforce ordinary CI audit severity, run the existing Story 5.2 verifier in CI, and exercise its actual subprocess behavior with controlled native test output. Review, verify, close the five selected rows, commit and push main as requested.

</frozen-after-approval>

## Implementation Notes

- Route facts: bounded updates to existing build/verification contracts with no unresolved user-visible choices, new product API, migration, data mutation, deployment or package publication. Main push is authorized. Five items are explicitly requested together. No sprint story key or in-repo AppHost applies; this domain repository delegates runtime hosting to Platform. Only root repository files change.
- Baseline: clean main at `31d04f454fade688bc91c010fca1c7351f234095`, captured directly with git rev-parse HEAD.
- Selected rows: GlobalJsonShouldPinTheSdk runner selection; six public Operations enums with implicit nonzero ordinals (2026-09-23 aggregate/contracts review); high/critical NuGet findings failing ordinary CI; Story 5.2 focused class-presence and regression skip/discovery gates; executable Story 5.2 verifier success/skip/unrun/zero fixtures. Keep external host, durable authorization, full Unknown-zero migration, and local audit-suppression DW-27 separate.
- `test/Hexalith.Agents.Server.Tests/BuildContractConformanceTests.cs`: extend GlobalJsonShouldPinTheSdk to require the existing Microsoft.Testing.Platform runner. Keep SDK selection unchanged. Update the existing build-policy guard to permit NU1901/NU1902 warnings while forbidding NU1903/NU1904 exemptions.
- `Directory.Build.props`: remove NU1903/NU1904 from WarningsNotAsErrors while preserving NuGetAudit=true, all dependency auditing and TreatWarningsAsErrors=true. The pinned shared domain-ci workflow restores without an audit suppression and builds Release with warnaserror; local verifier audit pins belong to separately tracked DW-27. Add a current-state note to the completed CI/CD artifact superseding its old exemption description.
- `src/Hexalith.Agents.Contracts/Operations/{AgentReadinessStatus,AgentCallOperationStatus,AuditAvailabilityStatus,ProposalOperationStatus,ProviderModelReadinessStatus,OperationalStatusInspectionStatus}.cs`: assign every current ordinal explicitly without changing any value. Extend the existing Pin/AssertPinnedOrdinals contract guard in `test/Hexalith.Agents.Contracts.Tests/AgentOperationContractsTests.cs`; preserve its complete-member comparisons and existing numeric serialization theories. Preserve CRLF in existing Contracts files.
- `eng/verify-story-5.2.ps1`: adapt the already hardened Story 5.3 native summary gate for independent focused/composition classes and full-project regression assemblies. Stream output, retain native diagnostics, require the expected assembly's single row in a single execution-summary section, and reject zero/absent/incomplete/ambiguous summaries, errors, failures, skips and not-run counts. Keep package-floor evaluation and its override/early-return unchanged. Preserve every existing selected class and source anchor.
- `.github/workflows/ci.yml`: after Story 5.1's source/package gate, run Story 5.2 with SkipBuild against the retained Debug/source assemblies. Keep the separate package-floor invocation. Extend `PackageInventoryTests.cs` to require the new call. No new CLI options or shared workflow revisions are needed.
- `tests/tooling/story_52_verifier_test.py`: follow the production-script subprocess fixture in story_53_verifier_test.py. Provide effective package-floor JSON via a controlled dotnet executable, dummy assemblies and source anchors. Assert all production class selections and full-project runs, independent later-class failure, zero/no/foreign/duplicate/incomplete summaries, reported failure/skip/not-run counts and native diagnostics. Include regression-lane failures, PackageFloorOnly behavior, streamed progress and real-assembly compatibility as appropriate. Keep the POSIX interception constraint explicit and every subprocess bounded.
- Verify full Story 5.2 source build and all five owning-project runs, affected contract/build classes, complete Python tooling suite, workflow lint and whitespace. Run audit-enabled ordinary Release/package restore/build to validate the policy without suppressing findings. Follow the fallback ladder for a genuine broad blocker; record exact diagnostics without weakening gates. Use independent blind review, triage all findings, validate the Conventional Commit message, fetch and inspect divergence before pushing main.

- Implemented the five existing verification contracts. Controlled fixtures exposed a PowerShell case-insensitive parameter/local collision during development; the assembly-name local now has a distinct name, and the real-runner plus positive subprocess probes pass. Workflow lint and PowerShell parsing pass. Full Story 5.2 source verification and the complete tooling suite are running; independent blind review is in progress.
- Full `pwsh -NoProfile -File eng/verify-story-5.2.ps1` passes: warning-free Debug/source solution build, all 20 independent focused/composition selections, all five full-project regressions (731 Contracts, 6 Client, 1,010 Domain, 1,344 Server, 1,169 UI; 4,260 total), live composition and source anchors. All 25 native runner summaries have zero errors/failures/skips/not-run. Audit-enabled Release/package verification follows.
- Audit-enabled ordinary Release/package restore and warning-as-error solution build pass with zero warnings/errors and no audit suppression. Controlled Story 5.2 tests pass all 13 originally loaded methods, including live progress and real-runner matched/unmatched selectors. The final complete tooling run additionally loads the new incomplete/malformed-summary matrix. Fetch reports zero divergence from origin/main.
- Independent blind review returned six findings. Five are verification corrections: standalone regression floors, enum pin-set completeness, evaluated audit policy, an active ordered CI-step guard, and controlled default restore/build coverage. Implemented these using existing Debug floors and helpers. Added below-floor subprocess cases for every owning project. The CI exact SkipBuild invocation also passed after the Release/package build before the floor correction; the final complete source gate verifies the strengthened floors.

- Post-review full Story 5.2 source verification passes again with warning-free solution build, all 20 selections, and 4,261 full-project tests (Server increases to 1,345 for the effective audit-policy guard). Every regression satisfies its existing Debug floor, and all errors/failures/skips/not-run counts are zero. New below-floor diagnostic assertions normalize PowerShell continuation wrapping; this changes fixture comparison only.

- Final `python3 -m unittest discover -s tests/tooling -p '*_test.py'` passes all 107 tests with zero skips. Final audit-enabled Release/package restore/build pass again after the review corrections with zero warnings/errors. The CI guard mutation probe rejects commented, disabled, nonblocking and misplaced steps. Exactly five ledger statuses are closed; no submodule files or pointers change, and no review follow-up is deferred. Final whitespace, workflow lint and Conventional Commit validation pass.

## Review Triage Log

- R1 — medium / patch: standalone regression runs accepted any positive count. Carry the existing Story 5.1 Debug floors into each full-assembly call and assert that a positive below-floor result fails for each project. Keep nonzero per-class guards.
- R2 — low / patch: newly pinned operation enums were not included in the pin-set completeness loop. Require the union of discovered setup and public operation enums so deleting any pin block fails the contract guard.
- R3 — low / patch: the original audit guard inspected only the first XML property assignment. Add an actual Release/package MSBuild evaluation of NuGetAudit, all-dependency auditing, warnings-as-errors, warning exemptions and NoWarn. This detects effective high/critical suppression; the ordinary audit-enabled Release restore/build also passed. A synthetic advisory transport would add disproportionate machinery for this configuration change.
- R4 — low / patch: a whole-file command substring could match a commented, conditional or misplaced step. Require the active consecutive source/package and Story 5.2 steps with exact blocking shell/run bodies, rejecting injected step conditions or continue-on-error without adding a YAML dependency.
- R5 — low / patch: controlled fixtures skipped the default build path. Add a production-script default invocation proving floor evaluation, Debug/source restore, warning-as-error build and only then all test gates.
- R6 — low / rejected: the new fixture repeats parts of the Story 5.3 runner pattern. No divergent interception/timeout bug was demonstrated; extracting a shared fixture framework would broaden this change beyond a simple correction and couple different package-floor, build, selector and regression-floor scenarios. Keep the explicit bounded fixtures.
