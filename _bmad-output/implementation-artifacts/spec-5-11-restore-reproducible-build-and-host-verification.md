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
- [ ] Record a nonpublishing release rehearsal at that same parent revision. Historical for `2b21256`: the November 21 inventory contains only the successful CI run. The current candidate is `0d98305` below. [Workflow runs for this revision](https://api.github.com/repos/Hexalith/Hexalith.Agents/actions/runs?head_sha=2b212567fc546cef1d194c9ff778b082fc80fccb)

## November 22 verification

The refreshed parent is `412528ed47dfa9bd33453784e70aaedc6dc2214d`. Evidence for this candidate can be reproduced from a clean checkout. Keep the November 20 and 21 results above with their original revisions.

| Check at `412528e` | Evidence or remaining work |
| --- | --- |
| Fetch recorded sources | Passed. An empty Git repository fetched the parent by SHA. A shallow, nonrecursive submodule update from the declared remotes checked out all 13 root gitlinks at the exact index revisions. There were zero initialized nested worktrees. |
| Hosted Release CI | Passed on November 21. The build-and-test job completed the Release build, unit tests, after-unit-tests hook and integration stage. [CI job 136901781255](https://github.com/Hexalith/Hexalith.Agents/actions/runs/44922636989/job/136901781255) |
| Debug/source qualification | Passed with SDK 11.0.100 and the released `Hexalith.Builds.Module.Cli` 6.4.4. The solution built with zero warnings and errors. All 6,667 tests passed across five projects with zero skips, including 37 Conversations source-adapter cases verified separately. The earlier contributor procedure built a development CLI that failed producer identity check `HXW009`. [Source verification receipt](tests/story-5-11-2026-11-22/source-verification.json) · [Build procedure](../../CONTRIBUTING.md#build-and-test) |
| Nonpublishing rehearsal | Historical for `412528e`: the exact-SHA inventory was still only CI. The current candidate is `0d98305` below. [Candidate runs](https://api.github.com/repos/Hexalith/Hexalith.Agents/actions/runs?head_sha=412528ed47dfa9bd33453784e70aaedc6dc2214d) · [Release caller](../../.github/workflows/release.yml) |

## November 22 CiCd rehearsal investigation

The current parent is `0d98305f91899a088ddad0b0a23a596453913274`. Keep the November 20–22 results above with their original revisions. This section does not close the rehearsal checkbox.

| Check at `0d98305` | Evidence or remaining work |
| --- | --- |
| Hosted Release CI | Passed. `ci / Build and tests` completed successfully on the push of this SHA. Builds pin in the delivered workflows is `v6.4.4`. [Hosted CI job 137152331716](https://github.com/Hexalith/Hexalith.Agents/actions/runs/45004296055/job/137152331716) |
| Exact-SHA run inventory | One run: CI `45004296055` (`success`). No `release.yml` run exists for this SHA. [Workflow runs for this revision](https://api.github.com/repos/Hexalith/Hexalith.Agents/actions/runs?head_sha=0d98305f91899a088ddad0b0a23a596453913274) |
| Nonpublishing rehearsal | Deferred. CiCd `builds-version` is `v6.6.0` (`d532b1e9b1f595e2c9867bfa9780dccd78c9a2d9`) and the rendered Agents caller already pins `release.yml@v6.6.0`, but Agents `main` still dispatches `v6.4.4`. Neither tag accepts a rehearsal input. `HEXALITH_RELEASE_PUBLISH_ENABLED=false` skips version, build and draft (`outcome: disabled`) and cannot rehearse. The live variable API is 403; Agents published immutable `v2.0.3` on 2026-11-21, which required the switch `true`. Dispatching today's caller with that switch would run create-mode: it can cut a new stable version, create a marked draft and publish. The 2026-11-18 maintainer decision still defers a no-publish rehearsal because it needs a Builds release and redelivery to every module. |

**Rehearsal receipt (still missing).** Record these four fields when a supported rehearsal exists and has run. Until then Story 5.11 stays in progress.

| Field | Value at `0d98305` |
| --- | --- |
| Agents SHA | `0d98305f91899a088ddad0b0a23a596453913274` |
| Builds version | `v6.4.4` on this revision (`v6.6.0` is the current CiCd pin, not yet delivered here) |
| Run URL | none — no supported rehearsal run |
| Result | not run; mode still deferred |

**Smallest Builds and CiCd change.** Add optional `rehearsal` boolean inputs, default `false`, on Builds shared `release.yml` (`workflow_call`) and `templates/.github/workflows/release.yml.j2` (`workflow_dispatch`), and pass `with: rehearsal`. When rehearsal is true and the kill switch is `true`, keep `enabled` true so version and the audited build run, skip the draft job, add `draft` to `intentional_skips` in `Github/lib/release_outcome.py`, force `publish=false`, and emit `rehearsed`. Extend `schemas/hexalith.release-outcome.v1.json`. A skipped draft with today's reducer is `failed` ("unexpectedly skipped"). Nightly D2 (`tools/nightly_acceptance.py`) must reject `rehearsed` explicitly; adding it to `OUTCOMES` without a branch would pass like `published`/`nothing`. Leave the 90-day Actions `release` artifact as build proof, not nuget.org or GitHub publication. Nightly must keep sending only `{delivered-sha}` so the new input stays omitted. `concurrency: group: release` serializes a rehearsal with a real release. AD-17 CodeQL and the nightly hook still run. Do not dispatch the current Agents caller: it still pins `v6.4.4` and only declares `delivered-sha`, so an extra input is dropped and create-mode would publish. Ship a Builds tag after `v6.6.0`, pin it in CiCd, and deliver the caller to every module as the 2026-11-18 deferral required.

**Maintainer decision required to proceed.** Un-defer the 2026-11-18 no-publish rehearsal. Approve the input, the `rehearsed` outcome and D2 reject, the Actions artifact as build proof, fleet redelivery of owned `release.yml`, and that the maintainer or an interactive CiCd session dispatches it (module agents cannot). An Agents-only delivery would be a new exception to that deferral. Do not dispatch Agents `release.yml` and do not set the kill switch to `false` as a substitute.

## CiCd handoff

The November 20 handoff recorded a mismatched Builds revision and obsolete verification scripts. The delivered workflows now use the same Builds tag, and the hosted after-unit-tests hook runs the maintained package-consumer check. CI uses Release/package mode. The November 21 fetch failures belong to `2b21256`. At `412528e` an empty-cache checkout fetched all 13 pins and Debug/source qualification passed. The remaining hosted gate is the nonpublishing rehearsal at `0d98305`. [Current CI](../../.github/workflows/ci.yml) · [Current release workflow](../../.github/workflows/release.yml) · [Package-consumer hook](../../.github/actions/after-unit-tests/action.yml)

The Platform integration remains a separate Story 5.6 dependency. The four committed external qualification records retain their exact owner targets. [Acceptance criteria](../planning-artifacts/epics.md) · [Course correction and ownership](../planning-artifacts/sprint-change-proposal-2026-11-20.md)
