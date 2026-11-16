# Deferred state and dependency verification — 2026-11-16

All five selected items are implemented and independently reviewed. The coordinator reran the affected product suites after the review corrections. This report and `verification.json` supersede the implementation handoff manifests; the original handoff and failed attempts remain retained.

| Final check | Result | Evidence directory |
| --- | --- | --- |
| UI Debug/source | 1,352 passed; warning-free build; no failures/skips | `coordinator-final-ui` |
| Shipped browser module | 5 passed, including non-queued Web Lock refusal | `coordinator-final-ui` |
| Custody Debug/source | 806 passed; warning-free build; no failures/skips | `coordinator-final-custody` |
| Current EventStore gateway | 142 source and 142 package cases; both builds and maintained floor pass | `coordinator-final-dependencies/current` |
| Historical five-pin counterfactual | 42 original cases in each exact lane | `coordinator-final-dependencies/historical` |
| Exact Platform tuple | 8 default, 38 mapped catalog and 8 mapped related cases; 29 propagation controls | `coordinator-final-dependencies/platform` |
| Six Release packages and seven isolated consumers | All pass; packaged module equals current source bytes | `coordinator-final-packages` |
| Broad tooling discovery | 333 pass, 2 real-runner cases initially skipped for absent root assembly | `coordinator-final-tooling/tooling-final.log` |
| Both original real-runner cases against a genuinely built disposable Domain assembly | 2 pass; no skips | `coordinator-runner-compatibility` |
| Final runner controls; UI package/consumer controls; existing package controls | 21, 11 and 20 pass respectively | `coordinator-final-tooling` |

The initial parallel Custody run failed one unchanged signing-actor stage-save deadline case while other jobs were active. Its native failure remains in `coordinator-final-state`. The seven signing-actor cases and the complete 806-case suite subsequently passed with native runner parallelism disabled; no assertions, skips or timeouts were changed.

Three independent primary review layers completed. The ten blind findings, two edge findings and the later package fingerprint finding are patched; the verification-gap reviewers found no further gaps. No implementation findings were deferred. Final source hashes include the coordinator's documentation edits, all product/test changes and the two reproduction scripts. Original run bindings remain immutable. The dependency runner's final environment-recording allowlist and the stricter package fingerprint check were added after their full replays: 21 focused runner controls, 11 package/consumer controls, independently inspected installed-SDK vectors and a genuine SDK registration sample cover those final changes. They do not change the successful build/test execution environments or packaged module.

## What the dependency evidence establishes

The selected EventStore gitlink is `eecc0e4da70a0d64aaeff4fb75cdc06f94eb7563`; released 5.1.1 is `f792bba6c0564d3c36dda6ad1c2fc55b0b0a6331`. The complete raw ten-project input inventories are **not identical**: seven project references consume two revisions of the same EventStore `.editorconfig`. The only difference adds LF save rules for configuration, Git attributes and non-C# governance files. C# rules and every other captured compiler, analyzer, additional-file, product, import and package input match. The runner permits only that exact reviewed path/hash pair and records `completeConsumedGraphsEqual: false` and `reviewedConsumedInputsCompatible: true`. Source and package gateway executions independently pass. The separate default Server producer-mapping refusal is retained; it is not counted as a successful source run.

The historical experiment exports Agents `13857fd86e9fda7e2ba0f4017aadb042750e4284` in both lanes, fixes EventStore at `4bc61d9a60fae13a65f23963c1cb731065222f2a`, retains SDK 10.0.401, and varies only the five exact gitlinks from `599208dd40efadef728363c227a0f75ebd888337`. Both 42-case lanes pass. The previous five pins were sufficient for that selected integration; current pins remain unchanged.

Platform evidence uses immutable baseline `17b1f688b332020fafbe1da07a5c04a41886c7bd` plus three explicitly hashed local corrections: Composition's project file, CatalogBuilder's Program and the propagation regression script. The pristine HXW006 and intermediate adapter-inventory refusals remain in earlier evidence directories. The corrections preserve plain-source Tooling catalog neutrality, retain mapped producer authority and compare complete adapter route-ID sets. All 18 selected producer entries and their exact dependency revisions remain intact. The coordinator fetched exact Works `b2985d22b3bfbc6d86bcde28a5da722ae7f930c3` without changing its checkout. No owner source was substituted and no nested submodule was initialized. These are local source compatibility results; live owner qualification remains separate.

## State and package boundaries

Browser mutations use non-waiting Web Locks around an exact-byte compare/set. Conflicts return to fresh C# validation and authentication-generation checks, with bounded retries. Distinct runtimes preserve independent additions, acknowledgements and removals; conflicts, corruption, storage denial, cancellation and authentication transitions refuse safely. The ambiguity of an already delivered synchronous write remains documented.

Historical spool fixtures are synthetic bytes generated from five exact pre-reservation source files at `8a56bd57837784806aae989159fbcbc71fbd41d4`, with source/output provenance in Platform's `Fixtures/pre-reservation/` directory. Their historical bytes and digests survive record copies, restart, pending recovery and mixed archive rollover. Compact legacy reservation preserves the unchanged 32 MiB limit; an empty successor promotes to current encoding. Current defaults, partial/duplicate reservation fields and tampering retain their existing gates. No production inventory was supplied.

The UI package guard enrolls exactly one browser module, four reviewed SDK registration templates and the complete module-bound manifest. It checks route metadata, integrity and the installed Razor SDK's exact signed-prefix/base36 fingerprint algorithm. Missing/extra assets, injected targets and coordinated wrong fingerprints refuse. Consumer receipts retain actual package versions and exact import paths/hashes; only the produced UI package's version in the policy comparison is represented as `$produced`. No packages were published.

## Reproduction

Run from the Agents root, using Python 3.12+ on Linux and the repository's pinned .NET SDKs. Every evidence destination must be fresh. All scratch work belongs under the wrapper's private TMPDIR.

```bash
run-with-temp bash _bmad-output/implementation-artifacts/tests/deferred-state-dependencies-2026-11-16/verify-state.sh
run-with-temp python3 -m unittest discover -s tests/tooling -p deferred_dependency_cohort_test.py -v
run-with-temp bash _bmad-output/implementation-artifacts/tests/deferred-state-dependencies-2026-11-16/verify-packages.sh /absolute/durable/fresh-package-evidence
run-with-temp python3 scripts/verify-deferred-dependency-cohort.py --evidence /absolute/durable/fresh-cohort-evidence
```

For the exact corrected Platform lane, supply the recorded owner object source and three explicit inputs:

```bash
platform_source_root="$PWD/references/Hexalith.Platform"
run-with-temp python3 scripts/verify-deferred-dependency-cohort.py --lane platform \
  --owner-root Hexalith.Works=/home/administrator/projects/hexalith/works \
  --platform-overlay "src/Hexalith.Platform.Composition/Hexalith.Platform.Composition.csproj=$platform_source_root/src/Hexalith.Platform.Composition/Hexalith.Platform.Composition.csproj" \
  --platform-overlay "eng/test_source_sdk_propagation.py=$platform_source_root/eng/test_source_sdk_propagation.py" \
  --platform-overlay "src/Hexalith.Platform.CatalogBuilder/Program.cs=$platform_source_root/src/Hexalith.Platform.CatalogBuilder/Program.cs" \
  --evidence /absolute/durable/fresh-platform-evidence
```

The runner exports exact local Git objects and performs no remote operations. Missing owner objects refuse. Native validation requires exact normalized assembly paths, executed assembly fingerprints, original display/type/method case multisets and exact method identities; failures, skips, errors and zero discovery refuse. Per-configuration evaluated project graphs exclude stale/archived assets. Owned subprocess groups are terminated and reaped on timeout or interruption before scratch cleanup. Recorded environment values use an explicit allowlist.

`compressed-artifacts.json` binds every retained gzip file to its original and compressed SHA-256. References to compressed-away files resolve through that map. `verification.json` binds the final native counts and retained evidence bytes; `source-hashes.json` binds the final implementation/documentation inputs. Earlier manifests are named `*-handoff` or `*-initial` and describe their original runs.

## Delivery

Platform main was fast-forwarded to remote main before Conventional Commit `38d6c81fc7d8058bb93414a96993986e96da1058`. Its tested product inputs were unchanged by the fast-forward. That local commit is unpublished. All other root submodules retain their prior checkouts and have no unpublished commits.

The required `python3 references/Hexalith.Builds/check.py` fails in both repositories because CiCd-owned `.github/hexalith-ci.json` is absent. Baseline and final-commit guard receipts are retained under `coordinator-final-tooling`. CiCd's registry has no Agents or Platform onboarding entry; repairing delivery therefore requires a separate CiCd registration/adoption change. No guard, audit or test was bypassed. No push occurred, and the Agents commit/gitlink update waits for successful child publication, as required by repository policy.

The earlier Aspire baseline observation is retained in `aspire-baseline.json`. It does not establish runtime owner qualification.
