"""Attempt the maintained producer mapping in an exact local disposable cohort."""
import importlib.util
import json
import os
from pathlib import Path
import subprocess
import sys
root = Path.cwd()
spec = importlib.util.spec_from_file_location('cohort', root / 'scripts/verify-deferred-dependency-cohort.py')
m = importlib.util.module_from_spec(spec); spec.loader.exec_module(m)
evidence = root / 'artifacts/five-ingress-source-preparation'
evidence.mkdir(exist_ok=True)
experiment = m.Experiment('mapped', Path(os.environ['TMPDIR']), evidence)
try:
    head = m.git(root, 'rev-parse', 'HEAD').decode().strip()
    platform = root / 'references/Hexalith.Platform'
    platform_head = m.git(platform, 'rev-parse', 'HEAD').decode().strip()
    selected = m.pins(platform, platform_head)
    selected.pop('Hexalith.Agents', None)
    direct = m.pins(root, head)
    # Producer-selected extra owner roots stay exact; all root-declared dependency pins retain authority.
    selected.update(direct)
    repo = experiment.cohort('Hexalith.Agents', head, selections=selected)
    subprocess.run(['rsync', '-a', '--exclude=.git', '--exclude=references', '--exclude=artifacts',
        '--exclude=bin', '--exclude=obj', '--exclude=_bmad', '--exclude=_bmad-output', '--exclude=node_modules',
        str(root) + '/', str(repo) + '/'], check=True)
    _, _, environment = experiment.prepare(repo)
    experiment.run('server-source-build', ['dotnet', 'build', 'test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj',
        '-c', 'Debug', '-m:1', '-p:MinVerVersionOverride=1.0.0'], repo, environment)
    assembly = repo / 'test/Hexalith.Agents.Server.Tests/bin/Debug/net11.0/Hexalith.Agents.Server.Tests.dll'
    experiment.tests('server-source-tests', assembly, repo, environment=environment, minimum=1500)
    experiment.record['status'] = 'passed'
except Exception as error:
    experiment.record['blocker'] = str(error)
    print(str(error), file=sys.stderr)
finally:
    experiment.save()
raise SystemExit(0 if experiment.record['status'] == 'passed' else 1)
