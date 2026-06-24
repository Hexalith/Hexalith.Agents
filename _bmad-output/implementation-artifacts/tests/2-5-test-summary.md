# Story 2.5 — Post Automatic Responses Through Conversations — Test Summary

**Date:** 2026-06-24
**Story:** `2-5-post-automatic-responses-through-conversations`
**Build:** `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` → **0 warnings / 0 errors**
**Stack:** net10.0, LangVersion 14, nullable, warnings-as-errors, Central Package Management, xUnit v3 3.2.2, Shouldly 4.3.0, NSubstitute 5.3.0 (Server.Tests only).

## Per-project results (`dotnet test … --configuration Release --no-build`)

| Test project | Failed | Passed | Skipped | Total | Duration |
|---|---:|---:|---:|---:|---:|
| Hexalith.Agents.Tests (domain) | 0 | 527 | 0 | 527 | 158 ms |
| Hexalith.Agents.Contracts.Tests | 0 | 175 | 0 | 175 | 179 ms |
| Hexalith.Agents.Server.Tests | 0 | 232 | 0 | 232 | 198 ms |
| Hexalith.Agents.UI.Tests | 0 | 156 | 0 | 156 | 655 ms |
| **Total** | **0** | **1090** | **0** | **1090** | — |

All projects tested individually (not solution-wide). No VSTest `SocketException` fallback was needed in this run.

## New / extended tests for Story 2.5

### `Hexalith.Agents.Tests` — `AgentInteractionPostingAggregateTests` (new)
- Posted outcome → `AgentResponsePosted` + status `Posted`; records the safe `AgentPostedMessageEvidence` (ids only).
- Each failure outcome (`PartyIdentityUnavailable`, `MembershipUnavailable`, `MembershipRejected`, `ConversationUnavailable`, `PostRejected`, `AdapterFailure`, `Unknown`→`AdapterFailure`) → `AgentResponsePostingFailed` with the mapped reason + status `PostingFailed`.
- Not-postable rejections: never-requested / rejection-only stream → `InteractionNotRequested`; **Confirmation mode** → `NotAutomaticResponseMode`; every non-`Generated` Automatic status (Requested/Authorized/ContextReady/ContextBlocked/GenerationFailed/SafetyFailed) → `OutputNotGenerated`.
- Terminal idempotency (AC3): re-post after `Posted` and after `PostingFailed` → `DomainResult.NoOp()`, no flip, no second message.
- `Evaluate`/`Decide` no-drift theory across every outcome.
- Full reflection-dispatch + JSON round-trip through `ProcessAsync` for the success and failure paths.

### `Hexalith.Agents.Tests` — `AgentInteractionStateReplayTests` (extended)
- `Apply(AgentResponsePosted)` transitions to `Posted` + records evidence; request payload untouched (AD-14).
- `Apply(AgentResponsePostingFailed)` records `PostingFailed` + reason + attempted evidence.
- `Apply(AgentResponseNotPostableRejection)` is a replay-safe no-op.
- Posting outcomes apply only over a requested stream (IsRequested guard); deterministic across independent rebuilds.

### `Hexalith.Agents.Contracts.Tests` — `AgentInteractionPostingContractsTests` (new)
- Marker interfaces: `AgentResponsePosted`/`AgentResponsePostingFailed` are `IEventPayload` (not rejections); `AgentResponseNotPostableRejection` is `IRejectionEvent`; `PostAgentResponse` has no marker / no Hexalith-namespaced attribute.
- JSON round-trip for the evidence, result, command, both events, and the rejection.
- Enums serialize by name and default to `Unknown` (`AgentResponsePostingOutcome`, `AgentResponsePostingFailureReason`, `AgentResponseNotPostableReason`).
- `AgentInteractionStatus` ordinals 0–9 unperturbed; `Posted` = 10, `PostingFailed` = 11; both serialize by name.
- AD-14 no-leak: serializing every posting surface (`AgentResponsePosted`/`…Failed`/`PostAgentResponse`/`AgentResponsePostingResult`/`AgentPostedMessageEvidence`/rejection) contains no generated-content sample string; structural "no content-bearing member" guard. The assembly-wide `ContractsSecretNonDisclosureTests` auto-covers the new types.

### `Hexalith.Agents.Server.Tests` — `AgentInteractionPostingOrchestratorTests` (new, NSubstitute)
- Happy path dispatches `PostAgentResponse` (domain `agent-interaction`, correct `AggregateId`), author = the Agent `PartyId`, deterministic `MessageId`/idempotency key derived from interaction + version; returns `Posted`.
- Content never appears in the dispatched envelope or returned outcome; it is handed only to the poster's append.
- Membership `SeamUnavailable` → `PostingFailed`/`MembershipUnavailable` and **append never called**; membership rejected → `MembershipRejected`.
- Party not-available → `PartyIdentityUnavailable` (version/membership/append never reached); version not-available → `AdapterFailure` (append never reached).
- Append rejected → `PostRejected`; append throws → fail-closed `AdapterFailure` (no raw error / no content leak); membership throws → `ConversationUnavailable` (no leak).
- All-deferred default graph fails closed → `PostingFailed`/`PartyIdentityUnavailable` (first deferred seam).
- Retried post reuses the same deterministic `MessageId`/idempotency key.
- Reserved trust extensions stripped, benign preserved; envelope scope (tenant/message/correlation/user/causation) from the request.
- Confirmation-mode request short-circuits without dispatch (returns unchanged `Generated`, reads/posts nothing).
- End-to-end: the dispatched command drives the real aggregate to `AgentResponsePosted` (status agrees — shared policy, no drift).
- Genuine `OperationCanceledException` propagates; no command dispatched.

### `Hexalith.Agents.Server.Tests` — `ConversationClientResponsePosterTests` (live Conversations adapter)
- Membership verify type-discrimination (AC1): Agent present as `AiAgent` → `Present`; same Party present only as `Human`, or a *different* Party typed `AiAgent`, or no participants → fail closed `SeamUnavailable` (no public establish seam).
- `PartyId` match is case/whitespace-insensitive (closes the near-identical-id substitution bypass; AD-7).
- Trust-bearing gate (AC1; AD-7): a `Visible`-but-stale projection fails closed to `ConversationUnavailable` **even when the Agent is present** — membership is not authorized from a stale participant list (Senior Developer Review fix).
- Hidden read / failure result / thrown read → fail closed `ConversationUnavailable` without propagating; genuine cancellation propagates.
- Append (AC2, AC3): authored by the Agent `PartyId` with the deterministic `MessageId`/idempotency key, tenant-scoped; rejected → `PostRejected`; thrown → `AdapterFailure` (no error/content leak); cancellation propagates.

## Structural / contract guard tests (regression — all green)

`ProjectReferenceDirectionTests`, `ModuleLayout`, `PublicContractPackageBoundaryTests`, `StructuralSeedConformanceTests`, `PackageVersionCentralizationTests`, `BuildContractConformanceTests` (all in `Hexalith.Agents.Server.Tests`) pass — Contracts stays inward-only (no `Hexalith.Conversations.*`/`Hexalith.Parties.*` reference; Conversations types appear only in the Server adapter), `.slnx` only, SDK pinned, package versions centralized.
