# Interaction intake recovery verification — 2026-11-16

Exactly five previously approved Story 6.1 review patches are complete: typed intake outcomes, identifier validation before effects, revision-contended permit retry, rejection-only directory recovery, and rejection-only interaction creation. The original seven separately deferred runtime/host contracts remain open.

| Final check | Result |
|---|---|
| Agents Release/package Contracts | 2,005 passed |
| Agents Release/package Client | 7 passed |
| Agents Release/package Domain | 1,042 passed |
| Agents Release/package Server | 1,924 passed |
| Agents Release/package UI | 1,352 passed |
| Existing Story 5.1 package gate | Passed: authority/floors, all five suites, negative dependency-mode checks, six packages and seven isolated consumers |
| Existing Story 5.2 focused/regression gate | Passed |
| Platform standalone Release/package Custody | 806 passed |
| Platform standalone CatalogBuilder | Warning-free Release/package build |
| Platform exact recorded source-propagation controls | 29 passed |
| Broad tooling suite | 336 passed; 2 assembly-discovery skips independently replayed below |
| Original real-runner compatibility cases | 2 passed against the genuinely built Release/package Domain assembly |

All listed product tests have zero failures, errors, skips or not-run cases. Successful builds have zero warnings/errors. The final native Server XML independently passes all seven frozen matrix rows and the cancellation controls. Fourteen production/test/build inputs match the disposable package-gate compilation copy; their SHA-256 values are retained in `verification.json`.

Three independent review layers completed. The edge-case reviewer found no additional defects. Accepted corrections preserve cancellation during known scheduler refusal, pace contended submissions, and test repeated contention, cached terminal rejection, delayed visibility, Closing and protected-content/frozen-metadata divergence. One pre-existing intake directory-scope issue was appended to the deferred ledger; its baseline behavior is unchanged by this batch.

Debug/source Domain and standalone Custody builds remain blocked by pre-existing EventStore Contracts CA1822 errors in `ProductionProfileValidationResult.Authorization` and `.Qualification`. The separate Server source attempt stops at Conversations’ required Platform-selected source mapping. No audit, analyzer or test gate was suppressed. Package verification is separate evidence and does not establish live owner acceptance.

Platform's rebased commit is `fecdcbc309e19c95808673ff7c5549d993b19b76`. Its standalone verification used its own recorded Builds `e2bbbd0893f586e4d447ef8358ff747ea3d34435`, EventStore `f85419d501f279e9a813435aa73538185ad26ada`, and Commons `fbf4e94233821f6ca336af91a06feaaeb7a16855`; the propagation replay exported all eleven consumed owner revisions before restoring Composition. No nested submodule was initialized. Earlier incomplete-copy failures are retained as harness failures, superseded by these standalone results.

The starting Agents commit `895039d8513363424882b3b7d64bca574fddc43b` already has a [failed hosted CI policy job](https://github.com/Hexalith/Hexalith.Agents/actions/runs/43890214742/job/133379065623): “Require the pinned Builds workflow source.” The unchanged workflow expects `264e17d3185c87b72b0757dd66a13040793756bb`, while the recorded Builds gitlink is `fdda17c0a4538b0f7c99f813c20ca24b49006530`. The baseline build/test job succeeded. This batch preserves the CI policy and records the pipeline mismatch independently of local product verification.

Detailed commands, complete native XML, build logs, source hashes, matrix audit and original unsuccessful attempts remain under `/home/administrator/verification/agents-intake-recovery-20261116/`. Disposable copies and build outputs were created through `run-with-temp`; all child jobs are awaited before delivery. Neither Agents nor Platform has adopted `.github/hexalith-ci.json`, so their existing gates apply.

To reproduce the maintained package gate, run `run-with-temp pwsh -NoProfile -File eng/verify-story.ps1 -Story 5.1 -DependencyMode Package` from an isolated Agents copy that retains the tracked package-authority probe files. Then run `eng/verify-story-5.2.ps1 -SkipBuild -Configuration Release` and the tooling suite. For Platform, export its exact recorded owners into a standalone copy; build Custody and CatalogBuilder in Release/package mode, restore Composition in Debug, and run `python3 -B eng/test_source_sdk_propagation.py --evidence <fresh-durable-directory>`. The detailed local command receipts record the MinVer environment pin used outside Git checkouts. No packages were published.

Platform was pushed and its full remote SHA verified before staging the parent gitlink. The authorized main update was confirmed through Git’s remote ref. All root-declared submodules were inspected; no other local work or unpublished commits were found.
