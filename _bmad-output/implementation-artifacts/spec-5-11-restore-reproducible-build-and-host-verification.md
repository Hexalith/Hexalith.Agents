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

- [x] Initialize the 13 candidate gitlinks from empty module caches and compare each checkout to the parent index. All 13 matched and all nested worktrees remained uninitialized. With the same pin set and the Builds-generated source mapping, the Debug solution built with zero warnings and errors, and all 37 Conversations adapter tests passed against sibling source.
- [x] Verify a Release build and all five test projects in the working tree. All 6,581 tests passed. Process tests exercised credential enforcement and the typed unavailable response. Router tests compared the versioned and legacy inventories.
- [x] Run the package-consumer hook for each shipped library and for their combined installation. All seven selections matched the reviewed executable asset identities, hashes, paths and versions.
- [x] Run the tooling suite and investigate failures and skips. Python discovery during concurrent builds hit subprocess timeouts. All affected methods and the setup-blocked fixture passed on repeat runs, with the last 93-case identity matrix passing under its original timeout in isolation. All 51 previously skipped owner and Debug runner cases passed against the restored sources. The JavaScript storage tests, release planner and commit lint also passed.
- [ ] Have the CiCd owner deliver the matching build/release workflow. Require a green hosted run and a nonpublishing release rehearsal at the same parent revision.

## CiCd handoff

The delivered CI guard expects Builds `264e17d3` while the module records `6b6eb4ee`. Release preparation also names the old revision. CI invokes the retired `eng/verify-story.ps1` and `eng/verify-story-5.2.ps1`. The CiCd owner should add the module to its setup inventory and deliver a workflow using one reviewed build-tools revision throughout. Preserve the package-authority check, EventStore floor, package-consumer hook and credential checks. A hosted run with successful checkout, build, tests and consumer verification, followed by a nonpublishing release rehearsal, closes this part of the story. [Current CI](../../.github/workflows/ci.yml) · [Current release workflow](../../.github/workflows/release.yml) · [Maintained package hook](../../scripts/check-built-packages.py)

The Platform integration remains a separate Story 5.6 dependency. The four committed external qualification records retain their exact owner targets. [Acceptance criteria](../planning-artifacts/epics.md) · [Course correction and ownership](../planning-artifacts/sprint-change-proposal-2026-11-20.md)
