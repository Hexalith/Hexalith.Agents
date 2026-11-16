# Final combined-source verification — 2026-11-16

The five approved ledger records are handled: intake scope isolation and
unambiguous governance JSON are fixed; raw-time admission and verified query
caller behavior are locally reconciled; executable consumer dependencies are
observed and checked against schema-2 ownership/hash policy. Three records are
closed, and the two ingress-authority records retain deployed-host qualification
under DW-21. The pre-existing `/query` administrator-verifier gap remains open.

The approved baseline remains `805b35da01efe4d3ac5f1402e611950ac282d0a1`.
Final changes are integrated on `c5c22620cafaa0af2625896cf41022e614e9b490`.
No dependency pin or owner CI policy changed. [final-verification.json](final-verification.json)
binds phase-specific source hashes, native identities/counts, logs and submodule
revisions. Earlier reports remain historical evidence.

| Final check | Result |
| --- | --- |
| Current EventStore package floor | Pass |
| Maintained Story 6.1 Package gate | 6,365 passed; zero failures/errors/skips/not-run |
| Owning suites | Contracts 2,005; Client 7; Domain 1,042; Server 1,959; UI 1,352 |
| Complete Release/package solution build | Pass; zero warnings/errors |
| Actual built-package product hook | Six packages and seven isolated consumers pass exact policy; scratch cleaned |
| Final consumer/observer units | 12/12 pass |
| Final built-hook units | 6/6 pass |
| Real executable/restore fixtures | 7/7 pass, including static/sanitized children, fileless assemblies, native restoration and detached-child cleanup |
| Actual CiCd source discovery | Zero errors; selected source hashes still match |
| Independent follow-up reviews | No unresolved implementation finding; DW-21 gap retained |

Combined-source tooling discovery ran **324** tests: **322 passed**, one error and
one environment skip. The error was a pre-existing graph verifier parsing a real
MSBuild server-fallback warning as JSON. The verifier now reads the SDK's dedicated
result file and keeps full diagnostic logs and exit/audit gates. Its affected
class passes **7/7**, including two new controls; another actual SDK graph passes
with the same real diagnostic retained. [U3 evidence](u3-cohort-parser.json).
The broad failure remains recorded and is not relabeled as an all-green rerun.
The skipped Story 5.3 real-runner test separately passes against a Release/package
Domain assembly through an explicit temporary path alias, including class/method
selection and zero-test refusal; this does not claim a Debug build.

Debug/source preparation succeeded, but the mapped build remains blocked by
the two existing EventStore `CA1822` errors in
`ProductionProfileValidationResult.Authorization` and `.Qualification`.
No audit/analyzer suppression or owner API change was made. Gateway callback
Handle/Apply history is in-memory model evidence. Direct query-dispatcher tests
do not qualify the installed HTTP endpoint, its administrator verifier,
credentials or durable-store behavior. Owner acceptance, retained-history,
migration and live-runtime qualification remain open.

Upstream CI at the integration commit already fails. Its
[policy job](https://github.com/Hexalith/Hexalith.Agents/actions/runs/43921989955/job/133491381141)
fails **Require the pinned Builds workflow source**: the workflow expects
`264e17d3185c87b72b0757dd66a13040793756bb`, while the recorded Builds gitlink is
`fdda17c0a4538b0f7c99f813c20ca24b49006530`. Its
[shared build job](https://github.com/Hexalith/Hexalith.Agents/actions/runs/43921989955/job/133491382179)
fails **Fail on blocking test failures**; that run's exact failing cases were not
available from the fetched log, while the final local suites above pass.
[Commitlint](https://github.com/Hexalith/Hexalith.Agents/actions/runs/43921989916/job/133491380937)
fails **Install dependencies** before validating messages. Static workflow inputs
also retain npm installation and deleted Story 5.2/generic verifier commands after
upstream removed those files. These pipeline defects are reported with their
run links; owner policy was preserved.

All twelve root-declared submodules are clean and their recorded heads are
reachable from fetched `origin/main`; no nested submodules were initialized.
Disposable repositories, feeds and outputs stayed beneath `run-with-temp`
workspaces, and verification children finished before cleanup.

For the maintained combined-source gates, with the documented Linux/glibc,
ptrace/inotify, C/static-libc and capable system-strace prerequisites:

```bash
run-with-temp bash _bmad-output/implementation-artifacts/tests/ingress-consumer-2026-11-16/verify-final.sh
```

The old Story 5.2 scripts, root package manifest/lock and removed tests were not
restored. The reproduction command retains the complete tracked checkout needed
by package-authority validation; it may retain the documented Debug-only skip.
