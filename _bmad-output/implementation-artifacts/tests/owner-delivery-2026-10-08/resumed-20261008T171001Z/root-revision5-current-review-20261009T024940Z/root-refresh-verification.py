"""Record normal Host diagnostics and refresh Server after unrelated source drift."""
import datetime
import json
from pathlib import Path
import subprocess

review = Path(__file__).resolve().parent
base = review.parents[1]
original = base / 'root-revision5-full-local-20261009t030010z/command-index.json'
matrix = json.loads(original.read_text())
out = review / 'supplemental-verification'
out.mkdir(exist_ok=False)
artifacts = '/tmp/hexalith-owner-r5-root-refresh-20261009t0318z'
records = []

def run(name, cwd, argv):
    log = out / (str(len(records) + 1) + '.log')
    start = datetime.datetime.now(datetime.timezone.utc).isoformat()
    with log.open('w') as stream:
        result = subprocess.run(argv, cwd=cwd, stdout=stream, stderr=subprocess.STDOUT)
    row = dict(name=name, cwd=cwd, argv=argv, startedUtc=start,
               finishedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
               exitCode=result.returncode, invocationLog=str(log))
    records.append(row)
    (out / 'command-index.json').write_text(json.dumps(records, indent=2) + '\n')
    print(json.dumps(row), flush=True)
    if result.returncode:
        raise SystemExit(result.returncode)
    return row

host = next(r for r in matrix['commands'] if r['name'] == 'Host LocalScaffold')
h = run('Host normal Debug diagnostic build', host['cwd'],
        ['dotnet', 'build', './apphost.cs', '-c', 'Debug', '--artifacts-path', artifacts + '/host',
         '-p:TreatWarningsAsErrors=true', '-p:AspireUseCliBundle=true', '-p:NuGetAudit=false',
         '-p:MinVerVersionOverride=1.0.0', '-v', 'normal'])
host['buildLogs'] = [h['invocationLog']]
host['supplementalDiagnostics'] = h
for suffix in ['normal build', 'owned consumer tests']:
    row = next(r for r in matrix['commands'] if r['name'] == 'Hexalith.EventStore.Server.Tests ' + suffix)
    argv = list(row['argv'])
    for i, arg in enumerate(argv):
        if arg.startswith('/tmp/hexalith-owner-root-revision5-full-local-20261009t030010z-20261008'):
            argv[i] = arg.replace('/tmp/hexalith-owner-root-revision5-full-local-20261009t030010z-20261008', artifacts)
        if arg.endswith('.xml'):
            argv[i] = str(out / 'server-tests.xml')
    executed = run(row['name'], row['cwd'], argv)
    row.update(executed)
    row['buildLogs'] = [executed['invocationLog']] if suffix == 'normal build' else []
    row['xmlFiles'] = [str(out / 'server-tests.xml')] if suffix == 'owned consumer tests' else []
matrix['originalCommandIndex'] = str(original)
matrix['supplementalCommandIndex'] = str(out / 'command-index.json')
matrix['scope'] = 'Original full Local matrix with fresh affected Server build/tests and explicit normal Host diagnostics; no live qualification.'
(out / 'combined-command-index.json').write_text(json.dumps(matrix, indent=2) + '\n')
