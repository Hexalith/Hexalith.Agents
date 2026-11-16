# Contributing to Hexalith.Agents

Read [AGENTS.md](AGENTS.md) and the referenced Hexalith instructions before working
in this repository. Run the commands below from the repository root.

## Setup

Install the .NET SDK selected by `global.json`, Python 3.10 or later, PowerShell
7, and Node.js 24.10 or later (Node 22.14 or later within version 22 is also
supported by `package.json`). Initialize only the root-declared submodules:

```bash
mapfile -t paths < <(git config -f .gitmodules --get-regexp path | awk '{print $2}')
git -c submodule.recurse=false submodule update --init -- "${paths[@]}"
npm ci --ignore-scripts
npm audit signatures
```

The submodule commands require Bash. Do not initialize nested submodules. Keep
Node dependencies locked in `package-lock.json`; use `npm ci` to reproduce them.

## Build and test

Local development uses Debug builds and sibling Hexalith project references:

```bash
dotnet restore Hexalith.Agents.slnx -p:Configuration=Debug
dotnet build Hexalith.Agents.slnx -c Debug --no-restore
dotnet test --project test/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj -c Debug --no-build --fail-skips on
python3 -m unittest discover -s tests/tooling -p '*_test.py'
```

Run each affected test project individually. `global.json` selects
Microsoft.Testing.Platform; use `dotnet test --project`, with platform options
such as `--minimum-expected-tests` and `--fail-skips on`. For a focused xUnit v3
run, build the project and invoke its test assembly with single-dash `-class`
or `-method` selectors. Do not use solution-level `dotnet test` or assume the
legacy VSTest `--filter` syntax applies.

The tooling suite also runs the pinned Parties readiness verifiers against
temporary loopback fixtures. To run only those regressions:

```bash
python3 -m unittest discover -s tests/tooling -p 'parties_readiness_verifier_test.py'
```

These checks require PowerShell 7 and use the root-declared Parties submodule.
They verify expiry, retained transitions, receive limits, scope and caller-relative
paths. They establish synthetic verifier behavior; dependency acceptance and
production qualification still require their existing owner evidence.

To build the pinned Parties and EventStore identity projects and run their focused
current-read, client completion-time, authority-change, successor-continuity,
endpoint-security, query/transport-deadline and exact-target receipt regressions:

```bash
python3 scripts/verify-parties-identity.py --artifacts /tmp/agents-parties-identity
```

The command uses Debug project references to root-declared dependencies, with the
existing package fallback for optional Memories. It records build logs, fresh xUnit
results and a success-only `identity-verification.json` with source revisions,
dirty state and execution timestamps. Root-owned non-null authority-change tests
are compiled into the owner test assembly only for this command. The deadline
selections exercise current and historical queries, noncooperative source/custody
providers, the shared operation budget, cancellation races and a successful read
immediately before expiry. These checks establish bounded verdict waits; shared
worker admission and resource reclamation still require owner qualification.
The successor selections require authenticated actor-free continuity certificates
and deny missing, foreign, expired or malformed proofs. The endpoint selections
run all process and security cases through the real workload/channel handlers and
unchanged SDK audit; their substituted backend health does not prove backend
readiness. Standalone retained-history transport selections additionally exercise
synchronous and noncooperative providers, blocking cancellation callbacks, terminal
budget checks and late owned-material disposal. EventStore source provenance is
recorded alongside Agents and Parties, plus Builds, Commons, Tenants, McpCli and
FrontComposer. The receipt fingerprints tracked diffs and untracked content,
checks source stability before success, and records test assembly hashes. Keep
these repositories stable during the command.
Relative artifact paths resolve from the caller's directory. Choose a directory
outside every consumed repository; source-tree outputs and ancestors are rejected
before verification starts. Concurrent runs need separate artifact directories. This is local regression evidence; it does not change the dependency
register or establish live readiness.

The CI package lane restores and builds Release with
`-p:UseHexalithProjectReferences=false`. Release and packing require package
references. All external package versions come from
`references/Hexalith.Builds/Props/Directory.Packages.props`. To run the existing
Story 5.1 source/package test floors, skip rejection, package validation, and
isolated consumer gates, use:

```bash
pwsh -NoProfile -File eng/verify-story.ps1 -Story 5.1
```

This full gate packs local verification artifacts; it does not publish packages.
CI also runs `eng/verify-story-5.2.ps1 -PackageFloorOnly` and shared package-authority
validation as declared in `.github/workflows/ci.yml`.

## Commits and pull requests

Use Conventional Commits for commit messages and PR titles, for example
`fix(release): reject unsafe package paths`. The commitlint workflow checks PR
titles and commits pushed to `main`; dependency updates follow the same rule.
Validate a message before committing it:

```bash
printf '%s\n' 'fix(release): reject unsafe package paths' > /tmp/agents-commit-message.txt
npx --no-install commitlint --edit /tmp/agents-commit-message.txt
git commit -F /tmp/agents-commit-message.txt
```

Semantic-release uses `.releaserc.json` and commit history to calculate releases:
`fix` normally selects a patch, `feat` a minor, and a breaking change a major.
Other types alone normally do not trigger publication. The planner reads that
same configuration. `node eng/semantic-release-plan.mjs` computes the decision
using an isolated local mirror without publishing. Put analysis options in the
configured commit-analyzer and release-notes-generator plugin tuples. The planner
rejects top-level `analyzeCommits`, `generateNotes` and `extends` settings to avoid
silently planning a different release from publication. Additional plugins and
exec analysis hooks also require planner support before they can be used.

## Release policy

Releases are manually dispatched through `.github/workflows/release.yml` from
the current green `main` tip. A push to `main` runs CI; it does not dispatch a
release. The repository variable `HEXALITH_RELEASE_PUBLISH_ENABLED` must equal
the string `true` to enable publication. Otherwise the workflow stays green,
reports the freeze in its job summary, and skips publication and its assertions.

Both publication workflows use the protected `production` environment and
NuGet trusted publishing. Set the repository variable
`NUGET_TRUSTED_PUBLISHING_USER` to the individual policy creator's NuGet.org
profile username, including for organization-owned packages; do not use the
organization name. Configure separate GitHub Actions policies for
`Hexalith/Hexalith.Agents`, one for `release.yml` and one for
`recover-release.yml`, each matching the `production` environment. Authorize
both workflow identities to publish all six packages in
`eng/release-packages.json`: `Hexalith.Agents.Contracts`,
`Hexalith.Agents.EventStore`, `Hexalith.Agents.Client`, `Hexalith.Agents`,
`Hexalith.Agents.UI` and `Hexalith.Agents.Testing`.

Each publication job requests `id-token: write`; the pinned `NuGet/login` action
exchanges that identity for a short-lived publication credential. Only that
action's output is passed to the publisher scripts as `NUGET_API_KEY`. Stored
API-key secrets are ignored. A missing or mismatched policy, or a failed login,
blocks publication; correct the policy/account/workflow/environment match
before retrying. Disabled publication jobs do not request credentials.

The release workflow reserves the semantic-release version and packs the six
manifest packages. It validates archives and an isolated
consumer, proves package versions absent, and rechecks the exact green main
source before the first push. Publication sends only validated manifest/version
archives; unrelated files are preserved. It then verifies NuGet presence and the
GitHub Release's source, six downloaded archives and retained provenance. Each GitHub asset must match
its API SHA-256 digest, byte length and package identity. NuGet presence requires
downloading every exact package and comparing all ordinary archive members with
the release artifacts. Both gates validate package boundaries and enforce archive
and expanded-content limits. Existing versions fail the release;
`--skip-duplicate` is not used. Before publication, the root-owned
`eng/github-release-publisher.mjs` semantic-release hook creates a draft release
containing all six original archives and `release-provenance.json` (repository,
version, exact source SHA/tag, archive byte lengths and SHA-256 hashes). It
replaces the GitHub publisher plugin so only one release is created. The draft
is finalized only after complete NuGet content verification; failures retain it.
Presence verification allows ninety ten-second waits (fifteen minutes), plus
bounded request time, for NuGet indexing after accepted uploads. If indexing
still times out, retain the draft and original archives and recover that exact
release once the feed is available; do not repack or choose a new version merely
because the accepted uploads are not yet indexed.

To recover a partial push, dispatch `.github/workflows/recover-release.yml` from
`main` with the original version and source SHA. The protected `production`
environment, publication freeze and `release-production` concurrency group also
apply to recovery. Recovery requires the original source still to be current
green main, its exact existing tag and original unpublished draft. It downloads
those original archives, checks GitHub asset digests and provenance, validates
every archive, then probes the full immutable NuGet set and compares every
existing package before any missing push. A collision, unconfirmed probe,
changed main or archive mismatch refuses every missing push. It never repacks or
runs ordinary release planning. After missing pushes, it verifies the complete
remote content set before finalizing the draft. If main has advanced, recovery
requires a new approved procedure; this workflow provides no older-source exception.

The NuGet comparison permits only changes to the root `.signature.p7s` member:
[NuGet.org repository signing](https://devblogs.microsoft.com/dotnet/introducing-repository-signatures/)
adds or countersigns that entry while preserving other package contents. This is
content comparison, not cryptographic signature verification. Run the presence
CLI with `--package-directory <original-archives>`; the GitHub CLI verifier requires
`--asset-directory <downloaded-assets>`. The release workflow supplies both.
It preflights release metadata before downloading, caps downloaded files at 64 MiB,
then runs full archive verification. Metadata-only success explicitly states that
contents have not yet been verified.

Dependency validation retains nuspec include/exclude metadata and checks ordinary
dependency build selection. Analyzer and `buildTransitive` selection have separate
semantics. `scripts/validate-consumer-package-references.py <package-directory>`
restores and builds each packed Agents package individually and all six together
in isolated directories with a copy of the root `global.json`. Its normalized
schema-2 receipt records selected analyzers, active imports, and assemblies and
native libraries actually loaded by MSBuild, tasks and the fresh compiler. Each
package input carries its owning package, exact version, path and SHA-256. Available
archive assets, generated NuGet imports, the generated observation assembly, SDK
assemblies/native libraries and OS native libraries have separate provenance.

Executable qualification requires Linux/glibc and permitted kernel ptrace/inotify
observation. The standalone CLI additionally requires a native `cc` compiler.
The actual after-unit-tests product hook, `scripts/check-built-packages.py`, uses
system `strace` with `--kill-on-exit`, `-ff`, `-e trace=process` and `-E` support
so its compiler commands remain literal and discoverable by CiCd. It refuses a
missing system tracer before packing and probes its tracing/capability support
before observer compilation or consumer restore. No package installation is
performed by the hook. Unsupported platforms and missing CLI prerequisites
refuse at the complete consumer entry before restore or SDK builds. The native
regression fixtures also require static libc development files (`cc -static`);
missing prerequisites fail with explicit diagnostics and never skip qualification.
The standalone verifier first compiles/probes `scripts/consumer-process-observer.c`, which
independently follows process/thread creation and execution through kernel ptrace.
Static executables and children clearing their environment cannot disappear from
this process inventory; executions lacking managed/loader observations refuse.
The tracer uses `PTRACE_SEIZE`/`PTRACE_LISTEN` to preserve genuine group stops
and SIGCONT while distinguishing synthetic attachment events. It preserves ordinary
SIGTRAP delivery, retries interrupted waits and awaits detached descendants; it
does not consume WCONTINUED notifications intended for the child's real parent.
The hook's system tracer independently follows the same process tree, waits for
detached descendants and kills tracees if the tracer exits. Both backends require
complete process observations for preprocessing and Build; either phase missing
or truncated refuses qualification. Every referenced thread/descendant must have
a terminal trace record. Loader output uses absolute paths so a
compiler's working directory cannot drop its native observations.

The verifier compiles `scripts/StartupHook.cs` beneath the consumer's excluded
`obj` directory and installs its .NET assembly-load observer alongside
`LD_DEBUG=files`. Shared compilation and persistent MSBuild nodes are disabled. Analyzer items come
from that same observed Build. Import preprocessing runs under the same complete
properties and environment (including `UseSharedCompilation=false`), with the same
process, managed-load and native-load observation; it never runs a second Build.
An inotify watch covers restored package directories and file inodes throughout
both phases. Any write, replacement or membership mutation refuses, even when a
helper executes changed bytes and atomically restores the reviewed file before
exit. Package provenance requires exact membership in both the restored assets
library file inventory and its `.nupkg` archive. Archive membership matches NuGet's
normalization of redundant slashes only when the resulting member is unique;
ambiguous normalized members and traversal components refuse. This handles the
published MessagePackAnalyzer 3.1.12 CodeFixes member with its doubled archive
slash without admitting generated or unlisted files.
Both the MSBuild process and a fresh compiler must complete observation; missing
managed/native traces, byte-only or dynamic assemblies without file provenance,
unobserved executable subprocesses, changed observed bytes and unowned package
helpers refuse. Unsupported platforms refuse before building. Native libraries
under the resolved `/lib`, `/lib64` and `/usr/lib` roots are recorded as OS inputs;
those under the installed dotnet host, SDK, packs or shared runtime are SDK inputs.
These inputs do not silently become package-owned baseline entries.

The isolated restore regression executes a real source generator and `UsingTask`
assembly that load a separate managed helper and native helper. Changing either
helper while preserving the entry assemblies/imports causes policy refusal;
external helper symlinks and unlisted files beneath a package root fail ownership
checks. Real static/sanitized-environment children, byte-loaded and emitted managed
assemblies, and native atomic-restoration fixtures prove refusal after their helper
code successfully executes. This is local consumer qualification, not installed-host
or live runtime qualification.

Ingress gateway callback fixtures apply/replay aggregate events in memory. They do
not qualify durable-store transactions or a production proof/admission provider.
Provider caller tests invoke the SDK dispatcher directly and bypass the `/query`
endpoint's administrator verifier. A working platform-query endpoint still needs
an owner-bound `IDomainServiceAdministratorVerifier` and authenticated HTTP positive
control; that pre-existing composition qualification remains open under DW-21.

`eng/consumer-executable-assets.json` is the reviewed executable asset
policy. CI refuses additions, removals, version changes, hash changes and changed
per-consumer selection. Retain `--receipt <path>` for dependency-change reviews.
`--write-baseline <candidate-path>` only generates a candidate: inspect every
selected package/path/version/hash and each consumer graph before explicitly
replacing the policy. Build exclusions alone never establish analyzer purity.

The legacy provider command-outcome operation is retained with typed rejection.
Consumers use `IProviderCatalogOperations.GetCommandWriteOutcomeAsync` and
`/api/v1/agents/operations/providers/commands/{targetTenantId}/{messageId}`.
`AgentSetupWriteOutcome.Unknown=0` fails closed; legacy enum ordinals and setup
result records remain stable. HTTP operation responses preserve typed bodies and
correlation while mapping failures to their canonical non-200 statuses. Pending
and checking outcomes use 202; unknown statuses use 500.

For the focused EventStore gateway source-reference regression, use fresh artifacts
to avoid stale package/source assets in shared `obj` directories:

```bash
dotnet build test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj -c Debug --artifacts-path /tmp/agents-gateway -p:UseHexalithProjectReferences=true -m:1
dotnet /tmp/agents-gateway/bin/Hexalith.Agents.Server.Tests/debug/Hexalith.Agents.Server.Tests.dll -class Hexalith.Agents.Server.Tests.AgentsEventStoreGatewayIntegrationTests -result-xml /tmp/agents-gateway.xml
```

Inspect the fresh XML for nonzero passing tests and zero failures, skips, not-run
tests and errors. The gateway suite includes equivalent-intent replay and changed
payload conflict; the owner admission actor regressions additionally check that
the first stored intent is retained. These are local regressions and do not
establish production Dapr persistence or dependency acceptance.

## Terminal setup command status

A configured `Agents:EventStore:BaseUrl` (HTTP or HTTPS) and exact
`Agents:EventStore:AppId` bind the live setup status reader. The optional
`Agents:EventStore:DaprApiToken` supplies routing authentication only. Status uses
`GET api/v1/commands/status/workload/{tenant}/{messageId}` with an independently
provisioned `IEventStoreGatewayWorkloadAssertionSource`: its grant must authorize
`eventstore:command-status`, the receiving gateway audience, and the exact trusted
tenant and command domain. Dapr attribution and human bearer credentials do not
replace that assertion. Register host overrides before setup composition. Legacy
message-only reader registrations remain preserved, but their evidence is not used
for scoped reads: implement the scoped rejection overload and status DTO read
explicitly. Host command admission must preserve the exact transient
`AgentCommandIntent.Authority` reference when adding `trust:` metadata. When reconstructing
an intent, explicitly copy its original `Authority` property; omitting, replacing
or renewing that bound is refused before EventStore command construction.

Both dependency modes use the supported public SDK workload-status transport and
exact-domain assertion delegation. The maintained package gate requires verified
compatible Client/DomainService **3.128.3** or later; the shared Builds catalog
selects the versions. Both source and package tests exercise the configured live
reader, declared-domain issuer, credential refusal and host-reader overrides.
The earlier 3.127.0 package refusal remains historical evidence in
[the terminal setup report](_bmad-output/implementation-artifacts/deferred-terminal-setup-verification-2026-10-25.md).
A client transport update alone is insufficient: the host issuer must grant the
exact requested status operation, tenant and declared domain.

Agents generates no credentials. Missing/refused credentials and unsupported
SDK capabilities preserve `UnableToVerify` and the original retry identity;
Dapr routing identity and human credentials cannot substitute for a workload
grant. The public user-status route is never a fallback.

Only matching tenant, domain, message and correlation evidence with a canonical
domain rejection releases a fresh attempt. A rejected/blocked UI result uses the
existing localized rejection message; a retry cannot erase an earlier accepted
receipt or its projection target. Loading, unavailable, missing and unknown setup
surfaces refuse stale submit events before allocating identities. Local unit and
in-process evidence does not qualify a deployed Dapr topology, owner credential
provisioning, production readiness, or DW-6/DW-19/DW-21.

## Updating the Builds dependency

A Dependabot proposal that advances `references/Hexalith.Builds` must carry the
matching workflow and conformance-test updates in the same parent commit or PR.
A gitlink-only proposal is expected to fail the consistency gate. Do not remove
or relax that gate to merge the update.

1. Review the proposed Builds revision and its reusable workflow changes. Obtain
   its full commit ID from Git, for example
   `git -C references/Hexalith.Builds rev-parse HEAD`, after selecting the reviewed
   checkout. Keep updates scoped to the root-declared submodule.
2. Set all five reusable workflow refs to that same full ID in
   `.github/workflows/{ci,release,codeql,commitlint,dependency-review}.yml`. Update
   the literal Builds checkout assertion in `ci.yml` and `builds-execution-sha`
   in `release.yml`. Review input/permission compatibility with the new workflows.
3. Update the expected revision in
   `test/Hexalith.Agents.Server.Tests/PackageInventoryTests.cs`. Search for the
   previous full ID in active workflows/tests to catch every coupled assertion.
   Historical implementation specs keep their original evidence.
4. Install locked Node tooling, run the tooling tests and package-authority
   validation, then run the Story 5.1 gate above against the selected catalog.
   If available, run `actionlint -no-color` for the workflow changes. Investigate
   any new dependency or test-floor failure before merging.
5. Include the Builds gitlink, all workflow refs/assertions, and the conformance
   test updates together. Confirm they all name the reviewed revision and that
   source/package validation passes. The parent CI checks the initialized Builds
   checkout against its literal pin before proceeding.

## Deferred state and dependency replay

The pending governance-command store requires browser Web Locks and retains a
command when storage, authentication, cancellation or contention prevents a safe
conditional mutation. Its module compares the exact previously validated storage
bytes while holding the browser lock. A stale comparison returns to C# for a
fresh authenticated read and validation, with eight attempts at most.

Run state regressions with disposable repository copies and retain their native
results outside the job directory:

```bash
run-with-temp bash _bmad-output/implementation-artifacts/tests/deferred-state-dependencies-2026-11-16/verify-state.sh
run-with-temp node --test tests/tooling/pending_provider_storage_test.mjs
run-with-temp python3 -m unittest discover -s tests/tooling -p deferred_dependency_cohort_test.py -v
```

The historical spool fixtures are generated by the exact pre-reservation source,
with source and JSON hashes retained in Platform's fixture provenance. From the
Agents root, reproduce them with:

```bash
run-with-temp python3 references/Hexalith.Platform/tests/Hexalith.Platform.Custody.Tests/Fixtures/pre-reservation/generate.py
```

The three dependency questions are independent. The replay exports recorded Git
archives into its private `TMPDIR`, preserves the original historical SDK and
Builds catalogs, changes only the five historical pins, and runs native owning
assemblies. It never updates checkout gitlinks or initializes nested submodules.
Use a fresh durable evidence directory for each run:

```bash
run-with-temp python3 scripts/verify-deferred-dependency-cohort.py --evidence /absolute/durable/cohort-evidence
```

`--lane current`, `--lane historical`, or `--lane platform` selects one experiment.
Repeated `--owner-root Hexalith.Works=/absolute/local/repository` arguments supply
local repositories containing exact recorded owner objects. No remote retrieval
occurs. Complete producer preparation retains every selected root; missing
objects refuse before the producer can initialize them. The Platform tuple
requires Works `b2985d22b3bfbc6d86bcde28a5da722ae7f930c3`; use its exact local
repository when supplying `--owner-root`. Repeated
`--platform-overlay relative/path=/absolute/local/file` arguments verify explicit
local Platform corrections over the recorded baseline. Their original/corrected
hashes and bytes are retained, and the run is labeled as an overlay rather than
pristine baseline evidence. Passing the eight default source cases does not
replace the required producer-mapped 38-case catalog selection. Failed or
incomplete lanes remain blocked; restore failure alone does not prove a pin
necessary. These checks grant
no live owner, deployment, release or package-publication qualification.
