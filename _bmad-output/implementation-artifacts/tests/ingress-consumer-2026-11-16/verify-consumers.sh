#!/usr/bin/env bash
set -uo pipefail
root=$(pwd)
logs="$root/artifacts/five-ingress-consumers-final"
mkdir -p "$logs"
# Pack only the canonical six inventory projects, retaining Release/package authority.
while read -r project; do
    dotnet pack "$project" -c Release --artifacts-path "$TMPDIR/artifacts" -o "$TMPDIR/feed" -m:1 -nr:false -p:MinVerVersionOverride=1.0.0 >> "$logs/pack.log" 2>&1 || exit 1
done < <(python3 -c 'import json;print("\n".join(item["project"] for item in json.load(open("eng/release-packages.json"))["packages"]))')
python3 scripts/validate-consumer-package-references.py "$TMPDIR/feed" --receipt "$logs/receipt.json" > "$logs/consumers.log" 2>&1
result=$?
tail -20 "$logs/consumers.log"
exit "$result"
