---
title: '5.4 Parties cancellation review fixes'
type: 'feature'
created: '2026-10-06'
status: 'in-progress'
route: 'dispatch'
human_approval: 'accepted'
baseline_commit: 'b3794a4dcbe2fff3e9ea5c420a3c4695e2d1a8ca'
review_loop_iteration: 0
context:
  - '/home/administrator/projects/hexalith/parties/AGENTS.md'
  - '/home/administrator/projects/hexalith/parties/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-owner-prerequisites.md'
---
<frozen-after-approval reason="authorized prerequisite review fixes">

## Intent

Ensure current and retained historical Parties identity queries respect caller cancellation even when injected readers/custody/authority return normally after cancelling the caller. Preserve original source sequences, Branch B Organization, historical expiry and authority invariants. No production policy/provider or complete P-01–P-10 acceptance exists.

## Constraints

Edit only our query service, its identity tests and owner evidence in sibling Parties. Preserve six pre-existing modified documents and concurrent edits. No staging, commits, remote calls, deployments, nested submodules or analyzer suppression. This is the already-rendered bmad-build handoff; do not render again. One documented type per file, owning Allman/CRLF conventions; Debug isolated source builds. Do not fix or weaken the unrelated existing json-redacted strict replay compatibility test.

## I/O & Edge-Case Matrix

| Input | Expected |
| --- | --- |
| Pre-cancelled current/historical query | Caller cancellation before authority/source lookup |
| Noncooperative current/history reader returns after cancellation | Cancellation propagated, no usable result |
| Custody cancels caller then returns true | Cancellation propagated for both current and historical queries |
| Synchronous terminal authority cancels before valid result | Cancellation propagated, no result released |
| Valid/denied/expired/mismatched ordinary query | All previous focused behavior preserved |

</frozen-after-approval>

## Code Map

- src/Hexalith.Parties/Queries/PartyIdentityQueryService.cs: ResolveAsync and ResolveAtAsync lack entry, post-await and terminal token checks. Add checks before admission, after each awaited external operation, after synchronous final authority and certificate work, and before every result where cancellation may have occurred. Bound Task/ValueTask awaits against the caller token if injected providers ignore it. Caller cancellation must not become Unavailable through generic catch. No new arbitrary timeout/profile.
- tests/Hexalith.Parties.Tests/Queries: extend our identity/history tests with meaningful pre-cancelled and noncooperative reader/custody/authority cases. Use a short watchdog for never-completing fixtures, not blocking sleeps. Test cancellation source belongs to the test and is passed explicitly.
- _bmad-output/implementation-artifacts/ext-parties-1-owner-prerequisites-2026-10-06.md and tests/ext-parties-1-owner-source-evidence-2026-10-06.json: refresh exact query/test hashes, current observed base head, commands/logs/XML and focused totals; preserve existing blockers and immutable initial baseline.
- Previous focused total 59 = 38 domain/admission + 18 client + 3 contract tests. Unchanged Client/Contract evidence may be reused explicitly. Full Local stopped at 489/490 with unchanged PartyDomainProcessorValidationTests.ProcessAsync_ProtectedHistoricalPayloadWithDestroyedKey_RedactsAndContinuesRehydration; keep it reported.

## Tasks & Acceptance

- [x] Enforce entry, bounded outstanding waits, post-await and terminal caller cancellation.
- [x] Run all affected identity query tests and meaningful regressions with normal warning-free builds.
- [x] Refresh exact source/evidence inventory and report full qualification/policy/provider/cleanup/restore gaps.

## Verification

Artifacts /tmp/hexalith-agents54-parties-artifacts, optional Memories pin /tmp/parties-no-memories-source to avoid missing nested McpCli; no nested init. Build -c Debug -m:1 -p:NuGetAudit=false -p:MinVerVersionOverride=1.0.0, with existing source flags. Execute built xUnit assemblies with single-dash class filters after successful builds. Preserve prior logs; use reviewfix names.


## Implementation Notes

Direct implementation because a fresh subagent thread was unavailable. Reviewed both entry points and executed the normal Debug source build (0 warnings/errors) and 52/52 domain/admission tests, including 14 new cancellation cases. Client 18 and Contracts 3 results are explicitly reused, yielding 73 focused tests. Owner evidence preserves immutable baseline and the broad 489/490 failure. Full parent scope and external acceptance remain incomplete.
