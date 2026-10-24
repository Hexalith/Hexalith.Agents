# Deferred evidence reconciliation — 2026-10-24

This batch closes five open ledger entries against existing implementations and
fresh local evidence. It adds maintained verification of Party query deadlines;
it does not introduce five new runtime features. The older completed planner and
audit entries already describe the same fixes as their remaining open references.
Those references are reconciled here without counting their historical fixes twice.

Baseline Agents revision: `e8294427aedeb1d9a917a4a8eaa4222d430b49e3`.
The checked-out owner revisions are recorded in the accompanying
[verification record](deferred-evidence-verification-2026-10-24.json).
No dependency checkout or gitlink changes in this batch.

## Shared release configuration

The open “Derive semantic-release planning and publication from one release
configuration” entry is the older reference to the planner item closed by
[the CI/release follow-up](spec-five-deferred-ci-release-followups.md).
[The planner](../../eng/semantic-release-plan.mjs) now reads the repository's
`.releaserc.json` branches, tag format and analysis plugin options. It runs against
an isolated mirror and excludes publication hooks. Publication reads the same JSON
file. Top-level analysis overrides and shareable configurations are refused with
an explicit diagnostic, preventing a successful plan that silently ignores release
rules. Analysis options belong in the declared analysis plugin tuples. Unreviewed
plugins and exec analysis hooks are also refused instead of silently omitted.

The maintained ten planner tests execute scratch repositories and cover custom
branches, tags, release rules, reordered plugins, forbidden hooks and source-ref
preservation, plus refusal of top-level analysis overrides and shareable configs.
This closes duplicated configuration; partial NuGet publication
recovery and simultaneous credential-holder publication remain open.

## High and critical NuGet audit diagnostics

The older “Decide whether high and critical NuGet audit diagnostics should fail the
build” entry describes the policy already implemented by
[the build verification batch](spec-five-deferred-build-verification-items.md).
[Directory.Build.props](../../Directory.Build.props) enables all-dependency auditing
and warnings as errors, with exemptions only for `NU1901` and `NU1902`.
`NU1903` and `NU1904` are neither exempted nor suppressed in ordinary Release
evaluation. Controlled verifier subprocesses inject high-severity failures and
require immediate termination, with no audit-disable argument.

The earlier Story 5.2 advisory wording is historical. Its appended current-policy
note explicitly supersedes that wording using the already adopted severity policy.
No new advisory exception or security risk acceptance is granted.

## Story 5.2 CI and release evidence

Every sub-item of the broad “Make CI and release evidence honest” entry now has a
current implementation and maintained guard:

| Concern | Current evidence |
| --- | --- |
| Focused classes, source mode, floors and skips | `ci.yml` runs Story 5.1 source/package floors, then the full Story 5.2 `-SkipBuild` gate. Each focused native result requires the expected assembly and nonzero passed cases; project regressions require their Debug floor and reject errors, failures, skips and unrun cases. Controlled subprocess tests exercise missing selections and bad summaries. |
| Builds workflow alignment | CI/release refs and release execution SHA match the root Builds gitlink `359fee24aeca4d9eabed4b425d4cfa2520b2adda`. CI checks the actual checkout. |
| Checkout credentials and npm tooling | Root caller checkouts disable persisted credentials; the Agents policy job uses locked installation with ignored scripts, followed by npm signature auditing, matching the original source-spec finding. |
| Package and test inventory | `PackageInventoryTests` compares the five declared test projects with disk and the expected release package count with the six-package manifest. |
| Dependabot gitlinks | `.github/dependabot.yml` includes the root gitsubmodule ecosystem. The coupled Builds update procedure remains documented in `CONTRIBUTING.md`. |
| Disabled publication | Release stays green, writes an explicit “No packages were published” summary, and skips publication and its assertions unless the enable variable equals `true`. The corresponding conformance test pins that choice. |

The original credential finding specifically names `agents-policy` in the
completed Story 5.2 source spec (the review task beginning “The agents-policy job
persists checkout credentials”). This closure concerns that root job. The pinned
shared `domain-ci.yml` build checkout still uses its default credential behavior;
this batch neither changes it nor claims all reusable checkouts disable credentials.

The fresh 26-case `PackageInventoryTests` and `BuildContractConformanceTests`
selection passes with zero failures, skips, errors or unrun tests. Workflow lint
and the complete tooling suite verify the existing automation. These are local
checks; no new hosted Actions run, release dispatch or package publication is
claimed. Earlier full source/package runs remain in their original implementation
specs; this batch does not relabel those historical results as fresh runs.

## Packaged retained-history API B01

The shared catalog currently selects EventStore `3.126.0`. Inspection of the
actual NuGet Client package finds
`Hexalith.EventStore.Client.Streams.RetainedIdentityHistoryReader`, implementing
`IRetainedIdentityHistoryReader`. Its `ReadAsync(AggregateIdentity, string,
CancellationToken)` returns `Task<RetainedIdentityHistoryReadResult>`.

An isolated Debug console project outside the repository references only
`Hexalith.EventStore.Client` version `3.126.0`. It compiles a delegate invoking
that exact interface method and loads the concrete implementation. Restored
assets contain package libraries only, including Client `3.126.0`; no project
reference supplies the missing type. The verification JSON preserves the probe
source/project, outputs, package hash and resolved library inventory.

Reproduce inspection with:

```bash
dnx dotnet-inspect -y -- member RetainedIdentityHistoryReader --package Hexalith.EventStore.Client@3.126.0
dnx dotnet-inspect -y -- member IRetainedIdentityHistoryReader --package Hexalith.EventStore.Client@3.126.0 -m ReadAsync
```

To reconstruct and run the package-only compilation from the repository root,
using the current shared catalog and repository SDK selection:

```bash
python3 - <<'PY'
import json, shutil, subprocess, tempfile, xml.etree.ElementTree as ET
from pathlib import Path
root = Path.cwd()
probe = json.loads((root / '_bmad-output/implementation-artifacts/deferred-evidence-verification-2026-10-24.json').read_text())['packageProbe']
version = subprocess.check_output(['dotnet', 'msbuild', 'src/Hexalith.Agents.Contracts/Hexalith.Agents.Contracts.csproj', '-nologo', '-getProperty:HexalithEventStoreVersion', '-p:Configuration=Release', '-p:UseHexalithProjectReferences=false'], text=True, timeout=60).strip()
project = ET.fromstring(probe['project'])
project.find('.//PackageReference').set('Version', version)
with tempfile.TemporaryDirectory(prefix='agents-retained-package-') as directory:
    target = Path(directory)
    shutil.copyfile(root / 'global.json', target / 'global.json')
    ET.ElementTree(project).write(target / 'TransportProbe.csproj')
    (target / 'Program.cs').write_text(probe['source'])
    subprocess.run(['dotnet', '--version'], cwd=target, check=True, timeout=30)
    subprocess.run(['dotnet', 'run', '--project', 'TransportProbe.csproj', '-c', 'Debug'], cwd=target, check=True, timeout=180)
    libraries = json.loads((target / 'obj/project.assets.json').read_text())['libraries']
    assert f'Hexalith.EventStore.Client/{version}' in libraries
    assert all(library['type'] == 'package' for library in libraries.values())
    print(f'Package-only retained-history API verified: {version}')
PY
```

This closes the claim that packaged retained transport APIs are absent. It proves
API availability and package-only compilation, not an authenticated owner read,
retention/erasure conformance, source/package revision equality or full production
qualification. Those requirements and dependency acceptance remain unchanged.

## Party query deadline B06

The pinned Parties query service already starts a single operation budget for
both current and historical identity queries. The default and maximum are 30
seconds; invalid timeouts refuse before providers. Source and custody reads share
that budget. Bounded waits and isolated invocation prevent a noncooperative
provider from indefinitely delaying the caller's safe verdict. Terminal checks
also enforce elapsed time when timer callbacks lag.

[The maintained identity command](../../scripts/verify-parties-identity.py) now
requires 54 additional exact owner theory rows across twelve methods, including
noncooperative async/synchronous source and custody providers, shared budget,
invalid timeouts, setup time, final authority expiry, both caller-cancellation
orderings, blocking cancellation callbacks, late callback faults and successful
reads one tick before expiry. Each selection
requires fresh native XML, the exact assembly/type/method, all named scenarios and
zero failures, errors, skips or unrun tests. Thirty-six controlled negative executions
independently omit, substitute or skip results at each of the twelve new selections
and must prevent the success receipt and every later stage.

Reproduce with:

```bash
python3 scripts/verify-parties-identity.py --artifacts /tmp/agents-parties-identity-deadlines
python3 -m unittest discover -s tests/tooling -p '*_test.py'
```

The command requires the prior 24 owner cases plus the 54 deadline cases and 14
exact-target receipt cases. B06's unbounded query wait is closed. Synchronous
authority responsiveness, abandoned-worker admission/resource reclamation,
standalone retained transport invocation and live owner qualification retain
their separate open entries; bounded verdict waits do not establish reclamation.

## Verification provenance

The machine-readable record includes commands, counts, canonical revisions,
result hashes, each native test identity/outcome/summary, tool versions and the
isolated package probe. Temporary log hashes supplement the durable embedded native
results. The first Server test attempt used
output under `/tmp`, where the existing `ModuleLayout` cannot discover its
repository ancestor. Rebuilding into a fresh ignored repository artifact directory
and rerunning the exact selection resolves that fixture location error without
changing its root-discovery code or weakening a check.

Final independent review and final verification outcomes are appended to
[the implementation spec](spec-five-deferred-evidence-reconciliation-items.md).


Final results: the initial complete tooling discovery passed 213 tests. After review
corrections, both affected modules passed again (31 identity-harness and 10 planner
tests). Fresh native verification passed all 78 owner cases, 14 exact-target receipt
cases and 26 Server conformance cases, with zero failures, errors, skips or unrun
cases. The package reconstruction passed under SDK 10.0.402; inspection used
dotnet-inspect 0.26.0+d236a7a. Workflow lint, JavaScript syntax, all new links and
append-only historical evidence checks passed. Every original ledger byte is
preserved beneath exactly five three-line closure insertions.
