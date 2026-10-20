# Epic 6 Context: One Safe Automatic Conversation Response

<!-- Compiled from planning artifacts. Edit freely. Regenerate with compile-epic-context if planning docs change. -->

## Goal

Let a Conversation Participant use the sole Conversation-owned **Call hexa** action and receive exactly one safe, `hexa`-attributed automatic response. Otherwise the call gets a precise typed fail-closed outcome before any unsafe Provider or Conversation effect. The epic builds durable Dapr Workflow execution on Epic 5 setup and readiness, and the automatic path works without Epic 7.

## Stories

- Story 6.1: Start And Recover An Automatic Interaction
- Story 6.2: Use The Complete Authorized Conversation Or Block
- Story 6.3: Block Unsafe Prompt Context Or Output
- Story 6.4: Generate Within Hard Cost Reservations
- Story 6.5: Enforce Capacity Backpressure And Tenant Fairness
- Story 6.6: Join And Post Exactly Once As hexa
- Story 6.7: Call hexa And Follow Automatic Status Accessibly
- Story 6.8: Add Missing Fail-Closed Runtime Contract Members

## Requirements & Constraints

- **Fixed acceptance order:**
  1. caller authorization, Party state and Conversation access (a non-person caller is denied);
  2. lifecycle, suspension, Party identity and dependency freshness;
  3. local `Blocked` check;
  4. Provider/model eligibility;
  5. joint rate/concurrency admission;
  6. Approver resolution (Confirmation mode only);
  7. complete context;
  8. cost reservation;
  9. prompt/context safety;
  10. membership read/join, immediately before `AgentCallAccepted`.

  A failure at steps 1–9 has no membership effect. Any failure before acceptance records a typed outcome and aborts or settles every preparation, but keeps the permit.
- **Context:** the complete authorized Conversation, measured with the exact tokenizer, or blocked. Never truncated, summarized, windowed, sampled, substituted or reused stale.
- **Safety:** a fresh versioned Allow is required before the Provider call and again before any side effect. No Approver override, and no weaker retry.
- **Failures:** Provider errors, timeouts and safety failures leave only content-free status and Audit Evidence on `AgentInteraction`. No proposal, version, message, retained bytes or live `GenerationFailureRecord`. The output-safety public status belongs to `OD-INITIAL-OUTPUT-SAFETY-STATUS-1`; no local mapping.
- **Cost and capacity:** Cost uses hard caps, an atomic maximum-cost reservation, reconciliation, and release only on authoritative no-use. Capacity uses one shared linearizable allocator. Process-local counters and queues are forbidden.
- **Posting:** exactly once as `hexa`'s Party, verified by immutable id, using a deterministic `MessageId` through supported Conversations seams only.
- **Recovery (NFR-11):** RPO 0, with no duplicate interaction, command, timer, attempt, version or post. A frozen-cohort recovery finishes within 15 monotonic minutes, terminal decisions stay unchanged, and mixed clock origins cannot qualify.
- **Isolation:** unauthorized or cross-tenant calls are denied before interaction creation or workflow start. No identity, timing, count or existence leak.
- **Dependency gating:**
  - `Uncommitted` blocks `ready-for-dev`, and `Committed` permits contract/package work only.
  - Executing a seam needs `Available` plus a passing exact compatibility command; otherwise the result is `DependencyNotAvailable(record)`.
  - Content paths fail with `PayloadProtectionUnavailable` until `EXT-PROTECTION-1` is attested.
  - Deterministic no-content tests are never live evidence.

## Technical Decisions

- **Workflow:** Dapr Workflow is the sole durable owner, and its instance id is `AgentInteractionId`. A non-terminal instance is never re-created. Workflow history is execution state; EventStore is truth.
  - Microsoft Agent Framework may run only inside a generation activity, after `EXT-PROVIDER-1` commits.
  - DurableAgent, MCP, A2A, in-memory workers and alternate owners must not exist.
  - Dapr Workflow is not yet used by any project; Story 6.1 owns its adoption and compatibility evidence. Versions come only from the Hexalith.Builds catalog (Dapr `1.18.7`), with no local pins.
  - Live workloads need an approved, runtime-matched `OD-DAPR-SECURITY-1`.
- **Commands:** an impure fail-closed orchestrator wraps a pure aggregate and dispatches at most one trusted command per step. Authorized I/O is split into authorize → effect → result. The twin-policy `Evaluate`/`Decide` pair shares one `Compute`.
- **Intake:** `RegisterInteractionPermit` on `ConversationAgentState` appends a content-free permit and a creation outbox holding sealed `ProtectedContent` under the interaction DEK, selected by key alias. This is not acceptance. The outbox creates the exact `AgentInteraction`, whose first append emits the directory acknowledgement and the workflow-start outbox.
- **Effect leases:** workflow start, Approver resolution, context, membership, Provider, proposal mutation and posting each move `Reserved → CommittedToEffect → Settled` or `Reserved → CancelledBeforeCommit`.
  - Only the commit revision authorizes the phase.
  - Deletion Closing races the commit at the same revision.
  - Committed leases are never revoked.
  - Rate, open, Budget and capacity phases need interaction-owned `*Authorized` revisions instead.
- **Provider branch:** the generic lease commits on owner-local checks. After that, exactly one immutable `ProviderInvocationAuthorized` (phase-pinned decision mapping) or `ProviderInvocationNotAuthorized` (NotInvoked/`CancelNoInvocation` settlement) is recorded. Generated output needs a separate committed `ProposalMutation(RecordGeneratedVersion)` lease, and the output is discarded if Closing wins.
- **Execution-state boundary:** workflow inputs, outputs, status, timers and retry state hold only ids, versions, digests, safe codes and `ProtectedContentReference`s. Content stays inside activities. Terminal workflows are purged within terminal handling plus 7 days.
- **Identity, time and principals:**
  - One `AgentsIdentity` canonicalizer derives all ids. `AgentInteractionId = H(TenantId, AgentId, SourceConversationId, CallerPartyId, ClientIdempotencyKey)`.
  - Sensitive digests use HMAC under a per-tenant `DigestKey`, versioned.
  - Aggregates never read clocks; `EvaluatedAt` comes from an injected `TimeProvider`.
  - The Interaction Workflow principal sends only allowlisted commands.
- **Migration (6.1):** a Platform-started `InteractionDirectoryMigration` quiesces writers, freezes the inventory, installs the EventStore epoch write fence and revokes legacy writes. It materializes `MigratedFrom` members, then needs two identical post-fence scans before `DirectoryReady`.
  - Repair rotates the epoch through a bridge.
  - Legacy plaintext stays `PayloadProtectionUnavailable`; only a protected or signed exact-empty inventory completes.
- **Shipped code:** directly created interactions and the `SafetyFailed` mapping are debt, not authority.

## UX & Interaction Patterns

- **Call hexa** is provided through `EXT-CONV-UI-1`. Agents owns the dialog body and the persistent status region, which is the only live region. The action is `DisabledFocusable` with a safe reason when it is not proven callable, and absent without access. Story 6.7 removes the `/agents/conversation-call` harness.
- **Status flow:** `submitted` (local) → `authoritative pending` (only after the accepted identity and projection version render) → projection-confirmed terminal. Only `Posted` gets Success.
  - Blocked, queued, rejected, generating, posting and unknown states stay distinct.
  - A failure is never a proposal.
- **Presentation:** Fluent V5/FrontComposer, with color, icon and text together. Whole-string English/French parity, WCAG 2.2 AA, focus return, and fail-closed restrictive viewports.

## Cross-Story Dependencies

- **Within the epic:**
  - 6.1 proves orchestration with deterministic context, safety, generation and posting activities, and has no forward dependency.
  - 6.4 uses injected admission until 6.5 supplies the allocator.
  - 6.6 needs 5.4 and 6.3–6.5.
  - 6.7 needs 6.1–6.6.
  - Story 7.7 consumes 6.8.
- **Prerequisites for 6.1:** Stories 5.5–5.8, still backlog at compile time.
- **External status (register, 2026-10-19):**
  - `EXT-HOST-1`, `EXT-SECRETS-1`, `EXT-PARTIES-1` (Branch B) and `EXT-CONV-AI-1` are `Committed` but none is `Available`; their owner commands exited 1 on 2026-10-18.
  - `EXT-PROTECTION-1`, `EXT-TOPOLOGY-1`, `EXT-PROVIDER-1`, `EXT-SAFETY-1`, `EXT-TOKEN-1` and `EXT-CONV-UI-1` are `Uncommitted`.
  - Open decisions: `OD-DAPR-SECURITY-1`, `OD-INITIAL-OUTPUT-SAFETY-STATUS-1`, `OD-RATE-CONCURRENCY-CONSUMPTION-1`, and the conditional `OD-LEGACY-PLAINTEXT-DISPOSITION-1`. No local default resolves any of them.
