---
title: '5.2 Correlate Setup Writes With Their Exact Projected Outcome'
type: 'feature'
created: '2026-09-14'
status: 'ready-for-dev'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-5-context.md'
  - 'references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-state-instructions.md'
  - 'references/Hexalith.AI.Tools/hexalith-ux-instructions.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Story 5.2 setup writes are confirmed by a locally guessed `ConfigurationVersion + 1`. A no-op can therefore poll forever, a lost acknowledgement cannot be retried safely by the UI, and an unrelated concurrent write can satisfy the guessed threshold.

**Approach:** Return the actual applied/no-op result and resulting configuration version through EventStore's existing `DomainResult.ResultPayload`, preserve the canonical gateway receipt through Agents, and make the UI retain one ULID idempotency key and exact payload until that command-derived target is projected or fails closed.

**Decision (2026-09-14):** Deliver exact write/outcome/projection correlation as the current slice. Defer `ProvisionHexa` and legacy Party link/replace retirement together until Platform authority and `EXT-PARTIES-1` are concrete; do not retire the only current identity path before its replacement exists.

## Boundaries & Constraints

**Always:** Authorize before dispatch/read access; use the canonical EventStore receipt and ULID identities; keep V1 additions backward-compatible with `Unknown = 0`; keep aggregates pure and reads behind `IReadModelStore`; preserve API/client/UI parity, the 250 ms / 8 s catch-up contract, English/French whole strings, and lifecycle-versus-callability separation. A missing legacy result payload is unverifiable.

**Never:** Confirm from a locally predicted version; mint a new key or change payload while an outcome is unresolved; expose infrastructure metadata; add command-status/sequence machinery when existing result-payload/idempotency facilities suffice; change Party identity behavior; initialize nested submodules.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
|----------|---------------|----------------------------|----------------|
| Eventful write | Canonical receipt carries `Applied` and target version | Poll until the projection reaches that version | Missing/malformed payload is `UnableToVerify` |
| No-op | Receipt carries `AlreadyApplied` and unchanged version | Stop and render localized no-change outcome | Never poll for an unreachable version |
| Domain rejection | EventStore returns a typed rejection | Preserve prior setup and show the safe mapped failure | Never render Submitted or Success |
| Lost acknowledgement | UI retains one key and payload | Retry only that pair for idempotent replay | Block different submission until resolved/abandoned |
| Concurrent write | Another command advances projection | Confirm only against the original command-derived target | Local N+1 is forbidden |
| Catch-up exhaustion | Target absent after 8 seconds | Keep attempt; show `awaiting projection` and refresh/retry | Never imply Success |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Agents/Agent/AgentAggregate.cs` plus a focused result type -- enrich live setup success/no-op `DomainResult`s with effect and authoritative version; keep rejections unchanged.
- `src/Hexalith.Agents.Server/Ports/{IAgentCommandDispatcher,EventStoreAgentCommandDispatcher,IAgentCommandIdentityFactory}.cs` -- return/validate the canonical `SubmitCommandResponse`; replace GUID identities with ULIDs.
- `src/Hexalith.Agents.Server/Application/Agents/` outcome/orchestrator types -- thread one receipt; parse it only at the operations boundary.
- `src/Hexalith.Agents.Contracts/{Operations/AgentCommandAcceptance.cs,Agent/AgentSetupWriteResult.cs}` -- add compatible target-version and effect evidence with `Unknown = 0`.
- `src/Hexalith.Agents.Server/Application/Agents/EventStoreAgentAdministrationOperations.cs` and `Api/AgentsOperationEndpoints.cs` -- validate receipt identity, bind idempotency/correlation metadata, and map unverifiable payloads safely.
- `src/Hexalith.Agents.UI/Services/Gateways/`, `Components/Pages/AgentConfiguration.razor`, and `Resources/AgentsResources*.resx` -- retain exact attempts, terminate no-op, poll for 8 seconds, block conflicts, and provide accessible recovery.
- `test/Hexalith.Agents.{Tests,Contracts.Tests,Server.Tests,UI.Tests}` and `eng/verify-story-5.2.ps1` -- cover payload, receipt, no-op, rejection, concurrency, retry, timeout, disclosure, and persisted end state.

## Tasks & Acceptance

**Execution:**
- [ ] `src/Hexalith.Agents/Agent/` -- emit bounded applied/no-op result payloads for every live setup mutation.
- [ ] `src/Hexalith.Agents.Contracts/`, `Server/`, and `Client/` -- preserve canonical receipts and expose only identity, target version, and safe effect.
- [ ] `src/Hexalith.Agents.UI/` -- implement retained-key exact-intent handling, bounded catch-up, no-op termination, and accessible recovery.
- [ ] `test/` and `eng/verify-story-5.2.ps1` -- prove the matrix, no-disclosure, compatible serialization, replay, and persisted end state.

**Acceptance Criteria:**
- Given an applied setup command, when EventStore completes it, then public acceptance carries its canonical receipt and authoritative resulting version.
- Given no-op, rejection, malformed legacy payload, lost acknowledgement, or concurrent write, when handled, then the attempt terminates or remains fail-closed without false confirmation or duplication.
- Given projection lag, when bounded polling expires, then localized `awaiting projection` retains the attempt and exposes accessible recovery.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

`DomainResult.ResultPayload` already crosses EventStore only for completed success/no-op results and participates in idempotency replay. Carry only effect and resulting version; the gateway response owns message/correlation identity.

A later projected version confirms a command-derived target because ordered projection cannot skip that earlier event. No-op completes without polling; missing payload remains `UnableToVerify`.

## Verification

**Commands:**
- `pwsh ./eng/verify-story-5.2.ps1` -- expected: all named Story 5.2 domain, contract, server, UI, replay, and persisted read-model gates pass.
- `dotnet build Hexalith.Agents.slnx --configuration Release -warnaserror -p:UseHexalithProjectReferences=true` -- expected: warning-free source-mode build with additive contracts.
- `git diff --check` in each owning repository -- expected: no whitespace errors.
