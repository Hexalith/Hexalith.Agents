---
title: '5.4 Conversations client cancellation review fix'
type: 'feature'
created: '2026-10-06'
status: 'in-progress'
route: 'dispatch'
human_approval: 'accepted'
baseline_commit: '9938aa8aa5cfc082de6be3d28eede22957b14a26'
review_loop_iteration: 0
context:
  - '/home/administrator/projects/hexalith/conversations/AGENTS.md'
  - '/home/administrator/projects/hexalith/conversations/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/agents/_bmad-output/implementation-artifacts/spec-5-4-conversations-owner-implementation.md'
---
<frozen-after-approval reason="authorized prerequisite review fix">
## Intent
Finish cancellation handling on the new restricted Conversations client operations. The server provider awaits are already bounded against caller cancellation; the new client HTTP awaits currently still rely on cooperative transports. Preserve all response checks and live closure.
## Constraints
Only ConversationClient's two new SubmitAgent helpers, new client tests/fixtures and owner evidence in sibling Conversations. Do not modify legacy client methods or unrelated commits. No staging, commit, remote, deployment, nested submodules, suppressed checks or new deadline/profile. This is the already-rendered bmad-build handoff; do not render again.
## I/O & Edge-Case Matrix
| Input | Expected |
| --- | --- |
| Command HTTP send never completes; caller cancels | Prompt caller cancellation, no accepted/persisted success |
| Query HTTP send never completes; caller cancels | Prompt cancellation, no content release |
| Response JSON read ignores token | Caller cancellation stops waiting; owned eventual response/body safely disposed |
| Ordinary valid and malformed responses | All previous client response checks and behavior unchanged |
</frozen-after-approval>
## Code Map
- src/Hexalith.Conversations.Client/ConversationClient.cs: SubmitAgentCommandAsync and SubmitAgentQueryAsync: bound SendAsync and ReadFromJsonAsync tasks using WaitAsync(cancellationToken). Preserve cancellation checks and exception handling; safely observe/dispose eventual owned responses after a cancelled noncooperative send. Keep tiny private helper in same type, no framework.
- tests/Hexalith.Conversations.Client.Tests/ConversationAgentClientTests.cs and helper files: meaningful never-completing send and body tests with caller cancellation and a short watchdog; one documented type per file, normal owning format.
- docs/implementation/ext-conv-ai-1-owner-implementation-2026-10-06.md and ext-conv-ai-1-source-evidence-2026-10-06.json: refresh changed source hashes, client XML/build/logs and exact new commands. Preserve original execution HEAD/full 1535-test suite evidence as earlier evidence; do not claim later source/HEAD passed unchanged full solution. No availability or full C4 claim.
## Tasks & Acceptance
- [x] Enforce bounded new-client awaits and safe eventual response disposal.
- [x] Build normal Debug/source-reference client tests; execute focused and full Client assembly, no skip.
- [x] Refresh exact source/evidence and unchanged production blockers.
## Verification
Use existing /tmp/hexalith-agents54-conversations-artifacts, unique client-reviewfix logs/XML. Properties UseHexalithProjectReferences=true, explicit EventStore/Commons/Tenants sibling roots, NuGetAudit=false, MinVerVersionOverride=1.0.0, -c Debug -m:1. The full Client assembly's doc tests need exact built bytes staged under ignored .artifacts/ext-conv-ai-1-tests/Hexalith.Conversations.Client.Tests/debug/run as the existing verifier does. Reuse unchanged server/domain/contracts results explicitly; do not rerun whole solution.


## Root Verification

Reviewed all 79 current selected Conversations source hashes and 24 artifact hashes, including the four changed/new client files; focused XML9 and full Client XML39 pass without failures/skips. Earlier full Contracts618/Domain185/Server698/Client34 evidence remains historical; only unchanged earlier lanes are reused. Full solution was not rerun after this client-only fix. Full C4 and live acceptance remain incomplete.
