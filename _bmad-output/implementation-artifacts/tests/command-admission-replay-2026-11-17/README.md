# Command admission and replay evidence — 2026-11-17

## Final review corrections and coordinator verification

The final full Domain suite passes **1,084/1,084**; the final full Server suite passes **2,062/2,063**, with only the same baseline-reproduced `OrdinaryReleaseShouldRejectAllAuditFindings` failure. Together with the unchanged Contracts, Client and UI suites, package verification executes **6,521** cases: **6,520 pass**, one policy failure, zero errors/skips/not-run. Both final affected builds have zero warnings/errors. All **66 named command-admission controls** pass in the final native XML. The focused review corrections separately pass **48/48** cases (37 Domain and 11 Server).

The gateway inventory now uses ten handler-valid cases spanning all nine command types, with separate provider authorization and protected generation. Alternate casing and quoted revisions agree with typed SDK binding and actual `ProcessAsync` decisions; public Handle/Apply positive controls succeed. Malformed inputs require exact binding exceptions. Nonces remain intent-neutral, actor authority remains significant, and missing/foreign authority rejects both initial writes and accepted duplicates. Phase guards independently cover the remaining authority members, a real foreign-phase committed lease, and misplaced digest/key metadata. Existing Provider and ProposalMutation failure-binding controls continue passing in the full Server suite.

The coordinator also passed the full Release solution build (zero warnings/errors), direct package-authority validation for all **66 projects**, exact-message commitlint, and fresh publication/clean-state checks for all twelve root-declared submodules. The existing CI workflow's separate Builds-pin check fails: it requires `264e17d3185c87b72b0757dd66a13040793756bb`, while the concurrently published parent selects `717e5ab29569fc57b0f43415f1aee2fc93441859`. Neither the CI pin nor audit policy was weakened or edited.

Final commands, XML, hashes and logs are retained in `/home/administrator/verification/agents-command-admission-20261117/final-owning-suites/` and `coordinator-checks/`. The [manifest](verification.json) separates initial evidence, review corrections, final source bindings and coordinator checks. The spec records all ten review verdicts. Historical camelCase retained-descriptor exposure remains unverified without an accepted host/retention inventory; the first-party dispatcher writes PascalCase, and no migration compatibility is invented.

After the failed-verification escalation, the user explicitly instructed the coordinator to push all changes to main. Delivery proceeds under that direction with both known policy failures preserved; neither gate is weakened. Exact commit and remote delivery are recorded in Git. Earlier partial code and reference updates were independently published by the concurrent `/pushall` session.

The implementation completes the five selected Story 6.1 patches: shared payload reader options, business revision identity, trusted delivery metadata, public-handler authority and isolated phase-fact guards. The adapter retains its existing PascalCase writer and transport-revision normalization. The trusted policy and aggregate guards require no production changes.

**Initial focused verification passes 53/53 cases:** 28 Domain and 25 Server, with zero warnings, failures, errors, skips or not-run cases. Debug/source also passes the 28 focused Domain cases and the full **1,075/1,075** Domain suite. The [manifest](verification.json) records every named focused case, exact native XML hashes, initial five compiled code/test hashes, snapshot revisions and executed commands.

**The maintained Release/package gate exits 1.** Its completed suites are Contracts **2,005/2,005**, Client **7/7**, Domain **1,075/1,075** and Server **2,058/2,059**. The single failure is the existing `BuildContractConformanceTests.OrdinaryReleaseShouldRejectAllAuditFindings`: consumed Builds `717e5ab29569fc57b0f43415f1aee2fc93441859` exempts `NU1901;NU1902` in `Props/Directory.Packages.props`. The same focused assertion fails on clean Agents baseline `492a0217dfa08f1dc57b5d24840a257c1bd30861` with identical policy-input hashes and the same Builds revision. No audit or test is weakened. UI, which the verifier had not reached, passes separately **1,362/1,362** against the same bound code/build inputs. Together the five package suites execute **6,508** cases: **6,507 pass**, one reproduced external policy failure, zero errors/skips/not-run. All five package project builds report zero warnings/errors.

| Patch | Executed controls |
| --- | --- |
| Payload casing | `RegisteredAutomaticAdaptersBindEveryDeclaredCasingAndPreserveOpaqueProtectedValues`: PascalCase, camelCase and mixed case for all nine registered commands and their nested records; changed business values stay distinct; opaque key spelling, ciphertext, embedded `ExpectedRevision` and nested `Fact.Revision` stay significant; malformed object/array/null/revision inputs refuse. |
| Business revisions | `RegisteredAutomaticAdaptersRetainEachBusinessRevisionUnderEveryWireSpelling`: seven field locations cover lease and settlement `CommitRevision`/`ResultRevision`, creation `PermitRevision`, acknowledgement `InteractionCreationRevision` and `Fact.LeaseCommitRevision`; independent `ExpectedRevision` and `Fact.Revision` controls retain equality. The existing nine-command canonical-intent test also passes. |
| Delivery metadata | `AutomaticDeliveryMetadataRequiresGovernedRouteExactProofAndAuthenticatedAgentsWorkload`: all nine commands claim the governed `trust:deliveryNonce`; exact proof, principal, request, key/value, route, Dapr authentication, Agents workload and nonblank values are enforced. Existing nine-command authority policy cases also pass. |
| Public handler authority | `InteractionCommandAuthorityTests.CorrectAuthorityAcceptsWhileMissingAndOtherActorKeysRejectWithoutBusinessEffects`: nine handlers each execute correct, missing and foreign-key envelopes. Positive controls pin the intended event; refusals pin `InvalidTarget` or the existing lease-operation `LeaseNotFound`, then replay the rejection without changing accepted state. |
| Phase guards | `AutomaticInteractionPhaseGuardTests.InvalidFactRejectsForItsOwnReasonAndReplaysWithoutAcceptedPhaseChange`: Final 28 cases isolate stale revision inputs, ordering, lease defects including substitution of a real committed capability from another phase, every required authority member, and independent protected-output defects. Public Handle/Apply prefixes acquire and commit real fixture directory leases. Each exact typed refusal preserves accepted phase state, and the neighboring valid command and exact retry pass. |

The original reader was restored **only inside disposable scratch** for a red reproduction: PascalCase passed; camelCase and mixed case failed (1 pass, 2 failures, exit 1). Restoring the final adapter yields the passing focused cases and full owning behavior evidence above.

Debug/source Server remains blocked by Conversations' required Platform tool-selected source mapping (`Directory.Build.props:8`), before Agents Server compilation. The earlier EventStore CA1822 blocker did not recur at the consumed source revision. The first implementation run caught a new-test CA1062 diagnostic; it was corrected with argument validation before the verified snapshot.

The apparently stalled full Server run completed naturally. Existing `PackageVersionCentralizationTests.WholeRepositoryShouldHaveOnePackageVersionAuthorityAsync` spent 285.706 seconds in `Process.WaitForExit`, then passed. A retained triage dump and a temporary current diagnostic tool identified that stack; installed `dotnet-dump` 9 could not read the .NET 11 debugging layer. These bulky diagnostics remain outside the repository.

Exact reproduction scripts, build logs, native XML, source bindings and diagnostic evidence are retained under `/home/administrator/verification/agents-command-admission-20261117/`:

- `implementation-02/run.sh`: complete local checkout, root-declared submodules only, Debug/source checks, native focused cases, old-reader reproduction and the unsuppressed maintained package gate.
- `ui-continuation-03/run.sh`: remaining UI build/suite, with matching code/build input hashes.
- `baseline-audit-04/run.sh`: clean approved Agents baseline plus the exact consumed Builds revision, then the single existing audit assertion.

Re-run a retained script from any directory with `bash <script-path>`. Each uses `run-with-temp`, local Git copies, explicit native xUnit selectors and durable output paths; choose fresh evidence paths before re-running to preserve these results. To run only the maintained gate in an already prepared disposable checkout:

```bash
pwsh -NoLogo -NoProfile -File eng/verify-story-6.1.ps1 -Mode Local \
  -DependencyMode Package -ArtifactsPath "$ADMISSION_EVIDENCE/package"
```

Snapshot root was `fcb51cad0178b1cf60b8d8be8410cd1f938116ac`; exact consumed root-declared revisions are recorded in the manifest because concurrent external Git delivery advanced the workspace. The initial snapshot checked 1,061 stable code/build inputs. Final corrected source hashes are separately bound to the coordinator owning-suite results; the initial and final manifests remain distinct. The wrappers and diagnostic children finished before scratch cleanup. No Git staging, commit, push or submodule update was performed in this handoff.

This is Level 2 evidence. Live dependencies, owner authority/credentials, AD-29 identities, Closing/migration policy, deployment and package publication remain outside this work. All three independent reviews completed, and nine review patch findings are corrected. The final coordinator checks below retain the broad audit-policy failure and source Server blocker as explicit limits.
