---
title: '5.2 Correlate Setup Writes With Their Exact Projected Outcome'
type: 'feature'
created: '2026-09-14'
status: 'done'
route: 'dispatch'
baseline_commit: '599208dd40efadef728363c227a0f75ebd888337'
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
- [x] `src/Hexalith.Agents/Agent/` -- emit bounded applied/no-op result payloads for every live setup mutation.
- [x] `src/Hexalith.Agents.Contracts/`, `Server/`, and `Client/` -- preserve canonical receipts and expose only identity, target version, and safe effect.
- [x] `src/Hexalith.Agents.UI/` -- implement retained-key exact-intent handling, bounded catch-up, no-op termination, and accessible recovery.
- [x] `test/` and `eng/verify-story-5.2.ps1` -- prove the matrix, no-disclosure, compatible serialization, replay, and persisted end state.

**Acceptance Criteria:**
- Given an applied setup command, when EventStore completes it, then public acceptance carries its canonical receipt and authoritative resulting version.
- Given no-op, rejection, malformed legacy payload, lost acknowledgement, or concurrent write, when handled, then the attempt terminates or remains fail-closed without false confirmation or duplication.
- Given projection lag, when bounded polling expires, then localized `awaiting projection` retains the attempt and exposes accessible recovery.

## Implementation Notes

## Spec Change Log

- 2026-09-14: Corrected the verification lanes to use project references only for Debug development builds and package references for Release builds, as required by the loaded repository instructions. Intent and acceptance criteria are unchanged.

## Review Triage Log

| Reviewer | Finding | Verdict | Route | Evidence |
| --- | --- | --- | --- | --- |
| edge-case | A previous terminal/no-op write remains visible while a new attempt is awaiting its receipt. | medium | patch | `SubmitAsync` creates `_activeAttempt` without clearing `_pendingWrite`; after a terminal/no-op result clears only the attempt, a later slow submission can display the earlier outcome as current. |
| edge-case | `AgentSetupWriteResult.Failed` accepts `AlreadyApplied` without acceptance evidence. | low | reject | No production caller passes a success-like status to `Failed`; guarding this hypothetical public-factory misuse would add an invariant branch for a path not exercised by the implementation. |
| edge-case | `ConfigurationVersion + 1` can overflow at `int.MaxValue`. | low | reject | The theoretical overflow predates this story across the aggregate's versioned events and requires more than two billion setup mutations; a new exhaustion policy is disproportionate to this slice. |
| verification-gap | Live setup handlers are not individually verified to emit matching effect/version result payloads. | medium | patch | Only `UpdateAgentConfiguration` currently asserts `ResultPayload`; another handler could retain the correct event version while returning a stale command target and falsely confirm an older projection. |
| verification-gap | The invalid-idempotency test exits at invalid correlation validation first. | medium | patch | No authorized test reaches the message-id branch with a valid correlation ULID and malformed idempotency key, so that guard could regress unnoticed. |
| verification-gap | Manual awaiting-projection refresh is rendered but never exercised. | medium | patch | Tests assert only that the refresh button exists; they do not prove it reads the retained target, avoids resubmission, and clears recovery after confirmation. |
| verification-gap | Response-mode retry does not verify retention of the original mode. | medium | patch | Exact payload/ULID retry is tested only for configuration updates; closing over mutable `_responseMode` would reuse one key for two intents without failing current tests. |
| verification-gap | Real localization tests omit the new configuration recovery keys. | low | patch | Component tests use a permissive stub, while real-resource enumeration does not resolve `Agents.Config.Write.*` or the three recovery actions in both languages. |
| verification-gap-other | Manual refresh overwrites unsaved drafts after polling exhaustion. | medium | patch | `RefreshPendingAsync` reaches `RefreshAfterWriteAsync`, whose first read applies authoritative drafts; editable draft fields can therefore be silently reset by the new recovery action. |

## Design Notes

`DomainResult.ResultPayload` already crosses EventStore only for completed success/no-op results and participates in idempotency replay. Carry only effect and resulting version; the gateway response owns message/correlation identity.

A later projected version confirms a command-derived target because ordered projection cannot skip that earlier event. No-op completes without polling; missing payload remains `UnableToVerify`.

## Verification

**Commands:**
- `pwsh ./eng/verify-story-5.2.ps1` -- expected: all named Story 5.2 domain, contract, server, UI, replay, and persisted read-model gates pass.
- `dotnet build Hexalith.Agents.slnx --configuration Debug -warnaserror -p:UseHexalithProjectReferences=true -p:NuGetAudit=false` -- expected: warning-free source-mode development build with additive contracts.
- `dotnet build Hexalith.Agents.slnx --configuration Release -warnaserror -p:UseHexalithProjectReferences=false -p:NuGetAudit=false` -- expected: warning-free package-mode release build.
- `git diff --check` in each owning repository -- expected: no whitespace errors.
