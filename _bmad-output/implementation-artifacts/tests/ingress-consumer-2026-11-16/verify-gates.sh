#!/usr/bin/env bash
set -uo pipefail
root=$(pwd)
logs="$root/artifacts/five-ingress-gates"
mkdir -p "$logs" "$TMPDIR/repo"
rsync -a --exclude=.git --exclude=bin --exclude=obj --exclude=_bmad-output --exclude=_bmad --exclude=node_modules --exclude=artifacts --exclude=.agents --exclude=.codex --exclude=.claude "$root/" "$TMPDIR/repo/" || exit 1
mkdir -p "$TMPDIR/repo/_bmad-output/implementation-artifacts" "$TMPDIR/repo/_bmad-output/planning-artifacts"
cp "$root/_bmad-output/implementation-artifacts/4-5-governance-conformance-report.md" "$TMPDIR/repo/_bmad-output/implementation-artifacts/"
cp "$root/_bmad-output/planning-artifacts/external-dependency-register.md" "$TMPDIR/repo/_bmad-output/planning-artifacts/"
cd "$TMPDIR/repo" || exit 1
pwsh -NoProfile -File eng/verify-story-5.2.ps1 -Configuration Release > "$logs/story-5.2.log" 2>&1
story52=$?
printf 'Story 5.2 Release exit: %s\n' "$story52"
pwsh -NoProfile -File eng/verify-story-6.1.ps1 -DependencyMode Package -ArtifactsPath "$logs/story-6.1" > "$logs/story-6.1.log" 2>&1
story61=$?
printf 'Story 6.1 Package exit: %s\n' "$story61"
# Required exact-case evidence for the new matrix, alongside maintained full owning suites.
dotnet test/Hexalith.Agents.Server.Tests/bin/Release/net11.0/Hexalith.Agents.Server.Tests.dll -noLogo -failSkips -class '*AutomaticInteractionIntakeRecoveryTests' -class '*ProviderCatalogCoordinationIntegrationTests' -class '*VerifiedProviderQueryCallerTests' -class '*AgentsEventStoreGatewayIntegrationTests' -result-xml "$logs/ingress-matrix.xml" > "$logs/ingress-matrix.log" 2>&1
matrix=$?
printf 'Ingress matrix exit: %s\n' "$matrix"
# Final policy enforcement against freshly produced exact packages and seven new isolated consumers.
while read -r project; do
    dotnet pack "$project" -c Release -o "$TMPDIR/feed" -m:1 -nr:false -p:MinVerVersionOverride=1.0.0 >> "$logs/pack.log" 2>&1 || exit 1
done < <(python3 -c 'import json;print("\n".join(item["project"] for item in json.load(open("eng/release-packages.json"))["packages"]))')
python3 scripts/validate-consumer-package-references.py "$TMPDIR/feed" --receipt "$logs/consumer-receipt.json" > "$logs/consumers.log" 2>&1
consumers=$?
printf 'Seven enforced consumers exit: %s\n' "$consumers"
[ "$story52" -eq 0 ] && [ "$story61" -eq 0 ] && [ "$matrix" -eq 0 ] && [ "$consumers" -eq 0 ]
