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
