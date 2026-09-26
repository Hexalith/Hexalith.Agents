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

- **Execution scope while four dependencies remain `Uncommitted`:** Keep full 5.4 in `draft` until owners accept targets and commands (preserves the full outcome); or narrow this run to Branch-B-compatible contracts/tests and defer live integration to another story (delivers only a prerequisite).

## Code Map

- `src/Hexalith.Agents.Server/Ports/HttpAgentAdministrationContextProvider.cs`, `ITenantAccessReader.cs` — claim-only authority and deferred tenant read; Tenants' handler accepts gaps.
- `src/Hexalith.Agents.Server/Ports/PartiesAgentPartyDirectory.cs`, `IApproverPolicyResolver.cs` — current Party read ignores tenant and assumes Organization; approver reader is deferred.
- `src/Hexalith.Agents/Agent/AgentAggregate.cs`, `src/Hexalith.Agents.UI/Components/Pages/ApproverPolicy.razor` — Caller is still accepted/offered; preserve its wire enum.
- `src/Hexalith.Agents.Server/Api/AgentsOperationEndpoints.cs`, `src/Hexalith.Agents.Server/Application/Queries/AgentSetupQueryHandlerBase.cs` — public paths and global-admin bypass need pre-lookup authority.
- `src/Hexalith.Agents.EventStore/AgentsTrustedCommandExtensionPolicy.cs`, `src/Hexalith.Agents.Server/Ports/EventStoreAgentCommandDispatcher.cs` — literal flags and shared dispatch lack signed principal, scope, and delivery nonce.
- `src/Hexalith.Agents/TrustedEnvelopeReplay/`, `src/Hexalith.Agents/SecurityEventLog/` — missing pure aggregates and private registrar/recorder bindings.

## Tasks & Acceptance

**Execution:**
- [ ] `src/Hexalith.Agents.Server/Ports/ProjectedTenantAccessReader.cs`, `PartiesAgentPartyDirectory.cs`, `ProjectedApproverPolicyResolver.cs`, and `src/Hexalith.Agents.Server/Composition/AgentDomainHostComposition.cs` — bind contiguous tenant events and current Party, actor, and approver sources.
- [ ] `src/Hexalith.Agents/Agent/AgentAggregate.cs` and `src/Hexalith.Agents.UI/Components/Pages/ApproverPolicy.razor` — reject Caller and render exact safe basis.
- [ ] `src/Hexalith.Agents.Server/Api/AgentsOperationEndpoints.cs`, `src/Hexalith.Agents.Server/Application/Queries/AgentSetupQueryHandlerBase.cs`, and `src/Hexalith.Agents.Server/Application/AgentInteractions/AgentInteractionGateOrchestrator.cs` — authorize before protected reads or effects.
- [ ] `src/Hexalith.Agents.EventStore/AgentsTrustedCommandExtensionPolicy.cs` and `src/Hexalith.Agents.Server/Ports/EventStoreAgentCommandDispatcher.cs` — verify signed operation/principal/scope and separate logical/delivery ids.
- [ ] `src/Hexalith.Agents/TrustedEnvelopeReplay/TrustedEnvelopeReplayAggregate.cs` and `src/Hexalith.Agents/SecurityEventLog/SecurityEventLogAggregate.cs` — implement pure folds; bind private exact-target replay/spool/worker ports in `src/Hexalith.Agents.Server/Composition/AgentDomainHostComposition.cs`.
- [ ] `eng/verify-story-5.4.ps1` — run focused persisted-state, replay, ACL, cross-tenant, UI, and live compatibility tests only when external targets are `Available`.

**Acceptance Criteria:**
- Given active tenant/Party/approver evidence, when setup is queried, then API/UI expose the same safe basis; revocation or uncertainty blocks before downstream effects.
- Given a signed scoped envelope, when replay admission runs, then only its permitted principal reaches dispatch; exact replay preserves stored first-seen facts and changed fields reject/audit.
- Given a denial during EventStore or worker outage, when recovery runs, then one spool item reaches one acknowledged daily security event; spool outage constructs no target command.
- Given tenant B targets tenant A, when any public or application path denies, then no lookup, effect, count, message, log, or accessible output discloses A.

## Implementation Notes

## Spec Change Log

## Review Triage Log

## Design Notes

The register marks `EXT-CONV-AI-1`, `EXT-HOST-1`, `EXT-PARTIES-1`, and `EXT-SECRETS-1` Uncommitted: full story cannot enter `ready-for-dev`. Branch-B-compatible code alone is not launch evidence. The host lives outside this repository; the spool is host infrastructure, while both security aggregates are EventStore state.

## Verification

**Commands:**
- `pwsh ./eng/verify-story-5.4.ps1` — warning-free builds, individual suites, persisted end state and negative evidence; live lanes only against `Available` targets.
