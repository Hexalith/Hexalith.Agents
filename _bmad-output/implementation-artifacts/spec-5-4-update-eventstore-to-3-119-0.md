---
title: '5.4 Complete EventStore 3.119.0 compatibility'
type: 'chore'
created: '2026-10-09'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'd12ac2ffe7b8bf00972da38cf3a61fd9db531fb8'
context:
  - '_bmad-output/implementation-artifacts/epic-5-context.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 5.4 needs Hexalith.EventStore 3.119.0. The effective dependency version and checked-in source must be verified before making a redundant or regressive update.

**Approach:** Confirm the shared package catalog and package-mode MSBuild evaluation select 3.119.0, then verify restore/build and relevant tests. Change dependency references only if the effective version is below 3.119.0.

</frozen-after-approval>

## Implementation Notes

- The authoritative catalog at `references/Hexalith.Builds/Props/Directory.Packages.props` already sets `HexalithEventStoreVersion` to `3.119.0`; package-mode MSBuild evaluates that exact value.
- The checked-in `references/Hexalith.EventStore` source is at `28c66fae56bc006d8fdde2bf76b1170154fa5b29`, three commits after tag `v3.119.0` (`f463442cca19e4199982a23a08bae4a490767d4a`). No dependency pointer change was needed.
- Package-mode solution restore and serialized Release build succeeded with zero warnings and errors. Every source project's resolved EventStore packages are `3.119.0`. All 18 `BuildContractConformanceTests` passed.
- The package-mode gateway and provider-catalog integration test run had 105 cases, with three failures in `A_command_the_setup_adapter_rejects_fails_on_the_gateway_before_execution`: its package-only branch expects retryable HTTP 503, but EventStore 3.119.0 returns nonretryable HTTP 400 with `correct_request`. The existing source-mode branch already expects HTTP 400. The version update therefore needs a small test compatibility fix before completion.
- Debug source-reference restore succeeded. The Debug server test project build is blocked in sibling `references/Hexalith.Conversations/src/Hexalith.Conversations.Client` by nine CA1062 errors. The EventStore source projects compiled before that blocker. Do not weaken the build gate or change Conversations for this version task.

## Code Map

- `references/Hexalith.Builds/Props/Directory.Packages.props` — authoritative `HexalithEventStoreVersion` is already `3.119.0`; preserve this single version source.
- `Directory.Packages.props` — imports the shared catalog without overriding the EventStore version; no change needed.
- `references/Hexalith.EventStore` — checked-in source commit is three commits after the `v3.119.0` tag; avoid moving the pointer backward and losing subsequent fixes.
- `test/Hexalith.Agents.Server.Tests/AgentsEventStoreGatewayIntegrationTests.cs` — remove the obsolete package-only 503 branch in `A_command_the_setup_adapter_rejects_fails_on_the_gateway_before_execution`; use its existing 400/nonretryable source assertion for both modes.

## Tasks & Acceptance

**Execution:**
- [x] `test/Hexalith.Agents.Server.Tests/AgentsEventStoreGatewayIntegrationTests.cs` — make all three invalid-command rows assert the EventStore 3.119.0 response (`400`, `retryable: false`, `clientAction: correct_request`) and preserve zero execution/ledger assertions.
- [x] `_bmad-output/implementation-artifacts/spec-5-4-update-eventstore-to-3-119-0.md` — record exact package-mode verification commands and results, including the independent source-mode build blocker.

**Acceptance Criteria:**
- Given package-mode Release restore against the shared catalog, when the Agents solution builds, then every resolved EventStore package is `3.119.0` and the build succeeds without warnings or errors.
- Given an invalid setup command in any of the three existing test rows, when the EventStore gateway rejects it, then it returns nonretryable HTTP 400 with `correct_request` and neither domain execution nor admission ledger execution occurs.

## Verification

**Commands:**
- `dotnet restore Hexalith.Agents.slnx -p:UseHexalithProjectReferences=false -p:NuGetAudit=false` — already passed.
- `dotnet build Hexalith.Agents.slnx --no-restore --configuration Release -p:UseHexalithProjectReferences=false -p:NuGetAudit=false -m:1` — already passed, zero warnings/errors.
- `dotnet test/Hexalith.Agents.Server.Tests/bin/Release/net10.0/Hexalith.Agents.Server.Tests.dll -class Hexalith.Agents.Server.Tests.BuildContractConformanceTests` — already passed, 18/18.
- `dotnet test/Hexalith.Agents.Server.Tests/bin/Release/net10.0/Hexalith.Agents.Server.Tests.dll -class Hexalith.Agents.Server.Tests.AgentsEventStoreGatewayIntegrationTests -class Hexalith.Agents.Server.Tests.ProviderCatalogEventStoreIntegrationTests` — rerun after the test correction; baseline 102/105 passed.
- `dotnet build test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj --no-restore --configuration Debug -p:UseHexalithProjectReferences=true -p:NuGetAudit=false -m:1` — baseline blocked by nine CA1062 errors in the Conversations.Client sibling; report separately from package-mode evidence.

**Results (2026-10-09):**

- `dotnet restore Hexalith.Agents.slnx -p:UseHexalithProjectReferences=false -p:NuGetAudit=false` — passed.
- `python3 -c "import json,pathlib; files=sorted(pathlib.Path('src').glob('**/obj/project.assets.json')); packages=[(str(p),n) for p in files for n in json.loads(p.read_text())['libraries'] if n.startswith('Hexalith.EventStore.')]; print(len(files), len(packages)); assert packages and all(n.endswith('/3.119.0') for _,n in packages)"` — passed after package-mode restore: 7 source project assets files, 17 resolved EventStore package entries, all `3.119.0`.
- `dotnet build Hexalith.Agents.slnx --no-restore --configuration Release -p:UseHexalithProjectReferences=false -p:NuGetAudit=false -m:1` — passed with 0 warnings and 0 errors.
- `dotnet test/Hexalith.Agents.Server.Tests/bin/Release/net10.0/Hexalith.Agents.Server.Tests.dll -class Hexalith.Agents.Server.Tests.BuildContractConformanceTests` — 18 passed, 0 failed or skipped.
- `dotnet test/Hexalith.Agents.Server.Tests/bin/Release/net10.0/Hexalith.Agents.Server.Tests.dll -class Hexalith.Agents.Server.Tests.AgentsEventStoreGatewayIntegrationTests -class Hexalith.Agents.Server.Tests.ProviderCatalogEventStoreIntegrationTests` — 105 passed, 0 failed or skipped; all three invalid-command rows passed.
- `dotnet restore test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj -p:UseHexalithProjectReferences=true -p:NuGetAudit=false` — passed independently for Debug source references.
- `dotnet build test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj --no-restore --configuration Debug -p:UseHexalithProjectReferences=true -p:NuGetAudit=false -m:1` — blocked by 9 CA1062 errors in `references/Hexalith.Conversations/src/Hexalith.Conversations.Client`; EventStore source projects compiled before the blocker. No build gate was weakened.
- After the review patch, package-mode restore and the serialized Release solution build passed again with 0 warnings and errors; the build-contract class passed 18/18 and the gateway/provider-catalog classes passed 105/105. Inspection of all 12 `src` and `test` `project.assets.json` files found 29 EventStore package entries, all `3.119.0`.

The parent commit pins `references/Hexalith.EventStore` to `28c66fae56bc006d8fdde2bf76b1170154fa5b29`. During this run, its local worktree was already at `37451b529ab21869fa4e2806968b5143ddeea14b`; this implementation did not move the pointer.

## Review Triage Log

- **false — Blind Hunter, EventStore gitlink:** The parent worktree contains an unstaged external move to `37451b52`; the implementation did not move or stage it, and it will not enter this change's commit.
- **false — Blind Hunter, version floor:** `eng/verify-story-5.2.ps1` deliberately enforces Story 5.2's older minimum. The shared catalog currently selects `3.119.0`, and the gateway integration assertion detects the prior retryable response.
- **maybe-false, deferred — Blind Hunter, source mode:** The Debug test project cannot reach the changed test because of nine CA1062 errors in Conversations.Client. The source-mode assertion was already HTTP 400 before this change; a passing source-reference build and run after that independent blocker is corrected would settle compatibility.
- **false — Blind Hunter, Story 5.4 scope:** This artifact's title identifies EventStore compatibility only; the canonical trusted-principal Story 5.4 spec and sprint status remain untouched.
- **false — Blind Hunter, test-project assets:** A follow-up check inspected all 12 source and test asset files and found 29 EventStore entries, every one at `3.119.0`.
- **low, patched — Edge Case Hunter, unused symbol:** The gateway test no longer consumes `HEXALITH_EVENTSTORE_FROM_SOURCE`; removed its sole definition from the server test project and rebuilt successfully.
- **false — Edge Case Hunter, EventStore gitlink:** The additional local gitlink movement is unstaged and was not produced by the implementation.
- **false — Verification Gap Reviewer, EventStore gitlink:** The published package checks do not claim to validate the externally moved source pointer; the parent commit's pin and the local checkout are recorded separately.
