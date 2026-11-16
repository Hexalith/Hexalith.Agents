#!/usr/bin/env bash
# Run from the module root through run-with-temp, with the documented host tools on PATH.
set -euo pipefail
verification_root=$(pwd)
verification_logs="$verification_root/artifacts/five-ingress-reproduced-final"
mkdir -p "$verification_logs"
python3 - <<'PY'
import os
from pathlib import Path
import shutil
import subprocess

source = Path.cwd()
target = Path(os.environ['TMPDIR']) / 'repo'
subprocess.run(['git', 'clone', '--quiet', '--shared', '--no-recurse-submodules', str(source), str(target)], check=True)
origin = subprocess.check_output(['git', 'remote', 'get-url', 'origin'], cwd=source, text=True).strip()
subprocess.run(['git', '-C', str(target), 'remote', 'set-url', 'origin', origin], check=True)
patch = subprocess.check_output(['git', 'diff', '--binary', 'HEAD'], cwd=source)
if patch:
    subprocess.run(['git', 'apply', '--binary', '-'], cwd=target, input=patch, check=True)
for name in subprocess.check_output(['git', 'ls-files', '--others', '--exclude-standard', '-z'], cwd=source).decode().split('\0'):
    if name and (source / name).is_file():
        destination = target / name
        destination.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source / name, destination)
for line in subprocess.check_output(['git', 'config', '-f', '.gitmodules', '--get-regexp', 'path'], cwd=source, text=True).splitlines():
    name = line.split(None, 1)[1]
    revision = subprocess.check_output(['git', '-C', str(source / name), 'rev-parse', 'HEAD'], text=True).strip()
    subprocess.run(['git', 'clone', '--quiet', '--shared', '--no-recurse-submodules', str(source / name), str(target / name)], check=True)
    subprocess.run(['git', '-C', str(target / name), 'checkout', '--quiet', '--detach', revision], check=True)
    origin = subprocess.check_output(['git', '-C', str(source / name), 'remote', 'get-url', 'origin'], text=True).strip()
    subprocess.run(['git', '-C', str(target / name), 'remote', 'set-url', 'origin', origin], check=True)
PY
cd "$TMPDIR/repo"
pwsh -NoProfile -File eng/verify-eventstore-package-floor.ps1 > "$verification_logs/floor.log" 2>&1
pwsh -NoProfile -File eng/verify-story-6.1.ps1 -DependencyMode Package -ArtifactsPath "$verification_logs/native" > "$verification_logs/native.log" 2>&1
dotnet build Hexalith.Agents.slnx -c Release -m:1 -nr:false -p:UseHexalithProjectReferences=false -p:MinVerVersionOverride=1.0.0 > "$verification_logs/solution-build.log" 2>&1
python3 -B scripts/check-built-packages.py > "$verification_logs/product-hook.log" 2>&1
python3 -B -m unittest discover -s tests/tooling -p '*_test.py' -v > "$verification_logs/tooling.log" 2>&1
