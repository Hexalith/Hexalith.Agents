---
title: '5.4 Truthful Platform host qualification gates'
type: 'feature'
created: '2026-10-06'
status: 'in-progress'
route: 'dispatch'
human_approval: 'accepted'
baseline_commit: '3d8eaf400582f0698297d393f4330a54709c9a07'
review_loop_iteration: 0
context:
  - '/home/administrator/projects/hexalith/platform/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/agents/_bmad-output/specs/spec-story-5-4-dependency-unblock/owner-implementation-plan.md'
  - '/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/external-dependency-register.md'
---
<frozen-after-approval reason="authorized prerequisite safety/verification work">

## Intent

Stop a Release build of an empty Platform scaffold from reporting EXT-HOST-1 compatibility PASS. Give the existing script a truthful local Debug scaffold mode and reject requested Agents activation while the full owner composition and current accepted prerequisites are absent. Preserve Works/Identity/admin work. Full H1–H4 remain undelivered.

## Constraints

Only apphost.cs, eng/verify-agents-host.sh, existing docs/ext-host-1-agents-composition.md and separate owner evidence. No new database/service/framework, made-up replicated spool, fake credentials/providers or fixtures presented as production. No domain persistence/Agents aggregate implementation before original entry commitments. No live external consumption, deployment, staging, commits, remote ops or submodule initialization. This is the already-rendered bmad-build handoff; do not render again.

The explicit Agents enabled flag must throw a clear unavailable-composition error before resource creation. Default scaffold and explicit Works lane remain as before. Do not change existing Works profile or insert Identity into unrelated lanes.

## I/O & Edge-Case Matrix

| Input | Expected |
| --- | --- |
| Default/full verification without complete H1–H4 | Nonzero unavailable, no live invocation and no EXT-HOST-1 PASS |
| Explicit LocalScaffold mode | Isolated Debug build succeeds; output says scaffold only, no compatibility/Available claim |
| Unknown script argument | Nonzero before build |
| Platform:Agents:Enabled=true | Startup refuses before resource composition, no empty green Agents host |
| Default Platform startup | Existing scaffold unchanged |
| Explicit Works lane | Source and wiring unchanged |

</frozen-after-approval>

## Code Map

- eng/verify-agents-host.sh currently prints EXT-HOST-1 PASS after apphost.cs Release build and defers full live composition. Default Full mode must deny until actual H1–H4 and acceptance delivered; an explicit --mode LocalScaffold is allowed to build Debug with isolated caller-selectable artifacts path, no arbitrary success wording. No staged fake register may unlock the missing implementation. Preserve ownership checks.
- apphost.cs: add narrow explicit Platform:Agents:Enabled guard immediately after builder creation before Works composition. Full unavailable error is content-free. Do not wire missing providers or rely on default-empty resource success.
- docs/ext-host-1-agents-composition.md: exact runnable modes, partial evidence limits and full prerequisites; do not say S2/key-profile or scaffold alone supplies full H1–H4.
- Separate owner source/evidence notes: source hashes, immutable original baseline/current observed base HEAD, exact commands/logs/outcomes, productionReady=false. List missing replicated spool technology/failure model/retention, private exact replay/recorder/worker credentials, production protection attestation/FR-34, S3/S4 and H3/H4 migration/guard/compromise, owner acceptance.

## Tasks & Acceptance

- [x] Truthful default Full gate and isolated local Debug scaffold build.
- [x] Early explicit Agents activation guard, preserving unrelated composition.
- [x] Execute script syntax, unknown/default Full zero-call checks, LocalScaffold build, and owned isolated AppHost default/enabled startup checks.
- [x] Record exact source/evidence with full owner gaps.

## Verification

Use /tmp/hexalith-agents54-host-artifacts and owned isolated runtime paths. Aspire baseline was already run by root: default resources [], exact owned host safely stopped. Read /home/administrator/.agents/skills/aspire/SKILL.md and routed orchestration skill before lifecycle work. Changes require restarting only an exact owned host, never stopping another user's app. Use CLI resources/status output without dashboard tokens; verify absence/default and refusal/enabled. Do not print secrets. Tests must execute script behavior rather than weaken assertions. Set -e for build-before-run. Build is reversible and authorized; no approval needed. Record environment blocker separately if startup cannot execute; do not claim runtime proof from a text search.

## Implementation Notes

Direct implementation completed local gate tasks: warning-free Debug scaffold build, six zero-invocation negative cases, exact-source isolated default startup with zero application resources, explicit Agents+Works refusal before resource composition and exact owned cleanup. The original Works/Identity block is unchanged. CLI runtime ASPIRE010/developer-certificate warnings are recorded separately. Full H1–H4, production providers, persisted qualification and owner acceptance remain incomplete.

Direct implementation completed local gate tasks: warning-free Debug scaffold build, six zero-invocation negative cases, exact-source isolated default startup with zero application resources, explicit Agents+Works refusal before resource composition and exact owned cleanup. The original Works/Identity block is unchanged. CLI runtime ASPIRE010/developer-certificate warnings are recorded separately. Full H1–H4, production providers, persisted qualification and owner acceptance remain incomplete.
