---
title: 'Synchronize and release Agents with the current SDK and packages'
type: 'chore'
created: '2026-11-15'
status: 'in-review'
baseline_commit: '347e01206dab754a3e21b4169819cad383edc414'
route: 'dispatch'
review_loop_iteration: 0
context:
  - /home/administrator/projects/hexalith/agents/AGENTS.md
  - /home/administrator/projects/hexalith/agents/references/Hexalith.AI.Tools/hexalith-llm-instructions.md
  - /home/administrator/projects/hexalith/agents/references/Hexalith.AI.Tools/hexalith-git-instructions.md
---

<frozen-after-approval reason="user authorized updates, pushes, release and bypass of human validation">

## Intent

**Problem:** Agents main records an unavailable Conversations commit and a Builds pointer inconsistent with its workflows. Its net10.0 projects consume a shared catalog now selecting .NET 11 dependencies. CI fails and the previous release failed NuGet authentication.

**Approach:** Synchronize every root-declared submodule and existing change, retain the latest stable SDK, update packages through their owning authority, repair compatibility and CI, then publish an exact-source release and verify all six packages on NuGet.

## Boundaries & Constraints

**Always:** Preserve existing user changes and pending commits. Work on main. Initialize only root-declared submodules. Push and remotely verify submodule commits before recording gitlinks. Validate Conventional Commits with the owning pinned CLI. Use run-with-temp for disposable builds, feeds and repository copies; keep useful logs outside TMPDIR. Retain package authority, warning gates, test floors, exact-source checks and package integrity checks. Use Debug/source for development and Release/packages for CI and publication.

**Never:** Initialize nested submodules, discard local work, force-push shared history, weaken failing checks, fabricate a successful publication, or change runtime business authorization/approval behavior. The user's human-validation bypass applies to this maintenance/release workflow. NuGet account authentication may still require its owner to configure the matching policy.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|--------------|---------------------------|----------------|
| Diverged dependency | Conversations has one local cleanup commit and newer remote main | Preserve both histories and push a remotely available main commit | Resolve conflicts without dropping user work |
| Current dependencies | SDK 11.0.100 and latest shared packages | net11.0 projects, consumers, scripts and package checks agree | Fix compatibility rather than downgrade silently |
| Release | Exact current main has green CI | GitHub release and all six manifest packages match the source/version | Preserve automated publication checks |
| Missing publishing identity | NuGet OIDC returns HTTP 401 | Report precise prerequisite; continue independent work | Retry after policy correction; never claim publication |

</frozen-after-approval>

## Code Map

- `.gitmodules`, `references/*`: twelve root dependencies; only Conversations diverges; Builds is one commit behind fetched main. Existing Conversations cleanup removes generated evidence outputs.
- `global.json`: SDK 11.0.100, already the latest stable Microsoft release on 2026-11-15.
- `Directory.Build.props`, `Directory.Packages.props`: target and language settings; atomic import of Builds' sole version catalog.
- `references/Hexalith.Builds/Props/Directory.Packages.props`: dependency version authority; current published EventStore 5.1.1, Conversations 2.0.0, Commons 3.0.0 and ASP.NET Core 11.
- `.github/workflows/*.yml`: currently bind Builds 6ad5854aa14a9950e1fbfc32d7e27953a1c4b82b; pin one validated updated source consistently.
- `eng/verify-story*.ps1`, `scripts/validate-*-packages.py`, `scripts/validate-consumer-package-references.py`, `tests/tooling`, `test`: active net10 paths and expectations requiring coherent migration.
- `eng/consumer-executable-assets.json`: measured package analyzer/import baseline; regenerate only from actual validated archives and inspect changes.
- `eng/release-packages.json`, `.releaserc.json`, `scripts/verify-nuget-publication.py`: exact six-package inventory and publication verification.

## Tasks & Acceptance

**Execution:**
- [x] `references/*` -- fetch, reconcile, validate and push pending work; verify remote availability -- make all recorded dependencies reproducible.
- [x] `global.json`, shared package catalog and npm locks -- verify current registry versions and update owning declarations where needed -- satisfy latest SDK/package request.
- [x] `Directory.Build.props`, active scripts/tests/consumer policies -- migrate to .NET 11 and fix compatibility -- restore a coherent package graph.
- [ ] `.github/workflows/*.yml` and contract tests -- align Builds binding and repair failures -- pass current CI without weakening gates.
- [ ] `eng/release-packages.json` publication lane -- build, test, pack and validate all six packages; commit and push all changes -- release reviewable artifacts.
- [ ] GitHub/NuGet -- dispatch release after green CI and verify exact version, assets and package contents -- establish publication evidence.

**Acceptance Criteria:**
- Given synchronized dependencies, when a fresh CI checkout initializes root submodules, then every referenced commit is available remotely and workflow assertions agree.
- Given latest compatible stable dependencies, when the maintained validation lanes run, then builds, configured tests, tooling checks and isolated package consumption pass.
- Given green current main and valid publication authentication, when Release executes, then the GitHub release and all six NuGet versions are verified against their produced archives.

## Implementation Notes

- Initial main: 347e01206dab754a3e21b4169819cad383edc414. User explicitly requested all existing changes be pushed and human validation bypassed; no additional spec approval checkpoint is required.
- Existing Story 5.4 spec/evidence changes are preserved and included in synchronization; their incomplete business acceptance is not rewritten by this maintenance task.
- Production currently has no protection rules. Publication flag is true. Missing NuGet policy was raised to the user while independent implementation continues.
- Registry audit: `artifacts/dependency-audit-2026-11-15.json` checks 317 NuGet IDs; evaluated Agents Release versions, including 21 direct dependencies, already match latest stable. Update npm `@semantic-release/github` to 12.0.11 in root, Commons, PolymorphicSerializations and Conversations; update Platform `conventional-changelog-conventionalcommits` to 10.4.1.
- Fetched Builds tip is afeee370137aaf961451e4f6881a61251273e674. Conversations origin/main already targets net11.0 and SDK 11.0.100; rebase its single existing cleanup commit onto that main, preserving all changes.
- Implementation handoff performs local synchronization, content changes, dependency installs and disposable validation only; leave commits, pushes, external release dispatch and publication verification to the coordinator after independent review. For local submodule reconciliation, use nonrecursive Git operations. Record remaining coordinator tasks explicitly when returning.

- Authentication update: coordinator confirmed Agents is selected for the existing organization `NUGET_API_KEY` secret. Release selects that inherited secret when present; otherwise it invokes NuGet trusted publishing. Publication gating and authentication failure behavior remain blocking. Credential values are never emitted.

- Coordinator adjudicated the EventStore 5.1.1 reserved-scope contract: the SDK and server deliberately reject `system` workload command-status scope. Keep runtime authorization unchanged, retain positive exact-domain composition tests using canonical tenant inputs, and add real-composed reserved-system negatives proving no assertion issuance or HTTP access. README records that default provider-catalog system status remains unavailable pending an approved platform route or host reader; the major release must disclose this compatibility limitation.
- npm compatibility and audit: Platform's latest Conventional Commits preset 10.4.1 requires writer 9+, so its owning override selects writer 9.3.0 with notes-generator 14.1.1. Polymorphic and Platform explicitly omit npm publishing, permitting the existing unused-plugin alias pattern; current audits then report zero vulnerabilities.

## Spec Change Log

## Review Triage Log


| Finding | Verdict | Evidence and route |
|---|---|---|
| BH-01 | medium | The default provider-catalog system status gap is real, but the initial main already selected EventStore 5.1.1; its client/server deliberately reject system. No runtime status reader changed in this maintenance diff. Canonical-domain positive tests and explicit real-SDK refusal tests replace an invalid expectation; README discloses the pre-existing capability gap. Defer the approved platform reader/readiness solution. |
| BH-02 | low | Owning npm changes must be committed and pushed before root gitlinks. These remain explicit coordinator tasks and no superproject commit has been created. Resolve through commit/push ordering and remote reachability checks. |
| BH-03 | low | A key with an unset trusted-publishing username fails the shared preparation contract, but the supported repository configuration retains its required nonempty username. Removing that existing prerequisite would require changing the shared owner action's input contract; reject this unsupported configuration extension for this maintenance run. |
| BH-04 | low | Secret-presence selection intentionally prefers the repository-authorized organization key and blocks invalid credentials. The current OIDC policy is absent, so automatic fallback would not repair the observed failure. An explicit override is additional configuration machinery for a future credential-management choice; reject. |
| BH-05 | medium | The .NET 11 requirement needs a major release. Resolve through a validated breaking Conventional Commit and an explicit release-plan major-version check before publication. |
| BH-06 | medium | The strict archive reader flattens dependency groups and accepts a foreign target framework despite net11 DLL inventory. Patch framework-group validation and negative fixtures; retain exact archive/consumer checks. |
| BH-07 | medium | Authentication can change during an awaited operation because the storage key is captured before the semaphore/browser waits. Those operations and ordering are unchanged from initial main; the new suppression only addresses an analyzer's type-level subscription heuristic. Defer a scoped in-flight identity design and suspended-interop regression. |
| BH-08 | medium | A first browser persistence failure after backend submission can leave no durable pending identity on refresh. The submission/persistence ordering and catch-all UI handling predate this change; the new wrapper preserves failure rather than returning success. Defer the recovery lifecycle solution. |
| BH-09 | medium | Existing deserialization accepts JSON null as an empty list and does not validate null entries. That exact expression predates the maintenance diff. Defer fail-closed stored-shape validation with corruption tests. |
| BH-10 | low | Polymorphic's explicit shared NuGet release configuration does not load the npm publisher, and locked install, signatures and zero-vulnerability audit passed after removing its unused payload. A future config edit that adds the aliased publisher is not current execution; reject extra smoke-test machinery for this mechanical tooling cleanup. |
| ECH-01 | low | Same reachable unsupported key-only/missing-username configuration as BH-03. The maintained environment supplies the shared action's required username. Reject the broader shared-action contract extension. |
| VG | false | The independent verification reviewer found no verification gaps after tracing actual storage consumers, focused tests, isolated package builds and release wiring. |

## Verification

- Run shared catalog/consumer authority validators and latest registry audit.
- Use disposable workspaces for solution restore/build, individual configured test projects, maintained package-mode story gates, tooling unit tests, six-package pack validation and isolated consumer validation.
- Inspect GitHub CI/CodeQL/commitlint results for the pushed main SHA.
- Run the existing Release workflow and exact six-package NuGet verification; preserve run URLs, version, source SHA and any concrete authentication blocker.

- Local implementation verification (2026-11-15): SDK 11.0.100; Release/package solution build passed with zero warnings/errors; all five configured projects passed 6,144 tests with zero skips. Story 5.2 focused and full regression evidence passed. The complete Story 5.1 package gate passed shared authority (66 projects, 40 exact archived-project exclusions), the EventStore floor (13 rows at 5.1.1), dependency-mode negative checks, six-package pack/archive validation, and seven isolated consumers against the reviewed executable-asset baseline. Logs: `artifacts/sync-release-2026-11-15/package-gate-verified.log` and `tests-acceptance.log`.
- Tooling verification: full suite completed 287 tests (285 passed and two pre-existing optional checks skipped) because the isolated tooling workspace lacks the Debug Domain test assembly; the affected package/verifier suite completed 75 tests (73 passed with those same two skips). New authentication/exclusion checks passed. Platform source-release suite passed 82/82; locked installs, registry signatures, and zero-vulnerability npm audits passed for all updated roots. Logs: `tooling.log`, `tooling-affected-final.log`, and `npm-verified.log` in the same artifact directory.
- Debug/source fallback: Contracts project build and 1,997 tests passed with zero skips. Full source preparation remains blocked by the Platform checkout having no exact release tag at `ddb1c0b141372a66d4a9d9c231d0041afbb7cd0c`, its pinned Builds tuple `e2bbbd0893f586e4d447ef8358ff747ea3d34435` differing from current Builds, and local source-tool version identity; raw diagnostics also reflect the disposable pre-commit gitlink index. Direct full Debug fallback correctly refuses Conversations without the governed source mapping. No proof or source-mapping gate was bypassed. Evidence: `source-mapping-diagnostic.log`, `debug-fallback.log`, and `debug-focused.log`.
- Coordinator work remains: independently review, commit and push owning submodules, verify their recorded commits remotely, commit/push root main with an appropriate breaking release declaration, wait for exact-source CI/CodeQL/commitlint, dispatch Release, and verify the GitHub assets and all six NuGet packages. No publication is claimed by local validation.

- Final combined disposable package gate exited 0, including its subsequent complete Story 5.2 verification. `git diff --check` and `git diff --cached --check` passed.

- Review patch verification: all 20 archive-validator tests passed, including wrong/missing/extra target frameworks, and the coordinator revalidated all six real net11.0 archives successfully.

- Coordinator pushed and remotely verified Commons ab5e92faa3bab1fa43b8ecadd423432dfe652bb2, PolymorphicSerializations 476cfccac3c5ba694a8e0ef11b1157728b9ec093, and Platform d386e36005214f925b391a34684748e6c56728e6. The Platform push reported the authorized pull-request-rule bypass. All twelve root dependencies were fetched without recursion and remotely verified before the parent commit.

- Final synchronization advanced Builds to 264e17d3185c87b72b0757dd66a13040793756bb; its only change after the locally verified afeee370137aaf961451e4f6881a61251273e674 producer is correcting two test expectations from EventStore 5.1.0 to 5.1.1. Catalog and workflow/action implementations are identical. Updated all active workflow/contract pins together. FrontComposer and McpCli also fast-forwarded to their remotely verified tips; Release/package dependencies are unchanged.
