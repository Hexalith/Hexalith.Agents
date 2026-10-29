# Published setup verification — 2026-10-29

This delivery enables published-SDK command status, reconciles five selected deferred records, and includes the 37 staged Story 5.4 files expressly authorized by the user. Four records keep their earlier 2026-10-25 local completion date; the published capability follow-up is newly completed on 2026-10-29.

| Record | Verified disposition |
| --- | --- |
| DW-7 | Existing live reader binding is exercised with published SDK transport and real issuer credentials; original status now agrees with its closure addendum. |
| DW-12 | Existing terminal rejection and retained accepted-write behavior pass the complete package UI suite; original status now agrees with its closure addendum. |
| DW-24 | Exact tenant/domain workload delegation works with published Client/DomainService 3.128.3; original status now agrees with its closure addendum. |
| DW-26 | Existing unbound/loading/unavailable admission safeguards pass package and source UI suites; original status now agrees with its closure addendum. |
| Published exact-domain SDK follow-up | Removed the obsolete source-only capability guard and package refusal. Both modes compile the same adapter and tests. The maintained floor requires stable compatible versions and both Client and DomainService rows. |

DW-25 is the historical alias owned by DW-12 and is reconciled without counting it separately. DW-6, DW-19, DW-21, external owner qualification and the full Story 5.4 acceptance remain separately governed.

## Package and local evidence

The maintained `pwsh -NoProfile -File eng/verify-story-5.2.ps1 -Configuration Release` passes on the final changes, including warning-free package restore/build, all focused/composition checks, and every owning test project:

| Project | Passing cases |
| --- | ---: |
| Contracts | 1,997 |
| Client | 7 |
| Domain | 1,017 |
| Server | 1,834 |
| UI | 1,249 |
| Total | 6,104 |

Every native summary reports zero failures, errors, skips and unrun cases. The final direct authority/gate selection passes 223 cases. Real issuer tests cover Agent, platform Provider catalog and tenant enablement domains, configured and missing credentials, exact claims, workload routes and undeclared-domain refusal. HTTP/backend responses remain local substitutes; these tests establish no deployed gateway, Dapr persistence or dependency availability.

Published API inspection uses `dnx dotnet-inspect -y -- member IEventStoreGatewayWorkloadAssertionSource --package Hexalith.EventStore.Client@3.128.3 -m IssueAsync` and the corresponding DomainService transport extension inspection. Current Builds already selects 3.128.3; no catalog version or submodule pointer changed.

The complete tooling suite passes all 281 tests with zero skips. Canonical dependency revisions, exact commands, native XML summaries, source fingerprints and artifact hashes are retained in [verification.json](tests/published-setup-status-2026-10-29/verification.json). [evidence.tar.gz](tests/published-setup-status-2026-10-29/evidence.tar.gz) retains the corresponding build/test logs, XML and published API output.

## Source validation boundary

`pwsh -NoProfile -File eng/verify-story-5.4.ps1 -Mode Local -ArtifactsPath /tmp/agents-published-status-story54` freshly builds warning-free and passes 1,997 Contracts, 7 Client and 1,017 Domain tests, then stops building Server because Conversations requires its Platform tool-selected source mapping. The maintained Story 5.2 source restore stops at the same requirement. The focused UI Debug/source build passes warning-free and all 1,249 UI tests pass.

The exact blocker is `references/Hexalith.Conversations/Directory.Build.props:8`: “Conversations requires the Platform tool-selected source mapping; validation does not resolve dependency mappings.” The mapping guard and all build gates remain intact. These partial source results and complete package results are separate evidence; neither completes the parent story or live acceptance.

## Independent review

Blind Hunter inspected the complete staged/unstaged changes and returned ten findings. All were checked and patched; its final recheck reports no remaining concrete issue. Fifteen targeted regressions reproduced eight failures before correction. The final changes retain original authority through nested provider preflight/dispatch/receipt resolution, check payload-less Agent status before and after awaits, deny faulted setup reads after expiry, retain tenant clock history and shorter role deadlines through gate admission, map expected final refusal to Denied, strengthen package-floor fixtures, and document explicit authority-reference preservation for host admission. The implementation spec retains all ten triage records.

No deployed owner qualification, deployment, provisioning, external publication or nested submodule initialization was performed. Main delivery uses the user-authorized push after Conventional Commit validation and divergence inspection.
