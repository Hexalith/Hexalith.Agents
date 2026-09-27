---
title: '5.4 Prove Trusted Principal Tenant Party And Approver Readiness'
type: 'feature'
created: '2026-09-27'
status: 'draft'
route: 'dispatch'
review_loop_iteration: 0
context:
  - '_bmad-output/implementation-artifacts/epic-5-context.md'
  - '_bmad-output/planning-artifacts/external-dependency-register.md'
  - '_bmad-output/implementation-artifacts/story-5-4-dependency-recheck.md'
  - '_bmad-output/specs/spec-story-5-4-dependency-unblock/SPEC.md'
---

<frozen-after-approval reason="human-owned intent — do not modify unless human renegotiates">

## Intent

**Problem:** Setup trusts claim-derived roles and unsigned flags. Tenant, Party, and approver readers are deferred; replay admission and durable denial recording are absent.

**Approach:** Implement current-evidence authorization, Agent Party and approver resolution, AD-30 trusted ingress, replay admission, and durable security observations. Prove fail-closed behavior and safe API/UI basis against accepted live contracts.

## Boundaries & Constraints

**Always:** Select one operation-specific `User`, `Administrator`, `Platform`, or `Workflow` principal from fresh Tenants/Parties evidence; check Platform in `global-administrators`. Verify immutable Agent Party by tenant/id and AI type only under Branch A. Keep Caller deserializable but reject it before mutation. Strip client-reserved keys; sign/verify AD-29 canonical bytes using the committed secrets profile. Register (`Issuer`, `DeliveryNonce`) in reserved `system` before target-command construction; exact replay preserves stored first-seen times. Durably spool each denial before reporting it processed, then append one acknowledged daily `SecurityEventLog` event. Use EventStore for domain state and private exact-target credentials for both pre-command exceptions.

**Never:** Trust JWT roles, old projections, Party type alone, or client extensions. Never persist Party PII, secrets, or claimed target identity. Do not replace the Party, create another Agent, invent an Agents host/spool, execute a seam before `Available`, or claim live readiness from mocks.

## I/O & Edge-Case Matrix

| Scenario | Input / State | Expected Output / Behavior | Error Handling |
| --- | --- | --- | --- |
| Current authority | Contiguous tenant events; active Party and approver basis | Same safe API/UI basis | Revoked, gapped, stale, ambiguous, or unavailable blocks |
| Trusted delivery | Valid principal, scope, tag, nonce | One command; exact replay reaches AD-29 idempotency | Forged/changed/cross-tenant nonce rejects and audits |
| Denial recovery | EventStore or worker outage | Durable spool item drains once | Spool outage blocks before target command |

</frozen-after-approval>

## Open Questions

- **Owner acceptance:** The user selected Branch B as the implementation path. Product and Parties Maintainer must formally accept its complete launch contract; final identity/binding, Conversations, custody and host contracts, complete immutable targets, integration dates and executable compatibility commands remain unaccepted. See the [owner work and acceptance packet](../specs/spec-story-5-4-dependency-unblock/SPEC.md).

The user confirmed full scope on 2026-09-27. The former narrowing question is resolved: retain all 5.4 work in `draft`; local Caller-policy retirement is a prerequisite within that scope, not an alternative completion.

## Code Map

- `src/Hexalith.Agents.Server/Ports/HttpAgentAdministrationContextProvider.cs` — JWT role authority; tenant/approver readers remain deferred.
- `src/Hexalith.Agents.Server/Ports/PartiesAgentPartyDirectory.cs` — ambient tenant, unchecked returned ID, nullable freshness; existing Organization provisioning supports the user-selected Branch B path but does not satisfy its contract.
- `src/Hexalith.Agents.Server/Application/Queries/AgentSetupQueryHandlerBase.cs` — global-admin bypass needs current authority.
- `src/Hexalith.Agents.EventStore/AgentsTrustedCommandExtensionPolicy.cs` — Dapr identity plus literal flags, without signed ingress. Reuse EventStore idempotency separately from AD-29/30 signing.

## Tasks & Acceptance

**Execution:**
- [ ] `src/Hexalith.Agents.Server/Ports/ProjectedTenantAccessReader.cs`, `PartiesAgentPartyDirectory.cs`, `ProjectedApproverPolicyResolver.cs`, and `src/Hexalith.Agents.Server/Composition/AgentDomainHostComposition.cs` — bind contiguous tenant events and current Party, actor, and approver sources.
- [ ] `src/Hexalith.Agents/Agent/AgentAggregate.cs` and `src/Hexalith.Agents.UI/Components/Pages/ApproverPolicy.razor` — reject Caller and render exact safe basis.
- [ ] `src/Hexalith.Agents.Server/Api/AgentsOperationEndpoints.cs`, `src/Hexalith.Agents.Server/Application/Queries/AgentSetupQueryHandlerBase.cs`, and `src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionGateOrchestrator.cs` — authorize before protected reads or effects.
- [ ] `src/Hexalith.Agents.EventStore/AgentsTrustedCommandExtensionPolicy.cs` and `src/Hexalith.Agents.Server/Ports/EventStoreAgentCommandDispatcher.cs` — verify signed operation/principal/scope and separate logical/delivery ids.
- [ ] `src/Hexalith.Agents/TrustedEnvelopeReplay/TrustedEnvelopeReplayAggregate.cs` and `src/Hexalith.Agents/SecurityEventLog/SecurityEventLogAggregate.cs` — implement pure folds; bind private exact-target replay/spool/worker ports in `src/Hexalith.Agents.Server/Composition/AgentDomainHostComposition.cs`.
- [ ] `test/Hexalith.Agents.Tests/AgentApproverPolicyTests.cs`, `test/Hexalith.Agents.UI.Tests/ApproverPolicyTests.cs`, and `eng/verify-story-5.4.ps1` — cover matrix edge cases, persisted replay, ACL, isolation, and UI; execute external seams only at `Available`.

**Acceptance Criteria:**
- Given active tenant/Party/approver evidence, when setup is queried, then API/UI expose the same safe basis; revocation or uncertainty blocks before downstream effects.
- Given a signed scoped envelope, when replay admission runs, then only its permitted principal reaches dispatch; exact replay preserves stored first-seen facts and changed fields reject/audit.
- Given a denial during EventStore or worker outage, when recovery runs, then one spool item reaches one acknowledged daily security event; spool outage constructs no target command.
- Given tenant B targets tenant A, when any public or application path denies, then no lookup, effect, count, message, log, or accessible output discloses A.

## Implementation Notes

## Spec Change Log

- 2026-09-27: User directed preservation of full scope. Added the Parties-first implementation proposal and complete dependency-ordered owner work. No frozen intent, acceptance criterion, dependency status or readiness gate changed.
- 2026-09-27: User selected Branch B as the implementation path. Product and Parties Maintainer acceptance and every complete dependency gate remain pending.

## Review Triage Log

## Design Notes

Rechecked 2026-09-27: all four dependency records remain `Uncommitted`; owner issues remain open without replies. The [implementation proposal](../specs/spec-story-5-4-dependency-unblock/SPEC.md) now directs Branch B work, supplies the concrete Parties identity/current-and-historical binding spec and preserves complete Conversations/custody/host owner work, including v21/v23. Its source revisions are observations, not accepted targets. Full 5.4 stays `draft` until all entry requirements are met; consuming seam execution still requires `Available`. No migration, deployment, or external write is proposed. Preserve 5.3's platform catalog, tenant enablement, and exact command-outcome behavior.

## Verification

**Commands:**
- `pwsh ./eng/verify-story-5.4.ps1` — warning-free builds, individual suites, persisted end state and negative evidence; live lanes only against `Available` targets.
