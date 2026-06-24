# Test Automation Summary — Story 3.5: Approve A Selected Version And Post It

**Workflow:** `bmad-qa-generate-e2e-tests` · **Date:** 2026-06-24 · **Engineer role:** QA automation (Administrator)
**Story:** `_bmad-output/implementation-artifacts/3-5-approve-a-selected-version-and-post-it.md` (status: review) · **FR coverage:** FR-17 (+ FR-14, FR-20, FR-21, FR-24)
**Test framework (detected):** .NET 10 / Blazor — xUnit v3 `3.2.2`, Shouldly `4.3.0`, NSubstitute, bUnit (no Playwright/Cypress JS stack). Run via the built xUnit v3 executables directly (VSTest TCP socket unavailable in the sandbox). Used the existing framework; added no dependency.

## Scope

Story 3.5 shipped with happy-path coverage across all four layers plus a contracts suite. This QA pass did **not** regenerate that suite; it performed a coverage-gap analysis — every approval/posting behavior, branch, enum, guard and fail-closed path cross-referenced against the existing tests and Task 5's stated requirements — and **auto-applied tests for the discovered gaps only**. No production code was changed; gaps were filled additively in the existing approval test classes (reusing established helpers/fixtures) plus one new end-to-end lifecycle file.

## Baseline (story Dev Agent Record)

| Project | Total | Failed |
| --- | --- | --- |
| Hexalith.Agents.Contracts.Tests | 278 | 0 |
| Hexalith.Agents.Tests (domain) | 609 | 0 |
| Hexalith.Agents.Server.Tests | 318 | 0 |
| Hexalith.Agents.UI.Tests | 498 | 0 |
| **Total** | **1703** | **0** |

Release build: **0 warnings / 0 errors** (warnings-as-errors).

## Coverage gap analysis — discovered gaps and the tests added

### Domain — `AgentInteractionProposalApprovalAggregateTests.cs` (+17)

| Test | Gap closed | AC |
|------|-----------|----|
| `Approved_outcome_records_approval_and_posting_pending_without_posting` | Only the `Posted` 3-event path was tested; the `Approved` ⇒ **PostingPending** two-event path (the "Approved is not posted" guardrail) was untested. | AC1 |
| `Empty_selected_version_records_approval_failed_with_selected_version_missing` | The shipped test covered an *invalid* (unknown-id) version; the *missing* (empty-id) ⇒ `SelectedVersionMissing` branch was untested. | AC1 / AC4 |
| `Approving_an_interaction_with_no_recorded_stream_is_rejected_as_not_proposed` | `InteractionNotProposed` structural rejection (null stream) had no test. | AC4 |
| `Approval_preserves_every_prior_generated_version` | FR-14 version preservation through approval was unasserted at the aggregate. | AC1 / FR-14 |
| `Decide_matches_the_aggregate_recorded_decision_for_each_outcome` (13 cases) | Task 5 requires the `Evaluate`/`Decide` no-drift theory **over every outcome**; it was entirely absent for approval. Now exhaustive over outcome×verdict, asserting pure `Decide` and the aggregate-recorded terminal status never diverge. | AC1–AC4 |

### Domain — `AgentInteractionProposalApprovalLifecycleE2ETests.cs` (new, +3)

The headline E2E deliverable. Proposal- and edit-lifecycle E2E suites existed for earlier stories but **none existed for approval**. Drives the whole command chain (request → gate → context → generate → propose → approve) through the real reflection-dispatch + JSON round-trip pipeline.

| Test | Gap closed | AC |
|------|-----------|----|
| `A_confirmation_interaction_reaches_a_posted_proposal_through_the_full_command_chain` | No end-to-end proof that a full Confirmation path posts exactly the selected version, preserves the prior version, and emits content-free evidence. | AC1 / AC2 / AD-14 |
| `A_proposal_cannot_be_approved_before_it_exists_end_to_end` | No E2E fail-closed-before-side-effects proof (approve before a proposal exists ⇒ rejection, no posting events). | AC4 |
| `Re_approving_the_same_posted_version_is_an_idempotent_no_op_end_to_end` | No E2E idempotent-retry proof (same message id + idempotency key ⇒ no-op, no duplicate Conversation Message). | AC3 |

### Server — `AgentInteractionProposalApprovalOrchestratorTests.cs` (+7)

Only happy / unauthorized / terminal paths existed. Added the fail-closed-before-side-effects matrix required by Task 5.

| Test | Gap closed | AC |
|------|-----------|----|
| `Selected_version_unavailable_fails_closed_before_reading_party_or_posting` | Version-read not-available ⇒ `SelectedVersionMissing`, short-circuits before party read + append. | AC4 |
| `Missing_agent_party_identity_fails_closed_before_posting` | Party reader unavailable ⇒ `PartyIdentityUnavailable`, no append. | AC4 |
| `Unavailable_conversation_membership_fails_closed_before_posting` | Membership seam unavailable ⇒ `MembershipUnavailable`, no append. | AC4 |
| `Conversation_rejecting_the_append_maps_to_posting_failed` | Append `PostRejected` ⇒ `ProposalPostingFailed`, posted-message-id empty. | AC2 / AC4 |
| `Posting_adapter_failure_maps_to_posting_failed` | Append `AdapterFailure` ⇒ `ProposalPostingFailed`. | AC4 |
| `A_genuine_cancellation_propagates_and_no_command_is_dispatched` | `OperationCanceledException` propagation (vs. swallowed as a failure) was untested. | — |
| `Posting_identity_is_deterministic_so_a_retry_targets_the_same_message` | Deterministic message-id reuse across retries (idempotency) was untested at the orchestrator. | AC3 |

### UI — `Hexalith.Agents.UI.Tests` (+15)

| Test | Gap closed | AC |
|------|-----------|----|
| `DeferredProposalApprovalGateway_fails_closed_with_not_authorized_and_no_message` (`DeferredGatewayTests`) | The approval deferred write-path placeholder was the only proposal gateway lacking a fail-closed contract test. | AC4 / AD-12 |
| `LocalizationResourceTests` — 10 `Agents.ProposalApprover.*` keys added to the enforced en/fr parity set | The new approver whole-strings were absent from the localization-parity guard (en theory + fr no-missing check). | Task 4 |
| `Empty_selected_version_short_circuits_to_unavailable_without_calling_the_gateway` (`ProposalApproverTests`) | The control's empty-version short-circuit branch (no gateway call) was uncovered. | AC4 |
| `Gateway_status_renders_its_whole_string_key_in_the_correct_live_region` (3 cases) | `Approved` / `NotPending` / `Unavailable` status rendering + `role`/`aria-live` live-region totality were uncovered (only Posted/PostingFailed/PostingPending/NotAuthorized were). | AC2 / AC4 |

## Result (after this pass)

| Project | Before | After | Δ |
| --- | --- | --- | --- |
| Hexalith.Agents.Contracts.Tests | 278 | **278** | 0 (regression check) |
| Hexalith.Agents.Tests (domain) | 609 | **629** | +20 |
| Hexalith.Agents.Server.Tests | 318 | **325** | +7 |
| Hexalith.Agents.UI.Tests | 498 | **513** | +15 |
| **Total** | 1703 | **1745** | **+42** |

**Result: Passed — Errors: 0, Failed: 0, Skipped: 0** across all four projects. Release build after additions: **0 warnings / 0 errors**.

## AC coverage after this pass

- **AC1** (records exactly one selected version; posting-pending state): domain Approved/PostingPending + missing-version + preserved-versions + no-drift; E2E posted-version; server happy path. ✅
- **AC2** (posted as `hexa`; audit evidence linkage): E2E full-chain post + content-free evidence; server append attribution + post-rejected mapping. ✅
- **AC3** (idempotent retries): domain no-op retry; E2E idempotent re-approval; server deterministic message identity. ✅
- **AC4** (fail closed before side effects; distinguish approval vs posting failure): domain not-proposed/missing-version + no-drift; server fail-closed matrix (version/party/membership/append/cancellation); UI deferred-gateway + control fail-closed. ✅

## Checklist validation (`bmad-qa-generate-e2e-tests/checklist.md`)

- [x] API / write-path tests generated (orchestrator + aggregate command/event surface)
- [x] E2E / component tests generated (new approval lifecycle via `ProcessAsync` + bUnit approver)
- [x] Tests use standard framework APIs (xUnit v3 / Shouldly / NSubstitute / bUnit; no raw `Assert.*`)
- [x] Happy path covered (authorized approval + post + dispatch)
- [x] 1–2 critical error cases covered (not-proposed, missing/invalid version, fail-closed party/membership/append, cancellation)
- [x] All tests run successfully — **1745 total, 0 failed**
- [x] Proper locators (semantic `data-testid` / `role` / `aria-live` queries in UI)
- [x] Clear descriptions (`Subject_scenario_expectation`)
- [x] No hardcoded waits/sleeps (bUnit `WaitForAssertion`, no `Thread.Sleep`)
- [x] Tests are independent (each builds its own state/render/substitutes; no order dependency)
- [x] Summary created with coverage metrics (this file)

## Files changed (tests only — no production change)

- `Hexalith.Agents/tests/Hexalith.Agents.Tests/AgentInteractionProposalApprovalAggregateTests.cs` (+5 facts/theory, `ApprovalResult` helper extended with optional `verdict`, `TerminalStatus` helper)
- `Hexalith.Agents/tests/Hexalith.Agents.Tests/AgentInteractionProposalApprovalLifecycleE2ETests.cs` (**new** — 3 E2E tests)
- `Hexalith.Agents/tests/Hexalith.Agents.Server.Tests/AgentInteractionProposalApprovalOrchestratorTests.cs` (+7 tests, `+using System;` `+using NSubstitute.ExceptionExtensions;`, shared stub helpers)
- `Hexalith.Agents/tests/Hexalith.Agents.UI.Tests/DeferredGatewayTests.cs` (+1 test)
- `Hexalith.Agents/tests/Hexalith.Agents.UI.Tests/LocalizationResourceTests.cs` (+10 approver keys)
- `Hexalith.Agents/tests/Hexalith.Agents.UI.Tests/ProposalApproverTests.cs` (+1 fact, +1 theory/3 cases)

## Notes / boundaries

- No production code changed — the gaps were purely missing test coverage. The Contracts suite was re-run to confirm (not modify) green status.
- The live server read-model / projection / query-handler / BFF binding for `AgentProposalApprovalEvidenceResult` remains **deferred to Epic 4**; its integration/API tests belong with that work.
- Story 3.7 will host the approver control inside the full proposal-detail workspace / version-history / keyboard-focus accessibility — intentionally not duplicated here.

## Next steps

- Run the four projects in CI individually (never solution-level `dotnet test`; build with `-m:1`).
- When Epic 4 wires the live approval read/write path, replace the deferred-gateway fail-closed tests with live-binding integration tests and add the audit-evidence projection/query tests deferred out of Story 3.5 scope.
