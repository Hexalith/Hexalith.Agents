---
title: '5.4 Conversations owner implementation'
type: 'feature'
created: '2026-10-06'
status: 'in-progress'
route: 'dispatch'
human_approval: 'accepted'
approved_on: '2026-10-06'
baseline_commit: '9938aa8aa5cfc082de6be3d28eede22957b14a26'
review_loop_iteration: 0
context:
  - '/home/administrator/projects/hexalith/conversations/AGENTS.md'
  - '/home/administrator/projects/hexalith/conversations/references/Hexalith.AI.Tools/hexalith-llm-instructions.md'
  - '/home/administrator/projects/hexalith/conversations/references/Hexalith.AI.Tools/hexalith-state-instructions.md'
  - '/home/administrator/projects/hexalith/agents/_bmad-output/specs/spec-story-5-4-dependency-unblock/owner-implementation-plan.md'
  - '/home/administrator/projects/hexalith/agents/_bmad-output/planning-artifacts/external-dependency-register.md'
---

<frozen-after-approval reason="human-owned prerequisite intent">

## Intent

Implement the Conversations-owned six-seam restricted Agents service contract C1–C4. The user authorized prerequisite implementation on 2026-10-06. Prepare working domain/client/query behavior and runnable local verification. Full owner qualification and Available status remain gated by real current authority, complete source enumeration, independent deletion approval, authenticated receiver and accepted exact targets/commands. Original Story 5.4 stays blocked until all original requirements are met.

## Constraints

Work only in the sibling Conversations owner checkout. Preserve the existing new `Contracts/Agents` files, original API compatibility, Facilitator, and unrelated edits. Domain persistence uses EventStore; aggregates are pure Handle/Apply. Infrastructure belongs to EventStore/Platform. Do not add raw storage, an AppHost, proprietary CLI, token authority or projection-based authority. No remote operations, commit, deployment, nested submodules, or live consumption of unavailable dependencies. Production ports must fail closed when unconfigured; local fixtures never establish live readiness. Read every required baseline. Build Debug with source references, isolated artifacts and `set -e`; run individual xUnit assemblies with valid exact class filters. Never alter an existing acceptance expectation merely to fit code.

## I/O & Edge-Case Matrix

| Case | Expected behavior |
| --- | --- |
| Exact Agent Organization/AiAgent/Member membership retry | One replayable membership, no extra event; changed identity/type/role conflicts |
| Removed Agent principal | Durable removal evidence; service retry cannot rejoin |
| Cross-tenant, unrelated Party or uncertain current authority | Fail before protected source lookup or effect; no existence/content disclosure |
| Deterministic post retry/lost acknowledgement | Same MessageId and intent survive serialized replay; changed intent conflicts |
| Current content/roster | Edit, delete/redaction and Facilitator reflected; typed deletion/removal/absence distinct after authorization |
| Tenant active count | Counts active source Conversations including zero Agent Calls; missing complete catalogue is Unavailable, never zero |
| Approved deletion | One durable approval event embeds immutable signal and original approval source revision |
| Delivery retry/backfill/rollover | Same signal/revision; checkpoint advances only on exact authenticated acknowledgement |
| Changed or poison acknowledgement | Durable quarantine, no checkpoint advance or success claim |

</frozen-after-approval>

## Code Map

- `src/Hexalith.Conversations.Contracts/Agents` — existing portable outcome, read/count, provenance, deletion and delivery contracts; extend deliberately where needed.
- `Contracts/Commands/AppendMessageCommand.cs`, `Contracts/Events/MessageAppended.cs`, `Contracts/Queries` — compatible deterministic posting/provenance and SDK query DTOs.
- `src/Hexalith.Conversations/Aggregates/ConversationAggregate.cs`, `State/ConversationState.cs`, `State/ConversationMessage.cs`, `Commands`, `Events`, `Validation` — pure membership/removal/post/edit/deletion/delivery state and exhaustive replay.
- `src/Hexalith.Conversations.Server/Queries`, command/admission handlers and registration — SDK handlers plus narrow current authority, immutable Party, independent approval and authenticated receipt ports. Ports do not constitute delivered production providers.
- `src/Hexalith.Conversations.Client/IConversationClient.cs` and HTTP client — publish all six owner seams with typed outcomes and exact returned MessageId checks; gateway submission, no alternate persistence.
- Sibling EventStore `Client/Streams/IAuthoritativeEventStreamReader.cs` — bounded exact-stream complete prefix between stable sampled heads. Reconfirm authority after awaited reads. No complete tenant catalogue exists: require an authenticated complete catalogue/feed port and leave production count unavailable until its owner supplies one.
- SDK `IDomainServiceAdmissionStage` and `ITrustedCommandExtensionPolicy` — reusable admission before pure processing, with explicit denied defaults. Existing `/process`/`query` remain transport boundaries; avoid domain-specific HTTP infrastructure.

## Tasks & Acceptance

- [ ] Publish and implement all six portable client/service seams, source/current reads and restricted command behavior.
- [ ] Extend the existing aggregate/state with deterministic event-only membership, removal tombstones, posting/provenance/current message mutations and deletion publication/delivery/quarantine.
- [ ] Require authenticated current scope and exact immutable Organization Party. Independent approval and receiver evidence cannot be minted by the Agents membership authority.
- [ ] Preserve original source positions: any tracked revision must count every supported source event once and handle rejection/tail/snapshot input safely. Reject uncertified old snapshots and mismatched heads. Never embed a caller-claimed revision as persisted proof.
- [ ] Add complete local tests for the matrix, serialized fresh-state replay and concurrent intent behavior. Distinguish local persisted-event simulation from live storage/restart evidence.
- [ ] Add `eng/verify-ext-conv-ai-1.ps1` with named six-seam Local checks, executed-class evidence and a full Live gate that rejects missing accepted targets/providers before any calls. Required lanes cannot be skipped as a success.
- [ ] Produce a separate owner implementation/evidence note listing exact changed files, commands, source revision, results and every remaining complete-record gap. Do not update acceptance/register fields.

Acceptance: Given a current admitted service operation, when complete source is replayed, then the typed result and durable event intent match the matrix. Given missing production authority/catalogue/receiver, when service or verification runs, then it fails closed without inferring readiness. Given the original full owner record, when local work is reported, then unimplemented or unavailable full-contract portions remain explicit.

## Verification

Use isolated `/tmp/hexalith-agents54-conversations-artifacts` outputs. Keep exact build logs and XML. Report actual failures and compatibility blockers. Final report must distinguish implemented, tested and still externally gated behavior.

## Implementation Notes

- 2026-10-06: The user requested pragmatic solutions without over-engineering. Extend the existing aggregate, SDK query and client paths. Do not introduce a tenant catalogue database, delivery framework or additional service merely to make a missing provider appear available. Keep any proposed window semantics explicit and unaccepted until the owner confirms them; count authorization must not grant access to unjoined Conversation content.
