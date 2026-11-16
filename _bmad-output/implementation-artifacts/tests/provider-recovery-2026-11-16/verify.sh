#!/usr/bin/env bash
set -uo pipefail
overall=0
root=$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../../../.." && pwd) || exit 1
logs="$root/_bmad-output/implementation-artifacts/tests/provider-recovery-2026-11-16"
mkdir -p "$TMPDIR/repo" || exit 1
rsync -a --exclude=.git --exclude=bin --exclude=obj --exclude=_bmad-output --exclude=_bmad --exclude=node_modules --exclude=.agents --exclude=.codex --exclude=.claude "$root/" "$TMPDIR/repo/" || exit 1
mkdir -p "$TMPDIR/repo/_bmad-output/implementation-artifacts" || exit 1
cp "$root/_bmad-output/implementation-artifacts/4-5-governance-conformance-report.md" "$TMPDIR/repo/_bmad-output/implementation-artifacts/" || exit 1
cd "$TMPDIR/repo" || exit 1
for project in ${PROVIDER_RECOVERY_PROJECTS:-UI Server Client Contracts}; do
    dotnet build "test/Hexalith.Agents.$project.Tests/Hexalith.Agents.$project.Tests.csproj" -c Debug -m:1 -p:MinVerVersionOverride=1.0.0 > "$logs/source-$project.log" 2>&1
    result=$?
    printf '%s source build exit: %s\n' "$project" "$result"
    [ "$result" -eq 0 ] || overall=1
    if [ "$result" -eq 0 ]; then
        dotnet "test/Hexalith.Agents.$project.Tests/bin/Debug/net11.0/Hexalith.Agents.$project.Tests.dll" -noLogo -failSkips -result-xml "$logs/source-$project.xml" > "$logs/source-$project-tests.log" 2>&1
        result=$?
        printf '%s source tests exit: %s\n' "$project" "$result"
        [ "$result" -eq 0 ] || overall=1
    elif [ "$project" = Server ]; then
        dotnet build "test/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj" -c Release -m:1 -p:MinVerVersionOverride=1.0.0 > "$logs/package-Server.log" 2>&1
        result=$?
        printf 'Server package build exit: %s\n' "$result"
        [ "$result" -eq 0 ] || overall=1
        if [ "$result" -eq 0 ]; then
            dotnet "test/Hexalith.Agents.Server.Tests/bin/Release/net11.0/Hexalith.Agents.Server.Tests.dll" -noLogo -failSkips -result-xml "$logs/package-Server.xml" > "$logs/package-Server-tests.log" 2>&1
            result=$?
            printf 'Server package tests exit: %s\n' "$result"
            [ "$result" -eq 0 ] || overall=1
        fi
    fi
done

exit "$overall"
