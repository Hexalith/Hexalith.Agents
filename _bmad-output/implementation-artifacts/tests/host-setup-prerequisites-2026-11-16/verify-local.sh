#!/usr/bin/env bash
# Local prerequisites only: no owner/live calls, package publication or submodule updates.
set -euo pipefail
PREREQUISITE_ROOT=$(cd "$(dirname "${BASH_SOURCE[0]}")/../../../.." && pwd)
PREREQUISITE_EVIDENCE=${1:-"$PREREQUISITE_ROOT/artifacts/host-setup-prerequisites-2026-11-16/reproduction"}
mkdir -p "$PREREQUISITE_EVIDENCE"
PREREQUISITE_EVIDENCE=$(cd "$PREREQUISITE_EVIDENCE" && pwd)
export PREREQUISITE_ROOT PREREQUISITE_EVIDENCE
run-with-temp bash <<'JOB'
set -euo pipefail
export MSBUILDDISABLENODEREUSE=1 DOTNET_CLI_USE_MSBUILD_SERVER=0
# The focused source build uses unchanged direct checkouts and puts all generated output in private scratch.
roots=(-p:HexalithBuildsRoot="$PREREQUISITE_ROOT/references/Hexalith.Builds")
for owner in EventStore Commons Conversations Parties Tenants FrontComposer; do
    roots+=("-p:Hexalith${owner}Root=$PREREQUISITE_ROOT/references/Hexalith.$owner")
done
set +e
dotnet build "$PREREQUISITE_ROOT/test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj" \
    -c Debug -m:1 --artifacts-path "$TMPDIR/source" -p:UseHexalithProjectReferences=true \
    -p:MinVerVersionOverride=1.0.0 "${roots[@]}" --nologo \
    > "$PREREQUISITE_EVIDENCE/focused-source-ui-build.log" 2>&1
source_exit=$?
set -e
printf '%s\n' "$source_exit" > "$PREREQUISITE_EVIDENCE/focused-source-ui-build.exit"
# Preserve the complete tracked inventory, including archived MSBuild inputs the authority validator inspects.
# Both clones are local; no network request or nested submodule initialization occurs.
git clone --quiet --shared --no-recurse-submodules "$PREREQUISITE_ROOT" "$TMPDIR/repo"
git clone --quiet --shared --no-recurse-submodules "$PREREQUISITE_ROOT/references/Hexalith.Builds" \
    "$TMPDIR/repo/references/Hexalith.Builds"
rsync -a --delete --exclude=.git --exclude=references --exclude=/artifacts --exclude=bin --exclude=obj \
    --exclude=node_modules "$PREREQUISITE_ROOT/" "$TMPDIR/repo/"
cd "$TMPDIR/repo"
pwsh -NoLogo -NoProfile -File eng/verify-story-6.1.ps1 -DependencyMode Package \
    -ArtifactsPath "$PREREQUISITE_EVIDENCE/package" > "$PREREQUISITE_EVIDENCE/package-gate.log" 2>&1
server=test/Hexalith.Agents.Server.Tests/bin/Release/net11.0/Hexalith.Agents.Server.Tests.dll
ui=test/Hexalith.Agents.UI.Tests/bin/Release/net11.0/Hexalith.Agents.UI.Tests.dll
dotnet "$server" -class Hexalith.Agents.Server.Tests.ProviderQueryEndpointPrerequisiteTests \
    -result-xml "$PREREQUISITE_EVIDENCE/query.xml" > "$PREREQUISITE_EVIDENCE/query.log" 2>&1
dotnet "$server" -class Hexalith.Agents.Server.Tests.AgentsEventStoreGatewayIntegrationTests \
    -class Hexalith.Agents.Server.Tests.EventStoreAgentCommandStatusReaderTests \
    -class Hexalith.Agents.Server.Tests.AgentSetupStatusIssuerTests \
    -class Hexalith.Agents.Server.Tests.AgentSetupProjectionTests \
    -result-xml "$PREREQUISITE_EVIDENCE/host-boundaries.xml" > "$PREREQUISITE_EVIDENCE/host-boundaries.log" 2>&1
dotnet "$ui" -class Hexalith.Agents.UI.Tests.AgentsUiCompositionTests \
    -result-xml "$PREREQUISITE_EVIDENCE/ui-composition.xml" > "$PREREQUISITE_EVIDENCE/ui-composition.log" 2>&1
python3 - <<'PY'
from collections import Counter
import json
import os
from pathlib import Path
import xml.etree.ElementTree as E
root = Path(os.environ['PREREQUISITE_EVIDENCE'])
manifest = json.loads((Path(os.environ['PREREQUISITE_ROOT']) /
    '_bmad-output/implementation-artifacts/tests/host-setup-prerequisites-2026-11-16/verification.json').read_text())
expected = manifest['expectedFocusedTotals']
required = manifest['requiredMethodCaseCounts']
if set(expected) != {'query', 'host-boundaries', 'ui-composition'} or not required:
    raise SystemExit('Missing focused selection or required method inventory')
observed = Counter()
for name, total in expected.items():
    if type(total) is not int or total <= 0:
        raise SystemExit(f'Invalid expected total: {name}')
    document = E.parse(root / (name + '.xml')).getroot()
    assemblies = document.findall('assembly')
    if len(assemblies) != 1 or document.findall('.//error'):
        raise SystemExit(f'Invalid assembly or error results: {name}')
    assembly = assemblies[0]
    rows = assembly.findall('.//test')
    if any(int(assembly.get(key, '-1')) != total for key in ['total', 'passed']) or len(rows) != total:
        raise SystemExit(f'Incomplete focused results: {name}')
    if any(int(assembly.get(key, '-1')) != 0 for key in ['failed', 'errors', 'skipped', 'not-run']):
        raise SystemExit(f'Failed, skipped or missing results: {name}')
    for row in rows:
        if row.get('result') != 'Pass' or not all(row.get(key) for key in ['type', 'method', 'name']):
            raise SystemExit(f'Failed or malformed case: {name}')
        observed[row.get('type') + '.' + row.get('method')] += 1
    print(f'{name}: {total}/{total}, no failures, skips, errors or missing cases')
for method, count in required.items():
    if type(count) is not int or count <= 0 or observed[method] != count:
        raise SystemExit(f'Missing required passing method/case count: {method}')
print(f'All {sum(required.values())} required added cases passed')
PY
JOB
