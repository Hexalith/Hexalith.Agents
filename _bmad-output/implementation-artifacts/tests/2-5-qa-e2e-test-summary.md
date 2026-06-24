# Story 2.5 — QA E2E Test Automation Summary

**Workflow:** `bmad-qa-generate-e2e-tests`
**Story:** 2-5-post-automatic-responses-through-conversations (status: review)
**Date:** 2026-06-24
**Engineer role:** QA automation (test generation only — no production code changed)

> Story 2.5 is a pure backend event-sourcing feature: the automatic-mode posting step on the
> `AgentInteraction` aggregate (additive `Posted`/`PostingFailed` status, the pure
> `AgentResponsePostingPolicy`, the aggregate's 5th `Handle(PostAgentResponse)`, the in-module
> Agent-Party + selected-version readers, the Conversation posting+membership port — live
> config-gated over `IConversationClient` plus a deferred fail-closed default — and the posting
> orchestrator). The live command dispatch, read-model binding, and durable-owner runtime are
> deferred to later stories, so there is **no API controller or UI surface to drive** in this story.
> The "E2E/automated" coverage is therefore the project's existing tiers — the pure domain pipeline
> (`ProcessAsync` reflection-dispatch + replay), the contract serialization surface, the **live
> Conversations posting/membership adapter**, and the server posting orchestration — plus the
> **cross-seam E2E** that runs the orchestrator's own dispatched command back through the real pure
> aggregate (already present from the dev story, kept green).

## Framework detected

xUnit v3 (`xunit.v3` 3.2.2) + Shouldly 4.3.0 assertions + NSubstitute 5.3.0 mocks — the project's
existing stack. New tests reuse the established fixtures (`AgentInteractionTestData`, the reader-adapter
test's `Details`/`FreshFreshness` builders, the orchestrator test's NSubstitute seam patterns),
snake_case `[Fact]` naming, and Shouldly (no raw `Assert.*`); aggregate/replay tests stay pure
command/state/event tests, NSubstitute only at the server adapter seam. **No new packages were added.**

> VSTest opened its local listener without the `SocketException (13)` earlier stories saw, so the
> standard `dotnet test --no-build` runner was used for all four suites; no fallback to the raw xUnit v3
> executables was needed this run.

## Result

| Test project | Before | After | Δ | Failed | Skipped |
| --- | --- | --- | --- | --- | --- |
| `Hexalith.Agents.Tests` (domain) | 526 | **527** | +1 | 0 | 0 |
| `Hexalith.Agents.Server.Tests` | 218 | **231** | +13 | 0 | 0 |
| `Hexalith.Agents.Contracts.Tests` | 175 | 175 | 0 | 0 | 0 |
| `Hexalith.Agents.UI.Tests` (regression) | 156 | 156 | 0 | 0 | 0 |
| **Total** | 1075 | **1089** | **+14** | 0 | 0 |

Build: `dotnet build Hexalith.Agents.slnx --configuration Release -m:1` → **0 warnings / 0 errors**
(`TreatWarningsAsErrors=true`). All pre-existing structural/guard tests stayed green; every new test is
additive (no existing test was modified or deleted).

## Coverage gaps discovered and closed

The dev-story tests already covered the headline paths: the aggregate's posted / every-failure-outcome /
not-postable (not-requested, not-generated, not-automatic) decision matrix, terminal idempotency for both
`Posted` and `PostingFailed`, the `Decide`/`Evaluate` no-drift theory, the contract round-trips +
ordinal-stability + AD-14 no-content-leak guards, and the **orchestrator's** per-outcome mapping +
fail-closed-on-throw + content-redaction + deterministic-id reuse + confirmation-mode short-circuit +
cross-seam-into-the-real-aggregate. The QA pass walked each acceptance criterion and every non-trivial
branch of the policy / orchestrator / **live adapter** / state replay, and found the following **untested
gaps** — concentrated in the one impure surface the dev story left without a direct test (the live
Conversations poster, which the orchestrator tests stub out) — all now auto-applied:

| # | Gap (untested behavior) | AC / Spine | Tier | Test added |
| --- | --- | --- | --- | --- |
| 1 | The **live `ConversationClientResponsePoster` adapter had no test at all** (the orchestrator tests substitute `IConversationResponsePoster`). Its sibling `ConversationClientContextReader` (2.3) is fully covered — the membership-verify + append mapping over the real `IConversationClient` was the one impure 2.5 surface with 0% direct coverage. | AC1, AC2, AC3 | Server | **`ConversationClientResponsePosterTests.cs` (new, 13 facts)** |
| 1a | Agent present **and typed `AiAgent`** → `Present` (the only proceed case) | AC1 / AD-7 | Server | `Agent_present_as_ai_agent_maps_to_present` |
| 1b | Agent's own `PartyId` present but typed **`Human`** (not `AiAgent`) → fail closed `SeamUnavailable` — the **type discrimination at the heart of AC1**, and there is no public establish seam to upgrade it | AC1 / AD-6, AD-7 | Server | `Agent_present_only_as_a_human_participant_fails_closed_to_seam_unavailable` |
| 1c | A **different** Party is the conversation's `AiAgent`; the Agent's id is absent → `SeamUnavailable` (membership is identity-bound, not type-bound) | AC1 / AD-7 | Server | `A_different_party_typed_ai_agent_does_not_satisfy_the_agent_membership` |
| 1d | An authorized conversation with **no participants** → `SeamUnavailable` (absent → fail closed, never silently proceed) | AC1 / AD-6 | Server | `No_participants_fails_closed_to_seam_unavailable` |
| 1e | `PartyId` **case/whitespace normalization**: a request id differing only by casing/whitespace still matches the recorded participant → `Present` (closes the substitution-bypass where a near-identical id reads as a different identity) | AC1 / AD-7 | Server | `Agent_party_match_is_case_and_whitespace_insensitive` |
| 1f | A **Hidden** read (no details) → fail closed `ConversationUnavailable` (existence never leaks) | AC1 / AD-6, AD-12 | Server | `A_hidden_read_with_no_details_fails_closed_to_conversation_unavailable` |
| 1g | A client **`!IsSuccess`** result → `ConversationUnavailable` | AC1 / AD-12 | Server | `A_failure_client_result_fails_closed_to_conversation_unavailable` |
| 1h | A membership read that **throws** (non-cancellation) → fail closed `ConversationUnavailable`, raw error never crosses the boundary (safe-enum-only result) | AC1 / AD-12, AD-14 | Server | `A_thrown_membership_read_fails_closed_to_conversation_unavailable_without_propagating` |
| 1i | A **genuine cancellation** during membership still propagates (not swallowed by the fail-closed `when (ex is not OperationCanceledException)`) | AC3 | Server | `A_genuine_cancellation_propagates_from_membership` |
| 1j | Append success **builds the `AppendMessageCommand` authored by the Agent Party** (`AuthorPartyId` + `Metadata.ActorPartyId` = agentPartyId, not the caller), with the deterministic `MessageId`/`IdempotencyKey`, tenant scope, correlation, and the sensitive `Text` — the AC2/AC3 boundary contract, previously only asserted on the *orchestrator's request DTO*, never on the *actual Conversations command* | AC2, AC3 / AD-13 | Server | `Append_success_authors_the_message_as_the_agent_party_with_the_deterministic_identity` |
| 1k | Conversations **rejects** the append (`!IsSuccess`) → `PostRejected` | AC3 / AD-12 | Server | `An_append_rejected_by_conversations_maps_to_post_rejected` |
| 1l | An append that **throws** (error text carrying both a secret and the generated content) → fail closed `AdapterFailure`; neither the content nor the error crosses the boundary (AD-14) | AC4 / AD-12, AD-14 | Server | `A_thrown_append_fails_closed_to_adapter_failure_without_propagating_the_content_or_error` |
| 1m | A **genuine cancellation** during append still propagates | AC3 | Server | `A_genuine_cancellation_propagates_from_append` |
| 2 | Replay determinism was pinned for the **`Posted`** stream only; a request→…→**`PostingFailed`** stream rehydrating an identical status/reason/safe-id-evidence across independent rebuilds was untested — the fail-closed Audit Evidence must be stable for inspection (mirrors the 2.3 context-blocked determinism guard) | AC4 / AD-13 | Domain | `Replay_over_request_through_posting_failed_is_deterministic_across_rebuilds` |

## Acceptance-criteria coverage after the QA pass

- **AC1 — posting verifies the Agent is an `AiAgent` participant through a Conversations-owned seam, or
  fails closed:** the live adapter's membership verify is now fully pinned — `Present` only when the
  Agent `PartyId` is present **and** typed `AiAgent`; wrong-type, wrong-party, no-participants,
  no-details (Hidden), `!IsSuccess`, and thrown reads all fail closed (`SeamUnavailable` /
  `ConversationUnavailable`), and the `PartyId` normalization match is covered. No append is attempted on
  any fail-closed membership outcome (the orchestrator already proved the skip; the adapter now proves the
  classification).
- **AC2 — the posted message is authored by the Agent Party identity and references the interaction:**
  the **actual `AppendMessageCommand`** built by the live adapter is now asserted to carry
  `AuthorPartyId`/`Metadata.ActorPartyId` = the Agent Party (not the caller), the deterministic
  `MessageId`, the tenant scope, and the correlation — closing the gap where AC2 was only asserted on the
  orchestrator's request DTO, never on the command crossing the Conversations boundary.
- **AC3 — posting is idempotent on retry:** the deterministic `MessageId`/`IdempotencyKey` now ride all
  the way into the real `AppendMessageCommand`/`ConversationCommandMetadata` (Conversations dedupes on
  them); cancellation propagation on both membership and append is covered; the aggregate's terminal
  `Posted`/`PostingFailed` no-op remains green from the dev story.
- **AC4 — posting failure is a distinct, safe, auditable status that never leaks content:** the live
  adapter's append-throws path is covered with an error message carrying both a secret and the generated
  content, proving neither crosses the safe-enum boundary; and the `PostingFailed` audit record is now
  guarded for replay determinism so the fail-closed evidence is stable across rebuilds.

## Commands run (from `Hexalith.Agents/`)

```
dotnet restore Hexalith.Agents.slnx
dotnet build   Hexalith.Agents.slnx --configuration Release -m:1        # 0W / 0E
dotnet test tests/Hexalith.Agents.Tests/Hexalith.Agents.Tests.csproj                     --configuration Release --no-build # 527 / 0 / 0
dotnet test tests/Hexalith.Agents.Server.Tests/Hexalith.Agents.Server.Tests.csproj       --configuration Release --no-build # 231 / 0 / 0
dotnet test tests/Hexalith.Agents.Contracts.Tests/Hexalith.Agents.Contracts.Tests.csproj --configuration Release --no-build # 175 / 0 / 0
dotnet test tests/Hexalith.Agents.UI.Tests/Hexalith.Agents.UI.Tests.csproj               --configuration Release --no-build # 156 / 0 / 0 (regression)
```

## Files changed (tests only)

- `tests/Hexalith.Agents.Server.Tests/ConversationClientResponsePosterTests.cs` (**new** — live Conversations poster/membership adapter, 13 facts)
- `tests/Hexalith.Agents.Tests/AgentInteractionStateReplayTests.cs` (+1 fact — `PostingFailed`-stream replay determinism)

## Notes / out of scope

- **No API/UI E2E:** there is intentionally no HTTP controller, live dispatcher binding, query/read-model
  binding, or invocation UI in Story 2.5 (deferred to Story 2.6 and the read-model story), so browser/API
  E2E is not yet applicable. The cross-seam orchestrator→aggregate test
  (`End_to_end_the_dispatched_post_command_drives_the_aggregate_to_the_same_decision`, dev story) is the
  closest end-to-end available and remains green.
- **Membership-establish stays fail-closed by design (AD-6 / Story 2.0a):** `IConversationClient` exposes
  no `AddParticipantAsync`, so the live adapter returns `SeamUnavailable` whenever the Agent is not
  already an `AiAgent` participant. The new tests lock that fail-closed behavior in; the establish path
  (returning `Established`) is reachable only when Conversations exposes a public membership seam — no
  Agents test asserts a non-fail-closed establish today, exactly as the story intends.
- **Sibling module untouched:** no change to `Hexalith.Conversations` (its project-context forbids editing
  sibling modules from an Agents story). The adapter test drives the Conversations public client surface
  through NSubstitute only.

## Checklist validation (`bmad-qa-generate-e2e-tests/checklist.md`)

- [x] API tests generated (live Conversations posting/membership adapter + server orchestration — the closest analogue to API for this backend story)
- [x] E2E tests generated (cross-seam orchestrator→aggregate present from dev story; no UI surface to drive)
- [x] Tests use standard test framework APIs (xUnit v3 + Shouldly + NSubstitute)
- [x] Tests cover happy path (AiAgent present → Present; append success → Posted, authored by the Agent Party with the deterministic identity)
- [x] Tests cover critical error cases (wrong-type/wrong-party/absent membership, Hidden/`!IsSuccess`/thrown reads, append-rejected, append-throws content+secret redaction, cancellation propagation)
- [x] All generated tests run successfully (527 / 231 / 175 / 156, 0 failed, 0 skipped)
- [x] Tests use proper locators — N/A for backend; tests use typed fixtures and explicit command/participant lookups
- [x] Tests have clear descriptions (snake_case behavioral names)
- [x] No hardcoded waits or sleeps
- [x] Tests are independent (no order dependency; per-test substitutes)
- [x] Test summary created (this file)
- [x] Tests saved to the project's existing test directories
- [x] Summary includes coverage metrics (before/after table + gap table)
