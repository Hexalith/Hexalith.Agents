---
title: 'Fix CI package-mode verification for release'
type: 'bugfix'
created: '2026-10-28'
status: 'done'
route: 'oneshot'
review_loop_iteration: 0
context: []
---

<frozen-after-approval reason="user-authorized CI fixes and unattended release">

## Intent

**Problem:** The Agents policy job runs Debug/source verification inside CI, which fails because updated dependency sources require Platform-generated mappings. Workflows and package contract tests must also follow the synchronized Builds revision.

**Approach:** Retain local source verification while explicitly selecting Release/package verification in CI for Story 5.1 and Story 5.2. Preserve audit failures, per-project floors, focused selections, skip rejection, dependency-mode negative checks, six-package inventory, isolated consumers, and exact-source release checks. Update immutable workflow pins and their contract expectations together. The user explicitly authorizes pushing, release dispatch and publication without human validation.

</frozen-after-approval>

## Implementation Notes

- GitHub CI run 40570052086 proves the shared Release build and tests pass while the policy source build fails on Conversations source mappings.
- Root submodules have been fast-forwarded to upstream main; nested submodules remain uninitialized.
- Scope: CI workflow pins, two verification scripts, existing contract tests and focused executable tooling regression tests. No new public application API.
- Validation: focused tooling tests and workflow lint, then exact-source GitHub CI. Release and NuGet publication follow once CI succeeds.

- Added explicit Package selection to Story 5.1; Story 5.2 accepts Debug/source or Release/package and canonicalizes accepted configuration casing for Linux paths.
- Executable successful-lane coverage exposed ProcessStartInfo resolving a different dotnet than PowerShell PATH. Negative gates now resolve the same application explicitly and run from the verifier root.
- Focused regression evidence passed: five audit/pipeline tests; four Release verifier tests and one casing test. Workflow lint passes.

## Review Triage Log

- R1 — low, patched: case-insensitive parameter validation accepted lowercase configuration but Linux assembly paths require canonical Release/Debug casing.
- R2 — rejected: Both retains the pre-existing explicit dual-lane verifier behavior; the new CI command selects Package. A source-only local command is outside this CI correction and is not required by the preserved verifier contract.
- R3 — medium, patched: added successful Package execution proving five project floors, skip rejection, dependency-mode failures, inventory and isolated consumers all remain reachable.
- R4 — medium, patched: added controlled Release restore/build audit failures proving execution stops before test assembly invocation.
- R5 — medium, patched during R3 validation: negative-gate child processes now use the PATH-resolved dotnet and verifier working directory, avoiding the host executable-directory preference and accidental workspace execution from a fixture.

- Release solution build passed with zero warnings/errors. Five complete test projects passed with zero skips: Contracts 1997, Client 7, Domain 1017, Server 1556, UI 1249 (5826 total).
- Story 5.1 package lane passed all dependency rejection gates, exact six-package validation, and seven isolated consumers at 0.0.0-story-5-1.
- Story 5.2 Release focused/regression/composition verification passed unchanged selections and no skipped tests.
- Workflow pins and contract expectations follow Builds b15dab4cc740b6fb2d2065682f76b8166a7e120a, whose audit refresh was separately validated upstream. All seven focused package/workflow contract tests passed after updating the pin.

- Took the final root-only dependency snapshot after syncing upstream fixes; all nested dependencies remain uninitialized. Full local tooling discovery remains in progress. Exact-source GitHub CI is required before the separately authorized release dispatch.

- Full local tooling discovery passed all 272 tests (988.058 seconds). npm verified 504 package signatures and 127 attestations.
- Submodule CI investigation additionally found Aspire SDK/catalog drift in EventStore and McpCli and a stale FrontComposer hosting-version assertion. Their owning repositories received focused, validated repairs; root pointer synchronization follows those upstream commits without initializing nested checkouts.

- Adopted upstream local source-mapping preparation; all five audit/pipeline regression tests pass after that integration.
- Final Builds execution pin is 4250edc94af021945fe072085f02b5122c9b21c3, superseding b15dab4 after the AppHost exception-inventory repair. All 16 inventory entries and 30 validator scenarios passed upstream; root workflow lint and all seven package/workflow contract tests pass with the new pin.
- Adopted the upstream Parties consumer-fixture repair after both affected tests and its Release build passed.
- Conversations repair preserves the historical SM-2 qualification boundary, restores only the frozen AppHost fixture bytes, and retains a newly measured v4 projection successor. Nine baseline checks, seven public-package compatibility tests, eleven tooling checks, and two independent clean-cache restores passed. Final conformance validation remains with the owning submodule before root synchronization.

- Conversations final conformance passed 297 portable and 90 internal cases before its owning repair was pushed.
- Exact-source GitHub CI completed the Release build, all per-project floors, exact package inventory and seven isolated consumers, then exposed ANSI-colored xUnit summary records in the focused verifier. The verifier now removes terminal formatting before its unchanged strict assembly/count/skip checks; executable colored-summary acceptance and colored-skip rejection regressions pass.

- Follow-up quality checks found FrontComposer catalog-selector and McpCli workflow-coordinate drift. Their owning fixes passed three governance and 22 release checks, and 139 topology-tooling checks, respectively; the final root synchronization includes both published fixes.

- Complete GitHub CI, CodeQL and commitlint passed at 07fd1e19878d7d2ad1fbf33345b97ad9a1dc47b3, including the ANSI-summary repair and all tooling contracts.
- Final shared validation source advances to Builds 9f85d8e66e5c16675cee8c224f1fd88430d96007. Its HTTPS trust setup now covers .NET-only Aspire tests; 408 workflow assertions and 61 provenance checks pass, with the existing certificate-validation behavior retained.
- Adopted upstream Parties scoped HTTPS preparation and its independent execution coordinates, discarding the superseded local variant. Its build and CI/documentation guards pass; flat dependency validation preserves the no-nested-checkout constraint.
- Adopted upstream Conversations exact package-only condition handling, retaining unknown-condition rejection; the failed method and eleven direct guard checks pass.
- Adopted upstream Memories selected-catalog assertions (twelve security checks pass) and narrowly bounded same-instance Dapr inbox read retries. Original status assertions remain intact; obsolete-run cancellation and the local nested-layout limitation are recorded in external validation logs, without treating them as passing runtime evidence.

- Shared Builds CI passed at 9f85d8e66e5c16675cee8c224f1fd88430d96007. Complete Agents CI, CodeQL and commit validation passed at 395dd7ebf7f25459e37c31d884bcfe6a785b2436, including Release build/tests, package floors, six archives, seven consumers, focused verification and all tooling contracts.
- Adopted the already-green EventStore legacy replay/qualification-credential fixes at 903174461632e7bcabd6111092aa059f16e1b1c7 and Tenants consumer synchronization at 9202960cc31e4d6f4fa82a9f6a957ec0e12dba18, plus published Platform metadata at eee6ce27965b7c7108bbfcc02d447038e0553751.
- Conversations 986875bfcb5471953378b600710b5d859f3eaba4 preserves mapped AppHost provenance, supplies the retained topology's declared enrollment/state/pubsub bindings, and serializes the existing minimal-module fixture build. Independent review accepted the diff; source-mapped build passed without warnings and eleven compatibility checks passed in an isolated fixture. Full hosted model/runtime proof remains pending; nested-layout local blockers are not counted as passes.
- FrontComposer 1ccb4afc47ec8f1bb5379fd98452242546bbd06c recognizes only the exact inactive EventStore qualification override in its own catalog profile. Independent review, 107 graph tests, unchanged-policy validation across 51 edges/seven selectors, a warning-free Shell build and nineteen infrastructure checks passed. Hosted CI and Quality remain pending.
- Adopted Parties 35e82673a1e3e7fa6c13a2c996fa4eb7f56c179e, which retains the existing five-minute overall topology deadline while allowing cold sidecars the remaining startup budget; exact hosted Aspire verification remains pending.
- NuGet publication preflight confirms all six planned 1.2.1 versions are absent. The prior release's first push failed HTTP 403; organization NUGET_API_KEY metadata is unchanged since 2025-09-20. Requested the user's credential refresh asynchronously while continuing CI repairs and exact-source release preparation. No credentials are stored here.

- Complete Agents CI, CodeQL and commit validation passed again at bd2956a50cef3f44e0f1f1b01b4dcc68ec09aecc. No application behavior changed in the subsequent owning-repository CI qualification repairs.
- Commons 3edfbdafc7d4fa915286dce8a37dc4cc7c06085a adds synchronized instructions to validate incoming dependency commits and squash invalid incoming messages into a validated integration commit. Existing commitlint rules and published history are preserved; exact CI, CodeQL and commitlint all passed.
- Tenants 0a2adfcdc843cca34e1aad65b4324253a4642b19 adopts the published credential-free release planner repair while retaining protected publication authentication. Fourteen planner tests, real unauthenticated version calculation, five warning-free package builds/packs and isolated consumers passed; complete hosted CI including Aspire, Story Guards, source-reference, CodeQL and commitlint passed. Its existing trusted-publishing authentication remains separate from Agents' legacy secret.
- Parties ebdaef74d3fd3572174d187b281842463de25d21 includes the reviewed deferred Admin.Server binding to the owned sidecar's actual HTTP TargetPort, fresh same-PID kernel exit proof, upstream UI listener diagnostics and a bounded fixture port pool outside kernel/client ephemeral ranges. Thirty-seven combined focused controls passed without skips and warning-error builds passed; exact hosted runtime qualification remains pending.
- Memories fecd80ed0b01994b6ede45bbb59cdef35325bb3b preserves the existing maximum sixty-minute unit-job guard while accommodating the measured all-passing producer fixture duration. Its fresh integration run passed the prior deletion-status regression but exposed a workflow-registration startup race before a strict tenant-creation POST.
- Memories 86011d4dc1e16d0d8222e07e6a492faf8e2347c0 adds a bounded read-only readiness precondition against the exact fixture-owned sidecar: a connected workflow worker, ready actor host and both registered internal workflow/activity types, including zero-count registrations. The provisioning POST remains single-shot with its original result assertions. Independent review, sixty-four focused helper/fixture cases without skips and a warning-free Release build passed; full new-head CI remains pending.
- Root URL configuration was synchronized for all twelve root-declared modules; all nested checkouts remain uninitialized. Exact-current-main proof and publication verification remain required for the separately authorized Agents release.

- Complete Agents CI, CodeQL and commit validation passed at 3c6b36130927413909e6b401bd87f92cbe7ebf44. A real 1.2.1 candidate produced all six warning-free packages, passed package validation and seven isolated consumers, and recorded that exact source SHA in every package.
- Memories 86011d4dc1e16d0d8222e07e6a492faf8e2347c0 passed hosted integration: 435 passed, nine existing optional skips, no failures. Both earlier failing scenarios and all nine readiness cases passed; its retained unit-contract lane continues under the unchanged sixty-minute maximum.
- Parties f7bd8ffa7943499f6e10db2aa02ab793c5a29a91 supersedes the earlier cross-resource TargetPort helper with the exported CLI EndpointReference. A producer-only DCP port template is now included in regression coverage. Its actor-host HTTP channels use actual listeners rather than early-listening startup proxies, preventing Dapr from permanently caching an empty app configuration before Kestrel starts. The exact gateway, scoped Memories host and existing explicit-start UI are covered while unrelated endpoints remain intact.
- The Parties continuation preserves the concurrent upstream gateway actor-registration guard and all seed/persisted-state assertions. Independent review, forty-nine focused integration controls, thirty-four CI guards, a warning-free build and pinned commitlint passed; exact hosted CI remains pending. The next root checkpoint requires its own exact-source CI proof before publication.

- Complete Agents CI, CodeQL and commit validation passed at 70b5a5eb81cea3fb93cb1cde0f35985d86cb73cf; live exact-main proof passed and the release planner still selects 1.2.1 with all six registry versions absent. Memories' complete sixty-minute-bounded CI also passed at 86011d4dc1e16d0d8222e07e6a492faf8e2347c0.
- Parties' f7bd hosted run passed both counter seeds/persisted streams and all four live shutdown cases. Its remaining UI failure traced to cold concurrent native builds missing EventStore.Contracts.dll, so the original UI identity assertions remain intact.
- Adopted published Parties 3e1dd7cac6ee4db1db48d1bfb478e1b5848f7b8e: the existing certificate preparation runs first, followed by serial native Release builds of the exact eight external runtime projects. Normal restore, native dependency graphs and warnings-as-errors remain enabled; the ten-minute preparation bound retains the thirty-minute runtime job and existing startup deadline. No experimental launch override or assertion relaxation is introduced. Upstream qualification records eight warning-free native builds plus producer-order/failure and declared-runtime-inventory controls; exact hosted CI remains pending.
