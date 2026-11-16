"""Run the actual CiCd reachability checker on a frozen working-tree snapshot."""
import ast
import hashlib
import json
import os
from pathlib import Path
import subprocess
import sys

root = Path.cwd()
controller = root.parent / 'cicd'
revision = subprocess.check_output(['git', '-C', str(controller), 'rev-parse', 'HEAD'], text=True).strip()
trusted = Path(os.environ['TMPDIR']) / 'trusted-cicd-tools'
trusted.mkdir()
checker_hashes = {}
for name in ('scan_discovery.py', 'delivery_runtime.py'):
    contents = subprocess.check_output(['git', '-C', str(controller), 'show', revision + ':tools/' + name])
    (trusted / name).write_bytes(contents)
    checker_hashes['tools/' + name] = hashlib.sha256(contents).hexdigest()
sys.path.insert(0, str(trusted))
from scan_discovery import Discovery

class WorkingTreeSnapshot:
    def __init__(self):
        self.rows = {}
        for line in subprocess.check_output(['git', 'ls-tree', '-r', 'HEAD'], text=True).splitlines():
            header, path = line.split('\t')
            mode, kind, oid = header.split()
            if (root / path).exists():
                self.rows[path] = (mode, kind, oid)
        for path in subprocess.check_output(['git', 'diff', '--name-only', 'HEAD'], text=True).splitlines():
            if (root / path).is_file():
                self.rows[path] = ('100644', 'blob', 'working-tree')
        self.blobs = {path: (root / path).read_bytes() for path, (_, kind, _) in self.rows.items() if kind == 'blob'}
    def items(self, revision):
        return self.rows
    def blob(self, revision, path):
        return self.blobs[path]

snapshot = WorkingTreeSnapshot()
selected, errors = Discovery(snapshot, 'working-tree').discover()
calls = []
for node in ast.walk(ast.parse(snapshot.blobs['scripts/check-built-packages.py'])):
    if isinstance(node, ast.Call) and ast.unparse(node.func) == 'subprocess.run':
        calls.append({'line': node.lineno, 'argv': ast.literal_eval(node.args[0])})
report = {
    'checker': 'scan_discovery.Discovery.discover (actual CiCd coverage checker, pinned source)',
    'controllerRevision': revision,
    'checkerSourceSha256': checker_hashes,
    'moduleRevision': subprocess.check_output(['git', 'rev-parse', 'HEAD'], text=True).strip(),
    'snapshot': 'HEAD tree plus current tracked working-tree file bytes; no commit or owner policy mutation',
    'selected': sorted(selected),
    'selectedSourceSha256': {path: hashlib.sha256(snapshot.blobs[path]).hexdigest() for path in sorted(selected)},
    'literalHookCommands': sorted(calls, key=lambda row: row['line']),
    'errors': errors,
    'notRun': 'The full network cross-module duplicate detector is outside this focused source-discovery check.'
}
print(json.dumps(report, indent=2))
raise SystemExit(bool(errors))
