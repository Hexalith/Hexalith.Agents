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

To build the pinned Parties identity projects and run their focused current-read,
client completion-time, authority-change, query-deadline and exact-target receipt
regressions:

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
Relative artifact
paths resolve from the caller's directory; concurrent runs need separate artifact
directories. This is local regression evidence; it does not change the dependency
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

The workflow reserves the semantic-release version, uses the protected
`production` environment and `NUGET_API_KEY` secret, and packs the six packages
declared in `eng/release-packages.json`. It validates archives and an isolated
consumer, proves package versions absent, and rechecks the exact green main
source before the first push. Publication sends only validated manifest/version
archives; unrelated files are preserved. It then verifies NuGet presence and the
GitHub Release's source and six downloaded archives. Each GitHub asset must match
its API SHA-256 digest, byte length and package identity. NuGet presence requires
downloading every exact package and comparing all ordinary archive members with
the release artifacts. Both gates validate package boundaries and enforce archive
and expanded-content limits. Existing versions fail the release;
`--skip-duplicate` is not used. A partial push requires maintainer investigation
because automatic reconciliation is not implemented.

The NuGet comparison permits only changes to the root `.signature.p7s` member:
[NuGet.org repository signing](https://devblogs.microsoft.com/dotnet/introducing-repository-signatures/)
adds or countersigns that entry while preserving other package contents. This is
content comparison, not cryptographic signature verification. Run the presence
CLI with `--package-directory <original-archives>`; the GitHub CLI verifier requires
`--asset-directory <downloaded-assets>`. The release workflow supplies both.
It preflights release metadata before downloading, caps downloaded files at 64 MiB,
then runs full archive verification. Metadata-only success explicitly states that
contents have not yet been verified.

Dependency validation retains nuspec include/exclude metadata and rejects ordinary
dependency build assets. The offline restore regression shows that removing the
generated `exclude="Build,Analyzers"` imports an ordinary build target. This check
does not establish suppression of transitive analyzer or `buildTransitive` assets;
the fixture explicitly checks the analyzer limitation against the installed SDK.

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
