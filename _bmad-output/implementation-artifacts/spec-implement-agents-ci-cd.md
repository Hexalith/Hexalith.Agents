---
title: 'Implement Agents CI/CD and NuGet Publication Verification'
type: 'feature'
created: '2026-09-18'
status: 'done'
route: 'dispatch'
review_loop_iteration: 0
baseline_commit: 'c29cd1ddfa8903cee2b224bd64be25aa0e94cad7'
context:
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - 'references/Hexalith.Builds/.github/workflows/ci-cd-standards.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Agents validates Release/package mode locally but does not use the ecosystem's shared CI operating model and has no semantic-release, protected release workflow, commit/security callers, or public packages. NuGet currently returns 404 for all six manifest IDs.

**Approach:** Adopt the common Tenants/EventStore/FrontComposer pattern: thin Hexalith.Builds callers, module-specific policy gates, manual exact-source release through a protected environment, manifest-driven pack/validate/publish, and live verification that every declared package reached NuGet.

## Boundaries & Constraints

**Always:** Preserve the exact six-package inventory, Release package-reference mode, import-only Builds package authority, per-project Microsoft Testing Platform tests, root-declared-only submodule initialization, collision-failing publication, explicit secrets, least practical permissions, and unrelated user changes. Pin publication to one reviewed full Builds SHA and pass that identical SHA as `builds-execution-sha`; use `domain-ci.yml@main` for ordinary non-governed CI per shared policy.

**Never:** Initialize nested submodules; publish containers; use `--skip-duplicate`, mutable release refs, `secrets: inherit`, solution-level tests, local dependency versions, or automatic publication on push. Do not push, dispatch a release, alter GitHub environment/secrets/variables, or claim unpublished packages are published.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|---------------------------|----------------|
| CI | Push/PR to `main` | Shared Release build, six-package consumer validation, five project test shards, package-authority/floor/tooling gates | Any failed gate blocks CI and retains test evidence |
| Valid release | Manual dispatch at current green `main`; publication enabled; version absent | Protected release publishes exactly six packages and a GitHub Release, then verifies exact source and all NuGet versions | No duplicate skipping; any mismatch fails |
| Frozen release | `HEXALITH_RELEASE_PUBLISH_ENABLED` is not exactly `true` | Build may run, publication and post-publication assertions skip successfully | Emit an explicit freeze notice |
| Invalid source/collision | Stale/non-main/red source or any target version already exists | Stop before protected publication or before the first NuGet push | Actionable source/package diagnostic |

</frozen-after-approval>

## Code Map

- `.github/workflows/ci.yml` -- replace duplicated build/test jobs with shared `domain-ci`; retain Agents-only authority, EventStore-floor, and tooling checks.
- `.github/workflows/{release,commitlint,codeql,dependency-review}.yml`, `.github/dependabot.yml` -- add the common manually gated delivery and security surface.
- `.releaserc.json`, `package.json`, `package-lock.json`, `commitlint.config.mjs` -- locked Conventional Commit and semantic-release toolchain; no changelog/git release commits.
- `eng/release-packages.json`, `scripts/{pack-release-packages,validate-nuget-packages,validate-consumer-package-references}.py` -- reuse the authoritative six-package inventory and existing package gates.
- `scripts/verify-nuget-publication.py` -- new fail-closed absence/presence probe used before and after publication, with bounded retries for NuGet indexing.
- `Directory.Build.props` -- keep NuGet audit enabled while preventing NU1901-NU1904 from becoming compiler errors.
- `test/Hexalith.Agents.Server.Tests/{PackageInventoryTests,BuildContractConformanceTests}.cs` and `tests/tooling/` -- lock workflow, release, audit, and registry-probe contracts.
- `references/Hexalith.{Builds,Tenants,EventStore,FrontComposer}` -- evidence only; do not modify or copy repository-specific containers, governance handoffs, coverage targets, or package rules.

## Tasks & Acceptance

**Execution:**
- [x] CI/security workflow files -- adopt shared callers and Agents-specific supplemental gates.
- [x] Release/toolchain files -- add locked semantic-release, exact-green-main preflight, protected NuGet-only release, freeze handling, and exact-source post-verification.
- [x] Publication probe and tests -- validate manifest identities and exact NuGet version absence/presence, including 404, malformed responses, partial publication, and retry exhaustion.
- [x] Build/package tests -- assert the six-package count, immutable Builds release pin, no duplicate skipping, explicit secret scope, and NuGet audit policy.
- [x] Verification -- run workflow lint, npm integrity checks, focused tooling/.NET tests, Release build, pack validation, isolated consumer validation, and live registry checks.

**Acceptance Criteria:**
- Given a push or PR, when CI runs, then shared package-mode build/tests and all Agents-specific gates execute without duplicated solution-level test jobs.
- Given an authorized manual release, when semantic-release selects a new version, then exactly the manifest's six packages are validated, published without duplicate suppression, attached to a GitHub Release, and confirmed on NuGet for the dispatched SHA.
- Given today's registry state, when publication status is checked, then all six IDs are accurately reported unpublished; no release is simulated or claimed.
- Given malformed, stale, frozen, missing-secret, inventory-drift, or collision state, when the relevant gate runs, then it fails or skips exactly as the matrix specifies before unauthorized side effects.

## Implementation Notes

- Publication is pinned to the root-reviewed Hexalith.Builds gitlink commit `cb91511794c8898b738d85dc6c751f82b832cbc9`; ordinary CI uses `domain-ci.yml@main`.
- The NuGet verifier follows compressed registration leaves to their NuGet catalog entry and treats only an exact HTTP 404 as proof of absence.
- No workflow was dispatched and no package, tag, release, environment, secret, or repository variable was changed.

## Spec Change Log

- 2026-09-18: Implemented the CI, security, release, package-publication, audit, and regression-test surface.
- 2026-09-18: Applied review fixes for frozen/no-op gating, write-boundary source proof, release metadata verification, transient registry handling, timeout bounds, CodeQL coverage, and Dependabot commit compatibility.

## Review Triage Log

| ID | Verdict / route | Evidence |
|---|---|---|
| B1 | medium / patch | Dependabot emits `chore(deps)` for NuGet and npm, while the new commitlint type list rejects `chore`; routine dependency PRs would fail their required gate. |
| B2 | low / patch | CodeQL is limited to C# even though `eng/semantic-release-plan.mjs` is executable release code; adding the supported JavaScript/TypeScript language is a direct correction. |
| B3 | medium / patch | The caller detects a freeze before planning, but still runs the fallible plan and protected reusable job; this can fail or request approval instead of the frozen run skipping green. |
| B4 | low / rejected | The planner and `.releaserc.json` currently use the same branch, tag format, analyzer, and notes plugins. Divergence is only a future maintenance risk, and eliminating it would add disproportionate release-tool indirection. |
| B5 | low / rejected | A tag-set mutation between planning and release can make post-verification fail against the planned version, but it cannot produce the claimed green unverified release; fixing this rare external race requires a wider authority contract. |
| B6 | medium / rejected (intent-excluded) | A mid-batch NuGet failure can leave immutable partial publication, but the frozen intent explicitly requires collision failure and forbids duplicate suppression; this run must fail red rather than silently resume across an existing version. |
| B7 | false / rejected | The required exact-source CI runs `ReleasePackageManifestShouldRemainExact`, which pins all six IDs, and release preflight requires that green CI SHA; a mutable manifest cannot silently redefine the count on an admitted release. |
| B8 | false / rejected | The pinned reusable workflow contains a skipped governed job with `id-token` and attestation permissions; GitHub validates the called workflow's maximum permissions, so the caller must grant them even when legacy mode runs. |
| B9 | medium / patch | Presence verification fails immediately on transient 429/5xx/transport faults, so a fully published release can fail before confirmation and become difficult to reconcile. |
| B10 | low / patch | Presence retries re-probe confirmed packages despite saying they retry missing inventory, increasing latency and rate-limit exposure; retaining only missing IDs is a direct correction. |
| B11 | false / rejected | Semantic Release uploads and pushes the same freshly packed local `nupkgs` set, while the post-check proves its exact names and source tag; the alleged independent wrong-byte substitution has no demonstrated path in this workflow. |
| B12 | false / rejected | The loose workflow regex consumes only Semantic Release's stable-main output, which is a three-part stable version; the malformed and combined suffix examples cannot be emitted by the configured planner. |
| B13 | low / patch | Python's default JSON decoder accepts duplicate manifest keys, contrary to the fail-closed inventory requirement; strict duplicate-key rejection is local and testable. |
| V1 | medium / patch | The verification-gap review found no executable planner tests; a false-negative planner would skip the post-publication job while the reusable release still ran. |
| V2 | high / patch | Exact-green-main admission is asserted only by YAML substrings, and the legacy reusable path does not re-prove successful caller CI for this NuGet-only publisher; executable fixtures are needed. |
| V3 | medium / patch | No test proves non-404 HTTP failures stay fail-closed during collision checks; a regression could reinterpret a registry error as absence. |
| V4 | medium / patch | The GitHub Release tag/draft/exact-asset comparison has no executable fixture coverage, so missing assets could regress without any current test failing. |
| V5 | medium / patch | Direct execution of the configured commitlint confirms that Dependabot's `chore(deps)` prefix is rejected. |
| E1 | medium / patch | Same root cause as B1/V5: the generated Dependabot prefixes and commitlint allow-list disagree. |
| E2 | medium / patch | `True == 1` and `1.0 == 1` in Python, so malformed boolean/float schema versions pass the current equality check and violate fail-closed manifest handling. |
| E3 | low / rejected | NaN/infinite CLI timing values can yield an uncaught runtime error, but production uses fixed defaults and the edge is not encountered in normal use; extra timing-policy guards are not justified here. |
| E4 | low / rejected | Numeric prerelease leading zeros are accepted by the generic verifier, but the production main-branch Semantic Release emits stable versions only; the invalid coordinate is unreachable in this workflow. |
| E5 | high / patch | The reusable workflow rechecks current main before Semantic Release, but Agents spends additional time packing before its first NuGet write and performs no boundary recheck; main can advance during that gap. |
| E6 | maybe-false / defer | A competing credential holder might publish the same version between the absence proof and batch push, but no competing publisher is evidenced. Confirming the risk requires an inventory of every principal able to publish these six IDs or a NuGet reservation capability. |
| E7 | medium / rejected (intent-excluded) | Same immutable partial-publication risk as B6; the approved collision-failing/no-skip policy deliberately favors a loud red release over automatic reconciliation of an occupied version. |
| E8 | medium / patch | Twelve attempts across six packages and possible registration-plus-catalog requests can exceed the job's 10-minute timeout, bypassing the verifier's bounded retry diagnostic. |

## Verification

**Commands:**
- `actionlint -no-color` -- all workflows valid.
- `npm ci && npm audit signatures` -- locked release dependencies install and verify.
- `python3 -m unittest discover -s tests/tooling -p '*_test.py'` -- tooling contracts pass.
- `dotnet build Hexalith.Agents.slnx -c Release -p:UseHexalithProjectReferences=false /m:1 /nr:false` -- zero errors/warnings.
- Per-project `dotnet test` for all five test projects in Release/package mode -- all pass.
- `python3 scripts/pack-release-packages.py nupkgs 0.0.0-ci && python3 scripts/validate-nuget-packages.py nupkgs && python3 scripts/validate-consumer-package-references.py nupkgs` -- exact inventory and isolated consumer pass.
- `python3 scripts/verify-nuget-publication.py eng/release-packages.json 0.0.0-ci --expect absent` -- all six live destinations are absent.

**Results:**
- `actionlint -no-color` passed for every workflow.
- `npm ci && npm audit signatures` passed; 504 package signatures and 127 attestations verified.
- Tooling discovery passed 86 tests, including release/no-release planning, source-proof, GitHub Release, commit-policy, registry 404, malformed-response, partial-publication, collision, transient-failure, and retry-exhaustion cases.
- Release/package-mode solution build passed with zero warnings and zero errors.
- All five project test shards passed: Contracts 529, Client 6, Domain 787, Server 546, and UI 1073 tests (2,941 total).
- The exact six-package pack, metadata validation, and isolated-consumer build passed at `0.0.0-ci`.
- Live NuGet verification reported all six `0.0.0-ci` versions absent; no publication was attempted.
