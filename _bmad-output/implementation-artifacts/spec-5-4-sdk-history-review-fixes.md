---
title: '5.4 Retained history SDK bounded-await review fixes'
type: 'feature'
created: '2026-10-06'
status: 'in-progress'
route: 'dispatch'
human_approval: 'accepted'
baseline_commit: 'd24a03569ed0c8e4d773e24e630a2eab2ec9f074'
review_loop_iteration: 0
context:
  - '/home/administrator/projects/hexalith/eventstore/AGENTS.md'
  - '/home/administrator/projects/hexalith/eventstore/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md'
---

<frozen-after-approval reason="authorized owner prerequisite review fixes">

## Intent

Finish the retained-history SDK review by enforcing its existing 30-second deadline even if an admission, custody or transport operation ignores cancellation. Preserve all existing history certificates, scope, retention, source positions and fail-closed behavior. No production policy/provider is approved or installed.

## Constraints

Work only in the sibling EventStore checkout and our retained-history files. Preserve the extensive unrelated staged/unstaged/committed 6.1/6.6 work. No remote, staging, commit, deployment, nested submodules or live calls. This is the already-rendered bmad-build implementation handoff; do not render again. Use documented one-type files and owning Allman/CRLF conventions, Debug, isolated outputs, individual xUnit assemblies and set -e.

## I/O & Edge-Case Matrix

| State | Expected |
| --- | --- |
| Admission/custody never completes and caller cancels | Read promptly throws caller cancellation; no successful/partial history |
| Admission/custody ignores operation deadline | Existing bounded deadline returns content-free unavailable |
| HTTP send/body read ignores caller cancellation | Client promptly stops; no certificate release |
| Noncooperative operation eventually completes after cancellation | No source lookup/loop/release continues; dispose owned transport material safely |
| Ordinary valid and invalid history | Existing 36 passing tests retain their behavior |

</frozen-after-approval>

## Code Map

- src/Hexalith.EventStore.Server/Security/RetainedIdentityHistorySourceReader.cs: actor metadata/page awaits already use WaitAsync(readToken), but admission and custody Task awaits do not. Bound first/final admission, unprotect, first/final CanRead and any other outstanding work using the existing linked token. Keep final clock/validity and terminal token checks.
- src/Hexalith.EventStore.Client/Streams/RetainedIdentityHistoryReader.cs: bound HTTP send, response/body acquisition and each Task/ValueTask body read against the existing deadline, including deliberately noncooperative test transports. Keep 32 MiB wire /16 MiB decoded and 10k position bounds, authenticated host-supplied credentials, certificate validation and content-free failures.
- Existing tests in Client.Tests/Streams/RetainedIdentityHistoryReaderTests.cs and Server.Tests/Security/RetainedIdentityHistorySourceReaderTests.cs plus their owned helpers. Add meaningful never-completing-operation regression fixtures with a short test watchdog and caller cancellation; tests must fail promptly if the bounded wait is removed. Do not spend 30 seconds per test or change the production 30-second bound merely for tests. Reuse the already injected TimeProvider to drive the deadline timer, with an owned manually advanced timer fixture; execute the deadline-unavailable matrix row as well as caller cancellation. Deadline expiry must occur while a provider/transport task remains incomplete.
- All our 16 retained-history files are new/untracked. Do not modify unrelated existing SDK internals, domain replay or tests.
- Source evidence: exact original baseline above plus current observed HEAD 785d58fc99ce4c2751c0546ca6368e993c654ed9 are base observations only. Capture the exact changed file hashes and executed build/test commands/logs/XML; no accepted target or live readiness.

## Tasks & Acceptance

- [x] Enforce bounded outstanding SDK awaits and safe caller cancellation, including transports that ignore tokens.
- [x] Add and execute negative regression cases and all affected focused history tests with warning-free normal builds.
- [x] Produce a separate owner source/evidence note with exact source inventory and all current results; distinguish local fixtures from production provider/cleanup/restore and acceptance.

## Verification

Use /tmp/hexalith-agents54-history-artifacts. Build -c Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0. Reuse unchanged contract evidence (11 passing tests in /tmp/hexalith-agents54-history-contracts-final-results.xml) and rerun changed Client/Server classes only after successful normal builds. New log/XML names should include reviewfix to preserve the prior evidence.


## Root Verification

Reviewed all 23 selected SDK source hashes and 10 artifact hashes, plus executed XML: Client16, Server27 and reused Contracts11 =54 passes with zero failures/skips; normal Debug builds have zero warnings/errors. The full review-fix diff and actual cancellation/deadline cases were inspected. Production retained-history custody/provider/authority is still unavailable.
