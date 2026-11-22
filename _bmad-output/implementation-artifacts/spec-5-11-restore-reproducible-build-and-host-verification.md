---
title: 'Story 5.11: Restore Reproducible Build and Host Verification'
type: 'chore'
created: '2026-11-20'
status: 'in-progress'
baseline_commit: 'e4b48774833a13be7ba8748e86da9a07f78e916d'
---

# Restore reproducible build and host verification

A contributor must be able to reproduce the supported source and package builds, and a release operator must be able to start the actual server with its documented credentials. At the audited revision, package tests passed even though the executable failed to start. A fresh source checkout also failed before compilation.

## Qualification checklist

The first four checks record local results from the November 20 correction at Agents `05ddee8`. The hosted results below use parent `2b21256`.

- [x] Initialize the 13 candidate gitlinks from empty module caches and compare each checkout to the parent index. All 13 matched and all nested worktrees remained uninitialized. With the same pin set and the Builds-generated source mapping, the Debug solution built with zero warnings and errors, and all 37 Conversations adapter tests passed against sibling source.
- [x] Verify a Release build and all five test projects in the working tree. All 6,581 tests passed. Process tests exercised credential enforcement and the typed unavailable response. Router tests compared the versioned and legacy inventories.
- [x] Run the package-consumer hook for each shipped library and for their combined installation. All seven selections matched the reviewed executable asset identities, hashes, paths and versions.
- [x] Run the tooling suite and investigate failures and skips. Python discovery during concurrent builds hit subprocess timeouts. All affected methods and the setup-blocked fixture passed on repeat runs, with the last 93-case identity matrix passing under its original timeout in isolation. All 51 previously skipped owner and Debug runner cases passed against the restored sources. The JavaScript storage tests, release planner and commit lint also passed.
- [x] Verify that the delivered CI and release workflows both use Hexalith.Builds `v6.4.4`. The hosted build and test job passed on parent `2b212567fc546cef1d194c9ff778b082fc80fccb`, including Release build, unit tests and the package-consumer hook. [Hosted CI job](https://github.com/Hexalith/Hexalith.Agents/actions/runs/44899631836/job/136832612608)
- [ ] Record a nonpublishing release rehearsal at that same parent revision. The November 21 exact-revision workflow inventory contains only the successful CI run. [Workflow runs for this revision](https://api.github.com/repos/Hexalith/Hexalith.Agents/actions/runs?head_sha=2b212567fc546cef1d194c9ff778b082fc80fccb)

## November 22 verification

The refreshed parent is `412528ed47dfa9bd33453784e70aaedc6dc2214d`. Evidence for this candidate can be reproduced from a clean checkout. Keep the November 20 and 21 results above with their original revisions.

| Check at `412528e` | Evidence or remaining work |
| --- | --- |
| Fetch recorded sources | Passed. An empty Git repository fetched the parent by SHA. A shallow, nonrecursive submodule update from the declared remotes checked out all 13 root gitlinks at the exact index revisions. There were zero initialized nested worktrees. |
| Hosted Release CI | Passed on November 21. The build-and-test job completed the Release build, unit tests, after-unit-tests hook and integration stage. [CI job 136901781255](https://github.com/Hexalith/Hexalith.Agents/actions/runs/44922636989/job/136901781255) |
| Debug/source qualification | Passed with SDK 11.0.100 and the released `Hexalith.Builds.Module.Cli` 6.4.4. The solution built with zero warnings and errors. All 6,667 tests passed across five projects with zero skips, including 37 Conversations source-adapter cases verified separately. The earlier contributor procedure built a development CLI that failed producer identity check `HXW009`. [Source verification receipt](tests/story-5-11-2026-11-22/source-verification.json) · [Build procedure](../../CONTRIBUTING.md#build-and-test) |
| Nonpublishing rehearsal | Open. On November 22 the exact-SHA run inventories for `412528e` and `2b21256` each returned one successful CI run. The delivered release caller accepts `delivered-sha` and can hand off to a publisher. The CiCd owner must provide a supported nonpublishing path and a receipt identifying the rehearsed revision, Builds version and result. Pair the receipt with green CI for that same revision. [Candidate runs](https://api.github.com/repos/Hexalith/Hexalith.Agents/actions/runs?head_sha=412528ed47dfa9bd33453784e70aaedc6dc2214d) · [Release caller](../../.github/workflows/release.yml) |

## CiCd handoff

The November 20 handoff recorded a mismatched Builds revision and obsolete verification scripts. The delivered workflows now use the same Builds tag, and the hosted after-unit-tests hook runs the maintained package-consumer check. CI uses Release/package mode. On November 21, ten of the thirteen direct source commits recorded by `2b21256` could not be fetched by SHA. Reproducing the Debug/source checkout at this revision remains open alongside the release rehearsal. [Current CI](../../.github/workflows/ci.yml) · [Current release workflow](../../.github/workflows/release.yml) · [Package-consumer hook](../../.github/actions/after-unit-tests/action.yml)

The Platform integration remains a separate Story 5.6 dependency. The four committed external qualification records retain their exact owner targets. [Acceptance criteria](../planning-artifacts/epics.md) · [Course correction and ownership](../planning-artifacts/sprint-change-proposal-2026-11-20.md)
