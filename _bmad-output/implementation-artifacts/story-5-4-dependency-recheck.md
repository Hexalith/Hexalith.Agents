# Story 5.4 dependency recheck

Checked 2026-09-27 during `bmad-build 5.4`. Full Story 5.4 remains in planning: no accepted external target or executable compatibility command was found. This report records observations, not owner acceptance or live readiness evidence.

## Inspected revisions

| Repository | Full HEAD |
| --- | --- |
| Agents | `ce129c60cc0b5d02764506c8d168cfa2f261015a` |
| Tenants reference | `49ca5b46c6cab066b5a939a5551a27aa8db1e541` |
| Parties reference | `c069af07af5f1e42bdfa1287705d943b8b4cadf7` |
| Conversations reference | `30fffc2cd427e837c719b87cb33475e34a6fc8de` |
| EventStore reference | `68492519b868899e6ab6bf64931f19f0cb1ca6a7` |
| Builds reference | `0610f7837221c9859e280f06f333d7452a40f5b1` |
| Sibling Platform | `98db8cf9fd6058c17e36d45b1935ec5301a68e00` |

Agents started clean on `main`. The sibling Platform worktree contains pre-existing edits and is not an immutable acceptance target; it was inspected read-only.

## Owner requests

Read through `gh issue view --json title,state,url,body,comments`; each issue is open with an empty comments collection:

| Dependency | Request | Missing acceptance |
| --- | --- | --- |
| `EXT-CONV-AI-1` | [Conversations #4](https://github.com/Hexalith/Hexalith.Conversations/issues/4) | Complete six-seam target, date, final typed public contract and live verification command |
| `EXT-PARTIES-1` | [Parties #54](https://github.com/Hexalith/Hexalith.Parties/issues/54) | Product/maintainer Branch A or B decision plus complete identity and current/historical human-binding target, date and live command |
| `EXT-SECRETS-1` | [Platform #1](https://github.com/Hexalith/Hexalith.Platform/issues/1) | Owning repository, full custody target/date/command and numeric signing/replay profile |
| `EXT-HOST-1` | [Platform #2](https://github.com/Hexalith/Hexalith.Platform/issues/2) | Complete host target/date/command, replicated security spool and restricted replay/recorder capabilities |

The authoritative [dependency register](../planning-artifacts/external-dependency-register.md) still marks all four `Uncommitted`, with target, date and command `TBD`. Local implementation, a partial test, or a newer submodule revision cannot establish acceptance. No messages were sent to owners.

## Code evidence

- **Tenants:** `references/Hexalith.Tenants/src/Hexalith.Tenants.Client/Projections/TenantLocalState.cs` and `TenantProjectionEventMetadata.cs` supply status, membership and sequence metadata. `Handlers/TenantProjectionEventHandler.cs` does not prove contiguous delivery: it accepts sequence jumps and equal-sequence events. Agents still binds `DeferredTenantAccessReader` in `src/Hexalith.Agents.Server/Composition/AgentDomainHostComposition.cs`.
- **Parties:** `references/Hexalith.Parties/src/Hexalith.Parties.Contracts/Models/PartyDetail.cs` supplies type/liveness and nullable freshness; no stable authenticated human actor ID, binding version/interval or historical binding query was found. `ValueObjects/PartyType.cs` has only Unknown, Person and Organization. Existing-state `CreateParty` handling in `src/Hexalith.Parties/Domain/PartyAggregate.cs` returns no-op before divergent-request validation. Agents' `PartiesAgentPartyDirectory` relies on ambient tenant scope, does not verify the returned ID, and accepts absent freshness.
- **Conversations:** `IConversationClient.GetConversationAsync`, `ConversationDetailsV1.Participants` and `ParticipantRole.Facilitator` are reusable public primitives. `ConversationDetailResult.Hidden` does not distinguish `ConversationDeleted` from `PrincipalRemovedFromConversation`; the complete six-seam contract is absent.
- **Approvers:** Agents binds `DeferredApproverPolicyResolver`; its result lacks an exact current human binding/evidence basis. `AgentAggregate` still accepts Caller, `ApproverPolicy.razor` still offers it, and current domain/UI tests assert that behavior. Local retirement can preserve the wire enum while rejecting new writes and removing the choice, without calling an external seam.
- **Ingress:** `HttpAgentAdministrationContextProvider` trusts JWT roles; `AgentSetupQueryHandlerBase` accepts `query.IsGlobalAdmin`. `AgentsTrustedCommandExtensionPolicy` checks authenticated Dapr identity and allowlisted literal flags, not AD-30 signatures. `EventStoreAgentCommandDispatcher` forwards the supplied envelope with `MessageId` as idempotency key and no separate nonce admission.
- **EventStore:** `ITrustedCommandExtensionPolicy`, `IIdempotencyIntentAdapter`, `IIdempotencyAdmissionCoordinator` and `CanonicalIdempotencyIntentEncoder` are existing integration seams. The business-idempotency encoder has its own format; it is not the AD-29/30 signing format. No replay ledger or security observation/spool implementation was found in Agents, EventStore or Builds source.
- **Platform:** `../platform/docs/ext-host-1-agents-composition.md` describes the historical scaffold. `../platform/eng/verify-agents-host.sh` builds that scaffold and explicitly defers live Agents composition. It cannot establish the expanded host/secrets contracts.

## Planning disposition and verification

The full draft retains its frozen intent and remains `draft`; sprint status remains `backlog`. The unresolved choice is to retain this complete scope until owner acceptance, or explicitly narrow to local Caller-policy retirement and its regression tests. Narrowing would not select Branch B, close the four dependencies, or complete live readiness.

`eng/verify-story-5.4.ps1` does not exist yet. No source code changed, builds/tests were not run, and no external seam was executed. Implementation still needs persisted-state replay/denial recovery, restricted-capability ACL, cross-tenant non-disclosure and live dependency evidence after the applicable gates are satisfied. Prior Story 5.3 test results are continuity context only.

## Full-scope owner implementation follow-through — 2026-09-27

The user has resolved the scope choice above: preserve **all** of Story 5.4. The [owner implementation proposal](../specs/spec-story-5-4-dependency-unblock/SPEC.md) replaces the proposed narrowing option with concrete work: Parties identity-by-id, tenant isolation, divergent-retry conflicts and durable current/historical human binding first; then the complete six Conversations seams, full custody profile and complete Platform composition including v21/v23.

Branch B was recommended from the current Organization provisioning code and selected by the user as the implementation path on 2026-09-27; formal Product/Parties acceptance remains pending. The proposal's [evidence and acceptance handoff](../specs/spec-story-5-4-dependency-unblock/evidence-and-acceptance.md) records the newer inspected reference/owner revisions, existing in-memory identity-binding work and EventStore crypto core without treating them as delivered contracts or live evidence. Read-only owner issue checks still found four open issues with no comments.

The original inspection above remains historical evidence. The story is still draft/backlog; all four complete records remain Uncommitted with no accepted target/date/command. The register now records the Branch B implementation direction; its commitment fields and all original entry/execution gates are unchanged. No owner messages were posted and no external seam was executed.
