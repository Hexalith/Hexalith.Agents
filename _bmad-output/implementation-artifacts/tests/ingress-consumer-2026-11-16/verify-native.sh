#!/usr/bin/env bash
set -uo pipefail
root=$(pwd)
logs="$root/artifacts/five-ingress-final-native"
mkdir -p "$logs" "$TMPDIR/repo"
rsync -a --exclude=references --exclude=.git --exclude=bin --exclude=obj --exclude=_bmad-output --exclude=_bmad --exclude=node_modules --exclude=artifacts --exclude=.agents --exclude=.codex --exclude=.claude "$root/" "$TMPDIR/repo/" || exit 1
ln -s "$root/references" "$TMPDIR/repo/references"
mkdir -p "$TMPDIR/repo/_bmad-output/implementation-artifacts"
cp "$root/_bmad-output/implementation-artifacts/4-5-governance-conformance-report.md" "$TMPDIR/repo/_bmad-output/implementation-artifacts/"
cd "$TMPDIR/repo" || exit 1
overall=0
for suite in ${INGRESS_NATIVE_SUITES:-Server UI}; do
    dotnet build "test/Hexalith.Agents.$suite.Tests/Hexalith.Agents.$suite.Tests.csproj" -c Release -m:1 -nr:false -p:MinVerVersionOverride=1.0.0 > "$logs/$suite-build.log" 2>&1
    result=$?
    printf '%s build exit: %s\n' "$suite" "$result"
    if [ "$result" -ne 0 ]; then overall=1; continue; fi
    assembly="test/Hexalith.Agents.$suite.Tests/bin/Release/net11.0/Hexalith.Agents.$suite.Tests.dll"
    if [ "$suite" = Server ]; then
        dotnet "$assembly" -noLogo -failSkips -class '*AutomaticInteractionIntakeRecoveryTests' -class '*ProviderCatalogCoordinationIntegrationTests' -class '*VerifiedProviderQueryCallerTests' -class '*AgentsEventStoreGatewayIntegrationTests' -result-xml "$logs/matrix.xml" > "$logs/matrix.log" 2>&1
        result=$?; printf 'Final matrix exit: %s\n' "$result"; [ "$result" -eq 0 ] || overall=1
    else
        dotnet "$assembly" -noLogo -failSkips -method 'Hexalith.Agents.UI.Tests.ProviderCatalogGovernanceUiTests.Pending_enablement_for_one_tenant_does_not_lock_same_model_for_another' -result-xml "$logs/UI-focused.xml" > "$logs/UI-focused.log" 2>&1
        result=$?; printf 'UI focused retry exit: %s\n' "$result"; [ "$result" -eq 0 ] || overall=1
    fi
    dotnet "$assembly" -noLogo -failSkips -result-xml "$logs/$suite.xml" > "$logs/$suite.log" 2>&1
    result=$?; printf '%s owning suite exit: %s\n' "$suite" "$result"; [ "$result" -eq 0 ] || overall=1
done
exit "$overall"
