# Upstream integration qualification — 2026-11-16

Parent-owned combined verification and review have now completed; see
[FINAL.md](FINAL.md). This supplement retains its phase-specific evidence below.

This supplement applies to main `c5c22620cafaa0af2625896cf41022e614e9b490`
plus the recorded working-tree changes. The original [report](README.md),
[verification](verification.json), [review corrections](review-corrections.json)
and native/gate/consumer logs are pre-integration evidence. Their original rows,
source hashes, frozen intent and results remain historical records; they do not
claim combined-source requalification. [integration.json](integration.json)
records this supplement's source hashes and focused results. Parent-owned combined
validation and independent review remain required.

The upstream pure preparation/receipt module is retained and now owns the common
schema-2 observation/provenance checks. Both the CLI and the actual
`.github/actions/after-unit-tests` hook use it. The hook retains all six literal
pack commands, exact manifest/archive inventory, reserved isolated scratch and
cleanup on success/failure. Each consumer runs one observed Build; its analyzer
items come from that Build. Preprocessing uses the same complete properties and
environment, including `UseSharedCompilation=false`, and is independently traced.
The CLI retains `--no-receipt`, exact archive checks and its reviewed native C
observer. Suppressing receipt output does not suppress observation or policy.
No deleted Story-wide verifier, package manifest/lock or other tooling is restored.

The actual MessagePackAnalyzer 3.1.12 archive contains
`analyzers/roslyn4.3/cs//MessagePack.Analyzers.CodeFixes.dll`, while NuGet's library
inventory and extracted file have one slash. Package provenance now compares
unique archive members after redundant-slash normalization. Library-file
membership remains exact. Ambiguous normalized names, traversal components,
unlisted/generated files, unowned symlinks, observed-byte changes and package
mutation still refuse. The real-package restore regression demonstrates the
published member and independently refuses an injected library-listed file that
is absent from the archive. No executable baseline or dependency version changed.

The product hook uses literal `strace --kill-on-exit -ff -e trace=process` commands
and the same managed/native load observer, mutation watcher and exact policy as
the CLI. The complete hook checks Linux/glibc/inotify and the presence of `strace`
before packing. A literal capability/ptrace probe runs before observer compilation
or consumer restore. The tool must support `--kill-on-exit`, fork/exec following,
per-thread output and `-E` environment selection. Missing tool/capability,
missing/truncated phase traces and executions without managed/native traces
refuse qualification. The final parser requires trace-file closure over every
created thread/process and terminal exit records for every file; `-q` retains
those records. Focused controls refuse a missing thread file and a thread file
truncated before its terminal record. Loader trace output is absolute and inherited environment
settings are restored. The native fixture suite additionally requires `cc` and
static libc development files; absent prerequisites fail rather than skip.

The host initially lacked `strace`. The parent extracted the distro package into
a retained `run-with-temp` directory and supplied only that private bin directory
on PATH; no global install occurred. [strace-bootstrap.json](strace-bootstrap.json)
retains package/binary hashes and version 6.19. The actual tracee-cleanup regression
forks a detached child, verifies strace remains waiting after its parent exits,
kills strace, and verifies the child is killed.

[scan-discovery.json](scan-discovery.json) records the actual CiCd
`scan_discovery.Discovery.discover` coverage checker, run against a frozen
working-tree snapshot and pinned committed checker bytes. It accepts the actual
hook metadata and shared-helper imports with zero errors and records the literal
compiler argv. No owner checker/policy was changed. This focused source-discovery
check is not the full network cross-module duplicate detector.

With those host prerequisites available on PATH, reproduce the actual product
hook and source-discovery check from the repository root:

```bash
run-with-temp bash _bmad-output/implementation-artifacts/tests/ingress-consumer-2026-11-16/verify-integrated-product-hook.sh
run-with-temp python3 -B _bmad-output/implementation-artifacts/tests/ingress-consumer-2026-11-16/verify-integrated-discovery.py
```

The first real hook attempt exposed relative native trace paths losing compiler
children after a working-directory change. That attempt refused qualification
and cleaned its reserved scratch. Absolute native trace output fixed the issue.
The separate successful runs and final focused results are listed in
[integration.json](integration.json); no C# gate is claimed rerun after integration.
An exploratory whole-hook harness was edited while Bash was still reading it,
which produced a shell command error; its result is discarded and retained as a
harness failure. The final whole-hook harness uses stable script bytes and fresh
private preparation. An attempted reuse of an already-cleaned producer scratch
ran no qualification and was replaced by that fresh preparation.

An upstream limitation remains outside these changes: current `ci.yml` still
references `npm ci` despite the removed root package manifest/lock, and invokes
the removed `eng/verify-story.ps1` and `eng/verify-story-5.2.ps1`. The corresponding
old setup/gate prose is also stale. These files were not resurrected and owner CI
was not changed. The actual product-hook qualification does not certify that
separate stale workflow lane. The pre-existing endpoint/DW-21 owner-bound
administrator-verifier and durable-store qualification boundaries remain open.

Final tooling exposed triage **U3**, a pre-existing cohort-verifier parser defect:
successful SDK graph evaluation can retain an MSBuild-server fallback diagnostic
before JSON in its combined log. The verifier now reads the SDK's dedicated
result file and retains the complete diagnostic log and all existing exit/audit
checks. The affected compiler/asset class passes 7/7; a further actual SDK fixture
passes while preserving the same real diagnostic. [U3 evidence and exact source
hashes](u3-cohort-parser.json) record the failed reproduction, both successful
focused runs and the unchanged pre-repair graph across both source baselines.
No broader test rerun or product-runtime change was made for this repair.
