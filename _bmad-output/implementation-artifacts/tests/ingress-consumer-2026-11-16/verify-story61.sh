#!/usr/bin/env bash
set -uo pipefail
root=$(pwd)
logs="$root/artifacts/five-ingress-story61-final"
mkdir -p "$logs" "$TMPDIR/repo"
rsync -a --exclude=references --exclude=.git --exclude=bin --exclude=obj --exclude=_bmad-output --exclude=_bmad --exclude=node_modules --exclude=artifacts --exclude=.agents --exclude=.codex --exclude=.claude "$root/" "$TMPDIR/repo/" || exit 1
ln -s "$root/references" "$TMPDIR/repo/references"
mkdir -p "$TMPDIR/repo/_bmad-output/implementation-artifacts" "$TMPDIR/repo/_bmad-output/planning-artifacts"
cp "$root/_bmad-output/implementation-artifacts/4-5-governance-conformance-report.md" "$TMPDIR/repo/_bmad-output/implementation-artifacts/"
cp "$root/_bmad-output/planning-artifacts/external-dependency-register.md" "$TMPDIR/repo/_bmad-output/planning-artifacts/"
cd "$TMPDIR/repo" || exit 1
pwsh -NoProfile -File eng/verify-story-6.1.ps1 -DependencyMode Package -ArtifactsPath "$logs/evidence" > "$logs/gate.log" 2>&1
result=$?
printf 'Final maintained Story 6.1 Package exit: %s\n' "$result"
tail -18 "$logs/gate.log"
exit "$result"
