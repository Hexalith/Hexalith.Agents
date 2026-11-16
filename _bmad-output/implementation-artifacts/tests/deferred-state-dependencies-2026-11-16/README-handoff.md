# Deferred state and dependency verification — 2026-11-16

Review corrections changed the browser mutation, compact legacy reservation and
verification guards after the full runs below. Only focused tests were rerun:
5 JavaScript, 19 runner/reproduction controls and 16 spool cases. The coordinator
must complete the final full run and rebind source/evidence manifests after all
patches and metadata edits. See [review handoff](review-fixes-ada6895cd66d/REQUIRES-COORDINATOR-REBINDING.md).

All five selected ledger questions are resolved locally. The final Platform run
uses its exact recorded owner tuple plus three explicitly retained local Platform
corrections. All 18 producer-selected entries remain intact. This is corrected
source evidence, not pristine baseline evidence or live owner qualification. No
gitlink, audit policy or test expectation was weakened. The implementation handoffs
performed no staging, commits, pushes or Git remote operations; the coordinator
retrieved the exact Works object and owns independent review and delivery.

| Check | Result | Durable evidence |
| --- | --- | --- |
| UI Debug/source owning project | 1,352 passed; 0 failed/skipped/not run/errors; warning-free build | `ui-all.xml`, `ui-all.log`, `ui-all-build.log` |
| Pending browser store focused | 60 passed | `browser-store.xml`, `browser-store.log` |
| Actual shipped JS module | 3 passed | `browser-module.log` |
| Custody Debug/source owning project | 803 passed; 0 failed/skipped/not run/errors; warning-free build | `spool-all.xml`, `spool-all.log`, `spool-isolated-build.log` |
| Spool focused native class | 73 passed | `spool-focused.xml`, `spool-focused.log` |
| Runner refusal controls | 6 passed | `runner-tooling.log` |
| Current source/released graph | All 10 consumed projects' product files, evaluated imports and package selections match | `final-dependencies/current/selected-graph.json`, `released-graph.json`, expanded imports |
| Current integration Debug/source and Release/package | Both warning-free; maintained package floor passes | `final-dependencies/current/integration-*.log`, `package-floor.log`, resolved asset/output hashes |
| Current gateway Debug/source and Release/package | 142 passed in each lane | `final-dependencies/current/gateway-*.xml`, logs and `result.json` |
| Historical reviewed vs previous five pins | Both warning-free; 42 native gateway cases pass in each lane | `dependencies/historical/{reviewed,previous-five}/` |
| Exact Platform default source graph | Warning-free build; all 8 original related cases pass | `platform-final/platform/default-source-build.log`, `default-related.xml`, `result.json` |
| Corrected Platform producer-mapped catalog | Warning-free build; 38 catalog cases and all 8 related cases pass | `platform-final-correction/platform/{mapped-source-build.log,mapped-catalog.xml,mapped-related.xml,result.json}` |
| Source propagation controls | All 29 pass, including plain-source neutral Tooling catalog and mapped direct/promoted edges | `platform-final-correction/platform/source-propagation-controls/` |

`verification.json` binds the executed result counts and source/evidence hashes.
Any compressed artifact is lossless; `compressed-artifacts.json` maps its retained
compressed bytes to the original SHA-256 recorded by the experiment. Earlier
attempts remain under `dependencies`, `final-dependencies/platform`,
`spool-initial.*`, `ui-outside-root.log`, and the initial build logs.

The original Platform cases all executed in plain and mapped modes, including
`StaticValidityRequiresExplicitProjectAndScopedConfigurationBeforeComposition`,
`SignedPartiesRunUsesTheRunnerJwtSettingsAsync`, and
`CompositionWiresSingularPartyGatewayAndDeclarativeTenantSubscription`. The final
mapped run executes all 38 Conversations cases, including the actual reader,
adapter/domain refusal controls and real gateway Program registration. It starts
no distributed resources and grants no live qualification.

The coordinator retrieved exact Works
`b2985d22b3bfbc6d86bcde28a5da722ae7f930c3` from
`https://github.com/Hexalith/Hexalith.Works.git`, without changing checkout content.
The earlier missing-object refusal remains in `platform-final`. The pristine
baseline then refused HXW006 because Composition's Builds.Tooling reference
unconditionally removed `HexalithEventStoreVersion`, overriding the producer
mapping. That immutable refusal remains in `platform-complete`.

The correction retains neutral Tooling selection in plain builds and defers to
producer authority for mapped references. Full catalog execution also exposed an
equal-sized foreign adapter inventory: the old count check rejected it later with
a different diagnostic. CatalogBuilder now compares complete route-ID sets before
binding. The existing refusal assertions and artifact-absence checks remain
unchanged. The intermediate failure remains in `platform-corrected`.

`platform-final-correction/platform/result.json` marks `pristineBaseline:false`
and binds the original/corrected SHA-256 plus retained bytes for exactly three
inputs: Composition's project file, CatalogBuilder's Program, and the propagation
regression script. All dependency revisions, package authorities and producer
roots remain unchanged. The final source controls pass 29/29 and the native
selections pass 8 default, 38 catalog, and 8 mapped-related cases with no failures,
skips, not-run results or errors.

The EventStore current/released comparison uses gitlink
`eecc0e4da70a0d64aaeff4fb75cdc06f94eb7563` and released 5.1.1 commit
`f792bba6c0564d3c36dda6ad1c2fc55b0b0a6331`. The default Server source build still
refuses the missing producer mapping, retained separately in
`server-default-source.log`; successful producer-selected mapped source execution
and package gateway execution independently establish the integration result.
Version pins and Builds catalog authority remain unchanged.

The historical experiment exports Agents
`13857fd86e9fda7e2ba0f4017aadb042750e4284` in both lanes, fixes EventStore at
`4bc61d9a60fae13a65f23963c1cb731065222f2a`, preserves SDK 10.0.401 and its original
catalog, and varies only the five exact gitlinks from Agents
`599208dd40efadef728363c227a0f75ebd888337` versus the reviewed snapshot. All 42
original gateway cases pass in both lanes. The previous pins were sufficient for
that integration; this is no reason to roll back today's unrelated changes.

## Reproduction

From the Agents root:

```bash
run-with-temp bash _bmad-output/implementation-artifacts/tests/deferred-state-dependencies-2026-11-16/verify-state.sh
run-with-temp python3 -m unittest discover -s tests/tooling -p deferred_dependency_cohort_test.py -v
run-with-temp python3 scripts/verify-deferred-dependency-cohort.py --evidence /absolute/durable/fresh-cohort-evidence
```

Use `--lane current`, `--lane historical`, or `--lane platform` for a single
experiment. Supply additional exact local repositories with repeated
`--owner-root Hexalith.Works=/absolute/path` arguments. Missing objects refuse
before the producer can initialize them. An unmodified archive replay retains the
baseline HXW006 refusal. Reproduce the corrected Platform run with explicit local
inputs:

```bash
platform_source_root="$PWD/references/Hexalith.Platform"
run-with-temp python3 scripts/verify-deferred-dependency-cohort.py --lane platform \
  --owner-root Hexalith.Works=/home/administrator/projects/hexalith/works \
  --platform-overlay "src/Hexalith.Platform.Composition/Hexalith.Platform.Composition.csproj=$platform_source_root/src/Hexalith.Platform.Composition/Hexalith.Platform.Composition.csproj" \
  --platform-overlay "eng/test_source_sdk_propagation.py=$platform_source_root/eng/test_source_sdk_propagation.py" \
  --platform-overlay "src/Hexalith.Platform.CatalogBuilder/Program.cs=$platform_source_root/src/Hexalith.Platform.CatalogBuilder/Program.cs" \
  --evidence /absolute/durable/fresh-platform-evidence
```

The executed arguments are retained in
`platform-final-correction/invocation.json`. The runner rejects wrong assemblies,
errors, failures, skips, not-run results, zero/insufficient discovery, and missing
original methods. It retains every producer-selected environment entry and
mapping root, together with explicit overlay provenance. It performs no Git
remote operations or package publication.

The state script copies sources outside the Agents parent directory before
building Platform, preventing Agents `.editorconfig` rules from contaminating
Platform tests. UI uses normal output paths inside a disposable repository copy
because its source-floor test locates the workspace from the native assembly.
No analyzers, audits, warnings or tests are disabled. All child jobs completed
before the wrapper removed its workspace. Original failed harness attempts are
retained separately.

Platform's historical fixtures are synthetic bytes generated from five exact
pre-reservation source files at
`8a56bd57837784806aae989159fbcbc71fbd41d4`, not production inventory. Their
source/output provenance and generator live in
`references/Hexalith.Platform/tests/Hexalith.Platform.Custody.Tests/Fixtures/pre-reservation/`.
The empty historical hash is
`C63A578DF54A5C6F8918B9093660EE01CB6F0648341F8EFD4F4F158AF21F4137`.
The converter retains layout identity across record copies and generic recovery;
new snapshots remain current. Legacy heads retain compact encoding through reservation; the empty successor
promotes to current encoding. Older archive pages retain original bytes and links.

## Matrix coverage

| Approved scenario | Executed tests |
| --- | --- |
| Overlapping stores | `DistinctRuntimesRetryValidatedStateWithoutLosingIndependentLocks`, four addition/acknowledgement/removal/conflict cases; actual module's exact-byte conditional mutation cases |
| Interrupted browser work | Existing suspended read/delivered-write authentication cases; `ConflictedMutationRevalidatesCancellationAuthenticationAndCorruption`; read/write denial and corruption regressions; actual module coordination/storage refusal |
| Retained spool | `HistoricalSnapshotBytesSurviveRestartDrainAndRecordCopies`; historical pending recovery and interrupted mixed-chain rollover tests; existing reservation crash controls |
| Invalid spool | `MalformedReservationLayoutsRefuseWithoutMutation`; historical pending and archive tamper refusal; existing anchored-state negatives |
| Dependency comparisons | Matching 10-project current/released graph and both gateway lanes; historical five-pin counterfactual; exact default/mapped Platform eight-case reruns and all 38 producer-mapped catalog cases; pristine and intermediate refusals retained separately |

## Baseline observation

The exact Platform AppHost was started with the Aspire orchestration skill,
inspected via `aspire describe --apphost <absolute Platform/apphost.cs>`, then
stopped successfully with the same exact target. Infrastructure containers were
healthy; owner services were unhealthy or waiting. This is a baseline observation
only and does not qualify runtime owners. `aspire-baseline.json` records the
commands and observed states without dashboard credentials.
