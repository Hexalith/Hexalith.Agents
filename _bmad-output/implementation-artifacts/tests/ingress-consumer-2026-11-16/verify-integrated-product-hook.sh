#!/usr/bin/env bash
set -uo pipefail
root=$(pwd)
logs="$root/artifacts/${INGRESS_HOOK_LOG_LABEL:-five-ingress-integration-product-hook}"
mkdir -p "$logs" "$TMPDIR/repo"
rsync -a --exclude=references --exclude=.git --exclude=bin --exclude=obj --exclude=_bmad-output --exclude=_bmad --exclude=node_modules --exclude=artifacts --exclude=.agents --exclude=.codex --exclude=.claude "$root/" "$TMPDIR/repo/" || exit 1
ln -s "$root/references" "$TMPDIR/repo/references"
cd "$TMPDIR/repo" || exit 1
while read -r project; do
    dotnet build "$project" -c Release -m:1 -nr:false -p:MinVerVersionOverride=0.0.0-ci-check -p:Version=0.0.0-ci-check -p:PackageVersion=0.0.0-ci-check >> "$logs/built-inputs.log" 2>&1 || { tail -30 "$logs/built-inputs.log"; exit 1; }
done < <(python3 -c 'import json;print("\n".join(item["project"] for item in json.load(open("eng/release-packages.json"))["packages"]))')
python3 -B scripts/check-built-packages.py > "$logs/hook.log" 2>&1
result=$?
if [ -e artifacts/cicd-agents-package-check ]; then
    printf 'Product hook scratch cleanup failed\n' >> "$logs/hook.log"
    result=1
else
    printf 'Product hook scratch cleanup confirmed\n' >> "$logs/hook.log"
fi
tail -25 "$logs/hook.log"
exit "$result"
