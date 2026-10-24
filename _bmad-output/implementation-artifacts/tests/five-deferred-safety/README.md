# Five deferred contract and release safety items

## Final verification

The final reviewed implementation passes the maintained Story 5.2 source gate: **5,766 tests** across Contracts (1,997), Client (7), Domain (1,017), Server (1,572) and UI (1,173), plus focused and composition checks. All builds and native outcomes are warning-free, failure-free and skip-free. The complete tooling suite passes **266 tests** in 1,113.969 seconds.

Fresh Release/package-mode packing produces the exact six packages. All seven isolated SDK-pinned consumers pass the independently reviewed executable asset policy: 30 selected assets (12 analyzers and 18 imports), with both union and per-consumer agreement. The receipt distinguishes available package files, selected inputs, SDK inputs and controlled generated imports. Arbitrary-extension imports are covered. One unverified dynamically loaded helper/task-dependency follow-up is recorded separately in the deferred ledger; no omitted helper load was established in the selected initial graph.

`root-final-verification.json` is the final acceptance record. It binds source hashes, current test binary hashes, unchanged submodule gitlinks, command completion records and compressed log/receipt hashes. `root-source-completion.json`, `root-tooling-completion.json` and `root-packages-completion.json` record actual zero exit codes. Read logs without changing evidence using `gzip -dc <file.gz>`. Focused selections overlap full suites and must not be added to the full-project count.

Three independent review layers produced fifteen findings, plus one parent finding. Concrete defects and test gaps were corrected and verified; the focused independent recheck found no remaining concrete defects. The low-priority replay-caching suggestion was rejected, and the helper-dependency question remains explicitly unverified. The spec records every triage decision.

## Five closed entries

Exactly DW-11, DW-16, structural Agent projection payload validation, partial NuGet publication recovery, and actual consumer analyzer/buildTransitive inventory were closed. Each ledger resolution links here. Every other original ledger byte was preserved; the separate unverified review follow-up was appended.

The successor status contract preserves historical ordinals and prior interface implementations. Legacy HTTP rejection emits no default success-like Value. Shared route conventions cover both API families. HTTP tests cover actual pending writes, nested setup results, proposal operations, unknown outcomes and canonical correlation handling.

Projection tests assert persisted-state preservation, rebuild refusal and corrected-redelivery convergence. Historical lifecycle defaults, incomplete drafts, optional nulls, empty lists and future/retired scalar enums remain supported; malformed enum objects, arrays and booleans are refused before checkpoint advancement.

Release fixtures execute controlled subprocesses and loopback HTTP transports, including the real final verifier call, original archive hash comparison and wrong-source provenance refusal. No actual release, NuGet push or deployment was executed. Recovery deliberately requires the original source still to be current green main. Command-status host qualification remains open; no deployed topology is certified.

## Earlier evidence

`native-commands.json`, `native-results.json`, their compressed native reports and `implementation-source-hashes.json` describe the earlier implementation run, before review corrections. `review-fixes/results.json` records focused correction checks. The final root acceptance record above supersedes those earlier source/binary hashes and aggregate counts. The agent-owned broad tooling run ended without completion and is not acceptance evidence; the completed 266-test root run is authoritative.
