# Adversarial Boundary Review

Verdict: Pass after fixes.

Attack: two builders both obey the ADs but still diverge.

Case 1: Proposal builder treats generated reply as a Conversation message; posting builder posts only approved versions.

- Covered by AD-5 and AD-6. Proposed Agent Reply is never a Conversation Message, and posting requires selected version plus Conversations boundary.

Case 2: Provider builder stores provider-specific model knobs in public contracts; UI builder renders those knobs directly.

- Covered by AD-9 and AD-10. Public contracts expose normalized capability metadata only; provider-specific knobs stay adapter-local until a new architecture decision.

Case 3: Approval builder resolves "conversation owner" from the first participant; UI builder resolves it from facilitator role.

- Initially a hole. Fixed by AD-8: V1 resolves conversation authority from Conversations detail and maps product owner authority to `ParticipantRole.Facilitator` unless a first-class owner resolver exists.

Case 4: Posting builder appends as Agent PartyId without membership; membership builder assumes Conversations already enrolled the Agent.

- Initially under-specified. Fixed by AD-6/AD-7 plus Deferred prerequisite: membership must be established through a Conversations-owned `AddParticipant` seam or posting fails closed.

Case 5: Operations builder logs prompts/generated content for debugging; audit builder hides content.

- Covered and tightened by AD-14. Raw content and provider payloads are forbidden in logs/telemetry/status/audit summaries; production content-bearing workflows require payload protection/redaction conventions.

Remaining adversarial risk:

- If implementation chooses to make denial audit events for unauthorized callers, it must avoid cross-tenant audit leakage. AD-12 and AD-14 constrain this, but the detailed denial-audit contract should be tested under AD-17.

## 2026-06-23 Dapr Runtime Amendment

Verdict: Pass.

Attack: two builders both obey the ADs but still diverge.

Case 6: Runtime builder implements generation with an in-memory hosted service; workflow builder implements proposal expiry with Dapr Workflow.

- Covered by AD-18. V1 agent workflow execution must run through Dapr Workflow for context loading, generation, proposal waits, expiry, retries, and posting.

Case 7: Provider builder exposes Dapr AI or Dapr Agents request types in public contracts; domain builder keeps aggregates SDK-neutral.

- Covered by AD-9 and AD-18. Dapr AI, Dapr Agents, provider SDK, and workflow SDK types are adapter/runtime leaves and cannot appear in public contracts or EventStore aggregates.

Case 8: Workflow builder treats Dapr Workflow state as the source of truth; aggregate builder treats EventStore as the source of truth.

- Covered by AD-1, AD-3, and AD-18. Dapr Workflow owns execution coordination only; Agents domain state changes through `AgentInteraction` commands/events.

Remaining adversarial risk:

- Workflow versioning and replay-safe activity naming need implementation discipline. AD-18 binds the substrate; detailed workflow versioning tests should be included under AD-17.
