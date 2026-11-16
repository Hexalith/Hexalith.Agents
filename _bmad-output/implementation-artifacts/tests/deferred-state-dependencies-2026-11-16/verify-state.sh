#!/usr/bin/env bash
set -euo pipefail
root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../../.." && pwd)
if [[ $# == 0 ]]; then
    evidence=$(mktemp -d "$root/_bmad-output/implementation-artifacts/tests/deferred-state-dependencies-2026-11-16/state-run.XXXXXXXX")
elif [[ $# == 2 && $1 == --evidence ]]; then
    evidence=$(realpath -m -- "$2")
    case "$evidence/" in "$TMPDIR/"*) echo 'Evidence must survive outside TMPDIR.' >&2; exit 2;; esac
    mkdir -p -- "$(dirname -- "$evidence")"
    mkdir -- "$evidence"
else
    echo 'Usage: verify-state.sh [--evidence /fresh/durable/directory]' >&2
    exit 2
fi
printf 'State verification evidence: %s\n' "$evidence"
record_native() {
    python3 - "$evidence" "$1" "$2" <<'PY'
import hashlib,json,pathlib,sys,xml.etree.ElementTree as ET
root,assembly,xml=map(pathlib.Path,sys.argv[1:])
parsed=ET.parse(xml).getroot();owners=parsed.findall('assembly')
if len(owners)!=1 or pathlib.Path(owners[0].get('name','')).resolve()!=assembly.resolve():
    raise SystemExit('Native output is not the owning assembly.')
owner=owners[0];rows=owner.findall('.//test')
if not rows or any(row.get('result')!='Pass' for row in rows) or parsed.findall('.//error') \
        or any(int(owner.get(key,'-1')) for key in ('errors','failed','skipped','not-run')) \
        or int(owner.get('passed','-1'))!=len(rows) or int(owner.get('total','-1'))!=len(rows):
    raise SystemExit('Native execution is incomplete or failed.')
path=root/'native-results.json';data=json.loads(path.read_text()) if path.exists() else {}
data[assembly.stem]={'assemblyPath':str(assembly.resolve()),'assemblySha256':hashlib.sha256(assembly.read_bytes()).hexdigest(),
    'xml':xml.name,'xmlSha256':hashlib.sha256(xml.read_bytes()).hexdigest(),'passed':len(rows)}
path.write_text(json.dumps(data,indent=2)+'\n')
PY
}
finish() {
    result=$?
    python3 - "$root" "$evidence" "$result" <<'PY'
import gzip,hashlib,json,os,pathlib,sys
root,evidence=map(pathlib.Path,sys.argv[1:3]);sha=lambda data:hashlib.sha256(data).hexdigest()
paths=['src/Hexalith.Agents.UI/State/BrowserSessionPendingProviderCommandStore.cs',
       'src/Hexalith.Agents.UI/wwwroot/pendingProviderCommands.js','test/Hexalith.Agents.UI.Tests/PendingProviderCommandStoreTests.cs',
       'test/Hexalith.Agents.UI.Tests/InMemorySessionStorageJsRuntime.cs','tests/tooling/pending_provider_storage_test.mjs',
       'references/Hexalith.Platform/src/Hexalith.Platform.Custody/SecuritySpoolSnapshot.cs',
       'references/Hexalith.Platform/src/Hexalith.Platform.Custody/SecuritySpoolSnapshotJsonConverter.cs',
       'references/Hexalith.Platform/src/Hexalith.Platform.Custody/ReplicatedSecurityObservationSpool.cs',
       'references/Hexalith.Platform/tests/Hexalith.Platform.Custody.Tests/ReplicatedSecurityObservationSpoolTests.cs',
       '_bmad-output/implementation-artifacts/tests/deferred-state-dependencies-2026-11-16/verify-state.sh']
for path in evidence.glob('*.log'):
    path.with_suffix(path.suffix+'.gz').write_bytes(gzip.compress(path.read_bytes(),mtime=0))
(evidence/'bindings.json').write_text(json.dumps({'exitCode':int(sys.argv[3]),'stateSuites':os.environ.get('STATE_SUITES','all'),
    'spoolFilters':os.environ.get('SPOOL_FILTERS',''),'sources':{path:sha((root/path).read_bytes()) for path in paths},
    'outputs':{p.name:sha(p.read_bytes()) for p in evidence.iterdir() if p.is_file()}},indent=2)+'\n')
PY
}
trap finish EXIT
if [[ "${STATE_SUITES:-all}" != UI ]]; then
    mkdir -p "$TMPDIR/state/references"
    for owner in Platform Builds EventStore Commons; do
        rsync -a --exclude=.git --exclude=bin --exclude=obj --exclude=node_modules --exclude=_bmad-output --exclude=_bmad --exclude=references "$root/references/Hexalith.$owner/" "$TMPDIR/state/references/Hexalith.$owner/"
    done
    cd "$TMPDIR/state/references/Hexalith.Platform"
    dotnet build tests/Hexalith.Platform.Custody.Tests/Hexalith.Platform.Custody.Tests.csproj -c Debug -m:1 -p:MinVerVersionOverride=1.0.0 > "$evidence/spool-isolated-build.log" 2>&1
    assembly="$PWD/tests/Hexalith.Platform.Custody.Tests/bin/Debug/net11.0/Hexalith.Platform.Custody.Tests.dll"
    dotnet "$assembly" ${SPOOL_FILTERS:-} -failSkips -result-xml "$evidence/spool-all.xml" > "$evidence/spool-all.log" 2>&1
    record_native "$assembly" "$evidence/spool-all.xml"
fi
cd "$root"
node --test tests/tooling/pending_provider_storage_test.mjs > "$evidence/browser-module.log" 2>&1
rsync -a --exclude=.git --exclude=.artifacts --exclude=bin --exclude=obj --exclude=node_modules --exclude=_bmad-output --exclude=_bmad --exclude=references --exclude=.agents --exclude=.github --exclude=.codex --exclude=.claude --exclude=docs "$root/" "$TMPDIR/ui-repo/"
mkdir -p "$TMPDIR/ui-repo/references"
for owner in Builds EventStore Commons FrontComposer Conversations Parties Tenants; do
    rsync -a --exclude=.git --exclude=bin --exclude=obj --exclude=node_modules --exclude=_bmad-output --exclude=_bmad --exclude=references --exclude=docs "$root/references/Hexalith.$owner/" "$TMPDIR/ui-repo/references/Hexalith.$owner/"
done
cd "$TMPDIR/ui-repo"
dotnet build test/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj -c Debug -m:1 -p:MinVerVersionOverride=1.0.0 > "$evidence/ui-all-build.log" 2>&1
assembly="$PWD/test/Hexalith.Agents.UI.Tests/bin/Debug/net11.0/Hexalith.Agents.UI.Tests.dll"
dotnet "$assembly" -failSkips -result-xml "$evidence/ui-all.xml" > "$evidence/ui-all.log" 2>&1
record_native "$assembly" "$evidence/ui-all.xml"
