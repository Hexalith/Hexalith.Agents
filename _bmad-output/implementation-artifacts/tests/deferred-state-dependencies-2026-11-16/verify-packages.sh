#!/usr/bin/env bash
set -euo pipefail
root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../../.." && pwd)
evidence=$(realpath -m -- "${1:?Supply a fresh durable evidence directory.}")
case "$evidence/" in "$TMPDIR/"*) echo 'Evidence must survive outside TMPDIR.' >&2; exit 2;; esac
mkdir -p -- "$(dirname -- "$evidence")"
mkdir -- "$evidence"
printf 'Package verification evidence: %s\n' "$evidence"
mkdir -p "$TMPDIR/packages-repo/references"
rsync -a --exclude=.git --exclude=references --exclude=.artifacts --exclude=bin --exclude=obj --exclude=node_modules --exclude=_bmad-output --exclude=_bmad --exclude=docs --exclude=.agents --exclude=.github --exclude=.codex --exclude=.claude "$root/" "$TMPDIR/packages-repo/"
rsync -a --exclude=.git --exclude=references --exclude=bin --exclude=obj --exclude=node_modules --exclude=_bmad-output --exclude=_bmad "$root/references/Hexalith.Builds/" "$TMPDIR/packages-repo/references/Hexalith.Builds/"
cd "$TMPDIR/packages-repo"
python3 - "$TMPDIR/packages" "$evidence" <<'PY'
import hashlib,json,pathlib,subprocess,sys,zipfile
packages,evidence=map(pathlib.Path,sys.argv[1:]);packages.mkdir()
for entry in json.loads(pathlib.Path('eng/release-packages.json').read_text())['packages']:
    command=['dotnet','pack',entry['project'],'-c','Release','-m:1','-p:MinVerVersionOverride=1.0.0','-o',str(packages)]
    with (evidence/(entry['id']+'.log')).open('wb') as log:
        subprocess.run(command,stdout=log,stderr=subprocess.STDOUT,check=True)
with (evidence/'package-validation.log').open('wb') as log:
    subprocess.run(['python3','scripts/validate-nuget-packages.py',str(packages)],stdout=log,stderr=subprocess.STDOUT,check=True)
with (evidence/'consumer-validation.log').open('wb') as log:
    subprocess.run(['python3','scripts/validate-consumer-package-references.py',str(packages),'--receipt',str(evidence/'consumer-receipt.json')],stdout=log,stderr=subprocess.STDOUT,check=True)
module=pathlib.Path('src/Hexalith.Agents.UI/wwwroot/pendingProviderCommands.js').read_bytes()
with zipfile.ZipFile(packages/'Hexalith.Agents.UI.1.0.0.nupkg') as archive:
    assert archive.read('staticwebassets/pendingProviderCommands.js')==module
data={'version':'1.0.0','published':False,'moduleSha256':hashlib.sha256(module).hexdigest(),
      'packages':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in packages.glob('*.nupkg')},
      'evidenceSha256':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in evidence.iterdir() if p.is_file()}}
(evidence/'package-verification.json').write_text(json.dumps(data,indent=2)+'\n')
print('All six packages and seven isolated consumers passed; the UI module bytes match source.')
PY
