"""Refresh the actual SDK Server graph and changed class after concurrent drift."""
import datetime
import hashlib
import json
from pathlib import Path
import subprocess

review = Path(__file__).resolve().parent
base = review.parents[1]
original = base / 'root-revision6-full-local-20261009075941z/command-index.json'
matrix = json.loads(original.read_text())
out = review / 'supplemental-verification'
out.mkdir(exist_ok=False)
artifacts = '/tmp/hexalith-owner-r6-root-server-refresh-20261009t0813z'
if Path(artifacts).exists():
    raise ValueError('Fresh artifacts required')
old_artifacts = '/tmp/hexalith-owner-root-revision6-full-local-20261009075941z-20261008'
records = []
subprocess.run(['python', str(base / 'root-source-snapshot.py'), str(out / 'source-before.json')], check=True)

def run(name, cwd, argv):
    log = out / (str(len(records) + 1) + '.log')
    start = datetime.datetime.now(datetime.timezone.utc).isoformat()
    print(json.dumps({'starting': name, 'log': str(log)}), flush=True)
    with log.open('w') as stream:
        result = subprocess.run(argv, cwd=cwd, stdout=stream, stderr=subprocess.STDOUT, timeout=3600)
    row = dict(name=name, cwd=cwd, argv=argv, startedUtc=start,
               finishedUtc=datetime.datetime.now(datetime.timezone.utc).isoformat(),
               exitCode=result.returncode, invocationLog=str(log))
    records.append(row)
    (out / 'command-index.json').write_text(json.dumps(records, indent=2) + '\n')
    print(json.dumps({'finished': name, 'exitCode': result.returncode}), flush=True)
    if result.returncode:
        raise SystemExit(result.returncode)
    return row

for suffix in ['normal build', 'owned consumer tests']:
    row = next(r for r in matrix['commands'] if r['name'] == 'Hexalith.EventStore.Server.Tests ' + suffix)
    argv = [arg.replace(old_artifacts, artifacts) if arg.startswith(old_artifacts) else arg for arg in row['argv']]
    if suffix == 'owned consumer tests':
        argv[argv.index('-result-xml') + 1] = str(out / 'server-tests.xml')
        argv[argv.index('-result-xml'):argv.index('-result-xml')] = ['-class', 'Hexalith.EventStore.Server.Tests.Security.SecretsProtectionTests']
    executed = run(row['name'], row['cwd'], argv)
    row.update(executed)
    row['buildLogs'] = [executed['invocationLog']] if suffix == 'normal build' else []
    row['xmlFiles'] = [str(out / 'server-tests.xml')] if suffix == 'owned consumer tests' else []
    if suffix == 'owned consumer tests':
        row['requiredClasses'].append('Hexalith.EventStore.Server.Tests.Security.SecretsProtectionTests')
        row['additionalChangedUnownedClass'] = 'Meaningful affected verification for a concurrent edit; not attributed to this build.'
subprocess.run(['python', str(base / 'root-source-snapshot.py'), str(out / 'source-after.json')], check=True)
matrix['originalCommandIndex'] = str(original)
matrix['originalCommandIndexSha256'] = hashlib.sha256(original.read_bytes()).hexdigest()
matrix['supplementalCommandIndex'] = str(out / 'command-index.json')
matrix['scope'] = 'Original complete Local matrix with actual fresh affected Server build/tests plus concurrent SecretsProtectionTests; unrelated drift classified separately. No live qualification.'
(out / 'combined-command-index.json').write_text(json.dumps(matrix, indent=2) + '\n')
